# Tutorial 1-10: informe de `level-generator --handmade` (Tintalú v0)

Generado por `dotnet run --project tools/level-generator -- --handmade tools/level-generator/specs/tutorial_v0.json --out Assets/_Project/Data/Levels --report tools/out/tutorial_v0_report.md`: no se edita a mano. Todas las reglas (vertido legal, cantidad, fútiles, sellado, victoria y Aguas estancadas) se evalúan con `AF.Core.PourRules` y el par se contrasta con `AF.Solver.AStarSolver`, las mismas que ejecuta el cliente (04 §5.1; `.claude/rules/core.md`). Fuentes: 02 §1 y §13, glosario §9 y §11, 03 §3-§5, 08 §4.1 y `docs/dev/prototipo-v0.md` (contrato b).

- Diseño a mano: `tools/level-generator/specs/tutorial_v0.json`.
- **Modo de vertido: `single`** (el modo por defecto, `PourModes.Default`; decisión del dueño D-001 del 2026-10-06, `docs/dev/decisiones.md`): cada vertido mueve una sola dosis y no existe el vertido parcial. El par del pack, la solución óptima y el camino a Aguas estancadas son de este modo; el par en `run` (la racha que cabe, 02 R9) sale solo como referencia. El pack **no declara el modo**: el subconjunto v0 de `af-level/1` no tiene ese campo y añadirlo es un cambio de contrato (`pack_schema` 2, pregunta P7), así que asume el modo por defecto del cliente (`rules_pour_mode` = `"single"`). Jugado en `run` (`--pour-mode run` o `rules_pour_mode` = `"run"`), su par no vale (riesgo R-2): hay que regenerarlo con `--pour-mode run`.
- Pack v0: `Assets/_Project/Data/Levels/tutorial_v0.json` (subconjunto de `af-level/1` sin `pack_schema`), SHA-256 `8e25f19b5e57dacbf605e7d92c3991b10a151a13e064638ee1eb2f1ddf8d5a25`.
- Resultado: **141/141 comprobaciones PASS**.
- Notación: cuias con índices 0-based en orden visual; dosis de abajo arriba con el índice de motor del glosario §9 (8 Açaí, 4 Aguaje, 3 Taperebá, 1 Guanábana, 7 Bacaba); un vertido es `origen→destino ×dosis` (siempre ×1 en `single`).
- Bot casual ×40 (03 §6): no se calcula. El bot aún no existe en `AF.Solver` (su PRNG espera la decisión del dueño sobre la paridad con MT19937, riesgo R11 del plan) y el contrato b no lo exige para el tutorial.

## Resumen

| Nivel | id | Cuias = colores + vacíos | Colores | Par `single` | Par `run` | Rango [P] de 02 §13 (`run`) | Estados `single` | Sin salida | Aguas estancadas | Enseñanza comprobada |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `m01_v1_n0001` | 3 = 2 + 1 | Açaí, Aguaje | **3** | 3 | 3-3 | 8 | 0 | — | estado literal de 02 §13 |
| 2 | `m01_v1_n0002` | 5 = 3 + 2 | Açaí, Aguaje, Taperebá | **9** | 6 | 4-6 | 114 | 0 | — | una dosis por vertido: 3 × 2→4 (vertidos 6-8) |
| 3 | `m01_v1_n0003` | 5 = 3 + 2 | Açaí, Aguaje, Taperebá | **10** | 7 | 5-7 | 170 | 0 | — | 2/6 vertidos iniciales no acercan |
| 4 | `m01_v1_n0004` | 6 = 4 + 2 | Açaí, Aguaje, Taperebá, Guanábana | **12** | 8 | 7-9 | 375 | 0 | — | debut de Guanábana |
| 5 | `m01_v1_n0005` | 6 = 4 + 2 | Açaí, Aguaje, Taperebá, Guanábana | **16** | 10 | 9-11 | 805 | 0 | — | Aguaje en 3 cuias |
| 6 | `m01_v1_n0006` | 5 = 4 + 1 | Açaí, Aguaje, Taperebá, Guanábana | **17** | 10 | 10-13 | 72 | 1 | 1 vertido(s) | 1 vacío; estancadas en 1 |
| 7 | `m01_v1_n0007` | 7 = 5 + 2 | Açaí, Aguaje, Taperebá, Guanábana, Bacaba | **15** | 11 | 10-13 | 922 | 0 | — | debut de Bacaba |
| 8 | `m01_v1_n0008` | 7 = 5 + 2 | Açaí, Aguaje, Taperebá, Guanábana, Bacaba | **17** | 13 | 12-15 | 1478 | 3 | 2 vertido(s) | estancadas en 2 con 2 vacíos |
| 9 | `m01_v1_n0009` | 7 = 5 + 2 | Açaí, Aguaje, Taperebá, Guanábana, Bacaba | **16** | 14 | 13-16 | 1961 | 4 | — | sin enseñanza de tablero |
| 10 | `m01_v1_n0010` | 7 = 5 + 2 | Açaí, Aguaje, Taperebá, Guanábana, Bacaba | **19** | 16 | 14-18 | 3634 | 0 | — | sin enseñanza de tablero |

