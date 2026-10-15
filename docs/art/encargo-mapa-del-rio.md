# Encargo de arte: fondos por parada y Mapa del río

Fecha: 2026-10-14 · Autor: documentación (agente; tanda 4, tarea M1.8) · Para: el dueño, que lo envía al artista 2D freelance, y el asesor cultural · Estado: **listo para enviar en el orden de la sección 2**, con dos preguntas para el dueño antes de los pasos 2 y 3 (sección 8); ninguna parte sale antes de su condición.

Fuentes:
- `docs/dev/decisiones.md`: D-013 (Mapa del río: Q5-A, Q8-A, Q14-A, Q18-A, punto 11, «Consecuencias» y «Dependencias externas») y D-023 (puntos 12 y 16);
- glosario §3.2 y §11 (nombres de las ranuras);
- 07 §2.1 (tabla de paradas y fondos), §6.1, §8.1, §8.2 y §11; 08 §5.4 y §6.2;
- `docs/art/direccion-visual-v2.md` §7, §9 (VA-20 a VA-23) y §10.3;
- `docs/research/mapa-del-rio/propuesta-mapa-del-rio.md` §5.3, §10.2, §10.3, §10.6, §10.7, §10.8, §11.3 y §12;
- CLAUDE.md, «Humanos».

La medida, el uso, el respaldo y la compresión de cada ranura están en `Assets/_Project/Art/Generated/README.md` («Ranuras reservadas»). Este documento dice qué se pinta, en qué orden y con qué condiciones.

Convenciones: **dp**, unidad del lienzo de UI (3 px por dp en el teléfono de pruebas); **[P]**, propuesta pendiente de calibrar; **[E]**, estimación.

## 0. En pocas palabras

1. **Qué:** el kit de cada mundo, los héroes de los sitios, los sprites de ambiente y las piezas del Mapa del río.
2. **Quién:**
   - el artista pinta todo el arte nuevo;
   - los agentes solo lo integran;
   - el asesor cultural revisa antes de pintar los sitios marcados y antes de integrar;
   - el dueño envía el encargo y aprueba.
3. **Cuándo:**
   - los 3 primeros héroes, después de VA-20 en el teléfono, al cerrar la Ola 2. La selección es una propuesta [P] que decide el dueño (sección 8);
   - los kits, en paralelo con los héroes en cuanto la estimación de peso (tarea B0) esté lista, **sin esperar a una medida del AAB**, como pide el plan de la tanda 4. Esto se aparta de D-013, y el dueño tiene que confirmarlo (sección 8). El de Ucayali (`bg_kit_w09`) espera además a la respuesta del asesor sobre Shanay-Timpishka (sección 5);
   - los otros héroes, después de medir el AAB con los 3 primeros (D-013);
   - las piezas del Mapa, al final.

## 1. Reglas para todas las piezas

- **Estilo:** D-003, «Selva brillante premium»: vidrio, madera y luz de juego, con la luz desde arriba a la izquierda. Se pinta con la clave de estilo de `docs/art/prompts-chatgpt.md` (sección 1). **Nada de papel, pergamino ni acuarela.**
- **Referencias:** varias fotos de cada sitio, sin calcar ninguna, y las que dé el asesor.
- **Lo que nunca aparece:**
  - la lista «Evitar» de la guía visual (§7) y la columna «No» de 07 §8.1. La fila «Lugar equivocado» de la guía se lee así:
    - la «playa» vetada es la de mar, con cocoteros y turquesa caribeño. Las playas de río y las islas de arena sí van donde las pide el canon: en los kits (3.2; 07 §2.1, capa 0b), en la Ilha do Amor de Alter do Chão (07 §2.1), en las islas de Anavilhanas (propuesta del Mapa §10.3) y en la arena blanca del varillal de Allpahuayo-Mishana (07 §2.1). Pacaya-Samiria al alba va sin playas al descubierto (07 §2.1);
    - los cerros y las montañas solo van donde los nombra el canon: son los relieves del repertorio de cada mundo en 07 §2.1 (con D-013, la columna «Paisaje» es ese repertorio; «Fondos de la 1.0») y de su tabla de paradas. Por ejemplo, el cerro de selva de Alter do Chão; los tepuyes de Chiribiquete, solo como silueta lejana en el Trapecio (07 §2.1 y §8.1); en el kit de Madidi (`bg_kit_w08`; 07 §2.1, fila 8), los Andes al fondo de la selva, la meseta de Huanchaca y el escarpe de la cascada Arcoiris de Noel Kempff; y en el de Ucayali (`bg_kit_w09`; 07 §2.1, fila 9), el Pongo de Manseriche;
  - **en los fondos** (VA-22):
    - personas;
    - canastos, que son la Canasta;
    - frutas sueltas;
    - banderas y cuias: las banderas y las mini cuias son del Mapa;
    - canoas a menos de 48 dp del HUD: la canoa es el icono de Canoas;
  - sumaúma o lupuna, que son sagradas;
  - cocoteros y mar azul caribeño.
