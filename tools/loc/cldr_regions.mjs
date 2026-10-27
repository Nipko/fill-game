#!/usr/bin/env node
// cldr_regions.mjs - Amazon Fill (Tintalú): extrae de CLDR los nombres de los países en es-419, en y pt-BR (D-057, P7 (a)).
//
// La Liga del río muestra el país del jugador por su código ISO 3166-1 alfa-2 (docs/dev/liga.md §1). Con el lanzamiento
// mundial (D-055, D-057) hacen falta todos los países, en los tres idiomas, y no se escriben a mano (.claude/rules/localizacion.md:
// no se inventan nombres ni traducciones). La fuente es CLDR (Unicode Common Locale Data Repository), la que usan Android, iOS y
// los navegadores, a través de Intl.DisplayNames de Node, que lleva ICU completo con su CLDR.
//
// Escribe tools/loc/data/cldr_regions.json: una instantánea con la versión de ICU y de CLDR, para que la generación de las filas
// (tools/loc/league_countries.py, solo biblioteca estándar de Python) no dependa de la versión de Node de cada máquina. Solo
// hace falta volver a ejecutarlo para cambiar de versión de CLDR; después, league_countries.py vuelve a generar las filas.
//
// Uso (desde la raíz del repositorio; Node 22.18 o superior, el del stack):
//     node tools/loc/cldr_regions.mjs            # escribe la instantánea
//     node tools/loc/cldr_regions.mjs --check    # no escribe; código 1 si la instantánea difiere (con la misma versión de CLDR)
// Códigos: 0 OK · 1 difiere o falta un nombre · 2 argumentos.
//
// Reglas (fijas aquí, no a mano por país):
// - la lista de códigos es la de ISO 3166-1 alfa-2 (249 códigos asignados) más XK (Kosovo, código de usuario que CLDR, Android y
//   Google Play usan);
// - el nombre es el estilo «long» de CLDR, salvo en SHORT_STYLE, donde el «long» lleva una aclaración entre paréntesis
//   («RAE de Hong Kong (China)», «Territorios Palestinos») y CLDR da una forma corta propia;
// - un idioma por columna: es-419 (español de América Latina), en (inglés) y pt-BR (portugués de Brasil), las tres de la tabla.

import { readFileSync, writeFileSync, mkdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const HERE = dirname(fileURLToPath(import.meta.url));
const OUT = join(HERE, "data", "cldr_regions.json");
const LOCALES = ["es-419", "en", "pt-BR"];
const SHORT_STYLE = ["HK", "MO", "PS"];

// ISO 3166-1 alfa-2: los 249 códigos asignados, más XK.
const CODES = (
  "AD AE AF AG AI AL AM AO AQ AR AS AT AU AW AX AZ BA BB BD BE BF BG BH BI BJ BL BM BN BO BQ BR BS BT BV BW BY BZ " +
  "CA CC CD CF CG CH CI CK CL CM CN CO CR CU CV CW CX CY CZ DE DJ DK DM DO DZ EC EE EG EH ER ES ET FI FJ FK FM FO FR " +
  "GA GB GD GE GF GG GH GI GL GM GN GP GQ GR GS GT GU GW GY HK HM HN HR HT HU ID IE IL IM IN IO IQ IR IS IT JE JM JO JP " +
  "KE KG KH KI KM KN KP KR KW KY KZ LA LB LC LI LK LR LS LT LU LV LY MA MC MD ME MF MG MH MK ML MM MN MO MP MQ MR MS MT " +
  "MU MV MW MX MY MZ NA NC NE NF NG NI NL NO NP NR NU NZ OM PA PE PF PG PH PK PL PM PN PR PS PT PW PY QA RE RO RS RU RW " +
  "SA SB SC SD SE SG SH SI SJ SK SL SM SN SO SR SS ST SV SX SY SZ TC TD TF TG TH TJ TK TL TM TN TO TR TT TV TW TZ UA UG " +
  "UM US UY UZ VA VC VE VG VI VN VU WF WS YE YT ZA ZM ZW XK"
).split(" ");

function build() {
  if (CODES.length !== 250 || new Set(CODES).size !== CODES.length) {
    throw new Error("la lista de códigos no tiene 249 + XK distintos: " + CODES.length);
  }

  const names = {};
  for (const code of [...CODES].sort()) {
    const row = {};
    for (const locale of LOCALES) {
      const style = SHORT_STYLE.includes(code) ? "short" : "long";
      const dn = new Intl.DisplayNames([locale], { type: "region", style, fallback: "none" });
      const name = dn.of(code);
      if (!name || name === code) {
        throw new Error(`CLDR no tiene nombre para ${code} en ${locale}`);
      }
      row[locale] = name.normalize("NFC");
    }
    names[code] = row;
  }

  return {
    format: "af-cldr-regions/1",
    generator: "tools/loc/cldr_regions.mjs",
    source: "Unicode CLDR por Intl.DisplayNames de Node (ICU)",
    icu: process.versions.icu,
    cldr: process.versions.cldr,
    locales: LOCALES,
    short_style: SHORT_STYLE,
    names,
  };
}

function main(argv) {
  const check = argv.includes("--check");
  if (argv.some((a) => a !== "--check")) {
    console.error("uso: node tools/loc/cldr_regions.mjs [--check]");
    return 2;
  }

  const doc = build();
  const text = JSON.stringify(doc, null, 2) + "\n";
  if (check) {
    let old = null;
    try {
      old = readFileSync(OUT, "utf8");
    } catch {
      console.log("DIFIERE: falta " + OUT);
      return 1;
    }
    const prev = JSON.parse(old);
    if (prev.cldr !== doc.cldr) {
      console.log(`aviso: la instantánea es de CLDR ${prev.cldr} y este Node trae CLDR ${doc.cldr}; no se compara`);
      return 0;
    }
    if (old !== text) {
      console.log("DIFIERE: " + OUT + "; ejecuta node tools/loc/cldr_regions.mjs");
      return 1;
    }
    console.log(`identico (${Object.keys(doc.names).length} países, CLDR ${doc.cldr})`);
    return 0;
  }

  mkdirSync(dirname(OUT), { recursive: true });
  writeFileSync(OUT, text, { encoding: "utf8" });
  console.log(`escrito ${OUT} (${Object.keys(doc.names).length} países, CLDR ${doc.cldr}, ICU ${doc.icu})`);
  return 0;
}

process.exitCode = main(process.argv.slice(2));
