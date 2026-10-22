# Curación de 301-420 (variante b): retos, hitos y estrenos para el dueño

Generado por `dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/campaign_v2.json --levels 301-420 --variant b --out Assets/_Project/Data/Levels --curation tools/out/curation_w07.md --decisions tools/level-generator/specs/curation_v2.json` con los mismos niveles colocados que el pack: no se edita a mano y no lleva tiempos (la decisión del dueño sale de `tools/level-generator/specs/curation_v2.json`). Niveles del mundo 7 (Madre de Dios, 301-420).

Fuentes: 09 §0.1 (fila de level-pipeline-dev: el dueño aprueba la curación de retos e hitos de cada paquete) y §0.3 (propuesta de curación de los 30 retos e hitos que aprueba el dueño), 03 §7 (un reto por debajo de 0,75 se publica sin etiqueta y la curación decide si se sustituye por otro del pool del slot; banda por el cuantil q de D en el pool del rango), D-009 (etiqueta por el bot ×40 con la semilla del nivel: Difícil ≥ 0,75 y Muy difícil ≥ 0,90; retos en p6, p9, p12, p15 y p18 e hito en p20; c.2: un reto o el hito fuera de su ventana se publica con la etiqueta que mide), 03 §11-§12, 02 §9.2 regla 2 (estrenos) y la tabla de novedades de D2 (d.5.3; D-023 punto 10). La columna «Propuesta» es [P]: la decide el dueño.

Compuerta automática (verde antes de pedir la curación): `dotnet run --project tools/level-qa -c Release -- Assets/_Project/Data/Levels/mundo_07_v1.json --spec tools/level-generator/specs/campaign_v2.json --levels 301-420 --variant b`. Cada reto o hito con aviso QA-WINDOW (bot_fail fuera de la ventana del slot) sale aquí como «revisar», y cada hito con aviso QA-BAND-Q (fuera de la banda de hito con el mínimo cumplido), como «revisar» o con nota de banda; los avisos QA-BAND-Q de las recuperaciones no entran en este informe. QA-BAND-Q no mira los retos y level-qa no mira risk_adjacent: las notas de banda de los retos y los «revisar» por risk_adjacent salen solo de este informe.

## Resumen

| Pack | Niveles | `pack_schema` | Bytes | SHA-256 |
|---|---|---|---|---|
| `Assets/_Project/Data/Levels/mundo_07_v1.json` | 120 | 3 | 175927 | `779a830acdea3ff83d5803cb3f9fe3d2ef5cf419b2b1a4bf34733c331cd211fe` |

- Retos e hitos: 36 (30 retos y 6 hitos); 35 miden la etiqueta objetivo o más (D-009) y 1 no.
- No miden su etiqueta objetivo y se publican con la que miden (c.2; 03 §7): 369 (reto Pi+B2 (D): Normal, bot_fail 0.450).
- Etiquetas publicadas en 301-420: 29 Difícil y 6 Muy difícil de 120 niveles (03 §11: 25 % y 5 %).
- Para revisar: 312 (reto), 329 (reto), 332 (reto), 340 (hito), 349 (reto), 355 (reto), 358 (reto), 369 (reto), 378 (reto), 395 (reto), 406 (reto), 412 (reto), 418 (reto), 420 (hito). El resto, aceptar.
- Estrenos: ★B2 en 343, 344, 345; ★Fl en 403, 404, 405 (6 de 6 con un ejemplar, 2 vacíos y bot_fail < 0,20).

## Retos e hitos

Etiqueta = la que mide el bot ×40 y se publica (D-009); Objetivo = la de la celda (D) o (MD). q = cuantil de D en el pool del rango (banda: recuperación ≤ 0,10, comodidad ≤ 0,70, reto ≤ 0,95, hito > 0,95). «Otros del slot» = candidatos medidos del mismo slot, sin rechazo, que miden la etiqueta objetivo o más: con ellos se puede sustituir el nivel al regenerar (03 §7), si la escalera del ciclo lo admite.

