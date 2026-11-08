# Propuesta: el Mapa del río v2 (1.1)

Fecha: 2026-11-06 · Autor: dirección de arte y diseño del Mapa (agente) · Para: el dueño · Estado: **propuesta, nada decidido**. No cambia código, datos, herramientas ni el GDD; las preguntas de la sección 8 llevan su recomendada.

Fuentes: D-013 (y sus añadidos), D-053, D-058, D-003, D-052, D-010, D-029 y D-054 en `docs/dev/decisiones.md`; `docs/research/mapa-del-rio/propuesta-mapa-del-rio.md`; `tools/river-map/README.md`; los datos `Assets/_Project/Data/Map/river_map_v1.json` y `river_stops_v1.json` (medidos el 2026-11-06); el código de `Assets/_Project/Scripts/UI/RiverMapScreen*.cs` y `Scripts/Presentation/` (`LandGraphic`, `RiverRibbonGraphic`, `ProceduralArt.MapIcons`, `HomeRiver`); glosario §3 y §8; 07 §6; `docs/dev/memoria.md`; `docs/art/encargo-mapa-del-rio.md`; `docs/art/estilo-personajes-3d.md`.

Convenciones: **dp**, unidad de la interfaz (unos 2,6-3 px por dp en el teléfono de pruebas); **[V]** verificado con fuente; **[E]** estimación o candidato por verificar; **[NV]** no verificado, no se publica; **[P]** propuesta a calibrar.

> **La petición (literal):** «Algo muy importante: mejorar la visualización del río, realmente subirlo de nivel, que se vea mejor, mejorar las orillas, pasar por más ciudades intermedias, tener más participación de Capi, luego se pueden unir más personajes subiendo el río; analicemos y tengamos algo bien bacano y concreto para trabajarlo.»

---

## 0. En una página

![Antes (1.0) y después (maqueta de la 1.1)](../research/mapa-del-rio/v2/lamina-antes-despues.png)

**La maqueta:** `docs/research/mapa-del-rio/v2/maqueta-mapa-v2.png` (1080 × 2340, la pantalla del teléfono; tramo Manaos → Tefé, jugando el 76). La lámina de arriba la pone junto al Mapa de la 1.0. Todo está dibujado por código, con la paleta de la guía visual y el acabado de juguete de D-058, como se haría en el juego (D-052).

**Qué pasa hoy, en tres frases.** El río es una línea de 4-8 dp sobre un verde plano, dentro de una placa que ocupa la mitad de la pantalla. En 32 de los 70 tramos la canoa casi no se mueve (mismo sitio con otra vista), y en otros 9 cruza 400-1.000 km sin nada que ver: los 18 pueblos que ya están en los datos se dibujan como un punto de 9 dp. Capi es una canoa de 40 dp que rema sin reaccionar a nada.

**Qué proponemos para la 1.1: cuatro bloques.**

| Bloque | Qué se ve | Tamaño [E] |
|---|---|---|
| **M2-1 · El río vivo** | Mapa a pantalla completa; río 3-4 veces más ancho, con profundidad, brillo y corriente; **orillas** con playas en las curvas interiores, barrancos rojos en las exteriores y várzea clara; selva en capas; niebla y Andes al fondo; luz propia de cada mundo; primer plano con parallax | 10 días de agente |
| **M2-2 · Pueblos de paso** | Unos **40 pueblos y ciudades** en 1-700 (los 18 de los datos más 22 candidatos), uno cada 3-5 niveles en los tramos largos, cada uno con su viñeta (muelle y palafitos, iglesia, barco de línea, mercado del muelle), su rótulo y, si está verificado, un dato. En los tramos que no se mueven, una **excursión local** | 9 días |
| **M2-3 · Capi al remo** | Canoa de 60 dp (hoy 40), 5 gestos nuevos (saluda, mira, celebra, amarra la muestra, rema fuerte), emociones sin texto, la carga de cuias crece con el Muestrario, momentos de cámara | 6 días |
| **M2-4 · La tripulación** | Animales que **se unen al viaje** río arriba: en la 1.1, el tucán (Alter do Chão, 21) y el tucuxi (Manaos, 51; D-069); los miquitos saludan desde su árbol en su tramo | 4 días |

Total de la 1.1: **unos 29 días de agente** (3-4 semanas con dos carriles de Editor). Memoria: **+6 MiB** de texturas solo con el Mapa abierto (hoy 48 MiB en el Mapa frente a un tope de 120). AAB: **+0,1 MB** (todo por código). En la 1.2: guacamayos, nutrias y hoatzin; la lluvia de la Temporada de lluvias en el Mapa; la noche; el fin de contenido con toda la tripulación; y, si quieres, frases de Capi.

**Lo que hace falta que decidas:** 11 preguntas en la sección 8. Las que más cambian el plan son la P3 (¿Capi habla?, que toca Q3-A de D-013), la P2 (Mapa a pantalla completa) y la P7 (los contratos 29 y 30 pasan a `/2`).

---

## 1. Diagnóstico del Mapa de la 1.0

### 1.1 Lo que se ve

| Captura | Qué muestra |
|---|---|
| ![Viaje en el Tapajós](../art/personajes-3d/piloto/despues/p3d_04_mapa_viaje.png) | Viaje de la victoria en el Tapajós (piloto de D-058) |
| ![Madre de Dios, nivel 306](../art/personajes-3d/fase2/despues/f2_05_mapa_bufeo.png) | Mapa abierto en el 306 (Madre de Dios, con el bufeo; fase 2 de D-058) |

Capturas ya hechas por los tests de capturas de D-058 (no hizo falta lanzar Unity). La de la ficha de Play (`docs/store-listing/graficos-v2/A/es-419/03-mapa-del-rio.png`) muestra la vista Cuenca con «Norte arriba».

### 1.2 Qué se siente plano (medido en el código y en los datos)

