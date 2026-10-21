// deleteAccount (08 §11; plan F2-8; Q-B13-21): una Lambda para las cuatro rutas de la cuenta.
//   POST /v1/account/code            (JWT)              emite un código de borrado nuevo de 128 bits; guarda solo su
//                                                        hash e invalida los anteriores del UID (rota).
//   POST /v1/account/delete          (JWT + integridad) borra la cuenta desde la app.
//   POST /v1/account/delete-request  (web, sin sesión)  código → token de confirmación de 5 minutos.
//   POST /v1/account/delete-confirm  (web, sin sesión)  token → borra la cuenta.
// El borrado (eraseAccount) es idempotente: borra rewards del UID, quita el uid del libro mayor (la fila queda
// anonimizada el plazo fiscal), borra integrity, el usuario de Firebase Auth y, al final, account_codes, en ese
// orden (si algo falla, el código y el token de la web siguen sirviendo para reintentar).
// Carreras con el borrado (F2c): verifyPurchase, rewardedSsv y el código de borrado escriben en una transacción que
// exige que el UID no esté marcado como borrado, así que nada se escribe a su nombre después de la marca. Lo que se
// escribió justo antes de la marca puede no verse aún en los GSI by_uid (consistencia eventual): por eso la
// limpieza (sweep) se repite tras borrar el usuario de Firebase Auth. Riesgo residual: una fila cuyo GSI tarde más
// que esa llamada en propagarse queda con el uid (rewards caduca a los 30 días; el libro mayor no caduca).
import { DeleteCommand, GetCommand, PutCommand, QueryCommand, TransactWriteCommand, UpdateCommand } from '@aws-sdk/lib-dynamodb';
import type { DynamoDBDocumentClient } from '@aws-sdk/lib-dynamodb';
import { randomBytes } from 'node:crypto';
import { accountCodeHash, newAccountCode, newRequestToken, sha256hex } from '../shared/crypto.ts';
import { ONCE_PER_ACCOUNT_SKUS, onceKey } from '../shared/catalog.ts';
import { cancellationCodes, epochS, isConditionalCheckFailed } from '../shared/ddb.ts';
import type { Env } from '../shared/env.ts';
import { requireEnv } from '../shared/env.ts';
import { HttpError, badRequest } from '../shared/errors.ts';
import type { FirebaseAdmin } from '../shared/firebaseAdmin.ts';
import type { HttpEvent, HttpHandler } from '../shared/http.ts';
import { header, json, jsonBody, requireUid, withErrors } from '../shared/http.ts';
import type { IntegrityVerifier } from '../shared/integrity.ts';
import { INTEGRITY_HEADER, requireIntegrity } from '../shared/integrity.ts';
import { log, metric, uidHash } from '../shared/log.ts';
import { enforce, isDeleted, markDeleted, notDeletedCheck } from '../shared/rateLimit.ts';
import { lazy, realDdb, realFirebaseAdmin, realIntegrityVerifier } from '../shared/runtime.ts';
import { parseAccountDelete, parseDeleteConfirm, parseDeleteRequest } from '../shared/validate.ts';

export interface DeleteAccountDeps {
  readonly ddb: DynamoDBDocumentClient;
  readonly firebase: FirebaseAdmin;
  readonly integrity: IntegrityVerifier;
  readonly env: Env;
  readonly now: () => number;
  readonly random: (n: number) => Buffer;
}

/** Validez del token de confirmación del borrado por web (plan F2-8). */
export const REQUEST_TOKEN_TTL_S = 300;
/** Límites por clave [P] (además de los de API Gateway: 2 rps y ráfaga 5 en las rutas web). */
export const LIMITS = {
  code: { limit: 10, windowS: 86_400 },
  deleteRequest: { limit: 5, windowS: 3600 },
  deleteConfirm: { limit: 10, windowS: 300 },
} as const;
const REQUEST_PREFIX = 'req#';

interface Tables {
  purchases: string;
  rewards: string;
  integrity: string;
  accountCodes: string;
  rateLimits: string;
}

