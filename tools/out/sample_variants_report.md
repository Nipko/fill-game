# Campaña 241-320: informe de `level-generator --spec`

Generado por `dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/sample_variants.json --levels 241-320 --out tools/out/sample_variants --report tools/out/sample_variants_report.md`: no se edita a mano y no lleva tiempos. Generador 1.0.0, `pack_schema` 3, modo de vertido `run` (D-005).

Fuentes: D-009 (ciclo escalonado) y D-007 (debuts) con la propuesta `docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md` (c.1-c.4, d.1-d.4); GDD 03 §2, §4, §7-§13 y §17; 02 §8-§11; 08 §4.1. Todo se mide con `AF.Core` y `AF.Solver`: A* w = 1 a 300.000 (par), bot ×40 con la semilla del nivel (`bot_fail` y etiqueta), `EarlyTraps` (trampas tempranas, Q16 B), ruta de referencia para ramificación y trampas (`--parity`), `SafePath` en Aguas turbias y `LevelValidation` en cada tablero.

## Paquetes

| Pack | Niveles | Bytes | Bytes sin relief | Bytes sin relief por 100 niveles (≤ 100 KB, 03 §17 #11) | SHA-256 |
|---|---|---|---|---|---|
| `tools/out/sample_variants/mundo_06_v1.json` | 60 | 78213 | 49103 | 79.9 KB | `a46ff4d372b82a19dbb42613a13d4b97dc44c01014b638330380711eaedea931` |
| `tools/out/sample_variants/mundo_07_v1.json` | 20 | 26261 | 16710 | 81.6 KB | `fb8faf487ac9cbca7e6a1d590368f9d8d120343194415aa88a7dd61d37bb29d2` |

## Pool por rango e índice D (03 §7)

D = 0,35·z(bot_fail) + 0,25·z(par − 2K) + 0,20·z(ramificación) + 0,10·z(trampas) + 0,10·[E libres = 1], con media y desviación del pool del rango (todos los candidatos en ventana de sus slots; congelados tras la primera pasada). Banda = cuantil de D en el pool: recuperación ≤ 0,10, comodidad ≤ 0,70, reto ≤ 0,95, hito > 0,95.

| Rango | Slots | Pool | Pool / slots | bot_fail μ ± σ | par − 2K μ ± σ | Ramif. μ ± σ | Trampas μ ± σ |
|---|---|---|---|---|---|---|---|
| 241-320 | 80 | 350 | 4.4 | 0.46 ± 0.30 | 3.5 ± 3.8 | 5.29 ± 1.64 | 0.04 ± 0.08 |

## Etiquetas por rango frente a 03 §11

| Rango (niveles generados) | Difícil | Muy difícil | 03 §11 | Aviso (> 5 puntos) |
|---|---|---|---|---|
| 241-320 (80) | 19 (23.8 %) | 4 (5.0 %) | 25-25 % / 5 % | — |

Nota: con D-009 desde el 61 los cinco retos apuntan a Difícil y el hito a Muy difícil (25 % y 5 % por ciclo), por encima del 15 % de 03 §11 para 61-100: es la cuenta del ciclo, no un error de la generación [P: la tabla de 03 §11 se revisa en la integración].

## Ciclos

| Ciclo | Temporada | Puros | Difícil | Muy difícil | Objetivo D / MD | Acierto de etiqueta (c.7) | Trampas tempranas | Relajaciones |
|---|---|---|---|---|---|---|---|---|
| 241-260 | A | 12/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 4 | 14 |
| 261-280 | B | 12/20 | 4 | 1 | 5 / 1 | 5/6 (83 %) | 7 | 13 |
| 281-300 | B | 12/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 0 | 10 |
| 301-320 | A | 12/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 4 | 12 |

## Niveles

Columnas: p = posición en el ciclo (0 en la rampa); E = vacíos de capacidad ≥ 4 (con la Hoja o el Matapi vacíos), libres = sin mecánica; q = cuantil de D en el pool del rango (3 decimales: la banda de hito empieza por encima de 0,95) y su banda; TT = trampas tempranas; alivio = tipos publicados en `relief` en orden (− = minus_color, + = plus_empty).

| Nivel | p | Rol | Calendario | K | E (libres) | Cuias | Fuente | Par | Estados A* | bot_fail | Ventana | Etiqueta | Objetivo | D | q (banda) | Ramif. | Trampas | TT | Compl. | Alivio | Notas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 241 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1482 | 0.075 | [0.00, 0.30) | normal | — | -0.85 | 0.017 (recuperacion) | 4.38 | 0.00 | 0 | 42 | −4(14; 0.00) +(0.00) | 1 ronda(s) de banda |
| 242 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1435 | 0.100 | [0.00, 0.30) | normal | — | -0.77 | 0.030 (recuperacion) | 4.75 | 0.00 | 0 | 44 | −4(15; 0.00) +(0.00) |  |
| 243 | 3 | estreno | *Ln | 9 | 2 (2) | 11 | walk | 22 | 9301 | 0.175 | [0.00, 0.20) | normal | — | -0.21 | 0.396 (comodidad) | 6.50 | 0.00 | 0 | 56 | −7(21; 0.00) +(0.08) |  |
| 244 | 4 | estreno | *Ln | 9 | 2 (2) | 11 | walk | 20 | 2787 | 0.000 | [0.00, 0.20) | normal | — | -0.52 | 0.153 (comodidad) | 6.70 | 0.00 | 0 | 54 | −6(19; 0.00) +(0.00) |  |
| 245 | 5 | estreno | *Ln | 9 | 2 (2) | 11 | walk | 19 | 1171 | 0.000 | [0.00, 0.20) | normal | — | -0.64 | 0.076 (recuperacion) | 5.95 | 0.03 | 0 | 52 | −4(18; 0.00) +(0.00) |  |
| 246 | 6 | reto | Ln (D) | 9 | 2 (2) | 11 | fill | 26 | 7283 | 0.875 | [0.75, 0.90) | hard | hard | 0.53 | 0.781 (reto) | 3.46 | 0.02 | 0 | 64 | −3(22; 0.00) +(0.10) | relajacion: fill con 2 vacios (03 sec. 4.4) |
| 247 | 7 | comodidad | C | 9 | 2 (2) | 11 | walk | 18 | 3000 | 0.500 | [0.20, 0.60] | normal | — | -0.19 | 0.410 (comodidad) | 4.67 | 0.10 | 0 | 48 | −1(16; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 248 | 8 | comodidad | C | 9 | 2 (2) | 11 | walk | 21 | 6769 | 0.400 | [0.20, 0.60] | normal | — | -0.03 | 0.559 (comodidad) | 6.33 | 0.00 | 0 | 54 | −8(20; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 249 | 9 | reto | C (D) | 9 | 2 (2) | 11 | fill | 29 | 6838 | 0.800 | [0.75, 0.90) | hard | hard | 0.62 | 0.824 (reto) | 3.59 | 0.00 | 0 | 72 | −1(25; 0.00) +(0.00) | relajacion: fill con 2 vacios (03 sec. 4.4) |
| 250 | 10 | comodidad | C | 9 | 2 (2) | 11 | walk | 19 | 3648 | 0.275 | [0.20, 0.60] | normal | — | -0.12 | 0.479 (comodidad) | 7.84 | 0.00 | 0 | 52 | −3(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 251 | 11 | comodidad | Ln | 9 | 2 (2) | 11 | walk | 18 | 993 | 0.550 | [0.20, 0.60] | normal | — | -0.16 | 0.433 (comodidad) | 4.33 | 0.10 | 0 | 50 | −5(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4) |
| 252 | 12 | reto | Ln (D) | 9 | 2 (2) | 11 | fill | 28 | 6403 | 0.850 | [0.75, 0.90) | hard | hard | 0.70 | 0.876 (reto) | 4.04 | 0.02 | 1 | 68 | −1(25; 0.05) +(0.35) | relajacion: fill con 2 vacios (03 sec. 4.4) |
| 253 | 13 | comodidad | C | 9 | 2 (2) | 11 | walk | 19 | 6628 | 0.200 | [0.20, 0.60] | normal | — | -0.30 | 0.299 (comodidad) | 7.16 | 0.00 | 0 | 50 | −3(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 254 | 14 | comodidad | C | 9 | 2 (2) | 11 | walk | 19 | 1983 | 0.575 | [0.20, 0.60] | normal | — | 0.16 | 0.653 (comodidad) | 6.32 | 0.09 | 0 | 52 | −1(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 255 | 15 | reto | C (D) | 9 | 2 (2) | 11 | fill | 28 | 5919 | 0.850 | [0.75, 0.90) | hard | hard | 0.71 | 0.879 (reto) | 3.68 | 0.06 | 0 | 68 | −9(24; 0.00) +(0.15) | relajacion: fill con 2 vacios (03 sec. 4.4) |
| 256 | 16 | comodidad | C | 9 | 2 (2) | 11 | walk | 22 | 8944 | 0.375 | [0.20, 0.60] | normal | — | 0.05 | 0.613 (comodidad) | 6.41 | 0.03 | 0 | 56 | −13(21; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 257 | 17 | comodidad | C | 9 | 2 (2) | 11 | walk | 20 | 4646 | 0.450 | [0.20, 0.60] | normal | — | 0.21 | 0.676 (comodidad) | 8.35 | 0.00 | 0 | 54 | −1(19; 0.00) +(0.13) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 258 | 18 | reto | Ln (D) | 9 | 2 (2) | 11 | fill | 26 | 1063 | 0.850 | [0.75, 0.90) | hard | hard | 0.78 | 0.919 (reto) | 4.04 | 0.18 | 0 | 64 | −6(24; 0.25) +(0.13) | relajacion: fill con 2 vacios (03 sec. 4.4) |
| 259 | 19 | remanso | S | 9 | 2 (2) | 11 | walk | 19 | 1834 | 0.400 | [0.20, 0.40] | normal | — | -0.21 | 0.390 (comodidad) | 5.26 | 0.06 | 0 | 52 | −3(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 260 | 20 | hito | Ln (MD) | 9 | 1 (1) | 10 | fill | 30 | 106 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.27 | 0.996 (hito) | 2.00 | 0.33 | 3 | 72 | +(0.73) −10(26; 0.55) |  |
| 261 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1525 | 0.050 | [0.00, 0.30) | normal | — | -0.63 | 0.079 (recuperacion) | 6.38 | 0.00 | 0 | 44 | −8(15; 0.00) +(0.00) |  |
| 262 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1490 | 0.000 | [0.00, 0.30) | normal | — | -0.71 | 0.056 (recuperacion) | 6.19 | 0.00 | 0 | 44 | −2(14; 0.00) +(0.00) |  |
| 263 | 3 | estreno | *Ha | 9 | 2 (2) | 11 | walk | 19 | 2475 | 0.075 | [0.00, 0.20) | normal | — | -0.37 | 0.221 (comodidad) | 7.74 | 0.00 | 0 | 52 | −14(18; 0.00) +(0.00) |  |
| 264 | 4 | estreno | *Ha | 9 | 2 (2) | 11 | walk | 19 | 3292 | 0.175 | [0.00, 0.20) | normal | — | -0.55 | 0.136 (comodidad) | 5.32 | 0.00 | 0 | 50 | −1(17; 0.00) +(0.00) |  |
| 265 | 5 | estreno | *Ha | 9 | 2 (2) | 11 | walk | 20 | 3979 | 0.075 | [0.00, 0.20) | normal | — | -0.50 | 0.167 (comodidad) | 6.15 | 0.00 | 0 | 52 | −4(19; 0.00) +(0.03) |  |
| 266 | 6 | reto | Ha+Ln (D) | 9 | 2 (2) | 11 | fill | 26 | 4291 | 0.825 | [0.75, 0.90) | hard | hard | 0.47 | 0.741 (reto) | 3.54 | 0.01 | 1 | 62 | −3(24; 0.05) +(0.03) | relajacion: fill con 2 vacios (03 sec. 4.4) |
| 267 | 7 | comodidad | C | 9 | 2 (2) | 11 | walk | 21 | 6777 | 0.550 | [0.20, 0.60] | normal | — | -0.15 | 0.444 (comodidad) | 3.90 | 0.00 | 0 | 54 | −8(20; 0.05) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 268 | 8 | comodidad | C | 9 | 2 (2) | 11 | walk | 20 | 7910 | 0.450 | [0.20, 0.60] | normal | — | -0.07 | 0.524 (comodidad) | 6.05 | 0.00 | 0 | 52 | −2(19; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 269 | 9 | reto | C (D) | 9 | 1 (1) | 10 | deep | 21 | 267 | 0.875 | [0.75, 0.90) | hard | hard | 0.56 | 0.794 (reto) | 2.48 | 0.31 | 0 | 54 | +(0.40) −7(20; 0.38) | 1 ronda(s) extra de candidatos |
| 270 | 10 | comodidad | C | 9 | 2 (2) | 11 | walk | 21 | 9638 | 0.500 | [0.20, 0.60] | normal | — | -0.02 | 0.573 (comodidad) | 4.67 | 0.07 | 0 | 54 | −9(19; 0.05) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 271 | 11 | comodidad | Ha | 9 | 1 (1) | 11 | walk | 18 | 3912 | 0.425 | [0.20, 0.60] | normal | — | -0.23 | 0.367 (comodidad) | 4.72 | 0.05 | 0 | 46 | +(0.00) −4(16; 0.03) | relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4) |
| 272 | 12 | reto | Ha (D) | 9 | 2 (2) | 11 | fill | 27 | 3516 | 0.800 | [0.75, 0.90) | hard | hard | 0.59 | 0.810 (reto) | 3.59 | 0.07 | 1 | 68 | −2(25; 0.03) +(0.10) | relajacion: fill con 2 vacios (03 sec. 4.4) |
| 273 | 13 | comodidad | C | 9 | 2 (2) | 11 | walk | 18 | 2024 | 0.475 | [0.20, 0.60] | normal | — | -0.31 | 0.287 (comodidad) | 4.22 | 0.07 | 0 | 50 | −1(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 274 | 14 | comodidad | C | 9 | 2 (2) | 11 | walk | 20 | 8250 | 0.400 | [0.20, 0.60] | normal | — | -0.33 | 0.270 (comodidad) | 4.40 | 0.00 | 0 | 52 | −10(19; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 275 | 15 | reto | C (D) | 9 | 2 (2) | 11 | fill | 27 | 4690 | 0.675 | [0.75, 0.90) | normal | hard | 0.68 | 0.866 (reto) | 6.11 | 0.02 | 0 | 68 | −9(24; 0.00) +(0.03) | relajacion: fill con 2 vacios (03 sec. 4.4); fuera de ventana (relajacion agotada) |
| 276 | 16 | comodidad | C | 9 | 2 (2) | 11 | walk | 19 | 5662 | 0.350 | [0.20, 0.60] | normal | — | -0.33 | 0.276 (comodidad) | 5.47 | 0.00 | 0 | 50 | −6(18; 0.03) +(0.05) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 277 | 17 | comodidad | C | 9 | 2 (2) | 11 | walk | 20 | 4418 | 0.300 | [0.20, 0.60] | normal | — | -0.33 | 0.264 (comodidad) | 5.35 | 0.00 | 0 | 52 | −3(19; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 278 | 18 | reto | Ha (D) | 9 | 2 (2) | 11 | fill | 28 | 4694 | 0.775 | [0.75, 0.90) | hard | hard | 0.78 | 0.924 (reto) | 4.18 | 0.14 | 1 | 68 | −7(25; 0.00) +(0.18) | relajacion: fill con 2 vacios (03 sec. 4.4) |
| 279 | 19 | remanso | S | 9 | 2 (2) | 11 | walk | 23 | 14138 | 0.375 | [0.20, 0.40] | normal | — | -0.25 | 0.359 (comodidad) | 3.74 | 0.00 | 0 | 58 | −3(21; 0.08) +(0.05) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 280 | 20 | hito | Ha (MD) | 9 | 1 (1) | 10 | fill | 28 | 230 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 1.00 | 0.979 (hito) | 2.36 | 0.20 | 4 | 70 | +(0.68) −3(25; 0.53) |  |
| 281 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 17 | 2216 | 0.025 | [0.00, 0.30) | normal | — | -0.61 | 0.093 (recuperacion) | 6.29 | 0.00 | 0 | 46 | −3(16; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 282 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 1521 | 0.075 | [0.00, 0.30) | normal | — | -0.74 | 0.041 (recuperacion) | 5.25 | 0.00 | 0 | 44 | −2(15; 0.00) +(0.00) | 1 ronda(s) extra de candidatos |
| 283 | 3 | estreno | *B2 | 9 | 2 (2) | 12 | walk | 18 | 3376 | 0.000 | [0.00, 0.20) | normal | — | -0.57 | 0.116 (comodidad) | 7.39 | 0.00 | 0 | 54 | −3(17; 0.00) +(0.00) |  |
| 284 | 4 | estreno | *B2 | 9 | 2 (2) | 12 | walk | 18 | 999 | 0.000 | [0.00, 0.20) | normal | — | -0.46 | 0.187 (comodidad) | 8.28 | 0.00 | 0 | 57 | −4(17; 0.00) +(0.00) |  |
| 285 | 5 | estreno | *B2 | 9 | 2 (2) | 12 | walk | 19 | 1307 | 0.000 | [0.00, 0.20) | normal | — | -0.55 | 0.131 (comodidad) | 7.00 | 0.00 | 0 | 57 | −1(18; 0.00) +(0.00) |  |
| 286 | 6 | reto | B2+Ha (D) | 9 | 1 (1) | 11 | deep | 22 | 2140 | 0.750 | [0.75, 0.90) | hard | hard | 0.52 | 0.770 (reto) | 4.27 | 0.17 | 0 | 61 | +(0.05) −3(21; 0.03) | 1 ronda(s) extra de candidatos |
| 287 | 7 | comodidad | C | 9 | 2 (2) | 11 | walk | 18 | 1752 | 0.350 | [0.20, 0.60] | normal | — | -0.26 | 0.336 (comodidad) | 6.56 | 0.00 | 0 | 50 | −9(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 288 | 8 | comodidad | C | 9 | 2 (2) | 11 | walk | 19 | 4416 | 0.350 | [0.20, 0.60] | normal | — | -0.16 | 0.436 (comodidad) | 6.32 | 0.05 | 0 | 50 | −5(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 289 | 9 | reto | C (D) | 9 | 2 (2) | 11 | fill | 29 | 11659 | 0.775 | [0.75, 0.90) | hard | hard | 0.54 | 0.787 (reto) | 3.10 | 0.00 | 0 | 68 | −11(25; 0.05) +(0.03) | relajacion: fill con 2 vacios (03 sec. 4.4); 1 ronda(s) extra de candidatos |
| 290 | 10 | comodidad | C | 9 | 2 (2) | 11 | walk | 18 | 5334 | 0.200 | [0.20, 0.60] | normal | — | -0.38 | 0.219 (comodidad) | 7.00 | 0.00 | 0 | 48 | −4(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 291 | 11 | comodidad | B2 | 9 | 1 (1) | 11 | walk | 19 | 1113 | 0.250 | [0.20, 0.60] | normal | — | -0.12 | 0.476 (comodidad) | 7.26 | 0.00 | 0 | 57 | +(0.00) −1(18; 0.00) |  |
| 292 | 12 | reto | B2 (D) | 9 | 1 (1) | 11 | deep | 22 | 7951 | 0.825 | [0.75, 0.90) | hard | hard | 0.68 | 0.861 (reto) | 6.59 | 0.01 | 0 | 58 | +(0.00) −4(20; 0.00) | 1 ronda(s) extra de candidatos |
| 293 | 13 | comodidad | C | 9 | 1 (1) | 10 | walk | 18 | 212 | 0.450 | [0.20, 0.60] | normal | — | -0.15 | 0.450 (comodidad) | 3.28 | 0.22 | 0 | 48 | +(0.08) −14(17; 0.13) | 1 ronda(s) extra de candidatos |
| 294 | 14 | comodidad | C | 9 | 2 (2) | 11 | walk | 21 | 3713 | 0.550 | [0.20, 0.60] | normal | — | -0.11 | 0.490 (comodidad) | 3.95 | 0.02 | 0 | 52 | −3(19; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 295 | 15 | reto | C (D) | 9 | 2 (2) | 11 | fill | 30 | 11540 | 0.825 | [0.75, 0.90) | hard | hard | 0.70 | 0.877 (reto) | 3.37 | 0.01 | 0 | 72 | −3(25; 0.00) +(0.08) | relajacion: fill con 2 vacios (03 sec. 4.4); 1 ronda(s) extra de candidatos |
| 296 | 16 | comodidad | C | 9 | 2 (2) | 11 | walk | 21 | 6251 | 0.375 | [0.20, 0.60] | normal | — | 0.03 | 0.604 (comodidad) | 7.05 | 0.00 | 0 | 56 | −4(20; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 297 | 17 | comodidad | C | 9 | 2 (2) | 11 | walk | 18 | 2765 | 0.400 | [0.20, 0.60] | normal | — | -0.36 | 0.224 (comodidad) | 5.22 | 0.00 | 0 | 50 | −1(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 298 | 18 | reto | B2 (D) | 9 | 1 (1) | 11 | deep | 24 | 16311 | 0.825 | [0.75, 0.90) | hard | hard | 0.71 | 0.881 (reto) | 5.42 | 0.04 | 0 | 63 | +(0.00) −1(23; 0.05) | 1 ronda(s) extra de candidatos |
| 299 | 19 | remanso | S | 9 | 2 (2) | 11 | walk | 19 | 3286 | 0.300 | [0.20, 0.40] | normal | — | -0.52 | 0.156 (comodidad) | 4.37 | 0.00 | 0 | 52 | −7(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); 1 ronda(s) extra de candidatos |
| 300 | 20 | hito | B2 (MD) | 9 | 1 (1) | 11 | deep | 22 | 6203 | 0.950 | [0.90, 1.00] | very_hard | very_hard | 0.77 | 0.914 (reto) | 4.82 | 0.13 | 0 | 59 | +(0.08) −3(20; 0.05) | relajacion: deep con 60.000 nodos; 1 ronda(s) extra de candidatos; 6 ronda(s) de banda |
| 301 | 1 | recuperacion | R | 8 | 2 (2) | 10 | walk | 16 | 2321 | 0.000 | [0.00, 0.30) | normal | — | -0.66 | 0.066 (recuperacion) | 6.63 | 0.00 | 0 | 42 | −10(14; 0.00) +(0.00) |  |
| 302 | 2 | recuperacion | R | 8 | 2 (2) | 10 | walk | 17 | 1949 | 0.050 | [0.00, 0.30) | normal | — | -0.66 | 0.070 (recuperacion) | 5.65 | 0.00 | 0 | 46 | −2(16; 0.00) +(0.00) |  |
| 303 | 3 | estreno | *Fl | 9 | 2 (2) | 11 | walk | 20 | 1077 | 0.100 | [0.00, 0.20) | normal | — | -0.75 | 0.039 (recuperacion) | 3.75 | 0.01 | 0 | 52 | −4(18; 0.00) +(0.00) |  |
| 304 | 4 | estreno | *Fl | 9 | 2 (2) | 11 | walk | 18 | 1718 | 0.000 | [0.00, 0.20) | normal | — | -0.86 | 0.013 (recuperacion) | 5.00 | 0.00 | 0 | 48 | −7(16; 0.00) +(0.00) |  |
| 305 | 5 | estreno | *Fl | 9 | 2 (2) | 11 | walk | 18 | 3124 | 0.000 | [0.00, 0.20) | normal | — | -0.72 | 0.053 (recuperacion) | 6.17 | 0.00 | 0 | 46 | −4(16; 0.00) +(0.00) |  |
| 306 | 6 | reto | Fl+B2 (D) | 9 | 1 (1) | 11 | deep | 22 | 601 | 0.750 | [0.75, 0.90) | hard | hard | 0.26 | 0.696 (comodidad) | 3.77 | 0.02 | 0 | 58 | +(0.00) −3(20; 0.00) |  |
| 307 | 7 | comodidad | C | 9 | 1 (1) | 10 | walk | 18 | 442 | 0.250 | [0.20, 0.60] | normal | — | -0.80 | 0.024 (recuperacion) | 2.28 | 0.00 | 0 | 48 | +(0.00) −3(17; 0.00) |  |
| 308 | 8 | comodidad | C | 9 | 2 (2) | 11 | walk | 19 | 4920 | 0.250 | [0.20, 0.60] | normal | — | -0.26 | 0.327 (comodidad) | 6.95 | 0.00 | 0 | 52 | −1(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 309 | 9 | reto | C (D) | 9 | 2 (2) | 11 | fill | 26 | 16293 | 0.800 | [0.75, 0.90) | hard | hard | 0.51 | 0.767 (reto) | 3.85 | 0.04 | 1 | 64 | −1(23; 0.00) +(0.05) | relajacion: fill con 2 vacios (03 sec. 4.4) |
| 310 | 10 | comodidad | C | 9 | 2 (2) | 11 | walk | 18 | 2308 | 0.250 | [0.20, 0.60] | normal | — | -0.36 | 0.227 (comodidad) | 6.67 | 0.00 | 0 | 50 | −6(17; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 311 | 11 | comodidad | Fl | 9 | 2 (2) | 11 | walk | 19 | 1113 | 0.575 | [0.20, 0.60] | normal | — | 0.22 | 0.684 (comodidad) | 7.16 | 0.06 | 0 | 52 | −1(17; 0.00) +(0.08) | relajacion: 2 vacios (03 sec. 4.4) |
| 312 | 12 | reto | Fl (D) | 9 | 2 (2) | 11 | fill | 25 | 1260 | 0.800 | [0.75, 0.90) | hard | hard | 0.56 | 0.794 (reto) | 4.12 | 0.10 | 0 | 60 | −14(22; 0.00) +(0.00) | relajacion: fill con 2 vacios (03 sec. 4.4); 1 ronda(s) extra de candidatos |
| 313 | 13 | comodidad | C | 9 | 2 (2) | 11 | walk | 19 | 2551 | 0.425 | [0.20, 0.60] | normal | — | -0.04 | 0.547 (comodidad) | 7.05 | 0.00 | 0 | 52 | −3(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 314 | 14 | comodidad | C | 9 | 2 (2) | 11 | walk | 21 | 4986 | 0.450 | [0.20, 0.60] | normal | — | 0.14 | 0.641 (comodidad) | 7.29 | 0.00 | 0 | 56 | −14(20; 0.23) +(0.20) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 315 | 15 | reto | C (D) | 9 | 2 (2) | 11 | fill | 29 | 10956 | 0.825 | [0.75, 0.90) | hard | hard | 0.63 | 0.836 (reto) | 3.41 | 0.00 | 0 | 70 | −1(25; 0.08) +(0.10) | relajacion: fill con 2 vacios (03 sec. 4.4) |
| 316 | 16 | comodidad | C | 9 | 2 (2) | 11 | walk | 19 | 3032 | 0.225 | [0.20, 0.60] | normal | — | -0.32 | 0.281 (comodidad) | 6.74 | 0.00 | 0 | 52 | −4(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 317 | 17 | comodidad | C | 9 | 2 (2) | 11 | walk | 19 | 3615 | 0.350 | [0.20, 0.60] | normal | — | -0.08 | 0.519 (comodidad) | 7.47 | 0.00 | 0 | 52 | −2(18; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 318 | 18 | reto | Fl (D) | 9 | 2 (2) | 11 | fill | 28 | 1689 | 0.850 | [0.75, 0.90) | hard | hard | 0.80 | 0.933 (reto) | 3.75 | 0.12 | 1 | 68 | −2(24; 0.03) +(0.08) | relajacion: fill con 2 vacios (03 sec. 4.4) |
| 319 | 19 | remanso | S | 9 | 2 (2) | 11 | walk | 20 | 4797 | 0.325 | [0.20, 0.40] | normal | — | -0.19 | 0.404 (comodidad) | 6.25 | 0.00 | 0 | 52 | −8(19; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 320 | 20 | hito | Fl (MD) | 9 | 1 (1) | 10 | fill | 24 | 165 | 1.000 | [0.90, 1.00] | very_hard | very_hard | 0.93 | 0.967 (hito) | 2.38 | 0.33 | 2 | 62 | −11(20; 0.63) | alivio plus_empty no publicado (no baja bot_fail 0,20) |

## Par / 2K por fuente frente a 03 §18

| Fuente | Niveles | Par / 2K medio | bot_fail medio | 03 §18 (par / 2K) |
|---|---|---|---|---|
| deep | 6 | 1.23 | 0.83 | 1,17 |
| fill | 18 | 1.52 | 0.84 | 1,53 |
| walk | 56 | 1.07 | 0.27 | 1,09 |

## Debuts

| Nivel | intro_mechanic | skippable | Celda |
|---|---|---|---|
| 243 | `liana_n` | false | *Ln |
| 263 | `hoja_atada` | false | *Ha |
| 283 | `brote_n2` | false | *B2 |
| 303 | `flor` | false | *Fl |

## Apariciones en los packs (debut real y huecos)

Sobre los tableros publicados, no sobre el calendario: primera aparición (debut real), niveles en los que sale, hueco máximo entre dos apariciones y niveles desde la última hasta el 320 (resumen, punto 6; d.2 regla 4: ≤ 120; level-qa lo exige con QA-GAP-120). Las capacidades cuentan también cuando el generador las usa como forma de tablero (02 §8); la cueva de la Piraña no cuenta como Cuia chica.

| Mecánica o capacidad | Debut del calendario | Debut real | Niveles | Hueco máximo | Hasta el final |
|---|---|---|---|---|---|
| liana_n (Ln) | 243 | 243 | 9 | 6 (252→258) | 54 |
| hoja_atada (Ha) | 263 | 263 | 9 | 6 (272→278) | 34 |
| brote_n2 (B2) | 283 | 283 | 9 | 6 (292→298) | 14 |
| flor (Fl) | 303 | 303 | 8 | 6 (312→318) | 0 |
| Cuia chica (cap 1-3) | — | 271 | 1 | — | 49 |
| Cuia alta (cap 5-6) | — | **no sale** | 0 | — | — |

## Remanso frente a la comodidad del ciclo (c.4)

p19 es «puro, más fácil que la media de comodidad» (c.4; c.3 regla 5: respiro antes del pico): la colocación exige D(p19) < media de D de las comodidades del ciclo que no son tableros de estreno, y level-qa lo comprueba (QA-REMANSO).

| Ciclo | Remanso | D | bot_fail | Par | Comodidades | D medio | bot_fail medio | Puesto del remanso por D (de las comodidades y él) |
|---|---|---|---|---|---|---|---|---|
| 241-260 | 259 | -0.21 | 0.400 | 19 | 8 | -0.05 | 0.416 | 8 de 9 (1 = el más difícil) |
| 261-280 | 279 | -0.25 | 0.375 | 23 | 8 | -0.22 | 0.431 | 5 de 9 (1 = el más difícil) |
| 281-300 | 299 | -0.52 | 0.300 | 19 | 8 | -0.19 | 0.366 | 9 de 9 (1 = el más difícil) |
| 301-320 | 319 | -0.19 | 0.325 | 20 | 8 | -0.19 | 0.347 | 5 de 9 (1 = el más difícil) |

## Banda medida por rol (03 §7: «la banda coloca»)

La colocación exige, con el cuantil q de D en el pool del rango (recuperación ≤ 0,10, comodidad ≤ 0,70, reto ≤ 0,95, hito > 0,95): comodidad, estreno y remanso como mucho en banda de comodidad; recuperación (p1-p2, c.3 regla 3) en banda de recuperación o, como mínimo, con D por debajo de la comodidad más fácil del ciclo; hito (p20, «Pico» de c.3) en banda de hito o, como mínimo, con par − 2K ≥ la media del pool del rango; y en 21-40 los retos suaves por debajo del hito 40. level-qa lo comprueba regenerando la campaña (QA-BAND-Q y QA-LADDER).

| Ciclo | Recuperación p1 / p2: q (D) | Comodidad más fácil (D) | q máximo de comodidad y estreno | Remanso: q | Retos: D mín.-máx. | Hito: q (banda) · par − 2K / media del pool |
|---|---|---|---|---|---|---|
| 241-260 | 0.017 (-0.85) / 0.030 (-0.77) | -0.30 | 0.676 | 0.390 | 0.53 / 0.78 | 0.996 (hito) · 12 / 3.5 |
| 261-280 | 0.079 (-0.63) / 0.056 (-0.71) | -0.33 | 0.573 | 0.359 | 0.47 / 0.78 | 0.979 (hito) · 10 / 3.5 |
| 281-300 | 0.093 (-0.61) / 0.041 (-0.74) | -0.38 | 0.604 | 0.156 | 0.52 / 0.71 | 0.914 (reto) · 4 / 3.5 |
| 301-320 | 0.066 (-0.66) / 0.070 (-0.66) | -0.80 | 0.684 | 0.404 | 0.26 / 0.80 | 0.967 (hito) · 6 / 3.5 |

Fuera de la banda de su rol, con el mínimo cumplido (aviso QA-BAND-Q): 300 (hito, q 0.914).

## Vacíos de la comodidad y del remanso por rango (03 §4.4 y §11)

Comodidades sin estreno y remansos. «2 vacíos» = dos o más vacíos libres (sin mecánica, capacidad ≥ 4); «Cuia chica» = Cuia chica de 2 añadida como medio vacío por la relajación de 03 §4.4. Canon: 2 vacíos en 11-30; 1 desde el 31 (03 §11: «2 en respiros y recuperación» en 31-60, «2 en 30 % de comodidad» en 61-100 y 1 en 101-200; 03 §4.4: «1 (31+) con 2 en el 30 % de los slots») [P: se revisa con el tech-lead].

| Rango | Comodidades y remansos | Con 2 vacíos | Con Cuia chica de relajación | Relajados (%) | Canon |
|---|---|---|---|---|---|
| 241-320 | 36 | 32 | 1 | 92 % | 1 |

## Cuias por rango frente a 03 §11

Con 1 vacío, el K del rango que permiten los pares de riesgo (03 §4.5; `k_plan`) no siempre llega al mínimo de cuias de 03 §11 (aviso QA-CUIAS de level-qa) [P: k_plan, vacíos o la tabla se revisan en la integración].

| Rango | 03 §11 | Mínimo y máximo generados | Por debajo | Por encima | Niveles por debajo |
|---|---|---|---|---|---|
| 241-320 | 10-13 | 10-12 | 0 | 0 | — |

## Saltos de banda (03 §12: +2,2 / −3,4)

Fuera del límite (blando, registrado): 259→260: sube 2.43 bandas (> 2.2).

## Variantes de la 1.0

Variantes de 02 §9.1 (propuesta de D-007 b.3; firma de `pack_schema` 3, D-023 punto 5) con los códigos de d.5.1 [P]: Ln = Liana de cuenta (`liana` con `n`; n = 1 en el estreno y 1-2 en los demás [P]), Ha = Hoja atada (`hoja_atada`, sobre una cuia con su fruta en el fondo y cap = U_c, D-024), B2 = Brote de dos hojas (`brote` con `n` = 2, 1-2 dosis) y Fl = Flor de vitória-régia (`flor` con `key` y `key2` distintas). Estreno (02 §9.2 regla 2): un ejemplar, 2 vacíos y `bot_fail` < 0,20.

| Variante | Debut | Niveles | Ejemplares | Tableros de estreno | Estrenos con un ejemplar, 2 vacíos y bot < 0,20 |
|---|---|---|---|---|---|
| Ln | 243 (`liana_n`) | 9 | 9 | 3 | 3 de 3 |
| Ha | 263 (`hoja_atada`) | 9 | 9 | 3 | 3 de 3 |
| B2 | 283 (`brote_n2`) | 9 | 9 | 3 | 3 de 3 |
| Fl | 303 (`flor`) | 8 | 8 | 3 | 3 de 3 |

| Nivel | p | Rol | Celda | Variante | Ejemplares de mecánica | Vacíos | bot_fail |
|---|---|---|---|---|---|---|---|
| 243 | 3 | estreno | *Ln | liana n=1 (4 dosis) | 1 | 2 | 0.175 |
| 244 | 4 | estreno | *Ln | liana n=1 (4 dosis) | 1 | 2 | 0.000 |
| 245 | 5 | estreno | *Ln | liana n=1 (4 dosis) | 1 | 2 | 0.000 |
| 246 | 6 | reto | Ln (D) | liana n=2 (4 dosis) | 1 | 2 | 0.875 |
| 251 | 11 | comodidad | Ln | liana n=2 (4 dosis) | 1 | 2 | 0.550 |
| 252 | 12 | reto | Ln (D) | liana n=1 (4 dosis) | 1 | 2 | 0.850 |
| 258 | 18 | reto | Ln (D) | liana n=1 (4 dosis) | 1 | 2 | 0.850 |
| 260 | 20 | hito | Ln (MD) | liana n=1 (4 dosis) | 1 | 1 | 1.000 |
| 263 | 3 | estreno | *Ha | hoja_atada key=11 (4 dosis) | 1 | 2 | 0.075 |
| 264 | 4 | estreno | *Ha | hoja_atada key=5 (4 dosis) | 1 | 2 | 0.175 |
| 265 | 5 | estreno | *Ha | hoja_atada key=2 (4 dosis) | 1 | 2 | 0.075 |
| 266 | 6 | reto | Ha+Ln (D) | hoja_atada key=6 (4 dosis); liana n=2 (4 dosis) | 2 | 2 | 0.825 |
| 271 | 11 | comodidad | Ha | hoja_atada key=2 (4 dosis) | 1 | 1 | 0.425 |
| 272 | 12 | reto | Ha (D) | hoja_atada key=11 (4 dosis) | 1 | 2 | 0.800 |
| 278 | 18 | reto | Ha (D) | hoja_atada key=9 (4 dosis) | 1 | 2 | 0.775 |
| 280 | 20 | hito | Ha (MD) | hoja_atada key=5 (4 dosis) | 1 | 1 | 1.000 |
| 283 | 3 | estreno | *B2 | brote n=2 (1 dosis) | 1 | 2 | 0.000 |
| 284 | 4 | estreno | *B2 | brote n=2 (2 dosis) | 1 | 2 | 0.000 |
| 285 | 5 | estreno | *B2 | brote n=2 (2 dosis) | 1 | 2 | 0.000 |
| 286 | 6 | reto | B2+Ha (D) | hoja_atada key=14 (4 dosis); brote n=2 (2 dosis) | 2 | 1 | 0.750 |
| 291 | 11 | comodidad | B2 | brote n=2 (2 dosis) | 1 | 1 | 0.250 |
| 292 | 12 | reto | B2 (D) | brote n=2 (1 dosis) | 1 | 1 | 0.825 |
| 298 | 18 | reto | B2 (D) | brote n=2 (2 dosis) | 1 | 1 | 0.825 |
| 300 | 20 | hito | B2 (MD) | brote n=2 (2 dosis) | 1 | 1 | 0.950 |
| 303 | 3 | estreno | *Fl | flor key=13 key2=5 (4 dosis) | 1 | 2 | 0.100 |
| 304 | 4 | estreno | *Fl | flor key=3 key2=13 (4 dosis) | 1 | 2 | 0.000 |
| 305 | 5 | estreno | *Fl | flor key=2 key2=9 (4 dosis) | 1 | 2 | 0.000 |
| 306 | 6 | reto | Fl+B2 (D) | flor key=2 key2=14 (3 dosis); brote n=2 (1 dosis) | 2 | 1 | 0.750 |
| 311 | 11 | comodidad | Fl | flor key=9 key2=6 (4 dosis) | 1 | 2 | 0.575 |
| 312 | 12 | reto | Fl (D) | flor key=10 key2=2 (4 dosis) | 1 | 2 | 0.800 |
| 318 | 18 | reto | Fl (D) | flor key=13 key2=5 (4 dosis) | 1 | 2 | 0.850 |
| 320 | 20 | hito | Fl (MD) | flor key=13 key2=7 (4 dosis) | 1 | 1 | 1.000 |

Todas las celdas con una variante la llevan en el nivel publicado.
