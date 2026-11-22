# Curva 2 de la 1.1: material de curación de 301-700 (fase F5 de D-068)

- **Autor:** level-pipeline-dev (agente), 2026-11-07. **Rama:** `v11/curva-f5-packs`.
- **Para qué:** el dueño decide con este material qué niveles se rehacen (D-068, «Pendiente del dueño: la curación de los packs
  (F5)»; F1-2: se aceptan unos 3-4 retos por mundo sin la etiqueta Difícil). Nada de este documento cambia los packs: cada nivel que
  el dueño mande rehacer se cambia en `tools/level-generator/specs/campaign_v3.json` (o con `--decisions`) y se regeneran los packs.
- **Packs** (generados, no se editan a mano): `Assets/_Project/Data/Levels/curva_07_v1.json`, `curva_08_v1.json` y `curva_09_v1.json`
  (400 niveles, `pack_schema` 5) y su reserva `reserva_curva_07_v1.json` … `reserva_curva_09_v1.json` (80 tableros). Son
  byte a byte los del pool de prueba de F3b (`tools/out/curva_f3b/`): las respuestas del dueño a F3 y F3b ya estaban aplicadas en
  ese pool como recomendadas, y lo que cambió después (el plan de presión, las notas y la regla de la Lancha) no mueve ningún
  tablero.
- **Informes completos:** `tools/out/curva2/curacion_curva2.md` (retos e hitos con los otros candidatos de cada slot y la propuesta
  «aceptar» o «revisar» [P]), `tools/out/curva2/curva_report.md`, `tools/out/curva2/qa_curva2.md` y
  `tools/out/curva2/qa_reserva_curva2.md`.
- **Capturas** (pantalla de 360 × 780 dp, 1080 × 2340 px, con gráficos; `CurveSnapshotTests.CaptureTheF5Packs`, `[Explicit]`):
  `Logs/snapshots/es-419/f5_l303.png` (estreno del Aguapé), `f5_l347.png` (estreno del espeso), `f5_l363.png` (estreno de la
  Lancha), y tres hitos: `f5_l380.png` (Pi+Ba+Ag, el de bot atento más bajo), `f5_l560.png` y `f5_l700.png` (cúspides con el
  espeso). `Logs/` no se versiona: se regeneran con
  `unity test <repo> --mode PlayMode --architecture x86_64 --filter AF.Tests.PlayMode.CurveSnapshotTests.CaptureTheF5Packs`
  (sin `-nographics`).

## 1. Resumen [V]

| Medida | Mundo 7 (301-420) | Mundo 8 (421-560) | Mundo 9 (561-700) |
|---|---|---|---|
| Niveles | 120 | 140 | 140 |
| Retos con Difícil | 28 de 30 | 32 de 35 | 33 de 35 |
| Hitos con Muy difícil | 6 de 6 | 7 de 7 | 7 de 7 |
| T media / K media | 10,4 / 8,3 | 10,4 / 8,4 | 10,4 / 8,4 |
| Niveles con T ≤ 11 / ≥ 12 / ≥ 13 | 104 / 16 / 6 | 127 / 13 / 7 | 128 / 12 / 7 |
| Con Aguapé (espeso incluido) / espeso / Lancha | 25 / 3 / 11 | 18 / 4 / 14 | 22 / 5 / 13 |
| Con `rows` (3 filas) | 13 | 9 | 14 |
| Con dosis ocultas | 19 | 28 | 27 |
| Escalón de presión de los hitos (tablero) | 5, 5, 6, 5, 6, 8 | 5, 5, 5, 6, 7, 7, 8 | 5, 6, 6, 7, 7, 7, 8 |

Plan de presión (`pressure_plan`, con 660, 680 y 700 en 7, 7 y 8 por decisión del dueño): 3, 4, 5, 5, 6, 7 · 4, 5, 5, 6, 7, 7, 8 ·
5, 6, 6, 7, 7, 7, 8. Todos los hitos llegan a su escalón del plan o lo pasan.

Tamaño: 650.685 bytes los seis packs (curva 549.736 y reserva 100.949); comprimidos con gzip -9 archivo a archivo, 57.783 bytes. Los packs de la
1.0 del mismo rango (`mundo_07..09_v1.json`) son 580.668 bytes.

## 2. Retos sin la etiqueta Difícil

Se publican como Normal, sin chip ni cinta (D-009; 03 §7); F1-2 acepta 3-4 por mundo. La curación decide si se aceptan o se
rehacen (cambiando la celda o con otro candidato del slot, `curacion_curva2.md`):

| Reto | Celda | Bot casual | Bot atento | Nota |
|---|---|---|---|---|
| 329 | Ma (D), K 10 | 72,5 % | 40 % | con K 10 desde F1-1; no llega |
| 352 | B2+Re (D) | 57,5 % | 25 % | el más bajo de los siete |
| 449 | Br+Re (D) | 72,5 % | 40 % | |
| 509 | Fl (D) | 72,5 % | 70 % | el Aguapé no lo ayudaba (65-70 %) |
| 549 | La+Ln (D) | 72,5 % | 48 % | |
| 672 | Li+Re (D) | 70 % | 15 % | con el Aguapé, 68 % |
| 686 | Ma+Ho (D) | 72,5 % | 95 % | sin chip por la respuesta 1 de F3 (ni K 10 ni el Aguapé lo llevan a 75 %) |

Son 2 en el mundo 7, 2 en el 8 y 3 en el 9 (7 de 100 retos).

## 3. Avisos de level-qa (0 fallos)

`level-qa` con reproducción (QA-REPRO regenera 301-700 y compara byte a byte): **400 niveles, 75 comprobaciones, 0 fallos y
281 avisos**; la reserva: **80 tableros, 51 comprobaciones, 0 fallos y 73 avisos**.

