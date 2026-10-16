# -*- coding: utf-8 -*-
"""Mapa de muestra del Mapa del río (Tintalú / Amazon Fill) con la forma real del Amazonas.

Material de apoyo, no canon (docs/research/mapa-del-rio). Lee rio-amazonas.json, que está en la misma
carpeta, y escribe mapa-muestra.svg: el estado del Mapa al ganar el nivel 200 (último de los mundos 1-5; fin
de la 1.0 solo si el dueño elige la B de la pregunta Q14), con el agua real en lo remontado y el resto del río en
vidrio. Solo biblioteca estándar de Python 3.14.

Uso (desde la raíz del repositorio):
    py -3.14 docs/research/mapa-del-rio/generar_mapa.py                  # escribe mapa-muestra.svg
    py -3.14 docs/research/mapa-del-rio/generar_mapa.py --check          # valida el JSON (código 1 si falla)
    py -3.14 docs/research/mapa-del-rio/generar_mapa.py --actualizar-xy  # recalcula las coordenadas 0-1

Proyección (la de rio-amazonas.json, sección «proyeccion»): equirrectangular con factor cos(lat0) y giro
de 90° en el sentido de las agujas del reloj, sin espejo: Andes arriba, Atlántico abajo, norte a la derecha.
"""
from __future__ import annotations

import argparse
import json
import math
import re
import sys
import unicodedata
from pathlib import Path
from xml.sax.saxutils import escape

HERE = Path(__file__).resolve().parent
JSON_PATH = HERE / "rio-amazonas.json"
SVG_PATH = HERE / "mapa-muestra.svg"

# ------------------------------------------------------------------ estilo (D-003, direccion-visual-v2 §2)
C = {
    "fondo_a": "#0E2B23", "fondo_b": "#143D30",
    "madera_oscura": "#36261C", "marco": "#7C4E3A", "marco_bisel": "#D29B6E", "crema": "#FFF8E6",
    "selva_sombra": "#1C5236", "selva_media": "#2C7A4B", "selva_luz": "#63B548", "hoja_media": "#23824A",
    "roca_a": "#B7A07C", "roca_b": "#8C775A", "nieve": "#F4FBFF",
    "mar_a": "#0F6275", "mar_b": "#13788F", "ola": "#8ADBE6",
    "blanca": "#B08A5A", "negra": "#4A2C14", "clara": "#2E8B8B", "andina": "#9FC9D4",
    "vidrio": "#CFEFF3", "vidrio_borde": "#EAF7F3", "destello": "#FFF4D2",
    "fuera": "#9FB7C4", "panel": "#234452", "ambar": "#C98A2B", "tinta": "#2B2118",
}
FONT = "'Baloo 2','Nunito','Segoe UI',Arial,sans-serif"
TEXT = "'Nunito','Segoe UI',Arial,sans-serif"

AGUA_COLOR = {"blanca": C["blanca"], "negra": C["negra"], "clara": C["clara"]}
RIO_AGUA = {  # agua dominante para pintar la cinta llena
    "amazonas": "blanca", "para": "blanca", "tapajos": "clara", "negro": "negra", "casiquiare": "negra",
    "branco": "blanca", "takutu": "blanca", "putumayo_ica": "blanca", "napo": "blanca", "maranon": "blanca",
    "madre_de_dios": "blanca", "beni": "blanca", "mamore": "blanca", "itenez": "clara", "madeira": "blanca",
    "xingu": "clara", "trombetas": "clara", "purus": "blanca", "jurua": "blanca", "japura": "blanca",
    "nanay": "negra", "maroni": "blanca", "orinoco": "blanca", "tocantins": "clara",
    "samiria": "negra",  # ríos locales: solo los de agua comprobada; los [NV] se dibujan punteados
}
ANCHO = {"cauce": 7.0, "ramal": 5.5, "afluente_mundo": 4.2, "afluente_paso": 3.0, "fuera_de_cuenca": 2.4, "rio_local": 1.2}
ENTERO = "entero"  # valor de «afluentes_llenos_hasta_km_desde_la_boca» para llenar un río completo


# ------------------------------------------------------------------ utilidades
def hav(a, b):
    r = 6371.0
    la1, lo1, la2, lo2 = map(math.radians, (a[0], a[1], b[0], b[1]))
    d = math.sin((la2 - la1) / 2) ** 2 + math.cos(la1) * math.cos(la2) * math.sin((lo2 - lo1) / 2) ** 2
    return 2 * r * math.asin(math.sqrt(d))


def fmt(v):
    return f"{v:.1f}".rstrip("0").rstrip(".")


def esc(s):
    return escape(str(s), {'"': "&quot;"})


class Proj:
    """Proyección del JSON: (lat, lon) → (x, y) normalizadas en [0, 1] y aspecto alto/ancho."""

    def __init__(self, p):
        caja = p["caja"]
        self.lat0 = p["lat0"]
        self.la0, self.la1 = caja["lat_min"], caja["lat_max"]
        self.lo0, self.lo1 = caja["lon_min"], caja["lon_max"]
        self.aspect = (self.lo1 - self.lo0) * math.cos(math.radians(self.lat0)) / (self.la1 - self.la0)

    def norm(self, lat, lon):
        return (lat - self.la0) / (self.la1 - self.la0), (lon - self.lo0) / (self.lo1 - self.lo0)


class View:
    """Ventana de dibujo: una caja (lat, lon) puesta en un rectángulo de píxeles, con la misma rotación."""

    def __init__(self, x0, y0, w, lat_rng, lon_rng, lat0, escala=1.0):
        self.x0, self.y0, self.w = x0, y0, w
        self.la0, self.la1 = lat_rng
        self.lo0, self.lo1 = lon_rng
        self.k = w / (self.la1 - self.la0)  # px por grado de latitud
        self.c = math.cos(math.radians(lat0))
        self.h = (self.lo1 - self.lo0) * self.c * self.k
        self.escala = escala

    def __call__(self, lat, lon):
        return self.x0 + (lat - self.la0) * self.k, self.y0 + (lon - self.lo0) * self.c * self.k


def smooth_path(pts):
    """Camino SVG suave (Catmull-Rom → Bézier cúbicas) por los puntos en píxeles."""
    if len(pts) < 2:
        return ""
    d = [f"M{fmt(pts[0][0])},{fmt(pts[0][1])}"]
    n = len(pts)
    for i in range(n - 1):
        p0 = pts[i - 1] if i > 0 else pts[i]
        p1, p2 = pts[i], pts[i + 1]
        p3 = pts[i + 2] if i + 2 < n else pts[i + 1]
        c1 = (p1[0] + (p2[0] - p0[0]) / 6, p1[1] + (p2[1] - p0[1]) / 6)
        c2 = (p2[0] - (p3[0] - p1[0]) / 6, p2[1] - (p3[1] - p1[1]) / 6)
        d.append(f"C{fmt(c1[0])},{fmt(c1[1])} {fmt(c2[0])},{fmt(c2[1])} {fmt(p2[0])},{fmt(p2[1])}")
    return " ".join(d)


def split_by_distance(latlon, lim_km_from_mouth):
    """Parte una línea (cabecera → boca) en el tramo de vidrio y el tramo lleno (los últimos lim km).

    lim_km_from_mouth = ENTERO (o math.inf) llena la línea completa, sea cual sea su longitud."""
    if lim_km_from_mouth == ENTERO or lim_km_from_mouth == math.inf:
        return [], list(latlon)
    dist = [0.0] * len(latlon)
    for i in range(len(latlon) - 2, -1, -1):
        dist[i] = dist[i + 1] + hav(latlon[i], latlon[i + 1])
    if lim_km_from_mouth <= 0:
        return latlon, []
    if lim_km_from_mouth >= dist[0]:
        return [], latlon
    for i in range(len(latlon) - 1):
        if dist[i] >= lim_km_from_mouth >= dist[i + 1]:
            t = (dist[i] - lim_km_from_mouth) / max(1e-9, dist[i] - dist[i + 1])
            cut = (latlon[i][0] + (latlon[i + 1][0] - latlon[i][0]) * t, latlon[i][1] + (latlon[i + 1][1] - latlon[i][1]) * t)
            return latlon[: i + 1] + [cut], [cut] + latlon[i + 1:]
    return latlon, []


def split_main_by_km(points, km_lim):
    """Cauce principal: lleno donde km_desde_nacimiento ≥ km_lim (de ahí hasta el mar)."""
    glass, full = [], []
    for i, p in enumerate(points):
        ll = (p["lat"], p["lon"])
        if p["km_desde_nacimiento"] < km_lim:
            glass.append(ll)
            nxt = points[i + 1] if i + 1 < len(points) else None
            if nxt and nxt["km_desde_nacimiento"] >= km_lim:
                t = (km_lim - p["km_desde_nacimiento"]) / (nxt["km_desde_nacimiento"] - p["km_desde_nacimiento"])
                cut = (p["lat"] + (nxt["lat"] - p["lat"]) * t, p["lon"] + (nxt["lon"] - p["lon"]) * t)
                glass.append(cut)
                full.append(cut)
        else:
            full.append(ll)
    return glass, full


def km_segment(points, km_a, km_b):
    """Puntos (lat, lon) del cauce entre dos km desde el nacimiento (km_a < km_b)."""
    out = []
    for i in range(len(points) - 1):
        a, b = points[i], points[i + 1]
        ka, kb = a["km_desde_nacimiento"], b["km_desde_nacimiento"]
        for k in (km_a, km_b):
            if ka <= k <= kb and kb > ka:
                t = (k - ka) / (kb - ka)
                out.append((k, (a["lat"] + (b["lat"] - a["lat"]) * t, a["lon"] + (b["lon"] - a["lon"]) * t)))
        if km_a < ka < km_b:
            out.append((ka, (a["lat"], a["lon"])))
    out.sort(key=lambda e: e[0])
    res = []
    for _, ll in out:
        if not res or res[-1] != ll:
            res.append(ll)
    return res


def offset_line(px, d):
    """Desplaza una polilínea en píxeles d unidades a su izquierda."""
    out = []
    for i in range(len(px)):
        a = px[max(0, i - 1)]
        b = px[min(len(px) - 1, i + 1)]
        dx, dy = b[0] - a[0], b[1] - a[1]
        n = math.hypot(dx, dy) or 1.0
        out.append((px[i][0] - dy / n * d, px[i][1] + dx / n * d))
    return out


# ------------------------------------------------------------------ comprobaciones
FIRST_LEVELS = [1, 11, 41, 81, 141, 201, 301, 421, 561, 701, 821, 921, 961]  # WorldTable.FirstLevels


def world_of(level):
    w = 1
    for i, f in enumerate(FIRST_LEVELS):
        if level >= f:
            w = i + 1
    return w


def iter_points(doc):
    for r in doc["rios"]:
        for p in r["puntos"]:
            yield p
    for key in ("paradas_1_200", "puntos_de_paso_1_200", "miradores_1_200", "cruces_de_pais", "rotulos_de_pais"):
        for p in doc[key]:
            yield p
    # Paradas detalladas de 201-700 (fase F9, desde M3.7a) y sus ríos locales nuevos.
    for p in doc.get("paradas_201_700", []):
        yield p
    for r in doc.get("rios_locales_201_700", []):
        for p in r["puntos"]:
            yield p
    for m in doc["paradas_201_1000"]:
        for s in m["sitios"] + m.get("puntos_de_paso_del_viaje", []):
            if "lat" in s:
                yield s
    deco = doc["decorado_de_la_muestra"]
    for key in ("costa_atlantica", "costa_pacifica", "picos_y_cabeceras", "pie_oriental_de_los_andes"):
        for p in deco[key]:
            yield p


ETIQUETA = re.compile(r"\[(V|E|NV|P)(?:\s+([^\]]*))?\]")


def ids_citados(texto):
    """IDs de fuente dentro de las etiquetas «[V id, id]», «[E id; id]» o «[V id: comentario]» de un texto."""
    for m in ETIQUETA.finditer(texto or ""):
        cuerpo = (m.group(2) or "").split(":")[0]
        for tok in re.split(r"[,;]", cuerpo):
            tok = tok.strip()
            if tok:
                yield tok


# «Otro dato» de las tarjetas de 1-200 (M2.7; propuesta §5.1 bis y D-013, punto 11): campos de cada entrada de
# «otros_datos», forma de la clave (glosario §10: map.stop.<lugar>.dato_NN o map.country.<iso2>.dato_NN), fecha en que
# se reabrieron sus fuentes (R5) y los candidatos de §(e) que tienen que estar.
CAMPOS_OTRO_DATO = ("clave", "texto", "etiqueta", "fuente", "releida", "origen", "nota")
# La misma forma que esas dos ramas de fact_key en schemas/af-river-stops-1.schema.json (contrato 30): [0-9] y no \d,
# que también acepta dígitos Unicode, y se aplica con fullmatch, porque «$» de Python admite un salto de línea final.
CLAVE_DATO = re.compile(r"^map\.(stop\.[a-z0-9_]+|country\.[a-z]{2})\.dato_[0-9]{2}$")
FECHA = re.compile(r"^\d{4}-\d{2}-\d{2}$")
# Q8-A (D-018): ninguna cifra con separador de miles ni decimales dentro del texto; van como {n} con su valor en «args».
CIFRA_CON_SEPARADOR = re.compile(r"\d[.,]\d|\d[ \u00a0\u202f]\d{3}(?!\d)")
# Marcador con nombre: «{n}», «{n:formato}» o el plural de Smart Strings del contrato 32, «{n:plural:a|b}», cuyas
# formas pueden llevar «{}» para la cifra («{n:plural:Te falta {} semilla|Te faltan {} semillas}», propuesta de
# idiomas 3.3). El nombre (grupo 1) siempre necesita su valor en «args»; el formato (grupo 2) solo cuenta al medir.
MARCADOR = re.compile(r"\{([a-z_]+)(?::((?:[^{}]|\{\})*))?\}")
CANDIDATOS_5_1_BIS = {  # id de la parada: claves que tienen que estar en sus «otros_datos» (propuesta §5.1 bis)
    "belem": ("map.stop.belem.dato_01", "map.stop.belem.dato_02"),          # Ver-o-Peso (1977) y el chibé
    "santarem": ("map.stop.santarem.dato_01",),                              # las cuias del Bajo Amazonas
    "encontro": ("map.stop.encontro.dato_01",),                              # IPHAN, 2010
    "manaos": ("map.stop.manaos.dato_03", "map.stop.manaos.dato_04"),       # la régua (1902) y los flutuantes
    "tefe": ("map.stop.tefe.dato_02", "map.stop.tefe.dato_03"),             # el pirarucú de Mamirauá y el manatí
    "tarapoto": ("map.stop.tarapoto.dato_02",),                              # Ramsar
    "itaya": ("map.stop.itaya.dato_01",),                                    # la creciente, sin nombrar el barrio
    "pacaya_samiria": ("map.stop.pacaya_samiria.dato_03",),                  # las tortugas, con las comunidades
}


