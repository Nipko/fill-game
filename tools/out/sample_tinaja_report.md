# Campaña 321-340: informe de `level-generator --spec`

Generado por `dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/sample_tinaja.json --levels 321-340 --out tools/out/sample_tinaja --report tools/out/sample_tinaja_report.md`: no se edita a mano y no lleva tiempos. Generador 1.0.0, `pack_schema` 3, modo de vertido `run` (D-005).

Fuentes: D-009 (ciclo escalonado) y D-007 (debuts) con la propuesta `docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md` (c.1-c.4, d.1-d.4); GDD 03 §2, §4, §7-§13 y §17; 02 §8-§11; 08 §4.1. Todo se mide con `AF.Core` y `AF.Solver`: A* w = 1 a 300.000 (par), bot ×40 con la semilla del nivel (`bot_fail` y etiqueta), `EarlyTraps` (trampas tempranas, Q16 B), ruta de referencia para ramificación y trampas (`--parity`), `SafePath` en Aguas turbias y `LevelValidation` en cada tablero.

Tinaja (M13; D-019, Q-Ti1 A; 03 §8; propuesta de la tinaja c.3): paso «Ti» de la superposición, búsquedas sin poda de fútiles (`SolverOptions.ForLevel`), y en cada candidato que se mide `SafePath`, paseos seguros y casuales, «S0 sin trampas» en el estreno y la compuerta de coste de Morpho (`TinajaWalks`); ver la sección «Tinaja».

## Paquetes

