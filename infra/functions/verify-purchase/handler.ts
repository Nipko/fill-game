// verifyPurchase (08 §10.3; plan F2-4): verifica la compra en Google Play, la reconoce y la anota en el libro
// mayor af-<env>-purchases. Exige el JWT de Firebase (autorizador de API Gateway) y la integridad (Q-AWS-4).
//
// Orden pensado para que el dinero no se conceda dos veces ni se pierda:
//   1. validar el cuerpo y el SKU; 2. integridad; 3. si el token ya está en el libro mayor → duplicate (mismo
//   UID) o 409 (otro UID o anonimizado), sin llamar a Play; 4. products.get: pendiente → pending (no concede),
//   cancelada o inexistente → 400, comprada a nombre de otra cuenta → 409; 5. acknowledge si falta (antes de
//   escribir: si después falla la escritura, el reintento ve la compra ya reconocida y la escribe); 6. PutItem
//   condicional (attribute_not_exists): solo una petición gana; la que pierde la carrera responde duplicate o 409.
// Solo `ok` lleva `grant`. En `duplicate` (p. ej. se perdió la respuesta del `ok`), el cliente acredita solo si
// el token_hash no está en applied_purchases: la misma regla que con restoreLedger (08 §11), sin doble concesión.
import { GetCommand, PutCommand } from '@aws-sdk/lib-dynamodb';
import type { DynamoDBDocumentClient } from '@aws-sdk/lib-dynamodb';
import { CATALOG, grantFor } from '../shared/catalog.ts';
import { sha256hex, tokenHash as hashToken } from '../shared/crypto.ts';
import { isConditionalCheckFailed } from '../shared/ddb.ts';
import type { Env } from '../shared/env.ts';
import { requireEnv } from '../shared/env.ts';
import { HttpError, badRequest } from '../shared/errors.ts';
import type { HttpEvent, HttpHandler } from '../shared/http.ts';
import { header, json, jsonBody, requireUid, withErrors } from '../shared/http.ts';
import type { IntegrityVerifier } from '../shared/integrity.ts';
import { INTEGRITY_HEADER, requireIntegrity } from '../shared/integrity.ts';
import { log, uidHash } from '../shared/log.ts';
import type { PlayApi, ProductPurchase } from '../shared/playApi.ts';
import { lazy, realDdb, realIntegrityVerifier, realPlayApi } from '../shared/runtime.ts';
import { parseVerifyPurchase } from '../shared/validate.ts';

export interface VerifyPurchaseDeps {
  readonly ddb: DynamoDBDocumentClient;
  readonly play: PlayApi;
  readonly integrity: IntegrityVerifier;
  readonly env: Env;
  readonly now: () => number;
}

interface LedgerItem {
  readonly token_hash: string;
  readonly uid?: string;
  readonly state?: string;
}

const otherUser = (): HttpError =>
  new HttpError(409, 'token_belongs_to_other_user', 'La compra está anotada a nombre de otra cuenta.');

/**
 * ¿La cuenta ofuscada que guarda Play es la de este UID? 08 §10.1 pide setObfuscatedAccountId = UID; se admite
 * también su SHA-256 por si el cliente lo pasa con hash (Play recomienda no enviar identificadores en claro).
 */
export function obfuscatedMatches(obfuscated: string | undefined, uid: string): boolean {
  return obfuscated === undefined || obfuscated === uid || obfuscated === sha256hex(uid);
}

export function createHandler(getDeps: () => VerifyPurchaseDeps): HttpHandler {
  return withErrors('verifyPurchase', async (event: HttpEvent) => {
    const deps = getDeps();
    const uid = requireUid(event);
    const req = parseVerifyPurchase(jsonBody(event));
    const product = CATALOG.get(req.sku);
    if (!product) throw badRequest('SKU fuera del catálogo de 1.0.');

    const purchasesTable = requireEnv(deps.env, 'AF_TABLE_PURCHASES');
    await requireIntegrity(
      {
        ddb: deps.ddb,
        verifier: deps.integrity,
        integrityTable: requireEnv(deps.env, 'AF_TABLE_INTEGRITY'),
        rateLimitsTable: requireEnv(deps.env, 'AF_TABLE_RATE_LIMITS'),
        envName: deps.env.AF_ENV ?? 'dev',
        now: deps.now,
      },
      uid,
      header(event, INTEGRITY_HEADER),
    );

    const th = hashToken(req.token);
    const fields = { uid_hash: uidHash(uid), sku: req.sku, token_hash: th.slice(0, 12) };

    const readLedger = async (): Promise<LedgerItem | undefined> =>
      (await deps.ddb.send(new GetCommand({ TableName: purchasesTable, Key: { token_hash: th }, ConsistentRead: true })))
        .Item as LedgerItem | undefined;
    const answerExisting = (item: LedgerItem): ReturnType<typeof json> => {
      if (item.uid === uid) {
        log.info('purchase_duplicate', fields);
        return json(200, { status: 'duplicate', token_hash: th });
      }
      log.warn('purchase_other_user', { ...fields, anonymized: item.uid === undefined });
      throw otherUser();
    };

    const existing = await readLedger();
    if (existing) return answerExisting(existing);

    const p: ProductPurchase | null = await deps.play.getProduct(req.sku, req.token);
    if (!p) throw badRequest('Google Play no reconoce esa compra.');
    if (!obfuscatedMatches(p.obfuscatedAccountId, uid)) {
      log.warn('purchase_obfuscated_mismatch', fields);
      throw otherUser();
    }
    if (p.state === 'pending') {
      log.info('purchase_pending', fields);
      return json(200, { status: 'pending' });
    }
    if (p.state !== 'purchased') {
      log.info('purchase_canceled', fields);
      throw badRequest('La compra está cancelada.');
    }
    if (!p.acknowledged) await deps.play.acknowledge(req.sku, req.token);

    const nowMs = deps.now();
    try {
      await deps.ddb.send(
        new PutCommand({
          TableName: purchasesTable,
          Item: {
            token_hash: th,
            uid,
            sku: req.sku,
            order_id: p.orderId,
            ts: p.purchaseTimeMs > 0 ? p.purchaseTimeMs : nowMs,
            verified_at: nowMs,
            country: p.regionCode,
            quantity: p.quantity,
            purchase_type: p.purchaseType,
            state: 'ok',
          },
          ConditionExpression: 'attribute_not_exists(token_hash)',
        }),
      );
    } catch (e) {
      if (!isConditionalCheckFailed(e)) throw e;
      // Otra petición con el mismo token ganó la carrera: se responde como si hubiera llegado después.
      const winner = await readLedger();
      if (!winner) throw e;
      return answerExisting(winner);
    }

    log.info('purchase_ok', { ...fields, test: p.purchaseType === 0, quantity: p.quantity });
    return json(200, {
      status: 'ok',
      token_hash: th,
      grant: grantFor(product, p.quantity),
      ...(product.entitlements ? { entitlements: product.entitlements } : {}),
    });
  });
}

const deps = lazy(
  (): VerifyPurchaseDeps => ({
    ddb: realDdb(),
    play: realPlayApi(process.env),
    integrity: realIntegrityVerifier(process.env),
    env: process.env,
    now: Date.now,
  }),
);

export const handler = createHandler(deps);
