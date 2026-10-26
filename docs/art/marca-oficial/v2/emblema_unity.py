"""El emblema v2 («El río que se tiñe», D-045) como capas de polígonos para el juego (añadido de D-051). Solo biblioteca estándar.

Lee el SVG que escribe generar.py (fondo() y frente(): la selva con sus palmeras, el río blanco, el açaí que se abre desde el
impacto, la orilla de acá, el chorro, el aro del impacto y la cuia de vidrio con sus tres dosis) y lo convierte en capas de
contornos aplanados, en el orden de pintado del SVG, con su relleno (color liso o degradado vertical de hasta 3 paradas), su
opacidad y, en las tramas (puntos, escamas, rombos y trenza), la capa a la que se recortan. Los trazos se convierten en
polígonos (cuadriláteros por tramo y discos en las juntas y los cabos redondos). El juego los rasteriza nítidos a cualquier
densidad (BrandEmblem). Lo usa exportar.py --unity-emblem.

Grupos (lo que el juego anima por separado): 0 base (palmeras, selva, franja, río blanco y sus ondas), 1 açaí (el teñido y sus
ondas), 2 orilla de acá, 3 impacto (aros y gotitas), 4 chorro, 5 cuia.
"""
import math
import os
import re
import sys
import xml.etree.ElementTree as ET

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import generar  # noqa: E402

BASE, ACAI, BANK, IMPACT, STREAM, CUIA = range(6)

# Degradados de generar.py (DEFS): paradas (desplazamiento, #RRGGBB, opacidad).
GRADIENTES = {
    "aguaBlanca": [(0.0, "#8F6B3F", 1.0), (0.25, generar.AGUA, 1.0), (1.0, "#C29C6A", 1.0)],
    "aguaAcai": [(0.0, "#4E1F6E", 1.0), (0.25, generar.ACAI, 1.0), (1.0, "#7E3AAE", 1.0)],
    "chorro": [(0.0, generar.ACAI, 1.0), (1.0, "#8E44C4", 1.0)],
    "vidrio": [(0.0, "#FFFFFF", 0.18), (1.0, "#FFFFFF", 0.02)],
}


def grupo_de(fill, stroke, seccion):
    """El grupo de un elemento por su color y su sección (fondo o frente)."""
    color = (fill if fill not in (None, "none") else stroke) or ""
    if seccion == "frente":
        if color == "url(#chorro)" or color == "#E7C8FA":
            return STREAM
        if color in ("#EBD5F8", "#B57BE0"):
            return IMPACT
        return CUIA
    if color in ("#8A4FA8", "url(#aguaAcai)", "#A866DA", "#CDA0F0"):
        return ACAI
    if color == "#0E4232" and fill == "#0E4232" and seccion == "orilla":
        return BANK
    return BASE


# ----------------------------------------------------------------------------------------- trazados
def tokens(d):
    for m in re.finditer(r"[MLCQqZz]|-?\d*\.?\d+(?:e-?\d+)?", d):
        yield m.group(0)


def aplanar(d):
    """Subtrazados de d (M, L, C, Q, q y Z) aplanados: lista de (puntos, cerrado)."""
    t = list(tokens(d))
    i = 0
    subs, cur, cmd = [], [], None
    pos = (0.0, 0.0)

    def num():
        nonlocal i
        v = float(t[i])
        i += 1
        return v

    while i < len(t):
        if re.match(r"[A-Za-z]", t[i]):
            cmd = t[i]
            i += 1
            if cmd in "Zz":
                if cur:
                    subs.append((cur, True))
                cur = []
                continue
        if cmd == "M":
            if cur:
                subs.append((cur, False))
            pos = (num(), num())
            cur = [pos]
            cmd = "L"
        elif cmd == "L":
            pos = (num(), num())
            cur.append(pos)
        elif cmd == "C":
            c1, c2, p = (num(), num()), (num(), num()), (num(), num())
            largo = math.dist(pos, c1) + math.dist(c1, c2) + math.dist(c2, p)
            n = max(4, min(48, int(largo / 10)))
            for k in range(1, n + 1):
                s = k / n
                u = 1 - s
                cur.append((u ** 3 * pos[0] + 3 * u * u * s * c1[0] + 3 * u * s * s * c2[0] + s ** 3 * p[0],
                            u ** 3 * pos[1] + 3 * u * u * s * c1[1] + 3 * u * s * s * c2[1] + s ** 3 * p[1]))
            pos = p
        elif cmd in "Qq":
            c, p = (num(), num()), (num(), num())
            if cmd == "q":
                c, p = (pos[0] + c[0], pos[1] + c[1]), (pos[0] + p[0], pos[1] + p[1])
            largo = math.dist(pos, c) + math.dist(c, p)
            n = max(3, min(32, int(largo / 8)))
            for k in range(1, n + 1):
                s = k / n
                u = 1 - s
                cur.append((u * u * pos[0] + 2 * u * s * c[0] + s * s * p[0], u * u * pos[1] + 2 * u * s * c[1] + s * s * p[1]))
            pos = p
        else:
            raise ValueError("comando no soportado: " + str(cmd))
    if cur:
        subs.append((cur, False))
    return subs


