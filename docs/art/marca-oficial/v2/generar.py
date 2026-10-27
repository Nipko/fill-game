"""Tintalú · propuesta B «El río que se tiñe» (Claude, 2026-10-23). Solo biblioteca estándar.

Genera en ./out los SVG del icono maestro, las capas adaptativas de Android (fondo, primer plano y monocroma)
y el logo con el nombre (horizontal y vertical, sobre claro y sobre oscuro). Las exportaciones PNG las hace
exportar.ps1 con Chrome sin interfaz.

Fuentes que manda el juego:
- silueta de la cuia: Assets/_Project/Scripts/Presentation/CuiaGeometry.cs (aquí, capacidad 3);
- vertido: D-006 (inclinación 92° − 9° por dosis que queda; el líquido queda horizontal);
- colores: agua blanca del Amazonas #B08A5A (glosario §9, Solimões); Taperebá #F2B705 con puntos,
  Aguaje #EF7D1A con escamas, Açaí #6A2C91 con rombos (glosario §9);
- tipografía: Assets/_Project/Art/Fonts/Baloo2-ExtraBold.ttf (OFL).
"""
import math
import os
import struct
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "out")
REPO = os.path.abspath(os.path.join(HERE, "..", "..", "..", ".."))
FONT = os.path.join(REPO, "Assets", "_Project", "Art", "Fonts", "Baloo2-ExtraBold.ttf")

AGUA = "#B08A5A"
ACAI = "#6A2C91"
PETROLEO_TXT = "#13303A"
MARFIL_TXT = "#F4EEDF"

# --------------------------------------------------------------------------------------------- cuia
CAP = 3
U = 84.0
FOOT, CB, CH, SH, NK, RIM = 0.30, 0.60, 0.80, 0.40, 0.35, 0.25
RIMH, NECKO, NECKI, SHO, BODY, BASE, FOOTH, TOPF = 0.55, 0.50, 0.40, 0.99, 1.00, 0.95, 0.80, 0.40
H = CB + CAP + SH + NK + RIM
TOP = CB + CAP


def _ss(t):
    t = min(1, max(0, t))
    return t * t * (3 - 2 * t)


def _eo(t):
    t = min(1, max(0, t))
    return 1 - (1 - t) * (1 - t)


def outer(y):
    if y < FOOT:
        return FOOTH
    if y < CB:
        return FOOTH + (BASE - FOOTH) * _eo((y - FOOT) / (CB - FOOT))
    if y < TOP:
        peak = TOP - 0.40
        if y <= peak:
            return BASE + (BODY - BASE) * _ss((y - CB) / (peak - CB))
        return BODY - (BODY - SHO) * ((y - peak) / (TOP - peak))
    if y < TOP + SH:
        return SHO + (NECKO - SHO) * _ss((y - TOP) / SH)
    if y < TOP + SH + NK:
        return NECKO
    return RIMH


def inner(y):
    if y < TOP:
        return CH
    if y < TOP + SH:
        return CH + (NECKI - CH) * _ss((y - TOP) / SH)
    return NECKI


def sag(xr):
    e = min(1, abs(xr) / CH)
    return 0.5 * TOPF * math.sqrt(max(0, 1 - e * e))


def X(y):  # eje de la cuia → x local; la boca queda en x = 212
    return y * U - (H * U - 212)


def rot(p, r, t):
    c, s = math.cos(r), math.sin(r)
    return (t[0] + p[0] * c - p[1] * s, t[1] + p[0] * s + p[1] * c)


def clip(poly, c, below):
    out = []
    n = len(poly)
    for i in range(n):
        a, b = poly[i], poly[(i + 1) % n]
        ia, ib = (a[1] >= c) == below, (b[1] >= c) == below
        if ia:
            out.append(a)
        if ia != ib:
            t = (c - a[1]) / (b[1] - a[1])
            out.append((a[0] + t * (b[0] - a[0]), c))
    return out


def area(p):
    return abs(sum(p[i][0] * p[(i + 1) % len(p)][1] - p[(i + 1) % len(p)][0] * p[i][1] for i in range(len(p)))) / 2


def level_for(poly, a):
    lo, hi = min(q[1] for q in poly), max(q[1] for q in poly)
    for _ in range(60):
        m = (lo + hi) / 2
        if area(clip(poly, m, True)) > a:
            lo = m
        else:
            hi = m
    return (lo + hi) / 2


def P(pts):
    return "M " + " L ".join(f"{a:.1f} {b:.1f}" for a, b in pts) + " Z"


def L(pts):
    return "M " + " L ".join(f"{a:.1f} {b:.1f}" for a, b in pts)


YI = [CB + i * (H - CB) / 100 for i in range(101)]
CAV = ([(X(y), -inner(y) * U) for y in YI] + [(X(y), inner(y) * U) for y in reversed(YI)]
       + [(X(CB - sag(xr)), xr * U) for xr in [CH - 2 * CH * i / 40 for i in range(41)]])
YS = [FOOT + i * (TOP + SH + NK - FOOT) / 120 for i in range(121)]
GLASS = [(X(y), -outer(y) * U) for y in YS] + [(X(y), outer(y) * U) for y in reversed(YS)]
DOSE = 2 * CH * U * U


