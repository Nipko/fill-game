# -*- coding: utf-8 -*-
"""riverdata.py - lectura compartida de las fuentes del Mapa del rio (D-013) para tools/river-map.

Solo biblioteca estandar de Python 3.14 (08 §4.5). Lo usan stops.py, geometry.py (M2.6) y sus pruebas.

- Rutas del repositorio: la investigacion `docs/research/mapa-del-rio/rio-amazonas.json` (fuente de las paradas;
  nunca se modifica desde aqui), `Assets/_Project/Scripts/Levels/WorldTable.cs` (tramos de los mundos del cliente) y
  las salidas de `Assets/_Project/Data/Map/`.
- `WorldTable`: lee `FirstLevels` y `LastLevel` de WorldTable.cs, para que la herramienta valide contra el mismo
  reparto de niveles que usa el cliente (glosario §8) sin copiarlo a mano.
- Utilidades de texto: mensajes solo en ASCII y el veto de «Belén» (D-013, punto 11; 07 §8.1).
- Tablas de textos (`Assets/_Project/Data/Loc/facts.csv`, contrato 32; 08 §4.7): partir el archivo en registros RFC 4180 sin
  tocar sus bytes y escribir filas nuevas con UTF-8, LF y comillas solo donde hacen falta (como las deja L2b).
- Puntos de paso y miradores de la investigacion (M2.6): su id (`place_id`), su nombre visible (`place_name`) y la clave de su
  dato (`fact_key_of`), los mismos en river_map_v1.json (geometry.py) y en el bloque map.* de facts.csv (stops.py).
- Paradas de la investigacion en sus dos listas (`stop_records`: paradas_1_200 y, desde M4.5a, paradas_201_700), los
  puntos de paso de los viajes de entrada de 201-1.000 que llevan id (`travel_passing_points`: teotonio) y el texto final
  de un dato de punto de paso cuando la investigacion lo da entre «» (`final_fact_of`).
"""
from __future__ import annotations

import csv
import io
import json
import re
import unicodedata
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent

SOURCE_REL = "docs/research/mapa-del-rio/rio-amazonas.json"
SOURCE_PATH = ROOT / SOURCE_REL
WORLDTABLE_PATH = ROOT / "Assets" / "_Project" / "Scripts" / "Levels" / "WorldTable.cs"
OVERRIDES_PATH = HERE / "overrides.json"
MAP_DATA_DIR = ROOT / "Assets" / "_Project" / "Data" / "Map"
STOPS_PATH = MAP_DATA_DIR / "river_stops_v1.json"
STOPS_SCHEMA_PATH = ROOT / "schemas" / "af-river-stops-1.schema.json"
MAP_PATH = MAP_DATA_DIR / "river_map_v1.json"
MAP_SCHEMA_PATH = ROOT / "schemas" / "af-river-map-1.schema.json"
# La muestra de la investigacion: proyeccion de referencia (test_geometry.py) y candidatos de la propuesta §5.1 bis
# (CANDIDATOS_5_1_BIS, la copia unica de esa lista; stops.py --check).
GENERAR_MAPA_PATH = ROOT / "docs" / "research" / "mapa-del-rio" / "generar_mapa.py"
FACTS_REL = "Assets/_Project/Data/Loc/facts.csv"
FACTS_PATH = ROOT / FACTS_REL

# Nombre visible en es-419 de cada país de la cuenca por su ISO2 (glosario §8, columna «País»; los rótulos de país de
# rio-amazonas.json, `rotulos_de_pais`, que stops.py comprueba). Solo para escribir las filas map.country.<iso2>.name.
COUNTRY_NAMES = {
    "BR": "Brasil",
    "CO": "Colombia",
    "PE": "Perú",
    "EC": "Ecuador",
    "BO": "Bolivia",
    "VE": "Venezuela",
    "GY": "Guyana",
    "SR": "Surinam",
    "GF": "Guayana Francesa",
}


class SourceError(Exception):
    """Un archivo de entrada no se puede leer o no tiene la forma esperada."""


def load_json(path: Path) -> dict:
    """Lee un JSON UTF-8 (sin BOM) y exige un objeto en la raiz."""
    try:
        text = Path(path).read_text(encoding="utf-8")
    except OSError as e:
        raise SourceError(f"no se puede leer {path}: {e.strerror}") from e
    try:
        doc = json.loads(text)
    except json.JSONDecodeError as e:
        raise SourceError(f"{path}: JSON invalido en la linea {e.lineno}, columna {e.colno}: {e.msg}") from e
    if not isinstance(doc, dict):
        raise SourceError(f"{path}: la raiz no es un objeto")
    return doc


