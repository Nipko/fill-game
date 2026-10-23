// Web pública (infra/web/site): al día con su generador, enlaces internos, sin recursos externos, los 3 idiomas con
// las mismas páginas, lang, títulos y sin texto sin traducir.
import { existsSync, readdirSync } from 'node:fs';
import * as path from 'node:path';
import { describe, expect, it } from 'vitest';
import { MARKER_RE, OUT_DIR, diffSite, fileForPath, renderSite } from '../../web/build.ts';
import type { Lang, PageDef } from '../../web/src/site.ts';
import { LANGS, PAGES, UI } from '../../web/src/site.ts';
import { WEB_RUNTIME_CONFIG_FILE } from '../../lib/web-stack.ts';

const site = renderSite();

function listFiles(dir: string, base = dir): string[] {
  const out: string[] = [];
  for (const e of readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, e.name);
    if (e.isDirectory()) out.push(...listFiles(full, base));
    else out.push(path.relative(base, full).split(path.sep).join('/'));
  }
  return out.sort();
}

function html(page: PageDef, lang: Lang): string {
  const content = site.get(fileForPath(page.path[lang]));
  if (content === undefined) throw new Error(`Falta ${page.id} en ${lang}`);
  return content;
}

function attrs(doc: string, attr: 'href' | 'src'): string[] {
  return [...doc.matchAll(new RegExp(`\\s${attr}="([^"]*)"`, 'g'))].map((m) => m[1]);
}

function ids(doc: string): Set<string> {
  return new Set([...doc.matchAll(/\sid="([^"]+)"/g)].map((m) => m[1]));
}

function decode(s: string): string {
  return s
    .replace(/&quot;/g, '"')
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&amp;/g, '&');
}

/** Bloques de texto visibles (encabezados, párrafos, celdas, elementos de lista, etiquetas y botones). */
function textBlocks(doc: string): string[] {
  const main = /<main[^>]*>([\s\S]*)<\/main>/.exec(doc)?.[1] ?? '';
  const blocks = [...main.matchAll(/<(h1|h2|h3|p|li|td|th|label|button|legend)\b[^>]*>([\s\S]*?)<\/\1>/g)].map((m) => m[2]);
  return blocks.map((b) => decode(b.replace(/<[^>]+>/g, '')).replace(/\s+/g, ' ').trim()).filter(Boolean);
}

function markers(doc: string): string[] {
  return [...doc.matchAll(MARKER_RE)].map((m) => m[0]).sort();
}

const HTML_FILES = [...site.keys()].filter((f) => f.endsWith('.html'));

