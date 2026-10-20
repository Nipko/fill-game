#!/usr/bin/env python3
"""
apply_translations.py - Amazon Fill (Tintalú): vuelca traducciones entregadas aparte sobre las tablas de textos.

Las tablas de textos son Assets/_Project/Data/Loc/strings.csv y facts.csv (contrato 32; 08 §4.7; D-018). Las
traducciones con IA de la tarea T2 (D-022, pregunta 51 (b)) se entregan en docs/dev/traspaso/spark-traducciones/, un CSV
por tabla, para no chocar con las ramas que añaden filas a las tablas:

    key,es-419,en,pt-BR,status_en,status_pt-BR

- key: la clave de la fila de la tabla;
- es-419: el texto fuente con el que se tradujo (solo de referencia: si la tabla ya dice otra cosa, la traducción es de un
  español viejo y es un conflicto: 08 §4.7, «si el asesor cambia el español, los estados de los demás idiomas vuelven a
  vacío»);
- en, pt-BR: la traducción (vacía = nada que aplicar en ese idioma);
- status_en, status_pt-BR: el estado del idioma, «traducido» o «revisado_nativo» (08 §4.7), obligatorio con texto.

El archivo de traducciones va en UTF-8 sin BOM, LF, RFC 4180 y en orden ordinal por clave. La tabla de destino sale del
nombre: strings*.csv va a strings.csv y facts*.csv a facts.csv.

Uso (desde la raíz del repositorio):
    python tools/loc/apply_translations.py                 # aplica strings.csv y facts.csv de la carpeta de entrega
    python tools/loc/apply_translations.py --check         # no escribe; código 1 si aplicar cambiaría algo o hay conflictos
    python tools/loc/apply_translations.py <archivos.csv>  # solo esos archivos (por ejemplo, strings_mapa.csv o strings_p14.csv)
    python tools/loc/apply_translations.py --self-test     # pruebas propias, en memoria
Opciones: --loc-dir <carpeta de las tablas> (por defecto Assets/_Project/Data/Loc).

Qué hace y qué no:
- Cambia solo las celdas en, pt-BR, status_en y status_pt-BR de las filas citadas. Las demás filas, las demás columnas, el
  orden, las comillas y los saltos de línea quedan byte a byte como estaban (cada campo que no se toca conserva su texto
  crudo, comillas incluidas).
- Nunca pisa una celda ya rellena: si la tabla ya tiene un texto o un estado distinto del entregado, es un conflicto; si es
  el mismo, no hace nada (aplicar dos veces da lo mismo).
- Nunca escribe en las filas map.* de facts.csv: las regenera solo tools/river-map/stops.py, con en y pt-BR vacíos (08 §4.5),
  y una traducción ahí se perdería en la siguiente regeneración.
- Comprueba cada traducción como LocTablesTests: los mismos marcadores con nombre que es-419 (también {sep}), las llaves
  parejas y la longitud visible ≤ max_chars × 1,3 (la regla de VisibleLength).
- Una clave que ya no existe en la tabla es un aviso (se salta), no un conflicto.
- Si hay algún conflicto o error, no escribe nada en ninguna tabla.
Códigos: 0 OK (con --check: nada que aplicar) · 1 conflictos o errores (con --check: también si aplicar cambiaría algo) ·
2 argumentos o archivos que no se pueden leer.
Solo la biblioteca estándar.
"""
from __future__ import annotations

import argparse
import csv
import io
import re
import sys
from dataclasses import dataclass, field
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
DEFAULT_LOC = REPO / "Assets" / "_Project" / "Data" / "Loc"
DEFAULT_DIR = REPO / "docs" / "dev" / "traspaso" / "spark-traducciones"
DEFAULT_FILES = ("strings.csv", "facts.csv")

TABLE_HEADER = ["key", "es-419", "en", "pt-BR", "context", "max_chars", "status", "status_en", "status_pt-BR", "notes", "args"]
TR_HEADER = ["key", "es-419", "en", "pt-BR", "status_en", "status_pt-BR"]
LOCALES = ("en", "pt-BR")
STATUSES = ("traducido", "revisado_nativo")
GENERATED_PREFIX = "map."  # bloque de facts.csv que escribe solo tools/river-map/stops.py