En negrita, el par que lleva el pack (`single`). Estados = estados canónicos alcanzables desde el inicio en `single` (03 §3); sin salida = de ellos, los que ya no pueden ganar; Aguas estancadas = vertidos del camino más corto a un estado sin vertidos útiles (02 R13), en `single`.

Informativo, no falla: los rangos [P] de la columna "Par" de 02 §13 se fijaron con la racha (`run`) y la tabla los conserva hasta que el dueño responda la pregunta P2 de `docs/dev/decisiones.md` (adoptar como rangos los pares medidos en `single` o rediseñar los niveles 2-10 para una rampa monótona). Por eso la comprobación de rango usa el par en `run`. Par en `single` fuera de su rango: 2 (9 frente a 4-6), 3 (10 frente a 5-7), 4 (12 frente a 7-9), 5 (16 frente a 9-11), 6 (17 frente a 10-13), 7 (15 frente a 10-13), 8 (17 frente a 12-15), 10 (19 frente a 14-18). Rampa del par en `single`: no monótona, 6 (17) > 7 (15) y 8 (17) > 9 (16).

## Comprobaciones

| Nivel | Comprobación | Resultado | Detalle |
|---|---|---|---|
| 1 | 02 §13: cuias = colores + vacíos | PASS | 3 = 2 + 1 (tabla: 3 = 2 + 1); 2 cuias llenas y 1 vacías |
| 1 | 02 §13: tintes de la fila con su ID del glosario §9 | PASS | Açaí (acai = 8), Aguaje (aguaje = 4) |
| 1 | debut de cada tinte ≤ nivel (glosario §9) | PASS | ok |
| 1 | U_c = 4 por tinte y cap 4 (02 R1, R3) | PASS | aguaje=4 acai=4 |
| 1 | 0 pares de riesgo (max_pares_riesgo = 0 en 1-40, glosario §9) | PASS | 0 |
| 1 | nivel 1: estado inicial literal de 02 §13 | PASS | [8,8,8,4] [4,4,4,8] [] |
| 1 | BFS exhaustivo: ≤ 8 cuias (prototipo-v0 b) | PASS | 3 cuias |
| 1 | no está resuelto al empezar (02 R12) | PASS | 0 cuias selladas |
| 1 | no está en Aguas estancadas al empezar (02 R13; 02 §9.2 regla 1) | PASS | 2 vertidos útiles |
| 1 | resoluble en `single` (BFS exhaustivo con AF.Core.PourRules) | PASS | 8 estados alcanzables |
| 1 | par en `run` dentro del rango [P] de 02 §13 (rangos fijados en `run`; P2) | PASS | par `run` 3; rango 3-3; par `single` 3, dentro del rango (informativo hasta P2) |
| 1 | par = A* de AF.Solver en `single` (03 §5, w = 1) | PASS | A* Solved, par 3, 5 expandidos |
| 1 | la solución del informe gana con par vertidos legales en `single` | PASS | 3 vertidos, todos de 1 dosis (D-001) |
| 1 | ningún color en 3 cuias antes del 5 (02 §13: el 5 trae el primer color problema) | PASS | máximo 2 cuias por color |
| 2 | 02 §13: cuias = colores + vacíos | PASS | 5 = 3 + 2 (tabla: 5 = 3 + 2); 3 cuias llenas y 2 vacías |
| 2 | 02 §13: tintes de la fila con su ID del glosario §9 | PASS | Açaí (acai = 8), Aguaje (aguaje = 4), Taperebá (tapereba = 3) |
| 2 | debut de cada tinte ≤ nivel (glosario §9) | PASS | ok |
| 2 | U_c = 4 por tinte y cap 4 (02 R1, R3) | PASS | tapereba=4 aguaje=4 acai=4 |
| 2 | 0 pares de riesgo (max_pares_riesgo = 0 en 1-40, glosario §9) | PASS | 0 |
| 2 | BFS exhaustivo: ≤ 8 cuias (prototipo-v0 b) | PASS | 5 cuias |
| 2 | no está resuelto al empezar (02 R12) | PASS | 0 cuias selladas |
| 2 | no está en Aguas estancadas al empezar (02 R13; 02 §9.2 regla 1) | PASS | 6 vertidos útiles |
| 2 | resoluble en `single` (BFS exhaustivo con AF.Core.PourRules) | PASS | 114 estados alcanzables |
| 2 | par en `run` dentro del rango [P] de 02 §13 (rangos fijados en `run`; P2) | PASS | par `run` 6; rango 4-6; par `single` 9, fuera del rango (informativo hasta P2) |
| 2 | par = A* de AF.Solver en `single` (03 §5, w = 1) | PASS | A* Solved, par 9, 74 expandidos |
| 2 | la solución del informe gana con par vertidos legales en `single` | PASS | 9 vertidos, todos de 1 dosis (D-001) |
| 2 | ningún color en 3 cuias antes del 5 (02 §13: el 5 trae el primer color problema) | PASS | máximo 2 cuias por color |
| 2 | nivel 2: una dosis por vertido: toda solución óptima repite 3 veces un mismo vertido A→B y la del informe los hace seguidos (02 §13 [P]; D-001) | PASS | las 1008 soluciones óptimas repiten 3 veces un vertido; la del informe, en los vertidos 6-8: 3 × 2→4 ×1 (racha de 3 en la cuia 2 al empezar) |
| 3 | 02 §13: cuias = colores + vacíos | PASS | 5 = 3 + 2 (tabla: 5 = 3 + 2); 3 cuias llenas y 2 vacías |
| 3 | 02 §13: tintes de la fila con su ID del glosario §9 | PASS | Açaí (acai = 8), Aguaje (aguaje = 4), Taperebá (tapereba = 3) |
| 3 | debut de cada tinte ≤ nivel (glosario §9) | PASS | ok |
| 3 | U_c = 4 por tinte y cap 4 (02 R1, R3) | PASS | tapereba=4 aguaje=4 acai=4 |
| 3 | 0 pares de riesgo (max_pares_riesgo = 0 en 1-40, glosario §9) | PASS | 0 |
| 3 | BFS exhaustivo: ≤ 8 cuias (prototipo-v0 b) | PASS | 5 cuias |
| 3 | no está resuelto al empezar (02 R12) | PASS | 0 cuias selladas |
| 3 | no está en Aguas estancadas al empezar (02 R13; 02 §9.2 regla 1) | PASS | 6 vertidos útiles |
| 3 | resoluble en `single` (BFS exhaustivo con AF.Core.PourRules) | PASS | 170 estados alcanzables |
| 3 | par en `run` dentro del rango [P] de 02 §13 (rangos fijados en `run`; P2) | PASS | par `run` 7; rango 5-7; par `single` 10, fuera del rango (informativo hasta P2) |
| 3 | par = A* de AF.Solver en `single` (03 §5, w = 1) | PASS | A* Solved, par 10, 101 expandidos |
| 3 | la solución del informe gana con par vertidos legales en `single` | PASS | 10 vertidos, todos de 1 dosis (D-001) |
| 3 | ningún color en 3 cuias antes del 5 (02 §13: el 5 trae el primer color problema) | PASS | máximo 2 cuias por color |
| 3 | nivel 3: hay vertidos iniciales que alejan de la solución (tutorial de Marea, 02 §13) | PASS | 2 de 6 vertidos útiles iniciales no acercan (Δ ≥ 0) |
| 4 | 02 §13: cuias = colores + vacíos | PASS | 6 = 4 + 2 (tabla: 6 = 4 + 2); 4 cuias llenas y 2 vacías |
| 4 | 02 §13: tintes de la fila con su ID del glosario §9 | PASS | Açaí (acai = 8), Aguaje (aguaje = 4), Taperebá (tapereba = 3), Guanábana (guanabana = 1) |
| 4 | debut de cada tinte ≤ nivel (glosario §9) | PASS | ok |
| 4 | U_c = 4 por tinte y cap 4 (02 R1, R3) | PASS | guanabana=4 tapereba=4 aguaje=4 acai=4 |
| 4 | 0 pares de riesgo (max_pares_riesgo = 0 en 1-40, glosario §9) | PASS | 0 |
| 4 | BFS exhaustivo: ≤ 8 cuias (prototipo-v0 b) | PASS | 6 cuias |
| 4 | no está resuelto al empezar (02 R12) | PASS | 0 cuias selladas |
| 4 | no está en Aguas estancadas al empezar (02 R13; 02 §9.2 regla 1) | PASS | 8 vertidos útiles |
| 4 | resoluble en `single` (BFS exhaustivo con AF.Core.PourRules) | PASS | 375 estados alcanzables |
| 4 | par en `run` dentro del rango [P] de 02 §13 (rangos fijados en `run`; P2) | PASS | par `run` 8; rango 7-9; par `single` 12, fuera del rango (informativo hasta P2) |
| 4 | par = A* de AF.Solver en `single` (03 §5, w = 1) | PASS | A* Solved, par 12, 222 expandidos |
| 4 | la solución del informe gana con par vertidos legales en `single` | PASS | 12 vertidos, todos de 1 dosis (D-001) |
| 4 | ningún color en 3 cuias antes del 5 (02 §13: el 5 trae el primer color problema) | PASS | máximo 2 cuias por color |
| 5 | 02 §13: cuias = colores + vacíos | PASS | 6 = 4 + 2 (tabla: 6 = 4 + 2); 4 cuias llenas y 2 vacías |
| 5 | 02 §13: tintes de la fila con su ID del glosario §9 | PASS | Açaí (acai = 8), Aguaje (aguaje = 4), Taperebá (tapereba = 3), Guanábana (guanabana = 1) |
| 5 | debut de cada tinte ≤ nivel (glosario §9) | PASS | ok |
| 5 | U_c = 4 por tinte y cap 4 (02 R1, R3) | PASS | guanabana=4 tapereba=4 aguaje=4 acai=4 |
| 5 | 0 pares de riesgo (max_pares_riesgo = 0 en 1-40, glosario §9) | PASS | 0 |
| 5 | BFS exhaustivo: ≤ 8 cuias (prototipo-v0 b) | PASS | 6 cuias |
| 5 | no está resuelto al empezar (02 R12) | PASS | 0 cuias selladas |
| 5 | no está en Aguas estancadas al empezar (02 R13; 02 §9.2 regla 1) | PASS | 8 vertidos útiles |
| 5 | resoluble en `single` (BFS exhaustivo con AF.Core.PourRules) | PASS | 805 estados alcanzables |
| 5 | par en `run` dentro del rango [P] de 02 §13 (rangos fijados en `run`; P2) | PASS | par `run` 10; rango 9-11; par `single` 16, fuera del rango (informativo hasta P2) |
| 5 | par = A* de AF.Solver en `single` (03 §5, w = 1) | PASS | A* Solved, par 16, 692 expandidos |
| 5 | la solución del informe gana con par vertidos legales en `single` | PASS | 16 vertidos, todos de 1 dosis (D-001) |
| 5 | nivel 5: un color repartido en 3 cuias (02 §13) | PASS | aguaje |
| 6 | 02 §13: cuias = colores + vacíos | PASS | 5 = 4 + 1 (tabla: 5 = 4 + 1); 4 cuias llenas y 1 vacías |
| 6 | 02 §13: tintes de la fila con su ID del glosario §9 | PASS | Açaí (acai = 8), Aguaje (aguaje = 4), Taperebá (tapereba = 3), Guanábana (guanabana = 1) |
| 6 | debut de cada tinte ≤ nivel (glosario §9) | PASS | ok |
| 6 | U_c = 4 por tinte y cap 4 (02 R1, R3) | PASS | guanabana=4 tapereba=4 aguaje=4 acai=4 |
| 6 | 0 pares de riesgo (max_pares_riesgo = 0 en 1-40, glosario §9) | PASS | 0 |
| 6 | BFS exhaustivo: ≤ 8 cuias (prototipo-v0 b) | PASS | 5 cuias |
| 6 | no está resuelto al empezar (02 R12) | PASS | 0 cuias selladas |
| 6 | no está en Aguas estancadas al empezar (02 R13; 02 §9.2 regla 1) | PASS | 4 vertidos útiles |
| 6 | resoluble en `single` (BFS exhaustivo con AF.Core.PourRules) | PASS | 72 estados alcanzables |
| 6 | par en `run` dentro del rango [P] de 02 §13 (rangos fijados en `run`; P2) | PASS | par `run` 10; rango 10-13; par `single` 17, fuera del rango (informativo hasta P2) |
| 6 | par = A* de AF.Solver en `single` (03 §5, w = 1) | PASS | A* Solved, par 17, 40 expandidos |
| 6 | la solución del informe gana con par vertidos legales en `single` | PASS | 17 vertidos, todos de 1 dosis (D-001) |
| 6 | nivel 6: un solo vacío (excepción tutorial, 02 §13) | PASS | 1 vacío(s) |
| 6 | nivel 6: Aguas estancadas alcanzable (02 §13: primer modal) | PASS | en 1 vertido(s) |
| 7 | 02 §13: cuias = colores + vacíos | PASS | 7 = 5 + 2 (tabla: 7 = 5 + 2); 5 cuias llenas y 2 vacías |
| 7 | 02 §13: tintes de la fila con su ID del glosario §9 | PASS | Açaí (acai = 8), Aguaje (aguaje = 4), Taperebá (tapereba = 3), Guanábana (guanabana = 1), Bacaba (bacaba = 7) |
| 7 | debut de cada tinte ≤ nivel (glosario §9) | PASS | ok |
| 7 | U_c = 4 por tinte y cap 4 (02 R1, R3) | PASS | guanabana=4 tapereba=4 aguaje=4 bacaba=4 acai=4 |
| 7 | 0 pares de riesgo (max_pares_riesgo = 0 en 1-40, glosario §9) | PASS | 0 |
| 7 | BFS exhaustivo: ≤ 8 cuias (prototipo-v0 b) | PASS | 7 cuias |
| 7 | no está resuelto al empezar (02 R12) | PASS | 0 cuias selladas |
| 7 | no está en Aguas estancadas al empezar (02 R13; 02 §9.2 regla 1) | PASS | 10 vertidos útiles |
| 7 | resoluble en `single` (BFS exhaustivo con AF.Core.PourRules) | PASS | 922 estados alcanzables |
| 7 | par en `run` dentro del rango [P] de 02 §13 (rangos fijados en `run`; P2) | PASS | par `run` 11; rango 10-13; par `single` 15, fuera del rango (informativo hasta P2) |
| 7 | par = A* de AF.Solver en `single` (03 §5, w = 1) | PASS | A* Solved, par 15, 477 expandidos |
| 7 | la solución del informe gana con par vertidos legales en `single` | PASS | 15 vertidos, todos de 1 dosis (D-001) |
| 8 | 02 §13: cuias = colores + vacíos | PASS | 7 = 5 + 2 (tabla: 7 = 5 + 2); 5 cuias llenas y 2 vacías |
| 8 | 02 §13: tintes de la fila con su ID del glosario §9 | PASS | Açaí (acai = 8), Aguaje (aguaje = 4), Taperebá (tapereba = 3), Guanábana (guanabana = 1), Bacaba (bacaba = 7) |
| 8 | debut de cada tinte ≤ nivel (glosario §9) | PASS | ok |
| 8 | U_c = 4 por tinte y cap 4 (02 R1, R3) | PASS | guanabana=4 tapereba=4 aguaje=4 bacaba=4 acai=4 |
| 8 | 0 pares de riesgo (max_pares_riesgo = 0 en 1-40, glosario §9) | PASS | 0 |
| 8 | BFS exhaustivo: ≤ 8 cuias (prototipo-v0 b) | PASS | 7 cuias |
| 8 | no está resuelto al empezar (02 R12) | PASS | 0 cuias selladas |
| 8 | no está en Aguas estancadas al empezar (02 R13; 02 §9.2 regla 1) | PASS | 10 vertidos útiles |
| 8 | resoluble en `single` (BFS exhaustivo con AF.Core.PourRules) | PASS | 1478 estados alcanzables |
| 8 | par en `run` dentro del rango [P] de 02 §13 (rangos fijados en `run`; P2) | PASS | par `run` 13; rango 12-15; par `single` 17, fuera del rango (informativo hasta P2) |
| 8 | par = A* de AF.Solver en `single` (03 §5, w = 1) | PASS | A* Solved, par 17, 947 expandidos |
| 8 | la solución del informe gana con par vertidos legales en `single` | PASS | 17 vertidos, todos de 1 dosis (D-001) |
| 8 | nivel 8: Aguas estancadas alcanzable (02 §13: reinicio probable con 2 vacíos) | PASS | en 2 vertido(s) |
| 9 | 02 §13: cuias = colores + vacíos | PASS | 7 = 5 + 2 (tabla: 7 = 5 + 2); 5 cuias llenas y 2 vacías |
| 9 | 02 §13: tintes de la fila con su ID del glosario §9 | PASS | Açaí (acai = 8), Aguaje (aguaje = 4), Taperebá (tapereba = 3), Guanábana (guanabana = 1), Bacaba (bacaba = 7) |
| 9 | debut de cada tinte ≤ nivel (glosario §9) | PASS | ok |
| 9 | U_c = 4 por tinte y cap 4 (02 R1, R3) | PASS | guanabana=4 tapereba=4 aguaje=4 bacaba=4 acai=4 |
| 9 | 0 pares de riesgo (max_pares_riesgo = 0 en 1-40, glosario §9) | PASS | 0 |
| 9 | BFS exhaustivo: ≤ 8 cuias (prototipo-v0 b) | PASS | 7 cuias |
| 9 | no está resuelto al empezar (02 R12) | PASS | 0 cuias selladas |
| 9 | no está en Aguas estancadas al empezar (02 R13; 02 §9.2 regla 1) | PASS | 10 vertidos útiles |
| 9 | resoluble en `single` (BFS exhaustivo con AF.Core.PourRules) | PASS | 1961 estados alcanzables |
| 9 | par en `run` dentro del rango [P] de 02 §13 (rangos fijados en `run`; P2) | PASS | par `run` 14; rango 13-16; par `single` 16, dentro del rango (informativo hasta P2) |
| 9 | par = A* de AF.Solver en `single` (03 §5, w = 1) | PASS | A* Solved, par 16, 462 expandidos |
| 9 | la solución del informe gana con par vertidos legales en `single` | PASS | 16 vertidos, todos de 1 dosis (D-001) |
| 10 | 02 §13: cuias = colores + vacíos | PASS | 7 = 5 + 2 (tabla: 7 = 5 + 2); 5 cuias llenas y 2 vacías |
| 10 | 02 §13: tintes de la fila con su ID del glosario §9 | PASS | Açaí (acai = 8), Aguaje (aguaje = 4), Taperebá (tapereba = 3), Guanábana (guanabana = 1), Bacaba (bacaba = 7) |
| 10 | debut de cada tinte ≤ nivel (glosario §9) | PASS | ok |
| 10 | U_c = 4 por tinte y cap 4 (02 R1, R3) | PASS | guanabana=4 tapereba=4 aguaje=4 bacaba=4 acai=4 |
| 10 | 0 pares de riesgo (max_pares_riesgo = 0 en 1-40, glosario §9) | PASS | 0 |
| 10 | BFS exhaustivo: ≤ 8 cuias (prototipo-v0 b) | PASS | 7 cuias |
| 10 | no está resuelto al empezar (02 R12) | PASS | 0 cuias selladas |
| 10 | no está en Aguas estancadas al empezar (02 R13; 02 §9.2 regla 1) | PASS | 10 vertidos útiles |
| 10 | resoluble en `single` (BFS exhaustivo con AF.Core.PourRules) | PASS | 3634 estados alcanzables |
| 10 | par en `run` dentro del rango [P] de 02 §13 (rangos fijados en `run`; P2) | PASS | par `run` 16; rango 14-18; par `single` 19, fuera del rango (informativo hasta P2) |
| 10 | par = A* de AF.Solver en `single` (03 §5, w = 1) | PASS | A* Solved, par 19, 1176 expandidos |
| 10 | la solución del informe gana con par vertidos legales en `single` | PASS | 19 vertidos, todos de 1 dosis (D-001) |
| pack | nivel 1: campos del subconjunto de af-level/1 y comprobaciones de 08 §4.1 | PASS | ok (sin solution, safe_path ni qa en el pack) |
| pack | nivel 2: campos del subconjunto de af-level/1 y comprobaciones de 08 §4.1 | PASS | ok (sin solution, safe_path ni qa en el pack) |
| pack | nivel 3: campos del subconjunto de af-level/1 y comprobaciones de 08 §4.1 | PASS | ok (sin solution, safe_path ni qa en el pack) |
| pack | nivel 4: campos del subconjunto de af-level/1 y comprobaciones de 08 §4.1 | PASS | ok (sin solution, safe_path ni qa en el pack) |
| pack | nivel 5: campos del subconjunto de af-level/1 y comprobaciones de 08 §4.1 | PASS | ok (sin solution, safe_path ni qa en el pack) |
| pack | nivel 6: campos del subconjunto de af-level/1 y comprobaciones de 08 §4.1 | PASS | ok (sin solution, safe_path ni qa en el pack) |
| pack | nivel 7: campos del subconjunto de af-level/1 y comprobaciones de 08 §4.1 | PASS | ok (sin solution, safe_path ni qa en el pack) |
| pack | nivel 8: campos del subconjunto de af-level/1 y comprobaciones de 08 §4.1 | PASS | ok (sin solution, safe_path ni qa en el pack) |
| pack | nivel 9: campos del subconjunto de af-level/1 y comprobaciones de 08 §4.1 | PASS | ok (sin solution, safe_path ni qa en el pack) |
| pack | nivel 10: campos del subconjunto de af-level/1 y comprobaciones de 08 §4.1 | PASS | ok (sin solution, safe_path ni qa en el pack) |

