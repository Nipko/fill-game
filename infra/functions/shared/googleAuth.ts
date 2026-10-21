// Token OAuth2 de una cuenta de servicio de Google (flujo JWT bearer, RFC 7523), sin googleapis:
// la clave JSON sale de Secrets Manager (la carga el dueño; Q-AWS-7), el JWT se firma con jose (RS256)
// y se canjea con fetch nativo en https://oauth2.googleapis.com/token.
import { SignJWT, importPKCS8 } from 'jose';
import { ConfigError, UpstreamError } from './errors.ts';
import type { SecretSource } from './secrets.ts';

/** Única URL de canje admitida (no se usa el `token_uri` del JSON: el secreto no decide adónde se llama). */
export const GOOGLE_TOKEN_URL = 'https://oauth2.googleapis.com/token';

export type FetchFn = typeof fetch;

export interface ServiceAccountKey {
  readonly client_email: string;
  readonly private_key: string;
  readonly private_key_id?: string;
}

/** Proveedor de tokens de acceso para un ámbito. */
export interface AccessTokenProvider {
  token(): Promise<string>;
}

/** Valida la forma de la clave JSON de la cuenta de servicio (sin registrar nada de ella). */
export function parseServiceAccount(raw: string): ServiceAccountKey {
  let j: unknown;
  try {
    j = JSON.parse(raw);
  } catch {
    throw new ConfigError('El secreto de la cuenta de servicio no es JSON (¿sigue el relleno inicial?).');
  }
  const o = j as Record<string, unknown>;
  if (
    typeof o !== 'object' || o === null ||
    o.type !== 'service_account' ||
    typeof o.client_email !== 'string' || !o.client_email.endsWith('.iam.gserviceaccount.com') ||
    typeof o.private_key !== 'string' || !o.private_key.includes('PRIVATE KEY')
  ) {
    throw new ConfigError('El secreto no es una clave JSON de cuenta de servicio de Google.');
  }
  return {
    client_email: o.client_email,
    private_key: o.private_key,
    private_key_id: typeof o.private_key_id === 'string' ? o.private_key_id : undefined,
  };
}

const TIMEOUT_MS = 4000;

export class ServiceAccountTokenProvider implements AccessTokenProvider {
  private cached: { token: string; until: number } | undefined;
  private readonly secrets: SecretSource;
  private readonly secretArn: string;
  private readonly scope: string;
  private readonly fetchFn: FetchFn;
  private readonly now: () => number;

  constructor(opts: { secrets: SecretSource; secretArn: string; scope: string; fetchFn?: FetchFn; now?: () => number }) {
    this.secrets = opts.secrets;
    this.secretArn = opts.secretArn;
    this.scope = opts.scope;
    this.fetchFn = opts.fetchFn ?? fetch;
    this.now = opts.now ?? Date.now;
  }

  async token(): Promise<string> {
    const now = this.now();
    if (this.cached && this.cached.until > now) return this.cached.token;
    const sa = parseServiceAccount(await this.secrets.get(this.secretArn));
    let key: Awaited<ReturnType<typeof importPKCS8>>;
    try {
      key = await importPKCS8(sa.private_key, 'RS256');
    } catch {
      throw new ConfigError('La clave privada de la cuenta de servicio no es PKCS#8 RSA válida.');
    }
    const iat = Math.floor(now / 1000);
    const assertion = await new SignJWT({ scope: this.scope })
      .setProtectedHeader({ alg: 'RS256', typ: 'JWT', ...(sa.private_key_id ? { kid: sa.private_key_id } : {}) })
      .setIssuer(sa.client_email)
      .setAudience(GOOGLE_TOKEN_URL)
      .setIssuedAt(iat)
      .setExpirationTime(iat + 3600)
      .sign(key);

    let res: Response;
    try {
      res = await this.fetchFn(GOOGLE_TOKEN_URL, {
        method: 'POST',
        headers: { 'content-type': 'application/x-www-form-urlencoded' },
        body: new URLSearchParams({ grant_type: 'urn:ietf:params:oauth:grant-type:jwt-bearer', assertion }).toString(),
        signal: AbortSignal.timeout(TIMEOUT_MS),
      });
    } catch (e) {
      throw new UpstreamError('google-oauth', e instanceof Error ? e.name : 'fetch');
    }
    if (!res.ok) throw new UpstreamError('google-oauth', `canje rechazado (${res.status})`, res.status);
    const body = (await res.json()) as { access_token?: unknown; expires_in?: unknown };
    if (typeof body.access_token !== 'string' || body.access_token === '') {
      throw new UpstreamError('google-oauth', 'respuesta sin access_token');
    }
    const expiresIn = typeof body.expires_in === 'number' ? body.expires_in : 3600;
    // Margen de 2 minutos para no usar un token a punto de caducar.
    this.cached = { token: body.access_token, until: now + Math.max(0, expiresIn - 120) * 1000 };
    return body.access_token;
  }
}
