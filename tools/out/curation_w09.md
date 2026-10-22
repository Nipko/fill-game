# Curación de 561-700 (variante b): retos, hitos y estrenos para el dueño

Generado por `dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/campaign_v2.json --levels 561-700 --variant b --out Assets/_Project/Data/Levels --curation tools/out/curation_w09.md --decisions tools/level-generator/specs/curation_v2.json` con los mismos niveles colocados que el pack: no se edita a mano y no lleva tiempos (la decisión del dueño sale de `tools/level-generator/specs/curation_v2.json`). Niveles del mundo 9 (Ucayali, 561-700).

Fuentes: 09 §0.1 (fila de level-pipeline-dev: el dueño aprueba la curación de retos e hitos de cada paquete) y §0.3 (propuesta de curación de los 30 retos e hitos que aprueba el dueño), 03 §7 (un reto por debajo de 0,75 se publica sin etiqueta y la curación decide si se sustituye por otro del pool del slot; banda por el cuantil q de D en el pool del rango), D-009 (etiqueta por el bot ×40 con la semilla del nivel: Difícil ≥ 0,75 y Muy difícil ≥ 0,90; retos en p6, p9, p12, p15 y p18 e hito en p20; c.2: un reto o el hito fuera de su ventana se publica con la etiqueta que mide), 03 §11-§12, 02 §9.2 regla 2 (estrenos) y la tabla de novedades de D2 (d.5.3; D-023 punto 10). La columna «Propuesta» es [P]: la decide el dueño.

Compuerta automática (verde antes de pedir la curación): `dotnet run --project tools/level-qa -c Release -- Assets/_Project/Data/Levels/mundo_09_v1.json --spec tools/level-generator/specs/campaign_v2.json --levels 561-700 --variant b`. Cada reto o hito con aviso QA-WINDOW (bot_fail fuera de la ventana del slot) sale aquí como «revisar», y cada hito con aviso QA-BAND-Q (fuera de la banda de hito con el mínimo cumplido), como «revisar» o con nota de banda; los avisos QA-BAND-Q de las recuperaciones no entran en este informe. QA-BAND-Q no mira los retos y level-qa no mira risk_adjacent: las notas de banda de los retos y los «revisar» por risk_adjacent salen solo de este informe.

## Resumen

| Pack | Niveles | `pack_schema` | Bytes | SHA-256 |
|---|---|---|---|---|
| `Assets/_Project/Data/Levels/mundo_09_v1.json` | 140 | 3 | 202709 | `a40f04d5a6d4b8ac50b5f1e5ed8a22a0d44dba805d8902f01c415a762e7ead4e` |

- Retos e hitos: 42 (35 retos y 7 hitos); 34 miden la etiqueta objetivo o más (D-009) y 8 no.
- No miden su etiqueta objetivo y se publican con la que miden (c.2; 03 §7): 566 (reto Ma+Re (D): Normal, bot_fail 0.700), 569 (reto B2+Ba (D): Normal, bot_fail 0.675), 592 (reto Pi+B2 (D): Normal, bot_fail 0.425), 615 (reto Li+Re (D): Normal, bot_fail 0.650), 646 (reto Pi+Br+Ha (D): Normal, bot_fail 0.550; primer reto con tres mecánicas, d.5.2; 03 §11), 658 (reto Ha+Re (D): Normal, bot_fail 0.675), 686 (reto Ma+Ho (D): Normal, bot_fail 0.625), 692 (reto Ma+Ha (D): Normal, bot_fail 0.725).
- Etiquetas publicadas en 561-700: 27 Difícil y 7 Muy difícil de 140 niveles (03 §11: 25 % y 5 %).
- Para revisar: 566 (reto), 569 (reto), 586 (reto), 589 (reto), 592 (reto), 598 (reto), 606 (reto), 609 (reto), 612 (reto), 615 (reto), 626 (reto), 629 (reto), 632 (reto), 638 (reto), 646 (reto), 652 (reto), 658 (reto), 660 (hito), 675 (reto), 678 (reto), 686 (reto), 689 (reto), 692 (reto), 695 (reto), 698 (reto). El resto, aceptar.

## Retos e hitos

