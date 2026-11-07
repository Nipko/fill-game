# -*- coding: utf-8 -*-
"""test_towns.py - los pueblos de paso del Mapa v2 (D-069, bloque M2-2; propuesta del Mapa v2 §4 y §9.1).

Ejecutar desde la raiz del repositorio:
    py -3.14 -m unittest discover -s tools/river-map -p "test_*.py"

Cubre, sobre los archivos commiteados (sin las descargas): el anclaje de los candidatos a <= 5 km y su nombre verificado (P4
(a)), los que no entran (excluded), los vetos (Belen y Boca Colorado, §9.5), el pais, el tier y la vinneta de cada punto de
paso, la fraccion f de cada via y el nivel en que se pasa (§4.1, regla 3), la cadencia (§4.1, regla 2: un tramo de >= 150
km sin pueblo y sin excepcion falla; una excepcion que sobra, tambien) y la excursion local de los 25 tramos de 0 km (P5
(a); §4.4). Solo biblioteca estandar.
"""
from __future__ import annotations

import copy
import math
import sys
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

import geometry  # noqa: E402
import stops  # noqa: E402
from riverdata import MAP_PATH, OVERRIDES_PATH, SOURCE_PATH, STOPS_PATH, fold, load_json  # noqa: E402

NEW_TOWNS = ("almeirim", "prainha", "manacapuru", "codajas", "amatura", "sao_paulo_de_olivenca", "indiana", "orellana",
             "humaita", "manicore", "cachuela_esperanza", "guajara_mirim")


def pass_level(n: int, f: float) -> int:
    """Nivel en que se pasa un pueblo del tramo de la parada n (propuesta §4.1, regla 3): 10 (n - 1) + ceil(10 f)."""
    return 10 * (n - 1) + max(1, math.ceil(10 * f - 1e-9))


