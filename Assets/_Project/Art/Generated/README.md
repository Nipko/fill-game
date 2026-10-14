# Recursos generados (provisionales): ranuras de arte

Carpeta de los PNG pintados provisionales de la dirección visual v2 «Selva brillante premium» (D-003; `docs/art/direccion-visual-v2.md` 10.3). Los genera el dueño con ChatGPT a partir de `docs/art/prompts-chatgpt.md`. Son provisionales: el arte final es del artista (CLAUDE.md, «Humanos»), que entra por las mismas ranuras con los encargos `docs/art/encargo-mapa-del-rio.md` y `docs/art/encargo-titino-frutas-tinaja.md`. Estado cultural: pendiente de revisión (07 §8.2).

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

## Ranuras reservadas: fondos por parada, Mapa del río, TiTino y Frutas del río

Nombres del glosario §11 (D-013, D-016 y D-017). **Reservada** quiere decir que el nombre está fijado, pero el juego todavía no pinta esa ranura: un PNG con ese nombre aún no cambia nada y, mientras `ArtSlots.IsKnown` no conozca el nombre, se ignora con un aviso en la consola. Cada ranura pasa a «Conectada» con la tarea que la integra.
- **Arte final:** del artista, con los encargos `docs/art/encargo-mapa-del-rio.md` y `docs/art/encargo-titino-frutas-tinaja.md` (orden y condiciones de envío).
- **Provisionales del dueño:** los de `docs/art/prompts-chatgpt.md` (3.17 y 3.18). Nunca van en la ficha, las capturas ni la UA.
- **Compresión en Android** (propuesta del Mapa §10.7; la aplica el script de importación, nunca el `.meta` a mano): **ASTC 8×8 en los kits, 6×6 en los héroes y 4×4 en la UI**. Las demás familias van marcadas en la tabla.