def sin_tildes(texto):
    return "".join(c for c in unicodedata.normalize("NFD", texto) if not unicodedata.combining(c)).casefold()


def es_cifra(valor):
    return isinstance(valor, (int, float)) and not isinstance(valor, bool)


def texto_mostrado(texto, args):
    """El texto con sus marcadores resueltos en el caso más largo: cifras con separador de miles y, en los plurales
    «{n:plural:a|b}», la forma más larga con su «{}» resuelto; del plural solo se quita el formato. Sirve para medir
    el límite de caracteres."""
    def cifra(valor):
        return f"{valor:,}".replace(",", ".")

    def sustituye(m):
        nombre, formato = m.group(1), m.group(2) or ""
        valor = args.get(nombre)
        if formato.startswith("plural:"):
            formas = formato[len("plural:"):].split("|")
            if es_cifra(valor):
                formas = [f.replace("{}", cifra(valor)) for f in formas]
            return max(formas, key=len)
        if not es_cifra(valor):
            return m.group(0)
        return cifra(valor)
    return MARCADOR.sub(sustituye, texto)


def revisar_texto(donde, texto, args, errores, medir=True):
    """Reglas comunes a todo texto visible de una parada: ≤ 90 caracteres, Q8-A y el veto de «Belén» (07 §8.1).
    Todo marcador, también el del plural, necesita su valor en «args» (el del plural, una cifra). El dato del nivel
    ya se mide arriba (medir=False), para no dar dos veces el mismo error."""
    marcadores = list(MARCADOR.finditer(texto))
    nombres = {m.group(1) for m in marcadores}
    if nombres != set(args):
        errores.append(f"{donde}: marcadores {sorted(nombres)} y args {sorted(args)} no coinciden")
    for m in marcadores:
        if (m.group(2) or "").startswith("plural:") and m.group(1) in args and not es_cifra(args[m.group(1)]):
            errores.append(f"{donde}: el plural «{m.group(1)}» necesita una cifra en args")
    if CIFRA_CON_SEPARADOR.search(texto):
        errores.append(f"{donde}: cifra con separador o decimales en el texto (va como {{n}} con su valor en args, Q8-A)")
    n = len(texto_mostrado(texto, args))
    if medir and n > 90:
        errores.append(f"{donde}: {n} caracteres (> 90)")
    if "belen" in sin_tildes(texto):
        errores.append(f"{donde}: nombra «Belén» (07 §8.1; vetos del mundo 5)")


def revisar_otros_datos(doc, ids, etiqueta_de, errores):
    """«Otro dato» de las tarjetas de 1-200 (M2.7) y de 201-700 (paradas_201_700, desde M3.7a): mismas reglas, y las
    claves no se repiten entre las dos listas."""
    fuente_de = {f["id"]: f for f in doc["fuentes"]}
    claves = {}
    for s in doc["paradas_1_200"] + doc.get("paradas_201_700", []):
        d = s.get("dato_del_nivel")
        if isinstance(d, dict):
            # Sin «clave», el error legible lo da revisar_paradas_201_700 («clave del dato None fuera de …»).
            if d.get("clave"):
                claves.setdefault(d["clave"], []).append(s["id"])
            revisar_texto(f"parada {s['id']}: dato del nivel", d.get("texto", ""), d.get("args", {}), errores, medir=False)
        otros = s.get("otros_datos")
        if not isinstance(otros, list):
            errores.append(f"parada {s['id']}: sin «otros_datos» (una lista, vacía si la tarjeta no lleva «Otro dato»)")
            continue
        for i, o in enumerate(otros):
            donde = f"parada {s['id']}: otros_datos[{i}]"
            if not isinstance(o, dict):
                errores.append(f"{donde}: no es un objeto")
                continue
            faltan = [c for c in CAMPOS_OTRO_DATO if not o.get(c)]
            if faltan:
                errores.append(f"{donde}: faltan {', '.join(faltan)}")
                continue
            clave = o["clave"]
            claves.setdefault(clave, []).append(s["id"])
            if not CLAVE_DATO.fullmatch(clave):
                errores.append(f"{donde}: clave «{clave}» fuera de la forma map.stop.<lugar>.dato_NN o map.country.<iso2>.dato_NN")
            if "belen" in sin_tildes(clave):
                errores.append(f"{donde}: la clave nombra «Belén»")
            args = o.get("args", {})
            if not isinstance(args, dict):
                errores.append(f"{donde}: «args» tiene que ser un objeto")
                args = {}
            revisar_texto(f"{donde} ({clave})", o["texto"], args, errores)
            if o["etiqueta"] not in ("V", "E"):
                errores.append(f"{donde}: etiqueta {o['etiqueta']}; solo [V] o [E] (ningún [NV] ni [P])")
            if not FECHA.match(o["releida"]):
                errores.append(f"{donde}: «releida» no es una fecha AAAA-MM-DD")
            fs = re.split(r",\s*", o["fuente"])
            for f in fs:
                if f not in ids:
                    errores.append(f"{donde}: fuente {f} sin entrada en 'fuentes'")
                elif fuente_de[f].get("releida", "") < o["releida"]:
                    errores.append(f"{donde}: la fuente {f} no se reabrió el {o['releida']} (falta «releida», R5)")
            if o["etiqueta"] == "V" and not any(etiqueta_de.get(f) == "V" for f in fs):
                errores.append(f"{donde}: dato [V] sin ninguna fuente [V]")
    for clave, donde in claves.items():
        if len(donde) > 1:
            errores.append(f"clave {clave} repetida ({', '.join(donde)})")
    por_id = {s["id"]: s for s in doc["paradas_1_200"]}
    for sid, requeridas in CANDIDATOS_5_1_BIS.items():
        otros = por_id.get(sid, {}).get("otros_datos")
        tiene = {o.get("clave") for o in otros if isinstance(o, dict)} if isinstance(otros, list) else set()
        for c in requeridas:
            if c not in tiene:
                errores.append(f"parada {sid}: falta el candidato de §5.1 bis {c} en «otros_datos»")


def autoprueba_marcadores():
    """Casos fijos de revisar_texto con el plural del contrato 32 (M2.7f y FIXI): el positivo no da errores y mide la
    forma más larga con su cifra; los negativos sí los dan: el plural sin su valor en «args» y el plural con un valor
    que no es una cifra («1200» en texto o True). Devuelve los fallos de la prueba."""
    plural = "Faltan {n:plural:{} nivel|{} niveles} para el lago."
    fallos = []
    positivo = []
    revisar_texto("autoprueba positiva", plural, {"n": 1200}, positivo)
    if positivo:
        fallos.append(f"autoprueba: el plural con su valor en args da errores ({'; '.join(positivo)})")
    if texto_mostrado(plural, {"n": 1200}) != "Faltan 1.200 niveles para el lago.":
        fallos.append(f"autoprueba: el plural se mide como «{texto_mostrado(plural, {'n': 1200})}»")
    negativo = []
    revisar_texto("autoprueba negativa", plural, {}, negativo)
    if not any("no coinciden" in e for e in negativo):
        fallos.append("autoprueba: el plural sin su valor en args no da error")
    for valor in ("1200", True):
        sin_cifra = []
        revisar_texto("autoprueba sin cifra", plural, {"n": valor}, sin_cifra)
        if not any("necesita una cifra en args" in e for e in sin_cifra):
            fallos.append(f"autoprueba: el plural con {valor!r} en args no da «necesita una cifra en args»")
    return fallos


def autoprueba_otros_datos():
    """Caso fijo de revisar_otros_datos (FIXI): una parada candidata de §5.1 bis cuyas entradas de «otros_datos» no son
    objetos ("texto" y null) da «no es un objeto» por cada una y ningún otro error (los de candidatos que faltan se
    filtran), sin lanzar ninguna excepción. Devuelve los fallos de la prueba."""
    doc = {"fuentes": [], "paradas_1_200": [{"id": "belem", "otros_datos": ["texto", None]}]}
    errores = []
    try:
        revisar_otros_datos(doc, set(), {}, errores)
    except Exception as e:  # la prueba tiene que fallar con un mensaje, no romper --check
        return [f"autoprueba: «otros_datos» con entradas que no son objetos lanza {type(e).__name__}: {e}"]
    esperados = [f"parada belem: otros_datos[{i}]: no es un objeto" for i in range(2)]
    resto = [e for e in errores if "falta el candidato de §5.1 bis" not in e]
    if resto != esperados:
        return [f"autoprueba: «otros_datos» con entradas que no son objetos da {resto} y se esperaba {esperados}"]
    return []


# Paradas detalladas de 201-700 (fase F9 de la propuesta §13, con Q14-A; M3.7a-M3.7d): el mismo registro que las de
# 1-200, más «sitio» (el del resumen de paradas_201_1000 que la contiene) y «wikidata» (el elemento de su coordenada).
# Sin héroe (D-013, punto 2): el fondo es el kit del mundo con una pieza, kit:<pieza>. Sus ríos locales nuevos van en
# rios_locales_201_700, con «boca_en» (su río anfitrión), hasta que tools/river-map/geometry.py los dé de alta (M4.5a).
# Las mismas formas que fact_key en schemas/af-river-stops-1.schema.json (contrato 30; glosario §10; 08 §4.5), con el
# país en iso2 de dos letras y [0-9] en lugar de \d (que acepta dígitos Unicode), y sin «belen» (el «not» del esquema):
# también map.general.*, como el dato del agua negra de Anavilhanas (parada 6). Se aplica con fullmatch, como CLAVE_DATO.
# La unicidad de las claves, también frente a las de 1-200, la vigila revisar_otros_datos.
CLAVE_DATO_LLEGADA = re.compile(
    r"^map\.(stop\.[a-z0-9_]+\.dato_[0-9]{2}|country\.[a-z]{2}\.dato_[0-9]{2}|myth\.[a-z0-9_]+|general\.[a-z0-9_]+)$")
TIPOS_PARADA = ("gran_parada", "parada", "castillo")
DATO_DEL_200 = "map.myth.source"  # arrival_fact de la parada 20 (08 §4.5; D-023, punto 20)
KM_CASA_MAS = 29  # km_casa = km_desde_el_mar − 332 (Gurupá) + 361 (ramal del Pará); propuesta §2.2
# Pregunta 50 (a) (D-022): los cuatro candidatos de Pará de la tinaja (b.2 de su propuesta) van como «Otro dato» de la
# tarjeta de la parada del estreno (k = 32, niveles 321-330), con su lugar real (M3.7b; propuesta §5.4 ter).
CANDIDATOS_PREGUNTA_50 = {32: ("map.stop.sandoval.dato_04",   # las cuias del Bajo Amazonas (Aritapera), T1
                               "map.stop.sandoval.dato_05",   # el panelão de Quatipuru-Mirim, T4
                               "map.stop.sandoval.dato_06",   # el barro que enfría el agua (dato general), T7
                               "map.stop.sandoval.dato_07")}  # la alfarería de Icoaraci (1903), T5
# Puntos de paso que tienen que estar en el viaje de entrada de un mundo detallado, con su dato: el relevo de Rosa por el
# bufeo boliviano en los rápidos de Teotônio (Q18-A de D-013; D-023, punto 19, P-M8), en el viaje del 300, y la vuelta de
# Rosa en el mismo sitio, en el viaje del 560 (M3.7d).
PUNTOS_DE_PASO_REQUERIDOS = {7: ("teotonio",), 9: ("teotonio",)}
# Un punto de paso que sale en varios viajes es un solo elemento de passing[] en river_map_v1.json, con un solo fact
# (contrato 29), con un solo nombre (map.stop.<id>.name): en todos los viajes lleva la misma coordenada, el mismo nombre y
# el mismo dato (M3.7d).
CAMPOS_DEL_PUNTO_DE_PASO = ("lat", "lon", "nombre", "conf", "dato")


