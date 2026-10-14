# tools/river-map: datos del Mapa del río

Herramienta del Mapa del río (D-013; propuesta del Mapa §11.3). Genera los datos que lee el cliente en `Assets/_Project/Data/Map/` desde la investigación de `docs/research/mapa-del-rio/rio-amazonas.json`, que nunca modifica.

- **Python 3.14, solo biblioteca estándar** en los generadores (08 §4.5). `jsonschema`, que ya está en el stack, solo en `test_schemas.py`.
- **Generados, nunca a mano:** `Assets/_Project/Data/Map/**` y, desde M1.1t, el bloque de filas `map.*` de `Assets/_Project/Data/Loc/facts.csv`.
- Los `.meta` de `Data/Map/` los genera Unity (M1.2, que se fusiona junto con M1.1).

## Archivos

| Archivo | Qué es |
|---|---|
| `stops.py` | Generador y comprobación de `river_stops_v1.json` (`af-river-stops/1`, contrato 30). |
| `riverdata.py` | Lectura compartida: rutas, `rio-amazonas.json`, `WorldTable.cs` (tramos de los mundos del cliente), mensajes ASCII y el veto de «Belén». |
| `overrides.json` | Decisiones [P] de P-M1 (a) que `rio-amazonas.json` no lleva en sus campos (hoy, el país de la parada 13). |
| `test_stops.py` | Pruebas del generador: campos, conversión, sincronía con `WorldTable.cs`, determinismo, desfase y fixtures inválidos. |
| `test_schemas.py` | Esquemas del Mapa con `jsonschema`: el esquema, el archivo real y fixtures inválidos. |
| `../../schemas/af-river-stops-1.schema.json` | Esquema de `af-river-stops/1`; con 08 §4.5, la copia única del formato. |

## Uso (desde la raíz del repositorio)

```
py -3.14 tools/river-map/stops.py                    # escribe Assets/_Project/Data/Map/river_stops_v1.json
py -3.14 tools/river-map/stops.py --check            # valida y compara con lo commiteado; código 1 si algo falla
py -3.14 -m unittest discover -s tools/river-map -p "test_*.py"
py -3.14 docs/research/mapa-del-rio/generar_mapa.py --check
```

- `--check` regenera en memoria y falla (código 1, mensajes solo en ASCII) si una regla no se cumple o si el archivo commiteado no es byte a byte lo que se regenera (desfase, BOM o CRLF incluidos). Con errores no se escribe nada.
- `--source`, `--overrides`, `--worldtable` y `--out` cambian las rutas de entrada y salida: los usan las pruebas para los fixtures.
- Determinista: UTF-8 sin BOM, LF, raíz con 2 espacios, una parada por línea y orden de campos fijo. No depende de la semilla de hash ni de la cultura.
- Si cambia `rio-amazonas.json` (M2.7, M3.7), la tarea que regenera (M2.6, M4.5) se fusiona con ella: si no, `--check` falla por desfase.

## `river_stops_v1.json` (`af-river-stops/1`)

Raíz `{format, source, stops[]}` [P]: `format` = `af-river-stops/1`, `source` = la ruta de la fuente y una parada por `n`. Orden de campos de cada parada (08 §4.5 con los extras de P-M1 (a); `at` y `leg` los añade M2.6 y el esquema ya los admite):

`n`, `levels`, `world`, `id`, `kind`, `country`, `country_name`, [`at`], `km_from_sea`, `km_home`, `sample`, `flag_raise`, `arrival_fact`, `facts`, `world_question`, `answers_world_question`, `bg` {`kit`, `hero`, `code`, `lut`}, [`leg`].

Solo claves de texto, nunca textos (08 §4.5): los textos de `arrival_fact`, `facts`, `world_question` y `country_name`, y los nombres por convención (`map.stop.<id>.name`, `map.stop.<id>.river` y `map.country.<iso2>.name`), van en `facts.csv` (contrato 32; los escribe M1.1t con la forma de clave de la pregunta 49 (a): contenido con puntos).

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
- `overrides.json`: solo paradas que existen y campos `country`, `country_name` y `why`.

Fuera de alcance (tareas siguientes): textos y longitudes (M1.1t), `at`, `leg` y `facts` (M2.6), paradas 201-700 (M4.5a y M4.5b) y la geometría de producción (M5.1).
