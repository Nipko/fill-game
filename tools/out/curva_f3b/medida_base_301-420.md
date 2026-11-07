# Medida de la curva: campaign_v3, niveles 301-420

Generado por (no editar a mano):

```
dotnet run --project tools/level-generator -c Release -- --measure --spec tools/level-generator/specs/campaign_v3.json --levels 301-420 --report <este archivo>
```

Fases 1-3 de `--spec` (candidatos, índice D y colocación en el ciclo de 20 de D-009) sin alivio ni packs; un ciclo sin
colocación no detiene la medida. «En ventana»: el elegido mide la ventana de su slot (reto 75-89 %, hito ≥ 90 %, comodidad
20-60 %…); un reto o hito fuera de ella se publicaría con la etiqueta que mide (c.2). T = cuias del tablero, K = colores,
libres = cuias vacías, bot = fallo del bot casual ×40 del elegido. Topes de T por papel: D-068 (P2 A).

## Resumen

| Medida | Valor |
|---|---|
| Niveles | 120 (6 ciclos) |
| Ciclos sin colocación | 1 (401-420) |
| Retos colocados en su ventana | 23 de 30 |
| Hitos colocados en su ventana | 5 de 6 |
| Retos con algún candidato en su ventana | 30 de 30 |
| T media / mediana (colocados) | 10.4 / 10 |
| K media / mediana (colocados) | 8.3 / 8 |
| Niveles con T ≤ 11 / T ≥ 12 / T ≥ 13 | 86 / 14 / 5 |
| Niveles con ocultas (dosis) | 15 (15 %; 26 dosis) |
| Fuera del tope de T de su papel | 0 |

## Por papel

| Papel | Tope T | Niveles | T mín-med-máx | T (recuento) | K mín-med-máx | Libres (med.) | Bot (mediana) | Bot mín-máx | En ventana | Fuera del tope | Con ocultas |
|---|---|---|---|---|---|---|---|---|---|---|---|
| recuperacion | 8-10 | 10 de 12 | 10-10-10 | 10: 10 | 8-8-8 | 2 | 0 % | 0 %-5 % | 10 | 0 | 0 |
| estreno | 9-10 | 13 de 16 | 9-10-10 | 9: 1 · 10: 12 | 6-7-8 | 2 | 0 % | 0 %-15 % | 13 | 0 | 2 |
| comodidad | 9-11 | 42 de 50 | 9-10-11 | 9: 7 · 10: 32 · 11: 3 | 8-8-8 | 1 | 35 % | 20 %-60 % | 42 | 0 | 10 |
| reto | 10-12 | 25 de 30 | 10-11-12 | 10: 1 · 11: 15 · 12: 9 | 9-9-10 | 1 | 85 % | 58 %-88 % | 23 | 0 | 3 |
| remanso | 8-10 | 5 de 6 | 10-10-10 | 10: 5 | 8-8-8 | 2 | 28 % | 23 %-33 % | 5 | 0 | 0 |
| hito | 12-13 | 5 | 13-13-13 | 13: 5 | 11-11-11 | 0 | 100 % | 93 %-100 % | 5 | 0 | 0 |
| cuspide | 13-14 | 0 de 1 | — | — | — | — | — | — | — | — | — |

## Por ciclo

| Ciclo | Coloca | Retos en ventana | T de los retos | Bot de los retos | Hito | T del hito | Bot del hito | En ventana |
|---|---|---|---|---|---|---|---|---|
| 301-320 | sí | 5 de 5 | 11, 11, 11, 10, 11 | 80 %, 88 %, 85 %, 88 %, 80 % | Ha+Ll+Br (MD) | 13 | 93 % | sí |
| 321-340 | sí | 4 de 5 | 12, 12, 11, 11, 11 | 75 %, 73 %, 85 %, 88 %, 83 % | Ag+Br+Ba (MD) | 13 | 100 % | sí |
| 341-360 | sí | 4 de 5 | 12, 12, 11, 11, 11 | 78 %, 80 %, 58 %, 88 %, 83 % | B2+Ln+Ag (MD) | 13 | 100 % | sí |
| 361-380 | sí | 5 de 5 | 12, 11, 12, 12, 12 | 88 %, 75 %, 85 %, 88 %, 75 % | Pi+Ba+Ag (MD) | 13 | 93 % | sí |
| 381-400 | sí | 5 de 5 | 11, 11, 12, 11, 11 | 85 %, 85 %, 88 %, 85 %, 88 % | Li+Ha+Ag (MD) | 13 | 100 % | sí |
| 401-420 | **no** | — | — | — | Fl+Ba+A2 (MD) | — | — | — |

