import type { HttpHandler } from '../shared/http.ts';
import { header, json, jsonBody, requireUid, withErrors } from '../shared/http.ts';
import { badRequest } from '../shared/errors.ts';
import type { IntegrityVerifier } from '../shared/integrity.ts';
import { INTEGRITY_HEADER, requireIntegrity } from '../shared/integrity.ts';
import { requireEnv } from '../shared/env.ts';
import { lazy, realDdb, realIntegrityVerifier } from '../shared/runtime.ts';
import { board, liveAccount, setAvatar, submit } from './service.ts';
import type { LeagueDeps } from './service.ts';
import { parseAvatarRequest, parseRequest } from './rules.ts';
export interface LeagueHttpDeps extends LeagueDeps { integrity: IntegrityVerifier }
export type LeagueKind = 'wins'|'board'|'avatar';
export function createLeagueHandler(kind: LeagueKind, deps: LeagueHttpDeps): HttpHandler {
  return withErrors(`league${kind}`,async event=>{
    const uid = requireUid(event);
    const req = kind==='wins' ? parseRequest(jsonBody(event)) : undefined;
    const avatar = kind==='avatar' ? parseAvatarRequest(jsonBody(event)).avatar : undefined;
    const scope = event.queryStringParameters?.scope ?? 'global';
    if(kind==='board' && scope!=='global' && scope!=='country') throw badRequest('Ámbito no válido.');
    await liveAccount(deps,uid);
    await requireIntegrity({ddb:deps.ddb,verifier:deps.integrity,integrityTable:requireEnv(deps.env,'AF_TABLE_INTEGRITY'),rateLimitsTable:requireEnv(deps.env,'AF_TABLE_RATE_LIMITS'),envName:deps.env.AF_ENV??'dev',now:deps.now},uid,header(event,INTEGRITY_HEADER));
    if (avatar !== undefined) return json(200,await setAvatar(deps,uid,avatar));
    return json(200,req ? await submit(deps,uid,req) : await board(deps,uid,scope as 'global'|'country'));
  });
}
export function runtimeHandler(kind: LeagueKind): HttpHandler {
  const get = lazy(()=>createLeagueHandler(kind,{ddb:realDdb(),env:process.env,now:Date.now,integrity:realIntegrityVerifier(process.env)}));
  return event=>get()(event);
}
