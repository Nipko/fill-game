# -*- coding: utf-8 -*-
"""test_geometry.py - pruebas de tools/river-map/geometry.py (geometria af-river-map/1 con el respaldo de la
investigacion, y at y leg de las paradas; M2.6).

Ejecutar desde la raiz del repositorio:
    py -3.14 -m unittest discover -s tools/river-map -p "test_*.py"

Cubre: la proyeccion (cinco puntos contra generar_mapa.py); el base64 de ida y vuelta (<= 0,5 px); las s de las bocas y
de las paradas (de acuerdo con los km de la investigacion); la triangulacion por orejas; el archivo commiteado
(determinista, al dia, UTF-8 sin BOM y LF); los legs continuos, que no cruzan tierra, con la ida y vuelta por las
confluencias y el del 41 por Obidos, Parintins, Itacoatiara y la boca del Madeira; y los fallos de --check: una parada
desplazada 20 km, un leg roto, una boca fuera de su rio, un candidato de la propuesta §5.1 bis ausente y el desfase.
Solo biblioteca estandar.
"""
from __future__ import annotations

import copy
import importlib.util
import json
import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

import geometry  # noqa: E402
from riverdata import (  # noqa: E402
    GENERAR_MAPA_PATH,
    MAP_PATH,
    OVERRIDES_PATH,
    ROOT,
    SOURCE_PATH,
    STOPS_PATH,
    WorldTable,
    load_json,
)

GEOMETRY_SCRIPT = HERE / "geometry.py"


def run_geometry(*args: str, env: dict | None = None) -> subprocess.CompletedProcess:
    return subprocess.run([sys.executable, str(GEOMETRY_SCRIPT), *args], cwd=ROOT, capture_output=True, env=env,
                          timeout=180)


def is_ascii(data: bytes) -> bool:
    return all(b < 128 for b in data)


