# Medida de la curva: campaign_v3_f3b_q, niveles 421-560

Generado por (no editar a mano):

```
dotnet run --project tools/level-generator -c Release -- --measure --spec tools/level-generator/specs/experimental/campaign_v3_f3b_q.json --levels 421-560 --report <este archivo>
```

Fases 1-3 de `--spec` (candidatos, índice D y colocación en el ciclo de 20 de D-009) sin alivio ni packs; un ciclo sin
colocación no detiene la medida. «En ventana»: el elegido mide la ventana de su slot (reto 75-89 %, hito ≥ 90 %, comodidad
20-60 %…); un reto o hito fuera de ella se publicaría con la etiqueta que mide (c.2). T = cuias del tablero, K = colores,
libres = cuias vacías, bot = fallo del bot casual ×40 del elegido. Topes de T por papel: D-068 (P2 A).

## Resumen

| Medida | Valor |
|---|---|
| Niveles | 140 (7 ciclos) |
| Ciclos sin colocación | 0 |
| Retos colocados en su ventana | 33 de 35 |
| Hitos colocados en su ventana | 7 de 7 |
| Retos con algún candidato en su ventana | 35 de 35 |
| T media / mediana (colocados) | 10.4 / 10 |
| K media / mediana (colocados) | 8.4 / 8 |
| Niveles con T ≤ 11 / T ≥ 12 / T ≥ 13 | 128 / 12 / 7 |
| Niveles con ocultas (dosis) | 28 (20 %; 52 dosis) |
| Fuera del tope de T de su papel | 0 |

## Por papel

| Papel | Tope T | Niveles | T mín-med-máx | T (recuento) | K mín-med-máx | Libres (med.) | Bot (mediana) | Bot mín-máx | En ventana | Fuera del tope | Con ocultas |
|---|---|---|---|---|---|---|---|---|---|---|---|
| recuperacion | 8-10 | 14 | 10-10-10 | 10: 14 | 8-8-8 | 2 | 13 % | 0 %-25 % | 14 | 0 | 0 |
| comodidad | 9-11 | 77 | 9-10-11 | 9: 14 · 10: 52 · 11: 11 | 8-8-8 | 1 | 35 % | 20 %-60 % | 77 | 0 | 21 |
| reto | 10-12 | 35 | 10-11-12 | 10: 2 · 11: 28 · 12: 5 | 9-9-10 | 1 | 83 % | 53 %-88 % | 33 | 0 | 7 |
| remanso | 8-10 | 7 | 10-10-10 | 10: 7 | 8-8-8 | 2 | 30 % | 25 %-35 % | 7 | 0 | 0 |
| hito | 12-13 | 6 | 13-13-13 | 13: 6 | 11-11-11 | 0 | 100 % | 95 %-100 % | 6 | 0 | 0 |
| cuspide | 13-14 | 1 | 14-14-14 | 14: 1 | 12-12-12 | 0 | 98 % | 98 %-98 % | 1 | 0 | 0 |

## Por ciclo

| Ciclo | Coloca | Retos en ventana | T de los retos | Bot de los retos | Hito | T del hito | Bot del hito | En ventana |
|---|---|---|---|---|---|---|---|---|
| 421-440 | sí | 5 de 5 | 11, 11, 11, 11, 11 | 75 %, 88 %, 85 %, 78 %, 78 % | Ag+Ho+Re (MD) | 13 | 100 % | sí |
| 441-460 | sí | 4 de 5 | 11, 11, 12, 11, 11 | 88 %, 73 %, 83 %, 78 %, 80 % | Pi+Ba+Ag (MD) | 13 | 100 % | sí |
| 461-480 | sí | 5 de 5 | 11, 11, 12, 11, 12 | 88 %, 83 %, 75 %, 75 %, 85 % | Li+B2+Ho (MD) | 13 | 100 % | sí |
| 481-500 | sí | 5 de 5 | 11, 11, 10, 11, 11 | 78 %, 83 %, 85 %, 83 %, 85 % | Re+Ha+A2 (MD) | 13 | 95 % | sí |
| 501-520 | sí | 4 de 5 | 12, 11, 11, 11, 11 | 88 %, 53 %, 88 %, 88 %, 80 % | Li+Ha+A2 (MD) | 13 | 100 % | sí |
| 521-540 | sí | 5 de 5 | 11, 11, 11, 11, 11 | 80 %, 80 %, 85 %, 83 %, 80 % | Ln+Ha+A2 (MD) | 13 | 98 % | sí |
| 541-560 | sí | 5 de 5 | 11, 12, 10, 11, 11 | 80 %, 85 %, 85 %, 83 %, 83 % | Fl+Re+A2 (MD) | 14 | 98 % | sí |

