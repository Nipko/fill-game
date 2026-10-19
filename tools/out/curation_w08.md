# Curación de 421-560 (variante a): retos, hitos y estrenos para el dueño

Generado por `dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/campaign_v2.json --levels 421-560 --variant a --out Assets/_Project/Data/Levels --curation tools/out/curation_w08.md --decisions tools/level-generator/specs/curation_v2.json` con los mismos niveles colocados que el pack: no se edita a mano y no lleva tiempos (la decisión del dueño sale de `tools/level-generator/specs/curation_v2.json`). Niveles del mundo 8 (Madidi, 421-560).

Fuentes: 09 §0.1 (fila de level-pipeline-dev: el dueño aprueba la curación de retos e hitos de cada paquete) y §0.3 (propuesta de curación de los 30 retos e hitos que aprueba el dueño), 03 §7 (un reto por debajo de 0,75 se publica sin etiqueta y la curación decide si se sustituye por otro del pool del slot; banda por el cuantil q de D en el pool del rango), D-009 (etiqueta por el bot ×40 con la semilla del nivel: Difícil ≥ 0,75 y Muy difícil ≥ 0,90; retos en p6, p9, p12, p15 y p18 e hito en p20; c.2: un reto o el hito fuera de su ventana se publica con la etiqueta que mide), 03 §11-§12, 02 §9.2 regla 2 (estrenos) y la tabla de novedades de D2 (d.5.3; D-023 punto 10). La columna «Propuesta» es [P]: la decide el dueño.

Compuerta automática (verde antes de pedir la curación): `dotnet run --project tools/level-qa -c Release -- Assets/_Project/Data/Levels/mundo_08_v1.json --spec tools/level-generator/specs/campaign_v2.json --levels 421-560 --variant a`. Cada reto o hito con aviso QA-WINDOW (bot_fail fuera de la ventana del slot) sale aquí como «revisar», y cada hito con aviso QA-BAND-Q (fuera de la banda de hito con el mínimo cumplido), como «revisar» o con nota de banda; los avisos QA-BAND-Q de las recuperaciones no entran en este informe. QA-BAND-Q no mira los retos y level-qa no mira risk_adjacent: las notas de banda de los retos y los «revisar» por risk_adjacent salen solo de este informe.

## Resumen

| Pack | Niveles | `pack_schema` | Bytes | SHA-256 |
|---|---|---|---|---|
| `Assets/_Project/Data/Levels/mundo_08_v1.json` | 140 | 3 | 201608 | `c3e8c7bf3465285b1ab519ea4eb248f6b8c1780837179d7560d0f73d3f6c2ac9` |

- Retos e hitos: 42 (35 retos y 7 hitos); 37 miden la etiqueta objetivo o más (D-009) y 5 no.
- No miden su etiqueta objetivo y se publican con la que miden (c.2; 03 §7): 429 (reto Ma+Re (D): Normal, bot_fail 0.650), 452 (reto Pi+Br (D): Normal, bot_fail 0.600), 466 (reto Li+B2 (D): Normal, bot_fail 0.675), 469 (reto Ln+B2 (D): Normal, bot_fail 0.725), 486 (reto Ma+Ha (D): Normal, bot_fail 0.725).
- Etiquetas publicadas en 421-560: 30 Difícil y 7 Muy difícil de 140 niveles (03 §11: 25 % y 5 %).
- Para revisar: 429 (reto), 452 (reto), 466 (reto), 469 (reto), 486 (reto). El resto, aceptar.

## Retos e hitos

Etiqueta = la que mide el bot ×40 y se publica (D-009); Objetivo = la de la celda (D) o (MD). q = cuantil de D en el pool del rango (banda: recuperación ≤ 0,10, comodidad ≤ 0,70, reto ≤ 0,95, hito > 0,95). «Otros del slot» = candidatos medidos del mismo slot, sin rechazo, que miden la etiqueta objetivo o más: con ellos se puede sustituir el nivel al regenerar (03 §7), si la escalera del ciclo lo admite.

