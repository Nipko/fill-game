# -*- coding: utf-8 -*-
"""test_stops_preserve.py - stops.py conserva las traducciones del bloque map.* al regenerarlo (T2-Q1).

Ejecutar desde la raiz del repositorio:
    py -3.14 -m unittest discover -s tools/river-map -p "test_*.py"

Cubre: render_facts copia en, pt-BR, status_en y status_pt-BR de una fila map.* cuyo es-419 no cambió; las vuelve a vacío
si el es-419 cambió (08 §4.7); no inventa nada para una fila nueva; deja intactas las filas de fuera del bloque; y el
bloque real de facts.csv sigue al día con --check (lo regenera igual que está). Solo biblioteca estandar.
"""
from __future__ import annotations

import sys
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

import stops  # noqa: E402

HEADER = "key,es-419,en,pt-BR,context,max_chars,status,status_en,status_pt-BR,notes,args\n"
COLS = HEADER.strip().split(",")


def row(key: str, es: str) -> dict:
    return {"key": key, "es-419": es, "en": "", "pt-BR": "", "context": "ctx", "max_chars": "90",
            "status": "pendiente_asesor", "status_en": "", "status_pt-BR": "", "notes": "generada", "args": ""}


def parse(text: str) -> dict:
    out = {}
    for record in stops.split_csv_records(text)[1:]:
        fields = stops.parse_csv_record(record)
        out[fields[0]] = dict(zip(COLS, fields))
    return out


class PreserveTests(unittest.TestCase):
    CURRENT = (HEADER
               + "fact.x.01,Fruta,Fruit,Fruta,ctx,90,pendiente_asesor,traducido,traducido,n,\n"
               + "map.a.dato_01,Dato A,Fact A,Dado A,ctx,90,pendiente_asesor,traducido,revisado_nativo,generada,\n"
               + "map.b.dato_01,Dato B viejo,Old B,B velho,ctx,90,pendiente_asesor,traducido,traducido,generada,\n"
               + "tint.x.name,X,X,X,ctx,24,pendiente_asesor,traducido,traducido,n,\n")

    def render(self, rows: dict) -> dict:
        text, errors = stops.render_facts(self.CURRENT, rows)
        self.assertEqual(errors, [])
        return parse(text)

    def test_the_marker_exists(self):
        self.assertEqual(stops.PRESERVED_LOCALE_COLUMNS, ("en", "pt-BR", "status_en", "status_pt-BR"))

    def test_same_spanish_keeps_translation_and_status(self):
        got = self.render({"map.a.dato_01": row("map.a.dato_01", "Dato A")})
        a = got["map.a.dato_01"]
        self.assertEqual((a["en"], a["pt-BR"], a["status_en"], a["status_pt-BR"]), ("Fact A", "Dado A", "traducido", "revisado_nativo"))
        self.assertEqual(a["notes"], "generada", "las demas columnas salen de la fuente")

    def test_changed_spanish_resets_to_empty(self):
        got = self.render({"map.b.dato_01": row("map.b.dato_01", "Dato B nuevo")})
        b = got["map.b.dato_01"]
        self.assertEqual((b["es-419"], b["en"], b["pt-BR"], b["status_en"], b["status_pt-BR"]), ("Dato B nuevo", "", "", "", ""))

    def test_new_row_has_no_translation(self):
        got = self.render({"map.c.dato_01": row("map.c.dato_01", "Dato C")})
        self.assertEqual(got["map.c.dato_01"]["en"], "")
        self.assertNotIn("map.a.dato_01", got, "una fila que la fuente ya no da desaparece, como antes")

    def test_rows_outside_the_block_are_byte_identical(self):
        text, _ = stops.render_facts(self.CURRENT, {"map.a.dato_01": row("map.a.dato_01", "Dato A")})
        for line in self.CURRENT.splitlines(keepends=True):
            if line.startswith(("fact.", "tint.", "key,")):
                self.assertIn(line, text)

    def test_rendering_twice_is_stable(self):
        rows = {"map.a.dato_01": row("map.a.dato_01", "Dato A"), "map.b.dato_01": row("map.b.dato_01", "Dato B viejo")}
        once, _ = stops.render_facts(self.CURRENT, rows)
        twice, _ = stops.render_facts(once, rows)
        self.assertEqual(once, twice)
        self.assertEqual(once, self.CURRENT, "con el mismo es-419, regenerar no cambia ni un byte")


if __name__ == "__main__":
    unittest.main()