| Pack | Niveles | Bytes | Bytes sin relief | Bytes sin relief por 100 niveles (≤ 100 KB, 03 §17 #11) | SHA-256 |
|---|---|---|---|---|---|
| `tools/out/sample_tinaja/mundo_07_v1.json` | 20 | 25077 | 15894 | 77.6 KB | `1deee76bd8901dcb0eb81e92b21a64e21c9921ff7ae7efc675b4af17e7416666` |

## Pool por rango e índice D (03 §7)

D = 0,35·z(bot_fail) + 0,25·z(par − 2K) + 0,20·z(ramificación) + 0,10·z(trampas) + 0,10·[E libres = 1], con media y desviación del pool del rango (todos los candidatos en ventana de sus slots; congelados tras la primera pasada). Banda = cuantil de D en el pool: recuperación ≤ 0,10, comodidad ≤ 0,70, reto ≤ 0,95, hito > 0,95.

| Rango | Slots | Pool | Pool / slots | bot_fail μ ± σ | par − 2K μ ± σ | Ramif. μ ± σ | Trampas μ ± σ |
|---|---|---|---|---|---|---|---|
| 321-340 | 20 | 88 | 4.4 | 0.46 ± 0.30 | 4.4 ± 3.8 | 4.15 ± 1.30 | 0.04 ± 0.07 |

## Etiquetas por rango frente a 03 §11

| Rango (niveles generados) | Difícil | Muy difícil | 03 §11 | Aviso (> 5 puntos) |
|---|---|---|---|---|
| 321-340 (20) | 5 (25.0 %) | 1 (5.0 %) | 25-25 % / 5 % | — |

Nota: con D-009 desde el 61 los cinco retos apuntan a Difícil y el hito a Muy difícil (25 % y 5 % por ciclo), por encima del 15 % de 03 §11 para 61-100: es la cuenta del ciclo, no un error de la generación [P: la tabla de 03 §11 se revisa en la integración].

## Ciclos

| Ciclo | Temporada | Puros | Difícil | Muy difícil | Objetivo D / MD | Acierto de etiqueta (c.7) | Trampas tempranas | Relajaciones |
|---|---|---|---|---|---|---|---|---|
| 321-340 | B | 12/20 | 5 | 1 | 5 / 1 | 6/6 (100 %) | 1 | 5 |

## Niveles

Columnas: p = posición en el ciclo (0 en la rampa); E = vacíos de capacidad ≥ 4 (con la Hoja o el Matapi vacíos), libres = sin mecánica; q = cuantil de D en el pool del rango (3 decimales: la banda de hito empieza por encima de 0,95) y su banda; TT = trampas tempranas; alivio = tipos publicados en `relief` en orden (− = minus_color, + = plus_empty).

| Nivel | p | Rol | Calendario | K | E (libres) | Cuias | Fuente | Par | Estados A* | bot_fail | Ventana | Etiqueta | Objetivo | D | q (banda) | Ramif. | Trampas | TT | Compl. | Alivio | Notas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 321 | 1 | recuperacion | R | 7 | 2 (2) | 9 | walk | 14 | 752 | 0.000 | [0.00, 0.30) | normal | — | -0.73 | 0.063 (recuperacion) | 5.00 | 0.00 | 0 | 38 | −2(13; 0.00) +(0.00) |  |
| 322 | 2 | recuperacion | R | 7 | 2 (2) | 9 | walk | 14 | 562 | 0.000 | [0.00, 0.30) | normal | — | -0.69 | 0.097 (recuperacion) | 5.29 | 0.00 | 0 | 38 | −3(13; 0.00) +(0.00) |  |
| 323 | 3 | estreno | *Ti | 6 | 2 (2) | 10 | walk | 16 | 5097 | 0.000 | [0.00, 0.20) | normal | — | -0.46 | 0.131 (comodidad) | 5.13 | 0.00 | 0 | 42 | −6(15; 0.00) +(0.00) | tinaja: K <= 6 en el estreno (propuesta de la tinaja c.3 y d.3); tinaja: 3 dosis (2 ocultas), 1 realidades |
| 324 | 4 | estreno | *Ti | 6 | 2 (2) | 10 | walk | 13 | 1704 | 0.000 | [0.00, 0.20) | normal | — | -0.79 | 0.040 (recuperacion) | 4.23 | 0.00 | 0 | 36 | −7(12; 0.00) +(0.00) | tinaja: K <= 6 en el estreno (propuesta de la tinaja c.3 y d.3); tinaja: 3 dosis (2 ocultas), 1 realidades |
| 325 | 5 | estreno | *Ti | 6 | 2 (2) | 10 | walk | 14 | 1682 | 0.000 | [0.00, 0.20) | normal | — | -0.80 | 0.028 (recuperacion) | 3.71 | 0.00 | 0 | 29 | −6(13; 0.00) +(0.00) | tinaja: K <= 6 en el estreno (propuesta de la tinaja c.3 y d.3); tinaja: 3 dosis (2 ocultas), 2 realidades |
| 326 | 6 | reto | C (D) | 8 | 2 (2) | 10 | fill | 22 | 2788 | 0.800 | [0.75, 0.90) | hard | hard | 0.49 | 0.733 (reto) | 4.41 | 0.00 | 0 | 56 | −2(20; 0.00) +(0.00) | relajacion: fill con 2 vacios (03 sec. 4.4) |
| 327 | 7 | comodidad | Ti | 7 | 1 (1) | 10 | walk | 15 | 876 | 0.425 | [0.20, 0.60] | normal | — | -0.23 | 0.358 (comodidad) | 3.67 | 0.04 | 0 | 39 | +(0.00) −7(14; 0.00) | tinaja: 3 dosis (2 ocultas), 1 realidades |
| 328 | 8 | comodidad | C | 7 | 1 (1) | 8 | walk | 15 | 90 | 0.275 | [0.20, 0.60] | normal | — | -0.21 | 0.369 (comodidad) | 3.07 | 0.24 | 0 | 40 | +(0.00) −1(14; 0.00) |  |
| 329 | 9 | reto | Ti (D) | 8 | 1 (1) | 11 | deep | 23 | 4297 | 0.800 | [0.75, 0.90) | hard | hard | 0.50 | 0.744 (reto) | 2.96 | 0.04 | 0 | 47 | +(0.00) −3(22; 0.00) | tinaja: 5 dosis (4 ocultas), 24 realidades |
| 330 | 10 | comodidad | C | 7 | 2 (2) | 9 | walk | 16 | 1887 | 0.425 | [0.20, 0.60] | normal | — | 0.10 | 0.631 (comodidad) | 6.00 | 0.04 | 0 | 40 | −3(14; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 331 | 11 | comodidad | C | 7 | 1 (1) | 8 | walk | 16 | 156 | 0.400 | [0.20, 0.60] | normal | — | -0.37 | 0.210 (comodidad) | 2.38 | 0.05 | 0 | 42 | +(0.03) −6(15; 0.00) |  |
| 332 | 12 | reto | Ti (D) | 7 | 1 (1) | 10 | deep | 22 | 4408 | 0.825 | [0.75, 0.90) | hard | hard | 0.58 | 0.761 (reto) | 3.32 | 0.00 | 0 | 48 | +(0.00) −10(21; 0.00) | relajacion: K - 1 (tinaja: vacios fijos); 1 ronda(s) extra de candidatos; tinaja: 5 dosis (4 ocultas), 18 realidades |
| 333 | 13 | comodidad | C | 7 | 2 (2) | 9 | walk | 18 | 2586 | 0.200 | [0.20, 0.60] | normal | — | -0.10 | 0.517 (comodidad) | 5.78 | 0.02 | 0 | 44 | −3(16; 0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica); alivio plus_empty no publicado (no baja bot_fail 0,20) |
| 334 | 14 | comodidad | C | 7 | 1 (1) | 8 | walk | 15 | 174 | 0.450 | [0.20, 0.60] | normal | — | -0.51 | 0.119 (comodidad) | 2.00 | 0.00 | 0 | 40 | +(0.00) −7(14; 0.05) |  |
| 335 | 15 | reto | C (D) | 8 | 1 (1) | 9 | fill | 23 | 72 | 0.875 | [0.75, 0.90) | hard | hard | 0.58 | 0.767 (reto) | 1.83 | 0.17 | 1 | 60 | +(0.43) −8(21; 0.00) |  |
| 336 | 16 | comodidad | C | 7 | 1 (1) | 8 | walk | 15 | 122 | 0.550 | [0.20, 0.60] | normal | — | 0.29 | 0.688 (comodidad) | 2.53 | 0.42 | 0 | 40 | +(0.03) −4(14; 0.30) |  |
| 337 | 17 | comodidad | C | 7 | 1 (1) | 8 | walk | 15 | 62 | 0.550 | [0.20, 0.60] | normal | — | -0.24 | 0.347 (comodidad) | 2.13 | 0.09 | 0 | 42 | +(0.25) −14(13; 0.23) |  |
| 338 | 18 | reto | Ti (D) | 8 | 1 (1) | 11 | fill | 27 | 23610 | 0.825 | [0.75, 0.90) | hard | hard | 0.95 | 0.949 (reto) | 4.44 | 0.00 | 0 | 67 | +(0.03) −2(22; 0.00) | tinaja: 4 dosis (3 ocultas), 1 realidades |
| 339 | 19 | remanso | S | 7 | 2 (2) | 9 | walk | 16 | 1359 | 0.300 | [0.20, 0.40] | normal | — | -0.28 | 0.301 (comodidad) | 4.88 | 0.00 | 0 | 42 | −6(14; 0.00) +(0.00) | relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica) |
| 340 | 20 | hito | Ti (MD) | 8 | 1 (1) | 11 | fill | 29 | 11483 | 0.950 | [0.90, 1.00] | very_hard | very_hard | 1.08 | 0.972 (hito) | 3.07 | 0.04 | 0 | 64 | +(0.05) −8(25; 0.00) | tinaja: 5 dosis (4 ocultas), 18 realidades |

## Par / 2K por fuente frente a 03 §18

| Fuente | Niveles | Par / 2K medio | bot_fail medio | 03 §18 (par / 2K) |
|---|---|---|---|---|
| deep | 2 | 1.50 | 0.81 | 1,17 |
| fill | 4 | 1.58 | 0.86 | 1,53 |
| walk | 14 | 1.12 | 0.26 | 1,09 |

## Debuts

| Nivel | intro_mechanic | skippable | Celda |
|---|---|---|---|
| 323 | `tinaja` | false | *Ti |

## Apariciones en los packs (debut real y huecos)

Sobre los tableros publicados, no sobre el calendario: primera aparición (debut real), niveles en los que sale, hueco máximo entre dos apariciones y niveles desde la última hasta el 340 (resumen, punto 6; d.2 regla 4: ≤ 120; level-qa lo exige con QA-GAP-120). Las capacidades cuentan también cuando el generador las usa como forma de tablero (02 §8); la cueva de la Piraña no cuenta como Cuia chica.

| Mecánica o capacidad | Debut del calendario | Debut real | Niveles | Hueco máximo | Hasta el final |
|---|---|---|---|---|---|
| tinaja (Ti) | 323 | 323 | 8 | 6 (332→338) | 0 |
| Cuia chica (cap 1-3) | — | **no sale** | 0 | — | — |
| Cuia alta (cap 5-6) | — | **no sale** | 0 | — | — |

## Remanso frente a la comodidad del ciclo (c.4)

p19 es «puro, más fácil que la media de comodidad» (c.4; c.3 regla 5: respiro antes del pico): la colocación exige D(p19) < media de D de las comodidades del ciclo que no son tableros de estreno, y level-qa lo comprueba (QA-REMANSO).

| Ciclo | Remanso | D | bot_fail | Par | Comodidades | D medio | bot_fail medio | Puesto del remanso por D (de las comodidades y él) |
|---|---|---|---|---|---|---|---|---|
| 321-340 | 339 | -0.28 | 0.300 | 16 | 8 | -0.16 | 0.409 | 7 de 9 (1 = el más difícil) |

## Banda medida por rol (03 §7: «la banda coloca»)

La colocación exige, con el cuantil q de D en el pool del rango (recuperación ≤ 0,10, comodidad ≤ 0,70, reto ≤ 0,95, hito > 0,95): comodidad, estreno y remanso como mucho en banda de comodidad; recuperación (p1-p2, c.3 regla 3) en banda de recuperación o, como mínimo, con D por debajo de la comodidad más fácil del ciclo; hito (p20, «Pico» de c.3) en banda de hito o, como mínimo, con par − 2K ≥ la media del pool del rango; y en 21-40 los retos suaves por debajo del hito 40. level-qa lo comprueba regenerando la campaña (QA-BAND-Q y QA-LADDER).

| Ciclo | Recuperación p1 / p2: q (D) | Comodidad más fácil (D) | q máximo de comodidad y estreno | Remanso: q | Retos: D mín.-máx. | Hito: q (banda) · par − 2K / media del pool |
|---|---|---|---|---|---|---|
| 321-340 | 0.063 (-0.73) / 0.097 (-0.69) | -0.51 | 0.688 | 0.301 | 0.49 / 0.95 | 0.972 (hito) · 13 / 4.4 |

Todas las recuperaciones en banda de recuperación y todos los hitos en banda de hito.

## Vacíos de la comodidad y del remanso por rango (03 §4.4 y §11)

Comodidades sin estreno y remansos. «2 vacíos» = dos o más vacíos libres (sin mecánica, capacidad ≥ 4); «Cuia chica» = Cuia chica de 2 añadida como medio vacío por la relajación de 03 §4.4. Canon: 2 vacíos en 11-30; 1 desde el 31 (03 §11: «2 en respiros y recuperación» en 31-60, «2 en 30 % de comodidad» en 61-100 y 1 en 101-200; 03 §4.4: «1 (31+) con 2 en el 30 % de los slots») [P: se revisa con el tech-lead].

| Rango | Comodidades y remansos | Con 2 vacíos | Con Cuia chica de relajación | Relajados (%) | Canon |
|---|---|---|---|---|---|
| 321-340 | 9 | 3 | 0 | 33 % | 1 |

## Cuias por rango frente a 03 §11

Con 1 vacío, el K del rango que permiten los pares de riesgo (03 §4.5; `k_plan`) no siempre llega al mínimo de cuias de 03 §11 (aviso QA-CUIAS de level-qa) [P: k_plan, vacíos o la tabla se revisan en la integración].

| Rango | 03 §11 | Mínimo y máximo generados | Por debajo | Por encima | Niveles por debajo |
|---|---|---|---|---|---|
| 321-340 | 11-13 | 8-11 | 17 | 0 | 321 (9), 322 (9), 323 (10), 324 (10), 325 (10), 326 (10), 327 (10), 328 (8), 330 (9), 331 (8), 332 (10), 333 (9), 334 (8), 335 (9), 336 (8), 337 (8), 339 (9) |

## Saltos de banda (03 §12: +2,2 / −3,4)

Ninguno fuera del límite.

## Tinaja

Filtros de `TinajaWalks` sobre el tablero publicado (03 §8): ruta segura (`SafePath`), 20 paseos seguros (todos tienen que ganar), 30 paseos casuales (ganados / perdidos por error / de adivinar / sin terminar en 400 vertidos; ninguno puede ser de adivinar), «S0 sin trampas» en el estreno (vertidos útiles de S0 que no son seguros; tiene que ser 0) y la compuerta de coste de Morpho: w = 2 y 40000 nodos por realidad desde cero en S0, en la ruta y en los paseos, con un tope de 18750 nodos por consulta (150 ms × 1000 nodos/ms del PC [E] ÷ factor 8× del teléfono [P], hasta PF1). Todas las búsquedas van sin poda de fútiles. Dosis por tipo de nivel (D-019, Q-Ti1 A): 3 en estreno, comodidad y práctica; 4-5 en retos; 5 en el hito. Vacíos sin contar la pareja: 2 en los tableros de estreno y 1 en los demás (la comodidad fuera del estreno, 1 [P], pendiente de decisión). Como mucho 12 cuias con la pareja (02 §5) [P]: si no caben, se baja K.

| Nivel | p | Rol | Dosis (ocultas) | Realidades | Ruta segura | Paseos seguros | Casuales (g / p / a / s) | S0 sin trampas | Morpho: estados, nodos máx. | Candidatos rechazados por la tinaja |
|---|---|---|---|---|---|---|---|---|---|---|
| 323 | 3 | estreno | 3 (2) | 1 | 16 vertidos | 20/20 | 30 / 0 / 0 / 0 | 0 | 898, 412 | — |
| 324 | 4 | estreno | 3 (2) | 1 | 13 vertidos | 20/20 | 30 / 0 / 0 / 0 | 0 | 607, 560 | — |
| 325 | 5 | estreno | 3 (2) | 2 | 14 vertidos | 20/20 | 30 / 0 / 0 / 0 | 0 | 465, 260 | trampa en S0: 1 |
| 327 | 7 | comodidad | 3 (2) | 1 | 15 vertidos | 20/20 | 24 / 6 / 0 / 0 | — | 345, 205 | — |
| 329 | 9 | reto | 5 (4) | 24 | 23 vertidos | 20/20 | 11 / 19 / 0 / 0 | — | 484, 8298 | — |
| 332 | 12 | reto | 5 (4) | 18 | 22 vertidos | 20/20 | 19 / 11 / 0 / 0 | — | 683, 10356 | — |
| 338 | 18 | reto | 4 (3) | 1 | 27 vertidos | 20/20 | 8 / 22 / 0 / 0 | — | 1090, 3755 | adivinar: 1 |
| 340 | 20 | hito | 5 (4) | 18 | 29 vertidos | 20/20 | 3 / 27 / 0 / 0 | — | 760, 10606 | coste de Morpho: 2 |

Ningún nivel con tinaja en recuperación (p1-p2), remanso (p19) ni la rampa (D-019, Q-Ti1 A).

Alivio (03 §13): `minus_color` nunca quita una fruta de la tinaja y las variantes repiten la ruta segura y los paseos (con `TinajaWalks.ReliefOptions`, las de Morpho: w = 2 y 40000 nodos por realidad [P], pendiente de decisión) con la máscara de la tinaja, que el pack escribe en su `hidden` (`pack_schema` 3; 08 §4.1: `relief_board` admite `hidden` si y solo si su `attrs` contiene la tinaja). `plus_empty` no se publica si pasa de 12 cuias (02 §5) [P].
