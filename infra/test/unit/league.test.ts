import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { createLeagueHandler } from '../../functions/league/http.ts';
import { board, eraseLeague, aliasOf, submit } from '../../functions/league/service.ts';
import type { LeagueDeps } from '../../functions/league/service.ts';
import { countryOf, evaluate, NAME_KEYS, percentile, weekOf } from '../../functions/league/rules.ts';
import type { Request, Win } from '../../functions/league/rules.ts';
import { markDeleted } from '../../functions/shared/rateLimit.ts';
import { uidHash } from '../../functions/shared/log.ts';
import { createHandler as deleteHandler } from '../../functions/delete-account/handler.ts';
import { validator, readExample } from '../contract/ajv.ts';
import { httpEvent, UID, OTHER_UID } from './fixtures.ts';
import { MemDb, TABLES, transactionCanceled } from './memdb.ts';
const now = Date.parse('2026-10-23T20:00:00Z');
const campaign = (level: number, at = now-60000): Win => ({win_id:`c:${level}`,kind:'campaign',level,won_at:new Date(at).toISOString()});
const request = (wins: Win[], region = 'CO'): Request => ({wins,max_level:700,region});

describe('Liga: reglas',()=>{
  it('semana lunes 12 UTC y cambio de año ISO',()=>{
    expect(weekOf(Date.parse('2026-10-26T11:59:59Z')).week).toBe('2026-W43');
    expect(weekOf(Date.parse('2026-10-26T12:00:00Z')).week).toBe('2026-W44');
    expect(weekOf(Date.parse('2027-01-01T20:00:00Z')).week).toBe('2026-W53');
  });
  it('puntos calculados desde packs y duplicados semanales',()=>{
    const initial = evaluate(undefined,request([campaign(41),campaign(42),campaign(41)]),UID,now,0);
    expect(initial.next.points).toBe(20);
    expect(initial.accepted).toEqual(['c:41','c:42']);
    expect(initial.rejected).toEqual([{win_id:'c:41',reason:'duplicate'}]);
    const daily: Win = {win_id:'d:2026-10-23',kind:'daily',day:'2026-10-23',won_at:'2026-10-23T18:00:00Z'};
    const result = evaluate(initial.next,request([daily,daily]),UID,now,42);
    expect(result.next.points).toBe(40);
    expect(result.rejected[0].reason).toBe('duplicate');
    expect(evaluate(undefined,request([campaign(46)]),UID,now,0).next.points).toBe(20);
    expect(evaluate(undefined,request([campaign(60)]),UID,now,0).next.points).toBe(30);
  });
  it('primer envío establece referencia; salto de un nivel y saltos imposibles',()=>{
    const first = evaluate(undefined,request([campaign(57)]),UID,now,0);
    const next = evaluate(first.next,request([campaign(59),campaign(64)]),UID,now,57);
    expect(next.accepted).toEqual(['c:59']);
    expect(next.rejected).toEqual([{win_id:'c:64',reason:'unreachable_level'}]);
    expect(next.next.flagged).toBe(true);
    expect(evaluate(undefined,{wins:[campaign(57)],max_level:56},UID,now,0).rejected[0].reason).toBe('unreachable_level');
    expect(evaluate(undefined,request([campaign(40)]),UID,now,0).accepted).toEqual([]);
  });
  it('fechas fuera de semana, futuro e ID inconsistente',()=>{
    const result = evaluate(undefined,request([campaign(41,weekOf(now).start-1),campaign(42,now+1),{...campaign(43),win_id:'c:99'}]),UID,now,0);
    expect(result.rejected.map(r=>r.reason)).toEqual(['outside_week','outside_week','invalid']);
  });
  it('el límite horario cruza el lunes aunque los puntos se reinicien',()=>{
    const monday = Date.parse('2026-10-26T12:30:00Z');
    const previous = Array.from({length:40},(_,i)=>({id:`c:${41+i}`,at:monday-35*60000+i*1000}));
    const result = evaluate(undefined,request([campaign(81,monday)]),UID,monday,80,previous);
    expect(result.rejected[0].reason).toBe('rate_limited');
    expect(result.next.points).toBe(0);
  });
  it('Reto: fecha local de inicio, cruce de medianoche y una sola recompensa por día',()=>{
    const stamp = Date.parse('2026-10-24T06:00:00Z');
    const win: Win = {win_id:'d:2026-10-23',kind:'daily',day:'2026-10-23',won_at:new Date(stamp).toISOString()};
    const first = evaluate(undefined,request([win,win]),UID,stamp,41);
    expect(first.accepted).toEqual(['d:2026-10-23']);
    expect(first.next.points).toBe(20);
    expect(first.rejected[0].reason).toBe('duplicate');
    // UTC+14: el día local puede ser el siguiente al de won_at UTC.
    expect(evaluate(undefined,request([{...win,day:'2026-10-24',win_id:'d:2026-10-24',won_at:'2026-10-23T18:00:00Z'}]),UID,stamp,41).accepted).toHaveLength(1);
    // La fecha de inicio local del domingo puede ganar tras el cambio semanal UTC del lunes.
    const monday = Date.parse('2026-10-26T12:01:00Z');
    expect(evaluate(undefined,request([{...win,day:'2026-10-25',win_id:'d:2026-10-25',won_at:new Date(monday).toISOString()}]),UID,monday,41).accepted).toHaveLength(1);
    for(const [day,reason] of [['2026-02-30','invalid'],['2026-01-01','outside_week'],['2026-10-30','outside_week']]) {
      const result = evaluate(undefined,request([{...win,day,win_id:`d:${day}`}]),UID,stamp,41);
      expect(result.rejected[0].reason).toBe(reason);
    }
  });
  it('40/hora móvil: lotes offline y desordenados no evaden el límite',()=>{
    const wins = Array.from({length:41},(_,i)=>campaign(41+i,now-50*60000+i*30000));
    const result = evaluate(undefined,request(wins),UID,now,0);
    expect(result.accepted).toHaveLength(40);
    expect(result.rejected).toEqual([{win_id:'c:81',reason:'rate_limited'}]);
    const older = campaign(81,now-51*60000);
    expect(evaluate(result.next,request([older]),UID,now,80).rejected[0].reason).toBe('rate_limited');
    expect(evaluate(result.next,request([campaign(81,now)]),UID,now,80).accepted).toEqual([]);
    // 41 victorias repartidas en más de una hora sí pueden viajar en un solo lote offline.
    expect(evaluate(undefined,request(wins.map((w,i)=>({...w,won_at:new Date(now-(41-i)*120000).toISOString()}))),UID,now,0).accepted).toHaveLength(41);
  });
  it('país ISO, percentil por histograma y las 24 claves exactas',()=>{
    expect(countryOf('CO')).toBe('CO');
    for(const c of ['ZZ','XK','EU','co','']) expect(countryOf(c)).toBeNull();
    expect(NAME_KEYS).toHaveLength(24);
    expect(NAME_KEYS[0]).toBe('league_name_capybara');
    expect(NAME_KEYS[23]).toBe('league_name_arapaima');
    expect(percentile(140,[{bucket:1,count:20},{bucket:2,count:10},{bucket:0,count:70}])).toBe(20);
    expect(percentile(0,[])).toBeNull();
  });
});

