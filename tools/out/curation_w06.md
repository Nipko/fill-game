# Curación de 201-300 (variante a): retos, hitos y estrenos para el dueño

Generado por `dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/campaign_v2.json --levels 201-300 --variant a --out Assets/_Project/Data/Levels --curation tools/out/curation_w06.md --decisions tools/level-generator/specs/curation_v2.json` con los mismos niveles colocados que el pack: no se edita a mano y no lleva tiempos (la decisión del dueño sale de `tools/level-generator/specs/curation_v2.json`). Niveles del mundo 6 (Oriente, 201-300).

Fuentes: 09 §0.1 (fila de level-pipeline-dev: el dueño aprueba la curación de retos e hitos de cada paquete) y §0.3 (propuesta de curación de los 30 retos e hitos que aprueba el dueño), 03 §7 (un reto por debajo de 0,75 se publica sin etiqueta y la curación decide si se sustituye por otro del pool del slot; banda por el cuantil q de D en el pool del rango), D-009 (etiqueta por el bot ×40 con la semilla del nivel: Difícil ≥ 0,75 y Muy difícil ≥ 0,90; retos en p6, p9, p12, p15 y p18 e hito en p20; c.2: un reto o el hito fuera de su ventana se publica con la etiqueta que mide), 03 §11-§12, 02 §9.2 regla 2 (estrenos) y la tabla de novedades de D2 (d.5.3; D-023 punto 10). La columna «Propuesta» es [P]: la decide el dueño.

Compuerta automática (verde antes de pedir la curación): `dotnet run --project tools/level-qa -c Release -- Assets/_Project/Data/Levels/mundo_06_v1.json --spec tools/level-generator/specs/campaign_v2.json --levels 201-300`. Sus avisos QA-WINDOW (fuera de la ventana del slot) y QA-BAND-Q (fuera de la banda del rol) son los niveles con «revisar» o con nota de banda de este informe.

## Resumen

| Pack | Niveles | `pack_schema` | Bytes | SHA-256 |
|---|---|---|---|---|
| `Assets/_Project/Data/Levels/mundo_06_v1.json` | 100 | 3 | 139470 | `9903e8363553e65c9f77a067ed7221146a8250e6dedbccb6cfdae96db031689f` |

- Retos e hitos: 30 (25 retos y 5 hitos); 27 miden la etiqueta objetivo o más (D-009) y 3 no.
- Etiquetas publicadas en 201-300: 23 Difícil y 4 Muy difícil de 100 niveles (03 §11: 25 % y 5 %).
- Para revisar: 246 (reto), 266 (reto), 280 (hito), 298 (reto). El resto, aceptar.
- Estrenos: ★Ln en 243, 244, 245; ★Ha en 283, 284, 285 (6 de 6 con un ejemplar, 2 vacíos y bot_fail < 0,20).

## Retos e hitos

Etiqueta = la que mide el bot ×40 y se publica (D-009); Objetivo = la de la celda (D) o (MD). q = cuantil de D en el pool del rango (banda: recuperación ≤ 0,10, comodidad ≤ 0,70, reto ≤ 0,95, hito > 0,95). «Otros del slot» = candidatos medidos del mismo slot, sin rechazo, que miden la etiqueta objetivo o más: con ellos se puede sustituir el nivel al regenerar (03 §7), si la escalera del ciclo lo admite.

