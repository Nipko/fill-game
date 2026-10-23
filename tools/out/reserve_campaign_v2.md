# Reserva del 20 %: campaign_v2

Generado por `dotnet run --project tools/level-generator -c Release -- --reserve Assets/_Project/Data/Levels/mundo_06_v1.json Assets/_Project/Data/Levels/mundo_07_v1.json Assets/_Project/Data/Levels/mundo_08_v1.json Assets/_Project/Data/Levels/mundo_09_v1.json --spec tools/level-generator/specs/campaign_v2.json --out Assets/_Project/Data/Levels --report tools/out/reserve_campaign_v2.md`: no se edita a mano.
QA: `dotnet run --project tools/level-qa -c Release -- --reserve <reserva_NN_v1.json> --campaign <mundo_NN_v1.json> --spec tools/level-generator/specs/campaign_v2.json`.

Fuente: D-033, añadido «la reserva del 20 %» (Q-B11-7 (a)); 03 §17; D-020 (pregunta 31 b). Un pack por mundo (`kind` = `reserve`, `pack_schema` 4) con, para los niveles que usan una mecánica con kill switch (`mech_enabled_*`, 08 §9), otro tablero del mismo slot sin mecánicas y en su ventana, hasta el 20 % de los niveles del mundo. El cliente solo los usa desde `level_overrides`.

Reglas [P] de esta herramienta: orden por rondas sobre los kill switches de 08 §9 (cada mecánica toma su primer nivel aún sin elegir); tablero con la celda sin mecánicas (ni Aguas turbias) y con sus capacidades; entre los candidatos en la ventana con el par en el objetivo del rango ±30 %, el de la misma etiqueta que el publicado y el `bot_fail` más cercano; alivio desde el 31; sin `D` (no entra en el pool del rango).

## Resumen

| Mundo | Pack | Niveles del mundo | Con mecánica | Tope (20 %) | Tableros | Saltados | Bytes | SHA-256 |
|---|---|---|---|---|---|---|---|---|
| 6 (201-300) | `reserva_06_v1.json` | 100 | 57 | 20 | 20 | 0 | 26606 | `b021f854b983ac30fb98aaa738408fcf45cb1ac557216e17c16268287fe2472b` |
| 7 (301-420) | `reserva_07_v1.json` | 120 | 71 | 24 | 24 | 0 | 33299 | `b88b3fe1da1a686ab59d58d26bc6aee64fc3870070de423ea41d60270e2e265b` |
| 8 (421-560) | `reserva_08_v1.json` | 140 | 77 | 28 | 28 | 0 | 37695 | `0d89f0ee6047d3f28564290e7ee7676af810ecb5ce1bd948d4927d91d751db59` |
| 9 (561-700) | `reserva_09_v1.json` | 140 | 75 | 28 | 28 | 0 | 38132 | `a096e74e99483bba856144f01f6915334d8e5976a46613736d876f7586c7453c` |

## Cobertura por mecánica

Niveles del mundo que usan cada kill switch y cuántos tienen tablero de reserva (el primero sin cubrir, si lo hay).

| Mundo | Mecánica | Niveles | Con reserva | Primer nivel sin reserva |
|---|---|---|---|---|
| 6 | `hoja` | 16 | 5 | 240 |
| 6 | `matapi` | 5 | 3 | 255 |
| 6 | `liana` | 17 | 4 | 243 |
| 6 | `remolino` | 9 | 5 | 252 |
| 6 | `barro` | 11 | 3 | 220 |
| 6 | `lluvia` | 5 | 4 | 231 |
| 6 | `brote` | 9 | 2 | 235 |
| 6 | `pirana` | 7 | 3 | 266 |
| 7 | `hoja` | 19 | 5 | 335 |
| 7 | `matapi` | 6 | 3 | 331 |
| 7 | `liana` | 18 | 3 | 358 |
| 7 | `remolino` | 7 | 3 | 355 |
| 7 | `barro` | 15 | 4 | 337 |
| 7 | `lluvia` | 8 | 6 | 406 |
| 7 | `brote` | 22 | 9 | 340 |
| 7 | `pirana` | 5 | 3 | 378 |
| 8 | `hoja` | 29 | 7 | 457 |
| 8 | `matapi` | 10 | 6 | 486 |
| 8 | `liana` | 18 | 5 | 472 |
| 8 | `remolino` | 11 | 5 | 488 |
| 8 | `barro` | 13 | 5 | 477 |
| 8 | `lluvia` | 6 | 3 | 514 |
| 8 | `brote` | 16 | 6 | 455 |
| 8 | `pirana` | 9 | 5 | 523 |
| 9 | `hoja` | 27 | 5 | 589 |
| 9 | `matapi` | 16 | 6 | 626 |
| 9 | `liana` | 12 | 7 | 663 |
| 9 | `remolino` | 14 | 7 | 652 |
| 9 | `barro` | 11 | 4 | 600 |
| 9 | `lluvia` | 5 | 4 | 629 |
| 9 | `brote` | 24 | 6 | 589 |
| 9 | `pirana` | 8 | 4 | 643 |

