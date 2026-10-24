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
  AF_TABLE_LEAGUE_PLAYERS: 'af-test-league_players',
  AF_TABLE_LEAGUE_BOARD: 'af-test-league_board',
  AF_TABLE_LEAGUE_HISTOGRAM: 'af-test-league_histogram',
  AF_TABLE_LEAGUE_NAMES: 'af-test-league_names',
} as const;

const PK: Record<string, string> = {
  [TABLES.AF_TABLE_PURCHASES]: 'token_hash',
  [TABLES.AF_TABLE_REWARDS]: 'nonce',
  [TABLES.AF_TABLE_INTEGRITY]: 'uid',
  [TABLES.AF_TABLE_ACCOUNT_CODES]: 'code_hash',
  [TABLES.AF_TABLE_RATE_LIMITS]: 'key',
  [TABLES.AF_TABLE_JOBS]: 'job',
  [TABLES.AF_TABLE_LEAGUE_PLAYERS]: 'uid',
  [TABLES.AF_TABLE_LEAGUE_BOARD]: 'scope',
  [TABLES.AF_TABLE_LEAGUE_HISTOGRAM]: 'scope',
  [TABLES.AF_TABLE_LEAGUE_NAMES]: 'uid',
};
const SORT: Record<string,string> = { [TABLES.AF_TABLE_LEAGUE_PLAYERS]:'week', [TABLES.AF_TABLE_LEAGUE_BOARD]:'sort', [TABLES.AF_TABLE_LEAGUE_HISTOGRAM]:'bucket' };
const keyOf = (table: string, item: Item) => String(item[PK[table]]) + (SORT[table] ? `|${String(item[SORT[table]])}` : '');

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
  Delete?: { TableName: string; Key: Item; ConditionExpression?: string; ExpressionAttributeValues?: Item };
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

type Names = Record<string, string> | undefined;

const attrName = (x: string, names: Names): string => (x.startsWith('#') ? (names?.[x] ?? x) : x);

/** Parte `s` por las comas que no están dentro de paréntesis. */
function splitTop(s: string): string[] {
  const out: string[] = [];
  let depth = 0;
  let cur = '';
  for (const ch of s) {
    if (ch === '(') depth++;
    if (ch === ')') depth--;
    if (ch === ',' && depth === 0) {
      out.push(cur.trim());
      cur = '';
    } else cur += ch;
  }
  if (cur.trim() !== '') out.push(cur.trim());
  return out;
}

/**
 * Condiciones de DynamoDB que usan las Lambdas en transacciones: `OR` de `AND` de términos `attribute_exists(x)`,
 * `attribute_not_exists(x)` y `x <op> :v` (=, <>, <, <=), con `x` un atributo o un alias `#n`. Un ítem que no
 * existe no tiene atributos. Lo que no entiende, lanza (el test falla en vez de aprobar por error).
 */