class CsvError(Exception):
    pass


# ------------------------------------------------------------------------------------------------ CSV con texto crudo

def split_records(text: str, name: str) -> list[list[str]]:
    """Registros RFC 4180 de text, cada uno como la lista de sus campos crudos (con sus comillas), sin el salto final."""
    records: list[list[str]] = []
    i, n = 0, len(text)
    line = 1
    while i < n:
        fields: list[str] = []
        while True:
            start = i
            if i < n and text[i] == '"':
                i += 1
                while True:
                    if i >= n:
                        raise CsvError(f"{name}, línea {line}: comillas sin cerrar")
                    if text[i] == '"':
                        if i + 1 < n and text[i + 1] == '"':
                            i += 2
                            continue
                        i += 1
                        break
                    if text[i] == "\n":
                        line += 1
                    i += 1
                if i < n and text[i] not in ",\n":
                    raise CsvError(f"{name}, línea {line}: texto tras las comillas de cierre")
            else:
                while i < n and text[i] not in ",\n":
                    if text[i] == '"':
                        raise CsvError(f"{name}, línea {line}: comilla dentro de un campo sin comillas")
                    i += 1
            fields.append(text[start:i])
            if i < n and text[i] == ",":
                i += 1
                continue
            break
        records.append(fields)
        if i < n and text[i] == "\n":
            line += 1
            i += 1
        elif i >= n:
            raise CsvError(f"{name}: no termina en salto de línea")
    return records


def unquote(raw: str) -> str:
    if raw.startswith('"'):
        return raw[1:-1].replace('""', '"')
    return raw


def quote(value: str) -> str:
    """Como csv.QUOTE_MINIMAL (la forma de las tablas): comillas si hay coma, comilla o salto de línea."""
    if any(c in value for c in ',"\n\r'):
        return '"' + value.replace('"', '""') + '"'
    return value


def decode(data: bytes, name: str) -> str:
    if data.startswith(b"\xef\xbb\xbf"):
        raise CsvError(f"{name}: lleva BOM (va en UTF-8 sin BOM)")
    try:
        text = data.decode("utf-8")
    except UnicodeDecodeError as e:
        raise CsvError(f"{name}: no es UTF-8 ({e})") from None
    if "\r" in text:
        raise CsvError(f"{name}: lleva CR (va con LF)")
    return text


# ------------------------------------------------------------------------------------------------ reglas de LocTablesTests

def visible_length(s: str, start: int = 0, end: int | None = None) -> int:
    """La VisibleLength de LocTablesTests: sin etiquetas ni saltos; un plural cuenta su forma más larga; {} = 2,
    {sep} = 3 y cualquier otro marcador, lo que ocupa escrito."""
    end = len(s) if end is None else end
    n, i = 0, start
    while i < end:
        c = s[i]
        if c == "{":
            j = i + 1
            while j < end and (s[j].isalnum() or s[j] == "_"):
                j += 1
            if j < end and s[j] == "}":
                name = s[i + 1:j]
                n += 2 if not name else (3 if name == "sep" else j + 1 - i)
                i = j + 1
                continue
            if s.startswith(":plural:", j):
                k = j + len(":plural:")
                form, depth, longest = k, 0, 0
                while k < end:
                    ch = s[k]
                    if ch == "{":
                        depth += 1
                    elif ch == "}" and depth > 0:
                        depth -= 1
                    elif ch in "|}" and depth == 0:
                        longest = max(longest, visible_length(s, form, k))
                        form = k + 1
                        if ch == "}":
                            break
                    k += 1
                n += longest
                i = k + 1
                continue
        if c == "<":
            close = s.find(">", i)
            if close > i + 1 and (s[i + 1].isalpha() or s[i + 1] == "/"):
                i = close + 1
                continue
        if c != "\n":
            n += 1
        i += 1
    return n


MARKER = re.compile(r"\{([A-Za-z0-9_]+)(?=[}:])")


def markers(s: str) -> list[str]:
    return sorted(MARKER.findall(s))


