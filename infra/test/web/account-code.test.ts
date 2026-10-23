// Normalizador del código de borrado por web (D-033, añadido del 2026-10-22, punto 11; common.schema.json).
import { readFileSync } from 'node:fs';
import * as path from 'node:path';
import { describe, expect, it } from 'vitest';
import { CONTRACTS_DIR } from '../../lib/paths.ts';
import {
  ACCOUNT_CODE_PATTERN,
  DISPLAY_GROUPS,
  formatAccountCode,
  isValidAccountCode,
  normalizeAccountCode,
} from '../../web/src/browser/account-code.ts';

const CODE = '3HZ7M2Q9XK4T8VB1NC6DRF0WPE';
const SHOWN = '3HZ7-M2Q9-XK4T-8VB1-NC6D-RF0-WPE';

describe('normalizeAccountCode', () => {
  it('usa el mismo patrón que el contrato', () => {
    const common = JSON.parse(readFileSync(path.join(CONTRACTS_DIR, 'common.schema.json'), 'utf8')) as {
      $defs: { accountCode: { pattern: string } };
    };
    expect(ACCOUNT_CODE_PATTERN.source).toBe(common.$defs.accountCode.pattern);
  });

  it('acepta el código tal como lo enseña Ajustes (grupos 4-4-4-4-4-3-3 con guiones)', () => {
    expect(DISPLAY_GROUPS.reduce((a, b) => a + b, 0)).toBe(26);
    expect(normalizeAccountCode(SHOWN)).toBe(CODE);
    expect(isValidAccountCode(normalizeAccountCode(SHOWN))).toBe(true);
  });

  it('pasa a mayúsculas y quita espacios y guiones de cualquier tipo', () => {
    expect(normalizeAccountCode(' 3hz7 m2q9\txk4t\n8vb1-nc6d-rf0-wpe ')).toBe(CODE);
    expect(normalizeAccountCode('3HZ7‐M2Q9‑XK4T– 8VB1—NC6D−RF0 WPE')).toBe(CODE);
    expect(normalizeAccountCode('3HZ7－M2Q9­XK4T8VB1NC6DRF0WPE')).toBe(CODE);
  });

  it('convierte I y L en 1 y O en 0 (Base32 de Crockford), también en minúsculas', () => {
    expect(normalizeAccountCode('IiLl')).toBe('1111');
    expect(normalizeAccountCode('Oo')).toBe('00');
    expect(normalizeAccountCode('3HZ7-M2Q9-XK4T-8VBI-NC6D-RFO-WPE')).toBe(CODE);
    expect(normalizeAccountCode('3hz7-m2q9-xk4t-8vbl-nc6d-rfo-wpe')).toBe(CODE);
  });

  it('normaliza caracteres de ancho completo (NFKC)', () => {
    expect(normalizeAccountCode('３ＨＺ７')).toBe('3HZ7');
  });

  it('no inventa: lo que no es Base32 de Crockford sigue siendo inválido', () => {
    expect(isValidAccountCode(normalizeAccountCode(CODE.slice(0, 25)))).toBe(false); // 25 caracteres
    expect(isValidAccountCode(normalizeAccountCode(CODE + 'A'))).toBe(false); // 27
    expect(isValidAccountCode(normalizeAccountCode('8' + CODE.slice(1)))).toBe(false); // el primero va de 0 a 7
    expect(isValidAccountCode(normalizeAccountCode(CODE.slice(0, 25) + 'U'))).toBe(false); // U no existe
    expect(isValidAccountCode(normalizeAccountCode(CODE.slice(0, 25) + '*'))).toBe(false);
    expect(isValidAccountCode(normalizeAccountCode(CODE.slice(0, 13) + '_' + CODE.slice(13)))).toBe(false);
    expect(isValidAccountCode(normalizeAccountCode(''))).toBe(false);
  });

  it('el ejemplo de la página y de strings.csv es un código con formato válido', () => {
    expect(isValidAccountCode(CODE)).toBe(true);
  });
});

describe('formatAccountCode', () => {
  it('vuelve a poner los grupos de Ajustes', () => {
    expect(formatAccountCode(CODE)).toBe(SHOWN);
    expect(normalizeAccountCode(formatAccountCode(CODE))).toBe(CODE);
  });

  it('no pierde caracteres si el texto es más largo o más corto', () => {
    expect(formatAccountCode('ABCDE')).toBe('ABCD-E');
    expect(formatAccountCode(CODE + 'XY')).toBe(SHOWN + '-XY');
  });
});
