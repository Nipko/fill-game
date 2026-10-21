// restoreLedger (08 §11, fila Conflicto, y §10.2; plan F2-5 y hallazgo 5): devuelve las compras del UID
// (índice by_uid del libro mayor, con su estado ok o voided) y, de los nonces de recompensa que el cliente
// tiene pendientes, los que el servidor ya confirmó por SSV para este UID. Sin integridad (solo lectura).
import { BatchGetCommand, QueryCommand } from '@aws-sdk/lib-dynamodb';
import type { DynamoDBDocumentClient } from '@aws-sdk/lib-dynamodb';
import type { Env } from '../shared/env.ts';
import { requireEnv } from '../shared/env.ts';
import type { HttpEvent, HttpHandler } from '../shared/http.ts';
import { json, jsonBody, requireUid, withErrors } from '../shared/http.ts';
import { log, uidHash } from '../shared/log.ts';
import { lazy, realDdb } from '../shared/runtime.ts';
import { parseRestoreLedger } from '../shared/validate.ts';

export interface RestoreLedgerDeps {
  readonly ddb: DynamoDBDocumentClient;
  readonly env: Env;
}

/** Tope del contrato (restore-ledger.response.schema.json: maxItems 1000). */
export const MAX_PURCHASES = 1000;
const BATCH_GET_MAX = 100;
const BATCH_RETRIES = 3;

interface PurchaseRow {
  token_hash: string;
  sku: string;
  state: 'ok' | 'voided';
  ts: number;
  voided_at?: number;
}

async function purchasesOf(ddb: DynamoDBDocumentClient, table: string, uid: string): Promise<PurchaseRow[]> {
  const rows: PurchaseRow[] = [];
  let start: Record<string, unknown> | undefined;
  do {
    const out = await ddb.send(
      new QueryCommand({
        TableName: table,
        IndexName: 'by_uid',
        KeyConditionExpression: '#u = :u',
        ExpressionAttributeNames: { '#u': 'uid', '#s': 'state', '#t': 'ts' },
        ExpressionAttributeValues: { ':u': uid },
        ProjectionExpression: 'token_hash, sku, #s, #t, voided_at',
        ExclusiveStartKey: start,
        Limit: MAX_PURCHASES,
      }),
    );
    for (const it of (out.Items ?? []) as Array<Record<string, unknown>>) {
      if (rows.length >= MAX_PURCHASES) break;
      rows.push({
        token_hash: String(it.token_hash),
        sku: String(it.sku),
        state: it.state === 'voided' ? 'voided' : 'ok',
        ts: Number(it.ts),
        ...(typeof it.voided_at === 'number' ? { voided_at: it.voided_at } : {}),
      });
    }
    start = out.LastEvaluatedKey;
  } while (start && rows.length < MAX_PURCHASES);
  if (start) log.warn('restore_truncated', { uid_hash: uidHash(uid), max: MAX_PURCHASES });
  return rows;
}

async function confirmedNonces(
  ddb: DynamoDBDocumentClient,
  table: string,
  uid: string,
  nonces: readonly string[],
): Promise<string[]> {
  const confirmed = new Set<string>();
  for (let i = 0; i < nonces.length; i += BATCH_GET_MAX) {
    let keys: Array<Record<string, unknown>> = nonces.slice(i, i + BATCH_GET_MAX).map((nonce) => ({ nonce }));
    for (let attempt = 0; keys.length > 0; attempt++) {
      if (attempt > BATCH_RETRIES) {
        // Sin confirmar no es lo mismo que rechazado: el cliente vuelve a preguntar; mejor fallar que callar.
        throw new Error('BatchGetItem dejó claves sin procesar tras los reintentos.');
      }
      const out = await ddb.send(
        new BatchGetCommand({
          RequestItems: {
            [table]: {
              Keys: keys,
              ConsistentRead: true,
              ProjectionExpression: 'nonce, #u',
              ExpressionAttributeNames: { '#u': 'uid' },
            },
          },
        }),
      );
      for (const it of (out.Responses?.[table] ?? []) as Array<{ nonce?: string; uid?: string }>) {
        if (it.uid === uid && typeof it.nonce === 'string') confirmed.add(it.nonce);
      }
      keys = (out.UnprocessedKeys?.[table]?.Keys ?? []) as Array<Record<string, unknown>>;
    }
  }
  // Mismo orden que pidió el cliente.
  return nonces.filter((n) => confirmed.has(n));
}

export function createHandler(getDeps: () => RestoreLedgerDeps): HttpHandler {
  return withErrors('restoreLedger', async (event: HttpEvent) => {
    const deps = getDeps();
    const uid = requireUid(event);
    const req = parseRestoreLedger(jsonBody(event));
    const [purchases, confirmed] = await Promise.all([
      purchasesOf(deps.ddb, requireEnv(deps.env, 'AF_TABLE_PURCHASES'), uid),
      confirmedNonces(deps.ddb, requireEnv(deps.env, 'AF_TABLE_REWARDS'), uid, req.pendingNonces),
    ]);
    log.info('restore_ok', { uid_hash: uidHash(uid), purchases: purchases.length, nonces: req.pendingNonces.length, confirmed: confirmed.length });
    return json(200, { purchases, confirmed_nonces: confirmed });
  });
}

const deps = lazy((): RestoreLedgerDeps => ({ ddb: realDdb(), env: process.env }));

export const handler = createHandler(deps);
