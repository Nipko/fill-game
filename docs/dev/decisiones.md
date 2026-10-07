# Registro de decisiones del producto

Fecha de creación: 2026-10-06 · Autor: tech-lead · Parte de `main` en `8854993` (prototipo v0 jugable; árbol limpio).

Cada decisión del dueño que cambia reglas, UX o contratos del GDD se registra aquí, en formato ADR breve: fecha, estado, contexto, decisión, consecuencias, alternativas descartadas y cómo revertirla. El GDD sigue siendo la fuente de verdad (CLAUDE.md, "Fuentes de verdad"): este registro **no** sustituye la edición de los capítulos. Cada decisión lleva su tarea de documentación (sección 3, T5), y mientras esa tarea no se fusione, ante una contradicción entre el GDD y este registro manda la decisión fechada del dueño. Las entradas no se borran: si una decisión se revierte, se añade otra que la sustituye.

| ID | Fecha | Título | Estado |
|---|---|---|---|
| D-001 | 2026-10-06 | Una dosis por vertido | Decidida por el dueño; implementada en el árbol de trabajo, sin commitear (T1-T3 y T5) y verificada en el SM-S938B el 2026-10-07 (T7: A7 y A8). Pendientes: commit y PR, T6 y las preguntas P2-P7 |
| D-002 | 2026-10-06 | Sin resaltado de destinos al seleccionar | Decidida por el dueño; implementada en el árbol de trabajo, sin commitear (T4 y T5) y verificada en el SM-S938B el 2026-10-07 (T7: B1-B2 y captura de B3). Pendientes: commit y PR y la revisión visual del dueño (B3) |
| D-003 | 2026-10-07 | Dirección visual «Selva brillante premium» | Decidida por el dueño; sin implementar. Pendientes: P8-P10, la fase visual (V1-V8 de `docs/reviews/2026-10-07/evaluacion-claude.md`) y la edición del GDD |

---

## D-001 · Una dosis por vertido

- **Fecha:** 2026-10-06.
- **Estado:** decidida por el dueño tras jugar el prototipo v0 en su teléfono (Samsung SM-S938B). Implementada y verificada en el teléfono el 2026-10-07, sin commitear (ver la tabla de arriba).

**Contexto.** El núcleo aplica la regla canónica del género, que es también la del GDD: un vertido A→B mueve `cantidad(A, B) = min(racha visible de A, espacio de B)` (02 §1.2, R9). Si la racha no cabe entera, se vierte lo que cabe y el resto queda en A ("vertido parcial"). En el teléfono, el dueño pidió que al pasar a otro recipiente se mueva una sola dosis cada vez, aunque haya varias seguidas del mismo color (mecánica tipo *ball sort*).

**Decisión.**
1. Cada vertido mueve **una sola dosis**: la de la cima de A, aunque debajo haya más del mismo color. La legalidad del vertido no cambia.
2. Implementación indicada en el encargo del 2026-10-06:
   - parámetro del núcleo `PourMode { Single, Run }` con valor por defecto `Single`;
   - clave de Remote Config `rules_pour_mode` = `"single"` | `"run"`, con default `"single"`, para probar el otro modo sin tocar código.

   La clave nueva tiene que entrar en la tabla única de 08 §9 (pregunta P1).
3. El modo queda fijado al cargar cada nivel y no cambia a mitad de un nivel, porque el historial de Marea y el par dependen de él.

**Consecuencias.**
- Desaparece el vertido parcial en modo `Single`: siempre se mueve una dosis. El sellado (R5), la victoria (R12) y la legalidad no cambian.
- Hacen falta más vertidos y más toques por nivel. En el tutorial, el par sube entre ×1,0 (nivel 1) y ×1,7 (nivel 6): 8 de los 10 pares quedan fuera de los rangos [P] de 02 §13 (sección 2.1.4).
- El solver explora más estados: entre ×1 y ×34 de estados expandidos en el tutorial (sección 2.1.3).
- La lección del nivel 2 ("vertido parcial") deja de tener sentido y hay que sustituirla (sección 2.1.5).
- Marea deshace una dosis por uso. Además, `restart_confirm_min_pours` (8) se alcanza antes y `lluvia_k_pours`/`pirana_k_pours` pesan más. Todo esto exige recalibrar antes de los niveles 11+ (fuera de v0; pregunta P6).
- Tintalú se separa de la regla canónica del género (research/01 §2 [V]; 01 tabla de principios). Debe constar en 02 "Desviaciones de la dirección" y en GDD.md §2 (decisión 2).

**Alternativas descartadas.**
- **Mantener la racha** (`Run`): es la regla de los líderes del género, pero el dueño la descartó tras jugar.
- **`Single` sin conservar `Run`:** se perderían la prueba A/B por Remote Config y la paridad con `tools/levelgen_proto.py`, que vierte rachas.
- **Verter la racha con un gesto aparte** (mantener pulsado o arrastrar): añade un gesto, choca con C9/C10 de 02 §3.1 y contradice "de una en una".

**Cómo revertir.**
- **Antes de que exista `AF.RemoteConfig`:** poner `GameConfig.RulesPourMode = "run"`, regenerar el pack del tutorial con `--pour-mode run` (el par cambia) y revertir los textos de T5. Además, en la misma tarea: cambiar `PourModes.Default` a `PourMode.Run` (lo usan el default de `--pour-mode` y `TutorialPackTests`, que comprueba el par del pack en ese modo) o ajustar los tests que fijan `single` como modo por defecto (`LevelFlowTests.RulesPourModeDefault_IsSingle_AndEveryLevelLoadsInIt`, `GamePlayModeTests`, `PourChainTests`, `DevProbeTests` y `LaunchOptionsTests`). Si no, EditMode y PlayMode quedan en rojo.
- **Con Remote Config:** `rules_pour_mode = "run"` se aplica desde la siguiente carga de nivel. Ojo: los packs llevan el par calculado en `Single`, así que la reversión definitiva exige regenerarlos.
- **En todos los casos:** una decisión nueva en este registro que sustituya a D-001.

---

## D-002 · Sin resaltado de destinos al seleccionar

- **Fecha:** 2026-10-06.
- **Estado:** decidida por el dueño. Implementada y verificada en el teléfono el 2026-10-07, sin commitear (ver la tabla de arriba).

**Contexto.** Al seleccionar un origen, v0 dibuja un aro marfil al 40 % sobre cada destino legal. Viene de 07 §4.3 (fila "Destino válido (resaltado)") y del storyboard de 07 §5.2 (t = 0,00: "destinos legales con aro marfil"). El dueño pidió que no se muestre a dónde verter: elegir bien es parte del juego.

**Decisión.**
- Al seleccionar una cuia **no** cambia ninguna otra: no se resalta ni se sugiere ningún destino.
- La selección solo eleva el origen (16 dp, 02 §4.1).
- Las ayudas que señalan algo son explícitas y quedan fuera de v0:
  - la pista por inactividad (02 §3.4) solo señala un origen;
  - Morpho (04 §2.3) muestra origen y destino porque el jugador lo pide.

