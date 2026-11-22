# Reserva del 20 %: campaign_v3

Generado por `dotnet run --project tools/level-generator -c Release -- --reserve tools/out/curva_f3/curva_08_v1.json --spec tools/level-generator/specs/campaign_v3.json --out tools/out/curva_f3 --report tools/out/curva_f3/reserva_curva_report.md`: no se edita a mano.
QA: `dotnet run --project tools/level-qa -c Release -- --reserve <reserva_NN_v1.json> --campaign <mundo_NN_v1.json> --spec tools/level-generator/specs/campaign_v3.json`.

Fuente: D-033, añadido «la reserva del 20 %» (Q-B11-7 (a)); 03 §17; D-020 (pregunta 31 b). Un pack por mundo (`kind` = `reserve`, `pack_schema` 4) con, para los niveles que usan una mecánica con kill switch (`mech_enabled_*`, 08 §9), otro tablero del mismo slot sin mecánicas y en su ventana, hasta el 20 % de los niveles del mundo. El cliente solo los usa desde `level_overrides`.

Reglas [P] de esta herramienta: orden por rondas sobre los kill switches de 08 §9 (cada mecánica toma su primer nivel aún sin elegir); tablero con la celda sin mecánicas (ni Aguas turbias) y con sus capacidades; entre los candidatos en la ventana con el par en el objetivo del rango ±30 %, el de la misma etiqueta que el publicado y el `bot_fail` más cercano; alivio desde el 31; sin `D` (no entra en el pool del rango).

## Resumen

| Mundo | Pack | Niveles del mundo | Con mecánica | Tope (20 %) | Tableros | Saltados | Bytes | SHA-256 |
|---|---|---|---|---|---|---|---|---|
| 8 (421-560) | `reserva_curva_08_v1.json` | 140 | 77 | 28 | 28 | 0 | 35815 | `fc102faaee16e708024b968a9aad605ccc69224673c98c3fe886594923a543e4` |

## Cobertura por mecánica

Niveles del mundo que usan cada kill switch y cuántos tienen tablero de reserva (el primero sin cubrir, si lo hay).

| Mundo | Mecánica | Niveles | Con reserva | Primer nivel sin reserva |
|---|---|---|---|---|
| 8 | `hoja` | 29 | 7 | 457 |
| 8 | `matapi` | 10 | 6 | 486 |
| 8 | `liana` | 18 | 5 | 472 |
| 8 | `remolino` | 11 | 5 | 488 |
| 8 | `barro` | 13 | 5 | 477 |
| 8 | `lluvia` | 6 | 3 | 514 |
| 8 | `brote` | 16 | 6 | 455 |
| 8 | `pirana` | 9 | 5 | 523 |

## Mundo 8: `reserva_curva_08_v1.json`

