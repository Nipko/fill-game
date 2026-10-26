"""Exporta a PNG los SVG de ./out con Chrome sin interfaz (transparencia conservada). Solo biblioteca estándar.

Uso: python exportar.py   (antes: python generar.py)
     python exportar.py --android-res <carpeta res>   además, las capas del icono adaptativo por densidad
       (drawable-{m,h,xh,xxh,xxxh}dpi/af_launcher_{background,foreground,monochrome}.png: 108 dp a 1×-4×), para
       Assets/_Project/Plugins/Android/AfIcon.androidlib/src/main/res; escribe también los XML de --android-xml.
     python exportar.py --android-xml <carpeta res> [--check]   solo los XML de la pantalla del sistema (D-051, punto 2), sin
       Chrome: values/af_splash.xml (el petróleo y el tema de arranque) y values-v31/af_splash.xml (la pantalla de Android 12+:
       el icono adaptativo sobre el petróleo). Con --check no escribe nada: compara y sale con 1 si difieren.
     python exportar.py --unity-wordmark <archivo .cs> [--check]   solo el nombre del logo para la pantalla de arranque del
       juego (añadido de D-051), sin Chrome: los contornos de «Tintalú» en Baloo 2 ExtraBold (los del logo, generar.py:
       cuerpo 300 y espaciado -0,005), aplanados en polilíneas, como datos de C# para
       Assets/_Project/Scripts/Presentation/BrandWordmark.Generated.cs; el juego los rasteriza nítidos a cualquier densidad.
       Con --check no escribe nada: compara y sale con 1 si difiere.
     python exportar.py --unity-emblem <archivo .cs> [--check]   el emblema v2 para la intro y la pantalla de arranque del juego
       (añadido de D-051), sin Chrome: el SVG de generar.py (selva, río, açaí, orilla, chorro, impacto y la cuia con sus dosis)
       en capas de polígonos (emblema_unity.py), para Assets/_Project/Scripts/Presentation/BrandEmblem.Generated.cs.
       Con --check no escribe nada: compara y sale con 1 si difiere.
Códigos: 0 hecho (o idéntico con --check) · 1 fallo o diferencia · 2 argumentos o sin Chrome.
"""
import math
import os
import subprocess
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "out")
PNG = os.path.join(OUT, "png")
CHROME = next((p for p in (r"C:\Program Files\Google\Chrome\Application\chrome.exe",
                           r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe") if os.path.exists(p)), None)

# (svg, png, ancho, alto)
TRABAJOS = [
    ("icono-1024.svg", "app-store-1024.png", 1024, 1024),
    ("icono-1024.svg", "google-play-512.png", 512, 512),
    ("icono-1024.svg", "favicon-16.png", 16, 16),
    ("icono-1024.svg", "favicon-32.png", 32, 32),
    ("icono-1024.svg", "apple-touch-icon.png", 180, 180),
    ("feature-graphic.svg", "feature-graphic.png", 1024, 500),
    ("adaptativo-fondo.svg", "android-fondo-432.png", 432, 432),
    ("adaptativo-frente.svg", "android-frente-432.png", 432, 432),
    ("adaptativo-monocromo.svg", "android-monocromo-432.png", 432, 432),
    ("adaptativo-vista.svg", "android-vista-432.png", 432, 432),
]
for n in (48, 72, 96, 144, 192):
    TRABAJOS.append(("icono-1024.svg", f"legacy-{n}.png", n, n))
for f in sorted(os.listdir(OUT)):
    if f.startswith("logo-"):
        import re
        head = open(os.path.join(OUT, f), encoding="utf-8").read(400)
        w, h = map(float, re.search(r'width="([\d.]+)" height="([\d.]+)"', head).groups())
        TRABAJOS.append((f, f.replace(".svg", ".png"), int(w), int(h)))


DENSIDADES = {"mdpi": 108, "hdpi": 162, "xhdpi": 216, "xxhdpi": 324, "xxxhdpi": 432}
CAPAS = {"background": "adaptativo-fondo.svg", "foreground": "adaptativo-frente.svg", "monochrome": "adaptativo-monocromo.svg"}


# Fondo petróleo de la marca (D-045; D-051, punto 2): el tono medio del degradado «cielo» del icono (generar.py, DEFS), el
# fondo azul petróleo del icono v2. Lo repite ProjectSetup.BrandPetroleum (C#), y un test EditMode compara los dos.
PETROLEO = "#14444A"

# Recursos de la pantalla del sistema (D-051, punto 2). AfLaunchTheme hereda el tema de Unity (BaseUnityGameActivityTheme, de
# unityLibrary; solo se resuelve al enlazar el lanzador) y Assets/Plugins/Android/LauncherManifest.xml se lo pone a la actividad.
# windowBackground: el petróleo también antes de Android 12 y tras la pantalla del sistema, hasta el primer fotograma del juego.
SPLASH_XML = {
    os.path.join("values", "af_splash.xml"): f"""<?xml version="1.0" encoding="utf-8"?>
<!-- Generado por docs/art/marca-oficial/v2/exportar.py (D-051, punto 2): no se edita a mano. -->
<resources>
    <color name="af_brand_petroleum">{PETROLEO}</color>
    <style name="AfLaunchTheme" parent="BaseUnityGameActivityTheme">
        <item name="android:windowBackground">@color/af_brand_petroleum</item>
    </style>
</resources>
""",
    os.path.join("values-v31", "af_splash.xml"): """<?xml version="1.0" encoding="utf-8"?>
<!-- Generado por docs/art/marca-oficial/v2/exportar.py (D-051, punto 2): no se edita a mano.
     Android 12+: el icono adaptativo v2 (la mascara redonda la pone el sistema) sobre el petroleo de la marca, sin animacion. -->
<resources>
    <style name="AfLaunchTheme" parent="BaseUnityGameActivityTheme">
        <item name="android:windowBackground">@color/af_brand_petroleum</item>
        <item name="android:windowSplashScreenBackground">@color/af_brand_petroleum</item>
        <item name="android:windowSplashScreenAnimatedIcon">@mipmap/af_launcher</item>
        <item name="android:windowSplashScreenAnimationDuration">0</item>
    </style>
</resources>
""",
}


def android_xml(res, check):
    """Escribe (o compara, con check) los XML de la pantalla del sistema. Devuelve 0 si quedan iguales, 1 si no."""
    distintos = 0
    for rel, texto in SPLASH_XML.items():
        dst = os.path.join(res, rel)
        actual = open(dst, encoding="utf-8", newline="").read() if os.path.exists(dst) else None
        if check:
            if actual != texto:
                print("DIFIERE", rel)
                distintos += 1
            else:
                print("identico", rel)
            continue
        if actual != texto:
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            with open(dst, "w", encoding="utf-8", newline="\n") as f:
                f.write(texto)
        print("OK", rel)
    return 1 if distintos else 0


def _plano(a, b, c, n):
    """Puntos de la cuadrática a-b-c (sin el primero), en n tramos."""
    out = []
    for i in range(1, n + 1):
        t = i / n
        u = 1 - t
        out.append((u * u * a[0] + 2 * u * t * b[0] + t * t * c[0], u * u * a[1] + 2 * u * t * b[1] + t * t * c[1]))
    return out


def wordmark_contornos():
    """Contornos de «Tintalú» como en el logo (texto_a_path de generar.py), aplanados: lista de listas de (x, y), y hacia abajo,
    con el origen arriba a la izquierda de su caja. Devuelve (contornos, ancho, alto)."""
    sys.path.insert(0, HERE)
    import generar
    font = generar.TTF(generar.FONT)
    size, track = 300, -0.005
    sc = size / font.upem
    contornos = []
    pen = 0.0
    for ch in "Tintalú":
        g = font.map.get(ord(ch), 0)
        for c in font.contours(g):
            pts = [((px + pen) * sc, -py * sc, on) for px, py, on in c]
            n = len(pts)
            k0 = next((i for i in range(n) if pts[i][2]), None)
            if k0 is None:
                a, b = pts[0], pts[1]
                pts.insert(0, ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2, 1))
                k0 = 0
                n += 1
            pts = pts[k0:] + pts[:k0]
            poli = [(pts[0][0], pts[0][1])]
            i = 1
            while i <= n:
                p = pts[i % n]
                cur = poli[-1]
                if p[2]:
                    poli.append((p[0], p[1]))
                    i += 1
                    continue
                nx = pts[(i + 1) % n]
                if nx[2]:
                    fin = (nx[0], nx[1])
                    i += 2
                else:
                    fin = ((p[0] + nx[0]) / 2, (p[1] + nx[1]) / 2)
                    i += 1
                largo = math.hypot(p[0] - cur[0], p[1] - cur[1]) + math.hypot(fin[0] - p[0], fin[1] - p[1])
                poli += _plano(cur, (p[0], p[1]), fin, max(2, min(16, int(round(largo / 7)))))
            if len(poli) > 1 and math.hypot(poli[-1][0] - poli[0][0], poli[-1][1] - poli[0][1]) < 1e-6:
                poli.pop()
            contornos.append(poli)
        pen += font.adv(g) + track * font.upem
    x0 = min(x for c in contornos for x, _ in c)
    y0 = min(y for c in contornos for _, y in c)
    x1 = max(x for c in contornos for x, _ in c)
    y1 = max(y for c in contornos for _, y in c)
    contornos = [[(round(x - x0, 1), round(y - y0, 1)) for x, y in c] for c in contornos]
    return contornos, round(x1 - x0, 1), round(y1 - y0, 1)


def wordmark_cs():
    contornos, ancho, alto = wordmark_contornos()
    fines, total = [], 0
    for c in contornos:
        total += len(c)
        fines.append(total)
    num = lambda v: (f"{v:.1f}".rstrip("0").rstrip(".") or "0") + "f"
    filas = []
    plano = [num(v) for c in contornos for p in c for v in p]
    for i in range(0, len(plano), 16):
        filas.append("            " + ", ".join(plano[i:i + 16]) + ",")
    fines_txt = ", ".join(str(f) for f in fines)
    return f"""// <auto-generated>
// Generado por docs/art/marca-oficial/v2/exportar.py --unity-wordmark (añadido de D-051): no se edita a mano.
// </auto-generated>
namespace AF.Presentation
{{
    /// <summary>Contornos del nombre del logo v2 (D-045), aplanados: los rasteriza <see cref="BrandWordmark.Render"/>.</summary>
    public static partial class BrandWordmark
    {{
        /// <summary>Ancho de la caja de los contornos (unidades del logo: cuerpo 300).</summary>
        public const float Width = {num(ancho)};

        /// <summary>Alto de la caja de los contornos (unidades del logo), de lo alto de la tilde a la línea base.</summary>
        public const float Height = {num(alto)};

        /// <summary>Índice (en puntos) donde termina cada contorno de <see cref="Points"/>.</summary>
        internal static readonly int[] ContourEnds = {{ {fines_txt} }};

        /// <summary>Puntos (x, y) seguidos, con y hacia abajo y el origen arriba a la izquierda de la caja.</summary>
        internal static readonly float[] Points =
        {{
{chr(10).join(filas)}
        }};
    }}
}}
"""


def unity_emblem(dst, check):
    sys.path.insert(0, HERE)
    import emblema_unity
    return escribir(dst, emblema_unity.cs(), check)


def unity_wordmark(dst, check):
    return escribir(dst, wordmark_cs(), check)


def escribir(dst, texto, check):
    actual = open(dst, encoding="utf-8", newline="").read() if os.path.exists(dst) else None
    if check:
        print("identico" if actual == texto else "DIFIERE", os.path.basename(dst))
        return 0 if actual == texto else 1
    if actual != texto:
        os.makedirs(os.path.dirname(os.path.abspath(dst)), exist_ok=True)
        with open(dst, "w", encoding="utf-8", newline="\n") as f:
            f.write(texto)
    print("OK", os.path.basename(dst))
    return 0


def main():
    if len(sys.argv) in (3, 4) and sys.argv[1] == "--unity-emblem":
        if len(sys.argv) == 4 and sys.argv[3] != "--check":
            print(__doc__)
            return 2
        return unity_emblem(sys.argv[2], len(sys.argv) == 4)
    if len(sys.argv) in (3, 4) and sys.argv[1] == "--unity-wordmark":
        if len(sys.argv) == 4 and sys.argv[3] != "--check":
            print(__doc__)
            return 2
        return unity_wordmark(sys.argv[2], len(sys.argv) == 4)
    if len(sys.argv) in (3, 4) and sys.argv[1] == "--android-xml":
        if len(sys.argv) == 4 and sys.argv[3] != "--check":
            print(__doc__)
            return 2
        return android_xml(sys.argv[2], len(sys.argv) == 4)
    if CHROME is None:
        print("FALLO: no encuentro Chrome ni Edge")
        return 2
    os.makedirs(PNG, exist_ok=True)
    trabajos = [(svg, os.path.join(PNG, png), w, h) for svg, png, w, h in TRABAJOS]
    if len(sys.argv) == 3 and sys.argv[1] == "--android-res":
        for dens, px in DENSIDADES.items():
            carpeta = os.path.join(sys.argv[2], f"drawable-{dens}")
            os.makedirs(carpeta, exist_ok=True)
            for capa, svg in CAPAS.items():
                trabajos.append((svg, os.path.join(carpeta, f"af_launcher_{capa}.png"), px, px))
        if android_xml(sys.argv[2], False) != 0:
            return 1
    elif len(sys.argv) > 1:
        print(__doc__)
        return 2
    tmp = tempfile.mkdtemp(prefix="tintalu-export-")
    for svg, dst, w, h in trabajos:
        png = os.path.relpath(dst, HERE)
        html = os.path.join(tmp, "p.html")
        src = "file:///" + os.path.join(OUT, svg).replace("\\", "/")
        open(html, "w", encoding="utf-8").write(
            f'<!doctype html><html><body style="margin:0;background:transparent">'
            f'<img src="{src}" style="display:block;width:{w}px;height:{h}px"></body></html>')
        if os.path.exists(dst):
            os.remove(dst)
        r = subprocess.run([CHROME, "--headless=new", "--disable-gpu", "--hide-scrollbars",
                            "--default-background-color=00000000", f"--window-size={w},{h}",
                            "--allow-file-access-from-files", f"--screenshot={dst}", "file:///" + html.replace("\\", "/")],
                           capture_output=True, text=True, timeout=120)
        if not os.path.exists(dst):
            print("FALLO", png, r.stderr[-400:])
            return 1
        print("OK", png, f"{w}x{h}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
