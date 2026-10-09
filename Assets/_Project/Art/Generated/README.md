# Recursos generados (provisionales): ranuras de arte

Carpeta de los PNG pintados provisionales de la dirección visual v2 «Selva brillante premium» (D-003; `docs/art/direccion-visual-v2.md` 10.3). Los genera el dueño con ChatGPT a partir de `docs/art/prompts-chatgpt.md`. Son provisionales: el arte final es del artista (CLAUDE.md, «Humanos»). Estado cultural: pendiente de revisión (07 §8.2).

## Cómo funciona

- Cada elemento visual tiene una **ranura** con el nombre exacto del PNG (tabla de abajo). Si existe `Assets/_Project/Art/Generated/<ranura>.png`, el juego lo usa; si no, usa el arte procedural de `Assets/_Project/Scripts/Presentation/ProceduralArt*.cs`.
- Los ajustes de importación (Sprite, PPU, bordes 9-slice, sin mipmaps, ASTC en Android) los aplica `AF.EditorTools.GeneratedArt` (`Assets/_Project/Editor/CI/GeneratedArt.cs`) al importar. **No se editan los `.meta` a mano** (CLAUDE.md, regla 3).
- El mismo script mantiene el catálogo `Assets/_Project/Resources/AF/ArtSlotCatalog.asset`, que es lo que lee el juego en el build (`AF.Presentation.ArtSlots`). Se reconstruye al importar o borrar un PNG de esta carpeta y antes de cada build; también a mano con el menú **Amazon Fill › Arte › Actualizar catálogo de arte generado**. Sin PNG no hay catálogo y todo es procedural.
- Un PNG con un nombre que no es una ranura se ignora (aviso en la consola). Las ranuras de fauna valen para cualquier especie del catálogo (`fauna_<id>_partes`, `fauna_<id>_retrato`); las de rama, para cualquier mundo (`rama_<mundo>`).
- Con algún `fruta_*.png`, el mismo script genera el **atlas de vuelo de 64 px** `Assets/_Project/Resources/AF/fruit_flight_atlas.png` (512 × 128, 8 × 2 celdas en el orden de los índices de motor 1-14): cada PNG reducido con un filtro de caja y el respaldo procedural en las frutas que falten. Lo lee `ProceduralArt.FruitFlight`; no se edita a mano.
- Las especies sin su PNG usan en desarrollo el miquito provisional por código; en un build de release, la intro de ese tramo va en **modo dosel** (sin tropa), nunca con la especie de otro mundo (guion §4.3).
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
| `fruta_<tinte>` × 14: `fruta_guanabana`, `fruta_cupuacu`, `fruta_tapereba`, `fruta_aguaje`, `fruta_urucum`, `fruta_camu_camu`, `fruta_bacaba`, `fruta_acai`, `fruta_jenipapo`, `fruta_agua_tapajos`, `fruta_lulo`, `fruta_araza`, `fruta_cacao`, `fruta_agua_negro` | 256 × 256 | — | — | La fruta que cae en «Llenar y teñir» (guion de la intro §5 y §12) y la tarjeta «Fruta nueva»; con la forma de la familia del icono de 07 §3. Nunca la semilla ámbar de la moneda (taperebá con rabito y hoja, cacao como trozo de mazorca, urucum con semillitas rojas y angulosas, Río Negro como hoja parda rizada, Tapajós como gotas) | Las 14 frutas de 64 px de `ProceduralArt.Fruit.cs` | Conectadas (intro) |
| `fauna_<id>_partes` (un PNG por especie de `Assets/_Project/Data/Fauna/miquitos_v0.json`) | 1024 × 1024 | — | — | Piezas del rig recortable del miquito (guion §11-§12, Q16 A): rejilla de 4 × 4 celdas de 256 px; en la fila de arriba, de izquierda a derecha, **cabeza** (mirando a la derecha, centrada), **torso** (elipse que llena la celda), **extremidad** (cápsula vertical en el cuarto izquierdo de su celda, mano o pie abajo; se estira en 9-slice con 20 % arriba y abajo) y **párpado** (arriba a la izquierda de su celda, 40 % × 16 %). La cola la dibuja el código con la paleta del catálogo. Contorno de 2 px de tinta al 80 %, sin ropa ni accesorios, postura animal | Rig procedural por esqueleto A-D (`ProceduralArt.Fauna.cs`, `MonkeyRig.cs`) | Conectada |
| `fauna_<id>_retrato` | 512 × 512 | — | — | Retrato de 3/4 para la tarjeta «¡Miquito a la vista!» (40 dp) y el Cuaderno | La cabeza procedural | Conectada |
| `rama_<mundo>` (`rama_estuario`…) | 1024 × 256 | — | — | Rama de la tropa (árbol de várzea o igapó; nunca sumaúma ni lupuna) | Bézier con corteza y 4 hojas (`TroopLayer`) | Reservada |
| `liana_tile` | 64 × 256 (tesela) | — | — | Liana del modelo `cuelga` | Bézier verde de 2 dp | Reservada |

Siempre por código, sin ranura (guía 10.3): relleno del panel y de los modales, vidrio, líquido, cavidad, repisas, sombras y partículas.

## Registro de PNG

| Archivo | Fecha | Prompt (sección) | Familia | Clave de estilo adjunta | Cambios al prompt | Posproceso | 9-slice medido (px) | Revisión cultural |
|---|---|---|---|---|---|---|---|---|
| — | — | — | — | — | — | — | — | — |