| Nivel | p | Rol | Calendario | K | Cuias | Vacíos (libres) | Par | bot_fail | Ventana | Etiqueta | Objetivo | D | q (banda) | Otros del slot | Propuesta [P] |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 306 | 6 | reto | Ha+Ll (D) | 11 | 13 | 2 (1) | 22 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.27 | 0.733 (reto) | 5 | aceptar |
| 309 | 9 | reto | Br+Ll (D) | 11 | 13 | 1 (0) | 25 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.29 | 0.748 (reto) | 2 | aceptar |
| 312 | 12 | reto | Ha+Br (D) | 11 | 13 | 1 (1) | 27 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.70 | 0.914 (reto) | 3 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 315 | 15 | reto | Br+Tu (D) | 11 | 13 | 1 (1) | 25 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.74 | 0.924 (reto) | 5 | aceptar (turbia: 3 dosis ocultas en 2 cuia(s), 6 realidad(es) compatibles con lo visible) |
| 318 | 18 | reto | Ln+Ll (D) | 11 | 13 | 2 (1) | 25 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.87 | 0.947 (reto) | 5 | aceptar |
| 320 | 20 | hito | Ha+Ll+Br (MD) | 11 | 13 | 1 (0) | 25 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 0.90 | 0.952 (hito) | 3 | aceptar |
| 326 | 6 | reto | Br+Ma (D) | 11 | 13 | 1 (0) | 25 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.35 | 0.790 (reto) | 5 | aceptar |
| 329 | 9 | reto | Ma (D) | 11 | 13 | 2 (1) | 23 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.40 | 0.835 (reto) | 3 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 332 | 12 | reto | Ba+Ho (D) | 11 | 13 | 1 (0) | 23 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.47 | 0.870 (reto) | 5 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 335 | 15 | reto | Ma+Ho (D) | 11 | 14 | 3 (1) | 26 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.55 | 0.883 (reto) | 8 | aceptar |
| 338 | 18 | reto | Ma+Ba (D) | 11 | 13 | 1 (0) | 24 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.58 | 0.892 (reto) | 5 | aceptar |
| 340 | 20 | hito | Br+Ba (MD) | 11 | 13 | 1 (1) | 24 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 0.83 | 0.939 (reto) | 4 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) (D en banda reto (q 0.939), con el mínimo del hito cumplido: par − 2K = 2 ≥ 1.9 (aviso QA-BAND-Q)) |
| 346 | 6 | reto | B2+Ln (D) | 11 | 13 | 1 (1) | 25 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.26 | 0.727 (reto) | 6 | aceptar |
| 349 | 9 | reto | B2 (D) | 11 | 13 | 1 (1) | 25 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.30 | 0.756 (reto) | 6 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 352 | 12 | reto | B2+Re (D) | 11 | 13 | 1 (1) | 22 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 0.36 | 0.805 (reto) | 5 | aceptar |
| 355 | 15 | reto | Re (D) | 11 | 12 | 1 (0) | 23 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.45 | 0.864 (reto) | 0 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 358 | 18 | reto | Ln+Re (D) | 11 | 13 | 2 (2) | 32 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 1.18 | 0.960 (hito) | 1 | **revisar**: risk_adjacent: 1 contacto(s) de tintes de un par de riesgo en una cuia (03 §4.5) |
| 360 | 20 | hito | B2+Ln (MD) | 11 | 13 | 1 (1) | 29 | 0.900 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.21 | 0.964 (hito) | 8 | aceptar |
| 366 | 6 | reto | Ho+Tu (D) | 11 | 13 | 1 (0) | 25 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.32 | 0.768 (reto) | 11 | aceptar (turbia: 2 dosis ocultas en 2 cuia(s), 2 realidad(es) compatibles con lo visible) |
| 369 | 9 | reto | Pi+B2 (D) | 11 | 14 | 1 (1) | 24 | 0.450 | [0.75, 0.90) | Normal | Difícil | 0.34 | 0.782 (reto) | 0 | **revisar**: mide Normal (bot_fail 0.450) y apunta a Difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir; ningún otro candidato del slot mide la etiqueta objetivo: sustituirlo pide más candidatos (otra ronda del generador) o cambiar la celda |
| 372 | 12 | reto | B2+Ho (D) | 11 | 13 | 1 (0) | 27 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 0.43 | 0.854 (reto) | 6 | aceptar |
| 375 | 15 | reto | Pi+Ba (D) | 11 | 13 | 0 (0) | 26 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.43 | 0.847 (reto) | 11 | aceptar |
| 378 | 18 | reto | Pi+Ho (D) | 11 | 14 | 2 (1) | 27 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.66 | 0.907 (reto) | 8 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 380 | 20 | hito | Pi+Ho (MD) | 11 | 13 | 1 (0) | 33 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.04 | 0.957 (hito) | 8 | aceptar |
| 386 | 6 | reto | Ba+Tu (D) | 11 | 13 | 1 (1) | 26 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 0.69 | 0.912 (reto) | 2 | aceptar (turbia: 1 dosis ocultas en 1 cuia(s), 1 realidad(es) compatibles con lo visible) |
| 389 | 9 | reto | Li+Ha (D) | 11 | 13 | 2 (2) | 34 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 0.92 | 0.953 (hito) | 2 | aceptar |
| 392 | 12 | reto | Ba (D) | 11 | 13 | 1 (1) | 34 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 1.03 | 0.954 (hito) | 10 | aceptar |
| 395 | 15 | reto | Li+Ba (D) | 11 | 13 | 1 (1) | 35 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 1.26 | 0.969 (hito) | 5 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 398 | 18 | reto | Ha+Ba (D) | 11 | 13 | 1 (1) | 34 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 1.29 | 0.970 (hito) | 1 | aceptar |
| 400 | 20 | hito | Li+Ha+Ba (MD) | 11 | 13 | 1 (1) | 34 | 0.975 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.48 | 0.979 (hito) | 17 | aceptar |
| 406 | 6 | reto | Ba+Ll (D) | 11 | 13 | 2 (1) | 22 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.18 | 0.619 (comodidad) | 17 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) (Difícil por el bot, con D en banda comodidad (q 0.619)) |
| 409 | 9 | reto | Fl (D) | 11 | 12 | 1 (1) | 22 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.23 | 0.656 (comodidad) | 1 | aceptar (Difícil por el bot, con D en banda comodidad (q 0.656)) |
| 412 | 12 | reto | Fl+Ba (D) | 11 | 12 | 0 (0) | 23 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.45 | 0.750 (reto) | 0 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 415 | 15 | reto | Fl+Ll (D) | 11 | 13 | 2 (1) | 22 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.47 | 0.769 (reto) | 17 | aceptar |
| 418 | 18 | reto | Ba+Tu (D) | 11 | 13 | 1 (1) | 24 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 0.76 | 0.931 (reto) | 3 | **revisar**: cvd_adjacent: 2 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) (turbia: 1 dosis ocultas en 1 cuia(s), 1 realidad(es) compatibles con lo visible) |
| 420 | 20 | hito | Fl+B2 (MD) «Zafra» [P] | 11 | 13 | 1 (1) | 24 | 0.950 | [0.90, 1.00] | Muy difícil | Muy difícil | 0.99 | 0.956 (hito) | 17 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |

