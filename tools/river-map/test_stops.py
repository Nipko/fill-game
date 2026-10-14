# -*- coding: utf-8 -*-
"""test_stops.py - pruebas de tools/river-map/stops.py (paradas af-river-stops/1 de 1-200, M1.1).

Ejecutar desde la raiz del repositorio:
    py -3.14 -m unittest discover -s tools/river-map -p "test_*.py"

Cubre: los campos en n = 0, 1, 4, 8, 9, 13, 16, 17 y 19; la conversion de cada registro de rio-amazonas.json; la
sincronia con WorldTable.cs (leido aqui con su propio analizador); el determinismo (mismos bytes con distintas semillas
de hash); el desfase con lo commiteado; y los fixtures invalidos, cada uno con codigo 1 y mensajes ASCII en --check.
Solo biblioteca estandar.
"""
from __future__ import annotations

import copy
import hashlib
import json
import os
import re
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

import stops  # noqa: E402
from riverdata import (  # noqa: E402
    OVERRIDES_PATH,
    ROOT,
    SOURCE_PATH,
    STOPS_PATH,
    WORLDTABLE_PATH,
    WorldTable,
    load_json,
)

STOPS_SCRIPT = HERE / "stops.py"


def run_stops(*args: str, env: dict | None = None) -> subprocess.CompletedProcess:
    """Ejecuta stops.py como lo haria un humano o el CI (salida en bytes, para comprobar que es ASCII)."""
    return subprocess.run([sys.executable, str(STOPS_SCRIPT), *args], cwd=ROOT, capture_output=True,
                          env=env, timeout=120)


def is_ascii(data: bytes) -> bool:
    return all(b < 128 for b in data)


def parse_worldtable_cs() -> tuple[list[int], int]:
    """Analizador propio de WorldTable.cs (independiente del de riverdata) para la prueba de sincronia."""
    text = WORLDTABLE_PATH.read_text(encoding="utf-8")
    first = re.search(r"int\[\]\s+FirstLevels\s*=\s*\{\s*([0-9,\s]+)\}", text)
    last = re.search(r"LastLevel\s*=\s*([0-9]+)", text)
    assert first and last, "WorldTable.cs sin FirstLevels o LastLevel"
    return [int(x) for x in first.group(1).replace(" ", "").split(",") if x], int(last.group(1))