def vertido(lip, impact, vol):
    """Cuia inclinada con el líquido por gravedad y el chorro en parábola. Devuelve geometría en mundo."""
    lipl = (X(H) - 6, NECKI * U)
    best = None
    for k in range(4001):
        r = math.radians(-20 + k * 0.0125)
        lw = rot(lipl, r, (0, 0))
        poly = [rot(q, r, (0, 0)) for q in CAV]
        d = abs((lw[1] - level_for(poly, vol * DOSE)) - 14)
        if best is None or d < best[0]:
            best = (d, r)
    r = best[1]
    lw0 = rot(lipl, r, (0, 0))
    t = (lip[0] - lw0[0], lip[1] - lw0[1])
    poly = [rot(q, r, t) for q in CAV]
    l1, l2, l3 = level_for(poly, DOSE), level_for(poly, 2 * DOSE), level_for(poly, vol * DOSE)
    g = {
        "r": r,
        "glass": [rot(q, r, t) for q in GLASS],
        "cav": poly,
        "yel": clip(poly, l1, True),
        "ora": clip(clip(poly, l2, True), l1, False),
        "aca": clip(clip(poly, l3, True), l2, False),
        "l3": l3,
    }
    surf = [q for q in g["aca"] if abs(q[1] - l3) < 0.5]
    g["surf"] = (min(q[0] for q in surf), max(q[0] for q in surf))
    rect = lambda x0, y0, w, h: [(x0, y0), (x0 + w, y0), (x0 + w, y0 + h), (x0, y0 + h)]
    g["foot"] = [rot(q, r, t) for q in rect(X(0), -FOOTH * U, FOOT * U, 2 * FOOTH * U)]
    g["rim"] = [rot(q, r, t) for q in rect(X(H - RIM), -RIMH * U, RIM * U, 2 * RIMH * U)]
    g["hl"] = (rot((X(CB + 0.3), -(BODY - 0.25) * U), r, t), rot((X(TOP - 0.3), -(BODY - 0.25) * U), r, t))
    # chorro
    ux, uy = math.cos(r), math.sin(r)
    sx, sy = lip[0], lip[1] - 7
    dx, dy = impact[0] - sx, impact[1] - sy
    v = dx / ux
    gr = 2 * (dy - v * uy)
    pts = []
    for i in range(41):
        tt = i / 40
        x, y = sx + v * ux * tt, sy + v * uy * tt + gr * tt * tt / 2
        vx, vy = v * ux, v * uy + gr * tt
        n = math.hypot(vx, vy)
        w = (32 + (17 - 32) * math.sqrt(tt)) / 2
        pts.append((x, y, -vy / n, vx / n, w))
    g["stream"] = [(x + nx * w, y + ny * w) for x, y, nx, ny, w in pts] + [
        (x - nx * w, y - ny * w) for x, y, nx, ny, w in reversed(pts)]
    g["stream_hl"] = [(x + nx * w * 0.35, y + ny * w * 0.35) for x, y, nx, ny, w in pts[3:26]]
    return g


# La cuia y el impacto del icono v2 recto (D-045, 2026-10-23). Los bocetos de meandros y el icono oficial con meandros
# (añadido de D-045 del 2026-10-27, variante B) los desplazan; LIP, IMPACT y V oficiales se fijan al final del bloque de meandros.
LIP0, IMPACT0 = (558, 452), (604, 646)
V0 = vertido(LIP0, IMPACT0, 2.6)

# ----------------------------------------------------------------------------------------- piezas SVG
DEFS = f"""<defs>
  <linearGradient id="cielo" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0" stop-color="#226A6A"/><stop offset="0.5" stop-color="#14444A"/><stop offset="1" stop-color="#0A2A2E"/>
  </linearGradient>
  <radialGradient id="halo" cx="0.5" cy="0.5" r="0.5">
    <stop offset="0" stop-color="#5FB3A6" stop-opacity="0.45"/><stop offset="1" stop-color="#5FB3A6" stop-opacity="0"/>
  </radialGradient>
  <linearGradient id="aguaBlanca" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0" stop-color="#8F6B3F"/><stop offset="0.25" stop-color="{AGUA}"/><stop offset="1" stop-color="#C29C6A"/>
  </linearGradient>
  <linearGradient id="aguaAcai" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0" stop-color="#4E1F6E"/><stop offset="0.25" stop-color="{ACAI}"/><stop offset="1" stop-color="#7E3AAE"/>
  </linearGradient>
  <linearGradient id="chorro" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0" stop-color="{ACAI}"/><stop offset="1" stop-color="#8E44C4"/>
  </linearGradient>
  <linearGradient id="vidrio" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0" stop-color="#FFFFFF" stop-opacity="0.18"/><stop offset="1" stop-color="#FFFFFF" stop-opacity="0.02"/>
  </linearGradient>
  <pattern id="puntos" width="34" height="34" patternUnits="userSpaceOnUse"><circle cx="17" cy="17" r="8" fill="#B88400" opacity="0.45"/></pattern>
  <pattern id="escamas" width="30" height="22" patternUnits="userSpaceOnUse"><path d="M0 22 A15 15 0 0 1 30 22" fill="none" stroke="#B4520A" stroke-width="4" opacity="0.5"/></pattern>
  <pattern id="rombos" width="34" height="34" patternUnits="userSpaceOnUse"><path d="M17 7 L27 17 L17 27 L7 17 Z" fill="#4A1C68" opacity="0.55"/></pattern>
  <pattern id="trenza" width="16" height="16" patternUnits="userSpaceOnUse" patternTransform="rotate(45)"><rect width="16" height="16" fill="#C98A45"/><rect width="8" height="16" fill="#8E5A2B"/></pattern>
</defs>"""


def palmera(x, y, h, lean, color):
    """Palmera de açaí en silueta: tronco fino y penacho de hojas colgantes."""
    tx, ty = x + lean, y - h
    s = [f'<path d="M {x} {y} Q {x + lean * 0.3:.0f} {y - h * 0.5:.0f} {tx} {ty}" stroke="{color}" stroke-width="9" fill="none" stroke-linecap="round"/>']
    for ang, ln in [(-160, 95), (-130, 105), (-95, 80), (-60, 105), (-25, 95), (-200, 70), (15, 70)]:
        a = math.radians(ang)
        ex, ey = tx + math.cos(a) * ln, ty + math.sin(a) * ln * 0.55 + 40
        cx, cy = tx + math.cos(a) * ln * 0.55, ty + math.sin(a) * ln * 0.9 - 10
        s.append(f'<path d="M {tx} {ty} Q {cx:.0f} {cy:.0f} {ex:.0f} {ey:.0f}" stroke="{color}" stroke-width="12" fill="none" stroke-linecap="round"/>')
    return "\n  ".join(s)