## Mundo 6: `reserva_06_v1.json`

| Slot | Sustituye | Mecánicas del publicado | Rol | Celda | Ventana | Publicado | Reserva | K | Cuias | Vacíos | Par | Alivio | Ronda |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 203 | `m06_v1_n0203` | liana | comodidad | Li | [0.20, 0.60] | normal 0.425 | normal 0.475 | 10 | 12 | 2 (2) | 22 | minus_color, plus_empty | 1 |
| 205 | `m06_v1_n0205` | remolino | comodidad | Re | [0.20, 0.60] | normal 0.300 | normal 0.325 | 10 | 12 | 2 (2) | 21 | minus_color, plus_empty | 1 |
| 206 | `m06_v1_n0206` | barro+pirana | reto | Pi+Ba (D) | [0.75, 0.90) | hard 0.800 | hard 0.800 | 10 | 11 | 1 (1) | 22 | plus_empty, minus_color | 1 |
| 208 | `m06_v1_n0208` | brote | comodidad | Br | [0.20, 0.60] | normal 0.250 | normal 0.250 | 10 | 12 | 2 (2) | 20 | minus_color, plus_empty | 1 |
| 209 | `m06_v1_n0209` | liana+remolino | reto | Li+Re (D) | [0.75, 0.90) | hard 0.875 | hard 0.875 | 10 | 12 | 2 (2) | 32 | minus_color, plus_empty | 1 |
| 211 | `m06_v1_n0211` | liana | comodidad | Li | [0.20, 0.60] | normal 0.550 | normal 0.400 | 10 | 12 | 2 (2) | 23 | minus_color, plus_empty | 1 |
| 212 | `m06_v1_n0212` | pirana | reto | Pi (D) | [0.75, 0.90) | hard 0.850 | hard 0.850 | 10 | 12 | 2 (2) | 28 | minus_color, plus_empty | 1 |
| 215 | `m06_v1_n0215` | remolino+barro | reto | Re+Ba (D) | [0.75, 0.90) | hard 0.875 | hard 0.850 | 10 | 11 | 1 (1) | 25 | plus_empty, minus_color | 1 |
| 217 | `m06_v1_n0217` | hoja | comodidad | Ho | [0.20, 0.60] | normal 0.425 | normal 0.425 | 10 | 12 | 2 (2) | 22 | minus_color, plus_empty | 1 |
| 218 | `m06_v1_n0218` | liana+barro | reto | Li+Ba (D) | [0.75, 0.90) | hard 0.825 | hard 0.825 | 10 | 12 | 2 (2) | 32 | minus_color, plus_empty | 1 |
| 223 | `m06_v1_n0223` | lluvia | comodidad | Ll | [0.20, 0.60] | normal 0.300 | normal 0.375 | 10 | 12 | 2 (2) | 21 | minus_color, plus_empty | 1 |
| 225 | `m06_v1_n0225` | hoja | comodidad | Ho | [0.20, 0.60] | normal 0.225 | normal 0.275 | 10 | 12 | 2 (2) | 21 | minus_color, plus_empty | 1 |
| 226 | `m06_v1_n0226` | hoja+lluvia | reto | Ll+Ho (D) | [0.75, 0.90) | hard 0.800 | hard 0.775 | 10 | 12 | 2 (2) | 30 | minus_color, plus_empty | 1 |
| 229 | `m06_v1_n0229` | lluvia+brote | reto | Br+Ll (D) | [0.75, 0.90) | hard 0.850 | hard 0.875 | 10 | 12 | 2 (2) | 32 | minus_color, plus_empty | 1 |
| 232 | `m06_v1_n0232` | hoja+matapi | reto | Ma+Ho (D) | [0.75, 0.90) | hard 0.775 | hard 0.800 | 10 | 12 | 2 (2) | 33 | minus_color, plus_empty | 1 |
| 237 | `m06_v1_n0237` | remolino | comodidad | Re | [0.20, 0.60] | normal 0.475 | normal 0.550 | 10 | 12 | 2 (2) | 20 | minus_color, plus_empty | 1 |
| 238 | `m06_v1_n0238` | hoja+lluvia | reto | Ll+Ho (D) | [0.75, 0.90) | hard 0.875 | hard 0.875 | 10 | 12 | 2 (2) | 32 | minus_color, plus_empty | 1 |
| 246 | `m06_v1_n0246` | matapi+remolino | reto | Ma+Re (D) | [0.75, 0.90) | normal 0.600 | hard 0.750 | 10 | 12 | 2 (2) | 29 | minus_color, plus_empty | 1 |
| 251 | `m06_v1_n0251` | matapi | comodidad | Ma | [0.20, 0.60] | normal 0.550 | normal 0.525 | 10 | 12 | 2 (2) | 21 | minus_color, plus_empty | 1 |
| 263 | `m06_v1_n0263` | pirana | comodidad | Pi | [0.20, 0.60] | normal 0.400 | normal 0.425 | 10 | 12 | 2 (2) | 20 | minus_color, plus_empty | 1 |