const tablesOf = (env: Env): Tables => ({
  purchases: requireEnv(env, 'AF_TABLE_PURCHASES'),
  rewards: requireEnv(env, 'AF_TABLE_REWARDS'),
  integrity: requireEnv(env, 'AF_TABLE_INTEGRITY'),
  accountCodes: requireEnv(env, 'AF_TABLE_ACCOUNT_CODES'),
  rateLimits: requireEnv(env, 'AF_TABLE_RATE_LIMITS'),
});

/** Todas las claves de un UID en el índice by_uid de `table` (paginado). */
async function keysByUid(ddb: DynamoDBDocumentClient, table: string, pk: string, uid: string): Promise<string[]> {
  const keys: string[] = [];
  let start: Record<string, unknown> | undefined;
  do {
    const out = await ddb.send(
      new QueryCommand({
        TableName: table,
        IndexName: 'by_uid',
        KeyConditionExpression: '#u = :u',
        ExpressionAttributeNames: { '#u': 'uid', '#pk': pk },
        ExpressionAttributeValues: { ':u': uid },
        ProjectionExpression: '#pk',
        ExclusiveStartKey: start,
      }),
    );
    for (const it of (out.Items ?? []) as Array<Record<string, unknown>>) {
      if (typeof it[pk] === 'string') keys.push(it[pk]);
    }
    start = out.LastEvaluatedKey;
  } while (start);
  return keys;
}

interface SweepCounts {
  rewards: number;
  purchases_anonymized: number;
}

/**
 * Una pasada de limpieza: borra las recompensas del UID, anonimiza sus filas del libro mayor, borra su caché de
 * integridad y sus marcas `once_per_account`. Idempotente; eraseAccount la llama dos veces.
 */
async function sweep(deps: DeleteAccountDeps, t: Tables, uid: string, nowMs: number): Promise<SweepCounts> {
  const { ddb } = deps;
  let rewards = 0;
  for (const nonce of await keysByUid(ddb, t.rewards, 'nonce', uid)) {
    try {
      const out = await ddb.send(
        new DeleteCommand({
          TableName: t.rewards,
          Key: { nonce },
          // Solo si sigue siendo de este UID (el índice es eventualmente consistente).
          ConditionExpression: 'attribute_not_exists(nonce) OR #u = :u',
          ExpressionAttributeNames: { '#u': 'uid' },
          ExpressionAttributeValues: { ':u': uid },
          // Solo cuenta lo que de verdad borra (el GSI puede enseñar aún filas ya borradas).
          ReturnValues: 'ALL_OLD',
        }),
      );
      if (out.Attributes) rewards++;
    } catch (e) {
      if (!isConditionalCheckFailed(e)) throw e;
    }
  }

  let anonymized = 0;
  for (const th of await keysByUid(ddb, t.purchases, 'token_hash', uid)) {
    try {
      await ddb.send(
        new UpdateCommand({
          TableName: t.purchases,
          Key: { token_hash: th },
          // Sin uid la fila sale del índice disperso by_uid y queda anonimizada (plan §2).
          UpdateExpression: 'REMOVE #u SET anonymized_at = :t',
          ConditionExpression: '#u = :u',
          ExpressionAttributeNames: { '#u': 'uid' },
          ExpressionAttributeValues: { ':u': uid, ':t': nowMs },
        }),
      );
      anonymized++;
    } catch (e) {
      if (!isConditionalCheckFailed(e)) throw e;
    }
  }

  await ddb.send(new DeleteCommand({ TableName: t.integrity, Key: { uid } }));
  // Marcas «ya comprado» de los SKU once_per_account (F2c): pseudónimas, pero del UID; no se necesitan más.
  for (const sku of ONCE_PER_ACCOUNT_SKUS) {
    await ddb.send(new DeleteCommand({ TableName: t.rateLimits, Key: { key: onceKey(sku, uid) } }));
  }
  return { rewards, purchases_anonymized: anonymized };
}