## Hitos

p20 de cada ciclo («Pico» de c.3): Muy difícil por el bot (≥ 0,90; D-009) y, por D, en banda de hito (q > 0,95; 03 §7) o, como mínimo, con par − 2K ≥ la media del pool del rango (criterio [P] del generador y de level-qa, revisión del 2026-10-09; README del generador; QA-BAND-Q). Los nombres de hito de la celda son [P] y pasan por el asesor (D-023 punto 21): no van al pack.

### 320 · Ha+Ll+Br (MD)

- Tablero: K 11, 13 cuias (13 de 4), 1 vacío(s) (0 libre(s)); fuente `deep`.
- Mecánicas: Ll (cuia de 4); Br (cuia de 4); hoja_atada key=7 (3 dosis).
- Medida: par 25 (2K = 22; par − 2K = 3, media del pool 1.9), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 0.90, q 0.952 (banda hito), ramificación 6.64, trampas tempranas 0.
- Propuesta [P]: aceptar.

### 340 · Br+Ba (MD)

- Tablero: K 11, 13 cuias (13 de 4), 1 vacío(s) (1 libre(s)); fuente `deep`.
- Mecánicas: Br (cuia de 4); Barro: 1 dosis.
- Medida: par 24 (2K = 22; par − 2K = 2, media del pool 1.9), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 0.83, q 0.939 (banda reto), ramificación 5.21, trampas tempranas 3.
- Propuesta [P]: **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) (D en banda reto (q 0.939), con el mínimo del hito cumplido: par − 2K = 2 ≥ 1.9 (aviso QA-BAND-Q)).

