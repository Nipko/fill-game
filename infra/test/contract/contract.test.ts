// Tests del contrato v1 (Q-AWS-12): los ejemplos validan contra sus esquemas y los inválidos no.
import { readdirSync, readFileSync } from 'node:fs';
import * as path from 'node:path';
import { describe, expect, it } from 'vitest';
import { loadContractIndex, routeKey } from '../../lib/contract.ts';
import { CONTRACTS_DIR } from '../../lib/paths.ts';
import { readExample, SCHEMA_BASE, SCHEMA_FILES, validator } from './ajv.ts';

const index = loadContractIndex();
const EXAMPLES = readdirSync(path.join(CONTRACTS_DIR, 'examples')).filter((f) => f.endsWith('.json')).sort();

describe('contrato v1: esquemas', () => {
  it('todos los esquemas compilan en modo estricto y su $id coincide con el archivo', () => {
    for (const f of SCHEMA_FILES) {
      const json = JSON.parse(readFileSync(path.join(CONTRACTS_DIR, f), 'utf8')) as { $id: string; $schema: string };
      expect(json.$id).toBe(SCHEMA_BASE + f);
      expect(json.$schema).toBe('https://json-schema.org/draft/2020-12/schema');
      expect(() => validator(f)).not.toThrow();
    }
  });

  it('cada esquema citado en el índice existe y cada esquema se usa', () => {
    const cited = new Set<string>(['common.schema.json', 'error.schema.json']);
    for (const op of index.operations) {
      if (op.request) cited.add(op.request);
      cited.add(op.response);
    }
    for (const j of index.jobs) {
      cited.add(j.input);
      cited.add(j.output);
    }
    expect([...cited].sort()).toEqual(SCHEMA_FILES);
  });

  it('las rutas son únicas, van bajo /v1/ y cubren las 7 funciones', () => {
    const keys = index.operations.map(routeKey);
    expect(new Set(keys).size).toBe(keys.length);
    const fns = new Set([...index.operations.map((o) => o.function), ...index.jobs.map((j) => j.function)]);
    expect([...fns].sort()).toEqual(['delete-account', 'league-board', 'league-wins', 'restore-ledger', 'rewarded-ssv', 'verify-purchase', 'voided-purchases-job']);
  });

  it('integridad solo con Firebase Auth, y nunca en restoreLedger (plan F2-5)', () => {
    for (const op of index.operations) {
      if (op.integrity) expect(op.auth).toBe('firebase');
    }
    expect(index.operations.find((o) => o.operation === 'restoreLedger')?.integrity).toBe(false);
    expect(index.operations.find((o) => o.operation === 'verifyPurchase')?.integrity).toBe(true);
    expect(index.operations.find((o) => o.operation === 'deleteAccount')?.integrity).toBe(true);
  });

  it('el borrado por web no usa el UID y tiene el límite estricto (Q-B13-21)', () => {
    const web = index.operations.filter((o) => o.path.startsWith('/v1/account/delete-'));
    expect(web.map((o) => o.operation).sort()).toEqual(['deleteConfirm', 'deleteRequest']);
    for (const op of web) {
      expect(op.auth).toBe('none');
      expect(op.throttle).toBe('delete');
    }
  });

  it('todo error declarado está en el enum de error.schema.json', () => {
    const errorSchema = JSON.parse(readFileSync(path.join(CONTRACTS_DIR, 'error.schema.json'), 'utf8')) as {
      properties: { error: { properties: { code: { enum: string[] } } } };
    };
    const codes = new Set(errorSchema.properties.error.properties.code.enum);
    for (const op of index.operations) {
      for (const list of Object.values(op.errors)) for (const c of list) expect(codes.has(c), `${op.operation}: ${c}`).toBe(true);
      expect(op.errors['501']).toContain('not_implemented');
    }
  });
});

describe('contrato v1: ejemplos', () => {
  const used = new Set<string>();

  for (const op of index.operations) {
    it(`${op.operation}: los ejemplos válidos pasan y los inválidos no`, () => {
      if (op.request) {
        const v = validator(op.request);
        for (const f of op.examples.request) {
          used.add(f);
          expect(v(readExample(f)), `${f}: ${JSON.stringify(v.errors)}`).toBe(true);
        }
        for (const f of op.examples.invalidRequest) {
          used.add(f);
          expect(v(readExample(f)), `${f} debería fallar`).toBe(false);
        }
      } else {
        expect(op.examples.request).toEqual([]);
      }
      const r = validator(op.response);
      expect(op.examples.response.length).toBeGreaterThan(0);
      for (const f of op.examples.response) {
        used.add(f);
        expect(r(readExample(f)), `${f}: ${JSON.stringify(r.errors)}`).toBe(true);
      }
    });
  }

  for (const job of index.jobs) {
    it(`${job.operation}: entrada y salida`, () => {
      const vi = validator(job.input);
      const vo = validator(job.output);
      for (const f of job.examples.input) {
        used.add(f);
        expect(vi(readExample(f)), f).toBe(true);
      }
      for (const f of job.examples.output) {
        used.add(f);
        expect(vo(readExample(f)), f).toBe(true);
      }
    });
  }

  it('los ejemplos de error validan contra error.schema.json', () => {
    const v = validator('error.schema.json');
    for (const f of EXAMPLES.filter((e) => e.startsWith('error.'))) {
      used.add(f);
      expect(v(readExample(f)), `${f}: ${JSON.stringify(v.errors)}`).toBe(true);
    }
  });

  it('no hay ejemplos huérfanos', () => {
    expect(EXAMPLES.filter((f) => !used.has(f))).toEqual([]);
  });
});

describe('contrato v1: formatos', () => {
  it('el código de borrado tiene 128 bits como mínimo y no admite un UID', () => {
    const v = validator('account-delete-request.request.schema.json');
    // 26 caracteres de Base32 (5 bits) = 130 bits; el primero, de 0 a 7, deja 128 bits efectivos.
    expect(v({ code: '7ZZZZZZZZZZZZZZZZZZZZZZZZZ' })).toBe(true);
    expect(v({ code: '8ZZZZZZZZZZZZZZZZZZZZZZZZZ' })).toBe(false);
    expect(v({ code: '0123456789ABCDEFGHJKMNPQR' })).toBe(false); // 25 caracteres
    expect(v({ code: '0123456789ABCDEFGHJKMNPQRI' })).toBe(false); // I no es Crockford
    expect(v({ code: 'kT3x9QeVb2Rz8LmN4pWs7YcA1dF5' })).toBe(false); // UID de Firebase
    expect(v({ code: '0123456789ABCDEFGHJKMNPQRS', uid: 'x' })).toBe(false);
  });

  it('verifyPurchase no acepta un uid en el cuerpo (sale del token)', () => {
    const v = validator('verify-purchase.request.schema.json');
    expect(v({ token: 'abc', sku: 'af_noads', uid: 'kT3x9QeVb2Rz8LmN4pWs7YcA1dF5' })).toBe(false);
    expect(v({ token: 'abc', sku: 'noads' })).toBe(false);
  });
});
