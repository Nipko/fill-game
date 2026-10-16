# tools/river-map: datos del Mapa del río

Herramienta del Mapa del río (D-013; propuesta del Mapa §11.3). Genera los datos que lee el cliente en `Assets/_Project/Data/Map/` desde la investigación de `docs/research/mapa-del-rio/rio-amazonas.json`, que nunca modifica.

- **Python 3.14, solo biblioteca estándar** en los generadores (08 §4.5). `jsonschema`, que ya está en el stack, solo en `test_schemas.py`.
- **Generados, nunca a mano:** `Assets/_Project/Data/Map/**` (`river_stops_v1.json` con `stops.py` y, desde M2.6, `river_map_v1.json` con `geometry.py`) y, desde M1.1t, el bloque de filas `map.*` de `Assets/_Project/Data/Loc/facts.csv`.
- Los `.meta` de `Data/Map/` los genera Unity (M1.2, que se fusiona junto con M1.1).

## Archivos

| Archivo | Qué es |
|---|---|
| `stops.py` | Generador y comprobación de `river_stops_v1.json` (`af-river-stops/1`, contrato 30), con `at` y `leg` desde M2.6, y del bloque de filas `map.*` de `facts.csv` (contrato 32; M1.1t y M2.6). |
| `geometry.py` | Generador y comprobación de `river_map_v1.json` (`af-river-map/1`, contrato 29; M2.6) con la geometría de respaldo de `rio-amazonas.json`, y las rutas de las paradas (`at` y `leg`) que usa `stops.py`. |
| `riverdata.py` | Lectura compartida: rutas, `rio-amazonas.json`, `WorldTable.cs` (tramos de los mundos del cliente), nombres de país por ISO2, registros CSV RFC 4180 sin tocar sus bytes, mensajes ASCII, el veto de «Belén», el id, el nombre y la clave del dato de los puntos de paso y de los miradores, y (M4.5a) las paradas de sus dos listas (`stop_records`), los puntos de paso con id de los viajes de entrada (`travel_passing_points`) y el texto final de un dato entre «» (`final_fact_of`). |
| `overrides.json` | Decisiones [P] de P-M1 (a) que `rio-amazonas.json` no lleva en sus campos: el país de la parada 13 y los nombres y ríos de la placa que la investigación no da en la forma de 07 §2.1 o de `map_arrived` (n = 0, 7, 12, 13, 17, 18, 19 y, desde M4.5a, 34, cuya placa con dos ríos no cabía en los 64 caracteres de `map_arrived`), cada una con su `why`. |
| `test_stops.py` | Pruebas del generador: campos, conversión, sincronía con `WorldTable.cs`, determinismo, desfase, fixtures inválidos (una por regla) y el bloque de textos de `facts.csv`. |
| `test_geometry.py` | Pruebas de `geometry.py`: proyección (cinco puntos contra `generar_mapa.py`), base64 de ida y vuelta, `s` de bocas y paradas, triangulación, determinismo, legs continuos y fallos de `--check`. |
| `test_schemas.py` | Esquemas del Mapa con `jsonschema`: los dos esquemas, los dos archivos reales y fixtures inválidos. |
| `../../schemas/af-river-stops-1.schema.json` | Esquema de `af-river-stops/1`; con 08 §4.5, la copia única del formato. |
| `../../schemas/af-river-map-1.schema.json` | Esquema de `af-river-map/1` (M2.6); con 08 §4.5, la copia única del formato. |

## Uso (desde la raíz del repositorio)

```
py -3.14 tools/river-map/geometry.py                 # escribe Assets/_Project/Data/Map/river_map_v1.json
py -3.14 tools/river-map/geometry.py --check         # valida el Mapa y las rutas de las paradas; código 1 si algo falla
py -3.14 tools/river-map/stops.py                    # escribe Assets/_Project/Data/Map/river_stops_v1.json
py -3.14 tools/river-map/stops.py --write-texts      # regenera solo el bloque map.* de Assets/_Project/Data/Loc/facts.csv
py -3.14 tools/river-map/stops.py --check            # valida y compara las dos salidas con lo commiteado; código 1 si algo falla
py -3.14 -m unittest discover -s tools/river-map -p "test_*.py"
py -3.14 docs/research/mapa-del-rio/generar_mapa.py --check
```