### 360 · B2+Ln (MD)

- Tablero: K 11, 13 cuias (13 de 4), 1 vacío(s) (1 libre(s)); fuente `deep`.
- Mecánicas: liana n=1 (4 dosis); brote n=2 (1 dosis).
- Medida: par 29 (2K = 22; par − 2K = 7, media del pool 1.9), bot_fail 0.900 → Muy difícil (objetivo Muy difícil), D 1.21, q 0.964 (banda hito), ramificación 4.97, trampas tempranas 0.
- Notas de la generación: relajacion: deep con 60.000 nodos; 1 ronda(s) extra de candidatos.
- Propuesta [P]: aceptar.

### 380 · Pi+Ho (MD)

- Tablero: K 11, 13 cuias (12 de 4, 1 de 2), 1 vacío(s) (0 libre(s)); fuente `fill`.
- Mecánicas: Pi (cuia de 2); Ho key=14 (cuia de 4).
- Medida: par 33 (2K = 22; par − 2K = 11, media del pool 1.9), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 1.04, q 0.957 (banda hito), ramificación 1.97, trampas tempranas 0.
- Notas de la generación: 1 ronda(s) extra de candidatos.
- Propuesta [P]: aceptar.

### 400 · Li+Ha+Ba (MD)

- Tablero: K 11, 13 cuias (13 de 4), 1 vacío(s) (1 libre(s)); fuente `fill`.
- Mecánicas: Li key=11 (cuia de 4); hoja_atada key=5 (4 dosis); Barro: 1 dosis.
- Medida: par 34 (2K = 22; par − 2K = 12, media del pool 1.9), bot_fail 0.975 → Muy difícil (objetivo Muy difícil), D 1.48, q 0.979 (banda hito), ramificación 3.97, trampas tempranas 0.
- Notas de la generación: relajacion: fill con 2 vacios (03 sec. 4.4); 2 ronda(s) extra de candidatos.
- Propuesta [P]: aceptar.

### 420 · Fl+B2 (MD) «Zafra» [P]

- Tablero: K 11, 13 cuias (13 de 4), 1 vacío(s) (1 libre(s)); fuente `deep`.
- Mecánicas: brote n=2 (2 dosis); flor key=4 key2=13 (3 dosis).
- Medida: par 24 (2K = 22; par − 2K = 2, media del pool 1.6), bot_fail 0.950 → Muy difícil (objetivo Muy difícil), D 0.99, q 0.956 (banda hito), ramificación 5.79, trampas tempranas 0.
- Notas de la generación: 2 ronda(s) extra de candidatos.
- Propuesta [P]: **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3).

## Tableros de estreno

02 §9.2 regla 2: el estreno va en 2-3 tableros fáciles (p3-p5) con la mecánica sola, un ejemplar, 2 vacíos y `bot_fail` < 0,20; el primero lleva `intro_mechanic` (`skippable` = false).

