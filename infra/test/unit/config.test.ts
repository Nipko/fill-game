import { describe, expect, it } from 'vitest';
import { loadConfig } from '../../lib/config.ts';

const ctx = (values: Record<string, unknown>) => ({ tryGetContext: (k: string) => values[k] });

describe('loadConfig', () => {
  it('exige -c env=dev|prod', () => {
    expect(() => loadConfig(ctx({}))).toThrow(/env=dev/);
    expect(() => loadConfig(ctx({ env: 'staging' }))).toThrow(/env=dev/);
  });

  it('dev: regiones, prefijo, presupuesto 10 USD y verificador falso', () => {
    const c = loadConfig(ctx({ env: 'dev' }));
    expect(c.region).toBe('sa-east-1');
    expect(c.globalRegion).toBe('us-east-1');
    expect(c.prefix).toBe('af-dev');
    expect(c.stackPrefix).toBe('AfDev');
    expect(c.budgetUsd).toBe(10);
    expect(c.budgetThresholdsPct).toEqual([50, 80, 100]);
    expect(c.fakePlayVerifier).toBe(true);
    expect(c.firebaseProjectId).toBe('amazon-fill-dev');
    expect(c.account).toBeUndefined();
    expect(c.domain).toBeUndefined();
    expect(c.logRetentionDays).toBe(30);
  });

  it('prod: presupuesto 50 USD y nunca el verificador falso', () => {
    const c = loadConfig(ctx({ env: 'prod' }));
    expect(c.prefix).toBe('af-prod');
    expect(c.budgetUsd).toBe(50);
    expect(c.fakePlayVerifier).toBe(false);
    expect(c.firebaseProjectId).toBe('amazon-fill-prod');
  });

  it('valida la cuenta, el correo y el proyecto de Firebase', () => {
    expect(() => loadConfig(ctx({ env: 'dev', account: '12345' }))).toThrow(/12 dígitos/);
    expect(() => loadConfig(ctx({ env: 'dev', alertEmail: 'no-es-correo' }))).toThrow(/correo/);
    expect(() => loadConfig(ctx({ env: 'dev', firebaseProjectId: 'X' }))).toThrow(/Firebase/);
    expect(loadConfig(ctx({ env: 'dev', firebaseProjectId: 'otro-proyecto-1' })).firebaseProjectId).toBe('otro-proyecto-1');
  });

  it('el dominio necesita también la zona', () => {
    expect(() => loadConfig(ctx({ env: 'prod', domain: 'example.com' }))).toThrow(/hostedZoneId/);
    expect(() => loadConfig(ctx({ env: 'prod', hostedZoneId: 'Z0123456789ABCDEFGHIJ' }))).toThrow(/domain/);
    expect(() => loadConfig(ctx({ env: 'prod', domain: 'no valido', hostedZoneId: 'Z0123456789ABCDEFGHIJ' }))).toThrow(/dominio/);
    const c = loadConfig(ctx({ env: 'prod', domain: 'Example.com', hostedZoneId: 'Z0123456789ABCDEFGHIJ' }));
    expect(c.domain).toEqual({ name: 'example.com', hostedZoneId: 'Z0123456789ABCDEFGHIJ' });
  });
});
