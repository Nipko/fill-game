// Avatar de la Liga (D-056, D-057; docs/dev/avatares.md §4.4): POST /v1/league/avatar, copia a las filas del top,
// cupo diario, borrado de cuenta y la anaconda fuera de los seudónimos nuevos (Q-AV-12).
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { createLeagueHandler } from '../../functions/league/http.ts';
import { aliasOf, board, setAvatar, submit } from '../../functions/league/service.ts';
import type { LeagueDeps } from '../../functions/league/service.ts';
import { ASSIGNABLE_NAME_KEYS, AVATAR_CHANGES_PER_DAY, NAME_KEYS, weekOf } from '../../functions/league/rules.ts';
import type { Request, Win } from '../../functions/league/rules.ts';
import { markDeleted } from '../../functions/shared/rateLimit.ts';
import { uidHash } from '../../functions/shared/log.ts';
import { createHandler as deleteHandler } from '../../functions/delete-account/handler.ts';
import { readExample, validator } from '../contract/ajv.ts';
import { httpEvent, OTHER_UID, UID } from './fixtures.ts';
import { MemDb, TABLES } from './memdb.ts';

const now = Date.parse('2026-10-23T20:00:00Z');
const campaign = (level: number, at = now - 60000): Win => ({ win_id: `c:${level}`, kind: 'campaign', level, won_at: new Date(at).toISOString() });
const request = (wins: Win[], region = 'CO'): Request => ({ wins, max_level: 700, region });
const ROUTE = 'POST /v1/league/avatar';