- **Legibilidad del tablero:**
  - VA-03, escena brillante;
  - VA-21, calma junto al panel: en los 16 dp sobre su borde superior, ningún detalle con ΔL* > 30;
  - VA-23, agua coherente: el agua del primer plano la pinta el código con el preset del mundo, y el arte no la pinta.
- **Revisión de cada pieza:** la lista de 07 §8.2:
  1. ¿hay personas o símbolos?
  2. ¿la decoración es propia?
  3. ¿el lugar es de la cuenca o lleva rótulo?
  4. ¿el texto usa palabras de la lista permitida?
  5. ¿lo vio el asesor?

  Todo nace «pendiente del asesor».

## 2. Orden del encargo y condiciones

| Paso | Qué | Condición para enviarlo | Quién | Qué pasa después |
|---|---|---|---|---|
| 1 | **VA-20 en el teléfono.** No se encarga nada: es la prueba | Al cerrar la Ola 2, con la hoja de las 20 composiciones (`02f_board_stops_sheet.png`, tarea M2.4) y el héroe provisional geométrico en las capturas `02_board_l7` (2 filas) y `02b_board_l1` (1 fila) | Dueño y asesor | Si un sitio no pasa, se cambia su composición antes de encargarlo |
| 2 | **Los 3 primeros héroes** [P]: `bg_hero_ver_o_peso`, `bg_hero_teatro_amazonas` y `bg_hero_leticia` (3.3). La selección la decide el dueño (sección 8) | VA-20 aprobado | Dueño → artista | La tarea M5.2 los importa y mide el AAB, el 16 KB y el arranque frente a la estimación de B0 |
| 3 | **Los kits:** `bg_kit_w01` … `bg_kit_w05` (1-200) y `bg_kit_w06` … `bg_kit_w09` (201-700, Q14-A), con los sprites de ambiente (3.4) | En paralelo con los héroes, en cuanto la tarea B0 (Ola 3) dé la estimación de peso, **sin esperar a ninguna medida del AAB**, como pide el plan de la tanda 4 (tareas M1.8 y B0). **Se aparta de D-013**, que mide el AAB tras integrar los 3 primeros sitios, «antes de encargar el resto» («Consecuencias»). Hasta que el dueño lo confirme y se registre, es una pregunta abierta (sección 8); sin su confirmación, los kits esperan a la medida de M5.2, como el paso 3 bis. `bg_kit_w09` espera además a que el asesor responda sobre Shanay-Timpishka (sección 5, punto 5) | Dueño → artista | La tarea M5.3 los integra con las piezas de `bg.pieces` y el ambiente |
| 3 bis | **Los otros 11 héroes y la reserva** (3.3) | Cuando los 3 primeros pasen la revisión del dueño y del asesor, y **tras la medida del AAB de M5.2 con los 3 primeros sitios** (D-013, «Consecuencias»; propuesta del Mapa §10.7 y §12) | Dueño → artista | Integración por sitio |
| 4 | **Las piezas del Mapa** (sección 4), con la **máscara de zonas** para la geometría de producción (tarea M5.1) | Después de los kits. La casa de TiTino espera además al visto bueno del asesor sobre la embaúba; las banderas, que son las oficiales, pasan por el abogado y el asesor antes de usarse en release (sección 4) | Dueño → artista | M5.1 vectoriza la máscara; M5.2 importa el arte del Mapa y hornea el atlas de siluetas |

## 3. Fondos por parada

### 3.1 Qué pinta el artista en cada fondo

