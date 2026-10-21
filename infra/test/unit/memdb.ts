// DynamoDB en memoria sobre aws-sdk-client-mock: entiende exactamente las operaciones y expresiones que usan
// las Lambdas (sin red). Sirve para probar idempotencia y carreras con un estado real, no solo respuestas fijas.
import {
  BatchGetCommand,
  DeleteCommand,
  DynamoDBDocumentClient,
  GetCommand,
  PutCommand,
  QueryCommand,
  UpdateCommand,
} from '@aws-sdk/lib-dynamodb';
import { DynamoDBClient } from '@aws-sdk/client-dynamodb';
import { mockClient } from 'aws-sdk-client-mock';

type Item = Record<string, unknown>;

export const TABLES = {
  AF_TABLE_PURCHASES: 'af-test-purchases',
  AF_TABLE_REWARDS: 'af-test-rewards',
  AF_TABLE_INTEGRITY: 'af-test-integrity',
  AF_TABLE_ACCOUNT_CODES: 'af-test-account_codes',
  AF_TABLE_RATE_LIMITS: 'af-test-rate_limits',
  AF_TABLE_JOBS: 'af-test-jobs',
} as const;

const PK: Record<string, string> = {
  [TABLES.AF_TABLE_PURCHASES]: 'token_hash',
  [TABLES.AF_TABLE_REWARDS]: 'nonce',
  [TABLES.AF_TABLE_INTEGRITY]: 'uid',
  [TABLES.AF_TABLE_ACCOUNT_CODES]: 'code_hash',
  [TABLES.AF_TABLE_RATE_LIMITS]: 'key',
  [TABLES.AF_TABLE_JOBS]: 'job',
};

export function conditionalFailed(): Error {
  const e = new Error('The conditional request failed');
  e.name = 'ConditionalCheckFailedException';
  return e;
}

export class MemDb {
  readonly tables = new Map<string, Map<string, Item>>();
  // Región y credenciales de mentira: el envío está simulado y nunca sale a la red.
  readonly client = DynamoDBDocumentClient.from(
    new DynamoDBClient({ region: 'sa-east-1', credentials: { accessKeyId: 'test', secretAccessKey: 'test' } }),
  );
  readonly mock: { restore(): void };
  /** Claves que BatchGet devuelve como no procesadas la próxima vez (para probar reintentos). */
  unprocessedOnce = new Set<string>();
  /** Si se pone, la siguiente operación de este tipo lanza este error. */
  failNext: { command: string; table?: string; error: Error } | undefined;

