# tools/level-generator

Generador offline de niveles de Amazon Fill (Tintalú): GDD 08 §2.4 y GDD 03. Es una consola .NET 10 que usa las
mismas reglas que el cliente (`AF.Core` y `AF.Solver`, del paquete `Packages/com.amazonfill.core`): aquí no se
copia ni se reimplementa ninguna regla del juego (`.claude/rules/core.md`).

La consola imprime ASCII (`.claude/rules/ci.md`); los informes Markdown son UTF-8 y no llevan tiempos, para que dos
ejecuciones den los mismos bytes.

## Modos

| Modo | Qué hace | Salida |
|---|---|---|
| `--handmade <spec> --out <carpeta> --report <md> [--check]` | Tutorial 1-10 hecho a mano (02 §13) | `Assets/_Project/Data/Levels/tutorial_v0.json` y `tools/out/tutorial_v0_report.md` |
| `--validate-spec <campaign.json>` | Comprueba la especificación de la campaña (11-200 y 201-700, esta en sus dos variantes) | Resumen ASCII por ciclo |
| `--registry <campaign.json> --out <registro.json> [--variant a\|b] [--check]` | Registro de combinaciones publicadas (d.5.6; P4) | `tools/level-generator/specs/combinations_registry_v1.json` |
| `--parity [--report <md>]` | Paridad de las métricas con el prototipo (03 §17-§18; 09 R26) | `tools/out/parity_report.md` |
| `--spec <campaign.json> [--levels a-b] [--variant a\|b] --out <carpeta> [--report <md>] [--curation <md>] [--check]` | Genera la campaña (03 §2); sin `--levels`, todos los niveles de la especificación; `--curation`, el informe de curación para el dueño (P7a) | `Assets/_Project/Data/Levels/mundo_0N_v1.json`, `tools/out/campaign_v1_report.md` y `tools/out/curation_wNN.md` |
| `--seasons <campaign.json> [...] --out <tabla.json> [--until <nivel>] [--check]` | Tabla de temporada del cliente (pregunta 32 b), hasta el último nivel publicado | `Assets/_Project/Data/Levels/seasons_v1.json` |
| `--self-test [tinaja\|seasons\|variants\|registry]` | Pruebas de la herramienta sin proyecto de tests (sin nombre, todas) | PASS / FAIL por consola |

Códigos de salida: 0 = correcto; 1 = alguna comprobación falla; 2 = uso incorrecto o especificación ilegible.

## Especificación de la campaña: `specs/campaign_v1.json`

Datos versionados, no reglas. Los leen el generador y `tools/level-qa` (un solo lector, `Campaign/CampaignSpec.cs`,
que `level-qa` enlaza). Fuentes:

- **D-009** (2026-10-08; Q1 A, Q2 sí, Q3 A, Q4 A): ciclo de 20 con retos en p6, p9, p12, p15 y p18, remanso puro en
  p19 e hito en p20; propuesta `docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md` c.1-c.4 y c.8.
- **D-007** (2026-10-08): debuts Hoja 43, Matapi 63, Liana con fruta 83, Aguas turbias 100, Remolino 103, Barro 123,
  Lluvia 143, Brote 163 y Piraña 183; Cuia chica 14/18/22 y Cuia alta 27/160 (02 §8); combinaciones de d.3 y Q14 A.
- **Calendario d.4** (21-200) literal salvo cuatro celdas [P] por el bloqueo C1-9 de `AF.Core` (la Liana no admite
  capacidades mixtas; opción (b) de la revisión, mientras no se amplíe `futil`): 94 Ma → Li y 95 Li+Al → Ma+Al, 160
  Ll+Li+Al6 → Ll+Al6 y 198 Pi+Li → Pi+Ho (`calendar_note`). Más la rampa 11-20 (d.4, «Niveles 1-20») con las filas 11
  y 12 de 02 §13 (`ramp.rows`: 8 cuias = 6 + 2, par 15-19 y 16-20, bot 0-10 %). Leyenda: R, C y S puros; ★ estreno;
  (s) reto suave; (D) apunta a Difícil; (MD) apunta a Muy difícil; (tinte X) debut de tinte. Los nombres de hito no
  pasan al pack (pendientes del asesor).
- **GDD vigente** donde no cambia: rangos de 03 §11 (K, cuias, vacíos, par objetivo y porcentajes de etiquetas),
  pool de tintes del glosario §8-§9, pares de riesgo y `max_pares_riesgo` (03 §4.5), mundos 2-5 y agua base
  (glosario §8, §11).
- **[P] de la especificación**: K por rol (`k_plan`, limitado por el pool y por los pares de riesgo), ventanas de
  bot de los retos (41-59 con etiqueta 75-89 %, suaves 50-74 %, desde el 61 75-89 %), «Ch» = Cuia chica de 2.

## Especificación de 201-700: `specs/campaign_v2.json` (P4)

Copia versionada de la tabla de novedades de 201-700 rehecha por D2 (propuesta, d.5-d.5.8; 03 §11 y §12), cuya
recomendación aplica D-023, punto 10. Mismo formato que `campaign_v1.json`, con estas claves nuevas [P] (todas opcionales:
`campaign_v1.json` no cambia):

- `calendar`: las 500 celdas de d.5.3, literales (copiadas por programa de las tablas de la propuesta), con los códigos de
  d.5.1 (Ln, Ha, Ti, B2 y Fl) y «(MD; K = 13)» en los hitos de 14 cuias. Es la **variante a**, con la Tinaja (estreno en el
  323, D-019).
- `fallback`: el respaldo sin la Tinaja de cada una de sus 19 celdas (d.5.4). `--variant b` (en `--spec`, `--registry` y
  en la lectura, `CampaignSpec.Load(path, "b")`) toma el calendario con esas celdas: la **variante b**, la salida B de
  D-019. `published_variant` (a) es la que se lee sin `--variant`; la cambia C1b si se publica la b (d.5.8).
- En la tabla `mechanics`, las nueve de `campaign_v1` (para leer las celdas y contar la historia) más las de la 1.0, con
  `variant_of` (Ln y Fl de la Liana con fruta, Ha de la Hoja y B2 del Brote: pueden estrenarse en cualquier temporada, no van
  con su base y su estreno entra en el registro como `variant`), `not_with` y `only_with` (la columna «Con qué no va» de
  d.5.1; la Tinaja solo con Ho, Ha, Br, B2, Ma, Ba y capacidades) y `only_variant` (la Tinaja, solo en la a).
- `previous`: `campaign_v1.json`. Su calendario es la historia de las reglas que la necesitan (GAP-120, NO-3-ROW, SEASONS,
  el RETURN de la Piraña en 201-220, FIRST-TIME y NOVELTY-60), que solo fallan en 201-700; el registro sale de las dos.
- `max_risk_pairs` con `"max": "libre"` desde el 201 (03 §11): el lector lo toma como los 10 pares de riesgo (el máximo de
  la cabecera del pack).
- `mechanics_per_level` (2; 3 en retos desde el 301 y en hitos desde el 320), `capacities_per_level` (2 Cuias chicas y 1
  Cuia alta antes del 200, 02 §8: desde el 201 no hay tope por nivel) y `novelty_max_gap` (60, D-009 punto 8). Con ellas,
  ningún «antes del 300» ni «antes del 200» queda en el código: sin la clave, valen los de `campaign_v1` (02 §8 y §9.2).
