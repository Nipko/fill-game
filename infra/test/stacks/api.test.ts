import { Match } from 'aws-cdk-lib/assertions';
import { describe, expect, it } from 'vitest';
import { loadContractIndex, routeKey } from '../../lib/contract.ts';
import { build, DEV, PROD, PROD_DOMAIN, TEST_APP_ID, resourcesOfType } from '../helpers.ts';

const contract = loadContractIndex();

type Statement = { Effect: string; Action: string | string[]; Resource: unknown; Condition?: unknown };

function roleStatements(json: Record<string, unknown>, functionLogicalPrefix: string): Statement[] {
  return resourcesOfType(json, 'AWS::IAM::Policy')
    .filter(([id]) => id.startsWith(functionLogicalPrefix))
    .flatMap(([, p]) => (p.PolicyDocument as { Statement: Statement[] }).Statement);
}

/** Etiqueta legible de un recurso de IAM: tabla (o tabla/índice), secreto o log group. */
function labelOf(resource: unknown): string {
  const text = JSON.stringify(resource);
  const table = /PublishOutputFnGetAtt([A-Za-z]+?)[0-9A-F]{8}Arn/.exec(text);
  if (table) return text.includes('/index/by_uid') ? `${table[1]}/by_uid` : table[1];
  const secret = /"Ref":"(PlayServiceAccount|FirebaseAdmin)[0-9A-F]{8}"/.exec(text);
  if (secret) return `secret:${secret[1]}`;
  if (/Logs[0-9A-F]{8}/.test(text)) return 'logs';
  return text;
}

/** Acciones concedidas por recurso. */
function grantsOf(st: Statement[]): Record<string, string[]> {
  const out: Record<string, string[]> = {};
  for (const s of st) {
    const actions = Array.isArray(s.Action) ? s.Action : [s.Action];
    for (const r of Array.isArray(s.Resource) ? s.Resource : [s.Resource]) {
      const k = labelOf(r);
      out[k] = [...new Set([...(out[k] ?? []), ...actions])].sort();
    }
  }
  return out;
}

