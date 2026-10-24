"""Hoja de revisión: out/hoja-revision.html con todas las piezas incrustadas (abre sin servidor)."""
import base64, os
HERE=os.path.dirname(os.path.abspath(__file__)); PNG=os.path.join(HERE,"out","png")
u=lambda f:"data:image/png;base64,"+base64.b64encode(open(os.path.join(PNG,f),"rb").read()).decode()
bg,fg,mono=u("android-fondo-432.png"),u("android-frente-432.png"),u("android-monocromo-432.png")
def mask(shape,size):
    r={"circulo":"50%","squircle":"30%","cuadrado":"14%"}[shape]
    # el lanzador muestra los 72 dp centrales de los 108: se recorta 1/6 por lado
    return (f'<div style="width:{size}px;height:{size}px;border-radius:{r};overflow:hidden;position:relative">'
            f'<img src="{bg}" style="position:absolute;width:{size*1.5}px;left:{-size*0.25}px;top:{-size*0.25}px">'
            f'<img src="{fg}" style="position:absolute;width:{size*1.5}px;left:{-size*0.25}px;top:{-size*0.25}px"></div>')
def tema(fondo,tinta,size):
    return (f'<div style="width:{size}px;height:{size}px;border-radius:50%;overflow:hidden;position:relative;background:{fondo}">'
            f'<div style="position:absolute;width:{size*1.5}px;height:{size*1.5}px;left:{-size*0.25}px;top:{-size*0.25}px;'
            f'background:{tinta};-webkit-mask:url({mono}) center/contain no-repeat;mask:url({mono}) center/contain no-repeat"></div></div>')
f=lambda c,t:f'<figure>{c}<figcaption>{t}</figcaption></figure>'
h=f'''<!doctype html><html lang="es"><head><meta charset="utf-8"><title>Tintalú · propuesta B</title><style>
body{{margin:0;padding:24px;background:#EEF1EE;font:14px system-ui,sans-serif;color:#13303A}}
.fila{{display:flex;gap:24px;align-items:flex-end;flex-wrap:wrap;margin:0 0 30px}}figure{{margin:0;text-align:center}}
.oscuro{{background:#121417;padding:20px;border-radius:12px;display:flex;gap:24px;align-items:flex-end}}
img{{display:block}}</style></head><body>
<h2>Tintalú · propuesta B «El río que se tiñe»</h2>
<div class="fila">{f(f'<img src="{u("app-store-1024.png")}" width="300" style="border-radius:22%">',"iOS / Play (máscara del sistema)")}
{f(mask("circulo",200),"Android círculo")}{f(mask("squircle",200),"Android squircle")}{f(mask("cuadrado",200),"Android cuadrado")}</div>
<h3>Tamaño real</h3><div class="fila">{"".join(f(f'<img src="{u(f"legacy-{n}.png")}" width="{n}" style="border-radius:22%">',str(n)) for n in (192,144,96,72,48))}
{f(f'<img src="{u("legacy-48.png")}" width="32" style="border-radius:22%">',"32")}</div>
<h3>Icono temático (Android 13+): la monocroma</h3><div class="fila">
{f(tema("#D9E7DF","#1F4D3B",120),"claro")}{f(tema("#2A3B33","#BFE3CF",120),"oscuro")}{f(tema("#E9DDF6","#4A2B6B",120),"otro tema")}{f(tema("#D9E7DF","#1F4D3B",48),"48")}</div>
<h3>Logo con el nombre</h3>
<div class="fila"><img src="{u("logo-horizontal-claro.png")}" width="560" style="background:#fff;border-radius:10px;padding:12px">
<img src="{u("logo-vertical-claro.png")}" width="260" style="background:#fff;border-radius:10px;padding:12px"></div>
<div class="fila oscuro"><img src="{u("logo-horizontal-oscuro.png")}" width="560"><img src="{u("logo-vertical-oscuro.png")}" width="260"></div>
</body></html>'''
open(os.path.join(HERE,"out","hoja-revision.html"),"w",encoding="utf-8").write(h); print("ok")