Etiqueta = la que mide el bot ×40 y se publica (D-009); Objetivo = la de la celda (D) o (MD). q = cuantil de D en el pool del rango (banda: recuperación ≤ 0,10, comodidad ≤ 0,70, reto ≤ 0,95, hito > 0,95). «Otros del slot» = candidatos medidos del mismo slot, sin rechazo, que miden la etiqueta objetivo o más: con ellos se puede sustituir el nivel al regenerar (03 §7), si la escalera del ciclo lo admite.

| Nivel | p | Rol | Calendario | K | Cuias | Vacíos (libres) | Par | bot_fail | Ventana | Etiqueta | Objetivo | D | q (banda) | Otros del slot | Propuesta [P] |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 566 | 6 | reto | Ma+Re (D) | 10 | 12 | 2 (0) | 20 | 0.700 | [0.75, 0.90) | Normal | Difícil | 0.03 | 0.563 (comodidad) | 3 | **revisar**: mide Normal (bot_fail 0.700) y apunta a Difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir |
| 569 | 9 | reto | B2+Ba (D) | 10 | 12 | 1 (1) | 22 | 0.675 | [0.75, 0.90) | Normal | Difícil | 0.22 | 0.706 (reto) | 4 | **revisar**: mide Normal (bot_fail 0.675) y apunta a Difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir; cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 572 | 12 | reto | Ma+Ba (D) | 10 | 12 | 1 (0) | 22 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 0.25 | 0.724 (reto) | 5 | aceptar |
| 575 | 15 | reto | Ma+Tu (D) | 10 | 12 | 1 (0) | 24 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.45 | 0.817 (reto) | 5 | aceptar (turbia: 2 dosis ocultas en 1 cuia(s), 1 realidad(es) compatibles con lo visible) |
| 578 | 18 | reto | B2+Re (D) | 10 | 12 | 1 (1) | 24 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.60 | 0.867 (reto) | 1 | aceptar |
| 580 | 20 | hito | Ma+B2 (MD) | 12 | 14 | 1 (0) | 29 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.19 | 0.975 (hito) | 5 | aceptar |
| 586 | 6 | reto | Pi+Ha (D) | 10 | 12 | 1 (1) | 22 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.34 | 0.775 (reto) | 15 | **revisar**: cvd_adjacent: 3 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 589 | 9 | reto | Ha+B2 (D) | 10 | 12 | 1 (1) | 22 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.37 | 0.787 (reto) | 15 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 592 | 12 | reto | Pi+B2 (D) | 10 | 13 | 1 (1) | 22 | 0.425 | [0.75, 0.90) | Normal | Difícil | 0.40 | 0.795 (reto) | 0 | **revisar**: mide Normal (bot_fail 0.425) y apunta a Difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir; ningún otro candidato del slot mide la etiqueta objetivo: sustituirlo pide más candidatos (otra ronda del generador) o cambiar la celda |
| 595 | 15 | reto | Ba+Tu (D) | 10 | 12 | 1 (1) | 24 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.56 | 0.848 (reto) | 3 | aceptar (turbia: 2 dosis ocultas en 1 cuia(s), 1 realidad(es) compatibles con lo visible) |
| 598 | 18 | reto | Pi+Ba (D) | 10 | 12 | 0 (0) | 24 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.66 | 0.884 (reto) | 17 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 600 | 20 | hito | Ha+B2+Ba (MD) | 12 | 14 | 1 (1) | 27 | 0.950 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.11 | 0.968 (hito) | 17 | aceptar |
| 606 | 6 | reto | Ln+Re (D) | 10 | 11 | 1 (0) | 20 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 0.29 | 0.743 (reto) | 6 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 609 | 9 | reto | Li+Ll (D) | 10 | 12 | 2 (1) | 21 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.33 | 0.770 (reto) | 17 | **revisar**: cvd_adjacent: 4 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 612 | 12 | reto | Ll+Re (D) | 10 | 11 | 1 (0) | 20 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.43 | 0.809 (reto) | 5 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 615 | 15 | reto | Li+Re (D) | 10 | 12 | 2 (2) | 31 | 0.650 | [0.75, 0.90) | Normal | Difícil | 1.17 | 0.974 (hito) | 6 | **revisar**: mide Normal (bot_fail 0.650) y apunta a Difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir |
| 618 | 18 | reto | Ln+Ll (D) | 10 | 13 | 3 (2) | 30 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 1.27 | 0.979 (hito) | 16 | aceptar |
| 620 | 20 | hito | Ln+Ll+Re (MD; K = 13) | 13 | 14 | 1 (0) | 31 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.53 | 0.990 (hito) | 29 | aceptar |
| 626 | 6 | reto | Ma+Ba (D) | 10 | 12 | 1 (0) | 22 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.26 | 0.730 (reto) | 5 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 629 | 9 | reto | Ll+Ma (D) | 10 | 12 | 2 (0) | 25 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 0.40 | 0.797 (reto) | 5 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 632 | 12 | reto | Ma+Ho (D) | 10 | 12 | 2 (0) | 24 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.57 | 0.858 (reto) | 3 | **revisar**: risk_adjacent: 1 contacto(s) de tintes de un par de riesgo en una cuia (03 §4.5) |
| 635 | 15 | reto | Ma+Tu (D) | 10 | 12 | 1 (0) | 24 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 0.72 | 0.907 (reto) | 5 | aceptar (turbia: 3 dosis ocultas en 2 cuia(s), 6 realidad(es) compatibles con lo visible) |
| 638 | 18 | reto | Ba+Ho (D) | 10 | 13 | 2 (1) | 28 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 0.89 | 0.940 (reto) | 5 | **revisar**: cvd_adjacent: 2 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 640 | 20 | hito | Ma+Ba (MD) | 12 | 14 | 1 (0) | 27 | 0.975 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.04 | 0.957 (hito) | 5 | aceptar |
| 646 | 6 | reto | Pi+Br+Ha (D) | 10 | 13 | 1 (1) | 20 | 0.550 | [0.75, 0.90) | Normal | Difícil | 0.14 | 0.663 (comodidad) | 0 | **revisar**: mide Normal (bot_fail 0.550) y apunta a Difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir; ningún otro candidato del slot mide la etiqueta objetivo: sustituirlo pide más candidatos (otra ronda del generador) o cambiar la celda; cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3); primer reto con tres mecánicas, d.5.2; 03 §11 |
| 649 | 9 | reto | Pi+Ha (D) | 10 | 12 | 1 (1) | 22 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.30 | 0.756 (reto) | 4 | aceptar |
| 652 | 12 | reto | Br+Re (D) | 10 | 12 | 1 (1) | 22 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.36 | 0.783 (reto) | 0 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 655 | 15 | reto | Br+Tu (D) | 10 | 11 | 0 (0) | 22 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.41 | 0.801 (reto) | 5 | aceptar (turbia: 2 dosis ocultas en 1 cuia(s), 1 realidad(es) compatibles con lo visible) |
| 658 | 18 | reto | Ha+Re (D) | 10 | 12 | 2 (2) | 29 | 0.675 | [0.75, 0.90) | Normal | Difícil | 1.00 | 0.957 (hito) | 0 | **revisar**: mide Normal (bot_fail 0.675) y apunta a Difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir; ningún otro candidato del slot mide la etiqueta objetivo: sustituirlo pide más candidatos (otra ronda del generador) o cambiar la celda; cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 660 | 20 | hito | Br+Ha+Re (MD) | 12 | 14 | 1 (1) | 29 | 0.975 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.21 | 0.977 (hito) | 6 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 666 | 6 | reto | Li+Ha (D) | 10 | 11 | 1 (1) | 22 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 0.15 | 0.665 (comodidad) | 1 | aceptar (Difícil por el bot, con D en banda comodidad (q 0.665)) |
| 669 | 9 | reto | Li+Br (D) | 10 | 12 | 1 (1) | 25 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.73 | 0.916 (reto) | 14 | aceptar |
| 672 | 12 | reto | Li+Re (D) | 10 | 12 | 2 (2) | 31 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 1.07 | 0.964 (hito) | 6 | aceptar |
| 675 | 15 | reto | Br+Tu (D) | 10 | 12 | 1 (1) | 26 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 1.12 | 0.970 (hito) | 17 | **revisar**: cvd_adjacent: 2 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) (turbia: 1 dosis ocultas en 1 cuia(s), 1 realidad(es) compatibles con lo visible) |
| 678 | 18 | reto | Ha+Re (D) | 10 | 12 | 2 (2) | 28 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 1.14 | 0.972 (hito) | 5 | **revisar**: cvd_adjacent: 2 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 680 | 20 | hito | Li+Ha+Re (MD; K = 13) | 13 | 14 | 1 (1) | 30 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.21 | 0.976 (hito) | 17 | aceptar |
| 686 | 6 | reto | Ma+Ho (D) | 10 | 12 | 2 (0) | 25 | 0.625 | [0.75, 0.90) | Normal | Difícil | 0.25 | 0.723 (reto) | 3 | **revisar**: mide Normal (bot_fail 0.625) y apunta a Difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir; cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 689 | 9 | reto | Ho+B2 (D) | 10 | 12 | 1 (0) | 24 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.32 | 0.768 (reto) | 1 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 692 | 12 | reto | Ma+Ha (D) | 10 | 12 | 2 (1) | 23 | 0.725 | [0.75, 0.90) | Normal | Difícil | 0.54 | 0.843 (reto) | 7 | **revisar**: mide Normal (bot_fail 0.725) y apunta a Difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir |
| 695 | 15 | reto | Ho+Tu (D) | 10 | 12 | 1 (0) | 24 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.57 | 0.853 (reto) | 5 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) (turbia: 2 dosis ocultas en 1 cuia(s), 1 realidad(es) compatibles con lo visible) |
| 698 | 18 | reto | Ha+B2 (D) | 10 | 12 | 1 (1) | 22 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 0.70 | 0.898 (reto) | 3 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 700 | 20 | hito | Ma+Ha+B2 (MD) «Yarinacocha» [P] | 12 | 14 | 1 (0) | 28 | 0.975 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.06 | 0.961 (hito) | 2 | aceptar |