describe('Liga: transacciones, top y borrado',()=>{
  let db: MemDb, deps: LeagueDeps, n: number;
  beforeEach(()=>{db=new MemDb();n=0;deps={ddb:db.client,env:{...TABLES,AF_ENV:'dev'},now:()=>now,pick:max=>(n++)%max};});
  afterEach(()=>db.restore());
  it('envío idempotente, país fijado al aceptar y ninguna exposición del UID',async()=>{
    const result = await submit(deps,UID,request([campaign(57)]));
    expect(validator('league-wins.response.schema.json')(result)).toBe(true);
    expect((await submit(deps,UID,request([campaign(57)],'BR'))).rejected[0].reason).toBe('duplicate');
    const view = await board(deps,UID,'global');
    expect(view.country).toBe('CO');
    expect(view.me.rank).toBe(1);
    expect(view.top).toHaveLength(1);
    expect(JSON.stringify(view)).not.toContain(UID);
    expect(validator('league-board.response.schema.json')(view)).toBe(true);
    expect((await board(deps,UID,'country')).top).toHaveLength(1);
    expect(db.items(TABLES.AF_TABLE_LEAGUE_HISTOGRAM).map(i=>i.count)).toEqual([1,1]);
  });
  it('sin puntos, país desconocido y cabecera de país falsificada ignorada',async()=>{
    const view = await board(deps,UID,'global');
    expect(view.me).toMatchObject({points:0,rank:null,percentile:null});
    await expect(board(deps,UID,'country')).rejects.toMatchObject({status:404,code:'country_unknown'});
    await submit(deps,UID,request([campaign(57)],'ZZ'));
    expect((await board(deps,UID,'global')).me.country).toBeNull();
  });
  it('HTTP no confía en CloudFront-Viewer-Country en el endpoint directo',async()=>{
    db.put(TABLES.AF_TABLE_INTEGRITY,{uid:UID,verdict:'ok',ttl:Math.floor(now/1000)+3600});
    const wins = createLeagueHandler('wins',{...deps,integrity:{variant:'app-check',verify:()=>Promise.resolve({ok:true})}});
    const response = await wins(httpEvent({routeKey:'POST /v1/league/wins',uid:UID,body:request([campaign(41)]),headers:{'cloudfront-viewer-country':'BR'}}));
    expect(response.statusCode).toBe(200);
    expect((JSON.parse(response.body!) as {country:string}).country).toBe('CO');
  });
  it('empates ordenados por llegada; actualización elimina la antigua fila',async()=>{
    await submit(deps,UID,request([campaign(41)]));
    await submit({...deps,now:()=>now+1000},OTHER_UID,request([campaign(41)]));
    expect((await board(deps,OTHER_UID,'global')).me.rank).toBe(2);
    await submit({...deps,now:()=>now+2000},OTHER_UID,request([campaign(42)]));
    const view = await board(deps,UID,'global');
    expect(view.top).toHaveLength(2);
    expect(view.me.rank).toBe(2);
    expect(db.items(TABLES.AF_TABLE_LEAGUE_HISTOGRAM).map(i=>i.count)).toEqual([2,2]);
  });
  it('top exacto de 100 y percentil para el resto, sin recorrer jugadores',async()=>{
    for(let i=0;i<101;i++) await submit({...deps,now:()=>now+i*1000},`Player${i}`,request([campaign(41)]));
    const view = await board({...deps,now:()=>now+200000},'Player100','global');
    expect(view.top).toHaveLength(100);
    expect(view.me.rank).toBeNull();
    expect(view.me.percentile).toBe(50);
  });
  it('flagged sigue sumando para sí y se retira de top/histograma',async()=>{
    await submit(deps,UID,request([campaign(41)]));
    await submit(deps,UID,request([campaign(50)]));
    const own = await submit(deps,UID,request([campaign(42)]));
    expect(own.points).toBe(20);
    expect((await board(deps,UID,'global')).top).toEqual([]);
    expect(db.items(TABLES.AF_TABLE_LEAGUE_HISTOGRAM).map(i=>i.count)).toEqual([0,0]);
  });
  it('concurrencia reintenta desde estado fresco sin sumar dos veces',async()=>{
    await aliasOf(deps,UID);
    db.failNext={command:'TransactWrite',table:TABLES.AF_TABLE_LEAGUE_PLAYERS,error:transactionCanceled(['TransactionConflict'])};
    await Promise.all([submit(deps,UID,request([campaign(41)])),submit(deps,UID,request([campaign(41)]))]);
    expect((await board(deps,UID,'global')).me.points).toBe(10);
    expect(db.items(TABLES.AF_TABLE_LEAGUE_BOARD)).toHaveLength(2);
  });
  it('colisiones de seudónimo reservadas atómicamente',async()=>{
    const a = await aliasOf({...deps,pick:()=>0},UID);
    let pick = 0;
    const b = await aliasOf({...deps,pick:max=>max===24?0:(pick++===0?0:1)},OTHER_UID);
    expect(a.name_key).toBe(b.name_key);
    expect(a.name_num).not.toBe(b.name_num);
  });
  it('deleteAccount elimina semanas, clasificaciones, seudónimo y reserva; impide recrearlos',async()=>{
    await submit({...deps,now:()=>now-7*86400_000},UID,request([campaign(41,now-7*86400_000-60000)]));
    await submit(deps,UID,request([campaign(41)]));
    await submit(deps,OTHER_UID,request([campaign(41)]));
    db.put(TABLES.AF_TABLE_INTEGRITY,{uid:UID,verdict:'ok',ttl:Math.floor(now/1000)+3600});
    const handler = deleteHandler(()=>({...deps,integrity:{variant:'app-check',verify:()=>Promise.resolve({ok:true})},firebase:{deleteUser:()=>Promise.resolve({existed:true})},random:n=>Buffer.alloc(n,1)}));
    const response = await handler(httpEvent({routeKey:'POST /v1/account/delete',uid:UID,body:readExample('account-delete.request.json')}));
    expect(response.statusCode).toBe(200);
    expect(db.items(TABLES.AF_TABLE_LEAGUE_PLAYERS).every(p=>p.uid!==UID)).toBe(true);
    expect(db.get(TABLES.AF_TABLE_LEAGUE_NAMES,{uid:UID})).toBeUndefined();
    expect(db.items(TABLES.AF_TABLE_LEAGUE_NAMES).some(i=>i.owner===UID)).toBe(false);
    expect((await board(deps,OTHER_UID,'global')).top).toHaveLength(1);
    await expect(submit(deps,UID,request([campaign(42)]))).rejects.toMatchObject({status:401});
    await eraseLeague(deps,UID);
    expect(db.items(TABLES.AF_TABLE_LEAGUE_HISTOGRAM).filter(i=>String(i.scope).startsWith(weekOf(now).week)).map(i=>i.count)).toEqual([1,1]);
  });
  it('borrado que gana la carrera bloquea transacción y no deja puntos',async()=>{
    await aliasOf(deps,UID);
    db.hookNext={command:'TransactWrite',table:TABLES.AF_TABLE_LEAGUE_PLAYERS,fn:()=>db.put(TABLES.AF_TABLE_RATE_LIMITS,{key:`deleted#${uidHash(UID)}`,ttl:Math.floor(now/1000)+7200})};
    await expect(submit(deps,UID,request([campaign(41)]))).rejects.toMatchObject({status:401});
    expect(db.items(TABLES.AF_TABLE_LEAGUE_BOARD)).toEqual([]);
  });
  it('dos borrados concurrentes no liberan un seudónimo reasignado a otro jugador',async()=>{
    const alias = await aliasOf(deps,UID);
    await markDeleted(db.client,TABLES.AF_TABLE_RATE_LIMITS,uidHash(UID),now);
    db.hookNext={command:'TransactWrite',table:TABLES.AF_TABLE_LEAGUE_NAMES,fn:()=>{
      db.table(TABLES.AF_TABLE_LEAGUE_NAMES).delete(UID);
      db.put(TABLES.AF_TABLE_LEAGUE_NAMES,{uid:alias.reservation,owner:OTHER_UID});
    }};
    await eraseLeague(deps,UID);
    expect(db.get(TABLES.AF_TABLE_LEAGUE_NAMES,{uid:alias.reservation})?.owner).toBe(OTHER_UID);
  });
  it('HTTP exige JWT y App Check en ambos endpoints y valida el cuerpo',async()=>{
    const integrity = {variant:'app-check' as const,verify:()=>Promise.resolve({ok:false})};
    const wins = createLeagueHandler('wins',{...deps,integrity});
    const get = createLeagueHandler('board',{...deps,integrity});
    expect((await wins(httpEvent({routeKey:'POST /v1/league/wins',body:request([campaign(41)])}))).statusCode).toBe(401);
    expect((await wins(httpEvent({routeKey:'POST /v1/league/wins',uid:UID,body:request([campaign(41)])}))).statusCode).toBe(403);
    expect((await get(httpEvent({routeKey:'GET /v1/league',uid:UID}))).statusCode).toBe(403);
    expect((await wins(httpEvent({routeKey:'POST /v1/league/wins',uid:UID,body:{points:100}}))).statusCode).toBe(400);
    await markDeleted(db.client,TABLES.AF_TABLE_RATE_LIMITS,uidHash(UID),now);
  });
});
