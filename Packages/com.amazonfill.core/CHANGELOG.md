# Changelog

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/); versiones con SemVer.

## [Sin publicar]

### Pendiente de integración (obligatorio antes del merge)

- **Aguas estancadas por bucle (T51): no integrar hasta la respuesta del dueño a la pregunta 46 de `docs/GDD.md` §6.3 (D-014).**
  `DeadEndProbe` aplica la opción (a), la recomendada: el cierre recorre todos los vertidos legales, fútiles incluidos, y no solo
  los útiles que dice el texto de D-014 y de T51. Si el dueño elige (b) o (c), cambian `DeadEndProbe.Expand`, sus tests y el del
  66 (con (b), `if (futile) continue;` antes de aplicar, el bloqueo saltaría tras el vertido 6 y
  `RuleCatalogTests.R14_ASolvableBoardWithUsefulPours_IsNeverDeclaredStuck_AlongItsSolution` fallaría al empezar: es el falso
  bloqueo que (b) acepta). Desvío respecto a T51, que
  sigue de la opción (a): en la secuencia del dueño, Aguas estancadas sale justo tras el 9.º vertido (`1>5`), cuando el nivel
  queda perdido de verdad, y no tras el 12.º (`R13b_Level66_TheOwnersSequence_IsStuckRightAfterTheNinthPour`). Riesgo al
  reanudar: una partida guardada con un build anterior a mitad de un bucle (con vertidos posteriores a un estado que ahora es
  `Stuck`, como la del dueño en el 66 con 12) no se puede rehacer, porque `TryPour` devuelve false fuera de `Playing`
  (`LevelFlow.Resume`), y el nivel vuelve a empezar (02 §14; 04 §10.2). Va también en la descripción del PR y en la nota para
  el dueño.
- **`.meta` del paquete y verificación en Unity** (regla dura 3; README, «Paso obligatorio de integración en Unity»). La rama
  `fase1-mecanicas-niveles` se escribió sin Editor: los 59 archivos `.cs` nuevos del paquete (32 en `Runtime/` y 27 en
  `Tests/Editor/`) y las 5 carpetas nuevas (`Runtime/AF.Core/Determinism`, `Runtime/AF.Core/Mechanics`, `Runtime/AF.Solver/Bot`,
  `Runtime/AF.Solver/Generation` y `Runtime/AF.Solver/Murky`) no tienen `.meta`, y los 467 tests compartidos solo se han ejecutado
  en `dotnet` (un ensamblado; en Unity son dos, `AF.Core.Tests` y `AF.Solver.Tests`). Antes del merge, en esta misma rama:
  importarla en Unity en un worktree sin Editor abierto (`scripts/unity-compile.ps1` o el Editor), commitear los 64 `.meta`
  generados (nunca a mano) junto a los packs `mundo_02_v1` a `mundo_05_v1` y sus `.meta` (pendientes por el mismo motivo desde
  `def28bc` y `57b31c6`), y pasar `scripts/unity-compile.ps1` y `scripts/unity-test.ps1 -Mode EditMode` con código 0. Va
  también en la descripción del PR.

### Añadido