| Nivel | p | Rol | Calendario | K | Cuias | Vacíos (libres) | Par | bot_fail | Ventana | Etiqueta | Objetivo | D | q (banda) | Otros del slot | Propuesta [P] |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 426 | 6 | reto | Fl+Ma (D) | 10 | 12 | 2 (1) | 21 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.17 | 0.633 (comodidad) | 5 | aceptar (Difícil por el bot, con D en banda comodidad (q 0.633)) |
| 429 | 9 | reto | Ma+Re (D) | 10 | 12 | 2 (0) | 20 | 0.650 | [0.75, 0.90) | Normal | Difícil | 0.26 | 0.701 (reto) | 4 | **revisar**: mide Normal (bot_fail 0.650) y apunta a Difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir |
| 432 | 12 | reto | Fl+Ho (D) | 10 | 12 | 2 (1) | 23 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.30 | 0.739 (reto) | 5 | aceptar |
| 435 | 15 | reto | Ma+Tu (D) | 10 | 12 | 1 (0) | 23 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.52 | 0.845 (reto) | 5 | aceptar (turbia: 1 dosis ocultas en 1 cuia(s), 1 realidad(es) compatibles con lo visible) |
| 438 | 18 | reto | Ho+Re (D) | 10 | 12 | 2 (1) | 22 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 0.63 | 0.878 (reto) | 5 | aceptar |
| 440 | 20 | hito | Ma+Ho (MD) | 11 | 13 | 2 (0) | 28 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.04 | 0.954 (hito) | 5 | aceptar |
| 446 | 6 | reto | Pi+Ba (D) | 10 | 12 | 0 (0) | 21 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.26 | 0.700 (reto) | 11 | aceptar |
| 449 | 9 | reto | Br+Re (D) | 10 | 12 | 1 (1) | 20 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 0.48 | 0.837 (reto) | 2 | aceptar |
| 452 | 12 | reto | Pi+Br (D) | 10 | 13 | 1 (1) | 21 | 0.600 | [0.75, 0.90) | Normal | Difícil | 0.56 | 0.858 (reto) | 0 | **revisar**: mide Normal (bot_fail 0.600) y apunta a Difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir; ningún otro candidato del slot mide la etiqueta objetivo: sustituirlo pide más candidatos (otra ronda del generador) o cambiar la celda |
| 455 | 15 | reto | Br+Tu (D) | 10 | 12 | 1 (1) | 25 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.63 | 0.878 (reto) | 11 | aceptar (turbia: 2 dosis ocultas en 1 cuia(s), 1 realidad(es) compatibles con lo visible) |
| 458 | 18 | reto | Ba+Re (D) | 10 | 11 | 0 (0) | 20 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.71 | 0.899 (reto) | 0 | aceptar |
| 460 | 20 | hito | Pi+Ba (MD) | 11 | 13 | 0 (0) | 32 | 0.925 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.07 | 0.957 (hito) | 11 | aceptar |
| 466 | 6 | reto | Li+B2 (D) | 10 | 12 | 1 (1) | 20 | 0.675 | [0.75, 0.90) | Normal | Difícil | 0.10 | 0.590 (comodidad) | 12 | **revisar**: mide Normal (bot_fail 0.675) y apunta a Difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir |
| 469 | 9 | reto | Ln+B2 (D) | 10 | 12 | 1 (1) | 22 | 0.725 | [0.75, 0.90) | Normal | Difícil | 0.15 | 0.622 (comodidad) | 8 | **revisar**: mide Normal (bot_fail 0.725) y apunta a Difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir |
| 472 | 12 | reto | Li+Ho (D) | 10 | 12 | 2 (1) | 23 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 0.32 | 0.756 (reto) | 11 | aceptar |
| 475 | 15 | reto | Ho+Tu (D) | 10 | 12 | 1 (0) | 25 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.39 | 0.795 (reto) | 11 | aceptar (turbia: 2 dosis ocultas en 1 cuia(s), 2 realidad(es) compatibles con lo visible) |
| 478 | 18 | reto | Ln+Ho (D) | 10 | 11 | 1 (0) | 23 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.39 | 0.797 (reto) | 10 | aceptar |
| 480 | 20 | hito | Li+B2+Ho (MD) | 11 | 13 | 1 (0) | 25 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.04 | 0.954 (hito) | 30 | aceptar |
| 486 | 6 | reto | Ma+Ha (D) | 10 | 12 | 2 (1) | 20 | 0.725 | [0.75, 0.90) | Normal | Difícil | 0.31 | 0.748 (reto) | 5 | **revisar**: mide Normal (bot_fail 0.725) y apunta a Difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir |
| 489 | 9 | reto | Ma+Ba (D) | 10 | 12 | 1 (0) | 20 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.40 | 0.807 (reto) | 10 | aceptar |
| 492 | 12 | reto | Ha+Ba (D) | 10 | 11 | 0 (0) | 24 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 0.48 | 0.838 (reto) | 5 | aceptar |
| 495 | 15 | reto | Ma+Tu (D) | 10 | 12 | 1 (0) | 25 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 0.58 | 0.863 (reto) | 11 | aceptar (turbia: 2 dosis ocultas en 1 cuia(s), 1 realidad(es) compatibles con lo visible) |
| 498 | 18 | reto | Fl+Ha (D) | 10 | 12 | 2 (2) | 24 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.76 | 0.918 (reto) | 1 | aceptar |
| 500 | 20 | hito | Ma+Ha (MD) | 11 | 12 | 1 (0) | 25 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 0.99 | 0.951 (hito) | 11 | aceptar |
| 506 | 6 | reto | Ll+Ba (D) | 10 | 12 | 2 (1) | 22 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.31 | 0.752 (reto) | 11 | aceptar |
| 509 | 9 | reto | Fl (D) | 10 | 11 | 1 (1) | 21 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.32 | 0.755 (reto) | 2 | aceptar |
| 512 | 12 | reto | Ll+Ha (D) | 10 | 12 | 2 (1) | 21 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.32 | 0.754 (reto) | 11 | aceptar |
| 515 | 15 | reto | Ba+Tu (D) | 10 | 12 | 1 (1) | 25 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.65 | 0.884 (reto) | 1 | aceptar (turbia: 2 dosis ocultas en 1 cuia(s), 2 realidad(es) compatibles con lo visible) |
| 518 | 18 | reto | Fl+Ba (D) | 10 | 12 | 1 (1) | 30 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.99 | 0.951 (hito) | 2 | aceptar |
| 520 | 20 | hito | Ll+Ba+Ha (MD) | 11 | 12 | 1 (0) | 27 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.19 | 0.972 (hito) | 11 | aceptar |
| 526 | 6 | reto | Pi+Ha (D) | 10 | 12 | 1 (1) | 20 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.23 | 0.675 (comodidad) | 2 | aceptar (Difícil por el bot, con D en banda comodidad (q 0.675)) |
| 529 | 9 | reto | Ho+Re (D) | 10 | 12 | 2 (0) | 22 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.27 | 0.707 (reto) | 5 | aceptar |
| 532 | 12 | reto | Pi+Ho (D) | 10 | 12 | 1 (0) | 23 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.31 | 0.751 (reto) | 2 | aceptar |
| 535 | 15 | reto | Ho+Tu (D) | 10 | 12 | 1 (0) | 22 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.37 | 0.786 (reto) | 5 | aceptar (turbia: 1 dosis ocultas en 1 cuia(s), 1 realidad(es) compatibles con lo visible) |
| 538 | 18 | reto | Ha+Re (D) | 10 | 11 | 1 (1) | 21 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.48 | 0.835 (reto) | 0 | aceptar |
| 540 | 20 | hito | Pi+Ha (MD) | 11 | 13 | 1 (1) | 33 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.52 | 0.989 (hito) | 5 | aceptar |
| 546 | 6 | reto | Fl+Ho (D) | 10 | 12 | 2 (1) | 21 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.30 | 0.744 (reto) | 11 | aceptar |
| 549 | 9 | reto | Ln+Ho (D) | 10 | 12 | 2 (1) | 22 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 0.34 | 0.766 (reto) | 11 | aceptar |
| 552 | 12 | reto | Ln+Re (D) | 10 | 11 | 1 (0) | 24 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.40 | 0.799 (reto) | 2 | aceptar |
| 555 | 15 | reto | Ho+Tu (D) | 10 | 12 | 1 (0) | 25 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.40 | 0.803 (reto) | 11 | aceptar (turbia: 2 dosis ocultas en 1 cuia(s), 2 realidad(es) compatibles con lo visible) |
| 558 | 18 | reto | Fl (D) | 10 | 12 | 2 (2) | 30 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 0.87 | 0.936 (reto) | 4 | aceptar |
| 560 | 20 | hito | Fl+Re (MD) «Riberalta» [P] | 11 | 12 | 1 (1) | 27 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.09 | 0.960 (hito) | 11 | aceptar |

