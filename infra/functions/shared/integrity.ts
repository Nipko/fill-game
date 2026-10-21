// Integridad de la app (Q-AWS-4): variante (a), token de App Check «de uso limitado» verificado con jose
// contra el JWKS público de App Check; variante (b), Play Integrity directo, declarada y sin implementar.
// Por encima, `requireIntegrity`: caché del veredicto 24 h por UID en af-<env>-integrity y anti-replay del
// token en af-<env>-rate_limits. Solo la exigen verifyPurchase y deleteAccount desde la app (plan §1).
//
// [NV] Formato del token de App Check (lo confirma F0-R/F4): JWT RS256 con `iss` =
// https://firebaseappcheck.googleapis.com/<número de proyecto>, `aud` = ["projects/<número>",
// "projects/<id>"], `sub` = id de la app de Firebase y, en los de uso limitado, `jti`.
import { GetCommand, TransactWriteCommand } from '@aws-sdk/lib-dynamodb';
import type { DynamoDBDocumentClient } from '@aws-sdk/lib-dynamodb';
import { createRemoteJWKSet, errors as joseErrors, jwtVerify } from 'jose';
import type { JWTVerifyGetKey } from 'jose';
import { sha256hex } from './crypto.ts';
import { cancellationCodes, epochS } from './ddb.ts';
import { UpstreamError, integrityFailed } from './errors.ts';
import { log, metric, uidHash } from './log.ts';
import { consumeOnce, notDeletedCheck } from './rateLimit.ts';

export const APP_CHECK_JWKS_URL = 'https://firebaseappcheck.googleapis.com/v1/jwks';
const APP_CHECK_ISSUER_RE = /^https:\/\/firebaseappcheck\.googleapis\.com\/(\d{1,20})$/;

/** Cabecera del token de integridad (contrato v1: conventions.integrityHeader). */
export const INTEGRITY_HEADER = 'x-firebase-appcheck';
/** Caché del veredicto por UID (plan §1: 24 h [P]). */
export const VERDICT_TTL_S = 24 * 3600;

export type IntegrityVariant = 'app-check' | 'play-integrity' | 'fake';

export interface IntegrityVerdict {
  readonly ok: boolean;
  readonly reason?: string;
  /** Identificador único del token, para el anti-replay (si no lo trae, se usa el hash del token). */
  readonly jti?: string;
  /** Caducidad del token en segundos epoch. */
  readonly exp?: number;
}

export interface IntegrityVerifier {
  readonly variant: IntegrityVariant;
  verify(token: string): Promise<IntegrityVerdict>;
}

/** (a) App Check de uso limitado, verificado contra el JWKS de App Check (Q-AWS-4, recomendada). */
export class AppCheckVerifier implements IntegrityVerifier {
  readonly variant = 'app-check' as const;
  private readonly projectId: string;
  private readonly keySet: JWTVerifyGetKey;
  private readonly appIds: readonly string[];
  private readonly now: () => number;

  constructor(opts: { projectId: string; keySet?: JWTVerifyGetKey; appIds?: readonly string[]; now?: () => number }) {
    this.projectId = opts.projectId;
    this.keySet = opts.keySet ?? createRemoteJWKSet(new URL(APP_CHECK_JWKS_URL), { timeoutDuration: 3000, cacheMaxAge: 6 * 3600_000 });
    this.appIds = opts.appIds ?? [];
    this.now = opts.now ?? Date.now;
  }

  async verify(token: string): Promise<IntegrityVerdict> {
    try {
      const { payload, protectedHeader } = await jwtVerify(token, this.keySet, {
        algorithms: ['RS256'],
        audience: `projects/${this.projectId}`,
        currentDate: new Date(this.now()),
        clockTolerance: 30,
        requiredClaims: ['iss', 'sub', 'exp', 'aud'],
      });
      if (protectedHeader.typ !== undefined && protectedHeader.typ !== 'JWT') return { ok: false, reason: 'typ' };
      const m = APP_CHECK_ISSUER_RE.exec(payload.iss ?? '');
      if (!m) return { ok: false, reason: 'iss' };
      const aud = Array.isArray(payload.aud) ? payload.aud : [payload.aud];
      if (!aud.includes(`projects/${m[1]}`)) return { ok: false, reason: 'aud_number' };
      if (this.appIds.length > 0 && !this.appIds.includes(payload.sub ?? '')) return { ok: false, reason: 'app_id' };
      return { ok: true, jti: typeof payload.jti === 'string' ? payload.jti : undefined, exp: payload.exp };
    } catch (e) {
      // Si no se pudo descargar el JWKS, es un fallo de Google (502), no del cliente.
      if (
        e instanceof joseErrors.JWKSTimeout ||
        e instanceof joseErrors.JWKSInvalid ||
        (e instanceof Error && /Expected 200 OK/.test(e.message)) ||
        e instanceof TypeError
      ) {
        throw new UpstreamError('app-check-jwks', e.name);
      }
      if (e instanceof joseErrors.JOSEError) return { ok: false, reason: e.code };
      return { ok: false, reason: 'malformed' };
    }
  }
}