def text_problems(source: str, text: str, max_chars: int) -> list[str]:
    out = []
    if markers(text) != markers(source):
        out.append(f"marcadores {markers(text)} y no {markers(source)} como es-419")
    if text.count("{") != text.count("}"):
        out.append("llaves desparejas")
    if max_chars > 0 and visible_length(text) * 10 > max_chars * 13:
        out.append(f"mide {visible_length(text)} y pasa de max_chars × 1,3 ({max_chars} × 1,3)")
    return out


# ------------------------------------------------------------------------------------------------ aplicar

@dataclass
class Result:
    text: str | None = None          # tabla nueva (None si no cambia)
    changes: list[str] = field(default_factory=list)
    conflicts: list[str] = field(default_factory=list)
    warnings: list[str] = field(default_factory=list)


def read_translations(text: str, name: str) -> tuple[list[dict], list[str]]:
    errors: list[str] = []
    rows = list(csv.reader(io.StringIO(text, newline="")))
    if not rows or rows[0] != TR_HEADER:
        return [], [f"{name}: la cabecera tiene que ser {','.join(TR_HEADER)}"]
    out = []
    keys = []
    for line, r in enumerate(rows[1:], start=2):
        if len(r) != len(TR_HEADER):
            errors.append(f"{name}, fila {line}: {len(r)} campos y no {len(TR_HEADER)}")
            continue
        d = dict(zip(TR_HEADER, r))
        keys.append(d["key"])
        for loc in LOCALES:
            st = d["status_" + loc]
            if d[loc] and st not in STATUSES:
                errors.append(f"{name} {d['key']} ({loc}): estado {st!r}; con texto va {' o '.join(STATUSES)}")
            if not d[loc] and st:
                errors.append(f"{name} {d['key']} ({loc}): estado {st!r} sin texto")
        out.append(d)
    if keys != sorted(keys):
        errors.append(f"{name}: las filas no van en orden ordinal por clave")
    if len(set(keys)) != len(keys):
        errors.append(f"{name}: claves repetidas")
    return out, errors


def apply(table_text: str, table_name: str, translations: list[dict], tr_name: str) -> Result:
    res = Result()
    records = split_records(table_text, table_name)
    header = [unquote(f) for f in records[0]]
    missing = [c for c in TABLE_HEADER if c not in header]
    if missing:
        res.conflicts.append(f"{table_name}: a la cabecera le faltan {missing} (08 §4.7)")
        return res
    col = {c: header.index(c) for c in header}
    index: dict[str, int] = {}
    for r, fields in enumerate(records[1:], start=1):
        if len(fields) != len(header):
            res.conflicts.append(f"{table_name}, registro {r + 1}: {len(fields)} campos y no {len(header)}")
            return res
        index[unquote(fields[0])] = r
    changed = False
    for tr in translations:
        key = tr["key"]
        if key not in index:
            res.warnings.append(f"{tr_name} {key}: la clave ya no existe en {table_name}; se salta")
            continue
        fields = records[index[key]]
        values = {c: unquote(fields[col[c]]) for c in header}
        if key.startswith(GENERATED_PREFIX) and table_name.startswith("facts"):
            res.conflicts.append(f"{tr_name} {key}: fila del bloque map.*, que regenera solo tools/river-map/stops.py")
            continue
        if tr["es-419"] != values["es-419"]:
            res.conflicts.append(f"{tr_name} {key}: el es-419 de la tabla cambió desde la traducción "
                                 f"(tabla «{values['es-419']}», traducción «{tr['es-419']}»)")
            continue
        try:
            max_chars = int(values["max_chars"] or "0")
        except ValueError:
            max_chars = 0
        for loc in LOCALES:
            text, status = tr[loc], tr["status_" + loc]
            if not text:
                continue
            have_text, have_status = values[loc], values["status_" + loc]
            if have_text or have_status:
                if have_text == text and have_status == status:
                    continue  # ya aplicada
                res.conflicts.append(f"{tr_name} {key} ({loc}): la tabla ya tiene «{have_text}» ({have_status or 'sin estado'}); "
                                     f"no se pisa con «{text}» ({status})")
                continue
            probs = text_problems(values["es-419"], text, max_chars)
            if probs:
                res.conflicts.append(f"{tr_name} {key} ({loc}): " + "; ".join(probs))
                continue
            fields[col[loc]] = quote(text)
            fields[col["status_" + loc]] = quote(status)
            res.changes.append(f"{key} ({loc})")
            changed = True
    if changed and not res.conflicts:
        res.text = "".join(",".join(f) + "\n" for f in records)
    return res


