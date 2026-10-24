// F2-2 · api-stack: HTTP API con autorizador JWT de Firebase, rutas /v1/... del contrato v1,
// las 5 Lambdas de esqueleto (501), límites por ruta, secretos vacíos y el job diario.
import { CfnOutput, Duration, Fn, RemovalPolicy, Stack } from 'aws-cdk-lib';
import type { StackProps } from 'aws-cdk-lib';
import { AccessLogFormat } from 'aws-cdk-lib/aws-apigateway';
import type {
  CfnStage} from 'aws-cdk-lib/aws-apigatewayv2';
import {
  ApiMapping,
  CfnRoute,
  DomainName,
  HttpApi,
  HttpMethod,
  HttpNoneAuthorizer,
  HttpStage,
  LogGroupLogDestination,
  SecurityPolicy,
} from 'aws-cdk-lib/aws-apigatewayv2';
import { HttpJwtAuthorizer } from 'aws-cdk-lib/aws-apigatewayv2-authorizers';
import { HttpLambdaIntegration } from 'aws-cdk-lib/aws-apigatewayv2-integrations';
import { Certificate, CertificateValidation } from 'aws-cdk-lib/aws-certificatemanager';
import type { CfnFunction } from 'aws-cdk-lib/aws-lambda';
import { LogGroup, RetentionDays } from 'aws-cdk-lib/aws-logs';
import { ARecord, AaaaRecord, HostedZone, RecordTarget } from 'aws-cdk-lib/aws-route53';
import { ApiGatewayv2DomainProperties } from 'aws-cdk-lib/aws-route53-targets';
import { Schedule, ScheduleExpression } from 'aws-cdk-lib/aws-scheduler';
import { LambdaInvoke } from 'aws-cdk-lib/aws-scheduler-targets';
import { Secret } from 'aws-cdk-lib/aws-secretsmanager';
import { Queue, QueueEncryption } from 'aws-cdk-lib/aws-sqs';
import type { Construct } from 'constructs';
import type { AfConfig } from './config.ts';
import { externalDnsOutputs } from './global-stack.ts';
import type { FunctionName } from './contract.ts';
import { loadContractIndex, routeKey } from './contract.ts';
import { AfFunction } from './constructs/af-function.ts';
import type { AfTables } from './data-stack.ts';
import { BY_UID } from './data-stack.ts';
import { ack } from './nag.ts';

export interface ApiStackProps extends StackProps {
  readonly config: AfConfig;
  readonly tables: AfTables;
}

/** Secretos que crea el CDK con un valor aleatorio de relleno; el dueño carga el real con put-secret-value. */
export interface AfSecrets {
  /** Clave JSON de la cuenta de servicio de Google con permiso de pedidos en Play Console (Q-AWS-7). */
  readonly playServiceAccount: Secret;
  /** Clave JSON de la cuenta de servicio de Firebase Admin, para borrar el usuario de Auth (08 §11). */
  readonly firebaseAdmin: Secret;
  /** Clave de los callbacks S2S de MAX (08 §10.1) [NV: formato]. */
  readonly maxSsvKey: Secret;
}

const DDB_GET = ['dynamodb:GetItem'];
const DDB_BATCH_GET = ['dynamodb:BatchGetItem'];
const DDB_QUERY = ['dynamodb:Query'];
const DDB_PUT = ['dynamodb:PutItem'];
const DDB_UPDATE = ['dynamodb:UpdateItem'];
const DDB_DELETE = ['dynamodb:DeleteItem'];
/** ConditionCheck de TransactWriteItems (F2c: la marca de cuenta borrada, atómica con la escritura). */
const DDB_CONDITION_CHECK = ['dynamodb:ConditionCheckItem'];

export class ApiStack extends Stack {
  readonly api: HttpApi;
  readonly stage: HttpStage;
  readonly functions: Record<FunctionName, AfFunction>;
  readonly secrets: AfSecrets;
  readonly jobDlq: Queue;
  /** Origen del API (`https://<id>.execute-api.<región>.amazonaws.com`), sin barra final. */
  readonly apiEndpoint: string;

