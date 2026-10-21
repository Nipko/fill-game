// Firma de los callbacks S2S de recompensa de AppLovin MAX, aislada para cambiarla sin tocar el handler.
//
// [NV] SUPUESTO (lo confirma F0-R o la prueba F4-2 con anuncios de prueba): MAX sustituye la macro
// {EVENT_TOKEN} por hex(SHA-256(<valor de {EVENT_ID}> + <clave S2S de recompensas de la cuenta de MAX>)).
// La URL que el dueño configura en el panel de MAX (infra/README.md) pasa esos valores como `event_id` y
// `event_token`. Si el formato real fuera otro (p. ej. {EVENT_TOKEN_ALL} sobre todos los parámetros, o
// HMAC), solo cambia esta función y sus tests.
import { createHash } from 'node:crypto';
import { safeEqual } from '../shared/crypto.ts';

/** El event_id se usa tal cual en el hash: se acota para no hashear basura enorme. */
const EVENT_ID_RE = /^[A-Za-z0-9._:-]{1,256}$/;

/** Calcula el event_token esperado (SHA-256 en hexadecimal en minúsculas). */
export function expectedEventToken(eventId: string, key: string): string {
  return createHash('sha256').update(eventId + key, 'utf8').digest('hex');
}

/** ¿El callback lo firmó MAX con nuestra clave? Comparación en tiempo constante; clave vacía → nunca. */
export function verifyMaxCallback(eventId: string | undefined, eventToken: string | undefined, key: string): boolean {
  if (!key || !eventId || !eventToken) return false;
  if (!EVENT_ID_RE.test(eventId) || !/^[0-9a-fA-F]{64}$/.test(eventToken)) return false;
  return safeEqual(expectedEventToken(eventId, key), eventToken.toLowerCase());
}