| Nivel | p | Rol | Calendario | K | Cuias | Vacíos (libres) | Par | bot_fail | Ventana | Etiqueta | Objetivo | D | q (banda) | Otros del slot | Propuesta [P] |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 206 | 6 | reto | Pi+Ba (D) | 10 | 12 | 0 (0) | 25 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.43 | 0.784 (reto) | 11 | aceptar |
| 209 | 9 | reto | Li+Re (D) | 10 | 11 | 1 (1) | 23 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.59 | 0.854 (reto) | 5 | aceptar |
| 212 | 12 | reto | Pi (D) | 10 | 12 | 1 (1) | 24 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.78 | 0.895 (reto) | 6 | aceptar |
| 215 | 15 | reto | Re+Ba (D) | 10 | 12 | 1 (1) | 25 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.85 | 0.911 (reto) | 2 | aceptar |
| 218 | 18 | reto | Li+Ba (D) | 10 | 12 | 1 (1) | 30 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 1.08 | 0.944 (reto) | 7 | aceptar |
| 220 | 20 | hito | Ba+Tu+Ch (MD) | 10 | 12 | 1 (1) | 28 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.20 | 0.967 (hito) | 3 | aceptar (turbia: 2 dosis ocultas en 1 cuia(s), 1 realidad(es) compatibles con lo visible) |
| 226 | 6 | reto | Ll+Ho (D) | 10 | 12 | 2 (0) | 23 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.35 | 0.759 (reto) | 11 | aceptar |
| 229 | 9 | reto | Br+Ll (D) | 10 | 12 | 1 (0) | 23 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.37 | 0.765 (reto) | 3 | aceptar |
| 232 | 12 | reto | Ma+Ho (D) | 10 | 12 | 2 (0) | 25 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.42 | 0.779 (reto) | 8 | aceptar |
| 235 | 15 | reto | Br+Tu (D) | 10 | 11 | 0 (0) | 23 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.49 | 0.816 (reto) | 11 | aceptar (turbia: 2 dosis ocultas en 1 cuia(s), 2 realidad(es) compatibles con lo visible) |
| 238 | 18 | reto | Ll+Ho (D) | 10 | 12 | 2 (0) | 25 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.49 | 0.811 (reto) | 10 | aceptar |
| 240 | 20 | hito | Ho+Br (MD) | 10 | 13 | 2 (1) | 25 | 0.900 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.01 | 0.936 (reto) | 56 | aceptar (D en banda reto (q 0.936), con el mínimo del hito cumplido: par − 2K = 5 ≥ 2.4 (aviso QA-BAND-Q)) |
| 246 | 6 | reto | Ma+Re (D) | 10 | 12 | 2 (0) | 21 | 0.600 | [0.75, 0.90) | Normal | Difícil | 0.24 | 0.716 (reto) | 0 | **revisar**: mide Normal (bot_fail 0.600) y apunta a Difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir; ningún otro candidato del slot mide la etiqueta objetivo: sustituirlo pide más candidatos (otra ronda del generador) o cambiar la celda |
| 249 | 9 | reto | Ln (D) | 10 | 11 | 1 (1) | 22 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.36 | 0.760 (reto) | 5 | aceptar |
| 252 | 12 | reto | Ln+Re (D) | 10 | 11 | 1 (1) | 20 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.52 | 0.831 (reto) | 2 | aceptar |
| 255 | 15 | reto | Ma+Tu (D) | 10 | 12 | 1 (0) | 25 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.61 | 0.865 (reto) | 11 | aceptar (turbia: 3 dosis ocultas en 2 cuia(s), 2 realidad(es) compatibles con lo visible) |
| 258 | 18 | reto | Ln+Ma (D) | 10 | 12 | 2 (1) | 24 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.65 | 0.875 (reto) | 4 | aceptar |
| 260 | 20 | hito | Ln+Ba (MD) | 10 | 11 | 0 (0) | 30 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.22 | 0.972 (hito) | 11 | aceptar |
| 266 | 6 | reto | Pi+Br (D) | 10 | 13 | 1 (1) | 23 | 0.425 | [0.75, 0.90) | Normal | Difícil | 0.36 | 0.760 (reto) | 0 | **revisar**: mide Normal (bot_fail 0.425) y apunta a Difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir; ningún otro candidato del slot mide la etiqueta objetivo: sustituirlo pide más candidatos (otra ronda del generador) o cambiar la celda |
| 269 | 9 | reto | Ln+Br (D) | 10 | 12 | 1 (1) | 24 | 0.800 | [0.75, 0.90) | Difícil | Difícil | 0.39 | 0.768 (reto) | 13 | aceptar |
| 272 | 12 | reto | Pi+Ba (D) | 10 | 12 | 0 (0) | 23 | 0.850 | [0.75, 0.90) | Difícil | Difícil | 0.45 | 0.801 (reto) | 17 | aceptar |
| 275 | 15 | reto | Ba+Tu (D) | 10 | 12 | 1 (1) | 25 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.51 | 0.826 (reto) | 2 | aceptar (turbia: 3 dosis ocultas en 2 cuia(s), 2 realidad(es) compatibles con lo visible) |
| 278 | 18 | reto | Ln+Ba (D) | 10 | 11 | 0 (0) | 22 | 0.875 | [0.75, 0.90) | Difícil | Difícil | 0.65 | 0.872 (reto) | 16 | aceptar |
| 280 | 20 | hito | Pi+Br (MD) | 10 | 13 | 1 (1) | 25 | 0.675 | [0.90, 1.00] | Normal | Muy difícil | 0.95 | 0.928 (reto) | 0 | **revisar**: mide Normal (bot_fail 0.675) y apunta a Muy difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir; ningún otro candidato del slot mide la etiqueta objetivo: sustituirlo pide más candidatos (otra ronda del generador) o cambiar la celda |
| 286 | 6 | reto | Li+Re (D) | 10 | 11 | 1 (1) | 20 | 0.775 | [0.75, 0.90) | Difícil | Difícil | 0.05 | 0.583 (comodidad) | 7 | aceptar (Difícil por el bot, con D en banda comodidad (q 0.583)) |
| 289 | 9 | reto | Ha (D) | 10 | 11 | 1 (1) | 23 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 0.15 | 0.653 (comodidad) | 9 | aceptar (Difícil por el bot, con D en banda comodidad (q 0.653)) |
| 292 | 12 | reto | Ha+Re (D) | 10 | 11 | 1 (0) | 20 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 0.25 | 0.719 (reto) | 1 | aceptar |
| 295 | 15 | reto | Br+Tu (D) | 10 | 12 | 1 (1) | 22 | 0.750 | [0.75, 0.90) | Difícil | Difícil | 0.29 | 0.730 (reto) | 17 | aceptar (turbia: 2 dosis ocultas en 1 cuia(s), 2 realidad(es) compatibles con lo visible) |
| 298 | 18 | reto | Ha+Br (D) | 10 | 12 | 1 (1) | 23 | 0.825 | [0.75, 0.90) | Difícil | Difícil | 0.34 | 0.754 (reto) | 13 | **revisar**: risk_adjacent: 1 contacto(s) de tintes de un par de riesgo en una cuia (03 §4.5) |
| 300 | 20 | hito | Ha+Li (MD) «Cuyabeno» [P] | 10 | 12 | 2 (2) | 34 | 1.000 | [0.90, 1.00] | Muy difícil | Muy difícil | 1.45 | 0.986 (hito) | 17 | aceptar |

