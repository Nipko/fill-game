# Registro de decisiones del producto

Fecha de creación: 2026-10-06 · Autor: tech-lead · Parte de `main` en `8854993` (prototipo v0 jugable; árbol limpio).

Cada decisión del dueño que cambia reglas, UX o contratos del GDD se registra aquí, en formato ADR breve: fecha, estado, contexto, decisión, consecuencias, alternativas descartadas y cómo revertirla. El GDD sigue siendo la fuente de verdad (CLAUDE.md, "Fuentes de verdad"): este registro **no** sustituye la edición de los capítulos. Cada decisión lleva su tarea de documentación (sección 3, T5), y mientras esa tarea no se fusione, ante una contradicción entre el GDD y este registro manda la decisión fechada del dueño. Las entradas no se borran: si una decisión se revierte, se añade otra que la sustituye.

| ID | Fecha | Título | Estado |
|---|---|---|---|
| D-001 | 2026-10-06 | Una dosis por vertido | Sustituida por D-005 (2026-10-08). Antes: decidida por el dueño; implementada en el árbol de trabajo, sin commitear (T1-T3 y T5) y verificada en el SM-S938B el 2026-10-07 (T7: A7 y A8). Pendientes: commit y PR, T6 y las preguntas P2-P7 |
| D-002 | 2026-10-06 | Sin resaltado de destinos al seleccionar | Decidida por el dueño; implementada en el árbol de trabajo, sin commitear (T4 y T5) y verificada en el SM-S938B el 2026-10-07 (T7: B1-B2 y captura de B3). Pendientes: commit y PR y la revisión visual del dueño (B3) |
| D-003 | 2026-10-07 | Dirección visual «Selva brillante premium» | Decidida por el dueño; fase visual implementada con arte procedural provisional en `19177bd` y vista por el dueño en el SM-S938B el 2026-10-07 (de ahí D-004). Pendientes: P8-P10, los PNG de F0 y su integración (F6 de `docs/art/direccion-visual-v2.md`) y la edición del GDD. El punto 5, en lo que toca a la cavidad y el menisco, queda sustituido por D-006 (2026-10-08) |
| D-004 | 2026-10-07 | Panel azul petróleo, vertido a 38° y cuias más anchas con pocas cuias | Decidida por el dueño (respuestas a C16-C18 de `docs/art/direccion-visual-v2.md`); implementada en el árbol de trabajo, sin commitear (código, tests y documentación de «Archivos afectados»; EditMode y PlayMode en verde el 2026-10-07). Pendientes: commit y PR, revisión en el teléfono (`dev.apk`) y nueva medida de C19. Actualización del 2026-10-08: implementada en `b2d34de` (rama `d004-cuias-anchas`); la decisión 2 (38° fijos y labio a 0,6-1,6 u), el techo del vertido [P], la consecuencia «no cambia el número de filas» y el alto de la cuia (1:3, 6 u; pasa a 5,6 u) quedan sustituidos por D-006, que también matiza el recálculo a mitad de nivel: las filas se fijan al cargar y no se recalculan, solo el ancho; el resto sigue (panel azul petróleo y topes 64/56/48 dp) |
| D-005 | 2026-10-08 | Vertido por tramo, como Magic Sort | Decidida por el dueño; en implementación (C1, L1, U1, D1-D6) |
| D-006 | 2026-10-08 | Tablero, cuia y vertido como Magic Sort | Decidida por el dueño; en implementación (U2-U6, D1-D6). Pendiente: P12 (ranura de 42 dp en 16:9 con cuias añadidas) |
| D-008 | 2026-10-08 | Bucle de partida como Magic Sort | Decidida por el dueño. Documentación aplicada el 2026-10-08 en `117f4be` (capítulos de «Archivos afectados»), dentro de la rama de código: **pendiente de la aprobación explícita del dueño antes de fusionar** o de un PR de documentación aparte (cambia el contrato 7). En implementación: pista B (B1-1-B1-6 y B2-1-B2-6). Los textos nuevos y los puntos [P] están pendientes (P14-P16, no bloquean). Abierta: P17, cuándo sale «Canoas infinitas 1 h» en Sin canoas (04 §7.3 y 05 §4.1 se contradicen). El número D-007 queda reservado para las mecánicas especiales, en pausa |
| D-010 | 2026-10-08 | «Llenar y teñir»: el río llena las cuias y los miquitos las tiñen con fruta | Decidida por el dueño (idea y ampliación del 2026-10-08); guion `docs/research/intro-rio-miquitos/guion.md` aprobado para implementar. Implementada en la rama `intro-rio-miquitos` (`fbdf11e`, `e479cf0` y `8955ef3`), sin fusionar. Documentación aplicada el 2026-10-09 en la misma rama. Pendientes, sin bloquear: confirmar Q1-Q16 del guion (el código aplica sus recomendaciones salvo Q11 y Q14), Q17, toda la fauna con el asesor cultural, el arte del artista y las preguntas abiertas de la propuesta de fauna (F7, F9-F11 y F13). Los números D-007 y D-009 los usa la pista de mecánicas y niveles (rama `fase1-mecanicas-niveles`), sin entrada aún en este registro |
| D-011 | 2026-10-11 | Ajustes de la prueba en el teléfono: la victoria se cobra cuando se ve, avisos legibles, miquitos que se ven y Cosecha doble que avanza | Decidida por el dueño tras probar el `dev.apk` en el SM-S938B. Sin implementar: tareas P1-P4 de `docs/dev/traspaso/pruebas-telefono-2026-10-11.md`, para el equipo de escritorio |

---

## D-001 · Una dosis por vertido

- **Fecha:** 2026-10-06.
- **Estado:** decidida por el dueño tras jugar el prototipo v0 en su teléfono (Samsung SM-S938B). Implementada y verificada en el teléfono el 2026-10-07, sin commitear (ver la tabla de arriba). **Sustituida por D-005 el 2026-10-08.**

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

*Nota del 2026-10-08: las secciones 1-6 analizan D-001 y D-002. D-001 quedó sustituida por D-005: lo que aquí se dice de `single` como modo por defecto se conserva como historia y como descripción del modo de pruebas. Los estados de P1-P7 y R-1-R-6 están en las secciones 5 y 6.*

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

## 5. Preguntas para el dueño

| # | Pregunta | Recomendación | Bloquea | Estado (2026-10-08) |
|---|---|---|---|---|
| P1 | ¿Confirmas la clave `rules_pour_mode` (`"single"` \| `"run"`, default `"single"`) en la tabla única de 08 §9, con la nueva familia de prefijo `rules_`, y que `Run` quede solo como opción de prueba? | Sí | T5 (08 §9) | Respondida. Con D-005 el default de `rules_pour_mode` pasa a `run`; `single` queda como opción de pruebas |
| P2 | Par del tutorial en `Single`: ¿(A) mantener los 10 tableros y fijar como rango [P] su par medido (3, 9, 10, 12, 16, 17, 15, 17, 16, 19), aceptando la rampa no monótona, o (B) rediseñar los niveles 2-10 para una rampa monótona con rangos que propone level-pipeline-dev en el PR? | B | T2 | Cerrada por D-005: los pares en `run` (3, 6, 7, 8, 10, 10, 11, 13, 14, 16) entran en los rangos de 02 §13; no se rediseñan los niveles 2-10 |
| P3 | Nivel 2: ¿sustituir la lección del vertido parcial por "una dosis por vertido" con el texto [P] "Cada vertido pasa una sola dosis: repite para mover las demás."? | Sí | T2, T5 | Cerrada: el nivel 2 vuelve a la lección del vertido parcial (02 §13); el texto «Cada vertido pasa una sola dosis…» no se usa |
| P4 | Tras verter, ¿el origen se deselecciona (C4 actual, estilo *ball sort*) o sigue seleccionado si puede volver a verter al mismo destino (selección pegajosa, menos toques)? | Deseleccionar (C4 actual) | T3 (solo si cambia) | Cerrada: tras verter, el origen se deselecciona (C4 canónico) |
| P5 | El dedo guía de los niveles 1-2 (02 §13) y Morpho (04 §2.3) muestran un destino: ¿se mantienen como ayudas explícitas compatibles con D-002? | Sí | T5 | Sigue abierta: depende de D-002, no de D-001 (Magic Sort no mostró mano guía en los niveles 387-389, ficha §2.10) |
| P6 | ¿Una dosis por vertido es también la regla de 1.0? Si es así, antes de la Fase 1 hay que recalibrar en `Single`: las cifras [V-proto] de 03 (`par ≥ 2K`, bot ×40 y etiquetas, índice D), Marea "3 por nivel", `restart_confirm_min_pours`, `lluvia_k_pours` y `pirana_k_pours` | Sí, con una tarea de recalibración aparte | Fase 1 (no v0) | Cerrada: `run` es la regla de 1.0; sin recalibración |
| P7 | En 1.0, ¿el pack declara el modo con el que se calculó el par (cambio de contrato: `af-level/2`, `pack_schema` 2)? | Sí | `schemas/` (no v0) | Reescrita: «Con `run` por defecto y `single` solo de pruebas, ¿el formato sigue sin declarar el modo (los packs se generan siempre en `run`) y se retira P7?». Recomendación: sí. No bloquea |

## 6. Riesgos

| # | Riesgo | Mitigación | Estado (2026-10-08) |
|---|---|---|---|
| R-1 | Presupuesto del solver en `Single` con K de 8 a 14: entre ×1 y ×34 de estados expandidos en el tutorial (nivel 7: 19 → 641); Morpho debe quedar < 50 ms con 40.000 nodos | `H_single` (hasta ×2 menos en el tutorial); medir con `tools/solver-bench` (aún no existe; tarea de core-dev o level-pipeline-dev) antes de 1.0 | Mitigado por D-005: en producción se juega en `run` |
| R-2 | Mezclar modos: un pack con par `Single` jugado en `Run` (o al revés) da pares falsos en QA y en automatización | Modo en el informe y en `AF_STATE load`; el modo solo cambia al cargar un nivel; T6 retira las firmas sin modo | Sigue para los packs de pruebas generados en `single` |
| R-3 | `main` en rojo entre T2 y T3 | Fusión en lote y EditMode tras las dos | — |
| R-4 | Saltos visibles en la cola con toques rápidos (3 o más vertidos iguales) | Encadenado (A9) | Con D-006 aplica a todo vertido: 870 ms de la salida al reposo con 1 dosis. La cola de 2 vertidos por cuia (02 §3.2) lo cubre; el encadenado solo actúa en `single` |
| R-5 | Más rechazos y frustración sin resaltado, sobre todo con 6-7 cuias | Marea ilimitada hasta el 10; vigilar la tasa de `reject` por nivel en `AF_STATE` durante la prueba del dueño | — |
| R-6 | Desviación del canon del género (research/01 §2): las cifras de investigación y de prototipo se midieron con rachas | P6; registrar la desviación en 02 y GDD.md | Desaparece con D-005: `run` vuelve a ser el modo por defecto |

---

## D-003 · Dirección visual «Selva brillante premium»