## Retos e hitos

Candidatos: en ventana (del pool) / medidos / intentos del slot, contando las rondas extra.

| Nivel | p | Papel | Celda | K plan | T | K | Libres | Ocultas | Par | Bot | Etiqueta | En ventana | Candidatos |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 306 | 6 | reto | Ha+Ll (D) | 9 | 11 | 9 | 1 | 0 | 20 | 80 % | hard | sí | 10 / 26 / 167 |
| 309 | 9 | reto | Ag+Ll (D) | 9 | 11 | 9 | 0 | 0 | 24 | 88 % | hard | sí | 12 / 14 / 90 |
| 312 | 12 | reto | Ag+Br (D) | 9 | 11 | 9 | 0 | 0 | 23 | 85 % | hard | sí | 12 / 12 / 24 |
| 315 | 15 | reto | Br+Tu (D) | 9 | 10 | 9 | 0 | 2 | 22 | 88 % | hard | sí | 11 / 14 / 124 |
| 318 | 18 | reto | Ag+Ln (D) | 9 | 11 | 9 | 1 | 0 | 26 | 80 % | hard | sí | 4 / 138 / 210 |
| 320 | 20 | hito | Ha+Ll+Br (MD) | 11 | 13 | 11 | 1 | 0 | 25 | 93 % | very_hard | sí | 38 / 75 / 25 |
| 326 | 6 | reto | Ag+Ma (D) | 9 | 12 | 9 | 1 | 0 | 20 | 75 % | hard | sí | 12 / 23 / 76 |
| 329 | 9 | reto | Ma (D) | 10 | 12 | 10 | 1 | 0 | 25 | 73 % | normal | no | 4 / 91 / 180 |
| 332 | 12 | reto | Ba+Ho (D) | 9 | 11 | 9 | 0 | 0 | 23 | 85 % | hard | sí | 12 / 15 / 178 |
| 335 | 15 | reto | Ma+Ho (D) | 9 | 11 | 9 | 0 | 0 | 30 | 88 % | hard | sí | 7 / 29 / 180 |
| 338 | 18 | reto | Ag+Ba (D) | 9 | 11 | 9 | 1 | 0 | 30 | 83 % | hard | sí | 9 / 20 / 96 |
| 340 | 20 | hito | Ag+Br+Ba (MD) | 11 | 13 | 11 | 0 | 0 | 27 | 100 % | very_hard | sí | 12 / 15 / 16 |
| 346 | 6 | reto | B2+Ln (D) | 10 | 12 | 10 | 1 | 0 | 24 | 78 % | hard | sí | 5 / 12 / 180 |
| 349 | 9 | reto | B2 (D) | 10 | 12 | 10 | 1 | 0 | 21 | 80 % | hard | sí | 3 / 10 / 180 |
| 352 | 12 | reto | B2+Re (D) | 9 | 11 | 9 | 1 | 0 | 23 | 58 % | normal | no | 2 / 13 / 210 |
| 355 | 15 | reto | B2+Re (D) | 9 | 11 | 9 | 1 | 0 | 21 | 88 % | hard | sí | 2 / 10 / 90 |
| 358 | 18 | reto | Ag+Ln (D) | 9 | 11 | 9 | 1 | 0 | 28 | 83 % | hard | sí | 1 / 58 / 210 |
| 360 | 20 | hito | B2+Ln+Ag (MD) | 11 | 13 | 11 | 0 | 0 | 27 | 100 % | very_hard | sí | 30 / 44 / 21 |
| 366 | 6 | reto | Ho+Ag+Tu (D) | 9 | 12 | 9 | 0 | 3 | 20 | 88 % | hard | sí | 18 / 19 / 55 |
| 369 | 9 | reto | Pi+Ma (D) | 9 | 11 | 9 | 0 | 0 | 22 | 75 % | hard | sí | 4 / 50 / 180 |
| 372 | 12 | reto | La+B2 (D) | 9 | 12 | 9 | 1 | 0 | 24 | 85 % | hard | sí | 3 / 17 / 180 |
| 375 | 15 | reto | La+Ba (D) | 9 | 12 | 9 | 1 | 0 | 23 | 88 % | hard | sí | 4 / 25 / 180 |
| 378 | 18 | reto | La+Ho (D) | 9 | 12 | 9 | 0 | 0 | 24 | 75 % | hard | sí | 7 / 20 / 30 |
| 380 | 20 | hito | Pi+Ba+Ag (MD) | 11 | 13 | 11 | 0 | 0 | 35 | 93 % | very_hard | sí | 18 / 36 / 15 |
| 386 | 6 | reto | Ba+Tu (D) | 9 | 11 | 9 | 1 | 3 | 23 | 85 % | hard | sí | 1 / 2 / 1440 |
| 389 | 9 | reto | Li+Ha (D) | 9 | 11 | 9 | 2 | 0 | 27 | 85 % | hard | sí | 1 / 51 / 210 |
| 392 | 12 | reto | La+Ba (D) | 9 | 12 | 9 | 1 | 0 | 26 | 88 % | hard | sí | 1 / 7 / 180 |
| 395 | 15 | reto | Li+Ba (D) | 9 | 11 | 9 | 1 | 0 | 28 | 85 % | hard | sí | 5 / 14 / 210 |
| 398 | 18 | reto | Ha+Ba (D) | 9 | 11 | 9 | 1 | 0 | 29 | 88 % | hard | sí | 2 / 14 / 210 |
| 400 | 20 | hito | Li+Ha+Ag (MD) | 11 | 13 | 11 | 1 | 0 | 34 | 100 % | very_hard | sí | 6 / 23 / 34 |
| 406 | 6 | reto | Ag+Ll (D) | 9 | — | — | — | — | — | — | sin colocar | — | 18 / 18 / 19 |
| 409 | 9 | reto | Fl (D) | 9 | — | — | — | — | — | — | sin colocar | — | 11 / 129 / 210 |
| 412 | 12 | reto | Fl+La (D) | 9 | — | — | — | — | — | — | sin colocar | — | 14 / 35 / 180 |
| 415 | 15 | reto | Fl+Ll (D) | 9 | — | — | — | — | — | — | sin colocar | — | 18 / 28 / 139 |
| 418 | 18 | reto | Ba+Tu (D) | 9 | — | — | — | — | — | — | sin colocar | — | 1 / 1 / 360 |
| 420 | 20 | cuspide | Fl+Ba+A2 (MD) | 12 | — | — | — | — | — | — | sin colocar | — | 16 / 220 / 66 |

