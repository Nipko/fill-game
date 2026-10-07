Lote: 20 niveles, 20 aceptados, tiempo total 440.6 s (Python 3.14.3, presupuesto A* 300000, bot x40).

| # | Fuente | K | Vacios | Cuias | Intentos gen | Par | Estados A* | t solve (ms) | Fallo bot | Ramif. | Trampas | Compl. | D | Banda | Etiqueta | Alivio +1 vacio par | Alivio -1 color (par) | t total (s) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | walk | 5 | 2 | 7 | 5 | 11 | 347 | 14 | 0.00 | 4.45 | 0.00 | 28 | -0.83 | recovery | normal | 11 | 2 (9) | 0.26 |
| 2 | walk | 6 | 2 | 8 | 3 | 15 | 1403 | 76 | 0.47 | 5.20 | 0.00 | 38 | -0.15 | comfort | normal | 15 | 4 (14) | 1.91 |
| 3 | walk | 6 | 1 | 7 | 6 | 12 | 49 | 2 | 0.97 | 2.50 | 0.10 | 34 | -0.03 | comfort | very_hard | 12 | 5 (11) | 0.08 |
| 4 | walk | 7 | 2 | 9 | 2 | 19 | 2865 | 179 | 0.10 | 3.32 | 0.02 | 48 | -0.60 | recovery | normal | 19 | 6 (17) | 6.37 |
| 5 | walk | 8 | 2 | 10 | 3 | 16 | 2574 | 158 | 0.12 | 6.12 | 0.00 | 42 | -0.57 | comfort | normal | 16 | 5 (15) | 5.38 |
| 6 | walk | 9 | 2 | 11 | 1 | 18 | 2865 | 231 | 0.38 | 7.22 | 0.00 | 50 | -0.20 | comfort | normal | 18 | 6 (17) | 10.51 |
| 7 | walk | 10 | 2 | 12 | 2 | 21 | 6764 | 524 | 0.25 | 7.43 | 0.00 | 56 | -0.22 | comfort | normal | 21 | 4 (19) | 21.29 |
| 8 | walk | 11 | 1 | 12 | 3 | 22 | 204 | 13 | 0.95 | 2.86 | 0.19 | 58 | +0.07 | comfort | very_hard | 21 | 1 (20) | 4.21 |
| 9 | deep | 7 | 1 | 8 | 1 | 18 | 122 | 6 | 0.95 | 2.17 | 0.13 | 48 | +0.20 | comfort | very_hard | 18 | 6 (17) | 1.21 |
| 10 | deep | 8 | 1 | 9 | 1 | 19 | 173 | 9 | 0.88 | 2.11 | 0.15 | 52 | +0.07 | comfort | hard | 19 | 6 (18) | 1.40 |
| 11 | deep | 9 | 1 | 10 | 1 | 19 | 230 | 13 | 0.88 | 2.47 | 0.13 | 54 | -0.03 | comfort | hard | 19 | 1 (18) | 2.14 |
| 12 | deep | 10 | 1 | 11 | 1 | 23 | 221 | 13 | 1.00 | 3.17 | 0.26 | 62 | +0.42 | challenge | very_hard | 23 | 6 (22) | 3.34 |
| 13 | deep | 11 | 1 | 12 | 1 | 27 | 174 | 13 | 1.00 | 2.70 | 0.40 | 72 | +0.61 | challenge | very_hard | 27 | 5 (26) | 8.64 |
| 14 | deep | 10 | 1 | 11 | 1 | 22 | 382 | 22 | 0.97 | 3.32 | 0.32 | 60 | +0.39 | comfort | very_hard | 22 | 4 (21) | 3.35 |
| 15 | fill | 6 | 2 | 8 | 1 | 19 | 1868 | 92 | 0.07 | 3.05 | 0.00 | 44 | -0.53 | comfort | normal | 19 | 3 (15) | 3.52 |
| 16 | fill | 7 | 1 | 8 | 53 | 19 | 139 | 6 | 1.00 | 2.53 | 0.27 | 48 | +0.48 | challenge | very_hard | 19 | 5 (17) | 0.87 |
| 17 | fill | 8 | 2 | 10 | 1 | 26 | 6241 | 396 | 0.85 | 2.85 | 0.03 | 60 | +0.40 | challenge | hard | None | 6 (23) | 48.54 |
| 18 | fill | 9 | 1 | 10 | 26 | 25 | 144 | 7 | 0.95 | 1.60 | 0.03 | 64 | +0.24 | comfort | very_hard | 25 | 4 (23) | 3.60 |
| 19 | fill | 10 | 2 | 12 | 1 | 32 | 13141 | 1056 | 0.97 | 3.66 | 0.05 | 76 | +0.77 | milestone | very_hard | None | - | 157.39 |
| 20 | fill | 10 | 2 | 12 | 1 | 32 | 5167 | 428 | 0.80 | 2.88 | 0.05 | 78 | +0.51 | challenge | hard | None | 4 (28) | 156.54 |

| Fuente | n | Fallo bot medio | Fallo bot min-max | Par medio | Par/2K | t gen mediana (s) | t total mediana (s) |
|---|---|---|---|---|---|---|---|
| walk | 8 | 0.41 | 0.00-0.97 | 16.8 | 1.09 | 0.01 | 4.80 |
| deep | 6 | 0.95 | 0.88-1.00 | 21.3 | 1.17 | 0.47 | 2.74 |
| fill | 6 | 0.78 | 0.07-1.00 | 25.5 | 1.53 | 0.01 | 26.07 |

Solver A*: mediana 18 ms, media 163 ms, max 1056 ms; estados expandidos mediana 364, max 13141.
Bot casual x40: mediana 63 ms por nivel.
Trampas (hasta 120 pares, A* 3k): mediana 0.22 s por nivel.
Variantes de alivio: mediana 2.52 s por nivel.
Pipeline completo por nivel: mediana 3.56 s, max 157.39 s.
Tamanio JSON minificado por nivel: min 567 B, mediana 654 B, max 715 B; pack de 20 niveles: 13132 B -> C:\Users\Nipko-lab\Desktop\projects\amazon-fill\tools\out\levels_proto.json