def load_generar_mapa():
    spec = importlib.util.spec_from_file_location("generar_mapa_ref", GENERAR_MAPA_PATH)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class Base(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.source = load_json(SOURCE_PATH)
        cls.doc, cls.model, errors = geometry.build_map(cls.source)
        assert not errors, errors
        cls.lines = {line["id"]: line for line in cls.doc["lines"]}
        cls.stops = load_json(STOPS_PATH)
        cls.by_n = {s["n"]: s for s in cls.stops["stops"]}
        cls.records = [r for r in cls.source["paradas_1_200"]]


class ProjectionTests(Base):
    def test_five_points_match_generar_mapa(self):
        ref = load_generar_mapa().Proj(self.source["proyeccion"])
        frame = self.model.frame
        self.assertAlmostEqual(ref.aspect, frame.aspect, places=12)
        self.assertAlmostEqual(1.31498, frame.aspect, places=5)
        points = {r["id"]: (r["lat"], r["lon"]) for r in self.records if r["id"] in
                  ("belem", "santarem", "manaos", "leticia", "iquitos")}
        self.assertEqual(5, len(points))
        for sid, (lat, lon) in sorted(points.items()):
            with self.subTest(stop=sid):
                want = ref.norm(lat, lon)
                got = frame.norm(lat, lon)
                self.assertAlmostEqual(want[0], got[0], places=12)
                self.assertAlmostEqual(want[1], got[1], places=12)

    def test_cw90_without_mirror(self):
        # Norte a la derecha (x crece con la latitud) y este abajo (y crece con la longitud): Andes arriba, Atlantico abajo.
        frame = self.model.frame
        north = frame.norm(5.0, -60.0)
        south = frame.norm(-10.0, -60.0)
        east = frame.norm(-3.0, -50.0)
        west = frame.norm(-3.0, -75.0)
        self.assertGreater(north[0], south[0])
        self.assertGreater(east[1], west[1])
        self.assertEqual({"rotation": "cw90", "lat": [-18.0, 7.0], "lon": [-80.0, -47.0], "lat0": -5.0}, self.doc["frame"])


class EncodingTests(Base):
    def test_round_trip_with_wrapping_deltas(self):
        pts = [(0, 0), (65535, 0), (0, 65535), (12345, 54321), (65535, 65535), (1, 2)]
        self.assertEqual(pts, geometry.decode_points(geometry.encode_points(pts)))
        idx = [0, 1, 2, 65535, 7]
        self.assertEqual(idx, geometry.decode_indices(geometry.encode_indices(idx)))

    def test_first_pair_absolute_and_deltas_little_endian(self):
        import base64
        raw = base64.b64decode(geometry.encode_points([(10, 20), (5, 25)]))
        self.assertEqual(bytes([10, 0, 20, 0, 0xFB, 0xFF, 5, 0]), raw, "delta -5 = 65531 en uint16 LE")

    def test_quantization_error_is_under_half_a_pixel(self):
        # Con 328 dp de ancho util (360 - 2 x 16) y el zoom maximo de la vista Mundo (15x): <= 0,5 px.
        frame = self.model.frame
        width = 328 * 15
        worst = 0.0
        for r in self.source["rios"]:
            for p in r["puntos"]:
                x, y = frame.norm(p["lat"], p["lon"])
                X, Y = frame.quant(p["lat"], p["lon"])
                worst = max(worst, abs(X / geometry.Q - x) * width, abs(Y / geometry.Q - y) * width * frame.aspect)
        self.assertLessEqual(worst, 0.5)

    def test_committed_pts_decode_to_the_source_points(self):
        committed = load_json(MAP_PATH)
        for line in committed["lines"]:
            with self.subTest(line=line["id"]):
                self.assertEqual(self.model.lines[line["id"]].q, geometry.decode_points(line["pts"]))


class LinesTests(Base):
    def test_kinds_and_classes(self):
        kinds = {lid: line["kind"] for lid, line in self.lines.items()}
        self.assertEqual("main", kinds["amazonas"])
        self.assertEqual("main", kinds["para"])
        for lid in ("xingu", "trombetas", "purus", "jurua", "japura", "putumayo_ica", "tocantins"):
            self.assertEqual("passby", kinds[lid], lid)
        for lid in ("tapajos", "negro", "madeira", "napo", "maranon"):
            self.assertEqual("tributary", kinds[lid], lid)
        self.assertNotIn("orinoco", kinds, "fuera de la cuenca: recuadros, vacios en la 1.0")
        self.assertNotIn("maroni", kinds)
        self.assertEqual([], self.doc["insets"])
        for line in self.doc["lines"]:
            self.assertEqual(geometry.CLASS[line["kind"]], line["class"])
            self.assertEqual("rio_amazonas", line["src"])

    def test_local_rivers_of_stops_11_13_15_16_18(self):
        for r in self.records:
            if r["k"] in (11, 13, 15, 16, 18):
                self.assertTrue(r.get("rios_locales"), r["id"])
                for rid in r["rios_locales"]:
                    with self.subTest(stop=r["k"], river=rid):
                        self.assertEqual("local", self.lines[rid]["kind"])

    def test_passby_whole_and_madeira_and_napo_only_the_mouth(self):
        for line in self.doc["lines"]:
            if line["kind"] == "passby":
                self.assertEqual(1.0, line["fill_on_pass"], line["id"])
        for lid in ("madeira", "napo"):
            fill = self.lines[lid]["fill_on_pass"]
            km = fill * self.model.lines[lid].arc_km[-1]
            self.assertLess(fill, 0.2, lid)
            self.assertAlmostEqual(geometry.MOUTH_FILL_KM, km, delta=1.0, msg=lid)

    def test_water_of_sioli(self):
        main = self.lines["amazonas"]["water"]
        self.assertEqual(["andean", "white"], [w["type"] for w in main], "la cabecera andina y el resto blanca")
        self.assertEqual(0.0, main[0]["s0"])
        self.assertEqual(1.0, main[-1]["s1"])
        self.assertEqual("clear", self.lines["tapajos"]["water"][0]["type"])
        self.assertEqual("black", self.lines["negro"]["water"][0]["type"])
        self.assertEqual("black", self.lines["nanay"]["water"][0]["type"])
        self.assertEqual([], self.lines["casiquiare"]["water"], "[NV]: sin agua, solo vidrio")

    def test_splits_of_santarem_and_the_meeting_of_waters(self):
        splits = {sp["id"]: sp for sp in self.lines["amazonas"]["splits"]}
        self.assertEqual({"encontro", "santarem"}, set(splits))
        # Mirando rio abajo, el Negro (del norte) va a la izquierda y el Tapajos (del sur) a la derecha.
        self.assertEqual(("black", "white"), (splits["encontro"]["left"], splits["encontro"]["right"]))
        self.assertEqual(("white", "clear"), (splits["santarem"]["left"], splits["santarem"]["right"]))
        self.assertEqual(self.lines["negro"]["mouth"]["s"], splits["encontro"]["s0"])
        self.assertEqual(self.lines["tapajos"]["mouth"]["s"], splits["santarem"]["s0"])
        for sp in splits.values():
            km0 = self.model.km_at("amazonas", sp["s0"])
            km1 = self.model.km_at("amazonas", sp["s1"])
            self.assertAlmostEqual(geometry.SPLIT_KM, km0 - km1, delta=0.5)

    def test_mouths_fall_where_the_research_puts_the_confluences(self):
        # Km del cauce de las confluencias (rio-amazonas.json): Santarem 704, Encuentro 1437, Nauta 3837.
        for lid, km in (("tapajos", 704), ("negro", 1437), ("maranon", 3837)):
            with self.subTest(line=lid):
                self.assertAlmostEqual(km, self.model.km_at("amazonas", self.lines[lid]["mouth"]["s"]), delta=2.0)
        # s crece rio abajo: las bocas cercanas al mar tienen mas s.
        s = {lid: self.lines[lid]["mouth"]["s"] for lid in ("xingu", "trombetas", "tapajos", "madeira", "negro", "purus",
                                                            "japura", "napo", "maranon")}
        order = sorted(s, key=s.get, reverse=True)
        self.assertEqual(["xingu", "tapajos", "trombetas", "madeira", "negro", "purus", "japura", "napo", "maranon"], order)

    def test_home_in_belem_on_guajara_bay(self):
        home = self.doc["home"]
        self.assertEqual("para", home["on"])
        self.assertEqual(self.by_n[0]["at"], {"line": "para", "s": home["s"]})

    def test_labels_viewpoints_and_passing(self):
        self.assertEqual(["BR", "PE", "CO", "EC", "BO", "VE", "GY", "SR", "GF"], [lab["country"] for lab in self.doc["labels"]])
        self.assertEqual(3, len(self.doc["viewpoints"]))
        self.assertTrue(all(v["reveal_on"] == 10 for v in self.doc["viewpoints"]))
        passing = {p["id"]: p for p in self.doc["passing"]}
        self.assertEqual(17, len(passing))
        self.assertEqual("map.stop.obidos.dato_01", passing["obidos"]["fact"])
        self.assertEqual("para", passing["breves"]["line"])
        self.assertEqual("amazonas", passing["gurupa"]["line"], "Gurupa, en la bifurcacion: el cauce gana el empate")
        self.assertEqual("amazonas", passing["boca_del_napo"]["line"])


class TriangulationTests(Base):
    def test_ear_clip_square_and_concave(self):
        square = [(0, 0), (10, 0), (10, 10), (0, 10)]
        self.assertEqual(6, len(geometry.ear_clip(square)))
        concave = [(0, 0), (10, 0), (10, 10), (5, 3), (0, 10)]
        tris = geometry.ear_clip(concave)
        self.assertEqual(9, len(tris))
        area = sum(geometry._cross(*(concave[i] for i in tris[k:k + 3])) for k in range(0, len(tris), 3))
        self.assertEqual(geometry._area2(concave), area)
        for k in range(0, len(tris), 3):
            self.assertGreater(geometry._cross(*(concave[i] for i in tris[k:k + 3])), 0)

    def test_collinear_vertex_adds_no_triangle(self):
        poly = [(0, 0), (5, 0), (10, 0), (10, 10), (0, 10)]
        tris = geometry.ear_clip(poly)
        area = sum(geometry._cross(*(poly[i] for i in tris[k:k + 3])) for k in range(0, len(tris), 3))
        self.assertEqual(geometry._area2(poly), area)

    def test_self_intersecting_ring_is_reported(self):
        self.assertTrue(geometry.polygon_problems([(0, 0), (10, 10), (10, 0), (0, 10)]))

    def test_land_of_the_file(self):
        land = self.doc["land"]
        verts = geometry.decode_points(land["verts"])
        tris = geometry.decode_indices(land["tris"])
        self.assertEqual([], geometry.polygon_problems(verts))
        self.assertEqual(len(verts) - 2, len(tris) // 3)
        self.assertEqual([{"id": "land", "tile": "canopy", "first": 0, "count": len(tris) // 3}], land["zones"])
        area = sum(geometry._cross(*(verts[i] for i in tris[k:k + 3])) for k in range(0, len(tris), 3))
        self.assertEqual(geometry._area2(verts), area)
        # Belem y Manaos, en tierra; un punto del Atlantico, fuera.
        def inside(p):
            return any(geometry._in_triangle(p, *(verts[i] for i in tris[k:k + 3])) for k in range(0, len(tris), 3))
        frame = self.model.frame
        self.assertTrue(inside(frame.quant(-1.4558, -48.5039)))
        self.assertTrue(inside(frame.quant(-3.1189, -60.0217)))
        self.assertFalse(inside(frame.quant(5.0, -48.0)))


class FileTests(Base):
    def test_committed_file_is_what_the_source_gives(self):
        self.assertEqual(geometry.render(self.doc).encode("utf-8"), MAP_PATH.read_bytes())

    def test_utf8_without_bom_with_lf(self):
        data = MAP_PATH.read_bytes()
        self.assertFalse(data.startswith(b"\xef\xbb\xbf"))
        self.assertNotIn(b"\r", data)
        self.assertTrue(data.endswith(b"\n"))
        self.assertLess(len(data), 90 * 1024, "unos 70-90 KB con las 100 paradas (08 §4.5)")

    def test_deterministic_bytes_with_different_hash_seeds(self):
        outs = []
        with tempfile.TemporaryDirectory() as tmp:
            for seed in ("0", "1", "12345"):
                out = Path(tmp) / f"map_{seed}.json"
                env = dict(os.environ, PYTHONHASHSEED=seed)
                proc = run_geometry("--out", str(out), env=env)
                self.assertEqual(0, proc.returncode, proc.stdout)
                outs.append(out.read_bytes())
        self.assertEqual(1, len(set(outs)))
        self.assertEqual(MAP_PATH.read_bytes(), outs[0])


class RouteTests(Base):
    def test_twenty_stops_with_at_and_leg(self):
        self.assertEqual(20, len(self.stops["stops"]))
        for s in self.stops["stops"]:
            self.assertIn(s["at"]["line"], self.lines)
            self.assertTrue(s["leg"]["path"])
        self.assertEqual([], geometry.validate_routes(self.stops, self.model, self.records))

    def test_leg_of_41_passes_obidos_parintins_itacoatiara_and_the_madeira_mouth(self):
        leg = self.by_n[4]["leg"]
        self.assertEqual(["obidos", "parintins", "itacoatiara", "boca_del_madeira"], leg["via"])
        self.assertIn("madeira", leg["mouths"])
        self.assertEqual(["trombetas", "madeira", "negro"], leg["mouths"])
        self.assertEqual(["tapajos", "amazonas"], [p["line"] for p in leg["path"]], "baja el Tapajos y sube el cauce")

    def test_there_and_back_through_the_confluences(self):
        # Iquitos/Itaya -> Allpahuayo-Mishana: el cauce hasta la boca del Nanay y el Nanay arriba; despues, abajo y a Nauta.
        self.assertEqual(["amazonas", "nanay"], [p["line"] for p in self.by_n[16]["leg"]["path"]])
        self.assertEqual(["nanay", "amazonas"], [p["line"] for p in self.by_n[17]["leg"]["path"]])
        self.assertEqual(["para", "amazonas"], [p["line"] for p in self.by_n[1]["leg"]["path"]])
        self.assertEqual(["breves", "gurupa", "boca_del_xingu", "monte_alegre"], self.by_n[1]["leg"]["via"])

    def test_first_map_fills_the_tocantins_and_each_mouth_once(self):
        self.assertEqual(["tocantins"], self.by_n[0]["leg"]["mouths"])
        seen = []
        for s in self.stops["stops"]:
            for m in s["leg"]["mouths"]:
                self.assertNotIn(m, seen, f"{m} se llena dos veces")
                seen.append(m)
        for lid in ("xingu", "trombetas", "purus", "jurua", "japura", "putumayo_ica", "madeira", "napo", "maranon"):
            self.assertIn(lid, seen)

    def test_stops_sit_on_their_river_and_km_never_go_back(self):
        prev = None
        for s in self.stops["stops"]:
            rec = next(r for r in self.records if r["k"] == s["n"])
            if s["km_from_sea"] is not None:
                self.assertAlmostEqual(s["km_from_sea"], self.model.km_at(s["at"]["line"], s["at"]["s"]), delta=1.0)
            pos = self.model.lines[s["at"]["line"]].point_at_s(s["at"]["s"])
            limit = geometry.OFF_RIVER_KM.get(s["id"], geometry.TOLERANCE_KM)
            self.assertLessEqual(geometry.haversine((rec["lat"], rec["lon"]), pos), limit, s["id"])
            if prev is not None and prev["km_from_sea"] is not None:
                self.assertGreaterEqual(s["km_from_sea"], prev["km_from_sea"])
            prev = s

    def test_same_place_only_at_a_confluence(self):
        tapajos = self.lines["tapajos"]["mouth"]["s"]
        self.assertTrue(geometry.same_place(self.model, ("tapajos", 1.0), ("amazonas", tapajos)))
        self.assertTrue(geometry.same_place(self.model, ("amazonas", tapajos), ("tapajos", 1.0)))
        self.assertFalse(geometry.same_place(self.model, ("tapajos", 0.5), ("amazonas", tapajos)))
        self.assertFalse(geometry.same_place(self.model, ("negro", 1.0), ("amazonas", tapajos)))

    def test_broken_leg_is_reported(self):
        doc = copy.deepcopy(self.stops)
        leg = doc["stops"][4]["leg"]
        leg["path"] = [leg["path"][1]]   # sin el Tapajos: salta de la FLONA al cauce por tierra
        errors = geometry.validate_routes(doc, self.model, self.records)
        self.assertTrue(any("leg roto" in e and "por tierra" in e for e in errors), errors)


class CheckTests(Base):
    def check(self, source: dict | None = None, stops: dict | None = None, map_bytes: bytes | None = None):
        with tempfile.TemporaryDirectory() as tmp:
            args = ["--check"]
            if source is not None:
                path = Path(tmp) / "rio.json"
                path.write_text(json.dumps(source, ensure_ascii=False), encoding="utf-8")
                args += ["--source", str(path)]
            if stops is not None:
                path = Path(tmp) / "river_stops_v1.json"
                path.write_text(json.dumps(stops, ensure_ascii=False), encoding="utf-8")
                args += ["--stops", str(path)]
            if map_bytes is not None:
                path = Path(tmp) / "river_map_v1.json"
                path.write_bytes(map_bytes)
                args += ["--out", str(path)]
            proc = run_geometry(*args)
        self.assertTrue(is_ascii(proc.stdout + proc.stderr))
        return proc.returncode, proc.stdout.decode("ascii")

    def assertFails(self, fragment: str, **kwargs):
        code, out = self.check(**kwargs)
        self.assertEqual(1, code, out)
        self.assertIn(fragment, out)

    def test_check_passes_on_the_committed_files(self):
        code, out = self.check()
        self.assertEqual(0, code, out)
        self.assertIn("27 lineas, 17 puntos de paso y 3 miradores", out)

    def test_stop_moved_20_km_fails(self):
        src = copy.deepcopy(self.source)
        rec = next(r for r in src["paradas_1_200"] if r["k"] == 14)
        rec["lat"] = round(rec["lat"] + 0.18, 4)   # unos 20 km al norte
        self.assertFails("parada n=14 ('iquitos'): el sitio queda a 20.", source=src)

    def test_stop_moved_along_the_river_fails_by_its_km(self):
        stops = copy.deepcopy(self.stops)
        stops["stops"][14]["km_from_sea"] += 20
        self.assertFails("km_from_sea 3720 y su punto del rio esta en el km 3700", stops=stops)

    def test_broken_leg_fails(self):
        stops = copy.deepcopy(self.stops)
        stops["stops"][4]["leg"]["path"][1]["s0"] = 0.8   # el cauce ya no empieza en la boca del Tapajos
        self.assertFails("parada n=4 ('encontro'): leg roto entre path[0]", stops=stops)

    def test_mouth_outside_its_river_fails(self):
        src = copy.deepcopy(self.source)
        madeira = next(r for r in src["rios"] if r["id"] == "madeira")
        madeira["puntos"][-1]["lat"] = round(madeira["puntos"][-1]["lat"] - 0.3, 4)   # ~33 km al sur del cauce
        self.assertFails("linea madeira: su boca queda a", source=src)

    def test_missing_candidate_fails(self):
        src = copy.deepcopy(self.source)
        rec = next(r for r in src["paradas_1_200"] if r["id"] == "pacaya_samiria")
        rec["otros_datos"] = []
        self.assertFails("falta el candidato de la propuesta sec. 5.1 bis map.stop.pacaya_samiria.dato_03", source=src)

    def test_drift_fails(self):
        data = MAP_PATH.read_bytes().replace(b'"reveal_on": 10', b'"reveal_on": 20', 1)
        self.assertFails("desfase", map_bytes=data)

    def test_validate_map_reports_a_main_line_too_short(self):
        src = copy.deepcopy(self.source)
        pts = next(r for r in src["rios"] if r["id"] == "amazonas")["puntos"]
        pts[40]["km_desde_el_mar"] = pts[41]["km_desde_el_mar"] + 1   # el tramo mide 1 km y la linea recta mas
        doc, model, errors = geometry.build_map(src)
        errors += geometry.validate_map(doc, model, src)
        self.assertTrue(any("mide 1 km y la linea recta" in e for e in errors), errors)


if __name__ == "__main__":
    unittest.main()