/**
 * (b) Play Integrity directo (`decodeIntegrityToken`): necesita el paquete `com.google.play.integrity` en el
 * cliente, que no está aprobado (Q-AWS-4 eligió la (a)). Declarada para poder cambiar de variante sin tocar
 * los handlers; no concede nunca.
 */
export class PlayIntegrityVerifier implements IntegrityVerifier {
  readonly variant = 'play-integrity' as const;
  verify(): Promise<IntegrityVerdict> {
    return Promise.reject(new Error('Play Integrity directo (Q-AWS-4 b) no está implementado.'));
  }
}

/** Prefijo de los tokens del verificador falso (solo dev; plan §4). */
export const FAKE_INTEGRITY_PREFIX = 'fake-ok-';

/** Verificador falso (solo dev con AF_FAKE_PLAY_VERIFIER=1): `fake-ok-<único>` pasa; el resto va al real. */
export class DevIntegrityVerifier implements IntegrityVerifier {
  readonly variant = 'fake' as const;
  private readonly real: IntegrityVerifier;
  private readonly now: () => number;

  constructor(real: IntegrityVerifier, now: () => number = Date.now) {
    this.real = real;
    this.now = now;
  }

  verify(token: string): Promise<IntegrityVerdict> {
    if (token.startsWith(FAKE_INTEGRITY_PREFIX) && token.length > FAKE_INTEGRITY_PREFIX.length) {
      return Promise.resolve({ ok: true, exp: epochS(this.now()) + 3600 });
    }
    return this.real.verify(token);
  }
}

export function createIntegrityVerifier(opts: {
  variant: IntegrityVariant;
  projectId: string;
  appIds?: readonly string[];
  fake: boolean;
}): IntegrityVerifier {
  const real: IntegrityVerifier =
    opts.variant === 'play-integrity'
      ? new PlayIntegrityVerifier()
      : new AppCheckVerifier({ projectId: opts.projectId, appIds: opts.appIds });
  return opts.fake ? new DevIntegrityVerifier(real) : real;
}

export interface IntegrityDeps {
  readonly ddb: DynamoDBDocumentClient;
  readonly verifier: IntegrityVerifier;
  readonly integrityTable: string;
  readonly rateLimitsTable: string;
  readonly envName: string;
  readonly now: () => number;
}

/**
 * Exige integridad para `uid`. Si hay un veredicto bueno de menos de 24 h en la caché, pasa sin mirar el
 * token. Si no: verifica el token, lo marca como usado (un token de uso limitado no se puede repetir) y
 * guarda el veredicto. Falta o fallo → 403 integrity_failed. Los fallos no se guardan en la caché.
 */
export async function requireIntegrity(deps: IntegrityDeps, uid: string, token: string | undefined): Promise<void> {
  const nowMs = deps.now();
  const nowS = epochS(nowMs);
  const cached = await deps.ddb.send(
    new GetCommand({ TableName: deps.integrityTable, Key: { uid }, ConsistentRead: true }),
  );
  const item = cached.Item as { verdict?: string; ttl?: number } | undefined;
  if (item?.verdict === 'ok' && typeof item.ttl === 'number' && item.ttl > nowS) return;

  if (!token) throw integrityFailed('Falta el token de integridad.');
  if (token.length > 8192) throw integrityFailed('Token de integridad no válido.');
  metric('IntegrityChecks', deps.envName);
  let verdict: IntegrityVerdict;
  try {
    verdict = await deps.verifier.verify(token);
  } catch (e) {
    if (e instanceof UpstreamError) throw e;
    log.error('integrity_verifier_error', { variant: deps.verifier.variant, err: e instanceof Error ? e.message : 'error' });
    throw integrityFailed('No se pudo comprobar la integridad.');
  }
  if (!verdict.ok) {
    log.info('integrity_rejected', { variant: deps.verifier.variant, reason: verdict.reason });
    throw integrityFailed('Token de integridad no válido.');
  }
  const exp = verdict.exp ?? nowS + 3600;
  const replayKey = `appcheck#${sha256hex(verdict.jti ? `jti:${verdict.jti}` : `tok:${token}`)}`;
  if (!(await consumeOnce(deps.ddb, deps.rateLimitsTable, replayKey, exp + 300))) {
    log.info('integrity_rejected', { variant: deps.verifier.variant, reason: 'replay' });
    throw integrityFailed('Token de integridad ya usado.');
  }
  // F2c: la caché se escribe en una transacción que exige que el UID no esté marcado como borrado, para no dejar
  // una fila con el uid en claro si el borrado ocurre a la vez. Si lo está, el token sigue siendo bueno (el borrado
  // repetido por la app debe poder pasar), pero no se guarda nada: quien llama decide (verify y /code dan 401).
  try {
    await deps.ddb.send(
      new TransactWriteCommand({
        TransactItems: [
          {
            Put: {
              TableName: deps.integrityTable,
              Item: { uid, verdict: 'ok', variant: deps.verifier.variant, checked_at: nowMs, ttl: nowS + VERDICT_TTL_S },
            },
          },
          notDeletedCheck(deps.rateLimitsTable, uidHash(uid), nowMs),
        ],
      }),
    );
  } catch (e) {
    if (cancellationCodes(e)?.[1] !== 'ConditionalCheckFailed') throw e;
    log.info('integrity_not_cached_deleted', { uid_hash: uidHash(uid) });
  }
}