def revisar_paradas_201_700(doc, ids, etiqueta_de, errores):
    paradas = doc.get("paradas_201_700", [])
    if not isinstance(paradas, list):
        errores.append("paradas_201_700 no es una lista")
        return
    fuente_de = {f["id"]: f for f in doc["fuentes"]}
    resumen = {m["mundo"]: m for m in doc["paradas_201_1000"]}
    rios = {r["id"]: r for r in doc["rios"]}
    locales = doc.get("rios_locales_201_700", [])
    for r in locales:
        donde = f"río local {r.get('id')}"
        if r.get("id") in rios:
            errores.append(f"{donde}: repetido en 'rios'")
        if r.get("tipo") != "rio_local":
            errores.append(f"{donde}: tipo {r.get('tipo')!r} y se esperaba «rio_local»")
        if len(r.get("puntos", [])) < 2:
            errores.append(f"{donde}: menos de 2 puntos")
        for f in r.get("fuentes", []):
            if f not in ids:
                errores.append(f"{donde}: fuente {f} sin entrada en 'fuentes'")
    todos = dict(rios)
    todos.update({r["id"]: r for r in locales})
    for r in locales:
        if r.get("boca_en") not in todos or r.get("boca_en") == r.get("id"):
            errores.append(f"río local {r.get('id')}: «boca_en» {r.get('boca_en')!r} no es otro río")
    vistos = {s["id"] for s in doc["paradas_1_200"]}
    km_prev = {}  # mundo: (km_casa, ancla_rio) de la parada anterior
    km_rio = {}   # (mundo, ancla_rio): km_casa de la última parada de ese río en el mundo
    for i, s in enumerate(paradas):
        donde = f"parada {s.get('id')}"
        k = 20 + i
        if s.get("k") != k:
            errores.append(f"{donde}: k={s.get('k')} y se esperaba {k}")
        if s.get("id") in vistos or not re.fullmatch(r"[a-z0-9_]+", s.get("id") or "") or "belen" in (s.get("id") or ""):
            errores.append(f"{donde}: id repetido, fuera de [a-z0-9_]+ o con «Belén»")
        vistos.add(s.get("id"))
        a = 10 * k + 1
        if s.get("niveles") != [a, a + 9] or s.get("llegada_al_ganar") != a - 1:
            errores.append(f"{donde}: niveles {s.get('niveles')} o llegada {s.get('llegada_al_ganar')} y la k da {a}-{a + 9}")
        w = world_of(a)
        m = resumen.get(w, {})
        if s.get("mundo") != w or s.get("mundo_id") != m.get("mundo_id"):
            errores.append(f"{donde}: mundo {s.get('mundo')} ({s.get('mundo_id')}) y WorldTable dice {w} ({m.get('mundo_id')})")
        ultimo = (FIRST_LEVELS[w] - 1) if w < len(FIRST_LEVELS) else 1000
        tipo = "gran_parada" if a == FIRST_LEVELS[w - 1] else "castillo" if a + 9 == ultimo else "parada"
        if s.get("tipo") != tipo:
            errores.append(f"{donde}: tipo {s.get('tipo')!r} y se esperaba {tipo!r} (primera, última u otra del mundo)")
        if s.get("conf") not in ("V", "E") or not re.fullmatch(r"Q\d+", s.get("wikidata") or ""):
            errores.append(f"{donde}: coordenada sin conf V o E o sin su elemento de Wikidata")
        if s.get("ancla_rio") not in todos:
            errores.append(f"{donde}: ancla_rio {s.get('ancla_rio')!r} sin entrada en 'rios' ni en 'rios_locales_201_700'")
        for rl in s.get("rios_locales", []):
            if todos.get(rl, {}).get("tipo") != "rio_local":
                errores.append(f"{donde}: río local {rl} sin entrada de tipo «rio_local»")
        km, casa = s.get("km_desde_el_mar"), s.get("km_casa")
        baja = s.get("baja_por_confluencia")
        ancla = s.get("ancla_rio")
        if not isinstance(km, int) or casa != km + KM_CASA_MAS:
            errores.append(f"{donde}: km_casa {casa} y km_desde_el_mar {km} (+{KM_CASA_MAS})")
        else:
            # Dentro de un mundo los km no retroceden, salvo en una bajada declarada por una confluencia (M3.7c): la parada
            # lleva «baja_por_confluencia» (por dónde baja y por dónde sube) y está en otro río que la anterior, como el
            # «Beni abajo → Mamoré arriba» del mundo 8. Por un mismo río nunca retroceden, como en geometry.py (08 §4.5).
            previa = km_prev.get(w)
            if previa is not None and casa < previa[0]:
                if not isinstance(baja, str) or not baja.strip():
                    errores.append(f"{donde}: los km hasta casa retroceden dentro del mundo {w} ({casa} < {previa[0]})")
                elif ancla == previa[1]:
                    errores.append(f"{donde}: «baja_por_confluencia» sin cambiar de río ({ancla}): por un mismo río los km no retroceden")
            elif baja is not None:
                errores.append(f"{donde}: «baja_por_confluencia» sobrante (los km hasta casa no bajan)")
            mismo_rio = km_rio.get((w, ancla))
            if mismo_rio is not None and casa < mismo_rio:
                errores.append(f"{donde}: los km hasta casa retroceden por el {ancla} dentro del mundo {w} ({casa} < {mismo_rio})")
        if isinstance(casa, int):
            km_prev[w] = (casa, ancla)
            km_rio[(w, ancla)] = casa
        d = s.get("dato_del_nivel")
        if not isinstance(d, dict):
            errores.append(f"{donde}: sin dato de llegada")
        else:
            clave = d.get("clave")
            if not isinstance(clave, str) or not CLAVE_DATO_LLEGADA.fullmatch(clave):
                errores.append(f"{donde}: clave del dato {clave!r} fuera de map.stop.<lugar>.dato_NN, map.country.<iso2>.dato_NN, map.myth.<tema> o map.general.<id>")
            elif "belen" in clave:
                errores.append(f"{donde}: la clave del dato {clave!r} nombra «Belén» (contrato 30)")
            if k == 20 and d.get("clave") != DATO_DEL_200:
                errores.append(f"{donde}: el dato de llegada del 200 tiene que ser {DATO_DEL_200} (08 §4.5)")
            # Marcadores, cifras y «Belén» ya los mira revisar_otros_datos; aquí, solo la longitud.
            largo = len(texto_mostrado(d.get("texto", ""), d.get("args", {})))
            if largo > 90:
                errores.append(f"{donde}: dato de {largo} caracteres (> 90)")
            if d.get("etiqueta") not in ("V", "E"):
                errores.append(f"{donde}: dato con etiqueta {d.get('etiqueta')}; solo [V] o [E]")
            fs = re.split(r",\s*", d.get("fuente", ""))
            for f in fs:
                if f not in ids:
                    errores.append(f"{donde}: fuente {f} sin entrada en 'fuentes'")
                elif not FECHA.match(d.get("releida", "")) or fuente_de[f].get("releida", "") < d["releida"]:
                    errores.append(f"{donde}: la fuente {f} del dato no se reabrió en su «releida» (R5)")
            if d.get("etiqueta") == "V" and not any(etiqueta_de.get(f) == "V" for f in fs):
                errores.append(f"{donde}: dato [V] sin ninguna fuente [V]")
        fondo = s.get("fondo", {})
        if (fondo.get("kit") != f"bg_kit_w{w:02d}" or not str(fondo.get("heroe", "")).startswith("kit:")
                or fondo.get("tipo") != "kit_y_rotulo"):
            errores.append(f"{donde}: fondo distinto de bg_kit_w{w:02d} con kit:<pieza> (sin héroe en 201-700, Q14-A)")
        q = s.get("pregunta_del_mundo")
        if q:
            responde = [p for p in paradas if p.get("mundo") == w and p.get("llegada_al_ganar") == q.get("se_responde_en")]
            if len(q.get("texto", "")) > 50 or not str(m.get("pregunta_del_mundo", "")).startswith(q.get("texto", "?")):
                errores.append(f"{donde}: pregunta de más de 50 caracteres o distinta de la del resumen del mundo {w}")
            if not responde or q.get("se_responde_en") <= s.get("llegada_al_ganar", 0):
                errores.append(f"{donde}: la pregunta no se responde en una llegada posterior del mismo mundo")
        b = s.get("bandera")
        if b and not any(c.get("pais") == b and c.get("nivel") == s.get("llegada_al_ganar") for c in doc["cruces_de_pais"]):
            errores.append(f"{donde}: bandera {b} sin cruce de país en el {s.get('llegada_al_ganar')}")
        if not isinstance(s.get("asesor"), bool) or not s.get("temporada") or not isinstance(s.get("coincidencias"), list):
            errores.append(f"{donde}: faltan asesor (true/false), temporada o coincidencias")
        cadenas = json.dumps(s, ensure_ascii=False)
        if "[NV]" in cadenas or "belen" in sin_tildes(cadenas):
            errores.append(f"{donde}: lleva un [NV] o nombra «Belén» (ningún [NV] en las paradas de 201-700)")
    # Cada mundo con paradas detalladas, entero y con los sitios de su resumen, en su orden y con sus paradas.
    for w in sorted({s.get("mundo") for s in paradas}):
        m = resumen.get(w)
        if m is None:
            continue
        orden = []
        for s in paradas:
            if s.get("mundo") == w:
                if not orden or orden[-1][0] != s.get("sitio"):
                    orden.append([s.get("sitio"), 0])
                orden[-1][1] += 1
        esperado = [[x["nombre"], x["paradas"]] for x in m["sitios"]]
        if orden != esperado:
            errores.append(f"mundo {w}: las paradas detalladas dan {orden} y el resumen {esperado}")
        preguntas = [s for s in paradas if s.get("mundo") == w and s.get("pregunta_del_mundo")]
        if len(preguntas) != 1:
            errores.append(f"mundo {w}: {len(preguntas)} paradas con la pregunta del mundo y debería ser 1")
        # El resumen de un mundo detallado ya no deja sitios sin coordenada ni con [NV] (M3.7b).
        for x in m["sitios"]:
            if not all(isinstance(x.get(c), (int, float)) for c in ("lat", "lon")) or "[NV]" in json.dumps(x, ensure_ascii=False):
                errores.append(f"mundo {w}: el sitio «{x.get('nombre')}» del resumen sin coordenada o con un [NV]")
        # Puntos de paso obligatorios del viaje de entrada (Q18-A: teotonio en el del 300), con su coordenada y su dato.
        pasos = {p.get("id"): p for p in m.get("puntos_de_paso_del_viaje", []) if isinstance(p, dict)}
        for pid in PUNTOS_DE_PASO_REQUERIDOS.get(w, ()):
            p = pasos.get(pid)
            if p is None:
                errores.append(f"mundo {w}: falta el punto de paso «{pid}» en el viaje de entrada (Q18-A; D-023, punto 19)")
                continue
            dato = str(p.get("dato", ""))
            clave = dato.split(" ")[0]
            citados = list(ids_citados(dato))
            if (p.get("conf") not in ("V", "E") or not all(isinstance(p.get(c), (int, float)) for c in ("lat", "lon"))
                    or not CLAVE_DATO.fullmatch(clave) or not citados):
                errores.append(f"mundo {w}: el punto de paso «{pid}» sin coordenada V o E o sin dato «map.stop.<lugar>.dato_NN [V|E fuente]: …»")
            # El dato irá a facts.csv como los de las paradas (passing[] en M4.5a): las mismas reglas de texto (≤ 90
            # caracteres, Q8-A y «Belén») y fuentes con entrada en 'fuentes' y reabiertas (R5).
            texto = re.search(r"«(.*)»", dato)
            if texto is None:
                errores.append(f"mundo {w}: el punto de paso «{pid}» sin el texto de su dato entre «»")
            else:
                revisar_texto(f"mundo {w}: el punto de paso «{pid}» ({clave})", texto.group(1), {}, errores)
            for f in citados:
                if f not in ids:
                    errores.append(f"mundo {w}: el punto de paso «{pid}» cita la fuente {f} sin entrada en 'fuentes'")
                elif not FECHA.match(fuente_de[f].get("releida", "")):
                    errores.append(f"mundo {w}: el punto de paso «{pid}» cita la fuente {f} sin «releida» (R5)")
        # Candidatos que tienen que ir en el «Otro dato» de una parada (pregunta 50 (a) en k = 32).
        for s in paradas:
            if s.get("mundo") != w:
                continue
            tiene = {o.get("clave") for o in s.get("otros_datos") or [] if isinstance(o, dict)}
            for c in CANDIDATOS_PREGUNTA_50.get(s.get("k"), ()):
                if c not in tiene:
                    errores.append(f"parada {s.get('id')}: falta el candidato de la pregunta 50 (a) {c} en «otros_datos»")
    # Un punto de paso con id que sale en el viaje de entrada de varios mundos (teotonio en el 300 y en el 560) es el
    # mismo elemento de passing[]: misma coordenada, mismo nombre, misma confianza y mismo dato en todos (M3.7d).
    primero = {}
    for m in doc["paradas_201_1000"]:
        for p in m.get("puntos_de_paso_del_viaje", []):
            if not isinstance(p, dict) or not p.get("id"):
                continue
            if p["id"] not in primero:
                primero[p["id"]] = (m["mundo"], p)
                continue
            w0, p0 = primero[p["id"]]
            distintos = [c for c in CAMPOS_DEL_PUNTO_DE_PASO if p.get(c) != p0.get(c)]
            if distintos:
                errores.append(f"mundo {m['mundo']}: el punto de paso «{p['id']}» no es igual que en el mundo {w0} "
                               f"({', '.join(distintos)} distintos): es un solo elemento de passing[]")
    # Las banderas que el resumen pone en un sitio de un mundo detallado tienen que estar en su primera parada, y las del
    # viaje de entrada de un mundo detallado, en la parada a la que llega ese viaje (la primera del mundo; desde M3.7b).
    detallados = {s.get("mundo") for s in paradas}
    for c in doc["cruces_de_pais"]:
        ll = c.get("llega_en", {})
        if ll.get("mundo") in detallados or ll.get("viaje_de_entrada") in detallados:
            hay = [s for s in paradas if s.get("bandera") == c.get("pais") and s.get("llegada_al_ganar") == c.get("nivel")]
            if not hay:
                errores.append(f"cruce {c['nombre']}: ninguna parada de 201-700 lleva su bandera en el {c.get('nivel')}")