## Hitos

p20 de cada ciclo («Pico» de c.3): Muy difícil por el bot (≥ 0,90; D-009) y, por D, en banda de hito (q > 0,95; 03 §7) o, como mínimo, con par − 2K ≥ la media del pool del rango (criterio [P] del generador y de level-qa, revisión del 2026-10-09; README del generador; QA-BAND-Q). Los nombres de hito de la celda son [P] y pasan por el asesor (D-023 punto 21): no van al pack.

### 580 · Ma+B2 (MD)

- Tablero: K 12, 14 cuias (14 de 4), 1 vacío(s) (0 libre(s)); fuente `deep`.
- Mecánicas: brote n=2 (2 dosis); Ma (cuia de 4).
- Medida: par 29 (2K = 24; par − 2K = 5, media del pool 1.9), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 1.19, q 0.975 (banda hito), ramificación 4.55, trampas tempranas 0.
- Notas de la generación: endurecimiento: deep.
- Propuesta [P]: aceptar.

### 600 · Ha+B2+Ba (MD)

- Tablero: K 12, 14 cuias (14 de 4), 1 vacío(s) (1 libre(s)); fuente `deep`.
- Mecánicas: brote n=2 (2 dosis); hoja_atada key=7 (4 dosis); Barro: 1 dosis.
- Medida: par 27 (2K = 24; par − 2K = 3, media del pool 1.9), bot_fail 0.950 → Muy difícil (objetivo Muy difícil), D 1.11, q 0.968 (banda hito), ramificación 7.04, trampas tempranas 0.
- Notas de la generación: 2 ronda(s) extra de candidatos.
- Propuesta [P]: aceptar.

