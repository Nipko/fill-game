#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""geometry.py - Mapa del rio (D-013): genera y comprueba la geometria af-river-map/1.

Escribe Assets/_Project/Data/Map/river_map_v1.json (contrato 29; 08 §4.5 con los extras [P] de P-M1, D-023) desde la
geometria de produccion (M5.1; D-013, punto 5, Q11-A; propuesta del Mapa §3.6 y §11.3): las descargas de Natural Earth
1:10m y HydroRIVERS v1.0 en tools/river-map/source/ (sin versionar; sus SHA-256 y licencias, en sources.json), leidas con
shapefile.py. docs/research/mapa-del-rio/rio-amazonas.json (que nunca modifica) da los rios, sus puntos de control, las
paradas y los km. Solo biblioteca estandar de Python 3.14 (08 §4.5).

Uso (desde la raiz del repositorio):
    py -3.14 tools/river-map/geometry.py            # escribe river_map_v1.json (necesita las descargas)
    py -3.14 tools/river-map/geometry.py --check    # valida el Mapa y las rutas de las paradas; codigo 1 si algo falla

Tres geometrias (Geometry; las tres pasan por el mismo build_map):
- produccion (las descargas completas y con su SHA-256): se regenera todo. Lo usa el escritorio.
- commiteado (sin ninguna descarga, solo con --check): los puntos de cada linea y la tierra (verts, tris y zones) se toman
  de river_map_v1.json tal cual, y todo lo demas (s, bocas, aguas, franjas, km, puntos de paso, rutas...) se regenera y se
  compara byte a byte, con todas las validaciones. Lo usa el portatil, que no tiene las descargas; tambien stops.py, que
  saca las rutas del Mapa commiteado. Sin las descargas y sin --check no se escribe nada (FAIL).
- respaldo (build_map sin geometria; solo las pruebas): los puntos de la investigacion (src = rio_amazonas), la geometria
  de M2.6.
Con unas descargas incompletas o con otro SHA-256, FAIL (o estan todas o ninguna).

Opciones: --sources (la carpeta de las descargas) y --manifest (sources.json); --source, --overrides, --worldtable, --out,
--stops y --facts cambian las rutas para las pruebas. Determinista: misma entrada, mismos bytes (UTF-8 sin BOM, LF, orden de
claves fijo y una linea por elemento). Mensajes solo en ASCII.

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
  del Mismi al Atlantico; en el ramal del Para, de Gurupa a la bahia), asi que s crece rio abajo [P].

Geometria de produccion (M5.1):
- Lineas (src): HydroRIVERS por defecto. Cada rio, por la red (NEXT_DOWN) desde el tramo junto a su cabecera de la
  investigacion que pasa a <= TOLERANCE_KM de mas puntos de control, hasta su anfitrion: la boca es un vertice del
  anfitrion. Un rio «solo_boca» sube desde su boca por el tramo de mas cuenca. El cauce y el Tocantins acaban en el mar de
  HydroRIVERS, que no dibuja los estuarios como rio: siguen con los puntos de la investigacion (la cola del canal norte y la
  del estuario del Para). LINE_SRC: el ramal del Para, el Itaya y el Pacaya, de la investigacion; el Casiquiare, de
  Natural Earth (una bifurcacion).
  DESVIACION [P] DEL CANON, pendiente del duenno (pregunta 65 propuesta, parte 2; README, notas de M5.1): D-013, punto 5
  (Q11-A), 08 §4.5 (el «Origen» y su ejemplo, con src «ne10m» en el cauce) y la propuesta del Mapa (§3.6 y §11.3, punto 1)
  piden Natural Earth 1:10m para la vista Cuenca y HydroRIVERS para la vista Mundo. Aqui las lineas salen de HydroRIVERS en
  las dos vistas (la misma linea, simplificada por vista: DP_CUENCA_KM y DP_MUNDO_KM), y de Natural Earth solo la tierra y
  el Casiquiare. Por que: el contrato 29 da un solo pts por linea (no hay una linea por vista), y Natural Earth no tiene la
  red (ni NEXT_DOWN ni las bocas en un vertice del anfitrion), le faltan 11 rios del Mapa (el Takutu, el Nanay, el
  Tambopata, el Manu...) y la propia Natural Earth avisa de «suspect alignments» en partes de la cuenca del Amazonas (su
  pagina de los rios; propuesta §3.6, [V ne_rivers]). No es canon hasta que el duenno lo ratifique y el carril de
  documentacion alinee 08 §4.5.
- Douglas-Peucker por vista: DP_MUNDO_KM a <= MUNDO_RADIUS_KM de una parada de la 1.0 (MundoZone) y DP_CUENCA_KM en el
  resto; las bocas, los cortes y las colas no se simplifican. MundoZone [P] no es todo lo que ensenna la vista Mundo (de 4x a
  15x y se arrastra por todo el mapa): fuera de ella, a 4x y 15x, los 6 km de DP_CUENCA_KM son ~2,8 y ~10,6 dp, por encima
  de los 2 dp de esa vista (pregunta 65 propuesta, parte 3).
- Km: el cauce, km_desde_el_mar de sus puntos de control y de las paradas a <= TOLERANCE_KM, interpolado en s; los
  afluentes, el km de su boca de la investigacion mas el recorrido sin meandros por sus puntos (como la investigacion).
- Tierra: Natural Earth land recortada a la caja (el continente y las islas de al menos MIN_ISLAND_KM2), simplificada por
  vista sin que la costa se corte y triangulada por orejas, anillo a anillo; zonas: land_zones, el punto de entrada de la
  mascara del artista (pendiente).

Que escribe:
- lines: el cauce y el ramal del Para (main), los afluentes de los mundos (tributary), los de paso con el Tocantins
  (passby) y los rios locales de las paradas 11, 13, 15, 16 y 18 y, desde M4.5a, los de rios_locales_201_700 (el Aguarico,
  el Cuyabeno, el Tambopata y el Manu de las paradas 25-26, 35-37 y 40-41; local); los dos rios de fuera de la cuenca van en los
  recuadros, que la 1.0 deja vacios (insets = []). Cada linea con su class [P] (1 el cauce y el ramal, 2 los afluentes, 3
  los locales), su src, su agua (water, la de Sioli de la investigacion: white, black, clear y andean en la cabecera del
  cauce; sin tramos si la investigacion la da [NV]), su boca (mouth: la linea y la s donde entra) y fill_on_pass (1 en los
  de paso y los locales; en los afluentes, la fraccion de sus primeros MOUTH_FILL_KM km desde la boca [P]).
- splits: las dos aguas lado a lado de Santarem y del Encuentro (propuesta §3.4), desde la confluencia SPLIT_KM km rio
  abajo [P], con el agua de cada orilla mirando rio abajo (left y right).
- land: la tierra (verts, tris y zones), con una sola zona [P] hasta la mascara del artista.
- home: la casa de TiTino, en Belem, en la orilla de la bahia de Guajara (notas de la parada 0).
- labels (los paises en su region, rotulos_de_pais), viewpoints (los miradores, reveal_on = 10 [P]) y passing (los puntos
  de paso de 1-200 y, desde M4.5a, los de los viajes de entrada que llevan id, como teotonio en el del 300, con su linea,
  su s y la clave de su dato, si la tiene); los nombres van por convencion en map.stop.<id>.name y
  map.country.<iso2>.name, en el bloque map.* de facts.csv que escribe stops.py.

--check (codigo 1 si algo falla): lo commiteado es lo que se regenera (desfase); cada punto de control de la investigacion
queda a <= 2 dp de su linea en su vista (la Mundo, <= MUNDO_TOLERANCE_KM; la Cuenca, <= TOLERANCE_KM); cada tramo del cauce
mide al menos la linea recta; cada boca cae sobre su rio; la tierra esta bien triangulada (land_problems: sus aristas sin
pareja son anillos simples que no se cortan y suman el area de los triangulos); el archivo mide <= MAX_BYTES; y las rutas
de river_stops_v1.json (at y leg) son las de este Mapa: paradas a <= TOLERANCE_KM km de su punto del rio (medidos desde su
punto de atraque si la investigacion lo da), km de acuerdo con km_from_sea y que no retroceden, tramos continuos que no
cruzan tierra (dos tramos solo se unen en el mismo (line, s) o en una confluencia: la boca de un afluente, o el arranque
del ramal, y su s sobre el anfitrion; cualquier otro salto cruza tierra), via y mouths existentes; ademas, todas las reglas de
stops.py (candidatos de la propuesta §5.1 bis incluidos). Las unicas excepciones a las tolerancias son las [P] pendientes
del duenno (PENDING_SITE_KM, PENDING_MOUTH_KM y PENDING_CONTROL_KM: la cifra medida, sin margen; pregunta 65 propuesta): con
ellas, el criterio de M5.1 de las dos vistas aun no esta cumplido. Avisa (sin fallar) de cada excepcion en uso, de las
lineas que siguen siendo el esbozo de la investigacion (pending_sketches), de las descargas ausentes y de los datos de los
puntos de paso y de los miradores que aun no tienen texto en facts.csv.