## Hitos

p20 de cada ciclo («Pico» de c.3): Muy difícil por el bot (≥ 0,90; D-009) y, por D, en banda de hito (q > 0,95; 03 §7) o, como mínimo, con par − 2K ≥ la media del pool del rango (criterio [P] del generador y de level-qa, revisión del 2026-10-09; README del generador; QA-BAND-Q). Los nombres de hito de la celda son [P] y pasan por el asesor (D-023 punto 21): no van al pack.

### 440 · Ma+Ho (MD)

- Tablero: K 11, 13 cuias (13 de 4), 2 vacío(s) (0 libre(s)); fuente `deep`.
- Mecánicas: Ma (cuia de 4); Ho key=3 (cuia de 4).
- Medida: par 28 (2K = 22; par − 2K = 6, media del pool 2.1), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 1.04, q 0.954 (banda hito), ramificación 2.86, trampas tempranas 4.
- Propuesta [P]: aceptar.

### 460 · Pi+Ba (MD)

- Tablero: K 11, 13 cuias (12 de 4, 1 de 2), 0 vacío(s) (0 libre(s)); fuente `fill`.
- Mecánicas: Pi (cuia de 2); Barro: 1 dosis.
- Medida: par 32 (2K = 22; par − 2K = 10, media del pool 2.1), bot_fail 0.925 → Muy difícil (objetivo Muy difícil), D 1.07, q 0.957 (banda hito), ramificación 3.50, trampas tempranas 1.
- Notas de la generación: 1 ronda(s) extra de candidatos.
- Propuesta [P]: aceptar.

