// Configuración por entorno del backend de Amazon Fill (D-035; docs/dev/plan-backend-aws.md §2 y §4).
//
// Todo lo que identifica una cuenta o a una persona llega por contexto (`-c clave=valor`) y nunca se
// guarda en el repositorio: el id de la cuenta de AWS, el correo de las alarmas y el dominio con su zona.
// El id del proyecto de Firebase es público por diseño (va dentro del APK) y tiene un valor por defecto.

export type EnvName = 'dev' | 'prod';

export interface DomainConfig {
  /** Dominio raíz de la web (`<dominio>` y `www.<dominio>`). La CDN puede ir en `cdn.<dominio>` y la API en `api.<dominio>`. */
  readonly name: string;
  /**
   * Id de la zona alojada de Route 53 (sin `fromLookup`: se pasa por contexto). Sin él, el DNS es **externo**
   * (p. ej. Cloudflare, D-044): certificados validados por CNAME que crea el dueño y ningún registro DNS en AWS.
   */
  readonly hostedZoneId?: string;
  /** `cdn.<dominio>` en la CDN. Con Route 53, siempre; con DNS externo, solo con `-c domainExtras=cdn`. */
  readonly cdn: boolean;
  /** `api.<dominio>` en la API. Con Route 53, siempre; con DNS externo, solo con `-c domainExtras=api`. */
  readonly api: boolean;
}

/** Nombres extra que admite `-c domainExtras` con DNS externo. */
export const DOMAIN_EXTRAS = ['cdn', 'api'] as const;

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
  /**
   * Solo la web (D-044): monta únicamente `<Prefijo>Global` y `<Prefijo>Web`, sin Data, Api, Cdn ni Ops, para publicar
   * la web antes de que exista el resto del backend. Sin API, el borrado por web queda desactivado (`config.json`).
   */
  readonly webOnly: boolean;
  /** Id del proyecto de Firebase: emisor y audiencia del JWT de Firebase Auth. */
  readonly firebaseProjectId: string;
  /**
   * Ids de las apps de Firebase (`1:<número>:android:<hash>`) admitidos en el token de App Check (Q-AWS-4).
   * Obligatorios en prod (F2c: solo las apps Android registradas emiten tokens válidos); en dev, opcionales
   * (sin ellos vale cualquier app del proyecto).
   */
  readonly firebaseAppIds: readonly string[];
  /**
   * Paquete de la app en Google Play (applicationId). En dev, el provisional `com.amazonfill.dev`; en prod solo por
   * contexto (`-c packageName`) tras el dictamen de marca (regla 6 de CLAUDE.md) y nunca el provisional.
   */
  readonly packageName?: string;
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
const FIREBASE_APP_ID_RE = /^1:\d{1,20}:android:[0-9a-f]{6,64}$/;
const PACKAGE_RE = /^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]*)+$/;
/** applicationId provisional de los builds de prueba (CLAUDE.md, regla 6): nunca en prod. */
export const DEV_PACKAGE_NAME = 'com.amazonfill.dev';

/** Lector mínimo del contexto de CDK (para poder probar `loadConfig` sin una App). */
export interface ContextReader {
  tryGetContext(key: string): unknown;
}

