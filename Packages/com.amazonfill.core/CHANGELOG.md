# Changelog

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/); versiones con SemVer.

## [Sin publicar]

### Añadido

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
