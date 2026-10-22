# Reserva del 20 %: campaign_v1

Generado por `dotnet run --project tools/level-generator -c Release -- --reserve Assets/_Project/Data/Levels/mundo_02_v1.json Assets/_Project/Data/Levels/mundo_03_v1.json Assets/_Project/Data/Levels/mundo_04_v1.json Assets/_Project/Data/Levels/mundo_05_v1.json --spec tools/level-generator/specs/campaign_v1.json --out Assets/_Project/Data/Levels --report tools/out/reserve_campaign_v1.md`: no se edita a mano.
QA: `dotnet run --project tools/level-qa -c Release -- --reserve <reserva_NN_v1.json> --campaign <mundo_NN_v1.json> --spec tools/level-generator/specs/campaign_v1.json`.

Fuente: D-033, añadido «la reserva del 20 %» (Q-B11-7 (a)); 03 §17; D-020 (pregunta 31 b). Un pack por mundo (`kind` = `reserve`, `pack_schema` 4) con, para los niveles que usan una mecánica con kill switch (`mech_enabled.*`, 08 §9), otro tablero del mismo slot sin mecánicas y en su ventana, hasta el 20 % de los niveles del mundo. El cliente solo los usa desde `level_overrides`.

Reglas [P] de esta herramienta: orden por rondas sobre los kill switches de 08 §9 (cada mecánica toma su primer nivel aún sin elegir); tablero con la celda sin mecánicas (ni Aguas turbias) y con sus capacidades; entre los candidatos en la ventana con el par en el objetivo del rango ±30 %, el de la misma etiqueta que el publicado y el `bot_fail` más cercano; alivio desde el 31; sin `D` (no entra en el pool del rango).

## Resumen

| Mundo | Pack | Niveles del mundo | Con mecánica | Tope (20 %) | Tableros | Saltados | Bytes | SHA-256 |
|---|---|---|---|---|---|---|---|---|
| 2 (11-40) | — | 30 | 0 | 6 | 0 | 0 | 0 | — |
| 3 (41-80) | `reserva_03_v1.json` | 40 | 18 | 8 | 8 | 0 | 9765 | `edc2516c80ebd42ffeb05a3cc12b91b82aead47784ad54f3cb5765b65b4c76bb` |
| 4 (81-140) | `reserva_04_v1.json` | 60 | 33 | 12 | 12 | 0 | 15345 | `698256d4d8789f736a1914ce478f6744487543aa3662077be716753be5f1c316` |
| 5 (141-200) | `reserva_05_v1.json` | 60 | 36 | 12 | 12 | 0 | 15426 | `f5241da8dfc25b8d1bfdc6e345535ef82f44231d37e61709d57a70351de0468a` |

## Cobertura por mecánica

Niveles del mundo que usan cada kill switch y cuántos tienen tablero de reserva (el primero sin cubrir, si lo hay).

| Mundo | Mecánica | Niveles | Con reserva | Primer nivel sin reserva |
|---|---|---|---|---|
| 3 | `hoja` | 12 | 4 | 49 |
| 3 | `matapi` | 8 | 4 | 69 |
| 4 | `hoja` | 6 | 3 | 118 |
| 4 | `matapi` | 7 | 4 | 115 |
| 4 | `liana` | 12 | 4 | 85 |
| 4 | `remolino` | 10 | 2 | 105 |
| 4 | `barro` | 8 | 2 | 125 |
| 5 | `hoja` | 4 | 2 | 186 |
| 5 | `matapi` | 6 | 3 | 174 |
| 5 | `liana` | 2 | 2 | — |
| 5 | `remolino` | 3 | 2 | 191 |
| 5 | `barro` | 5 | 2 | 172 |
| 5 | `lluvia` | 10 | 4 | 144 |
| 5 | `brote` | 9 | 1 | 164 |
| 5 | `pirana` | 8 | 1 | 184 |

## Mundo 3: `reserva_03_v1.json`

