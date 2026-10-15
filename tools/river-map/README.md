# tools/river-map: datos del Mapa del río

Herramienta del Mapa del río (D-013; propuesta del Mapa §11.3). Genera los datos que lee el cliente en `Assets/_Project/Data/Map/` desde la investigación de `docs/research/mapa-del-rio/rio-amazonas.json`, que nunca modifica.

- **Python 3.14, solo biblioteca estándar** en los generadores (08 §4.5). `jsonschema`, que ya está en el stack, solo en `test_schemas.py`.
- **Generados, nunca a mano:** `Assets/_Project/Data/Map/**` y, desde M1.1t, el bloque de filas `map.*` de `Assets/_Project/Data/Loc/facts.csv`.
- Los `.meta` de `Data/Map/` los genera Unity (M1.2, que se fusiona junto con M1.1).

## Archivos

| Archivo | Qué es |
|---|---|
| `stops.py` | Generador y comprobación de `river_stops_v1.json` (`af-river-stops/1`, contrato 30) y del bloque de filas `map.*` de `facts.csv` (contrato 32; M1.1t). |
| `riverdata.py` | Lectura compartida: rutas, `rio-amazonas.json`, `WorldTable.cs` (tramos de los mundos del cliente), nombres de país por ISO2, registros CSV RFC 4180 sin tocar sus bytes, mensajes ASCII y el veto de «Belén». |
| `overrides.json` | Decisiones [P] de P-M1 (a) que `rio-amazonas.json` no lleva en sus campos: el país de la parada 13 y los nombres y ríos de la placa que la investigación no da en la forma de 07 §2.1 o de `map_arrived` (n = 0, 7, 12, 13, 17, 18 y 19), cada una con su `why`. |
| `test_stops.py` | Pruebas del generador: campos, conversión, sincronía con `WorldTable.cs`, determinismo, desfase, fixtures inválidos (una por regla) y el bloque de textos de `facts.csv`. |
| `test_schemas.py` | Esquemas del Mapa con `jsonschema`: el esquema, el archivo real y fixtures inválidos. |
| `../../schemas/af-river-stops-1.schema.json` | Esquema de `af-river-stops/1`; con 08 §4.5, la copia única del formato. |

## Uso (desde la raíz del repositorio)

```
py -3.14 tools/river-map/stops.py                    # escribe Assets/_Project/Data/Map/river_stops_v1.json
py -3.14 tools/river-map/stops.py --write-texts      # regenera solo el bloque map.* de Assets/_Project/Data/Loc/facts.csv
py -3.14 tools/river-map/stops.py --check            # valida y compara las dos salidas con lo commiteado; código 1 si algo falla
py -3.14 -m unittest discover -s tools/river-map -p "test_*.py"
py -3.14 docs/research/mapa-del-rio/generar_mapa.py --check
```

- `--check` regenera en memoria y falla (código 1, mensajes solo en ASCII) si una regla no se cumple o si lo commiteado (`river_stops_v1.json` y el bloque `map.*` de `facts.csv`) no es byte a byte lo que se regenera (desfase, BOM o CRLF incluidos). Con errores no se escribe nada.
- Si cambia la fuente, se regeneran las dos salidas: `stops.py` y `stops.py --write-texts`.
- `--source`, `--overrides`, `--worldtable`, `--out` y `--facts` cambian las rutas de entrada y salida: los usan las pruebas para los fixtures.
- Determinista: UTF-8 sin BOM, LF, raíz con 2 espacios, una parada por línea y orden de campos fijo. No depende de la semilla de hash ni de la cultura.
- Si cambia `rio-amazonas.json` (M2.7, M3.7), la tarea que regenera (M2.6, M4.5) se fusiona con ella: si no, `--check` falla por desfase.

## `river_stops_v1.json` (`af-river-stops/1`)

Raíz `{format, source, stops[]}` [P]: `format` = `af-river-stops/1`, `source` = la ruta de la fuente y una parada por `n`. Orden de campos de cada parada (08 §4.5 con los extras de P-M1 (a); `at` y `leg` los añade M2.6 y el esquema ya los admite):

`n`, `levels`, `world`, `id`, `kind`, `country`, `country_name`, [`at`], `km_from_sea`, `km_home`, `sample`, `flag_raise`, `arrival_fact`, `facts`, `world_question`, `answers_world_question`, `bg` {`kit`, `hero`, `code`, `lut`}, [`leg`].

Solo claves de texto, nunca textos (08 §4.5): los textos de `arrival_fact`, `facts`, `world_question` y `country_name`, y los nombres por convención (`map.stop.<id>.name`, `map.stop.<id>.river` y `map.country.<iso2>.name`), van en `facts.csv` (contrato 32; los escribe `stops.py --write-texts` con la forma de clave de la pregunta 49 (a): contenido con puntos).

### Conversión desde `paradas_1_200`