def fondo_recto():
    """El fondo del icono v2 recto (D-045, hasta el 2026-10-27): el río como una franja. Se guarda como boceto «recto»."""
    copas = ("M -600 650 L -600 560 C -500 520 -440 540 -400 556 " +
             " ".join(f"C {x - 70} {500 + (i % 3) * 8} {x - 10} {492 + (i % 2) * 14} {x + 30} {530 + (i % 2) * 6}"
                      for i, x in enumerate(range(-320, 1700, 130))) + " L 1700 650 Z")
    return f"""<rect x="-600" y="-600" width="2224" height="2224" fill="url(#cielo)"/>
  <ellipse cx="430" cy="380" rx="420" ry="300" fill="url(#halo)"/>
  <!-- Selva de la orilla de enfrente, con dos palmeras de açaí -->
  {palmera(150, 600, 210, -18, "#0E4232")}
  {palmera(905, 600, 250, 22, "#0E4232")}
  <path d="{copas}" fill="#0E4232"/>
  <path d="M -600 670 L -600 604 C -300 576 0 600 200 596 C 420 580 600 604 800 596 C 1000 580 1300 600 1700 596 L 1700 670 Z" fill="#26603A"/>
  <!-- Río arriba: agua blanca del Amazonas -->
  <path d="M -600 624 C 250 590 560 650 1700 600 L 1700 905 C 700 935 300 860 -600 900 Z" fill="url(#aguaBlanca)"/>
  <path d="M -600 728 C 260 696 520 742 1100 708" fill="none" stroke="#D8BA8A" stroke-width="30" opacity="0.4" stroke-linecap="round"/>
  <!-- Río abajo, desde el impacto: el açaí se abre con la corriente hasta llenar el cauce -->
  <path d="M 540 646 C 640 640 800 616 1700 600 L 1700 905 C 900 912 780 880 712 800 C 660 738 600 690 540 668 Z" fill="#8A4FA8" opacity="0.5"/>
  <path d="M 566 650 C 660 644 810 620 1700 602 L 1700 905 C 920 908 810 870 748 796 C 694 734 628 690 566 664 Z" fill="url(#aguaAcai)"/>
  <path d="M 720 706 C 840 694 960 692 1600 680" fill="none" stroke="#A866DA" stroke-width="26" opacity="0.45" stroke-linecap="round"/>
  <g fill="none" stroke-linecap="round" stroke-width="9">
    <path d="M 70 780 q 24 -14 48 0 q 24 14 48 0" stroke="#E6CB9E" opacity="0.8"/>
    <path d="M 270 822 q 20 -12 40 0 q 20 12 40 0" stroke="#E6CB9E" opacity="0.7"/>
    <path d="M 150 862 q 18 -10 36 0" stroke="#E6CB9E" opacity="0.6"/>
    <path d="M 430 764 q 20 -12 40 0 q 20 12 40 0" stroke="#E6CB9E" opacity="0.6"/>
    <path d="M 820 770 q 24 -14 48 0 q 24 14 48 0" stroke="#CDA0F0" opacity="0.8"/>
    <path d="M 900 852 q 20 -12 40 0 q 20 12 40 0" stroke="#CDA0F0" opacity="0.7"/>
    <path d="M 700 662 q 18 -10 36 0" stroke="#CDA0F0" opacity="0.6"/>
  </g>
  <!-- Orilla cercana -->
  <path d="M -600 1700 L -600 885 C 300 850 700 925 1700 885 L 1700 1700 Z" fill="#0E4232"/>
  <path d="M -600 908 C 300 872 700 946 1700 906" fill="none" stroke="#26603A" stroke-width="22"/>"""


def _frente(v, imp, aros=(76, 22, 122, 36)):
    """La cuia, el chorro y los aros del impacto, para la cuia v (vertido) que vierte sobre imp."""
    ix, iy = imp
    a1x, a1y, a2x, a2y = aros
    sx0, sx1 = v["surf"]
    return f"""<!-- Chorro: sale del canto de la boca y cae en parábola, cada vez más fino -->
  <path d="{P(v['stream'])}" fill="url(#chorro)"/>
  <path d="{L(v['stream_hl'])}" fill="none" stroke="#E7C8FA" stroke-width="5" stroke-linecap="round" opacity="0.6"/>
  <!-- Impacto -->
  <g fill="none" stroke-linecap="round">
    <ellipse cx="{ix}" cy="{iy + 2}" rx="{a1x}" ry="{a1y}" stroke="#EBD5F8" stroke-width="8" opacity="0.9"/>
    <ellipse cx="{ix}" cy="{iy + 6}" rx="{a2x}" ry="{a2y}" stroke="#EBD5F8" stroke-width="6" opacity="0.35"/>
  </g>
  <circle cx="{ix - 64}" cy="{iy - 34}" r="10" fill="#B57BE0"/><circle cx="{ix + 72}" cy="{iy - 42}" r="8" fill="#B57BE0"/><circle cx="{ix - 84}" cy="{iy - 12}" r="6" fill="#B57BE0"/>
  <!-- Cuia de vidrio (CuiaGeometry.cs, capacidad 3), inclinada {math.degrees(v['r']):.0f}° sobre la horizontal (D-006).
       Quedan 2,6 de 3 dosis: el açaí, arriba, es el que se va. -->
  <path d="{P(v['glass'])}" fill="#06201F" opacity="0.35" transform="translate(10 18)"/>
  <path d="{P(v['yel'])}" fill="#F2B705"/><path d="{P(v['yel'])}" fill="url(#puntos)"/>
  <path d="{P(v['ora'])}" fill="#EF7D1A"/><path d="{P(v['ora'])}" fill="url(#escamas)"/>
  <path d="{P(v['aca'])}" fill="{ACAI}"/><path d="{P(v['aca'])}" fill="url(#rombos)"/>
  <path d="M {sx0:.1f} {v['l3'] + 4:.1f} L {sx1:.1f} {v['l3'] + 4:.1f}" stroke="#A060D0" stroke-width="9" opacity="0.9"/>
  <path d="{P(v['glass'])}" fill="url(#vidrio)" stroke="#EAF6F4" stroke-width="9" stroke-linejoin="round"/>
  <path d="{P(v['cav'])}" fill="none" stroke="#FFFFFF" stroke-width="3" opacity="0.3"/>
  <path d="M {v['hl'][0][0]:.1f} {v['hl'][0][1]:.1f} L {v['hl'][1][0]:.1f} {v['hl'][1][1]:.1f}" stroke="#FFFFFF" stroke-width="11" stroke-linecap="round" opacity="0.5"/>
  <path d="{P(v['foot'])}" fill="url(#trenza)" stroke="#5E3A18" stroke-width="4" stroke-linejoin="round"/>
  <path d="{P(v['rim'])}" fill="url(#trenza)" stroke="#5E3A18" stroke-width="4" stroke-linejoin="round"/>"""


RIO_MONO = """<path d="M -600 624 C 250 590 560 650 1700 600" fill="none" stroke="#FFF" stroke-width="16"/>
    <path d="M -600 900 C 300 860 700 935 1700 905" fill="none" stroke="#FFF" stroke-width="16"/>
    <path d="M 566 650 C 660 644 810 620 1700 602 L 1700 905 C 920 908 810 870 748 796 C 694 734 628 690 566 664 Z" fill="#FFF"/>"""