- **Fecha:** 2026-10-07.
- **Estado:** decidida por el dueño tras ver el prototipo v0.1 (`ef676a4`) y el concepto «Orillas vivas» de la revisión del 2026-10-07. Fase visual implementada con arte procedural provisional en `19177bd` (ver la tabla de arriba). Hay dos puntos pendientes de respuesta: la frontera exacta con el trade dress de los líderes (P8) y los umbrales de contraste (P9).

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
   - La cuia-garrafa, su cavidad recta, las alturas iguales por dosis, las marcas y el menisco (07 §4). *Las proporciones de la cavidad (0,80 del ancho; 4 de 5,6 u) y el menisco (elipse de 0,40 u): sustituidos por D-006 (2026-10-08); la silueta, la cavidad recta, las alturas iguales y las marcas siguen.*
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

---

## D-004 · Panel azul petróleo, vertido a 38° y cuias más anchas con pocas cuias

- **Fecha:** 2026-10-07.
- **Estado:** decidida por el dueño tras ver en su teléfono (SM-S938B) el build de la fase visual de D-003 (`19177bd`). Respondió a las preguntas C16-C18 de `docs/art/direccion-visual-v2.md` (sección 11) con «azul petróleo, botellas más anchas y 38° está bien». Implementada en el árbol de trabajo, sin commitear (ver la tabla de arriba).

**Contexto.**
- La guía visual v2 dejó abiertas tres preguntas tras la revisión del 2026-10-07:
  - C16, tono del panel del tablero: la variante A «azul petróleo de río» o la B «caoba lacada», para elegir con dos capturas lado a lado (guía 4.6, paso F1).
  - C17, inclinación del vertido: con 38° (02 §4.1, 07 §5.2) y dos filas, el origen tapa la parte alta de la cuia vecina mientras vierte (≤ 400 ms). Los críticos proponían 75° fijos o una inclinación dinámica.
  - C18, ancho máximo de la cuia: 02 §5 y 07 §4.1 lo fijaban en 48 dp en todos los tableros. En el nivel 1 (3 cuias), eso dejaba cuias de 48 dp en ranuras de 112 dp y 196,5 dp de muelle vacío bajo el panel (360 × 780 dp).
- Los líderes del género agrandan los frascos en los primeros niveles (guía, C18).

**Decisión.**
1. **Panel (C16): variante A, «azul petróleo de río»**, tal como la vio el dueño en el build: degradado `panel_alto` #234452 → `panel_bajo` #142C36 (L* 27 → 17) y `cavidad` #102228 (`Palette.PanelA` en `19177bd`). La tabla de la guía en `19177bd` (secciones 2 y 4.6) daba todavía #1E3A44 → #173540, anterior a la ronda 2 de la revisión visual; el build ya usaba los valores de arriba, que son los que vio el dueño. Se descarta la variante B «caoba lacada» (#44302C → #372521, cavidad #251A17), que solo existía para la comparación de F1: sale del código, de las capturas y de los prompts.
2. **Vertido (C17): se mantienen los 38°** de 02 §4.1 y 07 §5.2, con una elevación de 0 a 1 u, de modo que el labio queda entre 0,6 y 1,6 u por encima del borde del destino (`BoardAnimator.MaxRaiseU`, revisión visual del 2026-10-07). Se acepta que, en tableros de dos filas, el origen tape la parte alta de la cuia vecina mientras vierte (≤ 400 ms). Se descartan los 75° fijos y la inclinación dinámica. Esta respuesta no cambia el código. *Sustituido por D-006 (2026-10-08): pose medida en Magic Sort y sin techo.*
3. **Cuias más anchas (C18):** el tope del ancho de la cuia depende del número de cuias T del tablero (las que hay en pantalla, incluidas Cuia extra y Cuia de rescate; 02 R1):
   - tope(T) = 64 dp con T ≤ 3, 56 dp con T = 4-5 y 48 dp con T ≥ 6 (como hasta ahora);
   - ancho = clamp(0,70 × ranura, 28, tope(T)); alto según la proporción 1:3 (6 u); u = ancho / 2;
   - a 360 dp, con ranura = (360 − 24) / cuias_por_fila: T = 3 → 64 × 192 dp, u = 32; T = 4 → 56 × 168 dp, u = 28; T = 5 → ranura de 67,2 dp y 0,70 × 67,2 ≈ 47 dp (el tope no actúa); T ≥ 6, sin cambios. *El alto: sustituido por D-006 (2026-10-08), que lo fija en 5,6 u (≈ 1:2,8; con T = 3, 64 × 179,2 dp). Los anchos y los topes siguen.*
   - el encaje vertical sigue mandando: si el bloque de filas no cabe, `BoardLayout.Compute` reduce el ancho (mínimo 28 dp);
   - tabletas: el tablero se centra con 480 dp de ancho como máximo y la cuia no supera tope(T);
   - si T cambia a mitad de nivel (Cuia extra, Cuia de rescate), el layout se recalcula con el T nuevo (re-layout animado de 300 ms, 02 §4.1). *Matizado por D-006 (2026-10-08): el número de filas se fija al cargar el nivel y no cambia; la cuia añadida va a la fila más corta y solo se recalcula el ancho.*

   Cambian 02 §5 y 07 §4.1 a la vez, como exige 07 §4.1.

**Consecuencias.**
- Tutorial v0 (los datos de `tutorial_v0.json` no cambian):
  - el nivel 1 (T = 3) pasa a cuias de 64 × 192 dp (u = 32);
  - los niveles 2, 3 y 6 (T = 5) siguen en ≈ 47 dp;
  - los niveles 4 y 5 (T = 6, dos filas de 3) y del 7 al 10 (T = 7, filas de 4 y 3) siguen en 48 dp;
  - ningún nivel del tutorial tiene T = 4.
- Nivel 1 a 360 × 780 dp: el panel pasa de 219 a 281 dp de alto (y = 206,5-487,5) y la banda libre del muelle, de 196,5 a 172,5 dp. No baja a ≈ 140 dp como estimaba C18: el bloque está centrado y crece hacia los dos lados. El panel incluye los 36 dp de margen superior de los tableros de una fila [P] (ver el techo del vertido, más abajo).
  - A 360 × 800: panel en 216,5-497,5 y banda de 182,5 dp.
  - A 360 × 640: panel en 136,5-417,5 y banda de 102,5 dp. El poste del muelle sigue visible (banda ≥ 64 dp).
  - El horizonte del panorama sube de 180,5 a 142,5 dp (`GameScreen.HorizonFor`).
- La u máxima pasa de 24 a 32 dp. Lo que se mide en u escala solo (vidrio, líquido, chorro, salpicaduras, pose del vertido). La selección sigue elevando la cuia 16 dp (02 §4.1), que son 0,5 u con la cuia de 64 dp.
- Texturas:
  - en el SM-S938B, el vidrio se hornea a 96 px/u con T ≤ 3 (antes, 72);
  - `ProceduralArt.MaxCuiaPixelsPerU` sube de 96 a 128 para las pantallas de 4 px/dp [P];
  - el aro y la tapa del sellado se generan para u = 32;
  - la fuente del artista a 4× del tamaño máximo pasa de 192 × 576 a 256 × 768 px por cuia de 4 (07 §4.1).
- **Techo del vertido y margen superior en una fila [P].** Son dos propuestas derivadas de C18, no respuestas del dueño. *Sustituido por D-006 (2026-10-08): pose medida en Magic Sort y sin techo.*
  - Con u = 32, el canto alto del origen inclinado sube 0,94 u (30 dp) sobre el borde del destino sin elevación: el labio a 0,6 u más 0,34 u del borde de fibra (`CuiaGeometry.RimHalfWidthU` = 0,55 u, inclinado 38°). Con 1 u de elevación sube 1,94 u (62 dp). Con el margen de 22 dp, una elevación de más de 0,25 u lo sacaba del panel por arriba.
  - **Margen superior:** en los tableros de una fila, el panel deja 36 dp sobre el bloque (`BoardLayout.PanelMarginTopOneRow`, ≈ 22 + 13,5 del marco). Sobre las cuias en reposo quedan 52 dp hasta el borde exterior del panel y 38,5 hasta el canto interior del marco. En dos y tres filas se mantienen 22 dp (`BoardLayout.PanelMargin`): con 36, el río del nivel 7 bajaría de 34,5 a 20,5 dp.
  - **Techo** (`GameScreen.PourCeilingFor`): el origen inclinado se queda 4 dp por debajo del canto interior del marco (`GameScreen.PourCeilingAirDp`: con menos de 1 dp la cuerda del labio rozaba el bisel y la cuia parecía apoyarse en el marco) si entre ese canto y la fila 1 elevada quedan ≥ 22 dp de cavidad (`GameScreen.PourCavityDp`; tableros de una fila, con 22,5 dp). Si no, el techo es el borde exterior del panel (dos y tres filas, 38 dp sobre las cuias en reposo). Nunca sube más de 4 dp dentro de la fila del HUD.
  - En el nivel 1, el techo deja ≈ 4,5 dp (≈ 0,14 u) de elevación y el labio sigue ≥ 0,6 u sobre el borde del destino; la elevación que lo pasaría se recorta. Sin elevación siempre cabe.
- **Tapado en tableros de una fila: aceptado.** En una fila, el vertido entre cuias no vecinas tapa la parte alta de la cuia del medio mientras vierte (captura `04b_pouring_l1`, nivel 1, cuia 0 → 2). Entra en la decisión 2: el dueño respondió «38° está bien» después de ver la captura del teléfono `v2-tel-rafaga-1` (nivel 1, vertido 0 → 2 con la cuia del medio tapada). Si al verlo con las cuias anchas prefiere otra cosa, se abre una decisión nueva.
- C19 (familia parda ≤ 40 % [P]) se vuelve a medir con las cuias de D-004 (`02b_board_l1`, `02d_board_l2`) y con el muelle pintado (F6).
- No cambian: las reglas, el solver, los pares, el formato de nivel, Remote Config (no hay clave nueva), D-001, D-002, D-003, las áreas táctiles (02 §3.3), el paso de fila ni el número de filas (02 §5). *El número de filas: sustituido por D-006 (2026-10-08), que fija 2 o 3 filas desde 11 cuias.*

**Alternativas descartadas.**
- Panel «caoba lacada» (B): cálido, pero el tablero queda más «mueble» que «agua» (guía 4.6).
- Inclinación de 75° fija, o dinámica: 38° + 12° × (4 − dosis que quedan), con tope de 80° (C17).
- Mantener el tope de 48 dp en todos los tableros (C18).
- Una clave de Remote Config para el tope: nadie la ha pedido y 08 §9 no la tiene.

**Cómo revertir.**
- Revertir los commits de la implementación (`BoardLayout.cs`, `ProceduralArt.cs`, `BoardAnimator.cs`, `CuiaGeometry.cs`, `GameScreen.cs` y sus tests) y restaurar el texto de 02 §5 y 07 §4.1, los dos a la vez.
- La variante B se recupera de `19177bd` (`Palette.PanelB`).
- En cualquier caso, añadir una decisión nueva que sustituya a D-004.

