# Campaña 11-200: informe de `level-generator --spec`

Generado por `dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/campaign_v1.json --levels 11-200 --out Assets/_Project/Data/Levels --report tools/out/campaign_v1_report.md`: no se edita a mano y no lleva tiempos. Generador 1.0.0, `pack_schema` 2, modo de vertido `run` (D-005).

Fuentes: D-009 (ciclo escalonado) y D-007 (debuts) con la propuesta `docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md` (c.1-c.4, d.1-d.4); GDD 03 §2, §4, §7-§13 y §17; 02 §8-§11; 08 §4.1. Todo se mide con `AF.Core` y `AF.Solver`: A* w = 1 a 300.000 (par), bot ×40 con la semilla del nivel (`bot_fail` y etiqueta), `EarlyTraps` (trampas tempranas, Q16 B), ruta de referencia para ramificación y trampas (`--parity`), `SafePath` en Aguas turbias y `LevelValidation` en cada tablero.

## Paquetes

| Pack | Niveles | Bytes | Bytes sin relief | Bytes sin relief por 100 niveles (≤ 100 KB, 03 §17 #11) | SHA-256 |
|---|---|---|---|---|---|
| `Assets/_Project/Data/Levels/mundo_02_v1.json` | 30 | 26140 | 22490 | 73.2 KB | `0c199fc2eed29bdc3cceac5908d8b707657d29db0f95f64a217f918363640e40` |
| `Assets/_Project/Data/Levels/mundo_03_v1.json` | 40 | 48468 | 31165 | 76.1 KB | `f62dfa95b818c724ba3cbf6fe4d866b61a6e4850459e1e6b8b760f9f05588cc6` |
| `Assets/_Project/Data/Levels/mundo_04_v1.json` | 60 | 78475 | 49330 | 80.3 KB | `47b9b946fe9828262fd6ec433cae97356c466f20a6b3ef6478270ff4e26b24aa` |
| `Assets/_Project/Data/Levels/mundo_05_v1.json` | 60 | 80052 | 49793 | 81.0 KB | `92deb69da96be44d2672a2d2f52e53566f3c560cd8523b8aced64d3a46471820` |

## Pool por rango e índice D (03 §7)

D = 0,35·z(bot_fail) + 0,25·z(par − 2K) + 0,20·z(ramificación) + 0,10·z(trampas) + 0,10·[E libres = 1], con media y desviación del pool del rango (todos los candidatos en ventana de sus slots; congelados tras la primera pasada). Banda = cuantil de D en el pool: recuperación ≤ 0,10, comodidad ≤ 0,70, reto ≤ 0,95, hito > 0,95.

| Rango | Slots | Pool | Pool / slots | bot_fail μ ± σ | par − 2K μ ± σ | Ramif. μ ± σ | Trampas μ ± σ |
|---|---|---|---|---|---|---|---|
| 11-30 | 20 | 75 | 3.8 | 0.21 ± 0.23 | 2.7 ± 2.7 | 5.22 ± 1.01 | 0.00 ± 0.01 |
| 31-60 | 30 | 147 | 4.9 | 0.48 ± 0.28 | 3.9 ± 3.4 | 4.29 ± 1.47 | 0.04 ± 0.07 |
| 61-100 | 40 | 173 | 4.3 | 0.45 ± 0.30 | 3.2 ± 3.4 | 5.08 ± 1.64 | 0.04 ± 0.07 |
| 101-200 | 100 | 440 | 4.4 | 0.46 ± 0.30 | 2.5 ± 2.7 | 5.98 ± 1.99 | 0.04 ± 0.08 |

## Etiquetas por rango frente a 03 §11

| Rango (niveles generados) | Difícil | Muy difícil | 03 §11 | Aviso (> 5 puntos) |
|---|---|---|---|---|
| 11-30 (20) | 0 (0.0 %) | 0 (0.0 %) | 0-0 % / 0 % | — |
| 31-60 (30) | 4 (13.3 %) | 1 (3.3 %) | 10-13 % / 3 % | — |
| 61-100 (40) | 8 (20.0 %) | 2 (5.0 %) | 15-15 % / 5 % | — |
| 101-200 (100) | 24 (24.0 %) | 5 (5.0 %) | 15-20 % / 5 % | — |

Nota: con D-009 desde el 61 los cinco retos apuntan a Difícil y el hito a Muy difícil (25 % y 5 % por ciclo), por encima del 15 % de 03 §11 para 61-100: es la cuenta del ciclo, no un error de la generación [P: la tabla de 03 §11 se revisa en la integración].

## Ciclos

| Ciclo | Temporada | Puros | Difícil | Muy difícil | Objetivo D / MD | Acierto de etiqueta (c.7) | Trampas tempranas | Relajaciones |
|---|---|---|---|---|---|---|---|---|
| 11-20 (rampa) | B | 8/10 | 0 | 0 | 0 / 0 | — | 0 | 0 |
| 21-40 | B | 8/20 | 1 | 0 | 1 / 0 | 1/1 (100 %) | 1 | 3 |
| 41-60 | A | 8/20 | 3 | 1 | 3 / 1 | 4/4 (100 %) | 2 | 2 |
| 61-80 | B | 8/20 | 3 | 1 | 5 / 1 | 4/6 (67 %) | 0 | 9 |
| 81-100 | A | 8/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 3 | 7 |
| 101-120 | B | 8/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 5 | 10 |
| 121-140 | A | 8/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 5 | 9 |
| 141-160 | A | 8/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 2 | 9 |
| 161-180 | B | 8/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 3 | 8 |
| 181-200 | B | 8/20 | 4 | 1 | 5 / 1 | 5/6 (83 %) | 5 | 8 |

## Niveles

Columnas: p = posición en el ciclo (0 en la rampa); E = vacíos de capacidad ≥ 4 (con la Hoja o el Matapi vacíos), libres = sin mecánica; q = cuantil de D en el pool del rango y su banda; TT = trampas tempranas; alivio = tipos publicados en `relief` en orden (− = minus_color, + = plus_empty).

| Nivel | p | Rol | Calendario | K | E (libres) | Cuias | Fuente | Par | Estados A* | bot_fail | Ventana | Etiqueta | Objetivo | D | q (banda) | Ramif. | Trampas | TT | Compl. | Alivio | Notas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 11 | 0 | rampa | C | 6 | 2 (2) | 8 | walk | 14 | 625 | 0.100 | [0.00, 0.20) | normal | — | -0.20 | 0.46 (comodidad) | 5.50 | 0.00 | 0 | 36 | — |  |
| 12 | 0 | rampa | C | 6 | 2 (2) | 8 | walk | 14 | 1245 | 0.025 | [0.00, 0.20) | normal | — | -0.34 | 0.30 (comodidad) | 5.36 | 0.00 | 0 | 36 | — |  |
| 13 | 0 | rampa | C | 6 | 2 (2) | 8 | walk | 15 | 1150 | 0.025 | [0.00, 0.20) | normal | — | -0.37 | 0.27 (comodidad) | 4.73 | 0.00 | 0 | 38 | — |  |
| 14 | 0 | rampa | *Ch | 6 | 1 (1) | 8 | walk | 13 | 933 | 0.125 | [0.00, 0.20) | normal | — | -0.07 | 0.54 (comodidad) | 5.92 | 0.00 | 0 | 36 | — |  |
| 15 | 0 | rampa | C | 6 | 2 (2) | 8 | walk | 16 | 1571 | 0.175 | [0.00, 0.20) | normal | — | 0.21 | 0.69 (comodidad) | 6.06 | 0.00 | 0 | 40 | — |  |
| 16 | 0 | rampa | C | 6 | 2 (2) | 8 | walk | 14 | 316 | 0.050 | [0.00, 0.20) | normal | — | -0.50 | 0.15 (comodidad) | 4.36 | 0.00 | 0 | 36 | — |  |
| 17 | 0 | rampa | C | 6 | 2 (2) | 8 | walk | 15 | 523 | 0.075 | [0.00, 0.20) | normal | — | -0.06 | 0.55 (comodidad) | 5.93 | 0.00 | 0 | 40 | — |  |
| 18 | 0 | rampa | *Ch1 | 6 | 1 (1) | 8 | walk | 12 | 86 | 0.000 | [0.00, 0.20) | normal | — | -0.31 | 0.33 (comodidad) | 5.67 | 0.01 | 0 | 34 | — |  |
| 19 | 0 | rampa | C | 6 | 2 (2) | 8 | walk | 13 | 638 | 0.000 | [0.00, 0.20) | normal | — | -0.79 | 0.02 (recuperacion) | 3.77 | 0.00 | 0 | 34 | — |  |
| 20 | 0 | rampa | C | 6 | 2 (2) | 8 | walk | 12 | 154 | 0.175 | [0.00, 0.20) | normal | — | -0.39 | 0.25 (comodidad) | 4.92 | 0.00 | 0 | 34 | — |  |
| 21 | 1 | recuperacion | R | 6 | 2 (2) | 8 | walk | 14 | 853 | 0.175 | [0.00, 0.30) | normal | — | -0.01 | 0.62 (comodidad) | 5.86 | 0.00 | 0 | 36 | — |  |
| 22 | 2 | recuperacion | *Ch3 | 6 | 1 (1) | 8 | walk | 17 | 3442 | 0.275 | [0.00, 0.30) | normal | — | 0.56 | 0.78 (reto) | 6.06 | 0.00 | 0 | 40 | — |  |
| 23 | 3 | comodidad | Ch | 7 | 1 (1) | 9 | walk | 15 | 1610 | 0.250 | [0.20, 0.60] | normal | — | -0.29 | 0.34 (comodidad) | 3.87 | 0.00 | 0 | 40 | — |  |
| 24 | 4 | comodidad | C | 7 | 2 (2) | 9 | walk | 16 | 1095 | 0.375 | [0.20, 0.60] | normal | — | 0.33 | 0.73 (reto) | 6.06 | 0.00 | 0 | 42 | — |  |
| 25 | 5 | comodidad | C | 7 | 2 (2) | 9 | walk | 15 | 973 | 0.275 | [0.20, 0.60] | normal | — | -0.26 | 0.41 (comodidad) | 4.33 | 0.00 | 0 | 40 | — |  |
| 26 | 6 | reto | Ch (s) | 7 | 1 (1) | 9 | fill | 23 | 6735 | 0.575 | [0.50, 0.70] | normal | — | 1.28 | 0.98 (hito) | 4.48 | 0.03 | 0 | 52 | — |  |
| 27 | 7 | estreno | *Al | 7 | 2 (2) | 9 | walk | 15 | 1059 | 0.150 | [0.00, 0.20) | normal | — | -0.04 | 0.57 (comodidad) | 4.20 | 0.06 | 0 | 40 | — |  |
| 28 | 8 | estreno | *Al | 7 | 2 (2) | 9 | walk | 16 | 1356 | 0.075 | [0.00, 0.20) | normal | — | -0.04 | 0.58 (comodidad) | 6.50 | 0.00 | 0 | 42 | — |  |
| 29 | 9 | reto | Ch (s) | 7 | 1 (1) | 9 | fill | 23 | 6662 | 0.650 | [0.50, 0.70] | normal | — | 0.86 | 0.87 (reto) | 2.83 | 0.00 | 0 | 54 | — |  |
| 30 | 10 | comodidad | C | 7 | 2 (2) | 9 | walk | 15 | 920 | 0.250 | [0.20, 0.60] | normal | — | 0.26 | 0.70 (comodidad) | 6.00 | 0.03 | 0 | 40 | — |  |
| 31 | 11 | comodidad | C | 7 | 1 (1) | 8 | walk | 15 | 128 | 0.275 | [0.20, 0.60] | normal | — | -0.32 | 0.28 (comodidad) | 2.27 | 0.26 | 0 | 38 | +(0.00) −1(14; 0.00) |  |
| 32 | 12 | reto | Al (s) | 7 | 2 (2) | 9 | fill | 25 | 7842 | 0.650 | [0.50, 0.70] | normal | — | 0.48 | 0.74 (reto) | 2.72 | 0.01 | 0 | 56 | −3(21; 0.00) +(0.00) |  |
| 33 | 13 | comodidad | Ch | 7 | 1 (1) | 9 | walk | 14 | 97 | 0.300 | [0.20, 0.60] | normal | — | -0.11 | 0.45 (comodidad) | 6.93 | 0.00 | 0 | 40 | +(0.03) −3(13; 0.00) |  |
| 34 | 14 | comodidad | C | 7 | 2 (2) | 9 | walk | 16 | 1021 | 0.225 | [0.20, 0.60] | normal | — | -0.54 | 0.11 (comodidad) | 3.50 | 0.05 | 0 | 40 | −8(14; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 35 | 15 | reto | Ch+Al (s) | 7 | 1 (1) | 9 | fill | 24 | 6135 | 0.600 | [0.50, 0.70] | normal | — | 0.57 | 0.79 (reto) | 3.38 | 0.04 | 0 | 54 | +(0.00) −10(21; 0.00) | relajacion: fill con 1 vacio |
| 36 | 16 | comodidad | C | 7 | 1 (1) | 8 | walk | 14 | 103 | 0.550 | [0.20, 0.60] | normal | — | -0.35 | 0.26 (comodidad) | 2.50 | 0.03 | 0 | 40 | +(0.00) −4(13; 0.00) |  |
| 37 | 17 | comodidad | Al | 7 | 1 (1) | 8 | walk | 14 | 158 | 0.600 | [0.20, 0.60] | normal | — | -0.14 | 0.43 (comodidad) | 2.36 | 0.15 | 0 | 38 | +(0.05) −3(13; 0.05) |  |
| 38 | 18 | reto | Ch (s) | 7 | 1 (1) | 9 | fill | 24 | 6797 | 0.625 | [0.50, 0.70] | normal | — | 0.68 | 0.85 (reto) | 4.00 | 0.03 | 0 | 56 | +(0.00) −4(20; 0.00) |  |
| 39 | 19 | remanso | S | 7 | 2 (2) | 9 | walk | 15 | 1042 | 0.325 | [0.20, 0.40] | normal | — | -0.54 | 0.12 (comodidad) | 3.73 | 0.00 | 0 | 38 | −10(14; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 40 | 20 | hito | Ch+Al (D) | 7 | 1 (1) | 9 | fill | 25 | 6898 | 0.825 | [0.75, 0.85] | hard | hard | 0.93 | 0.92 (reto) | 3.32 | 0.05 | 1 | 52 | +(0.03) −4(19; 0.00) |  |
| 41 | 1 | recuperacion | R | 6 | 2 (2) | 8 | walk | 14 | 792 | 0.050 | [0.00, 0.30) | normal | — | -0.55 | 0.11 (comodidad) | 5.64 | 0.00 | 0 | 36 | −1(12; 0.00) +(0.00) |  |
| 42 | 2 | recuperacion | R | 6 | 2 (2) | 8 | walk | 13 | 421 | 0.000 | [0.00, 0.30) | normal | — | -0.78 | 0.05 (recuperacion) | 4.92 | 0.00 | 0 | 34 | −1(11; 0.00) +(0.00) |  |
| 43 | 3 | estreno | *Ho | 7 | 2 (1) | 9 | walk | 15 | 648 | 0.000 | [0.00, 0.20) | normal | — | -0.83 | 0.03 (recuperacion) | 3.80 | 0.00 | 0 | 40 | −8(14; 0.00) +(0.00) |  |
| 44 | 4 | estreno | *Ho | 7 | 2 (1) | 9 | walk | 16 | 383 | 0.100 | [0.00, 0.20) | normal | — | -0.77 | 0.06 (recuperacion) | 2.81 | 0.00 | 0 | 40 | −3(14; 0.00) +(0.00) |  |
| 45 | 5 | estreno | *Ho | 7 | 2 (1) | 9 | walk | 18 | 1931 | 0.125 | [0.00, 0.20) | normal | — | -0.47 | 0.16 (comodidad) | 3.72 | 0.00 | 0 | 44 | −7(16; 0.00) +(0.00) |  |
| 46 | 6 | reto | Ch (D) | 7 | 1 (1) | 9 | deep | 18 | 1642 | 0.775 | [0.75, 1.00] | hard | hard | 0.57 | 0.79 (reto) | 4.94 | 0.04 | 0 | 46 | +(0.08) −5(17; 0.03) |  |
| 47 | 7 | comodidad | Ho | 7 | 2 (1) | 9 | walk | 17 | 893 | 0.225 | [0.20, 0.60] | normal | — | -0.39 | 0.21 (comodidad) | 3.88 | 0.00 | 0 | 44 | +(0.00) −5(15; 0.00) |  |
| 48 | 8 | comodidad | C | 7 | 1 (1) | 8 | walk | 15 | 115 | 0.575 | [0.20, 0.60] | normal | — | -0.24 | 0.35 (comodidad) | 2.60 | 0.03 | 0 | 40 | +(0.13) −1(14; 0.30) |  |
| 49 | 9 | reto | Ho (s) | 7 | 3 (2) | 10 | fill | 21 | 7628 | 0.550 | [0.50, 0.75) | normal | — | 0.39 | 0.71 (reto) | 5.29 | 0.00 | 0 | 50 | −1(17; 0.00) +(0.00) | relajacion: fill con 2 vacios (03 sec. 4.4) |
| 50 | 10 | comodidad | C | 7 | 2 (2) | 9 | walk | 16 | 2017 | 0.275 | [0.20, 0.60] | normal | — | -0.14 | 0.42 (comodidad) | 6.56 | 0.00 | 0 | 42 | −4(15; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 51 | 11 | comodidad | Al | 7 | 1 (1) | 8 | walk | 14 | 121 | 0.575 | [0.20, 0.60] | normal | — | -0.18 | 0.38 (comodidad) | 2.79 | 0.10 | 0 | 38 | +(0.00) −3(13; 0.00) |  |
| 52 | 12 | reto | Ho+Ch (D) | 7 | 1 (0) | 9 | fill | 21 | 3349 | 0.775 | [0.75, 1.00] | hard | hard | 0.64 | 0.83 (reto) | 4.48 | 0.05 | 0 | 48 | +(0.03) −5(19; 0.00) |  |
| 53 | 13 | comodidad | C | 7 | 1 (1) | 8 | walk | 15 | 181 | 0.200 | [0.20, 0.60] | normal | — | -0.56 | 0.09 (recuperacion) | 2.67 | 0.13 | 0 | 38 | +(0.00) −1(13; 0.00) |  |
| 54 | 14 | comodidad | C | 7 | 1 (1) | 8 | walk | 15 | 161 | 0.450 | [0.20, 0.60] | normal | — | -0.37 | 0.23 (comodidad) | 3.07 | 0.00 | 0 | 42 | +(0.00) −1(14; 0.00) |  |
| 55 | 15 | reto | Al (s) | 7 | 1 (1) | 8 | deep | 16 | 148 | 0.700 | [0.50, 0.75) | normal | — | -0.02 | 0.52 (comodidad) | 2.19 | 0.06 | 0 | 44 | +(0.13) −4(14; 0.00) |  |
| 56 | 16 | comodidad | C | 7 | 1 (1) | 8 | walk | 14 | 142 | 0.325 | [0.20, 0.60] | normal | — | -0.56 | 0.08 (recuperacion) | 2.79 | 0.05 | 0 | 38 | +(0.00) −3(13; 0.00) |  |
| 57 | 17 | comodidad | Ch | 7 | 1 (1) | 9 | walk | 15 | 1651 | 0.450 | [0.20, 0.60] | normal | — | 0.10 | 0.59 (comodidad) | 6.53 | 0.00 | 0 | 40 | +(0.00) −7(14; 0.00) |  |
| 58 | 18 | reto | Ho+Al (D) | 7 | 1 (0) | 8 | deep | 19 | 112 | 1.000 | [0.75, 1.00] | hard | hard | 0.88 | 0.90 (reto) | 2.42 | 0.33 | 0 | 46 | +(0.10) −7(18; 0.28) |  |
| 59 | 19 | remanso | S | 7 | 1 (1) | 8 | walk | 15 | 178 | 0.350 | [0.20, 0.40] | normal | — | -0.35 | 0.26 (comodidad) | 2.93 | 0.11 | 0 | 40 | +(0.03) −1(14; 0.05) |  |
| 60 | 20 | hito | Ho+Ch (MD) | 7 | 1 (0) | 9 | fill | 23 | 2239 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 0.98 | 0.94 (reto) | 3.91 | 0.04 | 2 | 52 | +(0.00) −4(20; 0.00) |  |
| 61 | 1 | recuperacion | R | 7 | 2 (2) | 9 | walk | 16 | 2155 | 0.175 | [0.00, 0.30) | normal | — | -0.25 | 0.33 (comodidad) | 6.94 | 0.00 | 0 | 42 | −1(15; 0.00) +(0.00) |  |
| 62 | 2 | recuperacion | R | 7 | 2 (2) | 9 | walk | 16 | 792 | 0.050 | [0.00, 0.30) | normal | — | -0.53 | 0.14 (comodidad) | 5.88 | 0.00 | 0 | 42 | −7(15; 0.00) +(0.00) |  |
| 63 | 3 | estreno | *Ma | 8 | 2 (1) | 10 | walk | 16 | 6902 | 0.025 | [0.00, 0.20) | normal | — | -0.40 | 0.17 (comodidad) | 7.56 | 0.00 | 0 | 42 | −7(15; 0.00) +(0.00) |  |
| 64 | 4 | estreno | *Ma | 8 | 2 (1) | 10 | walk | 16 | 2717 | 0.000 | [0.00, 0.20) | normal | — | -0.62 | 0.10 (recuperacion) | 6.06 | 0.00 | 0 | 44 | −1(15; 0.00) +(0.00) |  |
| 65 | 5 | estreno | *Ma | 8 | 2 (1) | 10 | walk | 16 | 5758 | 0.000 | [0.00, 0.20) | normal | — | -0.58 | 0.11 (comodidad) | 6.38 | 0.00 | 0 | 44 | −1(14; 0.00) +(0.00) |  |
| 66 | 6 | reto | Ho+Ch (D) | 8 | 1 (0) | 10 | deep | 20 | 682 | 0.725 | [0.75, 0.90) | normal | hard | 0.19 | 0.66 (comodidad) | 2.80 | 0.11 | 0 | 48 | +(0.03) −1(17; 0.00) | fuera de ventana (relajacion agotada); 1 ronda(s) extra de candidatos |
| 67 | 7 | comodidad | Ma | 8 | 2 (1) | 10 | walk | 19 | 4661 | 0.425 | [0.20, 0.60] | normal | — | 0.30 | 0.69 (comodidad) | 7.53 | 0.01 | 0 | 50 | +(0.18) −5(18; 0.00) |  |
| 68 | 8 | comodidad | C | 8 | 2 (2) | 10 | walk | 18 | 1501 | 0.375 | [0.20, 0.60] | normal | — | -0.10 | 0.48 (comodidad) | 5.44 | 0.07 | 0 | 46 | −3(16; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 69 | 9 | reto | Ma (D) | 8 | 2 (1) | 10 | walk | 17 | 3367 | 0.825 | [0.75, 0.90) | hard | hard | 0.44 | 0.75 (reto) | 6.12 | 0.00 | 0 | 46 | +(0.00) −3(16; 0.00) | relajacion: walk con 1 vacio |
| 70 | 10 | comodidad | C | 8 | 2 (2) | 10 | walk | 17 | 2864 | 0.400 | [0.20, 0.60] | normal | — | -0.18 | 0.41 (comodidad) | 6.00 | 0.00 | 0 | 46 | −6(16; 0.00) +(0.03) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 71 | 11 | comodidad | Al | 8 | 1 (1) | 10 | walk | 17 | 1504 | 0.300 | [0.20, 0.60] | normal | — | -0.07 | 0.52 (comodidad) | 7.00 | 0.00 | 0 | 48 | +(0.00) −14(16; 0.00) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4) |
| 72 | 12 | reto | Ma+Ho (D) | 8 | 2 (0) | 10 | fill | 25 | 2043 | 0.725 | [0.75, 0.90) | normal | hard | 0.54 | 0.79 (reto) | 3.28 | 0.05 | 0 | 58 | +(0.03) −5(22; 0.13) | fuera de ventana (relajacion agotada); 1 ronda(s) extra de candidatos |
| 73 | 13 | comodidad | C | 8 | 1 (1) | 9 | walk | 18 | 288 | 0.375 | [0.20, 0.60] | normal | — | -0.39 | 0.20 (comodidad) | 2.17 | 0.08 | 0 | 48 | +(0.13) −7(17; 0.13) |  |
| 74 | 14 | comodidad | Ho | 8 | 2 (1) | 10 | walk | 16 | 732 | 0.600 | [0.20, 0.60] | normal | — | -0.10 | 0.49 (comodidad) | 3.94 | 0.05 | 0 | 46 | +(0.00) −4(15; 0.00) |  |
| 75 | 15 | reto | Al+Ch (D) | 8 | 1 (1) | 10 | deep | 19 | 4449 | 0.775 | [0.75, 0.90) | hard | hard | 0.55 | 0.81 (reto) | 5.32 | 0.09 | 0 | 48 | +(0.05) −5(17; 0.00) |  |
| 76 | 16 | comodidad | C | 8 | 1 (1) | 9 | walk | 16 | 231 | 0.575 | [0.20, 0.60] | normal | — | -0.25 | 0.34 (comodidad) | 2.69 | 0.07 | 0 | 40 | +(0.00) −5(14; 0.00) |  |
| 77 | 17 | comodidad | C | 8 | 2 (2) | 10 | walk | 20 | 3170 | 0.425 | [0.20, 0.60] | normal | — | 0.04 | 0.58 (comodidad) | 5.70 | 0.00 | 0 | 50 | −14(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 78 | 18 | reto | Ma+Al (D) | 8 | 2 (1) | 10 | walk | 20 | 13065 | 0.750 | [0.75, 0.90) | hard | hard | 0.77 | 0.89 (reto) | 5.75 | 0.17 | 0 | 50 | +(0.08) −1(18; 0.10) | relajacion: walk con 1 vacio |
| 79 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 18 | 2808 | 0.225 | [0.20, 0.40] | normal | — | -0.26 | 0.32 (comodidad) | 6.11 | 0.03 | 0 | 48 | −1(16; 0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 80 | 20 | hito | Ho+Ma (MD) | 8 | 2 (0) | 10 | fill | 28 | 1278 | 0.950 | [0.90, 1.00] | very_hard | very_hard | 0.98 | 0.95 (hito) | 2.46 | 0.09 | 0 | 62 | +(0.10) −3(23; 0.03) |  |
| 81 | 1 | recuperacion | R | 7 | 2 (2) | 9 | walk | 17 | 3371 | 0.225 | [0.00, 0.30) | normal | — | -0.38 | 0.21 (comodidad) | 4.76 | 0.00 | 0 | 42 | −1(16; 0.00) +(0.00) |  |
| 82 | 2 | recuperacion | R | 7 | 2 (2) | 9 | walk | 14 | 1199 | 0.000 | [0.00, 0.30) | normal | — | -0.92 | 0.01 (recuperacion) | 4.36 | 0.00 | 0 | 36 | −1(12; 0.00) +(0.00) |  |
| 83 | 3 | estreno | *Li | 8 | 2 (2) | 10 | walk | 17 | 699 | 0.025 | [0.00, 0.20) | normal | — | -0.68 | 0.07 (recuperacion) | 5.47 | 0.00 | 0 | 44 | −14(15; 0.00) +(0.00) |  |
| 84 | 4 | estreno | *Li | 8 | 2 (2) | 10 | walk | 17 | 1475 | 0.000 | [0.00, 0.20) | normal | — | -0.76 | 0.03 (recuperacion) | 5.06 | 0.00 | 0 | 46 | −7(16; 0.00) +(0.00) |  |
| 85 | 5 | estreno | *Li | 8 | 2 (2) | 10 | walk | 16 | 2213 | 0.050 | [0.00, 0.20) | normal | — | -0.76 | 0.04 (recuperacion) | 5.19 | 0.00 | 0 | 44 | −1(15; 0.00) +(0.00) |  |
| 86 | 6 | reto | Ma+Ho (D) | 9 | 2 (0) | 11 | deep | 23 | 2076 | 0.875 | [0.75, 0.90) | hard | hard | 0.51 | 0.78 (reto) | 3.00 | 0.14 | 1 | 56 | +(0.00) −3(21; 0.10) |  |
| 87 | 7 | comodidad | Li | 8 | 2 (2) | 10 | walk | 19 | 2095 | 0.450 | [0.20, 0.60] | normal | — | -0.07 | 0.53 (comodidad) | 5.16 | 0.00 | 0 | 48 | −10(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4) |
| 88 | 8 | comodidad | C | 8 | 1 (1) | 9 | walk | 17 | 267 | 0.550 | [0.20, 0.60] | normal | — | -0.14 | 0.44 (comodidad) | 3.18 | 0.07 | 0 | 46 | +(0.00) −10(16; 0.00) |  |
| 89 | 9 | reto | Li (D) | 9 | 2 (2) | 11 | fill | 27 | 2502 | 0.825 | [0.75, 0.90) | hard | hard | 0.64 | 0.85 (reto) | 3.00 | 0.06 | 0 | 64 | −7(23; 0.08) +(0.15) | relajacion: fill con 2 vacios (03 sec. 4.4) |
| 90 | 10 | comodidad | C | 8 | 1 (1) | 9 | walk | 18 | 296 | 0.475 | [0.20, 0.60] | normal | — | -0.31 | 0.28 (comodidad) | 2.17 | 0.05 | 0 | 46 | +(0.00) −7(16; 0.00) |  |
| 91 | 11 | comodidad | Ch | 8 | 1 (1) | 10 | walk | 16 | 853 | 0.350 | [0.20, 0.60] | normal | — | -0.13 | 0.46 (comodidad) | 6.63 | 0.00 | 0 | 46 | +(0.00) −3(15; 0.00) |  |
| 92 | 12 | reto | Li+Ho (D) | 9 | 1 (0) | 10 | fill | 27 | 161 | 0.800 | [0.75, 0.90) | hard | hard | 0.65 | 0.85 (reto) | 2.11 | 0.18 | 0 | 68 | +(0.55) −10(24; 0.45) |  |
| 93 | 13 | comodidad | C | 8 | 2 (2) | 10 | walk | 19 | 2296 | 0.375 | [0.20, 0.60] | normal | — | -0.32 | 0.27 (comodidad) | 3.53 | 0.03 | 0 | 48 | −4(16; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 94 | 14 | comodidad | Ma | 8 | 2 (1) | 10 | walk | 17 | 8436 | 0.225 | [0.20, 0.60] | normal | — | -0.25 | 0.33 (comodidad) | 6.24 | 0.00 | 0 | 44 | +(0.00) −10(16; 0.00) |  |
| 95 | 15 | reto | Li+Al (D) | 9 | 2 (2) | 11 | fill | 28 | 5126 | 0.800 | [0.75, 0.90) | hard | hard | 0.75 | 0.89 (reto) | 4.14 | 0.01 | 0 | 68 | −1(25; 0.13) +(0.05) | Liana con capacidades mixtas: se quitan Cuia chica y Cuia alta (bloqueo C1-9 de AF.Core); relajacion: fill con 2 vacios (03 sec. 4.4) |
| 96 | 16 | comodidad | C | 8 | 1 (1) | 9 | walk | 17 | 238 | 0.575 | [0.20, 0.60] | normal | — | -0.19 | 0.40 (comodidad) | 2.59 | 0.07 | 0 | 44 | +(0.05) −5(15; 0.00) |  |
| 97 | 17 | comodidad | C | 8 | 2 (2) | 10 | walk | 16 | 1540 | 0.325 | [0.20, 0.60] | normal | — | -0.22 | 0.37 (comodidad) | 6.69 | 0.03 | 0 | 44 | −8(15; 0.00) +(0.03) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 98 | 18 | reto | Li+Ma (D) | 9 | 2 (1) | 11 | walk | 23 | 6773 | 0.825 | [0.75, 0.90) | hard | hard | 0.95 | 0.92 (reto) | 5.09 | 0.25 | 0 | 58 | +(0.03) −7(21; 0.18) | relajacion: walk con 1 vacio |
| 99 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 19 | 3661 | 0.300 | [0.20, 0.40] | normal | — | -0.02 | 0.56 (comodidad) | 7.00 | 0.00 | 0 | 48 | −1(17; 0.00) +(0.03) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 100 | 20 | hito | *Tu (MD) | 9 | 0 (0) | 10 | deep | 20 | 112 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 0.99 | 0.96 (hito) | 3.10 | 0.55 | 2 | 52 | +(0.55) −5(17; 0.00) | turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 101 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 17 | 2109 | 0.050 | [0.00, 0.30) | normal | — | -0.63 | 0.06 (recuperacion) | 6.35 | 0.00 | 0 | 46 | −2(16; 0.00) +(0.00) |  |
| 102 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1523 | 0.025 | [0.00, 0.30) | normal | — | -0.72 | 0.03 (recuperacion) | 6.69 | 0.00 | 0 | 44 | −6(15; 0.00) +(0.00) |  |
| 103 | 3 | estreno | *Re | 9 | 2 (2) | 11 | walk | 19 | 27091 | 0.150 | [0.00, 0.20) | normal | — | -0.50 | 0.15 (comodidad) | 6.47 | 0.00 | 0 | 50 | −3(18; 0.00) +(0.00) |  |
| 104 | 4 | estreno | *Re | 9 | 2 (2) | 11 | walk | 19 | 25261 | 0.075 | [0.00, 0.20) | normal | — | -0.03 | 0.52 (comodidad) | 12.00 | 0.00 | 0 | 50 | −7(17; 0.00) +(0.00) |  |
| 105 | 5 | estreno | *Re | 9 | 2 (2) | 11 | walk | 18 | 13675 | 0.075 | [0.00, 0.20) | normal | — | -0.41 | 0.21 (comodidad) | 7.94 | 0.09 | 0 | 48 | −6(17; 0.00) +(0.00) |  |
| 106 | 6 | reto | Ho+Tu (D) | 9 | 1 (0) | 11 | deep | 19 | 400 | 0.800 | [0.75, 0.90) | hard | hard | -0.01 | 0.53 (comodidad) | 2.58 | 0.10 | 0 | 54 | +(0.18) −1(18; 0.00) | 1 ronda(s) extra de candidatos; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 107 | 7 | comodidad | Re | 9 | 1 (0) | 11 | walk | 18 | 61448 | 0.200 | [0.20, 0.60] | normal | — | -0.58 | 0.09 (recuperacion) | 6.06 | 0.00 | 0 | 50 | +(0.00) −2(16; 0.00) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4); 1 ronda(s) extra de candidatos |
| 108 | 8 | comodidad | C | 9 | 2 (2) | 11 | walk | 18 | 4286 | 0.275 | [0.20, 0.60] | normal | — | -0.29 | 0.31 (comodidad) | 8.00 | 0.00 | 0 | 50 | −2(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 109 | 9 | reto | Re (D) | 9 | 1 (1) | 10 | walk | 18 | 329 | 0.825 | [0.75, 0.90) | hard | hard | 0.03 | 0.57 (comodidad) | 2.61 | 0.11 | 1 | 50 | +(0.18) −1(17; 0.28) | relajacion: walk con 1 vacio; 1 ronda(s) extra de candidatos |
| 110 | 10 | comodidad | C | 9 | 2 (2) | 11 | walk | 18 | 2926 | 0.225 | [0.20, 0.60] | normal | — | -0.62 | 0.07 (recuperacion) | 5.39 | 0.00 | 0 | 48 | −5(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 111 | 11 | comodidad | Ma | 9 | 2 (1) | 11 | walk | 21 | 13310 | 0.500 | [0.20, 0.60] | normal | — | 0.04 | 0.58 (comodidad) | 5.00 | 0.00 | 0 | 56 | +(0.00) −1(19; 0.00) |  |
| 112 | 12 | reto | Re+Li (D) | 9 | 1 (0) | 10 | walk | 18 | 19679 | 0.850 | [0.75, 0.90) | hard | hard | 0.29 | 0.73 (reto) | 5.33 | 0.15 | 0 | 46 | +(0.53) −2(15; 0.43) | relajacion: walk con 1 vacio; 1 ronda(s) extra de candidatos |
| 113 | 13 | comodidad | C | 9 | 2 (2) | 11 | walk | 18 | 3910 | 0.375 | [0.20, 0.60] | normal | — | -0.20 | 0.38 (comodidad) | 7.78 | 0.00 | 0 | 50 | −3(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 114 | 14 | comodidad | Ch | 9 | 1 (1) | 11 | walk | 18 | 2098 | 0.350 | [0.20, 0.60] | normal | — | -0.20 | 0.37 (comodidad) | 7.06 | 0.00 | 0 | 52 | +(0.00) −3(17; 0.00) |  |
| 115 | 15 | reto | Ma+Tu (D) | 9 | 1 (0) | 11 | deep | 23 | 18657 | 0.775 | [0.75, 0.90) | hard | hard | 0.75 | 0.90 (reto) | 6.17 | 0.15 | 0 | 63 | +(0.15) −3(21; 0.00) | 1 ronda(s) extra de candidatos; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 116 | 16 | comodidad | C | 9 | 2 (2) | 11 | walk | 18 | 2073 | 0.225 | [0.20, 0.60] | normal | — | -0.56 | 0.09 (recuperacion) | 5.89 | 0.00 | 0 | 50 | −11(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 117 | 17 | comodidad | C | 9 | 2 (2) | 11 | walk | 20 | 6691 | 0.400 | [0.20, 0.60] | normal | — | 0.03 | 0.57 (comodidad) | 7.45 | 0.04 | 0 | 52 | −1(18; 0.00) +(0.03) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 118 | 18 | reto | Re+Ho (D) | 9 | 2 (0) | 11 | walk | 22 | 28704 | 0.775 | [0.75, 0.90) | hard | hard | 0.81 | 0.91 (reto) | 9.59 | 0.00 | 0 | 56 | +(0.15) −3(19; 0.00) | relajacion: walk con 1 vacio; 1 ronda(s) extra de candidatos |
| 119 | 19 | remanso | S | 9 | 2 (2) | 11 | walk | 23 | 10894 | 0.325 | [0.20, 0.40] | normal | — | 0.15 | 0.64 (comodidad) | 7.13 | 0.01 | 0 | 58 | −4(21; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 120 | 20 | hito | Re+Ma (MD) | 9 | 1 (0) | 10 | fill | 32 | 44879 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.89 | 1.00 (hito) | 5.47 | 0.24 | 4 | 70 | +(0.18) | 1 ronda(s) extra de candidatos |
| 121 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1832 | 0.200 | [0.00, 0.30) | normal | — | -0.60 | 0.08 (recuperacion) | 5.44 | 0.03 | 0 | 42 | −1(14; 0.00) +(0.00) |  |
| 122 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 20 | 6664 | 0.075 | [0.00, 0.30) | normal | — | -0.17 | 0.41 (comodidad) | 7.85 | 0.00 | 0 | 50 | −4(18; 0.00) +(0.00) |  |
| 123 | 3 | estreno | *Ba | 9 | 2 (2) | 12 | walk | 19 | 7299 | 0.000 | [0.00, 0.20) | normal | — | -0.44 | 0.20 (comodidad) | 8.79 | 0.00 | 0 | 58 | −2(17; 0.00) +(0.00) |  |
| 124 | 4 | estreno | *Ba | 9 | 2 (2) | 12 | walk | 18 | 1419 | 0.000 | [0.00, 0.20) | normal | — | -0.51 | 0.14 (comodidad) | 9.00 | 0.00 | 0 | 56 | −2(17; 0.00) +(0.00) |  |
| 125 | 5 | estreno | *Ba | 9 | 2 (2) | 12 | walk | 21 | 39361 | 0.000 | [0.00, 0.20) | normal | — | -0.36 | 0.26 (comodidad) | 7.76 | 0.00 | 0 | 57 | −11(20; 0.00) +(0.00) |  |
| 126 | 6 | reto | Re+Ho (D) | 9 | 1 (0) | 10 | deep | 21 | 399 | 0.825 | [0.75, 0.90) | hard | hard | 0.27 | 0.71 (reto) | 4.29 | 0.02 | 1 | 54 | +(0.15) −6(18; 0.03) |  |
| 127 | 7 | comodidad | Ba | 9 | 1 (1) | 11 | walk | 18 | 172 | 0.225 | [0.20, 0.60] | normal | — | -0.22 | 0.37 (comodidad) | 8.33 | 0.00 | 0 | 58 | +(0.00) −1(17; 0.00) |  |
| 128 | 8 | comodidad | C | 9 | 1 (1) | 10 | walk | 18 | 266 | 0.350 | [0.20, 0.60] | normal | — | -0.54 | 0.11 (comodidad) | 2.61 | 0.09 | 0 | 50 | +(0.00) −7(17; 0.05) |  |
| 129 | 9 | reto | Ba (D) | 9 | 0 (0) | 10 | walk | 20 | 551 | 0.750 | [0.75, 0.90) | hard | hard | 0.33 | 0.74 (reto) | 3.35 | 0.28 | 0 | 55 | +(0.10) −4(19; 0.00) | relajacion: walk con 1 vacio |
| 130 | 10 | comodidad | C | 9 | 2 (2) | 11 | walk | 20 | 5096 | 0.300 | [0.20, 0.60] | normal | — | -0.43 | 0.21 (comodidad) | 4.55 | 0.00 | 0 | 54 | −2(19; 0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 131 | 11 | comodidad | Li | 9 | 2 (2) | 11 | walk | 21 | 1763 | 0.600 | [0.20, 0.60] | normal | — | 0.12 | 0.62 (comodidad) | 5.57 | 0.01 | 0 | 56 | −10(20; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4) |
| 132 | 12 | reto | Ba+Ma (D) | 9 | 1 (0) | 11 | walk | 20 | 17716 | 0.750 | [0.75, 0.90) | hard | hard | 0.52 | 0.83 (reto) | 8.20 | 0.05 | 0 | 55 | +(0.03) −7(19; 0.00) | relajacion: walk con 1 vacio |
| 133 | 13 | comodidad | C | 9 | 1 (1) | 10 | walk | 20 | 735 | 0.300 | [0.20, 0.60] | normal | — | -0.52 | 0.13 (comodidad) | 2.60 | 0.00 | 0 | 50 | +(0.00) −3(18; 0.00) |  |
| 134 | 14 | comodidad | Ho | 9 | 2 (1) | 11 | walk | 22 | 9817 | 0.225 | [0.20, 0.60] | normal | — | -0.17 | 0.42 (comodidad) | 5.05 | 0.01 | 0 | 54 | +(0.00) −5(20; 0.00) |  |
| 135 | 15 | reto | Ba+Tu (D) | 9 | 1 (1) | 11 | fill | 29 | 7436 | 0.875 | [0.75, 0.90) | hard | hard | 1.14 | 0.97 (hito) | 2.86 | 0.11 | 1 | 69 | +(0.23) −4(24; 0.00) | sin Aguas turbias (ningun candidato midio Dificil o Muy dificil, T1); relajacion: fill con 2 vacios (03 sec. 4.4); sin Aguas turbias (T1) |
| 136 | 16 | comodidad | C | 9 | 2 (2) | 11 | walk | 21 | 12581 | 0.550 | [0.20, 0.60] | normal | — | 0.00 | 0.55 (comodidad) | 5.05 | 0.00 | 0 | 54 | −14(20; 0.03) +(0.05) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 137 | 17 | comodidad | C | 9 | 2 (2) | 11 | walk | 19 | 3811 | 0.325 | [0.20, 0.60] | normal | — | -0.19 | 0.39 (comodidad) | 7.58 | 0.00 | 0 | 52 | −1(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 138 | 18 | reto | Ba+Li (D) | 9 | 1 (1) | 11 | fill | 29 | 3532 | 0.875 | [0.75, 0.90) | hard | hard | 1.23 | 0.98 (hito) | 4.03 | 0.09 | 0 | 73 | +(0.03) −3(25; 0.05) | relajacion: fill con 2 vacios (03 sec. 4.4) |
| 139 | 19 | remanso | S | 9 | 2 (2) | 11 | walk | 21 | 5082 | 0.200 | [0.20, 0.40] | normal | — | -0.27 | 0.33 (comodidad) | 6.33 | 0.00 | 0 | 56 | −7(20; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 140 | 20 | hito | Re+Li (MD) | 9 | 1 (1) | 10 | deep | 26 | 62097 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.26 | 0.98 (hito) | 4.08 | 0.22 | 3 | 58 | +(0.50) −6(22; 0.58) |  |
| 141 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 21 | 2609 | 0.275 | [0.00, 0.30) | normal | — | -0.03 | 0.52 (comodidad) | 6.05 | 0.00 | 0 | 54 | −1(20; 0.00) +(0.08) |  |
| 142 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1999 | 0.025 | [0.00, 0.30) | normal | — | -0.82 | 0.01 (recuperacion) | 5.31 | 0.02 | 0 | 42 | −14(15; 0.00) +(0.03) |  |
| 143 | 3 | estreno | *Ll | 9 | 2 (1) | 11 | walk | 18 | 25202 | 0.075 | [0.00, 0.20) | normal | — | -0.49 | 0.16 (comodidad) | 7.33 | 0.00 | 0 | 50 | −1(17; 0.00) +(0.00) |  |
| 144 | 4 | estreno | *Ll | 9 | 2 (1) | 11 | walk | 18 | 5148 | 0.125 | [0.00, 0.20) | normal | — | -0.60 | 0.08 (recuperacion) | 5.72 | 0.00 | 0 | 52 | −3(17; 0.00) +(0.00) |  |
| 145 | 5 | estreno | *Ll | 9 | 2 (1) | 11 | walk | 19 | 7923 | 0.025 | [0.00, 0.20) | normal | — | -0.53 | 0.13 (comodidad) | 6.63 | 0.00 | 0 | 54 | −4(18; 0.00) +(0.00) |  |
| 146 | 6 | reto | Ba+Re (D) | 9 | 0 (0) | 10 | walk | 20 | 949 | 0.875 | [0.75, 0.90) | hard | hard | 0.28 | 0.73 (reto) | 3.25 | 0.14 | 1 | 55 | +(0.13) −2(17; 0.05) | relajacion: walk con 1 vacio; 2 ronda(s) extra de candidatos |
| 147 | 7 | comodidad | Ll | 9 | 2 (1) | 11 | walk | 21 | 29029 | 0.475 | [0.20, 0.60] | normal | — | 0.23 | 0.70 (comodidad) | 6.33 | 0.07 | 0 | 56 | +(0.05) −2(20; 0.08) | 2 ronda(s) extra de candidatos |
| 148 | 8 | comodidad | C | 9 | 2 (2) | 11 | walk | 22 | 13134 | 0.375 | [0.20, 0.60] | normal | — | 0.10 | 0.61 (comodidad) | 7.14 | 0.00 | 0 | 54 | −5(20; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 149 | 9 | reto | Ll (D) | 9 | 1 (0) | 10 | deep | 22 | 1963 | 0.750 | [0.75, 0.90) | hard | hard | 0.30 | 0.73 (reto) | 2.27 | 0.20 | 0 | 56 | +(0.20) −1(20; 0.28) | 2 ronda(s) extra de candidatos |
| 150 | 10 | comodidad | C | 9 | 2 (2) | 11 | walk | 22 | 6376 | 0.400 | [0.20, 0.60] | normal | — | 0.18 | 0.66 (comodidad) | 7.64 | 0.00 | 0 | 56 | −3(20; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 2 ronda(s) extra de candidatos |
| 151 | 11 | comodidad | Li | 9 | 2 (2) | 11 | walk | 19 | 2148 | 0.450 | [0.20, 0.60] | normal | — | -0.25 | 0.34 (comodidad) | 4.05 | 0.12 | 0 | 50 | −2(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4) |
| 152 | 12 | reto | Ll+Ho (D) | 9 | 2 (0) | 11 | deep | 23 | 7002 | 0.875 | [0.75, 0.90) | hard | hard | 0.47 | 0.81 (reto) | 3.30 | 0.07 | 1 | 54 | +(0.30) −6(20; 0.40) | 2 ronda(s) extra de candidatos |
| 153 | 13 | comodidad | C | 9 | 2 (2) | 11 | walk | 21 | 8382 | 0.525 | [0.20, 0.60] | normal | — | -0.11 | 0.47 (comodidad) | 4.19 | 0.00 | 0 | 56 | −2(20; 0.00) +(0.08) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 2 ronda(s) extra de candidatos |
| 154 | 14 | comodidad | Ma | 9 | 2 (1) | 11 | walk | 19 | 6639 | 0.325 | [0.20, 0.60] | normal | — | 0.11 | 0.62 (comodidad) | 9.58 | 0.00 | 0 | 52 | +(0.00) −3(18; 0.00) |  |
| 155 | 15 | reto | Ma+Tu (D) | 9 | 1 (1) | 11 | deep | 23 | 1394 | 0.850 | [0.75, 0.90) | hard | hard | 0.56 | 0.84 (reto) | 3.91 | 0.03 | 0 | 60 | +(0.03) −11(22; 0.20) | 2 ronda(s) extra de candidatos; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 156 | 16 | comodidad | C | 9 | 2 (2) | 11 | walk | 19 | 3264 | 0.350 | [0.20, 0.60] | normal | — | -0.31 | 0.30 (comodidad) | 6.05 | 0.00 | 0 | 52 | −2(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 2 ronda(s) extra de candidatos |
| 157 | 17 | comodidad | C | 9 | 2 (2) | 11 | walk | 20 | 10747 | 0.300 | [0.20, 0.60] | normal | — | -0.17 | 0.41 (comodidad) | 6.90 | 0.02 | 0 | 50 | −2(19; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 158 | 18 | reto | Ll+Ba (D) | 9 | 2 (1) | 11 | walk | 21 | 29975 | 0.825 | [0.75, 0.90) | hard | hard | 0.66 | 0.88 (reto) | 7.05 | 0.03 | 0 | 55 | +(0.33) −6(18; 0.00) | relajacion: walk con 1 vacio; 2 ronda(s) extra de candidatos |
| 159 | 19 | remanso | S | 9 | 2 (2) | 11 | walk | 19 | 1439 | 0.350 | [0.20, 0.40] | normal | — | -0.38 | 0.24 (comodidad) | 5.00 | 0.03 | 0 | 52 | −2(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 2 ronda(s) extra de candidatos |
| 160 | 20 | hito | Ll+Li+Al6 (MD) | 9 | 1 (0) | 10 | deep | 23 | 2519 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.02 | 0.95 (hito) | 3.22 | 0.39 | 0 | 58 | +(0.25) −14(21; 0.43) | Liana con capacidades mixtas: se quitan Cuia chica y Cuia alta (bloqueo C1-9 de AF.Core); 2 ronda(s) extra de candidatos |
| 161 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 19 | 3453 | 0.250 | [0.00, 0.30) | normal | — | -0.39 | 0.23 (comodidad) | 4.26 | 0.02 | 0 | 48 | −1(17; 0.00) +(0.00) |  |
| 162 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 18 | 3311 | 0.150 | [0.00, 0.30) | normal | — | -0.46 | 0.19 (comodidad) | 5.94 | 0.00 | 0 | 46 | −8(16; 0.00) +(0.00) |  |
| 163 | 3 | estreno | *Br | 9 | 2 (2) | 12 | walk | 19 | 15567 | 0.000 | [0.00, 0.20) | normal | — | -0.52 | 0.14 (comodidad) | 8.00 | 0.00 | 0 | 54 | −5(18; 0.00) +(0.00) |  |
| 164 | 4 | estreno | *Br | 9 | 2 (2) | 12 | walk | 18 | 1611 | 0.000 | [0.00, 0.20) | normal | — | -0.56 | 0.11 (comodidad) | 8.56 | 0.00 | 0 | 55 | −2(16; 0.00) +(0.00) |  |
| 165 | 5 | estreno | *Br | 9 | 2 (2) | 12 | walk | 19 | 100 | 0.000 | [0.00, 0.20) | normal | — | -0.56 | 0.10 (comodidad) | 7.58 | 0.00 | 0 | 56 | −2(18; 0.00) +(0.00) |  |
| 166 | 6 | reto | Ll+Re (D) | 9 | 2 (1) | 11 | walk | 18 | 25499 | 0.775 | [0.75, 0.90) | hard | hard | 0.35 | 0.75 (reto) | 6.61 | 0.08 | 0 | 50 | +(0.20) −7(17; 0.00) | relajacion: walk con 1 vacio; 1 ronda(s) extra de candidatos |
| 167 | 7 | comodidad | Br | 9 | 1 (1) | 11 | walk | 18 | 168 | 0.350 | [0.20, 0.60] | normal | — | -0.04 | 0.51 (comodidad) | 8.72 | 0.00 | 0 | 54 | +(0.00) −4(17; 0.00) |  |
| 168 | 8 | comodidad | C | 9 | 2 (2) | 11 | walk | 24 | 9824 | 0.425 | [0.20, 0.60] | normal | — | 0.34 | 0.75 (reto) | 6.96 | 0.01 | 0 | 60 | −7(22; 0.15) +(0.15) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 169 | 9 | reto | Br (D) | 9 | 1 (1) | 11 | deep | 22 | 5498 | 0.850 | [0.75, 0.90) | hard | hard | 0.42 | 0.79 (reto) | 3.82 | 0.00 | 0 | 61 | +(0.08) −2(21; 0.00) | endurecimiento: deep con 60.000 nodos |
| 170 | 10 | comodidad | C | 9 | 2 (2) | 11 | walk | 19 | 1870 | 0.375 | [0.20, 0.60] | normal | — | -0.35 | 0.27 (comodidad) | 5.42 | 0.00 | 0 | 52 | −3(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 171 | 11 | comodidad | Ho | 9 | 2 (1) | 11 | walk | 20 | 3962 | 0.200 | [0.20, 0.60] | normal | — | -0.44 | 0.20 (comodidad) | 4.60 | 0.00 | 0 | 54 | −2(19; 0.00) | alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 172 | 12 | reto | Br+Ba (D) | 9 | 1 (1) | 11 | deep | 21 | 17653 | 0.800 | [0.75, 0.90) | hard | hard | 0.44 | 0.79 (reto) | 4.86 | 0.05 | 0 | 58 | +(0.05) −2(19; 0.15) |  |
| 173 | 13 | comodidad | C | 9 | 2 (2) | 11 | walk | 23 | 10382 | 0.300 | [0.20, 0.60] | normal | — | 0.08 | 0.60 (comodidad) | 6.39 | 0.03 | 0 | 58 | −2(21; 0.00) +(0.03) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 174 | 14 | comodidad | Ma | 9 | 2 (1) | 11 | walk | 19 | 7327 | 0.425 | [0.20, 0.60] | normal | — | -0.11 | 0.46 (comodidad) | 6.16 | 0.00 | 0 | 52 | +(0.00) −6(18; 0.00) |  |
| 175 | 15 | reto | Br+Tu (D) | 9 | 1 (1) | 11 | deep | 24 | 7342 | 0.800 | [0.75, 0.90) | hard | hard | 0.46 | 0.81 (reto) | 2.75 | 0.02 | 0 | 62 | +(0.00) −11(23; 0.00) | relajacion: turbias con el vacio del rol y la holgura; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 176 | 16 | comodidad | C | 9 | 1 (1) | 10 | walk | 18 | 400 | 0.575 | [0.20, 0.60] | normal | — | -0.33 | 0.28 (comodidad) | 2.94 | 0.02 | 0 | 48 | −6(17; 0.08) | alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 177 | 17 | comodidad | C | 9 | 1 (1) | 10 | walk | 18 | 403 | 0.525 | [0.20, 0.60] | normal | — | -0.10 | 0.48 (comodidad) | 3.44 | 0.21 | 0 | 48 | +(0.03) −7(17; 0.00) |  |
| 178 | 18 | reto | Br+Ma (D) | 9 | 1 (0) | 11 | deep | 21 | 8664 | 0.825 | [0.75, 0.90) | hard | hard | 0.48 | 0.82 (reto) | 4.76 | 0.15 | 0 | 58 | +(0.03) −4(20; 0.00) | endurecimiento: deep |
| 179 | 19 | remanso | S | 9 | 2 (2) | 11 | walk | 18 | 4064 | 0.225 | [0.20, 0.40] | normal | — | -0.46 | 0.19 (comodidad) | 6.94 | 0.00 | 0 | 50 | −2(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 180 | 20 | hito | Ma+Li (MD) | 9 | 1 (0) | 10 | deep | 18 | 119 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 0.55 | 0.83 (reto) | 2.72 | 0.43 | 3 | 50 | +(0.48) −7(16; 0.10) |  |
| 181 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1274 | 0.000 | [0.00, 0.30) | normal | — | -0.77 | 0.02 (recuperacion) | 6.44 | 0.00 | 0 | 44 | −9(15; 0.00) +(0.00) |  |
| 182 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 2121 | 0.000 | [0.00, 0.30) | normal | — | -0.97 | 0.00 (recuperacion) | 4.25 | 0.01 | 0 | 44 | −6(15; 0.00) +(0.00) |  |
| 183 | 3 | estreno | *Pi | 9 | 2 (2) | 12 | walk | 19 | 28958 | 0.000 | [0.00, 0.20) | normal | — | -0.07 | 0.49 (comodidad) | 12.42 | 0.00 | 0 | 50 | −2(17; 0.00) +(0.00) |  |
| 184 | 4 | estreno | *Pi | 9 | 2 (2) | 12 | walk | 19 | 8303 | 0.050 | [0.00, 0.20) | normal | — | -0.15 | 0.43 (comodidad) | 11.05 | 0.00 | 0 | 52 | −6(18; 0.00) +(0.00) |  |
| 185 | 5 | estreno | *Pi | 9 | 2 (2) | 12 | walk | 22 | 177252 | 0.000 | [0.00, 0.20) | normal | — | -0.15 | 0.43 (comodidad) | 8.86 | 0.00 | 0 | 56 | −8(19; 0.00) +(0.00) |  |
| 186 | 6 | reto | Br+Ho (D) | 9 | 1 (0) | 11 | deep | 21 | 2088 | 0.875 | [0.75, 0.90) | hard | hard | 0.29 | 0.73 (reto) | 3.43 | 0.06 | 0 | 57 | +(0.05) −8(19; 0.00) | 2 ronda(s) extra de candidatos |
| 187 | 7 | comodidad | Pi | 9 | 1 (1) | 11 | walk | 18 | 8277 | 0.450 | [0.20, 0.60] | normal | — | 0.14 | 0.64 (comodidad) | 8.89 | 0.03 | 0 | 48 | +(0.00) −1(17; 0.00) | 2 ronda(s) extra de candidatos |
| 188 | 8 | comodidad | C | 9 | 1 (1) | 10 | walk | 19 | 437 | 0.575 | [0.20, 0.60] | normal | — | -0.22 | 0.36 (comodidad) | 2.58 | 0.06 | 0 | 48 | +(0.00) −2(17; 0.00) |  |
| 189 | 9 | reto | Pi (D) | 9 | 1 (1) | 11 | walk | 18 | 7393 | 0.725 | [0.75, 0.90) | normal | hard | 0.31 | 0.73 (reto) | 7.89 | 0.00 | 0 | 48 | +(0.10) −11(17; 0.00) | relajacion: walk con 1 vacio; fuera de ventana (relajacion agotada); 2 ronda(s) extra de candidatos |
| 190 | 10 | comodidad | C | 9 | 2 (2) | 11 | walk | 19 | 4815 | 0.275 | [0.20, 0.60] | normal | — | -0.24 | 0.35 (comodidad) | 7.58 | 0.00 | 0 | 52 | −2(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 2 ronda(s) extra de candidatos |
| 191 | 11 | comodidad | Re | 9 | 1 (1) | 11 | walk | 18 | 31280 | 0.550 | [0.20, 0.60] | normal | — | 0.11 | 0.61 (comodidad) | 7.83 | 0.00 | 0 | 50 | +(0.00) −1(13; 0.00) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4) |
| 192 | 12 | reto | Pi+Ma (D) | 9 | 1 (0) | 11 | deep | 23 | 14036 | 0.775 | [0.75, 0.90) | hard | hard | 0.34 | 0.75 (reto) | 4.00 | 0.00 | 0 | 60 | +(0.00) −1(21; 0.00) | 2 ronda(s) extra de candidatos |
| 193 | 13 | comodidad | C | 9 | 2 (2) | 11 | walk | 20 | 3336 | 0.275 | [0.20, 0.60] | normal | — | -0.28 | 0.32 (comodidad) | 6.30 | 0.00 | 0 | 54 | −3(19; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 2 ronda(s) extra de candidatos |
| 194 | 14 | comodidad | Ll | 9 | 2 (1) | 11 | walk | 19 | 4497 | 0.325 | [0.20, 0.60] | normal | — | 0.04 | 0.58 (comodidad) | 8.79 | 0.00 | 0 | 54 | +(0.00) −7(18; 0.00) |  |
| 195 | 15 | reto | Ba+Tu (D) | 9 | 1 (1) | 11 | deep | 23 | 13881 | 0.850 | [0.75, 0.90) | hard | hard | 0.80 | 0.91 (reto) | 5.22 | 0.12 | 0 | 64 | +(0.00) −14(21; 0.00) | relajacion: turbias con el vacio del rol y la holgura; 2 ronda(s) extra de candidatos; turbia: 3 dosis ocultas en 2 cuia(s), 6 realidades |
| 196 | 16 | comodidad | C | 9 | 2 (2) | 11 | walk | 18 | 2247 | 0.325 | [0.20, 0.60] | normal | — | -0.31 | 0.30 (comodidad) | 7.06 | 0.02 | 0 | 50 | −6(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 2 ronda(s) extra de candidatos |
| 197 | 17 | comodidad | C | 9 | 2 (2) | 11 | walk | 20 | 2538 | 0.375 | [0.20, 0.60] | normal | — | -0.19 | 0.40 (comodidad) | 6.10 | 0.00 | 0 | 54 | −1(18; 0.00) +(0.03) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 198 | 18 | reto | Pi+Li (D) | 9 | 2 (1) | 11 | fill | 28 | 177159 | 0.800 | [0.75, 0.90) | hard | hard | 0.91 | 0.94 (reto) | 3.71 | 0.00 | 0 | 68 | +(0.03) | Liana con Pirana: cueva de cap 4 (bloqueo C1-9 de AF.Core); 2 ronda(s) extra de candidatos |
| 199 | 19 | remanso | S | 9 | 2 (2) | 11 | walk | 19 | 4063 | 0.300 | [0.20, 0.40] | normal | — | -0.16 | 0.42 (comodidad) | 8.16 | 0.00 | 0 | 52 | −8(18; 0.00) +(0.03) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 2 ronda(s) extra de candidatos |
| 200 | 20 | hito | Pi+Ba (MD) | 9 | 0 (0) | 11 | fill | 30 | 21079 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.36 | 0.98 (hito) | 3.80 | 0.11 | 5 | 73 | +(0.18) −11(24; 0.43) | 2 ronda(s) extra de candidatos |

## Par / 2K por fuente frente a 03 §18

| Fuente | Niveles | Par / 2K medio | bot_fail medio | 03 §18 (par / 2K) |
|---|---|---|---|---|
| deep | 23 | 1.23 | 0.85 | 1,17 |
| fill | 19 | 1.63 | 0.78 | 1,53 |
| walk | 148 | 1.09 | 0.30 | 1,09 |

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

## Saltos de banda (03 §12: +2,2 / −3,4)

Ninguno fuera del límite.