class FieldTests(unittest.TestCase):
    """Campos de 08 §4.5 y de P-M1 en las paradas que fija el plan (n = 0, 1, 4, 8, 9, 13, 16, 17 y 19)."""

    @classmethod
    def setUpClass(cls):
        cls.source = load_json(SOURCE_PATH)
        doc, errors = stops.build(cls.source, load_json(OVERRIDES_PATH), WorldTable.from_cs())
        assert not errors, errors
        cls.doc = doc
        cls.by_n = {s["n"]: s for s in doc["stops"]}
        cls.records = {r["k"]: r for r in cls.source["paradas_1_200"]}

    def bg(self, kit, hero=None, code=None, lut=None) -> dict:
        return {"kit": kit, "hero": hero, "code": code, "lut": lut}

    def assertStop(self, n: int, **expected):
        stop = self.by_n[n]
        for key, value in expected.items():
            with self.subTest(n=n, field=key):
                self.assertEqual(value, stop[key])

    def test_twenty_stops_with_the_fields_of_08_4_5_in_fixed_order(self):
        self.assertEqual("af-river-stops/1", self.doc["format"])
        self.assertEqual("docs/research/mapa-del-rio/rio-amazonas.json", self.doc["source"])
        self.assertEqual(list(range(20)), [s["n"] for s in self.doc["stops"]])
        for s in self.doc["stops"]:
            self.assertEqual(list(stops.STOP_FIELDS), list(s.keys()))
            self.assertEqual(["kit", "hero", "code", "lut"], list(s["bg"].keys()))
            self.assertEqual([], s["facts"], "facts[] vacio hasta M2.6")

    def test_n0_belem(self):
        self.assertStop(0, levels=[1, 10], world=1, id="belem", kind="gate", country="BR", country_name=None,
                        km_from_sea=None, km_home=0, sample="white", flag_raise="BR", arrival_fact=None,
                        world_question=None, answers_world_question=False,
                        bg=self.bg("bg_kit_w01", hero="bg_hero_ver_o_peso"))

    def test_n1_santarem_takes_the_estuary_fact_of_the_map_of_10(self):
        self.assertStop(1, levels=[11, 20], world=2, id="santarem", kind="gate", country="BR", country_name=None,
                        sample="clear_white", flag_raise=None, arrival_fact="map.stop.estuario.dato_03",
                        world_question="map.world.w02.question", answers_world_question=False,
                        bg=self.bg("bg_kit_w02", code="codigo:dos_aguas_clara_blanca"))
        self.assertEqual(self.records[0]["dato_del_nivel"]["clave"], self.by_n[1]["arrival_fact"])

    def test_n4_encontro(self):
        self.assertStop(4, levels=[41, 50], world=3, id="encontro", kind="gate", country="BR",
                        sample="black_white", flag_raise=None, arrival_fact="map.stop.manaos.dato_01",
                        world_question="map.world.w03.question", answers_world_question=False,
                        bg=self.bg("bg_kit_w03", code="codigo:dos_aguas_negra_blanca"))

    def test_n8_leticia_raises_the_flag_of_colombia(self):
        self.assertStop(8, levels=[81, 90], world=4, id="leticia", kind="gate", country="CO", country_name=None,
                        sample="white", flag_raise="CO", arrival_fact="map.country.co.dato_01",
                        world_question="map.world.w04.question", answers_world_question=False,
                        bg=self.bg("bg_kit_w04", hero="bg_hero_leticia"))

    def test_n9_leticia_at_dusk(self):
        self.assertStop(9, levels=[91, 100], world=4, id="leticia_atardecer", kind="stop", country="CO",
                        flag_raise=None, arrival_fact="map.stop.leticia.dato_02", world_question=None,
                        answers_world_question=False, bg=self.bg("bg_kit_w04", hero="bg_hero_leticia", lut="dusk"))

    def test_n13_border_river_with_two_countries(self):
        self.assertStop(13, levels=[131, 140], world=4, id="rio_frontera", kind="close", country="CO",
                        country_name="map.stop.rio_frontera.country", flag_raise=None,
                        arrival_fact="map.stop.rio_frontera.dato_01", world_question=None, answers_world_question=True,
                        bg=self.bg("bg_kit_w04", code="kit:rio_ancho_con_baliza"))

    def test_n16_allpahuayo_black_water_in_a_white_world(self):
        self.assertStop(16, levels=[161, 170], world=5, id="allpahuayo_mishana", kind="stop", country="PE",
                        sample="black", flag_raise=None, arrival_fact="map.stop.allpahuayo.dato_01",
                        answers_world_question=False, bg=self.bg("bg_kit_w05", hero="bg_hero_allpahuayo"))

    def test_n17_nauta_answers_the_question_of_world_5(self):
        self.assertStop(17, levels=[171, 180], world=5, id="nauta", kind="stop", country="PE", sample="white",
                        arrival_fact="map.stop.nauta.dato_01", world_question=None, answers_world_question=True,
                        bg=self.bg("bg_kit_w05", code="kit:orilla_de_nauta"))

    def test_n19_pacaya_samiria_at_dawn_closes_world_5(self):
        self.assertStop(19, levels=[191, 200], world=5, id="pacaya_alba", kind="close", country="PE",
                        sample="black", flag_raise=None, arrival_fact="map.stop.pacaya_samiria.dato_02",
                        answers_world_question=False,
                        bg=self.bg("bg_kit_w05", hero="bg_hero_pacaya_samiria", lut="dawn"))

    def test_flags_and_luts_across_all_stops(self):
        flags = {s["n"]: s["flag_raise"] for s in self.doc["stops"] if s["flag_raise"]}
        self.assertEqual({0: "BR", 8: "CO", 14: "PE"}, flags)
        luts = {s["n"]: s["bg"]["lut"] for s in self.doc["stops"] if s["bg"]["lut"]}
        self.assertEqual({9: "dusk", 19: "dawn"}, luts)
        questions = {s["n"]: s["world_question"] for s in self.doc["stops"] if s["world_question"]}
        self.assertEqual({1: "map.world.w02.question", 4: "map.world.w03.question", 8: "map.world.w04.question",
                          14: "map.world.w05.question"}, questions)
        answers = [s["n"] for s in self.doc["stops"] if s["answers_world_question"]]
        self.assertEqual([3, 6, 13, 17], answers)

    def test_each_stop_comes_from_its_record(self):
        for s in self.doc["stops"]:
            r = self.records[s["n"]]
            with self.subTest(n=s["n"]):
                self.assertEqual(r["id"], s["id"])
                self.assertEqual(r["niveles"], s["levels"])
                self.assertEqual(r["mundo"], s["world"])
                self.assertEqual(r["km_desde_el_mar"], s["km_from_sea"])
                self.assertEqual(r["km_casa"], s["km_home"])
                self.assertEqual(r["fondo"]["kit"], s["bg"]["kit"])
                if s["n"] >= 2:
                    self.assertEqual(r["dato_del_nivel"]["clave"], s["arrival_fact"])
                self.assertEqual(stops.KIND[r["tipo"]], s["kind"])

    def test_no_text_only_keys(self):
        key = re.compile(r"map\.[a-z0-9_.]+")
        for s in self.doc["stops"]:
            for field in ("arrival_fact", "world_question", "country_name"):
                if s[field] is not None:
                    self.assertRegex(s[field], key)


