// Hashes y valores aleatorios del backend (plan §2; Q-B13-21). Sin dependencias: node:crypto.
import { createHash, randomBytes, timingSafeEqual } from 'node:crypto';

/** SHA-256 en hexadecimal en minúsculas. */
export function sha256hex(value: string): string {
  return createHash('sha256').update(value, 'utf8').digest('hex');
}

/** PK del libro mayor: SHA-256 del purchaseToken (plan §2; common.schema.json#/$defs/tokenHash). */
export function tokenHash(purchaseToken: string): string {
  return sha256hex(purchaseToken);
}

/** Alfabeto Base32 de Crockford (sin I, L, O ni U). */
export const CROCKFORD = '0123456789ABCDEFGHJKMNPQRSTVWXYZ';

/**
 * Código de borrado por web (Q-B13-21): 128 bits aleatorios en Base32 de Crockford, 26 caracteres; el primero
 * va de 0 a 7 (26 × 5 = 130 bits, los dos de arriba a cero). Cumple common.schema.json#/$defs/accountCode.
 */
export function newAccountCode(random: (n: number) => Buffer = randomBytes): string {
  const bytes = random(16);
  if (bytes.length !== 16) throw new Error('Se esperaban 16 bytes aleatorios.');
  let n = BigInt(`0x${bytes.toString('hex')}`);
  let out = '';
  for (let i = 0; i < 26; i++) {
    out = CROCKFORD[Number(n & 31n)] + out;
    n >>= 5n;
  }
  return out;
}

/** Hash con el que se guarda un código de borrado (el servidor nunca guarda el código). */
export function accountCodeHash(code: string): string {
  return sha256hex(`af-code:${code}`);
}

/** Token de confirmación del borrado por web: 256 bits en base64url sin relleno (43 caracteres). */
export function newRequestToken(random: (n: number) => Buffer = randomBytes): string {
  return random(32).toString('base64url');
}

/** Compara dos cadenas en tiempo constante (longitudes distintas → false). */
export function safeEqual(a: string, b: string): boolean {
  const ba = Buffer.from(a, 'utf8');
  const bb = Buffer.from(b, 'utf8');
  if (ba.length !== bb.length) return false;
  return timingSafeEqual(ba, bb);
}