| Ranura (`<ranura>.png`) | Medida (px) | Uso | Respaldo sin PNG | Compresión | Estado |
|---|---|---|---|---|---|
| `bg_kit_w{nn}`: en la 1.0, `bg_kit_w01` … `bg_kit_w09` (`w01`-`w05` en 1-200; `w06`-`w09` en 201-700) | Entrega: atlas de 2048 × 2048 (a 2×) con 8-10 piezas. En el build: 1024 × 1024 [P] | Capa 0b del fondo de cada parada de su mundo (tablero, inicio, banner, victoria con vista y tarjeta del Mapa): orilla lejana en tiras que se repiten, selva y palmeras del mundo, islas, playas de río, árboles en el agua, barcos lejanos sin personas y silueta de ciudad genérica. La posición de cada pieza va en `bg.pieces` de `river_stops_v1.json` (D-023, punto 12). En 201-700 es todo el fondo, sin héroe | El panorama procedural de hoy; si falta el pack del mundo, el «río genérico» del canon | ASTC 8×8 | Reservada |
| `bg_hero_<sitio>`: en 1-200, `bg_hero_ver_o_peso`, `bg_hero_alter_do_chao`, `bg_hero_flona_tapajos`, `bg_hero_teatro_amazonas`, `bg_hero_anavilhanas`, `bg_hero_lago_tefe`, `bg_hero_leticia`, `bg_hero_amacayacu`, `bg_hero_puerto_narino`, `bg_hero_tarapoto`, `bg_hero_iquitos`, `bg_hero_itaya_creciente`, `bg_hero_allpahuayo` y `bg_hero_pacaya_samiria`, más la reserva `bg_hero_casa_de_fierro` (07 §2.1). Nunca `belen` | Entrega: 1440 × 480 (2× de 240 × 80 dp). En el build: 720 × 240 [P] | Capa 0c: el héroe del sitio, con el pie en el horizonte de `GameScreen.HorizonFor`. Hasta 48 dp de silueta por encima, con lo reconocible en sus 30 dp de abajo, y hasta 32 dp de orilla por debajo; el código alarga el reflejo con el mismo sprite volteado (propuesta del Mapa §10.2 y §10.6) | La silueta geométrica de la misma altura, que es también el héroe provisional de VA-20, solo en el Editor y en desarrollo; en release, la parada va sin héroe (D-023, punto 16) | ASTC 6×6 | Reservada |
| `amb_*`: `amb_parakeets`, `amb_dolphin_back` y `amb_mist` | 256 × 256 | Capa 1: el ambiente del sitio, animado por código con 16 partículas como mucho: pericos en Leticia, lomos de delfín en Tarapoto y bruma en Pacaya-Samiria | Sin el sprite, el sitio va sin su ambiente [P]; lo fija la tarea que lo integra | ASTC 4×4 [P], como los sprites de fauna de hoy | Reservada |
| `map_tile_*`: `map_tile_canopy`, `map_tile_varzea`, `map_tile_savanna`, `map_tile_rock` y `map_tile_snow` | 512 × 512, tesela sin costuras | Tierra del Mapa del río en mosaico, una por zona (dosel, várzea, sabana, roca y nieve). Las zonas salen de la máscara del artista, que es un dato de `tools/river-map` y no una ranura | Por código (guía 10.3) | ASTC 6×6 (§10.7) | Reservada |
| `map_canoe` | Bucle de 12 fotogramas a 32-40 dp | La canoa de Capi remando en el Mapa, con las cuias atadas con cuerda y un mástil con anclaje para la bandera; sin personas (del bucle de `capi_rescue`) | Por código | ASTC 4×4 | Reservada |
| `map_flag_<iso2>`: solo banderas oficiales; `map_flag_br`, `map_flag_co` y `map_flag_pe` en 1-200, y también `map_flag_ec` y `map_flag_bo` en 201-700 | 96 × 64, con 2 fotogramas | La bandera de cortesía en el mástil de la canoa, que se iza al entrar en un país. Nunca en tierra, en islas ni en zonas reclamadas; Bolivia, con la tricolor [P]; el uso de símbolos nacionales lo revisan el abogado y el asesor (D-013) | Bandera simplificada solo en el Editor y en desarrollo; en release, solo las oficiales aprobadas (D-023, punto 16) | ASTC 4×4 | Reservada |
| `map_minicuia` | 96 × 160, vidrio en capas | La mini cuia del Muestrario, con su etiqueta: la silueta de D-006, nunca un tubo. El líquido lo pinta el código | Por código | ASTC 4×4 | Reservada |
| `map_rack` | 9-slice (bordes medidos en el PNG real) | La repisa del Muestrario, de madera tallada (guía 4.4 y 4.5) | Por código | ASTC 4×4 | Reservada |
| `map_home` | 256 × 256 | La casa de TiTino en el Mapa: su embaúba a escala de Mapa. Lleva la condición de TiTino: el asesor confirma la embaúba (encargo de TiTino) | Por código, solo en el Editor y en desarrollo (D-023, punto 16) | ASTC 4×4 | Reservada |
| `map_home_window` | 9-slice (bordes medidos en el PNG real) | La ventanita de casa de TiTino, en el Mapa y en el inicio (Q17-A de D-013) | Por código, solo en el Editor y en desarrollo (D-023, punto 16) | ASTC 4×4 | Reservada |
| `map_compass`, `map_plaque`, `map_buoy`, `map_beacon` y `map_mirador` | Piezas del atlas de UI del Mapa | La rosa de los vientos, las placas, la boya, la baliza y el «?» de los miradores, en madera y vidrio | Por código | ASTC 4×4 | Reservada |
| `fauna_bradypus_variegatus_partes` | 1024 × 1024, rejilla de 4 × 4 celdas de 256 px con pivotes fijos | Las piezas del rig de TiTino; las poses `sloth_*` las anima el código. Incluye las hojas de `sloth_cheer` (propuesta de TiTino, c.7) | El rig procedural provisional, solo en el Editor y en desarrollo; en release, TiTino no sale sin su arte final (07 §6.5; D-023, punto 16) | ASTC 4×4, como la fauna de hoy | Reservada |
| `fauna_bradypus_variegatus_retrato` | 512 × 512 | El retrato de TiTino: la placa del Dato del río en el Mapa, la instrucción del tutorial, el aviso tras Volver a la orilla y los bocadillos | El retrato procedural, solo en el Editor y en desarrollo | ASTC 4×4 | Reservada |
| `arbol_embauba` | 1024 × 256 | La rama de embaúba que entra con TiTino en cada pantalla (no usa el patrón `rama_<mundo>`, que es de los mundos) | La rama procedural, solo en el Editor y en desarrollo | ASTC 4×4, como las ramas de hoy | Reservada |
| `ficha_<id del tinte>` [P] × 14, en el orden de las series: `ficha_acai`, `ficha_agua_tapajos`, `ficha_urucum`, `ficha_agua_negro`, `ficha_cupuacu`, `ficha_aguaje`, `ficha_bacaba`, `ficha_jenipapo`, `ficha_camu_camu`, `ficha_tapereba`, `ficha_cacao`, `ficha_lulo`, `ficha_guanabana` y `ficha_araza` (ids del glosario §9) | 1024 × 640 | La escena de la cabecera de la ficha «Frutas del río» (y de la página Frutas del Cuaderno, en la 1.1): la planta en su paisaje y el fruto entero y abierto | La ficha funciona solo con el icono `fruta_<id del tinte>` (propuesta de Frutas, §7) | ASTC 6×6 [P], como un fondo | Reservada |

