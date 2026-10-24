"""Exporta a PNG los SVG de ./out con Chrome sin interfaz (transparencia conservada). Solo biblioteca estándar.

Uso: python exportar.py   (antes: python generar.py)
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


def main():
    if CHROME is None:
        print("FALLO: no encuentro Chrome ni Edge")
        return 2
    os.makedirs(PNG, exist_ok=True)
    tmp = tempfile.mkdtemp(prefix="tintalu-export-")
    for svg, png, w, h in TRABAJOS:
        html = os.path.join(tmp, "p.html")
        src = "file:///" + os.path.join(OUT, svg).replace("\\", "/")
        open(html, "w", encoding="utf-8").write(
            f'<!doctype html><html><body style="margin:0;background:transparent">'
            f'<img src="{src}" style="display:block;width:{w}px;height:{h}px"></body></html>')
        dst = os.path.join(PNG, png)
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
