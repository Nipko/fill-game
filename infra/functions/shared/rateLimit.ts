// Límites de peticiones y marcas de un solo uso en la tabla af-<env>-rate_limits (plan §2, Abuso; Q-AWS-10).
// Las claves nunca llevan datos en claro: el que llama pasa un hash (UID, código o token).
import { GetCommand, PutCommand, UpdateCommand } from '@aws-sdk/lib-dynamodb';
import type { DynamoDBDocumentClient } from '@aws-sdk/lib-dynamodb';
import { epochS, isConditionalCheckFailed } from './ddb.ts';
import { rateLimited } from './errors.ts';

export interface RateLimitResult {
  readonly allowed: boolean;
  readonly count: number;
  /** Segundos hasta que se abre la siguiente ventana. */
  readonly retryAfterS: number;
}

/**
 * Cuenta una petición en la ventana fija actual de `windowS` segundos para `key` (UpdateItem con ADD,
 * atómico) y dice si sigue dentro de `limit`. El contador caduca por TTL al acabar la ventana.
 */
export async function hit(
  ddb: DynamoDBDocumentClient,
  table: string,
  key: string,
  limit: number,
  windowS: number,
  nowMs: number,
): Promise<RateLimitResult> {
  const nowS = epochS(nowMs);
  const window = Math.floor(nowS / windowS);
  const windowEnd = (window + 1) * windowS;
  const out = await ddb.send(
    new UpdateCommand({
      TableName: table,
      Key: { key: `${key}#${windowS}#${window}` },
      UpdateExpression: 'ADD #c :one SET #t = if_not_exists(#t, :ttl)',
      ExpressionAttributeNames: { '#c': 'count', '#t': 'ttl' },
      ExpressionAttributeValues: { ':one': 1, ':ttl': windowEnd + 60 },
      ReturnValues: 'UPDATED_NEW',
    }),
  );
  const count = Number((out.Attributes as { count?: number } | undefined)?.count ?? 1);
  return { allowed: count <= limit, count, retryAfterS: Math.max(1, windowEnd - nowS) };
}

/** Como `hit`, pero lanza 429 rate_limited con retry_after_s si se pasa del límite. */
export async function enforce(
  ddb: DynamoDBDocumentClient,
  table: string,
  key: string,
  limit: number,
  windowS: number,
  nowMs: number,
): Promise<void> {
  const r = await hit(ddb, table, key, limit, windowS, nowMs);
  if (!r.allowed) throw rateLimited(r.retryAfterS);
}

/** Clave de la marca de cuenta borrada (sin el UID en claro). */
export const deletedKey = (uidHashValue: string): string => `deleted#${uidHashValue}`;
/**
 * Vida de la marca de cuenta borrada: más que la del ID token de Firebase (1 h), que el autorizador JWT sigue
 * aceptando tras el borrado porque no consulta revocaciones.
 */
export const DELETED_MARK_TTL_S = 2 * 3600;

/** Marca el UID como borrado (lo escribe deleteAccount al empezar el borrado). */
export async function markDeleted(ddb: DynamoDBDocumentClient, table: string, uidHashValue: string, nowMs: number): Promise<void> {
  await ddb.send(new PutCommand({ TableName: table, Item: { key: deletedKey(uidHashValue), ttl: epochS(nowMs) + DELETED_MARK_TTL_S } }));
}

/** ¿El UID se borró hace menos de DELETED_MARK_TTL_S? (El TTL de DynamoDB borra tarde: se mira la caducidad.) */
export async function isDeleted(ddb: DynamoDBDocumentClient, table: string, uidHashValue: string, nowMs: number): Promise<boolean> {
  const out = await ddb.send(new GetCommand({ TableName: table, Key: { key: deletedKey(uidHashValue) }, ConsistentRead: true }));
  const ttl = (out.Item as { ttl?: number } | undefined)?.ttl;
  return typeof ttl === 'number' && ttl > epochS(nowMs);
}

/**
 * Elemento ConditionCheck de un TransactWriteItems que exige que el UID **no** esté marcado como borrado (la
 * misma regla que `isDeleted`, pero atómica con la escritura). Cierra la carrera «comprobé que no estaba
 * borrado → deleteAccount marca y limpia → escribo a nombre del UID borrado» (F2c).
 */
export function notDeletedCheck(table: string, uidHashValue: string, nowMs: number) {
  return {
    ConditionCheck: {
      TableName: table,
      Key: { key: deletedKey(uidHashValue) },
      ConditionExpression: 'attribute_not_exists(#k) OR #t <= :now',
      ExpressionAttributeNames: { '#k': 'key', '#t': 'ttl' },
      ExpressionAttributeValues: { ':now': epochS(nowMs) },
    },
  };
}

/**
 * Marca `key` como usada hasta `expiresAtS` (PutItem condicional). Devuelve false si ya estaba marcada:
 * sirve para que un token de uso limitado no se pueda repetir (anti-replay).
 */
export async function consumeOnce(
  ddb: DynamoDBDocumentClient,
  table: string,
  key: string,
  expiresAtS: number,
): Promise<boolean> {
  try {
    await ddb.send(
      new PutCommand({
        TableName: table,
        Item: { key, ttl: expiresAtS },
        ConditionExpression: 'attribute_not_exists(#k)',
        ExpressionAttributeNames: { '#k': 'key' },
      }),
    );
    return true;
  } catch (e) {
    if (isConditionalCheckFailed(e)) return false;
    throw e;
  }
}