Informativo, no falla: 08 §4.1 exige `par ≥ 2·K` en `tools/level-qa`; en este tutorial (par en `single`) no lo cumple el nivel 1 (par 3 < 4) (pregunta 2 de `docs/dev/prototipo-v0.md`: ¿queda exento el tutorial?).

Informativo, nivel 3 (en `single`): el mayor cambio de la distancia del solver con un vertido útil, en cualquier estado resoluble, es +1 (0 = el vertido no acerca y cuesta 1 de más); vertidos útiles hacia un estado sin salida: 0. Si no llega a +2, el disparador literal del tutorial de Marea de 02 §13 ("distancia del solver +2") no puede darse en el 3 y queda su respaldo (inicio del nivel 5); la comprobación del nivel 3 usa la lectura débil: vertidos que no acercan.

## Detalle por nivel

### Nivel 1 · `m01_v1_n0001` · par 3 (`single`; en `run`: 3)

Estado inicial literal de 02 §13: la cuia casi llena se termina antes de apartar.

- cuia 0: Açaí, Açaí, Açaí, Aguaje `[8, 8, 8, 4]`
- cuia 1: Aguaje, Aguaje, Aguaje, Açaí `[4, 4, 4, 8]`
- cuia 2: vacía `[]`

Solución óptima en `single` (3 vertidos): `0→2 ×1 · 1→0 ×1 · 2→1 ×1`

