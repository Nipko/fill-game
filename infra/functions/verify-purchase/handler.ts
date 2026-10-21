// verifyPurchase (08 §10.3; plan F2-4): verifica la compra en Google Play, la reconoce y la anota en el libro
// mayor af-<env>-purchases. Exige el JWT de Firebase (autorizador de API Gateway) y la integridad (Q-AWS-4).
//
// Orden pensado para que el dinero no se conceda dos veces ni se pierda:
//   1. validar el cuerpo y el SKU; 2. integridad; 3. si el token ya está en el libro mayor → duplicate (mismo
//   UID) o 409 (otro UID o anonimizado), sin llamar a Play; 4. products.get: pendiente → pending (no concede),
//   cancelada o inexistente → 400, comprada a nombre de otra cuenta → 409; 5. acknowledge si falta (antes de
//   escribir: si después falla la escritura, el reintento ve la compra ya reconocida y la escribe); 6. una
//   transacción (TransactWriteItems) con el Put condicional del libro mayor (attribute_not_exists: solo una
//   petición gana; la que pierde responde duplicate o 409), la comprobación de que el UID no está marcado como
//   borrado (F2c: un borrado a la vez no deja una fila con el uid de la cuenta borrada) y, en los SKU
//   `once_per_account`, la marca onceKey (F2c).
// F2c: un token con lápida `voided` (anulado en Play antes de verificarse; voidedPurchasesJob) → 400 sin llamar a
// Play. Un `af_starter` con otro token cuando la cuenta ya tiene uno → 400 sin reconocer (Play lo reembolsa a
// los 3 días); el cliente no debe consumir la transacción (consumir también reconoce).
// Solo `ok` lleva `grant`. En `duplicate` (p. ej. se perdió la respuesta del `ok`), el cliente acredita solo si
// el token_hash no está en applied_purchases: la misma regla que con restoreLedger (08 §11), sin doble concesión.
import { GetCommand, TransactWriteCommand } from '@aws-sdk/lib-dynamodb';
import type { DynamoDBDocumentClient } from '@aws-sdk/lib-dynamodb';
import { CATALOG, grantFor, onceKey } from '../shared/catalog.ts';
import { sha256hex, tokenHash as hashToken } from '../shared/crypto.ts';
import { cancellationCodes } from '../shared/ddb.ts';
import type { Env } from '../shared/env.ts';
import { requireEnv } from '../shared/env.ts';
import { HttpError, badRequest } from '../shared/errors.ts';
import type { HttpEvent, HttpHandler } from '../shared/http.ts';
import { header, json, jsonBody, requireUid, withErrors } from '../shared/http.ts';
import type { IntegrityVerifier } from '../shared/integrity.ts';
import { INTEGRITY_HEADER, requireIntegrity } from '../shared/integrity.ts';
import { log, metric, uidHash } from '../shared/log.ts';
import type { PlayApi, ProductPurchase } from '../shared/playApi.ts';
import { enforce, isDeleted, notDeletedCheck } from '../shared/rateLimit.ts';
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
  /** Lápida de voidedPurchasesJob: anulada en Play antes de pasar por aquí (sin uid). */
  readonly tombstone?: boolean;
}

/** Tope de verificaciones por UID y hora [P]: protege la cuota de la Play Developer API de tokens inventados. */
export const VERIFY_HOURLY_LIMIT = 30;

const otherUser = (): HttpError =>
  new HttpError(409, 'token_belongs_to_other_user', 'La compra está anotada a nombre de otra cuenta.');
const accountDeleted = (): HttpError => new HttpError(401, 'unauthorized', 'La cuenta se ha borrado.');
/** Lápida `voided`: igual que una compra cancelada en Play (contrato v1: 400 bad_request). */
const voidedTombstone = (): HttpError => badRequest('La compra está anulada en Google Play.');
/** Segunda compra de un SKU `once_per_account` (contrato v1: 400 bad_request; no concede ni reconoce). */
const alreadyOwned = (): HttpError => badRequest('Este producto solo se puede comprar una vez por cuenta.');

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
    const rateLimitsTable = requireEnv(deps.env, 'AF_TABLE_RATE_LIMITS');
    // El ID token de una cuenta recién borrada sigue valiendo hasta 1 h: no se anota nada a su nombre.
    if (await isDeleted(deps.ddb, rateLimitsTable, uidHash(uid), deps.now())) throw accountDeleted();
    await enforce(deps.ddb, rateLimitsTable, `verify#${uidHash(uid)}`, VERIFY_HOURLY_LIMIT, 3600, deps.now());
    await requireIntegrity(
      {
        ddb: deps.ddb,
        verifier: deps.integrity,
        integrityTable: requireEnv(deps.env, 'AF_TABLE_INTEGRITY'),
        rateLimitsTable,
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
      if (item.tombstone === true) {
        log.warn('purchase_voided_tombstone', fields);
        metric('PurchaseVoidedBeforeVerify', deps.env.AF_ENV ?? 'dev');
        throw voidedTombstone();
      }
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
    if (p.obfuscatedAccountId === undefined) {
      // 08 §10.1 pide setObfuscatedAccountId = UID; sin él no se puede atar la compra a la cuenta [P: exigirlo].
      log.warn('purchase_without_obfuscated_account', fields);
      metric('PurchaseWithoutObfuscatedAccount', deps.env.AF_ENV ?? 'dev');
    }
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
    const once = product.kind === 'once_per_account' ? onceKey(req.sku, uid) : undefined;
    if (once) {
      // Antes de reconocer: si no se reconoce, Play reembolsa la segunda compra sola a los 3 días.
      const mark = (await deps.ddb.send(new GetCommand({ TableName: rateLimitsTable, Key: { key: once }, ConsistentRead: true })))
        .Item as { token_hash?: string } | undefined;
      if (mark && mark.token_hash !== th) {
        log.warn('purchase_once_repeat', fields);
        metric('OncePerAccountRepeat', deps.env.AF_ENV ?? 'dev');
        throw alreadyOwned();
      }
    }
    if (!p.acknowledged) await deps.play.acknowledge(req.sku, req.token);

    const nowMs = deps.now();
    try {
      await deps.ddb.send(
        new TransactWriteCommand({
          TransactItems: [
            {
              Put: {
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
              },
            },
            notDeletedCheck(rateLimitsTable, uidHash(uid), nowMs),
            ...(once
              ? [
                  {
                    Put: {
                      TableName: rateLimitsTable,
                      Item: { key: once, token_hash: th, created_at: nowMs },
                      ConditionExpression: 'attribute_not_exists(#k)',
                      ExpressionAttributeNames: { '#k': 'key' },
                    },
                  },
                ]
              : []),
          ],
        }),
      );
    } catch (e) {
      const codes = cancellationCodes(e);
      if (!codes) throw e;
      // 1) La cuenta se borró mientras tanto: no queda nada a su nombre.
      if (codes[1] === 'ConditionalCheckFailed') {
        log.warn('purchase_account_deleted', fields);
        throw accountDeleted();
      }
      // 2) Otra petición con el mismo token ganó (o la lápida del job llegó antes): se responde como si esta
      //    hubiera llegado después. Con TransactionConflict, igual si el ganador ya está escrito.
      const winner = await readLedger();
      if (winner) return answerExisting(winner);
      // 3) Otro token del mismo SKU once_per_account ganó la carrera para este UID.
      if (once && codes[2] === 'ConditionalCheckFailed') {
        log.warn('purchase_once_repeat', { ...fields, race: true });
        metric('OncePerAccountRepeat', deps.env.AF_ENV ?? 'dev');
        throw alreadyOwned();
      }
      throw e;
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
