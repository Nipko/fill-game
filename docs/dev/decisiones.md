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
| D-007 | 2026-10-08 | Mecánicas del río: catálogo de 12, estrenos de la 1.0 y reglas finas | Decidida por el dueño (todas las recomendaciones de la propuesta de cultura ribereña, §(h), y las reglas finas Q-A, Q-B y Q-C). Implementada en el núcleo y el generador (pista A, integrada en `6faf7c3`) y en Unity (paso 1 del traspaso, hasta `ce0ced4`). Registrada y llevada al GDD el 2026-10-11 y el 2026-10-12 (paso 2), con la lista única de estrenos en 02 §9.1. Pendientes, sin bloquear la 1.0: C1-9 (02 R16 con Liana y capacidades mixtas), la confirmación de Lluvia y Piraña en la 1.0 (criterio de GDD.md §6 #6), «Zafra», Revelar en la 3.ª ranura, el formato [P] de `pack_schema` 2, el de los respaldos del kill-switch, la tabla de novedades de 201-1000, las propiedades P1-P3 con el Remolino de Q-B, el revelado rítmico y la revisión del asesor cultural (preguntas 24, 27-31 y 33-35 de `docs/GDD.md` §6.3) |
| D-008 | 2026-10-08 | Bucle de partida como Magic Sort | Decidida por el dueño. Documentación aplicada el 2026-10-08 en `117f4be` (capítulos de «Archivos afectados»), dentro de la rama de código: **pendiente de la aprobación explícita del dueño antes de fusionar** o de un PR de documentación aparte (cambia el contrato 7). En implementación: pista B (B1-1-B1-6 y B2-1-B2-6). Los textos nuevos y los puntos [P] están pendientes (P14-P16, no bloquean). Abierta: P17, cuándo sale «Canoas infinitas 1 h» en Sin canoas (04 §7.3 y 05 §4.1 se contradicen). El número D-007 quedó reservado para las mecánicas especiales, en pausa; tiene su entrada desde el 2026-10-11 |
| D-009 | 2026-10-08 | Dificultad escalonada: ciclo de 20 reordenado, etiqueta antes de jugar y rotación de plantillas | Decidida por el dueño (Q1 A, Q2 sí, Q3 A y Q4 A de la misma propuesta). Implementada en el generador y en `tools/econ_sim.py` (pista A) y en Unity (paso 1). Registrada y llevada al GDD el 2026-10-11 (paso 2). Pendientes: aprobar las cifras [sim] regeneradas con el ciclo nuevo (P-2) y decidir la tabla de etiquetas de 03 §11 frente a la campaña medida, la fuente de la temporada del ciclo, que no está en el pack, y la tabla de novedades de 201-1000 (preguntas 25, 26, 32 y 33 de `docs/GDD.md` §6.3); los nombres internos de las posiciones y de los hitos [P] esperan al asesor cultural |
| D-010 | 2026-10-08 | «Llenar y teñir»: el río llena las cuias y los miquitos las tiñen con fruta | Decidida por el dueño (idea y ampliación del 2026-10-08); guion `docs/research/intro-rio-miquitos/guion.md` aprobado para implementar. Implementada en la rama `intro-rio-miquitos` (`fbdf11e`, `e479cf0` y `8955ef3`), sin fusionar. Documentación aplicada el 2026-10-09 en la misma rama. Pendientes, sin bloquear: confirmar Q1-Q16 del guion (el código aplica sus recomendaciones salvo Q11 y Q14), Q17, toda la fauna con el asesor cultural, el arte del artista y las preguntas abiertas de la propuesta de fauna (F7, F9-F11 y F13). Los números D-007 y D-009 los usó la pista de mecánicas y niveles (rama `fase1-mecanicas-niveles`); tienen su entrada desde el 2026-10-11. **Ampliación del 2026-10-09** (elenco de unas 50 especies que tiñen, una nueva cada 20 niveles), registrada el 2026-10-11: pendiente del diseño del paso 3 del traspaso |
| D-011 | 2026-10-11 | Ajustes de la prueba en el teléfono: la victoria se cobra cuando se ve, avisos legibles, miquitos que se ven y Cosecha doble que avanza | Decidida por el dueño tras probar el `dev.apk` en el SM-S938B (tareas P1-P4 de `docs/dev/traspaso/pruebas-telefono-2026-10-11.md`). Puntos 1, 3 y 4 implementados en la tanda 1 (integrada en `7d755c4`); punto 2 (avisos legibles) y la tarjeta del miquito tras la tropa, en curso (tanda 2). Llevada al GDD el 2026-10-12 (paso 2). Pendiente: ratificar sus [P] (pregunta 38 de `docs/GDD.md` §6.3) |
| D-012 | 2026-10-09 y 2026-10-11 | Prueba del 2026-10-09: estreno de la Piraña, banner de mundo, sacudida, patrones, guardado por vertido y marco del panel, con las respuestas del 2026-10-11 | Decidida por el dueño. Registrada y llevada al GDD el 2026-10-12 (paso 2). Implementados en la tanda 1 (`7d755c4`): el mordisco fallido visible, la marca de pareja del Remolino y el guardado por vertido con escritor en segundo plano (falta medirlo en el teléfono). En curso (tanda 2): el tutorial de la Piraña con mordisco animado, el banner de mundo, la sacudida y los patrones e iconos. Pendiente: el vertido que gana frente a «ninguno > 8 ms» (pregunta 37 de `docs/GDD.md` §6.3) |

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

## D-007 · Mecánicas del río: catálogo de 12, estrenos de la 1.0 y reglas finas

- **Fecha:** 2026-10-08 (respuestas del dueño a las preguntas de la propuesta y reglas finas Q-A, Q-B y Q-C del mismo día). Registrada el 2026-10-11.
- **Estado:** decidida por el dueño, que aceptó todas las recomendaciones de `docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md` §(h). La parte de dificultad de esa propuesta (Q1-Q4, el ciclo y la rotación) se registra aparte como D-009.
  - Implementada en el núcleo, el solver, el generador y `tools/level-qa` en la pista A (rama `fase1-mecanicas-niveles`, integrada en `6faf7c3`; packs 11-200 en `56f220d`) y en Unity en el paso 1 del traspaso (olas 1-4 y verificación T18, de `ecb7892` a `ce0ced4`).
  - Documentación aplicada el 2026-10-11 y completada el 2026-10-12 (paso 2 del traspaso; «Archivos afectados»).
  - **Toda la fauna, la flora, los lugares, los nombres de las mecánicas nuevas y los textos de los tutoriales quedan pendientes de revisión del asesor cultural** (07 §8.2; lista de la propuesta §(i)).
  - Pendientes que no bloquean la 1.0 (preguntas 24, 27-31 y 33-35 de `docs/GDD.md` §6.3): el bloqueo C1-9 (Consecuencias), la confirmación de Lluvia y Piraña en la 1.0 (punto 3), el formato de los respaldos del kill-switch (02 §9.2, regla 3), la tabla de novedades de 201-1000 con el alcance de Q8 (D-009, punto 8), las propiedades P1-P3 y el Remolino con Q-B (Consecuencias), el destino del revelado rítmico (punto 1), «Zafra» (pregunta 28), Revelar en la 3.ª ranura (pregunta 29) y el formato [P] de `pack_schema` 2 (pregunta 30).
- **Sustituye:**
  - el debut del Brote en el 63 (contrato 1; 02 §9.1 y `mech_debut_level`): pasa al 163 y el Matapi ocupa el 63;
  - la Liana de número como regla de debut (02 §9.1 M3): la Liana debuta con fruta en el 83 y la de número pasa a variante en el 243;
  - la regla de fútiles de 02 §1.4, su copia de 04 §5.1 y el orden de R10 (02 §1.2), por los del anexo B de la propuesta con la respuesta Q-B;
  - en 03 §8, la frase «nunca en niveles con mecánicas con disparador», que Q14 A define.
- **No sustituye:**
  - las reglas de Hoja, Brote, Remolino, Barro, Lluvia y Piraña de 02 §9.1, salvo lo que añaden Q-A, Q-B, Q-C y Q18 B;
  - Aguas turbias (02 §11, T1-T7) ni su estreno en el hito 100 con 1 cuia turbia y la Luciérnaga regalada (04 §2.5);
  - D-002 (ninguna mecánica resalta destinos) ni D-005 (vertido por tramo);
  - el formato `af-level/1` ni el nombre de su esquema: sube a `pack_schema` 2 (contrato 21);
  - el criterio de GDD.md §6 #6 y de la decisión 12 de 09 §8 para Lluvia y Piraña (propuesta §(f): «Criterio de la 1.0 sin cambios»; punto 3).

**Contexto.**
- D-008 dejó reservados el número D-007 y la pregunta P13 para las mecánicas especiales, en pausa: el dueño pidió investigar antes la cultura ribereña y la dificultad escalonada. Esta entrada cierra P13, reservada por D-008.
- La investigación (`docs/research/cultura-ribera/investigacion.md`, web consultada el 2026-10-08) y la propuesta (misma carpeta) sintetizan dos propuestas puntuadas por jueces, con sus correcciones (anexo A). La propuesta termina con 18 preguntas cerradas (§(h)).
- Filtro de cada idea (b.0): copia una función probada de Magic Sort; tiene una regla exacta compatible con el vertido por tramo, con D-002, con el solver y con la generación por superposición; no lleva contadores a favor del jugador; se lee en un segundo; cada desbloqueo tiene animación y sonido; nada sagrado ni personas.
- Antes de programar el Remolino, la Lluvia y la Piraña, la pista A pidió tres reglas finas (Q-A, Q-B y Q-C).

**Decisión.**
1. **Catálogo de 12 mecánicas de campaña** (propuesta b.1 y b.2): las 7 del canon y 5 nuevas. La numeración M8-M12 es provisional. Aguas turbias es un modificador del canon y no cuenta entre las 12.

   | # | Mecánica | Función de Magic Sort que copia | Regla en una línea | Estreno | Versión |
   |---|---|---|---|---|---|
   | M1 | Hoja | Botella atada y frascos cubiertos | Vacío reservado a una fruta; cae con su primer vertido | 43 | 1.0 |
   | M10 | **Matapi** (nueva) | Frasco «?» (equivalencia [E]: la regla de Magic Sort no está observada en el dispositivo) | Nunca es origen: lo que entra no sale | 63 | 1.0 |
   | M3 | Liana con fruta | Tela con icono | Atada; se suelta al sellar una cuia de su fruta | 83 | 1.0 |
   | — | Aguas turbias (modificador) | Capa «?» | Dosis ocultas que se revelan al quedar arriba | 100 | 1.0 |
   | M4 | Remolino | Portales | Lo vertido entra por el fondo de la pareja | 103 | 1.0 |
   | M5 | Barro | Slime y caja regalo | Dosis inerte que acepta cualquier fruta al quedar arriba | 123 | 1.0 |
   | M6 | Lluvia | Caja con temporizador | Cada k vertidos cae agua del mundo | 143 | 1.0 |
   | M2 | Brote | Corcho por color | Cerrado no es origen; germina con su color | 163 | 1.0 |
   | M7 | Piraña | Temporizador enemigo | Cada k vertidos muerde la última cuia destino | 183 | 1.0 |
   | M8 | **Aguapé** (nueva) | Hielo por fila | Un grupo no es origen hasta que una de sus cuias sella | 223 | 1.1 |
   | M9 | **Palizada** (nueva; sustituye a la «Nasa») | Armario con candado y llave dentro del líquido | Troncos sobre 2 o 4 cuias; se abren cuando la dosis-llave queda arriba | 263 | 1.1 |
   | M11 | **Lancha** (nueva) | Cortina que cae con N vacíos | La cuia de carga atraca cuando hay v cuias vacías | 303 | 1.1 |
   | M12 | **Cocha** (nueva) | Temporizador como ventana que se cierra | En v vertidos deja de recibir; solo sirve de origen | 583 | 1.1 |

   - Regla exacta, presentación, desbloqueo, tutorial, base cultural y coste de cada una: propuesta b.2. Las del canon no cambian salvo lo que dicen los puntos 5 y 6.
   - **Variantes** (b.3), para que haya algo nuevo hasta el 1000: Liana de cuenta (243), Hoja atada (283), Brote de dos hojas (343), Flor de vitória-régia (403), Palizada en cadena (443), Llave en la lancha (520, una combinación sin regla nueva, que no es un estreno), Casiquiare (703), Lancha de vuelta (763) y Aguapé que crece (883).
   - **Fuera de la campaña** (b.4; Q10): el nido de tortuga, el defeso y la piracema (contadores a favor del jugador, que se gastan con vertidos fútiles), el tipiti, la Cuia de tacacá (cambia el sellado), el mijano, Dos aguas, la Fiesta de la fruta y las Mareas de equinoccio van a Travesías y eventos, cada una donde dice b.4; el Banzeiro se descarta (b.4: «No»). El revelado rítmico tampoco entra en la campaña, pero las fuentes no coinciden en adónde va: Q10 lo lleva a Travesías y b.4 solo conserva su animación de burbuja para Aguas turbias (pregunta 35 de GDD.md §6.3). Descartes: b.5 (Nasa, tronco hueco, tarrafa como hielo, curral, cacuri, friagem, gamitana, «Regla del río», «Repiquete» como subtítulo visible y la llave entre aguas turbias).
2. **Estrenos de la 1.0** (niveles 1-200; Q5 B):
   - Cuia chica 14 y Cuia alta 27, sin cambios (02 §8);
   - Hoja 43, Matapi 63 y Liana con fruta 83;
   - Aguas turbias 100, con estreno suave (Q9 sí): es lo único especial del nivel, con 1 cuia turbia de 2 dosis ocultas y 1 Luciérnaga regalada. Es la única excepción a «nada debuta en un reto ni en un hito» (D-009) y el único Muy difícil sin un segundo componente: para las «2 mecánicas activas» de 02 §10, una capacidad protagonista cuenta como componente [P] (d.4: 60 Ho+Ch y, con el bloqueo C1-9, 160 Ll+Al6);
   - Remolino 103, Barro 123, Lluvia 143, Brote 163 y Piraña 183.
   - Cada estreno de bloqueador (43-183) ocupa p3-p5 de su ciclo con 2 vacíos, la mecánica sola y un solo ejemplar (02 §9.2, regla 2). Las capacidades no siguen esa regla: la Cuia chica se estrena en la rampa a mano (14 y 18) y en p2 (22), la Cuia alta de 5 en p7-p8 (27 y 28) y la de 6 en el hito 160 (02 §8; d.4); Aguas turbias, en el hito 100 (p20).
3. **Alcance por versión** (Q8: (c) junto con (b′)):
   - la 1.0 lleva las mecánicas hasta el 200 (el canon más el Matapi) y cubre 201-700 con las variantes de coste S (Liana de cuenta, Hoja atada, Brote de dos hojas y Flor) y con combinaciones de lo que ya trae;
   - **1.1:** Aguapé 223, Palizada 263, Lancha 303 y Cocha 583. Llegan en un build, porque exigen código en `AF.Core` y subir `pack_schema`, y sustituyen niveles de 201-700 con `level_overrides` o con una vuelta nueva (08 §4.4). Regla de migración: si el jugador ya pasó el debut de una mecánica, se le sirve una vez su tablero de debut antes de usarla (03 §9, consecuencia 3);
   - las demás variantes dependen de mecánicas de la 1.1 (Palizada en cadena, Llave en la lancha, Lancha de vuelta y Aguapé que crece) o caen después del 700 (Casiquiare);
   - Lluvia y Piraña: sigue vigente el criterio de GDD.md §6 #6 y de la decisión 12 de 09 §8 (propuesta §(f): «Criterio de la 1.0 sin cambios»). La evidencia C3-3 de `Packages/com.amazonfill.core/README.md` lo cumple y la 1.0 las incluye por Q8 (b′), a falta de que el dueño lo confirme al cierre de la Fase 1, como reserva ese criterio (el README del núcleo: «La decisión es del dueño»; pregunta 27 de GDD.md §6.3).
4. **Respuestas del dueño** (propuesta §(h), todas con la recomendación; Q1-Q4 son de D-009):
   - **Q5 B:** Matapi en el 63 y Brote en el 163. La equivalencia del Matapi con el frasco «?» de Magic Sort sigue siendo [E]: el Matapi vale como mecánica propia.
   - **Q6 sí:** la Liana debuta con fruta (`key`) y la de número (`n`) pasa a variante (243).
   - **Q7 sí:** Aguapé, Palizada, Lancha y Cocha entran en la campaña, con kill-switch y respaldo.
   - **Q8:** (c) junto con (b′) (punto 3).
   - **Q9 sí:** el estreno de Aguas turbias en el 100 es lo único especial del nivel, con 2 dosis ocultas (punto 2).
   - **Q10 sí:** los contadores a favor del jugador van a Travesías y eventos, nunca a la campaña.
   - **Q11 A:** las temporadas de aguas altas y bajas solo se ven en la LUT del fondo, sin cartel (D-009, punto 6).
   - **Q12 no:** ni manatí en el elenco ni Rosa como llave de la Palizada; la corriente se lleva el aguapé y la llave es un pez genérico. Se revisa con el arte.
   - **Q13 sí:** se arreglan la regla de fútiles y el orden de R10 antes de programar mecánicas (anexo B; hecho en el núcleo, `AF.Core.Rules.BoardRules`).
   - **Q14 A:** una «mecánica con disparador» es una de liberación (Liana, Flor, Aguapé, Palizada, Lancha) o un contador (Lluvia, Piraña, Cocha, Aguapé que crece). Los filtros (Hoja, Brote, Matapi, Barro) sí pueden ir con Aguas turbias.
   - **Q15 sí:** se encarga la skin Cuia de Santarém a ASARISAN, con pago y crédito, si el asesor y el presupuesto lo permiten.
   - **Q16 B:** `tools/level-qa` mide las trampas tempranas con un tope por etiqueta: 0 en Normal y 1 en Difícil (D-009, punto 9).
   - **Q17 A:** el contenido de la Liana y de la Palizada se ve; ninguna mecánica tapa contenido.
   - **Q18 B:** los vertidos fútiles no avanzan los contadores de Lluvia y Piraña; el contador visible solo baja con vertidos útiles, y R16 se mantiene.
5. **Reglas finas** (respuestas del dueño a Q-A, Q-B y Q-C, 2026-10-08):
   - **Q-A:** una cuia del Remolino no vierte en su propia pareja: el vertido es ilegal (rechazo sin texto, R11).
   - **Q-B:** la exención de `futil` vale también para el origen si es un Remolino o una Lluvia con gotas pendientes, no solo como destino: el anexo B solo eximía como origen la Cocha cerrada, y Q-B añade el Remolino y la Lluvia con gotas pendientes. Así R14 se cumple con esas dos mecánicas.
   - **Q-C:** si la Piraña no puede morder (la cuia que recibió quedó sellada, es un Matapi o la propia cueva, o la cueva está llena), pierde ese mordisco y el contador sigue.
6. **Reglas del núcleo** (anexo B, Q13, Q-B y Q18 B; `AF.Core.Rules.BoardRules`):
   - **R10 por etapas:** (a) revelado de la nueva cima de A; (b) aperturas por recepción (Hoja, Brote); (c) sellado; (d) liberaciones por sellado (Liana); (e) contadores (Lluvia, Piraña), una sola vez por vertido y nunca en un fútil; (f) condiciones del tablero (Palizada y Lancha, en la 1.1); (f′) se repiten (c), (d) y (f) hasta que nada cambie; (g) victoria; (h) bloqueo. Marea restaura el estado anterior entero.
   - **`futil`:** no es fútil verter en una cuia que cambia al recibir (Hoja puesta, Brote cerrado, Matapi, Remolino, Lluvia con gotas pendientes) ni desde un origen exento (Remolino o Lluvia con gotas pendientes, Q-B; en la 1.1, la Cocha cerrada). En lo demás rige 02 §1.4, con la excepción de Aguas turbias.
   - **Restricciones:** como mucho un contador por nivel (Lluvia o Piraña, nunca juntas); las combinaciones prohibidas de la propuesta d.3 (Aguas turbias solo con filtros y capacidades; el Matapi nunca en un par de Remolino, como cuia de Lluvia ni como cueva de Piraña, ni con Barro u ocultas dentro; Lancha y Liana juntas no antes del 401; la dosis-llave de una Palizada nunca en un Remolino ni bajo una dosis de su color); la Pororoca deja fijas las cuias con mecánica cerrada y se deshabilita con Lluvia y Piraña. La implementación añade [P]: sin Remolino junto a la Piraña ni con dosis ocultas.

**Consecuencias.**
- **Contratos:** cambian el contrato 1 (debuts: Hoja 43 · Matapi 63 · Liana con fruta 83 · Remolino 103 · Barro 123 · Lluvia 143 · Brote 163 · Piraña 183; `mech_debut_level` y `mech_enabled` ganan `matapi`) y el contrato 21 (`pack_schema` 2: tipo `matapi`; Liana con `key` o con `n`, una y solo una; campos obligatorios por tipo; `intro_mechanic` con `chica`, `alta`, `matapi` y `murky` [P], y `difficulty.par_approx` [P]).
- **Piraña:** con el mordisco inmediato de Q-C la heurística `H` sigue siendo admisible y consistente y el par es exacto: no hacen falta `ceil(H/2)` ni `par_approx` (CHANGELOG del núcleo, C3-2).
- **Rendimiento** (README del núcleo, C3-3): en los 36 tableros de referencia de 12-14 cuias, A* llega a 186.346 expandidos, Morpho tarda 1,0 ms como máximo y el bot ×40, 4,8 ms. Con el presupuesto de 300.000 se agotan algunos tableros con Remolino, Lluvia y Piraña, que el pipeline descarta o regenera.
- **Bloqueo C1-9** (pregunta de 02 R16, abierta): la propiedad P1 de R14 falla con Liana y capacidades mixtas (contraejemplo en el CHANGELOG del núcleo). Provisionalmente, `LevelValidation` rechaza la Liana con capacidades mixtas (también con la cueva de la Piraña), y el calendario de la campaña cambia cuatro celdas de d.4 [P] para que la Liana nunca vaya con ellas: 94 Ma → Li y 95 Li+Al → Ma+Al, 160 Ll+Li+Al6 → Ll+Al6 y 198 Pi+Li → Pi+Ho (`tools/level-generator/specs/campaign_v1.json`, `calendar_note`). Las capacidades se quedan: la Cuia alta sigue en el 95 y la de 6, en el 160. Opciones: (a) ampliar `futil` y quitar el bloqueo; (b) mantenerlo. Lo deciden el tech-lead o el dueño.
- **Propiedades P1-P3** (hallazgo de la pista A; CHANGELOG del núcleo, C1-2): el test literal del anexo B («no existe un ciclo de vertidos útiles que vuelva al mismo estado canónico») es falso ya en el juego base (A = [1, 2, 2, 2], B = [3, 3, 2]). La pista A lo sustituyó por tres propiedades que cada mecánica pasa antes de su debut, como garantía aplicada [P]: P1 (R14: todo estado atascado es irresoluble), P2 (R16 en `run`: un fútil entre dos cuias sin mecánica y de la misma capacidad no cambia el estado canónico) y P3 (ningún par monocolor ↔ vacía es útil en los dos sentidos). El dueño aceptó el anexo B con Q13, que incluye ese test entre sus pruebas, y no ha ratificado la sustitución (pregunta 34 de GDD.md §6.3).
- **Capacidades mixtas:** la poda de fútiles del solver puede alargar el par o dejar sin camino útil un tablero que se gana (C1-8; 3 de 309 tableros pequeños en `run`); R14 se cumple.
- **Remolino con Q-B frente a P3** (pregunta abierta del README del núcleo): con la exención de origen, X (Remolino monocolor) → vacía es útil y vacía → X entra por el fondo de la pareja, así que con la pareja vacía el tablero vuelve al mismo estado canónico (test `P3_Remolino_QB_AUsefulRoundTripCanReturnToTheSameCanonicalState`). P1 se cumple (R14), pero P3 no vale para el Remolino: en un estado cuyo único vertido útil sea ese vaivén, `atascado(S)` no se cumple y Aguas estancadas no sale hasta que el jugador cambie el tablero. Es el riesgo que el anexo B describe al eximir orígenes (pregunta 34 de GDD.md §6.3).
- **Kill-switch:** los respaldos por slot de 02 §9.2 (regla 3) no tienen todavía formato de pack ni de id. Hasta decidirlo, `mech_enabled` = false no puede sustituir los niveles de la 1.0 (pregunta 31 de GDD.md §6.3).
- **Paso 1 (Unity):** cada mecánica tiene aspecto procedural [P] (D-003) y desbloqueo al estilo Magic Sort con duraciones [P] (07 §5.6), y cada estreno, su tutorial de una pantalla (02 §8 y §9.1: los 11 textos caben en 2 líneas). El 100 regala la Luciérnaga.
- **Arte y audio:** unas 20 piezas nuevas entre sprites, animaciones y sonidos (propuesta §(g), 07). Todo el arte nuevo lo hace el artista.

**Alternativas descartadas.**
- Q5 A (opción mínima): el canon intacto y el Matapi en el 163, intercambiando Ma y Br en todas las columnas 61-200.
- Q8 (a), todas las mecánicas en la 1.0, y (b), las nuevas por `levels_remote` en la 1.1. La (b) no es posible: los niveles 1-700 van instalados, un paquete nuevo nunca cambia un nivel publicado y el cliente rechaza un `pack_schema` mayor (08 §4.4).
- Q17 C: tapar el contenido de la Liana y de la Palizada, como la tela de Magic Sort. Sería información oculta desde el 83 en niveles Normales.
- Q18 A: que los fútiles avancen los contadores. La Piraña quedaría sin bloqueo posible.
- La Nasa con pez, sustituida por la Palizada, y las demás ideas de b.5.
- Eximir en `futil` a cualquier origen con mecánica: el ir y venir monocolor contaría como útil y el modal de Aguas estancadas no saldría nunca (anexo B). Q-B lo limita al Remolino y a la Lluvia con gotas.

**Cómo revertir.**
- Debuts: cambiar el calendario de `campaign_v1.json` y regenerar los packs. Un pack publicado no cambia: hace falta una vuelta nueva o `level_overrides` (08 §4.4).
- Reglas del núcleo (R10, `futil`, Q-A, Q-B, Q-C y Q18 B): en `BoardRules` y en las mecánicas de `AF.Core`, con sus tests y las propiedades P1-P3.
- Siempre con una decisión nueva que sustituya a D-007.

**Archivos afectados.**
- **Núcleo, solver y herramientas** (pista A): `Packages/com.amazonfill.core/Runtime/AF.Core/{Rules, Mechanics, Board, Session, Determinism}`, `Runtime/AF.Solver/{Search, Bot, Generation, Murky}`, sus tests y `dotnet/AF.Core.Tests/DotnetOnly`; `tools/level-generator` (`--spec` y `specs/campaign_v1.json`), `tools/level-qa`, `schemas/af-level-1.schema.json` (`pack_schema` 2) y `tools/validate_schema.py`; los packs `Assets/_Project/Data/Levels/mundo_02_v1.json` a `mundo_05_v1.json` y el informe `tools/out/campaign_v1_report.md`.
- **Unity** (paso 1): `Assets/_Project/Scripts/{Levels, GameFlow, Boosters, Presentation, UI}` y sus tests (tareas del paso 1 en `docs/dev/traspaso/README.md` §6).
- **Documentación** (2026-10-11 y 2026-10-12, paso 2): este registro; `docs/GDD.md` §2, §3.3, §4, §5, §6 y §7; `docs/gdd/{00, 01, 02, 03, 04, 06, 07, 08, 09}`, con la lista única de estrenos de 02 §9.1 copiada en 02 (Parámetros), 08 §9, 00 §6.5 y el contrato 1; las descripciones de `mechanic_type` y `difficulty.par_approx` en `schemas/af-level-1.schema.json` (cambio editorial, sin subir `pack_schema`); `docs/dev/traspaso/README.md` (§6, paso 2: estado, y paso 4: C1-9; §7). Las cifras [sim] no cambian (D-009, Consecuencias).

---

## D-008 · Bucle de partida como Magic Sort

- **Fecha:** 2026-10-08.
- **Estado:** decidida por el dueño el 2026-10-08: «la estructura se copia de Magic Sort; las cifras salen de nuestro GDD». Su postura general: copiar lo que funciona sin suavizarlo, salvo por política de Google Play.
  - El número D-007 y la pregunta P13 quedan reservados para las mecánicas especiales, **en pausa**: el dueño pidió investigar antes la cultura ribereña y la dificultad escalonada. Nada de esta entrada depende de D-007. *Nota del 2026-10-11: D-007 y D-009 ya tienen su entrada, y D-007 cierra P13; D-009 colorea las cabeceras de ¿Seguro? y Nivel fallido con la etiqueta del nivel.*
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

## D-009 · Dificultad escalonada: ciclo de 20 reordenado, etiqueta antes de jugar y rotación de plantillas

- **Fecha:** 2026-10-08 (respuestas del dueño a Q1-Q4 de la propuesta). Registrada el 2026-10-11.
- **Estado:** decidida por el dueño con la recomendación de cada pregunta: Q1 A, Q2 sí, Q3 A y Q4 A de `docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md` §(h). El resto de la propuesta (mecánicas) es D-007.
  - Implementada en la pista A: `tools/level-generator` (`--spec`, con el ciclo y el calendario de `specs/campaign_v1.json`), `tools/level-qa` y `tools/econ_sim.py` (tarea P-2, `0faf51e`). En Unity, en el paso 1 del traspaso: chip y cinta en Jugar y en Siguiente, tarjeta previa, HUD y cabeceras de ¿Seguro? y Nivel fallido.
  - Documentación aplicada el 2026-10-11 (paso 2 del traspaso).
  - **Las cifras [sim] regeneradas con el ciclo nuevo esperan la aprobación del dueño** (P-2; `docs/dev/traspaso/planes-integracion.json`). Hasta entonces el GDD conserva las de la corrida del 2026-10-06 (GDD.md §7.3, V5).
  - Los nombres internos de las posiciones (Vaciante, Aguas bajas, Repiquete, Bajada, Remanso, Pico) y los nombres de hito son [P] y pasan por el asesor cultural.
- **Sustituye:**
  - en 03 §12, los retos en p6, p10, p14, p17 y p19 (contrato 1, incidencia 20 de GDD.md §7.2): pasan a p6, p9, p12, p15 y p18, y p19 pasa a ser un remanso puro;
  - en 03 §11, la regla «el hito siempre va precedido de un reto»: el hito va precedido de un remanso, y este de un reto;
  - los retos con etiqueta de 41-59 (el modelo de 05 §3.1 usaba p6, p14 y p19): pasan a p6, p12 y p18, y p9 y p15 quedan suaves;
  - los retos suaves de 21-39 (26, 30, 34, 37 y 39): pasan a 26, 29, 32, 35 y 38;
  - en 02 §9, «nunca en reto (p6, p10, p14, p17, p19)»;
  - en 02 §10, la señalización previa (solo la tarjeta): se suman el chip y la cinta en Jugar y en Siguiente.
- **No sustituye:**
  - la cuenta del ciclo: 12 de comodidad, 5 retos, 2 de recuperación y 1 hito;
  - el primer Difícil en el 40 y el primer Muy difícil en el 60 (contrato 6);
  - los umbrales del bot ×40 (Difícil ≥ 75 %, Muy difícil ≥ 90 %) ni que la etiqueta la decide la medición, no el slot;
  - los premios de 20, 60 y 80 semillas (`eco_reward_*`);
  - el reintento con la misma semilla, «El río bajó» al tercer fallo y el Salto del Boto (c.6);
  - la excepción de debut de 03 §12 (2 vacíos, una sola mecánica y bot < 20 % en p3-p5).

**Contexto.**
- El dueño pidió una dificultad «escalonada» como la de Magic Sort: dientes de sierra con etiquetas antes de jugar, un respiro tras cada pico y plantillas que rotan y vuelven (traspaso §8).
- Magic Sort pone la etiqueta y el «x5» en el botón con el que se arranca el nivel y anuncia el pico tras un respiro, como en sus niveles 387 → 388 → 389 (`docs/research/magic-sort/ficha-magic-sort.md` §3.6).
- En el ciclo anterior, el reto p19 iba seguido del hito p20: dos niveles con etiqueta seguidos (anexo A de la propuesta).

**Decisión.**
1. **Ciclo de 20 reordenado** (c.3; Q1 A). Desde el 21, `p = ((n − 1) mod 20) + 1`:

   | p | Rol | Nombre interno [P] | Vacíos | Fuente | Ventana de bot (21-40 / 41-60 / 61+) | Etiqueta posible | Contenido |
   |---|---|---|---|---|---|---|---|
   | 1-2 | Recuperación | Vaciante | 2 | `walk`, K−1 | < 30 % | — | Puro |
   | 3-5 | Comodidad o estreno | Aguas bajas | 1 (2 hasta el 30 y en debuts) | `walk` | 20-60 % (< 20 % en debuts) | — | Debut, o 0-1 mecánicas |
   | **6** | Reto | Repiquete | 1 (2 en 21-39) | `fill`/`deep` | 50-70 / 60-80 / ≥ 75 | Difícil desde el 41 | 1-2 mecánicas |
   | 7-8 | Comodidad | Bajada | 1 | `walk` | 20-60 % | — | 0-1 mecánicas |
   | **9** | Reto | Repiquete | ídem | ídem | ídem | Difícil | 1-2 mecánicas |
   | 10-11 | Comodidad | Bajada | 1 | `walk` | 20-60 % | — | 0-1 mecánicas |
   | **12** | Reto | Repiquete | ídem | ídem | ídem | Difícil | 1-2 mecánicas |
   | 13-14 | Comodidad | Bajada | 1 | `walk` | 20-60 % | — | 0-1 mecánicas |
   | **15** | Reto | Repiquete | ídem | ídem | ídem | Difícil | 1-2 mecánicas |
   | 16-17 | Comodidad | Bajada | 1 | `walk` | 20-60 % | — | 0-1 mecánicas |
   | **18** | Reto | Repiquete | ídem | ídem | ídem | Difícil | 1-2 mecánicas |
   | **19** | Remanso | Remanso | 1 (2 hasta el 30) | `walk` | 20-40 % | — | **Puro** |
   | **20** | Hito | Pico | 1 | `deep`, K máximo | 75-85 / ≥ 90 / ≥ 90 | Difícil (40) · Muy difícil (60+) | 2 mecánicas (3 desde el 320) |

   Reglas del escalón (c.3):
   1. Nunca hay dos niveles con etiqueta seguidos: entre dos retos hay siempre dos niveles y p19 no lleva etiqueta.
   2. Tras cada nivel con etiqueta, comodidad: el índice D baja al menos una banda, dentro del −3,4 de 03 §12.
   3. Tras el hito, dos niveles de recuperación.
   4. Los retos suben dentro del ciclo: desde el 61, D(p6) ≤ D(p9) ≤ D(p12) ≤ D(p15) ≤ D(p18) < D(p20); en 41-59 solo cuentan los retos con etiqueta, D(p6) ≤ D(p12) ≤ D(p18) < D(p20).
   5. El pico se anuncia tras un respiro.
   6. Nada debuta en un reto ni en un hito. Única excepción: Aguas turbias en el hito 100 (D-007). Una combinación sin regla nueva no es un debut.
   7. En 41-59, los retos con etiqueta son p6, p12 y p18 (3 de 5); p9 y p15 son suaves. El 40 es el primer Difícil y el 60, el primer Muy difícil.

   La especificación de la campaña concreta las ventanas de los retos [P]: con etiqueta, 75-89 %; suaves, 50-74 % (`tools/level-generator/README.md`).
2. **La etiqueta se ve antes de jugar** (c.1; Q2 sí):

   | Dónde | Normal | Difícil | Muy difícil |
   |---|---|---|---|
   | Botón «Jugar · Nivel n» del inicio (D-008, punto 6) | Sin chip | Chip ámbar con la rana dardo y la cinta «×3» | Chip índigo con el jaguar, la cinta «×4» y el botón en variante nocturna |
   | Botón «Siguiente» de la victoria (D-008, punto 3; 02 §6) | Sin chip | Chip ámbar y cinta «×3» del nivel siguiente | Chip índigo y cinta «×4» del nivel siguiente |
   | Tarjeta previa de 1 toque | Ninguna; en un debut, el tutorial hace de tarjeta | Rana, «Difícil», «Aguas bravas», «Premio: 60» e iconos de las mecánicas del nivel [P] | Jaguar, «Muy difícil», «Noche de jaguar», «Premio: 80» e iconos de las mecánicas [P] |
   | HUD | — | Chip y marco ámbar | Chip y marco índigo, fondo de noche |
   | ¿Seguro? y Nivel fallido (D-008) | Cabecera normal | Cabecera ámbar | Cabecera índigo |

   - Siguiente lleva la etiqueta porque, con D-008, tras ganar se pasa directo al nivel siguiente sin ver el botón del inicio.
   - Un reto que mide menos del 75 % se publica sin chip y sin «×3» (c.2). Sin calavera.
3. **Premios** (c.2; Q3 A): 20, 60 y 80 semillas, mostrados como cinta «×3» y «×4» sobre las 20 de un nivel normal. El «×5» (Muy difícil a 100) exige antes rehacer `tools/econ_sim.py` y `tools/out/econ_*.md` (contrato 18).
4. **Subtítulos** (c.1; Q4 A): «Aguas bravas» en Difícil y «Noche de jaguar» en Muy difícil, los del canon (glosario §6.1); se apagan con `ui_show_difficulty_subtitles`. «Repiquete» queda como nombre interno de los retos.
5. **La señal de dificultad es la luz, no la altura del agua** (c.5): el agua alta es la Crecida, que es buena. Muy difícil lleva la variante noche y el fondo un 15 % más oscuro (02 §10); el Difícil puede llevar una luz de tarde [P]. Sin barra ni texto; la luz sigue a la etiqueta medida.
6. **Rotación de plantillas** (c.4 y d.1-d.2):
   - **Plantillas por posición** (c.4): p1-p2 puras, con 2 vacíos y K−1. p3-p5: en un ciclo de debut, la mecánica nueva sola en 2-3 tableros; en los demás, comodidad con la titular del ciclo. p7-p8, p10-p11, p13-p14 y p16-p17: 0-1 mecánicas, con al menos 3 de esos 8 niveles puros. Retos: según el ciclo de vida (abajo). p19: puro y más fácil que la media de comodidad. p20: mecánicas con 15 niveles o más desde su debut y que ya salieron en un reto; al final de un mundo, su firma.
   - **Temporadas** (d.1; Q11 A de D-007): cada ciclo es de aguas altas (A: Lluvia, Aguapé, Lancha, Liana y Flor) o de aguas bajas (B: Matapi, Palizada, Piraña y Cocha); Hoja, Hoja atada, Brote, Remolino, Casiquiare y Barro son de todo el año. La temporada solo cambia el peso de cada mecánica en la bolsa (×2 las de su familia, ×1 las de todo el año, ×0,5 las de la otra) y la LUT del fondo, sin cartel. No sigue el calendario real.
   - **Ciclo de vida de una mecánica** (d.1): C0, el debut (titular en p3-p5, en p7, sola en el reto p9 y en dos o tres retos más; como mucho 8 niveles en el ciclo); C+1, secundaria de la siguiente titular en 2-3 retos; C+2, descanso o una comodidad; C+3 o C+4, regreso en una combinación nueva; después, la bolsa general.
   - **Reglas de la bolsa** (d.2): al menos un 30 % de niveles puros por ciclo (p1, p2, p19 y 3 de comodidad); una mecánica en 6 niveles por ciclo como mucho (8 en su ciclo de debut) y nunca en 3 seguidos fuera del estreno; al menos una «primera vez» por ciclo (debut, variante, firma, primera combinación o forma de tablero nueva), con un registro versionado de combinaciones publicadas para que el test de reproducibilidad siga valiendo; toda mecánica vuelve al menos cada 3 ciclos de su temporada (120 niveles o menos) y las de todo el año, cada 2; como mucho 2 mecánicas por nivel antes del 300 y 3 después (3 solo en retos e hitos); un contador por nivel; temporadas alternas, nunca más de 2 ciclos seguidos de la misma, y el ciclo de debut de una mecánica toma su temporada; kill-switch con `mech_enabled.<id>`.
7. **Calendario 1-200** (d.4): la tabla de la propuesta, nivel a nivel, con los estrenos de D-007, los debuts de tinte, los retos suaves, los retos que apuntan a Difícil, los hitos que apuntan a Muy difícil y los nombres de hito [P], pendientes del asesor. La campaña la copia en `tools/level-generator/specs/campaign_v1.json`, salvo las cuatro celdas [P] del bloqueo C1-9 (D-007, Consecuencias). Cada titular sale 8 veces en su ciclo de debut y vuelve como secundaria en el ciclo siguiente.
8. **Regla para 201-1000** (d.5): la misma plantilla y las mismas reglas; cerca del 25 % de Difícil y del 5 % de Muy difícil desde el 201 (03 §11, sin cambios); 2 titulares por ciclo (de la temporada o de todo el año) y 1-2 invitadas, y una titular descansa en el siguiente ciclo de su temporada; las temporadas por ciclo de la tabla de d.5 (201 A · 221 A · 241 B…); nunca más de 60 niveles sin algo nunca visto; después del 1000, las vueltas reciclan las plantillas con las LUT de lluvia, noche y alba. La tabla de novedades de d.5 suponía otro alcance de la 1.0: con el (c) de Q8 (D-007) hay que rehacerla.
9. **Medición** (c.7), además de los KPI de 03 §15:
   - intentos medios por mecánica en su ciclo de debut: alarma si superan 2,5 en un nivel Normal; se sustituyen los slots con `level_overrides` y nunca se aplaza el estreno siguiente;
   - trampas tempranas (jugadas iniciales que llevan a Aguas estancadas en 2 vertidos o menos): tope 0 en Normal y 1 en Difícil (Q16 B de D-007);
   - acierto de etiqueta (retos que alcanzan el 75 %): si baja del 60 % en un ciclo, se revisa el generador de esa posición.

**Consecuencias.**
- **Simulación** (P-2, `0faf51e`): `tools/econ_sim.py` pasa a `CHALLENGE_POSITIONS` = {6, 9, 12, 15, 18} y a los retos con etiqueta p6, p12 y p18 en 41-59. La cuenta de etiquetas no cambia, pero el orden de los retos mueve las cifras [sim]. Las celdas que cita el GDD (05 §3 y sus copias en 01, 04, 09 y GDD.md) que cambian:
  - **Escenario B** (canon, `tools/out/econ_B.md`): saldo final del casual 1.842 → 1.912, del medio 15.510 → 15.856, del hábil 26.408 → 26.092 y del pagador ligero 8.393 → 8.843; ingreso gratuito total del casual 30.709 → 30.735, del medio 48.857 → 48.816 y del hábil 46.486 → 46.431; ingreso por victoria 61,4 → 61,5 (casual), 97,7 → 97,6 (medio), 93,0 → 92,9 (hábil) y 97,2 → 97,3 (pagador); Cuias de rescate pagables por día del hábil 2,57 → 2,56; Cuias de rescate usadas 72,5 → 72,3, 72,1 → 71,3, 45,0 → 45,4 y 87,5 → 86,7; bloqueos sin recursos del casual 5,9 → 5,6 por corrida, en el 73 → 74 % de las corridas, con el primero en el 249 → 260; **el primer bloqueo sin recursos del medio pasa del 87 al 44** (corridas afectadas, 2 → 1 %) y **el del pagador ligero, del 311 al 282** (corridas afectadas, 10 → 14 %; 0,4 → 0,5 bloqueos por corrida); neto diario en 101-500 del casual −11 → −9, del medio +316 → +325, del hábil +851 → +841 y del pagador +124 → +138; USD del pagador 3,54 → 3,64. Los ratios de referencia no cambian (7,7 / 7,2:1).
  - **Escenario A** (`tools/out/econ_A.md`): saldo final del medio 23.815 → 24.043, del hábil 32.107 → 31.727 y del pagador 14.323 → 14.739; **primer bloqueo sin recursos del pagador 110 → 258** y del medio 57 → 49. Los ratios de referencia no cambian (7,0 / 6,6:1).
  - **B con tramos** (`tools/out/econ_B_tramos.md`): referencia de 101-500, 10,2 → 10,3:1; casual con 21,3 bloqueos por corrida en el 100 % de las corridas → 20,7 en el 99 %, primer bloqueo 198 → 203 y saldo final 895 → 893; medio con 3,1 (45 %) → 2,9 (44 %), neto diario −34 → −23, primer bloqueo 352 → 355 y saldo final 4.224 → 4.670; hábil con saldo final 18.120 → 17.710 y neto +508 → +494; pagador con 12,9 (90 %) → 13,2 (92 %), primer bloqueo 305 → 314 y saldo final 1.536 → 1.584.
  - **Variantes** (`tools/out/econ_variants.md`): en V0, saldo final del medio 15.686 → 16.067 y del casual 1.876 → 2.014, y casual con bloqueo sin recursos 70 → 76 %; en las demás, los ratios se mueven 0,1 como mucho y los saldos y porcentajes, en la misma medida.
  - El detalle celda a celda es el diff de `0faf51e` en `tools/out/`. Las cifras del GDD se actualizan cuando el dueño las apruebe (V5; pregunta 25 de GDD.md §6.3).
- **Campaña v1 medida** (`tools/out/campaign_v1_report.md`):
  - etiquetas: 31-60, 13,3 % Difícil y 3,3 % Muy difícil; 61-100, 20 % y 5 %; 101-200, 23 % y 5 %. Desde el 61 los cinco retos apuntan a Difícil (25 % por ciclo), por encima del 15 % y del 15-20 % de 03 §11: queda como pregunta abierta;
  - acierto de etiqueta por ciclo: 67-100 %; cada remanso queda por debajo de la media de comodidad de su ciclo (QA-REMANSO);
  - cuias y vacíos: en 31-100 hay 22 niveles con menos cuias que el mínimo de 03 §11 y en 101-200 el 64 % de las comodidades y los remansos se relajó, a 2 vacíos o con una Cuia chica de 2 (03 §4.4); el informe los deja para revisar.
- **Unity** (paso 1): chip y cinta en Jugar y en Siguiente; tarjeta previa de 1 toque (una sola en el 100, combinada con el tutorial de Aguas turbias y la línea T4); chip y marco en el HUD, con un velo que oscurece el fondo un 15 % en Muy difícil (la variante noche con LUT queda [P]); cabeceras de ¿Seguro? y Nivel fallido.
- **Pendiente técnico:** la temporada de cada ciclo no tiene todavía fuente de datos en el pack (la pista A la dejó solo en `campaign_v1.json`): la LUT de temporada necesita un campo nuevo, que subiría `pack_schema`, o una tabla del cliente.

**Alternativas descartadas.**
- Q1 B: el ciclo del canon (retos en p6, p10, p14, p17 y p19), con dos etiquetas seguidas en p19 y p20.
- Q3 B: el Muy difícil a 100 semillas («×5», como Magic Sort) sin rehacer la simulación.
- Q4 B: «Repiquete» como subtítulo visible, un regionalismo poco conocido.
- La «Regla del río» como cuarta señal de dificultad y subir el agua en los niveles difíciles: chocan con la barra de la Crecida, donde el agua alta es buena.
- Un cartel de temporada en p1 (Q11 B): competiría con «El río bajó».

**Cómo revertir.**
- Volver a las posiciones anteriores en `campaign_v1.json`, `tools/level-qa` y `tools/econ_sim.py`, regenerar `tools/out/econ_*.md` (contrato 18) y regenerar la campaña. Los niveles ya publicados no cambian: hace falta una vuelta nueva o `level_overrides` (08 §4.4).
- Siempre con una decisión nueva que sustituya a D-009.

**Archivos afectados.**
- **Herramientas** (pista A): `tools/level-generator` (`Campaign/`, `Generation/` y `specs/campaign_v1.json`), `tools/level-qa` (comprobaciones del ciclo, de banda, del remanso y de trampas), `tools/econ_sim.py` y `tools/out/econ_*.md` (P-2).
- **Unity** (paso 1): `Assets/_Project/Scripts/UI/{LobbyScreen, DifficultyChip, GameScreen.Labels, GameScreen.Funnel, GiveUpConfirm, LevelFailedScreen, LevelCardView}.cs` y `Assets/_Project/Scripts/GameFlow/{LevelCardInfo, LevelFlow.Cards}.cs`, con sus tests.
- **Documentación** (2026-10-11, paso 2): este registro; `docs/GDD.md` §2, §4, §6 y §7; `docs/gdd/{00, 02, 03, 04, 05, 06, 07, 08, 09}`, con solo una nota fechada en 05 §2.1 y §3.1. Las cifras [sim] de 01, 04, 05, 09 y GDD.md quedan para cuando el dueño las apruebe (V5).

---

## D-010 · «Llenar y teñir»: el río llena las cuias y los miquitos las tiñen con fruta

- **Fecha:** 2026-10-08 (idea del dueño y su ampliación, y guion de la intro del mismo día). Registrada el 2026-10-09. La ampliación del 2026-10-09 (elenco de unas 50 especies que tiñen) se registró el 2026-10-11, al final de esta entrada.
- **Estado:** decidida por el dueño, con el guion `docs/research/intro-rio-miquitos/guion.md` aprobado para implementar. Implementada en la rama `intro-rio-miquitos` (`fbdf11e`, con las revisiones `e479cf0` y `8955ef3`), **sin fusionar en `main`**.
  - Las 16 preguntas del guion (§18) no tienen respuesta explícita una a una. La implementación aplica la recomendación de cada una, salvo Q11 y Q14. Aquí quedan como **recomendación aplicada, pendiente de confirmación** (tabla del final): no bloquean y el registro no elige por el dueño (CLAUDE.md, regla 10).
  - **Toda la fauna, los nombres comunes, los datos de las tarjetas y la palabra «miquitos» (y su traducción) quedan pendientes de revisión del asesor cultural** (07 §8.2).
  - La propuesta de fauna (`docs/research/fauna/propuesta-fauna.md`) queda como **material de apoyo**, no canon, con sus preguntas abiertas (F1-F14; estado al final). Donde choca con el guion, manda el guion (punto 7).
  - Numeración: D-007 (mecánicas, debuts) y D-009 (dificultad escalonada) son de la pista de mecánicas y niveles (rama `fase1-mecanicas-niveles`, `tools/level-generator/README.md`), que todavía no ha traído sus entradas a este registro. El código de esta rama cita la intro como «pendiente de D-009» (`IntroAnimation.cs`, `MonkeyRig.cs`, `TroopLayer.cs` y `SightingCard.cs`): debe decir D-010. *Nota del 2026-10-11: D-007 y D-009 ya tienen su entrada.*
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
- F3 y F4: tarjeta tras la intro, en el siguiente nivel libre, de 2,5 s y sin bloquear. *Nota del 2026-10-12: D-011 cambia la duración (por lectura, 3-7 s [P]; punto 2) y el momento (cuando la tropa se ha ido; punto 3).*
- F5: las especies amenazadas entran, con sus reglas. Pendiente de confirmación como Q7.
- F6: sin miquitos en el mundo 13.
- F8 es Q11, y F12 es Q9.
- F14: el churuco en el mundo 4.

**Ampliación del 2026-10-09: elenco de unas 50 especies que tiñen.** Decisión del dueño, registrada el 2026-10-11 (traspaso §6, paso 3).
- **Ritmo:** una especie nueva cada 20 niveles (≈ 50 en 1-1.000), coordinada con los estrenos de mecánicas (D-007) para que no coincidan dos novedades en el mismo nivel.
- **Quién tiñe:** monos, aves y otros animales que de verdad dejan caer fruta (tucanes, guacamayos, oropéndolas, murciélagos fruteros, kinkajú…), no solo miquitos.
- **Cómo:** cada tipo con su forma de soltar la fruta (desde la rama, en vuelo, colgado, de noche), siempre en el mundo donde vive la especie.
- **Qué cambia de esta entrada cuando se diseñe el elenco:** el punto 4 (una especie de miquito por mundo, en 11 tramos) pasa a una especie nueva cada 20 niveles, y los tres modelos de suelta (`fila`, `cuelga` y `sacude`) tendrán que cubrir las formas de soltar de las aves y de los demás animales.
- **Qué no cambia:** T (1.400 / 800 ms) y las fases a-d de la intro (la entrada, el reposo y la salida de la tropa los fija D-011, punto 3), «nunca la especie de otro mundo», el modo dosel, las reglas duras de representación (punto 6), las especies como datos sin Remote Config y D-002.
- **Pendiente:**
  - el diseño del elenco (`docs/research/fauna/elenco-tinteros.md`), con el guion del equipo `docs/dev/traspaso/workflow-elenco-50-especies.js` (investigación, diseño, verificación biológica y técnica, implementación, revisión y documentación), que se lanza después de la integración en Unity;
  - hasta entonces sigue el catálogo `miquitos_v0.json` (10 especies y una variante);
  - la tarjeta «¡Miquito a la vista!» y la palabra «miquitos» no nombran aves ni murciélagos: su texto lo fijan el diseño del elenco y el asesor, no esta entrada;
  - cada especie, su nombre común y su forma de soltar la fruta, pendientes del asesor cultural (07 §8.2).

---

## D-011 · Ajustes de la prueba en el teléfono: la victoria se cobra cuando se ve, avisos legibles, miquitos que se ven y Cosecha doble que avanza

- **Fecha:** 2026-10-11.
- **Estado:** decidida por el dueño tras probar el `dev.apk` en el SM-S938B (compilado en el portátil sobre `80d37fb` o antes; las cuatro causas seguían en `7677085`). El punto 3 lo eligió entre tres opciones cerradas («Más presencia sin frenar», la recomendada). Tareas P1-P4 de `docs/dev/traspaso/pruebas-telefono-2026-10-11.md`.
  - *Actualización del 2026-10-12:* los puntos 1 y 4 están implementados en la tanda 1 (A1, integrada en `3035081`) y el punto 3, también (B1, `d8b9ca3`), salvo que la tarjeta «¡Miquito a la vista!» espere a que la tropa se vaya: B1 deja a la vista ese estado (`IntroAnimation.TroopVisible` y `TroopGoneMs`) y la tarjeta lo usa en la tanda 2. El punto 2 (avisos legibles) está en curso en la tanda 2 (A2). Toda la tanda 1 está en `7d755c4`, sin probar todavía en el teléfono.
  - Documentación aplicada el 2026-10-12 (paso 2 del traspaso): 02 §2, §4.2 y §6; 04 §2.2; 06 §4.7; 07 §5.1, §5.4 y §6.4; 08 §5.3; GDD.md §2 #22; la nota de F3 y F4 de la tabla de D-010 y el guion de la intro (nota de cabecera, §4.5, §7.2, §7.3, §14 y los criterios A3, A8 y A15). Los [P] de la tanda 1 que tiene que ratificar el dueño están en la pregunta 38 de GDD.md §6.3.

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

---

## D-012 · Prueba del 2026-10-09: estreno de la Piraña, banner de mundo, sacudida, patrones, guardado por vertido y marco del panel

- **Fecha:** 2026-10-09 (seis decisiones del dueño sobre la prueba de `80d37fb` en el teléfono, recogidas en la hoja de pruebas de ese día) y 2026-10-11 (sus respuestas a tres preguntas de la auditoría del paso 1b: (a) banner de mundo, (b) Remolino y (c) guardado). Registrada el 2026-10-12. Lleva el número 12 porque D-011 se registró antes.
- **Estado:** decidida por el dueño.
  - Implementadas en la tanda 1 (integrada en `7d755c4`): la parte C de la decisión 1 (mordisco fallido visible; tarea B2), la respuesta (b) (marca de pareja del Remolino; B2) y la decisión 5 con la respuesta (c) (guardado por vertido con un escritor en segundo plano; C2, `060b518`). Falta medir en el teléfono el objetivo de la decisión 5.
  - En curso (tanda 2): la parte D de la decisión 1 (tutorial con mordisco animado; A4), la decisión 2 con la respuesta (a) (banner de mundo; A4), la decisión 3 (sacudida; B3) y la decisión 4 (patrones e iconos; B3).
  - La decisión 6 no cambia nada.
  - Documentación aplicada el 2026-10-12 (paso 2 del traspaso; «Archivos afectados»).
  - **El texto nuevo del tutorial de la Piraña y los nombres de mundos, países y ríos del banner quedan pendientes de revisión del asesor cultural** (07 §8.2).
- **Sustituye:**
  - la sacudida de rechazo de ±6 dp, 2 ciclos y 120 ms (contrato 24; GDD.md §2 #3 y §7.2 #30, 02 §3.1 y §4.1, 07 §4.3): pasa a ±8 dp, 3 ciclos y 240 ms (punto 3);
  - el banner de mundo «tras Siguiente… con Rosa guiando» (02 §6) y el banner del mundo 2 en el Mapa del río tras la victoria del 10 (06 §2.2 y §2.3): sale al cargar el primer nivel del mundo, antes de la intro, y lo presenta Capi (punto 2 y respuesta (a));
  - el tutorial del estreno de la Piraña como texto con el icono (02 §9.1 M7): enseña un mordisco animado (punto 1, D);
  - el amago corto de la Piraña que no puede morder (02 §4.1; 07 §4.3 y §5.6): el cardumen llega a la víctima, choca y vuelve (punto 1, C);
  - en 07 §4.3, «con varias parejas, unos puntos junto a la bajada las distinguen»: las dos cuias de una pareja del Remolino llevan la misma marca también con un solo par (respuesta (b)).
- **No sustituye:**
  - las reglas de la Piraña (02 §9.1 M7, con Q-C y Q18 B de D-007) ni los niveles de la campaña: no se regeneran;
  - el texto del banner (`world_banner`, glosario §10) ni la tabla de 08 §9: no hay claves de Remote Config nuevas;
  - la opacidad del patrón y el nivel de 07 §3 y 08 §9 (`ui_pattern_opacity` = 0,20; `ui_patterns_default_from_level` = 41): la decisión 4 es «como el GDD»;
  - D-002 (nada se resalta al seleccionar, tampoco la marca de pareja), D-008 (se guarda tras cada vertido) ni la escritura atómica de 08 §11 (`.tmp`, `Flush(true)` y `File.Replace` con `.bak`);
  - el marco del panel bajo el HUD con 3 filas (guía visual v2 §3.2).

**Contexto.**
- El portátil probó `80d37fb` (olas 1 y 2 del paso 1) en el SM-S938B. La hoja del 2026-10-09 (copiada del mensaje del dueño del 2026-10-11) recoge 9 defectos fuera del plan (T-A a T-G), criterios para las olas 3 y 4, correcciones para el paso 2 y estas seis decisiones.
- Medidas que las motivan:
  - la sacudida de 120 ms y ±6 dp casi no se veía en el teléfono;
  - guardar en cada vertido costaba 3,5 ms de mediana, 5,6 de p95 y 23,2 de máximo (nivel 143), con 5 vertidos por encima de 8 ms, el tope de CPU del hilo principal de 08 §5.4. La causa: `LocalProfileStore.Save` hacía `Flush(true)` y `File.Replace` dentro del toque.
- La auditoría del 2026-10-11 dejó tres preguntas que el dueño respondió ese mismo día:
  - quién presenta el banner, si lo lleva el nivel 1 y si el del mundo 2 sale en el Mapa del río o en el nivel 11;
  - cómo se lee la pareja del Remolino entre filas no contiguas (en el 103, filas 1 y 3): A, canal por detrás del panel; B, la misma marca en las dos; C, las dos cosas;
  - si la tarea del guardado puede tocar `AF.Save` (`.claude/rules/sdk.md`: todo cambio ahí va en un PR aprobado por el dueño).
- Ese día el dueño aprobó también la caché de `LocalDateKey` en `AF.Platform`, aplicada en `7677085` (pregunta 20 de GDD.md §6.3, decidida).

**Decisión.**
1. **Estreno de la Piraña (183): opción C + D.** No se regeneran niveles.
   - **C, mordisco fallido visible** (ola 3): cuando la Piraña no puede morder (Q-C), el cardumen sale de la cueva, llega a la tapa o al Matapi de la víctima, choca y vuelve, en ≈ 400 ms [P]; con la cueva llena, se asoma y vuelve a dormirse.
   - **D, tutorial con un mordisco animado** (ola 4): la tarjeta de estreno del 183 enseña, antes de jugar, cómo la piraña sale de la cueva y se lleva la dosis de la cima de la cuia que acaba de recibir. El texto añade que sellar a tiempo evita el mordisco [P, pendiente del asesor cultural], en 2 líneas como mucho a 360 dp y a 12 sp o más. Con Reducir movimiento, una versión quieta [P].
   - Capítulos: 02 §4.1 y §9.1 (M7), 07 §4.3 y §5.6.
2. **Banner de mundo: opción A**, en la ola 4. Con la respuesta (a) del 2026-10-11:
   - sale al cargar el primer nivel de cada mundo salvo el Estuario (en la campaña 1-200: 11, 41, 81 y 141); **el nivel 1 no lleva banner**;
   - antes de Llenar y teñir y **solo en el primer intento** de ese nivel: ni al reintentar ni al reanudar; da igual que se entre con Jugar o con Siguiente [P];
   - es una tarjeta simple que se cierra con un toque: «Mundo {n}: {nombre} · {país} · {río}» (`world_banner`, glosario §10), con los nombres del glosario §8;
   - **la presenta Capi con la pata** (`capi_present`) **mientras Rosa asoma** (`rosa_surface`) **y se sumerge** al cerrarla (`rosa_dive`); con Reducir movimiento, quietos [P];
   - si el nivel tiene también tarjeta previa, primero va el banner [P]; los avisos esperan a que termine (D-011, punto 2).
   - Capítulos: 02 §2 y §6, 06 §2.2, §2.3 y §4.5, 07 §2.2 (regla 1) y §5.1; el texto ya está en el glosario §10.
3. **Sacudida de rechazo: ±8 dp, 3 ciclos y 240 ms** (T-G), con el mismo amortiguado. Un toque válido durante la sacudida se atiende igual que antes. El latido del embudo del Matapi la acompaña (240 ms, dentro de los ≤ 300 ms [P] del criterio de la ola 3).
   - Capítulos: GDD.md §2 #3, §7.2 #30 y §9, 02 §3.1 y §4.1, 07 §4.3, 09 §1.1 y la guía visual v2.
4. **Patrones e iconos de fruta: como el GDD** (T-E; 07 §3, 02 §12 y 08 §9):
   - hasta el 40, el patrón del tinte va sutil, al 15 %; desde el 41, Patrones de fruta está activado por defecto: patrón al 20 % e icono de fruta en cada dosis visible, de 0,45 u de alto y del color de la trama del tinte [P];
   - en cualquier nivel, al levantar una cuia, sus dosis visibles muestran el icono, y al bajarla desaparece;
   - nunca lleva icono una dosis oculta, el Barro ni el agua base;
   - el icono es uno por tinte, según la columna «Icono de accesibilidad» de 07 §3, procedural [P] hasta el arte del artista;
   - mientras no exista la pantalla de Ajustes, el interruptor es una constante del cliente, activa desde el 41 [P].
   - Capítulos: GDD.md §2 #5, 02 §12, 07 §3 y 08 §9 (columna «Uso», sin claves nuevas).
5. **Guardado por vertido: se mantiene el objetivo de T-D.** En el hilo principal del teléfono, p95 < 5 ms y ningún guardado > 8 ms, medido en ≥ 200 vertidos de 3 niveles; un force-stop a mitad de nivel deja al reabrir el tablero del último vertido. Con la respuesta (c), la tarea puede tocar `AF.Save` (opción B de T-D):
   - el vertido serializa en el hilo principal (< 1 ms) y entrega los bytes a un único escritor en segundo plano con cola de 1, en la que gana el último;
   - el escritor hace la escritura atómica de 08 §11;
   - al terminar el nivel, en cada mutación de la cartera, al pasar a segundo plano y al cerrar la app, se espera al escritor y se escribe en síncrono;
   - si el escritor falla, el perfil sigue en memoria y se reintenta en el siguiente guardado;
   - `AF_PERF` mide en `save_ms` solo el hilo principal y añade `bg_ms`;
   - sin paquetes nuevos y sin tocar `AF.Platform`.
   - Capítulos: 08 §5.4 y §11.
6. **Marco del panel bajo el HUD con 3 filas: se deja como está** (8 dp; guía visual v2 §3.2). Ningún capítulo cambia.
7. **Remolino, respuesta (b) del 2026-10-11: opción C.** El canal une las bases de la pareja, también entre filas no contiguas, y las dos cuias llevan la misma marca de pareja, también cuando el nivel tiene un solo par; con dos pares, las marcas se distinguen entre sí. La marca está siempre a la vista, a ΔE00 ≥ 10 de los 14 tintes y del turquesa de Marea, y nada cambia al seleccionar (D-002).
   - Capítulos: 07 §4.3.

**Consecuencias.**
- **Código** (todo en Unity; núcleo, solver, packs, esquema y Remote Config sin cambios):
  - tanda 1, integrada en `7d755c4`: B2 (mordisco fallido de ≈ 400 ms, `PiranaOverlay.BiteLostStepMs`; ficha de pareja del Remolino, igual en las dos cuias, con `id + 1` puntos) y C2 (`AF.Save.ProfileWriter`, `ProfileSaveBatch` y los guardados síncronos de `GameBootstrap`, con un tope de espera de 1.000 ms [P]);
  - tanda 2, en curso: A4 (banner de mundo y mordisco animado en la tarjeta del 183) y B3 (sacudida, patrones e iconos).
- **Teléfono:** falta medir la decisión 5 (`AF_PERF` con `save_ms`, `bg_ms` y `sync`) y ver la sacudida, el banner y el estreno de la Piraña cuando llegue la tanda 2.
- **Preguntas abiertas** (`docs/GDD.md` §6.3): el vertido que gana el nivel escribe en síncrono y puede pasar de 8 ms (pregunta 37), y los [P] de la tanda 1, entre ellos el tope de 1.000 ms y el aire de la ficha del Remolino (pregunta 38).

**Alternativas descartadas.**
- Mantener la sacudida del contrato 24: casi no se veía en el teléfono.
- Remolino: A (solo el canal, por detrás del panel) y B (solo la marca en las dos cuias).
- Banner de mundo: presentado por Rosa, en el nivel 1 o, para el mundo 2, en el Mapa del río tras la victoria del 10 (respuesta (a)).
- Guardado: seguir escribiendo con `Flush(true)` y `File.Replace` dentro del toque.

**Cómo revertir.**
- Sacudida: las constantes de `CuiaView` (`ShakeDp`, `ShakeMs` y el número de ciclos).
- Banner: su regla de niveles y su tarjeta (tanda 2).
- Piraña: el paso `BiteLost` de `PiranaOverlay` y la demostración de la tarjeta del 183 (tanda 2).
- Patrones e iconos: las constantes de `Palette` y la propiedad del material del líquido (tanda 2).
- Guardado: volver a la escritura síncrona en `ProfileSaveBatch` (C2).
- Remolino: la ficha de pareja de `RemolinoOverlay`.
- Siempre con una decisión nueva que sustituya a D-012.

**Archivos afectados.**
- **Código** (tanda 1): `Assets/_Project/Scripts/Presentation/Mechanics/{PiranaOverlay.cs, RemolinoOverlay.cs}`, `Assets/_Project/Scripts/Presentation/ProceduralArt.Remolino.cs`, `Assets/_Project/Scripts/Save/{ProfileWriter.cs, LocalProfileStore.cs, IProfileStore.cs}`, `Assets/_Project/Scripts/GameFlow/ProfileSaveBatch.cs`, `Assets/_Project/Scripts/UI/{GameBootstrap.cs, DevProbe.cs}`, sus tests y `docs/dev/prototipo-v0.md`. Tanda 2: los de sus especificaciones (A4 y B3).
- **Documentación** (2026-10-12, paso 2): este registro; `docs/GDD.md` §2, §6.3, §7.2 y §9; `docs/gdd/{00, 02, 06, 07, 08, 09}`; la guía visual v2 (`docs/art/direccion-visual-v2.md`, revisión 6: la sacudida); `docs/dev/traspaso/README.md` (§6 y §7).
