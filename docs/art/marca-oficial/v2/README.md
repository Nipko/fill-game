# Tintalú · identidad oficial v2: «El río que se tiñe»

**Oficial por decisión del dueño (D-045, 2026-10-23); sustituye a la v1 de Codex** (`docs/art/marca-oficial/v1/`, historial). La pidió el dueño: el Amazonas en su color de verdad y una cuia que vierte açaí y tiñe el río **desde el impacto, río abajo**. Es vectorial y se genera por código (Claude, propuesta B); el acabado final puede encargarse al artista sobre esta composición.

## Qué es fiel al juego
- **La cuia:** silueta generada con las cifras de `Assets/_Project/Scripts/Presentation/CuiaGeometry.cs`, en capacidad 3: fondo plano con pie de fibra recto, base de vidrio grueso, hombro, cuello corto y borde de fibra; las fronteras entre dosis son el arco del tablero.
- **El vertido (D-006):** la inclinación sale del volumen. Quedan 2,6 de 3 dosis, así que vierte casi horizontal, como en el juego (92° − 9° por dosis que queda). El líquido queda horizontal por gravedad, la capa de açaí es la que se va y el chorro cae en parábola, cada vez más fino.
- **Colores** (glosario §9): agua blanca del Amazonas #B08A5A; Taperebá #F2B705 con puntos, Aguaje #EF7D1A con escamas y Açaí #6A2C91 con rombos.
- **Tipografía:** Baloo 2 ExtraBold, la del juego (OFL), convertida en trazados.

## Archivos (`out/`, generados: no se editan a mano)
| Uso | Archivo |
|---|---|
| Icono maestro (iOS, Play) | `icono-1024.svg`, `png/app-store-1024.png`, `png/google-play-512.png` |
| Android adaptativo, 108 dp a 4× | `png/android-fondo-432.png` y `png/android-frente-432.png` (el primer plano llega a 32,1 dp del centro; zona segura de 33 dp) |
| Android temático (13+) | `png/android-monocromo-432.png`: una sola tinta; el río teñido va lleno y el de agua blanca, solo con sus orillas |
| Android clásico | `png/legacy-48 … 192.png` |
| Logo con el nombre | `logo-horizontal-{claro,oscuro}` y `logo-vertical-{claro,oscuro}` (SVG y PNG con transparencia); «claro» = letras oscuras para fondo claro |
| Revisión | `hoja-revision.png`: máscaras de Android, tamaños reales, icono temático y logos |

## Cómo regenerar
```
python generar.py     # SVG; solo biblioteca estándar (lee la fuente TTF del juego)
python exportar.py    # PNG con Chrome o Edge sin interfaz
python hoja.py        # out/hoja-revision.html (la captura out/hoja-revision.png se hace con Chrome sin interfaz)
```
`bocetos/a-meandro.svg` es la versión A descartada, guardada como referencia.

## Pendiente
- Integrarla en Unity (Player Settings: icono, adaptativo y monocromo) y en la web (favicon, apple-touch-icon y logo); la web la lleva Codex (`docs/dev/traspaso/codex-web-marca.md`).
- No hay versión oscura ni tintada de iOS ni catálogo de Xcode; se hacen cuando se elija.
- Probar en un teléfono el icono adaptativo y el temático.
