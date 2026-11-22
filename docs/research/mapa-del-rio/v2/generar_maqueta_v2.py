"""Maqueta del Mapa del río v2 (propuesta para la 1.1; docs/dev/propuesta-mapa-rio-v2.md).

Genera, solo con la biblioteca estándar de Python 3.14:
  - maqueta-mapa-v2.svg: la pantalla del Mapa (vista Mundo) a 1080 x 2340 px, el tramo Manaos -> Tefé
    (niveles 71-80, jugando el 76): orillas nuevas, Encuentro de las Aguas, pueblos de paso,
    Capi en su canoa y el tucuxi que se une al viaje;
  - maqueta-mapa-v2.html: la misma pantalla con las fuentes del juego (Baloo 2 y Nunito) incrustadas,
    para exportar el PNG con Chrome headless;
  - lamina-antes-despues.html: el Mapa actual (captura de la 1.0) junto a la maqueta, con las llamadas numeradas.

Uso (desde la raíz del repo):
  py -3.14 -I docs/research/mapa-del-rio/v2/generar_maqueta_v2.py
y después, para los PNG:
  chrome --headless --hide-scrollbars --screenshot=<png> --window-size=1080,2340 <html>

Es una maqueta de dirección de arte, no datos del juego: la geometría es estilizada (no sale de
river_map_v1.json) y los pueblos nuevos son candidatos [E] hasta que tools/river-map los verifique.
"""

from __future__ import annotations

import base64
import math
import random
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[3]
FONTS = REPO / "Assets" / "_Project" / "Art" / "Fonts"

W, H = 1080, 2340
HEADER = 200
MAP_BOTTOM = 2010

# ---------------------------------------------------------------- paleta (direccion-visual-v2 §4)
C = {
    "selva_sombra": "#1C5236", "selva_media": "#2C7A4B", "selva_luz": "#63B548",
    "hoja_oscura": "#145A35", "hoja_media": "#23824A", "hoja_clara": "#5DBB3F", "hoja_brillo": "#C6EF6B",
    "rio_reflejo": "#F6CF92", "rio_medio": "#CDA06A", "rio_base": "#B08A5A", "rio_profundo": "#80603E",
    "agua_negra": "#3B1C08", "agua_negra_luz": "#7A4314",
    "vidrio": "#D7EEF2", "vidrio_borde": "#EAF7F3",
    "arena": "#EBD3A0", "arena_sombra": "#CDAE76", "barranco": "#A65A3A", "barranco_luz": "#C98058",
    "barranco_sombra": "#7A3F28",
    "muelle_luz": "#A87A50", "muelle_base": "#80583A", "muelle_sombra": "#5A3D28",
    "madera_oscura": "#36261C", "marco": "#7C4E3A", "crema": "#FFF8E6", "tinta": "#2B2118",
    "destello": "#FFF4D2", "heliconia": "#E2483A", "fibra": "#C49A5C",
    "hoja_btn_a": "#86D25A", "hoja_btn_b": "#2F7D32", "hoja_btn_c": "#236A29",
}


def fmt(v: float) -> str:
    s = f"{v:.1f}"
    return s[:-2] if s.endswith(".0") else s


def pts(seq) -> str:
    return " ".join(f"{fmt(x)},{fmt(y)}" for x, y in seq)


# ---------------------------------------------------------------- geometría del río
def catmull(points, per_seg=14):
    out = []
    n = len(points)
    for i in range(n - 1):
        p0 = points[max(i - 1, 0)]
        p1 = points[i]
        p2 = points[i + 1]
        p3 = points[min(i + 2, n - 1)]
        for k in range(per_seg):
            t = k / per_seg
            t2, t3 = t * t, t * t * t
            x = 0.5 * ((2 * p1[0]) + (-p0[0] + p2[0]) * t + (2 * p0[0] - 5 * p1[0] + 4 * p2[0] - p3[0]) * t2
                       + (-p0[0] + 3 * p1[0] - 3 * p2[0] + p3[0]) * t3)
            y = 0.5 * ((2 * p1[1]) + (-p0[1] + p2[1]) * t + (2 * p0[1] - 5 * p1[1] + 4 * p2[1] - p3[1]) * t2
                       + (-p0[1] + 3 * p1[1] - 3 * p2[1] + p3[1]) * t3)
            out.append((x, y))
    out.append(points[-1])
    return out


def normals(line):
    res = []
    n = len(line)
    for i in range(n):
        a = line[max(i - 1, 0)]
        b = line[min(i + 1, n - 1)]
        dx, dy = b[0] - a[0], b[1] - a[1]
        L = math.hypot(dx, dy) or 1.0
        res.append((-dy / L, dx / L))
    return res


def curvature(line):
    n = len(line)
    k = [0.0] * n
    for i in range(1, n - 1):
        ax, ay = line[i][0] - line[i - 1][0], line[i][1] - line[i - 1][1]
        bx, by = line[i + 1][0] - line[i][0], line[i + 1][1] - line[i][1]
        cross = ax * by - ay * bx
        la, lb = math.hypot(ax, ay), math.hypot(bx, by)
        k[i] = cross / ((la * lb) or 1.0) / (0.5 * (la + lb) or 1.0)
    # suavizado
    for _ in range(6):
        k = [k[0]] + [(k[i - 1] + 2 * k[i] + k[i + 1]) / 4 for i in range(1, n - 1)] + [k[-1]]
    return k


def offset(line, nrm, d):
    if callable(d):
        return [(p[0] + q[0] * d(i), p[1] + q[1] * d(i)) for i, (p, q) in enumerate(zip(line, nrm))]
    return [(p[0] + q[0] * d, p[1] + q[1] * d) for p, q in zip(line, nrm)]


def band(line, nrm, d0, d1, i0=0, i1=None):
    """Polígono entre dos desplazamientos (constantes o funciones del índice)."""
    i1 = len(line) if i1 is None else i1
    sub_l, sub_n = line[i0:i1], nrm[i0:i1]
    f0 = d0 if callable(d0) else (lambda i, v=d0: v)
    f1 = d1 if callable(d1) else (lambda i, v=d1: v)
    a = [(p[0] + q[0] * f0(i + i0), p[1] + q[1] * f0(i + i0)) for i, (p, q) in enumerate(zip(sub_l, sub_n))]
    b = [(p[0] + q[0] * f1(i + i0), p[1] + q[1] * f1(i + i0)) for i, (p, q) in enumerate(zip(sub_l, sub_n))]
    return a + b[::-1]


def path_d(seq, close=False) -> str:
    d = "M" + " L".join(f"{fmt(x)},{fmt(y)}" for x, y in seq)
    return d + ("Z" if close else "")


def index_at_y(line, y):
    best, bi = 1e9, 0
    for i, p in enumerate(line):
        if abs(p[1] - y) < best:
            best, bi = abs(p[1] - y), i
    return bi


# El cauce (Solimões) de abajo (río abajo, hacia el Atlántico) arriba (río arriba, hacia los Andes).
MAIN_CTRL = [(470, 2130), (520, 2000), (600, 1890), (640, 1770), (590, 1650), (470, 1545), (385, 1425),
             (405, 1295), (520, 1185), (650, 1085), (690, 955), (610, 845), (480, 765), (420, 645),
             (470, 525), (560, 430), (585, 315), (530, 190)]
NEGRO_CTRL = [(1160, 1730), (1060, 1690), (960, 1720), (860, 1790), (750, 1825), (640, 1822), (585, 1835)]
PURUS_CTRL = [(-50, 1080), (90, 1130), (220, 1190), (330, 1250), (410, 1290)]
TEFE_LAKE = (225, 560)

MAIN = catmull(MAIN_CTRL, 18)
MAIN_N = normals(MAIN)
MAIN_K = curvature(MAIN)
NEGRO = catmull(NEGRO_CTRL, 14)
NEGRO_N = normals(NEGRO)
PURUS = catmull(PURUS_CTRL, 14)
PURUS_N = normals(PURUS)

HW_MAIN = 74.0   # medio ancho del cauce (px): ~27 dp, contra los 4 dp de la 1.0
HW_NEGRO = 66.0
HW_PURUS = 30.0

I_JUNCTION = index_at_y(MAIN, 1805)
I_CANOE = index_at_y(MAIN, 1205)
I_TEFE = index_at_y(MAIN, 650)
I_PURUS = index_at_y(MAIN, 1285)


def hw_main(i):
    # se estrecha un poco río arriba
    t = i / (len(MAIN) - 1)
    return HW_MAIN * (1.08 - 0.22 * t)


# ---------------------------------------------------------------- utilidades de dibujo
out: list[str] = []


def add(s: str):
    out.append(s)


def dist_to_line(x, y, line):
    best = 1e18
    for i in range(0, len(line) - 1, 2):
        ax, ay = line[i]
        bx, by = line[min(i + 2, len(line) - 1)]
        dx, dy = bx - ax, by - ay
        L2 = dx * dx + dy * dy or 1.0
        t = max(0.0, min(1.0, ((x - ax) * dx + (y - ay) * dy) / L2))
        px, py = ax + t * dx, ay + t * dy
        d = (x - px) ** 2 + (y - py) ** 2
        if d < best:
            best = d
    return math.sqrt(best)