El fondo de cada parada se compone por capas (07 §2.1; propuesta del Mapa §10.2):
- **por código:** el cielo con la hora del mundo, las líneas entre aguas de Santarém y del Encuentro, la banda de río y la LUT;
- **del artista:**
  - el **kit del mundo** (capa 0b);
  - el **héroe del sitio** (capa 0c);
  - los **sprites de ambiente** (capa 1);
  - el **segundo juego de follaje** (3.5).

El muelle y el follaje de Brasil ya son del canon. La misma composición sirve al tablero, al inicio, al banner, a la victoria con vista y a la tarjeta del Mapa.

### 3.2 El kit del mundo

- **Entrega:** un atlas de **2048 × 2048** (a 2×) con 8-10 piezas sobre fondo transparente:
  - la orilla lejana en tiras que se repiten sin costura;
  - la selva y las palmeras del mundo (açaí en Brasil, aguaje en Loreto);
  - islas y playas de río;
  - árboles con el pie en el agua;
  - barcos lejanos sin personas;
  - una silueta de ciudad genérica.

  La posición de cada pieza la anota quien integra el kit en `bg.pieces` de `river_stops_v1.json` (D-023, punto 12).
- **El paisaje de cada mundo** sale del repertorio y de la hora de 07 §2.1 (Estuario al alba con bruma baja, Tapajós a mediodía, Manaos con luz rasante de tarde, Trapecio en una mañana nublada tras la lluvia, Loreto al atardecer, y así hasta Ucayali). El kit de Ucayali espera además a la respuesta del asesor sobre Shanay-Timpishka (sección 5).
- **Dos paradas de 1-200 van solo con el kit**, así que sus kits llevan esas piezas:
  - el río frontera (parada 13, `kit:rio_ancho_con_baliza`, en `bg_kit_w04`): el Amazonas ancho entre dos orillas de várzea, con una baliza de navegación;
  - Nauta (parada 17, `kit:orilla_de_nauta`, en `bg_kit_w05`): el río muy ancho con Nauta lejana. Las dos aguas son blancas: no se inventan dos colores.
- **En 201-700** el kit es todo el fondo, sin héroe; el sitio lo dicen el rótulo, la tarjeta y el Mapa (D-013, Q14-A).
- **Compresión:** ASTC 8×8 en el teléfono. Es lejanía con bruma, sin detalle fino que se pierda.

### 3.3 Los héroes del sitio

- **Entrega:** **1440 × 480** (2× de 240 × 80 dp), con capas, sobre fondo transparente.
- **Encaje** (propuesta del Mapa §10.2 y §10.6):
  - el pie, en la línea del horizonte, a 48 dp del borde de arriba;
  - por encima, hasta 48 dp de silueta, con lo que hace reconocible el sitio en sus 30 dp de abajo: es lo que se ve con 2 filas. Una silueta baja y ancha, nunca la fachada entera de un edificio;
  - por debajo, hasta 32 dp de orilla. El reflejo lo alarga el código con el mismo sprite volteado, y el agua la pinta el código.
- **Compresión:** ASTC 6×6. Es el detalle reconocible.

**Los 3 primeros [P]** (paso 2). El canon solo dice «empezando por los 3 primeros sitios», sin nombrarlos (07 §2.1, «Legibilidad»; guía visual, VA-20; propuesta del Mapa §10.7 y §12).
- **Propuesta:** un sitio por cada mundo con fondo en el AAB y héroe de ciudad: 1 Estuario, 3 Manaos y 4 Trapecio. Son tres hitos propios en tres mundos, con tres luces distintas (alba, tarde y mañana nublada tras la lluvia).
- **Alternativa:** el orden de la ruta, con los tres primeros sitios pintados: Ver-o-Peso, Alter do Chão y Flona Tapajós (paradas 0, 2 y 3; la 1, Santarém, es un héroe por código).
- **Lo decide el dueño** (sección 8). Lo que se reconoce y lo que hay que evitar en cada sitio está en la tabla de paradas de 07 §2.1:

