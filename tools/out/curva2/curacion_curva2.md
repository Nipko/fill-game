# Curación de 301-700: retos, hitos y estrenos para el dueño

Generado por `dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/campaign_v3.json --levels 301-700 --out Assets/_Project/Data/Levels --curation tools/out/curva2/curacion_curva2.md` con los mismos niveles colocados que el pack: no se edita a mano y no lleva tiempos (la decisión del dueño sale de `--decisions`, que aquí no se pasó). Niveles del mundo 7 (Madre de Dios, 301-420), mundo 8 (Madidi, 421-560), mundo 9 (Ucayali, 561-700).

Fuentes: 09 §0.1 (fila de level-pipeline-dev: el dueño aprueba la curación de retos e hitos de cada paquete) y §0.3 (propuesta de curación de los 30 retos e hitos que aprueba el dueño), 03 §7 (un reto por debajo de 0,75 se publica sin etiqueta y la curación decide si se sustituye por otro del pool del slot; banda por el cuantil q de D en el pool del rango), D-009 (etiqueta por el bot ×40 con la semilla del nivel: Difícil ≥ 0,75 y Muy difícil ≥ 0,90; retos en p6, p9, p12, p15 y p18 e hito en p20; c.2: un reto o el hito fuera de su ventana se publica con la etiqueta que mide), 03 §11-§12, 02 §9.2 regla 2 (estrenos) y la tabla de novedades de D2 (d.5.3; D-023 punto 10). La columna «Propuesta» es [P]: la decide el dueño.

Compuerta automática (verde antes de pedir la curación): `dotnet run --project tools/level-qa -c Release -- Assets/_Project/Data/Levels/curva_07_v1.json Assets/_Project/Data/Levels/curva_08_v1.json Assets/_Project/Data/Levels/curva_09_v1.json --spec tools/level-generator/specs/campaign_v3.json --levels 301-700`. Cada reto o hito con aviso QA-WINDOW (bot_fail fuera de la ventana del slot) sale aquí como «revisar», y cada hito con aviso QA-BAND-Q (fuera de la banda de hito con el mínimo cumplido), como «revisar» o con nota de banda; los avisos QA-BAND-Q de las recuperaciones no entran en este informe. QA-BAND-Q no mira los retos y level-qa no mira risk_adjacent: las notas de banda de los retos y los «revisar» por risk_adjacent salen solo de este informe.

## Resumen

| Pack | Niveles | `pack_schema` | Bytes | SHA-256 |
|---|---|---|---|---|
| `Assets/_Project/Data/Levels/curva_07_v1.json` | 120 | 5 | 165753 | `6411c254983ad648901a8c9607e2f9f0d3848521a14d6a1267f37fd843ea8ac7` |
| `Assets/_Project/Data/Levels/curva_08_v1.json` | 140 | 5 | 192119 | `855e295c291a761ac97e4c973128158e0b18f053ebe7e8ed2cb6b75b9ccf6864` |
| `Assets/_Project/Data/Levels/curva_09_v1.json` | 140 | 5 | 191864 | `fd0919117c3f4b2640857f5a52c7d3d14fa6eaf88f03f46e489aa2392b27b51e` |

- Retos e hitos: 120 (100 retos y 20 hitos); 113 miden la etiqueta objetivo o más (D-009) y 7 no.
- No miden su etiqueta objetivo y se publican con la que miden (c.2; 03 §7): 329 (reto Ma (D): Normal, bot_fail 0.725), 352 (reto B2+Re (D): Normal, bot_fail 0.575), 449 (reto Br+Re (D): Normal, bot_fail 0.725), 509 (reto Fl (D): Normal, bot_fail 0.725), 549 (reto La+Ln (D): Normal, bot_fail 0.725), 672 (reto Li+Re (D): Normal, bot_fail 0.700), 686 (reto Ma+Ho (D): Normal, bot_fail 0.725).
- Etiquetas publicadas en 301-700: 93 Difícil y 20 Muy difícil de 400 niveles (03 §11: 25 % y 5 %).
- Para revisar: 306 (reto), 320 (hito), 329 (reto), 349 (reto), 352 (reto), 355 (reto), 369 (reto), 372 (reto), 386 (reto), 389 (reto), 395 (reto), 400 (hito), 409 (reto), 449 (reto), 460 (hito), 466 (reto), 472 (reto), 480 (hito), 498 (reto), 500 (hito), 509 (reto), 512 (reto), 518 (reto), 526 (reto), 529 (reto), 538 (reto), 546 (reto), 549 (reto), 558 (reto), 566 (reto), 572 (reto), 580 (hito), 592 (reto), 595 (reto), 598 (reto), 600 (hito), 618 (reto), 632 (reto), 635 (reto), 640 (hito), 649 (reto), 652 (reto), 655 (reto), 660 (hito), 672 (reto), 680 (hito), 686 (reto). El resto, aceptar.
- Estrenos: ★Ag en 303, 304, 305; ★Tu en 323, 324; ★B2 en 343, 344, 345; ★A2 en 347, 348; ★La en 363, 364, 365; ★Fl en 403, 404, 405 (11 de 16 con un ejemplar, 2 vacíos y bot_fail < 0,20).

## Retos e hitos

Etiqueta = la que mide el bot ×40 y se publica (D-009); Objetivo = la de la celda (D) o (MD). q = cuantil de D en el pool del rango (banda: recuperación ≤ 0,10, comodidad ≤ 0,70, reto ≤ 0,95, hito > 0,95). «Otros del slot» = candidatos medidos del mismo slot, sin rechazo, que miden la etiqueta objetivo o más: con ellos se puede sustituir el nivel al regenerar (03 §7), si la escalera del ciclo lo admite.

