# tools/level-qa

Control de calidad de los paquetes de niveles de Amazon Fill (Tintalú): GDD 08 §2.4 y §4.1 («Validación en
`tools/level-qa`») y 03 §17. Es una consola .NET 10 que recalcula cada nivel con las mismas reglas que el cliente
(`AF.Core` y `AF.Solver`) y falla el CI si algo no cumple. No usa dependencias NuGet: lee el JSON con `System.Text.Json`.
Comparte con `tools/level-generator` el lector de la especificación de la campaña, las métricas, la forma de los
tableros y la generación (`Campaign/*.cs`, `Metrics/*.cs` y `Generation/*.cs`, enlazados en `level-qa.csproj`, no
copiados).

**Es la compuerta completa en un solo comando** (08 §4.1 «Validación en `tools/level-qa`» y la tabla de 08 §2.4, que le
asignan el esquema y el determinismo por semilla): además de las comprobaciones por nivel, `level-qa`

- valida cada pack contra `schemas/af-level-1.schema.json` lanzando `python tools/validate_schema.py --pack` (QA-SCHEMA;
  la única copia del esquema, y .NET no trae un validador de JSON Schema). Sin Python 3 con `jsonschema`, falla;
- regenera la campaña en memoria con la especificación y comprueba que cada pack sale byte a byte (QA-REPRO, 03 §17 #10).
  D depende del pool de cada rango de 03 §11, así que siempre se regenera el rango completo de la especificación
  (`first_level`-`last_level`: 11-200 en `campaign_v1.json`), aunque se compruebe un solo pack: tarda lo que el generador (en Release con 8 hilos en la máquina del dueño, entre 6 y
  13 minutos según la carga);
- con el pool regenerado, mide la banda de cada nivel (cuantil de D) y la compara con su rol (QA-BAND-Q, 03 §7).

Con `--no-reproduce` salta QA-REPRO y QA-BAND-Q (fixtures negativos, pruebas rápidas) y lo deja como aviso; el CI nunca lo
usa.

**Tinaja (M13; D-019, Q-Ti1 A; propuesta de la tinaja c.5; P3 de la tanda 4).** «Hay ocultas» ya no quiere decir «Aguas
turbias»: la tinaja y su cuia son una sola mecánica (`Ti`, un ejemplar; sus capacidades 5 y 2 no son Cuia alta ni Cuia
chica) y solo cuenta `Tu` con ocultas fuera de la tinaja. Sus controles (QA-TINAJA-*, abajo) repiten lo que el esquema no
puede expresar y los filtros del generador con el mismo código y las mismas semillas (`TinajaWalks.Check`: ruta segura, 20
paseos seguros y 30 casuales, «S0 sin trampas» en el estreno y la compuerta de coste de Morpho con el tope y el factor de P1;
en las variantes de alivio, la ruta y los paseos con `TinajaWalks.ReliefOptions` [P], sin la compuerta). Todas las búsquedas
van con las opciones del nivel: el par y las trampas con `SolverOptions.ForLevel` y la ruta segura de Aguas turbias con
`SafeMoves.ForPipeline(modo, nivel)`, sin poda de fútiles en los niveles con tinaja. Los packs sin tinaja dan el mismo
resultado que antes.

**201-700 y las variantes de la 1.0 (P6 de la tanda 4; 03 §11-§12; 02 §9.1 y §9.2; propuesta de D-007 b.3 y d.5.1).**

- **Rangos desde la especificación y la cabecera, no desde el código.** El rango que se regenera es el de la especificación
  (`first_level`-`last_level`); K, con el `k_min`-`k_max` de la cabecera del pack (QA-KMAX); el tope de mecánicas por nivel
  (QA-MECH-COUNT), con la clave `mechanics_per_level` de la especificación: `{"max": 2, "raised_from_level": 301,
  "raised_milestone_from_level": 320, "raised_max": 3}` dice «2 antes del 300 y 3 después, 3 solo en retos e hitos» (02 §9.2
  regla 4) con los niveles de 03 §12 (fila p20: «2 mecánicas (3 desde el 320)») y de d.5.1 de la propuesta («1-2 en los retos
  (hasta 3 desde el 301 …); 2 en el hito (hasta 3 desde el 320)»): los retos suben desde `raised_from_level` y los hitos desde
  `raised_milestone_from_level` (si falta, desde `raised_from_level`). Sin la clave, el tope es `max` (2) en todos los niveles:
  es el caso de `campaign_v1.json` (11-200), que no la lleva y no cambia. La llevan `sample_variants.json` y
  `sample_tinaja.json`, y la llevará `campaign_v2.json` (P4). Los hitos de K = 13 (QA-HITO-14) salen del `k_plan` (el hito con
  K = 13, desde el 561 en 03 §11; en `campaign_v2.json`, el 620 y el 680, D-027). MAX-2 de `--validate-spec` del generador sigue con «3 desde el 300» en el código hasta P4.
- **Códigos del calendario de las variantes.** `MechanicChecks.Codes` lee cada `attrs[i]` con `MechanicCodes.CodeOf`, el mismo
  código que el generador: la Liana de cuenta es `Ln` y no `Li`, el Brote de dos hojas `B2` y no `Br`, y la Hoja atada `Ha` y
  la Flor `Fl` tienen su código (antes salían como `?`). Así QA-MECH-SPEC, QA-MECH-CELL, QA-MECH-DEBUT, QA-GAP-120, QA-MECH-COUNT
  y el `intro_mechanic` de QA-DEBUT (`liana_n` pide una Liana de cuenta y `brote_n2` un Brote de dos hojas, por el código de la
  tabla de la especificación) las cuentan bien. QA-MECH-KEY ya no pide llave a la Liana de cuenta (lleva `n`, no `key`).
- **Controles por variante (QA-VAR-*, `Checks/VariantChecks.cs`)**, sobre los datos publicados (también si `BoardBuilder` los
  rechaza) y en cada variante de alivio. `LevelValidation` de AF.Core (QA-VALID y QA-RELIEF-VALID) sigue siendo la regla
  completa de cada mecánica; estos controles repiten, con un motivo propio, lo que pide P6.

## Uso

```
dotnet run --project tools/level-qa -c Release -- Assets/_Project/Data/Levels/mundo_02_v1.json Assets/_Project/Data/Levels/mundo_03_v1.json Assets/_Project/Data/Levels/mundo_04_v1.json Assets/_Project/Data/Levels/mundo_05_v1.json --report tools/out/level_qa_report.md
```

Pack de muestra de la tinaja (P1; `pack_schema` 3, niveles 321-340), con su especificación:

```
dotnet run --project tools/level-qa -c Release -- tools/out/sample_tinaja/mundo_07_v1.json --spec tools/level-generator/specs/sample_tinaja.json
```

Opciones: `--report <informe.md>` (Markdown UTF-8 con cada comprobación y sus fallos), `--spec <campaign.json>` (por
defecto `tools/level-generator/specs/campaign_v1.json`; se ejecuta desde la raíz del repositorio), `--levels a-b` (rango
que se regenera; por defecto el de la especificación, el mismo que el `--levels` por defecto del generador), `--variant a|b`
(P7b: la variante de una especificación con respaldos, como el `--variant` del generador; por defecto la publicada,
`published_variant`; con `b`, la salida B de D-019 de `campaign_v2.json`, sin la Tinaja: el calendario de las comprobaciones y
la regeneración de QA-REPRO y QA-BAND-Q salen de ella; una especificación sin variantes solo admite `a`) y `--no-reproduce`.
El informe de `--report` copia en su línea de origen `--spec`, `--levels` y `--variant` si se pasan.

Mundos 7-9 (GEN79, con D-027), en sus dos variantes: desde C1b, la publicada es la salida B (b, sin la Tinaja; d.5.8), en
`Assets/_Project/Data/Levels/`, y la a (con ella) va a `tools/out/salida_a/` (con GEN79 era al revés: la a publicada y la b en
`tools/out/salida_b/`). Cada pack con su rango (`--levels 301-420`, `421-560` y `561-700`); el del mundo 7:

```
dotnet run --project tools/level-qa -c Release -- Assets/_Project/Data/Levels/mundo_07_v1.json --spec tools/level-generator/specs/campaign_v2.json --levels 301-420
dotnet run --project tools/level-qa -c Release -- tools/out/salida_a/mundo_07_v1.json --spec tools/level-generator/specs/campaign_v2.json --levels 301-420 --variant a
```

Resultado de los seis packs (GEN79): 0 fallos, con los avisos conocidos de QA-CUIAS, QA-WINDOW y QA-BAND-Q que anota
`tools/level-generator/README.md` («Mundos 7, 8 y 9»).

Códigos de salida: 0 = todo cumple (los avisos no fallan); 1 = algún nivel no cumple, el esquema falla, el pack no se
reproduce o falta Python; 2 = uso incorrecto, sin paquetes o un archivo ilegible. Sin argumentos sale con 2 (un glob vacío
nunca deja pasar la compuerta). La consola es ASCII.

### `--self-test key2` (P5 de la tanda 4)

```
dotnet run --project tools/level-qa -c Release -- --self-test key2
```

Prueba de este lector (`PackReader`) con las variantes de la 1.0 (firma de `pack_schema` 3, D-023 punto 5): genera el
estreno de la Liana de cuenta, la Hoja atada, el Brote de dos hojas y la Flor de `tools/level-generator/specs/sample_variants.json`
(243, 263, 283 y 303), lo escribe con `PackWriter` y su alivio a un pack temporal y comprueba que `PackReader` da el mismo
tablero, los mismos `attrs` (también la `key2` de la Flor) y el mismo hash que el generador, en el nivel y en cada variante
de relief, y que la Flor sin `key2` no se construye (`VariantsSelfTest.RunPackReadBack`, enlazado del generador). Así, si
`PackReader` dejara de leer la `key2`, falla aquí aunque los controles de las variantes (P6) no lo notaran. Sale con 0 si todo
pasa, 1 si algo falla y 2 si no encuentra la especificación; tarda unos segundos.

### `--self-test reglas` (P6 de la tanda 4)

```
dotnet run --project tools/level-qa -c Release -- --self-test reglas
```

Prueba en positivo lo que los fixtures negativos no pueden probar (`Checks/RulesSelfTest.cs`), sobre tableros escritos en la
prueba y sobre las especificaciones de muestra:

- **QA-MECH-COUNT** (`CampaignSpec.MaxMechanicsAt`): con `sample_variants.json`, (299, reto) → 2, (300, reto) → 2, (300, hito)
  → 2, (301, reto) → 3, (306, reto) → 3, (646, reto) → 3, (307, comodidad) → 2, (319, hito) → 2, (320, hito) → 3, (700, hito)
  → 3 y sin plan → 2; con `campaign_v1.json` (sin `mechanics_per_level`), 2 también en retos e hitos; sin
  `raised_milestone_from_level`, los hitos suben con los retos; `raised_milestone_from_level` sin `raised_from_level` o
  `raised_max` < `max` no se cargan. Si `MaxMechanicsAt` devolviera siempre `max`, o el rol dejara de contar, falla aquí.
- **QA-HITO-14** (`CycleChecks.Hito14Problems`): pasan el hito de K = 13 con 13 cuias llenas y 1 vacía, el del 580 de antes de D-027 (Brote de
  dos hojas con 2 dosis y un Matapi con 2, sin cuia vacía: la holgura va en cuias a medias) y el del 600 (con Barro, que no
  cuenta en la holgura), y no son de esta comprobación un reto ni un hito de 401-560 con K = 12 + 1 vacío + la cueva de la
  Piraña (14 cuias); fallan K = 13 en un reto o en un hito con otro K en el `k_plan`, con una Cuia alta, con la Piraña o con
  otra holgura.
- **QA-VAR-B2** (`VariantChecks.B2Problems`, la misma función que usa el control): el Brote de dos hojas de los hitos del 580
  y del 600 no da motivos, y el del 580 con 3 dosis (espacio 1) da el suyo.
- **QA-VAR-FLOR** (`VariantChecks.FlorProblems` y `CanSealOutside`): las casas de las dos frutas de la Flor se cuentan como
  D-024 (una Hoja atada, un Matapi con dosis o la Lluvia solo son casa de su fruta; la Liana, otra Flor, la Tinaja y su cuia,
  de ninguna), con un caso que pasa y otro que falla para cada una, y la fruta de U_c = 5 cuya única cuia de 5 es la Flor.
- **La Tinaja (D-027)** (`TinajaChecks.Structure` y `BaseChecks.TinajaDebutParExempt`): un nivel de 12 cuias con la tinaja
  pasa QA-TINAJA-CUIAS y uno de 13 no; con el tope del alivio, 13 pasa y 14 no; QA-PAR exime del par del rango el 323, el 324 y el
  325 de `campaign_v2.json` (estreno con la tinaja y K ≤ 6) y no la práctica 327, el estreno con K = 7, el estreno del Brote de
  dos hojas (343, sin la tinaja) ni el 324 de la variante b (C).

Sale con 0 si todo pasa, 1 si algo falla y 2 si no encuentra las especificaciones; tarda menos de un segundo.

### `--self-test variante` (P7b de la tanda 4)

```
dotnet run --project tools/level-qa -c Release -- --self-test variante
```

Prueba la opción `--variant` por la línea de comandos (`Checks/VariantOptionSelfTest.cs`): lanza este mismo level-qa en un
proceso hijo, con `--no-reproduce` y `--spec tools/level-generator/specs/campaign_v2.json`, sobre un pack temporal con dos
niveles del pack de muestra de la tinaja (`tools/out/sample_tinaja/mundo_07_v1.json`): su 323 tal cual (estreno de la tinaja;
la celda es ★Ti en la a y Ma, su respaldo de d.5.4, en la b) y su 328, una comodidad pura, con el número 327 (Ti en la a y C en
la b). Solo mira las comprobaciones de las mecánicas del calendario, QA-MECH-SPEC y QA-MECH-CELL (las demás fallan por ser
dos niveles sueltos):

- con `--variant a`: el 323 pasa y el 327 da QA-MECH-CELL (la tinaja de su celda no está en el nivel);
- con `--variant b`, y sin `--variant` (la publicada de `campaign_v2.json` es la b desde C1b), al revés: el 327 pasa y el 323 da QA-MECH-SPEC (Ti fuera de la celda Ma) y QA-MECH-CELL (falta el
  Matapi). Si `--variant` dejara de llegar a la especificación, la b daría lo mismo que la a y fallaría aquí;
- `--variant c` con `campaign_v2.json` y `--variant b` con `campaign_v1.json` (sin variantes) salen con 2 y su motivo.

Sale con 0 si todo pasa, 1 si algo falla y 2 si falta el pack de muestra o la especificación; tarda alrededor de un minuto
(tres pasadas de la compuerta sin reproducción, con el bot y los filtros de la tinaja del 323).

Packs de muestra de las variantes (P5; `pack_schema` 3, niveles 241-320), con su especificación (P6: pasan con 0 fallos):

```
dotnet run --project tools/level-qa -c Release -- tools/out/sample_variants/mundo_06_v1.json tools/out/sample_variants/mundo_07_v1.json --spec tools/level-generator/specs/sample_variants.json
```

## Reto del río: `--daily` (R4 del encargo del Spark; 03 §16-§17)

QA del pool del Reto del río (`daily_challenge`; nota de diseño `docs/dev/reto-del-rio.md` §13), en `Daily/` (`DailyQaCommand.cs`
y `DailyChecks.cs`). Las reglas de cada banda (K por día, vacíos, capacidad, ventana del bot, paleta, tope de pares y agua del
mundo del día) salen de la especificación del generador, `tools/level-generator/specs/daily_v1.json`, nunca de la cabecera
del pack. El lector de la especificación, los ids y las semillas y la generación son **el mismo código del generador**:
`level-qa.csproj` enlaza `tools/level-generator/Daily/*.cs` salvo `DailyCommand*.cs` (la línea de comandos del generador),
igual que `Campaign/`, `Metrics/` y `Generation/`.

```
dotnet run --project tools/level-qa -c Release -- --daily Assets/_Project/Data/Daily/daily_pool_v1.json
dotnet run --project tools/level-qa -c Release -- --daily tools/out/daily_sample/daily_pool_v1.json --partial
```

Opciones: `--spec <daily_v1.json>` (por defecto la de arriba; los tintes y los pares de riesgo, de su `tints_from`),
`--partial` (una muestra: los días que faltan no fallan, pero sale siempre el aviso QA-DAILY-DAYS «no es el pool completo»;
el tamaño se mira igual) y `--no-reproduce` (salta QA-DAILY-REPRO y lo deja como aviso; nunca en el CI). Sin `--partial`, el
pack tiene que traer las 3 bandas × 729 días (R-Q2b).

- **QA-DAILY-REPRO** (03 §17 #10) regenera **en memoria**, sin proceso hijo, las bandas y los días del pack con
  `DailyRunner` y `DailyPackWriter` y compara cada tablero entero con el regenerado (igualdad profunda del JSON: el mismo
  tablero, no solo el mismo hash). Si el generador no puede dar un día (ninguno en la ventana del bot, R-Q11 (a)) o rechaza su
  tablero (`DailyPackWriter.CheckBoard`), también es un fallo de QA-DAILY-REPRO. El pool publicado (2.187 tableros) tarda
  unos 25 s en Release.
- **QA-DAILY-SCHEMA** lanza `python tools/validate_schema.py --pack`, como QA-SCHEMA: sin Python 3 con `jsonschema`, falla.
- La anti-repetición (03 §17 #9) y las trampas tempranas (03 §17 #3) solo dan **aviso** (R-Q10): el pool publicado da 0
  fallos y unos 1.700 avisos de esas dos comprobaciones.

| Id | Qué comprueba |
|---|---|
| QA-DAILY-SCHEMA | el esquema `af-level/1` (03 §17 #11) |
| QA-DAILY-HEADER | `kind` daily y `pack_id` de la especificación; paletas en el pool y K entre `k_min` y `k_max` |
| QA-DAILY-STRUCT | tablero legible y válido (`BoardBuilder` y `LevelValidation` de AF.Core) |
| QA-DAILY-SLOT | `slot {band 1-3, day 0-728}` coherente con el `id` `d{b}_n{nnnn}` y la `seed` `amazonfill:v1:daily:b{b}:d{ddd}` |
| QA-DAILY-DAYS | 729 días por banda, sin huecos ni duplicados |
| QA-DAILY-SHAPE | K del día (`k_by_day`), vacíos de la banda, cuias de 4 llenas o vacías |
| QA-DAILY-CUIAS-14 | como mucho 14 cuias (02 R1) |
| QA-DAILY-PALETTE | paleta = tintes presentes, con debut dentro del tramo de la banda (R-Q1) |
| QA-DAILY-RISK | `risk_pairs` recalculado y dentro del tope de la banda (B3 libre: R-Q1b (a)) |
| QA-DAILY-MECH | sin mecánicas ni ocultos (03 §16) |
| QA-DAILY-FIELDS | sin `level`, `difficulty`, `solution`, `safe_path` ni `qa` |
| QA-DAILY-RELIEF | `relief` vacío |
| QA-DAILY-WATER | mundo y `water_base` del día (R-Q2) |
| QA-DAILY-ASTAR | resoluble con A* (03 §17 #1) |
| QA-DAILY-PAR | par ≥ 2K (03 §17 #2) |
| QA-DAILY-BOT | `bot_fail` ×40 con la semilla del tablero dentro de la ventana de la banda: compuerta (R-Q11 (a)) |
| QA-DAILY-REPRO | la semilla reproduce el tablero (arriba) |
| QA-DAILY-SIZE | pack < 2 MB sin comprimir, también con `--partial` |
| QA-DAILY-REPEAT-SHAPE | aviso: forma distinta de la de los 200 días anteriores de su banda, con la vuelta del ciclo |
| QA-DAILY-REPEAT-HAMMING | aviso: Hamming ≥ 25 % con los 20 días anteriores de su banda |
| QA-DAILY-EARLY-TRAPS | aviso: trampas tempranas dentro del tope de la etiqueta que da el bot |

Códigos de salida: 0 = sin fallos (los avisos no fallan); 1 = algún fallo; 2 = uso, especificación o pack ilegibles.

### `--self-test daily`

```
dotnet run --project tools/level-qa -c Release -- --self-test daily
```

Pasa el fixture válido de `fixtures/daily/` (`daily_valido.json`, con `--partial`, reproducción y esquema: 0 fallos y el aviso
de muestra parcial; sin `--partial`, QA-DAILY-DAYS) y cada fixture malo (`QA-DAILY-<id>__<caso>.json`), que tiene que fallar con
su id y solo con las comprobaciones que su cambio arrastra; además, el tope de tamaño, B3 sin tope de pares, y que las
trampas tempranas, la anti-repetición (también con dos días casi iguales y con la vuelta del ciclo, 728 → 0) solo avisan.
Sale con 0 si todo pasa, 1 si algo falla y 2 si no encuentra la especificación; tarda unos 20 s.

**`--self-test` sin nombre** ejecuta `key2` y `daily` y sale con el código mayor de los dos. `reglas` y `variante` se lanzan
por su nombre.

## Comprobaciones

Cada comprobación tiene un id estable; la consola imprime `FAIL [ID] nivel N: motivo`.

| Id | Qué comprueba | Fuente |
|---|---|---|
| QA-STRUCT | caps, cuias y attrs de la misma longitud; ≤ 14 cuias generadas; niveles sin repetir | 02 R1; 08 §4.1 |
| QA-UNITS | U_c coherente con `units_per_color` y con las capacidades | 02 R3, §8; 03 §17 #5 |
| QA-SEALED | ninguna cuia sellada al inicio | 03 §17 #5 |
| QA-STUCK | `atascado(S0)` = false | 02 §9.2 regla 1 |
| QA-VALID | el resto de `LevelValidation` (mecánicas, Barro, Aguas turbias) | 03 §17 #5-#7 |
| QA-PAR | par recalculado (A* w = 1, `run`) igual al publicado; par ≥ 2K; par objetivo del rango ±30 %, salvo en los tableros de estreno de la Tinaja (★ con la tinaja y K ≤ 6: el 323-325), exentos del par del rango por su K (decisión del dueño del 2026-10-18, D-027; `BaseChecks.TinajaDebutParExempt`) | 03 §17 #1-#2; D-027 |
| QA-TINT-DEBUT | cada tinte con debut anterior o igual al nivel | 03 §17 #4 |
| QA-KMAX | K entre `k_min` y `k_max` de la cabecera | 03 §17 #4 |
| QA-RISK | `risk_pairs` recalculado y ≤ `max_risk_pairs` | 03 §4.5 |
| QA-PALETTE | `palette` = tintes presentes en `cuias` | 08 §4.1 |
| QA-BOT | `bot_fail` recalculado con la semilla del nivel, idéntico | 03 §6 |
| QA-LABEL | etiqueta coherente (≥ 0,75 hard y ≥ 0,90 very_hard, desde el 40 y el 60) | 03 §7, §11 |
| QA-LABEL-EARLY | ningún nivel mide ≥ 0,90 antes del 60 ni ≥ 0,75 antes del 40 (aunque se publique con la etiqueta inferior) | contrato 6; 03 §11-§12 |
| QA-CUIAS | aviso: cuias del nivel fuera de las cuias totales del rango | 03 §11 |
| QA-FIELDS | sin `solution`, `safe_path` ni `qa` | 08 §4.1 |
| QA-SIZE | ≤ 100 KB por 100 niveles sin `relief` | 03 §17 #11 |
| QA-EARLY-TRAPS | trampas tempranas dentro del tope: 0 en Normal, 1 en Difícil, sin tope en Muy difícil | D-007 Q16 B |
| QA-BAND | `difficulty.band` = rol de la posición en el ciclo (remanso = comfort) | 03 §12; D-009 |
| QA-LABEL-SLOT | etiquetas solo donde se permiten | D-009; c.3 regla 7 |
| QA-LABEL-ADJ | nunca dos niveles con etiqueta seguidos | c.3 regla 1 |
| QA-AFTER-LABEL | tras una etiqueta, el siguiente con D menor (condición necesaria de «bajar una banda») | c.3 regla 2 |
| QA-LADDER | escalera de D (61+ y 41-59); en 21-40, los retos suaves por debajo del hito 40 | c.3 regla 4; 03 §7 |
| QA-REMANSO | p19 puro y más fácil que la media de comodidad: D(p19) < media de D de las comodidades del ciclo que no son estreno | c.4; c.3 regla 5 |
| QA-RAMP | filas 11 y 12: 8 cuias = 6 colores + 2 vacíos, par 15-19 y 16-20, `bot_fail` 0-10 % | 02 §13 |
| QA-PURE-30 | ≥ 30 % de niveles puros por ciclo completo | d.2 regla 1 |
| QA-WINDOW | `bot_fail` en la ventana del slot: falla en recuperación, comodidad, estreno (< 0,20), remanso y rampa; un reto o el hito fuera de su ventana es aviso (se publica con la etiqueta que mide) | 03 §17 #3; 08 §4.1; c.2 |
| QA-DEBUT | estrenos con 2 vacíos sin contar la tinaja ni su cuia (también el de una capacidad fuera de la rampa y del hito: la Cuia chica de 3 en el 22) y un ejemplar (la tinaja con su cuia es uno); `intro_mechanic` y `skippable` = false solo en el nivel de intro | 02 §9.2 regla 2; 02 §8; propuesta de la tinaja c.5 |
| QA-LABEL-ACCURACY | aviso: acierto de etiqueta por ciclo < 60 % | c.7 |
| QA-HITO-14 | K = 13 solo en un hito (p20) cuyo K del `k_plan` de la especificación es 13 (desde el 561; en `campaign_v2.json`, el 620 y el 680, D-027 punto 3); ese hito lleva 14 cuias de 4 con 13 colores + 1 vacío, sin capacidades, sin la cueva de la Piraña (sería la cuia 15) y sin la Tinaja (con ella, 12 cuias como mucho). El vacío es la holgura de la aritmética del nivel, no una cuia vacía literal [P]: Σ caps − Σ unidades = 4, sin el Barro y con las gotas pendientes de la Lluvia, así que puede ir repartido en cuias a medias (antes de D-027, el Brote de dos hojas de los hitos de K = 13 del 580, el 600 y el 700, d.5.2; con D-027 solo lo hace la relajación del hito del 680 en las etapas 1-2 del generador, y su tablero publicado lleva la cuia vacía). Las 14 cuias con K ≤ 12 no son de esta comprobación: K + 1 vacío + la cueva de la Piraña (un reto o un hito de 401-560, o la firma 920 de d.5.7) y los hitos de K = 12 + 1 vacío + la cuia a medias de su mecánica (el 580, el 600, el 640, el 660 y el 700, D-027 punto 3); más de 14 cuias, QA-STRUCT | 03 §1, §11-§12; d.5.1-d.5.2 [P]; D-027; 02 R1 y §5 |
| QA-RELIEF-MIN | ≥ 1 variante desde el 31 | 03 §13 |
| QA-RELIEF-VALID | variantes válidas; con tinaja, también su forma (una, su cuia contigua y en la misma fila, ≤ 13 cuias, T4 y combinaciones): el tope del nivel (12) más la cuia vacía de `plus_empty` (D-027; `Superposition.TinajaReliefMaxCuias`, el mismo del generador) | 03 §13; 02 §9.1 M13; D-027 |
| QA-RELIEF-MINUS | `minus_color` resoluble con el par entero publicado y sin quitar una fruta de la tinaja | 03 §13; propuesta de la tinaja c.3 punto 4 |
| QA-RELIEF-PLUS | `plus_empty` = el base con exactamente un vacío de 4 más: al final o, con tinaja, en la ranura que deja la pareja en la misma fila | 03 §13 |
| QA-RELIEF-BOT | `bot_fail` de la variante recalculado; baja ≥ 0,20 (base < 0,20: par menor o vacío extra) | 03 §17 #8 |
| QA-RELIEF-MASK | la variante de un nivel con tinaja la conserva y su `hidden` es la máscara (todas sus capas menos la boca); una variante sin tinaja no lleva `hidden` | 08 §4.1 (decisión del 2026-10-14, opción a de P2a) |
| QA-RELIEF-SAFE | variante con tinaja: ruta segura y paseos seguros y casuales (opciones de Morpho [P], pendiente de decisión; semilla del nivel + `/` + tipo) | 03 §13; propuesta de la tinaja c.3 punto 4 |
| QA-REPEAT-SHAPE | forma distinta de los 200 niveles anteriores | 03 §17 #9 |
| QA-REPEAT-HAMMING | Hamming ≥ 25 % con los 20 anteriores | 03 §17 #9 |
| QA-MECH-DEBUT | mecánicas y capacidades con debut anterior o igual al nivel | D-007; 02 §8 |
| QA-MECH-SPEC | mecánicas permitidas por la celda del calendario | especificación |
| QA-MECH-CELL | cada mecánica y capacidad de la celda está en el nivel; sin turbias por el respaldo de T1, aviso | especificación; 02 §8 |
| QA-GAP-120 | ninguna mecánica ni capacidad (Cuia chica, Cuia alta) pasa más de 120 niveles sin volver, sobre lo publicado; solo cuenta un hueco con todos sus niveles en los paquetes | resumen 6; d.2 regla 4 |
| QA-MECH-KEY | llave presente (Hoja y Liana con fruta; la Hoja atada y la Flor, en QA-VAR-HA y QA-VAR-FLOR); agua del mundo en la Lluvia; llave del tutorial en el estreno | 02 §9.1; D-007 |
| QA-MECH-COUNT | mecánicas distintas por nivel dentro del tope de `mechanics_per_level` de la especificación: 2 antes del 300 y 3 después, 3 solo en retos (desde el 301) y en hitos (desde el 320) (la tinaja con su cuia cuenta como una; las capacidades no cuentan [P]) | 02 §9.2 regla 4; 03 §12; D-009 |
| QA-VAR-HA | Hoja atada: su fruta llave en el tablero y el nivel no empieza en Aguas estancadas | propuesta de D-007 b.3; 03 §17 #6; 02 §9.2 regla 1 |
| QA-VAR-B2 | Brote de dos hojas: espacio ≥ 2 al empezar, con 1-2 dosis («empieza con 2 dosis como mucho») | b.3; 02 R10 b |
| QA-VAR-FLOR | Flor de vitória-régia: `key` ≠ `key2` (c1 ≠ c2), las dos frutas en el tablero, ninguna dosis suya dentro de la flor y casa fuera de ella para las dos a la vez (una cuia con cap = U_c de cada una, distintas): una Hoja atada, un Matapi con dosis o la Lluvia solo son casa de su fruta, y la Liana, otra Flor, la Tinaja y su cuia, de ninguna (AF.Core solo cuenta las capacidades) | b.3; D-023 punto 5; D-024 |
| QA-VAR-LN | Liana de cuenta: `n` de 1 a 2 | 02 §9.1 M3; b.3 |
| QA-VAR-DEBUT | tablero de estreno de una variante (Ln, Ha, B2, Fl): sola, sin otra mecánica ni capacidad, y un ejemplar (los 2 vacíos y `bot_fail` < 0,20, QA-DEBUT y QA-WINDOW) | 02 §9.2 regla 2 |
| QA-MURKY-FLAG | `murky` coherente con `hidden` (Aguas turbias o tinaja) | 08 §4.1 |
| QA-MURKY-T1 | Aguas turbias solo desde el 100 y en Difícil o Muy difícil; la tinaja queda fuera (excepción de D-019) | 02 T1; D-019, Q-Ti1 A |
| QA-MURKY-LIMITS | ≤ 3 cuias y ≤ 4 dosis ocultas; T4 | 02 T2-T4 |
| QA-MURKY-SAFE | ruta segura en cada estado | 03 §8; 02 T5 |
| QA-MURKY-COMBO | sin Lluvia, Piraña, Liana ni Remolino | Q14 A |
| QA-MURKY-Q9 | estreno del 100: una cuia, 2 dosis ocultas, sin otra mecánica | Q9 |
| QA-TINAJA-TIER | dosis de la tinaja por tipo de nivel: estreno, comodidad y práctica 3 (2 ocultas); retos 4-5 (3-4); hito 5 (4) (`Superposition.TinajaDoses`) | D-019, Q-Ti1 A; 02 §9.1 M13 |
| QA-TINAJA-ONE | una sola tinaja y una sola cuia suya | 02 §9.1 M13 regla 1 |
| QA-TINAJA-PAIR | su cuia en la ranura contigua y en la misma fila con cualquiera de las filas del cliente (`Superposition.SameRow`) | 02 §9.1 M13 regla 1; D-006 |
| QA-TINAJA-CUIAS | como mucho 12 cuias con la tinaja en el nivel, contando la pareja (en sus variantes de alivio, 13: QA-RELIEF-VALID) | 02 §5 [P]; D-027 |
| QA-TINAJA-MASK | `hidden` = todas las capas de la tinaja menos la boca, y nada oculto fuera de ella (sin Aguas turbias) | 02 §9.1 M13 regla 1; propuesta de la tinaja c.4 |
| QA-TINAJA-T4 | T4 en toda la columna de la tinaja | 02 §9.1 M13 regla 1 |
| QA-TINAJA-COMBO | sin Liana, Remolino, Lluvia ni Piraña, y sin Barro dentro de la tinaja o de su cuia (fuera de la pareja, sí) | 02 §9.1 M13, combinaciones |
| QA-TINAJA-SLOT | ninguna tinaja en p1, p2, p19 ni en la rampa | D-019, Q-Ti1 A |
| QA-TINAJA-DEBUT | tablero de estreno con tinaja: sola y un ejemplar, 2 vacíos sin contar la pareja, `bot_fail` < 0,20 y S0 sin trampas (todo vertido útil de S0 es seguro) | 02 §9.2 regla 2; propuesta de la tinaja c.3 y d.3 |
| QA-TINAJA-SAFE | ruta segura, 20 paseos seguros y 30 casuales [P] (`TinajaWalks.Check`, la misma semilla que el generador); el motivo separa «sin vertido seguro» de «presupuesto agotado» (también en los paseos casuales desde FIXG: un `SafeMoves` sin presupuesto es «presupuesto agotado», sin consultar la realidad verdadera, nunca «adivinar») | 03 §8; D-019, Q-Ti1 A |
| QA-TINAJA-MORPHO | coste de Morpho (w = 2, 40.000 nodos por realidad) en S0, la ruta y los paseos: tope de nodos = 150 ms × nodos/ms del PC ÷ factor del teléfono (8× [P] hasta PF1), el mismo que P1 | propuesta de la tinaja c.3 punto 3; 08 §9 |
| QA-SCHEMA | JSON válido contra `schemas/af-level-1.schema.json` (`python tools/validate_schema.py --pack`); sin Python o sin `jsonschema`, falla | 08 §4.1; 03 §17 #11 |
| QA-REPRO | la especificación y las semillas reproducen cada pack byte a byte (campaña regenerada en memoria) | 03 §17 #10; 08 §2.4 y §4.1 |
| QA-BAND-Q | banda medida (cuantil de D en el pool regenerado) acorde al rol: comodidad, estreno, remanso y rampa ≤ 0,70; recuperación ≤ 0,10 o, como mínimo, D por debajo de la comodidad más fácil del ciclo; hito > 0,95 o, como mínimo, par − 2K ≥ la media del pool del rango. Cumplir solo el mínimo es aviso. Un pack que no se reproduce se mide con su D publicada (aproximado) | 03 §7; c.3 reglas 3 y 5 |

Reproducibilidad (03 §17 #10): la comprueba `level-qa` (QA-REPRO). `dotnet run --project tools/level-generator -c Release
-- --spec … --check` hace la misma comparación contra los packs del disco, sin las demás comprobaciones.

## Fixtures negativos: `fixtures/*.json`

Cada fixture es un paquete con un defecto. El nombre empieza por el id de la comprobación que debe fallar
(`QA-PAR__par_distinto.json`). Para pasarlos todos (Git Bash):

```
for f in tools/level-qa/fixtures/QA-*.json; do
  id=$(basename "$f" | sed 's/__.*//')
  case "$f" in
    *__tinaja_v2_*) spec="--spec tools/level-generator/specs/campaign_v2.json";;
    *__tinaja_*) spec="--spec tools/level-generator/specs/sample_tinaja.json";;
    *__variante_*) spec="--spec tools/level-generator/specs/sample_variants.json";;
    *__hito14_*) spec="--spec tools/level-qa/fixtures/specs/hito14_k13.json";;
    *) spec="";;
  esac
  case "$f" in
    *__variante_flor_casas_ocupadas*) absent="QA-MECH-COUNT QA-VALID";;
    *) absent="";;
  esac
  out=$(dotnet run --project tools/level-qa -c Release --no-build -- "$f" --no-reproduce $spec); code=$?
  extra=""; for a in $absent; do echo "$out" | grep -q "\[$a\]" && extra="$extra $a-PRESENTE"; done
  echo "$(basename "$f"): exit=$code $(echo "$out" | grep -q "\[$id\]" && echo presente || echo AUSENTE)$extra"