def elipse(cx, cy, rx, ry, n=72):
    return [(cx + rx * math.cos(2 * math.pi * k / n), cy + ry * math.sin(2 * math.pi * k / n)) for k in range(n)]


def area(p):
    return sum(p[i][0] * p[(i + 1) % len(p)][1] - p[(i + 1) % len(p)][0] * p[i][1] for i in range(len(p))) / 2


def positivo(p):
    return p if area(p) > 0 else list(reversed(p))


def disco(c, r):
    n = max(10, min(24, int(r * 1.2)))
    return positivo([(c[0] + r * math.cos(2 * math.pi * k / n), c[1] + r * math.sin(2 * math.pi * k / n)) for k in range(n)])


def trazo(pts, cerrado, ancho, redondo):
    """El contorno de un trazo de ancho dado (juntas en inglete con límite 2): un polígono si está abierto (con cabos redondos o
    rectos) y dos de sentido contrario si está cerrado (la regla no nula deja el anillo)."""
    h = ancho / 2
    p = [q for k, q in enumerate(pts) if k == 0 or math.dist(q, pts[k - 1]) > 1e-6]
    if cerrado and len(p) > 2 and math.dist(p[0], p[-1]) < 1e-6:
        p.pop()
    n = len(p)
    if n < 2:
        return []

    def normal(a, b):
        dx, dy = b[0] - a[0], b[1] - a[1]
        ln = math.hypot(dx, dy)
        return (-dy / ln, dx / ln)

    izq, der = [], []
    for k in range(n):
        if cerrado or 0 < k < n - 1:
            n1 = normal(p[(k - 1) % n], p[k])
            n2 = normal(p[k], p[(k + 1) % n])
            mx, my = n1[0] + n2[0], n1[1] + n2[1]
            ln = math.hypot(mx, my)
            if ln < 1e-6:
                mx, my, ln = n1[0], n1[1], 1.0
            mx, my = mx / ln, my / ln
            f = h / max(0.72, mx * n1[0] + my * n1[1])
        else:
            mx, my = normal(p[0], p[1]) if k == 0 else normal(p[-2], p[-1])
            f = h
        izq.append((p[k][0] + mx * f, p[k][1] + my * f))
        der.append((p[k][0] - mx * f, p[k][1] - my * f))
    if cerrado:
        return [izq, list(reversed(der))]

    def cabo(c, desde, hasta):
        if not redondo:
            return []
        # De un lado al otro por delante del cabo: media vuelta en sentido negativo (con la normal (−dy, dx)).
        a0 = math.atan2(desde[1] - c[1], desde[0] - c[0])
        da = -math.pi
        m = 8
        return [(c[0] + h * math.cos(a0 + da * t / m), c[1] + h * math.sin(a0 + da * t / m)) for t in range(1, m)]

    poli = izq + cabo(p[-1], izq[-1], der[-1]) + list(reversed(der)) + cabo(p[0], der[0], izq[0])
    return [positivo(poli)]


# ----------------------------------------------------------------------------------------- tramas
def caja(polis):
    xs = [x for p in polis for x, _ in p]
    ys = [y for p in polis for _, y in p]
    return min(xs), min(ys), max(xs), max(ys)


