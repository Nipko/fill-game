// rewardedSsv (08 §10.1 y §10.2; plan F2-6): callback servidor a servidor de MAX cuando un jugador termina un
// anuncio recompensado. Sin JWT: lo autentica la firma de MAX (maxSignature.ts, [NV]) con la clave de Secrets
// Manager. Anota rewards/{nonce} = {uid, placement, network, ts} con TTL de 30 días; un nonce repetido responde
// 200 duplicate sin escribir (idempotente: MAX reintenta si no recibe 200). El cliente confirma sus nonces
// pendientes con restoreLedger.
import { PutCommand } from '@aws-sdk/lib-dynamodb';
import type { DynamoDBDocumentClient } from '@aws-sdk/lib-dynamodb';
import { epochS, isConditionalCheckFailed } from '../shared/ddb.ts';
import type { Env } from '../shared/env.ts';
import { requireEnv } from '../shared/env.ts';
import { HttpError } from '../shared/errors.ts';
import type { HttpEvent, HttpHandler } from '../shared/http.ts';
import { json, withErrors } from '../shared/http.ts';
import { log, uidHash } from '../shared/log.ts';
import { enforce } from '../shared/rateLimit.ts';
import { lazy, realDdb, realSecrets } from '../shared/runtime.ts';
import type { SecretSource } from '../shared/secrets.ts';
import { parseSsvQuery } from '../shared/validate.ts';
import { verifyMaxCallback } from './maxSignature.ts';

export interface RewardedSsvDeps {
  readonly ddb: DynamoDBDocumentClient;
  readonly secrets: SecretSource;
  readonly env: Env;
  readonly now: () => number;
}

/** Retención de rewards (plan §2 y §5: 30 días [P]). */
export const REWARD_TTL_S = 30 * 24 * 3600;
/** Tope de recompensas confirmadas por UID y día [P]: muy por encima de los topes de 04 §6.2 (ads_rv_*). */
export const SSV_DAILY_LIMIT = 100;

export function createHandler(getDeps: () => RewardedSsvDeps): HttpHandler {
  return withErrors('rewardedSsv', async (event: HttpEvent) => {
    const deps = getDeps();
    const params = parseSsvQuery(event.queryStringParameters ?? {});
    const key = (await deps.secrets.get(requireEnv(deps.env, 'AF_SECRET_MAX_SSV'))).trim();
    if (!verifyMaxCallback(params.eventId, params.eventToken, key)) {
      log.warn('ssv_bad_signature', { placement: params.placement });
      throw new HttpError(403, 'bad_signature', 'Firma no válida.');
    }

    const nowMs = deps.now();
    const fields = { uid_hash: uidHash(params.uid), placement: params.placement, network: params.network };
    // Después de la firma: un tercero sin la clave no puede gastar el cupo de un UID.
    await enforce(deps.ddb, requireEnv(deps.env, 'AF_TABLE_RATE_LIMITS'), `ssv#${uidHash(params.uid)}`, SSV_DAILY_LIMIT, 86_400, nowMs);

    try {
      await deps.ddb.send(
        new PutCommand({
          TableName: requireEnv(deps.env, 'AF_TABLE_REWARDS'),
          Item: {
            nonce: params.nonce,
            uid: params.uid,
            placement: params.placement,
            network: params.network ?? 'applovin',
            ts: nowMs,
            ttl: epochS(nowMs) + REWARD_TTL_S,
          },
          ConditionExpression: 'attribute_not_exists(nonce)',
        }),
      );
    } catch (e) {
      if (!isConditionalCheckFailed(e)) throw e;
      log.info('ssv_duplicate', fields);
      return json(200, { status: 'duplicate' });
    }
    log.info('ssv_ok', fields);
    return json(200, { status: 'ok' });
  });
}

const deps = lazy((): RewardedSsvDeps => ({ ddb: realDdb(), secrets: realSecrets(), env: process.env, now: Date.now }));

export const handler = createHandler(deps);
