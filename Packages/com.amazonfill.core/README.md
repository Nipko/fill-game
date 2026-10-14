# com.amazonfill.core

Núcleo de Amazon Fill en C# puro: reglas del tablero (`AF.Core`) y solver (`AF.Solver`).
Es la **única copia** del código. Unity lo importa como paquete embebido y `dotnet/` compila estos mismos `.cs`
para `dotnet test` y para las herramientas de `tools/` (informe `docs/setup/research/d-dotnet-nucleo-compartido.md`).

## Reglas para quien programe aquí

- C# 9 y .NET Standard 2.1, igual que Unity 6.x. Nada de `UnityEngine` (`noEngineReferences: true`) ni de `#if UNITY_*` en `Runtime/`.
- Cada archivo empieza por `#nullable enable`.
- Determinismo entre CoreCLR, Mono e IL2CPP: sin `System.Random`, `string.GetHashCode`, `HashCode`, `DateTime.Now` ni LINQ
  (lo bloquea `dotnet/BannedSymbols.txt`); solo aritmética entera en reglas, hash y solver.
- Bucle caliente sin `new` de clases, *closures*, `params` ni `foreach` sobre interfaces.
- Tests compartidos (`Tests/Editor/**`): solo atributos de NUnit 3.5 y `Assert.That` con restricciones.
  Los tests que necesiten APIs de .NET 10 o lotes grandes van en `dotnet/AF.Core.Tests/DotnetOnly/`.
- Los `.meta` los genera Unity al abrir el proyecto y se confirman en git junto con su archivo (regla dura 3: nunca a mano).
  Una rama escrita sin Editor los deja pendientes y no se fusiona hasta generarlos: ver «Paso obligatorio de integración».

## Verificación

```powershell
dotnet build dotnet/AmazonFill.slnx -warnaserror
dotnet test dotnet/AmazonFill.slnx
```

## Paso obligatorio de integración en Unity (rama `fase1-mecanicas-niveles`)

La rama se escribió en un worktree sin Unity (sin `Library/`): añade al paquete 59 archivos `.cs` (32 en `Runtime/` y 27 en
`Tests/Editor/`) y 5 carpetas sin su `.meta`, y sus 467 tests compartidos solo se han ejecutado en `dotnet` (filtro
`FullyQualifiedName!~DotnetOnly`). Si se fusiona así, el primer Editor que importe `main` generará GUID sin control. Antes
del merge, en la integración y **en esta misma rama**:

1. Importar la rama en Unity en un worktree sin ningún Editor abierto en él (regla dura 7):
   `pwsh -NoProfile -File scripts/unity-compile.ps1`, o abriendo ese worktree en el Editor. Unity genera los **64 `.meta`** que
   faltan: los de los 59 archivos y los de las carpetas `Runtime/AF.Core/Determinism`, `Runtime/AF.Core/Mechanics`,
   `Runtime/AF.Solver/Bot`, `Runtime/AF.Solver/Generation` y `Runtime/AF.Solver/Murky`.
2. Commitear esos `.meta` tal como los genera Unity, en la misma rama y junto a los packs
   `Assets/_Project/Data/Levels/mundo_02_v1.json` a `mundo_05_v1.json` y sus `.meta`, que quedan pendientes por el mismo motivo
   (commits `def28bc` y `57b31c6`). Comprobar que no falta ninguno (Git Bash, desde la raíz; sin salida = completo):

   ```bash
   cd Packages/com.amazonfill.core
   git ls-files | grep -v '\.meta$' | while read -r f; do git ls-files --error-unmatch "$f.meta" >/dev/null 2>&1 || echo "falta $f.meta"; done
   find Runtime Tests -mindepth 1 -type d | while read -r d; do git ls-files --error-unmatch "$d.meta" >/dev/null 2>&1 || echo "falta $d.meta"; done
   ```

3. Pasar, con código 0, `pwsh -NoProfile -File scripts/unity-compile.ps1` y
   `pwsh -NoProfile -File scripts/unity-test.ps1 -Mode EditMode`. En `dotnet` los tests compartidos forman un solo ensamblado
   (`dotnet/AF.Core.Tests`); en Unity, dos (`AF.Core.Tests` y `AF.Solver.Tests`, cada uno con su asmdef y el NUnit 3.5 del Test
   Runner): un test que use un tipo del otro ensamblado o una API de NUnit posterior a la 3.5 compila en `dotnet` y no en
   Unity. Hasta que este paso se cumpla, la rama no está verificada en Unity.

Este paso va también en la descripción del PR de integración.

## Contenido actual