| Nivel | p | Rol | Calendario | K | Cuias | Vacíos (libres) | Par | bot_fail | Ventana | Etiqueta | Objetivo | D | q (banda) | Otros del slot | Propuesta [P] |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 306 | 6 | reto | Ha+Ll (D) | 9 | 11 | 2 (1) | 20 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.41 | 0.758 (reto) | 9 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 309 | 9 | reto | Ag+Ll (D) | 9 | 11 | 2 (0) | 24 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.52 | 0.797 (reto) | 11 | aceptar |
| 312 | 12 | reto | Ag+Br (D) | 9 | 11 | 1 (0) | 23 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.59 | 0.821 (reto) | 11 | aceptar |
| 315 | 15 | reto | Br+Tu (D) | 9 | 10 | 0 (0) | 22 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.70 | 0.852 (reto) | 10 | aceptar (turbia: 2 dosis ocultas en 1 cuia(s), 2 realidad(es) compatibles con lo visible) |
| 318 | 18 | reto | Ag+Ln (D) | 9 | 11 | 2 (1) | 26 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.87 | 0.898 (reto) | 3 | aceptar |
| 320 | 20 | hito | Ha+Ll+Br (MD) | 11 | 13 | 2 (1) | 25 | 0.925 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.02 | 0.935 (reto) | 37 | **revisar**: cvd_adjacent: 2 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) (D en banda reto (q 0.935), con el mínimo del hito cumplido: par − 2K = 3 ≥ 2.5 (aviso QA-BAND-Q)) |
| 326 | 6 | reto | Ag+Ma (D) | 9 | 12 | 3 (1) | 20 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 0.62 | 0.827 (reto) | 11 | aceptar |
| 329 | 9 | reto | Ma (D) | 10 | 12 | 2 (1) | 25 | 0.725 | [0.75, 0.90) | Normal | Difícil | 0.63 | 0.830 (reto) | 4 | **revisar**: mide Normal (bot_fail 0.725) y apunta a Difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir |
| 332 | 12 | reto | Ba+Ho (D) | 9 | 11 | 1 (0) | 23 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.68 | 0.842 (reto) | 11 | aceptar |
| 335 | 15 | reto | Ma+Ho (D) | 9 | 11 | 2 (0) | 30 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 1.11 | 0.954 (hito) | 6 | aceptar |
| 338 | 18 | reto | Ag+Ba (D) | 9 | 11 | 1 (1) | 30 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 1.14 | 0.965 (hito) | 8 | aceptar |
| 340 | 20 | hito | Ag+Br+Ba (MD) | 11 | 13 | 1 (0) | 27 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.20 | 0.974 (hito) | 11 | aceptar |
| 346 | 6 | reto | B2+Ln (D) | 10 | 12 | 1 (1) | 24 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.57 | 0.810 (reto) | 4 | aceptar |
| 349 | 9 | reto | B2 (D) | 10 | 12 | 1 (1) | 21 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.60 | 0.823 (reto) | 2 | **revisar**: risk_adjacent: 1 contacto(s) de tintes de un par de riesgo en una cuia (03 §4.5); cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 352 | 12 | reto | B2+Re (D) | 9 | 11 | 1 (1) | 23 | 0.575 | [0.75, 0.90) | Normal | Difícil | 0.69 | 0.848 (reto) | 2 | **revisar**: mide Normal (bot_fail 0.575) y apunta a Difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir |
| 355 | 15 | reto | B2+Re (D) | 9 | 11 | 1 (1) | 21 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.89 | 0.902 (reto) | 1 | **revisar**: risk_adjacent: 1 contacto(s) de tintes de un par de riesgo en una cuia (03 §4.5) |
| 358 | 18 | reto | Ag+Ln (D) | 9 | 11 | 2 (1) | 28 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 1.02 | 0.933 (reto) | 0 | aceptar |
| 360 | 20 | hito | B2+Ln+Ag (MD) | 11 | 13 | 1 (0) | 27 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.33 | 0.984 (hito) | 29 | aceptar |
| 366 | 6 | reto | Ho+Ag+Tu (D) | 9 | 12 | 2 (0) | 20 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.25 | 0.725 (reto) | 17 | aceptar (turbia: 3 dosis ocultas en 2 cuia(s), 4 realidad(es) compatibles con lo visible); primer reto con tres mecánicas, d.5.2; 03 §11 |
| 369 | 9 | reto | Pi+Ma (D) | 9 | 11 | 1 (0) | 22 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 0.27 | 0.725 (reto) | 3 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 372 | 12 | reto | La+B2 (D) | 9 | 12 | 1 (1) | 24 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.66 | 0.836 (reto) | 2 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 375 | 15 | reto | La+Ba (D) | 9 | 12 | 1 (1) | 23 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.74 | 0.861 (reto) | 3 | aceptar |
| 378 | 18 | reto | La+Ho (D) | 9 | 12 | 1 (0) | 24 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 0.76 | 0.861 (reto) | 6 | aceptar |
| 380 | 20 | hito | Pi+Ba+Ag (MD) | 11 | 13 | 0 (0) | 35 | 0.925 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.20 | 0.975 (hito) | 17 | aceptar |
| 386 | 6 | reto | Ba+Tu (D) | 9 | 11 | 1 (1) | 23 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.52 | 0.795 (reto) | 0 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) (turbia: 3 dosis ocultas en 2 cuia(s), 4 realidad(es) compatibles con lo visible) |
| 389 | 9 | reto | Li+Ha (D) | 9 | 11 | 2 (2) | 27 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.81 | 0.873 (reto) | 0 | **revisar**: risk_adjacent: 1 contacto(s) de tintes de un par de riesgo en una cuia (03 §4.5); cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 392 | 12 | reto | La+Ba (D) | 9 | 12 | 1 (1) | 26 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 1.02 | 0.936 (reto) | 0 | aceptar |
| 395 | 15 | reto | Li+Ba (D) | 9 | 11 | 1 (1) | 28 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 1.24 | 0.978 (hito) | 4 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 398 | 18 | reto | Ha+Ba (D) | 9 | 11 | 1 (1) | 29 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 1.55 | 0.991 (hito) | 1 | aceptar |
| 400 | 20 | hito | Li+Ha+Ag (MD) | 11 | 13 | 2 (1) | 34 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.82 | 0.993 (hito) | 5 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 406 | 6 | reto | Ag+Ll (D) | 9 | 11 | 2 (0) | 20 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.07 | 0.619 (comodidad) | 5 | aceptar (Difícil por el bot, con D en banda comodidad (q 0.619)) |
| 409 | 9 | reto | Fl+Ll (D) | 9 | 11 | 2 (1) | 18 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 0.32 | 0.740 (reto) | 5 | **revisar**: cvd_adjacent: 2 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 412 | 12 | reto | Fl+La (D) | 9 | 12 | 1 (1) | 21 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.35 | 0.751 (reto) | 3 | aceptar |
| 415 | 15 | reto | Fl (D) | 9 | 10 | 1 (1) | 21 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.39 | 0.773 (reto) | 2 | aceptar |
| 418 | 18 | reto | Br+Tu (D) | 9 | 11 | 1 (1) | 20 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.62 | 0.843 (reto) | 5 | aceptar (turbia: 2 dosis ocultas en 1 cuia(s), 2 realidad(es) compatibles con lo visible) |
| 420 | 20 | hito | Fl+Ba+A2 (MD) «Zafra» [P] | 12 | 14 | 1 (0) | 39 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 2.28 | 1.000 (hito) | 5 | aceptar |
| 426 | 6 | reto | Fl+Ma (D) | 9 | 11 | 2 (1) | 19 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.45 | 0.789 (reto) | 4 | aceptar |
| 429 | 9 | reto | Ag+Ma+Re (D) | 9 | 11 | 2 (0) | 23 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 0.61 | 0.840 (reto) | 7 | aceptar |
| 432 | 12 | reto | Fl+Ho (D) | 9 | 12 | 3 (2) | 31 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 0.95 | 0.929 (reto) | 5 | aceptar |
| 435 | 15 | reto | Ma+Tu (D) | 9 | 11 | 1 (0) | 23 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 1.00 | 0.940 (reto) | 5 | aceptar (turbia: 2 dosis ocultas en 1 cuia(s), 1 realidad(es) compatibles con lo visible) |
| 438 | 18 | reto | Ho+Re (D) | 9 | 11 | 2 (1) | 23 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 1.01 | 0.943 (reto) | 5 | aceptar |
| 440 | 20 | hito | Ag+Ho+Re (MD) | 11 | 13 | 2 (0) | 28 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.44 | 0.980 (hito) | 4 | aceptar |
| 446 | 6 | reto | Pi+Ba (D) | 9 | 11 | 0 (0) | 19 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.34 | 0.747 (reto) | 5 | aceptar |
| 449 | 9 | reto | Br+Re (D) | 9 | 11 | 1 (1) | 20 | 0.725 | [0.75, 0.90) | Normal | Difícil | 0.52 | 0.809 (reto) | 1 | **revisar**: mide Normal (bot_fail 0.725) y apunta a Difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir |
| 452 | 12 | reto | Pi+Br+Ag (D) | 9 | 12 | 1 (0) | 22 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 0.52 | 0.811 (reto) | 5 | aceptar |
| 455 | 15 | reto | Br+Tu (D) | 9 | 11 | 1 (1) | 21 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.57 | 0.825 (reto) | 5 | aceptar (turbia: 3 dosis ocultas en 2 cuia(s), 2 realidad(es) compatibles con lo visible) |
| 458 | 18 | reto | Ba+Re (D) | 9 | 11 | 1 (1) | 27 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 1.18 | 0.967 (hito) | 2 | aceptar |
| 460 | 20 | hito | Pi+Ba+Ag (MD) | 11 | 13 | 0 (0) | 34 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.34 | 0.978 (hito) | 17 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 466 | 6 | reto | Li+B2 (D) | 9 | 11 | 1 (1) | 19 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.30 | 0.727 (reto) | 11 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 469 | 9 | reto | Ln+B2 (D) | 9 | 11 | 1 (1) | 21 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 0.33 | 0.743 (reto) | 8 | aceptar |
| 472 | 12 | reto | Li+La (D) | 9 | 12 | 1 (1) | 21 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 0.37 | 0.756 (reto) | 5 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 475 | 15 | reto | Ho+Tu (D) | 9 | 11 | 1 (0) | 25 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 0.41 | 0.774 (reto) | 11 | aceptar (turbia: 2 dosis ocultas en 1 cuia(s), 2 realidad(es) compatibles con lo visible) |
| 478 | 18 | reto | La+Ln (D) | 9 | 12 | 1 (1) | 23 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.61 | 0.841 (reto) | 4 | aceptar |
| 480 | 20 | hito | Li+B2+Ho (MD) | 11 | 13 | 1 (0) | 30 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.21 | 0.967 (hito) | 15 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 486 | 6 | reto | Ma+Ha (D) | 9 | 11 | 2 (1) | 19 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.37 | 0.759 (reto) | 11 | aceptar |
| 489 | 9 | reto | Ma+Ba (D) | 9 | 11 | 1 (0) | 20 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 0.46 | 0.796 (reto) | 11 | aceptar |
| 492 | 12 | reto | Ag+Ha+Ba (D) | 9 | 10 | 0 (0) | 20 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.46 | 0.799 (reto) | 0 | aceptar |
| 495 | 15 | reto | Ma+Tu (D) | 9 | 11 | 1 (0) | 23 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 0.58 | 0.827 (reto) | 11 | aceptar (turbia: 3 dosis ocultas en 2 cuia(s), 6 realidad(es) compatibles con lo visible) |
| 498 | 18 | reto | Fl+Ha (D) | 9 | 11 | 2 (2) | 27 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.99 | 0.938 (reto) | 3 | **revisar**: cvd_adjacent: 2 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 500 | 20 | hito | Re+Ha+A2 (MD) | 11 | 13 | 2 (0) | 29 | 0.950 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.13 | 0.957 (hito) | 11 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 506 | 6 | reto | La+Ba (D) | 9 | 12 | 1 (1) | 19 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.36 | 0.753 (reto) | 1 | aceptar |
| 509 | 9 | reto | Fl (D) | 9 | 11 | 2 (2) | 26 | 0.725 | [0.75, 0.90) | Normal | Difícil | 0.37 | 0.759 (reto) | 10 | **revisar**: mide Normal (bot_fail 0.725) y apunta a Difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir; risk_adjacent: 1 contacto(s) de tintes de un par de riesgo en una cuia (03 §4.5); cvd_adjacent: 3 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 512 | 12 | reto | Ll+Ha (D) | 9 | 11 | 2 (1) | 19 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.51 | 0.808 (reto) | 5 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 515 | 15 | reto | Ba+Tu (D) | 9 | 11 | 1 (1) | 21 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.55 | 0.821 (reto) | 0 | aceptar (turbia: 2 dosis ocultas en 1 cuia(s), 1 realidad(es) compatibles con lo visible) |
| 518 | 18 | reto | Fl+Ba (D) | 9 | 11 | 1 (1) | 26 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.71 | 0.871 (reto) | 2 | **revisar**: cvd_adjacent: 2 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 520 | 20 | hito | Li+Ha+A2 (MD) | 11 | 13 | 2 (0) | 33 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.58 | 0.990 (hito) | 5 | aceptar |
| 526 | 6 | reto | Pi+Ha (D) | 9 | 11 | 1 (1) | 20 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.45 | 0.788 (reto) | 11 | **revisar**: risk_adjacent: 1 contacto(s) de tintes de un par de riesgo en una cuia (03 §4.5) |
| 529 | 9 | reto | Ag+Ho+Re (D) | 9 | 11 | 2 (0) | 24 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.45 | 0.792 (reto) | 11 | **revisar**: cvd_adjacent: 3 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 532 | 12 | reto | Pi+Ho (D) | 9 | 11 | 1 (0) | 25 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.54 | 0.815 (reto) | 11 | aceptar |
| 535 | 15 | reto | Ho+Tu (D) | 9 | 11 | 1 (0) | 23 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 0.58 | 0.828 (reto) | 11 | aceptar (turbia: 2 dosis ocultas en 2 cuia(s), 1 realidad(es) compatibles con lo visible) |
| 538 | 18 | reto | Ha+Re (D) | 9 | 11 | 2 (2) | 30 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 1.15 | 0.964 (hito) | 2 | **revisar**: risk_adjacent: 1 contacto(s) de tintes de un par de riesgo en una cuia (03 §4.5) |
| 540 | 20 | hito | Ln+Ha+A2 (MD) | 11 | 13 | 2 (0) | 29 | 0.975 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.24 | 0.969 (hito) | 11 | aceptar |
| 546 | 6 | reto | Fl+Ho (D) | 9 | 11 | 2 (1) | 21 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.24 | 0.700 (reto) | 5 | **revisar**: cvd_adjacent: 4 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 549 | 9 | reto | La+Ln (D) | 9 | 12 | 1 (1) | 19 | 0.725 | [0.75, 0.90) | Normal | Difícil | 0.36 | 0.755 (reto) | 4 | **revisar**: mide Normal (bot_fail 0.725) y apunta a Difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir |
| 552 | 12 | reto | Ln+Re (D) | 9 | 10 | 1 (0) | 18 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.37 | 0.758 (reto) | 2 | aceptar |
| 555 | 15 | reto | Ho+Tu (D) | 9 | 11 | 1 (0) | 25 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 0.73 | 0.874 (reto) | 5 | aceptar (turbia: 2 dosis ocultas en 1 cuia(s), 2 realidad(es) compatibles con lo visible) |
| 558 | 18 | reto | Fl (D) | 9 | 11 | 2 (2) | 27 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 0.77 | 0.886 (reto) | 3 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 560 | 20 | hito | Fl+Re+A2 (MD) «Riberalta» [P] | 12 | 14 | 2 (0) | 30 | 0.925 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.31 | 0.976 (hito) | 3 | aceptar |
| 566 | 6 | reto | Ag+Ma+Re (D) | 9 | 12 | 3 (0) | 18 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 0.33 | 0.744 (reto) | 5 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 569 | 9 | reto | B2+Ba (D) | 9 | 11 | 1 (1) | 23 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 0.39 | 0.773 (reto) | 5 | aceptar |
| 572 | 12 | reto | Ma+Ba (D) | 9 | 11 | 1 (0) | 23 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.53 | 0.812 (reto) | 5 | **revisar**: cvd_adjacent: 2 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 575 | 15 | reto | Ma+Tu (D) | 9 | 11 | 1 (0) | 21 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.54 | 0.816 (reto) | 5 | aceptar (turbia: 3 dosis ocultas en 2 cuia(s), 2 realidad(es) compatibles con lo visible) |
| 578 | 18 | reto | B2+Re (D) | 9 | 11 | 1 (1) | 20 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.54 | 0.815 (reto) | 2 | aceptar |
| 580 | 20 | hito | Ba+B2+Ag (MD) | 11 | 13 | 1 (0) | 28 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.24 | 0.969 (hito) | 5 | **revisar**: cvd_adjacent: 2 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 586 | 6 | reto | Pi+Ha (D) | 9 | 11 | 1 (1) | 20 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 0.32 | 0.741 (reto) | 5 | aceptar |
| 589 | 9 | reto | Ha+B2 (D) | 9 | 11 | 1 (1) | 21 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.44 | 0.787 (reto) | 2 | aceptar |
| 592 | 12 | reto | Pi+B2+Ag (D) | 9 | 12 | 1 (0) | 25 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.73 | 0.878 (reto) | 2 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 595 | 15 | reto | Ba+Tu (D) | 9 | 11 | 1 (1) | 25 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.81 | 0.898 (reto) | 0 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) (turbia: 2 dosis ocultas en 1 cuia(s), 2 realidad(es) compatibles con lo visible) |
| 598 | 18 | reto | Pi+Ba (D) | 9 | 11 | 0 (0) | 29 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 1.05 | 0.950 (reto) | 5 | **revisar**: cvd_adjacent: 2 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 600 | 20 | hito | Ha+Ba+A2 (MD) | 11 | 13 | 1 (0) | 29 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.17 | 0.965 (hito) | 5 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 606 | 6 | reto | Ln+Re (D) | 9 | 10 | 1 (0) | 19 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.27 | 0.713 (reto) | 7 | aceptar |
| 609 | 9 | reto | Li+Ll (D) | 9 | 11 | 2 (1) | 20 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.39 | 0.771 (reto) | 10 | aceptar |
| 612 | 12 | reto | Ag+Ll (D) | 9 | 11 | 2 (0) | 25 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.72 | 0.873 (reto) | 10 | aceptar |
| 615 | 15 | reto | Li+Re (D) | 9 | 11 | 2 (2) | 28 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 1.11 | 0.956 (hito) | 4 | aceptar |
| 618 | 18 | reto | Ln+Ll (D) | 9 | 12 | 3 (2) | 28 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 1.14 | 0.961 (hito) | 11 | **revisar**: risk_adjacent: 2 contacto(s) de tintes de un par de riesgo en una cuia (03 §4.5); cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 620 | 20 | hito | Ln+Ll+Ag (MD) | 11 | 13 | 2 (0) | 33 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.34 | 0.978 (hito) | 11 | aceptar |
| 626 | 6 | reto | Ma+Ba (D) | 9 | 11 | 1 (0) | 19 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.28 | 0.715 (reto) | 10 | aceptar |
| 629 | 9 | reto | Ll+Ma (D) | 9 | 11 | 2 (0) | 21 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.31 | 0.735 (reto) | 8 | aceptar |
| 632 | 12 | reto | Ma+Ho (D) | 9 | 11 | 2 (0) | 23 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 0.38 | 0.765 (reto) | 7 | **revisar**: cvd_adjacent: 2 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 635 | 15 | reto | Ma+Tu (D) | 9 | 11 | 1 (0) | 19 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.53 | 0.813 (reto) | 7 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) (turbia: 2 dosis ocultas en 1 cuia(s), 2 realidad(es) compatibles con lo visible) |
| 638 | 18 | reto | Ag+Ba+Ho (D) | 9 | 12 | 2 (0) | 26 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 0.68 | 0.863 (reto) | 9 | aceptar |
| 640 | 20 | hito | Li+Ba+A2 (MD) | 11 | 13 | 1 (0) | 32 | 0.950 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.22 | 0.967 (hito) | 11 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 646 | 6 | reto | Pi+Br+Ha (D) | 9 | 12 | 1 (1) | 19 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.55 | 0.820 (reto) | 0 | aceptar |
| 649 | 9 | reto | Pi+Ha (D) | 9 | 11 | 1 (1) | 22 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 0.82 | 0.899 (reto) | 8 | **revisar**: cvd_adjacent: 2 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 652 | 12 | reto | Ag+Br+Re (D) | 9 | 11 | 1 (0) | 20 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 0.88 | 0.913 (reto) | 11 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 655 | 15 | reto | Br+Tu (D) | 9 | 11 | 1 (1) | 25 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 1.05 | 0.950 (reto) | 10 | **revisar**: risk_adjacent: 1 contacto(s) de tintes de un par de riesgo en una cuia (03 §4.5); cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) (turbia: 3 dosis ocultas en 2 cuia(s), 6 realidad(es) compatibles con lo visible) |
| 658 | 18 | reto | Ag+Ha+Re (D) | 9 | 11 | 2 (1) | 26 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 1.30 | 0.974 (hito) | 0 | aceptar |
| 660 | 20 | hito | Ln+Ha+A2 (MD) | 11 | 13 | 2 (0) | 36 | 0.975 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.49 | 0.985 (hito) | 11 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 666 | 6 | reto | Li+Ha (D) | 9 | 10 | 1 (1) | 18 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.05 | 0.607 (comodidad) | 6 | aceptar (Difícil por el bot, con D en banda comodidad (q 0.607)) |
| 669 | 9 | reto | Li+Br (D) | 9 | 11 | 1 (1) | 22 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.37 | 0.761 (reto) | 11 | aceptar |
| 672 | 12 | reto | Li+Re (D) | 9 | 11 | 2 (2) | 26 | 0.700 | [0.75, 0.90) | Normal | Difícil | 0.59 | 0.832 (reto) | 4 | **revisar**: mide Normal (bot_fail 0.700) y apunta a Difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir; cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 675 | 15 | reto | Br+Tu (D) | 9 | 11 | 1 (1) | 22 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.78 | 0.893 (reto) | 11 | aceptar (turbia: 2 dosis ocultas en 1 cuia(s), 2 realidad(es) compatibles con lo visible) |
| 678 | 18 | reto | Ha+Re (D) | 9 | 11 | 2 (2) | 28 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 1.04 | 0.948 (reto) | 3 | aceptar |
| 680 | 20 | hito | Li+Ha+A2 (MD) | 11 | 13 | 2 (0) | 29 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.07 | 0.952 (hito) | 11 | **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) |
| 686 | 6 | reto | Ma+Ho (D) | 9 | 11 | 2 (0) | 21 | 0.725 | [0.75, 0.90) | Normal | Difícil | 0.11 | 0.632 (comodidad) | 6 | **revisar**: mide Normal (bot_fail 0.725) y apunta a Difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir |
| 689 | 9 | reto | Ho+B2 (D) | 9 | 11 | 1 (0) | 23 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.34 | 0.749 (reto) | 2 | aceptar |
| 692 | 12 | reto | Ma+Ha (D) | 9 | 11 | 2 (1) | 18 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.38 | 0.765 (reto) | 1 | aceptar |
| 695 | 15 | reto | Ho+Tu (D) | 9 | 11 | 1 (0) | 22 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.46 | 0.796 (reto) | 5 | aceptar (turbia: 2 dosis ocultas en 1 cuia(s), 2 realidad(es) compatibles con lo visible) |
| 698 | 18 | reto | Ag+Ha+B2 (D) | 9 | 11 | 1 (0) | 21 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.69 | 0.866 (reto) | 5 | aceptar |
| 700 | 20 | hito | Fl+Ha+A2 (MD) «Yarinacocha» [P] | 12 | 14 | 2 (0) | 32 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.18 | 0.966 (hito) | 5 | aceptar |

