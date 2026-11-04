// Cadena de certificados de prueba (raíz → intermedio → hoja, ECDSA P-256) y JWS de StoreKit 2 firmados con ella.
// node:crypto no crea certificados: se arman en DER con un codificador mínimo. Nunca son de Apple: el verificador real
// solo confía en Apple Root CA - G3 y los tests le inyectan esta raíz.
import { CompactSign } from 'jose';
import { X509Certificate, generateKeyPairSync, sign } from 'node:crypto';
import type { KeyObject } from 'node:crypto';
import { OID_APPLE_INTERMEDIATE, OID_APPLE_LEAF } from '../../functions/shared/appStore.ts';
import { NOW } from './fixtures.ts';

// ---------- DER ----------

function tlv(tag: number, content: Buffer): Buffer {
  const n = content.length;
  const len = n < 0x80 ? Buffer.from([n]) : n < 0x100 ? Buffer.from([0x81, n]) : Buffer.from([0x82, n >> 8, n & 0xff]);
  return Buffer.concat([Buffer.from([tag]), len, content]);
}
const seq = (...items: Buffer[]): Buffer => tlv(0x30, Buffer.concat(items));
const set = (...items: Buffer[]): Buffer => tlv(0x31, Buffer.concat(items));
const ctx = (n: number, content: Buffer): Buffer => tlv(0xa0 | n, content);
const int = (v: number): Buffer => tlv(0x02, Buffer.from([v & 0x7f]));
const bool = (v: boolean): Buffer => tlv(0x01, Buffer.from([v ? 0xff : 0x00]));
const octet = (b: Buffer): Buffer => tlv(0x04, b);
const bits = (b: Buffer): Buffer => tlv(0x03, Buffer.concat([Buffer.from([0]), b]));
const utf8 = (s: string): Buffer => tlv(0x0c, Buffer.from(s, 'utf8'));

function oid(dotted: string): Buffer {
  const [a, b, ...rest] = dotted.split('.').map(Number);
  const out = [40 * a + b];
  for (const v of rest) {
    const chunk = [v & 0x7f];
    for (let x = Math.floor(v / 128); x > 0; x = Math.floor(x / 128)) chunk.unshift((x & 0x7f) | 0x80);
    out.push(...chunk);
  }
  return tlv(0x06, Buffer.from(out));
}

function utcTime(ms: number): Buffer {
  const d = new Date(ms);
  const p = (n: number) => String(n).padStart(2, '0');
  const s = `${p(d.getUTCFullYear() % 100)}${p(d.getUTCMonth() + 1)}${p(d.getUTCDate())}${p(d.getUTCHours())}${p(d.getUTCMinutes())}${p(d.getUTCSeconds())}Z`;
  return tlv(0x17, Buffer.from(s, 'ascii'));
}

const ECDSA_SHA256 = seq(oid('1.2.840.10045.4.3.2'));
const name = (cn: string): Buffer => seq(set(seq(oid('2.5.4.3'), utf8(cn))));
const ext = (id: string, value: Buffer, critical = false): Buffer => seq(oid(id), ...(critical ? [bool(true)] : []), octet(value));
const basicConstraints = (ca: boolean): Buffer => ext('2.5.29.19', ca ? seq(bool(true)) : seq(), true);
/** Extensión de marca de Apple (valor: NULL, como en los certificados reales). */
const marker = (id: string): Buffer => ext(id, Buffer.from([0x05, 0x00]));

export interface TestCert {
  readonly der: Buffer;
  readonly x509: X509Certificate;
  readonly key: KeyObject;
  readonly subject: string;
}

export interface CertOptions {
  readonly subject: string;
  readonly issuer?: TestCert;
  readonly ca: boolean;
  readonly markers?: readonly string[];
  readonly notBefore?: number;
  readonly notAfter?: number;
  readonly serial?: number;
}

/** Certificado X.509 v3 firmado por `issuer` (o autofirmado). */
export function makeCert(o: CertOptions): TestCert {
  const { publicKey, privateKey } = generateKeyPairSync('ec', { namedCurve: 'P-256' });
  const spki = publicKey.export({ type: 'spki', format: 'der' });
  const tbs = seq(
    ctx(0, int(2)),
    int(o.serial ?? 1),
    ECDSA_SHA256,
    name(o.issuer?.subject ?? o.subject),
    seq(utcTime(o.notBefore ?? NOW - 365 * 86_400_000), utcTime(o.notAfter ?? NOW + 365 * 86_400_000)),
    name(o.subject),
    spki,
    ctx(3, seq(basicConstraints(o.ca), ...(o.markers ?? []).map(marker))),
  );
  const signature = sign('sha256', tbs, o.issuer?.key ?? privateKey);
  const der = seq(tbs, ECDSA_SHA256, bits(signature));
  return { der, x509: new X509Certificate(der), key: privateKey, subject: o.subject };
}

export interface TestChain {
  readonly root: TestCert;
  readonly intermediate: TestCert;
  readonly leaf: TestCert;
}

export function makeChain(): TestChain {
  const root = makeCert({ subject: 'Test Root CA', ca: true });
  const intermediate = makeCert({ subject: 'Test WWDR', issuer: root, ca: true, markers: [OID_APPLE_INTERMEDIATE] });
  const leaf = makeCert({ subject: 'Test StoreKit Signing', issuer: intermediate, ca: false, markers: [OID_APPLE_LEAF] });
  return { root, intermediate, leaf };
}

export const BUNDLE_ID = 'com.amazonfill.dev';
export const TX_ID = '2000000123456789';

/** Payload de una transacción de StoreKit 2 (JWSTransactionDecodedPayload) con valores por defecto válidos. */
export function txPayload(over: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    transactionId: TX_ID,
    originalTransactionId: '2000000000000001',
    bundleId: BUNDLE_ID,
    productId: 'af_coins_2',
    purchaseDate: NOW - 1000,
    originalPurchaseDate: NOW - 1000,
    quantity: 1,
    type: 'Consumable',
    inAppOwnershipType: 'PURCHASED',
    signedDate: NOW - 500,
    environment: 'Production',
    storefront: 'COL',
    storefrontId: '143501',
    transactionReason: 'PURCHASE',
    ...over,
  };
}

/** JWS compacto ES256 con x5c = [hoja, intermedio, raíz] (o la cadena y la clave que se pidan). */
export async function signTx(
  payload: Record<string, unknown>,
  chain: TestChain,
  opts: { certs?: readonly TestCert[]; key?: KeyObject; alg?: string } = {},
): Promise<string> {
  const certs = opts.certs ?? [chain.leaf, chain.intermediate, chain.root];
  return new CompactSign(new TextEncoder().encode(JSON.stringify(payload)))
    .setProtectedHeader({ alg: opts.alg ?? 'ES256', x5c: certs.map((c) => c.der.toString('base64')) })
    .sign(opts.key ?? chain.leaf.key);
}