def autoprueba_paradas_201_700(doc):
    """Casos fijos de revisar_paradas_201_700 (M3.7a). Con las paradas de verdad la revisión no da ningún error; cada
    caso cambia una copia y tiene que dar un error nuevo (que no estaba en la base) con el texto esperado y a nombre de
    la parada, el mundo o el cruce cambiado; el caso con un dato map.general.* no tiene que dar ninguno. Las claves de
    llegada fuera del fact_key del contrato 30 (país de más de dos letras, dígitos Unicode, «belen», sin clave) dan
    error, y una clave de 1-200 repetida en un «Otro dato» de 201-700 da «repetida» en revisar_otros_datos. Las reglas
    de la bandera, los km, el orden de los sitios y la unicidad de las claves las reutilizan M3.7b-M3.7d. Desde M3.7b,
    también dan error un candidato de la pregunta 50 (a) que falta en k = 32, el viaje del 300 sin el punto de paso
    teotonio o con su dato sin etiqueta, sin longitud, con un texto de más de 90 caracteres o con una fuente que no está
    en 'fuentes' (Q18-A) y un sitio del resumen de un mundo detallado sin coordenada; y la bandera
    de un viaje de entrada (BO en el 300) se exige en la primera parada de su mundo. Desde M3.7c, los km que bajan dentro
    de un mundo solo valen en una parada con «baja_por_confluencia» y en otro río que la anterior, y nunca por debajo de la
    última parada de su río: dan error la bajada sin declarar, la declarada en el mismo río, la que baja de la última
    parada de su río y una declaración sobrante. Desde M3.7d, teotonio también tiene que estar en el viaje del 560 (los
    casos del punto de paso se repiten para el mundo 9), y un punto de paso que sale en dos viajes da error si en el
    segundo cambia la longitud o el dato. Devuelve los fallos de la prueba."""
    paradas = doc.get("paradas_201_700", [])
    if not paradas:
        return []
    ids = {f["id"] for f in doc["fuentes"]}
    etiqueta_de = {f["id"]: f.get("etiqueta") for f in doc["fuentes"]}
    base = []
    revisar_paradas_201_700(doc, ids, etiqueta_de, base)
    if base:
        return [f"autoprueba: las paradas de 201-700 sin cambiar ya dan {len(base)} errores; no se pueden probar los casos"]
    por_id = {s.get("id"): i for i, s in enumerate(paradas)}
    con_bandera = [s for s in paradas if s.get("bandera")]
    fallos = []

    def caso(nombre, id_parada, cambia, esperado, autor):
        """Cambia la parada id_parada de una copia (o el documento, si id_parada es None) y comprueba los errores
        nuevos: esperado = None exige que no haya ninguno; si no, uno que contenga esperado y empiece por autor."""
        if id_parada is not None and id_parada not in por_id:
            return  # la parada del caso aún no existe (otra tanda de M3.7)
        copia = json.loads(json.dumps(doc))
        cambia(copia["paradas_201_700"][por_id[id_parada]] if id_parada is not None else copia)
        errores = []
        revisar_paradas_201_700(copia, ids, etiqueta_de, errores)
        nuevos = [e for e in errores if e not in base]
        if esperado is None:
            if nuevos:
                fallos.append(f"autoprueba: {nombre} da errores ({nuevos}) y no debería")
        elif not any(esperado in e and e.startswith(autor) for e in nuevos):
            fallos.append(f"autoprueba: {nombre} no da «{esperado}» a nombre de «{autor}» ({nuevos})")

    primera = paradas[0]["id"]
    caso("el dato del 200 con otra clave", primera,
         lambda p: p["dato_del_nivel"].update(clave="map.stop.mazan.dato_09"),
         "tiene que ser map.myth.source", f"parada {primera}:")
    caso("un [NV] en una nota", primera, lambda p: p.update(notas="punto aproximado [NV]"),
         "lleva un [NV]", f"parada {primera}:")
    caso("un héroe pintado", primera, lambda p: p["fondo"].update(heroe="bg_hero_mazan", tipo="pintado"),
         "sin héroe en 201-700", f"parada {primera}:")
    if len(paradas) > 1:
        segunda = paradas[1]["id"]
        caso("un dato de llegada map.general.* (contrato 30)", segunda,
             lambda p: p["dato_del_nivel"].update(clave="map.general.dato_prueba"), None, "")
        fuera = "fuera de map.stop.<lugar>.dato_NN"
        for nombre, clave in (("el país en tres letras", "map.country.ecu.dato_01"),
                              ("el país con su nombre entero", "map.country.ecuador.dato_01"),
                              ("el país con un guion bajo", "map.country.e_c.dato_01"),
                              ("dígitos que no son ASCII", "map.stop.mazan.dato_\u0660\u0661"),
                              ("un salto de línea al final", "map.stop.mazan.dato_01\n")):
            caso(f"un dato de llegada con {nombre} ({clave!r})", segunda,
                 lambda p, c=clave: p["dato_del_nivel"].update(clave=c), fuera, f"parada {segunda}:")
        caso("un dato de llegada sin clave", segunda, lambda p: p["dato_del_nivel"].pop("clave"),
             "clave del dato None fuera de", f"parada {segunda}:")
        caso("un dato de llegada con «belen» en la clave", segunda,
             lambda p: p["dato_del_nivel"].update(clave="map.stop.belen.dato_01"), "nombra «Belén» (contrato 30)",
             f"parada {segunda}:")
        caso("km hasta casa que retroceden", segunda,
             lambda p: p.update(km_desde_el_mar=p["km_desde_el_mar"] - 1000, km_casa=p["km_casa"] - 1000),
             "retroceden", f"parada {segunda}:")
        mundo = paradas[1].get("mundo")
        caso("sitios fuera de orden", segunda, lambda p: p.update(sitio=paradas[-1].get("sitio")),
             "las paradas detalladas dan", f"mundo {mundo}:")
        caso("una «baja_por_confluencia» sobrante", segunda, lambda p: p.update(baja_por_confluencia="prueba"),
             "sobrante", f"parada {segunda}:")
    # M3.7c: las bajadas declaradas por una confluencia (Beni abajo → Mamoré arriba en el mundo 8). Sin la declaración, los
    # km que bajan dan error; con ella, también si la parada sigue en el mismo río que la anterior o si baja por debajo de
    # la última parada de su río en el mundo.
    bajas = [s for s in paradas if s.get("baja_por_confluencia")]
    for s in bajas[:1]:
        anterior = paradas[por_id[s["id"]] - 1]
        caso(f"la bajada de {s['id']} sin «baja_por_confluencia»", s["id"], lambda p: p.pop("baja_por_confluencia"),
             "retroceden dentro del mundo", f"parada {s['id']}:")
        caso(f"la bajada de {s['id']} por el mismo río que la parada anterior", s["id"],
             lambda p, a=anterior.get("ancla_rio"): p.update(ancla_rio=a), "sin cambiar de río", f"parada {s['id']}:")
    for s in bajas:
        i = por_id[s["id"]]
        antes = [p for p in paradas[:i] if p.get("mundo") == s.get("mundo") and p.get("ancla_rio") == s.get("ancla_rio")]
        if antes:
            menos = antes[-1]["km_casa"] - 100
            caso(f"la bajada de {s['id']} por debajo de {antes[-1]['id']}, en su mismo río", s["id"],
                 lambda p, m=menos: p.update(km_desde_el_mar=m - KM_CASA_MAS, km_casa=m),
                 f"retroceden por el {s.get('ancla_rio')}", f"parada {s['id']}:")
    for s in con_bandera:
        cruce = next((c for c in doc["cruces_de_pais"]
                      if c.get("pais") == s["bandera"] and c.get("nivel") == s.get("llegada_al_ganar")), None)
        if cruce is not None:
            caso(f"sin la bandera {s['bandera']} en el {s.get('llegada_al_ganar')}", s["id"],
                 lambda p: p.pop("bandera"), "ninguna parada de 201-700 lleva su bandera", f"cruce {cruce['nombre']}:")
    otra = next((s for s in paradas if not s.get("bandera")), None)
    if con_bandera and otra is not None:
        pais = con_bandera[0]["bandera"]
        caso(f"una bandera {pais} sobrante", otra["id"], lambda p: p.update(bandera=pais),
             "sin cruce de país", f"parada {otra['id']}:")
    # M3.7b: los candidatos de la pregunta 50 (a) en k = 32, el punto de paso teotonio del viaje del 300 (Q18-A) y los
    # sitios del resumen de un mundo detallado, con coordenada y sin [NV].
    for k, requeridas in CANDIDATOS_PREGUNTA_50.items():
        s = next((p for p in paradas if p.get("k") == k), None)
        if s is not None:
            quita = requeridas[0]
            caso(f"sin el candidato {quita} de la pregunta 50 (a)", s["id"],
                 lambda p, q=quita: p.update(otros_datos=[o for o in p["otros_datos"] if o.get("clave") != q]),
                 f"falta el candidato de la pregunta 50 (a) {quita}", f"parada {s['id']}:")
    mundos_detallados = {s.get("mundo") for s in paradas}
    for w, pids in PUNTOS_DE_PASO_REQUERIDOS.items():
        if w not in mundos_detallados:
            continue
        i = next(j for j, m in enumerate(doc["paradas_201_1000"]) if m["mundo"] == w)
        for pid in pids:
            caso(f"el viaje del mundo {w} sin el punto de paso {pid}", None,
                 lambda d_, i=i, pid=pid: d_["paradas_201_1000"][i].update(puntos_de_paso_del_viaje=[
                     p for p in d_["paradas_201_1000"][i]["puntos_de_paso_del_viaje"] if p.get("id") != pid]),
                 f"falta el punto de paso «{pid}»", f"mundo {w}:")
            caso(f"el punto de paso {pid} sin la etiqueta de su dato", None,
                 lambda d_, i=i, pid=pid: [p.update(dato=p["dato"].split(" [")[0]) for p in
                                           d_["paradas_201_1000"][i]["puntos_de_paso_del_viaje"] if p.get("id") == pid],
                 f"el punto de paso «{pid}» sin coordenada V o E o sin dato", f"mundo {w}:")
            caso(f"el punto de paso {pid} sin longitud", None,
                 lambda d_, i=i, pid=pid: [p.pop("lon") for p in
                                           d_["paradas_201_1000"][i]["puntos_de_paso_del_viaje"] if p.get("id") == pid],
                 f"el punto de paso «{pid}» sin coordenada V o E o sin dato", f"mundo {w}:")
            caso(f"el punto de paso {pid} con un texto de más de 90 caracteres", None,
                 lambda d_, i=i, pid=pid: [p.update(dato=p["dato"].replace("»", " Y sigue y sigue y sigue.»")) for p in
                                           d_["paradas_201_1000"][i]["puntos_de_paso_del_viaje"] if p.get("id") == pid],
                 "caracteres (> 90)", f"mundo {w}: el punto de paso «{pid}»")
            caso(f"el punto de paso {pid} con una fuente sin entrada en 'fuentes'", None,
                 lambda d_, i=i, pid=pid: [p.update(dato=p["dato"].replace("]:", ", fuente_inventada]:", 1)) for p in
                                           d_["paradas_201_1000"][i]["puntos_de_paso_del_viaje"] if p.get("id") == pid],
                 "cita la fuente fuente_inventada sin entrada en 'fuentes'", f"mundo {w}:")
        caso(f"un sitio del resumen del mundo {w} sin coordenada", None,
             lambda d_, i=i: [d_["paradas_201_1000"][i]["sitios"][-1].pop(c) for c in ("lat", "lon", "xy")],
             "sin coordenada o con un [NV]", f"mundo {w}:")
    # M3.7d: un punto de paso que sale en dos viajes (teotonio en el 300 y en el 560) tiene que ser igual en los dos; en
    # la copia, el del mundo posterior cambia de longitud, de nombre o de dato.
    vistos_pp = {}
    for i, m in enumerate(doc["paradas_201_1000"]):
        for p in m.get("puntos_de_paso_del_viaje", []):
            pid = p.get("id") if isinstance(p, dict) else None
            if not pid:
                continue
            if pid not in vistos_pp:
                vistos_pp[pid] = m["mundo"]
                continue
            for nombre, cambia in (("con otra longitud", lambda q: q.update(lon=q["lon"] + 0.1)),
                                   ("con otro nombre", lambda q: q.update(nombre=str(q.get("nombre", "")) + " (bis)")),
                                   ("con otro dato", lambda q: q.update(dato=q["dato"].replace("»", " Otra vez.»")))):
                caso(f"el punto de paso {pid} del mundo {m['mundo']} {nombre} que en el mundo {vistos_pp[pid]}", None,
                     lambda d_, i=i, pid=pid, cambia=cambia: [cambia(q) for q in
                                                              d_["paradas_201_1000"][i]["puntos_de_paso_del_viaje"]
                                                              if q.get("id") == pid],
                     f"no es igual que en el mundo {vistos_pp[pid]}", f"mundo {m['mundo']}:")
    # Unicidad entre 1-200 y 201-700 (revisar_otros_datos, desde M3.7a): un «Otro dato» de 1-200 copiado en la primera
    # parada de 201-700 da «clave … repetida» con las dos paradas; y una parada sin clave en su dato de llegada no la
    # rompe (el error legible lo da revisar_paradas_201_700, caso de arriba).
    base_otros = []
    revisar_otros_datos(doc, ids, etiqueta_de, base_otros)
    otro_1_200 = next(((s["id"], o) for s in doc["paradas_1_200"] for o in (s.get("otros_datos") or [])
                       if isinstance(o, dict) and o.get("clave")), None)
    if otro_1_200 is not None:
        origen, otro = otro_1_200
        copia = json.loads(json.dumps(doc))
        copia["paradas_201_700"][0]["otros_datos"].append(json.loads(json.dumps(otro)))
        errores = []
        revisar_otros_datos(copia, ids, etiqueta_de, errores)
        nuevos = [e for e in errores if e not in base_otros]
        esperado = f"clave {otro['clave']} repetida ("
        if not any(e.startswith(esperado) and origen in e and primera in e for e in nuevos):
            fallos.append(f"autoprueba: un «Otro dato» de {origen} repetido en {primera} no da «{esperado}…» ({nuevos})")
    if len(paradas) > 1:
        copia = json.loads(json.dumps(doc))
        copia["paradas_201_700"][1]["dato_del_nivel"].pop("clave", None)
        try:
            revisar_otros_datos(copia, ids, etiqueta_de, [])
        except Exception as e:  # la prueba tiene que fallar con un mensaje, no romper --check
            fallos.append(f"autoprueba: un dato de llegada sin clave rompe revisar_otros_datos ({type(e).__name__}: {e})")
    return fallos


def primer_nivel_del_sitio(mundo, nombre):
    """Primer nivel de un sitio de 201-1.000: las paradas de cada mundo cubren 10 niveles cada una, en orden."""
    nivel = mundo["niveles"][0]
    for s in mundo["sitios"]:
        if s["nombre"].startswith(nombre):
            return nivel
        nivel += 10 * s["paradas"]
    return None