function bool(ctx: ContextReader, key: string): boolean {
  const v = ctx.tryGetContext(key);
  if (v === undefined || v === null || v === '' || v === false || v === 'false') return false;
  if (v === true || v === 'true') return true;
  throw new Error(`El contexto "${key}" debe ser true o false.`);
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
 * `firebaseAppIds` (separados por comas; obligatorio en prod salvo con `webOnly`), `packageName`, `domain`,
 * `hostedZoneId` (solo con `domain`: DNS en Route 53; sin él, DNS externo), `domainExtras` (`cdn`, `api`; solo con
 * DNS externo) y `webOnly` (true: solo los stacks de la web).
 */
export function loadConfig(ctx: ContextReader): AfConfig {
  const envRaw = str(ctx, 'env');
  if (envRaw !== 'dev' && envRaw !== 'prod') {
    throw new Error('Falta el entorno: usa `-c env=dev` o `-c env=prod`.');
  }
  const envName: EnvName = envRaw;
  const isProd = envName === 'prod';
  const webOnly = bool(ctx, 'webOnly');

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

  const firebaseAppIds = (str(ctx, 'firebaseAppIds') ?? '').split(',').map((a) => a.trim()).filter(Boolean);
  for (const a of firebaseAppIds) {
    if (!FIREBASE_APP_ID_RE.test(a)) throw new Error('El contexto "firebaseAppIds" debe ser una lista de ids de app Android de Firebase.');
  }
  if (isProd && firebaseAppIds.length === 0 && !webOnly) {
    // Sin la lista, cualquier app del proyecto de Firebase (p. ej. una web o una de pruebas) emitiría tokens de
    // App Check válidos para la API de prod.
    throw new Error('En prod falta el contexto "firebaseAppIds": los ids de las apps Android de Firebase (`1:<número>:android:<hash>`).');
  }

  const packageName = str(ctx, 'packageName') ?? (isProd ? undefined : DEV_PACKAGE_NAME);
  if (packageName !== undefined) {
    if (!PACKAGE_RE.test(packageName)) throw new Error('El contexto "packageName" no es un applicationId válido.');
    if (isProd && packageName === DEV_PACKAGE_NAME) {
      throw new Error(`"${DEV_PACKAGE_NAME}" es provisional y no puede llegar a prod (CLAUDE.md, regla 6).`);
    }
  }

  const domainName = str(ctx, 'domain')?.toLowerCase();
  const hostedZoneId = str(ctx, 'hostedZoneId');
  const extrasRaw = (str(ctx, 'domainExtras') ?? '').split(',').map((e) => e.trim().toLowerCase()).filter(Boolean);
  let domain: DomainConfig | undefined;
  if (domainName === undefined) {
    if (hostedZoneId !== undefined) throw new Error('El contexto "hostedZoneId" necesita también "domain".');
    if (extrasRaw.length > 0) throw new Error('El contexto "domainExtras" necesita también "domain".');
  } else {
    if (!DOMAIN_RE.test(domainName)) throw new Error('El contexto "domain" no es un dominio válido.');
    for (const e of extrasRaw) {
      if (!(DOMAIN_EXTRAS as readonly string[]).includes(e)) {
        throw new Error(`El contexto "domainExtras" solo admite ${DOMAIN_EXTRAS.join(' y ')} (separados por comas).`);
      }
    }
    if (hostedZoneId !== undefined) {
      // Route 53: la vía de siempre, con la web, cdn. y api. en el dominio.
      if (!ZONE_RE.test(hostedZoneId)) throw new Error('El contexto "hostedZoneId" no es un id de zona de Route 53.');
      if (extrasRaw.length > 0) throw new Error('Con "hostedZoneId" (Route 53) cdn. y api. van siempre en el dominio: quita "domainExtras".');
      domain = { name: domainName, hostedZoneId, cdn: true, api: true };
    } else {
      // DNS externo (Cloudflare, D-044): solo la web, salvo que se pidan cdn. o api.
      domain = { name: domainName, cdn: extrasRaw.includes('cdn'), api: extrasRaw.includes('api') };
    }
    if (webOnly && (hostedZoneId !== undefined || domain.cdn || domain.api)) {
      throw new Error('Con "webOnly" no hay CDN ni API: usa el dominio con DNS externo y sin "domainExtras".');
    }
  }

  // El verificador falso de Play solo existe en dev (plan §4); un test de stacks lo comprueba en prod.
  const fakePlayVerifier = !isProd;

  return {
    envName,
    isProd,
    prefix: `af-${envName}`,
    stackPrefix: isProd ? 'AfProd' : 'AfDev',
    region: 'sa-east-1',
    globalRegion: 'us-east-1',
    account,
    domain,
    webOnly,
    firebaseProjectId,
    firebaseAppIds,
    packageName,
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
