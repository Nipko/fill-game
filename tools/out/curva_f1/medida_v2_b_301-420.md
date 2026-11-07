# Medida de la curva: campaign_v2, niveles 301-420, variante b

Generado por (no editar a mano):

```
dotnet run --project tools/level-generator -c Release -- --measure --spec tools/level-generator/specs/campaign_v2.json --levels 301-420 --variant b --report <este archivo>
```

Fases 1-3 de `--spec` (candidatos, índice D y colocación en el ciclo de 20 de D-009) sin alivio ni packs; un ciclo sin
colocación no detiene la medida. «En ventana»: el elegido mide la ventana de su slot (reto 75-89 %, hito ≥ 90 %, comodidad
20-60 %…); un reto o hito fuera de ella se publicaría con la etiqueta que mide (c.2). T = cuias del tablero, K = colores,
libres = cuias vacías, bot = fallo del bot casual ×40 del elegido. Topes de T por papel: D-068 (P2 A).

## Resumen

| Medida | Valor |
|---|---|
| Niveles | 120 (6 ciclos) |
| Ciclos sin colocación | 0 |
| Retos colocados en su ventana | 29 de 30 |
| Hitos colocados en su ventana | 6 de 6 |
| Retos con algún candidato en su ventana | 29 de 30 |
| T media / mediana (colocados) | 12.9 / 13 |
| K media / mediana (colocados) | 10.9 / 11 |
| Niveles con T ≤ 11 / T ≥ 12 / T ≥ 13 | 0 / 120 / 98 |
| Niveles con ocultas (dosis) | 4 (3 %; 7 dosis) |
| Fuera del tope de T de su papel | 111 |

## Por papel

| Papel | Tope T | Niveles | T mín-med-máx | T (recuento) | K mín-med-máx | Libres (med.) | Bot (mediana) | Bot mín-máx | En ventana | Fuera del tope | Con ocultas |
|---|---|---|---|---|---|---|---|---|---|---|---|
| recuperacion | 8-10 | 12 | 12-12-12 | 12: 12 | 10-10-10 | 2 | 9 % | 0 %-28 % | 12 | 12 | 0 |
| estreno | 9-10 | 6 | 12-13.5-14 | 12: 2 · 13: 1 · 14: 3 | 10-11-11 | 2 | 5 % | 0 %-15 % | 6 | 6 | 0 |
| comodidad | 9-11 | 60 | 12-13-13 | 12: 2 · 13: 58 | 11-11-11 | 2 | 45 % | 20 %-60 % | 60 | 60 | 0 |
| reto | 10-12 | 30 | 12-13-14 | 12: 3 · 13: 24 · 14: 3 | 11-11-11 | 1 | 83 % | 45 %-88 % | 29 | 27 | 4 |
| remanso | 8-10 | 6 | 12-12.5-13 | 12: 3 · 13: 3 | 10-10.5-11 | 2 | 35 % | 23 %-40 % | 6 | 6 | 0 |
| hito | 12-13 | 5 | 13-13-13 | 13: 5 | 11-11-11 | 1 | 100 % | 90 %-100 % | 5 | 0 | 0 |
| cuspide | 13-14 | 1 | 13-13-13 | 13: 1 | 11-11-11 | 1 | 95 % | 95 %-95 % | 1 | 0 | 0 |

## Por ciclo

| Ciclo | Coloca | Retos en ventana | T de los retos | Bot de los retos | Hito | T del hito | Bot del hito | En ventana |
|---|---|---|---|---|---|---|---|---|
| 301-320 | sí | 5 de 5 | 13, 13, 13, 13, 13 | 85 %, 80 %, 88 %, 78 %, 80 % | Ha+Ll+Br (MD) | 13 | 100 % | sí |
| 321-340 | sí | 5 de 5 | 13, 13, 13, 14, 13 | 85 %, 85 %, 88 %, 80 %, 80 % | Br+Ba (MD) | 13 | 100 % | sí |
| 341-360 | sí | 5 de 5 | 13, 13, 13, 12, 13 | 78 %, 78 %, 75 %, 88 %, 88 % | B2+Ln (MD) | 13 | 90 % | sí |
| 361-380 | sí | 4 de 5 | 13, 14, 13, 13, 14 | 88 %, 45 %, 75 %, 88 %, 80 % | Pi+Ho (MD) | 13 | 100 % | sí |
| 381-400 | sí | 5 de 5 | 13, 13, 13, 13, 13 | 83 %, 75 %, 80 %, 88 %, 83 % | Li+Ha+Ba (MD) | 13 | 98 % | sí |
| 401-420 | sí | 5 de 5 | 13, 12, 12, 13, 13 | 85 %, 78 %, 85 %, 88 %, 83 % | Fl+B2 (MD) | 13 | 95 % | sí |