### 480 · Li+B2+Ho (MD)

- Tablero: K 11, 13 cuias (13 de 4), 1 vacío(s) (0 libre(s)); fuente `deep`.
- Mecánicas: brote n=2 (2 dosis); Ho key=6 (cuia de 4); Li key=2 (cuia de 4).
- Medida: par 25 (2K = 22; par − 2K = 3, media del pool 2.1), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 1.04, q 0.954 (banda hito), ramificación 5.88, trampas tempranas 0.
- Notas de la generación: 1 ronda(s) extra de candidatos; 2 ronda(s) de banda.
- Propuesta [P]: aceptar.

### 500 · Ma+Ha (MD)

- Tablero: K 11, 12 cuias (12 de 4), 1 vacío(s) (0 libre(s)); fuente `deep`.
- Mecánicas: hoja_atada key=10 (4 dosis); Ma (cuia de 4).
- Medida: par 25 (2K = 22; par − 2K = 3, media del pool 2.1), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 0.99, q 0.951 (banda hito), ramificación 3.08, trampas tempranas 4.
- Notas de la generación: 1 ronda(s) extra de candidatos.
- Propuesta [P]: aceptar.

### 520 · Ll+Ba+Ha (MD)

- Tablero: K 11, 12 cuias (12 de 4), 1 vacío(s) (0 libre(s)); fuente `deep`.
- Mecánicas: hoja_atada key=10 (4 dosis); Ll (cuia de 4); Barro: 1 dosis.
- Medida: par 27 (2K = 22; par − 2K = 5, media del pool 2.1), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 1.19, q 0.972 (banda hito), ramificación 4.30, trampas tempranas 0.
- Notas de la generación: 1 ronda(s) extra de candidatos.
- Propuesta [P]: aceptar.