class WorldTable:
    """Tramos de los mundos de la primera vuelta, leidos de AF.Levels/WorldTable.cs (FirstLevels y LastLevel)."""

    _FIRST = re.compile(r"FirstLevels\s*=\s*\{([^}]*)\}")
    _LAST = re.compile(r"const\s+int\s+LastLevel\s*=\s*(\d+)\s*;")

    def __init__(self, first_levels: list[int], last_level: int):
        if not first_levels or first_levels[0] != 1:
            raise SourceError("WorldTable: FirstLevels tiene que empezar en 1")
        if any(b <= a for a, b in zip(first_levels, first_levels[1:])):
            raise SourceError("WorldTable: FirstLevels tiene que ser creciente")
        if last_level < first_levels[-1]:
            raise SourceError("WorldTable: LastLevel menor que el primer nivel del ultimo mundo")
        self.first_levels = list(first_levels)
        self.last_level = last_level

    @classmethod
    def from_cs(cls, path: Path = WORLDTABLE_PATH) -> "WorldTable":
        try:
            text = Path(path).read_text(encoding="utf-8")
        except OSError as e:
            raise SourceError(f"no se puede leer {path}: {e.strerror}") from e
        first = cls._FIRST.search(text)
        last = cls._LAST.search(text)
        if not first or not last:
            raise SourceError(f"{path}: no encuentro FirstLevels o LastLevel")
        try:
            levels = [int(tok) for tok in first.group(1).split(",") if tok.strip()]
        except ValueError as e:
            raise SourceError(f"{path}: FirstLevels no es una lista de enteros") from e
        return cls(levels, int(last.group(1)))

    @property
    def world_count(self) -> int:
        return len(self.first_levels)

    def first_level(self, world: int) -> int:
        if not 1 <= world <= self.world_count:
            raise ValueError(f"mundo {world} fuera de 1..{self.world_count}")
        return self.first_levels[world - 1]

    def last_level_of(self, world: int) -> int:
        if not 1 <= world <= self.world_count:
            raise ValueError(f"mundo {world} fuera de 1..{self.world_count}")
        return self.last_level if world == self.world_count else self.first_levels[world] - 1

    def world_of(self, level: int) -> int:
        if not 1 <= level <= self.last_level:
            raise ValueError(f"nivel {level} fuera de 1..{self.last_level}")
        world = 1
        for i, first in enumerate(self.first_levels):
            if level >= first:
                world = i + 1
        return world


def fold(text: str) -> str:
    """Minusculas sin tildes (NFKD sin marcas combinantes): «Belén» -> «belen», «Belém» -> «belem»."""
    decomposed = unicodedata.normalize("NFKD", text)
    return "".join(c for c in decomposed if not unicodedata.combining(c)).casefold()


def has_belen(text: str) -> bool:
    """Veto de D-013, punto 11: «Belén» (el barrio de Iquitos) nunca como nombre, rotulo, nivel ni ranura.

    «Belém» (la ciudad del estuario, parada 0) no cae en el veto: sin tilde es «belem», no «belen».
    """
    return "belen" in fold(text)


_ASCII_PUNCT = str.maketrans({
    "§": "sec. ", "«": '"', "»": '"', "“": '"', "”": '"', "‘": "'", "’": "'", "…": "...", "–": "-", "—": "-",
    "→": "->", "≤": "<=", "≥": ">=", "·": "-", " ": " ", " ": " ",
})


def ascii_text(text: str) -> str:
    """Texto en ASCII para los mensajes de la herramienta: puntuacion tipografica a ASCII, sin tildes y el resto
    escapado (\\xNN)."""
    decomposed = unicodedata.normalize("NFKD", str(text).translate(_ASCII_PUNCT))
    plain = "".join(c for c in decomposed if not unicodedata.combining(c))
    return plain.encode("ascii", "backslashreplace").decode("ascii")