## Hitos

p20 de cada ciclo («Pico» de c.3): Muy difícil por el bot (≥ 0,90; D-009) y, por D, en banda de hito (q > 0,95; 03 §7) o, como mínimo, con par − 2K ≥ la media del pool del rango (criterio [P] del generador y de level-qa, revisión del 2026-10-09; README del generador; QA-BAND-Q). Los nombres de hito de la celda son [P] y pasan por el asesor (D-023 punto 21): no van al pack.

### 320 · Ha+Ll+Br (MD)

- Tablero: K 11, 13 cuias (13 de 4), 2 vacío(s) (1 libre(s)); fuente `deep`.
- Mecánicas: Br (cuia de 4); hoja_atada key=4 (4 dosis); Ll (cuia de 4).
- Medida: par 25 (2K = 22; par − 2K = 3, media del pool 2.5), bot_fail 0.925 → Muy difícil (objetivo Muy difícil), D 1.02, q 0.935 (banda reto), ramificación 7.88, trampas tempranas 0.
- Notas de la generación: 1 ronda(s) extra de candidatos; 6 ronda(s) de banda.
- Propuesta [P]: **revisar**: cvd_adjacent: 2 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3) (D en banda reto (q 0.935), con el mínimo del hito cumplido: par − 2K = 3 ≥ 2.5 (aviso QA-BAND-Q)).