- **QA-WINDOW (7):** los siete retos de la sección 2, con `bot_fail` por debajo de la ventana [0,75; 0,90).
- **QA-BOT-ATENTO (8; 03 §17 #20, solo informe):** el bot atento ×40 no crece de un hito al siguiente en 360 (83 % tras 100 %),
  380 (43 % tras 83 %), 460 (85 % tras 100 %), 500 (95 % tras 100 %), 520 (73 % tras 95 %), 560 (78 % tras 93 %), 600 (75 % tras
  90 %) y 640 (93 % tras 98 %). El 380 (Pi+Ba+Ag) es el que más baja.
- **QA-BAND-Q (2):** el hito 320 mide en la banda de reto (q 0,935; cumple el mínimo de par) y la recuperación 442 en la de
  comodidad (q 0,284; cumple el mínimo de D).
- **QA-CVD-ADJ (264 en la curva, en 98 niveles contando sus variantes de alivio, y 73 en la reserva):** dos tintes de un par del
  simulador de daltonismo juntos en una cuia, que el empaquetador no pudo separar sin cambiar el tablero (Q-B9-13 bis, (b) y (A));
  es aviso, como en la 1.0, y `curacion_curva2.md` marca «revisar» los retos e hitos que lo llevan.

## 4. Tablas por mundo

Papel: recuperación (p1-p2), estreno (★), comodidad, reto (p6, p9, p12, p15 y p18), remanso (p19), hito (p20) y cúspide (420, 560
y 700). T / K = cuias / colores. Celda = la de `campaign_v3.json` (Ag Aguapé, A2 Aguapé espeso, La Lancha, Tu Aguas turbias; las
demás, las de la 1.0). Ocultas = dosis ocultas. Filas = `rows` (3 filas con 11 cuias o más y el Aguapé). Bot casual = `bot_fail` (redondeado al entero)
del bot ×40 (la etiqueta sale de él: Difícil ≥ 75 %, Muy difícil ≥ 90 %). Bot atento = el ×40 de 03 §17 #20, solo en retos e
hitos (informe de level-qa). En negrita, los retos sin Difícil.

### Mundo 7 · Madre de Dios (301-420)

| Nivel | Papel | T / K | Celda | Ocultas | Filas | Etiqueta | Bot casual | Bot atento |
|---|---|---|---|---|---|---|---|---|
| 301 | recuperación | 10 / 8 | R (tinte Arazá) | — | — | Normal | 0 % | — |
| 302 | recuperación | 10 / 8 | R | — | — | Normal | 0 % | — |
| 303 | estreno | 10 / 8 | ★Ag | — | — | Normal | 0 % | — |
| 304 | estreno | 10 / 8 | ★Ag | — | — | Normal | 5 % | — |
| 305 | estreno | 10 / 8 | ★Ag | — | — | Normal | 15 % | — |
| 306 | reto | 11 / 9 | Ha+Ll (D) | — | — | Difícil | 80 % | 83 % |
| 307 | comodidad | 9 / 8 | C | — | — | Normal | 38 % | — |
| 308 | comodidad | 10 / 8 | Ag | — | — | Normal | 50 % | — |
| 309 | reto | 11 / 9 | Ag+Ll (D) | — | 4/4/3 | Difícil | 88 % | 28 % |
| 310 | comodidad | 10 / 8 | C | — | — | Normal | 38 % | — |
| 311 | comodidad | 10 / 8 | Ba | — | — | Normal | 30 % | — |
| 312 | reto | 11 / 9 | Ag+Br (D) | — | 4/4/3 | Difícil | 85 % | 73 % |
| 313 | comodidad | 9 / 8 | C | — | — | Normal | 53 % | — |
| 314 | comodidad | 10 / 8 | Ag | — | — | Normal | 43 % | — |
| 315 | reto | 10 / 9 | Br+Tu (D) | 2 | — | Difícil | 88 % | 100 % |
| 316 | comodidad | 10 / 8 | C | — | — | Normal | 20 % | — |
| 317 | comodidad | 10 / 8 | Ll | — | — | Normal | 43 % | — |
| 318 | reto | 11 / 9 | Ag+Ln (D) | — | 4/4/3 | Difícil | 80 % | 60 % |
| 319 | remanso | 10 / 8 | S | — | — | Normal | 33 % | — |
| 320 | hito | 13 / 11 | Ha+Ll+Br (MD) | — | — | Muy difícil | 93 % | 90 % |
| 321 | recuperación | 10 / 8 | R | — | — | Normal | 0 % | — |
| 322 | recuperación | 10 / 8 | R | — | — | Normal | 3 % | — |
| 323 | estreno | 10 / 7 | ★Tu | 1 | — | Normal | 0 % | — |
| 324 | estreno | 10 / 7 | ★Tu | 1 | — | Normal | 0 % | — |
| 325 | comodidad | 10 / 8 | Ba | — | — | Normal | 33 % | — |
| 326 | reto | 12 / 9 | Ag+Ma (D) | — | 4/4/4 | Difícil | 75 % | 65 % |
| 327 | comodidad | 9 / 8 | C | — | — | Normal | 50 % | — |
| 328 | comodidad | 10 / 8 | C | — | — | Normal | 38 % | — |
| 329 | reto | 12 / 10 | Ma (D) | — | — | **Normal** | 73 % | 40 % |
| 330 | comodidad | 9 / 8 | C | — | — | Normal | 45 % | — |
| 331 | comodidad | 10 / 8 | Ma+Tu | 2 | — | Normal | 38 % | — |
| 332 | reto | 11 / 9 | Ba+Ho (D) | — | — | Difícil | 85 % | 63 % |
| 333 | comodidad | 10 / 8 | C | — | — | Normal | 30 % | — |
| 334 | comodidad | 10 / 8 | Ag | — | — | Normal | 53 % | — |
| 335 | reto | 11 / 9 | Ma+Ho (D) | — | — | Difícil | 88 % | 90 % |
| 336 | comodidad | 10 / 8 | C | — | — | Normal | 25 % | — |
| 337 | comodidad | 10 / 8 | Ba | — | — | Normal | 20 % | — |
| 338 | reto | 11 / 9 | Ag+Ba (D) | — | 4/4/3 | Difícil | 83 % | 93 % |
| 339 | remanso | 10 / 8 | S | — | — | Normal | 33 % | — |
| 340 | hito | 13 / 11 | Ag+Br+Ba (MD) | — | 5/4/4 | Muy difícil | 100 % | 100 % |
| 341 | recuperación | 10 / 8 | R | — | — | Normal | 0 % | — |
| 342 | recuperación | 10 / 8 | R | — | — | Normal | 0 % | — |
| 343 | estreno | 10 / 7 | ★B2 | — | — | Normal | 0 % | — |
| 344 | estreno | 10 / 7 | ★B2 | — | — | Normal | 0 % | — |
| 345 | estreno | 10 / 7 | ★B2 | — | — | Normal | 0 % | — |
| 346 | reto | 12 / 10 | B2+Ln (D) | — | — | Difícil | 78 % | 18 % |
| 347 | estreno | 10 / 8 | ★A2 | — | — | Normal | 10 % | — |
| 348 | estreno | 9 / 7 | ★A2 | — | — | Normal | 10 % | — |
| 349 | reto | 12 / 10 | B2 (D) | — | — | Difícil | 80 % | 35 % |
| 350 | comodidad | 10 / 8 | Tu | 2 | — | Normal | 23 % | — |
| 351 | comodidad | 10 / 8 | Ba+Tu | 2 | — | Normal | 23 % | — |
| 352 | reto | 11 / 9 | B2+Re (D) | — | — | **Normal** | 57 % | 25 % |
| 353 | comodidad | 9 / 8 | C | — | — | Normal | 60 % | — |
| 354 | comodidad | 9 / 8 | C | — | — | Normal | 38 % | — |
| 355 | reto | 11 / 9 | B2+Re (D) | — | — | Difícil | 88 % | 23 % |
| 356 | comodidad | 10 / 8 | C | — | — | Normal | 48 % | — |
| 357 | comodidad | 10 / 8 | Br+Tu | 1 | — | Normal | 28 % | — |
| 358 | reto | 11 / 9 | Ag+Ln (D) | — | 4/4/3 | Difícil | 83 % | 55 % |
| 359 | remanso | 10 / 8 | S | — | — | Normal | 28 % | — |
| 360 | hito | 13 / 11 | B2+Ln+Ag (MD) | — | 5/4/4 | Muy difícil | 100 % | 83 % |
| 361 | recuperación | 10 / 8 | R | — | — | Normal | 3 % | — |
| 362 | recuperación | 10 / 8 | R | — | — | Normal | 0 % | — |
| 363 | estreno | 10 / 6 | ★La | — | — | Normal | 0 % | — |
| 364 | estreno | 10 / 6 | ★La | — | — | Normal | 0 % | — |
| 365 | estreno | 10 / 6 | ★La | — | — | Normal | 0 % | — |
| 366 | reto | 12 / 9 | Ho+Ag+Tu (D) | 3 | 4/4/4 | Difícil | 88 % | 83 % |
| 367 | comodidad | 10 / 8 | C | — | — | Normal | 30 % | — |
| 368 | comodidad | 10 / 8 | Ag+Tu | 2 | — | Normal | 20 % | — |
| 369 | reto | 11 / 9 | Pi+Ma (D) | — | — | Difícil | 75 % | 68 % |
| 370 | comodidad | 10 / 8 | Tu | 2 | — | Normal | 23 % | — |
| 371 | comodidad | 11 / 8 | La | — | — | Normal | 35 % | — |
| 372 | reto | 12 / 9 | La+B2 (D) | — | — | Difícil | 85 % | 25 % |
| 373 | comodidad | 10 / 8 | C | — | — | Normal | 45 % | — |
| 374 | comodidad | 11 / 8 | La+Al | — | — | Normal | 28 % | — |
| 375 | reto | 12 / 9 | La+Ba (D) | — | — | Difícil | 88 % | 28 % |
| 376 | comodidad | 9 / 8 | C | — | — | Normal | 48 % | — |
| 377 | comodidad | 10 / 8 | Br+Tu | 1 | — | Normal | 35 % | — |
| 378 | reto | 12 / 9 | La+Ho (D) | — | — | Difícil | 75 % | 73 % |
| 379 | remanso | 10 / 8 | S | — | — | Normal | 23 % | — |
| 380 | hito | 13 / 11 | Pi+Ba+Ag (MD) | — | 5/4/4 | Muy difícil | 93 % | 43 % |
| 381 | recuperación | 10 / 8 | R | — | — | Normal | 3 % | — |
| 382 | recuperación | 10 / 8 | R | — | — | Normal | 5 % | — |
| 383 | comodidad | 10 / 8 | Li | — | — | Normal | 50 % | — |
| 384 | comodidad | 10 / 8 | Tu | 2 | — | Normal | 28 % | — |
| 385 | comodidad | 10 / 8 | Ag | — | — | Normal | 33 % | — |
| 386 | reto | 11 / 9 | Ba+Tu (D) | 3 | — | Difícil | 85 % | 53 % |
| 387 | comodidad | 10 / 8 | C | — | — | Normal | 40 % | — |
| 388 | comodidad | 10 / 8 | Br+Tu | 1 | — | Normal | 30 % | — |
| 389 | reto | 11 / 9 | Li+Ha (D) | — | — | Difícil | 85 % | 50 % |
| 390 | comodidad | 10 / 8 | C | — | — | Normal | 25 % | — |
| 391 | comodidad | 10 / 8 | Li | — | — | Normal | 25 % | — |
| 392 | reto | 12 / 9 | La+Ba (D) | — | — | Difícil | 88 % | 0 % |
| 393 | comodidad | 11 / 8 | La | — | — | Normal | 23 % | — |
| 394 | comodidad | 10 / 8 | Ag+Ch | — | — | Normal | 38 % | — |
| 395 | reto | 11 / 9 | Li+Ba (D) | — | — | Difícil | 85 % | 55 % |
| 396 | comodidad | 10 / 8 | C | — | — | Normal | 28 % | — |
| 397 | comodidad | 10 / 8 | Ho+Tu | 1 | — | Normal | 53 % | — |
| 398 | reto | 11 / 9 | Ha+Ba (D) | — | — | Difícil | 88 % | 73 % |
| 399 | remanso | 10 / 8 | S | — | — | Normal | 25 % | — |
| 400 | hito | 13 / 11 | Li+Ha+Ag (MD) | — | 5/4/4 | Muy difícil | 100 % | 100 % |
| 401 | recuperación | 10 / 8 | R | — | — | Normal | 23 % | — |
| 402 | recuperación | 10 / 8 | R | — | — | Normal | 13 % | — |
| 403 | estreno | 10 / 8 | ★Fl | — | — | Normal | 0 % | — |
| 404 | estreno | 10 / 8 | ★Fl | — | — | Normal | 10 % | — |
| 405 | estreno | 10 / 8 | ★Fl | — | — | Normal | 15 % | — |
| 406 | reto | 11 / 9 | Ag+Ll (D) | — | 4/4/3 | Difícil | 78 % | 90 % |
| 407 | comodidad | 10 / 8 | Fl | — | — | Normal | 48 % | — |
| 408 | comodidad | 10 / 8 | Tu | 1 | — | Normal | 23 % | — |
| 409 | reto | 11 / 9 | Fl+Ll (D) | — | — | Difícil | 75 % | 48 % |
| 410 | comodidad | 9 / 8 | C | — | — | Normal | 53 % | — |
| 411 | comodidad | 9 / 8 | Ag | — | — | Normal | 60 % | — |
| 412 | reto | 12 / 9 | Fl+La (D) | — | — | Difícil | 78 % | 38 % |
| 413 | comodidad | 10 / 8 | C | — | — | Normal | 33 % | — |
| 414 | comodidad | 10 / 8 | Br+Tu | 1 | — | Normal | 50 % | — |
| 415 | reto | 10 / 9 | Fl (D) | — | — | Difícil | 85 % | 60 % |
| 416 | comodidad | 10 / 8 | C | — | — | Normal | 40 % | — |
| 417 | comodidad | 10 / 8 | Ho+Tu | 2 | — | Normal | 55 % | — |
| 418 | reto | 11 / 9 | Br+Tu (D) | 2 | — | Difícil | 88 % | 5 % |
| 419 | remanso | 10 / 8 | S | — | — | Normal | 30 % | — |
| 420 | cúspide | 14 / 12 | Fl+Ba+A2 (MD) «Zafra» [P] | — | 5/5/4 | Muy difícil | 100 % | 100 % |

### Mundo 8 · Madidi (421-560)

| Nivel | Papel | T / K | Celda | Ocultas | Filas | Etiqueta | Bot casual | Bot atento |
|---|---|---|---|---|---|---|---|---|
| 421 | recuperación | 10 / 8 | R | — | — | Normal | 8 % | — |
| 422 | recuperación | 10 / 8 | R | — | — | Normal | 0 % | — |
| 423 | comodidad | 10 / 8 | Ma+Tu | 2 | — | Normal | 48 % | — |
| 424 | comodidad | 9 / 8 | C | — | — | Normal | 33 % | — |
| 425 | comodidad | 11 / 8 | La | — | — | Normal | 30 % | — |
| 426 | reto | 11 / 9 | Fl+Ma (D) | — | — | Difícil | 78 % | 60 % |
| 427 | comodidad | 10 / 8 | C | — | — | Normal | 33 % | — |
| 428 | comodidad | 10 / 8 | Ba+Tu | 1 | — | Normal | 57 % | — |
| 429 | reto | 11 / 9 | Ag+Ma+Re (D) | — | 4/4/3 | Difícil | 75 % | 58 % |
| 430 | comodidad | 10 / 8 | C | — | — | Normal | 38 % | — |
| 431 | comodidad | 10 / 8 | Ag+B2 | — | — | Normal | 57 % | — |
| 432 | reto | 12 / 9 | Fl+Ho (D) | — | — | Difícil | 75 % | 75 % |
| 433 | comodidad | 10 / 8 | Tu | 1 | — | Normal | 23 % | — |
| 434 | comodidad | 10 / 8 | Al | — | — | Normal | 45 % | — |
| 435 | reto | 11 / 9 | Ma+Tu (D) | 2 | — | Difícil | 88 % | 98 % |
| 436 | comodidad | 9 / 8 | C | — | — | Normal | 48 % | — |
| 437 | comodidad | 11 / 8 | La+Ha | — | — | Normal | 45 % | — |
| 438 | reto | 11 / 9 | Ho+Re (D) | — | — | Difícil | 78 % | 75 % |
| 439 | remanso | 10 / 8 | S | — | — | Normal | 38 % | — |
| 440 | hito | 13 / 11 | Ag+Ho+Re (MD) | — | 5/4/4 | Muy difícil | 100 % | 100 % |
| 441 | recuperación | 10 / 8 | R | — | — | Normal | 3 % | — |
| 442 | recuperación | 10 / 8 | R | — | — | Normal | 13 % | — |
| 443 | comodidad | 10 / 8 | Pi | — | — | Normal | 35 % | — |
| 444 | comodidad | 11 / 8 | La | — | — | Normal | 33 % | — |
| 445 | comodidad | 10 / 8 | Br+Tu | 1 | — | Normal | 48 % | — |
| 446 | reto | 11 / 9 | Pi+Ba (D) | — | — | Difícil | 88 % | 43 % |
| 447 | comodidad | 10 / 8 | C | — | — | Normal | 33 % | — |
| 448 | comodidad | 10 / 8 | Ho+Tu | 2 | — | Normal | 57 % | — |
| 449 | reto | 11 / 9 | Br+Re (D) | — | — | **Normal** | 73 % | 40 % |
| 450 | comodidad | 10 / 8 | C | — | — | Normal | 55 % | — |
| 451 | comodidad | 10 / 8 | Pi | — | — | Normal | 23 % | — |
| 452 | reto | 12 / 9 | Pi+Br+Ag (D) | — | 4/4/4 | Difícil | 83 % | 80 % |
| 453 | comodidad | 10 / 8 | Tu | 1 | — | Normal | 38 % | — |
| 454 | comodidad | 11 / 8 | La | — | — | Normal | 30 % | — |
| 455 | reto | 11 / 9 | Br+Tu (D) | 3 | — | Difícil | 78 % | 8 % |
| 456 | comodidad | 10 / 8 | C | — | — | Normal | 25 % | — |
| 457 | comodidad | 10 / 8 | Ag+Ha | — | — | Normal | 53 % | — |
| 458 | reto | 11 / 9 | Ba+Re (D) | — | — | Difícil | 80 % | 45 % |
| 459 | remanso | 10 / 8 | S | — | — | Normal | 35 % | — |
| 460 | hito | 13 / 11 | Pi+Ba+Ag (MD) | — | 5/4/4 | Muy difícil | 100 % | 85 % |
| 461 | recuperación | 10 / 8 | R | — | — | Normal | 3 % | — |
| 462 | recuperación | 10 / 8 | R | — | — | Normal | 18 % | — |
| 463 | comodidad | 10 / 8 | Li | — | — | Normal | 43 % | — |
| 464 | comodidad | 10 / 8 | Tu | 1 | — | Normal | 53 % | — |
| 465 | comodidad | 10 / 8 | B2 | — | — | Normal | 23 % | — |
| 466 | reto | 11 / 9 | Li+B2 (D) | — | — | Difícil | 88 % | 48 % |
| 467 | comodidad | 9 / 8 | C | — | — | Normal | 30 % | — |
| 468 | comodidad | 10 / 8 | Ag | — | — | Normal | 60 % | — |
| 469 | reto | 11 / 9 | Ln+B2 (D) | — | — | Difícil | 83 % | 40 % |
| 470 | comodidad | 9 / 8 | C | — | — | Normal | 23 % | — |
| 471 | comodidad | 10 / 8 | Br+Tu | 2 | — | Normal | 33 % | — |
| 472 | reto | 12 / 9 | Li+La (D) | — | — | Difícil | 75 % | 3 % |
| 473 | comodidad | 10 / 8 | C | — | — | Normal | 28 % | — |
| 474 | comodidad | 10 / 8 | Ag+Ch | — | — | Normal | 45 % | — |
| 475 | reto | 11 / 9 | Ho+Tu (D) | 2 | — | Difícil | 75 % | 13 % |
| 476 | comodidad | 9 / 8 | C | — | — | Normal | 50 % | — |
| 477 | comodidad | 10 / 8 | Ba+Tu | 1 | — | Normal | 43 % | — |
| 478 | reto | 12 / 9 | La+Ln (D) | — | — | Difícil | 85 % | 3 % |
| 479 | remanso | 10 / 8 | S | — | — | Normal | 25 % | — |
| 480 | hito | 13 / 11 | Li+B2+Ho (MD) | — | — | Muy difícil | 100 % | 100 % |
| 481 | recuperación | 10 / 8 | R | — | — | Normal | 10 % | — |
| 482 | recuperación | 10 / 8 | R | — | — | Normal | 3 % | — |
| 483 | comodidad | 10 / 8 | Ma+Tu | 2 | — | Normal | 50 % | — |
| 484 | comodidad | 10 / 8 | C | — | — | Normal | 35 % | — |
| 485 | comodidad | 11 / 8 | La | — | — | Normal | 28 % | — |
| 486 | reto | 11 / 9 | Ma+Ha (D) | — | — | Difícil | 78 % | 13 % |
| 487 | comodidad | 10 / 8 | Tu | 2 | — | Normal | 30 % | — |
| 488 | comodidad | 10 / 8 | Ag | — | — | Normal | 57 % | — |
| 489 | reto | 11 / 9 | Ma+Ba (D) | — | — | Difícil | 83 % | 13 % |
| 490 | comodidad | 10 / 8 | C | — | — | Normal | 35 % | — |
| 491 | comodidad | 10 / 8 | Br+Tu | 2 | — | Normal | 23 % | — |
| 492 | reto | 10 / 9 | Ag+Ha+Ba (D) | — | — | Difícil | 85 % | 58 % |
| 493 | comodidad | 9 / 8 | C | — | — | Normal | 48 % | — |
| 494 | comodidad | 9 / 8 | C | — | — | Normal | 57 % | — |
| 495 | reto | 11 / 9 | Ma+Tu (D) | 3 | — | Difícil | 83 % | 55 % |
| 496 | comodidad | 10 / 8 | C | — | — | Normal | 45 % | — |
| 497 | comodidad | 11 / 8 | La+B2 | — | — | Normal | 30 % | — |
| 498 | reto | 11 / 9 | Fl+Ha (D) | — | — | Difícil | 85 % | 58 % |
| 499 | remanso | 10 / 8 | S | — | — | Normal | 23 % | — |
| 500 | hito | 13 / 11 | Re+Ha+A2 (MD) | — | 5/4/4 | Muy difícil | 95 % | 95 % |
| 501 | recuperación | 10 / 8 | R | — | — | Normal | 5 % | — |
| 502 | recuperación | 10 / 8 | R | — | — | Normal | 0 % | — |
| 503 | comodidad | 10 / 8 | Ll | — | — | Normal | 35 % | — |
| 504 | comodidad | 9 / 8 | C | — | — | Normal | 23 % | — |
| 505 | comodidad | 10 / 8 | Ba+Tu | 2 | — | Normal | 25 % | — |
| 506 | reto | 12 / 9 | La+Ba (D) | — | — | Difícil | 88 % | 45 % |
| 507 | comodidad | 10 / 8 | C | — | — | Normal | 55 % | — |
| 508 | comodidad | 10 / 8 | Ho+Tu | 2 | — | Normal | 23 % | — |
| 509 | reto | 11 / 9 | Fl (D) | — | — | **Normal** | 73 % | 70 % |
| 510 | comodidad | 10 / 8 | C | — | — | Normal | 28 % | — |
| 511 | comodidad | 9 / 8 | Ag+Li | — | — | Normal | 60 % | — |
| 512 | reto | 11 / 9 | Ll+Ha (D) | — | — | Difícil | 88 % | 63 % |
| 513 | comodidad | 10 / 8 | Tu | 2 | — | Normal | 38 % | — |
| 514 | comodidad | 10 / 8 | Ll+Al | — | — | Normal | 25 % | — |
| 515 | reto | 11 / 9 | Ba+Tu (D) | 2 | — | Difícil | 88 % | 73 % |
| 516 | comodidad | 10 / 8 | C | — | — | Normal | 55 % | — |
| 517 | comodidad | 11 / 8 | La+B2 | — | — | Normal | 35 % | — |
| 518 | reto | 11 / 9 | Fl+Ba (D) | — | — | Difícil | 80 % | 90 % |
| 519 | remanso | 10 / 8 | S | — | — | Normal | 28 % | — |
| 520 | hito | 13 / 11 | Li+Ha+A2 (MD) | — | 5/4/4 | Muy difícil | 100 % | 73 % |
| 521 | recuperación | 10 / 8 | R | — | — | Normal | 15 % | — |
| 522 | recuperación | 10 / 8 | R | — | — | Normal | 25 % | — |
| 523 | comodidad | 10 / 8 | Pi | — | — | Normal | 43 % | — |
| 524 | comodidad | 10 / 8 | Tu | 2 | — | Normal | 30 % | — |
| 525 | comodidad | 10 / 8 | Ag | — | — | Normal | 57 % | — |
| 526 | reto | 11 / 9 | Pi+Ha (D) | — | — | Difícil | 85 % | 83 % |
| 527 | comodidad | 9 / 8 | C | — | — | Normal | 23 % | — |
| 528 | comodidad | 10 / 8 | Br+Tu | 2 | — | Normal | 30 % | — |
| 529 | reto | 11 / 9 | Ag+Ho+Re (D) | — | 4/4/3 | Difícil | 80 % | 65 % |
| 530 | comodidad | 9 / 8 | C | — | — | Normal | 28 % | — |
| 531 | comodidad | 10 / 8 | Tu | 2 | — | Normal | 43 % | — |
| 532 | reto | 11 / 9 | Pi+Ho (D) | — | — | Difícil | 85 % | 63 % |
| 533 | comodidad | 11 / 8 | La | — | — | Normal | 40 % | — |
| 534 | comodidad | 10 / 8 | Ch | — | — | Normal | 30 % | — |
| 535 | reto | 11 / 9 | Ho+Tu (D) | 2 | — | Difícil | 83 % | 83 % |
| 536 | comodidad | 10 / 8 | C | — | — | Normal | 28 % | — |
| 537 | comodidad | 11 / 8 | La+B2 | — | — | Normal | 48 % | — |
| 538 | reto | 11 / 9 | Ha+Re (D) | — | — | Difícil | 80 % | 35 % |
| 539 | remanso | 10 / 8 | S | — | — | Normal | 30 % | — |
| 540 | hito | 13 / 11 | Ln+Ha+A2 (MD) | — | 5/4/4 | Muy difícil | 98 % | 93 % |
| 541 | recuperación | 10 / 8 | R | — | — | Normal | 0 % | — |
| 542 | recuperación | 10 / 8 | R | — | — | Normal | 8 % | — |
| 543 | comodidad | 9 / 8 | Fl | — | — | Normal | 48 % | — |
| 544 | comodidad | 10 / 8 | C | — | — | Normal | 25 % | — |
| 545 | comodidad | 10 / 8 | Ho+Tu | 2 | — | Normal | 38 % | — |
| 546 | reto | 11 / 9 | Fl+Ho (D) | — | — | Difícil | 80 % | 98 % |
| 547 | comodidad | 10 / 8 | C | — | — | Normal | 60 % | — |
| 548 | comodidad | 11 / 8 | Ba+Tu | 2 | — | Normal | 20 % | — |
| 549 | reto | 12 / 9 | La+Ln (D) | — | — | **Normal** | 73 % | 48 % |
| 550 | comodidad | 10 / 8 | C | — | — | Normal | 23 % | — |
| 551 | comodidad | 10 / 8 | Ag+Ll | — | — | Normal | 33 % | — |
| 552 | reto | 10 / 9 | Ln+Re (D) | — | — | Difícil | 88 % | 70 % |
| 553 | comodidad | 10 / 8 | C | — | — | Normal | 57 % | — |
| 554 | comodidad | 11 / 8 | La+Al | — | — | Normal | 48 % | — |
| 555 | reto | 11 / 9 | Ho+Tu (D) | 2 | — | Difícil | 83 % | 85 % |
| 556 | comodidad | 10 / 8 | C | — | — | Normal | 45 % | — |
| 557 | comodidad | 10 / 8 | Br+Tu | 2 | — | Normal | 33 % | — |
| 558 | reto | 11 / 9 | Fl (D) | — | — | Difícil | 83 % | 65 % |
| 559 | remanso | 10 / 8 | S | — | — | Normal | 35 % | — |
| 560 | cúspide | 14 / 12 | Fl+Re+A2 (MD) «Riberalta» [P] | — | 5/5/4 | Muy difícil | 93 % | 78 % |

### Mundo 9 · Ucayali (561-700)

| Nivel | Papel | T / K | Celda | Ocultas | Filas | Etiqueta | Bot casual | Bot atento |
|---|---|---|---|---|---|---|---|---|
| 561 | recuperación | 10 / 8 | R | — | — | Normal | 15 % | — |
| 562 | recuperación | 10 / 8 | R | — | — | Normal | 3 % | — |
| 563 | comodidad | 10 / 8 | Ma+Tu | 1 | — | Normal | 33 % | — |
| 564 | comodidad | 10 / 8 | C | — | — | Normal | 45 % | — |
| 565 | comodidad | 11 / 8 | La+B2 | — | — | Normal | 35 % | — |
| 566 | reto | 12 / 9 | Ag+Ma+Re (D) | — | 4/4/4 | Difícil | 75 % | 53 % |
| 567 | comodidad | 9 / 8 | C | — | — | Normal | 45 % | — |
| 568 | comodidad | 10 / 8 | Ag+Ha | — | — | Normal | 50 % | — |
| 569 | reto | 11 / 9 | B2+Ba (D) | — | — | Difícil | 75 % | 35 % |
| 570 | comodidad | 11 / 8 | La | — | — | Normal | 23 % | — |
| 571 | comodidad | 10 / 8 | Ho+Tu | 1 | — | Normal | 35 % | — |
| 572 | reto | 11 / 9 | Ma+Ba (D) | — | — | Difícil | 80 % | 13 % |
| 573 | comodidad | 10 / 8 | C | — | — | Normal | 60 % | — |
| 574 | comodidad | 9 / 8 | C | — | — | Normal | 33 % | — |
| 575 | reto | 11 / 9 | Ma+Tu (D) | 3 | — | Difícil | 80 % | 55 % |
| 576 | comodidad | 9 / 8 | C | — | — | Normal | 35 % | — |
| 577 | comodidad | 10 / 8 | Br+Tu | 2 | — | Normal | 28 % | — |
| 578 | reto | 11 / 9 | B2+Re (D) | — | — | Difícil | 78 % | 23 % |
| 579 | remanso | 10 / 8 | S | — | — | Normal | 30 % | — |
| 580 | hito | 13 / 11 | Ba+B2+Ag (MD) | — | 5/4/4 | Muy difícil | 100 % | 90 % |
| 581 | recuperación | 10 / 8 | R | — | — | Normal | 8 % | — |
| 582 | recuperación | 10 / 8 | R | — | — | Normal | 3 % | — |
| 583 | comodidad | 10 / 8 | Pi | — | — | Normal | 45 % | — |
| 584 | comodidad | 10 / 8 | Tu | 1 | — | Normal | 25 % | — |
| 585 | comodidad | 10 / 8 | Ag+Ha | — | — | Normal | 40 % | — |
| 586 | reto | 11 / 9 | Pi+Ha (D) | — | — | Difícil | 83 % | 38 % |
| 587 | comodidad | 9 / 8 | C | — | — | Normal | 40 % | — |
| 588 | comodidad | 10 / 8 | Ho+Tu | 2 | — | Normal | 28 % | — |
| 589 | reto | 11 / 9 | Ha+B2 (D) | — | — | Difícil | 88 % | 23 % |
| 590 | comodidad | 10 / 8 | C | — | — | Normal | 35 % | — |
| 591 | comodidad | 11 / 8 | La | — | — | Normal | 38 % | — |
| 592 | reto | 12 / 9 | Pi+B2+Ag (D) | — | 4/4/4 | Difícil | 78 % | 90 % |
| 593 | comodidad | 10 / 8 | C | — | — | Normal | 28 % | — |
| 594 | comodidad | 11 / 8 | La+Al | — | — | Normal | 23 % | — |
| 595 | reto | 11 / 9 | Ba+Tu (D) | 2 | — | Difícil | 88 % | 90 % |
| 596 | comodidad | 10 / 8 | C | — | — | Normal | 38 % | — |
| 597 | comodidad | 10 / 8 | Br+Tu | 2 | — | Normal | 30 % | — |
| 598 | reto | 11 / 9 | Pi+Ba (D) | — | — | Difícil | 88 % | 68 % |
| 599 | remanso | 10 / 8 | S | — | — | Normal | 20 % | — |
| 600 | hito | 13 / 11 | Ha+Ba+A2 (MD) | — | 5/4/4 | Muy difícil | 100 % | 75 % |
| 601 | recuperación | 10 / 8 | R | — | — | Normal | 18 % | — |
| 602 | recuperación | 10 / 8 | R | — | — | Normal | 25 % | — |
| 603 | comodidad | 9 / 8 | Ln | — | — | Normal | 48 % | — |
| 604 | comodidad | 10 / 8 | Tu | 2 | — | Normal | 60 % | — |
| 605 | comodidad | 10 / 8 | Ag | — | — | Normal | 38 % | — |
| 606 | reto | 10 / 9 | Ln+Re (D) | — | — | Difícil | 80 % | 53 % |
| 607 | comodidad | 9 / 8 | C | — | — | Normal | 50 % | — |
| 608 | comodidad | 10 / 8 | Ho+Tu | 2 | — | Normal | 25 % | — |
| 609 | reto | 11 / 9 | Li+Ll (D) | — | — | Difícil | 80 % | 40 % |
| 610 | comodidad | 9 / 8 | C | — | — | Normal | 53 % | — |
| 611 | comodidad | 10 / 8 | Br+Tu | 1 | — | Normal | 23 % | — |
| 612 | reto | 11 / 9 | Ag+Ll (D) | — | 4/4/3 | Difícil | 88 % | 60 % |
| 613 | comodidad | 11 / 8 | La | — | — | Normal | 30 % | — |
| 614 | comodidad | 10 / 8 | Ch | — | — | Normal | 50 % | — |
| 615 | reto | 11 / 9 | Li+Re (D) | — | — | Difícil | 85 % | 35 % |
| 616 | comodidad | 10 / 8 | C | — | — | Normal | 28 % | — |
| 617 | comodidad | 11 / 8 | La+Fl | — | — | Normal | 30 % | — |
| 618 | reto | 12 / 9 | Ln+Ll (D) | — | — | Difícil | 85 % | 5 % |
| 619 | remanso | 10 / 8 | S | — | — | Normal | 20 % | — |
| 620 | hito | 13 / 11 | Ln+Ll+Ag (MD) | — | 5/4/4 | Muy difícil | 100 % | 98 % |
| 621 | recuperación | 10 / 8 | R | — | — | Normal | 5 % | — |
| 622 | recuperación | 10 / 8 | R | — | — | Normal | 0 % | — |
| 623 | comodidad | 10 / 8 | Ma+Tu | 2 | — | Normal | 28 % | — |
| 624 | comodidad | 10 / 8 | C | — | — | Normal | 28 % | — |
| 625 | comodidad | 10 / 8 | Ag+Ba | — | — | Normal | 55 % | — |
| 626 | reto | 11 / 9 | Ma+Ba (D) | — | — | Difícil | 88 % | 15 % |
| 627 | comodidad | 10 / 8 | Tu | 2 | — | Normal | 48 % | — |
| 628 | comodidad | 11 / 8 | La+B2 | — | — | Normal | 25 % | — |
| 629 | reto | 11 / 9 | Ll+Ma (D) | — | — | Difícil | 88 % | 23 % |
| 630 | comodidad | 10 / 8 | C | — | — | Normal | 28 % | — |
| 631 | comodidad | 10 / 8 | Tu | 1 | — | Normal | 35 % | — |
| 632 | reto | 11 / 9 | Ma+Ho (D) | — | — | Difícil | 83 % | 68 % |
| 633 | comodidad | 10 / 8 | C | — | — | Normal | 43 % | — |
| 634 | comodidad | 10 / 8 | Al | — | — | Normal | 30 % | — |
| 635 | reto | 11 / 9 | Ma+Tu (D) | 2 | — | Difícil | 85 % | 50 % |
| 636 | comodidad | 10 / 8 | C | — | — | Normal | 35 % | — |
| 637 | comodidad | 11 / 8 | La+Ha | — | — | Normal | 33 % | — |
| 638 | reto | 12 / 9 | Ag+Ba+Ho (D) | — | 4/4/4 | Difícil | 75 % | 48 % |
| 639 | remanso | 10 / 8 | S | — | — | Normal | 20 % | — |
| 640 | hito | 13 / 11 | Li+Ba+A2 (MD) | — | 5/4/4 | Muy difícil | 95 % | 93 % |
| 641 | recuperación | 10 / 8 | R | — | — | Normal | 5 % | — |
| 642 | recuperación | 10 / 8 | R | — | — | Normal | 20 % | — |
| 643 | comodidad | 10 / 8 | Pi | — | — | Normal | 38 % | — |
| 644 | comodidad | 11 / 8 | La | — | — | Normal | 33 % | — |
| 645 | comodidad | 10 / 8 | Br+Tu | 2 | — | Normal | 50 % | — |
| 646 | reto | 12 / 9 | Pi+Br+Ha (D) | — | — | Difícil | 80 % | 3 % |
| 647 | comodidad | 10 / 8 | C | — | — | Normal | 40 % | — |
| 648 | comodidad | 11 / 8 | La+Al | — | — | Normal | 38 % | — |
| 649 | reto | 11 / 9 | Pi+Ha (D) | — | — | Difícil | 83 % | 75 % |
| 650 | comodidad | 10 / 8 | Tu | 1 | — | Normal | 25 % | — |
| 651 | comodidad | 9 / 8 | C | — | — | Normal | 33 % | — |
| 652 | reto | 11 / 9 | Ag+Br+Re (D) | — | 4/4/3 | Difícil | 83 % | 93 % |
| 653 | comodidad | 10 / 8 | Tu | 1 | — | Normal | 25 % | — |
| 654 | comodidad | 10 / 8 | Ag+Pi | — | — | Normal | 45 % | — |
| 655 | reto | 11 / 9 | Br+Tu (D) | 3 | — | Difícil | 85 % | 58 % |
| 656 | comodidad | 9 / 8 | C | — | — | Normal | 53 % | — |
| 657 | comodidad | 10 / 8 | Ag+B2 | — | — | Normal | 40 % | — |
| 658 | reto | 11 / 9 | Ag+Ha+Re (D) | — | 4/4/3 | Difícil | 85 % | 63 % |
| 659 | remanso | 10 / 8 | S | — | — | Normal | 33 % | — |
| 660 | hito | 13 / 11 | Ln+Ha+A2 (MD) | — | 5/4/4 | Muy difícil | 98 % | 95 % |
| 661 | recuperación | 10 / 8 | R | — | — | Normal | 20 % | — |
| 662 | recuperación | 10 / 8 | R | — | — | Normal | 18 % | — |
| 663 | comodidad | 11 / 8 | La+Li | — | — | Normal | 25 % | — |
| 664 | comodidad | 10 / 8 | Tu | 2 | — | Normal | 28 % | — |
| 665 | comodidad | 10 / 8 | Ag+Ha | — | — | Normal | 50 % | — |
| 666 | reto | 10 / 9 | Li+Ha (D) | — | — | Difícil | 88 % | 13 % |
| 667 | comodidad | 9 / 8 | C | — | — | Normal | 35 % | — |
| 668 | comodidad | 10 / 8 | Ho+Tu | 2 | — | Normal | 60 % | — |
| 669 | reto | 11 / 9 | Li+Br (D) | — | — | Difícil | 85 % | 13 % |
| 670 | comodidad | 10 / 8 | C | — | — | Normal | 50 % | — |
| 671 | comodidad | 10 / 8 | Ba+Tu | 2 | — | Normal | 33 % | — |
| 672 | reto | 11 / 9 | Li+Re (D) | — | — | **Normal** | 70 % | 15 % |
| 673 | comodidad | 9 / 8 | C | — | — | Normal | 55 % | — |
| 674 | comodidad | 10 / 8 | Ag+Ch | — | — | Normal | 55 % | — |
| 675 | reto | 11 / 9 | Br+Tu (D) | 2 | — | Difícil | 80 % | 55 % |
| 676 | comodidad | 9 / 8 | C | — | — | Normal | 35 % | — |
| 677 | comodidad | 11 / 8 | La+B2 | — | — | Normal | 20 % | — |
| 678 | reto | 11 / 9 | Ha+Re (D) | — | — | Difícil | 88 % | 68 % |
| 679 | remanso | 10 / 8 | S | — | — | Normal | 33 % | — |
| 680 | hito | 13 / 11 | Li+Ha+A2 (MD) | — | 5/4/4 | Muy difícil | 100 % | 100 % |
| 681 | recuperación | 10 / 8 | R | — | — | Normal | 3 % | — |
| 682 | recuperación | 10 / 8 | R | — | — | Normal | 0 % | — |
| 683 | comodidad | 10 / 8 | Ma+Tu | 2 | — | Normal | 55 % | — |
| 684 | comodidad | 10 / 8 | C | — | — | Normal | 30 % | — |
| 685 | comodidad | 10 / 8 | Ho | — | — | Normal | 55 % | — |
| 686 | reto | 11 / 9 | Ma+Ho (D) | — | — | **Normal** | 73 % | 95 % |
| 687 | comodidad | 10 / 8 | C | — | — | Normal | 28 % | — |
| 688 | comodidad | 11 / 8 | La | — | — | Normal | 20 % | — |
| 689 | reto | 11 / 9 | Ho+B2 (D) | — | — | Difícil | 80 % | 0 % |
| 690 | comodidad | 10 / 8 | Tu | 2 | — | Normal | 23 % | — |
| 691 | comodidad | 9 / 8 | C | — | — | Normal | 60 % | — |
| 692 | reto | 11 / 9 | Ma+Ha (D) | — | — | Difícil | 80 % | 8 % |
| 693 | comodidad | 9 / 8 | C | — | — | Normal | 60 % | — |
| 694 | comodidad | 11 / 8 | Ma+Ch | — | — | Normal | 20 % | — |
| 695 | reto | 11 / 9 | Ho+Tu (D) | 2 | — | Difícil | 88 % | 83 % |
| 696 | comodidad | 10 / 8 | C | — | — | Normal | 45 % | — |
| 697 | comodidad | 10 / 8 | Ba+Tu | 2 | — | Normal | 43 % | — |
| 698 | reto | 11 / 9 | Ag+Ha+B2 (D) | — | 4/4/3 | Difícil | 88 % | 85 % |
| 699 | remanso | 10 / 8 | S | — | — | Normal | 38 % | — |
| 700 | cúspide | 14 / 12 | Fl+Ha+A2 (MD) «Yarinacocha» [P] | — | 5/5/4 | Muy difícil | 100 % | 100 % |