def actualizar_xy(doc, pr):
    doc["proyeccion"]["aspecto_alto_sobre_ancho"] = round(pr.aspect, 5)
    for p in iter_points(doc):
        x, y = pr.norm(p["lat"], p["lon"])
        p["xy"] = [round(x, 5), round(y, 5)]


def check(doc, pr):
    errores = []
    if abs((doc["proyeccion"].get("aspecto_alto_sobre_ancho") or 0) - round(pr.aspect, 5)) > 1e-5:
        errores.append("aspecto_alto_sobre_ancho no coincide con la caja")
    n = 0
    for p in iter_points(doc):
        n += 1
        x, y = pr.norm(p["lat"], p["lon"])
        xy = p.get("xy")
        if not xy or abs(xy[0] - x) > 1e-4 or abs(xy[1] - y) > 1e-4:
            errores.append(f"xy desfasado o ausente en {p.get('nombre') or p.get('id')}")
        if not (-0.001 <= x <= 1.001 and -0.001 <= y <= 1.001):
            errores.append(f"fuera de la caja: {p.get('nombre') or p.get('id')} ({p['lat']}, {p['lon']})")
    # Ningún tramo del cauce puede medir menos que la línea recta entre sus extremos.
    main = next(r for r in doc["rios"] if r["tipo"] == "cauce")
    pts = main["puntos"]
    for a, b in zip(pts, pts[1:]):
        dk = b["km_desde_nacimiento"] - a["km_desde_nacimiento"]
        g = hav((a["lat"], a["lon"]), (b["lat"], b["lon"]))
        if dk + 0.5 < g:
            errores.append(f"cauce: {a['nombre']} → {b['nombre']} mide {dk} km y la línea recta {g:.1f} km")
        if a["km_desde_el_mar"] + a["km_desde_nacimiento"] != pts[-1]["km_desde_nacimiento"]:
            errores.append(f"cauce: km_desde_el_mar no cuadra en {a['nombre']}")
    for r in doc["rios"]:
        if len(r["puntos"]) < 2 and not r.get("solo_boca"):
            errores.append(f"río {r['id']}: menos de 2 puntos y sin «solo_boca»")
    paradas = doc["paradas_1_200"]
    km_prev = -1
    for i, s in enumerate(paradas):
        if s["k"] != i:
            errores.append(f"parada {s['id']}: k={s['k']} y se esperaba {i}")
        if s["niveles"] != [10 * i + 1, 10 * i + 10]:
            errores.append(f"parada {s['id']}: niveles {s['niveles']}")
        if s["llegada_al_ganar"] != (None if i == 0 else 10 * i):
            errores.append(f"parada {s['id']}: llegada_al_ganar {s['llegada_al_ganar']}")
        if s["mundo"] != world_of(s["niveles"][0]):
            errores.append(f"parada {s['id']}: mundo {s['mundo']} y WorldTable dice {world_of(s['niveles'][0])}")
        if s["km_casa"] < km_prev:
            errores.append(f"parada {s['id']}: los km hasta casa retroceden ({s['km_casa']} < {km_prev})")
        km_prev = s["km_casa"]
        d = s.get("dato_del_nivel")
        largo = len(texto_mostrado(d["texto"], d.get("args", {}))) if d else 0
        if largo > 90:
            errores.append(f"parada {s['id']}: dato de {largo} caracteres (> 90)")
        if not s.get("fondo", {}).get("heroe"):
            errores.append(f"parada {s['id']}: sin fondo")
        q = s.get("pregunta_del_mundo")
        if q and len(q["texto"]) > 50:
            errores.append(f"parada {s['id']}: pregunta de {len(q['texto'])} caracteres (> 50)")
    firsts = {s["niveles"][0] for s in paradas}
    for f in FIRST_LEVELS[:5]:
        if f not in firsts:
            errores.append(f"ningún tramo empieza en el primer nivel del mundo ({f})")
    total = sum(m["paradas"] for m in doc["paradas_201_1000"])
    if total != 80:
        errores.append(f"201-1.000 suma {total} paradas y deberían ser 80")
    for m in doc["paradas_201_1000"]:
        a, b = m["niveles"]
        if (b - a + 1) != 10 * m["paradas"]:
            errores.append(f"mundo {m['mundo']}: {m['paradas']} paradas para {b - a + 1} niveles")
        if sum(s["paradas"] for s in m["sitios"]) != m["paradas"]:
            errores.append(f"mundo {m['mundo']}: los sitios no suman {m['paradas']} paradas")
        if FIRST_LEVELS[m["mundo"] - 1] != a:
            errores.append(f"mundo {m['mundo']}: empieza en {a} y WorldTable dice {FIRST_LEVELS[m['mundo'] - 1]}")
    ids = [f["id"] for f in doc["fuentes"]]
    for f in {x for x in ids if ids.count(x) > 1}:
        errores.append(f"fuente {f} repetida")
    etiqueta_de = {f["id"]: f.get("etiqueta") for f in doc["fuentes"]}
    ids = set(ids)
    for f in doc["fuentes"]:
        if not f.get("url"):
            errores.append(f"fuente {f['id']} sin url")
        if f.get("etiqueta") not in ("V", "E"):
            errores.append(f"fuente {f['id']} sin etiqueta V o E")
    for r in doc["rios"]:
        for f in r["fuentes"]:
            if f not in ids:
                errores.append(f"río {r['id']}: fuente {f} sin entrada en 'fuentes'")
    # Dato de llegada: sus fuentes existen y un [V] se apoya al menos en una fuente [V] (una página de Wikipedia, un
    # portal turístico o la prensa solos dan [E]: cabecera de la propuesta).
    for s in paradas:
        d = s.get("dato_del_nivel")
        if d:
            fs = re.split(r",\s*", d["fuente"])
            for f in fs:
                if f not in ids:
                    errores.append(f"parada {s['id']}: fuente {f} sin entrada en 'fuentes'")
            if d["etiqueta"] == "V" and not any(etiqueta_de.get(f) == "V" for f in fs):
                errores.append(f"parada {s['id']}: dato [V] sin ninguna fuente [V]")
    # «Otro dato» de cada tarjeta (M2.7): campos, claves únicas, texto, etiqueta, fuentes reabiertas y candidatos.
    revisar_otros_datos(doc, ids, etiqueta_de, errores)
    # Paradas detalladas de 201-700 (F9; M3.7a): registro completo, dato, fondo sin héroe, sitios del resumen y banderas.
    revisar_paradas_201_700(doc, ids, etiqueta_de, errores)
    errores.extend(autoprueba_marcadores())
    errores.extend(autoprueba_otros_datos())
    errores.extend(autoprueba_paradas_201_700(doc))
    # Toda etiqueta «[V id]» / «[E id]» de cualquier texto del JSON (datos, notas, preguntas, cuidado, miradores, puntos
    # de paso, ríos, cruces…) tiene que llevar a una entrada de «fuentes» con su URL.
    def textos(o, ruta):
        if isinstance(o, dict):
            for k, v in o.items():
                if not (ruta == "" and k == "fuentes"):
                    yield from textos(v, f"{ruta}/{k}")
        elif isinstance(o, list):
            for i, v in enumerate(o):
                yield from textos(v, f"{ruta}[{i}]")
        elif isinstance(o, str):
            yield ruta, o
    for donde, t in textos(doc, ""):
        for f in ids_citados(t):
            if f not in ids:
                errores.append(f"{donde}: la etiqueta cita «{f}», que no está en 'fuentes'")
    # Ríos locales de las paradas con zoom (vista Mundo): tienen que existir y ser «rio_local».
    tipo_de = {r["id"]: r["tipo"] for r in doc["rios"]}
    for s in paradas:
        for rl in s.get("rios_locales", []):
            if rl not in tipo_de:
                errores.append(f"parada {s['id']}: río local {rl} sin entrada en 'rios'")
            elif tipo_de[rl] != "rio_local":
                errores.append(f"parada {s['id']}: {rl} no es de tipo «rio_local»")
        if s["k"] in (11, 13, 15, 16, 18) and not s.get("rios_locales"):
            errores.append(f"parada {s['id']}: falta su río local")
    # Llenado de la muestra: los afluentes de paso, enteros.
    lim = doc["estado_de_muestra_nivel_200"]["afluentes_llenos_hasta_km_desde_la_boca"]
    for r in doc["rios"]:
        if r["tipo"] == "afluente_paso" and lim.get(r["id"]) != ENTERO:
            errores.append(f"río {r['id']}: afluente de paso que no se llena entero en la muestra")
    # Cruces de país: el nivel sale de las paradas (10 niveles cada una), no se escribe a mano.
    mundos = {m["mundo"]: m for m in doc["paradas_201_1000"]}
    for c in doc["cruces_de_pais"]:
        ll = c.get("llega_en", {})
        if "parada_1_200" in ll:
            k = ll["parada_1_200"]
            esperado = 10 if k == 0 else 10 * k
        elif "viaje_de_entrada" in ll:
            esperado = FIRST_LEVELS[ll["viaje_de_entrada"] - 1] - 1
        elif "mundo" in ll:
            primero = primer_nivel_del_sitio(mundos[ll["mundo"]], ll["sitio"])
            esperado = None if primero is None else primero - 1
        else:
            esperado = None
        if esperado is None:
            errores.append(f"cruce {c['nombre']}: sin «llega_en» válido")
        elif c["nivel"] != esperado:
            errores.append(f"cruce {c['nombre']}: nivel {c['nivel']} y las paradas dan {esperado}")
    return n, errores


# ------------------------------------------------------------------ dibujo
class Svg:
    def __init__(self):
        self.parts = []

    def add(self, s):
        self.parts.append(s)

    def text(self, x, y, s, size=13, fill=None, anchor="start", weight="700", family=FONT, italic=False,
             opacity=1.0, halo=None, spacing=None):
        fill = fill or C["crema"]
        st = ' font-style="italic"' if italic else ""
        sp = f' letter-spacing="{spacing}"' if spacing else ""
        op = f' opacity="{opacity}"' if opacity != 1.0 else ""
        if halo:
            self.add(f'<text x="{fmt(x)}" y="{fmt(y)}" font-family="{family}" font-size="{size}" font-weight="{weight}"'
                     f' text-anchor="{anchor}"{st}{sp}{op} fill="none" stroke="{halo}" stroke-width="3.2" stroke-linejoin="round">{esc(s)}</text>')
        self.add(f'<text x="{fmt(x)}" y="{fmt(y)}" font-family="{family}" font-size="{size}" font-weight="{weight}"'
                 f' text-anchor="{anchor}"{st}{sp}{op} fill="{fill}">{esc(s)}</text>')

    def plaque(self, x, y, s, size=12.5, anchor="start", fill=None, text_fill=None, pad=5, opacity=0.93, weight="700"):
        w = len(s) * size * 0.56 + 2 * pad
        h = size + 2 * pad - 1
        if anchor == "end":
            rx = x - w
        elif anchor == "middle":
            rx = x - w / 2
        else:
            rx = x
        fill = fill or C["madera_oscura"]
        self.add(f'<rect x="{fmt(rx)}" y="{fmt(y - h + pad)}" width="{fmt(w)}" height="{fmt(h)}" rx="4" fill="{fill}"'
                 f' opacity="{opacity}" stroke="{C["marco_bisel"]}" stroke-opacity="0.55" stroke-width="0.8"/>')
        self.text(rx + pad, y, s, size=size, fill=text_fill, weight=weight, family=TEXT)
        return rx, y - h + pad, w, h

    def svg(self, w, h):
        head = (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w} {h}" width="{w}" height="{h}" '
                f'role="img" aria-labelledby="titulo desc">')
        return head + "\n" + "\n".join(self.parts) + "\n</svg>\n"


def defs(svg, map_rect, insets):
    x, y, w, h = map_rect
    clips = "".join(f'<clipPath id="clip{name}"><rect x="{fmt(v.x0)}" y="{fmt(v.y0)}" width="{fmt(v.w)}" height="{fmt(v.h)}"/></clipPath>'
                    for name, v in insets)
    svg.add(f"""<title id="titulo">Mapa del río: muestra con la forma real del Amazonas (nivel 200)</title>
<desc id="desc">Cuenca real del Amazonas girada 90 grados: Andes arriba, Atlántico abajo y norte a la derecha. El tramo remontado en los niveles 1-200 (mundos 1-5, de Belém a Pacaya-Samiria) está lleno de su agua real; el resto del río está en vidrio. Paradas numeradas, países sin fronteras dibujadas, la costa del Atlántico y la del Pacífico, el inicio, el nivel 200 y la meta en el nevado Mismi, una de las nacientes del Amazonas.</desc>
<defs>
<linearGradient id="gFondo" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{C['fondo_a']}"/><stop offset="1" stop-color="{C['fondo_b']}"/></linearGradient>
<linearGradient id="gTierra" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{C['selva_media']}"/><stop offset="0.55" stop-color="#2A8048"/><stop offset="1" stop-color="{C['hoja_media']}"/></linearGradient>
<linearGradient id="gMar" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="{C['mar_b']}"/><stop offset="1" stop-color="{C['mar_a']}"/></linearGradient>
<linearGradient id="gAndes" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{C['roca_a']}"/><stop offset="1" stop-color="{C['roca_b']}"/></linearGradient>
<linearGradient id="gMadera" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#8A5A43"/><stop offset="1" stop-color="{C['marco']}"/></linearGradient>
<radialGradient id="gLuz" cx="0.25" cy="0.15" r="0.9"><stop offset="0" stop-color="{C['destello']}" stop-opacity="0.22"/><stop offset="1" stop-color="{C['destello']}" stop-opacity="0"/></radialGradient>
<pattern id="pDosel" width="34" height="30" patternUnits="userSpaceOnUse">
 <circle cx="7" cy="8" r="6" fill="{C['selva_sombra']}" opacity="0.28"/><circle cx="24" cy="6" r="5" fill="{C['selva_luz']}" opacity="0.14"/>
 <circle cx="17" cy="21" r="7" fill="{C['selva_sombra']}" opacity="0.22"/><circle cx="31" cy="24" r="4" fill="{C['selva_luz']}" opacity="0.12"/>
 <circle cx="4" cy="26" r="3" fill="{C['selva_luz']}" opacity="0.10"/>
</pattern>
<pattern id="pOla" width="46" height="22" patternUnits="userSpaceOnUse">
 <path d="M2,12 q6,-6 12,0 t12,0" fill="none" stroke="{C['ola']}" stroke-width="1.4" opacity="0.35"/>
 <path d="M24,3 q5,-4 10,0" fill="none" stroke="{C['ola']}" stroke-width="1.1" opacity="0.25"/>
</pattern>
<clipPath id="clipMapa"><rect x="{fmt(x)}" y="{fmt(y)}" width="{fmt(w)}" height="{fmt(h)}"/></clipPath>
{clips}
</defs>""")


