// Compras del App Store (StoreKit 2; mac-ios.md, fase M3): verificación **sin red ni credenciales** del JWS firmado de
// la transacción (`Transaction.jwsRepresentation`), como hace la app-store-server-library de Apple sin `onlineChecks`:
//   1. cabecera: alg ES256 y x5c con 3 certificados en base64 DER (hoja, intermedio, raíz);
//   2. cadena: hoja ← intermedio ← raíz, cada uno emitido y firmado por el siguiente, vigentes en now(), el intermedio
//      y la raíz con basicConstraints CA y la hoja sin ella; la raíz, EXACTAMENTE Apple Root CA - G3 (igualdad DER
//      con el PEM embebido, cuya huella SHA-256 se comprueba al cargar el módulo);
//   3. las extensiones de Apple: 1.2.840.113635.100.6.11.1 en la hoja y 1.2.840.113635.100.6.2.1 en el intermedio
//      (X509Certificate no expone extensiones arbitrarias: se leen del DER con un lector mínimo, `extensionOids`);
//   4. la firma del JWS con la clave pública de la hoja (jose, solo ES256);
//   5. el payload: bundleId en la lista del entorno, productId = sku, transactionId = token, sin revocationDate y
//      environment Production o Sandbox (Xcode nunca: lo firma Xcode, no Apple).
// Limitación: sin red no se consulta OCSP (revocación de los certificados de Apple). La librería de Apple lo hace solo
// con `enableOnlineChecks`; un certificado de firma de Apple revocado es un caso extremo [P].
import { X509Certificate } from 'node:crypto';
import type { KeyObject } from 'node:crypto';
import { compactVerify, decodeProtectedHeader } from 'jose';
import type { ProductPurchase } from './playApi.ts';

/** Apple Root CA - G3 (ECC P-384, válida de 2014-04-30 a 2039-04-30), de https://www.apple.com/certificateauthority/. */
export const APPLE_ROOT_CA_G3_PEM = `-----BEGIN CERTIFICATE-----
MIICQzCCAcmgAwIBAgIILcX8iNLFS5UwCgYIKoZIzj0EAwMwZzEbMBkGA1UEAwwS
QXBwbGUgUm9vdCBDQSAtIEczMSYwJAYDVQQLDB1BcHBsZSBDZXJ0aWZpY2F0aW9u
IEF1dGhvcml0eTETMBEGA1UECgwKQXBwbGUgSW5jLjELMAkGA1UEBhMCVVMwHhcN
MTQwNDMwMTgxOTA2WhcNMzkwNDMwMTgxOTA2WjBnMRswGQYDVQQDDBJBcHBsZSBS
b290IENBIC0gRzMxJjAkBgNVBAsMHUFwcGxlIENlcnRpZmljYXRpb24gQXV0aG9y
aXR5MRMwEQYDVQQKDApBcHBsZSBJbmMuMQswCQYDVQQGEwJVUzB2MBAGByqGSM49
AgEGBSuBBAAiA2IABJjpLz1AcqTtkyJygRMc3RCV8cWjTnHcFBbZDuWmBSp3ZHtf
TjjTuxxEtX/1H7YyYl3J6YRbTzBPEVoA/VhYDKX1DyxNB0cTddqXl5dvMVztK517
IDvYuVTZXpmkOlEKMaNCMEAwHQYDVR0OBBYEFLuw3qFYM4iapIqZ3r6966/ayySr
MA8GA1UdEwEB/wQFMAMBAf8wDgYDVR0PAQH/BAQDAgEGMAoGCCqGSM49BAMDA2gA
MGUCMQCD6cHEFl4aXTQY2e3v9GwOAEZLuN+yRhHFD/3meoyhpmvOwgPUnPWTxnS4
at+qIxUCMG1mihDK1A3UT82NQz60imOlM27jbdoXt2QfyFMm+YhidDkLF1vLUagM
6BgD56KyKA==
-----END CERTIFICATE-----`;

/** Huella SHA-256 publicada por Apple para Apple Root CA - G3 (hex, mayúsculas, con dos puntos, como `fingerprint256`). */
export const APPLE_ROOT_CA_G3_SHA256 =
  '63:34:3A:BF:B8:9A:6A:03:EB:B5:7E:9B:3F:5F:A7:BE:7C:4F:5C:75:6F:30:17:B3:A8:C4:88:C3:65:3E:91:79';

/** Extensión de Apple en el certificado de firma de las transacciones (hoja). */
export const OID_APPLE_LEAF = '1.2.840.113635.100.6.11.1';
/** Extensión de Apple en el intermedio (Apple Worldwide Developer Relations). */
export const OID_APPLE_INTERMEDIATE = '1.2.840.113635.100.6.2.1';

