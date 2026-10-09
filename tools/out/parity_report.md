# Paridad de métricas con el prototipo (`level-generator --parity`)

Generado por `dotnet run --project tools/level-generator -- --parity`: no se edita a mano y no lleva tiempos (dos ejecuciones dan los mismos bytes). Fuentes: GDD 03 §7 (métricas), §13 (variante −1 color), §17-§18 y 09 R26 (paridad sobre los estados exportados: el PRNG del núcleo no es el MT19937 del prototipo, así que `bot_fail` y `D` no se comparan). Las métricas se calculan con `AF.Core` y `AF.Solver` (A* w = 1 a 300.000 en `run`; trampas con A* a 3.000 y 120 pares como máximo, muestreados por el índice entero ⌊k·n/120⌋ [P]; −1 color con A* a 100.000).

- Niveles: 40 (`tools/out/levels_proto.json` y `tools/out_inverse/levels_proto.json`).
- Par igual: **40/40** (umbral 40/40).
- Complejidad igual: **40/40** (umbral 40/40).
- Ramificación igual a 2 decimales: **40/40** (umbral 38/40), contando los `MOVIMIENTOS` literales de 03 §5 (la definición de 03 §7). Con `|SolverMoves|`, que funde además orígenes y destinos idénticos, coincidirían 26/40: por eso la métrica usa la definición literal [P].
- Trampas iguales a 2 decimales: **40/40** (umbral 38/40).
- Color y par de −1 color iguales: **39/39** (los niveles en los que el prototipo los publica).
- Resultado: **PASS**.

