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
