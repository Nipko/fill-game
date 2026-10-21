// Piezas reales de las Lambdas, montadas desde las variables de entorno del stack. Los handlers las reciben
// por inyección (createHandler(deps)); los tests pasan falsos y nunca llegan aquí con red.
import type { DynamoDBDocumentClient } from '@aws-sdk/lib-dynamodb';
import { ddbClient } from './ddb.ts';
import type { Env } from './env.ts';
import { fakeVerifierEnabled, requireEnv } from './env.ts';
import type { FirebaseAdmin } from './firebaseAdmin.ts';
import { IDENTITY_TOOLKIT_SCOPE, IdentityToolkitAdmin } from './firebaseAdmin.ts';
import { ServiceAccountTokenProvider } from './googleAuth.ts';
import type { IntegrityVerifier } from './integrity.ts';
import { createIntegrityVerifier } from './integrity.ts';
import type { PlayApi } from './playApi.ts';
import { ANDROIDPUBLISHER_SCOPE, DevPlayApi, FakePlayApi, GooglePlayApi } from './playApi.ts';
import type { SecretSource } from './secrets.ts';
import { SecretsManagerSource } from './secrets.ts';
import { ConfigError } from './errors.ts';

let secrets: SecretSource | undefined;
const sharedSecrets = (): SecretSource => (secrets ??= new SecretsManagerSource());

export const realDdb = (): DynamoDBDocumentClient => ddbClient();

/** Play Developer API con la cuenta de servicio de AF_SECRET_PLAY; en dev con la bandera, los `fake-*` al falso. */
export function realPlayApi(env: Env): PlayApi {
  const packageName = env.AF_PACKAGE_NAME ?? '';
  const fake = fakeVerifierEnabled(env);
  if (!packageName && !fake) throw new ConfigError('Falta AF_PACKAGE_NAME (en prod, -c packageName tras el dictamen de marca).');
  const real = new GooglePlayApi({
    packageName,
    auth: new ServiceAccountTokenProvider({
      secrets: sharedSecrets(),
      secretArn: requireEnv(env, 'AF_SECRET_PLAY'),
      scope: ANDROIDPUBLISHER_SCOPE,
    }),
  });
  return fake ? new DevPlayApi(new FakePlayApi(), real) : real;
}

export function realFirebaseAdmin(env: Env): FirebaseAdmin {
  return new IdentityToolkitAdmin({
    projectId: requireEnv(env, 'AF_FIREBASE_PROJECT_ID'),
    auth: new ServiceAccountTokenProvider({
      secrets: sharedSecrets(),
      secretArn: requireEnv(env, 'AF_SECRET_FIREBASE_ADMIN'),
      scope: IDENTITY_TOOLKIT_SCOPE,
    }),
  });
}

export function realIntegrityVerifier(env: Env): IntegrityVerifier {
  const appIds = (env.AF_APPCHECK_APP_IDS ?? '').split(',').map((s) => s.trim()).filter(Boolean);
  // Como la síntesis (lib/config.ts): en prod, sin la lista de apps no se acepta ningún token (falla cerrada).
  if (env.AF_ENV !== 'dev' && appIds.length === 0) {
    throw new ConfigError('Falta AF_APPCHECK_APP_IDS (en prod, -c firebaseAppIds).');
  }
  return createIntegrityVerifier({
    variant: 'app-check',
    projectId: requireEnv(env, 'AF_FIREBASE_PROJECT_ID'),
    appIds,
    fake: fakeVerifierEnabled(env),
  });
}

export const realSecrets = sharedSecrets;

/** Memoiza la construcción de las dependencias en la primera invocación (y la reintenta si falla). */
export function lazy<T>(make: () => T): () => T {
  let v: T | undefined;
  return () => (v ??= make());
}
