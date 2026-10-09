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
- Los `.meta` los genera Unity al abrir el proyecto y se confirman en git junto con su archivo.

## Verificación

```powershell
dotnet build dotnet/AmazonFill.slnx -warnaserror
dotnet test dotnet/AmazonFill.slnx
```

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
| `AF.Core` | Barro (M5, debut 123; núcleo 2): color 15 inerte dentro del contenido, sin despacho de mecánica. Con Barro en la cima (`Cuia.MudExposed`) la cuia no es origen y acepta cualquier color; el primer vertido lo lava y ocupa su lugar (`CuiaRules.EffectiveSpace`: espacio + 1); una cuia con Barro no está sellada ni vacía; evento `BarroWashedMask` / `PourResult.BarroWashed`; validación: capa 2-3 de una cuia de 4, nunca con mecánica ni oculto | 02 R2, §1.2, §9.1 M5; 03 §9; propuesta b.2 M5 |
| `AF.Core` | Aguas turbias en la partida (núcleo 2): `LevelSession` con máscara de revelado del nivel (`RevealedLayers`), que persiste tras Marea y Volver a la orilla; `Reveal(cuia)` (Luciérnaga) y `RevealAll()` (Pororoca); `PourEvents.RevealedMask`; validación T2-T4 con los límites de 08 §9 como parámetros y las combinaciones de Q14 A | 02 R7, §11 T2-T4, T6; 04 §2.1, §2.4 a, §2.5, §2.7, §5.1; D-007 |
| `AF.Core` | `Mechanics.IMechanic` (interno): ganchos de mecánica sin estado (origen, destino, espacio, recepción, `cambia_al_recibir`, exención de origen, etapas (b), (d), (e), (f), victoria y validación), despachados por una tabla estática indexada por el tipo de la cuia | 08 §2.2; 02 §9 |
| `AF.Core` | `Rules.CuiaRules`: envoltorio heredado sobre cuias sueltas sin mecánicas; capa base de `BoardRules` para el sellado (R5) y el núcleo de `futil` | 02 §1.2, §1.4, §3.1; 04 §5.1 |
| `AF.Core` | `Rules.Pour` y `Rules.PourRules`: validan argumentos y delegan en `BoardRules`: vertido según el modo (parcial solo en `Run`), `Apply` con `PourEvents`, `CanBeSource`, `IsFutile`, `CollectUsefulMoves`, `HasUsefulMove`, `IsStuck` | 02 §1.2-1.4 R9-R11, R13, R14, R16; §3.1 C1-C2 |
| `AF.Core` | `Session.LevelSession`, `LevelStatus`, `PourResult` (con `Events`): partida con R10 en un modo de vertido fijo, Marea (deshacer; una dosis por uso en `Single`) y Volver a la orilla (reinicio) | 02 §1.3-1.4, §2; 04 §2.1, §2.7 |
| `AF.Core` | `Determinism.Prng`: xoshiro256** sembrado con SplitMix64 desde la FNV-1a de 64 bits de `amazonfill:v1:m{mm}:v{v}:n{nnnn}` (`LevelSeed`), sub-semillas (`SubSeed`) y `NextInt(bound)` sin sesgo (Lemire); solo enteros, valores dorados en `PrngTests` | 09 R26; 08 §2.1 #5, §4.1 |
| `AF.Solver` | `Collections.MinHeap`: montículo con desempate FIFO (sustituto de `PriorityQueue`) | 08 §2.2 |
| `AF.Solver` | `Bot.CasualBot`, `BotOptions`, `BotResult`: bot casual ×40 (85/15, tope 400) sobre `SolverMoves` y `BoardRules`, con `PUNTUACION_CASUAL` ×4 en enteros y ruido entero equivalente, congelada; `fallo_bot` reproducible por semilla (`SeedFor`) y sin asignar una vez calentado | 03 §6-§7; 08 §2.2, §14.2 |
| `AF.Solver` | `Generation.InverseMoves`, `InverseWalk`, `InverseDeep`, `InverseSpec`, `Complexity` y `Collections.StateSet`: barajado inverso (`WALK` con mejor prefijo; `DEEP` en DFS con 20.000 nodos) sobre K, E, capacidades y `U_c`, con el PRNG del núcleo, la puntuación en enteros, la complejidad de laicwben2 y solución por construcción | 03 §4.1-§4.2; 02 §8 |
| `AF.Solver` | `Search.EarlyTraps`: trampas tempranas (jugadas iniciales que atascan en ≤ 2 vertidos; `CountUnavoidable` en lectura estricta) | Q16 B de D-007 [P] |
| `AF.Solver` | `Search.SolverMoves` y `SolverMove`: `MOVIMIENTOS` de 03 §5 (útiles, con la cantidad del modo, cuias idénticas una sola vez y fútiles marcados con el kill-switch apagado), sin asignaciones; los usan el solver, el bot y las métricas | 03 §5 |
| `AF.Solver` | `Search.AStarSolver`, `SolverOptions`, `SolveResult`, `SolveStatus`: A* (w entero) sobre estados canónicos en los dos modos de vertido (`SolverOptions.ForPipeline(mode)`), presupuesto de expandidos y prueba de irresolubilidad; a igual `f`, primero el de menor `g` (tupla `(f, g, S)` de 03 §5 y del prototipo) | 03 §5; D-005 |
| `AF.Solver` | `Search.SolverHeuristic`: `H = max(cortes, dispersión)`; en `Single`, `H_single = max(dosis sobre la racha del fondo, dispersión)` [P]; con Barro, `h1` resta 1 por barro y `h2` ignora el 15 (03 §9) | 03 §5, §9; decisiones.md §2.1.3 |
| `AF.Solver` | `Murky.RealityEnumerator`: realidades de Aguas turbias (asignaciones de las posiciones ocultas coherentes con lo visible, `units_per_color` y T4 respecto al tablero inicial; > 24 → `TooMany`, el nivel se rechaza), sin asignaciones | 02 §11 T2, T4; 03 §8; 08 §2.2 |
| `AF.Solver` | `Murky.SafeMoves`: vertidos útiles seguros en todas las realidades (A* por realidad: 20.000 nodos en el pipeline, `ForPipeline`; el cliente pasa `hint_budget_nodes`), memo exacto por estado canónico y un único `AStarSolver` (`SolveLength`, sin construir la solución); sin seguro, `Count` = 0 (T5: Morpho no cobra) | 02 §11 T5; 03 §8 |
| `AF.Solver` | `Murky.SafePath`: `RUTA_SEGURA` (hasta 400 pasos; en cada uno, el seguro de menor par en la realidad verdadera), con T4 obligatoria en el tablero inicial (`RejectedT4`) | 03 §8; 02 §11 `validar_turbio` |

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

Pendiente de portar: Remolino (C2-4), Lluvia (C3-1) y Piraña (C3-2); cancelación por tiempo del solver para Morpho
(`hint_timeout_ms`; el núcleo no lee relojes); el generador `fill` y el pipeline (`tools/`). Pregunta abierta de 02 R16 (C1-8,
C1-9): con capacidades mixtas la poda de fútiles puede alargar el par o dejar sin camino útil un tablero resoluble, y con una
Liana hay estados atascados con solución (P1 de R14 falla); ver el CHANGELOG.
