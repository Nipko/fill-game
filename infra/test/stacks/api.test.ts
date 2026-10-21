import { Match } from 'aws-cdk-lib/assertions';
import { describe, expect, it } from 'vitest';
import { loadContractIndex, routeKey } from '../../lib/contract.ts';
import { build, DEV, PROD, PROD_DOMAIN, resourcesOfType } from '../helpers.ts';

const contract = loadContractIndex();

type Statement = { Effect: string; Action: string | string[]; Resource: unknown; Condition?: unknown };

function roleStatements(json: Record<string, unknown>, functionLogicalPrefix: string): Statement[] {
  return resourcesOfType(json, 'AWS::IAM::Policy')
    .filter(([id]) => id.startsWith(functionLogicalPrefix))
    .flatMap(([, p]) => (p.PolicyDocument as { Statement: Statement[] }).Statement);
}

const actionsOf = (st: Statement[]): string[] => st.flatMap((s) => (Array.isArray(s.Action) ? s.Action : [s.Action])).sort();

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

  it('5 Lambdas Node 24 arm64 con log group de 30 días', () => {
    const fns = resourcesOfType(json, 'AWS::Lambda::Function');
    expect(fns.map(([, p]) => p.FunctionName).sort()).toEqual(
      ['delete-account', 'restore-ledger', 'rewarded-ssv', 'verify-purchase', 'voided-purchases-job'].map((n) => `af-${envName}-${n}`),
    );
    for (const [, p] of fns) {
      expect(p.Runtime).toBe('nodejs24.x');
      expect(p.Architectures).toEqual(['arm64']);
      expect(p.LoggingConfig).toMatchObject({ LogFormat: 'JSON' });
    }
    for (const [, p] of resourcesOfType(json, 'AWS::Logs::LogGroup')) expect(p.RetentionInDays).toBe(30);
  });

  it(`verificador falso de Play ${envName === 'prod' ? 'ausente' : 'presente'}`, () => {
    const withFake = ['delete-account', 'verify-purchase', 'voided-purchases-job'].map((n) => `af-${envName}-${n}`);
    for (const [, p] of resourcesOfType(json, 'AWS::Lambda::Function')) {
      const vars = ((p.Environment as { Variables?: Record<string, string> } | undefined)?.Variables) ?? {};
      if (envName === 'prod' || !withFake.includes(p.FunctionName as string)) expect(vars).not.toHaveProperty('AF_FAKE_PLAY_VERIFIER');
      else expect(vars.AF_FAKE_PLAY_VERIFIER).toBe('1');
      expect(vars.AF_ENV).toBe(envName);
    }
    expect(JSON.stringify(json).includes('AF_FAKE_PLAY_VERIFIER')).toBe(envName !== 'prod');
  });

  it('tres secretos sin valor en la plantilla', () => {
    const secrets = resourcesOfType(json, 'AWS::SecretsManager::Secret');
    expect(secrets.map(([, p]) => p.Name).sort()).toEqual(
      ['firebase-admin-service-account', 'max-ssv-key', 'play-service-account'].map((n) => `af-${envName}/${n}`),
    );
    for (const [, p] of secrets) {
      expect(p.SecretString).toBeUndefined();
      expect(p.GenerateSecretString).toBeDefined();
    }
  });

  it('IAM mínimo: roles sin políticas gestionadas ni comodines', () => {
    const roles = resourcesOfType(json, 'AWS::IAM::Role').filter(([id]) => !id.startsWith('SchedulerRole'));
    expect(roles).toHaveLength(5);
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

  it('cada función solo tiene lo suyo', () => {
    expect(actionsOf(roleStatements(json, 'RestoreLedger'))).toEqual(
      ['dynamodb:GetItem', 'dynamodb:Query', 'logs:CreateLogStream', 'logs:PutLogEvents'],
    );
    const rest = JSON.stringify(roleStatements(json, 'RestoreLedger'));
    expect(rest).not.toMatch(/secretsmanager/);
    expect(actionsOf(roleStatements(json, 'RewardedSsv'))).toEqual(
      ['dynamodb:PutItem', 'dynamodb:UpdateItem', 'logs:CreateLogStream', 'logs:PutLogEvents', 'secretsmanager:GetSecretValue'],
    );
    // Ninguna función puede hacer Scan, BatchWriteItem ni borrar tablas.
    expect(JSON.stringify(json)).not.toMatch(/dynamodb:(Scan|BatchWriteItem|DeleteTable|\*)/);
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
