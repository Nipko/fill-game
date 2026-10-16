# -*- coding: utf-8 -*-
"""test_stops.py - pruebas de tools/river-map/stops.py (paradas af-river-stops/1 de 1-200, M1.1, y sus textos, M1.1t).

Ejecutar desde la raiz del repositorio:
    py -3.14 -m unittest discover -s tools/river-map -p "test_*.py"

Cubre: los campos en n = 0, 1, 4, 8, 9, 13, 16, 17 y 19; la conversion de cada registro de rio-amazonas.json; la
sincronia con WorldTable.cs (leido aqui con su propio analizador); el determinismo (mismos bytes con distintas semillas
de hash); el desfase con lo commiteado; y los fixtures invalidos, cada uno con codigo 1 y mensajes ASCII en --check.
Textos (M1.1t): el bloque map.* de facts.csv (contenido, columnas, orden ordinal, args, determinismo, filas de fuera del
bloque intactas) y los fallos de --check (desfase, clave sin fila o sin texto, longitudes, cifras con separador y Belen).
M2.6: facts[] con los «Otro dato», at y leg en cada parada, los nombres de los puntos de paso, de los miradores y de los
paises de los rotulos, y los fixtures de las rutas (parada desplazada 20 km, boca fuera de su rio y candidato ausente).
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
    FACTS_PATH,
    MAP_PATH,
    OVERRIDES_PATH,
    ROOT,
    SOURCE_PATH,
    STOPS_PATH,
    WORLDTABLE_PATH,
    WorldTable,
    load_json,
    parse_csv_record,
    split_csv_records,
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
            record = self.records[s["n"]]
            self.assertEqual([o["clave"] for o in record["otros_datos"]], s["facts"], "facts[] = otros_datos (M2.6)")
            self.assertEqual(["line", "s"], list(s["at"].keys()))
            self.assertEqual(["path", "via", "mouths"], list(s["leg"].keys()))

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
        # fullmatch con los patrones del generador: una clave seguida de texto («map.x Manaos es grande») no pasa.
        def assertKey(pattern: re.Pattern, value, where: str):
            with self.subTest(where=where):
                self.assertIsInstance(value, str)
                self.assertNotIn(" ", value)
                self.assertIsNotNone(pattern.fullmatch(value), f"{value!r} no es una clave {pattern.pattern}")

        self.assertIsNone(stops.RE_FACT_KEY.fullmatch("map.stop.manaos.dato_01 Manaos es grande"))
        for s in self.doc["stops"]:
            n = s["n"]
            if s["arrival_fact"] is not None:
                assertKey(stops.RE_FACT_KEY, s["arrival_fact"], f"n={n} arrival_fact")
            for i, fact in enumerate(s["facts"]):
                assertKey(stops.RE_FACT_KEY, fact, f"n={n} facts[{i}]")
            if s["world_question"] is not None:
                assertKey(stops.RE_QUESTION, s["world_question"], f"n={n} world_question")
            if s["country_name"] is not None:
                assertKey(re.compile(rf"map\.stop\.{re.escape(s['id'])}\.country"), s["country_name"],
                          f"n={n} country_name")
        self.assertEqual(19, sum(s["arrival_fact"] is not None for s in self.doc["stops"]))
        self.assertEqual(4, sum(s["world_question"] is not None for s in self.doc["stops"]))
        self.assertEqual(1, sum(s["country_name"] is not None for s in self.doc["stops"]))


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


def _question_answered_at(k: int, level: int):
    def mutate(src: dict, ov: dict) -> None:
        _rec(src, k)["pregunta_del_mundo"]["se_responde_en"] = level
    return mutate


def _second_question(src: dict, ov: dict) -> None:
    # Una segunda pregunta en el mundo 2 (parada k=2), respondida al llegar a ella misma (nivel 20).
    _rec(src, 2)["pregunta_del_mundo"] = {"texto": "¿Otra pregunta?", "se_responde_en": 20}


def _override_field(sid: str, field: str, value):
    def mutate(src: dict, ov: dict) -> None:
        ov["stops"].setdefault(sid, {})[field] = value
    return mutate


_DATO_K1 = {"clave": "map.stop.santarem.dato_01", "texto": "Un dato de Santarem.", "etiqueta": "E", "fuente": "MR-F19"}


def _set_dato(k: int, field: str, value):
    def mutate(src: dict, ov: dict) -> None:
        _rec(src, k)["dato_del_nivel"][field] = value
    return mutate


def _set_dato_with_args(k: int, text: str, args: dict):
    def mutate(src: dict, ov: dict) -> None:
        _rec(src, k)["dato_del_nivel"].update(texto=text, args=args)
    return mutate


def _drop_dato_field(k: int, field: str):
    def mutate(src: dict, ov: dict) -> None:
        del _rec(src, k)["dato_del_nivel"][field]
    return mutate


def _set_question(k: int, text: str):
    def mutate(src: dict, ov: dict) -> None:
        _rec(src, k)["pregunta_del_mundo"]["texto"] = text
    return mutate


def _set_otro(k: int, i: int, field: str, value):
    """Fixture: cambia el campo de un «Otro dato» (otros_datos[i] de la parada k)."""
    def mutate(src: dict, ov: dict) -> None:
        _rec(src, k)["otros_datos"][i][field] = value
    return mutate


def _drop_otro(k: int, key: str):
    """Fixture: quita un «Otro dato» de la parada k (un candidato de la propuesta §5.1 bis que falta)."""
    def mutate(src: dict, ov: dict) -> None:
        _rec(src, k)["otros_datos"] = [o for o in _rec(src, k)["otros_datos"] if o["clave"] != key]
    return mutate


def _shift_stop(k: int, dlat: float):
    """Fixture: desplaza el sitio de la parada k dlat grados de latitud (0,18 grados = 20 km)."""
    def mutate(src: dict, ov: dict) -> None:
        _rec(src, k)["lat"] = round(_rec(src, k)["lat"] + dlat, 4)
    return mutate


def _shift_mouth(rid: str, dlat: float):
    """Fixture: mueve la boca (el ultimo punto) de un rio dlat grados de latitud."""
    def mutate(src: dict, ov: dict) -> None:
        river = next(r for r in src["rios"] if r["id"] == rid)
        river["puntos"][-1]["lat"] = round(river["puntos"][-1]["lat"] + dlat, 4)
    return mutate


def _set_source(sid: str, field: str, value):
    def mutate(src: dict, ov: dict) -> None:
        next(f for f in src["fuentes"] if f["id"] == sid)[field] = value
    return mutate


def _drop_override(sid: str, field: str):
    def mutate(src: dict, ov: dict) -> None:
        del ov["stops"][sid][field]
    return mutate


def _unknown_country(src: dict, ov: dict) -> None:
    # Las paradas del Peru (k = 14-19) pasan a un pais «XX» sin entrada en COUNTRY_NAMES, con su bandera y su cruce de
    # pais coherentes: validate() no se queja y falla solo el nombre del pais.
    for r in src["paradas_1_200"]:
        if r["k"] >= 14:
            r["pais"] = "XX"
            if "bandera" in r:
                r["bandera"] = "XX"
    next(c for c in src["cruces_de_pais"] if c.get("llega_en") == {"parada_1_200": 14})["pais"] = "XX"


def _drop_rotulo(name: str):
    def mutate(src: dict, ov: dict) -> None:
        src["rotulos_de_pais"] = [r for r in src["rotulos_de_pais"] if r["nombre"] != name]
    return mutate


# Un dato de 91 caracteres (uno mas que el limite de 08 §4.5).
_TEXT_91 = "En Brasil el río se llama Solimões hasta aquí; con el Negro ya se llama «Amazonas», al fin."


# (nombre, mutacion de la fuente y de overrides, fragmento esperado en la salida)
FIXTURES = [
    ("levels mal", _set_rec(5, "niveles", [52, 61]), "levels"),
    ("n no contiguo (falta la parada 7)", _drop_rec(7), "n no contiguo"),
    ("falta la ultima parada (k=19)", _drop_rec(19),
     "no cubre los niveles 1-200 (D-013, punto 3): faltan las paradas k=[19]"),
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
    # Reglas de validate() y convert() que no tenian fixture (pendientes de la revision de M1.1).
    ("pregunta respondida antes de plantearse", _question_answered_at(1, 10), "antes de plantearse"),
    ("segunda pregunta del mundo", _second_question, "segunda pregunta del mundo 2"),
    ("pregunta respondida en otro mundo", _question_answered_at(1, 40), "que es del mundo 3"),
    ("country_name fuera de la convencion", _override_field("rio_frontera", "country_name", "map.stop.leticia.country"),
     "la convencion es map.stop.rio_frontera.country"),
    ("dato de la llegada del 10 en la parada k=1", _set_rec(1, "dato_del_nivel", _DATO_K1),
     "el dato de la llegada del 10 va en el registro k=0"),
    ("registro k=0 sin el dato de la llegada del 10", _set_rec(0, "dato_del_nivel", None),
     "el registro k=0 no tiene dato_del_nivel.clave para la llegada del 10"),
    # Textos del bloque map.* de facts.csv (M1.1t).
    ("dato de 91 caracteres", _set_dato(5, "texto", _TEXT_91), "map.stop.manaos.dato_02: 91 caracteres y el maximo es 90"),
    ("dato sin texto", _set_dato(5, "texto", ""), "map.stop.manaos.dato_02: clave sin texto es-419"),
    ("dato sin el campo texto", _drop_dato_field(5, "texto"), "map.stop.manaos.dato_02: clave sin texto es-419"),
    ("cifra con separador de miles", _set_dato(5, "texto", "Manaos esta a unos 1.480 km del mar por el rio."),
     "la cifra '1.480' lleva separador de miles o decimales"),
    ("cifra con decimales", _set_dato(5, "texto", "El agua del Negro tiene un pH de 4,5 en esta orilla."),
     "la cifra '4,5' lleva separador"),
    ("Belen en un dato", _set_dato(15, "texto", "En la creciente, el barrio de Belén queda sobre el agua."),
     "map.stop.iquitos.dato_01: 'En la creciente, el barrio de Belen"),
    ("Belen en un nombre", _set_rec(15, "nombre", "Belén (Iquitos)"), "map.stop.itaya.name"),
    ("nombre de 33 caracteres", _set_rec(3, "nombre", "Floresta Nacional do Tapajós (PA)"),
     "map.stop.flona_tapajos.name: 33 caracteres y el maximo es 32"),
    ("rio de 33 caracteres", _set_rec(2, "rio", "Tapajós, frente a la Ilha do Amor"),
     "map.stop.alter_do_chao.river: 33 caracteres y el maximo es 32"),
    ("override de nombre vacio", _override_field("nauta", "name", " "), "map.stop.nauta.name: clave sin texto"),
    ("pregunta de 51 caracteres", _set_question(1, "¿Por qué el agua del Tapajós es tan clara como esa?"),
     "map.world.w02.question: 51 caracteres y el maximo es 50"),
    ("dato [V] sin fuente [V]", _set_dato(8, "fuente", "wp_tipos"), "map.country.co.dato_01: dato [V] sin ninguna fuente [V]"),
    ("dato [NV]", _set_dato(2, "etiqueta", "NV"), "un dato del Mapa es [V] o [E]"),
    ("dato sin fuente", _set_dato(2, "fuente", ""), "map.stop.alter_do_chao.dato_01: dato sin fuente"),
    ("fuente que no existe", _set_dato(2, "fuente", "MR-F19, MR-F999"), "la fuente 'MR-F999' no esta"),
    ("fuente sin URL", _set_source("MR-F19", "url", None), "la fuente 'MR-F19' no tiene URL"),
    ("marcador sin su cifra en args", _set_dato(5, "texto", "Manaos esta a unos {n} km del mar."),
     "los marcadores ['n'] y args [] no coinciden"),
    ("args sin su marcador", _set_dato(5, "args", {"n": 1480}), "los marcadores [] y args ['n'] no coinciden"),
    ("args con un decimal sin comillas", _set_dato_with_args(5, "El Negro baja con un pH de {ph}.", {"ph": 4.5}),
     "no es un entero ni un decimal con punto"),
    ("pais sin rotulo en la investigacion", _drop_rotulo("Perú"), "map.country.pe.name: 'Peru' no es ningun rotulo"),
    ("pais sin entrada en COUNTRY_NAMES", _unknown_country, "map.country.xx.name: el pais XX no esta en COUNTRY_NAMES"),
    # Rutas y «Otro dato» (M2.6).
    ("candidato de la propuesta §5.1 bis ausente", _drop_otro(7, "map.stop.tefe.dato_03"),
     "falta el candidato de la propuesta sec. 5.1 bis map.stop.tefe.dato_03"),
    ("parada desplazada 20 km", _shift_stop(14, 0.18), "parada n=14 ('iquitos'): el sitio queda a 20."),
    ("boca de un rio fuera de su rio", _shift_mouth("xingu", 0.3), "linea xingu: su boca queda a"),
    ("Otro dato de 91 caracteres", _set_otro(5, 0, "texto", _TEXT_91), "map.stop.manaos.dato_03: 91 caracteres"),
    ("Otro dato con una fuente que no existe", _set_otro(0, 1, "fuente", "CR-F999"), "map.stop.belem.dato_02: la fuente"),
    # Belen en el contexto (M2.6: por la nota de un «Otro dato»; el titulo de una fuente con Belen se omite y queda su id
    # y su URL, FactsSourceTitleTests).
    ("Belen en la nota de un Otro dato (sale en el contexto)", _set_otro(15, 0, "nota", "Las casas flotantes de Belén."),
     "map.stop.itaya.dato_01: la columna context cae en el veto de Belen"),
    # {rio} de map_arrived: solo el nombre del rio (overrides.json lo corrige donde la investigacion da una descripcion).
    ("rio con una descripcion entre parentesis", _drop_override("belem", "river"),
     "map.stop.belem.river: 'Para (estuario)' lleva una descripcion entre parentesis"),
    ("rio que es una descripcion en minuscula", _drop_override("tarapoto", "river"),
     "map.stop.tarapoto.river: 'lagos de la varzea del Amazonas' empieza por minuscula"),
    ("rio con la palabra rio", _override_field("tefe", "river", "Río Tefé"),
     "map.stop.tefe.river: 'Rio Tefe' empieza por la palabra"),
    # args con las reglas de LocArgs.TryParse: lo que el cliente rechazara dejaria toda facts.csv sin cargar.
    ("args con 10 decimales", _set_dato_with_args(5, "El Negro baja con un pH de {ph}.", {"ph": "4.1234567891"}),
     "el valor de 'ph' (4.1234567891) lleva 10 decimales y LocArgs admite 9"),
    ("args con un entero fuera de long", _set_dato_with_args(5, "Unos {n} litros.", {"n": 9223372036854775808}),
     "el valor de 'n' (9223372036854775808) es un entero fuera del rango de long"),
    ("args con un entero negativo fuera de long", _set_dato_with_args(5, "Unos {n} litros.", {"n": "-9223372036854775809"}),
     "es un entero fuera del rango de long"),
    ("args con mas de 3 argumentos",
     _set_dato_with_args(5, "De {a} a {b}, de {c} a {d}.", {"a": 1, "b": 2, "c": 3, "d": 4}),
     "map.stop.manaos.dato_02: 4 argumentos y la tabla admite 3"),
    ("args con el nombre reservado sep", _set_dato_with_args(5, "Uno {sep} dos.", {"sep": 1}),
     "args lleva el nombre 'sep', que no vale como marcador"),
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

    def test_truncated_source_reports_the_world_left_without_close(self):
        # Sin las paradas 18 y 19, el mundo 5 se quedaria sin su close: falla por la cobertura de paradas_1_200 y por
        # el final del documento, y no escribe nada.
        src = copy.deepcopy(self.source)
        src["paradas_1_200"] = [r for r in src["paradas_1_200"] if r["k"] < 18]
        with tempfile.TemporaryDirectory() as tmp:
            src_path = Path(tmp) / "rio.json"
            out = Path(tmp) / "river_stops_v1.json"
            src_path.write_text(json.dumps(src, ensure_ascii=False), encoding="utf-8")
            proc = run_stops("--source", str(src_path), "--out", str(out))
            text = proc.stdout.decode("ascii", "replace")
            self.assertEqual(1, proc.returncode, text)
            self.assertIn("faltan las paradas k=[18, 19]", text)
            self.assertIn("el documento termina en el nivel 180 y el mundo 5 acaba en el 200", text)
            self.assertFalse(out.exists(), "con errores no se escribe nada")

    def test_validate_requires_the_document_to_end_a_world(self):
        # Regla generica (vale tambien para 1-700): la ultima parada cierra un mundo.
        wt = WorldTable.from_cs()
        doc, errors = stops.build(copy.deepcopy(self.source), copy.deepcopy(self.overrides), wt)
        self.assertEqual([], errors)
        for last_n, ok in ((19, True), (18, False), (14, False), (13, True), (7, True), (5, False), (0, True)):
            with self.subTest(last_n=last_n):
                cut = dict(doc, stops=doc["stops"][:last_n + 1])
                ending = [e for e in stops.validate(cut, wt) if "el documento termina" in e]
                self.assertEqual(ok, not ending, ending)

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


# ------------------------------------------------------------------------------ textos del Mapa en facts.csv (M1.1t)

def facts_rows(data: bytes) -> list[list[str]]:
    """Filas de facts.csv (cabecera incluida) con el lector RFC 4180 de riverdata."""
    return [parse_csv_record(r) for r in split_csv_records(data.decode("utf-8"))]


def without_block(data: bytes) -> bytes:
    """facts.csv sin las filas map.* (lo que habia antes de M1.1t)."""
    records = split_csv_records(data.decode("utf-8"))
    kept = [records[0]] + [r for r in records[1:] if not parse_csv_record(r)[0].startswith("map.")]
    return "".join(kept).encode("utf-8")


class TextBlockTests(unittest.TestCase):
    """El bloque map.* que escribe stops.py --write-texts en facts.csv: contenido, columnas, orden y args."""

    HEADER = ["key", "es-419", "en", "pt-BR", "context", "max_chars", "status", "status_en", "status_pt-BR", "notes",
              "args"]

    @classmethod
    def setUpClass(cls):
        cls.data = FACTS_PATH.read_bytes()
        cls.rows = facts_rows(cls.data)
        cls.header = cls.rows[0]
        cls.table = {r[0]: dict(zip(cls.header, r)) for r in cls.rows[1:]}
        cls.doc = json.loads(STOPS_PATH.read_text(encoding="utf-8"))
        cls.source = load_json(SOURCE_PATH)

    def test_header_of_l2b_utf8_without_bom_with_lf(self):
        self.assertEqual(self.HEADER, self.header)
        self.assertFalse(self.data.startswith(b"\xef\xbb\xbf"))
        self.assertNotIn(b"\r", self.data)
        self.assertTrue(self.data.endswith(b"\n"))

    def test_every_key_cited_by_the_stops_has_a_row_with_text(self):
        cited = stops.cited_keys(self.doc)
        self.assertEqual(80, len(cited), "67 de M1.1t y 13 «Otro dato» de M2.6")
        for key in cited:
            with self.subTest(key=key):
                self.assertIn(key, self.table)
                self.assertTrue(self.table[key]["es-419"].strip())
        block = [k for k in self.table if k.startswith("map.")]
        river_map = json.loads(MAP_PATH.read_text(encoding="utf-8"))
        map_keys = ({f"map.stop.{p['id']}.name" for p in river_map["passing"] + river_map["viewpoints"]}
                    | {f"map.country.{lab['country'].lower()}.name" for lab in river_map["labels"]})
        self.assertEqual(26, len(map_keys - set(cited)), "17 puntos de paso, 3 miradores y 6 paises solo de los rotulos")
        self.assertEqual(sorted(set(cited) | map_keys), sorted(block),
                         "el bloque tiene justo las claves que citan river_stops_v1.json y river_map_v1.json")

    def test_rows_are_ordinal_and_the_block_is_contiguous(self):
        keys = [r[0] for r in self.rows[1:]]
        self.assertEqual(sorted(keys), keys, "facts.csv en orden ordinal (sorted() = StringComparer.Ordinal)")
        idx = [i for i, k in enumerate(keys) if k.startswith("map.")]
        self.assertEqual(list(range(idx[0], idx[-1] + 1)), idx)
        # El orden es por codigo: «.» (U+002E) va antes que «_» (U+005F) y que las letras.
        self.assertLess(keys.index("map.stop.leticia.name"), keys.index("map.stop.leticia_atardecer.name"))

    def test_columns_of_each_row(self):
        for key, row in self.table.items():
            if not key.startswith("map."):
                continue
            with self.subTest(key=key):
                self.assertEqual("", row["en"])
                self.assertEqual("", row["pt-BR"])
                self.assertEqual("", row["status_en"])
                self.assertEqual("", row["status_pt-BR"])
                self.assertEqual("pendiente_asesor", row["status"])
                self.assertRegex(row["notes"], r"^\[(V|E|P)\] ")
                if re.search(r"\[(V|E)\]", row["notes"]):
                    # 08 §4.5: cada etiqueta [V] o [E] lleva a una fuente con URL.
                    self.assertRegex(row["context"], r"https?://", "fila [V] o [E] sin URL en su contexto")
                if not (".dato_" in key or key.startswith("map.general.")):
                    self.assertTrue(row["notes"].startswith("[P] "), "nombres, rios, paises y preguntas sin [V] ni [E]")
                self.assertIn("no se edita a mano", row["notes"])
                self.assertTrue(row["context"])
                expected_max = ("90" if ".dato_" in key or key.startswith("map.general.") else
                                "50" if key.endswith(".question") else "32")
                self.assertEqual(expected_max, row["max_chars"])
                self.assertLessEqual(len(row["es-419"]), int(row["max_chars"]))
                self.assertIsNone(stops.RE_NUMBER_SEP.search(row["es-419"]))
                self.assertEqual("", row["args"], "1-200 no tiene cifras con separador")

    def test_facts_carry_their_source_url_and_mark(self):
        records = {r["k"]: r for r in self.source["paradas_1_200"]}
        for s in self.doc["stops"]:
            if s["arrival_fact"] is None:
                continue
            row = self.table[s["arrival_fact"]]
            dato = records[0 if s["n"] == 1 else s["n"]]["dato_del_nivel"]
            with self.subTest(key=s["arrival_fact"]):
                self.assertEqual(stops.clean_text(dato["texto"]), row["es-419"])
                self.assertTrue(row["notes"].startswith(f"[{dato['etiqueta']}] "))
                self.assertIn("https://", row["context"])
                for sid in dato["fuente"].split(","):
                    self.assertIn(sid.strip(), row["context"])
                self.assertIn(f"victoria del {10 * s['n']}", row["context"])

    def test_names_rivers_countries_and_questions(self):
        t = {k: v["es-419"] for k, v in self.table.items()}
        self.assertEqual("Belém", t["map.stop.belem.name"])
        self.assertEqual("Pará", t["map.stop.belem.river"], "overrides.json: sin «(estuario)» en «río {río}»")
        self.assertEqual("Alter do Chão", t["map.stop.alter_do_chao.name"])
        self.assertEqual("Tapajós", t["map.stop.alter_do_chao.river"])
        self.assertEqual("El río frontera", t["map.stop.rio_frontera.name"], "07 §2.1, por overrides.json")
        self.assertEqual("Colombia y Perú", t["map.stop.rio_frontera.country"], "pais CO/PE")
        self.assertEqual("Nauta", t["map.stop.nauta.name"], "07 §2.1, por overrides.json")
        self.assertEqual("Tefé", t["map.stop.tefe.river"])
        self.assertEqual("Amazonas", t["map.stop.tarapoto.river"])
        self.assertEqual("Marañón y Ucayali", t["map.stop.pacaya_samiria.river"])
        self.assertEqual({"map.country.bo.name": "Bolivia", "map.country.br.name": "Brasil", "map.country.co.name": "Colombia",
                          "map.country.ec.name": "Ecuador", "map.country.gf.name": "Guayana Francesa",
                          "map.country.gy.name": "Guyana", "map.country.pe.name": "Perú", "map.country.sr.name": "Surinam",
                          "map.country.ve.name": "Venezuela"},
                         {k: t[k] for k in t if k.startswith("map.country.") and k.endswith(".name")},
                         "los de las paradas y los de los rotulos del Mapa (M2.6)")
        self.assertEqual("Breves", t["map.stop.breves.name"], "sin el parentesis final")
        self.assertEqual("Boca del Madeira", t["map.stop.boca_del_madeira.name"], "con mayuscula inicial")
        self.assertEqual("Óbidos", t["map.stop.obidos.name"])
        self.assertEqual("Isla de Marajó", t["map.stop.isla_de_marajo.name"])
        self.assertEqual("¿Por qué el agua del Tapajós es clara?", t["map.world.w02.question"])
        self.assertEqual(4, sum(k.startswith("map.world.") for k in t))
        self.assertEqual(32, sum(".dato_" in k or k.startswith("map.general.") for k in t if k.startswith("map.")),
                         "19 datos de llegada y 13 «Otro dato»")

    def test_rivers_are_only_the_name_of_the_river(self):
        for key, row in self.table.items():
            if key.startswith("map.stop.") and key.endswith(".river"):
                with self.subTest(key=key):
                    self.assertIsNone(stops.river_problem(row["es-419"]), row["es-419"])
        two = self.table["map.stop.pacaya_samiria.river"]["context"]
        self.assertIn("pendiente del dueño y del asesor", two, "dos rios en «rio {rio}»: pregunta abierta")
        self.assertNotIn("pendiente del dueño", self.table["map.stop.manaos.river"]["context"])

    def test_arrival_context_says_who_presents_the_fact_and_where(self):
        ctx = self.table["map.stop.manaos.dato_02"]["context"]
        self.assertIn("TiTino", ctx)
        self.assertIn("placa del Dato del río, con su retrato", ctx)
        self.assertIn("D-013, punto 6", ctx)
        first = self.table["map.stop.estuario.dato_03"]["context"]
        self.assertIn("no sale bajo map_arrived", first)
        self.assertIn("06 §2.3", first)
        self.assertIn("TiTino se presenta", first)

    def test_each_arrival_uses_the_fact_of_its_own_stop(self):
        # FX4 (observacion 4 de la prueba de la ola 2 en el telefono): la llegada al Encuentro de las Aguas (parada 4, la
        # victoria del 40) usa map.stop.manaos.dato_01. No es un error de clave: en map.stop.<lugar>.dato_NN, <lugar> es el
        # lugar del grupo de datos de la investigacion, no el id de la parada (glosario §10 y 08 §4.7 distinguen
        # map.stop.<id>.name de map.stop.<lugar>.dato_NN), y la propuesta del Mapa (§4, fila k = 4) asigna manaos.dato_01 al
        # Encuentro. El dato de llegada de cada parada es el dato_del_nivel de su propio registro (el de k = 0 en n = 1), su
        # texto habla de ella («Lugar del dato» en el contexto) y ninguna clave se repite ni sale tambien como «Otro dato».
        records = {r["k"]: r for r in self.source["paradas_1_200"]}
        overrides = load_json(OVERRIDES_PATH)["stops"]  # el nombre de la placa, si overrides.json lo cambia (n = 13 y 17)
        names = {r["k"]: overrides.get(r["id"], {}).get("name", r["nombre"]) for r in self.source["paradas_1_200"]}
        seen = {}
        for s in self.doc["stops"]:
            key = s["arrival_fact"]
            if s["n"] == 0:
                self.assertIsNone(key)
                continue
            holder = 0 if s["n"] == 1 else s["n"]
            with self.subTest(n=s["n"]):
                self.assertEqual(records[holder]["dato_del_nivel"]["clave"], key, "el dato de su propio registro")
                self.assertIn(f"Lugar del dato: {names[holder]}.", self.table[key]["context"])
                self.assertNotIn(key, seen, f"repetida con la parada {seen.get(key)}")
                self.assertNotIn(key, {k for t in self.doc["stops"] for k in t["facts"]}, "no es un «Otro dato»")
                seen[key] = s["n"]
        # Las llegadas cuyo <lugar> no es el id de su parada, una a una: una nueva tiene que mirarse y anotarse aqui.
        lugar = {}
        for s in self.doc["stops"]:
            key = s["arrival_fact"]
            if key is not None and not key.startswith(f"map.stop.{s['id']}."):
                lugar[s["n"]] = key
        self.assertEqual({
            1: "map.stop.estuario.dato_03",       # el Mapa del 10 habla del estuario (06 §2.3)
            4: "map.stop.manaos.dato_01",         # el Encuentro, en el grupo de Manaos (propuesta del Mapa §4, k = 4)
            6: "map.general.dato_negra",          # dato general del agua negra (Anavilhanas)
            8: "map.country.co.dato_01",          # dato de pais: Colombia
            9: "map.stop.leticia.dato_02",        # Leticia al atardecer, en el grupo de Leticia
            14: "map.country.pe.dato_01",         # dato de pais: Peru
            15: "map.stop.iquitos.dato_01",       # el rio Itaya, en el grupo de Iquitos
            16: "map.stop.allpahuayo.dato_01",    # Allpahuayo-Mishana, con el lugar abreviado
            19: "map.stop.pacaya_samiria.dato_02",  # Pacaya-Samiria al alba, en el grupo de Pacaya-Samiria
        }, lugar)
        encuentro = self.table["map.stop.manaos.dato_01"]
        self.assertIn("Lugar del dato: Encuentro de las Aguas.", encuentro["context"])
        self.assertIn("parada 4 del Mapa del río", encuentro["context"])
        self.assertIn("sin mezclarse", encuentro["es-419"])
        self.assertIn("Otro dato» de la tarjeta de la parada 4", self.table["map.stop.encontro.dato_01"]["context"])

    def test_titino_milestones_move_the_arrival_fact_to_the_next_victory(self):
        # D-022, punto 1 (pregunta 45 (a); T6-A): en el fin de mundo salvo el 10 (40, 80 y 140), el primer Difícil (40) y
        # el primer Muy difícil (60) habla TiTino y el dato de llegada sale en la victoria siguiente.
        moved = {
            "map.stop.manaos.dato_01": (40, "fin del mundo 2 y primer Difícil"),
            "map.general.dato_negra": (60, "primer Muy difícil"),
            "map.country.co.dato_01": (80, "fin del mundo 3"),
            "map.country.pe.dato_01": (140, "fin del mundo 4"),
        }
        for key, (level, why) in moved.items():
            with self.subTest(key=key):
                ctx = self.table[key]["context"]
                self.assertIn(f"La victoria del {level} es un hito de TiTino ({why})", ctx)
                self.assertIn(f"sale en la victoria siguiente, la del {level + 1}", ctx)
                self.assertIn("D-022, punto 1", ctx)
                self.assertIn(f"si TiTino no puede hablar en la del {level}, sale en ella", ctx)
        for s in self.doc["stops"]:
            key = s["arrival_fact"]
            if key is None or key in moved:
                continue
            with self.subTest(key=key):
                self.assertNotIn("hito de TiTino", self.table[key]["context"])
                self.assertNotIn("D-022", self.table[key]["context"])
        q3 = self.table["map.world.w03.question"]["context"]
        self.assertIn("(map.general.dato_negra), que sale en la victoria del 61", q3)
        self.assertIn("D-022, punto 1", q3)
        self.assertNotIn("en la victoria del 60", q3)
        q2 = self.table["map.world.w02.question"]["context"]
        self.assertIn("(map.stop.flona_tapajos.dato_01), en la victoria del 30", q2)
        self.assertNotIn("D-022", q2)

    def test_country_context_names_every_place_and_skips_the_two_country_stop(self):
        co = self.table["map.country.co.name"]["context"]
        self.assertIn("en las paradas 8-12 del Mapa del río (la 13, El río frontera, usa "
                      "map.stop.rio_frontera.country)", co)
        self.assertNotIn("8-13", co)
        self.assertIn("en las paradas 0-7 del Mapa del río;", self.table["map.country.br.name"]["context"])
        self.assertIn("en las paradas 14-19 del Mapa del río;", self.table["map.country.pe.name"]["context"])
        for iso in ("br", "co", "pe"):
            with self.subTest(iso=iso):
                ctx = self.table[f"map.country.{iso}.name"]["context"]
                self.assertIn("map_country_welcome («{país} te da la bienvenida»", ctx)
                self.assertIn("«O Brasil»", ctx)
        self.assertEqual("0-2 y 5", stops.stop_ranges([5, 0, 2, 1]))
        self.assertEqual("13", stops.stop_ranges([13]))
        self.assertEqual("1, 3 y 5-6", stops.stop_ranges([1, 3, 5, 6]))

    def test_stop_name_context_names_every_place(self):
        # map_hito_preview solo en las paradas a las que llega un hito p20 desde el primer Difícil (40, 60, ... 180).
        hito_stops = {4, 6, 8, 10, 12, 14, 16, 18}
        for s in self.doc["stops"]:
            with self.subTest(n=s["n"]):
                ctx = self.table[f"map.stop.{s['id']}.name"]["context"]
                self.assertIn("map_arrived", ctx)
                self.assertIn("stop_label («{lugar} · {país}»", ctx)
                self.assertIn("map_stop_remaining", ctx)
                self.assertIn("«Canasta · {sitio} k/10»", ctx)
                if s["n"] in hito_stops:
                    self.assertIn(f"map_hito_preview («Si lo superas, llegas a {{lugar}}»), en la tarjeta previa del "
                                  f"nivel {10 * s['n']},", ctx)
                else:
                    self.assertNotIn("map_hito_preview", ctx)

    def test_other_facts_of_the_cards(self):
        # «Otro dato» de la tarjeta (M2.6 con los de M2.7): texto, etiqueta, fuentes con URL y donde sale.
        t = self.table
        self.assertEqual(13, sum(1 for k in t if "Otro dato» de la tarjeta" in t[k]["context"]))
        row = t["map.stop.tefe.dato_03"]
        self.assertEqual("Cuando el agua baja, los manatíes de Mamirauá viajan a un refugio: el lago Amanã.", row["es-419"])
        self.assertTrue(row["notes"].startswith("[V] «Otro dato» del Mapa del río."))
        self.assertIn("map_card_other_fact", row["context"])
        self.assertIn("https://", row["context"])
        self.assertEqual("90", row["max_chars"])
        self.assertEqual("pendiente_asesor", row["status"])
        self.assertIn("5-6 m", t["map.stop.itaya.dato_01"]["es-419"], "U+00A0 entre la cifra y la unidad")

    def test_a_source_titled_with_belen_is_cited_by_id_and_url_only(self):
        # El veto de D-013 (punto 11) vale para todas las columnas: CR-F28 es la pagina del distrito (su titulo lleva el
        # nombre vetado), asi que el contexto de itaya.dato_01 la cita por su id y su URL.
        context = self.table["map.stop.itaya.dato_01"]["context"]
        self.assertIn("CR-F28 [E]: título omitido por el veto de D-013, punto 11, https://", context)
        self.assertNotIn("belen", stops.ascii_text(context).lower())
        fuentes = {"X1": {"id": "X1", "nombre": "Mercado de Belén", "url": "https://example.org/x1", "etiqueta": "E"}}
        text, errors = stops._sources_text({"etiqueta": "E", "fuente": "X1"}, fuentes, "map.stop.x.dato_01")
        self.assertEqual([], errors)
        self.assertEqual("Fuente X1 [E]: título omitido por el veto de D-013, punto 11, https://example.org/x1.", text)

    def test_unit_gets_a_no_break_space(self):
        self.assertIn("6\u00a0km", self.table["map.stop.manaos.dato_01"]["es-419"])
        self.assertIn("42\u00a0%", self.table["map.country.co.dato_01"]["es-419"])
        self.assertEqual("2 millones de hectáreas", stops.clean_text("2 millones de hectáreas"))
        self.assertEqual("{n}\u00a0km y 35\u00a0m", stops.clean_text("{n} km y 35 m"))
        self.assertEqual("5 metros", stops.clean_text("5 metros"))

    def test_no_belen_anywhere_in_the_block(self):
        from riverdata import has_belen
        for key, row in self.table.items():
            if key.startswith("map."):
                for col, value in row.items():
                    self.assertFalse(has_belen(value), f"{key} ({col})")


class WriteTextsTests(unittest.TestCase):
    """--write-texts: determinista, solo toca el bloque map.* y lo coloca en su sitio ordinal."""

    def write(self, data: bytes, *extra: str, env: dict | None = None) -> tuple[subprocess.CompletedProcess, bytes]:
        with tempfile.TemporaryDirectory() as tmp:
            facts = Path(tmp) / "facts.csv"
            facts.write_bytes(data)
            proc = run_stops("--write-texts", "--facts", str(facts), *extra, env=env)
            return proc, facts.read_bytes()

    def test_regenerating_produces_no_diff_with_any_hash_seed(self):
        committed = FACTS_PATH.read_bytes()
        for seed in ("0", "1", "4242"):
            with self.subTest(seed=seed):
                proc, out = self.write(committed, env=dict(os.environ, PYTHONHASHSEED=seed))
                self.assertEqual(0, proc.returncode, proc.stdout.decode("ascii", "replace"))
                self.assertTrue(is_ascii(proc.stdout + proc.stderr))
                self.assertEqual(committed, out)

    def test_from_the_table_without_the_block_gives_the_committed_file(self):
        committed = FACTS_PATH.read_bytes()
        proc, out = self.write(without_block(committed))
        self.assertEqual(0, proc.returncode, proc.stdout.decode("ascii", "replace"))
        self.assertEqual(committed, out)

    def test_rows_outside_the_block_do_not_change(self):
        # Filas ajenas antes y despues del bloque en orden ordinal (una de fruta, con puntos, antes de «map.» y una de
        # tinte despues), con comillas, comas y un salto de linea dentro de un campo: quedan byte a byte. Las claves extra no
        # estan en la tabla: desde F1F2 ya hay filas fact.* y tint.* de Frutas del rio a los dos lados del bloque.
        committed = FACTS_PATH.read_bytes().decode("utf-8")
        records = split_csv_records(committed)
        extra_before = 'fact.aaa.01,"Uno, dos",,,"Linea con\nsalto y ""comillas""",90,pendiente_asesor,,,x,\n'
        extra_after = "tint.zzz.name,Açaí,,,ctx,16,pendiente_asesor,,,x,\n"
        others = [r for r in records[1:] if not parse_csv_record(r)[0].startswith("map.")]
        block = [r for r in records[1:] if parse_csv_record(r)[0].startswith("map.")]
        before = [r for r in others if parse_csv_record(r)[0] < "map."]
        after = [r for r in others if parse_csv_record(r)[0] > "map."]
        source = "".join([records[0], extra_before] + others + [extra_after])
        proc, out = self.write(source.encode("utf-8"))
        self.assertEqual(0, proc.returncode, proc.stdout.decode("ascii", "replace"))
        expected = "".join([records[0], extra_before] + before + block + after + [extra_after])
        self.assertEqual(expected, out.decode("utf-8"))
        keys = [r[0] for r in facts_rows(out)[1:]]
        self.assertEqual(sorted(keys), keys)

    def test_a_fact_with_args_writes_n_and_its_value(self):
        src = load_json(SOURCE_PATH)
        _rec(src, 5)["dato_del_nivel"].update(texto="Manaos está a unos {n} km del mar por el río.", args={"n": 1480})
        with tempfile.TemporaryDirectory() as tmp:
            src_path = Path(tmp) / "rio.json"
            src_path.write_text(json.dumps(src, ensure_ascii=False), encoding="utf-8")
            proc, out = self.write(FACTS_PATH.read_bytes(), "--source", str(src_path))
        self.assertEqual(0, proc.returncode, proc.stdout.decode("ascii", "replace"))
        row = {r[0]: r for r in facts_rows(out)}["map.stop.manaos.dato_02"]
        self.assertEqual("Manaos está a unos {n}\u00a0km del mar por el río.", row[1])
        self.assertEqual("n=1480", row[-1])
        self.assertIn("{n}", row[4], "el contexto explica el marcador")
        self.assertEqual("n=3.25;x=7", stops.format_args({"x": "7", "n": "3.25"}))
        self.assertEqual("1.480", stops._shown_number("1480"))
        self.assertEqual("1.234.567,5", stops._shown_number("1234567.5"))
        # La cifra cuenta como se ve: «{n}» con 1480 ocupa 5 («1.480»), y nunca menos de lo escrito.
        self.assertEqual(len("a 1.480 km"), stops.visible_length("a {n} km", {"n": "1480"}))
        self.assertEqual(len("a {n} km"), stops.visible_length("a {n} km", {"n": "5"}))

    def test_writing_refuses_an_invalid_source_and_leaves_the_table(self):
        src = load_json(SOURCE_PATH)
        _rec(src, 5)["dato_del_nivel"]["texto"] = "Manaos está a unos 1.480 km del mar."
        committed = FACTS_PATH.read_bytes()
        with tempfile.TemporaryDirectory() as tmp:
            src_path = Path(tmp) / "rio.json"
            src_path.write_text(json.dumps(src, ensure_ascii=False), encoding="utf-8")
            proc, out = self.write(committed, "--source", str(src_path))
        self.assertEqual(1, proc.returncode)
        self.assertEqual(committed, out, "con errores no se escribe nada")

    def test_args_follow_locargs_tryparse(self):
        ok, errors = stops._parse_args({"a": "4.123456789", "b": stops.ARG_INT_MAX, "c": str(stops.ARG_INT_MIN)}, "k")
        self.assertEqual([], errors)
        self.assertEqual({"a": "4.123456789", "b": "9223372036854775807", "c": "-9223372036854775808"}, ok)
        for raw, fragment in (({"a": "4.1234567891"}, "10 decimales"), ({"a": stops.ARG_INT_MAX + 1}, "fuera del rango"),
                              ({"a": str(stops.ARG_INT_MIN - 1)}, "fuera del rango"), ({"sep": 1}, "'sep'"),
                              ({"a": 1, "b": 2, "c": 3, "d": 4}, "4 argumentos"), ({"a": True}, "no es un entero"),
                              ({"A": 1}, "'A'")):
            with self.subTest(raw=raw):
                _, errors = stops._parse_args(raw, "k")
                self.assertTrue(any(fragment in e for e in errors), errors)

    def test_a_tag_without_url_fails(self):
        # Si un dato [V] o [E] llegara sin URL en el contexto, build_texts falla (08 §4.5).
        from unittest import mock
        source, overrides, wt = load_json(SOURCE_PATH), load_json(OVERRIDES_PATH), WorldTable.from_cs()
        doc, errors = stops.build(source, overrides, wt)
        self.assertEqual([], errors)
        with mock.patch.object(stops, "_sources_text", lambda dato, fuentes, key: ("Fuente sin enlace.", [])):
            _, errors = stops.build_texts(doc, source, overrides)
        self.assertTrue(errors)
        self.assertTrue(all("no cita ninguna fuente con URL" in e for e in errors), errors)
        self.assertEqual(32, len(errors), "uno por dato de llegada y otro por «Otro dato»")

    def test_csv_helpers_round_trip(self):
        from riverdata import format_csv_row
        text = 'a,b\nk,"x, y"\nq,"uno\ndos ""tres"""\nz,\n'
        records = split_csv_records(text)
        self.assertEqual(4, len(records))
        self.assertEqual(text, "".join(records))
        self.assertEqual(["q", 'uno\ndos "tres"'], parse_csv_record(records[2]))
        self.assertEqual('q,"uno\ndos ""tres"""\n', format_csv_row(["q", 'uno\ndos "tres"']))
        self.assertEqual("k,v,,\n", format_csv_row(["k", "v", "", ""]))