## Retos e hitos

Candidatos: en ventana (del pool) / medidos / intentos del slot, contando las rondas extra.

| Nivel | p | Papel | Celda | K plan | T | K | Libres | Ocultas | Par | Bot | Etiqueta | En ventana | Candidatos |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 306 | 6 | reto | Ha+Ll (D) | 11 | 13 | 11 | 1 | 0 | 22 | 85 % | hard | sí | 6 / 9 / 84 |
| 309 | 9 | reto | Br+Ll (D) | 11 | 13 | 11 | 0 | 0 | 25 | 80 % | hard | sí | 3 / 7 / 210 |
| 312 | 12 | reto | Ha+Br (D) | 11 | 13 | 11 | 1 | 0 | 27 | 88 % | hard | sí | 4 / 11 / 210 |
| 315 | 15 | reto | Br+Tu (D) | 11 | 13 | 11 | 1 | 3 | 25 | 78 % | hard | sí | 6 / 9 / 155 |
| 318 | 18 | reto | Ln+Ll (D) | 11 | 13 | 11 | 1 | 0 | 25 | 80 % | hard | sí | 6 / 16 / 121 |
| 320 | 20 | hito | Ha+Ll+Br (MD) | 11 | 13 | 11 | 0 | 0 | 25 | 100 % | very_hard | sí | 4 / 11 / 90 |
| 326 | 6 | reto | Br+Ma (D) | 11 | 13 | 11 | 0 | 0 | 25 | 85 % | hard | sí | 6 / 7 / 45 |
| 329 | 9 | reto | Ma (D) | 11 | 13 | 11 | 1 | 0 | 23 | 85 % | hard | sí | 4 / 60 / 210 |
| 332 | 12 | reto | Ba+Ho (D) | 11 | 13 | 11 | 0 | 0 | 23 | 88 % | hard | sí | 6 / 10 / 119 |
| 335 | 15 | reto | Ma+Ho (D) | 11 | 14 | 11 | 1 | 0 | 26 | 80 % | hard | sí | 9 / 41 / 180 |
| 338 | 18 | reto | Ma+Ba (D) | 11 | 13 | 11 | 0 | 0 | 24 | 80 % | hard | sí | 6 / 19 / 137 |
| 340 | 20 | hito | Br+Ba (MD) | 11 | 13 | 11 | 1 | 0 | 24 | 100 % | very_hard | sí | 5 / 9 / 90 |
| 346 | 6 | reto | B2+Ln (D) | 11 | 13 | 11 | 1 | 0 | 25 | 78 % | hard | sí | 7 / 34 / 210 |
| 349 | 9 | reto | B2 (D) | 11 | 13 | 11 | 1 | 0 | 25 | 78 % | hard | sí | 7 / 13 / 139 |
| 352 | 12 | reto | B2+Re (D) | 11 | 13 | 11 | 1 | 0 | 22 | 75 % | hard | sí | 6 / 26 / 210 |
| 355 | 15 | reto | Re (D) | 11 | 12 | 11 | 0 | 0 | 23 | 88 % | hard | sí | 1 / 84 / 210 |
| 358 | 18 | reto | Ln+Re (D) | 11 | 13 | 11 | 2 | 0 | 32 | 88 % | hard | sí | 2 / 85 / 210 |
| 360 | 20 | hito | B2+Ln (MD) | 11 | 13 | 11 | 1 | 0 | 29 | 90 % | very_hard | sí | 9 / 15 / 90 |
| 366 | 6 | reto | Ho+Tu (D) | 11 | 13 | 11 | 0 | 2 | 25 | 88 % | hard | sí | 12 / 12 / 24 |
| 369 | 9 | reto | Pi+B2 (D) | 11 | 14 | 11 | 1 | 0 | 24 | 45 % | normal | no | 0 / 4 / 90 |
| 372 | 12 | reto | B2+Ho (D) | 11 | 13 | 11 | 0 | 0 | 27 | 75 % | hard | sí | 7 / 22 / 210 |
| 375 | 15 | reto | Pi+Ba (D) | 11 | 13 | 11 | 0 | 0 | 26 | 88 % | hard | sí | 12 / 62 / 136 |
| 378 | 18 | reto | Pi+Ho (D) | 11 | 14 | 11 | 1 | 0 | 27 | 80 % | hard | sí | 9 / 31 / 180 |
| 380 | 20 | hito | Pi+Ho (MD) | 11 | 13 | 11 | 0 | 0 | 33 | 100 % | very_hard | sí | 9 / 29 / 60 |
| 386 | 6 | reto | Ba+Tu (D) | 11 | 13 | 11 | 1 | 1 | 26 | 83 % | hard | sí | 3 / 6 / 360 |
| 389 | 9 | reto | Li+Ha (D) | 11 | 13 | 11 | 2 | 0 | 34 | 75 % | hard | sí | 3 / 175 / 210 |
| 392 | 12 | reto | Ba (D) | 11 | 13 | 11 | 1 | 0 | 34 | 80 % | hard | sí | 11 / 57 / 210 |
| 395 | 15 | reto | Li+Ba (D) | 11 | 13 | 11 | 1 | 0 | 35 | 88 % | hard | sí | 6 / 22 / 210 |
| 398 | 18 | reto | Ha+Ba (D) | 11 | 13 | 11 | 1 | 0 | 34 | 83 % | hard | sí | 2 / 39 / 210 |
| 400 | 20 | hito | Li+Ha+Ba (MD) | 11 | 13 | 11 | 1 | 0 | 34 | 98 % | very_hard | sí | 18 / 64 / 38 |
| 406 | 6 | reto | Ba+Ll (D) | 11 | 13 | 11 | 1 | 0 | 22 | 85 % | hard | sí | 18 / 44 / 152 |
| 409 | 9 | reto | Fl (D) | 11 | 12 | 11 | 1 | 0 | 22 | 78 % | hard | sí | 2 / 151 / 210 |
| 412 | 12 | reto | Fl+Ba (D) | 11 | 12 | 11 | 0 | 0 | 23 | 85 % | hard | sí | 1 / 21 / 210 |
| 415 | 15 | reto | Fl+Ll (D) | 11 | 13 | 11 | 1 | 0 | 22 | 88 % | hard | sí | 18 / 63 / 162 |
| 418 | 18 | reto | Ba+Tu (D) | 11 | 13 | 11 | 1 | 1 | 24 | 83 % | hard | sí | 4 / 6 / 360 |
| 420 | 20 | cuspide | Fl+B2 (MD) | 11 | 13 | 11 | 1 | 0 | 24 | 95 % | very_hard | sí | 18 / 22 / 17 |