## Hitos

p20 de cada ciclo («Pico» de c.3): Muy difícil por el bot (≥ 0,90) y, por D, en banda de hito o, como mínimo, con par − 2K ≥ la media del pool del rango (03 §7; QA-BAND-Q). Los nombres de hito de la celda son [P] y pasan por el asesor (D-023 punto 21): no van al pack.

### 220 · Ba+Tu+Ch (MD)

- Tablero: K 10, 12 cuias (11 de 4, 1 de 2), 1 vacío(s) (1 libre(s)); fuente `deep`.
- Mecánicas: Barro: 1 dosis; Aguas turbias: 2 dosis ocultas en 1 cuia(s), 1 realidad(es) compatibles con lo visible.
- Medida: par 28 (2K = 20; par − 2K = 8, media del pool 2.4), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 1.20, q 0.967 (banda hito), ramificación 3.29, trampas tempranas 0.
- Notas de la generación: 1 ronda(s) extra de candidatos.
- Propuesta [P]: aceptar (turbia: 2 dosis ocultas en 1 cuia(s), 1 realidad(es) compatibles con lo visible).

### 240 · Ho+Br (MD)

- Tablero: K 10, 13 cuias (13 de 4), 2 vacío(s) (1 libre(s)); fuente `deep`.
- Mecánicas: Ho key=14 (cuia de 4); Br (cuia de 4).
- Medida: par 25 (2K = 20; par − 2K = 5, media del pool 2.4), bot_fail 0.900 → Muy difícil (objetivo Muy difícil), D 1.01, q 0.936 (banda reto), ramificación 8.04, trampas tempranas 0.
- Notas de la generación: relajacion: deep con 60.000 nodos; 1 ronda(s) extra de candidatos; 6 ronda(s) de banda.
- Propuesta [P]: aceptar (D en banda reto (q 0.936), con el mínimo del hito cumplido: par − 2K = 5 ≥ 2.4 (aviso QA-BAND-Q)).