**Consecuencias.**
- Habrá más toques rechazados (sacudida, C5-C6) y más Marea. En v0 Marea es ilimitada, así que no afecta a la economía.
- El rechazo y el cambio de selección (C5) siguen informando, pero solo **después** de elegir.
- El lector de pantalla sigue anunciando "açaí seleccionado: elige destino" (02 §12), porque no sugiere ningún destino.
- El dedo guía de los niveles 1-2 (02 §13) muestra un destino. Es una ayuda puntual del tutorial y queda fuera de v0 (pregunta P5).

**Alternativas descartadas.**
- **Resaltar solo en el tutorial:** el dueño pidió quitarlo, sin excepción.
- **Interruptor de accesibilidad "Mostrar destinos":** exigiría una clave nueva en 08 §9 y no se ha pedido. No se inventa.
- **Resaltar solo tras N rechazos:** sería una pista encubierta, y para eso ya existen las ayudas explícitas.

**Cómo revertir.** Revertir el commit de T4 y restaurar las dos frases de 07 §4.3 y §5.2. No hay clave de Remote Config: reactivarlo es otra decisión.

---

## Impacto técnico

### 1. Estado de partida

- Verificado hoy en `main` (`8854993`):
  - `git status` vacío;
  - `dotnet build dotnet/AmazonFill.slnx -warnaserror`: 0 avisos y 0 errores;
  - `dotnet test dotnet/AmazonFill.slnx`: 143/143;
  - `python tools/validate_schema.py`: 23/23 PASS, sin cambios en `tools/out/schema_example_08_4_1.json`.
- Medición exploratoria:
  - script de Python fuera del repositorio que reimplementa 02 §1 sin ocultas ni mecánicas, con cap 4 y `U_c` = 4;
  - en modo racha reproduce **exactamente** `tools/out/tutorial_v0_report.md`: par, estados alcanzables, estados sin salida, camino a Aguas estancadas y estados expandidos por A* de los niveles 1-9;
  - sus cifras en modo `Single` son, por tanto, fiables como estimación, pero las tareas T1 y T2 deben reproducirlas con `AF.Core`/`AF.Solver` (criterio A5).

### 2.1 D-001: una dosis por vertido

#### 2.1.1 Reglas de 02 §1 que cambian

| Regla (02 §1) | Hoy (`Run`) | Con D-001 (`Single`, por defecto) | Texto a cambiar (T5) |
|---|---|---|---|
| `legal(A, B)` (§1.2) | Pseudocódigo de §1.2 | **Sin cambios.** No depende de la cantidad | — |
| `cantidad(A, B)` (§1.2) | `min(len(racha_visible(A)), espacio_efectivo(B))` | **1** si `legal(A, B)` | `cantidad(A, B) = 1 si rules_pour_mode == single; min(...) si run` |
| R6 racha visible | Fija la cantidad | Se conserva como definición: la usan `Run`, el bot (03 §6), Remolino en `Run` y la excepción de ocultas (04 §5.1). En `Single` no fija la cantidad | Nota en R6 |
| R9 vertido y parcial | Mueve la racha; si no cabe, vertido parcial; 1 movimiento | Mueve **una** dosis de la cima de A; **el vertido parcial no existe**; 1 movimiento | Reescribir R9 con los dos modos y citar D-001 |
| R7 / R10 (a) revelado | La cima oculta se revela cuando sale la racha | Igual: se revela cuando sale la **última** dosis de la racha visible, uno o más vertidos después | Sin cambio de texto; aclaración en R10 |
| R5 sellado, R12 victoria | — | **Sin cambios** | — |
| R13-R15 bloqueo y modal | — | **Sin cambios de código** (sección 2.1.2) | — |
| R16 garantía de fútiles | "El estado canónico no cambia con un movimiento fútil, salvo casa correcta" | Ya no es literal en `Single`: ver 2.1.2 | Reescribir R16 |
| Remolino M4 (§9.1) | `cantidad = min(racha, espacio(pareja))` | 1 | Nota en §9.1 |
| Barro M5 | El primer vertido lava el barro (`espacio_efectivo + 1`) | Igual, con una dosis | — |
| Lluvia M6 / Piraña M7 | Tic cada `k` vertidos | Con más vertidos por nivel el tic llega antes respecto al progreso: recalibrar `lluvia_k_pours` y `pirana_k_pours` (rango 2-5) antes de los niveles 143 y 183 | Nota en §9.1 (fuera de v0) |

#### 2.1.2 Fútiles y bloqueo en modo `Single`

**Hecho clave:**
- `legal(A, B)` y `futil(A, B)` no dependen de `cantidad`. Por tanto, para un mismo estado S, `movimientos_utiles(S)` (salvo el campo `Amount`) y `atascado(S)` son **idénticos** en los dos modos.
- `PourRules.IsLegal`, `IsFutile`, `HasUsefulMove` e `IsStuck` no necesitan el modo ni cambian.
- Lo que cambia es **qué estados se alcanzan** y qué significa un vertido fútil.

**Ejemplos** (cuias de abajo arriba; cap 4, `U_c` = 4):

| # | Estado y vertido | `Run` | `Single` | ¿Fútil? |
|---|---|---|---|---|
| E1 | A = [c] (única dosis, monocolor) → B vacía | Resultado idéntico al de `Single` | B = [c], A vacía: estado canónico igual | Sí, en ambos. R16 se cumple literalmente |
| E2 | A = [c, c, c] → B vacía | Mueve 3 dosis: estado canónico igual | Mueve 1: A = [c, c], B = [c]. El estado canónico **cambia** y la dispersión de c sube 1 (retroceso) | Sí, en ambos. Con capacidades iguales partir una monocolor nunca acerca a la victoria. R16 debe reformularse |
| E3 | Cuia chica (cap 2) = [c, c] → cuia estándar vacía | Mueve 2: "casa correcta" | Mueve 1: primer vertido de los dos que llevan c a su casa | No, en ambos (excepción `cap_A ≠ U_c ∧ cap_B = U_c`) |
| E4 | A = [c, c, c] ("sellable") → B = [x, c] | Mueve 2: A = [c], B = [x, c, c, c] | Mueve 1: A = [c, c], B = [x, c, c] | No: B no está vacía. Es un vertido **regresivo** pero útil a efectos de R13, en ambos modos |
| E5 | A = [x, c, c] → B vacía | Mueve 2 | Mueve 1: A = [x, c], B = [c]; hace falta otro para destapar x | No: A no es monocolor |
| E6 | A = [c, c] y B = [c] | A→B o B→A, juntando en un vertido | A→B y B→A se alternan (ciclos de longitud 2) | No. Los ciclos no son Aguas estancadas en ningún modo (R14; caso 3 de research/01 §7) |