| Ensamblado | Tipo | Reglas del GDD |
|---|---|---|
| `AF.Core` | `Board.Cuia`: cuia empaquetada en un `ulong` (capacidad, dosis, máscara de ocultos y atributos de mecánica en los bits 39-60: tipo, `key`, id de pareja, `n`, `k`, contador y banderas; bits libres a 0) | 02 §1.1 R1, R2, R4, R6, R7; 08 §2.2, §4.1; 03 §3 |
| `AF.Core` | `Mechanics.MechanicType`, `MechanicTypes` (claves `attrs.type`) y `MechanicSpec` (espejo de `attrs[i]`) | 02 §9.1; D-007; 08 §4.1 |
| `AF.Core` | `Board.BoardBuilder` y `UnitsPerColor`: tablero desde `caps`, `cuias`, `attrs`, `hidden` y `units_per_color` (entero u objeto), con los campos obligatorios por mecánica, el id de pareja del Remolino y el motivo del rechazo; falla cerrado con las mecánicas sin portar | 08 §4.1; 02 §9.1; 03 §3 |
| `AF.Core` | `Board.LevelValidation`: reglas estructurales del tablero inicial (≤ 16 cuias, `U_c` ∈ {4, 5, 6}, una cuia propia por color y fruta abundante por Cuia alta, sin sellos ni victoria al inicio, reglas de cada mecánica, `atascado(S0)` = false) | 03 §17 #5-#6; 02 §8; 02 §9.2 regla 1 |
| `AF.Core` | `Board.BoardState`: tablero inmutable, `U_c`, sellado, victoria, igualdad por valor (`IEquatable`) y `CanonicalHash()` | 02 §1.1 R3, R5; §1.3 R12; 03 §3 |
| `AF.Core` | `Board.BoardCanon`: orden canónico (por `Packed`) y hash FNV-1a de 64 bits, sin asignaciones | 03 §3; 08 §2.2 |
| `AF.Core` | `Rules.PourMode` y `Rules.PourModes`: modo de vertido `Run` (la racha que cabe, con vertido parcial; por defecto y `default(PourMode)`) o `Single` (una dosis por vertido, solo pruebas), con los valores `"run"`/`"single"` de `rules_pour_mode` | D-005, que sustituye a D-001 (`docs/dev/decisiones.md`); 02 §1.2, R6, R9 |
| `AF.Core` | `Rules.BoardRules`: **única** implementación de las reglas con contexto de tablero y de la aplicación de vertidos: origen, `legal`, `cantidad`, `futil` del anexo B, `ApplyInPlace` con R10 por etapas ((a) revelado, (b) aperturas, (c) sellado, (d) liberaciones, (e) contadores solo en vertidos no fútiles, (f) condiciones, (f′) punto fijo, (g) victoria, (h) bloqueo), sin asignaciones, sobre `(Cuia[], offset, count, U_c)`; `PourRules`, `BoardState`, `LevelSession`, el solver y el bot delegan aquí | 02 §1.2-§1.4; anexo B de la propuesta de D-007 (Q13, Q18 B); 04 §5.1 |
| `AF.Core` | `Rules.PourEvents`: lo ocurrido en cada etapa de R10 (revelado, aperturas, sellados, liberaciones, contadores, estado), para animar en orden y deshacer en orden inverso | R10 (anexo B) |
| `AF.Core` | Mecánicas portadas (D-007, núcleo 1): Hoja (M1, debut 43), Matapi (M10, 63), Liana con fruta (M3, 83) y de cuenta (variante, 243) y Brote (M2, 163), como `IMechanic` sin estado; eventos `HojaOpenedMask`, `BroteOpenedMask` y `LianaReleasedMask` en `PourEvents`; cada una con su validación. La Liana con capacidades mixtas está bloqueada en `LevelValidation` hasta decidir la pregunta de 02 R16 (P1 falla) | 02 §9.1; D-007; propuesta de D-007 b.2-b.3 y anexo B |
| `AF.Core` | Remolino (M4, debut 103; núcleo 2): `RemolinoMechanic`; tocar una cuia del par mete la racha por el fondo de la pareja (cualquier color; la tocada puede estar llena; la pareja con hueco y sin sellar); `min(racha, espacio(pareja))` en `run`; como origen, la regla normal; nunca fútil como destino ni, por Q-B, como origen; Q-A: verter en la propia pareja es ilegal; `PourResult.Receiver` / `ThroughRemolino`; validación: dos cuias por pareja, sin ocultos ni Piraña [P] | 02 §1.2, §9.1 M4; 03 §9; 04 §5.1; Q-A, Q-B |
| `AF.Core` | Lluvia (M6, debut 143; C3-1): `LluviaMechanic`; cada `k` vertidos útiles (nunca en un fútil, Q18 B) cae una gota del agua `key` encima, sin regla de color, si hay hueco (si no, espera al siguiente vertido útil); con la última se va la nube; solo sella con agua; con gotas pendientes impide la victoria y no es fútil como destino ni, por Q-B, como origen; `U_agua` = dosis + gotas = 4; eventos `LluviaDropMask`, `LluviaWaitingMask`, `LluviaEndedMask`; validación: `key` 10 o 14, `k` 2-5, sin ocultos, un contador por nivel | 02 R3, §9.1 M6, §9.2; 03 §9; anexo B; Q-B, Q14 A, Q18 B |
| `AF.Core` | Piraña (M7, debut 183; C3-2): `PiranaMechanic` en la cueva; cada `k` vertidos útiles (nunca en un fútil) muerde la dosis superior de la cuia que recibió el vertido y la lleva a la cueva, salvo sellado, Matapi, la propia cueva o cueva llena (Q-C: el mordisco se pierde y el contador sigue); eventos `PiranaBittenMask`, `PiranaCaveMask`, `PiranaBiteLostMask`; validación: `k` 2-5, sin ocultos ni Remolino [P], un contador por nivel | 02 §9.1 M7, §9.2; 03 §9; anexo B; Q-C, Q14 A, Q18 B |
| `AF.Core` | Tinaja (M13, debut 323 [P]; T55, D-019): `TinajaMechanic` (capacidad 5, 3-5 dosis ocultas salvo la boca; nunca recibe; **solo vierte en su cuia** por el gancho `IMechanic.AllowsTarget`, la primera línea de `BoardRules.Accepts`; una dosis cada vez por T4; exenta como origen en `futil`; vacía queda inerte) y `CucharonMechanic` (su Cuia chica de 2, contigua: solo recibe de la tinaja, cualquier fruta; exenta como origen mientras cuelga; se suelta en R10 f cuando la tinaja queda vacía y pasa a ser una Cuia chica normal); evento `CucharonReleasedMask`; validación: una tinaja con su cuia, T4 en la columna, sin Barro ni Liana, Remolino, Lluvia o Piraña, única fuente de ocultas con tope propio (4) y fuera de las dos comparaciones de capacidades de 02 §8. Ver «Tinaja (M13)» | Propuesta de la tinaja b.2, c.1-c.2; 02 §1.2, §1.4, R10, R14, §8, §11; Q14 A; Q-Ti1, Q-Ti2, Q-Ti6 |
| `AF.Core` | Variantes de la 1.0 (K3a; propuesta de D-007 b.3; firma de `pack_schema` 3, pregunta 8): **Hoja atada** (`MechanicType.HojaAtada` = 10 [P], clave `hoja_atada`, debut 283; `HojaAtadaMechanic`: solo recibe su fruta `key`, vacía o con ella en la cima; sí es origen; nunca se suelta, ni al sellar; nunca es fútil como destino; puede empezar con su fruta en el fondo; capacidad = `U_c` de su llave [P]) y **Brote de dos hojas** (el Brote con `n` = 2, debut 343: `Cuia.MechanicN` = hojas que le faltan, resta las dosis recibidas en la etapa (b) y germina al llegar a 0; un tramo de 2 lo abre de una vez en `run`; evento `BroteLeafOpenedMask`; espacio ≥ 2 al empezar). Ver «Variantes de la 1.0 (K3a)» | 02 §1.2, §1.4, R10 (b), R16; 02 §9.1; propuesta de D-007 b.3 |
| `AF.Core` | `Session.LevelCounter`: contador visible del nivel para el HUD (Lluvia o Piraña: `Remaining` vertidos útiles hasta la gota o el mordisco, gotas pendientes, `Blocked`), en `LevelSession.Counter` y `PourResult.Counter`; solo baja con vertidos útiles | 02 R8, §9.1 M6-M7; D-007, Q10, Q18 B |
| `AF.Core` | Barro (M5, debut 123; núcleo 2): color 15 inerte dentro del contenido, sin despacho de mecánica. Con Barro en la cima (`Cuia.MudExposed`) la cuia no es origen y acepta cualquier color; el primer vertido lo lava y ocupa su lugar (`CuiaRules.EffectiveSpace`: espacio + 1); una cuia con Barro no está sellada ni vacía; evento `BarroWashedMask` / `PourResult.BarroWashed`; validación: capa 2-3 de una cuia de 4, nunca con mecánica ni oculto | 02 R2, §1.2, §9.1 M5; 03 §9; propuesta b.2 M5 |
| `AF.Core` | Aguas turbias en la partida (núcleo 2): `LevelSession` con máscara de revelado del nivel (`RevealedLayers`), que persiste tras Marea y Volver a la orilla; `Reveal(cuia)` (Luciérnaga), `RevealAll()` (Pororoca) y `RestoreRevealed(cuia, layers)` (restaurar capas reveladas, también en parte, al reanudar y al rehacer la sesión; ver «Restaurar las capas reveladas»); `PourEvents.RevealedMask`; validación T2-T4 con los límites de 08 §9 como parámetros y las combinaciones de Q14 A | 02 R7, R8, §11 T2-T4, T6, §14; 04 §2.1, §2.2, §2.4 a, §2.5, §2.7, §5.1; D-007 |
| `AF.Core` | `Mechanics.IMechanic` (interno): ganchos de mecánica sin estado (origen, destino, espacio, recepción, `cambia_al_recibir`, exención de origen, etapas (b), (d), (e), (f), victoria y validación), despachados por una tabla estática indexada por el tipo de la cuia | 08 §2.2; 02 §9 |
| `AF.Core` | `Rules.CuiaRules`: envoltorio heredado sobre cuias sueltas sin mecánicas; capa base de `BoardRules` para el sellado (R5) y el núcleo de `futil` | 02 §1.2, §1.4, §3.1; 04 §5.1 |
| `AF.Core` | `Rules.Pour` y `Rules.PourRules`: validan argumentos y delegan en `BoardRules`: vertido según el modo (parcial solo en `Run`), `Apply` con `PourEvents`, `CanBeSource`, `IsFutile`, `CollectUsefulMoves`, `HasUsefulMove`, `IsStuck` | 02 §1.2-1.4 R9-R11, R13, R14, R16; §3.1 C1-C2 |
| `AF.Core` | `Session.LevelSession`, `LevelStatus`, `PourResult` (con `Events`): partida con R10 en un modo de vertido fijo, Marea (deshacer; una dosis por uso en `Single`) y Volver a la orilla (reinicio); el bloqueo (h) es R13 y, si aún quedan vertidos útiles, R13 ampliada con `DeadEndProbe` | 02 §1.3-1.4, §2; 04 §2.1, §2.7; D-014 |
| `AF.Core` | `Session.DeadEndProbe`: cierre acotado de Aguas estancadas por bucle (R13 ampliada): estados alcanzables con todos los vertidos legales, en anchura, canónicos (`BoardCanon`, igualdad por valor); ≤ `MaxStates` (64 [P]) y sin victoria ⇒ perdido; sin asignaciones tras calentar (ver «Aguas estancadas por bucle») | 02 R13, R14; D-014 |
| `AF.Core` | `Determinism.Prng`: xoshiro256** sembrado con SplitMix64 desde la FNV-1a de 64 bits de `amazonfill:v1:m{mm}:v{v}:n{nnnn}` (`LevelSeed`), sub-semillas (`SubSeed`) y `NextInt(bound)` sin sesgo (Lemire); solo enteros, valores dorados en `PrngTests` | 09 R26; 08 §2.1 #5, §4.1 |
| `AF.Solver` | `Collections.MinHeap`: montículo con desempate FIFO (sustituto de `PriorityQueue`) | 08 §2.2 |
| `AF.Solver` | `Bot.CasualBot`, `BotOptions`, `BotResult`: bot casual ×40 (85/15, tope 400) sobre `SolverMoves` y `BoardRules`, con `PUNTUACION_CASUAL` ×4 en enteros y ruido entero equivalente, congelada (en el Remolino puntúa la pareja: +16 solo si es monocolor del color vertido y +40 solo si además sella con `cap` = `U_c` [P]; con la cuia colgada de la Tinaja, +16 y +40 solo si su cima es de la fruta sacada: rama mínima de c.2); `fallo_bot` reproducible por semilla (`SeedFor`) y sin asignar una vez calentado | 03 §6-§7; 08 §2.2, §14.2; propuesta de la tinaja c.2 |
| `AF.Solver` | `Generation.InverseMoves`, `InverseWalk`, `InverseDeep`, `InverseSpec`, `Complexity` y `Collections.StateSet`: barajado inverso (`WALK` con mejor prefijo; `DEEP` en DFS con 20.000 nodos) sobre K, E, capacidades y `U_c`, con el PRNG del núcleo, la puntuación en enteros, la complejidad de laicwben2 y solución por construcción | 03 §4.1-§4.2; 02 §8 |
| `AF.Solver` | `Search.EarlyTraps`: trampas tempranas (jugadas iniciales que atascan en ≤ 2 vertidos; `CountUnavoidable` en lectura estricta) | Q16 B de D-007 [P] |
| `AF.Solver` | `Search.SolverMoves` y `SolverMove`: `MOVIMIENTOS` de 03 §5 (útiles, con la cantidad del modo, cuias idénticas una sola vez y fútiles marcados con el kill-switch apagado), sin asignaciones; los usan el solver, el bot y las métricas | 03 §5 |
| `AF.Solver` | `Search.AStarSolver`, `SolverOptions`, `SolveResult`, `SolveStatus`: A* (w entero) sobre estados canónicos en los dos modos de vertido (`SolverOptions.ForPipeline(mode)`), presupuesto de expandidos y prueba de irresolubilidad; a igual `f`, primero el de menor `g` (tupla `(f, g, S)` de 03 §5 y del prototipo). En niveles con Tinaja, sin poda de fútiles: `RequiresUnprunedSearch`, `ForLevel`, `WithFutileRule` y `ForPipeline(mode, level)` (sin tinaja, las opciones de siempre) | 03 §5; D-005; propuesta de la tinaja b.2 R14, c.2 |
| `AF.Solver` | `Search.SolverCancellation` (`Request`, `IsRequested`, `Reset`; `PollInterval` = 256 [P]), `SolverOptions.Cancellation` y `WithCancellation` (null por defecto y en el pipeline), `SolveResult.WasCancelled` y `SafeMoves.WasCancelled`: corte cooperativo por tiempo, que marca otro hilo; el solver la consulta cada 256 expansiones sin asignar y para con `BudgetExceeded` (ver «Hilos y corte por tiempo») | 08 §9 `hint_timeout_ms`; 03 §5; 04 §2.3; 02 T5 (D-019) |
| `AF.Solver` | `Search.SolverHeuristic`: `H = max(cortes, dispersión)`; en `Single`, `H_single = max(dosis sobre la racha del fondo, dispersión)` [P]; con Barro, `h1` resta 1 por barro y `h2` ignora el 15 (03 §9); con Lluvia y Piraña no cambia (una gota no elimina cortes; el mordisco inmediato de Q-C reparte el propio vertido: cada término baja ≤ 1 por paso), así que el par sigue siendo exacto | 03 §5, §9; decisiones.md §2.1.3; Q-C |
| `AF.Solver` | `Murky.RealityEnumerator`: realidades de Aguas turbias (asignaciones de las posiciones ocultas coherentes con lo visible, `units_per_color` y T4 respecto al tablero inicial; > 24 → `TooMany`, el nivel se rechaza), sin asignaciones | 02 §11 T2, T4; 03 §8; 08 §2.2 |
| `AF.Solver` | `Murky.SafeMoves`: vertidos útiles seguros en todas las realidades (A* por realidad: 20.000 nodos en el pipeline, `ForPipeline`; el cliente pasa `hint_budget_nodes`), memo exacto por estado canónico y un único `AStarSolver` (`SolveLength`, sin construir la solución); sin seguro, `Count` = 0 (T5: Morpho no cobra) y `Outcome` dice por qué, en el orden de respuestas de Morpho (`NoExit`, `SomeRealitySolvable` —nunca con R = 1—, `BudgetExceeded`; además `Found`, `Won` y `RealitiesRejected`); `ForPipeline(mode, level)` sin poda con Tinaja | 02 §11 T5; 03 §8; propuesta de la tinaja b.2 «Morpho», c.2 |
| `AF.Solver` | `Murky.SafePath`: `RUTA_SEGURA` (hasta 400 pasos; en cada uno, el seguro de menor par en la realidad verdadera), con T4 obligatoria en el tablero inicial (`RejectedT4`); con `NoSafeMove`, `NoSafeMoveOutcome` separa «sin salida», «alguna realidad tiene solución» y «presupuesto agotado» para el informe | 03 §8; 02 §11 `validar_turbio`; propuesta de la tinaja c.3 |