## Retos e hitos

Candidatos: en ventana (del pool) / medidos / intentos del slot, contando las rondas extra.

| Nivel | p | Papel | Celda | K plan | T | K | Libres | Ocultas | Par | Bot | Etiqueta | En ventana | Candidatos |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 426 | 6 | reto | Fl+Ma (D) | 9 | 11 | 9 | 1 | 0 | 19 | 75 % | hard | sí | 10 / 92 / 210 |
| 429 | 9 | reto | Ma+Re (D) | 10 | 11 | 10 | 0 | 0 | 20 | 88 % | hard | sí | 2 / 34 / 180 |
| 432 | 12 | reto | Fl+Ho (D) | 9 | 11 | 9 | 1 | 0 | 24 | 85 % | hard | sí | 12 / 33 / 143 |
| 435 | 15 | reto | Ma+Tu (D) | 9 | 11 | 9 | 0 | 2 | 21 | 78 % | hard | sí | 12 / 13 / 55 |
| 438 | 18 | reto | Ho+Re (D) | 9 | 11 | 9 | 1 | 0 | 23 | 78 % | hard | sí | 12 / 25 / 163 |
| 440 | 20 | hito | Ag+Ho+Re (MD) | 11 | 13 | 11 | 0 | 0 | 31 | 100 % | very_hard | sí | 11 / 36 / 30 |
| 446 | 6 | reto | Pi+Ba (D) | 9 | 11 | 9 | 0 | 0 | 19 | 88 % | hard | sí | 6 / 16 / 102 |
| 449 | 9 | reto | Br+Re (D) | 9 | 11 | 9 | 1 | 0 | 20 | 73 % | normal | no | 1 / 9 / 210 |
| 452 | 12 | reto | Pi+Br+Ag (D) | 9 | 12 | 9 | 0 | 0 | 18 | 83 % | hard | sí | 6 / 13 / 146 |
| 455 | 15 | reto | Br+Tu (D) | 9 | 11 | 9 | 1 | 3 | 21 | 78 % | hard | sí | 6 / 6 / 146 |
| 458 | 18 | reto | Ba+Re (D) | 9 | 11 | 9 | 1 | 0 | 27 | 80 % | hard | sí | 3 / 61 / 210 |
| 460 | 20 | hito | Pi+Ba+Ag (MD) | 11 | 13 | 11 | 0 | 0 | 36 | 100 % | very_hard | sí | 18 / 32 / 25 |
| 466 | 6 | reto | Li+B2 (D) | 9 | 11 | 9 | 1 | 0 | 19 | 88 % | hard | sí | 12 / 31 / 25 |
| 469 | 9 | reto | Ln+B2 (D) | 9 | 11 | 9 | 1 | 0 | 21 | 83 % | hard | sí | 9 / 22 / 210 |
| 472 | 12 | reto | Li+La (D) | 9 | 12 | 9 | 1 | 0 | 21 | 75 % | hard | sí | 6 / 24 / 180 |
| 475 | 15 | reto | Ho+Tu (D) | 9 | 11 | 9 | 0 | 2 | 25 | 75 % | hard | sí | 12 / 13 / 32 |
| 478 | 18 | reto | La+Ln (D) | 9 | 12 | 9 | 1 | 0 | 23 | 85 % | hard | sí | 5 / 20 / 180 |
| 480 | 20 | hito | Li+B2+Ho (MD) | 11 | 13 | 11 | 0 | 0 | 30 | 100 % | very_hard | sí | 16 / 44 / 30 |
| 486 | 6 | reto | Ma+Ha (D) | 9 | 11 | 9 | 1 | 0 | 19 | 78 % | hard | sí | 12 / 27 / 206 |
| 489 | 9 | reto | Ma+Ba (D) | 9 | 11 | 9 | 0 | 0 | 20 | 83 % | hard | sí | 12 / 31 / 140 |
| 492 | 12 | reto | Ag+Ha+Ba (D) | 9 | 10 | 9 | 0 | 0 | 20 | 85 % | hard | sí | 1 / 9 / 210 |
| 495 | 15 | reto | Ma+Tu (D) | 9 | 11 | 9 | 0 | 3 | 23 | 83 % | hard | sí | 12 / 14 / 108 |
| 498 | 18 | reto | Fl+Ha (D) | 9 | 11 | 9 | 2 | 0 | 31 | 85 % | hard | sí | 4 / 92 / 210 |
| 500 | 20 | hito | Re+Ha+A2 (MD) | 11 | 13 | 11 | 0 | 0 | 29 | 95 % | very_hard | sí | 12 / 51 / 48 |
| 506 | 6 | reto | La+Ba (D) | 9 | 12 | 9 | 1 | 0 | 19 | 88 % | hard | sí | 2 / 10 / 90 |
| 509 | 9 | reto | Fl (D) | 9 | 11 | 9 | 2 | 0 | 27 | 53 % | normal | no | 10 / 120 / 210 |
| 512 | 12 | reto | Ll+Ha (D) | 9 | 11 | 9 | 1 | 0 | 19 | 88 % | hard | sí | 6 / 9 / 188 |
| 515 | 15 | reto | Ba+Tu (D) | 9 | 11 | 9 | 1 | 2 | 21 | 88 % | hard | sí | 1 / 1 / 360 |
| 518 | 18 | reto | Fl+Ba (D) | 9 | 11 | 9 | 1 | 0 | 26 | 80 % | hard | sí | 3 / 11 / 210 |
| 520 | 20 | hito | Li+Ha+A2 (MD) | 11 | 13 | 11 | 0 | 0 | 36 | 100 % | very_hard | sí | 6 / 44 / 46 |
| 526 | 6 | reto | Pi+Ha (D) | 9 | 11 | 9 | 1 | 0 | 18 | 80 % | hard | sí | 12 / 32 / 132 |
| 529 | 9 | reto | Ag+Ho+Re (D) | 9 | 11 | 9 | 0 | 0 | 24 | 80 % | hard | sí | 12 / 20 / 99 |
| 532 | 12 | reto | Pi+Ho (D) | 9 | 11 | 9 | 0 | 0 | 25 | 85 % | hard | sí | 12 / 14 / 17 |
| 535 | 15 | reto | Ho+Tu (D) | 9 | 11 | 9 | 0 | 2 | 23 | 83 % | hard | sí | 12 / 12 / 36 |
| 538 | 18 | reto | Ha+Re (D) | 9 | 11 | 9 | 2 | 0 | 30 | 80 % | hard | sí | 3 / 91 / 210 |
| 540 | 20 | hito | Ln+Ha+A2 (MD) | 11 | 13 | 11 | 0 | 0 | 29 | 98 % | very_hard | sí | 12 / 77 / 13 |
| 546 | 6 | reto | Fl+Ho (D) | 9 | 11 | 9 | 1 | 0 | 21 | 80 % | hard | sí | 12 / 21 / 86 |
| 549 | 9 | reto | La+Ln (D) | 9 | 12 | 9 | 1 | 0 | 20 | 85 % | hard | sí | 4 / 18 / 180 |
| 552 | 12 | reto | Ln+Re (D) | 9 | 10 | 9 | 0 | 0 | 19 | 85 % | hard | sí | 6 / 89 / 210 |
| 555 | 15 | reto | Ho+Tu (D) | 9 | 11 | 9 | 0 | 2 | 25 | 83 % | hard | sí | 12 / 14 / 62 |
| 558 | 18 | reto | Fl (D) | 9 | 11 | 9 | 2 | 0 | 27 | 83 % | hard | sí | 9 / 103 / 210 |
| 560 | 20 | cuspide | Fl+Re+A2 (MD) | 12 | 14 | 12 | 0 | 0 | 29 | 98 % | very_hard | sí | 12 / 40 / 29 |

