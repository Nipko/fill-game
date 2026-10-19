# Reto del río: informe del pool `daily_pool_v1`

Generado por `tools/level-generator --daily` (tarea R3 del encargo `docs/dev/traspaso/spark-reto-del-rio.md`; nota de diseño `docs/dev/reto-del-rio.md` §13). No se edita a mano: se regenera con el comando.

- Comando: `dotnet run --project tools/level-generator -- --daily --spec tools/level-generator/specs/daily_v1.json --bands 1,2,3 --days 0-9 --out tools/out/daily_sample --report tools/out/daily_sample_report.md`
- Especificación: `tools/level-generator/specs/daily_v1.json` (daily_v1); tintes y pares de riesgo de `campaign_v2.json`.
- Pack: `tools/out/daily_sample/daily_pool_v1.json`, 20144 bytes, SHA-256 `61cf6bef29673efb5ccc9affa8f98c5cc19aa86933aca82fa7e14a4a117a3ac1`.
- Tableros: 30; errores: 0.
- **Estado [P]:** la especificación está pendiente de R-Q1b (paleta de B2 con K = 9 y de B3, nota §1.1), de la fórmula del agua de R-Q2 y de R-Q11 (días fuera de ventana, nota §1.6). Este pool no se publica (R5) hasta que el dueño responda.

## Resumen por banda

| Banda | Tableros | K (tableros) | Ventana del bot | bot_fail mín · mediana · media · máx | par mín · mediana · media · máx | par/K mediana | Candidatos (media por día) | En ventana | Fuera de ventana | Con respaldo | Pares > tope |
|---|---|---|---|---|---|---|---|---|---|---|---|
| B1 | 10 | 6: 5, 7: 5 | 0.40-0.60 | 0.400 · 0.438 · 0.470 · 0.600 | 13 · 16.5 · 16.5 · 21 | 2.45 | 179 (17.9) | 10 | 0 | 2 | 0 |
| B2 | 10 | 8: 5, 9: 5 | 0.60-0.80 | 0.625 · 0.725 · 0.740 · 0.900 | 19 · 20.5 · 20.9 · 25 | 2.35 | 132 (13.2) | 8 | 2 | 0 | 5 |
| B3 | 10 | 10: 5, 11: 5 | 0.75-1.00 | 0.750 · 0.950 · 0.938 · 1.000 | 21 · 22.5 · 22.8 · 25 | 2.19 | 11 (1.1) | 10 | 0 | 0 | 10 |

## Rechazos por banda y motivo

Candidatos descartados antes del elegido (los «fuera de la ventana del bot» incluyen, en los días fuera de ventana, el que acabó elegido como el más cercano).

| Banda | Motivo | Candidatos |
|---|---|---|
| B1 | fuera de la ventana del bot | 169 |
| B2 | fuera de la ventana del bot | 124 |
| B3 | par < 2K | 1 |

## Días fuera de la ventana del bot (R-Q11) [P]

Ningún candidato entró en la ventana; se guardó el más cercano que A* resuelve con par >= 2K. `level-qa --daily` (R4) lo da como fallo.

| id | K | Fuente | bot_fail | Ventana | Candidatos |
|---|---|---|---|---|---|
| d2_n0002 | 8 | fill | 0.850 | 0.60-0.80 | 30 |
| d2_n0006 | 8 | fill | 0.900 | 0.60-0.80 | 30 |

## Tableros con más pares de riesgo que el tope de su banda (R-Q1b) [P]

La paleta de R-Q1 no admite ese K con el tope (nota §1.1): el tablero lleva el mínimo de pares posible con ese K; no cambian ni K ni la paleta.

| Banda | K | Tope de la banda | Mínimo posible | Tableros | Pares por tablero |
|---|---|---|---|---|---|
| B2 | 9 | 1 | 3 | 5 | 3: 5 |
| B3 | 10 | 2 | 3 | 5 | 3: 5 |
| B3 | 11 | 2 | 4 | 5 | 4: 5 |

## Tiempos

- Total (pared): 1.6 s con 20 hilos.
- B1: 1.8 s sumando el tiempo de pared de cada uno de sus 10 tableros (con varios hilos se solapan; 182 ms por tablero; el más lento, 519 ms).
- B2: 7.5 s sumando el tiempo de pared de cada uno de sus 10 tableros (con varios hilos se solapan; 752 ms por tablero; el más lento, 1554 ms).
- B3: 2.4 s sumando el tiempo de pared de cada uno de sus 10 tableros (con varios hilos se solapan; 244 ms por tablero; el más lento, 368 ms).

