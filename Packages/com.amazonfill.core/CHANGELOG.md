# Changelog

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/); versiones con SemVer.

## [Sin publicar]

### Añadido

- Barro (C2-3; 02 R2, §1.2 y §9.1 M5; 03 §9; propuesta de D-007 b.2 M5; D-007, Q14 A: es un filtro; debut 123). No es
  una mecánica de cuia sino el color 15 dentro del contenido, así que lo tratan `BoardRules` y `CuiaRules` sin despacho:
  dosis inerte que corta las rachas; con Barro en la cima (`Cuia.MudExposed`, `barro_expuesto`) la cuia no es origen y,
  como destino, acepta cualquier color tras los filtros de mecánica (también llena: `barro_expuesto` va antes que
  `llena(B)` en `legal`); el primer vertido lava el barro y ocupa su lugar con `cantidad = min(racha, espacio + 1)`
  (`CuiaRules.EffectiveSpace`, `espacio_efectivo`); el lavado no revela la dosis de debajo (R7: nunca queda arriba); una
  cuia con Barro no está sellada ni vacía (R5, R12: `CuiaRules.IsSealed` exige un tinte). Evento BarroWashed:
  `PourEvents.BarroWashedMask` y `PourResult.BarroWashed`. Marea restaura el barro (estado anterior).
  `BoardState` y `BoardBuilder` aceptan el 15 y siguen rechazando el 16 (reservado); `units_per_color` no declara el Barro
  (no es un color). `LevelValidation`: solo en la capa 2 o 3 (índice 1-2) de una cuia de 4; nunca en una cuia con mecánica
  ni oculto [P]; las reglas de `U_c` y de casa ignoran el 15. Solver: el descarte rápido de destinos de `SolverMoves` no
  descarta el barro expuesto; heurística de 03 §9 (`h1` cuenta el barro como corte y resta 1 por barro; `h2` ignora el
  15; en `single`, `h3` no cuenta el barro [P]); sin Barro da los mismos valores (mismos pares y expandidos). Bot: exponer
  el Barro no puntúa el +8 de «deja expuesto un color» (no es un color) [P]. Tests compartidos `BarroTests` (cada regla
  con su §) y `BarroSolverTests` (A* con y sin poda contra la búsqueda exhaustiva y heurística admisible y consistente en
  32 tableros, uniformes y mixtos, en `run` y `single`; inyector `BarroInjector`: inserta el barro en la capa 2-3 sin
  quitar fruta); P1-P3 con Barro en `FutilePropertyTests` (unos 20.000 estados por combinación, sin violaciones) y
  `HotLoopAllocationTests.BarroBoard_HotLoop_DoesNotAllocate` (0 bytes en reglas y bot; solver calentado < 1 KB).
  Pendiente para el pipeline: 03 §9 dice que la superposición «sustituye la capa 2 o 3 por 15 y retira una unidad de ese
  color», lo que dejaría `U_c` = 3 en cuias de 4 (sin casa): los tests insertan el barro sin quitar fruta.
- Q13 de D-007, parte 1 (C1-1): `AF.Core.Rules.BoardRules`, reglas con contexto de tablero sobre
  `(Cuia[] cuias, offset, count, int[] U_c)` y sin asignaciones: `CanBeSource`, `IsLegal`, `Amount`, `IsFutile` con la
  estructura del anexo B (`cambia_al_recibir(B)` y `no_puede_recibir(A)` como ganchos de mecánica, hoy sin efecto),
  `ApplyInPlace` con R10 por etapas en el orden del anexo B ((a) revelado de A, (b) aperturas por recepción, (c) sellado,
  (d) liberaciones por sellado, (e) contadores una vez por vertido y solo si no es fútil, Q18 B, (f) condiciones, (f′) punto
  fijo de (c), (d) y (f); la sobrecarga con `futileRuleEnabled` evalúa además (g) victoria y (h) bloqueo), `EvaluateStatus`,
  `IsWon`, `HasUsefulMove` e `IsStuck`. Es la única implementación que aplica vertidos.