Camino más corto a Aguas estancadas en `single`: no existe (ningún estado alcanzable se queda sin vertidos útiles).

### Nivel 2 · `m01_v1_n0002` · par 9 (`single`; en `run`: 6)

Lección «una dosis por vertido» (D-001; sustituye al vertido parcial, decisiones.md §2.1.5): la cuia 2 muestra una racha de 3 açaí que solo pasa a otra cuia de una en una. Toda solución óptima repite 3 veces un mismo vertido, y la del informe pasa las 3 açaí con 3 vertidos seguidos 2→4 sobre la açaí que la cuia 0 dejó en un vacío. El otro vacío queda en reserva. En el modo run el mismo tablero ofrecía el vertido parcial de una racha de 3 sobre un hueco de 2 (la lección anterior, que el generador sigue comprobando con --pour-mode run).

- cuia 0: Aguaje, Açaí, Aguaje, Aguaje `[4, 8, 4, 4]`
- cuia 1: Taperebá, Taperebá, Taperebá, Aguaje `[3, 3, 3, 4]`
- cuia 2: Taperebá, Açaí, Açaí, Açaí `[3, 8, 8, 8]`
- cuia 3: vacía `[]`
- cuia 4: vacía `[]`

Solución óptima en `single` (9 vertidos): `0→3 ×1 · 0→3 ×1 · 0→4 ×1 · 0→3 ×1 · 1→3 ×1 · 2→4 ×1 · 2→4 ×1 · 2→4 ×1 · 2→1 ×1`

