// rewardedSsv (08 §10.1 y §10.2; plan F2-6; D-049): callback de verificación en el servidor (SSV) de AdMob cuando un jugador
// termina un anuncio recompensado. Sin JWT: lo autentica la firma ECDSA de AdMob sobre la consulta cruda (admobSignature.ts),
// con la clave pública de Google por `key_id`. Anota rewards/{nonce} = {uid, placement, network, ts} con TTL de 30 días; un
// nonce repetido responde 200 duplicate sin escribir (idempotente). El cliente confirma sus nonces pendientes con
// restoreLedger. F2c: la fila de rewards se escribe en una transacción con la comprobación de que el UID no está marcado como
// borrado, para que un callback que llegue durante el borrado no deje el UID en rewards.
import { GetCommand, PutCommand, TransactWriteCommand, UpdateCommand } from '@aws-sdk/lib-dynamodb';
import type { DynamoDBDocumentClient } from '@aws-sdk/lib-dynamodb';
import { cancellationCodes, epochS, isConditionalCheckFailed } from '../shared/ddb.ts';
import type { Env } from '../shared/env.ts';
import { requireEnv } from '../shared/env.ts';
import { HttpError, badRequest } from '../shared/errors.ts';
import type { HttpEvent, HttpHandler } from '../shared/http.ts';
import { json, withErrors } from '../shared/http.ts';
import { sha256hex } from '../shared/crypto.ts';
import { log, uidHash } from '../shared/log.ts';
import { enforce, isDeleted, notDeletedCheck } from '../shared/rateLimit.ts';
import { lazy, realDdb } from '../shared/runtime.ts';
import { adUnitNumber, hasSsvIdentity, parseAdUnitList, parseSsvQuery } from '../shared/validate.ts';
import type { VerifierKeySource } from './admobSignature.ts';
import { AdMobVerifierKeys, splitSignedQuery, verifyAdMobSignature } from './admobSignature.ts';

export interface RewardedSsvDeps {
  readonly ddb: DynamoDBDocumentClient;
  readonly keys: VerifierKeySource;
  readonly env: Env;
  readonly now: () => number;
}

/** Retención de rewards (plan §2 y §5: 30 días [P]). */
export const REWARD_TTL_S = 30 * 24 * 3600;
/** Tope de recompensas confirmadas por UID y día [P]: muy por encima de los topes de 04 §6.2 (ads_rv_*). */
export const SSV_DAILY_LIMIT = 100;
/**
 * Antigüedad máxima del `timestamp` de AdMob [P]: 24 h, el plazo `sec_ssv_grace_h` tras el que el cliente revoca la concesión
 * provisional (08 §10.2); una confirmación más vieja ya no sirve.
 */
export const SSV_MAX_AGE_MS = 24 * 3600_000;
/** Margen para un `timestamp` en el futuro (relojes) [P]. */
export const SSV_MAX_SKEW_MS = 10 * 60_000;
/** Longitud máxima de la consulta cruda. */
export const MAX_QUERY_LENGTH = 8192;

const badSignature = (): HttpError => new HttpError(403, 'bad_signature', 'Firma no válida.');

/** Lee la parte firmada como parámetros, sin admitir claves repetidas (lo firmado y lo leído son lo mismo). */
function signedParams(message: string): Record<string, string> {
  const out: Record<string, string> = {};
  for (const [k, v] of new URLSearchParams(message)) {
    if (Object.prototype.hasOwnProperty.call(out, k)) throw badRequest(`Parámetro repetido: ${k.slice(0, 40)}.`);
    out[k] = v;
  }
  return out;
}

