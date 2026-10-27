#!/usr/bin/env python3
"""
apply_translations.py - Amazon Fill (Tintalú): vuelca traducciones entregadas aparte sobre las tablas de textos.

Las tablas de textos son Assets/_Project/Data/Loc/strings.csv y facts.csv (contrato 32; 08 §4.7; D-018). Las
traducciones con IA de la tarea T2 (D-022, pregunta 51 (b)) se entregan en docs/dev/traspaso/spark-traducciones/, un CSV
por tabla, para no chocar con las ramas que añaden filas a las tablas (strings_t2b.csv: la segunda tanda, del 2026-10-22,
con las claves de UI que llegaron después de T2; lo que queda vacío a propósito, en pendientes_t2b.csv):

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
    python tools/loc/apply_translations.py                 # aplica strings.csv, strings_p14.csv, strings_t2b.csv, facts_mapa.csv y facts_mapa_2.csv de la carpeta de entrega
    python tools/loc/apply_translations.py --check         # no escribe; código 1 si aplicar cambiaría algo o hay conflictos
    python tools/loc/apply_translations.py <archivos.csv>  # solo esos archivos (strings_mapa.csv, strings_glosario.csv, facts*.csv)
    python tools/loc/apply_translations.py --self-test     # pruebas propias, en memoria
Opciones: --loc-dir <carpeta de las tablas> (por defecto Assets/_Project/Data/Loc; la raíz del repositorio es la que la
contiene, para las comprobaciones de los parches).

Entregas que piden un parche (docs/dev/traspaso/spark-enganches/ y spark-traducciones/), comprobado antes de escribir:
- *glosario*.csv: los nombres nuevos de glosario.diff (glossary.csv con el en de booster.undo);
- strings_mapa*.csv: MapTextTests.diff (la prueba ya no exige en y pt-BR vacíos en las claves del Mapa);
- facts_mapa*.csv y cualquier fila map.*: stops-preserve.diff (tools/river-map/stops.py conserva en, pt-BR y sus estados).

Retención (T2-Q6; 08 §4.7: «una traducción solo vale con status en aprobado_es»; FactCatalogTests exige los estados vacíos
mientras el español no esté aprobado): en facts.csv, una fila con status por debajo de aprobado_es no se escribe; sale como
«RETENIDA» (aviso, no conflicto) y se aplicará cuando el asesor apruebe el español. Si entonces el español cambió, la
comprobación del es-419 la convierte en conflicto.
Excepción, el bloque map.* (prueba en el teléfono del 2026-10-23, observación 1: los nombres de parada, las preguntas de
los mundos y los datos del Mapa salían vacíos en en y pt-BR porque estaban retenidos): sus filas se escriben aunque el
español siga en pendiente_asesor. Siguen sin llegar al jugador en release, porque Loc solo muestra un texto largo con
aprobado_es y revisado_nativo (08 §4.7); en desarrollo, como el español sin aprobar, sí se ven. stops.py las conserva solo
mientras el es-419 no cambie (T2-Q1). El resto de facts.csv (frutas y fauna) sigue retenido.

Qué hace y qué no:
- Cambia solo las celdas en, pt-BR, status_en y status_pt-BR de las filas citadas. Las demás filas, las demás columnas, el
  orden, las comillas y los saltos de línea quedan byte a byte como estaban (cada campo que no se toca conserva su texto
  crudo, comillas incluidas).
- Nunca pisa una celda ya rellena: si la tabla ya tiene un texto o un estado distinto del entregado, es un conflicto; si es
  el mismo, no hace nada (aplicar dos veces da lo mismo).
- Nunca escribe en las filas map.* de facts.csv mientras tools/river-map/stops.py las regenere con en y pt-BR vacíos (08
  §4.5): sin stops-preserve.diff, una traducción ahí se perdería en la siguiente regeneración.
- Comprueba cada traducción como LocTablesTests: los mismos marcadores con nombre que es-419 (también {sep}), las llaves
  parejas y la longitud visible ≤ max_chars × 1,3 (la regla de VisibleLength); y, si el es-419 lleva el espacio duro U+00A0
  entre una cifra y su unidad (08 §4.7), que la traducción también lo lleve (salvo antes de «%», que en en y pt-BR va pegado).
- Una clave que ya no existe en la tabla es un aviso (se salta), no un conflicto.
- Lo aplicado a strings.csv se aplica igual a sus copias (MIRRORS: docs/dev/traspaso/spark-strings-reto.csv, cuyas filas
  DailyUiLogicTests exige idénticas a las de la tabla), solo en las claves que tienen.
- Si hay algún conflicto o error, no escribe nada en ninguna tabla.
- Una traducción «traducido» de una entrega de T2 sobre una celda que ya está en revisado_nativo (la revisión de D-054) queda
  superada: no se aplica ni es conflicto, aunque el español haya cambiado después (la revisión ya tradujo el español nuevo).

Modo de revisión (--review [carpeta]; D-054, encargo docs/dev/traspaso/gemini-revision-textos.md §4). La entrega está en
docs/dev/traspaso/gemini-revision/ (por defecto) y tiene tres archivos:
- revision_es.csv (key,tabla,es_actual,veredicto,es_propuesto,motivo,fuente_url): el veredicto sobre el español de cada fila en
  pendiente_asesor. «ok» la pasa a aprobado_es; «cambia» cambia el es-419 por es_propuesto y la pasa a aprobado_es (y, como
  pide 08 §4.7, vacía en, pt-BR y sus estados, que rellena la traducción revisada del mismo paso); «duda» la deja en
  pendiente_asesor. es_actual tiene que ser el texto de la tabla (si no, el español cambió desde la revisión: conflicto) y la
  fila tiene que estar en pendiente_asesor (o ya aplicada: aprobado_es con el texto del veredicto). es_propuesto se comprueba
  como una traducción contra es_actual (marcadores, plurales, U+00A0), con la longitud del español: ≤ max_chars;
- strings_revision.csv y facts_revision.csv (la cabecera de las entregas de T2, con revisado_nativo): la traducción revisada.
  Es el único modo que puede sustituir una celda en «traducido» por la revisada; una celda ya en revisado_nativo con otro
  texto es conflicto. Su es-419 tiene que ser el de la tabla tras aplicar los veredictos. La retención de facts.csv (T2-Q6) y
  el requisito del bloque map.* valen igual que en el modo normal.
Se aplica primero revision_es.csv y luego las traducciones, en la tabla y en sus copias (MIRRORS); con --check, nada se escribe.
Sin archivos, --check comprueba las entregas de T2 por defecto y, si existe la carpeta, también la revisión.

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
DEFAULT_FILES = ("strings.csv", "strings_p14.csv", "strings_t2b.csv", "facts_mapa.csv", "facts_mapa_2.csv")
REVIEW_DIR = REPO / "docs" / "dev" / "traspaso" / "gemini-revision"
REVIEW_VERDICTS = "revision_es.csv"
REVIEW_FILES = ("strings_revision.csv", "facts_revision.csv")
VERDICT_HEADER = ["key", "tabla", "es_actual", "veredicto", "es_propuesto", "motivo", "fuente_url"]
VERDICTS = ("ok", "cambia", "duda")
REVIEWED = "revisado_nativo"
STATUS_ORDER = {"": 0, "borrador": 1, "pendiente_asesor": 2, "aprobado_es": 3}
STOPS_MARKER = "PRESERVED_LOCALE_COLUMNS"  # lo añade stops-preserve.diff a tools/river-map/stops.py
MAP_TEST_OLD = 'Is.Empty, key + " (" + locale + "): pendiente del traductor"'  # lo quita MapTextTests.diff
GLOSSARY_MARKER = "booster.undo"  # glosario.diff le da en
# Copias de filas de strings.csv que las pruebas exigen idénticas a la tabla (DailyUiLogicTests, E5: «la misma fila, tal
# cual»): las mismas celdas se aplican también ahí, con las mismas reglas, solo en las claves que tienen.
MIRRORS = {"strings.csv": ("docs/dev/traspaso/spark-strings-reto.csv",)}

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


# Espacio duro (U+00A0) entre una cifra (o un marcador, «{n}») y su unidad (08 §4.7; .claude/rules/localizacion.md): «6\u00a0km»,
# «{n}\u00a0m³». No cuenta el de antes de «%»: en en y pt-BR el porcentaje va pegado («42%»).
NBSP_UNIT = re.compile("[0-9}]\u00a0(?!%)")


def markers(s: str) -> list[str]:
    return sorted(MARKER.findall(s))


def text_problems(source: str, text: str, max_chars: int) -> list[str]:
    out = []
    if markers(text) != markers(source):
        out.append(f"marcadores {markers(text)} y no {markers(source)} como es-419")
    if text.count("{") != text.count("}"):
        out.append("llaves desparejas")
    need = len(NBSP_UNIT.findall(source))
    have = len(NBSP_UNIT.findall(text))
    if have < need:
        out.append(f"es-419 lleva {need} espacio(s) duro(s) U+00A0 entre cifra y unidad y la traducción solo {have} (08 §4.7)")
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
    retained: list[str] = field(default_factory=list)
    superseded: list[str] = field(default_factory=list)
    verdicts: dict[str, int] = field(default_factory=dict)  # veredictos aplicados ahora: {"ok": n, "cambia": n}


def read_translations(text: str, name: str, review: bool = False) -> tuple[list[dict], list[str]]:
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
            elif review and d[loc] and st != REVIEWED:
                errors.append(f"{name} {d['key']} ({loc}): estado {st!r}; la revisión solo entrega {REVIEWED}")
            if not d[loc] and st:
                errors.append(f"{name} {d['key']} ({loc}): estado {st!r} sin texto")
        out.append(d)
    if keys != sorted(keys):
        errors.append(f"{name}: las filas no van en orden ordinal por clave")
    if len(set(keys)) != len(keys):
        errors.append(f"{name}: claves repetidas")
    return out, errors


def apply(table_text: str, table_name: str, translations: list[dict], tr_name: str, map_ok: bool = False,
          review: bool = False) -> Result:
    """Aplica translations. Con review (modo de revisión, D-054), una celda en «traducido» se sustituye por la revisada; sin
    review, una entrega «traducido» sobre celdas ya en revisado_nativo queda superada (ni se aplica ni es conflicto)."""
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
        if key.startswith(GENERATED_PREFIX) and table_name.startswith("facts") and not map_ok:
            res.conflicts.append(f"{tr_name} {key}: fila del bloque map.*, que tools/river-map/stops.py regenera con en y pt-BR "
                                 "vacíos (falta stops-preserve.diff)")
            continue
        delivered = [loc for loc in LOCALES if tr[loc]]
        if (not review and delivered and all(values["status_" + loc] == REVIEWED and tr["status_" + loc] != REVIEWED
                                             for loc in delivered)):
            res.superseded.append(f"{tr_name} {key}: ya está en {REVIEWED} (revisión de D-054)")
            continue
        if tr["es-419"] != values["es-419"]:
            res.conflicts.append(f"{tr_name} {key}: el es-419 de la tabla cambió desde la traducción "
                                 f"(tabla «{values['es-419']}», traducción «{tr['es-419']}»)")
            continue
        if (table_name.startswith("facts") and not key.startswith(GENERATED_PREFIX)
                and STATUS_ORDER.get(values["status"], 0) < STATUS_ORDER["aprobado_es"]):
            if any(tr[loc] for loc in LOCALES):
                res.retained.append(f"{tr_name} {key}: español en «{values['status'] or 'sin estado'}»; se aplicará con aprobado_es")
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
                if review and have_status != REVIEWED:
                    pass  # el modo de revisión sustituye una traducción sin revisar (traducido) por la revisada
                else:
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


# ------------------------------------------------------------------------------------------------ modo de revisión (D-054)

def read_verdicts(text: str, name: str) -> tuple[list[dict], list[str]]:
    """revision_es.csv: un veredicto por fila (ok, cambia o duda), en orden ordinal por clave y sin claves repetidas."""
    errors: list[str] = []
    rows = list(csv.reader(io.StringIO(text, newline="")))
    if not rows or rows[0] != VERDICT_HEADER:
        return [], [f"{name}: la cabecera tiene que ser {','.join(VERDICT_HEADER)}"]
    out = []
    for line, r in enumerate(rows[1:], start=2):
        if len(r) != len(VERDICT_HEADER):
            errors.append(f"{name}, fila {line}: {len(r)} campos y no {len(VERDICT_HEADER)}")
            continue
        d = dict(zip(VERDICT_HEADER, r))
        what = f"{name} {d['key']}"
        if d["tabla"] not in ("strings", "facts"):
            errors.append(f"{what}: tabla {d['tabla']!r} (va strings o facts)")
        if d["veredicto"] not in VERDICTS:
            errors.append(f"{what}: veredicto {d['veredicto']!r} (va {', '.join(VERDICTS)})")
        elif d["veredicto"] == "cambia" and (not d["es_propuesto"] or d["es_propuesto"] == d["es_actual"]):
            errors.append(f"{what}: «cambia» sin un es_propuesto distinto de es_actual")
        elif d["veredicto"] != "cambia" and d["es_propuesto"]:
            errors.append(f"{what}: «{d['veredicto']}» con es_propuesto (solo lo lleva «cambia»)")
        if not d["es_actual"]:
            errors.append(f"{what}: sin es_actual")
        out.append(d)
    keys = [(d["tabla"], d["key"]) for d in out]
    if [d["key"] for d in out] != sorted(d["key"] for d in out):
        errors.append(f"{name}: las filas no van en orden ordinal por clave")
    if len(set(keys)) != len(keys):
        errors.append(f"{name}: claves repetidas")
    return out, errors


def apply_verdicts(table_text: str, table_name: str, verdicts: list[dict], v_name: str) -> Result:
    """Aplica los veredictos de la revisión del español (revision_es.csv) a una tabla: ok y cambia pasan a aprobado_es."""
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
    for v in verdicts:
        key, verdict = v["key"], v["veredicto"]
        if key not in index:
            res.warnings.append(f"{v_name} {key}: la clave ya no existe en {table_name}; se salta")
            continue
        fields = records[index[key]]
        values = {c: unquote(fields[col[c]]) for c in header}
        es, status = values["es-419"], values["status"]
        target = v["es_propuesto"] if verdict == "cambia" else v["es_actual"]
        if verdict == "duda":
            if es != v["es_actual"]:
                res.conflicts.append(f"{v_name} {key}: el es-419 de la tabla cambió desde la revisión "
                                     f"(tabla «{es}», revisión «{v['es_actual']}»)")
            elif status != "pendiente_asesor":
                res.conflicts.append(f"{v_name} {key}: «duda» sobre una fila en «{status or 'sin estado'}» y no en pendiente_asesor")
            else:
                res.retained.append(f"{v_name} {key}: «duda»; sigue en pendiente_asesor (D-054)")
            continue
        if status == "aprobado_es" and es == target:
            continue  # ya aplicado
        if es != v["es_actual"]:
            res.conflicts.append(f"{v_name} {key}: el es-419 de la tabla cambió desde la revisión "
                                 f"(tabla «{es}», revisión «{v['es_actual']}»)")
            continue
        if status != "pendiente_asesor":
            res.conflicts.append(f"{v_name} {key}: veredicto «{verdict}» sobre una fila en «{status or 'sin estado'}» "
                                 "(la revisión solo cubre pendiente_asesor)")
            continue
        if verdict == "cambia":
            try:
                max_chars = int(values["max_chars"] or "0")
            except ValueError:
                max_chars = 0
            # Como una traducción del es-419 viejo (marcadores, llaves, U+00A0), pero con el tope del español: max_chars.
            probs = [p for p in text_problems(es, target, 0)]
            if max_chars > 0 and visible_length(target) > max_chars:
                probs.append(f"mide {visible_length(target)} y pasa de max_chars ({max_chars})")
            if probs:
                res.conflicts.append(f"{v_name} {key} (es-419): " + "; ".join(probs))
                continue
            fields[col["es-419"]] = quote(target)
            # 08 §4.7: si el español cambia, los demás idiomas vuelven a vacío (la traducción revisada los rellena después).
            for loc in LOCALES:
                fields[col[loc]] = ""
                fields[col["status_" + loc]] = ""
        fields[col["status"]] = "aprobado_es"
        res.changes.append(f"{key} (es-419: {verdict})")
        res.verdicts[verdict] = res.verdicts.get(verdict, 0) + 1
        changed = True
    if changed and not res.conflicts:
        res.text = "".join(",".join(f) + "\n" for f in records)
    return res


def review_table(text: str, table_name: str, verdicts: list[dict], v_name: str, translations: list[dict], tr_name: str,
                 map_ok: bool) -> tuple[str, Result]:
    """Los veredictos y luego las traducciones revisadas sobre una tabla (o su copia). Devuelve el texto nuevo y el resultado
    combinado (changes cuenta celdas: un veredicto aplicado cuenta como una)."""
    total = Result()
    rv = apply_verdicts(text, table_name, verdicts, v_name)
    total.changes += rv.changes
    total.conflicts += rv.conflicts
    total.warnings += rv.warnings
    total.retained += rv.retained
    total.verdicts = dict(rv.verdicts)
    if rv.conflicts:
        return text, total
    if rv.text is not None:
        text = rv.text
    rt = apply(text, table_name, translations, tr_name, map_ok, review=True)
    total.changes += rt.changes
    total.conflicts += rt.conflicts
    total.warnings += rt.warnings
    total.retained += rt.retained
    if rt.text is not None and not rt.conflicts:
        text = rt.text
    return text, total


def run_review(review_dir: Path, loc_dir: Path, check: bool, out=sys.stdout) -> int:
    """El modo de revisión (D-054): revision_es.csv y las traducciones revisadas de review_dir sobre las tablas y sus copias."""
    repo = repo_of(loc_dir)
    stops = repo / "tools" / "river-map" / "stops.py"
    map_ok = stops.exists() and STOPS_MARKER in stops.read_text(encoding="utf-8")
    errors: list[str] = []
    try:
        # Una revisión parcial (la 2, solo strings) puede venir sin revision_es.csv o sin facts_revision.csv.
        verdicts: list[dict] = []
        if (review_dir / REVIEW_VERDICTS).exists():
            verdicts, errs = read_verdicts(decode((review_dir / REVIEW_VERDICTS).read_bytes(), REVIEW_VERDICTS), REVIEW_VERDICTS)
            errors.extend(errs)
        present = [name for name in REVIEW_FILES if (review_dir / name).exists()]
        if not present:
            print(f"ERROR {review_dir}: no hay ninguna de {', '.join(REVIEW_FILES)}", file=out)
            return 2
        deliveries: dict[str, tuple[list[dict], str]] = {}
        for name in present:
            rows, errs = read_translations(decode((review_dir / name).read_bytes(), name), name, review=True)
            errors.extend(errs)
            errors.extend(requirement_errors(name, repo))
            deliveries[target_of(Path(name)) or name] = (rows, name)
        tables = {t: decode((loc_dir / t).read_bytes(), t) for t in deliveries}
    except OSError as e:
        print(f"ERROR no se puede leer: {e}", file=out)
        return 2
    except CsvError as e:
        print(f"ERROR {e}", file=out)
        return 2
    by_table = {t: [v for v in verdicts if v["tabla"] + ".csv" == t] for t in deliveries}
    conflicts, warnings, retained = list(errors), [], []
    counts: dict[str, int] = {}
    total_changes = 0
    new_tables: dict[str, str] = {}
    mirrors: dict[Path, str] = {}
    for target, (rows, name) in deliveries.items():
        try:
            text, res = review_table(tables[target], target, by_table[target], REVIEW_VERDICTS, rows, name, map_ok)
        except CsvError as e:
            conflicts.append(str(e))
            continue
        conflicts += res.conflicts
        warnings += res.warnings
        retained += res.retained
        total_changes += len(res.changes)
        for k, n in res.verdicts.items():
            counts[k] = counts.get(k, 0) + n
        if text != tables[target]:
            new_tables[target] = text
        for rel in MIRRORS.get(target, ()):
            path = repo / rel
            if not path.exists():
                continue
            try:
                original = decode(path.read_bytes(), path.name)
                keys = {unquote(f[0]) for f in split_records(original, path.name)[1:]}
                mtext, mres = review_table(original, path.name, [v for v in by_table[target] if v["key"] in keys],
                                           REVIEW_VERDICTS, [r for r in rows if r["key"] in keys], name, map_ok)
            except CsvError as e:
                conflicts.append(str(e))
                continue
            conflicts += [f"{c} (copia {rel})" for c in mres.conflicts]
            total_changes += len(mres.changes)
            if mtext != original:
                mirrors[path] = mtext
    for w in warnings:
        print(f"AVISO {w}", file=out)
    if retained:
        print(f"RETENIDAS {len(retained)} filas (dudas o español sin aprobar); la primera: {retained[0]}", file=out)
    for c in conflicts:
        print(f"CONFLICTO {c}", file=out)
    if conflicts:
        print(f"FAIL revisión: {len(conflicts)} conflictos o errores: no se escribe nada", file=out)
        return 1
    where = sorted(new_tables) + sorted(p.name for p in mirrors)
    if check:
        if total_changes:
            print(f"FAIL --check (revisión): aplicar cambiaría {total_changes} celdas o estados en {', '.join(where)}", file=out)
            return 1
        print("OK --check (revisión): la revisión de D-054 ya está en las tablas (nada que aplicar)", file=out)
        return 0
    for target, text in new_tables.items():
        (loc_dir / target).write_bytes(text.encode("utf-8"))
    for path, text in mirrors.items():
        path.write_bytes(text.encode("utf-8"))
    summary = ", ".join(f"{n} {k}" for k, n in sorted(counts.items())) or "ningún veredicto nuevo"
    print(f"OK revisión: {total_changes} celdas o estados aplicados ({summary})"
          + (f" en {', '.join(where)}" if where else " (nada que aplicar)"), file=out)
    return 0


def target_of(path: Path) -> str | None:
    name = path.name
    if name.startswith("strings"):
        return "strings.csv"
    if name.startswith("facts"):
        return "facts.csv"
    return None


def requirement_errors(name: str, repo: Path) -> list[str]:
    """Lo que pide una entrega antes de aplicarse (el parche que la acompaña)."""
    errs = []

    def text(rel: str) -> str:
        try:
            return (repo / rel).read_text(encoding="utf-8")
        except OSError:
            return ""

    if "glosario" in name:
        rows = list(csv.reader(io.StringIO(text("Assets/_Project/Data/Loc/glossary.csv"), newline="")))
        if not any(r and r[0] == GLOSSARY_MARKER and len(r) > 3 and r[3] for r in rows):
            errs.append(f"{name}: necesita glosario.diff aplicado (glossary.csv sin el en de {GLOSSARY_MARKER})")
    if name.startswith("strings_mapa") and MAP_TEST_OLD in text("Assets/_Project/Tests/EditMode/MapTextTests.cs"):
        errs.append(f"{name}: necesita MapTextTests.diff aplicado (la prueba aún exige en y pt-BR vacíos)")
    if name.startswith("facts_mapa") and STOPS_MARKER not in text("tools/river-map/stops.py"):
        errs.append(f"{name}: necesita stops-preserve.diff aplicado (stops.py no conserva en y pt-BR del bloque map.*)")
    return errs


def repo_of(loc_dir: Path) -> Path:
    """La raíz del repositorio: la carpeta que contiene Assets/_Project/Data/Loc."""
    p = loc_dir.resolve()
    return p.parents[3] if len(p.parents) > 3 else p


def run(files: list[Path], loc_dir: Path, check: bool, out=sys.stdout) -> int:
    repo = repo_of(loc_dir)
    stops = repo / "tools" / "river-map" / "stops.py"
    map_ok = stops.exists() and STOPS_MARKER in stops.read_text(encoding="utf-8")
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
        errors.extend(requirement_errors(f.name, repo))
        pending.setdefault(target, []).append((rows, f.name))
    new_tables: dict[str, str] = {}
    total_changes, conflicts, warnings, retained, superseded = 0, list(errors), [], [], []
    for target, batches in pending.items():
        text = tables[target]
        for rows, name in batches:
            try:
                res = apply(text, target, rows, name, map_ok)
            except CsvError as e:
                conflicts.append(str(e))
                continue
            conflicts.extend(res.conflicts)
            warnings.extend(res.warnings)
            retained.extend(res.retained)
            superseded.extend(res.superseded)
            total_changes += len(res.changes)
            if res.text is not None:
                text = res.text
        if text != tables[target]:
            new_tables[target] = text
    mirrors: dict[Path, str] = {}
    for target, batches in pending.items():
        for rel in MIRRORS.get(target, ()):
            path = repo / rel
            if not path.exists():
                continue
            try:
                original = decode(path.read_bytes(), path.name)
            except CsvError as e:
                conflicts.append(str(e))
                continue
            mtext = original
            for rows, name in batches:
                try:
                    keys = {unquote(f[0]) for f in split_records(mtext, path.name)[1:]}
                    res = apply(mtext, path.name, [r for r in rows if r["key"] in keys], name, map_ok)
                except CsvError as e:
                    conflicts.append(str(e))
                    continue
                conflicts.extend(f"{c} (copia {rel})" for c in res.conflicts)
                total_changes += len(res.changes)
                if res.text is not None:
                    mtext = res.text
            if mtext != original:
                mirrors[path] = mtext
    for w in warnings:
        print(f"AVISO {w}", file=out)
    if retained:
        print(f"RETENIDAS {len(retained)} filas de facts.csv con el español sin aprobar (T2-Q6; 08 §4.7); la primera: "
              f"{retained[0]}", file=out)
    if superseded:
        print(f"SUPERADAS {len(superseded)} filas de las entregas de T2 que la revisión de D-054 ya dejó en {REVIEWED}",
              file=out)
    for c in conflicts:
        print(f"CONFLICTO {c}", file=out)
    if conflicts:
        print(f"FAIL {len(conflicts)} conflictos o errores: no se escribe nada", file=out)
        return 1
    if check:
        if total_changes:
            where = sorted(new_tables) + sorted(p.name for p in mirrors)
            print(f"FAIL --check: aplicar cambiaría {total_changes} celdas de texto en {', '.join(where)}", file=out)
            return 1
        print("OK --check: las traducciones ya están en las tablas (nada que aplicar)", file=out)
        return 0
    for target, text in new_tables.items():
        (loc_dir / target).write_bytes(text.encode("utf-8"))
    for path, text in mirrors.items():
        path.write_bytes(text.encode("utf-8"))
    where = sorted(new_tables) + sorted(p.name for p in mirrors)
    print(f"OK {total_changes} celdas aplicadas" + (f" en {', '.join(where)}" if where else " (nada que aplicar)"), file=out)
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
    # 11. Retención de facts.csv (T2-Q6): por debajo de aprobado_es no se escribe; con aprobado_es, sí.
    facts2 = (H + "fact.x.01,Mide 35 m,,,ctx,90,pendiente_asesor,,,,\n"
              + "fact.x.02,Pesa 4 kg,,,ctx,90,aprobado_es,,,,\n"
              + "map.stop.x.dato_01,Dato,,,ctx,90,aprobado_es,,,generado,\n")
    rows11, _ = read_translations(tr([["fact.x.01", "Mide 35 m", "It's 35 m", "Mede 35 m", "traducido", "traducido"],
                                      ["fact.x.02", "Pesa 4 kg", "It weighs 4 kg", "", "traducido", ""]]), "facts.csv")
    r11 = apply(facts2, "facts.csv", rows11, "facts.csv")
    case("facts en pendiente_asesor: retenida, sin escribir", len(r11.retained) == 1 and "fact.x.01" in r11.retained[0]
         and "fact.x.01,Mide 35 m,,,ctx" in (r11.text or ""))
    case("facts en aprobado_es: se escribe", r11.changes == ["fact.x.02 (en)"] and not r11.conflicts)
    r11b = apply(H + "fact.x.01,Mide 35 m,,,ctx,90,pendiente_asesor,,,,\n", "facts.csv", rows11[:1], "facts.csv")
    case("solo retenidas: nada que escribir ni conflicto", r11b.text is None and not r11b.conflicts and r11b.retained)
    case("strings no se retiene (borrador se escribe)", apply(table, "strings.csv", rows, "t.csv").changes)
    # 12. map.*: solo con stops-preserve.diff.
    rows12, _ = read_translations(tr([["map.stop.x.dato_01", "Dato", "Fact", "", "traducido", ""]]), "facts_mapa.csv")
    case("map.* sin el parche: conflicto", apply(facts2, "facts.csv", rows12, "facts_mapa.csv").conflicts)
    case("map.* con el parche: se escribe", apply(facts2, "facts.csv", rows12, "facts_mapa.csv", True).changes == ["map.stop.x.dato_01 (en)"])
    # 12c. El espacio duro entre cifra y unidad (08 §4.7): si el es-419 lo lleva, la traducción también; «42%» pegado vale.
    case("U+00A0: falta en la traducción", any("U+00A0" in p for p in text_problems("Mide {n}\u00a0km.", "It is {n} km long.", 90)))
    case("U+00A0: con él, sin problema", not text_problems("Mide {n}\u00a0km.", "It is {n}\u00a0km long.", 90))
    case("U+00A0: el de antes de % no cuenta", not text_problems("Ocupa el 42\u00a0%.", "It covers 42%.", 90))
    case("U+00A0: sin él en es-419, no se pide", not text_problems("Mide 5 metros.", "It is 5 meters.", 90))
    # 12b. El bloque map.* no se retiene con el español en pendiente_asesor (observación 1 de la prueba del 2026-10-23); el
    # resto de facts.csv, sí.
    facts12 = (H + "fact.x.01,Mide 35 m,,,ctx,90,pendiente_asesor,,,,\n"
               + "map.stop.x.dato_01,Dato,,,ctx,90,pendiente_asesor,,,generado,\n")
    r12b = apply(facts12, "facts.csv", rows11[:1] + rows12, "facts_mapa.csv", True)
    case("map.* en pendiente_asesor: se escribe; fact.* sigue retenida", r12b.changes == ["map.stop.x.dato_01 (en)"]
         and len(r12b.retained) == 1 and "fact.x.01" in r12b.retained[0]
         and "map.stop.x.dato_01,Dato,Fact,,ctx,90,pendiente_asesor,traducido,,generado," in (r12b.text or ""))
    # 13. Entregas que piden un parche.
    with tempfile.TemporaryDirectory() as d:
        repo = Path(d)
        (repo / "Assets/_Project/Data/Loc").mkdir(parents=True)
        (repo / "Assets/_Project/Tests/EditMode").mkdir(parents=True)
        (repo / "tools/river-map").mkdir(parents=True)
        glossary = repo / "Assets/_Project/Data/Loc/glossary.csv"
        mtest = repo / "Assets/_Project/Tests/EditMode/MapTextTests.cs"
        stops = repo / "tools/river-map/stops.py"
        glossary.write_text("term,kind,es-419,en,pt-BR,notes\nbooster.undo,booster,Marea,,,n\n", encoding="utf-8")
        mtest.write_text("Assert.That(strings.TextAt(r, locale), " + MAP_TEST_OLD + ");\n", encoding="utf-8")
        stops.write_text("# stops\n", encoding="utf-8")
        case("glosario sin el parche: error", requirement_errors("strings_glosario.csv", repo))
        case("strings_mapa sin el parche: error", requirement_errors("strings_mapa.csv", repo))
        case("facts_mapa sin el parche: error", requirement_errors("facts_mapa.csv", repo))
        case("strings.csv no pide nada", not requirement_errors("strings.csv", repo))
        glossary.write_text("term,kind,es-419,en,pt-BR,notes\nbooster.undo,booster,Marea,Tide,Maré,n\n", encoding="utf-8")
        mtest.write_text("// parcheado\n", encoding="utf-8")
        stops.write_text(STOPS_MARKER + " = ()\n", encoding="utf-8")
        case("con los tres parches: sin errores", not requirement_errors("strings_glosario.csv", repo)
             and not requirement_errors("strings_mapa.csv", repo) and not requirement_errors("facts_mapa.csv", repo))
        case("la raíz sale de --loc-dir", repo_of(repo / "Assets/_Project/Data/Loc") == repo.resolve())
    # 14. La copia de strings.csv (MIRRORS) recibe las mismas celdas, solo en sus claves.
    with tempfile.TemporaryDirectory() as d:
        repo = Path(d)
        loc = repo / "Assets/_Project/Data/Loc"
        loc.mkdir(parents=True)
        (repo / "docs/dev/traspaso").mkdir(parents=True)
        (loc / "strings.csv").write_bytes(table.encode())
        (loc / "facts.csv").write_bytes(facts.encode())
        mirror = repo / MIRRORS["strings.csv"][0]
        mirror_text = H + 'a_key,"Hola, mundo",,,"ctx, con coma",20,borrador,,,"nota ""citada""",\n'
        mirror.write_bytes(mirror_text.encode())
        tfile = repo / "strings_t.csv"
        tfile.write_bytes(tr([["a_key", "Hola, mundo", "Hello, world", "", "traducido", ""],
                              ["c_key", "Dos\nlíneas", "Two\nlines", "", "traducido", ""]]).encode())
        sink = io.StringIO()
        case("con copia: --check antes cuenta la copia", run([tfile], loc, True, sink) == 1 and "spark-strings-reto.csv" in sink.getvalue())
        case("con copia: aplica", run([tfile], loc, False, io.StringIO()) == 0)
        table_row = next(ln for ln in (loc / "strings.csv").read_text(encoding="utf-8").split("\n") if ln.startswith("a_key,"))
        case("la fila de la copia es idéntica a la de la tabla", mirror.read_text(encoding="utf-8").split("\n")[1] == table_row)
        case("con copia: --check después, 0", run([tfile], loc, True, io.StringIO()) == 0)
    # 15. Modo de revisión (D-054): veredictos del español y traducciones revisadas.
    V = ",".join(VERDICT_HEADER) + "\n"

    def vr(rows):
        return V + "".join(",".join(quote(x) for x in r) + "\n" for r in rows)

    rtable = (H
              + "a_key,Sin afán,Easy,Sem pressa,ctx,20,pendiente_asesor,traducido,traducido,n,\n"
              + "b_key,Nivel {n},Level {n},Nível {n},ctx,12,pendiente_asesor,traducido,traducido,,\n"
              + "c_key,Hola,Hi,Oi,ctx,10,pendiente_asesor,traducido,traducido,,\n"
              + "d_key,Borrador,Draft,Rascunho,ctx,12,borrador,traducido,traducido,,\n")
    verd, ev = read_verdicts(vr([["a_key", "strings", "Sin afán", "cambia", "Sin prisa", "regionalismo", ""],
                                 ["b_key", "strings", "Nivel {n}", "ok", "", "", ""],
                                 ["c_key", "strings", "Hola", "duda", "", "", ""]]), "revision_es.csv")
    case("revisión: lee los veredictos sin errores", not ev and len(verd) == 3)
    rv = apply_verdicts(rtable, "strings.csv", verd, "revision_es.csv")
    pv = {p["key"]: p for p in csv.DictReader(io.StringIO(rv.text or "", newline=""))}
    case("revisión: ok pasa a aprobado_es y conserva las traducciones", pv["b_key"]["status"] == "aprobado_es"
         and pv["b_key"]["en"] == "Level {n}" and pv["b_key"]["status_en"] == "traducido")
    case("revisión: cambia cambia el español, pasa a aprobado_es y vacía en y pt-BR (08 §4.7)",
         pv["a_key"]["es-419"] == "Sin prisa" and pv["a_key"]["status"] == "aprobado_es"
         and pv["a_key"]["en"] == "" and pv["a_key"]["status_pt-BR"] == "")
    case("revisión: duda sigue en pendiente_asesor", pv["c_key"]["status"] == "pendiente_asesor" and len(rv.retained) == 1)
    case("revisión: cuenta los veredictos", rv.verdicts == {"cambia": 1, "ok": 1})
    case("revisión: veredictos dos veces no cambian nada", (lambda r: r.text is None and not r.changes and not r.conflicts)(
        apply_verdicts(rv.text or "", "strings.csv", verd, "revision_es.csv")))
    vbad, _ = read_verdicts(vr([["a_key", "strings", "Sin afan", "ok", "", "", ""]]), "revision_es.csv")
    case("revisión: es_actual distinto de la tabla: conflicto", apply_verdicts(rtable, "strings.csv", vbad, "r").conflicts)
    vdraft, _ = read_verdicts(vr([["d_key", "strings", "Borrador", "ok", "", "", ""]]), "revision_es.csv")
    case("revisión: veredicto sobre una fila en borrador: conflicto", apply_verdicts(rtable, "strings.csv", vdraft, "r").conflicts)
    vmark, _ = read_verdicts(vr([["b_key", "strings", "Nivel {n}", "cambia", "Nivel {k}", "", ""]]), "revision_es.csv")
    case("revisión: cambia con otros marcadores: conflicto", apply_verdicts(rtable, "strings.csv", vmark, "r").conflicts)
    vlong, _ = read_verdicts(vr([["c_key", "strings", "Hola", "cambia", "Hola, ¿qué tal?", "", ""]]), "revision_es.csv")
    case("revisión: cambia más largo que max_chars (sin el 1,3): conflicto", apply_verdicts(rtable, "strings.csv", vlong, "r").conflicts)
    _, everr = read_verdicts(vr([["b", "strings", "x", "cambia", "", "", ""], ["a", "strings", "x", "ok", "y", "", ""],
                                 ["c", "otra", "x", "quizá", "", "", ""]]), "revision_es.csv")
    case("revisión: errores del archivo de veredictos (cambia vacío, ok con texto, tabla, veredicto, orden)", len(everr) == 5)
    rtr, etr = read_translations(tr([["a_key", "Sin prisa", "No rush", "Sem pressa", REVIEWED, REVIEWED],
                                     ["b_key", "Nivel {n}", "Level {n}", "Nível {n}", REVIEWED, REVIEWED],
                                     ["c_key", "Hola", "Hello", "Olá", REVIEWED, REVIEWED]]), "strings_revision.csv", review=True)
    case("revisión: lee las traducciones revisadas", not etr)
    _, etr2 = read_translations(tr([["a_key", "x", "y", "", "traducido", ""]]), "strings_revision.csv", review=True)
    case("revisión: una entrega de revisión en «traducido»: error", etr2)
    text15, r15 = review_table(rtable, "strings.csv", verd, "revision_es.csv", rtr, "strings_revision.csv", False)
    p15 = {p["key"]: p for p in csv.DictReader(io.StringIO(text15, newline=""))}
    case("revisión: sustituye «traducido» por la revisada", p15["c_key"]["en"] == "Hello" and p15["c_key"]["status_en"] == REVIEWED
         and p15["b_key"]["status_pt-BR"] == REVIEWED and not r15.conflicts)
    case("revisión: rellena la fila cuyo español cambió", p15["a_key"]["en"] == "No rush" and p15["a_key"]["status_en"] == REVIEWED)
    text15b, r15b = review_table(text15, "strings.csv", verd, "revision_es.csv", rtr, "strings_revision.csv", False)
    case("revisión: aplicar dos veces no cambia nada", text15b == text15 and not r15b.changes and not r15b.conflicts)
    rtr3, _ = read_translations(tr([["c_key", "Hola", "Hey", "", REVIEWED, ""]]), "strings_revision.csv", review=True)
    case("revisión: no pisa una celda ya revisada con otro texto", apply(text15, "strings.csv", rtr3, "s", review=True).conflicts)
    rtr4, _ = read_translations(tr([["b_key", "Nivel {k}", "Level {n}", "", REVIEWED, ""]]), "strings_revision.csv", review=True)
    case("revisión: es-419 distinto del de la tabla: conflicto", apply(text15, "strings.csv", rtr4, "s", review=True).conflicts)
    rtr5, _ = read_translations(tr([["b_key", "Nivel {n}", "Level {count}", "", REVIEWED, ""]]), "strings_revision.csv", review=True)
    case("revisión: marcadores distintos: conflicto", apply(rtable, "strings.csv", rtr5, "s", review=True).conflicts)
    # Sin el modo de revisión, una entrega «traducido» sobre celdas ya revisadas queda superada, aunque el español cambiara.
    old, _ = read_translations(tr([["a_key", "Sin afán", "Easy", "Sem pressa", "traducido", "traducido"],
                                   ["c_key", "Hola", "Hi", "", "traducido", ""]]), "strings.csv")
    r15c = apply(text15, "strings.csv", old, "strings.csv")
    case("normal: «traducido» sobre revisado_nativo queda superada", len(r15c.superseded) == 2 and not r15c.conflicts and r15c.text is None)
    case("normal: sin revisión, «traducido» sobre «traducido» distinto sigue siendo conflicto",
         apply(rtable, "strings.csv", read_translations(tr([["c_key", "Hola", "Hey", "", "traducido", ""]]), "s")[0],
               "strings.csv").conflicts)
    # facts.csv: la traducción revisada de una fila que no queda aprobada (duda) se retiene.
    rfacts = H + "fact.x.01,Mide 35 m,,,ctx,90,pendiente_asesor,,,,\n" + "fact.x.02,Pesa 4 kg,,,ctx,90,pendiente_asesor,,,,\n"
    fverd, _ = read_verdicts(vr([["fact.x.01", "facts", "Mide 35 m", "duda", "", "", ""],
                                 ["fact.x.02", "facts", "Pesa 4 kg", "ok", "", "", ""]]), "revision_es.csv")
    ftr, _ = read_translations(tr([["fact.x.01", "Mide 35 m", "It's 35 m", "Mede 35 m", REVIEWED, REVIEWED],
                                   ["fact.x.02", "Pesa 4 kg", "It weighs 4 kg", "Pesa 4 kg", REVIEWED, REVIEWED]]), "facts_revision.csv", review=True)
    ftext, fres = review_table(rfacts, "facts.csv", fverd, "revision_es.csv", ftr, "facts_revision.csv", False)
    case("revisión: facts en duda se retiene; la aprobada se escribe",
         "fact.x.01,Mide 35 m,,,ctx,90,pendiente_asesor,,,," in ftext
         and "fact.x.02,Pesa 4 kg,It weighs 4 kg,Pesa 4 kg,ctx,90,aprobado_es,revisado_nativo,revisado_nativo,," in ftext
         and not fres.conflicts)
    # Extremo a extremo: --review --check falla antes, aplica (con la copia), --check pasa; con un conflicto no escribe nada.
    with tempfile.TemporaryDirectory() as d:
        repo = Path(d)
        loc = repo / "Assets/_Project/Data/Loc"
        loc.mkdir(parents=True)
        rdir = repo / "rev"
        rdir.mkdir()
        (repo / "docs/dev/traspaso").mkdir(parents=True)
        (loc / "strings.csv").write_bytes(rtable.encode())
        (loc / "facts.csv").write_bytes(rfacts.encode())
        mirror = repo / MIRRORS["strings.csv"][0]
        mirror.write_bytes((H + "a_key,Sin afán,Easy,Sem pressa,ctx,20,pendiente_asesor,traducido,traducido,n,\n").encode())
        (rdir / REVIEW_VERDICTS).write_bytes(vr([["a_key", "strings", "Sin afán", "cambia", "Sin prisa", "", ""],
                                                 ["b_key", "strings", "Nivel {n}", "ok", "", "", ""],
                                                 ["c_key", "strings", "Hola", "duda", "", "", ""],
                                                 ["fact.x.01", "facts", "Mide 35 m", "duda", "", "", ""],
                                                 ["fact.x.02", "facts", "Pesa 4 kg", "ok", "", "", ""]]).encode())
        (rdir / "strings_revision.csv").write_bytes(tr([["a_key", "Sin prisa", "No rush", "Sem pressa", REVIEWED, REVIEWED],
                                                        ["b_key", "Nivel {n}", "Level {n}", "Nível {n}", REVIEWED, REVIEWED]]).encode())
        (rdir / "facts_revision.csv").write_bytes(tr([["fact.x.02", "Pesa 4 kg", "It weighs 4 kg", "Pesa 4 kg", REVIEWED, REVIEWED]]).encode())
        sink = io.StringIO()
        case("revisión: --check antes de aplicar, 1", run_review(rdir, loc, True, sink) == 1)
        case("revisión: aplica, 0", run_review(rdir, loc, False, sink) == 0)
        case("revisión: --check después, 0", run_review(rdir, loc, True, sink) == 0)
        row = next(ln for ln in (loc / "strings.csv").read_text(encoding="utf-8").split("\n") if ln.startswith("a_key,"))
        case("revisión: la copia queda idéntica a la tabla", mirror.read_text(encoding="utf-8").split("\n")[1] == row
             and row == "a_key,Sin prisa,No rush,Sem pressa,ctx,20,aprobado_es,revisado_nativo,revisado_nativo,n,")
        (loc / "strings.csv").write_bytes(rtable.replace("Hola,Hi", "Adiós,Hi").encode())
        (loc / "facts.csv").write_bytes(rfacts.encode())
        before = (loc / "strings.csv").read_bytes(), (loc / "facts.csv").read_bytes()
        case("revisión: con un conflicto, 1", run_review(rdir, loc, False, io.StringIO()) == 1)
        case("revisión: con un conflicto no escribe ninguna tabla",
             ((loc / "strings.csv").read_bytes(), (loc / "facts.csv").read_bytes()) == before)
        case("revisión: sin la carpeta, 2", run_review(repo / "no", loc, True, io.StringIO()) == 2)
    failed = [n for n, ok in checks if not ok]
    for n, ok in checks:
        print(("PASS " if ok else "FAIL ") + n)
    print(f"{len(checks) - len(failed)}/{len(checks)} PASS")
    return 1 if failed else 0


