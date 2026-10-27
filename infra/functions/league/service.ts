import { randomInt } from 'node:crypto';
import { GetCommand, QueryCommand, TransactWriteCommand } from '@aws-sdk/lib-dynamodb';
import type { DynamoDBDocumentClient, TransactWriteCommandInput } from '@aws-sdk/lib-dynamodb';
import { cancellationCodes } from '../shared/ddb.ts';
import { HttpError, rateLimited, unauthorized } from '../shared/errors.ts';
import type { Env } from '../shared/env.ts';
import { requireEnv } from '../shared/env.ts';
import { uidHash } from '../shared/log.ts';
import { isDeleted, notDeletedCheck } from '../shared/rateLimit.ts';
import { ASSIGNABLE_NAME_KEYS, AVATAR_CHANGES_PER_DAY, bucketOf, evaluate, isAvatar, percentile, rankKey, scopesOf, visible, weekOf } from './rules.ts';
import type { Player, Request } from './rules.ts';

type Tx = NonNullable<TransactWriteCommandInput['TransactItems']>;
export interface LeagueDeps { ddb: DynamoDBDocumentClient; env: Env; now: () => number; pick?: (max: number) => number }
export interface Alias {
  uid: string; name_key: string; name_num: number; highest: number; rev: number; reservation: string;
  /** Avatar elegido (D-056; 0-63), con el seudónimo y copiado a las filas del top. Sin él, nunca eligió. */
  avatar?: number;
  /** Cupo de cambios de avatar del día UTC [P], en la misma fila: se borra con el seudónimo. */
  avatar_day?: string; avatar_changes?: number;
}
/** El avatar público de una fila del seudónimo o del top: null si nunca eligió (o la fila es anterior a D-056). */
export const avatarOf = (row: { avatar?: unknown } | undefined): number | null => isAvatar(row?.avatar) ? row.avatar : null;
/**
 * Campo `avatar` de las respuestas (contrato v1, opcional): solo si el jugador eligió uno. Sin elegir, se omite y
 * la respuesta es idéntica a la anterior a D-056; el esquema admite también null.
 */