- **Aguas estancadas por bucle, R13 ampliada** (T51; D-014, decisión del dueño del 2026-10-13; 02 R13, R14; defecto del nivel 66
  en `docs/dev/traspaso/pruebas-telefono-2026-10-13.md`). `AF.Core.Session.DeadEndProbe`: cierre acotado de los estados
  alcanzables desde S, en anchura y en orden determinista, con estados canónicos (`BoardCanon` y FNV-1a de 64 bits; igualdad
  confirmada por valor) y los vertidos aplicados con `BoardRules`; `IsDeadEnd` = el cierre se agota con ≤ `MaxStates` (64 [P],
  `DeadEndProbeMaxStates`, constante del núcleo) estados y ninguno es victoria. `LevelSession` lo usa en su evaluación (h) cuando R13
  da `Playing` (constructor, `TryPour`, `Undo`, `Restart`, `Reveal`, `RevealAll` y `RestoreRevealed`): el 66 sale en Aguas estancadas
  justo tras el 9.º vertido del dueño (`1>5`), cuando el nivel queda perdido de verdad; los vertidos 10-12 ya eran el bucle. Más de 64
  estados o una victoria alcanzable: sigue en `Playing` (conservador). Aguas turbias: usa el estado verdadero; contadores de Lluvia y
  Piraña: parte del estado. **Recorre todos los vertidos legales, también los fútiles**, aunque el texto de la decisión diga «útiles»:
  con capacidades mixtas un fútil puede ser parte de la única salida (C1-8), y solo con útiles el cierre declararía tableros que se
  ganan, contra la condición de la misma decisión y R14. En el 66, tras el 6.º vertido, la búsqueda con útiles se agota en 50
  estados y el nivel se gana en 14 con algún fútil; en partidas al azar sobre los niveles 1-200, un cierre solo con útiles habría
  declarado 327 estados, 56 ganables (con todos los legales: 271, ninguno ganable). Con capacidades iguales los dos son idénticos (P2).
  No depende de `futile_rule_enabled`. Es la opción (a) de la pregunta 46 de `docs/GDD.md` §6.3 (T50), la recomendada: pendiente de
  la confirmación del dueño antes del merge (ver «Pendiente de integración»). Sin asignaciones tras calentar y < 1 ms en Release (0,015 ms en el 66; peor
  medido 0,322 ms en 4.026 estados de los tableros de referencia). Sin cambios en `BoardRules`, `PourRules`, el solver, el bot, `EarlyTraps`,
  `LevelValidation`, el generador ni level-qa: packs, informes y pares idénticos (`--check`). Tests compartidos `DeadEndProbeTests`
  (`R13b_Level66_Loop_IsStuck`, `_TheOwnersSequence_IsStuckRightAfterTheNinthPour`,
  `_AWinnableStateWhoseUsefulClosureHasNoWin_IsPlaying`, `_MareaAndRestart_ReevaluateTheLoop`, orden canónico, los 10 niveles del
  tutorial a lo largo de su solución A*, cierre de más de 64, tablero mixto que solo se gana con fútiles, Aguas turbias, tablero
  ganado) y `RuleCatalogTests` (`R13b_AnUnsolvableBoardWithFewReachableStates_IsStuck` y
  `R14_ASolvableBoardWithUsefulPours_IsNeverDeclaredStuck_AlongItsSolution`); solo dotnet `DeadEndProbePropertyTests` (si el
  cierre declara, A* exhaustivo da `Unsolvable` con todos los legales y con la poda: 36 tableros de referencia en `run` y `single` y
  los niveles 1-200; los tableros de referencia baratos siguen en `Playing` a lo largo de su solución),
  `HotLoopAllocationTests.WarmDeadEndProbe_DoesNotAllocate` y `DeadEndProbePerformanceTests` (explícito, `Performance`). README,
  «Aguas estancadas por bucle».
- `LevelSession.RestoreRevealed(int cuia, int layers)` (T01, paso 1 del traspaso; 02 R7, R8, §11 T6 y §14; 04 §2.1, §2.2 y
  §6): restaura capas reveladas de Aguas turbias, también en parte (un vertido puede revelar una de las dos ocultas de una
  cuia; en el nivel 100 la cuia 0 tiene dos), que `Reveal` y `RevealAll` no expresan. Hace OR de `layers` en la máscara de
  revelado, la aplica a `Current`, recalcula `Status` como `Reveal` (R10 g-h) y devuelve cuántas capas nuevas reveló; es
  idempotente (0 y nada cambia si ya lo estaban), no entra en el historial de Marea y es permanente (I5: sigue tras Marea y
  Volver a la orilla). Lanza `ArgumentOutOfRangeException` con una cuia fuera del tablero y `ArgumentException` con `layers`
  negativa o con bits que no son dosis ocultas de la cuia en `Initial`. Usos: **reanudar** (02 §14, R8: se rehacen los
  vertidos guardados y se restaura la máscara guardada) y **rehacer la sesión con la Cuia extra o la Cuia de rescate**
  (`LevelFlow.Funnel.AddCuia`: la cuia vacía va al final, así que la máscara se copia cuia a cuia). En los dos usos la
  máscara se restaura al final del rehacer, como hace el cliente desde la integración del paso 1 (`docs/dev/traspaso/README.md` §6) (el historial `h2` guarda por
  cuia solo la máscara, sin el punto del revelado). Es exacto también con capas reveladas a mitad del historial sin que el
  historial rehecho las revele (Luciérnaga o un vertido deshecho con Marea): con T4, ver una oculta solo cambia la racha (R6)
  o el sellado (R5) si un lavado de Barro deja encima su mismo color, y ningún nivel de 1-200 lleva Barro en una cuia turbia
  (de los 7 tableros turbios, el 135 y el 195 lo llevan solo en otras cuias). Si un pack futuro lo combinara, habría que
  guardar el punto del revelado o prohibir la combinación en `LevelValidation`; `_AtTheRevealPoint_ReplaysTheHistoryExactly`
  documenta ese caso teórico. La Pororoca queda fuera: su revelado va justo antes de un barajado que vacía el historial y
  cambia el tablero (04 §2.1 y §2.4 a; 02 §14), así que tras ella la sesión ya no se rehace desde `Initial` más los vertidos
  (fuera del paso 1). Tests en
  `LevelSessionMurkyTests`: `RestoreRevealed_PartialLayer_KeepsOtherHidden`, `_SurvivesUndoAndRestart`,
  `_RejectsLayersNotHidden`, `_RejectsCuiaOutOfRange`, `_IsIdempotentAndOutsideHistory`, `_RecomputesStatusLikeReveal_R10gh`
  y `_AtTheRevealPoint_ReplaysTheHistoryExactly`; README, «Restaurar las capas reveladas».