def trama(nombre, polis):
    """Capas (color, opacidad, polígonos) de la trama nombre sobre la caja de polis, en coordenadas de usuario."""
    x0, y0, x1, y1 = caja(polis)
    if nombre == "puntos":
        c = [disco((17 + 34 * i, 17 + 34 * j), 8) for i in range(int(x0 // 34) - 1, int(x1 // 34) + 2)
             for j in range(int(y0 // 34) - 1, int(y1 // 34) + 2)]
        return [("#B88400", 0.45, c)]
    if nombre == "rombos":
        c = [positivo([(17 + 34 * i, 7 + 34 * j), (27 + 34 * i, 17 + 34 * j), (17 + 34 * i, 27 + 34 * j), (7 + 34 * i, 17 + 34 * j)])
             for i in range(int(x0 // 34) - 1, int(x1 // 34) + 2) for j in range(int(y0 // 34) - 1, int(y1 // 34) + 2)]
        return [("#4A1C68", 0.55, c)]
    if nombre == "escamas":
        c = []
        for i in range(int(x0 // 30) - 1, int(x1 // 30) + 2):
            for j in range(int(y0 // 22) - 1, int(y1 // 22) + 2):
                arco = [(30 * i + 15 - 15 * math.cos(math.pi * k / 8), 22 * j + 22 - 15 * math.sin(math.pi * k / 8)) for k in range(9)]
                c += trazo(arco, False, 4, False)
        return [("#B4520A", 0.5, c)]
    if nombre == "trenza":
        # rotate(45): la x de la trama es u = (x + y) / √2; bandas oscuras con u en [16k, 16k + 8).
        r = math.sqrt(0.5)
        us = [(x + y) * r for x in (x0, x1) for y in (y0, y1)]
        vs = [(y - x) * r for x in (x0, x1) for y in (y0, y1)]
        v0, v1 = min(vs) - 2, max(vs) + 2
        c = []
        for k in range(int(min(us) // 16) - 1, int(max(us) // 16) + 2):
            u0, u1 = 16 * k, 16 * k + 8
            c.append(positivo([((u - v) * r, (u + v) * r) for u, v in ((u0, v0), (u1, v0), (u1, v1), (u0, v1))]))
        return [("#8E5A2B", 1.0, c)]
    raise ValueError(nombre)


# ----------------------------------------------------------------------------------------- capas
def capas():
    """Lista de capas: dict(grupo, clip, opacidad, paradas[(o, '#RRGGBB', a)], gy0, gy1, polis)."""
    out = []
    ultimo_relleno = {}

    def nueva(grupo, polis, paradas, opacidad, clip=-1, gcaja=None):
        polis = [p for p in polis if len(p) >= 3]
        if not polis:
            return -1
        gy0, gy1 = (gcaja or caja(polis))[1], (gcaja or caja(polis))[3]
        out.append(dict(grupo=grupo, clip=clip, opacidad=opacidad, paradas=paradas, gy0=gy0, gy1=gy1, polis=polis))
        return len(out) - 1

    def recorrer(el, hered, seccion):
        tag = el.tag
        a = dict(hered)
        a.update(el.attrib)
        if tag == "g":
            for h in el:
                recorrer(h, {k: v for k, v in a.items() if k in ("fill", "stroke", "stroke-width", "stroke-linecap", "opacity")}, seccion)
            return
        fill = el.attrib.get("fill", hered.get("fill"))
        stroke = a.get("stroke")
        if fill in ("url(#cielo)", "url(#halo)"):
            return
        opacidad = float(el.attrib.get("opacity", hered.get("opacity", 1)))
        dx = dy = 0.0
        tr = el.attrib.get("transform")
        if tr:
            m = re.match(r"translate\(([-\d.]+)[ ,]+([-\d.]+)\)", tr)
            dx, dy = float(m.group(1)), float(m.group(2))
        if tag == "path":
            subs = aplanar(el.attrib["d"])
        elif tag == "ellipse":
            subs = [(elipse(float(a["cx"]), float(a["cy"]), float(a["rx"]), float(a["ry"])), True)]
        elif tag == "circle":
            subs = [(elipse(float(a["cx"]), float(a["cy"]), float(a["r"]), float(a["r"]), 32), True)]
        else:
            return
        subs = [([(x + dx, y + dy) for x, y in p], c) for p, c in subs]
        sec = seccion
        if seccion == "fondo" and fill == "#0E4232" and el.attrib.get("d", "").startswith("M -600 1700"):
            sec = "orilla"
        if seccion == "fondo" and stroke == "#26603A" and el.attrib.get("d", "").startswith("M -600 908"):
            grupo = BANK
        else:
            grupo = grupo_de(fill, stroke, sec)
        clave = el.attrib.get("d", "")
        if fill not in (None, "none"):
            polis = [p for p, _ in subs]
            m = re.match(r"url\(#(\w+)\)", fill)
            if m and m.group(1) in ("puntos", "escamas", "rombos", "trenza"):
                if m.group(1) == "trenza":
                    base = nueva(grupo, polis, [(0.0, "#C98A45", 1.0)], opacidad)
                else:
                    base = ultimo_relleno[clave]
                for color, op, tp in trama(m.group(1), polis):
                    nueva(grupo, tp, [(0.0, color, 1.0)], op * opacidad, clip=base)
                ultimo_relleno[clave] = base
            elif m:
                ultimo_relleno[clave] = nueva(grupo, polis, GRADIENTES[m.group(1)], opacidad)
            else:
                ultimo_relleno[clave] = nueva(grupo, polis, [(0.0, fill, 1.0)], opacidad)
        if stroke not in (None, "none"):
            ancho = float(a.get("stroke-width", 1))
            redondo = a.get("stroke-linecap") == "round" or a.get("stroke-linejoin") == "round"
            polis = []
            for p, c in subs:
                polis += trazo(p, c, ancho, redondo)
            nueva(grupo, polis, [(0.0, stroke, 1.0)], opacidad)

    raiz = ET.fromstring("<svg><fondo>" + generar.fondo() + "</fondo><frente>" + generar.frente() + "</frente></svg>")
    for seccion in ("fondo", "frente"):
        for el in raiz.find(seccion):
            recorrer(el, {}, seccion)
    return out


def rgba(hexa, a):
    h = hexa.lstrip("#")
    return f"0x{h.upper()}{int(round(a * 255)):02X}u"


def cs():
    lista = capas()
    num = lambda v: (f"{v:.1f}".rstrip("0").rstrip(".") or "0").replace("-0", "-0") + "f"
    puntos, fines, primeros, cuantos = [], [], [], []
    for c in lista:
        primeros.append(len(fines))
        cuantos.append(len(c["polis"]))
        for p in c["polis"]:
            for x, y in p:
                puntos += [num(x), num(y)]
            fines.append(len(puntos) // 2)
    paradas, colores = [], []
    for c in lista:
        ps = list(c["paradas"])
        while len(ps) < 3:
            ps.append((1.0, ps[-1][1], ps[-1][2]))
        for o, col, a in ps:
            paradas.append(num(o))
            colores.append(rgba(col, a))

    def filas(vals, ancho=16):
        return "\n".join("            " + ", ".join(vals[i:i + ancho]) + "," for i in range(0, len(vals), ancho))

    lip = generar.LIP
    imp = generar.IMPACT
    return f"""// <auto-generated>
// Generado por docs/art/marca-oficial/v2/exportar.py --unity-emblem (añadido de D-051) desde generar.py: no se edita a mano.
// </auto-generated>
namespace AF.Presentation
{{
    /// <summary>
    /// El emblema v2 en capas de polígonos aplanados (unidades del icono de 1024, y hacia abajo), en el orden de pintado del SVG:
    /// las rasteriza <see cref="BrandEmblem"/>.
    /// </summary>
    public static partial class BrandEmblem
    {{
        /// <summary>Canto de la boca de la cuia, de donde sale el chorro (generar.py, LIP).</summary>
        public const float LipX = {num(lip[0])};

        /// <summary>Canto de la boca de la cuia (y).</summary>
        public const float LipY = {num(lip[1])};

        /// <summary>Punto del impacto del chorro en el río (generar.py, IMPACT).</summary>
        public const float ImpactX = {num(imp[0])};

        /// <summary>Punto del impacto (y).</summary>
        public const float ImpactY = {num(imp[1])};

        /// <summary>Número de capas.</summary>
        public const int LayerCount = {len(lista)};

        internal static readonly int[] LayerGroup = {{ {", ".join(str(c["grupo"]) for c in lista)} }};

        internal static readonly int[] LayerClip = {{ {", ".join(str(c["clip"]) for c in lista)} }};

        internal static readonly float[] LayerOpacity = {{ {", ".join(num(c["opacidad"]) for c in lista)} }};

        internal static readonly float[] LayerGradientY0 = {{ {", ".join(num(c["gy0"]) for c in lista)} }};

        internal static readonly float[] LayerGradientY1 = {{ {", ".join(num(c["gy1"]) for c in lista)} }};

        /// <summary>Tres paradas por capa: desplazamiento (0-1).</summary>
        internal static readonly float[] LayerStops =
        {{
{filas(paradas, 15)}
        }};

        /// <summary>Tres paradas por capa: color 0xRRGGBBAA.</summary>
        internal static readonly uint[] LayerColors =
        {{
{filas(colores, 9)}
        }};

        internal static readonly int[] LayerFirstContour = {{ {", ".join(str(v) for v in primeros)} }};

        internal static readonly int[] LayerContourCount = {{ {", ".join(str(v) for v in cuantos)} }};

        /// <summary>Índice (en puntos) donde termina cada contorno de <see cref="Points"/>.</summary>
        internal static readonly int[] ContourEnds =
        {{
{filas([str(f) for f in fines], 20)}
        }};

        /// <summary>Puntos (x, y) seguidos.</summary>
        internal static readonly float[] Points =
        {{
{filas(puntos)}
        }};
    }}
}}
"""


if __name__ == "__main__":
    for c in capas():
        print(c["grupo"], c["clip"], c["opacidad"], c["paradas"][0][1], len(c["polis"]), [round(v) for v in caja(c["polis"])])