| Fuente | Nivel | K | E | Par proto / C# | Compl. | Ramif. proto / C# | Trampas proto / C# (pares) | −1 color proto / C# |
|---|---|---|---|---|---|---|---|---|
| `tools/out/levels_proto.json` | proto_n0001 | 5 | 2 | 11 / 11 | 28 / 28 | 4.45 / 4.45 | 0.000 / 0.000 (49) | 2 (9) / 2 (9) |
| `tools/out/levels_proto.json` | proto_n0002 | 6 | 2 | 15 / 15 | 38 / 38 | 5.20 / 5.20 | 0.000 / 0.000 (78) | 4 (14) / 4 (14) |
| `tools/out/levels_proto.json` | proto_n0003 | 6 | 1 | 12 / 12 | 34 / 34 | 2.50 / 2.50 | 0.100 / 0.100 (30) | 5 (11) / 5 (11) |
| `tools/out/levels_proto.json` | proto_n0004 | 7 | 2 | 19 / 19 | 48 / 48 | 3.32 / 3.32 | 0.016 / 0.016 (63, 1 sin decidir) | 6 (17) / 6 (17) |
| `tools/out/levels_proto.json` | proto_n0005 | 8 | 2 | 16 / 16 | 42 / 42 | 6.12 / 6.12 | 0.000 / 0.000 (98) | 5 (15) / 5 (15) |
| `tools/out/levels_proto.json` | proto_n0006 | 9 | 2 | 18 / 18 | 50 / 50 | 7.22 / 7.22 | 0.000 / 0.000 (120) | 6 (17) / 6 (17) |
| `tools/out/levels_proto.json` | proto_n0007 | 10 | 2 | 21 / 21 | 56 / 56 | 7.43 / 7.43 | 0.000 / 0.000 (120, 11 sin decidir) | 4 (19) / 4 (19) |
| `tools/out/levels_proto.json` | proto_n0008 | 11 | 1 | 22 / 22 | 58 / 58 | 2.86 / 2.86 | 0.190 / 0.190 (63) | 1 (20) / 1 (20) |
| `tools/out/levels_proto.json` | proto_n0009 | 7 | 1 | 18 / 18 | 48 / 48 | 2.17 / 2.17 | 0.128 / 0.128 (39) | 6 (17) / 6 (17) |
| `tools/out/levels_proto.json` | proto_n0010 | 8 | 1 | 19 / 19 | 52 / 52 | 2.11 / 2.11 | 0.150 / 0.150 (40) | 6 (18) / 6 (18) |
| `tools/out/levels_proto.json` | proto_n0011 | 9 | 1 | 19 / 19 | 54 / 54 | 2.47 / 2.47 | 0.128 / 0.128 (47) | 1 (18) / 1 (18) |
| `tools/out/levels_proto.json` | proto_n0012 | 10 | 1 | 23 / 23 | 62 / 62 | 3.17 / 3.17 | 0.260 / 0.260 (73) | 6 (22) / 6 (22) |
| `tools/out/levels_proto.json` | proto_n0013 | 11 | 1 | 27 / 27 | 72 / 72 | 2.70 / 2.70 | 0.397 / 0.397 (73) | 5 (26) / 5 (26) |
| `tools/out/levels_proto.json` | proto_n0014 | 10 | 1 | 22 / 22 | 60 / 60 | 3.32 / 3.32 | 0.315 / 0.315 (73) | 4 (21) / 4 (21) |
| `tools/out/levels_proto.json` | proto_n0015 | 6 | 2 | 19 / 19 | 44 / 44 | 3.05 / 3.05 | 0.000 / 0.000 (58) | 3 (15) / 3 (15) |
| `tools/out/levels_proto.json` | proto_n0016 | 7 | 1 | 19 / 19 | 48 / 48 | 2.53 / 2.53 | 0.271 / 0.271 (48) | 5 (17) / 5 (17) |
| `tools/out/levels_proto.json` | proto_n0017 | 8 | 2 | 26 / 26 | 60 / 60 | 2.85 / 2.85 | 0.027 / 0.027 (74, 10 sin decidir) | 6 (23) / 6 (23) |
| `tools/out/levels_proto.json` | proto_n0018 | 9 | 1 | 25 / 25 | 64 / 64 | 1.60 / 1.60 | 0.025 / 0.025 (40) | 4 (23) / 4 (23) |
| `tools/out/levels_proto.json` | proto_n0019 | 10 | 2 | 32 / 32 | 76 / 76 | 3.66 / 3.66 | 0.051 / 0.051 (117, 32 sin decidir) | - (-) / - (-) |
| `tools/out/levels_proto.json` | proto_n0020 | 10 | 2 | 32 / 32 | 78 / 78 | 2.88 / 2.88 | 0.054 / 0.054 (92, 15 sin decidir) | 4 (28) / 4 (28) |
| `tools/out_inverse/levels_proto.json` | proto_n0001 | 5 | 2 | 11 / 11 | 28 / 28 | 4.45 / 4.45 | 0.000 / 0.000 (49) | 2 (9) / 2 (9) |
| `tools/out_inverse/levels_proto.json` | proto_n0002 | 6 | 1 | 12 / 12 | 32 / 32 | 2.33 / 2.33 | 0.036 / 0.036 (28) | 2 (11) / 2 (11) |
| `tools/out_inverse/levels_proto.json` | proto_n0003 | 6 | 2 | 14 / 14 | 36 / 36 | 4.43 / 4.43 | 0.000 / 0.000 (62) | 3 (13) / 3 (13) |
| `tools/out_inverse/levels_proto.json` | proto_n0004 | 7 | 1 | 14 / 14 | 40 / 40 | 2.79 / 2.79 | 0.026 / 0.026 (39) | 1 (13) / 1 (13) |
| `tools/out_inverse/levels_proto.json` | proto_n0005 | 7 | 2 | 15 / 15 | 38 / 38 | 4.80 / 4.80 | 0.000 / 0.000 (72) | 2 (14) / 2 (14) |
| `tools/out_inverse/levels_proto.json` | proto_n0006 | 8 | 1 | 17 / 17 | 46 / 46 | 3.00 / 3.00 | 0.157 / 0.157 (51) | 1 (16) / 1 (16) |
| `tools/out_inverse/levels_proto.json` | proto_n0007 | 8 | 2 | 18 / 18 | 46 / 46 | 5.28 / 5.28 | 0.000 / 0.000 (95) | 1 (16) / 1 (16) |
| `tools/out_inverse/levels_proto.json` | proto_n0008 | 9 | 1 | 18 / 18 | 52 / 52 | 2.39 / 2.39 | 0.116 / 0.116 (43) | 1 (16) / 1 (16) |
| `tools/out_inverse/levels_proto.json` | proto_n0009 | 9 | 2 | 21 / 21 | 54 / 54 | 5.81 / 5.81 | 0.025 / 0.025 (120, 8 sin decidir) | 9 (20) / 9 (20) |
| `tools/out_inverse/levels_proto.json` | proto_n0010 | 10 | 1 | 19 / 19 | 54 / 54 | 3.11 / 3.11 | 0.305 / 0.305 (59) | 3 (18) / 3 (18) |
| `tools/out_inverse/levels_proto.json` | proto_n0011 | 10 | 2 | 24 / 24 | 62 / 62 | 4.50 / 4.50 | 0.028 / 0.028 (108, 37 sin decidir) | 2 (22) / 2 (22) |
| `tools/out_inverse/levels_proto.json` | proto_n0012 | 11 | 1 | 22 / 22 | 62 / 62 | 3.18 / 3.18 | 0.100 / 0.100 (70) | 1 (21) / 1 (21) |
| `tools/out_inverse/levels_proto.json` | proto_n0013 | 5 | 2 | 12 / 12 | 32 / 32 | 4.67 / 4.67 | 0.000 / 0.000 (56) | 2 (10) / 2 (10) |
| `tools/out_inverse/levels_proto.json` | proto_n0014 | 7 | 1 | 16 / 16 | 42 / 42 | 2.25 / 2.25 | 0.083 / 0.083 (36) | 1 (14) / 1 (14) |
| `tools/out_inverse/levels_proto.json` | proto_n0015 | 8 | 1 | 12 / 12 | 38 / 38 | 2.58 / 2.58 | 0.194 / 0.194 (31) | 1 (11) / 1 (11) |
| `tools/out_inverse/levels_proto.json` | proto_n0016 | 9 | 1 | 18 / 18 | 50 / 50 | 3.06 / 3.06 | 0.327 / 0.327 (55) | 2 (17) / 2 (17) |
| `tools/out_inverse/levels_proto.json` | proto_n0017 | 10 | 1 | 21 / 21 | 56 / 56 | 2.76 / 2.76 | 0.293 / 0.293 (58) | 3 (19) / 3 (19) |
| `tools/out_inverse/levels_proto.json` | proto_n0018 | 10 | 2 | 23 / 23 | 60 / 60 | 7.87 / 7.87 | 0.017 / 0.017 (120, 27 sin decidir) | 5 (22) / 5 (22) |
| `tools/out_inverse/levels_proto.json` | proto_n0019 | 11 | 1 | 22 / 22 | 62 / 62 | 3.50 / 3.50 | 0.312 / 0.312 (77) | 7 (21) / 7 (21) |
| `tools/out_inverse/levels_proto.json` | proto_n0020 | 8 | 2 | 18 / 18 | 46 / 46 | 6.11 / 6.11 | 0.000 / 0.000 (110) | 2 (17) / 2 (17) |

