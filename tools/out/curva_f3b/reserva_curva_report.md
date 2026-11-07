# Reserva del 20 %: campaign_v3

Generado por `dotnet run --project tools/level-generator -c Release -- --reserve tools/out/curva_f3b/curva_07_v1.json tools/out/curva_f3b/curva_08_v1.json tools/out/curva_f3b/curva_09_v1.json --spec tools/level-generator/specs/campaign_v3.json --out tools/out/curva_f3b --report tools/out/curva_f3b/reserva_curva_report.md`: no se edita a mano.
QA: `dotnet run --project tools/level-qa -c Release -- --reserve <reserva_NN_v1.json> --campaign <mundo_NN_v1.json> --spec tools/level-generator/specs/campaign_v3.json`.

Fuente: D-033, añadido «la reserva del 20 %» (Q-B11-7 (a)); 03 §17; D-020 (pregunta 31 b). Un pack por mundo (`kind` = `reserve`, `pack_schema` 4) con, para los niveles que usan una mecánica con kill switch (`mech_enabled_*`, 08 §9), otro tablero del mismo slot sin mecánicas y en su ventana, hasta el 20 % de los niveles del mundo. El cliente solo los usa desde `level_overrides`.

Reglas [P] de esta herramienta: orden por rondas sobre los kill switches de 08 §9 (cada mecánica toma su primer nivel aún sin elegir); tablero con la celda sin mecánicas (ni Aguas turbias) y con sus capacidades; entre los candidatos en la ventana con el par en el objetivo del rango ±30 %, el de la misma etiqueta que el publicado y el `bot_fail` más cercano; alivio desde el 31; sin `D` (no entra en el pool del rango).

## Resumen

| Mundo | Pack | Niveles del mundo | Con mecánica | Tope (20 %) | Tableros | Saltados | Bytes | SHA-256 |
|---|---|---|---|---|---|---|---|---|
| 7 (301-420) | `reserva_curva_07_v1.json` | 120 | 75 | 24 | 24 | 0 | 30207 | `bd5c967f40dc2df2720317bd9f13908969c43b15aed2ddb33657065bb8a9006f` |
| 8 (421-560) | `reserva_curva_08_v1.json` | 140 | 82 | 28 | 28 | 0 | 35558 | `fa8a22e1b03100bcd2370a63f5358281a841f151b1846db052d9b35edf27cbc1` |
| 9 (561-700) | `reserva_curva_09_v1.json` | 140 | 81 | 28 | 28 | 0 | 35184 | `3c445ee935a81467b3b976969e66d24176a243b12f3343b6fbf3ed48a5603e81` |

## Cobertura por mecánica

Niveles del mundo que usan cada kill switch y cuántos tienen tablero de reserva (el primero sin cubrir, si lo hay).

| Mundo | Mecánica | Niveles | Con reserva | Primer nivel sin reserva |
|---|---|---|---|---|
| 7 | `hoja` | 11 | 2 | 332 |
| 7 | `matapi` | 5 | 3 | 331 |
| 7 | `liana` | 17 | 2 | 358 |
| 7 | `remolino` | 2 | 2 | — |
| 7 | `barro` | 14 | 2 | 332 |
| 7 | `lluvia` | 6 | 4 | 406 |
| 7 | `brote` | 18 | 5 | 315 |
| 7 | `pirana` | 2 | 1 | 380 |
| 7 | `aguape` | 25 | 10 | 314 |
| 7 | `lancha` | 11 | 4 | 372 |
| 8 | `hoja` | 24 | 6 | 457 |
| 8 | `matapi` | 8 | 5 | 486 |
| 8 | `liana` | 20 | 4 | 469 |
| 8 | `remolino` | 10 | 5 | 500 |
| 8 | `barro` | 12 | 4 | 477 |
| 8 | `lluvia` | 4 | 3 | 551 |
| 8 | `brote` | 16 | 6 | 465 |
| 8 | `pirana` | 8 | 5 | 523 |
| 8 | `aguape` | 18 | 5 | 457 |
| 8 | `lancha` | 14 | 3 | 454 |
| 9 | `hoja` | 27 | 6 | 600 |
| 9 | `matapi` | 13 | 4 | 623 |
| 9 | `liana` | 15 | 5 | 617 |
| 9 | `remolino` | 8 | 5 | 658 |
| 9 | `barro` | 12 | 5 | 600 |
| 9 | `lluvia` | 5 | 3 | 620 |
| 9 | `brote` | 20 | 8 | 597 |
| 9 | `pirana` | 8 | 4 | 643 |
| 9 | `aguape` | 22 | 7 | 600 |
| 9 | `lancha` | 13 | 3 | 594 |