class WorldTableSyncTests(unittest.TestCase):
    """Las paradas respetan WorldTable.cs: cada una cae en un solo mundo y gate/close casan con sus tramos."""

    @classmethod
    def setUpClass(cls):
        cls.first_levels, cls.last_level = parse_worldtable_cs()
        cls.doc = json.loads(STOPS_PATH.read_text(encoding="utf-8"))

    def world_of(self, level: int) -> int:
        return max(i + 1 for i, first in enumerate(self.first_levels) if level >= first)

    def last_of(self, world: int) -> int:
        return self.last_level if world == len(self.first_levels) else self.first_levels[world] - 1

    def test_riverdata_reads_the_same_table(self):
        wt = WorldTable.from_cs()
        self.assertEqual(self.first_levels, wt.first_levels)
        self.assertEqual(self.last_level, wt.last_level)

    def test_every_stop_matches_worldtable(self):
        for s in self.doc["stops"]:
            a, b = s["levels"]
            with self.subTest(n=s["n"]):
                self.assertEqual(self.world_of(a), s["world"])
                self.assertEqual(self.world_of(b), s["world"])
                self.assertEqual(s["kind"] == "gate", a == self.first_levels[s["world"] - 1])
                is_last = b == self.last_of(s["world"]) and s["kind"] != "gate"
                self.assertEqual(is_last, s["kind"] == "close")

    def test_stops_cover_whole_worlds_1_to_5(self):
        covered = [lvl for s in self.doc["stops"] for lvl in range(s["levels"][0], s["levels"][1] + 1)]
        self.assertEqual(list(range(1, self.last_of(5) + 1)), covered)
        gates = [s["levels"][0] for s in self.doc["stops"] if s["kind"] == "gate"]
        self.assertEqual(self.first_levels[:5], gates)