def target_of(path: Path) -> str | None:
    name = path.name
    if name.startswith("strings"):
        return "strings.csv"
    if name.startswith("facts"):
        return "facts.csv"
    return None


def run(files: list[Path], loc_dir: Path, check: bool, out=sys.stdout) -> int:
    tables: dict[str, str] = {}
    pending: dict[str, list[tuple[list[dict], str]]] = {}
    errors: list[str] = []
    for f in files:
        target = target_of(f)
        if target is None:
            print(f"ERROR {f}: el nombre tiene que empezar por strings o facts", file=out)
            return 2
        try:
            tr_text = decode(f.read_bytes(), f.name)
            if target not in tables:
                tables[target] = decode((loc_dir / target).read_bytes(), target)
        except OSError as e:
            print(f"ERROR no se puede leer: {e}", file=out)
            return 2
        except CsvError as e:
            print(f"ERROR {e}", file=out)
            return 2
        rows, errs = read_translations(tr_text, f.name)
        errors.extend(errs)
        pending.setdefault(target, []).append((rows, f.name))
    new_tables: dict[str, str] = {}
    total_changes, conflicts, warnings = 0, list(errors), []
    for target, batches in pending.items():
        text = tables[target]
        for rows, name in batches:
            try:
                res = apply(text, target, rows, name)
            except CsvError as e:
                conflicts.append(str(e))
                continue
            conflicts.extend(res.conflicts)
            warnings.extend(res.warnings)
            total_changes += len(res.changes)
            if res.text is not None:
                text = res.text
        if text != tables[target]:
            new_tables[target] = text
    for w in warnings:
        print(f"AVISO {w}", file=out)
    for c in conflicts:
        print(f"CONFLICTO {c}", file=out)
    if conflicts:
        print(f"FAIL {len(conflicts)} conflictos o errores: no se escribe nada", file=out)
        return 1
    if check:
        if total_changes:
            print(f"FAIL --check: aplicar cambiaría {total_changes} celdas de texto en {', '.join(sorted(new_tables))}", file=out)
            return 1
        print("OK --check: las traducciones ya están en las tablas (nada que aplicar)", file=out)
        return 0
    for target, text in new_tables.items():
        (loc_dir / target).write_bytes(text.encode("utf-8"))
    print(f"OK {total_changes} traducciones aplicadas" + (f" en {', '.join(sorted(new_tables))}" if new_tables else " (nada que aplicar)"), file=out)
    return 0


# ------------------------------------------------------------------------------------------------ autoprueba