/** Borra los datos del UID (idempotente; ver la cabecera). Devuelve cuántas filas tocó, para el log. */
export async function eraseAccount(deps: DeleteAccountDeps, t: Tables, uid: string): Promise<Record<string, number>> {
  const { ddb } = deps;
  const nowMs = deps.now();
  // Primero la marca de cuenta borrada: el ID token sigue valiendo hasta 1 h y no debe volver a crear nada.
  await markDeleted(ddb, t.rateLimits, uidHash(uid), nowMs);

  const first = await sweep(deps, t, uid, nowMs);

  // Firebase Auth antes que los códigos: si falla, el código y el token de la web siguen y se puede reintentar.
  const { existed } = await deps.firebase.deleteUser(uid);

  // Segunda pasada (F2c): lo escrito justo antes de la marca que el GSI aún no mostraba en la primera.
  const second = await sweep(deps, t, uid, nowMs);
  if (second.rewards + second.purchases_anonymized > 0) {
    log.warn('erase_second_pass', { uid_hash: uidHash(uid), ...second });
    metric('EraseSecondPass', deps.env.AF_ENV ?? 'dev');
  }

  // Lo último: los códigos de borrado (y los tokens de la web) del UID.
  const codes = await keysByUid(ddb, t.accountCodes, 'code_hash', uid);
  for (const code_hash of codes) {
    await ddb.send(new DeleteCommand({ TableName: t.accountCodes, Key: { code_hash } }));
  }
  return {
    rewards: first.rewards + second.rewards,
    purchases_anonymized: first.purchases_anonymized + second.purchases_anonymized,
    second_pass: second.rewards + second.purchases_anonymized,
    codes: codes.length,
    auth_user: existed ? 1 : 0,
  };
}

async function issueCode(deps: DeleteAccountDeps, t: Tables, uid: string): Promise<string> {
  const nowMs = deps.now();
  if (await isDeleted(deps.ddb, t.rateLimits, uidHash(uid), nowMs)) {
    throw new HttpError(401, 'unauthorized', 'La cuenta se ha borrado.');
  }
  await enforce(deps.ddb, t.rateLimits, `code#${uidHash(uid)}`, LIMITS.code.limit, LIMITS.code.windowS, nowMs);
  const previous = await keysByUid(deps.ddb, t.accountCodes, 'code_hash', uid);
  const code = newAccountCode(deps.random);
  const hash = accountCodeHash(code);
  try {
    await deps.ddb.send(
      new TransactWriteCommand({
        TransactItems: [
          {
            Put: {
              TableName: t.accountCodes,
              Item: { code_hash: hash, uid, kind: 'code', created_at: nowMs },
              ConditionExpression: 'attribute_not_exists(code_hash)',
            },
          },
          // F2c: si el borrado empezó entre isDeleted y aquí, no queda un código a nombre del UID borrado.
          notDeletedCheck(t.rateLimits, uidHash(uid), nowMs),
        ],
      }),
    );
  } catch (e) {
    if (cancellationCodes(e)?.[1] === 'ConditionalCheckFailed') {
      throw new HttpError(401, 'unauthorized', 'La cuenta se ha borrado.');
    }
    throw e;
  }
  // Rota: el código nuevo invalida los anteriores y las confirmaciones web pendientes del UID.
  for (const old of previous) {
    if (old !== hash) await deps.ddb.send(new DeleteCommand({ TableName: t.accountCodes, Key: { code_hash: old } }));
  }
  return code;
}

async function deleteRequest(deps: DeleteAccountDeps, t: Tables, code: string): Promise<{ token: string }> {
  const nowMs = deps.now();
  const hash = accountCodeHash(code);
  await enforce(deps.ddb, t.rateLimits, `delreq#${hash}`, LIMITS.deleteRequest.limit, LIMITS.deleteRequest.windowS, nowMs);
  const item = (await deps.ddb.send(new GetCommand({ TableName: t.accountCodes, Key: { code_hash: hash }, ConsistentRead: true })))
    .Item as { uid?: string; kind?: string } | undefined;
  if (!item || item.kind !== 'code' || typeof item.uid !== 'string') {
    throw new HttpError(404, 'code_not_found', 'Código no encontrado.');
  }
  const token = newRequestToken(deps.random);
  await deps.ddb.send(
    new PutCommand({
      TableName: t.accountCodes,
      Item: {
        code_hash: `${REQUEST_PREFIX}${sha256hex(token)}`,
        uid: item.uid,
        kind: 'request',
        expires_at: nowMs + REQUEST_TOKEN_TTL_S * 1000,
        ttl: epochS(nowMs) + REQUEST_TOKEN_TTL_S,
      },
      ConditionExpression: 'attribute_not_exists(code_hash)',
    }),
  );
  log.info('delete_requested', { uid_hash: uidHash(item.uid) });
  return { token };
}

