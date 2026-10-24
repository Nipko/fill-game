// Monta los stacks de un entorno. Lo usan bin/app.ts y los tests (sin credenciales ni lookups).
import { Tags, Validations } from 'aws-cdk-lib';
import type { App, Environment, Stack } from 'aws-cdk-lib';
import { AwsSolutionsChecks } from 'cdk-nag';
import { ApiStack } from './api-stack.ts';
import { CdnStack } from './cdn-stack.ts';
import type { AfConfig } from './config.ts';
import { DataStack } from './data-stack.ts';
import { GlobalStack } from './global-stack.ts';
import { OpsStack } from './ops-stack.ts';
import { WebStack } from './web-stack.ts';

/** Stacks de un entorno. Con `webOnly` (D-044) solo existen `global` y `web`. */
export interface AfStacks {
  readonly global: GlobalStack;
  readonly web: WebStack;
  readonly data?: DataStack;
  readonly api?: ApiStack;
  readonly cdn?: CdnStack;
  readonly ops?: OpsStack;
}

export function buildApp(app: App, config: AfConfig, opts: { nag?: boolean } = {}): AfStacks {
  // Sin cuenta por contexto, los stacks salen agnósticos de cuenta (nunca se lee de las credenciales).
  const env = (region: string): Environment => ({ account: config.account, region });
  const regional = env(config.region);
  const p = config.stackPrefix;
  // Solo con dominio hay referencias entre regiones (certificado de us-east-1 en CloudFront).
  const crossRegionReferences = config.domain !== undefined;

  const global = new GlobalStack(app, `${p}Global`, {
    config,
    env: env(config.globalRegion),
    crossRegionReferences,
    description: `${config.prefix}: presupuesto y certificado de CloudFront (us-east-1).`,
  });

  let stacks: AfStacks;
  if (config.webOnly) {
    // Solo la web (D-044): sin Data, Api, Cdn ni Ops. La web no depende de la API: el borrado web queda desactivado
    // hasta que se despliegue el entorno completo (sin webOnly), que añade la ruta y lo activa en config.json.
    const web = new WebStack(app, `${p}Web`, {
      config,
      env: regional,
      certificate: global.cloudFrontCertificate,
      crossRegionReferences,
      description: `${config.prefix}: web estática (S3 + CloudFront).`,
    });
    stacks = { global, web };
  } else {
    const data = new DataStack(app, `${p}Data`, {
      config,
      env: regional,
      terminationProtection: config.isProd,
      description: `${config.prefix}: tablas de DynamoDB.`,
    });
    const api = new ApiStack(app, `${p}Api`, {
      config,
      env: regional,
      tables: data.tables,
      terminationProtection: config.isProd,
      description: `${config.prefix}: HTTP API v1, Lambdas, secretos y job diario.`,
    });
    const web = new WebStack(app, `${p}Web`, {
      config,
      env: regional,
      apiId: api.api.apiId,
      certificate: global.cloudFrontCertificate,
      crossRegionReferences,
      description: `${config.prefix}: web estática (S3 + CloudFront).`,
    });
    const cdn = new CdnStack(app, `${p}Cdn`, {
      config,
      env: regional,
      certificate: global.cdnCertificate,
      crossRegionReferences,
      description: `${config.prefix}: CDN del contenido remoto (S3 + CloudFront).`,
    });
    const ops = new OpsStack(app, `${p}Ops`, {
      config,
      env: regional,
      api,
      tables: data.tables,
      description: `${config.prefix}: alarmas y avisos.`,
    });
    stacks = { global, data, api, web, cdn, ops };
  }

  for (const s of Object.values(stacks) as Stack[]) {
    Tags.of(s).add('af:project', 'amazon-fill');
    Tags.of(s).add('af:env', config.envName);
  }

  if (opts.nag ?? true) {
    // cdk-nag 3 en modo error: cualquier hallazgo sin reconocer hace fallar la síntesis.
    Validations.of(app).addPlugins(new AwsSolutionsChecks(app, { verbose: true, writeSuppressionsToCloudFormation: true }));
  }
  return stacks;
}