### 340 · Ag+Br+Ba (MD)

- Tablero: K 11, 13 cuias (13 de 4), 1 vacío(s) (0 libre(s)); fuente `deep`.
- Mecánicas: Br (cuia de 4); Ag (cuia de 4); Ag (cuia de 4); Barro: 1 dosis.
- Medida: par 27 (2K = 22; par − 2K = 5, media del pool 2.5), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 1.20, q 0.974 (banda hito), ramificación 6.93, trampas tempranas 0.
- Notas de la generación: 1 ronda(s) extra de candidatos.
- Propuesta [P]: aceptar.

### 360 · B2+Ln+Ag (MD)

- Tablero: K 11, 13 cuias (13 de 4), 1 vacío(s) (0 libre(s)); fuente `deep`.
- Mecánicas: liana n=1 (3 dosis); brote n=2 (2 dosis); Ag (cuia de 4); Ag (cuia de 4).
- Medida: par 27 (2K = 22; par − 2K = 5, media del pool 2.5), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 1.33, q 0.984 (banda hito), ramificación 5.37, trampas tempranas 1.
- Notas de la generación: relajacion: deep con 60.000 nodos; 2 ronda(s) de banda.
- Propuesta [P]: aceptar.

### 380 · Pi+Ba+Ag (MD)