## Decisiones para la paridad

1. **Ruta óptima de referencia.** Con varias soluciones de la misma longitud, la ramificación y las trampas dependen de la ruta medida. `AF.Solver` expande en el orden canónico de las cuias y el prototipo en el orden real del estado guardado. `Metrics/ReferencePath.cs` repite el `ASTAR` de 03 §5 en el orden del prototipo (encola `(f, g, llegada)`) con las reglas y la heurística de `AF.Core`/`AF.Solver`; se usa solo si su ruta tiene la longitud del par de `AF.Solver` [P].
2. **Ramificación = `|MOVIMIENTOS(S)|` literal de 03 §5** (legales y no fútiles, un vacío por capacidad y mecánica y por origen), que es la definición de 03 §7. `AF.Solver.SolverMoves` funde además orígenes y destinos idénticos y daría otra cifra en los tableros con dos cuias iguales [P].
3. **Trampas:** los pares son esos `MOVIMIENTOS` en el orden real de cada estado de la ruta; con más de 120 se toma el índice entero ⌊k·n/120⌋ [P] en lugar de `int(k·n/120.0)` del prototipo.
4. **Redondeo:** las cifras del prototipo están redondeadas por Python (al par en el punto medio); la comparación redondea las de C# igual (`MidpointRounding.ToEven`) y la tabla imprime la cifra de C# ya redondeada así, la misma que se compara.

## Diferencias

Ninguna: ramificación y trampas coinciden a 2 decimales en todos los niveles.