/** Carga la raíz de Apple y comprueba su huella: si el PEM embebido se corrompe, falla al arrancar, nunca en silencio. */
export function appleRootCaG3(): X509Certificate {
  const root = new X509Certificate(APPLE_ROOT_CA_G3_PEM);
  if (root.fingerprint256 !== APPLE_ROOT_CA_G3_SHA256 || !root.verify(root.publicKey)) {
    throw new Error('El PEM embebido de Apple Root CA - G3 no coincide con su huella SHA-256.');
  }
  return root;
}

/** Lo que el servidor usa de la transacción firmada (JWSTransactionDecodedPayload de Apple). */
export interface AppStoreTransaction {
  readonly transactionId: string;
  readonly originalTransactionId?: string;
  readonly bundleId: string;
  readonly productId: string;
  readonly purchaseDateMs: number;
  readonly quantity: number;
  readonly environment: string;
  /** Storefront ISO 3166-1 alfa-3 (p. ej. COL). */
  readonly storefront?: string;
  readonly revocationDateMs?: number;
}

export type AppStoreCheck =
  | { readonly ok: true; readonly purchase: ProductPurchase; readonly transaction: AppStoreTransaction }
  /** `reason` es para el log (corto, sin el JWS ni ids enteros); al cliente solo le llega un 400 genérico. */
  | { readonly ok: false; readonly reason: string };

/** Verificador de las compras del App Store, inyectable como PlayApi (los tests usan una raíz de prueba). */
export interface AppStoreVerifier {
  verify(signedTransaction: string, expected: { readonly sku: string; readonly transactionId: string }): Promise<AppStoreCheck>;
}

export interface JwsAppStoreVerifierOptions {
  /** bundleId admitidos (AF_IOS_BUNDLE_IDS). Vacía = ninguno: falla cerrada. */
  readonly bundleIds: readonly string[];
  /** Raíz de confianza (por defecto, Apple Root CA - G3). Solo los tests pasan otra. */
  readonly root?: X509Certificate;
  readonly now?: () => number;
}

// ---------- lector DER mínimo (solo lo necesario para leer los OID de las extensiones) ----------

interface Tlv {
  readonly tag: number;
  readonly start: number;
  readonly end: number;
}

function readTlv(buf: Buffer, off: number, limit: number): Tlv {
  if (off + 2 > limit) throw new Error('der');
  const tag = buf[off];
  let len = buf[off + 1];
  let p = off + 2;
  if (len & 0x80) {
    const n = len & 0x7f;
    if (n === 0 || n > 4 || p + n > limit) throw new Error('der');
    len = 0;
    for (let i = 0; i < n; i++) len = len * 256 + buf[p + i];
    p += n;
  }
  if (p + len > limit) throw new Error('der');
  return { tag, start: p, end: p + len };
}

function children(buf: Buffer, t: Tlv): Tlv[] {
  const out: Tlv[] = [];
  for (let p = t.start; p < t.end; ) {
    const c = readTlv(buf, p, t.end);
    out.push(c);
    p = c.end;
  }
  return out;
}

function decodeOid(buf: Buffer, t: Tlv): string {
  const parts: number[] = [];
  let v = 0;
  for (let i = t.start; i < t.end; i++) {
    v = v * 128 + (buf[i] & 0x7f);
    if ((buf[i] & 0x80) === 0) {
      if (parts.length === 0) parts.push(v < 80 ? Math.floor(v / 40) : 2, v < 80 ? v % 40 : v - 80);
      else parts.push(v);
      v = 0;
    }
  }
  return parts.join('.');
}

/** OID de las extensiones X.509 v3 de un certificado DER (TBSCertificate, [3] extensions). */
export function extensionOids(der: Buffer): string[] {
  const cert = readTlv(der, 0, der.length);
  const tbs = children(der, cert)[0];
  if (cert.tag !== 0x30 || tbs?.tag !== 0x30) throw new Error('der');
  const ext = children(der, tbs).find((c) => c.tag === 0xa3);
  if (!ext) return [];
  const seq = children(der, ext)[0];
  if (seq?.tag !== 0x30) throw new Error('der');
  return children(der, seq).map((e) => {
    const oid = children(der, e)[0];
    if (oid?.tag !== 0x06) throw new Error('der');
    return decodeOid(der, oid);
  });
}

// ---------- verificador ----------

const num = (v: unknown): number | undefined => (typeof v === 'number' && Number.isFinite(v) ? v : undefined);
const str = (v: unknown): string | undefined => (typeof v === 'string' && v !== '' ? v : undefined);
/** transactionId: en el JWS es texto; se admite número por si acaso, siempre como entero decimal. */
const idStr = (v: unknown): string | undefined =>
  typeof v === 'string' && /^\d{1,20}$/.test(v) ? v : typeof v === 'number' && Number.isSafeInteger(v) && v >= 0 ? String(v) : undefined;

const fail = (reason: string): AppStoreCheck => ({ ok: false, reason });