- `--check` regenera en memoria y falla (código 1, mensajes solo en ASCII) si una regla no se cumple o si lo commiteado (`river_stops_v1.json` y el bloque `map.*` de `facts.csv`) no es byte a byte lo que se regenera (desfase, BOM o CRLF incluidos). Con errores no se escribe nada.
- Si cambia la fuente, se regeneran las tres salidas: `geometry.py`, `stops.py` y `stops.py --write-texts`.
- `geometry.py --check` regenera el Mapa en memoria y falla si lo commiteado no es byte a byte lo mismo, si una regla de la geometría no se cumple o si las rutas commiteadas de `river_stops_v1.json` no son de este Mapa; además, ejecuta todas las reglas de `stops.py` (candidatos de la propuesta §5.1 bis incluidos). Sus opciones `--source`, `--overrides`, `--worldtable`, `--out`, `--stops` y `--facts` las usan las pruebas. Avisa con `AVISO`, sin fallar, de los datos de los puntos de paso y de los miradores que aún no tienen texto en `facts.csv` (la investigación da su clave, pero no su texto final).
- `--source`, `--overrides`, `--worldtable`, `--out` y `--facts` cambian las rutas de entrada y salida: los usan las pruebas para los fixtures.
- Determinista: UTF-8 sin BOM, LF, raíz con 2 espacios, una parada por línea y orden de campos fijo. No depende de la semilla de hash ni de la cultura.
- Si cambia `rio-amazonas.json` (M2.7, M3.7), la tarea que regenera (M2.6, M4.5) se fusiona con ella: si no, `--check` falla por desfase.

## `river_stops_v1.json` (`af-river-stops/1`)

Raíz `{format, source, stops[]}` [P]: `format` = `af-river-stops/1`, `source` = la ruta de la fuente y una parada por `n`. Orden de campos de cada parada (08 §4.5 con los extras de P-M1 (a); `at` y `leg` los añade M2.6 y el esquema ya los admite):

`n`, `levels`, `world`, `id`, `kind`, `country`, `country_name`, `at` {`line`, `s`}, `km_from_sea`, `km_home`, `sample`, `flag_raise`, `arrival_fact`, `facts`, `world_question`, `answers_world_question`, `bg` {`kit`, `hero`, `code`, `lut`}, `leg` {`path`, `via`, `mouths`}. `at` y `leg`, obligatorios desde M2.6.

Solo claves de texto, nunca textos (08 §4.5): los textos de `arrival_fact`, `facts`, `world_question` y `country_name`, y los nombres por convención (`map.stop.<id>.name`, `map.stop.<id>.river` y `map.country.<iso2>.name`), van en `facts.csv` (contrato 32; los escribe `stops.py --write-texts` con la forma de clave de la pregunta 49 (a): contenido con puntos).

### Conversión desde `paradas_1_200` y `paradas_201_700`

Las paradas salen de dos listas de la investigación con el mismo registro: `paradas_1_200` (k = 0-19, niveles 1-200; M1.1) y, desde M4.5a, `paradas_201_700` (k = 20-69, niveles 201-700, Q14-A), que lleva además `sitio` y `wikidata` (propuesta del Mapa §5.4 bis). Hoy llega hasta k = 41 (los mundos 6 y 7, de M3.7a y M3.7b): 42 paradas, niveles 1-420. Ninguna k se repite entre las dos listas.