class OutputTests(unittest.TestCase):
    """Determinismo, desfase y --check sobre el archivo commiteado."""

    def test_committed_file_is_utf8_without_bom_with_lf(self):
        data = STOPS_PATH.read_bytes()
        self.assertFalse(data.startswith(b"\xef\xbb\xbf"))
        self.assertNotIn(b"\r", data)
        self.assertTrue(data.endswith(b"}\n"))
        data.decode("utf-8")

    def test_check_passes_on_the_committed_file(self):
        proc = run_stops("--check")
        self.assertEqual(0, proc.returncode, proc.stdout.decode("ascii", "replace"))
        self.assertTrue(is_ascii(proc.stdout + proc.stderr))

    def test_deterministic_bytes_with_different_hash_seeds(self):
        source_hash = hashlib.sha256(SOURCE_PATH.read_bytes()).hexdigest()
        with tempfile.TemporaryDirectory() as tmp:
            outs = []
            for seed in ("0", "1", "4242"):
                out = Path(tmp) / f"stops_{seed}.json"
                env = dict(os.environ, PYTHONHASHSEED=seed)
                proc = run_stops("--out", str(out), env=env)
                self.assertEqual(0, proc.returncode, proc.stdout.decode("ascii", "replace"))
                outs.append(out.read_bytes())
            self.assertEqual(1, len(set(outs)))
            self.assertEqual(STOPS_PATH.read_bytes(), outs[0], "regenerar produce diff con lo commiteado")
        self.assertEqual(source_hash, hashlib.sha256(SOURCE_PATH.read_bytes()).hexdigest(),
                         "stops.py no puede modificar rio-amazonas.json")

    def test_render_is_stable_in_memory(self):
        source, overrides, wt = load_json(SOURCE_PATH), load_json(OVERRIDES_PATH), WorldTable.from_cs()
        first = stops.render(stops.build(copy.deepcopy(source), overrides, wt)[0])
        second = stops.render(stops.build(copy.deepcopy(source), overrides, wt)[0])
        self.assertEqual(first, second)

    def test_check_fails_on_drift(self):
        with tempfile.TemporaryDirectory() as tmp:
            out = Path(tmp) / "river_stops_v1.json"
            out.write_bytes(STOPS_PATH.read_bytes().replace(b'"km_home": 1479', b'"km_home": 1480'))
            proc = run_stops("--check", "--out", str(out))
            self.assertEqual(1, proc.returncode)
            self.assertIn(b"desfase", proc.stdout)
            self.assertTrue(is_ascii(proc.stdout + proc.stderr))

    def test_check_fails_on_crlf(self):
        with tempfile.TemporaryDirectory() as tmp:
            out = Path(tmp) / "river_stops_v1.json"
            out.write_bytes(STOPS_PATH.read_bytes().replace(b"\n", b"\r\n"))
            proc = run_stops("--check", "--out", str(out))
            self.assertEqual(1, proc.returncode)
            self.assertIn(b"desfase", proc.stdout)

    def test_check_fails_when_the_file_is_missing(self):
        with tempfile.TemporaryDirectory() as tmp:
            proc = run_stops("--check", "--out", str(Path(tmp) / "no_existe.json"))
            self.assertEqual(1, proc.returncode)
            self.assertIn(b"falta", proc.stdout)


# ------------------------------------------------------------------------------ fixtures invalidos de --check

def _rec(doc: dict, k: int) -> dict:
    return next(r for r in doc["paradas_1_200"] if r["k"] == k)


def _set_rec(k: int, field: str, value):
    def mutate(src: dict, ov: dict) -> None:
        _rec(src, k)[field] = value
    return mutate


def _set_fondo(k: int, field: str, value):
    def mutate(src: dict, ov: dict) -> None:
        _rec(src, k)["fondo"][field] = value
    return mutate


def _drop_rec(k: int):
    def mutate(src: dict, ov: dict) -> None:
        src["paradas_1_200"] = [r for r in src["paradas_1_200"] if r["k"] != k]
    return mutate


def _no_overrides(src: dict, ov: dict) -> None:
    ov["stops"] = {}


def _unknown_override(src: dict, ov: dict) -> None:
    ov["stops"]["macapa"] = {"country": "BR"}


def _extra_crossing(src: dict, ov: dict) -> None:
    src["cruces_de_pais"].append({"nombre": "bandera de mas", "pais": "CO", "nivel": 90,
                                  "llega_en": {"parada_1_200": 9}})


def _question_nobody_answers(src: dict, ov: dict) -> None:
    _rec(src, 1)["pregunta_del_mundo"]["se_responde_en"] = 35