done
```

Todos deben dar `exit=1` y `presente`, sin nada detrás: `flor_casas_ocupadas` comprueba además que no salgan QA-MECH-COUNT
(es un reto del 306 con tres mecánicas, que desde el 301 valen en los retos) ni QA-VALID (AF.Core lo da por válido). Los de QA-RAMP (11), QA-LABEL-EARLY (58), QA-MECH-CELL (160) y QA-REMANSO
(108, 110 y 119, `remanso_mas_dificil_que_la_comodidad`) son niveles de los packs anteriores a la revisión del
2026-10-09. QA-WINDOW tiene dos: un estreno con `bot_fail` 0,25 y una comodidad (p8 del 48) con el tablero de recuperación
del 42 de esos packs, que el bot no falla nunca (`comodidad_bajo_la_ventana`). QA-GAP-120 no tiene fixture (necesita más de
120 niveles seguidos): se comprueba con esos packs anteriores, en los que falla («Cuia alta no vuelve desde el 78»).
QA-REPRO y QA-BAND-Q tampoco (necesitan la campaña completa): con los packs anteriores a esta revisión de la colocación
(2026-10-09) y el generador actual, `level-qa` sale con 1 y 20 fallos: los cuatro packs no se reproducen (QA-REPRO);
QA-BAND-Q en las recuperaciones 21, 22, 61, 81, 122, 141 y 161, las comodidades 24, 30, 67 y 168 y los hitos 100 y 180
(q 0,85 con par − 2K = 0); QA-LADDER en el reto suave 26 (D 1,26 por encima del hito 40), y QA-DEBUT y QA-WINDOW en el 22
(un vacío libre y `bot_fail` 0,275 en el estreno de la Cuia chica de 3). `fixtures/schema/` son los fixtures de
`validate_schema.py --pack` (uno válido y uno inválido); cualquier fixture con un JSON fuera del esquema daría además
QA-SCHEMA.

Los de la tinaja (`QA-*__tinaja_*.json`) son niveles 321-340 y se pasan con la
especificación de la muestra de P1 (`specs/sample_tinaja.json`, la única con la tinaja en el calendario hasta P4), salvo los
`QA-*__tinaja_v2_*.json`, que se pasan con `campaign_v2.json` (su par objetivo es el de 201-400).

Los dos de D-027 (decisión del dueño del 2026-10-18): `QA-RELIEF-VALID__tinaja_alivio_de_14_cuias` es el reto 329 de la muestra
(11 cuias) con su `plus_empty` llevado a 14 cuias (dos vacíos más al final): pasa del tope de 13 del alivio con la tinaja; da
además QA-RELIEF-PLUS (no es el base con un solo vacío más). El `plus_empty` de 13 cuias de un nivel de 12 pasa (los niveles
con tinaja de 12 cuias del mundo 7 y `--self-test reglas`). `QA-PAR__tinaja_v2_par_corto_fuera_del_estreno` es el estreno 324 de
la muestra (K = 6 y par 13) con el número de la práctica 327 (Ti, sin ★): fuera del estreno, el par del rango 201-400 (22-36
±30 %, 15,4 como mínimo) vuelve a exigirse y falla; da además QA-WINDOW (`bot_fail` 0 en una comodidad). El mismo tablero como
324 pasa QA-PAR (`--self-test reglas` prueba la regla con el 323-325, el 327, K = 7, el estreno sin tinaja y la variante b). Salen del
pack de muestra con un defecto (QA-TINAJA-ONE, PAIR, CUIAS, MASK, T4, COMBO, SLOT en p1 y en p19, TIER, QA-RELIEF-MASK y
QA-RELIEF-MINUS) o de tableros que rechazan los filtros del generador: QA-TINAJA-DEBUT
(`tinaja_estreno_s0_sin_seguridad_demostrada`; antes de FIXG, `tinaja_estreno_con_trampa_en_s0`) es un estreno con 10 vertidos
útiles de S0 que no son seguros, ninguno demostrado inseguro: tras cada uno, A* no deja ninguna realidad de S0 demostrada sin
solución con los 20.000 nodos por realidad, así que el motivo dice «0 demostrado(s) inseguro(s) (trampa en S0); 10 sin
seguridad demostrada (presupuesto agotado)», y QA-TINAJA-SAFE no repite ese rechazo de S0 (el generador lo clasifica como
«presupuesto agotado»; lo fija `--self-test tinaja`). La trampa demostrada no tiene fixture: la fija el tablero perdido de b.2
en `--self-test tinaja`; QA-TINAJA-SAFE (`tinaja_presupuesto_agotado`), un candidato del reto 338 de la muestra que el
generador rechazó (el «presupuesto agotado: 1» del 338 en el informe de la muestra; antes de FIXG, «adivinar: 1»): el paseo casual 2 llega tras 1 vertido a un estado en el que
`SafeMoves` agota sus 20.000 nodos por realidad (4 realidades) y la realidad verdadera es resoluble. Es «presupuesto
agotado», no «adivinar»: desde FIXG el motivo acaba en `[presupuesto agotado]` (antes, en `[adivinar]`), con el mismo id.
El informe de la muestra no cuenta ningún otro candidato rechazado por «adivinar», así que el «adivinar» de verdad
(`SafeMovesOutcome.SomeRealitySolvable` con la realidad verdadera resoluble) no tiene fixture: lo fija el test de
clasificación de FIXG; QA-TINAJA-MORPHO, un hito de 12 cuias con 5 dosis en el que Morpho expande 66.496 nodos en S0
(tope 18.750), y QA-RELIEF-SAFE, una variante `minus_color` válida para AF.Core y sin Aguas estancadas al empezar en la que
ninguna realidad tiene solución (con las opciones de alivio, los tableros de prueba que se ganan en la realidad verdadera
siempre tuvieron ruta segura; por eso da también QA-RELIEF-MINUS).

Los de las variantes y de 201-700 (P6; `QA-*__variante_*.json`, niveles 241-320) se pasan con la especificación de la muestra
de P5 (`specs/sample_variants.json`) y salen de sus packs con un defecto; además del suyo, dan los fallos que se citan:
QA-VAR-HA (`hoja_atada_sin_su_fruta`: la llave del 263 pasa a Arazá, que no está en el tablero; también QA-VALID y
QA-RELIEF-VALID, y QA-VAR-HA en las dos variantes de alivio; `hoja_atada_atascada`: cinco cuias llenas en cuadrado latino con la
Hoja atada, que empieza en Aguas estancadas; también QA-STUCK), QA-VAR-B2 (`brote_dos_hojas_sin_espacio`: el Brote de dos hojas
del 283 con 3 dosis; también QA-VALID), QA-VAR-FLOR (`flor_llaves_iguales`: key2 = key en el 303, que `BoardBuilder` y el
esquema rechazan, QA-STRUCT y QA-SCHEMA; `flor_con_su_fruta_dentro`: una dosis de su key2 dentro de la Flor; también QA-VALID;
`flor_llave_ausente`: la key2 del 303 pasa a Guanábana (1), que no está en el tablero, también en las dos variantes de alivio;
también QA-VALID, QA-RELIEF-VALID, QA-PAR y QA-BOT; `flor_sin_casa_fuera`: en la comodidad 311 la Flor es una cuia de 5 y su
llave tiene U_c = 5 (una dosis más en un vacío), así que la única cuia de 5 es la propia Flor; también QA-VALID, QA-PAR y QA-BOT;
`flor_casas_ocupadas`: el reto 306 rehecho con la Flor, una Hoja atada por cada otra fruta y un Matapi con dosis de la 3, con
una sola cuia libre fuera de la Flor para sus dos frutas: AF.Core lo da por válido, porque solo cuenta capacidades, y A* prueba
que no tiene solución; también QA-PAR, QA-BOT, QA-MECH-SPEC y QA-MECH-CELL, y nunca QA-MECH-COUNT ni QA-VALID),
QA-VAR-LN (`liana_de_cuenta_n3`: n = 3 en el 243; también QA-STRUCT), QA-VAR-DEBUT (`estreno_con_dos_ejemplares`: dos Lianas de
cuenta en el 243; `estreno_con_otra_mecanica`: el estreno de la Hoja atada con una Liana de cuenta; los dos, también QA-DEBUT)
y QA-MECH-COUNT (`tres_mecanicas_antes_del_300`: el reto 286 con una tercera mecánica; `tres_mecanicas_en_comodidad`: la
comodidad 307 con tres, que solo valen en los retos desde el 301 y en los hitos desde el 320; los dos, también QA-MECH-SPEC). QA-HITO-14 tiene dos:
`variante_k13_fuera_de_su_hito`, un hito del 300 con 14 cuias (K = 13 + 1 vacío) cuando el `k_plan` de la muestra lleva K = 9
en el hito, y `hito14_con_cuia_alta`, el mismo hito con una Cuia alta de 5, que se pasa con `fixtures/specs/hito14_k13.json`
(la muestra con K = 13 en los hitos; solo para este fixture, no se genera): el hito de 14 cuias está planificado y solo falla
su forma. Los tableros rehechos a mano (`hoja_atada_atascada`, `brote_dos_hojas_sin_espacio`, `flor_con_su_fruta_dentro`,
`flor_sin_casa_fuera`, `flor_casas_ocupadas` y los dos de QA-HITO-14) no llevan variantes de alivio, así que dan además
QA-RELIEF-MIN y QA-SCHEMA. Lo que estos fixtures no pueden probar en positivo (un reto o un hito con tres mecánicas desde su
nivel, el hito de K = 13 con el vacío repartido en cuias a medias, las 14 cuias con K = 12 y la cueva, y cada clase de casa de
la Flor) lo prueba `--self-test reglas`.