## Mundo 7: `reserva_curva_07_v1.json`

| Slot | Sustituye | Mecánicas del publicado | Rol | Celda | Ventana | Publicado | Reserva | K | Cuias | Vacíos | Par | Alivio | Ronda |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 303 | `m07_v1_n0303_c2` | aguape | estreno | *Ag | [0.00, 0.20) | normal 0.000 | normal 0.025 | 8 | 10 | 2 (2) | 18 | minus_color, plus_empty | 1 |
| 304 | `m07_v1_n0304_c2` | aguape | estreno | *Ag | [0.00, 0.20) | normal 0.050 | normal 0.050 | 8 | 10 | 2 (2) | 19 | minus_color, plus_empty | 1 |
| 305 | `m07_v1_n0305_c2` | aguape | estreno | *Ag | [0.00, 0.20) | normal 0.150 | normal 0.175 | 8 | 10 | 2 (2) | 16 | minus_color, plus_empty | 1 |
| 306 | `m07_v1_n0306_c2` | hoja+lluvia | reto | Ha+Ll (D) | [0.75, 0.90) | hard 0.800 | hard 0.800 | 9 | 11 | 2 (2) | 26 | minus_color, plus_empty | 1 |
| 308 | `m07_v1_n0308_c2` | aguape | comodidad | Ag | [0.20, 0.60] | normal 0.500 | normal 0.525 | 8 | 10 | 2 (2) | 20 | minus_color, plus_empty | 1 |
| 309 | `m07_v1_n0309_c2` | lluvia+aguape | reto | Ag+Ll (D) | [0.75, 0.90) | hard 0.875 | hard 0.850 | 9 | 10 | 1 (1) | 20 | plus_empty, minus_color | 1 |
| 311 | `m07_v1_n0311_c2` | barro | comodidad | Ba | [0.20, 0.60] | normal 0.300 | normal 0.325 | 8 | 10 | 2 (2) | 17 | minus_color, plus_empty | 1 |
| 312 | `m07_v1_n0312_c2` | brote+aguape | reto | Ag+Br (D) | [0.75, 0.90) | hard 0.850 | hard 0.850 | 9 | 11 | 2 (2) | 28 | minus_color, plus_empty | 1 |
| 317 | `m07_v1_n0317_c2` | lluvia | comodidad | Ll | [0.20, 0.60] | normal 0.425 | normal 0.400 | 8 | 10 | 2 (2) | 18 | minus_color, plus_empty | 1 |
| 318 | `m07_v1_n0318_c2` | liana+aguape | reto | Ag+Ln (D) | [0.75, 0.90) | hard 0.800 | hard 0.825 | 9 | 11 | 2 (2) | 24 | minus_color, plus_empty | 1 |
| 320 | `m07_v1_n0320_c2` | hoja+lluvia+brote | hito | Ha+Ll+Br (MD) | [0.90, 1.00] | very_hard 0.925 | very_hard 0.950 | 11 | 12 | 1 (1) | 24 | plus_empty, minus_color | 1 |
| 325 | `m07_v1_n0325_c2` | barro | comodidad | Ba | [0.20, 0.60] | normal 0.325 | normal 0.375 | 8 | 10 | 2 (2) | 18 | minus_color, plus_empty | 1 |
| 326 | `m07_v1_n0326_c2` | matapi+aguape | reto | Ag+Ma (D) | [0.75, 0.90) | hard 0.750 | hard 0.775 | 9 | 11 | 2 (2) | 27 | minus_color, plus_empty | 1 |
| 329 | `m07_v1_n0329_c2` | matapi | reto | Ma (D) | [0.75, 0.90) | normal 0.725 | hard 0.800 | 10 | 12 | 2 (2) | 33 | minus_color, plus_empty | 1 |
| 346 | `m07_v1_n0346_c2` | liana+brote | reto | B2+Ln (D) | [0.75, 0.90) | hard 0.775 | hard 0.775 | 10 | 12 | 2 (2) | 31 | minus_color, plus_empty | 1 |
| 347 | `m07_v1_n0347_c2` | aguape | estreno | *A2 | [0.00, 0.20) | normal 0.100 | normal 0.100 | 8 | 10 | 2 (2) | 20 | minus_color, plus_empty | 1 |
| 348 | `m07_v1_n0348_c2` | aguape | estreno | *A2 | [0.00, 0.20) | normal 0.100 | normal 0.125 | 8 | 10 | 2 (2) | 18 | minus_color, plus_empty | 1 |
| 352 | `m07_v1_n0352_c2` | remolino+brote | reto | B2+Re (D) | [0.75, 0.90) | normal 0.575 | hard 0.775 | 9 | 11 | 2 (2) | 28 | minus_color, plus_empty | 1 |
| 355 | `m07_v1_n0355_c2` | remolino+brote | reto | B2+Re (D) | [0.75, 0.90) | hard 0.875 | hard 0.875 | 9 | 11 | 2 (2) | 29 | minus_color, plus_empty | 1 |
| 363 | `m07_v1_n0363_c2` | lancha | estreno | *La | [0.00, 0.20) | normal 0.000 | normal 0.000 | 6 | 9 | 3 (3) | 12 | minus_color, plus_empty | 1 |
| 364 | `m07_v1_n0364_c2` | lancha | estreno | *La | [0.00, 0.20) | normal 0.000 | normal 0.000 | 6 | 9 | 3 (3) | 15 | minus_color, plus_empty | 1 |
| 365 | `m07_v1_n0365_c2` | lancha | estreno | *La | [0.00, 0.20) | normal 0.000 | normal 0.000 | 6 | 9 | 3 (3) | 13 | minus_color, plus_empty | 1 |
| 369 | `m07_v1_n0369_c2` | matapi+pirana | reto | Pi+Ma (D) | [0.75, 0.90) | hard 0.750 | hard 0.775 | 9 | 11 | 2 (2) | 29 | minus_color, plus_empty | 1 |
| 371 | `m07_v1_n0371_c2` | lancha | comodidad | La | [0.20, 0.60] | normal 0.350 | normal 0.250 | 8 | 9 | 1 (1) | 18 | minus_color | 1 |