### 620 · Ln+Ll+Re (MD; K = 13)

- Tablero: K 13, 14 cuias (14 de 4), 1 vacío(s) (0 libre(s)); fuente `deep`.
- Mecánicas: Re (cuia de 4); liana n=1 (4 dosis); Ll (cuia de 4); Re (cuia de 4).
- Medida: par 31 (2K = 26; par − 2K = 5, media del pool 1.9), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 1.53, q 0.990 (banda hito), ramificación 7.03, trampas tempranas 0.
- Notas de la generación: hito de 14 cuias: K y vacio fijos (QA-HITO-14); 2 ronda(s) extra de candidatos; 1 ronda(s) de banda.
- Propuesta [P]: aceptar.

### 640 · Ma+Ba (MD)

- Tablero: K 12, 14 cuias (14 de 4), 1 vacío(s) (0 libre(s)); fuente `deep`.
- Mecánicas: Ma (cuia de 4); Barro: 1 dosis.
- Medida: par 27 (2K = 24; par − 2K = 3, media del pool 1.9), bot_fail 0.975 → Muy difícil (objetivo Muy difícil), D 1.04, q 0.957 (banda hito), ramificación 5.00, trampas tempranas 0.
- Notas de la generación: relajacion: deep con 60.000 nodos.
- Propuesta [P]: aceptar.