- `AF.Core.Rules.PourEvents` (lo ocurrido en cada etapa de R10), `PourRules.Apply(state, a, b, mode, out events)` y
  `PourResult.Events` (con un constructor nuevo; el anterior se conserva).
- `AF.Core.Mechanics.IMechanic` (interno): ganchos sin estado de las mecánicas (08 §2.2), despachados por una tabla estática
  indexada por el código de mecánica de la cuia (bits 39-42 de `Cuia.Packed`, hoy siempre 0); los ganchos solo se llaman
  para cuias con mecánica. `AF.Core` concede `InternalsVisibleTo("AF.Solver")` para que el solver recorra las mismas
  reglas por sus rutas internas.
- Liana con fruta y Liana de cuenta (C1-9; D-007: con fruta en el 83, de cuenta como variante en el 243; propuesta b.2 M3,
  b.3 y anexo B): `LianaMechanic` (interno). Atada no es origen ni destino (R11); con fruta (`key` = c) se suelta en la
  etapa (d) de R10 en cuanto existe otra cuia sellada de c; de cuenta (`n` = 1-2), cuando hay `n` cuias selladas; al
  soltarse pasa a `None` (`PourEvents.LianaReleasedMask`, evento LianaReleased); Marea y Volver a la orilla la vuelven a
  atar. Validación: exactamente uno de `key` o `n` (`BoardBuilder`), sin dosis ocultas en el nivel (Q14 A), la fruta está
  en el tablero y puede sellar fuera de la Liana; la de cuenta, con al menos `n` colores que pueden sellar fuera. Tests
  compartidos `LianaTests` (el obligatorio del anexo B «el vertido que sella c suelta la Liana en el mismo vertido», Marea,
  cuenta 1 y 2, validación) y `LianaSolverTests` (32 tableros por variante en `run` y `single`, heurística).
- **Bloqueo de C1-9 (pregunta de 02 R16 para el tech-lead):** P1 (R14) falla con Liana y capacidades mixtas. Contraejemplo
  alcanzable (`run`): Cuia alta de 5 con `[2,2,2,2]` (casa ocupada de la fruta abundante 1, U_1 = 5), `[1,1,1]` en una
  cuia de 4, Cuia chica de 3 vacía y Liana con fruta 2 con `[1,1]` en la otra cuia de 4: R13 declara Aguas estancadas (solo
  quedan vertidos fútiles) y hay solución en 4 vertidos empezando por el fútil `[1,1,1]` → chica. En búsquedas amplias
  (millones de estados) no aparece en el juego base mixto ni con Hoja, Brote o Matapi, ni con Liana y capacidades
  uniformes; sí con Liana, Cuia alta y Cuia chica juntas. Provisional y reversible: `LevelValidation` rechaza la Liana con
  capacidades mixtas; los casos de propiedad mixtos de la Liana quedan `Ignore` y `P1_Liana_MixedCapacities_OpenR16Counterexample`
  fija el contraejemplo. P1-P3 con Liana y capacidades uniformes pasan.
- Brote y Matapi, los filtros (C1-8). `BroteMechanic` (02 §9.1 M2, misma regla; debut 163 por D-007): cerrado no es
  origen, como destino sigue la regla normal y al recibir su color germina en la etapa (b) y pasa a `None`
  (`PourEvents.BroteOpenedMask`); validación: 1-3 dosis visibles, sin ocultas y sin llenar. `MatapiMechanic` (D-007, debut
  63; propuesta b.2 M10, reglas 1-8): nunca es origen (vertido ilegal, R11), como destino la regla normal, nunca fútil como
  destino, acaba sellado o vacío (R12); validación: vacío o 1-3 dosis visibles de un color, sin Barro ni ocultas y, con
  dosis, capacidad = `U_c` de su color [P, derivado de R12]. La excepción de la Piraña (regla 7) llega con C3-2. Tests
  compartidos `BroteTests`, `MatapiTests` y `FilterSolverTests` (32 tableros por mecánica en `run` y `single`, A* con y
  sin poda contra la búsqueda exhaustiva y heurística admisible y consistente); P1-P3 con cada filtro en
  `FutilePropertyTests`; tablero con Hoja, Brote y Matapi sin asignaciones en `HotLoopAllocationTests`.
