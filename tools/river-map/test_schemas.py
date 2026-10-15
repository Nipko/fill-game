# -*- coding: utf-8 -*-
"""test_schemas.py - esquemas del Mapa del rio (contratos 29 y 30) con jsonschema.

Ejecutar desde la raiz del repositorio:
    py -3.14 -m unittest discover -s tools/river-map -p "test_*.py"

Es el unico sitio de tools/river-map que usa jsonschema (ya esta en el stack); los generadores solo usan la biblioteca
estandar (08 §4.5). Estos esquemas nunca se validan en tools/validate_schema.py, que es solo de af-level/1.

af-river-stops/1: el esquema es un JSON Schema draft 2020-12 valido, el archivo real valida (con at y leg, obligatorios
desde M2.6), una parada con at y leg en la forma del ejemplo de 08 §4.5 valida y los fixtures invalidos fallan cada uno por
su motivo.

af-river-map/1 (M2.6): lo mismo para la geometria, river_map_v1.json, con sus fixtures invalidos.
"""
from __future__ import annotations

import copy
import json
import sys
import unittest
from pathlib import Path

from jsonschema import Draft202012Validator

sys.path.insert(0, str(Path(__file__).resolve().parent))

from riverdata import MAP_PATH, MAP_SCHEMA_PATH, STOPS_PATH, STOPS_SCHEMA_PATH  # noqa: E402


def _stop(doc: dict, n: int) -> dict:
    return next(s for s in doc["stops"] if s["n"] == n)


def _set(path: tuple, value):
    """Fixture: pone value en la ruta (n de la parada, campo, subcampo...) de una copia del archivo real."""
    def mutate(doc: dict) -> None:
        target = _stop(doc, path[0])
        for key in path[1:-1]:
            target = target[key]
        target[path[-1]] = value
    return mutate


def _add_field(n: int, key: str, value):
    def mutate(doc: dict) -> None:
        _stop(doc, n)[key] = value
    return mutate


def _drop_field(n: int, key: str):
    def mutate(doc: dict) -> None:
        del _stop(doc, n)[key]
    return mutate


def _root(key: str, value):
    def mutate(doc: dict) -> None:
        doc[key] = value
    return mutate


# Fixtures invalidos: (nombre, mutacion del archivo real). Cada uno tiene que fallar.
INVALID = [
    ("formato desconocido", _root("format", "af-river-stops/2")),
    ("kind fuera de gate/stop/close", _set((3, "kind"), "castle")),
    ("country en minusculas", _set((8, "country"), "co")),
    ("kit sin el mundo con 2 cifras", _set((14, "bg", "kit"), "bg_kit_w5")),
    ("heroe con Belen (veto de D-013, punto 11)", _set((15, "bg", "hero"), "bg_hero_belen")),
    ("dato de llegada como texto y no como clave", _set((2, "arrival_fact"), "Cuando el rio baja aparece una isla.")),
    ("campo de texto suelto en la parada (solo claves)", _add_field(5, "name", "Manaos")),
    ("falta km_home (extra de P-M1)", _drop_field(1, "km_home")),
    ("falta at (obligatorio desde M2.6)", _drop_field(3, "at")),
    ("falta leg (obligatorio desde M2.6)", _drop_field(0, "leg")),
    ("leg sin tramos", _set((4, "leg", "path"), [])),
    ("at con s fuera de [0, 1]", _set((6, "at", "s"), 1.5)),
    ("via con Belen", _set((1, "leg", "via"), ["belen"])),
]


class StopsSchemaTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.schema = json.loads(STOPS_SCHEMA_PATH.read_text(encoding="utf-8"))
        cls.real = json.loads(STOPS_PATH.read_text(encoding="utf-8"))
        cls.validator = Draft202012Validator(cls.schema)

    def errors(self, instance) -> list:
        return sorted(self.validator.iter_errors(instance), key=lambda e: list(e.absolute_path))

    def test_schema_is_valid_draft_2020_12(self):
        Draft202012Validator.check_schema(self.schema)
        self.assertEqual(self.schema["$schema"], "https://json-schema.org/draft/2020-12/schema")
        self.assertEqual(self.schema["properties"]["format"]["const"], "af-river-stops/1")

    def test_real_file_validates(self):
        errs = self.errors(self.real)
        self.assertEqual([], [f"{list(e.absolute_path)}: {e.message}" for e in errs])
        self.assertEqual(20, len(self.real["stops"]))

    def test_every_stop_has_at_and_leg(self):
        for s in self.real["stops"]:
            self.assertIn("at", s)
            self.assertIn("leg", s)
        self.assertIn("at", self.schema["$defs"]["stop"]["required"])
        self.assertIn("leg", self.schema["$defs"]["stop"]["required"])

    def test_stop_with_at_and_leg_of_08_4_5_validates(self):
        doc = copy.deepcopy(self.real)
        iquitos = _stop(doc, 14)
        # Forma de at y leg del ejemplo de 08 §4.5 (los añade M2.6).
        iquitos["at"] = {"line": "amazonas", "s": 0.437}
        iquitos["leg"] = {"path": [{"line": "amazonas", "s0": 0.49, "s1": 0.437}],
                          "via": ["caballococha", "pebas"], "mouths": ["napo"]}
        iquitos["facts"] = ["map.stop.iquitos.dato_01"]
        self.assertEqual([], [e.message for e in self.errors(doc)])

    def test_invalid_fixtures_fail(self):
        self.assertGreaterEqual(len(INVALID), 6)
        for name, mutate in INVALID:
            with self.subTest(fixture=name):
                doc = copy.deepcopy(self.real)
                mutate(doc)
                self.assertNotEqual([], self.errors(doc), f"el esquema acepto el fixture «{name}»")