describe('Liga: avatar (D-056)', () => {
  let db: MemDb, deps: LeagueDeps, n: number;
  const okIntegrity = { variant: 'app-check' as const, verify: () => Promise.resolve({ ok: true }) };
  const handler = (d: LeagueDeps = deps) => createLeagueHandler('avatar', { ...d, integrity: okIntegrity });
  const trustUid = (uid: string) => db.put(TABLES.AF_TABLE_INTEGRITY, { uid, verdict: 'ok', ttl: Math.floor(now / 1000) + 3600 });
  beforeEach(() => {
    db = new MemDb();
    n = 0;
    deps = { ddb: db.client, env: { ...TABLES, AF_ENV: 'dev' }, now: () => now, pick: (max) => (n++) % max };
  });
  afterEach(() => db.restore());

  it('sin seudónimo previo lo crea y guarda el avatar con él; la respuesta cumple el contrato', async () => {
    trustUid(UID);
    const res = await handler()(httpEvent({ routeKey: ROUTE, uid: UID, body: readExample('league-avatar.request.json') }));
    expect(res.statusCode).toBe(200);
    const body = JSON.parse(res.body!) as unknown;
    expect(body).toEqual({ avatar: 7 });
    expect(validator('league-avatar.response.schema.json')(body)).toBe(true);
    const alias = db.get(TABLES.AF_TABLE_LEAGUE_NAMES, { uid: UID });
    expect(alias).toMatchObject({ avatar: 7, avatar_changes: 1, avatar_day: '2026-10-23' });
    // Sin puntos no hay filas de top, pero la fila del jugador ya lleva el avatar.
    expect(db.items(TABLES.AF_TABLE_LEAGUE_BOARD)).toEqual([]);
    const view = await board(deps, UID, 'global');
    expect(view.me.avatar).toBe(7);
    expect(validator('league-board.response.schema.json')(view)).toBe(true);
    // La primera victoria ya sale con el avatar en el top y en la respuesta.
    const wins = await submit(deps, UID, request([campaign(41)]));
    expect(wins.avatar).toBe(7);
    expect(validator('league-wins.response.schema.json')(wins)).toBe(true);
    expect((await board(deps, OTHER_UID, 'global')).top[0].avatar).toBe(7);
  });

  it('sin avatar elegido: el campo se omite en el top, en me y en las victorias (respuesta idéntica a la anterior)', async () => {
    const wins = await submit(deps, UID, request([campaign(41)]));
    expect(wins).not.toHaveProperty('avatar');
    const view = await board(deps, UID, 'global');
    expect(view.top[0]).not.toHaveProperty('avatar');
    expect(view.me).not.toHaveProperty('avatar');
    expect(validator('league-board.response.schema.json')(view)).toBe(true);
    // Ni las filas guardadas del top llevan el atributo.
    expect(db.items(TABLES.AF_TABLE_LEAGUE_BOARD).every((r) => !('avatar' in r))).toBe(true);
  });

  it('con filas en el top las reescribe en la misma transacción, sin otra victoria ni tocar el histograma', async () => {
    await submit(deps, UID, request([campaign(41)]));
    await submit({ ...deps, now: () => now + 1000 }, OTHER_UID, request([campaign(41)]));
    const histogram = structuredClone(db.items(TABLES.AF_TABLE_LEAGUE_HISTOGRAM));
    const sorts = db.items(TABLES.AF_TABLE_LEAGUE_BOARD).map((r) => `${String(r.scope)}|${String(r.sort)}`).sort();
    await setAvatar(deps, UID, 12);
    const rows = db.items(TABLES.AF_TABLE_LEAGUE_BOARD);
    expect(rows.map((r) => `${String(r.scope)}|${String(r.sort)}`).sort()).toEqual(sorts);
    expect(rows.filter((r) => r.avatar === 12)).toHaveLength(2); // global y CO
    expect(rows.filter((r) => r.avatar === undefined)).toHaveLength(2); // las del otro jugador
    expect(db.items(TABLES.AF_TABLE_LEAGUE_HISTOGRAM)).toEqual(histogram);
    const seen = await board(deps, OTHER_UID, 'country');
    expect(seen.top.map((r) => r.avatar)).toEqual([12, undefined]);
    expect(seen.me).not.toHaveProperty('avatar');
    // Una victoria posterior conserva el avatar al mover las filas.
    await submit(deps, UID, request([campaign(42)]));
    expect(db.items(TABLES.AF_TABLE_LEAGUE_BOARD).filter((r) => r.avatar === 12)).toHaveLength(2);
    expect(db.get(TABLES.AF_TABLE_LEAGUE_NAMES, { uid: UID })?.avatar).toBe(12);
  });

  it('una cuenta marcada no gana filas públicas al cambiar de avatar', async () => {
    await aliasOf(deps, UID);
    db.put(TABLES.AF_TABLE_LEAGUE_PLAYERS, { uid: UID, week: weekOf(now).week, points: 30, country: 'CO', wins: [], flagged: true, reached: now, rev: 1, ttl: 0 });
    await setAvatar(deps, UID, 3);
    expect(db.items(TABLES.AF_TABLE_LEAGUE_BOARD)).toEqual([]);
  });

  it('acepta 0 y 63; rechaza 64, negativos, decimales, texto, campos extra y cuerpo vacío con 400', async () => {
    trustUid(UID);
    const h = handler();
    for (const avatar of [0, 63]) {
      const res = await h(httpEvent({ routeKey: ROUTE, uid: UID, body: { avatar } }));
      expect(res.statusCode, String(avatar)).toBe(200);
      expect(JSON.parse(res.body!)).toEqual({ avatar });
    }
    for (const body of [{ avatar: 64 }, { avatar: -1 }, { avatar: 1.5 }, { avatar: '7' }, { avatar: true }, { avatar: null }, {}, { avatar: 7, uid: OTHER_UID }, readExample('league-avatar.request.invalid-out-of-range.json')]) {
      const res = await h(httpEvent({ routeKey: ROUTE, uid: UID, body }));
      expect(res.statusCode, JSON.stringify(body)).toBe(400);
      expect(validator('error.schema.json')(JSON.parse(res.body!))).toBe(true);
    }
    expect(db.get(TABLES.AF_TABLE_LEAGUE_NAMES, { uid: UID })?.avatar).toBe(63);
  });

  it(`límite de ${AVATAR_CHANGES_PER_DAY} cambios por día UTC; repetir el vigente no gasta cupo`, async () => {
    trustUid(UID);
    for (let i = 0; i < AVATAR_CHANGES_PER_DAY; i++) await setAvatar(deps, UID, i % 2);
    // El mismo avatar que ya tiene: idempotente, 200 aunque el cupo esté agotado.
    expect(await setAvatar(deps, UID, 1)).toEqual({ avatar: 1 });
    const res = await handler()(httpEvent({ routeKey: ROUTE, uid: UID, body: { avatar: 5 } }));
    expect(res.statusCode).toBe(429);
    const err = JSON.parse(res.body!) as { error: { code: string; retry_after_s: number } };
    expect(err.error.code).toBe('rate_limited');
    // Hasta la medianoche UTC: de 20:00 a 24:00.
    expect(err.error.retry_after_s).toBe(4 * 3600);
    expect(res.headers?.['retry-after']).toBe(String(4 * 3600));
    expect(db.get(TABLES.AF_TABLE_LEAGUE_NAMES, { uid: UID })).toMatchObject({ avatar: 1, avatar_changes: AVATAR_CHANGES_PER_DAY });
    // Al día siguiente el cupo vuelve a empezar, en la misma fila del seudónimo (sin filas nuevas en rate_limits).
    const tomorrow = Date.parse('2026-10-24T00:00:01Z');
    expect(await setAvatar({ ...deps, now: () => tomorrow }, UID, 5)).toEqual({ avatar: 5 });
    expect(db.get(TABLES.AF_TABLE_LEAGUE_NAMES, { uid: UID })).toMatchObject({ avatar: 5, avatar_changes: 1, avatar_day: '2026-10-24' });
    expect(db.items(TABLES.AF_TABLE_RATE_LIMITS).some((r) => String(r.key).includes('avatar'))).toBe(false);
  });

  it('una victoria concurrente hace reintentar desde el estado fresco sin perder el avatar ni los puntos', async () => {
    await submit(deps, UID, request([campaign(41)]));
    db.hookNext = {
      command: 'TransactWrite',
      table: TABLES.AF_TABLE_LEAGUE_NAMES,
      fn: () => {
        const alias = db.get(TABLES.AF_TABLE_LEAGUE_NAMES, { uid: UID })!;
        db.put(TABLES.AF_TABLE_LEAGUE_NAMES, { ...alias, rev: Number(alias.rev) + 1 });
      },
    };
    await setAvatar(deps, UID, 9);
    expect(db.get(TABLES.AF_TABLE_LEAGUE_NAMES, { uid: UID })).toMatchObject({ avatar: 9, avatar_changes: 1 });
    expect(db.items(TABLES.AF_TABLE_LEAGUE_BOARD).map((r) => r.avatar)).toEqual([9, 9]);
    expect((await board(deps, UID, 'global')).me.points).toBe(10);
  });

  it('exige JWT y App Check como las otras rutas de la Liga', async () => {
    const bad = createLeagueHandler('avatar', { ...deps, integrity: { variant: 'app-check', verify: () => Promise.resolve({ ok: false }) } });
    expect((await bad(httpEvent({ routeKey: ROUTE, body: { avatar: 7 } }))).statusCode).toBe(401);
    expect((await bad(httpEvent({ routeKey: ROUTE, uid: UID, body: { avatar: 7 } }))).statusCode).toBe(403);
    expect(db.items(TABLES.AF_TABLE_LEAGUE_NAMES)).toEqual([]);
  });

  it('cuenta borrada: 401 sin escribir, también si el borrado gana la carrera', async () => {
    trustUid(UID);
    await markDeleted(db.client, TABLES.AF_TABLE_RATE_LIMITS, uidHash(UID), now);
    expect((await handler()(httpEvent({ routeKey: ROUTE, uid: UID, body: { avatar: 7 } }))).statusCode).toBe(401);
    expect(db.get(TABLES.AF_TABLE_LEAGUE_NAMES, { uid: UID })).toBeUndefined();

    await submit(deps, OTHER_UID, request([campaign(41)]));
    db.hookNext = {
      command: 'TransactWrite',
      table: TABLES.AF_TABLE_LEAGUE_NAMES,
      fn: () => db.put(TABLES.AF_TABLE_RATE_LIMITS, { key: `deleted#${uidHash(OTHER_UID)}`, ttl: Math.floor(now / 1000) + 7200 }),
    };
    await expect(setAvatar(deps, OTHER_UID, 7)).rejects.toMatchObject({ status: 401 });
    expect(db.get(TABLES.AF_TABLE_LEAGUE_NAMES, { uid: OTHER_UID })?.avatar).toBeUndefined();
    expect(db.items(TABLES.AF_TABLE_LEAGUE_BOARD).some((r) => r.avatar !== undefined)).toBe(false);
  });

  it('deleteAccount borra el avatar con el seudónimo y sus filas; ninguna otra tabla lo guarda', async () => {
    await submit({ ...deps, now: () => now - 7 * 86400_000 }, UID, request([campaign(41, now - 7 * 86400_000 - 60000)]));
    await setAvatar({ ...deps, now: () => now - 7 * 86400_000 }, UID, 4);
    await submit(deps, UID, request([campaign(41)]));
    await setAvatar(deps, UID, 6);
    await submit(deps, OTHER_UID, request([campaign(41)]));
    await setAvatar(deps, OTHER_UID, 2);
    // Antes del borrado: el avatar solo vive en league_names y en league_board.
    const holders = () => [...db.tables.entries()].filter(([, t]) => [...t.values()].some((i) => 'avatar' in i)).map(([name]) => name).sort();
    expect(holders()).toEqual([TABLES.AF_TABLE_LEAGUE_BOARD, TABLES.AF_TABLE_LEAGUE_NAMES].sort());
    expect(db.items(TABLES.AF_TABLE_LEAGUE_BOARD).filter((r) => r.avatar === 4)).toHaveLength(2); // semana anterior

    trustUid(UID);
    const del = deleteHandler(() => ({ ...deps, integrity: okIntegrity, firebase: { deleteUser: () => Promise.resolve({ existed: true }) }, random: (k: number) => Buffer.alloc(k, 1) }));
    const res = await del(httpEvent({ routeKey: 'POST /v1/account/delete', uid: UID, body: readExample('account-delete.request.json') }));
    expect(res.statusCode).toBe(200);
    expect(db.get(TABLES.AF_TABLE_LEAGUE_NAMES, { uid: UID })).toBeUndefined();
    // Solo queda el avatar del otro jugador: ni el seudónimo, ni el cupo, ni las filas de ninguna semana del borrado.
    const avatars = [...db.tables.values()].flatMap((t) => [...t.values()]).filter((i) => 'avatar' in i);
    expect(avatars.every((i) => i.avatar === 2)).toBe(true);
    expect(avatars.filter((i) => i.uid === OTHER_UID)).toHaveLength(1);
    expect((await board(deps, OTHER_UID, 'global')).top.map((r) => r.avatar)).toEqual([2]);
    // Y no se puede volver a poner con un ID token todavía válido.
    await expect(setAvatar(deps, UID, 6)).rejects.toMatchObject({ status: 401 });
  });
});