class FactsCheckTests(unittest.TestCase):
    """--check sobre facts.csv: desfase del bloque, claves sin fila o sin texto, bloque partido."""

    def check(self, data: bytes) -> subprocess.CompletedProcess:
        with tempfile.TemporaryDirectory() as tmp:
            facts = Path(tmp) / "facts.csv"
            facts.write_bytes(data)
            return run_stops("--check", "--facts", str(facts))

    def assertFails(self, data: bytes, *fragments: str):
        proc = self.check(data)
        out = proc.stdout.decode("ascii", "replace")
        self.assertEqual(1, proc.returncode, out)
        self.assertTrue(is_ascii(proc.stdout + proc.stderr), out)
        for fragment in fragments:
            self.assertIn(fragment, out)

    def test_check_passes_on_the_committed_table(self):
        proc = self.check(FACTS_PATH.read_bytes())
        self.assertEqual(0, proc.returncode, proc.stdout.decode("ascii", "replace"))
        self.assertIn(b"106 textos map.* validos", proc.stdout)

    def test_edited_row_is_drift(self):
        # Solo en la fila del Mapa: desde F1F2, la linea 14 del Agua del Tapajos (fact.*, antes del bloque) tambien la nombra.
        records = split_csv_records(FACTS_PATH.read_bytes().decode("utf-8"))
        data = "".join(r.replace("Ilha do Amor", "Isla del Amor", 1) if r.startswith("map.stop.alter_do_chao.dato_01,") else r
                       for r in records).encode("utf-8")
        self.assertFails(data, "desfase", "map.stop.alter_do_chao.dato_01", "--write-texts")

    def test_crlf_is_drift(self):
        self.assertFails(FACTS_PATH.read_bytes().replace(b"\n", b"\r\n"), "desfase")

    def test_missing_row_of_a_cited_key(self):
        records = split_csv_records(FACTS_PATH.read_bytes().decode("utf-8"))
        data = "".join(r for r in records if not r.startswith("map.stop.nauta.dato_01,")).encode("utf-8")
        self.assertFails(data, "map.stop.nauta.dato_01: citada en river_stops_v1.json y sin fila", "desfase")

    def test_row_without_text(self):
        old = "map.world.w02.question,¿Por qué el agua del Tapajós es clara?,".encode("utf-8")
        data = FACTS_PATH.read_bytes().replace(old, b"map.world.w02.question,,", 1)
        self.assertFails(data, "map.world.w02.question: citada en river_stops_v1.json y sin texto es-419")

    def test_missing_block(self):
        self.assertFails(without_block(FACTS_PATH.read_bytes()), "80 claves citadas", "falta el bloque map.*")

    def test_block_split_by_another_row(self):
        records = split_csv_records(FACTS_PATH.read_bytes().decode("utf-8"))
        fauna = next(r for r in records if r.startswith("fauna_aotus"))
        moved = [r for r in records if r is not fauna]
        at = next(i for i, r in enumerate(moved) if r.startswith("map.stop."))
        moved.insert(at, fauna)
        self.assertFails("".join(moved).encode("utf-8"), "no es contiguo")

    def test_missing_table(self):
        with tempfile.TemporaryDirectory() as tmp:
            proc = run_stops("--check", "--facts", str(Path(tmp) / "no_existe.csv"))
        self.assertEqual(1, proc.returncode)
        self.assertIn(b"falta", proc.stdout)


if __name__ == "__main__":
    unittest.main()