- Solver y bot con todas las mecánicas: rendimiento y asignaciones (C3-3, cierra el núcleo 3; regla dura 2; 03 §5-§6; 08 §14.2;
  02 T2; GDD §6 #6). Solo dotnet: `DotnetOnly/ReferenceBoards` (36 tableros de 12-14 cuias: las 9 mecánicas de 1-200, Cuia
  chica, Cuia alta y las 25 combinaciones del calendario d.4 de D-007, generados con `InverseWalk`, superposición y
  verificación, percentil 75 de 30 semillas; Ll+Li+Al6 queda bloqueado por C1-9 en `LevelValidation`),
  `DotnetOnly/MechanicsPerformanceTests` (cada tablero válido y resuelto por A* con w = 1 dentro de su número de expandidos
  fijado con un 25 % de margen y nunca más de 300.000; Morpho con w = 2 y 40.000 determinista; bot ×40 reproducible; tabla de
  tiempos en Release en un test explícito de la categoría `Performance`, con Morpho < 50 ms) y
  `HotLoopAllocationTests.ReferenceBoard_HotLoop_DoesNotAllocate` (0 bytes en `SolverMoves`, `IsStuck`,
  `BoardRules.ApplyInPlace` y el bot calentado; < 1 KB por `Solve` y por realidad en `SafeMoves`). Compartido:
  `MechanicsSolverTests` (20 combinaciones del calendario sin Aguas turbias, 8 tableros pequeños cada una con el nuevo
  `CombinedInjector`, en `run` y `single`: A* con y sin poda contra la búsqueda exhaustiva y heurística admisible y
  consistente). Resultados y reparto de 30 semillas por combinación en la tabla de evidencias del README: A* hasta 186.346
  expandidos en los tableros de referencia; Morpho 1,0 ms como máximo; bot ×40 4,8 ms como máximo; con el presupuesto de
  300.000 se agotan sobre todo Ll+Re (13/30), Pi+Ma (7/30) y Re+Ma (5/30); `ceil(H/2)` con Piraña lo agotaría en 29-30 de 30.
- **Hallazgo de C3-3 para la integración (02 §1.4, anexo B, R16):** la cueva de la Piraña no está en la lista de
  `cambia_al_recibir`, así que verter una cuia monocolor en la cueva vacía es fútil; pero los mordiscos se apilan en la cueva
  y, en `single`, ese fútil puede formar parte de la solución óptima: un tablero con Liana y Piraña de capacidades uniformes da
  par 9 sin poda y 10 con la poda de 03 §5 (`PiranaSolverTests.M7_Single_AFutilePourIntoTheCave_CanShortenTheSolution`). P1
  se cumple (R14) y en `run`, el modo de juego, no apareció en 291 tableros con Piraña ni en 98 con Liana y Piraña; la
  comparación «uniforme ⇒ mismo par» de `MechanicBoards` excluye la Piraña en `single`. Si se quiere que la poda no cambie
  nunca el par, la cueva vacía tendría que contar como `cambia_al_recibir` (decisión del dueño; no se ha tocado la regla).
  Aparte: `MechanicBoards` puede generar inicios con una cuia sellada (no pasan `LevelValidation`); con una Liana cuya fruta ya
  está sellada al inicio, la poda deja sin camino útil un tablero resoluble, pero ese estado es inalcanzable en partida (la
  Liana se suelta en el mismo vertido que sella su fruta).