- Sin `ramp` (la rampa 11-20 es de `campaign_v1`); `report_sources`, las fuentes del informe de la generación.

Datos del GDD: rangos de las filas 201-400 y 401-700 de 03 §11; mundos 6-9 del glosario §8 (Oriente 201-300, Madre de
Dios 301-420, Madidi 421-560 y Ucayali 561-700; `world_id` [P]); temporadas de d.5, punto 4; ciclo, ventanas y etiquetas
de D-009, como en `campaign_v1`. `k_plan` [P]: mundo 6, recuperación 9 y el resto 10; mundos 7 y 8, 10 y 11; mundo 9,
recuperación 10, comodidad 11, retos 12 e hito 13 (K = 13 + 1 vacío desde el 561, el primero en el 580). Con la Tinaja el
generador baja K hasta 12 cuias (02 §5).

```
dotnet run --project tools/level-generator -c Release -- --validate-spec tools/level-generator/specs/campaign_v2.json
dotnet run --project tools/level-generator -c Release -- --registry tools/level-generator/specs/campaign_v2.json --out tools/level-generator/specs/combinations_registry_v1.json --check
dotnet run --project tools/level-generator -c Release -- --self-test registry
```

No genera packs: los mundos 6-9 los generan P7a-P7d con `--levels` por mundo (y la b con `--variant b`, en `tools/out`); el
mundo 6 ya está (sección «Mundo 6: 201-300», P7a).

## Registro de combinaciones publicadas (`--registry`; P4)

La entrada versionada del pipeline que pide d.2, regla 3 (03 §17, comprobación 15; d.5.6 de la propuesta): con él,
«primera combinación» se decide sin mirar los packs y la reproducibilidad por hash sigue valiendo.

```
dotnet run --project tools/level-generator -c Release -- --registry tools/level-generator/specs/campaign_v2.json --out tools/level-generator/specs/combinations_registry_v1.json
```

Formato [P] `af-combinations/1` (`Campaign/SpecValidator.cs`, `CombinationsRegistry`):

```
{
  "format": "af-combinations/1",
  "source": ["campaign_v1", "campaign_v2"],
  "variant": "a",
  "combinations": [
    {"level": 14, "codes": ["Ch"], "kind": "debut"},
    {"level": 35, "codes": ["Al", "Ch"], "kind": "combination"},
    ...
  ]
}
```

- Una entrada por conjunto de códigos de celda (mecánicas y capacidades, sin R, C, S, marcas, tintes ni nombres), en el
  primer nivel donde sale; el conjunto entero, no sus pares (un trío es nuevo si esos tres nunca salieron juntos); los
  códigos en orden ordinal y las entradas por nivel. Un conjunto que ya salió, también en 11-200, no vuelve a entrar.
- `kind`: `debut` (un código), `variant` (un código con `variant_of`: Ln, Ha, B2 o Fl) o `combination` (dos o más).
  `kind` solo mira cuántos códigos lleva el conjunto, no la ★: un código que sale solo por primera vez en una celda sin ★
  (porque antes solo había salido combinado) también se anota `debut` o `variant`. Con las mecánicas no pasa (DEBUT-LEVEL y
  DEBUT-SLOT exigen que su primer tablero sea el de estreno, con ★ y la mecánica sola); con las capacidades podría pasar
  con la Cuia alta de 6, que debuta combinada en el 160 (Ll+Al6) y sin ★, si algún día sale sola. Hoy no pasa: las 18
  entradas de un código del registro son estrenos con ★.
- `source`: la cadena de `previous`; `variant`: la publicada (o la de `--variant`). UTF-8 sin BOM, LF y una entrada por
  línea: regenerarlo da los mismos bytes. Nunca se edita a mano.
- El archivo refleja solo la variante que se publica: 102 entradas con la a (40 de 11-200 y 62 de 201-700); con la b serían
  96 (40 y 56). **Se regenera antes de fusionar** si cambia lo que se publica (la b, el respaldo del hito 340 o el del 354;
  d.5.6 y d.5.8). `--check` no escribe y sale con 1 si el del disco difiere o falta; la especificación tiene que pasar
  `--validate-spec` en todas sus variantes, como en `--validate-spec` (si no, 1 y no escribe).
- **Respaldo del hito 340 en la a** (d.5.4: si la tinaja no llega al 0,90 del bot): en `campaign_v2.json` se cambia la celda
  340 a `Br+Ba (MD)`, **se quita también su entrada de `fallback`** (la celda ya no lleva la Tinaja y, si el respaldo se
  queda, falla FALLBACK) y se regenera el registro. TITULAR-8 admite entonces 7 apariciones de la Tinaja en 321-340. El
  fixture positivo `specs/tests/hito340_fallback_in_a.json` lo hace así y pasa `--validate-spec` y `--registry`.
- `--validate-spec` no lo lee: cada variante se comprueba con su propio registro, calculado en memoria (d.5.6).

## `--validate-spec`

```
dotnet run --project tools/level-generator -- --validate-spec tools/level-generator/specs/campaign_v1.json
```

Imprime por ciclo los niveles puros, los retos con su marca, las etiquetas objetivo (D y MD) y los debuts, y después
un `FAIL [regla]` por cada incumplimiento o un `AVISO`. Reglas (ids estables):