**Decisiones de diseño técnico:**
- **No se amplía `futil`.** Declarar fútiles los vertidos regresivos (E4) exigiría el solver para decidir y rompería R14 ("nunca anunciar bloqueo mientras exista un movimiento útil"). R13 debe seguir siendo exacto y barato: O(T²) sin asignaciones.
- **R16 reformulada (propuesta para T5):** "Un vertido fútil nunca es necesario para ganar. En `run` deja el estado canónico igual (salvo la rama casa correcta). En `single` puede además partir una cuia monocolor en dos (E2), lo que con capacidades iguales solo aumenta la dispersión. Por eso R13 nunca declara bloqueo en una posición con progreso posible."
- **Con capacidades uniformes (`cap_i = U_c` para todo i y c, como en todo v0), la regla de fútiles no cambia el veredicto de R13 en ningún modo.** Prueba:
  1. Si existe un vertido fútil, existe una cuia vacía E.
  2. Si hay alguna cuia no vacía, no sellada y no monocolor, su vertido a E es útil.
  3. Si todas las cuias no vacías y no selladas son monocolor y dos comparten color, verter de una a otra es útil.
  4. Si no, cada color está en una sola cuia, que contiene sus `U_c` = `cap` dosis: está llena y monocolor, luego sellada, y el nivel está ganado.

  Por tanto, `futile_rule_enabled` solo importa con la Cuia chica (debut en el 14), la Cuia alta (27) y la poda del solver.
- **Poda del solver en `Single`:** podar los fútiles no cambia el par en los 10 niveles del tutorial (BFS con y sin poda: el mismo par en 10/10). Los estados sin salida (0, 0, 0, 0, 0, 1, 0, 3, 4, 0) y los caminos más cortos a Aguas estancadas (nivel 6: 1 vertido; nivel 8: 2) coinciden con `Run`. T1 lo convierte en un test de propiedad sobre tableros aleatorios.

#### 2.1.3 Solver

- **Igual que hoy:**
  - representación y codificación (`Cuia.Packed`, `BoardCanon`, hash FNV-1a de 64 bits);
  - los pares (A, B) legales de cada estado, así que el **factor de ramificación por nodo es el mismo**.
- **Lo que cambia es la transición:** la cantidad pasa a ser 1.
- **Corrección a "mismo espacio de estados":** el universo de configuraciones es el mismo, pero el **conjunto alcanzable crece**:
  - todo vertido `Run` de k dosis equivale a k vertidos `Single` legales seguidos;
  - por tanto, alcanzables(`Run`) ⊆ alcanzables(`Single`) y "resoluble en `Run`" implica "resoluble en `Single`" (los 10 niveles del tutorial siguen siendo resolubles);
  - `Single` alcanza además estados con una racha partida entre dos cuias (E2, E5);
  - la inversa no está garantizada.
- **Heurística actual** `H = max(h1, h2)` (03 §5, `SolverHeuristic`):
  - sigue siendo **admisible y consistente** en `Single`, porque un vertido de una dosis elimina como mucho un corte y saca un color de como mucho una cuia;
  - A* con `w = 1` sigue dando el par exacto **sin cambiar el código**;
  - pero informa menos: una racha de 3 cuesta 3 vertidos y `h1` la cuenta como 1.
- **Heurística propuesta para `Single` [P]:** `H_single = max(Σ_i (n_i − fondo_i), h2)`, donde `fondo_i` es la longitud de la racha del fondo de la cuia i.
  - Es admisible: toda dosis por encima de la racha del fondo tiene que moverse al menos una vez, y cada vertido `Single` mueve exactamente una.
  - Es consistente: cada vertido cambia la suma en −1, 0 o +1.
  - Se comprobó en todos los estados alcanzables de los 10 niveles: `H_single ≤ distancia` en el 100 %.
- **Profundidad y coste** (A* `w = 1` con poda de fútiles; los estados expandidos en `Run` coinciden con el informe actual en los niveles 1-9):

| Nivel | Par `Run` | Par `Single` | Estados alcanzables `Run` → `Single` | Expandidos `Run` | Expandidos `Single` con H | Expandidos `Single` con `H_single` |
|---|---|---|---|---|---|---|
| 1 | 3 | 3 | 6 → 8 | 5 | 5 | 5 |
| 2 | 6 | 9 | 42 → 114 | 25 | 75 | 74 |
| 3 | 7 | 10 | 109 → 170 | 37 | 108 | 101 |
| 4 | 8 | 12 | 179 → 375 | 52 | 251 | 222 |
| 5 | 10 | 16 | 481 → 805 | 157 | 744 | 692 |
| 6 | 10 | 17 | 36 → 72 | 25 | 55 | 40 |
| 7 | 11 | 15 | 563 → 922 | 19 | 641 | 477 |
| 8 | 13 | 17 | 742 → 1.478 | 319 | 1.056 | 947 |
| 9 | 14 | 16 | 1.361 → 1.961 | 435 | 937 | 462 |
| 10 | 16 | 19 | 2.479 → 3.634 | 78 | 2.488 | 1.176 |

