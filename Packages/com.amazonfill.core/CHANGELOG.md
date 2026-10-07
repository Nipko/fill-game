# Changelog

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/); versiones con SemVer.

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
