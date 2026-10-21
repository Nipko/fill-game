// Configuración por entorno del backend de Amazon Fill (D-035; docs/dev/plan-backend-aws.md §2 y §4).
//
// Todo lo que identifica una cuenta o a una persona llega por contexto (`-c clave=valor`) y nunca se
// guarda en el repositorio: el id de la cuenta de AWS, el correo de las alarmas y el dominio con su zona.
// El id del proyecto de Firebase es público por diseño (va dentro del APK) y tiene un valor por defecto.

export type EnvName = 'dev' | 'prod';

export interface DomainConfig {
  /** Dominio raíz de la web (p. ej. el canónico tras el dictamen de marca). La CDN va en `cdn.<dominio>` y la API en `api.<dominio>`. */
  readonly name: string;
  /** Id de la zona alojada de Route 53 (sin `fromLookup`: se pasa por contexto). */
  readonly hostedZoneId: string;
}

export interface ThrottleConfig {
  readonly rateLimit: number;
  readonly burstLimit: number;
}

export interface AfConfig {
  readonly envName: EnvName;
  readonly isProd: boolean;
  /** Prefijo interno de recursos: `af-<env>` (nunca la marca). */
  readonly prefix: string;
  /** Prefijo de los ids de stack: `AfDev` o `AfProd`. */
  readonly stackPrefix: string;
  /** Región de datos, API y funciones (Q-AWS-5). */
  readonly region: 'sa-east-1';
  /** Región de lo global: ACM y WAF de CloudFront, y Budgets (Q-AWS-5). */
  readonly globalRegion: 'us-east-1';
  /** Cuenta de AWS, solo por contexto. Sin ella los stacks salen agnósticos de cuenta. */
  readonly account?: string;
  /** Dominio propio, opcional (Q-AWS-9: en dev, los dominios de AWS). */
  readonly domain?: DomainConfig;
  /** Id del proyecto de Firebase: emisor y audiencia del JWT de Firebase Auth. */
  readonly firebaseProjectId: string;
  /** Correo para alarmas y avisos de presupuesto, solo por contexto. */
  readonly alertEmail?: string;
  /** Presupuesto mensual en USD (Q-AWS-6). */
  readonly budgetUsd: number;
  /** Umbrales de aviso del presupuesto, en % (Q-AWS-6). */
  readonly budgetThresholdsPct: readonly number[];
  /** Verificador falso de Play (plan §4, hallazgo 2): solo puede existir en dev. */
  readonly fakePlayVerifier: boolean;
  /** Límite por defecto de la API (plan §2). */
  readonly apiThrottle: ThrottleConfig;
  /** Límite de las rutas de borrado (plan §2: 2 rps en `/v1/account/delete-*`). */
  readonly deleteThrottle: ThrottleConfig;
  /** Retención de logs en días (plan §2 y §5). */
  readonly logRetentionDays: 30;
  /** Umbral diario de la alarma de la cuota de integridad: 70 % de 10.000 (plan §1; F5-4). */
  readonly integrityDailyAlarm: number;
}

/** Valores por defecto del proyecto de Firebase por entorno (08, cabecera y §15.3 #2). */
const DEFAULT_FIREBASE_PROJECT: Record<EnvName, string> = {
  dev: 'amazon-fill-dev',
  prod: 'amazon-fill-prod',
};

const ACCOUNT_RE = /^\d{12}$/;
const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const DOMAIN_RE = /^(?=.{1,253}$)([a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z]{2,63}$/;
const ZONE_RE = /^Z[A-Z0-9]{5,32}$/;
const FIREBASE_PROJECT_RE = /^[a-z][a-z0-9-]{4,28}[a-z0-9]$/;

/** Lector mínimo del contexto de CDK (para poder probar `loadConfig` sin una App). */
export interface ContextReader {
  tryGetContext(key: string): unknown;
}

function str(ctx: ContextReader, key: string): string | undefined {
  const v = ctx.tryGetContext(key);
  if (v === undefined || v === null || v === '') return undefined;
  if (typeof v !== 'string') throw new Error(`El contexto "${key}" debe ser texto.`);
  return v.trim();
}

/**
 * Lee la configuración del entorno desde el contexto de CDK.
 *
 * Contexto admitido: `env` (obligatorio: dev | prod), `account`, `alertEmail`, `firebaseProjectId`,
 * `domain` y `hostedZoneId` (los dos o ninguno).
 */
export function loadConfig(ctx: ContextReader): AfConfig {
  const envRaw = str(ctx, 'env');
  if (envRaw !== 'dev' && envRaw !== 'prod') {
    throw new Error('Falta el entorno: usa `-c env=dev` o `-c env=prod`.');
  }
  const envName: EnvName = envRaw;
  const isProd = envName === 'prod';

  const account = str(ctx, 'account');
  if (account !== undefined && !ACCOUNT_RE.test(account)) {
    throw new Error('El contexto "account" debe ser un id de cuenta de AWS de 12 dígitos.');
  }

  const alertEmail = str(ctx, 'alertEmail');
  if (alertEmail !== undefined && !EMAIL_RE.test(alertEmail)) {
    throw new Error('El contexto "alertEmail" no parece un correo.');
  }

  const firebaseProjectId = str(ctx, 'firebaseProjectId') ?? DEFAULT_FIREBASE_PROJECT[envName];
  if (!FIREBASE_PROJECT_RE.test(firebaseProjectId)) {
    throw new Error('El contexto "firebaseProjectId" no es un id de proyecto de Firebase válido.');
  }

  const domainName = str(ctx, 'domain')?.toLowerCase();
  const hostedZoneId = str(ctx, 'hostedZoneId');
  let domain: DomainConfig | undefined;
  if (domainName !== undefined || hostedZoneId !== undefined) {
    if (domainName === undefined || hostedZoneId === undefined) {
      throw new Error('El dominio necesita los dos contextos: "domain" y "hostedZoneId".');
    }
    if (!DOMAIN_RE.test(domainName)) throw new Error('El contexto "domain" no es un dominio válido.');
    if (!ZONE_RE.test(hostedZoneId)) throw new Error('El contexto "hostedZoneId" no es un id de zona de Route 53.');
    domain = { name: domainName, hostedZoneId };
  }

  const fakePlayVerifier = !isProd;
  if (isProd && fakePlayVerifier) {
    throw new Error('El verificador falso de Play no puede existir en prod.');
  }

  return {
    envName,
    isProd,
    prefix: `af-${envName}`,
    stackPrefix: isProd ? 'AfProd' : 'AfDev',
    region: 'sa-east-1',
    globalRegion: 'us-east-1',
    account,
    domain,
    firebaseProjectId,
    alertEmail,
    budgetUsd: isProd ? 50 : 10,
    budgetThresholdsPct: [50, 80, 100],
    fakePlayVerifier,
    apiThrottle: { rateLimit: 50, burstLimit: 100 },
    deleteThrottle: { rateLimit: 2, burstLimit: 5 },
    logRetentionDays: 30,
    integrityDailyAlarm: 7000,
  };
}