| Nivel | p | Calendario | `intro_mechanic` | Mecánica | Ejemplares | Vacíos | Par | bot_fail | D | q (banda) | Regla 2 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 343 | 3 | ★B2 | `brote_n2` | brote n=2 (2 dosis) | 1 | 2 | 22 | 0.000 | -0.64 | 0.074 (recuperación) | cumple |
| 344 | 4 | ★B2 | — | brote n=2 (1 dosis) | 1 | 2 | 22 | 0.000 | -0.72 | 0.041 (recuperación) | cumple |
| 345 | 5 | ★B2 | — | brote n=2 (2 dosis) | 1 | 2 | 22 | 0.000 | -0.69 | 0.057 (recuperación) | cumple |
| 403 | 3 | ★Fl | `flor` | flor key=3 key2=1 (4 dosis) | 1 | 2 | 21 | 0.150 | -0.51 | 0.169 (comodidad) | cumple |
| 404 | 4 | ★Fl | — | flor key=3 key2=2 (4 dosis) | 1 | 2 | 20 | 0.125 | -0.93 | 0.031 (recuperación) | cumple |
| 405 | 5 | ★Fl | — | flor key=13 key2=5 (4 dosis) | 1 | 2 | 23 | 0.100 | -0.63 | 0.106 (comodidad) | cumple |

## Variantes de la 1.0

Variantes de 02 §9.1 (propuesta de D-007 b.3; firma de `pack_schema` 3, D-023 punto 5) con los códigos de d.5.1 [P]: Ln = Liana de cuenta (`liana` con `n`; n = 1 en el estreno y 1-2 en los demás [P]), Ha = Hoja atada (`hoja_atada`, sobre una cuia con su fruta en el fondo y cap = U_c, D-024), B2 = Brote de dos hojas (`brote` con `n` = 2, 1-2 dosis) y Fl = Flor de vitória-régia (`flor` con `key` y `key2` distintas). Estreno (02 §9.2 regla 2): un ejemplar, 2 vacíos y `bot_fail` < 0,20.

| Variante | Debut | Niveles | Ejemplares | Tableros de estreno | Estrenos con un ejemplar, 2 vacíos y bot < 0,20 |
|---|---|---|---|---|---|
| Ln | 243 (`liana_n`) | 5 | 5 | 0 | 0 de 0 |
| Ha | 283 (`hoja_atada`) | 10 | 10 | 0 | 0 de 0 |
| B2 | 343 (`brote_n2`) | 11 | 11 | 3 | 3 de 3 |
| Fl | 403 (`flor`) | 8 | 8 | 3 | 3 de 3 |

