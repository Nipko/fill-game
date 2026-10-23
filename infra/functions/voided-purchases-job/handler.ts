// voidedPurchasesJob (08 §10.1, fila Compra falsa; plan F2-7): job diario de EventBridge Scheduler. Lee el
// checkpoint de af-<env>-jobs, recorre voidedpurchases.list de Google Play página a página y marca `voided` en el
// libro mayor las compras anuladas (reembolsos, contracargos). Idempotente: volver a marcar no cambia nada y el
// checkpoint solo avanza cuando todas las páginas se procesaron. Un fallo lanza: el Scheduler reintenta 3 veces
// y luego deja el evento en la DLQ (alarma VoidedJobFailed). El cliente retira lo anulado en restoreLedger.
// F2c: si el token aún no está en el libro mayor (se anuló antes de que el cliente lo verificara), guarda una
// lápida `voided` sin uid (PutItem condicional si no existe): verifyPurchase responde 400 a ese token y no concede.
// La lápida no tiene uid, así que no sale en restoreLedger ni en el borrado de cuenta; se conserva como el libro
// mayor (sin TTL).
// Q-IAP-4: si la compra se reasignó a otro UID, la fila principal lista en `shadow_keys` las filas sombra de los
// UID que la tuvieron antes; también se marcan `voided` (siempre, aunque la principal ya lo estuviera: así un
// reintento tras un fallo a medias las completa), para que restoreLedger se lo retire también al UID anterior.
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
  /** Lápidas nuevas: anulaciones de tokens que aún no estaban en el libro mayor. */
  readonly tombstones: number;
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
/** UpdateItem en paralelo por tanda. */
export const UPDATE_CONCURRENCY = 25;

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
    let tombstones = 0;
    let shadows = 0;
    let pageToken: string | undefined;
    do {
      if (pages >= MAX_PAGES) throw new Error(`voidedPurchasesJob: más de ${MAX_PAGES} páginas; sin avanzar el checkpoint.`);
      const page = await deps.play.listVoided(startMs, endMs, pageToken);
      pages++;
      type Item = (typeof page.items)[number];
      const markExisting = async (th: string, v: Item): Promise<boolean> => {
        try {
          await deps.ddb.send(
            new UpdateCommand({
              TableName: purchasesTable,
              Key: { token_hash: th },
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
          return true;
        } catch (e) {
          // No está en el libro mayor o ya estaba anulada.
          if (!isConditionalCheckFailed(e)) throw e;
          return false;
        }
      };
      const putTombstone = async (th: string, v: Item): Promise<boolean> => {
        try {
          await deps.ddb.send(
            new PutCommand({
              TableName: purchasesTable,
              Item: {
                token_hash: th,
                state: 'voided',
                tombstone: true,
                voided_at: v.voidedTimeMs,
                voided_reason: v.voidedReason ?? -1,
                voided_source: v.voidedSource ?? -1,
                created_at: endMs,
              },
              ConditionExpression: 'attribute_not_exists(token_hash)',
            }),
          );
          return true;
        } catch (e) {
          if (!isConditionalCheckFailed(e)) throw e;
          return false;
        }
      };
      // Q-IAP-4: marca las sombras de la fila principal (se leen después de marcarla: una reasignación exige
      // `state = ok`, así que ya no puede añadir otra).
      const markShadows = async (th: string, v: Item): Promise<void> => {
        const main = (
          await deps.ddb.send(new GetCommand({ TableName: purchasesTable, Key: { token_hash: th }, ConsistentRead: true }))
        ).Item as { shadow_keys?: unknown } | undefined;
        const keys = Array.isArray(main?.shadow_keys) ? main.shadow_keys.filter((k): k is string => typeof k === 'string') : [];
        for (const k of keys) if (await markExisting(k, v)) shadows++;
      };
      // Resultado de una anulación: 'voided' (fila marcada), 'tombstone' (lápida nueva) o 'none' (ya estaba).
      const markOne = async (v: Item): Promise<'voided' | 'tombstone' | 'none'> => {
        const th = tokenHash(v.purchaseToken);
        const marked = async (): Promise<'voided' | 'tombstone' | 'none'> => {
          if (await markExisting(th, v)) return 'voided';
          if (await putTombstone(th, v)) return 'tombstone';
          // La fila existe: o ya estaba anulada (o es una lápida), o verifyPurchase la escribió entre los dos pasos.
          // Un último intento la marca en el segundo caso.
          return (await markExisting(th, v)) ? 'voided' : 'none';
        };
        const r = await marked();
        if (r !== 'tombstone') await markShadows(th, v);
        return r;
      };
      // En tandas de UPDATE_CONCURRENCY para que 1.000 anulaciones por página quepan en el tiempo del job.
      for (let i = 0; i < page.items.length; i += UPDATE_CONCURRENCY) {
        const marked = await Promise.all(page.items.slice(i, i + UPDATE_CONCURRENCY).map(markOne));
        voided += marked.filter((m) => m === 'voided').length;
        tombstones += marked.filter((m) => m === 'tombstone').length;
      }
      pageToken = page.nextPageToken;
    } while (pageToken);

    await deps.ddb.send(
      new PutCommand({ TableName: jobsTable, Item: { job: JOB_ID, cursor_ms: endMs, updated_at: endMs } }),
    );
    log.info('voided_job_ok', { pages, voided, tombstones, shadows, start_ms: startMs, cursor_ms: endMs });
    return { status: 'ok', pages, voided, tombstones, cursor_ms: endMs };
  };
}

const deps = lazy((): VoidedJobDeps => ({ ddb: realDdb(), play: realPlayApi(process.env), env: process.env, now: Date.now }));

export const handler = createHandler(deps);