### 660 · Br+Ha+Re (MD)

- Tablero: K 12, 14 cuias (14 de 4), 1 vacío(s) (1 libre(s)); fuente `deep`.
- Mecánicas: Br (cuia de 4); Re (cuia de 4); hoja_atada key=10 (3 dosis); Re (cuia de 4).
- Medida: par 29 (2K = 24; par − 2K = 5, media del pool 1.9), bot_fail 0.975 → Muy difícil (objetivo Muy difícil), D 1.21, q 0.977 (banda hito), ramificación 7.24, trampas tempranas 0.
- Notas de la generación: 2 ronda(s) de banda.
- Propuesta [P]: **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3).

### 680 · Li+Ha+Re (MD; K = 13)

- Tablero: K 13, 14 cuias (14 de 4), 1 vacío(s) (1 libre(s)); fuente `deep`.
- Mecánicas: Li key=7 (cuia de 4); Re (cuia de 4); hoja_atada key=14 (4 dosis); Re (cuia de 4).
- Medida: par 30 (2K = 26; par − 2K = 4, media del pool 1.9), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 1.21, q 0.976 (banda hito), ramificación 5.00, trampas tempranas 4.
- Notas de la generación: 2 ronda(s) extra de candidatos.
- Propuesta [P]: aceptar.

### 700 · Ma+Ha+B2 (MD) «Yarinacocha» [P]

- Tablero: K 12, 14 cuias (14 de 4), 1 vacío(s) (0 libre(s)); fuente `deep`.
- Mecánicas: brote n=2 (2 dosis); Ma (cuia de 4); hoja_atada key=3 (4 dosis).
- Medida: par 28 (2K = 24; par − 2K = 4, media del pool 1.9), bot_fail 0.975 → Muy difícil (objetivo Muy difícil), D 1.06, q 0.961 (banda hito), ramificación 5.21, trampas tempranas 0.
- Propuesta [P]: aceptar.

## Variantes de la 1.0

Variantes de 02 §9.1 (propuesta de D-007 b.3; firma de `pack_schema` 3, D-023 punto 5) con los códigos de d.5.1 [P]: Ln = Liana de cuenta (`liana` con `n`; n = 1 en el estreno y 1-2 en los demás [P]), Ha = Hoja atada (`hoja_atada`, sobre una cuia con su fruta en el fondo y cap = U_c, D-024), B2 = Brote de dos hojas (`brote` con `n` = 2, 1-2 dosis) y Fl = Flor de vitória-régia (`flor` con `key` y `key2` distintas). Estreno (02 §9.2 regla 2): un ejemplar, 2 vacíos y `bot_fail` < 0,20.

| Variante | Debut | Niveles | Ejemplares | Tableros de estreno | Estrenos con un ejemplar, 2 vacíos y bot < 0,20 |
|---|---|---|---|---|---|
| Ln | 243 (`liana_n`) | 4 | 4 | 0 | 0 de 0 |
| Ha | 283 (`hoja_atada`) | 17 | 17 | 0 | 0 de 0 |
| B2 | 343 (`brote_n2`) | 13 | 13 | 0 | 0 de 0 |
| Fl | 403 (`flor`) | 1 | 1 | 0 | 0 de 0 |