  constructor() {
    const m = mockClient(DynamoDBDocumentClient);
    this.mock = m;
    const guard = (command: string, table?: string): void => {
      if (this.failNext?.command === command && (this.failNext.table === undefined || this.failNext.table === table)) {
        const e = this.failNext.error;
        this.failNext = undefined;
        throw e;
      }
    };
    m.on(GetCommand).callsFake((i: { TableName: string; Key: Item }) => {
      guard('Get', i.TableName);
      const it = this.get(i.TableName, i.Key);
      return Promise.resolve({ Item: it ? structuredClone(it) : undefined });
    });
    m.on(PutCommand).callsFake((i: { TableName: string; Item: Item; ConditionExpression?: string }) => {
      guard('Put', i.TableName);
      const pk = PK[i.TableName];
      const key = String(i.Item[pk]);
      const t = this.table(i.TableName);
      if (i.ConditionExpression?.startsWith('attribute_not_exists') && t.has(key)) throw conditionalFailed();
      t.set(key, structuredClone(i.Item));
      return Promise.resolve({});
    });
    m.on(DeleteCommand).callsFake(
      (i: { TableName: string; Key: Item; ConditionExpression?: string; ExpressionAttributeValues?: Item }) => {
        guard('Delete', i.TableName);
        const t = this.table(i.TableName);
        const key = String(i.Key[PK[i.TableName]]);
        const cur = t.get(key);
        if (i.ConditionExpression && cur && cur.uid !== i.ExpressionAttributeValues?.[':u']) throw conditionalFailed();
        t.delete(key);
        return Promise.resolve({});
      },
    );
    m.on(UpdateCommand).callsFake(
      (i: { TableName: string; Key: Item; UpdateExpression: string; ConditionExpression?: string; ExpressionAttributeValues: Item }) => {
        guard('Update', i.TableName);
        const t = this.table(i.TableName);
        const pk = PK[i.TableName];
        const key = String(i.Key[pk]);
        const cur = t.get(key);
        const v = i.ExpressionAttributeValues;
        if (i.UpdateExpression.startsWith('ADD #c')) {
          const next = { [pk]: key, ...(cur ?? {}), count: Number(cur?.count ?? 0) + 1, ttl: cur?.ttl ?? v[':ttl'] };
          t.set(key, next);
          return Promise.resolve({ Attributes: { count: next.count, ttl: next.ttl } });
        }
        if (i.UpdateExpression === 'SET counted = :t') {
          t.set(key, { [pk]: key, ...(cur ?? {}), counted: v[':t'] });
          return Promise.resolve({});
        }
        if (i.UpdateExpression.startsWith('SET #s = :voided')) {
          if (!cur || cur.state === 'voided') throw conditionalFailed();
          t.set(key, { ...cur, state: 'voided', voided_at: v[':at'], voided_reason: v[':r'], voided_source: v[':src'] });
          return Promise.resolve({});
        }
        if (i.UpdateExpression.startsWith('REMOVE #u')) {
          if (!cur || cur.uid !== v[':u']) throw conditionalFailed();
          const next: Item = { ...cur, anonymized_at: v[":t"] };
          delete next.uid;
          t.set(key, next);
          return Promise.resolve({});
        }
        throw new Error(`UpdateExpression no soportada en MemDb: ${i.UpdateExpression}`);
      },
    );
    m.on(QueryCommand).callsFake(
      (i: { TableName: string; IndexName?: string; ExpressionAttributeValues: Item }) => {
        guard('Query', i.TableName);
        if (i.IndexName !== 'by_uid') throw new Error('MemDb solo consulta by_uid');
        const items = [...this.table(i.TableName).values()]
          .filter((it) => it.uid === i.ExpressionAttributeValues[':u'])
          .sort((a, b) => Number(a.ts ?? 0) - Number(b.ts ?? 0))
          .map((it) => structuredClone(it));
        return Promise.resolve({ Items: items });
      },
    );
    m.on(BatchGetCommand).callsFake((i: { RequestItems: Record<string, { Keys: Item[] }> }) => {
      guard('BatchGet');
      const Responses: Record<string, Item[]> = {};
      const UnprocessedKeys: Record<string, { Keys: Item[] }> = {};
      for (const [table, req] of Object.entries(i.RequestItems)) {
        Responses[table] = [];
        for (const k of req.Keys) {
          const key = String(k[PK[table]]);
          if (this.unprocessedOnce.delete(key)) {
            (UnprocessedKeys[table] ??= { Keys: [] }).Keys.push(k);
            continue;
          }
          const it = this.table(table).get(key);
          if (it) Responses[table].push(structuredClone(it));
        }
      }
      return Promise.resolve({ Responses, UnprocessedKeys });
    });
  }

  table(name: string): Map<string, Item> {
    let t = this.tables.get(name);
    if (!t) {
      t = new Map();
      this.tables.set(name, t);
    }
    return t;
  }

  get(table: string, key: Item): Item | undefined {
    return this.table(table).get(String(key[PK[table]]));
  }

  put(table: string, item: Item): void {
    this.table(table).set(String(item[PK[table]]), structuredClone(item));
  }

  items(table: string): Item[] {
    return [...this.table(table).values()];
  }

  restore(): void {
    this.mock.restore();
  }
}