def _monocroma(v, imp, rio, aro=(96, 30, 80, 24)):
    """Icono temático de Android 13+: una sola tinta (el sistema la colorea). Río abajo lleno = teñido."""
    ix, iy = imp
    # Máscara: blanco = tinta; negro = hueco transparente (separa las dosis y despega la cuia del río).
    return f"""<mask id="tinta" maskUnits="userSpaceOnUse" x="-600" y="-600" width="2224" height="2224">
    <!-- río arriba: solo sus orillas; río abajo, desde el impacto: lleno (teñido) -->
    {rio}
    <ellipse cx="{ix}" cy="{iy + 2}" rx="{aro[0]}" ry="{aro[1]}" fill="#000"/>
    <ellipse cx="{ix}" cy="{iy + 2}" rx="{aro[2]}" ry="{aro[3]}" fill="none" stroke="#FFF" stroke-width="12"/>
    <path d="{P(v['stream'])}" fill="#FFF" stroke="#000" stroke-width="10"/>
    <path d="{P(v['glass'])}" fill="#000" stroke="#000" stroke-width="40" stroke-linejoin="round"/>
    <path d="{P(v['glass'])}" fill="none" stroke="#FFF" stroke-width="16" stroke-linejoin="round"/>
    <path d="{P(v['yel'])}" fill="#FFF" stroke="#000" stroke-width="10"/>
    <path d="{P(v['ora'])}" fill="#FFF" stroke="#000" stroke-width="10"/>
    <path d="{P(v['aca'])}" fill="#FFF" stroke="#000" stroke-width="10"/>
    <path d="{P(v['foot'])}" fill="#FFF"/><path d="{P(v['rim'])}" fill="#FFF"/>
  </mask>
  <rect x="-600" y="-600" width="2224" height="2224" fill="#FFFFFF" mask="url(#tinta)"/>"""


def svg(body, size=1024, view="0 0 1024 1024", extra_defs=""):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="{view}" width="{size}" height="{size}">\n'
            f'{DEFS}{extra_defs}\n  {body}\n</svg>\n')


# Capa adaptativa: lienzo de 108 dp; se ve el círculo central de 72 dp y la zona segura es de 66 dp.
# Escala 0,86 alrededor del centro: lo esencial (cuia, chorro, impacto) cae dentro de los 66 dp.
ADAPT = 'transform="translate(512 512) scale(0.86) translate(-512 -512)"'


# ---------------------------------------------------------------------------------- lector mínimo de TTF
class TTF:
    def __init__(self, path):
        self.b = open(path, "rb").read()
        n = struct.unpack(">H", self.b[4:6])[0]
        self.t = {}
        for i in range(n):
            tag, _, off, ln = struct.unpack(">4sIII", self.b[12 + 16 * i: 28 + 16 * i])
            self.t[tag.decode()] = (off, ln)
        h = self.t["head"][0]
        self.upem = struct.unpack(">H", self.b[h + 18:h + 20])[0]
        self.locfmt = struct.unpack(">h", self.b[h + 50:h + 52])[0]
        self.nglyphs = struct.unpack(">H", self.b[self.t["maxp"][0] + 4:self.t["maxp"][0] + 6])[0]
        hh = self.t["hhea"][0]
        self.nhm = struct.unpack(">H", self.b[hh + 34:hh + 36])[0]
        self.asc, self.desc = struct.unpack(">hh", self.b[hh + 4:hh + 8])
        self._cmap()

    def _cmap(self):
        c = self.t["cmap"][0]
        n = struct.unpack(">H", self.b[c + 2:c + 4])[0]
        self.map = {}
        for i in range(n):
            pid, eid, off = struct.unpack(">HHI", self.b[c + 4 + 8 * i:c + 12 + 8 * i])
            st = c + off
            fmt = struct.unpack(">H", self.b[st:st + 2])[0]
            if fmt == 4 and pid in (0, 3):
                segx2 = struct.unpack(">H", self.b[st + 6:st + 8])[0]
                seg = segx2 // 2
                ends = struct.unpack(f">{seg}H", self.b[st + 14:st + 14 + segx2])
                starts = struct.unpack(f">{seg}H", self.b[st + 16 + segx2:st + 16 + 2 * segx2])
                deltas = struct.unpack(f">{seg}h", self.b[st + 16 + 2 * segx2:st + 16 + 3 * segx2])
                ro_off = st + 16 + 3 * segx2
                ros = struct.unpack(f">{seg}H", self.b[ro_off:ro_off + segx2])
                for k in range(seg):
                    for ch in range(starts[k], ends[k] + 1):
                        if ch == 0xFFFF:
                            continue
                        if ros[k] == 0:
                            gid = (ch + deltas[k]) & 0xFFFF
                        else:
                            a = ro_off + 2 * k + ros[k] + 2 * (ch - starts[k])
                            gid = struct.unpack(">H", self.b[a:a + 2])[0]
                            if gid:
                                gid = (gid + deltas[k]) & 0xFFFF
                        self.map.setdefault(ch, gid)
                return

    def adv(self, g):
        hm = self.t["hmtx"][0]
        i = min(g, self.nhm - 1)
        return struct.unpack(">H", self.b[hm + 4 * i:hm + 4 * i + 2])[0]

    def _loc(self, g):
        lo = self.t["loca"][0]
        if self.locfmt == 0:
            a, b = struct.unpack(">HH", self.b[lo + 2 * g:lo + 2 * g + 4])
            return a * 2, b * 2
        return struct.unpack(">II", self.b[lo + 4 * g:lo + 4 * g + 8])

    def contours(self, g, dx=0.0, dy=0.0):
        a, b = self._loc(g)
        if a == b:
            return []
        p = self.t["glyf"][0] + a
        nc = struct.unpack(">h", self.b[p:p + 2])[0]
        if nc < 0:  # compuesto
            out = []
            q = p + 10
            while True:
                fl, gi = struct.unpack(">HH", self.b[q:q + 4])
                q += 4
                if fl & 1:
                    ax, ay = struct.unpack(">hh", self.b[q:q + 4]); q += 4
                else:
                    ax, ay = struct.unpack(">bb", self.b[q:q + 2]); q += 2
                if fl & 8:
                    q += 2
                elif fl & 0x40:
                    q += 4
                elif fl & 0x80:
                    q += 8
                out += self.contours(gi, dx + ax, dy + ay)
                if not fl & 0x20:
                    break
            return out
        ends = struct.unpack(f">{nc}H", self.b[p + 10:p + 10 + 2 * nc])
        npts = ends[-1] + 1
        q = p + 10 + 2 * nc
        il = struct.unpack(">H", self.b[q:q + 2])[0]
        q += 2 + il
        flags = []
        while len(flags) < npts:
            f = self.b[q]; q += 1
            flags.append(f)
            if f & 8:
                r = self.b[q]; q += 1
                flags += [f] * r
        xs, ys = [], []
        v = 0
        for f in flags:
            if f & 2:
                d = self.b[q]; q += 1
                v += d if f & 16 else -d
            elif not f & 16:
                v += struct.unpack(">h", self.b[q:q + 2])[0]; q += 2
            xs.append(v)
        v = 0
        for f in flags:
            if f & 4:
                d = self.b[q]; q += 1
                v += d if f & 32 else -d
            elif not f & 32:
                v += struct.unpack(">h", self.b[q:q + 2])[0]; q += 2
            ys.append(v)
        out, s = [], 0
        for e in ends:
            out.append([(xs[i] + dx, ys[i] + dy, flags[i] & 1) for i in range(s, e + 1)])
            s = e + 1
        return out