1. **La tierra es un solo color.** `LandGraphic` pinta la malla con `SetFlatColor` (#2A7A4B). La malla admite zonas (dosel, várzea, sabana, roca y nieve), pero los datos traen una sola: `land.zones` = `[{id: land, tile: canopy}]` (457 triángulos). No hay luz, relieve, hora del día ni profundidad.
2. **El río es una línea de metro.** `RiverRibbonGraphic` dibuja cintas de **8, 5 y 3 dp** (`WorldWidthClass1Dp`-`3Dp`) con un contorno crema. Sin orillas, sin agua con fondo, sin brillo ni corriente. El agua real que llena el tramo (Q2-A) es la mejor idea del Mapa, y a 8 dp casi no se lee.
3. **Sin orillas.** No hay playas, barrancos, várzea ni vegetación: el agua toca el verde directamente.
4. **El Mapa vive en una placa.** El visor con su marco de 8 dp ocupa **≈ 57 % del alto y ≈ 50 % de la pantalla**; el resto es la cabecera, el Muestrario, el selector y «Jugar».
5. **El ritmo del viaje es irregular.** De los 70 tramos de 1-700 (`river_stops_v1.json`, distancia por `km_home`):
   - **25 tramos de 0 km** (36 %): la misma ciudad con otra vista (`leticia_atardecer`, `yasuni_dosel`, `sandoval_aguajal`, `collpa_guacamayos`, `tambopata_nidos`, `madidi_selva`, `yarinacocha_garzas`…), y **7 de menos de 25 km**. En **32 tramos (46 %)** la canoa casi no se mueve durante 10 niveles.
   - **9 tramos de 400-1.000 km** con poco o nada en medio: Belém → Santarém (733), Flona → Encuentro (597), Anavilhanas → Tefé (613), Tefé → Leticia (962), Río frontera → Iquitos (432), Riberalta → Rurrenabaque (417), Guayaramerín → Noel Kempff (619), Requena → Contamana (656) y Pucallpa → Bolognesi (407).
6. **Los pueblos ya están, pero no se ven.** `river_map_v1.json` trae **18 puntos de paso** (`passing`: Breves, Gurupá, boca del Xingu, Monte Alegre, Óbidos, Parintins, Itacoatiara, boca del Madeira, boca del Purus, Coari, Fonte Boa, Santo Antônio do Içá, Tabatinga, Caballococha, Pebas, boca del Napo, Tamshiyacu y Teotônio), con su nombre en `facts.csv`. Se dibujan como un **punto crema de 9 dp** (`PassingDotDp`) y solo 2 tienen dato (Óbidos y Teotônio). **En los mundos 6-9 (niveles 201-700) no hay ninguno**: 500 niveles sin un pueblo intermedio.
7. **Los frascos tapan el viaje.** Cada parada futura es una mini cuia gris de 20 × 32 dp con su cesta. En el 306 se ven 13 frascos grises y 5 banderas apiladas: se lee como un almacén, no como un río.
8. **Capi apenas está.** Canoa de **40 × 32 dp** (`MapCanoeWidthDp`, el 3,7 % del ancho), con balanceo de ± 4° y 250 ms de anticipación (D-053). Sale en los viajes (el 10, «Ver mapa» del `10k`, el banner de mundo y el fin de contenido) y quieta en los demás. No saluda, no celebra, no reacciona a una ciudad, a un afluente ni al Encuentro de las Aguas. No habla (Q3-A de D-013, a propósito: la voz es de TiTino).

### 1.3 Lo que funciona y se queda

La forma real del río (HydroRIVERS, D-028), el **río de vidrio que se llena** con su agua real (Q2-A), el viaje que hace esperar a «Jugar» hasta que Capi llega (D-053), las banderas de cortesía (Q8-A), el Muestrario, la ventanita de casa de TiTino, la vista Cuenca con «Norte arriba» y el respeto a Reducir movimiento. La v2 se construye **encima** de esto: no cambia el progreso, el guardado (`map_seen_level`), la economía ni la cadena corta de D-008.

---

## 2. Referentes

| Juego | Qué hace | Qué tomamos | Qué no | Fuente |
|---|---|---|---|---|
| **Candy Crush Saga** (King) | Mapa vertical «saga», una zona temática por episodio con su pequeña historia y un personaje que la presenta; los amigos aparecen en el nivel al que llegaron; el arte se reutilizó rotando unos 10 mapas | El camino que sube por la pantalla, **cada tramo con su sello propio** (pueblo, luz, fauna) y un personaje que presenta cada zona | Reciclar el mismo fondo: nuestro río es real y cada tramo es distinto | [V] patente US 9.345.965 (https://image-ppubs.uspto.gov/dirsearch-public/print/downloadPdf/9345965); [E] portafolio del artista del mapa (https://kleeam.artstation.com/projects/L3lDBv); [E] https://www.gamedeveloper.com/design/rethinking-progression-in-mobile-puzzle-games |
| **Royal Match** (Dream Games) | Meta de áreas: cada área se completa y se ve completada; cofre al acabarla | La **sensación de completar**: un tramo recorrido queda bonito y «vivo» (agua real, pueblo encendido) | Que no se puedan ver las áreas ya hechas (crítica de analistas): nuestro río entero sigue a la vista | [E] https://medium.com/@ekinmelissezer/game-analysis-for-royal-match-and-toon-blast-9c4bff8ef48b; [E] https://medium.com/@chrisjameswallace/a-second-swing-at-a-royal-match-sequel-will-this-one-work-4b386eed2580 |
| **Gardenscapes / Homescapes** (Playrix) | Un personaje (Austin) reacciona a cada logro, comenta y celebra en la escena | **Capi reacciona** a lo que pasa en el Mapa (pueblo, afluente, Encuentro, bandera) | Diálogos largos y obligatorios: aquí son gestos y emociones de ≤ 1,2 s | [E] conocimiento del género, sin URL |
| **Toon Blast** (Peak) | Mapa con episodios y el elenco que aparece en el camino | Que **el elenco aparezca en el viaje** y se vaya sumando | Personajes que hablan en cada episodio | [E] https://medium.com/@ekinmelissezer/game-analysis-for-royal-match-and-toon-blast-9c4bff8ef48b |
| **Magic Sort** (el competidor directo) | Escenas de fondo que se completan con el avance | Ya respondido en D-013 con el Muestrario y los fondos por parada; la v2 lo lleva al Mapa | Escenas sin geografía: la nuestra es el río de verdad | [E] `docs/research/magic-sort/propuesta-tintalu.md`, pregunta #16 |
| **Mundos de Mario** (idea original del dueño en D-013, «tipo Mario») | Un camino con vida: cosas que se mueven, hitos con forma propia, el personaje que recorre el camino y celebra | **Hitos con silueta** (cada pueblo se reconoce sin leer), **vida ambiente** (corriente, barcos, aves) y el personaje como centro de la cámara | Enemigos y caminos alternativos | [E] |
| **Monument Valley / Alto's Odyssey** | Profundidad con pocas capas: niebla, degradados y parallax | **Niebla río arriba** («lo que falta») y **primer plano** que se desplaza más rápido | Ritmo contemplativo largo | [E] |

---

## 3. Propuesta (a): el río y las orillas («El río vivo», M2-1)

### 3.1 Capas, de atrás adelante

| # | Capa | Cómo se ve | Cómo se hace (por código, D-052) | Reducir movimiento |
|---|---|---|---|---|
| 0 | **Luz del mundo** | Un degradado cálido o frío desde arriba a la izquierda (la luz de la guía visual) y el tono de la hora del mundo (3.3) | Un `Image` con degradado sobre todo el visor, multiplicado; un color por mundo | Igual |
| 1 | **Tierra con zonas** | Tierra firme verde oscura lejos del agua; **várzea** más clara en una banda de unos 40 dp junto al río; sabana amarilla en los Llanos de Moxos (mundo 8); roca y nieve de los Andes al fondo | `LandGraphic` con las zonas que ya admite (`canopy`, `varzea`, `savanna`, `rock`, `snow`), generadas por `tools/river-map` (hoy solo hay `land`) y color por vértice para el degradado | Igual |
| 2 | **Orillas** | **Playa** de arena en la curva interior de cada meandro (≤ 16 dp de ancho) y **barranco** de tierra roja con estratos en la exterior (≤ 8 dp), como en un meandro real; una línea fina de hierba en el resto | Tramos de orilla calculados por `tools/river-map` desde la curvatura con signo de la línea de la vista Mundo; un `RiverBankGraphic` nuevo (malla con colores por vértice) | Igual |
| 3 | **Agua** | Cinta de **26 dp** en el cauce, **14** en los afluentes y **6** en los ríos locales (hoy 8, 5 y 3); canal profundo más oscuro, reflejo del cielo en la orilla iluminada, destellos, plantas flotantes en los remansos; el **Encuentro de las Aguas** con el Negro y el Solimões lado a lado sin mezclarse; río arriba, el **vidrio azul hielo** que falta llenar (Q2-A, sin cambios) | `RiverRibbonGraphic` con color por vértice (centro × 0,75) y ancho por clase y por zoom; el Encuentro como una franja de agua negra que se funde en unos 150 dp | Igual |
| 4 | **Corriente** | Chevrons claros que bajan con el río (1 cada 60 dp, 12 dp/s), destellos que titilan, la estela de la canoa | Sprites movidos con `PolylineCursor` y la opacidad del `CanvasRenderer`, como `HomeRiver` (sin asignar memoria ni rehacer mallas) | **Quietos** |
| 5 | **Selva en capas** | Copas en racimo (3 tamaños, 3 verdes, con sombra de contacto y brillo de barniz), un 1 % de ipê amarillo o rosa; palmeras de açaí o aguaje a ≤ 30 dp del agua; más denso y oscuro lejos del río | Puntos de vegetación esparcidos de forma determinista por `tools/river-map` (fuera del agua, las orillas y los pueblos) y dibujados como quads de un atlas por código: un `CanopyGraphic` por capa, una llamada de dibujo por atlas | Igual |
| 6 | **Pueblos y paradas** | Sección 4 | | |
| 7 | **Personajes** | Secciones 5 y 6 | | |
| 8 | **Atmósfera** | **Niebla** en el 25 % superior (río arriba: lo que queda por descubrir), con los **Andes** lejanos en azul; dos **sombras de nubes** que derivan | Dos `Image` con degradado y dos manchas suaves | Nubes **quietas** |
| 9 | **Primer plano** | Dos racimos de hojas desenfocadas en las esquinas (abajo a la izquierda, arriba a la derecha), con una heliconia; se desplazan ×1,15 al mover el Mapa (**parallax**) | Dos sprites por código; el desplazamiento se suma al del contenido | Sin parallax |

**Pantalla completa.** El visor pasa a ocupar todo lo que queda entre la cabecera y la barra de abajo (de ≈ 57 % a ≈ 77 % del alto), sin placa ni marco: la cabecera y la barra son de madera y flotan sobre el Mapa (maqueta). Los botones, chips y la ventanita de casa no cambian de tamaño.

**Menos frascos.** Las paradas futuras dejan la mini cuia gris y pasan a una **boya de vidrio** de 14 dp; la mini cuia sale solo en las paradas alcanzadas (con su agua) y en la **siguiente** (con un brillo que late). El cierre de mundo conserva la baliza y «Premio del mundo · faltan N».

### 3.2 Lo que la maqueta demuestra

En la maqueta, el Solimões lleno de agua blanca sube desde el Encuentro de las Aguas (abajo a la derecha, el Negro aún oscuro al lado), pasa por Manacapuru, recibe el Purus (afluente recién pasado, que se llena entero, como hoy) y llega hasta la canoa. Más arriba, el vidrio que falta llenar, Coari como próximo pueblo y Tefé en la niebla, con la baliza del cierre de mundo.

### 3.3 La luz de cada mundo [P]

| Mundo | Hora | Clave de color |
|---|---|---|
| 1 Estuario | Mañana dorada | Cálido suave, agua con reflejo rosado |
| 2 Tapajós | Mediodía claro | Agua verde azulada, playas blancas (Alter do Chão) |
| 3 Manaos | Tarde | Ámbar cálido; el Negro con brillo de té |
| 4 Trapecio | Atardecer | Naranja y lila; luces de los pueblos encendidas |
| 5 Loreto | Alba con neblina | Frío y claro, niebla baja sobre las cochas |
| 6 Oriente | Nublado tropical | Verde intenso, nubes bajas de la ceja de selva |
| 7 Madre de Dios | Mañana con guacamayos | Rojo de la collpa, sol bajo |
| 8 Madidi | Tarde dorada | Llanos amarillos, horizonte ancho |
| 9 Ucayali | Atardecer anaranjado | Agua blanca encendida, primeras estribaciones |
| 10 Alto Negro (si llega en la 1.1) | Crepúsculo azul | Agua negra con reflejo de estrellas |

El cambio de luz al cambiar de mundo va dentro del banner de D-012 (un fundido de 600 ms). Con Reducir movimiento, corte.

---

## 4. Propuesta (b): más pueblos y ciudades («Pueblos de paso», M2-2)

### 4.1 Reglas

1. **Un pueblo de paso no es una parada:** no tiene fondo de tablero, Canasta ni muestra (como los puntos de paso de hoy, propuesta del Mapa §5.2). Las paradas (una cada 10 niveles, Q1-A) no cambian.
2. **Cadencia:** ningún tramo largo pasa más de 5 niveles sin algo que ver. Un tramo de ≥ 150 km lleva al menos 1 pueblo; uno de ≥ 400 km, 2 o 3.
3. **Cuándo se pasa:** al ganar el nivel `10k + ⌈10 · f⌉`, donde `f` es la fracción del tramo (en km) hasta el pueblo. Se deduce de `max_level`: **sin campo nuevo en el guardado**.
4. **Al pasarlo en un viaje:** sale su rótulo y Capi saluda (600 ms, sin parar la canoa ni retrasar «Jugar»). Tocándolo en el Mapa se abre su ficha (nombre, país, río y, si lo hay, su dato), como hoy con los puntos de paso.
5. **Datos:** nombre real verificable; el dato, solo [V] (o [E] revisado; ningún [NV]), ≤ 90 caracteres (D-013, punto 11). Sin dato, sale solo el nombre. Los textos nuevos los revisa Gemini (D-054) y el asesor, si lo hay.
6. **Sin personas** (VA-22 de la guía visual): la vida se ve en los barcos, las luces, el humo de la cocina, las hamacas colgadas en el barco de línea y las canoas amarradas.

### 4.2 Cómo se dibujan: 5 viñetas por código

| Viñeta | Qué lleva | Dónde |
|---|---|---|
| **Muelle y palafitos** | 3 casas de colores sobre pilotes, un muelle de tablas, una canoa amarrada | Pueblos ribereños pequeños (Gurupá, Manacapuru, Caballococha, Pebas…) |
| **Iglesia y plaza** | Iglesia blanca con campanario, casas alrededor | Pueblos con iglesia de fundación antigua (Óbidos, Coari, Fonte Boa…) |
| **Puerto con barco de línea** | El barco de dos pisos blanco y azul («recreio»), con las hamacas | Puertos grandes (Itacoatiara, Parintins, Tabatinga, Porto Velho…) |
| **Mercado del muelle** | Puestos con toldos y cestas de frutas en el muelle, barcos de feria | Ciudades de mercado (Santarém, Iquitos, Pucallpa…). **No «mercado flotante»:** no es una tradición amazónica conocida [E] |
| **Ciudad con hito** | La silueta del héroe que ya existe (Teatro Amazonas, Ver-o-Peso…) | Solo en las paradas que la tienen |

Cada viñeta mide unos 80 × 60 dp y tiene 3 variantes de color; al atardecer se encienden las ventanas. Las malocas y cualquier arquitectura indígena quedan fuera en la 1.1 (glosario §3.3, descartes; pregunta al asesor si se quiere en el futuro).

### 4.3 Qué pueblos

**Ya en los datos (18):** se dibujan con su viñeta en vez del punto de 9 dp. Óbidos tiene dato («el paso más estrecho del Amazonas en Brasil, unos 1,9 km» [V]: IBGE, https://biblioteca.ibge.gov.br/visualizacao/dtb/para/obidos.pdf; Diário do Litoral; las fuentes van de 1,6 a 2,6 km según la época, así que el texto dice «unos 2 km»), y Teotônio el suyo.

**Candidatos nuevos (22) [E]: se publican solo si `tools/river-map` los ancla a ≤ 5 km de su línea y el nombre se verifica en una fuente.**

| Mundo · niveles | Tramo | Candidatos | Notas |
|---|---|---|---|
| 1-2 · 11-20 | Belém → Santarém (733 km) | Almeirim, Prainha | Con Breves, Gurupá y Monte Alegre: 5 pueblos en 10 niveles |
| 3 · 41-50 | Flona → Encuentro | (Óbidos, Parintins, Itacoatiara ya están) | **Parintins:** sin bois ni colores de Garantido o Caprichoso (marcas de asociaciones, glosario §3.3) |
| 3 · 71-80 | Anavilhanas → Tefé (613 km) | Manacapuru, Codajás | Con Coari y la boca del Purus |
| 4 · 81-90 | Tefé → Leticia (962 km) | Amaturá, São Paulo de Olivença | Con Fonte Boa, Santo Antônio do Içá y Tabatinga |
| 5 · 141-150 | Río frontera → Iquitos | Indiana | Con Pebas y la boca del Napo. **Belén**, nunca (D-013, punto 11) |
| 6 · 201-300 | Napo | Cabo Pantoja (Perú), Tiputini y Pañacocha (Ecuador) | Pantoja, el paso de frontera en el Napo |
| 7 · 301-310 | El viaje de mundo por el Madeira | **Porto Velho**, Humaitá, Manicoré, Borba | Porto Velho es la ciudad grande del Madeira (Teotônio está en su municipio). **Boca Colorado, nunca** (minería ilegal de oro) |
| 7 · 331-420 | Madre de Dios | Laberinto | |
| 8 · 421-560 | Beni, Mamoré e Iténez | **Guajará-Mirim** (Brasil, frente a Guayaramerín), Cachuela Esperanza, Costa Marques | |
| 9 · 561-700 | Ucayali | Orellana, Masisea | Atalaya → Puerto Prado: sin candidato fiable [NV] |
| 10 · 701-820 (si entra en la 1.1) | Río Negro | Novo Airão, Barcelos, Santa Isabel do Rio Negro, São Gabriel da Cachoeira, Cucuí, San Carlos de Río Negro | **São Gabriel, con el asesor** (D-013, punto 11) |

Total: **40 pueblos en 1-700** (y 46 con el mundo 10).

### 4.4 Los tramos que no se mueven: «excursión local»

En los 25 tramos de 0 km, la canoa sale de la parada, da una vuelta corta por su lago, caño o cocha y vuelve: la collpa de los guacamayos, el aguajal de Sandoval, el igapó de Cuyabeno, el dosel del Yasuní. El recorrido usa los ríos locales que ya existen (`nanay`, `itaya`, `samiria`, `cuyabeno`, `tambopata`, `manu`…) o, si no hay, un lazo de ≈ 30 dp alrededor de la parada que calcula la herramienta. Las 10 boyas se reparten por el lazo. Con Reducir movimiento, fundido como hoy.

---

## 5. Propuesta (c): Capi protagonista («Capi al remo», M2-3)

### 5.1 La canoa

- **60 × 44 dp** en la vista Mundo (hoy 40 × 32), con la cabeza de Capi 1,4 veces mayor y el acabado de juguete de D-058 (`ToyShading`).
- **La carga crece con el Muestrario:** a la popa, una cuia atada por cada mundo completado (como mucho 5 a la vista), con el agua de su mundo.
- El mástil y la bandera de cortesía, como hoy (Q8-A).

### 5.2 Gestos nuevos (rig por código; bucles cortos)

| Gesto [P] | Cuándo | Duración |
|---|---|---|
| `capi_paddle` (ya existe) | Remando en el viaje | Bucle de 12 f |
| `capi_lookout` · la pata de visera | Anticipación de D-053 (los 250 ms antes de salir) | 250 ms |
| `capi_wave` (ya existe en la Bitácora) | Al pasar un pueblo; al abrir el Mapa sin viaje (1 vez por sesión, mirando al jugador) | 600 ms |
| `capi_paddle_hard` | Rápidos (Teotônio), corriente fuerte, el Pongo | Bucle |
| `capi_cheer_oar` · el remo en alto | Llegada a una parada | 500 ms |
| `capi_tie` · amarra la muestra | Tras la llegada: sube la mini cuia nueva y la ata a la popa | 400 ms |

Los gestos se ven también en la franja de llegada de la victoria (`ArrivalStrip`, 17 dp: allí solo el remo en alto).

### 5.3 Emociones sin texto (recomendado) o frases cortas

Capi **no habla en el Mapa** (Q3-A de D-013: la voz es de TiTino). Se propone que muestre emociones en una burbuja sin texto, 1,2 s, como mucho una por viaje: ♪ remando, ♥ al unirse alguien, ! en el Encuentro o al cambiar el agua, ? ante la niebla del fin de contenido, una cuia al llenar la muestra. No hay que traducir nada. La alternativa (pregunta P3) son frases de ≤ 24 caracteres en 3 idiomas («¡Mira, agua negra!»), que cambiaría Q3-A y T1-A.

### 5.4 Reacciones a lo que pasa

| Evento | Reacción |
|---|---|
| Pasar un pueblo | Saluda; el pueblo enciende una luz |
| Boca de un afluente | Mira el agua nueva que entra (!) |
| Encuentro de las Aguas | Se asoma por la borda a ver los dos colores (!) |
| Cambio de país | Iza la bandera (ya existe) y la saluda |
| Llegada a parada | Remo en alto, amarra la muestra, sonido `sfx_map_sample` (ya existe) |
| Se une alguien | ♥ y le hace sitio en la canoa |
| Rápidos | Rema fuerte; salpicaduras |

### 5.5 Cámara

- **Anticipación:** un acercamiento de 1,00 a 1,12 sobre Capi en los 250 ms de D-053.
- **Viaje:** la cámara sigue a la canoa con un retraso suave (120 ms) y la deja en el tercio inferior, para que se vea lo que viene río arriba.
- **Llegada:** un acercamiento de 1,15 y una vuelta de 300 ms al encuadre.
- **Cambio de mundo:** el travelling largo que ya existe en el banner (≤ 4 s), con un plano final de Capi, la tripulación y la bandera.
- **Tiempo:** ningún momento retrasa «Jugar» más de **400 ms** sobre D-053 (amarrar la muestra), y el toque lo acelera ×2, como hoy.
  - *Nota del 2026-11-08 (añadido de D-069, observación 3 de la prueba de la 1.1):* el viaje de los Mapas con viaje pasa a 450 ms por boya (4,5 s un tramo, entre 3 y 6 s), y el toque acelera ×6 para que con un toque «Jugar» llegue a los 0,75 s de antes (con un toque); sin tocar, llega 3 s más tarde que antes, así que el tope de 400 ms vale para los gestos, no para el viaje.
- **Reducir movimiento:** sin movimientos de cámara: cortes.

---

## 6. Propuesta (d): personajes que se unen («La tripulación», M2-4)

### 6.1 Reglas (para no chocar con el canon)

1. **Solo animales** del elenco (glosario §3.3) o fauna real de ese tramo. **Nunca** personas, seres míticos ni pueblos indígenas (descartes del glosario §3.3).
2. **Sin nombre propio ni voz**, como el bufeo boliviano (Q18-A). Se presentan por su especie.
3. **TiTino no viaja** (D-013): sigue en la ventanita de casa.
4. **Los miquitos no suben a la canoa:** siempre libres y en su árbol (D-010). En su tramo, uno **saluda desde un árbol de la orilla** al pasar (cameo; en la maqueta, el sauim-de-coleira de Manaos).
5. **Sin economía:** no se compran, no dan semillas y no salen en pantallas de pago (como Rosa y TiTino).
6. **Se deducen de `max_level`:** sin campo nuevo en el guardado.
7. **Como mucho 2 a bordo** (más Rosa o el bufeo nadando delante). Al cambiar de mundo, el anterior se queda en su tramo (se le ve en él al volver) y, en la 1.2, pasa a una página «Compañeros de viaje» del Cuaderno de campo.

### 6.2 Quién y dónde

| Compañero | Se une | Cómo va | Por qué ahí | Versión |
|---|---|---|---|---|
| **Tucán** | Alter do Chão (21, mundo 2) | Polizón en la proa, 18 dp; mira hacia delante y abre el pico en las llegadas | Reservado en el elenco (07 §6.3) y común en la región [E] | **1.1** |
| **Tucuxi** (delfín gris, *Sotalia fluviatilis*) | Salida de Manaos (51, mundo 3) | Nada junto a Rosa, 17 dp (el 75 % de Rosa, como en la maqueta); salta en las llegadas y Rosa asoma (D-029). *Corregido por D-069 (respuestas del dueño a las preguntas de M2-4): antes decía 71 y 34 dp, y que saltaban juntos* | Vive en el Amazonas y el Negro [E]; ya salta en las celebraciones (D-029), así que es **el rig de Rosa con otra paleta** | **1.1** |
| **Hoatzin** | Cuyabeno (251, mundo 6) | Posado en la popa, 20 dp; despliega las alas en las llegadas | Ave típica de las lagunas de Cuyabeno [E]. Hoy reservado para Volver a la orilla en el mundo 11: compatible | 1.2 |
| **Pareja de guacamayos** | Collpa de Tambopata (361, mundo 7) | Vuelan en círculos sobre la canoa, 14 dp | La collpa es su sitio (parada 36) [E] | 1.2 |
| **Familia de nutrias gigantes** | Cochas del Manu (411, mundo 7) | Nadan alrededor de la canoa, 3 de 16 dp | Cocha Salvador del Manu [E]; ya están en el elenco (la Crecida) | 1.2 |
| Bufeo boliviano | Mundos 7 y 8 (ya existe, Q18-A) | Sin cambios | | Ya en la 1.0 |

**Cómo se presentan:** la primera vez que se abre el Mapa tras pasar el punto, el compañero llega (nadando, volando o saltando a la proa) y se acomoda; sale el chip «¡Se une al viaje! · {especie}» 2 s (en la maqueta, el tucuxi) y Capi hace ♥. Con Reducir movimiento: aparece en su sitio y sale el chip. TalkBack: «Se une al viaje: {especie}».

### 6.3 Momentos especiales

| Momento | Qué pasa | Duración extra |
|---|---|---|
| **Pasar un pueblo** | Rótulo, Capi saluda, luz en una ventana | 0 (sin parar) |
| **Llegada a una parada** (victoria del `10k` y Mapa con viaje) | Capi con el remo en alto, la tripulación salta o aletea, el pueblo se enciende, Capi amarra la muestra | ≤ 400 ms sobre D-053 |
| **Cambio de mundo** (banner de D-012) | Cambia la luz del mundo; plano final de Capi, la tripulación y la bandera | Dentro de los 4 s actuales |
| **Cierre de mundo** («castillo») | La baliza se enciende y se iluminan las boyas del mundo | Dentro de la llegada |
| **Fin de contenido** (700) | La canoa con toda la tripulación se detiene en la niebla al pie de los Andes; Rosa asoma; «El río sigue creciendo» (06 §9) | 1.2 |
| **Temporada de lluvias** (evento de la 1.2, 06) | Lluvia en el Mapa, el río algo más ancho y las playas cubiertas (en vaciante vuelven). Nunca «seco» (riesgo R3 de D-013) | 1.2 |
| **Noche** (1.2) | Luciérnagas y ventanas encendidas si el jugador abre el Mapa de noche (hora local) | 1.2 |

---

## 7. Qué entra en la 1.1 y qué en la 1.2

### 7.1 Fases

| Fase | Contenido | Agente [E] | Dependencias |
|---|---|---|---|
| **1.1 · M2-1 El río vivo** | Capas 0-5, 8 y 9 de 3.1; pantalla completa; boyas en vez de frascos futuros; luz de 9 mundos | Herramientas 2,5 d + Unity 6 d + QA 1,5 d = **10 d** | P2, P7 |
| **1.1 · M2-2 Pueblos de paso** | 5 viñetas; 18 + 22 pueblos; fichas; excursión local de 25 tramos | Datos e investigación 3 d + herramientas 2 d + Unity 4 d = **9 d** | P4, P5; Gemini (D-054) |
| **1.1 · M2-3 Capi al remo** | Canoa de 60 dp, 5 gestos, emociones, carga, cámara | **6 d** | P3 |
| **1.1 · M2-4 La tripulación** | Marco de compañeros; tucán y tucuxi; cameos de los miquitos | **4 d** | P6 |
| **1.1, total** | | **≈ 29 d** (3-4 semanas con 2 carriles de Editor, regla dura 7) | |
| 1.2 · Compañeros | Hoatzin, guacamayos, nutrias; página del Cuaderno | 4 d | |
| 1.2 · Clima y noche | Temporada de lluvias en el Mapa, noche y luciérnagas | 5 d | Evento de 06 |
| 1.2 · Fin de contenido | El cuadro final con la tripulación | 2 d | |
| 1.2 · Frases de Capi (si P3 = b) | ≤ 30 frases en 3 idiomas | 2 d + textos | P3 |
| 1.2 · Arte de artista (opcional) | 5 viñetas de ciudad hito en PNG (sección 7.4) | Integrar: 1 d | P9 |

### 7.2 Rendimiento, memoria y tamaño

- **Llamadas de dibujo:** +8-10 en el Mapa (tierra, orillas, 2 de agua, 2 de vegetación, pueblos, niebla, primer plano), con un atlas por capa. La vegetación se limita a lo visible: ≤ 600 copas en pantalla = ≤ 2.400 vértices.
- **Animación sin asignar memoria:** la corriente, los destellos y las nubes mueven rectángulos guardados y la opacidad del `CanvasRenderer`, como `HomeRiver`; las mallas solo se rehacen al cambiar el zoom.
- **Objetivo:** 60 fps en el teléfono de pruebas y ≥ 30 fps en gama baja, medidos con la sonda `AF_PERF` antes de cerrar M2-1.
- **Memoria (`docs/dev/memoria.md`):** atlas de vegetación de 1024 × 1024 RGBA32 (4 MiB) y de pueblos de 1024 × 512 (2 MiB), a la densidad de la pantalla: **+6 MiB** solo con el Mapa abierto; se sueltan al cerrarlo, como hoy (0,4 MiB residentes). El Mapa pasaría de unos 48 a unos 54 MiB de texturas, frente a un tope de 120 MiB (08 §5.4). No toca el problema abierto de `Unknown` (~210 MB, §6 de memoria.md). Heap: +~150 KB de datos.
- **AAB:** por código, solo datos: **≈ +0,1 MB** comprimido. Con las 5 viñetas de artista, unos +1,5 MB. Lejos de la compuerta de 80 MB.

### 7.3 Accesibilidad

- **Reducir movimiento:** corriente, nubes, parallax y cámara quietos; los viajes hacen el fundido de hoy; los gestos y las emociones se ven quietos 1,2 s.
- **TalkBack:** cada pueblo con su etiqueta («Coari · pueblo de paso · Brasil»), con zona táctil de 48 dp; el chip de «se une» se anuncia.
- **Contraste:** los rótulos de los pueblos, crema sobre madera o tinta sobre crema (pares permitidos de la guía visual). El Encuentro se distingue por luminosidad, no por color.

### 7.4 Arte: por código, con un encargo opcional

Todo lo de la 1.1 se hace **por código** (D-052, que dice que el arte por código es el final). Si el dueño quiere un acabado de ilustración en las ciudades grandes, el encargo para el artista 2D freelance, que los agentes solo integran:

> **Brief: 5 viñetas de ciudad hito para el Mapa del río.** Belém (Ver-o-Peso), Santarém (la orla con los barcos), Manaos (Teatro Amazonas y el puerto), Leticia y Tabatinga (el muelle de la frontera) e Iquitos (el malecón, **sin Belén**). Cada una de 512 × 384 px PNG con alfa, vista cenital a 3/4 con la luz arriba a la izquierda, estilo D-003 («Selva brillante premium»: vidrio, madera y luz de juego; nada de papel ni acuarela) y el acabado de juguete de D-058; **sin personas** (VA-22); con 3 ventanas que se puedan encender (capa aparte). Referencias: la maqueta `docs/research/mapa-del-rio/v2/maqueta-mapa-v2.png` y fotos de cada sitio, sin calcar. Ranuras `map_town_<id>` en `ArtSlots`; si no hay PNG, sale la viñeta por código. [E] 1,5 días de artista por viñeta.

---

## 8. Preguntas para el dueño

| # | Pregunta | Opciones | Recomendada |
|---|---|---|---|
| **P1** | ¿Qué entra en la 1.1? | (a) los 4 bloques M2-1 a M2-4 (≈ 29 d); (b) solo el río y los pueblos (M2-1 y M2-2, ≈ 19 d); (c) solo el río | **(a)**: la petición son las cuatro cosas, y la tripulación de la 1.1 es barata (dos rigs que ya existen casi) |
| **P2** | ¿El Mapa a pantalla completa, sin la placa ni el marco? | (a) sí; (b) no, la placa como hoy | **(a)**: gana un 35 % de Mapa a la vista y es lo que más cambia la impresión |
| **P3** | ¿Capi habla en el Mapa? | (a) emociones sin texto en una burbuja, sin cambiar Q3-A; (b) frases de ≤ 24 caracteres (cambia Q3-A de D-013 y matiza T1-A: la voz deja de ser solo de TiTino; 3 idiomas) | **(a)** en la 1.1; (b) se puede probar en la 1.2 detrás de una clave |
| **P4** | ¿Los 22 pueblos candidatos? | (a) entran los que `tools/river-map` ancle a ≤ 5 km y tengan el nombre verificado; el dato solo si es [V] y revisado por Gemini (D-054); sin dato, solo el nombre; (b) esperar al asesor humano | **(a)** |
| **P5** | ¿Qué se hace en los 25 tramos de 0 km? | (a) la excursión local; (b) nada, como hoy; (c) fusionar paradas (cambiaría Q1-A: una parada cada 10 niveles) | **(a)**: no toca el progreso ni Q1-A |
| **P6** | ¿Qué compañeros en la 1.1? | (a) tucán y tucuxi, y los cameos de los miquitos; (b) los cinco; (c) ninguno | **(a)**: el resto, en la 1.2 |
| **P7** | ¿Cómo crecen los datos? | (a) contratos 29 y 30 en versión `/2` (`af-river-map/2` y `af-river-stops/2`), regenerados por la herramienta y leídos solo en `/2`; (b) campos opcionales dentro de `/1` | **(a)**: más limpio de validar con `jsonschema` y de revisar |
| **P8** | ¿Claves de Remote Config? | (a) `ui_map_v2` (`on` por defecto, con respaldo al Mapa de la 1.0), `ui_map_crew` y `ui_map_towns`, en la tabla de 08 §9; (b) sin claves | **(a)**: deja apagarlo en gama baja si la sonda lo pide |
| **P9** | ¿Arte de artista para las ciudades hito? | (a) no: todo por código en la 1.1 (D-052); (b) encargar las 5 viñetas del brief de 7.4 | **(a)**; (b) solo si las capturas de la ficha lo piden |
| **P10** | ¿El mundo 10 (Alto Negro, 701-820) llega en la 1.1? | (a) sí, con sus 6 pueblos del río Negro; (b) no | Depende del plan de contenido de la 1.1. Si llega, **(a)**, con São Gabriel revisado por el asesor |
| **P11** | ¿Eventos de analítica nuevos? | (a) ninguno en la 1.1: el paso por un pueblo y la llegada de un compañero se deducen de `af_stop_arrive` y `max_level`; (b) `af_map_town_pass` y `af_map_crew_join` | **(a)**: el catálogo de 08 §8 no crece |

---

## 9. Detalle técnico

### 9.1 Datos (contratos 29 y 30; P7)

`river_map` (`af-river-map/2`), campos nuevos:
- `land.zones`: `canopy`, `varzea` (buffer de la línea en la vista Mundo [P] 40 dp), `savanna`, `rock` y `snow`, con un color por vértice para el degradado de la tierra firme.
- `banks`: por línea, tramos `{line, s0, s1, side, kind: beach|bluff, w}`, con la curvatura de la línea simplificada de la vista Mundo (umbral [P]).
- `veg`: puntos de vegetación cuantizados (uint16, como `pts`) por celda, con su tipo (copa, palmera, flor) y tamaño; determinista (semilla por celda).
- `passing[]`: añade `vignette` (`stilts`, `church`, `port`, `market`), `country`, `tier` (pueblo o ciudad) y `fact`.

`river_stops` (`af-river-stops/2`):
- `leg.via` crece con los pueblos nuevos y añade la fracción `f` de cada uno.
- `leg.excursion`: la ruta local de los tramos de 0 km.
- `crew`: `[{id, species, joins_at_stop, mode: bow|stern|swim|fly}]` [P] (o una tabla aparte, `river_crew_v1.json`).

### 9.2 Herramientas (`tools/river-map`, Python 3.14, solo biblioteca estándar; `jsonschema` solo en las pruebas, D-023 punto 32)

- `geometry.py`: las orillas por curvatura, la zona de várzea, la dispersión de la vegetación (Poisson determinista, fuera del agua, las orillas y los pueblos), las excursiones; `--check` byte a byte, como hoy.
- `stops.py`: los pueblos nuevos desde `rio-amazonas.json` y `sources.json`, con el anclaje a ≤ 5 km (como `CONFLUENCE_KM`), la regla de cadencia del 4.1 (falla si un tramo ≥ 150 km queda sin pueblo y sin excepción anotada) y los vetos (Belén y Boca Colorado).
- Pruebas: `test_geometry.py`, `test_stops.py` y `test_schemas.py` con los esquemas `/2`. Comandos de `CLAUDE.md` sin cambios.

### 9.3 Cliente (Unity; sin escenas, prefabs ni YAML a mano; sin paquetes ni shaders nuevos)

- `LandGraphic`: zonas con color y degradado (ya admite zonas).
- `RiverRibbonGraphic`: ancho por clase y zoom, color por vértice (canal profundo y reflejo) y la franja del Encuentro.
- Nuevos: `RiverBankGraphic`, `CanopyGraphic` (quads de un atlas), `RiverFlowFx` (corriente y destellos, como `HomeRiver`), `MapAtmosphere` (luz del mundo, niebla, nubes y primer plano), `ProceduralArt.Towns.cs` (las 5 viñetas), `MapCrew` y los gestos nuevos de Capi en `ProceduralArt.MapIcons` y su rig.
- `RiverMapScreen`: pantalla completa (cambian `PlateFit` y el marco), las boyas de las paradas futuras, la cámara y los pueblos en `Meta` (hoy `BuildPassing`). Los archivos ya son grandes (`Markers`, 3.600 líneas): todo lo nuevo, en parciales nuevos.
- Tests: EditMode (geometría de orillas, cadencia, compañeros deducidos de `max_level`, Reducir movimiento) y PlayMode con capturas de antes y después en 3 tramos (Estuario, Encuentro y Madre de Dios) a 320, 360 y 412 dp.
- Sondas: `AF_LAYOUT river_map` con los pueblos tocables; `AF_STATE map_journey` con `towns_passed` y `crew_joined`.

### 9.4 Lo que cambia del canon (si se aprueba)

| Qué | Dónde | Cambio |
|---|---|---|
| Canoa de 32-40 dp | Propuesta del Mapa §12, pieza 7; 07 §6.1 (`capi_paddle`) | 60 dp en la vista Mundo; gestos nuevos |
| Capi no reacciona | D-013, punto 6 | Reacciona con gestos y emociones (sin texto si P3 = a) |
| Puntos de paso de 9 dp | Propuesta del Mapa §5.2; 06 §4.5 | Pueblos con viñeta; 22 nuevos |
| Mini cuia en cada parada futura | Propuesta del Mapa §7 | Boya; mini cuia en las alcanzadas y la siguiente |
| Tierra de un color, ríos de 8/5/3 dp | Propuesta del Mapa §3.3 y §11.1 | Zonas, orillas, ríos de 26/14/6 dp |
| Contratos 29 y 30 | GDD.md §7.4; 08 §4.5 | Versión `/2` (P7) |
| Elenco | Glosario §3.3; 07 §6.3 | Tucán, tucuxi, hoatzin, guacamayos y nutrias como compañeros del Mapa |
| Remote Config | 08 §9 | 3 claves (P8) |

### 9.5 Riesgos

- **Ruido visual.** Con 40 pueblos, rótulos y compañeros, el Mapa se puede llenar. Mitigación: los rótulos solo de la parada siguiente, la última alcanzada y los pueblos a ≤ 1 tramo de la canoa; el resto, viñeta sin rótulo; un nivel de detalle por zoom.
- **Gama baja.** Si la sonda da < 30 fps, `ui_map_v2` permite volver al Mapa de la 1.0 y la vegetación tiene una densidad de respaldo (la mitad).
- **Cultural.** Parintins sin bois; Boca Colorado y Belén fuera; São Gabriel con el asesor; nada de malocas en la 1.1; el lago de Tefé, sin botos ni orillas secas (Rosa nada en el cauce, nunca en el lago).
- **Datos sin verificar.** Los 22 candidatos son [E]; un pueblo que no se verifica no entra.

---

## 10. La maqueta

- `docs/research/mapa-del-rio/v2/maqueta-mapa-v2.png`: la pantalla, 1080 × 2340.
- `docs/research/mapa-del-rio/v2/maqueta-mapa-v2.svg`: la misma, en vectorial (sin las fuentes del juego).
- `docs/research/mapa-del-rio/v2/maqueta-mapa-v2.html`: con las fuentes del juego (Baloo 2 y Nunito) incrustadas.
- `docs/research/mapa-del-rio/v2/lamina-antes-despues.html` y `.png`: la captura de la 1.0 junto a la maqueta, con las 9 llamadas numeradas.
- `docs/research/mapa-del-rio/v2/generar_maqueta_v2.py`: el generador (Python 3.14, solo biblioteca estándar): `py -3.14 -I docs/research/mapa-del-rio/v2/generar_maqueta_v2.py`, y después Chrome headless para los PNG (cabecera del script).

Es una maqueta de dirección de arte: la geometría del río es estilizada (no sale de `river_map_v1.json`), y Manacapuru y Coari salen como ejemplo de pueblo de paso (Coari ya está en los datos; Manacapuru es candidato [E]).
