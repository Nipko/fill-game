// Lambda del backend: Node 24 en arm64, empaquetada con esbuild, con rol propio y mínimo,
// log group con 30 días de retención y logs en JSON (plan §2 y §5).
import { Duration, RemovalPolicy } from 'aws-cdk-lib';
import { PolicyStatement, Role, ServicePrincipal } from 'aws-cdk-lib/aws-iam';
import type { ITableV2 } from 'aws-cdk-lib/aws-dynamodb';
import { ApplicationLogLevel, Architecture, LoggingFormat, Runtime } from 'aws-cdk-lib/aws-lambda';
import { NodejsFunction, OutputFormat } from 'aws-cdk-lib/aws-lambda-nodejs';
import { LogGroup, RetentionDays } from 'aws-cdk-lib/aws-logs';
import type { ISecret } from 'aws-cdk-lib/aws-secretsmanager';
import { Construct } from 'constructs';
import * as path from 'node:path';
import type { AfConfig } from '../config.ts';
import { FUNCTIONS_DIR, INFRA_ROOT, LOCK_FILE } from '../paths.ts';

export interface AfFunctionProps {
  readonly config: AfConfig;
  /** Carpeta de `functions/` con su `handler.ts` (p. ej. `verify-purchase`). */
  readonly name: string;
  readonly description: string;
  readonly timeout?: Duration;
  readonly environment?: Record<string, string>;
  /** Si la función verifica Play o la integridad: solo entonces recibe el verificador falso en dev. */
  readonly usesPlayVerifier?: boolean;
}

export class AfFunction extends Construct {
  readonly fn: NodejsFunction;
  readonly role: Role;
  readonly logGroup: LogGroup;

  constructor(scope: Construct, id: string, props: AfFunctionProps) {
    super(scope, id);
    const { config } = props;
    const functionName = `${config.prefix}-${props.name}`;

    this.logGroup = new LogGroup(this, 'Logs', {
      logGroupName: `/aws/lambda/${functionName}`,
      retention: RetentionDays.ONE_MONTH,
      removalPolicy: config.isProd ? RemovalPolicy.RETAIN : RemovalPolicy.DESTROY,
    });

    // Rol propio sin políticas gestionadas: solo escribir en su log group; el resto, con `grant*` explícitos.
    this.role = new Role(this, 'Role', {
      assumedBy: new ServicePrincipal('lambda.amazonaws.com'),
      description: `Rol de ${functionName}: solo sus tablas, sus secretos y su log group.`,
    });
    this.role.addToPolicy(
      new PolicyStatement({
        actions: ['logs:CreateLogStream', 'logs:PutLogEvents'],
        resources: [this.logGroup.logGroupArn],
      }),
    );

    const environment: Record<string, string> = {
      AF_ENV: config.envName,
      AF_FIREBASE_PROJECT_ID: config.firebaseProjectId,
      NODE_OPTIONS: '--enable-source-maps',
      ...props.environment,
    };
    // Verificador falso de Play e integridad (plan §4): solo en dev. Un test de stacks lo comprueba.
    if (config.fakePlayVerifier && props.usesPlayVerifier) environment.AF_FAKE_PLAY_VERIFIER = '1';

    this.fn = new NodejsFunction(this, 'Function', {
      functionName,
      description: props.description,
      entry: path.join(FUNCTIONS_DIR, props.name, 'handler.ts'),
      handler: 'handler',
      projectRoot: INFRA_ROOT,
      depsLockFilePath: LOCK_FILE,
      runtime: Runtime.NODEJS_24_X,
      architecture: Architecture.ARM_64,
      memorySize: 256,
      timeout: props.timeout ?? Duration.seconds(10),
      role: this.role,
      logGroup: this.logGroup,
      loggingFormat: LoggingFormat.JSON,
      applicationLogLevelV2: ApplicationLogLevel.INFO,
      environment,
      bundling: {
        format: OutputFormat.ESM,
        target: 'node24',
        minify: true,
        sourceMap: true,
        // Los clientes del SDK v3 se empaquetan (versión fijada por el lockfile), no los del runtime.
        externalModules: [],
        mainFields: ['module', 'main'],
        banner: "import { createRequire } from 'module';const require = createRequire(import.meta.url);",
      },
    });
  }

  /** Permite acciones concretas de DynamoDB sobre una tabla (solo la tabla, no sus índices). */
  grantTable(table: ITableV2, actions: string[]): void {
    this.role.addToPolicy(new PolicyStatement({ actions, resources: [table.tableArn] }));
  }

  /** Permite acciones concretas (p. ej. Query) sobre un índice concreto, no sobre la tabla. */
  grantIndex(table: ITableV2, indexName: string, actions: string[]): void {
    this.role.addToPolicy(new PolicyStatement({ actions, resources: [`${table.tableArn}/index/${indexName}`] }));
  }

  /** Lectura de un secreto (solo `GetSecretValue` de ese ARN). */
  grantSecret(secret: ISecret): void {
    this.role.addToPolicy(
      new PolicyStatement({ actions: ['secretsmanager:GetSecretValue'], resources: [secret.secretArn] }),
    );
  }
}
