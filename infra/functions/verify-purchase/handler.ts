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
// Q-IAP-4 (D-040, recomendada (a)): un **no consumible** que Play devuelve a otra instalación de la misma cuenta de
// Google con otro UID (reinstalar sin recuperar el UID) ya no se queda en 409: el servidor reasigna la fila del libro
// mayor al UID nuevo y responde `duplicate` (el cliente lo trata como restauración), o `ok` si ningún UID la había
// verificado. Reglas y límites en `reassignNonConsumable` y en docs/dev/iap.md §4.3.
import { GetCommand, TransactWriteCommand, UpdateCommand } from '@aws-sdk/lib-dynamodb';
import type { DynamoDBDocumentClient } from '@aws-sdk/lib-dynamodb';
import { CATALOG, grantFor, onceKey } from '../shared/catalog.ts';
import { sha256hex, tokenHash as hashToken } from '../shared/crypto.ts';
import { cancellationCodes, epochS, isConditionalCheckFailed } from '../shared/ddb.ts';
import type { Env } from '../shared/env.ts';
import { requireEnv } from '../shared/env.ts';
import { HttpError, badRequest, rateLimited } from '../shared/errors.ts';
import type { HttpEvent, HttpHandler } from '../shared/http.ts';
import { header, json, jsonBody, requireUid, withErrors } from '../shared/http.ts';
import type { IntegrityVerifier } from '../shared/integrity.ts';
import { INTEGRITY_HEADER, requireIntegrity } from '../shared/integrity.ts';
import { log, metric, uidHash } from '../shared/log.ts';
import type { PlayApi, ProductPurchase } from '../shared/playApi.ts';
import { enforce, isDeleted, notDeletedCheck, peekQuota, quotaTxUpdate, windowOf } from '../shared/rateLimit.ts';
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
  readonly sku?: string;
  readonly state?: string;
  /** Lápida de voidedPurchasesJob: anulada en Play antes de pasar por aquí (sin uid). */
  readonly tombstone?: boolean;
  /** Q-IAP-4: reasignaciones de cualquier tipo (cerrojo optimista de la escritura). */
  readonly reassign_seq?: number;
  /** Q-IAP-4: reasignaciones a un UID que no es el que compró (las que cuentan para el tope). */
  readonly reassign_count?: number;
  /** Q-IAP-4: epoch ms de la última reasignación que contó (para la espera entre dos). */
  readonly reassigned_at?: number;
  readonly ts?: number;
}

/**
 * Q-IAP-4: clave de la fila sombra que guarda, para el UID que tenía la compra antes de la reasignación `n`, que
 * la tuvo (`shadow_of` = token_hash de la principal). La lee restoreLedger y la marca voidedPurchasesJob.
 */
export function shadowKeyOf(tokenHashValue: string, n: number): string {
  return `${tokenHashValue}#prev#${n}`;
}

/** Entrada del historial de reasignaciones de una fila (sin UID ni hash: solo cuándo y de qué tipo). */
export interface ReassignLogEntry {
  readonly at: number;
  /**
   * `claim`: ningún UID la había verificado y la pide otro UID; `other`: de un UID a otro que no es el que compró;
   * `anonymized`: la fila estaba anonimizada (borrado de cuenta); `owner`: vuelve al UID que la compró.
   */
  readonly kind: 'claim' | 'other' | 'anonymized' | 'owner';
}

/** Tope de verificaciones por UID y hora [P]: protege la cuota de la Play Developer API de tokens inventados. */
export const VERIFY_HOURLY_LIMIT = 30;
/** Vida de la reserva de la marca `once_per_account` entre reservarla y escribir el libro mayor (F2c) [P]. */
export const ONCE_RESERVATION_S = 15 * 60;
/**
 * Q-IAP-4 [P]: veces que un mismo token de no consumible puede pasar a un UID que no es el que lo compró (el
 * `obfuscatedAccountId` de Play). Una reinstalación sin recuperar el UID gasta una; volver al UID que compró no gasta.
 */
export const REASSIGN_MAX_PER_TOKEN = 5;
/** Q-IAP-4 [P]: espera mínima entre dos reasignaciones que cuentan del mismo token. */
export const REASSIGN_COOLDOWN_S = 24 * 3600;
/**
 * Q-IAP-4 [P]: reasignaciones que cuentan por UID y día. 4 = los no consumibles del catálogo (af_noads y las tres
 * skins): una reinstalación legítima los recupera todos el mismo día; un UID que junta tokens ajenos, no.
 */
export const REASSIGN_DAILY_PER_UID = 4;

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