describe.each([
  ['dev', DEV],
  ['prod', PROD],
])('api-stack (%s)', (envName, ctx) => {
  const { t } = build(ctx);
  const json = t.api.toJSON() as Record<string, unknown>;
  const project = envName === 'prod' ? 'amazon-fill-prod' : 'amazon-fill-dev';

  it('autorizador JWT de Firebase con emisor y audiencia del entorno', () => {
    t.api.resourceCountIs('AWS::ApiGatewayV2::Authorizer', 1);
    t.api.hasResourceProperties('AWS::ApiGatewayV2::Authorizer', {
      AuthorizerType: 'JWT',
      IdentitySource: ['$request.header.Authorization'],
      JwtConfiguration: { Issuer: `https://securetoken.google.com/${project}`, Audience: [project] },
    });
  });

  it('las rutas son exactamente las del contrato, con su autorización', () => {
    const routes = resourcesOfType(json, 'AWS::ApiGatewayV2::Route');
    const byKey = new Map(routes.map(([, p]) => [p.RouteKey as string, p]));
    expect([...byKey.keys()].sort()).toEqual(contract.operations.map(routeKey).sort());
    for (const op of contract.operations) {
      const r = byKey.get(routeKey(op));
      expect(r?.AuthorizationType, routeKey(op)).toBe(op.auth === 'firebase' ? 'JWT' : 'NONE');
    }
  });

  it('límite por defecto y límite estricto en el borrado por web', () => {
    const stage = resourcesOfType(json, 'AWS::ApiGatewayV2::Stage')[0][1];
    expect(stage.StageName).toBe('$default');
    expect(stage.DefaultRouteSettings).toMatchObject({ ThrottlingRateLimit: 50, ThrottlingBurstLimit: 100 });
    const rs = stage.RouteSettings as Record<string, { ThrottlingRateLimit: number; ThrottlingBurstLimit: number }>;
    expect(Object.keys(rs).sort()).toEqual(['POST /v1/account/delete-confirm', 'POST /v1/account/delete-request']);
    for (const v of Object.values(rs)) expect(v).toEqual({ ThrottlingRateLimit: 2, ThrottlingBurstLimit: 5 });
    expect(stage.AccessLogSettings).toBeDefined();
    expect(JSON.stringify(stage.AccessLogSettings)).not.toMatch(/claims|\$context\.authorizer\.claims|principalId/);
  });

  it('sin CORS en la API', () => {
    const apis = resourcesOfType(json, 'AWS::ApiGatewayV2::Api');
    expect(apis).toHaveLength(1);
    expect(apis[0][1].CorsConfiguration).toBeUndefined();
  });

  it('7 Lambdas Node 24 arm64 con log group de 30 días', () => {
    const fns = resourcesOfType(json, 'AWS::Lambda::Function');
    expect(fns.map(([, p]) => p.FunctionName).sort()).toEqual(
      ['delete-account', 'league-board', 'league-wins', 'restore-ledger', 'rewarded-ssv', 'verify-purchase', 'voided-purchases-job'].map((n) => `af-${envName}-${n}`),
    );
    for (const [, p] of fns) {
      expect(p.Runtime).toBe('nodejs24.x');
      expect(p.Architectures).toEqual(['arm64']);
      expect(p.LoggingConfig).toMatchObject({ LogFormat: 'JSON' });
    }
    for (const [, p] of resourcesOfType(json, 'AWS::Logs::LogGroup')) expect(p.RetentionInDays).toBe(30);
  });

  it(`verificador falso de Play ${envName === 'prod' ? 'ausente' : 'presente'}`, () => {
    const withFake = ['delete-account', 'verify-purchase', 'voided-purchases-job', 'league-wins', 'league-board'].map((n) => `af-${envName}-${n}`);
    for (const [, p] of resourcesOfType(json, 'AWS::Lambda::Function')) {
      const vars = ((p.Environment as { Variables?: Record<string, string> } | undefined)?.Variables) ?? {};
      if (envName === 'prod' || !withFake.includes(p.FunctionName as string)) expect(vars).not.toHaveProperty('AF_FAKE_PLAY_VERIFIER');
      else expect(vars.AF_FAKE_PLAY_VERIFIER).toBe('1');
      expect(vars.AF_ENV).toBe(envName);
    }
    expect(JSON.stringify(json).includes('AF_FAKE_PLAY_VERIFIER')).toBe(envName !== 'prod');
  });

  it('dos secretos sin valor en la plantilla (sin el de MAX: el SSV de AdMob usa claves públicas, D-049)', () => {
    const secrets = resourcesOfType(json, 'AWS::SecretsManager::Secret');
    expect(secrets.map(([, p]) => p.Name).sort()).toEqual(
      ['firebase-admin-service-account', 'play-service-account'].map((n) => `af-${envName}/${n}`),
    );
    for (const [, p] of secrets) {
      expect(p.SecretString).toBeUndefined();
      expect(p.GenerateSecretString).toBeDefined();
    }
  });

  it('IAM mínimo: roles sin políticas gestionadas ni comodines', () => {
    const roles = resourcesOfType(json, 'AWS::IAM::Role').filter(([id]) => !id.startsWith('SchedulerRole'));
    expect(roles).toHaveLength(7);
    for (const [, p] of roles) expect(p.ManagedPolicyArns).toBeUndefined();
    for (const [id, p] of resourcesOfType(json, 'AWS::IAM::Policy')) {
      for (const s of (p.PolicyDocument as { Statement: Statement[] }).Statement) {
        const actions = Array.isArray(s.Action) ? s.Action : [s.Action];
        expect(s.Effect, id).toBe('Allow');
        for (const a of actions) expect(a, id).not.toMatch(/\*/);
        expect(JSON.stringify(s.Resource), id).not.toBe('"*"');
      }
    }
  });

  it('cada función solo tiene lo suyo: acciones exactas por tabla, índice y secreto (F2-3 a F2-8)', () => {
    const expected: Record<string, Record<string, string[]>> = {
      VerifyPurchase: {
        Purchases: ['dynamodb:GetItem', 'dynamodb:PutItem', 'dynamodb:UpdateItem'],
        Integrity: ['dynamodb:GetItem', 'dynamodb:PutItem'],
        RateLimits: ['dynamodb:ConditionCheckItem', 'dynamodb:GetItem', 'dynamodb:PutItem', 'dynamodb:UpdateItem'],
        'secret:PlayServiceAccount': ['secretsmanager:GetSecretValue'],
      },
      RestoreLedger: {
        'Purchases/by_uid': ['dynamodb:Query'],
        Rewards: ['dynamodb:BatchGetItem'],
      },
      RewardedSsv: {
        Rewards: ['dynamodb:PutItem'],
        RateLimits: ['dynamodb:ConditionCheckItem', 'dynamodb:GetItem', 'dynamodb:PutItem', 'dynamodb:UpdateItem'],
      },
      DeleteAccount: {
        LeaguePlayers: ['dynamodb:GetItem', 'dynamodb:Query', 'dynamodb:DeleteItem'],
        LeagueBoard: ['dynamodb:DeleteItem'],
        LeagueHistogram: ['dynamodb:UpdateItem'],
        LeagueNames: ['dynamodb:GetItem', 'dynamodb:DeleteItem'],
        'Purchases/by_uid': ['dynamodb:Query'],
        Purchases: ['dynamodb:UpdateItem'],
        'Rewards/by_uid': ['dynamodb:Query'],
        Rewards: ['dynamodb:DeleteItem'],
        Integrity: ['dynamodb:DeleteItem', 'dynamodb:GetItem', 'dynamodb:PutItem'],
        'AccountCodes/by_uid': ['dynamodb:Query'],
        AccountCodes: ['dynamodb:DeleteItem', 'dynamodb:GetItem', 'dynamodb:PutItem'],
        RateLimits: ['dynamodb:ConditionCheckItem', 'dynamodb:DeleteItem', 'dynamodb:GetItem', 'dynamodb:PutItem', 'dynamodb:UpdateItem'],
        'secret:FirebaseAdmin': ['secretsmanager:GetSecretValue'],
      },
      VoidedPurchasesJob: {
        Purchases: ['dynamodb:GetItem', 'dynamodb:PutItem', 'dynamodb:UpdateItem'],
        Jobs: ['dynamodb:GetItem', 'dynamodb:PutItem'],
        'secret:PlayServiceAccount': ['secretsmanager:GetSecretValue'],
      },
      LeagueWins: {
        RateLimits: ['dynamodb:GetItem','dynamodb:PutItem','dynamodb:ConditionCheckItem'],
        Integrity: ['dynamodb:GetItem','dynamodb:PutItem'],
        LeagueNames: ['dynamodb:GetItem','dynamodb:PutItem'],
        LeaguePlayers: ['dynamodb:GetItem','dynamodb:PutItem'],
        LeagueBoard: ['dynamodb:PutItem','dynamodb:DeleteItem'],
        LeagueHistogram: ['dynamodb:UpdateItem'],
      },
      LeagueBoard: {
        RateLimits: ['dynamodb:GetItem','dynamodb:PutItem','dynamodb:ConditionCheckItem'],
        Integrity: ['dynamodb:GetItem','dynamodb:PutItem'],
        LeagueNames: ['dynamodb:GetItem','dynamodb:PutItem'],
        LeaguePlayers: ['dynamodb:GetItem'],
        LeagueBoard: ['dynamodb:Query'],
        LeagueHistogram: ['dynamodb:Query'],
      },
    };
    for (const [fn, want] of Object.entries(expected)) {
      const got = grantsOf(roleStatements(json, fn));
      const logs = got.logs;
      delete got.logs;
      expect(logs, fn).toEqual(['logs:CreateLogStream', 'logs:PutLogEvents']);
      expect(got, fn).toEqual(Object.fromEntries(Object.entries(want).map(([k, v]) => [k, [...v].sort()])));
    }
    // Ninguna función puede hacer Scan, BatchWriteItem ni borrar tablas.
    expect(JSON.stringify(json)).not.toMatch(/dynamodb:(Scan|BatchWriteItem|DeleteTable|\*)/);
  });

  it(`paquete de Play: ${envName === 'prod' ? 'sin valor en prod hasta el dictamen' : 'el provisional en dev'}`, () => {
    for (const [, p] of resourcesOfType(json, 'AWS::Lambda::Function')) {
      const vars = ((p.Environment as { Variables?: Record<string, string> } | undefined)?.Variables) ?? {};
      const usesPlay = ['verify-purchase', 'voided-purchases-job'].some((n) => p.FunctionName === `af-${envName}-${n}`);
      if (envName === 'dev' && usesPlay) expect(vars.AF_PACKAGE_NAME).toBe('com.amazonfill.dev');
      else expect(vars).not.toHaveProperty('AF_PACKAGE_NAME');
    }
    if (envName === 'prod') expect(JSON.stringify(json)).not.toContain('com.amazonfill.dev');
  });

  it(`App Check: ${envName === 'prod' ? 'las Lambdas con integridad solo admiten las apps de -c firebaseAppIds' : 'sin lista de apps en dev'}`, () => {
    for (const [, p] of resourcesOfType(json, 'AWS::Lambda::Function')) {
      const vars = ((p.Environment as { Variables?: Record<string, string> } | undefined)?.Variables) ?? {};
      const usesIntegrity = ['verify-purchase', 'delete-account', 'league-board', 'league-wins'].some((n) => p.FunctionName === `af-${envName}-${n}`);
      if (envName === 'prod' && usesIntegrity) expect(vars.AF_APPCHECK_APP_IDS).toBe(TEST_APP_ID);
      else expect(vars).not.toHaveProperty('AF_APPCHECK_APP_IDS');
    }
  });

  it('job diario a las 03:00 UTC con reintentos y DLQ cifrada', () => {
    t.api.hasResourceProperties('AWS::Scheduler::Schedule', {
      ScheduleExpression: 'cron(0 3 * * ? *)',
      ScheduleExpressionTimezone: 'Etc/UTC',
      Target: Match.objectLike({
        RetryPolicy: { MaximumRetryAttempts: 3, MaximumEventAgeInSeconds: 7200 },
        DeadLetterConfig: Match.objectLike({ Arn: Match.anyValue() }),
      }),
    });
    t.api.hasResourceProperties('AWS::SQS::Queue', { SqsManagedSseEnabled: true });
  });

  it('solo API Gateway y el Scheduler pueden invocar las Lambdas; sin Function URL', () => {
    t.api.resourceCountIs('AWS::Lambda::Url', 0);
    for (const [, p] of resourcesOfType(json, 'AWS::Lambda::Permission')) {
      expect(p.Principal).toBe('apigateway.amazonaws.com');
      expect(p.SourceArn).toBeDefined();
    }
  });
});

describe('api-stack con dominio', () => {
  it('api.<dominio> con certificado regional y alias', () => {
    const { t } = build(PROD_DOMAIN);
    t.api.hasResourceProperties('AWS::ApiGatewayV2::DomainName', {
      DomainName: 'api.example.com',
      DomainNameConfigurations: [Match.objectLike({ SecurityPolicy: 'TLS_1_2' })],
    });
    t.api.resourceCountIs('AWS::ApiGatewayV2::ApiMapping', 1);
    t.api.resourceCountIs('AWS::Route53::RecordSet', 2);
  });

  it('sin dominio no hay certificado ni registros', () => {
    const { t } = build(DEV);
    t.api.resourceCountIs('AWS::CertificateManager::Certificate', 0);
    t.api.resourceCountIs('AWS::Route53::RecordSet', 0);
  });
});
