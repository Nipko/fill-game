// Sitio que sube el stack Web (D-044). Sin dominio, web/site/ tal cual (variante sin dominio: canonical y hreflang
// relativos, robots.txt sin indexar). Con dominio, la variante del mismo generador (web/build.ts) con canonical y
// hreflang absolutos y, solo en prod, robots.txt que deja indexar; se genera al sintetizar en una carpeta temporal
// con nombre por contenido (CDK la copia a cdk.out como cualquier asset), así que no hay una segunda copia en el repo.
import { createHash } from 'node:crypto';
import { existsSync, renameSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import * as path from 'node:path';
import type { SiteOptions } from '../web/build.ts';
import { renderSite, writeSiteTo } from '../web/build.ts';
import type { AfConfig } from './config.ts';
import { WEB_SITE_DIR } from './paths.ts';

/** Opciones del generador para un entorno: origen con dominio; indexable solo en prod con dominio. */
export function siteOptions(config: AfConfig): SiteOptions {
  if (!config.domain) return {};
  return { origin: `https://${config.domain.name}`, indexable: config.isProd };
}

/** La web solo se indexa en prod con dominio (robots.txt y X-Robots-Tag). */
export function isIndexable(config: AfConfig): boolean {
  return siteOptions(config).indexable === true;
}

/** Carpeta con el sitio que se despliega en este entorno. */
export function deployableSiteDir(config: AfConfig): string {
  const opts = siteOptions(config);
  if (opts.origin === undefined) return WEB_SITE_DIR;
  const site = renderSite(opts);
  const hash = createHash('sha256');
  for (const f of [...site.keys()].sort()) hash.update(f).update('\0').update(site.get(f) ?? '').update('\0');
  const dir = path.join(tmpdir(), 'af-web-site', hash.digest('hex').slice(0, 24));
  if (!existsSync(dir)) {
    // Se escribe aparte y se renombra: dos síntesis a la vez (p. ej. los tests) no ven una carpeta a medias.
    const staging = `${dir}.${process.pid}.${Date.now()}.tmp`;
    writeSiteTo(staging, site);
    try {
      renameSync(staging, dir);
    } catch (err) {
      rmSync(staging, { recursive: true, force: true });
      if (!existsSync(dir)) throw err;
    }
  }
  return dir;
}
