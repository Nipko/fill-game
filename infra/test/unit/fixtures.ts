// Utilidades de los tests unitarios: eventos de la HTTP API, fetch falso (Google, Firebase) y claves de prueba.
// Ninguna llamada sale a la red: todo fetch es una función local que registra las peticiones.
import type { APIGatewayProxyStructuredResultV2 } from 'aws-lambda';
import { SignJWT, createLocalJWKSet, exportJWK, exportPKCS8, generateKeyPair } from 'jose';
import type { CryptoKey as JoseKey, JWTVerifyGetKey } from 'jose';
import type { HttpEvent } from '../../functions/shared/http.ts';
import type { SecretSource } from '../../functions/shared/secrets.ts';

export const UID = 'kT3x9QeVb2Rz8LmN4pWs7YcA1dF5';
export const OTHER_UID = 'Zz9y8X7w6V5u4T3s2R1q0PoNmLkJ';
export const NOW = 1_792_540_800_000; // 2026-10-21T00:00:00Z aprox.

export function httpEvent(opts: {
  routeKey: string;
  body?: unknown;
  rawBody?: string;
  uid?: string;
  headers?: Record<string, string>;
  query?: Record<string, string>;
}): HttpEvent {
  const [method, path] = opts.routeKey.split(' ');
  return {
    version: '2.0',
    routeKey: opts.routeKey,
    rawPath: path,
    rawQueryString: new URLSearchParams(opts.query ?? {}).toString(),
    headers: opts.headers ?? {},
    queryStringParameters: opts.query,
    isBase64Encoded: false,
    body: opts.rawBody ?? (opts.body === undefined ? undefined : JSON.stringify(opts.body)),
    requestContext: {
      accountId: 'anon',
      apiId: 'api',
      domainName: 'example.invalid',
      domainPrefix: 'api',
      http: { method, path, protocol: 'HTTP/1.1', sourceIp: '192.0.2.1', userAgent: 'test' },
      requestId: 'req',
      routeKey: opts.routeKey,
      stage: '$default',
      time: '',
      timeEpoch: NOW,
      ...(opts.uid ? { authorizer: { jwt: { claims: { sub: opts.uid }, scopes: [] } } } : {}),
    },
  } as HttpEvent;
}

export interface Res {
  status: number;
  body: Record<string, unknown>;
  headers: Record<string, string>;
}

export function res(r: APIGatewayProxyStructuredResultV2): Res {
  return {
    status: r.statusCode ?? 0,
    body: JSON.parse(r.body ?? '{}') as Record<string, unknown>,
    headers: (r.headers ?? {}) as Record<string, string>,
  };
}

export interface FetchCall {
  url: string;
  method: string;
  body: string;
  headers: Record<string, string>;
}

/** fetch falso: `routes` decide la respuesta por URL; todas las llamadas quedan en `calls`. */
export function fakeFetch(route: (call: FetchCall) => Response | Promise<Response>): typeof fetch & { calls: FetchCall[] } {
  const calls: FetchCall[] = [];
  const f = (async (input: string | URL | Request, init?: RequestInit): Promise<Response> => {
    const url = typeof input === 'string' ? input : input instanceof URL ? input.toString() : input.url;
    const call: FetchCall = {
      url,
      method: init?.method ?? 'GET',
      body: typeof init?.body === 'string' ? init.body : '',
      headers: Object.fromEntries(Object.entries((init?.headers ?? {}) as Record<string, string>).map(([k, v]) => [k.toLowerCase(), v])),
    };
    calls.push(call);
    return route(call);
  }) as typeof fetch & { calls: FetchCall[] };
  f.calls = calls;
  return f;
}

export const jsonResponse = (status: number, body: unknown): Response =>
  new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } });

/** Clave JSON de cuenta de servicio de prueba (RSA generada en el test; nunca una real). */
export async function testServiceAccount(): Promise<{ json: string; publicKey: JoseKey }> {
  const { privateKey, publicKey } = await generateKeyPair('RS256', { extractable: true });
  const pem = await exportPKCS8(privateKey);
  return {
    json: JSON.stringify({
      type: 'service_account',
      project_id: 'amazon-fill-dev',
      private_key_id: 'test-kid',
      private_key: pem,
      client_email: 'test@amazon-fill-dev.iam.gserviceaccount.com',
      token_uri: 'https://evil.invalid/token',
    }),
    publicKey,
  };
}

export class MapSecrets implements SecretSource {
  readonly values = new Map<string, string>();
  reads = 0;
  get(arn: string): Promise<string> {
    this.reads++;
    const v = this.values.get(arn);
    return v === undefined ? Promise.reject(new Error('sin secreto')) : Promise.resolve(v);
  }
}

/** Emisor de tokens de App Check de prueba con su JWKS local. */
export async function appCheckIssuer(projectId = 'amazon-fill-dev', projectNumber = '123456789') {
  const { privateKey, publicKey } = await generateKeyPair('RS256', { extractable: true });
  const jwk = { ...(await exportJWK(publicKey)), kid: 'k1', alg: 'RS256', use: 'sig' };
  const keySet: JWTVerifyGetKey = createLocalJWKSet({ keys: [jwk] });
  const other = await generateKeyPair('RS256');
  const sign = async (
    over: {
      iss?: string;
      aud?: string[];
      sub?: string;
      expS?: number;
      iatS?: number;
      jti?: string;
      foreignKey?: boolean;
      alg?: 'RS256';
    } = {},
  ): Promise<string> => {
    const iat = over.iatS ?? Math.floor(NOW / 1000);
    return new SignJWT({ ...(over.jti !== undefined ? { jti: over.jti } : {}) })
      .setProtectedHeader({ alg: 'RS256', kid: 'k1', typ: 'JWT' })
      .setIssuer(over.iss ?? `https://firebaseappcheck.googleapis.com/${projectNumber}`)
      .setAudience(over.aud ?? [`projects/${projectNumber}`, `projects/${projectId}`])
      .setSubject(over.sub ?? `1:${projectNumber}:android:abcdef0123456789`)
      .setIssuedAt(iat)
      .setExpirationTime(over.expS ?? iat + 3600)
      .sign(over.foreignKey ? other.privateKey : privateKey);
  };
  return { keySet, sign, projectId, projectNumber };
}
