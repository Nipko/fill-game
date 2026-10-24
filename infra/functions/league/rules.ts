import catalog from './levels.generated.json' with { type: 'json' };
import { badRequest } from '../shared/errors.ts';

export const NAME_KEYS = ['capybara','jaguar','toucan','macaw','boto','sloth','tapir','otter','manatee','turtle','howler','squirrel_monkey','caiman','anaconda','dart_frog','morpho','hummingbird','heron','kingfisher','anteater','armadillo','ocelot','coati','arapaima'].map(n => `league_name_${n}`);
// ISO 3166-1 alpha-2; se excluyen códigos reservados (EU, UK, XK, ZZ).
const COUNTRIES = new Set(('AD AE AF AG AI AL AM AO AQ AR AS AT AU AW AX AZ BA BB BD BE BF BG BH BI BJ BL BM BN BO BQ BR BS BT BV BW BY BZ CA CC CD CF CG CH CI CK CL CM CN CO CR CU CV CW CX CY CZ DE DJ DK DM DO DZ EC EE EG EH ER ES ET FI FJ FK FM FO FR GA GB GD GE GF GG GH GI GL GM GN GP GQ GR GS GT GU GW GY HK HM HN HR HT HU ID IE IL IM IN IO IQ IR IS IT JE JM JO JP KE KG KH KI KM KN KP KR KW KY KZ LA LB LC LI LK LR LS LT LU LV LY MA MC MD ME MF MG MH MK ML MM MN MO MP MQ MR MS MT MU MV MW MX MY MZ NA NC NE NF NG NI NL NO NP NR NU NZ OM PA PE PF PG PH PK PL PM PN PR PS PT PW PY QA RE RO RS RU RW SA SB SC SD SE SG SH SI SJ SK SL SM SN SO SR SS ST SV SX SY SZ TC TD TF TG TH TJ TK TL TM TN TO TR TT TV TW TZ UA UG UM US UY UZ VA VC VE VG VI VN VU WF WS YE YT ZA ZM ZW').split(' '));
export function countryOf(region: unknown): string | null {
  return typeof region === 'string' && COUNTRIES.has(region) ? region : null;
}
export function weekOf(ms: number) {
  const d = new Date(ms - 12 * 3600_000);
  const midnight = Date.UTC(d.getUTCFullYear(), d.getUTCMonth(), d.getUTCDate());
  const start = midnight - ((d.getUTCDay() + 6) % 7) * 86400_000 + 12 * 3600_000;
  const thursday = new Date(start + 3 * 86400_000);
  const year = thursday.getUTCFullYear();
  const jan4 = new Date(Date.UTC(year, 0, 4));
  const firstMonday = jan4.getTime() - ((jan4.getUTCDay() + 6) % 7) * 86400_000 + 12 * 3600_000;
  return { week: `${year}-W${String(Math.round((start - firstMonday) / (7 * 86400_000)) + 1).padStart(2, '0')}`, start, end: start + 7 * 86400_000 };
}
export type Win = { win_id: string; kind: string; level?: number; day?: string; won_at: string };
export type Request = { wins: unknown[]; max_level: number; region?: unknown };
export type Rejection = { win_id: string; reason: 'duplicate' | 'rate_limited' | 'unreachable_level' | 'outside_week' | 'invalid' };
export type Seen = { id: string; at: number };
function calendarDay(day: string | undefined): number | undefined {
  if (!day) return undefined;
  const ms = Date.parse(`${day}T00:00:00Z`);
  return Number.isFinite(ms) && new Date(ms).toISOString().slice(0,10) === day ? ms : undefined;
}
export interface Player {
  uid: string; week: string; points: number; country: string | null; wins: Seen[];
  flagged: boolean; reached: number; rev: number; ttl: number;
}
export function parseRequest(raw: Record<string, unknown>): Request {
  // Solo el sobre invalida el lote; sus elementos se juzgan individualmente.
  if (Object.keys(raw).some(k => !['wins','max_level','region'].includes(k)) || !Number.isInteger(raw.max_level) || Number(raw.max_level) < 0 || !Array.isArray(raw.wins) || raw.wins.length < 1 || raw.wins.length > 50) throw badRequest('Petición de Liga no válida.');
  return raw as Request;
}
function parseWin(item: unknown): Win | undefined {
  if (!item || typeof item !== 'object' || Array.isArray(item)) return undefined;
  const w = item as Record<string, unknown>;
  if (Object.keys(w).some(k => !['win_id','kind','level','day','won_at'].includes(k)) || typeof w.win_id !== 'string' || w.win_id.length < 1 || w.win_id.length > 40 || typeof w.won_at !== 'string' || !/^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d(?:\.\d{1,3})?Z$/.test(w.won_at) || !Number.isFinite(Date.parse(w.won_at)) || calendarDay(w.won_at.slice(0,10)) === undefined) return undefined;
  if (w.kind === 'campaign' && typeof w.level === 'number' && Number.isSafeInteger(w.level) && Number(w.level) >= 1 && w.day === undefined && w.win_id === `c:${w.level}`) return w as Win;
  if (w.kind === 'daily' && typeof w.day === 'string' && /^\d{4}-\d\d-\d\d$/.test(w.day) && calendarDay(w.day) !== undefined && w.level === undefined && w.win_id === `d:${w.day}`) return w as Win;
  return undefined;
}
export function evaluate(old: Player | undefined, req: Request, uid: string, now: number, highest: number, previous: Seen[] = []) {
  const week = weekOf(now);
  const next: Player = old ? structuredClone(old) : { uid, week: week.week, points: 0, country: null, wins: [], flagged: false, reached: 0, rev: 0, ttl: Math.floor((week.end + 14 * 86400_000) / 1000) };
  const accepted: string[] = [], rejected: Rejection[] = [];
  const table = catalog.levels as Record<string, { points: number; skippable: boolean }>;
  // Ordenar las campañas entre sus posiciones; conservar el orden/posición de los Retos.
  const items = req.wins.map(item => ({ win: parseWin(item), id: item && typeof item === 'object' && 'win_id' in item && typeof item.win_id === 'string' ? item.win_id : '' }));
  const campaigns = items.filter(item => item.win?.kind === 'campaign').sort((a,b) => a.win!.level! - b.win!.level!);
  let cursor = 0, max = highest;
  for (const item of items) {
    const { win, id } = item.win?.kind === 'campaign' ? campaigns[cursor++] : item;
    if (!win) { rejected.push({win_id:id,reason:'invalid'}); continue; }
    // Referencia informativa, nunca prueba de juego: también persiste los rechazos.
    if (win.kind === 'campaign') max = Math.max(max, win.level!);
    let reason: Rejection['reason'] | undefined;
    const at = Date.parse(win.won_at);
    const day = win.kind === 'daily' ? calendarDay(win.day) : undefined;
    if (win.kind === 'campaign' && (!Object.hasOwn(table,win.level!) || win.level! < 41)) reason = 'invalid';
    else if (win.kind === 'campaign' && win.level! > req.max_level) reason = 'unreachable_level';
    else if (req.max_level < 41) reason = 'invalid';
    else if (at < week.start || at >= week.end || at > now + 300_000) reason = 'outside_week';
    // El Reto usa la fecha LOCAL de inicio (06 §4.1; R-Q3), no la fecha UTC de won_at.
    // Sin offset en el contrato, admitir las fechas que pueden solaparse con la semana,
    // incluido el día previo por huso/cambio de medianoche; impedir días remotos o futuros.
    else if (day !== undefined && (day < Date.parse(new Date(week.start).toISOString().slice(0,10)) - 86400_000 || day > Date.parse(new Date(at).toISOString().slice(0,10)) + 86400_000)) reason = 'outside_week';
    else if (next.wins.some(w => w.id === win.win_id) || (win.kind === 'daily' && previous.some(w => w.id === win.win_id))) reason = 'duplicate';
    else {
      // Ventana móvil real sobre won_at, también con lotes offline desordenados.
      const times = [...previous.filter(w=>w.at>week.start-3600_000), ...next.wins, { id: win.win_id, at }].map(w => w.at).sort((a,b) => a-b);
      let left = 0;
      for (let right = 0; right < times.length; right++) {
        while (times[right] - times[left] >= 3600_000) left++;
        if (right - left + 1 > 40) { reason = 'rate_limited'; break; }
      }
    }
    if (reason) {
      rejected.push({ win_id: win.win_id, reason });
      continue;
    }
    if (next.wins.length === 0) next.country = countryOf(req.region);
    next.wins.push({ id: win.win_id, at });
    next.points += win.kind === 'daily' ? 20 : table[win.level!].points;
    next.reached = now;
    accepted.push(win.win_id);
  }
  next.rev++;
  return { next, accepted, rejected, highest: max };
}
// Ascendente por clave: más puntos primero; empate por instante de alcanzar puntos y UID estable.
export const rankKey = (p: Player) => `${String(999999 - p.points).padStart(6,'0')}#${String(p.reached).padStart(13,'0')}#${p.uid}`;
export const scopesOf = (p: Player) => [ `${p.week}#global`, ...(p.country ? [`${p.week}#${p.country}`] : []) ];
export const visible = (p: Player | undefined): p is Player => !!p && p.points > 0 && !p.flagged;
export const bucketOf = (points: number) => Math.floor(points / 100);
export function percentile(points: number, histogram: Array<{ bucket: number; count: number }>): number | null {
  const total = histogram.reduce((n,b) => n + Math.max(0,b.count),0);
  if (!points) return null;
  if (!total) return 100;
  const above = histogram.filter(b => b.bucket > bucketOf(points)).reduce((n,b) => n + Math.max(0,b.count),0);
  const same = Math.max(0,histogram.find(b => b.bucket === bucketOf(points))?.count ?? 0);
  return Math.min(100, Math.max(1, Math.ceil(100 * (above + same / 2) / total)));
}