function conditionHolds(cond: string | undefined, cur: Item | undefined, names: Names, values: Item | undefined): boolean {
  if (cond === undefined) return true;
  const it = cur ?? {};
  const term = (t: string): boolean => {
    const fn = /^(attribute_exists|attribute_not_exists)\(([#A-Za-z_]+)\)$/.exec(t);
    if (fn) return (it[attrName(fn[2], names)] !== undefined) === (fn[1] === 'attribute_exists');
    const cmp = /^([#A-Za-z_]+) (=|<>|<=|<) (:[A-Za-z0-9_]+)$/.exec(t);
    if (!cmp) throw new Error(`Término no soportado en MemDb: ${t}`);
    const a = it[attrName(cmp[1], names)];
    const b = values?.[cmp[3]];
    if (cmp[2] === '=') return a === b;
    if (cmp[2] === '<>') return a !== b;
    if (a === undefined) return false;
    return cmp[2] === '<' ? Number(a) < Number(b) : Number(a) <= Number(b);
  };
  return cond.split(' OR ').some((alt) => alt.split(' AND ').every((t) => term(t.trim())));
}

/**
 * Aplica una UpdateExpression (SET, ADD, REMOVE) a una copia del ítem: valores `:v`, `if_not_exists(x, :v)` y
 * `list_append(a, b)`. Lo que no entiende, lanza.
 */
function applyUpdate(cur: Item, expr: string, names: Names, values: Item | undefined): Item {
  const next: Item = structuredClone(cur);
  const value = (raw: string): unknown => {
    const v = raw.trim();
    if (v.startsWith(':')) {
      if (!(v in (values ?? {}))) throw new Error(`Valor sin definir: ${v}`);
      return values?.[v];
    }
    const call = /^(if_not_exists|list_append)\((.*)\)$/.exec(v);
    if (call) {
      const [a, b] = splitTop(call[2]);
      if (call[1] === 'if_not_exists') {
        const x = next[attrName(a, names)];
        return x !== undefined ? x : value(b);
      }
      return [...(value(a) as unknown[]), ...(value(b) as unknown[])];
    }
    if (/^[#A-Za-z_]+$/.test(v)) return next[attrName(v, names)];
    throw new Error(`Valor no soportado en MemDb: ${v}`);
  };
  const marks = [...expr.matchAll(/\b(SET|ADD|REMOVE)\s+/g)];
  if (marks.length === 0 || marks[0].index !== 0) throw new Error(`UpdateExpression no soportada en MemDb: ${expr}`);
  marks.forEach((m, i) => {
    const body = expr.slice(m.index + m[0].length, i + 1 < marks.length ? marks[i + 1].index : undefined);
    for (const clause of splitTop(body)) {
      if (m[1] === 'REMOVE') delete next[attrName(clause, names)];
      else if (m[1] === 'ADD') {
        const [path, v] = clause.split(/\s+/);
        next[attrName(path, names)] = Number(next[attrName(path, names)] ?? 0) + Number(value(v));
      } else {
        const eq = clause.indexOf('=');
        next[attrName(clause.slice(0, eq).trim(), names)] = value(clause.slice(eq + 1));
      }
    }
  });
  return next;
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
      const key = keyOf(i.TableName,i.Item);
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
        const key = keyOf(i.TableName,i.Key);
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
        const key = keyOf(i.TableName,i.Key);
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
      guard('TransactWrite', (first?.Put ?? first?.Update ?? first?.Delete ?? first?.ConditionCheck)?.TableName);
      const seen = new Set<string>();
      const codes = i.TransactItems.map((it) => {
        const op = it.Put ?? it.Update ?? it.Delete ?? it.ConditionCheck;
        if (!op) throw new Error('MemDb: solo Put, Update y ConditionCheck en transacciones');
        checkAliases(op);
        const key = it.Put ? it.Put.Item : (op as { Key: Item }).Key;
        // DynamoDB rechaza dos operaciones sobre el mismo ítem en una transacción.
        const id = `${op.TableName}/${keyOf(op.TableName,key)}`;
        if (seen.has(id)) throw Object.assign(new Error(`Dos operaciones sobre ${id}`), { name: 'ValidationException' });
        seen.add(id);
        const names = (op as { ExpressionAttributeNames?: Record<string, string> }).ExpressionAttributeNames;
        return conditionHolds(op.ConditionExpression, this.get(op.TableName, key), names, op.ExpressionAttributeValues)
          ? 'None'
          : 'ConditionalCheckFailed';
      });
      if (codes.some((c) => c !== 'None')) throw transactionCanceled(codes);
      for (const it of i.TransactItems) {
        if (it.Put) this.put(it.Put.TableName, it.Put.Item);
        if (it.Delete) this.table(it.Delete.TableName).delete(keyOf(it.Delete.TableName,it.Delete.Key));
        if (it.Update) {
          // Cualquier UpdateExpression que entienda applyUpdate (reasignación de Q-IAP-4, cupos, marca `once`).
          const u = it.Update;
          const cur = this.get(u.TableName, u.Key) ?? { ...u.Key };
          this.put(u.TableName, applyUpdate(cur, u.UpdateExpression, u.ExpressionAttributeNames, u.ExpressionAttributeValues));
        }
      }
      return Promise.resolve({});
    });
    m.on(QueryCommand).callsFake(
      (i: { TableName: string; IndexName?: string; ExpressionAttributeValues: Item; Limit?: number; ExclusiveStartKey?: Item }) => {
        guard('Query', i.TableName);
        checkAliases(i);
        if (!i.IndexName && i.TableName.includes('league_')) {
          const values = Object.values(i.ExpressionAttributeValues);
          const pk = PK[i.TableName], sort = SORT[i.TableName];
          const all = this.items(i.TableName).filter(it=>it[pk]===values[0]).sort((a,b)=>sort==='bucket'?Number(a[sort])-Number(b[sort]):String(a[sort]).localeCompare(String(b[sort])));
          const offset = i.ExclusiveStartKey ? all.findIndex(it=>keyOf(i.TableName,it)===keyOf(i.TableName,i.ExclusiveStartKey!))+1 : 0;
          const items = all.slice(offset,offset+(i.Limit??all.length));
          return Promise.resolve({Items:structuredClone(items)});
        }
        if (i.IndexName !== 'by_uid') throw new Error('MemDb solo consulta by_uid o Liga');
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
          const key = keyOf(table,k);
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
    return this.table(table).get(keyOf(table,key));
  }

  put(table: string, item: Item): void {
    this.table(table).set(keyOf(table,item), structuredClone(item));
  }

  items(table: string): Item[] {
    return [...this.table(table).values()];
  }

  restore(): void {
    this.mock.restore();
  }
}