## Mundo 7: `reserva_07_v1.json`

| Slot | Sustituye | Mecánicas del publicado | Rol | Celda | Ventana | Publicado | Reserva | K | Cuias | Vacíos | Par | Alivio | Ronda |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 303 | `m07_v1_n0303` | lluvia | comodidad | Ll | [0.20, 0.60] | normal 0.250 | normal 0.400 | 11 | 13 | 2 (2) | 22 | minus_color, plus_empty | 1 |
| 305 | `m07_v1_n0305` | brote | comodidad | Br | [0.20, 0.60] | normal 0.200 | normal 0.200 | 11 | 13 | 2 (2) | 22 | minus_color, plus_empty | 1 |
| 306 | `m07_v1_n0306` | hoja+lluvia | reto | Ha+Ll (D) | [0.75, 0.90) | hard 0.850 | hard 0.850 | 11 | 13 | 2 (2) | 36 | minus_color, plus_empty | 1 |
| 308 | `m07_v1_n0308` | remolino | comodidad | Re | [0.20, 0.60] | normal 0.525 | normal 0.450 | 11 | 13 | 2 (2) | 25 | minus_color, plus_empty | 1 |
| 309 | `m07_v1_n0309` | lluvia+brote | reto | Br+Ll (D) | [0.75, 0.90) | hard 0.800 | hard 0.800 | 11 | 13 | 2 (2) | 33 | minus_color, plus_empty | 1 |
| 311 | `m07_v1_n0311` | barro | comodidad | Ba | [0.20, 0.60] | normal 0.525 | normal 0.525 | 11 | 13 | 2 (2) | 25 | minus_color, plus_empty | 1 |
| 312 | `m07_v1_n0312` | hoja+brote | reto | Ha+Br (D) | [0.75, 0.90) | hard 0.875 | hard 0.875 | 11 | 13 | 2 (2) | 36 | minus_color, plus_empty | 1 |
| 314 | `m07_v1_n0314` | hoja | comodidad | Ho | [0.20, 0.60] | normal 0.550 | normal 0.450 | 11 | 13 | 2 (2) | 23 | minus_color, plus_empty | 1 |
| 315 | `m07_v1_n0315` | brote | reto | Br+Tu (D) | [0.75, 0.90) | hard 0.775 | hard 0.825 | 11 | 13 | 2 (2) | 35 | minus_color, plus_empty | 1 |
| 317 | `m07_v1_n0317` | lluvia | comodidad | Ll | [0.20, 0.60] | normal 0.300 | normal 0.375 | 11 | 13 | 2 (2) | 23 | minus_color, plus_empty | 1 |
| 318 | `m07_v1_n0318` | liana+lluvia | reto | Ln+Ll (D) | [0.75, 0.90) | hard 0.800 | hard 0.825 | 11 | 13 | 2 (2) | 33 | minus_color, plus_empty | 1 |
| 320 | `m07_v1_n0320` | hoja+lluvia+brote | hito | Ha+Ll+Br (MD) | [0.90, 1.00] | very_hard 1.000 | very_hard 1.000 | 11 | 12 | 1 (1) | 23 | plus_empty, minus_color | 1 |
| 323 | `m07_v1_n0323` | matapi | comodidad | Ma | [0.20, 0.60] | normal 0.250 | normal 0.350 | 11 | 13 | 2 (2) | 22 | minus_color, plus_empty | 1 |
| 325 | `m07_v1_n0325` | barro | comodidad | Ba | [0.20, 0.60] | normal 0.325 | normal 0.525 | 11 | 13 | 2 (2) | 22 | minus_color, plus_empty | 1 |
| 326 | `m07_v1_n0326` | matapi+brote | reto | Br+Ma (D) | [0.75, 0.90) | hard 0.850 | hard 0.875 | 11 | 13 | 2 (2) | 35 | minus_color, plus_empty | 1 |
| 329 | `m07_v1_n0329` | matapi | reto | Ma (D) | [0.75, 0.90) | hard 0.850 | hard 0.800 | 11 | 12 | 1 (1) | 23 | plus_empty, minus_color | 1 |
| 332 | `m07_v1_n0332` | hoja+barro | reto | Ba+Ho (D) | [0.75, 0.90) | hard 0.875 | hard 0.875 | 11 | 13 | 2 (2) | 35 | minus_color, plus_empty | 1 |
| 334 | `m07_v1_n0334` | remolino | comodidad | Re | [0.20, 0.60] | normal 0.300 | normal 0.525 | 11 | 13 | 2 (2) | 25 | minus_color, plus_empty | 1 |
| 346 | `m07_v1_n0346` | liana+brote | reto | B2+Ln (D) | [0.75, 0.90) | hard 0.775 | hard 0.750 | 11 | 13 | 2 (2) | 35 | minus_color, plus_empty | 1 |
| 351 | `m07_v1_n0351` | liana | comodidad | Ln | [0.20, 0.60] | normal 0.550 | normal 0.600 | 11 | 13 | 2 (2) | 22 | minus_color, plus_empty | 1 |
| 352 | `m07_v1_n0352` | remolino+brote | reto | B2+Re (D) | [0.75, 0.90) | hard 0.750 | hard 0.750 | 11 | 13 | 2 (2) | 36 | minus_color, plus_empty | 1 |
| 363 | `m07_v1_n0363` | pirana | comodidad | Pi | [0.20, 0.60] | normal 0.500 | normal 0.575 | 11 | 13 | 2 (2) | 22 | minus_color, plus_empty | 1 |
| 369 | `m07_v1_n0369` | brote+pirana | reto | Pi+B2 (D) | [0.75, 0.90) | normal 0.450 | hard 0.750 | 11 | 13 | 2 (2) | 37 | minus_color, plus_empty | 1 |
| 375 | `m07_v1_n0375` | barro+pirana | reto | Pi+Ba (D) | [0.75, 0.90) | hard 0.875 | hard 0.875 | 11 | 13 | 2 (2) | 31 | minus_color, plus_empty | 1 |

