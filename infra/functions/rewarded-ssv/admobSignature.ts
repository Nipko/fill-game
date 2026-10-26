// Firma de los callbacks de verificación en el servidor (SSV) de los anuncios recompensados de AdMob (D-049; 08 §10.1-§10.2),
// aislada del handler.
//
// AdMob llama con GET a la URL configurada en el bloque recompensado, con `ad_network`, `ad_unit`, `custom_data` (si el
// cliente la fijó), `reward_amount`, `reward_item`, `timestamp` (epoch en ms), `transaction_id`, `user_id` (si el cliente lo
// fijó) y, siempre al final y en este orden, `signature` y `key_id`. La firma es ECDSA P-256 sobre SHA-256 de la cadena de
// consulta **tal cual llega** hasta antes de `&signature=` (sin reordenar ni decodificar), en DER y base64url. La clave
// pública sale de https://www.gstatic.com/admob/reward/verifier-keys.json por `key_id`; Google las rota, así que se
// descargan en ejecución y no se usan más de 24 h sin volver a pedirlas (research/06 C6.1 [V]).
import { createPublicKey, verify } from 'node:crypto';
import type { KeyObject } from 'node:crypto';
import { UpstreamError } from '../shared/errors.ts';
import type { FetchFn } from '../shared/googleAuth.ts';

/** Claves públicas de AdMob para el SSV. */
export const ADMOB_VERIFIER_KEYS_URL = 'https://www.gstatic.com/admob/reward/verifier-keys.json';
/** Vida de la caché de claves: 24 h como máximo (research/06 C6.1 [V]). */
export const KEYS_TTL_MS = 24 * 3600_000;
/**
 * Espera mínima entre dos descargas [P]: un `key_id` desconocido refresca la caché una vez, pero no más de una vez por minuto
 * (un tercero con `key_id` inventados no convierte cada petición en una descarga).
 */
export const KEYS_MIN_REFRESH_MS = 60_000;
const FETCH_TIMEOUT_MS = 3000;
const MAX_KEYS_BYTES = 65_536;

/** La parte firmada de la consulta, la firma (DER) y el `key_id`. */
export interface SignedQuery {
  readonly message: string;
  readonly signature: Buffer;
  readonly keyId: string;
}

/** Lo que va después de `&`: `signature=<base64url>&key_id=<decimal>`, nada más. */
const TAIL_RE = /^signature=([A-Za-z0-9_-]{16,512})(?:=|%3D){0,2}&key_id=([0-9]{1,20})$/;

/**
 * Separa la consulta cruda en la parte firmada (todo lo anterior a `&signature=`) y la firma con su `key_id`, que deben ser
 * los dos últimos parámetros. Cualquier otra forma → undefined (firma no válida).
 */
export function splitSignedQuery(rawQuery: string): SignedQuery | undefined {
  const at = rawQuery.indexOf('&signature=');
  if (at <= 0) return undefined;
  const m = TAIL_RE.exec(rawQuery.slice(at + 1));
  if (!m) return undefined;
  return { message: rawQuery.slice(0, at), signature: Buffer.from(m[1], 'base64url'), keyId: m[2] };
}

/** ¿Firmó AdMob `message` con `key`? ECDSA con SHA-256, firma DER. Nunca lanza. */
export function verifyAdMobSignature(message: string, signature: Buffer, key: KeyObject): boolean {
  try {
    return verify('sha256', Buffer.from(message, 'utf8'), { key, dsaEncoding: 'der' }, signature);
  } catch {
    return false;
  }
}

function isP256(key: KeyObject): boolean {
  return key.type === 'public' && key.asymmetricKeyType === 'ec' && key.asymmetricKeyDetails?.namedCurve === 'prime256v1';
}

/**
 * Lee el JSON de claves (`{"keys":[{"keyId":…,"pem":"…","base64":"…"}]}`): `pem` o, si falta, `base64` (SPKI en DER). Solo
 * admite claves públicas EC P-256; las demás entradas se ignoran. Sin ninguna válida, lanza.
 */
export function parseVerifierKeys(json: unknown): Map<string, KeyObject> {
  const out = new Map<string, KeyObject>();
  const list = (json as { keys?: unknown } | null)?.keys;
  if (!Array.isArray(list)) throw new UpstreamError('admob-keys', 'formato');
  for (const entry of list as Array<Record<string, unknown>>) {
    const id = entry?.keyId;
    if ((typeof id !== 'number' && typeof id !== 'string') || !/^[0-9]{1,20}$/.test(String(id))) continue;
    try {
      const key = typeof entry.pem === 'string'
        ? createPublicKey(entry.pem)
        : typeof entry.base64 === 'string'
          ? createPublicKey({ key: Buffer.from(entry.base64, 'base64'), format: 'der', type: 'spki' })
          : undefined;
      if (key && isP256(key)) out.set(String(id), key);
    } catch {
      // Entrada ilegible: se ignora.
    }
  }
  if (out.size === 0) throw new UpstreamError('admob-keys', 'sin claves');
  return out;
}

/** Fuente de claves públicas por `key_id` (inyectable en los tests). */
export interface VerifierKeySource {
  get(keyId: string): Promise<KeyObject | undefined>;
}

/**
 * Caché de las claves de AdMob en la memoria de la Lambda (vive entre invocaciones del mismo contenedor). Descarga al primer
 * uso, cuando la caché tiene más de 24 h o cuando llega un `key_id` que no conoce (una vez, con la espera mínima). Con la caché
 * caducada y la descarga fallida no usa las claves viejas: UpstreamError (502).
 */
export class AdMobVerifierKeys implements VerifierKeySource {
  private keys = new Map<string, KeyObject>();
  private fetchedAt = Number.NEGATIVE_INFINITY;
  private lastAttempt = Number.NEGATIVE_INFINITY;
  private readonly fetchFn: FetchFn;
  private readonly now: () => number;
  private readonly url: string;

  constructor(opts: { fetchFn?: FetchFn; now?: () => number; url?: string } = {}) {
    this.fetchFn = opts.fetchFn ?? fetch;
    this.now = opts.now ?? Date.now;
    this.url = opts.url ?? ADMOB_VERIFIER_KEYS_URL;
  }

  async get(keyId: string): Promise<KeyObject | undefined> {
    const now = this.now();
    const fresh = now - this.fetchedAt < KEYS_TTL_MS;
    if (fresh) {
      const k = this.keys.get(keyId);
      if (k) return k;
    }
    if (now - this.lastAttempt < KEYS_MIN_REFRESH_MS) {
      if (fresh) return undefined; // key_id desconocido y ya se refrescó hace nada
      throw new UpstreamError('admob-keys', 'espera');
    }
    await this.refresh(now);
    return this.keys.get(keyId);
  }

  private async refresh(now: number): Promise<void> {
    this.lastAttempt = now;
    let res: Response;
    try {
      res = await this.fetchFn(this.url, { method: 'GET', signal: AbortSignal.timeout(FETCH_TIMEOUT_MS) });
    } catch (e) {
      throw new UpstreamError('admob-keys', e instanceof Error ? e.name : 'fetch');
    }
    if (!res.ok) throw new UpstreamError('admob-keys', 'http', res.status);
    const text = await res.text();
    if (text.length > MAX_KEYS_BYTES) throw new UpstreamError('admob-keys', 'demasiado grande');
    let json: unknown;
    try {
      json = JSON.parse(text);
    } catch {
      throw new UpstreamError('admob-keys', 'json');
    }
    this.keys = parseVerifierKeys(json);
    this.fetchedAt = now;
  }
}