| Slot | Sustituye | Mecánicas del publicado | Rol | Celda | Ventana | Publicado | Reserva | K | Cuias | Vacíos | Par | Alivio | Ronda |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 43 | `m03_v1_n0043` | hoja | estreno | *Ho | [0.00, 0.20) | normal 0.000 | normal 0.000 | 7 | 9 | 2 (2) | 14 | minus_color, plus_empty | 1 |
| 44 | `m03_v1_n0044` | hoja | estreno | *Ho | [0.00, 0.20) | normal 0.100 | normal 0.175 | 7 | 9 | 2 (2) | 18 | minus_color, plus_empty | 1 |
| 45 | `m03_v1_n0045` | hoja | estreno | *Ho | [0.00, 0.20) | normal 0.125 | normal 0.050 | 7 | 9 | 2 (2) | 18 | minus_color, plus_empty | 1 |
| 47 | `m03_v1_n0047` | hoja | comodidad | Ho | [0.20, 0.60] | normal 0.225 | normal 0.250 | 7 | 8 | 1 (1) | 16 | plus_empty, minus_color | 1 |
| 63 | `m03_v1_n0063` | matapi | estreno | *Ma | [0.00, 0.20) | normal 0.025 | normal 0.000 | 8 | 10 | 2 (2) | 16 | minus_color, plus_empty | 1 |
| 64 | `m03_v1_n0064` | matapi | estreno | *Ma | [0.00, 0.20) | normal 0.000 | normal 0.025 | 8 | 10 | 2 (2) | 16 | minus_color, plus_empty | 1 |
| 65 | `m03_v1_n0065` | matapi | estreno | *Ma | [0.00, 0.20) | normal 0.000 | normal 0.000 | 8 | 10 | 2 (2) | 16 | minus_color, plus_empty | 1 |
| 67 | `m03_v1_n0067` | matapi | comodidad | Ma | [0.20, 0.60] | normal 0.225 | normal 0.225 | 8 | 10 | 2 (2) | 22 | minus_color, plus_empty | 1 |

## Mundo 4: `reserva_04_v1.json`

| Slot | Sustituye | Mecánicas del publicado | Rol | Celda | Ventana | Publicado | Reserva | K | Cuias | Vacíos | Par | Alivio | Ronda |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 83 | `m04_v1_n0083` | liana | estreno | *Li | [0.00, 0.20) | normal 0.025 | normal 0.100 | 8 | 10 | 2 (2) | 16 | minus_color, plus_empty | 1 |
| 84 | `m04_v1_n0084` | liana | estreno | *Li | [0.00, 0.20) | normal 0.000 | normal 0.000 | 8 | 10 | 2 (2) | 18 | minus_color, plus_empty | 1 |
| 86 | `m04_v1_n0086` | hoja+matapi | reto | Ma+Ho (D) | [0.75, 0.90) | hard 0.875 | hard 0.875 | 9 | 10 | 1 (1) | 19 | plus_empty, minus_color | 1 |
| 92 | `m04_v1_n0092` | hoja+liana | reto | Li+Ho (D) | [0.75, 0.90) | hard 0.850 | hard 0.850 | 9 | 11 | 2 (2) | 27 | minus_color, plus_empty | 1 |
| 95 | `m04_v1_n0095` | matapi | reto | Ma+Al (D) | [0.75, 0.90) | hard 0.750 | hard 0.750 | 9 | 11 | 2 (2) | 28 | minus_color, plus_empty | 1 |
| 98 | `m04_v1_n0098` | matapi+liana | reto | Li+Ma (D) | [0.75, 0.90) | hard 0.850 | hard 0.825 | 9 | 11 | 2 (2) | 27 | minus_color, plus_empty | 1 |
| 103 | `m04_v1_n0103` | remolino | estreno | *Re | [0.00, 0.20) | normal 0.125 | normal 0.125 | 9 | 11 | 2 (2) | 18 | minus_color, plus_empty | 1 |
| 104 | `m04_v1_n0104` | remolino | estreno | *Re | [0.00, 0.20) | normal 0.175 | normal 0.125 | 9 | 11 | 2 (2) | 18 | minus_color, plus_empty | 1 |
| 106 | `m04_v1_n0106` | hoja | reto | Ho+Tu (D) | [0.75, 0.90) | hard 0.775 | hard 0.775 | 9 | 11 | 2 (2) | 30 | minus_color, plus_empty | 1 |
| 111 | `m04_v1_n0111` | matapi | comodidad | Ma | [0.20, 0.60] | normal 0.500 | normal 0.475 | 9 | 11 | 2 (2) | 19 | minus_color, plus_empty | 1 |
| 123 | `m04_v1_n0123` | barro | estreno | *Ba | [0.00, 0.20) | normal 0.000 | normal 0.050 | 9 | 11 | 2 (2) | 18 | minus_color, plus_empty | 1 |
| 124 | `m04_v1_n0124` | barro | estreno | *Ba | [0.00, 0.20) | normal 0.000 | normal 0.000 | 9 | 11 | 2 (2) | 18 | minus_color, plus_empty | 1 |

