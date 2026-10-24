import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { createLeagueHandler } from '../../functions/league/http.ts';
import { evaluate, parseRequest, weekOf } from '../../functions/league/rules.ts';
import { aliasOf, board, submit } from '../../functions/league/service.ts';
import type { LeagueDeps } from '../../functions/league/service.ts';
import { readExample, validator } from '../contract/ajv.ts';
import { httpEvent, UID } from './fixtures.ts';
import { MemDb, TABLES } from './memdb.ts';

const now = Date.parse('2026-10-23T20:00:00Z');
const campaign = (level: number, at = now - 60000) => ({win_id:`c:${level}`,kind:'campaign',level,won_at:new Date(at).toISOString()});
const request = (wins: unknown[], region: unknown = 'CO', max_level = 700) => ({wins,max_level,region});

describe('Liga revisión 1: persistencia del avance sin bloqueo ni marca automática',()=>{
  let db: MemDb, deps: LeagueDeps;
  beforeEach(()=>{db=new MemDb();deps={ddb:db.client,env:{...TABLES,AF_ENV:'dev'},now:()=>now,pick:()=>0};});
  afterEach(()=>db.restore());
  const highest = (db: MemDb) => db.get(TABLES.AF_TABLE_LEAGUE_NAMES,{uid:UID})?.highest;
  const unflagged = (db: MemDb, stamp = now) => expect(db.get(TABLES.AF_TABLE_LEAGUE_PLAYERS,{uid:UID,week:weekOf(stamp).week})?.flagged).toBe(false);

  it('semana sin red: rechazar la cola antigua guarda el avance y acepta el nivel actual',async()=>{
    await submit({...deps,now:()=>now-7*86400_000},UID,request([campaign(41,now-7*86400_000-60000)]));
    const old = await submit(deps,UID,request([campaign(60,now-7*86400_000)]));
    expect(old.rejected).toEqual([{win_id:'c:60',reason:'outside_week'}]);
    expect(highest(db)).toBe(60);
    expect((await submit(deps,UID,request([campaign(61)]))).accepted).toEqual(['c:61']);
    unflagged(db);
  });

  it('41.ª por hora: guarda referencia pese al límite y la siguiente hora vuelve a puntuar',async()=>{
    const result = await submit(deps,UID,request(Array.from({length:41},(_,i)=>campaign(41+i,now-120000+i*1000))));
    expect(result.accepted).toHaveLength(40);
    expect(result.rejected).toEqual([{win_id:'c:81',reason:'rate_limited'}]);
    expect(highest(db)).toBe(81);
    unflagged(db);
    const later = now+3600_000;
    expect((await submit({...deps,now:()=>later},UID,request([campaign(84,later)]))).accepted).toEqual(['c:84']);
    expect((await board({...deps,now:()=>later},UID,'global')).top).toHaveLength(1);
  });

  it.each(['Liga apagada durante el avance','reinstalación que pierde la cola'])('%s: un hueco repone referencia y cuenta',async()=>{
    await submit(deps,UID,request([campaign(41)]));
    // No se recibieron las victorias intermedias; simularlas en el backend sería incorrecto.
    const result = await submit(deps,UID,request([campaign(150)]));
    expect(result.accepted).toEqual(['c:150']);
    expect(result.rejected).toEqual([]);
    expect(highest(db)).toBe(150);
    unflagged(db);
  });

  it('reloj adelantado: guarda el rechazo y sigue contando tras corregir el reloj',async()=>{
    await submit(deps,UID,request([campaign(41)]));
    const result = await submit(deps,UID,request([campaign(60,now+300001)]));
    expect(result.rejected).toEqual([{win_id:'c:60',reason:'outside_week'}]);
    expect(highest(db)).toBe(60);
    expect((await submit(deps,UID,request([campaign(61)]))).accepted).toEqual(['c:61']);
    unflagged(db);
  });

  it('duplicate y level > max_level guardan referencia sin marcar; un ID inválido no la altera',async()=>{
    await submit(deps,UID,request([campaign(60)]));
    const alias = await aliasOf(deps,UID);
    db.put(TABLES.AF_TABLE_LEAGUE_NAMES,{...alias,highest:0});
    expect((await submit(deps,UID,request([campaign(60)]))).rejected[0].reason).toBe('duplicate');
    expect(highest(db)).toBe(60);
    const result = await submit(deps,UID,request([campaign(100)],'CO',99));
    expect(result.rejected).toEqual([{win_id:'c:100',reason:'unreachable_level'}]);
    expect(highest(db)).toBe(100);
    await submit(deps,UID,request([{...campaign(300),win_id:'c:999'}]));
    expect(highest(db)).toBe(100);
    unflagged(db);
  });

  it('lote invertido: procesa campaña por nivel, también al alcanzar 40/h',async()=>{
    const wins = Array.from({length:41},(_,i)=>campaign(41+i)).reverse();
    const result = await submit(deps,UID,request(wins));
    expect(result.accepted).toEqual(Array.from({length:40},(_,i)=>`c:${41+i}`));
    expect(result.rejected).toEqual([{win_id:'c:81',reason:'rate_limited'}]);
    expect(highest(db)).toBe(81);
  });

  it.each(['',null,'co','ZZ',42,{},[]].map(region=>({region})))('HTTP: región $region es desconocida sin perder la victoria',async({region})=>{
    db.put(TABLES.AF_TABLE_INTEGRITY,{uid:UID,verdict:'ok',ttl:Math.floor(now/1000)+3600});
    const handler = createLeagueHandler('wins',{...deps,integrity:{variant:'app-check',verify:()=>Promise.resolve({ok:true})}});
    const body = request([campaign(41)],region);
    expect(validator('league-wins.request.schema.json')(body)).toBe(true);
    const response = await handler(httpEvent({routeKey:'POST /v1/league/wins',uid:UID,body}));
    expect(response.statusCode).toBe(200);
    expect(JSON.parse(response.body!)).toMatchObject({accepted:['c:41'],country:null});
  });

  it('ejemplo de lote mixto: HTTP 200, ambas victorias válidas y cada inválida separada',async()=>{
    db.put(TABLES.AF_TABLE_INTEGRITY,{uid:UID,verdict:'ok',ttl:Math.floor(now/1000)+3600});
    const handler = createLeagueHandler('wins',{...deps,integrity:{variant:'app-check',verify:()=>Promise.resolve({ok:true})}});
    const body = readExample('league-wins.request.mixed.json');
    expect(validator('league-wins.request.schema.json')(body)).toBe(true);
    const response = await handler(httpEvent({routeKey:'POST /v1/league/wins',uid:UID,body}));
    expect(response.statusCode).toBe(200);
    const result: unknown = JSON.parse(response.body!);
    expect(result).toEqual(readExample('league-wins.response.mixed.json'));
    expect(validator('league-wins.response.schema.json')(result)).toBe(true);
    unflagged(db);
  });
});