- Piraña (C3-2; 02 §9.1 M7 y §9.2; 03 §3 y §9; propuesta de D-007 b.2 M7 y b.2 M10 regla 7; anexo B; D-007, Q10, Q14 A y
  Q18 B; respuesta Q-C del dueño; GDD §6 #6; debut 183). `PiranaMechanic` (interno), registrada en la tabla y aceptada por
  `BoardBuilder` (con ella están portadas todas las mecánicas definidas). La cueva es la cuia marcada (normalmente una Cuia
  chica de 2) y guarda `k` y el contador en la clave canónica; el último destino es el del propio vertido, así que no se
  guarda. Etapa (e) de R10, una vez por vertido y nunca en un fútil (Q18 B): suma 1 y al llegar a `k` el contador vuelve a 0
  y la piraña muerde la dosis superior de la cuia que recibió el vertido y la lleva a la cima de la cueva, salvo que esa
  cuia haya quedado sellada (02 §9.1: sellar en el vertido n·k evita el mordisco), sea un Matapi (regla 7) o sea la propia
  cueva, o que la cueva esté llena (**Q-C**: el mordisco se pierde y el contador sigue). La cueva es una cuia normal como
  origen y como destino; lo que el mordisco selle o suelte ocurre en el mismo vertido (f′). Eventos en `PourEvents`:
  `PiranaBittenMask`, `PiranaCaveMask` y `PiranaBiteLostMask`; en `PourResult`, `PiranaBit` y `PiranaBiteLost`;
  `LevelCounter` cubre la Piraña (`Blocked` = cueva llena). Validación: `k` de 2 a 5 y el contador a 0, sin ocultos (Q14 A),
  sin Remolino [P] y un contador por nivel (nunca con la Lluvia). **Heurística: `H` no cambia y el par es exacto.** El plan
  proponía `ceil(H/2)` [P] («un vertido con mordisco baja H como mucho en 2»); con el mordisco inmediato de Q-C la dosis
  mordida es siempre una de las que el propio vertido acaba de dejar en la cima de la cuia que recibe, así que vertido +
  mordisco reparte el mismo vertido entre esa cuia y la cueva y cada término (`h1`, `h2`, `h3`) baja como mucho en 1 (también
  con el lavado del Barro). `H` sigue siendo admisible y consistente (comprobado en todos los estados alcanzables de 32
  tableros en los dos modos, con más de 50 mordiscos), así que no hace falta `ceil(H/2)` (que expandiría más) ni marcar el
  par como aproximado (03 §9 lo temía para un mordisco diferido sobre un `ultimo_destino` guardado). Tests compartidos
  `PiranaTests` (mordisco, sellar en el vertido k, Matapi, los dos casos de Q-C, Q18 B con R16, Marea, el mordisco que sella
  la cueva y suelta una Liana en el mismo vertido, validación) y `PiranaSolverTests` (A* con y sin poda contra la búsqueda
  exhaustiva y `H` admisible y consistente en 32 tableros de hasta 5.000 estados, uniformes y mixtos, en `run` y `single`;
  inyector `PiranaInjector`, cueva = la vacía más chica); P1-P3 con Piraña en `FutilePropertyTests` (20.000-30.000 estados
  por combinación, sin violaciones) y `HotLoopAllocationTests.PiranaBoard_HotLoop_DoesNotAllocate` (0 bytes en reglas,
  `BoardRules.ApplyInPlace` y bot; solver calentado < 1 KB).
- Lluvia (C3-1; 02 R3, §9.1 M6 y §9.2; 03 §3 y §9; 04 §5.1 regresión 4; propuesta de D-007 b.2 M6 y anexo B; D-007, Q10,
  Q14 A y Q18 B; respuesta Q-B del dueño; GDD §6 #6; debut 143). `LluviaMechanic` (interno), registrada en la tabla y
  aceptada por `BoardBuilder`. Estado en la cuia (03 §9, en la clave canónica): `key` = tinte agua, `n` = gotas pendientes,
  `k` y el contador de vertidos útiles desde la última gota. Etapa (e) de R10, una vez por vertido y nunca en un fútil (Q18 B):
  suma 1 y al llegar a `k` cae encima una dosis de `key`, sin regla de color, si hay hueco; si la cuia está llena la gota
  espera al siguiente vertido útil (el contador queda en `k − 1`, que se comporta igual y no crea un estado más); con la
  última gota la nube se va y la cuia pasa a `None`. Lo que la gota selle o suelte ocurre en el mismo vertido por el punto
  fijo (f′) (test obligatorio del anexo B: la gota que sella suelta la Liana con fruta del agua). «La cuia Lluvia solo sella
  con agua» (02 §9.1 M6): `CuiaRules.IsSealed` no da por sellada una cuia de Lluvia con gotas pendientes (llena de otra fruta
  sigue siendo origen). Con gotas pendientes impide la victoria (R12) y nunca es fútil como destino (`cambia_al_recibir`) ni,
  por **Q-B**, como origen; la exención acaba al caer la última gota. `U_agua` = dosis iniciales + gotas pendientes:
  `BoardState` (constructor público) suma ya las gotas, como `BoardBuilder`. Eventos de la etapa (e) en `PourEvents`:
  `LluviaDropMask`, `LluviaWaitingMask` y `LluviaEndedMask` (y `CountersAdvanced`); en `PourResult`, `RainDropped`,
  `RainWaiting` y `RainEnded`. Contador para el HUD: `Session.LevelCounter` (tipo, cuia, `k`, `Elapsed`, `Remaining` =
  vertidos útiles hasta la gota, `PendingDrops` y `Blocked` = cuia llena), en `LevelSession.Counter` y `PourResult.Counter`;
  solo baja con vertidos útiles (Q18 B) y Marea lo restaura con el estado. Validación: `key` = 10 o 14
  (`LevelValidation.TapajosWater`, `RioNegroWater`; 02 §9.1), `n` ≥ 1 y `n` + dosis iniciales = 4
  (`LluviaWaterUnits`), `k` de 2 a 5 y el contador a 0, sin ocultos (Q14 A); `LevelValidation` exige además **un contador
  por nivel** (Lluvia o Piraña; propuesta b.2 M6-M7, d.2 regla 6). Heurística sin cambios (03 §9: una gota nunca elimina
  cortes; comprobado). Tests compartidos `LluviaTests` (cada regla con su §, Q18 B con R16, la gota que espera, el
  obligatorio del anexo B, regresión 4 de 04 §5.1, el final de nivel del contraejemplo de Q-B: no hay bloqueo y se gana en 4,
  la victoria bloqueada, Marea y Volver a la orilla, validación) y `LluviaSolverTests` (A* con y sin poda contra la búsqueda
  exhaustiva y heurística admisible y consistente en 32 tableros de hasta 5.000 estados, uniformes y mixtos, en `run` y
  `single`; inyector `LluviaInjector`; el solver espera la gota en el contraejemplo de Q-B, par 4); P1-P3 con Lluvia en
  `FutilePropertyTests` (unos 20.000 estados por combinación, sin violaciones) y
  `HotLoopAllocationTests.LluviaBoard_HotLoop_DoesNotAllocate` (0 bytes en reglas, `BoardRules.ApplyInPlace` y bot; solver
  calentado < 1 KB).
- Remolino (C2-4, cierra el núcleo 2; 02 §1.2 rama del remolino en `legal` y §9.1 M4; 03 §9; 04 §2.1 b y §5.1 regresión 4;
  propuesta de D-007 b.2 M4; respuestas Q-A y Q-B del dueño; debut 103). `RemolinoMechanic` (interno), registrada en la
  tabla y aceptada por `BoardBuilder`: tocar una cuia del par como destino mete la racha por el fondo de la pareja, sea del
  color que sea (`Cuia.InsertAtBottom`, interno); legal si la cuia tocada no está sellada y la pareja tiene hueco y no está
  sellada (la tocada puede estar llena); cantidad `min(racha, espacio(pareja))` en `run` y 1 en `single`; como origen, la
  regla normal; como destino nunca es fútil (`cambia_al_recibir`). **Q-A**: una cuia del par no puede verter en su propia
  pareja (vertido ilegal, rechazo sin texto, R11). **Q-B**: la exención de `futil` se extiende al origen Remolino
  (`ExemptAsSource`); la de la Lluvia con gotas pendientes llega con C3-1. La pareja es la otra cuia de Remolino con el
  mismo id de pareja (el canon ya lo conserva, 03 §3). Eventos: `PourEvents.ThroughRemolino`, `PourResult.Receiver` y
  `PourResult.ThroughRemolino`; `PourResult.TargetSealed` mira la cuia que recibió (la pareja en el Remolino; sin
  Remolino no cambia). Validación: exactamente dos cuias por id de pareja (pares simétricos y distintos, los dos de tipo
  remolino; un Matapi nunca es pareja), sin ocultos [P] y sin Piraña en el nivel [P]. El descarte rápido de destinos de
  `SolverMoves` ya excluía las cuias con mecánica; la heurística no cambia (un vertido sigue eliminando ≤ 1 corte y sacando
  un color de ≤ 1 cuia; comprobado). Tests compartidos `RemolinoTests` (cada regla con su §, regresión 4 de 04 §5.1, Q-A y
  el contraejemplo de Q-B: X = [1, 1] e Y = [1, 1] en par, una vacía y el resto sellado → no hay bloqueo y se gana en 2) y
  `RemolinoSolverTests` (A* con y sin poda contra la búsqueda exhaustiva y heurística admisible y consistente en 32
  tableros de hasta 5.000 estados, uniformes y mixtos, en `run` y `single`; inyector `RemolinoInjector`); P1 y P2 con
  Remolino en `FutilePropertyTests` (unos 20.000-29.000 estados por combinación, sin violaciones) y
  `HotLoopAllocationTests.RemolinoBoard_HotLoop_DoesNotAllocate` (0 bytes en reglas y bot; solver calentado < 1 KB).
- **Hallazgo de Q-B para la integración (P3, anexo B):** con la exención de origen, X (Remolino monocolor) → vacía es útil
  y vacía → X entra por el fondo de la pareja; con la pareja vacía el tablero vuelve al mismo estado canónico (X e Y son
  simétricos). Es el riesgo que el anexo B describe al eximir orígenes: un ir y venir sin progreso cuenta como útil y puede
  retrasar el modal de Aguas estancadas. R14 se cumple (P1 sin violaciones). `FutilePropertyTests` cuenta esos vaivenes
  aparte (1 en `single` con capacidades mixtas) y `P3_Remolino_QB_AUsefulRoundTripCanReturnToTheSameCanonicalState` los
  fija.
- Realidades, vertido seguro y ruta segura de Aguas turbias (C2-2; 02 §11 T2, T4, T5 y `validar_turbio`; 03 §8 y §18.3;
  08 §2.2). `AF.Solver.Murky.RealityEnumerator`: asignaciones de las posiciones ocultas coherentes con lo visible, con
  `units_per_color` (menos las gotas pendientes de Lluvia) y con T4; T4 se mira contra la dosis de encima del tablero de
  referencia (el inicial: el lavado del Barro sustituye la dosis de encima sin revelar la oculta); cada realidad conserva
  la máscara de ocultos, así que el solver juega la partida real; orden determinista y sin duplicados; más de 24 →
  `TooMany` (el nivel se rechaza); sin ninguna → `Inconsistent`; `SatisfiesT4`. `SafeMoves`: los vertidos de
  `SolverMoves` que dejan el tablero resoluble en todas las realidades (A* por realidad con el presupuesto de las opciones:
  `ForPipeline` = 20.000 nodos, w = 1; Morpho pasa `hint_budget_nodes`), memo exacto por estado canónico con el par, un
  único `AStarSolver`; sin ninguno, `Count` = 0 («ninguno», T5: Morpho no cobra); `TrueParAfter`. `SafePath`:
  `RUTA_SEGURA` (hasta 400 pasos, el seguro de menor par en la realidad verdadera; T4 obligatoria en S0 → `RejectedT4`;
  `enforceT4 = false` solo para la paridad con el prototipo). `AStarSolver.SolveLength` (interno): A* sobre un array sin
  construir la solución, sin asignaciones. `Cuia.WithColorAt` (interno). Tests compartidos `MurkyTests` (2 ocultas → ≤ 2
  realidades; 4 en 2 cuias → ≤ 24, 12 en el caso de 03 §8 sin T4 y 8 con T4; > 24 rechaza; T4 con Barro lavado; vertidos
  seguros contra la búsqueda exhaustiva en cada realidad; un tablero sin vertido seguro con solución en la realidad
  verdadera; memo; ruta segura en `run` y `single`; rechazo por T4) y solo dotnet `MurkyParityTests` (los 5 tableros de
  `murky_demo.json`, reproducidos con `levelgen_proto.generate_inverse` y sus semillas, tienen ruta segura con 12
  realidades máx. en 108-630 ms por nivel en Release; con T4, n0003 y n0004 se rechazan; 0 bytes por llamada calentada).
- Aguas turbias en la partida (C2-1; 02 R7, R8 y §11 T2-T4, T6; 04 §2.1, §2.4 a, §2.5, §2.7 y §5.1; D-007, Q14 A).
  `LevelSession` acepta dosis ocultas y lleva la máscara de revelado del nivel (`RevealedLayers(cuia)`, por posición
  inicial `[cuia, capa]`: una dosis oculta nunca se mueve), que persiste tras Marea y tras Volver a la orilla (invariante
  I5: lo revelado no vuelve a ocultarse; `Initial` no cambia). `Reveal(cuia)` (Luciérnaga) y `RevealAll()` (Pororoca)
  devuelven cuántas dosis revelan, no entran en el historial y recalculan el estado; `HiddenCount`. Rechaza con
  `NotSupportedException` un tablero con ocultos y Remolino (la inserción por el fondo desplazaría las capas) [P]. La
  Luciérnaga regalada del nivel 100 la da el cliente (04 §2.5). Eventos de revelado: `PourEvents.RevealedMask` (etapa (a)
  de R10) y `PourResult.RevealedMask`. `Cuia.WithRevealed(mask)`, `Cuia.HiddenCount` y `BoardState.HiddenCount`.
  `LevelValidation.Validate(state, futile, murkyMaxCuias, murkyMaxHiddenUnits)` (los límites de 08 §9 como parámetros, por
  defecto `DefaultMurkyMaxCuias` = 3 y `DefaultMurkyMaxHiddenUnits` = 4): T2; T3 (como mucho 3 dosis, nunca llena, cima
  visible); T4 (bajo una dosis oculta nunca va el color de la de encima, también si la de encima está oculta); ocultos solo
  en cuias sin mecánica; con filtros (Hoja, Brote, Matapi, Barro) sí, con Liana, Lluvia y Piraña no (Q14 A), con Remolino
  no [P: propuesta b.2, «solo junto a filtros»]; el Barro nunca va oculto [P]. T1 y Q9 quedan para el pipeline. Tests
  compartidos `LevelSessionMurkyTests` (Marea tras revelar en `run` y `single`, reinicio tras revelar, Luciérnaga seguida
  de Marea, `RevealAll`, regresión 3 de 04 §5.1 en la partida) y `MurkyValidationTests`; solo dotnet
  `HotLoopAllocationTests.HiddenDoses_StuckDetection_DoesNotAllocate` (0 bytes con ocultos).
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

- T51 (D-014): `LevelSession.Status` pasa a `Stuck` también con vertidos útiles si el cierre acotado no tiene victoria (ver
  «Añadido»). R14 se reformula con D-014 (no se anuncia irresoluble mientras exista progreso posible); P1 se mantiene y la comprueba
  `DeadEndProbePropertyTests`. `RuleCatalogTests.R14_AnUnsolvableBoardWithUsefulPours_IsNotDeclaredStuck` sí cambia lo que
  prueba, porque probaba la R14 anterior («mientras exista un movimiento útil»), que D-014 sustituye: su tablero, perdido con
  vertidos útiles y pocos estados alcanzables, ahora es Aguas estancadas y pasa a
  `R13b_AnUnsolvableBoardWithFewReachableStates_IsStuck`. En su lugar, `R14_ASolvableBoardWithUsefulPours_IsNeverDeclaredStuck_AlongItsSolution`
  prueba la R14 reformulada: el tablero mixto que solo se gana empezando por fútiles (C1-8) sigue en `Playing` tras cada vertido
  de su solución A*, para R13 y para R13 ampliada, hasta la victoria. Los demás tests compartidos cuyo tablero estaba perdido con
  pocos estados alcanzables y esperaban `Playing` se adaptan sin cambiar lo que prueban: `R15_StuckBlocksInput_AndMareaReturnsToPlaying`
  usa un tablero que se gana y un vertido que lo pierde (vaivén de D-014); `LevelSessionTests.KillSwitchOff_SameBoard_IsPlaying` se
  parte en `KillSwitchOff_OnlyFutileBoardWithASolution_IsPlaying` (el contraejemplo de la Liana de C1-9) y
  `KillSwitchOff_LostBoardWithFewStates_IsStuckByR13b`; `HiddenDoses_AreAccepted`,
  `LevelSessionMurkyTests.Regression3_0451_…` y `PourModeTests.I3_Single_UndoReturnsOneDosePerUse` usan tableros que se ganan (una
  cuia más o una Cuia chica de 3).
- Revisión de la fase 1 (03 §6-§7; 02 §9.1 M4 y R5): `CasualBot.Score` con Remolino. La racha entra por el fondo de la pareja
  y de cualquier color, así que el bot daba +16 («juntar con el mismo color») y +40 («sella») a vertidos que mezclaban
  colores. Ahora, cuando la cuia que recibe es la pareja de un Remolino, +16 solo si es monocolor del color vertido y +40
  solo si además queda llena con `cap` = `U_c` (sella); si mezcla, no suma nada y la pareja vacía sigue costando −20 [P].
  Sin Remolino la puntuación no cambia: el tutorial y los niveles 11-100 salen idénticos byte a byte. Tests compartidos
  `CasualBotTests.Score_Remolino_*`. Cambia el `bot_fail` de los candidatos con Remolino y, con él, las z del pool 101-200
  y la colocación: `mundo_04_v1` (101-140) y `mundo_05_v1` difieren y la campaña 101-200 y sus informes se regeneran; el
  reto 166 (Ll+Re) baja a 0,675 y queda sin etiqueta (101-200: 23 Difícil en vez de 24; level-qa, 4 avisos en vez de 3).
- C3-3 (corrección de C1-9): `LianaMechanic` ya no cuenta el Barro (15) como un color de `U` = 1 al decidir si las capacidades
  son mixtas, así que un nivel con Liana y Barro y capacidades uniformes (Ba+Li, nivel 138 del calendario d.4) deja de
  rechazarse por el bloqueo provisional. Test `LianaTests.M3_Validation_WithBarro_IsNotMixedCapacities_D4_138`.
- C3-2: `BoardBuilderTests.Mechanics_NotYetPorted_AreRejected` pasa a `Mechanics_EveryDefinedTypeIsPorted` (el fallo
  cerrado sigue en `BoardBuilder` para los tipos que se definan en la 1.1).
- C3-1: `CuiaRules.IsSealed` no da por sellada una cuia de Lluvia con gotas pendientes (02 §9.1 M6: solo sella con agua);
  sin Lluvia no cambia nada. `BoardState(Cuia[])` cuenta en `U_c` las gotas pendientes de la Lluvia (02 R3), como ya hacía
  `BoardBuilder`: el test de `MurkyTests` que dependía de lo contrario pasa a
  `Realities_SubtractThePendingDropsOfTheLluvia_02R3`. `IMechanic.AdvanceCounter` devuelve true solo si cambió el contenido
  de alguna cuia (gota o mordisco). `PourResult` gana un constructor con el `LevelCounter`; los anteriores se conservan.
- C2-4: `PourResult.TargetSealed` mira la cuia que recibió las dosis (`Receiver`): en el Remolino, la pareja. Sin Remolino es
  la misma que antes. `MechanicBoards.Generate` admite un tope de estados alcanzables.
- C2-2: `AStarSolver.Solve` separa la búsqueda (`Search`) de la construcción de la solución; `SolveLength` (interno) la usa
  sin construirla. Mismo par y mismos expandidos.
- C2-1: `LevelSession` ya no rechaza los tableros con dosis ocultas (los tests `HiddenDoses_AreRejected*` pasan a
  `HiddenDoses_AreAccepted*`); I3 e I4 se precisan con la máscara de revelado (I5).
- C2-3: `BoardState` y `BoardBuilder` aceptan el color 15 (Barro); `CuiaRules.IsSealed` exige un tinte y
  `CuiaRules.CanBeSource`, `IsLegal` y `Amount` tratan el Barro expuesto (`EffectiveSpace`).
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
