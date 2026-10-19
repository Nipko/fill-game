# Reto del río: informe del pool `daily_pool_v1`

Generado por `tools/level-generator --daily` (tarea R3 del encargo `docs/dev/traspaso/spark-reto-del-rio.md`; nota de diseño `docs/dev/reto-del-rio.md` §13). No se edita a mano: se regenera con el comando.

- Comando: `dotnet run --project tools/level-generator -- --daily --spec tools/level-generator/specs/daily_v1.json --bands 1,2,3 --days 0-9 --out tools/out/daily_sample --report tools/out/daily_sample_report.md`
- Especificación: `tools/level-generator/specs/daily_v1.json` (daily_v1); tintes y pares de riesgo de `campaign_v2.json`.
- Pack: `tools/out/daily_sample/daily_pool_v1.json`, 19967 bytes, SHA-256 `eb64897fc38d81603316fd2cdedb1af1cb18efea4d36d2f298f8dd1307a4dd69`.
- Tableros: 30; errores: 0.
- **Estado:** R-Q1b (a) y R-Q11 (a) decididas por el dueño el 2026-10-19: B2 solo con K = 8, B3 con max_pares_riesgo libre (03 §4.5) y la ventana del bot como compuerta (un día fuera de ventana no se publica; `level-qa --daily` lo da como fallo). Siguen [P] la fórmula del agua de R-Q2, `k_by_day` y las fuentes de cada banda.
- Tope de pares de riesgo por banda: B1 0, B2 1, B3 libre.
- Fuentes por banda (en orden; cada una con max_candidates candidatos): B1 fill E=2 -> deep -> walk -> deep (60000 nodos); B2 deep -> fill E=1; B3 deep.

## Resumen por banda

| Banda | Tableros | K (tableros) | Ventana del bot | bot_fail mín · mediana · media · máx | par mín · mediana · media · máx | par/K mediana | Candidatos (media por día) | En ventana | Fuera de ventana | Con respaldo | Pares > tope |
|---|---|---|---|---|---|---|---|---|---|---|---|
| B1 | 10 | 6: 5, 7: 5 | 0.40-0.60 | 0.400 · 0.463 · 0.473 · 0.575 | 18 · 19.0 · 20.0 · 24 | 3.07 | 110 (11.0) | 10 | 0 | 0 | 0 |
| B2 | 10 | 8: 10 | 0.60-0.80 | 0.625 · 0.700 · 0.708 · 0.800 | 16 · 17.5 · 17.9 · 21 | 2.19 | 45 (4.5) | 10 | 0 | 0 | 0 |
| B3 | 10 | 10: 5, 11: 5 | 0.75-1.00 | 0.775 · 0.950 · 0.930 · 1.000 | 21 · 22.5 · 22.7 · 25 | 2.18 | 13 (1.3) | 10 | 0 | 0 | 0 |

## Rechazos por banda y motivo

Candidatos descartados antes del elegido.

| Banda | Motivo | Candidatos |
|---|---|---|
| B1 | fuera de la ventana del bot | 100 |
| B2 | fuera de la ventana del bot | 35 |
| B3 | fuera de la ventana del bot | 2 |
| B3 | par < 2K | 1 |

## Días fuera de la ventana del bot (R-Q11 (a))

La ventana es compuerta: un día sin candidato en ella es un error y el pack no se escribe. Esta lista sale vacía en un pack escrito.

Ninguno.

## Tableros con más pares de riesgo que el tope de su banda (R-Q1b (a))

Un K que la paleta no admite con su tope es un error de la especificación antes de generar; esta lista sale vacía.

Ninguno.

## Tiempos

- Total (pared): 1.4 s con 20 hilos.
- B1: 7.8 s sumando el tiempo de pared de cada uno de sus 10 tableros (con varios hilos se solapan; 777 ms por tablero; el más lento, 1232 ms).
- B2: 10.6 s sumando el tiempo de pared de cada uno de sus 10 tableros (con varios hilos se solapan; 1062 ms por tablero; el más lento, 1392 ms).
- B3: 3.9 s sumando el tiempo de pared de cada uno de sus 10 tableros (con varios hilos se solapan; 387 ms por tablero; el más lento, 887 ms).