| Id | Regla | Fuente |
|---|---|---|
| CAL-COVER | calendario de `first_level` a `last_level` sin huecos | — |
| CAL-ROLE | R solo en p1-p2, S solo en p19, p1-p2 y p19 puros | c.3, c.4 |
| LABELS | (s), (D) y (MD) en su posición | D-009; c.3 regla 7 |
| TITULAR-8 | cada titular sale 8 veces en su ciclo de debut; la Tinaja, 7 si el hito de su ciclo no la lleva (el respaldo del 340 aplicado en la a) | d.4; d.5.4; 03 §12 |
| PURE-30 | al menos un 30 % de niveles puros por ciclo | d.2 regla 1 |
| PURE-COMFORT | al menos 3 de las 8 comodidades de p7-p17 puras | c.4 |
| FORBIDDEN | turbias solo con filtros; un contador por nivel; Lluvia nunca con Piraña; Remolino nunca con Piraña [P]; `not_with` y `only_with` de la tabla; una variante nunca con su base (`variant_of`) | d.3; Q14 A; d.5.1 |
| MURKY-SLOT | turbias solo en slots con etiqueta | T1; b.2 |
| MURKY-2 | turbias como mucho 2 veces por ciclo | b.2 |
| BEFORE-DEBUT | nada antes de su debut | D-007; 02 §8 |
| DEBUT-LEVEL | el primer uso es el debut, con ★ | D-007; 02 §8 |
| DEBUT-SLOT | ningún debut en reto ni hito salvo las turbias en el 100; 2-3 estrenos seguidos con la mecánica sola | c.3 regla 6; 02 §9.2 regla 2 |
| MAX-2 | mecánicas por nivel dentro de `mechanics_per_level` (2; 3 en retos desde el 301 y en hitos desde el 320; las capacidades no cuentan [P]) | 02 §9.2 regla 4; d.5.1 |
| RETURN | la titular vuelve como secundaria en un reto o hito del ciclo siguiente | d.4 |
| CAP-CYCLE | 6 niveles por ciclo como mucho (8 en el de debut) | d.2 regla 2 |
| NO-3-ROW | nunca 3 niveles seguidos fuera del estreno | d.2 regla 2 |
| SEASONS | temporadas alternas (la racha sigue desde `previous`), el ciclo de debut toma la de la mecánica salvo las variantes | d.2 regla 7 |
| GAP-120 | ninguna mecánica ni capacidad (Cuia chica, Cuia alta) pasa más de 120 niveles sin volver, contando `previous` | resumen 6; d.2 regla 4; 02 §8 |
| FIRST-TIME | al menos una primera vez por ciclo: estreno, tinte nuevo o entrada del registro de combinaciones (un conjunto nunca visto, también contra 11-200) | d.2 regla 3; d.5.6 |
| CAPACITIES | Cuias chicas (con la cueva) y Cuias altas dentro de `capacities_per_level` (≤ 2 y ≤ 1 antes del 200); Cuia alta de 6 solo en hitos desde el 141 | 02 §8 |
| LIANA-CAPS | la Liana (con fruta o de cuenta) y la Flor nunca van con Cuia chica, Cuia alta ni Piraña: `AF.Core` las rechaza (C1-9; K3b) y el generador no quita nada de la celda | C1-9 |
| FALLBACK | con dos variantes, cada celda con una mecánica de una sola variante (la Tinaja) lleva su respaldo sin ella en `fallback`, y solo esas | d.5.4; D-019 |
| HITO-14 | K ≥ 13 (14 cuias) solo en hitos, con «K = 13» en la celda igual al `k_plan`, sin capacidades, Piraña ni Tinaja | 03 §11; d.5.1 |
| NOVELTY-60 | nunca más de `novelty_max_gap` (60) niveles entre dos primeras veces seguidas (entradas del registro o tintes nuevos), desde la última de `previous` | D-009 punto 8; d.5 punto 5; d.5.6 |

Las tres últimas solo se aplican si la especificación las necesita (dos variantes, un K ≥ 13 o `novelty_max_gap`). Con
`campaign_v1.json` y con `sample_variants.json`: 21 reglas, 0 fallos (la salida no cambia con P4, salvo el texto de MAX-2).
Con `campaign_v2.json`: 24 reglas, 0 fallos en las dos variantes, que se comprueban enteras, cada una con su resumen, su
registro en memoria (102 entradas en la a y 96 en la b) y su hueco máximo entre primeras veces (34, del 526 al 560); los
fallos llevan la variante. Las reglas comprueban el calendario; lo publicado lo comprueba `level-qa` (QA-MECH-CELL y
QA-GAP-120).

### Fixtures negativos: `specs/tests/*.json`

Cada fixture «extiende» `campaign_v1.json` (`"extends"`), cambia solo las celdas que trae y declara la regla que debe
fallar (`"expect_rule"`). El validador sale con 1 e imprime `esperado: [REGLA] presente`. Para pasarlos todos (Git Bash):

```
for f in tools/level-generator/specs/tests/*.json; do
  out=$(dotnet run --project tools/level-generator --no-build -- --validate-spec "$f"); code=$?
  echo "$(basename "$f"): exit=$code $(echo "$out" | grep '^esperado')"
done
```

Todos deben dar `exit=1` y `presente`, salvo el positivo `hito340_fallback_in_a.json` (`exit=0`). Los de P4 extienden `specs/campaign_v2.json` y se comprueban en sus dos variantes
(con `"expect_variant"`, el fallo tiene que salir en esa): `novelty_gap_61.json` (61 niveles del 526 al 587 sin primera vez:
NOVELTY-60, y FIRST-TIME en 541-560 y 561-580, porque con una primera vez por ciclo el hueco no pasa de 59),
`novelty_gap_60.json` (el límite: 60 niveles, del 526 al 586, no fallan NOVELTY-60; sí FIRST-TIME, la que declara),
`combination_from_v1_not_new.json` (el 229 pasa a Ll+Ba, del 158: una combinación de 11-200 no cuenta como nueva y 221-240 falla
FIRST-TIME), `variant_b_forbidden.json` (el respaldo del 329 con dos contadores: FORBIDDEN solo en la b),
`tinaja_without_fallback.json` (la Tinaja en el 464 sin respaldo: FALLBACK en las dos variantes; declara la a, la que ve
`--registry`), `hito14_with_capacity.json` (Cuia alta en
el hito de 14 cuias 620: HITO-14) y `variant_with_base.json` (Hoja atada con Hoja en el 292: FORBIDDEN); `--self-test registry`
los repite. **`hito340_fallback_in_a.json` es positivo** (`exit=0`, sin línea `esperado`): el 340 con su respaldo en la a y
sin su entrada de `fallback` (un `null` en un `fallback` que extiende quita la entrada del base); `--self-test registry`
comprueba además que pasa `--registry` y que `--registry --check` sale con 1 si el archivo difiere o falta. Los de la Tinaja (`tinaja_in_recovery.json` y `tinaja_in_remanso.json`) extienden
`specs/sample_tinaja.json` y fallan por CAL-ROLE: p1, p2 y p19 son niveles puros, sin tinaja (D-019, Q-Ti1 A). Los de las
variantes de la 1.0 extienden `specs/sample_variants.json`: `liana_n_with_capacity.json` (Liana de cuenta con una Cuia chica) y
`flor_with_pirana.json` (Flor con la Piraña) fallan por LIANA-CAPS, y `brote_n2_before_debut.json` (Brote de dos hojas antes de
su estreno), por BEFORE-DEBUT; `--self-test variants` los repite. Los dos de LIANA-CAPS ponen la capacidad en una celda que ya
lleva la variante (251 y 311), para no cambiar sus apariciones; además fallan DEBUT-LEVEL (y DEBUT-SLOT el de la Piraña), porque
la Cuia chica y la Piraña se añaden a la tabla solo ahí, y el del Brote de dos hojas falla también DEBUT-LEVEL: la `description`
de cada fixture enumera sus reglas.

## Métricas, alivio y paridad (`Metrics/`)

- `Metrics.cs`: par (A* w = 1, 300.000, `run`), ramificación, trampas (A* a 3.000, ≤ 120 pares por el índice entero
  ⌊k·n/120⌋ [P]), trampas tempranas (`AF.Solver.EarlyTraps`, Q16 B), complejidad y bot ×40 con la semilla del nivel;
  índice D de 03 §7 (pesos 0,35 / 0,25 / 0,20 / 0,10 / 0,10 y z-scores del pool del rango).
- `ReferencePath.cs`: ruta óptima de referencia para medir ramificación y trampas en el orden del prototipo (03 §18); solo
  se usa si tiene la longitud del par de `AF.Solver`. La ramificación cuenta los `MOVIMIENTOS` literales de 03 §5 (un vacío
  por capacidad y mecánica y por origen), la definición de 03 §7 [P].
