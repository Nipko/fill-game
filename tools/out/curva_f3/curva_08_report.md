# Campaña 421-560: informe de `level-generator --spec`

Generado por `dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/campaign_v3.json --levels 421-560 --out tools/out/curva_f3 --report tools/out/curva_f3/curva_08_report.md`: no se edita a mano y no lleva tiempos. Generador 1.0.0, `pack_schema` 3, modo de vertido `run` (D-005).

Fuentes: D-009 (ciclo escalonado), D-007 (debuts) y la tabla de novedades de 201-700 de D2 (D-023 punto 10) con la propuesta `docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md` (c.1-c.4, d.1-d.3 y d.5), D-019 (Tinaja) y D-023 punto 5 (variantes de la 1.0); GDD 03 §2, §4, §7-§13 y §17; 02 §8-§11; 08 §4.1. Todo se mide con `AF.Core` y `AF.Solver`: A* w = 1 a 300.000 (par), bot ×40 con la semilla del nivel (`bot_fail` y etiqueta), `EarlyTraps` (trampas tempranas, Q16 B), ruta de referencia para ramificación y trampas (`--parity`), `SafePath` en Aguas turbias y `LevelValidation` en cada tablero.

## Paquetes

| Pack | Niveles | Bytes | Bytes sin relief | Bytes sin relief por 100 niveles (≤ 100 KB, 03 §17 #11) | SHA-256 |
|---|---|---|---|---|---|
| `tools/out/curva_f3/curva_08_v1.json` | 140 | 189705 | 120105 | 83.8 KB | `a5f4601e0ab3b3e94b9b377cd7a1b45d6eb705ae213cd8c344f9c1ebee6b976a` |

## Pool por rango e índice D (03 §7)

D = 0,35·z(bot_fail) + 0,25·z(par − 2K) + 0,20·z(ramificación) + 0,10·z(trampas) + 0,10·[E libres = 1], con media y desviación del pool del rango (todos los candidatos en ventana de sus slots; congelados tras la primera pasada). Banda = cuantil de D en el pool: recuperación ≤ 0,10, comodidad ≤ 0,70, reto ≤ 0,95, hito > 0,95.

| Rango | Slots | Pool | Pool / slots | bot_fail μ ± σ | par − 2K μ ± σ | Ramif. μ ± σ | Trampas μ ± σ |
|---|---|---|---|---|---|---|---|
| 401-700 | 140 | 601 | 4.3 | 0.50 ± 0.27 | 2.7 ± 2.9 | 5.39 ± 1.69 | 0.05 ± 0.08 |

## Etiquetas por rango frente a 03 §11

| Rango (niveles generados) | Difícil | Muy difícil | 03 §11 | Aviso (> 5 puntos) |
|---|---|---|---|---|
| 401-700 (140) | 32 (22.9 %) | 7 (5.0 %) | 25-25 % / 5 % | — |

Nota: con D-009 desde el 61 los cinco retos apuntan a Difícil y el hito a Muy difícil (25 % y 5 % por ciclo), por encima del 15 % de 03 §11 para 61-100: es la cuenta del ciclo, no un error de la generación [P: la tabla de 03 §11 se revisa en la integración].

## Ciclos

| Ciclo | Temporada | Puros | Difícil | Muy difícil | Objetivo D / MD | Acierto de etiqueta (c.7) | Trampas tempranas | Relajaciones |
|---|---|---|---|---|---|---|---|---|
| 421-440 | B | 7/20 | 4 | 1 | 5 / 1 | 5/6 (83 %) | 5 | 9 |
| 441-460 | B | 8/20 | 4 | 1 | 5 / 1 | 5/6 (83 %) | 0 | 10 |
| 461-480 | A | 7/20 | 4 | 1 | 5 / 1 | 5/6 (83 %) | 1 | 8 |
| 481-500 | B | 8/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 3 | 11 |
| 501-520 | A | 7/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 1 | 11 |
| 521-540 | B | 7/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 8 | 6 |
| 541-560 | A | 8/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 7 | 13 |

## Niveles

Columnas: p = posición en el ciclo (0 en la rampa); E = vacíos de capacidad ≥ 4 (con la Hoja o el Matapi vacíos), libres = sin mecánica; q = cuantil de D en el pool del rango (3 decimales: la banda de hito empieza por encima de 0,95) y su banda; TT = trampas tempranas; alivio = tipos publicados en `relief` en orden (− = minus_color, + = plus_empty).

| Nivel | p | Rol | Calendario | K | E (libres) | Cuias | Fuente | Par | Estados A* | bot_fail | Ventana | Etiqueta | Objetivo | D | q (banda) | Ramif. | Trampas | TT | Compl. | Alivio | Notas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 421 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 18 | 2465 | 0.075 | [0.00, 0.30) | normal | — | -0.70 | 0.041 (recuperacion) | 5.06 | 0.00 | 0 | 46 | −8(17; 0.00) +(0.00) |  |
| 422 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 17 | 2799 | 0.000 | [0.00, 0.30) | normal | — | -0.82 | 0.021 (recuperacion) | 5.65 | 0.00 | 0 | 44 | −8(16; 0.00) +(0.00) |  |
| 423 | 3 | comodidad | Ma+Tu | 8 | 1 (0) | 10 | deep | 19 | 3530 | 0.475 | [0.20, 0.60] | normal | — | 0.19 | 0.653 (comodidad) | 7.53 | 0.00 | 0 | 55 | +(0.00) −10(18; 0.00) | endurecimiento: deep; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 424 | 4 | comodidad | C | 8 | 1 (1) | 9 | walk | 17 | 144 | 0.325 | [0.20, 0.60] | normal | — | -0.58 | 0.079 (recuperacion) | 2.47 | 0.07 | 0 | 48 | +(0.05) −2(15; 0.00) |  |
| 425 | 5 | comodidad | Ho | 8 | 2 (1) | 10 | walk | 17 | 2935 | 0.375 | [0.20, 0.60] | normal | — | -0.11 | 0.475 (comodidad) | 6.65 | 0.00 | 0 | 44 | +(0.03) −2(15; 0.00) |  |
| 426 | 6 | reto | Fl+Ma (D) | 9 | 2 (1) | 11 | walk | 19 | 2712 | 0.750 | [0.75, 0.90) | hard | hard | 0.08 | 0.606 (comodidad) | 3.74 | 0.04 | 0 | 50 | +(0.05) −5(16; 0.03) | relajacion: walk con 1 vacio |
| 427 | 7 | comodidad | C | 8 | 2 (2) | 10 | walk | 17 | 1669 | 0.325 | [0.20, 0.60] | normal | — | -0.57 | 0.087 (recuperacion) | 4.18 | 0.00 | 0 | 46 | −1(15; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 428 | 8 | comodidad | Ba+Tu | 8 | 1 (1) | 10 | walk | 18 | 1222 | 0.575 | [0.20, 0.60] | normal | — | -0.06 | 0.508 (comodidad) | 3.33 | 0.08 | 0 | 54 | +(0.00) −8(17; 0.00) | turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 429 | 9 | reto | Ma+Re (D) | 9 | 2 (0) | 11 | walk | 19 | 125736 | 0.725 | [0.75, 0.90) | normal | hard | 0.28 | 0.707 (reto) | 7.05 | 0.00 | 0 | 52 | +(0.03) −11(17; 0.00) | relajacion: walk con 1 vacio; fuera de ventana (relajacion agotada); cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 430 | 10 | comodidad | C | 8 | 1 (1) | 9 | walk | 16 | 205 | 0.400 | [0.20, 0.60] | normal | — | -0.59 | 0.072 (recuperacion) | 2.81 | 0.02 | 0 | 46 | +(0.00) −2(15; 0.00) |  |
| 431 | 11 | comodidad | B2 | 8 | 1 (1) | 10 | walk | 18 | 788 | 0.525 | [0.20, 0.60] | normal | — | -0.13 | 0.457 (comodidad) | 3.50 | 0.06 | 0 | 50 | +(0.00) −5(17; 0.00) | cvd_adjacent: 1 contacto(s) |
| 432 | 12 | reto | Fl+Ho (D) | 9 | 2 (1) | 11 | walk | 24 | 2808 | 0.850 | [0.75, 0.90) | hard | hard | 0.59 | 0.844 (reto) | 3.42 | 0.04 | 0 | 60 | +(0.40) −10(21; 0.25) | relajacion: walk con 1 vacio |
| 433 | 13 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 16 | 3478 | 0.225 | [0.20, 0.60] | normal | — | -0.27 | 0.322 (comodidad) | 7.44 | 0.03 | 0 | 49 | +(0.00) −3(15; 0.00) | turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 434 | 14 | comodidad | Al | 8 | 1 (1) | 10 | walk | 19 | 3581 | 0.450 | [0.20, 0.60] | normal | — | -0.05 | 0.527 (comodidad) | 4.95 | 0.00 | 0 | 50 | +(0.00) −6(18; 0.03) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4) |
| 435 | 15 | reto | Ma+Tu (D) | 9 | 1 (0) | 11 | deep | 21 | 14525 | 0.775 | [0.75, 0.90) | hard | hard | 0.64 | 0.866 (reto) | 6.95 | 0.11 | 0 | 59 | +(0.10) −5(20; 0.00) | turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 436 | 16 | comodidad | C | 8 | 1 (1) | 9 | walk | 16 | 250 | 0.475 | [0.20, 0.60] | normal | — | -0.20 | 0.397 (comodidad) | 2.88 | 0.26 | 0 | 44 | +(0.15) −4(15; 0.05) | cvd_adjacent: 1 contacto(s) |
| 437 | 17 | comodidad | Ha | 8 | 1 (1) | 10 | walk | 16 | 1433 | 0.575 | [0.20, 0.60] | normal | — | 0.03 | 0.575 (comodidad) | 6.06 | 0.03 | 0 | 44 | +(0.00) −5(14; 0.00) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4) |
| 438 | 18 | reto | Ho+Re (D) | 9 | 2 (1) | 11 | walk | 23 | 22969 | 0.775 | [0.75, 0.90) | hard | hard | 1.03 | 0.951 (hito) | 9.09 | 0.00 | 0 | 58 | +(0.08) −10(20; 0.05) | relajacion: walk con 1 vacio |
| 439 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 17 | 1345 | 0.375 | [0.20, 0.40] | normal | — | -0.35 | 0.242 (comodidad) | 5.47 | 0.00 | 0 | 46 | −10(16; 0.00) +(0.13) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 440 | 20 | hito | Ma+Ho (MD) | 11 | 2 (0) | 13 | fill | 34 | 12820 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.30 | 0.979 (hito) | 3.68 | 0.09 | 5 | 82 | +(0.55) −5(30; 0.25) | risk_adjacent: 1 contacto(s) |
| 441 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 19 | 4529 | 0.025 | [0.00, 0.30) | normal | — | -0.69 | 0.042 (recuperacion) | 4.63 | 0.03 | 0 | 48 | −14(18; 0.00) +(0.00) | 2 ronda(s) extra de candidatos |
| 442 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 17 | 1367 | 0.175 | [0.00, 0.30) | normal | — | -0.60 | 0.071 (recuperacion) | 5.41 | 0.01 | 0 | 46 | −4(15; 0.00) +(0.00) | 2 ronda(s) extra de candidatos |
| 443 | 3 | comodidad | Pi | 8 | 1 (1) | 10 | walk | 18 | 13677 | 0.350 | [0.20, 0.60] | normal | — | -0.05 | 0.523 (comodidad) | 6.56 | 0.02 | 0 | 46 | +(0.00) −11(17; 0.00) | cvd_adjacent: 3 contacto(s) |
| 444 | 4 | comodidad | C | 8 | 1 (1) | 9 | walk | 16 | 228 | 0.450 | [0.20, 0.60] | normal | — | -0.54 | 0.106 (comodidad) | 2.69 | 0.02 | 0 | 40 | +(0.00) −5(14; 0.00) |  |
| 445 | 5 | comodidad | Br+Tu | 8 | 1 (1) | 10 | walk | 17 | 1937 | 0.475 | [0.20, 0.60] | normal | — | 0.14 | 0.638 (comodidad) | 7.71 | 0.00 | 0 | 50 | +(0.00) −14(16; 0.00) | cvd_adjacent: 2 contacto(s); turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 446 | 6 | reto | Pi+Ba (D) | 9 | 0 (0) | 11 | walk | 19 | 3754 | 0.875 | [0.75, 0.90) | hard | hard | 0.36 | 0.741 (reto) | 4.00 | 0.20 | 0 | 51 | +(0.05) −6(18; 0.20) | relajacion: walk con 1 vacio; 2 ronda(s) extra de candidatos |
| 447 | 7 | comodidad | C | 8 | 2 (2) | 10 | walk | 18 | 2211 | 0.200 | [0.20, 0.60] | normal | — | -0.44 | 0.174 (comodidad) | 5.83 | 0.01 | 0 | 48 | −4(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); cvd_adjacent: 1 contacto(s); 2 ronda(s) extra de candidatos |
| 448 | 8 | comodidad | Ho+Tu | 8 | 1 (0) | 10 | deep | 19 | 4232 | 0.575 | [0.20, 0.60] | normal | — | -0.16 | 0.428 (comodidad) | 3.53 | 0.00 | 0 | 53 | +(0.00) −4(17; 0.00) | endurecimiento: deep; turbia: 2 dosis ocultas en 1 cuia(s), 1 realidades |
| 449 | 9 | reto | Br+Re (D) | 9 | 1 (1) | 11 | deep | 23 | 259330 | 0.750 | [0.75, 0.90) | hard | hard | 0.46 | 0.790 (reto) | 4.09 | 0.04 | 0 | 58 | +(0.05) −3(21; 0.00) | 2 ronda(s) extra de candidatos |
| 450 | 10 | comodidad | C | 8 | 1 (1) | 9 | walk | 16 | 194 | 0.525 | [0.20, 0.60] | normal | — | -0.45 | 0.162 (comodidad) | 2.38 | 0.05 | 0 | 44 | +(0.00) −5(15; 0.00) | 2 ronda(s) extra de candidatos |
| 451 | 11 | comodidad | Pi | 8 | 1 (1) | 10 | walk | 17 | 22443 | 0.225 | [0.20, 0.60] | normal | — | -0.29 | 0.305 (comodidad) | 6.82 | 0.00 | 0 | 44 | +(0.00) −9(16; 0.00) |  |
| 452 | 12 | reto | Pi+Br (D) | 9 | 1 (1) | 12 | deep | 21 | 93553 | 0.450 | [0.75, 0.90) | normal | hard | 0.56 | 0.829 (reto) | 10.05 | 0.00 | 0 | 56 | +(0.00) −5(19; 0.00) | endurecimiento: deep; fuera de ventana (relajacion agotada); 2 ronda(s) extra de candidatos |
| 453 | 13 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 17 | 4068 | 0.375 | [0.20, 0.60] | normal | — | -0.08 | 0.495 (comodidad) | 6.24 | 0.07 | 0 | 49 | +(0.00) −5(15; 0.00) | 2 ronda(s) extra de candidatos; turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 454 | 14 | comodidad | C | 8 | 2 (2) | 10 | walk | 17 | 895 | 0.275 | [0.20, 0.60] | normal | — | -0.47 | 0.146 (comodidad) | 5.06 | 0.05 | 0 | 46 | −8(16; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 455 | 15 | reto | Br+Tu (D) | 9 | 1 (1) | 11 | deep | 21 | 14193 | 0.775 | [0.75, 0.90) | hard | hard | 0.59 | 0.841 (reto) | 6.76 | 0.00 | 0 | 59 | +(0.00) −14(19; 0.00) | relajacion: turbias con el vacio del rol y la holgura; 2 ronda(s) extra de candidatos; turbia: 3 dosis ocultas en 2 cuia(s), 2 realidades |
| 456 | 16 | comodidad | C | 8 | 2 (2) | 10 | walk | 18 | 1456 | 0.525 | [0.20, 0.60] | normal | — | 0.10 | 0.620 (comodidad) | 5.83 | 0.10 | 0 | 48 | −11(17; 0.00) +(0.08) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 2 ronda(s) extra de candidatos |
| 457 | 17 | comodidad | Ha | 8 | 1 (1) | 10 | walk | 18 | 1571 | 0.400 | [0.20, 0.60] | normal | — | -0.03 | 0.545 (comodidad) | 6.00 | 0.04 | 0 | 48 | +(0.08) −9(17; 0.00) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4); cvd_adjacent: 1 contacto(s) |
| 458 | 18 | reto | Ba+Re (D) | 9 | 1 (1) | 11 | fill | 26 | 62337 | 0.825 | [0.75, 0.90) | hard | hard | 1.08 | 0.955 (hito) | 6.08 | 0.07 | 0 | 67 | +(0.13) −2(25; 0.03) | relajacion: fill con 2 vacios (03 sec. 4.4); 2 ronda(s) extra de candidatos |
| 459 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 17 | 2926 | 0.350 | [0.20, 0.40] | normal | — | -0.23 | 0.362 (comodidad) | 6.82 | 0.00 | 0 | 44 | −1(15; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 2 ronda(s) extra de candidatos |
| 460 | 20 | hito | Pi+Ba (MD) | 11 | 0 (0) | 13 | fill | 33 | 70829 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.09 | 0.956 (hito) | 3.15 | 0.05 | 0 | 83 | +(0.00) −11(27; 0.00) | cvd_adjacent: 2 contacto(s); 2 ronda(s) extra de candidatos |
| 461 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1042 | 0.025 | [0.00, 0.30) | normal | — | -0.66 | 0.056 (recuperacion) | 7.44 | 0.00 | 0 | 44 | −7(15; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 462 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 18 | 1743 | 0.175 | [0.00, 0.30) | normal | — | -0.68 | 0.051 (recuperacion) | 4.17 | 0.00 | 0 | 48 | −14(17; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 463 | 3 | comodidad | Li | 8 | 2 (2) | 10 | walk | 18 | 1610 | 0.425 | [0.20, 0.60] | normal | — | -0.13 | 0.453 (comodidad) | 6.06 | 0.00 | 0 | 48 | −10(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4) |
| 464 | 4 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 17 | 3382 | 0.525 | [0.20, 0.60] | normal | — | -0.03 | 0.537 (comodidad) | 5.35 | 0.03 | 0 | 48 | +(0.00) −6(16; 0.00) | turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 465 | 5 | comodidad | B2 | 8 | 1 (1) | 10 | walk | 17 | 533 | 0.225 | [0.20, 0.60] | normal | — | -0.43 | 0.181 (comodidad) | 5.59 | 0.00 | 0 | 48 | +(0.03) −10(16; 0.00) |  |
| 466 | 6 | reto | Li+B2 (D) | 9 | 1 (1) | 11 | deep | 19 | 1270 | 0.725 | [0.75, 0.90) | normal | hard | 0.12 | 0.631 (comodidad) | 4.79 | 0.00 | 0 | 57 | +(0.00) −3(18; 0.00) | fuera de ventana (relajacion agotada); cvd_adjacent: 1 contacto(s); 2 ronda(s) extra de candidatos |
| 467 | 7 | comodidad | C | 8 | 1 (1) | 9 | walk | 17 | 158 | 0.300 | [0.20, 0.60] | normal | — | -0.28 | 0.319 (comodidad) | 2.82 | 0.31 | 0 | 44 | +(0.03) −2(15; 0.10) | 1 ronda(s) extra de candidatos |
| 468 | 8 | comodidad | Re | 8 | 1 (1) | 10 | walk | 17 | 8060 | 0.500 | [0.20, 0.60] | normal | — | -0.06 | 0.510 (comodidad) | 5.53 | 0.02 | 0 | 44 | +(0.03) −3(15; 0.00) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4) |
| 469 | 9 | reto | Ln+B2 (D) | 9 | 1 (1) | 11 | deep | 22 | 1982 | 0.750 | [0.75, 0.90) | hard | hard | 0.26 | 0.698 (comodidad) | 3.23 | 0.03 | 0 | 58 | +(0.00) −6(21; 0.00) | 1 ronda(s) extra de candidatos |
| 470 | 10 | comodidad | C | 8 | 1 (1) | 9 | walk | 18 | 214 | 0.225 | [0.20, 0.60] | normal | — | -0.63 | 0.062 (recuperacion) | 2.17 | 0.10 | 0 | 46 | −10(16; 0.00) | 1 ronda(s) extra de candidatos; alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 471 | 11 | comodidad | Br+Tu | 8 | 1 (1) | 10 | deep | 19 | 3627 | 0.325 | [0.20, 0.60] | normal | — | -0.08 | 0.502 (comodidad) | 5.68 | 0.04 | 0 | 55 | +(0.00) −9(18; 0.00) | endurecimiento: deep; turbia: 2 dosis ocultas en 1 cuia(s), 1 realidades |
| 472 | 12 | reto | Li+Ho (D) | 9 | 2 (1) | 11 | walk | 20 | 241 | 0.875 | [0.75, 0.90) | hard | hard | 0.27 | 0.702 (reto) | 3.25 | 0.05 | 1 | 50 | +(0.18) −1(18; 0.03) | relajacion: walk con 1 vacio; cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 473 | 13 | comodidad | C | 8 | 2 (2) | 10 | walk | 16 | 1355 | 0.275 | [0.20, 0.60] | normal | — | -0.56 | 0.092 (recuperacion) | 5.50 | 0.00 | 0 | 44 | −1(14; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 474 | 14 | comodidad | Ch | 8 | 1 (1) | 10 | walk | 19 | 3319 | 0.475 | [0.20, 0.60] | normal | — | 0.12 | 0.635 (comodidad) | 6.11 | 0.00 | 0 | 50 | +(0.03) −4(17; 0.00) | cvd_adjacent: 2 contacto(s) |
| 475 | 15 | reto | Ho+Tu (D) | 9 | 1 (0) | 11 | deep | 25 | 1699 | 0.750 | [0.75, 0.90) | hard | hard | 0.39 | 0.753 (reto) | 3.16 | 0.01 | 0 | 62 | +(0.00) −7(23; 0.00) | 1 ronda(s) extra de candidatos; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 476 | 16 | comodidad | C | 8 | 1 (1) | 9 | walk | 18 | 183 | 0.500 | [0.20, 0.60] | normal | — | -0.17 | 0.413 (comodidad) | 2.22 | 0.18 | 0 | 46 | +(0.03) −8(17; 0.00) | 1 ronda(s) extra de candidatos |
| 477 | 17 | comodidad | Ba+Tu | 8 | 1 (1) | 10 | walk | 16 | 395 | 0.425 | [0.20, 0.60] | normal | — | -0.28 | 0.312 (comodidad) | 4.75 | 0.07 | 0 | 50 | +(0.00) −6(14; 0.00) | turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 478 | 18 | reto | Ln+Ho (D) | 9 | 2 (1) | 11 | walk | 23 | 979 | 0.800 | [0.75, 0.90) | hard | hard | 0.43 | 0.783 (reto) | 2.74 | 0.10 | 0 | 58 | +(0.03) −8(21; 0.18) | relajacion: walk con 1 vacio; cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 479 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 18 | 4279 | 0.250 | [0.20, 0.40] | normal | — | -0.29 | 0.304 (comodidad) | 6.67 | 0.00 | 0 | 48 | −12(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 480 | 20 | hito | Li+B2+Ho (MD) | 11 | 1 (0) | 13 | deep | 30 | 1050 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.19 | 0.967 (hito) | 4.13 | 0.24 | 0 | 74 | +(0.78) −8(27; 0.60) | cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos; 1 ronda(s) de banda |
| 481 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 17 | 1191 | 0.100 | [0.00, 0.30) | normal | — | -0.71 | 0.039 (recuperacion) | 5.41 | 0.00 | 0 | 46 | −3(16; 0.00) +(0.00) |  |
| 482 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 17 | 1212 | 0.025 | [0.00, 0.30) | normal | — | -0.81 | 0.022 (recuperacion) | 5.41 | 0.00 | 0 | 46 | −9(15; 0.00) +(0.00) |  |
| 483 | 3 | comodidad | Ma+Tu | 8 | 1 (0) | 10 | deep | 22 | 16874 | 0.550 | [0.20, 0.60] | normal | — | 0.24 | 0.693 (comodidad) | 5.00 | 0.00 | 0 | 59 | +(0.00) −5(19; 0.00) | endurecimiento: deep; cvd_adjacent: 3 contacto(s); turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 484 | 4 | comodidad | C | 8 | 2 (2) | 10 | walk | 16 | 1490 | 0.350 | [0.20, 0.60] | normal | — | -0.49 | 0.131 (comodidad) | 5.31 | 0.00 | 0 | 44 | −2(15; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); cvd_adjacent: 1 contacto(s) |
| 485 | 5 | comodidad | Ha | 8 | 1 (1) | 10 | walk | 16 | 2321 | 0.275 | [0.20, 0.60] | normal | — | -0.48 | 0.139 (comodidad) | 5.25 | 0.01 | 0 | 40 | +(0.00) −3(13; 0.00) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4) |
| 486 | 6 | reto | Ma+Ha (D) | 9 | 2 (1) | 11 | walk | 18 | 2644 | 0.800 | [0.75, 0.90) | hard | hard | 0.61 | 0.854 (reto) | 8.22 | 0.06 | 0 | 50 | +(0.28) −1(17; 0.00) | relajacion: walk con 1 vacio |
| 487 | 7 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 17 | 2377 | 0.300 | [0.20, 0.60] | normal | — | -0.02 | 0.550 (comodidad) | 7.71 | 0.05 | 0 | 49 | +(0.00) −3(16; 0.00) | turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 488 | 8 | comodidad | Re | 8 | 1 (0) | 10 | walk | 16 | 25354 | 0.200 | [0.20, 0.60] | normal | — | -0.30 | 0.290 (comodidad) | 8.38 | 0.02 | 0 | 42 | +(0.00) −14(14; 0.00) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4) |
| 489 | 9 | reto | Ma+Ba (D) | 9 | 1 (0) | 11 | walk | 20 | 7733 | 0.825 | [0.75, 0.90) | hard | hard | 0.70 | 0.881 (reto) | 6.45 | 0.22 | 0 | 55 | +(0.00) −1(19; 0.00) | relajacion: walk con 1 vacio |
| 490 | 10 | comodidad | C | 8 | 2 (2) | 10 | walk | 18 | 3629 | 0.350 | [0.20, 0.60] | normal | — | -0.22 | 0.364 (comodidad) | 6.11 | 0.00 | 0 | 48 | −2(17; 0.00) +(0.05) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 491 | 11 | comodidad | Br+Tu | 8 | 1 (1) | 10 | walk | 18 | 2276 | 0.400 | [0.20, 0.60] | normal | — | 0.05 | 0.593 (comodidad) | 7.06 | 0.00 | 0 | 54 | +(0.00) −6(17; 0.00) | risk_adjacent: 1 contacto(s); turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 492 | 12 | reto | Ha+Ba (D) | 9 | 1 (1) | 11 | fill | 28 | 1832 | 0.800 | [0.75, 0.90) | hard | hard | 0.85 | 0.921 (reto) | 2.64 | 0.09 | 0 | 71 | +(0.10) −14(25; 0.18) | relajacion: fill con 2 vacios (03 sec. 4.4); cvd_adjacent: 1 contacto(s) |
| 493 | 13 | comodidad | C | 8 | 1 (1) | 9 | walk | 17 | 270 | 0.475 | [0.20, 0.60] | normal | — | -0.13 | 0.467 (comodidad) | 3.88 | 0.15 | 0 | 44 | +(0.00) −5(15; 0.00) | cvd_adjacent: 2 contacto(s) |
| 494 | 14 | comodidad | C | 8 | 1 (1) | 9 | walk | 17 | 273 | 0.575 | [0.20, 0.60] | normal | — | -0.17 | 0.415 (comodidad) | 2.47 | 0.14 | 0 | 46 | +(0.05) −14(16; 0.08) | cvd_adjacent: 1 contacto(s) |
| 495 | 15 | reto | Ma+Tu (D) | 9 | 1 (0) | 11 | deep | 24 | 13089 | 0.750 | [0.75, 0.90) | hard | hard | 0.87 | 0.926 (reto) | 6.42 | 0.16 | 0 | 65 | +(0.03) −1(21; 0.00) | turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 496 | 16 | comodidad | C | 8 | 2 (2) | 10 | walk | 17 | 2947 | 0.275 | [0.20, 0.60] | normal | — | -0.22 | 0.370 (comodidad) | 7.71 | 0.00 | 0 | 46 | −7(16; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 497 | 17 | comodidad | B2 | 8 | 1 (1) | 10 | walk | 18 | 1937 | 0.250 | [0.20, 0.60] | normal | — | -0.12 | 0.468 (comodidad) | 7.22 | 0.00 | 0 | 53 | +(0.00) −4(17; 0.00) |  |
| 498 | 18 | reto | Fl+Ha (D) | 9 | 2 (2) | 11 | fill | 31 | 3273 | 0.850 | [0.75, 0.90) | hard | hard | 1.10 | 0.958 (hito) | 3.26 | 0.06 | 0 | 72 | −3(27; 0.00) | relajacion: fill con 2 vacios (03 sec. 4.4); risk_adjacent: 1 contacto(s); cvd_adjacent: 1 contacto(s); alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 499 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 17 | 2398 | 0.275 | [0.20, 0.40] | normal | — | -0.42 | 0.194 (comodidad) | 6.00 | 0.00 | 0 | 46 | −4(16; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); cvd_adjacent: 1 contacto(s) |
| 500 | 20 | hito | Ma+Ha (MD) | 11 | 1 (0) | 12 | fill | 33 | 284 | 0.975 | [0.90, 1.00] | very_hard | very_hard | 1.24 | 0.969 (hito) | 2.52 | 0.25 | 3 | 84 | +(0.75) −4(30; 0.70) |  |
| 501 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 17 | 1212 | 0.050 | [0.00, 0.30) | normal | — | -0.86 | 0.012 (recuperacion) | 4.76 | 0.00 | 0 | 46 | −6(16; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 502 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 78 | 0.000 | [0.00, 0.30) | normal | — | -0.73 | 0.034 (recuperacion) | 7.13 | 0.00 | 0 | 46 | −11(15; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 503 | 3 | comodidad | Ll | 8 | 2 (1) | 10 | walk | 17 | 88 | 0.350 | [0.20, 0.60] | normal | — | 0.08 | 0.613 (comodidad) | 8.59 | 0.00 | 0 | 50 | +(0.00) −3(16; 0.00) | cvd_adjacent: 1 contacto(s) |
| 504 | 4 | comodidad | C | 8 | 1 (1) | 9 | walk | 16 | 281 | 0.225 | [0.20, 0.60] | normal | — | -0.87 | 0.009 (recuperacion) | 2.38 | 0.03 | 0 | 42 | +(0.00) −3(14; 0.00) |  |
| 505 | 5 | comodidad | Ba+Tu | 8 | 1 (1) | 10 | walk | 18 | 4041 | 0.250 | [0.20, 0.60] | normal | — | -0.37 | 0.227 (comodidad) | 4.28 | 0.08 | 0 | 52 | +(0.03) −3(16; 0.00) | 1 ronda(s) extra de candidatos turbios; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 506 | 6 | reto | Ll+Ba (D) | 9 | 2 (1) | 11 | walk | 18 | 12342 | 0.875 | [0.75, 0.90) | hard | hard | 0.41 | 0.773 (reto) | 5.67 | 0.07 | 0 | 51 | +(0.03) −1(16; 0.00) | relajacion: walk con 1 vacio; 1 ronda(s) extra de candidatos |
| 507 | 7 | comodidad | C | 8 | 2 (2) | 10 | walk | 18 | 2580 | 0.425 | [0.20, 0.60] | normal | — | -0.15 | 0.432 (comodidad) | 5.89 | 0.00 | 0 | 48 | −6(17; 0.00) +(0.03) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 508 | 8 | comodidad | Ho+Tu | 8 | 1 (0) | 10 | deep | 19 | 187 | 0.225 | [0.20, 0.60] | normal | — | -0.61 | 0.069 (recuperacion) | 3.16 | 0.03 | 0 | 52 | +(0.00) −6(17; 0.00) | endurecimiento: deep; cvd_adjacent: 2 contacto(s); turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 509 | 9 | reto | Fl (D) | 9 | 2 (2) | 11 | fill | 24 | 571 | 0.825 | [0.75, 0.90) | hard | hard | 0.42 | 0.782 (reto) | 2.71 | 0.08 | 1 | 60 | −10(22; 0.00) +(0.28) | relajacion: fill con 2 vacios (03 sec. 4.4); cvd_adjacent: 1 contacto(s); 2 ronda(s) extra de candidatos |
| 510 | 10 | comodidad | C | 8 | 2 (2) | 10 | walk | 18 | 4296 | 0.275 | [0.20, 0.60] | normal | — | -0.32 | 0.269 (comodidad) | 5.56 | 0.05 | 0 | 44 | −13(16; 0.00) +(0.05) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 511 | 11 | comodidad | Li | 8 | 2 (2) | 10 | walk | 16 | 685 | 0.300 | [0.20, 0.60] | normal | — | -0.43 | 0.187 (comodidad) | 5.69 | 0.07 | 0 | 44 | −1(15; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4) |
| 512 | 12 | reto | Ll+Ha (D) | 9 | 2 (1) | 11 | walk | 19 | 1780 | 0.875 | [0.75, 0.90) | hard | hard | 0.54 | 0.823 (reto) | 6.21 | 0.05 | 0 | 54 | +(0.00) −11(18; 0.00) | relajacion: walk con 1 vacio; cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 513 | 13 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 16 | 1272 | 0.375 | [0.20, 0.60] | normal | — | -0.24 | 0.349 (comodidad) | 5.88 | 0.04 | 0 | 49 | +(0.00) −5(15; 0.00) | cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 514 | 14 | comodidad | Ll+Al | 8 | 2 (1) | 10 | walk | 16 | 3770 | 0.250 | [0.20, 0.60] | normal | — | -0.39 | 0.217 (comodidad) | 6.13 | 0.03 | 0 | 44 | +(0.00) −12(15; 0.00) |  |
| 515 | 15 | reto | Ba+Tu (D) | 9 | 1 (1) | 11 | deep | 21 | 2611 | 0.875 | [0.75, 0.90) | hard | hard | 0.56 | 0.829 (reto) | 5.48 | 0.00 | 0 | 63 | +(0.00) −1(20; 0.00) | relajacion: turbias con el vacio del rol y la holgura; 1 ronda(s) extra de candidatos; turbia: 2 dosis ocultas en 1 cuia(s), 1 realidades |
| 516 | 16 | comodidad | C | 8 | 2 (2) | 10 | walk | 17 | 1206 | 0.550 | [0.20, 0.60] | normal | — | 0.07 | 0.597 (comodidad) | 7.12 | 0.00 | 0 | 48 | −6(16; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); cvd_adjacent: 4 contacto(s); 1 ronda(s) extra de candidatos |
| 517 | 17 | comodidad | B2 | 8 | 1 (1) | 10 | walk | 16 | 1384 | 0.250 | [0.20, 0.60] | normal | — | -0.29 | 0.300 (comodidad) | 7.25 | 0.00 | 0 | 46 | +(0.00) −6(15; 0.00) | cvd_adjacent: 1 contacto(s) |
| 518 | 18 | reto | Fl+Ba (D) | 9 | 1 (1) | 11 | fill | 26 | 20698 | 0.800 | [0.75, 0.90) | hard | hard | 0.69 | 0.879 (reto) | 3.38 | 0.03 | 0 | 67 | +(0.05) −9(22; 0.03) | relajacion: fill con 2 vacios (03 sec. 4.4); cvd_adjacent: 2 contacto(s); 1 ronda(s) extra de candidatos |
| 519 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 16 | 1379 | 0.325 | [0.20, 0.40] | normal | — | -0.37 | 0.225 (comodidad) | 6.56 | 0.00 | 0 | 44 | −4(15; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 520 | 20 | hito | Ll+Ba+Ha (MD) | 11 | 1 (0) | 12 | fill | 33 | 4525 | 0.950 | [0.90, 1.00] | very_hard | very_hard | 1.18 | 0.967 (hito) | 2.82 | 0.20 | 0 | 83 | +(0.53) −5(28; 0.70) | risk_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos; 1 ronda(s) de banda |
| 521 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1128 | 0.000 | [0.00, 0.30) | normal | — | -0.96 | 0.002 (recuperacion) | 5.19 | 0.00 | 0 | 44 | −11(15; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 522 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1368 | 0.250 | [0.00, 0.30) | normal | — | -0.57 | 0.089 (recuperacion) | 5.75 | 0.00 | 0 | 44 | −3(15; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 523 | 3 | comodidad | Pi | 8 | 1 (1) | 10 | walk | 17 | 17616 | 0.425 | [0.20, 0.60] | normal | — | -0.06 | 0.517 (comodidad) | 6.41 | 0.02 | 0 | 44 | +(0.00) −10(15; 0.00) |  |
| 524 | 4 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 18 | 5484 | 0.300 | [0.20, 0.60] | normal | — | -0.32 | 0.274 (comodidad) | 5.00 | 0.00 | 0 | 53 | +(0.00) −1(17; 0.00) | cvd_adjacent: 1 contacto(s); turbia: 2 dosis ocultas en 1 cuia(s), 1 realidades |
| 525 | 5 | comodidad | Re | 8 | 1 (0) | 10 | walk | 16 | 1466 | 0.250 | [0.20, 0.60] | normal | — | -0.20 | 0.382 (comodidad) | 8.81 | 0.00 | 0 | 46 | +(0.00) −5(15; 0.00) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4); cvd_adjacent: 1 contacto(s) |
| 526 | 6 | reto | Pi+Ha (D) | 9 | 1 (1) | 11 | walk | 18 | 7636 | 0.800 | [0.75, 0.90) | hard | hard | 0.36 | 0.745 (reto) | 6.78 | 0.00 | 0 | 50 | +(0.03) −2(16; 0.00) | relajacion: walk con 1 vacio; 1 ronda(s) extra de candidatos |
| 527 | 7 | comodidad | C | 8 | 1 (1) | 9 | walk | 18 | 165 | 0.225 | [0.20, 0.60] | normal | — | -0.36 | 0.239 (comodidad) | 2.78 | 0.26 | 0 | 46 | +(0.03) −1(16; 0.00) | 1 ronda(s) extra de candidatos |
| 528 | 8 | comodidad | Br+Tu | 8 | 1 (1) | 10 | walk | 17 | 2237 | 0.300 | [0.20, 0.60] | normal | — | -0.13 | 0.460 (comodidad) | 7.35 | 0.00 | 0 | 52 | +(0.00) −7(16; 0.00) | turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 529 | 9 | reto | Ho+Re (D) | 9 | 2 (1) | 11 | walk | 19 | 69206 | 0.825 | [0.75, 0.90) | hard | hard | 0.40 | 0.766 (reto) | 5.84 | 0.03 | 1 | 46 | +(0.13) −2(16; 0.00) | relajacion: walk con 1 vacio; 1 ronda(s) extra de candidatos |
| 530 | 10 | comodidad | C | 8 | 1 (1) | 9 | walk | 17 | 145 | 0.275 | [0.20, 0.60] | normal | — | -0.62 | 0.067 (recuperacion) | 2.53 | 0.09 | 0 | 48 | +(0.03) −13(16; 0.00) | risk_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 531 | 11 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 17 | 2820 | 0.425 | [0.20, 0.60] | normal | — | -0.16 | 0.427 (comodidad) | 5.71 | 0.00 | 0 | 51 | +(0.00) −8(16; 0.00) | turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 532 | 12 | reto | Pi+Ho (D) | 9 | 1 (0) | 11 | deep | 25 | 2494 | 0.850 | [0.75, 0.90) | hard | hard | 0.52 | 0.805 (reto) | 2.88 | 0.04 | 1 | 60 | +(0.35) −13(22; 0.18) | 1 ronda(s) extra de candidatos |
| 533 | 13 | comodidad | C | 8 | 1 (1) | 9 | walk | 17 | 243 | 0.325 | [0.20, 0.60] | normal | — | -0.68 | 0.049 (recuperacion) | 2.41 | 0.00 | 0 | 44 | +(0.00) −7(15; 0.00) | cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 534 | 14 | comodidad | Ch | 8 | 1 (1) | 10 | walk | 18 | 3428 | 0.300 | [0.20, 0.60] | normal | — | -0.44 | 0.172 (comodidad) | 4.00 | 0.00 | 0 | 48 | +(0.00) −9(17; 0.00) | cvd_adjacent: 1 contacto(s) |
| 535 | 15 | reto | Ho+Tu (D) | 9 | 1 (0) | 11 | deep | 23 | 8125 | 0.850 | [0.75, 0.90) | hard | hard | 0.61 | 0.855 (reto) | 4.57 | 0.10 | 0 | 60 | +(0.20) −9(21; 0.25) | 1 ronda(s) extra de candidatos; turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 536 | 16 | comodidad | C | 8 | 2 (2) | 10 | walk | 17 | 3901 | 0.275 | [0.20, 0.60] | normal | — | -0.29 | 0.302 (comodidad) | 7.12 | 0.00 | 0 | 44 | −9(16; 0.00) +(0.03) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 537 | 17 | comodidad | B2 | 8 | 1 (1) | 10 | walk | 17 | 1006 | 0.225 | [0.20, 0.60] | normal | — | -0.28 | 0.317 (comodidad) | 6.53 | 0.04 | 0 | 50 | +(0.00) −2(16; 0.00) |  |
| 538 | 18 | reto | Ha+Re (D) | 9 | 2 (2) | 11 | fill | 30 | 239755 | 0.800 | [0.75, 0.90) | hard | hard | 1.12 | 0.963 (hito) | 5.30 | 0.00 | 1 | 72 | −5(26; 0.00) +(0.03) | relajacion: fill con 2 vacios (03 sec. 4.4); risk_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 539 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 18 | 3065 | 0.300 | [0.20, 0.40] | normal | — | -0.39 | 0.215 (comodidad) | 5.22 | 0.00 | 0 | 46 | −7(16; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 540 | 20 | hito | Pi+Ha (MD) | 11 | 1 (1) | 13 | fill | 32 | 34819 | 0.975 | [0.90, 1.00] | very_hard | very_hard | 1.15 | 0.963 (hito) | 4.03 | 0.03 | 5 | 80 | +(0.65) −9(29; 0.10) | risk_adjacent: 1 contacto(s); cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 541 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 18 | 1314 | 0.150 | [0.00, 0.30) | normal | — | -0.55 | 0.099 (recuperacion) | 5.50 | 0.00 | 0 | 48 | −3(16; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 542 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1994 | 0.075 | [0.00, 0.30) | normal | — | -0.73 | 0.032 (recuperacion) | 6.25 | 0.00 | 0 | 44 | −1(14; 0.00) +(0.00) | cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 543 | 3 | comodidad | Fl | 8 | 1 (1) | 9 | walk | 16 | 167 | 0.475 | [0.20, 0.60] | normal | — | -0.49 | 0.136 (comodidad) | 2.63 | 0.05 | 0 | 44 | +(0.25) −3(14; 0.00) |  |
| 544 | 4 | comodidad | C | 8 | 2 (2) | 10 | walk | 20 | 4965 | 0.250 | [0.20, 0.60] | normal | — | -0.06 | 0.513 (comodidad) | 7.15 | 0.00 | 0 | 50 | −9(18; 0.00) +(0.03) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); cvd_adjacent: 1 contacto(s) |
| 545 | 5 | comodidad | Ho+Tu | 8 | 1 (0) | 10 | deep | 20 | 2860 | 0.375 | [0.20, 0.60] | normal | — | -0.25 | 0.339 (comodidad) | 3.55 | 0.06 | 0 | 57 | +(0.00) −11(19; 0.00) | endurecimiento: deep; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 546 | 6 | reto | Fl+Ho (D) | 9 | 2 (1) | 11 | walk | 21 | 1157 | 0.800 | [0.75, 0.90) | hard | hard | 0.36 | 0.743 (reto) | 3.19 | 0.13 | 0 | 54 | +(0.08) −13(19; 0.00) | relajacion: walk con 1 vacio; 1 ronda(s) extra de candidatos |
| 547 | 7 | comodidad | C | 8 | 2 (2) | 10 | walk | 17 | 1048 | 0.600 | [0.20, 0.60] | normal | — | -0.09 | 0.490 (comodidad) | 4.76 | 0.05 | 0 | 46 | −1(15; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); cvd_adjacent: 2 contacto(s); 1 ronda(s) extra de candidatos |
| 548 | 8 | comodidad | Ba+Tu | 8 | 2 (2) | 11 | walk | 20 | 42864 | 0.200 | [0.20, 0.60] | normal | — | 0.04 | 0.585 (comodidad) | 8.55 | 0.00 | 0 | 53 | −14(19; 0.00) +(0.00) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4); 1 ronda(s) extra de candidatos turbios; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 549 | 9 | reto | Ln+Ho (D) | 9 | 2 (1) | 11 | walk | 24 | 3506 | 0.850 | [0.75, 0.90) | hard | hard | 0.57 | 0.838 (reto) | 3.67 | 0.00 | 0 | 58 | +(0.05) −1(22; 0.13) | relajacion: walk con 1 vacio; cvd_adjacent: 4 contacto(s); 1 ronda(s) extra de candidatos |
| 550 | 10 | comodidad | C | 8 | 2 (2) | 10 | walk | 19 | 4966 | 0.225 | [0.20, 0.60] | normal | — | -0.19 | 0.399 (comodidad) | 7.00 | 0.00 | 0 | 50 | −4(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 551 | 11 | comodidad | Ll | 8 | 2 (1) | 10 | walk | 17 | 7363 | 0.250 | [0.20, 0.60] | normal | — | -0.52 | 0.114 (comodidad) | 4.59 | 0.00 | 0 | 44 | +(0.00) −3(16; 0.00) | cvd_adjacent: 1 contacto(s) |
| 552 | 12 | reto | Ln+Re (D) | 9 | 1 (0) | 10 | walk | 19 | 5913 | 0.850 | [0.75, 0.90) | hard | hard | 0.61 | 0.855 (reto) | 5.42 | 0.29 | 0 | 50 | +(0.35) −14(17; 0.48) | relajacion: walk con 1 vacio; cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 553 | 13 | comodidad | C | 8 | 2 (2) | 10 | walk | 18 | 2037 | 0.575 | [0.20, 0.60] | normal | — | 0.04 | 0.583 (comodidad) | 5.89 | 0.00 | 0 | 48 | −9(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 554 | 14 | comodidad | Al | 8 | 1 (1) | 10 | walk | 17 | 2713 | 0.300 | [0.20, 0.60] | normal | — | -0.22 | 0.369 (comodidad) | 6.59 | 0.00 | 0 | 44 | +(0.00) −3(15; 0.00) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4) |
| 555 | 15 | reto | Ho+Tu (D) | 9 | 1 (0) | 11 | deep | 25 | 14145 | 0.825 | [0.75, 0.90) | hard | hard | 0.71 | 0.889 (reto) | 4.72 | 0.05 | 0 | 65 | +(0.00) −11(23; 0.18) | 1 ronda(s) extra de candidatos; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 556 | 16 | comodidad | C | 8 | 2 (2) | 10 | walk | 20 | 6842 | 0.450 | [0.20, 0.60] | normal | — | -0.01 | 0.555 (comodidad) | 5.35 | 0.00 | 0 | 50 | −4(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 557 | 17 | comodidad | Br+Tu | 8 | 1 (1) | 10 | walk | 17 | 5796 | 0.325 | [0.20, 0.60] | normal | — | -0.22 | 0.367 (comodidad) | 6.29 | 0.00 | 0 | 49 | +(0.00) −12(16; 0.00) | turbia: 2 dosis ocultas en 1 cuia(s), 1 realidades |
| 558 | 18 | reto | Fl (D) | 9 | 2 (2) | 11 | fill | 27 | 1004 | 0.825 | [0.75, 0.90) | hard | hard | 0.74 | 0.896 (reto) | 3.15 | 0.08 | 0 | 66 | −1(24; 0.00) +(0.03) | relajacion: fill con 2 vacios (03 sec. 4.4); cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 559 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 17 | 1339 | 0.350 | [0.20, 0.40] | normal | — | -0.34 | 0.257 (comodidad) | 5.82 | 0.00 | 0 | 46 | −8(16; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 560 | 20 | hito | Fl+Re (MD) | 12 | 1 (1) | 13 | deep | 29 | 94244 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.28 | 0.973 (hito) | 5.07 | 0.35 | 7 | 70 | −13(21; 0.65) | risk_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos; alivio plus_empty no publicado (no baja bot_fail 0,20) |

## Par / 2K por fuente frente a 03 §18

| Fuente | Niveles | Par / 2K medio | bot_fail medio | 03 §18 (par / 2K) |
|---|---|---|---|---|
| deep | 20 | 1.25 | 0.68 | 1,17 |
| fill | 12 | 1.51 | 0.89 | 1,53 |
| walk | 108 | 1.08 | 0.39 | 1,09 |

## Debuts

| Nivel | intro_mechanic | skippable | Celda |
|---|---|---|---|

## Apariciones en los packs (debut real y huecos)

Sobre los tableros publicados, no sobre el calendario: primera aparición (debut real), niveles en los que sale, hueco máximo entre dos apariciones y niveles desde la última hasta el 700 (resumen, punto 6; d.2 regla 4: ≤ 120; level-qa lo exige con QA-GAP-120). Las capacidades cuentan también cuando el generador las usa como forma de tablero (02 §8); la cueva de la Piraña no cuenta como Cuia chica.

| Mecánica o capacidad | Debut del calendario | Debut real | Niveles | Hueco máximo | Hasta el final |
|---|---|---|---|---|---|
| hoja (Ho) | 43 | 425 | 17 | 28 (480→508) | 5 |
| matapi (Ma) | 63 | 423 | 10 | 43 (440→483) | 60 |
| liana (Li) | 83 | 463 | 5 | 31 (480→511) | 49 |
| murky (Tu) | 100 | 423 | 28 | 10 (435→445) | 3 |
| remolino (Re) | 103 | 429 | 11 | 37 (488→525) | 0 |
| barro (Ba) | 123 | 428 | 13 | 28 (520→548) | 12 |
| lluvia (Ll) | 143 | 503 | 6 | 31 (520→551) | 9 |
| brote (Br) | 163 | 445 | 8 | 37 (491→528) | 3 |
| pirana (Pi) | 183 | 443 | 9 | 63 (460→523) | 20 |
| liana_n (Ln) | 243 | 469 | 4 | 71 (478→549) | 8 |
| hoja_atada (Ha) | 283 | 437 | 12 | 28 (457→485) | 20 |
| brote_n2 (B2) | 343 | 431 | 8 | 34 (431→465) | 23 |
| flor (Fl) | 403 | 426 | 9 | 66 (432→498) | 0 |
| Cuia chica (cap 1-3) | 14 | 434 | 11 | 37 (488→525) | 6 |
| Cuia alta (cap 5-6) | 27 | 434 | 3 | 80 (434→514) | 6 |
| Ch1 (cap 1) | 18 | **no sale** | 0 | — | — |
| Ch3 (cap 3) | 22 | **no sale** | 0 | — | — |
| Al6 (cap 6) | 160 | **no sale** | 0 | — | — |

## Remanso frente a la comodidad del ciclo (c.4)

p19 es «puro, más fácil que la media de comodidad» (c.4; c.3 regla 5: respiro antes del pico): la colocación exige D(p19) < media de D de las comodidades del ciclo que no son tableros de estreno, y level-qa lo comprueba (QA-REMANSO).

| Ciclo | Remanso | D | bot_fail | Par | Comodidades | D medio | bot_fail medio | Puesto del remanso por D (de las comodidades y él) |
|---|---|---|---|---|---|---|---|---|
| 421-440 | 439 | -0.35 | 0.375 | 17 | 11 | -0.21 | 0.430 | 9 de 12 (1 = el más difícil) |
| 441-460 | 459 | -0.23 | 0.350 | 17 | 11 | -0.21 | 0.398 | 7 de 12 (1 = el más difícil) |
| 461-480 | 479 | -0.29 | 0.250 | 18 | 11 | -0.23 | 0.382 | 9 de 12 (1 = el más difícil) |
| 481-500 | 499 | -0.42 | 0.275 | 17 | 11 | -0.17 | 0.364 | 10 de 12 (1 = el más difícil) |
| 501-520 | 519 | -0.37 | 0.325 | 16 | 11 | -0.32 | 0.316 | 7 de 12 (1 = el más difícil) |
| 521-540 | 539 | -0.39 | 0.300 | 18 | 11 | -0.32 | 0.302 | 9 de 12 (1 = el más difícil) |
| 541-560 | 559 | -0.34 | 0.350 | 17 | 11 | -0.18 | 0.366 | 10 de 12 (1 = el más difícil) |

## Banda medida por rol (03 §7: «la banda coloca»)

La colocación exige, con el cuantil q de D en el pool del rango (recuperación ≤ 0,10, comodidad ≤ 0,70, reto ≤ 0,95, hito > 0,95): comodidad, estreno y remanso como mucho en banda de comodidad; recuperación (p1-p2, c.3 regla 3) en banda de recuperación o, como mínimo, con D por debajo de la comodidad más fácil del ciclo; hito (p20, «Pico» de c.3) en banda de hito o, como mínimo, con par − 2K ≥ la media del pool del rango; y en 21-40 los retos suaves por debajo del hito 40. level-qa lo comprueba regenerando la campaña (QA-BAND-Q y QA-LADDER).

| Ciclo | Recuperación p1 / p2: q (D) | Comodidad más fácil (D) | q máximo de comodidad y estreno | Remanso: q | Retos: D mín.-máx. | Hito: q (banda) · par − 2K / media del pool |
|---|---|---|---|---|---|---|
| 421-440 | 0.041 (-0.70) / 0.021 (-0.82) | -0.59 | 0.653 | 0.242 | 0.08 / 1.03 | 0.979 (hito) · 12 / 2.7 |
| 441-460 | 0.042 (-0.69) / 0.071 (-0.60) | -0.54 | 0.638 | 0.362 | 0.36 / 1.08 | 0.956 (hito) · 11 / 2.7 |
| 461-480 | 0.056 (-0.66) / 0.051 (-0.68) | -0.63 | 0.635 | 0.304 | 0.12 / 0.43 | 0.967 (hito) · 8 / 2.7 |
| 481-500 | 0.039 (-0.71) / 0.022 (-0.81) | -0.49 | 0.693 | 0.194 | 0.61 / 1.10 | 0.969 (hito) · 11 / 2.7 |
| 501-520 | 0.012 (-0.86) / 0.034 (-0.73) | -0.87 | 0.613 | 0.225 | 0.41 / 0.69 | 0.967 (hito) · 11 / 2.7 |
| 521-540 | 0.002 (-0.96) / 0.089 (-0.57) | -0.68 | 0.517 | 0.215 | 0.36 / 1.12 | 0.963 (hito) · 10 / 2.7 |
| 541-560 | 0.099 (-0.55) / 0.032 (-0.73) | -0.52 | 0.585 | 0.257 | 0.36 / 0.74 | 0.973 (hito) · 5 / 2.7 |

Todas las recuperaciones en banda de recuperación y todos los hitos en banda de hito.

## Vacíos de la comodidad y del remanso por rango (03 §4.4 y §11)

Comodidades sin estreno y remansos. «2 vacíos» = dos o más vacíos libres (sin mecánica, capacidad ≥ 4); «Cuia chica» = Cuia chica de 2 añadida como medio vacío por la relajación de 03 §4.4. Canon: 2 vacíos en 11-30; 1 desde el 31 (03 §11: «2 en respiros y recuperación» en 31-60, «2 en 30 % de comodidad» en 61-100 y 1 en 101-200; 03 §4.4: «1 (31+) con 2 en el 30 % de los slots») [P: se revisa con el tech-lead].

| Rango | Comodidades y remansos | Con 2 vacíos | Con Cuia chica de relajación | Relajados (%) | Canon |
|---|---|---|---|---|---|
| 401-700 | 84 | 27 | 9 | 43 % | 1 |

## Cuias por rango frente a 03 §11

Con 1 vacío, el K del rango que permiten los pares de riesgo (03 §4.5; `k_plan`) no siempre llega al mínimo de cuias de 03 §11 (aviso QA-CUIAS de level-qa) [P: k_plan, vacíos o la tabla se revisan en la integración].

| Rango | 03 §11 | Mínimo y máximo generados | Por debajo | Por encima | Niveles por debajo |
|---|---|---|---|---|---|
| 401-700 | 12-14 | 9-13 | 132 | 0 | 421 (10), 422 (10), 423 (10), 424 (9), 425 (10), 426 (11), 427 (10), 428 (10), 429 (11), 430 (9), 431 (10), 432 (11), 433 (10), 434 (10), 435 (11), 436 (9), 437 (10), 438 (11), 439 (10), 441 (10), 442 (10), 443 (10), 444 (9), 445 (10), 446 (11), 447 (10), 448 (10), 449 (11), 450 (9), 451 (10), 453 (10), 454 (10), 455 (11), 456 (10), 457 (10), 458 (11), 459 (10), 461 (10), 462 (10), 463 (10), 464 (10), 465 (10), 466 (11), 467 (9), 468 (10), 469 (11), 470 (9), 471 (10), 472 (11), 473 (10), 474 (10), 475 (11), 476 (9), 477 (10), 478 (11), 479 (10), 481 (10), 482 (10), 483 (10), 484 (10), 485 (10), 486 (11), 487 (10), 488 (10), 489 (11), 490 (10), 491 (10), 492 (11), 493 (9), 494 (9), 495 (11), 496 (10), 497 (10), 498 (11), 499 (10), 501 (10), 502 (10), 503 (10), 504 (9), 505 (10), 506 (11), 507 (10), 508 (10), 509 (11), 510 (10), 511 (10), 512 (11), 513 (10), 514 (10), 515 (11), 516 (10), 517 (10), 518 (11), 519 (10), 521 (10), 522 (10), 523 (10), 524 (10), 525 (10), 526 (11), 527 (9), 528 (10), 529 (11), 530 (9), 531 (10), 532 (11), 533 (9), 534 (10), 535 (11), 536 (10), 537 (10), 538 (11), 539 (10), 541 (10), 542 (10), 543 (9), 544 (10), 545 (10), 546 (11), 547 (10), 548 (11), 549 (11), 550 (10), 551 (10), 552 (10), 553 (10), 554 (10), 555 (11), 556 (10), 557 (10), 558 (11), 559 (10) |

## Saltos de banda (03 §12: +2,2 / −3,4)

Fuera del límite (blando, registrado): 439→440: sube 2.35 bandas (> 2.2); 499→500: sube 2.23 bandas (> 2.2).

## Variantes de la 1.0

Variantes de 02 §9.1 (propuesta de D-007 b.3; firma de `pack_schema` 3, D-023 punto 5) con los códigos de d.5.1 [P]: Ln = Liana de cuenta (`liana` con `n`; n = 1 en el estreno y 1-2 en los demás [P]), Ha = Hoja atada (`hoja_atada`, sobre una cuia con su fruta en el fondo y cap = U_c, D-024), B2 = Brote de dos hojas (`brote` con `n` = 2, 1-2 dosis) y Fl = Flor de vitória-régia (`flor` con `key` y `key2` distintas). Estreno (02 §9.2 regla 2): un ejemplar, 2 vacíos y `bot_fail` < 0,20.

| Variante | Debut | Niveles | Ejemplares | Tableros de estreno | Estrenos con un ejemplar, 2 vacíos y bot < 0,20 |
|---|---|---|---|---|---|
| Ln | 243 (`liana_n`) | 4 | 4 | 0 | 0 de 0 |
| Ha | 283 (`hoja_atada`) | 12 | 12 | 0 | 0 de 0 |
| B2 | 343 (`brote_n2`) | 8 | 8 | 0 | 0 de 0 |
| Fl | 403 (`flor`) | 9 | 9 | 0 | 0 de 0 |

| Nivel | p | Rol | Celda | Variante | Ejemplares de mecánica | Vacíos | bot_fail |
|---|---|---|---|---|---|---|---|
| 426 | 6 | reto | Fl+Ma (D) | flor key=3 key2=1 (4 dosis) | 2 | 2 | 0.750 |
| 431 | 11 | comodidad | B2 | brote n=2 (1 dosis) | 1 | 1 | 0.525 |
| 432 | 12 | reto | Fl+Ho (D) | flor key=2 key2=3 (4 dosis) | 2 | 2 | 0.850 |
| 437 | 17 | comodidad | Ha | hoja_atada key=4 (4 dosis) | 1 | 1 | 0.575 |
| 457 | 17 | comodidad | Ha | hoja_atada key=14 (4 dosis) | 1 | 1 | 0.400 |
| 465 | 5 | comodidad | B2 | brote n=2 (1 dosis) | 1 | 1 | 0.225 |
| 466 | 6 | reto | Li+B2 (D) | brote n=2 (2 dosis) | 2 | 1 | 0.725 |
| 469 | 9 | reto | Ln+B2 (D) | liana n=2 (3 dosis); brote n=2 (1 dosis) | 2 | 1 | 0.750 |
| 478 | 18 | reto | Ln+Ho (D) | liana n=2 (4 dosis) | 2 | 2 | 0.800 |
| 480 | 20 | hito | Li+B2+Ho (MD) | brote n=2 (2 dosis) | 3 | 1 | 1.000 |
| 485 | 5 | comodidad | Ha | hoja_atada key=2 (4 dosis) | 1 | 1 | 0.275 |
| 486 | 6 | reto | Ma+Ha (D) | hoja_atada key=12 (4 dosis) | 2 | 2 | 0.800 |
| 492 | 12 | reto | Ha+Ba (D) | hoja_atada key=5 (4 dosis) | 2 | 1 | 0.800 |
| 497 | 17 | comodidad | B2 | brote n=2 (2 dosis) | 1 | 1 | 0.250 |
| 498 | 18 | reto | Fl+Ha (D) | hoja_atada key=11 (4 dosis); flor key=5 key2=8 (4 dosis) | 2 | 2 | 0.850 |
| 500 | 20 | hito | Ma+Ha (MD) | hoja_atada key=13 (4 dosis) | 2 | 1 | 0.975 |
| 509 | 9 | reto | Fl (D) | flor key=13 key2=12 (4 dosis) | 1 | 2 | 0.825 |
| 512 | 12 | reto | Ll+Ha (D) | hoja_atada key=2 (3 dosis) | 2 | 2 | 0.875 |
| 517 | 17 | comodidad | B2 | brote n=2 (1 dosis) | 1 | 1 | 0.250 |
| 518 | 18 | reto | Fl+Ba (D) | flor key=10 key2=13 (4 dosis) | 2 | 1 | 0.800 |
| 520 | 20 | hito | Ll+Ba+Ha (MD) | hoja_atada key=14 (4 dosis) | 3 | 1 | 0.950 |
| 526 | 6 | reto | Pi+Ha (D) | hoja_atada key=9 (4 dosis) | 2 | 1 | 0.800 |
| 537 | 17 | comodidad | B2 | brote n=2 (2 dosis) | 1 | 1 | 0.225 |
| 538 | 18 | reto | Ha+Re (D) | hoja_atada key=3 (4 dosis) | 2 | 2 | 0.800 |
| 540 | 20 | hito | Pi+Ha (MD) | hoja_atada key=5 (4 dosis) | 2 | 1 | 0.975 |
| 543 | 3 | comodidad | Fl | flor key=10 key2=12 (4 dosis) | 1 | 1 | 0.475 |
| 546 | 6 | reto | Fl+Ho (D) | flor key=8 key2=5 (4 dosis) | 2 | 2 | 0.800 |
| 549 | 9 | reto | Ln+Ho (D) | liana n=2 (4 dosis) | 2 | 2 | 0.850 |
| 552 | 12 | reto | Ln+Re (D) | liana n=1 (4 dosis) | 2 | 1 | 0.850 |
| 558 | 18 | reto | Fl (D) | flor key=9 key2=7 (4 dosis) | 1 | 2 | 0.825 |
| 560 | 20 | hito | Fl+Re (MD) | flor key=6 key2=12 (4 dosis) | 2 | 1 | 1.000 |

Todas las celdas con una variante la llevan en el nivel publicado.