def self_test() -> int:
    import tempfile

    H = ",".join(TABLE_HEADER) + "\n"
    T = ",".join(TR_HEADER) + "\n"
    table = (H
             + 'a_key,"Hola, mundo",,,"ctx, con coma",20,borrador,,,"nota ""citada""",\n'
             + "b_key,Nivel {n},Level {n},Nível {n},ctx,12,aprobado_es,traducido,traducido,,\n"
             + 'c_key,"Dos\nlíneas",,,ctx,10,pendiente_asesor,,,,\n'
             + "d_key,{n:plural:Te falta {}|Te faltan {}},,,ctx,12,aprobado_es,,,,\n")
    facts = (H + "fact.x.01,Mide 35 m,,,ctx,90,pendiente_asesor,,,,\n"
             + "map.stop.x.dato_01,Dato,,,ctx,90,pendiente_asesor,,,generado,\n")
    checks: list[tuple[str, bool]] = []

    def case(name, ok):
        checks.append((name, bool(ok)))

    def tr(rows):
        return T + "".join(",".join(quote(v) for v in r) + "\n" for r in rows)

    # 1. Aplica en celdas vacías y deja todo lo demás byte a byte.
    rows, errs = read_translations(tr([["a_key", "Hola, mundo", "Hello, world", "Olá, mundo", "traducido", "traducido"],
                                       ["c_key", "Dos\nlíneas", "Two\nlines", "", "traducido", ""]]), "t.csv")
    r = apply(table, "strings.csv", rows, "t.csv")
    case("lee la entrega sin errores", not errs)
    case("tres celdas aplicadas", len(r.changes) == 3 and not r.conflicts)
    lines_in, lines_out = table.split("\n"), (r.text or "").split("\n")
    untouched = [ln for ln in lines_in if ln.startswith(("key,", "b_key,", "d_key,"))]
    case("filas sin tocar idénticas", len(untouched) == 3 and all(ln in lines_out for ln in untouched))
    case("las comillas y la nota «citada» se conservan", 'nota ""citada""' in (r.text or "") and '"ctx, con coma"' in (r.text or ""))
    case("la celda con coma va entre comillas", '"Hello, world"' in (r.text or ""))
    case("el salto de línea dentro de la celda se conserva", '"Two\nlines",' in (r.text or ""))
    parsed = list(csv.DictReader(io.StringIO(r.text or "", newline="")))
    case("el CSV resultante se lee con csv", parsed[0]["en"] == "Hello, world" and parsed[0]["status_pt-BR"] == "traducido"
         and parsed[2]["pt-BR"] == "" and parsed[2]["status_pt-BR"] == "")
    case("mismo número de filas y mismo orden", [p["key"] for p in parsed] == ["a_key", "b_key", "c_key", "d_key"])
    # 2. Idempotente: aplicar sobre el resultado no cambia nada.
    r2 = apply(r.text or "", "strings.csv", rows, "t.csv")
    case("aplicar dos veces no cambia nada", r2.text is None and not r2.changes and not r2.conflicts)
    # 3. Celda ya rellena con otro texto: conflicto y no escribe.
    rows3, _ = read_translations(tr([["b_key", "Nivel {n}", "Lvl {n}", "", "traducido", ""]]), "t.csv")
    r3 = apply(table, "strings.csv", rows3, "t.csv")
    case("no pisa una celda rellena", r3.conflicts and r3.text is None)
    # 4. El español cambió desde la traducción.
    rows4, _ = read_translations(tr([["a_key", "Hola mundo", "Hello world", "", "traducido", ""]]), "t.csv")
    case("es-419 distinto: conflicto", apply(table, "strings.csv", rows4, "t.csv").conflicts)
    # 5. Clave que ya no existe: aviso, sin conflicto.
    rows5, _ = read_translations(tr([["zz_gone", "X", "X", "", "traducido", ""]]), "t.csv")
    r5 = apply(table, "strings.csv", rows5, "t.csv")
    case("clave que ya no existe: aviso", r5.warnings and not r5.conflicts and r5.text is None)
    # 6. Marcadores, plurales y longitud.
    rows6, _ = read_translations(tr([["d_key", "{n:plural:Te falta {}|Te faltan {}}", "{count:plural:Need {}|Need {}}", "", "traducido", ""]]), "t.csv")
    case("marcador distinto: conflicto", apply(table, "strings.csv", rows6, "t.csv").conflicts)
    rows6b, _ = read_translations(tr([["d_key", "{n:plural:Te falta {}|Te faltan {}}", "{n:plural:Need {}|Need {} more}", "", "traducido", ""]]), "t.csv")
    case("plural válido de 12 × 1,3: aplica", apply(table, "strings.csv", rows6b, "t.csv").changes == ["d_key (en)"])
    rows6c, _ = read_translations(tr([["d_key", "{n:plural:Te falta {}|Te faltan {}}", "{n:plural:You need {}|You still need {}}", "", "traducido", ""]]), "t.csv")
    case("más de max_chars × 1,3: conflicto", apply(table, "strings.csv", rows6c, "t.csv").conflicts)
    case("VisibleLength como LocTablesTests", visible_length("Siguiente") == 9 and visible_length("Nivel {n}") == 9
         and visible_length("{n:plural:Te falta {} semilla|Te faltan {} semillas}") == 21
         and visible_length("+{n} Deshacer\nVer video") == 22 and visible_length("<b>{name}</b>{sep}{country}") == 18)
    # 7. El bloque map.* de facts.csv no se toca.
    rows7, _ = read_translations(tr([["fact.x.01", "Mide 35 m", "It's 35 m", "", "traducido", ""],
                                     ["map.stop.x.dato_01", "Dato", "Fact", "", "traducido", ""]]), "f.csv")
    r7 = apply(facts, "facts.csv", rows7, "f.csv")
    case("map.*: conflicto y nada escrito", r7.conflicts and r7.text is None)
    # 8. Errores de la entrega: estado, orden, cabecera.
    _, e8 = read_translations(tr([["b", "x", "y", "", "aprobado_es", ""], ["a", "x", "y", "", "traducido", ""]]), "t.csv")
    case("estado no válido y orden no ordinal", len(e8) == 2)
    _, e9 = read_translations("key,en,pt-BR\n", "t.csv")
    case("cabecera distinta: error", e9)
    _, e10 = read_translations(tr([["a", "x", "", "", "traducido", ""]]), "t.csv")
    case("estado sin texto: error", e10)
    # 9. Tablas mal formadas.
    for name, bad in (("BOM", b"\xef\xbb\xbf" + table.encode()), ("CR", table.replace("\n", "\r\n").encode())):
        try:
            decode(bad, "t.csv")
            case(name + ": error", False)
        except CsvError:
            case(name + ": error", True)
    try:
        split_records(H + 'a,"sin cerrar\n', "t.csv")
        case("comillas sin cerrar: error", False)
    except CsvError:
        case("comillas sin cerrar: error", True)
    # 10. Extremo a extremo con archivos: --check falla antes, aplica, --check pasa; con conflicto no escribe nada.
    with tempfile.TemporaryDirectory() as d:
        dpath = Path(d)
        (dpath / "strings.csv").write_bytes(table.encode())
        (dpath / "facts.csv").write_bytes(facts.encode())
        ts = dpath / "strings_t.csv"
        ts.write_bytes(tr([["a_key", "Hola, mundo", "Hello, world", "", "traducido", ""]]).encode())
        sink = io.StringIO()
        case("--check antes de aplicar: 1", run([ts], dpath, True, sink) == 1)
        case("aplica: 0", run([ts], dpath, False, sink) == 0)
        case("--check después: 0", run([ts], dpath, True, sink) == 0)
        written = (dpath / "strings.csv").read_bytes()
        case("escribe UTF-8 sin BOM y con LF", not written.startswith(b"\xef\xbb\xbf") and b"\r" not in written)
        tf = dpath / "facts_t.csv"
        tf.write_bytes(tr([["map.stop.x.dato_01", "Dato", "Fact", "", "traducido", ""]]).encode())
        before = (dpath / "strings.csv").read_bytes(), (dpath / "facts.csv").read_bytes()
        ts2 = dpath / "strings_u.csv"
        ts2.write_bytes(tr([["c_key", "Dos\nlíneas", "Two\nlines", "", "traducido", ""]]).encode())
        case("un conflicto en un archivo: 1", run([ts2, tf], dpath, False, sink) == 1)
        case("con conflicto no escribe ninguna tabla", ((dpath / "strings.csv").read_bytes(), (dpath / "facts.csv").read_bytes()) == before)
        bad = dpath / "otros.csv"
        bad.write_bytes(T.encode())
        case("nombre sin tabla: 2", run([bad], dpath, False, sink) == 2)
    failed = [n for n, ok in checks if not ok]
    for n, ok in checks:
        print(("PASS " if ok else "FAIL ") + n)
    print(f"{len(checks) - len(failed)}/{len(checks)} PASS")
    return 1 if failed else 0


def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description="Vuelca traducciones (key,es-419,en,pt-BR,status_en,status_pt-BR) sobre strings.csv y facts.csv.")
    ap.add_argument("files", nargs="*", type=Path, help=f"archivos de traducciones (por defecto, {' y '.join(DEFAULT_FILES)} de {DEFAULT_DIR.relative_to(REPO)})")
    ap.add_argument("--check", action="store_true", help="no escribe; código 1 si aplicar cambiaría algo o hay conflictos")
    ap.add_argument("--self-test", action="store_true", help="pruebas propias en memoria")
    ap.add_argument("--loc-dir", type=Path, default=DEFAULT_LOC, help="carpeta de strings.csv y facts.csv")
    args = ap.parse_args(argv)
    if args.self_test:
        return self_test()
    files = args.files or [DEFAULT_DIR / f for f in DEFAULT_FILES]
    return run(files, args.loc_dir, args.check)


if __name__ == "__main__":
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    sys.exit(main(sys.argv[1:]))