function withinValidity(c: X509Certificate, nowMs: number): boolean {
  const from = Date.parse(c.validFrom);
  const to = Date.parse(c.validTo);
  return Number.isFinite(from) && Number.isFinite(to) && from <= nowMs && nowMs <= to;
}

export class JwsAppStoreVerifier implements AppStoreVerifier {
  private readonly bundleIds: ReadonlySet<string>;
  private readonly root: X509Certificate;
  private readonly now: () => number;

  constructor(opts: JwsAppStoreVerifierOptions) {
    this.bundleIds = new Set(opts.bundleIds);
    this.root = opts.root ?? appleRootCaG3();
    this.now = opts.now ?? Date.now;
  }

  /** Cadena x5c → clave pública de la hoja, o el motivo del rechazo. */
  private leafKey(x5c: unknown, nowMs: number): KeyObject | string {
    if (!Array.isArray(x5c) || x5c.length !== 3 || !x5c.every((c) => typeof c === 'string' && /^[A-Za-z0-9+/]+={0,2}$/.test(c))) {
      return 'x5c';
    }
    let certs: X509Certificate[];
    try {
      certs = (x5c as string[]).map((c) => new X509Certificate(Buffer.from(c, 'base64')));
    } catch {
      return 'x5c_parse';
    }
    const [leaf, intermediate, root] = certs;
    if (!root.raw.equals(this.root.raw)) return 'root';
    if (!certs.every((c) => withinValidity(c, nowMs))) return 'validity';
    if (!intermediate.ca || leaf.ca) return 'basic_constraints';
    if (!intermediate.checkIssued(root) || !intermediate.verify(root.publicKey)) return 'intermediate_sig';
    if (!leaf.checkIssued(intermediate) || !leaf.verify(intermediate.publicKey)) return 'leaf_sig';
    try {
      if (!extensionOids(leaf.raw).includes(OID_APPLE_LEAF)) return 'leaf_oid';
      if (!extensionOids(intermediate.raw).includes(OID_APPLE_INTERMEDIATE)) return 'intermediate_oid';
    } catch {
      return 'der';
    }
    return leaf.publicKey;
  }

  async verify(signedTransaction: string, expected: { readonly sku: string; readonly transactionId: string }): Promise<AppStoreCheck> {
    const nowMs = this.now();
    let header: ReturnType<typeof decodeProtectedHeader>;
    try {
      header = decodeProtectedHeader(signedTransaction);
    } catch {
      return fail('header');
    }
    if (header.alg !== 'ES256') return fail('alg');
    const key = this.leafKey(header.x5c, nowMs);
    if (typeof key === 'string') return fail(key);

    let payload: Record<string, unknown>;
    try {
      const { payload: raw } = await compactVerify(signedTransaction, key, { algorithms: ['ES256'] });
      const parsed: unknown = JSON.parse(new TextDecoder().decode(raw));
      if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) return fail('payload');
      payload = parsed as Record<string, unknown>;
    } catch {
      return fail('signature');
    }

    const tx: AppStoreTransaction = {
      transactionId: idStr(payload.transactionId) ?? '',
      originalTransactionId: idStr(payload.originalTransactionId),
      bundleId: str(payload.bundleId) ?? '',
      productId: str(payload.productId) ?? '',
      purchaseDateMs: num(payload.purchaseDate) ?? 0,
      quantity: Math.max(1, Math.floor(num(payload.quantity) ?? 1)),
      environment: str(payload.environment) ?? '',
      storefront: str(payload.storefront),
      revocationDateMs: num(payload.revocationDate),
    };
    if (!this.bundleIds.has(tx.bundleId)) return fail('bundle');
    if (tx.productId !== expected.sku) return fail('product');
    if (tx.transactionId === '' || tx.transactionId !== expected.transactionId) return fail('transaction');
    if (tx.revocationDateMs !== undefined || payload.revocationReason !== undefined) return fail('revoked');
    // Xcode (StoreKit Testing local) lo firma Xcode, no Apple: nunca vale. LocalTesting, tampoco.
    if (tx.environment !== 'Production' && tx.environment !== 'Sandbox') return fail('environment');

    const purchase: ProductPurchase = {
      state: 'purchased',
      // StoreKit no tiene reconocimiento en servidor (finish() lo hace el cliente).
      acknowledged: true,
      orderId: tx.originalTransactionId ?? tx.transactionId,
      purchaseTimeMs: tx.purchaseDateMs,
      // El storefront de Apple es alfa-3 (COL); se guarda tal cual en `country` (Play guarda alfa-2).
      regionCode: tx.storefront,
      // Sin appAccountToken: no se ata a un UID (ver el handler, Q-IAP-4).
      obfuscatedAccountId: undefined,
      // Como las compras de prueba de Play (licencias de prueba): purchaseType 0.
      ...(tx.environment === 'Sandbox' ? { purchaseType: 0 } : {}),
      quantity: tx.quantity,
    };
    return { ok: true, purchase, transaction: tx };
  }
}