  constructor(scope: Construct, id: string, props: ApiStackProps) {
    super(scope, id, props);
    const { config, tables } = props;
    const contract = loadContractIndex();

    // ---- Secretos (plan §2: el CDK los crea; el dueño hace put-secret-value) ----
    const secret = (logicalId: string, name: string, description: string): Secret => {
      const s = new Secret(this, logicalId, {
        secretName: `${config.prefix}/${name}`,
        description: `${description} Lo carga el dueño con put-secret-value; hasta entonces guarda un relleno aleatorio.`,
        removalPolicy: config.isProd ? RemovalPolicy.RETAIN : RemovalPolicy.DESTROY,
      });
      ack(s, 'AwsSolutions-SMG4', 'Clave externa (Google, Firebase o AppLovin) que no admite rotación automática desde AWS; la rota el dueño a mano y WIF la sustituye en la 1.1 (Q-AWS-7).');
      return s;
    };
    this.secrets = {
      playServiceAccount: secret('PlayServiceAccount', 'play-service-account', 'Cuenta de servicio de Google Play Developer API.'),
      firebaseAdmin: secret('FirebaseAdmin', 'firebase-admin-service-account', 'Cuenta de servicio de Firebase Admin.'),
      maxSsvKey: secret('MaxSsvKey', 'max-ssv-key', 'Clave de los callbacks S2S de AppLovin MAX.'),
    };

    // ---- Funciones (una por pieza de 08 §2.4) con permisos mínimos: cada acción, solo sobre su tabla o índice ----
    const tableEnv = (entries: Record<string, { tableName: string }>): Record<string, string> =>
      Object.fromEntries(Object.entries(entries).map(([k, t]) => [`AF_TABLE_${k}`, t.tableName]));
    const secretEnv = (entries: Record<string, Secret>): Record<string, string> =>
      Object.fromEntries(Object.entries(entries).map(([k, s]) => [`AF_SECRET_${k}`, s.secretArn]));
    // Paquete de la app en Play (solo con valor: en prod llega por -c packageName tras el dictamen de marca).
    const playEnv: Record<string, string> = config.packageName ? { AF_PACKAGE_NAME: config.packageName } : {};
    // Ids de las apps de Firebase admitidos en el token de App Check (-c firebaseAppIds: obligatorio en prod, opcional en dev).
    const appCheckEnv: Record<string, string> = config.firebaseAppIds.length > 0
      ? { AF_APPCHECK_APP_IDS: config.firebaseAppIds.join(',') }
      : {};

    const verifyPurchase = new AfFunction(this, 'VerifyPurchase', {
      config,
      name: 'verify-purchase',
      usesPlayVerifier: true,
      description: 'verifyPurchase (08 §10.3): verifica la compra en Play y la anota en el libro mayor.',
      environment: {
        ...tableEnv({ PURCHASES: tables.purchases, INTEGRITY: tables.integrity, RATE_LIMITS: tables.rateLimits }),
        ...secretEnv({ PLAY: this.secrets.playServiceAccount }),
        ...playEnv,
        ...appCheckEnv,
      },
    });
    // Q-IAP-4 ya integrado: reasignación condicional de no consumibles, además del alta original.
    verifyPurchase.grantTable(tables.purchases, [...DDB_GET, ...DDB_PUT, ...DDB_UPDATE]);
    verifyPurchase.grantTable(tables.integrity, [...DDB_GET, ...DDB_PUT]);
    // Anti-replay del token de integridad, límite por UID, marca de cuenta borrada (también como ConditionCheck en
    // la transacción del libro mayor) y marca once_per_account (F2c).
    verifyPurchase.grantTable(tables.rateLimits, [...DDB_GET, ...DDB_PUT, ...DDB_UPDATE, ...DDB_CONDITION_CHECK]);
    verifyPurchase.grantSecret(this.secrets.playServiceAccount);

    const restoreLedger = new AfFunction(this, 'RestoreLedger', {
      config,
      name: 'restore-ledger',
      description: 'restoreLedger (08 §11): libro mayor del UID y nonces de SSV confirmados.',
      environment: tableEnv({ PURCHASES: tables.purchases, REWARDS: tables.rewards }),
    });
    restoreLedger.grantIndex(tables.purchases, BY_UID, DDB_QUERY);
    restoreLedger.grantTable(tables.rewards, DDB_BATCH_GET);

    const rewardedSsv = new AfFunction(this, 'RewardedSsv', {
      config,
      name: 'rewarded-ssv',
      description: 'rewardedSsv (08 §10.2): callback S2S de MAX; anota el nonce de la recompensa.',
      environment: {
        ...tableEnv({ REWARDS: tables.rewards, RATE_LIMITS: tables.rateLimits }),
        ...secretEnv({ MAX_SSV: this.secrets.maxSsvKey }),
      },
    });
    rewardedSsv.grantTable(tables.rewards, DDB_PUT);
    // Límite por UID, event_id de un solo uso y marca de cuenta borrada (ConditionCheck, F2c).
    rewardedSsv.grantTable(tables.rateLimits, [...DDB_GET, ...DDB_PUT, ...DDB_UPDATE, ...DDB_CONDITION_CHECK]);
    rewardedSsv.grantSecret(this.secrets.maxSsvKey);

    const deleteAccount = new AfFunction(this, 'DeleteAccount', {
      config,
      name: 'delete-account',
      usesPlayVerifier: true,
      description: 'deleteAccount (08 §11): borrado por app y por web con código (Q-B13-21).',
      environment: {
        ...tableEnv({
          PURCHASES: tables.purchases,
          REWARDS: tables.rewards,
          INTEGRITY: tables.integrity,
          ACCOUNT_CODES: tables.accountCodes,
          RATE_LIMITS: tables.rateLimits,
          LEAGUE_PLAYERS: tables.leaguePlayers,
          LEAGUE_BOARD: tables.leagueBoard,
          LEAGUE_HISTOGRAM: tables.leagueHistogram,
          LEAGUE_NAMES: tables.leagueNames,
        }),
        ...secretEnv({ FIREBASE_ADMIN: this.secrets.firebaseAdmin }),
        ...appCheckEnv,
      },
    });
    deleteAccount.grantIndex(tables.purchases, BY_UID, DDB_QUERY);
    deleteAccount.grantTable(tables.purchases, DDB_UPDATE);
    deleteAccount.grantIndex(tables.rewards, BY_UID, DDB_QUERY);
    deleteAccount.grantTable(tables.rewards, DDB_DELETE);
    deleteAccount.grantTable(tables.integrity, [...DDB_GET, ...DDB_PUT, ...DDB_DELETE]);
    deleteAccount.grantIndex(tables.accountCodes, BY_UID, DDB_QUERY);
    deleteAccount.grantTable(tables.accountCodes, [...DDB_GET, ...DDB_PUT, ...DDB_DELETE]);
    // Límites, marca de cuenta borrada (también ConditionCheck al emitir el código) y borrar las marcas
    // once_per_account del UID (F2c).
    deleteAccount.grantTable(tables.rateLimits, [...DDB_GET, ...DDB_PUT, ...DDB_UPDATE, ...DDB_DELETE, ...DDB_CONDITION_CHECK]);
    deleteAccount.grantSecret(this.secrets.firebaseAdmin);
    deleteAccount.grantTable(tables.leaguePlayers, [...DDB_GET, ...DDB_QUERY, ...DDB_DELETE]);
    deleteAccount.grantTable(tables.leagueBoard, DDB_DELETE);
    deleteAccount.grantTable(tables.leagueHistogram, DDB_UPDATE);
    deleteAccount.grantTable(tables.leagueNames, [...DDB_GET, ...DDB_DELETE]);

    const voidedJob = new AfFunction(this, 'VoidedPurchasesJob', {
      config,
      name: 'voided-purchases-job',
      usesPlayVerifier: true,
      description: 'voidedPurchasesJob (08 §10.1): marca voided las compras anuladas en Play.',
      timeout: Duration.minutes(5),
      environment: {
        ...tableEnv({ PURCHASES: tables.purchases, JOBS: tables.jobs }),
        ...secretEnv({ PLAY: this.secrets.playServiceAccount }),
        ...playEnv,
      },
    });
    // UpdateItem para marcar voided; PutItem condicional para la lápida de los tokens aún sin verificar (F2c).
    // Q-IAP-4: lee shadow_keys de la principal antes de anular las sombras.
    voidedJob.grantTable(tables.purchases, [...DDB_GET, ...DDB_PUT, ...DDB_UPDATE]);
    voidedJob.grantTable(tables.jobs, [...DDB_GET, ...DDB_PUT]);
    voidedJob.grantSecret(this.secrets.playServiceAccount);
    const leagueEnv = tableEnv({ LEAGUE_PLAYERS: tables.leaguePlayers, LEAGUE_BOARD: tables.leagueBoard, LEAGUE_HISTOGRAM: tables.leagueHistogram, LEAGUE_NAMES: tables.leagueNames, INTEGRITY: tables.integrity, RATE_LIMITS: tables.rateLimits });
    const leagueFunction = (id: string, name: 'league-wins' | 'league-board'): AfFunction => {
      const f = new AfFunction(this, id, { config, name, usesPlayVerifier: true, description: `${name}: Liga del río (D-046; docs/dev/liga.md).`, environment: { ...leagueEnv, ...appCheckEnv } });
      f.grantTable(tables.rateLimits, [...DDB_GET, ...DDB_PUT, ...DDB_CONDITION_CHECK]);
      f.grantTable(tables.integrity, [...DDB_GET, ...DDB_PUT]);
      f.grantTable(tables.leagueNames, [...DDB_GET, ...DDB_PUT]);
      return f;
    };
    const leagueWins = leagueFunction('LeagueWins', 'league-wins');
    leagueWins.grantTable(tables.leaguePlayers, [...DDB_GET, ...DDB_PUT]);
    leagueWins.grantTable(tables.leagueBoard, [...DDB_PUT, ...DDB_DELETE]);
    leagueWins.grantTable(tables.leagueHistogram, DDB_UPDATE);
    const leagueBoard = leagueFunction('LeagueBoard', 'league-board');
    leagueBoard.grantTable(tables.leaguePlayers, DDB_GET);
    leagueBoard.grantTable(tables.leagueBoard, DDB_QUERY);
    leagueBoard.grantTable(tables.leagueHistogram, DDB_QUERY);
    this.functions = {
      'verify-purchase': verifyPurchase,
      'restore-ledger': restoreLedger,
      'rewarded-ssv': rewardedSsv,
      'delete-account': deleteAccount,
      'voided-purchases-job': voidedJob,
      'league-wins': leagueWins,
      'league-board': leagueBoard,
    };

    // ---- HTTP API ----
    this.api = new HttpApi(this, 'Api', {
      apiName: `${config.prefix}-api`,
      description: 'API v1 del backend (contrato: infra/contracts/v1).',
      createDefaultStage: false,
      // Sin CORS: la app no lo necesita y la web llama a través de su propio CloudFront (mismo origen).
    });

    const accessLogs = new LogGroup(this, 'ApiAccessLogs', {
      logGroupName: `/aws/apigateway/${config.prefix}-api`,
      retention: RetentionDays.ONE_MONTH,
      removalPolicy: config.isProd ? RemovalPolicy.RETAIN : RemovalPolicy.DESTROY,
    });
    this.stage = new HttpStage(this, 'DefaultStage', {
      httpApi: this.api,
      stageName: '$default',
      autoDeploy: true,
      throttle: config.apiThrottle,
      accessLogSettings: {
        destination: new LogGroupLogDestination(accessLogs),
        // Sin UID ni cabeceras: solo lo necesario para operar y detectar abuso (plan §5).
        format: AccessLogFormat.custom(
          JSON.stringify({
            requestId: '$context.requestId',
            ip: '$context.identity.sourceIp',
            routeKey: '$context.routeKey',
            status: '$context.status',
            latencyMs: '$context.responseLatency',
            integrationError: '$context.integrationErrorMessage',
            authorizerError: '$context.authorizer.error',
          }),
        ),
      },
    });

    // Autorizador JWT de Firebase Auth (plan §1): emisor y audiencia del proyecto del entorno.
    const firebaseJwt = new HttpJwtAuthorizer(
      'FirebaseJwt',
      `https://securetoken.google.com/${config.firebaseProjectId}`,
      {
        authorizerName: `${config.prefix}-firebase-jwt`,
        jwtAudience: [config.firebaseProjectId],
        identitySource: ['$request.header.Authorization'],
      },
    );

    const integrations = new Map<FunctionName, HttpLambdaIntegration>();
    const integrationFor = (name: FunctionName): HttpLambdaIntegration => {
      let i = integrations.get(name);
      if (!i) {
        i = new HttpLambdaIntegration(`${name}-integration`, this.functions[name].fn);
        integrations.set(name, i);
      }
      return i;
    };

    // RouteSettings es JSON libre en CloudFormation: las claves van tal cual, en PascalCase.
    const routeSettings: Record<string, { ThrottlingRateLimit: number; ThrottlingBurstLimit: number }> = {};
    for (const op of contract.operations) {
      const routes = this.api.addRoutes({
        path: op.path,
        methods: [op.method === 'GET' ? HttpMethod.GET : HttpMethod.POST],
        integration: integrationFor(op.function),
        authorizer: op.auth === 'firebase' ? firebaseJwt : new HttpNoneAuthorizer(),
      });
      if (op.auth === 'none') {
        for (const r of routes) {
          ack(
            r,
            'AwsSolutions-APIG4',
            op.function === 'rewarded-ssv'
              ? 'Callback servidor a servidor de MAX: no lleva token de Firebase; se autentica con la firma o la clave de MAX en la Lambda (08 §10.1; F2-6).'
              : 'Borrado por web (08 §11): el usuario no tiene sesión; lo protege el código aleatorio de 128 bits (no se puede adivinar), el token de 5 minutos, el límite por ruta de API Gateway y el contador por hash de código en rate_limits (Q-B13-21; F2-8).',
          );
        }
      }
      if (op.throttle === 'delete') {
        routeSettings[routeKey(op)] = {
          ThrottlingRateLimit: config.deleteThrottle.rateLimit,
          ThrottlingBurstLimit: config.deleteThrottle.burstLimit,
        };
      }
    }
    const cfnStage = this.stage.node.defaultChild as CfnStage;
    cfnStage.routeSettings = routeSettings;
    // Los ajustes por ruta exigen que la ruta exista antes que la etapa.
    for (const c of this.api.node.findAll()) {
      if (c instanceof CfnRoute) cfnStage.addResourceDependency(c);
    }

    this.apiEndpoint = `https://${this.api.apiId}.execute-api.${this.region}.${this.urlSuffix}`;
    new CfnOutput(this, 'ApiUrl', { description: 'URL base de la API v1 (execute-api).', value: this.apiEndpoint });

    // ---- Dominio propio de la API (con Route 53, siempre; con DNS externo, solo con domainExtras=api) ----
    if (config.domain?.api) {
      const zone = config.domain.hostedZoneId === undefined
        ? undefined
        : HostedZone.fromHostedZoneAttributes(this, 'Zone', {
            hostedZoneId: config.domain.hostedZoneId,
            zoneName: config.domain.name,
          });
      const apiDomain = `api.${config.domain.name}`;
      const cert = new Certificate(this, 'ApiCertificate', {
        domainName: apiDomain,
        // Sin zona (DNS externo), CDK no crea registros: el dueño crea el CNAME de validación.
        validation: CertificateValidation.fromDns(zone),
      });
      const domainName = new DomainName(this, 'ApiDomain', {
        domainName: apiDomain,
        certificate: cert,
        securityPolicy: SecurityPolicy.TLS_1_2,
      });
      new ApiMapping(this, 'ApiMapping', { api: this.api, domainName, stage: this.stage });
      if (zone) {
        const target = RecordTarget.fromAlias(
          new ApiGatewayv2DomainProperties(domainName.regionalDomainName, domainName.regionalHostedZoneId),
        );
        new ARecord(this, 'ApiAlias', { zone, recordName: apiDomain, target });
        new AaaaRecord(this, 'ApiAliasV6', { zone, recordName: apiDomain, target });
      } else {
        externalDnsOutputs(this, 'Api', cert, [apiDomain]);
        new CfnOutput(this, 'ApiDnsRecord', {
          description: `Registro del DNS externo para ${apiDomain} (sin proxy).`,
          value: Fn.join('', [`${apiDomain} CNAME `, domainName.regionalDomainName]),
        });
      }
    }

    // ---- Job diario: EventBridge Scheduler a las 03:00 UTC con reintentos y DLQ (plan §2) ----
    this.jobDlq = new Queue(this, 'VoidedJobDlq', {
      queueName: `${config.prefix}-voided-purchases-job-dlq`,
      encryption: QueueEncryption.SQS_MANAGED,
      enforceSSL: true,
      retentionPeriod: Duration.days(14),
    });
    ack(this.jobDlq, 'AwsSolutions-SQS3', 'Esta cola ya es la DLQ del job; la vigila una alarma del ops-stack.');

    new Schedule(this, 'VoidedJobSchedule', {
      scheduleName: `${config.prefix}-voided-purchases-job`,
      description: 'voidedPurchasesJob diario (08 §10.1).',
      schedule: ScheduleExpression.cron({ minute: '0', hour: '3' }),
      target: new LambdaInvoke(voidedJob.fn, {
        input: undefined,
        retryAttempts: 3,
        maxEventAge: Duration.hours(2),
        deadLetterQueue: this.jobDlq,
      }),
    });
    // El rol que crea LambdaInvoke permite `lambda:InvokeFunction` sobre la función y sus versiones (`:*`).
    for (const child of this.node.children) {
      if (child.node.id.startsWith('SchedulerRoleForTarget')) {
        ack(
          child,
          `AwsSolutions-IAM5[Resource::<${this.getLogicalId(voidedJob.fn.node.defaultChild as CfnFunction)}.Arn>:*]`,
          'Rol del Scheduler generado por aws-cdk-lib: solo invoca voidedPurchasesJob (el comodín cubre sus versiones y alias).',
        );
      }
    }
  }
}