Camino más corto a Aguas estancadas en `single`: no existe (ningún estado alcanzable se queda sin vertidos útiles).

### Nivel 3 · `m01_v1_n0003` · par 10 (`single`; en `run`: 7)

Dos de los seis vertidos iniciales no acercan a la solución (cuatro en el modo run): da ocasión al tutorial de Marea.

- cuia 0: Taperebá, Taperebá, Aguaje, Aguaje `[3, 3, 4, 4]`
- cuia 1: Açaí, Taperebá, Açaí, Taperebá `[8, 3, 8, 3]`
- cuia 2: Açaí, Aguaje, Aguaje, Açaí `[8, 4, 4, 8]`
- cuia 3: vacía `[]`
- cuia 4: vacía `[]`

Solución óptima en `single` (10 vertidos): `0→3 ×1 · 0→3 ×1 · 1→0 ×1 · 1→4 ×1 · 1→0 ×1 · 1→4 ×1 · 2→4 ×1 · 2→3 ×1 · 2→3 ×1 · 2→4 ×1`

Camino más corto a Aguas estancadas en `single`: no existe (ningún estado alcanzable se queda sin vertidos útiles).

### Nivel 4 · `m01_v1_n0004` · par 12 (`single`; en `run`: 8)

Debut de Guanábana, en la cima de la cuia 0. Cada tinte ocupa dos cuias, en cadena (açaí-guanábana, aguaje-açaí, taperebá-aguaje, guanábana-taperebá).

