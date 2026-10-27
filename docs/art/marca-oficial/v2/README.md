# Tintalú · identidad oficial v2: «El río que se tiñe»

**Oficial por decisión del dueño (D-045, 2026-10-23); sustituye a la v1 de Codex** (`docs/art/marca-oficial/v1/`, historial). La pidió el dueño: el Amazonas en su color de verdad y una cuia que vierte açaí y tiñe el río **desde el impacto, río abajo**. Es vectorial y se genera por código (Claude, propuesta B); el acabado final puede encargarse al artista sobre esta composición.

## Qué es fiel al juego
- **La cuia:** silueta generada con las cifras de `Assets/_Project/Scripts/Presentation/CuiaGeometry.cs`, en capacidad 3: fondo plano con pie de fibra recto, base de vidrio grueso, hombro, cuello corto y borde de fibra; las fronteras entre dosis son el arco del tablero.
- **El vertido (D-006):** la inclinación sale del volumen. Quedan 2,6 de 3 dosis, así que vierte casi horizontal, como en el juego (92° − 9° por dosis que queda). El líquido queda horizontal por gravedad, la capa de açaí es la que se va y el chorro cae en parábola, cada vez más fino.
- **Colores** (glosario §9): agua blanca del Amazonas #B08A5A; Taperebá #F2B705 con puntos, Aguaje #EF7D1A con escamas y Açaí #6A2C91 con rombos.
- **Tipografía:** Baloo 2 ExtraBold, la del juego (OFL), convertida en trazados.
- **El río con meandros** (añadido de D-045 del 2026-10-27, variante B «dos meandros con perspectiva»): como en la pantalla de carga (`BrandRiverScene`), el río nace fino en el horizonte, hace dos curvas lejanas de agua blanca y, desde el impacto, una curva ancha de açaí hacia el primer plano, con playas en el interior de las curvas y orillas de selva en copas. Nunca es más ancho que el 70 % del radio de su curva. La cuia está 60 px a la derecha y 40 más arriba que en el río recto: su punto más lejano queda a 27,8 dp del centro del icono adaptativo (zona segura: 33; `generar.py` lo imprime). En la monocroma, aguas arriba solo se dibujan las orillas del tramo cercano (media anchura ≥ 30): las curvas lejanas, finas, harían ruido a 48 px.

## Archivos (`out/`, generados: no se editan a mano)
| Uso | Archivo |
|---|---|
| Icono maestro (iOS, Play) | `icono-1024.svg`, `png/app-store-1024.png`, `png/google-play-512.png` |
| Favicon y web de iOS | `png/favicon-16.png`, `png/favicon-32.png`, `png/apple-touch-icon.png` (180 × 180) |
| Gráfico de funciones provisional | `feature-graphic.svg`, `png/feature-graphic.png` (1024 × 500); logotipo horizontal y río teñido, generado con las piezas de esta identidad |
| Android adaptativo, 108 dp a 4× | `png/android-fondo-432.png` y `png/android-frente-432.png` (el primer plano llega a 32,1 dp del centro; zona segura de 33 dp) |
| Android temático (13+) | `png/android-monocromo-432.png`: una sola tinta; el río teñido va lleno y el de agua blanca, solo con sus orillas |
| Android clásico | `png/legacy-48 … 192.png` |
| Logo con el nombre | `logo-horizontal-{claro,oscuro}` y `logo-vertical-{claro,oscuro}` (SVG y PNG con transparencia); «claro» = letras oscuras para fondo claro |
| Revisión | `hoja-revision.png`: máscaras de Android, tamaños reales, icono temático y logos |

## Cómo regenerar
```
python generar.py     # SVG; solo biblioteca estándar (lee la fuente TTF del juego)
python exportar.py    # PNG con Chrome o Edge sin interfaz
python exportar.py --unity-wordmark <BrandWordmark.Generated.cs> [--check]   # el nombre para el juego, sin Chrome
python exportar.py --unity-emblem <BrandEmblem.Generated.cs> [--check]       # el emblema para el juego, sin Chrome
python hoja.py        # out/hoja-revision.html (la captura out/hoja-revision.png se hace con Chrome sin interfaz)
```
`emblema_unity.py` lo usa `exportar.py --unity-emblem`. `bocetos/a-meandro.svg` es la versión A descartada, guardada como referencia.

**Bocetos de meandros (2026-10-27):** `python generar.py --variante {recto,s-amplia,dos-meandros,aereo} --out <carpeta>` escribe un boceto (sin argumentos, el oficial, que es `dos-meandros`; `recto` es el v2 del 2026-10-23 al 2026-10-27). `bocetos/meandros/hoja-meandros.png` es la hoja con la que el dueño eligió la B (fase 1, commit 78e236bf); `python bocetos/meandros/construir.py` la rehace, pero su fila «actual» lee `out/`, que ya es la B. Las carpetas de los bocetos guardan la B tal como se propuso (después se acercó la cuia al centro, se ensancharon las curvas lejanas y se simplificó la monocroma).

## Pendiente
- **Unity: integrada** (2026-10-23) en `Assets/_Project/Plugins/Android/AfIcon.androidlib`, porque Unity 6000.3 no tiene capa monocroma. Lleva `mipmap-anydpi-v26` y `-v33` (este, con la monocroma) y las capas por densidad, que genera `python exportar.py --android-res <AfIcon.androidlib/src/main/res>`. `Assets/Plugins/Android/LauncherManifest.xml` apunta `android:icon` y `roundIcon` a `@mipmap/af_launcher`.
- **Intro y pantalla de arranque del juego: integradas** (2026-10-25, añadido de D-051), sin PNG. `python exportar.py --unity-emblem ../../../../Assets/_Project/Scripts/Presentation/BrandEmblem.Generated.cs` convierte la cuia, el chorro, los aros y las palmeras del icono (frente y fondo de `generar.py`) en capas de polígonos con sus colores, degradados, tramas y trazos (`emblema_unity.py`). `python exportar.py --unity-wordmark ../../../../Assets/_Project/Scripts/Presentation/BrandWordmark.Generated.cs` escribe los contornos de «Tintalú». Los dos van sin Chrome y, con `--check`, solo comparan. El juego compone a su alrededor el río con meandros para la pantalla vertical (`BrandRiverScene`) y lo dibuja todo nítido (`BrandEmblem`, `BrandWordmark`). Si cambia `generar.py`, hay que regenerarlos.
- **El río con meandros (fase 2, 2026-10-27):** regenerados `out/`, las capas de `AfIcon.androidlib`, `BrandEmblem.Generated.cs` (la cuia y el impacto se mueven; `BrandRiverScene` coloca la cuia por el impacto, así que la pantalla de arranque no cambia de composición) y la web (`npm run web:build` en `infra/`). `emblema_unity.py` ignora el `<defs>` de los meandros y comprueba que del fondo salgan las dos palmeras (16 trazos). La captura `out/hoja-revision.png` es de 1200 × 1520, con los logos.
- La web usa los favicons, el apple-touch-icon y los logos claro/oscuro derivados de estos archivos. El gráfico de funciones es provisional; no es una aprobación del nombre para publicación.
- No hay versión oscura ni tintada de iOS ni catálogo de Xcode; se hacen cuando se elija.
- Probar en un teléfono el icono adaptativo y el temático.
