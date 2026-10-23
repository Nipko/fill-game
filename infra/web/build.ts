// Generador de la web pública (infra/web/site/). Une el manifiesto (src/site.ts), los fragmentos de
// src/content/<idioma>/<página>.html, la hoja de estilo, los archivos estáticos y el script del borrado (empaquetado
// con esbuild, sin dependencias externas). web/site/ es su salida: no se edita a mano.
//
//   node web/build.ts           escribe web/site/ (borra lo que ya no genera)
//   node web/build.ts --check   código 1 si web/site/ no coincide con lo generado
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

export const MARKER_RE = /\[\[(DATO DEL DUEÑO|ABOGADO): [^\]]+\]\]/g;

function escapeHtml(s: string): string {
  return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

function readSrc(...parts: string[]): string {
  return readFileSync(path.join(SRC_DIR, ...parts), 'utf8').replace(/\r\n/g, '\n');
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
  return `<form id="delete-form" class="delete-form" method="post" novalidate hidden${data}>
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

function renderPage(page: PageDef, lang: Lang): string {
  const ui = UI[lang];
  const title = page.id === 'home' ? page.title[lang] : `${page.title[lang]} · Tintalú`;
  const alternates = LANGS.map(
    (l) => `  <link rel="alternate" hreflang="${l}" href="${page.path[l]}">`,
  ).join('\n');
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
  <link rel="stylesheet" href="/assets/site.css">
${alternates}${script}
</head>
<body>
  <a class="skip" href="#main">${escapeHtml(ui.skip)}</a>
  <header class="site-header">
    <a class="brand" href="${pagePath('home', lang)}">Tintalú</a>
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

/** Toda la web: ruta relativa dentro de web/site/ → contenido. */
export function renderSite(): Map<string, string> {
  const out = new Map<string, string>();
  for (const page of PAGES) {
    for (const lang of LANGS) {
      const file = fileForPath(page.path[lang]);
      if (out.has(file)) throw new Error(`Ruta repetida: ${file}`);
      out.set(file, renderPage(page, lang));
    }
  }
  out.set('assets/site.css', readSrc('static', 'site.css'));
  out.set('assets/delete-account.js', bundleScript());
  out.set('app-ads.txt', readSrc('static', 'app-ads.txt'));
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
    else if (readFileSync(full, 'utf8').replace(/\r\n/g, '\n') !== content) problems.push(`distinto ${f}`);
  }
  return problems;
}

function writeSite(): void {
  const site = renderSite();
  for (const f of listFiles(OUT_DIR)) if (!site.has(f)) rmSync(path.join(OUT_DIR, f));
  for (const [f, content] of site) {
    const full = path.join(OUT_DIR, f);
    mkdirSync(path.dirname(full), { recursive: true });
    writeFileSync(full, content, 'utf8');
  }
  // Carpetas vacías que quedaron tras borrar páginas.
  const prune = (dir: string): void => {
    for (const e of readdirSync(dir, { withFileTypes: true })) if (e.isDirectory()) prune(path.join(dir, e.name));
    if (dir !== OUT_DIR && readdirSync(dir).length === 0) rmSync(dir, { recursive: true });
  };
  prune(OUT_DIR);
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