- Tablero: K 11, 13 cuias (12 de 4, 1 de 2), 0 vacío(s) (0 libre(s)); fuente `fill`.
- Mecánicas: Pi (cuia de 2); Ag (cuia de 4); Ag (cuia de 4); Barro: 1 dosis.
- Medida: par 35 (2K = 22; par − 2K = 13, media del pool 2.5), bot_fail 0.925 → Muy difícil (objetivo Muy difícil), D 1.20, q 0.975 (banda hito), ramificación 3.14, trampas tempranas 2.
- Notas de la generación: 2 ronda(s) extra de candidatos.
- Propuesta [P]: aceptar.

### 400 · Li+Ha+Ag (MD)

- Tablero: K 11, 13 cuias (13 de 4), 2 vacío(s) (1 libre(s)); fuente `fill`.
- Mecánicas: hoja_atada key=6 (4 dosis); Li key=8 (cuia de 4); Ag (cuia de 4); Ag (cuia de 4).
- Medida: par 34 (2K = 22; par − 2K = 12, media del pool 2.5), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 1.82, q 0.993 (banda hito), ramificación 3.53, trampas tempranas 8.
- Notas de la generación: relajacion: fill con 2 vacios (03 sec. 4.4).
- Propuesta [P]: **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3).

### 420 · Fl+Ba+A2 (MD) «Zafra» [P]

- Tablero: K 12, 14 cuias (14 de 4), 1 vacío(s) (0 libre(s)); fuente `fill`.
- Mecánicas: flor key=6 key2=13 (4 dosis); A2 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); Barro: 1 dosis.
- Medida: par 39 (2K = 24; par − 2K = 15, media del pool 2.7), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 2.28, q 1.000 (banda hito), ramificación 5.00, trampas tempranas 4.
- Notas de la generación: Aguape espeso: dos vacios, bajo la mata.
- Propuesta [P]: aceptar.

### 440 · Ag+Ho+Re (MD)

- Tablero: K 11, 13 cuias (13 de 4), 2 vacío(s) (0 libre(s)); fuente `deep`.
- Mecánicas: Re (cuia de 4); Ho key=13 (cuia de 4); Re (cuia de 4); Ag (cuia de 4); Ag (cuia de 4); Ag (cuia de 4).
- Medida: par 28 (2K = 22; par − 2K = 6, media del pool 2.7), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 1.44, q 0.980 (banda hito), ramificación 9.68, trampas tempranas 2.
- Notas de la generación: Aguape con una mecanica que ocupa el vacio: un vacio mas, bajo la mata.
- Propuesta [P]: aceptar.

### 460 · Pi+Ba+Ag (MD)

- Tablero: K 11, 13 cuias (12 de 4, 1 de 2), 0 vacío(s) (0 libre(s)); fuente `fill`.
- Mecánicas: Pi (cuia de 2); Ag (cuia de 4); Ag (cuia de 4); Barro: 1 dosis.
- Medida: par 34 (2K = 22; par − 2K = 12, media del pool 2.7), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 1.34, q 0.978 (banda hito), ramificación 3.09, trampas tempranas 4.
- Notas de la generación: 1 ronda(s) de banda.
- Propuesta [P]: **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3).

### 480 · Li+B2+Ho (MD)

- Tablero: K 11, 13 cuias (13 de 4), 1 vacío(s) (0 libre(s)); fuente `deep`.
- Mecánicas: Li key=5 (cuia de 4); Ho key=13 (cuia de 4); brote n=2 (2 dosis).
- Medida: par 30 (2K = 22; par − 2K = 8, media del pool 2.7), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 1.21, q 0.967 (banda hito), ramificación 4.13, trampas tempranas 0.
- Notas de la generación: 1 ronda(s) extra de candidatos; 1 ronda(s) de banda.
- Propuesta [P]: **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3).

### 500 · Re+Ha+A2 (MD)

- Tablero: K 11, 13 cuias (13 de 4), 2 vacío(s) (0 libre(s)); fuente `deep`.
- Mecánicas: A2 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); hoja_atada key=10 (4 dosis); Re (cuia de 4); Re (cuia de 4).
- Medida: par 29 (2K = 22; par − 2K = 7, media del pool 2.7), bot_fail 0.950 → Muy difícil (objetivo Muy difícil), D 1.13, q 0.957 (banda hito), ramificación 6.97, trampas tempranas 4.
- Notas de la generación: Aguape espeso: dos vacios, bajo la mata; 1 ronda(s) extra de candidatos.
- Propuesta [P]: **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3).

### 520 · Li+Ha+A2 (MD)

- Tablero: K 11, 13 cuias (13 de 4), 2 vacío(s) (0 libre(s)); fuente `fill`.
- Mecánicas: Li key=10 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); hoja_atada key=5 (4 dosis).
- Medida: par 33 (2K = 22; par − 2K = 11, media del pool 2.7), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 1.58, q 0.990 (banda hito), ramificación 3.24, trampas tempranas 0.
- Notas de la generación: relajacion: fill con 2 vacios (03 sec. 4.4).
- Propuesta [P]: aceptar.

### 540 · Ln+Ha+A2 (MD)

- Tablero: K 11, 13 cuias (13 de 4), 2 vacío(s) (0 libre(s)); fuente `deep`.
- Mecánicas: A2 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); liana n=1 (4 dosis); hoja_atada key=1 (4 dosis).
- Medida: par 29 (2K = 22; par − 2K = 7, media del pool 2.7), bot_fail 0.975 → Muy difícil (objetivo Muy difícil), D 1.24, q 0.969 (banda hito), ramificación 4.79, trampas tempranas 2.
- Notas de la generación: Aguape espeso: dos vacios, bajo la mata; 1 ronda(s) extra de candidatos.
- Propuesta [P]: aceptar.

### 560 · Fl+Re+A2 (MD) «Riberalta» [P]

- Tablero: K 12, 14 cuias (14 de 4), 2 vacío(s) (0 libre(s)); fuente `deep`.
- Mecánicas: Re (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); flor key=5 key2=3 (4 dosis); Re (cuia de 4).
- Medida: par 30 (2K = 24; par − 2K = 6, media del pool 2.7), bot_fail 0.925 → Muy difícil (objetivo Muy difícil), D 1.31, q 0.976 (banda hito), ramificación 10.07, trampas tempranas 2.
- Notas de la generación: relajacion: deep con 60.000 nodos; Aguape espeso: dos vacios, bajo la mata.
- Propuesta [P]: aceptar.

### 580 · Ba+B2+Ag (MD)

- Tablero: K 11, 13 cuias (13 de 4), 1 vacío(s) (0 libre(s)); fuente `deep`.
- Mecánicas: brote n=2 (1 dosis); Ag (cuia de 4); Ag (cuia de 4); Barro: 1 dosis.
- Medida: par 28 (2K = 22; par − 2K = 6, media del pool 2.7), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 1.24, q 0.969 (banda hito), ramificación 4.14, trampas tempranas 0.
- Propuesta [P]: **revisar**: cvd_adjacent: 2 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3).

### 600 · Ha+Ba+A2 (MD)

- Tablero: K 11, 13 cuias (13 de 4), 1 vacío(s) (0 libre(s)); fuente `deep`.
- Mecánicas: A2 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); hoja_atada key=10 (4 dosis); Barro: 1 dosis.
- Medida: par 29 (2K = 22; par − 2K = 7, media del pool 2.7), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 1.17, q 0.965 (banda hito), ramificación 3.48, trampas tempranas 4.
- Notas de la generación: Aguape espeso: dos vacios, bajo la mata.
- Propuesta [P]: **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3).

### 620 · Ln+Ll+Ag (MD)

- Tablero: K 11, 13 cuias (13 de 4), 2 vacío(s) (0 libre(s)); fuente `fill`.
- Mecánicas: Ag (cuia de 4); Ag (cuia de 4); Ll (cuia de 4); liana n=2 (3 dosis).
- Medida: par 33 (2K = 22; par − 2K = 11, media del pool 2.7), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 1.34, q 0.978 (banda hito), ramificación 3.39, trampas tempranas 6.
- Notas de la generación: Aguape con una mecanica que ocupa el vacio: un vacio mas, bajo la mata; 1 ronda(s) extra de candidatos.
- Propuesta [P]: aceptar.

### 640 · Li+Ba+A2 (MD)