**Para estimar el peso** (tarea B0) y fijar la importación (M5.2):
- **Héroes y kits:** las cifras de la propuesta del Mapa (§10.7) suponen las piezas de fondo a 1× en el build (3 px/dp): un héroe de 720 × 240 son 75,0 KiB en ASTC 6×6 y un kit de 1024 × 1024, 256 KiB en 8×8. Importados a su medida de entrega (2×), pesarían 4 veces más: 300 KiB y 1 MiB [cálculo]. Cómo se pasa de 2× a 1× lo decide la tarea de importación.
- **Mapa:** las piezas de UI cuentan como un atlas de 1024 × 1024 en ASTC 4×4 (≈ 1,0 MiB, con las siluetas de las muestras horneadas) y las 5 texturas de terreno, 0,56 MiB en 6×6 (§10.7).
- **Fichas:** las 14 escenas suman ≈ 3,9 MiB en 6×6 y 8,75 MiB en 4×4 [cálculo].

**Piezas encargadas que aún no tienen nombre de ranura en el glosario §11.** El nombre se fija allí antes de integrarlas; mientras tanto, ningún PNG lleva un nombre inventado. Son:
- el sello «Ya conoces» [P] (256 × 256) y los dos marcos 9-slice de la línea y de la ficha de Frutas del río;
- el bocadillo 9-slice de TiTino;
- las piezas de la Tinaja: cuerpo, máscara de la boca, bandas encendidas, boca abajo, jirau y asa de la cuia;
- el segundo juego de follaje, de Amazonia occidental (propuesta del Mapa §12, n.º 4), que la guía visual llama `foliage_<juego>_*` (10.3).

## Registro de PNG

Cada PNG que entra en la carpeta se anota aquí. El del artista lleva «Artista» y su encargo en la columna «Prompt (sección)». Así se sabe qué PNG es provisional: los provisionales de los fondos por parada y del Mapa nunca van en la ficha, las capturas ni la UA (07 §2.1; propuesta del Mapa §10.2), como los miquitos provisionales (07 §6.4).

| Archivo | Fecha | Prompt (sección) | Familia | Clave de estilo adjunta | Cambios al prompt | Posproceso | 9-slice medido (px) | Revisión cultural |
|---|---|---|---|---|---|---|---|---|
| — | — | — | — | — | — | — | — | — |
