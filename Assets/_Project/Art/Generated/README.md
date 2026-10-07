# Recursos generados (provisionales): ranuras de arte

Carpeta de los PNG pintados provisionales de la dirección visual v2 «Selva brillante premium» (D-003; `docs/art/direccion-visual-v2.md` 10.3). Los genera el dueño con ChatGPT a partir de `docs/art/prompts-chatgpt.md`. Son provisionales: el arte final es del artista (CLAUDE.md, «Humanos»). Estado cultural: pendiente de revisión (07 §8.2).

## Cómo funciona

- Cada elemento visual tiene una **ranura** con el nombre exacto del PNG (tabla de abajo). Si existe `Assets/_Project/Art/Generated/<ranura>.png`, el juego lo usa; si no, usa el arte procedural de `Assets/_Project/Scripts/Presentation/ProceduralArt*.cs`.
- Los ajustes de importación (Sprite, PPU, bordes 9-slice, sin mipmaps, ASTC en Android) los aplica `AF.EditorTools.GeneratedArt` (`Assets/_Project/Editor/CI/GeneratedArt.cs`) al importar. **No se editan los `.meta` a mano** (CLAUDE.md, regla 3).
- El mismo script mantiene el catálogo `Assets/_Project/Resources/AF/ArtSlotCatalog.asset`, que es lo que lee el juego en el build (`AF.Presentation.ArtSlots`). Se reconstruye al importar o borrar un PNG de esta carpeta y antes de cada build; también a mano con el menú **Amazon Fill › Arte › Actualizar catálogo de arte generado**. Sin PNG no hay catálogo y todo es procedural.
- Un PNG con un nombre que no es una ranura se ignora (aviso en la consola).
- Los PNG van por Git LFS (`.gitattributes`).

## Ranuras

PPU 300 = 3 px por dp; PPU 400 = 4 px por dp. Bordes 9-slice en px del PNG final (izquierda / derecha / arriba / abajo).

| Ranura (`<ranura>.png`) | Tamaño final (px) | PPU | 9-slice | Dónde se usa | Respaldo procedural | Estado |
|---|---|---|---|---|---|---|
| `bg_estuario_panorama` | 1024 × 1536 | — | — | Panorama, 360 × 540 dp anclado arriba (guía 3.9) | Cielo, sol, nubes, orilla con palmeras de açaí, río (mínimo, guía 4.9) | Conectada |
| `bg_estuario_muelle` | 1024 × 1024 | — | — | Muelle, 360 × 360 dp anclado abajo; viga en H − 288 | Tablas, viga y postes | Conectada |
| `foliage_tl` | 512 × 512 | — | — | Esquina superior izquierda, 160 dp (pivote en la esquina; se balancea ±1,5°) | Fronda de açaí, filodendros y hoja en flecha | Conectada |
| `foliage_tr` | 512 × 512 | — | — | Esquina superior derecha, 160 dp, con el racimo de açaí | Ídem, con racimo | Conectada |
| `foliage_bl` | 512 × 512 | — | — | Esquina inferior izquierda, 140 dp | Ídem | Conectada |
| `foliage_br` | 512 × 512 | — | — | Esquina inferior derecha, 140 dp (único acento de heliconia) | Ídem | Conectada |
| `ui_cinta_hoja` | 1344 × 256 | 400 | 320 / 320 / 0 / 0 | Cinta-hoja de victoria, 336 × 64 dp | Hoja verde con franja de texto | Conectada |
| `ui_btn_hoja` | 384 × 192 | 300 | 84 / 84 / 0 / 0 | CTA Siguiente (cara, canto y sombra en un sprite; al pulsar baja 3 dp) | Botón con relieve por capas (guía 5.1) | Conectada |
| `ui_btn_agua` | 384 × 192 | 300 | 84 / 84 / 0 / 0 | CTA «Deshacer · Marea» del modal | Ídem | Conectada |
| `ui_btn_madera` | 384 × 192 | 300 | 84 / 84 / 0 / 0 | Botones secundarios (ninguno en v0) | — | Reservada |
| `ui_slot_agua` | 192 × 216 | 300 | — | Ranura de Deshacer de la bandeja, 64 × 72 dp | Cuadrado redondeado con relieve | Conectada |
| `ui_disc_madera` | 192 × 192 | 400 | — | Disco de Reiniciar, 40 dp | Disco con relieve | Conectada |
| `ui_panel_tablero` | 512 × 512 | 400 | 96 / 96 / 96 / 96 | Marco del panel del tablero (solo marco; el relleno es siempre por código) | Marco de 6 dp tallado | Conectada |
| `ui_panel_modal` | 512 × 512 | 400 | 112 / 112 / 112 / 112 | Marco de los modales (solo marco; la superficie la pone el código) | Marco y superficie | Conectada |
| `ui_bandeja` | 640 × 320 | 400 | 128 / 128 / 0 / 0 | Bandeja cumbuca, 96-328 × 80 dp | Madera oscura con rebaje y cuerda | Conectada |
| `ui_placa_nivel` | 544 × 208 | 400 | — | Insignia de nivel, 136 × 52 dp | Placa con veta, bisel, ranura y hojas | Conectada |
| `wood_dark_tile` | 512 × 512 | 400 | — (tesela) | Madera de bandeja y placa | Veta por código | Reservada (sin conectar) |
| `wood_light_tile` | 512 × 512 | 400 | — (tesela) | Repisas | Veta por código | Reservada (sin conectar) |
| `capi_idle` | 512 × 512 | — | — | Capi en el muelle, 64 × 56 dp (sin parpadeo con PNG) | Capibara sencilla de perfil | Conectada |
| `fruta_acai`, `fruta_aguaje`, `fruta_tapereba`, `fruta_guanabana`, `fruta_bacaba` | 256 × 256 | — | — | Tarjeta «Fruta nueva» (fase de jugabilidad) | — | Reservadas |

Siempre por código, sin ranura (guía 10.3): relleno del panel y de los modales, vidrio, líquido, cavidad, repisas, sombras y partículas.

## Registro de PNG

| Archivo | Fecha | Prompt (sección) | Familia | Clave de estilo adjunta | Cambios al prompt | Posproceso | 9-slice medido (px) | Revisión cultural |
|---|---|---|---|---|---|---|---|---|
| — | — | — | — | — | — | — | — | — |
