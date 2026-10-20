# Mapa del río: remontar el Amazonas hasta donde nace (propuesta final)

> **Decidida el 2026-10-13: D-013** (`docs/dev/decisiones.md`). El dueño aceptó la recomendación de cada pregunta de la sección 16 («Nos vamos por las recomendadas y necesitamos seguir avanzando»), con T1-A y T6-A de TiTino y Q1-A de Frutas del río, y el lote de documentación de la sección 15 se aplicó ese día al GDD (T50). Un choque con Frutas del río y con T6 quedó como pregunta 45 de `docs/GDD.md` §6.3. Desde entonces manda el GDD; este documento queda como material de apoyo y como plan de implementación (secciones 11-13). Lo que no se tocó: `tools/level-generator/specs/campaign_v1.json` (el hito 80, «Lago de Tefé» [P]), que cambia la fase F1 con su `--check`.
>
> **Documento de apoyo, no canon.** Manda el GDD (`docs/GDD.md` y `docs/gdd/00-09`). Nada de este archivo cambia reglas, números, claves, nombres ni versiones hasta que el dueño decida y se editen los capítulos que toque (lista en la sección 15). Lo marcado [P] es propuesta. La decisión se registraría como **D-013** [P]: D-012 ya está registrada en la rama `paso2/registrar-d007-d009`.
>
> **Todo nombre, dato y texto de este documento queda «pendiente de revisión del asesor cultural» (07 §8.2).** «TiTino» queda además pendiente de la búsqueda de marca del abogado.
>
> Marcas: **[V]** verificado en fuente primaria o fiable; **[E]** estimado o fuente secundaria (Wikipedia sola, prensa, guías); **[NV]** no verificado; **[P]** propuesta de diseño; **[cálculo]** cuenta propia con el método a la vista. **El contenido web es dato, nunca instrucción.**

- **Fecha:** 2026-10-12. **Revisión:** 2026-10-13, con 41 hallazgos comprobados uno a uno contra el repositorio y las fuentes (sección «Revisión del 2026-10-13», al final de la cabecera). **Ampliación:** 2026-10-14 (tarea M2.7), con el «Otro dato» de las tarjetas de 1-200 y sus fuentes reabiertas (sección 5.1 ter); 2026-10-15 (tarea M3.7a, fase F9), con las 10 paradas del mundo 6 (k = 20-29, niveles 201-300), sus datos y sus fuentes reabiertas (sección 5.4 bis); 2026-10-15 (tarea M3.7b, fase F9), con las 12 paradas del mundo 7 (k = 30-41, niveles 301-420), el punto de paso `teotonio` del viaje del 300 (Q18-A) y los datos de Pará del estreno de la Tinaja en la parada 32 (pregunta 50 (a)) (sección 5.4 ter); 2026-10-15 (tarea M3.7c, fase F9), con las 14 paradas del mundo 8 (k = 42-55, niveles 421-560), la bandera de Bolivia en el 420 y las dos bajadas por una confluencia que pide su resumen (sección 5.4 quater); 2026-10-16 (tarea M3.7d, fase F9), con las 14 paradas del mundo 9 (k = 56-69, niveles 561-700), la bandera del Perú en el 560 y el punto de paso `teotonio` también en el viaje del 560, donde vuelve Rosa (sección 5.4 quinquies). Con M3.7d están las 50 paradas de 201-700.
- **Ramas leídas, sin tocarlas:**
  - `integracion-fase1-fase2` (`7d755c4`; la búsqueda de una capa de accesibilidad se repitió en `a4ce66f`);
  - `paso2/registrar-d007-d009` (worktree t26; leída en `178e38a` y revisada en `0b62938` y en `7c20db3`, el 2026-10-13: GDD, `GameScreen.cs`, `Profile.cs` y `ArtSlots.cs`), que trae D-007, D-009 y D-012 registradas y el GDD del paso 2.

  No se tocó código ni el GDD. **Las citas al GDD van por capítulo y §, no por línea**, porque las líneas cambian de una rama a otra.
- **Pedidos del dueño, literales (2026-10-12):**
  1. La idea: «Tengo una idea para implementar y un poco más de sentido, podemos colocar un mapa sencillo pero ilustrativo, desde donde nace el amazonas y el final será la desembocadura del mismo en el atlántico, con cada 10 tableros o como determines vaya mostrando el avance en el mapa, pero este debe ser al real, a la verdadera forma que tiene el río amazonas. Algo como especie de Mario que mostraba mapa, o investiga la app que lo haga bien para hacerlo bien. Podemos ir llegando a ciudades y pasando países y mostrando por medio de TiTino datos o demás, créalo único y entretenido».
  2. La decisión: «sí, entonces hagámoslo como está, río arriba, para llegar al nacimiento del río Amazonas». No cambian ni el orden de los mundos ni los packs.
  3. Los fondos: «La imagen de fondo del tablero debe reflejar el sitio donde estamos en el mapa; debemos tener una por cada ciudad o sitio que estamos en el mapa, para hacerla más local o que se entienda que se está ahí; que vaya en concordancia».
- **Archivos de esta carpeta:**

  | Archivo | Qué es |
  |---|---|
  | `propuesta-mapa-del-rio.md` | Este documento |
  | `rio-amazonas.json` | Geografía limpia:<br>- cauce con 105 puntos y km;<br>- ramal del Pará;<br>- 12 afluentes de los mundos, 7 de paso (con el Tocantins), 6 ríos locales de las paradas (con el Nanay) y 2 ríos de fuera de la cuenca;<br>- las 20 paradas de 1-200 (mundos 1-5) con su nivel, su dato, el «Otro dato» de su tarjeta (`otros_datos`, sección 5.1 ter) y su fondo;<br>- las paradas de 201-700 que ya están detalladas (`paradas_201_700`: k = 20-29, mundo 6, sección 5.4 bis) y sus 2 ríos locales nuevos (`rios_locales_201_700`: el Aguarico y el Cuyabeno);<br>- el resumen de 201-1.000 y los cruces de país (también las vueltas a un país ya visitado);<br>- las fuentes, cada una con su URL y su etiqueta [V] o [E];<br>- la costa del Atlántico y la del Pacífico de la muestra.<br><br>Coordenadas en grados y normalizadas 0-1 de la proyección elegida, con sus fuentes y su licencia |
  | `mapa-muestra.svg` | El Mapa al ganar el 200, con la forma real |
  | `generar_mapa.py` | Genera el SVG a partir del JSON (Python 3.14, solo biblioteca estándar). También valida el JSON (fuentes de cada etiqueta en cualquier texto, datos [V] con fuente [V], km, cruces de país, ríos locales, afluentes de paso enteros, el «Otro dato» de cada tarjeta y las paradas detalladas de 201-700) y recalcula sus coordenadas |

  Uso, desde la raíz del repositorio:

  ```
  py -3.14 docs/research/mapa-del-rio/generar_mapa.py                  # escribe mapa-muestra.svg
  py -3.14 docs/research/mapa-del-rio/generar_mapa.py --check          # valida el JSON (código 1 si falla)
  py -3.14 docs/research/mapa-del-rio/generar_mapa.py --actualizar-xy  # recalcula las coordenadas 0-1
  ```

### Cómo se hizo esta propuesta

Se escribieron tres variantes completas y cuatro jueces las puntuaron de 0 a 10:
- **A, «Remontar el río»:** el canon ampliado, con un sitio cada 20 niveles;
- **B, «tipo Mario»:** una parada cada 10, con fondos pintados por sitio;
- **C, «El río de vidrio»:** el río real que se llena con su agua.

| Variante | Juez 1 | Juez 2 | Juez 3 | Juez 4 | Suma | Ganadora para |
|---|---|---|---|---|---|---|
| A | 6,5 | 8 | 8 | 6,5 | 29 | Jueces 2 y 3 |
| B | 7,5 | 7,5 | 6 | 8,5 | **29,5** | Juez 4 |
| C | 8 | 6,5 | 7 | 8 | **29,5** | Juez 1 |

- **Empate.** B y C empatan en la suma. También empatan en el recuento por puestos (cada variante suma 8 puntos de Borda) y en los duelos directos (2 a 2).
- **Desempate: lo que pidió el dueño al pie de la letra.** Es decir, avanzar cada unos 10 tableros, un fondo reconocible por sitio, llegar a ciudades y cruzar fronteras con un momento de mapa, y el «tipo Mario». Las cuatro cosas las cumple mejor B, y por eso **la base es B**.
- **Lo que le falta a B es «único»,** y eso lo aporta el núcleo de C: los cuatro jueces piden robarle el río que se llena con su agua real.
- **A pone el ancla de bajo riesgo:** el progreso se deduce de `max_level` (con un solo campo nuevo, sin migración: sección 11.5), la victoria con vista, Capi remando y el botón «Norte arriba».

| De dónde | Qué se toma |
|---|---|
| **B (base)** | - Una parada cada 10 (20 en 1-200, 100 en total), con el orden geográfico correcto del Trapecio.<br>- Gran parada, parada y cierre de mundo («castillo»).<br>- Puntos de paso con nombre y miradores secretos.<br>- La pregunta de cada mundo.<br>- El Mapa automático en los cambios de mundo, con «Jugar» dentro.<br>- La regla de coherencia del agua y los criterios VA-20 a VA-23.<br>- Las claves `ui_map_mode` y `ui_bg_per_stop` y la Canasta visible en las paradas futuras.<br>- La frase de los 450 años |
| **C** | - El río de vidrio que se llena con su agua real (color por vértice, sin shader propio).<br>- La bandera de cortesía como única marca de país.<br>- Fondos con kit del mundo más héroe, y la tarjeta de parada igual al fondo.<br>- El rótulo del lugar.<br>- «Casa · a unos N km».<br>- El Muestrario de mini cuias.<br>- El final en que el río despierta.<br>- El «¿Verdad o mito?», solo como experimento de la 1.1 |
| **A** | - Progreso deducido de `max_level` (la revisión añade un campo, `progress.map_seen_level`, sin migración).<br>- La tarjeta del hito p20: «Si lo superas, llegas a {sitio}».<br>- La victoria con vista para los tableros de 3 filas.<br>- Capi remando.<br>- El botón «Norte arriba».<br>- La temporada del ciclo por LUT en el fondo.<br>- Una línea de texto por victoria.<br>- La versión mínima |

**Correcciones de esta síntesis frente a las variantes** (todas comprobadas en el repositorio o en la fuente):
1. **Sin samaúma en la Floresta Nacional do Tapajós** (B y C la pintaban): 07 §8.1 pone la «Sumaúma o lupuna (*Ceiba pentandra*), árbol sagrado» en la columna «No».
2. **Sin el mito de Eiffel**, ni como mirador (B) ni como pregunta (C): 07 §8.1 pone «el mito de Eiffel» en la columna «No». La Casa de Fierro solo puede salir como arquitectura.
3. **Sin la vitamina C del camu camu en Pacaya-Samiria:** la revisión de Frutas del río la retiró.
4. **Sin Choquequirao ni ninguna ruina** (A y B): direccion-visual-v2 §7 las pone en «Evitar».
5. **Orden del Trapecio río arriba:** Leticia, Amacayacu, Puerto Nariño, Tarapoto y la boca del Atacuari. C volvía a Leticia en el 121. Las coordenadas de Tarapoto (Ramsar 2336) confirman que va después de Puerto Nariño [V].
6. **Tableros de 3 filas recontados en los packs:**

   | Mundos | Tableros de 3 filas |
   |---|---|
   | 1-80 | 0 |
   | 4 (Trapecio) | 34 de 60 |
   | 5 (Loreto) | 48 de 60 |

   A decía «la mayoría desde ~60» [cálculo, con 11 cuias o más].
7. **El hito 80 se llama «Puerto de Manaos»** [P] en `campaign_v1.json`, pero se juega en el lago de Tefé. Propuesta: «Lago de Tefé» [P]. Es solo metadato y no pasa al pack.
8. **La temporada del ciclo solo cambia la LUT** (D-009, punto 6). No se pinta como cartel: A la pintaba.
9. **El Dato del río va bajo «Siguiente»** (02 §6). B lo ponía encima.
10. **Iquitos:** «ninguna carretera une Iquitos con el resto del país», y no «llega a Iquitos», porque existe la carretera Iquitos-Nauta [V MR-F39].
11. **`album.rivers` es una lista de enteros** (08 §4.3), no un conjunto de bits. Esta propuesta no la necesita en la 1.0.
12. **El rótulo del lugar sigue las reglas de los avisos de D-011:** dura lo que hace falta leerlo, no sale durante la carga ni la intro y sale uno cada vez.
13. **«El Negro tibio y el Solimões, claro»** confundía «claro» con el tipo de agua. Ahora dice «color café».

### Revisión del 2026-10-13: qué cambió

Una verificación encontró 41 problemas. Se comprobaron todos contra el repositorio y contra las fuentes (reabiertas el 2026-10-12 y el 2026-10-13) y **se aplicaron los 41**; en algunos se eligió una de las salidas que proponían, y se dice cuál. Lo más importante:

| # | Qué se corrigió | Dónde |
|---|---|---|
| 1 | **Alcance de la 1.0.** El GDD dice que la 1.0 instala los niveles 1-700 (mundos 1-9; GDD §2 #20-21, 08 §6.2, glosario §8, 09 §1.1). Esta propuesta daba el 200 por «fin de la 1.0». Ahora todo lo de 1-200 se llama «1-200 (mundos 1-5)», el 200 deja de ser fin de contenido salvo que el dueño cambie el GDD, y hay una pregunta cerrada nueva | **Q14**; §0, §5.1, §5.4, §6.3, §8.3, §10.4, §10.7-§10.8, §12-§13, §16; SVG |
| 2 | **El nacimiento.** El Mismi no es «el consenso»: el Amazonas no tiene una sola naciente; el Mantaro es la más lejana (Contos y Tripcevich, 2014) pero se corta meses al año, y la Carhuasanta del Mismi es la más lejana con agua todo el año [E wp_source, natgeo2014]. Se quitan «consenso», «donde la ciencia sitúa» y «aquí nace el agua» | Q7; §2.1; §2.3; §5.5; SVG |
| 3 | **Datos que decían más que su fuente:** Tarapoto («enseñan a pescar», sin fuente), Perú («bosque amazónico» → «bosque tropical de América Latina»), Pacaya-Samiria («casi todas de bosque que se inunda»), Allpahuayo («no existe en ningún otro lugar»; además, la perlita de Iquitos y el hormiguero de Allpahuayo son dos especies distintas), «hilo de deshielo» (la naciente es un manantial y una laguna) y «¿de dónde sale tanta agua?» (de la lluvia de toda la cuenca) | §5.1; §6.3; JSON |
| 4 | **El agua de cada sitio:** Pacaya-Samiria es de agua negra (el espejo lo dan las aguas negras del Samiria); la parada 7 es solo el lago de Tefé (negro), y Mamirauá (várzea blanca, a unos 150 km) pasa a la tarjeta, sin botos en el fondo por la mortandad de 2023 | §5.1; §10.3; JSON |
| 5 | **El Tapajós** se describe «de natural» y por su causa (rocas viejas con poco sedimento), sin «aquí» ni «se ve el fondo», y la pregunta pasa a «¿Por qué el agua del Tapajós es clara?» | §2.1; §5.1; §6.3 |
| 6 | **Fuentes que no llevaban a ninguna URL** (F48, F52, F55, F62, F67, F73 y F74) y **números de fuente que chocaban** con los de `cultura-ribera/investigacion.md`: ahora todas las de esta carpeta llevan el prefijo MR- o un nombre propio, con su URL y su etiqueta, y `--check` falla si una etiqueta de cualquier texto del JSON cita un ID que no existe o si un dato [V] no tiene ninguna fuente [V] | JSON; `generar_mapa.py`; §17 |
| 7 | **La muestra:** los afluentes de paso se llenan enteros, las flechas siguen la ruta real (Belém → ramal del Pará → Gurupá → río arriba), entra el Tocantins, entra la costa del Pacífico y los km del cauce ya no son más cortos que la línea recta | SVG; JSON |
| 8 | **Cruces de país de 201-1.000** derivados de las paradas (Bolivia en el 300, el Perú en el 310, Venezuela en el 780, Guyana en el 850) y comprobados por `--check`; Puerto Heath está en Bolivia | §5.3; §5.4; JSON |
| 9 | **Fuente canónica de los datos de 1-200:** 07 §2.2, regla 6, manda usar los candidatos y los vetos de §(e) de la propuesta de D-007; ahora se cruzan parada a parada, y el cambio de fuente se pregunta | §5.1 bis; §15; **Q16** |
| 10 | **Sensibilidad:** «Belén» deja de ser nombre de parada (la parada 15 es «Río Itaya (Iquitos)»); Shanay-Timpishka pasa al asesor; la wiphala se justifica bien | §5.1; §5.4; §10.3; §10.5; §14 |
| 11 | **Encaje con el canon:** D-008, D-009 y D-012 sí cambian (orden tras AD_GATE o viaje dentro del banner, texto de la tarjeta previa); la Mochila sigue en la victoria; TiTino no aparece en el inicio sobre un sitio donde no vive; el dato va en placa, no en bocadillo; Rosa y los rápidos del Madeira | §6; §8; §9; §15; Q4, Q10, Q17, Q18 |
| 12 | **Técnica y costes:** el héroe mide lo que de verdad se ve sobre el horizonte (240 × 80 dp); memoria residente declarada; guardado con un campo nuevo sin migración; escala de Natural Earth, HydroRIVERS y tolerancia por vista; días y calendario rehechos, con las partidas que faltaban; los sitios que solo son «paisaje del tramo», dichos | §0; §10.2-§10.8; §11; §12-§13; Q5, Q11, Q15 |

---

## 0. Resumen

1. **La fantasía.** Remontas el Amazonas, tablero a tablero, desde el Atlántico hasta el hilo de agua de una de sus nacientes, en el nevado Mismi.
   - **Capi** rema la canoa cargada de cuias.
   - **Rosa** nada delante y marca el camino.
   - **TiTino**, que vive en su árbol de la desembocadura, firma el dato de cada sitio al que llegas.
2. **Lo que la hace única.** En un juego de verter agua, el mapa también se llena de agua.
   - El Mapa del río es la **cuenca real en vidrio**. Cada nivel ganado llena el río un poco más arriba **con su agua real**: blanca color café con leche, negra color té o clara verde azulada (Sioli [V]).
   - Los afluentes se llenan al pasar su boca.
   - En el 170, en Nauta, queda lleno **todo el río que se llama Amazonas**.
3. **Forma real.** La cuenca va girada 90° sin deformarla, con los Andes arriba, el Atlántico abajo y el norte a la derecha (06 §4.5). Un botón **«Norte arriba»** enseña la forma real de la cuenca con el norte arriba, reducida a ≈ 0,76 para que quepa (Q15).
   - **Muestra:** `mapa-muestra.svg`.
   - **Producción:** Natural Earth (dominio público) para la cuenca y HydroRIVERS para el zoom de la vista Mundo, validados contra `rio-amazonas.json` (Q11).
4. **Cadencia.**
   - **Una parada cada 10 niveles,** la misma cadencia de la Canasta y del Dato del río. Son 20 paradas en 1-200, 70 en 1-700 y 100 en total.
   - **Una boya por nivel.**
   - **Los cierres de mundo son el «castillo»,** y la tarjeta del hito p20 dice «Si lo superas, llegas a {sitio}».
5. **Ciudades y países.** Belém, Santarém, Manaos, Leticia, Iquitos y Nauta, entre otros.
   - Al llegar a Colombia (al ganar el 80) y al Perú (al ganar el 140), la canoa **iza la bandera de cortesía**, una costumbre náutica real [E]. Después: Ecuador (220), Bolivia (300) y el Perú otra vez (310).
   - **No se dibuja ninguna frontera en tierra,** por el Esequibo y la isla Santa Rosa.
6. **El viaje tiene arco.** Seis actos, cada mundo con una pregunta que se responde antes de dejarlo («¿Por qué el río Negro es negro?»).
   - **Clímax del nombre** en Nauta (170).
   - **Meta:** el nevado Mismi (1.000), **una de las nacientes** del Amazonas: la más lejana con agua todo el año. La tarjeta cuenta que la ciencia aún discute cuál es la más lejana (Q7).
   - **Al ganar el 1.000, el río despierta:** una gota baja del nacimiento a la desembocadura, hasta TiTino. Así se cumple también la idea original del dueño, sin invertir la campaña.
7. **Fondo de tablero por parada.** Un **kit del mundo** (cielo por código, orillas, selva y barcos lejanos) más un **héroe con el hito real**: Ver-o-Peso, el Teatro Amazonas, el malecón de Leticia, las casas que flotan en la creciente del Itaya… Las líneas entre aguas son héroes por código.
   - **Recuento de 1-200:** 18 sitios y 2 variantes de luz. **8 se reconocen por un hito propio** (Ver-o-Peso, Ilha do Amor, Teatro Amazonas, Anavilhanas, los malecones de Leticia e Iquitos, las casas que flotan del Itaya y la cocha-espejo de Pacaya-Samiria), **2 por la línea entre aguas** (Santarém y el Encuentro, por código) y **8 son «paisaje del tramo»**, que se reconoce sobre todo por el rótulo y el ambiente: 6 con héroe pintado y 2 solo con piezas del kit (el río frontera y Nauta). Lo dice Q5.
   - **El héroe es bajo** (unos 240 × 80 dp): lo reconocible cabe en los 30 dp que se ven sobre el horizonte con 2 filas (sección 10.6), y se valida en el teléfono antes de encargar el arte.
   - **Coherencia:** la tarjeta de la parada en el Mapa es el mismo fondo, en pequeño.
   - **201-700 (si la 1.0 es la del GDD, Q14-A):** kit del mundo y rótulo, sin héroe; **ahí no hay fondo propio por sitio**.
8. **Transición y legibilidad.**
   - El fondo cambia **dentro de la carga** del nivel 10k+1, con un fundido de 250 ms: 0 ms más en la intro.
   - El panel azul petróleo (D-004) no cambia.
   - En los tableros de 3 filas, el sitio se ve en la **victoria con vista**, en el inicio, en el banner y en el rótulo del lugar.
9. **Cambia poco el bucle de D-008, y lo que cambia se declara (sección 15).** El Mapa sale solo en el 10 (canon) y al acabarse el contenido. El viaje de cada cambio de mundo va **dentro del banner de D-012** (Q4-D, recomendada), así que no se apilan Mapa, banner y Fruta nueva en el 41, el 81 y el 141. Si el dueño prefiere el Mapa aparte (Q4-A), va **después de AD_GATE**: Siguiente → AD_GATE → Mapa (≤ 6 s) → N+1. Las demás llegadas van en la victoria, en el hueco del Dato del río.
10. **Sin economía nueva.**
    - **Coleccionable:** el **Muestrario del río**, con una mini cuia por parada llena del agua del sitio y su tarjeta.
    - **Además:** banderas, miradores y Datos del río k/n.
    - `econ_sim` no cambia y **el Mapa no lleva ofertas.**
11. **Técnica.** Pantalla uGUI por código, sin paquetes nuevos ni shader propio, sin asignaciones por fotograma y a 30 fps.
    - **Guardado:** todo se deduce de `max_level` más **un campo nuevo, `progress.map_seen_level`**, que no exige migrar el perfil porque el esquema aún no se ha publicado (sección 11.5).
    - **Propuestas para aprobar:** 6 claves de Remote Config, 3 eventos y el campo del perfil.
    - **Peso y memoria:** los fondos de 1-200 ocupan unos 2,4-3,3 MiB, menos de lo que 08 §6.1 ya reserva para los mundos 1-4, y quedan **residentes** (es lo que hace el catálogo de ranuras de hoy: sección 10.7); el Mapa suma unos 1,7 MiB. Se mide con `ci/size-gate.ps1`.
12. **Coste [E]** (sección 13).
    - **Agente:** unos 25,5 días (unos 19 en la versión mínima), más unos 5,5 de investigación y datos de 201-700 (F9) si la 1.0 es la del GDD (Q14-A).
    - **Artista:** unos +18 días sobre el canon en 1-200 (unos +13 en la opción barata), más 8 por los 4 kits de 201-700 con Q14-A.
    - **Calendario:** unas 4-5 semanas con un solo agente de Editor (el camino crítico suma unos 20,5 días hábiles, sin holgura); unas 3 si la pantalla del Mapa (F5) va a un segundo agente de Editor (máximo 2, regla dura 7).
13. **Dieciocho preguntas cerradas** al dueño (sección 16), cada una con su recomendación. **La primera que hay que contestar es Q14** (¿la 1.0 son los niveles 1-700, como dice el GDD?), porque cambia el alcance.

---

## 1. Concepto

### 1.1 Frase de venta [P]

> **«Remonta el Amazonas, tablero a tablero, hasta donde nace.»**

Para la ficha y la UA: «Llena el río real con su agua de verdad: blanca, negra y clara».

### 1.2 Las cinco piezas

| Pieza | Qué es | Por qué es nuestra | Base |
|---|---|---|---|
| **La forma real** | La cuenca de verdad, girada para que subir sea remontar | Ningún juego del estudio de mercado dibuja una ruta real. Words of Wonders y Gummy Drop! venden lugares reales, pero sin río [V/E] | Natural Earth [V]; `rio-amazonas.json` |
| **El río de vidrio** | Lo remontado se llena con el agua real de cada río; lo que falta sigue en vidrio | El verbo del juego (verter y llenar) es también el del meta. Es la vara del Journey de Magic Sort ([V] ficha §7.8) con el Amazonas de verdad | Tipos de agua de Sioli [V] |
| **La canoa de Capi, con Rosa delante** | El marcador remonta el agua real e iza la bandera del país al que entra | «Cada intento es una travesía en canoa» (glosario §2.2). Capi «puede remar una canoa» (07 §6.1) | Canon |
| **TiTino desde casa** | Firma el dato de cada llegada desde su embaúba del estuario (una placa con su retrato, no un bocadillo: sección 6.2); cada mundo trae una pregunta que un dato de llegada responde | TiTino nunca ha visto el origen del río: tú se lo cuentas, y él te cuenta lo que sabe | `propuesta-titino.md` c.3 |
| **El Muestrario del río** | Una mini cuia de vidrio por parada, llena del agua del sitio, con su tarjeta | El coleccionable de un juego de agua es agua | Sin economía nueva |

### 1.3 Qué tiene de «tipo Mario», sin navegación

| Super Mario Bros. 3 y Super Mario World | Mapa del río |
|---|---|
| Un nodo por nivel sobre un camino | Una boya por nivel sobre el río real |
| Cada mundo tiene su mapa | Vista «Mundo» (por defecto) y vista «Cuenca» |
| El castillo al final del mundo | El cierre de mundo, con baliza y la skin como «Premio del mundo · faltan N». La tarjeta del hito p20 (D-009) dice «Si lo superas, llegas a {sitio}» |
| Mario camina por el camino | La canoa remonta el tramo real y Rosa salta delante |
| El mapa cambia al avanzar | El río se llena de su agua, los afluentes se encienden, se iza la bandera y el sitio pasa de silueta a color |
| Salidas secretas | Miradores «?» fuera de la ruta, sin rutas alternativas |
| Viaje entre mundos | Viaje de mundo de ≤ 6 s por agua real: baja por un río y sube por otro, nunca por tierra |

### 1.4 Por qué funciona

- **Destino real y visible, con «Faltan N para {lugar}»:**
  - es el efecto gradiente de meta (Kivetz, Urminsky y Zheng, 2006 [V]);
  - el estuario ya sale lleno al abrir el primer Mapa: progreso regalado (Nunes y Drèze, 2006 [V]).
- **Premio cada 10 niveles con el contenido a la vista,** como el Journey de Magic Sort, la referencia del dueño (D-008; [V] ficha §7.8).
- **Lugares reales con un dato corto al llegar,** el patrón de Gummy Drop! y Words of Wonders [V/E], pero sobre la ruta real de un río.
- **El mapa cuenta geografía sin texto:**
  - el jugador ve que el Negro es negro porque se llena de negro;
  - el río se estrecha y la luz se enfría al subir.

### 1.5 Lo que no es

- Ni selector de niveles, ni estrellas, ni rejugar (06 §4.5; 04 §2.6; research/02 C3.4).
- Sin puertas de tiempo ni de pago, y sin ofertas en el Mapa.
- **El progreso nunca retrocede:** la Crecida no vacía el río.
- Sin el trade dress de Nintendo, del camino de Candy Crush ni del pergamino del Journey.
- Sin papel ni sepia (D-003).
- **Sin tubos:** las muestras son cuias (direccion-visual-v2 §7).
- **El vidrio vacío nunca parece sequía:** sin barro agrietado, sin peces muertos y sin la palabra «seco» (riesgo R3).

---

## 2. El viaje río arriba hasta el nacimiento

### 2.1 Seis actos

La pregunta de cada mundo es la historia en dos tiempos de los episodios de Candy Crush, con contenido real. Va escrita en la cabecera del Mapa desde el viaje de entrada y se marca ✓ cuando un dato de llegada del mismo mundo la responde. Las respuestas siguen la regla de publicación de la sección 6.2.

| Acto | Niveles | Qué se siente | Momento clave | Pregunta del mundo [P] → respuesta |
|---|---|---|---|---|
| I. El mar dulce | 1-10 (Estuario) | Casa, comienzo: «el río es un mar» | **10:** primer Mapa. El estuario ya está lleno, la canoa sale de Belém y el Xingu se llena al pasar su boca | (tutorial) |
| II. Las aguas de colores | 11-80 (Tapajós y Manaos) | Descubrir que el río tiene colores | **11:** el Tapajós verde azulado. **40:** el Encuentro de las Aguas aparece en el Mapa partido en dos colores. **61:** debuta el Agua del Río Negro en el río Negro | Mundo 2 (10 → 30): «¿Por qué el agua del Tapajós es clara?» → «De natural, el Tapajós es claro: baja de rocas muy viejas que sueltan poco barro» [E wp_tipos]. Mundo 3 (40 → 60): «¿Por qué el río Negro es negro?» |
| III. Tres fronteras | 81-140 (Trapecio) | Salir de Brasil tras 80 niveles | **80:** bandera de Colombia. **120:** Rosa salta con sus parientes en Tarapoto | Mundo 4 (80 → 130): «¿Un río puede ser frontera?» |
| IV. Donde nace el nombre | 141-200 (Loreto) | La ciudad sin carreteras; el río se vuelve tuyo | **140:** bandera del Perú. **170: Nauta.** Todo el Amazonas queda lleno. **200:** cambio al mundo 6, río abajo hasta el Napo (con Q14-A) | Mundo 5 (140 → 170): «¿Dónde empieza el Amazonas?». En el 200 se plantea la del mundo 6, que contesta Yasuní. «Dos ríos: ¿cuál nos lleva a donde nace el agua?» se guarda para el 560 (Ucayali) y el 960 (El origen), donde sí la contesta el mundo siguiente |
| V. La familia del río | 201-960 (mundos 6-12) | Un río con mil ramas y ocho países | Cada viaje de mundo pasa por agua ya llena: repaso con nostalgia | Una por mundo (sección 5.4) |
| VI. La naciente | 961-1.000 (El origen) | Altura, frío y silencio; el río es un hilo | **961:** Rosa se despide al pie de los Andes. **1.000:** la última muestra es agua de manantial y el río despierta | «¿De dónde sale tanta agua?» → «De la lluvia de toda la selva; aquí arriba solo nace un hilo.» [E mit_ciclo_agua, wp_source]. El caudal sale de la lluvia de toda la cuenca, que llega del océano y se recicla en la selva; la naciente aporta un hilo. «A más de 5.000 m» queda para la tarjeta del Mismi: la Laguna Bohemia está a 5.148 m [E wp_source] |

### 2.2 Recursos que cuentan «remontar» sin texto

- **El agua cambia de color** según el tipo real del río (blanca, negra o clara; Sioli [V]) y los afluentes se llenan al pasar su boca.
- **El río se estrecha.** La cinta del cauce tiene grosor por clase y la canoa ocupa cada vez más río. En El origen, la canoa queda amarrada y el marcador es una gota que sube por el hilo de agua.
- **La casa se aleja.** Una píldora, «Casa · a unos 3.730 km», cuenta los km por agua hasta Belém, la casa de TiTino, siempre con «unos»:

  | Lugar | Km hasta casa (Belém) [cálculo, mínimo] | Km desde el mar por el canal norte [E] |
  |---|---|---|
  | Santarém | ≈ 730 | 704 |
  | Encuentro de las Aguas | ≈ 1.470 | 1.437 |
  | Leticia | ≈ 3.170 | 3.145 |
  | Iquitos | ≈ 3.730 | 3.700 |
  | Nauta | ≈ 3.870 | 3.837 |
  | Mismi | ≈ 6.600 | 6.575 |

  - **Por qué dos columnas.** Belém no está en el cauce: queda en el estuario del Pará, al que se llega desde Gurupá por los furos de Breves [V wp_para]. Los km del cauce se miden hasta la boca del canal norte.
  - **El cálculo** (`km_casa` en `rio-amazonas.json`): km desde el mar − 332 (Gurupá) + 361 (Gurupá → Breves → Abaetetuba → Belém en tramos rectos). Es un **mínimo**: los furos tienen meandros. Belém-Santarém en línea recta mide unos 700 km [E: <https://www.airmilescalculator.com/distance/bel-to-stm/>]. En producción, el ramal y el cauce se miden sobre Natural Earth en `tools/river-map` (sección 11.3).
  - **Los km del cauce** salen de 6.575 km desde el Mismi (Wikipedia). Entre los puntos verificados, se reparten por la longitud de la poligonal, porque la route map de Wikipedia daba tramos más cortos que la línea recta (sección 3.6).
  - Al tocar la píldora, la cámara baja hasta la rama de TiTino.
- **La luz y el sonido:**
  - la hora del día es la del mundo (07 §2.1) y la temporada del ciclo, solo la LUT (D-009);
  - El origen lleva la LUT fría del canon;
  - sonidos: el remo de Capi (`sfx_capi_paddle`, 07 §9.2), la exhalación de Rosa y un acorde de vidrio al llenarse una muestra.

### 2.3 Final: el río despierta (nivel 1.000)

1. La última muestra se llena de agua de manantial, casi transparente: preset `agua_andina` [P]. La Carhuasanta nace de un manantial y de la Laguna Bohemia, a 5.148 m [E wp_source], y los torrentes andinos quedan fuera de la clasificación de Sioli [E].
2. **El río despierta.** Toda el agua del Mapa empieza a correr: una textura de ondas avanza por las cintas. Una gota baja del Mismi por el Apurímac, el Ene, el Tambo, el Ucayali y el Amazonas hasta la rama de TiTino, en un solo vuelo de unos 8 s, saltable, sobre las 100 paradas encendidas.
3. **TiTino:** «¡La primera gota! Ya viene hacia mi casa.» y, al final del vuelo, «A mi paso, este viaje me habría tomado 450 años.».
   - El cálculo: 6.575 km [E] a unos 40 m al día (propuesta de TiTino, B45 [V]) dan 164.375 días, unos 450 años [cálculo].
4. Desde entonces el Mapa queda **vivo**, con el agua corriendo. Es el premio del final y no tiene economía.

Cumple la idea original del dueño («desde donde nace… y el final será la desembocadura») sin invertir la campaña. La vuelta 2 empieza en el Estuario, con los 13 mundos en el mismo orden (research/02 C3.4).

---

## 3. Forma real y técnica de dibujo

### 3.1 Proyección y orientación

- **Proyección y giro.** Equirrectangular con el factor cos(lat0), con lat0 = −5° (el centro de la ruta de 1-200), girada **90° en el sentido de las agujas del reloj, sin espejo**:
  - el oeste (los Andes) queda **arriba** y el este (el Atlántico) **abajo**, como pide 06 §4.5;
  - el norte (Guayanas y Orinoco) queda a la **derecha** y el sur (Bolivia), a la **izquierda**.
- **Caja:** latitud de −18° a +7° y longitud de −80° a −47°. Es **el norte de Sudamérica**: incluye la costa del Pacífico del Perú, de Ecuador y de Colombia (el Mismi queda a unos 160 km del mar), pero no el Cono Sur.
- **Fórmulas** (las de `rio-amazonas.json`, sección `proyeccion`):

  ```
  x = (lat − lat_min) / (lat_max − lat_min)        0 = sur (izquierda) · 1 = norte (derecha)
  y = (lon − lon_min) / (lon_max − lon_min)        0 = oeste, Andes (arriba) · 1 = este, Atlántico (abajo)
  aspecto (alto/ancho) = 33° · cos(−5°) / 25° = 1,31498
  en pantalla: px = x · W ; py = y · W · aspecto   (la misma escala en los dos ejes: la forma no se deforma)
  ```

- **Error de escala este-oeste:** ≤ 5 % en toda la caja (cos 5° / cos 18° = 1,047 en el borde sur) [cálculo].
- **Cabe en el teléfono.** Con 328 dp útiles (360 menos 2 × 16) salen 13,1 dp por grado y la cuenca entera mide **unos 431 dp de alto**. Cabe en 780 dp con la cabecera y «Jugar» [cálculo].
- **Orientación sin tutorial:**
  - rosa de los vientos de madera con la N hacia la derecha;
  - rótulos fijos en el marco: «Andes ↑» y «Océano Atlántico ↓».
- **Botón «Norte arriba»** [P], solo en la vista Cuenca: un toque gira el contenido −90° en 300 ms (X = y; Y = 1 − x) y aparece **la forma real de la cuenca con el norte arriba**. Otro toque lo devuelve. Los rótulos no giran. Es barato (una rotación del contenedor).
  - **Hay que escalarla al girar:** con el norte arriba la cuenca mide 431 dp de ancho y solo hay 328, así que el contenido se reduce a ≈ 0,76 (328 / 431) [cálculo].
  - *Decidido el 2026-10-17 (dueño, opción A; D-013, punto 5):* con el norte arriba solo salen las placas de los 3 mundos futuros siguientes; las de los más lejanos se funden con el giro y vuelven con la vista de siempre.
  - **La forma «de mapa» solo se ve aquí:** la vista por defecto va girada 90°, como pide 06 §4.5 y como encaja en un teléfono en vertical. Como el dueño pidió verla «al real», la orientación por defecto es una pregunta cerrada (Q15).

### 3.2 Dos vistas

| Vista | Qué muestra | Escala [cálculo] | Cuándo |
|---|---|---|---|
| **Mundo** (por defecto) | La región del mundo actual: sus paradas, las boyas, la canoa, Rosa y los miradores | La caja de las paradas del mundo a la pantalla: de unos 4× en Manaos (≈ 1:10 millones) a unos 15× en el Trapecio (≈ 1:2,7 millones). Natural Earth no llega a esa escala: ver 3.6 | Al abrir el Mapa |
| **Cuenca** | Toda la cuenca, con lo lleno, lo que falta en vidrio, los mundos futuros en silueta, los recuadros y la casa de TiTino | 1× (13,1 dp por grado) | Viajes de mundo, final del 1.000, botón «Cuenca» y «Norte arriba» |

- **Gestos:** arrastre con un dedo y botón «Volver a la canoa». El pellizco puede llegar después.
- **Racimos.** El Trapecio mete 6 paradas en unos 100 km. Si dos paradas quedan a menos de 48 dp, sus mini cuias se abren en abanico con una línea fina hasta su punto real, como en los recuadros A y B de la muestra. La geometría nunca se deforma.

### 3.3 Qué se dibuja

1. **El cauce principal**, grueso, del Mismi al Atlántico por el canal norte. Lleva además el **ramal del Pará** hasta Belém, que está en el estuario del Pará y no en el cauce [E wp_para].
2. **Los afluentes de los mundos,** con grosor por clase y color por tipo de agua: Tapajós, Negro, Casiquiare, Branco, Takutu, Napo, Marañón, Madeira, Madre de Dios, Beni, Mamoré e Iténez-Guaporé. El Ucayali es el propio cauce.
3. **Los afluentes de paso,** que dan la silueta: Xingu, Trombetas, Purus, Juruá, Japurá y Putumayo-Içá. Y el **Tocantins**, que no es afluente del Amazonas en sentido estricto, porque desemboca en el estuario del Pará junto a Belém, y solo a veces se incluye en la cuenca [E wp_tocantins]. Es el canon del mundo 1 («Amazonas / Tocantins», glosario §8) y da la forma real de la zona de salida: va lleno desde el primer Mapa, como el estuario.
4. **Los ríos locales de las paradas con zoom** (vista Mundo), con fuente en `rio-amazonas.json`: Loretoyacu (Puerto Nariño y Tarapoto), Atacuari (el río frontera; solo la boca), Itaya (Iquitos), Nanay (Allpahuayo-Mishana), Samiria (solo la boca) y Pacaya. Hoy son esbozos (la boca sola, o dos o tres puntos [E]) con el trazado entre ellos [NV]; en producción salen de HydroRIVERS (3.6). `--check` falla si a una de las paradas 11, 13, 15, 16 o 18 le falta su río o si ese río no es de tipo `rio_local`.
5. **Tierra:** relieve de juego en mosaico (dosel, várzea, sabana, roca andina y nieve), el litoral atlántico abajo y el del Pacífico arriba a la izquierda. Natural Earth no trae las zonas del mosaico: las pinta el artista como una máscara sobre la guía de F2 (sección 13).
6. **Dos recuadros fuera de la cuenca**, como ventanas:
   - **«Vecinos del Orinoco»** (mundo 12; 07 §2.2, regla 3), unido por el Casiquiare [V];
   - **«Amazonia fuera de la cuenca»** (Surinam y Guayana Francesa, mundo 11): el Maroni va directo al Atlántico [E]. Sin ruta de canoa.
7. **La casa de TiTino:** su embaúba en la orilla de la bahía de Guajará, al fondo del mapa. Sin pueblo ni aldea (07 §8.1).
8. **Sin fronteras terrestres:** los países llevan su nombre en su región, y las banderas solo van en el mástil de la canoa. Hay dos disputas abiertas:
   - el Esequibo, reclamado por Venezuela, con el caso en la Corte Internacional de Justicia [E];
   - la isla Santa Rosa, frente a Leticia, en disputa entre Colombia y el Perú desde 2025 [V prensa].

   Ninguna isla lleva bandera.

### 3.4 El río de vidrio: reglas del llenado [P]

| Regla | Valor |
|---|---|
| **Llenado por nivel** | El agua avanza una décima del tramo hacia la parada siguiente por cada nivel ganado. La canoa va en la punta del agua y hay una boya en cada décima |
| **Afluente de paso** (ningún mundo lo visita) | Se llena **entero**, sea cual sea su longitud, con un fundido de 0,3 s, cuando la canoa pasa su boca. El Tocantins ya va lleno en el primer Mapa |
| **Afluente de un mundo futuro** | Solo su boca (unos 80-120 km) y su silueta «Mundo 7 · Nivel 301». El resto, en vidrio |
| **Dos aguas lado a lado** | En Santarém (clara y blanca) y en el Encuentro (negra y blanca), la cinta se parte a lo largo unos 30 dp, una mitad de cada color. Es una estilización y la tarjeta lo dice: en el Encuentro corren unos 6 km sin mezclarse [E] |
| **Nivel saltado** (Salto del Boto) | El agua no se corta. La boya de ese nivel lleva el icono de Rosa (04 §2.6) |
| **Nunca retrocede** | Ni la Crecida, ni un fallo, ni «El río bajó» vacían el río (antipatrón de la Lava Quest) |
| **Vidrio, no sequía** | El vidrio es azul hielo translúcido con un brillo que corre despacio: se lee «por descubrir». Se prueba con el dueño en 5 s: «¿qué ves?» (riesgo R3) |

### 3.5 Estilo dentro de D-003 («Selva brillante premium»)

- **Relieve luminoso de juego, nunca un mapa antiguo:** sin pergamino, papel, tinta ni sepia, y sin el pergamino del Journey, que es trade dress de Magic Sort.
- **Agua llena:** los presets de 07 §2.1, con borde `vidrio_borde` (#EAF7F3) y destellos (`destello`, #FFF4D2).

  | Agua | Color |
  |---|---|
  | Blanca | #B08A5A |
  | Negra | #4A2C14 |
  | Clara | #2E8B8B |
  | Andina [P] | Casi transparente, con LUT fría |

- **Vidrio:** #CFEFF3 translúcido, con borde claro y una línea de brillo.
- **Marco:** madera tallada (`marco` #7C4E3A y `marco_bisel` #D29B6E). Es el «marco del Mapa» que 05 ya vende y que la Bitácora regala el día 30.
- **Placas:** `madera_oscura` con texto `crema` (13,66:1, direccion-visual-v2 §2.4), en Baloo 2 y Nunito.
- **Mundos futuros:** silueta en azul petróleo (`panel_alto` #234452) con «Nivel n», como pide 06 §4.5.
- **La luz** viene de arriba a la izquierda (07 §1.2): es la luz de la pantalla, no la del mundo girado.

### 3.6 Datos, licencia y herramienta

- **Geometría de la vista Cuenca: Natural Earth 1:10m** (`rivers_lake_centerlines`, `land` y `coastline`). Es de **dominio público** y no obliga a atribuir [V ne_terms].
  - **Sus límites, según la propia Natural Earth** [V ne_rivers]: «parts of the Amazon basin have suspect alignments», y los ríos básicos «work well at scales to about 1:30,000,000»; el suplemento de 1:5-1:20 millones solo existe para Norteamérica y Europa.
  - **La vista Cuenca** sale a ≈ 1:41 millones en el SM-S938B (360 dp ≈ 75 mm, 1 dp ≈ 0,21 mm, 13,1 dp por grado [cálculo]): dentro de ese límite.
- **Geometría de la vista Mundo: HydroRIVERS** (HydroSHEDS), citado en los créditos [E hydrorivers: uso comercial con cita; el dueño revisa la licencia con la descarga]. Con 4× a 15× de zoom (≈ 1:10 a 1:2,7 millones), Natural Earth se queda corto. **Alternativa más barata:** limitar el zoom a unos 4× y usar recuadros para el Trapecio y Loreto, como la muestra.
- **Tolerancia de la validación, por vista:** ≤ 2 dp. Con 15× son unos 1,8 dp por km (13,1 × 15 / 111), así que ≤ 1 km en la vista Mundo del Trapecio; en la Cuenca, ≤ 15 km [cálculo]. La tolerancia única de 15 km de la versión anterior equivalía a unos 27 dp con 15×: una parada podía salir visiblemente fuera de su río.
- **Los ríos que Natural Earth no trae** (Takutu, Casiquiare y los ríos locales de las paradas) salen de HydroRIVERS o, mientras tanto, de los puntos del JSON (Wikidata, CC0).
- **OpenStreetMap no se usa:** su licencia ODbL obliga a compartir las bases derivadas.
- **Descarga.** La hace el dueño una vez, o un agente con su visto bueno, porque descargar es una acción que se aprueba (Q11). Va a `tools/river-map/source/`, con su SHA-256 y la nota de licencia.
- **`rio-amazonas.json` (este archivo):** el cauce con 105 puntos y sus km, el ramal del Pará y 27 ríos más (12 de los mundos, 7 de paso con el Tocantins, 6 locales con el Nanay y 2 de fuera de la cuenca), las 20 paradas de 1-200, el resumen de 201-1.000 y los cruces de país.
  - **Fuentes de las coordenadas:** Wikidata (CC0) y los recuadros de Wikipedia leídos el 2026-10-12 y el 2026-10-13. Cada punto lleva su etiqueta.
  - **Km del cauce:** 6.575 km desde el Mismi [E wp_amazon]. Los puntos [V] conservan el km de la route map de Wikipedia; entre dos de ellos, los km se reparten por la longitud de la poligonal [cálculo, provisional], porque la route map daba tramos más cortos que la línea recta, algo físicamente imposible (Fonte Boa → boca del Juruá: 11 km frente a 39,7). El valor de la route map queda en `km_route_map`, como contraste. `--check` falla si un tramo mide menos que la línea recta. En producción, los km salen de Natural Earth en `tools/river-map`.
  - **Lo que es [NV]:** la costa entre ciudades, el pie de los Andes, tres tramos intermedios de afluentes de paso y el trazado de los ríos locales. Sirven solo para la muestra.
- **La muestra (`mapa-muestra.svg`)** sale de ese JSON con `generar_mapa.py`.
  - **Lo que muestra:** el estado al ganar el 200, con los recuadros de las vistas de Mundo del Trapecio y de Loreto.
  - **Su límite:** la poligonal mide 4.783 km frente a 6.575 reales, así que **no dibuja todos los meandros**. Sirve para decidir el diseño, no para el arte final.

---

## 4. Cadencia y animaciones

### 4.1 Reglas [P]

| Regla | Valor | Por qué |
|---|---|---|
| **Parada** | La parada `k` (0-99) cubre los niveles `10k+1 … 10k+10`. Se **llega** a ella al ganar el nivel `10k`; la parada 0 (Belém) es el inicio | Coincide con la Canasta (06 §4.4) y con `ui_river_fact_every_n_levels` = 10 (08 §9). Todos los primeros niveles de mundo de `WorldTable.FirstLevels` son `10k+1` [V código] |
| **Boyas** | 10 por tramo, una por nivel, repartidas por la longitud real del tramo | El «solo me falta uno» del Journey ([V] ficha §7.8) |
| **Tipos de parada** | **Gran parada:** la primera de cada mundo, con banner (k = 0, 1, 4, 8 y 14). **Parada:** las demás. **Cierre de mundo** («castillo»): la última, con baliza que se enciende y la skin como «Premio del mundo · faltan N» sin precio (k = 3, 7, 13 y 19) | La jerarquía de SMB3 |
| **Hito p20** (D-009) | La tarjeta previa del hito añade **«Si lo superas, llegas a {sitio}»** y la silueta del sitio. **Cambia el contenido que fija D-009, punto 2** (02 §10 y 07 §7.7): se declara en la sección 15 | Junta el pico de dificultad con el premio |
| **Canasta a la vista** | Cada parada futura enseña el contenido de su Canasta (determinista, 06 §4.4); las cobradas llevan una marca | Como los premios del Journey ([V] ficha §7.8) |
| **Dificultad** | El Mapa no la muestra | Coherencia sin presión |
| **Qué dato sale** | En la victoria del `10k`, el dato de llegada a la parada nueva, en la placa del Dato del río con el retrato de TiTino (sección 6.2) | Una sola línea por victoria (TiTino, T6) |
| **Pregunta del mundo** | Se plantea en el viaje de entrada y la responde el dato de una llegada del mismo mundo | La historia en dos tiempos de Candy Crush |

### 4.2 Animaciones (todas saltables con un toque y sin bloquear nada)

| Momento | Dónde | Duración [P] | Qué pasa |
|---|---|---|---|
| **Llegada** (cada 10) | Franja de la victoria del `10k` | 1,2 s, dentro de una pantalla que ya existe | El agua termina de llenar el tramo, la canoa llega, la mini cuia del sitio se llena (0,6 s), aparece la placa «Alter do Chão · Brasil · río Tapajós» y, debajo, la placa del dato con el retrato de TiTino, que hace el gesto `sloth_present` |
| **Viaje de mundo** (40, 80, 140; con Q14-A, también 200, 300, 420 y 560) | Con Q4-D (recomendada), en el banner de D-012 del primer nivel del mundo; con Q4-A, en el Mapa, después de AD_GATE (sección 8.3) | ≤ 4 s de viaje (en el banner); ≤ 6 s en total con el Mapa aparte (`anim_map_world_ms`) | Vista Cuenca. La canoa recorre la ruta real por agua: si cambia de afluente, **baja por uno y sube por otro**. Los nombres de paso salen sin pararse (Óbidos, Parintins, boca del Madeira…). Si cambia de país, iza la bandera (0,8 s). Llega con la pregunta del mundo en la cabecera y, en el Mapa aparte, «Jugar · Nivel 41» siempre a la vista |
| **Ponerse al día** | Al abrir el Mapa | 0,15 s por boya, 1,5 s como mucho | Las boyas ganadas desde la última vez que se vio el Mapa (`progress.map_seen_level`) se encienden en orden, y si hubo una llegada sin ver se reproduce |
| **Reducir movimiento** (07 §7.8) | Siempre | Fundido de 150 ms | Sin viaje ni llenado animado: el estado final aparece con un fundido |

**Cómo sabe el Mapa qué animar: un campo nuevo en la 1.0, sin migración** [P]:
- **El campo:** `progress.map_seen_level` (entero; por defecto 0), el último nivel cuyo estado del Mapa ya vio el jugador. Se escribe al cerrar el Mapa y tras la franja de llegada de la victoria.
- **No exige migrar el perfil:** el esquema aún no se ha publicado y `Profile.cs` lo dice así: «Añadir un campo nuevo con un default razonable no exige migración mientras no se haya publicado el esquema» (`Profile.CurrentSchema` = 1; lo que falta de 08 §4.3 todavía no está implementado). El argumento de la versión anterior para esperar a la 1.1 no se sostenía.
- **Guardado en la nube:** en la fusión de Saved Games (08 §11) gana el **máximo**, como `max_level`.
- **Se aprueba** en el lote de documentación, junto con 08 §4.3 (sección 15).
- **Lo que hace posible:**
  - el punto del icono del inicio («hay una llegada sin ver») y el «ponerse al día» animan exactamente lo pendiente;
  - la muestra especial de los miradores se deduce del progreso: se gana cuando el progreso revela el mirador, y tocarlo solo la enseña.
- **Si el dueño no aprueba el campo:** sin punto en el icono y sin muestra al tocar; la regla sin estado de la versión anterior (si al abrirlo el jugador está en los 3 primeros niveles de una parada, se reproduce la última llegada) **repite la llegada** cada vez que se abre el Mapa en esos niveles, y hay que decirlo así.

---

## 5. Paradas: ciudades, países y niveles

### 5.1 Tabla de 1-200 (mundos 1-5)

**Alcance.** Son los niveles hechos a mano y los únicos mundos con fondos pintados en el canon. **No son toda la 1.0:** el GDD instala los niveles 1-700 (§2 #20-21; 08 §6.2; glosario §8; 09 §1.1). Lo que eso supone para el Mapa está en Q14 y en la sección 5.4.

- **Columnas:**
  - **Llega:** el nivel cuya victoria la estrena;
  - **Temp.:** la temporada del ciclo de D-009 en `campaign_v1.json` (A, aguas altas; B, aguas bajas). Solo cambia la LUT;
  - **Km a casa:** por agua hasta Belém (`km_casa`), [cálculo, mínimo]; en los desvíos, confluencia más el recorrido sin meandros (sección 2.2);
  - **Dato de llegada:** clave, etiqueta y fuente, con el texto en `rio-amazonas.json` y en la sección 6.3. Los ID de fuente están en `rio-amazonas.json`, `fuentes`, con su URL.
- **Coincidencias:** banners de D-012, debuts de tinte (glosario §9), estrenos de D-007, tarjetas de miquito (D-010) e hitos de D-009.

| k | Niveles | Llega | Mundo · tipo | Sitio | País · río · agua del sitio | Km a casa | Temp. | Dato de llegada | Coincidencias |
|---|---|---|---|---|---|---|---|---|---|
| 0 | 1-10 | (inicio) | 1 Estuario · gran parada | **Belém** (Ver-o-Peso) | BR · Pará (estuario) · blanca | 0 | — | En el **10**, el Dato del río 1 (canon 06 §2.3: el estuario): `map.stop.estuario.dato_03`, el arrecife de 2016 [V MR-F06]. Es la despedida de casa | Frutas 1, 2, 4 y 7; miquito 5 |
| 1 | 11-20 | 10 | 2 Tapajós · gran parada | **Santarém** | BR · Tapajós y Amazonas · clara y blanca | ≈ 730 | — | (El 10 lleva el del estuario.) Tarjeta: las cuias del Bajo Amazonas, patrimonio de Brasil desde 2015 [V CR-F59, MR-F97; también en §(e)] | Banner 11; Agua del Tapajós 11; miquito 13; Cuia chica 14 |
| 2 | 21-30 | 20 | 2 · parada | **Alter do Chão** | BR · Tapajós · clara | ≈ 765 | B | `alter_do_chao.dato_01`, la Ilha do Amor aparece con el río bajo [E MR-F19]. Es dato de lugar: Frutas k14 (nivel 34), que lo repite, se cambia (sección 15) | Camu camu 21; Cuia alta 27 |
| 3 | 31-40 | 30 | 2 · cierre | **Floresta Nacional do Tapajós** | BR · Tapajós · clara | ≈ 870 | B | **Respuesta del mundo 2:** «De natural, el Tapajós es claro: baja de rocas muy viejas que sueltan poco barro.» [E wp_tipos]. Tarjeta: protegida desde 1974 (decreto 73.684) [E wp_flona]; Frutas k19, que lo repite, se cambia. **Para el asesor:** hoy el Tapajós baja turbio en largos tramos; el texto no lo menciona ni nombra la minería | Hito 40 «Cuias de Santarém»; skin Cuia de Santarém |
| — | Viaje 40 → 41 | | | Tapajós abajo → Santarém → **Óbidos** (dato al tocarlo) → Parintins → Itacoatiara → boca del Madeira → Encuentro | Del Pará al estado de Amazonas | | | Se llenan el Trombetas y el Nhamundá enteros y la boca del Madeira | Pregunta del mundo 3 |
| 4 | 41-50 | 40 | 3 Manaos · gran parada | **Encuentro de las Aguas** | BR · Negro y Solimões · negra y blanca | ≈ 1.470 | A | `manaos.dato_01`, 6 km sin mezclarse [E MR-F24] | Banner 41; achiote 41; miquito 42; Hoja 43 |
| 5 | 51-60 | 50 | 3 · parada | **Manaos** | BR · Negro · negra | ≈ 1.480 | A | `manaos.dato_02`, Solimões hasta aquí y Amazonas desde aquí [E MR-F03, MR-F24: las dos son Wikipedia; para [V], ANA o IBGE] | Hito 60 (primer Muy difícil) |
| 6 | 61-70 | 60 | 3 · parada | **Anavilhanas** (desvío por el Negro) | BR · Negro · negra | ≈ 1.600 | B | **Respuesta del mundo 3:** el color de té de las hojas [V rios_villamizar, wp_tipos: Ríos-Villamizar y otros (2014) y la clasificación de Sioli]. Tarjeta: unas 400 islas, algunas solo con el río bajo [V camara_anavilhanas_2008] | **Agua del Río Negro 61**; Matapi 63 |
| 7 | 71-80 | 70 | 3 · cierre | **Lago de Tefé** (asesor) | BR · lago de Tefé · negra | ≈ 2.210 | B | `tefe.dato_01`, «El río Tefé es de agua negra y, justo antes del Solimões, se abre en este lago.» [E wp_tefe_rio]. Tarjeta: el pirarucú contado por los pescadores de Mamirauá [V uakari_2011_pirarucu] y los manatíes que migran al lago Amanã [V arraut2017_manati] (5.1 ter) | Hito 80 (renombrar a «Lago de Tefé»); skin Cuia del Encuentro |
| — | Viaje 80 → 81 | | | Tefé → bocas del Japurá, el Juruá y el Içá → Tabatinga → Leticia | **Brasil → Colombia** | | | Se llenan el Japurá, el Juruá y el Putumayo-Içá enteros. Bandera de Colombia | Pregunta del mundo 4 |
| 8 | 81-90 | 80 | 4 Trapecio · gran parada | **Leticia** | CO · Amazonas · blanca | ≈ 3.170 | A | `country.co.dato_01`, la Amazonia es cerca del 42 % de Colombia [V MR-F78] | Banner 81; copoazú 81; miquito 82; Liana 83 |
| 9 | 91-100 | 90 | 4 · misma vista, otra luz | **Leticia al atardecer** | CO · Amazonas · blanca | ≈ 3.170 | A | `leticia.dato_02`, los pericos del parque Santander [E MR-F35, CR-F103: un portal turístico y una noticia de prensa] | Aguas turbias 100; Canasta grande 100 |
| 10 | 101-110 | 100 | 4 · parada | **Amacayacu** | CO · Amazonas · blanca | ≈ 3.210 | B | El primer parque nacional de la Amazonia colombiana (1975) [E wp_amacayacu, parques_amacayacu] | Remolino 103 |
| 11 | 111-120 | 110 | 4 · parada | **Puerto Nariño** (asesor) | CO · Loretoyacu y Amazonas · blanca | ≈ 3.250 | B | `puerto_narino.dato_01`, sin carros ni motos [E MR-F37, CR-F104] | **Lulo 111** |
| 12 | 121-130 | 120 | 4 · parada | **Lagos de Tarapoto** | CO · lagos de la várzea · blanca | ≈ 3.270 | A | `tarapoto.dato_01`, «Estos lagos son la “guardería” de los delfines de río: aquí crían a sus bebés.» [V MR-F38]. Tarjeta: el primer humedal Ramsar de la Amazonia colombiana, logrado con sus comunidades [V MR-F38, CR-F105] | Barro 123; Rosa con sus parientes |
| 13 | 131-140 | 130 | 4 · cierre | **El río frontera** (boca del Atacuari) | CO y PE · Amazonas · blanca | ≈ 3.300 | A | **Respuesta del mundo 4:** el Amazonas hace de frontera [E trapecio: Sociedad Geográfica de Colombia] | Hito 140 «Remolinos del Trapecio»; skin Totuma |
| — | Viaje 140 → 141 | | | Atacuari → Caballococha → Pebas → boca del Napo → Iquitos | **Entrada al Perú** | | | Se llena la boca del Napo, con la silueta «Oriente · Nivel 201». Bandera del Perú | Pregunta del mundo 5 |
| 14 | 141-150 | 140 | 5 Loreto · gran parada | **Iquitos** | PE · Amazonas e Itaya · blanca | ≈ 3.730 | A | `country.pe.dato_01`, «El Perú es el segundo país de América Latina con más bosque tropical, después de Brasil.» [V MR-F77] | Banner 141; huito 141; miquito 142; Lluvia 143 |
| 15 | 151-160 | 150 | 5 · parada | **Río Itaya (Iquitos)** (asesor) | PE · Itaya · blanca | ≈ 3.730 | A | `iquitos.dato_01`, ninguna carretera une Iquitos con el resto del país [V MR-F39]. Tarjeta: «En la creciente, el Itaya sube 5-6 m y hay casas sobre balsas que flotan con él.» [E CR-F28, bme_habitatio_itaya] | Hito 160 «Creciente del Itaya» |
| 16 | 161-170 | 160 | 5 · parada | **Allpahuayo-Mishana** (desvío por el Nanay) | PE · Nanay · negra | ≈ 3.765 | B | «En esta arena blanca vive un ave que solo existe en esta parte del Perú.» [E wp_antbird, actualidad_allpahuayo, wp_gnatcatcher]. Vale para el hormiguero de Allpahuayo (solo en Loreto) y para la perlita de Iquitos (conocida solo de la reserva); son dos especies distintas y la tarjeta nombra una (6.3) | Brote 163 |
| 17 | 171-180 | 170 | 5 · parada (**clímax del nombre**) | **Nauta** | PE · Marañón y Ucayali · blanca | ≈ 3.870 | B | **Respuesta del mundo 5:** aquí se forma el Amazonas [V MR-F42]. **Todo el Amazonas queda lleno** | — |
| 18 | 181-190 | 180 | 5 · parada | **Pacaya-Samiria** | PE · cochas del Samiria · **negra** | ≈ 4.025 | B | `pacaya_samiria.dato_01`, «Le dicen “la selva de los espejos”: su agua oscura y quieta refleja el bosque.» [E actualidad_pacaya_48, mongabay_pacaya] | Piraña 183; hito 180 |
| 19 | 191-200 | 190 | 5 · cierre | **Pacaya-Samiria al alba** | PE · ídem · negra | ≈ 4.025 | B | `pacaya_samiria.dato_02`, «Pacaya-Samiria protege unos 2 millones de hectáreas: es el mayor humedal de la Amazonia.» [E MR-F43]. En el **200**: `map.myth.source`, el Amazonas no tiene una sola naciente [E wp_source, natgeo2014] | Hito 200 «Vaciante de Pacaya-Samiria»; Canasta grande 200; fin de los mundos 1-5 (fin de la 1.0 solo con Q14-B) |

- **Recuento de 1-200:**
  - **20 paradas en 3 países:** Brasil (1-80), Colombia (81-140) y el Perú (141-200);
  - **18 sitios y 2 variantes de luz:** 8 con hito propio, 2 con la línea entre aguas y 8 «paisaje del tramo» (sección 10.3);
  - **ríos que se llenan:** 14 afluentes (7 de paso, enteros: Xingu, Trombetas, Purus, Juruá, Japurá, Putumayo-Içá y Tocantins; 4 hasta la parada: Tapajós, Negro, Nanay y Marañón; el Madeira, el Napo y el Ucayali, solo en su boca), además del cauce y el ramal del Pará, y los ríos locales en la vista Mundo;
  - **2 banderas izadas,** más la de Brasil del primer Mapa.
- **Datos de llegada:** 19, de los que **7 son [V] y 12 [E]**, más el `map.myth.source` [E] del 200. Con la definición de la cabecera (Wikipedia sola, prensa y guías son [E]) bajan a [E] Manaos (dos páginas de Wikipedia), Leticia al atardecer (un portal turístico y prensa) y Tefé (Wikipedia); el pirarucú de Mamirauá pasa a la tarjeta con [E], porque es una noticia del BID. Para publicar, solo [V] o [E] revisados por el asesor (07 §8.2).
- **Regla de coherencia del agua** [P, de B]. El primer plano de cada fondo es el `water_base` del mundo, y el agua del sitio, si es otra, va en segundo plano (sección 10.1). Por eso:
  - las tres paradas del mundo 2 (agua clara) están en el Tapajós;
  - Óbidos, que está en el Amazonas de agua blanca, pasa a **punto de paso** con su dato;
  - la parada 7 es **solo el lago de Tefé**, negro como el mundo 3. Mamirauá es várzea de agua blanca, entre el Solimões, el Japurá y el Auati-Paraná, unos 150 km al noroeste [E wp_mamiraua, wp_varzea]: pasa a la tarjeta;
  - Allpahuayo-Mishana (el Nanay) y Pacaya-Samiria (el Samiria) llevan agua negra en un mundo de agua blanca: la cocha y la quebrada negras van en segundo plano, y la mini cuia del Muestrario se llena de agua negra. El espejo de Pacaya-Samiria viene de las aguas negras del Samiria [E actualidad_pacaya_48; mongabay_pacaya].
- **En 1-200 no hay** Parintins como parada (sus bois son marcas y tiene ritos representados), ni Fordlândia, ni la pororoca (su nombre choca con un comodín), ni **«Belén» como nombre de parada, rótulo o nivel** (07 §8.1, columna «No»: barrios pobres como títulos de nivel; §(e), vetos del mundo 5). La parada 15 se llama «Río Itaya (Iquitos)», como el hito 160 «Creciente del Itaya»; «Belén» solo puede salir dentro de la tarjeta, si el asesor lo aprueba.

#### 5.1 bis. Cruce con los candidatos y los vetos de §(e) (07 §2.2, regla 6)

Para los mundos 1-5, el canon fija que «los Datos del río candidatos y los vetos propios del mundo son los de la propuesta de D-007, §(e)» (07 §2.2, regla 6; `docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md`). La versión anterior de esta propuesta no la citaba. Ahora se cruza parada a parada; los ID «CR-F» son los de `docs/research/cultura-ribera/investigacion.md`, con su URL copiada en `rio-amazonas.json`.

| Mundo | Candidatos de §(e) | Cómo entran en el Mapa | Vetos de §(e) que se aplican |
|---|---|---|---|
| 1 Estuario | Ver-o-Peso, protegido desde 1977 [V iphan_pa_material; CR-F62 es Wikipedia]; el chibé [E CR-F72] | «Otro dato» de la tarjeta de Belém: `belem.dato_01` y `dato_02` | Peconha y trabajo infantil; objetos rituales de las bancas de hierbas (el héroe de Ver-o-Peso, sin ellas) |
| 2 Tapajós | Cuias de Santarém y ASARISAN, IPHAN 2015 [V CR-F59] y [E CR-F60]; playas de vaciante [NV] | La tarjeta de Santarém la lleva: `santarem.dato_01` [V CR-F59, MR-F97]. Las playas de vaciante no se usan hasta tener fuente | Copiar grafismos de las cuias |
| 3 Manaos | Encuentro, IPHAN 2010 [V CR-F25]; régua desde 1902 [V CR-F11]; flutuantes [E CR-F32]; matapi del Bajo Amazonas [V CR-F35]; manatí del medio Solimões [V CR-F148] | «Otro dato» del Encuentro (`encontro.dato_01`: el voto de 2010, no «patrimonio desde 2010») y de Manaos (`manaos.dato_03` y `dato_04`: régua y flutuantes). El manatí va en la tarjeta del lago de Tefé (`tefe.dato_03`), con el pirarucú de Mamirauá (`tefe.dato_02`). El matapi va con el debut del 63, no en el Mapa | La sequía de 2024 como juego (y, por la misma razón, la mortandad de botos de 2023 en el lago de Tefé); Parintins; la demolición de la Ciudad Flotante |
| 4 Trapecio | Rebojo [E CR-F47] (dato del Remolino, del Japurá en Brasil); el Japurá nace en Colombia como Caquetá [E CR-F145]; Tarapoto, sitio Ramsar [V CR-F105]; Puerto Nariño sin carros [E CR-F104]; loros de Leticia [E CR-F103] | Leticia y Puerto Nariño ya los usan como dato de llegada, con las dos fuentes; Tarapoto, como «Otro dato» (`tarapoto.dato_02`). El rebojo se queda en el debut del Remolino | Máscaras ticuna; Cobra Grande |
| 5 Loreto | Iquitos sin carretera [E CR-F80]; Belén sube 5-6 m [E CR-F28]; tortugas de la RNPS con crédito a las comunidades [V CR-F14]; pirarucú y su manejo [E CR-F44, CR-F91]; la gamitana come fruta [E CR-F90]; el aguaje con subidor [E CR-F122] | Iquitos sin carretera es el dato del 150; la creciente de 5-6 m va en la tarjeta del Itaya, sin nombrar Belén (`itaya.dato_01`); las tortugas, en la tarjeta de Pacaya-Samiria (`pacaya_samiria.dato_03`, taricaya; «charapa» solo con contexto, 07 §8.1) | Pasaje Paquito; caucho y Putumayo; mito de Eiffel; karuara; Yacumama; **Belén como nombre de nivel** |

- **Lo que sigue saliendo de esta propuesta** (no está en §(e)): el arrecife del 10 (que ya es canon de 06 §2.3), la Ilha do Amor, la respuesta del Tapajós, los 6 km del Encuentro, el nombre Solimões/Amazonas, el lago de Tefé, el 42 % de Colombia, Amacayacu, el río frontera, el bosque tropical del Perú, Allpahuayo, Nauta y Pacaya-Samiria. **Eso cambia la fuente canónica** de 07 §2.2, regla 6: se pregunta en **Q16** y se lista en la sección 15.

#### 5.1 ter. El «Otro dato» de cada tarjeta (2026-10-14)

Con D-013 (punto 11, Q16-A), los candidatos de §(e) entran como dato de llegada o como **«Otro dato» de la tarjeta de la parada** (sección 7). Están en `rio-amazonas.json`, campo `otros_datos` de cada parada (`{clave, texto, etiqueta, fuente, releida, origen, nota}`, y `args` si un texto llevara `{n}`), con el mismo formato que el dato de llegada. Las paradas sin «Otro dato» llevan la lista vacía. De ahí los toma `tools/river-map/stops.py` para `facts[]` y para el bloque del Mapa de `facts.csv` (M2.6; contrato 32). **Las URL de sus fuentes se reabrieron el 2026-10-14** (R5) y todo queda «pendiente del asesor» (07 §8.2).

| k · Parada | Clave | «Otro dato» (ES-419) | Car. | Etiqueta y fuente | Origen |
|---|---|---|---|---|---|
| 0 · Belém | `map.stop.belem.dato_01` | «Desde 1977, el conjunto de Ver-o-Peso, con sus mercados, es patrimonio de Brasil.» | 81 | [V] iphan_pa_material (IPHAN, Pará); CR-F62 | §(e) |
| 0 · Belém | `map.stop.belem.dato_02` | «El chibé es fariña con agua; las familias ribereñas lo toman solo o con pescado.» | 80 | [E] CR-F72 | §(e) |
| 1 · Santarém | `map.stop.santarem.dato_01` | «Desde 2015, el modo de hacer cuias del Bajo Amazonas es patrimonio cultural de Brasil.» | 86 | [V] CR-F59, MR-F97 | §(e) |
| 3 · Floresta Nacional do Tapajós | `map.stop.flona_tapajos.dato_02` | «La Floresta Nacional do Tapajós es un área protegida desde 1974.» | 64 | [E] wp_flona | §5.1 |
| 4 · Encuentro de las Aguas | `map.stop.encontro.dato_01` | «En 2010, el consejo de patrimonio de Brasil votó por unanimidad proteger este encuentro.» | 88 | [V] ufam_elaborar_2017 | §(e) |
| 5 · Manaos | `map.stop.manaos.dato_03` | «Desde 1902, en el puerto de Manaos se mide cada día la altura del río Negro.» | 76 | [V] CR-F11 | §(e) |
| 5 · Manaos | `map.stop.manaos.dato_04` | «Los flutuantes de Manaos son casas y negocios que flotan en el río; se llega en bote.» | 85 | [E] CR-F32 | §(e) |
| 6 · Anavilhanas | `map.stop.anavilhanas.dato_01` | «Anavilhanas tiene unas 400 islas: algunas solo aparecen cuando baja el río.» | 75 | [V] camara_anavilhanas_2008 | §5.1 |
| 7 · Lago de Tefé | `map.stop.tefe.dato_02` | «Cerca de aquí, en Mamirauá, los pescadores cuentan los pirarucús que suben a respirar.» | 86 | [V] uakari_2011_pirarucu; [E] CR-F44 | §5.1 |
| 7 · Lago de Tefé | `map.stop.tefe.dato_03` | «Cuando el agua baja, los manatíes de Mamirauá viajan a un refugio: el lago Amanã.» | 81 | [V] arraut2017_manati, CR-F148 | §(e) |
| 12 · Lagos de Tarapoto | `map.stop.tarapoto.dato_02` | «Son el primer humedal Ramsar de la Amazonia colombiana, logrado con sus comunidades.» | 84 | [V] MR-F38, CR-F105 | §(e) |
| 15 · Río Itaya (Iquitos) | `map.stop.itaya.dato_01` | «En la creciente, el Itaya sube 5-6 m y hay casas sobre balsas que flotan con él.» | 80 | [E] CR-F28, bme_habitatio_itaya | §(e) |
| 18 · Pacaya-Samiria | `map.stop.pacaya_samiria.dato_03` | «Las comunidades de Pacaya-Samiria cuidan nidos de taricaya y sueltan millones de crías.» | 87 | [V] minam_sinia_taricaya, CR-F14 | §(e) |

- **Recuento:** 13 «Otro dato» en 10 paradas, **9 [V] y 4 [E]** y ningún [NV]. Las otras 10 paradas no llevan (la tarjeta de Allpahuayo-Mishana nombra el ave, que no es un «Otro dato»). Ninguno lleva cifras con separador ni decimales (Q8-A): no hace falta `{n}`. Las claves siguen el glosario §10 y no se repiten con los datos de llegada; la de Santarém pasa a `dato_01`, porque la versión anterior citaba un `dato_01` [E] sin texto.
- **Cambios frente a la versión anterior:**
  - **Ver-o-Peso** sube a [V] con la página del IPHAN.
  - **El Encuentro** dice el voto unánime del Consejo Consultivo del IPHAN del 4-11-2010, no «patrimonio desde 2010». En 2017 la protección seguía sin homologar y estaba en los tribunales [V ufam_elaborar_2017], y la prensa la dio por oficializada en diciembre de 2017 [E portalamazonia_encontro_2017].
  - **El pirarucú** cambia de texto y de fuente: la noticia del BID (MR-F32) no se pudo reabrir. Ahora va a la revista del Instituto Mamirauá, con el crédito a los pescadores.
  - **Tarapoto** va sin año: la ficha Ramsar (designación de 2017) no se pudo reabrir, y MinAmbiente la anunció en 2018.
- **Para el asesor:**
  - el chibé dice «fariña» (harina gruesa de yuca), donde la fuente dice solo «farinha»;
  - el crédito a las artesanas de ASARISAN, en la tarjeta de Santarém;
  - los flutuantes, sin desalojos ni la Ciudad Flotante;
  - los nombres de los pueblos del resguardo Ticoya (ticuna, cocama y yagua) en Tarapoto;
  - el Itaya, sin nombrar el barrio y sin pobreza;
  - las taricayas, sin huevos, sin la parte que se vende y sin «charapa».
- **No se pudieron reabrir el 2026-10-14** (y ningún «Otro dato» se apoya en ellas): CR-F25 (Confea, conexión rechazada), MR-F32 (BID, 403) y ramsar_tarapoto (RSIS, 502).
- **`--check`** falla si una parada no tiene `otros_datos`, si una entrada no es un objeto o le falta un campo, si una clave no tiene la forma `map.stop.<lugar>.dato_NN` o se repite, si un marcador (también el del plural `{n:plural:…|…}`) no tiene su valor en `args`, si un texto pasa de 90 caracteres (con `{n}` resuelto y, en el plural, su forma más larga), lleva una cifra con separador o nombra «Belén», si la etiqueta no es [V] o [E], si un [V] no tiene fuente [V], si una fuente no se reabrió en la fecha de `releida` o si falta un candidato de esta sección.

### 5.2 Puntos de paso y miradores de 1-200

- **Puntos de paso:** solo un nombre en el río, que sale al pasar durante un viaje. Se tocan en el Mapa y, si tienen dato, lo cuentan. No tienen fondo, Canasta ni muestra.
  - **Belém → Santarém:** Breves (furos), Gurupá, boca del Xingu y Monte Alegre (sin la Pedra Pintada, que tiene pictografías).
  - **Hacia Manaos:** Óbidos (dato `obidos.dato_01`: el río se aprieta a unos 2 km [V MR-F21]), Parintins (solo el nombre), Itacoatiara y boca del Madeira.
  - **Hacia Tefé:** boca del Purus y Coari.
  - **Hacia Leticia:** Fonte Boa, Santo Antônio do Içá y Tabatinga.
  - **Hacia Iquitos:** Caballococha, Pebas y boca del Napo.
  - **Hacia Nauta:** Tamshiyacu.
- **Miradores** (el «secreto»): un «?» fuera de la ruta. El progreso los revela (sin estado nuevo) y en ese momento dan su muestra especial del Muestrario; al tocarlos, enseñan su dato (sección 4.2).

  | Mirador | Dato |
  |---|---|
  | Boca del Amazonas | El agua dulce se nota a cientos de km mar adentro [E MR-F03] |
  | Isla de Marajó | Unos 40.000 km² [V MR-F11] |
  | Macapá | La línea del ecuador [E MR-F15] |

  Los tres están en el estuario: el primer Mapa (nivel 10) ya invita a explorar. En la vuelta completa habrá uno o dos por mundo; por ejemplo, el Pongo de Manseriche (mundo 9) o la silueta de Chiribiquete, sin pictografías.

### 5.3 Países y cruces

Los niveles de los cruces **se derivan de las paradas** (cada una cubre 10 niveles, en orden; sección 5.4) y `generar_mapa.py --check` los comprueba (`cruces_de_pais`, campo `llega_en`).

| Cruce | Al ganar | Qué pasa [P] |
|---|---|---|
| Brasil (inicio) | 10 | Bandera de Brasil en la canoa del primer Mapa |
| Brasil → Colombia | 80 | Se iza la de Colombia. Placa «Colombia te da la bienvenida» y dato del país |
| Colombia → Perú | 140 | Se iza la del Perú en la boca del Atacuari; «El Perú te da la bienvenida» |
| Perú → Ecuador | 220 (Nuevo Rocafuerte, parada 22; la boca del Aguarico, la frontera, queda unos 20 km río abajo) | Ecuador. Placa «Ecuador te da la bienvenida» [P] (extensión de P-M7 al 220: D-023 punto 18 solo la fija en el 80 y el 140) y dato del país (sección 5.4 bis) |
| Brasil → Bolivia | 300 (viaje de entrada del mundo 7: Madeira, Villa Bella, Cachuela Esperanza y Riberalta) | Bolivia. **La parada 30 (Puerto Heath) está en Bolivia**, en el departamento de La Paz [E wp_puerto_heath], y lleva la bandera (`bandera` de la parada 30; sección 5.4 ter) |
| Bolivia → Perú | 310 (lago Sandoval, parada 31, aguas abajo de Puerto Maldonado; sección 5.4 ter) | El Perú otra vez, sin placa de bienvenida (es una vuelta) |
| Perú → Bolivia | 420 (viaje de entrada del mundo 8: Madre de Dios abajo por Puerto Heath). **La parada 42 (Riberalta) está en Bolivia** y lleva la bandera (`bandera` de la parada 42; sección 5.4 quater) | Bolivia, **con la tricolor**, su bandera nacional, de uso náutico [P]. La wiphala también es símbolo del Estado: el DS 0241/2009 la llama «símbolo sagrado» (art. 28) y no regula las banderas en embarcaciones [V lexivox_ds241, leído el 2026-10-15]; la Constitución de 2009 (art. 6.II) no se leyó. La elección la revisan el abogado y el asesor. Sin placa de bienvenida (es una vuelta) |
| Brasil → Venezuela | 780 (San Carlos de Río Negro) | Venezuela. En ese tramo el Negro también es frontera con Colombia [E]; Cucuí, la parada anterior, está en Brasil |
| Brasil → Guyana | 850 (Bonfim y Lethem, en el Takutu) | Guyana, **con revisión del asesor y del abogado** (zona del Esequibo) |
| Surinam y Guayana Francesa | 891-920 | Recuadro sin canoa ni bandera; la Guayana Francesa no tiene bandera propia |
| Colombia (Caño Cristales, mundo 12) | 941-950 | Recuadro de los «vecinos del Orinoco», sin canoa ni bandera |
| Vueltas a un país ya visitado | 560 (Perú), 700 (Brasil), 920 (Venezuela) y 960 (Perú) | En el viaje de entrada del mundo siguiente se iza otra vez la bandera del país al que se vuelve. La del 560 la lleva la parada 56, Jenaro Herrera, en el Perú (`bandera` de la parada 56; sección 5.4 quinquies). Están en `cruces_de_pais` con `"vuelta": true` (también el 310 y el 420) y la muestra no repite su mástil |

- **Bienvenidas sin género:** «te da la bienvenida», no «¡Bienvenido!» (propuesta de TiTino, e.2, regla 3).
- **Solo banderas oficiales,** y nunca en tierra, en islas ni en zonas reclamadas. El uso comercial de símbolos nacionales lo revisa el abogado [NV].
- **El Madeira** es frontera Bolivia-Brasil desde Villa Bella hasta la boca del Abunã [E wp_madeira]: en `rio-amazonas.json` lleva los dos países.

### 5.4 Resumen de 201-1.000 (80 paradas)

- **Si la 1.0 es la del GDD (Q14-A), 201-700 es contenido de la 1.0:** 50 paradas más (k = 20-69), con datos [V] o [E] revisados, rutas por agua (Napo, Madeira, Beni, Madre de Dios, Mamoré y Ucayali), las banderas de Ecuador y Bolivia y los viajes de mundo del 200, el 300, el 420 y el 560. Sus fondos llevan el kit del mundo y el rótulo, **sin héroe**, por PAD (`world_w06` … `world_w09`), así que **en 201-700 no hay fondo propio por sitio** (sección 10.4). La investigación y los datos cuestan unos 5,5 días de agente y los 4 kits, unos 8 de artista (secciones 10.8 y 13).
- **Sitios:** candidatos, por confirmar con investigación y con el asesor; las coordenadas de los que las tienen están en `rio-amazonas.json`.
- **Cuántos fondos (cuando lleguen sus packs):** los sitios que duran 2-3 paradas comparten fondo con otra luz, así que salen **unos 50 sitios con fondo propio** [E].

| Mundo · niveles · paradas | Viaje de entrada (por agua) | Sitios, río arriba | Bandera | Pregunta del mundo | Cuidado |
|---|---|---|---|---|---|
| 6 Oriente · 201-300 · 10 | Pacaya-Samiria → Nauta → Iquitos → boca del Napo → Napo arriba | Napo peruano ×2 (Mazán y Santa Clotilde); Nuevo Rocafuerte; Yasuní ×2; lagunas de Cuyabeno ×2 (desvío por el Aguarico); El Coca ×2 (la boca del río Coca y la ciudad); Napo alto y Misahuallí. **Detalladas en la sección 5.4 bis** | Ecuador (220) | Se plantea en el 200: «¿Cuántas especies de árbol caben en una hectárea?» → más de 655 en una hectárea de Yasuní («upwards of 655», Bass y otros, 2010) [V bass2010_yasuni]; la responde la llegada del 230 | Sin petróleo, sin la consulta de 2023 y sin pueblos aislados; §(e): ritual de la guayusa y maito sin crédito (lista completa para el asesor en 5.4 bis) |
| 7 Madre de Dios · 301-420 · 12 | Napo abajo → **repaso de lo ya lleno** → Madeira (rápidos de Teotônio: relevo del guía, punto de paso `teotonio`) → Villa Bella → Beni (Cachuela Esperanza, Riberalta) → Madre de Dios → Puerto Heath | Puerto Heath (Bolivia); lago Sandoval ×2; Puerto Maldonado ×2; collpa del Tambopata ×3; Boca Manu ×2; Manu alto ×2. **Detalladas en la sección 5.4 ter** | Bolivia (300) y Perú (310) | Se plantea en el 300: «¿Por qué los guacamayos comen arcilla?» → la hipótesis más apoyada: buscan la sal [E npr2017_collpas]; la responde la llegada del 350 | Sin minería; el istmo de Fitzcarrald no es ruta (era del caucho); tinaja en el 323; con Q18-A, Rosa se queda en Teotônio y guía el bufeo boliviano (sección 6.1); lista completa para el asesor en 5.4 ter |
| 8 Madidi · 421-560 · 14 | Madre de Dios abajo → Riberalta → Beni arriba; después Beni abajo (Cachuela Esperanza) → Mamoré arriba (Guayaramerín) → Iténez (ida y vuelta) → Mamoré arriba | Riberalta ×3; Rurrenabaque ×2; Madidi ×3; Guayaramerín; Iténez y Noel Kempff ×2 (Flor de Oro); Trinidad y Llanos de Moxos ×3. **Detalladas en la sección 5.4 quater** | Bolivia (420) | Se plantea en el 420: «¿Cómo cabe tanta vida en un solo parque?» → va de la selva a más de 5.700 m [E wp_madidi]; la responde la llegada del 470 | Casarabe y la arqueología de los Llanos, con el asesor; la bandera (sección 5.3); dos bajadas de km por una confluencia (`baja_por_confluencia` [P], pregunta abierta para el dueño en 5.4 quater); lista completa para el asesor en 5.4 quater |
| 9 Ucayali · 561-700 · 14 | Mamoré abajo → Madeira abajo (rápidos de Teotônio: vuelve Rosa, punto de paso `teotonio`) → Amazonas arriba (repaso) → Nauta → Ucayali arriba | Jenaro Herrera; Requena ×2; Contamana ×2; Pucallpa y Yarinacocha ×3; Bolognesi ×2; Atalaya, donde nace el Ucayali, ×2; Tambo bajo (Puerto Prado) ×2. **Detalladas en la sección 5.4 quinquies** | Perú (560) | Se plantea en el 560: «Dos ríos: ¿cuál nos lleva a donde nace el agua?» (cabecera del 560, sección 6.3; la contesta la llegada a Jenaro Herrera: el Ucayali). Del mundo, se plantea en el 560: «¿Por qué el río dibuja tantas curvas?» → Yarinacocha fue una curva del Ucayali [E CR-F147]; la responde la llegada del 620 | **Shanay-Timpishka:** el canon la pone como paisaje con vapor (glosario §8; 07 §2.1). La fuente, releída el 2026-10-16, no la llama lugar sagrado: cuenta que los chamanes locales atribuyen el agua hirviente a la Yacumama, que §(e) veta, y que a su orilla hay tres comunidades (Mayantuyacu, Santuario Huishtín y el centro Shanay-timpishka) [E wp_shanay]. **Pregunta para el asesor:** ¿parada, solo vapor lejano en el fondo (la alternativa del canon) o nada? Comunidades shipibo-konibo con el asesor; lista completa para el asesor en 5.4 quinquies |
| 10 Alto Negro · 701-820 · 12 | Ucayali → Amazonas → Encuentro → Negro arriba (Anavilhanas ya llena) | Jaú ×2; Barcelos y Mariuá ×2; Santa Isabel; São Gabriel ×2 (asesor); Cucuí; San Carlos de Río Negro ×2; el Casiquiare ×2 | Venezuela (780) | «¿Puede un río unir dos cuencas?» → el Casiquiare [E wp_casiquiare] | La Neblina solo como silueta, sin su sentido yanomami |
| 11 Escudo de Guayana · 821-920 · 10 | Negro abajo → Branco → Boa Vista → Takutu | Caracaraí; Boa Vista ×2; Bonfim y Lethem ×2; Rupununi ×2; recuadro (891-920): Voltzberg ×2 y Saül | Guyana (850; asesor y abogado) | «¿Toda esta selva va al Amazonas?» → no: algunos ríos van directo al mar [E] | Esequibo: ni líneas ni bandera en tierra |
| 12 Invitados del Orinoco · 921-960 · 4 | Takutu → Branco → Negro → **Casiquiare** → Orinoco (recuadro) | Orinoco alto; Puerto Ayacucho; Caño Cristales (Colombia, recuadro); Salto Ángel | Venezuela; Colombia sin bandera (recuadro) | «¿Este río también es del Amazonas?» → no, es del Orinoco [E wp_orinoco] | Rótulo «vecinos del Orinoco» siempre |
| 13 El origen · 961-1.000 · 4 | **Gran viaje** por agua ya llena: Orinoco → Casiquiare → Negro → Amazonas → Nauta → Ucayali → Tambo → Ene → Apurímac. Rosa se despide al pie de los Andes. Se plantea en el 960: «Dos ríos: ¿cuál nos lleva a donde nace el agua?» | Cañón del Apurímac (961); puna alta (971); quebradas Apacheta y Carhuasanta (981); **nevado Mismi** (991), una de las nacientes; meta en el 1.000 | Perú | «¿De dónde sale tanta agua?» → de la lluvia de toda la selva; aquí arriba solo nace un hilo [E mit_ciclo_agua, wp_source] | Sin Choquequirao (ruinas); Q'eswachaka solo con el asesor (ritual) |

- **Datos escritos:** las paradas de 1-200 tienen dato, y las de 201-300 (mundo 6), 301-420 (mundo 7) y 421-560 (mundo 8) también, desde el 2026-10-15 (secciones 5.4 bis, 5.4 ter y 5.4 quater), y las de 561-700 (mundo 9), desde el 2026-10-16 (sección 5.4 quinquies). Para 701-1.000 faltan sitios con dato verificado y coordenadas. **Con Q14-A, la parte de 201-700 es trabajo de la 1.0** (fase F9 de la sección 13: k = 20-29 hechas en M3.7a, k = 30-41 en M3.7b, k = 42-55 en M3.7c y k = 56-69 en M3.7d, así que F9 está completa); el resto, de la 1.1 en adelante.
- **Temporadas de 201+:** hay que rehacer la tabla de D-009 (d.5), y los fondos 201+ se pintan después de fijarla. *Actualización del 2026-10-14:* D2 ya la rehízo (d.5 de la propuesta de mecánicas; se aplica por D-023, punto 10); las temporadas de cada ciclo están en su punto 4 y son las mismas en las variantes A y B.
- **Cachuela Esperanza** está en el Beni aguas abajo de Riberalta (−10,54, −65,60; Riberalta, −11,01, −66,07): ya no es parada del mundo 8 sino punto de paso del viaje del 300 y del tramo Beni abajo. Su parada pasa a Riberalta (×3).

#### 5.4 bis. Paradas de 201-300: mundo 6, Oriente (2026-10-15, M3.7a)

Con Q14-A (D-013, punto 2), la fase F9 detalla las paradas de 201-700. Están en `rio-amazonas.json`, lista `paradas_201_700`, con el mismo registro que las de 1-200 y dos campos más: `sitio` (el sitio del resumen de 5.4 que la contiene) y `wikidata` (el elemento de su coordenada). De ahí las toma `tools/river-map/stops.py` en M4.5a, con la conversión de M1.1. Esta es la parte 1 (k = 20-29); las partes 2-4 (k = 30-69) siguen el mismo formato. **Las coordenadas son de Wikidata y las URL de los datos se reabrieron el 2026-10-15** (R5). Todo nombre, dato y texto queda «pendiente del asesor» (07 §8.2).

- **Columnas:** las de 5.1.
  - **Temp.** es la del punto 4 de d.5 de la propuesta de mecánicas (D2, aplicada por D-023, punto 10): A en 201-240 y 281-300, B en 241-280. Es la misma en las variantes A y B.
  - **Km a casa** es el mismo [cálculo, mínimo] de 5.1: la boca del Napo cae en el km 3.596 del cauce, y a eso se suma el recorrido sin meandros por la poligonal del Napo, que mide 735 km frente a los 1.075-1.130 reales [E CR-F77]. En Cuyabeno se suman además el Aguarico y el Cuyabeno.
- **Coincidencias:** banner del 201, debut del Cacao (glosario §9), miquito del mundo 6 (07 §6.4) y estrenos e hitos de d.4 y d.5.2 [P].

| k | Niveles | Llega | Mundo · tipo | Sitio | País · río · agua del sitio | Km a casa | Temp. | Dato de llegada | Coincidencias |
|---|---|---|---|---|---|---|---|---|---|
| — | Viaje 200 → 201 | | | Pacaya-Samiria → Nauta → Iquitos → boca del Napo → Mazán | Por el Perú, río abajo | | | Se llena la boca del Napo y el Napo hasta Mazán. Sin bandera nueva | Pregunta del mundo 6 |
| 20 | 201-210 | 200 | 6 Oriente · gran parada | **Mazán** (Napo peruano) | PE · Napo · blanca | ≈ 3.665 | A | En el **200**: `map.myth.source`, «El Amazonas no tiene una sola naciente: la ciencia aún discute cuál es la más lejana.» [E wp_source, natgeo2014]. Plantea la pregunta del mundo 6 | Banner 201; Cacao 201; miquito 202 (cotoncillo colorado) |
| 21 | 211-220 | 210 | 6 · parada | **Santa Clotilde** | PE · Napo · blanca | ≈ 3.795 | A | `santa_clotilde.dato_01`, «Aquí el Napo lleva casi el triple de agua que en la frontera con Ecuador.» [E CR-F77, con cálculo] | Hito 220 [P] (Barro con Aguas turbias y una Cuia chica) |
| 22 | 221-230 | 220 | 6 · parada | **Nuevo Rocafuerte** | EC · Napo · blanca | ≈ 4.060 | A | `country.ec.dato_01`, «En Ecuador, a la Amazonia también la llaman “el Oriente”: queda al este de los Andes.» [E eswp_amazonia_ec]. Antes, la placa «Ecuador te da la bienvenida» [P] (extensión de P-M7 al 220). **Bandera de Ecuador** | Lluvia con Brote 229 |
| 23 | 231-240 | 230 | 6 · parada | **Yasuní** (asesor) | EC · Napo · blanca | ≈ 4.145 | A | **Respuesta del mundo 6:** «En una hectárea típica de Yasuní crecen más de {n} especies de árbol.» (655) [V bass2010_yasuni]. Tarjeta: parque nacional desde 1979 y reserva de la biosfera desde 1989 [V] | Hito 240 [P] |
| 24 | 241-250 | 240 | 6 · parada | **El dosel de Yasuní** (asesor) | EC · Napo · blanca | ≈ 4.145 | B | `yasuni.dato_03`, al menos 596 especies de aves [V bass2010_yasuni]. Tarjeta: sus 150 especies de anfibios, récord mundial entre lugares parecidos [V] | Estreno de la Liana de cuenta 243 |
| 25 | 251-260 | 250 | 6 · parada | **Lagunas de Cuyabeno** (desvío por el Aguarico; asesor) | EC · Cuyabeno · **negra** | ≈ 4.195 | B | `cuyabeno.dato_01`, el mayor sistema de lagunas tropicales de Ecuador: 14 unidas por canales [V enfoqueute2016_cuyabeno]. Tarjeta: el humedal Ramsar más grande de Ecuador, declarado en 2017 [E] | Hito 260 [P] |
| 26 | 261-270 | 260 | 6 · parada | **Bosque inundado de Cuyabeno** (asesor) | EC · Cuyabeno · **negra** | ≈ 4.195 | B | `cuyabeno.dato_03`, «Las lagunas con bosque inundado parecen ser la guardería de los delfines de Cuyabeno.» [V denkinger2010_cuyabeno] | Piraña con Brote 266; Rosa con sus parientes [P] |
| 27 | 271-280 | 270 | 6 · parada | **Boca del río Coca** (asesor) | EC · Coca y Napo · blanca | ≈ 4.245 | B | `boca_del_coca.dato_01`, «En esta ciudad, a la que también llaman El Coca, el río Coca se une al Napo.» [E wp_el_coca, wp_coca_river] | Hito 280 [P] |
| 28 | 281-290 | 280 | 6 · parada | **El Coca** (asesor) | EC · Napo · blanca | ≈ 4.245 | A | `el_coca.dato_01`, «En el Napo, las canoas hacen de taxi: desde las cinco de la mañana van y vienen.» [E CR-F78; candidato de §(e)] | Estreno de la Hoja atada 283 |
| 29 | 291-300 | 290 | 6 · cierre | **Napo alto y Misahuallí** (asesor) | EC · Napo · blanca | ≈ 4.345 | A | `misahualli.dato_01`, «El Napo nace en las laderas de volcanes de Ecuador, como el Cotopaxi y el Antisana.» [E CR-F77; candidato de §(e)] | Hito 300 «Cuyabeno» [P], que se juega aquí y no en Cuyabeno (el asesor puede renombrarlo, D-023 punto 21); skin Borde de Pastaza (1.1); fin del mundo 6 |

- **Recuento:**
  - **10 paradas en 6 sitios,** los del resumen de 5.4 y en su orden, en 2 países: el Perú (201-220) y Ecuador (221-300);
  - **1 bandera izada:** Ecuador al ganar el 220 (`bandera` de la parada 22 y su cruce en `cruces_de_pais`);
  - **10 datos de llegada, 4 [V] y 6 [E],** y **4 «Otro dato», 2 [V] y 2 [E].** Ningún [NV]. Las cifras van como `{n}` con su valor en `args` (Q8-A); los años y la hora («las cinco») no.
  - **Todas las paradas llevan el fondo sin héroe** de Q14-A: `bg_kit_w06` con una pieza del kit (`kit:<pieza>`, tipo `kit_y_rotulo`) y el rótulo del sitio. Las piezas son [P] y son la lista para el artista del kit: napo ancho con lanchas, várzea peruana, río frontera con baliza, dosel de noche, dosel con aves, laguna negra con igapó, igapó con delfines, confluencia del Coca, Napo con canoas y piedemonte andino.
- **Cambios frente al resumen de 5.4** (también en `paradas_201_1000`, mundo 6):
  - **Yasuní:** la coordenada es la de Wikidata del parque, −0,77, −76,10 (antes, −1,0, −76,0 aproximada). Queda a unos 9 km del Napo de respaldo, dentro de los 15 km del canon, sin punto de atraque.
  - **Cuyabeno:** la parada está en la Laguna Grande (Wikidata Q131248042, que viene de GeoNames), no en el punto de Wikidata de la reserva (−0,1167, −75,8333), que no cae en las lagunas. Para llegar hacen falta **dos ríos locales nuevos**, el Aguarico y el Cuyabeno, con sus bocas de Wikidata y sus tramos en línea recta de respaldo. Van en `rios_locales_201_700`, con `boca_en` (el Napo y el Aguarico), y no en `rios`, para que `tools/river-map` siga en verde hasta que M4.5a les dé anfitrión en `geometry.py` (`HOSTS`).
  - **El Coca ×2:** la boca del río Coca (Wikidata Q1104914) va antes que la ciudad (Q1014564), porque queda unos 300 m más abajo por el Napo. Así los km no retroceden.
  - **Segundas paradas de un sitio:** la variante de luz de 1-200 (`variante_lut`) necesita un héroe, y en 201-700 no lo hay. Por eso «El dosel de Yasuní» y «Bosque inundado de Cuyabeno» llevan otro nombre [P], otra pieza del kit y otro dato.
  - **Nuevo Rocafuerte:** el sitio del resumen pasa a la coordenada de Wikidata de la parroquia (Q647791), −0,9333, −75,4, la de la parada 22 (antes, −0,9317, −75,3875, el punto «Boca del Yasuní / Nuevo Rocafuerte» del Napo de respaldo).
  - **Mazán:** Wikidata solo tiene el distrito (Q6799128), cuya capital está en la orilla del Napo, a unos 40 km de su boca por el río [cálculo].
- **Km:** el viaje del 200 deshace camino río abajo, de Pacaya-Samiria (≈ 4.025) a la boca del Napo, así que los km a casa bajan al entrar en el mundo, como en todo viaje que vuelve sobre lo ya lleno. Dentro del mundo no retroceden. El desvío del 250 baja por el Napo desde Yasuní hasta la boca del Aguarico, unos 20 km bajo Nuevo Rocafuerte, y sube por el Aguarico y el Cuyabeno: es una ida y vuelta por las confluencias, como la de Anavilhanas (sección 2.2), y la Laguna Grande queda más lejos de casa que Yasuní.
- **Pregunta del mundo 6:** se plantea en el 200 (`pregunta_del_mundo` de la parada 20) y la responde la llegada del 230 a Yasuní (parada 23).

**«Otro dato» de las tarjetas** (`otros_datos`, con el formato de 5.1 ter; longitudes medidas con el script de `--check`, con `{n}` resuelto):

| k · Parada | Clave | «Otro dato» (ES-419) | Car. | Etiqueta y fuente | Origen |
|---|---|---|---|---|---|
| 20 · Mazán | `map.stop.mazan.dato_01` | «Cerca de su boca, en Mazán, el Napo lleva de media unos {n} m³ de agua por segundo.» (6.800) | 85 | [E] CR-F77 | §(e) |
| 23 · Yasuní | `map.stop.yasuni.dato_02` | «Yasuní es parque nacional desde 1979 y reserva de la biosfera de la UNESCO desde 1989.» | 86 | [V] bass2010_yasuni | §5.4 bis |
| 24 · El dosel de Yasuní | `map.stop.yasuni.dato_04` | «Las {n} especies de anfibios de Yasuní son un récord mundial entre lugares parecidos.» (150) | 85 | [V] bass2010_yasuni | §5.4 bis |
| 25 · Lagunas de Cuyabeno | `map.stop.cuyabeno.dato_02` | «Estas lagunas son parte del humedal Ramsar más grande de Ecuador, declarado en 2017.» | 84 | [E] iucn_ramsar_ec, eloriente_ramsar_2017 | §5.4 bis |

- **Candidatos de §(e) del mundo 6** (07 §2.2, regla 6: en los mundos 6-13 son una lista para el asesor):
  - **El Napo** [F77]: el dato de llegada del 290 (nace en las laderas del Cotopaxi y el Antisana) y el «Otro dato» de Mazán (su caudal);
  - **las canoas-taxi de Coca** [F78]: el dato de llegada del 280;
  - **el aguapé nativo** [F109]: no entra en el Mapa. Era el dato de su estreno en el 223, y la tabla d.5.2 ya no lo estrena en el mundo 6.

  §(e) los daba con [V]; aquí van con [E], porque las fuentes son una página de Wikipedia y una noticia de prensa (cabecera de esta propuesta).
- **Vetos del mundo 6, para el asesor** (§(e), el resumen de 5.4 y el cuidado de `paradas_201_1000`; también en `nota_dato` de cada parada):
  - el ritual de la guayusa (la fuente de §(e) describe a los achuar: no se atribuye a otro pueblo);
  - los pueblos en aislamiento (tagaeri y taromenane) y su zona intangible;
  - el petróleo, sus derrames y la consulta de 2023;
  - el maito sin crédito;
  - los nombres en kichwa u otras lenguas, solo con un hablante.
- **Además, para el asesor:**
  - **Yasuní:** el texto solo habla de árboles, aves y anfibios, sin pueblos ni comunidades;
  - **Cuyabeno:** sin nombrar las comunidades siona, secoya, cofán, kichwa y shuar de la reserva, y sin las lagunas secas de 2023-2024 (riesgo R3, como la sequía de 2024 del mundo 3): la laguna del fondo siempre está llena;
  - **El Coca:** el artículo de las canoas nombra comunidades shuar, kichwa y huaorani, y el texto no las nombra; sin la cascada de San Rafael ni la erosión del río Coca de 2020;
  - **Napo alto:** sin turismo con fauna en cautiverio;
  - **los nombres nuevos** [P]: «El dosel de Yasuní», «Bosque inundado de Cuyabeno» y «Boca del río Coca»; y el hito 300, «Cuyabeno» [P], que se juega en Misahuallí;
  - **Rosa con sus parientes en Cuyabeno** [P], como en Tarapoto. Q18 solo cambia los mundos 7 y 8.
- **No se pudieron reabrir el 2026-10-15:** la ficha Ramsar del sitio 2332 (RSIS dio 502 y la copia del PNUMA, 404) ni la página de Ramsar de Ecuador (403). Por eso el «Otro dato» de Ramsar va con [E]: «el más grande de Ecuador» es de la UICN [V], pero el año (2017) solo sale de la prensa.
- **`--check`** (`revisar_paradas_201_700`) falla si:
  - la k no sigue desde el 20, si los niveles o la llegada no cuadran, si el mundo no es el de `WorldTable`, o si el tipo no es gran parada en la primera del mundo, cierre en la última y parada en las demás;
  - falta la coordenada [V] o [E] o su elemento de Wikidata, o si `ancla_rio` o un río local no existen;
  - `km_casa` no es `km_desde_el_mar` + 29, o si los km a casa retroceden dentro del mundo (desde M3.7c, salvo en una bajada declarada por una confluencia [P]: sección 5.4 quater);
  - falta el dato de llegada, si su clave no es `map.stop`/`map.country`…`dato_NN`, `map.myth.<tema>` ni `map.general.<id>` (las formas de `fact_key` del contrato 30, como el `map.general.dato_negra` de Anavilhanas), si el del 200 no es `map.myth.source` (08 §4.5), si pasa de 90 caracteres, si no es [V] o [E], si una fuente no existe o no se reabrió en su `releida`, o si un [V] no tiene fuente [V];
  - el fondo no es `bg_kit_wNN` con `kit:<pieza>`;
  - la pregunta del mundo pasa de 50 caracteres, si no es la del resumen o si no se responde en una llegada posterior del mismo mundo;
  - una bandera no tiene su cruce de país en el nivel de llegada (o al revés);
  - los sitios de un mundo detallado no son los del resumen, en su orden y con sus paradas;
  - una parada lleva un [NV] o nombra «Belén».

  El «Otro dato» de 201-700 pasa por la misma revisión que el de 1-200 (5.1 ter), con las claves sin repetir entre las dos listas. La autoprueba exige que las paradas de verdad no den ningún error y que cada caso fijo dé uno nuevo a nombre de la parada, el mundo o el cruce que cambia: el dato del 200 con otra clave, un [NV] en una nota, un héroe pintado, los km a casa que retroceden, los sitios fuera de orden, la bandera de Ecuador ausente en el 220 y una bandera sobrante; y que un dato de llegada `map.general.*` no dé ninguno.
- **Para M4.5a** (`stops.py` y `geometry.py`):
  - `kind`: la gran parada, la parada y el cierre, como en 1-200;
  - `arrival_fact`: la clave del dato de cada parada (en n = 20, `map.myth.source`);
  - `flag_raise`: EC en n = 22 (el cruce lleva `llega_en` {mundo 6, sitio «Nuevo Rocafuerte»});
  - `world_question` en n = 20 y `answers_world_question` en n = 23;
  - `bg.code`: `kit:<pieza>`;
  - `HOSTS`: `aguarico` → `napo` y `cuyabeno` → `aguarico`;
  - `sample`: black en n = 25 y 26.

#### 5.4 ter. Paradas de 301-420: mundo 7, Madre de Dios (2026-10-15, M3.7b)

Parte 2 de la fase F9 (k = 30-41), con el formato de 5.4 bis: las paradas están en `paradas_201_700` de `rio-amazonas.json`, con `sitio` y `wikidata`. **Las coordenadas son de Wikidata y las URL de los datos se reabrieron el 2026-10-15** (R5). Todo nombre, dato y texto queda «pendiente del asesor» (07 §8.2).

- **Columnas:** las de 5.4 bis.
  - **Temp.** es la del punto 4 de d.5 de la propuesta de mecánicas (D2, aplicada por D-023, punto 10): A en 301-320, 341-360 y 381-420; B en 321-340 y 361-380.
  - **Km a casa** sigue el mismo [cálculo, mínimo]: la boca del Madeira cae en el km 1.298 del cauce, y se suman las poligonales del Madeira hasta Villa Bella (1.140 km, frente a unos 1.450 reales [E wp_madeira]), del Beni hasta Riberalta y del Madre de Dios río arriba. El Tambopata y el Manu van en línea recta de su boca a la parada.
- **Coincidencias:** banner del 301, Tinte Arazá (el último, con la paleta completa), miquito del mundo 7 (pichico emperador, 07 §6.4), estrenos e hitos de d.4 y d.5.2 [P].

| k | Niveles | Llega | Mundo · tipo | Sitio | País · río · agua del sitio | Km a casa | Temp. | Dato de llegada | Coincidencias |
|---|---|---|---|---|---|---|---|---|---|
| — | Viaje 300 → 301 | | | Misahuallí → Napo abajo → Amazonas (repaso) → Madeira → **rápidos de Teotônio** → Villa Bella → Beni → Riberalta → Madre de Dios → Puerto Heath | Por Brasil y Bolivia | | | Se llenan el Madeira, el Beni hasta Riberalta y el Madre de Dios hasta Puerto Heath. En `teotonio`, Rosa se queda y guía el bufeo boliviano (Q18-A; P-M8). **Bandera de Bolivia** | Pregunta del mundo 7 |
| 30 | 301-310 | 300 | 7 Madre de Dios · gran parada | **Puerto Heath** (Bolivia) | BO · Madre de Dios · blanca | ≈ 2.905 | A | `country.bo.dato_01`, «Bolivia declaró en 2012 a su delfín de río tesoro nacional, con una ley que lo protege.» [E wp_boto]. Plantea la pregunta del mundo 7. **Bandera de Bolivia** | Banner 301; Tinte Arazá 301; miquito 302 |
| 31 | 311-320 | 310 | 7 · parada | **Lago Sandoval** | PE · Madre de Dios · blanca | ≈ 2.950 | A | `sandoval.dato_01`, «El lago Sandoval es una cocha: una vuelta vieja del río que quedó separada como lago.» [E wp_lake_sandoval]. **Bandera del Perú** (vuelta) | Hito 320 [P] (primer trío: Hoja atada + Lluvia + Brote) |
| 32 | 321-330 | 320 | 7 · parada | **Aguajal de Sandoval** | PE · Madre de Dios · blanca | ≈ 2.950 | B | `sandoval.dato_03`, «En las orillas del lago Sandoval crecen aguajes, palmeras donde viven guacamayos.» [E wp_tambopata_nr]. Tarjeta: los cuatro datos de Pará de la tinaja (pregunta 50 (a)) | **Estreno de la Tinaja 323** |
| 33 | 331-340 | 330 | 7 · parada | **Puerto Maldonado** | PE · Madre de Dios y Tambopata · blanca | ≈ 2.960 | B | `puerto_maldonado.dato_01`, «Puerto Maldonado está donde el río Tambopata se une al Madre de Dios.» [E CR-F96] | Hito 340 [P] |
| 34 | 341-350 | 340 | 7 · parada | **Astilleros de Puerto Maldonado** | PE · Madre de Dios y Tambopata · blanca | ≈ 2.960 | A | `puerto_maldonado.dato_03`, «En Puerto Maldonado, el turismo y la construcción de botes dan mucho trabajo.» [E CR-F96; candidato de §(e)] | Estreno del Brote de dos hojas 343 |
| 35 | 351-360 | 350 | 7 · parada | **Collpa del Tambopata** (asesor) | PE · Tambopata · blanca | ≈ 3.035 | A | **Respuesta del mundo 7:** «Según la idea más apoyada, los guacamayos comen arcilla para tomar la sal que les falta.» [E npr2017_collpas] | Hito 360 [P] |
| 36 | 361-370 | 360 | 7 · parada | **Los guacamayos del Tambopata** (asesor) | PE · Tambopata · blanca | ≈ 3.035 | B | `collpa_tambopata.dato_03`, «A la collpa Colorado llegan cada día loros de hasta {n} especies, a veces cientos.» (18) [E npr2017_collpas] | Piraña con Brote de dos hojas 366 |
| 37 | 371-380 | 370 | 7 · parada | **Nidos del Tambopata** (asesor) | PE · Tambopata · blanca | ≈ 3.035 | B | `collpa_tambopata.dato_05`, «Aquí, los guacamayos azul y amarillo anidan casi siempre en palmeras de aguaje muertas.» [E wp_macaw_society] | Hito 380 [P] |
| 38 | 381-390 | 380 | 7 · parada | **Boca Manu** (asesor) | PE · Madre de Dios y Manu · blanca | ≈ 3.175 | A | `boca_manu.dato_01`, «En Boca Manu, el río Manu se junta con el Alto Madre de Dios.» [E eswp_madre_de_dios, eswp_rio_manu] | Hoja atada con Barro 389 |
| 39 | 391-400 | 390 | 7 · parada | **El Alto Madre de Dios** (asesor) | PE · Alto Madre de Dios · blanca | ≈ 3.175 | A | `boca_manu.dato_02`, «Río arriba de aquí, el Madre de Dios se llama Alto Madre de Dios y baja de los Andes.» [E eswp_madre_de_dios] | Hito 400 [P] |
| 40 | 401-410 | 400 | 7 · parada | **Manu alto** (asesor) | PE · Manu · blanca | ≈ 3.240 | A | `manu.dato_01`, «El Manu es parque nacional desde 1973 y Patrimonio Natural de la Humanidad desde 1987.» [V sernanp_manu] | Estreno de la Flor de vitória-régia 403 |
| 41 | 411-420 | 410 | 7 · cierre | **Las cochas del Manu** (asesor) | PE · Manu · blanca | ≈ 3.240 | A | `manu.dato_03`, «En las cochas del Manu, lagos con forma de herradura, vive el lobo de río.» [V sernanp_manu] | Hito 420 «Zafra» [P], que no nombra un lugar (D-020, 28 a); fin del mundo 7 |

- **Recuento:**
  - **12 paradas en 6 sitios,** los del resumen de 5.4 y en su orden, en 2 países: Bolivia (301-310) y el Perú (311-420);
  - **2 banderas izadas:** Bolivia al ganar el 300 (`bandera` de la parada 30; el cruce llega con el viaje de entrada del mundo 7) y el Perú al ganar el 310 (`bandera` de la parada 31; cruce `llega_en` {mundo 7, sitio «Lago Sandoval»}, con `vuelta`);
  - **12 datos de llegada, 2 [V] y 10 [E],** y **14 «Otro dato», 5 [V] y 9 [E]** (la parada 38, Boca Manu, no lleva ninguno), más el dato del punto de paso `teotonio` [E]. Ningún [NV]. Las cifras van como `{n}` con su valor en `args` (Q8-A); los años no.
  - **Todas las paradas llevan el fondo sin héroe** de Q14-A: `bg_kit_w07` con una pieza del kit y el rótulo del sitio, a la hora del mundo 7 (el amanecer, la hora de la collpa; 07 §2.1). Las piezas [P] son la lista para el artista del kit: boca del Heath con baliza, cocha quieta, orilla de aguajes, confluencia del Tambopata, botes en la orilla, pared de arcilla, guacamayos en vuelo, aguajal con nidos, confluencia del Manu, piedemonte del Madre de Dios, selva del Manu y cocha con lobos de río.
- **Cambios frente al resumen de 5.4** (también en `paradas_201_1000`, mundo 7):
  - **El lago Sandoval va antes que Puerto Maldonado.** El lago está a unos 16 km de la ciudad y se llega a él por un sendero desde el Madre de Dios [E wp_lake_sandoval]; su coordenada cae aguas abajo, en el tramo hacia Puerto Heath. Con el orden anterior (Puerto Maldonado ×2 y después Sandoval ×2), los km a casa retrocedían dentro del mundo. Por eso el Perú se iza en el 310 al llegar al lago, no a la ciudad: el cruce cambia de sitio y de coordenada, pero no de nivel. La Tinaja se estrena en el 323, en la segunda parada del lago (k = 32), como pide la pregunta 50.
  - **Coordenadas que faltaban:** la collpa Colorado no tiene elemento propio en Wikidata. Va en la del Tambopata Research Center (Q115641210), junto a la collpa, donde la Macaw Society tuvo su sede de 1989 a 2020 [E wp_macaw_society]. El Manu alto va en la Estación Biológica de Cocha Cashu (Q5139274), en una cocha del Manu, porque la coordenada del parque (Q390264) no está sobre el río. Ningún sitio del resumen queda con [NV].
  - **Coordenadas de Wikidata** en Puerto Heath (Q6092075), el lago Sandoval (Q1800405), Puerto Maldonado (Q541435) y Boca Manu (Q27051064). Antes eran los puntos de la poligonal del Madre de Dios o un punto aproximado.
  - **Dos ríos locales nuevos,** el Tambopata y el Manu, en `rios_locales_201_700`, con `boca_en` el Madre de Dios. Llevan sus bocas y cabeceras de Wikidata o de Wikipedia y tramos rectos de respaldo. `tools/river-map` sigue en verde hasta que M4.5a les dé anfitrión (`HOSTS`).
  - **Segundas y terceras paradas de un sitio:** sin héroe no hay variante de luz. Por eso «Aguajal de Sandoval», «Astilleros de Puerto Maldonado», «Los guacamayos del Tambopata», «Nidos del Tambopata», «El Alto Madre de Dios» y «Las cochas del Manu» llevan otro nombre [P], otra pieza del kit y otro dato.
- **Km:** el viaje del 300 deshace camino (Napo abajo y Amazonas abajo hasta la boca del Madeira) y después sube el Madeira. Los km a casa bajan al entrar en el mundo: de ≈ 4.345 en Misahuallí a ≈ 2.905 en Puerto Heath. Dentro del mundo no retroceden. La collpa queda río arriba de Puerto Maldonado por el Tambopata (unos 77 km en línea recta). Para llegar a Boca Manu, la canoa vuelve por el Tambopata a la ciudad y sube el Madre de Dios: es una ida y vuelta por la confluencia, como la de Cuyabeno (5.4 bis). Boca Manu sigue más lejos de casa que la collpa.
- **Pregunta del mundo 7:** se plantea en el 300 (`pregunta_del_mundo` de la parada 30) y la responde la llegada del 350 a la collpa (parada 35). NPR cuenta las dos hipótesis, las toxinas y la sal, y el estudio de 13 años en *Ibis* apoya la del sodio; sus autores piden prudencia [E npr2017_collpas]. Por eso la respuesta dice «la idea más apoyada» y no «la ciencia demostró».

**Punto de paso `teotonio` (viaje del 300; Q18-A y P-M8):**

| id | Coordenada | Dato | Etiqueta y fuente | Relevo |
|---|---|---|---|---|
| `teotonio` | −8,85, −64,0333 (Wikidata Q22051335, la Cachoeira Teotônio; a unos 2 km del Madeira de respaldo) | `map.stop.teotonio.dato_01`, «Río arriba de Teotônio vive otra población de delfines de río, aislada por los rápidos.» (87 car.) | [E] wp_boto (reabierta el 2026-10-15) | Rosa se queda aquí y guía el bufeo boliviano en los mundos 7 y 8, sin campo nuevo en el contrato 30. Rosa vuelve en el viaje del 560, en este mismo punto de paso (sección 5.4 quinquies) |

- Los saltos de Teotônio quedaron bajo el embalse de Santo Antônio en 2012 [E wp_madeira]. Por eso el texto no dice «estos rápidos».
- La misma página del boto cuenta un estudio genético que no halló ADN mitocondrial propio río arriba de los rápidos. Por eso el texto habla de «otra población» y no de otra especie.
- Va en `puntos_de_paso_del_viaje` del mundo 7, con `id` y `relevo_del_guia`. De ahí lo toma `geometry.py` (`passing[]`) en M4.5a. Ese `passing[]` hoy solo lee `puntos_de_paso_1_200`.

**«Otro dato» de las tarjetas** (formato de 5.1 ter; longitudes medidas con el script de `--check`, con `{n}` resuelto):

| k · Parada | Clave | «Otro dato» (ES-419) | Car. | Etiqueta y fuente | Origen |
|---|---|---|---|---|---|
| 30 · Puerto Heath | `map.stop.puerto_heath.dato_01` | «El río Heath, que aquí llega al Madre de Dios, marca la frontera entre Bolivia y el Perú.» | 89 | [E] wp_heath_river, eswp_puerto_heath | §5.4 ter |
| 31 · Lago Sandoval | `map.stop.sandoval.dato_02` | «El lago Sandoval, de unas {n} hectáreas, es de las mayores cochas del sureste del Perú.» (125) | 87 | [E] wp_lake_sandoval | §5.4 ter |
| 32 · Aguajal de Sandoval | `map.stop.sandoval.dato_04` | «En las várzeas del Bajo Amazonas (Pará), cada casa usa cuias sin pintar de balde y vaso.» | 88 | [V] TJ-T1 | Pregunta 50 (a) |
| 32 · Aguajal de Sandoval | `map.stop.sandoval.dato_05` | «En Quatipuru-Mirim (Pará), las fazedeiras hacen a mano vasijas de barro para la lluvia.» | 87 | [V] TJ-T4 | Pregunta 50 (a) |
| 32 · Aguajal de Sandoval | `map.stop.sandoval.dato_06` | «El barro sin esmaltar suda: el agua que se evapora por sus poros enfría la de dentro.» | 85 | [E] TJ-T7 | Pregunta 50 (a) |
| 32 · Aguajal de Sandoval | `map.stop.sandoval.dato_07` | «En Icoaraci, en Pará, hay una alfarería de cerámica para el uso diario desde 1903.» | 82 | [E] TJ-T5 | Pregunta 50 (a) |
| 33 · Puerto Maldonado | `map.stop.puerto_maldonado.dato_02` | «El añuje abre el coco de la castaña, come semillas y entierra otras, que pueden brotar.» | 87 | [E] CR-F93 | §(e) |
| 34 · Astilleros de Puerto Maldonado | `map.stop.puerto_maldonado.dato_04` | «El coco de la castaña tarda unos {n} meses en madurar después de la flor.» (14) | 72 | [E] CR-F93 | §(e) |
| 35 · Collpa del Tambopata | `map.stop.collpa_tambopata.dato_02` | «La arcilla de una collpa tiene unas {n} veces más sodio que la comida de los loros.» (40) | 82 | [E] npr2017_collpas | §5.4 ter |
| 36 · Los guacamayos del Tambopata | `map.stop.collpa_tambopata.dato_04` | «La Reserva Nacional Tambopata se creó en 2000 y protege unas {n} hectáreas.» (274.690) | 79 | [V] elperuano_rntamb_2025 | §5.4 ter |
| 37 · Nidos del Tambopata | `map.stop.collpa_tambopata.dato_06` | «En la Reserva Tambopata, la zafra de la castaña va de enero a abril.» | 68 | [E] CR-F94 | §(e) |
| 39 · El Alto Madre de Dios | `map.stop.boca_manu.dato_03` | «El río Manu mide unos {n} km y corre entero por la región de Madre de Dios.» (356) | 75 | [E] eswp_rio_manu | §5.4 ter |
| 40 · Manu alto | `map.stop.manu.dato_02` | «En el parque del Manu se han registrado más de {n} especies de aves.» (1.000) | 70 | [V] sernanp_manu | §5.4 ter |
| 41 · Las cochas del Manu | `map.stop.manu.dato_04` | «En un solo árbol del Manu se encontraron más de {n} especies de hormigas.» (40) | 72 | [V] sernanp_manu | §5.4 ter |

- **El hueco de la pregunta 50, cerrado con la (a)** (D-022; GDD.md §6.3, pregunta 50): la tarjeta de la parada 32 lleva como «Otro dato» los cuatro candidatos de Pará de la tinaja (b.2 de `docs/research/cultura-ribera/propuesta-tinaja.md`). Cada uno lleva su lugar real, aunque la parada esté en el Perú, como el Matapi del 63. No añaden texto a la victoria ni al tutorial.
  - Las cuias del Bajo Amazonas (Aritapera, Santarém) y el panelão de Quatipuru-Mirim van con [V]: son un ensayo y un artículo de revista, reabiertos el 2026-10-15.
  - El barro que enfría el agua (el botijo, un dato general sin lugar) y la alfarería de Icoaraci de 1903 bajan a [E]: la fuente es Wikipedia sola o la prensa sola (cabecera de esta propuesta). La tinaja los daba con [V].
  - Las fuentes van con el prefijo `TJ-` y el número de la propuesta de la tinaja.
  - **Siguen pendientes del visto bueno del asesor** (la parada lleva `asesor` = true), con el riesgo del masato de la tinaja como punto expreso (Q-Ti7).
- **Candidatos de §(e) del mundo 7** (07 §2.2, regla 6: en los mundos 6-13 son una lista para el asesor):
  - **los astilleros de Puerto Maldonado** [F96]: el dato de llegada del 340 (el turismo y la construcción de botes);
  - **la castaña y el añuje** [F93]: los «Otro dato» de Puerto Maldonado (el añuje que entierra semillas y los 14 meses del coco);
  - **la zafra** [F94]: el «Otro dato» de los Nidos del Tambopata (de enero a abril, en la Reserva Tambopata). El hito 420 se llama «Zafra» [P] y se juega en las cochas del Manu, pero el nombre no nombra un lugar.

  §(e) los daba con [V]; aquí van con [E], porque las fuentes son Wikipedia sola y la prensa.
- **Vetos del mundo 7, para el asesor** (§(e), el resumen de 5.4 y el cuidado de `paradas_201_1000`; también en `nota_dato` de cada parada):
  - el oro y la minería: ni dragas, ni balsas, ni bateas, ni pepitas, ni el mercurio. La página de Puerto Maldonado los cuenta en la misma frase que los botes, y el texto los calla;
  - los pueblos en aislamiento y contacto inicial del Manu y del alto Madre de Dios (mashco piro y otros) y su zona;
  - la sumaúma o lupuna, árbol sagrado [V F153];
  - el caucho y el istmo de Fitzcarrald, que no es ruta. El distrito de Boca Manu lleva su nombre, y el texto no lo nombra;
  - los petroglifos de Pusharo;
  - los nombres en harakbut, ese eja, matsigenka, yine u otras lenguas, solo con un hablante.
- **Además, para el asesor:**
  - **Manu:** el texto solo habla del parque y su fauna, sin las comunidades que viven dentro (yora, matsigenka, harakbut y yine, según Wikipedia). Cocha Cashu es una estación de la zona restringida, y la parada no la nombra;
  - **la collpa:** sin turistas ni plataformas en el fondo; la hipótesis de la sal se cuenta como «la idea más apoyada»;
  - **el bufeo boliviano y la ley de 2012:** el dato del país no nombra al presidente que la firmó. El abogado y el asesor revisan también la bandera boliviana (sección 5.3);
  - **la placa «Bolivia te da la bienvenida» en el 300** sería otra extensión de P-M7 [P], como la del 220: la decide el dueño con M4.5a. El 310 es una vuelta y no lleva placa;
  - **los nombres nuevos** [P]: «Aguajal de Sandoval», «Astilleros de Puerto Maldonado», «Los guacamayos del Tambopata», «Nidos del Tambopata», «El Alto Madre de Dios» y «Las cochas del Manu»; el hito 420, «Zafra» [P];
  - **el añuje:** su nombre cambia por país (añuje, cutia, guatusa, picure).
- **`--check`** (`revisar_paradas_201_700`) suma, desde M3.7b, cuatro comprobaciones:
  - que un sitio del resumen de un mundo detallado tenga coordenada y no lleve ningún [NV];
  - que el viaje de entrada del mundo 7 lleve el punto de paso `teotonio` con coordenada [V] o [E] (`lat` y `lon`) y un dato `map.stop.<lugar>.dato_NN` con su etiqueta. Como ese dato irá a `facts.csv` (`passing[]` en M4.5a), su texto entre «» pasa las mismas reglas que el de las paradas (≤ 90 caracteres, cifras como `{n}` y sin «Belén»), y cada fuente que cita tiene que estar en `fuentes` con su `releida` (R5);
  - que la parada 32 lleve los cuatro candidatos de la pregunta 50 (a) en `otros_datos`;
  - que la bandera de un cruce que llega con el viaje de entrada de un mundo detallado (Bolivia en el 300) esté en la primera parada de ese mundo.

  La autoprueba suma un caso por cada una: sin un candidato de la pregunta 50, sin `teotonio`, `teotonio` sin la etiqueta de su dato, sin `lon`, con un texto de más de 90 caracteres o citando una fuente que no está en `fuentes`, y un sitio del resumen sin coordenada. El caso de la bandera ausente, que ya recorría todas las paradas con bandera, ahora también da error con Bolivia en el 300.
- **Para M4.5a** (`stops.py` y `geometry.py`):
  - `kind`: la gran parada, la parada y el cierre, como en 1-200;
  - `arrival_fact`: la clave del dato de cada parada (en n = 30, `map.country.bo.dato_01`);
  - `flag_raise`: BO en n = 30 y PE en n = 31;
  - `world_question` en n = 30 y `answers_world_question` en n = 35;
  - `bg.code`: `kit:<pieza>`;
  - `HOSTS`: `tambopata` → `madre_de_dios` y `manu` → `madre_de_dios`;
  - `passing[]`: `teotonio`, en el Madeira, con `map.stop.teotonio.dato_01`. Hoy `geometry.py` solo lee `puntos_de_paso_1_200`;
  - el guía de los mundos 7 y 8 (el bufeo boliviano), deducido del mundo (P-M8).

#### 5.4 quater. Paradas de 421-560: mundo 8, Madidi (2026-10-15, M3.7c)

Parte 3 de la fase F9 (k = 42-55), con el formato de 5.4 bis: las paradas están en `paradas_201_700` de `rio-amazonas.json`, con `sitio` y `wikidata`. **Las coordenadas son de Wikidata y las URL de los datos se abrieron o se reabrieron el 2026-10-15** (R5). Todo nombre, dato y texto queda «pendiente del asesor» (07 §8.2), y la bandera, también del abogado.

- **Columnas:** las de 5.4 bis.
  - **Temp.** es la del punto 4 de d.5 de la propuesta de mecánicas (D2, aplicada por D-023, punto 10): B en 421-460, 481-500 y 521-540; A en 461-480, 501-520 y 541-560.
  - **Km a casa** sigue el mismo [cálculo, mínimo] de 5.4 ter: la boca del Madeira cae en el km 1.298 del cauce, y se suman las poligonales del Madeira hasta Villa Bella (1.140 km, frente a unos 1.450 reales [E wp_madeira]) y la del río de cada parada: el Beni, el Mamoré o, para el Iténez, el Mamoré hasta su boca y el Iténez río arriba. Cada parada se proyecta sobre su río, sin ríos locales nuevos.
- **Coincidencias:** banner del 421, miquito del mundo 8 (maquisapa, 07 §6.4), combinaciones e hitos de d.5.2 y d.5.3 [P] y las Tinajas sueltas de la variante A (d.5.3, respaldo en d.5.4).

| k | Niveles | Llega | Mundo · tipo | Sitio | País · río · agua del sitio | Km a casa | Temp. | Dato de llegada | Coincidencias |
|---|---|---|---|---|---|---|---|---|---|
| — | Viaje 420 → 421 | | | Manu → Madre de Dios abajo (Puerto Maldonado, Puerto Heath) → Riberalta | Por el Perú y Bolivia, río abajo | | | Todo es agua ya llena. **Bandera de Bolivia** (vuelta). Sigue guiando el bufeo boliviano (Q18-A) | Pregunta del mundo 8 |
| 42 | 421-430 | 420 | 8 Madidi · gran parada | **Riberalta** | BO · Beni y Madre de Dios · blanca | ≈ 2.575 | B | `riberalta.dato_01`, «Riberalta está donde el río Madre de Dios se une al Beni.» [E CR-F95]. Plantea la pregunta del mundo 8. **Bandera de Bolivia** | Banner 421; miquito 422 (maquisapa); Flor con Matapi 426 |
| 43 | 431-440 | 430 | 8 · parada | **Los castaños de Riberalta** | BO · Beni · blanca | ≈ 2.575 | B | `riberalta.dato_03`, «En 2017-2018, Bolivia dio el {n} % de la castaña del mundo, más que ningún país.» (78) [E CR-F95; candidato de §(e)] | Hito 440 [P] |
| 44 | 441-450 | 440 | 8 · parada | **El Beni en Riberalta** | BO · Beni · blanca | ≈ 2.575 | B | `riberalta.dato_05`, «A Riberalta la llaman la capital amazónica de Bolivia; la castaña la hizo crecer.» [E CR-F95] | Brote con Remolino 449 |
| 45 | 451-460 | 450 | 8 · parada | **Rurrenabaque** | BO · Beni · blanca | ≈ 2.990 | B | `rurrenabaque.dato_01`, «En Rurrenabaque, el Beni sale de los pliegues de los Andes a los Llanos de Moxos.» [E wp_rurrenabaque] | Tinaja 454 (variante A); hito 460 [P] |
| 46 | 461-470 | 460 | 8 · parada | **Las dos orillas de Rurrenabaque** | BO · Beni · blanca | ≈ 2.990 | A | `rurrenabaque.dato_02`, «Al otro lado del Beni, frente a Rurrenabaque, San Buenaventura es la puerta del Madidi.» [E wp_madidi, wp_rurrenabaque] | Liana con fruta con Brote de dos hojas 466 |
| 47 | 471-480 | 470 | 8 · parada | **Madidi** (asesor) | BO · Beni y Tuichi · blanca | ≈ 3.005 | A | **Respuesta del mundo 8:** «Cabe tanta vida porque el Madidi va de la selva a cumbres de más de {n} metros.» (5.700) [E wp_madidi] | Hito 480 [P] |
| 48 | 481-490 | 480 | 8 · parada | **El río Tuichi** (asesor) | BO · Beni y Tuichi · blanca | ≈ 3.005 | B | `madidi.dato_03`, «El río Tuichi cruza la selva del Madidi y se une al Beni río arriba de Rurrenabaque.» [E wp_tuichi_river] | Matapi con Hoja atada 486 |
| 49 | 491-500 | 490 | 8 · parada | **La selva del Madidi** (asesor) | BO · Beni y Tuichi · blanca | ≈ 3.005 | B | `madidi.dato_05`, «Tras un censo de 2018, el Madidi es el parque con más aves y mariposas registradas.» [V wcs2018_madidi] | Tinaja 494 (variante A); Flor con Hoja atada 498; hito 500 [P] |
| — | Tramo 500 → 501 | | | Beni abajo (Riberalta, Cachuela Esperanza) → Villa Bella → Mamoré arriba | Por Bolivia, río abajo y río arriba | | | Agua ya llena hasta Villa Bella; se llena el Mamoré hasta Guayaramerín. Los km a casa bajan (`baja_por_confluencia` [P]) | |
| 50 | 501-510 | 500 | 8 · parada | **Guayaramerín** | BO · Mamoré · blanca | ≈ 2.520 | A | `guayaramerin.dato_01`, «Guayaramerín, en Bolivia, mira a Guajará-Mirim, en Brasil, al otro lado del Mamoré.» [E wp_guayaramerin] | Lluvia y Barro, titulares del ciclo 501-520 |
| 51 | 511-520 | 510 | 8 · parada | **Noel Kempff** | BO · Iténez · **clara** | ≈ 3.135 | A | `noel_kempff.dato_01`, «El río Iténez es el borde del parque Noel Kempff y la frontera con Brasil.» [E wp_noel_kempff] | Lluvia con la Cuia alta 514; hito 520 [P] (trío Lluvia + Barro + Hoja atada) |
| 52 | 521-530 | 520 | 8 · parada | **La cascada Arcoíris** | BO · Iténez · **clara** | ≈ 3.135 | B | `noel_kempff.dato_03`, «En el parque Noel Kempff, la cascada Arcoíris cae desde {n} metros de altura.» (88) [E wp_noel_kempff] | Piraña con Hoja atada 526 |
| — | Tramo 530 → 531 | | | Iténez abajo → boca del Iténez → Mamoré arriba → Ibare | Por Bolivia | | | Se llena el Mamoré hasta Trinidad. Los km a casa bajan (`baja_por_confluencia` [P]) | |
| 53 | 531-540 | 530 | 8 · parada | **Trinidad** (asesor) | BO · Mamoré · blanca | ≈ 2.980 | B | `trinidad.dato_01`, «En 1769, Trinidad se mudó lejos del Mamoré para escapar de sus crecidas.» [E wp_trinidad_beni] | Tinaja 531 (variante A); hito 540 [P] |
| 54 | 541-550 | 540 | 8 · parada | **Los Llanos de Moxos** (asesor) | BO · Mamoré · blanca | ≈ 2.980 | A | `llanos_de_moxos.dato_01`, «En las lluvias, el agua llega a cubrir hasta la mitad de los Llanos de Moxos.» [E wp_llanos_de_moxos, mongabay2013_moxos] | — |
| 55 | 551-560 | 550 | 8 · cierre | **Los bufeos de los Llanos** (asesor) | BO · Mamoré · blanca | ≈ 2.980 | A | `llanos_de_moxos.dato_03`, «Al delfín de río de Bolivia lo llaman bufeo: tiene más dientes que el del Amazonas.» [E wp_bolivian_river_dolphin] | Hito 560 «Riberalta» [P], que se juega aquí y no en Riberalta (el asesor puede renombrarlo, D-023 punto 21); fin del mundo 8 |

- **Recuento:**
  - **14 paradas en 6 sitios,** los del resumen de 5.4 y en su orden, todas en Bolivia;
  - **1 bandera izada:** Bolivia al ganar el 420 (`bandera` de la parada 42; el cruce llega con el viaje de entrada del mundo 8, con `vuelta`). Sin placa de bienvenida, como el 310;
  - **14 datos de llegada, 1 [V] y 13 [E],** y **13 «Otro dato», 1 [V] y 12 [E]** (las paradas 45 y 46, de Rurrenabaque, no llevan ninguno). Ningún [NV]. Las cifras van como `{n}` con su valor en `args` (Q8-A); los años no.
  - **Todas las paradas llevan el fondo sin héroe** de Q14-A: `bg_kit_w08` con una pieza del kit y el rótulo del sitio, a la hora del mundo 8 (el mediodía con niebla de cascada; 07 §2.1). Las piezas [P] son la lista para el artista del kit: confluencia del Madre de Dios y el Beni, castaños emergentes, cocos de castaña en la orilla, el Beni saliendo de los Andes, dos orillas frente a frente, selva con los Andes nevados, acantilados del Tuichi, mariposas en la playa, Mamoré frontera con baliza, Iténez de agua clara, cascada y meseta, puerto del Ibare, sabana inundada y laguna con bufeos.
- **Cambios frente al resumen de 5.4** (también en `paradas_201_1000`, mundo 8):
  - **Coordenadas de Wikidata** en Riberalta (Q941357), Rurrenabaque (Q749372), la boca del Tuichi (Q610021, con el calificador P518 de la boca) y Guayaramerín (Q1026253). Ya eran esas; ahora llevan su elemento.
  - **Iténez y Noel Kempff:** el sitio pasa al aeródromo de Flor de Oro (Q16891327), en la orilla del Iténez, en el norte del parque (antes, −14,5, −60,8 aproximada). La coordenada del parque (Q650710) queda a unos 50 km del Iténez de respaldo, y la de la cascada Arco Iris (Q8342787), a unos 19, más que los 15 km del canon: la cascada sale en el fondo, lejana.
  - **Trinidad y Llanos de Moxos:** el sitio pasa a la coordenada de Wikidata de la ciudad (Q34986), a unos 10 km del Mamoré de respaldo (antes, la boca del Ibare, el puerto, −14,6808, −65,0156). Los Llanos de Moxos (Q748368) no tienen coordenada en Wikidata.
  - **Segundas y terceras paradas de un sitio:** sin héroe no hay variante de luz. Por eso «Los castaños de Riberalta», «El Beni en Riberalta», «Las dos orillas de Rurrenabaque», «El río Tuichi», «La selva del Madidi», «La cascada Arcoíris», «Los Llanos de Moxos» y «Los bufeos de los Llanos» llevan otro nombre [P], otra pieza del kit y otro dato.
  - **Banderas de cortesía:** «BO (Riberalta)» pasa a decir que el cruce llega con el viaje de entrada y que Riberalta, la parada 42, está en Bolivia.
- **Km y las dos bajadas por una confluencia.** El viaje del 420 baja el Madre de Dios ya lleno, así que los km a casa bajan al entrar en el mundo: de ≈ 3.240 en el Manu a ≈ 2.575 en Riberalta. El resumen pide después «Beni abajo → Mamoré arriba», y el Iténez va antes que Trinidad. Con ese orden, los km bajan dos veces **dentro** del mundo:
  - de ≈ 3.005 en el Madidi a ≈ 2.520 en Guayaramerín (Beni abajo hasta Villa Bella y Mamoré arriba);
  - de ≈ 3.135 en Flor de Oro a ≈ 2.980 en Trinidad (Iténez abajo y Mamoré arriba).

  Para no cambiar el orden del resumen, que la tabla d.5.3 de la propuesta de mecánicas ya usa («el 560 se juega en Trinidad y los Llanos de Moxos»), las dos paradas llevan un campo nuevo [P], `baja_por_confluencia`, que dice por dónde baja y por dónde sube la canoa. No está en el GDD ni lo pedía la tarea: es una propuesta, aplicada con la opción recomendada y pendiente del dueño (pregunta de abajo). `--check` acepta [P] que los km bajen solo en una parada con ese campo y en otro río que la anterior, y nunca por debajo de la última parada de su mismo río en el mundo (Trinidad queda por encima de Guayaramerín, los dos en el Mamoré). Es la regla de `tools/river-map/geometry.py`, que compara los km solo entre paradas seguidas del mismo río (08 §4.5: «que los km no retrocedan»). La alternativa sin bajadas reordena los sitios en zigzag entre el Beni y el Mamoré (Guayaramerín, Riberalta, Trinidad, Rurrenabaque, el Madidi y Noel Kempff), con tres idas y vueltas largas; la de una sola bajada pone Noel Kempff al final y mueve el 560 de Trinidad al Iténez. Las dos tocan d.5.3, así que no se aplican sin el dueño. En la ventanita de casa, «Casa · a unos {km} km» baja en el 500 y en el 530 [P], como ya baja en cada viaje de entrada.

  **Pregunta abierta para el dueño (mundo 8):** ¿cómo se ordena la ruta del Beni, el Mamoré y el Iténez?
  - **A (recomendada, la aplicada) [P]:** el orden del resumen, con las dos bajadas declaradas (`baja_por_confluencia` en el 500 y en el 530) y la excepción de `--check`. No toca d.5.3; el jugador ve que los km a casa bajan dos veces a mitad de mundo.
  - **B:** zigzag entre el Beni y el Mamoré (Guayaramerín, Riberalta, Trinidad, Rurrenabaque, el Madidi y Noel Kempff), sin bajadas dentro del mundo, con tres idas y vueltas largas. Toca d.5.3 («el 560 se juega en Trinidad y los Llanos de Moxos»).
  - **C:** Noel Kempff al final, con una sola bajada, y el 560 pasa de Trinidad al Iténez. Toca d.5.3.

  Con B o C, M3.7c se rehace en esas paradas y el campo y la excepción de `--check` se quitan.
- **Pregunta del mundo 8:** se plantea en el 420 (`pregunta_del_mundo` de la parada 42) y la responde la llegada del 470 al Madidi (parada 47). Wikipedia da de 180 a 5.760 m [E wp_madidi], y WCS termina su expedición de 2018 en el Chaupi Orco, a 6.044 m, sin decir si está dentro del parque [V wcs2018_madidi]. Por eso la respuesta dice «más de 5.700», como el resumen, y no da la cota.

**«Otro dato» de las tarjetas** (formato de 5.1 ter; longitudes medidas con el script de `--check`, con `{n}` resuelto):

| k · Parada | Clave | «Otro dato» (ES-419) | Car. | Etiqueta y fuente | Origen |
|---|---|---|---|---|---|
| 42 · Riberalta | `map.stop.riberalta.dato_02` | «El paiche llegó a Bolivia en 1976: desplaza peces nativos y da trabajo a pescadores.» | 85 | [E] CR-F92 | §(e) |
| 43 · Los castaños de Riberalta | `map.stop.riberalta.dato_04` | «El castaño da cocos casi solo en la selva intacta: lo polinizan abejas grandes.» | 79 | [E] CR-F93 | §5.4 quater |
| 44 · El Beni en Riberalta | `map.stop.riberalta.dato_06` | «Un castaño puede vivir {n} años o más.» (500) | 38 | [E] CR-F93 | §5.4 quater |
| 47 · Madidi | `map.stop.madidi.dato_02` | «El Madidi es parque nacional desde 1995 y protege unos {n} km² de selva y montaña.» (18.958) | 85 | [E] wp_madidi | §5.4 quater |
| 48 · El río Tuichi | `map.stop.madidi.dato_04` | «El Tuichi mide unos {n} km y nace en las laderas de los Andes.» (265) | 62 | [E] eswp_rio_tuichi | §5.4 quater |
| 49 · La selva del Madidi | `map.stop.madidi.dato_06` | «En el censo de 2018, {n} hallazgos del Madidi podían ser especies nuevas para la ciencia.» (124) | 89 | [V] wcs2018_madidi | §5.4 quater |
| 50 · Guayaramerín | `map.stop.guayaramerin.dato_02` | «El Mamoré mide unos {n} km; aquí hace de frontera entre Bolivia y Brasil.» (1.930) | 75 | [E] wp_mamore_river, wp_guayaramerin | §5.4 quater |
| 51 · Noel Kempff | `map.stop.noel_kempff.dato_02` | «El Iténez separa Bolivia de Brasil a lo largo de unos {n} km.» (920) | 61 | [E] wp_guapore_river | §5.4 quater |
| 52 · La cascada Arcoíris | `map.stop.noel_kempff.dato_04` | «Las rocas de la meseta de Huanchaca se formaron hace unos {n} millones de años.» (1.000) | 81 | [E] wp_noel_kempff | §5.4 quater |
| 52 · La cascada Arcoíris | `map.stop.noel_kempff.dato_05` | «El parque Noel Kempff es Patrimonio Natural de la Humanidad desde el año 2000.» | 78 | [E] wp_noel_kempff | §5.4 quater |
| 53 · Trinidad | `map.stop.trinidad.dato_02` | «Trinidad, capital del Beni, es la capital de departamento más lluviosa de Bolivia.» | 82 | [E] wp_trinidad_beni | §5.4 quater |
| 54 · Los Llanos de Moxos | `map.stop.llanos_de_moxos.dato_02` | «En 2013, los Llanos de Moxos fueron el humedal Ramsar más grande del mundo.» | 75 | [E] mongabay2013_moxos, wp_llanos_de_moxos | §5.4 quater |
| 55 · Los bufeos de los Llanos | `map.stop.llanos_de_moxos.dato_04` | «Los Llanos de Moxos son sabanas con islas de bosque y bosques a la orilla de los ríos.» | 86 | [E] wp_llanos_de_moxos | §5.4 quater |

- **Candidatos de §(e) del mundo 8** (07 §2.2, regla 6: en los mundos 6-13 son una lista para el asesor):
  - **Riberalta y el 78 % de la castaña mundial** [F95]: el dato de llegada del 430;
  - **el paiche contado con sus dos caras** [F92]: el «Otro dato» de Riberalta. Una cara es el pez que desplaza a los nativos y la otra, la pesca que el Estado fomenta y que da trabajo a pescadores (según la fuente, en torno al 70 % de lo que pescan algunos; cultura-ribera 3.9: «un dato solo negativo estigmatiza un medio de vida»). El texto no dice «invasor» ni «especie a proteger». Para caber en 90 caracteres no dice «por accidente», que está en la fuente; el asesor puede elegir otra forma del medio de vida.

  §(e) los daba con [V]; aquí van con [E], porque las fuentes son Wikipedia sola y la prensa (cabecera de esta propuesta).
- **La bandera del 420** (sección 5.3): la tricolor, la bandera nacional, de uso náutico [P]. El DS 0241/2009 regula los símbolos del Estado y no dice nada de las banderas en embarcaciones; a la wiphala la llama «símbolo sagrado» (art. 28) [V lexivox_ds241, leído el 2026-10-15]. Es una razón más para no ponerla en la canoa sin el asesor. La elección final es del abogado y del asesor; la Constitución (art. 6.II) no se leyó.
- **Vetos del mundo 8, para el asesor** (§(e), el resumen de 5.4 y el cuidado de `paradas_201_1000`; también en `nota_dato` de cada parada):
  - **Casarabe** (encargo de M3.7c) y toda la arqueología de los Llanos de Moxos: lomas, camellones, canales y calzadas. Wikipedia cuenta unos 4.700 montículos [E wp_llanos_de_moxos], y ni el texto ni el fondo los nombran;
  - el paiche como «especie a proteger» (§(e));
  - el caucho y la época de las barracas;
  - el oro y la minería del Beni y del Madre de Dios: ni dragas ni balsas;
  - los pueblos indígenas del Madidi, del Beni y de los Llanos de Moxos, sus comunidades y sus territorios (también el TIPNIS), y los albergues de las comunidades del Tuichi;
  - la represa del Bala, en el angosto del Bala, sobre el Beni [E wp_madidi];
  - la muerte de Noel Kempff Mercado, asesinado en el parque que lleva su nombre [E wp_noel_kempff];
  - la isla Suárez, frente a Guayaramerín, en disputa entre Bolivia y Brasil [E wp_guayaramerin]: ni nombrada, ni dibujada, ni con bandera (también el abogado);
  - las misiones jesuitas (Trinidad se fundó en 1686 como pueblo jesuita [E wp_trinidad_beni]);
  - la wiphala en la canoa;
  - los nombres en tacana, moxeño, esse ejja u otras lenguas, solo con un hablante.
- **Además, para el asesor:**
  - **Madidi:** el texto solo habla del relieve y de las especies. No da el número de aves, porque las fuentes no coinciden (1.028 confirmadas por WCS y 1.254 en Wikipedia), ni el nombre científico del tití del Madidi (propuesta de fauna, regla 9);
  - **Trinidad y los Llanos:** el dato de llegada del 530 habla de las crecidas, no de la misión; el del 540, del agua, no de las obras de tierra;
  - **el bufeo:** la página lo da como subespecie de *Inia geoffrensis* («currently recognized as a subspecies»), aunque dice que no hay consenso, y el texto no lo dice. Su nombre cambia por país (bufeo, boto, delfín rosado);
  - **los nombres nuevos** [P]: «Los castaños de Riberalta», «El Beni en Riberalta», «Las dos orillas de Rurrenabaque», «El río Tuichi», «La selva del Madidi», «La cascada Arcoíris», «Los Llanos de Moxos» y «Los bufeos de los Llanos»; el hito 560, «Riberalta» [P], que se juega en los Llanos;
  - **la castaña:** su nombre cambia por país (castaña, nuez amazónica, castanha-do-pará).
- **No se pudo abrir el 2026-10-15** la ficha de la UNESCO de Noel Kempff (whc.unesco.org/en/list/967 dio 403). Por eso «Patrimonio Natural de la Humanidad desde 2000» va con [E].
- **`--check`** (`revisar_paradas_201_700`) cambia, desde M3.7c, una comprobación [P] (pregunta abierta del mundo 8, en «Km y las dos bajadas por una confluencia»): los km a casa pueden bajar dentro de un mundo solo en una parada con `baja_por_confluencia` y en otro río que la anterior, y nunca por debajo de la última parada de su mismo río en el mundo. La autoprueba suma cuatro casos: la bajada de Guayaramerín sin el campo, la misma bajada en el río de la parada anterior, la bajada de Trinidad por debajo de Guayaramerín y un `baja_por_confluencia` sobrante en una parada cuyos km no bajan.
- **Para M4.5b** (`stops.py` y `geometry.py`):
  - `kind`: la gran parada, la parada y el cierre, como en 1-200;
  - `arrival_fact`: la clave del dato de cada parada (en n = 47, la respuesta del mundo);
  - `flag_raise`: BO en n = 42;
  - `world_question` en n = 42 y `answers_world_question` en n = 47;
  - `bg.code`: `kit:<pieza>`;
  - los legs del Beni (n = 42-49), del Beni abajo y el Mamoré (n = 50), del Mamoré y el Iténez (n = 51) y del Iténez abajo y el Mamoré (n = 53). Los de n = 50 y 53 bajan por una confluencia y cambian de río, como ya permite `validate_routes`;
  - `sample`: clear en n = 51 y 52 (el Iténez);
  - el guía de los mundos 7 y 8 (el bufeo boliviano), deducido del mundo (P-M8).

#### 5.4 quinquies. Paradas de 561-700: mundo 9, Ucayali (2026-10-16, M3.7d)

Parte 4 y última de la fase F9 (k = 56-69), con el formato de 5.4 bis: las paradas están en `paradas_201_700` de `rio-amazonas.json`, con `sitio` y `wikidata`. **Las coordenadas son de Wikidata y las URL de los datos se abrieron o se reabrieron el 2026-10-16** (R5). Todo nombre, dato y texto queda «pendiente del asesor» (07 §8.2). Con esta parte, `paradas_201_700` tiene las 50 paradas de 201-700 (k = 20-69).

- **Columnas:** las de 5.4 bis.
  - **Temp.** es la del punto 4 de d.5 de la propuesta de mecánicas (D2, aplicada por D-023, punto 10): B en 561-600, 621-660 y 681-700; A en 601-620 y 661-680.
  - **Km a casa:** todas las paradas están en el cauce, porque el Ucayali y el Tambo son parte de él (Carhuasanta → Apurímac → Ene → Tambo → Ucayali → Amazonas: el cauce `amazonas` de `rios` en `rio-amazonas.json`). Por eso sus km son los del punto del cauce [E], como en las paradas del cauce de 1-200, más 29. La laguna de Yarinacocha se proyecta sobre el punto de Pucallpa. Sin ríos locales nuevos.
- **Coincidencias:** banner del 561, miquito (sigue la maquisapa del mundo 8; 07 §6.4), combinaciones e hitos de d.5.2 y d.5.3 [P] (desde el 580, los hitos son de 14 cuias, K = 13) y las Tinajas sueltas de la variante A (d.5.3, respaldo en d.5.4).

| k | Niveles | Llega | Mundo · tipo | Sitio | País · río · agua del sitio | Km a casa | Temp. | Dato de llegada | Coincidencias |
|---|---|---|---|---|---|---|---|---|---|
| — | Viaje 560 → 561 | | | Trinidad → Mamoré abajo → Villa Bella → Madeira abajo (**`teotonio`: vuelve Rosa**) → Amazonas arriba (repaso) → Nauta → Ucayali arriba | Por Bolivia, Brasil y el Perú | | | Agua ya llena hasta Nauta; se llena el Ucayali hasta Jenaro Herrera. **Bandera del Perú** (vuelta). Rosa vuelve y guía otra vez la canoa (Q18-A) | Cabecera del 560, «Dos ríos…» (sección 6.3); pregunta del mundo 9 |
| 56 | 561-570 | 560 | 9 Ucayali · gran parada | **Jenaro Herrera** | PE · Ucayali · blanca | ≈ 3.980 | B | `jenaro_herrera.dato_01`, «De los dos ríos, el Ucayali es la rama principal del Amazonas: la canoa sigue por él.» [E wp_ucayali]: contesta «Dos ríos». Plantea la pregunta del mundo 9. **Bandera del Perú** | Banner 561; reto 566 |
| 57 | 571-580 | 570 | 9 · parada | **Requena** | PE · Ucayali y Tapiche · blanca | ≈ 4.035 | B | `requena.dato_01`, «En Requena, el Ucayali lleva de media unos {n} m³ de agua por segundo.» (13.385) [E wp_ucayali] | Tinaja 574 (variante A); hito 580 [P], el primer hito de 14 cuias |
| 58 | 581-590 | 580 | 9 · parada | **El Ucayali en Requena** | PE · Ucayali · blanca | ≈ 4.035 | B | `requena.dato_03`, «El Ucayali llega a medir {n} metros de ancho por la cantidad de islas que tiene.» (1.200) [E wp_ucayali] | Hoja atada con Brote de dos hojas 589 |
| 59 | 591-600 | 590 | 9 · parada | **Contamana** | PE · Ucayali · blanca | ≈ 4.690 | B | `contamana.dato_01`, «Junto a Contamana, las rocas guardan unos {n} millones de años de historia de la selva.» (60) [V antoine2016_contamana] | Tinaja 591 (variante A); hito 600 [P] |
| 60 | 601-610 | 600 | 9 · parada | **Los fósiles de Contamana** | PE · Ucayali · blanca | ≈ 4.690 | A | `contamana.dato_03`, «Cerca de Contamana se han reconocido al menos {n} especies fósiles.» (485) [V antoine2016_contamana] | Liana con fruta con Lluvia 609 |
| 61 | 611-620 | 610 | 9 · parada | **Pucallpa** (asesor) | PE · Ucayali · blanca | ≈ 4.970 | A | `pucallpa.dato_01`, «Pucallpa es la segunda ciudad más grande de la Amazonía peruana, después de Iquitos.» [E CR-F108; candidato de §(e)] | Hito 620 [P] |
| 62 | 621-630 | 620 | 9 · parada | **Yarinacocha** (asesor) | PE · Ucayali y laguna de Yarinacocha · blanca | ≈ 4.970 | B | **Respuesta del mundo 9:** `yarinacocha.dato_01`, «El Ucayali cambia de curso: Yarinacocha es una vieja curva suya que quedó como laguna.» [E CR-F147; candidato de §(e)] | Lluvia con Matapi 629 |
| 63 | 631-640 | 630 | 9 · parada | **Las garzas de Yarinacocha** (asesor) | PE · Ucayali y laguna de Yarinacocha · blanca | ≈ 4.970 | B | `yarinacocha.dato_03`, «La laguna de Yarinacocha mide unos {n} km de largo.» (20) [E CR-F147] | Tinaja 631 (variante A); hito 640 [P] |
| 64 | 641-650 | 640 | 9 · parada | **Bolognesi** | PE · Ucayali · blanca | ≈ 5.375 | B | `bolognesi.dato_01`, «Si el río rompe el cuello de una curva, la curva queda aparte como una laguna en U.» [E wp_oxbow] | Primer reto con tres mecánicas 646 |
| 65 | 651-660 | 650 | 9 · parada | **El Ucayali medio** | PE · Ucayali · blanca | ≈ 5.375 | B | `bolognesi.dato_03`, «Aun en aguas bajas, el Ucayali guarda un canal de al menos metro y medio de hondo.» [E wp_ucayali] | Tinaja 651 (variante A); hito 660 [P] |
| 66 | 661-670 | 660 | 9 · parada | **Atalaya** (asesor) | PE · Ucayali, Tambo y Urubamba · blanca | ≈ 5.535 | A | `atalaya.dato_01`, «En Atalaya, el Tambo y el Urubamba se juntan y nace el río Ucayali.» [E wp_tambo_peru, eswp_atalaya] | Liana con fruta con Brote 669 |
| 67 | 671-680 | 670 | 9 · parada | **El Urubamba** (asesor) | PE · Ucayali, Tambo y Urubamba · blanca | ≈ 5.535 | A | `atalaya.dato_03`, «El Urubamba corre unos {n} km desde los Andes de Puno hasta juntarse con el Tambo.» (724) [E wp_urubamba] | Hito 680 [P] |
| 68 | 681-690 | 680 | 9 · parada | **Puerto Prado** (asesor) | PE · Tambo · blanca | ≈ 5.690 | B | `puerto_prado.dato_01`, «En Puerto Prado, el Ene y el Perené se juntan y forman el río Tambo.» [E wp_tambo_peru] | — |
| 69 | 691-700 | 690 | 9 · cierre | **El Tambo** (asesor) | PE · Tambo · blanca | ≈ 5.690 | B | `puerto_prado.dato_03`, «El Tambo mide unos {n} km y es parte de las cabeceras del Amazonas.» (159) [E wp_tambo_peru] | Tinaja 691 (variante A); hito 700 «Yarinacocha» [P], que se juega aquí y no en Yarinacocha (el asesor puede renombrarlo, D-023 punto 21); fin del mundo 9 y de los 700 niveles de la 1.0 |

- **Recuento:**
  - **14 paradas en 7 sitios,** los del resumen de 5.4 y en su orden, todas en el Perú;
  - **1 bandera izada:** el Perú al ganar el 560 (`bandera` de la parada 56; el cruce llega con el viaje de entrada del mundo 9, con `vuelta`). Sin placa de bienvenida, como el 310 y el 420;
  - **14 datos de llegada, 2 [V] y 12 [E],** y **15 «Otro dato», 1 [V] y 14 [E]** (Pucallpa lleva dos; las demás paradas, uno). Ningún [NV]. Las cifras van como `{n}` con su valor en `args` (Q8-A); los años no;
  - **Todas las paradas llevan el fondo sin héroe** de Q14-A: `bg_kit_w09` con una pieza del kit y el rótulo del sitio, a la hora del mundo 9 (la «Mañana con vapor» de 07 §2.1). Pintar ese vapor como la bruma de la mañana sobre el agua, y no como el del río hirviente, es una propuesta [P]: 07 §2.2 da como ambiente del mundo 9 el «vapor del río hirviente». **Pregunta para el asesor o el dueño:** mientras siga abierta la de Shanay-Timpishka (resumen de 5.4), ¿qué vapor se pinta? Las piezas [P] son la lista para el artista del kit: el Ucayali ancho con islas, la boca del Tapiche, el Ucayali partido por islas, un barranco con capas de roca, la orilla con cerros bajos, el puerto con barcos de pasajeros, la laguna en media luna, garzas en la laguna, una curva cortada con su laguna en U, playas de vaciante, la confluencia del Tambo y el Urubamba, el Urubamba entre cerros, la confluencia del Ene y el Perené y el Tambo con los Andes al fondo.
- **Cambios frente al resumen de 5.4** (también en `paradas_201_1000`, mundo 9):
  - **Coordenadas de Wikidata** en Jenaro Herrera (Q15696655), Requena (Q3344103), Contamana (Q3317773), Pucallpa (Q217071) y Bolognesi (Q27649307, en Tahuanía). Ya eran esas, las de los puntos del cauce; ahora llevan su elemento. Pucallpa conserva la confianza V del cauce, como Atalaya; los demás, E.
  - **Yarinacocha:** las paradas 62 y 63 van en la laguna (Q11688197), a unos 10 km de Pucallpa; el sitio sigue en la ciudad.
  - **Atalaya: nace el Ucayali:** la coordenada es la de la boca del Tambo en Wikidata (Q1480564, calificador P518), la misma que el punto del cauce [V]. El nacimiento del Ucayali en Wikidata (Q200952) queda a menos de 1 km, y la ciudad (Q3322627), a unos 3 km.
  - **Tambo bajo (Puerto Prado):** el sitio pasa al nacimiento del Tambo en Wikidata (Q1480564, calificador P518), a 1,4 km del punto del cauce (antes, −11,1608, −74,2467), con confianza E. Puerto Prado no tiene elemento en Wikidata. **Ojo:** Puerto Prado está en el nacimiento del Tambo, donde se juntan el Ene y el Perené [E wp_tambo_peru], y no en su tramo bajo. El nombre del sitio queda [P] para el asesor (por ejemplo, «El Tambo (Puerto Prado)»); no se cambia aquí porque `--check` compara los sitios con el resumen y d.5.3 ya dice que el 700 se juega «en el Tambo bajo».
  - **Segundas y terceras paradas de un sitio:** sin héroe no hay variante de luz. Por eso «El Ucayali en Requena», «Los fósiles de Contamana», «Yarinacocha», «Las garzas de Yarinacocha», «El Ucayali medio», «El Urubamba», «Puerto Prado» y «El Tambo» llevan otro nombre [P], otra pieza del kit y otro dato.
  - **Banderas de cortesía:** «PE (Nauta)» pasa a decir que el cruce llega con el viaje de entrada (Amazonas arriba por la boca del Atacuari) y que la parada 56, Jenaro Herrera, lleva la bandera.
  - **Viaje de entrada:** «Mamoré abajo → Villa Bella → Madeira abajo (rápidos de Teotônio: vuelve Rosa) → Amazonas arriba (repaso) → Nauta → Ucayali arriba», con `puntos_de_paso_del_viaje` (`teotonio`, Villa Bella y la confluencia de Nauta).
  - **Pregunta del mundo:** su fuente pasa de `scielo_yarinacocha` a CR-F147. El resumen de SciELO, reabierto el 2026-10-16, es el de un artículo sobre los peces del lago (Aylas y otros, 2023), y no cuenta el origen del lago; Wikipedia ES sí: «La laguna se formó al cambiar de curso el río Ucayali» [E CR-F147].
- **Km:** el viaje del 560 sube de ≈ 2.980 en Trinidad a ≈ 3.980 en Jenaro Herrera, y dentro del mundo los km solo suben, hasta ≈ 5.690 en Puerto Prado. Sin `baja_por_confluencia`.
- **Pregunta del mundo 9 y la cabecera del 560:** la pregunta, «¿Por qué el río dibuja tantas curvas?» (37 caracteres), se plantea en el 560 (`pregunta_del_mundo` de la parada 56) y la responde la llegada del 620 a Yarinacocha (parada 62). La cabecera del 560, «Dos ríos: ¿cuál nos lleva a donde nace el agua?» (sección 6.3), es la pregunta del arco V-VI y no la del mundo; la contesta el dato de llegada del 560: el Ucayali es «the main headstream of the Amazon River» [E wp_ucayali]. Ese dato dice «De los dos ríos», para no repetir el del 170 en Nauta. Las paradas 64 y 65 siguen el hilo de las curvas (cómo una curva cortada se vuelve laguna; el canal en aguas bajas). **Pregunta para el dueño:** en el 560 se plantean dos textos para la misma cabecera, la pregunta del arco («Dos ríos…», sección 6.3) y la del mundo 9; ¿cuál ocupa la cabecera del Mapa o del banner del 561, y dónde va la otra? Aquí no se decide: las dos quedan como estaban.

**Punto de paso `teotonio` en el viaje del 560 (Q18-A y P-M8):**

| Punto de paso | Coordenada | Dato | Etiqueta | Para qué |
|---|---|---|---|---|
| `teotonio` | El mismo del viaje del 300 (sección 5.4 ter): −8,85, −64,0333 (Wikidata Q22051335) | `map.stop.teotonio.dato_01`, el mismo: «Río arriba de Teotônio vive otra población de delfines de río, aislada por los rápidos.» (87 car.) | [E] wp_boto (reabierta el 2026-10-16) | Rosa vuelve aquí y guía otra vez la canoa desde el 560; el bufeo boliviano se queda en el Madeira, río arriba de los rápidos |

- En `river_map_v1.json`, `passing[]` lleva cada punto de paso una sola vez, con un solo `fact` (contrato 29). Por eso el `teotonio` del 560 es el mismo elemento que el del 300: la misma coordenada, la misma confianza y el mismo dato. El leg del 560 lo cruza en su `via` (M4.5b).
- Que Rosa vuelva al bajar el Madeira, río abajo de los rápidos, cuadra con la fuente: la población aislada es la de río arriba («upstream of the rapids of Teotonio, in Bolivia») [E wp_boto].

**«Otro dato» de las tarjetas** (formato de 5.1 ter; longitudes medidas con el script de `--check`, con `{n}` resuelto):

| k · Parada | Clave | «Otro dato» (ES-419) | Car. | Etiqueta y fuente | Origen |
|---|---|---|---|---|---|
| 56 · Jenaro Herrera | `map.stop.jenaro_herrera.dato_02` | «Jenaro Herrera lleva el nombre de un escritor y explorador nacido en Moyobamba.» | 79 | [E] eswp_jenaro_herrera | §5.4 quinquies |
| 57 · Requena | `map.stop.requena.dato_02` | «Requena se fundó en 1907 a orillas del Tapiche y del Ucayali.» | 61 | [E] eswp_requena | §5.4 quinquies |
| 58 · El Ucayali en Requena | `map.stop.requena.dato_04` | «La cuenca del Ucayali ocupa unos {n} km² de selva y montaña.» (351.549) | 64 | [E] wp_ucayali | §5.4 quinquies |
| 59 · Contamana | `map.stop.contamana.dato_02` | «En Contamana hay aguas termales: el agua sale caliente de la tierra.» | 68 | [E] eswp_contamana | §5.4 quinquies |
| 60 · Los fósiles de Contamana | `map.stop.contamana.dato_04` | «Los fósiles de Contamana se hallaron en {n} lugares a lo largo de una quebrada.» (34) | 78 | [V] antoine2016_contamana | §5.4 quinquies |
| 61 · Pucallpa | `map.stop.pucallpa.dato_02` | «En 1945, una carretera unió por fin Pucallpa con Lima.» | 54 | [E] CR-F108 | §(e) |
| 61 · Pucallpa | `map.stop.pucallpa.dato_03` | «Por el Ucayali, el puerto de Pucallpa se comunica con Iquitos.» | 62 | [E] CR-F108 | §(e) |
| 62 · Yarinacocha | `map.stop.yarinacocha.dato_02` | «En las lluvias, la laguna de Yarinacocha se une al Ucayali por sus dos extremos.» | 80 | [E] CR-F147 | §(e) |
| 63 · Las garzas de Yarinacocha | `map.stop.yarinacocha.dato_04` | «En Yarinacocha abundan las garzas, los shanshos y los patos silvestres.» | 71 | [E] CR-F147 | §5.4 quinquies |
| 64 · Bolognesi | `map.stop.bolognesi.dato_02` | «Las lagunas que dejan las curvas del Amazonas son un buen hogar para el lobo de río.» | 84 | [E] wp_oxbow | §5.4 quinquies |
| 65 · El Ucayali medio | `map.stop.bolognesi.dato_04` | «Desde Atalaya hasta el Marañón, el Ucayali corre unos {n} km.» (1.600) | 63 | [E] wp_ucayali | §5.4 quinquies |
| 66 · Atalaya | `map.stop.atalaya.dato_02` | «A Atalaya se llega por río, en avioneta o por una trocha de unos {n} km.» (164) | 72 | [E] eswp_atalaya | §5.4 quinquies |
| 67 · El Urubamba | `map.stop.atalaya.dato_04` | «Atalaya se fundó en 1928, justo donde empieza el Ucayali.» | 57 | [E] eswp_atalaya | §5.4 quinquies |
| 68 · Puerto Prado | `map.stop.puerto_prado.dato_02` | «El Perené mide unos {n} km y baja de las laderas orientales de los Andes.» (165) | 73 | [E] wp_perene | §5.4 quinquies |
| 69 · El Tambo | `map.stop.puerto_prado.dato_04` | «Río arriba quedan el Ene y el Apurímac, que nace en el nevado Mismi.» | 68 | [E] wp_ucayali | §5.4 quinquies |

- **Candidatos de §(e) del mundo 9** (07 §2.2, regla 6: en los mundos 6-13 son una lista para el asesor):
  - **Pucallpa** [F108]: el dato de llegada del 610 (la segunda ciudad de la Amazonía peruana) y dos «Otro dato» (la carretera de 1945 y el puerto que se comunica con Iquitos). La página habla también de madera y de la contaminación de Yarinacocha, que no se usan;
  - **Yarinacocha** [F147]: la respuesta del mundo en el 620 y un «Otro dato» (se une al Ucayali por sus dos extremos en las lluvias). El texto dice «curva», no «meandro» ni «tipishca» (palabras pendientes del asesor, 07 §8.1). El hito 700 se llama «Yarinacocha» [P] y se juega en el Tambo.

  §(e) los daba con [V]; aquí van con [E], porque la fuente es Wikipedia sola (cabecera de esta propuesta).
- **Vetos del mundo 9, para el asesor** (§(e), el resumen de 5.4 y el cuidado de `paradas_201_1000`; también en `nota_dato` de cada parada):
  - **Shanay-Timpishka** (encargo de M3.7d): no es parada ni sale en el fondo mientras el asesor no responda la pregunta del resumen (¿parada, solo vapor lejano o nada?). La fuente no la llama lugar sagrado, pero cuenta que los chamanes locales atribuyen su agua a la Yacumama, que §(e) veta, y que a su orilla hay tres comunidades [E wp_shanay]. Está en la cuenca del Pachitea, que entra en el Ucayali río arriba de Pucallpa: si el asesor la quiere como parada, lo natural es cambiar una de las dos de Bolognesi;
  - **las comunidades shipibo-konibo** (encargo de M3.7d): nunca como atracción, ni nombradas ni dibujadas;
  - el kené y cualquier diseño shipibo, la ayahuasca y el «turismo chamánico» (§(e));
  - la sumaúma o lupuna, árbol sagrado (§(e) [F153]);
  - la Yacumama y los seres del agua;
  - los asháninka y los demás pueblos del Tambo, del Ene, del Perené y del Urubamba, sus comunidades y sus territorios, y la violencia armada y la coca del valle del Ene;
  - la tala y el transporte de troncos (Pucallpa vive en parte de la madera [E CR-F108]);
  - la contaminación de la laguna de Yarinacocha, adonde va el drenaje de la ciudad [E CR-F108];
  - los nombres en shipibo, asháninka, quechua u otras lenguas, solo con un hablante: también el origen de los nombres de Contamana (en shipibo, según Wikipedia ES), Pucallpa y Tambo (del quechua, según Wikipedia EN);
  - el Pongo de Manseriche, que es del Marañón (solo mirador);
  - Machu Picchu y las demás ruinas del valle del Urubamba, como Choquequirao en el mundo 13.
- **Además, para el asesor:**
  - **los nombres nuevos** [P]: «El Ucayali en Requena», «Los fósiles de Contamana», «Yarinacocha», «Las garzas de Yarinacocha», «El Ucayali medio», «El Urubamba», «Puerto Prado» y «El Tambo»; el sitio «Tambo bajo (Puerto Prado)», que no está en el tramo bajo; el hito 700, «Yarinacocha» [P], que se juega en el Tambo;
  - **«shansho»**, el nombre regional del hoacín, que da la fuente de Yarinacocha;
  - **Jenaro Herrera:** el pueblo se escribe con J y la página del distrito escribe «Genaro Herrera», el nombre del escritor;
  - **Pucallpa:** el texto no da la población, porque la página da tres cifras distintas para 2017;
  - **Yarinacocha:** la página dice que los bufeos y los lagartos se ven cada vez menos; el texto no lo usa (un dato solo negativo, cultura-ribera 3.9);
  - **Contamana:** el texto no dice «saludables» de las aguas termales.
- **No se pudo leer el 2026-10-16:** la ficha del Centro de Investigaciones Jenaro Herrera del IIAP (portal.iiap.gob.pe no resolvió), el libro de Spichiger (1990) del repositorio del IIAP (PDF sin texto legible para la herramienta) y la ficha de dodis.ch (bloqueo anti-bots). Por eso la tarjeta de Jenaro Herrera no habla del centro de investigación de 1983, que solo vio el buscador; Bolognesi no tiene datos propios en una fuente abierta, y sus dos paradas cuentan el río del tramo.
- **`--check`** (`revisar_paradas_201_700`) suma, desde M3.7d, dos comprobaciones:
  - el viaje del 560 también tiene que llevar el punto de paso `teotonio`, con las mismas reglas que el del 300 (`PUNTOS_DE_PASO_REQUERIDOS`, mundo 9);
  - un punto de paso que sale en el viaje de entrada de varios mundos tiene que ser igual en todos (coordenada, confianza y dato), porque es un solo elemento de `passing[]`.

  La autoprueba repite para el mundo 9 los casos del punto de paso (sin `teotonio`, sin la etiqueta de su dato, sin longitud, con un texto de más de 90 caracteres o con una fuente que no está en `fuentes`, y un sitio del resumen sin coordenada) y suma dos: el `teotonio` del 560 con otra longitud y con otro dato que el del 300.
- **Para M4.5b** (`stops.py` y `geometry.py`):
  - `kind`: la gran parada, la parada y el cierre, como en 1-200;
  - `arrival_fact`: la clave del dato de cada parada (en n = 62, la respuesta del mundo);
  - `flag_raise`: PE en n = 56;
  - `world_question` en n = 56 y `answers_world_question` en n = 62;
  - `bg.code`: `kit:<pieza>`;
  - el leg del 560 (n = 56): el Mamoré abajo desde Trinidad, el Madeira abajo con `teotonio` en su `via`, el Amazonas arriba y el Ucayali, en el cauce, hasta Jenaro Herrera; los legs de n = 57-69, por el cauce río arriba;
  - `sample`: white en todas;
  - el guía: Rosa otra vez desde el mundo 9, deducido del mundo (P-M8; el bufeo boliviano solo en los mundos 7 y 8).

### 5.5 Si el dueño mantiene «Marañón alto» para El origen (Q7-B)

El final de 961-1.000 cambia de río y no cambia nada de 1-200:
- **viaje:** Nauta → Marañón;
- **paradas:** Pongo de Manseriche (961) [E], cañón del Marañón (971), alto Marañón (981) y laguna Lauricocha (991), la fuente histórica desde el mapa de Fritz de 1707 [E];
- **en el Mapa:** el Apurímac y el Mismi quedan como dato («la naciente más lejana con agua todo el año») y con un mirador.

**La recomendación es la A**, pero no porque «la ciencia sitúe ahí el nacimiento»: el Amazonas no tiene una sola naciente [E wp_source]. La A gana porque la Carhuasanta del Mismi es la cabecera más lejana con agua todo el año, la que se acepta desde hace décadas (National Geographic habla del Apurímac como fuente aceptada «since 1971») [E wp_source, natgeo2014], y porque permite un final de viaje con un sitio concreto. La tarjeta del 1.000 cuenta la discusión: el Mantaro es la más lejana según Contos y Tripcevich (2014), pero se corta varios meses al año, y el Marañón fue la fuente histórica.

---

## 6. TiTino, Rosa y Capi en el Mapa

### 6.1 Reparto [P] (Q3, atado a T1 de `propuesta-titino.md`)

| Personaje | Qué hace en el Mapa | Qué no hace | Poses |
|---|---|---|---|
| **Capi** (capibara) | **Rema la canoa** cargada de cuias atadas con cuerda (sin cestas, que serían la Canasta): es «el que trae cuias» (glosario §3.1), ahora río arriba. Sigue con el banner de mundo (D-012), la Canasta y el rescate | No habla en el Mapa. En el 961 la canoa queda amarrada | `capi_paddle` [P]: el bucle de `capi_rescue` («llega remando», 07 §6.1) reducido a 32-40 dp, sin pieza nueva |
| **Rosa** (boto) | **Guía:** nada delante por el agua ya llena (`rosa_swim`) y asoma al llegar (`rosa_surface`). En Tarapoto salta con sus parientes. **En el 961 se despide** al pie de los Andes (`rosa_farewell` [P]): más arriba hay cascadas y agua muy baja, que son los límites del área del boto [E wp_boto] | No habla. Nunca en pantallas de pago (07 §6.2). No entra en los recuadros fuera de la cuenca | Las del canon más `rosa_farewell` |
| **TiTino** (perezoso) | **Firma desde casa:** vive en su embaúba del estuario, abajo del Mapa (c.3: no viaja). El dato de cada llegada sale en la **placa del Dato del río con su retrato** y su gesto `sloth_present`, no en un bocadillo (6.2). Habla con bocadillo solo en sus frases propias (≤ 50 caracteres, e.2). Con la cámara lejos, sale en una **ventanita de casa** de 48-64 dp con «Casa · a unos N km». Dormita a los 30 s sin toques (`sloth_sleep`, que sustituye a `capi_sleep`) | **No viaja:** un perezoso recorre unos 40 m al día (B45 [V]), y llevarlo en canoa evocaría el turismo con fauna que prohíbe g.2. Nunca en pantallas de pago | `sloth_idle`, `sloth_present`, `sloth_sleep` y `sloth_cheer_big` (cierres de mundo) |

- **Si T1 no se aprueba:** Rosa guía y la placa del dato lleva su retrato, como dice el canon (06 §4.4), en vez de la «tarjeta de papel» de `rosa_fact`, que choca con D-003. Capi sigue remando y la ventanita de casa no existe. Todo lo demás vale igual.
- **Tres siluetas con papeles claros,** como el rey, el mayordomo y el guía de los líderes: Capi las manos, Rosa el camino y TiTino la voz.
- **TiTino en el inicio** (decisión pendiente, junto con T1: **Q17**). El inicio enseña el fondo de la parada actual (sección 9), y TiTino vive en el inicio colgado de su embaúba (propuesta de TiTino, d.7). Sobre el fondo de Manaos o de la puna parecería que está allí, y su especie no vive en Manaos (c.1: allí vive *B. tridactylus*) ni en la puna andina. Dos salidas:
  - **A (recomendada):** en el inicio, TiTino solo sale en la **ventanita de casa** (su embaúba del estuario, enmarcada), sobre el fondo de la parada. Es la misma pieza del Mapa y cuenta lo mismo: él se queda en casa y tú estás lejos.
  - **B:** el inicio se queda en el estuario, con TiTino en su árbol y una placa «Estás en {sitio}». Más barato, pero el inicio deja de enseñar el sitio en los tableros de 3 filas (sección 10.6).

  Cambia 06 §4.7 y la d.7 de la propuesta de TiTino (sección 15).
- **Rosa y los rápidos de Teotônio** (mundos 7 y 8; con el asesor: **Q18**). El viaje del 300 la hace nadar Madeira arriba hasta Bolivia, y en la realidad los rápidos y saltos del Madeira (Teotônio, hoy bajo el embalse de Santo Antônio) separan la población boliviana del delfín de río [E wp_boto, wp_madeira]. Dos salidas:
  - **A (recomendada):** Rosa se queda en los rápidos de Teotônio y en los mundos 7 y 8 guía **un bufeo boliviano** (el mismo rig con otra paleta, sin nombre propio). Da un dato real: «Río arriba de estos rápidos vive otra población de delfines de río» [E wp_boto]. Rosa vuelve en el viaje del 560.
  - **B:** Rosa sigue en los mundos 7 y 8, y la despedida del 961 se justifica solo por el agua muy baja de los Andes. Más barato, pero el viaje cruza unos rápidos que, en la realidad, el boto no cruza.

  Afecta a §5.4 (mundos 7 y 8) y a la frase del 961. **Decidida la A** (D-013, 2026-10-13): con P-M8 (D-023, punto 19), el guía se deduce del mundo y el relevo es el punto de paso `teotonio` del viaje del 300, con su dato (sección 5.4 ter). El cierre del mundo 8 (parada 55, «Los bufeos de los Llanos») cuenta cómo llaman en Bolivia a ese delfín (sección 5.4 quater). Rosa vuelve en el mismo punto de paso, `teotonio`, en el viaje del 560 (sección 5.4 quinquies).

### 6.2 Reglas de los textos

- **Longitud:** el Dato del río, ≤ 90 caracteres en ES (medida de Frutas del río y de g.3); las frases de personalidad de TiTino y las preguntas, ≤ 50 (e.2). EN y PT-BR con su `max_chars`.
- **El dato va en placa, no en bocadillo.** La regla 1 de e.2 de la propuesta de TiTino limita su bocadillo a ≤ 50 caracteres, y los datos llegan a 89. Por eso el dato sale en la **placa del Dato del río** (canon 02 §6, bajo «Siguiente»), con el retrato de TiTino como sello y su gesto. Así encaja con la opción A de T6 («el dato; TiTino solo hace el gesto») sin excepciones. Si el dueño quisiera el dato en boca de TiTino, habría que declarar una excepción a e.2 y volver a plantear T6 con ese sentido, junto con la Q1 de Frutas.
- **Una sola línea por victoria** (T6, opción A).
  - En la victoria del `10k` manda el dato de llegada.
  - Si se aprueba Frutas del río, su línea pasa a la victoria siguiente.
  - La frase propia de TiTino queda para el Mapa: la presentación del 10, el fin del contenido y el 1.000.
- **Duración:** la placa y el bocadillo se quedan hasta el toque (D-011: el dueño no alcanzaba a leer).
- **Claves:** las de la investigación (`map.stop.<lugar>.dato_NN`, `map.country.<iso2>.dato_NN`, `map.myth.<tema>` y `map.general.*`), sin nombres de personaje. El `fact_id` de `af_river_fact_shown` es esa clave. Estado inicial `pendiente_asesor`.
- **Publicación:** solo [V], o [E] revisados por el asesor. Ningún [NV]. Antes de cerrar un texto, se reabre su URL (riesgo R5): en esta revisión se reabrieron las de Tarapoto, el Perú, Pacaya-Samiria, Allpahuayo, el Tapajós y las nacientes, y tres textos decían más que su fuente.

### 6.3 Frases de ejemplo [P] (ES-419; longitudes medidas con script)

| Momento | Dónde sale | Frase | Caract. | Etiqueta y fuente |
|---|---|---|---|---|
| Primer Mapa (10), presentación | TiTino (bocadillo) | «Yo vivo aquí, donde el río entra al mar.» | 40 | Personaje (c.3) |
| Nivel 10, Dato del río 1 | Placa del dato | «Bajo el agua turbia de la desembocadura hay un arrecife que la ciencia describió en 2016.» | 89 | [V] MR-F06 (*Science Advances*, 2016) |
| 20 · Alter do Chão | Placa del dato | «Cuando el río baja aparece una isla de arena blanca: la Ilha do Amor.» | 69 | [E] MR-F19 |
| 30 · Floresta Nacional (respuesta del mundo 2) | Placa del dato | «De natural, el Tapajós es claro: baja de rocas muy viejas que sueltan poco barro.» | 81 | [E] wp_tipos (rocas cristalinas antiguas y suelos arenosos, con pocos sedimentos). Nota para el asesor: hoy baja turbio en largos tramos |
| 40 · Encuentro | Placa del dato | «El Negro, oscuro y tibio, y el Solimões, color café y más fresco, van 6 km sin mezclarse.» | 89 | [E] MR-F24 |
| 50 · Manaos | Placa del dato | «En Brasil el río se llama Solimões hasta aquí; con el Negro ya se llama Amazonas.» | 81 | [E] MR-F03, MR-F24 (Wikipedia) |
| 60 · Anavilhanas (respuesta del mundo 3) | Placa del dato | «Mira el Negro: su color de té viene de las hojas que se deshacen en el agua.» | 76 | [V] rios_villamizar (Ríos-Villamizar y otros, 2014) y wp_tipos (la clasificación de Sioli) |
| 70 · Lago de Tefé | Placa del dato | «El río Tefé es de agua negra y, justo antes del Solimões, se abre en este lago.» | 79 | [E] wp_tefe_rio |
| 70 · Lago de Tefé, tarjeta | Tarjeta | «Cerca de aquí, en Mamirauá, los pescadores cuentan los pirarucús que suben a respirar.» | 86 | [V] uakari_2011_pirarucu (revista del Instituto Mamirauá, 2011). El resto de los «Otro dato», en la sección 5.1 ter |
| 80 · Leticia | Placa del sistema y placa del dato | «La Amazonia ocupa cerca del 42 % del territorio continental de Colombia.» | 72 | [V] MR-F78 (SINCHI). Antes, la placa «Colombia te da la bienvenida» |
| 90 · Leticia al atardecer | Placa del dato | «Al atardecer llegan miles de pericos a dormir en los árboles del parque Santander.» | 82 | [E] MR-F35, CR-F103 |
| 120 · Tarapoto | Placa del dato | «Estos lagos son la “guardería” de los delfines de río: aquí crían a sus bebés.» | 78 | [V] MR-F38 (MinAmbiente, leída el 2026-10-12: «guardería» y «lugar clave para su crianza»). «Enseñan a pescar» no tiene fuente: [NV], no se publica |
| 130 · Río frontera (respuesta del mundo 4) | Placa del dato | «Sí: aquí el Amazonas hace de frontera, con Colombia en una orilla y el Perú en la otra.» | 87 | [E] trapecio (Sociedad Geográfica de Colombia) |
| 140 · Iquitos | Placa del sistema y placa del dato | «El Perú es el segundo país de América Latina con más bosque tropical, después de Brasil.» | 88 | [V] MR-F77 (MINAM: «bosques tropicales» de América Latina, no «bosque amazónico») |
| 150 · Río Itaya | Placa del dato | «Ninguna carretera une Iquitos con el resto del país: se llega en avión o en barco.» | 82 | [V] MR-F39 (Guinness) |
| 160 · Allpahuayo-Mishana | Placa del dato | «En esta arena blanca vive un ave que solo existe en esta parte del Perú.» | 72 | [E] wp_antbird: el hormiguero de Allpahuayo (*Percnostola arenarum*) solo vive en Loreto (Nanay, Itaya y Morona), no solo en la reserva. La perlita de Iquitos (*Polioptila clementsi*), la que cita Actualidad Ambiental, es otra especie, conocida solo de la reserva y cuya condición de especie se discute [E actualidad_allpahuayo, wp_gnatcatcher; releídas el 2026-10-13]. La tarjeta nombra al hormiguero |
| 170 · Nauta (respuesta del mundo 5) | Placa del dato | «Aquí el Marañón y el Ucayali forman el Amazonas. ¡Pero su agua viene de más arriba!» | 83 | [V] MR-F42 |
| 180 · Pacaya-Samiria | Placa del dato | «Le dicen “la selva de los espejos”: su agua oscura y quieta refleja el bosque.» | 78 | [E] actualidad_pacaya_48, mongabay_pacaya |
| 190 · Pacaya-Samiria al alba | Placa del dato | «Pacaya-Samiria protege unos 2 millones de hectáreas: es el mayor humedal de la Amazonia.» | 88 | [E] MR-F43 |
| 200 · con Q14-A, cambio al mundo 6 | Cabecera del Mapa o del banner | «¿Cuántas especies de árbol caben en una hectárea?» | 49 | Pregunta del mundo 6, que contesta Yasuní [V bass2010_yasuni] |
| 560 y 960 · entrada al Ucayali y a El origen | Cabecera | «Dos ríos: ¿cuál nos lleva a donde nace el agua?» | 47 | Pregunta del arco V-VI; la contesta el mundo siguiente (en el 560, el dato de llegada a Jenaro Herrera: sección 5.4 quinquies) |
| Fin del contenido (700 con Q14-A; 200 con Q14-B) | TiTino (bocadillo) | «Te espero aquí con más río muy pronto.» | 38 | Personaje |
| 961 · despedida de Rosa | TiTino (bocadillo) | «Rosa se queda: arriba hay cascadas y poca agua.» | 47 | [E] wp_boto: los límites del boto son las cascadas y el agua muy baja. Con el asesor (6.1) |
| 1.000 · el río despierta | TiTino (bocadillo) | «¡La primera gota! Ya viene hacia mi casa.» | 41 | Personaje. En la tarjeta del Mismi: «una de las nacientes del Amazonas», a más de 5.000 m (Laguna Bohemia, 5.148 m) [E wp_source] |
| 1.000 · final del vuelo | TiTino (bocadillo) | «A mi paso, este viaje me habría tomado 450 años.» | 48 | [cálculo] sobre B45 [V] y 6.575 km [E] |

### 6.4 Textos del sistema (claves nuevas del glosario §10 [P])

| Clave | ES | Notas |
|---|---|---|
| `map_stop_remaining` | «Faltan {n} niveles para {lugar}» | Plural: «Falta 1 nivel para {lugar}». En el inicio, la victoria y el Mapa |
| `map_arrived` | «{lugar} · {país} · río {río}» | Placa de llegada y tarjeta |
| `map_home_distance` | «Casa · a unos {km} km» | Ventanita de TiTino |
| `map_country_welcome` | «{país} te da la bienvenida» | Sin género |
| `map_world_question` | «{mundo} · {pregunta}» | Cabecera del Mapa; ✓ al responderse |
| `map_next_world` | «Mundo {n} · {nombre} · Nivel {nivel}» | Silueta |
| `map_samples` | «Muestrario · {k}/{n}» | Pestaña |
| `map_north_up` / `map_back_to_canoe` | «Norte arriba» / «Volver a la canoa» | Botones |
| `map_hito_preview` | «Si lo superas, llegas a {lugar}» | Tarjeta previa del hito p20 (D-009) |
| `stop_label` | «{lugar} · {país}» | Rótulo del lugar (sección 10.5) |

---

## 7. Coleccionables y economía (sin tocar 05)

| Pieza | Cómo se gana | Qué es | Dónde se ve | Economía |
|---|---|---|---|---|
| **Muestra del río** | Al llegar a la parada | Una **cuia de vidrio en miniatura** (silueta de D-006, nunca un tubo) con el agua del sitio y una **etiqueta de madera** con la silueta del sitio. **Las siluetas se hornean en el atlas del Mapa** (en el Editor o en `tools/river-map`), sin leer los héroes en tiempo de ejecución (sección 10.7). En los «paisajes del tramo», la silueta es la del kit con la inicial del sitio. Así las muestras de agua blanca de 81-200 se distinguen entre sí. En Santarém y el Encuentro lleva dos aguas en capas; en Allpahuayo-Mishana y Pacaya-Samiria, agua negra | Sobre el río, en su punto real (es el marcador de la parada), y en el Muestrario | Ninguna |
| **Muestrario del río** | Se llena solo | Repisa de madera (direccion-visual-v2 §4.5) con una fila por mundo: 1 + 3 + 4 + 6 + 6 = 20 en 1-200 (70 en 1-700). Parece un tablero en pequeño | Pestaña del Mapa. En la 1.1, la página Ríos del Cuaderno de campo (06 §4.6) | Ninguna |
| **Tarjeta de parada** (la «Escena del río») | Al tocar una muestra o una parada | **El fondo del tablero de esa parada, en pequeño** (kit + héroe), con nombre, país, río y tipo de agua, el dato con su fuente («Fuente: Ramsar»), «Otro dato», el miquito del tramo (propuesta de fauna) y la fruta que debutó allí | Mapa | Ninguna. Responde a la pregunta #16 de `propuesta-tintalu.md` (Escenas del río) |
| **Miradores** | Los revela el progreso, y la muestra especial se gana en ese momento | Un dato extra, que se lee al tocarlo | Mapa | Ninguna |
| **Banderas** | Al cruzar de país por el río | Las izadas se guardan en la lista de la canoa | Mapa | Ninguna |
| **Datos del río k/n** | Canon (06 §4.5) | Por parada y por mundo | Tarjeta | Ninguna |
| **Canasta de la parada** | Ya existe: cada 10 (06 §4.4); grande cada 100 (05 §2.1) | Contenido visible en las paradas futuras | Mapa y victoria | **Sin cambios** |
| **Skin del mundo** | Canon (06 §4.5) | «Premio del mundo · faltan N», **sin precio** en el Mapa | Cierre de mundo | Sin cambios |
| **Marco del Mapa** | Canon: el marco de país de la Bitácora del día 30 (05 §2.3). El sumidero de marcos a 2.000-5.000 semillas es **[P]**: está en el backlog de 09 y depende de la pregunta 2 de las preguntas abiertas de 05 | Cosmético | Marco de la pantalla | Sin cambios |
| **El río vivo** | Al ganar el 1.000 | El agua del Mapa corre para siempre | Mapa | Ninguna |

**Lo que no se hace:**
- nada se compra en el Mapa y no hay sobres ni azar (dirección 15);
- llegar no da semillas: el premio es el sitio, y la Canasta ya existe. `tools/econ_sim.py` y `tools/out/econ_*.md` **no cambian**;
- las palabras «sello» (es del Reto del río) y «postal» (sugiere papel, D-003) no se usan.

**Para después** [P]:
- **«Usar como fondo del inicio»** (el «USE» de Magic Sort): en la 1.1, con un campo del perfil (`settings.lobby_scene`).
- **«¿Verdad o mito?»** (de C): TiTino pregunta «Me contaron que…» y el jugador responde con un toque. Sería un **experimento de la 1.1** detrás de `ui_map_quiz` = `off` (Q12), porque:
  - añade interacción en la victoria, justo donde D-011 registró que el dueño no alcanzaba a leer;
  - un «Mito» mal etiquetado daña más la confianza que un dato flojo.

---

## 8. Dónde se abre y cuándo sale solo

### 8.1 Entradas

| Entrada | Comportamiento [P] | Canon |
|---|---|---|
| Inicio: icono «Mapa del río» | Abre la vista Mundo, con «Jugar · Nivel {n}» abajo y un punto si hay una llegada sin ver (`progress.map_seen_level`, sección 11.5) | 06 §4.7; oculto hasta el 10 (D-008) |
| Inicio: botón «Jugar · Nivel {n}» | Subtítulo «Faltan 3 para Manaos» | Gradiente de meta |
| Pausa → Mapa del río | Se abre **encima del nivel en pausa** y «Jugar» pasa a ser «Volver al nivel». No es salir del nivel, así que **no gasta canoa** (D-008: desde el 31, salir con el menú cuesta una) | 02 §5; 03 §13 |
| Victoria del `10k` | Franja de llegada en el hueco del Dato del río; enlace «Ver mapa» que abre el Mapa encima y vuelve a la victoria | 02 §6; 07 §5.4 |
| Victoria de los demás niveles | **Ninguna línea nueva:** la barra «Canasta k/10» que ya existe pasa a «Canasta · Manaos 7/10», con las 10 boyas del tramo. Cuenta lo mismo, porque la Canasta y la parada caen en el mismo nivel | 02 §6 (barras de evento: «Canasta "k/10"», máximo 3 visibles) |
| Sin canoas | El Mapa sigue disponible | 04 §7.1 |
| Mercado | **No** desde el Mapa (Q10) | — |

### 8.2 La victoria del nivel `10k`

```
┌────────────────────────────────────────┐
│        cinta «¡Nivel completado!»      │
│   +20 semillas     [Cosecha doble x2]  │
│   Canasta: 50 semillas + 3 Marea       │
│              [ Siguiente ]             │  con el chip del nivel siguiente (D-009)
│ ┌─ llegada (hueco del Dato del río) ─┐ │  bajo el botón (02 §6)
│ │ ~~~agua~~~ canoa ──► ◇ Alter do Chão│ │  1,2 s; un toque la completa
│ │ [retrato TiTino] «Cuando el río baja…»│ │  placa del Dato del río de este nivel
│ └────────────────────────────────────┘ │
│     Inicio                 Ver mapa    │
└────────────────────────────────────────┘
 Detrás: el panel se desvanece y el fondo pasa al sitio nuevo («victoria con vista»).
```

- **Victoria con vista** [P, de A] (Q6). Tras la ola de saltos (0,30-0,50 s, 07 §5.4), el panel y las cuias se desvanecen en 250 ms.
  - **En las llegadas,** el fondo pasa al sitio nuevo en el mismo fundido: el Teatro Amazonas aparece detrás de la cinta.
  - **En los tableros de 3 filas,** se ve el sitio actual.
  - **No añade tiempo:** «Siguiente» sale a la vez que hoy (1,40 s, VA-13) y el nivel siguiente ya carga con su fondo.
  - **Con Reducir movimiento,** corte directo.
- **El ejemplo de la Canasta** es el del canon: en el 20, 50 semillas y 3 Marea (05 §2.1; `eco_basket_cycle`, 06 §4.4).
- **Con la Cosecha doble,** que avanza sola tras el vídeo (D-011, punto 4), la franja ya se vio: empieza a la vez que la pantalla.

### 8.3 Cuándo sale solo (Q4, recomendación D)

| Momento | Qué pasa | Duración | Interrupciones |
|---|---|---|---|
| **Nivel 10** (06 §2.3) | Victoria → Canasta y Bitácora → **Mapa:** el estuario ya lleno, TiTino se presenta y da el Dato del río 1, la canoa sube a Santarém → inicio | Mapa ≤ 10 s; la secuencia, ≤ 30 s (canon) | La excepción del hito, ya canónica |
| **Cambio de mundo, con Q4-D (recomendada)** (40, 80, 140; con Q14-A, también 200, 300, 420 y 560) | Victoria con Canasta y skin, con la vista del sitio nuevo → Siguiente → AD_GATE → carga del 41 → **banner de D-012 con el viaje dentro**: Capi presenta, la canoa cruza en una franja del Mapa, se iza la bandera si cambia de país y la cabecera trae la pregunta del mundo; «Ver mapa» abre el Mapa entero → Fruta nueva, si toca → Llenar y teñir | ≤ 4 s dentro del banner, saltable | Ninguna pantalla nueva |
| **Cambio de mundo, con Q4-A** | Victoria → Siguiente → **AD_GATE → Mapa con el viaje de mundo** (bandera, dato y pregunta en la cabecera, «Jugar · Nivel 41» siempre a la vista) → carga del 41 con el banner de D-012 → Fruta nueva, si toca. Por el camino de la Cosecha doble (D-011, punto 4), igual: AD_GATE → Mapa → 41 | ≤ 6 s, saltable | Hito de mundo: 3 veces en 1-199. **En el 41, el 81 y el 141 se apilan Mapa, banner y Fruta nueva** (achiote, copoazú y huito), lo que 06 §4.5 evita en el 11 |
| **Resto de llegadas** | Solo la franja de la victoria | 0 s añadidos | 0 |
| **Nivel 200** | **Con Q14-A** (la 1.0 del GDD): un cambio de mundo más, al Oriente (Ecuador), con la pregunta del mundo 6. **Con Q14-B:** viaje hasta la boca del Napo y la pantalla «El río sigue creciendo» (06 §9) | ≤ 4-6 s | Hito |
| **Fin del contenido** (700 con Q14-A) | Viaje hasta el último tramo publicado y la pantalla «El río sigue creciendo» (06 §9) | ≤ 6 s | Hito |
| **Nivel 1.000** | El río despierta (sección 2.3) | Unos 8 s, saltable | Hito final |

- **Coste:** ninguna pantalla nueva en la primera sesión (el Mapa del 10 ya es canon); con Q4-D, unos 4 s saltables dentro del banner de cada cambio de mundo; con Q4-A, unos 6 s en una pantalla nueva entre AD_GATE y el nivel siguiente. Nada en el resto.
- **Esto cambia D-008** (02 §6, «Cadena corta»: Celebración → esta pantalla → Siguiente → AD_GATE → N+1): con Q4-A se añade el Mapa tras AD_GATE; con Q4-D, el banner de D-012 trae el viaje. Se declara en la sección 15.
- **Se respeta** `ui_max_takeovers_per_session` = 1 fuera de los hitos.
- **Si el dueño elige la B** (`ui_map_autopen` = `l10`), el cambio de mundo va como cualquier llegada, en la franja de la victoria, y el viaje se ve al abrir el Mapa.

### 8.4 La pantalla del Mapa (vista Mundo, 360 × 780 dp)

```
┌────────────────────────────────────────┐
│ ←  Mundo 3 · ¿Por qué el Negro es negro?│  cabecera de madera: pregunta del mundo
│ ┌──────────────── marco ─────────────┐ │
│ │ Andes ↑             ·· vidrio ·· ◇? │ │  parada futura en silueta: «Nivel 71»
│ │      ◇ Anavilhanas   [Canasta 60]   │ │  contenido visible
│ │   ~~agua negra~~ canoa(Capi) · Rosa →│ │  la canoa en la punta del agua
│ │  ◇ Manaos   ◇ Encuentro (dos aguas) │ │
│ │ ~~boca del Madeira (llena 10 %)~~   │ │  silueta «Mundo 7 · Nivel 301»
│ │                        N → (rosa)   │ │
│ │ (TiTino) Casa · a unos 1.480 km ↓   │ │  ventanita de casa
│ └─────────────────────────────────────┘ │
│ [Cuenca] Faltan 3 para Anavilhanas [Muestrario]│
│        [ Jugar · Nivel 58 ]             │  CTA hoja, siempre visible
└────────────────────────────────────────┘
```

---

## 9. Encaje con el resto del juego

| Sistema | Cómo encaja [P] |
|---|---|
| **Banner de mundo** (D-012: 11, 41, 81 y 141; Capi presenta y Rosa asoma) | Su fondo es el de la gran parada del mundo (el mismo kit y el mismo héroe). Texto `world_banner`. **Con Q4-D, lleva dentro el viaje de mundo** (≤ 4 s, saltable, con «Ver mapa»): es un cambio de D-012 que se declara en la sección 15 |
| **Dato del río** (cada 10) | **Es el dato de llegada:** sale en la victoria del `10k` (02 §6), en la placa con el retrato de TiTino (o de Rosa, sin T1), y se relee en la tarjeta. Así se cierran los tres sitios contradictorios del canon: victoria, pantalla de la Canasta y Mapa del 10 |
| **Frutas del río** (propuesta: una línea por victoria) | Canales separados:<br>- la **fruta**, en la victoria de cada nivel;<br>- el **lugar**, en la del `10k`.<br><br>En el `10k` gana el lugar y la fruta pasa a la siguiente. Las series de 20 caen sobre dos paradas (Agua del Tapajós 21-40, en Alter do Chão y la Floresta Nacional), y la fruta de la serie sale en la tarjeta. **Si se aprueba su opción A** (`ui_river_fact_every_n_levels` = 1), la línea de fruta necesita una clave propia, porque la cadencia de las paradas tiene que seguir en 10. Hay que contestar Frutas Q1 y T6 a la vez |
| **Bitácora del río** | Sin cambios. El «marco de país» del día 30 es un marco del Mapa |
| **Crecida** | Sin reflejo en el Mapa: el río nunca baja |
| **Reto del río** | Sin cambios. No mueve la canoa |
| **Salto del Boto** | La boya del nivel saltado lleva a Rosa. **Texto:** «Rosa te lleva río abajo» pasa a ser «Rosa te lleva río arriba, al siguiente nivel» (Q13) |
| **Mercado y ofertas** | El Mapa va **sin ofertas ni precios** (Q10):<br>- **Mochila de inicio:** la primera tarjeta **sigue en la victoria**, como fija el contrato cerrado (GDD §7.2, incidencia 11: «primera victoria con nivel ≥12 dentro de una sesión ≥2»; 06 §2.1, regla 1; 02 §13). Solo 05 §4.1 y §5 dicen «tarjeta en Mapa del río»: se corrigen;<br>- **Pase Expedición (1.2):** al Mercado o a un aviso en el inicio sin TiTino a la vista (05 §4.1 y §5 dicen «Mapa del río»);<br>- **entrada al Mercado:** desde el inicio (04 §4 dice «desde el Mapa del río»).<br><br>Rosa y TiTino nunca salen en pantallas de pago (07 §6.2; propuesta de TiTino, g.2). Con D-008 el jugador casi no pasa por el inicio, y por eso la Mochila se queda en la victoria |
| **D-008** (bucle de Magic Sort) | «Jugar» abre siempre el siguiente nivel. **Cambia la cadena corta de 02 §6** en los cambios de mundo: con Q4-A, Siguiente → AD_GATE → Mapa → N+1 (también por el camino de la Cosecha doble); con Q4-D, el viaje va en el banner. Sección 15 |
| **D-009** | Hito p20 = «Si lo superas, llegas a {sitio}»: **cambia el contenido de la tarjeta previa** que fija D-009, punto 2 (02 §10 y 07 §7.7). La temporada del ciclo es la LUT del fondo. Difícil (tarde [P]) y Muy difícil (noche, −15 %) son LUT sobre la misma escena |
| **D-010** (miquitos) | La especie sigue siendo por mundo. La tarjeta de la parada enseña «Miquitos de este tramo» (propuesta de fauna) |
| **D-011** | La llegada se ve en la victoria. El rótulo y los bocadillos siguen sus reglas de lectura |
| **D-003 y D-004** | Vidrio, madera y luz. El panel azul petróleo no cambia |
| **Inicio** (06 §4.7) | Fondo de la parada actual y «Faltan N para {sitio}». TiTino, solo en la ventanita de casa (sección 6.1, opción A) |
| **Cuaderno de campo** (1.1) | La página Ríos es el Muestrario |
| **«El río sigue creciendo»** (06 §9) | Su pantalla es el Mapa con los ríos que vienen en vidrio |

---

## 10. Fondos de tablero por parada (petición del dueño)

### 10.1 Principio

- **El fondo dice dónde está la canoa:** el mismo sitio que el Mapa, la misma agua base del mundo y la misma temporada del ciclo (por LUT).
- **Lo que no cambia:**
  - el **panel azul petróleo** (D-004) y su relleno opaco por código (direccion-visual-v2 §4.6; VA-04): nada del sitio entra en el panel;
  - la **banda de agua del primer plano**, que es el `water_base` del mundo (07 §2.1). Así el tablero, la intro de Llenar y teñir y el fondo cuentan la misma agua.
- **Si el agua del sitio es otra** (las dos aguas de Santarém y del Encuentro, o el Nanay y el Samiria negros en un mundo de agua blanca), va **en segundo plano**, en la orilla, en la cocha o en la línea entre aguas.
- **Coherencia por construcción.** La tarjeta de la parada, el inicio, el banner y la victoria con vista usan **la misma composición** que el tablero: no hay dos dibujos del mismo sitio que puedan contradecirse.

### 10.2 Construcción por capas (las de direccion-visual-v2 §3.4, con el panorama dividido)

| Capa | Hoy | Propuesta [P] | Quién |
|---|---|---|---|
| 0a Cielo y hora | Parte del panorama único (`bg_estuario_panorama`) | Degradado, sol y nubes **por código**, con la hora del mundo de 07 §2.1 | Código |
| 0b **Kit del mundo** | — | Un atlas por mundo con 8-10 piezas: orilla lejana en tiras que se repiten, selva y palmeras del mundo (açaí en Brasil, aguaje en Loreto), islas, playas de río, árboles en el agua, barcos lejanos sin personas y silueta de ciudad genérica | **Artista**, una vez por mundo |
| 0c **Héroe del sitio** | — | Un PNG **bajo y ancho**, de unos **240 × 80 dp**, con el pie anclado en el horizonte de `GameScreen.HorizonFor`: hasta 48 dp de silueta por encima del horizonte (lo reconocible, en sus 30 dp de abajo, que es lo que se ve con 2 filas) y hasta 32 dp de orilla y reflejo por debajo. El código alarga el reflejo hasta los 44 dp de la franja con el mismo sprite volteado, sin textura nueva (sección 10.6) | **Artista** |
| 0d Héroe por código | — | Las líneas entre aguas: Santarém (clara y blanca) y el Encuentro (negra y beige en diagonal, canon 07 §2.1). La Y de Nauta no: es una forma vista desde arriba y va en la tarjeta y en el Mapa (10.3) | Código (agente) |
| 0e Río | Banda del panorama | Banda con el preset del mundo y destellos (ya existen) | Código |
| 1 Ambiente | Garza y vitórias-régias (`LayoutBackdrop`) | Por sitio, con 16 partículas como mucho: pericos en Leticia, lomos de delfín en Tarapoto, bruma en Pacaya-Samiria | Código, con sprites del artista |
| 2 Muelle | `bg_estuario_muelle` | Uno compartido, con la LUT del mundo | Artista (canon) |
| 3 Follaje de esquina | `foliage_tl/tr/bl/br` | Dos juegos: Brasil (açaí y heliconia; el del canon) y Amazonia occidental (aguaje; nuevo, 4 piezas) | Artista |
| LUT | Variantes de 07 | Temporada del ciclo (D-009), Difícil y Muy difícil (D-009, punto 5), atardecer de la parada 9 y alba de la 19 (la hora base de Loreto ya es el atardecer, 07 §2.1) | Código |

- **La composición es un dato,** en `fondo` de cada parada (`river_stops_v1.json`): kit, héroe, pieza por código, LUT, y la posición y escala de las piezas del kit. La compone `StopBackdrop` con `UiBuilder`, como `LayoutBackdrop` hoy (`GameScreen.cs`).
- **Respaldo procedural mínimo,** solo para no bloquear el desarrollo ni las capturas y **nunca para enseñárselo al dueño** (VA-16; direccion-visual-v2 §4.9):
  - las piezas por código ya son definitivas;
  - los héroes pintados tienen una silueta geométrica de la misma altura (dos agujas sobre un rectángulo para Ver-o-Peso, un semicírculo con tambor para el Teatro, rectángulos sobre balsas para las casas del Itaya), que sirve también de héroe provisional para validar VA-20 antes de encargar el arte (10.6);
  - los kits tienen el panorama procedural actual.
- **El artista ve el resultado:** la herramienta de capturas genera una hoja con las 20 composiciones.
- **Arte provisional:** el dueño puede generarlo con ChatGPT (`docs/art/prompts-chatgpt.md`, entradas nuevas), pero **nunca va en la ficha, las capturas ni la UA** [P], la misma regla que 07 §6.4 fija para los miquitos provisionales.

### 10.3 Tabla de paradas y fondos de 1-200 (mundos 1-5)

Primer plano: el agua base del mundo (W1 blanca, W2 clara, W3 negra, W4 y W5 blanca). Todos los héroes están pendientes del asesor (07 §8.2). La columna **«Se reconoce por»** dice la verdad sobre cada sitio (Q5): **hito propio** (el lugar se ve), **línea entre aguas** (el fenómeno se ve) o **paisaje del tramo** (el fondo dice «estás en este tramo del río»; el lugar lo dicen el rótulo, el ambiente y la tarjeta).

| k | Parada | Héroe | Tipo | Se reconoce por | Hito en el fondo | Temp. (LUT) | Cuidado |
|---|---|---|---|---|---|---|---|
| 0 | Belém | `bg_hero_ver_o_peso` | Pintado | Hito propio | Torres del mercado de hierro (1901) desde la bahía de Guajará, al alba; palmeras de açaí | — (nivel 9: alba → mañana, canon) | Sin canastos (Canasta), sin el Círio y sin personas; sin los objetos rituales de las bancas de hierbas (§(e)) |
| 1 | Santarém | `codigo:dos_aguas_clara_blanca` | Código | Línea entre aguas | La línea del Tapajós verde azulado (delante) contra el Amazonas café (detrás), con la orla lejana del kit | — | Sin copiar grafismos de cuias |
| 2 | Alter do Chão | `bg_hero_alter_do_chao` | Pintado | Hito propio | La lengua de arena de la Ilha do Amor y el cerro de selva. El ciclo 21-40 es de aguas bajas, cuando la isla aparece | B | Sin cocoteros ni turquesa caribeño (guía §7); sin el Çairé ni bañistas |
| 3 | Floresta Nacional do Tapajós | `bg_hero_flona_tapajos` | Pintado | Paisaje del tramo | Orilla alta de selva de tierra firme sobre el Tapajós claro, con una playa estrecha | B | **Sin samaúma** (07 §8.1); sin comunidades ni madera cortada |
| 4 | Encuentro de las Aguas | `codigo:dos_aguas_negra_blanca` | Código | Línea entre aguas | La diagonal negra y beige (canon 07 §2.1), con Manaos lejana del kit | A | El nombre también es skin y evento |
| 5 | Manaos | `bg_hero_teatro_amazonas` | Pintado | Hito propio | La cúpula del Teatro Amazonas sobre la ciudad, en silueta baja, con el puerto flotante y el puente del río Negro al fondo | A | Sin nostalgia del caucho |
| 6 | Anavilhanas | `bg_hero_anavilhanas` | Pintado | Hito propio | Islas de selva sobre agua negra como espejo, con playas blancas | B | — |
| 7 | Lago de Tefé | `bg_hero_lago_tefe` | Pintado | Paisaje del tramo | Lago de agua negra con la orilla lejana de la ciudad y bosque. **Sin botos y sin orillas secas** | B (una LUT que no evoque la sequía) | **Asesor obligatorio:** el lago de Tefé vivió en 2023 una mortandad masiva de botos con la sequía y el agua muy caliente [V icmbio_tefe_2023], y 07 §8.1 veta las sequías de 2023-2024. Mamirauá (várzea blanca, a unos 150 km) va en la tarjeta, no en el fondo |
| 8 | Leticia | `bg_hero_leticia` | Pintado | Hito propio | El malecón y el río ancho, con la orilla de Tabatinga al fondo | A | **Ni Santa Rosa ni islas con bandera**; sin personas |
| 9 | Leticia al atardecer | = 8 | LUT de atardecer | Hito propio | La bandada de pericos (ambiente por código) | A | — |
| 10 | Amacayacu | `bg_hero_amacayacu` | Pintado | Paisaje del tramo | Árboles con el pie en el agua y el dosel alto | B | Sin comunidades |
| 11 | Puerto Nariño | `bg_hero_puerto_narino` | Pintado | Paisaje del tramo | La boca del Loretoyacu y el pueblo lejano, sin rasgos | B | **Asesor obligatorio** (07 §8.1: aldea identificable). Alternativa: solo la boca del río |
| 12 | Lagos de Tarapoto | `bg_hero_tarapoto` | Pintado | Paisaje del tramo (los lomos de delfín ayudan) | Lago quieto con lomos de delfín rosado y gris | A | Victorias regias lejos del borde del panel (mecánica Hoja) |
| 13 | El río frontera | `kit:rio_ancho_con_baliza` | **Solo kit** | Paisaje del tramo: rótulo «El río frontera · Colombia y Perú» | El Amazonas ancho entre dos orillas de várzea, con una baliza de navegación (piezas del kit) | A | Ninguna bandera en el agua ni en islas |
| 14 | Iquitos | `bg_hero_iquitos` | Pintado | Hito propio | El malecón con lanchas y los techos de la ciudad | A | Motocarros solo lejos y sin personas |
| 15 | Río Itaya (Iquitos) | `bg_hero_itaya_creciente` (reserva: `bg_hero_casa_de_fierro`) | Pintado | Hito propio | Casas que flotan sobre balsas en la creciente del Itaya (canon 07 §2.1), sin rasgos de barrio. El ciclo 141-160 es de aguas altas y el hito 160 es «Creciente del Itaya» | A | **Asesor obligatorio:** sin «Venecia», sin pobreza pintoresca y sin personas. «Belén» nunca como nombre de parada, rótulo ni ranura (07 §8.1; §(e)). La reserva va **sin el mito de Eiffel** |
| 16 | Allpahuayo-Mishana | `bg_hero_allpahuayo` | Pintado | Paisaje del tramo | Varillal de troncos finos sobre arena blanca, con la quebrada negra en segundo plano | B | Sin minería |
| 17 | Nauta | `kit:orilla_de_nauta` | **Solo kit** | Paisaje del tramo: rótulo «Nauta · Perú» | El río muy ancho con Nauta lejana (piezas del kit). La confluencia en Y es una forma vista desde arriba: sale en la tarjeta y en el Mapa, no en el panorama lateral, donde con dos aguas blancas no se leería | B | No inventar dos colores: las dos aguas son blancas. Alternativa: héroe pintado desde un punto alto real de Nauta, si se encuentra uno con fuente [NV] |
| 18 | Pacaya-Samiria | `bg_hero_pacaya_samiria` | Pintado | Hito propio | Primer plano: el agua blanca del mundo 5 (10.1). Segundo plano: la cocha de **agua negra** como espejo, con aguajes reflejados | B | Sin comunidades de la reserva |
| 19 | Pacaya-Samiria al alba | = 18 | LUT de alba, con bruma baja | Hito propio | La misma cocha. La hora base de Loreto ya es el atardecer (07 §2.1): con la LUT de atardecer, 18 y 19 se verían iguales | B | Sin playas al descubierto ni barro agrietado (R3) |

- **Recuento de 1-200:**
  - **14 héroes pintados y 1 de reserva:** 7 de ciudad o de forma única (Ver-o-Peso, Alter do Chão, Teatro, Leticia, Puerto Nariño, Iquitos y las casas del Itaya), la reserva (Casa de Fierro) y 7 de naturaleza (Floresta Nacional, Anavilhanas, lago de Tefé, Amacayacu, Tarapoto, Allpahuayo y Pacaya-Samiria);
  - **2 héroes por código** (Santarém y el Encuentro) y **2 paradas solo con el kit** (el río frontera y Nauta);
  - **5 kits y 2 variantes de luz;**
  - **18 sitios:** 8 se reconocen por un hito propio, 2 por la línea entre aguas y 8 son «paisaje del tramo» (6 con héroe pintado y 2 solo con el kit).
- **Antes de encargar estos héroes,** VA-20 se pasa con capturas del teléfono y el héroe provisional (10.6), sobre todo en los «paisajes del tramo».
- **El «río genérico»** (canon 07 §2.1 y 08 §6.2) sigue de respaldo para cualquier parada sin arte o sin pack.

### 10.4 Resumen de 201-1.000

| Mundo | Kit | Héroes candidatos (pintados) | Por código o solo kit |
|---|---|---|---|
| 6 Oriente | `bg_kit_w06`, nocturno (07 §2.1) | Lagunas negras de Cuyabeno; dosel de Yasuní; el Napo ancho en El Coca | Frontera de Nuevo Rocafuerte (solo kit) |
| 7 Madre de Dios | `bg_kit_w07` | Collpa de guacamayos; lago Sandoval con aguajes; Puerto Maldonado | Boca Manu (kit) |
| 8 Madidi | `bg_kit_w08` | Andes sobre la selva en Rurrenabaque; meseta y cascada de Noel Kempff; llanos de Moxos; Riberalta | — |
| 9 Ucayali | `bg_kit_w09` | Malecón de Pucallpa; Yarinacocha; Atalaya | Curvas del Ucayali (código) |
| 10 Alto Negro | `bg_kit_w10`, noche estrellada | Archipiélago de Mariuá; São Gabriel (asesor); Neblina como silueta | El Casiquiare como bifurcación (código) |
| 11 Escudo de Guayana | `bg_kit_w11` | Sabana del Rupununi y montes Kanuku; Voltzberg | Boa Vista (kit) |
| 12 Orinoco | `bg_kit_w12` | Caño Cristales rojo; Salto Ángel | — |
| 13 El origen | `bg_kit_w13`, LUT fría | Cañón del Apurímac; puna; nevado Mismi | Hilo de agua (código) |

- **Qué entra en la 1.0 si es la del GDD (Q14-A, 201-700, mundos 6-9):** solo el kit del mundo (`bg_kit_w06` … `bg_kit_w09`) y el rótulo del sitio, **sin héroe**. En 201-700 el fondo dice en qué mundo estás, no en qué sitio: el sitio lo dicen el rótulo, la tarjeta y el Mapa. Son 4 kits, unos 8 días de artista; la salida sin coste es el «río genérico» del canon (GDD §2 #20) con el rótulo. Los héroes de esos mundos llegan con sus packs, de la 1.1 en adelante. Las piezas del kit de cada parada de los mundos 6, 7, 8 y 9 (`kit:<pieza>`) están en las secciones 5.4 bis, 5.4 ter, 5.4 quater y 5.4 quinquies [P].
- **Recuento de 201-1.000:** 8 kits y **unos 40 héroes** [E] para unos 50 sitios.
- **Variantes:** los mundos largos reutilizan un héroe para dos o tres paradas, con otra luz o con la temporada del ciclo.

### 10.5 Cuándo y cómo cambia

1. **Sin precarga ni descarga.** Los kits y los héroes van como ranuras de `ArtSlots`, y `ArtSlotCatalog` es un catálogo único en `Resources/` con una referencia directa a cada Sprite, que se carga una vez y queda en un campo estático (`ArtSlots.cs`). Todo lo registrado queda **residente**, y `Resources.UnloadAsset` no lo libera mientras el catálogo lo referencie. Por eso cambiar de parada solo cambia qué sprite se pinta (sección 10.7, opción (a)).
2. **Al ganar el `10k`:** victoria con vista. El panel se desvanece y el fondo pasa al sitio nuevo con un fundido de 250 ms (`anim_bg_crossfade_ms`, constante [P]); la franja cuenta la llegada.
3. **Con «Siguiente», el nivel `10k+1` carga ya con su fondo.**
   - **Cero milisegundos añadidos** a la carga (< 300 ms, 08 §5.4) y a la intro (1,4 / 0,8 s, D-010).
   - Si la victoria con vista está apagada, el fundido va en paralelo a la construcción del tablero.
   - **Con Reducir movimiento,** corte directo.
4. **Si algún día pesa demasiado** (hoy no: 10.7), la salida es la opción (b): un catálogo por mundo, cargado y descargado por ruta (`Resources/AF/Stops/w0N`), con su propia tarea y sus días.
5. **Rótulo del lugar** [P, de C] (Q6), en el primer nivel de cada parada:
   - **Texto:** «Lagos de Tarapoto · Colombia», en Baloo 2 crema con contorno. El de la parada 15 dice «Río Itaya · Perú», **nunca «Belén»** (07 §8.1).
   - **Dónde:** sobre el horizonte en los tableros de 1-2 filas; en los de 3 filas, como placa bajo el HUD, en el sitio de los avisos.
   - **Reglas de D-011:** dura 1.000 ms + 55 ms por carácter, entre 3 y 7 s; sale cuando se habilita el toque, nunca durante la carga, la tarjeta ni la intro; uno cada vez; sin capturar toques.
   - **No sale** si el nivel tiene banner, Fruta nueva, tarjeta de miquito, estreno o tutorial. En 1-200 sale en 31, 51, 71, 91, 101, 121, 131, 151, 161, 171, 181 y 191.
6. **En los niveles 11, 41, 81 y 141,** el banner de D-012 ya presenta el sitio con su fondo.
7. **Si el pack del mundo no está** (PAD), se queda el kit con el «río genérico» y el nombre real del sitio en el Mapa, y se reintenta. Nunca se bloquea la partida (08 §6.2).

### 10.6 Legibilidad del tablero

- **Reglas que cumplen todas las composiciones:** VA-02, VA-03 (escena brillante: V medio ≥ 0,55, S ≥ 0,30), VA-04 (el panel no cambia), VA-08 (nada decorativo en las ranuras, los botones ni el chorro) y VA-16 (lista del dueño).
- **Composición: el héroe se define por lo que se ve sobre el horizonte, no por su caja.** El horizonte lo da `GameScreen.HorizonFor` = clamp(panel.top − 64, 118, 200) dp a 360 dp de ancho (`GameScreen.cs`); no es un valor fijo.
  - **Con 2 filas** (nivel 7, a 780 dp), entre el HUD y el panel solo se ve la franja de 88 a 162 dp (74 dp; direccion-visual-v2 §3.3), con el horizonte en 118: **30 dp sobre el horizonte y 44 por debajo**. Lo reconocible del hito tiene que caber en esos 30 dp: una silueta baja y ancha (la cúpula del Teatro con su tambor, las dos torres de Ver-o-Peso, la lengua de arena de la Ilha do Amor), no la fachada entera de un edificio.
  - **Con 1 fila** (nivel 1, panel en 226,9 dp), el horizonte baja a 163-187 dp y caben hasta 48 dp de silueta. En el inicio y en la victoria con vista se ve el héroe entero.
  - **Por debajo del horizonte,** la orilla y el reflejo, ≤ 44 dp (10.2).
  - **Antes de encargar el arte,** se valida VA-20 con el héroe provisional (10.2) en las capturas `02_board_l7` (2 filas) y `02b_board_l1` (1 fila).
- **Criterios nuevos** [P, de B] para direccion-visual-v2 §9:

  | ID | Criterio | Medida |
  |---|---|---|
  | VA-20 | **Hito visible:** en la captura del tablero de 2 filas a 780 dp, el héroe ocupa ≥ 30 % de la franja visible y se reconoce; en los «paisajes del tramo» basta con que el rótulo y el ambiente lo sitúen (10.3) | Revisión del dueño con el asesor, con capturas del teléfono |
  | VA-21 | **Calma junto al panel:** en los 16 dp sobre su borde superior, ningún detalle con ΔL* > 30 | Script |
  | VA-22 | **Sin confusiones:** ni canoas a menos de 48 dp del HUD, ni canastos, ni frutas sueltas, ni personas, ni banderas, ni cuias en el fondo (las banderas y las mini cuias son del Mapa) | Revisión |
  | VA-23 | **Agua coherente:** la banda del primer plano queda a ΔE00 ≤ 5 del preset del mundo. La muestra del Muestrario lleva el agua del **sitio**, que puede ser otra (Allpahuayo y Pacaya-Samiria: negra) | Script |

- **El límite, medido en los packs [cálculo].** Con 11 cuias o más, el tablero va en 3 filas y el panel empieza bajo el HUD (y = 80 a 780 dp; direccion-visual-v2 §3.3), así que **no queda franja**.

  | Mundos | Tableros de 3 filas |
  |---|---|
  | 1-3 | 0 |
  | 4 | 34 de 60 |
  | 5 | 48 de 60 |

  En esos niveles el sitio se ve:
  - en la **victoria con vista**;
  - en el **inicio**, que enseña el fondo entero;
  - en el **banner**;
  - en el **rótulo**, como placa bajo el HUD;
  - en el **follaje** de las esquinas, que es del kit;
  - en la **tarjeta** del Mapa.

  **No se toca el panel:** su relleno opaco es lo que hace legible el tablero (D-004).

### 10.7 Tamaño y memoria (AAB ≤ 80 MB y PAD)

Cálculo con ASTC, bloque de 128 bits: 6×6 = 3,56 bpp y 8×8 = 2 bpp [V Wikipedia ASTC]. Las piezas de fondo, a 3 px/dp. Los tamaños salen de contar bloques: por ejemplo, 720 × 240 px son 120 × 40 bloques de 6×6, a 16 B cada uno, 75,0 KiB [cálculo].

| Pieza | Píxeles | ASTC 6×6 | ASTC 8×8 |
|---|---|---|---|
| Héroe (240 × 80 dp) | 720 × 240 | 75,0 KiB | — |
| Kit del mundo (atlas) | 1024 × 1024 | 456,9 KiB | 256 KiB |
| **Fondos de los mundos 1-4** (4 kits y 10 héroes; install-time) | | 2,52 MiB | **1,73 MiB** (kits en 8×8) |
| **Fondos de Loreto** (1 kit y 5 héroes, con la Casa de Fierro) | | 0,81 MiB | 0,62 MiB |
| **Fondos de 1-200** | | **3,33 MiB** | **2,35 MiB** |
| Kits de 201-700 (solo con Q14-A; 4 kits) | | 1,78 MiB | 1,00 MiB |
| Texturas de terreno del Mapa (5 de 512²) | | 0,56 MiB | — |
| Atlas de UI del Mapa (1024², ASTC 4×4 como toda la UI), con las siluetas de las muestras horneadas | | 1,0 MiB | — |
| Datos (`river_map_v1` + `river_stops_v1`) | | ≈ 0,1 MiB | — |
| **Mapa** | | **≈ 1,7 MiB** | |

- **Encaje en el presupuesto:**
  - 08 §6.1 reserva **unos 4-6 MB para los fondos de los mundos 1-4** (4 pinturas de 1080 × 2400 en ASTC 6×6 más variantes). Los fondos por parada de esos mundos ocupan **1,7-2,5 MiB**, porque el cielo pasa a hacerse por código, los kits son atlas y los héroes son bajos. El margen paga el muelle y los dos juegos de follaje (≈ 1,5 MiB, piezas de D-003).
  - **El Mapa suma ≈ 1,7 MiB.**
  - **Impacto neto en el AAB** frente al canon [cálculo]: entre −2 y +1 MB con Q14-B, y entre −1 y +3 MB con Q14-A (los 4 kits de 201-700).
  - El AAB de humo mide 35 MB y la compuerta es de 80 MB (CLAUDE.md; 08 §6.1, base estimada en 65-76 MB). **Se mide** con `pwsh -NoProfile -File ci/size-gate.ps1 <aab>` tras integrar los 3 primeros sitios, antes de encargar el resto.
- **Recomendación:** kits en **ASTC 8×8**, porque son lejanía con bruma, y héroes en **6×6**, porque son el detalle reconocible. Suben de calidad solo si la captura del teléfono enseña bandas. Los ajustes de importación los aplica un `AssetPostprocessor` versionado, nunca el `.meta` a mano (regla dura 3).
- **PAD:**
  - **Addressables no está instalado** (`Packages/manifest.json`, comprobado; instalarlo exige el visto bueno del dueño, regla dura 4). **Hasta entonces, todo va en el AAB**: Loreto suma unos 0,6-0,8 MiB y, con Q14-A, los 4 kits de 201-700, unos 1,0-1,8 MiB.
  - **Cuando se apruebe,** el kit y los héroes de cada mundo desde el 5 viajan en `world_w05` … `world_w13` (08 §6.2), dentro de los 2-4 MB por pack del canon.
  - **201-1.000 completo:** 8 kits y unos 40 héroes ≈ 6,5 MiB en 6×6 (≈ 4,9 MiB con los kits en 8×8) [cálculo], repartidos por PAD.
- **Memoria en juego: todo residente (opción (a)).** El catálogo de ranuras de hoy no descarga nada (10.5, punto 1), así que la promesa de la versión anterior («nunca más de un kit y dos héroes en memoria») no se cumplía con ese mecanismo. Se acepta y se dice:
  - **residente siempre:** los fondos de 1-200 (2,4-3,3 MiB), los kits de 201-700 con Q14-A (1,0-1,8 MiB), el muelle y el follaje (≈ 1,5 MiB) y las texturas y el atlas del Mapa (≈ 1,6 MiB): **unos 6,4-8,2 MiB**, muy por debajo de los **120 MB de texturas residentes** de 08 §5.4;
  - **solo con el Mapa abierto,** ≈ 0,6 MiB de mallas, que se liberan al cerrarlo;
  - **las siluetas de las muestras** se hornean en el atlas del Mapa (en el Editor o en `tools/river-map`); en tiempo de ejecución no se lee ningún héroe;
  - **la opción (b)** (un catálogo por mundo cargado por ruta) queda como salida si la medida en el teléfono lo pidiera; hoy no compensa sus días.
- **Gama media:** ninguna capa nueva a pantalla completa (direccion-visual-v2 §8: ≤ 3). El héroe mide unos 240 × 80 dp y el kit se compone en la misma capa 0.

### 10.8 Coste de arte de los fondos [E]

| Partida | 1-200 | Días de artista |
|---|---|---|
| Kits de mundo (8-10 piezas cada uno) | 5 × 2 d | 10 |
| Héroes de ciudad o de forma única, más la reserva | 8 × 1 d | 8 |
| Héroes de naturaleza | 7 × 0,75 d | 5,25 |
| Sprites de ambiente (pericos, lomo de delfín, bruma) | 3 | 1 |
| Segundo juego de follaje (Amazonia occidental, con aguaje) | 4 piezas | 1,5 |
| Río genérico (canon) | 1 | 3 |
| **Total de fondos de 1-200** | | **≈ 28,75** |

- **Frente al canon:** 07 §11 prevé 18 días (6 pinturas de 3 días, con el río genérico). **Diferencia: unos +11 días de artista.** Los héroes más bajos (240 × 80 dp) no se abaratan en la cuenta, por prudencia.
- **Opción barata (Q5-B):** héroe pintado solo en las 7 ciudades y formas únicas y la reserva; las 7 paradas de naturaleza se componen con el kit (trabajo de agente). Sale en unos 23,5 días, **unos +5,5**.
- **Con Q14-A:** los 4 kits de 201-700, **unos 8 días** más (o ninguno con el «río genérico» del canon).
- **201-1.000 completo:** 8 kits (16 d) y unos 40 héroes (32-40 d) [E], en las fases de arte de la 1.1 en adelante.

---

## 11. Técnica en Unity

### 11.1 Arquitectura (sin escena nueva, sin prefabs y sin YAML a mano)

| Pieza | Ensamblado | Qué hace |
|---|---|---|
| `RiverJourney` | `AF.GameFlow` (sin motor) | Funciones puras sobre `ProgressData.MaxLevel`, `NextLevel`, `SkippedLevels` y el campo nuevo `MapSeenLevel` (`Save/Profile.cs`; sección 11.5) y `WorldTable`: lo pendiente de animar (de `MapSeenLevel` a `MaxLevel`), la parada `k = (nextLevel − 1) / 10`, la fracción de llenado, las bocas pasadas, las banderas, los km, los niveles que faltan y si la victoria del nivel `n` es llegada (`n % 10 == 0`) o cambio de mundo |
| `MapMoments` | `AF.GameFlow` | Qué toca en cada victoria: franja, Mapa automático o nada. Lee las claves de RC de 11.5 |
| `RiverStopCatalog` y su lector | `AF.Levels` (sin motor) | Carga `river_stops_v1.json` y lo contrasta con `WorldTable`, como `FaunaCatalogReader`. `StopOf(level)` sin asignaciones |
| `RiverMapScreen` | `AF.UI` | Pantalla construida por código en `Game.unity`, como `LobbyScreen`, en superposición sobre el inicio, la pausa o la victoria: cabecera, zona del mapa con `RectMask2D`, ventanita de casa, Muestrario y «Jugar» |
| `RiverRibbonGraphic` | `AF.Presentation` | `MaskableGraphic` con `OnPopulateMesh`: tira de triángulos con uniones en inglete y **color por vértice**, siguiendo el patrón de `WaterStripGraphic` y `BezierStripGraphic`, que ya existen. Tres instancias: **vidrio** (toda la cuenca, una vez), **lleno** (al abrir y tras una llegada) y **frente** (solo el tramo que se anima, ≤ 400 vértices; desde la revisión de FX11, sin ese tope [P]: ver la nota de 11.4) |
| `LandGraphic` | `AF.Presentation` | Tierra ya triangulada en la herramienta, con texturas en mosaico |
| `PolylineCursor` | `AF.Presentation` | Posición y tangente por longitud de arco, con búsqueda binaria sobre `float[]`: canoa, Rosa y cámara |
| `StopBackdrop` | `AF.Presentation` | Compone el fondo (kit + héroe + código + LUT, con el reflejo del héroe por código) en el tablero, el inicio, el banner y la tarjeta, con el fundido. Sin precarga ni descarga: las ranuras quedan residentes (10.5 y 10.7) |
| `ArtSlots` | `AF.Presentation` | Prefijos nuevos sin la marca (glosario §11): `bg_kit_w{nn}`, `bg_hero_<sitio>`, `map_tile_*`, `map_canoe`, `map_flag_<iso2>`, `map_minicuia`, `map_rack`, `map_compass`, `map_home` y `amb_*`. Todo lo que se registra aquí queda residente, porque `ArtSlotCatalog` referencia cada Sprite (10.7) |

- **Sin shader propio:** vidrio y agua son colores por vértice sobre el material de UI por defecto, y los brillos, sprites aditivos (sin Bloom, guía §7).
  - **El agua que corre del final** desplaza el UV de una textura de ondas en una instancia de material creada una sola vez.
- **Sin `LineRenderer` ni SpriteShape:** trabajan en espacio de mundo, y todo el juego es uGUI en `ScreenSpaceOverlay`. SpriteShape 13.0.0 está instalado, pero no es de uGUI.
- **Sin paquetes nuevos** (regla dura 4) y sin tocar `Packages/com.amazonfill.core/`.

### 11.2 Datos

**`Assets/_Project/Data/Map/river_map_v1.json` (geometría)** [P]:

```json
{
  "format": "af-river-map/1",
  "frame": { "rotation": "cw90", "lat": [-18.0, 7.0], "lon": [-80.0, -47.0], "lat0": -5.0 },
  "lines": [
    { "id": "amazonas", "kind": "main", "class": 1, "src": "ne10m", "pts": "<base64 uint16 x,y delta>",
      "water": [{ "s0": 0.0, "s1": 0.58, "type": "white" }],
      "splits": [{ "id": "encontro", "s0": 0.214, "s1": 0.219, "left": "black", "right": "white" }] },
    { "id": "xingu", "kind": "passby", "class": 2, "water": [{ "s0": 0, "s1": 1, "type": "clear" }],
      "mouth": { "on": "amazonas", "s": 0.053 }, "fill_on_pass": 1.0, "pts": "..." },
    { "id": "madeira", "kind": "tributary", "class": 1, "water": [{ "s0": 0, "s1": 1, "type": "white" }],
      "mouth": { "on": "amazonas", "s": 0.198 }, "fill_on_pass": 0.10, "pts": "..." }
  ],
  "land": { "verts": "<base64>", "tris": "<base64 uint16>", "zones": [{ "id": "andes", "tile": "rock" }] },
  "insets": [{ "id": "orinoco", "rect": [0.78, 0.05, 0.98, 0.30], "lines": ["orinoco"], "link": "casiquiare" }],
  "home": { "on": "para", "s": 0.97 }
}
```

**`Assets/_Project/Data/Map/river_stops_v1.json` (paradas y tramos)** [P]: la estructura de `paradas_1_0` de `rio-amazonas.json`, más la ruta de cada tramo como lista de tramos de línea, para que la canoa siga el agua real con ida y vuelta por las confluencias:

```json
{ "n": 14, "levels": [141, 150], "world": 5, "id": "iquitos", "kind": "gate", "country": "PE",
  "at": { "line": "amazonas", "s": 0.437 }, "km_from_sea": 3700, "sample": "white", "flag_raise": "PE",
  "arrival_fact": "map.country.pe.dato_01", "facts": ["map.stop.iquitos.dato_01"],
  "world_question": "map.world.w05.question",
  "bg": { "kit": "bg_kit_w05", "hero": "bg_hero_iquitos", "code": null, "lut": null },
  "leg": { "path": [{ "line": "amazonas", "s0": 0.49, "s1": 0.437 }], "via": ["caballococha", "pebas"], "mouths": ["napo"] } }
```

- **Tamaño:** unos 3.300-4.500 puntos × 4 bytes ≈ 13-18 KB en binario, más la tierra triangulada (20-40 KB) y unos 25 KB de las 100 paradas: **unos 70-90 KB** [cálculo].
- **Contrato:** son dos formatos nuevos. Si se quieren con esquema (`schemas/af-river-map-1.schema.json`), entran como contrato nuevo de GDD §7 con la aprobación del dueño.
- **La tabla de paradas no va en Remote Config** (como el catálogo de fauna, 08 §5.3).

### 11.3 Herramienta `tools/river-map/` (Python 3.14, solo biblioteca estándar)

1. **Lee** Natural Earth 1:10m (vista Cuenca) y HydroRIVERS (vista Mundo y ríos locales) desde `tools/river-map/source/`: lector mínimo de `.shp` y `.dbf` con `struct`, sin paquetes nuevos (los dos son shapefiles).
2. **Selecciona** los ríos por nombre (con el **Tocantins**, que da la forma de la zona de salida, y los ríos locales de las paradas), recorta a la caja, gira, simplifica (Douglas-Peucker a unos 2 km en la Cuenca y a unos 0,3 km en la vista Mundo: 300-800 puntos en el cauce y 100-300 por afluente), cuantiza a uint16 y triangula la tierra (orejas). Las **zonas del terreno** (dosel, várzea, sabana, roca y nieve) no las trae Natural Earth: salen de la máscara que pinta el artista sobre la guía de esta herramienta (sección 12), que aquí se vectoriza.
3. **Calcula:**
   - los tramos de agua por tipo;
   - la posición `s` de cada boca y de cada parada;
   - las franjas partidas de Santarém y del Encuentro;
   - los km hasta el mar;
   - las rutas por agua entre paradas (camino más corto en el grafo de confluencias).
4. **Valida:**
   - cada punto de control de `rio-amazonas.json` queda a **≤ 2 dp de su línea en la vista donde se ve**: ≤ 15 km en la Cuenca (13,1 dp por grado) y ≤ 1 km en la vista Mundo a 15× (Natural Earth avisa de alineaciones dudosas en la cuenca y solo garantiza hasta 1:30 millones, sección 3.6);
   - cada tramo del cauce mide al menos la línea recta entre sus extremos (los km salen de la longitud de arco, ajustada a 6.575 km);
   - las rutas son continuas y nunca cruzan tierra;
   - los km nunca retroceden entre paradas del mismo río;
   - las paradas caen en `10k+1` y respetan `WorldTable`;
   - cada dato mide ≤ 90 caracteres y cada pregunta ≤ 50;
   - cada etiqueta «[V id]» o «[E id]» lleva a una fuente con URL, y un dato [V] tiene al menos una fuente [V] (como ya hace `generar_mapa.py --check`);
   - los cruces de país se derivan de las paradas.
5. **Escribe** los dos JSON y una **guía en PNG o SVG** por mundo para el artista. Para esto reutiliza `generar_mapa.py` de esta carpeta, que ya hace la proyección, el llenado y la validación de la muestra.
6. **`--check`:** código 1 si algo falla. Se añade a los comandos de verificación de CLAUDE.md en el lote de documentación.
7. **Mientras no llegue Natural Earth,** la pantalla se puede construir con la geometría de `rio-amazonas.json` como respaldo: la forma es correcta a escala de cuenca y le faltan los meandros.

### 11.4 Dibujo, rendimiento y accesibilidad

- **Al abrir el Mapa** (asignaciones permitidas una sola vez): se decodifican las líneas en `Vector2[]`, se calculan las longitudes acumuladas en `float[]` y se construyen las mallas de vidrio, lleno y tierra. Son unos 9.000 vértices de ríos y unos 3.000 de tierra, por debajo del límite de 65.000 por malla [cálculo].
- **Por fotograma, cero asignaciones** (la regla 2 del bucle caliente, aplicada por prudencia):
  - solo se reconstruye la malla del **frente** (≤ 400 vértices; desde la revisión de FX11, sin ese tope [P]: ver la nota de abajo) cuando cambia el llenado;
  - canoa y Rosa con `PolylineCursor`;
  - cámara con `anchoredPosition` y `localScale`;
  - «Faltan N» y los km con `TMP_Text.SetText(string, float)` solo cuando cambian.

  Se comprueba con un test EditMode con `Is.Not.AllocatingGCMemory()` y con el Profiler, que tiene que dar GC Alloc = 0 durante el viaje.

  > **Nota de la revisión de FX11 (2026-10-19), desviación provisional [P], pendiente del dueño.** Las tres opciones que tiene: mantener el tope de ≤ 400, quitarlo (lo que hace ahora el cliente) o un tope por pieza igual al coste de su intervalo (la alternativa del final de esta nota). El frente ya no lleva el tope de ≤ 400 vértices. Con el tope repartido entre las piezas del tramo (80 por pieza en el del 300: Napo, Amazonas, Madeira, Beni y Madre de Dios), cada cinta saltaba puntos, y el agua y su orla, con distinto coste por sección, saltaban puntos distintos: en Mundo se apartaban del vidrio de 5 a 20 dp (dos cauces a la vista). Ni siquiera 400 por pieza bastan: la del Amazonas pide 360 vértices de agua y 504 de orla. Desde entonces cada cinta del frente y su orla llevan el tope de una malla de uGUI (65.000), como el vidrio y el lleno, y no saltan puntos (`RiverMapScreen.BuildFront`). Medido en el tramo del 300 (5 piezas, el caso de la revisión): **2.534 vértices** entre agua (1.028) y orla (1.506) en Mundo y 724 en Cuenca; `Fx11Tests` lo acotaba en 3.000. **Revisión de FX12 (2026-10-19), provisional [P], pendiente del dueño:** con el trazo curvo de la vista Mundo partido hasta que sus codos no se ven (≤ 10° y flecha de ≤ 0,3 dp), el frente del 300 pide **5.764 vértices** en Mundo (2.326 de agua y 3.438 de orla) y `Fx11Tests` lo acota en 6.500. No cabe en 3.000 con ningún reparto medido: el más barato con esa calidad (puntos de la curva cada 3 anchos y dos pasadas del filtro) pide 4.088, pero aparta la curva hasta 6 dp de la polilínea en la canoa. Otra opción sería partir la curva solo en lo que se ve en pantalla, a cambio de rehacer el vidrio al arrastrar el Mapa. El frente solo se rehace cuando cambia su llenado, una vez por boya (≤ 10 por apertura). **Memoria durante el viaje** (`Fx11Tests`, en el Editor): los fotogramas sin cambio del llenado dan GC Alloc = 0, en el Tick y en la actualización del lienzo; los 10 fotogramas en que cambia (uno por boya) asignan de 27 a 38 bloques de GC.Alloc, **lo mismo con el tope de 400 repartido que sin él**: no lo causa este cambio, pero tampoco cumple el GC Alloc = 0 de arriba y queda por investigar (sin medir en un dispositivo ni con el Profiler). Alternativa si el dueño quiere mantener un tope: uno por pieza igual al coste de su intervalo de la línea (lo que pide sin saltar puntos), que da los mismos vértices.
- **Canvas:**
  - uno **estático** (tierra, vidrio y lleno);
  - uno **dinámico** anidado (frente, canoa, Rosa y brillos);
  - uno de **UI**.

  Es la regla de 08 §5.4 y de la guía §8. **Riesgo [NV]:** que uGUI vuelva a montar los lotes al arrastrar. Se mide; si pasa de 1 ms en clase A, el Mapa pasa a `MeshRenderer` con cámara propia.
- **Frecuencia:** `OnDemandRendering` a 30 fps en reposo (`perf_target_fps_menus` = 30) y 60 durante el viaje o el arrastre.
- **Presupuesto:** GPU ≤ 6 ms en clase A y ≤ 3 ms en clase B, y unos 8-10 draw calls. Está sin medir: no hay un dispositivo de esas clases (guía §8).
- **Accesibilidad:**
  - texto ≥ 4,5:1 sobre las placas, y tamaño del sistema al 130 % sin recortes;
  - muestras tocables ≥ 48 dp, con su **texto accesible en los datos** («Santarém, Brasil, río Tapajós, niveles 11 a 20, visitado»). **La capa que lo lee con TalkBack no está en este plan:** el proyecto aún no tiene ninguna capa de accesibilidad (`Assets/_Project/Scripts`: 0 resultados de «Accessibility», «TalkBack» o «contentDescription») y 08 §14.2 la pide para todo el juego. Es la tarea general de accesibilidad, que el Mapa aprovechará;
  - con Patrones de fruta (02 §12), las cintas llevan una trama por tipo de agua (puntos en la blanca, rayas en la negra, ondas en la clara), y el lleno frente al vidrio se distingue también por el trazo, no solo por el color;
  - Reducir movimiento (sección 4.2).

### 11.5 Guardado, Remote Config y analítica

- **Guardado: un campo nuevo en la 1.0, sin migración** [P]:
  - **`progress.map_seen_level`** (entero; por defecto 0): el último nivel cuyo estado del Mapa ya vio el jugador (sección 4.2). En la fusión de Saved Games (08 §11) gana el máximo. Añadirlo no exige migrar: el esquema no se ha publicado, y `Profile.cs` lo dice así («Añadir un campo nuevo con un default razonable no exige migración mientras no se haya publicado el esquema»; `CurrentSchema` = 1). Se aprueba en el lote de documentación con 08 §4.3;
  - **lo demás se deduce de `max_level`, `skipped_levels`, `WorldTable` y el catálogo:** la posición, el llenado, las muestras (también la de los miradores, que se gana cuando el progreso los revela), los afluentes y las banderas;
  - **en la 1.1,** si hace falta: `settings.lobby_scene` y el estado del «¿Verdad o mito?»;
  - **si el dueño no aprueba el campo,** se quitan el punto del icono y la muestra al tocar, y la llegada se repite al abrir el Mapa en los 3 primeros niveles de una parada (4.2).
- **Remote Config:** solo **propuestas** para 08 §9, la tabla única, con los prefijos existentes.

  | Clave [P] | Tipo | Por defecto | Rango | Para qué |
  |---|---|---|---|---|
  | `ui_map_mode` | string | `stops` | `stops` · `worlds` | Interruptor y A/B: `worlds` vuelve a 13 paradas (canon). Es **un filtro** de la misma pantalla y del mismo catálogo (solo las grandes paradas y los cierres, sin boyas intermedias), no una segunda implementación |
  | `ui_map_autopen` | string | `l10_world` | `l10` · `l10_world` | Q4 |
  | `ui_bg_per_stop` | bool | true | — | false = **el kit del mundo con el héroe de su gran parada, fijo en todo el mundo**. Las pinturas por mundo de 07 §11 ya no se producen (10.8), así que no hay otro «fondo del mundo» al que volver. Es un cambio de búsqueda en el catálogo, sin arte ni código aparte |
  | `ui_stop_label` | bool | true | — | Rótulo del lugar |
  | `ui_victory_view` | bool | true | — | Victoria con vista |
  | `anim_map_world_ms` | int | 6.000 | 3.000-8.000 | Tope del viaje de mundo |

  - **`ui_river_fact_every_n_levels` sigue en 10** y tiene que valer lo mismo que la cadencia de paradas.
  - **Las demás duraciones van como constantes [P],** como en D-011: tramo de 1,2 s, fundido de 250 ms, bandera de 0,8 s.
  - **En la 1.1:** `ui_map_quiz` (`off` · `victory` · `map`; por defecto, `off`).
- **Analítica:** no existe `AF.Analytics`. Las altas van en 08 §8.2 con la aprobación del dueño y, mientras tanto, en logcat en los builds de desarrollo, como `AF_STATE`.

  | Evento [P] | Parámetros | Para qué |
  |---|---|---|
  | `af_map_open` | `source ∈ {l10, world_change, lobby, pause, victory, content_end}`, `stop`, `world` | Uso del Mapa |
  | `af_map_close` | `dwell_ms`, `stops_tapped`, `view_changes`, `north_up` | Interés |
  | `af_stop_arrive` | `stop_id`, `world`, `level`, `view ∈ {victory, map}`, `skipped` | Llegadas vistas o saltadas |
  | `af_river_fact_shown` (ya existe) | `fact_id` = la clave del dato | Lectura |

  - **KPI:** D7 y D30 de la cohorte con `ui_map_mode` = `stops` frente a `worlds`.
  - **Ningún A/B público aísla un mapa pasivo** [NV, no encontrado]: esta sería la evidencia propia.
  - **En `AF_STATE`** de desarrollo se añaden `stop` y `map_open`, para verificar en el teléfono por logcat.

### 11.6 Tests

| Tipo | Qué prueba |
|---|---|
| EditMode | **`RiverJourney` para 1-1.000:** parada, llenado, bocas, banderas (con las vueltas a un país), km y «Faltan N», con niveles saltados, los límites 10/11, 40/41, 140/141, 200/201, 700/701 y 1.000, el fin de contenido (700 con Q14-A, 200 con Q14-B) y lo pendiente entre `map_seen_level` y `max_level`.<br>**`progress.map_seen_level`:** valor por defecto, perfil sin el campo y fusión en la nube (gana el máximo).<br>**`MapMoments`** por nivel y por RC.<br>**El catálogo contra `WorldTable`:** cada mundo empieza en su primer nivel; todas las paradas en `10k+1`; fondo o respaldo; dato que existe y mide ≤ 90; preguntas ≤ 50.<br>**`PolylineCursor`** sin asignaciones |
| PlayMode | El Mapa se abre desde el inicio y desde la pausa, y vuelve al nivel sin gastar canoa. Franja en la victoria del `10k`. Victoria con vista. Viaje de mundo dentro del banner (Q4-D) o en el Mapa tras AD_GATE con «Jugar» (Q4-A), también por la Cosecha doble. Rótulo con las reglas de D-011. Reducir movimiento. Capturas `12_map_mundo`, `13_map_cuenca`, `13b_map_norte`, `14_victory_arrival`, `15_rack` y `02f_board_stop_<id>`, a 360 × 640 y 360 × 780 y con el texto al 130 % |
| Herramientas | `py -3.14 tools/river-map/build.py --check` y sus tests con `unittest` |
| Rendimiento | GC Alloc = 0 por fotograma con el Mapa abierto (Profiler) y medida en el teléfono con `AF_PERF` |

### 11.7 Entrega

- **1.0, todo en el AAB** (en `Resources/`, como `ArtSlotCatalog`) mientras Addressables no esté aprobado. Con ese mecanismo, todo queda **residente** (10.7): unos 6,4-8,2 MiB, aceptados.
- **Después,** según 08 §6.2: mundos 1-4 en `core_install` y los demás por PAD, con el «río genérico» de respaldo.
- **El Mapa base, las texturas y el atlas,** install-time.

---

## 12. Lista de arte para el artista freelance

Todo el arte nuevo es del artista (CLAUDE.md, «Humanos»).
- **Entrega:** PNG o PSD por Git LFS, con capas.
- **Estilo:** D-003, con la clave de estilo de `docs/art/prompts-chatgpt.md`.
- **Referencias:** se pinta a partir de varias fotos, sin calcar ninguna, y con las referencias que dé el asesor.
- **Revisión:** cada pieza pasa la lista de 07 §8.2: personas, símbolos, decoración propia, lugar de la cuenca, palabras permitidas y visto bueno del asesor.
- **Orden:** los héroes se encargan **después** de pasar VA-20 en el teléfono con el héroe provisional (10.6), empezando por los 3 primeros sitios (y midiendo el AAB, 10.7).

| # | Pieza | Ranura | Entrega | 1-200 | Notas |
|---|---|---|---|---|---|
| 1 | Kit del mundo | `bg_kit_w01` … `w05` (con Q14-A, también `w06` … `w09`) | Atlas de 2048² (a 2×), con 8-10 piezas | 5 (+4 con Q14-A) | Tiras de orilla sin costura; barcos lejanos sin personas |
| 2 | Héroe del sitio | `bg_hero_<sitio>` | 1440 × 480 (2× de 240 × 80 dp), con capas | 14 y 1 de reserva | Tabla 10.3. El pie, a la altura del horizonte; lo reconocible, en los 30 dp de abajo; hasta 48 dp de silueta y 32 de orilla y reflejo (10.6) |
| 3 | Sprites de ambiente | `amb_parakeets`, `amb_dolphin_back`, `amb_mist` | 256² | 3 | Animados por código |
| 4 | Follaje | `foliage_<juego>_tl/tr/bl/br` | ≤ 480 × 510 | 1 juego nuevo × 4 (el de Brasil es el del canon) | El açaí solo en el juego de Brasil; el nuevo, de Amazonia occidental, con aguaje |
| 5 | Texturas de terreno | `map_tile_canopy/varzea/savanna/rock/snow` | 512², sin costuras | 5 | Brillantes, de juego; nada de papel |
| 6 | Máscara de zonas del terreno | Dato de `tools/river-map` | PNG o SVG por zonas, sobre la guía de F2 | 1 | Natural Earth no trae dosel, várzea, sabana, roca ni nieve: las dibuja el artista y la herramienta las vectoriza (11.3) |
| 7 | Canoa con Capi remando | `map_canoe` (rig de Capi) | Bucle de 12 fotogramas a 32-40 dp; cuias atadas con cuerda | 1 | Del bucle de `capi_rescue`; mástil con anclaje para la bandera; sin personas |
| 8 | Rosa en el Mapa | Poses del canon reducidas, más `rosa_farewell`; con Q18-A, la paleta del bufeo boliviano | Rig de Rosa | +1 pose (+1 paleta) | La despedida del 961; el bufeo, sin nombre propio |
| 9 | Mini cuia y su etiqueta | `map_minicuia` | 96 × 160, vidrio en capas | 1 | Silueta de D-006, **nunca un tubo**; el líquido lo pinta el código; las siluetas de los sitios se hornean en el atlas (10.7) |
| 10 | Repisa del Muestrario | `map_rack` | 9 cortes | 1 | Madera tallada y repisa (guía §4.4 y §4.5) |
| 11 | Casa de TiTino | `map_home` y `map_home_window` | 256² y 9 cortes | 2 | La embaúba a escala de Mapa (propuesta c.7); la misma ventanita sirve en el inicio (Q17); alternativa por si el asesor la desaconseja |
| 12 | Banderas | `map_flag_<iso2>` | 96 × 64, con 2 fotogramas | 3 en 1-200; 5 en 1-700 (Q14-A); 7 en total | Oficiales; Bolivia, la tricolor (5.3) |
| 13 | Rosa de los vientos, placas, boya, baliza y «?» | `map_compass`, `map_plaque`, `map_buoy`, `map_beacon`, `map_mirador` | Atlas de UI | 5 | Madera y vidrio |

**Coste del Mapa [E]:**

| Partida | Días |
|---|---|
| Texturas (5 × 0,5 d) | 2,5 |
| Máscara de zonas del terreno | 1 |
| Canoa con Capi | 1 |
| Rosa reducida, `rosa_farewell` y la paleta del bufeo (Q18-A) | 1,25 |
| Mini cuia y repisa | 1 |
| Casa y ventanita de TiTino | 0,75 |
| Banderas (5 en 1-700) | 0,75 |
| Rosa de los vientos, placas, boya, baliza y «?» | 1 |
| Menos el «Mapa del río (13 paradas)» que ya cuenta la UI de 07 §11 | −2 |
| **Total** | **≈ 7,25** |

**Total de arte nuevo de 1-200:** unos 11 días de fondos (10.8) más unos 7,25 de Mapa ≈ **+18 días de artista** sobre lo que ya presupuesta 07 §11. En la opción barata (Q5-B), unos **+13** [E]. Con Q14-A, **+8** por los 4 kits de 201-700.

---

## 13. Plan por fases y días

Cada fase es una rama, un worktree y un PR pequeño (`docs/setup/equipo-de-agentes.md`). Ninguna toca el núcleo, `manifest.json` ni el YAML a mano. Todas pasan los comandos de verificación de CLAUDE.md.

| Fase | Quién | Contenido | Depende de | Días de agente [E] | Editor |
|---|---|---|---|---|---|
| F0 | Dueño y `tech-lead` | Respuestas a la sección 16 (y a T1, T6 y Frutas Q1). Registro de D-013 y lote de documentación (sección 15) | — | 1,5 | No |
| F1 | Agente de C# | `RiverJourney`, `MapMoments`, `RiverStopCatalog` y `river_stops_v1.json` de 1-200; el campo `progress.map_seen_level` con su valor por defecto y su fusión en la nube (11.5); tests EditMode | F0 | 3 | No |
| F2 | Agente de herramientas | `tools/river-map/`: lector de Natural Earth y HydroRIVERS, giro, simplificación por vista, tipos de agua, bocas, rutas, zonas desde la máscara del artista, validación a ≤ 2 dp contra `rio-amazonas.json`, guía para el artista y `--check` | F0 y la descarga (Q11) | 3,5 | No |
| F3 | `unity-gameplay-dev` | Fondos por parada: `StopBackdrop`, ranuras residentes (sin precarga ni descarga), reflejo por código, respaldos y héroes provisionales, fundido, rótulo con las reglas de D-011, inicio con el fondo de la parada y la ventanita de TiTino (Q17), VA-20 a VA-23 y capturas | F1 | 3,5 | Sí |
| F4 | `unity-gameplay-dev` | Victoria: franja de llegada, barra «Canasta · {sitio} k/10», «Faltan N» en el inicio, tarjeta del hito p20 y victoria con vista | F1, F3 | 2,5 | Sí |
| F5 | `unity-gameplay-dev` | Pantalla del Mapa: vidrio, lleno y frente; dos vistas y arrastre; «Norte arriba» con su escala de 0,76; canoa con Capi, Rosa y banderas; casa de TiTino; tarjeta de parada; Muestrario con las siluetas horneadas; miradores; textos accesibles en los datos; 30 fps. **Sin la capa TalkBack** (11.4) | F1 (geometría de F2 o el respaldo) | 6 | Sí |
| F6 | `unity-gameplay-dev` | Secuencias: nivel 10, cambio de mundo (en el banner de D-012 con Q4-D, o en el Mapa tras AD_GATE con Q4-A, también por la Cosecha doble), fin de contenido (700 con Q14-A; 200 con Q14-B) y pausa en superposición | F4, F5 | 2 | Sí |
| F7 | Agente de C# | Las 6 claves de RC (los dos interruptores de respaldo son filtros del mismo catálogo, sin segunda implementación: 11.5) y los eventos en logcat | F1 | 1,5 | No |
| F8 | QA | Capturas, prueba en el teléfono con `AF_STATE` y medida del AAB con `ci/size-gate.ps1` | F3-F7 | 2 | Sí (capturas) |
| | | **Total de 1-200** | | **≈ 25,5** | |
| F9 | Investigación y agente de C# (**solo con Q14-A**) | Paradas k = 20-69 (201-700): sitios y coordenadas, datos [V] o [E] con la URL reabierta (R5), vetos como lista para el asesor, rutas por agua (Napo, Madeira, Beni, Madre de Dios, Mamoré y Ucayali), banderas de Ecuador y Bolivia y los viajes de mundo del 200, el 300, el 420 y el 560 en `river_stops_v1.json` | F0, F1 | 5,5 | No |
| | | **Total con Q14-A** | | **≈ 31** | |

- **Camino crítico [cálculo]:**
  - **con un solo agente de Editor**, F3, F4, F5, F6 y F8 van en serie: F0 + F1 + F3 + F4 + F5 + F6 + F8 = 1,5 + 3 + 3,5 + 2,5 + 6 + 2 + 2 = **20,5 días hábiles**, unas 4 semanas sin ninguna holgura. **Calendario: unas 4-5 semanas**;
  - **con F5 en un segundo agente de Editor** (máximo 2, regla dura 7), en paralelo con F3 y F4 (6 días cada rama): 1,5 + 3 + 6 + 2 + 2 = **14,5 días**, **unas 3 semanas**;
  - F2, F7 y F9 van con agentes sin Editor, en paralelo, y no alargan el camino (F9 acaba hacia el día 10).
- **Orden recomendado:**
  1. F1 y F2 en paralelo;
  2. **F3 y F4 primero**, porque dan los fondos y la llegada en la victoria sin esperar al Mapa, y el dueño ve en el teléfono el cambio de sitio (y VA-20, antes de encargar los héroes);
  3. F5 y F6.
- **Versión mínima [P] (≈ 19 días):**
  - F0 (1,5), F1 (3) y F2 (3,5);
  - F3 sin rótulo (3);
  - F4 solo con la franja (1,5);
  - F5 con una sola vista, sin «Norte arriba», Muestrario ni miradores (3,5);
  - F6 (1), F7 (0,5) y F8 (1,5).

  El resto llega en la 1.0.x.
- **Fuera de este plan, y dicho:** la capa TalkBack (tarea general de accesibilidad, 08 §14.2), el catálogo por mundo de la opción (b) de 10.7 y los héroes de 201-1.000.
- **Dependencias externas:**
  - las respuestas del dueño;
  - T1 y T6 de TiTino;
  - la descarga de Natural Earth y de HydroRIVERS;
  - el asesor (datos, sitios marcados, nombres, Shanay-Timpishka y el bufeo boliviano);
  - el abogado (banderas de Guyana y uso de símbolos nacionales);
  - el artista (la máscara de zonas para F2 y los héroes después de VA-20; los respaldos dejan avanzar sin él).

---

## 14. Riesgos

| # | Riesgo | Prob. · impacto | Mitigación |
|---|---|---|---|
| R1 | La forma «real» sale mal: Natural Earth tiene alineaciones dudosas en la cuenca y solo llega a 1:30 millones [V] | Media · alto (promesa del dueño) | HydroRIVERS para la vista Mundo; validación a ≤ 2 dp de la vista (≤ 15 km en la Cuenca, ≤ 1 km en la vista Mundo) contra los 105 puntos de `rio-amazonas.json`; revisión con la guía en SVG o PNG. Alternativa: zoom de unos 4× y recuadros |
| R2 | Política: fronteras y soberanía (Esequibo, isla Santa Rosa, bandera de Guyana en el Takutu) | Media · muy alto | Sin fronteras en tierra; banderas solo en el mástil y en cruces de río; Guyana con el asesor y el abogado; ninguna isla con bandera |
| R3 | **El río de vidrio vacío se lee como sequía.** Las sequías récord de 2023-2024 son un tema sensible (07 §8.1), y en el **lago de Tefé** (parada 7) hubo en 2023 una mortandad masiva de botos [V icmbio_tefe_2023] | Media · alto | Vidrio azul hielo con brillo, nunca barro ni grietas; nunca «seco»; prueba de 5 s con el dueño; plan B: el río por recorrer en silueta azul petróleo. En el lago de Tefé, sin botos ni orillas secas y con el asesor |
| R4 | Cultura: las casas del Itaya (Belén, nunca como nombre), Puerto Nariño, São Gabriel, Shanay-Timpishka, Q'eswachaka y Parintins | Media · alto | Asesor obligatorio en los marcados; alternativas preparadas (la Casa de Fierro sin Eiffel; solo la boca del Loretoyacu; Shanay-Timpishka como vapor lejano); nada sagrado, sin personas, sin ruinas y sin samaúma |
| R5 | Datos [E] que resultan falsos: muchos se leyeron por el resumen del buscador | Media · alto | Reabrir cada URL antes de cerrar un texto; publicar solo [V] o [E] con el asesor; la fuente a la vista en la tarjeta; `--check` exige una URL para cada etiqueta y una fuente [V] para cada dato [V] |
| R6 | En 3 filas apenas se ve el fondo (34 de 60 y 48 de 60) y el dueño siente que «no cambia» | Alta · medio | Victoria con vista, rótulo, inicio y banner; enseñarlo con capturas del teléfono antes de encargar el arte |
| R7 | El AAB crece | Baja · medio | Kits en ASTC 8×8; cielo por código; héroes bajos; medir tras 3 sitios; PAD cuando se apruebe |
| R8 | Se alarga el bucle o la primera sesión | Baja · alto | Cero pantallas nuevas fuera de los hitos; el viaje de mundo dentro del banner (Q4-D); todo saltable; `ui_map_autopen` = `l10` como salida |
| R9 | TiTino no se aprueba o cambia de nombre | Media · bajo | Narra Rosa (canon); las claves no llevan nombre de personaje |
| R10 | La canoa del Mapa se confunde con las vidas | Baja · bajo | Es la misma metáfora del canon (glosario §2.2); ninguna canoa junto al HUD (VA-22) |
| R11 | El Trapecio (6 paradas en unos 100 km) se siente quieto | Media · bajo | Vista Mundo a unos 15×, boyas y llenado por nivel, dos ambientes distintos (pericos y delfines) y la bandera en el 80 |
| R12 | Las idas y vueltas entre afluentes (mundos 6-13) se sienten raras | Media · bajo | Viajes de ≤ 4 s presentados como repaso por agua ya llena; el orden no se toca (decisión del dueño) |
| R13 | Composiciones de kit que parecen «de plantilla», y 8 de 18 sitios que solo son «paisaje del tramo» | Media · medio | Hoja de las 20 composiciones para el artista; héroe pintado en 14 sitios (Q5-A); en los paisajes del tramo, rótulo, ambiente y tarjeta; decirlo antes (Q5) |
| R14 | Trade dress (Mario, Candy Crush, el Journey, los tubos) | Baja · alto | Río real en vez de camino de nodos; mini cuias en vez de tubos; madera y vidrio en vez de pergamino |
| R15 | No hay analítica para medir el efecto | Alta · medio | Los eventos de 11.5 y el A/B de `ui_map_mode` en cuanto exista `AF.Analytics` |
| R16 | Con la 1.0 del GDD (Q14-A), 201-700 no tiene fondo propio por sitio y el dueño lo esperaba | Media · medio | Decirlo en §0 y en Q14; kit del mundo y rótulo; los héroes llegan con los packs |
| R17 | La meta promete «el nacimiento» y el Amazonas no tiene una sola naciente | Baja · medio | «Una de las nacientes»; la tarjeta del 1.000 cuenta la discusión (Mantaro, Marañón) y el Mantaro sale como mirador (Q7) |

---

## 15. Encaje con el GDD: qué editaría y en qué § (sin editarlo)

Todo va en el lote de documentación tras la aprobación del dueño. Las citas van por capítulo y §, no por línea.

| Documento | Hoy | Con esta propuesta [P] |
|---|---|---|
| `docs/dev/decisiones.md` | — | **D-013**, «Mapa del río: remontar el Amazonas hasta donde nace»: la decisión del 2026-10-12 (río arriba y fondos por parada), las respuestas de la sección 16 y **los cambios que trae a D-008** (con Q4-A, el Mapa entre AD_GATE y N+1 en los cambios de mundo), **a D-009** (texto de la tarjeta previa del hito) **y a D-012** (con Q4-D, el banner trae el viaje) |
| `docs/GDD.md` | §2 #24: «Capi guía y trae cuias; Rosa salta y guía el Mapa». §2 #20-21: la 1.0 instala 1-700. §7: contratos. §6.3: preguntas abiertas | §2 #24: Capi rema, Rosa guía y TiTino cuenta (si T1). §2 #20-21: sin cambios con Q14-A; con Q14-B, 1.0 = 1-200. §7: `af-river-map/1` y `af-river-stops/1` como contratos, si se adoptan. §6.3: las preguntas que queden abiertas |
| 00 glosario | §3.1-§3.3: Capi, Rosa y el perezoso del Mapa. §5.1: «Rosa te lleva río abajo». §7.1: `river_map` con «13 paradas». §8: El origen en el «Marañón alto»; el mundo 9 con Shanay-Timpishka. §10 y §11 | §3.1: Capi rema la canoa del Mapa. §3.2 y §5.1: «río arriba», la despedida del 961 y, con Q18-A, el bufeo boliviano. §3.3: el perezoso del Mapa es TiTino. §7.1: 100 paradas (una cada 10), Muestrario del río (`river_samples` [P]), mirador y bandera de cortesía. §8: Mismi y Apurímac (Q7); mundo 9: Shanay-Timpishka según responda el asesor (parada, vapor lejano o nada). §10: las claves de 6.4. §11: ids `s{nn}` y las ranuras `bg_kit_w{nn}`, `bg_hero_<sitio>` y `map_*` |
| 02 | §5: la pausa abre el Mapa. §6: el Dato del río en la victoria y la cadena corta de D-008 (Celebración → esta pantalla → Siguiente → AD_GATE → N+1). §10: la tarjeta previa del hito (D-009, punto 2). §13: la primera tarjeta de la Mochila, en la victoria | §5: el Mapa en superposición, sin salir del nivel. §6: franja de llegada en el hueco del Dato del río, victoria con vista, barra «Canasta · {sitio} k/10» y, con Q4-A, Siguiente → AD_GATE → Mapa → N+1 en los cambios de mundo (también por la Cosecha doble). §10: la tarjeta previa añade «Si lo superas, llegas a {sitio}». §13: sin cambios |
| 03 | §13: salir al Mapa | El Mapa en superposición no es salir del nivel |
| 04 | §2.6: la boya de Rosa en el Mapa. §4: el Mercado se abre desde el Mapa del río | §2.6: sin cambios. §4: el Mercado, desde el inicio |
| 05 | §4.1 y §5: la primera tarjeta de la Mochila de inicio y el Pase Expedición, «en Mapa del río». §4.1: los packs de skin con «el fondo del mundo como marco del Mapa». §2.3: el marco de país de la Bitácora | §4.1 y §5: **la Mochila, en la victoria**, como ya fijan el contrato cerrado (GDD §7.2, incidencia 11), 06 §2.1-§2.2 y 02 §13; **el Pase**, en el Mercado o en un aviso del inicio sin TiTino a la vista. §4.1: el «fondo del mundo» de los packs pasa a ser la escena de la gran parada del mundo. §2.3: sin cambios. Sin cambios de precio ni de `econ_sim` |
| 06 | §2.2-§2.3: Mapa del 10 con Rosa. §4.4: Dato del río «presentado por Rosa». §4.5: 13 paradas. §4.6: página Ríos. §4.7: inicio con el «fondo del mundo actual» | §2.3: canoa Belém → Santarém y TiTino con el dato del estuario. §4.4: el Dato del río es el dato de llegada, en placa con el retrato de TiTino. §4.5: reescritura completa. §4.6: la página Ríos es el Muestrario. §4.7: fondo de la parada actual, «Faltan N para {sitio}» y TiTino solo en la ventanita de casa (Q17) |
| 07 | §2.1: un fondo por mundo; el mundo 9 con Shanay-Timpishka. §2.2, regla 6: los Datos del río candidatos y los vetos de los mundos 1-5 son los de §(e). §6.1: `capi_idle` y `capi_sleep` en el Mapa. §6.2: `rosa_fact` con tarjeta de papel. §6.3: el perezoso. §7.6: icono «río con 13 puntos». §7.7: la tarjeta previa del hito. §10.2-§10.4: captura 4 y UA n.º 5 «Rosa te lleva río abajo». §11: el recuento de arte | §2.1: fondos por parada (kit + héroe bajo + código) con la tabla 10.3; Loreto; El origen; mundo 9 según el asesor. **§2.2, regla 6: los datos de llegada de 1-200 pasan a `river_stops_v1.json`, cruzados con §(e)** (sus candidatos como dato o «Otro dato» y sus vetos; Q16). §6.1: `capi_paddle`. §6.2: placa con retrato, `rosa_farewell` y, con Q18-A, el bufeo boliviano. §6.3: TiTino. §7.6: «río de vidrio a medio llenar». §7.7: la línea «Si lo superas, llegas a {sitio}». §10: «Remonta el Amazonas hasta donde nace» y captura del Mapa con la forma real. §11: lista de la sección 12 |
| 08 | §2: módulos. §4.3: perfil. §6.1-§6.2: presupuesto. §8.2: eventos. §9: claves | `RiverMapScreen` en AF.UI. **§4.3: `progress.map_seen_level`** (entero, 0 por defecto; en la fusión de 08 §11 gana el máximo), sin migración. §6.1-§6.2: líneas de 10.7, con los fondos residentes. §8.2: los 3 eventos. §9: las 6 claves |
| 09 | #20 y #27; fase de arte F3; compuerta de la Fase 3 (700 niveles) | Días de las secciones 12 y 13 (F9 con Q14-A) |
| `docs/art/direccion-visual-v2.md` | §3.4: panorama único. §9: VA-01 a VA-19. §10.3: ranuras | §3.4: cielo por código + kit + héroe bajo (≤ 30 dp reconocibles sobre el horizonte, 10.6). §9: VA-20 a VA-23. §10.3: las ranuras nuevas |
| `tools/level-generator/specs/campaign_v1.json` | Hito 80: «Puerto de Manaos» [P] | «Lago de Tefé» [P]. Es solo metadato: no cambia los packs |
| `docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md` §(e) | Datos candidatos y vetos de los mundos 1-5 | Sin cambios: sus candidatos entran como dato o «Otro dato» y sus vetos se aplican (5.1 bis) |
| `docs/research/frutas-del-rio/propuesta-frutas-del-rio.md` | Serie «Agua del Tapajós»: k14 (nivel 34) cuenta la Ilha do Amor y k19 (nivel 39), la Floresta Nacional | Esos dos datos son del Mapa (niveles 20 y 30): Frutas los cambia por otros de su serie. Con su opción A, la línea de fruta lleva clave propia (sección 9) |
| `docs/research/magic-sort/propuesta-tintalu.md` | #16: Escenas del río cada 20, pendiente | Respondida por las tarjetas de parada y el Muestrario |
| `docs/research/personaje/propuesta-titino.md` | f.1: «Rosa el camino (te lleva río abajo)». d.7: TiTino vive en el inicio, en su embaúba. T6, opción A | f.1: «río arriba». d.7: en el inicio, solo en la ventanita de casa (Q17). T6-A: el dato va en placa con su retrato y su gesto, no en bocadillo (6.2) |

**No cambian:** el orden de los mundos, los packs 1-200, `WorldTable`, las semillas de nivel, los debuts de tintes, la economía (`econ_sim`), D-004, D-010 ni D-011. **Sí cambian, y se declaran arriba:** D-008 (con Q4-A), D-009 (el texto de la tarjeta previa) y D-012 (con Q4-D).

---

## 16. Preguntas al dueño (cerradas, con recomendación)

> **Decididas el 2026-10-13 (D-013):** todas con su recomendación: Q14-A, Q1-A, Q2-A, Q3-A, **Q4-D**, Q5-A, Q6-A, Q7-A, Q8-A, Q9-A, Q10-A, Q11-A, Q12-A, Q13-A, Q15-A, Q16-A, Q17-A y Q18-A, junto con T1-A y T6-A de TiTino y Q1-A de Frutas del río. Lo que «se aprueba en el lote de documentación» (al final de esta sección) quedó aprobado con ellas: las claves y los eventos están en 08 §9 y §8.2, el campo `progress.map_seen_level` en 08 §4.3 y los formatos, como contratos 29 y 30 (GDD.md §7.4; 08 §4.5). El mensaje del dueño hablaba de «las 13 preguntas» (las de la primera versión); Q14-Q18 se registraron con la misma respuesta, y D-013 lo dice.

| # | Pregunta | Opciones | Recomendación |
|---|---|---|---|
| **Q14** (contestar primero) | ¿La 1.0 son los niveles 1-700, como dice el GDD? | **A.** Sí (GDD §2 #20-21; 08 §6.2; glosario §8; 09 §1.1). El Mapa de la 1.0 lleva 70 paradas: datos [V] o [E] para k = 20-69 (F9, unos 5,5 días de agente), rutas por agua, banderas de Ecuador y Bolivia y los viajes de mundo del 200, el 300, el 420 y el 560. Los fondos de 201-700 llevan el kit del mundo y el rótulo, **sin héroe** (4 kits, unos 8 días de artista; o el «río genérico» del canon con el rótulo, sin coste): ahí **no hay fondo propio por sitio**. **B.** Cambiar el GDD a 1.0 = 1-200: decisión tuya, que obliga a editar GDD §2 #20-21, 08 §6 y 09; el 200 pasa a ser el fin de contenido | **A.** Es lo que ya está aprobado y el Mapa se adapta sin tocar el alcance. Lo que no da es un fondo propio por sitio en 201-700: si eso pesa, se pide para la 1.1 con los packs |
| **Q1** | ¿Cada cuánto se llega a un sitio nuevo con su fondo? | **A.** Una parada cada 10 niveles: 20 en 1-200, 70 en 1-700 y 100 en total. Los cierres de mundo son el «castillo» y la tarjeta del hito p20 dice «Si lo superas, llegas a {sitio}». **B.** Un sitio cada 20 (el ciclo de D-009): 11 en 1-200. **C.** 13 paradas, una por mundo (canon) | **A.** Es tu «cada 10 tableros», coincide con la Canasta y el Dato del río, y da «Faltan 3 para Manaos» siempre cerca |
| **Q2** | ¿Cómo se ve el avance en el Mapa? | **A.** El río real en vidrio que se llena con su agua de verdad (blanca, negra, clara), y los afluentes al pasar su boca. **B.** Una ruta pintada que brilla, con boyas | **A.** Es lo que lo hace único en un juego de verter agua: el Journey de Magic Sort con el Amazonas de verdad |
| **Q3** | ¿Quién hace qué en el Mapa? | **A.** Capi rema la canoa, Rosa nada delante y TiTino cuenta desde su casa del estuario, con el dato en una placa con su retrato (atado a T1 y a T6). **B.** Rosa guía y cuenta (canon) y Capi rema. **C.** TiTino viaja en la canoa | **A.** Es lo que pediste, separa narrador y avatar, y aprovecha lo que ya hacen Capi y Rosa. C choca con las reglas éticas de TiTino (no viaja; g.2). Si T1 no sale, B sin rehacer nada |
| **Q4** | ¿Cuándo sale solo el Mapa y cómo se cuenta el cambio de mundo? | **A.** En el 10, en cada cambio de mundo y al acabarse el contenido; el cambio de mundo, en una pantalla aparte **después de AD_GATE** (Siguiente → AD_GATE → Mapa ≤ 6 s → N+1, también por la Cosecha doble). **B.** Solo en el 10 y al acabarse el contenido; el cambio de mundo, en la franja de la victoria. **C.** En cada parada. **D.** Como A, pero el viaje de cada cambio de mundo va **dentro del banner de D-012** del 41, el 81 y el 141 (≤ 4 s, saltable, con «Ver mapa»), sin pantalla nueva | **D.** Da el momento «Mario» de llegar a Colombia o al Perú sin apilar Mapa, banner y Fruta nueva (06 §4.5 lo evita en el 11) y sin alargar la cadena corta de D-008; cambia D-012 y se declara (sección 15). A añade una pantalla entre AD_GATE y el nivel; C añadiría 20 pantallas |
| **Q5** | ¿Cómo se hacen los fondos por parada? | **A.** Kit del mundo + héroe en los 18 sitios: 14 pintados (y uno de reserva), 2 por código y 2 solo con el kit. **Ojo:** 8 sitios se reconocen por un hito propio, 2 por la línea entre aguas y **8 son «paisaje del tramo»**, que se reconocen por el rótulo, el ambiente y la tarjeta, no por un hito (10.3). Unos +11 días de artista en fondos. **B.** Héroe pintado solo en las 7 ciudades y formas únicas (y la reserva); los 7 sitios de naturaleza, con el kit; unos +5,5. **C.** Uno por mundo (canon) | **A.** Es lo más cerca de «una por cada ciudad o sitio» que permite la franja del tablero. B es la salida barata. En los dos casos, VA-20 con capturas del teléfono antes de encargar |
| **Q6** | En los tableros de 3 filas el panel tapa el horizonte (34 de 60 en el Trapecio y 48 de 60 en Loreto). ¿Qué hacemos? | **A.** Victoria con vista (el panel se desvanece tras ganar y se ve el sitio; en las llegadas aparece el nuevo) y el rótulo del lugar. **B.** Aceptarlo sin más. **C.** Probar un «horizonte bajo» detrás del HUD | **A.** Es la única forma de ver el sitio en esos niveles sin tocar el panel (D-004) ni añadir tiempo |
| **Q7** | ¿Dónde termina el viaje y cómo se cierra? | **A.** En el nevado Mismi (quebrada Carhuasanta, Apurímac), **una de las nacientes**: la cabecera más lejana con agua todo el año, la que se acepta desde 1971; el Mantaro es la más lejana según Contos y Tripcevich (2014), pero se corta varios meses al año [E wp_source, natgeo2014]. Cierre: el río despierta (una gota del nacimiento al mar en unos 8 s, saltable); la tarjeta del 1.000 cuenta la discusión y el Mantaro sale como mirador. **B.** En el Marañón alto (canon del glosario §8), con el mismo final. **C.** En el Mismi, con corte directo | **A.** El Amazonas no tiene una sola naciente, así que ningún final puede decir «aquí nace»; el Mismi da un sitio concreto y la cabecera que nunca se seca, y el vuelo final cumple tu idea original (del nacimiento a la desembocadura). En la 1.0 no cambia nada |
| **Q8** | ¿Cómo se marcan los países? | **A.** Sin líneas en tierra: el nombre del país en su región y la bandera de cortesía izada en la canoa al entrar (y al volver). **B.** Las fronteras de Natural Earth. **C.** Ninguna bandera | **A.** Es una costumbre náutica real y evita el Esequibo y la isla Santa Rosa |
| **Q9** | ¿Cuál es el coleccionable? | **A.** El Muestrario del río: una mini cuia por parada con el agua del sitio y su tarjeta, que enseña el fondo en pequeño. **B.** Solo una galería de escenas. **C.** Ninguno | **A.** Es del mismo verbo del juego, no toca la economía y responde a la #16 de Escenas del río |
| **Q10** | ¿Ofertas en el Mapa? | **A.** Ninguna. La primera tarjeta de la **Mochila de inicio sigue en la victoria** (contrato cerrado: GDD §7.2, incidencia 11) y se corrige 05 §4.1 y §5; el **Pase Expedición** (1.2) va al Mercado o a un aviso del inicio sin TiTino a la vista; al Mercado se entra desde el inicio; la skin sale como «Premio del mundo · faltan N», sin precio. **B.** Las del canon de 05 (tarjetas de la Mochila y del Pase en el Mapa) | **A.** El contrato cerrado ya pone la Mochila en la victoria, con D-008 casi nadie pasa por el inicio ni por el Mapa, y Rosa y TiTino no salen en pantallas de pago (07 §6.2; g.2) |
| **Q11** | ¿De dónde sale la geometría? | **A.** Natural Earth 1:10m (dominio público) para la vista Cuenca y HydroRIVERS (con cita) para la vista Mundo: las descargas tú una vez en `tools/river-map/source/`, con su hash, y se validan a ≤ 2 dp contra `rio-amazonas.json`. **B.** Solo Natural Earth, con el zoom limitado a unos 4× y recuadros para el Trapecio y Loreto. **C.** Solo `rio-amazonas.json` (la forma sin meandros). **D.** El artista lo dibuja a mano | **A.** Precisión a las dos escalas, reproducible y con licencia limpia. B si no quieres una segunda fuente. Mientras llega, el desarrollo usa C como respaldo |
| **Q12** | ¿«¿Verdad o mito?» (TiTino pregunta y el jugador responde con un toque)? | **A.** Experimento de la 1.1 detrás de `ui_map_quiz` = `off`; en la 1.0, el dato simple. **B.** Desde la 1.0. **C.** Nunca | **A.** Es entretenido, pero añade interacción justo en la victoria (D-011) y exige unas 100 parejas con fuente. Se mide antes de encenderlo |
| **Q13** | Con la campaña río arriba, ¿corregimos los textos «Rosa te lleva río abajo» (glosario §3.2 y §5.1, 07 §10.4, propuesta de TiTino f.1)? | **A.** Sí: «Rosa te lleva río arriba, al siguiente nivel». **B.** No | **A.** «Río abajo» contradice el viaje que decidiste |
| **Q15** | ¿Con qué orientación se abre el Mapa? | **A.** Girada 90°, sin deformar (Andes arriba, Atlántico abajo; canon 06 §4.5): subir por la pantalla es remontar, y la cuenca cabe a lo alto del teléfono (431 dp). La forma con el norte arriba sale con el botón «Norte arriba» de la vista Cuenca, reducida a ≈ 0,76. **B.** Norte arriba por defecto: la forma que todos conocen, pero la cuenca mide 431 dp de ancho y se reduce a ≈ 0,76 siempre, y remontar va de derecha a izquierda | **A.** Es el formato vertical y el canon. «Al real» se cumple porque la forma no se deforma, solo gira; la forma «de mapa» se ve con el botón |
| **Q16** | ¿De dónde salen los datos de llegada de 1-200? | **A.** De esta propuesta, cruzada con §(e) de la propuesta de D-007: sus candidatos van como dato o como «Otro dato» de la tarjeta y sus vetos se aplican; 07 §2.2, regla 6, pasa a decir que los datos de llegada están en `river_stops_v1.json` (5.1 bis). **B.** Solo los candidatos de §(e), como fija hoy el canon: los sitios que §(e) no cubre (Alter do Chão, Amacayacu, Allpahuayo, Nauta, Pacaya-Samiria…) se quedan sin dato propio hasta que el asesor apruebe otro | **A.** §(e) no tiene un dato por parada; con A cada llegada tiene el suyo y §(e) sigue mandando en los vetos. Todo pasa por el asesor |
| **Q17** | TiTino en el inicio (atada a T1): el inicio enseña el fondo de la parada actual y TiTino no viaja | **A.** TiTino solo sale en la **ventanita de casa** (su embaúba del estuario, enmarcada) sobre el fondo de la parada. **B.** El inicio se queda en el estuario, con TiTino en su árbol y una placa «Estás en {sitio}» | **A.** Es la misma pieza del Mapa y cuenta lo mismo: él se queda en casa y tú estás lejos. B es más barata, pero el inicio deja de enseñar el sitio en los tableros de 3 filas (6.1) |
| **Q18** | Rosa y los rápidos del Madeira (con el asesor): el viaje del 300 sube el Madeira hasta Bolivia, y sus rápidos separan la población boliviana del delfín de río [E wp_boto] | **A.** Rosa se queda en los rápidos de Teotônio; en los mundos 7 y 8 guía un **bufeo boliviano** (el mismo rig con otra paleta, sin nombre propio), con un dato real; Rosa vuelve en el viaje del 560. **B.** Rosa sigue en los mundos 7 y 8, y la despedida del 961 se justifica solo por el agua muy baja de los Andes | **A.** El viaje no contradice lo que TiTino cuenta y gana un dato. B es más barata, pero cruza unos rápidos que el boto no cruza (6.1) |

**Lo que se aprueba en el lote de documentación** (no son preguntas, pero hay que firmarlo):
- el registro de D-013, con los cambios que trae a D-008, D-009 y D-012;
- las 6 claves de Remote Config y los 3 eventos;
- el campo `progress.map_seen_level` del perfil (08 §4.3), sin migración;
- los formatos `af-river-map/1` y `af-river-stops/1`;
- las descargas de Natural Earth y de HydroRIVERS (Q11);
- los textos nuevos del glosario §10;
- y, cuando toque PAD, el paquete Addressables (08 §6.2).

Conviene contestar a la vez **T1 y T6** de TiTino (con Q3 y Q17) y la **Q1 de Frutas del río**.

---

## 17. Fuentes

**Del repositorio** (rama `paso2/registrar-d007-d009`, salvo que se diga otra cosa; citas por capítulo y §):

- **GDD:**
  - `docs/GDD.md`: §2 #20-21 y #24, §6.3 y §7.2 (incidencia 11);
  - `docs/gdd/00-glosario.md`: §2.2, §3, §5.1, §7.1, §8, §10 y §11;
  - `docs/gdd/02-nucleo-reglas-ux.md`: §5, §6 (cadena corta de D-008), §10 y §13;
  - `docs/gdd/04-comodines-vidas-rescate.md`: §2.6 y §4;
  - `docs/gdd/05-economia-monetizacion.md`: §2.3, §4.1 y §5;
  - `docs/gdd/06-meta-retencion-liveops.md`: §2.1-§2.3, §4.4-§4.7 y §9;
  - `docs/gdd/07-arte-tematica-audio.md`: §2.1-§2.2 (regla 6), §5.4, §6.1-§6.3, §7.6-§7.7, §8, §10 y §11;
  - `docs/gdd/08-arquitectura-tecnica-publicacion.md`: §4.3, §5.4, §6.1-§6.2, §8.2, §9, §11 y §14.2;
  - `docs/gdd/09-roadmap-equipo-kpis-riesgos.md`: §1.1 y la compuerta de la Fase 3.
- **Decisiones:** `docs/dev/decisiones.md` (D-003, D-004, D-008, D-009, D-010, D-011 y D-012).
- **Guía visual:** `docs/art/direccion-visual-v2.md` (§2.2-§2.3, §3.1-§3.9, §4.9, §7, §8, §9 y §10.3).
- **Traspaso:** `docs/dev/traspaso/README.md` §8.
- **Propuestas:**
  - `docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md` §(e) (candidatos y vetos de los mundos 1-5) e `investigacion.md` (los CR-F);
  - `docs/research/personaje/propuesta-titino.md` (a, c.1, c.3, c.4, d.7, e.2, f.1-f.2, g.2, j y T6);
  - `docs/research/frutas-del-rio/propuesta-frutas-del-rio.md` (§5.3 y la serie del Agua del Tapajós);
  - `docs/research/magic-sort/ficha-magic-sort.md` §7.8 y `propuesta-tintalu.md` #16.
- **Especificación:** `tools/level-generator/specs/campaign_v1.json` (`seasons` y nombres de hito).
- **Código y datos:**
  - `Assets/_Project/Scripts/Levels/WorldTable.cs` (`FirstLevels`);
  - `Assets/_Project/Scripts/UI/GameScreen.cs` (`HorizonFor`, `HorizonAbovePanelDp` = 64, `MaxHorizonDp` = 200, y `LayoutBackdrop`);
  - `Assets/_Project/Scripts/Save/Profile.cs` (`CurrentSchema` = 1 y la regla de los campos nuevos sin migración);
  - `Assets/_Project/Scripts/Presentation/ArtSlots.cs` (catálogo estático), `ArtSlotCatalog.cs` (referencias directas a cada Sprite), `WaterStripGraphic.cs` y `BezierStripGraphic.cs`;
  - `Packages/manifest.json` (sin Addressables; SpriteShape 13.0.0);
  - los packs `Assets/_Project/Data/Levels/*.json` (recuento de 3 filas: script propio, con 11 cuias o más).
- **Trabajo previo del 2026-10-12:** investigación de mercado, geografía (105 puntos, de Wikidata), datos para TiTino (F01-F99, que aquí se citan como MR-F01 … MR-F99 para no chocar con los F de `docs/research/cultura-ribera/investigacion.md`; las que esta propuesta usa están en `rio-amazonas.json`, `fuentes`, con su URL y su etiqueta), inventario del repositorio y las tres variantes A, B y C.

**Externas.** Las del 2026-10-12 se leyeron en el recuadro o en el resumen de la página; las marcadas «releída» se reabrieron en la revisión del 2026-10-12 o del 2026-10-13, y las de «releída el 2026-10-14», para el «Otro dato» (sección 5.1 ter; en `rio-amazonas.json`, campo `releida`); las de «leída» o «releída el 2026-10-15», para las paradas del mundo 6 (sección 5.4 bis). Antes de cerrar un texto, hay que reabrirlas todas (R5). La lista completa, con un ID por fuente, está en `rio-amazonas.json`, `fuentes`.

| Tema | Etiqueta | Fuente |
|---|---|---|
| Nacientes del Amazonas: no hay una sola; la Carhuasanta del Mismi es la más lejana con agua todo el año; el Mantaro, la más lejana, se corta varios meses al año (Contos y Tripcevich, 2014); el Apurímac, la aceptada desde 1971; la Laguna Bohemia, a 5.148 m (releídas; otra vez el 2026-10-15, para el dato del 200) | [E] | <https://en.wikipedia.org/wiki/Source_of_the_Amazon_River> · <https://www.nationalgeographic.com/adventure/article/140213-amazon-river-length-source-maps-science> |
| Longitud (6.575 km desde el Mismi) y km del cauce | [E] | <https://en.wikipedia.org/wiki/Amazon_River> |
| Coordenadas de lugares y bocas | [E/V] | Wikidata (CC0): <https://query.wikidata.org/> |
| Tipos de agua (Sioli): el agua clara, por rocas cristalinas antiguas y suelos arenosos con poco sedimento, y de 1,5 a 4 m de visibilidad; los ríos de agua blanca (releída) | [E] Wikipedia · [V] el artículo | <https://en.wikipedia.org/wiki/Clearwater_river_(river_type)> · <https://en.wikipedia.org/wiki/Whitewater_river_(river_type)> · <https://en.wikipedia.org/wiki/Blackwater_river> · Ríos-Villamizar y otros (2014): <https://www.witpress.com/elibrary/wit-transactions-on-ecology-and-the-environment/178/25051> |
| El Tapajós hoy, turbio en largos tramos (para la nota del asesor; no va en ningún texto) | [E] | <https://www.brasildefato.com.br/2022/01/29/lama-do-garimpo-no-caribe-amazonico-pode-matar-rio-que-sustenta-indigenas-e-ribeirinhos> · <https://brasil.mapbiomas.org/wp-content/uploads/sites/4/2023/08/Nota_Tecnica_-_Sedimentos_Rio_Tapajos-5.pdf> |
| Natural Earth: dominio público, alineaciones dudosas en la cuenca y escala de hasta 1:30 millones | [V] | <https://www.naturalearthdata.com/about/terms-of-use/> · <https://www.naturalearthdata.com/downloads/10m-physical-vectors/10m-rivers-lake-centerlines/> |
| HydroRIVERS (HydroSHEDS), para la vista Mundo; uso con cita, la licencia se revisa con la descarga | [E] | <https://www.hydrosheds.org/products/hydrorivers> |
| OSM (ODbL) | [V] | <https://www.openstreetmap.org/copyright> |
| Ramal del Pará y furos de Breves | [E] | <https://en.wikipedia.org/wiki/Par%C3%A1_River> |
| Tocantins: desemboca en el estuario del Pará y solo a veces se incluye en la cuenca (releída) | [E] | <https://en.wikipedia.org/wiki/Tocantins_River> |
| Belém-Santarém en línea recta (unos 700 km) | [E] | <https://www.airmilescalculator.com/distance/bel-to-stm/> |
| Floresta Nacional do Tapajós: −3,517, −55,073; decreto 73.684 de 1974; 549.066 ha (releída el 2026-10-14) | [E] | <https://en.wikipedia.org/wiki/Tapaj%C3%B3s_National_Forest> |
| Amacayacu: −3,483, −70,200; creado en 1975 | [E] | <https://en.wikipedia.org/wiki/Amacayacu_National_Park> · <https://www.parquesnacionales.gov.co/sala-prensa/noticias/colombia-suma-hoy-23-parques-naturales-abiertos-al-ecoturismo/> |
| Lagos de Tarapoto: 03°46'35"S 70°32'23"W; sitio Ramsar 2336 (2017); «guardería» de los delfines y lugar clave para su crianza; primer humedal Ramsar de la Amazonia colombiana, con la consulta previa de las comunidades del resguardo Ticoya (MinAmbiente y SINCHI, releídas el 2026-10-14; la ficha Ramsar no abrió ese día) | [V] | <https://rsis.ramsar.org/ris/2336> · <https://www.minambiente.gov.co/lagos-de-tarapoto-nuevo-humedal-ramsar-en-colombia> · <https://en.sinchi.org.co/tarapoto-lakes-declared-ramsar-site-and-first-for-colombian-amazon> |
| Allpahuayo-Mishana: −3,928, −73,556; 23 km al suroeste de Iquitos; 2004 | [E] | <https://en.wikipedia.org/wiki/Allpahuayo-Mishana_National_Reserve> |
| Aves de la arena blanca de Allpahuayo-Mishana: el hormiguero de Allpahuayo (*Percnostola arenarum*), solo en Loreto (cuencas del Nanay y del Itaya y el Morona); la perlita de Iquitos (*Polioptila clementsi*), otra especie, conocida solo de la reserva, que BirdLife no separa (releídas el 2026-10-13) | [E] | <https://en.wikipedia.org/wiki/Allpahuayo_antbird> · <https://en.wikipedia.org/wiki/Iquitos_gnatcatcher> · <https://www.actualidadambiental.pe/loreto-reserva-nacional-allpahuayo-mishana-celebra-16-anos-conservando-biodiversidad/> |
| Belén (distrito de Maynas): −3,7643, −73,2505; el río sube 5-6 m; casas sobre balsas o pilotes en la llanura del Itaya (releídas el 2026-10-14) | [E] | <https://en.wikipedia.org/wiki/Bel%C3%A9n_District,_Maynas> · <https://habitatio.epitesz.bme.hu/en/?p=4822> |
| Río Tefé, de agua negra, que forma el lago antes del Solimões (releída) | [E] | <https://en.wikipedia.org/wiki/Tef%C3%A9_River> |
| Mamirauá: várzea de agua blanca entre el Solimões, el Japurá y el Auati-Paraná (−2,26, −65,68); várzea frente a igapó (releída) | [E] | <https://en.wikipedia.org/wiki/Mamirau%C3%A1_Sustainable_Development_Reserve> · <https://en.wikipedia.org/wiki/V%C3%A1rzea_forest> |
| Mortandad de botos del lago de Tefé (2023), tema sensible | [V] | <https://static.poder360.com.br/2023/10/boletim-tecnico-botos-lago-tefe-icmbio.pdf> (ICMBio) · <https://www.ihu.unisinos.br/categorias/646571-o-futuro-incerto-dos-botos-da-amazonia-no-meio-de-uma-seca-historica> [E] |
| Purus, Juruá, Japurá, Xingu, Trombetas y Orinoco: cabeceras y bocas | [E] | <https://en.wikipedia.org/wiki/Purus_River> · <https://en.wikipedia.org/wiki/Juru%C3%A1_River> · <https://en.wikipedia.org/wiki/Japur%C3%A1_River> · <https://en.wikipedia.org/wiki/Xingu_River> · <https://en.wikipedia.org/wiki/Trombetas_River> · <https://en.wikipedia.org/wiki/Orinoco> |
| Pueblos de esos ríos | [E] | Recuadros de Wikipedia: Boca do Acre, Lábrea, Tapauá, Cruzeiro do Sul, Eirunepé, Carauari, La Pedrera, Maraã, São Félix do Xingu, Altamira, Oriximiná y San Fernando de Atabapo. URL en `rio-amazonas.json`, `fuentes` |
| Costa para la muestra (Cayena, boca del Oiapoque, Salinópolis; ciudades del Pacífico de Wikidata) | [E] | <https://en.wikipedia.org/wiki/Cayenne> · <https://en.wikipedia.org/wiki/Oiapoque_River> · <https://en.wikipedia.org/wiki/Salin%C3%B3polis> · Wikidata. Entre ciudades, [NV] |
| Arrecife de la desembocadura (2016) | [V] | <https://agencia.fapesp.br/scientists-discover-reef-concealed-by-amazon-rivers-plume/23216> |
| Encuentro de las Aguas (6 km, 28 °C frente a 22 °C) | [E] | <https://en.wikipedia.org/wiki/Meeting_of_Waters> |
| Cuias del Bajo Amazonas, patrimonio de Brasil (IPHAN, 2015, Libro de los Saberes; ASARISAN) (releídas el 2026-10-14) | [V] | <https://www.oliberal.com/belem/da-arvore-a-tradicao-cuias-carregam-seculos-de-historia-e-resistem-no-cotidiano-da-amazonia-1.1009052> · <https://agencia.fapesp.br/cuias-feitas-por-mulheres-indigenas-abasteciam-o-mercado-de-luxo-europeu-no-seculo-xviii/53069> |
| Ver-o-Peso, protegido por el IPHAN en 1977 (releída el 2026-10-14) | [V] | <https://www.gov.br/iphan/pt-br/superintendencias/para/patrimonio-material> · <https://en.wikipedia.org/wiki/Ver-o-Peso_Complex> [E] |
| El chibé: farinha con agua, sola o con pescado (releída el 2026-10-14) | [E] | <https://portalamazonia.com/amazonia-de-a-a-z/c/chibe/> |
| Encuentro de las Aguas: voto unánime del Consejo Consultivo del IPHAN (4-11-2010), sin homologar y en los tribunales en 2017; oficializado según la prensa en diciembre de 2017 (releídas el 2026-10-14; Confea no abrió ese día) | [V] · [E] la prensa | <https://periodicos.ufam.edu.br/index.php/revistaelaborar/issue/view/276/Alvatir%20Carolino%20da%20Silva> · <https://portalamazonia.com/meio-ambiente/o-encontro-das-aguas-e-nosso-diz-roberio-braga-apos-tombamento-do-cartao-postal/> · <https://www.confea.org.br/node/109909> |
| Manaos: el nivel del Negro se mide a diario en el puerto desde 1902 (releída el 2026-10-14) | [V] | <https://www.scielo.br/j/aa/a/KH8cBzrnZSQTSppFSB55Bjg/?lang=pt> |
| Flutuantes de Manaos (releída el 2026-10-14) | [E] | <https://portalamazonia.com/cidades/flutuantes-casas-sobre-os-rios-da-amazonia/> |
| Anavilhanas: unas 400 islas, permanentes y temporales según el nivel del agua (releída el 2026-10-14) | [V] | <https://www2.camara.leg.br/atividade-legislativa/comissoes/comissoes-permanentes/cmads/arquivos/2008-03-23-comissao-aprova-parque-nacional-de> |
| Pirarucú: los pescadores lo cuentan cuando sube a respirar y el conteo da la cuota anual en Mamirauá y Amanã (releídas el 2026-10-14) | [V] · [E] la prensa | <https://mamiraua.org.br/wp-content/uploads/2025/11/Recount-Pirarucu-a-method-for-assessing-the-quality-of-pirarucu-countings.pdf> · <https://portalamazonia.com/meio-ambiente/saber-antigo-de-contar-pirarucu-e-aliado-ao-manejo-sustentavel-da-especie-entenda/> |
| Manatíes de Mamirauá: migran al lago Amanã en la vaciante (releídas el 2026-10-14) | [V] | <https://doi.org/10.1590/1809-4392201600862> · <https://revistapesquisa.fapesp.br/en/amazonian-manatee-knows-just-when-to-migrate/> |
| Taricayas de Pacaya-Samiria: grupos de manejo de las comunidades (MINAM, 2024) y manejo participativo desde 1991 (Soini, IIAP, 1996) (releídas el 2026-10-14) | [V] | <https://sinia.minam.gob.pe/novedades/comunidades-nativas-desarrollan-actividades-sostenibles-que> · <https://revistas.iiap.gob.pe/index.php/foliaamazonica/article/download/310/271> |
| Ilha do Amor con el río bajo | [E] | <https://www.correiobraziliense.com.br/cbradar/o-caribe-amazonico-que-viralizou-gracas-as-aguas-transparentes-e-clima-paradisiaco/> |
| Pirarucú contado en Mamirauá (noticia institucional; no abrió el 2026-10-14 y ya no se usa) | [E] | <https://www.iadb.org/en/news/man-and-nature-flooded-forest> |
| La Amazonia, cerca del 42 % de Colombia | [V] | <https://en.sinchi.org.co/region-amazonica-colombiana> |
| Pericos del parque Santander (portal turístico y prensa) | [E] | <https://colombia.travel/es/amazonas/avistamiento-de-aves-en-el-amazonas> · <https://www.semana.com/nacion/articulo/los-loros-que-viven-en-el-parque-de-leticia/540875> |
| Puerto Nariño sin carros | [E] | <https://colombia.travel/es/puerto-narino> |
| Trapecio: el Amazonas como frontera Colombia-Perú | [E] | <https://sogeocol.edu.co/documentos/004_01_el_trape_amazo.pdf> |
| Isla Santa Rosa en disputa (2025) | [V prensa] | <https://www.elespectador.com/mundo/america/peru-responde-a-colombia-por-disputa-territorial-sobre-la-isla-santa-rosa/> |
| Esequibo: caso en la Corte Internacional de Justicia | [E] | <https://www.aljazeera.com/news/2023/12/15/venezuela-and-guyana-agree-not-to-use-force-in-essequibo-dispute> |
| El Perú, segundo país de América Latina con más bosque tropical, después de Brasil (releída) | [V] | <https://www.minam.gob.pe/bosques/los-bosques-en-cifras/> |
| Iquitos, la mayor ciudad sin conexión vial | [V] | <https://www.guinnessworldrecords.com/world-records/101153-largest-city-with-no-major-road-connection> |
| Nauta: el Marañón y el Ucayali forman el Amazonas | [V] | <https://agu.confex.com/agu/fm14/webprogram/Paper31820.html> · <https://en.wikipedia.org/wiki/Nauta> [E] |
| Pacaya-Samiria: unos 2 millones de ha y el mayor humedal de la Amazonia; «selva de los espejos» por las aguas negras del Samiria (releída) | [E] | <https://www.actualidadambiental.pe/fotos-pacaya-samiria-la-selva-de-los-espejos-cumple-medio-siglo-de-conservacion/> · <https://www.actualidadambiental.pe/?p=61457> · <https://es.mongabay.com/2020/10/pacaya-samiria-amazonia-peru-fotos/> |
| Ciclo del agua amazónico: la lluvia entra del océano y se recicla en la selva (releída) | [E] | <https://web.mit.edu/12.000/www/m2006/teams/r6/final2/watercycle.htm> |
| Botos: límites del área (cascadas y agua muy baja); los rápidos del Madeira aíslan la población boliviana, río arriba de Teotônio; la ley boliviana de 2012 que lo declara tesoro nacional (releída el 2026-10-15) | [E] | <https://en.wikipedia.org/wiki/Amazon_river_dolphin> · <https://seamap.env.duke.edu/species/180407/html> |
| Madeira: frontera Bolivia-Brasil hasta la boca del Abunã; los saltos de Teotônio, bajo el embalse de Santo Antônio desde 2012 (releída el 2026-10-15) | [E] | <https://en.wikipedia.org/wiki/Madeira_River> |
| Puerto Heath, en el departamento de La Paz (Bolivia) | [E] | <https://en.wikipedia.org/wiki/Puerto_Heath_Airport> |
| Wiphala entre los símbolos del Estado de Bolivia (Constitución de 2009, art. 6.II; DS 0241/2009) | [E] | <https://www.lexivox.org/norms/BO-DS-N241.html> |
| Yasuní: más de 655 especies de árbol en una hectárea típica; al menos 596 aves; 150 anfibios, récord mundial entre paisajes parecidos; parque desde 1979 y reserva de la biosfera desde 1989 (releída el 2026-10-15) | [V] | <https://journals.plos.org/plosone/article?id=10.1371/journal.pone.0008767> |
| Coordenadas de las paradas y los ríos del mundo 6: Mazán (Q6799128), Santa Clotilde (Q107201179), Nuevo Rocafuerte (Q647791), Yasuní (Q1641326), la reserva Cuyabeno (Q917251), la Laguna Grande (Q131248042), El Coca (Q1014564), Puerto Misahuallí (Q48796581) y los ríos Aguarico (Q397928), Cuyabeno (Q5197070) y Coca (Q1104914) (consulta del 2026-10-15) | [E] | Wikidata (CC0): <https://www.wikidata.org/wiki/Q1641326> y los demás elementos por su Q |
| Napo: nace en las laderas del Antisana, el Sincholagua y el Cotopaxi; caudal medio en Mazán (6.800 m³/s), Santa Clotilde (5.895) y Nuevo Rocafuerte (2.032) (releída el 2026-10-15) | [E] | <https://en.wikipedia.org/wiki/Napo_River> (CR-F77) |
| Canoas-taxi del Napo en El Coca, desde las 05:00 (Expreso, 2016; releída el 2026-10-15) | [E] | <https://www.expreso.ec/actualidad/canoas-son-lsquo-taxis-rsquo-comunas-coca-53085.html> (CR-F78) |
| En Ecuador, la Amazonia también se llama «Oriente»; los Andes en su límite occidental (leída el 2026-10-15) | [E] | <https://es.wikipedia.org/wiki/Regi%C3%B3n_Amaz%C3%B3nica_del_Ecuador> |
| Cuyabeno: el mayor sistema lacustre tropical de Ecuador, 14 lagunas unidas por canales (Enfoque UTE, 2016; leída el 2026-10-15) | [V] | <https://www.redalyc.org/journal/5722/572261592005/html/> |
| Cuyabeno: el Cuyabeno y el Lagartococha, afluentes de agua negra del Aguarico, de agua blanca; las lagunas con igapó, zona de cría de los delfines (Denkinger, 2010; leída el 2026-10-15) | [V] | <https://revistas.usfq.edu.ec/index.php/avances/article/download/51/53/54> |
| Cuyabeno-Lagartococha-Yasuní, el humedal Ramsar más grande de Ecuador (UICN), declarado el 24-8-2017 (prensa); la ficha Ramsar 2332 no abrió el 2026-10-15 | [V] la UICN · [E] la prensa | <https://iucn.org/es/node/43308> · <https://www.eloriente.com/articulo/complejo-de-humedales-cuyabeno-lagartococha-yasuniacute-nuevo-sitio-ramsar/8113> · <https://rsis.ramsar.org/ris/2332> |
| El Coca (Puerto Francisco de Orellana), en la confluencia del Coca y el Napo (leídas el 2026-10-15) | [E] | <https://en.wikipedia.org/wiki/Puerto_Francisco_de_Orellana> · <https://en.wikipedia.org/wiki/Coca_River> |
| Coordenadas de las paradas, los ríos y el punto de paso del mundo 7: Puerto Heath (Q6092075), el lago Sandoval (Q1800405), Puerto Maldonado (Q541435), el Tambopata Research Center (Q115641210), Boca Manu (Q27051064), Cocha Cashu (Q5139274), el parque del Manu (Q390264), los ríos Tambopata (Q6978442), Manu (Q2749258) y Heath (Q960348) y la Cachoeira Teotônio (Q22051335) (consulta del 2026-10-15) | [E] | Wikidata (CC0): <https://www.wikidata.org/wiki/Q1800405> y los demás elementos por su Q |
| El Heath, frontera natural entre el Perú y Bolivia; Puerto Heath, en su boca (leídas el 2026-10-15) | [E] | <https://en.wikipedia.org/wiki/Heath_River> · <https://es.wikipedia.org/wiki/Puerto_Heath> |
| Lago Sandoval: una cocha (lago de meandro) de unas 125 ha, a unos 16 km de Puerto Maldonado; orillas de aguajes con guacamayos (leídas el 2026-10-15) | [E] | <https://en.wikipedia.org/wiki/Lake_Sandoval> · <https://en.wikipedia.org/wiki/Tambopata_National_Reserve> |
| Reserva Nacional Tambopata: DS 048-2000-AG, 274.690 ha (El Peruano, 2025; leída el 2026-10-15) | [V] | <https://busquedas.elperuano.pe/dispositivo/NL/2447821-1> |
| Tambopata: llega al Madre de Dios en Puerto Maldonado; la Macaw Society y la collpa Colorado; los guacamayos azul y amarillo anidan en aguajes muertos (leídas el 2026-10-15) | [E] | <https://en.wikipedia.org/wiki/Tambopata_River> · <https://en.wikipedia.org/wiki/Tambopata_Macaw_Project> |
| Puerto Maldonado: confluencia del Tambopata y el Madre de Dios; el turismo y la construcción de botes (CR-F96, releída el 2026-10-15) | [E] | <https://en.wikipedia.org/wiki/Puerto_Maldonado> |
| Castaña: el añuje entierra semillas que germinan; el coco tarda 14 meses (CR-F93); la zafra de enero a abril en Tambopata (CR-F94) (releídas el 2026-10-15) | [E] | <https://en.wikipedia.org/wiki/Brazil_nut> · <https://www.actualidadambiental.pe/inician-zafra-castanera-2011-en-la-reserva-nacional-tambopata-y-parque-nacional-bahuaja-sonene/> |
| Madre de Dios: el Alto Madre de Dios y la boca del Manu en Boca Manu; el Manu, 356 km en la región de Madre de Dios (leídas el 2026-10-15) | [E] | <https://es.wikipedia.org/wiki/R%C3%ADo_Madre_de_Dios> · <https://es.wikipedia.org/wiki/R%C3%ADo_Manu_(Per%C3%BA)> |
| Parque Nacional del Manu: 1973, reserva de biosfera en 1977 y Patrimonio Natural en 1987; más de 1.000 aves; más de 40 hormigas en un árbol; las cochas con el lobo de río (SERNANP; leída el 2026-10-15) | [V] | <https://www.gob.pe/institucion/sernanp/informes-publicaciones/1948163-parque-nacional-del-manu> |
| Datos de Pará de la tinaja (pregunta 50 (a)): las cuias pitingas del Bajo Amazonas (Aceno, 2020) y el panelão de Quatipuru-Mirim (Revista Cocar, 2023), [V]; el botijo (Wikipedia) y la Olaria do Espanhol de Icoaraci, de 1903 (Diário do Pará, 2024), [E] (releídas el 2026-10-15) | [V] / [E] | <https://periodicoscientificos.ufmt.br/ojs/index.php/aceno/article/view/8582/7555> · <https://periodicos.uepa.br/index.php/cocar/article/download/6824/3251/30977> · <https://en.wikipedia.org/wiki/Botijo> · <https://diariodopara.com.br/encartes/na-estrada-com-o-diario/ceramicas-de-icoaraci-dao-charme-a-vila-sorriso/> |
| Collpas: la hipótesis del sodio, la más apoyada; la collpa Colorado, sobre el Tambopata, con loros de hasta 18 especies cada día; 40 veces más sodio que sus plantas (releída el 2026-10-15) | [E] | <https://www.npr.org/sections/thesalt/2017/09/07/547981850/why-do-parrots-and-people-eat-clay> |
| Coordenadas de las paradas y los ríos del mundo 8: Riberalta (Q941357), Rurrenabaque (Q749372), la boca del Tuichi (Q610021), el parque Madidi (Q647206), Guayaramerín (Q1026253), Flor de Oro (Q16891327), el parque Noel Kempff (Q650710), la cascada Arco Iris (Q8342787), Trinidad (Q34986), el Ibare (Q5983734), Cachuela Esperanza (Q992821), los ríos Beni (Q142604), Mamoré (Q117615) e Iténez (Q916349) y los Llanos de Moxos (Q748368) (consulta del 2026-10-15) | [E] | Wikidata (CC0): <https://www.wikidata.org/wiki/Q941357> y los demás elementos por su Q |
| Madidi: de 180 a 5.760 m; parque desde 1995, 18.958 km²; se llega desde San Buenaventura, frente a Rurrenabaque (releída el 2026-10-15) | [E] | <https://en.wikipedia.org/wiki/Madidi_National_Park> |
| Identidad Madidi (WCS, 2018): el área protegida con más plantas, mariposas, aves y mamíferos registrados; 124 candidatas a especie nueva (leída el 2026-10-15) | [V] | <https://newsroom.wcs.org/News-Releases/articleType/ArticleView/articleId/11294/Two-and-a-Half-Year-Identidad-Madidi-Expedition-Ends-After-Visiting-15-Remote-Sites-in-Bolivias-Madidi-National-Park.aspx> |
| Riberalta: confluencia del Madre de Dios y el Beni; «capital amazónica»; el 78 % de la castaña en 2017/2018 (CR-F95); el castaño vive 500 años o más y da fruto casi solo en selva intacta (CR-F93) (releídas el 2026-10-15) | [E] | <https://en.wikipedia.org/wiki/Riberalta> · <https://en.wikipedia.org/wiki/Brazil_nut> |
| El paiche en Bolivia: llegó por accidente en 1976, desplaza peces nativos y se fomenta su pesca (Mongabay Latam, 2025; CR-F92, releída el 2026-10-15) | [E] | <https://es.mongabay.com/2025/07/paiche-invasor-pez-gigante-amenaza-amazonia-bolivia-reportaje-fotografico/> |
| Rurrenabaque y el Tuichi: el Beni sale de los Andes a los Llanos de Moxos; el Tuichi, 265 km, se une al Beni río arriba de Rurrenabaque (leídas el 2026-10-15) | [E] | <https://en.wikipedia.org/wiki/Rurrenabaque> · <https://en.wikipedia.org/wiki/Tuichi_River> · <https://es.wikipedia.org/wiki/R%C3%ADo_Tuichi> |
| Guayaramerín frente a Guajará-Mirim; el Mamoré, 1.930 km; el Iténez, 920 km de frontera Bolivia-Brasil (leídas el 2026-10-15) | [E] | <https://en.wikipedia.org/wiki/Guayaramer%C3%ADn> · <https://en.wikipedia.org/wiki/Mamor%C3%A9_River> · <https://en.wikipedia.org/wiki/Guapor%C3%A9_River> |
| Noel Kempff Mercado: el Iténez como borde; la cascada Arcoiris (88 m); la meseta de Huanchaca (unos 1.000 millones de años); UNESCO en 2000 (leída el 2026-10-15; la ficha de la UNESCO dio 403) | [E] | <https://en.wikipedia.org/wiki/Noel_Kempff_Mercado_National_Park> |
| Trinidad (Beni): se mudó en 1769 por las crecidas del Mamoré; la capital de departamento más lluviosa de Bolivia (leída el 2026-10-15) | [E] | <https://en.wikipedia.org/wiki/Trinidad,_Beni> |
| Llanos de Moxos: hasta la mitad se inunda cada año; sabanas con islas de bosque; el mayor sitio Ramsar designado hasta 2013 (Mongabay, 2013) (leídas el 2026-10-15) | [E] | <https://en.wikipedia.org/wiki/Llanos_de_Moxos> · <https://news.mongabay.com/2013/03/bolivia-leads-the-way-in-wetland-protection/> |
| Bufeo: el delfín de río de Bolivia, con más dientes que el del Amazonas (leída el 2026-10-15) | [E] | <https://en.wikipedia.org/wiki/Bolivian_river_dolphin> |
| Bolivia, DS 0241/2009 (símbolos del Estado): la Bandera Tricolor y la Wiphala, «símbolo sagrado» (art. 28); nada sobre embarcaciones (leído el 2026-10-15) | [V] | <https://www.lexivox.org/norms/BO-DS-N241.html> |
| Coordenadas de las paradas, los ríos y los sitios del mundo 9: Jenaro Herrera (Q15696655), Requena (Q3344103), Contamana (Q3317773), Pucallpa (Q217071), la laguna de Yarinacocha (Q11688197), Bolognesi (Q27649307), Atalaya (Q3322627) y los ríos Ucayali (Q200952) y Tambo (Q1480564), con sus calificadores de boca y nacimiento (consulta del 2026-10-16) | [E] | Wikidata (CC0): <https://www.wikidata.org/wiki/Q200952> y los demás elementos por su Q |
| Ucayali: la rama principal del Amazonas; de 400 a 1.200 m de ancho; un canal de al menos 1,5 m de hondo; 13.385 m³/s en Requena; cuenca de 351.549 km²; 1.600 km del Tambo al Marañón; la cabecera desde el Apurímac en el nevado Mismi (leída el 2026-10-16) | [E] | <https://en.wikipedia.org/wiki/Ucayali_River> |
| Pucallpa: la segunda ciudad de la Amazonía peruana; puerto que se comunica con Iquitos; carretera a Lima desde 1945 (CR-F108, releída el 2026-10-16) | [E] | <https://en.wikipedia.org/wiki/Pucallpa> |
| Yarinacocha: antiguo meandro del Ucayali, que se formó al cambiar el río de curso; 1.340 ha y 20 km; se une al río por sus dos extremos en las lluvias (CR-F147, releída el 2026-10-16) | [E] | <https://es.wikipedia.org/wiki/Laguna_de_Yarinacocha> |
| Yarinacocha, peces del lago (Aylas y otros, 2023): reabierta el 2026-10-16, su resumen no cuenta el origen del lago (la respuesta del mundo 9 pasa a CR-F147) | [E] | <https://scielo.br/j/bn/a/kP34JQdCqtmbsWbS3P8zdLv/abstract/?lang=pt> |
| Contamana: 34 localidades fósiles y al menos 485 especies en unos 60 millones de años (Antoine y otros, 2016, Gondwana Research 31: 30-59; resumen leído el 2026-10-16) | [V] | <https://authors.library.caltech.edu/records/vq65m-an176> |
| Jenaro Herrera, Requena, Contamana y Atalaya: el nombre del distrito; Requena, de 1907, a orillas del Tapiche y del Ucayali; las aguas termales de Contamana; Atalaya, de 1928, en el inicio del Ucayali, y cómo se llega (leídas el 2026-10-16) | [E] | <https://es.wikipedia.org/wiki/Distrito_de_Jenaro_Herrera> · <https://es.wikipedia.org/wiki/Requena_(Per%C3%BA)> · <https://es.wikipedia.org/wiki/Contamana> · <https://es.wikipedia.org/wiki/Atalaya_(Per%C3%BA)> |
| Tambo, Urubamba y Perené: el Tambo nace en Puerto Prado (Ene + Perené), mide unos 159 km y forma el Ucayali con el Urubamba en Atalaya; el Urubamba, 724 km; el Perené, 165 km (leídas el 2026-10-16) | [E] | <https://en.wikipedia.org/wiki/Tambo_River_(Peru)> · <https://en.wikipedia.org/wiki/Urubamba_River> · <https://en.wikipedia.org/wiki/Peren%C3%A9_River> |
| Lagunas en U: cómo una curva cortada se vuelve laguna; las del Amazonas, hábitat del lobo de río (leída el 2026-10-16) | [E] | <https://en.wikipedia.org/wiki/Oxbow_lake> |
| Casiquiare: une el Orinoco y el Amazonas | [E] | <https://en.wikipedia.org/wiki/Casiquiare_canal> |
| Shanay-Timpishka: los chamanes locales y la Yacumama; tres comunidades a su orilla; afluente del Pachitea; no la llama lugar sagrado (releída el 2026-10-16) | [E] | <https://en.wikipedia.org/wiki/Shanay-Timpishka> |
| Bandera de cortesía: costumbre náutica | [E] | <https://www.sailboat-cruising.com/Sailboat-Flag-Etiquette.html> |
| ASTC: bloque de 128 bits (6×6 = 3,56 bpp; 8×8 = 2 bpp) | [V] | <https://en.wikipedia.org/wiki/Adaptive_scalable_texture_compression> |
| Gradiente de meta; progreso regalado | [V] | <https://business.columbia.edu/sites/default/files-efs/pubfiles/1200/goalgradient.pdf> · <https://ideas.repec.org/a/oup/jconrs/v32y2006i4p504-512.html> |
| SMB3 y SMW (mapa de mundo y salidas secretas) | [V] | <https://en.wikipedia.org/wiki/Super_Mario_Bros._3> · <https://mariowiki.com/Secret_exit> |
| Words of Wonders y Gummy Drop! (lugares reales con dato) | [V/E] | <https://apps.apple.com/app/id1369521645> · <https://apps.apple.com/us/app/-/id828578246> |
| Candy Crush (episodios en dos tiempos; guía distinto del avatar) | [E] | <https://candycrush.fandom.com/wiki/Characters> |

---

## Anexo: cómo leer y regenerar la muestra

- **Qué muestra `mapa-muestra.svg`:** el estado al ganar el nivel 200, último de los mundos 1-5 (fin de la 1.0 solo con Q14-B).
  - **Lleno de agua real:** el cauce del Atlántico a Nauta y la boca del Ucayali; el ramal del Pará; el Tapajós hasta la Floresta Nacional; el Negro hasta Anavilhanas; el Marañón hasta la boca del Samiria; y los afluentes de paso, **enteros**, aunque su cabecera quede lejos: Tocantins, Xingu, Trombetas, Purus, Juruá, Japurá y Putumayo-Içá.
  - **En los recuadros** (vista Mundo), los ríos locales: el Nanay de agua negra, lleno hasta la reserva; el Loretoyacu, el Itaya y el Pacaya, punteados porque su trazado es [NV]; el Atacuari y el Samiria, solo con la boca.
  - **Solo la boca:** el Madeira y el Napo, mundos futuros.
  - **En vidrio:** el resto.
- **Qué más lleva:**
  - las paradas numeradas con su nombre (las del Trapecio y Loreto, en los recuadros A y B, que hacen de vista Mundo), cada mini cuia con el agua del sitio (Allpahuayo y Pacaya-Samiria, negra);
  - los puntos de paso, los miradores, las banderas de cortesía (en gris, las de después del 200, sin repetir el mástil de las vueltas) y los mundos por venir en silueta;
  - el inicio en Belém, el nivel 200 y la meta en el nevado Mismi, «una de las nacientes»;
  - las flechas del sentido del viaje por la ruta real de la canoa: Belém → ramal del Pará → Gurupá → río arriba (ninguna por el canal norte);
  - la costa del Atlántico y la del Pacífico (la caja enseña el norte de Sudamérica) y los países sin fronteras.
- **El cambio más habitual:** para cambiar una parada, se edita `rio-amazonas.json` y se ejecuta `--actualizar-xy`, `--check` y, por último, el comando sin opciones.
- **`--check` comprueba:**
  - que las coordenadas normalizadas cuadran con la proyección y caen dentro de la caja;
  - que ningún tramo del cauce mide menos que la línea recta entre sus extremos y que los km desde el mar cuadran;
  - que las paradas k = 0-19 cubren `10k+1 … 10k+10`, llegan al ganar el `10k` y su mundo coincide con `WorldTable.FirstLevels`;
  - que los km hasta casa no retroceden, que cada dato mide ≤ 90 caracteres y cada pregunta ≤ 50, y que cada parada tiene fondo;
  - que 201-1.000 suma 80 paradas, con cada mundo en su primer nivel y sus sitios sumando sus paradas;
  - que cada fuente tiene URL y etiqueta [V] o [E], que cada etiqueta «[V id]» o «[E id]» de **cualquier texto** del JSON lleva a una fuente existente y que cada dato [V] tiene al menos una fuente [V];
  - que las paradas 11, 13, 15, 16 y 18 tienen su río local, de tipo `rio_local`;
  - que los afluentes de paso se llenan enteros en la muestra;
  - que el nivel de cada cruce de país sale de las paradas (o del viaje de entrada del mundo) y no está escrito a mano;
  - que cada parada tiene su lista `otros_datos` y que cada «Otro dato» es un objeto con sus campos, una clave `map.stop.<lugar>.dato_NN` sin repetir, cada marcador (también el del plural `{n:plural:…|…}`, contrato 32) con su valor en `args`, ≤ 90 caracteres con los marcadores resueltos, ninguna cifra con separador (Q8-A), ninguna «Belén», etiqueta [V] o [E] con sus fuentes reabiertas en la fecha de `releida`, y que están los candidatos de la sección 5.1 bis (sección 5.1 ter);
  - que las paradas detalladas de 201-700 (`paradas_201_700`) están completas, con su dato, su fondo sin héroe, los sitios del resumen y sus banderas, y sin ningún [NV] (sección 5.4 bis); y, desde M3.7b, que los sitios del resumen de un mundo detallado tienen coordenada y ningún [NV], que el viaje del 300 lleva el punto de paso `teotonio` con su dato, que la parada 32 lleva los cuatro candidatos de la pregunta 50 (a) y que la bandera de un viaje de entrada está en la primera parada de su mundo (sección 5.4 ter); y, desde M3.7c, que los km a casa solo bajan dentro de un mundo en una parada con `baja_por_confluencia`, en otro río que la anterior y nunca por debajo de la última parada de su mismo río (sección 5.4 quater); y, desde M3.7d, que el viaje del 560 también lleva `teotonio` y que un punto de paso que sale en varios viajes es igual en todos (sección 5.4 quinquies);
  - con casos fijos (M2.7f y FIXI), que esa revisión acepta un plural con su valor en `args`, lo mide por su forma más larga con la cifra puesta y rechaza el mismo plural sin su valor o con un valor que no es una cifra (`"1200"` o `true`), y que una parada candidata cuyas entradas de `otros_datos` no son objetos (`"texto"` y `null`) da «no es un objeto» por cada una sin romper la validación; y, con casos fijos más (M3.7a), que la revisión de 201-700 no da errores con las paradas de verdad y que, a nombre de lo que cambia, rechaza el dato del 200 con otra clave, un [NV] en una nota, un héroe pintado, los km a casa que retroceden, los sitios fuera de orden y una bandera ausente o sobrante, y acepta un dato de llegada `map.general.*`; y (M3.7b) que rechaza la parada 32 sin un candidato de la pregunta 50, el viaje del 300 sin `teotonio` o con su dato sin etiqueta, un sitio del resumen sin coordenada y la parada 30 sin la bandera de Bolivia; y (M3.7c) que rechaza la bajada de km de Guayaramerín sin `baja_por_confluencia` o en el río de la parada anterior, la de Trinidad por debajo de Guayaramerín y un `baja_por_confluencia` sobrante; y (M3.7d) que repite para el viaje del 560 los casos de `teotonio` y del sitio sin coordenada, y que rechaza el `teotonio` del 560 con otra longitud o con otro dato que el del 300.
- **Límites de la muestra:** la costa, el pie de los Andes, tres tramos intermedios de afluentes de paso y el trazado de los ríos locales son aproximados [NV], y el cauce no lleva todos los meandros. El arte final sale de Natural Earth y HydroRIVERS (sección 3.6).
