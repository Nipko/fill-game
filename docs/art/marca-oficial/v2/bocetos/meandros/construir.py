"""Bocetos de meandros del icono v2 (fase 1, 2026-10-27): SVG, PNG y hoja de comparación. Solo biblioteca estándar.

Petición del dueño: el río del logo no se entiende como río; mantener la idea («El río que se tiñe», D-045) con meandros,
como en la pantalla de carga (BrandRiverScene, añadido de D-051).

Uso: python construir.py
  1. `generar.py --variante <nombre> --out <nombre>/` escribe los SVG de cada variante en esta carpeta;
  2. con Chrome o Edge sin interfaz (el método de exportar.py) los pasa a PNG en <nombre>/png/: el icono a 1024, 192, 96
     y 48 px, las capas adaptativas de Android a 432 (fondo, frente y monocroma) y el logo horizontal claro y oscuro;
  3. compone la hoja (HTML temporal con las imágenes incrustadas) y su captura hoja-meandros.png, con la versión actual
     (../../out, sin tocar) al lado para comparar.
No toca out/, AfIcon.androidlib, BrandEmblem.Generated.cs ni la web. El dueño eligió la B (añadido de D-045 del 2026-10-27); desde
la fase 2, out/ ya es la B, así que la fila «actual» de la hoja muestra la B oficial y no el río recto (la hoja de la fase 1 está
en el commit 78e236bf; el río recto, con `generar.py --variante recto`).
Códigos: 0 hecho · 1 fallo · 2 sin Chrome.
"""
import base64
import os
import re
import subprocess
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
V2 = os.path.abspath(os.path.join(HERE, "..", ".."))
CHROME = next((p for p in (r"C:\Program Files\Google\Chrome\Application\chrome.exe",
                           r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe") if os.path.exists(p)), None)

VARIANTES = [("s-amplia", "A · un meandro amplio en S"),
             ("dos-meandros", "B · dos meandros con perspectiva"),
             ("aereo", "C · desde el aire")]

# (svg, png, ancho, alto); los logos toman su tamaño del SVG
PIEZAS = [("icono-1024.svg", "icono-1024.png", 1024, 1024),
          ("icono-1024.svg", "icono-192.png", 192, 192),
          ("icono-1024.svg", "icono-96.png", 96, 96),
          ("icono-1024.svg", "icono-48.png", 48, 48),
          ("adaptativo-fondo.svg", "android-fondo-432.png", 432, 432),
          ("adaptativo-frente.svg", "android-frente-432.png", 432, 432),
          ("adaptativo-monocromo.svg", "android-monocromo-432.png", 432, 432),
          ("logo-horizontal-claro.svg", "logo-horizontal-claro.png", 0, 0),
          ("logo-horizontal-oscuro.svg", "logo-horizontal-oscuro.png", 0, 0)]

# La versión actual, de ../../out (generada por generar.py sin argumentos): mismas piezas con los nombres de exportar.py.
ACTUAL = {"icono-1024.png": "app-store-1024.png", "icono-192.png": "legacy-192.png", "icono-96.png": "legacy-96.png",
          "icono-48.png": "legacy-48.png"}


def png_chrome(svg, dst, w, h, tmp):
    """Igual que exportar.py: una página con el SVG a su tamaño y la captura con fondo transparente."""
    html = os.path.join(tmp, "p.html")
    src = "file:///" + svg.replace("\\", "/")
    with open(html, "w", encoding="utf-8") as f:
        f.write(f'<!doctype html><html><body style="margin:0;background:transparent">'
                f'<img src="{src}" style="display:block;width:{w}px;height:{h}px"></body></html>')
    return captura(html, dst, w, h, transparente=True)


def captura(html, dst, w, h, transparente=False):
    if os.path.exists(dst):
        os.remove(dst)
    args = [CHROME, "--headless=new", "--disable-gpu", "--hide-scrollbars", f"--window-size={w},{h}",
            "--allow-file-access-from-files", f"--screenshot={dst}", "file:///" + html.replace("\\", "/")]
    if transparente:
        args.insert(4, "--default-background-color=00000000")
    r = subprocess.run(args, capture_output=True, text=True, timeout=180)
    if not os.path.exists(dst):
        print("FALLO", os.path.relpath(dst, HERE), r.stderr[-400:])
        return False
    print("OK", os.path.relpath(dst, HERE), f"{w}x{h}")
    return True


def tam(svg):
    head = open(svg, encoding="utf-8").read(400)
    w, h = map(float, re.search(r'width="([\d.]+)" height="([\d.]+)"', head).groups())
    return int(w), int(h)


def exportar(nombre, tmp):
    carpeta = os.path.join(HERE, nombre)
    r = subprocess.run([sys.executable, os.path.join(V2, "generar.py"), "--variante", nombre, "--out", carpeta],
                       capture_output=True, text=True)
    if r.returncode != 0:
        print("FALLO generar.py", nombre, r.stdout, r.stderr)
        return False
    os.makedirs(os.path.join(carpeta, "png"), exist_ok=True)
    for svg, png, w, h in PIEZAS:
        src = os.path.join(carpeta, svg)
        if not w:
            w, h = tam(src)
        if not png_chrome(src, os.path.join(carpeta, "png", png), w, h, tmp):
            return False
    return True


def ruta(nombre, png):
    if nombre == "actual":
        return os.path.join(V2, "out", "png", ACTUAL.get(png, png))
    return os.path.join(HERE, nombre, "png", png)


def hoja(tmp):
    u = lambda f: "data:image/png;base64," + base64.b64encode(open(f, "rb").read()).decode()

    def mascara(n, forma, size, zona=False):
        r = {"circulo": "50%", "squircle": "30%"}[forma]
        bg, fg = u(ruta(n, "android-fondo-432.png")), u(ruta(n, "android-frente-432.png"))
        # el lanzador muestra los 72 dp centrales de los 108; la zona segura es el círculo de 66 dp
        z = (f'<div style="position:absolute;left:{size * 3 / 72:.1f}px;top:{size * 3 / 72:.1f}px;width:{size * 66 / 72:.1f}px;'
             f'height:{size * 66 / 72:.1f}px;border-radius:50%;border:2px dashed rgba(255,255,255,.75);box-sizing:border-box"></div>'
             if zona else "")
        return (f'<div style="width:{size}px;height:{size}px;border-radius:{r};overflow:hidden;position:relative">'
                f'<img src="{bg}" style="position:absolute;width:{size * 1.5}px;left:{-size * 0.25}px;top:{-size * 0.25}px">'
                f'<img src="{fg}" style="position:absolute;width:{size * 1.5}px;left:{-size * 0.25}px;top:{-size * 0.25}px">{z}</div>')

    def tema(n, fondo, tinta, size):
        mono = u(ruta(n, "android-monocromo-432.png"))
        return (f'<div style="width:{size}px;height:{size}px;border-radius:50%;overflow:hidden;position:relative;background:{fondo}">'
                f'<div style="position:absolute;width:{size * 1.5}px;height:{size * 1.5}px;left:{-size * 0.25}px;top:{-size * 0.25}px;'
                f'background:{tinta};-webkit-mask:url({mono}) center/contain no-repeat;mask:url({mono}) center/contain no-repeat"></div></div>')

    fig = lambda c, t: f'<figure>{c}<figcaption>{t}</figcaption></figure>'
    filas = []
    for n, titulo in [("actual", "Actual (D-045): el río recto")] + VARIANTES:
        ic = lambda px, w=None: f'<img src="{u(ruta(n, f"icono-{px}.png"))}" width="{w or px}" style="border-radius:22%">'
        filas.append(f'''<section><h2>{titulo}</h2><div class="fila">
{fig(ic(1024, 260), "1024 (iOS / Play)")}
{fig(mascara(n, "circulo", 170, True), "círculo · zona segura 66 dp")}{fig(mascara(n, "squircle", 170), "squircle")}
{fig(ic(192), "192")}{fig(ic(96), "96")}{fig(ic(48), "48")}
{fig(tema(n, "#D9E7DF", "#1F4D3B", 120), "temático claro")}{fig(tema(n, "#2A3B33", "#BFE3CF", 120), "temático oscuro")}
{fig(tema(n, "#D9E7DF", "#1F4D3B", 48), "48")}</div>
<div class="fila"><img src="{u(ruta(n, "logo-horizontal-claro.png"))}" height="100" style="background:#fff;border-radius:10px;padding:10px">
<div class="oscuro"><img src="{u(ruta(n, "logo-horizontal-oscuro.png"))}" height="100"></div></div></section>''')
    todos = [("actual", "actual")] + [(n, t.split(" ·")[0]) for n, t in VARIANTES]
    junto = "".join(fig(f'<img src="{u(ruta(n, "icono-48.png"))}" width="48" style="border-radius:22%">'
                        f' <img src="{u(ruta(n, "icono-48.png"))}" width="32" style="border-radius:22%">', t) for n, t in todos)
    junto_m = "".join(fig(tema(n, "#2A3B33", "#BFE3CF", 48), t) for n, t in todos)
    pantalla = "".join(fig(mascara(n, "circulo", 56), t) for n, t in todos)
    h = f'''<!doctype html><html lang="es"><head><meta charset="utf-8"><title>Tintalú · meandros</title><style>
body{{margin:0;padding:24px 28px;background:#EEF1EE;font:14px system-ui,sans-serif;color:#13303A;width:1544px}}
h1{{margin:0 0 4px;font-size:24px}} h2{{margin:0 0 10px;font-size:18px}} p{{margin:0 0 18px}}
section{{background:#fff;border-radius:14px;padding:16px 18px;margin:0 0 18px}}
.fila{{display:flex;gap:22px;align-items:flex-end;flex-wrap:wrap;margin:0 0 12px}}
figure{{margin:0;text-align:center;font-size:12px}} img{{display:block}} figure img{{display:inline-block}}
.oscuro{{background:#121417;padding:10px;border-radius:10px}}
.tira{{display:flex;gap:30px;align-items:flex-end;background:#1B2A2E;color:#DDE7E4;border-radius:14px;padding:16px 18px;margin:0 0 18px}}
</style></head><body>
<h1>Tintalú · el río con meandros (fase 1: bocetos)</h1>
<p>La idea de D-045 sin cambios (cuia de vidrio, chorro de açaí, el río se tiñe desde el impacto río abajo); el río serpentea como en la pantalla de carga.</p>
<div class="tira">{junto}<span style="width:24px"></span>{pantalla}<span style="width:24px"></span>{junto_m}</div>
{"".join(filas)}
</body></html>'''
    html = os.path.join(tmp, "hoja-meandros.html")  # temporal: lleva las imágenes incrustadas (varios MB)
    with open(html, "w", encoding="utf-8") as f:
        f.write(h)
    return captura(html, os.path.join(HERE, "hoja-meandros.png"), 1600, 2260)


def main():
    if CHROME is None:
        print("FALLO: no encuentro Chrome ni Edge")
        return 2
    tmp = tempfile.mkdtemp(prefix="tintalu-meandros-")
    for n, _ in VARIANTES:
        if not exportar(n, tmp):
            return 1
    return 0 if hoja(tmp) else 1


if __name__ == "__main__":
    sys.exit(main())
