# Campaña 301-700: informe de `level-generator --spec`

Generado por `dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/campaign_v3.json --levels 301-700 --out tools/out/curva_f3b --report tools/out/curva_f3b/curva_report.md`: no se edita a mano y no lleva tiempos. Generador 1.0.0, `pack_schema` 3, modo de vertido `run` (D-005).

Fuentes: D-009 (ciclo escalonado), D-007 (debuts) y la tabla de novedades de 201-700 de D2 (D-023 punto 10) con la propuesta `docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md` (c.1-c.4, d.1-d.3 y d.5), D-019 (Tinaja) y D-023 punto 5 (variantes de la 1.0); GDD 03 §2, §4, §7-§13 y §17; 02 §8-§11; 08 §4.1. Todo se mide con `AF.Core` y `AF.Solver`: A* w = 1 a 300.000 (par), bot ×40 con la semilla del nivel (`bot_fail` y etiqueta), `EarlyTraps` (trampas tempranas, Q16 B), ruta de referencia para ramificación y trampas (`--parity`), `SafePath` en Aguas turbias y `LevelValidation` en cada tablero.

## Paquetes

| Pack | Niveles | Bytes | Bytes sin relief | Bytes sin relief por 100 niveles (≤ 100 KB, 03 §17 #11) | SHA-256 |
|---|---|---|---|---|---|
| `tools/out/curva_f3b/curva_07_v1.json` | 120 | 165753 | 104171 | 84.8 KB | `6411c254983ad648901a8c9607e2f9f0d3848521a14d6a1267f37fd843ea8ac7` |
| `tools/out/curva_f3b/curva_08_v1.json` | 140 | 192119 | 121845 | 85.0 KB | `855e295c291a761ac97e4c973128158e0b18f053ebe7e8ed2cb6b75b9ccf6864` |
| `tools/out/curva_f3b/curva_09_v1.json` | 140 | 191864 | 122053 | 85.1 KB | `fd0919117c3f4b2640857f5a52c7d3d14fa6eaf88f03f46e489aa2392b27b51e` |

## Pool por rango e índice D (03 §7)

D = 0,35·z(bot_fail) + 0,25·z(par − 2K) + 0,20·z(ramificación) + 0,10·z(trampas) + 0,10·[E libres = 1], con media y desviación del pool del rango (todos los candidatos en ventana de sus slots; congelados tras la primera pasada). Banda = cuantil de D en el pool: recuperación ≤ 0,10, comodidad ≤ 0,70, reto ≤ 0,95, hito > 0,95.

| Rango | Slots | Pool | Pool / slots | bot_fail μ ± σ | par − 2K μ ± σ | Ramif. μ ± σ | Trampas μ ± σ |
|---|---|---|---|---|---|---|---|
| 201-400 | 100 | 382 | 3.8 | 0.45 ± 0.30 | 2.5 ± 2.7 | 5.55 ± 1.60 | 0.04 ± 0.07 |
| 401-700 | 300 | 1229 | 4.1 | 0.50 ± 0.28 | 2.7 ± 2.7 | 5.49 ± 1.76 | 0.05 ± 0.08 |

## Etiquetas por rango frente a 03 §11

| Rango (niveles generados) | Difícil | Muy difícil | 03 §11 | Aviso (> 5 puntos) |
|---|---|---|---|---|
| 201-400 (100) | 23 (23.0 %) | 5 (5.0 %) | 25-25 % / 5 % | — |
| 401-700 (300) | 70 (23.3 %) | 15 (5.0 %) | 25-25 % / 5 % | — |

Nota: con D-009 desde el 61 los cinco retos apuntan a Difícil y el hito a Muy difícil (25 % y 5 % por ciclo), por encima del 15 % de 03 §11 para 61-100: es la cuenta del ciclo, no un error de la generación [P: la tabla de 03 §11 se revisa en la integración].

## Ciclos

| Ciclo | Temporada | Puros | Difícil | Muy difícil | Objetivo D / MD | Acierto de etiqueta (c.7) | Trampas tempranas | Relajaciones |
|---|---|---|---|---|---|---|---|---|
| 301-320 | A | 7/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 0 | 7 |
| 321-340 | B | 8/20 | 4 | 1 | 5 / 1 | 5/6 (83 %) | 0 | 10 |
| 341-360 | A | 6/20 | 4 | 1 | 5 / 1 | 5/6 (83 %) | 1 | 7 |
| 361-380 | B | 6/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 2 | 4 |
| 381-400 | A | 6/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 8 | 13 |
| 401-420 | A | 6/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 5 | 8 |
| 421-440 | B | 7/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 2 | 8 |
| 441-460 | B | 6/20 | 4 | 1 | 5 / 1 | 5/6 (83 %) | 4 | 11 |
| 461-480 | A | 7/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 0 | 5 |
| 481-500 | B | 8/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 5 | 11 |
| 501-520 | A | 7/20 | 4 | 1 | 5 / 1 | 5/6 (83 %) | 0 | 11 |
| 521-540 | B | 6/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 5 | 5 |
| 541-560 | A | 8/20 | 4 | 1 | 5 / 1 | 5/6 (83 %) | 2 | 13 |
| 561-580 | B | 8/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 0 | 9 |
| 581-600 | B | 7/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 4 | 8 |
| 601-620 | A | 6/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 6 | 8 |
| 621-640 | B | 7/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 3 | 9 |
| 641-660 | B | 6/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 4 | 5 |
| 661-680 | A | 7/20 | 4 | 1 | 5 / 1 | 5/6 (83 %) | 1 | 10 |
| 681-700 | B | 8/20 | 4 | 1 | 5 / 1 | 5/6 (83 %) | 2 | 9 |

## Niveles

Columnas: p = posición en el ciclo (0 en la rampa); E = vacíos de capacidad ≥ 4 (con la Hoja o el Matapi vacíos), libres = sin mecánica; q = cuantil de D en el pool del rango (3 decimales: la banda de hito empieza por encima de 0,95) y su banda; TT = trampas tempranas; alivio = tipos publicados en `relief` en orden (− = minus_color, + = plus_empty).

| Nivel | p | Rol | Calendario | K | E (libres) | Cuias | Fuente | Par | Estados A* | bot_fail | Ventana | Etiqueta | Objetivo | D | q (banda) | Ramif. | Trampas | TT | Compl. | Alivio | Notas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 301 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 2565 | 0.000 | [0.00, 0.30) | normal | — | -0.78 | 0.038 (recuperacion) | 5.88 | 0.00 | 0 | 42 | −9(14; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 302 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 18 | 1921 | 0.000 | [0.00, 0.30) | normal | — | -0.82 | 0.020 (recuperacion) | 4.06 | 0.00 | 0 | 46 | −3(17; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 303 | 3 | estreno | *Ag | 8 | 2 (0) | 10 | walk | 16 | 1017 | 0.000 | [0.00, 0.20) | normal | — | -0.81 | 0.027 (recuperacion) | 4.81 | 0.08 | 0 | 42 | −4(14; 0.10) +(0.03) |  |
| 304 | 4 | estreno | *Ag | 8 | 2 (1) | 10 | walk | 17 | 4771 | 0.050 | [0.00, 0.20) | normal | — | -0.34 | 0.281 (comodidad) | 7.35 | 0.00 | 0 | 46 | −1(16; 0.20) +(0.13) |  |
| 305 | 5 | estreno | *Ag | 8 | 2 (1) | 10 | walk | 16 | 2005 | 0.150 | [0.00, 0.20) | normal | — | -0.34 | 0.289 (comodidad) | 6.81 | 0.04 | 0 | 44 | −8(14; 0.05) +(0.05) |  |
| 306 | 6 | reto | Ha+Ll (D) | 9 | 2 (1) | 11 | walk | 20 | 25866 | 0.800 | [0.75, 0.90) | hard | hard | 0.41 | 0.758 (reto) | 4.90 | 0.06 | 0 | 48 | +(0.20) −3(18; 0.13) | relajacion: walk con 1 vacio; cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 307 | 7 | comodidad | C | 8 | 1 (1) | 9 | walk | 16 | 150 | 0.375 | [0.20, 0.60] | normal | — | -0.54 | 0.137 (comodidad) | 2.31 | 0.11 | 0 | 44 | +(0.00) −1(14; 0.00) | cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 308 | 8 | comodidad | Ag | 8 | 1 (0) | 10 | walk | 16 | 2553 | 0.500 | [0.20, 0.60] | normal | — | -0.17 | 0.436 (comodidad) | 5.81 | 0.02 | 0 | 40 | −12(14; 0.05) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4); alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 309 | 9 | reto | Ag+Ll (D) | 9 | 2 (0) | 11 | fill | 24 | 10217 | 0.875 | [0.75, 0.90) | hard | hard | 0.52 | 0.797 (reto) | 3.13 | 0.04 | 0 | 62 | +(0.43) −11(21; 0.00) | Aguape con una mecanica que ocupa el vacio: un vacio mas, bajo la mata; 1 ronda(s) extra de candidatos |
| 310 | 10 | comodidad | C | 8 | 2 (2) | 10 | walk | 17 | 1829 | 0.375 | [0.20, 0.60] | normal | — | -0.15 | 0.454 (comodidad) | 6.65 | 0.00 | 0 | 46 | −6(16; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 311 | 11 | comodidad | Ba | 8 | 1 (1) | 10 | walk | 16 | 1309 | 0.300 | [0.20, 0.60] | normal | — | -0.25 | 0.355 (comodidad) | 6.13 | 0.03 | 0 | 50 | +(0.00) −2(15; 0.00) | cvd_adjacent: 1 contacto(s) |
| 312 | 12 | reto | Ag+Br (D) | 9 | 1 (0) | 11 | deep | 23 | 971 | 0.850 | [0.75, 0.90) | hard | hard | 0.59 | 0.821 (reto) | 3.22 | 0.18 | 0 | 58 | +(0.53) −2(21; 0.58) | 1 ronda(s) extra de candidatos |
| 313 | 13 | comodidad | C | 8 | 1 (1) | 9 | walk | 17 | 264 | 0.525 | [0.20, 0.60] | normal | — | -0.28 | 0.336 (comodidad) | 2.71 | 0.07 | 0 | 46 | +(0.08) −4(16; 0.03) | cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 314 | 14 | comodidad | Ag | 8 | 1 (0) | 10 | walk | 16 | 1790 | 0.425 | [0.20, 0.60] | normal | — | -0.11 | 0.483 (comodidad) | 7.13 | 0.01 | 0 | 44 | +(0.13) −4(15; 0.00) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4) |
| 315 | 15 | reto | Br+Tu (D) | 9 | 0 (0) | 10 | deep | 22 | 253 | 0.875 | [0.75, 0.90) | hard | hard | 0.70 | 0.852 (reto) | 2.77 | 0.34 | 0 | 60 | +(0.28) −5(21; 0.00) | 1 ronda(s) extra de candidatos; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 316 | 16 | comodidad | C | 8 | 2 (2) | 10 | walk | 17 | 2258 | 0.200 | [0.20, 0.60] | normal | — | -0.42 | 0.219 (comodidad) | 5.53 | 0.05 | 0 | 46 | −3(16; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 317 | 17 | comodidad | Ll | 8 | 2 (1) | 10 | walk | 20 | 31948 | 0.425 | [0.20, 0.60] | normal | — | 0.14 | 0.664 (comodidad) | 5.30 | 0.01 | 0 | 52 | +(0.00) −7(19; 0.00) |  |
| 318 | 18 | reto | Ag+Ln (D) | 9 | 2 (1) | 11 | fill | 26 | 2928 | 0.800 | [0.75, 0.90) | hard | hard | 0.87 | 0.898 (reto) | 3.77 | 0.09 | 0 | 64 | +(0.18) −13(24; 0.45) | relajacion: fill con 2 vacios (03 sec. 4.4); 2 ronda(s) extra de candidatos |
| 319 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 18 | 2944 | 0.325 | [0.20, 0.40] | normal | — | -0.44 | 0.195 (comodidad) | 4.00 | 0.00 | 0 | 48 | −13(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 320 | 20 | hito | Ha+Ll+Br (MD) | 11 | 2 (1) | 13 | deep | 25 | 84465 | 0.925 | [0.90, 1.00] | very_hard | very_hard | 1.02 | 0.935 (reto) | 7.88 | 0.06 | 0 | 68 | +(0.35) −1(22; 0.08) | cvd_adjacent: 2 contacto(s); 1 ronda(s) extra de candidatos; 6 ronda(s) de banda |
| 321 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1482 | 0.000 | [0.00, 0.30) | normal | — | -0.80 | 0.033 (recuperacion) | 5.69 | 0.00 | 0 | 44 | −9(15; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 322 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 17 | 1746 | 0.025 | [0.00, 0.30) | normal | — | -0.91 | 0.013 (recuperacion) | 3.82 | 0.00 | 0 | 46 | −1(16; 0.00) +(0.00) | 1 ronda(s) extra de candidatos; 1 ronda(s) de banda |
| 323 | 3 | estreno | *Tu | 7 | 2 (2) | 10 | walk | 14 | 4098 | 0.000 | [0.00, 0.20) | normal | — | -0.68 | 0.064 (recuperacion) | 6.64 | 0.00 | 0 | 41 | −4(12; 0.00) +(0.00) | turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 324 | 4 | estreno | *Tu | 7 | 2 (2) | 10 | walk | 14 | 136 | 0.000 | [0.00, 0.20) | normal | — | -0.73 | 0.056 (recuperacion) | 6.29 | 0.00 | 0 | 45 | −5(13; 0.00) +(0.00) | turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 325 | 5 | comodidad | Ba | 8 | 1 (1) | 10 | walk | 16 | 747 | 0.325 | [0.20, 0.60] | normal | — | -0.24 | 0.376 (comodidad) | 5.00 | 0.13 | 0 | 48 | +(0.00) −13(15; 0.00) |  |
| 326 | 6 | reto | Ag+Ma (D) | 9 | 3 (1) | 12 | walk | 20 | 34986 | 0.750 | [0.75, 0.90) | hard | hard | 0.62 | 0.827 (reto) | 7.20 | 0.05 | 0 | 54 | −5(19; 0.48) | relajacion: walk con 1 vacio; Aguape con una mecanica que ocupa el vacio: un vacio mas, bajo la mata; 1 ronda(s) extra de candidatos; alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 327 | 7 | comodidad | C | 8 | 1 (1) | 9 | walk | 16 | 188 | 0.500 | [0.20, 0.60] | normal | — | -0.41 | 0.229 (comodidad) | 2.56 | 0.07 | 0 | 42 | +(0.00) −2(14; 0.00) | 1 ronda(s) extra de candidatos |
| 328 | 8 | comodidad | C | 8 | 2 (2) | 10 | walk | 17 | 2357 | 0.375 | [0.20, 0.60] | normal | — | -0.02 | 0.562 (comodidad) | 7.65 | 0.00 | 0 | 46 | −5(16; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 329 | 9 | reto | Ma (D) | 10 | 2 (1) | 12 | walk | 25 | 36861 | 0.725 | [0.75, 0.90) | normal | hard | 0.63 | 0.830 (reto) | 5.44 | 0.03 | 0 | 64 | +(0.03) −6(23; 0.00) | relajacion: walk con 1 vacio; fuera de ventana (relajacion agotada); 2 ronda(s) extra de candidatos |
| 330 | 10 | comodidad | C | 8 | 1 (1) | 9 | walk | 16 | 181 | 0.450 | [0.20, 0.60] | normal | — | -0.49 | 0.158 (comodidad) | 2.69 | 0.05 | 0 | 46 | +(0.00) −10(15; 0.00) | 1 ronda(s) extra de candidatos |
| 331 | 11 | comodidad | Ma+Tu | 8 | 1 (0) | 10 | deep | 19 | 5223 | 0.375 | [0.20, 0.60] | normal | — | -0.38 | 0.242 (comodidad) | 3.26 | 0.00 | 0 | 52 | +(0.00) −2(18; 0.00) | endurecimiento: deep; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 332 | 12 | reto | Ba+Ho (D) | 9 | 1 (0) | 11 | walk | 23 | 3634 | 0.850 | [0.75, 0.90) | hard | hard | 0.68 | 0.842 (reto) | 5.09 | 0.07 | 0 | 61 | +(0.28) −4(22; 0.33) | relajacion: walk con 1 vacio; 1 ronda(s) extra de candidatos |
| 333 | 13 | comodidad | C | 8 | 2 (2) | 10 | walk | 19 | 4017 | 0.300 | [0.20, 0.60] | normal | — | -0.16 | 0.441 (comodidad) | 5.74 | 0.00 | 0 | 48 | −1(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 334 | 14 | comodidad | Ag | 8 | 1 (0) | 10 | walk | 18 | 6288 | 0.525 | [0.20, 0.60] | normal | — | 0.05 | 0.614 (comodidad) | 5.94 | 0.01 | 0 | 44 | +(0.25) −3(16; 0.18) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4) |
| 335 | 15 | reto | Ma+Ho (D) | 9 | 2 (0) | 11 | fill | 30 | 26408 | 0.875 | [0.75, 0.90) | hard | hard | 1.11 | 0.954 (hito) | 3.17 | 0.05 | 0 | 68 | +(0.20) −3(25; 0.25) | 1 ronda(s) extra de candidatos |
| 336 | 16 | comodidad | C | 8 | 2 (2) | 10 | walk | 16 | 1690 | 0.250 | [0.20, 0.60] | normal | — | -0.45 | 0.190 (comodidad) | 6.13 | 0.00 | 0 | 44 | −9(15; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 337 | 17 | comodidad | Ba | 8 | 1 (1) | 10 | walk | 18 | 1155 | 0.200 | [0.20, 0.60] | normal | — | -0.30 | 0.315 (comodidad) | 5.50 | 0.00 | 0 | 51 | +(0.00) −6(17; 0.00) |  |
| 338 | 18 | reto | Ag+Ba (D) | 9 | 1 (1) | 11 | fill | 30 | 5126 | 0.825 | [0.75, 0.90) | hard | hard | 1.14 | 0.965 (hito) | 2.77 | 0.08 | 0 | 73 | +(0.20) −4(26; 0.00) | relajacion: fill con 2 vacios (03 sec. 4.4); 1 ronda(s) extra de candidatos |
| 339 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 20 | 4454 | 0.325 | [0.20, 0.40] | normal | — | -0.33 | 0.293 (comodidad) | 3.25 | 0.02 | 0 | 50 | −9(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 340 | 20 | hito | Ag+Br+Ba (MD) | 11 | 1 (0) | 13 | deep | 27 | 5358 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.20 | 0.974 (hito) | 6.93 | 0.15 | 0 | 72 | +(0.63) −1(24; 0.35) | 1 ronda(s) extra de candidatos |
| 341 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 18 | 3346 | 0.000 | [0.00, 0.30) | normal | — | -0.69 | 0.062 (recuperacion) | 5.06 | 0.00 | 0 | 46 | −6(16; 0.00) +(0.00) |  |
| 342 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 17 | 1058 | 0.000 | [0.00, 0.30) | normal | — | -0.81 | 0.026 (recuperacion) | 4.88 | 0.00 | 0 | 46 | −12(16; 0.00) +(0.00) | 1 ronda(s) de banda |
| 343 | 3 | estreno | *B2 | 7 | 2 (2) | 10 | walk | 14 | 961 | 0.000 | [0.00, 0.20) | normal | — | -0.66 | 0.069 (recuperacion) | 6.79 | 0.00 | 0 | 40 | −10(13; 0.00) +(0.00) |  |
| 344 | 4 | estreno | *B2 | 7 | 2 (2) | 10 | walk | 15 | 1066 | 0.000 | [0.00, 0.20) | normal | — | -0.55 | 0.119 (comodidad) | 6.93 | 0.00 | 0 | 47 | −8(14; 0.00) +(0.00) |  |
| 345 | 5 | estreno | *B2 | 7 | 2 (2) | 10 | walk | 15 | 1242 | 0.000 | [0.00, 0.20) | normal | — | -0.69 | 0.059 (recuperacion) | 5.80 | 0.00 | 0 | 42 | −13(14; 0.00) +(0.00) | cvd_adjacent: 1 contacto(s) |
| 346 | 6 | reto | B2+Ln (D) | 10 | 1 (1) | 12 | deep | 24 | 8023 | 0.775 | [0.75, 0.90) | hard | hard | 0.57 | 0.810 (reto) | 4.88 | 0.07 | 0 | 64 | −3(22; 0.00) | alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 347 | 7 | estreno | *A2 | 8 | 2 (0) | 10 | walk | 19 | 4489 | 0.100 | [0.00, 0.20) | normal | — | -0.41 | 0.224 (comodidad) | 5.05 | 0.05 | 0 | 48 | −1(18; 0.40) +(0.28) |  |
| 348 | 8 | estreno | *A2 | 7 | 2 (0) | 9 | walk | 14 | 185 | 0.100 | [0.00, 0.20) | normal | — | -0.74 | 0.055 (recuperacion) | 3.50 | 0.16 | 0 | 38 | −3(13; 0.03) +(0.65) | relajacion: K - 1; 2 ronda(s) de banda |
| 349 | 9 | reto | B2 (D) | 10 | 1 (1) | 12 | deep | 21 | 13161 | 0.800 | [0.75, 0.90) | hard | hard | 0.60 | 0.823 (reto) | 7.19 | 0.06 | 0 | 61 | +(0.05) −1(20; 0.10) | risk_adjacent: 1 contacto(s); cvd_adjacent: 1 contacto(s) |
| 350 | 10 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 16 | 2149 | 0.225 | [0.20, 0.60] | normal | — | -0.26 | 0.349 (comodidad) | 7.13 | 0.00 | 0 | 49 | +(0.00) −1(15; 0.00) | turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 351 | 11 | comodidad | Ba+Tu | 8 | 1 (1) | 10 | walk | 16 | 1155 | 0.225 | [0.20, 0.60] | normal | — | -0.60 | 0.109 (comodidad) | 4.38 | 0.00 | 0 | 50 | +(0.00) −3(14; 0.00) | turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 352 | 12 | reto | B2+Re (D) | 9 | 1 (1) | 11 | deep | 23 | 274112 | 0.575 | [0.75, 0.90) | normal | hard | 0.69 | 0.848 (reto) | 7.61 | 0.01 | 0 | 58 | +(0.00) −6(21; 0.00) | fuera de ventana (relajacion agotada); 1 ronda(s) extra de candidatos |
| 353 | 13 | comodidad | C | 8 | 1 (1) | 9 | walk | 16 | 179 | 0.600 | [0.20, 0.60] | normal | — | -0.35 | 0.266 (comodidad) | 2.56 | 0.02 | 0 | 40 | +(0.00) −2(14; 0.00) |  |
| 354 | 14 | comodidad | C | 8 | 1 (1) | 9 | walk | 16 | 257 | 0.375 | [0.20, 0.60] | normal | — | -0.55 | 0.124 (comodidad) | 2.94 | 0.04 | 0 | 42 | +(0.00) −8(15; 0.00) |  |
| 355 | 15 | reto | B2+Re (D) | 9 | 1 (1) | 11 | deep | 21 | 17999 | 0.875 | [0.75, 0.90) | hard | hard | 0.89 | 0.902 (reto) | 7.71 | 0.03 | 0 | 56 | +(0.00) −1(19; 0.00) | endurecimiento: deep; risk_adjacent: 1 contacto(s) |
| 356 | 16 | comodidad | C | 8 | 2 (2) | 10 | walk | 17 | 1842 | 0.475 | [0.20, 0.60] | normal | — | -0.05 | 0.533 (comodidad) | 6.47 | 0.00 | 0 | 46 | −4(16; 0.00) +(0.15) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 357 | 17 | comodidad | Br+Tu | 8 | 1 (1) | 10 | walk | 17 | 1867 | 0.275 | [0.20, 0.60] | normal | — | -0.01 | 0.572 (comodidad) | 7.88 | 0.00 | 0 | 50 | +(0.00) −12(16; 0.00) | turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 358 | 18 | reto | Ag+Ln (D) | 9 | 2 (1) | 11 | fill | 28 | 5181 | 0.825 | [0.75, 0.90) | hard | hard | 1.02 | 0.933 (reto) | 3.39 | 0.07 | 0 | 66 | +(0.40) −8(25; 0.50) | relajacion: fill con 2 vacios (03 sec. 4.4) |
| 359 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 17 | 3633 | 0.275 | [0.20, 0.40] | normal | — | -0.34 | 0.284 (comodidad) | 6.06 | 0.00 | 0 | 46 | −1(16; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); cvd_adjacent: 1 contacto(s) |
| 360 | 20 | hito | B2+Ln+Ag (MD) | 11 | 1 (0) | 13 | deep | 27 | 2134 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.33 | 0.984 (hito) | 5.37 | 0.39 | 1 | 73 | +(0.15) −4(25; 0.10) | relajacion: deep con 60.000 nodos; 2 ronda(s) de banda |
| 361 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 19 | 3128 | 0.025 | [0.00, 0.30) | normal | — | -0.65 | 0.075 (recuperacion) | 4.42 | 0.00 | 0 | 48 | −5(17; 0.00) +(0.00) | 2 ronda(s) extra de candidatos |
| 362 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 17 | 3332 | 0.000 | [0.00, 0.30) | normal | — | -0.74 | 0.054 (recuperacion) | 5.41 | 0.00 | 0 | 44 | −7(16; 0.00) +(0.00) | 2 ronda(s) extra de candidatos |
| 363 | 3 | estreno | *La | 6 | 2 (2) | 10 | walk | 13 | 1542 | 0.000 | [0.00, 0.20) | normal | — | -0.62 | 0.090 (recuperacion) | 6.38 | 0.00 | 0 | 42 | −1(12; 0.00) +(0.00) |  |
| 364 | 4 | estreno | *La | 6 | 2 (2) | 10 | walk | 14 | 85 | 0.000 | [0.00, 0.20) | normal | — | -0.63 | 0.088 (recuperacion) | 5.57 | 0.00 | 0 | 45 | −14(13; 0.00) +(0.00) |  |
| 365 | 5 | estreno | *La | 6 | 2 (2) | 10 | walk | 12 | 113 | 0.000 | [0.00, 0.20) | normal | — | -0.88 | 0.017 (recuperacion) | 5.08 | 0.00 | 0 | 36 | −4(11; 0.00) +(0.00) |  |
| 366 | 6 | reto | Ho+Ag+Tu (D) | 9 | 2 (0) | 12 | deep | 20 | 2968 | 0.875 | [0.75, 0.90) | hard | hard | 0.25 | 0.725 (reto) | 3.85 | 0.05 | 0 | 57 | +(0.68) −14(19; 0.03) | Aguape con una mecanica que ocupa el vacio: un vacio mas, bajo la mata; 2 ronda(s) extra de candidatos; turbia: 3 dosis ocultas en 2 cuia(s), 4 realidades |
| 367 | 7 | comodidad | C | 8 | 2 (2) | 10 | walk | 19 | 2507 | 0.300 | [0.20, 0.60] | normal | — | -0.02 | 0.567 (comodidad) | 6.42 | 0.04 | 0 | 50 | −4(18; 0.00) +(0.03) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 2 ronda(s) extra de candidatos |
| 368 | 8 | comodidad | Ag+Tu | 8 | 1 (0) | 10 | walk | 17 | 2096 | 0.200 | [0.20, 0.60] | normal | — | -0.30 | 0.321 (comodidad) | 6.06 | 0.10 | 0 | 46 | −11(13; 0.00) | turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades; alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 369 | 9 | reto | Pi+Ma (D) | 9 | 1 (0) | 11 | deep | 22 | 90570 | 0.750 | [0.75, 0.90) | hard | hard | 0.27 | 0.725 (reto) | 4.18 | 0.00 | 0 | 56 | +(0.03) −2(18; 0.00) | cvd_adjacent: 1 contacto(s); 2 ronda(s) extra de candidatos |
| 370 | 10 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 16 | 802 | 0.225 | [0.20, 0.60] | normal | — | -0.07 | 0.517 (comodidad) | 8.63 | 0.00 | 0 | 52 | +(0.00) −4(15; 0.00) | 2 ronda(s) extra de candidatos; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 371 | 11 | comodidad | La | 8 | 1 (1) | 11 | walk | 16 | 1116 | 0.350 | [0.20, 0.60] | normal | — | -0.17 | 0.431 (comodidad) | 6.63 | 0.00 | 0 | 50 | +(0.00) −3(15; 0.00) | cvd_adjacent: 2 contacto(s) |
| 372 | 12 | reto | La+B2 (D) | 9 | 1 (1) | 12 | deep | 24 | 1968 | 0.850 | [0.75, 0.90) | hard | hard | 0.66 | 0.836 (reto) | 3.29 | 0.08 | 0 | 63 | +(0.18) −14(22; 0.13) | cvd_adjacent: 1 contacto(s); 2 ronda(s) extra de candidatos |
| 373 | 13 | comodidad | C | 8 | 2 (2) | 10 | walk | 17 | 2305 | 0.450 | [0.20, 0.60] | normal | — | -0.07 | 0.522 (comodidad) | 6.59 | 0.00 | 0 | 46 | −1(15; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 2 ronda(s) extra de candidatos |
| 374 | 14 | comodidad | La+Al | 8 | 1 (1) | 11 | walk | 20 | 36017 | 0.275 | [0.20, 0.60] | normal | — | -0.01 | 0.575 (comodidad) | 5.55 | 0.01 | 0 | 52 | +(0.00) −7(18; 0.00) |  |
| 375 | 15 | reto | La+Ba (D) | 9 | 1 (1) | 12 | deep | 23 | 11802 | 0.875 | [0.75, 0.90) | hard | hard | 0.74 | 0.861 (reto) | 4.22 | 0.09 | 0 | 65 | +(0.00) −12(22; 0.00) | 2 ronda(s) extra de candidatos |
| 376 | 16 | comodidad | C | 8 | 1 (1) | 9 | walk | 17 | 184 | 0.475 | [0.20, 0.60] | normal | — | -0.20 | 0.407 (comodidad) | 3.41 | 0.10 | 0 | 44 | +(0.00) −4(16; 0.00) | 2 ronda(s) extra de candidatos |
| 377 | 17 | comodidad | Br+Tu | 8 | 1 (1) | 10 | walk | 18 | 3083 | 0.350 | [0.20, 0.60] | normal | — | 0.07 | 0.635 (comodidad) | 7.11 | 0.00 | 0 | 53 | +(0.00) −1(17; 0.00) | turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 378 | 18 | reto | La+Ho (D) | 9 | 1 (0) | 12 | deep | 24 | 5290 | 0.750 | [0.75, 0.90) | hard | hard | 0.76 | 0.861 (reto) | 6.33 | 0.03 | 0 | 60 | +(0.03) −8(21; 0.00) | endurecimiento: deep; 2 ronda(s) extra de candidatos |
| 379 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 17 | 2141 | 0.225 | [0.20, 0.40] | normal | — | -0.30 | 0.318 (comodidad) | 6.82 | 0.00 | 0 | 46 | −5(16; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 2 ronda(s) extra de candidatos |
| 380 | 20 | hito | Pi+Ba+Ag (MD) | 11 | 0 (0) | 13 | fill | 35 | 20155 | 0.925 | [0.90, 1.00] | very_hard | very_hard | 1.20 | 0.975 (hito) | 3.14 | 0.01 | 2 | 89 | +(0.20) −1(32; 0.03) | 2 ronda(s) extra de candidatos |
| 381 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 18 | 1629 | 0.025 | [0.00, 0.30) | normal | — | -0.68 | 0.067 (recuperacion) | 4.94 | 0.00 | 0 | 48 | −2(17; 0.00) +(0.00) |  |
| 382 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1573 | 0.050 | [0.00, 0.30) | normal | — | -0.96 | 0.007 (recuperacion) | 3.94 | 0.00 | 0 | 44 | −6(15; 0.00) +(0.00) |  |
| 383 | 3 | comodidad | Li | 8 | 2 (2) | 10 | walk | 17 | 1087 | 0.500 | [0.20, 0.60] | normal | — | -0.19 | 0.420 (comodidad) | 4.88 | 0.02 | 0 | 44 | −11(16; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4); cvd_adjacent: 2 contacto(s) |
| 384 | 4 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 18 | 2145 | 0.275 | [0.20, 0.60] | normal | — | -0.03 | 0.548 (comodidad) | 6.94 | 0.00 | 0 | 50 | +(0.00) −8(17; 0.00) | cvd_adjacent: 1 contacto(s); turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 385 | 5 | comodidad | Ag | 8 | 1 (0) | 10 | walk | 18 | 6010 | 0.325 | [0.20, 0.60] | normal | — | -0.10 | 0.493 (comodidad) | 6.44 | 0.03 | 0 | 46 | +(0.03) −8(16; 0.08) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4) |
| 386 | 6 | reto | Ba+Tu (D) | 9 | 1 (1) | 11 | deep | 23 | 10134 | 0.850 | [0.75, 0.90) | hard | hard | 0.52 | 0.795 (reto) | 3.30 | 0.04 | 0 | 62 | +(0.10) −3(22; 0.18) | relajacion: turbias con el vacio del rol y la holgura; 3 ronda(s) extra de candidatos turbios; cvd_adjacent: 1 contacto(s); turbia: 3 dosis ocultas en 2 cuia(s), 4 realidades |
| 387 | 7 | comodidad | C | 8 | 2 (2) | 10 | walk | 16 | 603 | 0.400 | [0.20, 0.60] | normal | — | -0.35 | 0.271 (comodidad) | 4.81 | 0.06 | 0 | 44 | −12(15; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 388 | 8 | comodidad | Br+Tu | 8 | 1 (1) | 10 | walk | 16 | 2356 | 0.300 | [0.20, 0.60] | normal | — | -0.12 | 0.475 (comodidad) | 7.50 | 0.00 | 0 | 49 | +(0.00) −5(15; 0.00) | turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 389 | 9 | reto | Li+Ha (D) | 9 | 2 (2) | 11 | fill | 27 | 2979 | 0.850 | [0.75, 0.90) | hard | hard | 0.81 | 0.873 (reto) | 3.07 | 0.07 | 0 | 64 | −4(22; 0.03) +(0.08) | relajacion: fill con 2 vacios (03 sec. 4.4); risk_adjacent: 1 contacto(s); cvd_adjacent: 1 contacto(s) |
| 390 | 10 | comodidad | C | 8 | 2 (2) | 10 | walk | 18 | 1913 | 0.250 | [0.20, 0.60] | normal | — | -0.21 | 0.397 (comodidad) | 6.56 | 0.00 | 0 | 48 | −3(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 391 | 11 | comodidad | Li | 8 | 2 (2) | 10 | walk | 18 | 1366 | 0.250 | [0.20, 0.60] | normal | — | -0.36 | 0.263 (comodidad) | 5.06 | 0.03 | 0 | 48 | −12(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4) |
| 392 | 12 | reto | La+Ba (D) | 9 | 1 (1) | 12 | deep | 26 | 47174 | 0.875 | [0.75, 0.90) | hard | hard | 1.02 | 0.936 (reto) | 5.27 | 0.00 | 0 | 71 | +(0.00) −7(25; 0.00) |  |
| 393 | 13 | comodidad | La | 8 | 1 (1) | 11 | walk | 18 | 606 | 0.225 | [0.20, 0.60] | normal | — | -0.09 | 0.505 (comodidad) | 6.94 | 0.00 | 0 | 53 | +(0.00) −13(17; 0.00) | cvd_adjacent: 1 contacto(s); 2 ronda(s) de banda |
| 394 | 14 | comodidad | Ag+Ch | 8 | 1 (0) | 10 | walk | 16 | 136 | 0.375 | [0.20, 0.60] | normal | — | -0.19 | 0.412 (comodidad) | 6.94 | 0.01 | 0 | 46 | +(0.13) −3(15; 0.03) |  |
| 395 | 15 | reto | Li+Ba (D) | 9 | 1 (1) | 11 | fill | 28 | 12882 | 0.850 | [0.75, 0.90) | hard | hard | 1.24 | 0.978 (hito) | 5.50 | 0.03 | 0 | 67 | +(0.23) −10(24; 0.15) | relajacion: fill con 2 vacios (03 sec. 4.4); cvd_adjacent: 1 contacto(s) |
| 396 | 16 | comodidad | C | 8 | 2 (2) | 10 | walk | 18 | 1795 | 0.275 | [0.20, 0.60] | normal | — | -0.27 | 0.342 (comodidad) | 5.89 | 0.00 | 0 | 48 | −8(17; 0.00) +(0.03) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 397 | 17 | comodidad | Ho+Tu | 8 | 1 (0) | 10 | deep | 19 | 595 | 0.525 | [0.20, 0.60] | normal | — | -0.00 | 0.588 (comodidad) | 3.37 | 0.14 | 0 | 53 | +(0.00) −4(18; 0.08) | endurecimiento: deep; cvd_adjacent: 1 contacto(s); turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 398 | 18 | reto | Ha+Ba (D) | 9 | 1 (1) | 11 | fill | 29 | 3777 | 0.875 | [0.75, 0.90) | hard | hard | 1.55 | 0.991 (hito) | 5.14 | 0.19 | 0 | 73 | +(0.18) −4(26; 0.03) | relajacion: fill con 2 vacios (03 sec. 4.4) |
| 399 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 17 | 1479 | 0.250 | [0.20, 0.40] | normal | — | -0.22 | 0.391 (comodidad) | 7.24 | 0.00 | 0 | 46 | −1(16; 0.00) +(0.03) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 400 | 20 | hito | Li+Ha+Ag (MD) | 11 | 2 (1) | 13 | fill | 34 | 2126 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.82 | 0.993 (hito) | 3.53 | 0.37 | 8 | 82 | −2(31; 0.78) | relajacion: fill con 2 vacios (03 sec. 4.4); cvd_adjacent: 1 contacto(s); alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 401 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1671 | 0.225 | [0.00, 0.30) | normal | — | -0.69 | 0.055 (recuperacion) | 4.50 | 0.06 | 0 | 44 | −1(15; 0.00) +(0.00) |  |
| 402 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 17 | 1980 | 0.125 | [0.00, 0.30) | normal | — | -0.65 | 0.063 (recuperacion) | 5.76 | 0.00 | 0 | 46 | −8(16; 0.00) +(0.00) |  |
| 403 | 3 | estreno | *Fl | 8 | 2 (2) | 10 | walk | 16 | 510 | 0.000 | [0.00, 0.20) | normal | — | -1.24 | 0.000 (recuperacion) | 2.69 | 0.00 | 0 | 42 | −4(14; 0.00) +(0.00) |  |
| 404 | 4 | estreno | *Fl | 8 | 2 (2) | 10 | walk | 17 | 1626 | 0.100 | [0.00, 0.20) | normal | — | -0.63 | 0.070 (recuperacion) | 6.18 | 0.00 | 0 | 44 | −2(15; 0.00) +(0.00) | cvd_adjacent: 1 contacto(s) |
| 405 | 5 | estreno | *Fl | 8 | 2 (2) | 10 | walk | 16 | 759 | 0.150 | [0.00, 0.20) | normal | — | -0.73 | 0.047 (recuperacion) | 4.31 | 0.12 | 0 | 42 | −14(15; 0.00) +(0.00) |  |
| 406 | 6 | reto | Ag+Ll (D) | 9 | 2 (0) | 11 | deep | 20 | 5848 | 0.775 | [0.75, 0.90) | hard | hard | 0.07 | 0.619 (comodidad) | 3.80 | 0.03 | 0 | 54 | +(0.38) −1(19; 0.30) | Aguape con una mecanica que ocupa el vacio: un vacio mas, bajo la mata |
| 407 | 7 | comodidad | Fl | 8 | 2 (2) | 10 | walk | 16 | 201 | 0.475 | [0.20, 0.60] | normal | — | -0.58 | 0.091 (recuperacion) | 3.25 | 0.00 | 0 | 44 | −1(15; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4) |
| 408 | 8 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 17 | 2497 | 0.225 | [0.20, 0.60] | normal | — | -0.41 | 0.209 (comodidad) | 5.82 | 0.00 | 0 | 50 | +(0.00) −9(16; 0.00) | turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 409 | 9 | reto | Fl+Ll (D) | 9 | 2 (1) | 11 | walk | 18 | 591 | 0.750 | [0.75, 0.90) | hard | hard | 0.32 | 0.740 (reto) | 5.61 | 0.15 | 0 | 50 | +(0.03) −8(17; 0.00) | relajacion: walk con 1 vacio; cvd_adjacent: 2 contacto(s) |
| 410 | 10 | comodidad | C | 8 | 1 (1) | 9 | walk | 18 | 343 | 0.525 | [0.20, 0.60] | normal | — | -0.17 | 0.439 (comodidad) | 3.00 | 0.07 | 0 | 46 | +(0.23) −12(17; 0.15) |  |
| 411 | 11 | comodidad | Ag | 8 | 1 (0) | 9 | walk | 16 | 190 | 0.600 | [0.20, 0.60] | normal | — | -0.23 | 0.384 (comodidad) | 3.31 | 0.15 | 0 | 42 | +(0.20) −1(15; 0.03) | cvd_adjacent: 2 contacto(s) |
| 412 | 12 | reto | Fl+La (D) | 9 | 1 (1) | 12 | deep | 21 | 1589 | 0.775 | [0.75, 0.90) | hard | hard | 0.35 | 0.751 (reto) | 3.57 | 0.12 | 0 | 59 | +(0.00) −14(19; 0.00) |  |
| 413 | 13 | comodidad | C | 8 | 2 (2) | 10 | walk | 18 | 3013 | 0.325 | [0.20, 0.60] | normal | — | -0.17 | 0.443 (comodidad) | 6.94 | 0.00 | 0 | 48 | −3(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); cvd_adjacent: 2 contacto(s) |
| 414 | 14 | comodidad | Br+Tu | 8 | 1 (1) | 10 | walk | 16 | 2527 | 0.500 | [0.20, 0.60] | normal | — | -0.02 | 0.557 (comodidad) | 7.00 | 0.00 | 0 | 48 | +(0.00) −2(15; 0.03) | cvd_adjacent: 1 contacto(s); turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 415 | 15 | reto | Fl (D) | 9 | 1 (1) | 10 | walk | 21 | 230 | 0.850 | [0.75, 0.90) | hard | hard | 0.39 | 0.773 (reto) | 3.05 | 0.13 | 1 | 54 | +(0.55) −13(19; 0.18) | relajacion: walk con 1 vacio; 1 ronda(s) extra de candidatos |
| 416 | 16 | comodidad | C | 8 | 2 (2) | 10 | walk | 17 | 2116 | 0.400 | [0.20, 0.60] | normal | — | -0.31 | 0.301 (comodidad) | 5.65 | 0.00 | 0 | 46 | −2(15; 0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 417 | 17 | comodidad | Ho+Tu | 8 | 1 (0) | 10 | deep | 19 | 624 | 0.550 | [0.20, 0.60] | normal | — | -0.19 | 0.424 (comodidad) | 2.84 | 0.06 | 0 | 48 | +(0.00) −12(17; 0.00) | endurecimiento: deep; 1 ronda(s) extra de candidatos turbios; turbia: 2 dosis ocultas en 1 cuia(s), 1 realidades |
| 418 | 18 | reto | Br+Tu (D) | 9 | 1 (1) | 11 | deep | 20 | 8109 | 0.875 | [0.75, 0.90) | hard | hard | 0.62 | 0.843 (reto) | 5.50 | 0.13 | 0 | 54 | +(0.00) −8(19; 0.00) | relajacion: turbias con el vacio del rol y la holgura; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 419 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 19 | 2950 | 0.300 | [0.20, 0.40] | normal | — | -0.35 | 0.265 (comodidad) | 4.84 | 0.00 | 0 | 50 | −6(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); risk_adjacent: 1 contacto(s) |
| 420 | 20 | hito | Fl+Ba+A2 (MD) | 12 | 1 (0) | 14 | fill | 39 | 2264 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 2.28 | 1.000 (hito) | 5.00 | 0.52 | 4 | 93 | −11(35; 0.35) | Aguape espeso: dos vacios, bajo la mata; alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 421 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 18 | 2465 | 0.075 | [0.00, 0.30) | normal | — | -0.70 | 0.052 (recuperacion) | 5.06 | 0.00 | 0 | 46 | −8(17; 0.00) +(0.00) |  |
| 422 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 17 | 2799 | 0.000 | [0.00, 0.30) | normal | — | -0.82 | 0.022 (recuperacion) | 5.65 | 0.00 | 0 | 44 | −8(16; 0.00) +(0.00) |  |
| 423 | 3 | comodidad | Ma+Tu | 8 | 1 (0) | 10 | deep | 19 | 3530 | 0.475 | [0.20, 0.60] | normal | — | 0.18 | 0.663 (comodidad) | 7.53 | 0.00 | 0 | 55 | +(0.00) −10(18; 0.00) | endurecimiento: deep; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 424 | 4 | comodidad | C | 8 | 1 (1) | 9 | walk | 17 | 144 | 0.325 | [0.20, 0.60] | normal | — | -0.58 | 0.094 (recuperacion) | 2.47 | 0.07 | 0 | 48 | +(0.05) −2(15; 0.00) |  |
| 425 | 5 | comodidad | La | 8 | 1 (1) | 11 | walk | 17 | 1554 | 0.300 | [0.20, 0.60] | normal | — | -0.45 | 0.171 (comodidad) | 4.65 | 0.00 | 0 | 52 | +(0.00) −12(16; 0.00) |  |
| 426 | 6 | reto | Fl+Ma (D) | 9 | 2 (1) | 11 | walk | 19 | 6414 | 0.775 | [0.75, 0.90) | hard | hard | 0.45 | 0.789 (reto) | 6.47 | 0.08 | 0 | 50 | +(0.13) −5(18; 0.20) | relajacion: walk con 1 vacio |
| 427 | 7 | comodidad | C | 8 | 2 (2) | 10 | walk | 17 | 1669 | 0.325 | [0.20, 0.60] | normal | — | -0.57 | 0.096 (recuperacion) | 4.18 | 0.00 | 0 | 46 | −1(15; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 428 | 8 | comodidad | Ba+Tu | 8 | 1 (1) | 10 | walk | 18 | 1222 | 0.575 | [0.20, 0.60] | normal | — | -0.06 | 0.528 (comodidad) | 3.33 | 0.08 | 0 | 54 | +(0.00) −8(17; 0.00) | turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 429 | 9 | reto | Ag+Ma+Re (D) | 9 | 2 (0) | 11 | deep | 23 | 13952 | 0.750 | [0.75, 0.90) | hard | hard | 0.61 | 0.840 (reto) | 6.57 | 0.01 | 0 | 60 | +(0.55) −7(18; 0.53) | Aguape con una mecanica que ocupa el vacio: un vacio mas, bajo la mata; 1 ronda(s) extra de candidatos |
| 430 | 10 | comodidad | C | 8 | 2 (2) | 10 | walk | 18 | 3327 | 0.375 | [0.20, 0.60] | normal | — | -0.21 | 0.409 (comodidad) | 5.72 | 0.03 | 0 | 48 | −4(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 431 | 11 | comodidad | Ag+B2 | 8 | 1 (0) | 10 | walk | 19 | 2420 | 0.575 | [0.20, 0.60] | normal | — | -0.18 | 0.430 (comodidad) | 3.05 | 0.02 | 0 | 52 | −7(18; 0.28) | cvd_adjacent: 1 contacto(s); alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 432 | 12 | reto | Fl+Ho (D) | 9 | 3 (2) | 12 | fill | 31 | 24062 | 0.750 | [0.75, 0.90) | hard | hard | 0.95 | 0.929 (reto) | 3.29 | 0.00 | 0 | 70 | −1(25; 0.00) +(0.08) | relajacion: fill con 2 vacios (03 sec. 4.4) |
| 433 | 13 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 16 | 3478 | 0.225 | [0.20, 0.60] | normal | — | -0.29 | 0.324 (comodidad) | 7.44 | 0.03 | 0 | 49 | +(0.00) −3(15; 0.00) | turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 434 | 14 | comodidad | Al | 8 | 1 (1) | 10 | walk | 19 | 3581 | 0.450 | [0.20, 0.60] | normal | — | -0.05 | 0.541 (comodidad) | 4.95 | 0.00 | 0 | 50 | +(0.00) −6(18; 0.03) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4) |
| 435 | 15 | reto | Ma+Tu (D) | 9 | 1 (0) | 11 | deep | 23 | 13038 | 0.875 | [0.75, 0.90) | hard | hard | 1.00 | 0.940 (reto) | 6.04 | 0.25 | 0 | 58 | +(0.05) −10(21; 0.00) | turbia: 2 dosis ocultas en 1 cuia(s), 1 realidades |
| 436 | 16 | comodidad | C | 8 | 1 (1) | 9 | walk | 16 | 250 | 0.475 | [0.20, 0.60] | normal | — | -0.20 | 0.416 (comodidad) | 2.88 | 0.26 | 0 | 44 | +(0.15) −4(15; 0.05) | cvd_adjacent: 1 contacto(s) |
| 437 | 17 | comodidad | La+Ha | 8 | 1 (1) | 11 | walk | 17 | 1969 | 0.450 | [0.20, 0.60] | normal | — | 0.01 | 0.584 (comodidad) | 7.06 | 0.00 | 0 | 48 | +(0.00) −2(16; 0.00) |  |
| 438 | 18 | reto | Ho+Re (D) | 9 | 2 (1) | 11 | walk | 23 | 22969 | 0.775 | [0.75, 0.90) | hard | hard | 1.01 | 0.943 (reto) | 9.09 | 0.00 | 0 | 58 | +(0.08) −10(20; 0.05) | relajacion: walk con 1 vacio |
| 439 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 17 | 1345 | 0.375 | [0.20, 0.40] | normal | — | -0.36 | 0.247 (comodidad) | 5.47 | 0.00 | 0 | 46 | −10(16; 0.00) +(0.13) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 440 | 20 | hito | Ag+Ho+Re (MD) | 11 | 2 (0) | 13 | deep | 28 | 248809 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.44 | 0.980 (hito) | 9.68 | 0.07 | 2 | 62 | +(0.65) −10(22; 0.80) | Aguape con una mecanica que ocupa el vacio: un vacio mas, bajo la mata |
| 441 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 19 | 4529 | 0.025 | [0.00, 0.30) | normal | — | -0.68 | 0.057 (recuperacion) | 4.63 | 0.03 | 0 | 48 | −14(18; 0.00) +(0.00) |  |
| 442 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 21 | 3442 | 0.125 | [0.00, 0.30) | normal | — | -0.33 | 0.284 (comodidad) | 4.81 | 0.05 | 0 | 54 | −14(20; 0.00) +(0.00) |  |
| 443 | 3 | comodidad | Pi | 8 | 1 (1) | 10 | walk | 18 | 13677 | 0.350 | [0.20, 0.60] | normal | — | -0.06 | 0.528 (comodidad) | 6.56 | 0.02 | 0 | 46 | +(0.00) −11(17; 0.00) | cvd_adjacent: 3 contacto(s) |
| 444 | 4 | comodidad | La | 8 | 1 (1) | 11 | walk | 16 | 45 | 0.325 | [0.20, 0.60] | normal | — | -0.24 | 0.376 (comodidad) | 7.06 | 0.00 | 0 | 48 | +(0.00) −2(15; 0.00) |  |
| 445 | 5 | comodidad | Br+Tu | 8 | 1 (1) | 10 | walk | 17 | 1937 | 0.475 | [0.20, 0.60] | normal | — | 0.12 | 0.637 (comodidad) | 7.71 | 0.00 | 0 | 50 | +(0.00) −14(16; 0.00) | cvd_adjacent: 2 contacto(s); turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 446 | 6 | reto | Pi+Ba (D) | 9 | 0 (0) | 11 | walk | 19 | 3754 | 0.875 | [0.75, 0.90) | hard | hard | 0.34 | 0.747 (reto) | 4.00 | 0.20 | 0 | 51 | +(0.05) −6(18; 0.20) | relajacion: walk con 1 vacio |
| 447 | 7 | comodidad | C | 8 | 2 (2) | 10 | walk | 19 | 2959 | 0.325 | [0.20, 0.60] | normal | — | -0.15 | 0.464 (comodidad) | 6.32 | 0.00 | 0 | 48 | −10(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 448 | 8 | comodidad | Ho+Tu | 8 | 1 (0) | 10 | deep | 19 | 4232 | 0.575 | [0.20, 0.60] | normal | — | -0.15 | 0.460 (comodidad) | 3.53 | 0.00 | 0 | 53 | +(0.00) −4(17; 0.00) | endurecimiento: deep; turbia: 2 dosis ocultas en 1 cuia(s), 1 realidades |
| 449 | 9 | reto | Br+Re (D) | 9 | 1 (1) | 11 | deep | 20 | 16475 | 0.725 | [0.75, 0.90) | normal | hard | 0.52 | 0.809 (reto) | 7.70 | 0.00 | 0 | 54 | +(0.10) −3(18; 0.00) | fuera de ventana (relajacion agotada); 1 ronda(s) extra de candidatos |
| 450 | 10 | comodidad | C | 8 | 2 (2) | 10 | walk | 18 | 3027 | 0.550 | [0.20, 0.60] | normal | — | 0.12 | 0.641 (comodidad) | 7.00 | 0.00 | 0 | 48 | −2(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 451 | 11 | comodidad | Pi | 8 | 1 (1) | 10 | walk | 17 | 22443 | 0.225 | [0.20, 0.60] | normal | — | -0.30 | 0.316 (comodidad) | 6.82 | 0.00 | 0 | 44 | +(0.00) −9(16; 0.00) |  |
| 452 | 12 | reto | Pi+Br+Ag (D) | 9 | 1 (0) | 12 | walk | 22 | 35268 | 0.825 | [0.75, 0.90) | hard | hard | 0.52 | 0.811 (reto) | 5.91 | 0.00 | 0 | 61 | −7(18; 0.60) | relajacion: walk con 1 vacio; alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 453 | 13 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 17 | 4068 | 0.375 | [0.20, 0.60] | normal | — | -0.10 | 0.499 (comodidad) | 6.24 | 0.07 | 0 | 49 | +(0.00) −5(15; 0.00) | turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 454 | 14 | comodidad | La | 8 | 1 (1) | 11 | walk | 16 | 1118 | 0.300 | [0.20, 0.60] | normal | — | -0.29 | 0.323 (comodidad) | 6.88 | 0.00 | 0 | 47 | +(0.00) −12(15; 0.00) | cvd_adjacent: 1 contacto(s); 2 ronda(s) de banda |
| 455 | 15 | reto | Br+Tu (D) | 9 | 1 (1) | 11 | deep | 21 | 14193 | 0.775 | [0.75, 0.90) | hard | hard | 0.57 | 0.825 (reto) | 6.76 | 0.00 | 0 | 59 | +(0.00) −14(19; 0.00) | relajacion: turbias con el vacio del rol y la holgura; turbia: 3 dosis ocultas en 2 cuia(s), 2 realidades |
| 456 | 16 | comodidad | C | 8 | 2 (2) | 10 | walk | 18 | 1879 | 0.250 | [0.20, 0.60] | normal | — | -0.28 | 0.336 (comodidad) | 6.83 | 0.00 | 0 | 48 | −14(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); cvd_adjacent: 1 contacto(s) |
| 457 | 17 | comodidad | Ag+Ha | 8 | 1 (0) | 10 | walk | 16 | 2683 | 0.525 | [0.20, 0.60] | normal | — | -0.03 | 0.551 (comodidad) | 7.25 | 0.03 | 0 | 44 | +(0.13) −1(14; 0.15) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4) |
| 458 | 18 | reto | Ba+Re (D) | 9 | 1 (1) | 11 | fill | 27 | 150872 | 0.800 | [0.75, 0.90) | hard | hard | 1.18 | 0.967 (hito) | 6.96 | 0.01 | 0 | 67 | +(0.18) −13(24; 0.23) | relajacion: fill con 2 vacios (03 sec. 4.4); 1 ronda(s) extra de candidatos |
| 459 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 17 | 2926 | 0.350 | [0.20, 0.40] | normal | — | -0.24 | 0.371 (comodidad) | 6.82 | 0.00 | 0 | 44 | −1(15; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 460 | 20 | hito | Pi+Ba+Ag (MD) | 11 | 0 (0) | 13 | fill | 34 | 20227 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.34 | 0.978 (hito) | 3.09 | 0.15 | 4 | 83 | +(0.40) −4(30; 0.78) | cvd_adjacent: 1 contacto(s); 1 ronda(s) de banda |
| 461 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1042 | 0.025 | [0.00, 0.30) | normal | — | -0.67 | 0.060 (recuperacion) | 7.44 | 0.00 | 0 | 44 | −7(15; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 462 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 18 | 1743 | 0.175 | [0.00, 0.30) | normal | — | -0.67 | 0.059 (recuperacion) | 4.17 | 0.00 | 0 | 48 | −14(17; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 463 | 3 | comodidad | Li | 8 | 2 (2) | 10 | walk | 18 | 1610 | 0.425 | [0.20, 0.60] | normal | — | -0.14 | 0.469 (comodidad) | 6.06 | 0.00 | 0 | 48 | −10(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4) |
| 464 | 4 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 17 | 3382 | 0.525 | [0.20, 0.60] | normal | — | -0.05 | 0.541 (comodidad) | 5.35 | 0.03 | 0 | 48 | +(0.00) −6(16; 0.00) | turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 465 | 5 | comodidad | B2 | 8 | 1 (1) | 10 | walk | 17 | 533 | 0.225 | [0.20, 0.60] | normal | — | -0.44 | 0.183 (comodidad) | 5.59 | 0.00 | 0 | 48 | +(0.03) −10(16; 0.00) |  |
| 466 | 6 | reto | Li+B2 (D) | 9 | 1 (1) | 11 | deep | 19 | 1049 | 0.875 | [0.75, 0.90) | hard | hard | 0.30 | 0.727 (reto) | 3.63 | 0.12 | 0 | 54 | +(0.00) −5(18; 0.00) | cvd_adjacent: 1 contacto(s); 2 ronda(s) extra de candidatos |
| 467 | 7 | comodidad | C | 8 | 1 (1) | 9 | walk | 17 | 158 | 0.300 | [0.20, 0.60] | normal | — | -0.27 | 0.342 (comodidad) | 2.82 | 0.31 | 0 | 44 | +(0.03) −2(15; 0.10) | 1 ronda(s) extra de candidatos |
| 468 | 8 | comodidad | Ag | 8 | 1 (0) | 10 | walk | 16 | 557 | 0.600 | [0.20, 0.60] | normal | — | -0.27 | 0.344 (comodidad) | 3.69 | 0.08 | 0 | 44 | +(0.38) −3(14; 0.38) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4) |
| 469 | 9 | reto | Ln+B2 (D) | 9 | 1 (1) | 11 | deep | 21 | 1847 | 0.825 | [0.75, 0.90) | hard | hard | 0.33 | 0.743 (reto) | 3.29 | 0.07 | 0 | 57 | +(0.00) −5(20; 0.00) | 1 ronda(s) extra de candidatos |
| 470 | 10 | comodidad | C | 8 | 1 (1) | 9 | walk | 18 | 214 | 0.225 | [0.20, 0.60] | normal | — | -0.61 | 0.075 (recuperacion) | 2.17 | 0.10 | 0 | 46 | −10(16; 0.00) | 1 ronda(s) extra de candidatos; alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 471 | 11 | comodidad | Br+Tu | 8 | 1 (1) | 10 | deep | 19 | 3627 | 0.325 | [0.20, 0.60] | normal | — | -0.08 | 0.512 (comodidad) | 5.68 | 0.04 | 0 | 55 | +(0.00) −9(18; 0.00) | endurecimiento: deep; turbia: 2 dosis ocultas en 1 cuia(s), 1 realidades |
| 472 | 12 | reto | Li+La (D) | 9 | 1 (1) | 12 | deep | 21 | 12859 | 0.750 | [0.75, 0.90) | hard | hard | 0.37 | 0.756 (reto) | 4.71 | 0.05 | 0 | 56 | +(0.00) −12(20; 0.00) | cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 473 | 13 | comodidad | C | 8 | 2 (2) | 10 | walk | 16 | 1355 | 0.275 | [0.20, 0.60] | normal | — | -0.58 | 0.095 (recuperacion) | 5.50 | 0.00 | 0 | 44 | −1(14; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 474 | 14 | comodidad | Ag+Ch | 8 | 1 (0) | 10 | walk | 19 | 2297 | 0.450 | [0.20, 0.60] | normal | — | -0.10 | 0.496 (comodidad) | 5.16 | 0.02 | 0 | 50 | −12(17; 0.23) | alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 475 | 15 | reto | Ho+Tu (D) | 9 | 1 (0) | 11 | deep | 25 | 1699 | 0.750 | [0.75, 0.90) | hard | hard | 0.41 | 0.774 (reto) | 3.16 | 0.01 | 0 | 62 | +(0.00) −7(23; 0.00) | 1 ronda(s) extra de candidatos; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 476 | 16 | comodidad | C | 8 | 1 (1) | 9 | walk | 18 | 183 | 0.500 | [0.20, 0.60] | normal | — | -0.17 | 0.444 (comodidad) | 2.22 | 0.18 | 0 | 46 | +(0.03) −8(17; 0.00) | 1 ronda(s) extra de candidatos |
| 477 | 17 | comodidad | Ba+Tu | 8 | 1 (1) | 10 | walk | 16 | 395 | 0.425 | [0.20, 0.60] | normal | — | -0.29 | 0.322 (comodidad) | 4.75 | 0.07 | 0 | 50 | +(0.00) −6(14; 0.00) | turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 478 | 18 | reto | La+Ln (D) | 9 | 1 (1) | 12 | deep | 23 | 21231 | 0.850 | [0.75, 0.90) | hard | hard | 0.61 | 0.841 (reto) | 4.70 | 0.00 | 0 | 61 | +(0.00) −12(22; 0.00) | 1 ronda(s) extra de candidatos |
| 479 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 18 | 4279 | 0.250 | [0.20, 0.40] | normal | — | -0.30 | 0.321 (comodidad) | 6.67 | 0.00 | 0 | 48 | −12(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 480 | 20 | hito | Li+B2+Ho (MD) | 11 | 1 (0) | 13 | deep | 30 | 1050 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.21 | 0.967 (hito) | 4.13 | 0.24 | 0 | 74 | +(0.78) −8(27; 0.60) | cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos; 1 ronda(s) de banda |
| 481 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 17 | 1191 | 0.100 | [0.00, 0.30) | normal | — | -0.72 | 0.049 (recuperacion) | 5.41 | 0.00 | 0 | 46 | −3(16; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 482 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 17 | 1212 | 0.025 | [0.00, 0.30) | normal | — | -0.81 | 0.023 (recuperacion) | 5.41 | 0.00 | 0 | 46 | −9(15; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 483 | 3 | comodidad | Ma+Tu | 8 | 1 (0) | 10 | deep | 18 | 6377 | 0.500 | [0.20, 0.60] | normal | — | -0.01 | 0.567 (comodidad) | 6.39 | 0.00 | 0 | 53 | +(0.00) −4(17; 0.00) | endurecimiento: deep; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 484 | 4 | comodidad | C | 8 | 2 (2) | 10 | walk | 16 | 1490 | 0.350 | [0.20, 0.60] | normal | — | -0.51 | 0.135 (comodidad) | 5.31 | 0.00 | 0 | 44 | −2(15; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); cvd_adjacent: 1 contacto(s) |
| 485 | 5 | comodidad | La | 8 | 1 (1) | 11 | walk | 16 | 2535 | 0.275 | [0.20, 0.60] | normal | — | -0.41 | 0.207 (comodidad) | 6.06 | 0.00 | 0 | 46 | +(0.00) −1(14; 0.00) |  |
| 486 | 6 | reto | Ma+Ha (D) | 9 | 2 (1) | 11 | walk | 19 | 3690 | 0.775 | [0.75, 0.90) | hard | hard | 0.37 | 0.759 (reto) | 5.84 | 0.07 | 0 | 52 | +(0.00) −6(18; 0.05) | relajacion: walk con 1 vacio; 1 ronda(s) extra de candidatos |
| 487 | 7 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 17 | 2377 | 0.300 | [0.20, 0.60] | normal | — | -0.04 | 0.546 (comodidad) | 7.71 | 0.05 | 0 | 49 | +(0.00) −3(16; 0.00) | 1 ronda(s) extra de candidatos; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 488 | 8 | comodidad | Ag | 8 | 1 (0) | 10 | walk | 17 | 894 | 0.575 | [0.20, 0.60] | normal | — | -0.03 | 0.555 (comodidad) | 4.88 | 0.12 | 0 | 44 | −2(14; 0.00) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4); alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 489 | 9 | reto | Ma+Ba (D) | 9 | 1 (0) | 11 | walk | 20 | 3747 | 0.825 | [0.75, 0.90) | hard | hard | 0.46 | 0.796 (reto) | 5.80 | 0.10 | 0 | 57 | +(0.00) −6(19; 0.00) | relajacion: walk con 1 vacio; 1 ronda(s) extra de candidatos |
| 490 | 10 | comodidad | C | 8 | 2 (2) | 10 | walk | 18 | 3629 | 0.350 | [0.20, 0.60] | normal | — | -0.23 | 0.382 (comodidad) | 6.11 | 0.00 | 0 | 48 | −2(17; 0.00) +(0.05) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 491 | 11 | comodidad | Br+Tu | 8 | 1 (1) | 10 | walk | 16 | 1196 | 0.225 | [0.20, 0.60] | normal | — | -0.63 | 0.070 (recuperacion) | 4.44 | 0.03 | 0 | 49 | +(0.00) −5(15; 0.00) | cvd_adjacent: 1 contacto(s); turbia: 2 dosis ocultas en 1 cuia(s), 1 realidades |
| 492 | 12 | reto | Ag+Ha+Ba (D) | 9 | 0 (0) | 10 | walk | 20 | 158 | 0.850 | [0.75, 0.90) | hard | hard | 0.46 | 0.799 (reto) | 2.80 | 0.36 | 0 | 51 | +(0.20) −1(18; 0.13) | relajacion: walk con 1 vacio; 1 ronda(s) extra de candidatos |
| 493 | 13 | comodidad | C | 8 | 1 (1) | 9 | walk | 17 | 270 | 0.475 | [0.20, 0.60] | normal | — | -0.13 | 0.476 (comodidad) | 3.88 | 0.15 | 0 | 44 | +(0.00) −5(15; 0.00) | cvd_adjacent: 2 contacto(s); 1 ronda(s) extra de candidatos |
| 494 | 14 | comodidad | C | 8 | 1 (1) | 9 | walk | 17 | 273 | 0.575 | [0.20, 0.60] | normal | — | -0.18 | 0.433 (comodidad) | 2.47 | 0.14 | 0 | 46 | +(0.05) −14(16; 0.08) | cvd_adjacent: 1 contacto(s) |
| 495 | 15 | reto | Ma+Tu (D) | 9 | 1 (0) | 11 | deep | 23 | 7876 | 0.825 | [0.75, 0.90) | hard | hard | 0.58 | 0.827 (reto) | 5.13 | 0.04 | 0 | 65 | +(0.03) −8(22; 0.13) | 1 ronda(s) extra de candidatos; turbia: 3 dosis ocultas en 2 cuia(s), 6 realidades |
| 496 | 16 | comodidad | C | 8 | 2 (2) | 10 | walk | 19 | 4269 | 0.450 | [0.20, 0.60] | normal | — | -0.05 | 0.544 (comodidad) | 5.53 | 0.03 | 0 | 48 | −1(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); cvd_adjacent: 3 contacto(s); 1 ronda(s) extra de candidatos |
| 497 | 17 | comodidad | La+B2 | 8 | 1 (1) | 11 | deep | 19 | 2316 | 0.300 | [0.20, 0.60] | normal | — | -0.02 | 0.562 (comodidad) | 6.84 | 0.00 | 0 | 54 | +(0.00) −6(18; 0.00) | endurecimiento: deep |
| 498 | 18 | reto | Fl+Ha (D) | 9 | 2 (2) | 11 | fill | 27 | 4988 | 0.850 | [0.75, 0.90) | hard | hard | 0.99 | 0.938 (reto) | 4.63 | 0.10 | 1 | 62 | −10(24; 0.28) +(0.25) | relajacion: fill con 2 vacios (03 sec. 4.4); cvd_adjacent: 2 contacto(s); 1 ronda(s) extra de candidatos |
| 499 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 19 | 2561 | 0.225 | [0.20, 0.40] | normal | — | -0.54 | 0.114 (comodidad) | 3.47 | 0.05 | 0 | 46 | −3(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 500 | 20 | hito | Re+Ha+A2 (MD) | 11 | 2 (0) | 13 | deep | 29 | 76740 | 0.950 | [0.90, 1.00] | very_hard | very_hard | 1.13 | 0.957 (hito) | 6.97 | 0.04 | 4 | 70 | −11(27; 0.18) | Aguape espeso: dos vacios, bajo la mata; cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos; alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 501 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 17 | 1212 | 0.050 | [0.00, 0.30) | normal | — | -0.85 | 0.015 (recuperacion) | 4.76 | 0.00 | 0 | 46 | −6(16; 0.00) +(0.00) |  |
| 502 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 78 | 0.000 | [0.00, 0.30) | normal | — | -0.74 | 0.042 (recuperacion) | 7.13 | 0.00 | 0 | 46 | −11(15; 0.00) +(0.00) |  |
| 503 | 3 | comodidad | Ll | 8 | 2 (1) | 10 | walk | 17 | 88 | 0.350 | [0.20, 0.60] | normal | — | 0.06 | 0.611 (comodidad) | 8.59 | 0.00 | 0 | 50 | +(0.00) −3(16; 0.00) | cvd_adjacent: 1 contacto(s) |
| 504 | 4 | comodidad | C | 8 | 1 (1) | 9 | walk | 16 | 281 | 0.225 | [0.20, 0.60] | normal | — | -0.86 | 0.014 (recuperacion) | 2.38 | 0.03 | 0 | 42 | +(0.00) −3(14; 0.00) |  |
| 505 | 5 | comodidad | Ba+Tu | 8 | 1 (1) | 10 | walk | 18 | 4041 | 0.250 | [0.20, 0.60] | normal | — | -0.37 | 0.240 (comodidad) | 4.28 | 0.08 | 0 | 52 | +(0.03) −3(16; 0.00) | 1 ronda(s) extra de candidatos turbios; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 506 | 6 | reto | La+Ba (D) | 9 | 1 (1) | 12 | deep | 19 | 6768 | 0.875 | [0.75, 0.90) | hard | hard | 0.36 | 0.753 (reto) | 5.05 | 0.03 | 0 | 55 | −4(17; 0.00) | endurecimiento: deep; alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 507 | 7 | comodidad | C | 8 | 2 (2) | 10 | walk | 18 | 1704 | 0.550 | [0.20, 0.60] | normal | — | -0.28 | 0.333 (comodidad) | 3.11 | 0.04 | 0 | 46 | −9(16; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 508 | 8 | comodidad | Ho+Tu | 8 | 1 (0) | 10 | deep | 19 | 187 | 0.225 | [0.20, 0.60] | normal | — | -0.59 | 0.086 (recuperacion) | 3.16 | 0.03 | 0 | 52 | +(0.00) −6(17; 0.00) | endurecimiento: deep; cvd_adjacent: 2 contacto(s); turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 509 | 9 | reto | Fl (D) | 9 | 2 (2) | 11 | fill | 26 | 1701 | 0.725 | [0.75, 0.90) | normal | hard | 0.37 | 0.759 (reto) | 2.46 | 0.00 | 0 | 64 | −10(24; 0.20) +(0.08) | relajacion: fill con 2 vacios (03 sec. 4.4); fuera de ventana (relajacion agotada); risk_adjacent: 1 contacto(s); cvd_adjacent: 3 contacto(s); 1 ronda(s) extra de candidatos |
| 510 | 10 | comodidad | C | 8 | 2 (2) | 10 | walk | 18 | 4296 | 0.275 | [0.20, 0.60] | normal | — | -0.33 | 0.285 (comodidad) | 5.56 | 0.05 | 0 | 44 | −13(16; 0.00) +(0.05) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); cvd_adjacent: 1 contacto(s) |
| 511 | 11 | comodidad | Ag+Li | 8 | 1 (0) | 9 | walk | 16 | 127 | 0.600 | [0.20, 0.60] | normal | — | -0.26 | 0.350 (comodidad) | 2.25 | 0.22 | 0 | 44 | +(0.05) −3(14; 0.30) |  |
| 512 | 12 | reto | Ll+Ha (D) | 9 | 2 (1) | 11 | walk | 19 | 1780 | 0.875 | [0.75, 0.90) | hard | hard | 0.51 | 0.808 (reto) | 6.21 | 0.05 | 0 | 54 | +(0.00) −11(18; 0.00) | relajacion: walk con 1 vacio; cvd_adjacent: 1 contacto(s) |
| 513 | 13 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 16 | 1272 | 0.375 | [0.20, 0.60] | normal | — | -0.26 | 0.355 (comodidad) | 5.88 | 0.04 | 0 | 49 | +(0.00) −5(15; 0.00) | cvd_adjacent: 1 contacto(s); turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 514 | 14 | comodidad | Ll+Al | 8 | 2 (1) | 10 | walk | 16 | 3770 | 0.250 | [0.20, 0.60] | normal | — | -0.40 | 0.220 (comodidad) | 6.13 | 0.03 | 0 | 44 | +(0.00) −12(15; 0.00) |  |
| 515 | 15 | reto | Ba+Tu (D) | 9 | 1 (1) | 11 | deep | 21 | 2611 | 0.875 | [0.75, 0.90) | hard | hard | 0.55 | 0.821 (reto) | 5.48 | 0.00 | 0 | 63 | +(0.00) −1(20; 0.00) | relajacion: turbias con el vacio del rol y la holgura; turbia: 2 dosis ocultas en 1 cuia(s), 1 realidades |
| 516 | 16 | comodidad | C | 8 | 2 (2) | 10 | walk | 17 | 1206 | 0.550 | [0.20, 0.60] | normal | — | 0.04 | 0.604 (comodidad) | 7.12 | 0.00 | 0 | 48 | −6(16; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); cvd_adjacent: 4 contacto(s) |
| 517 | 17 | comodidad | La+B2 | 8 | 1 (1) | 11 | walk | 16 | 161 | 0.350 | [0.20, 0.60] | normal | — | -0.38 | 0.234 (comodidad) | 4.00 | 0.14 | 0 | 48 | +(0.00) −13(15; 0.00) | 2 ronda(s) de banda |
| 518 | 18 | reto | Fl+Ba (D) | 9 | 1 (1) | 11 | fill | 26 | 20698 | 0.800 | [0.75, 0.90) | hard | hard | 0.71 | 0.871 (reto) | 3.38 | 0.03 | 0 | 67 | +(0.05) −9(22; 0.03) | relajacion: fill con 2 vacios (03 sec. 4.4); cvd_adjacent: 2 contacto(s) |
| 519 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 18 | 2135 | 0.275 | [0.20, 0.40] | normal | — | -0.35 | 0.269 (comodidad) | 5.94 | 0.00 | 0 | 48 | −10(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 520 | 20 | hito | Li+Ha+A2 (MD) | 11 | 2 (0) | 13 | fill | 33 | 845 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.58 | 0.990 (hito) | 3.24 | 0.40 | 0 | 78 | +(0.48) −2(30; 0.63) | relajacion: fill con 2 vacios (03 sec. 4.4) |
| 521 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 17 | 1877 | 0.150 | [0.00, 0.30) | normal | — | -0.59 | 0.088 (recuperacion) | 5.53 | 0.04 | 0 | 46 | −9(16; 0.00) +(0.00) | cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 522 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1368 | 0.250 | [0.00, 0.30) | normal | — | -0.58 | 0.092 (recuperacion) | 5.75 | 0.00 | 0 | 44 | −3(15; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 523 | 3 | comodidad | Pi | 8 | 1 (1) | 10 | walk | 17 | 17616 | 0.425 | [0.20, 0.60] | normal | — | -0.07 | 0.517 (comodidad) | 6.41 | 0.02 | 0 | 44 | +(0.00) −10(15; 0.00) |  |
| 524 | 4 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 18 | 5484 | 0.300 | [0.20, 0.60] | normal | — | -0.32 | 0.294 (comodidad) | 5.00 | 0.00 | 0 | 53 | +(0.00) −1(17; 0.00) | cvd_adjacent: 1 contacto(s); turbia: 2 dosis ocultas en 1 cuia(s), 1 realidades |
| 525 | 5 | comodidad | Ag | 8 | 1 (0) | 10 | walk | 19 | 1599 | 0.575 | [0.20, 0.60] | normal | — | -0.04 | 0.549 (comodidad) | 3.74 | 0.07 | 0 | 48 | −3(17; 0.20) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4); cvd_adjacent: 1 contacto(s); alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 526 | 6 | reto | Pi+Ha (D) | 9 | 1 (1) | 11 | deep | 20 | 3281 | 0.850 | [0.75, 0.90) | hard | hard | 0.45 | 0.788 (reto) | 5.35 | 0.03 | 0 | 54 | +(0.05) −5(19; 0.00) | risk_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 527 | 7 | comodidad | C | 8 | 1 (1) | 9 | walk | 18 | 165 | 0.225 | [0.20, 0.60] | normal | — | -0.35 | 0.266 (comodidad) | 2.78 | 0.26 | 0 | 46 | +(0.03) −1(16; 0.00) | 1 ronda(s) extra de candidatos |
| 528 | 8 | comodidad | Br+Tu | 8 | 1 (1) | 10 | walk | 17 | 2237 | 0.300 | [0.20, 0.60] | normal | — | -0.15 | 0.467 (comodidad) | 7.35 | 0.00 | 0 | 52 | +(0.00) −7(16; 0.00) | turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 529 | 9 | reto | Ag+Ho+Re (D) | 9 | 2 (0) | 11 | deep | 24 | 22020 | 0.800 | [0.75, 0.90) | hard | hard | 0.45 | 0.792 (reto) | 3.42 | 0.05 | 1 | 58 | +(0.25) −6(21; 0.18) | Aguape con una mecanica que ocupa el vacio: un vacio mas, bajo la mata; cvd_adjacent: 3 contacto(s); 1 ronda(s) extra de candidatos |
| 530 | 10 | comodidad | C | 8 | 1 (1) | 9 | walk | 17 | 145 | 0.275 | [0.20, 0.60] | normal | — | -0.61 | 0.076 (recuperacion) | 2.53 | 0.09 | 0 | 48 | +(0.03) −13(16; 0.00) | risk_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 531 | 11 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 17 | 2820 | 0.425 | [0.20, 0.60] | normal | — | -0.18 | 0.437 (comodidad) | 5.71 | 0.00 | 0 | 51 | +(0.00) −8(16; 0.00) | turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 532 | 12 | reto | Pi+Ho (D) | 9 | 1 (0) | 11 | deep | 25 | 2494 | 0.850 | [0.75, 0.90) | hard | hard | 0.54 | 0.815 (reto) | 2.88 | 0.04 | 1 | 60 | +(0.35) −13(22; 0.18) | 1 ronda(s) extra de candidatos |
| 533 | 13 | comodidad | La | 8 | 1 (1) | 11 | deep | 20 | 22800 | 0.400 | [0.20, 0.60] | normal | — | 0.19 | 0.672 (comodidad) | 6.80 | 0.00 | 0 | 52 | +(0.00) −12(18; 0.00) | endurecimiento: deep; cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 534 | 14 | comodidad | Ch | 8 | 1 (1) | 10 | walk | 18 | 3428 | 0.300 | [0.20, 0.60] | normal | — | -0.44 | 0.188 (comodidad) | 4.00 | 0.00 | 0 | 48 | +(0.00) −9(17; 0.00) | cvd_adjacent: 1 contacto(s) |
| 535 | 15 | reto | Ho+Tu (D) | 9 | 1 (0) | 11 | deep | 23 | 8125 | 0.825 | [0.75, 0.90) | hard | hard | 0.58 | 0.828 (reto) | 4.57 | 0.10 | 0 | 60 | +(0.20) −9(21; 0.25) | 1 ronda(s) extra de candidatos; turbia: 2 dosis ocultas en 2 cuia(s), 1 realidades |
| 536 | 16 | comodidad | C | 8 | 2 (2) | 10 | walk | 17 | 3901 | 0.275 | [0.20, 0.60] | normal | — | -0.30 | 0.310 (comodidad) | 7.12 | 0.00 | 0 | 44 | −9(16; 0.00) +(0.03) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 537 | 17 | comodidad | La+B2 | 8 | 1 (1) | 11 | walk | 18 | 2892 | 0.475 | [0.20, 0.60] | normal | — | -0.06 | 0.535 (comodidad) | 5.39 | 0.00 | 0 | 50 | +(0.00) −2(17; 0.00) |  |
| 538 | 18 | reto | Ha+Re (D) | 9 | 2 (2) | 11 | fill | 30 | 239755 | 0.800 | [0.75, 0.90) | hard | hard | 1.15 | 0.964 (hito) | 5.30 | 0.00 | 1 | 72 | −5(26; 0.00) +(0.03) | relajacion: fill con 2 vacios (03 sec. 4.4); risk_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 539 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 18 | 3065 | 0.300 | [0.20, 0.40] | normal | — | -0.40 | 0.223 (comodidad) | 5.22 | 0.00 | 0 | 46 | −7(16; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 540 | 20 | hito | Ln+Ha+A2 (MD) | 11 | 2 (0) | 13 | deep | 29 | 8814 | 0.975 | [0.90, 1.00] | very_hard | very_hard | 1.24 | 0.969 (hito) | 4.79 | 0.31 | 2 | 70 | −13(25; 0.58) | Aguape espeso: dos vacios, bajo la mata; 1 ronda(s) extra de candidatos; alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 541 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1690 | 0.000 | [0.00, 0.30) | normal | — | -0.87 | 0.013 (recuperacion) | 6.00 | 0.00 | 0 | 42 | −14(15; 0.00) +(0.00) |  |
| 542 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1994 | 0.075 | [0.00, 0.30) | normal | — | -0.74 | 0.041 (recuperacion) | 6.25 | 0.00 | 0 | 44 | −1(14; 0.00) +(0.00) | cvd_adjacent: 1 contacto(s) |
| 543 | 3 | comodidad | Fl | 8 | 1 (1) | 9 | walk | 16 | 167 | 0.475 | [0.20, 0.60] | normal | — | -0.49 | 0.145 (comodidad) | 2.63 | 0.05 | 0 | 44 | +(0.25) −3(14; 0.00) |  |
| 544 | 4 | comodidad | C | 8 | 2 (2) | 10 | walk | 20 | 4965 | 0.250 | [0.20, 0.60] | normal | — | -0.06 | 0.533 (comodidad) | 7.15 | 0.00 | 0 | 50 | −9(18; 0.00) +(0.03) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); cvd_adjacent: 1 contacto(s) |
| 545 | 5 | comodidad | Ho+Tu | 8 | 1 (0) | 10 | deep | 20 | 2860 | 0.375 | [0.20, 0.60] | normal | — | -0.24 | 0.374 (comodidad) | 3.55 | 0.06 | 0 | 57 | +(0.00) −11(19; 0.00) | endurecimiento: deep; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 546 | 6 | reto | Fl+Ho (D) | 9 | 2 (1) | 11 | walk | 21 | 553 | 0.800 | [0.75, 0.90) | hard | hard | 0.24 | 0.700 (reto) | 3.14 | 0.05 | 0 | 56 | +(0.00) −14(20; 0.00) | relajacion: walk con 1 vacio; cvd_adjacent: 4 contacto(s) |
| 547 | 7 | comodidad | C | 8 | 2 (2) | 10 | walk | 17 | 1048 | 0.600 | [0.20, 0.60] | normal | — | -0.10 | 0.493 (comodidad) | 4.76 | 0.05 | 0 | 46 | −1(15; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); cvd_adjacent: 2 contacto(s) |
| 548 | 8 | comodidad | Ba+Tu | 8 | 2 (2) | 11 | walk | 20 | 42864 | 0.200 | [0.20, 0.60] | normal | — | 0.04 | 0.600 (comodidad) | 8.55 | 0.00 | 0 | 53 | −14(19; 0.00) +(0.00) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4); 1 ronda(s) extra de candidatos turbios; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 549 | 9 | reto | La+Ln (D) | 9 | 1 (1) | 12 | walk | 19 | 394 | 0.725 | [0.75, 0.90) | normal | hard | 0.36 | 0.755 (reto) | 5.26 | 0.17 | 0 | 54 | +(0.23) −7(18; 0.00) | relajacion: walk con 1 vacio; fuera de ventana (relajacion agotada); 1 ronda(s) extra de candidatos |
| 550 | 10 | comodidad | C | 8 | 2 (2) | 10 | walk | 19 | 4966 | 0.225 | [0.20, 0.60] | normal | — | -0.20 | 0.420 (comodidad) | 7.00 | 0.00 | 0 | 50 | −4(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 551 | 11 | comodidad | Ag+Ll | 8 | 2 (0) | 10 | walk | 17 | 2045 | 0.325 | [0.20, 0.60] | normal | — | -0.22 | 0.401 (comodidad) | 6.29 | 0.09 | 0 | 46 | −3(16; 0.08) | alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 552 | 12 | reto | Ln+Re (D) | 9 | 1 (0) | 10 | walk | 18 | 24432 | 0.875 | [0.75, 0.90) | hard | hard | 0.37 | 0.758 (reto) | 5.22 | 0.18 | 0 | 48 | +(0.03) −4(16; 0.00) | relajacion: walk con 1 vacio |
| 553 | 13 | comodidad | C | 8 | 2 (2) | 10 | walk | 18 | 2037 | 0.575 | [0.20, 0.60] | normal | — | 0.03 | 0.593 (comodidad) | 5.89 | 0.00 | 0 | 48 | −9(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 554 | 14 | comodidad | La+Al | 8 | 1 (1) | 11 | walk | 17 | 403 | 0.475 | [0.20, 0.60] | normal | — | -0.31 | 0.307 (comodidad) | 3.18 | 0.07 | 0 | 46 | +(0.00) −4(16; 0.00) |  |
| 555 | 15 | reto | Ho+Tu (D) | 9 | 1 (0) | 11 | deep | 25 | 14145 | 0.825 | [0.75, 0.90) | hard | hard | 0.73 | 0.874 (reto) | 4.72 | 0.05 | 0 | 65 | +(0.00) −11(23; 0.18) | turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 556 | 16 | comodidad | C | 8 | 2 (2) | 10 | walk | 20 | 6842 | 0.450 | [0.20, 0.60] | normal | — | -0.01 | 0.569 (comodidad) | 5.35 | 0.00 | 0 | 50 | −4(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 557 | 17 | comodidad | Br+Tu | 8 | 1 (1) | 10 | walk | 17 | 5796 | 0.325 | [0.20, 0.60] | normal | — | -0.23 | 0.379 (comodidad) | 6.29 | 0.00 | 0 | 49 | +(0.00) −12(16; 0.00) | turbia: 2 dosis ocultas en 1 cuia(s), 1 realidades |
| 558 | 18 | reto | Fl (D) | 9 | 2 (2) | 11 | fill | 27 | 1004 | 0.825 | [0.75, 0.90) | hard | hard | 0.77 | 0.886 (reto) | 3.15 | 0.08 | 0 | 66 | −1(24; 0.00) +(0.03) | relajacion: fill con 2 vacios (03 sec. 4.4); cvd_adjacent: 1 contacto(s) |
| 559 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 17 | 1339 | 0.350 | [0.20, 0.40] | normal | — | -0.36 | 0.254 (comodidad) | 5.82 | 0.00 | 0 | 46 | −8(16; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); cvd_adjacent: 1 contacto(s) |
| 560 | 20 | hito | Fl+Re+A2 (MD) | 12 | 2 (0) | 14 | deep | 30 | 141989 | 0.925 | [0.90, 1.00] | very_hard | very_hard | 1.31 | 0.976 (hito) | 10.07 | 0.00 | 2 | 76 | −1(25; 0.55) | relajacion: deep con 60.000 nodos; Aguape espeso: dos vacios, bajo la mata; alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 561 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 18 | 3392 | 0.150 | [0.00, 0.30) | normal | — | -0.71 | 0.051 (recuperacion) | 4.11 | 0.00 | 0 | 46 | −13(17; 0.00) +(0.00) |  |
| 562 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1954 | 0.025 | [0.00, 0.30) | normal | — | -0.86 | 0.015 (recuperacion) | 5.81 | 0.00 | 0 | 44 | −5(15; 0.00) +(0.00) | cvd_adjacent: 1 contacto(s); 1 ronda(s) de banda |
| 563 | 3 | comodidad | Ma+Tu | 8 | 1 (0) | 10 | deep | 19 | 4321 | 0.325 | [0.20, 0.60] | normal | — | -0.16 | 0.455 (comodidad) | 6.26 | 0.00 | 0 | 55 | +(0.00) −2(18; 0.03) | endurecimiento: deep; turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 564 | 4 | comodidad | C | 8 | 2 (2) | 10 | walk | 16 | 1195 | 0.450 | [0.20, 0.60] | normal | — | -0.19 | 0.428 (comodidad) | 6.38 | 0.06 | 0 | 44 | −2(15; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 565 | 5 | comodidad | La+B2 | 8 | 1 (1) | 11 | deep | 16 | 1083 | 0.350 | [0.20, 0.60] | normal | — | -0.35 | 0.264 (comodidad) | 5.81 | 0.00 | 0 | 46 | +(0.00) −3(15; 0.00) | endurecimiento: deep |
| 566 | 6 | reto | Ag+Ma+Re (D) | 9 | 3 (0) | 12 | walk | 18 | 5522 | 0.750 | [0.75, 0.90) | hard | hard | 0.33 | 0.744 (reto) | 7.61 | 0.06 | 0 | 50 | +(0.48) −1(17; 0.38) | relajacion: walk con 1 vacio; Aguape con una mecanica que ocupa el vacio: un vacio mas, bajo la mata; cvd_adjacent: 1 contacto(s) |
| 567 | 7 | comodidad | C | 8 | 1 (1) | 9 | walk | 16 | 262 | 0.450 | [0.20, 0.60] | normal | — | -0.56 | 0.104 (comodidad) | 2.25 | 0.06 | 0 | 44 | +(0.00) −1(15; 0.00) |  |
| 568 | 8 | comodidad | Ag+Ha | 8 | 1 (0) | 10 | walk | 16 | 1889 | 0.500 | [0.20, 0.60] | normal | — | -0.32 | 0.295 (comodidad) | 4.69 | 0.05 | 0 | 40 | +(0.18) −1(13; 0.23) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4) |
| 569 | 9 | reto | B2+Ba (D) | 9 | 1 (1) | 11 | deep | 23 | 13292 | 0.750 | [0.75, 0.90) | hard | hard | 0.39 | 0.773 (reto) | 3.48 | 0.04 | 0 | 62 | +(0.00) −13(21; 0.00) |  |
| 570 | 10 | comodidad | La | 8 | 1 (1) | 11 | walk | 16 | 1694 | 0.225 | [0.20, 0.60] | normal | — | -0.39 | 0.232 (comodidad) | 6.88 | 0.00 | 0 | 43 | +(0.00) −2(15; 0.00) |  |
| 571 | 11 | comodidad | Ho+Tu | 8 | 1 (0) | 10 | deep | 19 | 957 | 0.350 | [0.20, 0.60] | normal | — | -0.51 | 0.129 (comodidad) | 2.63 | 0.02 | 0 | 53 | +(0.00) −11(17; 0.00) | endurecimiento: deep; turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 572 | 12 | reto | Ma+Ba (D) | 9 | 1 (0) | 11 | walk | 23 | 6907 | 0.800 | [0.75, 0.90) | hard | hard | 0.53 | 0.812 (reto) | 4.61 | 0.08 | 0 | 59 | +(0.10) −4(21; 0.00) | relajacion: walk con 1 vacio; cvd_adjacent: 2 contacto(s) |
| 573 | 13 | comodidad | C | 8 | 2 (2) | 10 | walk | 20 | 1741 | 0.600 | [0.20, 0.60] | normal | — | 0.17 | 0.660 (comodidad) | 5.15 | 0.01 | 0 | 52 | −13(19; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); cvd_adjacent: 1 contacto(s) |
| 574 | 14 | comodidad | C | 8 | 1 (1) | 9 | walk | 18 | 334 | 0.325 | [0.20, 0.60] | normal | — | -0.51 | 0.133 (comodidad) | 2.61 | 0.04 | 0 | 46 | +(0.00) −2(16; 0.00) |  |
| 575 | 15 | reto | Ma+Tu (D) | 9 | 1 (0) | 11 | deep | 21 | 10577 | 0.800 | [0.75, 0.90) | hard | hard | 0.54 | 0.816 (reto) | 4.86 | 0.21 | 0 | 59 | −4(18; 0.00) | turbia: 3 dosis ocultas en 2 cuia(s), 2 realidades; alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 576 | 16 | comodidad | C | 8 | 1 (1) | 9 | walk | 16 | 265 | 0.350 | [0.20, 0.60] | normal | — | -0.74 | 0.043 (recuperacion) | 2.38 | 0.00 | 0 | 44 | +(0.15) −3(15; 0.13) |  |
| 577 | 17 | comodidad | Br+Tu | 8 | 1 (1) | 10 | walk | 18 | 2880 | 0.275 | [0.20, 0.60] | normal | — | 0.00 | 0.576 (comodidad) | 8.11 | 0.00 | 0 | 53 | +(0.00) −5(17; 0.00) | 1 ronda(s) extra de candidatos turbios; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 578 | 18 | reto | B2+Re (D) | 9 | 1 (1) | 11 | deep | 20 | 23339 | 0.775 | [0.75, 0.90) | hard | hard | 0.54 | 0.815 (reto) | 7.20 | 0.01 | 0 | 54 | +(0.00) −1(19; 0.00) |  |
| 579 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 19 | 2495 | 0.300 | [0.20, 0.40] | normal | — | -0.35 | 0.260 (comodidad) | 3.95 | 0.08 | 0 | 50 | −1(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 580 | 20 | hito | Ba+B2+Ag (MD) | 11 | 1 (0) | 13 | deep | 28 | 4182 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.24 | 0.969 (hito) | 4.14 | 0.41 | 0 | 71 | −12(25; 0.43) | cvd_adjacent: 2 contacto(s); alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 581 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 955 | 0.075 | [0.00, 0.30) | normal | — | -0.80 | 0.026 (recuperacion) | 5.75 | 0.00 | 0 | 44 | −7(15; 0.00) +(0.00) |  |
| 582 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 18 | 2766 | 0.025 | [0.00, 0.30) | normal | — | -0.64 | 0.068 (recuperacion) | 6.17 | 0.00 | 0 | 46 | −10(17; 0.00) +(0.00) |  |
| 583 | 3 | comodidad | Pi | 8 | 1 (1) | 10 | walk | 19 | 3756 | 0.450 | [0.20, 0.60] | normal | — | 0.19 | 0.668 (comodidad) | 7.00 | 0.00 | 0 | 50 | +(0.20) −6(17; 0.00) |  |
| 584 | 4 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 16 | 1310 | 0.250 | [0.20, 0.60] | normal | — | -0.34 | 0.276 (comodidad) | 7.00 | 0.00 | 0 | 48 | +(0.00) −8(15; 0.00) | turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 585 | 5 | comodidad | Ag+Ha | 8 | 1 (0) | 10 | walk | 16 | 1397 | 0.400 | [0.20, 0.60] | normal | — | -0.33 | 0.281 (comodidad) | 6.06 | 0.02 | 0 | 44 | −3(15; 0.10) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4); alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 586 | 6 | reto | Pi+Ha (D) | 9 | 1 (1) | 11 | deep | 20 | 22892 | 0.825 | [0.75, 0.90) | hard | hard | 0.32 | 0.741 (reto) | 4.80 | 0.00 | 0 | 54 | +(0.05) −7(19; 0.10) |  |
| 587 | 7 | comodidad | C | 8 | 1 (1) | 9 | walk | 17 | 137 | 0.400 | [0.20, 0.60] | normal | — | -0.43 | 0.196 (comodidad) | 2.47 | 0.12 | 0 | 42 | +(0.00) −5(15; 0.10) |  |
| 588 | 8 | comodidad | Ho+Tu | 8 | 1 (0) | 10 | deep | 18 | 2966 | 0.275 | [0.20, 0.60] | normal | — | -0.69 | 0.053 (recuperacion) | 2.89 | 0.00 | 0 | 53 | +(0.00) −7(17; 0.00) | endurecimiento: deep; 1 ronda(s) extra de candidatos turbios; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 589 | 9 | reto | Ha+B2 (D) | 9 | 1 (1) | 11 | deep | 21 | 1519 | 0.875 | [0.75, 0.90) | hard | hard | 0.44 | 0.787 (reto) | 3.86 | 0.06 | 0 | 58 | +(0.00) −2(20; 0.00) |  |
| 590 | 10 | comodidad | C | 8 | 2 (2) | 10 | walk | 18 | 1883 | 0.350 | [0.20, 0.60] | normal | — | -0.18 | 0.437 (comodidad) | 6.22 | 0.04 | 0 | 48 | −2(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 591 | 11 | comodidad | La | 8 | 1 (1) | 11 | deep | 18 | 2407 | 0.375 | [0.20, 0.60] | normal | — | -0.35 | 0.266 (comodidad) | 3.94 | 0.00 | 0 | 48 | +(0.00) −11(17; 0.00) | endurecimiento: deep; 2 ronda(s) de banda |
| 592 | 12 | reto | Pi+B2+Ag (D) | 9 | 1 (0) | 12 | deep | 25 | 131557 | 0.775 | [0.75, 0.90) | hard | hard | 0.73 | 0.878 (reto) | 5.72 | 0.02 | 0 | 63 | −5(20; 0.00) | cvd_adjacent: 1 contacto(s); alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 593 | 13 | comodidad | C | 8 | 2 (2) | 10 | walk | 18 | 3415 | 0.275 | [0.20, 0.60] | normal | — | -0.31 | 0.298 (comodidad) | 6.22 | 0.00 | 0 | 46 | −13(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); cvd_adjacent: 2 contacto(s) |
| 594 | 14 | comodidad | La+Al | 8 | 1 (1) | 11 | walk | 17 | 4061 | 0.225 | [0.20, 0.60] | normal | — | -0.05 | 0.546 (comodidad) | 9.06 | 0.00 | 0 | 46 | +(0.00) −7(16; 0.00) | cvd_adjacent: 1 contacto(s); 1 ronda(s) de banda |
| 595 | 15 | reto | Ba+Tu (D) | 9 | 1 (1) | 11 | deep | 25 | 17109 | 0.875 | [0.75, 0.90) | hard | hard | 0.81 | 0.898 (reto) | 4.28 | 0.03 | 0 | 69 | +(0.05) −1(24; 0.20) | relajacion: turbias con el vacio del rol y la holgura; 2 ronda(s) extra de candidatos turbios; cvd_adjacent: 1 contacto(s); turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 596 | 16 | comodidad | C | 8 | 2 (2) | 10 | walk | 16 | 2835 | 0.375 | [0.20, 0.60] | normal | — | -0.33 | 0.280 (comodidad) | 6.56 | 0.00 | 0 | 44 | −2(15; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 597 | 17 | comodidad | Br+Tu | 8 | 1 (1) | 10 | walk | 16 | 1158 | 0.300 | [0.20, 0.60] | normal | — | -0.25 | 0.365 (comodidad) | 7.25 | 0.00 | 0 | 48 | +(0.00) −7(15; 0.00) | turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 598 | 18 | reto | Pi+Ba (D) | 9 | 0 (0) | 11 | fill | 29 | 220922 | 0.875 | [0.75, 0.90) | hard | hard | 1.05 | 0.950 (reto) | 4.03 | 0.03 | 0 | 69 | +(0.03) −2(26; 0.05) | cvd_adjacent: 2 contacto(s) |
| 599 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 21 | 7309 | 0.200 | [0.20, 0.40] | normal | — | -0.37 | 0.246 (comodidad) | 4.19 | 0.00 | 0 | 54 | −10(20; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 600 | 20 | hito | Ha+Ba+A2 (MD) | 11 | 1 (0) | 13 | deep | 29 | 2507 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.17 | 0.965 (hito) | 3.48 | 0.35 | 4 | 73 | +(0.80) −9(26; 0.75) | Aguape espeso: dos vacios, bajo la mata; cvd_adjacent: 1 contacto(s) |
| 601 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 2630 | 0.175 | [0.00, 0.30) | normal | — | -0.60 | 0.085 (recuperacion) | 6.00 | 0.04 | 0 | 42 | −4(15; 0.00) +(0.00) | cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 602 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1797 | 0.250 | [0.00, 0.30) | normal | — | -0.60 | 0.081 (recuperacion) | 5.56 | 0.00 | 0 | 44 | −1(15; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 603 | 3 | comodidad | Ln | 8 | 1 (1) | 9 | walk | 20 | 152 | 0.475 | [0.20, 0.60] | normal | — | -0.11 | 0.487 (comodidad) | 2.75 | 0.05 | 0 | 48 | +(0.00) −2(17; 0.13) |  |
| 604 | 4 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 16 | 401 | 0.600 | [0.20, 0.60] | normal | — | -0.16 | 0.446 (comodidad) | 3.75 | 0.08 | 0 | 46 | +(0.00) −8(15; 0.00) | turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 605 | 5 | comodidad | Ag | 8 | 1 (0) | 10 | walk | 18 | 482 | 0.375 | [0.20, 0.60] | normal | — | -0.06 | 0.525 (comodidad) | 7.22 | 0.01 | 0 | 50 | −5(17; 0.00) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4); cvd_adjacent: 1 contacto(s); alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 606 | 6 | reto | Ln+Re (D) | 9 | 1 (0) | 10 | walk | 19 | 27299 | 0.800 | [0.75, 0.90) | hard | hard | 0.27 | 0.713 (reto) | 3.74 | 0.24 | 0 | 48 | +(0.20) −3(17; 0.18) | relajacion: walk con 1 vacio; 1 ronda(s) extra de candidatos |
| 607 | 7 | comodidad | C | 8 | 1 (1) | 9 | walk | 16 | 149 | 0.500 | [0.20, 0.60] | normal | — | -0.47 | 0.160 (comodidad) | 2.19 | 0.09 | 0 | 42 | +(0.08) −2(14; 0.00) | 1 ronda(s) extra de candidatos |
| 608 | 8 | comodidad | Ho+Tu | 8 | 1 (0) | 10 | deep | 20 | 2921 | 0.250 | [0.20, 0.60] | normal | — | -0.36 | 0.251 (comodidad) | 4.50 | 0.00 | 0 | 52 | +(0.00) −4(18; 0.00) | endurecimiento: deep; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 609 | 9 | reto | Li+Ll (D) | 9 | 2 (1) | 11 | walk | 20 | 4305 | 0.800 | [0.75, 0.90) | hard | hard | 0.39 | 0.771 (reto) | 4.80 | 0.08 | 0 | 50 | +(0.30) −12(19; 0.08) | relajacion: walk con 1 vacio; 1 ronda(s) extra de candidatos |
| 610 | 10 | comodidad | C | 8 | 1 (1) | 9 | walk | 17 | 147 | 0.525 | [0.20, 0.60] | normal | — | -0.04 | 0.548 (comodidad) | 2.82 | 0.27 | 0 | 46 | +(0.08) −1(16; 0.10) | 1 ronda(s) extra de candidatos |
| 611 | 11 | comodidad | Br+Tu | 8 | 1 (1) | 10 | walk | 17 | 1395 | 0.225 | [0.20, 0.60] | normal | — | -0.30 | 0.320 (comodidad) | 6.35 | 0.05 | 0 | 51 | +(0.00) −2(16; 0.00) | turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 612 | 12 | reto | Ag+Ll (D) | 9 | 2 (0) | 11 | fill | 25 | 20693 | 0.875 | [0.75, 0.90) | hard | hard | 0.72 | 0.873 (reto) | 4.12 | 0.05 | 0 | 64 | +(0.23) −6(23; 0.55) | Aguape con una mecanica que ocupa el vacio: un vacio mas, bajo la mata; 1 ronda(s) extra de candidatos |
| 613 | 13 | comodidad | La | 8 | 1 (1) | 11 | walk | 16 | 425 | 0.300 | [0.20, 0.60] | normal | — | -0.35 | 0.266 (comodidad) | 6.38 | 0.00 | 0 | 48 | +(0.00) −1(15; 0.00) | 1 ronda(s) extra de candidatos |
| 614 | 14 | comodidad | Ch | 8 | 1 (1) | 10 | walk | 20 | 4829 | 0.500 | [0.20, 0.60] | normal | — | 0.17 | 0.662 (comodidad) | 5.00 | 0.05 | 0 | 50 | +(0.00) −2(19; 0.00) |  |
| 615 | 15 | reto | Li+Re (D) | 9 | 2 (2) | 11 | fill | 28 | 115404 | 0.850 | [0.75, 0.90) | hard | hard | 1.11 | 0.956 (hito) | 5.64 | 0.03 | 0 | 66 | −8(25; 0.08) +(0.10) | relajacion: fill con 2 vacios (03 sec. 4.4); 1 ronda(s) extra de candidatos |
| 616 | 16 | comodidad | C | 8 | 2 (2) | 10 | walk | 18 | 2834 | 0.275 | [0.20, 0.60] | normal | — | -0.56 | 0.101 (comodidad) | 4.06 | 0.00 | 0 | 46 | −1(16; 0.00) +(0.03) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 617 | 17 | comodidad | La+Fl | 8 | 1 (1) | 11 | walk | 17 | 681 | 0.300 | [0.20, 0.60] | normal | — | -0.27 | 0.349 (comodidad) | 6.29 | 0.00 | 0 | 49 | +(0.00) −12(16; 0.00) |  |
| 618 | 18 | reto | Ln+Ll (D) | 9 | 3 (2) | 12 | fill | 28 | 297064 | 0.850 | [0.75, 0.90) | hard | hard | 1.14 | 0.961 (hito) | 6.25 | 0.00 | 0 | 66 | −5(23; 0.00) +(0.00) | relajacion: fill con 2 vacios (03 sec. 4.4); risk_adjacent: 2 contacto(s); cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 619 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 18 | 3373 | 0.200 | [0.20, 0.40] | normal | — | -0.26 | 0.356 (comodidad) | 7.56 | 0.00 | 0 | 46 | −5(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 620 | 20 | hito | Ln+Ll+Ag (MD) | 11 | 2 (0) | 13 | fill | 33 | 15225 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.34 | 0.978 (hito) | 3.39 | 0.20 | 6 | 80 | −4(29; 0.65) | Aguape con una mecanica que ocupa el vacio: un vacio mas, bajo la mata; 1 ronda(s) extra de candidatos; alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 621 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 973 | 0.050 | [0.00, 0.30) | normal | — | -0.83 | 0.022 (recuperacion) | 5.81 | 0.00 | 0 | 44 | −5(15; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 622 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 18 | 4677 | 0.000 | [0.00, 0.30) | normal | — | -0.60 | 0.080 (recuperacion) | 6.72 | 0.00 | 0 | 46 | −8(16; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 623 | 3 | comodidad | Ma+Tu | 8 | 1 (0) | 10 | deep | 22 | 14118 | 0.275 | [0.20, 0.60] | normal | — | -0.01 | 0.570 (comodidad) | 4.64 | 0.10 | 0 | 59 | +(0.00) −2(19; 0.00) | endurecimiento: deep; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 624 | 4 | comodidad | C | 8 | 2 (2) | 10 | walk | 20 | 10819 | 0.275 | [0.20, 0.60] | normal | — | -0.10 | 0.494 (comodidad) | 6.50 | 0.00 | 0 | 50 | −1(19; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 625 | 5 | comodidad | Ag+Ba | 8 | 1 (0) | 10 | walk | 17 | 3913 | 0.550 | [0.20, 0.60] | normal | — | -0.02 | 0.563 (comodidad) | 5.53 | 0.10 | 0 | 50 | −7(14; 0.03) | alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 626 | 6 | reto | Ma+Ba (D) | 9 | 1 (0) | 11 | walk | 19 | 28375 | 0.875 | [0.75, 0.90) | hard | hard | 0.28 | 0.715 (reto) | 5.58 | 0.00 | 0 | 53 | +(0.15) −13(18; 0.18) | relajacion: walk con 1 vacio; 1 ronda(s) extra de candidatos |
| 627 | 7 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 17 | 2823 | 0.475 | [0.20, 0.60] | normal | — | 0.16 | 0.653 (comodidad) | 8.06 | 0.00 | 0 | 52 | +(0.00) −7(16; 0.00) | 1 ronda(s) extra de candidatos; turbia: 2 dosis ocultas en 1 cuia(s), 1 realidades |
| 628 | 8 | comodidad | La+B2 | 8 | 1 (1) | 11 | walk | 16 | 102 | 0.250 | [0.20, 0.60] | normal | — | -0.36 | 0.249 (comodidad) | 6.81 | 0.00 | 0 | 48 | +(0.00) −2(15; 0.00) | cvd_adjacent: 1 contacto(s) |
| 629 | 9 | reto | Ll+Ma (D) | 9 | 2 (0) | 11 | deep | 21 | 23550 | 0.875 | [0.75, 0.90) | hard | hard | 0.31 | 0.735 (reto) | 4.00 | 0.02 | 0 | 56 | +(0.35) −11(19; 0.05) | 1 ronda(s) extra de candidatos |
| 630 | 10 | comodidad | C | 8 | 2 (2) | 10 | walk | 22 | 5504 | 0.275 | [0.20, 0.60] | normal | — | 0.08 | 0.623 (comodidad) | 6.45 | 0.00 | 0 | 56 | −11(20; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 631 | 11 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 18 | 2241 | 0.350 | [0.20, 0.60] | normal | — | 0.04 | 0.603 (comodidad) | 7.28 | 0.03 | 0 | 52 | +(0.00) −1(17; 0.00) | turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 632 | 12 | reto | Ma+Ho (D) | 9 | 2 (0) | 11 | deep | 23 | 16302 | 0.825 | [0.75, 0.90) | hard | hard | 0.38 | 0.765 (reto) | 3.83 | 0.00 | 0 | 58 | +(0.28) −4(21; 0.23) | cvd_adjacent: 2 contacto(s); 2 ronda(s) extra de candidatos |
| 633 | 13 | comodidad | C | 8 | 2 (2) | 10 | walk | 20 | 4080 | 0.425 | [0.20, 0.60] | normal | — | -0.00 | 0.574 (comodidad) | 5.70 | 0.00 | 0 | 50 | −11(19; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 634 | 14 | comodidad | Al | 8 | 1 (1) | 10 | walk | 16 | 2488 | 0.300 | [0.20, 0.60] | normal | — | -0.26 | 0.352 (comodidad) | 7.13 | 0.00 | 0 | 44 | +(0.00) −4(15; 0.00) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4) |
| 635 | 15 | reto | Ma+Tu (D) | 9 | 1 (0) | 11 | deep | 19 | 5763 | 0.850 | [0.75, 0.90) | hard | hard | 0.53 | 0.813 (reto) | 8.11 | 0.00 | 0 | 58 | +(0.00) −1(18; 0.00) | cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 636 | 16 | comodidad | C | 8 | 2 (2) | 10 | walk | 21 | 4486 | 0.350 | [0.20, 0.60] | normal | — | 0.13 | 0.643 (comodidad) | 6.76 | 0.01 | 0 | 54 | −4(20; 0.00) +(0.10) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 637 | 17 | comodidad | La+Ha | 8 | 1 (1) | 11 | walk | 17 | 6405 | 0.325 | [0.20, 0.60] | normal | — | -0.21 | 0.404 (comodidad) | 6.47 | 0.00 | 0 | 52 | +(0.00) −5(15; 0.00) |  |
| 638 | 18 | reto | Ag+Ba+Ho (D) | 9 | 2 (0) | 12 | walk | 26 | 14891 | 0.750 | [0.75, 0.90) | hard | hard | 0.68 | 0.863 (reto) | 4.85 | 0.01 | 1 | 65 | +(0.45) −12(24; 0.50) | relajacion: walk con 1 vacio; Aguape con una mecanica que ocupa el vacio: un vacio mas, bajo la mata; 1 ronda(s) extra de candidatos |
| 639 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 18 | 1508 | 0.200 | [0.20, 0.40] | normal | — | -0.38 | 0.236 (comodidad) | 6.50 | 0.00 | 0 | 48 | −6(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 640 | 20 | hito | Li+Ba+A2 (MD) | 11 | 1 (0) | 13 | deep | 32 | 2954 | 0.950 | [0.90, 1.00] | very_hard | very_hard | 1.22 | 0.967 (hito) | 3.44 | 0.22 | 2 | 75 | −1(29; 0.28) | Aguape espeso: dos vacios, bajo la mata; cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos; alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 641 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 18 | 1714 | 0.050 | [0.00, 0.30) | normal | — | -0.64 | 0.065 (recuperacion) | 5.83 | 0.00 | 0 | 48 | −2(16; 0.00) +(0.00) | cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 642 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 19 | 1821 | 0.200 | [0.00, 0.30) | normal | — | -0.64 | 0.067 (recuperacion) | 3.42 | 0.00 | 0 | 48 | −2(18; 0.00) +(0.00) | cvd_adjacent: 2 contacto(s); 1 ronda(s) extra de candidatos |
| 643 | 3 | comodidad | Pi | 8 | 1 (1) | 10 | walk | 16 | 229 | 0.375 | [0.20, 0.60] | normal | — | 0.00 | 0.580 (comodidad) | 8.63 | 0.00 | 0 | 46 | +(0.05) −13(15; 0.00) |  |
| 644 | 4 | comodidad | La | 8 | 1 (1) | 11 | walk | 16 | 848 | 0.325 | [0.20, 0.60] | normal | — | -0.32 | 0.293 (comodidad) | 6.31 | 0.00 | 0 | 44 | +(0.00) −5(15; 0.00) | 1 ronda(s) de banda |
| 645 | 5 | comodidad | Br+Tu | 8 | 1 (1) | 10 | walk | 18 | 3747 | 0.500 | [0.20, 0.60] | normal | — | 0.07 | 0.622 (comodidad) | 5.50 | 0.07 | 0 | 50 | +(0.00) −3(16; 0.00) | turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 646 | 6 | reto | Pi+Br+Ha (D) | 9 | 1 (1) | 12 | deep | 19 | 55965 | 0.800 | [0.75, 0.90) | hard | hard | 0.55 | 0.820 (reto) | 7.89 | 0.00 | 0 | 52 | +(0.00) −13(16; 0.00) | 1 ronda(s) extra de candidatos |
| 647 | 7 | comodidad | C | 8 | 2 (2) | 10 | walk | 18 | 3663 | 0.400 | [0.20, 0.60] | normal | — | -0.03 | 0.555 (comodidad) | 7.33 | 0.00 | 0 | 48 | −10(17; 0.00) +(0.10) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 648 | 8 | comodidad | La+Al | 8 | 1 (1) | 11 | walk | 16 | 7004 | 0.375 | [0.20, 0.60] | normal | — | -0.32 | 0.293 (comodidad) | 5.25 | 0.05 | 0 | 50 | +(0.00) −2(15; 0.00) |  |
| 649 | 9 | reto | Pi+Ha (D) | 9 | 1 (1) | 11 | deep | 22 | 43631 | 0.825 | [0.75, 0.90) | hard | hard | 0.82 | 0.899 (reto) | 7.23 | 0.03 | 0 | 54 | +(0.03) −2(20; 0.08) | cvd_adjacent: 2 contacto(s); 1 ronda(s) extra de candidatos |
| 650 | 10 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 16 | 1595 | 0.250 | [0.20, 0.60] | normal | — | -0.42 | 0.198 (comodidad) | 6.25 | 0.00 | 0 | 46 | +(0.00) −1(15; 0.00) | 1 ronda(s) extra de candidatos; turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 651 | 11 | comodidad | C | 8 | 1 (1) | 9 | walk | 17 | 267 | 0.325 | [0.20, 0.60] | normal | — | -0.29 | 0.325 (comodidad) | 3.24 | 0.24 | 0 | 44 | +(0.03) −1(16; 0.03) | cvd_adjacent: 2 contacto(s) |
| 652 | 12 | reto | Ag+Br+Re (D) | 9 | 1 (0) | 11 | deep | 20 | 3419 | 0.825 | [0.75, 0.90) | hard | hard | 0.88 | 0.913 (reto) | 10.65 | 0.00 | 0 | 58 | +(0.63) −6(18; 0.55) | cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 653 | 13 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 18 | 4059 | 0.250 | [0.20, 0.60] | normal | — | -0.10 | 0.497 (comodidad) | 6.61 | 0.08 | 0 | 53 | +(0.00) −12(17; 0.00) | 1 ronda(s) extra de candidatos; turbia: 1 dosis ocultas en 1 cuia(s), 1 realidades |
| 654 | 14 | comodidad | Ag+Pi | 8 | 1 (0) | 10 | walk | 16 | 2262 | 0.450 | [0.20, 0.60] | normal | — | -0.33 | 0.288 (comodidad) | 5.25 | 0.05 | 0 | 42 | −3(14; 0.00) | alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 655 | 15 | reto | Br+Tu (D) | 9 | 1 (1) | 11 | deep | 25 | 20404 | 0.850 | [0.75, 0.90) | hard | hard | 1.05 | 0.950 (reto) | 6.36 | 0.06 | 0 | 65 | +(0.00) −1(22; 0.10) | relajacion: turbias con el vacio del rol y la holgura; risk_adjacent: 1 contacto(s); cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos; turbia: 3 dosis ocultas en 2 cuia(s), 6 realidades |
| 656 | 16 | comodidad | C | 8 | 1 (1) | 9 | walk | 17 | 224 | 0.525 | [0.20, 0.60] | normal | — | -0.38 | 0.233 (comodidad) | 2.53 | 0.02 | 0 | 44 | +(0.00) −1(15; 0.00) | 1 ronda(s) extra de candidatos |
| 657 | 17 | comodidad | Ag+B2 | 8 | 1 (0) | 10 | walk | 16 | 1737 | 0.400 | [0.20, 0.60] | normal | — | -0.30 | 0.311 (comodidad) | 5.56 | 0.09 | 0 | 46 | −6(14; 0.08) | alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 658 | 18 | reto | Ag+Ha+Re (D) | 9 | 2 (1) | 11 | fill | 26 | 292360 | 0.850 | [0.75, 0.90) | hard | hard | 1.30 | 0.974 (hito) | 8.38 | 0.00 | 0 | 62 | +(0.48) −7(22; 0.38) | relajacion: fill con 2 vacios (03 sec. 4.4); 1 ronda(s) extra de candidatos |
| 659 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 17 | 1554 | 0.325 | [0.20, 0.40] | normal | — | -0.25 | 0.366 (comodidad) | 7.06 | 0.00 | 0 | 48 | −8(16; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 660 | 20 | hito | Ln+Ha+A2 (MD) | 11 | 2 (0) | 13 | fill | 36 | 5516 | 0.975 | [0.90, 1.00] | very_hard | very_hard | 1.49 | 0.985 (hito) | 2.78 | 0.18 | 4 | 84 | +(0.70) −13(31; 0.43) | relajacion: fill con 2 vacios (03 sec. 4.4); cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 661 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 17 | 1211 | 0.200 | [0.00, 0.30) | normal | — | -0.64 | 0.066 (recuperacion) | 5.00 | 0.00 | 0 | 46 | −12(16; 0.00) +(0.00) | cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos |
| 662 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 19 | 4783 | 0.175 | [0.00, 0.30) | normal | — | -0.61 | 0.073 (recuperacion) | 3.89 | 0.00 | 0 | 48 | −10(18; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 663 | 3 | comodidad | La+Li | 8 | 1 (1) | 11 | walk | 17 | 671 | 0.250 | [0.20, 0.60] | normal | — | -0.34 | 0.270 (comodidad) | 5.82 | 0.03 | 0 | 45 | +(0.00) −2(15; 0.00) |  |
| 664 | 4 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 17 | 906 | 0.275 | [0.20, 0.60] | normal | — | -0.70 | 0.052 (recuperacion) | 2.71 | 0.00 | 0 | 48 | +(0.00) −3(16; 0.00) | turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 665 | 5 | comodidad | Ag+Ha | 8 | 1 (0) | 10 | walk | 16 | 1701 | 0.500 | [0.20, 0.60] | normal | — | -0.33 | 0.286 (comodidad) | 4.25 | 0.09 | 0 | 42 | −8(15; 0.08) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4); alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 666 | 6 | reto | Li+Ha (D) | 9 | 1 (1) | 10 | walk | 18 | 276 | 0.875 | [0.75, 0.90) | hard | hard | 0.05 | 0.607 (comodidad) | 2.56 | 0.09 | 1 | 46 | +(0.10) −8(16; 0.13) | relajacion: walk con 1 vacio; 1 ronda(s) extra de candidatos |
| 667 | 7 | comodidad | C | 8 | 1 (1) | 9 | walk | 16 | 261 | 0.350 | [0.20, 0.60] | normal | — | -0.72 | 0.048 (recuperacion) | 2.56 | 0.00 | 0 | 42 | +(0.10) −1(15; 0.10) | 1 ronda(s) extra de candidatos |
| 668 | 8 | comodidad | Ho+Tu | 8 | 1 (0) | 10 | deep | 18 | 874 | 0.600 | [0.20, 0.60] | normal | — | -0.29 | 0.323 (comodidad) | 2.83 | 0.00 | 0 | 48 | +(0.00) −2(16; 0.00) | endurecimiento: deep; turbia: 2 dosis ocultas en 1 cuia(s), 1 realidades |
| 669 | 9 | reto | Li+Br (D) | 9 | 1 (1) | 11 | deep | 22 | 3191 | 0.850 | [0.75, 0.90) | hard | hard | 0.37 | 0.761 (reto) | 3.27 | 0.01 | 0 | 58 | +(0.00) −7(21; 0.20) | 1 ronda(s) extra de candidatos |
| 670 | 10 | comodidad | C | 8 | 2 (2) | 10 | walk | 19 | 3135 | 0.500 | [0.20, 0.60] | normal | — | 0.13 | 0.646 (comodidad) | 6.68 | 0.02 | 0 | 50 | −9(18; 0.00) +(0.05) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 671 | 11 | comodidad | Ba+Tu | 8 | 1 (1) | 10 | deep | 19 | 15407 | 0.325 | [0.20, 0.60] | normal | — | 0.01 | 0.581 (comodidad) | 6.53 | 0.03 | 0 | 55 | +(0.00) −6(17; 0.00) | endurecimiento: deep; turbia: 2 dosis ocultas en 1 cuia(s), 1 realidades |
| 672 | 12 | reto | Li+Re (D) | 9 | 2 (2) | 11 | fill | 26 | 40272 | 0.700 | [0.75, 0.90) | normal | hard | 0.59 | 0.832 (reto) | 4.50 | 0.02 | 0 | 64 | −10(22; 0.03) +(0.30) | relajacion: fill con 2 vacios (03 sec. 4.4); fuera de ventana (relajacion agotada); cvd_adjacent: 1 contacto(s); 2 ronda(s) extra de candidatos |
| 673 | 13 | comodidad | C | 8 | 1 (1) | 9 | walk | 17 | 218 | 0.550 | [0.20, 0.60] | normal | — | -0.28 | 0.328 (comodidad) | 2.65 | 0.07 | 0 | 44 | +(0.10) −4(15; 0.28) | 1 ronda(s) extra de candidatos |
| 674 | 14 | comodidad | Ag+Ch | 8 | 1 (0) | 10 | walk | 17 | 2183 | 0.550 | [0.20, 0.60] | normal | — | 0.07 | 0.616 (comodidad) | 4.82 | 0.23 | 0 | 42 | −2(12; 0.23) | alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 675 | 15 | reto | Br+Tu (D) | 9 | 1 (1) | 11 | deep | 22 | 13148 | 0.800 | [0.75, 0.90) | hard | hard | 0.78 | 0.893 (reto) | 6.50 | 0.10 | 0 | 60 | +(0.03) −2(20; 0.05) | relajacion: turbias con el vacio del rol y la holgura; 1 ronda(s) extra de candidatos; turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 676 | 16 | comodidad | C | 8 | 1 (1) | 9 | walk | 17 | 233 | 0.350 | [0.20, 0.60] | normal | — | -0.61 | 0.072 (recuperacion) | 2.41 | 0.02 | 0 | 44 | +(0.03) −8(15; 0.00) | cvd_adjacent: 4 contacto(s); 1 ronda(s) extra de candidatos |
| 677 | 17 | comodidad | La+B2 | 8 | 1 (1) | 11 | deep | 19 | 1154 | 0.200 | [0.20, 0.60] | normal | — | -0.42 | 0.197 (comodidad) | 4.26 | 0.01 | 0 | 51 | +(0.00) −2(18; 0.00) | endurecimiento: deep |
| 678 | 18 | reto | Ha+Re (D) | 9 | 2 (2) | 11 | fill | 28 | 114812 | 0.875 | [0.75, 0.90) | hard | hard | 1.04 | 0.948 (reto) | 5.11 | 0.00 | 0 | 66 | −10(24; 0.00) +(0.08) | relajacion: fill con 2 vacios (03 sec. 4.4); 1 ronda(s) extra de candidatos |
| 679 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 16 | 343 | 0.325 | [0.20, 0.40] | normal | — | -0.37 | 0.245 (comodidad) | 6.81 | 0.00 | 0 | 46 | −4(15; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 680 | 20 | hito | Li+Ha+A2 (MD) | 11 | 2 (0) | 13 | deep | 29 | 1690 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.07 | 0.952 (hito) | 3.52 | 0.26 | 0 | 68 | −3(24; 0.23) | Aguape espeso: dos vacios, bajo la mata; cvd_adjacent: 1 contacto(s); 1 ronda(s) extra de candidatos; alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 681 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1369 | 0.025 | [0.00, 0.30) | normal | — | -0.94 | 0.004 (recuperacion) | 5.06 | 0.00 | 0 | 42 | −4(14; 0.00) +(0.00) |  |
| 682 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1133 | 0.000 | [0.00, 0.30) | normal | — | -0.92 | 0.007 (recuperacion) | 5.56 | 0.00 | 0 | 44 | −6(15; 0.00) +(0.00) |  |
| 683 | 3 | comodidad | Ma+Tu | 8 | 1 (0) | 10 | deep | 20 | 8424 | 0.550 | [0.20, 0.60] | normal | — | 0.24 | 0.698 (comodidad) | 6.45 | 0.00 | 0 | 58 | +(0.00) −4(18; 0.00) | endurecimiento: deep; turbia: 2 dosis ocultas en 1 cuia(s), 1 realidades |
| 684 | 4 | comodidad | C | 8 | 2 (2) | 10 | walk | 21 | 2522 | 0.300 | [0.20, 0.60] | normal | — | 0.01 | 0.582 (comodidad) | 6.38 | 0.00 | 0 | 54 | −1(20; 0.00) +(0.03) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 685 | 5 | comodidad | Ho | 8 | 2 (1) | 10 | walk | 18 | 241 | 0.550 | [0.20, 0.60] | normal | — | -0.22 | 0.395 (comodidad) | 2.67 | 0.04 | 0 | 48 | +(0.00) −2(16; 0.00) |  |
| 686 | 6 | reto | Ma+Ho (D) | 9 | 2 (0) | 11 | deep | 21 | 3608 | 0.725 | [0.75, 0.90) | normal | hard | 0.11 | 0.632 (comodidad) | 3.38 | 0.07 | 0 | 54 | +(0.03) −1(18; 0.00) | fuera de ventana (relajacion agotada); 1 ronda(s) extra de candidatos |
| 687 | 7 | comodidad | C | 8 | 2 (2) | 10 | walk | 19 | 5956 | 0.275 | [0.20, 0.60] | normal | — | -0.13 | 0.474 (comodidad) | 7.00 | 0.00 | 0 | 48 | −10(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 688 | 8 | comodidad | La | 8 | 1 (1) | 11 | walk | 16 | 1121 | 0.200 | [0.20, 0.60] | normal | — | -0.45 | 0.174 (comodidad) | 6.56 | 0.00 | 0 | 45 | +(0.00) −14(15; 0.00) |  |
| 689 | 9 | reto | Ho+B2 (D) | 9 | 1 (0) | 11 | deep | 23 | 2970 | 0.800 | [0.75, 0.90) | hard | hard | 0.34 | 0.749 (reto) | 3.78 | 0.00 | 0 | 60 | +(0.23) −4(22; 0.00) |  |
| 690 | 10 | comodidad | Tu | 8 | 1 (1) | 10 | walk | 16 | 1330 | 0.225 | [0.20, 0.60] | normal | — | -0.79 | 0.031 (recuperacion) | 3.31 | 0.00 | 0 | 46 | +(0.00) −1(15; 0.00) | cvd_adjacent: 1 contacto(s); turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 691 | 11 | comodidad | C | 8 | 1 (1) | 9 | walk | 16 | 347 | 0.600 | [0.20, 0.60] | normal | — | -0.31 | 0.301 (comodidad) | 2.88 | 0.04 | 0 | 42 | +(0.03) −4(14; 0.08) |  |
| 692 | 12 | reto | Ma+Ha (D) | 9 | 2 (1) | 11 | walk | 18 | 3931 | 0.800 | [0.75, 0.90) | hard | hard | 0.38 | 0.765 (reto) | 6.39 | 0.08 | 0 | 50 | +(0.13) −12(17; 0.40) | relajacion: walk con 1 vacio |
| 693 | 13 | comodidad | C | 8 | 1 (1) | 9 | walk | 18 | 120 | 0.600 | [0.20, 0.60] | normal | — | -0.01 | 0.568 (comodidad) | 2.28 | 0.20 | 0 | 48 | +(0.03) −4(17; 0.08) |  |
| 694 | 14 | comodidad | Ma+Ch | 8 | 2 (1) | 11 | walk | 17 | 39228 | 0.200 | [0.20, 0.60] | normal | — | 0.13 | 0.644 (comodidad) | 10.88 | 0.00 | 0 | 44 | +(0.00) −10(16; 0.00) | relajacion: 2 vacios (03 sec. 4.4); 1 ronda(s) de banda |
| 695 | 15 | reto | Ho+Tu (D) | 9 | 1 (0) | 11 | deep | 22 | 5439 | 0.875 | [0.75, 0.90) | hard | hard | 0.46 | 0.796 (reto) | 4.32 | 0.04 | 0 | 62 | +(0.35) −10(20; 0.28) | turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 696 | 16 | comodidad | C | 8 | 2 (2) | 10 | walk | 20 | 4847 | 0.450 | [0.20, 0.60] | normal | — | 0.15 | 0.652 (comodidad) | 6.80 | 0.00 | 0 | 52 | −7(19; 0.00) +(0.03) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 697 | 17 | comodidad | Ba+Tu | 8 | 1 (1) | 10 | deep | 21 | 11936 | 0.425 | [0.20, 0.60] | normal | — | 0.11 | 0.633 (comodidad) | 5.00 | 0.00 | 0 | 58 | +(0.00) −12(19; 0.00) | endurecimiento: deep; 2 ronda(s) extra de candidatos turbios; risk_adjacent: 1 contacto(s); cvd_adjacent: 2 contacto(s); turbia: 2 dosis ocultas en 1 cuia(s), 2 realidades |
| 698 | 18 | reto | Ag+Ha+B2 (D) | 9 | 1 (0) | 11 | deep | 21 | 1333 | 0.875 | [0.75, 0.90) | hard | hard | 0.69 | 0.866 (reto) | 4.90 | 0.25 | 0 | 57 | +(0.55) −5(20; 0.63) |  |
| 699 | 19 | remanso | S | 8 | 2 (2) | 10 | walk | 16 | 1400 | 0.375 | [0.20, 0.40] | normal | — | -0.30 | 0.319 (comodidad) | 6.88 | 0.00 | 0 | 44 | −4(15; 0.03) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 700 | 20 | hito | Fl+Ha+A2 (MD) | 12 | 2 (0) | 14 | deep | 32 | 1219 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.18 | 0.966 (hito) | 3.16 | 0.31 | 2 | 76 | −9(24; 0.20) | Aguape espeso: dos vacios, bajo la mata; alivio plus_empty no publicado (no baja bot_fail 0,20) |

## Par / 2K por fuente frente a 03 §18

| Fuente | Niveles | Par / 2K medio | bot_fail medio | 03 §18 (par / 2K) |
|---|---|---|---|---|
| deep | 89 | 1.22 | 0.73 | 1,17 |
| fill | 29 | 1.54 | 0.87 | 1,53 |
| walk | 282 | 1.08 | 0.35 | 1,09 |

## Debuts

| Nivel | intro_mechanic | skippable | Celda |
|---|---|---|---|
| 303 | `aguape` | false | *Ag |
| 323 | `murky` | false | *Tu |
| 343 | `brote_n2` | false | *B2 |
| 347 | `aguape_n2` | false | *A2 |
| 363 | `lancha` | false | *La |
| 403 | `flor` | false | *Fl |

## Apariciones en los packs (debut real y huecos)

Sobre los tableros publicados, no sobre el calendario: primera aparición (debut real), niveles en los que sale, hueco máximo entre dos apariciones y niveles desde la última hasta el 700 (resumen, punto 6; d.2 regla 4: ≤ 120; level-qa lo exige con QA-GAP-120). Las capacidades cuentan también cuando el generador las usa como forma de tablero (02 §8); la cueva de la Piraña no cuenta como Cuia chica.

| Mecánica o capacidad | Debut del calendario | Debut real | Niveles | Hueco máximo | Hasta el final |
|---|---|---|---|---|---|
| hoja (Ho) | 43 | 332 | 29 | 31 (335→366) | 5 |
| matapi (Ma) | 63 | 326 | 26 | 68 (495→563) | 6 |
| liana (Li) | 83 | 383 | 19 | 89 (520→609) | 20 |
| murky (Tu) | 100 | 315 | 74 | 19 (331→350) | 3 |
| remolino (Re) | 103 | 352 | 20 | 74 (355→429) | 22 |
| barro (Ba) | 123 | 311 | 38 | 31 (640→671) | 3 |
| lluvia (Ll) | 143 | 306 | 15 | 94 (409→503) | 71 |
| brote (Br) | 163 | 312 | 26 | 37 (491→528) | 25 |
| pirana (Pi) | 183 | 369 | 18 | 63 (380→443) | 46 |
| liana_n (Ln) | 243 | 318 | 14 | 109 (360→469) | 40 |
| hoja_atada (Ha) | 283 | 306 | 33 | 69 (320→389) | 0 |
| brote_n2 (B2) | 343 | 343 | 28 | 59 (372→431) | 2 |
| flor (Fl) | 403 | 403 | 19 | 83 (617→700) | 0 |
| aguape (Ag) | 303 | 303 | 53 | 24 (674→698) | 2 |
| aguape_n2 (A2) | 347 | 347 | 12 | 80 (420→500) | 0 |
| lancha (La) | 363 | 363 | 38 | 21 (570→591) | 12 |
| Cuia chica (cap 1-3) | 14 | 308 | 21 | 51 (334→385) | 6 |
| Cuia alta (cap 5-6) | 27 | 374 | 7 | 80 (434→514) | 52 |
| Ch1 (cap 1) | 18 | **no sale** | 0 | — | — |
| Ch3 (cap 3) | 22 | **no sale** | 0 | — | — |
| Al6 (cap 6) | 160 | **no sale** | 0 | — | — |

## Remanso frente a la comodidad del ciclo (c.4)

p19 es «puro, más fácil que la media de comodidad» (c.4; c.3 regla 5: respiro antes del pico): la colocación exige D(p19) < media de D de las comodidades del ciclo que no son tableros de estreno, y level-qa lo comprueba (QA-REMANSO).

| Ciclo | Remanso | D | bot_fail | Par | Comodidades | D medio | bot_fail medio | Puesto del remanso por D (de las comodidades y él) |
|---|---|---|---|---|---|---|---|---|
| 301-320 | 319 | -0.44 | 0.325 | 18 | 8 | -0.22 | 0.391 | 8 de 9 (1 = el más difícil) |
| 321-340 | 339 | -0.33 | 0.325 | 20 | 9 | -0.27 | 0.367 | 6 de 10 (1 = el más difícil) |
| 341-360 | 359 | -0.34 | 0.275 | 17 | 6 | -0.30 | 0.363 | 4 de 7 (1 = el más difícil) |
| 361-380 | 379 | -0.30 | 0.225 | 17 | 8 | -0.10 | 0.328 | 8 de 9 (1 = el más difícil) |
| 381-400 | 399 | -0.22 | 0.250 | 17 | 11 | -0.17 | 0.336 | 9 de 12 (1 = el más difícil) |
| 401-420 | 419 | -0.35 | 0.300 | 19 | 8 | -0.26 | 0.450 | 7 de 9 (1 = el más difícil) |
| 421-440 | 439 | -0.36 | 0.375 | 17 | 11 | -0.22 | 0.414 | 9 de 12 (1 = el más difícil) |
| 441-460 | 459 | -0.24 | 0.350 | 17 | 11 | -0.12 | 0.389 | 8 de 12 (1 = el más difícil) |
| 461-480 | 479 | -0.30 | 0.250 | 18 | 11 | -0.27 | 0.389 | 9 de 12 (1 = el más difícil) |
| 481-500 | 499 | -0.54 | 0.225 | 19 | 11 | -0.20 | 0.398 | 11 de 12 (1 = el más difícil) |
| 501-520 | 519 | -0.35 | 0.275 | 18 | 11 | -0.33 | 0.364 | 7 de 12 (1 = el más difícil) |
| 521-540 | 539 | -0.40 | 0.300 | 18 | 11 | -0.21 | 0.361 | 10 de 12 (1 = el más difícil) |
| 541-560 | 559 | -0.36 | 0.350 | 17 | 11 | -0.16 | 0.389 | 11 de 12 (1 = el más difícil) |
| 561-580 | 579 | -0.35 | 0.300 | 19 | 11 | -0.32 | 0.382 | 6 de 12 (1 = el más difícil) |
| 581-600 | 599 | -0.37 | 0.200 | 21 | 11 | -0.28 | 0.334 | 10 de 12 (1 = el más difícil) |
| 601-620 | 619 | -0.26 | 0.200 | 18 | 11 | -0.23 | 0.393 | 6 de 12 (1 = el más difícil) |
| 621-640 | 639 | -0.38 | 0.200 | 18 | 11 | -0.05 | 0.350 | 12 de 12 (1 = el más difícil) |
| 641-660 | 659 | -0.25 | 0.325 | 17 | 11 | -0.22 | 0.380 | 5 de 12 (1 = el más difícil) |
| 661-680 | 679 | -0.37 | 0.325 | 16 | 11 | -0.32 | 0.405 | 8 de 12 (1 = el más difícil) |
| 681-700 | 699 | -0.30 | 0.375 | 16 | 11 | -0.12 | 0.398 | 9 de 12 (1 = el más difícil) |

## Banda medida por rol (03 §7: «la banda coloca»)

La colocación exige, con el cuantil q de D en el pool del rango (recuperación ≤ 0,10, comodidad ≤ 0,70, reto ≤ 0,95, hito > 0,95): comodidad, estreno y remanso como mucho en banda de comodidad; recuperación (p1-p2, c.3 regla 3) en banda de recuperación o, como mínimo, con D por debajo de la comodidad más fácil del ciclo; hito (p20, «Pico» de c.3) en banda de hito o, como mínimo, con par − 2K ≥ la media del pool del rango; y en 21-40 los retos suaves por debajo del hito 40. level-qa lo comprueba regenerando la campaña (QA-BAND-Q y QA-LADDER).

| Ciclo | Recuperación p1 / p2: q (D) | Comodidad más fácil (D) | q máximo de comodidad y estreno | Remanso: q | Retos: D mín.-máx. | Hito: q (banda) · par − 2K / media del pool |
|---|---|---|---|---|---|---|
| 301-320 | 0.038 (-0.78) / 0.020 (-0.82) | -0.54 | 0.664 | 0.195 | 0.41 / 0.87 | 0.935 (reto) · 3 / 2.5 |
| 321-340 | 0.033 (-0.80) / 0.013 (-0.91) | -0.49 | 0.614 | 0.293 | 0.62 / 1.14 | 0.974 (hito) · 5 / 2.5 |
| 341-360 | 0.062 (-0.69) / 0.026 (-0.81) | -0.60 | 0.572 | 0.284 | 0.57 / 1.02 | 0.984 (hito) · 5 / 2.5 |
| 361-380 | 0.075 (-0.65) / 0.054 (-0.74) | -0.30 | 0.635 | 0.318 | 0.25 / 0.76 | 0.975 (hito) · 13 / 2.5 |
| 381-400 | 0.067 (-0.68) / 0.007 (-0.96) | -0.36 | 0.588 | 0.391 | 0.52 / 1.55 | 0.993 (hito) · 12 / 2.5 |
| 401-420 | 0.055 (-0.69) / 0.063 (-0.65) | -0.58 | 0.557 | 0.265 | 0.07 / 0.62 | 1.000 (hito) · 15 / 2.7 |
| 421-440 | 0.052 (-0.70) / 0.022 (-0.82) | -0.58 | 0.663 | 0.247 | 0.45 / 1.01 | 0.980 (hito) · 6 / 2.7 |
| 441-460 | 0.057 (-0.68) / 0.284 (-0.33) | -0.30 | 0.641 | 0.371 | 0.34 / 1.18 | 0.978 (hito) · 12 / 2.7 |
| 461-480 | 0.060 (-0.67) / 0.059 (-0.67) | -0.61 | 0.541 | 0.321 | 0.30 / 0.61 | 0.967 (hito) · 8 / 2.7 |
| 481-500 | 0.049 (-0.72) / 0.023 (-0.81) | -0.63 | 0.567 | 0.114 | 0.37 / 0.99 | 0.957 (hito) · 7 / 2.7 |
| 501-520 | 0.015 (-0.85) / 0.042 (-0.74) | -0.86 | 0.611 | 0.269 | 0.36 / 0.71 | 0.990 (hito) · 11 / 2.7 |
| 521-540 | 0.088 (-0.59) / 0.092 (-0.58) | -0.61 | 0.672 | 0.223 | 0.45 / 1.15 | 0.969 (hito) · 7 / 2.7 |
| 541-560 | 0.013 (-0.87) / 0.041 (-0.74) | -0.49 | 0.600 | 0.254 | 0.24 / 0.77 | 0.976 (hito) · 6 / 2.7 |
| 561-580 | 0.051 (-0.71) / 0.015 (-0.86) | -0.74 | 0.660 | 0.260 | 0.33 / 0.54 | 0.969 (hito) · 6 / 2.7 |
| 581-600 | 0.026 (-0.80) / 0.068 (-0.64) | -0.69 | 0.668 | 0.246 | 0.32 / 1.05 | 0.965 (hito) · 7 / 2.7 |
| 601-620 | 0.085 (-0.60) / 0.081 (-0.60) | -0.56 | 0.662 | 0.356 | 0.27 / 1.14 | 0.978 (hito) · 11 / 2.7 |
| 621-640 | 0.022 (-0.83) / 0.080 (-0.60) | -0.36 | 0.653 | 0.236 | 0.28 / 0.68 | 0.967 (hito) · 10 / 2.7 |
| 641-660 | 0.065 (-0.64) / 0.067 (-0.64) | -0.42 | 0.622 | 0.366 | 0.55 / 1.30 | 0.985 (hito) · 14 / 2.7 |
| 661-680 | 0.066 (-0.64) / 0.073 (-0.61) | -0.72 | 0.646 | 0.245 | 0.05 / 1.04 | 0.952 (hito) · 7 / 2.7 |
| 681-700 | 0.004 (-0.94) / 0.007 (-0.92) | -0.79 | 0.698 | 0.319 | 0.11 / 0.69 | 0.966 (hito) · 8 / 2.7 |

Fuera de la banda de su rol, con el mínimo cumplido (aviso QA-BAND-Q): 320 (hito, q 0.935); 442 (recuperacion, q 0.284).

## Vacíos de la comodidad y del remanso por rango (03 §4.4 y §11)

Comodidades sin estreno y remansos. «2 vacíos» = dos o más vacíos libres (sin mecánica, capacidad ≥ 4); «Cuia chica» = Cuia chica de 2 añadida como medio vacío por la relajación de 03 §4.4. Canon: 2 vacíos en 11-30; 1 desde el 31 (03 §11: «2 en respiros y recuperación» en 31-60, «2 en 30 % de comodidad» en 61-100 y 1 en 101-200; 03 §4.4: «1 (31+) con 2 en el 30 % de los slots») [P: se revisa con el tech-lead].

| Rango | Comodidades y remansos | Con 2 vacíos | Con Cuia chica de relajación | Relajados (%) | Canon |
|---|---|---|---|---|---|
| 201-400 | 47 | 18 | 4 | 47 % | 1 |
| 401-700 | 177 | 53 | 11 | 36 % | 1 |

## Cuias por rango frente a 03 §11

Con 1 vacío, el K del rango que permiten los pares de riesgo (03 §4.5; `k_plan`) no siempre llega al mínimo de cuias de 03 §11 (aviso QA-CUIAS de level-qa) [P: k_plan, vacíos o la tabla se revisan en la integración].

| Rango | 03 §11 | Mínimo y máximo generados | Por debajo | Por encima | Niveles por debajo |
|---|---|---|---|---|---|
| 201-400 | 11-13 | 9-13 | 68 | 0 | 301 (10), 302 (10), 303 (10), 304 (10), 305 (10), 307 (9), 308 (10), 310 (10), 311 (10), 313 (9), 314 (10), 315 (10), 316 (10), 317 (10), 319 (10), 321 (10), 322 (10), 323 (10), 324 (10), 325 (10), 327 (9), 328 (10), 330 (9), 331 (10), 333 (10), 334 (10), 336 (10), 337 (10), 339 (10), 341 (10), 342 (10), 343 (10), 344 (10), 345 (10), 347 (10), 348 (9), 350 (10), 351 (10), 353 (9), 354 (9), 356 (10), 357 (10), 359 (10), 361 (10), 362 (10), 363 (10), 364 (10), 365 (10), 367 (10), 368 (10), 370 (10), 373 (10), 376 (9), 377 (10), 379 (10), 381 (10), 382 (10), 383 (10), 384 (10), 385 (10), 387 (10), 388 (10), 390 (10), 391 (10), 394 (10), 396 (10), 397 (10), 399 (10) |
| 401-700 | 12-14 | 9-14 | 273 | 0 | 401 (10), 402 (10), 403 (10), 404 (10), 405 (10), 406 (11), 407 (10), 408 (10), 409 (11), 410 (9), 411 (9), 413 (10), 414 (10), 415 (10), 416 (10), 417 (10), 418 (11), 419 (10), 421 (10), 422 (10), 423 (10), 424 (9), 425 (11), 426 (11), 427 (10), 428 (10), 429 (11), 430 (10), 431 (10), 433 (10), 434 (10), 435 (11), 436 (9), 437 (11), 438 (11), 439 (10), 441 (10), 442 (10), 443 (10), 444 (11), 445 (10), 446 (11), 447 (10), 448 (10), 449 (11), 450 (10), 451 (10), 453 (10), 454 (11), 455 (11), 456 (10), 457 (10), 458 (11), 459 (10), 461 (10), 462 (10), 463 (10), 464 (10), 465 (10), 466 (11), 467 (9), 468 (10), 469 (11), 470 (9), 471 (10), 473 (10), 474 (10), 475 (11), 476 (9), 477 (10), 479 (10), 481 (10), 482 (10), 483 (10), 484 (10), 485 (11), 486 (11), 487 (10), 488 (10), 489 (11), 490 (10), 491 (10), 492 (10), 493 (9), 494 (9), 495 (11), 496 (10), 497 (11), 498 (11), 499 (10), 501 (10), 502 (10), 503 (10), 504 (9), 505 (10), 507 (10), 508 (10), 509 (11), 510 (10), 511 (9), 512 (11), 513 (10), 514 (10), 515 (11), 516 (10), 517 (11), 518 (11), 519 (10), 521 (10), 522 (10), 523 (10), 524 (10), 525 (10), 526 (11), 527 (9), 528 (10), 529 (11), 530 (9), 531 (10), 532 (11), 533 (11), 534 (10), 535 (11), 536 (10), 537 (11), 538 (11), 539 (10), 541 (10), 542 (10), 543 (9), 544 (10), 545 (10), 546 (11), 547 (10), 548 (11), 550 (10), 551 (10), 552 (10), 553 (10), 554 (11), 555 (11), 556 (10), 557 (10), 558 (11), 559 (10), 561 (10), 562 (10), 563 (10), 564 (10), 565 (11), 567 (9), 568 (10), 569 (11), 570 (11), 571 (10), 572 (11), 573 (10), 574 (9), 575 (11), 576 (9), 577 (10), 578 (11), 579 (10), 581 (10), 582 (10), 583 (10), 584 (10), 585 (10), 586 (11), 587 (9), 588 (10), 589 (11), 590 (10), 591 (11), 593 (10), 594 (11), 595 (11), 596 (10), 597 (10), 598 (11), 599 (10), 601 (10), 602 (10), 603 (9), 604 (10), 605 (10), 606 (10), 607 (9), 608 (10), 609 (11), 610 (9), 611 (10), 612 (11), 613 (11), 614 (10), 615 (11), 616 (10), 617 (11), 619 (10), 621 (10), 622 (10), 623 (10), 624 (10), 625 (10), 626 (11), 627 (10), 628 (11), 629 (11), 630 (10), 631 (10), 632 (11), 633 (10), 634 (10), 635 (11), 636 (10), 637 (11), 639 (10), 641 (10), 642 (10), 643 (10), 644 (11), 645 (10), 647 (10), 648 (11), 649 (11), 650 (10), 651 (9), 652 (11), 653 (10), 654 (10), 655 (11), 656 (9), 657 (10), 658 (11), 659 (10), 661 (10), 662 (10), 663 (11), 664 (10), 665 (10), 666 (10), 667 (9), 668 (10), 669 (11), 670 (10), 671 (10), 672 (11), 673 (9), 674 (10), 675 (11), 676 (9), 677 (11), 678 (11), 679 (10), 681 (10), 682 (10), 683 (10), 684 (10), 685 (10), 686 (11), 687 (10), 688 (11), 689 (11), 690 (10), 691 (9), 692 (11), 693 (9), 694 (11), 695 (11), 696 (10), 697 (10), 698 (11), 699 (10) |

## Saltos de banda (03 §12: +2,2 / −3,4)

Fuera del límite (blando, registrado): 359→360: sube 2.38 bandas (> 2.2); 399→400: sube 2.38 bandas (> 2.2); 419→420: sube 2.72 bandas (> 2.2); 420→421: baja 3.47 bandas (> 3.4); 439→440: sube 2.36 bandas (> 2.2); 519→520: sube 2.52 bandas (> 2.2); 559→560: sube 2.26 bandas (> 2.2); 659→660: sube 2.26 bandas (> 2.2).

## Variantes de la 1.0

Variantes de 02 §9.1 (propuesta de D-007 b.3; firma de `pack_schema` 3, D-023 punto 5) con los códigos de d.5.1 [P]: Ln = Liana de cuenta (`liana` con `n`; n = 1 en el estreno y 1-2 en los demás [P]), Ha = Hoja atada (`hoja_atada`, sobre una cuia con su fruta en el fondo y cap = U_c, D-024), B2 = Brote de dos hojas (`brote` con `n` = 2, 1-2 dosis) y Fl = Flor de vitória-régia (`flor` con `key` y `key2` distintas). Estreno (02 §9.2 regla 2): un ejemplar, 2 vacíos y `bot_fail` < 0,20.

| Variante | Debut | Niveles | Ejemplares | Tableros de estreno | Estrenos con un ejemplar, 2 vacíos y bot < 0,20 |
|---|---|---|---|---|---|
| Ln | 243 (`liana_n`) | 14 | 14 | 0 | 0 de 0 |
| Ha | 283 (`hoja_atada`) | 33 | 33 | 0 | 0 de 0 |
| B2 | 343 (`brote_n2`) | 28 | 28 | 3 | 3 de 3 |
| Fl | 403 (`flor`) | 19 | 19 | 3 | 3 de 3 |

| Nivel | p | Rol | Celda | Variante | Ejemplares de mecánica | Vacíos | bot_fail |
|---|---|---|---|---|---|---|---|
| 306 | 6 | reto | Ha+Ll (D) | hoja_atada key=13 (3 dosis) | 2 | 2 | 0.800 |
| 318 | 18 | reto | Ag+Ln (D) | liana n=1 (4 dosis) | 3 | 2 | 0.800 |
| 320 | 20 | hito | Ha+Ll+Br (MD) | hoja_atada key=4 (4 dosis) | 3 | 2 | 0.925 |
| 343 | 3 | estreno | *B2 | brote n=2 (1 dosis) | 1 | 2 | 0.000 |
| 344 | 4 | estreno | *B2 | brote n=2 (2 dosis) | 1 | 2 | 0.000 |
| 345 | 5 | estreno | *B2 | brote n=2 (1 dosis) | 1 | 2 | 0.000 |
| 346 | 6 | reto | B2+Ln (D) | brote n=2 (1 dosis); liana n=1 (4 dosis) | 2 | 1 | 0.775 |
| 349 | 9 | reto | B2 (D) | brote n=2 (2 dosis) | 1 | 1 | 0.800 |
| 352 | 12 | reto | B2+Re (D) | brote n=2 (1 dosis) | 2 | 1 | 0.575 |
| 355 | 15 | reto | B2+Re (D) | brote n=2 (1 dosis) | 2 | 1 | 0.875 |
| 358 | 18 | reto | Ag+Ln (D) | liana n=2 (4 dosis) | 3 | 2 | 0.825 |
| 360 | 20 | hito | B2+Ln+Ag (MD) | liana n=1 (3 dosis); brote n=2 (2 dosis) | 4 | 1 | 1.000 |
| 372 | 12 | reto | La+B2 (D) | brote n=2 (1 dosis) | 2 | 1 | 0.850 |
| 389 | 9 | reto | Li+Ha (D) | hoja_atada key=2 (4 dosis) | 2 | 2 | 0.850 |
| 398 | 18 | reto | Ha+Ba (D) | hoja_atada key=14 (4 dosis) | 2 | 1 | 0.875 |
| 400 | 20 | hito | Li+Ha+Ag (MD) | hoja_atada key=6 (4 dosis) | 4 | 2 | 1.000 |
| 403 | 3 | estreno | *Fl | flor key=1 key2=11 (4 dosis) | 1 | 2 | 0.000 |
| 404 | 4 | estreno | *Fl | flor key=14 key2=1 (4 dosis) | 1 | 2 | 0.100 |
| 405 | 5 | estreno | *Fl | flor key=1 key2=3 (4 dosis) | 1 | 2 | 0.150 |
| 407 | 7 | comodidad | Fl | flor key=9 key2=3 (4 dosis) | 1 | 2 | 0.475 |
| 409 | 9 | reto | Fl+Ll (D) | flor key=13 key2=10 (3 dosis) | 2 | 2 | 0.750 |
| 412 | 12 | reto | Fl+La (D) | flor key=11 key2=3 (2 dosis) | 2 | 1 | 0.775 |
| 415 | 15 | reto | Fl (D) | flor key=6 key2=4 (4 dosis) | 1 | 1 | 0.850 |
| 420 | 20 | hito | Fl+Ba+A2 (MD) | flor key=6 key2=13 (4 dosis) | 7 | 1 | 1.000 |
| 426 | 6 | reto | Fl+Ma (D) | flor key=9 key2=13 (4 dosis) | 2 | 2 | 0.775 |
| 431 | 11 | comodidad | Ag+B2 | brote n=2 (1 dosis) | 3 | 1 | 0.575 |
| 432 | 12 | reto | Fl+Ho (D) | flor key=5 key2=6 (4 dosis) | 2 | 3 | 0.750 |
| 437 | 17 | comodidad | La+Ha | hoja_atada key=14 (3 dosis) | 2 | 1 | 0.450 |
| 457 | 17 | comodidad | Ag+Ha | hoja_atada key=12 (4 dosis) | 3 | 1 | 0.525 |
| 465 | 5 | comodidad | B2 | brote n=2 (1 dosis) | 1 | 1 | 0.225 |
| 466 | 6 | reto | Li+B2 (D) | brote n=2 (1 dosis) | 2 | 1 | 0.875 |
| 469 | 9 | reto | Ln+B2 (D) | liana n=2 (3 dosis); brote n=2 (2 dosis) | 2 | 1 | 0.825 |
| 478 | 18 | reto | La+Ln (D) | liana n=1 (4 dosis) | 2 | 1 | 0.850 |
| 480 | 20 | hito | Li+B2+Ho (MD) | brote n=2 (2 dosis) | 3 | 1 | 1.000 |
| 486 | 6 | reto | Ma+Ha (D) | hoja_atada key=5 (4 dosis) | 2 | 2 | 0.775 |
| 492 | 12 | reto | Ag+Ha+Ba (D) | hoja_atada key=10 (4 dosis) | 4 | 0 | 0.850 |
| 497 | 17 | comodidad | La+B2 | brote n=2 (2 dosis) | 2 | 1 | 0.300 |
| 498 | 18 | reto | Fl+Ha (D) | hoja_atada key=5 (4 dosis); flor key=12 key2=4 (4 dosis) | 2 | 2 | 0.850 |
| 500 | 20 | hito | Re+Ha+A2 (MD) | hoja_atada key=10 (4 dosis) | 7 | 2 | 0.950 |
| 509 | 9 | reto | Fl (D) | flor key=13 key2=5 (4 dosis) | 1 | 2 | 0.725 |
| 512 | 12 | reto | Ll+Ha (D) | hoja_atada key=2 (3 dosis) | 2 | 2 | 0.875 |
| 517 | 17 | comodidad | La+B2 | brote n=2 (2 dosis) | 2 | 1 | 0.350 |
| 518 | 18 | reto | Fl+Ba (D) | flor key=10 key2=13 (4 dosis) | 2 | 1 | 0.800 |
| 520 | 20 | hito | Li+Ha+A2 (MD) | hoja_atada key=5 (4 dosis) | 6 | 2 | 1.000 |
| 526 | 6 | reto | Pi+Ha (D) | hoja_atada key=12 (4 dosis) | 2 | 1 | 0.850 |
| 537 | 17 | comodidad | La+B2 | brote n=2 (2 dosis) | 2 | 1 | 0.475 |
| 538 | 18 | reto | Ha+Re (D) | hoja_atada key=3 (4 dosis) | 2 | 2 | 0.800 |
| 540 | 20 | hito | Ln+Ha+A2 (MD) | liana n=1 (4 dosis); hoja_atada key=1 (4 dosis) | 6 | 2 | 0.975 |
| 543 | 3 | comodidad | Fl | flor key=10 key2=12 (4 dosis) | 1 | 1 | 0.475 |
| 546 | 6 | reto | Fl+Ho (D) | flor key=2 key2=6 (4 dosis) | 2 | 2 | 0.800 |
| 549 | 9 | reto | La+Ln (D) | liana n=2 (1 dosis) | 2 | 1 | 0.725 |
| 552 | 12 | reto | Ln+Re (D) | liana n=1 (4 dosis) | 2 | 1 | 0.875 |
| 558 | 18 | reto | Fl (D) | flor key=9 key2=7 (4 dosis) | 1 | 2 | 0.825 |
| 560 | 20 | hito | Fl+Re+A2 (MD) | flor key=5 key2=3 (4 dosis) | 6 | 2 | 0.925 |
| 565 | 5 | comodidad | La+B2 | brote n=2 (1 dosis) | 2 | 1 | 0.350 |
| 568 | 8 | comodidad | Ag+Ha | hoja_atada key=11 (4 dosis) | 3 | 1 | 0.500 |
| 569 | 9 | reto | B2+Ba (D) | brote n=2 (2 dosis) | 2 | 1 | 0.750 |
| 578 | 18 | reto | B2+Re (D) | brote n=2 (1 dosis) | 2 | 1 | 0.775 |
| 580 | 20 | hito | Ba+B2+Ag (MD) | brote n=2 (1 dosis) | 4 | 1 | 1.000 |
| 585 | 5 | comodidad | Ag+Ha | hoja_atada key=14 (4 dosis) | 4 | 1 | 0.400 |
| 586 | 6 | reto | Pi+Ha (D) | hoja_atada key=5 (4 dosis) | 2 | 1 | 0.825 |
| 589 | 9 | reto | Ha+B2 (D) | brote n=2 (1 dosis); hoja_atada key=4 (3 dosis) | 2 | 1 | 0.875 |
| 592 | 12 | reto | Pi+B2+Ag (D) | brote n=2 (2 dosis) | 5 | 1 | 0.775 |
| 600 | 20 | hito | Ha+Ba+A2 (MD) | hoja_atada key=10 (4 dosis) | 6 | 1 | 1.000 |
| 603 | 3 | comodidad | Ln | liana n=2 (4 dosis) | 1 | 1 | 0.475 |
| 606 | 6 | reto | Ln+Re (D) | liana n=1 (4 dosis) | 2 | 1 | 0.800 |
| 617 | 17 | comodidad | La+Fl | flor key=8 key2=5 (4 dosis) | 2 | 1 | 0.300 |
| 618 | 18 | reto | Ln+Ll (D) | liana n=2 (4 dosis) | 2 | 3 | 0.850 |
| 620 | 20 | hito | Ln+Ll+Ag (MD) | liana n=2 (3 dosis) | 4 | 2 | 1.000 |
| 628 | 8 | comodidad | La+B2 | brote n=2 (1 dosis) | 2 | 1 | 0.250 |
| 637 | 17 | comodidad | La+Ha | hoja_atada key=8 (4 dosis) | 2 | 1 | 0.325 |
| 646 | 6 | reto | Pi+Br+Ha (D) | hoja_atada key=2 (4 dosis) | 3 | 1 | 0.800 |
| 649 | 9 | reto | Pi+Ha (D) | hoja_atada key=4 (4 dosis) | 2 | 1 | 0.825 |
| 657 | 17 | comodidad | Ag+B2 | brote n=2 (1 dosis) | 3 | 1 | 0.400 |
| 658 | 18 | reto | Ag+Ha+Re (D) | hoja_atada key=4 (4 dosis) | 5 | 2 | 0.850 |
| 660 | 20 | hito | Ln+Ha+A2 (MD) | hoja_atada key=12 (4 dosis); liana n=1 (4 dosis) | 6 | 2 | 0.975 |
| 665 | 5 | comodidad | Ag+Ha | hoja_atada key=14 (4 dosis) | 3 | 1 | 0.500 |
| 666 | 6 | reto | Li+Ha (D) | hoja_atada key=4 (4 dosis) | 2 | 1 | 0.875 |
| 677 | 17 | comodidad | La+B2 | brote n=2 (2 dosis) | 2 | 1 | 0.200 |
| 678 | 18 | reto | Ha+Re (D) | hoja_atada key=8 (4 dosis) | 2 | 2 | 0.875 |
| 680 | 20 | hito | Li+Ha+A2 (MD) | hoja_atada key=9 (4 dosis) | 7 | 2 | 1.000 |
| 689 | 9 | reto | Ho+B2 (D) | brote n=2 (1 dosis) | 2 | 1 | 0.800 |
| 692 | 12 | reto | Ma+Ha (D) | hoja_atada key=9 (4 dosis) | 2 | 2 | 0.800 |
| 698 | 18 | reto | Ag+Ha+B2 (D) | brote n=2 (2 dosis); hoja_atada key=4 (3 dosis) | 4 | 1 | 0.875 |
| 700 | 20 | hito | Fl+Ha+A2 (MD) | flor key=10 key2=5 (4 dosis); hoja_atada key=10 (4 dosis) | 7 | 2 | 1.000 |

Todas las celdas con una variante la llevan en el nivel publicado.