| Nivel | p | Rol | Celda | Variante | Ejemplares de mecánica | Vacíos | bot_fail |
|---|---|---|---|---|---|---|---|
| 306 | 6 | reto | Ha+Ll (D) | hoja_atada key=4 (4 dosis) | 2 | 2 | 0.850 |
| 312 | 12 | reto | Ha+Br (D) | hoja_atada key=1 (4 dosis) | 2 | 1 | 0.875 |
| 318 | 18 | reto | Ln+Ll (D) | liana n=1 (4 dosis) | 2 | 2 | 0.800 |
| 320 | 20 | hito | Ha+Ll+Br (MD) | hoja_atada key=7 (3 dosis) | 3 | 1 | 1.000 |
| 343 | 3 | estreno | ★B2 | brote n=2 (2 dosis) | 1 | 2 | 0.000 |
| 344 | 4 | estreno | ★B2 | brote n=2 (1 dosis) | 1 | 2 | 0.000 |
| 345 | 5 | estreno | ★B2 | brote n=2 (2 dosis) | 1 | 2 | 0.000 |
| 346 | 6 | reto | B2+Ln (D) | brote n=2 (2 dosis); liana n=1 (3 dosis) | 2 | 1 | 0.775 |
| 347 | 7 | comodidad | B2 | brote n=2 (2 dosis) | 1 | 1 | 0.375 |
| 349 | 9 | reto | B2 (D) | brote n=2 (2 dosis) | 1 | 1 | 0.775 |
| 351 | 11 | comodidad | Ln | liana n=2 (4 dosis) | 1 | 2 | 0.550 |
| 352 | 12 | reto | B2+Re (D) | brote n=2 (2 dosis) | 2 | 1 | 0.750 |
| 354 | 14 | comodidad | Ha+Al | hoja_atada key=14 (4 dosis) | 1 | 1 | 0.450 |
| 358 | 18 | reto | Ln+Re (D) | liana n=2 (4 dosis) | 2 | 2 | 0.875 |
| 360 | 20 | hito | B2+Ln (MD) | liana n=1 (4 dosis); brote n=2 (1 dosis) | 2 | 1 | 0.900 |
| 368 | 8 | comodidad | Ha | hoja_atada key=10 (4 dosis) | 1 | 1 | 0.525 |
| 369 | 9 | reto | Pi+B2 (D) | brote n=2 (2 dosis) | 2 | 1 | 0.450 |
| 372 | 12 | reto | B2+Ho (D) | brote n=2 (2 dosis) | 2 | 1 | 0.750 |
| 385 | 5 | comodidad | Ha | hoja_atada key=12 (4 dosis) | 1 | 1 | 0.500 |
| 389 | 9 | reto | Li+Ha (D) | hoja_atada key=8 (4 dosis) | 2 | 2 | 0.750 |
| 398 | 18 | reto | Ha+Ba (D) | hoja_atada key=2 (4 dosis) | 2 | 1 | 0.825 |
| 400 | 20 | hito | Li+Ha+Ba (MD) | hoja_atada key=5 (4 dosis) | 3 | 1 | 0.975 |
| 403 | 3 | estreno | ★Fl | flor key=3 key2=1 (4 dosis) | 1 | 2 | 0.150 |
| 404 | 4 | estreno | ★Fl | flor key=3 key2=2 (4 dosis) | 1 | 2 | 0.125 |
| 405 | 5 | estreno | ★Fl | flor key=13 key2=5 (4 dosis) | 1 | 2 | 0.100 |
| 407 | 7 | comodidad | Fl | flor key=12 key2=2 (4 dosis) | 1 | 2 | 0.550 |
| 409 | 9 | reto | Fl (D) | flor key=1 key2=13 (4 dosis) | 1 | 1 | 0.775 |
| 412 | 12 | reto | Fl+Ba (D) | flor key=6 key2=12 (4 dosis) | 2 | 0 | 0.850 |
| 415 | 15 | reto | Fl+Ll (D) | flor key=7 key2=1 (4 dosis) | 2 | 2 | 0.875 |
| 417 | 17 | comodidad | Ha | hoja_atada key=10 (4 dosis) | 1 | 1 | 0.600 |
| 420 | 20 | hito | Fl+B2 (MD) «Zafra» [P] | brote n=2 (2 dosis); flor key=4 key2=13 (3 dosis) | 2 | 1 | 0.950 |

Todas las celdas con una variante la llevan en el nivel publicado.

## Dónde va la Tinaja (variante b)

D-019 (Q-Ti1 A: nunca en recuperación, p1-p2, ni en remanso, p19; Q-Ti3 A: estreno en el 323, en la 1.0 si pasa sus pruebas y si Morpho con corte por tiempo y la Luciérnaga están listos en el cliente; si no, la salida B), la tabla de D2 (d.5.3, con el respaldo de cada celda en d.5.4; D-023 punto 10) y la propuesta de la tinaja d.3 (ciclo de estreno 321-340, «C0»: como mucho 8 apariciones). La variante a es la del dueño; la b, la salida B, se guarda aparte. Las 5 dosis de los retos y el hito dependen de la compuerta de coste de Morpho, con el factor 8× [P] del teléfono hasta medirlo en PF1 (pregunta 13): si PF1 lo cambia, este pack se regenera antes de integrarlo en el cliente (C1b).