| Nivel | p | Rol | Celda | Variante | Ejemplares de mecánica | Vacíos | bot_fail |
|---|---|---|---|---|---|---|---|
| 565 | 5 | comodidad | B2 | brote n=2 (1 dosis) | 1 | 1 | 0.250 |
| 568 | 8 | comodidad | Ha | hoja_atada key=9 (4 dosis) | 1 | 1 | 0.575 |
| 569 | 9 | reto | B2+Ba (D) | brote n=2 (1 dosis) | 2 | 1 | 0.675 |
| 578 | 18 | reto | B2+Re (D) | brote n=2 (1 dosis) | 2 | 1 | 0.775 |
| 580 | 20 | hito | Ma+B2 (MD) | brote n=2 (2 dosis) | 2 | 1 | 1.000 |
| 585 | 5 | comodidad | Ha | hoja_atada key=7 (4 dosis) | 1 | 1 | 0.475 |
| 586 | 6 | reto | Pi+Ha (D) | hoja_atada key=1 (4 dosis) | 2 | 1 | 0.850 |
| 589 | 9 | reto | Ha+B2 (D) | hoja_atada key=11 (4 dosis); brote n=2 (1 dosis) | 2 | 1 | 0.875 |
| 592 | 12 | reto | Pi+B2 (D) | brote n=2 (1 dosis) | 2 | 1 | 0.425 |
| 600 | 20 | hito | Ha+B2+Ba (MD) | brote n=2 (2 dosis); hoja_atada key=7 (4 dosis) | 3 | 1 | 0.950 |
| 603 | 3 | comodidad | Ln | liana n=2 (4 dosis) | 1 | 2 | 0.250 |
| 606 | 6 | reto | Ln+Re (D) | liana n=2 (4 dosis) | 2 | 1 | 0.825 |
| 617 | 17 | comodidad | Fl | flor key=1 key2=13 (4 dosis) | 1 | 2 | 0.575 |
| 618 | 18 | reto | Ln+Ll (D) | liana n=2 (4 dosis) | 2 | 3 | 0.875 |
| 620 | 20 | hito | Ln+Ll+Re (MD; K = 13) | liana n=1 (4 dosis) | 3 | 1 | 1.000 |
| 628 | 8 | comodidad | B2 | brote n=2 (1 dosis) | 1 | 1 | 0.200 |
| 637 | 17 | comodidad | Ha | hoja_atada key=2 (4 dosis) | 1 | 1 | 0.450 |
| 646 | 6 | reto | Pi+Br+Ha (D) | hoja_atada key=13 (4 dosis) | 3 | 1 | 0.550 |
| 649 | 9 | reto | Pi+Ha (D) | hoja_atada key=4 (4 dosis) | 2 | 1 | 0.850 |
| 657 | 17 | comodidad | B2 | brote n=2 (1 dosis) | 1 | 1 | 0.350 |
| 658 | 18 | reto | Ha+Re (D) | hoja_atada key=3 (4 dosis) | 2 | 2 | 0.675 |
| 660 | 20 | hito | Br+Ha+Re (MD) | hoja_atada key=10 (3 dosis) | 3 | 1 | 0.975 |
| 665 | 5 | comodidad | Ha | hoja_atada key=8 (4 dosis) | 1 | 1 | 0.450 |
| 666 | 6 | reto | Li+Ha (D) | hoja_atada key=13 (4 dosis) | 2 | 1 | 0.750 |
| 677 | 17 | comodidad | B2 | brote n=2 (1 dosis) | 1 | 1 | 0.375 |
| 678 | 18 | reto | Ha+Re (D) | hoja_atada key=8 (4 dosis) | 2 | 2 | 0.750 |
| 680 | 20 | hito | Li+Ha+Re (MD; K = 13) | hoja_atada key=14 (4 dosis) | 3 | 1 | 1.000 |
| 689 | 9 | reto | Ho+B2 (D) | brote n=2 (1 dosis) | 2 | 1 | 0.850 |
| 692 | 12 | reto | Ma+Ha (D) | hoja_atada key=6 (4 dosis) | 2 | 2 | 0.725 |
| 698 | 18 | reto | Ha+B2 (D) | hoja_atada key=14 (4 dosis); brote n=2 (2 dosis) | 2 | 1 | 0.750 |
| 700 | 20 | hito | Ma+Ha+B2 (MD) «Yarinacocha» [P] | brote n=2 (2 dosis); hoja_atada key=3 (4 dosis) | 3 | 1 | 0.975 |

Todas las celdas con una variante la llevan en el nivel publicado.

## Dónde va la Tinaja (variante b)

D-019 (Q-Ti1 A: nunca en recuperación, p1-p2, ni en remanso, p19; Q-Ti3 A: estreno en el 323, en la 1.0 si pasa sus pruebas y si Morpho con corte por tiempo y la Luciérnaga están listos en el cliente; si no, la salida B), la tabla de D2 (d.5.3, con el respaldo de cada celda en d.5.4; D-023 punto 10) y la propuesta de la tinaja d.3 (ciclo de estreno 321-340, «C0»: como mucho 8 apariciones). La variante a es la del dueño; la b, la salida B, se guarda aparte. Las 5 dosis de los retos y el hito dependen de la compuerta de coste de Morpho, con el factor 8× [P] del teléfono hasta medirlo en PF1 (pregunta 13): si PF1 lo cambia, este pack se regenera antes de integrarlo en el cliente (C1b).

