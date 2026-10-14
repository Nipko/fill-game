# Prompts para ChatGPT: recursos pintados provisionales

Fecha: 2026-10-07 · Autor: dirección de arte (agente) · Guía de referencia: `docs/art/direccion-visual-v2.md` («Selva brillante premium», D-003) · Estado: lista de encargo para el dueño. Los agentes no generan imágenes ni navegan: el dueño pega cada prompt en ChatGPT (generación de imágenes) y deja el PNG en la carpeta indicada.

Revisión 2 (2026-10-07), tras la revisión del crítico de arte (guía, sección 13):
- las piezas orgánicas (panorama, muelle, follaje y cinta-hoja) pasan a ser la base y se piden **ya**, en paralelo con F1;
- todo se genera a partir de una **clave de estilo** y por familias;
- los botones se piden a 2:1;
- los marcos de panel y modal se piden **solo como marco**, con el interior transparente y sin sombra;
- cambian el color del marco del tablero, el tamaño de la placa y de la bandeja, la cinta-hoja, el panorama y la esquina superior derecha.

Revisión 3 (2026-10-07), tras la revisión visual de las capturas del respaldo procedural: el río se leía como arena o playa y la escena era casi monocroma marrón. Cambian el panorama (3.1: reflejos oscuros de la selva en el agua, columna de destellos bajo el sol y el reflejo dorado solo junto al horizonte; nada de arena ni de playa) y el muelle (3.2: más estrecho que la pantalla, con río a los lados, madera más clara, haces de sol y sin postes que asomen sobre la viga). También el cielo de arriba (#5BC0EB) y la madera del muelle (#C08A58, #A8774C) de la paleta de referencia.

Revisión 4 (2026-10-14, tanda 4; D-013): entran los provisionales de los fondos por parada, en la familia S8 (3.17 y 3.18): los tres primeros héroes (Ver-o-Peso, el Teatro Amazonas y el malecón de Leticia) y el kit del Estuario.
- Son opcionales y no piden personas, canastos, banderas ni cuias.
- **Nunca van en la ficha, las capturas ni la UA** (propuesta del Mapa del río §10.2; 07 §2.1).
- El arte final de esas piezas, del Mapa, de TiTino, de Frutas del río y de la Tinaja es del artista: `docs/art/encargo-mapa-del-rio.md` y `docs/art/encargo-titino-frutas-tinaja.md`.

Qué son estos recursos:
- **Provisionales.** El arte final sigue siendo del artista (CLAUDE.md, «Humanos»). Las piezas orgánicas son la base de la fase visual; las de UI y material son opcionales, porque el código ya las hace bien (guía, 10.3).
- **Pendientes de revisión cultural.** El paisaje, Capi y las frutas quedan «pendiente de revisión» hasta el visto bueno del asesor cultural (07 §8.2).
- **Sin marcas ajenas.** Ningún prompt nombra juegos, marcas ni estilos de otros estudios. Así se evita pedir el trade dress que 07 §1.3 prohíbe.

## 1. Cómo usarlos

1. **Primero, la clave de estilo.** Genera `bg_estuario_panorama.png` (3.1) en una conversación propia y repítelo hasta que te guste. Esa imagen aprobada es la **clave de estilo**: fija el acabado, la luz y la saturación de todo lo demás. El acabado premium depende sobre todo de esa coherencia (07 §1.1).
2. **Después, una familia por conversación**, siempre con la clave adjunta:
   - adjunta el panorama aprobado al primer mensaje;
   - pega el prompt tal cual, en inglés, y añade al final esta línea:

     ```text
     Match the rendering style, lighting and saturation of the attached image.
     ```
   - genera en esa misma conversación todos los recursos de la familia (columna «Familia» de la tabla 2), uno detrás de otro;
   - si sale texto, un marco o algo de la lista de prohibidos, pide «same image, but remove …» en lugar de reescribir el prompt.
3. **Orden:** prioridad 1 ya (en paralelo con F1 de la guía); prioridad 2 a continuación; los opcionales, solo si el dueño quiere compararlos con lo procedural (F7); Capi y las frutas, en la fase de jugabilidad; los fondos por parada (S8), solo si el dueño quiere ver un sitio pintado antes de que llegue el arte del artista.
4. **Tamaño de generación.** ChatGPT genera a 1024 × 1024, 1024 × 1536 o 1536 × 1024 px. Cada ficha indica cuál pedir y a qué tamaño final hay que llevar el PNG.
5. **Transparencia.** Cuando la ficha pide alfa, el prompt ya pide «transparent background».
   - Si llega un fondo opaco, pide «make the background fully transparent».
   - Si aun así no sale, pide un fondo plano y recórtalo con «Color a alfa» de GIMP o Krita y luego quita el halo: magenta #FF00FF para el follaje y verde #00FF00 para lo demás.
6. **Posproceso** (GIMP, Krita o Photoshop):
   - recortar al contenido más el margen indicado **con la proporción del tamaño final**: si el recorte no la tiene, amplía el lienzo con transparencia hasta tenerla;
   - redimensionar con Lanczos al **tamaño final exacto**, siempre de forma **uniforme**. Nunca estirar en una sola dirección: las esquinas redondeadas se volverían elípticas y el 9-slice se rompería;
   - en las teselas, Filtros › Mapa › «Teselado perfecto», o el modo envolvente de Krita, y comprobar que los cuatro bordes casan;
   - exportar PNG de 8 bits por canal, sRGB, sin metadatos y de ≤ 2 MB.
7. **Comprobación** (sección 4) antes de guardar, incluida la hoja de contacto de la familia.
8. **Guardar** con el **nombre exacto** en `Assets/_Project/Art/Generated/`.
   - Los PNG van por Git LFS (`.gitattributes`: `*.png filter=lfs`).
   - Unity genera el `.meta` al importar. Los ajustes (Sprite, bordes 9-slice, ASTC, sin mipmaps) los aplica un script de Editor versionado, nunca editando el `.meta` a mano (CLAUDE.md, regla 3).
9. **Anotar cada PNG** en `Assets/_Project/Art/Generated/README.md` (plantilla en la sección 5). Si el prompt cambió, guardar también el texto final.

Unidades para integrar: el `CanvasScaler` usa `referencePixelsPerUnit` = 100 (`GameScreen`). Con el Pixels Per Unit (PPU) del sprite a 300, 3 px del PNG equivalen a 1 dp; a 400, 4 px equivalen a 1 dp. Así un borde 9-slice de 72 px con PPU 300 mide 24 dp en pantalla.

## 2. Resumen de recursos

Familias (una conversación cada una, con la clave adjunta salvo S0): **S0** clave de estilo · **S1** muelle · **S2** hojas (follaje y cinta-hoja) · **S3** botones · **S4** marcos y madera de UI · **S5** teselas de madera · **S6** Capi · **S7** frutas · **S8** fondos por parada (héroes y kit provisionales).

| # | Archivo (`Assets/_Project/Art/Generated/`) | Familia | Generar | Final (px) | Alfa | 9-slice (px del PNG final: izq./der./arriba/abajo) | PPU | En pantalla | Papel (guía 10.3) | Prioridad |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `bg_estuario_panorama.png` | S0 | 1024 × 1536 | 1024 × 1536 | No | — | — | 360 × 540 dp, anclado arriba | **Base** de la escena; también es la clave de estilo | **1** |
| 2 | `bg_estuario_muelle.png` | S1 | 1024 × 1024 | 1024 × 1024 | Sí (20 % superior vacío) | — | — | 360 × 360 dp, con la viga justo bajo el panel (se estira en vertical hasta un 15 %) | Base | **1** |
| 3 | `foliage_tl.png` | S2 | 1024 × 1024 | 512 × 512 | Sí | — | — | 160 × 160 dp, esquina superior izquierda | Base (guía 3.8) | **1** |
| 4 | `foliage_tr.png` | S2 | 1024 × 1024 | 512 × 512 | Sí | — | — | 160 × 160 dp, esquina superior derecha | Base | **1** |
| 5 | `foliage_bl.png` | S2 | 1024 × 1024 | 512 × 512 | Sí | — | — | 140 × 140 dp, esquina inferior izquierda | Base | **1** |
| 6 | `foliage_br.png` | S2 | 1024 × 1024 | 512 × 512 | Sí | — | — | 140 × 140 dp, esquina inferior derecha | Base | **1** |
| 7 | `ui_cinta_hoja.png` | S2 | 1536 × 1024 | 1344 × 256 | Sí | 320 / 320 / 0 / 0 | 400 | Cinta de 336 × 64 dp, a su alto nativo | Base (guía 5.5) | 2 |
| 8 | `ui_btn_hoja.png` | S3 | 1536 × 1024 | 384 × 192 (2:1) | Sí | 84 / 84 / 0 / 0 | 300 | CTA de 312 × 64 dp (Siguiente), a su alto nativo | Opcional (guía 5.1) | Opcional |
| 9 | `ui_btn_agua.png` | S3 | 1536 × 1024 | 384 × 192 (2:1) | Sí | 84 / 84 / 0 / 0 | 300 | CTA «Deshacer · Marea» del modal | Opcional | Opcional |
| 10 | `ui_btn_madera.png` | S3 | 1536 × 1024 | 384 × 192 (2:1) | Sí | 84 / 84 / 0 / 0 | 300 | Botones secundarios | Opcional | Opcional |
| 11 | `ui_slot_agua.png` | S3 | 1024 × 1024 | 192 × 216 | Sí | — (tamaño nativo) | 300 | Ranura de Deshacer de 64 × 72 dp | Opcional (guía 3.6) | Opcional |
| 12 | `ui_disc_madera.png` | S3 | 1024 × 1024 | 192 × 192 | Sí | — | 400 | 48 × 48 dp (disco de 40 + sombra) | Opcional (guía 3.5) | Opcional |
| 13 | `ui_panel_tablero.png` | S4 | 1024 × 1024 | 512 × 512 | Sí: **solo el marco**, interior transparente | 96 / 96 / 96 / 96 | 400 | Marco del panel; el relleno y la sombra los pone el código | Opcional (guía 4.6) | Opcional |
| 14 | `ui_panel_modal.png` | S4 | 1024 × 1024 | 512 × 512 | Sí: **solo el marco**, interior transparente | 112 / 112 / 112 / 112 | 400 | Marco de los paneles de victoria y de bloqueo | Opcional (guía 5.3) | Opcional |
| 15 | `ui_bandeja.png` | S4 | 1536 × 1024 | 640 × 320 (2:1) | Sí | 128 / 128 / 0 / 0 | 400 | Bandeja de 96-328 × 80 dp, a su alto nativo | Opcional (guía 3.6) | Opcional |
| 16 | `ui_placa_nivel.png` | S4 | 1536 × 1024 | 544 × 208 | Sí | — (tamaño fijo) | 400 | Insignia de 136 × 52 dp | Opcional (guía 3.5) | Opcional |
| 17 | `wood_dark_tile.png` | S5 | 1024 × 1024 | 512 × 512, tesela perfecta | No | — (tesela) | 400 | Tesela de 128 dp | Opcional: madera de bandeja y placa (guía 4.4) | Opcional |
| 18 | `wood_light_tile.png` | S5 | 1024 × 1024 | 512 × 512, tesela perfecta | No | — (tesela) | 400 | Tesela de 128 dp | Opcional: repisas (guía 4.5) | Opcional |
| 19 | `capi_idle.png` | S6 | 1024 × 1024 | 512 × 512 | Sí | — | — | 96-140 dp en banners (ya no va a 64 × 56 dp en el muelle: D-016) | Capi (guía 3.7, retirada por D-016) | Jugabilidad |
| 20 | `fruta_acai.png` | S7 | 1024 × 1024 | 256 × 256 | Sí | — | — | 48-96 dp | Tarjeta «Fruta nueva» | Jugabilidad |
| 21 | `fruta_aguaje.png` | S7 | 1024 × 1024 | 256 × 256 | Sí | — | — | Ídem | Ídem | Jugabilidad |
| 22 | `fruta_tapereba.png` | S7 | 1024 × 1024 | 256 × 256 | Sí | — | — | Ídem | Ídem | Jugabilidad |
| 23 | `fruta_guanabana.png` | S7 | 1024 × 1024 | 256 × 256 | Sí | — | — | Ídem | Ídem | Jugabilidad |
| 24 | `fruta_bacaba.png` | S7 | 1024 × 1024 | 256 × 256 | Sí | — | — | Ídem | Ídem | Jugabilidad |
| 25 | `bg_hero_ver_o_peso.png` | S8 | 1536 × 1024 | 1440 × 480 (banda 3:1) | Sí: el cielo y el agua también | — | — | Héroe de 240 × 80 dp con el pie en el horizonte (Belém, niveles 1-10) | Provisional del fondo por parada (guía 10.3) | Opcional (nunca en la ficha, las capturas ni la UA) |
| 26 | `bg_hero_teatro_amazonas.png` | S8 | 1536 × 1024 | 1440 × 480 (banda 3:1) | Sí: el cielo y el agua también | — | — | Ídem (Manaos, niveles 51-60) | Ídem | Ídem |
| 27 | `bg_hero_leticia.png` | S8 | 1536 × 1024 | 1440 × 480 (banda 3:1) | Sí: el cielo y el agua también | — | — | Ídem (Leticia, niveles 81-100) | Ídem | Ídem |
| 28 | `bg_kit_w01.png` | S8 | 2 × 1536 × 1024 y 6 × 1024 × 1024 | Atlas de 2048 × 2048 con 8 piezas (3.18) | Sí | — | — | Piezas del fondo de la parada del Estuario | Ídem | Ídem |

Qué no se genera:
- **Relleno del panel del tablero e interiores de los modales:** los dibuja el código (degradado por vértices o textura RGBA32 pequeña). Un interior oscuro generado por IA y comprimido en ASTC 4 × 4 trae ruido y bandas, y no pasaría VA-04 (σ L* ≤ 3).
- **Sombras proyectadas de paneles y marcos:** por código.
- **Iconos de accesibilidad dentro de las dosis** (0,45 u, 8-11 dp): son glifos sencillos de 07 §3 y se dibujan por código.
- **Estado deshabilitado:** es plano y se hace por código (guía 5.1).
- **Vidrio, líquido, repisas y partículas** (gotas, plumas, hojas, destellos): procedurales.
- **Fondos de otros mundos:** con D-013 son fondos por parada (cielo por código, kit del mundo y héroe del sitio). Aquí solo van los provisionales de 3.17 y 3.18; los demás kits y héroes los pinta el artista (`docs/art/encargo-mapa-del-rio.md`).
- **TiTino, la ficha de Frutas del río y la Tinaja:** solo los pinta el artista (`docs/art/encargo-titino-frutas-tinaja.md`). TiTino se encarga después del visto bueno del asesor; la ficha y la Tinaja pasan por el asesor antes de integrarse.

Cómo medir el 9-slice:
- Los números son píxeles del **PNG final**.
- Las esquinas (radio, contorno, sombra lateral, hojas de los extremos) deben quedar enteras dentro de los bordes. Si al medir el PNG real la esquina ocupa más, anota el borde real en el README (sección 5), con 6 px de holgura.
- El centro y los lados tienen que ser uniformes en la dirección en que se estiran: degradado vertical sí, motivos sueltos no.
- Los bordes con 0 arriba y abajo indican que el sprite se muestra a su alto nativo y solo se estira en horizontal.

## 3. Prompts

Paleta de referencia de la guía: cielo #5BC0EB → #FFC994 → #FFE4A8; río #B08A5A → #8A6C48 → #6B5640 con reflejos verdes de la selva (#1C5236 → #2C7A4B); muelle #C08A58 / #A8774C; hojas #145A35 / #23824A / #5DBB3F / #C6EF6B; marco del tablero en madera media rojiza #7C4E3A con bisel #D29B6E; madera oscura #36261C (bandeja, placa y modales); repisa #C4935E / #866650; familias de UI Hoja #86D25A → #2F7D32 → #236A29, Agua #8ADBE6 → #13788F → #0F6275 y Madera #BF8F62 → #7E5638 → #46302A. La luz viene siempre de arriba a la izquierda.

### 3.1 `bg_estuario_panorama.png`: fondo panorámico del río (clave de estilo)

Generar a 1024 × 1536 y conservar ese tamaño. Opaco. Se muestra a 360 dp de ancho. En el peor caso (nivel 7 en un teléfono de 780 dp de alto) solo se ve la franja entre el 16 % y el 30 % de la altura, y las esquinas superiores quedan tapadas por el follaje. Por eso el sol, las palmeras de açaí y el río pardo se concentran en esa franja y en el centro.

```text
Vertical 2:3 background painting for a mobile puzzle game, 1024x1536 pixels, no user interface.
Place: the wide estuary of the Amazon river near Belém, Pará, Brazil, at sunrise, seen from a low point just above the water.
Composition, from top to bottom (follow the percentages, the game UI will cover parts of the image):
- 0-15%: open sky, calm and simple, soft gradient from clear turquoise blue (#5BC0EB) at the very top to warm peach (#FFC994); two or three soft white clouds (#FFF6EA) with rounded bottoms and a warm peach shade (#F3AE8A) only in their lower third; no detail behind where a title bar will sit.
- Horizon at about 22% from the top.
- The rising sun is a warm glowing disc (#FFF6D6) centered at about 30% of the width and 19% of the height, its diameter about 15% of the image width, sitting on the horizon and half veiled by low golden mist (#FFE4A8).
- 13-22%: the far riverbank, a thin line of rainforest in blue-green haze (#7FB0A2, #4E8C78), with two clear groups of tall slender açaí palms (clumping palms with very thin trunks and drooping feathery fronds): one group between 35% and 50% of the width and one between 61% and 78% of the width; their crowns sit between 13% and 22% of the height, just above the horizon. Keep the far left 20% and far right 20% of this band simpler.
- 22-30%: clearly visible milky coffee-brown river water (#B08A5A, darkening to #8A6C48 toward the viewer). The golden sky (#F6CF92) reflects only in a thin line right at the horizon and in a narrow column under the sun, made of 20-30 short horizontal sparkles. Dark green mirrored reflections of the rainforest banks (#1C5236, #2C7A4B) run along both sides of the water and under the far treeline, broken by thin horizontal ripple gaps, plus soft wavy vertical reflections of the açaí palms. Ripples are pairs of a thin light line over a thin dark line. The water must never look like sand, a beach or a desert.
- 30-50%: a very wide, flat, calm brown river continuing toward the viewer, darker (#6B5640), with the green forest reflections along both edges.
- 50-100%: simple open river water, slightly darker (#80603E), gentle ripples, very little detail (this part will be hidden behind the game board).
At most two tiny white egrets flying far away on the right side of the sky.
Style: premium casual mobile game background, polished hand-painted 2.5D look, luminous and saturated but harmonious, warm golden sunrise light from the upper left, soft atmospheric perspective, clean readable shapes, inviting and joyful, detail concentrated between 13% and 30% of the height.
Do not include: any text, letters, numbers, logos, watermark or signature; people; boats or canoes; houses, villages, docks or buildings; mountains or hills; beach, sand, sandbanks or anything that looks like sand; coconut palms; blue ocean or blue water; castles, temples, ruins or any fantasy element; gold, gems or crowns; paper, parchment or watercolor-paper texture; frames, borders or vignettes.
```

Comprobar:
- las palmeras son de açaí (finas y en grupo), no de coco, y caen en los dos grupos centrales;
- no hay montañas ni casas;
- el sol está a la izquierda, sobre el horizonte;
- el agua es café con leche, no azul, y se ve entre el 22 y el 30 % de la altura, con reflejos oscuros de la selva en el agua y una columna de destellos bajo el sol; nada de arena ni de playa (con el dorado en toda la franja, el río se lee como arena);
- tapando con un rectángulo todo lo que queda fuera del 16-30 % de la altura, la franja que queda sigue diciendo «río amazónico al amanecer».

### 3.2 `bg_estuario_muelle.png`: muelle en primer plano

Generar a 1024 × 1024 y conservar ese tamaño. El 20 % superior queda transparente. El borde superior de la viga frontal queda en el 20 % de la imagen y el juego lo coloca justo bajo el panel del tablero (el panel se apoya en el muelle): según el nivel, la imagen se estira en vertical hasta un 15 %, así que las tablas son horizontales y sin formas redondas que se deformen. El muelle es más estrecho que la pantalla y deja ver el río a los lados (revisión visual del 2026-10-07: con tablas de borde a borde, la pantalla era marrón sobre marrón).

```text
Foreground element for a vertical mobile game, 1024x1024 pixels, PNG with a fully transparent background.
A sturdy wooden riverside dock deck seen from slightly above, filling the lower 80% of the image: the deck is narrower than the image (about 84% of the width, centered), with calm milky coffee-brown river water (#B08A5A, #8A6C48) visible on both sides of it, showing dark green reflections of the rainforest (#1C5236, #2C7A4B) and a few thin light ripples. Wide horizontal planks of light warm wood (#C08A58, #A8774C, shadow tone #5A3D28) with visible but calm grain, thin dark gaps between planks (#2A1A10), planks slightly wider toward the bottom for gentle perspective. Along the top edge of the deck, starting exactly at 20% from the top of the image, runs a thick front beam; nothing rises above the beam (no posts or poles). Two or three soft diagonal shafts of warm sunlight (#FFE4A8, very subtle) fall across the planks from the upper left. A neat coil of natural fiber rope (#C49A5C) rests on the deck near the right edge.
Everything above the deck and the water (top 20% of the image) is fully transparent.
Light: warm golden sunrise light from the upper left, soft contact shadows, gentle varnish sheen.
Style: polished hand-painted 2.5D casual mobile game art, clean readable shapes, tactile and inviting.
Do not include: text, letters, logos or watermark; people or animals; boats or canoes; baskets, fruits, tools, buckets or crates; posts or poles above the beam; gold or gems; paper texture; sand or beach; sky or any background scenery other than the river water at the sides.
```

### 3.3 Follaje de esquina: `foliage_tl.png`, `foliage_tr.png`, `foliage_bl.png` y `foliage_br.png`

Familia S2: las cuatro en la misma conversación, con la clave adjunta. Generar a 1024 × 1024 y llevar a 512 × 512. Transparente. Las hojas nacen en la esquina indicada y dejan vacía la parte opuesta. Hay que generar las cuatro: al reflejarlas, la luz quedaría del lado equivocado.

`foliage_tl.png` (arriba a la izquierda):

```text
Decorative foliage cluster for the TOP-LEFT corner of a vertical mobile game screen, 1024x1024 pixels, PNG with a fully transparent background.
The plants grow from the top-left corner of the image toward the center. The dense part stops at about 50% of the width and 60% of the height; only a few thin leaf tips reach 60% of the width. The bottom-right 40% of the image stays completely empty and transparent.
Lush Amazon rainforest plants: one drooping açaí palm frond with long thin leaflets, two large glossy heart-shaped philodendron leaves, and one small heliconia flower bract in red-orange and yellow. Glossy leaves with clear midribs and side veins, three tones of green (#145A35, #23824A, #5DBB3F), bright yellow-green rim light (#C6EF6B) on the edges facing the upper-left light, soft shadows where leaves overlap, crisp clean silhouettes.
Style: polished hand-painted 2.5D casual mobile game art, saturated and luminous, warm sunrise light from the upper left.
Do not include: text, logos or watermark; animals or insects; fruits; more than one flower; monstera leaves or coconut palm leaves; sky or any background; frame lines; paper texture.
```

`foliage_tr.png` (arriba a la derecha; lleva el único racimo de açaí de la escena, guía 3.8):

```text
Decorative foliage cluster for the TOP-RIGHT corner of a vertical mobile game screen, 1024x1024 pixels, PNG with a fully transparent background.
The plants grow from the top-right corner of the image toward the center and stop at about 60% of the width and 50% of the height, leaving the left 40% and the bottom half of the image completely empty and transparent.
Lush Amazon rainforest plants: one drooping açaí palm frond with long thin leaflets hanging down, one large glossy philodendron leaf and a few smaller leaves. Hanging from the base of the açaí frond, firmly attached to it by thin orange-brown branching stalks, one small bunch of açaí berries: many tiny round glossy berries, deep purple almost black (#3B1F4F) with tiny highlights; the bunch stays in the upper half of the image. Glossy leaves with clear midribs and side veins, three tones of green (#145A35, #23824A, #5DBB3F), bright yellow-green rim light (#C6EF6B) on the edges facing the upper-left light, so the leaves facing away from the light are darker, soft shadows where leaves overlap, crisp clean silhouettes.
Style: polished hand-painted 2.5D casual mobile game art, saturated and luminous, warm sunrise light from the upper left.
Do not include: text, logos or watermark; animals or insects; any fruit other than that single attached açaí bunch; loose or fallen berries; bowls; flowers; monstera leaves or coconut palm leaves; sky or any background; frame lines; paper texture.
```

`foliage_bl.png` (abajo a la izquierda):

```text
Decorative foliage cluster for the BOTTOM-LEFT corner of a vertical mobile game screen, 1024x1024 pixels, PNG with a fully transparent background.
The plants grow upward from the bottom-left corner of the image and stop at about 55% of the width and 55% of the height, leaving the top-right 45% of the image completely empty and transparent.
Riverbank plants of the Amazon estuary: several upright arrow-shaped leaves on long stems (riverbank aroids), two broad glossy philodendron leaves and a few short grass blades. Three tones of green (#145A35, #23824A, #5DBB3F), bright yellow-green rim light (#C6EF6B) on the edges facing the upper-left light, clear midribs, soft shadows where leaves overlap, crisp clean silhouettes.
Style: polished hand-painted 2.5D casual mobile game art, saturated and luminous, warm sunrise light from the upper left.
Do not include: text, logos or watermark; animals or insects; fruits; flowers; water; ground or soil; monstera leaves or coconut palm leaves; any background; frame lines; paper texture.
```

`foliage_br.png` (abajo a la derecha):

```text
Decorative foliage cluster for the BOTTOM-RIGHT corner of a vertical mobile game screen, 1024x1024 pixels, PNG with a fully transparent background.
The plants grow upward from the bottom-right corner of the image and stop at about 55% of the width and 55% of the height, leaving the top-left 45% of the image completely empty and transparent.
Riverbank plants of the Amazon estuary: one heliconia stem with a single red-orange and yellow flower bract, several broad glossy leaves and a few arrow-shaped leaves. Three tones of green (#145A35, #23824A, #5DBB3F), bright yellow-green rim light (#C6EF6B) on the edges facing the upper-left light, clear midribs, soft shadows where leaves overlap, crisp clean silhouettes.
Style: polished hand-painted 2.5D casual mobile game art, saturated and luminous, warm sunrise light from the upper left.
Do not include: text, logos or watermark; animals or insects; fruits; more than one flower; water; ground or soil; monstera leaves or coconut palm leaves; any background; frame lines; paper texture.
```

### 3.4 `ui_cinta_hoja.png`: cinta de victoria

Familia S2, en la misma conversación que el follaje. Generar a 1536 × 1024 y llevar a **1344 × 256** (5,25:1) con escalado uniforme. 9-slice horizontal de 320 / 320 / 0 / 0 px con PPU 400; se muestra a 336 × 64 dp. Es una hoja verde: nada de cinta de tela ni roja. El juego escribe «¡Nivel completado!» en `crema` (#FFF8E6) encima de la franja central, así que esa franja tiene que ser verde oscura y lisa (guía 5.5).

```text
A single long horizontal banner made from one big glossy tropical leaf, for a "level complete" title in a mobile game, perfectly front-facing view, centered on a fully transparent background, 1536x1024 pixels, exactly 5.25:1 width-to-height ratio, filling about 90% of the image width.
A broad heliconia or banana leaf laid horizontally like a banner, with pointed tips at both ends that curl slightly. The central band of the leaf, covering the middle 75% of its length and the middle 60% of its height, is smooth, uniform and deep green, with a gentle vertical gradient from #2F7D32 to #236A29 and no veins, no highlights and no texture, so it can be stretched horizontally and hold light text. The light central midrib runs along the upper third of the leaf, above that band, and the fine side veins appear only near the edges and the tips. Fresher greens (#5DBB3F, #23824A) only at the tips and along the edges, a darker green edge (#145A35), and a bright rim light (#C6EF6B) only along the top edge. Glossy sheen near the tips, soft drop shadow below.
Style: premium casual mobile game UI, joyful, glossy, clean edges, light from the upper left, symmetric left to right.
Do not include: text or letters; fabric or cloth ribbons; red or gold; gems; stars; flowers; background.
```

### 3.5 Botones: `ui_btn_hoja.png`, `ui_btn_agua.png` y `ui_btn_madera.png` (opcionales)

Familia S3, con la ranura (3.6) y el disco (3.7). Generar a 1536 × 1024. Recortar un rectángulo **2:1** que contenga el botón y su sombra con un 2 % de margen (ampliar el lienzo con transparencia si hace falta) y llevarlo a **384 × 192** con escalado uniforme. 9-slice **solo horizontal** de 84 / 84 / 0 / 0 px con PPU 300: el sprite se muestra a su alto nativo de 64 dp y solo se estira a lo ancho. Los 84 px de cada lado cubren el radio de la cara (unos 60 px), el contorno y la sombra lateral. Las capas siguen la guía 5.1: sombra, canto de 6 dp, contorno, cara con degradado, brillo interior y lustre arriba. La cara va **vacía**: el texto y el icono los pone el juego. Un CTA de 56 dp de cara más canto (02 §3.3) se coloca con un `RectTransform` de 64 dp de alto.

`ui_btn_hoja.png`:

```text
A single game user-interface button, perfectly front-facing orthographic view, centered on a fully transparent background, 1536x1024 pixels.
A rounded-rectangle button with exactly a 2:1 width-to-height ratio including its thickness, filling about 80% of the image width, corner radius about 40% of the face height. Layered construction: a soft dark drop shadow below; a darker green bottom edge that shows the button thickness (#17501E); a thin dark green outline all around (#0F3A15); the top face with a vertical gradient from fresh light green (#86D25A) at the very top to medium green (#2F7D32) at one quarter of the height and deep green (#236A29) at the bottom; a thin bright inner highlight along the top inner edge; a soft glossy white sheen ellipse across the upper quarter. The face is completely empty and smooth.
Style: premium casual mobile game UI, glossy, chunky and tactile, clean vector-like edges, light from the upper left, perfectly symmetric left to right.
Do not include: any text, letters, numbers or icons; gold or metal; gems; bevelled golden frames; ribbons; stars or sparkles outside the button; leaves or decorations; background.
```

`ui_btn_agua.png` (misma estructura, colores de la familia Agua):

```text
A single game user-interface button, perfectly front-facing orthographic view, centered on a fully transparent background, 1536x1024 pixels.
A rounded-rectangle button with exactly a 2:1 width-to-height ratio including its thickness, filling about 80% of the image width, corner radius about 40% of the face height. Layered construction: a soft dark drop shadow below; a darker teal bottom edge that shows the button thickness (#0A4A59); a thin dark teal outline all around (#073640); the top face with a vertical gradient from light aqua (#8ADBE6) at the very top to clear river teal (#13788F) at one quarter of the height and deep teal (#0F6275) at the bottom, with a very faint water-ripple sheen; a thin bright inner highlight along the top inner edge; a soft glossy white sheen ellipse across the upper quarter. The face is completely empty and smooth.
Style: premium casual mobile game UI, glossy like polished glass and water, chunky and tactile, clean vector-like edges, light from the upper left, perfectly symmetric left to right.
Do not include: any text, letters, numbers or icons; gold or metal; gems; bevelled golden frames; ribbons; stars or sparkles outside the button; bubbles or fish; background.
```

`ui_btn_madera.png`:

```text
A single game user-interface button, perfectly front-facing orthographic view, centered on a fully transparent background, 1536x1024 pixels.
A rounded-rectangle button with exactly a 2:1 width-to-height ratio including its thickness, filling about 80% of the image width, corner radius about 40% of the face height, made of polished carved wood. Layered construction: a soft dark drop shadow below; a darker wood bottom edge that shows the button thickness (#2E2018); a thin very dark brown outline all around (#1E140E); the top face with a vertical gradient from light warm wood (#BF8F62) at the very top to medium wood (#7E5638) at one quarter of the height and dark wood (#46302A) at the bottom, with fine straight horizontal grain; a thin bright inner highlight along the top inner edge; a soft varnish sheen across the upper quarter. The face is completely empty.
Style: premium casual mobile game UI, chunky and tactile, clean edges, light from the upper left, perfectly symmetric left to right.
Do not include: any text, letters, numbers or icons; gold or metal; gems; nails or screws; carvings in the middle; ribbons; background.
```

### 3.6 `ui_slot_agua.png`: ranura de Deshacer (opcional)

Familia S3. Generar a 1024 × 1024. Recortar a la sombra con la proporción 8:9 y llevar a **192 × 216** con escalado uniforme. Se muestra a su tamaño nativo de 64 × 72 dp con PPU 300, así que no necesita 9-slice.

```text
A single square game button for a bottom toolbar, perfectly front-facing orthographic view, centered on a fully transparent background, 1024x1024 pixels.
A rounded square (squircle) button filling about 60% of the image, corner radius about 28% of its width. Layered construction: a soft dark drop shadow below; a darker teal bottom edge that shows the thickness (#0A4A59); a thin dark teal outline (#073640); the face with a vertical gradient from light aqua (#8ADBE6) at the top to river teal (#13788F) at one quarter and deep teal (#0F6275) at the bottom; a thin bright inner highlight on the top inner edge; a glossy white sheen ellipse in the upper quarter. The face is completely empty and smooth.
Style: premium casual mobile game UI, glossy, chunky and tactile, clean edges, light from the upper left, perfectly symmetric.
Do not include: text, letters, numbers, icons or arrows; gold or metal; gems; decorations; background.
```

### 3.7 `ui_disc_madera.png`: disco de Reiniciar (opcional)

Familia S3. Generar a 1024 × 1024 y llevar a **192 × 192**. Transparente. El disco ocupa unos 160 px del PNG final y la sombra el resto. Con PPU 400 se muestra a 48 × 48 dp.

```text
A single round game button, perfectly front-facing orthographic view, centered on a fully transparent background, 1024x1024 pixels.
A circular button filling about 80% of the image, made of polished carved wood: a soft dark drop shadow below; a darker wood bottom edge that shows the thickness (#2E2018); a thin very dark brown outline (#1E140E); the face with a vertical gradient from light warm wood (#BF8F62) at the top to medium wood (#7E5638) and dark wood (#46302A) at the bottom, fine straight grain; a thin bright inner highlight on the upper inner edge; a varnish sheen ellipse in the upper third. The face is completely empty.
Style: premium casual mobile game UI, chunky and tactile, clean edges, light from the upper left.
Do not include: text, letters, numbers, icons or arrows; gold or metal rim; gems; decorations; background.
```

### 3.8 `ui_panel_tablero.png`: marco del panel del tablero (opcional)

Familia S4, con el modal, la bandeja y la placa. Generar a 1024 × 1024 y llevar a **512 × 512**. Es **solo el marco**: el interior y el exterior quedan transparentes y **sin sombra**. El relleno azul petróleo (elegido por el dueño, D-004) y la sombra proyectada los dibuja el código (guía 4.6). 9-slice de 96 px por lado con PPU 400 (24 dp): cubre el radio exterior de 20 dp (80 px) y el margen.

```text
A single rectangular carved wooden frame for a game board, perfectly front-facing orthographic view, centered on a fully transparent background, 1024x1024 pixels, the frame filling the whole image with only a 2% empty margin on each side.
The frame is a band of medium reddish tropical hardwood (#7C4E3A with subtle warm straight grain), its thickness about 5% of the image width; a thin bright bevel on its top inner and outer edges (#D29B6E); a thin dark outer line (#2A1A10); a thin engraved groove line running parallel to the frame; rounded corners with an outer radius of about 16% of the image width; one small simple carved leaf motif in each of the four corners only.
The area inside the frame is completely empty and fully transparent. The area outside the frame is fully transparent. No drop shadow and no inner shadow.
Style: premium casual mobile game UI, polished and tactile, clean edges, light from the upper left, perfectly symmetric.
Do not include: text, letters, icons; gold or metal; gems; glass; any fill, color, texture or shadow inside the frame; plants growing over the frame; patterns or geometric ornaments on the frame; background.
```

### 3.9 `ui_panel_modal.png`: marco de los paneles de victoria y de bloqueo (opcional)

Familia S4. Generar a 1024 × 1024 y llevar a **512 × 512**. Solo el marco, con el interior y el exterior transparentes y sin sombra: la superficie clara (`panel_claro` → `panel_claro_fondo`), su lustre y la sombra los dibuja el código (guía 5.3). 9-slice de 112 px por lado con PPU 400 (28 dp): cubre el radio exterior de 24 dp (96 px) y el margen.

```text
A single rectangular carved wooden frame for a pop-up panel in a mobile game, perfectly front-facing orthographic view, centered on a fully transparent background, 1024x1024 pixels, the frame filling the whole image with only a 2% empty margin on each side.
The frame is a band of dark polished tropical hardwood (#36261C with subtle warm grain), its thickness about 6% of the image width; a thin bright bevel on its top edges (#BF8F62); a thin engraved groove line; rounded corners with an outer radius of about 19% of the image width; one small simple carved leaf motif in each corner.
The area inside the frame is completely empty and fully transparent. The area outside the frame is fully transparent. No drop shadow and no inner shadow.
Style: premium casual mobile game UI, polished and tactile, clean edges, light from the upper left, perfectly symmetric.
Do not include: text, letters, icons; paper or parchment; scrolls; gold or metal; gems; ribbons; patterns or geometric ornaments; any fill, color or texture inside the frame; background.
```

### 3.10 `ui_bandeja.png`: bandeja cumbuca (opcional)

Familia S4. Generar a 1536 × 1024. Recortar un rectángulo 2:1 que contenga la bandeja y su sombra y llevarlo a **640 × 320** con escalado uniforme. 9-slice solo horizontal de 128 / 128 / 0 / 0 px con PPU 400: se muestra a su alto nativo de 80 dp, y los 128 px de cada lado (32 dp) cubren el radio de 24 dp, la cuerda y la sombra. La versión anterior (512 × 256 estirada a 80 dp, con 88 px abajo) recortaba las esquinas inferiores.

```text
A single long shallow tray for the bottom toolbar of a mobile game, perfectly front-facing orthographic view, centered on a fully transparent background, 1536x1024 pixels, exactly 2:1 width-to-height ratio including its shadow, filling about 85% of the image width.
Made of carved dark tropical wood (#36261C with subtle warm grain): rounded ends with a corner radius of about 30% of its height, a recessed inner floor with a soft inner shadow, a neat twisted natural-fiber rope trim running along the top edge (#EED29A, #C49A5C, #8A6436), a thin bright bevel on the top edges, and a soft dark drop shadow below.
Style: premium casual mobile game UI, polished and tactile, clean edges, light from the upper left, perfectly symmetric left to right; the middle part must be uniform so it can be stretched horizontally.
Do not include: text, icons, buttons or objects inside the tray; gold or metal; gems; carvings in the middle; background.
```

### 3.11 `ui_placa_nivel.png`: insignia de nivel (opcional)

Familia S4. Generar a 1536 × 1024 y llevar a **544 × 208** (2,6:1) con escalado uniforme. Con PPU 400 se muestra a 136 × 52 dp, su tamaño máximo: la placa no se estira (guía 3.5), así que no necesita 9-slice.

```text
A single horizontal name plaque for the top bar of a mobile game, perfectly front-facing orthographic view, centered on a fully transparent background, 1536x1024 pixels, exactly 2.6:1 width-to-height ratio including the leaves and the shadow, filling about 85% of the image width.
A rounded plaque of carved dark tropical wood (#36261C with subtle warm grain), a thin bright bevel on its top edge (#BF8F62), a thin engraved groove line inside the border, and a soft drop shadow below. At each end, two small glossy green leaves (#23824A with bright rim light #C6EF6B) peek out from behind the plaque, mirrored left and right, each leaf no wider than 6% of the total width. The center of the plaque is empty, smooth and uniform.
Style: premium casual mobile game UI, polished and tactile, clean edges, light from the upper left, perfectly symmetric.
Do not include: text, letters or numbers; gold or metal; gems; crowns; ribbons; flowers; background.
```

### 3.12 `wood_dark_tile.png`: madera oscura en tesela (opcional)

Familia S5. Generar a 1024 × 1024 y llevar a **512 × 512 con tesela perfecta**. Opaco.

```text
Seamless tileable texture, square, flat top-down view with perfectly even lighting: dark polished tropical hardwood with fine straight horizontal grain, rich dark brown base (#36261C) with subtle warmer streaks (#46302A, #5A3D28), very low contrast, a soft varnished look. No knots, no cracks, no nails, no planks or seams, no bevels, no borders, no perspective, no shadows, no vignette, no light spots. The texture must repeat seamlessly on all four sides.
Do not include: text, logos, watermark, objects, paper texture, gold, metal.
```

### 3.13 `wood_light_tile.png`: madera de repisa en tesela (opcional)

Familia S5. Generar a 1024 × 1024 y llevar a **512 × 512 con tesela perfecta**. Opaco. Tono medio: la cara superior de la repisa no puede pasar de L* 65 (guía 4.5).

```text
Seamless tileable texture, square, flat top-down view with perfectly even lighting: warm medium honey-brown carved wood for shelves, straight horizontal grain, base colors #C4935E and #A97B4E with slightly darker grain lines (#866650), medium-low contrast, clean and smooth, light varnish sheen, never lighter than the base color. No knots, no cracks, no nails, no planks or seams, no bevels, no borders, no perspective, no shadows, no vignette. The texture must repeat seamlessly on all four sides.
Do not include: text, logos, watermark, objects, paper texture, gold, metal.
```

### 3.14 `capi_idle.png`: Capi en pose neutra (fase de jugabilidad)

Familia S6, con la clave adjunta. Generar a 1024 × 1024 y llevar a **512 × 512**. Transparente, con un margen del 10 %. Colores de la hoja de modelo de 07 §6.1. Mira al jugador, nunca a un lado donde pueda haber una cuia (guía 3.7). Queda pendiente de revisión cultural y de la búsqueda de marca del nombre (glosario §3.1). Si el dueño lo aprueba antes, puede entrar como Capi estático en F7 (guía 10.2).

> *Actualización del 2026-10-14 (D-016, decidida el 2026-10-13):* ninguna mascota fija en el muelle durante la partida (guía 3.7, retirada; C4 cerrado). Capi solo entra en escena para la Cuia extra, la Cuia de rescate y el banner de mundo, así que este sprite sirve para esas apariciones (96-140 dp), no a 64 × 56 dp en el muelle. El prompt de la hoja de modelo de TiTino que pedía f.3 de `docs/research/personaje/propuesta-titino.md` **queda pendiente** (D-016): se escribirá con su hoja de modelo, después del asesor cultural. La hoja de modelo la pinta el artista, con las condiciones de `docs/art/encargo-titino-frutas-tinaja.md`.

```text
Character sprite of a mascot for a cozy Amazon-themed mobile puzzle game, 1024x1024 pixels, PNG with a fully transparent background, full body, centered, about 10% empty margin on every side.
A capybara, the real animal, anatomically recognizable: barrel-shaped body with a squarish profile, short sturdy legs, large blunt rectangular muzzle, small rounded ears, small dark eyes with one small light reflection, no visible tail. Sitting calmly on its haunches in three-quarter view, body turned slightly to the right, head turned to look straight at the viewer, relaxed friendly expression, mouth closed.
Colors: warm brown fur (#8B6A3E) with soft darker brown shading, lighter belly and muzzle (#C8A872), dark brown nose (#4A3222); clean dark-brown outline (#3A2516); soft volume shading and a warm rim light from the upper left.
Style: premium casual mobile game mascot, polished 2.5D hand-painted look, appealing but not babyish, simple bold silhouette that still reads clearly when shown 64 pixels tall.
Do not include: clothes, hats, collars, scarves, accessories, jewelry, face paint or body patterns; props, food or objects; other animals; ground, grass or shadow plate; background; text, logos or watermark. It must not look like a bear, a rat, a dog, a beaver or a guinea pig.
```

### 3.15 Frutas del Estuario (fase de jugabilidad)

Familia S7: las cinco en la misma conversación, con la clave adjunta, para que compartan estilo y escala. Se generan a 1024 × 1024 y se llevan a **256 × 256**. Transparentes, con un margen del 10 %. Sirven para la tarjeta «Fruta nueva» de la fase de jugabilidad, no para los iconos de accesibilidad de las dosis. Los nombres y las asociaciones quedan pendientes de revisión del asesor cultural; el GDD ya avisa de que el color del tinte es estilizado (07 §3).

Bloque de estilo común, ya incluido en cada prompt: *glossy game icon, polished 2.5D hand-painted look, saturated, rim light from the upper left, centered, transparent background, no text*.

`fruta_acai.png` (açaí; el icono de accesibilidad es un bol, 07 §3):

```text
Game icon, 1024x1024 pixels, PNG with a fully transparent background, centered with about 10% empty margin.
A small plain smooth dark-brown gourd bowl, seen slightly from above, filled with thick, creamy, deep-purple açaí pulp with a glossy surface; in front of the bowl, five loose round açaí berries: small, deep purple almost black (#3B1F4F), with a faint dusty bloom and a tiny highlight.
Style: glossy game icon, polished 2.5D hand-painted look, saturated, rim light from the upper left, clean bold silhouette readable at 48 pixels.
Do not include: granola, banana slices, strawberries, spoons, ice cream, cups with straws; engravings, patterns or decorations on the bowl; blueberries or grapes look; table, cloth or background; text, logos or watermark.
```

`fruta_aguaje.png` (aguaje o buriti, *Mauritia flexuosa*; escamas, 07 §3):

```text
Game icon, 1024x1024 pixels, PNG with a fully transparent background, centered with about 10% empty margin.
Three aguaje (buriti) palm fruits: egg-sized oval fruits covered in glossy reddish-brown overlapping scales arranged like a pine cone or snake skin (#7A3B1E, #A0522D with lighter scale edges); one fruit is cut in half showing a thin layer of bright orange-yellow pulp (#EF9A2A) around a large pale hard seed.
Style: glossy game icon, polished 2.5D hand-painted look, saturated, rim light from the upper left, clean bold silhouette readable at 48 pixels.
Do not include: citrus fruits, oranges, lychees, pineapples, pine cones, leaves, table or background; text, logos or watermark.
```

`fruta_tapereba.png` (taperebá o cajá, *Spondias mombin*; ciruela, 07 §3):

```text
Game icon, 1024x1024 pixels, PNG with a fully transparent background, centered with about 10% empty margin.
A small cluster of five taperebá (hog plum, Spondias mombin) fruits: small oval plums about 3 cm long with thin smooth glossy golden-yellow to yellow-orange skin (#F2B705, #E89B12) and tiny darker speckles, attached by short thin stems to a small twig with two narrow pinnate leaflets.
Style: glossy game icon, polished 2.5D hand-painted look, saturated, rim light from the upper left, clean bold silhouette readable at 48 pixels.
Do not include: lemons, oranges, mangoes, apricots or any citrus; cut fruit; table or background; text, logos or watermark.
```

`fruta_guanabana.png` (guanábana, *Annona muricata*; fruto espinoso, 07 §3):

```text
Game icon, 1024x1024 pixels, PNG with a fully transparent background, centered with about 10% empty margin.
One large soursop (guanábana, Annona muricata) fruit: heart-shaped to oval, dark green skin (#3F7A2E, #5E9A3A) covered with many short soft curved spines, with a lighter highlight; next to it, a thick slice showing creamy white fibrous pulp (#FAFAF7) with a few glossy black seeds.
Style: glossy game icon, polished 2.5D hand-painted look, saturated, rim light from the upper left, clean bold silhouette readable at 48 pixels.
Do not include: durian, jackfruit, pineapple, custard apple look, knife, plate, table or background; text, logos or watermark.
```

`fruta_bacaba.png` (bacaba, *Oenocarpus bacaba*; racimo, 07 §3):

```text
Game icon, 1024x1024 pixels, PNG with a fully transparent background, centered with about 10% empty margin.
A hanging bunch of bacaba palm fruits: many small round fruits, deep purple-black with a soft lilac-pink sheen (#3A2440 with highlights #E58FC6), densely attached to thin orange-brown branching stalks (rachillae) that fan out from one short main stem, like a broom-shaped cluster.
Style: glossy game icon, polished 2.5D hand-painted look, saturated, rim light from the upper left, clean bold silhouette readable at 48 pixels.
Do not include: grapes, blueberries or cherries look; leaves; bowls or glasses; table or background; text, logos or watermark.
```

### 3.16 Opcional para la fase de detalle: `ui_slot_ala_pista.png`

Ranura de Pista · Morpho, a partir del nivel 12 (fuera de v0). Silueta de **ala** de mariposa [P]: cuadrado redondeado con el borde superior derecho en curva de ala. No una hoja, que se confundiría con la mecánica Hoja y con Hoja escudo (guía 3.6). Color base azul morpho #2F6FD6 (07 §7.6). Se genera solo cuando llegue esa fase, en la familia S3 y con el prompt de 3.6 cambiando la forma y los colores.

### 3.17 Héroes provisionales de parada: `bg_hero_ver_o_peso.png`, `bg_hero_teatro_amazonas.png` y `bg_hero_leticia.png` (opcionales)

> **Aviso: provisionales.** Estos PNG sirven para ver un sitio pintado en desarrollo antes de que llegue el arte del artista. **Nunca van en la ficha de la tienda, en las capturas ni en los creativos de UA** (propuesta del Mapa del río §10.2; 07 §2.1), la misma regla que los miquitos provisionales (07 §6.4). Antes de grabar o capturar nada para la tienda o la UA, se sustituyen por los del artista o se quitan de la carpeta; el registro del README dice cuáles son provisionales. Los héroes no sustituyen a VA-20, que se valida con la silueta geométrica (guía 9). El arte final es del artista: `docs/art/encargo-mapa-del-rio.md`.

Familia S8: los tres en la misma conversación, con la clave adjunta. Son los tres primeros sitios del encargo al artista, uno por cada mundo con fondo en el AAB y héroe de ciudad: 1 Estuario, 3 Manaos y 4 Trapecio. Qué se reconoce y qué evitar en cada uno: 07 §2.1 (tabla de paradas).

Formato (propuesta del Mapa §10.2 y §10.6):
- **Generar** a 1536 × 1024. **Recortar** una banda de 1536 × 512 que deje el horizonte (el pie del hito) al 60 % de su alto y llevarla a **1440 × 480** con escalado uniforme: es el doble de 240 × 80 dp, a 6 px por dp.
- **Dentro de la banda final:**
  - el horizonte queda a 288 px del borde de arriba (48 dp);
  - la silueta puede llegar hasta el borde de arriba, pero lo que hace reconocible el sitio cabe en los 180 px justo encima del horizonte, los 30 dp que se ven con 2 filas;
  - por debajo, como mucho 192 px (32 dp) de orilla o de muelle.
- **Transparente** todo lo demás: el cielo, la hora y la banda de agua los pinta el código, y también el reflejo, con el mismo sprite volteado. Por eso el prompt no pide agua ni reflejo.
- **Sin personas, canastos, banderas ni cuias** (VA-22), ni canoas o barcos, ni frutas sueltas.

`bg_hero_ver_o_peso.png` (Belém, niveles 1-10; alba dorada):

```text
Wide, low landmark element for the background of a vertical mobile puzzle game, 1536x1024 pixels, PNG with a fully transparent background. The image will be cropped to a 3:1 horizontal strip, so keep everything between 25% and 75% of the image height.
Subject: the Ver-o-Peso market in Belém, Pará, Brazil, seen from the water of Guajará Bay at sunrise: the long, low cast-iron market hall from 1901 with two slender pointed turrets at its ends, and a few slender açaí palms beside it (clumping palms with very thin trunks and drooping feathery fronds).
Composition:
- a straight, level horizon line at 55% of the image height; the foot of the market and the stone quay wall sit exactly on it;
- the market is centered, low and wide, about 60% of the image width; the hall and its two turrets rise no more than 18% of the image height above the horizon; only the thin turret tips and the palm crowns may reach 30% above it;
- below the horizon, only the front of the quay wall, down to 75% of the image height at most; no water and no reflection;
- toward the left and right ends the riverbank becomes a few low trees that fade out, so the strip blends into a painted riverbank;
- everything else, including the whole sky, is fully transparent.
Light: warm golden sunrise light from the upper left, a thin low golden mist (#FFE4A8) at the foot of the market.
Style: premium casual mobile game background, polished hand-painted 2.5D look, luminous and saturated but harmonious, soft atmospheric perspective, clean readable shapes; the silhouette must read clearly when the whole strip is shown 240 points wide.
Do not include: people or crowds; baskets, crates, market stalls or goods; flags or flagpoles; boats, ships or canoes; gourds, calabash bowls, cups or bottles; loose fruit; religious images, processions, candles or banners; herbs, potions or charms; text, letters, signs, logos, signature or watermark; sky, clouds, sun or birds; water or reflections; frames or borders; paper or watercolor texture.
```

`bg_hero_teatro_amazonas.png` (Manaos, niveles 51-60; tarde con luz rasante):

```text
Wide, low landmark element for the background of a vertical mobile puzzle game, 1536x1024 pixels, PNG with a fully transparent background. The image will be cropped to a 3:1 horizontal strip, so keep everything between 25% and 75% of the image height.
Subject: the Amazon Theatre (Teatro Amazonas) of Manaus, Amazonas, Brazil, seen from far away over the low roofs of the city by the Rio Negro: its large rounded dome on a short drum, covered in colorful glazed tiles, rising above the rooftops and trees; far behind it, small and hazy, the low line of the floating port and the long bridge over the Rio Negro.
Composition:
- a straight, level horizon line at 55% of the image height; the foot of the city and the riverbank sit exactly on it;
- the dome is centered; the dome and its drum rise no more than 18% of the image height above the horizon, and nothing rises more than 30% above it;
- around the dome, the city is a low, simple band of roofs and trees, about 70% of the image width, that fades out toward both ends;
- below the horizon, only the riverbank, down to 75% of the image height at most; no water and no reflection;
- everything else, including the whole sky, is fully transparent.
Light: warm late-afternoon raking light from the upper left.
Style: premium casual mobile game background, polished hand-painted 2.5D look, luminous and saturated but harmonious, soft atmospheric perspective, clean readable shapes; the silhouette must read clearly when the whole strip is shown 240 points wide.
Do not include: people or crowds; flags or flagpoles; boats, ships or canoes; baskets; gourds, calabash bowls, cups or bottles; loose fruit; rubber trees, rubber tappers or any historical scene; cars or traffic; text, letters, signs, logos, signature or watermark; sky, clouds, sun or birds; water or reflections; frames or borders; paper or watercolor texture.
```

`bg_hero_leticia.png` (Leticia, niveles 81-100; mañana tras la lluvia; la parada 9 lo reutiliza con la LUT de atardecer):

```text
Wide, low landmark element for the background of a vertical mobile puzzle game, 1536x1024 pixels, PNG with a fully transparent background. The image will be cropped to a 3:1 horizontal strip, so keep everything between 25% and 75% of the image height.
Subject: the riverside promenade (malecón) of Leticia, Amazonas, Colombia, on a very wide stretch of the Amazon river: a long, low waterfront walkway with a simple railing and wide steps down to the river, rows of leafy tropical trees and a few low buildings with tiled roofs behind them; far in the distance along the same bank, the low, hazy riverbank of the neighbouring town of Tabatinga.
Composition:
- a straight, level horizon line at 55% of the image height; the foot of the promenade sits exactly on it;
- the promenade is centered, low and wide, about 70% of the image width; its trees and buildings rise no more than 18% of the image height above the horizon, and nothing rises more than 30% above it;
- below the horizon, only the front wall and the steps of the promenade, down to 75% of the image height at most; no water and no reflection;
- toward both ends it fades into low riverbank trees;
- everything else, including the whole sky, is fully transparent.
Light: soft, bright morning light from the upper left just after rain, wet surfaces with gentle highlights.
Style: premium casual mobile game background, polished hand-painted 2.5D look, luminous and saturated but harmonious, soft atmospheric perspective, clean readable shapes; the silhouette must read clearly when the whole strip is shown 240 points wide.
Do not include: people or crowds; flags or flagpoles; islands; boats, ships or canoes; baskets or market stalls; gourds, calabash bowls, cups or bottles; loose fruit; text, letters, signs, logos, signature or watermark; sky, clouds, sun or birds; water or reflections; frames or borders; paper or watercolor texture.
```

Comprobar:
- el hito se reconoce en los 180 px de encima del horizonte, y la banda tapada salvo esa franja sigue diciendo qué sitio es (la prueba de VA-20 con 2 filas);
- nada de personas, canastos, banderas, cuias, barcos ni frutas sueltas;
- el cielo y el agua son 100 % transparentes y el horizonte es recto y horizontal;
- los extremos se funden en árboles bajos, sin cortar un edificio.

### 3.18 Kit provisional del Estuario: `bg_kit_w01.png` (opcional)

> **Aviso: provisional.** El mismo de 3.17: nunca va en la ficha, las capturas ni la UA, y el kit final es del artista (`docs/art/encargo-mapa-del-rio.md`).

Familia S8, en la misma conversación que los héroes. El kit es un **atlas de 2048 × 2048** con 8 piezas sobre fondo transparente (capa 0b de la propuesta del Mapa §10.2). El dueño lo monta con 8 imágenes generadas por separado:
- **Reparto del atlas:** provisional [P]. El definitivo lo decide el artista, y la posición de cada pieza va en `bg.pieces` de `river_stops_v1.json` (D-023, punto 12), que escribe la tarea que integra los kits.
- **Sin playas:** en el Estuario, el río pardo se leía como arena (revisión 3).
- **Sin agua:** la banda de río la pinta el código; solo los árboles con el pie en el agua llevan un anillo fino de ondas.

| Zona del atlas (px) | Pieza | Generar | Llevar a |
|---|---|---|---|
| x 0-2047, y 0-511 | 1. Tira de orilla lejana, que se repite en horizontal | 1536 × 1024 | Banda de 1536 × 384 con la línea del agua abajo, ampliada a 2048 × 512 |
| x 0-2047, y 512-1023 | 2. Tira de orilla media, que se repite en horizontal | 1536 × 1024 | Ídem |
| x 0-511, y 1024-1535 | 3. Grupo alto de palmeras de açaí | 1024 × 1024 | 512 × 512 |
| x 512-1023, y 1024-1535 | 4. Grupo bajo y ancho de palmeras de açaí | 1024 × 1024 | 512 × 512 |
| x 1024-1535, y 1024-1535 | 5. Isla de várzea pequeña | 1024 × 1024 | 512 × 512 |
| x 1536-2047, y 1024-1535 | 6. Árboles con el pie en el agua | 1024 × 1024 | 512 × 512 |
| x 0-511, y 1536-2047 | 7. Barco de río lejano, sin nadie a bordo | 1024 × 1024 | 512 × 512 |
| x 512-1023, y 1536-2047 | 8. Silueta de ciudad genérica | 1024 × 1024 | 512 × 512 |
| x 1024-2047, y 1536-2047 | Libre (transparente) | — | — |

Las tiras son lo único que se amplía (×1,33): se acepta en piezas lejanas con bruma, nunca en un héroe. Cada pieza va centrada en su celda, con su base en el borde de abajo de la celda y sin salirse de ella.

Tiras (piezas 1 y 2), con la misma estructura:

```text
Seamless horizontal strip for the far background of a vertical mobile puzzle game, 1536x1024 pixels, PNG with a fully transparent background. The image will be cropped to a 4:1 horizontal strip, so keep everything between 31% and 69% of the image height.
Subject: a distant riverbank of the Amazon estuary near Belém, Pará, Brazil, at sunrise, seen from the water.
Strip 1 (far): a thin, fairly even line of far rainforest in blue-green morning haze (#7FB0A2, #4E8C78), with a few slender açaí palm crowns poking above it and a thin golden mist (#FFE4A8) along its foot; it rises no more than 20% of the image height above its base.
Strip 2 (middle): a closer band of floodplain (várzea) forest with varied rounded tree crowns in darker greens (#1C5236, #2C7A4B, #23824A) and a few açaí palms, rim light (#C6EF6B) on the crowns facing the upper-left light; it rises no more than 30% of the image height above its base.
Composition: the base of the forest is a straight, level waterline at 69% of the image height; the left and right edges match exactly, so the strip repeats seamlessly side by side; no single tall tree at either edge.
Light: warm golden sunrise light from the upper left, soft atmospheric perspective.
Style: premium casual mobile game background, polished hand-painted 2.5D look, luminous and harmonious, clean readable shapes.
Do not include: people or animals; houses, villages or docks; flags or flagpoles; boats, ships or canoes; baskets; gourds, calabash bowls, cups or bottles; fruit; beach, sand or sandbanks; mountains or hills; coconut palms; giant kapok (sumaúma) trees; water or reflections; sky, clouds or sun; text, letters, logos, signature or watermark; frames or borders; paper or watercolor texture.
```

Se genera dos veces: una con la línea «Strip 1» y otra con la línea «Strip 2», quitando la que no toca.

Piezas 3-8: pega el prompt y sustituye la línea `Element:` por la de la pieza.

```text
Single background element for a vertical mobile puzzle game, 1024x1024 pixels, PNG with a fully transparent background, centered with about 8% empty margin; its base sits on a straight, level line at 90% of the image height, as if it stood on a distant riverbank.
Element: (paste the line of the piece here)
Place and light: the wide Amazon estuary near Belém, Pará, Brazil, at sunrise; warm golden light from the upper left and soft distant haze; the element is seen from far away, so it is simple, slightly hazy and low in contrast.
Style: premium casual mobile game background, polished hand-painted 2.5D look, luminous and harmonious, clean readable silhouette.
Do not include: people or animals; flags or flagpoles; canoes; baskets; gourds, calabash bowls, cups or bottles; loose fruit; beach, sand or sandbanks; mountains; coconut palms; giant kapok (sumaúma) trees; sky, clouds or sun; text, letters, signs, logos, signature or watermark; frames or borders; paper or watercolor texture.
```

Líneas `Element:`:
- **3:** `Element: a tall group of five to seven slender açaí palms (clumping palms with very thin trunks and drooping feathery fronds) standing close together, three tones of green (#145A35, #23824A, #5DBB3F).`
- **4:** `Element: a low, wide group of three or four young açaí palms among low riverbank shrubs.`
- **5:** `Element: a small, low floodplain (várzea) island seen from the side: a narrow strip of land covered by rounded trees and a few açaí palms, with a flat waterline at its base and no beach.`
- **6:** `Element: a few floodplain trees standing with their feet in the water, with only a thin ring of small light ripples in milky coffee-brown water (#B08A5A) around their bases.`
- **7:** `Element: a distant two-deck wooden river passenger boat seen from the side, white with one colored stripe, small and simple, with nobody on board, no name and no letters on the hull.`
- **8:** `Element: a generic low city skyline seen from very far away across the water: a simple row of pale low buildings and a few trees in haze, with no recognizable landmark, no towers, no domes and no churches.`

Comprobar:
- las tiras casan al repetirlas, sin costura ni árbol cortado;
- ninguna pieza se sale de su zona del atlas;
- las palmeras son de açaí (finas y en grupo), nunca de coco, y no hay sumaúma;
- el barco es un barco de pasajeros lejano, sin nadie a bordo, nunca una canoa (el icono de Canoas; VA-22);
- nada de personas, canastos, banderas, cuias ni frutas.

## 4. Comprobación de cada PNG antes de guardarlo

| # | Comprobación | Cómo |
|---|---|---|
| 1 | Nombre y tamaño finales exactos (tabla 2) | Propiedades del archivo |
| 2 | Alfa correcto: sin halo y con el 20 % superior de `bg_estuario_muelle` vacío | Ver sobre fondo negro y sobre magenta |
| 3 | Sin texto, letras, firmas ni marcas de agua | Inspección a 100 % |
| 4 | Nada de la lista «Evitar» de la guía (7): oro, gemas, cintas rojas, gatos, calderos, castillos, montañas, palmeras de coco, papel, personas, casas, canoas, frutas sueltas | Inspección |
| 5 | Colores dominantes cerca de los HEX del prompt (ΔE00 ≤ 10 a ojo con el cuentagotas). Si no, ajustar tono y saturación | Cuentagotas |
| 6 | Luz desde arriba a la izquierda | Inspección |
| 7 | 9-slice: esquinas enteras dentro de los bordes de la tabla; centro uniforme en la dirección de estiramiento; simétrico | Guías en el editor de imagen |
| 8 | Teselas: sin costura visible al repetir 2 × 2 | Repetir la tesela en un lienzo de 1024 × 1024 |
| 9 | Botánica: açaí en palmeras finas y en grupo, y su racimo unido a la fronda; aguaje con escamas; taperebá como ciruela pequeña; guanábana verde con espinas blandas; bacaba en racimo de escoba. Ninguna parece un cítrico (error del concepto de la revisión) | Comparar con la descripción del prompt |
| 10 | Capi: anatomía de capibara, sin ropa ni accesorios, mirando al frente | Inspección |
| 11 | ≤ 2 MB y en sRGB | Propiedades del archivo |
| 12 | **Hoja de contacto:** todos los PNG de la familia, y después todos los aprobados, lado a lado junto al panorama en un lienzo neutro, a su tamaño relativo en pantalla. Mismo acabado, grosor de línea, saturación y luz. El que desentone se regenera con la clave adjunta | Lienzo de 2048 px de ancho en GIMP o Krita, antes de integrar nada |
| 13 | **Escalado uniforme:** la proporción del recorte es la del tamaño final; nada se ha estirado en una sola dirección (las esquinas redondeadas siguen siendo circulares) | Comparar la proporción del recorte con la tabla 2 |
| 14 | **Marcos** (`ui_panel_tablero`, `ui_panel_modal`): interior y exterior 100 % transparentes, sin sombra ni relleno | Ver sobre magenta |
| 15 | **Cinta-hoja:** la franja central es verde oscura (#2F7D32 → #236A29), lisa y sin nervios; el nervio va en el tercio superior. Con el cuentagotas, el `crema` #FFF8E6 da ≥ 4,5:1 sobre cualquier punto de la franja | Cuentagotas y una calculadora de contraste |
| 16 | **Panorama:** palmeras de açaí y río pardo visibles entre el 16 y el 30 % de la altura, en el centro (3.1) | Tapar el resto con un rectángulo |
| 17 | **Fondos por parada (S8):** nada de personas, canastos, banderas, cuias, canoas ni frutas sueltas (VA-22). En el héroe, el horizonte a 288 px del borde de arriba, lo reconocible en los 180 px de encima y como mucho 192 px de orilla por debajo; el cielo y el agua, transparentes. Los edificios del hito y la ciudad genérica del kit son la excepción a «casas» de la comprobación 4 | Guías en el editor de imagen y vista sobre magenta |
| 18 | **Kit (S8):** cada pieza en su zona del atlas (3.18) y las dos tiras sin costura al repetirlas | Repetir la tira en un lienzo de 4096 px de ancho |
| 19 | **Aviso de provisional (S8):** anotado como provisional en el registro del README, para que nunca llegue a la ficha, las capturas ni la UA | Registro del README |

## 5. Plantilla del registro (`Assets/_Project/Art/Generated/README.md`)

```markdown
# Recursos generados (provisionales)

Generados por el dueño con ChatGPT (generación de imágenes) a partir de `docs/art/prompts-chatgpt.md`.
Provisionales: el arte final es del artista (CLAUDE.md, «Humanos»). Estado cultural: pendiente de revisión (07 §8.2).

| Archivo | Fecha | Prompt (sección) | Familia | Clave de estilo adjunta | Cambios al prompt | Posproceso | 9-slice medido (px) | Revisión cultural |
|---|---|---|---|---|---|---|---|---|
| bg_estuario_panorama.png | AAAA-MM-DD | 3.1 | S0 | — (es la clave) | — | Sin cambios de tamaño | — | Pendiente |
```