| Campo | De dónde sale |
|---|---|
| `n`, `levels`, `world`, `id` | `k`, `niveles`, `mundo`, `id` |
| `kind` | `gran_parada` → `gate`; `parada` y `variante` → `stop`; `castillo` → `close` |
| `country` | `pais` (ISO2). Con dos países, lo decide `overrides.json` (n = 13: `CO`) |
| `country_name` | `overrides.json` (n = 13: `map.stop.rio_frontera.country`, «Colombia y Perú» [P]); `null` en las demás |
| `km_from_sea`, `km_home` | `km_desde_el_mar` (`null` en n = 0) y `km_casa` |
| `sample` | `agua_del_sitio` sin el paréntesis final: blanca → `white`, negra → `black`, clara → `clear`, clara y blanca → `clear_white`, negra y blanca → `black_white` |
| `flag_raise` | El cruce de `cruces_de_pais` que llega a la parada n: `llega_en.parada_1_200` = n (`BR` en 0, `CO` en 8 y `PE` en 14); `llega_en` {`mundo`, `sitio`}, la primera parada de ese mundo cuyo `sitio` empieza por ese nombre (`EC` en 22, Nuevo Rocafuerte; `PE` en 31, el lago Sandoval, con `vuelta`); y `llega_en.viaje_de_entrada` = mundo, su primera parada (`BO` en 30, Puerto Heath). Un cruce de un mundo sin paradas todavía (el 420 y los de 561-1.000) no iza nada; si lleva `nivel`, es el de la llegada, 10n |
| `arrival_fact` | `null` en n = 0; en n = 1, la clave del dato del registro k = 0 (`map.stop.estuario.dato_03`, el Dato del río 1 del Mapa del 10, 06 §2.3); desde n = 2, `dato_del_nivel.clave` |
| `facts` | Las claves de `otros_datos` de la parada, en su orden: los «Otro dato» de la tarjeta (M2.7; M2.6) |
| `at` | El punto del río de la parada (M2.6): en su `ancla_rio`, en su `km_desde_el_mar` (en el cauce, interpolando los km de sus puntos; en un afluente, el km de su boca más el recorrido sin meandros por sus puntos, como hace la investigación). En n = 0, sin km, el pie de Belém sobre el ramal del Pará. Un km que se pase del extremo de la línea en menos de 15 km se lleva a ese extremo (Allpahuayo-Mishana, en la cabecera del esbozo del Nanay) |
| `leg` | El viaje desde la parada anterior (M2.6): `path`, los tramos por agua `{line, s0, s1}` en el sentido del viaje, subiendo por los anfitriones hasta el río común y bajando hasta la parada (ida y vuelta por las confluencias); `via`, los puntos de paso que se cruzan, en orden (sin el del punto de partida); `mouths`, los afluentes cuya boca se cruza por primera vez y que no son parte del camino (se llenan con su `fill_on_pass`). En n = 0, un tramo de longitud cero en Belém con `mouths` = [`tocantins`], que va lleno desde el primer Mapa [P] |
| `world_question` | `map.world.wNN.question` si la parada lleva `pregunta_del_mundo` (n = 1, 4, 8 y 14) |
| `answers_world_question` | `true` en la parada a la que se llega al ganar el `se_responde_en` de la pregunta (n = 3, 6, 13 y 17) |
| `bg.kit` | `fondo.kit` |
| `bg.hero`, `bg.code` | `fondo.heroe` sin el sufijo «+ LUT …»: `bg_hero_<sitio>` va en `hero`; `codigo:<pieza>` y `kit:<pieza>`, en `code` |
| `bg.lut` | `dusk` o `dawn` si el héroe dice «+ LUT de atardecer» o «+ LUT de alba» (n = 9 y 19) |

### Validaciones de `--check`

- `paradas_1_200` cubre k = 0-19, es decir, los niveles 1-200 (D-013, punto 3); `paradas_201_700` solo lleva k = 20-69, y ninguna k se repite.
- `n` contiguos desde 0 y `levels` = [10n+1, 10n+10]; `world` igual a `WorldTable.cs` en los dos extremos del tramo.
- El documento termina en el último nivel de un mundo (regla genérica, vale también para 1-700): así cada mundo presente está completo.
- `gate` en el primer tramo de cada mundo y `close` en el último (el mundo 1, de un solo tramo, solo lleva `gate`).
- `bg.kit` = `bg_kit_w` + el mundo con 2 cifras; uno y solo uno de `hero` y `code`; `fondo.tipo` coherente (pintado, código, solo kit o variante de luz).
- Ids únicos (`[a-z0-9_]+`); ningún «Belén» ni «belen» en ninguna cadena (D-013, punto 11). «Belém» (`belem`, la parada 0) no cae en el veto.
- `country` ISO2; `flag_raise` al empezar y al cambiar de país, y coherente con `cruces_de_pais` y con `bandera`.
- `arrival_fact` `null` solo en n = 0; claves de contenido `map.*` (glosario §10); `country_name` = `map.stop.<id>.country`.
- Una pregunta por mundo, respondida por una sola parada del mismo mundo, después de plantearse.
- `overrides.json`: solo paradas que existen y campos `country`, `country_name`, `name`, `river` y `why`.

