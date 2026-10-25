// Generador de la web pública (infra/web/site/). Une el manifiesto (src/site.ts), los fragmentos de
// src/content/<idioma>/<página>.html, la hoja de estilo, los archivos estáticos y el script del borrado (empaquetado
// con esbuild, sin dependencias externas). web/site/ es su salida: no se edita a mano.
//
//   node web/build.ts           escribe web/site/ (borra lo que ya no genera)
//   node web/build.ts --check   código 1 si web/site/ no coincide con lo generado
//
// web/site/ es la variante sin dominio (dev y prod en *.cloudfront.net): enlaces canonical y hreflang relativos y un
// robots.txt que no deja indexar. Con dominio, el stack Web llama a renderSite({ origin, indexable }) al sintetizar
// (lib/web-site.ts): el mismo generador y las mismas fuentes, con canonical y hreflang absolutos y, solo en prod con
// dominio, un robots.txt que deja indexar. Las dos variantes solo difieren en esas URL y en robots.txt (lo prueba
// test/web/site.test.ts).
//
// Marcadores en los fragmentos:
//   {{page:<id>}}          ruta de esa página en el mismo idioma
//   {{delete-form}}        formulario del borrado por web, con los textos del idioma
//   [[DATO DEL DUEÑO: …]]  dato que falta (D-041) · [[ABOGADO: …]] lo decide el abogado; se resaltan con <mark>
import { buildSync } from 'esbuild';
import { existsSync, mkdirSync, readdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import * as path from 'node:path';
import { fileURLToPath } from 'node:url';
import type { Lang, PageDef, PageId } from './src/site.ts';
import { LANGS, PAGE_IDS, PAGES, UI, pagePath } from './src/site.ts';

export const WEB_DIR = path.dirname(fileURLToPath(import.meta.url));
export const SRC_DIR = path.join(WEB_DIR, 'src');
export const OUT_DIR = path.join(WEB_DIR, 'site');
const BRAND_OUT_DIR = path.join(WEB_DIR, '..', '..', 'docs', 'art', 'marca-oficial', 'v2', 'out');
const BINARY_SITE_ASSETS = new Set([
  'assets/favicon-16.png',
  'assets/favicon-32.png',
  'assets/apple-touch-icon.png',
]);

export const MARKER_RE = /\[\[(DATO DEL DUEÑO|ABOGADO): [^\]]+\]\]/g;

/** Opciones de despliegue del sitio (las pone el stack Web según el entorno y el dominio). */
export interface SiteOptions {
  /** Origen público, `https://<dominio>`: canonical y hreflang absolutos. Sin él, rutas relativas. */
  readonly origin?: string;
  /** robots.txt deja indexar. Solo con origen (prod con dominio). */
  readonly indexable?: boolean;
}