Paradas en una confluencia [P] (revision de M5.1; confluence_s): una parada cuyo sitio queda a <= CONFLUENCE_KM del punto
donde la investigacion pone la boca de un afluente de su linea se ancla en la s de esa boca (Santarem, el Encuentro, Nauta,
Boca Manu, Riberalta...): al llegar se cruza la boca y su afluente se llena.
"""
from __future__ import annotations

import argparse
import base64
import hashlib
import json
import math
import re
import struct
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from shapefile import Layer, ShapefileError  # noqa: E402  (M5.1: lector de .shp y .dbf de la biblioteca estandar)
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
    travel_passing_mismatches,
    travel_passing_points,
)

FORMAT = "af-river-map/1"
SRC = "rio_amazonas"   # la geometria de respaldo (D-013, punto 5) y las lineas de la investigacion (LINE_SRC; M5.1)
Q = 65535              # uint16: X = round(Q * x), Y = round(Q * y)
EARTH_KM = 6371.0

# Tolerancia de la validacion (D-013, punto 5; propuesta §3.6 y §11.3): <= 2 dp de la vista donde se ve cada punto. En la
# vista Cuenca, 13,1 dp por grado, 2 dp = ~15 km [calculo de la propuesta]; la vista Mundo (<= 1 km a 15x,
# MUNDO_TOLERANCE_KM) rige con la geometria de produccion (M5.1) en la zona de las paradas (MundoZone). La geometria de
# respaldo solo es correcta a escala de cuenca (§11.3, punto 7): se valida solo en la Cuenca.
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

# [P] Excepciones a las tolerancias del canon, PENDIENTES DEL DUENNO (no son canon). El canon (D-013, punto 5; 08 §4.5; D-023,
# punto 20) pide cada punto a <= 2 dp de su vista: <= 15 km en la Cuenca (TOLERANCE_KM) y, con la geometria de produccion,
# <= 1 km en la vista Mundo (MUNDO_TOLERANCE_KM). Con HydroRIVERS no se cumple sin estas excepciones, asi que el criterio de
# M5.1 («--check en verde con las tolerancias de las dos vistas») NO esta cumplido: --check pasa porque estas cifras son la
# distancia MEDIDA con los datos de hoy (redondeada hacia arriba a 0,1 km, sin margen), congelada para que falle si un dato
# se aleja mas; avisa de cada una. Se proponen al duenno como pregunta 65 de docs/GDD.md §6.3 (el siguiente numero libre en
# 5d2a569; lo registra el carril de documentacion), con cuatro opciones (README, notas de M5.1): (a) medir contra la orilla o
# el ancho del cauce y no contra su linea central; (b) en la vista Mundo, 2 dp al zoom real de cada mundo
# (RiverMapScreen.WorldZoom, de 4x a 15x: de ~4,2 a ~1,1 km) y no 1 km en todas partes; (c) que el investigador corrija los
# puntos [E]; (d) aceptar estas excepciones.
# - Sitios tierra adentro: el punto de la investigacion es el centro del area protegida, no un punto del rio. Se validan
#   por su punto de atraque (campo «atraque» {lat, lon} del registro de la investigacion) con los 15 km del canon; mientras
#   la investigacion no lo anada, rige la cifra medida entre el centro y su punto del rio. Medidas sobre la geometria de
#   produccion (M5.1; con el respaldo eran 23,1, 57,5 y 46,2 km): sus rios de HydroRIVERS pasan por otro sitio que la
#   poligonal recta de la investigacion. Yasuni (y su variante) entra con M5.1: a unos 9 km del Napo de respaldo, queda a
#   26 km del Napo de HydroRIVERS (su punto del rio, a 28,9 km).
PENDING_SITE_KM = {
    "flona_tapajos": 33.3,    # centro de la FLONA do Tapajos (notas: «la canoa se amarra en la orilla del Tapajos mas cercana»)
    "amacayacu": 58.6,        # centro del parque (notas: al norte del rio); su punto del rio, km 3.185 («frente del parque»)
    "pacaya_samiria": 78.6,   # centro de la reserva (notas: «la canoa entra por la boca del Samiria, en el Maranon»)
    "pacaya_alba": 78.6,      # la misma reserva, al alba
    "yasuni": 29.0,           # coordenada de Wikidata del parque (Q1641326), sin atraque
    "yasuni_dosel": 29.0,     # el mismo parque
}
# - Bocas: la del Pacaya esta en el canal de Puinahua, que el cauce no dibuja: con el respaldo, a 37,08 km; con el cauce de
#   HydroRIVERS (M5.1), a 23,83 km. El Pacaya de HydroRIVERS desde la cabecera de la investigacion no pasa por su boca
#   (va a otro rio): la linea sigue siendo el esbozo de la investigacion (LINE_SRC).
PENDING_MOUTH_KM = {"pacaya": 23.9}
# - Puntos de control de la investigacion sobre la geometria de produccion (M5.1; propuesta §3.6 y §11.3, punto 4): a
#   <= 15 km en la vista Cuenca y a <= 1 km en la vista Mundo (MundoZone). Los puntos de control son ciudades en la orilla,
#   bocas y confluencias estimadas [E] de rios de 1 a 20 km de ancho, y la linea de HydroRIVERS va por el centro de su
#   cauce principal. De los 299 puntos de las lineas de produccion, 129 caen en la vista Mundo: 70 quedan a mas de 1 km
#   (36 del cauce, de sus 105 puntos; Pucallpa, 5,6 km; Iquitos, 4,8; Careiro, 9,4...), 32 a mas de 2,1 km (2 dp a 8x), 17
#   a mas de 4,2 km (2 dp a 4x) y 2 a mas de 15 km (la boca del Curua, 27,5 km, y Belterra, 22,6 km); en la Cuenca, 1 (la
#   boca del Nhamunda, 33,7 km): entran por un lago o un brazo, o la ciudad esta en la meseta. {linea: {indice del punto: km medidos,
#   redondeados hacia arriba a 0,1 km y sin margen}}. Solo con la geometria de produccion (las lineas de la investigacion no
#   tienen excepciones; pending_sketches avisa de ellas).
PENDING_CONTROL_KM: dict = {
    "amazonas": {22: 1.2, 23: 1.4, 26: 1.8, 29: 4.5, 30: 1.9, 31: 3.0, 32: 5.6, 33: 1.8, 36: 1.4, 37: 1.3, 39: 1.1,
                 40: 8.1, 42: 2.6, 43: 1.1, 44: 1.4, 46: 4.8, 47: 3.1, 48: 2.1, 49: 1.9, 52: 2.3, 53: 2.8, 54: 2.8,
                 55: 2.9, 56: 1.5, 58: 13.0, 66: 3.5, 67: 2.8, 68: 4.1, 69: 4.9, 74: 4.8, 75: 5.1, 77: 9.5, 78: 8.8,
                 84: 33.7, 87: 1.2, 88: 27.5, 90: 4.8},
    "tapajos": {7: 2.6, 8: 2.0, 9: 2.4, 10: 22.6, 11: 12.7, 12: 2.7},
    "negro": {13: 6.3, 14: 1.9, 15: 1.6, 16: 2.1, 17: 1.9},
    "putumayo_ica": {1: 2.0, 6: 1.3, 9: 7.1, 10: 1.2},
    "napo": {3: 1.1, 5: 1.1, 6: 2.2, 9: 1.7},
    "maranon": {14: 8.4, 16: 1.4, 17: 1.5},
    "madre_de_dios": {3: 1.3, 4: 1.3, 8: 1.3},
    "beni": {5: 1.9},
    "mamore": {4: 1.3, 8: 3.3},
    "japura": {2: 1.1},
    "nanay": {1: 1.4},
    "atacuari": {0: 1.9},
    "aguarico": {2: 1.2, 3: 1.7},
    "manu": {0: 1.1},
}

# [P] Afluentes: «solo su boca (unos 80-120 km)» al pasarla (propuesta §3.4); los de paso y los locales, enteros.
MOUTH_FILL_KM = 100.0
# [P] Dos aguas lado a lado (propuesta §3.4: «unos 30 dp» en la vista Mundo; la muestra parte ~65 km).
SPLIT_KM = 65.0
SPLITS = (("encontro", "negro"), ("santarem", "tapajos"))   # (id, afluente que da la segunda agua)
SPLIT_SIDE_KM = 10.0   # de que orilla llega el afluente: su punto a 10 km de la boca frente al cauce +-10 km (revision de M5.1)
# [P] Los miradores de 1-200 estan en el estuario: el primer Mapa (nivel 10) ya los revela (propuesta §5.2).
VIEWPOINT_REVEAL_ON = 10
# [P] Una sola zona de tierra hasta la mascara del artista (su punto de entrada: land_zones; M5.1).
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
        self.km_from_sea = km_from_sea   # solo el cauce: km de cada punto (investigacion o calibrados, M5.1)
        # Km de cada punto hasta la boca (afluentes y locales): con el respaldo, la longitud de la propia linea; con la
        # geometria de produccion (M5.1), los km de la investigacion en sus puntos de control (calibrate).
        self.up_km = [self.arc_km[-1] - a for a in self.arc_km]
        self.src = SRC
        self.ctrl_s: list = []             # s de cada punto de control de la investigacion (en su orden)
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
        """Km por la linea desde su boca (su ultimo punto) hasta (i, t) (los desvios: «confluencia mas el recorrido sin
        meandros», propuesta §5.1): up_km interpolado (con el respaldo, la longitud geodesica de la propia linea)."""
        if len(self.ll) == 1:
            return 0.0
        if i >= len(self.seg_km):
            return self.up_km[-1]
        return self.up_km[i] + t * (self.up_km[i + 1] - self.up_km[i])

    def locate_arc_from_end(self, km: float) -> tuple:
        """(i, t) del punto a km por la propia linea (geodesica) desde su ultimo punto; el primero si la linea es mas corta."""
        if len(self.ll) == 1:
            return 0, 0.0
        target = max(0.0, self.arc_km[-1] - km)
        for i, d in enumerate(self.seg_km):
            if self.arc_km[i + 1] >= target and d > 0:
                return i, min(1.0, max(0.0, (target - self.arc_km[i]) / d))
        return len(self.seg_km) - 1, 1.0

    def locate_up_from_mouth(self, dkm: float) -> tuple | None:
        if len(self.ll) == 1:
            return (0, 0.0) if abs(dkm) < 1e-9 else None
        if dkm < -1e-9 or dkm > self.up_km[0] + 1e-9:
            return None
        dkm = max(0.0, min(self.up_km[0], dkm))
        for i in range(len(self.seg_km)):
            a, b = self.up_km[i], self.up_km[i + 1]
            if b <= dkm + 1e-12 and a > b:
                return i, min(1.0, max(0.0, (a - dkm) / (a - b)))
        return len(self.seg_km) - 1, 1.0

    def constrained_projection(self, lat: float, lon: float, start: int) -> tuple:
        """Pie de la perpendicular sobre los segmentos desde el indice start (los puntos de control van en orden, de la
        cabecera a la boca: un punto no se proyecta rio arriba del anterior). Devuelve (i, t, distancia en km al pie)."""
        if len(self.q) == 1:
            return 0, 0.0, haversine((lat, lon), self.ll[0])
        p = self.frame.screen(*_quant_float(self.frame, lat, lon))
        best = None
        for i in range(max(0, min(start, len(self.q) - 2)), len(self.q) - 1):
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

    def calibrate(self, anchors: list) -> list:
        """Km de cada punto por interpolacion lineal en s entre anclas (s, km) (M5.1): los puntos de control de la
        investigacion sobre la geometria de produccion. Las anclas van con s creciente y km decreciente; las que no lo
        cumplen se saltan. Devuelve los km de cada punto de la linea."""
        clean: list = []
        for s, km in sorted(anchors, key=lambda a: a[0]):
            if clean and (s <= clean[-1][0] + 1e-12 or km >= clean[-1][1]):
                continue
            clean.append((s, km))
        out = []
        k = 1
        for i in range(len(self.q)):
            s = self.s_of(i, 0.0) if i < len(self.seg) else 1.0
            if s <= clean[0][0]:
                out.append(clean[0][1])
                continue
            if s >= clean[-1][0]:
                out.append(clean[-1][1])
                continue
            while clean[k][0] < s:
                k += 1
            (s0, v0), (s1, v1) = clean[k - 1], clean[k]
            out.append(v0 + (s - s0) / (s1 - s0) * (v1 - v0))
        return out


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


class Geometry:
    """Puntos cuantizados de cada linea y tierra ya triangulada, de una fuente distinta del respaldo (M5.1):
    - kind = "produccion": las descargas de tools/river-map/source/ (Natural Earth y HydroRIVERS; production_geometry);
    - kind = "commiteado": lo que ya esta en river_map_v1.json (committed_geometry), para comprobar sin las descargas y
      para las rutas de stops.py.
    lines = {id: (src, [(X, Y), ...])}; land = (verts, tris, zones) con zones = [{id, tile, first, count}]."""

    def __init__(self, kind: str, lines: dict, land: tuple):
        self.kind = kind
        self.lines = lines
        self.land = land


def _research_arc_up(ctrl: list) -> list:
    """Km de cada punto de control hasta el ultimo por la poligonal de la investigacion (geodesicos, sin meandros)."""
    up = [0.0] * len(ctrl)
    for j in range(len(ctrl) - 2, -1, -1):
        up[j] = up[j + 1] + haversine(ctrl[j], ctrl[j + 1])
    return up


def _stop_anchors(line: "Line", source: dict) -> list:
    """Anclas (s, km desde el mar) de las paradas en esta linea (M5.1): las de la investigacion con ancla_rio = la linea,
    km_desde_el_mar y su sitio (o su atraque) a <= TOLERANCE_KM de la linea, en el pie de su sitio. Asi cada parada cae
    junto a su sitio aunque los meandros de la geometria de produccion no se repartan como la poligonal de la
    investigacion. No anclan los sitios tierra adentro sin atraque (PENDING_SITE_KM)."""
    out = []
    for r in stop_records(source):
        if r.get("ancla_rio") != line.id or not isinstance(r.get("km_desde_el_mar"), (int, float)):
            continue
        place, docked, problem = site_point(r)
        if problem or (not docked and r.get("id") in PENDING_SITE_KM):
            continue
        i, t, d = line.project(*place)
        if d <= TOLERANCE_KM:
            out.append((line.s_of(i, t), float(r["km_desde_el_mar"])))
    return out


def _calibrate(line: "Line", ctrl: list, km: list | None, source: dict) -> None:
    """Puntos de control de la investigacion sobre una linea de produccion (M5.1): su s, en orden y sin volver rio
    arriba (ctrl_s), los que quedan a <= TOLERANCE_KM (anchored, las anclas de los km) y, en el cauce, los km de cada
    punto: km_desde_el_mar interpolado entre las anclas, con los extremos en s = 0 y s = 1. Los km de los afluentes
    necesitan su anfitrion: _calibrate_up, cuando ya estan todas las bocas."""
    start = 0
    line.ctrl_s = []
    line.ctrl = list(ctrl)
    anchored = []
    for j, (lat, lon) in enumerate(ctrl):
        i, t, d = line.constrained_projection(lat, lon, start)
        s = line.s_of(i, t)
        line.ctrl_s.append(_round_s(s))
        # Un punto que se proyecta rio arriba del anterior anclado (en el mismo segmento) no ancla los km: con el cauce
        # simplificado (revision de M5.1), Careiro da Varzea, en la orilla sur y a 9,5 km de la linea, caia rio arriba del
        # Encuentro y su km (1.424) dejaba fuera el del Encuentro (1.437), y con el los km del Negro.
        if d <= TOLERANCE_KM and (not anchored or s >= anchored[-1][1]):
            start = i
            anchored.append((j, s))
    line.anchored = anchored
    last = len(ctrl) - 1
    if line.kind == "main" and km is not None and len(km) == len(ctrl) and len(ctrl) >= 2:
        anchors = [(0.0, float(km[0])), (1.0, float(km[last]))]
        anchors += [(s, float(km[j])) for j, s in anchored if 0 < j < last]
        line.km_from_sea = line.calibrate(anchors + _stop_anchors(line, source))


def _calibrate_up(model: "Model", source: dict) -> None:
    """Km hasta la boca de cada afluente y rio local de produccion (M5.1), como los mide la investigacion: el km de su boca
    de la investigacion sobre el anfitrion (el de su ultimo punto de control proyectado en el) mas el recorrido sin
    meandros por sus puntos. Las anclas son sus puntos de control a <= TOLERANCE_KM y, en s = 1, la boca de la linea (su
    km sobre el anfitrion); en s = 0, el primero. Sin km en el anfitrion (el Tocantins, en el ramal), o con un solo punto
    de control, se queda la longitud de la propia linea. Por profundidad: cada anfitrion antes que sus afluentes."""
    depth = {rid: len(model.chain(rid)) for rid in model.order}
    for rid in sorted(model.order, key=lambda r: (depth[r], model.order.index(r))):
        line = model.lines[rid]
        ctrl = getattr(line, "ctrl", None)
        if line.kind == "main" or line.host is None or not ctrl or len(ctrl) < 2:
            continue
        host = model.lines[line.host]
        base = model.km_at(line.host, line.host_s)
        i, t, _ = host.project(*ctrl[-1])
        mouth_km = model.km_at(line.host, host.s_of(i, t))
        if base is None or mouth_km is None:
            continue
        up = _research_arc_up(ctrl)
        last = len(ctrl) - 1
        anchors = [(0.0, mouth_km + up[0] - base), (1.0, 0.0)]
        anchors += [(s, mouth_km + up[j] - base) for j, s in line.anchored if 0 < j <= last and s < 1.0]
        anchors += [(s, km - base) for s, km in _stop_anchors(line, source) if s < 1.0]
        line.up_km = line.calibrate(anchors)


class Model:
    """La geometria en memoria: el marco, las lineas por id y en su orden, y las paradas de la fuente."""

    def __init__(self, frame: Frame):
        self.frame = frame
        self.lines: dict = {}
        self.order: list = []
        self.passing: list = []     # [{id, line, s, fact, lat, lon}]
        self.geo: Geometry | None = None

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
            if line.up_km[0] < dkm <= line.up_km[0] + slack_km:
                dkm = line.up_km[0]
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


def build_model(source: dict, geo: "Geometry | None" = None) -> tuple:
    """Lineas del Mapa en el marco cuantizado, con sus bocas. Sin geo, los puntos de rio-amazonas.json (el respaldo,
    src = rio_amazonas); con geo (M5.1), los de la geometria de produccion o los de lo commiteado, y los puntos de la
    investigacion quedan como puntos de control (sus s y sus km). Devuelve (Model, errores)."""
    errors: list = []
    frame = Frame.from_source(source)
    model = Model(frame)
    model.geo = geo
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
        ctrl = []
        for j, p in enumerate(pts):
            try:
                lat, lon = float(p["lat"]), float(p["lon"])
            except (TypeError, KeyError, ValueError):
                errors.append(f"rio {rid}: punto {j} sin lat/lon")
                continue
            if not frame.inside(lat, lon):
                errors.append(f"rio {rid}: punto {j} ({p.get('nombre')!r}) fuera de la caja del marco")
            q.append(frame.quant(lat, lon))
            ctrl.append((lat, lon))
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
        if geo is None:
            line = Line(rid, kind, frame, q, km)
            line.ctrl_s = [_round_s(line.s_of(j, 0.0)) if j < len(line.seg) else 1.0 for j in range(len(q))]
        else:
            entry = geo.lines.get(rid)
            if entry is None:
                errors.append(f"linea {rid}: falta en la geometria {geo.kind}")
                continue
            src, gq = entry
            if not gq:
                errors.append(f"linea {rid}: sin puntos en la geometria {geo.kind}")
                continue
            line = Line(rid, kind, frame, list(gq), None)
            line.src = src
            _calibrate(line, ctrl, km, source)
        line.source = r
        model.lines[rid] = line
        model.order.append(rid)
    if geo is not None:
        for rid in sorted(set(geo.lines) - set(model.lines)):
            errors.append(f"linea {rid}: esta en la geometria {geo.kind} y no en la investigacion")
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
    chains_ok = True
    for rid in model.order:
        try:
            model.chain(rid)
        except GeometryError as e:
            errors.append(str(e))
            chains_ok = False
    if geo is not None and chains_ok:
        _calibrate_up(model, source)
    # Puntos de paso: en la linea mas cercana (empate, a menos de TIE_KM: la primera en el orden de lineas, el cauce antes;
    # asi Gurupa, en el arranque del ramal, y las bocas de los afluentes quedan en el cauce; con la geometria de produccion,
    # ademas, la regla de la union, abajo).
    # Los de 1-200 y, desde M4.5a, los de los viajes de entrada de 201-1.000 que llevan id (teotonio, en el del 300): su id
    # es el de la investigacion, no el de su nombre.
    points = [("puntos_de_paso_1_200", p) for p in source.get("puntos_de_paso_1_200", []) or []]
    points += [("puntos_de_paso_del_viaje", p) for p in travel_passing_points(source)]
    errors.extend(travel_passing_mismatches(source))   # M4.5b: teotonio en los viajes del 300 y del 560, uno solo
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
        # M5.1: un punto en la union de una linea con su anfitrion (la boca de un afluente, el arranque del ramal) va en el
        # anfitrion, por el que pasa la canoa (con la geometria de produccion, el tramo final del afluente puede quedar
        # mas cerca que el cauce: la boca del Madeira, Gurupa).
        line = model.lines[rid]
        if line.host is not None and line.own_s is not None:
            joint = line.ll[-1] if line.own_s == 1.0 else line.ll[0]
            hi, ht, hd = model.lines[line.host].project(lat, lon)
            if haversine((lat, lon), joint) <= TOLERANCE_KM and hd <= TOLERANCE_KM:
                d, rid, i, t = hd, line.host, hi, ht
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
            s = line.ctrl_s[j]   # la s de su punto de control (con el respaldo, la del propio punto)
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
        # Orilla del afluente mirando rio abajo (s creciente): izquierda si su punto a SPLIT_SIDE_KM de la boca queda a la
        # izquierda del cauce entre SPLIT_SIDE_KM rio arriba y rio abajo de la confluencia. Con distancias y no con los
        # vertices vecinos: simplificado (revision de M5.1), el penultimo punto del Negro caia al otro lado del ultimo
        # segmento del cauce.
        up = model.s_at_km(MAIN_ID, km0 + SPLIT_SIDE_KM)
        down = model.s_at_km(MAIN_ID, km0 - SPLIT_SIDE_KM)
        a = model.frame.screen(*_quant_float(model.frame, *main.point_at_s(s0 if up is None else up)))
        b = model.frame.screen(*_quant_float(model.frame, *main.point_at_s(s1 if down is None else down)))
        c = model.frame.screen(*_quant_float(model.frame, *line.point(*line.locate_arc_from_end(SPLIT_SIDE_KM))))
        m = model.frame.screen(*_quant_float(model.frame, *main.point_at_s(s0)))
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


def land_problems(verts: list, tris: list, zones: list) -> list:
    """Por que verts, tris y zones no son una tierra bien triangulada (vacio si lo son). Sin suponer nada del orden de
    verts (M5.1: el continente y las islas, cada anillo seguido): todos los triangulos en sentido positivo y no
    degenerados; cada arista dirigida, en un solo triangulo; las aristas sin pareja (la costa) forman anillos cerrados,
    simples, en sentido positivo y que no se cortan entre si; los triangulos suman el area de esos anillos; y las zonas
    cubren todos los triangulos, seguidas desde el primero."""
    out: list = []
    if len(tris) % 3 or any(i >= len(verts) for i in tris):
        return ["tris no son triples de indices de verts"]
    edges: dict = {}
    area = 0
    bad = 0
    for k in range(0, len(tris), 3):
        a, b, c = (verts[i] for i in tris[k:k + 3])
        cr = _cross(a, b, c)
        if cr <= 0:
            bad += 1
            if bad <= 3:
                out.append(f"el triangulo {k // 3} es degenerado o va en sentido contrario")
        area += cr
        for e in ((a, b), (b, c), (c, a)):
            edges[e] = edges.get(e, 0) + 1
    repeated = sum(1 for n in edges.values() if n > 1)
    if repeated:
        out.append(f"{repeated} aristas dirigidas en mas de un triangulo (triangulos solapados)")
    nxt: dict = {}
    for (a, b) in edges:
        if (b, a) not in edges:
            if a in nxt:
                out.append(f"la costa pasa dos veces por el vertice {a}")
            nxt[a] = b
    rings = []
    seen = set()
    for a0 in sorted(nxt):
        if a0 in seen:
            continue
        ring = []
        a = a0
        while a not in seen:
            seen.add(a)
            ring.append(a)
            a = nxt.get(a)
            if a is None:
                out.append("la costa no cierra (una arista sin la siguiente)")
                break
        if a is not None and a != a0:
            out.append("la costa no cierra en un anillo simple")
        rings.append(ring)
    if out:
        return out
    for ring in rings:
        if _area2(ring) <= 0:
            out.append(f"un anillo de la costa ({len(ring)} vertices) va en sentido contrario o tiene area nula")
    cuts = crossings(rings)
    if cuts:
        out.append(f"la costa se corta a si misma en {len(cuts)} pares de aristas")
    ring_area = sum(_area2(r) for r in rings)
    if area != ring_area:
        out.append(f"los triangulos suman {area} y los anillos de la costa {ring_area} (area x2)")
    first = 0
    for z in zones:
        if z.get("first") != first:
            out.append(f"la zona {z.get('id')} empieza en el triangulo {z.get('first')} y no en el {first}")
        first += z.get("count", 0)
    if first != len(tris) // 3:
        out.append("las zonas no cubren todos los triangulos")
    return out


def _in_triangle(p, a, b, c) -> bool:
    """p dentro del triangulo (a, b, c) en sentido positivo, bordes incluidos."""
    return _cross(a, b, p) >= 0 and _cross(b, c, p) >= 0 and _cross(c, a, p) >= 0


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


def build_map(source: dict, geo: Geometry | None = None) -> tuple:
    """Documento af-river-map/1 y su modelo: sin geo, con el respaldo de la investigacion; con geo (M5.1), con sus lineas
    y su tierra. Devuelve (doc, model, errores)."""
    model, errors = build_model(source, geo)
    frame = model.frame
    lines = []
    for rid in model.order:
        line = model.lines[rid]
        entry = {"id": rid, "kind": line.kind, "class": CLASS[line.kind], "src": line.src, "pts": encode_points(line.q),
                 "water": _water_intervals(line)}
        if rid == MAIN_ID:
            splits, errs = _splits(model)
            errors += errs
            entry["splits"] = splits
        if line.kind != "main" and line.host is not None:
            entry["mouth"] = {"on": line.host, "s": line.host_s}
            entry["fill_on_pass"] = _fill_on_pass(line)
        lines.append(entry)
    if geo is not None:
        verts, tris, zones = geo.land
        land = {"verts": encode_points(verts), "tris": encode_indices(tris), "zones": [dict(z) for z in zones]}
    else:
        ring = land_ring(source, frame)
        problems = polygon_problems(ring)
        if problems:
            errors.append("tierra: el anillo de costas no es un poligono simple (" + "; ".join(problems) + ")")
            tris = []
        else:
            tris = triangulate(ring)
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
    mouth_s = confluence_s(model, record, rid, s)
    return {"line": rid, "s": _round_s(s if mouth_s is None else mouth_s)}, []


# [P] Paradas en una confluencia (revision de M5.1): una parada cuyo sitio (o atraque) queda a <= CONFLUENCE_KM del punto
# donde la investigacion pone la boca de un afluente de su linea (el ultimo punto de control del afluente) es una parada en
# esa confluencia (Santarem y el Tapajos, el Encuentro y el Negro, Nauta y el Maranon, Boca Manu y el Manu, Riberalta y el
# Madre de Dios...). Se ancla en la s de esa boca sobre su linea (si su punto por km queda a <= TOLERANCE_KM de ella): al
# llegar, la boca se cruza y su afluente se llena, como con el respaldo, y no quedan tramos de metros entre la parada y la
# boca. Con HydroRIVERS, el punto por km de Santarem quedaba ~3 km rio abajo de la boca del Tapajos y el de Boca Manu ~2 km
# rio abajo de la del Manu: la canoa, que sube, se paraba antes de la boca. 5 km: las coordenadas de las ciudades son su
# centro, a unos km de la boca (Santarem, 3,8 km); menos de 1 dp en la Cuenca.
CONFLUENCE_KM = 5.0


def confluence_s(model: Model, record: dict, rid: str, s: float) -> float | None:
    """s de la boca en la que se ancla una parada en una confluencia (CONFLUENCE_KM), o None. Con dos bocas, la del afluente
    mas cercano a su sitio (a igualdad, el primero del orden de la investigacion)."""
    place, _, problem = site_point(record)
    if problem:
        return None
    host = model.lines[rid]
    here = host.point_at_s(s)
    best = None
    for k, cid in enumerate(model.order):
        child = model.lines[cid]
        points = child.source.get("puntos") or []
        if child.host != rid or child.kind == "main" or child.host_s is None or not points:
            continue
        try:
            mouth = (float(points[-1]["lat"]), float(points[-1]["lon"]))
        except (TypeError, KeyError, ValueError):
            continue
        d = haversine(place, mouth)
        if d > CONFLUENCE_KM or haversine(here, host.point_at_s(child.host_s)) > TOLERANCE_KM:
            continue
        if best is None or (d, k) < best[0]:
            best = ((d, k), child.host_s)
    return None if best is None else best[1]


def route(model: Model, a: dict, b: dict) -> list:
    """Tramos por agua de a {line, s} a b {line, s}: subiendo por los anfitriones hasta el rio comun y bajando hasta b
    (ida y vuelta por las confluencias). Cada tramo, {line, s0, s1}; sin tramos de longitud cero salvo si a = b o para
    acabar en la linea de b (una parada en la boca del afluente por el que se llega: Riberalta)."""
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
    if not kept or kept[-1][0] != b["line"]:
        # Sin tramos (a = b) o, con las paradas en una confluencia (confluence_s), un viaje que baja un afluente hasta su
        # boca, donde esta la parada (Riberalta): el tramo de longitud cero en la linea de la parada, para que el viaje
        # siempre acabe en ella (el cliente lo espera).
        kept.append([b["line"], b["s"], b["s"]])
    return [{"line": p[0], "s0": p[1], "s1": p[2]} for p in kept]


# Dos s a menos de esto son el mismo punto al cruzar bocas y puntos de paso: las s van redondeadas a 6 decimales (en el
# cauce, 1e-6 de s son ~6 m; antes de confluence_s, la parada del Encuentro quedaba a 6 m de la boca del Negro).
S_EPS = 2e-6


def _on_piece(s: float, piece: dict, first: bool) -> bool:
    lo, hi = sorted((piece["s0"], piece["s1"]))
    if not lo - S_EPS <= s <= hi + S_EPS:
        return False
    return not (first and abs(s - piece["s0"]) <= S_EPS)   # el punto de partida ya se conto en el tramo anterior


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


def control_check(line: Line, j: int, point: dict, zone: "MundoZone | None") -> tuple:
    """(distancia en km, vista, maximo permitido, excepcion pendiente?) del punto de control j de una linea. Vista Mundo
    si la linea no es de la investigacion y el punto queda en la zona de las paradas (MundoZone); si no, Cuenca. El maximo
    es la tolerancia de la vista o, si la hay, la excepcion medida de PENDING_CONTROL_KM."""
    lat, lon = float(point["lat"]), float(point["lon"])
    d = line.distance_km(lat, lon)
    mundo = zone is not None and line.src != SRC and zone.contains(lat, lon)
    view = "Mundo" if mundo else "Cuenca"
    canon = MUNDO_TOLERANCE_KM if mundo else TOLERANCE_KM
    pending = PENDING_CONTROL_KM.get(line.id, {}).get(j) if line.src != SRC else None
    return d, view, max(canon, pending) if pending is not None else canon, pending is not None


def validate_map(doc: dict, model: Model, source: dict) -> list:
    """Reglas del documento que el esquema no expresa (geometry.py --check)."""
    errors: list = []
    frame = model.frame
    zone = MundoZone(source) if model.geo is not None else None
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
        # Cada punto de control de la investigacion, a <= 2 dp de su linea en su vista: la Cuenca (TOLERANCE_KM) con el
        # respaldo y lejos de las paradas; la Mundo (MUNDO_TOLERANCE_KM) en su zona, con la geometria de produccion (M5.1).
        for j, p in enumerate(line.source["puntos"]):
            d, view, allowed, pending = control_check(line, j, p, zone)
            if d > allowed:
                errors.append(f"{tag}: el punto de control {j} ({p.get('nombre')!r}) queda a {d:.1f} km de la linea "
                              f"(vista {view}: maximo {allowed:g} km{_limit_note(pending, view)})")
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
    errors += ["land: " + e for e in land_problems(verts, tris, land.get("zones", []))]
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
    """Dos posiciones (linea, s) son el mismo punto del agua: iguales, la boca (o el arranque) de una linea y su s sobre
    el anfitrion, o (M4.5b) las bocas de dos lineas que entran en el mismo anfitrion en la misma s (el Beni y el Mamore,
    que forman el Madeira en Villa Bella: el leg del 500 baja uno y sube el otro sin tramo de longitud cero en el
    Madeira). Es lo unico que une dos tramos de un leg: cualquier otro salto cruza tierra."""
    if a == b:
        return True
    for x, y in ((a, b), (b, a)):
        line = model.lines.get(x[0])
        if line is not None and line.host == y[0] and line.own_s == x[1] and line.host_s == y[1]:
            return True
    la, lb = model.lines.get(a[0]), model.lines.get(b[0])
    return (la is not None and lb is not None and la.host is not None and la.host == lb.host
            and la.own_s == a[1] and lb.own_s == b[1] and la.host_s == lb.host_s)


def _limit_note(pending: bool, view: str = "Cuenca") -> str:
    canon = MUNDO_TOLERANCE_KM if view == "Mundo" else TOLERANCE_KM
    if pending:
        return f": excepcion medida pendiente del duenno; el canon es {canon:g} km"
    return f", <= 2 dp en la vista {view}"


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
        if d > TOLERANCE_KM:
            warnings.append(f"linea {rid}: su boca queda a {d:.1f} km de {line.host} (canon {TOLERANCE_KM:g} km; "
                            f"excepcion {limit:g} km pendiente del duenno)")
        else:
            warnings.append(f"linea {rid}: su boca queda a {d:.1f} km de {line.host}; sobra su excepcion de "
                            f"PENDING_MOUTH_KM ({limit:g} km)")
    return warnings


def validate_routes(stops_doc: dict, model: Model, records: list) -> list:
    """Rutas de river_stops_v1.json (at y leg) contra el Mapa: paradas en su rio, km, continuidad y que no retroceden."""
    errors: list = []
    by_k = {r.get("k"): r for r in records if isinstance(r, dict)}
    prev = None
    last_on_line: dict = {}   # (mundo, linea) -> la ultima parada del mundo en esa linea (M4.5b)
    prev_home = None          # (mundo, km_home, linea) de la parada anterior
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
        # M4.5b (propuesta del Mapa §5.4 quater): dentro de un mundo, ninguna parada queda rio abajo de la ultima del mismo
        # mundo en su misma linea, aunque entre las dos la canoa haya ido por otro rio (Trinidad, en el Mamore, por encima
        # de Guayaramerin).
        world = s.get("world")
        here_line = last_on_line.get((world, at["line"]))
        if here_line is not None and (prev is None or here_line["n"] != prev["n"]):
            if at["s"] > here_line["s"] + 1e-9:
                errors.append(f"{tag}: queda rio abajo por {at['line']} de la parada {here_line['n']}, del mismo mundo "
                              "(dentro de un mundo los km no retroceden)")
            if km_value is not None and here_line["km"] is not None and km_value < here_line["km"]:
                errors.append(f"{tag}: km_from_sea {km_value} menor que el de la parada {here_line['n']} "
                              f"({here_line['km']}), del mismo mundo y en el mismo rio")
        last_on_line[(world, at["line"])] = {"n": n, "s": at["s"], "km": km_value}
        # Los km a casa solo bajan dentro de un mundo en una parada con baja_por_confluencia [P] (la canoa baja un rio y
        # sube otro por una confluencia: Guayaramerin y Trinidad, pregunta abierta del mundo 8) y en otra linea que la
        # parada anterior; un baja_por_confluencia en una parada cuyos km no bajan sobra.
        km_home = s.get("km_home")
        declared = rec.get("baja_por_confluencia")
        declared = isinstance(declared, str) and bool(declared.strip())
        drops = (prev_home is not None and prev_home[0] == world and isinstance(km_home, int)
                 and isinstance(prev_home[1], int) and km_home < prev_home[1])
        if drops and not declared:
            errors.append(f"{tag}: km_home {km_home} baja dentro del mundo {world} (la parada anterior, {prev_home[1]}) "
                          "sin baja_por_confluencia en la investigacion")
        elif drops and prev_home[2] == at["line"]:
            errors.append(f"{tag}: km_home baja con baja_por_confluencia, pero en la misma linea ({at['line']}) que la "
                          "parada anterior: no hay confluencia")
        elif declared and not drops:
            errors.append(f"{tag}: baja_por_confluencia en una parada cuyos km a casa no bajan dentro de su mundo: sobra")
        prev_home = (world, km_home, at["line"])
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


# --------------------------------------------------------------------------------------------- produccion (M5.1)
#
# Geometria de produccion (D-013, punto 5, Q11-A; 08 §4.5; propuesta del Mapa §3.6 y §11.3): las descargas del duenno en
# tools/river-map/source/ (sin versionar; sus SHA-256 y licencias, en sources.json). Lineas de HydroRIVERS v1.0 por la red
# (cada tramo sabe cual es el siguiente rio abajo: las bocas caen exactamente en un vertice del anfitrion) y, donde
# HydroRIVERS no llega, Natural Earth 1:10m o la investigacion; tierra de Natural Earth 1:10m (land), recortada a la caja.
# Seleccion, recorte, giro (el marco cw90) y Douglas-Peucker por vista: DP_MUNDO_KM cerca de las paradas de la 1.0 (la
# vista Mundo) y DP_CUENCA_KM en el resto (la vista Cuenca).

SOURCE_DIR = Path(__file__).resolve().parent / "source"
SOURCES_MANIFEST = Path(__file__).resolve().parent / "sources.json"
HR_LAYER = "HydroRIVERS_v10_sa"
NE_RIVERS_LAYER = "ne_10m_rivers_lake_centerlines"
NE_LAND_LAYER = "ne_10m_land"

# Origen de cada linea [P] (src): HydroRIVERS por defecto, en las dos vistas, la unica fuente con la red (bocas exactas) y a
# la escala de la vista Mundo. Es una desviacion [P] de D-013, punto 5, y de 08 §4.5, que piden Natural Earth en la vista
# Cuenca (docstring del modulo; pregunta 65 propuesta, parte 2). Excepciones: el ramal del Para (los furos de Breves y el
# estuario son agua ancha que ni HydroRIVERS ni Natural Earth dibujan como rio: se queda la poligonal de la investigacion)
# y el Casiquiare (una bifurcacion, que la red en arbol de HydroRIVERS no representa: su linea de Natural Earth, con la
# boca llevada al vertice mas cercano del Negro); el Itaya y el Pacaya, sus esbozos de la investigacion (README).
LINE_SRC = {"para": "rio_amazonas", "casiquiare": "ne10m", "itaya": "rio_amazonas", "pacaya": "rio_amazonas"}
# Por que las de la investigacion se quedan en su esbozo (pendiente del duenno o del investigador; revision de M5.1): se
# dibujan en la vista Mundo (el ramal lleva la casa de TiTino; el Itaya y el Pacaya, 08 §4.5, «solo en la vista Mundo»),
# pero sin meandros, y --check no puede medirlas con la tolerancia de esa vista: sus puntos de control son sus propios puntos.
# --check avisa de cada una (pending_sketches).
SKETCH_WHY = {
    "para": "los furos de Breves y el estuario son agua ancha: HydroRIVERS no los dibuja como rio y Natural Earth "
            "(rivers_lake_centerlines) no tiene ningun rio Para (en el estuario solo el final del Tocantins)",
    "itaya": "en HydroRIVERS el rio que nace en su cabecera entra en el cauce ~45 km antes de Iquitos, no en su boca",
    "pacaya": "en HydroRIVERS el rio que nace en su cabecera va a otro rio, no a su boca (canal de Puinahua)",
}
NE_NAMES = {"casiquiare": "Casiquiare"}

HEAD_SNAP_KM = 15.0     # cabecera de la investigacion -> tramos de HydroRIVERS candidatos (<= 2 dp de la vista Cuenca)
HEAD_CUT_KM = 30.0      # la linea empieza en el vertice mas cercano a la cabecera entre los primeros 30 km del camino
MOUTH_SNAP_KM = 15.0    # boca de un rio «solo_boca» -> tramos que entran en su anfitrion
# [P] Douglas-Peucker por vista (propuesta §11.3, punto 2: «a unos 2 km en la Cuenca y a unos 0,3 km en la vista Mundo»).
# Revision de M5.1 [P]: 0,3 y 2 km dejaban 1.213 puntos en el cauce (la propuesta §11.3 estimaba 300-800), segmentos mas
# cortos que el ancho del trazo en la Cuenca (2 km son ~0,24 dp: los rios se veian deshilachados, D-003) y la franja del
# viaje del 81 en 22 ms frente a sus 8 ms (propuesta §11.4). Ahora: en MundoZone, la misma tolerancia que los puntos de
# control de la vista Mundo (MUNDO_TOLERANCE_KM, 2 dp a 15x): ahi la simplificacion no aparta la linea mas de lo que admite
# esa vista; en el resto, 6 km (~0,7 dp en la Cuenca, muy por debajo de sus 15 km), que la vista Mundo tambien ensenna fuera
# de MundoZone (abajo). La franja del 81 tarda 7,8-8,3 ms y no baja con menos vertices (con 2 km en la vista Mundo,
# 7,75 ms): el resto es del cliente (README, notas de M5.1).
DP_MUNDO_KM = 1.0
DP_CUENCA_KM = 6.0
# [P] Zona de la vista Mundo: a <= MUNDO_RADIUS_KM de una parada de la 1.0 (su sitio o su atraque; las de rio-amazonas.json).
# Ahi la simplificacion es la de la vista Mundo y los puntos de control se validan con MUNDO_TOLERANCE_KM. No cubre todo lo
# que ensenna la vista Mundo: el encuadre (RiverMapScreen.WorldZoom: la caja de las paradas del mundo al 60 % de la zona,
# de 4x a 15x) abarca a 4x ~6,9 grados (~760 km) de ancho en 360 dp, y se arrastra por todo el mapa. Fuera de la zona, los
# 6 km de DP_CUENCA_KM son ~2,8 dp a 4x y ~10,6 dp a 15x (13,1 dp por grado * zoom / 111 km), por encima de los 2 dp de la
# vista (08 §4.5; D-023, punto 20). Con 1 km en todo el mapa el archivo mediria 54.114 bytes y 5.051 puntos (frente a
# 26.386 y 2.148; 200 km: 33.025 y 3.026; 400 km: 45.806 y 4.469; medido con las descargas), sin medir aun la franja del
# 81 (con 0,3 y 2 km, 53 KB y 22 ms). Pendiente del duenno: pregunta 65 propuesta, parte 3 (README, notas de M5.1).
MUNDO_RADIUS_KM = 100.0
# Tolerancia de la vista Mundo (propuesta §3.6: <= 2 dp a 15x son ~1 km).
MUNDO_TOLERANCE_KM = 1.0
# [P] Islas de Natural Earth que entran en la tierra: las de al menos 500 km2 (Marajo, Grande de Gurupa, Caviana,
# Mexiana...); las menores no llegan a 3 dp2 en la vista Cuenca.
MIN_ISLAND_KM2 = 500.0
LAND_REPAIR_ROUNDS = 12
# Tope del archivo (M5.1, criterio de aceptacion; 08 §4.5: «unos 70-90 KB»).
MAX_BYTES = 90 * 1024


class SourcesState:
    """Estado de las descargas frente a sources.json: complete (todas las que usa la geometria, con su SHA-256),
    absent (ninguna: se comprueba lo commiteado) o un problema (incompletas o con otro SHA-256: FAIL)."""

    def __init__(self, status: str, problems: list, files: dict):
        self.status = status       # "complete", "absent" o "bad"
        self.problems = problems
        self.files = files         # nombre -> ruta, de las presentes


def load_manifest(path: Path = SOURCES_MANIFEST) -> dict:
    try:
        manifest = json.loads(Path(path).read_bytes().decode("utf-8"))
    except (OSError, UnicodeDecodeError, json.JSONDecodeError) as e:
        raise GeometryError(f"no se puede leer {path.name} ({e})") from e
    files = manifest.get("files") if isinstance(manifest, dict) else None
    if not isinstance(files, list) or not files:
        raise GeometryError(f"{path.name}: sin la lista files")
    for f in files:
        if not (isinstance(f, dict) and isinstance(f.get("name"), str) and re.fullmatch(r"[0-9a-f]{64}", str(f.get("sha256")))
                and isinstance(f.get("bytes"), int) and isinstance(f.get("used"), bool)):
            raise GeometryError(f"{path.name}: cada archivo lleva name, sha256 (64 hex), bytes y used")
    return manifest


def sha256_of(path: Path) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def sources_state(src_dir: Path, manifest: dict) -> SourcesState:
    """Comprueba las descargas: tamanno y SHA-256 de cada archivo de sources.json que este presente."""
    present, problems = {}, []
    used = [f for f in manifest["files"] if f["used"]]
    for f in manifest["files"]:
        path = Path(src_dir) / f["name"]
        if not path.is_file():
            continue
        present[f["name"]] = path
        size = path.stat().st_size
        if size != f["bytes"]:
            problems.append(f"{f['name']}: {size} bytes y sources.json dice {f['bytes']}")
        elif sha256_of(path) != f["sha256"]:
            problems.append(f"{f['name']}: su SHA-256 no es el de sources.json (otra version o archivo danado)")
    if not present:
        return SourcesState("absent", [], present)
    missing = [f["name"] for f in used if f["name"] not in present]
    if missing:
        problems.append("faltan " + ", ".join(missing) + " (o estan todas o ninguna)")
    return SourcesState("bad" if problems else "complete", problems, present)


def _km_xy(lat: float, lon: float, lat_ref: float) -> tuple:
    """Plano local en km (equirrectangular en lat_ref): para distancias cortas y la simplificacion."""
    return lon * 111.32 * math.cos(math.radians(lat_ref)), lat * 110.574


def _dist_point_polyline_km(lat: float, lon: float, pts: list) -> tuple:
    """(distancia en km, indice del segmento) de (lat, lon) a una poligonal de (lat, lon) en el plano local del punto."""
    px, py = _km_xy(lat, lon, lat)
    best = (math.inf, 0)
    if len(pts) == 1:
        x, y = _km_xy(pts[0][0], pts[0][1], lat)
        return math.hypot(x - px, y - py), 0
    prev = _km_xy(pts[0][0], pts[0][1], lat)
    for i in range(1, len(pts)):
        cur = _km_xy(pts[i][0], pts[i][1], lat)
        dx, dy = cur[0] - prev[0], cur[1] - prev[1]
        l2 = dx * dx + dy * dy
        t = 0.0 if l2 == 0 else max(0.0, min(1.0, ((px - prev[0]) * dx + (py - prev[1]) * dy) / l2))
        d = math.hypot(prev[0] + t * dx - px, prev[1] + t * dy - py)
        if d < best[0]:
            best = (d, i - 1)
        prev = cur
    return best


class MundoZone:
    """Zona de la vista Mundo [P]: a <= MUNDO_RADIUS_KM de una parada de la 1.0 (las de la investigacion, su atraque si
    lo tiene). Rejilla de 1 grado para no medir contra todas."""

    def __init__(self, source: dict, radius_km: float = MUNDO_RADIUS_KM):
        self.radius = radius_km
        self.cells: dict = {}
        for r in stop_records(source):
            place, _, problem = site_point(r)
            if problem or place is None:
                continue
            self.cells.setdefault((math.floor(place[0]), math.floor(place[1])), []).append(place)

    def contains(self, lat: float, lon: float) -> bool:
        reach = 1 + int(self.radius // 100)
        ci, cj = math.floor(lat), math.floor(lon)
        for a in range(ci - reach, ci + reach + 1):
            for b in range(cj - reach - 1, cj + reach + 2):
                for p in self.cells.get((a, b), ()):
                    if haversine((lat, lon), p) <= self.radius:
                        return True
        return False


class HydroRivers:
    """HydroRIVERS v1.0 (tramos con HYRIV_ID, NEXT_DOWN y UPLAND_SKM): la red rio abajo, una rejilla de cajas para buscar
    tramos cerca de un punto y los puntos de cada tramo (leidos al pedirlos)."""

    CELL = 0.1   # grados

    def __init__(self, base: Path, frame: Frame):
        self.layer = Layer(base)
        table = self.layer.table
        try:
            ids = table.column("HYRIV_ID")
            nxt = table.column("NEXT_DOWN")
            self.up = table.column("UPLAND_SKM")
        except ShapefileError as e:
            self.layer.close()
            raise GeometryError(str(e)) from e
        index = {rid: i for i, rid in enumerate(ids) if rid is not None}
        self.ids = ids
        self.down = [index.get(n, -1) if n else -1 for n in nxt]
        self.grid: dict = {}
        self.bbox: dict = {}
        lat_lo, lat_hi = frame.lat_min - 1, frame.lat_max + 1
        lon_lo, lon_hi = frame.lon_min - 1, frame.lon_max + 1
        cell = self.CELL
        for i in range(self.layer.count):
            bb = self.layer.shapes.record_bbox(i)
            if bb is None or bb[2] < lon_lo or bb[0] > lon_hi or bb[3] < lat_lo or bb[1] > lat_hi:
                continue
            self.bbox[i] = bb
            for a in range(math.floor(bb[1] / cell), math.floor(bb[3] / cell) + 1):
                for b in range(math.floor(bb[0] / cell), math.floor(bb[2] / cell) + 1):
                    self.grid.setdefault((a, b), []).append(i)
        self._pts: dict = {}
        self._children: dict | None = None

    def close(self) -> None:
        self.layer.close()

    def points(self, i: int) -> list:
        """Puntos (lat, lon) del tramo i, de su cabecera a su final (el sentido del agua)."""
        pts = self._pts.get(i)
        if pts is None:
            pts = [(y, x) for part in self.layer.shapes.parts(i) for (x, y) in part]
            self._pts[i] = pts
        return pts

    def near(self, lat: float, lon: float, radius_km: float) -> dict:
        """{tramo: distancia en km} de los tramos a <= radius_km de (lat, lon)."""
        dlat = radius_km / 110.574
        dlon = radius_km / (111.32 * max(0.05, math.cos(math.radians(lat))))
        cell = self.CELL
        seen = set()
        out = {}
        for a in range(math.floor((lat - dlat) / cell), math.floor((lat + dlat) / cell) + 1):
            for b in range(math.floor((lon - dlon) / cell), math.floor((lon + dlon) / cell) + 1):
                for i in self.grid.get((a, b), ()):
                    if i in seen:
                        continue
                    seen.add(i)
                    bb = self.bbox[i]
                    if bb[0] - dlon > lon or bb[2] + dlon < lon or bb[1] - dlat > lat or bb[3] + dlat < lat:
                        continue
                    d, _ = _dist_point_polyline_km(lat, lon, self.points(i))
                    if d <= radius_km:
                        out[i] = d
        return out

    def downstream(self, i: int, stop: set | None) -> tuple:
        """Tramos de i rio abajo hasta antes del primero de stop, o hasta el mar. Devuelve (tramos, tramo de stop o -1)."""
        path = []
        guard = 0
        while i >= 0:
            if stop is not None and i in stop:
                return path, i
            path.append(i)
            i = self.down[i]
            guard += 1
            if guard > len(self.down):
                raise GeometryError("HydroRIVERS: ciclo en NEXT_DOWN")
        return path, -1

    def children(self, i: int) -> list:
        if self._children is None:
            self._children = {}
            for k, d in enumerate(self.down):
                if d >= 0:
                    self._children.setdefault(d, []).append(k)
        return self._children.get(i, [])

    def polyline(self, path: list) -> tuple:
        """Puntos de los tramos seguidos (sin repetir el vertice comun) y, por tramo, (tramo, indice de su primer punto,
        indice de su ultimo punto) en esa lista."""
        out: list = []
        spans = []
        for i in path:
            first = None
            for p in self.points(i):
                if not out or out[-1] != p:
                    out.append(p)
                if first is None:
                    first = len(out) - 1
            spans.append((i, first, len(out) - 1))
        return out, spans


def _nearest_vertex(pts: list, lat: float, lon: float, lo: int = 0, hi: int | None = None) -> tuple:
    """(indice, distancia en km) del vertice de pts[lo:hi] mas cercano a (lat, lon)."""
    best = (math.inf, lo)
    for i in range(lo, len(pts) if hi is None else hi):
        d = haversine((lat, lon), pts[i])
        if d < best[0]:
            best = (d, i)
    return best[1], best[0]


def _trace_hr(hr: HydroRivers, rid: str, record: dict, ctrl: list, host_reaches: set | None) -> tuple:
    """Linea de HydroRIVERS de un rio de la investigacion: de su cabecera rio abajo hasta su anfitrion (la boca, un vertice
    del anfitrion) o hasta el mar. La cabecera elige, entre los tramos a <= HEAD_SNAP_KM, el camino que pasa a <=
    TOLERANCE_KM de mas puntos de control (y, a igualdad, mas cerca de ellos, de la cabecera y con mas cuenca). Un rio
    «solo_boca» sube desde su boca por el tramo de mas cuenca. Devuelve (puntos (lat, lon), tramos con sus indices
    [(tramo, primero, ultimo)] en esos puntos, llego al anfitrion?)."""
    if record.get("solo_boca") or len(ctrl) == 1:
        if host_reaches is None:
            raise GeometryError(f"linea {rid}: solo_boca sin anfitrion de HydroRIVERS")
        lat, lon = ctrl[-1]
        cands = [(d, i) for i, d in hr.near(lat, lon, MOUTH_SNAP_KM).items()
                 if i not in host_reaches and hr.down[i] in host_reaches]
        if not cands:
            raise GeometryError(f"linea {rid}: ningun tramo de HydroRIVERS entra en su anfitrion a <= {MOUTH_SNAP_KM:g} km "
                                "de su boca")
        _, mouth = max(cands, key=lambda c: (hr.up[c[1]], -c[0], -c[1]))
        up_path = [mouth]
        while True:
            kids = hr.children(up_path[-1])
            if not kids:
                break
            up_path.append(max(kids, key=lambda k: (hr.up[k], -k)))
        pts, spans = hr.polyline(list(reversed(up_path)))
        return pts, spans, True
    head = ctrl[0]
    near_sets = [hr.near(lat, lon, TOLERANCE_KM) for lat, lon in ctrl]   # la cabecera y la boca tambien cuentan
    starts = dict(hr.near(head[0], head[1], HEAD_SNAP_KM))   # tramo -> distancia a la cabecera
    best = None
    for c, dc in sorted(starts.items(), key=lambda kv: (kv[1], kv[0])):
        if host_reaches is not None and c in host_reaches:
            continue
        path, hit = hr.downstream(c, host_reaches)
        if host_reaches is not None and hit < 0:
            continue
        pset = set(path)
        covered, dsum = 0, 0.0
        for ns in near_sets:
            m = min((d for k, d in ns.items() if k in pset), default=None)
            if m is None:
                dsum += TOLERANCE_KM
            else:
                covered += 1
                dsum += m
        key = (covered, -round(dsum, 3), -round(dc, 3), hr.up[c] or 0.0, -c)
        if best is None or key > best[0]:
            best = (key, path, hit)
    if best is None:
        raise GeometryError(f"linea {rid}: ningun tramo de HydroRIVERS a <= {HEAD_SNAP_KM:g} km de su cabecera llega a su "
                            "anfitrion")
    _, path, hit = best
    pts, spans = hr.polyline(path)
    # La linea empieza en el vertice del camino mas cercano a la cabecera (entre sus primeros HEAD_CUT_KM).
    run, hi = 0.0, len(pts)
    for i in range(1, len(pts)):
        run += haversine(pts[i - 1], pts[i])
        if run > HEAD_CUT_KM:
            hi = i + 1
            break
    start, _ = _nearest_vertex(pts, head[0], head[1], 0, hi)
    # Un tramo es de la linea (un afluente que llega a el acaba en su primer punto) si queda entero tras el corte; si el
    # corte cae dentro de un tramo, el afluente que llega a su primer punto lo recorre hasta el siguiente (Villa Bella).
    spans = [(i, a - start, b - start) for i, a, b in spans if a >= start]
    return pts[start:], spans, hit >= 0


def _research_tail(pts: list, spans: list, ctrl: list) -> tuple:
    """Linea que acaba en el mar en HydroRIVERS (el cauce, el Tocantins): corta en el vertice mas cercano al ultimo punto de
    control a <= TOLERANCE_KM tras el cual todos quedan lejos, y sigue con los puntos de la investigacion (el estuario: agua
    ancha que HydroRIVERS no dibuja como rio). Sin cola, corta en el vertice mas cercano al ultimo punto de control.
    Devuelve (puntos, tramos que quedan, numero de puntos de la cola)."""
    dists = [_dist_point_polyline_km(lat, lon, pts)[0] for lat, lon in ctrl]
    g = len(ctrl) - 1
    while g >= 0 and dists[g] > TOLERANCE_KM:
        g -= 1
    if g < 0:
        raise GeometryError("ningun punto de control queda cerca de la linea de HydroRIVERS")
    cut, _ = _nearest_vertex(pts, ctrl[g][0], ctrl[g][1])
    tail = [tuple(p) for p in ctrl[g + 1:]]
    return pts[:cut + 1] + tail, [s for s in spans if s[1] < cut], len(tail)


def _ne_line(src_dir: Path, rid: str, ctrl: list) -> list:
    """Linea de Natural Earth (rivers_lake_centerlines) por su nombre (NE_NAMES), orientada de la cabecera de la
    investigacion a la boca; sus partes, encadenadas por sus extremos."""
    name = NE_NAMES.get(rid)
    if name is None:
        raise GeometryError(f"linea {rid}: sin nombre de Natural Earth en NE_NAMES")
    parts = []
    with Layer(Path(src_dir) / NE_RIVERS_LAYER) as layer:
        names = layer.table.column("name")
        for i, n in enumerate(names):
            if n == name:
                parts += [[(y, x) for x, y in part] for part in layer.shapes.parts(i) if part]
    if not parts:
        raise GeometryError(f"linea {rid}: Natural Earth no tiene ningun rio llamado {name!r}")
    head = ctrl[0]
    line = parts.pop(0)
    while parts:
        best = None
        for k, part in enumerate(parts):
            for rev_line in (False, True):
                for rev_part in (False, True):
                    a = line[0] if rev_line else line[-1]
                    b = part[-1] if rev_part else part[0]
                    d = haversine(a, b)
                    if best is None or d < best[0]:
                        best = (d, k, rev_line, rev_part)
        _, k, rev_line, rev_part = best
        part = parts.pop(k)
        if rev_line:
            line.reverse()
        line += list(reversed(part)) if rev_part else part
    if haversine(head, line[-1]) < haversine(head, line[0]):
        line.reverse()
    out = []
    for p in line:
        if not out or out[-1] != p:
            out.append(p)
    return out


def simplify(points: list, tol: list, keep: set) -> list:
    """Douglas-Peucker con tolerancia por vertice (km; el tramo usa la menor de sus vertices) y vertices obligatorios.
    Distancia al segmento en el plano local del tramo. Devuelve los indices que quedan, en orden."""
    n = len(points)
    if n <= 2:
        return list(range(n))
    kept = [False] * n
    kept[0] = kept[-1] = True
    for k in keep:
        if 0 <= k < n:
            kept[k] = True
    marks = [i for i in range(n) if kept[i]]
    stack = list(zip(marks, marks[1:]))
    while stack:
        a, b = stack.pop()
        if b - a < 2:
            continue
        limit = min(tol[a:b + 1])
        lat_ref = (points[a][0] + points[b][0]) / 2
        ax, ay = _km_xy(points[a][0], points[a][1], lat_ref)
        bx, by = _km_xy(points[b][0], points[b][1], lat_ref)
        dx, dy = bx - ax, by - ay
        l2 = dx * dx + dy * dy
        best_d, best_i = -1.0, -1
        for i in range(a + 1, b):
            px, py = _km_xy(points[i][0], points[i][1], lat_ref)
            t = 0.0 if l2 == 0 else max(0.0, min(1.0, ((px - ax) * dx + (py - ay) * dy) / l2))
            d = math.hypot(ax + t * dx - px, ay + t * dy - py)
            if d > best_d:
                best_d, best_i = d, i
        if best_d > limit:
            kept[best_i] = True
            stack.append((best_i, b))
            stack.append((a, best_i))
    return [i for i in range(n) if kept[i]]


def _quantize_line(frame: Frame, points: list, idx: list, rid: str, errors: list) -> list:
    q = []
    for i in idx:
        lat, lon = points[i]
        if not frame.inside(lat, lon):
            errors.append(f"linea {rid}: el punto ({lat:.4f}, {lon:.4f}) queda fuera de la caja del marco")
        p = frame.quant(lat, lon)
        if not q or q[-1] != p:
            q.append(p)
    return q


# ----------------------------------------------------------------------------- tierra de Natural Earth (M5.1)

def _ring_area(ring: list) -> float:
    """Area con signo (formula del lazo) de un anillo de (x, y)."""
    n = len(ring)
    return sum(ring[i][0] * ring[(i + 1) % n][1] - ring[(i + 1) % n][0] * ring[i][1] for i in range(n)) / 2


def clip_ring(ring: list, coord: int, value: float, sign: int) -> list:
    """Recorta un anillo simple en sentido positivo (x, y) por el semiplano sign * (p[coord] - value) >= 0. Devuelve los
    anillos que quedan dentro (pueden ser varios): las cadenas de dentro, unidas a lo largo de la recta de corte. Con el
    interior a la izquierda, la recta se recorre en el sentido d (coord 0: y decreciente si sign > 0; coord 1: x creciente
    si sign > 0); cada salida se une con la entrada siguiente en ese sentido."""
    f = [sign * (p[coord] - value) for p in ring]
    n = len(ring)
    if all(v >= 0 for v in f):
        return [list(ring)]
    if all(v <= 0 for v in f):
        return []

    def cut(a, b, fa, fb):
        t = fa / (fa - fb)
        p = [a[0] + t * (b[0] - a[0]), a[1] + t * (b[1] - a[1])]
        p[coord] = value
        return tuple(p)

    start = next(i for i in range(n) if f[i] < 0)
    chains = []
    cur = None
    for k in range(1, n + 1):
        i, prev = (start + k) % n, (start + k - 1) % n
        a, b, fa, fb = ring[prev], ring[i], f[prev], f[i]
        if fa < 0 <= fb:
            c = b if fb == 0 else cut(a, b, fa, fb)
            cur = [c] if c == b else [c, b]
        elif fa >= 0 and fb >= 0:
            cur.append(b)
        elif fa >= 0 > fb:
            c = a if fa == 0 else cut(a, b, fa, fb)
            if cur[-1] != c:
                cur.append(c)
            chains.append(cur)
            cur = None
    other = 1 - coord
    direction = (-sign) if coord == 0 else sign

    def key(p):
        return p[other] * direction

    entries = sorted(range(len(chains)), key=lambda k: (key(chains[k][0]), k))
    rings = []
    used = [False] * len(chains)
    for k0 in range(len(chains)):
        if used[k0]:
            continue
        out = []
        k = k0
        while True:
            used[k] = True
            out += chains[k]
            u = key(chains[k][-1])
            nxt = next((e for e in entries if key(chains[e][0]) >= u and (e == k0 or not used[e])), None)
            if nxt is None:
                raise GeometryError("recorte de la tierra: una salida sin entrada a lo largo del borde (anillo no simple)")
            if nxt == k0:
                break
            k = nxt
        clean = []
        for p in out:
            if not clean or clean[-1] != p:
                clean.append(p)
        if len(clean) > 1 and clean[0] == clean[-1]:
            clean.pop()
        if len(clean) >= 3 and _ring_area(clean) > 0:
            rings.append(clean)
    return rings


def _area_km2(ring_lonlat: list) -> float:
    lat_ref = sum(p[1] for p in ring_lonlat) / len(ring_lonlat)
    pts = [_km_xy(p[1], p[0], lat_ref) for p in ring_lonlat]
    return abs(_ring_area(pts))


def ne_land_rings(src_dir: Path, frame: Frame) -> list:
    """Anillos de tierra de Natural Earth (land) dentro de la caja del marco, en (lat, lon): el continente recortado y las
    islas de al menos MIN_ISLAND_KM2. Orden fijo: el del archivo (registro, parte, trozo)."""
    out = []
    with Layer(Path(src_dir) / NE_LAND_LAYER) as layer:
        for i in range(layer.count):
            bb = layer.shapes.record_bbox(i)
            if bb is None or bb[2] < frame.lon_min or bb[0] > frame.lon_max or bb[3] < frame.lat_min or bb[1] > frame.lat_max:
                continue
            for part in layer.shapes.parts(i):
                ring = list(part)
                if len(ring) > 1 and ring[0] == ring[-1]:
                    ring.pop()
                if len(ring) < 3:
                    continue
                xs, ys = [p[0] for p in ring], [p[1] for p in ring]
                if max(xs) < frame.lon_min or min(xs) > frame.lon_max or max(ys) < frame.lat_min or min(ys) > frame.lat_max:
                    continue
                if _ring_area(ring) > 0:
                    raise GeometryError(f"{NE_LAND_LAYER}: un agujero (anillo interior) dentro de la caja del marco")
                ring.reverse()   # ESRI: los exteriores van en el sentido de las agujas del reloj
                pieces = [ring]
                for coord, value, sign in ((0, frame.lon_min, 1), (0, frame.lon_max, -1), (1, frame.lat_min, 1),
                                           (1, frame.lat_max, -1)):
                    pieces = [r for piece in pieces for r in clip_ring(piece, coord, value, sign)]
                for piece in pieces:
                    if _area_km2(piece) >= MIN_ISLAND_KM2:
                        out.append([(y, x) for x, y in piece])
    if not out:
        raise GeometryError(f"{NE_LAND_LAYER}: ninguna tierra dentro de la caja del marco")
    return out


def _on_box(frame: Frame, p: tuple) -> bool:
    return p[0] in (frame.lat_min, frame.lat_max) or p[1] in (frame.lon_min, frame.lon_max)


def _ring_dp(ring: list, tol: list, keep: set) -> list:
    """Douglas-Peucker de un anillo cerrado: se abre en su vertice 0, con el mas lejano tambien obligatorio."""
    far = max(range(len(ring)), key=lambda i: (haversine(ring[0], ring[i]), -i))
    pts = ring + [ring[0]]
    idx = simplify(pts, tol + [tol[0]], set(keep) | {far})
    return [i for i in idx if i < len(ring)]


def _segments(rings: list) -> list:
    return [(r, k, ring[k], ring[(k + 1) % len(ring)]) for r, ring in enumerate(rings) for k in range(len(ring))]


def crossings(rings: list) -> list:
    """Pares de aristas que se cortan entre los anillos (enteros), salvo las vecinas de un mismo anillo: [(r, k, r2, k2)]."""
    segs = _segments(rings)
    if not segs:
        return []
    cell = 1024   # unidades uint16 del marco (~40 km): cada arista va en todas las celdas de su caja
    grid: dict = {}
    for n, (_, _, a, b) in enumerate(segs):
        for gx in range(min(a[0], b[0]) // cell, max(a[0], b[0]) // cell + 1):
            for gy in range(min(a[1], b[1]) // cell, max(a[1], b[1]) // cell + 1):
                grid.setdefault((gx, gy), []).append(n)
    out = set()
    for cell_items in grid.values():
        for x in range(len(cell_items)):
            for y in range(x + 1, len(cell_items)):
                i, j = cell_items[x], cell_items[y]
                r1, k1, a1, b1 = segs[i]
                r2, k2, a2, b2 = segs[j]
                if r1 == r2:
                    n = len(rings[r1])
                    if abs(k1 - k2) == 1 or abs(k1 - k2) == n - 1:
                        continue
                if (min(i, j), max(i, j)) in out:
                    continue
                if _segments_cross(a1, b1, a2, b2):
                    out.add((min(i, j), max(i, j)))
    return sorted((segs[i][0], segs[i][1], segs[j][0], segs[j][1]) for i, j in out)


def simplify_land(frame: Frame, rings: list, zone: MundoZone) -> list:
    """Anillos de tierra simplificados por vista y cuantizados, sin cortes entre ellos ni consigo mismos: donde la
    simplificacion corta, se repite con la mitad de tolerancia en los tramos afectados (hasta LAND_REPAIR_ROUNDS veces)."""
    tols = [[DP_MUNDO_KM if zone.contains(lat, lon) else DP_CUENCA_KM for lat, lon in ring] for ring in rings]
    keeps = [{i for i, p in enumerate(ring) if _on_box(frame, p)} for ring in rings]
    for _ in range(LAND_REPAIR_ROUNDS + 1):
        idx = [_ring_dp(ring, tol, keep) for ring, tol, keep in zip(rings, tols, keeps)]
        q = []
        back = []
        for ring, ids in zip(rings, idx):
            qr, br = [], []
            for i in ids:
                p = frame.quant(*ring[i])
                if not qr or qr[-1] != p:
                    qr.append(p)
                    br.append(i)
            if len(qr) > 1 and qr[0] == qr[-1]:
                qr.pop()
                br.pop()
            q.append(qr)
            back.append(br)
        bad = crossings(q) + [(r, -1, r, -1) for r, qr in enumerate(q) if len(set(qr)) != len(qr) or len(qr) < 3]
        if not bad:
            out = []
            for qr in q:
                if _area2(qr) < 0:
                    qr = list(reversed(qr))
                out.append(qr)
            return out
        for r1, k1, r2, k2 in bad:
            for r, k in ((r1, k1), (r2, k2)):
                br = back[r]
                if k < 0:
                    tols[r] = [t / 2 for t in tols[r]]
                    continue
                a, b = br[k], br[(k + 1) % len(br)]
                span = range(a, b + 1) if b > a else list(range(a, len(rings[r]))) + list(range(0, b + 1))
                for i in span:
                    tols[r][i] = tols[r][i] / 2
    raise GeometryError("tierra: la simplificacion sigue cortando la costa tras "
                        f"{LAND_REPAIR_ROUNDS} rondas de reparacion")


def triangulate(poly: list) -> list:
    """Triangulacion por orejas de un poligono simple en sentido positivo con vertices enteros: indices de vertices, cada
    triangulo en el mismo sentido y sin area nula. Lista doblemente enlazada y recorrido circular desde el vertice 0; una
    oreja es un vertice estrictamente convexo cuyo triangulo no contiene (ni en sus bordes) ningun vertice reflejo o
    alineado (en una rejilla). Los vertices alineados no se quitan sin triangulo: asi ninguna arista de la costa queda
    partida por un vertice que solo ve un lado (M5.1: la tierra se valida por sus aristas). Determinista."""
    n = len(poly)
    if n < 3:
        return []
    prv = [(i - 1) % n for i in range(n)]
    nxt = [(i + 1) % n for i in range(n)]
    xs = [p[0] for p in poly]
    ys = [p[1] for p in poly]
    cell = max(64, int(max(max(xs) - min(xs), max(ys) - min(ys)) / max(1.0, math.sqrt(n))) + 1)
    grid: dict = {}
    blocking = [False] * n

    def cross_at(i):
        return _cross(poly[prv[i]], poly[i], poly[nxt[i]])

    def drop(i):
        if blocking[i]:
            blocking[i] = False
            grid[(poly[i][0] // cell, poly[i][1] // cell)].discard(i)

    for i in range(n):
        if cross_at(i) <= 0:
            blocking[i] = True
            grid.setdefault((poly[i][0] // cell, poly[i][1] // cell), set()).add(i)
    alive = n
    tris: list = []
    i = 0
    misses = 0
    while alive > 3:
        if misses > alive:
            raise GeometryError("triangulacion: no encuentro oreja (poligono no simple)")
        a, c = prv[i], nxt[i]
        A, B, C = poly[a], poly[i], poly[c]
        ok = _cross(A, B, C) > 0
        if ok:
            x0, x1 = min(A[0], B[0], C[0]) // cell, max(A[0], B[0], C[0]) // cell
            y0, y1 = min(A[1], B[1], C[1]) // cell, max(A[1], B[1], C[1]) // cell
            for gx in range(x0, x1 + 1):
                for gy in range(y0, y1 + 1):
                    for j in grid.get((gx, gy), ()):
                        if j in (a, i, c) or poly[j] in (A, B, C):
                            continue
                        if _in_triangle(poly[j], A, B, C):
                            ok = False
                            break
                    if not ok:
                        break
                if not ok:
                    break
        if ok:
            tris.extend((a, i, c))
            drop(i)
            nxt[a], prv[c] = c, a
            alive -= 1
            for k in (a, c):
                if blocking[k] and cross_at(k) > 0:
                    drop(k)
            misses = 0
        else:
            misses += 1
        i = c
    a, c = prv[i], nxt[i]
    if _cross(poly[a], poly[i], poly[c]) > 0:
        tris.extend((a, i, c))
    return tris


def land_zones(rings_q: list) -> list:
    """Zonas del terreno: [(id, tile, anillos)]. PUNTO DE ENTRADA DE LA MASCARA DEL ARTISTA (pendiente; encargo del Mapa,
    seccion 4, y propuesta §11.3, punto 2): cuando llegue (PNG o SVG pintado sobre la guia de proyeccion, con dosel, varzea,
    sabana, roca y nieve), se vectoriza en poligonos por mosaico, se cortan con estos anillos y cada zona se triangula
    aparte; el documento ya lleva first y count por zona. Hasta entonces, una sola zona [P] (LAND_ZONE)."""
    return [(LAND_ZONE["id"], LAND_ZONE["tile"], rings_q)]


def build_land(rings_q: list) -> tuple:
    """verts, tris y zones de los anillos cuantizados: cada zona, sus anillos seguidos en verts y sus triangulos seguidos
    en tris (first y count)."""
    verts: list = []
    tris: list = []
    zones = []
    for zid, tile, rings in land_zones(rings_q):
        first = len(tris) // 3
        for ring in rings:
            base = len(verts)
            verts += ring
            tris += [base + i for i in triangulate(ring)]
        zones.append({"id": zid, "tile": tile, "first": first, "count": len(tris) // 3 - first})
    if len(verts) > Q + 1:
        raise GeometryError(f"tierra: {len(verts)} vertices no caben en indices uint16")
    return verts, tris, zones


# ----------------------------------------------------------------------------- geometria de produccion

def _host_order(rivers: dict) -> list:
    """Ids en el orden de la investigacion, cada anfitrion antes que sus afluentes."""
    done: list = []
    pending = list(rivers)
    while pending:
        progressed = False
        for rid in list(pending):
            host = BRANCHES.get(rid) if KIND.get(rivers[rid].get("tipo")) == "main" else HOSTS.get(rid)
            if rid == MAIN_ID or host in done or host not in rivers:
                done.append(rid)
                pending.remove(rid)
                progressed = True
        if not progressed:
            raise GeometryError(f"anfitriones en ciclo: {pending}")
    return done


def production_geometry(source: dict, src_dir: Path = SOURCE_DIR) -> tuple:
    """Geometria de produccion desde las descargas (ya comprobadas con sources.json). Devuelve (Geometry, errores)."""
    errors: list = []
    frame = Frame.from_source(source)
    zone = MundoZone(source)
    rivers = {}
    for name in RIVER_LISTS:
        for r in source.get(name, []) or []:
            if isinstance(r, dict) and KIND.get(r.get("tipo")) and isinstance(r.get("id"), str):
                rivers[r["id"]] = r
    ctrls = {rid: [(float(p["lat"]), float(p["lon"])) for p in r.get("puntos", [])] for rid, r in rivers.items()}
    polylines: dict = {}
    reaches: dict = {}
    protect: dict = {}
    src: dict = {}
    hr = HydroRivers(Path(src_dir) / HR_LAYER, frame)
    try:
        for rid in _host_order(rivers):
            r, ctrl = rivers[rid], ctrls[rid]
            mode = LINE_SRC.get(rid, "hydrorivers")
            src[rid] = mode
            protect[rid] = set()
            host = HOSTS.get(rid) if KIND[r["tipo"]] != "main" else None
            host_reaches = reaches.get(host) if host is not None else None
            try:
                if mode == "rio_amazonas":
                    pts = list(ctrl)
                    protect[rid] = set(range(len(pts)))
                elif mode == "ne10m":
                    pts = _ne_line(src_dir, rid, ctrl)
                else:
                    pts, spans, joined = _trace_hr(hr, rid, r, ctrl, host_reaches)
                    if not joined or host_reaches is None:
                        pts, spans, tail = _research_tail(pts, spans, ctrl)
                        protect[rid] = set(range(len(pts) - tail - 1, len(pts)))
                    reaches[rid] = {s[0] for s in spans}
            except GeometryError as e:
                errors.append(f"linea {rid}: {e}" if not str(e).startswith("linea") else str(e))
                continue
            polylines[rid] = pts
    finally:
        hr.close()
    # Bocas: el ultimo punto de cada afluente es un vertice de su anfitrion (HydroRIVERS) o se lleva al mas cercano
    # (Natural Earth); ese vertice no se simplifica.
    for rid, pts in polylines.items():
        host = HOSTS.get(rid)
        if (host is None or host not in polylines or KIND[rivers[rid]["tipo"]] == "main" or src[host] == "rio_amazonas"
                or src[rid] == "rio_amazonas"):
            continue
        hpts = polylines[host]
        k, d = _nearest_vertex(hpts, *pts[-1])
        if src[rid] == "hydrorivers" and d > 0.01:
            errors.append(f"linea {rid}: su boca de HydroRIVERS no es un vertice de {host} ({d:.2f} km)")
        if d > TOLERANCE_KM:
            errors.append(f"linea {rid}: su boca queda a {d:.1f} km de {host}")
        if pts[-1] != hpts[k]:
            pts.append(hpts[k])
        protect[host].add(k)
    # El ramal (el Para) arranca en un vertice del cauce: se le antepone el mas cercano a su primer punto (Gurupa, a ~2 km
    # del cauce de HydroRIVERS), para que su arranque y su s sobre el cauce sean el mismo punto (el cliente une los dos
    # tramos de una ruta por ahi; RiverMapReader.JoinTolerance).
    for rid, host in BRANCHES.items():
        if rid in polylines and host in polylines:
            pts, hpts = polylines[rid], polylines[host]
            k, _ = _nearest_vertex(hpts, *pts[0])
            if pts[0] != hpts[k]:
                pts.insert(0, hpts[k])
                protect[rid] = {i + 1 for i in protect[rid]} | {0}
            protect[host].add(k)
    lines = {}
    for rid, pts in polylines.items():
        tol = [DP_MUNDO_KM if zone.contains(lat, lon) else DP_CUENCA_KM for lat, lon in pts]
        idx = simplify(pts, tol, protect[rid])
        lines[rid] = (src[rid], _quantize_line(frame, pts, idx, rid, errors))
    try:
        rings = simplify_land(frame, ne_land_rings(src_dir, frame), zone)
        land = build_land(rings)
    except (GeometryError, ShapefileError) as e:
        errors.append(str(e))
        land = ([], [], [])
    return Geometry("produccion", lines, land), errors


def committed_geometry(path: Path = MAP_PATH) -> Geometry:
    """La geometria de lo commiteado (river_map_v1.json): puntos de cada linea y tierra tal cual. Sin las descargas,
    --check regenera todo lo demas desde aqui y lo compara byte a byte; stops.py calcula con ella las rutas."""
    try:
        doc = json.loads(Path(path).read_bytes().decode("utf-8"))
        lines = {e["id"]: (e["src"], decode_points(e["pts"])) for e in doc["lines"]}
        land = doc["land"]
        geo_land = (decode_points(land["verts"]), decode_indices(land["tris"]),
                    [{"id": z["id"], "tile": z["tile"], "first": z["first"], "count": z["count"]} for z in land["zones"]])
    except FileNotFoundError as e:
        raise GeometryError(f"falta {_rel(path)}") from e
    except (OSError, UnicodeDecodeError, json.JSONDecodeError, KeyError, TypeError, ValueError) as e:
        raise GeometryError(f"{_rel(path)} no se puede leer como af-river-map/1 ({e})") from e
    return Geometry("commiteado", lines, geo_land)


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


def load_geometry(source: dict, sources: Path, manifest_path: Path, out: Path, check: bool) -> tuple:
    """La geometria que se usa: la de produccion si las descargas estan completas y con su SHA-256; sin ninguna, con
    --check, la de lo commiteado (y un aviso); sin ellas y sin --check, nada (no se regenera). Devuelve (Geometry o None,
    errores, avisos)."""
    manifest = load_manifest(manifest_path)
    state = sources_state(sources, manifest)
    if state.status == "bad":
        return None, [f"descargas de {_rel(sources)}: {p}" for p in state.problems], []
    if state.status == "complete":
        geo, errors = production_geometry(source, sources)
        return geo, errors, []
    if not check:
        return None, [f"faltan las descargas en {_rel(sources)} (Natural Earth y HydroRIVERS; sources.json, M5.1): sin "
                      "ellas no se regenera river_map_v1.json; geometry.py --check comprueba lo commiteado"], []
    return committed_geometry(out), [], [
        f"sin las descargas en {_rel(sources)}: se comprueba lo commiteado (sus puntos y su tierra tal cual; todo lo "
        "demas se regenera y se compara byte a byte)"]


def main(argv: list | None = None) -> int:
    ap = argparse.ArgumentParser(description="Genera o comprueba river_map_v1.json (af-river-map/1).")
    ap.add_argument("--check", action="store_true",
                    help="valida el Mapa y las rutas de river_stops_v1.json y compara con lo commiteado; codigo 1 si algo falla")
    ap.add_argument("--source", type=Path, default=SOURCE_PATH, help="rio-amazonas.json (por defecto, el de docs/research)")
    ap.add_argument("--sources", type=Path, default=SOURCE_DIR,
                    help="carpeta de las descargas de Natural Earth y HydroRIVERS (por defecto, tools/river-map/source)")
    ap.add_argument("--manifest", type=Path, default=SOURCES_MANIFEST, help="sources.json (SHA-256 y licencias)")
    ap.add_argument("--overrides", type=Path, default=OVERRIDES_PATH, help="decisiones [P] por parada (stops.py)")
    ap.add_argument("--worldtable", type=Path, default=WORLDTABLE_PATH, help="WorldTable.cs del cliente")
    ap.add_argument("--out", type=Path, default=MAP_PATH, help="river_map_v1.json que se escribe o se compara")
    ap.add_argument("--stops", type=Path, default=STOPS_PATH, help="river_stops_v1.json cuyas rutas se comprueban")
    ap.add_argument("--facts", type=Path, default=FACTS_PATH, help="facts.csv donde se buscan los datos de passing y viewpoints")
    args = ap.parse_args(argv)
    try:
        source = load_json(args.source)
        geo, errors, notes = load_geometry(source, args.sources, args.manifest, args.out, args.check)
        for note in notes:
            emit(f"AVISO {note}")
        if geo is None:
            for err in errors:
                emit(f"FAIL  {err}")
            return 1
        doc, model, errs = build_map(source, geo)
        errors += errs
    except (SourceError, GeometryError, ShapefileError) as e:
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
    size = len(text.encode("utf-8"))
    if size > MAX_BYTES:
        emit(f"FAIL  river_map_v1.json mediria {size} bytes (maximo {MAX_BYTES}, M5.1)")
        return 1
    if not args.check:
        out = Path(args.out)
        out.parent.mkdir(parents=True, exist_ok=True)
        out.write_bytes(text.encode("utf-8"))
        emit(f"OK  escrito {_rel(out)} ({geo.kind}): {len(doc['lines'])} lineas, {len(doc['passing'])} puntos de paso, "
             f"{size} bytes")
        emit("    despues: py -3.14 tools/river-map/stops.py y stops.py --write-texts (las rutas salen de este Mapa)")
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
        _, _, errs = stops_tool.build_all(source, overrides, wt, geo)
        errors += [f"stops.py: {e}" for e in errs]
    except (SourceError, GeometryError) as e:
        errors.append(str(e))
    for err in errors:
        emit(f"FAIL  {err}")
    if errors:
        return 1
    emit(f"OK  {len(doc['lines'])} lineas, {len(doc['passing'])} puntos de paso y {len(doc['viewpoints'])} miradores "
         f"validos y {_rel(args.out)} al dia ({geo.kind}; {size} bytes)")
    emit(f"OK  rutas de {_rel(args.stops)} (at y leg de {len(committed['stops'])} paradas) de acuerdo con el Mapa")
    for warning in (pending_exceptions(committed, model, records) + pending_controls(model, source)
                    + pending_sketches(model) + pending_facts(doc, args.facts)):
        emit(f"AVISO {warning}")
    return 0


def pending_controls(model: Model, source: dict) -> list:
    """Un aviso por linea con los puntos de control fuera de la tolerancia de su vista que solo pasan por su excepcion
    medida (PENDING_CONTROL_KM, pendientes del duenno), y de las excepciones que ya sobran."""
    zone = MundoZone(source) if model.geo is not None else None
    out = []
    for rid in model.order:
        line = model.lines[rid]
        used, spare = [], []
        for j, p in enumerate(line.source.get("puntos", [])):
            d, view, allowed, pending = control_check(line, j, p, zone)
            if not pending:
                continue
            canon = MUNDO_TOLERANCE_KM if view == "Mundo" else TOLERANCE_KM
            (used if d > canon else spare).append(f"{j} ({d:.1f} km, {view})")
        if used:
            out.append(f"linea {rid}: {len(used)} puntos de control fuera de la tolerancia de su vista, con excepcion "
                       "medida pendiente del duenno (PENDING_CONTROL_KM): " + ", ".join(used))
        if spare:
            out.append(f"linea {rid}: sobran sus excepciones de PENDING_CONTROL_KM en " + ", ".join(spare))
    return out


def pending_sketches(model: Model) -> list:
    """Un aviso por linea que, con la geometria de produccion o la commiteada, sigue siendo el esbozo de la investigacion
    (LINE_SRC = rio_amazonas): se ve en la vista Mundo sin meandros y su tolerancia no se puede medir (revision de M5.1)."""
    if model.geo is None:
        return []
    out = []
    for rid in model.order:
        line = model.lines[rid]
        if line.src != SRC:
            continue
        why = SKETCH_WHY.get(rid, "sin geometria de produccion")
        out.append(f"linea {rid}: sigue siendo el esbozo de la investigacion (src {SRC}, {len(line.q)} puntos, sin "
                   f"meandros); se dibuja en la vista Mundo y --check solo la mide en la Cuenca, contra sus propios puntos, "
                   f"nunca con la tolerancia de la vista Mundo (pendiente del duenno o del investigador: {why})")
    return out


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