### 540 · Pi+Ha (MD)

- Tablero: K 11, 13 cuias (12 de 4, 1 de 2), 1 vacío(s) (1 libre(s)); fuente `fill`.
- Mecánicas: Pi (cuia de 2); hoja_atada key=7 (4 dosis).
- Medida: par 33 (2K = 22; par − 2K = 11, media del pool 2.1), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 1.52, q 0.989 (banda hito), ramificación 4.21, trampas tempranas 6.
- Propuesta [P]: aceptar.

### 560 · Fl+Re (MD) «Riberalta» [P]

- Tablero: K 11, 12 cuias (12 de 4), 1 vacío(s) (1 libre(s)); fuente `deep`.
- Mecánicas: flor key=11 key2=4 (4 dosis); Re (cuia de 4); Re (cuia de 4).
- Medida: par 27 (2K = 22; par − 2K = 5, media del pool 2.1), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 1.09, q 0.960 (banda hito), ramificación 3.85, trampas tempranas 5.
- Notas de la generación: 1 ronda(s) extra de candidatos.
- Propuesta [P]: aceptar.

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
| 426 | 6 | reto | Fl+Ma (D) | flor key=7 key2=6 (4 dosis) | 2 | 2 | 0.775 |
| 431 | 11 | comodidad | B2 | brote n=2 (2 dosis) | 1 | 1 | 0.425 |
| 432 | 12 | reto | Fl+Ho (D) | flor key=13 key2=10 (4 dosis) | 2 | 2 | 0.800 |
| 437 | 17 | comodidad | Ha | hoja_atada key=9 (4 dosis) | 1 | 1 | 0.550 |
| 457 | 17 | comodidad | Ha | hoja_atada key=4 (4 dosis) | 1 | 1 | 0.300 |
| 465 | 5 | comodidad | B2 | brote n=2 (1 dosis) | 1 | 1 | 0.425 |
| 466 | 6 | reto | Li+B2 (D) | brote n=2 (2 dosis) | 2 | 1 | 0.675 |
| 469 | 9 | reto | Ln+B2 (D) | liana n=2 (4 dosis); brote n=2 (1 dosis) | 2 | 1 | 0.725 |
| 478 | 18 | reto | Ln+Ho (D) | liana n=1 (4 dosis) | 2 | 1 | 0.850 |
| 480 | 20 | hito | Li+B2+Ho (MD) | brote n=2 (2 dosis) | 3 | 1 | 1.000 |
| 485 | 5 | comodidad | Ha | hoja_atada key=6 (4 dosis) | 1 | 2 | 0.550 |
| 486 | 6 | reto | Ma+Ha (D) | hoja_atada key=5 (4 dosis) | 2 | 2 | 0.725 |
| 492 | 12 | reto | Ha+Ba (D) | hoja_atada key=4 (4 dosis) | 2 | 0 | 0.750 |
| 497 | 17 | comodidad | B2 | brote n=2 (1 dosis) | 1 | 1 | 0.375 |
| 498 | 18 | reto | Fl+Ha (D) | hoja_atada key=1 (4 dosis); flor key=13 key2=1 (4 dosis) | 2 | 2 | 0.775 |
| 500 | 20 | hito | Ma+Ha (MD) | hoja_atada key=10 (4 dosis) | 2 | 1 | 1.000 |
| 509 | 9 | reto | Fl (D) | flor key=13 key2=3 (4 dosis) | 1 | 1 | 0.850 |
| 512 | 12 | reto | Ll+Ha (D) | hoja_atada key=13 (4 dosis) | 2 | 2 | 0.850 |
| 517 | 17 | comodidad | B2 | brote n=2 (2 dosis) | 1 | 1 | 0.400 |
| 518 | 18 | reto | Fl+Ba (D) | flor key=1 key2=14 (4 dosis) | 2 | 1 | 0.850 |
| 520 | 20 | hito | Ll+Ba+Ha (MD) | hoja_atada key=10 (4 dosis) | 3 | 1 | 1.000 |
| 526 | 6 | reto | Pi+Ha (D) | hoja_atada key=1 (4 dosis) | 2 | 1 | 0.875 |
| 537 | 17 | comodidad | B2 | brote n=2 (1 dosis) | 1 | 1 | 0.275 |
| 538 | 18 | reto | Ha+Re (D) | hoja_atada key=4 (4 dosis) | 2 | 1 | 0.800 |
| 540 | 20 | hito | Pi+Ha (MD) | hoja_atada key=7 (4 dosis) | 2 | 1 | 1.000 |
| 543 | 3 | comodidad | Fl | flor key=4 key2=11 (4 dosis) | 1 | 2 | 0.475 |
| 546 | 6 | reto | Fl+Ho (D) | flor key=7 key2=12 (4 dosis) | 2 | 2 | 0.850 |
| 549 | 9 | reto | Ln+Ho (D) | liana n=1 (4 dosis) | 2 | 2 | 0.825 |
| 552 | 12 | reto | Ln+Re (D) | liana n=1 (4 dosis) | 2 | 1 | 0.850 |
| 558 | 18 | reto | Fl (D) | flor key=2 key2=13 (4 dosis) | 1 | 2 | 0.825 |
| 560 | 20 | hito | Fl+Re (MD) «Riberalta» [P] | flor key=11 key2=4 (4 dosis) | 2 | 1 | 1.000 |