| Nivel | p | Rol | Celda en la a | Respaldo en la b | Publicado en este pack | Dosis (ocultas) | K | Cuias |
|---|---|---|---|---|---|---|---|---|
| 574 | 14 | comodidad | Ti | C | C: sin la Tinaja | — | 11 | 13 |
| 591 | 11 | comodidad | Ti | C | C: sin la Tinaja | — | 11 | 13 |
| 631 | 11 | comodidad | Ti | C | C: sin la Tinaja | — | 11 | 13 |
| 651 | 11 | comodidad | Ti | C | C: sin la Tinaja | — | 11 | 13 |
| 691 | 11 | comodidad | Ti | C | C: sin la Tinaja | — | 11 | 13 |

Apariciones por ciclo (calendario de la a): 561-580: 1 (574); 581-600: 1 (591); 621-640: 1 (631); 641-660: 1 (651); 681-700: 1 (691).

- Tinaja solo donde la pone el calendario: sí.
- Nunca en p1, p2 ni p19: ninguno.
- Variante b: 0 niveles con la Tinaja (la salida B no la lleva).

## Otros niveles con avisos

| Nivel | Rol | Calendario | Aviso |
|---|---|---|---|
| 562 | recuperación | R | cvd_adjacent: 1 contacto(s) |
| 563 | comodidad | Ma | cvd_adjacent: 1 contacto(s) |
| 568 | comodidad | Ha | cvd_adjacent: 3 contacto(s) |
| 571 | comodidad | Ho | cvd_adjacent: 1 contacto(s) |
| 581 | recuperación | R | cvd_adjacent: 1 contacto(s) |
| 584 | comodidad | C | cvd_adjacent: 1 contacto(s) |
| 590 | comodidad | C | cvd_adjacent: 1 contacto(s) |
| 596 | comodidad | C | cvd_adjacent: 1 contacto(s) |
| 604 | comodidad | C | cvd_adjacent: 3 contacto(s) |
| 607 | comodidad | C | cvd_adjacent: 1 contacto(s) |
| 608 | comodidad | Ho | cvd_adjacent: 1 contacto(s) |
| 610 | comodidad | C | cvd_adjacent: 1 contacto(s) |
| 611 | comodidad | Br | cvd_adjacent: 1 contacto(s) |
| 633 | comodidad | C | cvd_adjacent: 1 contacto(s) |
| 641 | recuperación | R | cvd_adjacent: 1 contacto(s) |
| 643 | comodidad | Pi | cvd_adjacent: 1 contacto(s) |
| 645 | comodidad | Br | cvd_adjacent: 1 contacto(s) |
| 654 | comodidad | Pi | cvd_adjacent: 2 contacto(s) |
| 659 | remanso | S | cvd_adjacent: 2 contacto(s) |
| 667 | comodidad | C | cvd_adjacent: 1 contacto(s); tras una etiqueta no baja una banda entera de D (solo baja D) |
| 668 | comodidad | Ho | cvd_adjacent: 2 contacto(s) |
| 673 | comodidad | C | risk_adjacent: 1 contacto(s) |
| 676 | comodidad | C | cvd_adjacent: 2 contacto(s) |
| 679 | remanso | S | cvd_adjacent: 2 contacto(s) |
| 684 | comodidad | C | cvd_adjacent: 2 contacto(s) |
| 685 | comodidad | Ho | cvd_adjacent: 1 contacto(s) |
| 691 | comodidad | C | cvd_adjacent: 1 contacto(s) |
| 694 | comodidad | Ma+Ch | cvd_adjacent: 1 contacto(s) |
| 696 | comodidad | C | cvd_adjacent: 1 contacto(s) |
| 699 | remanso | S | cvd_adjacent: 1 contacto(s) |

## Curación del dueño

Estado: **pendiente** (0 de 42 niveles decididos) (09 §0.1 y §0.3). La decisión por nivel es «aceptar», «sustituir» (otro candidato del slot o del pool, 03 §7) o «cambiar la celda», con el motivo en los dos últimos casos. No se escribe en este informe, que es generado: va en `tools/level-generator/specs/curation_v2.json`, una entrada por nivel con el literal de la celda que vio el dueño, y la última columna la copia de ahí; si la celda cambia, la decisión deja de valer y el nivel vuelve a «pendiente». Si la curación pide cambios, se cambian la especificación o el generador y se regeneran el pack, este informe y la comprobación de `level-qa`.