| Ranura | Parada | Se reconoce por | Cuidado |
|---|---|---|---|
| `bg_hero_ver_o_peso` | 0, Belém (niveles 1-10). Mundo 1, agua blanca, alba dorada con bruma baja | Las torres del mercado de hierro (1901) desde la bahía de Guajará, con palmeras de açaí. Silueta de dos agujas sobre un rectángulo | Sin canastos, sin el Círio, sin personas y sin los objetos rituales de las bancas de hierbas |
| `bg_hero_teatro_amazonas` | 5, Manaos (niveles 51-60). Mundo 3, agua negra, tarde con luz rasante | La cúpula del Teatro Amazonas sobre la ciudad, en silueta baja, con el puerto flotante y el puente del río Negro al fondo. Silueta de un semicírculo con tambor | Sin nostalgia del caucho |
| `bg_hero_leticia` | 8 y 9, Leticia (niveles 81-100). Mundo 4, agua blanca, mañana nublada tras la lluvia; la parada 9 lo reutiliza con la LUT de atardecer y la bandada de pericos | El malecón y el río ancho, con la orilla de Tabatinga al fondo | Ni la isla Santa Rosa ni islas con bandera; sin personas |

**Los otros 11 y la reserva** (paso 3 bis): `bg_hero_alter_do_chao`, `bg_hero_flona_tapajos`, `bg_hero_anavilhanas`, `bg_hero_lago_tefe`, `bg_hero_amacayacu`, `bg_hero_puerto_narino`, `bg_hero_tarapoto`, `bg_hero_iquitos`, `bg_hero_itaya_creciente`, `bg_hero_allpahuayo` y `bg_hero_pacaya_samiria`, más la reserva `bg_hero_casa_de_fierro`.
- Lo que se reconoce y lo que hay que evitar en cada uno está en la tabla de paradas de 07 §2.1.
- Cuatro de ellos piden antes al asesor (sección 5).
- Ninguna ranura se llama `belen`.

### 3.4 Los sprites de ambiente

`amb_parakeets` (los pericos de Leticia), `amb_dolphin_back` (los lomos de delfín rosado y gris de Tarapoto) y `amb_mist` (la bruma de Pacaya-Samiria).
- **Medida:** 256 × 256.
- **Animación:** por código, con 16 partículas como mucho.
- **Cuándo:** van con los kits, porque los integra la misma tarea.

### 3.5 El segundo juego de follaje

El follaje de esquina de la Amazonia occidental, con aguaje: 4 piezas de 480 × 510 como mucho (propuesta del Mapa §12, n.º 4). El açaí solo va en el juego de Brasil, que es el del canon.
- **Pendiente:** el glosario §11 aún no le da nombre de ranura (la guía visual lo llama `foliage_<juego>_*`, 10.3).
- **Cuándo:** se encarga con los kits en cuanto tenga nombre (sección 8).

## 4. Piezas del Mapa del río (paso 4)

| Pieza | Ranura | Entrega | Cantidad | Notas |
|---|---|---|---|---|
| Texturas de terreno | `map_tile_canopy`, `map_tile_varzea`, `map_tile_savanna`, `map_tile_rock` y `map_tile_snow` | 512 × 512, sin costuras | 5 | Brillantes, de juego; nada de papel |
| **Máscara de zonas del terreno** | Ninguna: es un dato de `tools/river-map` | PNG o SVG con las zonas (dosel, várzea, sabana, roca y nieve), pintado sobre la guía de proyección de `tools/river-map` | 1 | Natural Earth no trae esas zonas: las dibuja el artista y la herramienta las vectoriza para la geometría de producción (tarea M5.1). Hace falta la guía antes de encargarla (sección 8) |
| Canoa de Capi remando | `map_canoe` (rig de Capi) | Bucle de 12 fotogramas a 32-40 dp | 1 | Del bucle de `capi_rescue`. Cuias atadas con cuerda, sin cestas (serían la Canasta); mástil con anclaje para la bandera; sin personas. En el 961, la canoa queda amarrada (07 §6.1) |
| Rosa en el Mapa | Poses del canon reducidas, más `rosa_farewell` [P] | Rig de Rosa | +1 pose y +1 paleta | La despedida del 961. Con Q18-A, la paleta del bufeo boliviano: el mismo rig, sin nombre propio (sección 5) |
| Mini cuia y su etiqueta | `map_minicuia` | 96 × 160, vidrio en capas | 1 | La silueta de D-006, **nunca un tubo**. El líquido lo pinta el código, y las siluetas de los sitios se hornean en el atlas |
| Repisa del Muestrario | `map_rack` | 9 cortes | 1 | Madera tallada y repisa (guía visual §4.4 y §4.5) |
| Casa de TiTino | `map_home` y `map_home_window` | 256 × 256 y 9 cortes | 2 | La embaúba a escala de Mapa (propuesta de TiTino, c.7). La ventanita sirve también en el inicio (Q17-A). **Lleva la condición de TiTino:** se encarga cuando el asesor confirme la embaúba (`docs/art/encargo-titino-frutas-tinaja.md`), con la alternativa preparada por si la desaconseja |
| Banderas | `map_flag_<iso2>` | 96 × 64, con 2 fotogramas | 3 en 1-200 (BR, CO y PE), 5 en 1-700 (más EC y BO) y 7 en 1-1.000 | **Solo banderas oficiales**: Bolivia, con la tricolor [P]. Nunca en tierra, en islas ni en zonas reclamadas. El uso de símbolos nacionales lo revisan el abogado y el asesor antes de usarlas en release (D-013, Q8-A; propuesta del Mapa §5.3) |
| Rosa de los vientos, placas, boya, baliza y «?» | `map_compass`, `map_plaque`, `map_buoy`, `map_beacon` y `map_mirador` | Atlas de UI | 5 | Madera y vidrio |

