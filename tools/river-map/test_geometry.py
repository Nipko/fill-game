# -*- coding: utf-8 -*-
"""test_geometry.py - pruebas de tools/river-map/geometry.py y shapefile.py (geometria af-river-map/1 y at y leg de las
paradas; M2.6, con la geometria de produccion de M5.1).

Ejecutar desde la raiz del repositorio:
    py -3.14 -m unittest discover -s tools/river-map -p "test_*.py"

Las pruebas no necesitan las descargas de Natural Earth y HydroRIVERS (tools/river-map/source/, sin versionar): leen la
geometria commiteada (el modo «commiteado» de --check, el del portatil) y usan fixtures pequennos escritos con
shapefile.write_layer. Si las descargas estan, ProductionTests regenera ademas desde ellas.

Cubre: la proyeccion (cinco puntos contra generar_mapa.py); el base64 de ida y vuelta (<= 0,5 px); el lector de
shapefiles (PolyLine, Polygon, forma nula, .dbf y errores); sources.json (SHA-256, licencias y descargas ausentes,
incompletas o con otro tamanno); el trazado por la red de HydroRIVERS (la cabecera que pasa por mas puntos de control,
la boca en un vertice del anfitrion, la cola de la investigacion y un rio «solo_boca»); Douglas-Peucker por vista; el
recorte de la tierra por la caja, la triangulacion sin vertices partidos y la validacion de la tierra por sus aristas
(continente e islas); las s de las bocas y de las paradas (de acuerdo con los km de la investigacion); el archivo
commiteado (determinista, al dia, UTF-8 sin BOM y LF, <= 90 KB); los legs continuos, que no cruzan tierra, con la ida y
vuelta por las confluencias; la tolerancia de cada vista con solo las excepciones medidas pendientes del duenno, y sus
avisos; y los fallos de --check: una parada desplazada 20 km (tambien un sitio tierra adentro y un punto de atraque), una
parada que retrocede, un leg roto, un punto de control fuera de su linea (tambien la boca de un rio), la boca del Pacaya
mas alla de su cifra medida, un candidato de la propuesta §5.1 bis ausente y el desfase.
Revision de M5.1, con fixtures de write_layer: la tierra de Natural Earth (sentido ESRI, recorte por la caja, agujero dentro
de la caja e islas por area), el Casiquiare de Natural Earth (partes desordenadas y orientacion desde la cabecera), la
reparacion de la costa que se corta y su fallo al agotar las rondas, production_geometry de punta a punta (bocas en un
vertice del anfitrion, boca fuera de vertice llevada al mas cercano y arranque del ramal), main con un archivo de mas de
MAX_BYTES, los archivos cerrados al fallar el lector, las paradas en una confluencia (confluence_s), los km que no vuelven rio
arriba y el aviso de las lineas que siguen siendo el esbozo de la investigacion.
Solo biblioteca estandar.
"""
from __future__ import annotations

import contextlib
import copy
import gc
import hashlib
import io
import importlib.util
import json
import os
import subprocess
import sys
import tempfile
import unittest
import warnings
from pathlib import Path
from unittest import mock

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

import geometry  # noqa: E402
import shapefile  # noqa: E402
from riverdata import (  # noqa: E402
    GENERAR_MAPA_PATH,
    MAP_PATH,
    ROOT,
    SOURCE_PATH,
    STOPS_PATH,
    load_json,
)

GEOMETRY_SCRIPT = HERE / "geometry.py"
# Una carpeta de descargas que no existe: --check en el modo «commiteado» (el del portatil), en cualquier maquina.
NO_SOURCES = HERE / "no_existe_m51"


def run_geometry(*args: str, env: dict | None = None) -> subprocess.CompletedProcess:
    return subprocess.run([sys.executable, str(GEOMETRY_SCRIPT), *args], cwd=ROOT, capture_output=True, env=env,
                          timeout=600)


def is_ascii(data: bytes) -> bool:
    return all(b < 128 for b in data)


def load_generar_mapa():
    spec = importlib.util.spec_from_file_location("generar_mapa_ref", GENERAR_MAPA_PATH)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def have_sources() -> bool:
    try:
        return geometry.sources_state(geometry.SOURCE_DIR, geometry.load_manifest()).status == "complete"
    except geometry.GeometryError:
        return False


