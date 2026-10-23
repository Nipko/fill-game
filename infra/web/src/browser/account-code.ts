// Código de borrado por web (Q-B13-21; contrato infra/contracts/v1/common.schema.json#/$defs/accountCode).
// Ajustes lo enseña en 7 grupos con guiones (4-4-4-4-4-3-3; D-033, añadido del 2026-10-22, punto 11); el valor
// que se envía va sin guiones. La web normaliza antes de enviarlo: mayúsculas, sin espacios ni guiones, I y L → 1
// y O → 0 (Base32 de Crockford). Sin DOM: lo usan el script de la página y los tests.

/** Mismo patrón que `accountCode` en common.schema.json (un test lo compara). */
export const ACCOUNT_CODE_PATTERN = /^[0-7][0-9A-HJKMNP-TV-Z]{25}$/;

/** Grupos con los que Ajustes enseña el código (D-033, punto 11). */
export const DISPLAY_GROUPS: readonly number[] = [4, 4, 4, 4, 4, 3, 3];

// Espacios de cualquier tipo y guiones (también los tipográficos que deja un copiar y pegar).
// Escrito con escapes en un texto para que el script generado quede en ASCII.
const SEPARATORS = new RegExp(
  '[\\s' +
    String.fromCharCode(0xad) + // guion suave
    String.fromCharCode(0x2010) + '-' + String.fromCharCode(0x2015) + // guiones y rayas, U+2010 a U+2015
    String.fromCharCode(0x2212) + // signo menos
    String.fromCharCode(0xfe58) + String.fromCharCode(0xfe63) + String.fromCharCode(0xff0d) + // compatibilidad
    '-]',
  'gu',
);

/** Normaliza lo que escribe el jugador al formato del contrato. No valida: usa `isValidAccountCode`. */
export function normalizeAccountCode(raw: string): string {
  return raw
    .normalize('NFKC')
    .toUpperCase()
    .replace(SEPARATORS, '')
    .replace(/[IL]/g, '1')
    .replace(/O/g, '0');
}

export function isValidAccountCode(code: string): boolean {
  return ACCOUNT_CODE_PATTERN.test(code);
}

/** Código normalizado en los grupos de Ajustes, para que el jugador lo reconozca al confirmar. */
export function formatAccountCode(code: string): string {
  const parts: string[] = [];
  let i = 0;
  for (const n of DISPLAY_GROUPS) {
    if (i >= code.length) break;
    parts.push(code.slice(i, i + n));
    i += n;
  }
  if (i < code.length) parts.push(code.slice(i));
  return parts.join('-');
}
