#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""geometry.py - Mapa del rio (D-013): genera la geometria af-river-map/1 con el respaldo de la investigacion.

Lee docs/research/mapa-del-rio/rio-amazonas.json (sin modificarlo) y escribe Assets/_Project/Data/Map/river_map_v1.json
(contrato 29; 08 §4.5 con los extras [P] de P-M1, D-023). Mientras no lleguen Natural Earth y HydroRIVERS (D-013, punto 5,
Q11-A: los descarga el duenno; M5.1), la geometria sale de los puntos de la investigacion: src = rio_amazonas, la forma
correcta a escala de cuenca y sin meandros (propuesta del Mapa §3.6 y §11.3, punto 7). Solo biblioteca estandar de
Python 3.14 (08 §4.5).

Uso (desde la raiz del repositorio):
    py -3.14 tools/river-map/geometry.py            # escribe river_map_v1.json
    py -3.14 tools/river-map/geometry.py --check    # valida el Mapa y las rutas de las paradas; codigo 1 si algo falla

Opciones para pruebas y fixtures: --source, --overrides, --worldtable, --out y --stops cambian las rutas. Determinista:
misma entrada, mismos bytes (UTF-8 sin BOM, LF, orden de claves fijo y una linea por elemento). Mensajes solo en ASCII.

Marco y codificacion (08 §4.5; P-M1):
- Proyeccion equirrectangular con el factor cos(lat0), lat0 = -5, girada 90 grados en el sentido de las agujas del reloj y
  sin espejo (06 §4.5; propuesta §3.1; la de generar_mapa.py): x = (lat - lat_min) / (lat_max - lat_min) (0 = sur, a la
  izquierda; 1 = norte, a la derecha) e y = (lon - lon_min) / (lon_max - lon_min) (0 = oeste, los Andes, arriba; 1 = este, el
  Atlantico, abajo). En pantalla, px = x * W y py = y * W * aspecto, con aspecto = (lon_max - lon_min) * cos(lat0) /
  (lat_max - lat_min) (1,31498): la misma escala en los dos ejes.
- Cada punto es un par uint16 (X, Y) = (round(65535 x), round(65535 y)). pts y verts: base64 (RFC 4648) de uint16
  little-endian, el primer par absoluto y los demas como deltas modulo 65536; tris: indices absolutos.
- s: fraccion de la longitud de la linea medida sobre esos mismos puntos en pantalla (X / 65535, Y / 65535 * aspecto),
  desde su primer punto: el cliente la recalcula igual al decodificar. Los puntos van de la cabecera a la boca (en el cauce,
  del Mismi al Atlantico; en el ramal del Para, de Gurupa a la bahia), asi que s crece rio abajo [P]. Se aparta de los
  valores del ejemplo de 08 §4.5 (en el cauce, s medida desde el mar, aunque el leg de iquitos del mismo ejemplo va en
  este sentido): pendiente de que el carril de documentacion alinee 08 §4.5 o de que el duenno elija.

Que escribe:
- lines: el cauce y el ramal del Para (main), los afluentes de los mundos (tributary), los de paso con el Tocantins
  (passby) y los rios locales de las paradas 11, 13, 15, 16 y 18 y, desde M4.5a, los de rios_locales_201_700 (el Aguarico,
  el Cuyabeno, el Tambopata y el Manu de las paradas 25-26, 35-37 y 40-41; local); los dos rios de fuera de la cuenca van en los
  recuadros, que la 1.0 deja vacios (insets = []). Cada linea con su class [P] (1 el cauce y el ramal, 2 los afluentes, 3
  los locales), su agua (water, la de Sioli de la investigacion: white, black, clear y andean en la cabecera del cauce; sin
  tramos si la investigacion la da [NV]), su boca (mouth: la linea y la s donde entra) y fill_on_pass (1 en los de paso y
  los locales; en los afluentes, la fraccion de sus primeros MOUTH_FILL_KM km desde la boca [P]).
- splits: las dos aguas lado a lado de Santarem y del Encuentro (propuesta §3.4), desde la confluencia SPLIT_KM km rio
  abajo [P], con el agua de cada orilla mirando rio abajo (left y right).
- land: la tierra de la caja entre la costa del Pacifico y la del Atlantico de la muestra (decorado_de_la_muestra, [NV]),
  triangulada por orejas, con una sola zona [P] (las zonas del mosaico llegan con la mascara del artista, M5.1).
- home: la casa de TiTino, en Belem, en la orilla de la bahia de Guajara (notas de la parada 0).
- labels (los paises en su region, rotulos_de_pais), viewpoints (los miradores, reveal_on = 10 [P]) y passing (los puntos
  de paso de 1-200 y, desde M4.5a, los de los viajes de entrada que llevan id, como teotonio en el del 300, con su linea,
  su s y la clave de su dato, si la tiene); los nombres van por convencion en map.stop.<id>.name y
  map.country.<iso2>.name, en el bloque map.* de facts.csv que escribe stops.py.