const avatarField = (row: { avatar?: unknown } | undefined): { avatar?: number } => { const a = avatarOf(row); return a === null ? {} : {avatar:a}; };
export const leagueTables = (env: Env) => ({
  players: requireEnv(env,'AF_TABLE_LEAGUE_PLAYERS'), board: requireEnv(env,'AF_TABLE_LEAGUE_BOARD'),
  histogram: requireEnv(env,'AF_TABLE_LEAGUE_HISTOGRAM'), names: requireEnv(env,'AF_TABLE_LEAGUE_NAMES'),
  rateLimits: requireEnv(env,'AF_TABLE_RATE_LIMITS'),
});
type Tables = ReturnType<typeof leagueTables>;
const unavailable = () => new HttpError(503,'upstream_error','Liga ocupada; reintenta más tarde.',1);
function retryable(e: unknown): boolean {
  const codes = cancellationCodes(e);
  return !!codes && codes.some(c => ['ConditionalCheckFailed','TransactionConflict'].includes(c)) || e instanceof Error && e.name === 'TransactionConflictException';
}
export async function liveAccount(deps: LeagueDeps, uid: string): Promise<void> {
  if (await isDeleted(deps.ddb,requireEnv(deps.env,'AF_TABLE_RATE_LIMITS'),uidHash(uid),deps.now())) throw unauthorized();
}
async function read<T>(deps: LeagueDeps, table: string, key: Record<string,string>): Promise<T | undefined> {
  return (await deps.ddb.send(new GetCommand({ TableName: table, Key: key, ConsistentRead: true }))).Item as T | undefined;
}
export async function aliasOf(deps: LeagueDeps, uid: string): Promise<Alias> {
  const t = leagueTables(deps.env);
  for (let attempt=0;attempt<64;attempt++) {
    await liveAccount(deps,uid);
    const existing = await read<Alias>(deps,t.names,{uid});
    if (existing) return existing;
    const pick = deps.pick ?? ((n: number) => randomInt(n));
    // Sin la anaconda en los seudónimos nuevos (D-057, Q-AV-12): los que ya la tienen la conservan.
    const name_key = ASSIGNABLE_NAME_KEYS[pick(ASSIGNABLE_NAME_KEYS.length)], name_num = 1000 + pick(9000);
    const reservation = `name#${name_key}#${name_num}`;
    const alias: Alias = {uid,name_key,name_num,highest:0,rev:0,reservation};
    try {
      await deps.ddb.send(new TransactWriteCommand({ TransactItems: [
        {Put:{TableName:t.names,Item:alias,ConditionExpression:'attribute_not_exists(uid)'}},
        {Put:{TableName:t.names,Item:{uid:reservation,owner:uid},ConditionExpression:'attribute_not_exists(uid)'}},
        notDeletedCheck(t.rateLimits,uidHash(uid),deps.now()),
      ] }));
      return alias;
    } catch(e) { if (!retryable(e)) throw e; }
  }
  throw unavailable();
}
/** Fila pública del top: sin UID ni nada del jugador fuera de seudónimo, avatar, país y puntos. */
function boardItem(scope: string, p: Player, alias: Alias) {
  return {scope,sort:rankKey(p),points:p.points,country:p.country,name_key:alias.name_key,name_num:alias.name_num,...avatarField(alias),ttl:p.ttl};
}
export function boardChanges(t: Tables, before: Player | undefined, after: Player | undefined, alias: Alias): Tx {
  const tx: Tx = [];
  if (before && after && rankKey(before) === rankKey(after) && visible(before) === visible(after)) return tx;
  const changes = new Map<string,{scope:string;bucket:number;delta:number;ttl:number}>();
  for (const [p,delta] of [[before,-1],[after,1]] as const) {
    if (!visible(p)) continue;
    for (const scope of scopesOf(p)) {
      if (delta === -1) tx.push({Delete:{TableName:t.board,Key:{scope,sort:rankKey(p)}}});
      else tx.push({Put:{TableName:t.board,Item:boardItem(scope,p,alias)}});
      const bucket = bucketOf(p.points), id = `${scope}|${bucket}`;
      const c = changes.get(id) ?? {scope,bucket,delta:0,ttl:p.ttl};
      c.delta += delta;
      changes.set(id,c);
    }
  }
  for (const c of changes.values()) if (c.delta) tx.push({Update:{
    TableName:t.histogram,Key:{scope:c.scope,bucket:c.bucket},UpdateExpression:'SET #t = :t ADD #c :d',
    ExpressionAttributeNames:{'#t':'ttl','#c':'count'},ExpressionAttributeValues:{':t':c.ttl,':d':c.delta},
  }});
  return tx;
}
export async function submit(deps: LeagueDeps, uid: string, req: Request) {
  const t = leagueTables(deps.env), now = deps.now(), week = weekOf(now);
  await aliasOf(deps,uid);
  for(let attempt=0;attempt<8;attempt++) {
    await liveAccount(deps,uid);
    const alias = await read<Alias>(deps,t.names,{uid});
    if (!alias) throw unauthorized();
    const old = await read<Player>(deps,t.players,{uid,week:week.week});
    // También si el lote llega días después: won_at puede ser de la primera hora del lunes.
    // Las fechas locales de Reto que cruzan el lunes no pueden cobrarse en dos semanas.
    const previous = await read<Player>(deps,t.players,{uid,week:weekOf(week.start-1).week});
    const result = evaluate(old,req,uid,now,alias.highest,previous?.wins);
    const next = result.next;
    try {
      await deps.ddb.send(new TransactWriteCommand({ TransactItems:[
        {Put:{TableName:t.players,Item:next,ConditionExpression:old ? '#r = :r' : 'attribute_not_exists(uid)',...(old ? {ExpressionAttributeNames:{'#r':'rev'},ExpressionAttributeValues:{':r':old.rev}} : {})}},
        {Put:{TableName:t.names,Item:{...alias,highest:result.highest,rev:alias.rev+1},ConditionExpression:'#r = :r',ExpressionAttributeNames:{'#r':'rev'},ExpressionAttributeValues:{':r':alias.rev}}},
        ...boardChanges(t,old,next,alias),
        notDeletedCheck(t.rateLimits,uidHash(uid),now),
      ] }));
      return {week:week.week,points:next.points,accepted:result.accepted,rejected:result.rejected,name_key:alias.name_key,name_num:alias.name_num,country:next.country,...avatarField(alias)};
    } catch(e) { if (!retryable(e)) throw e; }
  }
  throw unavailable();
}
export async function board(deps: LeagueDeps, uid: string, scope: 'global'|'country') {
  const t = leagueTables(deps.env), now = deps.now(), week = weekOf(now);
  const alias = await aliasOf(deps,uid);
  const player = await read<Player>(deps,t.players,{uid,week:week.week});
  const country = player?.country ?? null;
  if (scope === 'country' && !country) throw new HttpError(404,'country_unknown','País de la semana desconocido.');
  const partition = `${week.week}#${scope === 'country' ? country : 'global'}`;
  const rows = (await deps.ddb.send(new QueryCommand({TableName:t.board,KeyConditionExpression:'#s = :s',ExpressionAttributeNames:{'#s':'scope'},ExpressionAttributeValues:{':s':partition},ConsistentRead:true,Limit:100}))).Items ?? [];
  const meKey = player ? rankKey(player) : '';
  const index = rows.findIndex(row=>row.sort === meKey);
  let percent: number | null = null;
  if ((player?.points ?? 0)>0 && index < 0) {
    const bins: Array<{bucket:number;count:number}> = [];
    let start: Record<string,unknown> | undefined;
    do {
      const out = await deps.ddb.send(new QueryCommand({TableName:t.histogram,KeyConditionExpression:'#s = :s',ExpressionAttributeNames:{'#s':'scope'},ExpressionAttributeValues:{':s':partition},ConsistentRead:true,ExclusiveStartKey:start}));
      bins.push(...(out.Items ?? []) as Array<{bucket:number;count:number}>);
      start = out.LastEvaluatedKey;
    } while(start);
    percent = percentile(player!.points,bins);
  }
  // Nunca devolver sort (contiene UID) ni claves/TTL internos a otros jugadores.
  const top = rows.map((row,i)=>({rank:i+1,name_key:String(row.name_key),name_num:Number(row.name_num),country:typeof row.country==='string'?row.country:null,points:Number(row.points),...avatarField(row)}));
  await liveAccount(deps,uid);
  return {week:week.week,starts_at:new Date(week.start).toISOString(),ends_at:new Date(week.end).toISOString(),scope,country,updated_at:new Date(now).toISOString(),top,me:{points:player?.points??0,rank:index>=0?index+1:null,percentile:percent,name_key:alias.name_key,name_num:alias.name_num,country,...avatarField(alias)}};
}
/**
 * POST /v1/league/avatar (D-056; avatares.md §4.4): guarda el avatar con el seudónimo (lo crea si falta) y, en la
 * misma transacción, reescribe las filas del top de la semana en curso. Repetir el mismo avatar no gasta cupo.
 */
