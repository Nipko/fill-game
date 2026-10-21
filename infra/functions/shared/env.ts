// Variables de entorno de las Lambdas (las fija lib/api-stack.ts). Faltar una es un error de despliegue.
import { ConfigError } from './errors.ts';

export type Env = Readonly<Record<string, string | undefined>>;

export function requireEnv(env: Env, name: string): string {
  const v = env[name];
  if (v === undefined || v === '') throw new ConfigError(`Falta la variable de entorno ${name}.`);
  return v;
}

/**
 * Verificador falso de Play y de la integridad (plan §4; hallazgo 2): solo con `AF_FAKE_PLAY_VERIFIER=1`
 * **y** `AF_ENV=dev`. En prod la variable no existe (test de stacks) y, aunque existiera, aquí no se activa.
 */
export function fakeVerifierEnabled(env: Env): boolean {
  return env.AF_FAKE_PLAY_VERIFIER === '1' && env.AF_ENV === 'dev';
}