- **Presupuestos:** en el tutorial no hay riesgo (≤ 2.488 expandidos frente a 300.000 del pipeline y 40.000 de `hint_budget_nodes`). En campaña (K de 8 a 14) hay que medir con `tools/solver-bench` (aún no existe) antes de 1.0 el impacto en Morpho (<50 ms), Pororoca y el pipeline (riesgo R-1).
- **Pipeline de 1.0** (fuera de v0; todavía no portado a C#; pregunta P6):
  - 03 §4.1-§4.2 (`MOVIMIENTOS_INVERSOS`): en `Single` m = 1, y se omite la condición "b no vacía y top(b) == c: continuar", porque el vertido directo mueve siempre exactamente 1. A cambio, el origen del vertido directo debe quedar con c arriba o vacío (r ≥ 2 o len(a) = 1) y b no puede quedar sellada.
  - 03 §5 `MOVIMIENTOS`: "cantidad 1".
  - 03 §6 bot: `puntos = n` y la penalización "vertido parcial" pierden sentido. Hay que redefinir la puntuación y recalibrar los umbrales de etiquetas (Difícil ≥ 75 %, Muy difícil ≥ 90 %).
  - 03 §4.3 y QA #2 (`par ≥ 2K`) y el índice D (`par − 2K`): las cifras [V-proto] de 03 §18 se midieron en `Run`.

#### 2.1.4 Par de cada nivel del tutorial

El par se recalcula en `Single` con el mismo solver y el mismo BFS. Comparación con los rangos [P] de 02 §13 y con los toques de la solución óptima (2 por vertido):

| Nivel | Par `Run` (pack actual) | Par `Single` | Rango [P] de 02 §13 | ¿Dentro? | Toques `Run` → `Single` |
|---|---|---|---|---|---|
| 1 | 3 | 3 | 3-3 | Sí | 6 → 6 |
| 2 | 6 | 9 | 4-6 | No | 12 → 18 |
| 3 | 7 | 10 | 5-7 | No | 14 → 20 |
| 4 | 8 | 12 | 7-9 | No | 16 → 24 |
| 5 | 10 | 16 | 9-11 | No | 20 → 32 |
| 6 | 10 | 17 | 10-13 | No | 20 → 34 |
| 7 | 11 | 15 | 10-13 | No | 22 → 30 |
| 8 | 13 | 17 | 12-15 | No | 26 → 34 |
| 9 | 14 | 16 | 13-16 | Sí | 28 → 32 |
| 10 | 16 | 19 | 14-18 | No | 32 → 38 |

Lecturas:
- La comprobación "par dentro del rango [P] de 02 §13" del generador fallaría en 8 de los 10 niveles.
- La rampa deja de ser monótona: 6 (17) > 7 (15) y 8 (17) > 9 (16).
- Los tiempos objetivo [P] de 02 §13 (30-120 s) suben con los toques.
- Hay que elegir entre dos caminos (pregunta P2): adoptar como rango los pares medidos o rediseñar los niveles 2-10 para una rampa monótona.

#### 2.1.5 Lecciones del tutorial (02 §13)

| Nivel | Lección actual | Con D-001 | Sustitución propuesta [P] |
|---|---|---|---|
| 1 | Estado literal; "termina la cuia casi llena antes de apartar"; dedo guía | Sigue valiendo (par 3 en ambos modos) | — |
| 2 | **"Vertido parcial con dedo guía: una racha de 3 sobre un hueco de 2"**; dos vacíos ("reserva uno") | **Deja de tener sentido:** no hay parcial | Lección "una dosis por vertido": la solución óptima en `Single` del tablero actual ya contiene **3 vertidos seguidos 2→4** (la racha de 3 açaí pasada dosis a dosis). Texto ≤ 2 líneas [P]: "Cada vertido pasa una sola dosis: repite para mover las demás." (pregunta P3). Se mantiene "reserva uno" |
| 3 | Marea la primera vez que un vertido aleja de la solución (distancia del solver +2) | Sigue valiendo. Vertidos útiles iniciales que no acercan: 2 de 6 en `Single` (4 de 6 en `Run`); la comprobación (≥ 1) pasa | — (fuera de v0: tooltips) |
| 5 | Color repartido en 3 cuias | Propiedad del tablero: no cambia | — |
| 6 | Un vacío; Aguas estancadas alcanzables | Estancadas en 1 vertido en ambos modos | — |
| 8 | Reinicio probable con 2 vacíos | Estancadas en 2 vertidos en ambos modos | — |
| 4, 7, 9, 10 | Debuts y rampa | Solo el par (2.1.4) | — |

En el generador (T2):
- la comprobación "nivel 2: vertido parcial de una racha de 3 sobre un hueco de 2" se aplica solo en `Run`;
- en `Single` la sustituye "nivel 2: la solución óptima contiene ≥ 3 vertidos seguidos con el mismo origen y destino".

En Unity (T3), `TutorialPackTests.Level2_OffersThePartialPourOfThreeOntoTwo` se sustituye por la comprobación equivalente.

#### 2.1.6 Encolado de toques y animación

**Modelo** (02 §3.2):
- no cambia: el toque se evalúa contra el modelo y `TryPour` aplica R9-R10 al instante;
- `queue_max_lag_pours` sigue en 2;
- `TapController` no cambia de lógica.

**C4** dice hoy: "si quedan dosis del mismo color en X tras un parcial, X no sigue seleccionada". En `Single` ese caso se vuelve el habitual. Se mantiene el comportamiento (el origen se deselecciona) y solo se reescribe el texto: "tras el vertido X no sigue seleccionada, aunque le queden dosis del mismo color". La alternativa de la selección pegajosa queda como pregunta P4.

**Cola visual:**
- La solución óptima en `Single` encadena vertidos idénticos: 7 en el nivel 6 y 3 en el 2.
- Un vertido de 1 dosis dura unos 660 ms: 120 de elevación + 200 de traslado + 180 de chorro + 160 de retorno.
- Un jugador rápido (un toque cada 300 ms) encola el tercer A→B cuando A tiene ya 2 trabajos pendientes, y `EnforceLag` hace saltar el más antiguo a su fotograma final (salto visible).

**Propuesta [P] para `BoardAnimator`, el "encadenado":**
- Si llega un vertido con el mismo (A, B) que el que está en curso y la cuia origen aún no ha empezado el retorno, se alarga el chorro `anim_pour_dose_ms` (100 ms) en lugar de encolar retorno, elevación y traslado.
- Tres dosis costarían entonces 120 + 200 + 3·100 + 80 + 160 = 860 ms, lo mismo que hoy un vertido `Run` de 3.
- No cambia claves ni defaults.

**Chorro:** siempre 1 dosis, 180 ms (`anim_pour_dose_ms` + `anim_pour_tail_ms`).

**Marea (04 §2.1):**
- deshace una dosis por uso;
- es ilimitada en los niveles 1-10, así que no afecta a v0;
- desde el 11 "3 por nivel" equivale a 3 dosis: recalibrar antes (pregunta P6);
- `restart_confirm_min_pours` (8; fallo blando, 04 §5.2) también.

**`DevProbe`:**
- en `AF_STATE` el tercer elemento de `move` vale siempre 1 en `Single`;
- `pours` cuenta dosis movidas;
- se propone añadir `"pour_mode":"single"` al evento `load`.

**Guardado a mitad de nivel** (R8; fuera de v0): el guardado debe incluir el modo; un cambio de Remote Config solo se aplica en la siguiente carga de nivel.

#### 2.1.7 Configuración y formato

- **08 §9:**
  - nueva fila `rules_pour_mode` | string | `"single"` | `single` · `run` | 02 | "Cantidad por vertido (D-001): una dosis o la racha visible (R9)";
  - la regla de nombres añade la familia `rules_` (variantes de reglas del núcleo, dueño 02);
  - `futile_rule_enabled` conserva su nombre.
- **v0 no tiene `AF.RemoteConfig`** (sdk-integrator; Firebase pendiente):
  - `GameConfig.RulesPourMode = "single"`, con un parser que convierte un valor desconocido en `single` con aviso en el log;
  - para probar `run` sin tocar código antes de que exista Remote Config, se propone [P] el extra de intent de desarrollo `af_rules_pour_mode` en `LaunchOptions`, solo en builds de desarrollo, igual que `af_level`.
- **Pack del tutorial:**
  - el par depende del modo;
  - en v0 **no se cambia el formato** (el subconjunto de `af-level/1` no tiene ese campo);
  - el generador recibe `--pour-mode single|run` (default `single`) y el informe declara el modo y lista también el par `Run` como referencia;
  - para 1.0, que el pack declare su modo es un cambio de contrato (`pack_schema` 2): pregunta P7.

### 2.2 D-002: sin resaltado de destinos

**Qué lo resalta hoy** (búsqueda en `Assets/_Project/Scripts`):

| Archivo | Líneas | Qué es | Acción |
|---|---|---|---|
| `Scripts/UI/GameScreen.cs` | 575-584 | `HighlightTargets(LevelFlow, int source)`: recorre las cuias, consulta `PourRules.IsLegal(state, source, i)` y llama a `SetTargetHighlight` | **Quitar el método** |
| `Scripts/UI/GameScreen.cs` | 350, 356, 367, 372, 419, 428 | Llamadas a `HighlightTargets` en `OnTap` (Selected, Deselected, Rejected, Poured), `OnWin` y `OnStuck` | **Quitar las 6 llamadas** |
| `Scripts/UI/GameScreen.cs` | 571 | `_views[i].SetTargetHighlight(false)` en `ClearLifts` | **Quitar la línea** |
| `Scripts/Presentation/CuiaView.cs` | 50 | Campo `_targetRing` | **Quitar** |
| `Scripts/Presentation/CuiaView.cs` | 512-514 | Creación de la imagen `TargetRing` en la construcción de la vista | **Quitar** |
| `Scripts/Presentation/CuiaView.cs` | 158-159 | Posición y tamaño de `_targetRing` en el layout | **Quitar** |
| `Scripts/Presentation/CuiaView.cs` | 258-265 | `SetTargetHighlight(bool)` (comentario "07 §4.3") | **Quitar** |

**Se conserva:**
- `RingColor` y `ProceduralArt.Ring`, que usa el aro de sellado `SealRing` (`_glow`, 07 §5.3, y la celebración, 07 §5.4);
- la elevación del origen en el *touch-down* (`OnCuiaDown`, que solo consulta `CanBeSource`: C1-C2);
- la sacudida de rechazo.

`DevProbe` (`AF_LAYOUT` y `AF_STATE`) no expone resaltados: no cambia.

**Texto del GDD:**
- 02 §3-§4 **no** menciona hoy el resaltado de destinos: el resaltado vive en 07. Para dejar la decisión explícita en el capítulo de reglas, T5 añade:
  - 02 §3.1, C1: "X se selecciona y se eleva; ninguna otra cuia cambia: no se resaltan ni se sugieren destinos (D-002)";
  - 02 §4.1, fila Selección: "…; el resto del tablero no cambia (sin resaltado de destinos)".
- 02 §3.4 ya dice que la pista "no muestra el destino" y 02 §12 ("elige destino") no sugiere ninguno: no cambian.
- 07 §4.3: se borra la fila "Destino válido (resaltado)".
- 07 §5.2, fila t = 0,00: se borra "; destinos legales con aro marfil".

---

## 3. Tareas por agente

**Orden y paralelismo:**
- Sin dependencias, en paralelo: T1 (C# puro, Editor solo para verificar), T4 (Editor) y T5 (documentación).
- Cuando T1 esté fusionada:
  - T2 (sin Editor);
  - T3 (Editor; empieza al terminar T4, para no superar 2 Editores).
- **T2 y T3 se fusionan en lote, una detrás de otra, con EditMode tras las dos.** Por separado, `main` queda en rojo: el pack nuevo trae el par `Single` y los tests de Unity resuelven con el modo que fija T3.
- T6 y T7 van al final.
- Ninguna tarea comparte archivos con otra que corra a la vez. T6 retoca archivos de T1, pero solo después de fusionar T1-T4.
- Cada tarea pasa por code-reviewer.

| # | Agente | Archivos (y solo esos) | Depende de | Qué hace |
|---|---|---|---|---|
| T1 | core-dev | `Packages/com.amazonfill.core/Runtime/AF.Core/Rules/{PourMode.cs (nuevo), CuiaRules.cs, PourRules.cs}`, `Runtime/AF.Core/Session/LevelSession.cs`, `Runtime/AF.Solver/Search/{SolverOptions.cs, AStarSolver.cs, SolverHeuristic.cs}`, `Tests/Editor/AF.Core.Tests/PourModeTests.cs` (nuevo), `Tests/Editor/AF.Solver.Tests/AStarSolverPourModeTests.cs` (nuevo), `dotnet/AF.Core.Tests/DotnetOnly/{PourModePropertyTests.cs (nuevo), HotLoopAllocationTests.cs}`, `README.md`, `CHANGELOG.md`, y los `.meta` que genere Unity | — | Ver "API de T1" |
| T2 | level-pipeline-dev | `tools/level-generator/{Program.cs, Handmade/*.cs, specs/tutorial_v0.json}` y, generados, `Assets/_Project/Data/Levels/tutorial_v0.json` y `tools/out/tutorial_v0_report.md` | T1; P2 y P3 | `--pour-mode single\|run` (default `PourModes.Default`); `StateGraph` y A* con el modo; informe con el modo, par `Single` y par `Run`; comprobación del nivel 2 según el modo (2.1.5); rangos de par según P2 (si se rediseña, nuevos tableros 2-10 y rangos propuestos como [P] en el PR) |
| T3 | unity-gameplay-dev | `Scripts/GameFlow/{GameConfig.cs, LevelFlow.cs, TapController.cs}` (en TapController solo el comentario de C4), `Scripts/UI/{LaunchOptions.cs, DevProbe.cs}`, `Scripts/Presentation/BoardAnimator.cs`, `Tests/EditMode/{TutorialPackTests.cs, LevelFlowTests.cs, TapControllerTests.cs, TestPacks.cs}`, `Tests/PlayMode/{GamePlayModeTests.cs, VisualSnapshotTests.cs}`, `Tests/PlayMode/PourChainTests.cs` (nuevo) | T1 (en lote con T2) | `GameConfig.RulesPourMode = "single"` y su parser; `LevelSession(..., mode)`; extra de intent `af_rules_pour_mode` [P]; `"pour_mode"` en `AF_STATE load`; encadenado de vertidos iguales [P]; tests con `SolverOptions.ForPipeline(mode de la sesión)`; sustituir el test del parcial del nivel 2 |
| T4 | unity-gameplay-dev | `Scripts/UI/GameScreen.cs`, `Scripts/Presentation/CuiaView.cs`, `Tests/PlayMode/SelectionShowsNoTargetsTests.cs` (nuevo) | — | Quitar lo de la tabla 2.2 y test PlayMode |
| T5 | Documentación (sesión principal, con aprobación del dueño por tocar el GDD y un contrato) | `docs/gdd/02-nucleo-reglas-ux.md` (§1.2, R6, R9, R10, R16, §3.1 C1 y C4, §4.1, §9.1 notas, §13 fila 2 y rangos, Parámetros, Desviaciones), `docs/gdd/07-arte-tematica-audio.md` (§4.3, §5.2), `docs/gdd/08-arquitectura-tecnica-publicacion.md` (§9: fila y regla de nombres), `docs/gdd/03-niveles-generacion-dificultad.md` (notas en §4.1, §5 y §6: "en `single`…", sin recalibrar), `docs/gdd/01-vision-posicionamiento.md` (principio 1), `docs/GDD.md` (§1 "Qué es", §2 decisiones 2 y 3), `docs/dev/prototipo-v0.md` (b y e), `docs/dev/decisiones.md` (estado) | P1-P5 respondidas | Llevar D-001 y D-002 al canon |
| T6 | core-dev | Los archivos de `Runtime/` de T1 | T1-T4 fusionadas | Retirar las sobrecargas heredadas sin modo y `SolverOptions.Pipeline` (que hoy equivalen a `Run`), para que nadie mezcle modos sin querer |
| T7 | Sesión principal, en `main` | Ninguno (verificación) | T2-T4 | Build de desarrollo y verificación en el teléfono (criterios A7, A8, B3) |

**API de T1** (propuesta; core-dev puede ajustar nombres justificándolo):

```csharp
// AF.Core.Rules: default(PourMode) == Single (D-001)
public enum PourMode { Single = 0, Run = 1 }
public static class PourModes {
  public const PourMode Default = PourMode.Single;
  public static bool TryParse(string? key, out PourMode mode);   // "single" | "run", ordinal, sin cultura
  public static string ToKey(PourMode mode);
}
static int       PourRules.Amount(BoardState s, int a, int b, PourMode mode);
static BoardState PourRules.Apply(BoardState s, int a, int b, PourMode mode);
static int       PourRules.CollectUsefulMoves(BoardState s, bool futileRuleEnabled, PourMode mode, Pour[] buf);
// IsLegal, IsFutile, HasUsefulMove e IsStuck NO cambian (2.1.2)
LevelSession(BoardState initial, bool futileRuleEnabled, PourMode pourMode);   // + propiedad PourMode
SolverOptions(int nodeBudget, int weight, bool futileRuleEnabled, PourMode pourMode);
static SolverOptions SolverOptions.ForPipeline(PourMode mode);
```

Las firmas actuales sin modo se mantienen **sin cambio de comportamiento** (equivalen a `Run`), para que `tools/` y Unity sigan compilando entre fusiones. Se documentan como heredadas y las retira T6. `SolverParityTests`, la paridad con el prototipo de Python, que vierte rachas, sigue igual. `H_single` se usa solo con `PourMode.Single`. Sin asignaciones en el bucle caliente (`.claude/rules/core.md`).

## 4. Criterios de aceptación medibles

**D-001**

| # | Criterio | Cómo se mide |
|---|---|---|
| A1 | `PourRules.Amount(s, a, b, PourMode.Single) == 1` para todo par legal y 0 para todo par ilegal; con `Run`, el resultado de hoy | `PourModeTests` sobre los 10 tableros del tutorial y ≥ 200 tableros aleatorios con T ≤ 6 (PRNG propio, semilla fija); los 143 tests actuales siguen en verde sin tocarlos |
| A2 | Los pares (A, B) de `CollectUsefulMoves` son idénticos en los dos modos; `Amount` = 1 en `Single` | Test de propiedad en `DotnetOnly`, en el mismo corpus |
| A3 | Descomposición: un vertido `Run` de k dosis equivale a k vertidos `Single` legales y seguidos que llegan al mismo estado | Test de propiedad en el mismo corpus |
| A4 | Con `Single`: par de A* (w = 1) = par del BFS exhaustivo, con y sin poda de fútiles; `H_single ≤ distancia` en todos los estados alcanzables; 0 bytes asignados en el bucle caliente | `AStarSolverPourModeTests`, `PourModePropertyTests` y `HotLoopAllocationTests` ampliado |
| A5 | `level-generator --handmade` (por defecto `single`) da todas las comprobaciones en PASS; dos ejecuciones dan el mismo SHA-256; el informe declara "modo de vertido: single" y, con los tableros actuales, reproduce los pares `Single` de 2.1.4 (3, 9, 10, 12, 16, 17, 15, 17, 16, 19) y los estados alcanzables | Comando de prototipo-v0 b, dos veces, más `sha256sum` |
| A6 | `GameConfig.RulesPourMode == "single"`; `LevelFlow` crea la sesión en `Single`; `TutorialPackTests`: par del pack = par A* en `Single`, la solución gana con `pours` = par, y el nivel 2 tiene ≥ 3 vertidos iguales seguidos | EditMode en verde |
| A7 | En el teléfono (build de desarrollo): todo `AF_STATE` con `"event":"pour"` tiene `move[2] == 1`; cada nivel 1-10 jugado con la solución del informe termina en `win` con `pours` = par `Single`; el evento `load` trae `"pour_mode":"single"` | `adb logcat -d -s Unity \| grep AF_STATE` y un script que cuenta los `move` con tercer valor distinto de 1 (debe dar 0) |
| A8 | Con `adb shell am start ... --es af_rules_pour_mode run`, el primer vertido de una racha mueve más de una dosis (nivel 2, vertido 0→3: `move[2] == 2`) | `AF_STATE` en logcat |
| A9 [P] | Encadenado: tres vertidos A→B tocados cada 300 ms dejan la vista en reposo ≤ 1.200 ms después del último toque, sin saltos de fotograma (contador de saltos de `BoardAnimator` = 0) | `PourChainTests` (PlayMode) |

**D-002**

| # | Criterio | Cómo se mide |
|---|---|---|
| B1 | Tras seleccionar un origen, ninguna otra cuia cambia: no existe ningún objeto `TargetRing` en la jerarquía y solo la cuia seleccionada tiene `IsLifted` | `SelectionShowsNoTargetsTests` (PlayMode), en los niveles 2 y 7 con al menos dos destinos legales |
| B2 | `HighlightTargets`, `SetTargetHighlight` y `TargetRing` no aparecen en `Assets/_Project/Scripts` | `grep -rn "HighlightTargets\|SetTargetHighlight\|TargetRing" Assets/_Project/Scripts` sin resultados |
| B3 | La captura con una cuia seleccionada muestra solo el origen elevado, sin aros | `VisualSnapshotTests` (explícito): `Logs/snapshots/03_selected_l7.png`; en el teléfono, `adb exec-out screencap -p > Logs/device/select_l7.png` tras tocar un origen; revisión visual del dueño |
| B4 | El aro de sellado y la celebración siguen funcionando | PlayMode en verde (`Level1_SolverSolution_ReachesVictory_AndNextOpensLevel2`) |

**Verificación obligatoria de cada tarea** (CLAUDE.md):
- T1: `dotnet build dotnet/AmazonFill.slnx -warnaserror` y `dotnet test` (143 + nuevos); `scripts/unity-test.ps1 -Mode EditMode` (genera los `.meta` de los archivos nuevos del paquete, que se commitean tal cual).
- T2: lo anterior más `python tools/validate_schema.py` (23/23) y el generador dos veces.
- T3 y T4: `scripts/unity-compile.ps1`, `unity-test.ps1 -Mode EditMode` y `-Mode PlayMode`.
- T7: el build de desarrollo de CLAUDE.md, `adb install -r` y A7, A8 y B3. Si `adb devices` sale vacío, se pide al dueño que reactive la depuración inalámbrica.

## 5. Preguntas para el dueño (cerradas)

| # | Pregunta | Recomendación | Bloquea |
|---|---|---|---|
| P1 | ¿Confirmas la clave `rules_pour_mode` (`"single"` \| `"run"`, default `"single"`) en la tabla única de 08 §9, con la nueva familia de prefijo `rules_`, y que `Run` quede solo como opción de prueba? | Sí | T5 (08 §9) |
| P2 | Par del tutorial en `Single`: ¿(A) mantener los 10 tableros y fijar como rango [P] su par medido (3, 9, 10, 12, 16, 17, 15, 17, 16, 19), aceptando la rampa no monótona, o (B) rediseñar los niveles 2-10 para una rampa monótona con rangos que propone level-pipeline-dev en el PR? | B | T2 |
| P3 | Nivel 2: ¿sustituir la lección del vertido parcial por "una dosis por vertido" con el texto [P] "Cada vertido pasa una sola dosis: repite para mover las demás."? | Sí | T2, T5 |
| P4 | Tras verter, ¿el origen se deselecciona (C4 actual, estilo *ball sort*) o sigue seleccionado si puede volver a verter al mismo destino (selección pegajosa, menos toques)? | Deseleccionar (C4 actual) | T3 (solo si cambia) |
| P5 | El dedo guía de los niveles 1-2 (02 §13) y Morpho (04 §2.3) muestran un destino: ¿se mantienen como ayudas explícitas compatibles con D-002? | Sí | T5 |
| P6 | ¿Una dosis por vertido es también la regla de 1.0? Si es así, antes de la Fase 1 hay que recalibrar en `Single`: las cifras [V-proto] de 03 (`par ≥ 2K`, bot ×40 y etiquetas, índice D), Marea "3 por nivel", `restart_confirm_min_pours`, `lluvia_k_pours` y `pirana_k_pours` | Sí, con una tarea de recalibración aparte | Fase 1 (no v0) |
| P7 | En 1.0, ¿el pack declara el modo con el que se calculó el par (cambio de contrato: `af-level/2`, `pack_schema` 2)? | Sí | `schemas/` (no v0) |

## 6. Riesgos

| # | Riesgo | Mitigación |
|---|---|---|
| R-1 | Presupuesto del solver en `Single` con K de 8 a 14: entre ×1 y ×34 de estados expandidos en el tutorial (nivel 7: 19 → 641); Morpho debe quedar < 50 ms con 40.000 nodos | `H_single` (hasta ×2 menos en el tutorial); medir con `tools/solver-bench` (aún no existe; tarea de core-dev o level-pipeline-dev) antes de 1.0 |
| R-2 | Mezclar modos: un pack con par `Single` jugado en `Run` (o al revés) da pares falsos en QA y en automatización | Modo en el informe y en `AF_STATE load`; el modo solo cambia al cargar un nivel; T6 retira las firmas sin modo |
| R-3 | `main` en rojo entre T2 y T3 | Fusión en lote y EditMode tras las dos |
| R-4 | Saltos visibles en la cola con toques rápidos (3 o más vertidos iguales) | Encadenado (A9) |
| R-5 | Más rechazos y frustración sin resaltado, sobre todo con 6-7 cuias | Marea ilimitada hasta el 10; vigilar la tasa de `reject` por nivel en `AF_STATE` durante la prueba del dueño |
| R-6 | Desviación del canon del género (research/01 §2): las cifras de investigación y de prototipo se midieron con rachas | P6; registrar la desviación en 02 y GDD.md |

---

## D-003 · Dirección visual «Selva brillante premium»

- **Fecha:** 2026-10-07.
- **Estado:** decidida por el dueño tras ver el prototipo v0.1 (`ef676a4`) y el concepto «Orillas vivas» de la revisión del 2026-10-07. Sin implementar. Hay dos puntos pendientes de respuesta: la frontera exacta con el trade dress de los líderes (P8) y los umbrales de contraste (P9).

**Contexto.**
- **El canon vigente:**
  - 07 (Resumen y §1.1) fija el estilo «acuarela botánica sobre vidrio»: fondos desaturados y oscuros (S ≤ 35 %, V ≤ 55 %, 07 §1.2), paneles de papel marfil con grano y botones planos, sin bisel ni degradado (07 §7.2-7.3).
  - 07 §1.3 sustituye la «saturación alta y luz cálida “de cuento”» por esos fondos desaturados.
  - El dueño lo confirmó el 2026-10-06 (GDD.md §2, decisión 23, y §6.1, pregunta 12).
- **El prototipo v0.1** lo aproxima con arte procedural provisional (`ProceduralArt.cs`) y la fuente `LegacyRuntime.ttf`.
- **La revisión del 2026-10-07** (`docs/reviews/2026-10-07/`, hecha por otra IA a petición del dueño) propuso «Orillas vivas»: gouache, papel crema y cuaderno botánico, con el tablero sobre un muelle.
- **Al dueño no le gustan ni el aspecto actual ni ese concepto:** «no parece un juego sino algo como libro». Pide un enfoque «más de juego, con texturas», al nivel de los juegos más populares del género.
- Evaluación de la revisión: `docs/reviews/2026-10-07/evaluacion-claude.md`.

**Decisión.**
1. **La dirección visual es «Selva brillante premium».**
   - El nivel de acabado de los líderes del género: vidrio con brillos y reflejos, colores saturados, botones con volumen, luz cálida, texturas y partículas.
   - Con identidad amazónica propia: madera tallada, hojas, agua de río, frutas del Estuario y la fauna de 07 §2.2.
2. **Sustituye a «acuarela botánica sobre vidrio»** (07, Resumen y §1.1; GDD.md §2, decisión 23, y §6.1, pregunta 12).
3. **Se descarta la estética de papel, gouache y cuaderno de «Orillas vivas»**, incluida su variante B. Se conserva su composición útil:
   - una ventana al río en la franja superior;
   - una repisa o muelle de madera por fila, con sombra de contacto;
   - el bloque de cuias centrado en el área del tablero, no anclado abajo;
   - la bandeja de comodines abajo.
4. **Tipografía:** Baloo 2 (ExtraBold y Bold) para títulos y Nunito (ExtraBold, Bold y Regular) para el texto. Las dos tienen licencia SIL OFL, el dueño las aprobó y ya están copiadas en `Assets/_Project/Art/Fonts/`. Nunito SemiBold (07 §7.1) se sustituye por Bold.
5. **No cambian:**
   - D-001 y D-002: ningún brillo, partícula ni animación varía con la selección ni con la legalidad de un destino.
   - La cuia-garrafa, su cavidad recta, las alturas iguales por dosis, las marcas y el menisco (07 §4).
   - La lista cerrada de trade dress prohibido de 07 §1.3: gatos, caldero, oro biselado, gemas, cintas rojas, calavera y silueta de frasco con labio.
   - Sin personas ni aldeas identificables (07 §8.1).
   - El Estuario como primer mundo con sus cinco tintes, y la paleta, los HEX y los patrones de 07 §3.
   - Las posiciones del HUD de 07 §7.4.
   - Como mucho 60 partículas en gama baja, texto con contraste ≥ 4,5:1 y controles ≥ 48 dp (07 §1.2 y §7.8).
   - Sin Bloom del 2D Renderer (UUM-150232): los brillos se hacen con sprites aditivos o shaders de UI.

**Consecuencias.**
- **Hay que reescribir 07 §1-§7:**
  - el Resumen;
  - §1.1: sale el papel; entran el vidrio brillante, la madera tallada, la hoja y la piedra;
  - §1.2: se retiran el límite de saturación del fondo, el grano de papel y la regla de L*;
  - §1.3: las filas de botones y de «saturación alta y luz cálida», con la frontera que fije P8;
  - §2: pinturas base y variantes;
  - §4.2: brillo y cavidad aislada;
  - §5: partículas y brillos de los storyboards;
  - §6: estilo de Capi y Rosa;
  - §7.1-7.3: contorno de texto, tokens sin papel marfil y botones con volumen.

  Fuera de 07 §1-§7 también cambian:
  - en 07: §10.2 (capturas «en papel marfil»), §11, las «Preguntas abiertas» 1 y 7 y las «Desviaciones de la dirección»;
  - en GDD.md: §2 (decisión 23), §6.1 (pregunta 12) y §9 (paso 11, el brief al artista «acuarela»);
  - 02 §12: «fondos desaturados y oscuros por diseño».

  La revisión listaba solo 07 §§1, 2, 4, 7, 9 y 11, y se quedaba corta. Lo edita la sesión principal en un lote de documentación aprobado por el dueño, después de que este acepte las capturas de la fase visual (P10).
- **La regla de contraste de L* se sustituye.**
  - Comprobado el 2026-10-07 con los L* de 07 §3: ningún fondo uniforme cumple «cada tinte ≥ 25 puntos de L* respecto al fondo». El barrido de 0 a 100 en pasos de 0,1 no da ningún candidato.
  - Con los cinco tintes del Estuario solo valdría L* ≤ 7, un fondo casi negro, lo contrario de esta dirección.
  - En su lugar se mide el resultado compuesto, en espacio lineal y sobre el fondo real que hay detrás del tablero. Umbrales [P], pendientes de P9:
    1. cada tinte del mundo frente a la cavidad vacía: ΔL* ≥ 15;
    2. el contorno de la cuia frente al fondo local: contraste ≥ 3:1 (WCAG 2.1, criterio 1.4.11, componentes no textuales);
    3. la cavidad vacía frente a la Guanábana: ΔL* ≥ 50;
    4. las dosis vecinas de un par de riesgo se siguen distinguiendo por patrón e icono (07 §3), no por L*.
  - Para cumplir 1 y 3 sobre un fondo claro, la cavidad pasa a ser oscura y casi opaca, independiente del fondo:
    - hoy es translúcida, con alfa 0,62 (`ProceduralArt.cs:322`);
    - opaca, su color actual da L* ≈ 12, es decir, ΔL* 20 con el Açaí (32) y 86 con la Guanábana (98).
  - Lo comprueba un test EditMode con los cinco tintes del Estuario.
- **El fondo deja de estar limitado a S ≤ 35 % y V ≤ 55 %.** Fuera de las cuias, el cielo, los bordes y la vegetación pueden ser saturados y luminosos. Detrás de las cuias, el valor lo controlan la repisa o un panel.
- **Código afectado en la fase visual** (unity-gameplay-dev):
  - `BoardLayout.cs` y `BoardLayoutTests.cs`;
  - `GameScreen.cs`: río, repisas y `ShiftToClear`;
  - `ProceduralArt.cs`: fondo, río, vidrio, botones y cavidad;
  - `UiBuilder.cs`: fuentes y estado deshabilitado;
  - `CuiaView.cs`: brillos;
  - `VisualSnapshotTests.cs`.

  Sin paquetes nuevos: las fuentes se cargan como `Font` o con TextMeshPro, que viene con uGUI en Unity 6, importando sus recursos esenciales por script. Ningún `.unity`, `.prefab`, `.mat` ni `.asset` se edita a mano.
- **El arte final sigue siendo del artista** (CLAUDE.md, «Humanos»). El procedural de los agentes es provisional y servirá de referencia para el brief nuevo.
- **Riesgo:** el brillo y la saturación pueden competir con los tintes. Se mitiga con las comprobaciones compuestas y con capturas antes y después de los niveles 1 y 7 en el mismo estado (`VisualSnapshotTests`), además de capturas de estrés de layout con 14 cuias.
- **Después de lo visual** (registrado, sin implementar): la fase de jugabilidad, recursos y detalle de cada elemento, con el backlog priorizado de la §3 de `docs/reviews/2026-10-07/evaluacion-claude.md`.

**Alternativas descartadas.**
- **Mantener la acuarela del GDD:** es lo que el dueño ve como «un libro».
- **«Orillas vivas» completa** (gouache, papel, cuaderno): el dueño la rechazó.
- **Vector plano** (la otra opción de la pregunta 12): no da la sensación de juego que se pide.
- **Copiar el acabado de Royal Match o Magic Sort** (UI dorada biselada, frascos con labio): riesgo de trade dress (07 §1.3; research/08 §4.1).

**Cómo revertir.**
- Revertir los commits de la fase visual: `ProceduralArt.cs`, `GameScreen.cs`, `BoardLayout.cs`, `UiBuilder.cs`, `CuiaView.cs` y sus tests vuelven al estado de `ef676a4`. Si ya se reescribió 07, restaurar su texto anterior.
- Las fuentes pueden quedarse: Baloo 2 y Nunito ya están en 07 §7.1.
- La regla de L* no vuelve en ningún caso, porque es imposible con cualquier dirección: hará falta otra sustitución.
- En todos los casos, añadir a este registro una decisión nueva que sustituya a D-003.

**Preguntas para el dueño (cerradas).**

| # | Pregunta | Recomendación | Bloquea |
|---|---|---|---|
| P8 | ¿Se permiten degradados, relieve, sombras suaves y contorno de texto no dorado en botones de madera, piedra y hoja, manteniendo prohibidos el oro, las gemas, las cintas rojas, los gatos, el caldero, la calavera y la silueta de frasco con labio? | Sí | Botones de la fase visual (V5) y 07 §1.3 |
| P9 | ¿Se sustituye la regla de L* de 07 §1.2 por las cuatro comprobaciones compuestas de arriba, con esos umbrales [P]? | Sí | Test de contraste (V4) y 07 §1.2 |
| P10 | ¿Se reescriben 07 y GDD.md y se encarga el brief nuevo al artista después de aceptar las capturas antes y después de la fase visual? | Sí | Lote de documentación y encargo al artista |