# ---------------------------------------------------------------- defs
def defs():
    add("<defs>")
    add(f"""
<linearGradient id="land" x1="0" y1="0" x2="0.25" y2="1">
  <stop offset="0" stop-color="#3C8A57"/><stop offset="0.55" stop-color="#2E7A4D"/><stop offset="1" stop-color="#256C43"/>
</linearGradient>
<radialGradient id="warm" cx="0.12" cy="0.08" r="1.0">
  <stop offset="0" stop-color="#FFD9A0" stop-opacity="0.38"/><stop offset="0.5" stop-color="#FFC994" stop-opacity="0.10"/>
  <stop offset="1" stop-color="#FFC994" stop-opacity="0"/>
</radialGradient>
<linearGradient id="mist" x1="0" y1="0" x2="0" y2="1">
  <stop offset="0" stop-color="#EAF4F0" stop-opacity="0.96"/><stop offset="0.45" stop-color="#E4F1EC" stop-opacity="0.72"/>
  <stop offset="1" stop-color="#E4F1EC" stop-opacity="0"/>
</linearGradient>
<linearGradient id="header" x1="0" y1="0" x2="0" y2="1">
  <stop offset="0" stop-color="#4A3426"/><stop offset="1" stop-color="#2E2018"/>
</linearGradient>
<linearGradient id="bar" x1="0" y1="0" x2="0" y2="1">
  <stop offset="0" stop-color="#3C2A20"/><stop offset="1" stop-color="#1E140E"/>
</linearGradient>
<linearGradient id="btn" x1="0" y1="0" x2="0" y2="1">
  <stop offset="0" stop-color="{C['hoja_btn_a']}"/><stop offset="0.5" stop-color="{C['hoja_btn_b']}"/><stop offset="1" stop-color="{C['hoja_btn_c']}"/>
</linearGradient>
<linearGradient id="wood" x1="0" y1="0" x2="0" y2="1">
  <stop offset="0" stop-color="#9A6B48"/><stop offset="0.5" stop-color="#7E5638"/><stop offset="1" stop-color="#5A3D28"/>
</linearGradient>
<linearGradient id="encMask" x1="0" y1="0" x2="0" y2="1">
  <stop offset="0" stop-color="#fff" stop-opacity="0.15"/><stop offset="0.55" stop-color="#fff" stop-opacity="0.9"/>
  <stop offset="1" stop-color="#fff" stop-opacity="1"/>
</linearGradient>
<mask id="encFade" maskUnits="userSpaceOnUse" x="0" y="1780" width="{W}" height="{MAP_BOTTOM-1780}">
  <rect x="0" y="1780" width="{W}" height="{MAP_BOTTOM-1780}" fill="url(#encFadeG)"/>
</mask>
<linearGradient id="encFadeG" x1="0" y1="0" x2="0" y2="1">
  <stop offset="0" stop-color="#fff" stop-opacity="1"/><stop offset="1" stop-color="#fff" stop-opacity="0.55"/>
</linearGradient>
<radialGradient id="crown0" cx="0.36" cy="0.32" r="0.72">
  <stop offset="0" stop-color="#8FD468"/><stop offset="0.45" stop-color="#3E9A55"/><stop offset="1" stop-color="#1C5236"/>
</radialGradient>
<radialGradient id="crown1" cx="0.36" cy="0.32" r="0.72">
  <stop offset="0" stop-color="#A9DE6E"/><stop offset="0.45" stop-color="#4FA84E"/><stop offset="1" stop-color="#22603A"/>
</radialGradient>
<radialGradient id="crown2" cx="0.36" cy="0.32" r="0.72">
  <stop offset="0" stop-color="#7CC08A"/><stop offset="0.5" stop-color="#2F7F57"/><stop offset="1" stop-color="#163F2B"/>
</radialGradient>
<radialGradient id="crownFlower" cx="0.36" cy="0.32" r="0.72">
  <stop offset="0" stop-color="#FFE08A"/><stop offset="0.5" stop-color="#E9B23A"/><stop offset="1" stop-color="#8A6A1E"/>
</radialGradient>
<radialGradient id="crownPink" cx="0.36" cy="0.32" r="0.72">
  <stop offset="0" stop-color="#F7C2D6"/><stop offset="0.5" stop-color="#D9779D"/><stop offset="1" stop-color="#7E3A55"/>
</radialGradient>
<radialGradient id="capiBody" cx="0.35" cy="0.3" r="0.8">
  <stop offset="0" stop-color="#C59A62"/><stop offset="0.55" stop-color="#8B6A3E"/><stop offset="1" stop-color="#5C4426"/>
</radialGradient>
<radialGradient id="rosaBody" cx="0.35" cy="0.3" r="0.8">
  <stop offset="0" stop-color="#FBD6E1"/><stop offset="0.55" stop-color="#E8A0B4"/><stop offset="1" stop-color="#B27186"/>
</radialGradient>
<radialGradient id="tucuxiBody" cx="0.35" cy="0.3" r="0.8">
  <stop offset="0" stop-color="#D9E2EA"/><stop offset="0.55" stop-color="#8E9AA8"/><stop offset="1" stop-color="#55606E"/>
</radialGradient>
<linearGradient id="hull" x1="0" y1="0" x2="0" y2="1">
  <stop offset="0" stop-color="#C08A5A"/><stop offset="0.45" stop-color="#8A5C3A"/><stop offset="1" stop-color="#4E3220"/>
</linearGradient>
<linearGradient id="glassJar" x1="0" y1="0" x2="1" y2="0">
  <stop offset="0" stop-color="#FFFFFF" stop-opacity="0.85"/><stop offset="0.35" stop-color="#EAF7F3" stop-opacity="0.35"/>
  <stop offset="1" stop-color="#BFE3EA" stop-opacity="0.6"/>
</linearGradient>
<linearGradient id="dome" x1="0" y1="0" x2="0" y2="1">
  <stop offset="0" stop-color="#FFE27A"/><stop offset="0.5" stop-color="#3FA35A"/><stop offset="1" stop-color="#2D5FA8"/>
</linearGradient>
<radialGradient id="beacon" cx="0.5" cy="0.5" r="0.5">
  <stop offset="0" stop-color="#FFF6D6" stop-opacity="0.95"/><stop offset="0.4" stop-color="#FFD27A" stop-opacity="0.55"/>
  <stop offset="1" stop-color="#FFD27A" stop-opacity="0"/>
</radialGradient>
<filter id="blur2" x="-20%" y="-20%" width="140%" height="140%"><feGaussianBlur stdDeviation="2"/></filter>
<filter id="blur4" x="-30%" y="-30%" width="160%" height="160%"><feGaussianBlur stdDeviation="4"/></filter>
<filter id="blur10" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="10"/></filter>
<filter id="blur22" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="22"/></filter>
<filter id="shadow" x="-30%" y="-30%" width="160%" height="160%">
  <feDropShadow dx="0" dy="5" stdDeviation="4" flood-color="#0B2A1A" flood-opacity="0.45"/>
</filter>
<filter id="shadowSoft" x="-30%" y="-30%" width="160%" height="160%">
  <feDropShadow dx="0" dy="3" stdDeviation="3" flood-color="#0B2A1A" flood-opacity="0.35"/>
</filter>
<clipPath id="mapClip"><rect x="0" y="{HEADER}" width="{W}" height="{MAP_BOTTOM-HEADER}"/></clipPath>
""")
    add("</defs>")


# ---------------------------------------------------------------- tierra, várzea y relieve
def land():
    add(f'<rect x="0" y="{HEADER}" width="{W}" height="{MAP_BOTTOM-HEADER}" fill="url(#land)"/>')
    # várzea: banda clara junto al agua (se inunda en creciente: verde más claro)
    for line, hw in ((MAIN, HW_MAIN), (NEGRO, HW_NEGRO), (PURUS, HW_PURUS)):
        add(f'<path d="{path_d(line)}" fill="none" stroke="#5FA85A" stroke-opacity="0.55" '
            f'stroke-width="{fmt(hw*2+150)}" stroke-linecap="round" stroke-linejoin="round" filter="url(#blur22)"/>')
    # lago de Tefé
    lx, ly = TEFE_LAKE
    add(f'<ellipse cx="{lx}" cy="{ly}" rx="120" ry="58" fill="#5FA85A" opacity="0.5" filter="url(#blur10)"/>')


