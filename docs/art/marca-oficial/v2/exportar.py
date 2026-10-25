"""Exporta a PNG los SVG de ./out con Chrome sin interfaz (transparencia conservada). Solo biblioteca estándar.

Uso: python exportar.py   (antes: python generar.py)
     python exportar.py --android-res <carpeta res>   además, las capas del icono adaptativo por densidad
       (drawable-{m,h,xh,xxh,xxxh}dpi/af_launcher_{background,foreground,monochrome}.png: 108 dp a 1×-4×), para
       Assets/_Project/Plugins/Android/AfIcon.androidlib/src/main/res.
"""
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


def main():
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