Las piezas de UI del Mapa van en un atlas de 1024 × 1024 en ASTC 4×4 en el teléfono, y las texturas de terreno, en 6×6 (propuesta del Mapa §10.7).

## 5. Puntos para el asesor

Antes de pintar estos sitios y estas piezas, el asesor da su visto bueno o propone una alternativa (07 §8.2; propuesta del Mapa §10.3):

1. **Lago de Tefé** (parada 7, `bg_hero_lago_tefe`). **Asesor obligatorio:**
   - **sin botos y sin orillas secas**, con una LUT que no evoque la sequía. En 2023 el lago vivió una mortandad masiva de botos con la sequía y el agua muy caliente, y 07 §8.1 veta las sequías de 2023-2024 como chiste o como mecánica;
   - Mamirauá va en la tarjeta de la parada, no en el fondo.
2. **Puerto Nariño** (parada 11, `bg_hero_puerto_narino`). **Asesor obligatorio**, porque sería una aldea identificable (07 §8.1). La alternativa preparada: solo la boca del Loretoyacu.
3. **Río Itaya** (parada 15, `bg_hero_itaya_creciente`). **Asesor obligatorio:**
   - las casas que flotan sobre balsas en la creciente, sin rasgos de barrio, sin «Venecia», sin pobreza pintoresca y sin personas;
   - **«Belén» nunca** como nombre de parada, rótulo ni ranura;
   - la reserva, `bg_hero_casa_de_fierro`, va sin el mito de Eiffel.
4. **El bufeo** (mundos 7 y 8). Rosa se queda en los rápidos de Teotônio y guía un **bufeo boliviano**: el rig de Rosa con otra paleta y sin nombre propio. Rosa vuelve en el viaje del 560 (D-013, Q18-A; glosario §3.2). El asesor confirma el relevo y la paleta.
5. **Shanay-Timpishka** (mundo 9, `bg_kit_w09`). El río hirviente (~95 °C) entra en el repertorio de Ucayali «según responda el asesor: parada, solo vapor lejano en el fondo o nada»: que sea un lugar sagrado no está comprobado, y hay comunidades a su orilla (07 §2.1, fila 9; D-013, punto 11 y «Dependencias externas»). **Se decide con el asesor antes de pintar el kit de Ucayali**, y el paso 3 envía `bg_kit_w09` solo con esa respuesta.

Además:
- las **banderas**, con el abogado (sección 4);
- la **embaúba** de la casa de TiTino, con la condición de TiTino (sección 4).

## 6. Entrega

- **Formato:**
  - PNG y, si el artista los tiene, PSD o PSB con capas, por **Git LFS** (`.gitattributes`: `*.png`, `*.psd` y `*.psb`);
  - PNG de 8 bits por canal, sRGB y sin metadatos.
- **Nombres y medidas:** cada PNG lleva el **nombre exacto de su ranura** y su medida de entrega (`Assets/_Project/Art/Generated/README.md`; héroes y kits a 2×).
- **Integración:**
  - va en `Assets/_Project/Art/Generated/<ranura>.png`;
  - el `.meta` lo genera Unity;
  - los ajustes de importación los aplica el script de Editor versionado, nunca el `.meta` a mano (CLAUDE.md, regla 3);
  - los agentes solo integran.