- Hallazgo para la integración (02 R16; 03 §5), medido en `FutilePropertyTests.R16_MixedCapacities_PrunedParVersusExhaustivePar`:
  con capacidades mixtas, P1 se cumple (nunca se declara Aguas estancadas con solución), pero podar los fútiles
  (`MOVIMIENTOS`) puede alargar el par o dejar sin camino útil a la victoria un tablero que lo tiene: en 309 tableros
  mixtos pequeños, 3 en `run` (1 más largo y 2 sin camino) y 5 en `single`. Ejemplo en `run`: `[cap 5: 2]`,
  `[2,2,1,3]`, `[3,3,1]`, `[cap 1: 1]`, `[2,3,1,1]` (U_1 = 5): la solución (9 vertidos) necesita llevar el 2 suelto de
  la Cuia alta (la casa de la fruta abundante 1) a la Cuia chica, un vertido fútil según 02 §1.4 porque la chica no es la
  casa del 2. Los tests de solver comparan ahora A* con poda con la búsqueda con los mismos movimientos útiles y A* sin
  poda con la búsqueda completa; con capacidades uniformes exigen además que coincidan.
- Hoja (C1-7; 02 §9.1 M1 y §9.2; 03 §9; propuesta de D-007 b.2 M1 y anexo B; debut 43): `HojaMechanic` (interno),
  registrada en la tabla de mecánicas y aceptada por `BoardBuilder`. Cuia vacía con hoja y llave `key`: solo recibe
  `key` (en `run`, el tramo que cabe), no es origen mientras tenga la hoja, la hoja cae con el primer vertido en la etapa
  (b) de R10 y la cuia pasa a `MechanicType.None`; como destino nunca es fútil (`cambia_al_recibir`); Marea y Volver a la
  orilla la devuelven. `PourEvents.HojaOpenedMask` (evento HojaOpened). Validación: empieza vacía y su llave está en el
  tablero (03 §17 #6). Tests compartidos `HojaTests` (cada regla con su §, el caso del anexo B «verter el resto de una
  fruta en su Hoja vacía no es fútil» en un tablero que con la regla anterior se declaraba atascado) y `HojaSolverTests`
  (par de A* = búsqueda exhaustiva en 32 tableros pequeños, uniformes y mixtos, en `run` y `single`; heurística admisible y
  consistente en todos los estados alcanzables), con el generador compartido `MechanicBoards` e `IMechanicInjector`
  (también lo usa `DotnetOnly/PropertyBoards`, ahora con el PRNG del núcleo); P1-P3 con Hoja en `FutilePropertyTests`
  (P3 precisado: un vaivén monocolor ↔ vacía útil en los dos sentidos no puede volver al mismo estado; con la Hoja de 1 que
  cae y deja ir su fruta a la casa correcta sí hay vaivenes útiles, con progreso) y el bucle caliente sin asignaciones con
  Hoja en `HotLoopAllocationTests`.