**Archivos afectados.**
- Documentación: `docs/gdd/02-nucleo-reglas-ux.md` (cabecera, §4.1 y §5), `docs/gdd/07-arte-tematica-audio.md` (cabecera, §4.1, §4.3 y §5.2), `docs/art/direccion-visual-v2.md`, `docs/art/prompts-chatgpt.md`, este registro y `CLAUDE.md`. `docs/GDD.md` no cambia: ninguna fila de §2 cita el tope de 48 dp, y la decisión 23 da 34 × 102 dp con 7 por fila, que sigue igual.
- Código: `Assets/_Project/Scripts/UI/{BoardLayout.cs, GameScreen.cs, GameBootstrap.cs}` y `Assets/_Project/Scripts/Presentation/{Palette.cs, ProceduralArt.cs, ProceduralArt.Scene.cs, BoardAnimator.cs, CuiaGeometry.cs}`. Las propuestas [P] están en `BoardLayout.PanelMarginTopOneRow` (margen superior de 36 dp en una fila), `GameScreen.PourCeilingFor` y `GameScreen.PourCavityDp` (techo del vertido), y `CuiaGeometry.RimHalfWidthU` (canto alto del borde de fibra, que usa el techo en `BoardAnimator`).
- Tests: `Assets/_Project/Tests/EditMode/{BoardLayoutTests.cs, VisualContrastTests.cs, LiquidVolumeTests.cs}` y `Assets/_Project/Tests/PlayMode/{PourPoseTests.cs, VisualGuardTests.cs, VisualSnapshotTests.cs}`.
- Sin cambios: `Assets/_Project/Data/Levels/tutorial_v0.json`, `Packages/com.amazonfill.core/`, `tools/`, `schemas/` y 08 §9.

---

## D-005 · Vertido por tramo, como Magic Sort