- cuia 0: Açaí, Guanábana, Açaí, Guanábana `[8, 1, 8, 1]`
- cuia 1: Aguaje, Açaí, Açaí, Aguaje `[4, 8, 8, 4]`
- cuia 2: Taperebá, Taperebá, Aguaje, Aguaje `[3, 3, 4, 4]`
- cuia 3: Guanábana, Guanábana, Taperebá, Taperebá `[1, 1, 3, 3]`
- cuia 4: vacía `[]`
- cuia 5: vacía `[]`

Solución óptima en `single` (12 vertidos): `1→4 ×1 · 1→5 ×1 · 1→5 ×1 · 1→4 ×1 · 2→4 ×1 · 2→4 ×1 · 3→2 ×1 · 3→2 ×1 · 0→3 ×1 · 0→5 ×1 · 0→3 ×1 · 0→5 ×1`

Camino más corto a Aguas estancadas en `single`: no existe (ningún estado alcanzable se queda sin vertidos útiles).

### Nivel 5 · `m01_v1_n0005` · par 16 (`single`; en `run`: 10)

Color problema: Aguaje repartido en 3 cuias (enterrada en la 0 y la 1, arriba y abajo en la 2); los demás tintes, en dos.

- cuia 0: Açaí, Aguaje, Açaí, Açaí `[8, 4, 8, 8]`
- cuia 1: Guanábana, Aguaje, Guanábana, Guanábana `[1, 4, 1, 1]`
- cuia 2: Aguaje, Taperebá, Taperebá, Aguaje `[4, 3, 3, 4]`
- cuia 3: Taperebá, Guanábana, Taperebá, Açaí `[3, 1, 3, 8]`
- cuia 4: vacía `[]`
- cuia 5: vacía `[]`