- Rutas (M2.6, con el Mapa de `geometry.py`): `at` y `leg` existen; cada tramo es de una línea del Mapa y solo pasa a otra por una confluencia (la boca de un afluente y su `s` sobre el anfitrión): cualquier otro salto «cruza tierra»; el leg empieza donde estaba la canoa y acaba en la parada; el sitio queda a ≤ 15 km de su punto del río (≤ 2 dp en la vista Cuenca; 08 §4.5 y D-023, punto 20). En los sitios tierra adentro (áreas protegidas cuya coordenada es el centro), lo que se mide es su punto de atraque: el campo `atraque` {`lat`, `lon`} del registro de la investigación, con los mismos 15 km. Mientras la investigación no lo dé, rigen las **excepciones pendientes del dueño** de `PENDING_SITE_KM`, que no son canon: la distancia medida entre el centro y su punto del río, redondeada hacia arriba a 0,1 km y sin margen (la FLONA do Tapajós, 23,1 km; Amacayacu, 57,5 km; Pacaya-Samiria y su variante, 46,2 km). `--check` avisa de cada una y falla si el sitio se aleja más; con `atraque`, la excepción deja de usarse y el aviso dice que sobra; el km de su punto del río coincide con `km_from_sea` (±15 km); y entre dos paradas seguidas en la misma línea, ni la `s` baja río abajo ni los km retroceden.
- Candidatos de la propuesta §5.1 bis (`CANDIDATOS_5_1_BIS` de `generar_mapa.py`, la copia única de la lista): cada uno, en los `facts` o como `arrival_fact` de su parada.

Las reglas completas solo las comprueban `stops.py --check` y `geometry.py --check`. `RiverStopCatalog` (AF.Levels, M1.2) repite solo el formato, los `n` contiguos, `levels`, `WorldTable`, `gate` y `close` (contrato 30) y la parada final de cada mundo, y `ArtSlots` rechaza «belen».

## Textos del Mapa en `facts.csv` (M1.1t)