async function deleteConfirm(deps: DeleteAccountDeps, t: Tables, token: string): Promise<void> {
  const nowMs = deps.now();
  const key = `${REQUEST_PREFIX}${sha256hex(token)}`;
  await enforce(deps.ddb, t.rateLimits, `delconf#${sha256hex(key)}`, LIMITS.deleteConfirm.limit, LIMITS.deleteConfirm.windowS, nowMs);
  const item = (await deps.ddb.send(new GetCommand({ TableName: t.accountCodes, Key: { code_hash: key }, ConsistentRead: true })))
    .Item as { uid?: string; kind?: string; expires_at?: number } | undefined;
  // El TTL de DynamoDB borra tarde: la caducidad se comprueba aquí.
  if (!item || item.kind !== 'request' || typeof item.uid !== 'string' || typeof item.expires_at !== 'number' || item.expires_at <= nowMs) {
    throw new HttpError(410, 'request_expired', 'La solicitud caducó o no existe; vuelve a empezar.');
  }
  // El token se borra solo después del borrado: si eraseAccount falla a medias, la web puede reintentar
  // dentro de los 5 minutos. Dos confirmaciones a la vez repiten un borrado idempotente.
  const counts = await eraseAccount(deps, t, item.uid);
  // Por si el índice by_uid aún no lo veía (consistencia eventual): un solo uso.
  await deps.ddb.send(new DeleteCommand({ TableName: t.accountCodes, Key: { code_hash: key } }));
  log.info('account_deleted', { uid_hash: uidHash(item.uid), source: 'web', ...counts });
  metric('AccountDelete', deps.env.AF_ENV ?? 'dev', { Source: 'web' });
}

export function createHandler(getDeps: () => DeleteAccountDeps): HttpHandler {
  return withErrors('deleteAccount', async (event: HttpEvent) => {
    const deps = getDeps();
    const t = tablesOf(deps.env);
    switch (event.routeKey) {
      case 'POST /v1/account/code': {
        const uid = requireUid(event);
        const code = await issueCode(deps, t, uid);
        log.info('account_code_issued', { uid_hash: uidHash(uid) });
        return json(200, { code });
      }
      case 'POST /v1/account/delete': {
        const uid = requireUid(event);
        parseAccountDelete(jsonBody(event));
        await requireIntegrity(
          {
            ddb: deps.ddb,
            verifier: deps.integrity,
            integrityTable: t.integrity,
            rateLimitsTable: t.rateLimits,
            envName: deps.env.AF_ENV ?? 'dev',
            now: deps.now,
          },
          uid,
          header(event, INTEGRITY_HEADER),
        );
        const counts = await eraseAccount(deps, t, uid);
        log.info('account_deleted', { uid_hash: uidHash(uid), source: 'app', ...counts });
        return json(200, { status: 'deleted' });
      }
      case 'POST /v1/account/delete-request': {
        const { code } = parseDeleteRequest(jsonBody(event));
        const { token } = await deleteRequest(deps, t, code);
        return json(200, { request_token: token, expires_in_s: REQUEST_TOKEN_TTL_S });
      }
      case 'POST /v1/account/delete-confirm': {
        const { requestToken } = parseDeleteConfirm(jsonBody(event));
        await deleteConfirm(deps, t, requestToken);
        return json(200, { status: 'deleted' });
      }
      default:
        throw badRequest('Ruta desconocida.');
    }
  });
}

const deps = lazy(
  (): DeleteAccountDeps => ({
    ddb: realDdb(),
    firebase: realFirebaseAdmin(process.env),
    integrity: realIntegrityVerifier(process.env),
    env: process.env,
    now: Date.now,
    random: randomBytes,
  }),
);

export const handler = createHandler(deps);
