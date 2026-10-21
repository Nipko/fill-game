// Validación de las peticiones en las Lambdas con los patrones del contrato v1 (common.schema.json es la
// fuente única; Q-AWS-12). Sin ajv en tiempo de ejecución: validadores pequeños que los tests de contrato
// contrastan con los ejemplos válidos e inválidos de contracts/v1/examples.
import common from '../../contracts/v1/common.schema.json' with { type: 'json' };
import { badRequest } from './errors.ts';

type Defs = typeof common.$defs;
const pattern = (name: keyof Defs): RegExp => new RegExp((common.$defs[name] as { pattern: string }).pattern);

export const PATTERNS = {
  sku: pattern('sku'),
  purchaseToken: pattern('purchaseToken'),
  nonce: pattern('nonce'),
  firebaseUid: pattern('firebaseUid'),
  placement: pattern('placement'),
  accountCode: pattern('accountCode'),
  requestToken: pattern('requestToken'),
} as const;

const PURCHASE_TOKEN_MAX = common.$defs.purchaseToken.maxLength;

/** Solo admite las claves de `allowed` (additionalProperties: false del contrato). */
export function onlyKeys(body: Record<string, unknown>, allowed: readonly string[]): void {
  for (const k of Object.keys(body)) if (!allowed.includes(k)) throw badRequest(`Campo no admitido: ${k.slice(0, 40)}.`);
}

function matches(value: unknown, re: RegExp, maxLength = 4096): value is string {
  return typeof value === 'string' && value.length <= maxLength && re.test(value);
}

export interface VerifyPurchaseRequest {
  readonly token: string;
  readonly sku: string;
  readonly obfuscatedAccountId?: string;
}

export function parseVerifyPurchase(body: Record<string, unknown>): VerifyPurchaseRequest {
  onlyKeys(body, ['token', 'sku', 'obfuscated_account_id']);
  if (!matches(body.token, PATTERNS.purchaseToken, PURCHASE_TOKEN_MAX) || body.token.length < 1) throw badRequest('token no válido.');
  if (!matches(body.sku, PATTERNS.sku)) throw badRequest('sku no válido.');
  const oa = body.obfuscated_account_id;
  if (oa !== undefined && (typeof oa !== 'string' || oa.length < 1 || oa.length > 64)) {
    throw badRequest('obfuscated_account_id no válido.');
  }
  return { token: body.token, sku: body.sku, obfuscatedAccountId: oa };
}

export function parseRestoreLedger(body: Record<string, unknown>): { pendingNonces: string[] } {
  onlyKeys(body, ['pending_nonces']);
  const list = body.pending_nonces;
  if (!Array.isArray(list) || list.length > 200) throw badRequest('pending_nonces no válido.');
  const seen = new Set<string>();
  for (const n of list) {
    if (!matches(n, PATTERNS.nonce)) throw badRequest('pending_nonces contiene un nonce no válido.');
    if (seen.has(n)) throw badRequest('pending_nonces tiene repetidos.');
    seen.add(n);
  }
  return { pendingNonces: [...seen] };
}

export function parseAccountDelete(body: Record<string, unknown>): void {
  onlyKeys(body, ['confirm']);
  if (body.confirm !== true) throw badRequest('Falta confirm: true.');
}

export function parseDeleteRequest(body: Record<string, unknown>): { code: string } {
  onlyKeys(body, ['code']);
  if (!matches(body.code, PATTERNS.accountCode)) throw badRequest('code no válido.');
  return { code: body.code };
}

export function parseDeleteConfirm(body: Record<string, unknown>): { requestToken: string } {
  onlyKeys(body, ['request_token']);
  if (!matches(body.request_token, PATTERNS.requestToken)) throw badRequest('request_token no válido.');
  return { requestToken: body.request_token };
}

export interface SsvParams {
  readonly uid: string;
  readonly placement: string;
  readonly nonce: string;
  readonly network?: string;
  /** Parámetros de la firma de MAX ([NV]; rewarded-ssv/maxSignature.ts). */
  readonly eventId?: string;
  readonly eventToken?: string;
}

/** Parámetros de la query del callback de MAX (rewarded-ssv.request.schema.json). */
export function parseSsvQuery(q: Record<string, string | undefined>): SsvParams {
  for (const [k, v] of Object.entries(q)) {
    if (v !== undefined && v.length > 2048) throw badRequest(`Parámetro demasiado largo: ${k.slice(0, 40)}.`);
  }
  if (!matches(q.user_id, PATTERNS.firebaseUid)) throw badRequest('user_id no válido.');
  const cd = q.custom_data ?? '';
  const sep = cd.indexOf('|');
  const placement = sep >= 0 ? cd.slice(0, sep) : '';
  const nonce = sep >= 0 ? cd.slice(sep + 1) : '';
  if (!PATTERNS.placement.test(placement) || !PATTERNS.nonce.test(nonce)) throw badRequest('custom_data no válido.');
  if (q.network !== undefined && q.network.length > 64) throw badRequest('network no válido.');
  return { uid: q.user_id, placement, nonce, network: q.network, eventId: q.event_id, eventToken: q.event_token };
}