`stops.py --write-texts` regenera en `Assets/_Project/Data/Loc/facts.csv` (contrato 32; 08 §4.7) el bloque de filas `map.*`, contiguo y en orden ordinal (`sorted()` de Python = `StringComparer.Ordinal` de C#; regla de G1). La cabecera y las demás filas (las de fauna de L2b y las que lleguen) quedan byte a byte como estaban; el bloque va en su sitio ordinal entre ellas. Nadie más escribe filas `map.*`.

| Claves | Texto | `max_chars` | `notes` |
|---|---|---|---|
| `map.stop.<id>.name` | `nombre` de la parada, o `name` de `overrides.json` (07 §2.1: «El río frontera», «Nauta») | 32 [P] | [P]: la investigación no da una fuente con URL para el nombre, y su `conf` es la confianza de la coordenada |
| `map.stop.<id>.river` | `rio` de la parada, o `river` de `overrides.json`: va en {río} de `map_arrived` («{lugar} · {país} · río {río}»), así que es solo el nombre del río, con mayúscula, sin la palabra «río» ni descripciones entre paréntesis | 32 [P] | [P] |
| `map.country.<iso2>.name` | `COUNTRY_NAMES` de `riverdata.py` (glosario §8), que tiene que estar en `rotulos_de_pais` | 32 [P] | [P] |
| `country_name` (n = 13) | Los países de `pais` («CO/PE» → «Colombia y Perú») | 32 [P] | [P] |
| `arrival_fact` | `dato_del_nivel.texto` (en n = 1, el del registro k = 0); el contexto lleva el lugar, la victoria, quién lo presenta (TiTino, en la placa del Dato del río con su retrato; D-013, punto 6) y cada fuente con su etiqueta y su URL. El de n = 1 sale en el Mapa de la secuencia de la victoria del 10 (06 §2.3; 02 §6), no bajo `map_arrived` | 90 (08 §4.5) | La etiqueta del dato, [V] o [E] |
| `world_question` | `pregunta_del_mundo.texto`; el contexto dice dónde se plantea y qué dato la responde | 50 (08 §4.5) | [P] |
| `facts` (M2.6) | `otros_datos[].texto` de la parada: el «Otro dato» de la tarjeta (`map_card_other_fact`), nunca en la victoria; el contexto lleva el origen, la fecha en que se reabrieron sus fuentes, cada fuente con su etiqueta y su URL y la nota de la investigación | 90 (08 §4.5) | La etiqueta del dato, [V] o [E] |
| `map.stop.<id>.name` de los puntos de paso y de los miradores (M2.6) | `nombre` de `puntos_de_paso_1_200` y `miradores_1_200` sin el paréntesis final y con mayúscula inicial [P] (`place_name` de `riverdata.py`; el id, `place_id`: «boca del Madeira» → `boca_del_madeira`) | 32 [P] | [P] |
| `map.country.<iso2>.name` de los rótulos (M2.6) | Los países de `rotulos_de_pais` que no tiene ninguna parada (Ecuador, Bolivia, Venezuela, Guyana, Surinam y Guayana Francesa): salen en `labels` del Mapa | 32 [P] | [P] |

- Columnas (cabecera de L2b, pregunta 49 a): `en` y `pt-BR` vacías, `status` = `pendiente_asesor`, `status_en` y `status_pt-BR` vacías, `context` con el lugar y la fuente (en los datos, cada fuente con su URL), `notes` con la etiqueta y «no se edita a mano», y `args` [P]. Solo llevan [V] o [E] las filas cuyo contexto cita una fuente con URL (08 §4.5); el resto va con [P].
- Cifras (D-018, Q8-A): toda cifra con separador de miles o decimales va como `{n}` en el texto y su valor en `args` (`n=1480`; decimales con punto). La fuente las da en `dato_del_nivel.args` (`{"n": 1480}`, o `"3.25"` entre comillas para un decimal), con las reglas de `LocArgs.TryParse`: hasta 3 pares, nombres `[a-z][a-z0-9_]*` salvo `sep`, como mucho 9 decimales y enteros en el rango de `long` (un valor que el cliente rechazara dejaría toda `facts.csv` sin cargar). Las longitudes cuentan la cifra ya formateada (`1.480`). Ningún texto de 1-200 lleva cifras con separador, así que su `args` va vacía; las de 201-420 sí las llevan (`n=6800`, `n=274690`…).
- **Veto de «Belén» en todas las columnas:** de una fuente cuyo título lo lleva (CR-F28, la página del distrito, fuente de `itaya.dato_01`) el contexto da su id y su URL, que la identifican, y «título omitido por el veto de D-013, punto 11» en lugar del título (M2.6). Cualquier otra columna con «Belén» (un texto, un nombre o la nota de la investigación) sigue fallando.
- Entre una cifra (o `{n}`) y su unidad (`km`, `m`, `m³`, `%`…) va U+00A0 (08 §4.7): el generador cambia el espacio normal.
- `--check` falla si el bloque no está al día (desfase), si una clave que cita `river_stops_v1.json` (explícita o por convención) no tiene fila o no tiene texto es-419, si un texto pasa de su `max_chars`, si lleva una cifra con separador o decimales («1.480», «4,5»), si alguna columna cae en el veto de «Belén», si un `args` no lo aceptaría `LocArgs.TryParse`, si un río no es solo el nombre del río (paréntesis, la palabra «río» o minúscula), si una fila con [V] o [E] no cita ninguna URL en su contexto, si un dato es [NV], no tiene fuente, cita una fuente que no existe o sin URL, o si un dato [V] no tiene ninguna fuente [V].

## `river_map_v1.json` (`af-river-map/1`; M2.6)

Contrato 29 (08 §4.5, con la codificación y los extras [P] de P-M1, D-023). Mientras no lleguen Natural Earth y HydroRIVERS (D-013, punto 5, Q11-A; los descarga el dueño en `tools/river-map/source/`, M5.1), la geometría sale de los puntos de `rio-amazonas.json`: `src` = `rio_amazonas`, la forma correcta a escala de cuenca, sin meandros (propuesta §3.6 y §11.3, punto 7). Raíz, en este orden: `format`, `frame`, `lines`, `land`, `insets`, `home`, `labels`, `viewpoints` y `passing`; una línea del archivo por elemento.

- **Marco** (`frame`; propuesta §3.1, la proyección de `generar_mapa.py`): equirrectangular con cos(lat0), lat0 = −5, girada 90° en el sentido de las agujas del reloj y sin espejo: x = (lat − lat_min) / (lat_max − lat_min), y = (lon − lon_min) / (lon_max − lon_min); en pantalla, px = x · W y py = y · W · aspecto (1,31498). Cada punto es un par uint16 (X, Y) = (round(65535 x), round(65535 y)).
- **Codificación** (P-M1): `pts` y `verts`, base64 de uint16 LE, el primer par absoluto y los demás deltas módulo 65.536; `tris`, índices absolutos. El error de cuantizar no llega a 0,1 px ni a 15× (`test_geometry.py` exige ≤ 0,5).
- **`s`:** fracción de la longitud de la línea medida en pantalla (X / 65535, Y / 65535 · aspecto) desde su primer punto, la misma que recalcula el cliente. Los puntos van de la cabecera a la boca (el cauce, del Mismi al Atlántico; el ramal del Pará, de Gurupá a la bahía): **`s` crece río abajo** [P]. Es una desviación del ejemplo de 08 §4.5, sin registrar todavía: allí, en el cauce, `s` se mide desde el mar (`water` blanca en [0; 0,58], `splits` del Encuentro en 0,214, `mouth` del Xingu en 0,053), mientras que el `leg` de iquitos del mismo ejemplo (0,49 → 0,437 río arriba) va en este sentido: el ejemplo se contradice. Queda pendiente de que el carril de documentación alinee 08 §4.5 o de que el dueño elija; hasta entonces, el cliente (M2.x y M3.2) sigue este archivo y su esquema.
- **`lines`:** 31, en el orden de `rios` y después de `rios_locales_201_700`: el cauce y el ramal del Pará (`main`, class 1), los afluentes de los mundos (`tributary`, class 2), los de paso con el Tocantins (`passby`, class 2), los ríos locales de las paradas 11, 13, 15, 16 y 18 y, desde M4.5a, los de 201-420 (el Aguarico y el Cuyabeno del desvío a Cuyabeno, paradas 25 y 26; el Tambopata de la collpa, 35-37; y el Manu, 40 y 41), con sus tramos rectos de respaldo (`local`, class 3) [P]. El `boca_en` de un río de la investigación tiene que ser su anfitrión de `HOSTS`. El Maroni y el Orinoco, de fuera de la cuenca, van en los recuadros, que la 1.0 deja vacíos (`insets` = []); el esquema ya fija la forma de 08 §4.5 de cada recuadro (`{id, rect, lines, link}`).
  - `water`: el agua de Sioli de la investigación (`white`, `black`, `clear`; `andean` [P] en la cabecera del cauce, por el `agua` de cada punto). Si la investigación la da [NV] (Casiquiare, Takutu y cuatro locales), sin tramos: el cliente la pinta en vidrio.
  - `mouth` {`on`, `s`}: el anfitrión y la `s` donde entra su último punto (tabla `HOSTS` de `geometry.py`); la boca tiene que caer a ≤ 15 km de su río. La del Pacaya es una excepción pendiente del dueño (`PENDING_MOUTH_KM`, la cifra medida: 37,1 km): entra por el canal de Puinahua, que el cauce de respaldo no dibuja; `--check` lo avisa hasta HydroRIVERS (M5.1). El ramal del Pará sale del cauce en Gurupá (su `s` = 0) y no lleva `mouth`: es `main` y va lleno.
  - `fill_on_pass`: 1 en los de paso y en los locales; en los afluentes, la fracción de sus primeros `MOUTH_FILL_KM` = 100 km [P] («solo su boca, unos 80-120 km», propuesta §3.4: el Madeira, 0,088; el Napo, 0,136).
  - `splits` (solo en el cauce): Encuentro y Santarém, desde la confluencia `SPLIT_KM` = 65 km río abajo [P] (los de la muestra), con el agua de cada orilla mirando río abajo: el Negro, del norte, a la izquierda; el Tapajós, del sur, a la derecha.
- **`land`:** el anillo de la costa del Pacífico y la del Atlántico de la muestra (`decorado_de_la_muestra`, [NV]) con la esquina sureste de la caja, triangulado por orejas (enteros, determinista), con una sola zona [P] `{id: land, tile: canopy, first, count}` hasta la máscara del artista (M5.1). `first` y `count` (los triángulos de la zona, seguidos) son una extensión [P] de la forma de 08 §4.5 (`{id, tile}`), pendiente de llevar a 08 §4.5.
- **`home`:** la casa de TiTino, en Belém, en la bahía de Guajará (`on` = `para`, la `s` del pie de Belém).
- **Extras de P-M1:** `labels` {`country`, `xy`} (los `rotulos_de_pais`; texto `map.country.<iso2>.name`); `viewpoints` {`id`, `xy`, `reveal_on` = 10 [P], `fact`} (los tres miradores del estuario: el primer Mapa los revela); `passing` {`id`, `line`, `s`, `fact`} (los 17 de `puntos_de_paso_1_200` y, desde M4.5a, los de `puntos_de_paso_del_viaje` de `paradas_201_1000` que llevan `id`: `teotonio`, el relevo del guía en el Madeira del viaje del 300, Q18-A y P-M8; los que no llevan `id`, como Villa Bella, son notas del resumen; cada uno en la línea más cercana; con empate a menos de 0,1 km, la primera del orden, el cauce: Gurupá y las bocas quedan en el cauce). Los nombres, `map.stop.<id>.name` (el id es el de la investigación si lo da y, si no, el de su nombre); `fact`, la clave del campo `dato` de la investigación o null. Si la investigación da el texto final del dato entre «» (el de `teotonio`), `stops.py --write-texts` escribe su fila (90 caracteres, con su etiqueta y sus fuentes); los demás aún no tienen texto final: no hay fila en `facts.csv` y `geometry.py --check` lo avisa.

### Validaciones de `geometry.py --check`

- Desfase: `river_map_v1.json` es byte a byte lo que genera la fuente.
- Cada punto de control de `rio-amazonas.json` queda a ≤ 2 dp de su línea en su vista: con el respaldo, la vista Cuenca, ≤ 15 km (`TOLERANCE_KM`; con 13,1 dp por grado). La vista Mundo (≤ 1 km a 15×) llega con HydroRIVERS (M5.1).
- Cada tramo del cauce mide al menos la línea recta entre sus extremos; cada boca cae sobre su río; `water` cubre [0, 1] en tramos seguidos; un afluente de paso se llena entero.
- La tierra es un polígono simple y sus triángulos (todos en el mismo sentido) suman su área; las zonas cubren todos los triángulos.
- Ids únicos (puntos de paso, miradores y paradas no comparten `map.stop.<id>.name`) y ningún «Belén».
- Las rutas de `river_stops_v1.json` son de este Mapa (ver «Validaciones de `--check`» arriba) y todas las reglas de `stops.py`.

Fuera de alcance (tareas siguientes): paradas 421-700 y sus ríos (M4.5b, con M3.7c y M3.7d), la geometría de producción con Natural Earth y HydroRIVERS (M5.1), los recuadros y los textos de los datos de los puntos de paso de 1-200 y de los miradores.

Notas de M4.5a: los ríos locales se llenan enteros al pasar su boca (`fill_on_pass` = 1 [P]), aunque el Aguarico (301 km), el Tambopata (243 km) y el Manu (146 km) se pasan unas paradas antes de visitarlos (220, 330 y 380); el viaje de entrada de los mundos 6 y 7 deshace camino, así que los km a casa bajan al entrar en el mundo y solo dentro de cada mundo no retroceden.