Todas las celdas con una variante la llevan en el nivel publicado.

## Dónde va la Tinaja (variante a)

D-019 (Q-Ti1 A: nunca en recuperación, p1-p2, ni en remanso, p19; Q-Ti3 A: estreno en el 323, en la 1.0 si pasa sus pruebas y si Morpho con corte por tiempo y la Luciérnaga están listos en el cliente; si no, la salida B), la tabla de D2 (d.5.3, con el respaldo de cada celda en d.5.4; D-023 punto 10) y la propuesta de la tinaja d.3 (ciclo de estreno 321-340, «C0»: como mucho 8 apariciones). La variante a es la del dueño; la b, la salida B, se guarda aparte. Las 5 dosis de los retos y el hito dependen de la compuerta de coste de Morpho, con el factor 8× [P] del teléfono hasta medirlo en PF1 (pregunta 13): si PF1 lo cambia, este pack se regenera antes de integrarlo en el cliente (C1b).

| Nivel | p | Rol | Celda en la a | Respaldo en la b | Publicado en este pack | Dosis (ocultas) | K | Cuias |
|---|---|---|---|---|---|---|---|---|
| 454 | 14 | comodidad | Ti | C | Ti: con la Tinaja | 3 (2) | 9 | 12 |
| 494 | 14 | comodidad | Ti | C | Ti: con la Tinaja | 3 (2) | 9 | 12 |
| 531 | 11 | comodidad | Ti | C | Ti: con la Tinaja | 3 (2) | 9 | 12 |

Apariciones por ciclo (calendario de la a): 441-460: 1 (454); 481-500: 1 (494); 521-540: 1 (531).

- Tinaja solo donde la pone el calendario: sí.
- Nunca en p1, p2 ni p19: ninguno.

## Tinaja