def land_and_sea(svg, view, doc, clip_id):
    deco = doc["decorado_de_la_muestra"]
    x0, y0, w, h = view.x0, view.y0, view.w, view.h
    svg.add(f'<g clip-path="url(#{clip_id})">')
    svg.add(f'<rect x="{fmt(x0)}" y="{fmt(y0)}" width="{fmt(w)}" height="{fmt(h)}" fill="url(#gTierra)"/>')
    svg.add(f'<rect x="{fmt(x0)}" y="{fmt(y0)}" width="{fmt(w)}" height="{fmt(h)}" fill="url(#pDosel)"/>')
    # Andes (decorativo): del pie oriental hacia el oeste (arriba).
    pie = [view(p["lat"], p["lon"]) for p in deco["pie_oriental_de_los_andes"]]
    la_n = deco["pie_oriental_de_los_andes"][0]["lat"]
    la_s = deco["pie_oriental_de_los_andes"][-1]["lat"]
    poly = pie + [view(la_s - 5, -95.0), view(la_n + 5, -95.0)]
    svg.add(f'<path d="M{" L".join(f"{fmt(a)},{fmt(b)}" for a, b in poly)} Z" fill="url(#gAndes)" opacity="0.45"/>')
    # Atlántico: costa aproximada (muestra) hasta el borde de la caja.
    costa = [view(p["lat"], p["lon"]) for p in deco["costa_atlantica"]]
    first, last = deco["costa_atlantica"][0], deco["costa_atlantica"][-1]
    sea = costa + [view(last["lat"] - 2, -40.0), view(first["lat"] + 6, -40.0), view(first["lat"] + 6, first["lon"])]
    path = "M" + " L".join(f"{fmt(a)},{fmt(b)}" for a, b in sea) + " Z"
    svg.add(f'<path d="{path}" fill="url(#gMar)"/><path d="{path}" fill="url(#pOla)"/>')
    svg.add(f'<path d="M{" L".join(f"{fmt(a)},{fmt(b)}" for a, b in costa)}" fill="none" stroke="{C["destello"]}" stroke-width="1.6" opacity="0.55"/>')
    # Pacífico (dentro de la caja: de la costa del Chocó a Tacna), al oeste de la costa, es decir, arriba en la vista girada.
    pac = [view(p["lat"], p["lon"]) for p in deco["costa_pacifica"]]
    pa, pz = deco["costa_pacifica"][0], deco["costa_pacifica"][-1]
    poly = pac + [view(pz["lat"] - 6, -95.0), view(pa["lat"] + 6, -95.0)]
    path = "M" + " L".join(f"{fmt(a)},{fmt(b)}" for a, b in poly) + " Z"
    svg.add(f'<path d="{path}" fill="url(#gMar)"/><path d="{path}" fill="url(#pOla)"/>')
    svg.add(f'<path d="M{" L".join(f"{fmt(a)},{fmt(b)}" for a, b in pac)}" fill="none" stroke="{C["destello"]}" stroke-width="1.6" opacity="0.55"/>')
    svg.add(f'<rect x="{fmt(x0)}" y="{fmt(y0)}" width="{fmt(w)}" height="{fmt(h)}" fill="url(#gLuz)"/>')
    svg.add("</g>")


def ribbon(svg, px, width, kind, color=None, dashed=False):
    if len(px) < 2:
        return
    d = smooth_path(px)
    if kind == "lleno":
        # Borde fino (un 35 % del ancho, 1,2 como mínimo): en los ríos delgados el agua tiene que dominar sobre el borde,
        # o el río lleno se confunde con el vidrio.
        svg.add(f'<path d="{d}" fill="none" stroke="{C["vidrio_borde"]}" stroke-width="{fmt(width + max(1.2, width * 0.35))}" stroke-linecap="round" stroke-linejoin="round" opacity="0.92"/>')
        svg.add(f'<path d="{d}" fill="none" stroke="{color}" stroke-width="{fmt(width)}" stroke-linecap="round" stroke-linejoin="round"/>')
        svg.add(f'<path d="{d}" fill="none" stroke="{C["destello"]}" stroke-width="{fmt(max(0.8, width * 0.22))}" stroke-linecap="round" opacity="0.35" transform="translate(-0.6,-0.6)"/>')
    elif kind == "vidrio":
        svg.add(f'<path d="{d}" fill="none" stroke="{C["vidrio_borde"]}" stroke-width="{fmt(width + 2.6)}" stroke-linecap="round" stroke-linejoin="round" opacity="0.78"/>')
        svg.add(f'<path d="{d}" fill="none" stroke="{C["vidrio"]}" stroke-width="{fmt(width)}" stroke-linecap="round" stroke-linejoin="round" opacity="0.55"/>')
        svg.add(f'<path d="{d}" fill="none" stroke="#FFFFFF" stroke-width="{fmt(max(0.7, width * 0.25))}" stroke-linecap="round" opacity="0.65" transform="translate(-0.5,-0.5)"/>')
    else:  # fuera de la cuenca
        da = ' stroke-dasharray="6 4"' if dashed else ""
        svg.add(f'<path d="{d}" fill="none" stroke="{C["fuera"]}" stroke-width="{fmt(width)}" stroke-linecap="round"{da} opacity="0.85"/>')


def draw_rivers(svg, view, doc, scale=1.0):
    muestra = doc["estado_de_muestra_nivel_200"]
    lim = muestra["afluentes_llenos_hasta_km_desde_la_boca"]
    order = {"fuera_de_cuenca": 0, "rio_local": 1, "afluente_paso": 2, "afluente_mundo": 3, "ramal": 4, "cauce": 5}
    # Los ríos locales punteados ([NV]) van debajo de los que tienen el agua comprobada (el Itaya en línea recta cruza
    # el esbozo del Nanay).
    rios = sorted(doc["rios"], key=lambda r: (order[r["tipo"]], r["id"] in RIO_AGUA))
    main = None
    for r in rios:
        w = ANCHO[r["tipo"]] * scale
        if r["tipo"] == "rio_local":
            if scale < 2 or len(r["puntos"]) < 2:  # solo en los recuadros (vista Mundo); «solo_boca» no tiene trazado
                continue
            px = [view(p["lat"], p["lon"]) for p in r["puntos"]]
            if r["id"] in RIO_AGUA:
                ribbon(svg, px, w, "lleno", color=AGUA_COLOR[RIO_AGUA[r["id"]]])
            else:  # agua sin comprobar [NV]: trazo punteado
                svg.add(f'<path d="{smooth_path(px)}" fill="none" stroke="{C["vidrio_borde"]}" stroke-width="{fmt(w + 0.6)}" stroke-dasharray="2 3" stroke-linecap="round" opacity="0.9"/>')
            continue
        color = AGUA_COLOR[RIO_AGUA[r["id"]]]
        if r["tipo"] == "fuera_de_cuenca":
            ribbon(svg, [view(p["lat"], p["lon"]) for p in r["puntos"]], w, "fuera", dashed=True)
            continue
        if r["tipo"] == "cauce":
            main = r
            glass, full = split_main_by_km(r["puntos"], muestra["cauce"]["lleno_desde_km_desde_nacimiento"])
        elif r["tipo"] == "ramal":
            glass, full = [], [(p["lat"], p["lon"]) for p in r["puntos"]]
        else:
            glass, full = split_by_distance([(p["lat"], p["lon"]) for p in r["puntos"]], lim.get(r["id"], 0))
        ribbon(svg, [view(a, b) for a, b in glass], w, "vidrio")
        ribbon(svg, [view(a, b) for a, b in full], w, "lleno", color=color)
    # Dos aguas lado a lado (estilización): Encuentro (negra junto a blanca) y Santarém (clara junto a blanca).
    if main:
        w = ANCHO["cauce"] * scale
        for km_a, km_b, col in ((5138, 5205, C["negra"]), (5871, 5935, C["clara"])):
            seg = [view(a, b) for a, b in km_segment(main["puntos"], km_a, km_b)]
            if len(seg) >= 2:
                svg.add(f'<path d="{smooth_path(offset_line(seg, w * 0.22))}" fill="none" stroke="{col}" stroke-width="{fmt(w * 0.48)}" stroke-linecap="round"/>')
    return main


def route_upstream(doc, main, km_hasta):
    """Ruta real de la canoa en el sentido del viaje: Belém → ramal del Pará → Gurupá → cauce río arriba hasta km_hasta."""
    para = next(r for r in doc["rios"] if r["id"] == "para")["puntos"]
    i_belem = next(i for i, p in enumerate(para) if p["nombre"].startswith("Belém"))
    ruta = [(p["lat"], p["lon"]) for p in reversed(para[: i_belem + 1])]  # Belém, Abaetetuba, Breves, Gurupá
    km_gurupa = next(p["km_desde_nacimiento"] for p in main["puntos"] if p["nombre"] == "Gurupá")
    cauce = km_segment(main["puntos"], km_hasta, km_gurupa)  # de la cabecera a Gurupá
    return ruta + list(reversed(cauce))[1:]


def chevrons(svg, view, route, every_px=70):
    """Flechas «río arriba» sobre la ruta (lista lat, lon en el sentido del viaje)."""
    seg = [view(a, b) for a, b in route]
    acc = every_px / 2
    for i in range(len(seg) - 1):
        a, b = seg[i], seg[i + 1]  # en el sentido del viaje
        L = math.hypot(b[0] - a[0], b[1] - a[1])
        pos = 0.0
        while acc <= L - pos:
            pos += acc
            acc = every_px
            t = pos / L
            cx, cy = a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t
            ang = math.degrees(math.atan2(b[1] - a[1], b[0] - a[0]))
            svg.add(f'<path d="M-3.5,-3.2 L1.5,0 L-3.5,3.2" transform="translate({fmt(cx)},{fmt(cy)}) rotate({fmt(ang)})" fill="none" stroke="{C["destello"]}" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" opacity="0.95"/>')
        acc -= (L - pos)


def mini_cuia(svg, x, y, agua, s=1.0, future=False):
    """Marcador de parada: una cuia de vidrio en miniatura con el agua del sitio (nunca un tubo)."""
    cols = []
    a = agua.lower()
    for key in ("negra", "clara", "blanca"):
        if key in a:
            cols.append(AGUA_COLOR[key])
    if not cols:
        cols = [C["blanca"]]
    body = f"M{-8 * s},{-3 * s} Q{-8 * s},{8 * s} 0,{8 * s} Q{8 * s},{8 * s} {8 * s},{-3 * s} Z"
    g = [f'<g transform="translate({fmt(x)},{fmt(y)})">']
    g.append(f'<ellipse cx="0" cy="{fmt(9 * s)}" rx="{fmt(8 * s)}" ry="{fmt(2.2 * s)}" fill="#000" opacity="0.25"/>')
    if future:
        g.append(f'<path d="{body}" fill="{C["panel"]}" stroke="{C["vidrio_borde"]}" stroke-width="{fmt(1.2 * s)}" opacity="0.9"/>')
    elif len(cols) == 1:
        g.append(f'<path d="{body}" fill="{cols[0]}"/>')
    else:
        g.append(f'<path d="M{-8 * s},{-3 * s} Q{-8 * s},{8 * s} 0,{8 * s} L0,{-3 * s} Z" fill="{cols[0]}"/>')
        g.append(f'<path d="M0,{-3 * s} L0,{8 * s} Q{8 * s},{8 * s} {8 * s},{-3 * s} Z" fill="{cols[1]}"/>')
    if not future:
        g.append(f'<path d="{body}" fill="none" stroke="{C["vidrio_borde"]}" stroke-width="{fmt(1.5 * s)}"/>')
        g.append(f'<path d="M{-5.5 * s},{0} Q{-5 * s},{5 * s} {-1.5 * s},{6 * s}" fill="none" stroke="#FFFFFF" stroke-width="{fmt(1.1 * s)}" opacity="0.7"/>')
    g.append(f'<ellipse cx="0" cy="{fmt(-3 * s)}" rx="{fmt(8 * s)}" ry="{fmt(2 * s)}" fill="none" stroke="{C["vidrio_borde"]}" stroke-width="{fmt(1.2 * s)}"/>')
    g.append("</g>")
    svg.add("".join(g))


def badge(svg, x, y, label, r=8.5):
    w = max(2 * r, len(label) * 6.6 + 8)
    svg.add(f'<rect x="{fmt(x - w / 2)}" y="{fmt(y - r)}" width="{fmt(w)}" height="{fmt(2 * r)}" rx="{fmt(r)}" fill="{C["marco"]}" stroke="{C["crema"]}" stroke-width="1.2"/>')
    svg.text(x, y + 4, label, size=11.5, anchor="middle", family=TEXT, weight="800")


