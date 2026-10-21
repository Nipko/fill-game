// Utilidades de los tests: sintetizan los stacks sin credenciales, sin lookups y sin empaquetar Lambdas.
import { App } from 'aws-cdk-lib';
import type { Stack } from 'aws-cdk-lib';
import { Template } from 'aws-cdk-lib/assertions';
import { readFileSync } from 'node:fs';
import * as path from 'node:path';
import type { AfStacks } from '../lib/app-builder.ts';
import { buildApp } from '../lib/app-builder.ts';
import type { AfConfig } from '../lib/config.ts';
import { loadConfig } from '../lib/config.ts';
import { INFRA_ROOT } from '../lib/paths.ts';

const CDK_CONTEXT = (JSON.parse(readFileSync(path.join(INFRA_ROOT, 'cdk.json'), 'utf8')) as { context: Record<string, unknown> }).context;

/** Cuenta de documentación de AWS: solo para probar el modo con dominio. No es una cuenta real. */
export const DOC_ACCOUNT = '123456789012';

export interface Built {
  readonly app: App;
  readonly config: AfConfig;
  readonly stacks: AfStacks;
  readonly t: Record<keyof AfStacks, Template>;
}

const cache = new Map<string, Built>();

export function build(context: Record<string, string>, opts: { bundle?: boolean } = {}): Built {
  const key = JSON.stringify({ context, opts });
  const hit = cache.get(key);
  if (hit) return hit;
  const app = new App({
    context: {
      ...CDK_CONTEXT,
      ...context,
      // Sin empaquetar las Lambdas en los tests de stacks (cdk synth sí las empaqueta).
      ...(opts.bundle ? {} : { 'aws:cdk:bundling-stacks': [] }),
    },
  });
  const config = loadConfig(app.node);
  const stacks = buildApp(app, config, { nag: false });
  const t = Object.fromEntries(
    (Object.entries(stacks) as Array<[string, Stack]>).map(([k, s]) => [k, Template.fromStack(s)]),
  ) as Record<keyof AfStacks, Template>;
  const built = { app, config, stacks, t };
  cache.set(key, built);
  return built;
}

export const DEV = { env: 'dev' } as const;
export const PROD = { env: 'prod' } as const;
export const PROD_DOMAIN = {
  env: 'prod',
  account: DOC_ACCOUNT,
  domain: 'example.com',
  hostedZoneId: 'Z0123456789ABCDEFGHIJ',
  alertEmail: 'alerts@example.com',
} as const;

/** Todas las plantillas de un entorno como JSON. */
export function allTemplates(b: Built): Array<{ name: string; json: Record<string, unknown> }> {
  return Object.entries(b.t).map(([name, t]) => ({ name, json: t.toJSON() }));
}

/** Recursos de un tipo en una plantilla JSON. */
export function resourcesOfType(json: Record<string, unknown>, type: string): Array<[string, Record<string, unknown>]> {
  const res = (json.Resources ?? {}) as Record<string, { Type: string; Properties?: Record<string, unknown> }>;
  return Object.entries(res)
    .filter(([, r]) => r.Type === type)
    .map(([id, r]) => [id, (r.Properties ?? {})]);
}