def iter_strings(value, path: str = ""):
    """Recorre un JSON y da (ruta, texto) de cada cadena, claves de objeto incluidas."""
    if isinstance(value, dict):
        for key, item in value.items():
            here = f"{path}/{key}"
            yield here + " (clave)", key
            yield from iter_strings(item, here)
    elif isinstance(value, list):
        for i, item in enumerate(value):
            yield from iter_strings(item, f"{path}[{i}]")
    elif isinstance(value, str):
        yield path or "/", value


# --------------------------------------------------------------------------------------------- tablas de textos (CSV)

def split_csv_records(text: str) -> list[str]:
    """Parte un CSV RFC 4180 en registros crudos, cada uno con su LF final (el ultimo, si el archivo no acaba en LF, sin
    el): un salto de linea dentro de comillas no corta el registro. Unir la lista devuelve el texto byte a byte."""
    records: list[str] = []
    start = 0
    quoted = False
    for i, c in enumerate(text):
        if c == '"':
            quoted = not quoted
        elif c == "\n" and not quoted:
            records.append(text[start:i + 1])
            start = i + 1
    if start < len(text):
        records.append(text[start:])
    return records


def parse_csv_record(record: str) -> list[str]:
    """Campos de un registro crudo (RFC 4180: comillas dobles y "" para una comilla)."""
    rows = list(csv.reader(io.StringIO(record, newline=""), strict=True))
    if len(rows) != 1:
        raise ValueError("el registro no es una sola fila CSV")
    return rows[0]


def format_csv_row(fields: list[str]) -> str:
    """Una fila CSV con LF y comillas solo donde hacen falta (comas, comillas o saltos de linea), como las de L2b."""
    buf = io.StringIO(newline="")
    csv.writer(buf, quoting=csv.QUOTE_MINIMAL, lineterminator="\n").writerow(fields)
    return buf.getvalue()


# --------------------------------------------------------------------------------------------- puntos de paso y miradores

_RE_TRAILING_PAREN = re.compile(r"\s*\([^()]*\)\s*$")
_RE_FACT_KEY_PREFIX = re.compile(r"^(map\.[a-z0-9_]+\.[a-z0-9_]+\.dato_[0-9]{2})(?![0-9])")


def place_name(nombre: str) -> str:
    """Nombre visible de un punto de paso o de un mirador [P] (M2.6): el nombre de la investigacion sin el parentesis final
    («Breves (furos)» -> «Breves») y con mayuscula inicial («boca del Xingu» -> «Boca del Xingu»). Lo demas, tal cual."""
    text = _RE_TRAILING_PAREN.sub("", unicodedata.normalize("NFC", nombre)).strip()
    return text[:1].upper() + text[1:]


def place_id(nombre: str) -> str:
    """Id [a-z0-9_]+ de un punto de paso o de un mirador [P] (M2.6): su nombre visible en minusculas, sin tildes y con
    guiones bajos («Óbidos» -> «obidos», «boca del Madeira» -> «boca_del_madeira»). Su nombre por convencion es
    map.stop.<id>.name, como el de las paradas (08 §4.5)."""
    folded = fold(place_name(nombre))
    return re.sub(r"[^a-z0-9]+", "_", folded).strip("_")


def fact_key_of(dato) -> str | None:
    """Clave del dato de un punto de paso o de un mirador: el primer token de su campo «dato»
    («map.stop.obidos.dato_01 [V MR-F21]: ...» -> «map.stop.obidos.dato_01»), o None si no lo lleva."""
    if not isinstance(dato, str):
        return None
    m = _RE_FACT_KEY_PREFIX.match(dato.strip())
    return m.group(1) if m else None


# «map.stop.teotonio.dato_01 [E wp_boto]: «Texto final.»»: la clave, la etiqueta, las fuentes y el texto final entre «».
_RE_FINAL_FACT = re.compile(
    r"^(map\.[a-z0-9_]+\.[a-z0-9_]+\.dato_[0-9]{2}) \[([A-Z]+) ([^\]]+)\]: «(.+)»$", re.DOTALL)


def final_fact_of(dato) -> dict | None:
    """Dato de un punto de paso con su texto final (M4.5a): {clave, etiqueta, fuente, texto} si el campo «dato» lo da
    entre «» («map.stop.teotonio.dato_01 [E wp_boto]: «Río arriba de Teotônio…»»), o None si solo da la clave y una
    descripción (los de 1-200: «map.stop.obidos.dato_01 [V MR-F21]: el río se aprieta a unos 2 km»)."""
    if not isinstance(dato, str):
        return None
    m = _RE_FINAL_FACT.match(unicodedata.normalize("NFC", dato.strip()))
    if not m:
        return None
    return {"clave": m.group(1), "etiqueta": m.group(2), "fuente": m.group(3).strip(), "texto": m.group(4).strip()}