/** Como `obfuscatedMatches`, pero estricto: sin cuenta ofuscada no se sabe quién compró (Q-IAP-4). */
export function boughtBy(obfuscated: string | undefined, uid: string): boolean {
  return obfuscated !== undefined && (obfuscated === uid || obfuscated === sha256hex(uid));
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
    const envName = deps.env.AF_ENV ?? 'dev';

    const readLedger = async (): Promise<LedgerItem | undefined> =>
      (await deps.ddb.send(new GetCommand({ TableName: purchasesTable, Key: { token_hash: th }, ConsistentRead: true })))
        .Item as LedgerItem | undefined;
    const answerExisting = (item: LedgerItem): ReturnType<typeof json> => {
      if (item.tombstone === true) {
        log.warn('purchase_voided_tombstone', fields);
        metric('PurchaseVoidedBeforeVerify', envName);
        throw voidedTombstone();
      }
      if (item.uid === uid) {
        log.info('purchase_duplicate', fields);
        return json(200, { status: 'duplicate', token_hash: th });
      }
      log.warn('purchase_other_user', { ...fields, anonymized: item.uid === undefined });
      throw otherUser();
    };

    const quotaKey = `reassign#${uidHash(uid)}`;
    /**
     * Cupo por UID y día de las reasignaciones que cuentan (429 rate_limited, como el resto de límites). Se mira
     * antes (sin gastarlo) y se gasta dentro de la transacción que reasigna: si se cancela, no se gasta.
     */
    const checkReassignQuota = async (): Promise<void> => {
      const q = await peekQuota(deps.ddb, rateLimitsTable, quotaKey, REASSIGN_DAILY_PER_UID, 86_400, deps.now());
      if (!q.allowed) throw rateLimited(q.retryAfterS);
    };
    const quotaExhausted = (): HttpError => rateLimited(windowOf(quotaKey, 86_400, deps.now()).endS - epochS(deps.now()));

    /**
     * Q-IAP-4: la fila es de otro UID (o está anonimizada). Solo un no consumible, `ok` en el libro mayor, del mismo
     * SKU, que Play da como comprado con una cuenta ofuscada conocida, pasa al UID que la pide; todo lo demás sigue
     * en 409, como antes. Si Play dice que la compró este UID, vuelve sin gastar cupo; si no, gasta una de las
     * REASSIGN_MAX_PER_TOKEN, exige REASSIGN_COOLDOWN_S desde la anterior y una de las REASSIGN_DAILY_PER_UID.
     * Todo en una transacción:
     *   - la fila principal cambia de dueño (condicional: mismo dueño y mismo `reassign_seq` que se leyó, `ok`);
     *   - la comprobación de que el UID nuevo no está borrado;
     *   - el cupo diario del UID nuevo (solo si cuenta);
     *   - si había dueño, una **fila sombra** `<token_hash>#prev#<n>` con el UID anterior (`shadow_of` = token_hash)
     *     y la comprobación de que ese UID no se está borrando. El job de anuladas marca `voided` también las
     *     sombras, y restoreLedger se las da al UID anterior solo cuando están anuladas: así un reembolso posterior
     *     se retira también en el dispositivo viejo (si no, comprar en A, reasignar a B y reembolsar dejaría el
     *     derecho en A).
     * Dos peticiones a la vez → una reasigna y la otra ve el resultado. Responde `duplicate` (sin `grant`): la
     * compra ya se concedió una vez, al UID que la verificó primero.
     */
    const reassignNonConsumable = async (item: LedgerItem): Promise<ReturnType<typeof json>> => {
      const rf = { ...fields, anonymized: item.uid === undefined };
      if (product.kind !== 'non_consumable' || item.state !== 'ok' || item.sku !== req.sku) {
        log.warn('purchase_other_user', rf);
        throw otherUser();
      }
      const p = await deps.play.getProduct(req.sku, req.token);
      if (!p || p.state !== 'purchased' || p.obfuscatedAccountId === undefined) {
        log.warn('purchase_reassign_rejected', { ...rf, play_state: p?.state ?? 'unknown', obfuscated: p?.obfuscatedAccountId !== undefined });
        throw otherUser();
      }
      const owner = boughtBy(p.obfuscatedAccountId, uid);
      const count = item.reassign_count ?? 0;
      const nowMs = deps.now();
      if (!owner) {
        if (count >= REASSIGN_MAX_PER_TOKEN) {
          log.warn('purchase_reassign_limit', { ...rf, count });
          metric('PurchaseReassignLimit', envName);
          throw otherUser();
        }
        if (item.reassigned_at !== undefined && nowMs - item.reassigned_at < REASSIGN_COOLDOWN_S * 1000) {
          log.warn('purchase_reassign_cooldown', { ...rf, count });
          metric('PurchaseReassignLimit', envName);
          throw otherUser();
        }
        await checkReassignQuota();
      }
      const seq = item.reassign_seq ?? 0;
      const kind: ReassignLogEntry['kind'] = owner ? 'owner' : item.uid === undefined ? 'anonymized' : 'other';
      const entry: ReassignLogEntry = { at: nowMs, kind };
      const shadowKey = item.uid === undefined ? undefined : shadowKeyOf(th, seq + 1);
      const values: Record<string, unknown> = { ':new': uid, ':ok': 'ok', ':seq1': seq + 1, ':entry': [entry], ':empty': [] };
      const sets = ['#u = :new', 'reassign_seq = :seq1', 'reassign_log = list_append(if_not_exists(reassign_log, :empty), :entry)'];
      if (!owner) {
        // Volver al UID que compró no gasta cupo ni reinicia la espera.
        sets.push('reassign_count = :cnt', 'reassigned_at = :at');
        values[':cnt'] = count + 1;
        values[':at'] = nowMs;
      }
      if (shadowKey) {
        sets.push('shadow_keys = list_append(if_not_exists(shadow_keys, :empty), :shadow)');
        values[':shadow'] = [shadowKey];
      }
      const conditions = ['#s = :ok', 'attribute_not_exists(tombstone)'];
      if (item.uid === undefined) conditions.push('attribute_not_exists(#u)');
      else {
        conditions.push('#u = :old');
        values[':old'] = item.uid;
      }
      if (item.reassign_seq === undefined) conditions.push('attribute_not_exists(reassign_seq)');
      else {
        conditions.push('reassign_seq = :seq');
        values[':seq'] = seq;
      }
      const tx: NonNullable<ConstructorParameters<typeof TransactWriteCommand>[0]['TransactItems']> = [
        {
          Update: {
            TableName: purchasesTable,
            Key: { token_hash: th },
            // Se conservan order_id, ts, verified_at, country…: solo cambia el dueño y se apunta el historial.
            UpdateExpression: `SET ${sets.join(', ')}`,
            ConditionExpression: conditions.join(' AND '),
            ExpressionAttributeNames: { '#u': 'uid', '#s': 'state' },
            ExpressionAttributeValues: values,
          },
        },
        notDeletedCheck(rateLimitsTable, uidHash(uid), nowMs),
      ];
      const quotaIdx = owner ? -1 : tx.push(quotaTxUpdate(rateLimitsTable, quotaKey, REASSIGN_DAILY_PER_UID, 86_400, nowMs)) - 1;
      let oldDeletedIdx = -1;
      if (shadowKey && item.uid !== undefined) {
        tx.push({
          Put: {
            TableName: purchasesTable,
            Item: {
              token_hash: shadowKey,
              shadow_of: th,
              uid: item.uid,
              sku: item.sku,
              ts: item.ts,
              state: 'ok',
              reassigned_away_at: nowMs,
            },
            ConditionExpression: 'attribute_not_exists(token_hash)',
          },
        });
        // Sin sombra a nombre de un UID que se está borrando (su borrado ya no la vería).
        oldDeletedIdx = tx.push(notDeletedCheck(rateLimitsTable, uidHash(item.uid), nowMs)) - 1;
      }
      try {
        await deps.ddb.send(new TransactWriteCommand({ TransactItems: tx }));
      } catch (e) {
        const codes = cancellationCodes(e);
        if (!codes) throw e;
        if (codes[1] === 'ConditionalCheckFailed') {
          log.warn('purchase_account_deleted', fields);
          throw accountDeleted();
        }
        // Otra petición movió la fila entretanto (la misma del mismo UID, otra reasignación o el job de anuladas).
        const after = await readLedger();
        if (after?.uid === uid && after.state === 'ok') {
          log.info('purchase_duplicate', { ...fields, reassign_race: true });
          return json(200, { status: 'duplicate', token_hash: th });
        }
        if (quotaIdx >= 0 && codes[quotaIdx] === 'ConditionalCheckFailed') throw quotaExhausted();
        // El UID anterior se está borrando: cuando acabe, la fila quedará anonimizada y se podrá reasignar.
        log.warn(oldDeletedIdx >= 0 && codes[oldDeletedIdx] === 'ConditionalCheckFailed' ? 'purchase_reassign_old_deleting' : 'purchase_reassign_race_lost', rf);
        throw otherUser();
      }
      log.warn('purchase_reassigned', { ...rf, kind, count: owner ? count : count + 1 });
      metric('PurchaseReassigned', envName, { Kind: kind });
      return json(200, { status: 'duplicate', token_hash: th });
    };

    const existing = await readLedger();
    if (existing) {
      if (existing.tombstone !== true && existing.uid !== uid) return reassignNonConsumable(existing);
      return answerExisting(existing);
    }

    const p: ProductPurchase | null = await deps.play.getProduct(req.sku, req.token);
    if (!p) throw badRequest('Google Play no reconoce esa compra.');
    if (p.obfuscatedAccountId === undefined) {
      // 08 §10.1 pide setObfuscatedAccountId = UID; sin él no se puede atar la compra a la cuenta [P: exigirlo].
      log.warn('purchase_without_obfuscated_account', fields);
      metric('PurchaseWithoutObfuscatedAccount', envName);
    }
    // Q-IAP-4: un no consumible comprado con otro UID que nadie verificó (p. ej. se compró sin red y se reinstaló
    // antes de verificarlo) lo puede reclamar este UID, con los mismos límites que una reasignación (la primera).
    let claim = false;
    if (!obfuscatedMatches(p.obfuscatedAccountId, uid)) {
      if (product.kind !== 'non_consumable' || p.state !== 'purchased') {
        log.warn('purchase_obfuscated_mismatch', fields);
        throw otherUser();
      }
      claim = true;
      await checkReassignQuota();
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
      // Reserva atómica ANTES de reconocer (F2c): si otro token del mismo UID ya tiene la marca (o una reserva
      // viva), 400 sin reconocer y Play reembolsa esta compra sola a los 3 días. La reserva caduca a los
      // ONCE_RESERVATION_S para que un acknowledge o una escritura fallidos no bloqueen la cuenta para siempre;
      // la transacción del libro mayor la convierte en marca permanente (quita el ttl).
      try {
        await deps.ddb.send(
          new UpdateCommand({
            TableName: rateLimitsTable,
            Key: { key: once },
            UpdateExpression: 'SET token_hash = :th, #t = :ttl',
            ConditionExpression: 'attribute_not_exists(#k) OR token_hash = :th OR (attribute_exists(#t) AND #t <= :now)',
            ExpressionAttributeNames: { '#k': 'key', '#t': 'ttl' },
            ExpressionAttributeValues: { ':th': th, ':ttl': epochS(deps.now()) + ONCE_RESERVATION_S, ':now': epochS(deps.now()) },
          }),
        );
      } catch (e) {
        if (!isConditionalCheckFailed(e)) throw e;
        log.warn('purchase_once_repeat', fields);
        metric('OncePerAccountRepeat', envName);
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
                  ...(claim
                    ? {
                        reassign_seq: 1,
                        reassign_count: 1,
                        reassigned_at: nowMs,
                        reassign_log: [{ at: nowMs, kind: 'claim' } satisfies ReassignLogEntry],
                      }
                    : {}),
                },
                ConditionExpression: 'attribute_not_exists(token_hash)',
              },
            },
            notDeletedCheck(rateLimitsTable, uidHash(uid), nowMs),
            ...(once
              ? [
                  {
                    // La reserva de este token pasa a marca permanente (sin ttl). Si ya no es suya (caducó y otro
                    // token la tomó), la transacción se cancela: ver el caso 3 de abajo (riesgo residual).
                    Update: {
                      TableName: rateLimitsTable,
                      Key: { key: once },
                      UpdateExpression: 'REMOVE #t SET created_at = :now',
                      ConditionExpression: 'token_hash = :th',
                      ExpressionAttributeNames: { '#t': 'ttl' },
                      ExpressionAttributeValues: { ':th': th, ':now': nowMs },
                    },
                  },
                ]
              : []),
            // Q-IAP-4: reclamar una compra que nadie verificó gasta el cupo diario de reasignaciones (índice 2: un
            // no consumible nunca lleva la marca `once`).
            ...(claim ? [quotaTxUpdate(rateLimitsTable, quotaKey, REASSIGN_DAILY_PER_UID, 86_400, nowMs)] : []),
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
      // 3) La reserva de este token caducó (más de ONCE_RESERVATION_S entre reservar y escribir) y otro token
      //    del mismo UID la tomó: esta compra ya está reconocida y no se concede. Riesgo residual documentado.
      // 4) Q-IAP-4: el cupo diario se agotó entre mirarlo y escribir (otra reasignación a la vez). La compra ya
      //    está reconocida y nadie la tiene: el reintento la reclama cuando haya cupo.
      if (claim && codes[2] === 'ConditionalCheckFailed') throw quotaExhausted();
      if (once && codes[2] === 'ConditionalCheckFailed') {
        log.warn('purchase_once_repeat', { ...fields, race: true });
        metric('OncePerAccountRepeat', envName);
        throw alreadyOwned();
      }
      throw e;
    }

    if (claim) {
      log.warn('purchase_reassigned', { ...fields, kind: 'claim', count: 1 });
      metric('PurchaseReassigned', envName, { Kind: 'claim' });
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
