# Campaña 11-200: informe de `level-generator --spec`

Generado por `dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/campaign_v1.json --levels 11-200 --out Assets/_Project/Data/Levels --report tools/out/campaign_v1_report.md`: no se edita a mano y no lleva tiempos. Generador 1.0.0, `pack_schema` 2, modo de vertido `run` (D-005).

Fuentes: D-009 (ciclo escalonado) y D-007 (debuts) con la propuesta `docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md` (c.1-c.4, d.1-d.4); GDD 03 §2, §4, §7-§13 y §17; 02 §8-§11; 08 §4.1. Todo se mide con `AF.Core` y `AF.Solver`: A* w = 1 a 300.000 (par), bot ×40 con la semilla del nivel (`bot_fail` y etiqueta), `EarlyTraps` (trampas tempranas, Q16 B), ruta de referencia para ramificación y trampas (`--parity`), `SafePath` en Aguas turbias y `LevelValidation` en cada tablero.

## Paquetes

| Pack | Niveles | Bytes | Bytes sin relief | Bytes sin relief por 100 niveles (≤ 100 KB, 03 §17 #11) | SHA-256 |
|---|---|---|---|---|---|
| `Assets/_Project/Data/Levels/mundo_02_v1.json` | 30 | 26162 | 22509 | 73.3 KB | `510694990af435774e4917335367e4ca618358feff93f39e3374bec540ea783e` |
| `Assets/_Project/Data/Levels/mundo_03_v1.json` | 40 | 48527 | 31190 | 76.1 KB | `1a692717cf1147bc5e1021507ffaba94e36bcff5308518c3b2a1968b3b5e4439` |
| `Assets/_Project/Data/Levels/mundo_04_v1.json` | 60 | 78577 | 49411 | 80.4 KB | `0dbf54df4b7dbe3130166821f49c933f255d3231142b682a9d98381bcf4d4a90` |
| `Assets/_Project/Data/Levels/mundo_05_v1.json` | 60 | 80101 | 49821 | 81.1 KB | `f9abfb475fb1081c6ea3f33b1146c08afe3147df6a93c3545d6c1ac8d27bc3b3` |

## Pool por rango e índice D (03 §7)

D = 0,35·z(bot_fail) + 0,25·z(par − 2K) + 0,20·z(ramificación) + 0,10·z(trampas) + 0,10·[E libres = 1], con media y desviación del pool del rango (todos los candidatos en ventana de sus slots; congelados tras la primera pasada). Banda = cuantil de D en el pool: recuperación ≤ 0,10, comodidad ≤ 0,70, reto ≤ 0,95, hito > 0,95.

| Rango | Slots | Pool | Pool / slots | bot_fail μ ± σ | par − 2K μ ± σ | Ramif. μ ± σ | Trampas μ ± σ |
|---|---|---|---|---|---|---|---|
| 11-30 | 20 | 75 | 3.8 | 0.20 ± 0.23 | 2.8 ± 2.7 | 5.38 ± 1.31 | 0.00 ± 0.01 |
| 31-60 | 30 | 143 | 4.8 | 0.45 ± 0.26 | 3.7 ± 3.3 | 4.35 ± 1.48 | 0.03 ± 0.05 |
| 61-100 | 40 | 169 | 4.2 | 0.45 ± 0.29 | 3.0 ± 3.2 | 5.06 ± 1.63 | 0.04 ± 0.08 |
| 101-200 | 100 | 439 | 4.4 | 0.46 ± 0.30 | 2.4 ± 2.6 | 6.08 ± 2.06 | 0.04 ± 0.07 |

## Etiquetas por rango frente a 03 §11

| Rango (niveles generados) | Difícil | Muy difícil | 03 §11 | Aviso (> 5 puntos) |
|---|---|---|---|---|
| 11-30 (20) | 0 (0.0 %) | 0 (0.0 %) | 0-0 % / 0 % | — |
| 31-60 (30) | 4 (13.3 %) | 1 (3.3 %) | 10-13 % / 3 % | — |
| 61-100 (40) | 8 (20.0 %) | 2 (5.0 %) | 15-15 % / 5 % | — |
| 101-200 (100) | 23 (23.0 %) | 5 (5.0 %) | 15-20 % / 5 % | — |

Nota: con D-009 desde el 61 los cinco retos apuntan a Difícil y el hito a Muy difícil (25 % y 5 % por ciclo), por encima del 15 % de 03 §11 para 61-100: es la cuenta del ciclo, no un error de la generación [P: la tabla de 03 §11 se revisa en la integración].

## Ciclos

| Ciclo | Temporada | Puros | Difícil | Muy difícil | Objetivo D / MD | Acierto de etiqueta (c.7) | Trampas tempranas | Relajaciones |
|---|---|---|---|---|---|---|---|---|
| 11-20 (rampa) | B | 8/10 | 0 | 0 | 0 / 0 | — | 0 | 0 |
| 21-40 | B | 8/20 | 1 | 0 | 1 / 0 | 1/1 (100 %) | 1 | 3 |
| 41-60 | A | 8/20 | 3 | 1 | 3 / 1 | 4/4 (100 %) | 2 | 3 |
| 61-80 | B | 8/20 | 3 | 1 | 5 / 1 | 4/6 (67 %) | 0 | 9 |
| 81-100 | A | 8/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 1 | 8 |
| 101-120 | B | 8/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 4 | 10 |
| 121-140 | A | 8/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 4 | 10 |
| 141-160 | A | 8/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 2 | 10 |
| 161-180 | B | 8/20 | 4 | 1 | 5 / 1 | 5/6 (83 %) | 2 | 8 |
| 181-200 | B | 8/20 | 4 | 1 | 5 / 1 | 5/6 (83 %) | 0 | 8 |

## Niveles

Columnas: p = posición en el ciclo (0 en la rampa); E = vacíos de capacidad ≥ 4 (con la Hoja o el Matapi vacíos), libres = sin mecánica; q = cuantil de D en el pool del rango (3 decimales: la banda de hito empieza por encima de 0,95) y su banda; TT = trampas tempranas; alivio = tipos publicados en `relief` en orden (− = minus_color, + = plus_empty).

| Nivel | p | Rol | Calendario | K | E (libres) | Cuias | Fuente | Par | Estados A* | bot_fail | Ventana | Etiqueta | Objetivo | D | q (banda) | Ramif. | Trampas | TT | Compl. | Alivio | Notas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 11 | 0 | rampa | C | 6 | 2 (2) | 8 | walk | 15 | 950 | 0.075 | [0.00, 0.10] | normal | — | -0.18 | 0.447 (comodidad) | 5.53 | 0.00 | 0 | 38 | — |  |
| 12 | 0 | rampa | C | 6 | 2 (2) | 8 | walk | 17 | 1414 | 0.100 | [0.00, 0.10] | normal | — | -0.07 | 0.567 (comodidad) | 4.82 | 0.00 | 0 | 42 | — |  |
| 13 | 0 | rampa | C | 6 | 2 (2) | 8 | walk | 15 | 1150 | 0.025 | [0.00, 0.20) | normal | — | -0.38 | 0.233 (comodidad) | 4.73 | 0.00 | 0 | 38 | — |  |
| 14 | 0 | rampa | *Ch | 6 | 1 (1) | 8 | walk | 13 | 933 | 0.125 | [0.00, 0.20) | normal | — | -0.14 | 0.473 (comodidad) | 5.92 | 0.00 | 0 | 36 | — |  |
| 15 | 0 | rampa | C | 6 | 2 (2) | 8 | walk | 16 | 1571 | 0.175 | [0.00, 0.20) | normal | — | 0.14 | 0.660 (comodidad) | 6.06 | 0.00 | 0 | 40 | — |  |
| 16 | 0 | rampa | C | 6 | 2 (2) | 8 | walk | 14 | 316 | 0.050 | [0.00, 0.20) | normal | — | -0.50 | 0.153 (comodidad) | 4.36 | 0.00 | 0 | 36 | — |  |
| 17 | 0 | rampa | C | 6 | 2 (2) | 8 | walk | 15 | 523 | 0.075 | [0.00, 0.20) | normal | — | -0.12 | 0.500 (comodidad) | 5.93 | 0.00 | 0 | 40 | — |  |
| 18 | 0 | rampa | *Ch1 | 6 | 1 (1) | 8 | walk | 12 | 86 | 0.000 | [0.00, 0.20) | normal | — | -0.36 | 0.260 (comodidad) | 5.67 | 0.01 | 0 | 34 | — |  |
| 19 | 0 | rampa | C | 6 | 2 (2) | 8 | walk | 13 | 638 | 0.000 | [0.00, 0.20) | normal | — | -0.75 | 0.020 (recuperacion) | 3.77 | 0.00 | 0 | 34 | — |  |
| 20 | 0 | rampa | C | 6 | 2 (2) | 8 | walk | 12 | 154 | 0.175 | [0.00, 0.20) | normal | — | -0.41 | 0.220 (comodidad) | 4.92 | 0.00 | 0 | 34 | — |  |
| 21 | 1 | recuperacion | R | 6 | 2 (2) | 8 | walk | 13 | 591 | 0.000 | [0.00, 0.30) | normal | — | -0.77 | 0.007 (recuperacion) | 3.69 | 0.00 | 0 | 34 | — |  |
| 22 | 2 | recuperacion | *Ch3 | 6 | 2 (2) | 9 | walk | 13 | 522 | 0.000 | [0.00, 0.20) | normal | — | -0.34 | 0.267 (comodidad) | 6.46 | 0.00 | 0 | 36 | — | 6 ronda(s) de banda |
| 23 | 3 | comodidad | Ch | 7 | 1 (1) | 9 | walk | 15 | 1610 | 0.250 | [0.20, 0.60] | normal | — | -0.26 | 0.367 (comodidad) | 3.87 | 0.00 | 0 | 40 | — |  |
| 24 | 4 | comodidad | C | 7 | 2 (2) | 9 | walk | 14 | 827 | 0.350 | [0.20, 0.60] | normal | — | -0.17 | 0.467 (comodidad) | 4.71 | 0.00 | 0 | 38 | — | 1 ronda(s) de banda |
| 25 | 5 | comodidad | C | 7 | 2 (2) | 9 | walk | 15 | 973 | 0.275 | [0.20, 0.60] | normal | — | -0.25 | 0.380 (comodidad) | 4.33 | 0.00 | 0 | 40 | — |  |
| 26 | 6 | reto | Ch (s) | 7 | 1 (1) | 9 | fill | 18 | 2214 | 0.575 | [0.50, 0.70] | normal | — | 0.83 | 0.860 (reto) | 5.94 | 0.00 | 0 | 46 | — |  |
| 27 | 7 | estreno | *Al | 7 | 2 (2) | 9 | walk | 15 | 1059 | 0.150 | [0.00, 0.20) | normal | — | -0.03 | 0.620 (comodidad) | 4.20 | 0.06 | 0 | 40 | — |  |
| 28 | 8 | estreno | *Al | 7 | 2 (2) | 9 | walk | 16 | 1356 | 0.075 | [0.00, 0.20) | normal | — | -0.13 | 0.487 (comodidad) | 6.50 | 0.00 | 0 | 42 | — |  |
| 29 | 9 | reto | Ch (s) | 7 | 1 (1) | 9 | fill | 23 | 6662 | 0.650 | [0.50, 0.70] | normal | — | 0.94 | 0.900 (reto) | 2.83 | 0.00 | 0 | 54 | — |  |
| 30 | 10 | comodidad | C | 7 | 2 (2) | 9 | walk | 15 | 920 | 0.250 | [0.20, 0.60] | normal | — | 0.19 | 0.700 (comodidad) | 6.00 | 0.03 | 0 | 40 | — |  |
| 31 | 11 | comodidad | C | 7 | 1 (1) | 8 | walk | 15 | 128 | 0.275 | [0.20, 0.60] | normal | — | -0.19 | 0.353 (comodidad) | 2.27 | 0.26 | 0 | 38 | +(0.00) −1(14; 0.00) |  |
| 32 | 12 | reto | Al (s) | 7 | 2 (2) | 9 | fill | 25 | 7842 | 0.650 | [0.50, 0.70] | normal | — | 0.58 | 0.808 (reto) | 2.72 | 0.01 | 0 | 56 | −3(21; 0.00) +(0.00) |  |
| 33 | 13 | comodidad | Ch | 7 | 1 (1) | 9 | walk | 14 | 97 | 0.300 | [0.20, 0.60] | normal | — | -0.10 | 0.430 (comodidad) | 6.93 | 0.00 | 0 | 40 | +(0.03) −3(13; 0.00) |  |
| 34 | 14 | comodidad | C | 7 | 2 (2) | 9 | walk | 19 | 4904 | 0.500 | [0.20, 0.60] | normal | — | 0.17 | 0.612 (comodidad) | 4.84 | 0.00 | 0 | 46 | −7(17; 0.00) +(0.08) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 35 | 15 | reto | Ch+Al (s) | 7 | 1 (1) | 9 | fill | 24 | 6135 | 0.600 | [0.50, 0.70] | normal | — | 0.67 | 0.843 (reto) | 3.38 | 0.04 | 0 | 54 | +(0.00) −10(21; 0.00) | relajacion: fill con 1 vacio |
| 36 | 16 | comodidad | C | 7 | 1 (1) | 8 | walk | 14 | 103 | 0.550 | [0.20, 0.60] | normal | — | -0.30 | 0.269 (comodidad) | 2.50 | 0.03 | 0 | 40 | +(0.00) −4(13; 0.00) |  |
| 37 | 17 | comodidad | Al | 7 | 1 (1) | 8 | walk | 14 | 158 | 0.600 | [0.20, 0.60] | normal | — | -0.02 | 0.493 (comodidad) | 2.36 | 0.15 | 0 | 38 | +(0.05) −3(13; 0.05) |  |
| 38 | 18 | reto | Ch (s) | 7 | 1 (1) | 9 | fill | 24 | 6797 | 0.625 | [0.50, 0.70] | normal | — | 0.77 | 0.913 (reto) | 4.00 | 0.03 | 0 | 56 | +(0.00) −4(20; 0.00) |  |
| 39 | 19 | remanso | S | 7 | 2 (2) | 9 | walk | 17 | 2224 | 0.225 | [0.20, 0.40] | normal | — | -0.13 | 0.409 (comodidad) | 6.47 | 0.00 | 0 | 44 | −1(16; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 40 | 20 | hito | Ch+Al (D) | 7 | 1 (1) | 9 | fill | 25 | 6898 | 0.825 | [0.75, 0.85] | hard | hard | 1.06 | 0.962 (hito) | 3.32 | 0.05 | 1 | 52 | +(0.03) −4(19; 0.00) |  |
| 41 | 1 | recuperacion | R | 6 | 2 (2) | 8 | walk | 14 | 792 | 0.050 | [0.00, 0.30) | normal | — | -0.56 | 0.087 (recuperacion) | 5.64 | 0.00 | 0 | 36 | −1(12; 0.00) +(0.00) |  |
| 42 | 2 | recuperacion | R | 6 | 2 (2) | 8 | walk | 13 | 421 | 0.000 | [0.00, 0.30) | normal | — | -0.80 | 0.045 (recuperacion) | 4.92 | 0.00 | 0 | 34 | −1(11; 0.00) +(0.00) |  |
| 43 | 3 | estreno | *Ho | 7 | 2 (1) | 9 | walk | 15 | 648 | 0.000 | [0.00, 0.20) | normal | — | -0.86 | 0.024 (recuperacion) | 3.80 | 0.00 | 0 | 40 | −8(14; 0.00) +(0.00) |  |
| 44 | 4 | estreno | *Ho | 7 | 2 (1) | 9 | walk | 16 | 383 | 0.100 | [0.00, 0.20) | normal | — | -0.78 | 0.059 (recuperacion) | 2.81 | 0.00 | 0 | 40 | −3(14; 0.00) +(0.00) |  |
| 45 | 5 | estreno | *Ho | 7 | 2 (1) | 9 | walk | 18 | 1931 | 0.125 | [0.00, 0.20) | normal | — | -0.47 | 0.164 (comodidad) | 3.72 | 0.00 | 0 | 44 | −7(16; 0.00) +(0.00) |  |
| 46 | 6 | reto | Ch (D) | 7 | 1 (1) | 9 | deep | 18 | 1642 | 0.775 | [0.75, 0.90) | hard | hard | 0.67 | 0.850 (reto) | 4.94 | 0.04 | 0 | 46 | +(0.08) −5(17; 0.03) |  |
| 47 | 7 | comodidad | Ho | 7 | 2 (1) | 9 | walk | 17 | 893 | 0.225 | [0.20, 0.60] | normal | — | -0.38 | 0.227 (comodidad) | 3.88 | 0.00 | 0 | 44 | +(0.00) −5(15; 0.00) |  |
| 48 | 8 | comodidad | C | 7 | 1 (1) | 8 | walk | 15 | 115 | 0.575 | [0.20, 0.60] | normal | — | -0.19 | 0.360 (comodidad) | 2.60 | 0.03 | 0 | 40 | +(0.13) −1(14; 0.30) |  |
| 49 | 9 | reto | Ho (s) | 7 | 3 (2) | 10 | fill | 21 | 7628 | 0.550 | [0.50, 0.75) | normal | — | 0.46 | 0.738 (reto) | 5.29 | 0.00 | 0 | 50 | −1(17; 0.00) +(0.00) | relajacion: fill con 2 vacios (03 sec. 4.4) |
| 50 | 10 | comodidad | C | 7 | 2 (2) | 9 | walk | 16 | 2017 | 0.275 | [0.20, 0.60] | normal | — | -0.13 | 0.416 (comodidad) | 6.56 | 0.00 | 0 | 42 | −4(15; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 51 | 11 | comodidad | Al | 7 | 1 (1) | 8 | walk | 14 | 121 | 0.575 | [0.20, 0.60] | normal | — | -0.09 | 0.437 (comodidad) | 2.79 | 0.10 | 0 | 38 | +(0.00) −3(13; 0.00) |  |
| 52 | 12 | reto | Ho+Ch (D) | 7 | 1 (0) | 9 | fill | 21 | 3349 | 0.775 | [0.75, 0.90) | hard | hard | 0.75 | 0.906 (reto) | 4.48 | 0.05 | 0 | 48 | +(0.03) −5(19; 0.00) |  |
| 53 | 13 | comodidad | C | 7 | 1 (1) | 8 | walk | 15 | 181 | 0.200 | [0.20, 0.60] | normal | — | -0.50 | 0.129 (comodidad) | 2.67 | 0.13 | 0 | 38 | +(0.00) −1(13; 0.00) |  |
| 54 | 14 | comodidad | C | 7 | 1 (1) | 8 | walk | 15 | 161 | 0.450 | [0.20, 0.60] | normal | — | -0.34 | 0.241 (comodidad) | 3.07 | 0.00 | 0 | 42 | +(0.00) −1(14; 0.00) |  |
| 55 | 15 | reto | Al (s) | 7 | 1 (1) | 8 | deep | 16 | 148 | 0.700 | [0.50, 0.75) | normal | — | 0.07 | 0.563 (comodidad) | 2.19 | 0.06 | 0 | 44 | +(0.13) −4(14; 0.00) |  |
| 56 | 16 | comodidad | C | 7 | 1 (1) | 8 | walk | 14 | 142 | 0.325 | [0.20, 0.60] | normal | — | -0.53 | 0.101 (comodidad) | 2.79 | 0.05 | 0 | 38 | +(0.00) −3(13; 0.00) |  |
| 57 | 17 | comodidad | Ch | 7 | 1 (1) | 9 | walk | 15 | 1651 | 0.450 | [0.20, 0.60] | normal | — | 0.13 | 0.591 (comodidad) | 6.53 | 0.00 | 0 | 40 | +(0.00) −7(14; 0.00) |  |
| 58 | 18 | reto | Ho+Al (D) | 7 | 2 (1) | 9 | walk | 19 | 1527 | 0.850 | [0.75, 0.90) | hard | hard | 0.80 | 0.920 (reto) | 4.11 | 0.08 | 0 | 46 | +(0.23) −5(17; 0.00) | relajacion: walk con 1 vacio |
| 59 | 19 | remanso | S | 7 | 1 (1) | 8 | walk | 15 | 178 | 0.350 | [0.20, 0.40] | normal | — | -0.28 | 0.297 (comodidad) | 2.93 | 0.11 | 0 | 40 | +(0.03) −1(14; 0.05) |  |
| 60 | 20 | hito | Ho+Ch (MD) | 7 | 1 (0) | 9 | fill | 23 | 2239 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.12 | 0.976 (hito) | 3.91 | 0.04 | 2 | 52 | +(0.00) −4(20; 0.00) |  |
| 61 | 1 | recuperacion | R | 7 | 2 (2) | 9 | walk | 14 | 1077 | 0.000 | [0.00, 0.30) | normal | — | -0.75 | 0.044 (recuperacion) | 5.71 | 0.00 | 0 | 36 | −1(12; 0.00) +(0.00) |  |
| 62 | 2 | recuperacion | R | 7 | 2 (2) | 9 | walk | 14 | 1077 | 0.025 | [0.00, 0.30) | normal | — | -0.78 | 0.024 (recuperacion) | 5.21 | 0.00 | 0 | 38 | −1(12; 0.00) +(0.00) | 1 ronda(s) de banda |
| 63 | 3 | estreno | *Ma | 8 | 2 (1) | 10 | walk | 16 | 6902 | 0.025 | [0.00, 0.20) | normal | — | -0.39 | 0.175 (comodidad) | 7.56 | 0.00 | 0 | 42 | −7(15; 0.00) +(0.00) |  |
| 64 | 4 | estreno | *Ma | 8 | 2 (1) | 10 | walk | 16 | 2717 | 0.000 | [0.00, 0.20) | normal | — | -0.61 | 0.098 (recuperacion) | 6.06 | 0.00 | 0 | 44 | −1(15; 0.00) +(0.00) |  |
| 65 | 5 | estreno | *Ma | 8 | 2 (1) | 10 | walk | 16 | 5758 | 0.000 | [0.00, 0.20) | normal | — | -0.57 | 0.115 (comodidad) | 6.38 | 0.00 | 0 | 44 | −1(14; 0.00) +(0.00) |  |
| 66 | 6 | reto | Ho+Ch (D) | 8 | 1 (0) | 10 | deep | 20 | 682 | 0.725 | [0.75, 0.90) | normal | hard | 0.22 | 0.686 (comodidad) | 2.80 | 0.11 | 0 | 48 | +(0.03) −1(17; 0.00) | fuera de ventana (relajacion agotada); 1 ronda(s) extra de candidatos |
| 67 | 7 | comodidad | Ma | 8 | 2 (1) | 10 | walk | 16 | 4477 | 0.225 | [0.20, 0.60] | normal | — | 0.13 | 0.642 (comodidad) | 9.88 | 0.00 | 0 | 44 | +(0.00) −3(15; 0.00) |  |
| 68 | 8 | comodidad | C | 8 | 2 (2) | 10 | walk | 18 | 1501 | 0.375 | [0.20, 0.60] | normal | — | -0.08 | 0.506 (comodidad) | 5.44 | 0.07 | 0 | 46 | −3(16; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 69 | 9 | reto | Ma (D) | 8 | 2 (1) | 10 | walk | 17 | 3367 | 0.825 | [0.75, 0.90) | hard | hard | 0.47 | 0.772 (reto) | 6.12 | 0.00 | 0 | 46 | +(0.00) −3(16; 0.00) | relajacion: walk con 1 vacio |
| 70 | 10 | comodidad | C | 8 | 2 (2) | 10 | walk | 17 | 2864 | 0.400 | [0.20, 0.60] | normal | — | -0.16 | 0.429 (comodidad) | 6.00 | 0.00 | 0 | 46 | −6(16; 0.00) +(0.03) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 71 | 11 | comodidad | Al | 8 | 1 (1) | 10 | walk | 17 | 1504 | 0.300 | [0.20, 0.60] | normal | — | -0.05 | 0.541 (comodidad) | 7.00 | 0.00 | 0 | 48 | +(0.00) −14(16; 0.00) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4) |
| 72 | 12 | reto | Ma+Ho (D) | 8 | 2 (0) | 10 | fill | 21 | 2071 | 0.750 | [0.75, 0.90) | hard | hard | 0.64 | 0.858 (reto) | 4.52 | 0.19 | 0 | 54 | +(0.15) −4(19; 0.00) | 1 ronda(s) extra de candidatos |
| 73 | 13 | comodidad | C | 8 | 1 (1) | 9 | walk | 18 | 288 | 0.375 | [0.20, 0.60] | normal | — | -0.38 | 0.198 (comodidad) | 2.17 | 0.08 | 0 | 48 | +(0.13) −7(17; 0.13) |  |
| 74 | 14 | comodidad | Ho | 8 | 2 (1) | 10 | walk | 16 | 732 | 0.600 | [0.20, 0.60] | normal | — | -0.08 | 0.500 (comodidad) | 3.94 | 0.05 | 0 | 46 | +(0.00) −4(15; 0.00) |  |
| 75 | 15 | reto | Al+Ch (D) | 8 | 1 (1) | 10 | deep | 22 | 16286 | 0.725 | [0.75, 0.90) | normal | hard | 0.64 | 0.858 (reto) | 4.82 | 0.05 | 0 | 52 | +(0.00) −8(20; 0.00) | fuera de ventana (relajacion agotada) |
| 76 | 16 | comodidad | C | 8 | 1 (1) | 9 | walk | 16 | 231 | 0.575 | [0.20, 0.60] | normal | — | -0.24 | 0.340 (comodidad) | 2.69 | 0.07 | 0 | 40 | +(0.00) −5(14; 0.00) |  |
| 77 | 17 | comodidad | C | 8 | 2 (2) | 10 | walk | 20 | 3170 | 0.425 | [0.20, 0.60] | normal | — | 0.07 | 0.607 (comodidad) | 5.70 | 0.00 | 0 | 50 | −14(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 78 | 18 | reto | Ma+Al (D) | 8 | 2 (1) | 10 | walk | 20 | 13065 | 0.750 | [0.75, 0.90) | hard | hard | 0.80 | 0.902 (reto) | 5.75 | 0.17 | 0 | 50 | +(0.08) −1(18; 0.10) | relajacion: walk con 1 vacio |
| 79 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 18 | 2808 | 0.225 | [0.20, 0.40] | normal | — | -0.24 | 0.334 (comodidad) | 6.11 | 0.03 | 0 | 48 | −1(16; 0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 80 | 20 | hito | Ho+Ma (MD) | 8 | 2 (0) | 10 | fill | 28 | 1278 | 0.950 | [0.90, 1.00] | very_hard | very_hard | 1.04 | 0.962 (hito) | 2.46 | 0.09 | 0 | 62 | +(0.10) −3(23; 0.03) |  |
| 81 | 1 | recuperacion | R | 7 | 2 (2) | 9 | walk | 16 | 1549 | 0.050 | [0.00, 0.30) | normal | — | -0.67 | 0.080 (recuperacion) | 3.94 | 0.06 | 0 | 40 | −1(15; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 82 | 2 | recuperacion | R | 7 | 2 (2) | 9 | walk | 14 | 1199 | 0.000 | [0.00, 0.30) | normal | — | -0.92 | 0.009 (recuperacion) | 4.36 | 0.00 | 0 | 36 | −1(12; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 83 | 3 | estreno | *Li | 8 | 2 (2) | 10 | walk | 17 | 699 | 0.025 | [0.00, 0.20) | normal | — | -0.67 | 0.074 (recuperacion) | 5.47 | 0.00 | 0 | 44 | −14(15; 0.00) +(0.00) |  |
| 84 | 4 | estreno | *Li | 8 | 2 (2) | 10 | walk | 17 | 1475 | 0.000 | [0.00, 0.20) | normal | — | -0.75 | 0.038 (recuperacion) | 5.06 | 0.00 | 0 | 46 | −7(16; 0.00) +(0.00) |  |
| 85 | 5 | estreno | *Li | 8 | 2 (2) | 10 | walk | 16 | 2213 | 0.050 | [0.00, 0.20) | normal | — | -0.75 | 0.033 (recuperacion) | 5.19 | 0.00 | 0 | 44 | −1(15; 0.00) +(0.00) |  |
| 86 | 6 | reto | Ma+Ho (D) | 9 | 2 (0) | 11 | deep | 22 | 861 | 0.875 | [0.75, 0.90) | hard | hard | 0.37 | 0.728 (reto) | 2.59 | 0.11 | 0 | 54 | +(0.18) −1(20; 0.10) | 1 ronda(s) extra de candidatos |
| 87 | 7 | comodidad | Li | 8 | 2 (2) | 10 | walk | 19 | 2095 | 0.450 | [0.20, 0.60] | normal | — | -0.04 | 0.547 (comodidad) | 5.16 | 0.00 | 0 | 48 | −10(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4); 1 ronda(s) extra de candidatos |
| 88 | 8 | comodidad | C | 8 | 1 (1) | 9 | walk | 17 | 267 | 0.550 | [0.20, 0.60] | normal | — | -0.12 | 0.464 (comodidad) | 3.18 | 0.07 | 0 | 46 | +(0.00) −10(16; 0.00) |  |
| 89 | 9 | reto | Li (D) | 9 | 2 (2) | 11 | fill | 26 | 1047 | 0.750 | [0.75, 0.90) | hard | hard | 0.47 | 0.775 (reto) | 3.08 | 0.01 | 0 | 64 | −2(24; 0.03) +(0.05) | relajacion: fill con 2 vacios (03 sec. 4.4); 1 ronda(s) extra de candidatos |
| 90 | 10 | comodidad | C | 8 | 1 (1) | 9 | walk | 18 | 296 | 0.475 | [0.20, 0.60] | normal | — | -0.29 | 0.299 (comodidad) | 2.17 | 0.05 | 0 | 46 | +(0.00) −7(16; 0.00) | 1 ronda(s) extra de candidatos |
| 91 | 11 | comodidad | Ch | 8 | 1 (1) | 10 | walk | 16 | 853 | 0.350 | [0.20, 0.60] | normal | — | -0.12 | 0.470 (comodidad) | 6.63 | 0.00 | 0 | 46 | +(0.00) −3(15; 0.00) |  |
| 92 | 12 | reto | Li+Ho (D) | 9 | 2 (1) | 11 | walk | 20 | 1159 | 0.850 | [0.75, 0.90) | hard | hard | 0.51 | 0.793 (reto) | 4.30 | 0.12 | 0 | 54 | +(0.00) −7(19; 0.00) | relajacion: walk con 1 vacio; 1 ronda(s) extra de candidatos |
| 93 | 13 | comodidad | C | 8 | 2 (2) | 10 | walk | 19 | 2296 | 0.375 | [0.20, 0.60] | normal | — | -0.29 | 0.287 (comodidad) | 3.53 | 0.03 | 0 | 48 | −4(16; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 94 | 14 | comodidad | Li | 8 | 1 (1) | 9 | walk | 16 | 256 | 0.450 | [0.20, 0.60] | normal | — | -0.41 | 0.163 (comodidad) | 2.44 | 0.08 | 0 | 42 | +(0.00) −6(14; 0.00) |  |
| 95 | 15 | reto | Ma+Al (D) | 9 | 2 (1) | 11 | walk | 21 | 13202 | 0.750 | [0.75, 0.90) | hard | hard | 0.62 | 0.831 (reto) | 5.62 | 0.11 | 0 | 54 | +(0.10) −1(20; 0.15) | relajacion: walk con 1 vacio; 1 ronda(s) extra de candidatos |
| 96 | 16 | comodidad | C | 8 | 1 (1) | 9 | walk | 17 | 238 | 0.575 | [0.20, 0.60] | normal | — | -0.17 | 0.405 (comodidad) | 2.59 | 0.07 | 0 | 44 | +(0.05) −5(15; 0.00) | 1 ronda(s) extra de candidatos |
| 97 | 17 | comodidad | C | 8 | 2 (2) | 10 | walk | 16 | 1540 | 0.325 | [0.20, 0.60] | normal | — | -0.20 | 0.376 (comodidad) | 6.69 | 0.03 | 0 | 44 | −8(15; 0.00) +(0.03) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 98 | 18 | reto | Li+Ma (D) | 9 | 2 (1) | 11 | walk | 22 | 2530 | 0.850 | [0.75, 0.90) | hard | hard | 0.72 | 0.885 (reto) | 5.14 | 0.08 | 0 | 58 | +(0.35) −2(21; 0.43) | relajacion: walk con 1 vacio; 1 ronda(s) extra de candidatos |
| 99 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 19 | 1874 | 0.350 | [0.20, 0.40] | normal | — | -0.38 | 0.204 (comodidad) | 3.05 | 0.03 | 0 | 50 | −1(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 100 | 20 | hito | *Tu (MD) | 9 | 0 (0) | 10 | deep | 22 | 216 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 0.73 | 0.899 (reto) | 2.50 | 0.27 | 1 | 60 | +(0.03) −7(21; 0.00) | 1 ronda(s) extra de candidatos; 6 ronda(s) de banda; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 101 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 17 | 2109 | 0.050 | [0.00, 0.30) | normal | — | -0.64 | 0.063 (recuperacion) | 6.35 | 0.00 | 0 | 46 | −2(16; 0.00) +(0.00) |  |
| 102 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1523 | 0.025 | [0.00, 0.30) | normal | — | -0.73 | 0.024 (recuperacion) | 6.69 | 0.00 | 0 | 44 | −6(15; 0.00) +(0.00) |  |
| 103 | 3 | estreno | *Re | 9 | 2 (2) | 11 | walk | 21 | 5356 | 0.125 | [0.00, 0.20) | normal | — | -0.27 | 0.329 (comodidad) | 7.00 | 0.02 | 0 | 56 | −2(19; 0.00) +(0.00) |  |
| 104 | 4 | estreno | *Re | 9 | 2 (1) | 11 | walk | 19 | 125433 | 0.175 | [0.00, 0.20) | normal | — | -0.06 | 0.498 (comodidad) | 9.11 | 0.05 | 0 | 50 | −6(17; 0.00) +(0.00) |  |
| 105 | 5 | estreno | *Re | 9 | 2 (2) | 11 | walk | 20 | 9036 | 0.150 | [0.00, 0.20) | normal | — | 0.17 | 0.655 (comodidad) | 12.45 | 0.00 | 0 | 54 | −14(19; 0.08) +(0.00) |  |
| 106 | 6 | reto | Ho+Tu (D) | 9 | 1 (0) | 11 | deep | 22 | 7671 | 0.775 | [0.75, 0.90) | hard | hard | 0.30 | 0.728 (reto) | 3.64 | 0.05 | 0 | 58 | +(0.08) −5(20; 0.13) | turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 107 | 7 | comodidad | Re | 9 | 1 (0) | 11 | walk | 19 | 137364 | 0.350 | [0.20, 0.60] | normal | — | 0.21 | 0.685 (comodidad) | 11.11 | 0.03 | 0 | 50 | +(0.00) −10(17; 0.00) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4) |
| 108 | 8 | comodidad | C | 9 | 2 (2) | 11 | walk | 18 | 4286 | 0.275 | [0.20, 0.60] | normal | — | -0.31 | 0.293 (comodidad) | 8.00 | 0.00 | 0 | 50 | −2(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 109 | 9 | reto | Re (D) | 9 | 1 (0) | 10 | walk | 18 | 5946 | 0.875 | [0.75, 0.90) | hard | hard | 0.31 | 0.730 (reto) | 4.83 | 0.17 | 0 | 50 | +(0.28) −14(17; 0.18) | relajacion: walk con 1 vacio |
| 110 | 10 | comodidad | C | 9 | 2 (2) | 11 | walk | 20 | 4272 | 0.450 | [0.20, 0.60] | normal | — | 0.03 | 0.571 (comodidad) | 7.40 | 0.00 | 0 | 54 | −2(19; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 111 | 11 | comodidad | Ma | 9 | 2 (1) | 11 | walk | 21 | 13310 | 0.500 | [0.20, 0.60] | normal | — | 0.05 | 0.591 (comodidad) | 5.00 | 0.00 | 0 | 56 | +(0.00) −1(19; 0.00) |  |
| 112 | 12 | reto | Re+Li (D) | 9 | 1 (0) | 10 | walk | 19 | 4352 | 0.850 | [0.75, 0.90) | hard | hard | 0.46 | 0.810 (reto) | 7.53 | 0.04 | 0 | 50 | +(0.05) −14(16; 0.05) | relajacion: walk con 1 vacio |
| 113 | 13 | comodidad | C | 9 | 2 (2) | 11 | walk | 18 | 3910 | 0.375 | [0.20, 0.60] | normal | — | -0.22 | 0.363 (comodidad) | 7.78 | 0.00 | 0 | 50 | −3(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 114 | 14 | comodidad | Ch | 9 | 1 (1) | 11 | walk | 18 | 2098 | 0.350 | [0.20, 0.60] | normal | — | -0.22 | 0.366 (comodidad) | 7.06 | 0.00 | 0 | 52 | +(0.00) −3(17; 0.00) |  |
| 115 | 15 | reto | Ma+Tu (D) | 9 | 1 (0) | 11 | deep | 23 | 18657 | 0.775 | [0.75, 0.90) | hard | hard | 0.78 | 0.924 (reto) | 6.17 | 0.15 | 0 | 63 | +(0.15) −3(21; 0.00) | turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 116 | 16 | comodidad | C | 9 | 2 (2) | 11 | walk | 18 | 2073 | 0.225 | [0.20, 0.60] | normal | — | -0.58 | 0.090 (recuperacion) | 5.89 | 0.00 | 0 | 50 | −11(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 117 | 17 | comodidad | C | 9 | 2 (2) | 11 | walk | 20 | 6691 | 0.400 | [0.20, 0.60] | normal | — | 0.03 | 0.575 (comodidad) | 7.45 | 0.04 | 0 | 52 | −1(18; 0.00) +(0.03) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 118 | 18 | reto | Re+Ho (D) | 9 | 2 (1) | 11 | walk | 23 | 109491 | 0.875 | [0.75, 0.90) | hard | hard | 0.98 | 0.944 (reto) | 8.00 | 0.01 | 0 | 58 | +(0.10) | relajacion: walk con 1 vacio |
| 119 | 19 | remanso | S | 9 | 2 (2) | 11 | walk | 19 | 3933 | 0.250 | [0.20, 0.40] | normal | — | -0.24 | 0.347 (comodidad) | 8.05 | 0.00 | 0 | 52 | −7(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 120 | 20 | hito | Re+Ma (MD) | 9 | 1 (0) | 10 | deep | 22 | 4403 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.16 | 0.962 (hito) | 6.09 | 0.32 | 4 | 52 | +(0.35) −11(19; 0.15) |  |
| 121 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1832 | 0.200 | [0.00, 0.30) | normal | — | -0.60 | 0.079 (recuperacion) | 5.44 | 0.03 | 0 | 42 | −1(14; 0.00) +(0.00) |  |
| 122 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 921 | 0.025 | [0.00, 0.30) | normal | — | -0.83 | 0.006 (recuperacion) | 5.63 | 0.00 | 0 | 44 | −3(15; 0.00) +(0.00) |  |
| 123 | 3 | estreno | *Ba | 9 | 2 (2) | 12 | walk | 19 | 7299 | 0.000 | [0.00, 0.20) | normal | — | -0.46 | 0.188 (comodidad) | 8.79 | 0.00 | 0 | 58 | −2(17; 0.00) +(0.00) |  |
| 124 | 4 | estreno | *Ba | 9 | 2 (2) | 12 | walk | 18 | 1419 | 0.000 | [0.00, 0.20) | normal | — | -0.53 | 0.126 (comodidad) | 9.00 | 0.00 | 0 | 56 | −2(17; 0.00) +(0.00) |  |
| 125 | 5 | estreno | *Ba | 9 | 2 (2) | 12 | walk | 21 | 39361 | 0.000 | [0.00, 0.20) | normal | — | -0.36 | 0.256 (comodidad) | 7.76 | 0.00 | 0 | 57 | −11(20; 0.00) +(0.00) |  |
| 126 | 6 | reto | Re+Ho (D) | 9 | 2 (1) | 11 | walk | 18 | 36856 | 0.800 | [0.75, 0.90) | hard | hard | 0.27 | 0.714 (reto) | 6.39 | 0.03 | 1 | 46 | +(0.00) −7(16; 0.08) | relajacion: walk con 1 vacio |
| 127 | 7 | comodidad | Ba | 9 | 1 (1) | 11 | walk | 18 | 172 | 0.225 | [0.20, 0.60] | normal | — | -0.24 | 0.352 (comodidad) | 8.33 | 0.00 | 0 | 58 | +(0.00) −1(17; 0.00) |  |
| 128 | 8 | comodidad | C | 9 | 1 (1) | 10 | walk | 18 | 266 | 0.350 | [0.20, 0.60] | normal | — | -0.54 | 0.122 (comodidad) | 2.61 | 0.09 | 0 | 50 | +(0.00) −7(17; 0.05) |  |
| 129 | 9 | reto | Ba (D) | 9 | 0 (0) | 10 | walk | 20 | 551 | 0.750 | [0.75, 0.90) | hard | hard | 0.36 | 0.748 (reto) | 3.35 | 0.28 | 0 | 55 | +(0.10) −4(19; 0.00) | relajacion: walk con 1 vacio |
| 130 | 10 | comodidad | C | 9 | 2 (2) | 11 | walk | 20 | 5096 | 0.300 | [0.20, 0.60] | normal | — | -0.43 | 0.211 (comodidad) | 4.55 | 0.00 | 0 | 54 | −2(19; 0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 131 | 11 | comodidad | Li | 9 | 2 (2) | 11 | walk | 21 | 1763 | 0.600 | [0.20, 0.60] | normal | — | 0.13 | 0.625 (comodidad) | 5.57 | 0.01 | 0 | 56 | −10(20; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4) |
| 132 | 12 | reto | Ba+Ma (D) | 9 | 1 (0) | 11 | walk | 20 | 17716 | 0.750 | [0.75, 0.90) | hard | hard | 0.52 | 0.830 (reto) | 8.20 | 0.05 | 0 | 55 | +(0.03) −7(19; 0.00) | relajacion: walk con 1 vacio |
| 133 | 13 | comodidad | C | 9 | 1 (1) | 10 | walk | 20 | 735 | 0.300 | [0.20, 0.60] | normal | — | -0.52 | 0.142 (comodidad) | 2.60 | 0.00 | 0 | 50 | +(0.00) −3(18; 0.00) |  |
| 134 | 14 | comodidad | Ho | 9 | 2 (1) | 11 | walk | 22 | 9817 | 0.225 | [0.20, 0.60] | normal | — | -0.16 | 0.429 (comodidad) | 5.05 | 0.01 | 0 | 54 | +(0.00) −5(20; 0.00) |  |
| 135 | 15 | reto | Ba+Tu (D) | 9 | 1 (1) | 11 | deep | 21 | 8885 | 0.850 | [0.75, 0.90) | hard | hard | 0.57 | 0.844 (reto) | 5.19 | 0.07 | 0 | 60 | +(0.00) −4(20; 0.00) | relajacion: turbias con el vacio del rol y la holgura; 2 ronda(s) extra de candidatos turbios; turbia: 3 dosis ocultas en 2 cuia(s), 4 realidades |
| 136 | 16 | comodidad | C | 9 | 2 (2) | 11 | walk | 21 | 12581 | 0.550 | [0.20, 0.60] | normal | — | 0.01 | 0.552 (comodidad) | 5.05 | 0.00 | 0 | 54 | −14(20; 0.03) +(0.05) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 137 | 17 | comodidad | C | 9 | 2 (2) | 11 | walk | 19 | 3811 | 0.325 | [0.20, 0.60] | normal | — | -0.20 | 0.382 (comodidad) | 7.58 | 0.00 | 0 | 52 | −1(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 138 | 18 | reto | Ba+Li (D) | 9 | 1 (1) | 11 | fill | 26 | 3912 | 0.750 | [0.75, 0.90) | hard | hard | 0.66 | 0.883 (reto) | 3.19 | 0.01 | 0 | 67 | +(0.00) −8(23; 0.00) | relajacion: fill con 2 vacios (03 sec. 4.4) |
| 139 | 19 | remanso | S | 9 | 2 (2) | 11 | walk | 21 | 5082 | 0.200 | [0.20, 0.40] | normal | — | -0.27 | 0.325 (comodidad) | 6.33 | 0.00 | 0 | 56 | −7(20; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 140 | 20 | hito | Re+Li (MD) | 9 | 1 (1) | 10 | deep | 26 | 62097 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.31 | 0.976 (hito) | 4.08 | 0.22 | 3 | 58 | +(0.25) −6(22; 0.75) |  |
| 141 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 17 | 3587 | 0.050 | [0.00, 0.30) | normal | — | -0.68 | 0.047 (recuperacion) | 5.41 | 0.03 | 0 | 44 | −9(16; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 142 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1999 | 0.025 | [0.00, 0.30) | normal | — | -0.83 | 0.008 (recuperacion) | 5.31 | 0.02 | 0 | 42 | −14(15; 0.00) +(0.03) | 1 ronda(s) extra de candidatos |
| 143 | 3 | estreno | *Ll | 9 | 2 (1) | 11 | walk | 18 | 25202 | 0.075 | [0.00, 0.20) | normal | — | -0.51 | 0.145 (comodidad) | 7.33 | 0.00 | 0 | 50 | −1(17; 0.00) +(0.00) |  |
| 144 | 4 | estreno | *Ll | 9 | 2 (1) | 11 | walk | 18 | 5148 | 0.125 | [0.00, 0.20) | normal | — | -0.61 | 0.074 (recuperacion) | 5.72 | 0.00 | 0 | 52 | −3(17; 0.00) +(0.00) |  |
| 145 | 5 | estreno | *Ll | 9 | 2 (1) | 11 | walk | 19 | 7923 | 0.025 | [0.00, 0.20) | normal | — | -0.54 | 0.120 (comodidad) | 6.63 | 0.00 | 0 | 54 | −4(18; 0.00) +(0.00) |  |
| 146 | 6 | reto | Ba+Re (D) | 9 | 0 (0) | 10 | walk | 21 | 10608 | 0.750 | [0.75, 0.90) | hard | hard | 0.41 | 0.773 (reto) | 5.71 | 0.08 | 1 | 55 | +(0.15) −11(19; 0.03) | relajacion: walk con 1 vacio; 1 ronda(s) extra de candidatos |
| 147 | 7 | comodidad | Ll | 9 | 2 (1) | 11 | walk | 21 | 29029 | 0.475 | [0.20, 0.60] | normal | — | 0.24 | 0.691 (comodidad) | 6.33 | 0.07 | 0 | 56 | +(0.05) −2(20; 0.08) | 1 ronda(s) extra de candidatos |
| 148 | 8 | comodidad | C | 9 | 2 (2) | 11 | walk | 22 | 13134 | 0.375 | [0.20, 0.60] | normal | — | 0.11 | 0.618 (comodidad) | 7.14 | 0.00 | 0 | 54 | −5(20; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 149 | 9 | reto | Ll (D) | 9 | 2 (1) | 11 | walk | 20 | 14182 | 0.875 | [0.75, 0.90) | hard | hard | 0.44 | 0.798 (reto) | 5.60 | 0.00 | 0 | 56 | +(0.00) −6(19; 0.00) | relajacion: walk con 1 vacio; 1 ronda(s) extra de candidatos |
| 150 | 10 | comodidad | C | 9 | 2 (2) | 11 | walk | 22 | 6376 | 0.400 | [0.20, 0.60] | normal | — | 0.18 | 0.666 (comodidad) | 7.64 | 0.00 | 0 | 56 | −3(20; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 151 | 11 | comodidad | Li | 9 | 2 (2) | 11 | walk | 19 | 2148 | 0.450 | [0.20, 0.60] | normal | — | -0.24 | 0.350 (comodidad) | 4.05 | 0.12 | 0 | 50 | −2(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4) |
| 152 | 12 | reto | Ll+Ho (D) | 9 | 2 (0) | 11 | deep | 23 | 7002 | 0.875 | [0.75, 0.90) | hard | hard | 0.50 | 0.825 (reto) | 3.30 | 0.07 | 1 | 54 | +(0.30) −6(20; 0.40) | 2 ronda(s) extra de candidatos |
| 153 | 13 | comodidad | C | 9 | 2 (2) | 11 | walk | 21 | 8382 | 0.525 | [0.20, 0.60] | normal | — | -0.10 | 0.475 (comodidad) | 4.19 | 0.00 | 0 | 56 | −2(20; 0.00) +(0.08) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 154 | 14 | comodidad | Ma | 9 | 2 (1) | 11 | walk | 19 | 6639 | 0.325 | [0.20, 0.60] | normal | — | 0.10 | 0.612 (comodidad) | 9.58 | 0.00 | 0 | 52 | +(0.00) −3(18; 0.00) |  |
| 155 | 15 | reto | Ma+Tu (D) | 9 | 1 (1) | 11 | deep | 23 | 1394 | 0.850 | [0.75, 0.90) | hard | hard | 0.59 | 0.849 (reto) | 3.91 | 0.03 | 0 | 60 | +(0.03) −11(22; 0.20) | 1 ronda(s) extra de candidatos; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 156 | 16 | comodidad | C | 9 | 2 (2) | 11 | walk | 19 | 3264 | 0.350 | [0.20, 0.60] | normal | — | -0.32 | 0.290 (comodidad) | 6.05 | 0.00 | 0 | 52 | −2(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 157 | 17 | comodidad | C | 9 | 2 (2) | 11 | walk | 20 | 10747 | 0.300 | [0.20, 0.60] | normal | — | -0.17 | 0.402 (comodidad) | 6.90 | 0.02 | 0 | 50 | −2(19; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 158 | 18 | reto | Ll+Ba (D) | 9 | 2 (1) | 11 | walk | 21 | 29975 | 0.825 | [0.75, 0.90) | hard | hard | 0.67 | 0.887 (reto) | 7.05 | 0.03 | 0 | 55 | +(0.33) −6(18; 0.00) | relajacion: walk con 1 vacio; 1 ronda(s) extra de candidatos |
| 159 | 19 | remanso | S | 9 | 2 (2) | 11 | walk | 19 | 1439 | 0.350 | [0.20, 0.40] | normal | — | -0.38 | 0.238 (comodidad) | 5.00 | 0.03 | 0 | 52 | −2(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 160 | 20 | hito | Ll+Al6 (MD) | 9 | 1 (0) | 10 | deep | 23 | 3059 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.01 | 0.952 (hito) | 3.43 | 0.33 | 0 | 56 | +(0.20) −14(20; 0.43) | 1 ronda(s) extra de candidatos; 4 ronda(s) de banda |
| 161 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 2282 | 0.100 | [0.00, 0.30) | normal | — | -0.65 | 0.052 (recuperacion) | 6.63 | 0.00 | 0 | 44 | −8(15; 0.00) +(0.00) |  |
| 162 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 17 | 2226 | 0.125 | [0.00, 0.30) | normal | — | -0.71 | 0.031 (recuperacion) | 4.71 | 0.00 | 0 | 46 | −2(15; 0.00) +(0.00) |  |
| 163 | 3 | estreno | *Br | 9 | 2 (2) | 12 | walk | 19 | 15567 | 0.000 | [0.00, 0.20) | normal | — | -0.54 | 0.124 (comodidad) | 8.00 | 0.00 | 0 | 54 | −5(18; 0.00) +(0.00) |  |
| 164 | 4 | estreno | *Br | 9 | 2 (2) | 12 | walk | 18 | 1611 | 0.000 | [0.00, 0.20) | normal | — | -0.58 | 0.088 (recuperacion) | 8.56 | 0.00 | 0 | 55 | −2(16; 0.00) +(0.00) |  |
| 165 | 5 | estreno | *Br | 9 | 2 (2) | 12 | walk | 19 | 100 | 0.000 | [0.00, 0.20) | normal | — | -0.58 | 0.096 (recuperacion) | 7.58 | 0.00 | 0 | 56 | −2(18; 0.00) +(0.00) |  |
| 166 | 6 | reto | Ll+Re (D) | 9 | 2 (1) | 11 | walk | 19 | 30021 | 0.675 | [0.75, 0.90) | normal | hard | 0.22 | 0.686 (comodidad) | 6.32 | 0.03 | 0 | 50 | +(0.00) −6(18; 0.00) | relajacion: walk con 1 vacio; fuera de ventana (relajacion agotada); 1 ronda(s) extra de candidatos |
| 167 | 7 | comodidad | Br | 9 | 1 (1) | 11 | walk | 18 | 168 | 0.350 | [0.20, 0.60] | normal | — | -0.06 | 0.500 (comodidad) | 8.72 | 0.00 | 0 | 54 | +(0.00) −4(17; 0.00) |  |
| 168 | 8 | comodidad | C | 9 | 2 (2) | 11 | walk | 18 | 1779 | 0.450 | [0.20, 0.60] | normal | — | -0.20 | 0.375 (comodidad) | 7.06 | 0.00 | 0 | 50 | −2(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 169 | 9 | reto | Br (D) | 9 | 1 (1) | 11 | deep | 22 | 5498 | 0.850 | [0.75, 0.90) | hard | hard | 0.44 | 0.794 (reto) | 3.82 | 0.00 | 0 | 61 | +(0.08) −2(21; 0.00) | endurecimiento: deep con 60.000 nodos |
| 170 | 10 | comodidad | C | 9 | 2 (2) | 11 | walk | 19 | 1870 | 0.375 | [0.20, 0.60] | normal | — | -0.35 | 0.261 (comodidad) | 5.42 | 0.00 | 0 | 52 | −3(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 171 | 11 | comodidad | Ho | 9 | 2 (1) | 11 | walk | 20 | 3962 | 0.200 | [0.20, 0.60] | normal | — | -0.44 | 0.202 (comodidad) | 4.60 | 0.00 | 0 | 54 | −2(19; 0.00) | alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 172 | 12 | reto | Br+Ba (D) | 9 | 1 (1) | 11 | deep | 21 | 17653 | 0.800 | [0.75, 0.90) | hard | hard | 0.45 | 0.801 (reto) | 4.86 | 0.05 | 0 | 58 | +(0.05) −2(19; 0.15) |  |
| 173 | 13 | comodidad | C | 9 | 2 (2) | 11 | walk | 23 | 10382 | 0.300 | [0.20, 0.60] | normal | — | 0.09 | 0.607 (comodidad) | 6.39 | 0.03 | 0 | 58 | −2(21; 0.00) +(0.03) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 174 | 14 | comodidad | Ma | 9 | 2 (1) | 11 | walk | 19 | 7327 | 0.425 | [0.20, 0.60] | normal | — | -0.12 | 0.464 (comodidad) | 6.16 | 0.00 | 0 | 52 | +(0.00) −6(18; 0.00) |  |
| 175 | 15 | reto | Br+Tu (D) | 9 | 1 (1) | 11 | deep | 24 | 7342 | 0.800 | [0.75, 0.90) | hard | hard | 0.49 | 0.821 (reto) | 2.75 | 0.02 | 0 | 62 | +(0.00) −11(23; 0.00) | relajacion: turbias con el vacio del rol y la holgura; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 176 | 16 | comodidad | C | 9 | 1 (1) | 10 | walk | 18 | 400 | 0.575 | [0.20, 0.60] | normal | — | -0.33 | 0.281 (comodidad) | 2.94 | 0.02 | 0 | 48 | −6(17; 0.08) | alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 177 | 17 | comodidad | C | 9 | 1 (1) | 10 | walk | 18 | 403 | 0.525 | [0.20, 0.60] | normal | — | -0.08 | 0.486 (comodidad) | 3.44 | 0.21 | 0 | 48 | +(0.03) −7(17; 0.00) |  |
| 178 | 18 | reto | Br+Ma (D) | 9 | 1 (0) | 11 | deep | 21 | 8664 | 0.825 | [0.75, 0.90) | hard | hard | 0.50 | 0.826 (reto) | 4.76 | 0.15 | 0 | 58 | +(0.03) −4(20; 0.00) | endurecimiento: deep |
| 179 | 19 | remanso | S | 9 | 2 (2) | 11 | walk | 19 | 5447 | 0.275 | [0.20, 0.40] | normal | — | -0.19 | 0.391 (comodidad) | 7.42 | 0.06 | 0 | 52 | −6(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 180 | 20 | hito | Ma+Li (MD) | 9 | 1 (0) | 10 | fill | 26 | 150 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.17 | 0.968 (hito) | 2.12 | 0.33 | 2 | 66 | +(0.13) −2(23; 0.45) | 1 ronda(s) de banda |
| 181 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1274 | 0.000 | [0.00, 0.30) | normal | — | -0.78 | 0.015 (recuperacion) | 6.44 | 0.00 | 0 | 44 | −9(15; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 182 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 2121 | 0.000 | [0.00, 0.30) | normal | — | -0.98 | 0.001 (recuperacion) | 4.25 | 0.01 | 0 | 44 | −6(15; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 183 | 3 | estreno | *Pi | 9 | 2 (2) | 12 | walk | 19 | 28958 | 0.000 | [0.00, 0.20) | normal | — | -0.11 | 0.473 (comodidad) | 12.42 | 0.00 | 0 | 50 | −2(17; 0.00) +(0.00) |  |
| 184 | 4 | estreno | *Pi | 9 | 2 (2) | 12 | walk | 19 | 8303 | 0.050 | [0.00, 0.20) | normal | — | -0.18 | 0.395 (comodidad) | 11.05 | 0.00 | 0 | 52 | −6(18; 0.00) +(0.00) |  |
| 185 | 5 | estreno | *Pi | 9 | 2 (2) | 12 | walk | 22 | 177252 | 0.000 | [0.00, 0.20) | normal | — | -0.16 | 0.425 (comodidad) | 8.86 | 0.00 | 0 | 56 | −8(19; 0.00) +(0.00) |  |
| 186 | 6 | reto | Br+Ho (D) | 9 | 1 (0) | 11 | deep | 23 | 3240 | 0.750 | [0.75, 0.90) | hard | hard | 0.26 | 0.700 (reto) | 2.48 | 0.05 | 0 | 61 | +(0.05) −2(20; 0.00) | 1 ronda(s) extra de candidatos |
| 187 | 7 | comodidad | Pi | 9 | 1 (1) | 11 | walk | 18 | 8277 | 0.450 | [0.20, 0.60] | normal | — | 0.12 | 0.623 (comodidad) | 8.89 | 0.03 | 0 | 48 | +(0.00) −1(17; 0.00) | 1 ronda(s) extra de candidatos |
| 188 | 8 | comodidad | C | 9 | 1 (1) | 10 | walk | 19 | 437 | 0.575 | [0.20, 0.60] | normal | — | -0.21 | 0.370 (comodidad) | 2.58 | 0.06 | 0 | 48 | +(0.00) −2(17; 0.00) |  |
| 189 | 9 | reto | Pi (D) | 9 | 1 (1) | 11 | walk | 18 | 7393 | 0.725 | [0.75, 0.90) | normal | hard | 0.30 | 0.729 (reto) | 7.89 | 0.00 | 0 | 48 | +(0.10) −11(17; 0.00) | relajacion: walk con 1 vacio; fuera de ventana (relajacion agotada); 2 ronda(s) extra de candidatos |
| 190 | 10 | comodidad | C | 9 | 2 (2) | 11 | walk | 19 | 4815 | 0.275 | [0.20, 0.60] | normal | — | -0.26 | 0.334 (comodidad) | 7.58 | 0.00 | 0 | 52 | −2(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 191 | 11 | comodidad | Re | 9 | 1 (1) | 11 | walk | 18 | 31280 | 0.600 | [0.20, 0.60] | normal | — | 0.15 | 0.644 (comodidad) | 7.83 | 0.00 | 0 | 50 | +(0.05) −1(13; 0.00) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4) |
| 192 | 12 | reto | Pi+Ma (D) | 9 | 1 (0) | 11 | deep | 23 | 14036 | 0.775 | [0.75, 0.90) | hard | hard | 0.36 | 0.749 (reto) | 4.00 | 0.00 | 0 | 60 | +(0.00) −1(21; 0.00) | 1 ronda(s) extra de candidatos |
| 193 | 13 | comodidad | C | 9 | 2 (2) | 11 | walk | 20 | 3336 | 0.275 | [0.20, 0.60] | normal | — | -0.28 | 0.311 (comodidad) | 6.30 | 0.00 | 0 | 54 | −3(19; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 194 | 14 | comodidad | Ll | 9 | 2 (1) | 11 | walk | 19 | 4497 | 0.325 | [0.20, 0.60] | normal | — | 0.02 | 0.562 (comodidad) | 8.79 | 0.00 | 0 | 54 | +(0.00) −7(18; 0.00) |  |
| 195 | 15 | reto | Ba+Tu (D) | 9 | 1 (1) | 11 | deep | 23 | 13881 | 0.850 | [0.75, 0.90) | hard | hard | 0.83 | 0.928 (reto) | 5.22 | 0.12 | 0 | 64 | +(0.00) −14(21; 0.00) | relajacion: turbias con el vacio del rol y la holgura; 1 ronda(s) extra de candidatos; turbia: 3 dosis ocultas en 2 cuia(s), 6 realidades |
| 196 | 16 | comodidad | C | 9 | 2 (2) | 11 | walk | 18 | 2247 | 0.325 | [0.20, 0.60] | normal | — | -0.32 | 0.284 (comodidad) | 7.06 | 0.02 | 0 | 50 | −6(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 197 | 17 | comodidad | C | 9 | 2 (2) | 11 | walk | 20 | 2538 | 0.375 | [0.20, 0.60] | normal | — | -0.19 | 0.393 (comodidad) | 6.10 | 0.00 | 0 | 54 | −1(18; 0.00) +(0.03) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 198 | 18 | reto | Pi+Ho (D) | 9 | 1 (0) | 11 | fill | 28 | 30499 | 0.875 | [0.75, 0.90) | hard | hard | 0.94 | 0.941 (reto) | 3.14 | 0.05 | 0 | 66 | +(0.00) −6(22; 0.00) | 1 ronda(s) extra de candidatos |
| 199 | 19 | remanso | S | 9 | 2 (2) | 11 | walk | 19 | 5968 | 0.300 | [0.20, 0.40] | normal | — | -0.39 | 0.227 (comodidad) | 5.89 | 0.00 | 0 | 50 | −2(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 200 | 20 | hito | Pi+Ba (MD) | 9 | 0 (0) | 11 | fill | 28 | 39689 | 0.975 | [0.90, 1.00] | very_hard | very_hard | 1.17 | 0.965 (hito) | 4.43 | 0.03 | 0 | 69 | +(0.03) | 1 ronda(s) extra de candidatos |

## Par / 2K por fuente frente a 03 §18

| Fuente | Niveles | Par / 2K medio | bot_fail medio | 03 §18 (par / 2K) |
|---|---|---|---|---|
| deep | 21 | 1.26 | 0.84 | 1,17 |
| fill | 16 | 1.57 | 0.77 | 1,53 |
| walk | 153 | 1.09 | 0.31 | 1,09 |

## Debuts

| Nivel | intro_mechanic | skippable | Celda |
|---|---|---|---|
| 14 | `chica` | false | *Ch |
| 27 | `alta` | false | *Al |
| 43 | `hoja` | false | *Ho |
| 63 | `matapi` | false | *Ma |
| 83 | `liana` | false | *Li |
| 100 | `murky` | false | *Tu (MD) |
| 103 | `remolino` | false | *Re |
| 123 | `barro` | false | *Ba |
| 143 | `lluvia` | false | *Ll |
| 163 | `brote` | false | *Br |
| 183 | `pirana` | false | *Pi |

## Apariciones en los packs (debut real y huecos)

Sobre los tableros publicados, no sobre el calendario: primera aparición (debut real), niveles en los que sale, hueco máximo entre dos apariciones y niveles desde la última hasta el 200 (resumen, punto 6; d.2 regla 4: ≤ 120; level-qa lo exige con QA-GAP-120). Las capacidades cuentan también cuando el generador las usa como forma de tablero (02 §8); la cueva de la Piraña no cuenta como Cuia chica.

| Mecánica o capacidad | Debut del calendario | Debut real | Niveles | Hueco máximo | Hasta el final |
|---|---|---|---|---|---|
| hoja (Ho) | 43 | 43 | 22 | 19 (152→171) | 2 |
| matapi (Ma) | 63 | 63 | 21 | 22 (132→154) | 8 |
| liana (Li) | 83 | 83 | 14 | 29 (151→180) | 20 |
| murky (Tu) | 100 | 100 | 7 | 20 (115→135) | 5 |
| remolino (Re) | 103 | 103 | 13 | 25 (166→191) | 9 |
| barro (Ba) | 123 | 123 | 13 | 23 (172→195) | 0 |
| lluvia (Ll) | 143 | 143 | 10 | 28 (166→194) | 6 |
| brote (Br) | 163 | 163 | 9 | 8 (178→186) | 14 |
| pirana (Pi) | 183 | 183 | 8 | 6 (192→198) | 0 |
| Cuia chica (cap 1-3) | 14 | 14 | 21 | 77 (114→191) | 9 |
| Cuia alta (cap 5-6) | 27 | 27 | 14 | 65 (95→160) | 40 |
| Ch1 (cap 1) | 18 | 18 | 1 | — | 182 |
| Ch3 (cap 3) | 22 | 22 | 1 | — | 178 |
| Al6 (cap 6) | 160 | 160 | 1 | — | 40 |

## Remanso frente a la comodidad del ciclo (c.4)

p19 es «puro, más fácil que la media de comodidad» (c.4; c.3 regla 5: respiro antes del pico): la colocación exige D(p19) < media de D de las comodidades del ciclo que no son tableros de estreno, y level-qa lo comprueba (QA-REMANSO).

| Ciclo | Remanso | D | bot_fail | Par | Comodidades | D medio | bot_fail medio | Puesto del remanso por D (de las comodidades y él) |
|---|---|---|---|---|---|---|---|---|
| 21-40 | 39 | -0.13 | 0.225 | 17 | 9 | -0.10 | 0.372 | 5 de 10 (1 = el más difícil) |
| 41-60 | 59 | -0.28 | 0.350 | 15 | 8 | -0.25 | 0.384 | 5 de 9 (1 = el más difícil) |
| 61-80 | 79 | -0.24 | 0.225 | 18 | 8 | -0.10 | 0.409 | 7 de 9 (1 = el más difícil) |
| 81-100 | 99 | -0.38 | 0.350 | 19 | 8 | -0.21 | 0.444 | 8 de 9 (1 = el más difícil) |
| 101-120 | 119 | -0.24 | 0.250 | 19 | 8 | -0.13 | 0.366 | 7 de 9 (1 = el más difícil) |
| 121-140 | 139 | -0.27 | 0.200 | 21 | 8 | -0.24 | 0.359 | 6 de 9 (1 = el más difícil) |
| 141-160 | 159 | -0.38 | 0.350 | 19 | 8 | -0.03 | 0.400 | 9 de 9 (1 = el más difícil) |
| 161-180 | 179 | -0.19 | 0.275 | 19 | 8 | -0.19 | 0.400 | 5 de 9 (1 = el más difícil) |
| 181-200 | 199 | -0.39 | 0.300 | 19 | 8 | -0.12 | 0.400 | 9 de 9 (1 = el más difícil) |

## Banda medida por rol (03 §7: «la banda coloca»)

La colocación exige, con el cuantil q de D en el pool del rango (recuperación ≤ 0,10, comodidad ≤ 0,70, reto ≤ 0,95, hito > 0,95): comodidad, estreno y remanso como mucho en banda de comodidad; recuperación (p1-p2, c.3 regla 3) en banda de recuperación o, como mínimo, con D por debajo de la comodidad más fácil del ciclo; hito (p20, «Pico» de c.3) en banda de hito o, como mínimo, con par − 2K ≥ la media del pool del rango; y en 21-40 los retos suaves por debajo del hito 40. level-qa lo comprueba regenerando la campaña (QA-BAND-Q y QA-LADDER).

| Ciclo | Recuperación p1 / p2: q (D) | Comodidad más fácil (D) | q máximo de comodidad y estreno | Remanso: q | Retos: D mín.-máx. | Hito: q (banda) · par − 2K / media del pool |
|---|---|---|---|---|---|---|
| 21-40 | 0.007 (-0.77) / 0.267 (-0.34) | -0.30 | 0.700 | 0.409 | 0.58 / 0.94 | 0.962 (hito) · 11 / 3.7 |
| 41-60 | 0.087 (-0.56) / 0.045 (-0.80) | -0.53 | 0.591 | 0.297 | 0.07 / 0.80 | 0.976 (hito) · 9 / 3.7 |
| 61-80 | 0.044 (-0.75) / 0.024 (-0.78) | -0.38 | 0.642 | 0.334 | 0.22 / 0.80 | 0.962 (hito) · 12 / 3.0 |
| 81-100 | 0.080 (-0.67) / 0.009 (-0.92) | -0.41 | 0.547 | 0.204 | 0.37 / 0.72 | 0.899 (reto) · 4 / 3.0 |
| 101-120 | 0.063 (-0.64) / 0.024 (-0.73) | -0.58 | 0.685 | 0.347 | 0.30 / 0.98 | 0.962 (hito) · 4 / 2.4 |
| 121-140 | 0.079 (-0.60) / 0.006 (-0.83) | -0.54 | 0.625 | 0.325 | 0.27 / 0.66 | 0.976 (hito) · 8 / 2.4 |
| 141-160 | 0.047 (-0.68) / 0.008 (-0.83) | -0.32 | 0.691 | 0.238 | 0.41 / 0.67 | 0.952 (hito) · 5 / 2.4 |
| 161-180 | 0.052 (-0.65) / 0.031 (-0.71) | -0.44 | 0.607 | 0.391 | 0.22 / 0.50 | 0.968 (hito) · 8 / 2.4 |
| 181-200 | 0.015 (-0.78) / 0.001 (-0.98) | -0.32 | 0.644 | 0.227 | 0.26 / 0.94 | 0.965 (hito) · 10 / 2.4 |

Fuera de la banda de su rol, con el mínimo cumplido (aviso QA-BAND-Q): 22 (recuperacion, q 0.267); 100 (hito, q 0.899).

## Vacíos de la comodidad y del remanso por rango (03 §4.4 y §11)

Comodidades sin estreno y remansos. «2 vacíos» = dos o más vacíos libres (sin mecánica, capacidad ≥ 4); «Cuia chica» = Cuia chica de 2 añadida como medio vacío por la relajación de 03 §4.4. Canon: 2 vacíos en 11-30; 1 desde el 31 (03 §11: «2 en respiros y recuperación» en 31-60, «2 en 30 % de comodidad» en 61-100 y 1 en 101-200; 03 §4.4: «1 (31+) con 2 en el 30 % de los slots») [P: se revisa con el tech-lead].

| Rango | Comodidades y remansos | Con 2 vacíos | Con Cuia chica de relajación | Relajados (%) | Canon |
|---|---|---|---|---|---|
| 11-30 | 4 | 3 | 0 | 75 % | 2 |
| 31-60 | 15 | 3 | 0 | 20 % | 1 (2 en respiros) |
| 61-100 | 18 | 8 | 1 | 50 % | 1 (2 en el 30 %) |
| 101-200 | 45 | 27 | 2 | 64 % | 1 |

## Cuias por rango frente a 03 §11

Con 1 vacío, el K del rango que permiten los pares de riesgo (03 §4.5; `k_plan`) no siempre llega al mínimo de cuias de 03 §11 (aviso QA-CUIAS de level-qa) [P: k_plan, vacíos o la tabla se revisan en la integración].

| Rango | 03 §11 | Mínimo y máximo generados | Por debajo | Por encima | Niveles por debajo |
|---|---|---|---|---|---|
| 11-30 | 8-10 | 8-9 | 0 | 0 | — |
| 31-60 | 9-11 | 8-10 | 12 | 0 | 31 (8), 36 (8), 37 (8), 41 (8), 42 (8), 48 (8), 51 (8), 53 (8), 54 (8), 55 (8), 56 (8), 59 (8) |
| 61-100 | 10-12 | 9-11 | 10 | 0 | 61 (9), 62 (9), 73 (9), 76 (9), 81 (9), 82 (9), 88 (9), 90 (9), 94 (9), 96 (9) |
| 101-200 | 10-13 | 10-12 | 0 | 0 | — |

## Saltos de banda (03 §12: +2,2 / −3,4)

Ninguno fuera del límite.