### 260 · Ln+Ba (MD)

- Tablero: K 10, 11 cuias (11 de 4), 0 vacío(s) (0 libre(s)); fuente `fill`.
- Mecánicas: liana n=1 (4 dosis); Barro: 1 dosis.
- Medida: par 30 (2K = 20; par − 2K = 10, media del pool 2.4), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 1.22, q 0.972 (banda hito), ramificación 2.27, trampas tempranas 0.
- Notas de la generación: 1 ronda(s) extra de candidatos.
- Propuesta [P]: aceptar.

### 280 · Pi+Br (MD)

- Tablero: K 10, 13 cuias (12 de 4, 1 de 2), 1 vacío(s) (1 libre(s)); fuente `deep`.
- Mecánicas: Pi (cuia de 2); Br (cuia de 4).
- Medida: par 25 (2K = 20; par − 2K = 5, media del pool 2.4), bot_fail 0.675 → Normal (objetivo Muy difícil), D 0.95, q 0.928 (banda reto), ramificación 10.36, trampas tempranas 0.
- Notas de la generación: 2 ronda(s) extra de candidatos; 6 ronda(s) de banda.
- Propuesta [P]: **revisar**: mide Normal (bot_fail 0.675) y apunta a Muy difícil: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir; ningún otro candidato del slot mide la etiqueta objetivo: sustituirlo pide más candidatos (otra ronda del generador) o cambiar la celda.

### 300 · Ha+Li (MD) «Cuyabeno» [P]

- Tablero: K 10, 12 cuias (12 de 4), 2 vacío(s) (2 libre(s)); fuente `fill`.
- Mecánicas: hoja_atada key=11 (4 dosis); Li key=8 (cuia de 4).
- Medida: par 34 (2K = 20; par − 2K = 14, media del pool 2.4), bot_fail 1.000 → Muy difícil (objetivo Muy difícil), D 1.45, q 0.986 (banda hito), ramificación 2.97, trampas tempranas 0.
- Notas de la generación: relajacion: fill con 2 vacios (03 sec. 4.4); 2 ronda(s) extra de candidatos.
- Propuesta [P]: aceptar.

## Tableros de estreno

02 §9.2 regla 2: el estreno va en 2-3 tableros fáciles (p3-p5) con la mecánica sola, un ejemplar, 2 vacíos y `bot_fail` < 0,20; el primero lleva `intro_mechanic` (`skippable` = false).

| Nivel | p | Calendario | `intro_mechanic` | Mecánica | Ejemplares | Vacíos | Par | bot_fail | D | q (banda) | Regla 2 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 243 | 3 | ★Ln | `liana_n` | liana n=1 (4 dosis) | 1 | 2 | 20 | 0.100 | -0.58 | 0.112 (comodidad) | cumple |
| 244 | 4 | ★Ln | — | liana n=1 (4 dosis) | 1 | 2 | 24 | 0.075 | -0.36 | 0.233 (comodidad) | cumple |
| 245 | 5 | ★Ln | — | liana n=1 (4 dosis) | 1 | 2 | 21 | 0.125 | -0.64 | 0.090 (recuperación) | cumple |
| 283 | 3 | ★Ha | `hoja_atada` | hoja_atada key=4 (4 dosis) | 1 | 2 | 21 | 0.100 | -0.63 | 0.095 (recuperación) | cumple |
| 284 | 4 | ★Ha | — | hoja_atada key=13 (4 dosis) | 1 | 2 | 20 | 0.175 | -0.51 | 0.140 (comodidad) | cumple |
| 285 | 5 | ★Ha | — | hoja_atada key=6 (4 dosis) | 1 | 2 | 21 | 0.075 | -0.57 | 0.117 (comodidad) | cumple |