--check (codigo 1 si algo falla): lo commiteado es lo que se regenera (desfase); cada punto de control de la investigacion
queda a <= 2 dp de su linea en su vista (con el respaldo, la Cuenca: <= TOLERANCE_KM km); cada tramo del cauce mide al
menos la linea recta; cada boca cae sobre su rio; la tierra es un poligono simple bien triangulado; y las rutas de
river_stops_v1.json (at y leg) son las de este Mapa: paradas a <= TOLERANCE_KM km de su punto del rio (medidos desde su
punto de atraque si la investigacion lo da), km de acuerdo con km_from_sea y que no retroceden, tramos continuos que no
cruzan tierra (dos tramos solo se unen en el mismo (line, s) o en una confluencia: la boca de un afluente, o el arranque
del ramal, y su s sobre el anfitrion; cualquier otro salto cruza tierra), via y mouths existentes; ademas, todas las reglas de
stops.py (candidatos de la propuesta §5.1 bis incluidos). Las unicas excepciones a TOLERANCE_KM son las pendientes del
duenno (PENDING_SITE_KM y PENDING_MOUTH_KM: la cifra medida, sin margen). Avisa (sin fallar) de cada excepcion en uso y de
los datos de los puntos de paso y de los miradores que aun no tienen texto en facts.csv.
"""
from __future__ import annotations

import argparse
import base64
import json
import math
import re
import struct
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from riverdata import (  # noqa: E402  (la ruta de la herramienta va antes)
    COUNTRY_NAMES,
    FACTS_PATH,
    MAP_PATH,
    OVERRIDES_PATH,
    ROOT,
    SOURCE_PATH,
    STOPS_PATH,
    WORLDTABLE_PATH,
    SourceError,
    WorldTable,
    ascii_text,
    fact_key_of,
    has_belen,
    iter_strings,
    load_json,
    parse_csv_record,
    place_id,
    split_csv_records,
    stop_records,
    travel_passing_points,
)

FORMAT = "af-river-map/1"
SRC = "rio_amazonas"   # la geometria de respaldo (D-013, punto 5); M5.1 la cambia por ne10m y hydrorivers
Q = 65535              # uint16: X = round(Q * x), Y = round(Q * y)
EARTH_KM = 6371.0

# Tolerancia de la validacion (D-013, punto 5; propuesta §3.6 y §11.3): <= 2 dp de la vista donde se ve cada punto. La
# geometria de respaldo solo es correcta a escala de cuenca (§11.3, punto 7), asi que se valida en la vista Cuenca: 13,1 dp
# por grado, 2 dp = ~15 km [calculo de la propuesta]. La vista Mundo (<= 1 km a 15x) llega con HydroRIVERS (M5.1).
TOLERANCE_KM = 15.0

KIND = {"cauce": "main", "ramal": "main", "afluente_mundo": "tributary", "afluente_paso": "passby", "rio_local": "local"}
# fuera_de_cuenca (Maroni y Orinoco): van en los recuadros (propuesta §3.3, punto 6), y la 1.0 los deja vacios.
CLASS = {"main": 1, "tributary": 2, "passby": 2, "local": 3}   # [P] anchos de 6, 3,5 y 2 dp en el cliente (M3.2)
MAIN_ID = "amazonas"
TIE_KM = 0.1   # empate entre lineas: por debajo del error de cuantizar (~0,04 km)
WATER = {"blanca": "white", "negra": "black", "clara": "clear", "andina": "andean"}
WATERS = frozenset(WATER.values())

# Donde entra cada linea (su rio anfitrion). Los afluentes y los locales entran por su ultimo punto (la boca, s = 1); el
# ramal del Para sale del cauce por su primer punto (Gurupa, s = 0) y no lleva mouth (es main: el estuario va lleno).
HOSTS = {
    "tapajos": "amazonas", "negro": "amazonas", "casiquiare": "negro", "branco": "negro", "takutu": "branco",
    "putumayo_ica": "amazonas", "napo": "amazonas", "maranon": "amazonas", "madre_de_dios": "beni", "beni": "madeira",
    "mamore": "madeira", "itenez": "mamore", "madeira": "amazonas", "tocantins": "para", "xingu": "amazonas",
    "trombetas": "amazonas", "purus": "amazonas", "jurua": "amazonas", "japura": "amazonas", "nanay": "amazonas",
    "loretoyacu": "amazonas", "atacuari": "amazonas", "itaya": "amazonas", "samiria": "maranon", "pacaya": "amazonas",
    # Rios locales de 201-420 (rios_locales_201_700; M4.5a): el desvio a Cuyabeno (paradas 25 y 26) y los del mundo 7
    # (la collpa del Tambopata, 35-37, y el Manu, 40 y 41), con boca_en de la investigacion.
    "aguarico": "napo", "cuyabeno": "aguarico", "tambopata": "madre_de_dios", "manu": "madre_de_dios",
}
# Listas de rios de la investigacion, en este orden: los de 1-200 y los locales de 201-700 (M4.5a; propuesta §5.4 bis y
# §5.4 ter), que la investigacion deja aparte, con boca_en, hasta que esta herramienta les da anfitrion.
RIVER_LISTS = ("rios", "rios_locales_201_700")
BRANCHES = {"para": "amazonas"}   # ramal: su primer punto esta sobre el cauce

# Excepciones a TOLERANCE_KM PENDIENTES DEL DUENNO (pregunta abierta, propuesta como D-025; no son canon). La tolerancia es
# <= 15 km para todo (D-023, punto 20); estas cifras son la distancia MEDIDA con los datos de hoy (redondeada hacia arriba a
# 0,1 km, sin margen), congelada para que --check siga en verde y falle si un dato se aleja mas. --check avisa de cada una.
# - Sitios tierra adentro: el punto de la investigacion es el centro del area protegida, no un punto del rio. Se validan
#   por su punto de atraque (campo «atraque» {lat, lon} del registro de la investigacion) con los 15 km del canon; mientras
#   la investigacion no lo anada, rige la cifra medida entre el centro y su punto del rio.
PENDING_SITE_KM = {
    "flona_tapajos": 23.1,    # centro de la FLONA do Tapajos (notas: «la canoa se amarra en la orilla del Tapajos mas cercana»)
    "amacayacu": 57.5,        # centro del parque (notas: al norte del rio); su punto del rio, km 3.185 («frente del parque»)
    "pacaya_samiria": 46.2,   # centro de la reserva (notas: «la canoa entra por la boca del Samiria, en el Maranon»)
    "pacaya_alba": 46.2,      # la misma reserva, al alba
}
# - Bocas: la del Pacaya esta en el canal de Puinahua, que el cauce de respaldo no dibuja (37,08 km). HydroRIVERS (M5.1).
PENDING_MOUTH_KM = {"pacaya": 37.1}

# [P] Afluentes: «solo su boca (unos 80-120 km)» al pasarla (propuesta §3.4); los de paso y los locales, enteros.
MOUTH_FILL_KM = 100.0
# [P] Dos aguas lado a lado (propuesta §3.4: «unos 30 dp» en la vista Mundo; la muestra parte ~65 km).
SPLIT_KM = 65.0
SPLITS = (("encontro", "negro"), ("santarem", "tapajos"))   # (id, afluente que da la segunda agua)
# [P] Los miradores de 1-200 estan en el estuario: el primer Mapa (nivel 10) ya los revela (propuesta §5.2).
VIEWPOINT_REVEAL_ON = 10
# [P] Una sola zona de tierra hasta la mascara del artista (M5.1).
LAND_ZONE = {"id": "land", "tile": "canopy"}
# Casa de TiTino: Belem, en la bahia de Guajara (rio-amazonas.json, notas de la parada 0; propuesta §3.3, punto 7).
HOME_STOP = 0


class GeometryError(Exception):
    """La fuente no permite construir la geometria."""


# --------------------------------------------------------------------------------------------- marco y codificacion

def haversine(a: tuple, b: tuple) -> float:
    la1, lo1, la2, lo2 = map(math.radians, (a[0], a[1], b[0], b[1]))
    h = math.sin((la2 - la1) / 2) ** 2 + math.cos(la1) * math.cos(la2) * math.sin((lo2 - lo1) / 2) ** 2
    return 2 * EARTH_KM * math.asin(min(1.0, math.sqrt(h)))


class Frame:
    """Marco cw90 de la investigacion (proyeccion de rio-amazonas.json): (lat, lon) <-> (x, y) en [0, 1] y uint16."""

    def __init__(self, lat_min: float, lat_max: float, lon_min: float, lon_max: float, lat0: float):
        if not (lat_min < lat_max and lon_min < lon_max):
            raise GeometryError("proyeccion: caja vacia")
        self.lat_min, self.lat_max, self.lon_min, self.lon_max, self.lat0 = lat_min, lat_max, lon_min, lon_max, lat0
        self.aspect = (lon_max - lon_min) * math.cos(math.radians(lat0)) / (lat_max - lat_min)

    @classmethod
    def from_source(cls, source: dict) -> "Frame":
        p = source.get("proyeccion")
        try:
            caja = p["caja"]
            return cls(float(caja["lat_min"]), float(caja["lat_max"]), float(caja["lon_min"]), float(caja["lon_max"]),
                       float(p["lat0"]))
        except (TypeError, KeyError, ValueError) as e:
            raise GeometryError("rio-amazonas.json: proyeccion sin caja (lat_min, lat_max, lon_min, lon_max) o sin lat0") from e

    def to_json(self) -> dict:
        return {"rotation": "cw90", "lat": [self.lat_min, self.lat_max], "lon": [self.lon_min, self.lon_max],
                "lat0": self.lat0}

    def norm(self, lat: float, lon: float) -> tuple:
        return ((lat - self.lat_min) / (self.lat_max - self.lat_min), (lon - self.lon_min) / (self.lon_max - self.lon_min))

    def quant(self, lat: float, lon: float) -> tuple:
        x, y = self.norm(lat, lon)
        return (min(Q, max(0, round(x * Q))), min(Q, max(0, round(y * Q))))

    def inside(self, lat: float, lon: float) -> bool:
        x, y = self.norm(lat, lon)
        return -1e-9 <= x <= 1 + 1e-9 and -1e-9 <= y <= 1 + 1e-9

    def latlon(self, X: float, Y: float) -> tuple:
        return (self.lat_min + X / Q * (self.lat_max - self.lat_min), self.lon_min + Y / Q * (self.lon_max - self.lon_min))

    def screen(self, X: float, Y: float) -> tuple:
        """Posicion en pantalla con W = 1 (la que mide s): (X / Q, Y / Q * aspecto)."""
        return (X / Q, Y / Q * self.aspect)


def encode_points(points: list) -> str:
    """pts / verts: base64 de uint16 LE, el primer par absoluto y los demas como deltas modulo 65536 (P-M1)."""
    out = bytearray()
    px = py = 0
    for i, (x, y) in enumerate(points):
        if not (0 <= x <= Q and 0 <= y <= Q):
            raise GeometryError(f"punto fuera de uint16: {(x, y)}")
        if i == 0:
            out += struct.pack("<HH", x, y)
        else:
            out += struct.pack("<HH", (x - px) % 65536, (y - py) % 65536)
        px, py = x, y
    return base64.b64encode(bytes(out)).decode("ascii")


def decode_points(text: str) -> list:
    raw = base64.b64decode(text.encode("ascii"), validate=True)
    if len(raw) % 4:
        raise ValueError("la longitud no es multiplo de 4 bytes (pares uint16)")
    points = []
    x = y = 0
    for i in range(0, len(raw), 4):
        dx, dy = struct.unpack_from("<HH", raw, i)
        if i == 0:
            x, y = dx, dy
        else:
            x, y = (x + dx) % 65536, (y + dy) % 65536
        points.append((x, y))
    return points


def encode_indices(indices: list) -> str:
    """tris: base64 de uint16 LE, indices absolutos (P-M1)."""
    return base64.b64encode(b"".join(struct.pack("<H", i) for i in indices)).decode("ascii")


def decode_indices(text: str) -> list:
    raw = base64.b64decode(text.encode("ascii"), validate=True)
    if len(raw) % 2:
        raise ValueError("la longitud no es multiplo de 2 bytes (uint16)")
    return [struct.unpack_from("<H", raw, i)[0] for i in range(0, len(raw), 2)]


# --------------------------------------------------------------------------------------------- lineas

class Line:
    """Una linea del Mapa con sus puntos cuantizados, su longitud en pantalla (para s) y en km (geodesica)."""

    def __init__(self, rid: str, kind: str, frame: Frame, q: list, km_from_sea: list | None):
        self.id = rid
        self.kind = kind
        self.frame = frame
        self.q = q
        self.ll = [frame.latlon(x, y) for x, y in q]
        scr = [frame.screen(x, y) for x, y in q]
        self.seg = [math.hypot(b[0] - a[0], b[1] - a[1]) for a, b in zip(scr, scr[1:])]
        self.cum = [0.0]
        for d in self.seg:
            self.cum.append(self.cum[-1] + d)
        self.total = self.cum[-1]
        self.seg_km = [haversine(a, b) for a, b in zip(self.ll, self.ll[1:])]
        self.arc_km = [0.0]
        for d in self.seg_km:
            self.arc_km.append(self.arc_km[-1] + d)
        self.km_from_sea = km_from_sea   # solo el cauce: km de cada punto (investigacion)
        self.source: dict = {}           # el registro de rios de rio-amazonas.json
        self.host: str | None = None
        self.host_s: float | None = None   # s de la boca (o del arranque del ramal) sobre el anfitrion
        self.own_s: float | None = None    # 1.0 (boca de un afluente) o 0.0 (arranque del ramal)

    # posicion -> s
    def s_of(self, i: int, t: float) -> float:
        if self.total <= 0:
            return 0.0
        if i >= len(self.seg):
            return 1.0
        return (self.cum[i] + t * self.seg[i]) / self.total

    def locate(self, s: float) -> tuple:
        """(i, t) del punto en la fraccion s."""
        if len(self.q) == 1 or self.total <= 0:
            return 0, 0.0
        target = min(1.0, max(0.0, s)) * self.total
        for i, d in enumerate(self.seg):
            if self.cum[i + 1] >= target - 1e-15 and d > 0:
                return i, min(1.0, max(0.0, (target - self.cum[i]) / d))
        return len(self.seg) - 1, 1.0

    def point(self, i: int, t: float) -> tuple:
        """(lat, lon) del punto (i, t) sobre los puntos cuantizados."""
        if len(self.ll) == 1:
            return self.ll[0]
        a, b = self.ll[i], self.ll[min(i + 1, len(self.ll) - 1)]
        return (a[0] + t * (b[0] - a[0]), a[1] + t * (b[1] - a[1]))

    def point_at_s(self, s: float) -> tuple:
        return self.point(*self.locate(s))

    def project(self, lat: float, lon: float) -> tuple:
        """Pie de la perpendicular sobre la linea, en el plano del marco: (i, t, distancia en km al pie)."""
        p = self.frame.screen(*_quant_float(self.frame, lat, lon))
        if len(self.q) == 1:
            return 0, 0.0, haversine((lat, lon), self.ll[0])
        best = None
        for i in range(len(self.q) - 1):
            a = self.frame.screen(*self.q[i])
            b = self.frame.screen(*self.q[i + 1])
            dx, dy = b[0] - a[0], b[1] - a[1]
            length2 = dx * dx + dy * dy
            t = 0.0 if length2 == 0 else max(0.0, min(1.0, ((p[0] - a[0]) * dx + (p[1] - a[1]) * dy) / length2))
            d = math.hypot(p[0] - a[0] - t * dx, p[1] - a[1] - t * dy)
            if best is None or d < best[0] - 1e-15:
                best = (d, i, t)
        _, i, t = best
        return i, t, haversine((lat, lon), self.point(i, t))

    def distance_km(self, lat: float, lon: float) -> float:
        return self.project(lat, lon)[2]

    # km
    def km_main(self, i: int, t: float) -> float:
        km = self.km_from_sea
        if len(km) == 1:
            return float(km[0])
        return km[i] + t * (km[min(i + 1, len(km) - 1)] - km[i])

    def locate_main_km(self, km_value: float) -> tuple | None:
        km = self.km_from_sea
        for i in range(len(km) - 1):
            a, b = km[i], km[i + 1]
            if b <= km_value <= a:
                return i, (0.0 if a == b else (a - km_value) / (a - b))
        return None

    def km_up_from_mouth(self, i: int, t: float) -> float:
        """Km por la linea desde su boca (su ultimo punto) hasta (i, t), geodesicos (los desvios: «confluencia mas el
        recorrido sin meandros», propuesta §5.1)."""
        if len(self.ll) == 1:
            return 0.0
        here = self.arc_km[i] + t * self.seg_km[i] if i < len(self.seg_km) else self.arc_km[-1]
        return self.arc_km[-1] - here

    def locate_up_from_mouth(self, dkm: float) -> tuple | None:
        if len(self.ll) == 1:
            return (0, 0.0) if abs(dkm) < 1e-9 else None
        target = self.arc_km[-1] - dkm
        if target < -1e-9 or dkm < -1e-9:
            return None
        target = max(0.0, target)
        for i, d in enumerate(self.seg_km):
            if self.arc_km[i + 1] >= target - 1e-12 and d > 0:
                return i, min(1.0, max(0.0, (target - self.arc_km[i]) / d))
        return len(self.seg_km) - 1, 1.0


def _quant_float(frame: Frame, lat: float, lon: float) -> tuple:
    x, y = frame.norm(lat, lon)
    return (x * Q, y * Q)


def _round_s(s: float) -> float:
    v = round(min(1.0, max(0.0, s)), 6)
    return 0.0 if v == 0 else v


def _water_of(text) -> str | None:
    """Tipo de agua de Sioli del campo «agua» de un rio: su primera palabra si es blanca, negra o clara y no es [NV]."""
    if not isinstance(text, str) or "[NV]" in text:
        return None
    first = text.strip().split(" ")[0].strip(",;:").lower()
    return WATER.get(first) if first in ("blanca", "negra", "clara") else None


class Model:
    """La geometria en memoria: el marco, las lineas por id y en su orden, y las paradas de la fuente."""

    def __init__(self, frame: Frame):
        self.frame = frame
        self.lines: dict = {}
        self.order: list = []
        self.passing: list = []     # [{id, line, s, fact, lat, lon}]

    def km_at(self, line_id: str, s: float) -> float | None:
        """Km desde el mar del punto s de una linea, o None si la linea no llega al mar por el cauce (el ramal)."""
        line = self.lines[line_id]
        i, t = line.locate(s)
        if line.kind == "main":
            return line.km_main(i, t) if line.km_from_sea else None
        if line.host is None or line.host_s is None:
            return None
        base = self.km_at(line.host, line.host_s)
        return None if base is None else base + line.km_up_from_mouth(i, t)

    def s_at_km(self, line_id: str, km_value: float, slack_km: float = 0.0) -> float | None:
        """s del punto de una linea en un km desde el mar; con slack_km, un km que se pase del tramo de la linea en menos
        de slack_km se lleva a su extremo (el respaldo no tiene meandros: su longitud se queda corta)."""
        line = self.lines[line_id]
        if line.kind == "main":
            if not line.km_from_sea:
                return None
            km = line.km_from_sea
            lo, hi = min(km), max(km)
            if hi < km_value <= hi + slack_km:
                km_value = hi
            elif lo - slack_km <= km_value < lo:
                km_value = lo
            loc = line.locate_main_km(km_value)
        else:
            base = self.km_at(line.host, line.host_s) if line.host else None
            if base is None:
                return None
            dkm = km_value - base
            if line.arc_km[-1] < dkm <= line.arc_km[-1] + slack_km:
                dkm = line.arc_km[-1]
            elif -slack_km <= dkm < 0:
                dkm = 0.0
            loc = line.locate_up_from_mouth(dkm)
        return None if loc is None else line.s_of(*loc)

    def chain(self, line_id: str) -> list:
        out = [line_id]
        while self.lines[out[-1]].host is not None:
            out.append(self.lines[out[-1]].host)
            if len(out) > len(self.lines):
                raise GeometryError(f"ciclo de anfitriones en {line_id}")
        return out


def build_model(source: dict) -> tuple:
    """Lineas de rio-amazonas.json en el marco cuantizado, con sus bocas. Devuelve (Model, errores)."""
    errors: list = []
    frame = Frame.from_source(source)
    model = Model(frame)
    rios = source.get("rios")
    if not isinstance(rios, list):
        raise GeometryError("rio-amazonas.json: falta la lista rios")
    rios = list(rios)
    for name in RIVER_LISTS[1:]:
        extra = source.get(name, [])
        if not isinstance(extra, list):
            errors.append(f"rio-amazonas.json: {name} no es una lista")
            continue
        rios += extra
    for r in rios:
        if not isinstance(r, dict):
            errors.append("rios: una entrada no es un objeto")
            continue
        kind = KIND.get(r.get("tipo"))
        if kind is None:
            if r.get("tipo") != "fuera_de_cuenca":
                errors.append(f"rio {r.get('id')!r}: tipo {r.get('tipo')!r} desconocido")
            continue
        rid = r.get("id")
        pts = r.get("puntos")
        if not isinstance(rid, str) or not re.fullmatch(r"[a-z0-9_]+", rid):
            errors.append(f"rio {rid!r}: id que no es [a-z0-9_]+")
            continue
        if rid in model.lines:
            errors.append(f"rio {rid}: id repetido en {' y '.join(RIVER_LISTS)}")
            continue
        boca_en = r.get("boca_en")
        if boca_en is not None and boca_en != HOSTS.get(rid):
            errors.append(f"rio {rid}: boca_en {boca_en!r} de la investigacion y HOSTS (geometry.py) dice "
                          f"{HOSTS.get(rid)!r}")
        if not isinstance(pts, list) or not pts:
            errors.append(f"rio {rid}: sin puntos")
            continue
        if len(pts) < 2 and not r.get("solo_boca"):
            errors.append(f"rio {rid}: menos de 2 puntos y sin «solo_boca»")
        q = []
        for j, p in enumerate(pts):
            try:
                lat, lon = float(p["lat"]), float(p["lon"])
            except (TypeError, KeyError, ValueError):
                errors.append(f"rio {rid}: punto {j} sin lat/lon")
                continue
            if not frame.inside(lat, lon):
                errors.append(f"rio {rid}: punto {j} ({p.get('nombre')!r}) fuera de la caja del marco")
            q.append(frame.quant(lat, lon))
        dup = [j for j in range(1, len(q)) if q[j] == q[j - 1]]
        if dup:
            errors.append(f"rio {rid}: puntos repetidos tras cuantizar (indices {dup})")
        km = None
        if rid == MAIN_ID:
            try:
                km = [int(p["km_desde_el_mar"]) for p in pts]
            except (TypeError, KeyError, ValueError):
                errors.append(f"rio {rid}: el cauce necesita km_desde_el_mar en cada punto")
                km = None
        line = Line(rid, kind, frame, q, km)
        line.source = r
        model.lines[rid] = line
        model.order.append(rid)
    if MAIN_ID not in model.lines:
        raise GeometryError("rio-amazonas.json: falta el cauce (amazonas)")
    # Bocas y arranques, sobre su anfitrion.
    for rid in model.order:
        line = model.lines[rid]
        if line.kind == "main" and rid != MAIN_ID:
            host = BRANCHES.get(rid)
            end = 0
        elif line.kind == "main":
            continue
        else:
            host = HOSTS.get(rid)
            end = len(line.q) - 1
        if host is None:
            errors.append(f"linea {rid}: sin rio anfitrion en HOSTS (geometry.py)")
            continue
        if host not in model.lines or host == rid:
            errors.append(f"linea {rid}: su anfitrion {host!r} no es otra linea del Mapa")
            continue
        h = model.lines[host]
        lat, lon = line.ll[end]
        i, t, dist = h.project(lat, lon)
        line.host = host
        line.host_s = _round_s(h.s_of(i, t))
        line.own_s = 0.0 if end == 0 else 1.0
        allowed = PENDING_MOUTH_KM.get(rid, TOLERANCE_KM)
        if dist > allowed:
            errors.append(f"linea {rid}: su boca queda a {dist:.1f} km de {host} (maximo {allowed:g} km"
                          f"{_limit_note(rid in PENDING_MOUTH_KM)}): mouth fuera de su rio")
    for rid in model.order:
        try:
            model.chain(rid)
        except GeometryError as e:
            errors.append(str(e))
    # Puntos de paso: en la linea mas cercana (empate, a menos de TIE_KM: la primera en el orden de lineas, el cauce antes;
    # asi Gurupa, en el arranque del ramal, y las bocas de los afluentes quedan en el cauce).
    # Los de 1-200 y, desde M4.5a, los de los viajes de entrada de 201-1.000 que llevan id (teotonio, en el del 300): su id
    # es el de la investigacion, no el de su nombre.
    points = [("puntos_de_paso_1_200", p) for p in source.get("puntos_de_paso_1_200", []) or []]
    points += [("puntos_de_paso_del_viaje", p) for p in travel_passing_points(source)]
    for field, p in points:
        if not isinstance(p, dict):
            errors.append(f"{field}: una entrada no es un objeto")
            continue
        try:
            lat, lon = float(p["lat"]), float(p["lon"])
            name = str(p["nombre"])
        except (TypeError, KeyError, ValueError):
            errors.append(f"{field}: un punto sin lat, lon o nombre")
            continue
        pid = p["id"] if isinstance(p.get("id"), str) else place_id(name)
        if not re.fullmatch(r"[a-z0-9_]+", pid):
            errors.append(f"{field}: el punto de paso {name!r} tiene el id {pid!r}, que no es [a-z0-9_]+")
            continue
        best = None
        for rid in model.order:
            i, t, d = model.lines[rid].project(lat, lon)
            if best is None or d < best[0] - TIE_KM:
                best = (d, rid, i, t)
        d, rid, i, t = best
        if d > TOLERANCE_KM:
            errors.append(f"punto de paso {name!r}: a {d:.1f} km de la linea mas cercana ({rid}; maximo {TOLERANCE_KM:g})")
        model.passing.append({"id": pid, "line": rid, "s": _round_s(model.lines[rid].s_of(i, t)),
                              "fact": fact_key_of(p.get("dato")), "lat": lat, "lon": lon, "name": name})
    return model, errors


# --------------------------------------------------------------------------------------------- documento

def _water_intervals(line: Line) -> list:
    r = line.source
    if line.id == MAIN_ID:
        types = []
        for p in r["puntos"]:
            types.append(WATER.get(str(p.get("agua", "")).strip().lower()))
        if any(t is None for t in types):
            return []
        out = []
        for j, t in enumerate(types):
            s = _round_s(line.s_of(j, 0.0)) if j < len(line.seg) else 1.0
            if not out:
                out.append({"s0": 0.0, "s1": 1.0, "type": t})
            elif out[-1]["type"] != t:
                out[-1]["s1"] = s
                out.append({"s0": s, "s1": 1.0, "type": t})
        return out
    t = _water_of(r.get("agua"))
    return [] if t is None else [{"s0": 0.0, "s1": 1.0, "type": t}]


def _splits(model: Model) -> tuple:
    errors = []
    main = model.lines[MAIN_ID]
    out = []
    main_water = "white"
    for sid, trib in SPLITS:
        line = model.lines.get(trib)
        if line is None or line.host != MAIN_ID:
            errors.append(f"split {sid}: el afluente {trib} no entra en el cauce")
            continue
        water = _water_intervals(line)
        if not water:
            errors.append(f"split {sid}: el {trib} no tiene agua comprobada")
            continue
        s0 = line.host_s
        km0 = model.km_at(MAIN_ID, s0)
        s1 = model.s_at_km(MAIN_ID, km0 - SPLIT_KM) if km0 is not None else None
        if s1 is None or s1 <= s0:
            errors.append(f"split {sid}: no hay {SPLIT_KM:g} km de cauce rio abajo de la confluencia")
            continue
        # Orilla del afluente mirando rio abajo (s creciente): izquierda si su penultimo punto queda a la izquierda.
        i, t = main.locate(s0)
        a = model.frame.screen(*main.q[i])
        b = model.frame.screen(*main.q[min(i + 1, len(main.q) - 1)])
        c = model.frame.screen(*line.q[-2]) if len(line.q) >= 2 else a
        m = model.frame.screen(*_quant_float(model.frame, *main.point(i, t)))
        dx, dy = b[0] - a[0], b[1] - a[1]
        # En pantalla (y hacia abajo), la izquierda de la direccion (dx, dy) es (dy, -dx).
        left_side = (c[0] - m[0]) * dy + (c[1] - m[1]) * (-dx) > 0
        trib_water = water[-1]["type"]
        out.append({"id": sid, "s0": s0, "s1": _round_s(s1),
                    "left": trib_water if left_side else main_water, "right": main_water if left_side else trib_water})
    out.sort(key=lambda e: e["s0"])
    return out, errors


def _fill_on_pass(line: Line) -> float:
    if line.kind in ("passby", "local"):
        return 1.0
    length = line.arc_km[-1]
    return 1.0 if length <= MOUTH_FILL_KM else round(MOUTH_FILL_KM / length, 4)


def _cross(o, a, b) -> int:
    return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])


def _area2(poly: list) -> int:
    return sum(poly[i][0] * poly[(i + 1) % len(poly)][1] - poly[(i + 1) % len(poly)][0] * poly[i][1]
               for i in range(len(poly)))


def _segments_cross(p1, p2, p3, p4) -> bool:
    d1, d2 = _cross(p3, p4, p1), _cross(p3, p4, p2)
    d3, d4 = _cross(p1, p2, p3), _cross(p1, p2, p4)
    if ((d1 > 0 > d2) or (d1 < 0 < d2)) and ((d3 > 0 > d4) or (d3 < 0 < d4)):
        return True

    def on(p, q, r):
        return _cross(p, q, r) == 0 and min(p[0], q[0]) <= r[0] <= max(p[0], q[0]) and min(p[1], q[1]) <= r[1] <= max(p[1], q[1])
    return (d1 == 0 and on(p3, p4, p1)) or (d2 == 0 and on(p3, p4, p2)) or (d3 == 0 and on(p1, p2, p3)) or \
        (d4 == 0 and on(p1, p2, p4))


def polygon_problems(poly: list) -> list:
    """Por que un anillo no es un poligono simple (vacio si lo es)."""
    n = len(poly)
    if n < 3:
        return ["menos de 3 vertices"]
    if len(set(poly)) != n:
        return ["vertices repetidos"]
    out = []
    for i in range(n):
        for j in range(i + 1, n):
            if j == i + 1 or (i == 0 and j == n - 1):
                continue
            if _segments_cross(poly[i], poly[(i + 1) % n], poly[j], poly[(j + 1) % n]):
                out.append(f"las aristas {i} y {j} se cortan")
    if _area2(poly) == 0:
        out.append("area nula")
    return out


def _in_triangle(p, a, b, c) -> bool:
    """p dentro del triangulo (a, b, c) en sentido positivo, bordes incluidos."""
    return _cross(a, b, p) >= 0 and _cross(b, c, p) >= 0 and _cross(c, a, p) >= 0


def ear_clip(poly: list) -> list:
    """Triangulacion por orejas de un poligono simple en sentido positivo (area con signo > 0): indices de vertices, cada
    triangulo en el mismo sentido. Determinista: siempre la primera oreja valida desde el principio del anillo."""
    ring = list(range(len(poly)))
    tris: list = []
    guard = 0
    while len(ring) > 3:
        guard += 1
        if guard > 10 * len(poly) * len(poly):
            raise GeometryError("triangulacion: no encuentro oreja (poligono no simple)")
        n = len(ring)
        found = False
        for k in range(n):
            ia, ib, ic = ring[(k - 1) % n], ring[k], ring[(k + 1) % n]
            a, b, c = poly[ia], poly[ib], poly[ic]
            cr = _cross(a, b, c)
            if cr < 0:
                continue
            if cr == 0:
                # Vertice alineado: se quita sin triangulo (el area no cambia).
                ring.pop(k)
                found = True
                break
            if any(_in_triangle(poly[j], a, b, c) for j in ring if j not in (ia, ib, ic) and poly[j] not in (a, b, c)):
                continue
            tris.extend((ia, ib, ic))
            ring.pop(k)
            found = True
            break
        if not found:
            raise GeometryError("triangulacion: no encuentro oreja (poligono no simple)")
    if len(ring) == 3 and _cross(poly[ring[0]], poly[ring[1]], poly[ring[2]]) > 0:
        tris.extend(ring)
    return tris


def land_ring(source: dict, frame: Frame) -> list:
    """Anillo de la tierra de la caja: la costa del Pacifico de norte a sur, la esquina sureste de la caja y la costa
    del Atlantico de su borde este hasta el norte (decorado_de_la_muestra, [NV]: la costa de produccion es la de Natural
    Earth, M5.1). Cuantizado y en sentido positivo."""
    deco = source.get("decorado_de_la_muestra")
    try:
        pac = [frame.quant(float(p["lat"]), float(p["lon"])) for p in deco["costa_pacifica"]]
        atl = [frame.quant(float(p["lat"]), float(p["lon"])) for p in deco["costa_atlantica"]]
    except (TypeError, KeyError, ValueError) as e:
        raise GeometryError("rio-amazonas.json: decorado_de_la_muestra sin costa_pacifica o costa_atlantica") from e
    if not pac or not atl:
        raise GeometryError("rio-amazonas.json: costa vacia")
    # Pacifico de norte (x = 1) a sur (x = 0); Atlantico de norte (x = 1) a su borde este (y = 1).
    if pac[0][0] < pac[-1][0]:
        pac.reverse()
    if atl[0][0] < atl[-1][0]:
        atl.reverse()
    ring = pac + [(0, Q)] + list(reversed(atl))
    clean = []
    for p in ring:
        if not clean or clean[-1] != p:
            clean.append(p)
    if len(clean) > 1 and clean[0] == clean[-1]:
        clean.pop()
    if _area2(clean) < 0:
        clean.reverse()
    return clean


def build_map(source: dict) -> tuple:
    """Documento af-river-map/1 y su modelo. Devuelve (doc, model, errores)."""
    model, errors = build_model(source)
    frame = model.frame
    lines = []
    for rid in model.order:
        line = model.lines[rid]
        entry = {"id": rid, "kind": line.kind, "class": CLASS[line.kind], "src": SRC, "pts": encode_points(line.q),
                 "water": _water_intervals(line)}
        if rid == MAIN_ID:
            splits, errs = _splits(model)
            errors += errs
            entry["splits"] = splits
        if line.kind != "main" and line.host is not None:
            entry["mouth"] = {"on": line.host, "s": line.host_s}
            entry["fill_on_pass"] = _fill_on_pass(line)
        lines.append(entry)
    ring = land_ring(source, frame)
    problems = polygon_problems(ring)
    if problems:
        errors.append("tierra: el anillo de costas no es un poligono simple (" + "; ".join(problems) + ")")
        tris = []
    else:
        tris = ear_clip(ring)
    land = {"verts": encode_points(ring), "tris": encode_indices(tris),
            "zones": [{"id": LAND_ZONE["id"], "tile": LAND_ZONE["tile"], "first": 0, "count": len(tris) // 3}]}
    # Casa de TiTino.
    records = [r for r in source.get("paradas_1_200", []) if isinstance(r, dict)]
    home_rec = next((r for r in records if r.get("k") == HOME_STOP), None)
    home = None
    if home_rec is None or home_rec.get("ancla_rio") not in model.lines:
        errors.append("home: la parada 0 no tiene un ancla_rio del Mapa")
    else:
        hl = model.lines[home_rec["ancla_rio"]]
        i, t, d = hl.project(float(home_rec["lat"]), float(home_rec["lon"]))
        if d > TOLERANCE_KM:
            errors.append(f"home: Belem a {d:.1f} km de {hl.id}")
        home = {"on": hl.id, "s": _round_s(hl.s_of(i, t))}
    # Rotulos de pais.
    by_name = {name: iso for iso, name in COUNTRY_NAMES.items()}
    labels = []
    for r in source.get("rotulos_de_pais", []) or []:
        name = r.get("nombre") if isinstance(r, dict) else None
        iso = None
        if isinstance(name, str):
            iso = by_name.get(name) or next((i for n, i in by_name.items() if name.startswith(n + " (")), None)
        if iso is None:
            errors.append(f"rotulos_de_pais: {name!r} no es ningun pais de COUNTRY_NAMES (riverdata.py)")
            continue
        labels.append({"country": iso, "xy": list(frame.quant(float(r["lat"]), float(r["lon"])))})
    # Miradores.
    viewpoints = []
    for r in source.get("miradores_1_200", []) or []:
        if not isinstance(r, dict) or not isinstance(r.get("nombre"), str):
            errors.append("miradores_1_200: una entrada sin nombre")
            continue
        viewpoints.append({"id": place_id(r["nombre"]), "xy": list(frame.quant(float(r["lat"]), float(r["lon"]))),
                           "reveal_on": VIEWPOINT_REVEAL_ON, "fact": fact_key_of(r.get("dato"))})
    passing = [{"id": p["id"], "line": p["line"], "s": p["s"], "fact": p["fact"]} for p in model.passing]
    doc = {"format": FORMAT, "frame": frame.to_json(), "lines": lines, "land": land, "insets": [], "home": home,
           "labels": labels, "viewpoints": viewpoints, "passing": passing}
    return doc, model, errors


def render(doc: dict) -> str:
    """JSON estable: raiz con 2 espacios y un elemento de cada lista por linea (UTF-8 sin BOM y LF al escribir)."""
    def dumps(value) -> str:
        return json.dumps(value, ensure_ascii=False, separators=(", ", ": "))

    def block(key: str, items: list, last: bool = False) -> list:
        tail = "" if last else ","
        if not items:
            return [f'  "{key}": []{tail}']
        return [f'  "{key}": ['] + [",\n".join("    " + dumps(x) for x in items)] + [f"  ]{tail}"]

    lines = ["{", f'  "format": {dumps(doc["format"])},', f'  "frame": {dumps(doc["frame"])},']
    lines += block("lines", doc["lines"])
    lines.append(f'  "land": {dumps(doc["land"])},')
    lines += block("insets", doc["insets"])
    lines.append(f'  "home": {dumps(doc["home"])},')
    lines += block("labels", doc["labels"])
    lines += block("viewpoints", doc["viewpoints"])
    lines += block("passing", doc["passing"], last=True)
    lines.append("}")
    return "\n".join(lines) + "\n"


# --------------------------------------------------------------------------------------------- paradas: at y leg

def stop_at(model: Model, record: dict) -> tuple:
    """Punto del rio de una parada: en su ancla_rio, en su km_desde_el_mar (la posicion por el rio de la investigacion);
    sin km (Belem, en el ramal), el pie de su punto sobre el ancla. Devuelve ({line, s} o None, errores)."""
    tag = f"parada k={record.get('k')!r} ({record.get('id')!r})"
    rid = record.get("ancla_rio")
    if rid not in model.lines:
        return None, [f"{tag}: ancla_rio {rid!r} no es ninguna linea del Mapa"]
    km_value = record.get("km_desde_el_mar")
    if km_value is None:
        try:
            i, t, _ = model.lines[rid].project(float(record["lat"]), float(record["lon"]))
        except (TypeError, KeyError, ValueError):
            return None, [f"{tag}: sin lat/lon"]
        return {"line": rid, "s": _round_s(model.lines[rid].s_of(i, t))}, []
    s = model.s_at_km(rid, float(km_value), slack_km=TOLERANCE_KM)
    if s is None:
        return None, [f"{tag}: km_desde_el_mar {km_value} no cae en {rid} (fuera de su tramo)"]
    return {"line": rid, "s": _round_s(s)}, []


def route(model: Model, a: dict, b: dict) -> list:
    """Tramos por agua de a {line, s} a b {line, s}: subiendo por los anfitriones hasta el rio comun y bajando hasta b
    (ida y vuelta por las confluencias). Cada tramo, {line, s0, s1}; sin tramos de longitud cero salvo si a = b."""
    ca, cb = model.chain(a["line"]), model.chain(b["line"])
    common = next(x for x in ca if x in cb)
    pieces = []
    cur, s = a["line"], a["s"]
    while cur != common:
        line = model.lines[cur]
        pieces.append([cur, s, line.own_s])
        s, cur = line.host_s, line.host
    down = cb[:cb.index(common)]   # de b hasta el hijo del comun
    if down:
        child = model.lines[down[-1]]
        pieces.append([common, s, child.host_s])
        for k in range(len(down) - 1, 0, -1):
            line = model.lines[down[k]]
            pieces.append([down[k], line.own_s, model.lines[down[k - 1]].host_s])
        pieces.append([b["line"], model.lines[b["line"]].own_s, b["s"]])
    else:
        pieces.append([common, s, b["s"]])
    kept = [p for p in pieces if p[1] != p[2]]
    if not kept:
        kept = [[b["line"], b["s"], b["s"]]]
    return [{"line": p[0], "s0": p[1], "s1": p[2]} for p in kept]


def _on_piece(s: float, piece: dict, first: bool) -> bool:
    lo, hi = sorted((piece["s0"], piece["s1"]))
    if not lo - 1e-9 <= s <= hi + 1e-9:
        return False
    return not (first and abs(s - piece["s0"]) <= 1e-9)   # el punto de partida ya se conto en el tramo anterior


def _order_key(s: float, piece: dict) -> float:
    return s if piece["s1"] >= piece["s0"] else -s


def leg_extras(model: Model, path: list, filled: set) -> tuple:
    """via (puntos de paso) y mouths (bocas que se llenan por primera vez) de un tramo, en el orden del viaje."""
    via: list = []
    mouths: list = []
    in_path = {p["line"] for p in path}
    for k, piece in enumerate(path):
        first = k == 0
        here = sorted((p for p in model.passing if p["line"] == piece["line"] and _on_piece(p["s"], piece, first)),
                      key=lambda p: (_order_key(p["s"], piece), p["id"]))
        for p in here:
            if p["id"] not in via:
                via.append(p["id"])
        kids = sorted((model.lines[c] for c in model.order
                       if model.lines[c].host == piece["line"] and model.lines[c].kind != "main"
                       and _on_piece(model.lines[c].host_s, piece, first)),
                      key=lambda c: (_order_key(c.host_s, piece), c.id))
        for c in kids:
            if c.id not in in_path and c.id not in filled and c.id not in mouths:
                mouths.append(c.id)
    return via, mouths


# [P] Lo que va lleno desde el primer Mapa sin que la canoa pase su boca: el Tocantins (propuesta §3.3, punto 3, y §3.4: «el
# Tocantins ya va lleno en el primer Mapa»). Va en las bocas del tramo de la parada 0, que no tiene viaje.
INITIAL_MOUTHS = ("tocantins",)


def build_routes(model: Model, records: list) -> tuple:
    """at y leg de cada registro de paradas_1_200 y paradas_201_700, en orden de k. Devuelve ({k: (at, leg)}, errores)."""
    errors: list = []
    out: dict = {}
    filled: set = set()
    prev_at = None
    for r in sorted((r for r in records if isinstance(r.get("k"), int)), key=lambda r: r["k"]):
        at, errs = stop_at(model, r)
        errors += errs
        if at is None:
            prev_at = None
            continue
        if prev_at is None:
            path = [{"line": at["line"], "s0": at["s"], "s1": at["s"]}]
            mouths = [m for m in INITIAL_MOUTHS if m in model.lines] if r["k"] == 0 else []
            via: list = []
            missing = [m for m in INITIAL_MOUTHS if m not in model.lines]
            if r["k"] == 0 and missing:
                errors.append(f"INITIAL_MOUTHS: {missing} no son lineas del Mapa")
        else:
            path = route(model, prev_at, at)
            via, mouths = leg_extras(model, path, filled)
        filled.update(p["line"] for p in path)
        filled.update(mouths)
        out[r["k"]] = (at, {"path": path, "via": via, "mouths": mouths})
        prev_at = at
    return out, errors


def validate_map(doc: dict, model: Model, source: dict) -> list:
    """Reglas del documento que el esquema no expresa (geometry.py --check)."""
    errors: list = []
    frame = model.frame
    ids = set()
    for k, entry in enumerate(doc.get("lines", [])):
        rid = entry.get("id")
        tag = f"lines[{k}] ({rid})"
        if rid in ids:
            errors.append(f"{tag}: id repetido")
        ids.add(rid)
        try:
            pts = decode_points(entry["pts"])
        except (KeyError, ValueError, TypeError) as e:
            errors.append(f"{tag}: pts no se decodifica ({e})")
            continue
        line = model.lines.get(rid)
        if line is None or pts != line.q:
            errors.append(f"{tag}: pts no son los de la fuente")
            continue
        # Cada punto de control de la investigacion, a <= 2 dp de su linea en su vista (Cuenca con el respaldo).
        for j, p in enumerate(line.source["puntos"]):
            d = line.distance_km(float(p["lat"]), float(p["lon"]))
            if d > TOLERANCE_KM:
                errors.append(f"{tag}: el punto de control {j} ({p.get('nombre')!r}) queda a {d:.1f} km de la linea")
        water = entry.get("water", [])
        if water:
            if water[0]["s0"] != 0.0 or water[-1]["s1"] != 1.0 or any(
                    a["s1"] != b["s0"] for a, b in zip(water, water[1:])) or any(w["s0"] >= w["s1"] for w in water):
                errors.append(f"{tag}: water no cubre [0, 1] en tramos seguidos")
            if any(w["type"] not in WATERS for w in water):
                errors.append(f"{tag}: water con un tipo desconocido")
        mouth = entry.get("mouth")
        if entry.get("kind") == "main":
            if mouth is not None or "fill_on_pass" in entry:
                errors.append(f"{tag}: una linea main no lleva mouth ni fill_on_pass")
        else:
            if not isinstance(mouth, dict) or mouth.get("on") not in model.lines or mouth.get("on") == rid:
                errors.append(f"{tag}: mouth.on no es otra linea del Mapa")
            else:
                host = model.lines[mouth["on"]]
                lat, lon = line.ll[-1]
                d = haversine((lat, lon), host.point_at_s(mouth["s"]))
                allowed = PENDING_MOUTH_KM.get(rid, TOLERANCE_KM)
                if d > allowed:
                    errors.append(f"{tag}: su boca queda a {d:.1f} km de {host.id} en s = {mouth['s']} (maximo "
                                  f"{allowed:g} km{_limit_note(rid in PENDING_MOUTH_KM)}): mouth fuera de su rio")
            fill = entry.get("fill_on_pass")
            if not (isinstance(fill, (int, float)) and 0 < fill <= 1):
                errors.append(f"{tag}: fill_on_pass fuera de (0, 1]")
            elif entry.get("kind") == "passby" and fill != 1.0:
                errors.append(f"{tag}: un afluente de paso se llena entero (fill_on_pass = 1)")
        for sp in entry.get("splits", []):
            if not 0 <= sp["s0"] < sp["s1"] <= 1:
                errors.append(f"{tag}: split {sp.get('id')} con s0 >= s1")
    main = model.lines[MAIN_ID]
    pts = main.source["puntos"]
    for a, b in zip(pts, pts[1:]):
        dk = a["km_desde_el_mar"] - b["km_desde_el_mar"]
        g = haversine((a["lat"], a["lon"]), (b["lat"], b["lon"]))
        if dk + 0.5 < g:
            errors.append(f"cauce: {a.get('nombre')!r} -> {b.get('nombre')!r} mide {dk} km y la linea recta {g:.1f} km")
    # Tierra.
    land = doc.get("land", {})
    try:
        verts = decode_points(land["verts"])
        tris = decode_indices(land["tris"])
    except (KeyError, ValueError, TypeError) as e:
        return errors + [f"land: no se decodifica ({e})"]
    problems = polygon_problems(verts)
    if problems:
        errors.append("land: los vertices no son un poligono simple (" + "; ".join(problems) + ")")
    if len(tris) % 3 or any(i >= len(verts) for i in tris):
        errors.append("land: tris no son triples de indices de verts")
    else:
        area = 0
        for k in range(0, len(tris), 3):
            a, b, c = (verts[i] for i in tris[k:k + 3])
            cr = _cross(a, b, c)
            if cr <= 0:
                errors.append(f"land: el triangulo {k // 3} es degenerado o va en sentido contrario")
            area += cr
        if area != abs(_area2(verts)):
            errors.append(f"land: los triangulos suman {area} y el poligono {abs(_area2(verts))} (area x2)")
        zones = land.get("zones", [])
        if sum(z.get("count", 0) for z in zones) != len(tris) // 3:
            errors.append("land: las zonas no cubren todos los triangulos")
    home = doc.get("home")
    if not isinstance(home, dict) or home.get("on") not in model.lines:
        errors.append("home: on no es una linea del Mapa")
    names = set()
    for k, p in enumerate(doc.get("passing", [])):
        if p.get("line") not in model.lines:
            errors.append(f"passing[{k}] ({p.get('id')}): linea desconocida")
        if p.get("id") in names:
            errors.append(f"passing[{k}]: id {p.get('id')} repetido")
        names.add(p.get("id"))
    for k, v in enumerate(doc.get("viewpoints", [])):
        if v.get("id") in names:
            errors.append(f"viewpoints[{k}]: id {v.get('id')} repetido (tambien punto de paso o mirador)")
        names.add(v.get("id"))
    stop_ids = {r.get("id") for r in stop_records(source)}
    for clash in sorted(names & stop_ids):
        errors.append(f"{clash}: id de punto de paso o de mirador igual al de una parada (comparten map.stop.<id>.name)")
    for path, text in iter_strings(doc):
        if has_belen(text):
            errors.append(f"{path}: {text!r} cae en el veto de Belen (D-013, punto 11)")
    return errors


def same_place(model: Model, a: tuple, b: tuple) -> bool:
    """Dos posiciones (linea, s) son el mismo punto del agua: iguales, o la boca (o el arranque) de una linea y su s sobre
    el anfitrion. Es lo unico que une dos tramos de un leg: cualquier otro salto cruza tierra."""
    if a == b:
        return True
    for x, y in ((a, b), (b, a)):
        line = model.lines.get(x[0])
        if line is not None and line.host == y[0] and line.own_s == x[1] and line.host_s == y[1]:
            return True
    return False


def _limit_note(pending: bool) -> str:
    if pending:
        return f": excepcion medida pendiente del duenno; el canon es {TOLERANCE_KM:g} km"
    return ", <= 2 dp en la vista Cuenca"


def site_point(record: dict) -> tuple:
    """Punto de una parada que se valida contra su punto del rio: su atraque ({lat, lon}, un punto sobre el rio que da la
    investigacion para los sitios tierra adentro) o, sin el, su lat/lon. Devuelve ((lat, lon) o None, atraque?, problema)."""
    dock = record.get("atraque")
    if dock is not None:
        try:
            return (float(dock["lat"]), float(dock["lon"])), True, None
        except (TypeError, KeyError, ValueError):
            return None, True, "su atraque no es {lat, lon}"
    try:
        return (float(record["lat"]), float(record["lon"])), False, None
    except (TypeError, KeyError, ValueError):
        return None, False, "su registro de la investigacion no tiene lat/lon"


def pending_exceptions(stops_doc: dict, model: Model, records: list) -> list:
    """Avisos de las excepciones a TOLERANCE_KM que siguen en uso (pendientes del duenno) y de las que ya sobran."""
    warnings = []
    by_k = {r.get("k"): r for r in records if isinstance(r, dict)}
    for s in stops_doc.get("stops", []):
        sid, at = s.get("id"), s.get("at")
        if sid not in PENDING_SITE_KM or not isinstance(at, dict) or at.get("line") not in model.lines:
            continue
        place, docked, problem = site_point(by_k.get(s.get("n"), {}))
        if problem:
            continue
        if docked:
            warnings.append(f"{sid}: ya tiene atraque; sobra su excepcion de PENDING_SITE_KM ({PENDING_SITE_KM[sid]:g} km)")
        else:
            d = haversine(place, model.lines[at["line"]].point_at_s(at["s"]))
            warnings.append(f"{sid}: sin atraque en la investigacion; el centro queda a {d:.1f} km de su punto del rio "
                            f"(canon {TOLERANCE_KM:g} km; excepcion {PENDING_SITE_KM[sid]:g} km pendiente del duenno)")
    for rid, limit in sorted(PENDING_MOUTH_KM.items()):
        line = model.lines.get(rid)
        if line is None or line.host is None:
            continue
        d = haversine(line.ll[-1], model.lines[line.host].point_at_s(line.host_s))
        warnings.append(f"linea {rid}: su boca queda a {d:.1f} km de {line.host} (canon {TOLERANCE_KM:g} km; excepcion "
                        f"{limit:g} km pendiente del duenno)")
    return warnings


def validate_routes(stops_doc: dict, model: Model, records: list) -> list:
    """Rutas de river_stops_v1.json (at y leg) contra el Mapa: paradas en su rio, km, continuidad y que no retroceden."""
    errors: list = []
    by_k = {r.get("k"): r for r in records if isinstance(r, dict)}
    prev = None
    for s in stops_doc.get("stops", []):
        n, sid = s.get("n"), s.get("id")
        tag = f"parada n={n!r} ({sid!r})"
        at, leg = s.get("at"), s.get("leg")
        if not isinstance(at, dict) or at.get("line") not in model.lines or not isinstance(at.get("s"), (int, float)):
            errors.append(f"{tag}: at no es {{line, s}} sobre una linea del Mapa")
            prev = None
            continue
        line = model.lines[at["line"]]
        rec = by_k.get(n, {})
        pos = line.point_at_s(at["s"])
        place, docked, problem = site_point(rec)
        if problem:
            errors.append(f"{tag}: {problem}")
        else:
            d = haversine(place, pos)
            pending = not docked and sid in PENDING_SITE_KM
            allowed = PENDING_SITE_KM[sid] if pending else TOLERANCE_KM
            if d > allowed:
                what = "su punto de atraque" if docked else "el sitio"
                errors.append(f"{tag}: {what} queda a {d:.1f} km de su punto del rio ({at['line']}, s = {at['s']}); "
                              f"maximo {allowed:g} km{_limit_note(pending)}")
        km_here = model.km_at(at["line"], at["s"])
        km_value = s.get("km_from_sea")
        if km_value is not None and (km_here is None or abs(km_here - km_value) > TOLERANCE_KM):
            errors.append(f"{tag}: km_from_sea {km_value} y su punto del rio esta en el km "
                          f"{'?' if km_here is None else f'{km_here:.0f}'}")
        if prev is not None and prev["at"]["line"] == at["line"]:
            if at["s"] > prev["at"]["s"] + 1e-9:
                errors.append(f"{tag}: retrocede rio abajo por {at['line']} desde la parada {prev['n']} (los km no retroceden)")
            if km_value is not None and prev.get("km") is not None and km_value < prev["km"]:
                errors.append(f"{tag}: km_from_sea {km_value} menor que el de la parada {prev['n']} ({prev['km']}) en el mismo rio")
        # leg
        if not isinstance(leg, dict) or not isinstance(leg.get("path"), list) or not leg["path"]:
            errors.append(f"{tag}: sin leg.path")
        else:
            path = leg["path"]
            ok = True
            for k, p in enumerate(path):
                if p.get("line") not in model.lines or not all(isinstance(p.get(f), (int, float)) and 0 <= p[f] <= 1
                                                               for f in ("s0", "s1")):
                    errors.append(f"{tag}: leg.path[{k}] no es {{line, s0, s1}} sobre una linea del Mapa")
                    ok = False
            if ok:
                def gap(x: tuple, y: tuple) -> float:
                    return haversine(model.lines[x[0]].point_at_s(x[1]), model.lines[y[0]].point_at_s(y[1]))

                start = (prev["at"]["line"], prev["at"]["s"]) if prev is not None else (at["line"], at["s"])
                first = (path[0]["line"], path[0]["s0"])
                if not same_place(model, start, first):
                    errors.append(f"{tag}: leg roto: empieza en {first[0]} s = {first[1]} y la canoa esta en {start[0]} "
                                  f"s = {start[1]} (salto de {gap(start, first):.1f} km por tierra)")
                last = (path[-1]["line"], path[-1]["s1"])
                if not same_place(model, last, (at["line"], at["s"])):
                    errors.append(f"{tag}: leg roto: acaba en {last[0]} s = {last[1]} y la parada esta en {at['line']} "
                                  f"s = {at['s']} (salto de {gap(last, (at['line'], at['s'])):.1f} km por tierra)")
                for k in range(len(path) - 1):
                    a = (path[k]["line"], path[k]["s1"])
                    b = (path[k + 1]["line"], path[k + 1]["s0"])
                    if not same_place(model, a, b):
                        errors.append(f"{tag}: leg roto entre path[{k}] ({a[0]} s = {a[1]}) y path[{k + 1}] ({b[0]} s = "
                                      f"{b[1]}): no es una confluencia, salto de {gap(a, b):.1f} km por tierra (las rutas "
                                      "no cruzan tierra)")
            for v in leg.get("via", []):
                if v not in {p["id"] for p in model.passing}:
                    errors.append(f"{tag}: via {v!r} no es ningun punto de paso del Mapa")
            for m in leg.get("mouths", []):
                if m not in model.lines or model.lines[m].kind == "main":
                    errors.append(f"{tag}: mouths {m!r} no es ningun afluente del Mapa")
        prev = {"n": n, "at": at, "km": km_value}
    return errors


# --------------------------------------------------------------------------------------------- salida

def _rel(path: Path) -> str:
    try:
        return Path(path).resolve().relative_to(ROOT).as_posix()
    except ValueError:
        return str(path)


def compare(text: str, out: Path) -> list:
    try:
        actual = Path(out).read_bytes()
    except FileNotFoundError:
        return [f"falta {_rel(out)}: generalo con py -3.14 tools/river-map/geometry.py"]
    if actual == text.encode("utf-8"):
        return []
    got = actual.decode("utf-8", "replace").split("\n")
    want = text.split("\n")
    line = next((i + 1 for i, (a, b) in enumerate(zip(got, want)) if a != b), min(len(got), len(want)) + 1)
    return [f"desfase: {_rel(out)} no coincide con lo que genera la fuente (primera diferencia en la linea {line}); "
            "regeneralo con py -3.14 tools/river-map/geometry.py"]


def emit(message: str) -> None:
    print(ascii_text(message))


def main(argv: list | None = None) -> int:
    ap = argparse.ArgumentParser(description="Genera o comprueba river_map_v1.json (af-river-map/1).")
    ap.add_argument("--check", action="store_true",
                    help="valida el Mapa y las rutas de river_stops_v1.json y compara con lo commiteado; codigo 1 si algo falla")
    ap.add_argument("--source", type=Path, default=SOURCE_PATH, help="rio-amazonas.json (por defecto, el de docs/research)")
    ap.add_argument("--overrides", type=Path, default=OVERRIDES_PATH, help="decisiones [P] por parada (stops.py)")
    ap.add_argument("--worldtable", type=Path, default=WORLDTABLE_PATH, help="WorldTable.cs del cliente")
    ap.add_argument("--out", type=Path, default=MAP_PATH, help="river_map_v1.json que se escribe o se compara")
    ap.add_argument("--stops", type=Path, default=STOPS_PATH, help="river_stops_v1.json cuyas rutas se comprueban")
    ap.add_argument("--facts", type=Path, default=FACTS_PATH, help="facts.csv donde se buscan los datos de passing y viewpoints")
    args = ap.parse_args(argv)
    try:
        source = load_json(args.source)
        doc, model, errors = build_map(source)
    except (SourceError, GeometryError) as e:
        emit(f"FAIL  {e}")
        return 1
    if not errors:
        errors = validate_map(doc, model, source)
    if errors:
        for err in errors:
            emit(f"FAIL  {err}")
        emit(f"geometry.py: errores: {len(errors)}")
        return 1
    text = render(doc)
    if not args.check:
        out = Path(args.out)
        out.parent.mkdir(parents=True, exist_ok=True)
        out.write_bytes(text.encode("utf-8"))
        emit(f"OK  escrito {_rel(out)}: {len(doc['lines'])} lineas, {len(doc['passing'])} puntos de paso, "
             f"{len(text.encode('utf-8'))} bytes")
        return 0
    errors = compare(text, args.out)
    # Las rutas commiteadas, contra este Mapa; y todas las reglas de las paradas (stops.py), candidatos incluidos.
    import stops as stops_tool   # aqui y no arriba: stops.py importa este modulo
    try:
        committed = load_json(args.stops)
        records = stop_records(source)
        errors += validate_routes(committed, model, records)
        overrides = load_json(args.overrides)
        wt = WorldTable.from_cs(args.worldtable)
        _, _, errs = stops_tool.build_all(source, overrides, wt)
        errors += [f"stops.py: {e}" for e in errs]
    except SourceError as e:
        errors.append(str(e))
    for err in errors:
        emit(f"FAIL  {err}")
    if errors:
        return 1
    emit(f"OK  {len(doc['lines'])} lineas, {len(doc['passing'])} puntos de paso y {len(doc['viewpoints'])} miradores "
         f"validos y {_rel(args.out)} al dia")
    emit(f"OK  rutas de {_rel(args.stops)} (at y leg de {len(committed['stops'])} paradas) de acuerdo con el Mapa")
    for warning in pending_exceptions(committed, model, records) + pending_facts(doc, args.facts):
        emit(f"AVISO {warning}")
    return 0


def pending_facts(doc: dict, facts_path: Path) -> list:
    """Datos de los puntos de paso y de los miradores sin fila en facts.csv: la investigacion los identifica (su clave)
    pero aun no da su texto final (propuesta del Mapa §5.2). No es un error: el cliente no ensenna un dato sin texto
    (Loc.Fact devuelve «»); se avisa para que no se olviden."""
    try:
        records = split_csv_records(Path(facts_path).read_bytes().decode("utf-8"))
        keys = {parse_csv_record(r)[0] for r in records[1:] if r.strip()}
    except (OSError, UnicodeDecodeError, ValueError):
        return [f"no se puede leer {_rel(facts_path)} para buscar los datos de los puntos de paso y de los miradores"]
    out = []
    for kind, items in (("punto de paso", doc["passing"]), ("mirador", doc["viewpoints"])):
        for item in items:
            if item.get("fact") and item["fact"] not in keys:
                out.append(f"{item['fact']}: dato del {kind} {item['id']} sin texto en {_rel(facts_path)} (la "
                           "investigacion no da su texto final; pendiente)")
    return out


if __name__ == "__main__":
    sys.exit(main())