export async function setAvatar(deps: LeagueDeps, uid: string, avatar: number): Promise<{ avatar: number }> {
  const t = leagueTables(deps.env), now = deps.now(), week = weekOf(now);
  const day = new Date(now).toISOString().slice(0,10);
  await aliasOf(deps,uid);
  for (let attempt=0;attempt<8;attempt++) {
    await liveAccount(deps,uid);
    const alias = await read<Alias>(deps,t.names,{uid});
    if (!alias) throw unauthorized();
    if (avatarOf(alias) === avatar) return {avatar};
    const used = alias.avatar_day === day ? Number(alias.avatar_changes ?? 0) : 0;
    if (used >= AVATAR_CHANGES_PER_DAY) throw rateLimited((Date.parse(`${day}T00:00:00Z`) + 86400_000 - now) / 1000);
    const next: Alias = {...alias,avatar,avatar_day:day,avatar_changes:used+1,rev:alias.rev+1};
    const player = await read<Player>(deps,t.players,{uid,week:week.week});
    // Toda victoria aceptada sube rev del seudónimo, así que su condición ya detecta un cambio del top; la del
    // jugador protege además de una marca manual o un borrado que no pase por el seudónimo.
    const rows: Tx = visible(player) ? [
      {ConditionCheck:{TableName:t.players,Key:{uid,week:player.week},ConditionExpression:'#r = :r',ExpressionAttributeNames:{'#r':'rev'},ExpressionAttributeValues:{':r':player.rev}}},
      ...scopesOf(player).map(scope => ({Put:{TableName:t.board,Item:boardItem(scope,player,next)}})),
    ] : [];
    try {
      await deps.ddb.send(new TransactWriteCommand({ TransactItems:[
        {Put:{TableName:t.names,Item:next,ConditionExpression:'#r = :r',ExpressionAttributeNames:{'#r':'rev'},ExpressionAttributeValues:{':r':alias.rev}}},
        ...rows,
        notDeletedCheck(t.rateLimits,uidHash(uid),now),
      ] }));
      return {avatar};
    } catch(e) { if (!retryable(e)) throw e; }
  }
  throw unavailable();
}
/** Partición por UID, lectura fuerte y paginada: sin depender del retraso de un GSI. */
export async function eraseLeague(deps: LeagueDeps, uid: string): Promise<void> {
  const t = leagueTables(deps.env);
  const alias = await read<Alias>(deps,t.names,{uid});
  let start: Record<string,unknown> | undefined;
  do {
    const out = await deps.ddb.send(new QueryCommand({TableName:t.players,KeyConditionExpression:'#u = :u',ExpressionAttributeNames:{'#u':'uid'},ExpressionAttributeValues:{':u':uid},ConsistentRead:true,ExclusiveStartKey:start}));
    for (const player of (out.Items ?? []) as Player[]) {
      // La marca de borrado ya impide nuevas escrituras; conservar la condición para reintentos concurrentes.
      try {
        await deps.ddb.send(new TransactWriteCommand({TransactItems:[
          {Delete:{TableName:t.players,Key:{uid,week:player.week},ConditionExpression:'#r = :r',ExpressionAttributeNames:{'#r':'rev'},ExpressionAttributeValues:{':r':player.rev}}},
          ...boardChanges(t,player,undefined,alias ?? {uid,name_key:'',name_num:0,highest:0,rev:0,reservation:''}),
        ]}));
      } catch(e) {
        if (!retryable(e) || await read<Player>(deps,t.players,{uid,week:player.week})) throw e;
      }
    }
    start = out.LastEvaluatedKey;
  } while(start);
  if(alias) {
    try {
      await deps.ddb.send(new TransactWriteCommand({TransactItems:[
        {Delete:{TableName:t.names,Key:{uid}}},
        {Delete:{TableName:t.names,Key:{uid:alias.reservation},ConditionExpression:'attribute_not_exists(uid) OR #o = :u',ExpressionAttributeNames:{'#o':'owner'},ExpressionAttributeValues:{':u':uid}}},
      ]}));
    } catch(e) {
      // Otro borrado ya liberó la pareja y otro jugador la reservó: conservar al nuevo dueño.
      if (!retryable(e) || await read<Alias>(deps,t.names,{uid})) throw e;
    }
  }
}