| Nivel | Rol | Calendario | Etiqueta / objetivo | Propuesta [P] | Decisión del dueño |
|---|---|---|---|---|---|
| 566 | reto | Ma+Re (D) | Normal / Difícil | revisar | pendiente |
| 569 | reto | B2+Ba (D) | Normal / Difícil | revisar | pendiente |
| 572 | reto | Ma+Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 575 | reto | Ma+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 578 | reto | B2+Re (D) | Difícil / Difícil | aceptar | pendiente |
| 580 | hito | Ma+B2 (MD) | Muy difícil / Muy difícil | aceptar | pendiente |
| 586 | reto | Pi+Ha (D) | Difícil / Difícil | revisar | pendiente |
| 589 | reto | Ha+B2 (D) | Difícil / Difícil | revisar | pendiente |
| 592 | reto | Pi+B2 (D) | Normal / Difícil | revisar | pendiente |
| 595 | reto | Ba+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 598 | reto | Pi+Ba (D) | Difícil / Difícil | revisar | pendiente |
| 600 | hito | Ha+B2+Ba (MD) | Muy difícil / Muy difícil | aceptar | pendiente |
| 606 | reto | Ln+Re (D) | Difícil / Difícil | revisar | pendiente |
| 609 | reto | Li+Ll (D) | Difícil / Difícil | revisar | pendiente |
| 612 | reto | Ll+Re (D) | Difícil / Difícil | revisar | pendiente |
| 615 | reto | Li+Re (D) | Normal / Difícil | revisar | pendiente |
| 618 | reto | Ln+Ll (D) | Difícil / Difícil | aceptar | pendiente |
| 620 | hito | Ln+Ll+Re (MD; K = 13) | Muy difícil / Muy difícil | aceptar | pendiente |
| 626 | reto | Ma+Ba (D) | Difícil / Difícil | revisar | pendiente |
| 629 | reto | Ll+Ma (D) | Difícil / Difícil | revisar | pendiente |
| 632 | reto | Ma+Ho (D) | Difícil / Difícil | revisar | pendiente |
| 635 | reto | Ma+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 638 | reto | Ba+Ho (D) | Difícil / Difícil | revisar | pendiente |
| 640 | hito | Ma+Ba (MD) | Muy difícil / Muy difícil | aceptar | pendiente |
| 646 | reto | Pi+Br+Ha (D) | Normal / Difícil | revisar | pendiente |
| 649 | reto | Pi+Ha (D) | Difícil / Difícil | aceptar | pendiente |
| 652 | reto | Br+Re (D) | Difícil / Difícil | revisar | pendiente |
| 655 | reto | Br+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 658 | reto | Ha+Re (D) | Normal / Difícil | revisar | pendiente |
| 660 | hito | Br+Ha+Re (MD) | Muy difícil / Muy difícil | revisar | pendiente |
| 666 | reto | Li+Ha (D) | Difícil / Difícil | aceptar | pendiente |
| 669 | reto | Li+Br (D) | Difícil / Difícil | aceptar | pendiente |
| 672 | reto | Li+Re (D) | Difícil / Difícil | aceptar | pendiente |
| 675 | reto | Br+Tu (D) | Difícil / Difícil | revisar | pendiente |
| 678 | reto | Ha+Re (D) | Difícil / Difícil | revisar | pendiente |
| 680 | hito | Li+Ha+Re (MD; K = 13) | Muy difícil / Muy difícil | aceptar | pendiente |
| 686 | reto | Ma+Ho (D) | Normal / Difícil | revisar | pendiente |
| 689 | reto | Ho+B2 (D) | Difícil / Difícil | revisar | pendiente |
| 692 | reto | Ma+Ha (D) | Normal / Difícil | revisar | pendiente |
| 695 | reto | Ho+Tu (D) | Difícil / Difícil | revisar | pendiente |
| 698 | reto | Ha+B2 (D) | Difícil / Difícil | revisar | pendiente |
| 700 | hito | Ma+Ha+B2 (MD) «Yarinacocha» [P] | Muy difícil / Muy difícil | aceptar | pendiente |