# af-river-map/1 (M2.6): fixtures invalidos sobre una copia del archivo real.
def _line(lid: str, key: str, value):
    def mutate(doc: dict) -> None:
        next(line for line in doc["lines"] if line["id"] == lid)[key] = value
    return mutate


def _drop_line_key(lid: str, key: str):
    def mutate(doc: dict) -> None:
        del next(line for line in doc["lines"] if line["id"] == lid)[key]
    return mutate


def _map_root(key: str, value):
    def mutate(doc: dict) -> None:
        doc[key] = value
    return mutate


def _first(key: str, field: str, value):
    def mutate(doc: dict) -> None:
        doc[key][0][field] = value
    return mutate


MAP_INVALID = [
    ("formato desconocido", _map_root("format", "af-river-map/2")),
    ("giro distinto de cw90", lambda d: d["frame"].update(rotation="ccw90")),
    ("kind desconocido", _line("xingu", "kind", "river")),
    ("class fuera de 1-3", _line("xingu", "class", 4)),
    ("src desconocido", _line("xingu", "src", "osm")),
    ("pts que no es base64", _line("xingu", "pts", "no es base64!")),
    ("agua de un tipo desconocido", _line("negro", "water", [{"s0": 0.0, "s1": 1.0, "type": "brown"}])),
    ("linea main con boca", _line("amazonas", "mouth", {"on": "para", "s": 0.5})),
    ("afluente sin boca", _drop_line_key("madeira", "mouth")),
    ("afluente sin fill_on_pass", _drop_line_key("madeira", "fill_on_pass")),
    ("fill_on_pass de 0", _line("madeira", "fill_on_pass", 0)),
    ("splits en un afluente", _line("negro", "splits", [])),
    ("recuadros en la 1.0", _map_root("insets", [{"id": "orinoco"}])),
    ("rotulo con el pais en minusculas", _first("labels", "country", "br")),
    ("rotulo fuera de uint16", _first("labels", "xy", [70000, 0])),
    ("punto de paso con texto y no con clave", _first("passing", "fact", "El rio se aprieta a unos 2 km.")),
    ("mirador sin reveal_on", lambda d: d["viewpoints"][0].pop("reveal_on")),
    ("campo de texto suelto en una linea (solo claves)", _line("tapajos", "name", "Tapajos")),
    ("casa sin linea", _map_root("home", {"s": 0.5})),
    ("id con Belen", _first("passing", "id", "belen")),
]


class MapSchemaTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.schema = json.loads(MAP_SCHEMA_PATH.read_text(encoding="utf-8"))
        cls.real = json.loads(MAP_PATH.read_text(encoding="utf-8"))
        cls.validator = Draft202012Validator(cls.schema)

    def errors(self, instance) -> list:
        return sorted(self.validator.iter_errors(instance), key=lambda e: list(e.absolute_path))

    def test_schema_is_valid_draft_2020_12(self):
        Draft202012Validator.check_schema(self.schema)
        self.assertEqual(self.schema["$schema"], "https://json-schema.org/draft/2020-12/schema")
        self.assertEqual(self.schema["properties"]["format"]["const"], "af-river-map/1")

    def test_real_file_validates(self):
        errs = self.errors(self.real)
        self.assertEqual([], [f"{list(e.absolute_path)}: {e.message}" for e in errs])
        self.assertEqual(27, len(self.real["lines"]))

    def test_invalid_fixtures_fail(self):
        self.assertGreaterEqual(len(MAP_INVALID), 10)
        for name, mutate in MAP_INVALID:
            with self.subTest(fixture=name):
                doc = copy.deepcopy(self.real)
                mutate(doc)
                self.assertNotEqual([], self.errors(doc), f"el esquema acepto el fixture «{name}»")


if __name__ == "__main__":
    unittest.main()