Filtros de `TinajaWalks` sobre el tablero publicado (03 §8): ruta segura (`SafePath`), 20 paseos seguros (todos tienen que ganar), 30 paseos casuales (ganados / perdidos por error / de adivinar / sin terminar en 400 vertidos; ninguno puede ser de adivinar; el primer estado sin vertido seguro de cada paseo es «presupuesto agotado» si `SafeMoves` agota sus 20.000 nodos por realidad, sin consultar la realidad verdadera y nunca «adivinar»; si `SafeMoves` demostró que ningún vertido es seguro en todas las realidades, es un error del jugador si la verdadera no tiene solución, «adivinar» si la tiene y «presupuesto agotado» si A* agota sus 300.000 nodos en ella), «S0 sin trampas» en el estreno (vertidos útiles de S0 que no son seguros; tiene que ser 0: «trampa en S0» si alguno deja una realidad demostrada sin solución y, si no, «presupuesto agotado») y la compuerta de coste de Morpho: w = 2 y 40.000 nodos por realidad desde cero en S0, en la ruta y en los paseos, con un tope de 18.750 nodos por consulta (150 ms × 1.000 nodos/ms del PC [E] ÷ factor 8× del teléfono [P], hasta PF1). Todas las búsquedas van sin poda de fútiles. Dosis por tipo de nivel (D-019, Q-Ti1 A): 3 en estreno, comodidad y práctica; 4-5 en retos; 5 en el hito. Vacíos sin contar la pareja: 2 en los tableros de estreno y 1 en los demás (la comodidad fuera del estreno, 1 [P], pendiente de decisión). Como mucho 12 cuias con la pareja (02 §5) [P]: si no caben, se baja K.

| Nivel | p | Rol | Dosis (ocultas) | Realidades | Ruta segura | Paseos seguros | Casuales (g / p / a / s) | S0 sin trampas | Morpho: estados, nodos máx. | Candidatos rechazados por la tinaja |
|---|---|---|---|---|---|---|---|---|---|---|
| 454 | 14 | comodidad | 3 (2) | 2 | 19 vertidos | 20/20 | 14 / 16 / 0 / 0 | — | 435, 420 | — |
| 494 | 14 | comodidad | 3 (2) | 2 | 20 vertidos | 20/20 | 22 / 8 / 0 / 0 | — | 656, 677 | — |
| 531 | 11 | comodidad | 3 (2) | 2 | 21 vertidos | 20/20 | 23 / 7 / 0 / 0 | — | 901, 805 | — |

Ningún nivel con tinaja en recuperación (p1-p2), remanso (p19) ni la rampa (D-019, Q-Ti1 A).

Alivio (03 §13): `minus_color` nunca quita una fruta de la tinaja y las variantes repiten la ruta segura y los paseos (con `TinajaWalks.ReliefOptions`, las de Morpho: w = 2 y 40.000 nodos por realidad [P], pendiente de decisión) con la máscara de la tinaja, que el pack escribe en su `hidden` (`pack_schema` 3; 08 §4.1: `relief_board` admite `hidden` si y solo si su `attrs` contiene la tinaja). `plus_empty` no se publica si pasa de 13 cuias: el tope de 12 del nivel (02 §5) más su cuia vacía (D-027).

## Otros niveles con avisos

| Nivel | Rol | Calendario | Aviso |
|---|---|---|---|
| 433 | comodidad | C | risk_adjacent: 1 contacto(s) |
| 436 | comodidad | C | risk_adjacent: 1 contacto(s) |
| 477 | comodidad | Ba | risk_adjacent: 1 contacto(s) |
| 551 | comodidad | Ll | risk_adjacent: 1 contacto(s) |

## Curación del dueño