## Tableros

| id | K | Vacíos | Fuente | Candidato | bot_fail | par | Pares de riesgo | Contactos de riesgo | Mundo | Agua | Marcas |
|---|---|---|---|---|---|---|---|---|---|---|---|
| d1_n0000 | 6 | 2 | walk | 14 | 0.400 | 14 | 0 | 0 | 1 | white |  |
| d1_n0001 | 7 | 2 | walk | 6 | 0.400 | 21 | 0 | 0 | 2 | clear |  |
| d1_n0002 | 6 | 2 | walk | 19 | 0.425 | 13 | 0 | 0 | 3 | black |  |
| d1_n0003 | 7 | 2 | walk | 1 | 0.450 | 15 | 0 | 0 | 4 | white |  |
| d1_n0004 | 6 | 2 | fill | 44 | 0.525 | 18 | 0 | 0 | 5 | white | respaldo |
| d1_n0005 | 7 | 2 | walk | 11 | 0.400 | 18 | 0 | 0 | 6 | white |  |
| d1_n0006 | 6 | 2 | walk | 26 | 0.425 | 13 | 0 | 0 | 7 | white |  |
| d1_n0007 | 7 | 2 | walk | 7 | 0.575 | 14 | 0 | 0 | 8 | white |  |
| d1_n0008 | 6 | 2 | fill | 38 | 0.500 | 18 | 0 | 0 | 9 | white | respaldo |
| d1_n0009 | 7 | 2 | walk | 13 | 0.600 | 21 | 0 | 0 | 1 | white |  |
| d2_n0000 | 8 | 1 | fill | 14 | 0.675 | 21 | 1 | 0 | 1 | white |  |
| d2_n0001 | 9 | 1 | deep | 11 | 0.625 | 19 | 3 | 0 | 2 | clear | pares > tope |
| d2_n0002 | 8 | 1 | fill | 1 | 0.850 | 25 | 1 | 0 | 3 | black | fuera de ventana |
| d2_n0003 | 9 | 1 | deep | 1 | 0.700 | 20 | 3 | 0 | 4 | white | pares > tope |
| d2_n0004 | 8 | 1 | fill | 15 | 0.800 | 19 | 1 | 0 | 5 | white |  |
| d2_n0005 | 9 | 1 | deep | 7 | 0.800 | 21 | 3 | 0 | 6 | white | pares > tope |
| d2_n0006 | 8 | 1 | fill | 21 | 0.900 | 23 | 1 | 0 | 7 | white | fuera de ventana |
| d2_n0007 | 9 | 1 | deep | 5 | 0.675 | 20 | 3 | 0 | 8 | white | pares > tope |
| d2_n0008 | 8 | 1 | fill | 12 | 0.750 | 22 | 1 | 0 | 9 | white |  |
| d2_n0009 | 9 | 1 | deep | 7 | 0.625 | 19 | 3 | 0 | 1 | white | pares > tope |
| d3_n0000 | 10 | 1 | deep | 1 | 0.750 | 22 | 3 | 0 | 1 | white | pares > tope |
| d3_n0001 | 11 | 1 | deep | 2 | 0.975 | 24 | 4 | 0 | 2 | clear | pares > tope |
| d3_n0002 | 10 | 1 | deep | 1 | 1.000 | 22 | 3 | 0 | 3 | black | pares > tope |
| d3_n0003 | 11 | 1 | deep | 1 | 0.975 | 24 | 4 | 0 | 4 | white | pares > tope |
| d3_n0004 | 10 | 1 | deep | 1 | 0.950 | 22 | 3 | 0 | 5 | white | pares > tope |
| d3_n0005 | 11 | 1 | deep | 1 | 0.950 | 22 | 4 | 0 | 6 | white | pares > tope |
| d3_n0006 | 10 | 1 | deep | 1 | 0.900 | 21 | 3 | 0 | 7 | white | pares > tope |
| d3_n0007 | 11 | 1 | deep | 1 | 1.000 | 25 | 4 | 0 | 8 | white | pares > tope |
| d3_n0008 | 10 | 1 | deep | 1 | 0.925 | 23 | 3 | 0 | 9 | white | pares > tope |
| d3_n0009 | 11 | 1 | deep | 1 | 0.950 | 23 | 4 | 0 | 1 | white | pares > tope |