## Tableros

| id | K | Vacíos | Fuente | Candidato | bot_fail | par | Pares de riesgo | Contactos de riesgo | Mundo | Agua | Marcas |
|---|---|---|---|---|---|---|---|---|---|---|---|
| d1_n0000 | 6 | 2 | fill | 29 | 0.500 | 19 | 0 | 0 | 1 | white |  |
| d1_n0001 | 7 | 2 | fill | 1 | 0.550 | 22 | 0 | 0 | 2 | clear |  |
| d1_n0002 | 6 | 2 | fill | 7 | 0.425 | 18 | 0 | 0 | 3 | black |  |
| d1_n0003 | 7 | 2 | fill | 9 | 0.400 | 20 | 0 | 0 | 4 | white |  |
| d1_n0004 | 6 | 2 | fill | 21 | 0.500 | 18 | 0 | 0 | 5 | white |  |
| d1_n0005 | 7 | 2 | fill | 4 | 0.450 | 23 | 0 | 0 | 6 | white |  |
| d1_n0006 | 6 | 2 | fill | 19 | 0.425 | 18 | 0 | 0 | 7 | white |  |
| d1_n0007 | 7 | 2 | fill | 8 | 0.425 | 24 | 0 | 0 | 8 | white |  |
| d1_n0008 | 6 | 2 | fill | 11 | 0.575 | 19 | 0 | 0 | 9 | white |  |
| d1_n0009 | 7 | 2 | fill | 1 | 0.475 | 19 | 0 | 0 | 1 | white |  |
| d2_n0000 | 8 | 1 | deep | 6 | 0.700 | 17 | 1 | 0 | 1 | white |  |
| d2_n0001 | 8 | 1 | deep | 3 | 0.750 | 17 | 1 | 0 | 2 | clear |  |
| d2_n0002 | 8 | 1 | deep | 2 | 0.650 | 19 | 1 | 0 | 3 | black |  |
| d2_n0003 | 8 | 1 | deep | 9 | 0.650 | 16 | 1 | 0 | 4 | white |  |
| d2_n0004 | 8 | 1 | deep | 15 | 0.700 | 18 | 1 | 0 | 5 | white |  |
| d2_n0005 | 8 | 1 | deep | 2 | 0.800 | 16 | 1 | 0 | 6 | white |  |
| d2_n0006 | 8 | 1 | deep | 2 | 0.750 | 17 | 1 | 0 | 7 | white |  |
| d2_n0007 | 8 | 1 | deep | 1 | 0.750 | 20 | 1 | 0 | 8 | white |  |
| d2_n0008 | 8 | 1 | deep | 2 | 0.625 | 18 | 1 | 0 | 9 | white |  |
| d2_n0009 | 8 | 1 | deep | 3 | 0.700 | 21 | 1 | 0 | 1 | white |  |
| d3_n0000 | 10 | 1 | deep | 3 | 0.775 | 21 | 5 | 0 | 1 | white |  |
| d3_n0001 | 11 | 1 | deep | 2 | 0.975 | 24 | 4 | 0 | 2 | clear |  |
| d3_n0002 | 10 | 1 | deep | 1 | 1.000 | 22 | 5 | 0 | 3 | black |  |
| d3_n0003 | 11 | 1 | deep | 1 | 0.975 | 24 | 5 | 0 | 4 | white |  |
| d3_n0004 | 10 | 1 | deep | 1 | 0.950 | 22 | 4 | 0 | 5 | white |  |
| d3_n0005 | 11 | 1 | deep | 1 | 0.950 | 22 | 5 | 0 | 6 | white |  |
| d3_n0006 | 10 | 1 | deep | 1 | 0.800 | 21 | 4 | 0 | 7 | white |  |
| d3_n0007 | 11 | 1 | deep | 1 | 1.000 | 25 | 5 | 0 | 8 | white |  |
| d3_n0008 | 10 | 1 | deep | 1 | 0.925 | 23 | 4 | 0 | 9 | white |  |
| d3_n0009 | 11 | 1 | deep | 1 | 0.950 | 23 | 4 | 0 | 1 | white |  |