export function createHandler(getDeps: () => RewardedSsvDeps): HttpHandler {
  return withErrors('rewardedSsv', async (event: HttpEvent) => {
    const deps = getDeps();
    const raw = event.rawQueryString ?? '';
    if (raw.length > MAX_QUERY_LENGTH) throw badRequest('Consulta demasiado larga.');

    // 1) Firma de AdMob sobre la consulta tal cual llega (sin reordenar ni decodificar).
    const signed = splitSignedQuery(raw);
    if (!signed) {
      log.warn('ssv_bad_signature', { reason: 'format' });
      throw badSignature();
    }
    const key = await deps.keys.get(signed.keyId);
    if (!key) {
      log.warn('ssv_bad_signature', { reason: 'unknown_key', key_id: signed.keyId });
      throw badSignature();
    }
    if (!verifyAdMobSignature(signed.message, signed.signature, key)) {
      log.warn('ssv_bad_signature', { reason: 'mismatch', key_id: signed.keyId });
      throw badSignature();
    }

    // 2) Parámetros (solo los firmados).
    const q = signedParams(signed.message);
    // Las claves de Google son las mismas para todos los editores: solo valen nuestros bloques recompensados (lista blanca
    // del entorno, AF_ADMOB_AD_UNITS). Un bloque ajeno o sin ad_unit: 200 ignored, sin escribir ni gastar cupo.
    const ownUnits = parseAdUnitList(requireEnv(deps.env, 'AF_ADMOB_AD_UNITS'));
    const unit = adUnitNumber(q.ad_unit);
    if (!unit || !ownUnits.has(unit)) {
      log.warn('ssv_foreign_ad_unit', { ad_unit: unit ?? (q.ad_unit === undefined ? 'none' : 'malformed') });
      return json(200, { status: 'ignored' });
    }
    // Sin user_id o custom_data no hay nada que anotar: 200 sin escribir.
    if (!hasSsvIdentity(q)) {
      log.info('ssv_ignored', { has_user: !!q.user_id, has_custom_data: !!q.custom_data });
      return json(200, { status: 'ignored' });
    }
    const params = parseSsvQuery(q);
    const nowMs = deps.now();
    if (params.timestampMs > nowMs + SSV_MAX_SKEW_MS || params.timestampMs < nowMs - SSV_MAX_AGE_MS) {
      log.warn('ssv_expired', { placement: params.placement, age_s: Math.round((nowMs - params.timestampMs) / 1000) });
      throw new HttpError(400, 'request_expired', 'timestamp fuera de plazo.');
    }

    const rateLimitsTable = requireEnv(deps.env, 'AF_TABLE_RATE_LIMITS');
    const fields = { uid_hash: uidHash(params.uid), placement: params.placement, network: params.network };

    // Cada transaction_id vale para un único (uid, nonce). Una repetición con los mismos valores sigue (y acaba en
    // duplicate); el mismo transaction_id con otro uid o nonce → 403.
    // Cuenta borrada hace menos de 2 h: 200 sin escribir (no queda el UID en rewards).
    if (await isDeleted(deps.ddb, rateLimitsTable, uidHash(params.uid), nowMs)) {
      log.info('ssv_deleted_uid', fields);
      return json(200, { status: 'duplicate' });
    }

    const binding = sha256hex(`${params.uid}|${params.nonce}`);
    const eventKey = `ssv_event#${sha256hex(params.transactionId)}`;
    let counted = false;
    try {
      await deps.ddb.send(
        new PutCommand({
          TableName: rateLimitsTable,
          Item: { key: eventKey, binding, ttl: epochS(nowMs) + REWARD_TTL_S },
          ConditionExpression: 'attribute_not_exists(#k)',
          ExpressionAttributeNames: { '#k': 'key' },
        }),
      );
    } catch (e) {
      if (!isConditionalCheckFailed(e)) throw e;
      const prev = (await deps.ddb.send(new GetCommand({ TableName: rateLimitsTable, Key: { key: eventKey }, ConsistentRead: true })))
        .Item as { binding?: string; counted?: boolean } | undefined;
      if (prev?.binding !== binding) {
        log.warn('ssv_event_reused', fields);
        throw new HttpError(403, 'bad_signature', 'Firma ya usada.');
      }
      counted = prev.counted === true;
    }
    // Límite por UID, después de la firma (un tercero sin la clave de Google no gasta el cupo de un UID). Una repetición
    // solo se lo salta si la llamada anterior lo pasó (marca `counted`); si aquella dio 429, la repetición vuelve a pasar
    // por el límite y sigue en 429.
    if (!counted) {
      await enforce(deps.ddb, rateLimitsTable, `ssv#${uidHash(params.uid)}`, SSV_DAILY_LIMIT, 86_400, nowMs);
      await deps.ddb.send(
        new UpdateCommand({
          TableName: rateLimitsTable,
          Key: { key: eventKey },
          UpdateExpression: 'SET counted = :t',
          ExpressionAttributeValues: { ':t': true },
        }),
      );
    }

    try {
      await deps.ddb.send(
        new TransactWriteCommand({
          TransactItems: [
            {
              Put: {
                TableName: requireEnv(deps.env, 'AF_TABLE_REWARDS'),
                Item: {
                  nonce: params.nonce,
                  uid: params.uid,
                  placement: params.placement,
                  network: params.network ?? 'admob',
                  ts: nowMs,
                  ttl: epochS(nowMs) + REWARD_TTL_S,
                },
                ConditionExpression: 'attribute_not_exists(nonce)',
              },
            },
            notDeletedCheck(rateLimitsTable, uidHash(params.uid), nowMs),
          ],
        }),
      );
    } catch (e) {
      const codes = cancellationCodes(e);
      if (codes?.[1] === 'ConditionalCheckFailed') {
        log.info('ssv_deleted_uid', { ...fields, race: true });
        return json(200, { status: 'duplicate' });
      }
      if (codes?.[0] === 'ConditionalCheckFailed') {
        log.info('ssv_duplicate', fields);
        return json(200, { status: 'duplicate' });
      }
      // TransactionConflict u otro fallo: 500 (una repetición acaba en ok o duplicate).
      throw e;
    }
    log.info('ssv_ok', fields);
    return json(200, { status: 'ok' });
  });
}

/** Caché de claves de AdMob del contenedor (vive entre invocaciones; TTL 24 h). */
const adMobKeys = new AdMobVerifierKeys();

const deps = lazy((): RewardedSsvDeps => ({ ddb: realDdb(), keys: adMobKeys, env: process.env, now: Date.now }));

export const handler = createHandler(deps);