# ---------------------------------------------------------------- orillas
def banks_for(line, nrm, k, hwf, sand_side_sign=1.0, start=0, end=None, color_scale=1.0):
    end = len(line) if end is None else end
    n = len(line)
    # Playas en el lado interior de la curva, barrancos en el exterior (morfología real de un meandro).
    kmax = max(abs(v) for v in k) or 1.0
    # playa: sale donde la curva es fuerte
    i = start
    while i < end:
        side = 1 if k[i] > 0 else -1
        j = i
        while j < end and (1 if k[j] > 0 else -1) == side:
            j += 1
        seg_peak = max(abs(k[t]) for t in range(i, j)) if j > i else 0
        if j - i > 6 and seg_peak > 0.25 * kmax:
            def sw(t, i=i, j=j):
                u = (t - i) / max(j - i - 1, 1)
                bell = math.sin(math.pi * u) ** 1.4
                return bell * 46 * min(1.0, abs(k[t]) / (0.6 * kmax) + 0.25)

            # playa: lado interior (hacia el centro de curvatura)
            s_in = side * sand_side_sign
            poly = band(line, nrm, lambda t: s_in * (hwf(t) - 6), lambda t: s_in * (hwf(t) + sw(t)), i, j)
            add(f'<polygon points="{pts(poly)}" fill="{C["arena"]}"/>')
            poly2 = band(line, nrm, lambda t: s_in * (hwf(t) + sw(t) * 0.55), lambda t: s_in * (hwf(t) + sw(t)), i, j)
            add(f'<polygon points="{pts(poly2)}" fill="{C["arena_sombra"]}" opacity="0.55"/>')
            # barranco: lado exterior
            s_out = -s_in

            def bw(t, i=i, j=j):
                u = (t - i) / max(j - i - 1, 1)
                return (math.sin(math.pi * u) ** 0.8) * 22

            poly3 = band(line, nrm, lambda t: s_out * (hwf(t) - 4), lambda t: s_out * (hwf(t) + bw(t)), i, j)
            add(f'<polygon points="{pts(poly3)}" fill="{C["barranco"]}"/>')
            poly4 = band(line, nrm, lambda t: s_out * (hwf(t) + bw(t) * 0.6), lambda t: s_out * (hwf(t) + bw(t)), i, j)
            add(f'<polygon points="{pts(poly4)}" fill="{C["barranco_luz"]}"/>')
            # estratos del barranco
            for frac in (0.25, 0.45):
                seq = offset(line[i:j], nrm[i:j], lambda t, frac=frac, i=i: s_out * (hwf(t + i) + bw(t + i) * frac))
                add(f'<path d="{path_d(seq)}" fill="none" stroke="{C["barranco_sombra"]}" stroke-width="2" opacity="0.6"/>')
        i = j
    # borde de hierba/várzea en todo el cauce (fino, encima de nada)
    for s in (1, -1):
        seq = offset(line[start:end], nrm[start:end], lambda t, s=s: s * (hwf(t + start) + 2))
        add(f'<path d="{path_d(seq)}" fill="none" stroke="#7CC95A" stroke-width="5" opacity="0.55" stroke-linecap="round"/>')


# ---------------------------------------------------------------- agua
def water_polygon(line, nrm, hwf, i0=0, i1=None):
    return band(line, nrm, lambda t: hwf(t), lambda t: -hwf(t), i0, i1)


