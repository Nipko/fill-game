# Validación de los niveles 1-200 (2026-10-14)

Pedida por el dueño: validar jugabilidad, concordancia, dificultad, errores y que cada nivel se resuelva, de forma eficiente. Se hizo en el portátil sobre `integracion-fase1-fase2` en `00bcdaa` (packs `tutorial_v0` y `mundo_02..05_v1`), en dos frentes a la vez: los packs en el PC y una partida real del 1 al 200 en el teléfono.

**Resultado: los 200 niveles se resuelven y se ganan en el cliente real.**
- El tablero que carga el juego es idéntico al del pack en los 200.
- La dificultad sigue el ciclo de 20 de D-009.
- Los estrenos, las tarjetas, los banners y la Cosecha cuadran con el GDD.
- En logcat no hay ninguna excepción ni línea de error.
- No hay fallos. Quedan avisos de diseño para el dueño (al final).

Los informes completos, el logcat y los resultados por nivel están en el portátil, en `Logs/validacion/` (fuera de git): `level_qa_report.md`, `tutorial_report.md`, `soluciones.json`, `partida.jsonl` y `partida_logcat.txt`.

## 1 · Packs en el PC

| Comando | Resultado |
|---|---|
| `dotnet run --project tools/level-generator -c Release -- --handmade tools/level-generator/specs/tutorial_v0.json --out Assets/_Project/Data/Levels --report Logs/validacion/tutorial_report.md --check` | Niveles 1-10: **141/141 PASS**; el pack en disco coincide con el generado |
| `dotnet run --project tools/level-qa -c Release -- Assets/_Project/Data/Levels/mundo_0{2,3,4,5}_v1.json --report Logs/validacion/level_qa_report.md` | Niveles 11-200: **50 comprobaciones, 0 fallos, 28 avisos** (≈ 7 min) |
| `AStarSolver` (`run`, w = 1) sobre los 200 niveles, con una herramienta temporal fuera del repo | **200/200 resueltos**; en el 11-200 la solución mide exactamente el par publicado |

`level-qa` comprueba, entre otras cosas:
- estructura y unidades;
- que ningún nivel empieza atascado;
- el par recalculado y el `bot_fail` recalculado con su semilla;
- etiquetas, ventanas y escalera del ciclo (D-009), y el remanso;
- estrenos y calendario de mecánicas;
- Aguas turbias con ruta segura;
- variantes de alivio (`relief`) válidas;
- formas sin repetir;
- el esquema;
- que el pack se reproduce byte a byte desde la especificación (QA-REPRO).

## 2 · Partida real en el teléfono (1 → 200 con «Siguiente»)

`dev.apk` de `2c75897` en el SM-S938B, por depuración inalámbrica, con perfil limpio y `--es af_ads_fill no_fill` (sin intersticiales).

Un guion juega cada nivel con su solución A* mediante toques reales: compara el `AF_STATE load` con el pack, cierra el banner o la tarjeta previa si salen, espera la victoria y pulsa «Siguiente». Duró **49 min** y se jugaron 3.624 vertidos.

| Comprobación | Resultado |
|---|---|
| Niveles ganados en el cliente | **200/200**, sin un solo reintento ni error del guion |
| Tablero cargado frente al pack (dosis visibles, número de cuias, etiqueta, mundo, mecánicas) | Idéntico en los 200 |
| Rechazos (`reject`) durante las soluciones | 0 |
| Aguas estancadas durante una solución ganadora (D-014 no da falsos positivos) | 0 |
| Cosecha frente a etiqueta (20 / 60 / 80) | Cuadra en los 200 |
| Banner de mundo | Exactamente en el 11, 41, 81 y 141 |
| Tarjeta previa | Exactamente en los 43 niveles «Difícil» o «Muy difícil» y en los 11 estrenos (14, 27, 43, 63, 83, 100, 103, 123, 143, 163 y 183): 53 en total, porque el 100 es las dos cosas; ni una de más ni de menos |
| Excepciones o líneas `E Unity` en logcat | 0 |
| De `load` a `ready` (intro) | Mediana de 1.400 ms en el 1-20 y de 796 ms en el 21-200 (T = 1.400 / 800); máximo de 3.070 ms en el 11, banner incluido |
| `save_ms` (hilo principal), vertidos normales | Mediana 0,17 ms, p95 0,29 ms y máximo 6,49 ms; 2 de 3.427 pasan de 5 ms |
| `save_ms`, vertidos que ganan (`sync`, 197) | Mediana 3,72 ms, p95 5,34 ms y máximo 7,88 ms (nivel 171) |
| `save_ms`, todos | p95 2,96 ms; **ninguno pasa de 8 ms** (objetivo de T-D: p95 < 5 ms y ninguno > 8 ms) |
| `step_budget` (pasos de mecánica que pasan de 2,8 s desde el toque) | 6 avisos en 5 niveles (observación 3) |