def texto_a_path(font, txt, x, base, size, track=0.0):
    sc = size / font.upem
    d = []
    pen = 0.0
    for ch in txt:
        g = font.map.get(ord(ch), 0)
        for c in font.contours(g):
            pts = [(x + (px + pen) * sc, base - py * sc, on) for px, py, on in c]
            n = len(pts)
            # empieza en un punto sobre la curva (o en el medio de dos de control)
            k0 = next((i for i in range(n) if pts[i][2]), None)
            if k0 is None:
                a, b = pts[0], pts[1]
                pts.insert(0, ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2, 1)); k0 = 0; n += 1
            pts = pts[k0:] + pts[:k0]
            seg = [f"M {pts[0][0]:.1f} {pts[0][1]:.1f}"]
            i = 1
            while i <= n:
                p = pts[i % n]
                if p[2]:
                    seg.append(f"L {p[0]:.1f} {p[1]:.1f}")
                    i += 1
                else:
                    nx = pts[(i + 1) % n]
                    if nx[2]:
                        seg.append(f"Q {p[0]:.1f} {p[1]:.1f} {nx[0]:.1f} {nx[1]:.1f}")
                        i += 2
                    else:
                        mx, my = (p[0] + nx[0]) / 2, (p[1] + nx[1]) / 2
                        seg.append(f"Q {p[0]:.1f} {p[1]:.1f} {mx:.1f} {my:.1f}")
                        i += 1
            d.append(" ".join(seg) + " Z")
        pen += font.adv(g) + track * font.upem
    return " ".join(d), pen * sc


# ------------------------------------------------------------------------- meandros (bocetos, fase 1)
# Petición del dueño (2026-10-27): «no se logra entender que es un río … agregando los meandros como en la pantalla de carga».
# El río del emblema serpentea como en BrandRiverScene (añadido de D-051): un eje con su anchura en cada punto de control
# (fino a lo lejos, ancho en el primer plano), playas en el interior de las curvas, orillas de selva en copas y el açaí que
# se abre desde el impacto río abajo. Las variantes solo se usan con `python generar.py --variante <nombre> --out <carpeta>`;
# sin argumentos, generar.py escribe el icono oficial tal cual.

SELVA_SOMBRA, SELVA_COPA, SELVA_COPA2 = "#082A1E", "#17603A", "#226E36"


def _cr(a, b, c, d, t):
    t2, t3 = t * t, t * t * t
    return 0.5 * (2 * b + (c - a) * t + (2 * a - 5 * b + 4 * c - d) * t2 + (3 * b - a - 3 * c + d) * t3)


def _suave(v, r):
    return [sum(v[max(0, i - r):i + r + 1]) / len(v[max(0, i - r):i + r + 1]) for i in range(len(v))]


class Rio:
    """Eje del río por Catmull-Rom sobre puntos (x, y, media anchura); la corriente va del primero al último."""

    def __init__(self, ctrl, impacto, m=48):
        n = len(ctrl)
        ext = [ctrl[0]] + list(ctrl) + [ctrl[-1]]
        self.x, self.y, self.h = [], [], []
        for k in range(n - 1):
            a, b, c, d = ext[k], ext[k + 1], ext[k + 2], ext[k + 3]
            for j in range(m if k < n - 2 else m + 1):
                t = j / m
                self.x.append(_cr(a[0], b[0], c[0], d[0], t))
                self.y.append(_cr(a[1], b[1], c[1], d[1], t))
                self.h.append(max(1.0, _cr(a[2], b[2], c[2], d[2], t)))
        N = self.N = len(self.x)
        self.s = [0.0] * N
        for i in range(1, N):
            self.s[i] = self.s[i - 1] + math.hypot(self.x[i] - self.x[i - 1], self.y[i] - self.y[i - 1])
        self.nx, self.ny = [0.0] * N, [0.0] * N
        for i in range(N):
            a, b = max(0, i - 1), min(N - 1, i + 1)
            tx, ty = self.x[b] - self.x[a], self.y[b] - self.y[a]
            ln = max(1e-6, math.hypot(tx, ty))
            self.nx[i], self.ny[i] = ty / ln, -tx / ln  # a la izquierda de la corriente (con y hacia abajo)
        # curvatura con signo: positiva si la curva se cierra hacia la normal (la izquierda)
        raw = [0.0] * N
        for i in range(1, N - 1):
            ds = max(1e-6, self.s[i + 1] - self.s[i - 1])
            dtx = (-self.ny[i + 1]) - (-self.ny[i - 1])
            dty = self.nx[i + 1] - self.nx[i - 1]
            raw[i] = (dtx * self.nx[i] + dty * self.ny[i]) / ds
        curv = _suave(raw, 8)
        # el río nunca más ancho que el 70 % del radio de su curva: la orilla interior no se pliega (BrandRiverScene)
        cap = [min(self.h[i], 0.7 / max(1e-6, abs(curv[i]))) for i in range(N)]
        low = [min(cap[max(0, i - 8):i + 9]) for i in range(N)]
        sm = _suave(low, 8)
        self.h = [min(self.h[i], sm[i]) for i in range(N)]
        # playas: en el interior de las curvas cerradas, tanto más anchas cuanto más cierra la curva respecto del ancho
        cs = _suave(raw, 20)
        self.playaL, self.playaR = [0.0] * N, [0.0] * N
        for i in range(N):
            f = min(1.0, abs(cs[i]) * self.h[i] / 0.45) ** 1.5
            w = 0.42 * self.h[i] * f
            if cs[i] > 0:
                self.playaL[i] = w
            else:
                self.playaR[i] = w
        self.playaL, self.playaR = _suave(self.playaL, 6), _suave(self.playaR, 6)
        self.i_imp = min(range(N), key=lambda i: (self.x[i] - impacto[0]) ** 2 + (self.y[i] - impacto[1]) ** 2)

    def banda(self, izq, der, i0=0, i1=None):
        """Polígono entre la orilla izquierda (eje + n·izq(i)) y la derecha (eje − n·der(i))."""
        i1 = self.N - 1 if i1 is None else i1
        rng = self._paso(i0, i1)
        a = [(self.x[i] + self.nx[i] * izq(i), self.y[i] + self.ny[i] * izq(i)) for i in rng]
        b = [(self.x[i] - self.nx[i] * der(i), self.y[i] - self.ny[i] * der(i)) for i in reversed(rng)]
        return P(a + b)

    def linea(self, off, i0=0, i1=None):
        i1 = self.N - 1 if i1 is None else i1
        return L([(self.x[i] + self.nx[i] * off(i), self.y[i] + self.ny[i] * off(i)) for i in self._paso(i0, i1)])

    @staticmethod
    def _paso(i0, i1, k=3):
        """Las muestras que se escriben: una de cada k (y los extremos), para que el SVG no pese de más (el logo de la web)."""
        return list(range(i0, i1, k)) + [i1]

    def acai(self, r0, abre, extra=0.0):
        """Media anchura del açaí: una gota de radio r0 en el impacto que se abre río abajo hasta llenar el cauce."""
        s0 = self.s[self.i_imp]
        out = []
        for i in range(self.N):
            d = self.s[i] - s0
            if d < -r0:
                out.append(0.0)
            elif d < 0:
                out.append(r0 * math.sqrt(max(0.0, 1 - (d / r0) ** 2)) + extra)
            else:
                out.append(r0 + (self.h[i] - r0) * _ss(d / abre) + extra)
        out = [min(out[i], self.h[i]) for i in range(self.N)]
        i0 = next(i for i in range(self.N) if out[i] > 0)
        return out, max(0, i0 - 1)


