// Los validadores de las Lambdas (functions/shared/validate.ts) dicen lo mismo que el contrato v1: aceptan los
// ejemplos válidos de contracts/v1/examples y rechazan con 400 los inválidos.
import { describe, expect, it } from 'vitest';
import { HttpError } from '../../functions/shared/errors.ts';
import {
  parseAccountDelete,
  parseDeleteConfirm,
  parseDeleteRequest,
  parseRestoreLedger,
  parseSsvQuery,
  parseVerifyPurchase,
} from '../../functions/shared/validate.ts';
import { CATALOG } from '../../functions/shared/catalog.ts';
import { parseRequest } from '../../functions/league/rules.ts';
import { loadContractIndex } from '../../lib/contract.ts';
import { readExample, validator } from './ajv.ts';

const PARSERS: Record<string, (body: Record<string, unknown>) => unknown> = {
  leagueWins: parseRequest,
  verifyPurchase: parseVerifyPurchase,
  restoreLedger: parseRestoreLedger,
  rewardedSsv: (b) => parseSsvQuery(b as Record<string, string>),
  rewardedSsvPost: (b) => parseSsvQuery(b as Record<string, string>),
  deleteAccount: parseAccountDelete,
  deleteRequest: parseDeleteRequest,
  deleteConfirm: parseDeleteConfirm,
};

describe('validadores de las Lambdas frente al contrato v1', () => {
  const index = loadContractIndex();

  for (const op of index.operations.filter((o) => o.request)) {
    it(`${op.operation}: acepta los ejemplos válidos y rechaza los inválidos con 400`, () => {
      const parse = PARSERS[op.operation];
      expect(parse, op.operation).toBeDefined();
      for (const f of op.examples.request) expect(() => parse(readExample(f) as Record<string, unknown>), f).not.toThrow();
      for (const f of op.examples.invalidRequest) {
        try {
          parse(readExample(f) as Record<string, unknown>);
          expect.fail(`${f} debería fallar`);
        } catch (e) {
          expect(e, f).toBeInstanceOf(HttpError);
          expect((e as HttpError).status).toBe(400);
        }
      }
    });
  }

  it('lo que el validador acepta también lo acepta el esquema (casos límite)', () => {
    const vp = validator('verify-purchase.request.schema.json');
    const cases = [
      { token: 'a', sku: 'af_noads' },
      { token: 'a'.repeat(4096), sku: 'af_coins_2', obfuscated_account_id: 'x' },
    ];
    for (const c of cases) {
      expect(() => parseVerifyPurchase(c)).not.toThrow();
      expect(vp(c)).toBe(true);
    }
    const bad = [{ token: 'a'.repeat(4097), sku: 'af_noads' }, { token: '', sku: 'af_noads' }, { token: 'a', sku: 'af_noads', obfuscated_account_id: '' }];
    for (const c of bad) {
      expect(() => parseVerifyPurchase(c)).toThrow(HttpError);
      expect(vp(c)).toBe(false);
    }
  });

  it('cada SKU del catálogo del servidor cumple el patrón del contrato y su grant valida', () => {
    const v = validator('verify-purchase.response.schema.json');
    for (const [sku, e] of CATALOG) {
      expect(() => parseVerifyPurchase({ token: 't', sku })).not.toThrow();
      const body = { status: 'ok', token_hash: '0'.repeat(64), grant: e.grant, ...(e.entitlements ? { entitlements: e.entitlements } : {}) };
      expect(v(body), `${sku}: ${JSON.stringify(v.errors)}`).toBe(true);
    }
    // Los 13 SKU de 1.0 (05 §4.1; 09: sin af_pass).
    expect(CATALOG.size).toBe(13);
    expect(CATALOG.has('af_pass')).toBe(false);
  });
});
