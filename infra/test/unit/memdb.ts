// DynamoDB en memoria sobre aws-sdk-client-mock: entiende exactamente las operaciones y expresiones que usan
// las Lambdas (sin red). Sirve para probar idempotencia y carreras con un estado real, no solo respuestas fijas.
import {
  BatchGetCommand,
  DeleteCommand,
  DynamoDBDocumentClient,
  GetCommand,
  PutCommand,
  QueryCommand,
  TransactWriteCommand,
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

/** Transacción cancelada como la devuelve DynamoDB: un motivo por elemento, en orden. */
export function transactionCanceled(codes: string[]): Error {
  const e = new Error('Transaction cancelled') as Error & { CancellationReasons: Array<{ Code: string }> };
  e.name = 'TransactionCanceledException';
  e.CancellationReasons = codes.map((Code) => ({ Code }));
  return e;
}

type TxItem = {
  Put?: { TableName: string; Item: Item; ConditionExpression?: string; ExpressionAttributeValues?: Item };
  Update?: {
    TableName: string;
    Key: Item;
    UpdateExpression: string;
    ConditionExpression?: string;
    ExpressionAttributeNames?: Record<string, string>;
    ExpressionAttributeValues?: Item;
  };
  ConditionCheck?: { TableName: string; Key: Item; ConditionExpression: string; ExpressionAttributeValues?: Item };
};

/**
 * Conjunción de condiciones sobre el ítem (la reasignación de Q-IAP-4): cada término es `attribute_not_exists(x)`
 * o `x = :v`, con `x` un atributo o un alias `#n`. El ítem debe existir.
 */
function andConditionHolds(cond: string, cur: Item | undefined, names: Record<string, string> | undefined, values: Item | undefined): boolean {
  if (!cur) return false;
  const attr = (x: string): string => (x.startsWith('#') ? (names?.[x] ?? x) : x);
  return cond.split(' AND ').every((term) => {
    const ne = /^attribute_not_exists\(([#a-z_]+)\)$/.exec(term);
    if (ne) return cur[attr(ne[1])] === undefined;
    const eq = /^([#a-z_]+) = (:[a-z0-9_]+)$/.exec(term);
    if (eq) return cur[attr(eq[1])] === values?.[eq[2]];
    throw new Error(`Término no soportado en MemDb: ${term}`);
  });
}

/** Las condiciones que usan las transacciones de las Lambdas (y solo esas). */
function txConditionHolds(
  cond: string | undefined,
  cur: Item | undefined,
  values: Item | undefined,
  names?: Record<string, string>,
): boolean {
  if (cond === undefined) return true;
  if (cond === 'attribute_not_exists(#k) OR #t <= :now') return !cur || Number(cur.ttl) <= Number(values?.[':now']);
  if (cond === 'token_hash = :th') return cur?.token_hash === values?.[':th'];
  if (cond.includes(' AND ') && !cond.includes(' OR ')) return andConditionHolds(cond, cur, names, values);
  if (/^attribute_not_exists\([#a-z_]+\)$/.test(cond)) return !cur;
  throw new Error(`Condición no soportada en MemDb: ${cond}`);
}

/**
 * Como DynamoDB: todo alias de ExpressionAttributeNames/Values debe usarse en alguna expresión (si no,
 * ValidationException) y todo alias usado debe estar definido.
 */
function checkAliases(op: object): void {
  const o = op as Record<string, unknown>;
  const exprs = ['UpdateExpression', 'ConditionExpression', 'KeyConditionExpression', 'ProjectionExpression', 'FilterExpression']
    .map((k) => o[k])
    .filter((x): x is string => typeof x === 'string')
    .join(' ');
  for (const [field, re] of [['ExpressionAttributeNames', /#[A-Za-z0-9_]+/g], ['ExpressionAttributeValues', /:[A-Za-z0-9_]+/g]] as const) {
    const defined = Object.keys(o[field] ?? {});
    const used = new Set(exprs.match(re) ?? []);
    for (const d of defined) if (!used.has(d)) throw Object.assign(new Error(`${field} sin usar: ${d}`), { name: 'ValidationException' });
    for (const u of used) if (!defined.includes(u)) throw Object.assign(new Error(`${field} sin definir: ${u}`), { name: 'ValidationException' });
  }
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
  /** Si se pone, se ejecuta una vez justo antes de la siguiente operación de ese tipo (para simular carreras). */
  hookNext: { command: string; table?: string; fn: () => void } | undefined;
  /** Claves que el GSI by_uid no enseña en la próxima consulta (consistencia eventual del índice). */
  hiddenFromIndexOnce = new Set<string>();

  constructor() {
    const m = mockClient(DynamoDBDocumentClient);
    this.mock = m;
    const guard = (command: string, table?: string): void => {
      if (this.hookNext?.command === command && (this.hookNext.table === undefined || this.hookNext.table === table)) {
        const fn = this.hookNext.fn;
        this.hookNext = undefined;
        fn();
      }
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
        checkAliases(i);
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
        checkAliases(i);
        const t = this.table(i.TableName);
        const key = String(i.Key[PK[i.TableName]]);
        const cur = t.get(key);
        if (i.ConditionExpression && cur && cur.uid !== i.ExpressionAttributeValues?.[':u']) throw conditionalFailed();
        t.delete(key);
        return Promise.resolve((i as { ReturnValues?: string }).ReturnValues === 'ALL_OLD' && cur ? { Attributes: structuredClone(cur) } : {});
      },
    );
    m.on(UpdateCommand).callsFake(
      (i: { TableName: string; Key: Item; UpdateExpression: string; ConditionExpression?: string; ExpressionAttributeValues: Item }) => {
        guard('Update', i.TableName);
        checkAliases(i);
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
        if (i.UpdateExpression === 'SET token_hash = :th, #t = :ttl') {
          const ok = !cur || cur.token_hash === v[':th'] || (cur.ttl !== undefined && Number(cur.ttl) <= Number(v[':now']));
          if (!ok) throw conditionalFailed();
          t.set(key, { [pk]: key, ...(cur ?? {}), token_hash: v[':th'], ttl: v[':ttl'] });
          return Promise.resolve({});
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
    m.on(TransactWriteCommand).callsFake((i: { TransactItems: TxItem[] }) => {
      // La tabla del primer elemento identifica la transacción (libro mayor, rewards, códigos o integrity).
      const first = i.TransactItems[0];
      guard('TransactWrite', (first?.Put ?? first?.Update ?? first?.ConditionCheck)?.TableName);
      const seen = new Set<string>();
      const codes = i.TransactItems.map((it) => {
        const op = it.Put ?? it.Update ?? it.ConditionCheck;
        if (!op) throw new Error('MemDb: solo Put, Update y ConditionCheck en transacciones');
        checkAliases(op);
        const key = it.Put ? { [PK[op.TableName]]: it.Put.Item[PK[op.TableName]] } : (op as { Key: Item }).Key;
        // DynamoDB rechaza dos operaciones sobre el mismo ítem en una transacción.
        const id = `${op.TableName}/${String(key[PK[op.TableName]])}`;
        if (seen.has(id)) throw Object.assign(new Error(`Dos operaciones sobre ${id}`), { name: 'ValidationException' });
        seen.add(id);
        const names = (op as { ExpressionAttributeNames?: Record<string, string> }).ExpressionAttributeNames;
        return txConditionHolds(op.ConditionExpression, this.get(op.TableName, key), op.ExpressionAttributeValues, names)
          ? 'None'
          : 'ConditionalCheckFailed';
      });
      if (codes.some((c) => c !== 'None')) throw transactionCanceled(codes);
      for (const it of i.TransactItems) {
        if (it.Put) this.put(it.Put.TableName, it.Put.Item);
        if (it.Update?.UpdateExpression.startsWith('SET #u = :new, reassign_seq = :seq1, ')) {
          // Reasignación de Q-IAP-4: nuevo dueño, secuencia, historial y, si cuenta, contador y marca temporal.
          const v = it.Update.ExpressionAttributeValues ?? {};
          const cur = { ...(this.get(it.Update.TableName, it.Update.Key) ?? {}) };
          const next: Item = {
            ...cur,
            uid: v[':new'],
            reassign_seq: v[':seq1'],
            reassign_log: [...((cur.reassign_log as unknown[] | undefined) ?? []), ...(v[':entry'] as unknown[])],
          };
          if (':cnt' in v) next.reassign_count = v[':cnt'];
          if (':at' in v) next.reassigned_at = v[':at'];
          this.put(it.Update.TableName, next);
        } else if (it.Update) {
          if (it.Update.UpdateExpression !== 'REMOVE #t SET created_at = :now') throw new Error(`Update de transacción no soportado: ${it.Update.UpdateExpression}`);
          const cur = { ...(this.get(it.Update.TableName, it.Update.Key) ?? {}) };
          delete cur.ttl;
          this.put(it.Update.TableName, { ...cur, created_at: it.Update.ExpressionAttributeValues?.[':now'] });
        }
      }
      return Promise.resolve({});
    });
    m.on(QueryCommand).callsFake(
      (i: { TableName: string; IndexName?: string; ExpressionAttributeValues: Item }) => {
        guard('Query', i.TableName);
        checkAliases(i);
        if (i.IndexName !== 'by_uid') throw new Error('MemDb solo consulta by_uid');
        const pk = PK[i.TableName];
        const items = [...this.table(i.TableName).values()]
          .filter((it) => it.uid === i.ExpressionAttributeValues[':u'] && !this.hiddenFromIndexOnce.delete(String(it[pk])))
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