def flag(svg, x, y, pais, s=1.0, gris=False):
    """Bandera de cortesía en un mástil (oficial y simplificada; nunca en tierra ni en islas)."""
    g = [f'<g transform="translate({fmt(x)},{fmt(y)}) scale({s})">',
         f'<line x1="0" y1="0" x2="0" y2="-22" stroke="{C["crema"]}" stroke-width="1.6"/>']
    fx, fy, fw, fh = 1, -22, 16, 11
    if gris:
        g.append(f'<rect x="{fx}" y="{fy}" width="{fw}" height="{fh}" fill="#B9C6CC" opacity="0.8"/>')
    elif pais == "CO":
        g += [f'<rect x="{fx}" y="{fy}" width="{fw}" height="{fh / 2}" fill="#FCD116"/>',
              f'<rect x="{fx}" y="{fy + fh / 2}" width="{fw}" height="{fh / 4}" fill="#003893"/>',
              f'<rect x="{fx}" y="{fy + 3 * fh / 4}" width="{fw}" height="{fh / 4}" fill="#CE1126"/>']
    elif pais == "PE":
        g += [f'<rect x="{fx}" y="{fy}" width="{fw}" height="{fh}" fill="#D91023"/>',
              f'<rect x="{fx + fw / 3}" y="{fy}" width="{fw / 3}" height="{fh}" fill="#FFFFFF"/>']
    elif pais == "BR":
        g += [f'<rect x="{fx}" y="{fy}" width="{fw}" height="{fh}" fill="#009C3B"/>',
              f'<path d="M{fx + fw / 2},{fy + 1.2} L{fx + fw - 1.5},{fy + fh / 2} L{fx + fw / 2},{fy + fh - 1.2} L{fx + 1.5},{fy + fh / 2} Z" fill="#FFDF00"/>',
              f'<circle cx="{fx + fw / 2}" cy="{fy + fh / 2}" r="2.6" fill="#002776"/>']
    g.append(f'<rect x="{fx}" y="{fy}" width="{fw}" height="{fh}" fill="none" stroke="{C["tinta"]}" stroke-width="0.6"/>')
    g.append("</g>")
    svg.add("".join(g))


def peak(svg, x, y, s=1.0):
    svg.add(f'<path d="M{fmt(x - 9 * s)},{fmt(y + 6 * s)} L{fmt(x)},{fmt(y - 8 * s)} L{fmt(x + 9 * s)},{fmt(y + 6 * s)} Z" fill="{C["roca_b"]}" stroke="#5E4E3A" stroke-width="0.8"/>'
            f'<path d="M{fmt(x - 3.4 * s)},{fmt(y - 2.6 * s)} L{fmt(x)},{fmt(y - 8 * s)} L{fmt(x + 3.4 * s)},{fmt(y - 2.6 * s)} L{fmt(x + 1.2 * s)},{fmt(y - 3.6 * s)} L{fmt(x - 1 * s)},{fmt(y - 2.4 * s)} Z" fill="{C["nieve"]}"/>')


def canoe(svg, x, y, ang=-90, s=1.0):
    """Canoa de Capi con las cuias atadas y Rosa delante (glifo de muestra)."""
    svg.add(f'<g transform="translate({fmt(x)},{fmt(y)}) rotate({fmt(ang)}) scale({s})">'
            f'<path d="M-15,0 Q0,9 15,0 Q0,4 -15,0 Z" fill="#7E5638" stroke="{C["crema"]}" stroke-width="1"/>'
            f'<circle cx="-5" cy="-1.5" r="3.6" fill="#8B6A3E" stroke="#2B2118" stroke-width="0.6"/>'
            f'<circle cx="4" cy="0.5" r="2.3" fill="{C["vidrio_borde"]}" opacity="0.9"/><circle cx="8.5" cy="0.8" r="2" fill="{C["vidrio_borde"]}" opacity="0.9"/>'
            f'<path d="M24,1 q5,-6 10,0" fill="none" stroke="#E8A0B4" stroke-width="2.6" stroke-linecap="round"/>'
            f'</g>')


def future_world(svg, x, y, text, anchor="start"):
    svg.plaque(x, y, text, size=11.5, anchor=anchor, fill=C["panel"], opacity=0.9)


def leader(svg, x1, y1, x2, y2):
    svg.add(f'<path d="M{fmt(x1)},{fmt(y1)} L{fmt(x2)},{fmt(y2)}" stroke="{C["crema"]}" stroke-width="1" opacity="0.75"/>')