| Campo | De dónde sale |
|---|---|
| `n`, `levels`, `world`, `id` | `k`, `niveles`, `mundo`, `id` |
| `kind` | `gran_parada` → `gate`; `parada` y `variante` → `stop`; `castillo` → `close` |
| `country` | `pais` (ISO2). Con dos países, lo decide `overrides.json` (n = 13: `CO`) |
| `country_name` | `overrides.json` (n = 13: `map.stop.rio_frontera.country`, «Colombia y Perú» [P]); `null` en las demás |
| `km_from_sea`, `km_home` | `km_desde_el_mar` (`null` en n = 0) y `km_casa` |
| `sample` | `agua_del_sitio` sin el paréntesis final: blanca → `white`, negra → `black`, clara → `clear`, clara y blanca → `clear_white`, negra y blanca → `black_white` |
| `flag_raise` | `cruces_de_pais` con `llega_en.parada_1_200` = n: `BR` en 0, `CO` en 8 y `PE` en 14 |
| `arrival_fact` | `null` en n = 0; en n = 1, la clave del dato del registro k = 0 (`map.stop.estuario.dato_03`, el Dato del río 1 del Mapa del 10, 06 §2.3); desde n = 2, `dato_del_nivel.clave` |
| `facts` | `[]` hasta M2.6 (los «Otro dato» de M2.7) |
| `world_question` | `map.world.wNN.question` si la parada lleva `pregunta_del_mundo` (n = 1, 4, 8 y 14) |
| `answers_world_question` | `true` en la parada a la que se llega al ganar el `se_responde_en` de la pregunta (n = 3, 6, 13 y 17) |
| `bg.kit` | `fondo.kit` |
| `bg.hero`, `bg.code` | `fondo.heroe` sin el sufijo «+ LUT …»: `bg_hero_<sitio>` va en `hero`; `codigo:<pieza>` y `kit:<pieza>`, en `code` |
| `bg.lut` | `dusk` o `dawn` si el héroe dice «+ LUT de atardecer» o «+ LUT de alba» (n = 9 y 19) |

### Validaciones de `--check`

- `paradas_1_200` cubre k = 0-19, es decir, los niveles 1-200 (D-013, punto 3).
- `n` contiguos desde 0 y `levels` = [10n+1, 10n+10]; `world` igual a `WorldTable.cs` en los dos extremos del tramo.
- El documento termina en el último nivel de un mundo (regla genérica, vale también para 1-700): así cada mundo presente está completo.
- `gate` en el primer tramo de cada mundo y `close` en el último (el mundo 1, de un solo tramo, solo lleva `gate`).
- `bg.kit` = `bg_kit_w` + el mundo con 2 cifras; uno y solo uno de `hero` y `code`; `fondo.tipo` coherente (pintado, código, solo kit o variante de luz).
- Ids únicos (`[a-z0-9_]+`); ningún «Belén» ni «belen» en ninguna cadena (D-013, punto 11). «Belém» (`belem`, la parada 0) no cae en el veto.
- `country` ISO2; `flag_raise` al empezar y al cambiar de país, y coherente con `cruces_de_pais` y con `bandera`.
- `arrival_fact` `null` solo en n = 0; claves de contenido `map.*` (glosario §10); `country_name` = `map.stop.<id>.country`.
- Una pregunta por mundo, respondida por una sola parada del mismo mundo, después de plantearse.
- `overrides.json`: solo paradas que existen y campos `country`, `country_name`, `name`, `river` y `why`.

Las reglas completas solo las comprueba `stops.py --check`. `RiverStopCatalog` (AF.Levels, M1.2) repite solo el formato, los `n` contiguos, `levels`, `WorldTable`, `gate` y `close` (contrato 30) y la parada final de cada mundo, y `ArtSlots` rechaza «belen».

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

- Columnas (cabecera de L2b, pregunta 49 a): `en` y `pt-BR` vacías, `status` = `pendiente_asesor`, `status_en` y `status_pt-BR` vacías, `context` con el lugar y la fuente (en los datos, cada fuente con su URL), `notes` con la etiqueta y «no se edita a mano», y `args` [P]. Solo llevan [V] o [E] las filas cuyo contexto cita una fuente con URL (08 §4.5); el resto va con [P].
- Cifras (D-018, Q8-A): toda cifra con separador de miles o decimales va como `{n}` en el texto y su valor en `args` (`n=1480`; decimales con punto). La fuente las da en `dato_del_nivel.args` (`{"n": 1480}`, o `"3.25"` entre comillas para un decimal), con las reglas de `LocArgs.TryParse`: hasta 3 pares, nombres `[a-z][a-z0-9_]*` salvo `sep`, como mucho 9 decimales y enteros en el rango de `long` (un valor que el cliente rechazara dejaría toda `facts.csv` sin cargar). Las longitudes cuentan la cifra ya formateada (`1.480`). Hoy ningún texto de 1-200 lleva cifras con separador, así que `args` va vacía.
- Entre una cifra (o `{n}`) y su unidad (`km`, `m`, `%`…) va U+00A0 (08 §4.7): el generador cambia el espacio normal.
- `--check` falla si el bloque no está al día (desfase), si una clave que cita `river_stops_v1.json` (explícita o por convención) no tiene fila o no tiene texto es-419, si un texto pasa de su `max_chars`, si lleva una cifra con separador o decimales («1.480», «4,5»), si alguna columna cae en el veto de «Belén», si un `args` no lo aceptaría `LocArgs.TryParse`, si un río no es solo el nombre del río (paréntesis, la palabra «río» o minúscula), si una fila con [V] o [E] no cita ninguna URL en su contexto, si un dato es [NV], no tiene fuente, cita una fuente que no existe o sin URL, o si un dato [V] no tiene ninguna fuente [V].

Fuera de alcance (tareas siguientes): `at`, `leg` y `facts` con sus textos (M2.6), paradas 201-700 (M4.5a y M4.5b) y la geometría de producción (M5.1).