Solución óptima en `single` (16 vertidos): `0→4 ×1 · 0→4 ×1 · 0→5 ×1 · 0→4 ×1 · 1→0 ×1 · 1→0 ×1 · 1→5 ×1 · 1→0 ×1 · 2→5 ×1 · 2→1 ×1 · 2→1 ×1 · 2→5 ×1 · 3→4 ×1 · 3→1 ×1 · 3→0 ×1 · 3→1 ×1`

Camino más corto a Aguas estancadas en `single`: no existe (ningún estado alcanzable se queda sin vertidos útiles).

### Nivel 6 · `m01_v1_n0006` · par 17 (`single`; en `run`: 10)

Un solo vacío. Plan de 3 pasos: aguajes al vacío (dos vertidos, una dosis cada uno) y las dos guanábanas sobre la de la cuia 0. Llevar el taperebá suelto de la cuia 3 al vacío lleva en un vertido a Aguas estancadas.

- cuia 0: Açaí, Guanábana, Aguaje, Aguaje `[8, 1, 4, 4]`
- cuia 1: Taperebá, Açaí, Açaí, Guanábana `[3, 8, 8, 1]`
- cuia 2: Aguaje, Taperebá, Taperebá, Guanábana `[4, 3, 3, 1]`
- cuia 3: Guanábana, Aguaje, Açaí, Taperebá `[1, 4, 8, 3]`
- cuia 4: vacía `[]`

Solución óptima en `single` (17 vertidos): `0→4 ×1 · 0→4 ×1 · 1→0 ×1 · 2→0 ×1 · 3→2 ×1 · 3→1 ×1 · 3→4 ×1 · 0→3 ×1 · 0→3 ×1 · 0→3 ×1 · 1→0 ×1 · 1→0 ×1 · 1→0 ×1 · 2→1 ×1 · 2→1 ×1 · 2→1 ×1 · 2→4 ×1`

Camino más corto a Aguas estancadas en `single` (1 vertido(s)): `3→4 ×1` → `[8,1,4,4] [3,8,8,1] [4,3,3,1] [1,4,8] [3]`

### Nivel 7 · `m01_v1_n0007` · par 15 (`single`; en `run`: 11)

Debut de Bacaba, en la cima de las cuias 0 y 1. Tablero de comodidad tras el vacío único del 6: sin estados sin salida.

- cuia 0: Açaí, Açaí, Bacaba, Bacaba `[8, 8, 7, 7]`
- cuia 1: Taperebá, Aguaje, Aguaje, Bacaba `[3, 4, 4, 7]`
- cuia 2: Guanábana, Guanábana, Açaí, Taperebá `[1, 1, 8, 3]`
- cuia 3: Taperebá, Bacaba, Açaí, Aguaje `[3, 7, 8, 4]`
- cuia 4: Aguaje, Guanábana, Guanábana, Taperebá `[4, 1, 1, 3]`
- cuia 5: vacía `[]`
- cuia 6: vacía `[]`