Estado: **pendiente** (0 de 42 niveles decididos) (09 §0.1 y §0.3). La decisión por nivel es «aceptar», «sustituir» (otro candidato del slot o del pool, 03 §7) o «cambiar la celda», con el motivo en los dos últimos casos. No se escribe en este informe, que es generado: va en `tools/level-generator/specs/curation_v2.json`, una entrada por nivel con el literal de la celda que vio el dueño, y la última columna la copia de ahí; si la celda cambia, la decisión deja de valer y el nivel vuelve a «pendiente». Si la curación pide cambios, se cambian la especificación o el generador y se regeneran el pack, este informe y la comprobación de `level-qa`.

| Nivel | Rol | Calendario | Etiqueta / objetivo | Propuesta [P] | Decisión del dueño |
|---|---|---|---|---|---|
| 426 | reto | Fl+Ma (D) | Difícil / Difícil | aceptar | pendiente |
| 429 | reto | Ma+Re (D) | Normal / Difícil | revisar | pendiente |
| 432 | reto | Fl+Ho (D) | Difícil / Difícil | aceptar | pendiente |
| 435 | reto | Ma+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 438 | reto | Ho+Re (D) | Difícil / Difícil | aceptar | pendiente |
| 440 | hito | Ma+Ho (MD) | Muy difícil / Muy difícil | aceptar | pendiente |
| 446 | reto | Pi+Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 449 | reto | Br+Re (D) | Difícil / Difícil | aceptar | pendiente |
| 452 | reto | Pi+Br (D) | Normal / Difícil | revisar | pendiente |
| 455 | reto | Br+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 458 | reto | Ba+Re (D) | Difícil / Difícil | aceptar | pendiente |
| 460 | hito | Pi+Ba (MD) | Muy difícil / Muy difícil | aceptar | pendiente |
| 466 | reto | Li+B2 (D) | Normal / Difícil | revisar | pendiente |
| 469 | reto | Ln+B2 (D) | Normal / Difícil | revisar | pendiente |
| 472 | reto | Li+Ho (D) | Difícil / Difícil | aceptar | pendiente |
| 475 | reto | Ho+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 478 | reto | Ln+Ho (D) | Difícil / Difícil | aceptar | pendiente |
| 480 | hito | Li+B2+Ho (MD) | Muy difícil / Muy difícil | aceptar | pendiente |
| 486 | reto | Ma+Ha (D) | Normal / Difícil | revisar | pendiente |
| 489 | reto | Ma+Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 492 | reto | Ha+Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 495 | reto | Ma+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 498 | reto | Fl+Ha (D) | Difícil / Difícil | aceptar | pendiente |
| 500 | hito | Ma+Ha (MD) | Muy difícil / Muy difícil | aceptar | pendiente |
| 506 | reto | Ll+Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 509 | reto | Fl (D) | Difícil / Difícil | aceptar | pendiente |
| 512 | reto | Ll+Ha (D) | Difícil / Difícil | aceptar | pendiente |
| 515 | reto | Ba+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 518 | reto | Fl+Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 520 | hito | Ll+Ba+Ha (MD) | Muy difícil / Muy difícil | aceptar | pendiente |
| 526 | reto | Pi+Ha (D) | Difícil / Difícil | aceptar | pendiente |
| 529 | reto | Ho+Re (D) | Difícil / Difícil | aceptar | pendiente |
| 532 | reto | Pi+Ho (D) | Difícil / Difícil | aceptar | pendiente |
| 535 | reto | Ho+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 538 | reto | Ha+Re (D) | Difícil / Difícil | aceptar | pendiente |
| 540 | hito | Pi+Ha (MD) | Muy difícil / Muy difícil | aceptar | pendiente |
| 546 | reto | Fl+Ho (D) | Difícil / Difícil | aceptar | pendiente |
| 549 | reto | Ln+Ho (D) | Difícil / Difícil | aceptar | pendiente |
| 552 | reto | Ln+Re (D) | Difícil / Difícil | aceptar | pendiente |
| 555 | reto | Ho+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 558 | reto | Fl (D) | Difícil / Difícil | aceptar | pendiente |
| 560 | hito | Fl+Re (MD) «Riberalta» [P] | Muy difícil / Muy difícil | aceptar | pendiente |