- Tablero: K 11, 13 cuias (13 de 4), 1 vacío(s) (0 libre(s)); fuente `deep`.
- Mecánicas: A2 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); Li key=12 (cuia de 4); Barro: 1 dosis.
- Medida: par 32 (2K = 22; par − 2K = 10, media del pool 2.7), bot_fail 0.950 → Muy difícil (objetivo Muy difícil), D 1.22, q 0.967 (banda hito), ramificación 3.44, trampas tempranas 2.
- Notas de la generación: Aguape espeso: dos vacios, bajo la mata; 1 ronda(s) extra de candidatos.
- Propuesta [P]: **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3).

### 660 · Ln+Ha+A2 (MD)

- Tablero: K 11, 13 cuias (13 de 4), 2 vacío(s) (0 libre(s)); fuente `fill`.
- Mecánicas: A2 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); hoja_atada key=12 (4 dosis); liana n=1 (4 dosis).
- Medida: par 36 (2K = 22; par − 2K = 14, media del pool 2.7), bot_fail 0.975 → Muy difícil (objetivo Muy difícil), D 1.49, q 0.985 (banda hito), ramificación 2.78, trampas tempranas 4.
- Notas de la generación: relajacion: fill con 2 vacios (03 sec. 4.4); 1 ronda(s) extra de candidatos.
- Propuesta [P]: **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3).

### 680 · Li+Ha+A2 (MD)

- Tablero: K 11, 13 cuias (13 de 4), 2 vacío(s) (0 libre(s)); fuente `deep`.
- Mecánicas: A2 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); hoja_atada key=9 (4 dosis); Li key=9 (cuia de 4).
- Medida: par 29 (2K = 22; par − 2K = 7, media del pool 2.7), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 1.07, q 0.952 (banda hito), ramificación 3.52, trampas tempranas 0.
- Notas de la generación: Aguape espeso: dos vacios, bajo la mata; 1 ronda(s) extra de candidatos.
- Propuesta [P]: **revisar**: cvd_adjacent: 1 contacto(s) de tintes de un par del simulador de daltonismo en una cuia (03 §4.5; 07 §3, regla 3).

### 700 · Fl+Ha+A2 (MD) «Yarinacocha» [P]

- Tablero: K 12, 14 cuias (14 de 4), 2 vacío(s) (0 libre(s)); fuente `deep`.
- Mecánicas: A2 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); A2 (cuia de 4); flor key=10 key2=5 (4 dosis); hoja_atada key=10 (4 dosis).
- Medida: par 32 (2K = 24; par − 2K = 8, media del pool 2.7), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 1.18, q 0.966 (banda hito), ramificación 3.16, trampas tempranas 2.
- Notas de la generación: Aguape espeso: dos vacios, bajo la mata.
- Propuesta [P]: aceptar.

## Tableros de estreno

02 §9.2 regla 2: el estreno va en 2-3 tableros fáciles (p3-p5) con la mecánica sola, un ejemplar, 2 vacíos y `bot_fail` < 0,20; el primero lleva `intro_mechanic` (`skippable` = false).

| Nivel | p | Calendario | `intro_mechanic` | Mecánica | Ejemplares | Vacíos | Par | bot_fail | D | q (banda) | Regla 2 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 303 | 3 | ★Ag | `aguape` | Ag; Ag; Ag | 3 | 2 | 16 | 0.000 | -0.81 | 0.027 (recuperación) | **no cumple** |
| 304 | 4 | ★Ag | — | Ag; Ag | 2 | 2 | 17 | 0.050 | -0.34 | 0.281 (comodidad) | **no cumple** |
| 305 | 5 | ★Ag | — | Ag; Ag | 2 | 2 | 16 | 0.150 | -0.34 | 0.289 (comodidad) | **no cumple** |
| 323 | 3 | ★Tu | `murky` | — | 1 | 2 | 14 | 0.000 | -0.68 | 0.064 (recuperación) | cumple |
| 324 | 4 | ★Tu | — | — | 1 | 2 | 14 | 0.000 | -0.73 | 0.056 (recuperación) | cumple |
| 343 | 3 | ★B2 | `brote_n2` | brote n=2 (1 dosis) | 1 | 2 | 14 | 0.000 | -0.66 | 0.069 (recuperación) | cumple |
| 344 | 4 | ★B2 | — | brote n=2 (2 dosis) | 1 | 2 | 15 | 0.000 | -0.55 | 0.119 (comodidad) | cumple |
| 345 | 5 | ★B2 | — | brote n=2 (1 dosis) | 1 | 2 | 15 | 0.000 | -0.69 | 0.059 (recuperación) | cumple |
| 347 | 7 | ★A2 | `aguape_n2` | A2; A2; A2; A2 | 4 | 2 | 19 | 0.100 | -0.41 | 0.224 (comodidad) | **no cumple** |
| 348 | 8 | ★A2 | — | A2; A2; A2; A2; A2 | 5 | 2 | 14 | 0.100 | -0.74 | 0.055 (recuperación) | **no cumple** |
| 363 | 3 | ★La | `lancha` | La | 1 | 2 | 13 | 0.000 | -0.62 | 0.090 (recuperación) | cumple |
| 364 | 4 | ★La | — | La | 1 | 2 | 14 | 0.000 | -0.63 | 0.088 (recuperación) | cumple |
| 365 | 5 | ★La | — | La | 1 | 2 | 12 | 0.000 | -0.88 | 0.017 (recuperación) | cumple |
| 403 | 3 | ★Fl | `flor` | flor key=1 key2=11 (4 dosis) | 1 | 2 | 16 | 0.000 | -1.24 | 0.000 (recuperación) | cumple |
| 404 | 4 | ★Fl | — | flor key=14 key2=1 (4 dosis) | 1 | 2 | 17 | 0.100 | -0.63 | 0.070 (recuperación) | cumple |
| 405 | 5 | ★Fl | — | flor key=1 key2=3 (4 dosis) | 1 | 2 | 16 | 0.150 | -0.73 | 0.047 (recuperación) | cumple |

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
| 343 | 3 | estreno | ★B2 | brote n=2 (1 dosis) | 1 | 2 | 0.000 |
| 344 | 4 | estreno | ★B2 | brote n=2 (2 dosis) | 1 | 2 | 0.000 |
| 345 | 5 | estreno | ★B2 | brote n=2 (1 dosis) | 1 | 2 | 0.000 |
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
| 403 | 3 | estreno | ★Fl | flor key=1 key2=11 (4 dosis) | 1 | 2 | 0.000 |
| 404 | 4 | estreno | ★Fl | flor key=14 key2=1 (4 dosis) | 1 | 2 | 0.100 |
| 405 | 5 | estreno | ★Fl | flor key=1 key2=3 (4 dosis) | 1 | 2 | 0.150 |
| 407 | 7 | comodidad | Fl | flor key=9 key2=3 (4 dosis) | 1 | 2 | 0.475 |
| 409 | 9 | reto | Fl+Ll (D) | flor key=13 key2=10 (3 dosis) | 2 | 2 | 0.750 |
| 412 | 12 | reto | Fl+La (D) | flor key=11 key2=3 (2 dosis) | 2 | 1 | 0.775 |
| 415 | 15 | reto | Fl (D) | flor key=6 key2=4 (4 dosis) | 1 | 1 | 0.850 |
| 420 | 20 | hito | Fl+Ba+A2 (MD) «Zafra» [P] | flor key=6 key2=13 (4 dosis) | 7 | 1 | 1.000 |
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
| 560 | 20 | hito | Fl+Re+A2 (MD) «Riberalta» [P] | flor key=5 key2=3 (4 dosis) | 6 | 2 | 0.925 |
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
| 700 | 20 | hito | Fl+Ha+A2 (MD) «Yarinacocha» [P] | flor key=10 key2=5 (4 dosis); hoja_atada key=10 (4 dosis) | 7 | 2 | 1.000 |

Todas las celdas con una variante la llevan en el nivel publicado.

## Otros niveles con avisos