| Nivel | p | Rol | Celda en la a | Respaldo en la b | Publicado en este pack | Dosis (ocultas) | K | Cuias |
|---|---|---|---|---|---|---|---|---|
| 323 | 3 | comodidad | ★Ti | Ma | Ma: sin la Tinaja | — | 11 | 13 |
| 324 | 4 | comodidad | ★Ti | C | C: sin la Tinaja | — | 11 | 13 |
| 325 | 5 | comodidad | ★Ti | Ba | Ba: sin la Tinaja | — | 11 | 13 |
| 327 | 7 | comodidad | Ti | C | C: sin la Tinaja | — | 11 | 13 |
| 329 | 9 | reto | Ti (D) | Ma (D) | Ma (D): sin la Tinaja | — | 11 | 13 |
| 332 | 12 | reto | Ti+Ho (D) | Ba+Ho (D) | Ba+Ho (D): sin la Tinaja | — | 11 | 13 |
| 338 | 18 | reto | Ti+Ma (D) | Ma+Ba (D) | Ma+Ba (D): sin la Tinaja | — | 11 | 13 |
| 340 | 20 | hito | Ti+Ho (MD) | Br+Ba (MD) | Br+Ba (MD): sin la Tinaja | — | 11 | 13 |
| 355 | 15 | reto | Ti (D) | Re (D) | Re (D): sin la Tinaja | — | 11 | 12 |
| 392 | 12 | reto | Ti+Ha (D) | Ba (D) | Ba (D): sin la Tinaja | — | 11 | 13 |

Apariciones por ciclo (calendario de la a): 321-340: 8 (323, 324, 325, 327, 329, 332, 338, 340); 341-360: 1 (355); 381-400: 1 (392).

- Tinaja solo donde la pone el calendario: sí.
- Nunca en p1, p2 ni p19: ninguno.
- Ciclo de estreno 321-340: 0 niveles publicados con la Tinaja (como mucho 8).
- Variante b: 0 niveles con la Tinaja (la salida B no la lleva).

## Otros niveles con avisos

| Nivel | Rol | Calendario | Aviso |
|---|---|---|---|
| 307 | comodidad | C | cvd_adjacent: 1 contacto(s) |
| 308 | comodidad | Re | cvd_adjacent: 1 contacto(s) |
| 316 | comodidad | C | cvd_adjacent: 1 contacto(s) |
| 323 | comodidad | Ma | cvd_adjacent: 2 contacto(s) |
| 324 | comodidad | C | cvd_adjacent: 1 contacto(s) |
| 330 | comodidad | C | cvd_adjacent: 1 contacto(s) |
| 331 | comodidad | Ma | cvd_adjacent: 1 contacto(s) |
| 334 | comodidad | Re | cvd_adjacent: 1 contacto(s) |
| 336 | comodidad | C | cvd_adjacent: 1 contacto(s) |
| 339 | remanso | S | cvd_adjacent: 3 contacto(s) |
| 343 | estreno | ★B2 | cvd_adjacent: 2 contacto(s) |
| 348 | comodidad | C | cvd_adjacent: 2 contacto(s) |
| 351 | comodidad | Ln | cvd_adjacent: 2 contacto(s) |
| 357 | comodidad | Br | cvd_adjacent: 2 contacto(s) |
| 362 | recuperación | R | cvd_adjacent: 1 contacto(s) |
| 363 | comodidad | Pi | cvd_adjacent: 1 contacto(s) |
| 365 | comodidad | Ho | cvd_adjacent: 1 contacto(s) |
| 367 | comodidad | C | cvd_adjacent: 3 contacto(s) |
| 373 | comodidad | C | risk_adjacent: 1 contacto(s) |
| 381 | recuperación | R | cvd_adjacent: 2 contacto(s) |
| 385 | comodidad | Ha | risk_adjacent: 1 contacto(s) |
| 399 | remanso | S | risk_adjacent: 1 contacto(s); cvd_adjacent: 1 contacto(s) |
| 403 | estreno | ★Fl | cvd_adjacent: 2 contacto(s) |
| 407 | comodidad | Fl | cvd_adjacent: 3 contacto(s); tras una etiqueta no baja una banda entera de D (solo baja D) |
| 410 | comodidad | C | cvd_adjacent: 1 contacto(s); tras una etiqueta no baja una banda entera de D (solo baja D) |
| 413 | comodidad | C | cvd_adjacent: 1 contacto(s) |

## Curación del dueño

