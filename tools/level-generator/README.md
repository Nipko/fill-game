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
| `--validate-spec <campaign.json>` | Comprueba la especificación de la campaña 11-200 | Resumen ASCII por ciclo |
| `--parity [--report <md>]` | Paridad de las métricas con el prototipo (03 §17-§18; 09 R26) | `tools/out/parity_report.md` |
| `--spec <campaign.json> --levels a-b --out <carpeta> [--report <md>] [--check]` | Genera la campaña (03 §2) | `Assets/_Project/Data/Levels/mundo_0N_v1.json` y `tools/out/campaign_v1_report.md` |

Códigos de salida: 0 = correcto; 1 = alguna comprobación falla; 2 = uso incorrecto o especificación ilegible.

## Especificación de la campaña: `specs/campaign_v1.json`

Datos versionados, no reglas. Los leen el generador y `tools/level-qa` (un solo lector, `Campaign/CampaignSpec.cs`,
que `level-qa` enlaza). Fuentes:

- **D-009** (2026-10-08; Q1 A, Q2 sí, Q3 A, Q4 A): ciclo de 20 con retos en p6, p9, p12, p15 y p18, remanso puro en
  p19 e hito en p20; propuesta `docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md` c.1-c.4 y c.8.
- **D-007** (2026-10-08): debuts Hoja 43, Matapi 63, Liana con fruta 83, Aguas turbias 100, Remolino 103, Barro 123,
  Lluvia 143, Brote 163 y Piraña 183; Cuia chica 14/18/22 y Cuia alta 27/160 (02 §8); combinaciones de d.3 y Q14 A.
- **Calendario d.4** (21-200) literal, más la rampa 11-20 (d.4, «Niveles 1-20»). Leyenda: R, C y S puros; ★ estreno;
  (s) reto suave; (D) apunta a Difícil; (MD) apunta a Muy difícil; (tinte X) debut de tinte. Los nombres de hito no
  pasan al pack (pendientes del asesor).
- **GDD vigente** donde no cambia: rangos de 03 §11 (K, cuias, vacíos, par objetivo y porcentajes de etiquetas),
  pool de tintes del glosario §8-§9, pares de riesgo y `max_pares_riesgo` (03 §4.5), mundos 2-5 y agua base
  (glosario §8, §11).
- **[P] de la especificación**: K por rol (`k_plan`, limitado por el pool y por los pares de riesgo), ventanas de
  bot de los retos (41-59 con etiqueta desde 75 %, suaves 50-74 %, desde el 61 75-89 %), «Ch» = Cuia chica de 2.

## `--validate-spec`

```
dotnet run --project tools/level-generator -- --validate-spec tools/level-generator/specs/campaign_v1.json
```

Imprime por ciclo los niveles puros, los retos con su marca, las etiquetas objetivo (D y MD) y los debuts, y después
un `FAIL [regla]` por cada incumplimiento o un `AVISO`. Reglas (ids estables):

| Id | Regla | Fuente |
|---|---|---|
| CAL-COVER | calendario 11-200 sin huecos | — |
| CAL-ROLE | R solo en p1-p2, S solo en p19, p1-p2 y p19 puros | c.3, c.4 |
| LABELS | (s), (D) y (MD) en su posición | D-009; c.3 regla 7 |
| TITULAR-8 | cada titular sale 8 veces en su ciclo de debut | d.4 |
| PURE-30 | al menos un 30 % de niveles puros por ciclo | d.2 regla 1 |
| PURE-COMFORT | al menos 3 de las 8 comodidades de p7-p17 puras | c.4 |
| FORBIDDEN | turbias solo con filtros; un contador por nivel; Lluvia nunca con Piraña; Remolino nunca con Piraña [P] | d.3; Q14 A |
| MURKY-SLOT | turbias solo en slots con etiqueta | T1; b.2 |
| MURKY-2 | turbias como mucho 2 veces por ciclo | b.2 |
| BEFORE-DEBUT | nada antes de su debut | D-007; 02 §8 |
| DEBUT-LEVEL | el primer uso es el debut, con ★ | D-007; 02 §8 |
| DEBUT-SLOT | ningún debut en reto ni hito salvo las turbias en el 100; 2-3 estrenos seguidos con la mecánica sola | c.3 regla 6; 02 §9.2 regla 2 |
| MAX-2 | como mucho 2 mecánicas por nivel antes del 300 (las capacidades no cuentan [P]) | 02 §9.2 regla 4 |
| RETURN | la titular vuelve como secundaria en un reto o hito del ciclo siguiente | d.4 |
| CAP-CYCLE | 6 niveles por ciclo como mucho (8 en el de debut) | d.2 regla 2 |
| NO-3-ROW | nunca 3 niveles seguidos fuera del estreno | d.2 regla 2 |
| SEASONS | temporadas alternas, el ciclo de debut toma la de la mecánica | d.2 regla 7 |
| GAP-120 | ninguna mecánica pasa más de 120 niveles sin volver | resumen 6; d.2 regla 4 |
| FIRST-TIME | al menos una primera vez por ciclo | d.2 regla 3 |
| CAPACITIES | ≤ 2 Cuias chicas (con la cueva) y ≤ 1 Cuia alta antes del 200; Cuia alta de 6 solo en hitos desde el 141 | 02 §8 |
| LIANA-CAPS | aviso: Liana con capacidades mixtas, bloqueada en `AF.Core` (C1-9); el generador la relaja | C1-9 |