class TownsBase(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.source = load_json(SOURCE_PATH)
        cls.overrides = load_json(OVERRIDES_PATH)
        cls.towns = cls.overrides["towns"]
        cls.geo = geometry.committed_geometry(MAP_PATH)
        cls.doc, cls.model, errors = geometry.build_map(cls.source, cls.geo)
        assert not errors, errors
        cls.passing = {p["id"]: p for p in cls.doc["passing"]}
        cls.model_passing = {p["id"]: p for p in cls.model.passing}
        cls.stops = load_json(STOPS_PATH)
        cls.by_n = {s["n"]: s for s in cls.stops["stops"]}

    def model_errors(self, towns: dict | None = None, source: dict | None = None) -> list:
        _, errors = geometry.build_model(source or self.source, self.geo, towns if towns is not None else self.towns)
        return errors


class AnchorTests(TownsBase):
    def test_the_twelve_new_towns_are_anchored_within_5_km(self):
        self.assertEqual(5.0, geometry.TOWN_ANCHOR_KM, "P4 (a): 5 km o menos")
        for pid in NEW_TOWNS:
            with self.subTest(town=pid):
                p = self.model_passing[pid]
                self.assertLessEqual(p["anchor_km"], geometry.TOWN_ANCHOR_KM)
                self.assertIsNone(p["fact"], "ningun dato: solo [V] revisado por Gemini (D-054), y ninguno lo tiene")
        self.assertEqual(30, len(self.passing), "los 18 de la 1.0 y los 12 que entran")

    def test_a_candidate_moved_beyond_5_km_does_not_enter(self):
        src = copy.deepcopy(self.source)
        pts = next(r for r in src["rios"] if r["id"] == "amazonas")["puntos"]
        almeirim = next(p for p in pts if p["nombre"] == "Almeirim")
        almeirim["lat"] = round(almeirim["lat"] + 0.08, 4)   # ~9 km al norte del cauce
        errors = self.model_errors(source=src)
        self.assertTrue(any("towns.places.almeirim: 'Almeirim' se ancla a" in e and "maximo 5 km" in e for e in errors), errors)

    def test_an_unverified_name_does_not_enter(self):
        towns = copy.deepcopy(self.towns)
        towns["places"]["masisea"] = {"from": {"rio": "amazonas", "punto": "Masisea"}, "country": "PE", "tier": "town",
                                      "vignette": "stilts", "river": "Ucayali"}
        del towns["excluded"]["masisea"]
        errors = self.model_errors(towns)
        self.assertTrue(any("towns.places.masisea: 'Masisea' no es ningun punto de amazonas" in e for e in errors), errors)

    def test_the_excluded_ones_are_not_on_the_map(self):
        excluded = self.towns["excluded"]
        for pid in ("porto_velho", "borba", "costa_marques", "cabo_pantoja", "tiputini", "panacocha", "laberinto",
                    "masisea", "sao_gabriel_da_cachoeira", "belen", "boca_colorado"):
            with self.subTest(town=pid):
                self.assertIn(pid, excluded)
                self.assertTrue(excluded[pid].strip())
                self.assertNotIn(pid, self.passing)
        # Los tres que no se anclan a 5 km: lo dice la geometria, no solo la tabla.
        for rid, name, pid in (("madeira", "Porto Velho", "porto_velho"), ("madeira", "Borba", "borba"),
                               ("itenez", "Boca del São Domingos / Costa Marques", "costa_marques")):
            towns = copy.deepcopy(self.towns)
            del towns["excluded"][pid]
            entry = {"from": {"rio": rid, "punto": name}, "country": "BR", "tier": "town", "vignette": "stilts",
                     "river": "Madeira"}
            if pid == "costa_marques":
                entry["name"] = "Costa Marques"
            towns["places"][pid] = entry
            errors = self.model_errors(towns)
            self.assertTrue(any(f"towns.places.{pid}:" in e and "se ancla a" in e for e in errors), (pid, errors))

    def test_an_excluded_id_on_the_map_fails(self):
        towns = copy.deepcopy(self.towns)
        towns["excluded"]["coari"] = "prueba"
        errors = self.model_errors(towns)
        self.assertTrue(any("towns.places.coari: esta tambien en towns.excluded" in e for e in errors), errors)
        self.assertTrue(any("punto de paso coari: esta en towns.excluded" in e for e in errors), errors)


class VetoTests(TownsBase):
    def test_belen_and_boca_colorado_are_vetoed(self):
        self.assertEqual(("belen", "boca colorado"), geometry.TOWN_VETOES)
        for p in self.model.passing:
            self.assertNotIn("belen", fold(p["name"]))
            self.assertNotIn("boca colorado", fold(p["name"]))
        # Un candidato con el nombre de Boca Colorado: falla aunque se anclara.
        towns = copy.deepcopy(self.towns)
        towns["places"]["indiana"]["name"] = "Boca Colorado"
        errors = self.model_errors(towns)
        self.assertTrue(any("indiana" in e for e in errors), errors)
        src = copy.deepcopy(self.source)
        pts = next(r for r in src["rios"] if r["id"] == "amazonas")["puntos"]
        next(p for p in pts if p["nombre"] == "Indiana")["nombre"] = "Boca Colorado"
        towns = copy.deepcopy(self.towns)
        towns["places"]["boca_colorado"] = towns["places"].pop("indiana")
        towns["places"]["boca_colorado"]["from"]["punto"] = "Boca Colorado"
        del towns["excluded"]["boca_colorado"]
        errors = self.model_errors(towns, src)
        self.assertTrue(any("cae en el veto de 'boca colorado'" in e for e in errors), errors)

    def test_no_maloca_and_parintins_without_bois(self):
        self.assertEqual(("stilts", "church", "port", "market"), geometry.TOWN_VIGNETTES, "ninguna maloca (§4.2)")
        self.assertEqual("port", self.passing["parintins"]["vignette"], "Parintins, el puerto, sin bois (glosario §3.3)")
        self.assertIn("sin los bois", self.towns["why"])


class FieldTests(TownsBase):
    def test_every_passing_point_has_country_tier_and_vignette(self):
        for p in self.doc["passing"]:
            with self.subTest(point=p["id"]):
                self.assertEqual(["id", "line", "s", "fact", "vignette", "country", "tier"], list(p))
                self.assertIn(p["tier"], ("town", "city", "mark"))
                if p["tier"] == "mark":
                    self.assertIsNone(p["vignette"])
                else:
                    self.assertIn(p["vignette"], geometry.TOWN_VIGNETTES)
        marks = sorted(pid for pid, p in self.passing.items() if p["tier"] == "mark")
        self.assertEqual(["boca_del_madeira", "boca_del_napo", "boca_del_purus", "boca_del_xingu", "teotonio"], marks)
        self.assertEqual("BO", self.passing["cachuela_esperanza"]["country"])
        self.assertEqual("PE", self.passing["orellana"]["country"])

    def test_a_point_without_its_entry_fails(self):
        towns = copy.deepcopy(self.towns)
        del towns["places"]["coari"]
        errors = self.model_errors(towns)
        self.assertTrue(any("punto de paso coari: sin entrada en towns.places" in e for e in errors), errors)
        towns = copy.deepcopy(self.towns)
        towns["places"]["coari"]["vignette"] = "maloca"
        self.assertTrue(any("towns.places.coari: vignette 'maloca'" in e for e in self.model_errors(towns)))


class ViaTests(TownsBase):
    def test_every_via_has_its_fraction_in_order(self):
        for s in self.stops["stops"]:
            last = 0.0
            for v in s["leg"]["via"]:
                with self.subTest(stop=s["n"], via=v["id"]):
                    self.assertEqual(["id", "f"], list(v))
                    self.assertGreater(v["f"], 0.0)
                    self.assertLessEqual(v["f"], 1.0)
                    self.assertGreaterEqual(v["f"], last)
                    last = v["f"]

    def test_pass_level_follows_the_fraction(self):
        # Belém -> Santarém (el 10): Breves en el 3, Almeirim en el 7, Monte Alegre en el 9 (10 (n - 1) + ceil(10 f)).
        via = {v["id"]: v["f"] for v in self.by_n[1]["leg"]["via"]}
        self.assertEqual(3, pass_level(1, via["breves"]))
        self.assertEqual(7, pass_level(1, via["almeirim"]))
        self.assertEqual(9, pass_level(1, via["monte_alegre"]))
        # Anavilhanas -> Tefé (61-70): Manacapuru en el 64, Coari en el 68.
        via = {v["id"]: v["f"] for v in self.by_n[7]["leg"]["via"]}
        self.assertEqual(64, pass_level(7, via["manacapuru"]))
        self.assertEqual(68, pass_level(7, via["coari"]))
        self.assertEqual(1.0, geometry.town_f(10.0, 0.0))
        self.assertEqual(0.0001, geometry.town_f(0.0, 100.0), "nunca 0: el punto de partida no se cuenta")


class CadenceTests(TownsBase):
    def test_the_committed_stops_meet_the_cadence_with_the_annotated_exceptions(self):
        self.assertEqual([], geometry.town_cadence_problems(self.stops, self.model, self.towns))
        self.assertEqual(13, len(self.towns["cadence"]))
        for n, why in self.towns["cadence"].items():
            with self.subTest(leg=n):
                leg = self.by_n[int(n)]["leg"]
                self.assertGreaterEqual(geometry.path_km(self.model, leg["path"]), 150.0)
                self.assertTrue(why.strip())

    def test_a_long_leg_without_a_town_and_without_an_exception_fails(self):
        towns = copy.deepcopy(self.towns)
        del towns["cadence"]["42"]
        errors = geometry.town_cadence_problems(self.stops, self.model, towns)
        self.assertEqual(1, len(errors), errors)
        self.assertIn("parada n=42 (riberalta): su tramo mide 934 km y pasa 0 pueblos", errors[0])
        # Uno de >= 400 km con un solo pueblo pide 2 (§4.1, regla 2): el de Contamana, con Orellana.
        towns = copy.deepcopy(self.towns)
        del towns["cadence"]["59"]
        errors = geometry.town_cadence_problems(self.stops, self.model, towns)
        self.assertEqual(1, len(errors), errors)
        self.assertIn("pasa 1 pueblos", errors[0])
        self.assertIn("pide 2", errors[0])

    def test_marks_do_not_count_and_a_spare_exception_fails(self):
        stops_doc = copy.deepcopy(self.stops)
        # Sin Tamshiyacu, Nauta (176 km) solo tendria marcas: falla.
        leg = next(s for s in stops_doc["stops"] if s["n"] == 17)["leg"]
        leg["via"] = [v for v in leg["via"] if v["id"] != "tamshiyacu"]
        errors = geometry.town_cadence_problems(stops_doc, self.model, self.towns)
        self.assertTrue(any("parada n=17 (nauta)" in e for e in errors), errors)
        towns = copy.deepcopy(self.towns)
        towns["cadence"]["7"] = "sobra"
        errors = geometry.town_cadence_problems(self.stops, self.model, towns)
        self.assertEqual(["towns.cadence: la excepcion del tramo 7 sobra (ya cumple la cadencia o no existe)"], errors)


class ExcursionTests(TownsBase):
    def test_the_25_zero_legs_have_their_local_excursion(self):
        zero = [s for s in self.stops["stops"] if s["n"] > 0 and geometry.path_km(self.model, s["leg"]["path"]) == 0.0]
        self.assertEqual(25, len(zero), "P5 (a): los 25 tramos de 0 km")
        ats = [(s["n"], s["at"]) for s in self.stops["stops"]]
        for s in zero:
            with self.subTest(stop=s["id"]):
                exc = s["leg"]["excursion"]
                self.assertEqual(["line", "s0", "s1"], list(exc))
                self.assertEqual((s["at"]["line"], s["at"]["s"]), (exc["line"], exc["s0"]))
                line = self.model.lines[exc["line"]]
                km = abs(geometry._arc_at(line, exc["s1"]) - geometry._arc_at(line, exc["s0"]))
                self.assertGreaterEqual(km, geometry.EXCURSION_MIN_KM - 0.01)
                self.assertLessEqual(km, geometry.EXCURSION_KM + 0.01)
                lo, hi = sorted((exc["s0"], exc["s1"]))
                for n, at in ats:
                    if at["line"] == exc["line"] and abs(at["s"] - exc["s0"]) > geometry.S_EPS:
                        self.assertFalse(lo < at["s"] < hi, f"la excursion llega a la parada {n}")
        others = [s for s in self.stops["stops"] if s not in zero]
        self.assertTrue(all("excursion" not in s["leg"] for s in others), "solo en los tramos de 0 km")

    def test_upriver_when_there_is_room(self):
        # Pacaya al alba (190): rio arriba por el Maranon (s baja); Leticia al atardecer: rio abajo, porque Amacayacu queda a 33
        # km rio arriba (EXCURSION_GAP).
        exc = self.by_n[19]["leg"]["excursion"]
        self.assertLess(exc["s1"], exc["s0"])
        exc = self.by_n[9]["leg"]["excursion"]
        self.assertGreater(exc["s1"], exc["s0"])

    def test_check_fails_without_the_excursion(self):
        stops_doc = copy.deepcopy(self.stops)
        del next(s for s in stops_doc["stops"] if s["n"] == 32)["leg"]["excursion"]
        records = self.source["paradas_1_200"] + self.source["paradas_201_700"]
        errors = geometry.validate_routes(stops_doc, self.model, records)
        self.assertEqual(["parada n=32 ('sandoval_aguajal'): tramo de 0 km sin excursion local (P5 (a))"], errors)


class TextTests(TownsBase):
    def test_new_names_and_town_rivers_are_pending_gemini(self):
        doc, rows, errors = stops.build_all(self.source, self.overrides, stops.WorldTable.from_cs(stops.WORLDTABLE_PATH))
        self.assertEqual([], errors)
        for pid in NEW_TOWNS:
            with self.subTest(town=pid):
                row = rows[f"map.stop.{pid}.name"]
                self.assertEqual("pendiente_asesor", row["status"])
                self.assertIn("Pendiente de la revisión de Gemini (D-054)", row["notes"])
                self.assertIn("https://", row["context"], "el nombre, verificado con las fuentes del río (P4 (a))")
        self.assertEqual("Guajará-Mirim", rows["map.stop.guajara_mirim.name"]["es-419"])
        self.assertEqual("São Paulo de Olivença", rows["map.stop.sao_paulo_de_olivenca.name"]["es-419"])
        self.assertEqual("Solimões", rows["map.stop.coari.river"]["es-419"])
        self.assertNotIn("map.stop.teotonio.river", rows, "un mark no lleva rio de ficha")


if __name__ == "__main__":
    unittest.main()