class Base(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.source = load_json(SOURCE_PATH)
        cls.geo = geometry.committed_geometry(MAP_PATH)
        cls.doc, cls.model, errors = geometry.build_map(cls.source, cls.geo)
        assert not errors, errors
        cls.lines = {line["id"]: line for line in cls.doc["lines"]}
        cls.stops = load_json(STOPS_PATH)
        cls.by_n = {s["n"]: s for s in cls.stops["stops"]}
        cls.records = [r for r in cls.source["paradas_1_200"] + cls.source["paradas_201_700"]]


# Paradas en una confluencia, ancladas en la s de la boca de su afluente (geometry.confluence_s [P]; revision de M5.1):
# {n: afluente}.
CONFLUENCE_STOPS = {1: "tapajos", 4: "negro", 11: "loretoyacu", 13: "atacuari", 14: "itaya", 17: "maranon",
                    33: "tambopata", 34: "tambopata", 38: "manu", 39: "manu", 42: "madre_de_dios", 43: "madre_de_dios",
                    44: "madre_de_dios"}


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

    def test_model_lines_are_the_committed_points(self):
        committed = load_json(MAP_PATH)
        for line in committed["lines"]:
            with self.subTest(line=line["id"]):
                self.assertEqual(self.model.lines[line["id"]].q, geometry.decode_points(line["pts"]))


class ShapefileTests(unittest.TestCase):
    """Lector minimo de .shp, .shx y .dbf (M5.1) con fixtures escritos por shapefile.write_layer."""

    def test_polyline_round_trip_with_null_shape_and_attributes(self):
        with tempfile.TemporaryDirectory() as tmp:
            base = Path(tmp) / "rios"
            fields = [("HYRIV_ID", "N", 9, 0), ("UPLAND_SKM", "N", 10, 1), ("name", "C", 20, 0)]
            records = [([[(-60.0, -5.0), (-59.5, -5.25)]], [7, 12.5, "Río Negro"]),
                       ([], [8, None, ""]),
                       ([[(-61.0, -4.0), (-60.0, -4.0)], [(-58.0, -3.0), (-57.5, -3.5), (-57.0, -3.0)]], [9, 3.0, "Ñ"])]
            shapefile.write_layer(base, 3, records, fields)
            with shapefile.Layer(base) as layer:
                self.assertEqual(3, layer.count)
                self.assertEqual(3, layer.shapes.shape_type)
                self.assertEqual((-61.0, -5.25, -57.0, -3.0), layer.shapes.bbox)
                self.assertEqual([[(-60.0, -5.0), (-59.5, -5.25)]], layer.shapes.parts(0))
                self.assertEqual([], layer.shapes.parts(1))
                self.assertIsNone(layer.shapes.record_bbox(1))
                self.assertEqual(2, len(layer.shapes.parts(2)))
                self.assertEqual((-61.0, -4.0, -57.0, -3.0), layer.shapes.record_bbox(2))
                self.assertEqual([7, 8, 9], layer.table.column("HYRIV_ID"))
                self.assertEqual([12.5, None, 3.0], layer.table.column("UPLAND_SKM"))
                self.assertEqual({"HYRIV_ID": 7, "UPLAND_SKM": 12.5, "name": "Río Negro"}, layer.table.record(0))
                self.assertEqual("Ñ", layer.table.column("name")[2])
                with self.assertRaises(shapefile.ShapefileError):
                    layer.table.column("NO_EXISTE")

    def test_polygon_and_deleted_record(self):
        with tempfile.TemporaryDirectory() as tmp:
            base = Path(tmp) / "tierra"
            ring = [(0.0, 0.0), (0.0, 1.0), (1.0, 1.0), (1.0, 0.0), (0.0, 0.0)]
            shapefile.write_layer(base, 5, [([ring], [1]), ([ring], [2])], [("id", "N", 4, 0)])
            dbf = base.with_suffix(".dbf")
            data = bytearray(dbf.read_bytes())
            header_len = int.from_bytes(data[8:10], "little")
            record_len = int.from_bytes(data[10:12], "little")
            data[header_len + record_len] = ord("*")   # borra el segundo registro
            dbf.write_bytes(bytes(data))
            with shapefile.Layer(base) as layer:
                self.assertEqual(5, layer.shapes.shape_type)
                self.assertEqual([ring], layer.shapes.parts(0))
                self.assertEqual([1, None], layer.table.column("id"))
                self.assertIsNone(layer.table.record(1))

    def test_not_a_shapefile_fails(self):
        with tempfile.TemporaryDirectory() as tmp:
            base = Path(tmp) / "malo"
            base.with_suffix(".shp").write_bytes(b"\x00" * 100)
            base.with_suffix(".shx").write_bytes(b"\x00" * 100)
            # Revision de M5.1: al fallar la cabecera, los dos archivos quedan cerrados (en Windows, uno abierto queda
            # bloqueado): sin ResourceWarning.
            self.assertFailsClosed("no es un shapefile", lambda: shapefile.Shapes(base.with_suffix(".shp")))

    def assertFailsClosed(self, regex: str, fn) -> None:
        with warnings.catch_warnings(record=True) as caught:
            warnings.simplefilter("always", ResourceWarning)
            with self.assertRaisesRegex(shapefile.ShapefileError, regex):
                fn()
            gc.collect()
        self.assertEqual([], [str(w.message) for w in caught if issubclass(w.category, ResourceWarning)])

    def test_bad_index_or_shape_type_closes_its_files(self):
        with tempfile.TemporaryDirectory() as tmp:
            base = Path(tmp) / "rios"
            shapefile.write_layer(base, 3, [([[(-60.0, -5.0), (-59.0, -5.0)]], [1])], [("id", "N", 4, 0)])
            shx = base.with_suffix(".shx")
            good = shx.read_bytes()
            shx.write_bytes(good + b"\x00" * 3)   # un indice que no es de registros de 8 bytes
            self.assertFailsClosed("registros de 8 bytes", lambda: shapefile.Shapes(base.with_suffix(".shp")))
            shx.write_bytes(good)
            shp = base.with_suffix(".shp")
            data = bytearray(shp.read_bytes())
            data[32:36] = (1).to_bytes(4, "little")   # tipo 1 (Point): no soportado
            shp.write_bytes(bytes(data))
            self.assertFailsClosed("tipo de forma 1 no soportado", lambda: shapefile.Shapes(shp))


class SourcesTests(unittest.TestCase):
    """sources.json: SHA-256 de las descargas y su licencia (D-013, punto 5; M5.1)."""

    @classmethod
    def setUpClass(cls):
        cls.manifest = geometry.load_manifest()

    def test_manifest_lists_the_downloads_with_their_hash_and_license(self):
        files = {f["name"]: f for f in self.manifest["files"]}
        self.assertEqual(26, len(files))
        used = sorted(n for n, f in files.items() if f["used"])
        self.assertEqual(11, len(used), "shp, shx y dbf de las tres capas, y el cpg de las dos de Natural Earth")
        for layer in (geometry.HR_LAYER, geometry.NE_LAND_LAYER, geometry.NE_RIVERS_LAYER):
            for ext in ("shp", "shx", "dbf"):
                self.assertTrue(files[f"{layer}.{ext}"]["used"], f"{layer}.{ext}")
        self.assertFalse(files["ne_10m_coastline.shp"]["used"], "descargada, sin uso: su linea es el borde de land")
        datasets = {d["id"]: d for d in self.manifest["datasets"]}
        self.assertIn("Dominio publico", datasets["ne10m"]["license"])
        self.assertIn("Lehner, B., Grill G. (2013)", datasets["hydrorivers"]["citation"])
        self.assertIn("PENDIENTE DEL DUENNO", datasets["hydrorivers"]["license"])

    def test_downloads_are_not_versioned(self):
        self.assertIn("/tools/river-map/source/", (ROOT / ".gitignore").read_text(encoding="utf-8").splitlines())

    def test_absent_incomplete_and_wrong_size(self):
        with tempfile.TemporaryDirectory() as tmp:
            self.assertEqual("absent", geometry.sources_state(Path(tmp), self.manifest).status)
            cpg = Path(tmp) / "ne_10m_land.cpg"
            cpg.write_bytes(b"UTF-8")
            entry = next(f for f in self.manifest["files"] if f["name"] == "ne_10m_land.cpg")
            self.assertEqual(entry["sha256"], hashlib.sha256(b"UTF-8").hexdigest(), "el .cpg de Natural Earth es «UTF-8»")
            state = geometry.sources_state(Path(tmp), self.manifest)
            self.assertEqual("bad", state.status)
            self.assertTrue(state.problems[0].startswith("faltan HydroRIVERS_v10_sa.dbf"), state.problems)
            cpg.write_bytes(b"UTF-16")
            state = geometry.sources_state(Path(tmp), self.manifest)
            self.assertIn("ne_10m_land.cpg: 6 bytes y sources.json dice 5", state.problems)

    def test_complete_and_another_hash_with_a_fixture_manifest(self):
        with tempfile.TemporaryDirectory() as tmp:
            (Path(tmp) / "a.shp").write_bytes(b"abc")
            manifest = {"files": [{"name": "a.shp", "sha256": hashlib.sha256(b"abc").hexdigest(), "bytes": 3, "used": True},
                                  {"name": "a.pdf", "sha256": "0" * 64, "bytes": 1, "used": False}]}
            self.assertEqual("complete", geometry.sources_state(Path(tmp), manifest).status)
            (Path(tmp) / "a.shp").write_bytes(b"abd")
            state = geometry.sources_state(Path(tmp), manifest)
            self.assertEqual(["a.shp: su SHA-256 no es el de sources.json (otra version o archivo danado)"], state.problems)

    def test_check_with_bad_downloads_fails_and_without_them_does_not_regenerate(self):
        with tempfile.TemporaryDirectory() as tmp:
            (Path(tmp) / "ne_10m_land.cpg").write_bytes(b"UTF-16")
            proc = run_geometry("--check", "--sources", tmp)
            self.assertEqual(1, proc.returncode)
            self.assertIn(b"FAIL  descargas de", proc.stdout)
            self.assertIn(b"ne_10m_land.cpg: 6 bytes", proc.stdout)
            out = Path(tmp) / "map.json"
            proc = run_geometry("--sources", str(NO_SOURCES), "--out", str(out))
            self.assertEqual(1, proc.returncode)
            self.assertIn(b"FAIL  faltan las descargas en", proc.stdout)
            self.assertFalse(out.exists(), "sin las descargas no se escribe nada")


def _hr_fixture(tmp: str) -> Path:
    """Red de HydroRIVERS de juguete (lon, lat) dentro de la caja del marco. El cauce M (1 -> 2 -> 3 -> 12 -> mar), un
    afluente T (4 -> 5 -> M2, por su primer vertice), un senuelo D (9) que nace en la cabecera de T, con mas cuenca, y llega a
    M3 sin pasar por los puntos de control de T, otro E (10 -> 11) que va a otro mar, y un rio S «solo_boca» (6 y 7 -> 8 ->
    M3b) con el tramo 6 de mas cuenca que el 7."""
    base = Path(tmp) / "hr"
    reaches = [
        (1, 2, 100.0, [(-62.0, -5.0), (-61.5, -5.0), (-61.0, -5.0)]),
        (2, 3, 200.0, [(-61.0, -5.0), (-60.5, -5.02), (-60.0, -5.0)]),
        (3, 12, 300.0, [(-60.0, -5.0), (-59.5, -5.0)]),
        (12, 0, 320.0, [(-59.5, -5.0), (-59.0, -5.0)]),
        (4, 5, 20.0, [(-60.5, -6.21), (-60.7, -5.8)]),
        (5, 2, 40.0, [(-60.7, -5.8), (-60.85, -5.4), (-61.0, -5.0)]),
        (9, 3, 60.0, [(-60.48, -6.2), (-60.3, -5.6), (-60.0, -5.0)]),
        (10, 11, 1.0, [(-60.48, -6.201), (-59.5, -6.5)]),
        (11, 0, 2.0, [(-59.5, -6.5), (-59.0, -6.6)]),
        (6, 8, 10.0, [(-59.8, -4.0), (-59.9, -4.5)]),
        (7, 8, 3.0, [(-59.7, -4.4), (-59.9, -4.5)]),
        (8, 12, 15.0, [(-59.9, -4.5), (-59.5, -5.0)]),
    ]
    reaches.sort(key=lambda r: r[0])
    fields = [("HYRIV_ID", "N", 9, 0), ("NEXT_DOWN", "N", 9, 0), ("UPLAND_SKM", "N", 10, 1)]
    shapefile.write_layer(base, 3, [([pts], [rid, nxt, up]) for rid, nxt, up, pts in reaches], fields)
    return base


def _hr_fixture_off_vertex(tmp: str) -> Path:
    """La red de _hr_fixture con el ultimo punto de T (tramo 5) a ~0,1 km del primer vertice de M2: un afluente cuya boca de
    HydroRIVERS no es un vertice de su anfitrion."""
    base = Path(tmp) / "hr"
    reaches = [
        (1, 2, 100.0, [(-62.0, -5.0), (-61.5, -5.0), (-61.0, -5.0)]),
        (2, 3, 200.0, [(-61.0, -5.0), (-60.5, -5.02), (-60.0, -5.0)]),
        (3, 12, 300.0, [(-60.0, -5.0), (-59.5, -5.0)]),
        (4, 5, 20.0, [(-60.5, -6.21), (-60.7, -5.8)]),
        (5, 2, 40.0, [(-60.7, -5.8), (-60.85, -5.4), (-61.0, -5.001)]),
        (6, 8, 10.0, [(-59.8, -4.0), (-59.9, -4.5)]),
        (8, 12, 15.0, [(-59.9, -4.5), (-59.5, -5.0)]),
        (12, 0, 320.0, [(-59.5, -5.0), (-59.0, -5.0)]),
    ]
    fields = [("HYRIV_ID", "N", 9, 0), ("NEXT_DOWN", "N", 9, 0), ("UPLAND_SKM", "N", 10, 1)]
    shapefile.write_layer(base, 3, [([pts], [rid, nxt, up]) for rid, nxt, up, pts in reaches], fields)
    return base


def _land_layer(path: Path, rings: list) -> None:
    """Capa land de Natural Earth de juguete: un registro por anillo (lon, lat), tal cual (el sentido lo da la prueba)."""
    shapefile.write_layer(path, 5, [([ring], [k]) for k, ring in enumerate(rings)], [("featurecla", "N", 4, 0)])


# El continente en sentido horario (exterior ESRI) que sale de la caja por el este (lon_max = -47): se recorta.
CONTINENT_CW = [(-60.0, -10.0), (-60.0, 0.0), (-40.0, 0.0), (-40.0, -10.0), (-60.0, -10.0)]


def _fixture_source(rivers: list) -> dict:
    return {"proyeccion": {"caja": {"lat_min": -18.0, "lat_max": 7.0, "lon_min": -80.0, "lon_max": -47.0}, "lat0": -5.0},
            "rios": rivers, "paradas_1_200": [], "paradas_201_700": []}


def _river(rid: str, tipo: str, pts: list, **extra) -> dict:
    return dict({"id": rid, "tipo": tipo, "puntos": [{"lat": lat, "lon": lon} for lat, lon in pts]}, **extra)


class NaturalEarthTests(unittest.TestCase):
    """Natural Earth con fixtures de write_layer (revision de M5.1): la tierra (ne_land_rings) y el Casiquiare (_ne_line)."""

    frame = geometry.Frame(-18.0, 7.0, -80.0, -47.0, -5.0)

    def test_land_rings_clockwise_clipped_and_islands_by_area(self):
        small = [(-55.0, 2.0), (-55.0, 2.1), (-54.9, 2.1), (-54.9, 2.0), (-55.0, 2.0)]   # ~120 km2: fuera
        big = [(-55.0, 3.0), (-55.0, 3.5), (-54.5, 3.5), (-54.5, 3.0), (-55.0, 3.0)]     # ~3.000 km2: dentro
        outside = [(-30.0, 0.0), (-30.0, 1.0), (-29.0, 1.0), (-29.0, 0.0), (-30.0, 0.0)]  # fuera de la caja
        with tempfile.TemporaryDirectory() as tmp:
            _land_layer(Path(tmp) / geometry.NE_LAND_LAYER, [CONTINENT_CW, small, big, outside])
            rings = geometry.ne_land_rings(Path(tmp), self.frame)
        self.assertEqual(2, len(rings), "el continente y la isla grande; ni la pequenna ni la de fuera de la caja")
        continent, island = rings
        self.assertEqual({(-10.0, -60.0), (0.0, -60.0), (0.0, -47.0), (-10.0, -47.0)}, set(continent),
                         "(lat, lon), recortado por el borde este de la caja")
        self.assertEqual(4, len(continent), "sin el punto de cierre repetido")
        for ring in rings:
            self.assertGreater(geometry._ring_area([(lon, lat) for lat, lon in ring]), 0,
                               "en sentido positivo (los exteriores ESRI van en el de las agujas del reloj)")
        self.assertIn((3.5, -54.5), island)
        self.assertEqual(500.0, geometry.MIN_ISLAND_KM2)

    def test_hole_inside_the_box_fails_and_no_land_fails(self):
        hole = [(-55.0, -5.0), (-54.0, -5.0), (-54.0, -4.0), (-55.0, -4.0), (-55.0, -5.0)]   # antihorario: un agujero
        with tempfile.TemporaryDirectory() as tmp:
            _land_layer(Path(tmp) / geometry.NE_LAND_LAYER, [CONTINENT_CW, hole])
            with self.assertRaisesRegex(geometry.GeometryError, "un agujero"):
                geometry.ne_land_rings(Path(tmp), self.frame)
        with tempfile.TemporaryDirectory() as tmp:
            _land_layer(Path(tmp) / geometry.NE_LAND_LAYER, [[(-30.0, 0.0), (-30.0, 1.0), (-29.0, 1.0), (-30.0, 0.0)]])
            with self.assertRaisesRegex(geometry.GeometryError, "ninguna tierra dentro de la caja"):
                geometry.ne_land_rings(Path(tmp), self.frame)

    def test_ne_line_chains_unordered_parts_from_the_research_head(self):
        p0, p1, p2, p3 = (-67.0, 2.0), (-66.8, 2.2), (-66.6, 2.4), (-66.4, 2.6)   # (lon, lat), de la cabecera a la boca
        records = [([[(-70.0, 1.0), (-69.0, 1.5)]], ["Otro"]),
                   ([[p2, p3], [p1, p0]], ["Casiquiare"])]   # dos partes desordenadas, la segunda al reves
        with tempfile.TemporaryDirectory() as tmp:
            shapefile.write_layer(Path(tmp) / geometry.NE_RIVERS_LAYER, 3, records, [("name", "C", 29, 0)])
            line = geometry._ne_line(Path(tmp), "casiquiare", [(2.0, -67.0), (2.6, -66.4)])
            self.assertEqual([(2.0, -67.0), (2.2, -66.8), (2.4, -66.6), (2.6, -66.4)], line)
            # La cabecera de la investigacion en el otro extremo: la misma linea, al reves.
            self.assertEqual(list(reversed(line)), geometry._ne_line(Path(tmp), "casiquiare", [(2.6, -66.4), (2.0, -67.0)]))
            with self.assertRaisesRegex(geometry.GeometryError, "sin nombre de Natural Earth"):
                geometry._ne_line(Path(tmp), "negro", [(2.0, -67.0)])
            with mock.patch.dict(geometry.NE_NAMES, {"casiquiare": "No existe"}):
                with self.assertRaisesRegex(geometry.GeometryError, "ningun rio llamado 'No existe'"):
                    geometry._ne_line(Path(tmp), "casiquiare", [(2.0, -67.0)])


class _NoZone:
    """Una MundoZone vacia: todo en la vista Cuenca."""

    def contains(self, lat: float, lon: float) -> bool:
        return False


class LandRepairTests(unittest.TestCase):
    """simplify_land: si la simplificacion corta la costa, se repite con la mitad de tolerancia en esos tramos
    (LAND_REPAIR_ROUNDS); si sigue cortando, falla (revision de M5.1)."""

    frame = geometry.Frame(-18.0, 7.0, -80.0, -47.0, -5.0)
    # La costa norte del continente con una bahia en V de ~33 km de fondo y una isla que asoma en ella: al quitar el fondo
    # de la bahia (a 50 km de tolerancia), la costa recta corta la isla.
    COAST = [(-10.0, -70.0), (-10.0, -60.0), (-5.0, -60.0), (-5.0, -64.0), (-5.3, -65.0), (-5.0, -66.0), (-5.0, -70.0)]
    ISLAND = [(-4.9, -65.0), (-5.2, -64.85), (-5.2, -65.15)]

    def test_crossing_is_repaired_with_half_the_tolerance(self):
        rings = [self.COAST, self.ISLAND]
        straight = [self.frame.quant(*p) for p in self.COAST if p != (-5.3, -65.0)]
        self.assertTrue(geometry.crossings([straight, [self.frame.quant(*p) for p in self.ISLAND]]),
                        "sin el fondo de la bahia, la costa corta la isla")
        with mock.patch.object(geometry, "DP_CUENCA_KM", 50.0):
            out = geometry.simplify_land(self.frame, rings, _NoZone())
        self.assertEqual([], geometry.crossings(out))
        self.assertIn(self.frame.quant(-5.3, -65.0), out[0], "el fondo de la bahia vuelve tras la reparacion")
        self.assertEqual(3, len(out[1]))
        for ring in out:
            self.assertGreater(geometry._area2(ring), 0)

    def test_crossing_that_survives_the_rounds_fails(self):
        with mock.patch.object(geometry, "DP_CUENCA_KM", 50.0), mock.patch.object(geometry, "LAND_REPAIR_ROUNDS", 0):
            with self.assertRaisesRegex(geometry.GeometryError, "sigue cortando la costa tras 0 rondas"):
                geometry.simplify_land(self.frame, [self.COAST, self.ISLAND], _NoZone())


class ProductionGeometryTests(unittest.TestCase):
    """production_geometry de punta a punta con fixtures (revision de M5.1): sin las descargas reales."""

    RIVERS = [
        _river("amazonas", "cauce", [(-5.0, -62.0), (-5.02, -60.5), (-5.0, -59.0), (-4.5, -58.5), (-4.0, -58.0)]),
        _river("para", "ramal", [(-4.98, -60.75), (-4.5, -60.0)]),
        _river("tapajos", "afluente_mundo", [(-6.2, -60.48), (-5.8, -60.72), (-5.0, -61.0)]),
        _river("atacuari", "afluente_paso", [(-5.0, -59.5)], solo_boca=True),
        _river("itaya", "rio_local", [(-5.6, -60.3), (-5.03, -60.25)]),
    ]

    def build(self, hr_fixture) -> tuple:
        with tempfile.TemporaryDirectory() as tmp:
            base = hr_fixture(tmp)
            for ext in ("shp", "shx", "dbf", "cpg"):
                base.with_suffix(f".{ext}").rename(Path(tmp) / f"{geometry.HR_LAYER}.{ext}")
            _land_layer(Path(tmp) / geometry.NE_LAND_LAYER, [CONTINENT_CW])
            return geometry.production_geometry(_fixture_source(self.RIVERS), Path(tmp))

    def test_mouths_are_host_vertices_and_the_branch_starts_on_the_main(self):
        geo, errors = self.build(_hr_fixture)
        self.assertEqual([], errors)
        self.assertEqual("produccion", geo.kind)
        src = {rid: s for rid, (s, _) in geo.lines.items()}
        self.assertEqual({"amazonas": "hydrorivers", "para": "rio_amazonas", "tapajos": "hydrorivers",
                          "atacuari": "hydrorivers", "itaya": "rio_amazonas"}, src)
        main = geo.lines["amazonas"][1]
        frame = geometry.Frame(-18.0, 7.0, -80.0, -47.0, -5.0)
        self.assertEqual(frame.quant(-4.0, -58.0), main[-1], "la cola de la investigacion (el estuario)")
        self.assertIn(geo.lines["tapajos"][1][-1], main, "la boca, un vertice del cauce que no se simplifica")
        self.assertIn(geo.lines["atacuari"][1][-1], main, "solo_boca: sube desde un vertice del cauce")
        para = geo.lines["para"][1]
        self.assertIn(para[0], main, "el ramal empieza en el vertice del cauce mas cercano a su primer punto")
        self.assertEqual(frame.quant(-4.98, -60.75), para[1], "y despues, sus puntos de la investigacion")
        self.assertEqual([frame.quant(-5.6, -60.3), frame.quant(-5.03, -60.25)], geo.lines["itaya"][1],
                         "las lineas de la investigacion, tal cual")
        verts, tris, zones = geo.land
        self.assertEqual([{"id": "land", "tile": "canopy", "first": 0, "count": len(tris) // 3}], zones)
        self.assertEqual([], geometry.land_problems(verts, tris, zones))

    def test_hydrorivers_mouth_off_its_host_vertex_fails_and_is_taken_to_it(self):
        geo, errors = self.build(_hr_fixture_off_vertex)
        self.assertEqual(1, len(errors), errors)
        self.assertTrue(errors[0].startswith("linea tapajos: su boca de HydroRIVERS no es un vertice de amazonas (0.11 km)"),
                        errors)
        self.assertIn(geo.lines["tapajos"][1][-1], geo.lines["amazonas"][1], "la boca se lleva al vertice mas cercano")


class HydroRiversTraceTests(unittest.TestCase):
    """Trazado por la red de HydroRIVERS (M5.1) con un fixture."""

    @classmethod
    def setUpClass(cls):
        cls.tmp = tempfile.TemporaryDirectory()
        cls.frame = geometry.Frame(-18.0, 7.0, -80.0, -47.0, -5.0)
        cls.hr = geometry.HydroRivers(_hr_fixture(cls.tmp.name), cls.frame)
        cls.index = {rid: i for i, rid in enumerate(cls.hr.ids)}

    @classmethod
    def tearDownClass(cls):
        cls.hr.close()
        cls.tmp.cleanup()

    def reaches(self, *ids):
        return {self.index[i] for i in ids}

    def test_main_goes_to_the_sea_and_takes_the_research_tail(self):
        ctrl = [(-5.0, -62.0), (-5.02, -60.5), (-5.0, -59.3), (-4.5, -58.5), (-4.0, -58.0)]
        pts, spans, joined = geometry._trace_hr(self.hr, "m", {}, ctrl, None)
        self.assertFalse(joined)
        self.assertEqual((-5.0, -62.0), pts[0])
        self.assertEqual((-5.0, -59.0), pts[-1])
        self.assertEqual([self.index[i] for i in (1, 2, 3, 12)], [s[0] for s in spans])
        tail_pts, tail_spans, tail = geometry._research_tail(pts, spans, ctrl)
        self.assertEqual(2, tail, "los dos puntos del estuario, a mas de 15 km de la red")
        self.assertEqual([(-4.5, -58.5), (-4.0, -58.0)], tail_pts[-2:])
        cut = pts.index((-5.0, -59.5))
        self.assertEqual(pts[:cut + 1], tail_pts[:-2], "corta en el vertice mas cercano al ultimo punto de control bueno")
        self.assertEqual([self.index[i] for i in (1, 2, 3)], [s[0] for s in tail_spans], "el tramo 12 queda fuera")

    def test_head_with_more_control_points_wins_over_a_nearer_or_bigger_one(self):
        # D (tramo 9) nace en la misma cabecera, tiene mas cuenca y llega al cauce, pero no pasa por el punto medio ni por
        # la boca; E (10) va a otro mar. Gana T, que pasa por los tres puntos de control.
        ctrl = [(-6.2, -60.48), (-5.8, -60.72), (-5.0, -61.0)]
        pts, spans, joined = geometry._trace_hr(self.hr, "t", {}, ctrl, self.reaches(1, 2, 3, 12))
        self.assertTrue(joined)
        self.assertEqual([self.index[4], self.index[5]], [s[0] for s in spans])
        self.assertEqual((-5.0, -61.0), pts[-1], "la boca es el primer vertice de M2: un vertice del anfitrion")
        self.assertEqual((-6.21, -60.5), pts[0])

    def test_solo_boca_climbs_by_the_biggest_basin(self):
        pts, spans, joined = geometry._trace_hr(self.hr, "s", {"solo_boca": True}, [(-5.0, -59.5)],
                                                self.reaches(1, 2, 3, 12))
        self.assertTrue(joined)
        self.assertEqual([self.index[6], self.index[8]], [s[0] for s in spans], "el 6 (10 km2) y no el 7 (3 km2)")
        self.assertEqual([(-4.0, -59.8), (-4.5, -59.9), (-5.0, -59.5)], pts)

    def test_near_and_downstream(self):
        near = self.hr.near(-5.0, -61.0, 1.0)
        self.assertEqual({self.index[1], self.index[2], self.index[5]}, set(near))
        path, hit = self.hr.downstream(self.index[4], self.reaches(1, 2, 3, 12))
        self.assertEqual(([self.index[4], self.index[5]], self.index[2]), (path, hit))
        path, hit = self.hr.downstream(self.index[10], None)
        self.assertEqual(-1, hit)


class SimplifyTests(unittest.TestCase):
    def test_douglas_peucker_keeps_ends_protected_and_within_tolerance(self):
        # Un meandro de amplitud 1 km en un tramo recto de ~110 km de longitud a lo largo del ecuador.
        pts = [(0.0, -60.0 + i * 0.01) for i in range(101)]
        pts[50] = (0.009, pts[50][1])   # ~1 km al norte
        pts[20] = (0.001, pts[20][1])   # ~0,11 km
        idx = geometry.simplify(pts, [2.0] * 101, set())
        self.assertEqual([0, 100], idx, "a 2 km (vista Cuenca) basta la recta")
        idx = geometry.simplify(pts, [0.3] * 101, set())
        self.assertIn(50, idx, "a 0,3 km (vista Mundo) queda el meandro de 1 km")
        self.assertNotIn(20, idx)
        idx = geometry.simplify(pts, [2.0] * 101, {77})
        self.assertEqual([0, 77, 100], idx, "los vertices obligatorios (bocas, cortes) quedan")
        tol = [2.0] * 101
        tol[50] = 0.3   # el meandro, en la zona de la vista Mundo
        self.assertIn(50, geometry.simplify(pts, tol, set()))


class LandTests(Base):
    def test_clip_ring_by_a_half_plane(self):
        square = [(0.0, 0.0), (2.0, 0.0), (2.0, 2.0), (0.0, 2.0)]   # sentido positivo
        self.assertEqual([square], geometry.clip_ring(square, 0, -1.0, 1))
        self.assertEqual([], geometry.clip_ring(square, 0, 3.0, 1))
        rings = geometry.clip_ring(square, 0, 1.0, 1)   # x >= 1
        self.assertEqual(1, len(rings))
        self.assertEqual(2.0, geometry._ring_area(rings[0]))
        # Una U (abierta hacia arriba) cortada por y <= 1: dos patas, dos anillos.
        u = [(0.0, 0.0), (3.0, 0.0), (3.0, 2.0), (2.0, 2.0), (2.0, 0.5), (1.0, 0.5), (1.0, 2.0), (0.0, 2.0)]
        self.assertGreater(geometry._ring_area(u), 0)
        rings = geometry.clip_ring(u, 1, 1.0, -1)
        self.assertEqual(1, len(rings), "por abajo, la U entera es una pieza")
        self.assertAlmostEqual(3.0 * 1.0 - 1.0 * 0.5, geometry._ring_area(rings[0]))
        rings = geometry.clip_ring(u, 1, 1.0, 1)   # y >= 1: las dos patas
        self.assertEqual(2, len(rings))
        self.assertEqual([1.0, 1.0], sorted(geometry._ring_area(r) for r in rings))

    def test_triangulate_square_concave_and_collinear_without_split_vertices(self):
        for poly in ([(0, 0), (10, 0), (10, 10), (0, 10)],
                     [(0, 0), (10, 0), (10, 10), (5, 3), (0, 10)],
                     [(0, 0), (5, 0), (10, 0), (10, 10), (0, 10)],
                     [(0, 0), (5, 0), (10, 0), (10, 5), (10, 10), (5, 10), (0, 10), (0, 5)]):
            with self.subTest(poly=poly):
                tris = geometry.triangulate(poly)
                zones = [{"id": "land", "first": 0, "count": len(tris) // 3}]
                self.assertEqual([], geometry.land_problems(poly, tris, zones))
                self.assertEqual(len(poly) - 2, len(tris) // 3, "todos los vertices, tambien los alineados")

    def test_land_problems_reports_crossings_overlaps_and_split_vertices(self):
        self.assertTrue(geometry.polygon_problems([(0, 0), (10, 10), (10, 0), (0, 10)]))
        square = [(0, 0), (10, 0), (10, 10), (0, 10)]
        one = [{"id": "land", "first": 0, "count": 2}]
        self.assertEqual([], geometry.land_problems(square, [0, 1, 2, 0, 2, 3], one))
        self.assertIn("3 aristas dirigidas en mas de un triangulo (triangulos solapados)",
                      geometry.land_problems(square, [0, 1, 2, 0, 1, 2], one))
        # El vertice (5, 0) parte la arista (0,0)-(10,0) de un lado y no del otro (el triangulo de abajo no lo usa).
        split = [(0, 0), (10, 0), (10, 10), (0, 10), (5, 0), (5, -5)]
        problems = geometry.land_problems(split, [0, 1, 2, 0, 2, 3, 0, 5, 4, 4, 5, 1],
                                          [{"id": "land", "first": 0, "count": 4}])
        self.assertIn("la costa pasa dos veces por el vertice (0, 0)", problems, "un vertice que solo ve un lado")
        self.assertIn("las zonas no cubren todos los triangulos",
                      geometry.land_problems(square, [0, 1, 2, 0, 2, 3], [{"id": "land", "first": 0, "count": 1}]))

    def test_land_of_the_file_is_the_continent_and_its_islands(self):
        land = self.doc["land"]
        verts = geometry.decode_points(land["verts"])
        tris = geometry.decode_indices(land["tris"])
        self.assertEqual([], geometry.land_problems(verts, tris, land["zones"]))
        self.assertEqual([{"id": "land", "tile": "canopy", "first": 0, "count": len(tris) // 3}], land["zones"],
                         "una sola zona hasta la mascara del artista (land_zones)")

        def inside(p):
            return any(geometry._in_triangle(p, *(verts[i] for i in tris[k:k + 3])) for k in range(0, len(tris), 3))
        frame = self.model.frame
        self.assertTrue(inside(frame.quant(-3.1189, -60.0217)), "Manaos")
        self.assertTrue(inside(frame.quant(-1.0, -49.5)), "Marajo, una isla")
        self.assertFalse(inside(frame.quant(5.0, -48.0)), "el Atlantico")
        self.assertFalse(inside(frame.quant(-10.0, -79.5)), "el Pacifico")
        self.assertEqual([("land", "canopy", ["a"])], geometry.land_zones(["a"]))


class LinesTests(Base):
    def test_kinds_classes_and_sources(self):
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
            self.assertEqual(geometry.LINE_SRC.get(line["id"], "hydrorivers"), line["src"], line["id"])
        self.assertEqual({"para": "rio_amazonas", "casiquiare": "ne10m", "itaya": "rio_amazonas",
                          "pacaya": "rio_amazonas"}, geometry.LINE_SRC)

    def test_production_lines_are_finer_than_the_research(self):
        # El cauce de HydroRIVERS con los meandros: mas puntos que los 105 de la investigacion, y mas largo; simplificado por
        # vista, dentro de los 300-800 que estimaba la propuesta §11.3 (revision de M5.1: con 0,3 y 2 km eran 1.213).
        main = self.model.lines["amazonas"]
        self.assertTrue(300 <= len(main.q) <= 800, len(main.q))
        self.assertGreater(main.arc_km[-1], 5500, "la poligonal de la investigacion mide 4.783 km (propuesta §3.6)")
        self.assertEqual(6575.0, main.km_from_sea[0])
        self.assertEqual(0.0, main.km_from_sea[-1])
        self.assertTrue(all(a >= b for a, b in zip(main.km_from_sea, main.km_from_sea[1:])), "los km bajan rio abajo")

    def test_local_rivers_of_stops_11_13_15_16_18(self):
        for r in self.records:
            if r["k"] in (11, 13, 15, 16, 18):
                self.assertTrue(r.get("rios_locales"), r["id"])
                for rid in r["rios_locales"]:
                    with self.subTest(stop=r["k"], river=rid):
                        self.assertEqual("local", self.lines[rid]["kind"])

    def test_local_rivers_of_201_420_with_their_hosts(self):
        # M4.5a: rios_locales_201_700 de la investigacion, con el anfitrion de su boca_en (propuesta §5.4 bis y ter).
        hosts = {"aguarico": "napo", "cuyabeno": "aguarico", "tambopata": "madre_de_dios", "manu": "madre_de_dios"}
        ids = [line["id"] for line in self.doc["lines"]]
        self.assertEqual(31, len(ids), "27 de 1-200 y 4 locales de 201-420")
        self.assertEqual(list(hosts), ids[-4:], "despues de los de rios, en el orden de rios_locales_201_700")
        for rid, host in hosts.items():
            with self.subTest(river=rid):
                line = self.lines[rid]
                self.assertEqual("local", line["kind"])
                self.assertEqual(3, line["class"])
                self.assertEqual(host, line["mouth"]["on"])
                self.assertEqual(1.0, line["fill_on_pass"])
        self.assertEqual("black", self.lines["cuyabeno"]["water"][0]["type"], "laguna de agua negra")
        self.assertEqual("white", self.lines["aguarico"]["water"][0]["type"])

    def test_hydrorivers_mouths_are_vertices_of_their_host(self):
        for rid, line in self.model.lines.items():
            if line.src != "hydrorivers" or line.host is None or self.model.lines[line.host].src == "rio_amazonas":
                continue
            with self.subTest(line=rid):
                self.assertIn(line.q[-1], self.model.lines[line.host].q)
                self.assertLess(geometry.haversine(line.ll[-1], self.model.lines[line.host].point_at_s(line.host_s)), 0.01)

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
                self.assertAlmostEqual(km, self.model.km_at("amazonas", self.lines[lid]["mouth"]["s"]), delta=5.0)
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
        self.assertEqual(18, len(passing), "17 de 1-200 y teotonio (M4.5a)")
        self.assertEqual("madeira", passing["teotonio"]["line"], "el relevo del guia del viaje del 300 (Q18-A, P-M8)")
        self.assertEqual("map.stop.teotonio.dato_01", passing["teotonio"]["fact"])
        self.assertEqual("teotonio", self.doc["passing"][-1]["id"], "despues de los de 1-200")
        self.assertEqual("map.stop.obidos.dato_01", passing["obidos"]["fact"])
        self.assertEqual("para", passing["breves"]["line"])
        # En la union con su anfitrion, el punto va en el anfitrion (M5.1): Gurupa, en el arranque del ramal, y las bocas.
        for pid in ("gurupa", "boca_del_xingu", "boca_del_madeira", "boca_del_purus", "santo_antonio_do_ica", "boca_del_napo"):
            self.assertEqual("amazonas", passing[pid]["line"], pid)


class ViewTests(Base):
    """Tolerancias por vista (propuesta §3.6 y §11.3, punto 4; M5.1)."""

    def test_tolerances_and_simplification_of_each_view(self):
        self.assertEqual((15.0, 1.0), (geometry.TOLERANCE_KM, geometry.MUNDO_TOLERANCE_KM))
        self.assertEqual((6.0, 1.0), (geometry.DP_CUENCA_KM, geometry.DP_MUNDO_KM))
        # Revision de M5.1: la simplificacion de la vista Mundo no aparta la linea mas que la tolerancia de esa vista, y la
        # de la Cuenca queda muy por debajo de la suya (6 km son ~0,7 dp).
        self.assertEqual(geometry.MUNDO_TOLERANCE_KM, geometry.DP_MUNDO_KM)
        self.assertLess(geometry.DP_CUENCA_KM, geometry.TOLERANCE_KM / 2)
        zone = geometry.MundoZone(self.source)
        self.assertTrue(zone.contains(-3.1189, -60.0217), "Manaos, una parada")
        self.assertFalse(zone.contains(-15.512, -71.702), "el Mismi: El origen no es de la 1.0")

    def test_every_control_point_within_its_view_or_its_measured_exception(self):
        zone = geometry.MundoZone(self.source)
        used = 0
        for rid in self.model.order:
            line = self.model.lines[rid]
            for j, p in enumerate(line.source["puntos"]):
                d, view, allowed, pending = geometry.control_check(line, j, p, zone)
                with self.subTest(line=rid, point=j):
                    self.assertLessEqual(d, allowed)
                    if pending:
                        canon = geometry.MUNDO_TOLERANCE_KM if view == "Mundo" else geometry.TOLERANCE_KM
                        self.assertGreater(d, canon, "una excepcion que ya no hace falta")
                        self.assertLess(allowed - d, 0.1, "la excepcion no es la cifra medida")
                        used += 1
        self.assertEqual(71, used)
        self.assertEqual(71, sum(len(v) for v in geometry.PENDING_CONTROL_KM.values()))
        self.assertEqual([("amazonas", 84)], [(rid, j) for rid, items in geometry.PENDING_CONTROL_KM.items()
                                               for j, km in items.items() if km > geometry.TOLERANCE_KM
                                               and not zone.contains(*(lambda p: (p["lat"], p["lon"]))(
                                                   self.model.lines[rid].source["puntos"][j]))])

    def test_research_lines_have_no_exceptions(self):
        for rid, src in geometry.LINE_SRC.items():
            if src == "rio_amazonas":
                self.assertNotIn(rid, geometry.PENDING_CONTROL_KM)


class FileTests(Base):
    def test_committed_file_is_what_its_geometry_gives(self):
        self.assertEqual(geometry.render(self.doc).encode("utf-8"), MAP_PATH.read_bytes())

    def test_utf8_without_bom_with_lf_under_90_kb(self):
        data = MAP_PATH.read_bytes()
        self.assertFalse(data.startswith(b"\xef\xbb\xbf"))
        self.assertNotIn(b"\r", data)
        self.assertTrue(data.endswith(b"\n"))
        self.assertLessEqual(len(data), geometry.MAX_BYTES, "<= 90 KB (M5.1; 08 §4.5)")
        self.assertEqual(90 * 1024, geometry.MAX_BYTES)

    def test_main_fails_when_the_file_would_pass_max_bytes(self):
        # Revision de M5.1: main falla (y no escribe) si el documento pasa de MAX_BYTES.
        out = io.StringIO()
        with mock.patch.object(geometry, "MAX_BYTES", 1000), contextlib.redirect_stdout(out):
            code = geometry.main(["--check", "--sources", str(NO_SOURCES)])
        self.assertEqual(1, code)
        self.assertIn(f"FAIL  river_map_v1.json mediria {len(MAP_PATH.read_bytes())} bytes (maximo 1000, M5.1)",
                      out.getvalue())

    def test_check_without_downloads_is_deterministic_with_different_hash_seeds(self):
        for seed in ("0", "1", "12345"):
            env = dict(os.environ, PYTHONHASHSEED=seed)
            proc = run_geometry("--check", "--sources", str(NO_SOURCES), env=env)
            self.assertEqual(0, proc.returncode, proc.stdout)
            self.assertIn(b"(commiteado;", proc.stdout)


@unittest.skipUnless(have_sources(), "sin las descargas de Natural Earth y HydroRIVERS en tools/river-map/source/")
class ProductionTests(unittest.TestCase):
    """Con las descargas (escritorio): la geometria de produccion se regenera igual, byte a byte y con cualquier semilla."""

    def test_check_regenerates_from_the_downloads(self):
        proc = run_geometry("--check")
        self.assertEqual(0, proc.returncode, proc.stdout)
        self.assertIn(b"(produccion;", proc.stdout)

    def test_deterministic_bytes_with_different_hash_seeds(self):
        outs = []
        with tempfile.TemporaryDirectory() as tmp:
            for seed in ("0", "12345"):
                out = Path(tmp) / f"map_{seed}.json"
                env = dict(os.environ, PYTHONHASHSEED=seed)
                proc = run_geometry("--out", str(out), env=env)
                self.assertEqual(0, proc.returncode, proc.stdout)
                outs.append(out.read_bytes())
        self.assertEqual(1, len(set(outs)))
        self.assertEqual(MAP_PATH.read_bytes(), outs[0])


class RouteTests(Base):
    def test_seventy_stops_with_at_and_leg(self):
        self.assertEqual(70, len(self.stops["stops"]), "1-700 (M4.5b)")
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

    def test_leg_of_300_goes_down_the_napo_and_up_the_madeira_past_teotonio(self):
        # Viaje del 300 (propuesta §5.4 ter): Napo abajo, Amazonas abajo (repaso), Madeira, Beni y Madre de Dios.
        leg = self.by_n[30]["leg"]
        self.assertEqual(["napo", "amazonas", "madeira", "beni", "madre_de_dios"], [p["line"] for p in leg["path"]])
        self.assertIn("boca_del_madeira", leg["via"])
        self.assertEqual("teotonio", leg["via"][-1])
        self.assertLess(leg["via"].index("boca_del_madeira"), leg["via"].index("teotonio"))
        # El desvio a Cuyabeno es una ida y vuelta por las confluencias del Aguarico.
        self.assertEqual(["napo", "aguarico", "cuyabeno"], [p["line"] for p in self.by_n[25]["leg"]["path"]])
        self.assertEqual(["cuyabeno", "aguarico", "napo"], [p["line"] for p in self.by_n[27]["leg"]["path"]])
        self.assertEqual(["tambopata", "madre_de_dios"], [p["line"] for p in self.by_n[38]["leg"]["path"]])

    def test_legs_of_421_700_down_one_river_and_up_another(self):
        # Propuesta del Mapa §5.4 quater y quinquies (M4.5b): el Beni, el Beni abajo y el Mamore arriba por Villa Bella
        # (500), el Mamore y el Itenez (510), el Itenez abajo y el Mamore (530), y el viaje del 560 por el Madeira y el
        # cauce hasta el Ucayali.
        lines = {n: [p["line"] for p in self.by_n[n]["leg"]["path"]] for n in range(42, 70)}
        self.assertEqual(["manu", "madre_de_dios", "beni"], lines[42],
                         "Riberalta, en la boca del Madre de Dios (confluence_s [P]): acaba con un tramo de longitud cero")
        self.assertEqual(self.by_n[42]["leg"]["path"][-1]["s0"], self.by_n[42]["leg"]["path"][-1]["s1"])
        # Todo viaje acaba en la linea de su parada (el cliente lo espera: RiverMapReaderTests).
        for s in self.stops["stops"]:
            self.assertEqual(s["at"], {"line": s["leg"]["path"][-1]["line"], "s": s["leg"]["path"][-1]["s1"]}, s["id"])
        self.assertEqual(["beni", "madeira", "mamore"], lines[50],
                         "Beni abajo y Mamore arriba; en HydroRIVERS, por un tramo de ~0,5 km del Madeira (M5.1)")
        self.assertEqual(["mamore", "itenez"], lines[51])
        self.assertEqual(["itenez", "mamore"], lines[53])
        self.assertEqual(["mamore", "madeira", "amazonas"], lines[56])
        leg56 = self.by_n[56]["leg"]
        self.assertEqual("teotonio", leg56["via"][0], "Madeira abajo: los rapidos de Teotonio, antes que su boca")
        self.assertLess(leg56["via"].index("boca_del_madeira"), leg56["via"].index("tamshiyacu"))
        self.assertEqual("madeira", leg56["path"][1]["line"])
        self.assertEqual(1.0, leg56["path"][1]["s1"], "el Madeira hasta su boca")
        # El Beni y el Mamore entran en el Madeira junto a Villa Bella, a un tramo de HydroRIVERS el uno del otro.
        beni, mamore = self.lines["beni"]["mouth"], self.lines["mamore"]["mouth"]
        self.assertEqual(("madeira", "madeira"), (beni["on"], mamore["on"]))
        self.assertLess(abs(beni["s"] - mamore["s"]), 0.002)
        self.assertLess(geometry.haversine(self.model.lines["beni"].ll[-1], self.model.lines["mamore"].ll[-1]), 1.5)

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
                # En su km, salvo las paradas en una confluencia, en la boca (confluence_s [P]): a menos de TOLERANCE_KM.
                delta = geometry.TOLERANCE_KM if s["n"] in CONFLUENCE_STOPS else 1.0
                self.assertAlmostEqual(s["km_from_sea"], self.model.km_at(s["at"]["line"], s["at"]["s"]), delta=delta)
            pos = self.model.lines[s["at"]["line"]].point_at_s(s["at"]["s"])
            limit = geometry.PENDING_SITE_KM.get(s["id"], geometry.TOLERANCE_KM)
            self.assertLessEqual(geometry.haversine((rec["lat"], rec["lon"]), pos), limit, s["id"])
            # Dentro de un mundo los km no retroceden; el viaje de entrada de los mundos 6 a 9 deshace camino (§5.4 bis). En
            # el mundo 8 bajan dos veces, por una confluencia y a otro rio, en las paradas con baja_por_confluencia [P]
            # (Guayaramerin y Trinidad; propuesta §5.4 quater, pregunta abierta del duenno).
            if prev is not None and prev["km_from_sea"] is not None and prev["world"] == s["world"]:
                if "baja_por_confluencia" in rec:
                    self.assertLess(s["km_home"], prev["km_home"], s["id"])
                    self.assertNotEqual(prev["at"]["line"], s["at"]["line"], s["id"])
                else:
                    self.assertGreaterEqual(s["km_from_sea"], prev["km_from_sea"], s["id"])
                    self.assertGreaterEqual(s["km_home"], prev["km_home"], s["id"])
            prev = s
        self.assertEqual(["guayaramerin", "trinidad"],
                         [s["id"] for s in self.stops["stops"]
                          if "baja_por_confluencia" in next(r for r in self.records if r["k"] == s["n"])])

    def test_tolerance_is_the_canon_and_exceptions_are_only_the_pending_ones(self):
        # 08 §4.5 y D-023, punto 20: <= 2 dp de su vista, <= 15 km en la Cuenca. Las excepciones son las medidas que esperan
        # la firma del duenno (sin margen): ninguna nueva sin pasar por el. M5.1: medidas sobre la geometria de produccion.
        self.assertEqual(15.0, geometry.TOLERANCE_KM)
        self.assertEqual({"flona_tapajos": 33.3, "amacayacu": 58.6, "pacaya_samiria": 78.6, "pacaya_alba": 78.6,
                          "yasuni": 29.0, "yasuni_dosel": 29.0}, geometry.PENDING_SITE_KM)
        self.assertEqual({"pacaya": 23.9}, geometry.PENDING_MOUTH_KM)
        for s in self.stops["stops"]:
            if s["id"] not in geometry.PENDING_SITE_KM:
                continue
            rec = next(r for r in self.records if r["k"] == s["n"])
            d = geometry.haversine((rec["lat"], rec["lon"]), self.model.lines[s["at"]["line"]].point_at_s(s["at"]["s"]))
            self.assertGreater(d, geometry.TOLERANCE_KM, f"{s['id']}: ya cumple el canon; sobra su excepcion")
            self.assertLess(geometry.PENDING_SITE_KM[s["id"]] - d, 0.1, f"{s['id']}: la excepcion no es la cifra medida")

    def test_stops_at_a_confluence_sit_on_its_mouth(self):
        # [P] Revision de M5.1 (geometry.confluence_s): una parada cuyo sitio queda a <= CONFLUENCE_KM de la boca de un
        # afluente de su linea segun la investigacion se ancla en la s de esa boca. Son exactamente estas.
        self.assertEqual(5.0, geometry.CONFLUENCE_KM)
        got = {}
        for s in self.stops["stops"]:
            for cid in self.model.order:
                child = self.model.lines[cid]
                if child.host == s["at"]["line"] and child.kind != "main" and child.host_s == s["at"]["s"]:
                    got[s["n"]] = cid
        self.assertEqual(CONFLUENCE_STOPS, got)
        # Al llegar se cruza la boca y su afluente se llena (como con el respaldo): Santarem llena el Tapajos y Boca Manu
        # el Manu; y el viaje siguiente sale por el afluente, sin tramos de metros del anfitrion.
        self.assertEqual(["xingu", "tapajos"], self.by_n[1]["leg"]["mouths"])
        self.assertEqual(["manu"], self.by_n[38]["leg"]["mouths"])
        for n, first in ((2, "tapajos"), (5, "negro"), (18, "maranon"), (35, "tambopata"), (40, "manu")):
            self.assertEqual(first, self.by_n[n]["leg"]["path"][0]["line"], n)
        # Sin la regla, el punto por km de Santarem no cae en la boca del Tapajos (con la primera geometria de M5.1 quedaba
        # ~3 km rio abajo y la canoa, que sube, se paraba antes de ella).
        rec = next(r for r in self.records if r["k"] == 1)
        s_km = self.model.s_at_km("amazonas", float(rec["km_desde_el_mar"]), slack_km=geometry.TOLERANCE_KM)
        tapajos = self.model.lines["tapajos"].host_s
        self.assertNotEqual(tapajos, geometry._round_s(s_km))
        self.assertEqual(tapajos, geometry.confluence_s(self.model, rec, "amazonas", s_km))
        far = dict(rec, lat=rec["lat"] + 0.09)   # ~10 km: ya no es la confluencia
        self.assertIsNone(geometry.confluence_s(self.model, far, "amazonas", s_km))
        mouth = self.model.lines["tapajos"].source["puntos"][-1]
        at_mouth = dict(rec, lat=mouth["lat"], lon=mouth["lon"])
        self.assertIsNone(geometry.confluence_s(self.model, at_mouth, "amazonas", 0.5),
                          "su punto por km, a mas de TOLERANCE_KM de la boca: no se ancla")

    def test_km_anchors_never_go_back_up_the_river(self):
        # Revision de M5.1: Careiro da Varzea (orilla sur, a 9,5 km del cauce) se proyecta rio arriba del Encuentro; no
        # ancla los km, y la boca del Negro queda en el km 1.437 de la investigacion (no en el 1.419).
        main = self.model.lines["amazonas"]
        anchored = [s for _, s in main.anchored]
        self.assertEqual(sorted(anchored), anchored)
        self.assertAlmostEqual(1437.0, self.model.km_at("amazonas", self.lines["negro"]["mouth"]["s"]), delta=0.5)

    def test_same_place_only_at_a_confluence(self):
        tapajos = self.lines["tapajos"]["mouth"]["s"]
        self.assertTrue(geometry.same_place(self.model, ("tapajos", 1.0), ("amazonas", tapajos)))
        self.assertTrue(geometry.same_place(self.model, ("amazonas", tapajos), ("tapajos", 1.0)))
        self.assertFalse(geometry.same_place(self.model, ("tapajos", 0.5), ("amazonas", tapajos)))
        self.assertFalse(geometry.same_place(self.model, ("negro", 1.0), ("amazonas", tapajos)))
        self.assertFalse(geometry.same_place(self.model, ("itenez", 1.0), ("beni", 1.0)), "el Itenez entra en el Mamore")
        self.assertFalse(geometry.same_place(self.model, ("negro", 1.0), ("tapajos", 1.0)), "bocas en otro punto del cauce")

    def test_same_place_of_two_mouths_at_the_same_point(self):
        # M4.5b: dos bocas en el mismo punto del mismo anfitrion son el mismo sitio (con el respaldo, el Beni y el Mamore en
        # Villa Bella; en HydroRIVERS entran a un tramo de distancia, test_legs_of_421_700_...).
        doc, model, errors = geometry.build_map(self.source)
        self.assertEqual(["linea pacaya: su boca queda a 37.1 km de amazonas (maximo 23.9 km: excepcion medida pendiente "
                          "del duenno; el canon es 15 km): mouth fuera de su rio"], errors,
                         "con el respaldo, la boca del Pacaya queda mas lejos que la cifra medida de produccion")
        self.assertTrue(geometry.same_place(model, ("beni", 1.0), ("mamore", 1.0)))
        self.assertFalse(geometry.same_place(model, ("beni", 0.9), ("mamore", 1.0)))

    def test_in_world_rules_of_the_confluence_drops(self):
        # Propuesta §5.4 quater (M4.5b): los km a casa solo bajan dentro de un mundo con baja_por_confluencia y a otro rio;
        # nunca rio abajo de la ultima parada del mundo en el mismo rio.
        records = copy.deepcopy(self.records)
        by_k = {r["k"]: r for r in records}
        del by_k[50]["baja_por_confluencia"]
        errors = geometry.validate_routes(self.stops, self.model, records)
        self.assertEqual(["parada n=50 ('guayaramerin'): km_home 2518 baja dentro del mundo 8 (la parada anterior, 3007) "
                          "sin baja_por_confluencia en la investigacion"], errors)
        records = copy.deepcopy(self.records)
        next(r for r in records if r["k"] == 51)["baja_por_confluencia"] = "Mamore arriba"
        errors = geometry.validate_routes(self.stops, self.model, records)
        self.assertEqual(["parada n=51 ('noel_kempff'): baja_por_confluencia en una parada cuyos km a casa no bajan dentro "
                          "de su mundo: sobra"], errors)
        # La misma bajada en el rio de la parada anterior (la 54, en el Mamore como la 53): no hay confluencia.
        records = copy.deepcopy(self.records)
        next(r for r in records if r["k"] == 54)["baja_por_confluencia"] = "Mamore abajo"
        doc = copy.deepcopy(self.stops)
        doc["stops"][54]["km_home"] -= 100
        doc["stops"][55]["km_home"] -= 100
        errors = geometry.validate_routes(doc, self.model, records)
        self.assertEqual(["parada n=54 ('llanos_de_moxos'): km_home baja con baja_por_confluencia, pero en la misma linea "
                          "(mamore) que la parada anterior: no hay confluencia"], errors)
        # Trinidad (53) rio abajo de Guayaramerin (50), las dos en el Mamore: falla aunque entre medias vaya por el Itenez.
        doc = copy.deepcopy(self.stops)
        s = round(doc["stops"][50]["at"]["s"] + 0.01, 6)
        for n in (53, 54, 55):
            doc["stops"][n]["at"]["s"] = s
            doc["stops"][n]["leg"]["path"][-1]["s1"] = s
            if n > 53:
                doc["stops"][n]["leg"]["path"][0]["s0"] = s
        doc["stops"][56]["leg"]["path"][0]["s0"] = s
        errors = geometry.validate_routes(doc, self.model, self.records)
        self.assertIn("parada n=53 ('trinidad'): queda rio abajo por mamore de la parada 50, del mismo mundo (dentro de un "
                      "mundo los km no retroceden)", errors)

    def test_broken_leg_is_reported(self):
        doc = copy.deepcopy(self.stops)
        leg = doc["stops"][4]["leg"]
        leg["path"] = [leg["path"][1]]   # sin el Tapajos: salta de la FLONA al cauce por tierra
        errors = geometry.validate_routes(doc, self.model, self.records)
        self.assertTrue(any("leg roto" in e and "por tierra" in e for e in errors), errors)


class CheckTests(Base):
    """Fallos de --check en el modo «commiteado» (sin las descargas): el del portatil, y el mismo en cualquier maquina."""

    def check(self, source: dict | None = None, stops: dict | None = None, map_bytes: bytes | None = None):
        with tempfile.TemporaryDirectory() as tmp:
            args = ["--check", "--sources", str(NO_SOURCES)]
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
        self.assertIn("AVISO sin las descargas en", out)
        self.assertIn("31 lineas, 18 puntos de paso y 3 miradores", out)

    def test_stop_moved_20_km_fails(self):
        src = copy.deepcopy(self.source)
        rec = next(r for r in src["paradas_1_200"] if r["k"] == 14)
        rec["lat"] = round(rec["lat"] + 0.18, 4)   # unos 20 km al norte; el cauce pasa a 4,8 km de Iquitos
        self.assertFails("parada n=14 ('iquitos'): el sitio queda a 17.5", source=src)

    def test_inland_site_moved_20_km_away_from_its_river_fails(self):
        src = copy.deepcopy(self.source)
        rec = next(r for r in src["paradas_1_200"] if r["k"] == 10)
        rec["lat"] = round(rec["lat"] + 0.18, 4)   # unos 20 km mas al norte, mas lejos del Amazonas
        self.assertFails("parada n=10 ('amacayacu'): el sitio queda a 78.", source=src)

    def test_dock_point_is_checked_with_the_canon_tolerance(self):
        at = self.stops["stops"][10]["at"]
        lat, lon = self.model.lines[at["line"]].point_at_s(at["s"])
        src = copy.deepcopy(self.source)
        rec = next(r for r in src["paradas_1_200"] if r["k"] == 10)
        rec["atraque"] = {"lat": round(lat, 4), "lon": round(lon, 4)}
        code, out = self.check(source=src)
        self.assertEqual(0, code, out)
        self.assertIn("AVISO amacayacu: ya tiene atraque; sobra su excepcion", out)
        rec["atraque"]["lat"] = round(lat + 0.18, 4)   # el atraque, a unos 20 km: ya no vale la excepcion
        self.assertFails("parada n=10 ('amacayacu'): su punto de atraque queda a 20.0 km", source=src)
        code, out = self.check(source=src)
        self.assertIn("maximo 15 km, <= 2 dp en la vista Cuenca", out)

    def test_pending_exceptions_are_warned(self):
        code, out = self.check()
        self.assertEqual(0, code, out)
        for sid in geometry.PENDING_SITE_KM:
            self.assertIn(f"AVISO {sid}: sin atraque en la investigacion", out)
        self.assertIn("AVISO linea pacaya: su boca queda a 23.8 km de amazonas (canon 15 km; excepcion 23.9 km", out)
        self.assertIn("AVISO linea amazonas: 37 puntos de control fuera de la tolerancia de su vista, con excepcion medida "
                      "pendiente del duenno (PENDING_CONTROL_KM): 22 (1.2 km, Mundo)", out)
        self.assertIn("84 (33.7 km, Cuenca)", out)
        # Las lineas que siguen siendo el esbozo de la investigacion (revision de M5.1): una por linea, con su porque.
        for rid in ("para", "itaya", "pacaya"):
            self.assertIn(f"AVISO linea {rid}: sigue siendo el esbozo de la investigacion (src rio_amazonas", out)
        self.assertIn("Natural Earth (rivers_lake_centerlines) no tiene ningun rio Para", out)

    def test_pending_mouth_moved_further_fails(self):
        # La boca del Pacaya (su linea es el esbozo de la investigacion) ~11 km mas lejos del cauce: en la investigacion
        # falla su punto de control; en la geometria, su boca, mas alla de la cifra medida.
        src = copy.deepcopy(self.source)
        pacaya = next(r for r in src["rios"] if r["id"] == "pacaya")
        pacaya["puntos"][-1]["lat"] = round(pacaya["puntos"][-1]["lat"] + 0.2, 4)
        self.assertFails("lines[26] (pacaya): el punto de control 1 ('Boca (sistema del Ucayali-Puinahua)') queda a",
                         source=src)
        lines = dict(self.geo.lines)
        src_id, q = lines["pacaya"]
        lines["pacaya"] = (src_id, q[:-1] + [(q[-1][0] + 524, q[-1][1])])   # +0,2 grados de latitud
        _, _, errors = geometry.build_map(self.source, geometry.Geometry("prueba", lines, self.geo.land))
        self.assertTrue(any(e.startswith("linea pacaya: su boca queda a 29.8 km") and "(maximo 23.9 km: excepcion medida" in e
                            for e in errors), errors)

    def test_control_point_away_from_its_line_fails(self):
        # Vista Mundo (a <= 100 km de una parada): Tamshiyacu (45), a 0,17 km del cauce, llevado a 5,1 km.
        src = copy.deepcopy(self.source)
        pts = next(r for r in src["rios"] if r["id"] == "amazonas")["puntos"]
        pts[45]["lat"] = round(pts[45]["lat"] - 0.2, 4)
        self.assertFails("lines[0] (amazonas): el punto de control 45 ('Tamshiyacu') queda a 5.1 km de la linea (vista "
                         "Mundo: maximo 1 km, <= 2 dp en la vista Mundo)", source=src)
        # Vista Cuenca (lejos de las paradas): Amatura (60), llevado a 23,0 km.
        src = copy.deepcopy(self.source)
        pts = next(r for r in src["rios"] if r["id"] == "amazonas")["puntos"]
        pts[60]["lat"] = round(pts[60]["lat"] + 0.2, 4)
        self.assertFails("lines[0] (amazonas): el punto de control 60 ('Amatura') queda a 23.0 km de la linea (vista "
                         "Cuenca: maximo 15 km, <= 2 dp en la vista Cuenca)", source=src)

    def test_mouth_outside_its_river_fails(self):
        # La boca de la investigacion del Madeira, ~33 km al sur del cauce: su ultimo punto de control ya no esta en su linea.
        src = copy.deepcopy(self.source)
        madeira = next(r for r in src["rios"] if r["id"] == "madeira")
        madeira["puntos"][-1]["lat"] = round(madeira["puntos"][-1]["lat"] - 0.3, 4)
        self.assertFails("(madeira): el punto de control 11 ('Entra en el Amazonas') queda a", source=src)

    def test_stop_moved_along_the_river_fails_by_its_km(self):
        stops = copy.deepcopy(self.stops)
        stops["stops"][14]["km_from_sea"] += 20
        self.assertFails("km_from_sea 3720 y su punto del rio esta en el km 3696", stops=stops)

    def assertOnlyFailure(self, fragment: str, **kwargs):
        code, out = self.check(**kwargs)
        self.assertEqual(1, code, out)
        fails = [line for line in out.splitlines() if line.startswith("FAIL")]
        self.assertEqual(1, len(fails), out)
        self.assertIn(fragment, fails[0])

    def test_stop_going_back_down_the_river_fails(self):
        # Los km no retroceden (08 §4.5): la s de la parada 9 pasa por delante (rio abajo) de la de la parada 8, en la misma
        # linea; el leg se ajusta para que solo falle esta regla (el km sigue a menos de 15 km del suyo).
        stops = copy.deepcopy(self.stops)
        s = round(stops["stops"][8]["at"]["s"] + 0.00074, 6)
        stops["stops"][9]["at"]["s"] = s
        stops["stops"][9]["leg"]["path"][-1]["s1"] = s
        stops["stops"][10]["leg"]["path"][0]["s0"] = s
        self.assertOnlyFailure("parada n=9 ('leticia_atardecer'): retrocede rio abajo por amazonas desde la parada 8 "
                               "(los km no retroceden)", stops=stops)

    def test_km_from_sea_going_back_fails(self):
        # Los km no retroceden (08 §4.5): km_from_sea de la parada 9 por debajo del de la 8 en el mismo rio, aun dentro de
        # los 15 km de su punto del rio.
        stops = copy.deepcopy(self.stops)
        stops["stops"][9]["km_from_sea"] = stops["stops"][8]["km_from_sea"] - 10
        self.assertOnlyFailure("parada n=9 ('leticia_atardecer'): km_from_sea 3135 menor que el de la parada 8 (3145) en "
                               "el mismo rio", stops=stops)

    def test_broken_leg_fails(self):
        stops = copy.deepcopy(self.stops)
        stops["stops"][4]["leg"]["path"][1]["s0"] = 0.8   # el cauce ya no empieza en la boca del Tapajos
        self.assertFails("parada n=4 ('encontro'): leg roto entre path[0]", stops=stops)

    def test_missing_candidate_fails(self):
        src = copy.deepcopy(self.source)
        rec = next(r for r in src["paradas_1_200"] if r["id"] == "pacaya_samiria")
        rec["otros_datos"] = []
        self.assertFails("falta el candidato de la propuesta sec. 5.1 bis map.stop.pacaya_samiria.dato_03", source=src)

    def test_teotonio_with_another_coordinate_in_the_voyage_of_560_fails(self):
        # M4.5b: el teotonio de los viajes del 300 y del 560 es un solo elemento de passing[] (contrato 29).
        src = copy.deepcopy(self.source)
        world9 = next(w for w in src["paradas_201_1000"] if w.get("mundo") == 9)
        next(p for p in world9["puntos_de_paso_del_viaje"] if p.get("id") == "teotonio")["lon"] = -64.2
        self.assertFails("puntos_de_paso_del_viaje: teotonio del mundo 9 tiene otro lon que el del mundo 7", source=src)

    def test_drift_fails(self):
        data = MAP_PATH.read_bytes().replace(b'"reveal_on": 10', b'"reveal_on": 20', 1)
        self.assertFails("desfase", map_bytes=data)

    def test_land_with_a_broken_triangulation_fails(self):
        doc = json.loads(MAP_PATH.read_bytes().decode("utf-8"))
        tris = geometry.decode_indices(doc["land"]["tris"])
        tris[0], tris[1] = tris[1], tris[0]   # el primer triangulo, al reves
        doc["land"]["tris"] = geometry.encode_indices(tris)
        self.assertFails("land: el triangulo 0 es degenerado o va en sentido contrario",
                         map_bytes=geometry.render(doc).encode("utf-8"))

    def test_validate_map_reports_a_main_line_too_short(self):
        src = copy.deepcopy(self.source)
        pts = next(r for r in src["rios"] if r["id"] == "amazonas")["puntos"]
        pts[40]["km_desde_el_mar"] = pts[41]["km_desde_el_mar"] + 1   # el tramo mide 1 km y la linea recta mas
        doc, model, errors = geometry.build_map(src)
        errors += geometry.validate_map(doc, model, src)
        self.assertTrue(any("mide 1 km y la linea recta" in e for e in errors), errors)


if __name__ == "__main__":
    unittest.main()