## Mundo 8: `reserva_08_v1.json`

| Slot | Sustituye | Mecánicas del publicado | Rol | Celda | Ventana | Publicado | Reserva | K | Cuias | Vacíos | Par | Alivio | Ronda |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 423 | `m08_v1_n0423` | matapi | comodidad | Ma | [0.20, 0.60] | normal 0.400 | normal 0.325 | 11 | 13 | 2 (2) | 23 | minus_color, plus_empty | 1 |
| 425 | `m08_v1_n0425` | hoja | comodidad | Ho | [0.20, 0.60] | normal 0.600 | normal 0.600 | 11 | 13 | 2 (2) | 22 | minus_color, plus_empty | 1 |
| 426 | `m08_v1_n0426` | matapi+liana | reto | Fl+Ma (D) | [0.75, 0.90) | hard 0.775 | hard 0.750 | 10 | 12 | 2 (2) | 30 | minus_color, plus_empty | 1 |
| 428 | `m08_v1_n0428` | barro | comodidad | Ba | [0.20, 0.60] | normal 0.250 | normal 0.325 | 11 | 13 | 2 (2) | 23 | minus_color, plus_empty | 1 |
| 429 | `m08_v1_n0429` | matapi+remolino | reto | Ma+Re (D) | [0.75, 0.90) | normal 0.650 | hard 0.775 | 10 | 12 | 2 (2) | 29 | minus_color, plus_empty | 1 |
| 431 | `m08_v1_n0431` | brote | comodidad | B2 | [0.20, 0.60] | normal 0.425 | normal 0.400 | 11 | 13 | 2 (2) | 24 | minus_color, plus_empty | 1 |
| 432 | `m08_v1_n0432` | hoja+liana | reto | Fl+Ho (D) | [0.75, 0.90) | hard 0.800 | hard 0.800 | 10 | 12 | 2 (2) | 31 | minus_color, plus_empty | 1 |
| 435 | `m08_v1_n0435` | matapi | reto | Ma+Tu (D) | [0.75, 0.90) | hard 0.775 | hard 0.800 | 10 | 12 | 2 (2) | 29 | minus_color, plus_empty | 1 |
| 437 | `m08_v1_n0437` | hoja | comodidad | Ha | [0.20, 0.60] | normal 0.550 | normal 0.450 | 11 | 13 | 2 (2) | 22 | minus_color, plus_empty | 1 |
| 438 | `m08_v1_n0438` | hoja+remolino | reto | Ho+Re (D) | [0.75, 0.90) | hard 0.750 | hard 0.750 | 10 | 12 | 2 (2) | 32 | minus_color, plus_empty | 1 |
| 440 | `m08_v1_n0440` | hoja+matapi | hito | Ma+Ho (MD) | [0.90, 1.00] | very_hard 1.000 | very_hard 1.000 | 11 | 12 | 1 (1) | 25 | minus_color | 1 |
| 443 | `m08_v1_n0443` | pirana | comodidad | Pi | [0.20, 0.60] | normal 0.450 | normal 0.400 | 11 | 13 | 2 (2) | 22 | minus_color, plus_empty | 1 |
| 445 | `m08_v1_n0445` | brote | comodidad | Br | [0.20, 0.60] | normal 0.300 | normal 0.400 | 11 | 13 | 2 (2) | 23 | minus_color, plus_empty | 1 |
| 446 | `m08_v1_n0446` | barro+pirana | reto | Pi+Ba (D) | [0.75, 0.90) | hard 0.800 | hard 0.750 | 10 | 12 | 2 (2) | 29 | minus_color, plus_empty | 1 |
| 448 | `m08_v1_n0448` | hoja | comodidad | Ho | [0.20, 0.60] | normal 0.450 | normal 0.475 | 11 | 13 | 2 (2) | 22 | minus_color, plus_empty | 1 |
| 449 | `m08_v1_n0449` | remolino+brote | reto | Br+Re (D) | [0.75, 0.90) | hard 0.750 | hard 0.750 | 10 | 11 | 1 (1) | 22 | plus_empty, minus_color | 1 |
| 451 | `m08_v1_n0451` | pirana | comodidad | Pi | [0.20, 0.60] | normal 0.375 | normal 0.225 | 11 | 13 | 2 (2) | 22 | minus_color, plus_empty | 1 |
| 452 | `m08_v1_n0452` | brote+pirana | reto | Pi+Br (D) | [0.75, 0.90) | normal 0.600 | hard 0.850 | 10 | 12 | 2 (2) | 31 | minus_color, plus_empty | 1 |
| 458 | `m08_v1_n0458` | remolino+barro | reto | Ba+Re (D) | [0.75, 0.90) | hard 0.775 | hard 0.800 | 10 | 12 | 2 (2) | 32 | minus_color, plus_empty | 1 |
| 460 | `m08_v1_n0460` | barro+pirana | hito | Pi+Ba (MD) | [0.90, 1.00] | very_hard 0.925 | very_hard 0.950 | 11 | 12 | 1 (1) | 22 | plus_empty, minus_color | 1 |
| 463 | `m08_v1_n0463` | liana | comodidad | Li | [0.20, 0.60] | normal 0.575 | normal 0.600 | 11 | 13 | 2 (2) | 24 | minus_color, plus_empty | 1 |
| 466 | `m08_v1_n0466` | liana+brote | reto | Li+B2 (D) | [0.75, 0.90) | normal 0.675 | hard 0.750 | 10 | 12 | 2 (2) | 32 | minus_color, plus_empty | 1 |
| 468 | `m08_v1_n0468` | remolino | comodidad | Re | [0.20, 0.60] | normal 0.525 | normal 0.525 | 11 | 13 | 2 (2) | 23 | minus_color, plus_empty | 1 |
| 469 | `m08_v1_n0469` | liana+brote | reto | Ln+B2 (D) | [0.75, 0.90) | normal 0.725 | hard 0.750 | 10 | 12 | 2 (2) | 32 | minus_color, plus_empty | 1 |
| 483 | `m08_v1_n0483` | matapi | comodidad | Ma | [0.20, 0.60] | normal 0.425 | normal 0.375 | 11 | 13 | 2 (2) | 22 | minus_color, plus_empty | 1 |
| 503 | `m08_v1_n0503` | lluvia | comodidad | Ll | [0.20, 0.60] | normal 0.250 | normal 0.375 | 11 | 13 | 2 (2) | 25 | minus_color, plus_empty | 1 |
| 506 | `m08_v1_n0506` | barro+lluvia | reto | Ll+Ba (D) | [0.75, 0.90) | hard 0.800 | hard 0.800 | 10 | 12 | 2 (2) | 31 | minus_color, plus_empty | 1 |
| 512 | `m08_v1_n0512` | hoja+lluvia | reto | Ll+Ha (D) | [0.75, 0.90) | hard 0.850 | hard 0.875 | 10 | 11 | 1 (1) | 20 | plus_empty, minus_color | 1 |

