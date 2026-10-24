import { Match } from 'aws-cdk-lib/assertions';
import { describe, expect, it } from 'vitest';
import { build, DEV, PROD, resourcesOfType } from '../helpers.ts';

const TABLES = ['purchases', 'rewards', 'integrity', 'account_codes', 'rate_limits', 'jobs', 'league_players', 'league_board', 'league_histogram', 'league_names'];
const WITH_TTL = ['rewards', 'integrity', 'account_codes', 'rate_limits', 'league_players', 'league_board', 'league_histogram'];
const WITH_BY_UID = ['purchases', 'rewards', 'account_codes'];

describe.each([
  ['dev', DEV],
  ['prod', PROD],
])('data-stack (%s)', (envName, ctx) => {
  const { t } = build(ctx);
  const isProd = envName === 'prod';
  const tables = resourcesOfType(t.data.toJSON(), 'AWS::DynamoDB::GlobalTable');

  it('crea las 10 tablas con el prefijo af-<env>-', () => {
    const names = tables.map(([, p]) => p.TableName as string).sort();
    expect(names).toEqual(TABLES.map((n) => `af-${envName}-${n}`).sort());
  });

  it('on-demand, cifradas con KMS gestionada y con PITR', () => {
    for (const [, p] of tables) {
      expect(p.BillingMode).toBe('PAY_PER_REQUEST');
      expect(p.SSESpecification).toMatchObject({ SSEEnabled: true, SSEType: 'KMS' });
      const replicas = p.Replicas as Array<Record<string, unknown>>;
      expect(replicas).toHaveLength(1);
      expect(replicas[0].PointInTimeRecoverySpecification).toEqual({ PointInTimeRecoveryEnabled: true });
      expect(replicas[0].DeletionProtectionEnabled).toBe(isProd);
    }
  });

  it(`protección contra borrado y retención ${isProd ? 'activadas' : 'desactivadas'}`, () => {
    t.data.allResources('AWS::DynamoDB::GlobalTable', {
      DeletionPolicy: isProd ? 'Retain' : 'Delete',
      UpdateReplacePolicy: isProd ? 'Retain' : 'Delete',
    });
  });

  it('TTL en las tablas que caducan', () => {
    for (const [, p] of tables) {
      const short = (p.TableName as string).replace(`af-${envName}-`, '');
      if (WITH_TTL.includes(short)) {
        expect(p.TimeToLiveSpecification, short).toEqual({ AttributeName: 'ttl', Enabled: true });
      } else {
        expect(p.TimeToLiveSpecification, short).toBeUndefined();
      }
    }
  });

  it('índice by_uid donde lo pide el plan §2', () => {
    for (const [, p] of tables) {
      const short = (p.TableName as string).replace(`af-${envName}-`, '');
      const gsis = (p.GlobalSecondaryIndexes ?? []) as Array<{ IndexName: string }>;
      expect(gsis.map((g) => g.IndexName), short).toEqual(WITH_BY_UID.includes(short) ? ['by_uid'] : []);
    }
    t.data.hasResourceProperties('AWS::DynamoDB::GlobalTable', {
      TableName: `af-${envName}-purchases`,
      KeySchema: [{ AttributeName: 'token_hash', KeyType: 'HASH' }],
      GlobalSecondaryIndexes: [
        Match.objectLike({
          IndexName: 'by_uid',
          KeySchema: [
            { AttributeName: 'uid', KeyType: 'HASH' },
            { AttributeName: 'ts', KeyType: 'RANGE' },
          ],
        }),
      ],
    });
  });
});