# ------------------------------------------------------------------ composición
def build_svg(doc):
    pr = Proj(doc["proyeccion"])
    W = 1500
    MX, MY, MW = 40, 150, 980
    main_view = View(MX, MY, MW, (pr.la0, pr.la1), (pr.lo0, pr.lo1), pr.lat0)
    MH = main_view.h
    PX = MX + MW + 40  # panel derecho
    PW = W - PX - 40
    insetA = View(PX, MY + 470, PW, (-4.45, -3.30), (-70.85, -69.80), pr.lat0)
    insetB = View(PX, insetA.y0 + insetA.h + 62, PW, (-5.55, -3.15), (-74.85, -72.85), pr.lat0)
    list_y = MY + MH + 44
    H = int(list_y + 300)

    svg = Svg()
    defs(svg, (MX, MY, MW, MH), [("A", insetA), ("B", insetB)])
    svg.add(f'<rect width="{W}" height="{H}" fill="url(#gFondo)"/>')
    svg.add(f'<rect width="{W}" height="{H}" fill="url(#pDosel)" opacity="0.35"/>')

    # Cabecera de madera
    svg.add(f'<rect x="24" y="20" width="{W - 48}" height="98" rx="14" fill="{C["madera_oscura"]}" stroke="{C["marco_bisel"]}" stroke-width="1.5"/>')
    svg.text(48, 62, "Mapa del río · muestra con la forma real del Amazonas", size=30, weight="800")
    svg.text(48, 100, "Estado al ganar el nivel 200 (fin de los mundos 1-5; según el GDD, la 1.0 llega al 700: Q14). Lo remontado, lleno de su agua real; "
             "lo que falta, en vidrio. Giro de 90°: Andes arriba, Atlántico abajo, norte a la derecha.", size=14.5, weight="600", family=TEXT, opacity=0.92)
    svg.text(W - 48, 46, "Material de apoyo · no es arte final", size=13, anchor="end", weight="700", family=TEXT, fill=C["marco_bisel"])
    svg.text(W - 48, 66, "Pendiente del asesor cultural", size=13, anchor="end", weight="700", family=TEXT, fill=C["marco_bisel"])

    # Marco del mapa
    svg.add(f'<rect x="{MX - 12}" y="{MY - 12}" width="{MW + 24}" height="{fmt(MH + 24)}" rx="16" fill="url(#gMadera)" stroke="#4E2F22" stroke-width="2"/>')
    svg.add(f'<rect x="{MX - 5}" y="{MY - 5}" width="{MW + 10}" height="{fmt(MH + 10)}" rx="10" fill="none" stroke="{C["marco_bisel"]}" stroke-width="1.5" opacity="0.8"/>')

    land_and_sea(svg, main_view, doc, "clipMapa")
    svg.add('<g clip-path="url(#clipMapa)">')
    main = draw_rivers(svg, main_view, doc)
    chevrons(svg, main_view, route_upstream(doc, main, 2738), every_px=64)
    v = main_view

    deco = doc["decorado_de_la_muestra"]
    for p in deco["picos_y_cabeceras"]:
        x, y = v(p["lat"], p["lon"])
        peak(svg, x, y, 1.0 if "Mismi" not in p["nombre"] else 1.5)

    # Rótulos de país (sin fronteras)
    for p in doc["rotulos_de_pais"]:
        x, y = v(p["lat"], p["lon"])
        svg.text(x, y, p["nombre"].upper(), size=15 if len(p["nombre"]) < 12 else 12, anchor="middle", weight="800",
                 opacity=0.55, spacing="2", halo="#0B2A20")
    svg.text(MX + MW / 2, MY + 30, "ANDES  ↑  (oeste)", size=17, anchor="middle", weight="800", fill=C["crema"], halo="#3D3020", spacing="1.5")
    ox, oy = v(4.6, -49.2)
    svg.text(ox, oy, "OCÉANO ATLÁNTICO  ↓", size=17, anchor="middle", weight="800", halo="#073640", spacing="1.5")
    svg.text(ox, oy + 20, "(este)", size=12, anchor="middle", weight="700", family=TEXT, halo="#073640")
    qx, qy = v(-17.6, -78.8)
    svg.text(qx, qy, "OCÉANO PACÍFICO", size=14, anchor="start", weight="800", halo="#073640", spacing="1.5")

    # Mundos futuros (silueta azul petróleo con «Nivel n», 06 §4.5)
    fut = [(6, -0.4625, -76.9842, "start", (10, 4)), (7, -12.5971, -69.1665, "start", (26, -8)),
           (8, -14.4422, -67.5283, "start", (10, 14)), (9, -8.3833, -74.55, "end", (-10, 22)),
           (10, -0.13, -67.0889, "start", (10, 26)), (11, 2.82, -60.6719, "end", (-10, -60)),
           (12, 5.6631, -67.6264, "end", (-10, -14)), (13, -15.5253, -71.6908, "start", (14, 30))]
    nombres = {m["mundo"]: (m["nombre"], m["niveles"][0]) for m in doc["paradas_201_1000"]}
    for n, la, lo, anc, (dx, dy) in fut:
        x, y = v(la, lo)
        mini_cuia(svg, x, y, "blanca", 0.8, future=True)
        future_world(svg, x + dx, y + dy, f"Mundo {n} · {nombres[n][0]} · Nivel {nombres[n][1]}", anchor=anc)

    # Puntos de paso
    paso_off = {"Gurupá": (6, -4), "Óbidos": (7, -3), "Parintins": (7, 4), "boca del Madeira": (-7, 13),
                "Coari": (-6, 12), "Fonte Boa": (7, 3), "Pebas": (7, -2), "boca del Napo": (7, 10), "boca del Purus": (-6, 12)}
    for p in doc["puntos_de_paso_1_200"]:
        if p["nombre"] not in paso_off:
            continue
        x, y = v(p["lat"], p["lon"])
        dx, dy = paso_off[p["nombre"]]
        svg.add(f'<circle cx="{fmt(x)}" cy="{fmt(y)}" r="2.6" fill="{C["crema"]}" stroke="{C["tinta"]}" stroke-width="0.7"/>')
        svg.text(x + dx, y + dy, p["nombre"], size=11, anchor="start" if dx > 0 else "end", weight="700",
                 family=TEXT, italic=True, halo="#123326")

    # Miradores
    for p in doc["miradores_1_200"]:
        x, y = v(p["lat"], p["lon"])
        svg.add(f'<circle cx="{fmt(x)}" cy="{fmt(y)}" r="8" fill="{C["ambar"]}" stroke="{C["crema"]}" stroke-width="1.3"/>')
        svg.text(x, y + 4.5, "?", size=12.5, anchor="middle", weight="800")
    mx, my = v(0.62, -51.1)
    svg.text(mx + 4, my - 14, "Miradores del estuario", size=11, anchor="middle", family=TEXT, italic=True, halo="#073640")

    # Banderas de cortesía de 1-200 y cruces futuros (en gris). Las vueltas a un país en el mismo punto (560, 700, 920 y
    # 960) no se repiten: su mástil ya está dibujado.
    puestas = []
    for p in sorted(doc["cruces_de_pais"], key=lambda c: c["nivel"]):
        if any(abs(p["lat"] - a) < 0.1 and abs(p["lon"] - b) < 0.1 for a, b in puestas):
            continue
        puestas.append((p["lat"], p["lon"]))
        x, y = v(p["lat"], p["lon"])
        if p["nivel"] <= 200:
            flag(svg, x + 9, y - 2, p["pais"], 0.95)
        else:
            flag(svg, x + 6, y - 2, p["pais"], 0.7, gris=True)

    # Casa de TiTino
    tx, ty = v(-1.30, -48.25)
    svg.add(f'<g transform="translate({fmt(tx)},{fmt(ty)})"><rect x="-1.5" y="-4" width="3" height="14" fill="#6B4A32"/>'
            f'<circle cx="-5" cy="-7" r="6" fill="{C["selva_luz"]}"/><circle cx="5" cy="-8" r="6" fill="#4E9A3C"/><circle cx="0" cy="-13" r="6" fill="{C["selva_luz"]}"/></g>')
    svg.text(tx + 22, ty + 2, "Casa de TiTino [P]", size=11, family=TEXT, italic=True, halo="#123326")

    # Paradas de 1-200
    paradas = doc["paradas_1_200"]
    off = {  # etiqueta del mapa general: (dx, dy, ancla)
        "belem": (-14, 30, "end"), "santarem": (70, 2, "start"), "alter_do_chao": (73, 32, "start"),
        "flona_tapajos": (112, 59, "start"), "encontro": (56, 22, "start"), "manaos": (56, 46, "start"),
        "anavilhanas": (40, -14, "start"), "tefe": (-24, 34, "end"),
    }
    cluster_A = {"leticia", "leticia_atardecer", "amacayacu", "puerto_narino", "tarapoto", "rio_frontera"}
    cluster_B = {"iquitos", "itaya", "allpahuayo_mishana", "nauta", "pacaya_samiria", "pacaya_alba"}
    seen = set()
    for s in paradas:
        x, y = v(s["lat"], s["lon"])
        key = (round(s["lat"], 3), round(s["lon"], 3))
        if key in seen:
            continue
        seen.add(key)
        mini_cuia(svg, x, y, s["agua_del_sitio"], 0.9)
        if s["id"] in off:
            dx, dy, anc = off[s["id"]]
            lx, ly = x + dx, y + dy
            leader(svg, x, y + 4, lx + (4 if anc == "end" else -4), ly - 5)
            svg.plaque(lx, ly, f"{s['k']} · {s['nombre']}", size=12.5, anchor=anc)
    # recuadros de los racimos
    for name, ids, view_lbl, lbl_off in (("A", cluster_A, "A · Trapecio (81-140): ver recuadro", (None, 18, "start")),
                                         ("B", cluster_B, "B · Loreto (141-200): ver recuadro", ("top", -6, -6, "end"))):
        pts = [v(s["lat"], s["lon"]) for s in paradas if s["id"] in ids]
        xs, ys = [p[0] for p in pts], [p[1] for p in pts]
        bx0, bx1, by0, by1 = min(xs) - 14, max(xs) + 14, min(ys) - 14, max(ys) + 16
        svg.add(f'<rect x="{fmt(bx0)}" y="{fmt(by0)}" width="{fmt(bx1 - bx0)}" height="{fmt(by1 - by0)}" rx="6" fill="none" stroke="{C["crema"]}" stroke-width="1.4" stroke-dasharray="5 3"/>')
        if lbl_off[0] is None:  # a la derecha del recuadro
            svg.plaque(bx1 + 10, by0 + lbl_off[1], view_lbl, size=12.5, anchor=lbl_off[2])
        else:  # encima del recuadro, a la izquierda
            _, dx, dy, anc = lbl_off
            svg.plaque(bx0 + dx, by0 + dy, view_lbl, size=12.5, anchor=anc)

    # Inicio, nivel 200 y meta
    bx, by = v(-1.4558, -48.5039)
    svg.plaque(bx + 14, by + 48, "INICIO · nivel 1 · Belém (Estuario)", size=13, fill="#2F7D32")
    px_, py_ = v(-5.25, -74.6667)
    canoe(svg, px_ + 10, py_ - 4, ang=-90, s=1.1)
    svg.plaque(px_ - 18, py_ - 56, "NIVEL 200 · fin de los mundos 1-5 · canoa de Capi y Rosa", size=12.5, anchor="end", fill="#2F7D32")
    gx, gy = v(-15.5253, -71.6908)
    svg.plaque(gx + 16, gy - 12, "META · 1.000 · nevado Mismi: una de las nacientes", size=13, fill=C["ambar"])
    nx, ny = v(-4.4454, -73.4532)

    # Rosa de los vientos y escala
    cx, cy = v(-15.8, -50.0)
    svg.add(f'<g transform="translate({fmt(cx)},{fmt(cy)})"><circle r="30" fill="{C["madera_oscura"]}" opacity="0.85" stroke="{C["marco_bisel"]}" stroke-width="1.4"/>'
            f'<path d="M-22,0 L0,-5 L24,0 L0,5 Z" fill="{C["crema"]}"/><path d="M0,-22 L5,0 L0,22 L-5,0 Z" fill="{C["marco_bisel"]}"/></g>')
    svg.text(cx + 40, cy + 5, "N", size=16, anchor="middle", weight="800")
    svg.text(cx, cy - 36, "O", size=12, anchor="middle", weight="800", opacity=0.8)
    svg.text(cx, cy + 48, "E", size=12, anchor="middle", weight="800", opacity=0.8)
    svg.text(cx - 40, cy + 5, "S", size=12, anchor="middle", weight="800", opacity=0.8)
    km500 = 500 / 111.2 * v.k
    sx, sy = cx - 30, cy + 74
    svg.add(f'<rect x="{fmt(sx)}" y="{fmt(sy)}" width="{fmt(km500)}" height="6" fill="{C["crema"]}"/><rect x="{fmt(sx)}" y="{fmt(sy)}" width="{fmt(km500 / 2)}" height="6" fill="{C["madera_oscura"]}"/>')
    svg.text(sx, sy + 22, "500 km (de sur a norte)", size=11.5, family=TEXT, halo="#123326")
    svg.add("</g>")  # fin del clip del mapa

    # ---------------- panel derecho: leyenda
    ly = MY
    svg.add(f'<rect x="{PX}" y="{ly - 8}" width="{PW}" height="440" rx="12" fill="{C["madera_oscura"]}" opacity="0.92" stroke="{C["marco_bisel"]}" stroke-width="1"/>')
    svg.text(PX + 16, ly + 22, "Leyenda", size=19, weight="800")
    rows = [
        ("lleno", C["blanca"], "Río lleno de agua blanca (café con leche)"),
        ("lleno", C["negra"], "Río lleno de agua negra (color té)"),
        ("lleno", C["clara"], "Río lleno de agua clara (verde azulada)"),
        ("vidrio", None, "Río de vidrio: por remontar después del 200"),
        ("fuera", None, "Fuera de la cuenca (Orinoco, Maroni)"),
    ]
    yy = ly + 52
    for kind, col, label in rows:
        ribbon(svg, [(PX + 18, yy - 4), (PX + 62, yy - 4)], 6, kind, color=col, dashed=(kind == "fuera"))
        svg.text(PX + 74, yy, label, size=12.5, family=TEXT, weight="700")
        yy += 25
    mini_cuia(svg, PX + 40, yy - 6, "blanca", 0.9)
    svg.text(PX + 74, yy, "Parada (mini cuia con el agua del sitio)", size=12.5, family=TEXT)
    yy += 25
    mini_cuia(svg, PX + 40, yy - 6, "blanca", 0.8, future=True)
    svg.text(PX + 74, yy, "Mundo por venir (silueta con «Nivel n»)", size=12.5, family=TEXT)
    yy += 25
    svg.add(f'<circle cx="{PX + 40}" cy="{yy - 4}" r="2.8" fill="{C["crema"]}" stroke="{C["tinta"]}" stroke-width="0.7"/>')
    svg.text(PX + 74, yy, "Punto de paso: se nombra al pasar", size=12.5, family=TEXT)
    yy += 25
    svg.add(f'<circle cx="{PX + 40}" cy="{yy - 4}" r="8" fill="{C["ambar"]}" stroke="{C["crema"]}" stroke-width="1.3"/>')
    svg.text(PX + 40, yy + 0.5, "?", size=12.5, anchor="middle", weight="800")
    svg.text(PX + 74, yy, "Mirador fuera de la ruta (un dato extra)", size=12.5, family=TEXT)
    yy += 27
    flag(svg, PX + 34, yy + 4, "CO", 0.9)
    svg.text(PX + 74, yy, "Bandera de cortesía (en gris: después del 200)", size=12.5, family=TEXT)
    yy += 27
    svg.add(f'<path d="M-3.5,-3.2 L1.5,0 L-3.5,3.2" transform="translate({PX + 40},{yy - 4}) rotate(-90)" fill="none" stroke="{C["destello"]}" stroke-width="1.8" stroke-linecap="round"/>')
    svg.text(PX + 74, yy, "Viaje: Belém → ramal del Pará → río arriba", size=12.5, family=TEXT)
    yy += 25
    svg.add(f'<path d="M{PX + 18},{yy - 4} L{PX + 62},{yy - 4}" fill="none" stroke="{C["vidrio_borde"]}" stroke-width="1.8" stroke-dasharray="2 3" stroke-linecap="round"/>')
    svg.text(PX + 74, yy, "Río local: agua sin confirmar [NV]", size=12.5, family=TEXT)
    yy += 25
    svg.text(PX + 16, yy + 4, "Sin fronteras terrestres dibujadas (Esequibo, isla Santa Rosa):", size=11.5, family=TEXT, opacity=0.85)
    svg.text(PX + 16, yy + 20, "los países se nombran y la bandera va en la canoa.", size=11.5, family=TEXT, opacity=0.85)

    # ---------------- recuadros (vista de tramo)
    def inset(view, clip, title, ids, label_off, paso_names, flags_here, canoe_at=None):
        svg.add(f'<rect x="{fmt(view.x0 - 8)}" y="{fmt(view.y0 - 36)}" width="{fmt(view.w + 16)}" height="{fmt(view.h + 44)}" rx="12" fill="url(#gMadera)" stroke="#4E2F22" stroke-width="1.5"/>')
        svg.text(view.x0 + 4, view.y0 - 13, title, size=15, weight="800")
        land_and_sea(svg, view, doc, clip)
        svg.add(f'<g clip-path="url(#{clip})">')
        mn = draw_rivers(svg, view, doc, scale=2.2)
        for p in doc["puntos_de_paso_1_200"]:
            if p["nombre"] in paso_names:
                x, y = view(p["lat"], p["lon"])
                svg.add(f'<circle cx="{fmt(x)}" cy="{fmt(y)}" r="3" fill="{C["crema"]}" stroke="{C["tinta"]}" stroke-width="0.7"/>')
                dx, dy = paso_names[p["nombre"]]
                svg.text(x + dx, y + dy, p["nombre"], size=11.5, anchor="start" if dx > 0 else "end", family=TEXT, italic=True, halo="#123326")
        for code, la, lo in flags_here:
            x, y = view(la, lo)
            flag(svg, x + 10, y - 2, code, 1.1)
        done = set()
        for s in doc["paradas_1_200"]:
            if s["id"] not in ids:
                continue
            x, y = view(s["lat"], s["lon"])
            key = (round(s["lat"], 3), round(s["lon"], 3))
            if key not in done:
                mini_cuia(svg, x, y, s["agua_del_sitio"], 1.25)
                done.add(key)
            dx, dy, anc, txt = label_off[s["id"]]
            lx, ly = x + dx, y + dy
            if txt is not None:
                leader(svg, x, y + 4, lx + (4 if anc == "end" else -4), ly - 5)
                svg.plaque(lx, ly, txt.format(k=s["k"], n=s["nombre"]), size=12, anchor=anc)
        if canoe_at:
            x, y = view(*canoe_at)
            canoe(svg, x + 12, y - 6, ang=-90, s=1.3)
        svg.add("</g>")

    inset(insetA, "clipA", "A · Tramo del Trapecio (Colombia, 81-140)", cluster_A, {
        "leticia": (20, 34, "start", "{k} · 9 · Leticia (de día y al atardecer)"),
        "leticia_atardecer": (0, 0, "end", None),
        "amacayacu": (-10, 40, "end", "{k} · Amacayacu"),
        "puerto_narino": (34, 30, "start", "{k} · Puerto Nariño"),
        "tarapoto": (-26, 44, "end", "{k} · Lagos de Tarapoto"),
        "rio_frontera": (-26, -30, "end", "{k} · El río frontera"),
    }, {"Tabatinga": (-6, -10), "Caballococha": (-8, 4)}, [("CO", -4.215, -69.9411), ("PE", -3.8398, -70.619)])
    inset(insetB, "clipB", "B · Tramo de Loreto (Perú, 141-200)", cluster_B, {
        "iquitos": (-8, 34, "end", "{k} · Iquitos"),
        "itaya": (-6, 55, "end", "{k} · Río Itaya (Iquitos)"),
        "allpahuayo_mishana": (-18, -20, "end", "{k} · Allpahuayo-Mishana"),
        "nauta": (-110, 34, "start", "{k} · Nauta: nace el nombre"),
        "pacaya_samiria": (12, 34, "start", "{k} · 19 · Pacaya-Samiria"),
        "pacaya_alba": (0, 0, "end", None),
    }, {}, [], canoe_at=(-5.25, -74.6667))

    # ---------------- lista de paradas (bajo el mapa)
    svg.add(f'<rect x="{MX - 12}" y="{fmt(list_y - 26)}" width="{W - 2 * (MX - 12)}" height="230" rx="12" fill="{C["madera_oscura"]}" opacity="0.92" stroke="{C["marco_bisel"]}" stroke-width="1"/>')
    svg.text(MX + 4, list_y, "Paradas de 1-200 (mundos 1-5): una cada 10 niveles; se llega a la parada k al ganar el nivel 10k", size=16, weight="800")
    col_w = (W - 2 * MX) / 2
    tipo_txt = {"gran_parada": "gran parada", "parada": "parada", "castillo": "cierre de mundo", "variante": "misma vista, otra luz"}
    fondo_txt = {"pintado": "héroe pintado", "codigo": "héroe por código", "variante_lut": "otra luz (LUT)", "kit_y_rotulo": "paisaje del tramo: kit y rótulo"}
    for i, s in enumerate(paradas):
        col, row = divmod(i, 10)
        x = MX + 8 + col * col_w
        y = list_y + 30 + row * 18.5
        mini_cuia(svg, x + 6, y - 6, s["agua_del_sitio"], 0.62)
        a, b = s["niveles"]
        svg.text(x + 20, y, f"{s['k']:>2} · {s['nombre']}", size=12.5, family=TEXT, weight="800")
        svg.text(x + 300, y, f"{a}-{b} · {s['pais']} · {tipo_txt[s['tipo']]} · {fondo_txt[s['fondo']['tipo']]}", size=12, family=TEXT, weight="600", opacity=0.85)

    # Pie
    fy = list_y + 222
    svg.text(MX, fy, "Datos: rio-amazonas.json (coordenadas de Wikidata, CC0, y de las fuentes citadas; 105 puntos del cauce, sin todos los meandros). "
             "Producción: Natural Earth 1:10m (dominio público) y HydroRIVERS para la vista Mundo.", size=12, family=TEXT, weight="600", opacity=0.8)
    svg.text(MX, fy + 18, "Costas (entre ciudades de Wikidata), relieve andino, algunos tramos de afluentes de paso y el trazado de los ríos locales: aproximados [NV]. "
             "Generado con generar_mapa.py (Python 3.14, solo biblioteca estándar).", size=12, family=TEXT, weight="600", opacity=0.8)
    return svg.svg(W, H)


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--json", type=Path, default=JSON_PATH)
    ap.add_argument("--svg", type=Path, default=SVG_PATH)
    ap.add_argument("--check", action="store_true", help="valida el JSON y sale con código 1 si algo falla")
    ap.add_argument("--actualizar-xy", action="store_true", help="recalcula las coordenadas normalizadas del JSON")
    args = ap.parse_args(argv)
    doc = json.loads(args.json.read_text(encoding="utf-8"))
    pr = Proj(doc["proyeccion"])
    if args.actualizar_xy:
        actualizar_xy(doc, pr)
        args.json.write_text(json.dumps(doc, ensure_ascii=False, indent=1) + "\n", encoding="utf-8")
        print(f"xy actualizadas en {args.json.name} (aspecto {pr.aspect:.5f})")
    if args.check:
        n, errores = check(doc, pr)
        for e in errores:
            print("ERROR:", e)
        print(f"{n} puntos, {len(doc['paradas_1_200'])} paradas de 1-200, {sum(m['paradas'] for m in doc['paradas_201_1000'])} de 201-1.000 "
              f"({len(doc.get('paradas_201_700', []))} detalladas): "
              + ("todo correcto" if not errores else f"{len(errores)} errores"))
        return 1 if errores else 0
    if not args.actualizar_xy:
        args.svg.write_text(build_svg(doc), encoding="utf-8")
        print(f"escrito {args.svg.name} ({args.svg.stat().st_size // 1024} KB)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