## 3 · Dificultad

Ciclo de 20 (D-009) en los niveles 11-200, por posición. Etiqueta: N = normal, D = «Difícil», MD = «Muy difícil».

| Posición | Rol | Etiquetas | `bot_fail` medio | D medio |
|---|---|---|---|---|
| p1-p2 | Recuperación | N | 0,03-0,06 | −0,77 / −0,68 |
| p3-p5 | Comodidad (estrenos en p3) | N | 0,06-0,09 | ≈ −0,45 |
| p6 | Reto | 6 D, 3 N | 0,74 | 0,39 |
| p7-p8, p10-p11, p13-p14, p16-p17 | Comodidad | N | 0,30-0,41 | −0,33 a −0,01 |
| p9 | Reto | 6 D, 3 N | 0,76 | 0,47 |
| p12 | Reto | 8 D, 2 N | 0,72 | 0,47 |
| p15 | Reto | 6 D, 4 N | 0,71 | 0,54 |
| p18 | Reto | 8 D, 2 N | 0,72 | 0,65 |
| p19 | Remanso | N | 0,25 | −0,33 |
| p20 | Hito | 8 MD, 1 D, 1 N (rampa del 20) | 0,89 | 0,94 |

Por mundo, el par medio sube de 16,3 a 17,2, 19,4 y 20,0 (mundos 2 a 5). El fallo del bot pasa de 0,28 a ≈ 0,44.

| Mundo | Niveles | «Difícil» | «Muy difícil» |
|---|---|---|---|
| 2 | 30 | 1 | 0 |
| 3 | 40 | 6 | 2 |
| 4 | 60 | 15 | 3 |
| 5 | 60 | 13 | 3 |

**Estrenos en los packs:** idénticos a la lista única de 02 §9.1:

| Nivel | Estreno |
|---|---|
| 14 | Cuia chica de 2 |
| 18 | Cuia chica de 1 |
| 22 | Cuia chica de 3 |
| 27 | Cuia alta de 5 |
| 43 | Hoja |
| 63 | Matapi |
| 83 | Liana |
| 100 | Aguas turbias |
| 103 | Remolino |
| 123 | Barro |
| 143 | Lluvia |
| 160 | Cuia alta de 6 |
| 163 | Brote |
| 183 | Piraña |

## Observaciones para el dueño (ninguna bloquea)

1. **22 niveles con una cuia menos de lo que pide su tramo** (aviso `QA-CUIAS`, 03 §11).
   - En el 31-60 hay niveles de 8 cuias cuando el tramo pide 9-11: 31, 36, 37, 41, 42, 48, 51, 53-56 y 59.
   - En el 61-100 hay niveles de 9 cuias cuando pide 10-12: 61, 62, 73, 76, 81, 82, 88, 90, 94 y 96.
   - Se resuelven y cumplen su dificultad, pero quedan fuera del rango de 03 §11. **Decide el dueño:** si el rango es orientativo, se deja; si es obligatorio, se regeneran.
2. **Cuatro retos se publican sin etiqueta** (aviso `QA-WINDOW`): 66, 75, 166 y 189 están en posición de reto, pero miden un `bot_fail` de 0,675-0,725, por debajo del 0,75 de «Difícil». Por c.2 se publican con la etiqueta que miden (normal). El jugador los nota como retos sin aviso. ¿Se aceptan así o se piden más duros?
3. **`step_budget` en 85 (Liana), 95 (Matapi), 155 (Matapi con turbias), 192 (Piraña y Matapi) y 200 (Piraña).**
   - Los desbloqueos de esas mecánicas (600-900 ms) se encolan cuando se vierte cada ≈ 0,5 s, como hace el guion. El cliente acelera a ×0,35 y salta las últimas etapas, con `skip_from` C, D o E.
   - Al ritmo de una persona no salió en las tandas anteriores.
   - Conviene mirarlo jugando rápido a mano: si un jugador veloz ve saltos en la Liana o en el Matapi, quizá haya que subir el presupuesto o acortar esos pasos.
4. **Avisos menores de `QA-BAND-Q`:**
   - el 22 (recuperación) mide en la banda de comodidad;
   - el 100 (hito) mide en la de reto.
   Los dos cumplen el mínimo de su regla.
5. **Guardado:** el vertido que gana es síncrono por diseño (T-D) y llega a 7,88 ms (nivel 171), cerca del tope de 8 ms. El objetivo global se cumple (p95 2,96 ms), pero ese vertido es el que tiene menos margen.