- Barajado inverso (C1-6; 03 §4.1-§4.2): `AF.Solver.Generation.InverseMoves` (inverso exacto de un vertido útil:
  el directo `b → a` es legal, mueve exactamente `m` dosis en el modo y no es fútil, evaluado con `BoardRules`; en `run`
  equivale a las condiciones literales de 03 §4.1 y en `single` a su nota), `InverseWalk` (`WALK` con mejor prefijo: los 3
  mejores candidatos no visitados, puntuación entera `NextInt(2·65.536) − 4·65.536·[vacíos < E]`, hasta 200
  sub-semillas), `InverseDeep` (`DEEP`: DFS con retroceso, candidatos barajados con Fisher-Yates, 20.000 nodos y 5
  intentos), `InverseSpec` (K, E, capacidades y `U_c`, con el tablero resuelto del prototipo; Cuia chica y Cuia alta),
  `InverseResult`, `Complexity` (laicwben2) y `AF.Solver.Collections.StateSet` (visitados con comparación completa). Sin
  asignaciones por paso. Lote completo (200 semillas × K 5-11 × E 1-2, Release): todos los tableros válidos y resueltos por
  A*; `walk` con profundidad mediana 10-23 y máxima 15-31 y 0,06-0,46 ms por intento; `deep` con E = 1 y K 8-11, par
  mediano 19-23 (2K+1..2K+3 = 17-25) y 19-34 ms por nivel. Tests compartidos `InverseGenerationTests`; solo dotnet
  `InverseGenerationPropertyTests` (lote reducido siempre y lote completo explícito, categoría `Informe`).
- Bot casual ×40 (C1-5; 03 §6-§7; 08 §2.2 y §14.2): `AF.Solver.Bot.CasualBot.FailRate(state, seed, options)` con
  `BotOptions` (40 partidas, 85/15, tope 400; `ForPipeline(mode)`) y `BotResult` (fallos, partidas y `Rate`). Juega sobre
  `SolverMoves` en orden canónico y aplica con `BoardRules`, así que admite cada mecánica portada sin código propio.
  `PUNTUACION_CASUAL` congelada en enteros ×4 [P] (+16 juntar, +40 sellar = la cuia que recibe queda llena y monocolor,
  −20 vacío, −12 parcial, +8 color expuesto con destino; con mecánicas puntúa la cuia que recibe) y ruido entero
  `NextInt(131.072)` sobre una escala ×65.536, equivalente a «+ rng()·0,5». Semilla del nivel `SeedFor(seed)` =
  `SubSeed(SeedFrom(seed), 0xB07)`. Sin asignaciones una vez calentado.
- `AF.Solver.Search.EarlyTraps` (C1-5; Q16 B): `Count` = primeros vertidos útiles distintos tras los que el tablero queda
  atascado en ese vertido o puede quedarlo en el siguiente [P]; `CountUnavoidable`, la lectura estricta. En el tutorial v0
  solo los niveles 6 y 8 tienen trampas (1 cada uno), como el camino a Aguas estancadas que mide `tools/level-generator`.
- Informe del bot frente al prototipo (`DotnetOnly/BotParityTests`, 40 niveles de `tools/out*/levels_proto.json`): misma
  etiqueta en 37/40 y diferencia media 0,059 (criterio [P]: 75 % y 0,10). Bot ×40 en un tablero de 12 cuias, Release:
  0,2-6,8 ms (Python: 20-63 ms, 03 §18). Tests compartidos `CasualBotTests`.
