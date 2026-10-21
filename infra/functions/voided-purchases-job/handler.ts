// voidedPurchasesJob (08 §10.1, fila Compra falsa; plan F2-7): job diario de EventBridge Scheduler. Lee el
// checkpoint de af-<env>-jobs, recorre voidedpurchases.list de Google Play página a página y marca `voided` en el
// libro mayor las compras anuladas (reembolsos, contracargos). Idempotente: volver a marcar no cambia nada y el
// checkpoint solo avanza cuando todas las páginas se procesaron. Un fallo lanza: el Scheduler reintenta 3 veces
// y luego deja el evento en la DLQ (alarma VoidedJobFailed). El cliente retira lo anulado en restoreLedger.
import { GetCommand, PutCommand, UpdateCommand } from '@aws-sdk/lib-dynamodb';
import type { DynamoDBDocumentClient } from '@aws-sdk/lib-dynamodb';
import { tokenHash } from '../shared/crypto.ts';
import { isConditionalCheckFailed } from '../shared/ddb.ts';
import type { Env } from '../shared/env.ts';
import { requireEnv } from '../shared/env.ts';
import { log } from '../shared/log.ts';
import type { PlayApi } from '../shared/playApi.ts';
import { lazy, realDdb, realPlayApi } from '../shared/runtime.ts';

export interface JobResult {
  readonly status: 'ok';
  readonly pages: number;
  readonly voided: number;
  readonly cursor_ms: number;
}

export interface VoidedJobDeps {
  readonly ddb: DynamoDBDocumentClient;
  readonly play: PlayApi;
  readonly env: Env;
  readonly now: () => number;
}

export const JOB_ID = 'voided-purchases';
/** Play no deja pedir anulaciones de hace más de 30 días; se deja un margen de 1 hora. */
export const MAX_LOOKBACK_MS = 30 * 24 * 3600_000 - 3600_000;
/** Solapamiento con la ventana anterior [P]: una anulación que Play publique con retraso no se pierde. */
export const OVERLAP_MS = 24 * 3600_000;
/** Tope de páginas por ejecución (1.000 anulaciones por página): si se pasa, falla y avisa la alarma. */
export const MAX_PAGES = 50;

export function createHandler(getDeps: () => VoidedJobDeps): () => Promise<JobResult> {
  return async () => {
    const deps = getDeps();
    const jobsTable = requireEnv(deps.env, 'AF_TABLE_JOBS');
    const purchasesTable = requireEnv(deps.env, 'AF_TABLE_PURCHASES');
    const endMs = deps.now();

    const cp = (await deps.ddb.send(new GetCommand({ TableName: jobsTable, Key: { job: JOB_ID }, ConsistentRead: true })))
      .Item as { cursor_ms?: number } | undefined;
    const oldest = endMs - MAX_LOOKBACK_MS;
    let startMs = typeof cp?.cursor_ms === 'number' ? cp.cursor_ms - OVERLAP_MS : oldest;
    if (startMs < oldest) {
      if (cp?.cursor_ms !== undefined) log.warn('voided_gap', { cursor_ms: cp.cursor_ms, oldest_ms: oldest });
      startMs = oldest;
    }

    let pages = 0;
    let voided = 0;
    let pageToken: string | undefined;
    do {
      if (pages >= MAX_PAGES) throw new Error(`voidedPurchasesJob: más de ${MAX_PAGES} páginas; sin avanzar el checkpoint.`);
      const page = await deps.play.listVoided(startMs, endMs, pageToken);
      pages++;
      for (const v of page.items) {
        try {
          await deps.ddb.send(
            new UpdateCommand({
              TableName: purchasesTable,
              Key: { token_hash: tokenHash(v.purchaseToken) },
              UpdateExpression: 'SET #s = :voided, voided_at = :at, voided_reason = :r, voided_source = :src',
              ConditionExpression: 'attribute_exists(token_hash) AND #s <> :voided',
              ExpressionAttributeNames: { '#s': 'state' },
              ExpressionAttributeValues: {
                ':voided': 'voided',
                ':at': v.voidedTimeMs,
                ':r': v.voidedReason ?? -1,
                ':src': v.voidedSource ?? -1,
              },
            }),
          );
          voided++;
        } catch (e) {
          // No está en el libro mayor (nunca se verificó) o ya estaba anulada: nada que hacer.
          if (!isConditionalCheckFailed(e)) throw e;
        }
      }
      pageToken = page.nextPageToken;
    } while (pageToken);

    await deps.ddb.send(
      new PutCommand({ TableName: jobsTable, Item: { job: JOB_ID, cursor_ms: endMs, updated_at: endMs } }),
    );
    log.info('voided_job_ok', { pages, voided, start_ms: startMs, cursor_ms: endMs });
    return { status: 'ok', pages, voided, cursor_ms: endMs };
  };
}

const deps = lazy((): VoidedJobDeps => ({ ddb: realDdb(), play: realPlayApi(process.env), env: process.env, now: Date.now }));

export const handler = createHandler(deps);