## Mundo 9: `reserva_09_v1.json`

| Slot | Sustituye | Mecánicas del publicado | Rol | Celda | Ventana | Publicado | Reserva | K | Cuias | Vacíos | Par | Alivio | Ronda |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 563 | `m09_v1_n0563` | matapi | comodidad | Ma | [0.20, 0.60] | normal 0.325 | normal 0.375 | 11 | 13 | 2 (2) | 22 | minus_color, plus_empty | 1 |
| 565 | `m09_v1_n0565` | brote | comodidad | B2 | [0.20, 0.60] | normal 0.250 | normal 0.400 | 11 | 13 | 2 (2) | 22 | minus_color, plus_empty | 1 |
| 566 | `m09_v1_n0566` | matapi+remolino | reto | Ma+Re (D) | [0.75, 0.90) | normal 0.700 | hard 0.825 | 10 | 11 | 1 (1) | 21 | plus_empty, minus_color | 1 |
| 568 | `m09_v1_n0568` | hoja | comodidad | Ha | [0.20, 0.60] | normal 0.575 | normal 0.450 | 11 | 13 | 2 (2) | 25 | minus_color, plus_empty | 1 |
| 569 | `m09_v1_n0569` | barro+brote | reto | B2+Ba (D) | [0.75, 0.90) | normal 0.675 | hard 0.750 | 10 | 12 | 2 (2) | 30 | minus_color, plus_empty | 1 |
| 571 | `m09_v1_n0571` | hoja | comodidad | Ho | [0.20, 0.60] | normal 0.525 | normal 0.525 | 11 | 13 | 2 (2) | 22 | minus_color, plus_empty | 1 |
| 572 | `m09_v1_n0572` | matapi+barro | reto | Ma+Ba (D) | [0.75, 0.90) | hard 0.750 | hard 0.775 | 10 | 12 | 2 (2) | 28 | minus_color, plus_empty | 1 |
| 575 | `m09_v1_n0575` | matapi | reto | Ma+Tu (D) | [0.75, 0.90) | hard 0.875 | hard 0.825 | 10 | 12 | 2 (2) | 32 | minus_color, plus_empty | 1 |
| 577 | `m09_v1_n0577` | brote | comodidad | Br | [0.20, 0.60] | normal 0.375 | normal 0.350 | 11 | 13 | 2 (2) | 22 | minus_color, plus_empty | 1 |
| 578 | `m09_v1_n0578` | remolino+brote | reto | B2+Re (D) | [0.75, 0.90) | hard 0.775 | hard 0.775 | 10 | 12 | 2 (2) | 30 | minus_color, plus_empty | 1 |
| 580 | `m09_v1_n0580` | matapi+brote | hito | Ma+B2 (MD) | [0.90, 1.00] | very_hard 1.000 | very_hard 1.000 | 12 | 13 | 1 (1) | 28 | plus_empty, minus_color | 1 |
| 583 | `m09_v1_n0583` | pirana | comodidad | Pi | [0.20, 0.60] | normal 0.200 | normal 0.200 | 11 | 13 | 2 (2) | 22 | minus_color, plus_empty | 1 |
| 585 | `m09_v1_n0585` | hoja | comodidad | Ha | [0.20, 0.60] | normal 0.475 | normal 0.450 | 11 | 13 | 2 (2) | 22 | minus_color, plus_empty | 1 |
| 586 | `m09_v1_n0586` | hoja+pirana | reto | Pi+Ha (D) | [0.75, 0.90) | hard 0.850 | hard 0.850 | 10 | 12 | 2 (2) | 31 | minus_color, plus_empty | 1 |
| 588 | `m09_v1_n0588` | hoja | comodidad | Ho | [0.20, 0.60] | normal 0.300 | normal 0.325 | 11 | 13 | 2 (2) | 22 | minus_color, plus_empty | 1 |
| 592 | `m09_v1_n0592` | brote+pirana | reto | Pi+B2 (D) | [0.75, 0.90) | normal 0.425 | hard 0.750 | 10 | 12 | 2 (2) | 33 | minus_color, plus_empty | 1 |
| 595 | `m09_v1_n0595` | barro | reto | Ba+Tu (D) | [0.75, 0.90) | hard 0.850 | hard 0.850 | 10 | 11 | 1 (1) | 28 | plus_empty, minus_color | 1 |
| 598 | `m09_v1_n0598` | barro+pirana | reto | Pi+Ba (D) | [0.75, 0.90) | hard 0.850 | hard 0.850 | 10 | 12 | 2 (2) | 32 | minus_color, plus_empty | 1 |
| 603 | `m09_v1_n0603` | liana | comodidad | Ln | [0.20, 0.60] | normal 0.250 | normal 0.550 | 11 | 13 | 2 (2) | 25 | minus_color, plus_empty | 1 |
| 605 | `m09_v1_n0605` | remolino | comodidad | Re | [0.20, 0.60] | normal 0.500 | normal 0.600 | 11 | 13 | 2 (2) | 22 | minus_color, plus_empty | 1 |
| 606 | `m09_v1_n0606` | liana+remolino | reto | Ln+Re (D) | [0.75, 0.90) | hard 0.825 | hard 0.825 | 10 | 12 | 2 (2) | 31 | minus_color, plus_empty | 1 |
| 609 | `m09_v1_n0609` | liana+lluvia | reto | Li+Ll (D) | [0.75, 0.90) | hard 0.775 | hard 0.775 | 10 | 12 | 2 (2) | 30 | minus_color, plus_empty | 1 |
| 612 | `m09_v1_n0612` | remolino+lluvia | reto | Ll+Re (D) | [0.75, 0.90) | hard 0.850 | hard 0.825 | 10 | 12 | 2 (2) | 31 | minus_color, plus_empty | 1 |
| 615 | `m09_v1_n0615` | liana+remolino | reto | Li+Re (D) | [0.75, 0.90) | normal 0.650 | hard 0.775 | 10 | 12 | 2 (2) | 33 | minus_color, plus_empty | 1 |
| 617 | `m09_v1_n0617` | liana | comodidad | Fl | [0.20, 0.60] | normal 0.575 | normal 0.600 | 11 | 13 | 2 (2) | 27 | minus_color, plus_empty | 1 |
| 618 | `m09_v1_n0618` | liana+lluvia | reto | Ln+Ll (D) | [0.75, 0.90) | hard 0.875 | hard 0.875 | 10 | 12 | 2 (2) | 32 | minus_color, plus_empty | 1 |
| 620 | `m09_v1_n0620` | liana+remolino+lluvia | hito | Ln+Ll+Re (MD) | [0.90, 1.00] | very_hard 1.000 | very_hard 1.000 | 13 | 14 | 1 (1) | 27 | plus_empty, minus_color | 1 |
| 623 | `m09_v1_n0623` | matapi | comodidad | Ma | [0.20, 0.60] | normal 0.200 | normal 0.275 | 11 | 13 | 2 (2) | 23 | minus_color, plus_empty | 1 |