- `Relief.cs`: variantes de alivio de 03 §13 (`plus_empty` sin resolver, con par null; `minus_color` = la eliminación
  resoluble de par más alto con A* a 100.000). Se publican las dos [P] en el orden de 03 §13, si cumplen el criterio de
  level-qa (03 §17 #8). Nunca se quita un color que sea llave o contenido de una mecánica [P].
- `Parity.cs` (`--parity`): sobre los 40 niveles de `tools/out/levels_proto.json` y `tools/out_inverse/levels_proto.json`
  compara par y complejidad (40/40), ramificación y trampas a 2 decimales (≥ 38/40) y color y par de −1 color (donde el
  prototipo los tiene). `bot_fail` y `D` no se comparan: el PRNG cambia (09 R26). La cifra de C# se redondea al par
  (`ToEven`) a los decimales del prototipo, como hace Python, y el informe imprime esa misma cifra, la que se compara.

```
dotnet run --project tools/level-generator -c Release -- --parity
```

Resultado actual: par 40/40, complejidad 40/40, ramificación 40/40, trampas 40/40 y −1 color 39/39 (el nivel 19 del lote
híbrido no tiene −1 color en el prototipo).

## Generación de la campaña (`--spec`, `Generation/`)

```
dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/campaign_v1.json --levels 11-200 --out Assets/_Project/Data/Levels --report tools/out/campaign_v1_report.md
dotnet run --project tools/level-qa -c Release -- Assets/_Project/Data/Levels/mundo_02_v1.json Assets/_Project/Data/Levels/mundo_03_v1.json Assets/_Project/Data/Levels/mundo_04_v1.json Assets/_Project/Data/Levels/mundo_05_v1.json --report tools/out/level_qa_report.md
```

El segundo comando es la compuerta completa (`tools/level-qa/README.md`): comprobaciones por nivel, esquema
(`validate_schema.py --pack`), reproducibilidad (regenera la campaña en memoria con `CampaignRunner.Generate`, el mismo
código, y compara byte a byte) y banda medida. Usa siempre `-c Release` (en Debug tarda varias veces más). `--check`
regenera y compara byte a byte con los packs del disco sin escribir nada (03 §17 #10), sin las demás comprobaciones. El
índice D depende del pool de cada rango de 03 §11, así que un pack solo se reproduce con los mismos `--levels` (por
defecto, todos los de la especificación: 11-200 con `campaign_v1.json`). `--variant` elige la variante de una especificación
con respaldos (por defecto, la publicada); el informe lleva en el título los niveles generados y, con dos variantes, la
variante.

Fases (los tiempos salen por la consola; el informe no lleva tiempos):

1. **Candidatos por slot** (`SlotGenerator`, en paralelo; cada slot depende solo de su semilla
   `amazonfill:v1:m{mm}:v1:n{nnnn}`): tablero base de la fuente de la banda (03 §4.4: `walk` en recuperación, comodidad,
   remanso y estrenos; `fill` en los retos de 21-39; `deep` y `fill` alternos en los retos e hitos desde el 41 [P]),
   superposición de mecánicas (`Superposition`, 03 §9), tintes (`TintAssigner`, 03 §4.5), orden visual al azar,
   `BoardBuilder` y `LevelValidation`, bot ×40 con la semilla del nivel y, si entra en la ventana del slot, `SafePath` en
   Aguas turbias, A* (par ≥ 2K), trampas tempranas con el tope de su etiqueta (Q16 B) y métricas de la ruta. Rechazo por
   ventana con 30 candidatos por etapa (150 con `walk`, que cuesta milisegundos) y relajación en el orden de 03 §4.4
   (comodidad: Cuia chica de 2 como medio vacío y después 2 vacíos; los niveles puros pasan directamente a 2 vacíos; reto e
   hito: `fill` con 2 vacíos); si los candidatos quedan por debajo de la ventana, se endurece con `deep`. El pool busca 6
   candidatos en retos, hitos y los niveles que los siguen, y 3 en el resto. Sub-semilla del candidato n:
   `Prng.SubSeed(semilla, 0x47454E00 + n)`; `gen.candidates` del pack es n + 1.
2. **Pool e índice D** (`Pool`): z-scores del pool del rango (todos los candidatos en ventana), congelados; banda = cuantil.
3. **Colocación** (`Placement`, programación dinámica por ciclo): escalera de D-009, bajada de una banda entera de D tras
   cada etiqueta (y D menor), remanso con D por debajo de la media de comodidad del ciclo (c.4; `PlaceCycle`),
   anti-repetición (forma y Hamming, `Campaign/BoardShape.cs`) y saltos de +2,2 / −3,4 bandas como penalización. **La banda
   coloca** (03 §7; cuantil q de D en el pool del rango): comodidad, estreno, remanso y rampa como mucho en banda de
   comodidad (q ≤ 0,70); recuperación (p1-p2) en banda de recuperación (q ≤ 0,10) o, como mínimo, con D por debajo de la
   comodidad más fácil del ciclo (`PlaceCycle`); hito (p20) en banda de hito (q > 0,95) o, como mínimo, con par − 2K ≥ la
   media del pool del rango; en 21-40, todos los retos suaves por debajo del hito 40. Cumplir solo el mínimo penaliza
   fuerte (la colocación prefiere la banda del rol). Solo un reto o el hito pueden quedar fuera de su ventana (c.2); en
   recuperación, comodidad, estreno, remanso y rampa no se usa ningún candidato fuera de ella (03 §17 #3). Antes de colocar
   cada bloque, los slots sin ningún candidato de la banda de su rol piden más candidatos (rondas de banda, hasta 6, con
   sub-semillas desde el intento 20.000 y los estadísticos del pool ya congelados). Si un ciclo no tiene colocación, se
   piden más candidatos a sus retos, hito, siguientes y recuperaciones (hasta 2 rondas); un reto o hito que quede fuera de
   su ventana se publica con la etiqueta que mide (c.2: sin etiqueta si mide menos del 75 %) y el informe lo registra.
4. **Alivio** (`Metrics/Relief.cs`) desde el nivel 31.
5. **Packs** (`PackWriter`): `pack_schema` 2, o 3 si algún nivel del pack lleva la tinaja (D-019) o una variante de la 1.0
   del 3 (D-023, punto 5; sección «Variantes de la 1.0»), pack a pack (la cabecera del informe dice «2 (3 en los packs con
   tinaja o con variantes de la 1.0: …)» si una campaña tiene de los dos; FIXG); minificado con un nivel por línea, UTF-8 sin
   BOM y LF, decimales fijos.

El informe (`tools/out/campaign_v1_report.md`) trae, además de los packs, el pool, las etiquetas, los ciclos y la tabla
por nivel: el debut real y el hueco máximo de cada mecánica y capacidad sobre los tableros publicados, el remanso frente a
la comodidad de su ciclo, la banda medida por rol de cada ciclo (recuperaciones, comodidad más fácil, q máximo de la
comodidad, remanso, retos e hito con su par − 2K frente a la media del pool), el porcentaje de comodidades y remansos
relajados a 2 vacíos o a Cuia chica por rango y las cuias por rango frente a 03 §11.

Decisiones [P] de la generación (para revisar en la integración):

- La Cuia chica «cuenta como vacío» (02 §8): en un nivel con Cuia chica, cada una ocupa uno de los vacíos del rol y
  queda al menos un vacío de 4.
- La Hoja, el Matapi y la Lluvia ocupan un vacío: en los estrenos, uno de los 2 vacíos (ejemplo de 08 §4.1); en comodidad
  traen un vacío más; en retos e hitos usan los del rol.
- La Liana con capacidades mixtas está bloqueada en `AF.Core` (C1-9): ninguna celda la pide (LIANA-CAPS) y el generador
  nunca quita una capacidad o la cueva de la celda (si llegara a pasar, se detiene).
- Filas 11 y 12 de 02 §13: K y vacíos fijos; se rechaza el candidato con el par fuera de la fila y, si `walk` no basta,
  `deep` corto (2.000 nodos) y `deep`.
- Contrato 6: ningún candidato que mida ≥ 0,90 antes del 60 ni ≥ 0,75 antes del 40 entra en el pool, tampoco al relajar.
- Remanso (p19): la colocación exige D(p19) < media de D de las comodidades del ciclo que no son tableros de estreno (c.4).
- Banda por rol (03 §7): los mínimos de la recuperación (D por debajo de la comodidad más fácil del ciclo) y del hito
  (par − 2K ≥ la media del pool del rango) son los de la revisión del 2026-10-09; la banda exacta del rol solo penaliza.
- Estreno de una capacidad fuera de la rampa y del hito (la Cuia chica de 3 en el 22, p2): ventana de estreno (< 0,20) y
  los 2 vacíos libres de su posición; la Cuia chica de estreno no ocupa uno de ellos (02 §8: «debuta en 2-3 tableros
  fáciles»). Las de la rampa (14 y 18) no cambian.
- Aguas turbias: 1-2 cuias y como mucho 3 dosis ocultas (el tope de 02 T2 es 4) para que la ruta segura cueste segundos.
- Un nivel turbio pide hasta 6 rondas más de candidatos turbios (sub-semillas desde el intento 11.000) antes de publicarse
  sin turbias (respaldo de T1, registrado en el informe y aviso QA-MECH-CELL).
- Sin `par_approx`: la Piraña deja el par exacto (C3-2).

Diagnóstico: con la variable de entorno `LEVELGEN_TRACE=<nivel>` el generador traza por stderr cada candidato de ese
nivel (fuente, K, vacíos, bot_fail, motivo del rechazo y milisegundos) y un resumen del slot. No cambia la salida.

## Mundo 6: 201-300 (P7a)

El primer pack de 201-700: el mundo 6 (Oriente, `w06_oriente`) con `campaign_v2.json` en la variante publicada (a; sin la
Tinaja, que no sale antes del 323), con los estrenos de la Liana de cuenta (243-245, `liana_n`) y de la Hoja atada (283-285,
`hoja_atada`), y `pack_schema` 3 por la Hoja atada. D depende del pool del rango de 03 §11 (201-400), así que el pack solo se
reproduce con `--levels 201-300` (lo mismo en `--check` y en level-qa).

```
dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/campaign_v2.json --levels 201-300 --out Assets/_Project/Data/Levels --curation tools/out/curation_w06.md
dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/campaign_v2.json --levels 201-300 --out Assets/_Project/Data/Levels --check
dotnet run --project tools/level-qa -c Release -- Assets/_Project/Data/Levels/mundo_06_v1.json --spec tools/level-generator/specs/campaign_v2.json --levels 201-300
dotnet run --project tools/level-generator -- --seasons tools/level-generator/specs/campaign_v1.json tools/level-generator/specs/campaign_v2.json --until 300 --out Assets/_Project/Data/Levels/seasons_v1.json --check
```

Unos 5 minutos cada uno (en Release, con 24 hilos en el equipo de escritorio). El `.meta` de `mundo_06_v1.json` lo genera Unity
en C1a, que registra el pack en el cliente (se fusiona con P7a). Los packs 1-200 no cambian (`--check` de `campaign_v1.json` y
del tutorial).

### Informe de curación (`--curation`; 09 §2.1)

`--curation <md>` escribe, con los mismos niveles colocados que el pack, la propuesta de curación que aprueba el dueño (09 §2.1
y §2.2: los 30 retos e hitos de cada paquete; 03 §3: un reto por debajo de 0,75 se publica sin etiqueta y la curación decide si
se sustituye por otro del pool del slot). `Generation/CurationReport.cs`; sin tiempos, así que dos ejecuciones dan los mismos
bytes. Secciones:

- **Resumen**: pack, `pack_schema`, bytes y SHA-256; cuántos retos e hitos miden su etiqueta objetivo; los niveles para revisar.
- **Retos e hitos**: etiqueta medida por el bot ×40 frente a la objetivo (D-009), D y su banda (03 §7), cuántos candidatos más
  del slot miden la objetivo («Otros del slot»: con cuáles se podría sustituir) y la propuesta [P]: «aceptar» o «revisar» (no
  mide la objetivo, queda fuera de su ventana, `risk_adjacent` o sin turbias por T1), con notas que no piden revisar (hito con
  D en banda de reto y el mínimo cumplido, como el aviso QA-BAND-Q; retos Difícil con D en banda de comodidad; las realidades
  de las Aguas turbias).
- **Hitos**, uno a uno: tablero, mecánicas (con las Aguas turbias y sus realidades), medida y notas de la generación.
- **Tableros de estreno** (02 §9.2 regla 2) y la sección **Variantes de la 1.0** del informe de la generación.
- **Otros niveles con avisos** y la tabla **Curación del dueño**, que nace «pendiente»: el dueño anota su decisión por nivel en
  la última columna (la única que se edita a mano) y la repite en el PR, porque regenerar la devuelve a «pendiente». Si la
  curación pide cambios, se regeneran el pack, el informe y la comprobación de level-qa.

`tools/out/curation_w06.md` es el del mundo 6; P7b-P7d escriben `curation_w07.md` a `curation_w09.md`.

## Tinaja (M13; D-019, Q-Ti1 A; 03 §8 y §9; propuesta de la tinaja c.3)

La celda de calendario `Ti` (mecánica `tinaja`, clase `source` [P]: fuente de dosis ocultas) activa el paso «Ti» de la
superposición (`Generation/Superposition.cs`), antes de «Tu»:

- saca n dosis de las cimas de cuias sin mecánica con 2 dosis o más (no crea vacíos ni deja el Barro a la vista) y las
  ordena en la tinaja (cap 5) con T4 y la boca a la vista; el resto va en `hidden`;
- n por tipo de nivel: 3 en estreno (p3-p5), comodidad y práctica; 4-5 en retos (al azar); 5 en el hito. **Nunca en p1, p2
  ni p19** (el paso se niega; `--validate-spec` ya lo rechaza con CAL-ROLE);
- añade su cuia vacía (Cuia chica de 2) contigua; el orden visual (`ShuffledWithTinaja`) baraja las demás cuias y deja la
  pareja en una ranura de la misma fila con cualquiera de las filas que el cliente puede elegir (D-006: 1 fila con ≤ 5
  cuias, 2 con 6-10, 2 o 3 con 11-14, 3 con 15-16), con la tinaja a la izquierda o a la derecha [P];
- solo consume números aleatorios en los slots con tinaja: los packs 11-200 no cambian (`--check`).

Tablero base (c.3 punto 2): vacíos de 4 sin contar la pareja, 2 en los tableros de estreno (d.3; 03 §12, p3-p5: «1 (2
hasta el 30 y en debuts)») y 1 en los demás: práctica, retos, hito y la comodidad fuera del estreno. Esta última va con 1
[P], **pendiente de decisión**: c.3 punto 2 dice «2 … en el debut (K ≤ 6) y en comodidad», pero ninguna fuente dice qué
posiciones son «comodidad» y cuáles «práctica»; 03 §12 da 1 vacío a p3-p5 fuera de los debuts y, con 2 vacíos, el bot casi
no falla (c.9: 0-2 %) y el tablero no entra en la ventana de comodidad (20-60 %). K ≤ 6 en el estreno (c.3 y d.3).

**Como mucho 12 cuias con la pareja** (02 §5, fila «Tinaja y su cuia»: «como mucho 12 cuias en un nivel con tinaja» [P];
`Superposition.TinajaMaxCuias`): si la etapa pasa de 12, se baja K y el informe lo anota («tinaja: K <= n por el tope de 12
cuias»); si K quedara por debajo de 3, la etapa no aplica. Es un tope de layout, así que tampoco se publica un
`plus_empty` de 13 cuias [P]. Al relajar, los vacíos no cambian: K − 1 una vez (por debajo del tope), o `deep` si los
candidatos quedan por debajo de la ventana.

En los niveles con tinaja todas las búsquedas que deciden «hay solución» van **sin poda de fútiles**
(`SolverOptions.ForLevel`, `SafeMoves.ForPipeline(mode, level)`; C1-8): par y trampas (`Metrics.cs`), el rechazo de `fill`
(`FillSource.cs`), la ruta segura, los paseos y la prueba de irresolubilidad. El bot y las trampas tempranas siguen con la
poda.

Filtros (`Generation/TinajaWalks.cs`), en cada candidato que se mide:

| Filtro | Qué exige |
|---|---|
| Ruta segura | `SafePath` (03 §8) con 20.000 nodos por realidad |
| Paseos seguros [P] | 20, con un vertido seguro al azar hasta ganar: ninguno llega a un estado sin vertido seguro ni pasa de 400 vertidos |
| Paseos casuales [P] | 30, con cualquier vertido útil al azar. El primer estado sin vertido seguro de cada paseo se clasifica por `SafeMoves.Outcome` y, solo con `SomeRealitySolvable`, con A* en la realidad verdadera (`TinajaWalks.ClassifyCasual`; FIXG): si `SafeMoves` agota sus 20.000 nodos por realidad (los de `ReliefOptions` en las variantes), se rechaza por «presupuesto agotado» sin consultar la verdadera (aunque no tenga solución), nunca por «adivinar» (no se cuenta en la columna «a» del informe); `NoExit` es un error del jugador y el paseo acaba; con `SomeRealitySolvable`, la verdadera sin solución es un error del jugador, resoluble es «adivinar» y el nivel se rechaza, y si A* agota sus 300.000 nodos en ella, «presupuesto agotado» |
| S0 sin trampas | Solo en el estreno: todo vertido útil de S0 es seguro (por hash canónico). Uno que no lo es se rechaza como «trampa en S0» si A* deja alguna realidad de S0 demostrada sin solución tras él y, si no, por «presupuesto agotado» (FIXG; `TinajaWalks.S0Reject`: con alguna trampa demostrada, «trampa en S0» y los demás en el sufijo «; N mas sin seguridad demostrada») |
| Coste de Morpho | w = 2 y 40.000 nodos por realidad (`hint_weight`, `hint_budget_nodes`), desde cero en S0, en la ruta y en los paseos: Morpho da un vertido seguro y expande como mucho `MorphoNodeCap` = 150 ms × `PcNodesPerMs` [E] ÷ `MorphoDeviceFactor` (8× [P], hasta PF1) = 18.750 nodos |

`PcNodesPerMs` = 1.000 [E] sale de la calibración de `--self-test tinaja` (líneas INFO «calibracion»): Morpho desde cero en
el estado más pesado de los filtros de los retos 329 y 332 y del hito 340 del pack de muestra (8.298, 10.356 y 10.606 nodos
por consulta). Medido el 2026-10-14 en el equipo de escritorio x64 del dueño (AMD Ryzen 9 9900X, Windows 11, .NET 10.0.402
Release), con otros agentes en marcha: 1.000-1.463 nodos/ms por nivel en cuatro ejecuciones (6-10 ms por consulta; medias
de 1.083-1.377). 1.000 es el mínimo medido, con el equipo cargado; con más carga podría bajar. Es la velocidad de un PC, no
la del teléfono: el factor 8× [P] la convierte en el tope del teléfono hasta que PF1 lo mida en uno de clase A. Antes se
calibraba con 20 consultas de 32 nodos sobre el S0 de b.2, que miden sobre todo el coste fijo; ya no.

El motivo del rechazo separa «sin vertido seguro» de «presupuesto agotado» (`SafeMoves.Outcome`) en la ruta, en los paseos
seguros y casuales y en S0 (03 §8; FIXG); el informe lo cuenta por nivel en su sección «Tinaja» (columna «Candidatos
rechazados por la tinaja»), y `--self-test tinaja` fija la clasificación de los paseos casuales en cada combinación y la de «S0 sin trampas» (la trampa demostrada del tablero perdido de b.2, el presupuesto agotado con 1 nodo por realidad y en el estreno del fixture de QA-TINAJA-DEBUT, y el caso mixto); si falta uno de los fixtures de level-qa que usa, falla. Alivio (03 §13): `minus_color` nunca quita una fruta de la tinaja; las variantes conservan la
máscara de la tinaja y repiten la ruta segura y los paseos, sin la compuerta de coste. **Opciones de las variantes [P],
pendientes de decisión del tech-lead y del dueño** (revisión de P1; D-023 o la siguiente): `TinajaWalks.ReliefOptions`, que
hoy son las de Morpho (w = 2 y 40.000 nodos por realidad) y no las del pipeline de 03 §8 (w = 1 y 20.000 nodos por
realidad, la fila «Aguas turbias (seguridad por realidad)» de la tabla de 03 §5). Motivo: un alivio tiene más espacio libre
que su nivel y, sin poda de fútiles, A* óptimo agota los 20.000 nodos por realidad en tableros que se ganan; lo que cuenta en
la variante es que Morpho encuentre siempre un vertido seguro (un «seguro» solo sale de soluciones encontradas, así que el
filtro no se relaja). 03 §13 solo dice que «las variantes repiten la ruta segura y los paseos». Es el contrato entre el
generador y level-qa: P3 (`ReliefChecks`) usa `TinajaWalks.ReliefOptions` (level-qa enlaza `Generation/*.cs`). Si el dueño
lo rechaza, `ReliefOptions` pasa a `SafeMoves.ForPipeline(PourMode.Run, s0)` y las variantes que agotan el presupuesto se
tratan aparte (03 §13 exige `relief` no vacío desde el 31). El `plus_empty` pone su vacío donde la pareja siga en la misma
fila.

`PackWriter`: un pack con la tinaja sale con `pack_schema` 3 (`{"type":"tinaja"}` y `{"type":"cucharon"}` sin campos,
`murky` = true, `hidden` con las capas de la tinaja menos la boca e `intro_mechanic` = `tinaja` en su estreno), y cada
variante de `relief` que lleva la tinaja escribe su máscara en `hidden`, tras `attrs` y con los índices de la variante
(decisión del dueño del 2026-10-14, opción a de la revisión de P2a; 08 §4.1: `relief_board` admite `hidden` si y solo si su
`attrs` contiene la tinaja). Los demás packs, con el 2 y sus variantes sin `hidden`.

Muestra (no es el calendario de 201-700, que rehacen la pregunta 33 y P4):

```
dotnet run --project tools/level-generator -c Release -- --validate-spec tools/level-generator/specs/sample_tinaja.json
dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/sample_tinaja.json --levels 321-340 --out tools/out/sample_tinaja --report tools/out/sample_tinaja_report.md
dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/sample_tinaja.json --levels 321-340 --out tools/out/sample_tinaja --check
dotnet run --project tools/level-generator -c Release -- --self-test tinaja
```

`specs/sample_tinaja.json` es un ciclo 321-340 con la tinaja sola en estreno (323-325), práctica (327), retos (329, 332 y
338) e hito (340); su pack de muestra (`tools/out/sample_tinaja/mundo_07_v1.json`, `pack_schema` 3) y su informe
(`tools/out/sample_tinaja_report.md`) se regeneran con esos comandos. Las combinaciones de d.3 (Ti+Ho, Ti+Ma y Ti+Br) no
caben en una muestra de un ciclo (`--validate-spec` exige que cada mecánica de la tabla debute en el calendario).

## Variantes de la 1.0 (P5; 02 §9.1; propuesta de D-007 b.3; firma de `pack_schema` 3, D-023 punto 5)

Códigos de calendario [P] de P5, los mismos de d.5.1 de la propuesta (D2), con su paso propio en la superposición
(`Generation/Superposition.cs`) y su lectura sobre un `attrs` publicado (`MechanicCodes`, en `Campaign/CampaignSpec.cs`; el
lector de la especificación rechaza cualquier otro código):

| Código | Variante | `attrs[i]` | Paso de la superposición | `intro_mechanic` |
|---|---|---|---|---|
| Ln | Liana de cuenta (243) | `{"type":"liana","n":1}` o `n` = 2 | como la Liana con fruta: una cuia de 4 con contenido y sin Barro; n = 1 en los tableros de estreno y 1-2 al azar en los demás [P] (b.3: «n cuias (1-2)») | `liana_n` [P] |
| Ha | Hoja atada (283) | `{"type":"hoja_atada","key":c}` | una cuia con contenido cuya dosis del fondo es c, con cap = U_c de c (D-024) y sin Barro, que no esté llena de c; nunca un vacío [P], para no gastar los vacíos del rol | `hoja_atada` |
| B2 | Brote de dos hojas (343) | `{"type":"brote","n":2}` | como el Brote: una cuia con 1-2 dosis visibles (espacio ≥ 2, 02 R10 b) y sin Barro; su color es la cima; con la cuia de holgura y `deep`, como el Brote | `brote_n2` [P] |
| Fl | Flor de vitória-régia (403) | `{"type":"flor","key":c1,"key2":c2}` | como la Liana con fruta: una cuia de 4 con contenido y sin Barro, con dos frutas de 4 dosis distintas que no están dentro | `flor` |

- Orden de la superposición: `Re Ho Ma Ll Li Ln Fl Br B2 Ha Ba Pi Ti Tu`. Las variantes se intercalan sin cambiar el orden
  relativo de las demás (`Superposition.OrderBeforeVariants`), así que solo consumen números aleatorios en los slots que las
  llevan: los packs 11-200 no cambian (`--check`). La Hoja atada va antes del Barro (nunca en una cuia con mecánica) y de la
  tinaja (solo saca de cuias sin mecánica).
- La validez la decide `LevelValidation` (K3a y K3b): la Liana de cuenta con n colores que puedan sellar fuera de ella; la Hoja
  atada con su fruta en el fondo, cap = U_c y una por fruta (D-024); el Brote de dos hojas con espacio ≥ 2; la Flor con key ≠
  key2, ninguna dentro y las dos capaces de sellar fuera a la vez. La Liana de cuenta y la Flor van sin capacidades mixtas
  mientras dure el bloqueo C1-9 (el generador se detiene si una celda las pide, y LIANA-CAPS lo rechaza antes) y sin la Cuia
  chica de relajación.
- `GenBoard.MapColors` cambia también la `key2` de la Flor, y el alivio (`minus_color`) nunca quita una llave, tampoco la `key2`.
- `PackWriter`: un pack con la Hoja atada, la Flor (`key2` tras `key`), el Brote de dos hojas o un `intro_mechanic` de las
  variantes (`liana_n`, `hoja_atada`, `brote_n2` y `flor`; `PackWriter.Pack3Intros`) sale con `pack_schema` 3; la Liana de
  cuenta ya es del 2, así que un pack que solo la lleva fuera de su estreno sigue con el 2. La cabecera del informe dice «2 (3
  en los packs con tinaja o con variantes de la 1.0: …)» si una campaña tiene de los dos.
- `CampaignReport`: las apariciones distinguen Li de Ln y Br de B2, y la sección «Variantes de la 1.0» (solo si la
  especificación o algún nivel las lleva) cuenta por variante los niveles, los ejemplares y los tableros de estreno que cumplen
  02 §9.2 regla 2 (un ejemplar, 2 vacíos y `bot_fail` < 0,20), con los campos de cada nivel.
- `tools/level-qa` lee la `key2` (`PackReader`), así que la validación, el par, el bot y la reproducción usan el tablero del
  pack. Lo fija `dotnet run --project tools/level-qa -c Release -- --self-test key2` (`VariantsSelfTest.RunPackReadBack`,
  enlazado en level-qa): el estreno de cada variante de la muestra, escrito con `PackWriter` y su alivio a un pack temporal,
  se lee con el `PackReader` de level-qa con el mismo tablero, los mismos `attrs` (también la `key2`) y el mismo hash que el
  del generador, en el nivel y en sus variantes de relief; la Flor sin `key2` no se construye. Los controles por variante de
  level-qa (P6: los códigos Ln, Ha, B2 y Fl en QA-MECH-*, `intro_mechanic` `liana_n` y `brote_n2` en QA-DEBUT, QA-VAR-* y el
  tope de mecánicas de `mechanics_per_level`) los cumplen los packs de muestra: level-qa sale con 0 fallos con
  `--spec tools/level-generator/specs/sample_variants.json` (`tools/level-qa/README.md`). La clave `mechanics_per_level` de
  las especificaciones (`{"max": 2, "raised_from_level": 301, "raised_milestone_from_level": 320, "raised_max": 3}`: 02 §9.2
  regla 4, con los retos hasta 3 desde el 301 y los hitos desde el 320, 03 §12 fila p20 y d.5.1) solo la lee level-qa por
  ahora (`CampaignSpec.MaxMechanicsAt`, probado por `level-qa --self-test reglas`); desde P4 también MAX-2 de
  `--validate-spec`.

Muestra (no es el calendario de 201-700, que es `campaign_v2.json`, P4):

```
dotnet run --project tools/level-generator -c Release -- --validate-spec tools/level-generator/specs/sample_variants.json
dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/sample_variants.json --levels 241-320 --out tools/out/sample_variants --report tools/out/sample_variants_report.md
dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/sample_variants.json --levels 241-320 --out tools/out/sample_variants --check
dotnet run --project tools/level-generator -c Release -- --self-test variants
dotnet run --project tools/level-qa -c Release -- --self-test key2
dotnet run --project tools/level-qa -c Release -- --self-test reglas
dotnet run --project tools/level-qa -c Release -- tools/out/sample_variants/mundo_06_v1.json tools/out/sample_variants/mundo_07_v1.json --spec tools/level-generator/specs/sample_variants.json
```

`specs/sample_variants.json` son cuatro ciclos 241-320, uno por variante, con su estreno en p3-p5 (3 tableros), la titular 8
veces en su ciclo y su regreso como secundaria en el primer reto del ciclo siguiente. Los estrenos reales son 243, 283, 343 y
403, que no caben en cuatro ciclos seguidos (`--validate-spec` exige que cada mecánica de la tabla debute en el calendario y
una primera vez por ciclo): la muestra conserva el 243 de la Liana de cuenta y adelanta la Hoja atada, el Brote de dos hojas y
la Flor a 263, 283 y 303 [P]. Otros datos [P] de la muestra: los mundos 6 y 7, el `k_plan` de 101-200 (K 8-9), para que se
genere en menos de un minuto, y las temporadas 241 A · 261 B · 281 B · 301 A. Genera `tools/out/sample_variants/mundo_06_v1.json`
y `mundo_07_v1.json` (`pack_schema` 3) y `tools/out/sample_variants_report.md`.

`--self-test variants` comprueba los códigos (Li frente a Ln y Br frente a B2; una tabla `mechanics` con un código que no
está en `MechanicCodes.All` falla al cargar la especificación con «codigo de mecanica desconocido», probado con una copia
temporal de la muestra con una fila `Xx`, sin fixture en `specs/tests`), que un tablero sin variantes sale igual y gasta
los mismos números con el orden nuevo que con el de antes, cada paso sobre un tablero base fijo con 40 semillas (colocación
válida según `LevelValidation`, los campos de b.3 y determinismo), la `key2` en `MapColors` y `Permuted`, el estreno de cada
variante en la muestra (un candidato en ventana con un ejemplar, 2 vacíos y `bot_fail` < 0,20; su JSON con los `attrs` de la
firma, `intro_mechanic`, `skippable` = false y `pack_schema` 3, leído de vuelta con el lector del generador con el mismo
tablero y el mismo hash, que el `PackReader` de level-qa comprueba en su `--self-test key2`; la Flor sin `key2` no se
construye; el alivio no quita llaves), la muestra limpia en `--validate-spec` y los tres fixtures negativos.

## Tabla de temporada del cliente (`--seasons`; SE1)

La temporada de cada ciclo de 20 (A = aguas altas, B = aguas bajas; D-009, punto 6; Q11 A de D-007) no va en el pack: el
cliente la lee de **una tabla por ciclo, copiada del calendario versionado** (pregunta 32 de `docs/GDD.md` §6.3, decidida
el 2026-10-13: (b), D-020; 03 §12; 08, «Preguntas abiertas» 12). Solo cambia la LUT del fondo, sin cartel (la lee SE2).

```
dotnet run --project tools/level-generator -- --seasons tools/level-generator/specs/campaign_v1.json tools/level-generator/specs/campaign_v2.json --until 300 --out Assets/_Project/Data/Levels/seasons_v1.json
dotnet run --project tools/level-generator -- --seasons tools/level-generator/specs/campaign_v1.json tools/level-generator/specs/campaign_v2.json --until 300 --out Assets/_Project/Data/Levels/seasons_v1.json --check
dotnet run --project tools/level-generator -- --self-test seasons
```

`--until <nivel>` (P7a): la tabla acaba en el ciclo que termina en ese nivel, el último publicado en el cliente (300 desde P7a;
P7b, P7c y P7d lo suben a 420, 560 y 700 con sus mundos, y con él `SeasonTableWriter.PublishedUntil`). Las especificaciones se
comprueban enteras; solo se escriben sus ciclos hasta ese nivel, y `source` lleva solo las que aportan alguno (con `--until
200`, la tabla de `campaign_v1.json` sola, byte a byte). Un `--until` que no es el último nivel de un ciclo de las
especificaciones sale con 1 y no escribe.

Formato [P] `af-seasons/1` (`Generation/SeasonTableWriter.cs`; no es un contrato del GDD §7 todavía):

```
{
  "format": "af-seasons/1",
  "source": "tools/level-generator/specs/campaign_v1.json, tools/level-generator/specs/campaign_v2.json",
  "cycles": [
    {"from_level": 21, "season": "B"},
    ...
    {"from_level": 281, "season": "A"}
  ]
}
```

- `source`: la ruta de cada especificación, relativa a la raíz del repositorio (no a la carpeta desde la que se ejecuta) y
  con `/`; con varias, separadas por «, » en el orden de sus ciclos [P].
- `cycles`: un ciclo por línea, en orden y sin huecos; cada uno va de `from_level` a `from_level` + 19. Los niveles 1-20
  (tutorial y rampa) no tienen temporada. Hoy (P7a): 14 ciclos 21-281, los 9 de `campaign_v1.json` (21-181, idénticos a su
  `seasons`) y los 5 del mundo 6 de `campaign_v2.json` (201 A, 221 A, 241 B, 261 B y 281 A; d.5 punto 4). Sin `--until`, con
  `campaign_v2.json` entera serían 34 ciclos 21-681 (`--self-test registry` lo comprueba); P7b-P7d la amplían con sus mundos.
- UTF-8 sin BOM, LF y claves en orden fijo: la misma especificación da los mismos bytes.

La herramienta copia y comprueba el formato: cada ciclo de la especificación (de `cycle.from_level` a `last_level`) con A o
B, ninguna entrada de `seasons` fuera de un inicio de ciclo y, con varias especificaciones (`campaign_v2.json` para
201-700: `--seasons campaign_v1.json campaign_v2.json`), contiguas y sin solaparse. Si no, sale con
1 y no escribe. Las reglas del calendario (temporadas alternas, el ciclo de debut toma la de su mecánica) son de
`--validate-spec` (SEASONS). `--check` no escribe y sale con 1 si la tabla del disco no coincide byte a byte (desfase con
el calendario). `--self-test seasons` comprueba los ciclos 21-181 frente a `campaign_v1.json` (leído aparte), la temporada
de cada nivel 21-200 frente a `CampaignSpec.SeasonOf`, el formato, el determinismo, el desfase con la tabla del disco (la de
`campaign_v1.json` + `campaign_v2.json` hasta `PublishedUntil`, con los ciclos de `campaign_v2.json` y sus temporadas), el
corte de `--until` (hasta el 200, la tabla de `campaign_v1.json`; hasta el 60, dos ciclos), la unión de dos especificaciones y
los rechazos (también `--until` a mitad de ciclo o fuera de las especificaciones), entre ellos el fixture
`specs/tests/seasons_invalid_letter.json` (ciclo 61 con temporada C, que también falla `--validate-spec` por SEASONS).
