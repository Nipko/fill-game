Lote: 20 niveles, 20 aceptados, tiempo total 219.8 s (Python 3.14.3, presupuesto A* 300000, bot x40).

| # | K | Vacios | Cuias | Intentos gen | Par | Estados A* | t solve (ms) | Fallo bot | Ramif. | Trampas | Compl. | D | Banda | Etiqueta | Alivio +1 vacio par | Alivio -1 color (par) | t total (s) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 5 | 2 | 7 | 5 | 11 | 347 | 16 | 0.00 | 4.45 | 0.00 | 28 | -0.48 | comfort | normal | 11 | 2 (9) | 0.26 |
| 2 | 6 | 1 | 7 | 1 | 12 | 80 | 2 | 0.10 | 2.33 | 0.04 | 32 | -0.68 | recovery | normal | 12 | 2 (11) | 0.08 |
| 3 | 6 | 2 | 8 | 2 | 14 | 678 | 36 | 0.00 | 4.43 | 0.00 | 36 | -0.33 | comfort | normal | 14 | 3 (13) | 0.88 |
| 4 | 7 | 1 | 8 | 3 | 14 | 110 | 8 | 0.50 | 2.79 | 0.03 | 40 | -0.20 | comfort | normal | 14 | 1 (13) | 0.24 |
| 5 | 7 | 2 | 9 | 13 | 15 | 1963 | 114 | 0.00 | 4.80 | 0.00 | 38 | -0.43 | comfort | normal | 15 | 2 (14) | 2.71 |
| 6 | 8 | 1 | 9 | 8 | 17 | 184 | 11 | 0.72 | 3.00 | 0.16 | 46 | +0.33 | comfort | normal | 17 | 1 (16) | 0.80 |
| 7 | 8 | 2 | 10 | 2 | 18 | 2142 | 125 | 0.05 | 5.28 | 0.00 | 46 | -0.17 | comfort | normal | 18 | 1 (16) | 5.60 |
| 8 | 9 | 1 | 10 | 21 | 18 | 159 | 8 | 0.60 | 2.39 | 0.12 | 52 | -0.07 | comfort | normal | 18 | 1 (16) | 0.55 |
| 9 | 9 | 2 | 11 | 9 | 21 | 5055 | 372 | 0.45 | 5.81 | 0.03 | 54 | +0.51 | challenge | normal | 21 | 9 (20) | 17.94 |
| 10 | 10 | 1 | 11 | 12 | 19 | 214 | 12 | 0.68 | 3.11 | 0.31 | 54 | +0.12 | comfort | normal | 19 | 3 (18) | 1.55 |
| 11 | 10 | 2 | 12 | 2 | 24 | 18291 | 1513 | 0.33 | 4.50 | 0.03 | 62 | +0.34 | comfort | normal | None | 2 (22) | 99.54 |
| 12 | 11 | 1 | 12 | 2 | 22 | 326 | 20 | 0.80 | 3.18 | 0.10 | 62 | +0.24 | comfort | hard | 22 | 1 (21) | 4.48 |
| 13 | 5 | 2 | 7 | 5 | 12 | 86 | 5 | 0.12 | 4.67 | 0.00 | 32 | -0.17 | comfort | normal | 12 | 2 (10) | 0.19 |
| 14 | 7 | 1 | 8 | 2 | 16 | 114 | 4 | 0.57 | 2.25 | 0.08 | 42 | +0.16 | comfort | normal | 16 | 1 (14) | 0.43 |
| 15 | 8 | 1 | 9 | 15 | 12 | 72 | 3 | 0.03 | 2.58 | 0.19 | 38 | -1.18 | recovery | normal | 12 | 1 (11) | 0.14 |
| 16 | 9 | 1 | 10 | 4 | 18 | 162 | 8 | 0.90 | 3.06 | 0.33 | 50 | +0.53 | challenge | very_hard | 18 | 2 (17) | 1.13 |
| 17 | 10 | 1 | 11 | 1 | 21 | 423 | 26 | 0.90 | 2.76 | 0.29 | 56 | +0.60 | challenge | very_hard | 21 | 3 (19) | 3.78 |
| 18 | 10 | 2 | 12 | 7 | 23 | 13272 | 1131 | 0.57 | 7.87 | 0.02 | 60 | +0.91 | milestone | normal | 23 | 5 (22) | 65.71 |
| 19 | 11 | 1 | 12 | 2 | 22 | 546 | 36 | 0.93 | 3.50 | 0.31 | 62 | +0.60 | challenge | very_hard | 22 | 7 (21) | 6.33 |
| 20 | 8 | 2 | 10 | 3 | 18 | 2928 | 199 | 0.53 | 6.11 | 0.00 | 46 | +0.46 | challenge | normal | 18 | 2 (17) | 7.45 |

Solver A*: mediana 18 ms, media 182 ms, max 1513 ms; estados expandidos mediana 336, max 18291.
Bot casual x40: mediana 20 ms por nivel.
Trampas (hasta 120 pares, A* 3k): mediana 0.16 s por nivel.
Variantes de alivio: mediana 1.07 s por nivel.
Pipeline completo por nivel: mediana 1.34 s, max 99.54 s.
Tamanio JSON minificado por nivel: min 567 B, mediana 642 B, max 715 B; pack de 20 niveles: 13027 B -> out_inverse\levels_proto.json