- PRNG determinista (C1-4; 09 R26, decidido el 2026-10-06: PRNG nuevo en C# y paridad sobre los estados exportados):
  `AF.Core.Determinism.Prng`, xoshiro256** (Blackman y Vigna) sembrado con SplitMix64, escrito desde cero; semilla del
  nivel = FNV-1a de 64 bits de la cadena UTF-8 `amazonfill:v1:m{mm}:v{v}:n{nnnn}` (`LevelSeedText`, `LevelSeed`,
  `SeedFrom`), `SubSeed(seed, stream)`, `NextULong`, `NextUInt` y `NextInt(bound)` sin sesgo (Lemire con rechazo), solo
  enteros y sin asignar tras construirlo (`Reseed`). Valores dorados de las 16 primeras salidas de tres semillas en el test
  compartido `PrngTests` (calculados con una implementación independiente en Python); χ² con 10.000 muestras en
  `DotnetOnly/PrngDistributionTests`.
- `AF.Solver.Search.SolverMoves.Collect` y `SolverMove` (C1-4): `MOVIMIENTOS` de 03 §5 con la cantidad del modo y la
  deduplicación del solver, públicos y sin asignaciones (test `SolverMoves_DoNotAllocate`); `AStarSolver` expande con ellos.
  Tests compartidos `SolverMovesTests`. Mismo par en los 140 niveles de paridad y mismos expandidos.
- Pruebas de propiedad de R13, R14 y R16 (C1-2; Q13 de D-007, parte 2), solo dotnet: `DotnetOnly/PropertyBoards`
  (tableros pequeños con semilla fija, capacidades uniformes o mixtas con Cuia chica 1-3 y Cuia alta 5-6 con su fruta
  abundante, e `IMechanicInjector` para que cada mecánica añada sus casos antes de su debut, 02 R16) y
  `DotnetOnly/FutilePropertyTests` sobre todos los estados alcanzables (BFS de `ExhaustiveSearch`), en `run` y en `single`:
  P1 (R14) todo estado atascado es irresoluble; P2 (R16, `run`) un fútil entre dos cuias sin mecánica y de la misma
  capacidad no cambia el estado canónico; P3 (anexo B) ningún par monocolor ↔ vacía es útil en los dos sentidos. Unos
  20.000 estados por combinación (modo × familia), 0 violaciones, unos 1,5 s en total. Con capacidades distintas un fútil sí
  puede cambiar el estado canónico (test documentado: precisión del texto de R16); la garantía que se prueba es P1.
- Test compartido documentado del anexo B: «no existe un ciclo de vertidos útiles» es falso ya en el juego base
  (A = [1, 2, 2, 2], B = [3, 3, 2]); R13 es correcto, no completo, y P1-P3 lo sustituyen.
- Atributos por cuia (C1-3; 08 §4.1 `{type, key, partner, n, k}`; 03 §3 y §9): `Cuia` empaqueta en los bits 39-60 de
  `Packed` el tipo (`AF.Core.Mechanics.MechanicType`: `None`, `Hoja`, `Brote`, `Liana`, `Matapi`, `Remolino`, `Lluvia`,
  `Pirana`; 4 bits), `key` (tinte − 1), el id de pareja del Remolino, `n`, `k`, el contador y las banderas
  (`Cuia.FlagClosed`: Hoja puesta, Brote cerrado); los bits libres (30-31, 35 y 61-63) valen 0. Accesores
  `Mechanic`, `HasMechanic`, `MechanicKey`, `MechanicPairId`, `MechanicN`, `MechanicK`, `MechanicCounter` y
  `MechanicFlags`; `WithMechanic` y `WithoutMechanic` (una mecánica abierta o suelta deja la cuia normal, con el mismo
  `Packed`). Una cuia sin mecánica conserva su `Packed`: los tableros sin `attrs` dan el mismo hash que antes.
- `MechanicTypes` (claves `"hoja"`…`"pirana"`), `MechanicSpec`, `Board.UnitsPerColor` (forma entera y objeto),
  `Board.BoardBuilder.Build` (con `BoardBuildResult`: tablero o motivo del rechazo) y `Board.LevelValidation.Validate`.
- Tests compartidos `CuiaAttrTests` y `BoardBuilderTests`, y en `BoardCanonTests` la clave canónica con mecánicas
  (permutaciones, tipo, `key`, pareja, `n`, `k`, banderas y contador) y los vectores fijos sin `attrs`.
- Tests compartidos `R10OrderTests` (orden de R10: el revelado entra en el estado que evalúa el sellado; la victoria antes
  que el bloqueo; índices relativos con `offset`; `single`) y regresiones 1-3 de 04 §5.1 sobre `BoardRules` en
  `FutileRulesTests` (la 4 queda para C2-4 y C3-1).

- Modo de vertido (decisión del dueño D-005 del 2026-10-08, que sustituye a D-001; `docs/dev/decisiones.md`):
  `AF.Core.Rules.PourMode { Run = 0, Single = 1 }` y `PourModes` (`Default = Run`, de modo que `default(PourMode)` es el
  modo por defecto; `TryParse`/`ToKey` con los valores `"run"`/`"single"` de la clave de Remote Config `rules_pour_mode`,
  y un valor desconocido da `PourModes.Default`; `IsDefined`). `Run` es la regla de 0.1.0 (02 R6, R9): mueve la racha
  visible que cabe, con vertido parcial, como Magic Sort. `Single`, solo para pruebas, mueve exactamente una dosis aunque
  haya racha (no existe el vertido parcial). El modo solo cambia `cantidad(A, B)` y se aplica en un único sitio,
  `CuiaRules.Amount(from, to, mode)`; `legal`, `futil`, `movimientos_utiles` (salvo `Amount`), sellado, victoria y
  bloqueo no dependen de él.
- Sobrecargas con modo: `PourRules.Amount/Apply(state, a, b, mode)`, `PourRules.CollectUsefulMoves(state, futile, mode,
  buffer)`, `LevelSession(initial, futile, pourMode)` y su propiedad `PourMode` (Marea deshace una dosis por uso en
  `Single`), `SolverOptions(budget, weight, futile, pourMode)`, `SolverOptions.PourMode` y `SolverOptions.ForPipeline(mode)`.
- `AStarSolver` resuelve en los dos modos; en `Single` usa `H_single = max(Σ (dosis − racha del fondo), h2)` [P]
  (decisiones.md §2.1.3), admisible y consistente: `SolverHeuristic.Estimate(..., mode)` y `DosesAboveBottomRun`.
- Tests compartidos `PourModeTests` (cantidad, legalidad, fútiles y bloqueo con los ejemplos E1-E6, partida y Marea, en
  los dos modos) y `AStarSolverPourModeTests` (par de A* = búsqueda exhaustiva con y sin poda; heurísticas admisibles y
  consistentes en todos los estados alcanzables). Solo-dotnet: `PourModePropertyTests` (criterios A1-A4 sobre 400 + 240
  tableros aleatorios con semilla fija, el pack del tutorial y las cifras de decisiones.md §2.1.3-§2.1.4) y
  `HotLoopAllocationTests` en `Single` y en `Run` con el modo explícito (`UsefulMovesInRun_DoNotAllocate`,
  `WarmSolverInRun_OnlyAllocatesTheSolution`).

- `AF.Core.Rules.CuiaRules`: sellado, `legal`, `cantidad` y `futil` (02 §1.4 con la excepción de Aguas turbias de 04 §5.1)
  sobre cuias sueltas; única implementación para tablero, solver y bot.
- `AF.Core.Rules.Pour` y, en `PourRules`, `MaxPours`, `IsFutile`, `CollectUsefulMoves`, `HasUsefulMove` e `IsStuck`
  (02 R13, R14, R16; kill-switch `futile_rule_enabled`).
- `AF.Core.Board.BoardCanon` (orden canónico y FNV-1a de 64 bits); `BoardState` implementa `IEquatable<BoardState>` y
  añade `HasHidden` y `CanonicalHash()`.
- `AF.Core.Session`: `LevelSession` (vertidos con R10, Marea, Volver a la orilla e historial), `LevelStatus` y `PourResult`
  (contrato a de `docs/dev/prototipo-v0.md`).
- `AF.Solver.Search`: `AStarSolver`, `SolverOptions`, `SolveResult`, `SolveStatus` y `SolverHeuristic` (03 §3, §5).
- Tests compartidos `FutileRulesTests`, `LevelSessionTests`, `RuleCatalogTests` (R1-R16), `BoardCanonTests`,
  `AStarSolverTests` y `SolverHeuristicTests`; tests solo-dotnet de paridad con el prototipo y de asignaciones.

- `CuiaRules.CanBeSource` y `PourRules.CanBeSource`: "A no vacía ni sellada" (02 §1.2; C1-C2 de 02 §3.1), única
  definición del origen; la usan `IsLegal`, `CollectUsefulMoves`, `HasUsefulMove`, `AStarSolver` y el control tap-tap del
  cliente (antes la repetía `TapController`). Aquí se añadirá `bloqueada_origen` (Hoja, Brote, Liana; 02 §9).
- Tests compartidos: `CanBeSource` (`PourRulesTests`) y regresiones 2 y 3 de 04 §5.1 contra el solver
  (`AStarSolverTests.SameCapacityMonocolorToEmpty_IsNeverExpanded`, `HiddenMonocolorToEmpty_IsExpanded`); solver reutilizado
  con un tablero más ancho.

### Cambiado

- C1-1: `PourRules` (legal, cantidad, `Apply`, fútiles, movimientos útiles y bloqueo), `BoardState.IsWon` y su vertido
  interno, `LevelSession.TryPour` (R10 (a)-(f′) con `BoardRules` y después (g)-(h)) y `AStarSolver` (`Expand`, la meta y
  `BuildSolution`) delegan en `BoardRules`. La API pública que usa `Assets/` no cambia; `CuiaRules` queda como envoltorio
  heredado de cuias sin mecánica. Mismo par en los 140 niveles de paridad y mismos expandidos en el tablero de 14 cuias.
- Nota: antes de publicarse, D-001 tuvo `PourMode { Single = 0, Run = 1 }` con `Default = Single`; ninguna versión
  publicada lo llevó. El estado de esta versión es el de «Añadido» (D-005).
- Las firmas sin modo (`CuiaRules.Amount(from, to)`, `PourRules.Amount/Apply/CollectUsefulMoves` sin modo,
  `LevelSession(initial, futile)`, `SolverOptions(budget, weight, futile)` y `SolverOptions.Pipeline`) quedan como
  **heredadas**: equivalen a `Run` y conservan el comportamiento anterior para que Unity y `tools/` compilen y den lo mismo
  hasta que T3 pase el modo; las retira T6. `SolverHeuristic.Estimate` sin modo sigue siendo `H` (03 §5).
- `LevelSession.TryPour` aplica la cantidad ya calculada (una sola comprobación de legalidad por vertido).
- `PourRules.IsLegal`, `Amount` y `BoardState.IsSealed` delegan en `CuiaRules` (mismo comportamiento).
- `AStarSolver`: a igual `f` expande primero el de **menor** `g`, como la tupla `(f, g, S)` de 03 §5 y
  `(f, g, contador, clave)` de `levelgen_proto.py` (antes, mayor `g`). Con `w = 1` el par no cambia (las pruebas de paridad
  lo confirman); con `w = 2` la solución subóptima coincide en criterio con el prototipo. Coste: +36 % de expandidos en el
  tablero de 14 cuias de referencia.
- `AStarSolver`: los nodos guardan `T` cuias con paso `T` (antes reservaba 16 por nodo: 2,3 veces lo necesario con 7
  cuias); el array se redimensiona al empezar una búsqueda con un tablero más ancho, fuera del bucle de expansión.

## [0.1.0] - 2026-10-06

### Añadido

- Andamiaje del paquete embebido: `AF.Core` y `AF.Solver` con `noEngineReferences: true`, y asmdefs de tests de editor.
- `AF.Core.Board.Cuia`, `AF.Core.Board.BoardState` y `AF.Core.Rules.PourRules`: estado de cuias, sellado, victoria
  y vertido legal (parcial incluido) del núcleo canónico, sin mecánicas (GDD 02 §1.1-1.3).
- `AF.Solver.Collections.MinHeap`: montículo de mínimos con desempate FIFO.
- Polyfill interno `IsExternalInit` para `init`/`record` en C# 9.

### Cambiado

- Nombre del paquete `com.amazonfill.core` (nombre interno del proyecto, sin la marca comercial; antes `com.aquazonia.core`,
  marca descartada el 2026-10-06). Los GUID de los `.meta` se conservan.