## Escalón de presión de los hitos

Plan = `pressure_plan` de la especificación; tablero = palancas del elegido (`Pressure.OfBoard`, con el Aguapé); celda = lo que se espera de la celda (`Pressure.OfCell`); bot atento = `AttentiveBot` ×40 del elegido (03 §17 #20, solo informe).

| Hito | Celda | Plan | Celda (esperado) | Tablero | Bot | Bot atento |
|---|---|---|---|---|---|---|
| 320 | Ha+Ll+Br (MD) | 3 | 5 | 5 | 93 % | 88 % |
| 340 | Ag+Br+Ba (MD) | 4 | 5 | 5 | 100 % | 100 % |
| 360 | B2+Ln+Ag (MD) | 5 | 6 | 6 | 100 % | 83 % |
| 380 | Pi+Ba+Ag (MD) | 5 | 6 | 5 | 93 % | 43 % |
| 400 | Li+Ha+Ag (MD) | 6 | 6 | 6 | 100 % | 100 % |
| 420 | Fl+Ba+A2 (MD) | 7 | 8 | — | — | — |

## Todos los niveles

| Nivel | p | Papel | Celda | K plan | T | K | Libres | Ocultas | Par | Bot | Etiqueta | En ventana | Candidatos |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 301 | 1 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 16 | 0 % | normal | sí | 12 / 59 / 60 |
| 302 | 2 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 18 | 0 % | normal | sí | 6 / 17 / 21 |
| 303 | 3 | estreno | *Ag | 8 | 10 | 8 | 0 | 0 | 16 | 0 % | normal | sí | 3 / 20 / 200 |
| 304 | 4 | estreno | *Ag | 8 | 10 | 8 | 1 | 0 | 17 | 5 % | normal | sí | 3 / 12 / 63 |
| 305 | 5 | estreno | *Ag | 8 | 10 | 8 | 1 | 0 | 16 | 15 % | normal | sí | 3 / 3 / 25 |
| 306 | 6 | reto | Ha+Ll (D) | 9 | 11 | 9 | 1 | 0 | 20 | 80 % | hard | sí | 10 / 26 / 167 |
| 307 | 7 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 16 | 38 % | normal | sí | 12 / 136 / 158 |
| 308 | 8 | comodidad | Ag | 8 | 10 | 8 | 0 | 0 | 16 | 50 % | normal | sí | 3 / 27 / 172 |
| 309 | 9 | reto | Ag+Ll (D) | 9 | 11 | 9 | 0 | 0 | 24 | 88 % | hard | sí | 12 / 14 / 90 |
| 310 | 10 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 17 | 38 % | normal | sí | 12 / 154 / 204 |
| 311 | 11 | comodidad | Ba | 8 | 10 | 8 | 1 | 0 | 16 | 30 % | normal | sí | 3 / 9 / 40 |
| 312 | 12 | reto | Ag+Br (D) | 9 | 11 | 9 | 0 | 0 | 23 | 85 % | hard | sí | 12 / 12 / 24 |
| 313 | 13 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 17 | 53 % | normal | sí | 12 / 130 / 192 |
| 314 | 14 | comodidad | Ag | 8 | 10 | 8 | 0 | 0 | 16 | 43 % | normal | sí | 3 / 47 / 173 |
| 315 | 15 | reto | Br+Tu (D) | 9 | 10 | 9 | 0 | 2 | 22 | 88 % | hard | sí | 11 / 14 / 124 |
| 316 | 16 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 17 | 20 % | normal | sí | 12 / 125 / 168 |
| 317 | 17 | comodidad | Ll | 8 | 10 | 8 | 1 | 0 | 20 | 43 % | normal | sí | 3 / 8 / 18 |
| 318 | 18 | reto | Ag+Ln (D) | 9 | 11 | 9 | 1 | 0 | 26 | 80 % | hard | sí | 4 / 138 / 210 |
| 319 | 19 | remanso | S | 8 | 10 | 8 | 2 | 0 | 18 | 33 % | normal | sí | 12 / 58 / 175 |
| 320 | 20 | hito | Ha+Ll+Br (MD) | 11 | 13 | 11 | 1 | 0 | 25 | 93 % | very_hard | sí | 38 / 75 / 25 |
| 321 | 1 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 16 | 0 % | normal | sí | 12 / 38 / 27 |
| 322 | 2 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 17 | 3 % | normal | sí | 15 / 47 / 4 |
| 323 | 3 | estreno | *Tu | 7 | 10 | 7 | 2 | 1 | 14 | 0 % | normal | sí | 3 / 7 / 24 |
| 324 | 4 | estreno | *Tu | 7 | 10 | 7 | 2 | 1 | 14 | 0 % | normal | sí | 3 / 7 / 19 |
| 325 | 5 | comodidad | Ba | 8 | 10 | 8 | 1 | 0 | 16 | 33 % | normal | sí | 3 / 4 / 19 |
| 326 | 6 | reto | Ag+Ma (D) | 9 | 12 | 9 | 1 | 0 | 20 | 75 % | hard | sí | 12 / 23 / 76 |
| 327 | 7 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 16 | 50 % | normal | sí | 12 / 115 / 166 |
| 328 | 8 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 17 | 38 % | normal | sí | 3 / 66 / 161 |
| 329 | 9 | reto | Ma (D) | 10 | 12 | 10 | 1 | 0 | 25 | 73 % | normal | no | 4 / 91 / 180 |
| 330 | 10 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 16 | 45 % | normal | sí | 12 / 120 / 177 |
| 331 | 11 | comodidad | Ma+Tu | 8 | 10 | 8 | 0 | 2 | 19 | 38 % | normal | sí | 3 / 3 / 123 |
| 332 | 12 | reto | Ba+Ho (D) | 9 | 11 | 9 | 0 | 0 | 23 | 85 % | hard | sí | 12 / 15 / 178 |
| 333 | 13 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 19 | 30 % | normal | sí | 12 / 148 / 216 |
| 334 | 14 | comodidad | Ag | 8 | 10 | 8 | 0 | 0 | 18 | 53 % | normal | sí | 3 / 36 / 178 |
| 335 | 15 | reto | Ma+Ho (D) | 9 | 11 | 9 | 0 | 0 | 30 | 88 % | hard | sí | 7 / 29 / 180 |
| 336 | 16 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 16 | 25 % | normal | sí | 12 / 137 / 199 |
| 337 | 17 | comodidad | Ba | 8 | 10 | 8 | 1 | 0 | 18 | 20 % | normal | sí | 3 / 5 / 25 |
| 338 | 18 | reto | Ag+Ba (D) | 9 | 11 | 9 | 1 | 0 | 30 | 83 % | hard | sí | 9 / 20 / 96 |
| 339 | 19 | remanso | S | 8 | 10 | 8 | 2 | 0 | 20 | 33 % | normal | sí | 12 / 66 / 175 |
| 340 | 20 | hito | Ag+Br+Ba (MD) | 11 | 13 | 11 | 0 | 0 | 27 | 100 % | very_hard | sí | 12 / 15 / 16 |
| 341 | 1 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 18 | 0 % | normal | sí | 6 / 20 / 32 |
| 342 | 2 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 17 | 0 % | normal | sí | 12 / 26 / 11 |
| 343 | 3 | estreno | *B2 | 7 | 10 | 7 | 2 | 0 | 14 | 0 % | normal | sí | 3 / 4 / 4 |
| 344 | 4 | estreno | *B2 | 7 | 10 | 7 | 2 | 0 | 15 | 0 % | normal | sí | 3 / 13 / 14 |
| 345 | 5 | estreno | *B2 | 7 | 10 | 7 | 2 | 0 | 15 | 0 % | normal | sí | 3 / 4 / 5 |
| 346 | 6 | reto | B2+Ln (D) | 10 | 12 | 10 | 1 | 0 | 24 | 78 % | hard | sí | 5 / 12 / 180 |
| 347 | 7 | estreno | *A2 | 8 | 10 | 8 | 0 | 0 | 19 | 10 % | normal | sí | 1 / 4 / 300 |
| 348 | 8 | estreno | *A2 | 8 | 9 | 7 | 0 | 0 | 14 | 10 % | normal | sí | 1 / 15 / 300 |
| 349 | 9 | reto | B2 (D) | 10 | 12 | 10 | 1 | 0 | 21 | 80 % | hard | sí | 3 / 10 / 180 |
| 350 | 10 | comodidad | Tu | 8 | 10 | 8 | 1 | 2 | 16 | 23 % | normal | sí | 3 / 7 / 360 |
| 351 | 11 | comodidad | Ba+Tu | 8 | 10 | 8 | 1 | 2 | 16 | 23 % | normal | sí | 1 / 1 / 360 |
| 352 | 12 | reto | B2+Re (D) | 9 | 11 | 9 | 1 | 0 | 23 | 58 % | normal | no | 2 / 13 / 210 |
| 353 | 13 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 16 | 60 % | normal | sí | 6 / 50 / 160 |
| 354 | 14 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 16 | 38 % | normal | sí | 3 / 46 / 152 |
| 355 | 15 | reto | B2+Re (D) | 9 | 11 | 9 | 1 | 0 | 21 | 88 % | hard | sí | 2 / 10 / 90 |
| 356 | 16 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 17 | 48 % | normal | sí | 6 / 67 / 178 |
| 357 | 17 | comodidad | Br+Tu | 8 | 10 | 8 | 1 | 1 | 17 | 28 % | normal | sí | 3 / 4 / 87 |
| 358 | 18 | reto | Ag+Ln (D) | 9 | 11 | 9 | 1 | 0 | 28 | 83 % | hard | sí | 1 / 58 / 210 |
| 359 | 19 | remanso | S | 8 | 10 | 8 | 2 | 0 | 17 | 28 % | normal | sí | 6 / 23 / 180 |
| 360 | 20 | hito | B2+Ln+Ag (MD) | 11 | 13 | 11 | 0 | 0 | 27 | 100 % | very_hard | sí | 30 / 44 / 21 |
| 361 | 1 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 19 | 3 % | normal | sí | 18 / 76 / 27 |
| 362 | 2 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 17 | 0 % | normal | sí | 9 / 26 / 11 |
| 363 | 3 | estreno | *La | 6 | 10 | 6 | 2 | 0 | 13 | 0 % | normal | sí | 3 / 4 / 5 |
| 364 | 4 | estreno | *La | 6 | 10 | 6 | 2 | 0 | 14 | 0 % | normal | sí | 3 / 13 / 15 |
| 365 | 5 | estreno | *La | 6 | 10 | 6 | 2 | 0 | 12 | 0 % | normal | sí | 3 / 10 / 11 |
| 366 | 6 | reto | Ho+Ag+Tu (D) | 9 | 12 | 9 | 0 | 3 | 20 | 88 % | hard | sí | 18 / 19 / 55 |
| 367 | 7 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 19 | 30 % | normal | sí | 18 / 196 / 175 |
| 368 | 8 | comodidad | Ag+Tu | 8 | 10 | 8 | 0 | 2 | 17 | 20 % | normal | sí | 3 / 5 / 46 |
| 369 | 9 | reto | Pi+Ma (D) | 9 | 11 | 9 | 0 | 0 | 22 | 75 % | hard | sí | 4 / 50 / 180 |
| 370 | 10 | comodidad | Tu | 8 | 10 | 8 | 1 | 2 | 16 | 23 % | normal | sí | 9 / 15 / 360 |
| 371 | 11 | comodidad | La | 8 | 11 | 8 | 1 | 0 | 16 | 35 % | normal | sí | 2 / 3 / 150 |
| 372 | 12 | reto | La+B2 (D) | 9 | 12 | 9 | 1 | 0 | 24 | 85 % | hard | sí | 3 / 17 / 180 |
| 373 | 13 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 17 | 45 % | normal | sí | 18 / 187 / 169 |
| 374 | 14 | comodidad | La+Al | 8 | 11 | 8 | 1 | 0 | 20 | 28 % | normal | sí | 3 / 3 / 112 |
| 375 | 15 | reto | La+Ba (D) | 9 | 12 | 9 | 1 | 0 | 23 | 88 % | hard | sí | 4 / 25 / 180 |
| 376 | 16 | comodidad | C | 8 | 9 | 8 | 1 | 0 | 17 | 48 % | normal | sí | 18 / 199 / 180 |
| 377 | 17 | comodidad | Br+Tu | 8 | 10 | 8 | 1 | 1 | 18 | 35 % | normal | sí | 1 / 4 / 360 |
| 378 | 18 | reto | La+Ho (D) | 9 | 12 | 9 | 0 | 0 | 24 | 75 % | hard | sí | 7 / 20 / 30 |
| 379 | 19 | remanso | S | 8 | 10 | 8 | 2 | 0 | 17 | 23 % | normal | sí | 18 / 91 / 184 |
| 380 | 20 | hito | Pi+Ba+Ag (MD) | 11 | 13 | 11 | 0 | 0 | 35 | 93 % | very_hard | sí | 18 / 36 / 15 |
| 381 | 1 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 18 | 3 % | normal | sí | 6 / 8 / 11 |
| 382 | 2 | recuperacion | R | 8 | 10 | 8 | 2 | 0 | 16 | 5 % | normal | sí | 3 / 8 / 12 |
| 383 | 3 | comodidad | Li | 8 | 10 | 8 | 2 | 0 | 17 | 50 % | normal | sí | 3 / 38 / 164 |
| 384 | 4 | comodidad | Tu | 8 | 10 | 8 | 1 | 2 | 18 | 28 % | normal | sí | 1 / 3 / 360 |
| 385 | 5 | comodidad | Ag | 8 | 10 | 8 | 0 | 0 | 18 | 33 % | normal | sí | 3 / 55 / 211 |
| 386 | 6 | reto | Ba+Tu (D) | 9 | 11 | 9 | 1 | 3 | 23 | 85 % | hard | sí | 1 / 2 / 1440 |
| 387 | 7 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 16 | 40 % | normal | sí | 6 / 72 / 204 |
| 388 | 8 | comodidad | Br+Tu | 8 | 10 | 8 | 1 | 1 | 16 | 30 % | normal | sí | 1 / 2 / 360 |
| 389 | 9 | reto | Li+Ha (D) | 9 | 11 | 9 | 2 | 0 | 27 | 85 % | hard | sí | 1 / 51 / 210 |
| 390 | 10 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 18 | 25 % | normal | sí | 6 / 76 / 193 |
| 391 | 11 | comodidad | Li | 8 | 10 | 8 | 2 | 0 | 18 | 25 % | normal | sí | 3 / 36 / 182 |
| 392 | 12 | reto | La+Ba (D) | 9 | 12 | 9 | 1 | 0 | 26 | 88 % | hard | sí | 1 / 7 / 180 |
| 393 | 13 | comodidad | La | 8 | 11 | 8 | 1 | 0 | 18 | 23 % | normal | sí | 1 / 7 / 150 |
| 394 | 14 | comodidad | Ag+Ch | 8 | 10 | 8 | 0 | 0 | 16 | 38 % | normal | sí | 3 / 14 / 57 |
| 395 | 15 | reto | Li+Ba (D) | 9 | 11 | 9 | 1 | 0 | 28 | 85 % | hard | sí | 5 / 14 / 210 |
| 396 | 16 | comodidad | C | 8 | 10 | 8 | 2 | 0 | 18 | 28 % | normal | sí | 6 / 82 / 194 |
| 397 | 17 | comodidad | Ho+Tu | 8 | 10 | 8 | 0 | 1 | 19 | 53 % | normal | sí | 3 / 3 / 135 |
| 398 | 18 | reto | Ha+Ba (D) | 9 | 11 | 9 | 1 | 0 | 29 | 88 % | hard | sí | 2 / 14 / 210 |
| 399 | 19 | remanso | S | 8 | 10 | 8 | 2 | 0 | 17 | 25 % | normal | sí | 6 / 40 / 199 |
| 400 | 20 | hito | Li+Ha+Ag (MD) | 11 | 13 | 11 | 1 | 0 | 34 | 100 % | very_hard | sí | 6 / 23 / 34 |
| 401 | 1 | recuperacion | R | 8 | — | — | — | — | — | — | sin colocar | — | 18 / 51 / 22 |
| 402 | 2 | recuperacion | R | 8 | — | — | — | — | — | — | sin colocar | — | 18 / 57 / 13 |
| 403 | 3 | estreno | *Fl | 8 | — | — | — | — | — | — | sin colocar | — | 3 / 9 / 23 |
| 404 | 4 | estreno | *Fl | 8 | — | — | — | — | — | — | sin colocar | — | 3 / 19 / 59 |
| 405 | 5 | estreno | *Fl | 8 | — | — | — | — | — | — | sin colocar | — | 3 / 12 / 48 |
| 406 | 6 | reto | Ag+Ll (D) | 9 | — | — | — | — | — | — | sin colocar | — | 18 / 18 / 19 |
| 407 | 7 | comodidad | Fl | 8 | — | — | — | — | — | — | sin colocar | — | 18 / 118 / 200 |
| 408 | 8 | comodidad | Tu | 8 | — | — | — | — | — | — | sin colocar | — | 3 / 5 / 53 |
| 409 | 9 | reto | Fl (D) | 9 | — | — | — | — | — | — | sin colocar | — | 11 / 129 / 210 |
| 410 | 10 | comodidad | C | 8 | — | — | — | — | — | — | sin colocar | — | 18 / 183 / 174 |
| 411 | 11 | comodidad | Ag | 8 | — | — | — | — | — | — | sin colocar | — | 3 / 53 / 203 |
| 412 | 12 | reto | Fl+La (D) | 9 | — | — | — | — | — | — | sin colocar | — | 14 / 35 / 180 |
| 413 | 13 | comodidad | C | 8 | — | — | — | — | — | — | sin colocar | — | 18 / 181 / 182 |
| 414 | 14 | comodidad | Br+Tu | 8 | — | — | — | — | — | — | sin colocar | — | 2 / 2 / 360 |
| 415 | 15 | reto | Fl+Ll (D) | 9 | — | — | — | — | — | — | sin colocar | — | 18 / 28 / 139 |
| 416 | 16 | comodidad | C | 8 | — | — | — | — | — | — | sin colocar | — | 18 / 177 / 168 |
| 417 | 17 | comodidad | Ho+Tu | 8 | — | — | — | — | — | — | sin colocar | — | 3 / 4 / 259 |
| 418 | 18 | reto | Ba+Tu (D) | 9 | — | — | — | — | — | — | sin colocar | — | 1 / 1 / 360 |
| 419 | 19 | remanso | S | 8 | — | — | — | — | — | — | sin colocar | — | 18 / 117 / 214 |
| 420 | 20 | cuspide | Fl+Ba+A2 (MD) | 12 | — | — | — | — | — | — | sin colocar | — | 16 / 220 / 66 |