def _copas(rio, lado, lejos, cerca, paso, fill, rmul=1.0, dy=0.0):
    """Copas de la selva en la orilla: círculos sobre el borde exterior de la franja verde, del tamaño de la perspectiva."""
    out = []
    s = 0.0
    i = 0
    sg = 1 if lado == "izq" else -1
    while i < rio.N:
        h = rio.h[i]
        r = (lejos + (cerca - lejos) * min(1.0, h / 140)) * rmul
        play = rio.playaL[i] if lado == "izq" else rio.playaR[i]
        off = h + play + 0.06 * h + 3 + 0.27 * h + 3
        out.append(f'<circle cx="{rio.x[i] + sg * rio.nx[i] * off:.1f}" cy="{rio.y[i] + sg * rio.ny[i] * off + dy:.1f}" r="{r:.1f}"/>')
        s += max(4.0, r * paso)
        while i < rio.N and rio.s[i] < s:
            i += 1
    return f'<g fill="{fill}">' + "".join(out) + "</g>"


def _ondas(rio, color, opac, pos, i0=0, i1=None):
    """Ondas cortas sobre el agua (como en el icono): (fracción del tramo, desplazamiento lateral en medias anchuras)."""
    i1 = rio.N - 1 if i1 is None else i1
    out = []
    for f, lat in pos:
        i = int(i0 + f * (i1 - i0))
        h = rio.h[i]
        a = max(4.0, 0.16 * h)
        x = rio.x[i] + rio.nx[i] * lat * h - 2 * a
        y = rio.y[i] + rio.ny[i] * lat * h
        out.append(f'<path d="M {x:.1f} {y:.1f} q {a:.1f} {-0.6 * a:.1f} {2 * a:.1f} 0 q {a:.1f} {0.6 * a:.1f} {2 * a:.1f} 0" '
                   f'stroke-width="{max(3.0, 0.045 * h + 3):.1f}"/>')
    return f'<g fill="none" stroke="{color}" stroke-linecap="round" opacity="{opac}">' + "".join(out) + "</g>"


def rio_svg(rio, r0=40, abre=260):
    """El cauce: selva de la orilla con sus copas, línea oscura, playas, agua blanca, açaí desde el impacto y ondas."""
    N = rio.N
    h = rio.h
    banco = lambda i: 0.27 * h[i] + 3
    borde = lambda i: 0.06 * h[i] + 3
    a, ia = rio.acai(r0, abre)
    halo, ih = rio.acai(r0 + 10, abre * 0.8, extra=10)
    return f"""<g>
    {_copas(rio, "izq", 7, 30, 1.25, SELVA_SOMBRA, 1.15, 6)}{_copas(rio, "der", 7, 30, 1.25, SELVA_SOMBRA, 1.15, 6)}
    <path d="{rio.banda(lambda i: h[i] + rio.playaL[i] + borde(i) + banco(i), lambda i: h[i] + rio.playaR[i] + borde(i) + banco(i))}" fill="#26603A"/>
    {_copas(rio, "izq", 6, 26, 1.3, SELVA_COPA)}{_copas(rio, "der", 6, 26, 1.3, SELVA_COPA2)}
    <path d="{rio.banda(lambda i: h[i] + rio.playaL[i] + borde(i), lambda i: h[i] + rio.playaR[i] + borde(i))}" fill="#0A3428"/>
    <path d="{rio.banda(lambda i: h[i] + rio.playaL[i], lambda i: h[i] + rio.playaR[i])}" fill="url(#arena)"/>
    <path d="{rio.banda(lambda i: h[i], lambda i: h[i])}" fill="url(#aguaBlanca)"/>
    <path d="{rio.banda(lambda i: h[i] * 0.30, lambda i: -h[i] * 0.02)}" fill="#D8BA8A" opacity="0.32"/>
    {_ondas(rio, "#E6CB9E", 0.75, [(0.45, -0.45), (0.7, 0.35), (0.9, -0.2)], 0, rio.i_imp)}
    <!-- Río abajo, desde el impacto: el açaí se abre con la corriente hasta llenar el cauce -->
    <path d="{rio.banda(lambda i: halo[i], lambda i: halo[i], ih)}" fill="#8A4FA8" opacity="0.5"/>
    <path d="{rio.banda(lambda i: a[i], lambda i: a[i], ia)}" fill="url(#aguaAcai)"/>
    <path d="{rio.banda(lambda i: a[i] * 0.30, lambda i: -a[i] * 0.02, ia)}" fill="#A866DA" opacity="0.4"/>
    {_ondas(rio, "#CDA0F0", 0.8, [(0.22, 0.4), (0.45, -0.35), (0.7, 0.3), (0.9, -0.3)], rio.i_imp, N - 1)}
  </g>"""


