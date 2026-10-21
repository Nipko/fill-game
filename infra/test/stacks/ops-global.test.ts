import { describe, expect, it } from 'vitest';
import { build, DEV, PROD, PROD_DOMAIN, resourcesOfType } from '../helpers.ts';

describe('ops-stack', () => {
  it('alarmas de Lambda, API, job, DynamoDB e integridad, todas con aviso a SNS', () => {
    const { t } = build(DEV);
    const json = t.ops.toJSON() as Record<string, unknown>;
    const alarms = resourcesOfType(json, 'AWS::CloudWatch::Alarm');
    // 5 funciones × (errores + throttles) + 5xx + DLQ + 6 tablas + integridad = 19
    expect(alarms).toHaveLength(19);
    for (const [, p] of alarms) {
      expect((p.AlarmActions as unknown[]).length).toBe(1);
      expect(p.TreatMissingData).toBe('notBreaching');
      expect(p.AlarmName as string).toMatch(/^af-dev-/);
    }
    const integrity = alarms.find(([, p]) => p.MetricName === 'IntegrityChecks')?.[1];
    expect(integrity).toMatchObject({ Namespace: 'AF/Backend', Threshold: 7000, Period: 86400 });
    t.ops.hasResourceProperties('AWS::SNS::Topic', { TopicName: 'af-dev-alarms' });
  });

  it('sin -c alertEmail no hay suscripción; con él, una por correo', () => {
    build(PROD).t.ops.resourceCountIs('AWS::SNS::Subscription', 0);
    build(PROD_DOMAIN).t.ops.hasResourceProperties('AWS::SNS::Subscription', {
      Protocol: 'email',
      Endpoint: 'alerts@example.com',
    });
  });

  it('el tema de SNS exige TLS', () => {
    const json = build(DEV).t.ops.toJSON() as Record<string, unknown>;
    const policies = JSON.stringify(resourcesOfType(json, 'AWS::SNS::TopicPolicy'));
    expect(policies).toContain('aws:SecureTransport');
  });
});

describe('global-stack (us-east-1)', () => {
  it('presupuesto de 10 USD en dev y 50 USD en prod', () => {
    build(DEV).t.global.hasResourceProperties('AWS::Budgets::Budget', {
      Budget: { BudgetName: 'af-dev-monthly', BudgetType: 'COST', TimeUnit: 'MONTHLY', BudgetLimit: { Amount: 10, Unit: 'USD' } },
    });
    build(PROD).t.global.hasResourceProperties('AWS::Budgets::Budget', {
      Budget: { BudgetName: 'af-prod-monthly', BudgetType: 'COST', TimeUnit: 'MONTHLY', BudgetLimit: { Amount: 50, Unit: 'USD' } },
    });
  });

  it('avisos al 50, 80 y 100 % al correo del contexto', () => {
    const json = build(PROD_DOMAIN).t.global.toJSON() as Record<string, unknown>;
    const budget = resourcesOfType(json, 'AWS::Budgets::Budget')[0][1];
    const n = budget.NotificationsWithSubscribers as Array<{
      Notification: { Threshold: number; ThresholdType: string; NotificationType: string };
      Subscribers: Array<{ Address: string }>;
    }>;
    expect(n.map((x) => x.Notification.Threshold)).toEqual([50, 80, 100]);
    for (const x of n) {
      expect(x.Notification).toMatchObject({ ThresholdType: 'PERCENTAGE', NotificationType: 'ACTUAL' });
      expect(x.Subscribers).toEqual([{ SubscriptionType: 'EMAIL', Address: 'alerts@example.com' }]);
    }
  });

  it('región us-east-1 y certificado solo con dominio', () => {
    const dev = build(DEV);
    expect(dev.stacks.global.region).toBe('us-east-1');
    dev.t.global.resourceCountIs('AWS::CertificateManager::Certificate', 0);
    for (const k of ['data', 'api', 'web', 'cdn', 'ops'] as const) expect(dev.stacks[k].region).toBe('sa-east-1');
  });
});