- **Fecha:** 2026-10-08.
- **Estado:** decidida por el dueño tras analizar su propia sesión de 20 min de Magic Sort: «sí, hagámoslo» y «hagámoslo como Magic». Su método: «no vamos a inventar la rueda: copiamos lo que funciona y cómo se hace, con nuestra temática». Responde la pregunta 4.1 (10.2 #1) de `docs/research/magic-sort/propuesta-tintalu.md`. **Sustituye a D-001.** En implementación (ver la tabla de arriba).

**Contexto.**
- Magic Sort y Love Water vierten el tramo entero: 2 dosis en t = 250 y 430, y 3 dosis en t = 598, 600 y 838 (`docs/research/magic-sort/ficha-magic-sort.md` §2.5; Love Water, ficha §10).
- Es la regla de todos los líderes del género (02, Desviaciones 3; research/01 §2 [V]).
- El coste de `single` ya estaba medido en este registro: par del tutorial ×1,0-×1,7 (§2.1.4) y solver con ×1-×34 de estados expandidos (§2.1.3).

**Decisión.**
1. Un vertido mueve la **racha superior visible del mismo color que cabe en el destino**, con vertido parcial (02 §1.2 y R9; modo `run`).
2. `rules_pour_mode` = `run` por defecto (08 §9) y `PourModes.Default = PourMode.Run` en `AF.Core`. `single` queda solo como opción de pruebas: por Remote Config y con el extra de desarrollo `af_rules_pour_mode`.
3. El modo sigue fijo al cargar el nivel y no cambia a mitad (D-001, punto 3).
4. El tutorial v0 se regenera en `run`. Pares: 3, 6, 7, 8, 10, 10, 11, 13, 14 y 16, dentro de los rangos [P] de 02 §13. La lección del nivel 2 vuelve a ser el vertido parcial.
5. D-002 no cambia: Magic Sort tampoco resalta destinos (ficha §2.10).

**Consecuencias.**
- Preguntas de §5: P2, P3, P4 y P6 cerradas; P7 reescrita; P5 sigue abierta.
- Las cifras [V-proto] de 03, medidas en `run`, vuelven a valer.
- No hay que recalibrar Marea «3 por nivel», `restart_confirm_min_pours`, `lluvia_k_pours` ni `pirana_k_pours`.
- R-1 (presupuesto del solver en `single`) deja de aplicar en producción.
- El encadenado de 02 §3.2 solo actúa en `single`. En `run`, con las reglas de v0 (sin mecánicas ni ocultas), dos vertidos A→B seguidos nunca son legales: o el origen cambia de cima o el destino queda lleno.
- La desviación 10 de GDD.md §8 y la desviación 3 de 02 quedan superadas.
- T6 pierde urgencia, porque las firmas heredadas sin modo equivalen a `Run`, que ahora es el modo por defecto. Sigue pendiente.

**Alternativas descartadas.**
- Mantener D-001 (recomendación A de la pregunta 4.1 de la propuesta).
- `single` por defecto y `run` en 1.0.
- Verter la racha con un gesto aparte (ya descartado en D-001).

**Cómo revertir.**
- `PourModes.Default = PourMode.Single` (y el orden del enum), `GameConfig.RulesPourMode = "single"`, regenerar el pack con `--pour-mode single`, los tests de U1 y los textos de D2-D6.
- Siempre con una decisión nueva que sustituya a D-005.

**Archivos afectados.**
- Núcleo (C1): `Packages/com.amazonfill.core/Runtime/AF.Core/Rules/{PourMode.cs, CuiaRules.cs, Pour.cs, PourRules.cs}`, `Runtime/AF.Core/Session/LevelSession.cs`, `Runtime/AF.Solver/Search/{AStarSolver.cs, SolverHeuristic.cs, SolverOptions.cs}`, sus tests (`PourModeTests`, `AStarSolverPourModeTests`, `PourModePropertyTests` y `HotLoopAllocationTests`), `README.md` y `CHANGELOG.md`.
- Generador (L1): `tools/level-generator/{Program.cs, Handmade/HandmadeLevel.cs, Handmade/HandmadeReport.cs, Handmade/HandmadeRunner.cs, Handmade/StateGraph.cs}`, la especificación `tools/level-generator/specs/tutorial_v0.json` y, generados, `Assets/_Project/Data/Levels/tutorial_v0.json` y `tools/out/tutorial_v0_report.md`.
- Unity (U1): `GameConfig.RulesPourMode`, los textos y comentarios de D-001 en `GameFlow/{LevelFlow.cs, TapController.cs}` y `UI/{LaunchOptions.cs, DevProbe.cs, GameBootstrap.cs}`, y los tests que fijaban `single` como modo por defecto (lista en D-001, «Cómo revertir»). `LaunchOptions.cs` y `GameBootstrap.cs` llevan además el gancho de tests `LaunchOptions.LevelPackJsonOverride`, que usan los tests de filas de D-006.
- Documentación (D1-D6): este registro, `docs/GDD.md`, `docs/gdd/{00, 01, 02, 03, 04, 06, 07, 08, 09}`, `docs/art/direccion-visual-v2.md`, `docs/dev/prototipo-v0.md`, la nota de respuestas de `docs/research/magic-sort/propuesta-tintalu.md` y `CLAUDE.md`.

---

## D-006 · Tablero, cuia y vertido como Magic Sort

- **Fecha:** 2026-10-08.
- **Estado:** decidida por el dueño junto con D-005 («sí, hagámoslo» al paquete de tablero y vertido). Responde las preguntas 10.2 #3, #4, #5, #6 y #7 y la 4.3.5 de `docs/research/magic-sort/propuesta-tintalu.md` (secciones 3 y 4). **Sustituye** la decisión 2 y el techo del vertido [P] de D-004, su consecuencia «no cambia el número de filas» y el punto 5 de D-003 en lo que toca a la cavidad y el menisco. En implementación (ver la tabla de arriba).

**Contexto.**
- Medidas de la ficha de Magic Sort: §2.1 (rejilla y filas), §2.2 (botella), §2.4 (selección) y §2.6 (pose y tiempos del vertido).
- Su traducción a nuestra escala en la propuesta, §3.1-§3.5.
- Material: `Logs/device/magicsort/analisis/` (`observar.json`, `ficha_workflow_result.json`, `ms_pour_sheet.png` con el vertido cada 100 ms y los fotogramas de `det_ms/` a 1080 × 2340).

**Decisión.**
1. **Pose del vertido.**
   - El centro de la boca del origen queda sobre el eje del destino (±0,1 u) y su punto más bajo, a 0,2-0,6 u del borde superior del destino. Con el centro de la boca a 0,85 u [P], el punto más bajo queda a 0,30-0,51 u entre 38° y 92°.
   - El origen llega a 38° (ficha §2.6, 3.100 ms) y gira alrededor de la boca: al terminar cada dosis, la inclinación es 92° − 9° × dosis que quedan en el origen (65° con 3, frente a los ≈ 66° que midió Magic Sort; ≈ 92° con 0).
   - El cuerpo queda del lado del que viene la botella (en la misma columna, hacia el centro del tablero). Si ese lado saliera del lienzo entre 38° y el ángulo final, al otro lado [P]. Vale para cualquier vertido, no solo en la misma columna.
   - Escala 1,00.
   - Mientras vierte, el origen se dibuja **delante de todo** (vecinas, marco del panel y HUD), como Magic Sort en t = 250. No hay techo.
   - Se retiran `GameScreen.PourCeilingFor`, `PourCavityDp`, `PourCeilingAirDp`, `BoardAnimator.TopLimitY`, `MaxRaiseU`, `LipClearanceU` y `BoardLayout.PanelMarginTopOneRow`, que existía solo por el techo.
2. **Tiempos** (08 §9).
   - `anim_lift_ms` 100, `anim_travel_ms` 300, `anim_pour_dose_ms` 280 (rango 80-300; Magic Sort, ≈ 285 ms por dosis), `anim_pour_tail_ms` 80 y `anim_return_ms` 300.
   - El chorro sigue arrancando al 70 % del viaje (02 §4.1). De la salida al reposo: 870 ms con 1 dosis (≈ 0,9 s), 1.150 ms con 2 y 1.430 ms con 3.
   - La vuelta va directa a la ranura. El rebote de Magic Sort (≈ 50 px bajo el reposo, visto en un solo vertido) no se copia porque no está confirmado. Se mantiene el rebote de 0,03 u de 07 §5.2.
3. **Cuia por dentro** (opción (a) de la propuesta, §3.4).
   - 1 dosis = 1 u. Cavidad recta de 1,60 × 4,00 u: 0,80 del ancho exterior de 2,00 u.
   - Alto exterior: 5,60 u. Las 4 dosis ocupan 0,714 del alto (≈ 1:2,8).
   - Reparto [P]: pie 0,30, base 0,30, cavidad 4,00, hombro 0,40, cuello 0,35 y borde 0,25 u. El labio queda en 5,35 u.
   - Cara superior del líquido en elipse de 0,40 u, más clara que el tinte (el mismo aclarado por tinte que el menisco actual).
   - Se mantienen el hombro, el cuello corto, el pie, el borde de fibra y la lista de trade dress de 07 §1.3 (nada de labio de botella).
   - Las demás capacidades miden 1,60 + cap u.
4. **Tablero.**
   - 1 fila con T ≤ 5 y 2 filas con T = 6-10, sin cambios.
   - Con T = 11-14: 2 filas (hasta 7 por fila) o 3 filas (≤ 5 por fila; reparto de arriba abajo 4/4/3, 4/4/4, 5/4/4 y 5/5/4), la que dé la cuia más ancha. A igualdad, 2 filas [P].
   - Con T = 15-16: 3 filas (5/5/5 y 6/5/5).
   - El número de filas se fija al cargar el nivel con las T cuias generadas y **no cambia a mitad de nivel**. Cuia extra y Cuia de rescate van a la fila más corta (a igualdad, la de más abajo [P]) y solo se recalcula el ancho.
   - Los topes de D-004 (64/56/48 dp) no cambian. El hueco entre filas sigue en 40 dp (repisa de D-003). No se reserva hueco para el vertido.
   - Se mantiene ≥ 8 dp entre la fila 1 elevada y el canto interior del marco (guía visual v2, §3.2) [P].
5. **Elevación al seleccionar:** 16 dp (Magic Sort sube ≈ 17 dp a nuestra escala).

**Consecuencias.**
- Tamaños de la cuia a 360 × 780 dp:

  | Cuias (T) | Filas | Cuia (dp) |
  |---|---|---|
  | 3 | 1 | 64 × 179,2 (u = 32) |
  | 5 | 1 | 47,04 × 131,7 |
  | 6-8 | 2 | 48 × 134,4 |
  | 11-15 | 3 | 46,9 × 131,3 (Magic Sort con 13-14 botellas: 45 × 138) |
  | 16 | 3 | 39,2 × 109,8 |

  - A 360 × 800 dp, con 3 filas: 48 dp con T = 11-12 y 47,04 dp con T = 13-15.
  - A 360 × 640 dp (16:9) ganan las 2 filas con T = 11-14 (39,2 y 33,6 dp); con T = 15-16, las 3 filas bajan a 30,2 dp.
  - Ancho de color con 13 cuias: 37,5 dp (hoy, 22,1).
  - Fuente del artista a 4×: 256 × 717 px por cuia de 4.
- Cambian 02 §3.2, §4, §4.1, §5 y §8; 07 §3 (regla 4), §4.1-§4.5, §5.2 y §7.4; 08 §5.2, §9 y §14.1; 00 §4; 01 §3.3; 03 (Dependencias, fila 07); 04 §2.2; 09 (Fase 0 de §2.3 y días 4-5 de §9); GDD.md §2 (decisiones 3 y 23); y la guía visual v2.

**Alternativas descartadas.**
- 38° fijos (D-004).
- La inclinación dinámica de C17: 38° + 12° × (4 − dosis), con tope de 80°.
- Reservar el hueco del vertido (≈ 43,6 dp con 11-15 cuias; propuesta §3.3).
- La opción (b) de la propuesta §3.4 (redefinir u).
- El hueco de 22 dp entre filas de Magic Sort.
- La elevación proporcional de ≈ 17 dp (propuesta 4.3.5).

**Cómo revertir.**
- Revertir los commits de U2-U5 y los textos de D2-D6, y restaurar el techo de `b2d34de`.
- Siempre con una decisión nueva que sustituya a D-006.

**Archivos afectados.**
- Código y tests: los de U2-U6, entre ellos los que retiran `GameScreen.PourCeilingFor`, `PourCavityDp`, `PourCeilingAirDp`, `BoardAnimator.TopLimitY`, `MaxRaiseU`, `LipClearanceU` y `BoardLayout.PanelMarginTopOneRow`.
- Documentación (D2-D6): `docs/GDD.md`, `docs/gdd/{00, 01, 02, 03, 04, 07, 08, 09}`, `docs/art/direccion-visual-v2.md`, la nota de respuestas de la propuesta y `CLAUDE.md`.

**Pregunta para el dueño (abierta).** Surgió en la revisión del código de D-006. El registro no elige por el dueño (CLAUDE.md, regla 10).

| # | Pregunta | Opciones | Bloquea |
|---|---|---|---|
| P12 | En 16:9 (360 × 640 dp) ganan las 2 filas con 11-14 cuias generadas (02 §5). Como las filas no cambian a mitad de nivel (punto 4), con 13-14 cuias generadas la Cuia extra y la Cuia de rescate llevan la fila más corta a 8 cuias (14 + 1, 13 + 2 y 14 + 2): ranura de 42 dp y cuia de 29,4 dp, por debajo del mínimo táctil de 48 dp (02 §3.3; 07 §7.4). Antes de D-006, con 16 cuias se pasaba a 3 filas y la ranura medía 56 dp. ¿Qué se hace? | (a) Aceptar la ranura de 42 dp en ese caso y anotarlo como excepción en 02 §3.3, 02 §5 y 07 §7.4. (b) En 16:9, si la fila más corta ya tiene 7, abrir una 3.ª fila aunque no se fijara al cargar, como excepción al punto 4. (c) Decidir 3 filas al cargar cuando T + 2 pase de 14 | No bloquea v0 (el tutorial tiene T ≤ 7). Hay que responderla antes de los niveles con 13-14 cuias. Después, el caso entra en `BoardLayoutTests` |

---

## D-008 · Bucle de partida como Magic Sort

- **Fecha:** 2026-10-08.
- **Estado:** decidida por el dueño el 2026-10-08: «la estructura se copia de Magic Sort; las cifras salen de nuestro GDD». Su postura general: copiar lo que funciona sin suavizarlo, salvo por política de Google Play.
  - El número D-007 y la pregunta P13 quedan reservados para las mecánicas especiales, **en pausa**: el dueño pidió investigar antes la cultura ribereña y la dificultad escalonada. Nada de esta entrada depende de D-007.
  - Respuesta del dueño a la pregunta bloqueante 2 del plan de la fase (2026-10-08, transmitida por la sesión principal): AF.Platform, AF.Save, AF.Ads y AF.IAP se crean **solo con interfaces e implementaciones simuladas**. Sin SDK, sin cambios en `Packages/manifest.json` ni `packages-lock.json`, sin permisos de Android, sin claves y sin red. El dueño revisa el resultado al fusionar.
  - Documentación aplicada el 2026-10-08 en los capítulos de «Archivos afectados», en `117f4be`, dentro de la rama de código `fase2-bucle-partida`.
    - El plan de la fase dejaba estas ediciones del GDD (Dc2-Dc6) para después de las respuestas a P13-P16, como lote aprobado por el dueño. Por eso quedan **pendientes de su aprobación explícita antes de fusionar**. Si no las aprueba en el PR de la pista B, van en un PR de documentación aparte.
    - Qué tiene que aprobar:
      - el contrato 7 (04 §5.2): desde el 31, salir del nivel con el menú es fallo duro; cerrar la app sigue sin serlo;
      - 04 §6 y §7: el embudo con ¿Seguro? y Nivel fallido, que sustituye «Máximo un modal por bloqueo» (§6.6), y «Qué consume» (§7.1);
      - 08 §2.2, §2.3 y §8.2, y la columna «Uso» de tres filas de §9 (contrato 13), sin claves ni defaults nuevos;
      - 00, 02, 05, 06, 07, 09, GDD.md y la nota de respuestas de la propuesta, sin cifras nuevas.
    - Queda abierta una contradicción del GDD que estas ediciones no resolvieron: cuándo se ofrece «Canoas infinitas 1 h» en Sin canoas (P17).
- **Sustituye:**
  - en 04 §6.6, la regla «Máximo un modal por bloqueo; nunca se apila un segundo pop-up»;
  - en 04 §5.2 (contrato 7), la fila «Salir al Mapa del río … no es fallo», solo cuando se sale con el menú (cerrar la app sigue sin ser fallo);
  - 04 §7.1, «Qué consume»;
  - desde el nivel 31, el pie «Volver a la orilla (gratis)» del modal (04 §2.7; glosario §5.1);
  - los pasos 2 y 3 que la propuesta §6.1-§6.2 marcaba como «no copiamos».
- **No sustituye:**
  - precios y cifras (05 §2, 08 §9);
  - la misma semilla al reintentar (02 §2; GDD.md §2 #11);
  - Marea como primera opción del modal (02 R15; 04 §6.2, regla 0);
  - ninguna oferta de pago antes del 31 (05 §10);
  - intersticiales solo tras victoria (05 §6.1).

**Contexto.**
- **Magic Sort** (ficha §4.3-§4.7 y §6):
  - «No Available Moves»: Play On por 900 o Give Up;
  - «Continue?»: «You will lose 1 life!», con Play On por 900 otra vez;
  - «Level Failed!»: el premio perdido y Try Again;
  - con 0 vidas, «More Lives»: Refill por 900 o +1 Life por anuncio, con temporizador;
  - salir del nivel también cuesta una vida («Are You Sure?»);
  - tras la victoria, una cadena de pantallas;
  - las vidas ilimitadas no quitan los intersticiales.
- **Nuestro canon:** un solo modal con Marea → Cuia de rescate → Volver a la orilla (04 §6), y canoas desde el 31 (04 §7).

**Decisión.**
1. **Embudo, paso a paso.** Cifras de 08 §9. «≥ 31» = `rescue_min_level`.
   - **Paso 0 · Detección.**
     - Cuándo: `atascado(S)`, con los fútiles (R13).
     - Qué pasa: la entrada se bloquea al instante. El modal se abre `stuck_modal_delay_ms` = 400 ms después de la última animación (R15).
   - **Paso 1 · Aguas estancadas** («No hay más vertidos»).
     - El tablero queda atenuado detrás; arriba, semillas y canoas; el HUD de partida y la bandeja se ocultan.
     - **(a) Marea primero** (04 §6.2, regla 0):
       - Deshacer, si hay cupo o stock (ilimitada en 1-10; 3 por nivel desde el 11);
       - si no, la recarga: «+3 Deshacer · Ver video» (`rv_undo_refill`: 4 al día, 60 s) o «+3 · 30 semillas».
     - **(b) Play On, desde el 31:** «Continuar: +1 cuia» (Cuia de rescate).
       - 600 semillas, o 300 en el primer rescate del día (`rescue_first_day_mult` 0,5).
       - Máximo 1 por nivel.
       - Solo se ofrece si un A* de 40.000 nodos sobre el estado + 1 cuia confirma que la compra cumple (04 §6.4).
       - Sin saldo, el botón sale en gris con «Te faltan {n} semillas», junto a:
         - Puñado, 1,99 (COP 3.900, 1.000 semillas);
         - Balsa de rescate, 4,99 (COP 9.900): 1 al día, oculta 48 h tras 2 rechazos, orden T3.
     - **(c) Pie:**
       - en 1-30, «Volver a la orilla (gratis)» reinicia sin coste (rama C de 04 §6.3);
       - desde el 31, «Rendirse» [P] lleva al paso 2.
     - Bajo el titular, la línea de canoa (`stuck_canoe_lost` o `stuck_canoe_lost_last`), oculta con Canoas infinitas.
   - **Paso 2 · ¿Seguro?** [P]. Desde el 31, tras «Rendirse».
     - Mensaje: «Perderás 1 canoa» («tu última canoa» si solo queda una).
     - Vuelve a ofrecer «Continuar: +1 cuia», al mismo precio y con las mismas condiciones; si el rescate no está disponible, no aparece.
     - La X y «Rendirse» confirman la rendición.
     - Con Canoas infinitas no hay canoa en juego y se pasa directo al paso 3 [P].
   - **Paso 3 · Nivel fallido** [P]. Tras confirmar la rendición.
     - Muestra «Nivel {n}» y «Premio: {20 · 60 · 80} semillas», según la etiqueta (`eco_reward_*`); ese premio no se cobra.
     - Botones: «Reintentar» [P] y X, que vuelve al inicio.
     - Al entrar se resuelve el fallo duro (04 §5.2 y `failHard` de 04 §6.2):
       - `fails_level` + 1;
       - desde el 31, −1 canoa, salvo con Canoas infinitas;
       - la Crecida, cuando exista, se rompe salvo con Hoja escudo.
     - «Reintentar» carga el mismo tablero, con la misma semilla.
     - Al tercer fallo carga `relief[0]` con el cartel «El río bajó», de 2.000 ms (`anim_river_lowered_banner_ms`; 03 §13).
   - **Paso 4 · Sin canoas** (04 §7.3; «Más canoas» es como lo llamó el dueño, pregunta P14).
     - Se abre con «Reintentar», «Jugar» o «Siguiente» sin canoas, desde el 31.
     - Opciones:
       - cronómetro «Tu próxima canoa vuelve en mm:ss»: 1 canoa cada 25 min, máximo 5;
       - «Canoa extra · Ver video», +1 (`rv_extra_canoe`: 3 al día, 90 s);
       - «Llenar canoas · 200 semillas», hasta 5/5;
       - «Canoas infinitas 1 h» (`af_infinite_canoes_1h`: 1,99, COP 3.900).
         - *Nota del 2026-10-08: cuándo se ofrece está pendiente de P17. 04 §7.3 la pone siempre, y así lo hace `NoCanoesScreen`; 05 §4.1 y §5, solo tras rechazar dos veces la Canoa extra en el día. El código no cambia hasta la respuesta.*
     - En cuanto hay una canoa, vuelve a «Nivel fallido» con «Reintentar», como Magic Sort (t = 338) [P].
   - **Salir.** Desde la pausa, «Salir».
     - Desde el 31: «¿Salir del nivel? Perderás 1 canoa» [P] → fallo duro → «Nivel fallido» → inicio. Cuesta la canoa aunque no haya vertidos, como Magic Sort en t = 5 [P] (pregunta P16).
     - En 1-30 se sale sin coste.
     - Cerrar la app o un cuelgue no es fallo: el estado se guarda y se reabre tal cual (04 §5.2; 02 §14).
2. **Reiniciar por voluntad propia** no cambia: Volver a la orilla en el HUD o en la pausa, fuera del bloqueo, sigue siendo gratis.
   - Con ≥ 8 vertidos es un fallo blando: cuenta para «El río bajó» y para el Salto del Boto, pero no gasta canoa (04 §2.7 y §5.2).
3. **Victoria con una cadena corta.**
   - Celebración de 900 ms (`anim_celebration_ms`).
   - Pantalla de victoria (02 §6):
     - «¡Nivel completado!»;
     - Cosecha de +20, +60 o +80 semillas, acreditadas con un contador de 400 ms;
     - desde el 11, «Cosecha doble: x2 por video» (`rv_double_harvest`: 5 al día, 60 s); sin anuncio disponible, no aparece.
   - «Siguiente», sin auto-avance (`victory_autonext_s` = 0) → AD_GATE → nivel N+1.
   - «Inicio» → pantalla de inicio [P].
   - Como mucho 1 takeover por sesión.
   - Sin el calificativo «¡Perfecto!»: la pregunta 10.2 #25 sigue abierta.
4. **Anuncios simulados detrás de AF.Ads**, sin SDK.
   - **Intersticial**, solo tras tocar «Siguiente» (05 §6.1 y §6.3):
     - nunca antes del nivel 22 ni en la sesión de instalación;
     - cada 3 niveles en 22-60 y cada 2 desde el 61;
     - ≥ 150 s desde el anterior y ≥ 120 s desde el inicio de la sesión;
     - como mucho 6 por sesión y 12 al día;
     - nunca en las 24 h siguientes a una compra ni a < 60 s de un recompensado.
   - **Nunca** al reintentar, al rendirse, en «Nivel fallido» ni en «Sin canoas», aunque Magic Sort lo haga (05 §10).
   - **Recompensados:** Marea, Canoa extra y Cosecha doble, con sus topes por ubicación y 15 al día en total.
   - **Las Canoas infinitas no quitan los intersticiales** (postura del dueño, 2026-10-08).
     - Solo los corta la regla de 24 h tras cualquier compra, que también se aplica a `af_infinite_canoes_1h` (05 §4.1).
5. **Compras simuladas detrás de AF.IAP**, sin Play Billing.
   - Los 13 SKU de 05 §4.1, con los precios locales de 05 §4.2.
   - Lo comprado se acredita con `Wallet.Apply`.
   - Una compra fallida o cancelada nunca descuenta.
6. **Pantalla de inicio** (06 §4.7), con la composición de Magic Sort (ficha §7.1: saldos arriba, botón central, pestañas abajo) y sin su trade dress (07 §1.3; D-003).
   - Botón grande «Jugar · Nivel {n}», con el chip Difícil o Muy difícil cuando toque [P].
   - Semillas.
   - Canoas «n/5» con cronómetro, desde el 31.
   - Ajustes.
   - Mercado, Mapa del río, Reto del río y Bitácora quedan ocultos hasta que existan.
7. **Reintentar con otra distribución, como Magic Sort (ficha §2.9): no se adopta.** El GDD no lo permite (02 §2; GDD.md §2 #11; 05 §10, «cambiar el tablero sin avisar»). La pregunta 10.2 #20 sigue abierta.

**Consecuencias.**
- **Más presión y más ocasiones de compra:** una segunda oferta de rescate y la pérdida a la vista.
  - El ratio de presión y las cifras [sim] de 05 §3 no modelan la segunda oferta ni el coste de salir (contrato 18).
  - Se mantienen como referencia y se recalibran con el embudo real del soft launch [P].
- **Máquina de estados (02 §2):** nuevos estados GIVE_UP_CONFIRM, LEVEL_FAILED y NO_CANOES. FAIL_RESOLVE se dispara al confirmar la rendición.
- **Analítica (08 §8.2):** `af_stuck_choice` gana los valores `give_up`, `confirm_play_on` y `quit` [P].
- **Textos nuevos** [P] (pregunta P14):

  | Clave | Texto |
  |---|---|
  | `stuck_give_up` | Rendirse |
  | `giveup_confirm_title` | ¿Seguro? |
  | `giveup_confirm_canoe` | Perderás 1 canoa |
  | `level_failed` | Nivel fallido |
  | `level_failed_reward` | Premio: {n} semillas |
  | `retry` | Reintentar |
  | `quit_confirm` | ¿Salir del nivel? Perderás 1 canoa |

- **Persistencia:** el nivel en curso se guarda tras cada vertido (02 R8 y §14), para que cerrar la app no sea una salida gratis.
- **Remote Config:** sin claves nuevas en 08 §9.

**Alternativas descartadas.**
- Un solo modal y reinicio en 1 toque (04 §6.6; propuesta §6.1).
- Otra distribución en cada reintento: el GDD no la permite.
- Intersticial tras fallar o que corta la celebración (Magic Sort, t = 196 y 629).
- Que las Canoas infinitas quiten los anuncios.
- Quitar Marea del modal para igualarlo a Magic Sort (pregunta P15).

**Cómo revertir.**
- Revertir los commits de B2-2 y B2-3: el modal único de 04 §6.2 vuelve.
- Una constante de desarrollo, `GameConfig.StuckFunnel` [P], conserva la variante de un solo modal. No es una clave de Remote Config.
- Siempre con una decisión nueva que sustituya a D-008.

**Archivos afectados.**
- **Unity:**
  - `Assets/_Project/Scripts/{Platform,Save,Economy,Ads,IAP,Boosters,GameFlow,Levels,UI,Presentation}`;
  - `Assets/_Project/Data/Config/{iap_catalog,ad_placements}.json`;
  - `Assets/_Project/Tests/**`.
- **Documentación** (aplicada el 2026-10-08 en `117f4be`, pendiente de la aprobación explícita del dueño antes de fusionar, ver «Estado»; entre paréntesis, lo que se tocó además de la lista original):
  - 02 §2 y §7 (y §6, §14 y «Preguntas abiertas» 2);
  - 04 §2.7, §5.2, §6 y §7 (y §3, §4, §10.1, §11 y las secciones finales);
  - 05 §5 y §6.1 (y §2.4, §3.4, §6.3 y §10);
  - 06 §4.7 (y §4.3 y «Preguntas abiertas» 1);
  - 07 §5.5 y §7.5;
  - 08 §2.2 y §8.2 (y §2.3 y la columna «Uso» de tres filas de §9, sin claves nuevas);
  - 00 §2.2 y §10 (y §5.1, §6.3 y §6.4);
  - GDD.md §2 (#11, #14 y #15, y #17), §4 y §6.2 (contrato 7), con una nota en §7.1 y §9;
  - 09 §2.3 (y §1.1, filas #10, #13, #14 y #22);
  - la nota de respuestas de `docs/research/magic-sort/propuesta-tintalu.md`.
  - Nota de P17 (2026-10-08): 04 §7.3 y «Preguntas abiertas» 9; 05 §4.1, §5, «Conflictos detectados» y «Dependencias» (fila 04); y la fecha de cabecera de los dos. Solo señalan la contradicción; no cambian ninguna regla.
  - Pendientes: el resumen de la definición de fallo de 03 §13, la línea de decisiones de `CLAUDE.md` y, tras la respuesta a P17, alinear 04 §7.3 o 05 §4.1 y §5 (lote Dc4).

**Preguntas para el dueño** (no bloquean).

| # | Pregunta | Recomendación |
|---|---|---|
| P14 | ¿Apruebas los textos nuevos y que la pantalla de 04 §7.3 siga llamándose «Sin canoas» en vez de «Más canoas»? | Sí |
| P15 | ¿Se mantiene Marea como primera opción del modal (02 R15; 04 §6.2), aunque Magic Sort no la tenga? | Sí |
| P16 | Desde el 31, ¿salir del nivel cuesta una canoa también con 0 vertidos, como Magic Sort (t = 5)? | Sí |
| P17 | ¿Cuándo sale «Canoas infinitas 1 h» (`af_infinite_canoes_1h`, 1,99) en Sin canoas? El GDD se contradice. **A:** siempre, detrás de esperar, el video y las semillas, y sin cuenta atrás: así lo dibuja 04 §7.3, la listan sin condición 02 §2, 06 §4.7 y el paso 4 de esta decisión, y así lo hace ya `NoCanoesScreen`. **B:** solo al quedarse sin canoas tras rechazar la Canoa extra dos veces en el día, con 1 impresión contextual al día que caduca a los 10 min: así lo dicen 05 §4.1 y §5; habría que definir qué cuenta como «rechazar» y cambiar el código. En los dos casos sigue en el Mercado | A [P]: es lo que dicen los demás capítulos y lo que hace el código, y la caducidad de 10 min de 05 §5 choca con «Sin cuenta atrás de oferta» de 04 §7.3. En contra: Magic Sort no vende vidas ilimitadas en «More Lives» (ficha §4.5). Con A se corrigen 05 §4.1 y §5; con B, 04 §7.3, 02 §2, 06 §4.7, este paso 4 y el código. Se alinea en el lote Dc4; hasta la respuesta, el código no se toca |

---

## D-010 · «Llenar y teñir»: el río llena las cuias y los miquitos las tiñen con fruta

- **Fecha:** 2026-10-08 (idea del dueño y su ampliación, y guion de la intro del mismo día). Registrada el 2026-10-09.
- **Estado:** decidida por el dueño, con el guion `docs/research/intro-rio-miquitos/guion.md` aprobado para implementar. Implementada en la rama `intro-rio-miquitos` (`fbdf11e`, con las revisiones `e479cf0` y `8955ef3`), **sin fusionar en `main`**.
  - Las 16 preguntas del guion (§18) no tienen respuesta explícita una a una. La implementación aplica la recomendación de cada una, salvo Q11 y Q14. Aquí quedan como **recomendación aplicada, pendiente de confirmación** (tabla del final): no bloquean y el registro no elige por el dueño (CLAUDE.md, regla 10).
  - **Toda la fauna, los nombres comunes, los datos de las tarjetas y la palabra «miquitos» (y su traducción) quedan pendientes de revisión del asesor cultural** (07 §8.2).
  - La propuesta de fauna (`docs/research/fauna/propuesta-fauna.md`) queda como **material de apoyo**, no canon, con sus preguntas abiertas (F1-F14; estado al final). Donde choca con el guion, manda el guion (punto 7).
  - Numeración: D-007 (mecánicas, debuts) y D-009 (dificultad escalonada) son de la pista de mecánicas y niveles (rama `fase1-mecanicas-niveles`, `tools/level-generator/README.md`), que todavía no ha traído sus entradas a este registro. El código de esta rama cita la intro como «pendiente de D-009» (`IntroAnimation.cs`, `MonkeyRig.cs`, `TroopLayer.cs` y `SightingCard.cs`): debe decir D-010.
- **Sustituye:**
  - «Sumergir y teñir» (02 §4.2, 07 §5.1, 08 §5.3 y glosario §6.4): la caída de las cuias (fase a), el agua de inmersión por fila (guía visual v2, 6.11) y «un icono de fruta por dosis desde fuera de pantalla»;
  - la contradicción del orden del teñido (02 §4.2: «de abajo arriba»; 07 §5.1: «la de arriba primero»; el código teñía de arriba abajo): dentro de la cuia, de abajo arriba;
  - en Aguas turbias, «la dosis oculta recibe una gota parda y el ?» (07 §5.1);
  - `sfx_submerge` y `sfx_emerge` (07 §9.2).
- **No sustituye:**
  - la dirección 18 y sus claves de 08 §9 (`anim_tint_duration_ms_early` 1.400, `anim_tint_duration_ms_late` 800, `anim_tint_fast_from_level` 21, `anim_tint_skippable`, `anim_tint_skip_alarm_pct` 0,40): **sin claves, defaults ni tiempos nuevos**;
  - las fases a-d como cortes (300 + 400 + 250 + 450 ms y 150 + 250 + 150 + 250 ms);
  - el nivel en memoria antes de animar, el salto al primer toque y `af_anim_skipped` con `anim = tint`;
  - el fundido de 0,25 s con Reducir movimiento y el banner de mundo antes de la intro;
  - D-002: ningún miquito mira, señala ni reacciona a una cuia.

**Contexto.**
- **Idea del dueño (2026-10-08), literal:** «Quiero que plasmemos una idea única. El río Amazonas llena los recipientes: debemos hacerlo más visual, que se entienda mejor que eso pasa; y para poder pintar o revelar los colores, que unos miquitos tiren unas semillas de diferentes frutas autóctonas del Amazonas y los pinten de ese color; que sea muy visual y rápido. Investiguemos sobre ello y hagámoslo muy bien logrado.»
- **Ampliación del mismo día, literal:** «Podemos, a medida que suba de niveles, que diferentes miquitos de especies vayan apareciendo. Podemos explorar fuertemente la fauna del Amazonas para tener algunas cosas visuales bonitas y que hagan algo también.»
- **Lo que había:** «Sumergir y teñir», implementado en `IntroAnimation.cs` con `ImmersionWater.cs` (capturas `01_intro_l7` y `01b_intro_tint_l7`). El agua de inmersión nacía en la cara de la repisa sin un camino visible desde el río, así que no se entendía que el río llena, y la fruta caía «desde fuera de pantalla».
- **Método del dueño** (D-005): copiar lo que funciona y darle nuestra temática, aquí con base real, investigada y respetuosa.
- **Material:**
  - el guion (síntesis de dos guiones evaluados por jueces, con fuentes [V] y [E] en su §21);
  - la propuesta de fauna (elenco, 18 animales «que hacen cosas» y página Fauna del Cuaderno de campo);
  - `docs/research/cultura-ribera/investigacion.md` (pulso de inundación, F6).

**Decisión.**
1. **Nombre: «Llenar y teñir»** (EN «Fill & Tint» [P]; PT-BR «Encher e tingir» [P], con el asesor).
   - Sustituye a «Sumergir y teñir» en el glosario §6.4 y en todos los capítulos, y conserva el verbo de la marca (Tintalú, el tinte que colorea el agua).
   - Código: la clase `IntroAnimation` se mantiene; se añaden el planificador puro `IntroChoreography`, el catálogo `FaunaCatalog` y su resolución `IntroCast`. La analítica no cambia.
2. **El río llena, y se ve** (guion §6; canon en 02 §4.2 y 07 §5.1).
   - Las cuias están vacías y secas en su ranura desde el fotograma 0: no caen.
   - El río del fondo crece: se aclara y sus destellos se deslizan hacia el panel, sin cresta ni frente que avance (eso sería la Pororoca, comodín Barajar).
   - Desborda el canto superior del panel y corre por la madera del marco y de las repisas.
   - Un chorrito por cuia no vacía, desde la madera de encima, llena la cuia desde el fondo **solo hasta sus dosis**, con la gramática del vertido (D-006). Las vacías quedan secas.
   - Causalidad: el chorrito se abre ≤ 30 ms después de que el agua pase sobre el eje de la cuia, y el llenado empieza ≤ 50 ms después del contacto. Todo lleno antes de 700 / 400 ms.
   - El vaciado es vertical, nunca una ola lateral (icono de Marea).
3. **La fruta tiñe** (guion §5 y §7).
   - **Una fruta por racha visible**, como mucho 24. Con más rachas, una por cuia no vacía y la cuia florece de abajo arriba en cascada (25 ms entre rachas).
   - **Fruta, no semilla:** la semilla ámbar ovalada es el icono de la moneda (glosario §2.1). La forma en vuelo es de la familia del icono de accesibilidad del tinte (07 §3). Casos especiales: el achiote suelta semillitas rojas y angulosas; el Agua del Río Negro es una hoja seca que se hunde; el Agua del Tapajós, 3 gotas de una hoja sacudida. Nunca un grano de cacao suelto. Tabla por tinte en 07 §5.1.
   - Al tocar el agua, anillo en la superficie y floración OKLab de 100 ms desde el agua del río, igual para todos los tintes.
   - **Orden:** filas de arriba abajo; dentro de la fila, por pasadas; **dentro de la cuia, de abajo arriba** (Q4). La última floración termina exactamente en T.
   - **Aguas turbias:** las ocultas no reciben fruta ni nota y su «?» aparece en todas a la vez con el primer impacto. El calendario no depende del color real de una oculta.
4. **Los miquitos cambian de especie con el progreso** (ampliación del dueño; catálogo en 07 §6.4).
   - **Las especies son datos:** catálogo `Assets/_Project/Data/Fauna/miquitos_v0.json` (formato `af-fauna/0`) con esqueleto (A-D), modelo de suelta (`fila`, `cuelga` o `sacude`), paleta, tropa, rasgo de reposo, tramo de niveles, nombres, dato de la tarjeta y ranuras de arte. El código no conoce ninguna especie, y cambiar o añadir una no toca código. Sin Remote Config.
   - **Una especie por mundo, de géneros distintos**, porque los grandes ríos separan especies (Wallace 1852 [V]): mono ardilla (1-10), sagui plateado (11-40), sauim-de-coleira (41-80), churuco (81-140), huapo colorado (141-200), cotoncillo colorado (201-300), pichico emperador (301-420), maquisapa (421-700; recomendación de Q8), mono nocturno (701-820), saquí de cara blanca (821-920), mono ardilla de los Llanos como variante (921-960) y **sin miquitos en El origen** (961-1.000).
   - **Nunca la especie de otro mundo.** Pasa a **modo dosel** (sin tropa: el follaje se sacude y la fruta cae por detrás del HUD) en estos casos:
     - reintentos;
     - mundo 13;
     - sin franja libre de 44 dp entre el HUD y el panel (3 filas, pantallas de 640 dp);
     - especie sin arte final en un build de release.
   - La especie cambia **qué** se ve, nunca **cuánto** dura.
   - Tres modelos de suelta en lugar de un gesto a medida por especie. La identidad va en la silueta y en el rasgo de reposo.
5. **Tarjeta «¡Miquito a la vista!»** [P] (Q10).
   - Pone nombre a la especie **después** de la intro, en el primer nivel libre desde su «tarjeta desde» (07 §6.4), solo en primer intento.
   - Dura 2,5 s, no bloquea el toque y no tapa cuias ni botones. Una tarjeta por nivel, con prioridad tutorial → Fruta nueva → miquito.
   - Lenguaje de avistamiento: «a la vista», nunca «nuevo», «tu mono» ni «adoptar». Sin confeti.
   - Clave `fauna_sighted_card` (glosario §10).
6. **Representación: reglas duras** (guion §4.6; 07 §6.4).
   - Siempre libres, en su árbol y sin personas. Sin rasgos humanos ni pintura corporal.
   - No son guías: no hablan, no presentan el banner y no dan consejos.
   - No cruzan el río.
   - Las especies amenazadas hacen el mismo gesto neutro que las demás y nunca son premio.
   - La máscara del mono ardilla no se lee como calavera. Nombres vetados.
7. **Fauna fuera de la intro: no se decide aquí.**
   - La propuesta de fauna queda como material de apoyo con sus preguntas abiertas:
     - 18 animales «que hacen cosas»: ambiente con microinteracciones, celebraciones y presentación de mecánicas;
     - la página Fauna del Cuaderno de campo;
     - el puente en el Mapa del río.
   - **Donde choca con el guion, manda el guion:**
     - «¡Miquito a la vista!» en lugar de «¡Nuevo miquito!»;
     - modo dosel en lugar de la especie del mundo anterior cuando falta el arte;
     - sin lianas laterales con 3 filas (rompían la distancia de 8 dp a las cuias);
     - el avistamiento se registraría al mostrar la tarjeta, no al hacer la intro;
     - tres modelos de suelta en lugar de nueve gestos;
     - el maquisapa en lugar del fraile en los mundos 8-9 (Q8).

**Consecuencias.**
- **Tiempos y Remote Config:** sin cambios (contrato 13). Si Remote Config cambia T (1.200-1.500 o 600-1.000 ms), todo el calendario se escala por T / 1.400 o T / 800. La versión larga o corta la decide la clave, no T.
- **Código** (ya en la rama; lista en «Archivos afectados»):
  - un planificador puro y determinista calcula el calendario una vez, en arrays preasignados (0 B por fotograma);
  - todas las capas de la intro van en Canvas anidados propios con `raycastTarget = false`;
  - se borra `ImmersionWater.cs`;
  - la escena se regenera con `GameSceneBuilder.CreateGameScene`.
- **Arte** (guion §12; 07 §6.4):
  - el artista dibuja las piezas por especie sobre 4 esqueletos (Q16), además de los retratos, 14 frutas, la rama y la liana;
  - coste: unos 30 días para las especies 1-5 [E], con prioridad para el mono ardilla;
  - mientras no haya PNG, miquito procedural solo en builds de desarrollo. El vídeo de la tienda y el soft launch no se graban con provisionales.
  - Las plantillas por esqueleto que deben preparar los agentes en `docs/art/` no existen todavía.
- **Audio:** `sfx_river_rise`, `sfx_fill` y `sfx_fruit_plip` [P] sustituyen a `sfx_submerge` y `sfx_emerge` (07 §9.2). Hoy no hay audio en el código; lo encarga el dueño (07 §9.5).
- **Analítica:** sin cambios en 08 §8.2. `af_fauna_sighted` queda reservado y **no se emite** hasta Q11, que exige la aprobación del dueño, una migración del perfil (`ProfileMigrations`) y la entrada en 08 §8.2.
- **Accesibilidad:** el ajuste Reducir movimiento todavía no existe en el perfil. La ruta ya está en el código: `LevelFlow.ReduceMotion` y `GameConfig.ReducedMotionIntroMs` = 250, que no es una clave de 08 §9.
- **Riesgos** (guion §15):
  - densidad de la versión larga (R1);
  - con 3 filas no se ve el río (R3);
  - repetición en tramos largos (R5), que mitiga la variación determinista por nivel;
  - representación (R8);
  - arte sin encargar (R9);
  - rendimiento en clase A sin dispositivo para medir (R11).
  - La prueba de pasillo con cinco personas (A11) sigue pendiente. Si falla con 3 filas, el respaldo es la cortina de agua (guion §6.3).

**Alternativas descartadas** (guion §0.2 y §18).
- Mantener «Sumergir y teñir»: ya no describe lo que pasa y no se ve que el río llena.
- «Río y fruta» como nombre: rompe el par de verbos.
- Inundación por fila desde los lados: no puede llenar una cuia cuya boca queda por encima.
- La cortina desde arriba como camino principal: queda de respaldo con 3 filas.
- Un frente que avanza por el río: se lee como la Pororoca.
- Una fruta por dosis: hasta 64 en 250 ms con 16 cuias.
- Semillas en lugar de fruta: chocan con el icono de la moneda.
- Solo monos ardilla con otra paleta: el jugador no nota el cambio.
- Nueve gestos a medida: no se perciben en 150 ms.
- La especie del mundo anterior cuando falta el arte: contradice la barrera de los ríos.
- «¡Nuevo miquito!»: suena a adquisición, y el elenco tiene especies amenazadas (sauim CR; churuco y huapo VU; maquisapa EN).
- Lianas laterales con 3 filas.
- Una versión de estreno de 1,8 s en el nivel 1: exigiría una clave nueva (Q12).

**Cómo revertir.**
- Revertir `fbdf11e`, `e479cf0` y `8955ef3` y el commit de documentación de esta entrada:
  - `ImmersionWater.cs` y la intro anterior de `IntroAnimation.cs` vuelven de `b137533`;
  - la escena se regenera con `GameSceneBuilder.CreateGameScene`;
  - el texto de 02 §4.2, 07 §5.1 y §9.2, 08 §5.3, el glosario y la guía visual vuelve a su versión anterior.
- Quitar solo la tropa y dejar el río y la fruta (modo dosel siempre; fase 2 del guion, §19) **no tiene interruptor hoy**: exige un cambio de código.
- Siempre con una decisión nueva que sustituya a D-010.

**Archivos afectados.**
- **Código, datos y tests** (rama `intro-rio-miquitos`, `fbdf11e`, `e479cf0` y `8955ef3`):
  - `Assets/_Project/Data/Fauna/miquitos_v0.json`;
  - `Assets/_Project/Scripts/GameFlow/{GameConfig.cs, LevelFlow.cs, SightingRule.cs}`;
  - `Assets/_Project/Scripts/Levels/{FaunaCatalog.cs, FaunaCatalogReader.cs, WorldTable.cs}`;
  - `Assets/_Project/Scripts/Presentation/{IntroAnimation.cs, IntroChoreography.cs, WaterStripGraphic.cs, BezierStripGraphic.cs, TroopLayer.cs, MonkeyRig.cs, ProceduralArt.Fauna.cs, ProceduralArt.Fruit.cs, GraphicPool.cs, ShaderClock.cs, AmbientLayer.cs, ArtSlots.cs, CuiaView.cs, LiquidGraphic.cs, ProceduralArt.cs}` (se borra `ImmersionWater.cs`);
  - `Assets/_Project/Scripts/UI/{GameScreen.cs, SightingCard.cs, UiText.cs, GameBootstrap.cs, LaunchOptions.cs, DevProbe.cs, Editor/GameSceneBuilder.cs}`;
  - `Assets/_Project/Shaders/Resources/{AF_UI_Liquid.shader, AF_UI_Stream.shader}`;
  - `Assets/_Project/Editor/CI/GeneratedArt.cs` y `Assets/_Project/Art/Generated/README.md`;
  - la escena `Assets/_Project/Scenes/Game.unity`, regenerada;
  - tests EditMode (`FaunaCatalogTests`, `IntroArtTests`, `IntroChoreographyTests`, `SightingRuleTests`, `ArtExportTests`) y PlayMode (`IntroTests`, `VisualSnapshotTests`, `PlayModeHelpers`).
- **Documentación** (aplicada el 2026-10-09 en la misma rama):
  - 02: cabecera, §2 (el estado `INTRO(llenar_y_tenir)`) y §4.2;
  - 07: cabecera, §2.2 (regla 5), §5.1, §6.3, §6.4 (nueva), §9.2, «Desviaciones» (3) y «Preguntas abiertas» 11;
  - 00: cabecera, §1.2, §3.3, §5.3 (alias), §6.4, §10 (`fauna_sighted_card`) y §11 (fauna);
  - 08: cabecera, §5.3 y el nombre en la columna «Uso» de §9, sin claves nuevas;
  - GDD.md §2 (decisión 22);
  - la guía visual v2 (revisión 5, tabla de capas, exclusión del panel, 6.11 y C9);
  - este registro;
  - solo el nombre en 01, 06, 09 y en el resto de GDD.md, 02, 07 y 08; en 04 §2.7, el nombre y una nota que remite a Q17.
  - Los históricos y el material de apoyo no se tocan: `docs/research/`, `docs/reviews/` y `docs/dev/prototipo-v0.md`.
- **Pendientes de documentación** (necesitan una respuesta del dueño o del asesor):
  - 06 §4.6 y el glosario §3.3: la página Fauna del Cuaderno de campo con los miquitos (F7);
  - 08 §8.2: `af_fauna_sighted` (Q11);
  - 07 §11: los días de arte de los miquitos y las frutas;
  - 07 §2.2: la columna «Fauna de ambiente» con la especie y los animales de ambiente de cada mundo (F9);
  - 07 §6.2: si Rosa salta o solo asoma (F11);
  - 04 §2.7 y el glosario §3.3: el hoatzin del mundo 11 (Q17);
  - la línea de decisiones de `CLAUDE.md`;
  - los comentarios «pendiente de D-009» del código.

**Preguntas para el dueño.** No bloquean la rama. Q1-Q16 son las del guion (§18), con su numeración; Q17 la añade este registro.

| # | Pregunta | Recomendación | Estado en el código | Bloquea |
|---|---|---|---|---|
| Q1 | Nombre de la animación | «Llenar y teñir» | Aplicada (y en el GDD) | — |
| Q2 | Cómo entra el agua | El río crece, desborda, corre por la madera y cae un chorrito en cada cuia | Aplicada | — |
| Q3 | ¿Se quita la caída de las cuias? | Sí | Aplicada | — |
| Q4 | Orden del teñido dentro de la cuia | De abajo arriba, con las filas de arriba abajo | Aplicada (02, 07 y 08 alineados) | — |
| Q5 | ¿Una fruta por racha o por dosis? | Por racha, como mucho 24; si no, una por cuia con cascada | Aplicada | — |
| Q6 | ¿Qué sueltan los miquitos? | La fruta del tinte; hoja en el Río Negro; gotas en el Tapajós; semillitas rojas solo en el achiote | Aplicada | — |
| Q7 | ¿Qué miquitos van apareciendo? | Géneros distintos por mundo (07 §6.4) | Aplicada | Encargo de arte |
| Q8 | Especie de los mundos 8-9 (421-700) | Maquisapa (*Ateles chamek*, EN), tras confirmar su presencia en el Madidi y su nombre por país. Alternativa: fraile (*Saimiri boliviensis*), del mismo género que el mono del nivel 1 | Aplicada (maquisapa en el catálogo) | No bloquea la 1.0 si Q9 es A |
| Q9 | ¿Cuántas especies en la 1.0, y qué pasa sin arte? | Las especies 1-5. Un mundo sin arte final va en modo dosel, nunca con la especie de otro | Aplicada la regla del modo dosel; las especies de la 1.0 dependen del encargo | Encargo al artista (unos 30 días [E]) |
| Q10 | Texto de la tarjeta | «¡Miquito a la vista!» | Aplicada | Texto final con el asesor |
| Q11 | ¿Cuándo se registra el avistamiento? | `af_fauna_sighted` al mostrar la tarjeta, desde la 1.0 | **No aplicada**: la tarjeta sale, pero no se guarda ni se emite nada. Exige tu aprobación del evento (08 §8.2) y una migración del perfil, en un PR aparte | Cuaderno de campo (1.1) y el puente en el Mapa del río |
| Q12 | Duración del nivel 1 | 1.400 ms como el resto de 1-20; medir primero. Solo si la prueba de cinco personas lo pide, una versión de estreno de 1.800 ms con una clave nueva | Aplicada (1.400 ms) | — |
| Q13 | Reducir movimiento con especie | El miquito aparece quieto durante el fundido y 600 ms más | Aplicada en la ruta (el ajuste aún no existe) | — |
| Q14 | Agua negra (mundos 3 y 10) | Ámbar-té translúcido que mantenga los 29 puntos de L* frente al tinte 14, tras una captura de prueba | **No aplicada**: el preset sigue en #4A2C14 al 70 % | Antes del mundo 3 (nivel 41) |
| Q15 | Estilizaciones de color y forma (bacaba rosa, guanábana, lulo, icono bol del açaí) | Mantener el canon como estilización, sin Datos del río que la afirmen; en vuelo, ramita de açaí. Revisar los tintes con el asesor para el Cuaderno | Aplicada | — |
| Q16 | Técnica de animación de los miquitos | Rig recortable en uGUI: piezas sobre una plantilla por esqueleto y animación por código | Aplicada (rig procedural de 8 piezas) | Encargo al artista y plantillas en `docs/art/` |
| Q17 | En el mundo 11, el hoatzin hace la animación de Volver a la orilla (glosario §3.3; 04 §2.7: «la animación la ejecuta el hoatzin»). ¿Solo vacía las cuias, o también suelta la fruta en la intro corta del reintento? | Solo el vaciado. La intro del reintento va en modo dosel, sin tropa, como en todos los mundos | Sin hoatzin en el código | Mundo 11 (nivel 821) |

**Preguntas de la propuesta de fauna** (material de apoyo; sección f). Siguen abiertas las que el guion no resuelve:

| # | Pregunta | Recomendación de la propuesta | Estado |
|---|---|---|---|
| F7 | ¿Se amplían el glosario §3.3 y 06 §4.6 con la página Fauna del Cuaderno de campo («Miquitos» y «Río y selva»)? | Sí, tras el asesor | Abierta (06 §4.6 sigue con 12 animales del elenco) |
| F9 | ¿Microinteracciones al tocar la fauna del fondo (como mucho 2 animales por mundo, fuera del panel, sin dar nada económico)? | Sí, midiendo que no aumenten los toques erróneos en las cuias | Abierta |
| F10 | ¿La fauna reacciona al sellar una cuia? | No: solo en la victoria y los hitos (prudencia con D-002) | Abierta |
| F11 | ¿Quién salta en las celebraciones? | El tucuxi; Rosa asoma y exhala, como el boto real. Cambia 07 §6.2, con el asesor | Abierta |
| F13 | ¿Algo nuevo a mitad de los mundos largos (6-11)? | Un segundo gesto de la misma especie en la 1.1 y, después, invitados opcionales | Abierta. La variación determinista por nivel ya alterna el lado de entrada, quién lidera y el reposo |

Las demás preguntas de la propuesta quedan cubiertas por el guion:
- F1 es Q7, y F2 es Q6.
- F3 y F4: tarjeta tras la intro, en el siguiente nivel libre, de 2,5 s y sin bloquear.
- F5: las especies amenazadas entran, con sus reglas. Pendiente de confirmación como Q7.
- F6: sin miquitos en el mundo 13.
- F8 es Q11, y F12 es Q9.
- F14: el churuco en el mundo 4.

---

## D-011 · Ajustes de la prueba en el teléfono: la victoria se cobra cuando se ve, avisos legibles, miquitos que se ven y Cosecha doble que avanza

- **Fecha:** 2026-10-11.
- **Estado:** decidida por el dueño tras probar el `dev.apk` en el SM-S938B (compilado en el portátil sobre `80d37fb` o antes; las cuatro causas siguen en `7677085`). El punto 3 lo eligió entre tres opciones cerradas («Más presencia sin frenar», la recomendada). **Sin implementar:** tareas P1-P4 de `docs/dev/traspaso/pruebas-telefono-2026-10-11.md`.

**Contexto** (palabras del dueño, con la ortografía corregida):
- «Las semillas se suman antes de dar por ganador el juego […] deberían verse luego de terminar completamente y ahí sumarlo […] pareciera que no lo suma, que ya tenía eso. Pasa lo mismo con las ayudas, que desaparecen: aunque se sabe que se va a ganar, no es bueno darlo ya por hecho; debe ser hasta que ocurra.»
- «Los letreros que muestra salen y duran muy poco, no se alcanzan a leer: en el nivel 8 salió algo que ni me dio tiempo de leer, como también en un nivel anterior salió algo de monito que no leí todo.»
- «Los miquitos pueden tirar rápido las semillas al inicio del tablero, pero pueden demorar un poco más en entrar para verlos bien; también duran poco y no se aprecian bien.»
- «Cuando me pide que puedo ganar el doble por ver anuncio, cuando selecciono y veo el anuncio, debería ahí ya haber avanzado de nivel, pero pasa que veo el anuncio y luego vuelvo a Siguiente.»

**Decisión.**
1. **Nada se da por ganado antes de que se vea.**
   - La Cosecha se sigue acreditando en el instante lógico de la victoria, con el progreso y en la misma escritura del perfil, para no perderla si se cierra la app (02 §14).
   - Lo que cambia es la vista:
     - el saldo de arriba no la enseña hasta que la cuenta la fila de la Cosecha en la pantalla de victoria: ni durante el último vertido ni durante la celebración;
     - la bandeja de comodines se queda como estaba mientras termina el último vertido, y se apaga al empezar la celebración.
2. **Avisos legibles.**
   - Los avisos del tablero y la tarjeta «¡Miquito a la vista!» duran según lo que hay que leer: **1.000 ms + 55 ms por carácter visible, entre 3.000 y 7.000 ms** [P].
     - Ejemplos: el regalo de Capi (81 caracteres) dura ≈ 5,5 s; la tarjeta (título, nombre y dato) dura ≈ 5,5-6 s.
     - Sustituye a los 2,6 s de los avisos y a los 2,5 s de la tarjeta (D-010, punto 5; 07 §6.4; guion de la intro §4.5 y A15).
   - **Ningún aviso sale durante la carga, la tarjeta previa ni la intro:** espera a que se habilite el toque. Hoy el regalo de Capi del nivel 8 sale al cargar, encima de la intro y de los miquitos.
   - **Uno cada vez:** si coinciden dos (el regalo de Capi y la tarjeta del miquito, por ejemplo), el segundo espera a que se vaya el primero.
   - Siguen sin capturar toques.
   - El cartel «El río bajó» no cambia (2,0 s, 04 §2.7): alargarlo retrasaría cada reintento.
3. **Miquitos con más presencia, sin frenar el juego.**
   - **Entrada:** entran trepando por la rama en ≈ 500 ms mientras el río llena las cuias, también en la versión corta (desde el 21, primer intento), donde hoy ya están posados en t = 0.
   - **Suelta y T:** la fruta cae igual de rápido. T (1.400 / 800 ms), las fases a-d y el momento en que se habilita el toque no cambian.
   - **Reposo:** tras soltar se quedan ≈ 1.500 ms en la rama con un gesto natural de reposo mientras el jugador ya juega:
     - por ejemplo, comer la fruta, rascarse, mirar alrededor o balancear la cola;
     - el gesto rota de forma determinista por nivel, sobre la variación por nivel que ya existe (F13);
     - después salen trepando en ≈ 500 ms (hoy 300 ms).
   - **Reglas que se mantienen:** D-010 punto 6 (sin rasgos humanos: nada de saludar ni señalar) y D-002 (no miran, no señalan y no reaccionan a una cuia, a la selección ni a un vertido). La especie sigue cambiando qué se ve, nunca cuánto dura.
   - **Al saltar la intro** con un toque, la tropa se va como hoy. Al cargar otro nivel, desaparece.
   - **La tarjeta «¡Miquito a la vista!»** ocupa la misma franja que la rama: sale cuando la tropa se ha ido.
   - Sin tropa (modo dosel: reintentos, 3 filas sin franja, mundo 13) no cambia nada.
4. **La Cosecha doble avanza sola.**
   - Tras ver entero el vídeo de `rv_double_harvest`:
     - la fila cuenta de +N a +2N (400 ms) y las semillas llegan al saldo;
     - se ve ≈ 600 ms más;
     - el juego pasa solo al nivel siguiente por el mismo camino que «Siguiente»: AD_GATE, y después «El río sigue creciendo» o Sin canoas, como hoy.
   - En AD_GATE no sale intersticial: `AdPolicy` lo bloquea 60 s después de un recompensado (`AfterRewarded`).
   - Si el vídeo se cierra antes de tiempo (sin recompensa), se queda en la victoria como hoy.
   - No contradice `victory_autonext_s` = 0 (02 §2): no es un temporizador, sino la respuesta al toque del jugador.

**Consecuencias.**
- **Código** (todo en Unity; núcleo, solver, packs y Remote Config sin cambios): tareas P1-P4 de la hoja de pruebas, con sus pruebas EditMode y PlayMode.
- **Tiempos nuevos sin clave de Remote Config** [P]: duración de los avisos por lectura, entrada, reposo y salida de la tropa, y espera de la Cosecha doble. Van como constantes, igual que los de D-010.
- **GDD y guion** (tarea de documentación de la hoja):
  - 02 §2 y §6: tras la Cosecha doble, se avanza solo;
  - 07 §6.4: duración de la tarjeta, y la tropa que se queda en reposo;
  - el guion de la intro: §4.5, §7.1-§7.3 y A15;
  - la nota de la tabla de D-010 (F3 y F4: «de 2,5 s»).

**Alternativas descartadas.**
- **Acreditar la Cosecha al terminar la celebración:** se perdería si se cierra la app durante ella (02 §14).
- **Intro más larga para los miquitos:** frena cada nivel (opción que el dueño no eligió).
- **Miquitos que crecen con el progreso:** los primeros niveles se quedarían cortos, que es donde el dueño lo notó (tampoco la eligió).
- **Avisos que se cierran con un toque:** los avisos no capturan toques para no robar vertidos.

**Cómo revertir.** Volver a las constantes de antes (`GameScreen.ToastMs` = 2.600, `SightingRule.CardMs` = 2.500, `IntroChoreography.ExitMs` = 300 y la tropa posada en t = 0 en la versión corta), quitar la espera del saldo y de la bandeja y el avance tras la Cosecha doble. Siempre con una decisión nueva que sustituya a D-011.

**Archivos afectados** (previstos; la hoja de pruebas los detalla): `Assets/_Project/Scripts/GameFlow/{LevelFlow.cs, SightingRule.cs}`, `Assets/_Project/Scripts/UI/{GameScreen.cs, BoosterTray.cs, Toast.cs, SightingCard.cs, GameBootstrap.cs}`, `Assets/_Project/Scripts/Presentation/{IntroChoreography.cs, TroopLayer.cs, IntroAnimation.cs, MonkeyRig.cs}`, sus tests y la documentación de arriba.