describe('web/site', () => {
  it('está al día con web/build.ts (node web/build.ts lo regenera)', () => {
    expect(diffSite(site)).toEqual([]);
  });

  it('no incluye config.json: lo escribe el stack Web al desplegar', () => {
    expect(site.has(WEB_RUNTIME_CONFIG_FILE)).toBe(false);
    expect(existsSync(path.join(OUT_DIR, WEB_RUNTIME_CONFIG_FILE))).toBe(false);
  });

  it('los 3 idiomas tienen las mismas páginas, y no hay páginas sueltas', () => {
    const expected = PAGES.flatMap((p) => LANGS.map((l) => fileForPath(p.path[l]))).sort();
    expect(HTML_FILES.sort()).toEqual(expected);
    expect(listFiles(OUT_DIR).filter((f) => f.endsWith('.html')).sort()).toEqual(expected);
    // La ruta canónica del borrado de 08 §11.
    expect(PAGES.find((p) => p.id === 'delete')?.path['es-419']).toBe('/tintalu/eliminar-cuenta/');
  });

  it('cada página tiene las mismas secciones y los mismos marcadores pendientes en los 3 idiomas', () => {
    for (const page of PAGES) {
      const es = html(page, 'es-419');
      for (const lang of LANGS) {
        const doc = html(page, lang);
        const sections = (d: string): string[] => [...d.matchAll(/<h2 id="([^"]+)"/g)].map((m) => m[1]);
        expect(sections(doc), `${page.id} ${lang}`).toEqual(sections(es));
        expect(markers(doc), `${page.id} ${lang}`).toEqual(markers(es));
        expect(doc.includes('<form id="delete-form"'), `${page.id} ${lang}`).toBe(page.id === 'delete');
      }
    }
  });

  it('lang, título, descripción, un solo h1 y selector de idioma en cada página', () => {
    const titles = new Map<Lang, Set<string>>();
    for (const page of PAGES) {
      for (const lang of LANGS) {
        const doc = html(page, lang);
        const where = `${page.id} ${lang}`;
        expect(doc.startsWith('<!doctype html>\n<html lang="' + lang + '">'), where).toBe(true);
        const title = /<title>([^<]+)<\/title>/.exec(doc)?.[1] ?? '';
        expect(title, where).toContain(decode(page.title[lang]).split(' · ')[0]);
        expect(title, where).toMatch(/Tintalú/);
        const set = titles.get(lang) ?? new Set<string>();
        expect(set.has(title), `${where}: título repetido`).toBe(false);
        set.add(title);
        titles.set(lang, set);
        expect(doc, where).toMatch(/<meta name="description" content="[^"]{20,}">/);
        expect(doc.match(/<h1[\s>]/g), where).toHaveLength(1);
        for (const l of LANGS) {
          expect(doc, where).toContain(`<link rel="alternate" hreflang="${l}" href="${page.path[l]}">`);
          expect(doc, where).toContain(`<a href="${page.path[l]}" hreflang="${l}" lang="${l}"`);
        }
        expect(doc, where).toContain(`aria-current="page">${UI[lang].langName}</a>`);
        expect(doc.includes(UI[lang].draft), where).toBe(page.draft);
        expect(doc, where).toContain('<main id="main"');
      }
    }
  });

  it('sin texto sin traducir: ningún bloque largo se repite igual entre idiomas', () => {
    // Nombres propios y citas que son iguales en todos los idiomas.
    const allowed = new Set([
      'Baloo 2 · The Baloo 2 Project Authors.',
      'Nunito · The Nunito Project Authors.',
      'Google Play Billing Library.',
      'Unity Mobile Notifications.',
      'Google Analytics para Firebase', // nombre del producto en español y en portugués
    ]);
    for (const page of PAGES) {
      const blocks = new Map<Lang, Set<string>>(
        LANGS.map((l) => [l, new Set(textBlocks(html(page, l)).map((b) => b.replace(MARKER_RE, '').replace(/\s+/g, ' ').trim()))]),
      );
      for (const a of LANGS) {
        for (const b of LANGS) {
          if (a >= b) continue;
          const shared = [...(blocks.get(a) ?? [])].filter((t) => t.length >= 25 && !allowed.has(t) && blocks.get(b)?.has(t));
          expect(shared, `${page.id}: ${a} y ${b}`).toEqual([]);
        }
      }
    }
    // Los textos de la plantilla también son distintos por idioma.
    for (const key of ['skip', 'draft', 'draftDetail', 'footerDeveloper', 'independence'] as const) {
      expect(new Set(LANGS.map((l) => UI[l][key])).size, key).toBe(LANGS.length);
    }
    for (const key of Object.keys(UI['es-419'].deleteForm.msg) as Array<keyof typeof UI['es-419']['deleteForm']['msg']>) {
      expect(new Set(LANGS.map((l) => UI[l].deleteForm.msg[key])).size, key).toBe(LANGS.length);
    }
  });

  it('solo hay marcadores [[DATO DEL DUEÑO: …]] y [[ABOGADO: …]], resaltados, y ningún resto de borrador', () => {
    for (const f of HTML_FILES) {
      const doc = site.get(f) ?? '';
      const all = [...doc.matchAll(/\[\[[^\]]*\]\]/g)].map((m) => m[0]);
      for (const m of all) expect(m, f).toMatch(/^\[\[(DATO DEL DUEÑO|ABOGADO): [^\]]+\]\]$/);
      expect(doc.split('<mark class="pending">[[').length - 1, f).toBe(all.length);
      expect(doc, f).not.toMatch(/\[PENDIENTE|TODO|FIXME|\{\{/);
      expect(doc, f).not.toMatch(/AUTOMATION AI/i);
    }
  });

  it('todos los enlaces internos llevan a un archivo o a un id que existe', () => {
    const exists = (p: string): boolean => {
      const rel = p.replace(/^\//, '');
      if (p.endsWith('/')) return site.has(rel + 'index.html');
      return site.has(rel) || site.has(rel + '/index.html');
    };
    for (const f of HTML_FILES) {
      const doc = site.get(f) ?? '';
      for (const ref of [...attrs(doc, 'href'), ...attrs(doc, 'src')]) {
        if (/^https?:/.test(ref)) continue;
        const [p, frag] = ref.split('#');
        if (p === '') {
          expect(ids(doc).has(frag), `${f}: #${frag}`).toBe(true);
          continue;
        }
        expect(p.startsWith('/'), `${f}: ${ref} debe ser absoluta`).toBe(true);
        expect(exists(p), `${f}: ${ref}`).toBe(true);
        if (frag) {
          const target = site.get(p.endsWith('/') ? p.slice(1) + 'index.html' : p.slice(1)) ?? '';
          expect(ids(target).has(frag), `${f}: ${ref}`).toBe(true);
        }
      }
    }
    // Cada página enlaza a todas las páginas de su idioma (pie).
    for (const page of PAGES) for (const lang of LANGS) for (const p of PAGES) expect(html(page, lang)).toContain(`href="${p.path[lang]}"`);
  });

  it('sin recursos externos, sin scripts ni estilos en línea y sin rastreadores', () => {
    const externalLinks = new Set<string>();
    for (const f of HTML_FILES) {
      const doc = site.get(f) ?? '';
      for (const s of attrs(doc, 'src')) expect(s, f).toMatch(/^\/assets\/[a-z-]+\.js$/);
      for (const m of doc.matchAll(/<link\b[^>]*>/g)) {
        const href = /href="([^"]*)"/.exec(m[0])?.[1] ?? '';
        expect(href.startsWith('/') && !href.startsWith('//'), `${f}: ${m[0]}`).toBe(true);
      }
      expect(doc, f).not.toMatch(/<script(?![^>]*\bsrc=)[^>]*>/); // ningún script en línea
      expect(doc, f).not.toMatch(/<(style|iframe|img|object|embed|video|audio|source|base)\b/);
      expect(doc, f).not.toMatch(/\sstyle="|\son[a-z]+="|javascript:/i);
      expect(doc, f).not.toMatch(/googletagmanager|google-analytics|gtag\(|facebook\.net|fonts\.googleapis|cdn\./i);
      for (const m of doc.matchAll(/<a\b[^>]*href="(https?:[^"]*)"[^>]*>/g)) {
        externalLinks.add(m[1]);
        expect(m[0], f).toMatch(/rel="noopener"/);
      }
    }
    // Las únicas salidas son enlaces (no recursos) al sitio del desarrollador.
    expect([...externalLinks]).toEqual(['https://parallext.com']);
    const css = site.get('assets/site.css') ?? '';
    expect(css).not.toMatch(/@import|url\(|@font-face/);
    const js = site.get('assets/delete-account.js') ?? '';
    expect(js).not.toMatch(/https?:\/\/|document\.cookie|localStorage|sessionStorage|eval\(|new Function/);
    // Ningún carácter fuera de ASCII en el script (se sirve sin charset).
    expect([...js].every((c) => c.charCodeAt(0) < 128)).toBe(true);
  });

  it('la página de borrado tiene el formulario completo en cada idioma', () => {
    const page = PAGES.find((p) => p.id === 'delete');
    if (!page) throw new Error('sin página de borrado');
    expect(page.script).toBe('/assets/delete-account.js');
    for (const lang of LANGS) {
      const doc = html(page, lang);
      for (const id of ['delete-form', 'account-code', 'delete-submit', 'delete-step-code', 'delete-step-confirm', 'delete-confirm-code', 'delete-yes', 'delete-no', 'delete-status']) {
        expect(ids(doc).has(id), `${lang}: #${id}`).toBe(true);
      }
      for (const key of Object.keys(UI[lang].deleteForm.msg)) {
        const attr = 'data-msg-' + key.replace(/[A-Z]/g, (c) => '-' + c.toLowerCase());
        expect(doc, `${lang}: ${attr}`).toMatch(new RegExp(`${attr}="[^"]+"`));
      }
      expect(doc, lang).toContain('{minutes}');
      expect(doc, lang).toMatch(/<form id="delete-form"[^>]*\smethod="post"/); // sin JS nunca un GET con el código en la URL
      expect(doc, lang).toMatch(/<label for="account-code">/);
      expect(doc, lang).toMatch(/role="status" aria-live="polite"/);
      expect(doc, lang).toContain('<noscript>');
    }
  });

  it('app-ads.txt es una plantilla: solo comentarios hasta tener los IDs reales', () => {
    const ads = site.get('app-ads.txt') ?? '';
    const lines = ads.split('\n').filter((l) => l.trim() !== '');
    expect(lines.length).toBeGreaterThan(0);
    for (const l of lines) expect(l.startsWith('#'), l).toBe(true);
    for (const network of ['applovin.com', 'google.com', 'unity.com', 'facebook.com', 'mintegral.com']) expect(ads).toContain(`# ${network}, `);
  });
});