## Mundo 8: `reserva_curva_08_v1.json`

| Slot | Sustituye | Mecánicas del publicado | Rol | Celda | Ventana | Publicado | Reserva | K | Cuias | Vacíos | Par | Alivio | Ronda |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 423 | `m08_v1_n0423_c2` | matapi | comodidad | Ma+Tu | [0.20, 0.60] | normal 0.475 | normal 0.500 | 8 | 10 | 2 (2) | 19 | minus_color, plus_empty | 1 |
| 425 | `m08_v1_n0425_c2` | lancha | comodidad | La | [0.20, 0.60] | normal 0.300 | normal 0.425 | 8 | 10 | 2 (2) | 18 | minus_color, plus_empty | 1 |
| 426 | `m08_v1_n0426_c2` | matapi+liana | reto | Fl+Ma (D) | [0.75, 0.90) | hard 0.775 | hard 0.800 | 9 | 11 | 2 (2) | 29 | minus_color, plus_empty | 1 |
| 428 | `m08_v1_n0428_c2` | barro | comodidad | Ba+Tu | [0.20, 0.60] | normal 0.575 | normal 0.250 | 8 | 10 | 2 (2) | 16 | minus_color, plus_empty | 1 |
| 429 | `m08_v1_n0429_c2` | matapi+remolino+aguape | reto | Ag+Ma+Re (D) | [0.75, 0.90) | hard 0.750 | hard 0.750 | 9 | 11 | 2 (2) | 26 | minus_color, plus_empty | 1 |
| 431 | `m08_v1_n0431_c2` | brote+aguape | comodidad | Ag+B2 | [0.20, 0.60] | normal 0.575 | normal 0.300 | 8 | 10 | 2 (2) | 20 | minus_color, plus_empty | 1 |
| 432 | `m08_v1_n0432_c2` | hoja+liana | reto | Fl+Ho (D) | [0.75, 0.90) | hard 0.750 | hard 0.775 | 9 | 11 | 2 (2) | 29 | minus_color, plus_empty | 1 |
| 435 | `m08_v1_n0435_c2` | matapi | reto | Ma+Tu (D) | [0.75, 0.90) | hard 0.875 | hard 0.825 | 9 | 11 | 2 (2) | 28 | minus_color, plus_empty | 1 |
| 437 | `m08_v1_n0437_c2` | hoja+lancha | comodidad | La+Ha | [0.20, 0.60] | normal 0.450 | normal 0.425 | 8 | 10 | 2 (2) | 18 | minus_color, plus_empty | 1 |
| 438 | `m08_v1_n0438_c2` | hoja+remolino | reto | Ho+Re (D) | [0.75, 0.90) | hard 0.775 | hard 0.750 | 9 | 11 | 2 (2) | 28 | minus_color, plus_empty | 1 |
| 440 | `m08_v1_n0440_c2` | hoja+remolino+aguape | hito | Ag+Ho+Re (MD) | [0.90, 1.00] | very_hard 1.000 | very_hard 1.000 | 11 | 12 | 1 (1) | 23 | plus_empty, minus_color | 1 |
| 443 | `m08_v1_n0443_c2` | pirana | comodidad | Pi | [0.20, 0.60] | normal 0.350 | normal 0.350 | 8 | 10 | 2 (2) | 20 | minus_color, plus_empty | 1 |
| 444 | `m08_v1_n0444_c2` | lancha | comodidad | La | [0.20, 0.60] | normal 0.325 | normal 0.325 | 8 | 10 | 2 (2) | 18 | minus_color, plus_empty | 1 |
| 445 | `m08_v1_n0445_c2` | brote | comodidad | Br+Tu | [0.20, 0.60] | normal 0.475 | normal 0.450 | 8 | 10 | 2 (2) | 17 | minus_color, plus_empty | 1 |
| 446 | `m08_v1_n0446_c2` | barro+pirana | reto | Pi+Ba (D) | [0.75, 0.90) | hard 0.875 | hard 0.850 | 9 | 11 | 2 (2) | 26 | minus_color, plus_empty | 1 |
| 448 | `m08_v1_n0448_c2` | hoja | comodidad | Ho+Tu | [0.20, 0.60] | normal 0.575 | normal 0.575 | 8 | 10 | 2 (2) | 16 | minus_color, plus_empty | 1 |
| 449 | `m08_v1_n0449_c2` | remolino+brote | reto | Br+Re (D) | [0.75, 0.90) | normal 0.725 | hard 0.750 | 9 | 10 | 1 (1) | 21 | plus_empty, minus_color | 1 |
| 451 | `m08_v1_n0451_c2` | pirana | comodidad | Pi | [0.20, 0.60] | normal 0.225 | normal 0.350 | 8 | 9 | 1 (1) | 17 | plus_empty, minus_color | 1 |
| 452 | `m08_v1_n0452_c2` | brote+pirana+aguape | reto | Pi+Br+Ag (D) | [0.75, 0.90) | hard 0.825 | hard 0.800 | 9 | 10 | 1 (1) | 18 | plus_empty, minus_color | 1 |
| 455 | `m08_v1_n0455_c2` | brote | reto | Br+Tu (D) | [0.75, 0.90) | hard 0.775 | hard 0.775 | 9 | 11 | 2 (2) | 31 | minus_color, plus_empty | 1 |
| 458 | `m08_v1_n0458_c2` | remolino+barro | reto | Ba+Re (D) | [0.75, 0.90) | hard 0.800 | hard 0.825 | 9 | 11 | 2 (2) | 28 | minus_color, plus_empty | 1 |
| 460 | `m08_v1_n0460_c2` | barro+pirana+aguape | hito | Pi+Ba+Ag (MD) | [0.90, 1.00] | very_hard 1.000 | very_hard 1.000 | 11 | 12 | 1 (1) | 34 | plus_empty, minus_color | 1 |
| 463 | `m08_v1_n0463_c2` | liana | comodidad | Li | [0.20, 0.60] | normal 0.425 | normal 0.500 | 8 | 10 | 2 (2) | 22 | minus_color, plus_empty | 1 |
| 466 | `m08_v1_n0466_c2` | liana+brote | reto | Li+B2 (D) | [0.75, 0.90) | hard 0.875 | hard 0.875 | 9 | 11 | 2 (2) | 28 | minus_color, plus_empty | 1 |
| 483 | `m08_v1_n0483_c2` | matapi | comodidad | Ma+Tu | [0.20, 0.60] | normal 0.500 | normal 0.350 | 8 | 10 | 2 (2) | 19 | minus_color, plus_empty | 1 |
| 503 | `m08_v1_n0503_c2` | lluvia | comodidad | Ll | [0.20, 0.60] | normal 0.350 | normal 0.300 | 8 | 10 | 2 (2) | 17 | minus_color, plus_empty | 1 |
| 512 | `m08_v1_n0512_c2` | hoja+lluvia | reto | Ll+Ha (D) | [0.75, 0.90) | hard 0.875 | hard 0.850 | 9 | 11 | 2 (2) | 30 | minus_color, plus_empty | 1 |
| 514 | `m08_v1_n0514_c2` | lluvia | comodidad | Ll+Al | [0.20, 0.60] | normal 0.250 | normal 0.275 | 8 | 9 | 1 (1) | 17 | minus_color | 1 |