| Nivel | Rol | Calendario | Aviso |
|---|---|---|---|
| 307 | comodidad | C | cvd_adjacent: 1 contacto(s) |
| 311 | comodidad | Ba | cvd_adjacent: 1 contacto(s) |
| 313 | comodidad | C | cvd_adjacent: 1 contacto(s) |
| 345 | estreno | ★B2 | cvd_adjacent: 1 contacto(s) |
| 359 | remanso | S | cvd_adjacent: 1 contacto(s) |
| 371 | comodidad | La | cvd_adjacent: 2 contacto(s) |
| 383 | comodidad | Li | cvd_adjacent: 2 contacto(s) |
| 384 | comodidad | Tu | cvd_adjacent: 1 contacto(s) |
| 393 | comodidad | La | cvd_adjacent: 1 contacto(s) |
| 397 | comodidad | Ho+Tu | cvd_adjacent: 1 contacto(s) |
| 404 | estreno | ★Fl | cvd_adjacent: 1 contacto(s) |
| 411 | comodidad | Ag | cvd_adjacent: 2 contacto(s) |
| 413 | comodidad | C | cvd_adjacent: 2 contacto(s) |
| 414 | comodidad | Br+Tu | cvd_adjacent: 1 contacto(s) |
| 419 | remanso | S | risk_adjacent: 1 contacto(s) |
| 431 | comodidad | Ag+B2 | cvd_adjacent: 1 contacto(s) |
| 436 | comodidad | C | cvd_adjacent: 1 contacto(s) |
| 443 | comodidad | Pi | cvd_adjacent: 3 contacto(s) |
| 445 | comodidad | Br+Tu | cvd_adjacent: 2 contacto(s) |
| 454 | comodidad | La | cvd_adjacent: 1 contacto(s) |
| 456 | comodidad | C | cvd_adjacent: 1 contacto(s) |
| 484 | comodidad | C | cvd_adjacent: 1 contacto(s) |
| 491 | comodidad | Br+Tu | cvd_adjacent: 1 contacto(s) |
| 493 | comodidad | C | cvd_adjacent: 2 contacto(s) |
| 494 | comodidad | C | cvd_adjacent: 1 contacto(s) |
| 496 | comodidad | C | cvd_adjacent: 3 contacto(s) |
| 503 | comodidad | Ll | cvd_adjacent: 1 contacto(s) |
| 508 | comodidad | Ho+Tu | cvd_adjacent: 2 contacto(s) |
| 510 | comodidad | C | cvd_adjacent: 1 contacto(s) |
| 513 | comodidad | Tu | cvd_adjacent: 1 contacto(s) |
| 516 | comodidad | C | cvd_adjacent: 4 contacto(s) |
| 521 | recuperación | R | cvd_adjacent: 1 contacto(s) |
| 524 | comodidad | Tu | cvd_adjacent: 1 contacto(s) |
| 525 | comodidad | Ag | cvd_adjacent: 1 contacto(s) |
| 530 | comodidad | C | risk_adjacent: 1 contacto(s) |
| 533 | comodidad | La | cvd_adjacent: 1 contacto(s) |
| 534 | comodidad | Ch | cvd_adjacent: 1 contacto(s) |
| 536 | comodidad | C | cvd_adjacent: 1 contacto(s) |
| 542 | recuperación | R | cvd_adjacent: 1 contacto(s) |
| 544 | comodidad | C | cvd_adjacent: 1 contacto(s) |
| 547 | comodidad | C | cvd_adjacent: 2 contacto(s) |
| 559 | remanso | S | cvd_adjacent: 1 contacto(s) |
| 562 | recuperación | R | cvd_adjacent: 1 contacto(s) |
| 573 | comodidad | C | cvd_adjacent: 1 contacto(s) |
| 593 | comodidad | C | cvd_adjacent: 2 contacto(s) |
| 594 | comodidad | La+Al | cvd_adjacent: 1 contacto(s) |
| 601 | recuperación | R | cvd_adjacent: 1 contacto(s) |
| 605 | comodidad | Ag | cvd_adjacent: 1 contacto(s) |
| 628 | comodidad | La+B2 | cvd_adjacent: 1 contacto(s) |
| 630 | comodidad | C | cvd_adjacent: 1 contacto(s) |
| 641 | recuperación | R | cvd_adjacent: 1 contacto(s) |
| 642 | recuperación | R | cvd_adjacent: 2 contacto(s) |
| 651 | comodidad | C | cvd_adjacent: 2 contacto(s) |
| 661 | recuperación | R | cvd_adjacent: 1 contacto(s) |
| 676 | comodidad | C | cvd_adjacent: 4 contacto(s) |
| 690 | comodidad | Tu | cvd_adjacent: 1 contacto(s) |
| 697 | comodidad | Ba+Tu | risk_adjacent: 1 contacto(s); cvd_adjacent: 2 contacto(s) |

## Curación del dueño

Estado: **pendiente** (0 de 126 niveles decididos) (09 §0.1 y §0.3). La decisión por nivel es «aceptar», «sustituir» (otro candidato del slot o del pool, 03 §7) o «cambiar la celda», con el motivo en los dos últimos casos. No se escribe en este informe, que es generado: va en el archivo de `--decisions` (formato [P] `af-curation-decisions/1`), una entrada por nivel con el literal de la celda que vio el dueño, y la última columna la copia de ahí; si la celda cambia, la decisión deja de valer y el nivel vuelve a «pendiente». Si la curación pide cambios, se cambian la especificación o el generador y se regeneran el pack, este informe y la comprobación de `level-qa`.