- **Registro:** cada PNG se anota en el registro del README, con «Artista» y este encargo.
- **Crédito:** el nombre del artista va en la pantalla de créditos (07 §8.2).

## 7. Coste y peso [E]

- **Fondos de 1-200** (propuesta del Mapa §10.8; 07 §11): ≈ 28,75 días de artista en total, ≈ +11 sobre los 18 de las 6 pinturas de antes. De ellos, **≈ 25,75 son de este encargo** [cálculo]:
  - kits, 10;
  - héroes de ciudad o de forma única con la reserva, 8;
  - héroes de naturaleza, 5,25;
  - ambiente, 1;
  - segundo follaje, 1,5;
  - río genérico, 3: pieza del canon, fuera de este encargo (07 §11; 08 §6.2).

  Los 4 kits de 201-700 suman unos 8 días más (Q14-A).
- **Mapa** (propuesta del Mapa §12; 07 §11): ≈ 9,25 días de artista, todos de este encargo (sección 4). Son ≈ +7,25 sobre el canon de antes, porque los 2 días del Mapa de 13 paradas ya estaban en la fila de UI de 07 §11.
- **Total de este encargo:** ≈ 35 días (fondos de 1-200 sin el río genérico, y el Mapa), más ≈ 8 por los kits de 201-700 [cálculo].
- **Peso** (propuesta del Mapa §10.7):
  - fondos de 1-200, ≈ 2,35 MiB con los kits en 8×8;
  - Mapa, ≈ 1,7 MiB;
  - **memoria:** todo residente, unos 6,4-8,2 MiB con los kits de 201-700, el muelle y el follaje, muy por debajo de los 120 MB de texturas residentes de 08 §5.4;
  - **AAB:** el impacto neto frente al canon es de entre −1 y +3 MB con Q14-A [cálculo], frente al AAB de humo de 35 MB y la compuerta de 80 MB.

  La estimación temprana es de la tarea B0, y la medida real, de M5.2.

## 8. Pendientes

**Preguntas para el dueño, antes de enviar los pasos 2 y 3.** Cada respuesta se registra en `docs/dev/decisiones.md` (carril de documentación):
1. **Cuáles son los 3 primeros héroes** (paso 2; 3.3). El canon dice «empezando por los 3 primeros sitios», sin nombrarlos.
   - Propuesta [P]: Ver-o-Peso, Teatro Amazonas y Leticia, uno por cada mundo con fondo en el AAB y héroe de ciudad.
   - Alternativa: el orden de la ruta (Ver-o-Peso, Alter do Chão y Flona Tapajós). Con ella cambian 3.3 (la tabla y la lista de los otros 11) y los provisionales de `docs/art/prompts-chatgpt.md` (revisión 4 y 3.17).
2. **Encargar los kits con la estimación de B0, sin la medida del AAB** (paso 3).
   - Lo pide el plan de la tanda 4 (tareas M1.8 y B0).
   - D-013 («Consecuencias») y la propuesta del Mapa (§10.7 y §12) dicen otra cosa: el AAB se mide tras integrar los 3 primeros sitios, «antes de encargar el resto».
   - Si el dueño lo confirma, se registra como cambio a D-013. Si no, los kits esperan a la medida de M5.2, como el paso 3 bis.

**Pendientes que no bloquean el envío de los pasos 1-3:**

3. **Nombre de ranura del segundo juego de follaje** (3.5), en el glosario §11.
4. **Dónde viven los máster con capas (PSD o PSB).** Fuera de `Assets/_Project/Art/Generated/`, que solo admite los PNG de las ranuras: dentro de `Assets/`, Unity los importaría. Lo decide el dueño.
5. **Cómo pasan los héroes y los kits de 2× a 1× en el build.** Lo decide la tarea de importación, M5.2. El peso cambia por 4 (README, «Para estimar el peso»).
6. **La guía de proyección para la máscara de zonas** (propuesta del Mapa §11.3, punto 5). Aún no tiene tarea:
   - hoy solo existe la muestra `docs/research/mapa-del-rio/mapa-muestra.svg`, de `generar_mapa.py`;
   - antes del paso 4 hace falta una guía por mundo con la proyección de `river_map_v1.json`;
   - es una pregunta para el tech-lead.
