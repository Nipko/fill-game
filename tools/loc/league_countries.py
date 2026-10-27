#!/usr/bin/env python3
"""
league_countries.py - Amazon Fill (Tintalú): la tabla ISO completa de países de la Liga del río, en los tres idiomas (D-057, P7 (a)).

Con el lanzamiento mundial (D-055, D-057), un jugador de cualquier país tiene que ver el nombre de su país en la Liga (en la
pestaña del país, en las filas de Global y en Ajustes), no «Otro país» (docs/dev/lanzamiento-mundial.md §4.2). Los nombres no se
escriben a mano (.claude/rules/localizacion.md): salen de CLDR por la instantánea tools/loc/data/cldr_regions.json, que escribe
tools/loc/cldr_regions.mjs (Intl.DisplayNames de Node, ICU completo).

Escribe:
- en Assets/_Project/Data/Loc/strings.csv, una fila league_country_xx por cada país de la instantánea que no tenga ya una fila
  escrita a mano (las 21 de D-046, con sus notas del glosario: no se tocan; si su nombre no es el de CLDR, se avisa). Las filas
  generadas llevan en notes la marca GENERATED_MARK y se rehacen enteras en cada ejecución; las demás filas de la tabla quedan
  byte a byte como estaban, y las nuevas entran en su sitio por orden ordinal (.claude/rules/data-json.md);
- Assets/_Project/Scripts/UI/LeagueCountries.Generated.cs: los códigos con nombre (los a mano y los generados), que usa
  AF.UI.LeagueText.NamedCountries.

Cada fila generada: es-419, en y pt-BR de CLDR; context; max_chars = el mayor de 24 (el de las filas de D-046) y la longitud del
nombre más largo de los tres; status aprobado_es y revisado_nativo en en y pt-BR [P]: CLDR es la fuente de Android, iOS y los
navegadores, revisada por hablantes nativos (D-057: sin abogado ni revisor, se toma la fuente de quien ya lo hace bien); los
nombres de lugar siguen pendientes del asesor cultural (07 §8.2), como las filas de D-046, que D-054 pasó a aprobado_es.

Uso (desde la raíz del repositorio):
    python tools/loc/league_countries.py              # escribe strings.csv y LeagueCountries.Generated.cs
    python tools/loc/league_countries.py --check      # no escribe; código 1 si alguno difiere de lo que generaría
    python tools/loc/league_countries.py --self-test  # pruebas propias, en memoria
Códigos: 0 OK · 1 difiere o error · 2 argumentos.
Solo la biblioteca estándar. Python 3.14.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from apply_translations import CsvError, TABLE_HEADER, decode, quote, split_records, unquote  # noqa: E402

REPO = Path(__file__).resolve().parents[2]
SNAPSHOT = REPO / "tools" / "loc" / "data" / "cldr_regions.json"
STRINGS = REPO / "Assets" / "_Project" / "Data" / "Loc" / "strings.csv"
OUT_CS = REPO / "Assets" / "_Project" / "Scripts" / "UI" / "LeagueCountries.Generated.cs"

PREFIX = "league_country_"
OTHER = PREFIX + "other"
GENERATED_MARK = "Generado por tools/loc/league_countries.py"
MIN_MAX_CHARS = 24
LOCALES = ("es-419", "en", "pt-BR")
# D-057 (decisiones de la revisión del código mundial, punto 2): el asesor cultural revisa primero estos nombres.
POLITICAL = ("EH", "PS", "TW", "XK")
POLITICAL_NOTE = " Prioridad del asesor: nombre con carga política."


def load_snapshot(text: str) -> tuple[str, dict[str, dict[str, str]]]:
    doc = json.loads(text)
    if doc.get("format") != "af-cldr-regions/1":
        raise ValueError("la instantánea no es af-cldr-regions/1")
    names = doc["names"]
    for code, row in names.items():
        if len(code) != 2 or not code.isascii() or not code.isupper():
            raise ValueError("código raro en la instantánea: %r" % code)
        for loc in LOCALES:
            if not row.get(loc):
                raise ValueError("falta el nombre de %s en %s" % (code, loc))
    return doc["cldr"], names


def row_for(code: str, names: dict[str, str], cldr: str) -> list[str]:
    longest = max(len(names[loc]) for loc in LOCALES)
    context = ("Nombre del país %s (ISO 3166-1) en la Liga: junto a su placa con el código en la pestaña del país y en las filas de "
               "Global, y en Ajustes («País en la Liga: {country}»). Liga del río (D-046); tabla completa de D-057 (P7 (a))." % code)
    notes = ("%s desde CLDR %s (Unicode; nombres revisados por hablantes nativos): no se edita a mano. [P] Pendiente del asesor "
             "cultural (lugares; 07 §8.2)." % (GENERATED_MARK, cldr))
    if code in POLITICAL:
        notes += POLITICAL_NOTE
    return [PREFIX + code.lower(), names["es-419"], names["en"], names["pt-BR"], context, str(max(MIN_MAX_CHARS, longest)),
            "aprobado_es", "revisado_nativo", "revisado_nativo", notes, ""]


def render_row(fields: list[str]) -> str:
    return ",".join(quote(f) for f in fields)


def generate(strings_text: str, snapshot_text: str, warnings: list[str] | None = None) -> tuple[str, str]:
    """(strings.csv, LeagueCountries.Generated.cs) tal como deben quedar."""
    cldr, names = load_snapshot(snapshot_text)
    records = split_records(strings_text, "strings.csv")
    header = [unquote(f) for f in records[0]]
    if header != TABLE_HEADER:
        raise CsvError("strings.csv: cabecera inesperada")
    # Cada registro se recompone desde sus campos crudos (con sus comillas): las filas que no se generan quedan byte a byte.

    kept: list[tuple[str, str]] = []  # (clave, texto crudo del registro) de las filas que no son generadas
    hand: set[str] = set()
    for fields in records[1:]:
        key = unquote(fields[0])
        notes = unquote(fields[9]) if len(fields) > 9 else ""
        if key.startswith(PREFIX) and key != OTHER and GENERATED_MARK in notes:
            continue  # se rehace
        kept.append((key, ",".join(fields)))
        if key.startswith(PREFIX) and key != OTHER:
            code = key[len(PREFIX):].upper()
            hand.add(code)
            row = names.get(code)
            if row is None:
                if warnings is not None:
                    warnings.append("fila a mano sin país en CLDR: " + key)
            else:
                for i, loc in enumerate(LOCALES):
                    if unquote(fields[1 + i]) != row[loc] and warnings is not None:
                        warnings.append("%s en %s: «%s» a mano, «%s» en CLDR (se queda la de mano)" % (key, loc, unquote(fields[1 + i]), row[loc]))

    generated = [(PREFIX + code.lower(), render_row(row_for(code, names[code], cldr))) for code in names if code not in hand]
    rows = kept + generated
    rows.sort(key=lambda kv: kv[0])  # ordinal, como sorted() de Python (data-json.md)
    keys = [k for k, _ in rows]
    if len(keys) != len(set(keys)):
        raise CsvError("strings.csv: claves repetidas")
    out_csv = ",".join(records[0]) + "\n" + "".join(raw + "\n" for _, raw in rows)

    codes = sorted(hand | {c for c in names if c not in hand})
    return out_csv, render_cs(codes, cldr)


def render_cs(codes: list[str], cldr: str) -> str:
    body = []
    for i in range(0, len(codes), 20):
        body.append("            " + " ".join('"%s",' % c for c in codes[i:i + 20]))
    return (
        "// <auto-generated>\n"
        "// Generado por tools/loc/league_countries.py desde la instantánea de CLDR %s (tools/loc/data/cldr_regions.json). No lo edites\n"
        "// a mano: vuelve a ejecutar el generador (python tools/loc/league_countries.py; --check lo comprueba).\n"
        "// </auto-generated>\n"
        "namespace AF.UI\n"
        "{\n"
        "    /// <summary>Los países con nombre en <c>strings.csv</c> (<c>league_country_xx</c>; D-057, P7 (a)): ISO 3166-1 alfa-2 más XK.</summary>\n"
        "    internal static class LeagueCountriesData\n"
        "    {\n"
        "        /// <summary>Los códigos, en orden ordinal (%d).</summary>\n"
        "        internal static readonly string[] Codes =\n"
        "        {\n"
        "%s\n"
        "        };\n"
        "    }\n"
        "}\n" % (cldr, len(codes), "\n".join(body))
    )


def _read(path: Path) -> str | None:
    if not path.exists():
        return None
    return decode(path.read_bytes(), path.name)


def _write(path: Path, text: str) -> None:
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)


def self_test() -> int:
    failures: list[str] = []

    def check(name: str, cond: bool) -> None:
        if not cond:
            failures.append(name)

    head = ",".join(TABLE_HEADER) + "\n"
    snap = json.dumps({"format": "af-cldr-regions/1", "cldr": "99", "names": {
        "BR": {"es-419": "Brasil", "en": "Brazil", "pt-BR": "Brasil"},
        "JP": {"es-419": "Japón", "en": "Japan", "pt-BR": "Japão"},
        "CI": {"es-419": "Costa de Marfil", "en": "Côte d’Ivoire", "pt-BR": "Costa do Marfim"},
        "GS": {"es-419": "Islas Georgia del Sur y Sándwich del Sur", "en": "South Georgia & South Sandwich Islands",
               "pt-BR": "Ilhas Geórgia do Sul e Sandwich do Sul"},
    }})
    table = (head
             + "a_key,A,A,A,ctx,10,aprobado_es,revisado_nativo,revisado_nativo,,\n"
             + 'league_country_br,Brasil,Brazil,Brasil,"ctx, a mano",24,aprobado_es,revisado_nativo,revisado_nativo,Del glosario,\n'
             + "league_country_other,Otro país,Other country,Outro país,ctx,24,aprobado_es,revisado_nativo,revisado_nativo,,\n"
             + "zeta,Z,Z,Z,ctx,10,aprobado_es,revisado_nativo,revisado_nativo,,\n")
    warnings: list[str] = []
    out, cs = generate(table, snap, warnings)
    keys = [unquote(r[0]) for r in split_records(out, "t")[1:]]
    check("orden ordinal", keys == sorted(keys))
    check("las filas a mano quedan igual", 'league_country_br,Brasil,Brazil,Brasil,"ctx, a mano",24,' in out)
    check("una fila por país sin fila", keys.count("league_country_jp") == 1 and "league_country_ci" in keys and "league_country_gs" in keys)
    check("nada más", len(keys) == 7)
    jp = [r for r in split_records(out, "t") if unquote(r[0]) == "league_country_jp"][0]
    check("los tres idiomas", [unquote(f) for f in jp[1:4]] == ["Japón", "Japan", "Japão"])
    check("estados", [unquote(f) for f in jp[6:9]] == ["aprobado_es", "revisado_nativo", "revisado_nativo"])
    gs = [r for r in split_records(out, "t") if unquote(r[0]) == "league_country_gs"][0]
    check("max_chars cubre el más largo", unquote(gs[5]) == "40")
    check("max_chars mínimo 24", unquote(jp[5]) == "24")
    check("marca de generado", GENERATED_MARK in unquote(jp[9]))
    check("sin carga política, sin la nota", POLITICAL_NOTE.strip() not in unquote(jp[9]))
    check("carga política en la nota", POLITICAL_NOTE.strip() in generate(table, snap.replace('"CI"', '"PS"'))[0])
    again, cs2 = generate(out, snap)
    check("idempotente", again == out and cs2 == cs)
    check("C# con todos los códigos", '"BR", "CI", "GS", "JP",' in cs)
    check("sin avisos con la fila a mano igual a CLDR", warnings == [])
    w2: list[str] = []
    generate(table.replace("league_country_br,Brasil,Brazil", "league_country_br,Brasil,Brasil"), snap, w2)
    check("avisa si la fila a mano no es la de CLDR", len(w2) == 1)
    try:
        generate(table, snap.replace("af-cldr-regions/1", "x"))
        failures.append("no rechaza otra instantánea")
    except ValueError:
        pass

    real = generate(_read(STRINGS) or "", _read(SNAPSHOT) or "")
    check("tabla real: 250 países en el C#", real[1].count('"') == 2 * 250)

    for f in failures:
        print("FALLA: " + f)
    if failures:
        return 1
    print("self-test OK")
    return 0


def main(argv: list[str]) -> int:
    for stream in (sys.stdout, sys.stderr):
        stream.reconfigure(encoding="utf-8")
    ap = argparse.ArgumentParser(description="Genera los nombres de país de la Liga desde CLDR (D-057).")
    ap.add_argument("--check", action="store_true", help="no escribe; código 1 si algún archivo difiere")
    ap.add_argument("--self-test", action="store_true", help="pruebas propias")
    args = ap.parse_args(argv)
    if args.self_test:
        return self_test()
    try:
        warnings: list[str] = []
        csv_text, cs = generate(_read(STRINGS) or "", _read(SNAPSHOT) or "", warnings)
    except (CsvError, ValueError, KeyError) as e:
        print("ERROR: " + str(e), file=sys.stderr)
        return 1
    for w in warnings:
        print("aviso: " + w)
    targets = [(STRINGS, csv_text), (OUT_CS, cs)]
    if args.check:
        bad = [str(p) for p, t in targets if _read(p) != t]
        for b in bad:
            print("DIFIERE: " + b)
        if bad:
            print("Ejecuta: python tools/loc/league_countries.py")
            return 1
        print("identico (%d países)" % cs.count('",'))
        return 0
    for p, t in targets:
        _write(p, t)
    print("escrito %s y %s (%d países)" % (STRINGS, OUT_CS, cs.count('",')))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