| Slot | Sustituye | Mecánicas del publicado | Rol | Celda | Ventana | Publicado | Reserva | K | Cuias | Vacíos | Par | Alivio | Ronda |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 423 | `m08_v1_n0423_c2` | matapi | comodidad | Ma+Tu | [0.20, 0.60] | normal 0.475 | normal 0.500 | 8 | 10 | 2 (2) | 19 | minus_color, plus_empty | 1 |
| 425 | `m08_v1_n0425_c2` | hoja | comodidad | Ho | [0.20, 0.60] | normal 0.375 | normal 0.425 | 8 | 10 | 2 (2) | 18 | minus_color, plus_empty | 1 |
| 426 | `m08_v1_n0426_c2` | matapi+liana | reto | Fl+Ma (D) | [0.75, 0.90) | hard 0.750 | hard 0.800 | 9 | 11 | 2 (2) | 29 | minus_color, plus_empty | 1 |
| 428 | `m08_v1_n0428_c2` | barro | comodidad | Ba+Tu | [0.20, 0.60] | normal 0.575 | normal 0.250 | 8 | 10 | 2 (2) | 16 | minus_color, plus_empty | 1 |
| 429 | `m08_v1_n0429_c2` | matapi+remolino | reto | Ma+Re (D) | [0.75, 0.90) | normal 0.725 | hard 0.750 | 9 | 11 | 2 (2) | 26 | minus_color, plus_empty | 1 |
| 431 | `m08_v1_n0431_c2` | brote | comodidad | B2 | [0.20, 0.60] | normal 0.525 | normal 0.300 | 8 | 10 | 2 (2) | 20 | minus_color, plus_empty | 1 |
| 432 | `m08_v1_n0432_c2` | hoja+liana | reto | Fl+Ho (D) | [0.75, 0.90) | hard 0.850 | hard 0.875 | 9 | 11 | 2 (2) | 25 | minus_color, plus_empty | 1 |
| 435 | `m08_v1_n0435_c2` | matapi | reto | Ma+Tu (D) | [0.75, 0.90) | hard 0.775 | hard 0.775 | 9 | 10 | 1 (1) | 18 | plus_empty, minus_color | 1 |
| 437 | `m08_v1_n0437_c2` | hoja | comodidad | Ha | [0.20, 0.60] | normal 0.575 | normal 0.425 | 8 | 10 | 2 (2) | 18 | minus_color, plus_empty | 1 |
| 438 | `m08_v1_n0438_c2` | hoja+remolino | reto | Ho+Re (D) | [0.75, 0.90) | hard 0.775 | hard 0.750 | 9 | 11 | 2 (2) | 28 | minus_color, plus_empty | 1 |
| 440 | `m08_v1_n0440_c2` | hoja+matapi | hito | Ma+Ho (MD) | [0.90, 1.00] | very_hard 1.000 | very_hard 1.000 | 11 | 12 | 1 (1) | 23 | plus_empty, minus_color | 1 |
| 443 | `m08_v1_n0443_c2` | pirana | comodidad | Pi | [0.20, 0.60] | normal 0.350 | normal 0.350 | 8 | 10 | 2 (2) | 20 | minus_color, plus_empty | 1 |
| 445 | `m08_v1_n0445_c2` | brote | comodidad | Br+Tu | [0.20, 0.60] | normal 0.475 | normal 0.450 | 8 | 10 | 2 (2) | 17 | minus_color, plus_empty | 1 |
| 446 | `m08_v1_n0446_c2` | barro+pirana | reto | Pi+Ba (D) | [0.75, 0.90) | hard 0.875 | hard 0.850 | 9 | 11 | 2 (2) | 26 | minus_color, plus_empty | 1 |
| 448 | `m08_v1_n0448_c2` | hoja | comodidad | Ho+Tu | [0.20, 0.60] | normal 0.575 | normal 0.575 | 8 | 10 | 2 (2) | 16 | minus_color, plus_empty | 1 |
| 449 | `m08_v1_n0449_c2` | remolino+brote | reto | Br+Re (D) | [0.75, 0.90) | hard 0.750 | hard 0.750 | 9 | 10 | 1 (1) | 21 | plus_empty, minus_color | 1 |
| 451 | `m08_v1_n0451_c2` | pirana | comodidad | Pi | [0.20, 0.60] | normal 0.225 | normal 0.350 | 8 | 9 | 1 (1) | 17 | plus_empty, minus_color | 1 |
| 452 | `m08_v1_n0452_c2` | brote+pirana | reto | Pi+Br (D) | [0.75, 0.90) | normal 0.450 | hard 0.775 | 9 | 11 | 2 (2) | 28 | minus_color, plus_empty | 1 |
| 458 | `m08_v1_n0458_c2` | remolino+barro | reto | Ba+Re (D) | [0.75, 0.90) | hard 0.825 | hard 0.825 | 9 | 11 | 2 (2) | 28 | minus_color, plus_empty | 1 |
| 460 | `m08_v1_n0460_c2` | barro+pirana | hito | Pi+Ba (MD) | [0.90, 1.00] | very_hard 1.000 | very_hard 1.000 | 11 | 12 | 1 (1) | 34 | plus_empty, minus_color | 1 |
| 463 | `m08_v1_n0463_c2` | liana | comodidad | Li | [0.20, 0.60] | normal 0.425 | normal 0.500 | 8 | 10 | 2 (2) | 22 | minus_color, plus_empty | 1 |
| 466 | `m08_v1_n0466_c2` | liana+brote | reto | Li+B2 (D) | [0.75, 0.90) | normal 0.725 | hard 0.800 | 9 | 11 | 2 (2) | 29 | minus_color, plus_empty | 1 |
| 468 | `m08_v1_n0468_c2` | remolino | comodidad | Re | [0.20, 0.60] | normal 0.500 | normal 0.425 | 8 | 10 | 2 (2) | 17 | minus_color, plus_empty | 1 |
| 469 | `m08_v1_n0469_c2` | liana+brote | reto | Ln+B2 (D) | [0.75, 0.90) | hard 0.750 | hard 0.750 | 9 | 11 | 2 (2) | 27 | minus_color, plus_empty | 1 |
| 483 | `m08_v1_n0483_c2` | matapi | comodidad | Ma+Tu | [0.20, 0.60] | normal 0.550 | normal 0.350 | 8 | 10 | 2 (2) | 19 | minus_color, plus_empty | 1 |
| 503 | `m08_v1_n0503_c2` | lluvia | comodidad | Ll | [0.20, 0.60] | normal 0.350 | normal 0.300 | 8 | 10 | 2 (2) | 17 | minus_color, plus_empty | 1 |
| 506 | `m08_v1_n0506_c2` | barro+lluvia | reto | Ll+Ba (D) | [0.75, 0.90) | hard 0.875 | hard 0.875 | 9 | 11 | 2 (2) | 28 | minus_color, plus_empty | 1 |
| 512 | `m08_v1_n0512_c2` | hoja+lluvia | reto | Ll+Ha (D) | [0.75, 0.90) | hard 0.875 | hard 0.850 | 9 | 11 | 2 (2) | 30 | minus_color, plus_empty | 1 |