## Mundo 5: `reserva_05_v1.json`

| Slot | Sustituye | Mecánicas del publicado | Rol | Celda | Ventana | Publicado | Reserva | K | Cuias | Vacíos | Par | Alivio | Ronda |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 143 | `m05_v1_n0143` | lluvia | estreno | *Ll | [0.00, 0.20) | normal 0.075 | normal 0.075 | 9 | 11 | 2 (2) | 21 | minus_color, plus_empty | 1 |
| 146 | `m05_v1_n0146` | remolino+barro | reto | Ba+Re (D) | [0.75, 0.90) | hard 0.750 | hard 0.825 | 9 | 11 | 2 (2) | 30 | minus_color, plus_empty | 1 |
| 151 | `m05_v1_n0151` | liana | comodidad | Li | [0.20, 0.60] | normal 0.450 | normal 0.350 | 9 | 11 | 2 (2) | 18 | minus_color, plus_empty | 1 |
| 152 | `m05_v1_n0152` | hoja+lluvia | reto | Ll+Ho (D) | [0.75, 0.90) | hard 0.875 | hard 0.875 | 9 | 11 | 2 (2) | 30 | minus_color, plus_empty | 1 |
| 154 | `m05_v1_n0154` | matapi | comodidad | Ma | [0.20, 0.60] | normal 0.325 | normal 0.325 | 9 | 11 | 2 (2) | 20 | minus_color, plus_empty | 1 |
| 155 | `m05_v1_n0155` | matapi | reto | Ma+Tu (D) | [0.75, 0.90) | hard 0.850 | hard 0.825 | 9 | 11 | 2 (2) | 29 | minus_color, plus_empty | 1 |
| 158 | `m05_v1_n0158` | barro+lluvia | reto | Ll+Ba (D) | [0.75, 0.90) | hard 0.825 | hard 0.825 | 9 | 11 | 2 (2) | 26 | minus_color, plus_empty | 1 |
| 163 | `m05_v1_n0163` | brote | estreno | *Br | [0.00, 0.20) | normal 0.000 | normal 0.000 | 9 | 11 | 2 (2) | 20 | minus_color, plus_empty | 1 |
| 166 | `m05_v1_n0166` | remolino+lluvia | reto | Ll+Re (D) | [0.75, 0.90) | normal 0.675 | hard 0.750 | 9 | 11 | 2 (2) | 27 | minus_color, plus_empty | 1 |
| 171 | `m05_v1_n0171` | hoja | comodidad | Ho | [0.20, 0.60] | normal 0.200 | normal 0.225 | 9 | 11 | 2 (2) | 18 | minus_color, plus_empty | 1 |
| 180 | `m05_v1_n0180` | matapi+liana | hito | Ma+Li (MD) | [0.90, 1.00] | very_hard 1.000 | very_hard 1.000 | 9 | 10 | 1 (1) | 22 | plus_empty, minus_color | 1 |
| 183 | `m05_v1_n0183` | pirana | estreno | *Pi | [0.00, 0.20) | normal 0.000 | normal 0.000 | 9 | 11 | 2 (2) | 18 | minus_color, plus_empty | 1 |