const ORIGIN_RE = /^https:\/\/([a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z]{2,63}$/;

function checkOptions(opts: SiteOptions): void {
  if (opts.origin !== undefined && !ORIGIN_RE.test(opts.origin)) {
    throw new Error(`Origen no válido para la web: ${opts.origin} (se espera https://<dominio>, sin barra final).`);
  }
  if (opts.indexable && opts.origin === undefined) {
    throw new Error('Solo se indexa con dominio: indexable exige origin.');
  }
}

/** robots.txt: indexar solo con `indexable`; si no, nada. */
export function robotsTxt(opts: SiteOptions = {}): string {
  return opts.indexable ? 'User-agent: *\nAllow: /\n' : 'User-agent: *\nDisallow: /\n';
}

function escapeHtml(s: string): string {
  return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

function readSrc(...parts: string[]): string {
  return readFileSync(path.join(SRC_DIR, ...parts), 'utf8').replace(/\r\n/g, '\n');
}

function readBrandSvg(file: string): string {
  return readFileSync(path.join(BRAND_OUT_DIR, file), 'utf8').replace(/\r\n/g, '\n');
}

function readBrandPngBase64(file: string): string {
  return readFileSync(path.join(BRAND_OUT_DIR, 'png', file)).toString('base64');
}

/** Archivo de salida de una ruta con barra final: /a/b/ → a/b/index.html. */
export function fileForPath(p: string): string {
  return (p.replace(/^\//, '') + 'index.html').replace(/^\//, '');
}

function deleteForm(lang: Lang): string {
  const t = UI[lang].deleteForm;
  const data = Object.entries(t.msg)
    .map(([k, v]) => ` data-msg-${k.replace(/[A-Z]/g, (c) => '-' + c.toLowerCase())}="${escapeHtml(v)}"`)
    .join('');
  // El aviso y el formulario empiezan ocultos: el script muestra uno u otro según /config.json (sin API, el aviso).
  return `<div id="delete-soon" class="notice" hidden>
  <p>${escapeHtml(t.soon)}</p>
</div>
<form id="delete-form" class="delete-form" method="post" novalidate hidden${data}>
  <fieldset id="delete-step-code">
    <legend>${escapeHtml(t.legend)}</legend>
    <label for="account-code">${escapeHtml(t.label)}</label>
    <input id="account-code" name="code" type="text" inputmode="text" autocomplete="off" autocapitalize="characters" spellcheck="false" maxlength="64" required aria-describedby="account-code-hint delete-status">
    <p id="account-code-hint" class="hint">${escapeHtml(t.hint)}</p>
    <button id="delete-submit" type="submit">${escapeHtml(t.submit)}</button>
  </fieldset>
  <fieldset id="delete-step-confirm" hidden>
    <legend>${escapeHtml(t.confirmTitle)}</legend>
    <p>${escapeHtml(t.confirmBody)}</p>
    <p>${escapeHtml(t.confirmCodeLabel)} <strong id="delete-confirm-code" class="code"></strong></p>
    <div class="actions">
      <button id="delete-yes" type="button" class="danger">${escapeHtml(t.confirmYes)}</button>
      <button id="delete-no" type="button" class="secondary">${escapeHtml(t.confirmNo)}</button>
    </div>
  </fieldset>
</form>
<p id="delete-status" class="status" role="status" aria-live="polite" tabindex="-1"></p>
<noscript><p class="notice">${escapeHtml(t.noscript)}</p></noscript>`;
}

function renderContent(page: PageDef, lang: Lang): string {
  let html = readSrc('content', lang, `${page.id}.html`).trim();
  html = html.replace(/\{\{page:([a-z]+)\}\}/g, (_, id: string) => {
    if (!(PAGE_IDS as readonly string[]).includes(id)) throw new Error(`${lang}/${page.id}: página desconocida {{page:${id}}}`);
    return pagePath(id as PageId, lang);
  });
  if (html.includes('{{delete-form}}')) html = html.replace('{{delete-form}}', deleteForm(lang));
  const leftover = /\{\{[^}]*\}\}/.exec(html);
  if (leftover) throw new Error(`${lang}/${page.id}: marcador sin resolver ${leftover[0]}`);
  return html.replace(MARKER_RE, (m) => `<mark class="pending">${m}</mark>`);
}

function renderPage(page: PageDef, lang: Lang, opts: SiteOptions): string {
  const ui = UI[lang];
  const title = page.id === 'home' ? page.title[lang] : `${page.title[lang]} · Tintalú`;
  // canonical y hreflang: absolutos con dominio (los buscadores los prefieren así), relativos sin él.
  const url = (p: string): string => (opts.origin ?? '') + p;
  const alternates = [
    `  <link rel="canonical" href="${url(page.path[lang])}">`,
    ...LANGS.map((l) => `  <link rel="alternate" hreflang="${l}" href="${url(page.path[l])}">`),
    `  <link rel="alternate" hreflang="x-default" href="${url(page.path[LANGS[0]])}">`,
  ].join('\n');
  const langNav = LANGS.map((l) => {
    const current = l === lang ? ' aria-current="page"' : '';
    return `      <li><a href="${page.path[l]}" hreflang="${l}" lang="${l}"${current}>${escapeHtml(UI[l].langName)}</a></li>`;
  }).join('\n');
  const footerNav = PAGES.map((p) => {
    const current = p.id === page.id ? ' aria-current="page"' : '';
    return `      <li><a href="${p.path[lang]}"${current}>${escapeHtml(p.title[lang])}</a></li>`;
  }).join('\n');
  const script = page.script ? `\n  <script src="${page.script}" defer></script>` : '';
  const draft = page.draft
    ? `\n  <div class="draft" role="note"><strong>${escapeHtml(ui.draft)}</strong> ${escapeHtml(ui.draftDetail)}</div>`
    : '';
  return `<!doctype html>
<html lang="${lang}">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>${escapeHtml(title)}</title>
  <meta name="description" content="${escapeHtml(page.description[lang])}">
  <meta name="referrer" content="strict-origin-when-cross-origin">
  <meta name="color-scheme" content="light dark">
  <link rel="icon" type="image/png" sizes="16x16" href="/assets/favicon-16.png">
  <link rel="icon" type="image/png" sizes="32x32" href="/assets/favicon-32.png">
  <link rel="apple-touch-icon" sizes="180x180" href="/assets/apple-touch-icon.png">
  <link rel="stylesheet" href="/assets/site.css">
${alternates}${script}
</head>
<body>
  <a class="skip" href="#main">${escapeHtml(ui.skip)}</a>
  <header class="site-header">
    <a class="brand" href="${pagePath('home', lang)}">
      <picture>
        <source media="(prefers-color-scheme: dark)" srcset="/assets/brand/logo-horizontal-oscuro.svg">
        <img src="/assets/brand/logo-horizontal-claro.svg" alt="Tintalú" width="142" height="40">
      </picture>
    </a>
    <nav class="lang" aria-label="${escapeHtml(ui.langNav)}">
      <ul>
${langNav}
      </ul>
    </nav>
  </header>${draft}
  <main id="main" tabindex="-1">
${renderContent(page, lang)}
  </main>
  <footer class="site-footer">
    <nav aria-label="${escapeHtml(ui.footerNav)}">
      <ul>
${footerNav}
      </ul>
    </nav>
    <p>${escapeHtml(ui.footerDeveloper)}</p>
    <p>${escapeHtml(ui.independence)}</p>
  </footer>
</body>
</html>
`;
}

function bundleScript(): string {
  const result = buildSync({
    entryPoints: [path.join(SRC_DIR, 'browser', 'delete-account.ts')],
    bundle: true,
    write: false,
    format: 'iife',
    platform: 'browser',
    target: ['es2020'],
    legalComments: 'none',
    minify: false,
    logLevel: 'silent',
  });
  const banner = '// Generado por infra/web/build.ts desde web/src/browser/: no se edita a mano.\n';
  return banner + result.outputFiles[0].text.replace(/\r\n/g, '\n');
}

/** Toda la web: ruta relativa dentro de web/site/ → contenido. Sin opciones, la variante de web/site/. */
export function renderSite(opts: SiteOptions = {}): Map<string, string> {
  checkOptions(opts);
  const out = new Map<string, string>();
  for (const page of PAGES) {
    for (const lang of LANGS) {
      const file = fileForPath(page.path[lang]);
      if (out.has(file)) throw new Error(`Ruta repetida: ${file}`);
      out.set(file, renderPage(page, lang, opts));
    }
  }
  out.set('assets/site.css', readSrc('static', 'site.css'));
  out.set('assets/delete-account.js', bundleScript());
  out.set('assets/favicon-16.png', readBrandPngBase64('favicon-16.png'));
  out.set('assets/favicon-32.png', readBrandPngBase64('favicon-32.png'));
  out.set('assets/apple-touch-icon.png', readBrandPngBase64('apple-touch-icon.png'));
  out.set('assets/brand/logo-horizontal-claro.svg', readBrandSvg('logo-horizontal-claro.svg'));
  out.set('assets/brand/logo-horizontal-oscuro.svg', readBrandSvg('logo-horizontal-oscuro.svg'));
  out.set('app-ads.txt', readSrc('static', 'app-ads.txt'));
  out.set('robots.txt', robotsTxt(opts));
  return out;
}

function listFiles(dir: string, base = dir): string[] {
  if (!existsSync(dir)) return [];
  const files: string[] = [];
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) files.push(...listFiles(full, base));
    else files.push(path.relative(base, full).split(path.sep).join('/'));
  }
  return files.sort();
}

/** Diferencias entre web/site/ y lo generado (vacío = al día). Ignora CRLF frente a LF. */
export function diffSite(site = renderSite()): string[] {
  const problems: string[] = [];
  const onDisk = listFiles(OUT_DIR);
  for (const f of onDisk) if (!site.has(f)) problems.push(`sobra ${f}`);
  for (const [f, content] of site) {
    const full = path.join(OUT_DIR, f);
    if (!existsSync(full)) problems.push(`falta ${f}`);
    else {
      const actual = BINARY_SITE_ASSETS.has(f)
        ? readFileSync(full).toString('base64')
        : readFileSync(full, 'utf8').replace(/\r\n/g, '\n');
      if (actual !== content) problems.push(`distinto ${f}`);
    }
  }
  return problems;
}

/** Escribe un sitio generado en `dir` (borra lo que sobra y las carpetas vacías). */
export function writeSiteTo(dir: string, site: Map<string, string>): void {
  for (const f of listFiles(dir)) if (!site.has(f)) rmSync(path.join(dir, f));
  for (const [f, content] of site) {
    const full = path.join(dir, f);
    mkdirSync(path.dirname(full), { recursive: true });
    if (BINARY_SITE_ASSETS.has(f)) writeFileSync(full, Buffer.from(content, 'base64'));
    else writeFileSync(full, content, 'utf8');
  }
  // Carpetas vacías que quedaron tras borrar páginas.
  const prune = (d: string): void => {
    for (const e of readdirSync(d, { withFileTypes: true })) if (e.isDirectory()) prune(path.join(d, e.name));
    if (d !== dir && readdirSync(d).length === 0) rmSync(d, { recursive: true });
  };
  prune(dir);
}

function writeSite(): void {
  const site = renderSite();
  writeSiteTo(OUT_DIR, site);
  console.log(`web/site: ${site.size} archivos`);
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  if (process.argv.includes('--check')) {
    const problems = diffSite();
    if (problems.length > 0) {
      console.error(`web/site no está al día (node web/build.ts):\n  ${problems.join('\n  ')}`);
      process.exit(1);
    }
    console.log('web/site: identico');
  } else {
    writeSite();
  }
}