Estado: **pendiente** (0 de 38 niveles decididos) (09 §0.1 y §0.3). La decisión por nivel es «aceptar», «sustituir» (otro candidato del slot o del pool, 03 §7) o «cambiar la celda», con el motivo en los dos últimos casos. No se escribe en este informe, que es generado: va en `tools/level-generator/specs/curation_v2.json`, una entrada por nivel con el literal de la celda que vio el dueño, y la última columna la copia de ahí; si la celda cambia, la decisión deja de valer y el nivel vuelve a «pendiente». Si la curación pide cambios, se cambian la especificación o el generador y se regeneran el pack, este informe y la comprobación de `level-qa`.

| Nivel | Rol | Calendario | Etiqueta / objetivo | Propuesta [P] | Decisión del dueño |
|---|---|---|---|---|---|
| 306 | reto | Ha+Ll (D) | Difícil / Difícil | aceptar | pendiente |
| 309 | reto | Br+Ll (D) | Difícil / Difícil | aceptar | pendiente |
| 312 | reto | Ha+Br (D) | Difícil / Difícil | revisar | pendiente |
| 315 | reto | Br+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 318 | reto | Ln+Ll (D) | Difícil / Difícil | aceptar | pendiente |
| 320 | hito | Ha+Ll+Br (MD) | Muy difícil / Muy difícil | aceptar | pendiente |
| 326 | reto | Br+Ma (D) | Difícil / Difícil | aceptar | pendiente |
| 329 | reto | Ma (D) | Difícil / Difícil | revisar | pendiente |
| 332 | reto | Ba+Ho (D) | Difícil / Difícil | revisar | pendiente |
| 335 | reto | Ma+Ho (D) | Difícil / Difícil | aceptar | pendiente |
| 338 | reto | Ma+Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 340 | hito | Br+Ba (MD) | Muy difícil / Muy difícil | revisar | pendiente |
| 346 | reto | B2+Ln (D) | Difícil / Difícil | aceptar | pendiente |
| 349 | reto | B2 (D) | Difícil / Difícil | revisar | pendiente |
| 352 | reto | B2+Re (D) | Difícil / Difícil | aceptar | pendiente |
| 355 | reto | Re (D) | Difícil / Difícil | revisar | pendiente |
| 358 | reto | Ln+Re (D) | Difícil / Difícil | revisar | pendiente |
| 360 | hito | B2+Ln (MD) | Muy difícil / Muy difícil | aceptar | pendiente |
| 366 | reto | Ho+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 369 | reto | Pi+B2 (D) | Normal / Difícil | revisar | pendiente |
| 372 | reto | B2+Ho (D) | Difícil / Difícil | aceptar | pendiente |
| 375 | reto | Pi+Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 378 | reto | Pi+Ho (D) | Difícil / Difícil | revisar | pendiente |
| 380 | hito | Pi+Ho (MD) | Muy difícil / Muy difícil | aceptar | pendiente |
| 386 | reto | Ba+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 389 | reto | Li+Ha (D) | Difícil / Difícil | aceptar | pendiente |
| 392 | reto | Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 395 | reto | Li+Ba (D) | Difícil / Difícil | revisar | pendiente |
| 398 | reto | Ha+Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 400 | hito | Li+Ha+Ba (MD) | Muy difícil / Muy difícil | aceptar | pendiente |
| 406 | reto | Ba+Ll (D) | Difícil / Difícil | revisar | pendiente |
| 409 | reto | Fl (D) | Difícil / Difícil | aceptar | pendiente |
| 412 | reto | Fl+Ba (D) | Difícil / Difícil | revisar | pendiente |
| 415 | reto | Fl+Ll (D) | Difícil / Difícil | aceptar | pendiente |
| 418 | reto | Ba+Tu (D) | Difícil / Difícil | revisar | pendiente |
| 420 | hito | Fl+B2 (MD) «Zafra» [P] | Muy difícil / Muy difícil | revisar | pendiente |
| 343 | estreno | ★B2 | Normal / — | aceptar | pendiente |
| 403 | estreno | ★Fl | Normal / — | aceptar | pendiente |