def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description="Vuelca traducciones (key,es-419,en,pt-BR,status_en,status_pt-BR) sobre strings.csv y facts.csv.")
    ap.add_argument("files", nargs="*", type=Path, help=f"archivos de traducciones (por defecto, {', '.join(DEFAULT_FILES)} de {DEFAULT_DIR.relative_to(REPO)})")
    ap.add_argument("--check", action="store_true", help="no escribe; código 1 si aplicar cambiaría algo o hay conflictos")
    ap.add_argument("--self-test", action="store_true", help="pruebas propias en memoria")
    ap.add_argument("--loc-dir", type=Path, default=DEFAULT_LOC, help="carpeta de strings.csv y facts.csv")
    ap.add_argument("--review", nargs="?", type=Path, const=REVIEW_DIR, default=None, metavar="CARPETA",
                    help=f"modo de revisión (D-054): {REVIEW_VERDICTS} y {', '.join(REVIEW_FILES)} de CARPETA "
                         f"(por defecto {REVIEW_DIR.relative_to(REPO)})")
    args = ap.parse_args(argv)
    if args.self_test:
        return self_test()
    if args.review is not None:
        if args.files:
            print("ERROR --review no admite archivos: lee los de su carpeta")
            return 2
        return run_review(args.review, args.loc_dir, args.check)
    files = args.files or [DEFAULT_DIR / f for f in DEFAULT_FILES]
    code = run(files, args.loc_dir, args.check)
    if args.check and not args.files and REVIEW_DIR.is_dir():
        # Sin archivos, --check también comprueba que la revisión de D-054 está aplicada.
        code = max(code, run_review(REVIEW_DIR, args.loc_dir, True))
    return code


if __name__ == "__main__":
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    sys.exit(main(sys.argv[1:]))
