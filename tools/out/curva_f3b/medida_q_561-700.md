# Medida de la curva: campaign_v3_f3b_q, niveles 561-700

Generado por (no editar a mano):

```
dotnet run --project tools/level-generator -c Release -- --measure --spec tools/level-generator/specs/experimental/campaign_v3_f3b_q.json --levels 561-700 --report <este archivo>
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
| Retos colocados en su ventana | 32 de 35 |
| Hitos colocados en su ventana | 7 de 7 |
| Retos con algún candidato en su ventana | 35 de 35 |
| T media / mediana (colocados) | 10.4 / 10 |
| K media / mediana (colocados) | 8.4 / 8 |
| Niveles con T ≤ 11 / T ≥ 12 / T ≥ 13 | 127 / 13 / 7 |
| Niveles con ocultas (dosis) | 27 (19 %; 49 dosis) |
| Fuera del tope de T de su papel | 0 |

## Por papel

| Papel | Tope T | Niveles | T mín-med-máx | T (recuento) | K mín-med-máx | Libres (med.) | Bot (mediana) | Bot mín-máx | En ventana | Fuera del tope | Con ocultas |
|---|---|---|---|---|---|---|---|---|---|---|---|
| recuperacion | 8-10 | 14 | 10-10-10 | 10: 14 | 8-8-8 | 2 | 6 % | 0 %-25 % | 14 | 0 | 0 |
| comodidad | 9-11 | 77 | 9-10-11 | 9: 14 · 10: 49 · 11: 14 | 8-8-8 | 1 | 35 % | 20 %-60 % | 77 | 0 | 21 |
| reto | 10-12 | 35 | 10-11-12 | 10: 2 · 11: 27 · 12: 6 | 9-9-10 | 1 | 83 % | 45 %-88 % | 32 | 0 | 6 |
| remanso | 8-10 | 7 | 9-10-10 | 9: 1 · 10: 6 | 8-8-8 | 2 | 33 % | 20 %-40 % | 7 | 0 | 0 |
| hito | 12-13 | 6 | 13-13-13 | 13: 6 | 11-11-11 | 0 | 100 % | 95 %-100 % | 6 | 0 | 0 |
| cuspide | 13-14 | 1 | 14-14-14 | 14: 1 | 12-12-12 | 0 | 100 % | 100 %-100 % | 1 | 0 | 0 |

## Por ciclo

| Ciclo | Coloca | Retos en ventana | T de los retos | Bot de los retos | Hito | T del hito | Bot del hito | En ventana |
|---|---|---|---|---|---|---|---|---|
| 561-580 | sí | 5 de 5 | 12, 11, 11, 11, 11 | 75 %, 75 %, 80 %, 80 %, 78 % | Ba+B2+Ag (MD) | 13 | 100 % | sí |
| 581-600 | sí | 5 de 5 | 11, 11, 12, 11, 11 | 83 %, 88 %, 78 %, 88 %, 88 % | Ha+Ba+A2 (MD) | 13 | 100 % | sí |
| 601-620 | sí | 5 de 5 | 10, 11, 11, 11, 12 | 80 %, 80 %, 88 %, 85 %, 85 % | Ln+Ll+Ag (MD) | 13 | 100 % | sí |
| 621-640 | sí | 4 de 5 | 11, 11, 11, 11, 12 | 88 %, 88 %, 45 %, 88 %, 75 % | Li+Ba+A2 (MD) | 13 | 95 % | sí |
| 641-660 | sí | 5 de 5 | 12, 11, 11, 11, 11 | 80 %, 83 %, 83 %, 85 %, 85 % | Ln+Ha+A2 (MD) | 13 | 98 % | sí |
| 661-680 | sí | 4 de 5 | 10, 11, 11, 11, 11 | 88 %, 85 %, 70 %, 80 %, 88 % | Li+Ha+A2 (MD) | 13 | 100 % | sí |
| 681-700 | sí | 4 de 5 | 12, 11, 11, 11, 11 | 68 %, 80 %, 80 %, 88 %, 88 % | Fl+Ha+A2 (MD) | 14 | 100 % | sí |

## Retos e hitos

Candidatos: en ventana (del pool) / medidos / intentos del slot, contando las rondas extra.

| Nivel | p | Papel | Celda | K plan | T | K | Libres | Ocultas | Par | Bot | Etiqueta | En ventana | Candidatos |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 566 | 6 | reto | Ag+Ma+Re (D) | 9 | 12 | 9 | 0 | 0 | 18 | 75 % | hard | sí | 6 / 15 / 117 |
| 569 | 9 | reto | B2+Ba (D) | 9 | 11 | 9 | 1 | 0 | 23 | 75 % | hard | sí | 6 / 6 / 180 |
| 572 | 12 | reto | Ma+Ba (D) | 9 | 11 | 9 | 0 | 0 | 23 | 80 % | hard | sí | 6 / 15 / 205 |
| 575 | 15 | reto | Ma+Tu (D) | 9 | 11 | 9 | 0 | 3 | 21 | 80 % | hard | sí | 6 / 7 / 104 |
| 578 | 18 | reto | B2+Re (D) | 9 | 11 | 9 | 1 | 0 | 20 | 78 % | hard | sí | 3 / 8 / 210 |
| 580 | 20 | hito | Ba+B2+Ag (MD) | 11 | 13 | 11 | 0 | 0 | 28 | 100 % | very_hard | sí | 6 / 7 / 19 |
| 586 | 6 | reto | Pi+Ha (D) | 9 | 11 | 9 | 1 | 0 | 20 | 83 % | hard | sí | 6 / 12 / 84 |
| 589 | 9 | reto | Ha+B2 (D) | 9 | 11 | 9 | 1 | 0 | 21 | 88 % | hard | sí | 3 / 12 / 210 |
| 592 | 12 | reto | Pi+B2+Ag (D) | 9 | 12 | 9 | 0 | 0 | 25 | 78 % | hard | sí | 5 / 37 / 180 |
| 595 | 15 | reto | Ba+Tu (D) | 9 | 11 | 9 | 1 | 2 | 25 | 88 % | hard | sí | 1 / 1 / 1080 |
| 598 | 18 | reto | Pi+Ba (D) | 9 | 11 | 9 | 0 | 0 | 29 | 88 % | hard | sí | 6 / 18 / 106 |
| 600 | 20 | hito | Ha+Ba+A2 (MD) | 11 | 13 | 11 | 0 | 0 | 29 | 100 % | very_hard | sí | 6 / 57 / 58 |
| 606 | 6 | reto | Ln+Re (D) | 9 | 10 | 9 | 0 | 0 | 19 | 80 % | hard | sí | 8 / 107 / 210 |
| 609 | 9 | reto | Li+Ll (D) | 9 | 11 | 9 | 1 | 0 | 20 | 80 % | hard | sí | 11 / 26 / 207 |
| 612 | 12 | reto | Ag+Ll (D) | 9 | 11 | 9 | 0 | 0 | 25 | 88 % | hard | sí | 11 / 20 / 28 |
| 615 | 15 | reto | Li+Re (D) | 9 | 11 | 9 | 2 | 0 | 28 | 85 % | hard | sí | 5 / 88 / 210 |
| 618 | 18 | reto | Ln+Ll (D) | 9 | 12 | 9 | 2 | 0 | 28 | 85 % | hard | sí | 12 / 19 / 177 |
| 620 | 20 | hito | Ln+Ll+Ag (MD) | 11 | 13 | 11 | 0 | 0 | 33 | 100 % | very_hard | sí | 12 / 13 / 9 |
| 626 | 6 | reto | Ma+Ba (D) | 9 | 11 | 9 | 0 | 0 | 19 | 88 % | hard | sí | 5 / 43 / 210 |
| 629 | 9 | reto | Ll+Ma (D) | 9 | 11 | 9 | 0 | 0 | 21 | 88 % | hard | sí | 9 / 16 / 180 |
| 632 | 12 | reto | Ma+Ho (D) | 9 | 11 | 9 | 0 | 0 | 28 | 45 % | normal | no | 6 / 35 / 180 |
| 635 | 15 | reto | Ma+Tu (D) | 9 | 11 | 9 | 0 | 2 | 18 | 88 % | hard | sí | 2 / 2 / 360 |
| 638 | 18 | reto | Ag+Ba+Ho (D) | 9 | 12 | 9 | 0 | 0 | 26 | 75 % | hard | sí | 6 / 7 / 67 |
| 640 | 20 | hito | Li+Ba+A2 (MD) | 11 | 13 | 11 | 0 | 0 | 32 | 95 % | very_hard | sí | 6 / 71 / 73 |
| 646 | 6 | reto | Pi+Br+Ha (D) | 9 | 12 | 9 | 1 | 0 | 19 | 80 % | hard | sí | 1 / 9 / 180 |
| 649 | 9 | reto | Pi+Ha (D) | 9 | 11 | 9 | 1 | 0 | 22 | 83 % | hard | sí | 9 / 58 / 210 |
| 652 | 12 | reto | Ag+Br+Re (D) | 9 | 11 | 9 | 0 | 0 | 20 | 83 % | hard | sí | 12 / 21 / 49 |
| 655 | 15 | reto | Br+Tu (D) | 9 | 11 | 9 | 1 | 3 | 25 | 85 % | hard | sí | 11 / 12 / 173 |
| 658 | 18 | reto | Ag+Ha+Re (D) | 9 | 11 | 9 | 1 | 0 | 26 | 85 % | hard | sí | 1 / 38 / 210 |
| 660 | 20 | hito | Ln+Ha+A2 (MD) | 11 | 13 | 11 | 0 | 0 | 26 | 98 % | very_hard | sí | 12 / 96 / 53 |
| 666 | 6 | reto | Li+Ha (D) | 9 | 10 | 9 | 1 | 0 | 18 | 88 % | hard | sí | 7 / 114 / 210 |
| 669 | 9 | reto | Li+Br (D) | 9 | 11 | 9 | 1 | 0 | 22 | 85 % | hard | sí | 12 / 15 / 123 |
| 672 | 12 | reto | Li+Re (D) | 9 | 11 | 9 | 2 | 0 | 26 | 70 % | normal | no | 4 / 93 / 210 |
| 675 | 15 | reto | Br+Tu (D) | 9 | 11 | 9 | 1 | 2 | 22 | 80 % | hard | sí | 12 / 13 / 161 |
| 678 | 18 | reto | Ha+Re (D) | 9 | 11 | 9 | 2 | 0 | 28 | 88 % | hard | sí | 4 / 107 / 210 |
| 680 | 20 | hito | Li+Ha+A2 (MD) | 11 | 13 | 11 | 0 | 0 | 35 | 100 % | very_hard | sí | 12 / 124 / 62 |
| 686 | 6 | reto | Ma+Ho (D) | 10 | 12 | 10 | 0 | 0 | 22 | 68 % | normal | no | 5 / 31 / 30 |
| 689 | 9 | reto | Ho+B2 (D) | 9 | 11 | 9 | 0 | 0 | 23 | 80 % | hard | sí | 3 / 7 / 180 |
| 692 | 12 | reto | Ma+Ha (D) | 9 | 11 | 9 | 1 | 0 | 18 | 80 % | hard | sí | 2 / 38 / 210 |
| 695 | 15 | reto | Ho+Tu (D) | 9 | 11 | 9 | 0 | 2 | 22 | 88 % | hard | sí | 6 / 7 / 94 |
| 698 | 18 | reto | Ag+Ha+B2 (D) | 9 | 11 | 9 | 0 | 0 | 21 | 88 % | hard | sí | 6 / 6 / 25 |
| 700 | 20 | cuspide | Fl+Ha+A2 (MD) | 12 | 14 | 12 | 0 | 0 | 30 | 100 % | very_hard | sí | 6 / 16 / 16 |

## Escalón de presión de los hitos

Plan = `pressure_plan` de la especificación; tablero = palancas del elegido (`Pressure.OfBoard`, con el Aguapé); celda = lo que se espera de la celda (`Pressure.OfCell`); bot atento = `AttentiveBot` ×40 del elegido (03 §17 #20, solo informe).

| Hito | Celda | Plan | Celda (esperado) | Tablero | Bot | Bot atento |
|---|---|---|---|---|---|---|
| 580 | Ba+B2+Ag (MD) | 5 | 5 | 5 | 100 % | 93 % |
| 600 | Ha+Ba+A2 (MD) | 6 | 6 | 6 | 100 % | 70 % |
| 620 | Ln+Ll+Ag (MD) | 6 | 6 | 6 | 100 % | 98 % |
| 640 | Li+Ba+A2 (MD) | 7 | 7 | 7 | 95 % | 95 % |
| 660 | Ln+Ha+A2 (MD) | 8 | 7 | 7 | 98 % | 100 % |
| 680 | Li+Ha+A2 (MD) | 8 | 7 | 7 | 100 % | 98 % |
| 700 | Fl+Ha+A2 (MD) | 9 | 8 | 8 | 100 % | 100 % |

## Todos los niveles

| Nivel | p | Papel | Celda | K plan | T | K | Libres | Ocultas | Par | Bot | Etiqueta | En ventana | Candidatos |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 561 | 1 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 18 | 15 % | normal | sí | 6 / 12 / 16 |
| 562 | 2 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 16 | 3 % | normal | sí | 12 / 41 / 29 |
| 563 | 3 | comodidad | Ma+Tu | 8 | 10 | 8 | 0 | 1 | 19 | 33 % | normal | sí | 3 / 3 / 127 |
| 564 | 4 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 16 | 45 % | normal | sí | 3 / 52 / 158 |
| 565 | 5 | comodidad | La+B2 | 8 | 11 | 8 | 1 | 0 | 16 | 35 % | normal | sí | 3 / 4 / 181 |
| 566 | 6 | reto | Ag+Ma+Re (D) | 9 | 12 | 9 | 0 | 0 | 18 | 75 % | hard | sí | 6 / 15 / 117 |
| 567 | 7 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 16 | 45 % | normal | sí | 6 / 61 / 171 |
| 568 | 8 | comodidad | Ag+Ha | 8 | 10 | 8 | 0 | 0 | 16 | 50 % | normal | sí | 3 / 48 / 225 |
| 569 | 9 | reto | B2+Ba (D) | 9 | 11 | 9 | 1 | 0 | 23 | 75 % | hard | sí | 6 / 6 / 180 |
| 570 | 10 | comodidad | La | 8 | 11 | 8 | 1 | 0 | 16 | 23 % | normal | sí | 2 / 2 / 150 |
| 571 | 11 | comodidad | Ho+Tu | 8 | 10 | 8 | 0 | 1 | 19 | 35 % | normal | sí | 3 / 4 / 147 |
| 572 | 12 | reto | Ma+Ba (D) | 9 | 11 | 9 | 0 | 0 | 23 | 80 % | hard | sí | 6 / 15 / 205 |
| 573 | 13 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 20 | 60 % | normal | sí | 6 / 52 / 164 |
| 574 | 14 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 18 | 33 % | normal | sí | 3 / 62 / 155 |
| 575 | 15 | reto | Ma+Tu (D) | 9 | 11 | 9 | 0 | 3 | 21 | 80 % | hard | sí | 6 / 7 / 104 |
| 576 | 16 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 16 | 35 % | normal | sí | 6 / 72 / 173 |
| 577 | 17 | comodidad | Br+Tu | 8 | 10 | 8 | 1 | 2 | 18 | 28 % | normal | sí | 3 / 5 / 402 |
| 578 | 18 | reto | B2+Re (D) | 9 | 11 | 9 | 1 | 0 | 20 | 78 % | hard | sí | 3 / 8 / 210 |
| 579 | 19 | remanso | S | 8 | 10 | 8 | 2 | 0 | 17 | 38 % | normal | sí | 6 / 33 / 195 |
| 580 | 20 | hito | Ba+B2+Ag (MD) | 11 | 13 | 11 | 0 | 0 | 28 | 100 % | very_hard | sí | 6 / 7 / 19 |
| 581 | 1 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 16 | 8 % | normal | sí | 6 / 18 / 29 |
| 582 | 2 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 18 | 3 % | normal | sí | 3 / 15 / 31 |
| 583 | 3 | comodidad | Pi | 8 | 10 | 8 | 1 | 0 | 19 | 45 % | normal | sí | 3 / 13 / 18 |
| 584 | 4 | comodidad | Tu | 8 | 10 | 8 | 1 | 1 | 16 | 25 % | normal | sí | 2 / 6 / 360 |
| 585 | 5 | comodidad | Ag+Ha | 8 | 10 | 8 | 0 | 0 | 17 | 33 % | normal | sí | 3 / 53 / 304 |
| 586 | 6 | reto | Pi+Ha (D) | 9 | 11 | 9 | 1 | 0 | 20 | 83 % | hard | sí | 6 / 12 / 84 |
| 587 | 7 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 17 | 40 % | normal | sí | 6 / 90 / 208 |
| 588 | 8 | comodidad | Ho+Tu | 8 | 10 | 8 | 0 | 2 | 18 | 28 % | normal | sí | 3 / 4 / 249 |
| 589 | 9 | reto | Ha+B2 (D) | 9 | 11 | 9 | 1 | 0 | 21 | 88 % | hard | sí | 3 / 12 / 210 |
| 590 | 10 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 18 | 35 % | normal | sí | 6 / 59 / 186 |
| 591 | 11 | comodidad | La | 8 | 11 | 8 | 1 | 0 | 18 | 38 % | normal | sí | 9 / 17 / 150 |
| 592 | 12 | reto | Pi+B2+Ag (D) | 9 | 12 | 9 | 0 | 0 | 25 | 78 % | hard | sí | 5 / 37 / 180 |
| 593 | 13 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 18 | 28 % | normal | sí | 6 / 71 / 214 |
| 594 | 14 | comodidad | La+Al | 8 | 11 | 8 | 1 | 0 | 17 | 23 % | normal | sí | 2 / 7 / 150 |
| 595 | 15 | reto | Ba+Tu (D) | 9 | 11 | 9 | 1 | 2 | 25 | 88 % | hard | sí | 1 / 1 / 1080 |
| 596 | 16 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 18 | 40 % | normal | sí | 6 / 60 / 178 |
| 597 | 17 | comodidad | Br+Tu | 8 | 10 | 8 | 1 | 2 | 16 | 30 % | normal | sí | 3 / 5 / 117 |
| 598 | 18 | reto | Pi+Ba (D) | 9 | 11 | 9 | 0 | 0 | 29 | 88 % | hard | sí | 6 / 18 / 106 |
| 599 | 19 | remanso | S | 8 | 9 | 8 | 1 | 0 | 16 | 40 % | normal | sí | 6 / 29 / 172 |
| 600 | 20 | hito | Ha+Ba+A2 (MD) | 11 | 13 | 11 | 0 | 0 | 29 | 100 % | very_hard | sí | 6 / 57 / 58 |
| 601 | 1 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 16 | 18 % | normal | sí | 12 / 45 / 46 |
| 602 | 2 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 16 | 25 % | normal | sí | 6 / 26 / 11 |
| 603 | 3 | comodidad | Ln | 8 | 9 | 8 | 1 | 0 | 20 | 48 % | normal | sí | 3 / 65 / 209 |
| 604 | 4 | comodidad | Tu | 8 | 10 | 8 | 1 | 2 | 16 | 60 % | normal | sí | 3 / 5 / 70 |
| 605 | 5 | comodidad | Ag | 8 | 10 | 8 | 0 | 0 | 16 | 55 % | normal | sí | 3 / 40 / 203 |
| 606 | 6 | reto | Ln+Re (D) | 9 | 10 | 9 | 0 | 0 | 19 | 80 % | hard | sí | 8 / 107 / 210 |
| 607 | 7 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 16 | 50 % | normal | sí | 12 / 140 / 187 |
| 608 | 8 | comodidad | Ho+Tu | 8 | 10 | 8 | 0 | 2 | 20 | 25 % | normal | sí | 3 / 3 / 138 |
| 609 | 9 | reto | Li+Ll (D) | 9 | 11 | 9 | 1 | 0 | 20 | 80 % | hard | sí | 11 / 26 / 207 |
| 610 | 10 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 17 | 53 % | normal | sí | 12 / 135 / 183 |
| 611 | 11 | comodidad | Br+Tu | 8 | 10 | 8 | 1 | 1 | 17 | 23 % | normal | sí | 3 / 3 / 55 |
| 612 | 12 | reto | Ag+Ll (D) | 9 | 11 | 9 | 0 | 0 | 25 | 88 % | hard | sí | 11 / 20 / 28 |
| 613 | 13 | comodidad | La | 8 | 11 | 8 | 1 | 0 | 16 | 30 % | normal | sí | 1 / 5 / 150 |
| 614 | 14 | comodidad | Ch | 8 | 10 | 8 | 1 | 0 | 20 | 50 % | normal | sí | 3 / 13 / 24 |
| 615 | 15 | reto | Li+Re (D) | 9 | 11 | 9 | 2 | 0 | 28 | 85 % | hard | sí | 5 / 88 / 210 |
| 616 | 16 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 18 | 28 % | normal | sí | 12 / 123 / 166 |
| 617 | 17 | comodidad | La+Fl | 8 | 11 | 8 | 1 | 0 | 17 | 30 % | normal | sí | 3 / 7 / 60 |
| 618 | 18 | reto | Ln+Ll (D) | 9 | 12 | 9 | 2 | 0 | 28 | 85 % | hard | sí | 12 / 19 / 177 |
| 619 | 19 | remanso | S | 8 | 10 | 8 | 2 | 0 | 18 | 20 % | normal | sí | 12 / 60 / 191 |
| 620 | 20 | hito | Ln+Ll+Ag (MD) | 11 | 13 | 11 | 0 | 0 | 33 | 100 % | very_hard | sí | 12 / 13 / 9 |
| 621 | 1 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 16 | 5 % | normal | sí | 6 / 21 / 37 |
| 622 | 2 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 18 | 0 % | normal | sí | 3 / 13 / 18 |
| 623 | 3 | comodidad | Ma+Tu | 8 | 10 | 8 | 0 | 2 | 22 | 28 % | normal | sí | 3 / 3 / 126 |
| 624 | 4 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 20 | 28 % | normal | sí | 3 / 68 / 173 |
| 625 | 5 | comodidad | Ag+Ba | 8 | 10 | 8 | 0 | 0 | 17 | 55 % | normal | sí | 3 / 8 / 15 |
| 626 | 6 | reto | Ma+Ba (D) | 9 | 11 | 9 | 0 | 0 | 19 | 88 % | hard | sí | 5 / 43 / 210 |
| 627 | 7 | comodidad | Tu | 8 | 10 | 8 | 1 | 2 | 17 | 48 % | normal | sí | 1 / 5 / 360 |
| 628 | 8 | comodidad | La+B2 | 8 | 11 | 8 | 1 | 0 | 16 | 25 % | normal | sí | 1 / 1 / 150 |
| 629 | 9 | reto | Ll+Ma (D) | 9 | 11 | 9 | 0 | 0 | 21 | 88 % | hard | sí | 9 / 16 / 180 |
| 630 | 10 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 22 | 28 % | normal | sí | 6 / 59 / 193 |
| 631 | 11 | comodidad | Tu | 8 | 10 | 8 | 1 | 1 | 18 | 35 % | normal | sí | 3 / 3 / 66 |
| 632 | 12 | reto | Ma+Ho (D) | 9 | 11 | 9 | 0 | 0 | 28 | 45 % | normal | no | 6 / 35 / 180 |
| 633 | 13 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 20 | 43 % | normal | sí | 6 / 61 / 178 |
| 634 | 14 | comodidad | Al | 8 | 10 | 8 | 1 | 0 | 16 | 30 % | normal | sí | 3 / 65 / 175 |
| 635 | 15 | reto | Ma+Tu (D) | 9 | 11 | 9 | 0 | 2 | 18 | 88 % | hard | sí | 2 / 2 / 360 |
| 636 | 16 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 21 | 35 % | normal | sí | 6 / 56 / 170 |
| 637 | 17 | comodidad | La+Ha | 8 | 11 | 8 | 1 | 0 | 17 | 33 % | normal | sí | 3 / 4 / 86 |
| 638 | 18 | reto | Ag+Ba+Ho (D) | 9 | 12 | 9 | 0 | 0 | 26 | 75 % | hard | sí | 6 / 7 / 67 |
| 639 | 19 | remanso | S | 8 | 10 | 8 | 2 | 0 | 17 | 23 % | normal | sí | 6 / 45 / 196 |
| 640 | 20 | hito | Li+Ba+A2 (MD) | 11 | 13 | 11 | 0 | 0 | 32 | 95 % | very_hard | sí | 6 / 71 / 73 |
| 641 | 1 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 18 | 5 % | normal | sí | 12 / 45 / 43 |
| 642 | 2 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 19 | 20 % | normal | sí | 6 / 20 / 21 |
| 643 | 3 | comodidad | Pi | 8 | 10 | 8 | 1 | 0 | 16 | 38 % | normal | sí | 3 / 9 / 18 |
| 644 | 4 | comodidad | La | 8 | 11 | 8 | 1 | 0 | 16 | 33 % | normal | sí | 1 / 4 / 150 |
| 645 | 5 | comodidad | Br+Tu | 8 | 10 | 8 | 1 | 2 | 18 | 50 % | normal | sí | 3 / 3 / 93 |
| 646 | 6 | reto | Pi+Br+Ha (D) | 9 | 12 | 9 | 1 | 0 | 19 | 80 % | hard | sí | 1 / 9 / 180 |
| 647 | 7 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 18 | 40 % | normal | sí | 12 / 126 / 176 |
| 648 | 8 | comodidad | La+Al | 8 | 11 | 8 | 1 | 0 | 16 | 38 % | normal | sí | 3 / 5 / 85 |
| 649 | 9 | reto | Pi+Ha (D) | 9 | 11 | 9 | 1 | 0 | 22 | 83 % | hard | sí | 9 / 58 / 210 |
| 650 | 10 | comodidad | Tu | 8 | 10 | 8 | 1 | 1 | 16 | 25 % | normal | sí | 4 / 7 / 360 |
| 651 | 11 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 17 | 33 % | normal | sí | 3 / 44 / 153 |
| 652 | 12 | reto | Ag+Br+Re (D) | 9 | 11 | 9 | 0 | 0 | 20 | 83 % | hard | sí | 12 / 21 / 49 |
| 653 | 13 | comodidad | Tu | 8 | 10 | 8 | 1 | 1 | 18 | 25 % | normal | sí | 2 / 7 / 360 |
| 654 | 14 | comodidad | Ag+Pi | 8 | 10 | 8 | 0 | 0 | 16 | 45 % | normal | sí | 3 / 20 / 55 |
| 655 | 15 | reto | Br+Tu (D) | 9 | 11 | 9 | 1 | 3 | 25 | 85 % | hard | sí | 11 / 12 / 173 |
| 656 | 16 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 17 | 53 % | normal | sí | 12 / 117 / 170 |
| 657 | 17 | comodidad | Ag+B2 | 8 | 10 | 8 | 0 | 0 | 18 | 50 % | normal | sí | 3 / 11 / 30 |
| 658 | 18 | reto | Ag+Ha+Re (D) | 9 | 11 | 9 | 1 | 0 | 26 | 85 % | hard | sí | 1 / 38 / 210 |
| 659 | 19 | remanso | S | 8 | 10 | 8 | 2 | 0 | 17 | 33 % | normal | sí | 12 / 76 / 204 |
| 660 | 20 | hito | Ln+Ha+A2 (MD) | 11 | 13 | 11 | 0 | 0 | 26 | 98 % | very_hard | sí | 12 / 96 / 53 |
| 661 | 1 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 16 | 0 % | normal | sí | 12 / 34 / 33 |
| 662 | 2 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 19 | 18 % | normal | sí | 6 / 18 / 8 |
| 663 | 3 | comodidad | La+Li | 8 | 11 | 8 | 1 | 0 | 17 | 25 % | normal | sí | 3 / 5 / 82 |
| 664 | 4 | comodidad | Tu | 8 | 10 | 8 | 1 | 2 | 17 | 28 % | normal | sí | 3 / 5 / 106 |
| 665 | 5 | comodidad | Ag+Ha | 8 | 10 | 8 | 0 | 0 | 16 | 50 % | normal | sí | 3 / 34 / 247 |
| 666 | 6 | reto | Li+Ha (D) | 9 | 10 | 9 | 1 | 0 | 18 | 88 % | hard | sí | 7 / 114 / 210 |
| 667 | 7 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 16 | 35 % | normal | sí | 12 / 111 / 161 |
| 668 | 8 | comodidad | Ho+Tu | 8 | 10 | 8 | 0 | 2 | 18 | 60 % | normal | sí | 3 / 4 / 143 |
| 669 | 9 | reto | Li+Br (D) | 9 | 11 | 9 | 1 | 0 | 22 | 85 % | hard | sí | 12 / 15 / 123 |
| 670 | 10 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 19 | 50 % | normal | sí | 12 / 149 / 182 |
| 671 | 11 | comodidad | Ba+Tu | 8 | 10 | 8 | 1 | 2 | 19 | 33 % | normal | sí | 3 / 3 / 289 |
| 672 | 12 | reto | Li+Re (D) | 9 | 11 | 9 | 2 | 0 | 26 | 70 % | normal | no | 4 / 93 / 210 |
| 673 | 13 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 17 | 55 % | normal | sí | 12 / 137 / 194 |
| 674 | 14 | comodidad | Ag+Ch | 8 | 10 | 8 | 0 | 0 | 17 | 55 % | normal | sí | 3 / 11 / 18 |
| 675 | 15 | reto | Br+Tu (D) | 9 | 11 | 9 | 1 | 2 | 22 | 80 % | hard | sí | 12 / 13 / 161 |
| 676 | 16 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 17 | 35 % | normal | sí | 12 / 143 / 187 |
| 677 | 17 | comodidad | La+B2 | 8 | 11 | 8 | 1 | 0 | 19 | 20 % | normal | sí | 3 / 3 / 172 |
| 678 | 18 | reto | Ha+Re (D) | 9 | 11 | 9 | 2 | 0 | 28 | 88 % | hard | sí | 4 / 107 / 210 |
| 679 | 19 | remanso | S | 8 | 10 | 8 | 2 | 0 | 16 | 33 % | normal | sí | 12 / 51 / 185 |
| 680 | 20 | hito | Li+Ha+A2 (MD) | 11 | 13 | 11 | 0 | 0 | 35 | 100 % | very_hard | sí | 12 / 124 / 62 |
| 681 | 1 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 17 | 15 % | normal | sí | 6 / 21 / 28 |
| 682 | 2 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 16 | 0 % | normal | sí | 3 / 4 / 6 |
| 683 | 3 | comodidad | Ma+Tu | 8 | 10 | 8 | 0 | 2 | 20 | 55 % | normal | sí | 3 / 3 / 125 |
| 684 | 4 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 21 | 30 % | normal | sí | 3 / 51 / 156 |
| 685 | 5 | comodidad | Ho | 8 | 10 | 8 | 1 | 0 | 18 | 55 % | normal | sí | 3 / 4 / 10 |
| 686 | 6 | reto | Ma+Ho (D) | 10 | 12 | 10 | 0 | 0 | 22 | 68 % | normal | no | 5 / 31 / 30 |
| 687 | 7 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 18 | 20 % | normal | sí | 6 / 65 / 168 |
| 688 | 8 | comodidad | La | 8 | 11 | 8 | 1 | 0 | 16 | 20 % | normal | sí | 1 / 2 / 150 |
| 689 | 9 | reto | Ho+B2 (D) | 9 | 11 | 9 | 0 | 0 | 23 | 80 % | hard | sí | 3 / 7 / 180 |
| 690 | 10 | comodidad | Tu | 8 | 10 | 8 | 1 | 2 | 16 | 23 % | normal | sí | 1 / 7 / 360 |
| 691 | 11 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 16 | 60 % | normal | sí | 3 / 43 / 154 |
| 692 | 12 | reto | Ma+Ha (D) | 9 | 11 | 9 | 1 | 0 | 18 | 80 % | hard | sí | 2 / 38 / 210 |
| 693 | 13 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 18 | 60 % | normal | sí | 6 / 68 / 190 |
| 694 | 14 | comodidad | Ma+Ch | 8 | 11 | 8 | 1 | 0 | 17 | 20 % | normal | sí | 12 / 14 / 249 |
| 695 | 15 | reto | Ho+Tu (D) | 9 | 11 | 9 | 0 | 2 | 22 | 88 % | hard | sí | 6 / 7 / 94 |
| 696 | 16 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 20 | 45 % | normal | sí | 6 / 80 / 198 |
| 697 | 17 | comodidad | Ba+Tu | 8 | 10 | 8 | 1 | 2 | 21 | 43 % | normal | sí | 3 / 8 / 994 |
| 698 | 18 | reto | Ag+Ha+B2 (D) | 9 | 11 | 9 | 0 | 0 | 21 | 88 % | hard | sí | 6 / 6 / 25 |
| 699 | 19 | remanso | S | 8 | 10 | 8 | 2 | 0 | 16 | 38 % | normal | sí | 6 / 27 / 184 |
| 700 | 20 | cuspide | Fl+Ha+A2 (MD) | 12 | 14 | 12 | 0 | 0 | 30 | 100 % | very_hard | sí | 6 / 16 / 16 |
