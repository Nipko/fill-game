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
| `--self-test [tinaja]` | Pruebas de la herramienta sin proyecto de tests (hoy, la Tinaja) | PASS / FAIL por consola |

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
| GAP-120 | ninguna mecánica ni capacidad (Cuia chica, Cuia alta) pasa más de 120 niveles sin volver | resumen 6; d.2 regla 4; 02 §8 |
| FIRST-TIME | al menos una primera vez por ciclo | d.2 regla 3 |
| CAPACITIES | ≤ 2 Cuias chicas (con la cueva) y ≤ 1 Cuia alta antes del 200; Cuia alta de 6 solo en hitos desde el 141 | 02 §8 |
| LIANA-CAPS | la Liana nunca va con Cuia chica, Cuia alta ni Piraña: `AF.Core` la rechaza (C1-9) y el generador no quita nada de la celda | C1-9 |

Con `campaign_v1.json`: 21 reglas, 0 fallos. Las reglas comprueban el calendario; lo publicado lo comprueba `level-qa`
(QA-MECH-CELL y QA-GAP-120).

### Fixtures negativos: `specs/tests/*.json`

Cada fixture «extiende» `campaign_v1.json` (`"extends"`), cambia solo las celdas que trae y declara la regla que debe
fallar (`"expect_rule"`). El validador sale con 1 e imprime `esperado: [REGLA] presente`. Para pasarlos todos (Git Bash):

```
for f in tools/level-generator/specs/tests/*.json; do
  out=$(dotnet run --project tools/level-generator --no-build -- --validate-spec "$f"); code=$?
  echo "$(basename "$f"): exit=$code $(echo "$out" | grep '^esperado')"
done
```

Todos deben dar `exit=1` y `presente`. Los de la Tinaja (`tinaja_in_recovery.json` y `tinaja_in_remanso.json`) extienden
`specs/sample_tinaja.json` y fallan por CAL-ROLE: p1, p2 y p19 son niveles puros, sin tinaja (D-019, Q-Ti1 A).

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
defecto 11-200).

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
5. **Packs** (`PackWriter`): `pack_schema` 2, o 3 si algún nivel del pack lleva la tinaja (D-019), pack a pack (la
   cabecera del informe dice «2 (3 en los packs con tinaja: …)» si una campaña tiene de los dos; FIXG); minificado con
   un nivel por línea, UTF-8 sin BOM y LF, decimales fijos.

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