## Variantes de la 1.0

Variantes de 02 §9.1 (propuesta de D-007 b.3; firma de `pack_schema` 3, D-023 punto 5) con los códigos de d.5.1 [P]: Ln = Liana de cuenta (`liana` con `n`; n = 1 en el estreno y 1-2 en los demás [P]), Ha = Hoja atada (`hoja_atada`, sobre una cuia con su fruta en el fondo y cap = U_c, D-024), B2 = Brote de dos hojas (`brote` con `n` = 2, 1-2 dosis) y Fl = Flor de vitória-régia (`flor` con `key` y `key2` distintas). Estreno (02 §9.2 regla 2): un ejemplar, 2 vacíos y `bot_fail` < 0,20.

| Variante | Debut | Niveles | Ejemplares | Tableros de estreno | Estrenos con un ejemplar, 2 vacíos y bot < 0,20 |
|---|---|---|---|---|---|
| Ln | 243 (`liana_n`) | 10 | 10 | 3 | 3 de 3 |
| Ha | 283 (`hoja_atada`) | 8 | 8 | 3 | 3 de 3 |
| B2 | 343 (`brote_n2`) | 0 | 0 | 0 | 0 de 0 |
| Fl | 403 (`flor`) | 0 | 0 | 0 | 0 de 0 |

| Nivel | p | Rol | Celda | Variante | Ejemplares de mecánica | Vacíos | bot_fail |
|---|---|---|---|---|---|---|---|
| 243 | 3 | estreno | ★Ln | liana n=1 (4 dosis) | 1 | 2 | 0.100 |
| 244 | 4 | estreno | ★Ln | liana n=1 (4 dosis) | 1 | 2 | 0.075 |
| 245 | 5 | estreno | ★Ln | liana n=1 (4 dosis) | 1 | 2 | 0.125 |
| 247 | 7 | comodidad | Ln | liana n=2 (4 dosis) | 1 | 2 | 0.425 |
| 249 | 9 | reto | Ln (D) | liana n=2 (4 dosis) | 1 | 1 | 0.875 |
| 252 | 12 | reto | Ln+Re (D) | liana n=1 (4 dosis) | 2 | 1 | 0.850 |
| 258 | 18 | reto | Ln+Ma (D) | liana n=1 (4 dosis) | 2 | 2 | 0.775 |
| 260 | 20 | hito | Ln+Ba (MD) | liana n=1 (4 dosis) | 2 | 0 | 1.000 |
| 269 | 9 | reto | Ln+Br (D) | liana n=1 (4 dosis) | 2 | 1 | 0.800 |
| 278 | 18 | reto | Ln+Ba (D) | liana n=2 (4 dosis) | 2 | 0 | 0.875 |
| 283 | 3 | estreno | ★Ha | hoja_atada key=4 (4 dosis) | 1 | 2 | 0.100 |
| 284 | 4 | estreno | ★Ha | hoja_atada key=13 (4 dosis) | 1 | 2 | 0.175 |
| 285 | 5 | estreno | ★Ha | hoja_atada key=6 (4 dosis) | 1 | 2 | 0.075 |
| 287 | 7 | comodidad | Ha | hoja_atada key=13 (4 dosis) | 1 | 1 | 0.275 |
| 289 | 9 | reto | Ha (D) | hoja_atada key=14 (4 dosis) | 1 | 1 | 0.750 |
| 292 | 12 | reto | Ha+Re (D) | hoja_atada key=4 (4 dosis) | 2 | 1 | 0.825 |
| 298 | 18 | reto | Ha+Br (D) | hoja_atada key=8 (4 dosis) | 2 | 1 | 0.825 |
| 300 | 20 | hito | Ha+Li (MD) «Cuyabeno» [P] | hoja_atada key=11 (4 dosis) | 2 | 2 | 1.000 |

