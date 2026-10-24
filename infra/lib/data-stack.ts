// F2-1 · data-stack: las 6 tablas de DynamoDB del plan §2 (prefijo af-<env>-, sin la marca).
// On-demand, cifradas con la clave gestionada de AWS, con PITR y, en prod, protección contra borrado.
import { RemovalPolicy, Stack } from 'aws-cdk-lib';
import type { StackProps } from 'aws-cdk-lib';
import { AttributeType, Billing, TableEncryptionV2, TableV2 } from 'aws-cdk-lib/aws-dynamodb';
import type { GlobalSecondaryIndexPropsV2 } from 'aws-cdk-lib/aws-dynamodb';
import type { Construct } from 'constructs';
import type { AfConfig } from './config.ts';

export interface DataStackProps extends StackProps {
  readonly config: AfConfig;
}

export interface AfTables {
  /** Libro mayor de compras: PK token_hash; GSI disperso by_uid (uid, ts). */
  readonly purchases: TableV2;
  /** Recompensas por anuncio confirmadas por SSV: PK nonce; TTL 30 días; GSI by_uid. */
  readonly rewards: TableV2;
  /** Caché del veredicto de integridad: PK uid; TTL 24 h. */
  readonly integrity: TableV2;
  /** Códigos de borrado por web: PK code_hash; GSI by_uid (Q-B13-21). */
  readonly accountCodes: TableV2;
  /** Contadores de límite de peticiones: PK key; TTL. */
  readonly rateLimits: TableV2;
  /** Checkpoints de los jobs: PK job. */
  readonly jobs: TableV2;
  readonly leaguePlayers: TableV2;
  readonly leagueBoard: TableV2;
  readonly leagueHistogram: TableV2;
  readonly leagueNames: TableV2;
}

/** Nombre del índice por UID, común a purchases, rewards y account_codes. */
export const BY_UID = 'by_uid';
/** Atributo de caducidad (segundos epoch) de las tablas con TTL. */
export const TTL_ATTRIBUTE = 'ttl';

export class DataStack extends Stack {
  readonly tables: AfTables;

  constructor(scope: Construct, id: string, props: DataStackProps) {
    super(scope, id, props);
    const { config } = props;

    const table = (
      name: string,
      partitionKey: string,
      opts: { ttl?: boolean; gsi?: GlobalSecondaryIndexPropsV2[]; sort?: { name: string; type: AttributeType } } = {},
    ): TableV2 =>
      new TableV2(this, name.replace(/(^|_)(\w)/g, (_m, _s: string, c: string) => c.toUpperCase()), {
        tableName: `${config.prefix}-${name}`,
        partitionKey: { name: partitionKey, type: AttributeType.STRING },
        sortKey: opts.sort,
        billing: Billing.onDemand(),
        encryption: TableEncryptionV2.awsManagedKey(),
        pointInTimeRecoverySpecification: { pointInTimeRecoveryEnabled: true },
        deletionProtection: config.isProd,
        removalPolicy: config.isProd ? RemovalPolicy.RETAIN : RemovalPolicy.DESTROY,
        timeToLiveAttribute: opts.ttl ? TTL_ATTRIBUTE : undefined,
        globalSecondaryIndexes: opts.gsi,
      });

    const byUidWithTs: GlobalSecondaryIndexPropsV2 = {
      indexName: BY_UID,
      partitionKey: { name: 'uid', type: AttributeType.STRING },
      sortKey: { name: 'ts', type: AttributeType.NUMBER },
    };

    this.tables = {
      // Al borrar la cuenta se quita `uid`: la fila sale del GSI disperso y queda anonimizada (plan §2).
      purchases: table('purchases', 'token_hash', { gsi: [byUidWithTs] }),
      rewards: table('rewards', 'nonce', { ttl: true, gsi: [byUidWithTs] }),
      integrity: table('integrity', 'uid', { ttl: true }),
      // TTL para los tokens de confirmación de 5 minutos del borrado por web (F2-8); los códigos no caducan.
      accountCodes: table('account_codes', 'code_hash', {
        ttl: true,
        gsi: [{ indexName: BY_UID, partitionKey: { name: 'uid', type: AttributeType.STRING } }],
      }),
      rateLimits: table('rate_limits', 'key', { ttl: true }),
      jobs: table('jobs', 'job'),
      leaguePlayers: table('league_players', 'uid', { ttl: true, sort: { name: 'week', type: AttributeType.STRING } }),
      leagueBoard: table('league_board', 'scope', { ttl: true, sort: { name: 'sort', type: AttributeType.STRING } }),
      leagueHistogram: table('league_histogram', 'scope', { ttl: true, sort: { name: 'bucket', type: AttributeType.NUMBER } }),
      leagueNames: table('league_names', 'uid'),
    };
  }
}