| Nivel | Rol | Calendario | Etiqueta / objetivo | Propuesta [P] | Decisión del dueño |
|---|---|---|---|---|---|
| 306 | reto | Ha+Ll (D) | Difícil / Difícil | revisar | pendiente |
| 309 | reto | Ag+Ll (D) | Difícil / Difícil | aceptar | pendiente |
| 312 | reto | Ag+Br (D) | Difícil / Difícil | aceptar | pendiente |
| 315 | reto | Br+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 318 | reto | Ag+Ln (D) | Difícil / Difícil | aceptar | pendiente |
| 320 | hito | Ha+Ll+Br (MD) | Muy difícil / Muy difícil | revisar | pendiente |
| 326 | reto | Ag+Ma (D) | Difícil / Difícil | aceptar | pendiente |
| 329 | reto | Ma (D) | Normal / Difícil | revisar | pendiente |
| 332 | reto | Ba+Ho (D) | Difícil / Difícil | aceptar | pendiente |
| 335 | reto | Ma+Ho (D) | Difícil / Difícil | aceptar | pendiente |
| 338 | reto | Ag+Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 340 | hito | Ag+Br+Ba (MD) | Muy difícil / Muy difícil | aceptar | pendiente |
| 346 | reto | B2+Ln (D) | Difícil / Difícil | aceptar | pendiente |
| 349 | reto | B2 (D) | Difícil / Difícil | revisar | pendiente |
| 352 | reto | B2+Re (D) | Normal / Difícil | revisar | pendiente |
| 355 | reto | B2+Re (D) | Difícil / Difícil | revisar | pendiente |
| 358 | reto | Ag+Ln (D) | Difícil / Difícil | aceptar | pendiente |
| 360 | hito | B2+Ln+Ag (MD) | Muy difícil / Muy difícil | aceptar | pendiente |
| 366 | reto | Ho+Ag+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 369 | reto | Pi+Ma (D) | Difícil / Difícil | revisar | pendiente |
| 372 | reto | La+B2 (D) | Difícil / Difícil | revisar | pendiente |
| 375 | reto | La+Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 378 | reto | La+Ho (D) | Difícil / Difícil | aceptar | pendiente |
| 380 | hito | Pi+Ba+Ag (MD) | Muy difícil / Muy difícil | aceptar | pendiente |
| 386 | reto | Ba+Tu (D) | Difícil / Difícil | revisar | pendiente |
| 389 | reto | Li+Ha (D) | Difícil / Difícil | revisar | pendiente |
| 392 | reto | La+Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 395 | reto | Li+Ba (D) | Difícil / Difícil | revisar | pendiente |
| 398 | reto | Ha+Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 400 | hito | Li+Ha+Ag (MD) | Muy difícil / Muy difícil | revisar | pendiente |
| 406 | reto | Ag+Ll (D) | Difícil / Difícil | aceptar | pendiente |
| 409 | reto | Fl+Ll (D) | Difícil / Difícil | revisar | pendiente |
| 412 | reto | Fl+La (D) | Difícil / Difícil | aceptar | pendiente |
| 415 | reto | Fl (D) | Difícil / Difícil | aceptar | pendiente |
| 418 | reto | Br+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 420 | hito | Fl+Ba+A2 (MD) «Zafra» [P] | Muy difícil / Muy difícil | aceptar | pendiente |
| 426 | reto | Fl+Ma (D) | Difícil / Difícil | aceptar | pendiente |
| 429 | reto | Ag+Ma+Re (D) | Difícil / Difícil | aceptar | pendiente |
| 432 | reto | Fl+Ho (D) | Difícil / Difícil | aceptar | pendiente |
| 435 | reto | Ma+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 438 | reto | Ho+Re (D) | Difícil / Difícil | aceptar | pendiente |
| 440 | hito | Ag+Ho+Re (MD) | Muy difícil / Muy difícil | aceptar | pendiente |
| 446 | reto | Pi+Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 449 | reto | Br+Re (D) | Normal / Difícil | revisar | pendiente |
| 452 | reto | Pi+Br+Ag (D) | Difícil / Difícil | aceptar | pendiente |
| 455 | reto | Br+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 458 | reto | Ba+Re (D) | Difícil / Difícil | aceptar | pendiente |
| 460 | hito | Pi+Ba+Ag (MD) | Muy difícil / Muy difícil | revisar | pendiente |
| 466 | reto | Li+B2 (D) | Difícil / Difícil | revisar | pendiente |
| 469 | reto | Ln+B2 (D) | Difícil / Difícil | aceptar | pendiente |
| 472 | reto | Li+La (D) | Difícil / Difícil | revisar | pendiente |
| 475 | reto | Ho+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 478 | reto | La+Ln (D) | Difícil / Difícil | aceptar | pendiente |
| 480 | hito | Li+B2+Ho (MD) | Muy difícil / Muy difícil | revisar | pendiente |
| 486 | reto | Ma+Ha (D) | Difícil / Difícil | aceptar | pendiente |
| 489 | reto | Ma+Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 492 | reto | Ag+Ha+Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 495 | reto | Ma+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 498 | reto | Fl+Ha (D) | Difícil / Difícil | revisar | pendiente |
| 500 | hito | Re+Ha+A2 (MD) | Muy difícil / Muy difícil | revisar | pendiente |
| 506 | reto | La+Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 509 | reto | Fl (D) | Normal / Difícil | revisar | pendiente |
| 512 | reto | Ll+Ha (D) | Difícil / Difícil | revisar | pendiente |
| 515 | reto | Ba+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 518 | reto | Fl+Ba (D) | Difícil / Difícil | revisar | pendiente |
| 520 | hito | Li+Ha+A2 (MD) | Muy difícil / Muy difícil | aceptar | pendiente |
| 526 | reto | Pi+Ha (D) | Difícil / Difícil | revisar | pendiente |
| 529 | reto | Ag+Ho+Re (D) | Difícil / Difícil | revisar | pendiente |
| 532 | reto | Pi+Ho (D) | Difícil / Difícil | aceptar | pendiente |
| 535 | reto | Ho+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 538 | reto | Ha+Re (D) | Difícil / Difícil | revisar | pendiente |
| 540 | hito | Ln+Ha+A2 (MD) | Muy difícil / Muy difícil | aceptar | pendiente |
| 546 | reto | Fl+Ho (D) | Difícil / Difícil | revisar | pendiente |
| 549 | reto | La+Ln (D) | Normal / Difícil | revisar | pendiente |
| 552 | reto | Ln+Re (D) | Difícil / Difícil | aceptar | pendiente |
| 555 | reto | Ho+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 558 | reto | Fl (D) | Difícil / Difícil | revisar | pendiente |
| 560 | hito | Fl+Re+A2 (MD) «Riberalta» [P] | Muy difícil / Muy difícil | aceptar | pendiente |
| 566 | reto | Ag+Ma+Re (D) | Difícil / Difícil | revisar | pendiente |
| 569 | reto | B2+Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 572 | reto | Ma+Ba (D) | Difícil / Difícil | revisar | pendiente |
| 575 | reto | Ma+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 578 | reto | B2+Re (D) | Difícil / Difícil | aceptar | pendiente |
| 580 | hito | Ba+B2+Ag (MD) | Muy difícil / Muy difícil | revisar | pendiente |
| 586 | reto | Pi+Ha (D) | Difícil / Difícil | aceptar | pendiente |
| 589 | reto | Ha+B2 (D) | Difícil / Difícil | aceptar | pendiente |
| 592 | reto | Pi+B2+Ag (D) | Difícil / Difícil | revisar | pendiente |
| 595 | reto | Ba+Tu (D) | Difícil / Difícil | revisar | pendiente |
| 598 | reto | Pi+Ba (D) | Difícil / Difícil | revisar | pendiente |
| 600 | hito | Ha+Ba+A2 (MD) | Muy difícil / Muy difícil | revisar | pendiente |
| 606 | reto | Ln+Re (D) | Difícil / Difícil | aceptar | pendiente |
| 609 | reto | Li+Ll (D) | Difícil / Difícil | aceptar | pendiente |
| 612 | reto | Ag+Ll (D) | Difícil / Difícil | aceptar | pendiente |
| 615 | reto | Li+Re (D) | Difícil / Difícil | aceptar | pendiente |
| 618 | reto | Ln+Ll (D) | Difícil / Difícil | revisar | pendiente |
| 620 | hito | Ln+Ll+Ag (MD) | Muy difícil / Muy difícil | aceptar | pendiente |
| 626 | reto | Ma+Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 629 | reto | Ll+Ma (D) | Difícil / Difícil | aceptar | pendiente |
| 632 | reto | Ma+Ho (D) | Difícil / Difícil | revisar | pendiente |
| 635 | reto | Ma+Tu (D) | Difícil / Difícil | revisar | pendiente |
| 638 | reto | Ag+Ba+Ho (D) | Difícil / Difícil | aceptar | pendiente |
| 640 | hito | Li+Ba+A2 (MD) | Muy difícil / Muy difícil | revisar | pendiente |
| 646 | reto | Pi+Br+Ha (D) | Difícil / Difícil | aceptar | pendiente |
| 649 | reto | Pi+Ha (D) | Difícil / Difícil | revisar | pendiente |
| 652 | reto | Ag+Br+Re (D) | Difícil / Difícil | revisar | pendiente |
| 655 | reto | Br+Tu (D) | Difícil / Difícil | revisar | pendiente |
| 658 | reto | Ag+Ha+Re (D) | Difícil / Difícil | aceptar | pendiente |
| 660 | hito | Ln+Ha+A2 (MD) | Muy difícil / Muy difícil | revisar | pendiente |
| 666 | reto | Li+Ha (D) | Difícil / Difícil | aceptar | pendiente |
| 669 | reto | Li+Br (D) | Difícil / Difícil | aceptar | pendiente |
| 672 | reto | Li+Re (D) | Normal / Difícil | revisar | pendiente |
| 675 | reto | Br+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 678 | reto | Ha+Re (D) | Difícil / Difícil | aceptar | pendiente |
| 680 | hito | Li+Ha+A2 (MD) | Muy difícil / Muy difícil | revisar | pendiente |
| 686 | reto | Ma+Ho (D) | Normal / Difícil | revisar | pendiente |
| 689 | reto | Ho+B2 (D) | Difícil / Difícil | aceptar | pendiente |
| 692 | reto | Ma+Ha (D) | Difícil / Difícil | aceptar | pendiente |
| 695 | reto | Ho+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 698 | reto | Ag+Ha+B2 (D) | Difícil / Difícil | aceptar | pendiente |
| 700 | hito | Fl+Ha+A2 (MD) «Yarinacocha» [P] | Muy difícil / Muy difícil | aceptar | pendiente |
| 303 | estreno | ★Ag | Normal / — | revisar | pendiente |
| 323 | estreno | ★Tu | Normal / — | aceptar | pendiente |
| 343 | estreno | ★B2 | Normal / — | aceptar | pendiente |
| 347 | estreno | ★A2 | Normal / — | revisar | pendiente |
| 363 | estreno | ★La | Normal / — | aceptar | pendiente |
| 403 | estreno | ★Fl | Normal / — | aceptar | pendiente |