describe('Liga: la anaconda fuera de los seudónimos nuevos (D-057, Q-AV-12)', () => {
  let db: MemDb, deps: LeagueDeps;
  beforeEach(() => {
    db = new MemDb();
    deps = { ddb: db.client, env: { ...TABLES, AF_ENV: 'dev' }, now: () => now };
  });
  afterEach(() => db.restore());

  it('los índices no cambian: la clave sigue en el orden del contrato, pero no se asigna', () => {
    expect(NAME_KEYS).toHaveLength(24);
    expect(NAME_KEYS[13]).toBe('league_name_anaconda');
    expect(ASSIGNABLE_NAME_KEYS).toHaveLength(23);
    expect(ASSIGNABLE_NAME_KEYS).not.toContain('league_name_anaconda');
    expect(ASSIGNABLE_NAME_KEYS).toEqual(NAME_KEYS.filter((k) => k !== 'league_name_anaconda'));
  });

  it('ningún sorteo da la anaconda a un seudónimo nuevo', async () => {
    const keys = new Set<string>();
    for (let i = 0; i < 24; i++) {
      // pick(n) devuelve i (acotado) para el animal: recorre todas las posiciones que el servidor puede sortear.
      const alias = await aliasOf({ ...deps, pick: (max) => (max === 9000 ? i : Math.min(i, max - 1)) }, `Uid${i}`);
      keys.add(alias.name_key);
    }
    expect(keys.has('league_name_anaconda')).toBe(false);
    expect(keys.size).toBe(23);
  });

  it('quien ya la tiene la conserva al sumar puntos, al leer la Liga y al cambiar de avatar', async () => {
    db.put(TABLES.AF_TABLE_LEAGUE_NAMES, { uid: UID, name_key: 'league_name_anaconda', name_num: 4321, highest: 0, rev: 0, reservation: 'name#league_name_anaconda#4321' });
    db.put(TABLES.AF_TABLE_LEAGUE_NAMES, { uid: 'name#league_name_anaconda#4321', owner: UID });
    const wins = await submit(deps, UID, request([campaign(41)]));
    expect(wins).toMatchObject({ name_key: 'league_name_anaconda', name_num: 4321 });
    expect(validator('league-wins.response.schema.json')(wins)).toBe(true);
    await setAvatar(deps, UID, 10);
    const view = await board(deps, UID, 'global');
    expect(view.me).toMatchObject({ name_key: 'league_name_anaconda', name_num: 4321, avatar: 10 });
    expect(view.top[0]).toMatchObject({ name_key: 'league_name_anaconda', avatar: 10 });
    expect(validator('league-board.response.schema.json')(view)).toBe(true);
  });
});