## Escalón de presión de los hitos

Plan = `pressure_plan` de la especificación; tablero = palancas del elegido (`Pressure.OfBoard`, con el Aguapé); celda = lo que se espera de la celda (`Pressure.OfCell`); bot atento = `AttentiveBot` ×40 del elegido (03 §17 #20, solo informe).

| Hito | Celda | Plan | Celda (esperado) | Tablero | Bot | Bot atento |
|---|---|---|---|---|---|---|
| 440 | Ag+Ho+Re (MD) | 4 | 5 | 5 | 100 % | 100 % |
| 460 | Pi+Ba+Ag (MD) | 5 | 6 | 5 | 100 % | 100 % |
| 480 | Li+B2+Ho (MD) | 5 | 5 | 5 | 100 % | 100 % |
| 500 | Re+Ha+A2 (MD) | 6 | 6 | 6 | 95 % | 98 % |
| 520 | Li+Ha+A2 (MD) | 7 | 7 | 7 | 100 % | 93 % |
| 540 | Ln+Ha+A2 (MD) | 7 | 7 | 7 | 98 % | 93 % |
| 560 | Fl+Re+A2 (MD) | 8 | 8 | 8 | 98 % | 85 % |

## Todos los niveles

| Nivel | p | Papel | Celda | K plan | T | K | Libres | Ocultas | Par | Bot | Etiqueta | En ventana | Candidatos |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 421 | 1 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 18 | 15 % | normal | sí | 12 / 33 / 21 |
| 422 | 2 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 17 | 0 % | normal | sí | 6 / 24 / 20 |
| 423 | 3 | comodidad | Ma+Tu | 8 | 10 | 8 | 0 | 2 | 19 | 48 % | normal | sí | 3 / 3 / 129 |
| 424 | 4 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 17 | 33 % | normal | sí | 3 / 52 / 153 |
| 425 | 5 | comodidad | La | 8 | 11 | 8 | 1 | 0 | 17 | 30 % | normal | sí | 1 / 1 / 150 |
| 426 | 6 | reto | Fl+Ma (D) | 9 | 11 | 9 | 1 | 0 | 19 | 75 % | hard | sí | 10 / 92 / 210 |
| 427 | 7 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 17 | 33 % | normal | sí | 12 / 110 / 173 |
| 428 | 8 | comodidad | Ba+Tu | 8 | 10 | 8 | 1 | 1 | 18 | 58 % | normal | sí | 1 / 2 / 360 |
| 429 | 9 | reto | Ma+Re (D) | 10 | 11 | 10 | 0 | 0 | 20 | 88 % | hard | sí | 2 / 34 / 180 |
| 430 | 10 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 16 | 40 % | normal | sí | 12 / 128 / 183 |
| 431 | 11 | comodidad | Ag+B2 | 8 | 10 | 8 | 0 | 0 | 16 | 20 % | normal | sí | 3 / 9 / 24 |
| 432 | 12 | reto | Fl+Ho (D) | 9 | 11 | 9 | 1 | 0 | 24 | 85 % | hard | sí | 12 / 33 / 143 |
| 433 | 13 | comodidad | Tu | 8 | 10 | 8 | 1 | 1 | 16 | 23 % | normal | sí | 8 / 13 / 360 |
| 434 | 14 | comodidad | Al | 8 | 10 | 8 | 1 | 0 | 19 | 45 % | normal | sí | 3 / 65 / 171 |
| 435 | 15 | reto | Ma+Tu (D) | 9 | 11 | 9 | 0 | 2 | 21 | 78 % | hard | sí | 12 / 13 / 55 |
| 436 | 16 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 16 | 48 % | normal | sí | 12 / 136 / 170 |
| 437 | 17 | comodidad | La+Ha | 8 | 11 | 8 | 1 | 0 | 17 | 45 % | normal | sí | 2 / 5 / 150 |
| 438 | 18 | reto | Ho+Re (D) | 9 | 11 | 9 | 1 | 0 | 23 | 78 % | hard | sí | 12 / 25 / 163 |
| 439 | 19 | remanso | S | 8 | 10 | 8 | 2 | 0 | 17 | 33 % | normal | sí | 12 / 83 / 223 |
| 440 | 20 | hito | Ag+Ho+Re (MD) | 11 | 13 | 11 | 0 | 0 | 31 | 100 % | very_hard | sí | 11 / 36 / 30 |
| 441 | 1 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 16 | 18 % | normal | sí | 6 / 29 / 55 |
| 442 | 2 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 17 | 18 % | normal | sí | 3 / 13 / 18 |
| 443 | 3 | comodidad | Pi | 8 | 10 | 8 | 1 | 0 | 18 | 35 % | normal | sí | 3 / 17 / 28 |
| 444 | 4 | comodidad | La | 8 | 11 | 8 | 1 | 0 | 16 | 33 % | normal | sí | 1 / 3 / 150 |
| 445 | 5 | comodidad | Br+Tu | 8 | 10 | 8 | 1 | 1 | 17 | 48 % | normal | sí | 3 / 8 / 106 |
| 446 | 6 | reto | Pi+Ba (D) | 9 | 11 | 9 | 0 | 0 | 19 | 88 % | hard | sí | 6 / 16 / 102 |
| 447 | 7 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 18 | 20 % | normal | sí | 6 / 64 / 192 |
| 448 | 8 | comodidad | Ho+Tu | 8 | 10 | 8 | 0 | 2 | 19 | 58 % | normal | sí | 3 / 3 / 124 |
| 449 | 9 | reto | Br+Re (D) | 9 | 11 | 9 | 1 | 0 | 20 | 73 % | normal | no | 1 / 9 / 210 |
| 450 | 10 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 16 | 53 % | normal | sí | 6 / 69 / 182 |
| 451 | 11 | comodidad | Pi | 8 | 10 | 8 | 1 | 0 | 17 | 23 % | normal | sí | 3 / 24 / 36 |
| 452 | 12 | reto | Pi+Br+Ag (D) | 9 | 12 | 9 | 0 | 0 | 18 | 83 % | hard | sí | 6 / 13 / 146 |
| 453 | 13 | comodidad | Tu | 8 | 10 | 8 | 1 | 1 | 17 | 38 % | normal | sí | 5 / 7 / 360 |
| 454 | 14 | comodidad | La | 8 | 11 | 8 | 1 | 0 | 16 | 30 % | normal | sí | 1 / 9 / 150 |
| 455 | 15 | reto | Br+Tu (D) | 9 | 11 | 9 | 1 | 3 | 21 | 78 % | hard | sí | 6 / 6 / 146 |
| 456 | 16 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 18 | 25 % | normal | sí | 6 / 67 / 190 |
| 457 | 17 | comodidad | Ag+Ha | 8 | 10 | 8 | 0 | 0 | 16 | 55 % | normal | sí | 3 / 27 / 178 |
| 458 | 18 | reto | Ba+Re (D) | 9 | 11 | 9 | 1 | 0 | 27 | 80 % | hard | sí | 3 / 61 / 210 |
| 459 | 19 | remanso | S | 8 | 10 | 8 | 2 | 0 | 17 | 35 % | normal | sí | 6 / 37 / 213 |
| 460 | 20 | hito | Pi+Ba+Ag (MD) | 11 | 13 | 11 | 0 | 0 | 36 | 100 % | very_hard | sí | 18 / 32 / 25 |
| 461 | 1 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 16 | 3 % | normal | sí | 12 / 52 / 43 |
| 462 | 2 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 18 | 18 % | normal | sí | 6 / 9 / 6 |
| 463 | 3 | comodidad | Li | 8 | 10 | 8 | 2 | 0 | 18 | 43 % | normal | sí | 3 / 47 / 167 |
| 464 | 4 | comodidad | Tu | 8 | 10 | 8 | 1 | 1 | 17 | 53 % | normal | sí | 2 / 8 / 360 |
| 465 | 5 | comodidad | B2 | 8 | 10 | 8 | 1 | 0 | 17 | 23 % | normal | sí | 3 / 7 / 92 |
| 466 | 6 | reto | Li+B2 (D) | 9 | 11 | 9 | 1 | 0 | 19 | 88 % | hard | sí | 12 / 31 / 25 |
| 467 | 7 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 17 | 30 % | normal | sí | 12 / 141 / 178 |
| 468 | 8 | comodidad | Ag | 8 | 10 | 8 | 0 | 0 | 16 | 60 % | normal | sí | 3 / 40 / 165 |
| 469 | 9 | reto | Ln+B2 (D) | 9 | 11 | 9 | 1 | 0 | 21 | 83 % | hard | sí | 9 / 22 / 210 |
| 470 | 10 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 18 | 23 % | normal | sí | 12 / 123 / 186 |
| 471 | 11 | comodidad | Br+Tu | 8 | 10 | 8 | 1 | 2 | 19 | 33 % | normal | sí | 3 / 3 / 135 |
| 472 | 12 | reto | Li+La (D) | 9 | 12 | 9 | 1 | 0 | 21 | 75 % | hard | sí | 6 / 24 / 180 |
| 473 | 13 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 16 | 28 % | normal | sí | 12 / 128 / 178 |
| 474 | 14 | comodidad | Ag+Ch | 8 | 10 | 8 | 0 | 0 | 19 | 45 % | normal | sí | 3 / 11 / 44 |
| 475 | 15 | reto | Ho+Tu (D) | 9 | 11 | 9 | 0 | 2 | 25 | 75 % | hard | sí | 12 / 13 / 32 |
| 476 | 16 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 18 | 50 % | normal | sí | 12 / 117 / 177 |
| 477 | 17 | comodidad | Ba+Tu | 8 | 10 | 8 | 1 | 1 | 16 | 43 % | normal | sí | 1 / 3 / 360 |
| 478 | 18 | reto | La+Ln (D) | 9 | 12 | 9 | 1 | 0 | 23 | 85 % | hard | sí | 5 / 20 / 180 |
| 479 | 19 | remanso | S | 8 | 10 | 8 | 2 | 0 | 18 | 25 % | normal | sí | 12 / 82 / 241 |
| 480 | 20 | hito | Li+B2+Ho (MD) | 11 | 13 | 11 | 0 | 0 | 30 | 100 % | very_hard | sí | 16 / 44 / 30 |
| 481 | 1 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 17 | 10 % | normal | sí | 12 / 34 / 19 |
| 482 | 2 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 17 | 3 % | normal | sí | 6 / 27 / 31 |
| 483 | 3 | comodidad | Ma+Tu | 8 | 10 | 8 | 0 | 2 | 22 | 55 % | normal | sí | 3 / 4 / 131 |
| 484 | 4 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 16 | 35 % | normal | sí | 3 / 63 / 190 |
| 485 | 5 | comodidad | La | 8 | 11 | 8 | 1 | 0 | 16 | 28 % | normal | sí | 3 / 4 / 130 |
| 486 | 6 | reto | Ma+Ha (D) | 9 | 11 | 9 | 1 | 0 | 19 | 78 % | hard | sí | 12 / 27 / 206 |
| 487 | 7 | comodidad | Tu | 8 | 10 | 8 | 1 | 2 | 17 | 30 % | normal | sí | 4 / 12 / 360 |
| 488 | 8 | comodidad | Ag | 8 | 10 | 8 | 0 | 0 | 17 | 58 % | normal | sí | 3 / 39 / 174 |
| 489 | 9 | reto | Ma+Ba (D) | 9 | 11 | 9 | 0 | 0 | 20 | 83 % | hard | sí | 12 / 31 / 140 |
| 490 | 10 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 18 | 35 % | normal | sí | 12 / 135 / 205 |
| 491 | 11 | comodidad | Br+Tu | 8 | 10 | 8 | 1 | 2 | 16 | 23 % | normal | sí | 2 / 4 / 360 |
| 492 | 12 | reto | Ag+Ha+Ba (D) | 9 | 10 | 9 | 0 | 0 | 20 | 85 % | hard | sí | 1 / 9 / 210 |
| 493 | 13 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 17 | 48 % | normal | sí | 12 / 113 / 162 |
| 494 | 14 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 17 | 58 % | normal | sí | 3 / 61 / 160 |
| 495 | 15 | reto | Ma+Tu (D) | 9 | 11 | 9 | 0 | 3 | 23 | 83 % | hard | sí | 12 / 14 / 108 |
| 496 | 16 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 17 | 28 % | normal | sí | 12 / 120 / 197 |
| 497 | 17 | comodidad | La+B2 | 8 | 11 | 8 | 1 | 0 | 19 | 30 % | normal | sí | 3 / 3 / 172 |
| 498 | 18 | reto | Fl+Ha (D) | 9 | 11 | 9 | 2 | 0 | 31 | 85 % | hard | sí | 4 / 92 / 210 |
| 499 | 19 | remanso | S | 8 | 10 | 8 | 2 | 0 | 17 | 28 % | normal | sí | 12 / 76 / 217 |
| 500 | 20 | hito | Re+Ha+A2 (MD) | 11 | 13 | 11 | 0 | 0 | 29 | 95 % | very_hard | sí | 12 / 51 / 48 |
| 501 | 1 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 17 | 5 % | normal | sí | 6 / 17 / 25 |
| 502 | 2 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 16 | 0 % | normal | sí | 3 / 12 / 14 |
| 503 | 3 | comodidad | Ll | 8 | 10 | 8 | 1 | 0 | 17 | 35 % | normal | sí | 3 / 4 / 10 |
| 504 | 4 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 16 | 23 % | normal | sí | 3 / 47 / 159 |
| 505 | 5 | comodidad | Ba+Tu | 8 | 10 | 8 | 1 | 2 | 18 | 25 % | normal | sí | 1 / 4 / 720 |
| 506 | 6 | reto | La+Ba (D) | 9 | 12 | 9 | 1 | 0 | 19 | 88 % | hard | sí | 2 / 10 / 90 |
| 507 | 7 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 18 | 43 % | normal | sí | 6 / 68 / 186 |
| 508 | 8 | comodidad | Ho+Tu | 8 | 10 | 8 | 0 | 2 | 19 | 23 % | normal | sí | 3 / 3 / 129 |
| 509 | 9 | reto | Fl (D) | 9 | 11 | 9 | 2 | 0 | 27 | 53 % | normal | no | 10 / 120 / 210 |
| 510 | 10 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 18 | 28 % | normal | sí | 6 / 68 / 180 |
| 511 | 11 | comodidad | Ag+Li | 8 | 9 | 8 | 0 | 0 | 16 | 60 % | normal | sí | 3 / 20 / 176 |
| 512 | 12 | reto | Ll+Ha (D) | 9 | 11 | 9 | 1 | 0 | 19 | 88 % | hard | sí | 6 / 9 / 188 |
| 513 | 13 | comodidad | Tu | 8 | 10 | 8 | 1 | 2 | 16 | 38 % | normal | sí | 3 / 6 / 360 |
| 514 | 14 | comodidad | Ll+Al | 8 | 10 | 8 | 1 | 0 | 16 | 25 % | normal | sí | 3 / 5 / 13 |
| 515 | 15 | reto | Ba+Tu (D) | 9 | 11 | 9 | 1 | 2 | 21 | 88 % | hard | sí | 1 / 1 / 360 |
| 516 | 16 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 17 | 55 % | normal | sí | 6 / 67 / 188 |
| 517 | 17 | comodidad | La+B2 | 8 | 11 | 8 | 1 | 0 | 16 | 35 % | normal | sí | 1 / 15 / 150 |
| 518 | 18 | reto | Fl+Ba (D) | 9 | 11 | 9 | 1 | 0 | 26 | 80 % | hard | sí | 3 / 11 / 210 |
| 519 | 19 | remanso | S | 8 | 10 | 8 | 2 | 0 | 18 | 28 % | normal | sí | 6 / 28 / 195 |
| 520 | 20 | hito | Li+Ha+A2 (MD) | 11 | 13 | 11 | 0 | 0 | 36 | 100 % | very_hard | sí | 6 / 44 / 46 |
| 521 | 1 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 17 | 15 % | normal | sí | 12 / 44 / 31 |
| 522 | 2 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 16 | 25 % | normal | sí | 6 / 42 / 30 |
| 523 | 3 | comodidad | Pi | 8 | 10 | 8 | 1 | 0 | 17 | 43 % | normal | sí | 3 / 7 / 14 |
| 524 | 4 | comodidad | Tu | 8 | 10 | 8 | 1 | 2 | 18 | 30 % | normal | sí | 3 / 7 / 106 |
| 525 | 5 | comodidad | Ag | 8 | 10 | 8 | 0 | 0 | 19 | 58 % | normal | sí | 3 / 45 / 207 |
| 526 | 6 | reto | Pi+Ha (D) | 9 | 11 | 9 | 1 | 0 | 18 | 80 % | hard | sí | 12 / 32 / 132 |
| 527 | 7 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 18 | 23 % | normal | sí | 12 / 145 / 171 |
| 528 | 8 | comodidad | Br+Tu | 8 | 10 | 8 | 1 | 2 | 17 | 30 % | normal | sí | 3 / 3 / 33 |
| 529 | 9 | reto | Ag+Ho+Re (D) | 9 | 11 | 9 | 0 | 0 | 24 | 80 % | hard | sí | 12 / 20 / 99 |
| 530 | 10 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 17 | 28 % | normal | sí | 12 / 116 / 172 |
| 531 | 11 | comodidad | Tu | 8 | 10 | 8 | 1 | 2 | 17 | 43 % | normal | sí | 3 / 6 / 72 |
| 532 | 12 | reto | Pi+Ho (D) | 9 | 11 | 9 | 0 | 0 | 25 | 85 % | hard | sí | 12 / 14 / 17 |
| 533 | 13 | comodidad | La | 8 | 11 | 8 | 1 | 0 | 20 | 40 % | normal | sí | 7 / 7 / 182 |
| 534 | 14 | comodidad | Ch | 8 | 10 | 8 | 1 | 0 | 18 | 30 % | normal | sí | 3 / 10 / 18 |
| 535 | 15 | reto | Ho+Tu (D) | 9 | 11 | 9 | 0 | 2 | 23 | 83 % | hard | sí | 12 / 12 / 36 |
| 536 | 16 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 17 | 28 % | normal | sí | 12 / 124 / 175 |
| 537 | 17 | comodidad | La+B2 | 8 | 11 | 8 | 1 | 0 | 18 | 48 % | normal | sí | 1 / 2 / 150 |
| 538 | 18 | reto | Ha+Re (D) | 9 | 11 | 9 | 2 | 0 | 30 | 80 % | hard | sí | 3 / 91 / 210 |
| 539 | 19 | remanso | S | 8 | 10 | 8 | 2 | 0 | 17 | 30 % | normal | sí | 12 / 67 / 166 |
| 540 | 20 | hito | Ln+Ha+A2 (MD) | 11 | 13 | 11 | 0 | 0 | 29 | 98 % | very_hard | sí | 12 / 77 / 13 |
| 541 | 1 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 18 | 15 % | normal | sí | 12 / 38 / 33 |
| 542 | 2 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 16 | 8 % | normal | sí | 6 / 27 / 26 |
| 543 | 3 | comodidad | Fl | 8 | 9 | 8 | 1 | 0 | 16 | 48 % | normal | sí | 3 / 26 / 160 |
| 544 | 4 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 20 | 25 % | normal | sí | 3 / 54 / 168 |
| 545 | 5 | comodidad | Ho+Tu | 8 | 10 | 8 | 0 | 2 | 20 | 38 % | normal | sí | 3 / 3 / 130 |
| 546 | 6 | reto | Fl+Ho (D) | 9 | 11 | 9 | 1 | 0 | 21 | 80 % | hard | sí | 12 / 21 / 86 |
| 547 | 7 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 17 | 60 % | normal | sí | 12 / 149 / 185 |
| 548 | 8 | comodidad | Ba+Tu | 8 | 11 | 8 | 2 | 2 | 20 | 20 % | normal | sí | 1 / 8 / 720 |
| 549 | 9 | reto | La+Ln (D) | 9 | 12 | 9 | 1 | 0 | 20 | 85 % | hard | sí | 4 / 18 / 180 |
| 550 | 10 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 19 | 23 % | normal | sí | 12 / 144 / 213 |
| 551 | 11 | comodidad | Ag+Ll | 8 | 10 | 8 | 0 | 0 | 17 | 33 % | normal | sí | 3 / 9 / 32 |
| 552 | 12 | reto | Ln+Re (D) | 9 | 10 | 9 | 0 | 0 | 19 | 85 % | hard | sí | 6 / 89 / 210 |
| 553 | 13 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 18 | 58 % | normal | sí | 12 / 115 / 174 |
| 554 | 14 | comodidad | La+Al | 8 | 11 | 8 | 1 | 0 | 17 | 48 % | normal | sí | 2 / 4 / 150 |
| 555 | 15 | reto | Ho+Tu (D) | 9 | 11 | 9 | 0 | 2 | 25 | 83 % | hard | sí | 12 / 14 / 62 |
| 556 | 16 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 20 | 45 % | normal | sí | 12 / 138 / 185 |
| 557 | 17 | comodidad | Br+Tu | 8 | 10 | 8 | 1 | 2 | 17 | 33 % | normal | sí | 3 / 6 / 86 |
| 558 | 18 | reto | Fl (D) | 9 | 11 | 9 | 2 | 0 | 27 | 83 % | hard | sí | 9 / 103 / 210 |
| 559 | 19 | remanso | S | 8 | 10 | 8 | 2 | 0 | 17 | 35 % | normal | sí | 12 / 47 / 195 |
| 560 | 20 | cuspide | Fl+Re+A2 (MD) | 12 | 14 | 12 | 0 | 0 | 29 | 98 % | very_hard | sí | 12 / 40 / 29 |