Todas las celdas con una variante la llevan en el nivel publicado.

## Otros niveles con avisos

| Nivel | Rol | Calendario | Aviso |
|---|---|---|---|
| 277 | comodidad | Ho | risk_adjacent: 1 contacto(s) |
| 287 | comodidad | Ha | tras una etiqueta no baja una banda entera de D (solo baja D) |
| 290 | comodidad | C | tras una etiqueta no baja una banda entera de D (solo baja D) |

## Curación del dueño

Estado: **pendiente** (0 de 32 niveles decididos) (09 §0.1 y §0.3). La decisión por nivel es «aceptar», «sustituir» (otro candidato del slot o del pool, 03 §7) o «cambiar la celda», con el motivo en los dos últimos casos. No se escribe en este informe, que es generado: va en `tools/level-generator/specs/curation_v2.json`, una entrada por nivel con el literal de la celda que vio el dueño, y la última columna la copia de ahí; si la celda cambia, la decisión deja de valer y el nivel vuelve a «pendiente». Si la curación pide cambios, se cambian la especificación o el generador y se regeneran el pack, este informe y la comprobación de `level-qa`.

| Nivel | Rol | Calendario | Etiqueta / objetivo | Propuesta [P] | Decisión del dueño |
|---|---|---|---|---|---|
| 206 | reto | Pi+Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 209 | reto | Li+Re (D) | Difícil / Difícil | aceptar | pendiente |
| 212 | reto | Pi (D) | Difícil / Difícil | aceptar | pendiente |
| 215 | reto | Re+Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 218 | reto | Li+Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 220 | hito | Ba+Tu+Ch (MD) | Muy difícil / Muy difícil | aceptar | pendiente |
| 226 | reto | Ll+Ho (D) | Difícil / Difícil | aceptar | pendiente |
| 229 | reto | Br+Ll (D) | Difícil / Difícil | aceptar | pendiente |
| 232 | reto | Ma+Ho (D) | Difícil / Difícil | aceptar | pendiente |
| 235 | reto | Br+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 238 | reto | Ll+Ho (D) | Difícil / Difícil | aceptar | pendiente |
| 240 | hito | Ho+Br (MD) | Muy difícil / Muy difícil | aceptar | pendiente |
| 246 | reto | Ma+Re (D) | Normal / Difícil | revisar | pendiente |
| 249 | reto | Ln (D) | Difícil / Difícil | aceptar | pendiente |
| 252 | reto | Ln+Re (D) | Difícil / Difícil | aceptar | pendiente |
| 255 | reto | Ma+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 258 | reto | Ln+Ma (D) | Difícil / Difícil | aceptar | pendiente |
| 260 | hito | Ln+Ba (MD) | Muy difícil / Muy difícil | aceptar | pendiente |
| 266 | reto | Pi+Br (D) | Normal / Difícil | revisar | pendiente |
| 269 | reto | Ln+Br (D) | Difícil / Difícil | aceptar | pendiente |
| 272 | reto | Pi+Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 275 | reto | Ba+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 278 | reto | Ln+Ba (D) | Difícil / Difícil | aceptar | pendiente |
| 280 | hito | Pi+Br (MD) | Normal / Muy difícil | revisar | pendiente |
| 286 | reto | Li+Re (D) | Difícil / Difícil | aceptar | pendiente |
| 289 | reto | Ha (D) | Difícil / Difícil | aceptar | pendiente |
| 292 | reto | Ha+Re (D) | Difícil / Difícil | aceptar | pendiente |
| 295 | reto | Br+Tu (D) | Difícil / Difícil | aceptar | pendiente |
| 298 | reto | Ha+Br (D) | Difícil / Difícil | revisar | pendiente |
| 300 | hito | Ha+Li (MD) «Cuyabeno» [P] | Muy difícil / Muy difícil | aceptar | pendiente |
| 243 | estreno | ★Ln | Normal / — | aceptar | pendiente |
| 283 | estreno | ★Ha | Normal / — | aceptar | pendiente |