## Todos los niveles

| Nivel | p | Papel | Celda | K plan | T | K | Libres | Ocultas | Par | Bot | Etiqueta | En ventana | Candidatos |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 301 | 1 | recuperacion | R | 10 | 12 | 10 | 2 | 0 | 21 | 5 % | normal | sí | 6 / 36 / 108 |
| 302 | 2 | recuperacion | R | 10 | 12 | 10 | 2 | 0 | 22 | 0 % | normal | sí | 3 / 21 / 57 |
| 303 | 3 | comodidad | Ll | 11 | 13 | 11 | 1 | 0 | 22 | 25 % | normal | sí | 3 / 8 / 22 |
| 304 | 4 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 23 | 50 % | normal | sí | 3 / 44 / 173 |
| 305 | 5 | comodidad | Br | 11 | 13 | 11 | 1 | 0 | 22 | 20 % | normal | sí | 3 / 11 / 83 |
| 306 | 6 | reto | Ha+Ll (D) | 11 | 13 | 11 | 1 | 0 | 22 | 85 % | hard | sí | 6 / 9 / 84 |
| 307 | 7 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 23 | 40 % | normal | sí | 6 / 92 / 292 |
| 308 | 8 | comodidad | Re | 11 | 13 | 11 | 1 | 0 | 22 | 53 % | normal | sí | 3 / 46 / 259 |
| 309 | 9 | reto | Br+Ll (D) | 11 | 13 | 11 | 0 | 0 | 25 | 80 % | hard | sí | 3 / 7 / 210 |
| 310 | 10 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 24 | 43 % | normal | sí | 6 / 101 / 285 |
| 311 | 11 | comodidad | Ba | 11 | 13 | 11 | 1 | 0 | 26 | 53 % | normal | sí | 3 / 7 / 13 |
| 312 | 12 | reto | Ha+Br (D) | 11 | 13 | 11 | 1 | 0 | 27 | 88 % | hard | sí | 4 / 11 / 210 |
| 313 | 13 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 25 | 43 % | normal | sí | 6 / 91 / 257 |
| 314 | 14 | comodidad | Ho | 11 | 13 | 11 | 1 | 0 | 23 | 55 % | normal | sí | 3 / 7 / 21 |
| 315 | 15 | reto | Br+Tu (D) | 11 | 13 | 11 | 1 | 3 | 25 | 78 % | hard | sí | 6 / 9 / 155 |
| 316 | 16 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 23 | 53 % | normal | sí | 6 / 67 / 210 |
| 317 | 17 | comodidad | Ll | 11 | 13 | 11 | 1 | 0 | 22 | 30 % | normal | sí | 3 / 12 / 24 |
| 318 | 18 | reto | Ln+Ll (D) | 11 | 13 | 11 | 1 | 0 | 25 | 80 % | hard | sí | 6 / 16 / 121 |
| 319 | 19 | remanso | S | 11 | 13 | 11 | 2 | 0 | 26 | 33 % | normal | sí | 6 / 39 / 290 |
| 320 | 20 | hito | Ha+Ll+Br (MD) | 11 | 13 | 11 | 0 | 0 | 25 | 100 % | very_hard | sí | 4 / 11 / 90 |
| 321 | 1 | recuperacion | R | 10 | 12 | 10 | 2 | 0 | 20 | 28 % | normal | sí | 6 / 42 / 153 |
| 322 | 2 | recuperacion | R | 10 | 12 | 10 | 2 | 0 | 24 | 20 % | normal | sí | 3 / 11 / 31 |
| 323 | 3 | comodidad | Ma | 11 | 13 | 11 | 1 | 0 | 22 | 25 % | normal | sí | 3 / 16 / 36 |
| 324 | 4 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 27 | 53 % | normal | sí | 3 / 33 / 169 |
| 325 | 5 | comodidad | Ba | 11 | 13 | 11 | 1 | 0 | 25 | 33 % | normal | sí | 3 / 12 / 37 |
| 326 | 6 | reto | Br+Ma (D) | 11 | 13 | 11 | 0 | 0 | 25 | 85 % | hard | sí | 6 / 7 / 45 |
| 327 | 7 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 22 | 45 % | normal | sí | 6 / 67 / 204 |
| 328 | 8 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 23 | 40 % | normal | sí | 3 / 64 / 193 |
| 329 | 9 | reto | Ma (D) | 11 | 13 | 11 | 1 | 0 | 23 | 85 % | hard | sí | 4 / 60 / 210 |
| 330 | 10 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 23 | 60 % | normal | sí | 6 / 86 / 254 |
| 331 | 11 | comodidad | Ma | 11 | 13 | 11 | 1 | 0 | 23 | 25 % | normal | sí | 3 / 23 / 58 |
| 332 | 12 | reto | Ba+Ho (D) | 11 | 13 | 11 | 0 | 0 | 23 | 88 % | hard | sí | 6 / 10 / 119 |
| 333 | 13 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 24 | 35 % | normal | sí | 6 / 42 / 201 |
| 334 | 14 | comodidad | Re | 11 | 13 | 11 | 0 | 0 | 22 | 30 % | normal | sí | 3 / 56 / 263 |
| 335 | 15 | reto | Ma+Ho (D) | 11 | 14 | 11 | 1 | 0 | 26 | 80 % | hard | sí | 9 / 41 / 180 |
| 336 | 16 | comodidad | C | 11 | 12 | 11 | 1 | 0 | 22 | 60 % | normal | sí | 6 / 75 / 207 |
| 337 | 17 | comodidad | Ba | 11 | 13 | 11 | 1 | 0 | 24 | 48 % | normal | sí | 3 / 22 / 59 |
| 338 | 18 | reto | Ma+Ba (D) | 11 | 13 | 11 | 0 | 0 | 24 | 80 % | hard | sí | 6 / 19 / 137 |
| 339 | 19 | remanso | S | 11 | 13 | 11 | 2 | 0 | 24 | 28 % | normal | sí | 6 / 33 / 292 |
| 340 | 20 | hito | Br+Ba (MD) | 11 | 13 | 11 | 1 | 0 | 24 | 100 % | very_hard | sí | 5 / 9 / 90 |
| 341 | 1 | recuperacion | R | 10 | 12 | 10 | 2 | 0 | 21 | 10 % | normal | sí | 12 / 81 / 140 |
| 342 | 2 | recuperacion | R | 10 | 12 | 10 | 2 | 0 | 21 | 13 % | normal | sí | 6 / 66 / 79 |
| 343 | 3 | estreno | *B2 | 11 | 14 | 11 | 2 | 0 | 22 | 0 % | normal | sí | 3 / 11 / 11 |
| 344 | 4 | estreno | *B2 | 11 | 14 | 11 | 2 | 0 | 22 | 0 % | normal | sí | 3 / 32 / 34 |
| 345 | 5 | estreno | *B2 | 11 | 14 | 11 | 2 | 0 | 22 | 0 % | normal | sí | 3 / 25 / 27 |
| 346 | 6 | reto | B2+Ln (D) | 11 | 13 | 11 | 1 | 0 | 25 | 78 % | hard | sí | 7 / 34 / 210 |
| 347 | 7 | comodidad | B2 | 11 | 13 | 11 | 1 | 0 | 22 | 38 % | normal | sí | 8 / 38 / 450 |
| 348 | 8 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 22 | 60 % | normal | sí | 3 / 35 / 160 |
| 349 | 9 | reto | B2 (D) | 11 | 13 | 11 | 1 | 0 | 25 | 78 % | hard | sí | 7 / 13 / 139 |
| 350 | 10 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 22 | 28 % | normal | sí | 12 / 173 / 266 |
| 351 | 11 | comodidad | Ln | 11 | 13 | 11 | 2 | 0 | 24 | 55 % | normal | sí | 3 / 51 / 218 |
| 352 | 12 | reto | B2+Re (D) | 11 | 13 | 11 | 1 | 0 | 22 | 75 % | hard | sí | 6 / 26 / 210 |
| 353 | 13 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 24 | 58 % | normal | sí | 12 / 125 / 176 |
| 354 | 14 | comodidad | Ha+Al | 11 | 13 | 11 | 1 | 0 | 23 | 45 % | normal | sí | 3 / 79 / 328 |
| 355 | 15 | reto | Re (D) | 11 | 12 | 11 | 0 | 0 | 23 | 88 % | hard | sí | 1 / 84 / 210 |
| 356 | 16 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 22 | 45 % | normal | sí | 12 / 163 / 284 |
| 357 | 17 | comodidad | Br | 11 | 13 | 11 | 1 | 0 | 23 | 20 % | normal | sí | 3 / 11 / 75 |
| 358 | 18 | reto | Ln+Re (D) | 11 | 13 | 11 | 2 | 0 | 32 | 88 % | hard | sí | 2 / 85 / 210 |
| 359 | 19 | remanso | S | 11 | 13 | 11 | 2 | 0 | 24 | 38 % | normal | sí | 12 / 97 / 292 |
| 360 | 20 | hito | B2+Ln (MD) | 11 | 13 | 11 | 1 | 0 | 29 | 90 % | very_hard | sí | 9 / 15 / 90 |
| 361 | 1 | recuperacion | R | 10 | 12 | 10 | 2 | 0 | 23 | 8 % | normal | sí | 12 / 86 / 153 |
| 362 | 2 | recuperacion | R | 10 | 12 | 10 | 2 | 0 | 20 | 8 % | normal | sí | 6 / 22 / 24 |
| 363 | 3 | comodidad | Pi | 11 | 13 | 11 | 1 | 0 | 22 | 50 % | normal | sí | 3 / 77 / 178 |
| 364 | 4 | comodidad | C | 11 | 12 | 11 | 1 | 0 | 22 | 58 % | normal | sí | 3 / 51 / 171 |
| 365 | 5 | comodidad | Ho | 11 | 13 | 11 | 1 | 0 | 24 | 35 % | normal | sí | 3 / 7 / 24 |
| 366 | 6 | reto | Ho+Tu (D) | 11 | 13 | 11 | 0 | 2 | 25 | 88 % | hard | sí | 12 / 12 / 24 |
| 367 | 7 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 23 | 48 % | normal | sí | 12 / 177 / 249 |
| 368 | 8 | comodidad | Ha | 11 | 13 | 11 | 1 | 0 | 23 | 53 % | normal | sí | 3 / 51 / 224 |
| 369 | 9 | reto | Pi+B2 (D) | 11 | 14 | 11 | 1 | 0 | 24 | 45 % | normal | no | 0 / 4 / 90 |
| 370 | 10 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 23 | 23 % | normal | sí | 12 / 206 / 342 |
| 371 | 11 | comodidad | Re | 11 | 13 | 11 | 1 | 0 | 22 | 33 % | normal | sí | 3 / 17 / 183 |
| 372 | 12 | reto | B2+Ho (D) | 11 | 13 | 11 | 0 | 0 | 27 | 75 % | hard | sí | 7 / 22 / 210 |
| 373 | 13 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 22 | 58 % | normal | sí | 12 / 158 / 244 |
| 374 | 14 | comodidad | Al | 11 | 13 | 11 | 1 | 0 | 24 | 45 % | normal | sí | 3 / 70 / 246 |
| 375 | 15 | reto | Pi+Ba (D) | 11 | 13 | 11 | 0 | 0 | 26 | 88 % | hard | sí | 12 / 62 / 136 |
| 376 | 16 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 22 | 50 % | normal | sí | 12 / 204 / 256 |
| 377 | 17 | comodidad | Br | 11 | 13 | 11 | 1 | 0 | 22 | 53 % | normal | sí | 3 / 15 / 85 |
| 378 | 18 | reto | Pi+Ho (D) | 11 | 14 | 11 | 1 | 0 | 27 | 80 % | hard | sí | 9 / 31 / 180 |
| 379 | 19 | remanso | S | 11 | 12 | 10 | 2 | 0 | 24 | 38 % | normal | sí | 9 / 158 / 396 |
| 380 | 20 | hito | Pi+Ho (MD) | 11 | 13 | 11 | 0 | 0 | 33 | 100 % | very_hard | sí | 9 / 29 / 60 |
| 381 | 1 | recuperacion | R | 10 | 12 | 10 | 2 | 0 | 20 | 18 % | normal | sí | 18 / 130 / 132 |
| 382 | 2 | recuperacion | R | 10 | 12 | 10 | 2 | 0 | 21 | 15 % | normal | sí | 9 / 63 / 113 |
| 383 | 3 | comodidad | Li | 11 | 13 | 11 | 2 | 0 | 25 | 50 % | normal | sí | 3 / 42 / 194 |
| 384 | 4 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 24 | 35 % | normal | sí | 3 / 49 / 192 |
| 385 | 5 | comodidad | Ha | 11 | 13 | 11 | 1 | 0 | 22 | 50 % | normal | sí | 3 / 98 / 352 |
| 386 | 6 | reto | Ba+Tu (D) | 11 | 13 | 11 | 1 | 1 | 26 | 83 % | hard | sí | 3 / 6 / 360 |
| 387 | 7 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 25 | 30 % | normal | sí | 18 / 206 / 220 |
| 388 | 8 | comodidad | Br | 11 | 13 | 11 | 1 | 0 | 23 | 20 % | normal | sí | 3 / 7 / 77 |
| 389 | 9 | reto | Li+Ha (D) | 11 | 13 | 11 | 2 | 0 | 34 | 75 % | hard | sí | 3 / 175 / 210 |
| 390 | 10 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 22 | 40 % | normal | sí | 18 / 169 / 191 |
| 391 | 11 | comodidad | Li | 11 | 13 | 11 | 2 | 0 | 23 | 45 % | normal | sí | 3 / 56 / 258 |
| 392 | 12 | reto | Ba (D) | 11 | 13 | 11 | 1 | 0 | 34 | 80 % | hard | sí | 11 / 57 / 210 |
| 393 | 13 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 23 | 28 % | normal | sí | 18 / 206 / 200 |
| 394 | 14 | comodidad | Ch | 11 | 13 | 11 | 1 | 0 | 25 | 43 % | normal | sí | 3 / 34 / 83 |
| 395 | 15 | reto | Li+Ba (D) | 11 | 13 | 11 | 1 | 0 | 35 | 88 % | hard | sí | 6 / 22 / 210 |
| 396 | 16 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 22 | 35 % | normal | sí | 18 / 255 / 219 |
| 397 | 17 | comodidad | Ho | 11 | 13 | 11 | 1 | 0 | 24 | 45 % | normal | sí | 3 / 6 / 18 |
| 398 | 18 | reto | Ha+Ba (D) | 11 | 13 | 11 | 1 | 0 | 34 | 83 % | hard | sí | 2 / 39 / 210 |
| 399 | 19 | remanso | S | 11 | 12 | 10 | 2 | 0 | 23 | 23 % | normal | sí | 16 / 180 / 450 |
| 400 | 20 | hito | Li+Ha+Ba (MD) | 11 | 13 | 11 | 1 | 0 | 34 | 98 % | very_hard | sí | 18 / 64 / 38 |
| 401 | 1 | recuperacion | R | 10 | 12 | 10 | 2 | 0 | 20 | 5 % | normal | sí | 18 / 139 / 154 |
| 402 | 2 | recuperacion | R | 10 | 12 | 10 | 2 | 0 | 20 | 3 % | normal | sí | 18 / 107 / 113 |
| 403 | 3 | estreno | *Fl | 11 | 12 | 10 | 2 | 0 | 21 | 15 % | normal | sí | 3 / 24 / 310 |
| 404 | 4 | estreno | *Fl | 11 | 12 | 10 | 2 | 0 | 20 | 13 % | normal | sí | 3 / 19 / 278 |
| 405 | 5 | estreno | *Fl | 11 | 13 | 11 | 2 | 0 | 23 | 10 % | normal | sí | 3 / 21 / 301 |
| 406 | 6 | reto | Ba+Ll (D) | 11 | 13 | 11 | 1 | 0 | 22 | 85 % | hard | sí | 18 / 44 / 152 |
| 407 | 7 | comodidad | Fl | 11 | 13 | 11 | 2 | 0 | 26 | 55 % | normal | sí | 15 / 158 / 300 |
| 408 | 8 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 22 | 50 % | normal | sí | 3 / 73 / 229 |
| 409 | 9 | reto | Fl (D) | 11 | 12 | 11 | 1 | 0 | 22 | 78 % | hard | sí | 2 / 151 / 210 |
| 410 | 10 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 25 | 30 % | normal | sí | 18 / 282 / 258 |
| 411 | 11 | comodidad | Re | 11 | 13 | 11 | 0 | 0 | 22 | 55 % | normal | sí | 12 / 116 / 234 |
| 412 | 12 | reto | Fl+Ba (D) | 11 | 12 | 11 | 0 | 0 | 23 | 85 % | hard | sí | 1 / 21 / 210 |
| 413 | 13 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 24 | 33 % | normal | sí | 18 / 176 / 191 |
| 414 | 14 | comodidad | Br | 11 | 13 | 11 | 1 | 0 | 23 | 20 % | normal | sí | 3 / 6 / 47 |
| 415 | 15 | reto | Fl+Ll (D) | 11 | 13 | 11 | 1 | 0 | 22 | 88 % | hard | sí | 18 / 63 / 162 |
| 416 | 16 | comodidad | C | 11 | 13 | 11 | 2 | 0 | 22 | 38 % | normal | sí | 18 / 311 / 254 |
| 417 | 17 | comodidad | Ha | 11 | 13 | 11 | 1 | 0 | 22 | 60 % | normal | sí | 3 / 110 / 382 |
| 418 | 18 | reto | Ba+Tu (D) | 11 | 13 | 11 | 1 | 1 | 24 | 83 % | hard | sí | 4 / 6 / 360 |
| 419 | 19 | remanso | S | 11 | 12 | 10 | 2 | 0 | 20 | 40 % | normal | sí | 18 / 204 / 398 |
| 420 | 20 | cuspide | Fl+B2 (MD) | 11 | 13 | 11 | 1 | 0 | 24 | 95 % | very_hard | sí | 18 / 22 / 17 |