describe('Liga revisión 1: validación individual y reloj',()=>{
  it('margen inclusivo de cinco minutos sin ampliar el final semanal',()=>{
    const result = evaluate(undefined,request([campaign(41,now+1),campaign(42,now+300000),campaign(43,now+300001)]),UID,now,0);
    expect(result.accepted).toEqual(['c:41','c:42']);
    expect(result.rejected).toEqual([{win_id:'c:43',reason:'outside_week'}]);
    const nearEnd = weekOf(now).end-60000;
    expect(evaluate(undefined,request([campaign(41,weekOf(now).end)]),UID,nearEnd,0).rejected[0].reason).toBe('outside_week');
  });

  it('un elemento mal formado no invalida sus vecinos ni cambia la referencia',()=>{
    const bad = [null,[],17,'x',{}, {...campaign(300),extra:true}, {...campaign(300),kind:'unknown'}, {...campaign(300),level:'300'}, {...campaign(300),level:300.5}, campaign(1e100), campaign(Number.MAX_SAFE_INTEGER+1), {...campaign(300),day:'2026-10-23'}, {...campaign(300),won_at:'bad'}, {...campaign(300),won_at:'2026-02-30T12:00:00Z'}, {...campaign(300),win_id:'wrong'}, {win_id:'d:2026-10-23',kind:'daily',day:'2026-10-23',level:41,won_at:new Date(now).toISOString()}];
    const req = parseRequest(request([campaign(42),...bad,campaign(41)]));
    const result = evaluate(undefined,req,UID,now,0);
    expect(result.accepted).toEqual(['c:41','c:42']);
    expect(result.rejected).toHaveLength(bad.length);
    expect(result.rejected.every(r=>r.reason==='invalid')).toBe(true);
    expect(result.highest).toBe(42);
  });

  it('contenido futuro y nivel anterior al desbloqueo son invalid sin marca',()=>{
    const req = parseRequest(request([campaign(701),campaign(40),campaign(41)],null,1000));
    expect(validator('league-wins.request.schema.json')(req)).toBe(true);
    const result = evaluate(undefined,req,UID,now,0);
    expect(result.accepted).toEqual(['c:41']);
    expect(result.rejected).toEqual([{win_id:'c:40',reason:'invalid'},{win_id:'c:701',reason:'invalid'}]);
    expect(result.next.flagged).toBe(false);
    const daily = {win_id:'d:2026-10-23',kind:'daily',day:'2026-10-23',won_at:new Date(now).toISOString()};
    expect(evaluate(undefined,request([daily],'CO',40),UID,now,0).rejected[0].reason).toBe('invalid');
  });

  it.each([{wins:[],max_level:700},{wins:Array(51).fill(null),max_level:700},{wins:[null],max_level:-1},{wins:[null],max_level:1.5},{wins:[null]}, {wins:[null],max_level:700,uid:UID}])('solo el sobre inválido produce 400: %j',(raw)=>{
    expect(validator('league-wins.request.schema.json')(raw)).toBe(false);
    expect(()=>parseRequest(raw)).toThrowError(expect.objectContaining({status:400}));
  });
});
