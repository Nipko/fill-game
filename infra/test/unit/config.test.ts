import { describe, expect, it } from 'vitest';
import { loadConfig } from '../../lib/config.ts';

const APP = '1:123456789:android:0123456789abcdef';
// En prod el contexto firebaseAppIds es obligatorio (F2c) y, sin domain, noDomain (D-044): los ctx de prod los llevan
// salvo que se diga otra cosa.
const ctx = (values: Record<string, unknown>) => {
  let v = values.env === 'prod' && !('firebaseAppIds' in values) ? { firebaseAppIds: APP, ...values } : values;
  if (v.env === 'prod' && !('domain' in v) && !('noDomain' in v)) v = { noDomain: 'true', ...v };
  return { tryGetContext: (k: string) => v[k] };
};

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

  it('dominio con zona de Route 53: la vía de siempre, con cdn. y api.', () => {
    expect(() => loadConfig(ctx({ env: 'prod', hostedZoneId: 'Z0123456789ABCDEFGHIJ' }))).toThrow(/domain/);
    expect(() => loadConfig(ctx({ env: 'prod', domain: 'no valido', hostedZoneId: 'Z0123456789ABCDEFGHIJ' }))).toThrow(/dominio/);
    expect(() => loadConfig(ctx({ env: 'prod', domain: 'example.com', hostedZoneId: 'nozone' }))).toThrow(/zona/);
    const c = loadConfig(ctx({ env: 'prod', domain: 'Example.com', hostedZoneId: 'Z0123456789ABCDEFGHIJ' }));
    expect(c.domain).toEqual({ name: 'example.com', hostedZoneId: 'Z0123456789ABCDEFGHIJ', cdn: true, api: true });
    expect(() => loadConfig(ctx({ env: 'prod', domain: 'example.com', hostedZoneId: 'Z0123456789ABCDEFGHIJ', domainExtras: 'cdn' }))).toThrow(/domainExtras/);
  });

  it('dominio sin zona: DNS externo (D-044), solo la web salvo que se pidan cdn. o api.', () => {
    const c = loadConfig(ctx({ env: 'prod', domain: 'Example.com' }));
    expect(c.domain).toEqual({ name: 'example.com', cdn: false, api: false });
    expect(loadConfig(ctx({ env: 'prod', domain: 'example.com', domainExtras: 'cdn' })).domain).toEqual({ name: 'example.com', cdn: true, api: false });
    expect(loadConfig(ctx({ env: 'prod', domain: 'example.com', domainExtras: 'api, CDN' })).domain).toEqual({ name: 'example.com', cdn: true, api: true });
    expect(() => loadConfig(ctx({ env: 'prod', domain: 'example.com', domainExtras: 'www' }))).toThrow(/domainExtras/);
    expect(() => loadConfig(ctx({ env: 'prod', domainExtras: 'cdn' }))).toThrow(/domain/);
    expect(() => loadConfig(ctx({ env: 'prod', domain: 'no valido' }))).toThrow(/dominio/);
  });

  it('webOnly: solo la web, sin firebaseAppIds en prod y sin CDN ni API en el dominio', () => {
    expect(loadConfig(ctx({ env: 'prod' })).webOnly).toBe(false);
    const c = loadConfig(ctx({ env: 'prod', webOnly: 'true', firebaseAppIds: undefined, domain: 'example.com' }));
    expect(c.webOnly).toBe(true);
    expect(c.firebaseAppIds).toEqual([]);
    expect(loadConfig(ctx({ env: 'dev', webOnly: true })).webOnly).toBe(true);
    expect(loadConfig(ctx({ env: 'prod', webOnly: 'false' })).webOnly).toBe(false);
    expect(() => loadConfig(ctx({ env: 'prod', webOnly: 'si' }))).toThrow(/true o false/);
    expect(() => loadConfig(ctx({ env: 'prod', webOnly: 'true', domain: 'example.com', domainExtras: 'cdn' }))).toThrow(/webOnly/);
    expect(() => loadConfig(ctx({ env: 'prod', webOnly: 'true', domain: 'example.com', hostedZoneId: 'Z0123456789ABCDEFGHIJ' }))).toThrow(/webOnly/);
  });

  it('prod sin dominio falla salvo con noDomain=true (D-044: no quitar tintalu.com por olvido)', () => {
    expect(() => loadConfig(ctx({ env: 'prod', noDomain: undefined }))).toThrow(/prod falta el contexto "domain".*noDomain=true/);
    expect(() => loadConfig(ctx({ env: 'prod', webOnly: 'true', firebaseAppIds: undefined, noDomain: undefined }))).toThrow(/"domain"/);
    expect(loadConfig(ctx({ env: 'prod', noDomain: 'true' })).domain).toBeUndefined();
    expect(loadConfig(ctx({ env: 'prod', domain: 'tintalu.com' })).domain?.name).toBe('tintalu.com');
    expect(() => loadConfig(ctx({ env: 'prod', domain: 'tintalu.com', noDomain: 'true' }))).toThrow(/noDomain/);
    expect(() => loadConfig(ctx({ env: 'prod', noDomain: 'si' }))).toThrow(/true o false/);
    // dev no lo exige.
    expect(loadConfig(ctx({ env: 'dev' })).domain).toBeUndefined();
  });

  it('paquete de Play: provisional en dev, solo por contexto en prod y nunca el provisional (regla 6)', () => {
    expect(loadConfig(ctx({ env: 'dev' })).packageName).toBe('com.amazonfill.dev');
    expect(loadConfig(ctx({ env: 'prod' })).packageName).toBeUndefined();
    expect(() => loadConfig(ctx({ env: 'prod', packageName: 'com.amazonfill.dev' }))).toThrow(/provisional/);
    expect(loadConfig(ctx({ env: 'prod', packageName: 'com.estudio.juego' })).packageName).toBe('com.estudio.juego');
    expect(() => loadConfig(ctx({ env: 'dev', packageName: 'no valido' }))).toThrow(/applicationId/);
  });

  it('ids de app de Firebase para App Check: obligatorios en prod, opcionales en dev y validados', () => {
    expect(() => loadConfig(ctx({ env: 'prod', firebaseAppIds: undefined }))).toThrow(/prod falta el contexto "firebaseAppIds"/);
    expect(() => loadConfig(ctx({ env: 'prod', firebaseAppIds: '' }))).toThrow(/firebaseAppIds/);
    expect(() => loadConfig(ctx({ env: 'prod', firebaseAppIds: ' , ' }))).toThrow(/firebaseAppIds/);
    expect(loadConfig(ctx({ env: 'prod' })).firebaseAppIds).toEqual([APP]);
    expect(loadConfig(ctx({ env: 'dev' })).firebaseAppIds).toEqual([]);
    expect(loadConfig(ctx({ env: 'dev', firebaseAppIds: '1:123:android:abcdef, 1:123:android:0123456789' })).firebaseAppIds).toEqual([
      '1:123:android:abcdef',
      '1:123:android:0123456789',
    ]);
    expect(() => loadConfig(ctx({ env: 'dev', firebaseAppIds: 'amazon-fill-dev' }))).toThrow(/firebaseAppIds/);
  });
});