Con la especificación literal: 0 fallos y 3 avisos (95 Li+Al, 160 Ll+Li+Al6 y 198 Pi+Li).

### Fixtures negativos: `specs/tests/*.json`

Cada fixture «extiende» `campaign_v1.json` (`"extends"`), cambia solo las celdas que trae y declara la regla que debe
fallar (`"expect_rule"`). El validador sale con 1 e imprime `esperado: [REGLA] presente`. Para pasarlos todos (Git Bash):

```
for f in tools/level-generator/specs/tests/*.json; do
  out=$(dotnet run --project tools/level-generator --no-build -- --validate-spec "$f"); code=$?
  echo "$(basename "$f"): exit=$code $(echo "$out" | grep '^esperado')"
done
```

Todos deben dar `exit=1` y `presente`.

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
  prototipo los tiene). `bot_fail` y `D` no se comparan: el PRNG cambia (09 R26).

```
dotnet run --project tools/level-generator -c Release -- --parity
```

Resultado actual: par 40/40, complejidad 40/40, ramificación 40/40, trampas 40/40 y −1 color 39/39 (el nivel 19 del lote
híbrido no tiene −1 color en el prototipo).

## Generación de la campaña (`--spec`, `Generation/`)

```
dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/campaign_v1.json --levels 11-200 --out Assets/_Project/Data/Levels --report tools/out/campaign_v1_report.md
dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/campaign_v1.json --levels 11-200 --out Assets/_Project/Data/Levels --check
python tools/validate_schema.py --pack Assets/_Project/Data/Levels/mundo_02_v1.json Assets/_Project/Data/Levels/mundo_03_v1.json Assets/_Project/Data/Levels/mundo_04_v1.json Assets/_Project/Data/Levels/mundo_05_v1.json
dotnet run --project tools/level-qa -c Release -- Assets/_Project/Data/Levels/mundo_02_v1.json Assets/_Project/Data/Levels/mundo_03_v1.json Assets/_Project/Data/Levels/mundo_04_v1.json Assets/_Project/Data/Levels/mundo_05_v1.json --report tools/out/level_qa_report.md
```

Usa siempre `-c Release` (en Debug tarda varias veces más). `--check` regenera y compara byte a byte con los packs del
disco sin escribir nada (03 §17 #10). El índice D depende del pool de cada rango de 03 §11, así que un pack solo se
reproduce con los mismos `--levels` (por defecto 11-200).

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
   cada etiqueta (y D menor), anti-repetición (forma y Hamming, `Campaign/BoardShape.cs`) y saltos de +2,2 / −3,4 bandas
   como penalización. Si un ciclo no tiene colocación, se piden más candidatos a sus retos, hito y siguientes (hasta 2
   rondas); un reto o hito que quede fuera de su ventana se publica con la etiqueta que mide (c.2: sin etiqueta si mide
   menos del 75 %) y el informe lo registra.
4. **Alivio** (`Metrics/Relief.cs`) desde el nivel 31.
5. **Packs** (`PackWriter`): `pack_schema` 2, minificado con un nivel por línea, UTF-8 sin BOM y LF, decimales fijos.

Decisiones [P] de la generación (para revisar en la integración):

- La Cuia chica «cuenta como vacío» (02 §8): en un nivel con Cuia chica, cada una ocupa uno de los vacíos del rol y
  queda al menos un vacío de 4.
- La Hoja, el Matapi y la Lluvia ocupan un vacío: en los estrenos, uno de los 2 vacíos (ejemplo de 08 §4.1); en comodidad
  traen un vacío más; en retos e hitos usan los del rol.
- La Liana con capacidades mixtas está bloqueada en `AF.Core` (C1-9): en 95, 160 y 198 se quitan la Cuia chica y la Cuia
  alta y la cueva de la Piraña es de 4.
- Aguas turbias: 1-2 cuias y como mucho 3 dosis ocultas (el tope de 02 T2 es 4) para que la ruta segura cueste segundos.
- Un nivel turbio que no entre en su ventana se publica sin turbias (T1) y se registra.
- Sin `par_approx`: la Piraña deja el par exacto (C3-2).

Diagnóstico: con la variable de entorno `LEVELGEN_TRACE=<nivel>` el generador traza por stderr cada candidato de ese
nivel (fuente, K, vacíos, bot_fail, motivo del rechazo y milisegundos) y un resumen del slot. No cambia la salida.