Rendimiento medido (2026-10-06, .NET 10 Release, Windows ARM64): los 140 niveles de `tools/out*/levels_*.json` dan el
mismo par que `levelgen_proto.py`; un tablero de 14 cuias (12 colores, 2 vacíos, par 37) expande 16.114 estados. Con el
desempate de 03 §5 (menor `g` primero) son un 36 % más que con el anterior (mayor `g` primero: 11.879) y, en la misma
máquina y sesión, unos 100 ms frente a 80 ms; con `w = 2` (Morpho) el cambio es mínimo (128 frente a 116 expandidos, mismo
largo). Cada nodo guarda solo las `T` cuias del tablero. El bucle caliente no asigna (tests
`DotnetOnly/HotLoopAllocationTests`).

Reglas con contexto de tablero y mecánicas (C1-1 a C1-7, 2026-10-08, misma máquina y sesión, Release): con el solver
expandiendo `SolverMoves` y aplicando con `BoardRules`, el tablero de 14 cuias sigue expandiendo 16.114 estados (par 37) en
`Run` y 16.956 (par 48) en `Single`; el tiempo por búsqueda calentada pasó de 26,7 ms (`main`, e8edd1f) a 30,4 ms en `Run`
(+13 %) y de 30,0 ms a 33,8 ms en `Single`. Los 140 niveles de paridad dan el mismo par.