def rio_mono(rio, r0=40, abre=260, trazo=16, orillas_desde=0.0):
    """La monocroma del río: río arriba, solo sus orillas (desde donde la media anchura llega a orillas_desde: las lejanas,
    finas, se quedan fuera para que a 48 px no hagan ruido); desde el impacto, lleno (teñido)."""
    a, ia = rio.acai(r0, abre)
    i = rio.i_imp
    h = rio.h
    i0 = next((k for k in range(i) if h[k] >= orillas_desde), 0)
    return (f'<path d="{rio.linea(lambda k: h[k], i0, i)}" fill="none" stroke="#FFF" stroke-width="{trazo}" stroke-linecap="round"/>\n'
            f'    <path d="{rio.linea(lambda k: -h[k], i0, i)}" fill="none" stroke="#FFF" stroke-width="{trazo}" stroke-linecap="round"/>\n'
            f'    <path d="{rio.linea(lambda k: h[k], i)}" fill="none" stroke="#FFF" stroke-width="{trazo}"/>\n'
            f'    <path d="{rio.linea(lambda k: -h[k], i)}" fill="none" stroke="#FFF" stroke-width="{trazo}"/>\n'
            f'    <path d="{rio.banda(lambda k: a[k], lambda k: a[k], ia)}" fill="#FFF"/>')


DEFS_MEANDRO = """<defs>
  <linearGradient id="arena" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#B89560"/><stop offset="1" stop-color="#E6C88E"/></linearGradient>
  <linearGradient id="suelo" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#2A6658"/><stop offset="0.35" stop-color="#1A5236"/><stop offset="1" stop-color="#0B3326"/></linearGradient>
  <pattern id="dosel" width="150" height="130" patternUnits="userSpaceOnUse">
    <rect width="150" height="130" fill="#0F4630"/>
    <circle cx="38" cy="36" r="40" fill="#145236"/><circle cx="112" cy="40" r="34" fill="#165A38"/>
    <circle cx="75" cy="98" r="42" fill="#124C33"/><circle cx="0" cy="100" r="30" fill="#165A38"/><circle cx="150" cy="100" r="30" fill="#165A38"/>
    <circle cx="30" cy="26" r="12" fill="#22693A" opacity="0.6"/><circle cx="104" cy="30" r="10" fill="#22693A" opacity="0.6"/>
    <circle cx="66" cy="86" r="12" fill="#22693A" opacity="0.6"/>
  </pattern>
  <pattern id="doselSuelo" width="120" height="60" patternUnits="userSpaceOnUse">
    <ellipse cx="30" cy="20" rx="34" ry="16" fill="#0E4232"/><ellipse cx="90" cy="50" rx="34" ry="16" fill="#0E4232"/>
  </pattern>
</defs>"""


def horizonte(y, palmeras):
    """Cielo, halo, palmeras de açaí y el perfil de copas del horizonte a la altura y; debajo, el suelo de la selva."""
    copas = (f"M -600 {y + 60} L -600 {y - 30} " +
             " ".join(f"C {x - 70} {y - 80 + (i % 3) * 8} {x - 10} {y - 88 + (i % 2) * 14} {x + 30} {y - 50 + (i % 2) * 6}"
                      for i, x in enumerate(range(-320, 1700, 130))) + f" L 1700 {y + 60} Z")
    palm = "\n  ".join(palmera(px, y - 30, ph, lean, "#0E4232") for px, ph, lean in palmeras)
    return f"""<rect x="-600" y="-600" width="2224" height="2224" fill="url(#cielo)"/>
  <ellipse cx="430" cy="{y - 220}" rx="420" ry="300" fill="url(#halo)"/>
  {palm}
  <path d="{copas}" fill="#0E4232"/>
  <rect x="-600" y="{y}" width="2224" height="1800" fill="url(#suelo)"/>
  <rect x="-600" y="{y + 40}" width="2224" height="1800" fill="url(#doselSuelo)" opacity="0.22"/>"""


class Variante:
    """Un boceto de meandros: el eje del río, dónde cae el chorro (la cuia se mueve con él) y el horizonte, si lo hay."""

    def __init__(self, nombre, titulo, ctrl, mover=(0, 0), horiz=None, palmeras=(), r0=40, abre=260, aros=(76, 22, 122, 36),
                 orillas_desde=0.0):
        self.nombre, self.titulo = nombre, titulo
        self.lip = (LIP0[0] + mover[0], LIP0[1] + mover[1])
        self.imp = (IMPACT0[0] + mover[0], IMPACT0[1] + mover[1])
        self.orillas_desde = orillas_desde
        self.v = vertido(self.lip, self.imp, 2.6)
        self.rio = Rio(ctrl, self.imp)
        self.horiz, self.palmeras, self.r0, self.abre, self.aros = horiz, palmeras, r0, abre, aros

    def fondo(self):
        base = (horizonte(self.horiz, self.palmeras) if self.horiz is not None
                else '<rect x="-600" y="-600" width="2224" height="2224" fill="url(#dosel)"/>')
        return f"{DEFS_MEANDRO}\n  {base}\n  {rio_svg(self.rio, self.r0, self.abre)}"

    def frente(self):
        return _frente(self.v, self.imp, self.aros)

    def monocroma(self):
        a = self.aros
        return _monocroma(self.v, self.imp, rio_mono(self.rio, self.r0, self.abre, orillas_desde=self.orillas_desde),
                          (a[0] + 20, a[1] + 8, a[0] + 4, a[1] + 2))


class Recto:
    """El icono v2 recto (D-045, hasta el 2026-10-27), como boceto «recto» para comparar."""
    nombre, titulo = "recto", "El río recto (v2 hasta el 2026-10-27)"
    lip, imp, v = LIP0, IMPACT0, V0

    def fondo(self):
        return fondo_recto()

    def frente(self):
        return _frente(V0, IMPACT0)

    def monocroma(self):
        return _monocroma(V0, IMPACT0, RIO_MONO)