# --------------------------------------------------------------------------------------------- paradas y puntos de paso
#
# Las paradas detalladas de la investigacion van en dos listas con el mismo registro (propuesta del Mapa §5.1 y §5.4 bis):
# paradas_1_200 (k = 0-19, niveles 1-200; M1.1) y paradas_201_700 (k = 20-69, niveles 201-700, fase F9: k = 20-29 desde
# M3.7a y k = 30-41 desde M3.7b; M4.5a). Las de 201-700 llevan ademas «sitio» y «wikidata».

STOP_LISTS = (("paradas_1_200", 0, 19), ("paradas_201_700", 20, 69))

# Alcance de los datos del Mapa: las paradas k = 0-41 (niveles 1-420, mundos 1-7; M4.5a). La investigacion ya detalla
# k = 42-69 (mundos 8 y 9, M3.7c y M3.7d), pero sus legs (el Beni, el Mamore por la confluencia de Villa Bella, el
# Itenez, el Madeira y el Ucayali), sus kind, flag_raise, preguntas, fondos y muestras, y el teotonio del viaje del 560
# en el via de su leg, son de M4.5b (propuesta del Mapa §5.4 quater y quinquies, «Para M4.5b»), que sube este limite a
# 69. Hasta entonces stops.py y geometry.py no leen ni comprueban esas paradas ni los puntos de paso de esos viajes.
LAST_K_IN_MAP = 41


def in_map_scope(record: dict) -> bool:
    """False solo para una parada que la investigacion ya detalla pero que aun no entra en los datos del Mapa
    (LAST_K_IN_MAP < k <= 69, la ultima k de paradas_201_700). Un registro sin k entera o con una k fuera de las listas
    se deja pasar, para que stops.py lo rechace con su mensaje."""
    k = record.get("k")
    return not (isinstance(k, int) and not isinstance(k, bool) and LAST_K_IN_MAP < k <= STOP_LISTS[-1][2])


def stop_list_of(k) -> str:
    """Nombre de la lista de la investigacion donde va la parada k (para los mensajes y el contexto de los textos)."""
    for name, lo, hi in STOP_LISTS:
        if isinstance(k, int) and lo <= k <= hi:
            return name
    return STOP_LISTS[-1][0]


def stop_records(source: dict) -> list:
    """Registros de las paradas de la investigacion (paradas_1_200 y paradas_201_700), en el orden de las listas; solo
    los objetos (las entradas que no lo son las rechaza stops.py) y solo los del alcance del Mapa (LAST_K_IN_MAP)."""
    out = []
    for name, _, _ in STOP_LISTS:
        items = source.get(name)
        if isinstance(items, list):
            out.extend(r for r in items if isinstance(r, dict) and in_map_scope(r))
    return out


def travel_passing_points(source: dict) -> list:
    """Puntos de paso de los viajes de entrada de 201-1.000 (paradas_201_1000[].puntos_de_paso_del_viaje) que llevan id:
    los que el Mapa dibuja (passing[] de river_map_v1.json; M4.5a: teotonio, el relevo del guia del viaje del 300, Q18-A y
    P-M8). Los que no llevan id (Villa Bella, Cachuela Esperanza, Riberalta) son notas del resumen de su mundo. Solo los
    de los mundos cuyo primer nivel cae en el alcance del Mapa (LAST_K_IN_MAP): el teotonio del viaje del 560 (mundo 9)
    es el mismo elemento de passing[] que el del 300 y lo cruza el leg de n = 56, que es de M4.5b."""
    out = []
    for world in source.get("paradas_201_1000", []) or []:
        if not isinstance(world, dict):
            continue
        levels = world.get("niveles")
        first = levels[0] if isinstance(levels, list) and levels else None
        if isinstance(first, int) and not isinstance(first, bool) and (first - 1) // 10 > LAST_K_IN_MAP:
            continue
        for p in world.get("puntos_de_paso_del_viaje", []) or []:
            if isinstance(p, dict) and isinstance(p.get("id"), str):
                out.append(p)
    return out
