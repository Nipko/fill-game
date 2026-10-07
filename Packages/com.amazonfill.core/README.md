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
| `AF.Core` | `Board.Cuia`: cuia empaquetada en un `ulong` (capacidad, dosis, máscara de ocultos) | 02 §1.1 R1, R2, R4, R6, R7; 08 §2.2 |
| `AF.Core` | `Board.BoardState`: tablero inmutable, `U_c`, sellado, victoria, igualdad por valor (`IEquatable`) y `CanonicalHash()` | 02 §1.1 R3, R5; §1.3 R12; 03 §3 |
| `AF.Core` | `Board.BoardCanon`: orden canónico (por `Packed`) y hash FNV-1a de 64 bits, sin asignaciones | 03 §3; 08 §2.2 |
| `AF.Core` | `Rules.CuiaRules`: **única** implementación de sellado, origen posible (`CanBeSource`), `legal`, `cantidad` y `futil` sobre cuias sueltas; la usan el tablero, el solver, el bot y la selección del cliente | 02 §1.2, §1.4, §3.1; 04 §5.1 |
| `AF.Core` | `Rules.Pour` y `Rules.PourRules`: vertido (parcial), `CanBeSource`, `IsFutile`, `CollectUsefulMoves`, `HasUsefulMove`, `IsStuck` sin mecánicas | 02 §1.2-1.4 R9-R11, R13, R14, R16; §3.1 C1-C2 |
| `AF.Core` | `Session.LevelSession`, `LevelStatus`, `PourResult`: partida con R10, Marea (deshacer) y Volver a la orilla (reinicio) | 02 §1.3-1.4, §2; 04 §2.1, §2.7 |
| `AF.Solver` | `Collections.MinHeap`: montículo con desempate FIFO (sustituto de `PriorityQueue`) | 08 §2.2 |
| `AF.Solver` | `Search.AStarSolver`, `SolverOptions`, `SolveResult`, `SolveStatus`: A* (w entero) sobre estados canónicos, presupuesto de expandidos y prueba de irresolubilidad; a igual `f`, primero el de menor `g` (tupla `(f, g, S)` de 03 §5 y del prototipo) | 03 §5 |
| `AF.Solver` | `Search.SolverHeuristic`: `H = max(cortes, dispersión)` | 03 §5 |

Rendimiento medido (2026-10-06, .NET 10 Release, Windows ARM64): los 140 niveles de `tools/out*/levels_*.json` dan el
mismo par que `levelgen_proto.py`; un tablero de 14 cuias (12 colores, 2 vacíos, par 37) expande 16.114 estados. Con el
desempate de 03 §5 (menor `g` primero) son un 36 % más que con el anterior (mayor `g` primero: 11.879) y, en la misma
máquina y sesión, unos 100 ms frente a 80 ms; con `w = 2` (Morpho) el cambio es mínimo (128 frente a 116 expandidos, mismo
largo). Cada nodo guarda solo las `T` cuias del tablero. El bucle caliente no asigna (tests
`DotnetOnly/HotLoopAllocationTests`).

Pendiente de portar: mecánicas (02 §9) y Aguas turbias en la partida (máscara de revelado para Marea, R7), PRNG
determinista (riesgo R11 del plan de instalación: paridad con MT19937, decisión del dueño), bot casual ×40 (03 §6), cancelación por tiempo del solver para Morpho
(`hint_timeout_ms`) y enumeración de realidades (03 §8).