def variantes():
    """Los bocetos de meandros (fase 1) y el oficial («dos-meandros», añadido de D-045 del 2026-10-27). El punto de control con
    el impacto va en la lista: el chorro cae sobre el eje."""
    return {
        "recto": Recto(),
        # Un meandro amplio en S: nace fino en el horizonte, a la derecha; gira bajo la cuia y se abre en el primer plano.
        "s-amplia": Variante(
            "s-amplia", "A · un meandro amplio en S",
            [(800, 452, 4), (850, 478, 9), (800, 512, 18), (690, 556, 32), (604, 606, 46), (430, 660, 62), (320, 740, 76),
             (360, 840, 92), (580, 910, 108), (840, 970, 122), (1040, 1080, 136)],
            mover=(0, -40), horiz=450, palmeras=((120, 170, -14), (930, 200, 18))),
        # Dos meandros con perspectiva, como en la pantalla de carga: dos curvas lejanas y una ancha en el primer plano.
        # Oficial desde el añadido de D-045 del 2026-10-27 (fase 2): la cuia 60 px a la derecha y 40 arriba del recto, con margen
        # en la zona segura de 66 dp (ver zona_segura_dp); las curvas lejanas, algo más anchas para que se lean a 48 px; en la
        # monocroma, solo las orillas del tramo cercano (media anchura ≥ 30).
        "dos-meandros": Variante(
            "dos-meandros", "B · dos meandros con perspectiva",
            [(720, 342, 4), (806, 362, 7), (748, 392, 11), (656, 420, 16), (676, 454, 22), (806, 490, 30), (856, 536, 38),
             (786, 578, 46), (664, 606, 54), (530, 666, 66), (440, 772, 80), (490, 890, 96), (670, 986, 110),
             (800, 1110, 124)],
            mover=(60, -40), horiz=342, palmeras=((110, 140, -14), (930, 160, 18)), orillas_desde=30),
        # Vista desde el aire: sin cielo; el río cruza la selva de izquierda a derecha con meandros en U.
        "aereo": Variante(
            "aereo", "C · desde el aire",
            [(-120, 520, 70), (60, 700, 74), (230, 800, 76), (420, 720, 78), (584, 566, 80), (700, 680, 82),
             (800, 840, 84), (960, 860, 84), (1060, 700, 84), (1180, 560, 84)],
            mover=(-20, -80), aros=(76, 34, 122, 54)),
    }


OFICIAL = "dos-meandros"
_OF = variantes()[OFICIAL]
LIP, IMPACT, V = _OF.lip, _OF.imp, _OF.v


def fondo():
    """Todo lo que no es la cuia ni el chorro (el oficial: el río con dos meandros). Sangra mucho para la capa adaptativa."""
    return _OF.fondo()


def frente():
    """La cuia, el chorro y los aros del icono oficial."""
    return _OF.frente()


def monocroma():
    """La monocroma del icono oficial."""
    return _OF.monocroma()


def zona_segura_dp(pieza=None):
    """Distancia máxima (dp) al centro del icono adaptativo de la cuia (vidrio, pie y borde), con la escala de ADAPT; la zona
    segura de Android es un círculo de 33 dp de radio."""
    v = (pieza or _OF).v
    m = max(math.hypot(x - 512, y - 512) for x, y in v["glass"] + v["foot"] + v["rim"])
    return m * 0.86 / 1024 * 108


def icono_en(x, y, size, rid, pieza=None):
    """El icono maestro dentro de un cuadrado redondeado, para el logo."""
    s = size / 1024
    fo, fr = (pieza.fondo, pieza.frente) if pieza else (fondo, frente)
    return (f'<clipPath id="{rid}"><rect x="0" y="0" width="1024" height="1024" rx="230"/></clipPath>'
            f'<g transform="translate({x} {y}) scale({s:.5f})"><g clip-path="url(#{rid})">{fo()}\n{fr()}</g></g>')


def grafico_funciones(font, pieza=None):
    """Banner provisional de Play: icono y nombre, compuestos con las mismas piezas del logo horizontal."""
    d, _ = texto_a_path(font, "Tintalú", 0, 0, 180, track=-0.005)
    return f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1024 500" width="1024" height="500">
{DEFS}
  <rect width="1024" height="500" fill="url(#cielo)"/>
  {icono_en(28, 70, 360, "feature-rio", pieza)}
  <path d="{d}" fill="{MARFIL_TXT}" transform="translate(410 294)"/>
</svg>
'''


def main(argv=None):
    """Sin argumentos, el icono oficial en ./out. Con --variante <nombre> --out <carpeta>, un boceto de meandros."""
    argv = sys.argv[1:] if argv is None else argv
    out, pieza = OUT, None
    if argv:
        vs = variantes()
        if len(argv) != 4 or argv[0] != "--variante" or argv[2] != "--out" or argv[1] not in vs:
            print("Uso: python generar.py [--variante {" + ",".join(vs) + "} --out <carpeta>]")
            return 2
        pieza, out = vs[argv[1]], os.path.abspath(argv[3])
    fo, fr, mo = (pieza.fondo, pieza.frente, pieza.monocroma) if pieza else (fondo, frente, monocroma)
    os.makedirs(out, exist_ok=True)
    w = lambda name, body: open(os.path.join(out, name), "w", encoding="utf-8").write(body)
    w("icono-1024.svg", svg(f"{fo()}\n  {fr()}"))
    w("adaptativo-fondo.svg", svg(f"<g {ADAPT}>{fo()}</g>"))
    w("adaptativo-frente.svg", svg(f"<g {ADAPT}>{fr()}</g>"))
    w("adaptativo-monocromo.svg", svg(f"<g {ADAPT}>{mo()}</g>"))
    w("adaptativo-vista.svg", svg(f"<g {ADAPT}>{fo()}\n{fr()}</g>"))
    font = TTF(FONT)
    for tema, color in (("claro", PETROLEO_TXT), ("oscuro", MARFIL_TXT)):
        d, tw = texto_a_path(font, "Tintalú", 0, 0, 300, track=-0.005)
        # horizontal: icono de 360 a la izquierda; texto con la línea base alineada al centro óptico
        W = 360 + 60 + tw
        w(f"logo-horizontal-{tema}.svg",
          f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="-20 -20 {W + 40:.0f} 400" width="{W + 40:.0f}" height="400">\n'
          f'{DEFS}\n{icono_en(0, 0, 360, "r1", pieza)}\n'
          f'<path d="{d}" fill="{color}" transform="translate(420 262)"/>\n</svg>\n')
        Wv = max(tw, 420)
        w(f"logo-vertical-{tema}.svg",
          f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="-20 -20 {Wv + 40:.0f} 740" width="{Wv + 40:.0f}" height="740">\n'
          f'{DEFS}\n{icono_en((Wv - 420) / 2, 0, 420, "r2", pieza)}\n'
          f'<path d="{d}" fill="{color}" transform="translate({(Wv - tw) / 2:.1f} 680)"/>\n</svg>\n')
    w("feature-graphic.svg", grafico_funciones(font, pieza))
    print("inclinación", round(math.degrees((pieza.v if pieza else V)["r"]), 1), "grados; cuia a",
          round(zona_segura_dp(pieza), 1), "dp del centro (zona segura: 33); SVG en", out)
    return 0


if __name__ == "__main__":
    sys.exit(main())