def water():
    n = len(MAIN)
    # cauce lleno (agua blanca del Solimões) desde abajo hasta la canoa; vidrio río arriba
    filled = water_polygon(MAIN, MAIN_N, hw_main, 0, I_CANOE + 1)
    glass = water_polygon(MAIN, MAIN_N, hw_main, I_CANOE, n)
    # afluentes: el Negro (lleno, ya recorrido) y el Purus (su boca, recién pasada: se llena entera)
    negro = water_polygon(NEGRO, NEGRO_N, lambda t: HW_NEGRO * (0.85 + 0.15 * t / len(NEGRO)))
    purus = water_polygon(PURUS, PURUS_N, lambda t: HW_PURUS * (0.8 + 0.2 * t / len(PURUS)))

    # vidrio (lo que falta: azul hielo con brillo, nunca seco; D-013 Q2-A)
    add(f'<polygon points="{pts(glass)}" fill="{C["vidrio"]}" opacity="0.82"/>')
    add(f'<polygon points="{pts(glass)}" fill="none" stroke="{C["vidrio_borde"]}" stroke-width="4" opacity="0.9"/>')
    seq = offset(MAIN[I_CANOE:], MAIN_N[I_CANOE:], lambda t: hw_main(t + I_CANOE) * 0.45)
    add(f'<path d="{path_d(seq)}" fill="none" stroke="#FFFFFF" stroke-width="6" opacity="0.55" stroke-dasharray="70 40" stroke-linecap="round"/>')
    seq = offset(MAIN[I_CANOE:], MAIN_N[I_CANOE:], lambda t: -hw_main(t + I_CANOE) * 0.55)
    add(f'<path d="{path_d(seq)}" fill="none" stroke="#B6DCE4" stroke-width="5" opacity="0.6" stroke-dasharray="30 60" stroke-linecap="round"/>')

    # Purus: agua blanca
    add(f'<polygon points="{pts(purus)}" fill="{C["rio_base"]}"/>')
    add(f'<path d="{path_d(PURUS)}" fill="none" stroke="{C["rio_profundo"]}" stroke-width="16" opacity="0.45" filter="url(#blur4)"/>')

    # cauce lleno: base, canal profundo, reflejo y brillos (luz arriba a la izquierda)
    add(f'<polygon points="{pts(filled)}" fill="{C["rio_base"]}"/>')
    add(f'<path d="{path_d(MAIN[:I_CANOE + 1])}" fill="none" stroke="{C["rio_profundo"]}" stroke-width="58" opacity="0.5" filter="url(#blur10)"/>')
    seq = offset(MAIN[:I_CANOE + 1], MAIN_N[:I_CANOE + 1], lambda t: -hw_main(t) * 0.62)
    add(f'<path d="{path_d(seq)}" fill="none" stroke="{C["rio_reflejo"]}" stroke-width="18" opacity="0.55" filter="url(#blur4)"/>')

    # Encuentro de las Aguas: el Negro corre al lado del Solimões sin mezclarse, río abajo de la boca
    add(f'<polygon points="{pts(negro)}" fill="{C["agua_negra"]}"/>')
    add(f'<path d="{path_d(NEGRO)}" fill="none" stroke="{C["agua_negra_luz"]}" stroke-width="20" opacity="0.45" filter="url(#blur4)"/>')
    # banda oscura en el lado derecho (norte) del cauce, con borde ondulado
    j0 = I_JUNCTION
    dark = band(MAIN, MAIN_N,
                lambda t: hw_main(t),
                lambda t, j0=j0: 8 * math.sin((t - j0) * 0.55) - 4,
                0, j0 + 3)
    add(f'<g mask="url(#encFade)"><polygon points="{pts(dark)}" fill="{C["agua_negra"]}" opacity="0.92"/></g>')
    seam = offset(MAIN[:j0 + 1], MAIN_N[:j0 + 1], lambda t, j0=j0: 8 * math.sin((t - j0) * 0.55) - 4)
    add(f'<path d="{path_d(seam)}" fill="none" stroke="#E7C38D" stroke-width="5" opacity="0.65" stroke-dasharray="14 10"/>')

    # corriente: chevrons claros que bajan (en el juego, sprites que avanzan por la polilínea; quietos con Reducir movimiento)
    for i in range(6, I_CANOE - 6, 15):
        p, q = MAIN[i], MAIN_N[i]
        a = MAIN[i - 3]
        dx, dy = p[0] - a[0], p[1] - a[1]
        L = math.hypot(dx, dy) or 1
        tx, ty = dx / L, dy / L
        off = -18 if (i // 15) % 2 else 22
        cx, cy = p[0] + q[0] * off, p[1] + q[1] * off
        w = 16
        p1 = (cx - q[0] * w - tx * 10, cy - q[1] * w - ty * 10)
        p2 = (cx + tx * 4, cy + ty * 4)
        p3 = (cx + q[0] * w - tx * 10, cy + q[1] * w - ty * 10)
        col = C["destello"] if i > j0 else "#E7C38D"
        add(f'<path d="M{fmt(p1[0])},{fmt(p1[1])} Q{fmt(p2[0])},{fmt(p2[1])} {fmt(p3[0])},{fmt(p3[1])}" fill="none" '
            f'stroke="{col}" stroke-width="4" stroke-linecap="round" opacity="0.7"/>')
    # destellos (sparkles) en la cara iluminada
    rnd = random.Random(7)
    for i in range(3, I_CANOE - 3, 7):
        p, q = MAIN[i], MAIN_N[i]
        o = -hw_main(i) * rnd.uniform(0.25, 0.8)
        x, y = p[0] + q[0] * o, p[1] + q[1] * o
        s = rnd.uniform(4, 8)
        add(f'<path d="M{fmt(x)},{fmt(y-s)} L{fmt(x+s*0.3)},{fmt(y-s*0.3)} L{fmt(x+s)},{fmt(y)} L{fmt(x+s*0.3)},{fmt(y+s*0.3)} '
            f'L{fmt(x)},{fmt(y+s)} L{fmt(x-s*0.3)},{fmt(y+s*0.3)} L{fmt(x-s)},{fmt(y)} L{fmt(x-s*0.3)},{fmt(y-s*0.3)}Z" '
            f'fill="{C["destello"]}" opacity="0.85"/>')
    # frente del agua: brillo donde el agua real se encuentra con el vidrio
    p, q = MAIN[I_CANOE], MAIN_N[I_CANOE]
    a = (p[0] + q[0] * hw_main(I_CANOE), p[1] + q[1] * hw_main(I_CANOE))
    b = (p[0] - q[0] * hw_main(I_CANOE), p[1] - q[1] * hw_main(I_CANOE))
    add(f'<line x1="{fmt(a[0])}" y1="{fmt(a[1])}" x2="{fmt(b[0])}" y2="{fmt(b[1])}" stroke="{C["destello"]}" stroke-width="10" '
        f'opacity="0.9" stroke-linecap="round" filter="url(#blur2)"/>')

    # plantas flotantes (capim) en los remansos del agua ya llena
    for (i, side, r) in ((I_CANOE - 30, 1, 16), (I_CANOE - 58, -1, 13), (I_CANOE - 90, -1, 18), (I_CANOE - 112, 1, 12)):
        if i < 0:
            continue
        p, q = MAIN[i], MAIN_N[i]
        o = side * (hw_main(i) - r * 0.6)
        x, y = p[0] + q[0] * o, p[1] + q[1] * o
        add(f'<ellipse cx="{fmt(x)}" cy="{fmt(y)}" rx="{r*1.6}" ry="{r*0.8}" fill="#6DBA4E" opacity="0.92"/>')
        add(f'<ellipse cx="{fmt(x-4)}" cy="{fmt(y-3)}" rx="{r*0.9}" ry="{r*0.4}" fill="{C["hoja_brillo"]}" opacity="0.7"/>')


# ---------------------------------------------------------------- vegetación en capas
TOWNS = {}  # nombre -> (x, y, radio libre)


def vegetation():
    rnd = random.Random(2026)
    trees = []
    palms = []
    step = 34
    for gy in range(HEADER - 20, MAP_BOTTOM + 40, step):
        for gx in range(-20, W + 40, step):
            x = gx + rnd.uniform(-12, 12)
            y = gy + rnd.uniform(-12, 12)
            dm = dist_to_line(x, y, MAIN) - HW_MAIN * 1.05
            dn = dist_to_line(x, y, NEGRO) - HW_NEGRO
            dp = dist_to_line(x, y, PURUS) - HW_PURUS
            d = min(dm, dn, dp)
            if d < 46:
                continue
            if any(math.hypot(x - tx, y - ty) < tr for tx, ty, tr in TOWNS.values()):
                continue
            lx, ly = TEFE_LAKE
            if ((x - lx) / 150) ** 2 + ((y - ly) / 80) ** 2 < 1:
                continue
            # cerca del agua: palmeras de açaí y árboles más bajos y claros (várzea); lejos: dosel denso y oscuro
            if d < 85 and rnd.random() < 0.38:
                palms.append((x, y))
                continue
            r = rnd.uniform(20, 30) if d > 120 else rnd.uniform(15, 22)
            g = rnd.choices(["crown0", "crown1", "crown2", "crownFlower", "crownPink"], [0.44, 0.22, 0.31, 0.015, 0.015])[0]
            if d < 120 and g == "crown2":
                g = "crown1"
            trees.append((y, x, r, g))
    trees.sort()
    # sombra de contacto de todo el dosel (una sola capa)
    for y, x, r, g in trees:
        add(f'<ellipse cx="{fmt(x+5)}" cy="{fmt(y+9)}" rx="{fmt(r*1.05)}" ry="{fmt(r*0.8)}" fill="#0E3322" opacity="0.38"/>')
    for y, x, r, g in trees:
        add(f'<circle cx="{fmt(x-r*0.62)}" cy="{fmt(y+r*0.22)}" r="{fmt(r*0.68)}" fill="url(#{g})"/>')
        add(f'<circle cx="{fmt(x+r*0.6)}" cy="{fmt(y+r*0.28)}" r="{fmt(r*0.64)}" fill="url(#{g})"/>')
        add(f'<circle cx="{fmt(x)}" cy="{fmt(y)}" r="{fmt(r)}" fill="url(#{g})"/>')
        add(f'<circle cx="{fmt(x-r*0.38)}" cy="{fmt(y-r*0.42)}" r="{fmt(r*0.2)}" fill="#FFFFFF" opacity="0.28"/>')
    for x, y in sorted(palms, key=lambda p: p[1]):
        palm(x, y, rnd)


def palm(x, y, rnd):
    h = rnd.uniform(26, 38)
    lean = rnd.uniform(-8, 8)
    add(f'<ellipse cx="{fmt(x+4)}" cy="{fmt(y+4)}" rx="9" ry="4" fill="#0E3322" opacity="0.35"/>')
    add(f'<path d="M{fmt(x)},{fmt(y)} Q{fmt(x+lean*0.3)},{fmt(y-h*0.5)} {fmt(x+lean)},{fmt(y-h)}" stroke="#7A5A3A" stroke-width="4" fill="none" stroke-linecap="round"/>')
    tx, ty = x + lean, y - h
    for a in range(7):
        ang = math.pi * (1.05 + a * 0.15) + rnd.uniform(-0.08, 0.08)
        ex, ey = tx + math.cos(ang) * 24, ty + math.sin(ang) * 14 + 8
        add(f'<path d="M{fmt(tx)},{fmt(ty)} Q{fmt((tx+ex)/2)},{fmt(ty-10)} {fmt(ex)},{fmt(ey)}" stroke="{C["hoja_media"] if a % 2 else "#3E9A55"}" '
            f'stroke-width="6" fill="none" stroke-linecap="round"/>')
    for a in range(2):
        ang = -math.pi * (0.25 + a * 0.5)
        ex, ey = tx + math.cos(ang) * 20, ty + math.sin(ang) * 12 + 2
        add(f'<path d="M{fmt(tx)},{fmt(ty)} Q{fmt((tx+ex)/2)},{fmt(ty-8)} {fmt(ex)},{fmt(ey)}" stroke="#5DBB3F" stroke-width="5" fill="none" stroke-linecap="round"/>')
    add(f'<circle cx="{fmt(tx)}" cy="{fmt(ty+2)}" r="3.5" fill="#4B1F4F"/>')


# ---------------------------------------------------------------- pueblos de paso (viñetas por código)
def house(x, y, w, h, wall, roof, stilts=0, door=True):
    if stilts:
        for sx in (x + 4, x + w / 2, x + w - 4):
            add(f'<line x1="{fmt(sx)}" y1="{fmt(y+h)}" x2="{fmt(sx)}" y2="{fmt(y+h+stilts)}" stroke="{C["muelle_sombra"]}" stroke-width="3"/>')
    add(f'<rect x="{fmt(x)}" y="{fmt(y)}" width="{fmt(w)}" height="{fmt(h)}" rx="3" fill="{wall}" filter="url(#shadowSoft)"/>')
    add(f'<rect x="{fmt(x)}" y="{fmt(y)}" width="{fmt(w*0.35)}" height="{fmt(h)}" rx="3" fill="#FFFFFF" opacity="0.22"/>')
    add(f'<path d="M{fmt(x-5)},{fmt(y+2)} L{fmt(x+w/2)},{fmt(y-h*0.62)} L{fmt(x+w+5)},{fmt(y+2)}Z" fill="{roof}"/>')
    add(f'<path d="M{fmt(x-5)},{fmt(y+2)} L{fmt(x+w/2)},{fmt(y-h*0.62)} L{fmt(x+w/2)},{fmt(y+2)}Z" fill="#FFFFFF" opacity="0.18"/>')
    if door:
        add(f'<rect x="{fmt(x+w*0.4)}" y="{fmt(y+h*0.45)}" width="{fmt(w*0.22)}" height="{fmt(h*0.55)}" rx="2" fill="#5A3D28"/>')


def pier(x, y, length, ang_deg):
    add(f'<g transform="translate({fmt(x)},{fmt(y)}) rotate({fmt(ang_deg)})">'
        f'<rect x="0" y="-6" width="{fmt(length)}" height="12" rx="2" fill="url(#wood)" filter="url(#shadowSoft)"/>'
        + "".join(f'<line x1="{fmt(t)}" y1="-6" x2="{fmt(t)}" y2="6" stroke="{C["muelle_sombra"]}" stroke-width="1.5"/>' for t in range(8, int(length), 9))
        + f'<line x1="{fmt(length-3)}" y1="6" x2="{fmt(length-3)}" y2="16" stroke="{C["muelle_sombra"]}" stroke-width="3"/></g>')


def riverboat(x, y, s=1.0, flip=False):
    """Barco de línea (recreio): dos pisos, blanco con franja azul. Sin personas (VA-22)."""
    t = f"translate({fmt(x)},{fmt(y)}) scale({fmt(-s if flip else s)},{fmt(s)})"
    add(f'<g transform="{t}" filter="url(#shadowSoft)">'
        f'<path d="M-46,0 L46,0 L38,14 L-40,14Z" fill="#F5F1E8"/>'
        f'<rect x="-46" y="2" width="92" height="5" fill="#2F6FB0"/>'
        f'<rect x="-36" y="-16" width="70" height="16" rx="2" fill="#FFFFFF"/>'
        f'<rect x="-30" y="-12" width="58" height="6" fill="#9FD3E8"/>'
        f'<rect x="-26" y="-28" width="50" height="12" rx="2" fill="#FFFFFF"/>'
        f'<rect x="-22" y="-25" width="42" height="4" fill="#9FD3E8"/>'
        f'<rect x="-30" y="-31" width="62" height="4" rx="1" fill="#2F6FB0"/>'
        f'<rect x="20" y="-40" width="6" height="10" fill="#C83A2E"/>'
        f'</g>')


def chip(x, y, text, kind="pueblo", sub=None, anchor="middle"):
    """Rótulo de un sitio. pueblo: crema; parada: madera; proximo: crema con borde verde."""
    fs = 30 if kind != "parada" else 32
    tw = len(text) * fs * 0.52 + 44
    if sub:
        tw = max(tw, len(sub) * 22 * 0.5 + 44)
    h = 52 if not sub else 84
    if anchor == "start":
        x0 = x
    elif anchor == "end":
        x0 = x - tw
    else:
        x0 = x - tw / 2
    if kind == "parada":
        fill, stroke, col, scol = C["madera_oscura"], C["fibra"], C["crema"], "#EED29A"
    elif kind == "proximo":
        fill, stroke, col, scol = C["crema"], "#2F7D32", C["tinta"], "#5A3D28"
    else:
        fill, stroke, col, scol = C["crema"], C["marco"], C["tinta"], "#5A3D28"
    add(f'<g filter="url(#shadow)"><rect x="{fmt(x0)}" y="{fmt(y)}" width="{fmt(tw)}" height="{h}" rx="{h/2 if not sub else 22}" '
        f'fill="{fill}" stroke="{stroke}" stroke-width="4"/></g>')
    add(f'<text x="{fmt(x0+tw/2)}" y="{fmt(y+37)}" text-anchor="middle" font-family="&apos;Baloo 2&apos;, &apos;Segoe UI&apos;, sans-serif" font-weight="800" '
        f'font-size="{fs}" fill="{col}">{text}</text>')
    if sub:
        add(f'<text x="{fmt(x0+tw/2)}" y="{fmt(y+70)}" text-anchor="middle" font-family="Nunito, &apos;Segoe UI&apos;, sans-serif" font-weight="800" '
            f'font-size="22" fill="{scol}">{sub}</text>')
    return x0, tw


def town_manaus(cx, cy):
    """Manaos (parada ya alcanzada, abajo): el Teatro Amazonas con su cúpula de tejas y el puerto."""
    # puerto y barcos
    pier(cx - 60, cy + 70, 70, 150)
    riverboat(cx - 150, cy + 128, 0.95)
    riverboat(cx + 10, cy + 116, 0.8, flip=True)
    # casas
    for i, (dx, dy, col, roof) in enumerate(((-120, 10, "#FFE4A8", "#9E3B2E"), (90, 20, "#8ADBE6", "#C98A2B"),
                                              (130, -30, "#F3AE8A", "#9E3B2E"), (-80, -40, "#FFF8E6", "#C98A2B"))):
        house(cx + dx, cy + dy, 34, 26, col, roof)
    # Teatro Amazonas (07 §2.1: héroe del sitio). Rosa viejo con columnas y cúpula de tejas verdes, amarillas y azules
    tx, ty = cx - 10, cy - 30
    add(f'<g filter="url(#shadow)">'
        f'<rect x="{tx-50}" y="{ty}" width="100" height="44" rx="3" fill="#EDB8A6"/>'
        f'<rect x="{tx-50}" y="{ty}" width="34" height="44" rx="3" fill="#FFFFFF" opacity="0.25"/>'
        + "".join(f'<rect x="{tx-42+i*16}" y="{ty+12}" width="6" height="30" fill="#FFF8E6"/>' for i in range(6))
        + f'<path d="M{tx-56},{ty+2} L{tx},{ty-18} L{tx+56},{ty+2}Z" fill="#F6D2C4"/>'
        f'<path d="M{tx-26},{ty-12} Q{tx},{ty-74} {tx+26},{ty-12}Z" fill="url(#dome)"/>'
        f'<path d="M{tx-18},{ty-16} Q{tx-6},{ty-62} {tx},{ty-62}" stroke="#FFFFFF" stroke-width="4" fill="none" opacity="0.55"/>'
        f'<rect x="{tx-3}" y="{ty-80}" width="6" height="14" fill="#FFF8E6"/>'
        f'</g>')


def town_manacapuru(cx, cy):
    """Pueblo de paso ya pasado: palafitos sobre la orilla y un muelle."""
    pier(cx - 20, cy + 40, 60, 200)
    house(cx - 50, cy, 30, 22, "#FFE4A8", "#9E3B2E", stilts=14)
    house(cx - 12, cy - 8, 32, 24, "#8ADBE6", "#C98A2B", stilts=16)
    house(cx + 28, cy + 4, 28, 20, "#FFF8E6", "#9E3B2E", stilts=12)
    # canoa amarrada (sin nadie)
    add(f'<path d="M{cx-90},{cy+62} q30,14 60,0 l-6,8 q-24,8 -48,0z" fill="url(#hull)"/>')


def town_coari(cx, cy):
    """Pueblo de paso próximo: iglesia blanca, casas y un barco de línea en el muelle."""
    pier(cx + 60, cy + 30, 70, -20)
    riverboat(cx + 150, cy + 6, 0.75, flip=True)
    house(cx - 70, cy + 10, 30, 22, "#F3AE8A", "#9E3B2E")
    house(cx + 40, cy + 18, 30, 22, "#FFE4A8", "#C98A2B")
    house(cx - 30, cy + 34, 30, 22, "#8ADBE6", "#9E3B2E")
    # iglesia
    add(f'<g filter="url(#shadow)"><rect x="{cx-12}" y="{cy-30}" width="40" height="44" rx="3" fill="#FFF8E6"/>'
        f'<rect x="{cx-12}" y="{cy-30}" width="14" height="44" rx="3" fill="#FFFFFF"/>'
        f'<rect x="{cx-2}" y="{cy-64}" width="20" height="36" rx="2" fill="#FFF8E6"/>'
        f'<path d="M{cx-4},{cy-64} L{cx+8},{cy-86} L{cx+20},{cy-64}Z" fill="#2F6FB0"/>'
        f'<rect x="{cx+5}" y="{cy-56}" width="6" height="9" rx="3" fill="#5A3D28"/>'
        f'<rect x="{cx+2}" y="{cy-6}" width="12" height="20" rx="6" fill="#5A3D28"/></g>')


def town_tefe(cx, cy):
    """Cierre de mundo (el «castillo»): Tefé con su lago, la baliza y el premio del mundo."""
    lx, ly = TEFE_LAKE
    add(f'<ellipse cx="{lx}" cy="{ly}" rx="112" ry="48" fill="{C["vidrio"]}" opacity="0.85" stroke="{C["vidrio_borde"]}" stroke-width="4"/>')
    add(f'<path d="M{lx-70},{ly-8} q40,-14 90,-6" stroke="#FFFFFF" stroke-width="5" opacity="0.7" fill="none" stroke-linecap="round"/>')
    for dx, dy, col, roof in ((40, -64, "#FFE4A8", "#9E3B2E"), (80, -50, "#FFF8E6", "#C98A2B"), (0, -70, "#8ADBE6", "#9E3B2E")):
        house(lx + dx, ly + dy, 28, 20, col, roof)
    # baliza (cierre de mundo)
    bx, by = cx, cy
    add(f'<circle cx="{bx}" cy="{by-58}" r="70" fill="url(#beacon)"/>')
    add(f'<g filter="url(#shadow)"><path d="M{bx-14},{by} L{bx-8},{by-60} L{bx+8},{by-60} L{bx+14},{by}Z" fill="#FFF8E6"/>'
        f'<rect x="{bx-11}" y="{by-40}" width="22" height="9" fill="{C["heliconia"]}"/>'
        f'<rect x="{bx-9}" y="{by-20}" width="18" height="8" fill="{C["heliconia"]}"/>'
        f'<rect x="{bx-12}" y="{by-74}" width="24" height="16" rx="3" fill="#FFD27A"/>'
        f'<path d="M{bx-14},{by-74} L{bx},{by-88} L{bx+14},{by-74}Z" fill="{C["marco"]}"/></g>')


def mini_cuia(x, y, fill=None, scale=1.0, glow=False):
    """Muestra del río (D-013 Q9-A): cuia de vidrio en miniatura con el agua del sitio."""
    s = scale
    if glow:
        add(f'<circle cx="{fmt(x)}" cy="{fmt(y-30*s)}" r="{fmt(46*s)}" fill="url(#beacon)"/>')
    body = (f'M{fmt(x-16*s)},{fmt(y-6*s)} Q{fmt(x-22*s)},{fmt(y-40*s)} {fmt(x-10*s)},{fmt(y-50*s)} L{fmt(x-9*s)},{fmt(y-60*s)} '
            f'L{fmt(x+9*s)},{fmt(y-60*s)} L{fmt(x+10*s)},{fmt(y-50*s)} Q{fmt(x+22*s)},{fmt(y-40*s)} {fmt(x+16*s)},{fmt(y-6*s)}Z')
    add(f'<g filter="url(#shadowSoft)">')
    if fill:
        add(f'<path d="M{fmt(x-16*s)},{fmt(y-6*s)} Q{fmt(x-21*s)},{fmt(y-30*s)} {fmt(x-17*s)},{fmt(y-34*s)} L{fmt(x+17*s)},{fmt(y-34*s)} '
            f'Q{fmt(x+21*s)},{fmt(y-30*s)} {fmt(x+16*s)},{fmt(y-6*s)}Z" fill="{fill}"/>')
    add(f'<path d="{body}" fill="url(#glassJar)" stroke="{C["vidrio_borde"]}" stroke-width="{fmt(3*s)}"/>')
    add(f'<rect x="{fmt(x-18*s)}" y="{fmt(y-8*s)}" width="{fmt(36*s)}" height="{fmt(9*s)}" rx="{fmt(3*s)}" fill="{C["fibra"]}"/>')
    add(f'<rect x="{fmt(x-11*s)}" y="{fmt(y-64*s)}" width="{fmt(22*s)}" height="{fmt(6*s)}" rx="{fmt(2*s)}" fill="{C["fibra"]}"/>')
    add(f'<path d="M{fmt(x-10*s)},{fmt(y-44*s)} Q{fmt(x-14*s)},{fmt(y-28*s)} {fmt(x-10*s)},{fmt(y-14*s)}" stroke="#FFFFFF" '
        f'stroke-width="{fmt(3*s)}" fill="none" opacity="0.8" stroke-linecap="round"/>')
    add("</g>")


# ---------------------------------------------------------------- boyas (una por nivel del tramo 71-80)
def buoys():
    i0, i1 = I_JUNCTION - 4, I_TEFE + 6
    idx = [round(i0 + (i1 - i0) * k / 9) for k in range(10)]
    for k, i in enumerate(idx):
        p, q = MAIN[i], MAIN_N[i]
        o = hw_main(i) * 0.78 * (1 if k % 2 == 0 else -1)
        x, y = p[0] + q[0] * o, p[1] + q[1] * o
        lit = k < 5
        if lit:
            add(f'<g filter="url(#shadowSoft)"><ellipse cx="{fmt(x)}" cy="{fmt(y+6)}" rx="13" ry="5" fill="#000" opacity="0.18"/>'
                f'<path d="M{fmt(x-11)},{fmt(y+4)} Q{fmt(x)},{fmt(y-30)} {fmt(x+11)},{fmt(y+4)}Z" fill="{C["heliconia"]}"/>'
                f'<rect x="{fmt(x-10)}" y="{fmt(y-8)}" width="20" height="6" fill="#FFF8E6"/>'
                f'<circle cx="{fmt(x-4)}" cy="{fmt(y-14)}" r="3" fill="#FFFFFF" opacity="0.7"/></g>')
        else:
            add(f'<g opacity="0.85"><path d="M{fmt(x-10)},{fmt(y+4)} Q{fmt(x)},{fmt(y-26)} {fmt(x+10)},{fmt(y+4)}Z" fill="#EAF7F3" '
                f'fill-opacity="0.5" stroke="#FFFFFF" stroke-width="2.5"/></g>')


# ---------------------------------------------------------------- personajes (juguete brillante, D-058)
def capi_canoe(x, y, ang=-14):
    """Capi rema la canoa (D-013 Q3-A), más grande (60 dp), con las cuias atadas, la bandera de cortesía y una emoción."""
    add(f'<g transform="translate({fmt(x)},{fmt(y)}) rotate({ang}) scale(1.18)">')
    # estela (río abajo)
    add('<path d="M-70,22 L-150,46 M-60,30 L-140,70 M70,24 L150,52" stroke="#FFF4D2" stroke-width="5" opacity="0.75" stroke-linecap="round"/>')
    add('<ellipse cx="0" cy="26" rx="104" ry="16" fill="#3E2A16" opacity="0.28"/>')
    # remo (detrás) entrando al agua con salpicadura
    add('<line x1="18" y1="-58" x2="70" y2="34" stroke="#7A5A3A" stroke-width="7" stroke-linecap="round"/>')
    add('<ellipse cx="72" cy="38" rx="14" ry="7" fill="#C49A5C"/>')
    add('<g fill="#FFF4D2" opacity="0.95"><circle cx="90" cy="30" r="5"/><circle cx="98" cy="20" r="3.5"/><circle cx="82" cy="18" r="3"/></g>')
    # mástil y bandera de Brasil (cortesía, D-013 Q8-A)
    add('<line x1="-66" y1="4" x2="-66" y2="-96" stroke="#5A3D28" stroke-width="5" stroke-linecap="round"/>')
    add('<g transform="translate(-64,-96)"><rect x="0" y="0" width="54" height="36" rx="3" fill="#169B3A"/>'
        '<path d="M27,4 L50,18 L27,32 L4,18Z" fill="#FEDF00"/><circle cx="27" cy="18" r="8.5" fill="#1F3E8A"/>'
        '<rect x="0" y="0" width="54" height="12" rx="3" fill="#FFFFFF" opacity="0.18"/></g>')
    # casco
    add('<path d="M-100,0 Q-88,30 -40,34 L40,34 Q88,30 104,-2 Q60,10 0,10 Q-60,10 -100,0Z" fill="url(#hull)" filter="url(#shadowSoft)"/>')
    add('<path d="M-96,2 Q-60,12 0,12 Q60,12 100,0" stroke="#E2B07A" stroke-width="4" fill="none" opacity="0.8"/>')
    # cuias atadas con cuerda (popa)
    for i, col in enumerate(("#4B1F4F", "#F2B33D", "#C8283C")):
        cx = -52 + i * 22
        add(f'<g transform="translate({cx},6)"><path d="M-9,0 Q-12,-18 -6,-24 L-5,-30 L5,-30 L6,-24 Q12,-18 9,0Z" fill="{col}" opacity="0.9"/>'
            f'<path d="M-9,0 Q-12,-18 -6,-24 L-5,-30 L5,-30 L6,-24 Q12,-18 9,0Z" fill="url(#glassJar)" stroke="#EAF7F3" stroke-width="2"/>'
            f'<rect x="-10" y="-2" width="20" height="5" rx="2" fill="#C49A5C"/></g>')
    add('<path d="M-64,-8 Q-41,-2 -18,-8" stroke="#EED29A" stroke-width="3" fill="none"/>')
    # Capi (capibara, juguete brillante): cuerpo, cabeza, hocico, ojo con dos reflejos, oreja, brazo con el remo
    add('<ellipse cx="22" cy="-18" rx="34" ry="28" fill="url(#capiBody)"/>')
    add('<ellipse cx="22" cy="-6" rx="20" ry="12" fill="#C8A872" opacity="0.8"/>')
    add('<g transform="translate(36,-56) rotate(8)">'
        '<rect x="-28" y="-22" width="62" height="44" rx="20" fill="url(#capiBody)"/>'
        '<rect x="14" y="-12" width="26" height="30" rx="12" fill="#7A5A34"/>'
        '<ellipse cx="34" cy="-2" rx="4" ry="3" fill="#2B2118"/>'
        '<ellipse cx="-16" cy="-22" rx="8" ry="7" fill="#6E5230"/><ellipse cx="-16" cy="-22" rx="4" ry="3.5" fill="#4A3222"/>'
        '<ellipse cx="6" cy="-6" rx="6" ry="7" fill="#1E140E"/><circle cx="8" cy="-9" r="2.4" fill="#FFFFFF"/><circle cx="4" cy="-3" r="1.2" fill="#FFFFFF"/>'
        '<path d="M14,10 q6,5 12,1" stroke="#2B2118" stroke-width="2.4" fill="none" stroke-linecap="round"/>'
        '<ellipse cx="-4" cy="6" rx="6" ry="3.5" fill="#E79A7E" opacity="0.55"/>'
        '<path d="M-22,-14 Q-6,-26 16,-18" stroke="#FFFFFF" stroke-width="4" fill="none" opacity="0.35" stroke-linecap="round"/>'
        '</g>')
    add('<path d="M30,-24 Q44,-30 46,-40" stroke="#7A5A34" stroke-width="11" fill="none" stroke-linecap="round"/>')
    add('<circle cx="47" cy="-42" r="7" fill="#8B6A3E"/>')
    add('</g>')
    # emoción (sin texto: Capi no habla en el Mapa; D-013 Q3-A)
    ex, ey = x - 95, y - 150
    add(f'<g filter="url(#shadow)"><path d="M{ex-38},{ey-30} h76 a14,14 0 0 1 14,14 v34 a14,14 0 0 1 -14,14 h-44 l-14,16 l-2,-16 h-16 '
        f'a14,14 0 0 1 -14,-14 v-34 a14,14 0 0 1 14,-14z" fill="#FFF8E6" stroke="{C["marco"]}" stroke-width="3"/></g>')
    add(f'<text x="{ex}" y="{ey+14}" text-anchor="middle" font-family="&apos;Segoe UI Symbol&apos;, sans-serif" font-size="40" fill="{C["marco"]}">♪</text>')
    add(f'<text x="{ex+26}" y="{ey+4}" text-anchor="middle" font-family="&apos;Segoe UI Symbol&apos;, sans-serif" font-size="26" fill="{C["heliconia"]}">♥</text>')


def dolphin(x, y, scale, grad, belly, ang, splash=True):
    add(f'<g transform="translate({fmt(x)},{fmt(y)}) rotate({ang}) scale({scale})">')
    if splash:
        add('<g fill="#FFF4D2" opacity="0.9"><ellipse cx="-62" cy="26" rx="22" ry="7"/><circle cx="-74" cy="12" r="5"/>'
            '<circle cx="-52" cy="8" r="4"/><circle cx="-84" cy="20" r="3"/></g>')
    add(f'<path d="M-60,14 Q-30,-30 30,-24 Q58,-20 74,-6 L96,-4 Q98,2 92,4 L72,6 Q50,24 10,22 Q-30,22 -60,14Z" fill="url(#{grad})" filter="url(#shadowSoft)"/>')
    add(f'<path d="M-40,14 Q0,26 60,8" stroke="{belly}" stroke-width="9" fill="none" opacity="0.8" stroke-linecap="round"/>')
    add('<path d="M0,-24 Q8,-42 20,-26Z" fill="url(#' + grad + ')"/>')
    add('<path d="M-60,14 L-84,-2 L-78,16 L-86,30Z" fill="url(#' + grad + ')"/>')
    add('<ellipse cx="62" cy="-8" rx="4.5" ry="5" fill="#1E140E"/><circle cx="63.5" cy="-10" r="1.8" fill="#FFF"/>')
    add('<path d="M-30,-14 Q10,-30 50,-18" stroke="#FFFFFF" stroke-width="5" fill="none" opacity="0.45" stroke-linecap="round"/>')
    add('</g>')


def sauim(x, y):
    """Cameo de miquito (D-010: en su árbol, nunca guía): el sauim-de-coleira, el de Manaos."""
    add(f'<g transform="translate({x},{y})">'
        f'<path d="M14,10 q18,14 6,34" stroke="#6E5230" stroke-width="4" fill="none" stroke-linecap="round"/>'
        f'<ellipse cx="0" cy="12" rx="12" ry="11" fill="#8A6A44"/>'
        f'<ellipse cx="0" cy="2" rx="14" ry="7" fill="#FFF8E6"/>'
        f'<circle cx="0" cy="-10" r="12" fill="#1E140E"/>'
        f'<circle cx="-4" cy="-12" r="2.6" fill="#FFF"/><circle cx="5" cy="-12" r="2.6" fill="#FFF"/>'
        f'<circle cx="-4" cy="-12" r="1.2" fill="#000"/><circle cx="5" cy="-12" r="1.2" fill="#000"/>'
        f'<path d="M-12,4 l-14,-14" stroke="#1E140E" stroke-width="5" stroke-linecap="round"/>'
        f'</g>')


# ---------------------------------------------------------------- primer plano (parallax) y niebla
def leaf(x, y, L, wdt, ang, col="#145A35", vein="#2C7A4B"):
    add(f'<g transform="translate({fmt(x)},{fmt(y)}) rotate({ang})">'
        f'<path d="M0,0 Q{fmt(L*0.4)},{fmt(-wdt)} {fmt(L)},0 Q{fmt(L*0.4)},{fmt(wdt)} 0,0Z" fill="{col}"/>'
        f'<path d="M0,0 L{fmt(L*0.95)},0" stroke="{vein}" stroke-width="3"/>'
        f'<path d="M{fmt(L*0.15)},{fmt(-wdt*0.25)} Q{fmt(L*0.45)},{fmt(-wdt*0.8)} {fmt(L*0.8)},{fmt(-wdt*0.2)}" stroke="#5DBB3F" stroke-width="3" fill="none" opacity="0.5"/>'
        f'</g>')


def foreground():
    add('<g filter="url(#blur2)">')
    # abajo a la izquierda
    for (x, y, L, w, a, c) in ((-30, 2000, 230, 60, -58, "#0F4A2B"), (-40, 1940, 200, 52, -28, "#145A35"),
                               (-20, 2010, 170, 46, -82, "#1C5236"), (40, 2016, 150, 40, -100, "#145A35")):
        leaf(x, y, L, w, a, c)
    add(f'<path d="M30,1990 q30,-60 10,-110" stroke="#E2483A" stroke-width="18" fill="none" stroke-linecap="round"/>')
    add(f'<path d="M30,1990 q30,-60 10,-110" stroke="#FFD27A" stroke-width="6" fill="none" stroke-linecap="round" opacity="0.7"/>')
    # arriba a la derecha
    for (x, y, L, w, a, c) in ((1110, 190, 240, 62, 150, "#0F4A2B"), (1110, 250, 190, 50, 170, "#145A35"),
                               (1100, 170, 170, 44, 120, "#1C5236")):
        leaf(x, y, L, w, a, c)
    add("</g>")


def mist_and_light():
    # sombras de nubes que derivan
    add('<ellipse cx="300" cy="1300" rx="260" ry="120" fill="#0B2A1A" opacity="0.10" filter="url(#blur22)"/>')
    add('<ellipse cx="820" cy="560" rx="220" ry="100" fill="#0B2A1A" opacity="0.08" filter="url(#blur22)"/>')
    # luz de la tarde (mundo 3) desde arriba a la izquierda
    add(f'<rect x="0" y="{HEADER}" width="{W}" height="{MAP_BOTTOM-HEADER}" fill="url(#warm)"/>')
    # los Andes, lejos, tras la niebla (lo que falta por descubrir)
    add('<path d="M0,420 L120,330 L190,370 L300,270 L380,330 L470,250 L560,320 L660,240 L760,310 L860,260 L960,330 L1080,280 L1080,470 L0,470Z" '
        'fill="#9CC3C0" opacity="0.55"/>')
    add('<path d="M300,270 L330,292 L312,296 Z M660,240 L690,265 L668,268Z M470,250 L494,270 L478,272Z" fill="#FFFFFF" opacity="0.8"/>')
    add(f'<rect x="0" y="{HEADER}" width="{W}" height="560" fill="url(#mist)"/>')


# ---------------------------------------------------------------- interfaz (cabecera, controles)
def ui():
    # cabecera de madera
    add(f'<rect x="0" y="0" width="{W}" height="{HEADER}" fill="url(#header)"/>')
    add(f'<rect x="0" y="{HEADER-8}" width="{W}" height="8" fill="#1E140E" opacity="0.6"/>')
    add('<g filter="url(#shadow)"><circle cx="96" cy="112" r="50" fill="url(#wood)" stroke="#2E2018" stroke-width="4"/></g>')
    add('<path d="M120,112 H78 M96,90 L74,112 L96,134" stroke="#FFF8E6" stroke-width="10" fill="none" stroke-linecap="round" stroke-linejoin="round"/>')
    add('<text x="180" y="98" font-family="&apos;Baloo 2&apos;, &apos;Segoe UI&apos;, sans-serif" font-weight="800" font-size="40" fill="#FFF8E6">Manaos · ¿Por qué el río Negro</text>')
    add('<text x="180" y="150" font-family="&apos;Baloo 2&apos;, &apos;Segoe UI&apos;, sans-serif" font-weight="800" font-size="40" fill="#FFF8E6">es negro?</text>')
    # chip de progreso del tramo
    add('<g filter="url(#shadow)"><rect x="620" y="226" width="430" height="64" rx="32" fill="#36261C" stroke="#C49A5C" stroke-width="3"/></g>')
    add('<text x="835" y="268" text-anchor="middle" font-family="Nunito, &apos;Segoe UI&apos;, sans-serif" font-weight="800" font-size="28" fill="#FFF8E6">Canasta · Tefé 5/10</text>')
    # ventanita de casa de TiTino (no viaja; D-013)
    add('<g filter="url(#shadow)"><rect x="40" y="1760" width="150" height="150" rx="22" fill="#36261C"/>'
        '<rect x="52" y="1772" width="126" height="126" rx="14" fill="#BFE3EA"/></g>')
    add('<rect x="52" y="1772" width="126" height="40" rx="10" fill="#DDEFF3"/>')
    add('<ellipse cx="115" cy="1862" rx="42" ry="40" fill="#9A8C78"/><ellipse cx="115" cy="1856" rx="26" ry="22" fill="#E9DCC2"/>'
        '<ellipse cx="104" cy="1852" rx="8" ry="6" fill="#4A3A2A"/><ellipse cx="126" cy="1852" rx="8" ry="6" fill="#4A3A2A"/>'
        '<circle cx="105" cy="1851" r="2.5" fill="#FFF"/><circle cx="127" cy="1851" r="2.5" fill="#FFF"/>'
        '<ellipse cx="115" cy="1864" rx="5" ry="3.5" fill="#2B2118"/>'
        '<path d="M106,1872 q9,6 18,0" stroke="#2B2118" stroke-width="2.5" fill="none"/>')
    add('<path d="M52,1790 q30,30 10,70" stroke="#2C7A4B" stroke-width="12" fill="none"/><path d="M178,1790 q-30,30 -10,70" stroke="#2C7A4B" stroke-width="12" fill="none"/>')
    add('<g filter="url(#shadow)"><rect x="40" y="1922" width="330" height="56" rx="28" fill="#36261C" stroke="#C49A5C" stroke-width="3"/></g>')
    add('<text x="205" y="1960" text-anchor="middle" font-family="Nunito, &apos;Segoe UI&apos;, sans-serif" font-weight="800" font-size="27" fill="#FFF8E6">Casa · a unos 1.900 km</text>')
    # barra inferior
    add(f'<path d="M0,{MAP_BOTTOM} h{W} v{H-MAP_BOTTOM} h-{W}z" fill="url(#bar)"/>')
    add(f'<rect x="0" y="{MAP_BOTTOM}" width="{W}" height="6" fill="#C49A5C" opacity="0.6"/>')
    # Mundo | Cuenca
    add('<rect x="48" y="2046" width="440" height="100" rx="50" fill="#7C4E3A"/>'
        '<rect x="58" y="2056" width="210" height="80" rx="40" fill="#FFF8E6"/>')
    add('<text x="163" y="2108" text-anchor="middle" font-family="Nunito, &apos;Segoe UI&apos;, sans-serif" font-weight="800" font-size="34" fill="#2B2118">Mundo</text>')
    add('<text x="378" y="2108" text-anchor="middle" font-family="Nunito, &apos;Segoe UI&apos;, sans-serif" font-weight="800" font-size="34" fill="#FFF8E6">Cuenca</text>')
    # Muestrario
    add('<g filter="url(#shadow)"><rect x="530" y="2046" width="500" height="100" rx="50" fill="url(#wood)" stroke="#2E2018" stroke-width="3"/></g>')
    add('<text x="800" y="2110" text-anchor="middle" font-family="&apos;Baloo 2&apos;, &apos;Segoe UI&apos;, sans-serif" font-weight="800" font-size="36" fill="#FFF8E6">Muestrario · 8/73</text>')
    mini_cuia(588, 2122, fill="#522500", scale=0.95)
    # Jugar
    add('<g filter="url(#shadow)"><rect x="100" y="2178" width="880" height="122" rx="61" fill="url(#btn)" stroke="#17501E" stroke-width="4"/></g>')
    add('<rect x="130" y="2186" width="820" height="40" rx="20" fill="#FFFFFF" opacity="0.18"/>')
    add('<text x="540" y="2262" text-anchor="middle" font-family="&apos;Baloo 2&apos;, &apos;Segoe UI&apos;, sans-serif" font-weight="800" font-size="58" fill="#FFF8E6" '
        'stroke="#17501E" stroke-width="3" paint-order="stroke">Jugar · Nivel 76</text>')


# ---------------------------------------------------------------- escena
def scene():
    # pueblos: posiciones (reservan sitio para que no crezca selva encima)
    TOWNS["manaus"] = (880, 1610, 170)
    TOWNS["manacapuru"] = (640, 1420, 95)
    TOWNS["coari"] = (480, 975, 120)
    TOWNS["tefe"] = (280, 560, 150)
    TOWNS["tefe_b"] = (300, 690, 70)
    TOWNS["mono"] = (235, 1650, 34)
    TOWNS["casa"] = (110, 1850, 130)
    TOWNS["chip"] = (835, 258, 120)

    add(f'<g clip-path="url(#mapClip)">')
    land()
    banks_for(MAIN, MAIN_N, MAIN_K, hw_main)
    banks_for(NEGRO, NEGRO_N, curvature(NEGRO), lambda t: HW_NEGRO * (0.85 + 0.15 * t / len(NEGRO)))
    water()
    vegetation()

    # pueblos de paso y paradas
    town_manaus(880, 1625)
    town_manacapuru(640, 1425)
    town_coari(470, 985)
    town_tefe(330, 690)
    sauim(235, 1650)

    # marcadores de parada: Manaos (alcanzada, agua negra) y Tefé (próximo cierre, vidrio con brillo)
    mini_cuia(1010, 1575, fill="#522500", scale=1.15)
    mini_cuia(410, 690, fill=None, scale=1.25, glow=True)

    buoys()

    # Rosa delante, en el frente del agua; el tucuxi se une junto a la canoa
    dolphin(700, 1050, 0.95, "rosaBody", "#F3D3DB", -62)
    dolphin(330, 1270, 0.72, "tucuxiBody", "#F1D9DE", -28)
    capi_canoe(500, 1215)

    mist_and_light()
    foreground()
    add("</g>")

    # rótulos (encima de la niebla y del primer plano)
    chip(1050, 1395, "Manaos", kind="parada", sub="Brasil · río Negro", anchor="end")
    chip(690, 1290, "Manacapuru", kind="pueblo", anchor="start")
    chip(560, 880, "Coari", kind="proximo", sub="pueblo de paso · nivel 78")
    chip(470, 600, "Tefé", kind="parada", sub="Premio del mundo · faltan 5", anchor="start")
    # el tucuxi se une
    add('<g filter="url(#shadow)"><rect x="20" y="1440" width="310" height="92" rx="24" fill="#FFF8E6" stroke="#2F6FB0" stroke-width="4"/></g>')
    add('<text x="175" y="1480" text-anchor="middle" font-family="&apos;Baloo 2&apos;, &apos;Segoe UI&apos;, sans-serif" font-weight="800" font-size="31" fill="#2B2118">¡Se une al viaje!</text>')
    add('<text x="175" y="1515" text-anchor="middle" font-family="Nunito, &apos;Segoe UI&apos;, sans-serif" font-weight="800" font-size="25" fill="#2F6FB0">Tucuxi · delfín gris</text>')
    # boca del Purus (afluente recién pasado: se llena entero)
    add('<text x="70" y="1105" font-family="Nunito, &apos;Segoe UI&apos;, sans-serif" font-weight="800" font-style="italic" font-size="26" fill="#FFF8E6" '
        'stroke="#1C5236" stroke-width="5" paint-order="stroke">río Purus</text>')
    add('<text x="1000" y="1800" text-anchor="middle" font-family="Nunito, &apos;Segoe UI&apos;, sans-serif" font-weight="800" font-style="italic" font-size="26" fill="#FFF8E6" '
        'stroke="#1C5236" stroke-width="5" paint-order="stroke" transform="rotate(12 1000 1800)">río Negro</text>')
    add('<text x="540" y="318" text-anchor="middle" font-family="&apos;Baloo 2&apos;, &apos;Segoe UI&apos;, sans-serif" font-weight="800" font-size="34" fill="#4E7F78" opacity="0.9">Andes ↑</text>')
    ui()


def build_svg() -> str:
    out.clear()
    add(f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">')
    add("<title>Tintalú · Mapa del río v2 (maqueta): Manaos → Tefé, nivel 76</title>")
    defs()
    add(f'<rect width="{W}" height="{H}" fill="#0E2B23"/>')
    scene()
    add("</svg>")
    return "\n".join(out)


def font_face(name, file, weight):
    data = base64.b64encode((FONTS / file).read_bytes()).decode("ascii")
    return (f"@font-face{{font-family:'{name}';font-weight:{weight};"
            f"src:url(data:font/ttf;base64,{data}) format('truetype');}}")


def fonts_css():
    return "\n".join((font_face("Baloo 2", "Baloo2-ExtraBold.ttf", 800), font_face("Nunito", "Nunito-ExtraBold.ttf", 800)))


CALLOUTS = [
    # (n, x, y, texto)
    (1, 640, 1950, "Encuentro de las Aguas: el Negro corre junto al Solimões sin mezclarse"),
    (2, 500, 1420, "Orillas: playas en la curva interior, barrancos en la exterior, várzea clara"),
    (3, 900, 1130, "Selva en capas: dosel oscuro lejos, palmeras de açaí junto al agua"),
    (4, 560, 1500, "Pueblos de paso con su viñeta (palafitos, iglesia, barco de línea)"),
    (5, 640, 1215, "Capi más grande, con su gesto y su emoción (sin texto)"),
    (6, 330, 1440, "Un compañero se une al viaje: el tucuxi"),
    (7, 770, 760, "Agua real hasta la canoa; río arriba, vidrio que falta llenar"),
    (8, 540, 420, "Niebla y Andes al fondo: lo que queda por descubrir"),
    (9, 60, 1990, "Primer plano con parallax; el Mapa ocupa toda la pantalla"),
]


def build_html(svg: str) -> str:
    return f"""<!doctype html>
<html lang="es"><head><meta charset="utf-8"><title>Mapa del río v2 · maqueta</title>
<meta name="viewport" content="width=1080">
<style>{fonts_css()}
html,body{{margin:0;padding:0;background:#0E2B23}} svg{{display:block}}</style></head>
<body>{svg}</body></html>"""


def build_lamina(svg: str) -> str:
    calls = []
    for n, x, y, _ in CALLOUTS:
        calls.append(f'<g><circle cx="{x}" cy="{y}" r="34" fill="#E2483A" stroke="#FFF8E6" stroke-width="6"/>'
                     f'<text x="{x}" y="{y+14}" text-anchor="middle" font-family="Baloo 2" font-weight="800" font-size="40" fill="#FFF8E6">{n}</text></g>')
    svg_called = svg.replace("</svg>", "\n".join(calls) + "</svg>")
    legend = "".join(f"<li><b>{n}</b> {t}</li>" for n, _, _, t in CALLOUTS)
    return f"""<!doctype html>
<html lang="es"><head><meta charset="utf-8"><title>Mapa del río · antes y después</title>
<style>{fonts_css()}
html,body{{margin:0;background:#0E2B23;color:#FFF8E6;font-family:Nunito, 'Segoe UI', sans-serif}}
.wrap{{display:flex;gap:40px;padding:40px 40px 20px 40px;align-items:flex-start}}
.col{{width:1080px}} .col h2{{font-family:'Baloo 2';font-size:56px;margin:0 0 16px 0}}
.col img,.col svg{{width:1080px;height:2340px;display:block;border-radius:36px;box-shadow:0 10px 30px #0008}}
.tag{{font-size:30px;opacity:.85;margin:0 0 20px 0}}
ol{{list-style:none;padding:0 40px 40px 40px;margin:0;columns:2;font-size:30px;column-gap:60px}}
ol li{{margin:0 0 14px 0;break-inside:avoid}} ol b{{display:inline-block;width:46px;height:46px;border-radius:23px;background:#E2483A;
text-align:center;line-height:46px;margin-right:12px;font-family:'Baloo 2'}}
h1{{font-family:'Baloo 2';font-size:64px;margin:0;padding:40px 40px 0 40px}}
</style></head><body>
<h1>Mapa del río: de la 1.0 a la 1.1 (maqueta)</h1>
<div class="wrap">
 <div class="col"><h2>Antes · 1.0</h2><p class="tag">Captura del juego (Mapa abierto en el 306, fase 2 de D-058)</p>
  <img src="../../../art/personajes-3d/fase2/despues/f2_05_mapa_bufeo.png" alt="Mapa del río de la 1.0"></div>
 <div class="col"><h2>Después · propuesta 1.1</h2><p class="tag">Tramo Manaos → Tefé, jugando el 76 (maqueta por código)</p>
  {svg_called}</div>
</div>
<ol>{legend}</ol>
</body></html>"""


def main():
    svg = build_svg()
    (HERE / "maqueta-mapa-v2.svg").write_text(svg, encoding="utf-8", newline="\n")
    (HERE / "maqueta-mapa-v2.html").write_text(build_html(svg), encoding="utf-8", newline="\n")
    (HERE / "lamina-antes-despues.html").write_text(build_lamina(svg), encoding="utf-8", newline="\n")
    print("OK maqueta-mapa-v2.svg, maqueta-mapa-v2.html, lamina-antes-despues.html")


if __name__ == "__main__":
    main()