# (nombre, mutacion de la fuente y de overrides, fragmento esperado en la salida)
FIXTURES = [
    ("levels mal", _set_rec(5, "niveles", [52, 61]), "levels"),
    ("n no contiguo (falta la parada 7)", _drop_rec(7), "n no contiguo"),
    ("mundo mal", _set_rec(4, "mundo", 2), "WorldTable"),
    ("gate mal colocado", _set_rec(2, "tipo", "gran_parada"), "gate fuera"),
    ("primera parada del mundo sin gate", _set_rec(8, "tipo", "parada"), "tiene que ser gate"),
    ("ultima parada del mundo sin close", _set_rec(3, "tipo", "parada"), "tiene que ser close"),
    ("tipo desconocido", _set_rec(10, "tipo", "pueblo"), "tipo"),
    ("kit de otro mundo", _set_fondo(8, "kit", "bg_kit_w05"), "bg.kit"),
    ("id repetido", _set_rec(9, "id", "leticia"), "id repetido"),
    ("Belen como id", _set_rec(15, "id", "belen"), "veto de Belen"),
    ("Belen con tilde en la ranura", _set_fondo(15, "heroe", "bg_hero_belén"), "veto de Belen"),
    ("agua desconocida", _set_rec(2, "agua_del_sitio", "turquesa"), "agua_del_sitio"),
    ("sufijo de luz desconocido", _set_fondo(9, "heroe", "bg_hero_leticia + LUT de noche"), "sufijo"),
    ("parada de dos paises sin override", _no_overrides, "ISO2"),
    ("override de una parada que no existe", _unknown_override, "no es ninguna parada"),
    ("bandera incoherente con cruces_de_pais", _set_rec(8, "bandera", "PE"), "bandera"),
    ("bandera sin cambio de pais", _extra_crossing, "flag_raise"),
    ("dato de llegada ausente", _set_rec(5, "dato_del_nivel", None), "dato_del_nivel"),
    ("pregunta que no responde ninguna llegada", _question_nobody_answers, "no es la llegada"),
]


class InvalidFixtureTests(unittest.TestCase):
    """stops.py --check sale con 1 en cada fixture invalido, con su motivo y en ASCII."""

    @classmethod
    def setUpClass(cls):
        cls.source = load_json(SOURCE_PATH)
        cls.overrides = load_json(OVERRIDES_PATH)

    def test_fixtures_fail_with_code_1(self):
        self.assertGreaterEqual(len(FIXTURES), 10)
        with tempfile.TemporaryDirectory() as tmp:
            for i, (name, mutate, expected) in enumerate(FIXTURES):
                with self.subTest(fixture=name):
                    src, ov = copy.deepcopy(self.source), copy.deepcopy(self.overrides)
                    mutate(src, ov)
                    src_path = Path(tmp) / f"rio_{i:02d}.json"
                    ov_path = Path(tmp) / f"overrides_{i:02d}.json"
                    src_path.write_text(json.dumps(src, ensure_ascii=False), encoding="utf-8")
                    ov_path.write_text(json.dumps(ov, ensure_ascii=False), encoding="utf-8")
                    proc = run_stops("--check", "--source", str(src_path), "--overrides", str(ov_path))
                    out = proc.stdout.decode("ascii", "replace")
                    self.assertEqual(1, proc.returncode, out)
                    self.assertIn(expected, out)
                    self.assertTrue(is_ascii(proc.stdout + proc.stderr), out)

    def test_writing_refuses_an_invalid_source(self):
        src = copy.deepcopy(self.source)
        _rec(src, 15)["id"] = "belen"
        with tempfile.TemporaryDirectory() as tmp:
            src_path = Path(tmp) / "rio.json"
            out = Path(tmp) / "river_stops_v1.json"
            src_path.write_text(json.dumps(src, ensure_ascii=False), encoding="utf-8")
            proc = run_stops("--source", str(src_path), "--out", str(out))
            self.assertEqual(1, proc.returncode)
            self.assertFalse(out.exists(), "con errores no se escribe nada")

    def test_belem_is_not_belen(self):
        from riverdata import has_belen
        self.assertFalse(has_belen("belem"))
        self.assertFalse(has_belen("Belém"))
        self.assertTrue(has_belen("Belén"))
        self.assertTrue(has_belen("bg_hero_BELEN_mercado"))


if __name__ == "__main__":
    unittest.main()