Solución óptima en `single` (15 vertidos): `0→5 ×1 · 0→5 ×1 · 1→5 ×1 · 1→6 ×1 · 1→6 ×1 · 2→1 ×1 · 2→0 ×1 · 3→6 ×1 · 3→0 ×1 · 3→5 ×1 · 3→1 ×1 · 4→1 ×1 · 4→2 ×1 · 4→2 ×1 · 4→6 ×1`

Camino más corto a Aguas estancadas en `single`: no existe (ningún estado alcanzable se queda sin vertidos útiles).

### Nivel 8 · `m01_v1_n0008` · par 17 (`single`; en `run`: 13)

Reinicio probable con 2 vacíos: desenterrar las aguajes de la cuia 2 echando la guanábana y el taperebá sueltos a los dos vacíos lleva en dos vertidos a Aguas estancadas. La línea buena junta antes las bacabas.

- cuia 0: Aguaje, Aguaje, Bacaba, Bacaba `[4, 4, 7, 7]`
- cuia 1: Taperebá, Guanábana, Açaí, Bacaba `[3, 1, 8, 7]`
- cuia 2: Aguaje, Aguaje, Taperebá, Guanábana `[4, 4, 3, 1]`
- cuia 3: Guanábana, Açaí, Taperebá, Bacaba `[1, 8, 3, 7]`
- cuia 4: Guanábana, Taperebá, Açaí, Açaí `[1, 3, 8, 8]`
- cuia 5: vacía `[]`
- cuia 6: vacía `[]`

Solución óptima en `single` (17 vertidos): `0→5 ×1 · 0→5 ×1 · 1→5 ×1 · 1→6 ×1 · 3→5 ×1 · 4→6 ×1 · 4→6 ×1 · 3→4 ×1 · 3→6 ×1 · 1→3 ×1 · 2→3 ×1 · 2→1 ×1 · 0→2 ×1 · 0→2 ×1 · 4→1 ×1 · 4→1 ×1 · 4→3 ×1`

Camino más corto a Aguas estancadas en `single` (2 vertido(s)): `2→5 ×1 · 2→6 ×1` → `[4,4,7,7] [3,1,8,7] [4,4] [1,8,3,7] [1,3,8,8] [1] [3]`

### Nivel 9 · `m01_v1_n0009` · par 16 (`single`; en `run`: 14)

Sin enseñanza de tablero (la fila cambia la luz del fondo): par intermedio y sin Aguas estancadas alcanzables.

- cuia 0: Guanábana, Bacaba, Bacaba, Açaí `[1, 7, 7, 8]`
- cuia 1: Aguaje, Aguaje, Açaí, Guanábana `[4, 4, 8, 1]`
- cuia 2: Taperebá, Aguaje, Guanábana, Bacaba `[3, 4, 1, 7]`
- cuia 3: Bacaba, Açaí, Aguaje, Taperebá `[7, 8, 4, 3]`
- cuia 4: Taperebá, Guanábana, Taperebá, Açaí `[3, 1, 3, 8]`
- cuia 5: vacía `[]`
- cuia 6: vacía `[]`

Solución óptima en `single` (16 vertidos): `0→5 ×1 · 0→6 ×1 · 0→6 ×1 · 1→0 ×1 · 1→5 ×1 · 2→6 ×1 · 2→0 ×1 · 2→1 ×1 · 3→2 ×1 · 3→1 ×1 · 3→5 ×1 · 3→6 ×1 · 4→5 ×1 · 4→2 ×1 · 4→0 ×1 · 4→2 ×1`

Camino más corto a Aguas estancadas en `single`: no existe (ningún estado alcanzable se queda sin vertidos útiles).

### Nivel 10 · `m01_v1_n0010` · par 19 (`single`; en `run`: 16)

Cierre del Estuario: cuadrado latino de los cinco tintes (cada fruta baja en diagonal), sin pares repetidos ni estados sin salida.

- cuia 0: Açaí, Aguaje, Taperebá, Guanábana `[8, 4, 3, 1]`
- cuia 1: Bacaba, Açaí, Aguaje, Taperebá `[7, 8, 4, 3]`
- cuia 2: Guanábana, Bacaba, Açaí, Aguaje `[1, 7, 8, 4]`
- cuia 3: Taperebá, Guanábana, Bacaba, Açaí `[3, 1, 7, 8]`
- cuia 4: Aguaje, Taperebá, Guanábana, Bacaba `[4, 3, 1, 7]`
- cuia 5: vacía `[]`
- cuia 6: vacía `[]`

Solución óptima en `single` (19 vertidos): `0→5 ×1 · 0→6 ×1 · 1→6 ×1 · 0→1 ×1 · 3→0 ×1 · 4→3 ×1 · 4→5 ×1 · 4→6 ×1 · 1→4 ×1 · 1→4 ×1 · 1→0 ×1 · 2→4 ×1 · 2→0 ×1 · 2→1 ×1 · 2→5 ×1 · 3→1 ×1 · 3→1 ×1 · 3→5 ×1 · 3→6 ×1`

Camino más corto a Aguas estancadas en `single`: no existe (ningún estado alcanzable se queda sin vertidos útiles).
