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
| `AF.Core` | `Mechanics.IMechanic` (interno): ganchos de mecánica sin estado (origen, destino, espacio, recepción, `cambia_al_recibir`, exención de origen, etapas (b), (d), (e), (f), victoria y validación), despachados por una tabla estática indexada por el tipo de la cuia | 08 §2.2; 02 §9 |
| `AF.Core` | `Rules.CuiaRules`: envoltorio heredado sobre cuias sueltas sin mecánicas; capa base de `BoardRules` para el sellado (R5) y el núcleo de `futil` | 02 §1.2, §1.4, §3.1; 04 §5.1 |
| `AF.Core` | `Rules.Pour` y `Rules.PourRules`: validan argumentos y delegan en `BoardRules`: vertido según el modo (parcial solo en `Run`), `Apply` con `PourEvents`, `CanBeSource`, `IsFutile`, `CollectUsefulMoves`, `HasUsefulMove`, `IsStuck` | 02 §1.2-1.4 R9-R11, R13, R14, R16; §3.1 C1-C2 |
| `AF.Core` | `Session.LevelSession`, `LevelStatus`, `PourResult` (con `Events`): partida con R10 en un modo de vertido fijo, Marea (deshacer; una dosis por uso en `Single`) y Volver a la orilla (reinicio) | 02 §1.3-1.4, §2; 04 §2.1, §2.7 |
| `AF.Core` | `Determinism.Prng`: xoshiro256** sembrado con SplitMix64 desde la FNV-1a de 64 bits de `amazonfill:v1:m{mm}:v{v}:n{nnnn}` (`LevelSeed`), sub-semillas (`SubSeed`) y `NextInt(bound)` sin sesgo (Lemire); solo enteros, valores dorados en `PrngTests` | 09 R26; 08 §2.1 #5, §4.1 |
| `AF.Solver` | `Collections.MinHeap`: montículo con desempate FIFO (sustituto de `PriorityQueue`) | 08 §2.2 |
| `AF.Solver` | `Search.SolverMoves` y `SolverMove`: `MOVIMIENTOS` de 03 §5 (útiles, con la cantidad del modo, cuias idénticas una sola vez y fútiles marcados con el kill-switch apagado), sin asignaciones; los usan el solver, el bot y las métricas | 03 §5 |
| `AF.Solver` | `Search.AStarSolver`, `SolverOptions`, `SolveResult`, `SolveStatus`: A* (w entero) sobre estados canónicos en los dos modos de vertido (`SolverOptions.ForPipeline(mode)`), presupuesto de expandidos y prueba de irresolubilidad; a igual `f`, primero el de menor `g` (tupla `(f, g, S)` de 03 §5 y del prototipo) | 03 §5; D-005 |
| `AF.Solver` | `Search.SolverHeuristic`: `H = max(cortes, dispersión)`; en `Single`, `H_single = max(dosis sobre la racha del fondo, dispersión)` [P] | 03 §5; decisiones.md §2.1.3 |

Rendimiento medido (2026-10-06, .NET 10 Release, Windows ARM64): los 140 niveles de `tools/out*/levels_*.json` dan el
mismo par que `levelgen_proto.py`; un tablero de 14 cuias (12 colores, 2 vacíos, par 37) expande 16.114 estados. Con el
desempate de 03 §5 (menor `g` primero) son un 36 % más que con el anterior (mayor `g` primero: 11.879) y, en la misma
máquina y sesión, unos 100 ms frente a 80 ms; con `w = 2` (Morpho) el cambio es mínimo (128 frente a 116 expandidos, mismo
largo). Cada nodo guarda solo las `T` cuias del tablero. El bucle caliente no asigna (tests
`DotnetOnly/HotLoopAllocationTests`).

Reglas con contexto de tablero (C1-1, 2026-10-08, misma máquina, Release): con el solver delegando en `BoardRules`, el
tablero de 14 cuias sigue expandiendo 16.114 estados (par 37) en `Run` y 16.956 (par 48) en `Single`; el tiempo por
búsqueda calentada pasó de 29,1 ms a 20,9 ms en `Run` y de 28,5 ms a 26,8 ms en `Single` (la regla normal del destino ya no
recalcula el sellado de una cuia que no está llena). Los 140 niveles de paridad dan el mismo par.

Modo de vertido (D-005, 2026-10-08, sustituye a D-001): en `Run` el tutorial v0 da los pares 3, 6, 7, 8, 10, 10, 11, 13,
14 y 16 (en `Single`, solo de referencia: 3, 9, 10, 12, 16, 17, 15, 17, 16, 19; medidos el 2026-10-06 con los mismos
tableros). A* con `H_single` expande en `Single` como mucho 1.176 estados (nivel 10), las mismas cifras que decisiones.md
§2.1.3-§2.1.4. En el tablero de 14 cuias de referencia, `Run` da par 37 con 16.114 expandidos (`Single`: par 48, unos
17.000). Las firmas sin modo son heredadas (equivalen a `Run`, que desde D-005 es también `PourModes.Default`) hasta T6.

Pendiente de portar: mecánicas (02 §9) y Aguas turbias en la partida (máscara de revelado para Marea, R7), PRNG
determinista (riesgo R11 del plan de instalación: paridad con MT19937, decisión del dueño), bot casual ×40 (03 §6), cancelación por tiempo del solver para Morpho
(`hint_timeout_ms`) y enumeración de realidades (03 §8).