## Mundo 9: `reserva_curva_09_v1.json`

| Slot | Sustituye | Mecánicas del publicado | Rol | Celda | Ventana | Publicado | Reserva | K | Cuias | Vacíos | Par | Alivio | Ronda |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 563 | `m09_v1_n0563_c2` | matapi | comodidad | Ma+Tu | [0.20, 0.60] | normal 0.325 | normal 0.275 | 8 | 10 | 2 (2) | 16 | minus_color, plus_empty | 1 |
| 565 | `m09_v1_n0565_c2` | brote+lancha | comodidad | La+B2 | [0.20, 0.60] | normal 0.350 | normal 0.425 | 8 | 10 | 2 (2) | 17 | minus_color, plus_empty | 1 |
| 566 | `m09_v1_n0566_c2` | matapi+remolino+aguape | reto | Ag+Ma+Re (D) | [0.75, 0.90) | hard 0.750 | hard 0.750 | 9 | 10 | 1 (1) | 21 | plus_empty, minus_color | 1 |
| 568 | `m09_v1_n0568_c2` | hoja+aguape | comodidad | Ag+Ha | [0.20, 0.60] | normal 0.500 | normal 0.600 | 8 | 9 | 1 (1) | 16 | plus_empty, minus_color | 1 |
| 569 | `m09_v1_n0569_c2` | barro+brote | reto | B2+Ba (D) | [0.75, 0.90) | hard 0.750 | hard 0.800 | 9 | 11 | 2 (2) | 28 | minus_color, plus_empty | 1 |
| 570 | `m09_v1_n0570_c2` | lancha | comodidad | La | [0.20, 0.60] | normal 0.225 | normal 0.225 | 8 | 10 | 2 (2) | 19 | minus_color, plus_empty | 1 |
| 571 | `m09_v1_n0571_c2` | hoja | comodidad | Ho+Tu | [0.20, 0.60] | normal 0.350 | normal 0.325 | 8 | 10 | 2 (2) | 19 | minus_color, plus_empty | 1 |
| 572 | `m09_v1_n0572_c2` | matapi+barro | reto | Ma+Ba (D) | [0.75, 0.90) | hard 0.800 | hard 0.800 | 9 | 11 | 2 (2) | 28 | minus_color, plus_empty | 1 |
| 575 | `m09_v1_n0575_c2` | matapi | reto | Ma+Tu (D) | [0.75, 0.90) | hard 0.800 | hard 0.800 | 9 | 11 | 2 (2) | 26 | minus_color, plus_empty | 1 |
| 577 | `m09_v1_n0577_c2` | brote | comodidad | Br+Tu | [0.20, 0.60] | normal 0.275 | normal 0.250 | 8 | 9 | 1 (1) | 18 | minus_color | 1 |
| 578 | `m09_v1_n0578_c2` | remolino+brote | reto | B2+Re (D) | [0.75, 0.90) | hard 0.775 | hard 0.800 | 9 | 11 | 2 (2) | 27 | minus_color, plus_empty | 1 |
| 580 | `m09_v1_n0580_c2` | barro+brote+aguape | hito | Ba+B2+Ag (MD) | [0.90, 1.00] | very_hard 1.000 | very_hard 1.000 | 11 | 12 | 1 (1) | 30 | minus_color | 1 |
| 583 | `m09_v1_n0583_c2` | pirana | comodidad | Pi | [0.20, 0.60] | normal 0.450 | normal 0.500 | 8 | 9 | 1 (1) | 17 | plus_empty, minus_color | 1 |
| 585 | `m09_v1_n0585_c2` | hoja+aguape | comodidad | Ag+Ha | [0.20, 0.60] | normal 0.400 | normal 0.425 | 8 | 10 | 2 (2) | 17 | minus_color, plus_empty | 1 |
| 586 | `m09_v1_n0586_c2` | hoja+pirana | reto | Pi+Ha (D) | [0.75, 0.90) | hard 0.825 | hard 0.850 | 9 | 10 | 1 (1) | 21 | plus_empty, minus_color | 1 |
| 588 | `m09_v1_n0588_c2` | hoja | comodidad | Ho+Tu | [0.20, 0.60] | normal 0.275 | normal 0.250 | 8 | 10 | 2 (2) | 18 | minus_color, plus_empty | 1 |
| 589 | `m09_v1_n0589_c2` | hoja+brote | reto | Ha+B2 (D) | [0.75, 0.90) | hard 0.875 | hard 0.825 | 9 | 11 | 2 (2) | 30 | minus_color, plus_empty | 1 |
| 591 | `m09_v1_n0591_c2` | lancha | comodidad | La | [0.20, 0.60] | normal 0.375 | normal 0.375 | 8 | 10 | 2 (2) | 18 | minus_color, plus_empty | 1 |
| 592 | `m09_v1_n0592_c2` | brote+pirana+aguape | reto | Pi+B2+Ag (D) | [0.75, 0.90) | hard 0.775 | hard 0.775 | 9 | 11 | 2 (2) | 27 | minus_color, plus_empty | 1 |
| 595 | `m09_v1_n0595_c2` | barro | reto | Ba+Tu (D) | [0.75, 0.90) | hard 0.875 | hard 0.875 | 9 | 10 | 1 (1) | 20 | plus_empty, minus_color | 1 |
| 598 | `m09_v1_n0598_c2` | barro+pirana | reto | Pi+Ba (D) | [0.75, 0.90) | hard 0.875 | hard 0.875 | 9 | 11 | 2 (2) | 27 | minus_color, plus_empty | 1 |
| 603 | `m09_v1_n0603_c2` | liana | comodidad | Ln | [0.20, 0.60] | normal 0.475 | normal 0.450 | 8 | 10 | 2 (2) | 17 | minus_color, plus_empty | 1 |
| 606 | `m09_v1_n0606_c2` | liana+remolino | reto | Ln+Re (D) | [0.75, 0.90) | hard 0.800 | hard 0.800 | 9 | 11 | 2 (2) | 28 | minus_color, plus_empty | 1 |
| 609 | `m09_v1_n0609_c2` | liana+lluvia | reto | Li+Ll (D) | [0.75, 0.90) | hard 0.800 | hard 0.825 | 9 | 11 | 2 (2) | 29 | minus_color, plus_empty | 1 |
| 612 | `m09_v1_n0612_c2` | lluvia+aguape | reto | Ag+Ll (D) | [0.75, 0.90) | hard 0.875 | hard 0.875 | 9 | 11 | 2 (2) | 29 | minus_color, plus_empty | 1 |
| 615 | `m09_v1_n0615_c2` | liana+remolino | reto | Li+Re (D) | [0.75, 0.90) | hard 0.850 | hard 0.850 | 9 | 10 | 1 (1) | 20 | plus_empty, minus_color | 1 |
| 618 | `m09_v1_n0618_c2` | liana+lluvia | reto | Ln+Ll (D) | [0.75, 0.90) | hard 0.850 | hard 0.850 | 9 | 11 | 2 (2) | 25 | minus_color, plus_empty | 1 |
| 652 | `m09_v1_n0652_c2` | remolino+brote+aguape | reto | Ag+Br+Re (D) | [0.75, 0.90) | hard 0.825 | hard 0.800 | 9 | 11 | 2 (2) | 27 | minus_color, plus_empty | 1 |