Modo de vertido (D-005, 2026-10-08, sustituye a D-001): en `Run` el tutorial v0 da los pares 3, 6, 7, 8, 10, 10, 11, 13,
14 y 16 (en `Single`, solo de referencia: 3, 9, 10, 12, 16, 17, 15, 17, 16, 19; medidos el 2026-10-06 con los mismos
tableros). A* con `H_single` expande en `Single` como mucho 1.176 estados (nivel 10), las mismas cifras que decisiones.md
§2.1.3-§2.1.4. En el tablero de 14 cuias de referencia, `Run` da par 37 con 16.114 expandidos (`Single`: par 48, unos
17.000). Las firmas sin modo son heredadas (equivalen a `Run`, que desde D-005 es también `PourModes.Default`) hasta T6.

Aguas turbias (C2-2, 2026-10-08, misma máquina, Release): los 5 tableros de `tools/out_inverse/murky_demo.json` (03 §18.3)
tienen ruta segura con 12 realidades como máximo y 108-630 ms por nivel (prototipo en Python: 0,68-8,54 s; 03 §8 estimaba
< 1 s en C#), con 134-213 búsquedas A* de 20.000 nodos como máximo y memo por estado canónico. Ese lote es anterior a T3 y
T4: con T4 obligatoria, n0003 y n0004 se rechazan (esconden una dosis bajo otra de su color) y los otros tres tienen ruta con
8 realidades como máximo (111-394 ms). `SafeMoves` calentado asigna 0 bytes en una llamada con 12 realidades y 36 búsquedas.

Núcleo 2 (C2-1 a C2-4, 2026-10-08): con Barro y Remolino el tablero de 14 cuias de referencia sigue dando par 37 con 16.114
expandidos (`Single`: 48 y 16.956) y los 140 niveles de paridad el mismo par; el tiempo por búsqueda calentada queda dentro
del ruido de la máquina (compartida con un Editor de Unity: de +3 % a +14 % frente a ff9f8da en las mediciones, con
oscilaciones del mismo orden entre repeticiones). El Remolino multiplica los estados alcanzables: sus tests de solver usan
tableros de hasta 5.000 estados.

Todas las mecánicas (C3-3, 2026-10-08, .NET 10 Release, Windows ARM64; máquina compartida con un Editor de Unity, así que
los tiempos oscilan un 10-30 % entre repeticiones). Tableros de referencia de 12-14 cuias para cada mecánica y para cada
combinación del calendario 1-200 de D-007 (d.4) en `dotnet/AF.Core.Tests/DotnetOnly/ReferenceBoards.cs`: tablero base con
`InverseWalk` (K = 10, E = 2), superposición de las mecánicas (03 §9) y verificación con `LevelValidation` y A*; de 30 semillas
por combinación se eligió el del percentil 75 de expandidos. Ho Hoja, Ma Matapi, Li Liana con fruta, Tu Aguas turbias, Re
Remolino, Ba Barro, Ll Lluvia, Br Brote, Pi Piraña, Ch Cuia chica, Al y Al6 Cuia alta; Ti Tinaja; HoA Hoja atada y Br2 Brote de
dos hojas (K3a). Comando de la tabla:
`dotnet test dotnet/AmazonFill.slnx -c Release --filter Category=Performance`.

| Tablero | Cuias | Par | A* w = 1: expandidos (ms) | Morpho w = 2 (ms) | Bot ×40 (ms) | 30 semillas: válidos · resueltos · sin presupuesto · irresolubles | Expandidos: mediana / máx. |
|---|---|---|---|---|---|---|---|
| Ch | 13 | 18 | 1.105 (28) | 0,35 | 2,9 | 30 · 30 · 0 · 0 | 298 / 25.715 |
| Al | 12 | 22 | 7.365 (47) | 0,07 | 1,0 | 30 · 30 · 0 · 0 | 4.952 / 19.282 |
| Ho | 12 | 23 | 3.694 (6) | 0,12 | 0,3 | 30 · 30 · 0 · 0 | 2.492 / 12.492 |
| Ma | 12 | 23 | 31.188 (71) | 0,13 | 0,6 | 30 · 30 · 0 · 0 | 16.562 / 102.346 |
| Li | 12 | 21 | 2.975 (6) | 0,05 | 0,3 | 30 · 30 · 0 · 0 | 1.899 / 7.871 |
| Tu | 13 | 18 | 5.605 (16; 2 realidades: SafeMoves 328 ms en el pipeline, 2,7 ms con Morpho) | 0,07 | 0,4 | 9 · 9 · 0 · 0 | 243 / 20.651 |
| Re | 12 | 20 | 67.439 (543) | 0,18 | 1,3 | 30 · 26 · 4 · 0 | 25.988 / 249.805 |
| Ba | 13 | 19 | 4.910 (21) | 0,14 | 0,8 | 30 · 30 · 0 · 0 | 1.132 / 203.333 |
| Ll | 12 | 21 | 56.095 (155) | 0,38 | 1,6 | 30 · 30 · 0 · 0 | 32.846 / 216.387 |
| Br | 13 | 18 | 1.753 (10) | 0,13 | 0,7 | 30 · 30 · 0 · 0 | 641 / 7.403 |
| Pi | 13 | 22 | 126.829 (627) | 0,28 | 1,8 | 30 · 26 · 4 · 0 | 20.387 / 273.047 |
| Ho+Ch | 13 | 19 | 1.629 (5) | 0,15 | 0,7 | 30 · 30 · 0 · 0 | 758 / 64.095 |
| Ma+Ho | 13 | 26 | 90.687 (355) | 0,13 | 4,8 | 30 · 29 · 1 · 0 | 24.226 / 168.988 |
| Li+Ho | 12 | 21 | 2.861 (6) | 0,09 | 0,3 | 30 · 27 · 0 · 3 | 1.366 / 7.491 |
| Ho+Tu | 13 | 16 | 1.257 (4; 1 realidad: SafeMoves 47 ms en el pipeline, 0,9 ms con Morpho) | 0,10 | 0,8 | 14 · 14 · 0 · 0 | 528 / 48.887 |
| Re+Li | 12 | 20 | 61.355 (399) | 0,18 | 1,4 | 30 · 27 · 3 · 0 | 11.458 / 267.050 |
| Ba+Ma | 13 | 18 | 10.598 (53) | 0,14 | 1,8 | 30 · 30 · 0 · 0 | 3.007 / 35.125 |
| Ll+Ho | 13 | 19 | 186.346 (929) | 0,29 | 0,9 | 30 · 27 · 3 · 0 | 72.290 / 291.403 |
| Br+Ba | 13 | 17 | 11.149 (34) | 0,10 | 0,6 | 30 · 30 · 0 · 0 | 1.564 / 72.203 |
| Pi+Ma | 13 | 18 | 66.472 (355) | 0,21 | 1,2 | 30 · 23 · 7 · 0 | 15.986 / 239.855 |
| Ma+Tu | 13 | 19 | 8.856 (69; 3 realidades: SafeMoves 2.977 ms en el pipeline, 11,1 ms con Morpho) | 0,19 | 1,1 | 14 · 14 · 0 · 0 | 2.824 / 19.626 |
| Ba+Tu | 13 | 17 | 4.622 (28; 4 realidades: SafeMoves 2.233 ms en el pipeline, 9,9 ms con Morpho) | 0,12 | 0,9 | 10 · 10 · 0 · 0 | 2.575 / 35.762 |
| Br+Tu | 13 | 19 | 20.841 (70; 1 realidad: SafeMoves 587 ms en el pipeline, 1,3 ms con Morpho) | 0,15 | 0,8 | 7 · 7 · 0 · 0 | 6.636 / 26.824 |
| Ll+Re | 12 | 20 | 169.921 (1.325) | 1,00 | 1,2 | 30 · 17 · 13 · 0 | 33.694 / 239.972 |
| Ll+Ba | 13 | 17 | 23.869 (162) | 0,34 | 0,8 | 30 · 29 · 1 · 0 | 8.541 / 267.015 |
| Br+Ma | 13 | 14 | 1.976 (12) | 0,16 | 0,6 | 30 · 30 · 0 · 0 | 573 / 32.957 |
| Pi+Li | 12 | 23 | 35.971 (124) | 0,19 | 1,5 | 30 · 30 · 0 · 0 | 21.335 / 213.932 |
| Re+Ma | 12 | 22 | 111.046 (666) | 0,24 | 1,5 | 30 · 25 · 5 · 0 | 62.132 / 292.464 |
| Re+Ho | 12 | 21 | 64.796 (314) | 0,18 | 1,5 | 30 · 28 · 2 · 0 | 20.677 / 156.134 |
| Ba+Li | 13 | 20 | 3.820 (12) | 0,18 | 1,5 | 30 · 30 · 0 · 0 | 776 / 32.486 |
| Br+Ho | 13 | 15 | 1.385 (5) | 0,15 | 0,5 | 30 · 30 · 0 · 0 | 170 / 4.591 |
| Ba+Re | 13 | 17 | 31.897 (433) | 0,30 | 1,8 | 30 · 26 · 4 · 0 | 2.955 / 131.883 |
| Li+Ma | 12 | 23 | 8.903 (22) | 0,18 | 1,5 | 30 · 30 · 0 · 0 | 5.373 / 17.100 |
| Ll+Li+Al6 | 12 | 22 | 36.553 (113) | 0,81 | 1,2 | 30 · 30 · 0 · 0 | 14.282 / 58.071 |
| Pi+Ba | 14 | 18 | 8.678 (81) | 0,35 | 1,3 | 30 · 29 · 1 · 0 | 1.105 / 166.983 |
| Ma+Li | 12 | 22 | 14.292 (43) | 0,25 | 0,5 | 30 · 30 · 0 · 0 | 7.366 / 29.035 |
| Ti | 12 | 23 | 7.946 (16, sin poda; 6 realidades: SafeMoves 510 ms en el pipeline, 5,0 ms con Morpho) | 0,25 | 1,0 | 30 · 30 · 0 · 0 | 4.434 / 25.596 |
| HoA | 13 | 17 | 1.965 (8) | 0,20 | 0,5 | 30 · 30 · 0 · 0 | 498 / 25.202 |
| Br2 | 13 | 18 | 2.068 (6) | 0,18 | 0,6 | 30 · 30 · 0 · 0 | 580 / 25.266 |

Ll+Li+Al6 no pasa hoy `LevelValidation` por el bloqueo provisional de la Liana con capacidades mixtas (C1-9); el solver la
resuelve igual. **Ti** (Tinaja, M13; T55, 2026-10-13, .NET 10 Release en el equipo de escritorio x64): el reto o hito de c.9
de la propuesta de la tinaja (K = 9, 1 vacío, 5 dosis con 4 ocultas), con las búsquedas sin poda de fútiles; en sus 30
semillas, las 30 tienen ruta segura (2-24 realidades). **HoA** y **Br2** (variantes de la 1.0; K3a, 2026-10-14, .NET 10 Release en
el equipo de escritorio x64): `InverseWalk` con K = 10 en 13 cuias de 4 (E = 2) y la variante sobre una cuia elegida con el PRNG
del núcleo (la Hoja atada sobre una cuia con dosis, con la llave = la fruta de su fondo; el Brote de dos hojas sobre una con 1-3
dosis y espacio ≥ 2); en sus 30 semillas, las 30 son válidas y las resuelven A* y Morpho. Criterios de C3-3:

1. **Asignaciones** (`HotLoopAllocationTests.ReferenceBoard_HotLoop_DoesNotAllocate`, los 39 tableros, Ti, HoA y Br2 incluidos): 0
   bytes en `SolverMoves`, `IsStuck`, `BoardRules.ApplyInPlace` y el bot ×40 calentado; menos de 1 KB por `Solve`
   calentado y por realidad en `SafeMoves`. Cumplido.
2. **A\* (w = 1)**: los 39 tableros (Ti, sin poda) se resuelven con 186.346 expandidos como máximo (Ll+Ho, 0,93 s); el
   número de cada uno queda fijado con un 25 % de margen (`MechanicsPerformanceTests`). En el reparto de 30 semillas por
   combinación, el presupuesto de 300.000 se agota en Ll+Re 13/30, Pi+Ma 7/30, Re+Ma 5/30, Re, Pi y Ba+Re 4/30, Re+Li y
   Ll+Ho 3/30, Re+Ho 2/30 y Ma+Ho, Ll+Ba y Pi+Ba 1/30; en el resto, nunca. El Remolino es lo que más estados añade y la
   Lluvia y la Piraña los multiplican por 2-3, como estimaba 03 §9. El pipeline tendrá que descartar o regenerar esos
   tableros (sin par exacto).
3. **Morpho (w = 2, 40.000)**: determinista (mismo resultado con un solver nuevo y con uno reutilizado) y 1,0 ms como máximo
   (objetivo < 50 ms). En los tableros turbios, `SafeMoves` con las opciones de Morpho tarda 0,9-11,1 ms (02 T2: < 50 ms);
   con las del pipeline (w = 1 y 20.000 nodos por realidad), 47-2.977 ms por estado, así que la ruta segura de un nivel de 13
   cuias puede costar minutos offline (dato para P-11).
4. **Bot ×40**: 4,8 ms como máximo (objetivo < 200 ms) y reproducible por semilla.
5. **Heurística con Piraña**: `H`, exacta (ver `SolverHeuristic`). Con `ceil(H/2)`, la cota del plan [P], medida con un parche
   local que no se publicó, el presupuesto se agota en Pi 29/30, Pi+Ma 30/30, Pi+Li 27/30 y Pi+Ba 30/30 (con `H`: 4, 7, 0 y 1)
   y Morpho tarda 17-85 ms.

**Evidencia para el GDD §6 #6** (Lluvia y Piraña en la 1.0 si el solver con estado temporal pasa los tests): con capacidades
uniformes y mixtas, A* coincide con la búsqueda exhaustiva y la heurística es admisible y consistente en todos los estados
alcanzables (32 tableros por mecánica y 8 por combinación del calendario, en `run` y `single`), P1-P3 se cumplen (R14) y los
tableros de referencia cumplen los presupuestos de 03 §5 y 08 §14.2. La decisión es del dueño.

La cancelación por tiempo del solver para Morpho (`hint_timeout_ms`), que figuraba aquí como pendiente, está cerrada desde K2:
el núcleo sigue sin leer relojes, el cliente marca una `SolverCancellation` y, tras `SafeMoves.Find`, `Outcome` = `BudgetExceeded` con
`WasCancelled` es la respuesta 4 de Morpho (sección «Hilos y corte por tiempo»). Pregunta abierta: 02 R16 con capacidades
mixtas (C1-8, C1-9: la poda de fútiles puede alargar el par o dejar sin camino útil un tablero resoluble, y con una Liana hay
estados atascados con solución); ver el CHANGELOG. La consecuencia de Q-B en P3 (vaivén útil del Remolino que vuelve al mismo
estado) ya está decidida: se ratifican P1-P3 con la excepción del Remolino en P3 (pregunta 34 de `docs/GDD.md` §6.3, (a);
D-020; 02 R16). Para el jugador, el vaivén sin salida de la pregunta 15 lo cubre R13 ampliada (D-014; sección siguiente).

## Aguas estancadas por bucle (R13 ampliada, D-014)

Defecto que encontró el dueño en el nivel 66 (`docs/dev/traspaso/pruebas-telefono-2026-10-13.md`): tras
`3>6 4>3 7>4 0>5 1>5 2>0 2>1 5>2 1>5 2>1 0>7 1>2` quedan dos vertidos útiles, `2>1` y `7>0`, que son vaivenes parciales sin
salida. R13 solo mira si queda algún vertido útil, así que nunca salía Aguas estancadas y el jugador daba vueltas.

**Regla (D-014, decisión del dueño del 2026-10-13).** Con S en `Playing` según R13, `LevelSession` recorre el cierre de estados
alcanzables desde S (`Session.DeadEndProbe`). Si se agota con **≤ 64 estados [P]** (`DeadEndProbe.MaxStates`, S incluido; constante
del núcleo, no de Remote Config) y ninguno es victoria (R12), S queda en `Stuck`. Si pasa de 64 o encuentra una victoria, S sigue en
`Playing`: es conservador. Corre en cada evaluación de estado de la partida (constructor, `TryPour`, `Undo`, `Restart`, `Reveal`,
`RevealAll`, `RestoreRevealed`; la Cuia extra y la de rescate rehacen la sesión). `BoardRules.EvaluateStatus`, `IsStuck`,
`PourRules.IsStuck`, el solver, el bot, `EarlyTraps`, `LevelValidation`, el generador y level-qa no cambian: los packs, los informes
y los pares son idénticos (`--check` del tutorial y de la campaña 11-200).

- **Recorrido:** en anchura, en orden determinista (vertidos (A, B) ascendentes sobre el tablero en orden canónico), con estados
  canónicos (`BoardCanon.Sort` + FNV-1a de 64 bits; la igualdad se confirma por valor, `Packed` cuia a cuia), aplicando los vertidos
  con `BoardRules` (R10 completo). Las mecánicas con estado y los contadores de Lluvia y Piraña forman parte del estado: si el cierre
  crece por ellos más allá de 64, no se declara. Con Aguas turbias usa el estado verdadero (el núcleo conoce las ocultas; un vertido
  las revela como en la partida). El veredicto no depende del orden (el cierre es un conjunto).
- **Todos los vertidos legales, no solo los útiles.** El texto de la decisión habla de vertidos útiles, pero con capacidades mixtas
  un fútil puede ser parte de la única salida (C1-8) y un cierre solo con útiles declararía perdidos tableros que se ganan, en contra
  de la condición de la misma decisión («nunca declara atascado un tablero que se puede ganar»; R14). Medido: en el propio nivel 66,
  tras los 6 primeros vertidos del dueño, la búsqueda con solo útiles se agota en 50 estados sin victoria y el nivel se gana en 14
  con algún fútil; en partidas al azar sobre los niveles 1-200, un cierre solo con útiles habría declarado 327 estados, 56 de ellos
  ganables. En `run`, un fútil entre dos cuias sin mecánica y de la misma capacidad no cambia el estado canónico; con capacidades
  distintas sí puede cambiarlo (P2, 02 R16). Así que, sin mecánicas y con capacidades iguales, los dos cierres son idénticos en `run`.
  Por eso tampoco depende de `futile_rule_enabled`: el kill-switch devuelve R13 al criterio clásico, pero el cierre sigue.
  Es la opción (a) de la pregunta 46 de `docs/GDD.md` §6.3 (T50), la recomendada, aplicada por el dueño el 2026-10-13. R14 se reformula con D-014 («mientras exista progreso posible») y P1 se sigue exigiendo (`DeadEndProbePropertyTests`).
- **Nivel 66:** Aguas estancadas sale justo tras el 9.º vertido (`1>5`), cuando el nivel queda perdido de verdad (4 estados
  alcanzables; A* con todos los legales: irresoluble); los vertidos 10-12 del dueño ya eran vueltas del bucle y ahora no entran
  (R15). Hasta el 8.º el nivel se gana.
- **Coste** (.NET 10 Release, equipo de escritorio x64, 2026-10-13; `DeadEndProbePerformanceTests`, comando de la tabla de arriba):
  0,015 ms en el tablero del 66 (cierre entero), 0,093 ms en el de 14 cuias (se llena) y, en 4.026 estados de partidas al azar sobre
  los 36 tableros de referencia, 0,043 ms de media y 0,322 ms en el peor (Tu, 64 estados): objetivo < 1 ms. Sin asignaciones tras
  calentar (`HotLoopAllocationTests.WarmDeadEndProbe_DoesNotAllocate`); cada `LevelSession` reserva sus búferes una vez (unos 8 KB).
- **Tests:** compartidos `DeadEndProbeTests` (el tablero del 66, la secuencia completa, Marea y Volver a la orilla, el estado ganable
  del 66 cuyo cierre útil no tiene victoria, orden canónico, los 10 niveles del tutorial a lo largo de su solución, cierre de más de
  64, el tablero mixto que solo se gana con fútiles, Aguas turbias con la oculta verdadera, tablero ganado) y `RuleCatalogTests`
  (R13 ampliada; R14 reformulada, con el tablero mixto que solo se gana con fútiles a lo largo de su solución; R15 con el vaivén);
  solo dotnet `DeadEndProbePropertyTests` (si el cierre declara, A*
  exhaustivo da irresoluble, con todos los legales y con la poda: 36 tableros de referencia en `run` y `single` y los niveles 1-200,
  y los tableros de referencia baratos siguen en Playing a lo largo de su solución). El tablero del 66 y la secuencia del dueño
  están en un solo helper compartido, `Level66Board` (K1), que usan `DeadEndProbeTests`, `DeadEndProbePerformanceTests` y
  `HotLoopAllocationTests`. `StuckLoopTests` (PlayMode) juega el 66 desde su pack y lleva su propia copia de la secuencia, porque
  AF.Core.Tests es solo de Editor.

## Restaurar las capas reveladas (Aguas turbias)

`LevelSession.RestoreRevealed(int cuia, int layers)` (T01, paso 1 del traspaso) hace OR de `layers` (bit k = la dosis oculta
inicial `[cuia, k]`) en la máscara de revelado, la aplica a `Current`, recalcula `Status` como `Reveal` (R10 g-h) y devuelve
cuántas capas nuevas reveló. Es idempotente (0 y nada cambia si ya lo estaban), no entra en el historial de Marea (04 §2.1) y
es permanente como `Reveal` (I5: sigue tras Marea y Volver a la orilla). Lanza `ArgumentOutOfRangeException` con una cuia
fuera del tablero y `ArgumentException` con `layers` negativa o con bits que no son dosis ocultas de la cuia en `Initial`.

Cubre lo que `Reveal` y `RevealAll` no expresan: las capas reveladas solo en parte (un vertido puede revelar una de las dos
ocultas de una cuia, R7; en el nivel 100 la cuia 0 tiene dos). Dos usos, ambos sobre una sesión nueva construida desde el
tablero inicial con su historial rehecho:

1. **Reanudar** (02 §14, «App cerrada o muerta a medio nivel»; R8: la máscara de revelado es parte del estado persistente):
   se rehacen los vertidos guardados y se restaura la máscara guardada de cada cuia.
2. **Rehacer la sesión con la Cuia extra o la Cuia de rescate** (04 §2.2 y §6; `LevelFlow.Funnel.AddCuia`): la cuia vacía va
   al final, así que los índices de las demás no cambian y `RevealedLayers(i)` de la sesión anterior se copia cuia a cuia.

Las capas que el historial rehecho ya reveló cuentan como reveladas y no suman. En los dos usos la máscara se restaura **al
final del rehacer**, como hace el cliente desde la integración del paso 1 (`docs/dev/traspaso/README.md` §6): el historial `h2` de `level_state` guarda por cuia
solo la máscara revelada (la cuia en 1 dígito hex y la máscara en 2), sin el punto del historial en que se reveló cada capa, y
`LevelSession` tampoco lo expone. Es exacto aunque una capa se revelara a mitad del historial sin que el historial rehecho la
revele (con la Luciérnaga o con un vertido que Marea deshizo después): ver una oculta solo cambia lo que pasa después si cambia
la racha (R6) o el sellado (R5), y con T4 (02 §11; `LevelValidation`: bajo una dosis oculta nunca va el color de la de
encima) eso solo ocurre si un lavado de Barro (02 §9.1 M5) deja encima de la oculta su mismo color. Ningún nivel de 1-200
lleva Barro en una cuia turbia: de los 7 tableros turbios (100, 106, 115, 135, 155, 175 y 195), el 135 y el 195 lo llevan
solo en otras cuias, y los tableros de relief no tienen ocultas. Si un pack futuro lo combinara, habría que guardar el punto
del revelado (y restaurar la capa ahí) o prohibir la combinación en `LevelValidation`.
`RestoreRevealed_AtTheRevealPoint_ReplaysTheHistoryExactly` documenta ese caso teórico: con la capa restaurada en su punto,
el vertido siguiente mueve 2 dosis; sin ella, 1.

La Pororoca queda fuera de los dos usos: su revelado va justo antes de un barajado que vacía el historial y cambia el tablero
(04 §2.1 y §2.4 a; 02 §14), así que tras una Pororoca la sesión ya no se rehace desde `Initial` más los vertidos (fuera del
paso 1). Tests: `LevelSessionMurkyTests.RestoreRevealed_*`.

## Tinaja (M13)

T55 (tanda 3) implementa las secciones c.1 y c.2 de `docs/research/cultura-ribera/propuesta-tinaja.md` con su ficha b.2
(D-019: el dueño aceptó el 2026-10-13 las recomendaciones de (g): Q-Ti1 A, Q-Ti2 A, Q-Ti5 A y Q-Ti6 A). El generador, el
esquema (`pack_schema` 3), level-qa y Unity son de T56 y de una tarea posterior.

- **Piezas:** `{"type":"tinaja"}` (capacidad 5, 3-5 dosis, `hidden` = todas menos la boca) y `{"type":"cucharon"}` (Cuia chica
  de 2, vacía y contigua), sin campos: una tinaja por nivel. Las ocultas de la tinaja usan la máscara de Aguas turbias, así
  que todo lo que hoy lee «hay ocultos» como Aguas turbias tiene que distinguirla (inventario en c.5 y c.6).
- **`legal`:** el filtro de origen `IMechanic.AllowsTarget` es la primera comprobación de `BoardRules.Accepts`: la tinaja solo
  vierte en su cuia (ni en una Hoja vacía de su fruta ni en un Barro expuesto). La tinaja nunca recibe; su cuia, mientras
  cuelga, solo recibe de la tinaja, de cualquier fruta. Sacar mueve siempre 1 (T4).
- **`futil`:** la tinaja y su cuia colgada están exentas como origen; Φ = 2 · (dosis en la tinaja) + (dosis en su cuia) baja
  con cada vertido exento, así que no hay ciclos de exentos. Mientras cuelga, R13 de reglas (`PourRules.IsStuck`) solo se
  declara sin vertidos legales.
- **R10 (f):** cuando no queda ninguna tinaja con dosis, su cuia se suelta (`CucharonReleasedMask` y `ReleasedMask`) en el
  mismo vertido en todas las realidades. Marea la vuelve a colgar; lo revelado persiste (I5).
- **Búsquedas que deciden «hay solución»** (par, `SafePath`, `SafeMoves` del pipeline y de Morpho, paseos, prueba de
  irresolubilidad, verificación del rescate): en un nivel con tinaja, **sin poda de fútiles**
  (`SolverOptions.ForPipeline(mode, level)`, `SafeMoves.ForPipeline(mode, level)` u `options.ForLevel(level)`). Todo nivel con
  tinaja acaba con capacidades mixtas y la poda puede dejar sin camino útil un tablero que se gana: en el contraejemplo de
  b.2, A* con poda da «irresoluble» y sin poda gana en 10 (`TinajaSolverTests`). El bot sigue con la poda.
- **Morpho y el informe:** `SafeMoves.Outcome` da la respuesta: `Found` (propone y cobra), `NoExit` («Desde aquí no hay
  salida»), `SomeRealitySolvable` (Luciérnaga, sin prometer; nunca con R = 1), `BudgetExceeded` («No encuentro un camino»);
  en los tres últimos no cobra. `SafePathResult.NoSafeMoveOutcome` lo da en la ruta segura.
- **Medido:** P1-P3 de R14 con capacidades 2, 4, 5 y 6 en `run` y `single` (unos 20.000 estados por combinación, con la cuia
  colgada y suelta) sin violaciones; A* con y sin poda coincide con la búsqueda exhaustiva en 24 tableros pequeños; `H`
  admisible y consistente; 0 asignaciones en el bucle caliente; tablero de referencia «Ti» en la tabla de evidencias.
- **Vaivén útil en un nivel perdido** (el tablero perdido de b.2 y el vaivén con la cuia ya suelta): `PourRules.IsStuck`
  (R13 de reglas) no lo declara; la partida sí, por D-014 (cierre ≤ 64 estados sin victoria en `LevelSession`; T51). La
  antigua nota [P] para 02 R13 queda sustituida por D-014. `TinajaSolverTests.Morpho_TheLostBoardOfB2_…` y
  `FutilePropertyTests.P3_Tinaja_AfterTheRelease_…` fijan solo lo de las reglas.
- **Abierto (no es del núcleo):** la Pororoca con la tinaja y el tope de ocultas por tipo de nivel (pipeline). El corte por
  `hint_timeout_ms` ya tiene su bandera en el núcleo (K2, sección siguiente); el reloj y el hilo son del cliente (MO1).

## Hilos y corte por tiempo (K2)

Fuentes: 08 §9 (`hint_timeout_ms` = 150, rango 100-300; «al agotarse no se cobra»), 03 §5 («el cliente ejecuta el solver en un
hilo y cancela por tiempo»), 04 §2.3 y 02 T5 (orden de respuestas de D-019: la 4 es «No encuentro un camino desde aquí», sin
cobrar). **El núcleo no lee relojes.**

- **Regla de hilos:** una instancia de `AStarSolver` o de `SafeMoves` por hilo (ninguna es segura entre hilos: buffers, nodos,
  montículo y memo propios). Los `BoardState` son inmutables y se pueden compartir entre hilos, igual que las opciones
  (`SolverOptions` es un `readonly struct`). El núcleo no tiene estado estático mutable. `SolverCancellation` es lo único que
  toca otro hilo. Probado: 4 hilos con su propia instancia sobre 8 tableros de referencia dan lo mismo que en serie
  (`SolverThreadingTests`).
- **`SolverCancellation`:** clase sellada con un `int` volátil y `Request()`, `IsRequested` y `Reset()`. La marca el hilo
  principal del cliente al pasar `hint_timeout_ms` con su `IClock`; el núcleo solo la consulta. Se pasa con
  `options.WithCancellation(flag)` (`SolverOptions.Cancellation`). Null por defecto y en el pipeline, con el comportamiento de
  siempre; `ForLevel` y `WithFutileRule` la conservan. `Reset()` solo con la búsqueda parada (antes de la petición siguiente); si
  no, una instancia nueva por petición.
- **`AStarSolver`:** consulta la bandera antes de la expansión 1 y luego cada `SolverCancellation.PollInterval` = 256 expansiones
  [P] (una máscara sobre el contador: sin asignaciones ni closures). Al verla devuelve `BudgetExceeded` con
  `SolveResult.WasCancelled` y `Expanded` = las expansiones hechas (múltiplo de 256). Así, tras `Request()` expande como mucho 256
  estados más. Con null no consulta nada: los 37 tableros de referencia dan los mismos expandidos, estados y soluciones
  (pipeline, Morpho en `run` y `single` y `SafeMoves`), medido antes y después del cambio. Con una bandera sin marcar, lo mismo
  (`SolverThreadingTests`).
- **`SafeMoves`:** consulta la bandera al empezar, tras enumerar las realidades (si la enumeración falla, gana
  `RealitiesRejected`), y la propaga desde cada búsqueda A*: `Count` = 0, `Outcome` = `BudgetExceeded` y `WasCancelled`. No hay
  Pista parcial, aunque ya hubiera vertidos seguros demostrados, y el orden de respuestas de Morpho no cambia. Lo cortado no
  entra en el memo; lo terminado antes del corte sí, y vale para la llamada siguiente. Si la bandera llega cuando la llamada ya
  terminó, `WasCancelled` = false y el resultado es completo. `TrueParAfter` usa la bandera de la última `Find`: si su búsqueda
  se corta, devuelve `BudgetExceeded` (no entra en el memo) y `WasCancelled` = true solo para esa llamada (par no demostrado),
  sin tocar `Count`, `Outcome` ni los vertidos. `Find` y `TrueParAfter` reinician `WasCancelled` al empezar, y la respuesta 4
  se lee solo tras `Find`. En el pipeline (`SafePath`, el generador y level-qa) va en null.
- **Asignaciones** (`HotLoopAllocationTests.SolverCancellation_UnmarkedOrCutting_DoesNotAllocate`): con la bandera sin marcar, el
  solver calentado solo asigna la solución; `Solve` cortado y `SafeMoves` cortado, 0 bytes.
- **Tests:** compartidos `SolverCancellationTests`: marcada antes (0 expansiones; `SafeMoves` sin ninguna búsqueda), marcada tras
  N expansiones (para en ≤ N + 256), null idéntico, corte en cualquier consulta de `SafeMoves` y memo limpio tras `Reset`, y
  corte en `TrueParAfter` (respuesta de `Find` intacta y `WasCancelled` reiniciado en la llamada siguiente). Solo
  dotnet `SolverThreadingTests`: 4 instancias en 4 hilos sobre 8 tableros, null idéntico en los 37 tableros de referencia y un
  corte marcado de verdad desde otro hilo, en `AStarSolver` y en `SafeMoves`. Para los tests compartidos, que no pueden
  sincronizarse con un hilo a mitad de búsqueda, `SolverCancellation.RequestAtPoll(k)` (interno) simula que otro hilo la marca
  justo antes de la consulta k.

## Variantes de la 1.0: Hoja atada y Brote de dos hojas (K3a)

Fuentes: 02 §1.2 (`legal`: «la Hoja atada sí es origen» y «solo recibe su fruta (vacía o con ella en la cima)»), 02 §1.4 (anexo B:
la Hoja atada en `cambia_al_recibir`), 02 R10 (b) (el Brote de dos hojas «resta las dosis recibidas»), 02 R16 (P1-P3 antes de cada
estreno), 02 §9.1 (variantes de D-007) y la propuesta de D-007 b.3; formato, la firma de `pack_schema` 3 (pregunta 8:
`hoja_atada {key}` y `brote` con `n` ∈ {1, 2}). Estrenos: Hoja atada en el 283 y Brote de dos hojas en el 343.

- **Hoja atada** (`MechanicType.HojaAtada` = 10 [P], que no se persiste; clave `"hoja_atada"`; `HojaAtadaMechanic`): filtro de
  destino persistente de la fruta `key` (solo su fruta, y solo vacía o con ella en la cima; la regla normal decide el resto). Sí es
  origen y nunca se suelta, ni al recibir ni al sellar (sellada sigue atada y vale para ganar). Como destino nunca es fútil (anexo
  B); como origen no está exenta. Sin estado propio. Puede empezar con dosis si la del fondo es de su fruta (b.3). `BoardBuilder`
  exige `key` (un tinte de 1 a 14) y nada más; no lleva bandera. `Cuia.MechanicKey` devuelve su llave.
- **La Hoja atada es la casa de su fruta [P]** (validación): su capacidad es la `U_c` de su llave. Sin esta regla, con capacidades
  mixtas la exención de destino de `futil` rompe P3 de 02 R16: la fruta va de su casa a la hoja atada (útil, porque cambia al
  recibir) y vuelve (útil, porque va a la casa correcta), y el tablero vuelve al mismo estado canónico; lo fija
  `FutilePropertyTests.P3_HojaAtada_NotTheHomeOfItsFruit_ARoundTripReturnsToTheSameState`. Con la regla, P1-P3 se cumplen en `run`
  y `single` con capacidades uniformes y mixtas. Es coherente con b.3 («conserva la cuerda tras sellar»). **Pregunta para el dueño**
  (con el generador, P2b, que tiene que respetarla): ratificarla o elegir otra salida (una excepción de P3, como la del Remolino).
- **Brote de dos hojas** (el Brote con `n` = 2; `BroteMechanic`): `Cuia.MechanicN` guarda las hojas cerradas que le faltan (2 al
  empezar, 1 tras recibir una dosis; 0 en el Brote normal, que equivale a una). En la etapa (b) de R10 resta las dosis recibidas
  (`IMechanic.OnReceived` recibe ahora la cantidad) y germina al llegar a 0, como el Brote normal; en `run` un tramo de 2 lo abre
  de una vez y en `single` hacen falta dos vertidos. Abrir una hoja sin germinar es el evento `PourEvents.BroteLeafOpenedMask` (no
  entra en `OpenedMask` ni en `WasUnlocked`). El contador va en `Packed`, así que entra en el hash y en la clave canónica (03 §3).
  `BoardBuilder` lee `n` ∈ {1, 2}: sin `n` o con 1 es el Brote normal, con `n` = 0 en la cuia y el mismo `Packed` y el mismo hash
  que antes (los packs 1-200 no cambian); con 2, el de dos hojas. Validación: espacio ≥ 2 al empezar (b.3) y, en un tablero inicial,
  solo `n` = 0 o 2.
- **Solver, bot y heurística:** sin código propio. `H` y `H_single` siguen siendo admisibles y consistentes (los vertidos mueven
  las dosis igual: cada término baja ≤ 1 por paso), y A* con `w = 1` da el par exacto: A* frente a la búsqueda exhaustiva y la
  heurística en todos los estados alcanzables de 32 tableros por variante, uniformes y mixtos, en `run` y `single`
  (`FilterSolverTests`). El bot no cambia (su puntuación sigue congelada).
- **Combinaciones:** la Hoja atada es un filtro (02 §9.1 la cuenta así en la Tinaja), así que `LevelValidation` la deja ir con
  Aguas turbias y con la Tinaja, como la Hoja; nunca lleva ocultas ni Barro (reglas generales de las cuias con mecánica).
- **Pack:** el núcleo acepta los campos; que solo aparezcan con `pack_schema` 3 lo comprueban el esquema y el generador (P2b) y el
  cliente (TJ1). Los packs 1-200 no cambian (`--check` del tutorial y de la campaña 11-200).
- **Tests:** compartidos `HojaAtadaTests` y `BroteDosHojasTests` (una regla por test, con su §), `FilterSolverTests` (A* y
  heurística) y `CuiaAttrTests` (claves y `MechanicKey`); solo dotnet `FutilePropertyTests` (P1-P3 en `run` y `single`, uniformes y
  mixtas, y el contraejemplo de la regla [P]), `HotLoopAllocationTests` (un tablero por variante: 0 bytes en reglas, vertidos y bot;
  menos de 1 KB por `Solve`) y una fila por variante en `ReferenceBoards` y `MechanicsPerformanceTests` (HoA y Br2).
