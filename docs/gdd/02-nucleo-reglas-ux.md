# Capítulo 2 · Núcleo jugable, reglas y UX en nivel

Fecha: 2026-10-06 (actualizado el 2026-10-06 con dos decisiones del dueño tomadas tras jugar el prototipo v0 en su teléfono: **D-001, una dosis por vertido**, y **D-002, sin resaltado de destinos al seleccionar**; actualizado el 2026-10-07 con **D-004, cuias más anchas en los tableros de pocas cuias** (tope del ancho según el número de cuias, sección 5) y vertido a 38° con el labio a 0,6-1,6 u del borde del destino (sección 4.1), decidida tras ver en su teléfono el build «Selva brillante premium»; actualizado el 2026-10-08 con **D-005** (vertido por tramo, como Magic Sort; sustituye a D-001) y **D-006** (tablero, cuia y vertido como Magic Sort: secciones 3.2, 4, 4.1, 5 y 8); registro, impacto y preguntas pendientes en `docs/dev/decisiones.md`) · GDD de Amazon Fill (nombre comercial Tintalú) · Convención: **[V]** verificado en el informe citado, **[E]** estimación del informe, **[R]** reseñas, **[NV]** no verificado, **[P]** propuesta de este capítulo a calibrar en soft launch. "(dirección N)" remite a las decisiones de dirección vinculantes; los nombres siguen el glosario (`docs/gdd/00-glosario.md`).

## Resumen

Este capítulo fija el núcleo water-sort de Tintalú en forma programable: estado, vertido legal, cantidad por vertido según el modo (la racha con vertido parcial en `run`, el modo por defecto por decisión del dueño del 2026-10-08, D-005; una dosis en `single`, solo pruebas), sellado, victoria y detección de bloqueo con exclusión de movimientos fútiles (sección 1), la máquina de estados del nivel desde la carga hasta la pantalla siguiente (2), el control tap-tap con encolado y sus reglas de cancelación (3), las duraciones de cada pieza de *juice* (4), el layout de la pantalla de nivel para hasta 16 cuias en teléfonos 20:9 (5), la pantalla de victoria (6) y el modal de bloqueo Aguas estancadas (7). Define el catálogo exacto de capacidades variables y de las siete mecánicas reservadas por el glosario (Hoja, Brote, Liana, Remolino, Barro, Lluvia, Piraña) con nivel de debut, tutorial de una pantalla e interacción con solver y comodines (8-9), las reglas de los niveles Difícil y Muy difícil (10), las de Aguas turbias con la garantía de vertido seguro (11), accesibilidad (12), el tutorial de los niveles 1-12 con aritmética cuias = colores + vacíos recalculada (13) y los casos borde (14). Cierra con la lista de claves de Remote Config que este capítulo usa (la tabla única con defaults es la del cap. 08 §9), las desviaciones de la dirección y las preguntas al dueño del producto.

## 1. Especificación de reglas (numerada, lista para programar)

### 1.1 Estado

| # | Regla | Fuente |
|---|---|---|
| R1 | Un nivel es una lista ordenada de T cuias (3 ≤ T ≤ 14 generadas; hasta 16 en pantalla contando Cuia extra y Cuia de rescate). Cada cuia i tiene capacidad `cap_i` ∈ {1, 2, 3, 4, 5, 6}; 4 por defecto (dirección 2, 4). | research/01 §2 [V]; research/09 §Núcleo [V] |
| R2 | El contenido de una cuia es una pila de 0..`cap_i` dosis, de abajo arriba. Cada dosis tiene `color` ∈ {1..16} (`K_motor` = 16, dirección 3) y `visible` ∈ {true, false}. Los IDs 1-14 son los tintes de la paleta; el 15 es Barro (inerte, no jugable); el 16 queda reservado. | glosario §9; research/02 C3.2 |
| R3 | Para cada color c presente, `U_c` = número total de dosis de c en el nivel (contando las que llegarán por Lluvia). En campaña `U_c` ∈ {4, 5, 6}; 4 salvo "fruta abundante" asociada a una Cuia alta (sección 8). | research/01 Especificación 22 [E]; [P] |
| R4 | Una cuia es **monocolor** si todas sus dosis tienen el mismo color (ignorando `visible`). Es **vacía** si no tiene dosis. Está **llena** si tiene `cap_i` dosis. | research/01 §2 [V] |
| R5 | Una cuia está **sellada** (Cuia sellada) si está llena, es monocolor y todas sus dosis son visibles. Sellada ⇒ no es origen ni destino; se dibuja la tapa de calabaza. Una cuia de capacidad menor que `U_c` nunca puede sellar ese color: es un búfer que debe terminar vacío. | research/01 Esp. 10 [V]; glosario §4 |
| R6 | Racha superior visible de la cuia i: la secuencia contigua de dosis desde la cima hacia abajo con el mismo color **y** `visible = true`. Una dosis oculta corta la racha aunque su color real coincida. En el modo `run` (por defecto; D-005, sección 1.2) la racha fija la cantidad de un vertido. En el modo `single` (solo pruebas) la racha no fija la cantidad; se conserva como definición porque la usan Remolino (sección 9.1) y el bot casual (03 §6). | research/01 Esp. 7 [V]; D-005 (decisión del dueño, 2026-10-08) |
| R7 | La cima de toda cuia no vacía es siempre visible: en el instante en que una dosis oculta queda arriba se marca `visible = true` para el resto del nivel (incluidos reinicios del mismo intento y Marea). | research/01 Esp. 8 [V] |
| R8 | El estado persistente del nivel incluye: `cuias`, lista de movimientos realizados (para Marea y para reconstruir), stock de Marea, Cuia extra/Cuia de rescate añadidas, máscara de revelado, contadores de mecánicas (sección 9), `fails_level`, `intento`. Se guarda tras cada vertido. | research/01 Esp. 25 [V] |

### 1.2 Vertido legal, cantidad y parcial

**Modo de vertido (D-005, decisión del dueño del 2026-10-08, que sustituye a D-001).** Un vertido mueve la racha visible que cabe, con vertido parcial: es la regla canónica del género (research/01 §2 [V]) y la de Magic Sort (`docs/research/magic-sort/ficha-magic-sort.md` §2.5). El núcleo lo expresa con el parámetro `PourMode { Run, Single }` (`AF.Core`), cuyo valor por defecto es `Run`, y con la clave de Remote Config `rules_pour_mode` = `"run"` | `"single"` (default `"run"`; 08 §9):
- `run` (por defecto): un vertido mueve la racha visible que cabe, con vertido parcial.
- `single` (solo pruebas): un vertido mueve la dosis de la cima de A. No existe el vertido parcial. Era el modo por defecto de D-001 (2026-10-06), sustituida por D-005.

El modo se fija al cargar cada nivel y **no cambia a mitad de un nivel**, porque el historial de Marea y el par dependen de él. La legalidad (`legal`), los fútiles (`futil`, sección 1.4), el sellado (R5) y la victoria (R12) son los mismos en los dos modos.

```
legal(A, B):
  si A == B: false
  si vacia(A) o sellada(A) o bloqueada_origen(A): false      # Hoja/Brote/Liana (sección 9)
  si sellada(B) o bloqueada_destino(B): false
  si remolino(B): return espacio(pareja(B)) > 0 and not sellada(pareja(B))   # sección 9
  si barro_expuesto(B): return true                            # Barro acepta cualquier color
  si hoja(B): return color_top(A) == color_hoja(B) and espacio(B) > 0
  si llena(B): false
  return vacia(B) or color_top(B) == color_top(A)

cantidad(A, B):                                     # solo se evalúa si legal(A, B)
  si rules_pour_mode == single: return 1            # single: solo pruebas (D-001, sustituida por D-005)
  return min(len(racha_visible(A)), espacio_efectivo(B))   # run (por defecto, D-005): la racha visible que cabe
  espacio_efectivo(B) = cap_B - dosis(B)            # +1 si barro_expuesto(B) (el barro se sustituye)
                                                    # legal(A, B) garantiza hueco para al menos una dosis (en B o, con Remolino, en su pareja): la dosis de single siempre cabe
```

| # | Regla |
|---|---|
| R9 | Un vertido A→B legal mueve `cantidad(A,B)` dosis desde la cima de A a la cima de B (al fondo de la pareja si B es Remolino). En el modo `run` (por defecto; D-005, decisión del dueño del 2026-10-08) mueve la racha visible de A; si no cabe entera, se vierte lo que cabe y el resto queda en A (**vertido parcial**, que solo existe en `run`). En el modo `single` (solo pruebas) mueve **una** dosis, la de la cima de A, aunque debajo haya más del mismo color: para pasar una racha de 3 hacen falta 3 vertidos, y el vertido parcial no existe. En los dos modos un vertido cuenta como 1 movimiento. (research/01 §2 [V]; ficha de Magic Sort §2.5; D-005) |
| R10 | Tras cada vertido se evalúa en este orden: (a) revelado de la nueva cima de A (R7; en `single`, cuando sale la última dosis de la racha); (b) apertura de Hoja/Brote y liberación de Liana (sección 9); (c) sellado de A y de B (R5); (d) ticks de Lluvia y Piraña (sección 9); (e) victoria (R12); (f) bloqueo (R13-R15). El orden es parte del contrato con el solver y con Marea. |
| R11 | Un toque que intenta un vertido ilegal no consume movimiento, no genera texto y produce el feedback de rechazo (sección 4). |

### 1.3 Victoria

| # | Regla |
|---|---|
| R12 | **Victoria** ⇔ toda cuia está vacía o sellada. Equivale a: cada color c tiene sus `U_c` dosis en una sola cuia llena. Las cuias vacías sobrantes (incluidas Cuia chica, Cuia extra y Cuia de rescate) son válidas. Precisión de la dirección 2: "monocolor" significa monocolor **y llena**, como en todos los líderes (research/01 §2 [V]); dos cuias monocolor parciales del mismo color no son victoria. |

### 1.4 Bloqueo: sin vertidos legales y "solo movimientos fútiles"

```
futil(A, B):
  # mover una cuia monocolor a una vacía solo recoloca el problema, salvo que la mueva a su "casa"
  si tiene_ocultas(A): false                           # A con dosis ocultas nunca es fútil: el jugador no sabe si es monocolor (cap. 04 §5.1)
  si not (monocolor(A) and not sellada(A) and vacia(B)): false
  c = color_top(A)
  casa_correcta_B = (cap_B == U_c)
  casa_incorrecta_A = (cap_A != U_c)
  si casa_incorrecta_A and casa_correcta_B: false      # llevar la fruta a la cuia donde puede sellar sí progresa
  si mecanica_activa(B) in {lluvia, remolino}: false  # el destino cambia la lógica, nunca es fútil
  return true

movimientos_utiles(S) = { (A,B) : legal(A,B) and not futil(A,B) }
atascado(S) = (movimientos_utiles(S) == ∅)
```

| # | Regla | Fuente |
|---|---|---|
| R13 | **Aguas estancadas** se declara cuando `movimientos_utiles(S)` está vacío. Cubre los dos casos de research/01 §7: sin vertidos legales [V] y solo movimientos fútiles [E]. Un vertido fútil sigue siendo **legal** (el jugador puede hacerlo); solo no cuenta para evitar el bloqueo. | research/01 §7, Esp. 13 |
| R14 | El motor **nunca** anuncia "irresoluble" de forma espontánea mientras exista un movimiento útil (caso 3 de research/01 §7 [V]). Morpho responde con honestidad solo si se le pide. | research/01 §7 [V] |
| R15 | El modal se abre `stuck_modal_delay_ms` (400 ms [P]) después de que termine la animación del último vertido, con el input bloqueado desde el instante lógico del bloqueo. Marea desde el modal restaura el estado y cierra el modal (dirección 9). | research/09 §Qué copiar pero mejorar [V] |
| R16 | Garantía de la regla de fútiles: con `futil` así definida, en el modo `run` (por defecto, D-005) el estado canónico (cuias ordenadas) no cambia con un movimiento fútil, salvo en la rama "casa correcta", que sí se permite. En el modo `single` (pruebas) un vertido fútil puede además partir una cuia monocolor en dos (A = [c, c, c] → B vacía deja [c, c] y [c]): el estado canónico cambia, pero con capacidades iguales solo aumenta la dispersión y nunca acerca a la victoria. `legal` y `futil` no dependen de la cantidad, así que para un mismo estado `movimientos_utiles(S)` y `atascado(S)` son idénticos en los dos modos; con capacidades uniformes (todo el tutorial) la regla de fútiles no cambia el veredicto de R13 en ningún modo (prueba en `docs/dev/decisiones.md` §2.1.2). Por tanto R13 nunca declara bloqueo en una posición con progreso posible. Con capacidades mixtas (Cuia chica desde el 14, Cuia alta desde el 27), la garantía en `single` queda por verificar con tests de propiedad antes de esos debuts [P]. El kill-switch `futile_rule_enabled=false` vuelve al criterio clásico "sin vertidos legales". | research/02 §2.2 [V] (poda monocolor→vacío en todos los solvers); D-001 y D-005 |

## 2. Máquina de estados del nivel

```
LOADING ──nivel en memoria──▶ INTRO(sumergir_y_tenir) ──fin o toque──▶ PLAYING
PLAYING ──victoria(R12)──▶ CELEBRATION ──0,9 s o toque──▶ VICTORY_SCREEN
PLAYING ──atascado(R13)──▶ STUCK_MODAL
STUCK_MODAL ──Marea──▶ PLAYING
STUCK_MODAL ──Cuia de rescate / Balsa de rescate──▶ PLAYING (+1 cuia)
STUCK_MODAL ──Volver a la orilla (fallo duro)──▶ FAIL_RESOLVE ──▶ INTRO(corta) ──▶ PLAYING
PLAYING ──Volver a la orilla voluntario, ≥ 8 vertidos (fallo blando)──▶ FAIL_RESOLVE ──▶ INTRO(corta) ──▶ PLAYING
PLAYING ──Volver a la orilla voluntario, < 8 vertidos (no es fallo)──▶ INTRO(corta) ──▶ PLAYING
STUCK_MODAL ──Salto del Boto (fails_level ≥ 5)──▶ SKIP_RESOLVE (sin canoa, Crecida intacta, fails_level = 0) ──▶ LOADING(N+1)
VICTORY_SCREEN ──toque en Siguiente──▶ AD_GATE(nivel ≥ 22, cap. 05 §6) ──▶ LOADING(N+1)
cualquier estado ──app a segundo plano──▶ SUSPENDED (estado ya persistido) ──resume──▶ mismo estado con vista sincronizada
```

| Estado | Entra cuando | Qué hace | Sale a | Timers |
|---|---|---|---|---|
| LOADING | Se pide el nivel N | Lee JSON (precargado durante N−1), construye estado, calcula layout, reserva texturas, precarga rewarded si aplica | INTRO | Si >300 ms, muestra "Cargando…" con lazo de río; el intro nunca arranca sin el nivel en memoria (dirección 18) |
| INTRO | Nivel listo | Sumergir y teñir (sección 4.2). En reintento muestra "Nivel N · intento k"; en variante aliviada muestra el banner "El río bajó" durante `anim_river_lowered_banner_ms` (2,0 s, 08 §9) antes del teñido | PLAYING | 1,2-1,5 s (niveles 1-20), 0,8 s (≥21); saltable desde el primer fotograma |
| PLAYING | Fin de INTRO | Input activo; tras cada vertido ejecuta R10 | CELEBRATION, STUCK_MODAL, pausa | Pista por inactividad `hint_idle_delay_s` (sección 3.4) |
| CELEBRATION | R12 verdadera | Input bloqueado; sellos brillan en secuencia, confeti de guacamayo, toada corta | VICTORY_SCREEN | 900 ms [P] o toque |
| VICTORY_SCREEN | Fin de celebración | Cosecha, Crecida, Cosecha doble (≥11), barras de evento, Canasta, Dato del río (cada 10) | AD_GATE / LOADING | Sin auto-avance (`victory_autonext_s` = 0 [P]) |
| STUCK_MODAL | R13 | Modal Aguas estancadas (sección 7) | PLAYING / FAIL_RESOLVE / SKIP_RESOLVE | Sin cuenta atrás |
| FAIL_RESOLVE | Fallo duro (Volver a la orilla desde STUCK_MODAL) o fallo blando (Volver a la orilla voluntario con ≥ `restart_confirm_min_pours` vertidos) | `fails_level += 1` en ambos; **solo en fallo duro**: desde el nivel 31 resta 1 canoa (salvo Canoas infinitas) y rompe Crecida salvo Hoja escudo; si `fails_level == relief_after_fails` (3) carga la variante aliviada con el banner "El río bajó"; si no, misma semilla (dirección 7) | INTRO | — |
| SKIP_RESOLVE | Salto del Boto aceptado en STUCK_MODAL (`fails_level ≥ skip_min_fails` y ≥ `skip_min_gap_levels` niveles desde el último salto) | Cobra 300 Semillas, marca N como saltado, `fails_level = 0`, `last_skip_level = N`; **no** resta canoa ni toca la Crecida (ni la rompe ni la avanza): no es un fallo (canon cap. 04 §5.2) | LOADING(N+1) | — |
| AD_GATE | Toque explícito en Siguiente con nivel ≥ 22 y condiciones del cap. 05 §6 | Intersticial solo tras victoria, nunca al inicio ni en reintento (dirección 13); **solo tras toque explícito en Siguiente, nunca por temporizador** (`victory_autonext_s` = 0; cualquier valor > 0 jamás dispara AD_GATE sin toque) | LOADING | — |
| SUSPENDED | Pérdida de foco | Nada: el estado ya está en disco tras cada vertido | Estado previo | Al volver, la vista salta al modelo (sección 14) |

Definición de **fallo**: **ver cap. 04 §5.2** (tabla canónica única del GDD; este capítulo no la redefine). Resumen idéntico al canon: **fallo duro** = salir de STUCK_MODAL con Volver a la orilla (desde el 31 cuesta 1 canoa salvo Canoas infinitas, rompe la Crecida salvo Hoja escudo, `fails_level` +1); **fallo blando** = Volver a la orilla voluntario con ≥ `restart_confirm_min_pours` (8) vertidos (`fails_level` +1 para el alivio "El río bajó" a los 3 y el Salto del Boto a los 5, pero **nunca** gasta canoa ni rompe la Crecida); reinicio voluntario con < 8 vertidos, Marea desde el modal, Cuia de rescate, salir al Mapa del río o cerrar la app (con o sin modal abierto: el estado se guarda y se reabre tal cual, sección 14) **no** son fallo; Salto del Boto no es fallo y reinicia `fails_level` a 0. El reinicio sigue siendo gratis e ilimitado (dirección 2).

## 3. Controles: tap-tap con encolado

### 3.1 Reglas de selección

| # | Situación | Resultado |
|---|---|---|
| C1 | Toque en cuia X sin selección, X puede ser origen (no vacía, no sellada, no bloqueada) | X se selecciona y se eleva; **ninguna otra cuia cambia**: no se resaltan ni se sugieren destinos (D-002, decisión del dueño del 2026-10-06: elegir bien es parte del juego) |
| C2 | Toque en X sin selección, X vacía, sellada o bloqueada | Rechazo suave (sección 4): sacudida 120 ms y burbuja; sin selección |
| C3 | Toque en X ya seleccionada | Deselecciona (baja). Un doble toque equivale a seleccionar y deseleccionar: **no** existe vertido automático por doble toque [P] (riesgo de vertidos accidentales con solo 3 Marea gratis) |
| C4 | Toque en Y ≠ X con X seleccionada y `legal(X,Y)` | Vertido X→Y; la selección se limpia: tras el vertido X **no** sigue seleccionada, aunque le queden dosis del mismo color (en `run`, tras un vertido parcial; en `single`, de pruebas, es lo habitual). P4 de `docs/dev/decisiones.md`, cerrada por D-005: se deselecciona |
| C5 | Toque en Y con X seleccionada, vertido ilegal, Y puede ser origen | La selección pasa a Y (X baja, Y sube) con `tap_switch_selection=true` [P]; con false, X baja y Y se sacude (research/01 Esp. 9 [V]) |
| C6 | Toque en Y con X seleccionada, vertido ilegal, Y no puede ser origen (sellada, bloqueada) | X baja, Y se sacude; sonido neutro |
| C7 | Toque en el fondo o en zonas vacías del tablero | Deselecciona |
| C8 | Toque en HUD/comodín con selección | Deselecciona y ejecuta la acción del botón |
| C9 | Mantener ≥400 ms sobre una cuia sin mover el dedo | Muestra los nombres de fruta de sus dosis (accesibilidad, sección 12); al soltar no selecciona |
| C10 | Dedo baja en X y sube en Y ≠ X tras desplazarse ≥24 dp | Se interpreta como X→Y (compatibilidad con hábito de arrastre, research/01 §3 [V] Hexa Sort) [P]; soltar sobre el fondo cancela |
| C11 | Varios dedos | Los toques se procesan por orden de `timestamp`; nunca en paralelo |

La elevación empieza en el **touch-down** (0 ms de latencia percibida); la decisión tap/arrastre se toma en el touch-up.

**Sin resaltado de destinos (D-002, decisión del dueño del 2026-10-06).** Seleccionar un origen solo lo eleva (sección 4.1): el juego nunca marca qué cuias son destinos legales, ni al seleccionar ni durante la cola de toques, porque elegir bien es parte del juego. El rechazo (C2, C5, C6) informa solo **después** de que el jugador elige. Las únicas ayudas que señalan algo son explícitas y quedan fuera del prototipo v0: la pista por inactividad (sección 3.4) señala solo un origen, y Morpho (04 §2.3) muestra origen y destino porque el jugador la pide. El dedo guía del tutorial (sección 13) también muestra un destino; si se mantiene como ayuda puntual compatible con D-002 es la pregunta P5 de `docs/dev/decisiones.md`.

### 3.2 Encolado de toques

El modelo es la verdad; la vista lo persigue (research/01 Esp. 24 [V]; research/09 §Núcleo [V]/[R]: el encolado es el elogio UX más repetido de Magic Sort).

```
on_pour(A, B):
  aplicar_en_modelo(A, B)                      # instantáneo: R9-R10 completos, autosave
  encolar_animacion(A, B, cantidad)
animador:
  dos vertidos con cuias disjuntas se animan en paralelo
  un vertido que toca una cuia ocupada espera a que esa cuia termine
  si una cuia acumula > queue_max_lag_pours (2) animaciones pendientes: las anteriores saltan a su fotograma final (0 ms)
seleccion_visual = la del modelo; los toques siempre se evalúan contra el modelo
```

Consecuencias: el jugador puede tocar origen-destino-origen-destino sin esperar; la vista nunca retrasa más de dos vertidos; Aguas estancadas y victoria se calculan en el modelo y la UI espera a que la cola visual se vacíe antes de mostrar modal o celebración. La espera no tiene un máximo de diseño: depende de lo que quede en la cola (duraciones de la sección 4.1). Medido en el nivel 2 del tutorial, tocando cada 1,0 s la solución en `run`: la cola se vacía ≈ 2,0 s después del último toque, porque el último vertido (1→2, 3 dosis) espera a que termine el anterior (2→4, 3 dosis), que toca la misma cuia (con los tiempos de D-006 y sin saltos de la cola: `queue_max_lag_pours` = 2). La implementación corta la espera a los 3.000 ms (`GameConfig.MaxViewDrainWaitMs`, tope de seguridad [P del tech-lead]); si se agota, la vista salta a su estado final.

**Encadenado.** Solo actúa en el modo `single` (pruebas): en `run`, con las reglas de v0 (sin mecánicas ni ocultas), dos vertidos A→B seguidos nunca son legales, porque el origen cambia de cima o el destino queda lleno. Se conserva para no saltar fotogramas en `single` (`docs/dev/decisiones.md` §2.1.6). En `single`, `cantidad` vale siempre 1 y pasar una racha exige repetir el mismo vertido A→B. Un jugador rápido puede encolar el tercer A→B con dos pendientes y hacer saltar el más antiguo a su fotograma final. Para evitarlo [P]: si llega un vertido con el mismo (A, B) que el que está en curso y el origen aún no ha empezado el retorno, se alarga el chorro `anim_pour_dose_ms` en lugar de encolar retorno, elevación y traslado (no cambia claves ni defaults).

### 3.3 Objetivo táctil

| Elemento | Área táctil mínima | Nota |
|---|---|---|
| Cuia | Ancho completo de su ranura (≥48 dp) × (alto de la cuia + 32 dp por encima, para alcanzarla elevada) | 48 dp es la recomendación estándar de Android/Material [NV en la investigación; P como requisito] |
| Botón de comodín | 56 × 72 dp | Icono 40 dp + etiqueta funcional 10 sp |
| Botones de cabecera | 44 × 44 dp | Pausa, ajustes |
| CTA de modal | Ancho completo − 48 dp, 56 dp de alto | Un pulgar, una mano |

### 3.4 Pista por inactividad

Tras `hint_idle_delay_s` (12 s [P]) sin toques, en **todos los niveles desde el 3** (`hint_idle_from_level` = 3, sin tope superior; claves y defaults en el cap. 08 §9), un origen de la ruta del solver pulsa suavemente una vez (escala 1,04, 600 ms). Solo si el solver (presupuesto `hint_budget_nodes` = 40.000 nodos, <50 ms en móvil, research/02 §2.5 [E]) confirma solución desde el estado actual; nunca en posición perdida (research/01 Esp. 15 [V]). En Aguas turbias solo propone vertidos seguros (sección 11, T5); si no hay ninguno demostrable dentro de `hint_timeout_ms`, no pulsa nada. No es Morpho: no muestra el destino ni cobra. El jugador puede apagarla en Ajustes con el interruptor "Pista por inactividad" (sección 12), que equivale a `hint_idle_delay_s` = 0 en ese dispositivo.

## 4. Feedback y *juice*: duraciones

Ningún líder publica tiempos (research/01 §3 [E]; research/09 §Arte [E]). Desde D-006 (decisión del dueño del 2026-10-08), la selección y el vertido copian los tiempos medidos en Magic Sort (`docs/research/magic-sort/ficha-magic-sort.md` §2.4 y §2.6). Los valores siguientes son **[P]**. Las horquillas estimadas de research/01 §3 [E] (elevación 0,15-0,25 s, traslado 0,25-0,35 s, caudal 0,3-0,5 s por 1-2 dosis, retorno 0,25 s) y de research/06 §2 [E] (0,4-0,7 s por vertido y onda amortiguada 0,5-0,8 s) se conservan como referencia histórica. Varias quedan por debajo de lo medido: el caudal con 2 dosis, el retorno y el vertido completo. Con la elevación pasa al revés: la medida, 100 ms, queda por debajo de su horquilla. Todas las duraciones se multiplican por `anim_speed_factor_early` / `anim_speed_factor_late` (1,0 en niveles 1-20; 0,8 desde el 21 [P]) y por `anim_reduce_motion_factor` (0,5) si el jugador activa "Reducir movimiento" (claves `anim_*` del cap. 08 §9).

### 4.1 Tabla de animaciones de juego

| Evento | Visual | Duración [P] | Curva | Audio | Háptico |
|---|---|---|---|---|---|
| Selección | La cuia sube 16 dp (≈ 12 % de su alto con la cuia de 48 dp; 9 % con la de 64 dp), sombra se separa, brillo del vidrio +10 %; el resto del tablero no cambia: sin resaltado de destinos (D-002, decisión del dueño del 2026-10-06; sección 3.1) | 100 ms (Magic Sort, ≤ 100 ms, ficha §2.4; D-006) | ease-out | "ploc" de agua corto | — |
| Deselección | Baja a su sitio | 100 ms | ease-in | — | — |
| Traslado + inclinación | La cuia viaja en arco hasta dejar el centro de su boca sobre el eje del destino, con el punto más bajo de la boca a 0,2-0,6 u del borde del destino, y llega inclinada 38°; el cuerpo queda del lado del que viene (en la misma columna, hacia el centro del tablero); si ese lado saliera del lienzo entre 38° y el ángulo final, al otro lado [P]; se dibuja delante de todo (vecinas, marco y HUD), sin techo ni cambio de escala (D-006; storyboard en 07 §5.2); el líquido **no** rota (shader con variable de inclinación, research/01 §3 [V]) | 300 ms | ease-in-out | — | — |
| Chorro | Bézier cuadrática desde el canto interior del lado bajo de la boca (0,34 u del eje, `CuiaView.PourEdgeU`) hasta la superficie del destino (LineRenderer, research/06 §2 [V]); arranca al 70 % del traslado; con cada dosis la inclinación sube hasta 92° − 9° × dosis que quedan en el origen (65° con 3, frente a los ≈ 66° que midió Magic Sort; ≈ 92° con 0; ficha §2.6) | 280 ms por dosis + 80 ms de cola (1 dosis 360 ms, 3 dosis 920 ms) | lineal | lazo de agua con tono ±5 % aleatorio | tick ligero al inicio del chorro |
| Franjas | La franja superior del origen decrece mientras la del destino crece, sincronizadas con el chorro; cara superior en elipse (07 §4.2; D-006) y onda de superficie amortiguada | onda 600 ms | exp. decreciente | — | — |
| Retorno | La cuia vuelve directa a su ranura | 300 ms | ease-out | — | — |
| Vertido completo (1 dosis, sin cola de toques) | 100 + 870 (salida al reposo: 0,7 × 300 + 280 + 80 + 300) | ≈ 970 ms (≈ 0,9 s de la salida al reposo, como Magic Sort) | — | — | — |
| Rechazo | Sacudida horizontal ±6 dp, 2 ciclos; sin texto | 120 ms | — | burbuja apagada | ninguno |
| Cuia sellada | Tapa de calabaza cae 150 ms + rebote 100 ms; destello anular 200 ms; el agua se aclara un 10 % | 450 ms total | — | maracá (research/07 §6.3 [V]) | ligero |
| Revelado (Aguas turbias) | La dosis parda se aclara hasta su tinte con remolino de partículas | 300 ms | ease-out | gota | — |
| Cuia extra / Cuia de rescate | Capi entra desde la orilla izquierda con la cuia en alto, la deposita; re-layout animado | 600 ms + 300 ms re-layout | — | pasos + "ploc" | ligero |
| Hoja/Brote/Liana se abren | Hoja se desprende y flota; brote germina en 2 hojas; liana se desata | 350 ms | — | hoja seca / brote / cuerda | — |
| Celebración | Sellos brillan en secuencia (60 ms entre cuias), confeti de plumas rojo/azul de guacamayo 1,2 s, Capi asoma en la esquina | 900 ms antes del panel | — | toada corta de carimbó 1,5 s | medio |
| Aguas estancadas | El agua del fondo se oscurece, las cuias pierden brillo | 400 ms antes del modal | — | acorde grave corto | ninguno |

Reglas de audio: nunca el grito de la piha en bucles (>110 dB en la realidad, research/07 §6.3 [V]); piha solo en el banner de mundo. Háptico: `VibrationEffect` predefinidos (tick/click) en Android; desactivable en Ajustes (research/01 §3 [V]: los jugadores exigen que el apagado funcione).

### 4.2 Sumergir y teñir (carga del nivel)

Secuencia (research/06 §2 [E], recortada a la dirección 18):

| Fase | Niveles 1-20 | Desde 21 | Qué pasa |
|---|---|---|---|
| a | 0,30 s | 0,15 s | Las cuias vacías bajan desde arriba hasta la banda de río del fondo |
| b | 0,40 s | 0,25 s | Se sumergen: ondas concéntricas, salpicadura; entran llenas del agua base del mundo (negra #4A2C14 / blanca #B08A5A / clara #2E8B8B, research/07 §2 [E]) |
| c | 0,25 s | 0,15 s | Emergen a su ranura |
| d | 0,10 s por franja, solapado (máx. 0,45 s) | 0,25 s total | Un icono de fruta vuela a cada franja y la tiñe de abajo arriba (interpolación OKLab, research/06 §2 [E]) |
| Total | 1,2-1,5 s | 0,8 s | Un toque en cualquier punto desde el primer fotograma salta al estado final (0 ms); el input de juego se habilita en ese mismo fotograma |

Reglas: el nivel está parseado y las texturas residentes antes de la fase a (dirección 18); el nivel N+1 se precarga mientras se juega N; cada salto se registra con `af_anim_skipped` (`anim` = `tint`; taxonomía del cap. 08 §8.2, con `level` entre los parámetros comunes); si la tasa de salto supera el 40 % en un tramo (`anim_tint_skip_alarm_pct`), `anim_tint_duration_ms_early` / `anim_tint_duration_ms_late` se baja por Remote Config (cap. 08 §9) (research/06 §2 [E]: satisfacción del 70 % a ~1,6 s y 50 % a ~2,7 s [V]).

## 5. HUD y layout de la pantalla de nivel

Referencia: 360 × 780 dp (19,5:9); 20:9 = 360 × 800. Zonas seguras: superior 32 dp (hasta 48 con *cutout*), inferior 24-48 dp (barra de gestos). Proporción de la cuia ≈ 1:2,8 (D-006); 1 fila hasta 5 cuias, 2 filas de 6 a 10 y, desde 11, 2 o 3 filas según la regla del tablero (research/07 §4 [E]; glosario §4).

```
+------------------------------------------------+  0       safe-top (32-48)
| [||]   Nivel 57         1.240 o   4/5 ~  (<)   |  32-88   fila superior (8 + 48): pausa · nivel + chip debajo · Semillas · Canoas (desde 31) · (<)
|        [Dificil]                               |          (<) = Volver a la orilla: icono de 40 dp a la derecha, junto a Canoas (07 §7.4)
+------------------------------------------------+  88
| (evento de 1.2)  ##########..........  4/10    |  88-120  barra de evento: solo eventos de 1.2 (colapsada sin evento)
+------------------------------------------------+  120     tablero y = 120-620 (07 §7.4)
|                                              - |          Crecida: barra vertical del borde derecho (07 §7.4)
|     []    []    []    []    []    []    []   . |          fila 1 (hasta 7 cuias con 2 filas; ≤ 5 con 3, salvo 6/5/5 con 16)
|     ||    ||    ||    ||    ||    ||    ||   . |          x = 348-356, y = 120-560, 8 dp de ancho
|     ||    ||    ||    ||    ||    ||    ||   - |          muescas (-) en los hitos 3 · 6 · 10
|                                              . |          agua (#): sube 1 paso por victoria
|        []    []    []    []    []    []      - |          fila 2 (filas centradas)
|        ||    ||    ||    ||    ||    ||      # |
|        ||    ||    ||    ||    ||    ||      # |
|                                              @ |  560     Hoja escudo (@) al pie de la barra
|                                                |          (fila 3 desde 11 cuias, regla del tablero)
+------------------------------------------------+  620     fin del tablero
|                                                |  620-660 margen de 40 dp entre tablero y bandeja
|  +------------------------------------------+  |  660     bandeja cumbuca: y = 660-740, alto 80, ancho 328, centrada
|  | [Marea] [Cuia+] [Morpho] [Pororoca] [Luc]|  |          orden canon 07 §7.4
|  | Deshacer +1 cuia  Pista    Barajar  Rev. |  |          etiqueta funcional bajo el icono
|  |   x2     150 o    100 o    300 o   120 o |  |          saldo o precio
|  +------------------------------------------+  |  740
|  ~~~~~~ orilla con Capi (decorativa) ~~~~~~~~  |  740-780 orilla decorativa por debajo de la bandeja + safe-bottom
+------------------------------------------------+  780
```

| Zona | Contenido | Reglas |
|---|---|---|
| Cabecera (56 dp: fila superior de 48 dp a y = safe + 8, cap. 07 §7.4) | Izquierda: pausa (abre Ajustes, Volver a la orilla, Mapa del río). Centro: "Nivel {n}" 20 sp con el chip de dificultad debajo (sección 10; cap. 07 §7.7). Derecha: contador de Semillas con icono; Canoas "4/5" desde el nivel 31; Volver a la orilla como icono de 40 dp a la derecha de la fila superior, junto a Canoas (07 §7.4) | Volver a la orilla vive en el menú de pausa y como icono de 40 dp a la derecha de la fila superior, junto a Canoas (posición del cap. 07 §7.4, dueño del HUD; este capítulo no fija posiciones propias); no ocupa la bandeja de comodines. Pide confirmación solo si hay ≥ `restart_confirm_min_pours` (8) vertidos, el mismo umbral del fallo blando (sección 2) |
| Barra de evento (32 dp) | Solo eventos de 1.2: la barra del evento temporal activo (Travesía, Regata…; cap. 06 §5.2). La Crecida y la Hoja escudo **no** van aquí (fila siguiente) | Si no hay evento activo se colapsa y el tablero gana 32 dp; hasta que lleguen los eventos de 1.2 va siempre colapsada |
| Barra de la Crecida (vertical, 8 dp de ancho) | **Borde derecho**, junto al tablero, en la posición del cap. 07 §7.4 (dueño del HUD): x = 348-356 dp, y = 120-560. El nivel de agua sube un escalón por victoria hacia el hito siguiente, con muescas en los hitos 3 · 6 · 10 (cap. 05 §2.4); la Hoja escudo, si está activa, se dibuja como hoja gigante al pie de la barra. Visible desde el nivel 25 (`streak_unlock_level`) | A 360 dp cae en el margen derecho de 12 dp que deja la ranura ((360 − 24) / cuias_por_fila), así que no resta ancho al tablero; la pantalla de victoria la replica en grande (sección 6); este capítulo no fija posiciones propias |
| Tablero (y = 120-620, cap. 07 §7.4) | Filas (D-006, decisión del dueño del 2026-10-08): 1 fila si T ≤ 5; 2 filas si 6 ≤ T ≤ 10 (reparto ⌈T/2⌉ / ⌊T/2⌋); con 11 ≤ T ≤ 14, 2 filas (⌈T/2⌉ / ⌊T/2⌋, hasta 7 por fila) o 3 filas (≤ 5 por fila; de arriba abajo 4/4/3, 4/4/4, 5/4/4 y 5/5/4), la que dé la cuia más ancha (a igualdad, 2 filas [P]); con T = 15-16, 3 filas (5/5/5 y 6/5/5). El número de filas se fija al cargar el nivel con las T cuias generadas y **no cambia a mitad de nivel**: Cuia extra y Cuia de rescate van a la fila más corta (a igualdad, la de más abajo [P]) y solo se recalcula el ancho (re-layout de 300 ms, sección 4.1) | Ranura = (360 − 24) / cuias de la fila más larga; cuia_w = clamp(0,70 × ranura, 28, tope(T)) dp, con tope(T) = 64 dp si T ≤ 3, 56 dp si T = 4-5 y 48 dp si T ≥ 6 (T = cuias en pantalla, R1; D-004, decisión del dueño del 2026-10-07); cuia_h = 2,8 × cuia_w (5,6 u; D-006); paso de fila = cuia_h + 16 (elevación) + 24 (hueco). Sin hueco para el vertido: el origen se dibuja delante de todo (D-006). La fila 1 elevada queda ≥ 8 dp bajo el canto interior del marco del panel [P]. Si las filas no caben, el ancho baja hasta que quepan (mínimo 28 dp). Ejemplos a 360 × 780: T = 3 → 64 × 179,2 (u = 32); T = 4 → 56 × 156,8; T = 5 → 47,04 × 131,7; T = 6-8 → 48 × 134,4; T = 9-10 → 47,04 × 131,7; T = 11-15 → 3 filas, 46,9 × 131,3; T = 16 → 39,2 × 109,8. Magic Sort, con 13-14 botellas: 45 × 138 a la misma escala (ficha §2.1-§2.2; propuesta §3.1) |
| Cuia alta / Cuia chica | Alineadas por el pie de la fila; alturas según la tabla del cap. 07 §4.5 (estándar 5,60 u; Cuia alta de 5: 6,60 u; de 6: 7,60 u; Cuia chica 2,60-4,60 u), con u = cuia_w / 2; este capítulo no fija factores propios | Niveles con Cuia alta: la regla de filas mide con su alto [P], a confirmar antes del nivel 27; si no cabe, u baja hasta el mínimo de 14 dp (cap. 07 §4.5) |
| Bandeja cumbuca (barra de comodines; y = 660-740: 80 dp de alto, 328 dp de ancho, centrada; cap. 07 §7.4) | Bajo el tablero, tras un margen de 40 dp (y = 620-660), y por encima de la orilla decorativa (y = 740-780). Una sola fila con el **orden canónico del cap. 07 §7.4** (este capítulo lo copia tal cual, sin posiciones propias): [Deshacer · Marea] [+1 cuia · Cuia extra] [Pista · Morpho] [Barajar · Pororoca] (+ [Revelar · Luciérnaga] solo en niveles Aguas turbias); 4 ranuras de 64 dp, o 5 de 56 dp con Luciérnaga; ningún comodín flota fuera de la bandeja. Botón = icono + etiqueta funcional + saldo/precio (regla de HUD del glosario §0) | Desbloqueo escalonado (sección 13): Marea visible desde el nivel 1 sin contador (ilimitada hasta el 10, `undo_unlimited_until_level`; tutorial en el 3; contador "3/3" desde el 11), Cuia extra 8 (Capi regala 1 unidad; cap. 04 §2.2), Morpho 12 (3 unidades regaladas; cap. 04 §2.3), Pororoca 30, Luciérnaga ≥100 [P]; antes del desbloqueo la ranura no existe (la bandeja se centra conservando el orden relativo, 07 §7.4) |
| Capi | Decoración animada en la orilla inferior izquierda, esporádica (parpadea, se rasca, se va). Nunca fijo en el centro inferior con un objeto (glosario §3.1) | Aparece en escena solo para Cuia extra, Cuia de rescate, banner de mundo y victoria |

Dispositivos: a 320 dp de ancho se admiten 6 cuias por fila (16 cuias = 3 filas 6/5/5). En 16:9 (360 × 640 dp) la barra de evento siempre está colapsada; ganan las 2 filas con 11-14 cuias (39,2 dp con 6 por fila; 33,6 × 94,1 con 7) y con 15-16 las 3 filas bajan a 30,2 dp (D-006). Con 13-14 cuias generadas en 2 filas, las cuias añadidas (14 + 1, 13 + 2 y 14 + 2: Cuia extra y Cuia de rescate) llevan la fila más corta a 8 cuias: ranura de 42 dp y cuia de 29,4 dp, por debajo de los 48 dp de la sección 3.3. Pendiente del dueño: pregunta P12 de `docs/dev/decisiones.md`. Tabletas: el tablero se centra a 480 dp de ancho máximo y las cuias no superan tope(T) (64, 56 o 48 dp según T; D-004). Orientación: vertical bloqueada.

## 6. Pantalla de victoria

```
+------------------------------------------+
|           ¡Nivel completado!             |
|   (cuias selladas en miniatura, 2 filas) |
|                                          |
|   Cosecha        +20 o   (60/80 si D/MD) |
|   Crecida  ##########....  3/3 -> +300 o |
|   Canasta  7/10                          |
|                                          |
|   [ Cosecha doble: x2 por video ]  (>=11)|
|   [          Siguiente           ]       |
|                                          |
|   Dato del rio (cada 10 niveles)         |
+------------------------------------------+
```

| Elemento | Regla |
|---|---|
| Cosecha | 20 Semillas Normal, 60 Difícil, 80 Muy difícil (dirección 12); la cifra cuenta hacia arriba en 400 ms con sonido de semillas cayendo en la cesta |
| Crecida | La barra del borde derecho (07 §7.4) se replica grande: el agua sube un escalón por victoria; al tocar hito (fila única del cap. 05 §2.4: 3 → 300 Semillas · 6 → Canasta · 10 → 1 Cuia extra + río crecido; cada hito se paga una vez al día local; tope diario 600, que solo suma las 300 Semillas del hito 3 y las 50 de la Canasta del hito 6, sin el bonus del río crecido ni los comodines de los hitos 6 y 10; clave `streak_milestones`) la recompensa se anima aquí (glosario §6.3). Tras el hito 10 la barra vuelve a 0 y el río crecido sigue activo hasta el siguiente fallo duro no escudado. Sustituye al multiplicador ×2→×100 de Magic Sort (research/09 §Arte [V]) porque la dirección 12 fija hitos, no multiplicador; el doble emplazamiento (borde derecho del tablero + victoria) se conserva |
| Cosecha doble | Botón secundario desde el nivel 11 (dirección 13); muestra "+20 → +40"; sin anuncio precargado el botón no aparece |
| Barras de evento | Reto del río (si el nivel cuenta), Canasta "k/10", en v1.2 Regata/Travesía; máximo 3 barras visibles, el resto en un chevron |
| Dato del río | Una frase verificada cada 10 niveles (glosario §6.4), bajo el botón, con icono del animal/río; no bloquea Siguiente |
| Siguiente | CTA primario; sin auto-avance por defecto (`victory_autonext_s` = 0) porque la oferta x2 exige una decisión [P]; research/01 Esp. 12 sugería auto-avance 2-3 s [E], queda como parámetro |
| Banner de mundo | Si N+1 abre un mundo, tras Siguiente se muestra la tarjeta de país/río/agua base con Rosa guiando (glosario §3.2), 1 toque para continuar |

## 7. Modal de bloqueo: Aguas estancadas

Dirección 10 y glosario §5.1. Un solo modal, sin cuenta atrás, orden fijo: Marea (si queda cupo o recarga), Cuia de rescate, Volver a la orilla al pie. Antes del nivel 31 el modal solo tiene Marea y Volver a la orilla: sin rescate, sin canoas, sin IAP (dirección 10).

```
+------------------------------------------+
|            No hay mas vertidos           |
|        (Capi en canoa con una cuia)      |
|  "Capi llega al rescate con una cuia     |
|   mas! Sigue desde aqui."                |
|                                          |
|  [ Deshacer (Marea)        x2 restantes ]|   si stock = 0: "+3 por video o 30 o"
|                                          |
|  [ Continuar: +1 cuia        ~~600~~ 300 o ]  600 fijo desde el 31; primer rescate del dia 300 (-50 %)
|                                          |
|  [ Saltar nivel (Salto del Boto)  300 o ]|   solo si fails_level >= 5
|                                          |
|           Volver a la orilla             |   enlace; desde el 31 cuesta 1 canoa
+------------------------------------------+
```

| Elemento | Regla |
|---|---|
| Titular | "No hay más vertidos" (`no_moves`), nunca "has perdido" |
| Marea | Siempre primero (corrige la queja estructural de Magic Sort, research/09 §Comodines [V]). Con stock (ilimitado hasta el nivel 10, sección 13): deshace y cierra el modal. Sin stock: recarga +3 por anuncio recompensado o 30 Semillas (dirección 9); si no hay anuncio (`no_fill`), solo la opción de Semillas |
| Cuia de rescate | Precio **600 Semillas fijas desde el nivel 31** (clave única `rescue_price_bands` = `[[31,600]]`, cap. 05; no existe `rescue_base_price` ni tramos 750/900 ni cohortes por geografía: el ajuste LatAm va en el precio local de los IAP); primer rescate del día 300 (`rescue_first_day_mult` = 0,5, 600 tachado); máximo `rescue_max_per_level` = 1 (decisión cerrada) para que T ≤ 14 + Cuia extra + Cuia de rescate ≤ 16 ranuras. Al aceptar: Capi trae la cuia (600 ms), el modal se cierra, el estado sigue; la cuia **persiste** en reinicios y cierres del mismo nivel (research/03 C2.4 [E]; glosario §5.1) |
| Sin saldo | El botón muestra el precio en rojo y "Te faltan {n}"; al tocar abre una hoja inferior con Puñado 1,99 (1.000) y, si `rescue_hoy = 0`, Balsa de rescate 4,99; el detalle de la hoja y el árbol de decisión son del cap. 04 §6 y los precios del cap. 05 §4 (research/03 C2.2) |
| Salto del Boto | Aparece solo con `fails_level ≥ 5` y ≥10 niveles desde el último salto (dirección 9); Rosa salta y lleva al N+1; **no** es fallo: no gasta canoa ni rompe la Crecida (tampoco la avanza) y reinicia `fails_level` (canon cap. 04 §5.2) |
| Volver a la orilla | Enlace al pie; es el **fallo duro** (sección 2): mismo tablero (misma semilla) salvo que `fails_level` alcance 3 (variante aliviada con "El río bajó", dirección 7); desde el nivel 31 resta 1 canoa y rompe la Crecida salvo Hoja escudo. Qué bloquean 0 canoas (misma frase que el cap. 04 §7.1): con **0 canoas no se puede iniciar ni reiniciar un nivel de campaña**: Jugar (Mapa del río), Siguiente (pantalla de victoria) y Volver a la orilla (cabecera o modal) abren Sin canoas (cap. 04 §7.3: Canoa extra por anuncio, llenar por 200 Semillas o esperar); una partida ya cargada nunca se interrumpe por canoas, y la Cuia de rescate no requiere canoa |
| Cuia extra | **No** aparece aquí: es un comodín planificado "solo mientras quede un vertido legal" (dirección 9). El botón de la barra se desactiva al entrar en Aguas estancadas |
| Cierre por fuera | No existe: el modal es modal. Volver al Mapa del río desde pausa o cerrar la app **no** es fallo: el estado (incluido el modal abierto) se guarda y se reabre tal cual al volver al nivel, sin cobrar canoa (canon cap. 04 §5.2; sección 14) |

## 8. Capacidades variables

| Tipo | Capacidad | Debut [P] | Regla | Visual |
|---|---|---|---|---|
| Cuia estándar | 4 | 1 | R1-R16 | Silueta base, 4 marcas de dosis |
| **Cuia chica** | 2 (debut), luego 1 y 3 | 14 (cap 2), 18 (cap 1), 22 (cap 3) | Búfer: `cap < U_c` para todo color ⇒ nunca sella ⇒ debe terminar vacía. Cuenta como "vacío" para la aritmética del nivel, pero de valor reducido | Misma silueta recortada por arriba: 1-3 marcas; borde de fibra más grueso |
| **Cuia alta** | 5 (debut), 6 | 27 (cap 5); cap 6 solo en niveles hito desde el 141 (primero: 160) | Llega con una **fruta abundante**: un color con `U_c = cap` que solo puede sellar en ella. Las frutas de 4 dosis pueden entrar y salir, pero una Cuia alta llena de 4 dosis de otra fruta no está llena ⇒ no sella (R5): el jugador tiene que sacarla | Silueta alargada (6,60 u la de 5 y 7,60 u la de 6 frente a 5,60 u de la estándar; tabla del cap. 07 §4.5); la fruta abundante lleva una hoja en el icono |
| Cuia extra / Cuia de rescate | 4 | 8 / 31 | Cuia estándar añadida a la fila más corta (a igualdad, la de más abajo [P]); el número de filas no cambia a mitad de nivel (D-006); re-layout de 300 ms | Idéntica a la estándar con un lazo de fibra |

Restricciones del generador [P]: hasta 2 Cuia chica y 1 Cuia alta por nivel antes del 200; `Σ_i cap_i − Σ_c U_c` ≥ 2 en niveles 1-30 (salvo las excepciones tutoriales 1 y 6, con 1 vacío; sección 13) y ≥ 1 después (dirección 4), contando la Cuia chica a su capacidad real; la mecánica debuta en 2-3 tableros fáciles (research/02 §7.2 [V], patrón Royal Match). Tutorial de una pantalla al debut: Cuia chica "Esta cuia chica solo guarda dos dosis: úsala para apartar, no para terminar"; Cuia alta "Esta fruta tiene cinco dosis: solo cabe en la cuia alta".

## 9. Catálogo de mecánicas especiales

Direcciones 8 y 16; nombres reservados en el glosario §6.5. Cadencia: Cuia chica 14 y Cuia alta 27 (dentro del tramo 1-30 de 2 vacíos construido a mano, fuera del ciclo), primer bloqueador nuevo 43 (ventana 30-50), luego una mecánica cada 20 niveles hasta el 143 (≤150) y la séptima 40 niveles después (183; ventana 40-60). Todos los debuts de bloqueadores caen en la **posición 3 del ciclo diente de sierra de 20** (dirección 5: tramo de comodidad, bot <20 %), nunca en reto (p6, p10, p14, p17, p19), recuperación (p1-p2) ni hito (p20), según el ciclo de 03 §12: así la regla transversal (2) de 9.2 se cumple por construcción y el cap. 03 fija los 2-3 tableros de debut en las posiciones 3-5. Esta tabla y `mech_debut_level` son la **única fuente** de debuts para el cap. 03 §9/§11 y el cap. 09 §1.1. Magic Sort introduce su primer bloqueador nuevo en el ~221 y recibe la crítica "tarde y sin novedad" (research/09 §Tipos de nivel [V]); aquí hay siete antes del 185.

### 9.1 Tabla maestra

| # | Mecánica | Debut [P] | Regla exacta | Tutorial de 1 pantalla (texto) | `attrs[i]` en el JSON (sintaxis del cap. 08 §4.1; este capítulo fija el catálogo de campos) |
|---|---|---|---|---|---|
| M1 | **Hoja** | 43 | Cuia **vacía** cubierta por una hoja de palma con el icono de un color c. Solo acepta vertidos de c mientras tenga hoja (`legal` en 1.2); con el primer vertido de c la hoja se desprende y la cuia pasa a ser normal. No es origen mientras tenga hoja (está vacía). Es un "vacío reservado" | "Esta hoja solo deja pasar aguaje. Vierte aguaje para abrirla." (dedo guía en el primer uso) | `{"type":"hoja","key":4}` (`key` = índice de motor del tinte) |
| M2 | **Brote** | 63 | Cuia con 1-3 dosis visibles y un tapón-brote en la boca cuyo color es el de su cima. **No es origen** mientras esté cerrado; como destino sigue la regla normal (mismo color). Al recibir un vertido de su color, el brote germina (se abre) y la cuia queda normal | "El brote está cerrado: vierte más camu camu encima para que germine." | `{"type":"brote"}` (color implícito = cima) |
| M3 | **Liana** | 83 | Cuia (vacía o con contenido visible) atada por una liana con un número n (1-2). No es origen ni destino. Se desata en el instante en que el jugador **sella** n cuias desde el inicio del intento. Reiniciar vuelve a atarla | "La liana se suelta cuando selles 2 cuias." | `{"type":"liana","n":2}` |
| M4 | **Remolino** | 103 | **Par** de cuias unidas por debajo. Tocar una como destino siempre significa remolino: la racha vertida entra por el **fondo** de la pareja, de cualquier color (no aplica la regla de mismo color); requiere `espacio(pareja) ≥ 1` y pareja no sellada; `cantidad` = `min(racha, espacio(pareja))` en `run` (por defecto, D-005) y 1 en `single` (pruebas; sección 1.2). Como origen, cada cuia del par vierte su cima con la regla normal. Ambas sellan como cualquier cuia; si una sella, la otra ya no puede recibir | "Las dos cuias del remolino están unidas por debajo: lo que viertes en una aparece en el fondo de la otra." | `{"type":"remolino","partner":7}` en ambas (`partner` = índice 0-based de la pareja) |
| M5 | **Barro** | 123 | Dosis inerte (índice 15) en posición 2 o 3 de una cuia de 4. No se puede verter (no es un color, corta rachas). Cuando queda **expuesta** en la cima, la cuia acepta **cualquier** color: el primer vertido lava el barro y ocupa su lugar (`espacio_efectivo + 1`). Con barro en la cima la cuia no es origen | "El barro acepta cualquier fruta; el primer vertido lo lava… y entierra lo que hay debajo." | Sin `attrs`: el barro es la unidad 15 dentro de `cuias[i]`, p. ej. `[8,15,3,3]` |
| M6 | **Lluvia** | 143 | Cuia abierta al cielo con nube y `gotas_pendientes`. Cada `lluvia_k_pours` (3) vertidos del jugador cae **una dosis del tinte agua del mundo** (Agua del Tapajós en aguas blanca/clara, Agua del Río Negro en negra) **encima** de lo que haya, sin regla de color, si hay espacio; si está llena, la gota espera al siguiente vertido. `U_agua` = dosis iniciales + gotas programadas = 4. La cuia Lluvia solo sella con agua; si el agua sella en otra cuia, la Lluvia debe terminar vacía. Al caer la última gota la nube se va | "La lluvia llena esta cuia cada tres vertidos: deja arriba el agua o saca la fruta antes de que la tape." | `{"type":"lluvia","key":10,"n":2,"k":3}` (`key` = tinte agua del mundo, `n` = gotas, `k` opcional; si falta, `lluvia_k_pours`) + contador en estado |
| M7 | **Piraña** | 183 | Una piraña ronda el tablero con contador de `pirana_k_pours` (4). Al cumplirse, **muerde** la dosis superior de la **última cuia destino** del jugador (si no está sellada) y la lleva a su **cueva** (una cuia marcada, normalmente Cuia chica de 2). Si la cueva está llena, la piraña duerme hasta que se vacíe una dosis. El jugador vierte desde la cueva con la regla normal. Sellar la cuia destino en el vertido n·k evita el mordisco | "Cada cuatro vertidos la piraña muerde la cuia que acabas de llenar y se lleva una dosis a su cueva." | `{"type":"pirana","k":4}` en la cuia cueva (`k` opcional; si falta, `pirana_k_pours`) + contador y `ultimo_destino` en estado |

Hoja (M1) usa una hoja de palma alargada; la hoja redonda de Victoria amazonica está reservada a Hoja escudo (glosario §6.3): la dirección de arte debe mantener ambas siluetas distintas.

En el modo `single` (solo pruebas) un nivel necesita más vertidos para el mismo progreso, así que los tics de Lluvia y Piraña llegarían antes respecto al avance. En `run`, el modo de juego desde D-005, no hace falta recalibrar `lluvia_k_pours` ni `pirana_k_pours` (pregunta P6 de `docs/dev/decisiones.md`, cerrada). Barro no cambia: el primer vertido lava el barro en los dos modos.

### 9.2 Interacción con solver, generador y comodines

| Mecánica | Solver / Morpho | Generador (barajado inverso) | Marea | Pororoca | Cuia extra / de rescate | Luciérnaga | Coste de implementación [P] |
|---|---|---|---|---|---|---|---|
| Cuia chica / alta | Heurística de cortes no depende de la capacidad (research/02 C4.5 [V]); se añade `cap_i` al estado canónico | Movimiento inverso respeta `cap_i`; `U_c` por color | Normal | Normal (reconstrucción inversa con capacidades) | Normal | — | S |
| Hoja | Estado: flag `hoja` por cuia; poda: destino solo c | Inverso: la hoja se "vuelve a poner" al vaciar la cuia hacia atrás | Restaura la hoja si el vertido deshecho la abrió | Las cuias con Hoja cerrada quedan fijas (no se barajan) | Normal | — | S |
| Brote | Flag `brote`; poda: no origen | Igual que Hoja | Restaura | Fijas | Normal | — | S |
| Liana | Contador de sellos en el estado | Inverso: la liana cuenta hacia atrás | Restaura contador | Fijas; el contador no cambia | Normal | — | S |
| Remolino | Transición distinta (inserción al fondo de la pareja); estado canónico conserva el emparejamiento | Inverso: extraer racha del **fondo** de la pareja hacia la cima del origen | Normal (registro de la inserción) | Permitido: reconstrucción con la transición de remolino | Normal | — | M |
| Barro | Color 15 inerte; `espacio_efectivo` | Inverso: el barro se repone al deshacer el lavado | Restaura el barro | Fijas las cuias con barro | Normal | — | S |
| Lluvia | Estado incluye `gotas_pendientes` y `vertidos mod k`; par y bot casual lo soportan | Las gotas se generan hacia atrás (quitar agua del tope cada k) | Restaura contador y gota caída | **Deshabilitado** en niveles con Lluvia (estado temporal) [P] | La nueva cuia es normal | — | M |
| Piraña | Estado incluye contador y `ultimo_destino` | Inverso del mordisco | Restaura el mordisco | **Deshabilitado** [P] | Normal | — | M |
| Aguas turbias | Enumeración de ≤24 realidades (T2); solo propone vertidos seguros; si agota `hint_timeout_ms` sin uno demostrable, Morpho no cobra y lo dice (T5; sin "Pista parcial") | Solo pools offline (research/02 C4.5 [V]) | Lo revelado permanece | Revela todas las capas (research/02 C4.5 [E]) | Normal | Revela una cuia | M |

Reglas transversales: (1) ninguna mecánica puede dejar el nivel sin movimientos útiles al inicio: el validador rechaza niveles con `atascado(S0)`; (2) cada mecánica debuta en 2-3 tableros consecutivos con fallo del bot <20 %, 2 vacíos, una sola mecánica (la que debuta) y un solo ejemplar de ella, en las posiciones 3-5 del ciclo de 20 (9.1; ejemplo canónico: el nivel 43, debut de Hoja, en el cap. 08 §4.1) [P]; (3) kill-switch `mech_enabled.<id>` por Remote Config: con false, los niveles que la usan se sustituyen por el nivel de respaldo del mismo slot del pool (research/02 §7.3); (4) máximo 2 mecánicas distintas por nivel antes del 300 y 3 después [P]; (5) Morpho y la pista por inactividad usan el mismo solver extendido; si el presupuesto se agota sin solución, Morpho no cobra y dice "No encuentro salida desde aquí" con Marea y Volver a la orilla.

### 9.3 Formato de nivel (ilustrativo)

**Ver cap. 08 §4.1 y `schemas/af-level-1.schema.json`** (esquema único `af-level/1`; este capítulo no define variantes propias). Resumen idéntico al canon: `caps[]` (1-6) y `cuias[]` (listas de índices de motor 1-16 de abajo arriba, 15 = Barro, `[]` = vacía) de la misma longitud; `attrs[]` con `{type, key, partner, n, k}` o `null` (catálogo de campos en 9.1: `n` = sellos necesarios en Liana o gotas programadas en Lluvia; `k` = ritmo en vertidos de Lluvia y Piraña, y si falta rigen `lluvia_k_pours` / `pirana_k_pours`; el Barro no es un `type` de `attrs`: es el tinte 15 dentro de `cuias`); `murky` + `hidden` como **lista** de `[cuia, capa]` (admite ocultos no contiguos); `units_per_color`; `relief` como **lista** de tableros completos `{type ∈ plus_empty/minus_color, …}`, obligatoria desde el nivel 31 (`par` nulo solo en `plus_empty`, cap. 03 §13); semilla `amazonfill:v1:m{mm}:v{v}:n{nnnn}` con ceros a la izquierda (ej. `amazonfill:v1:m03:v1:n0057`) e `id` `m{mm}_v{v}_n{nnnn}`; en memoria `AF.Core` empaqueta cada cuia como `len` + un nibble por unidad con el valor tinte − 1. El ejemplo canónico de 08 §4.1 es el nivel 43, debut de Hoja (`m03_v1_n0043`, `intro_mechanic` = "hoja": 2 vacíos, una sola mecánica y un solo ejemplar, regla transversal 2 de 9.2). El pipeline de generación y los campos de dificultad son del cap. 03 §10.

## 10. Niveles Difícil y Muy difícil

| Aspecto | Normal | **Difícil** | **Muy difícil** |
|---|---|---|---|
| Umbral del bot casual ×40 | <75 % de fallo | ≥75 % | ≥90 % (research/01 §6 [V]; dirección 5) |
| Qué cambia en el tablero [P] | 2 vacíos (1-30; excepciones tutoriales 1 y 6), 1 vacío desde ~31 | 1 vacío, K+1 color respecto al tramo, pares de riesgo hasta `max_pares_riesgo`, 1 Cuia chica como único búfer | 1 vacío, K+1 o K+2, 2 mecánicas activas, Aguas turbias permitidas desde el 100 |
| Señalización previa | Ninguna | Tarjeta de 1 toque antes del nivel: icono rana dardo, "Difícil", subtítulo opcional "Aguas bravas", recompensa 60 | Tarjeta: jaguar (solo animal), "Muy difícil", subtítulo "Noche de jaguar", recompensa 80 |
| En el HUD | — | Chip ámbar junto al nivel; marco ámbar sutil del tablero | Chip índigo nocturno; marco índigo; fondo 15 % más oscuro |
| Recompensa | 20 Semillas | 60 | 80 (dirección 12) |
| Cadencia | — | 5 niveles "reto" de cada ciclo de 20; primer nivel etiquetado Difícil: **40** (cap. 03 §11) | 1 hito por ciclo (dirección 5); primer Muy difícil: **60** (cap. 03 §12) |
| Honestidad | — | Garantía: solución sin comodines verificada por el solver; etiqueta recalibrada en vivo con intentos medios por nivel (`lvl_label_override`) | Igual; sin calavera (glosario §6.1) |

Los subtítulos temáticos se apagan con `ui_show_difficulty_subtitles=false` si confunden (glosario, Preguntas 6).

## 11. Aguas turbias (colores ocultos)

| # | Regla |
|---|---|
| T1 | Solo en niveles Difícil o Muy difícil desde `lvl_murky_from_level` = 100 (dirección 6); nunca en el Reto del río antes del 100 ni en Río sin fin generado en cliente (research/02 C4.5 [V]) |
| T2 | Máximo `lvl_murky_max_cuias` = 3 cuias turbias y `lvl_murky_max_hidden_units` = 4 dosis ocultas por nivel (canon para 02, 03, 04 y 08) (≤24 realidades, que el cliente enumera en <50 ms para Morpho, pista por inactividad y verificación de rescate; research/02 C4.5: 4 bloques = 24 disposiciones, 8 = 40.320 [E]; el lote de prueba del cap. 03 §8 midió 12 realidades como máximo con 4 dosis). El límite anterior de 6 dosis (≤720 realidades) se descarta: solo era tratable offline (0,7-8,5 s por nivel) y rompía el presupuesto de 50-150 ms del solver en cliente (cap. 08 §2.2 AF.Solver) |
| T3 | Una cuia turbia tiene como mucho 3 dosis al inicio (nunca llena) y su cima es visible. Las ocultas se dibujan como agua parda opaca con "?" y la cuia muestra "?×n" |
| T4 | Regla de no repetición (restricción del generador, garantizada en el cap. 03 §8): bajo una dosis oculta nunca se repite el color de la dosis visible inmediatamente superior; se comunica al jugador en el tutorial del debut de Aguas turbias. Reduce el azar percibido (queja n.º 1 de Magic Sort, research/09 §Mejorar [V]) |
| T5 | **Vertido seguro**: un vertido legal es seguro en el estado S si, para toda asignación de colores ocultos consistente con lo visible y con `units_per_color`, el estado resultante sigue siendo resoluble. El generador garantiza `seguros(S) ≠ ∅` en S0 y en cada estado de la ruta canónica (dirección 6). Morpho y la pista por inactividad solo proponen vertidos seguros. **No existe "Pista parcial"** (canon del cap. 04 §2.3): si el estado del jugador no está en la ruta canónica y no hay vertido seguro demostrable dentro de `hint_timeout_ms`, Morpho responde con honestidad ("No encuentro un camino desde aquí", con Marea y Volver a la orilla; regla transversal 5 de 9.2) y **no cobra**; la pista por inactividad, en ese caso, no pulsa nada. La verificación "la compra cumple" de la Cuia de rescate usa la realidad verdadera, que el cliente conoce: no enumera (cap. 04 §6.4) |
| T6 | Luciérnaga revela todas las dosis ocultas de **una** cuia (100-150 Semillas o anuncio; glosario §5.1). Pororoca revela todas las del nivel antes de barajar (research/02 C4.5 [E]). Lo revelado permanece revelado tras Marea y tras Volver a la orilla en el mismo intento |
| T7 | Aguas turbias · Bonus **(1.1)**: tarjeta opcional (cap. 06; `ui_murky_bonus_enabled` = false en 1.0) que ofrece jugar el siguiente Reto del río en turbio por premio doble; las reglas de nivel son T2-T6 |

```
validar_turbio(nivel):
  realidades = asignaciones(hidden, units_per_color, T4)      # campos del cap. 08 §4.1; ≤24 con 4 dosis ocultas
  para S en ruta_canonica(nivel):
    seguros = { m in movimientos_utiles(S) : para toda r en realidades: resoluble(aplicar(S, m, r)) }
    si seguros == ∅: RECHAZAR
  aceptar; guardar seguros(S0) como primera pista
```

## 12. Accesibilidad

| Requisito | Especificación |
|---|---|
| Patrón por tinte | Textura del tinte al 15 % de opacidad siempre (research/07 §8 [E]); con **Patrones de fruta** activado, patrón al **20 %** (`ui_pattern_opacity` = 0,20; 35 % solo en Alto contraste, canon del cap. 07 §3) + icono de fruta centrado en cada dosis (60 % del ancho de la dosis). Activado por defecto desde `ui_patterns_default_from_level` = 41 y disponible en Ajustes desde el nivel 1 (dirección 3) |
| Pares de riesgo | El generador respeta `max_pares_riesgo` 0/1/2/libre (dirección 3); la lista de 10 pares es la de research/07 §3.2 [E] y glosario §9 |
| Modo daltónico | No existe como modo aparte: es Patrones de fruta más contraste del patrón al 50 % (research/07 §3.2 [E]). Apple Arcade incluye modo daltónico en el género (research/01 §3 [V]) |
| Nombres | Mantener ≥400 ms sobre una cuia muestra el nombre de cada dosis (C9); el tooltip del icono seleccionado dice la fruta |
| Tamaño táctil | ≥48 dp por ranura (sección 3.3); separación mínima 8 dp entre áreas |
| Texto | ≥12 sp; contraste ≥4,5:1 sobre fondos (los fondos son desaturados y oscuros por diseño, research/07 §8) |
| Pantallas pequeñas | 320 dp: 6 cuias por fila; cuia mínima 28 dp de ancho con la ranura de 48 dp |
| Movimiento | "Reducir movimiento": `anim_reduce_motion_factor` 0,5, sin sacudidas, sin confeti; la onda del shader se apaga |
| Sonido | Cada acción tiene un sonido distinto (selección, vertido, rechazo, sellado); subtítulos del Dato del río; sin información solo por audio |
| Háptico | Desactivable; nunca en rechazo |
| Idioma | ES-419, EN, PT-BR desde 1.0 (glosario §10); textos del tutorial ≤2 líneas |
| Lector de pantalla | TalkBack/VoiceOver: toda cuia expone una etiqueta accesible con sus dosis de abajo arriba y los huecos ("cuia 3: açaí, açaí, aguaje, 1 hueco"; las dosis ocultas se leen "turbia"); todo botón lleva `contentDescription` con etiqueta funcional + estado ("Deshacer, 2 restantes"); el foco recorre las cuias por orden de ranura y después la barra de comodines; la selección se anuncia ("açaí seleccionado: elige destino") [P] |
| Tamaño de texto | Respeta la escala de fuente del sistema hasta el 130 % sin desbordes ni truncados a 360 dp (los contenedores crecen; nunca se recorta un precio ni una etiqueta de comodín); por encima del 130 % se fija ese tope. Prueba de desborde con fuente grande en la suite de QA del cap. 08 §14.2 [P] |
| Pista por inactividad | Interruptor "Pista por inactividad" en Ajustes (activado por defecto); apagado equivale a `hint_idle_delay_s` = 0 en el dispositivo y no afecta a Morpho (sección 3.4) [P] |

## 13. Tutorial: niveles 1-12 (Mundo 1 · Estuario y arranque del Tapajós)

Tabla única del arranque: el cap. 06 §2.2 copia sin alterar sus columnas de cuias, colores, vacíos, par y tiempo, y el `offer_scheduler` del cap. 05 y los desbloqueos del cap. 04 toman de aquí sus niveles. Aritmética: cuias = colores + vacíos. Debuts de tinte según glosario §9 (research/02 C3.3 [E]): Açaí y Aguaje 1, Taperebá 2, Guanábana 4, Bacaba 7, Agua del Tapajós 11. Fallo del bot 0-10 % (research/02 §7.2 [E]). Los niveles 1 y 6 son las "excepciones tutoriales" con 1 vacío de la dirección 4 (el 1 porque con dos colores un segundo vacío lo vuelve trivial; el 6 para que el jugador viva una vez, con ayuda a mano, la restricción que será la norma desde el 31); el resto del 1 al 30 lleva 2 vacíos. La tabla de research/05 §Primera sesión tenía filas donde recipientes ≠ colores + vacíos; se recalcula aquí.

| Nivel | Cuias = colores + vacíos | Colores | Par [P] | Novedad / enseñanza | Tiempo objetivo [P] |
|---|---|---|---|---|---|
| 1 | 3 = 2 + 1 | Açaí, Aguaje | 3 | Estado inicial: [açaí, açaí, açaí, aguaje], [aguaje, aguaje, aguaje, açaí], vacía. Dedo guía en los dos primeros toques (origen → destino); "termina la cuia casi llena antes de apartar" (research/09 §Onboarding [V]). Marea ya está en la barra, sin contador | 30 s |
| 2 | 5 = 3 + 2 | + Taperebá | 4-6 | Vertido parcial con dedo guía: una racha de 3 sobre un hueco de 2 (modo `run`, D-005). Dos vacíos por primera vez: "reserva uno". Primera cosecha (+20) | 45 s |
| 3 | 5 = 3 + 2 | — | 5-7 | Tutorial de Marea la primera vez que un vertido aleja de la solución (distancia del solver +2) o al inicio del nivel 5 si no ocurrió; Marea ilimitada hasta el 10 (`undo_unlimited_until_level` = 10, cap. 04 §2.1), contador oculto ("∞"); 3 por nivel con contador "3/3" desde el 11 | 60 s |
| 4 | 6 = 4 + 2 | + Guanábana | 7-9 | Cuatro colores; primera textura de patrón visible (blanco liso) | 75 s |
| 5 | 6 = 4 + 2 | — | 9-11 | Primer "color problema" repartido en 3 cuias | 75 s |
| 6 | 5 = 4 + 1 | — | 10-13 | Excepción tutorial: un solo vacío. Planificar 3 pasos; sin dedo guía. Si entra en Aguas estancadas, primer modal con Marea (ilimitada) y Volver a la orilla; tooltip "Reiniciar es gratis, siempre, mismo tablero. Desde el nivel 31, quedarte sin vertidos cuesta una canoa" (texto canon del cap. 04 §2.7) | 90 s |
| 7 | 7 = 5 + 2 | + Bacaba | 10-13 | Cinco colores; tras ganar, tarjeta de presentación del Reto del río (cap. 06) | 90 s |
| 8 | 7 = 5 + 2 | — | 12-15 | Capi regala 1 Cuia extra al inventario (desbloqueo del comodín; sin precio ni anuncio en el tutorial, cap. 04 §2.2; Desviaciones 1); dedo guía en su primer uso. Primer nivel con reinicio probable con 2 vacíos: enseñanza de Volver a la orilla con el mismo tooltip del nivel 6 ("Reiniciar es gratis, siempre, mismo tablero. Desde el nivel 31, quedarte sin vertidos cuesta una canoa") | 100 s |
| 9 | 7 = 5 + 2 | — | 13-16 | Variante de luz del fondo (alba → mañana) | 100 s |
| 10 | 7 = 5 + 2 | — | 14-18 | Canasta (contenido visible), Dato del río y Bitácora del río (cap. 06); sin pre-prompt de push: pasa a la primera victoria de la 2.ª sesión (cap. 06 §8.1; `push_preprompt_session` = 2, `push_preprompt_min_level` = 10); el Mercado se abre desde esta victoria (cap. 05 §2.1); fin del Estuario; último nivel con Marea ilimitada | 120 s |
| 11 | 8 = 6 + 2 | + Agua del Tapajós | 15-19 | Banner Mundo 2 · Tapajós (agua clara); el contador de Marea aparece en "3/3" (dirección 9); Cosecha doble como primer anuncio recompensado voluntario (dirección 13) | 120 s |
| 12 | 8 = 6 + 2 | — | 16-20 | Morpho se desbloquea con 3 unidades regaladas (cap. 04 §2.3): la primera se gasta con dedo guía. Tarjeta de Mochila de inicio (0,99 USD) en la **primera victoria con nivel ≥ 12 dentro de una sesión ≥ 2** (`eco_starter_first_show` = "L12_or_session2"; nunca en la primera sesión; ventana 10-20; cap. 05 §4.1) | 120 s |

**Par y lecciones (D-005, decisión del dueño del 2026-10-08).** La columna "Par [P]" y los tiempos se fijaron con la racha (modo `run`), que vuelve a ser el modo de juego. Los pares del pack v0 (3, 6, 7, 8, 10, 10, 11, 13, 14 y 16) entran en sus rangos y la rampa no decrece. En `single` (solo pruebas) serían 3, 9, 10, 12, 16, 17, 15, 17, 16 y 19 (`docs/dev/decisiones.md` §2.1.4). Las lecciones valen: el nivel 1 tiene par 3; el 2 enseña el vertido parcial; el 3 (tutorial de Marea) tiene 4 de 6 vertidos útiles iniciales que no acercan a la solución; el reparto en 3 cuias del 5 es una propiedad del tablero; y en los niveles 6 y 8 se llega a Aguas estancadas en 1 y 2 vertidos. Marea deshace un vertido (en `run`, la racha que movió). El dedo guía de los niveles 1-2 muestra un destino (sección 3.1, D-002; pregunta P5, abierta).

Sesión objetivo 10-14 min [P] para los niveles 1-10 (research/05 proponía 8-10 [E]; Magic Sort: 60 niveles el primer día y primer fallo en el 19 tras ~1 h 21 min, research/09 §Onboarding [V]). Tutorial jugado, nunca leído: una instrucción por nivel, ≤2 líneas, sin pantallas de texto (research/05 §4 [V]: 60 % de abandono en un tutorial que apilaba mecánicas). Los niveles 1-12 se construyen a mano y se validan con el mismo solver (research/02 §Preguntas [E]). Hitos dentro de la tabla: 3 tutorial de Marea; 7 Reto del río; 8 Cuia extra con una unidad gratis en inventario (Desviaciones 1); 10 Mercado; 11 contador de Marea y Cosecha doble; 12 Morpho con 3 unidades y, solo desde la 2.ª sesión, Mochila de inicio (`eco_starter_first_show`). Siguientes hitos: 14 Cuia chica; 20 Cántaro (cap. 05); 21 Camu camu y animación corta; 27 Cuia alta; 30 Pororoca; 31 Canoas + Cuia de rescate (tarjeta de 1 pantalla antes del nivel); 43 Hoja (sección 9.1).

## 14. Casos borde

| Caso | Comportamiento |
|---|---|
| Toque durante Sumergir y teñir | Salta al estado final en 0 ms; el toque **no** se interpreta como selección |
| Toque durante la celebración | Pasa a la pantalla de victoria; sin pérdida de recompensa |
| App a segundo plano a medio vertido | El modelo ya estaba actualizado y persistido; al volver, la vista salta al modelo sin animar; la selección se limpia |
| App cerrada o muerta a medio nivel | Al abrir: LOADING del mismo nivel desde el estado guardado (cuias, movimientos, stock de Marea, cuias añadidas, revelados, contadores); sin intro; si estaba en STUCK_MODAL se reabre el modal (sin cobrar canoa por el cierre) |
| Interrupción por llamada/anuncio durante una animación | Igual que segundo plano; el anuncio recompensado solo arranca desde modal o pantalla de victoria, nunca en PLAYING |
| Nivel N+1 no listo al pulsar Siguiente | "Cargando…" con lazo de río hasta 2 s; si falla la carga, reintento y mensaje "Revisa tu conexión" solo si el paquete es remoto; los primeros **700 niveles (mundos 1-9)** van instalados en el AAB (`levels_install`, cap. 08 §6.2) y del 701 en adelante son remotos (`levels_remote`, anunciados por `content_version`) |
| Rewarded sin inventario (`no_fill`) | El botón de anuncio se oculta; Marea solo por Semillas; Cuia de rescate sin alternativa de anuncio (no la tiene por diseño) |
| Marea con cola de animación pendiente | La cola salta a su fotograma final y después se anima el deshacer (200 ms, chorro inverso) |
| Marea tras Cuia extra | Deshace vertidos, no la cuia añadida |
| Pororoca | Vacía el historial de Marea (nuevo tablero); conserva stock |
| Cambio de hora/fecha | No afecta al nivel; afecta a "primer rescate del día" (reloj del dispositivo + cabecera `Date` del fetch, research/02 C4.2 [E]) |
| 16 cuias ya en pantalla | No puede ocurrir: T ≤ 14 generadas + 1 Cuia extra + 1 Cuia de rescate |
| Dispositivo de 2 GB RAM | Un material de líquido con arrays de colores/alturas, sin instanciar sprites por franja (research/06 §2 [V]); objetivo 60 fps con 16 cuias |
| Memoria baja al volver | Si el proceso murió, es el caso "app cerrada"; el estado se guardó tras cada vertido |

## Parámetros configurables (Remote Config)

**Ver cap. 08 §9**: es la única tabla de Remote Config del GDD (clave, tipo, default compilado, rango y dueño). Este capítulo no repite defaults; describe el comportamiento y usa exactamente estos nombres (los alias retirados `lift_ms`, `travel_ms`, `return_ms`, `pour_dose_ms`, `pour_tail_ms`, `celebration_ms`, `tint_anim_ms_*`, `tint_anim_fast_from_level`, `tint_anim_skippable`, `idle_nudge_s`, `idle_nudge_from_level`, `level_label_override`, `murky_from_level`, `murky_max_cuias`, `murky_max_hidden_doses`, `skip_after_fails`, `extra_cuia_tutorial_level`, `haptics_default_on`, `reduce_motion_factor`, `rescue_price` y `rescue_base_price` ya no existen):

| Sección | Claves que este capítulo usa (dueño 02 salvo indicación) |
|---|---|
| 1.2 Vertido | `rules_pour_mode` (D-005, decisión del dueño del 2026-10-08): `run` por defecto, la racha con vertido parcial; `single`, una dosis por vertido, solo pruebas; se aplica al cargar el nivel, nunca a mitad |
| 1.4 Bloqueo | `futile_rule_enabled` |
| 2 Máquina de estados | `stuck_modal_delay_ms`; `relief_after_fails` (03); `restart_confirm_min_pours`, `skip_min_fails`, `skip_min_gap_levels`, `skip_min_level`, `rescue_min_level`, `anim_river_lowered_banner_ms` (04; banner "El río bajó" del INTRO); `victory_autonext_s` = 0 (int: nunca auto-avance; cualquier valor > 0 jamás dispara AD_GATE sin toque) |
| 3 Controles | `tap_switch_selection`, `drag_pour_enabled`, `queue_max_lag_pours`; pista por inactividad `hint_idle_from_level` = 3 y `hint_idle_delay_s` = 12 (en todos los niveles desde el 3, sin tope; 0 = apagada); `hint_budget_nodes`, `hint_timeout_ms` (03: al agotarse no se cobra; sin Pista parcial) |
| 4 Animación | `anim_lift_ms`, `anim_travel_ms`, `anim_return_ms`, `anim_pour_dose_ms`, `anim_pour_tail_ms`, `anim_speed_factor_early`, `anim_speed_factor_late`, `anim_fast_from_level`, `anim_tint_duration_ms_early`, `anim_tint_duration_ms_late`, `anim_tint_fast_from_level`, `anim_tint_skippable`, `anim_celebration_ms`, `anim_reduce_motion_factor`; `anim_tint_skip_alarm_pct` (07) |
| 5 y 13 Comodines en el HUD y tutorial | `undo_unlimited_until_level` = 10 (Marea ilimitada en 1-10; 3 por nivel desde el 11), `extra_cuia_unlock_level` = 8 (1 unidad regalada), `hint_unlock_level` = 12 (3 unidades regaladas, `hint_tutorial_units` del cap. 04 §2.3), `shuffle_unlock_level`, `undo_refill_seed_price`, `extra_cuia_seed_price`, `hint_seed_price`, `shuffle_seed_price`, `reveal_seed_price` (04); `push_preprompt_session` = 2 y `push_preprompt_min_level` = 10 (06: pre-prompt de push en la primera victoria de la 2.ª sesión, nunca en la secuencia del nivel 10) |
| 6 Victoria | `streak_milestones`, `streak_daily_cap`, `streak_hits_once_per_day`, `streak_unlock_level` (05; también rigen la barra de la Crecida del borde derecho, sección 5); `eco_reward_normal`, `eco_reward_hard`, `eco_reward_very_hard` (05); `ads_rv_double_harvest_from_level`, `ads_first_interstitial_level` (05); `ui_river_fact_every_n_levels` (00); `eco_starter_first_show` = "L12_or_session2" (05) |
| 7 Aguas estancadas | `rescue_price_bands` = `[[31,600]]` (clave única; 600 fijo, sin `rescue_base_price`), `rescue_first_day_mult`, `rescue_max_per_level` = 1, `rescue_check_solvable`, `stuck_order_t1`, `stuck_order_t3`, `af_rescue_daily_cap`, `canoes_*` (04/05) |
| 9 Mecánicas | `mech_enabled.{hoja,brote,liana,remolino,barro,lluvia,pirana}`; `mech_debut_level.{chica,alta,hoja,brote,liana,remolino,barro,lluvia,pirana}` = 14 · 27 · 43 · 63 · 83 · 103 · 123 · 143 · 183 (única fuente de debuts, sección 9.1; 08 §9 copia estos valores); `lluvia_k_pours`, `pirana_k_pours` |
| 10 Dificultad | `lvl_labels_from_level` = 40, `lvl_very_hard_from_level` = 60, `lvl_label_override`, `ui_show_difficulty_subtitles` (03/00) |
| 11 Aguas turbias | `lvl_murky_from_level` = 100, `lvl_murky_max_cuias` = 3, `lvl_murky_max_hidden_units` = 4 (≤24 realidades; T2), `ui_murky_bonus_enabled` (false en 1.0; true en 1.1) |
| 12 Accesibilidad | `ui_patterns_default_from_level` (00), `ui_pattern_opacity` = 0,20 (07), `ui_haptics_default_on`, `anim_reduce_motion_factor` |

Los precios de comodines y rescate los fija el cap. 05 y los muestra este capítulo; el modal lee `rescue_price_bands` (un único tramo `[[31,600]]` en 1.0), nunca una constante en código.

## Desviaciones de la dirección

1. **Cuia extra gratuita en el nivel 8** (dirección 9: 150 Semillas, 1 por nivel). Se concede **una** unidad al inventario en el nivel 8 (Capi la trae; coherente con cap. 03 §4.4 y cap. 04 §2.2) para enseñar el comodín jugándolo, igual que la Canasta regala comodines; el precio y el límite por nivel no cambian. Razón: el tutorial debe ser jugado, no leído (research/05 §4 [V]).
2. **Marea ilimitada en los niveles 1-10** (dirección 9: 3 usos gratis por nivel). Durante el tutorial Marea no tiene contador ni tope (`undo_unlimited_until_level` = 10; coherente con cap. 04 §2.1) para que el jugador aprenda a deshacer sin miedo a gastar; el contador "3/3" y la recarga por anuncio o Semillas entran en el 11 junto con la primera ubicación rewarded. Razón: la misma de la desviación 1, y que el primer bloqueo probable (nivel 6, un solo vacío) se resuelva deshaciendo y no solo reiniciando. Del 11 en adelante rige la dirección 9 sin cambios.
3. **Una dosis por vertido** (dirección 2: "racha contigua sobre vacío o mismo color", la regla de todos los líderes, research/01 §2 [V]). Decisión del dueño del 2026-10-06 (D-001), tomada tras jugar el prototipo v0 en su teléfono: en el modo por defecto (`rules_pour_mode` = `single`) cada vertido mueve una dosis y no existe el vertido parcial; la regla de la dirección se conserva como modo `run`, seleccionable por Remote Config para probarla sin tocar código (sección 1.2, R9). Consecuencias: más vertidos y toques por nivel (par del tutorial ×1,0-×1,7), más estados para el solver (03 §5) y recalibración en `single` de lo que cuenta vertidos o se midió con rachas (cifras [V-proto] de 03, Marea "3 por nivel", `restart_confirm_min_pours`, `lluvia_k_pours`, `pirana_k_pours`) antes de la Fase 1 (pregunta P6 de `docs/dev/decisiones.md`). *Superada el 2026-10-08 por D-005: el modo por defecto vuelve a ser `run` y la dirección 2 rige sin desviación.*
4. Ninguna otra. "Vacío o monocolor" (dirección 2) se precisa como "vacío o sellado (lleno monocolor)", que es la regla del género (research/01 §2 [V]); la distinción fallo duro / fallo blando (canon cap. 04 §5.2: el reinicio voluntario nunca gasta canoa, pero con ≥ 8 vertidos cuenta para el alivio y el Salto) y Cuia de rescate máx. 1 por nivel (`rescue_max_per_level` = 1, decisión cerrada en la revisión del 06-10) son precisiones que la dirección no fijaba. La Cuia de rescate a 600 fijo es el valor por defecto de la dirección 10 (los tramos 750/900 quedan como hipótesis del cap. 05, no como configuración).

## Preguntas abiertas para el dueño del producto

1. **Remolino (M4)**: ¿se acepta la regla "entra por el fondo de la pareja, cualquier color" o se prefiere la versión simple de una sola cuia (lo vertido va al fondo de la misma cuia)? Decidir tras prototipo de legibilidad con 5 jugadores.
2. **Reinicio voluntario**: Cerrada: canon del cap. 04 §5.2 (fallo blando con ≥ 8 vertidos: cuenta para el alivio y el Salto del Boto, nunca gasta canoa ni rompe la Crecida; salir al Mapa del río no es fallo). El umbral `restart_confirm_min_pours` = 8 (rango 5-12, 08 §9) solo se calibra en soft launch con la distribución real de vertidos antes de reiniciar (GDD §6 #7), sin reabrir la regla.
3. **Cuia de rescate**: Cerrada: 1 por nivel (`rescue_max_per_level` = 1, rango 1; revisión del 06-10 y cap. 04 §3/§6.2): T ≤ 14 generadas + Cuia extra + Cuia de rescate ≤ 16 ranuras; el generador no baja a T ≤ 13.
4. **Doble toque**: ¿mantener sin función o permitir "vertido al único destino legal" como atajo (riesgo de vertidos accidentales)?
5. **Lluvia y Piraña** (mecánicas con estado temporal): ¿entran en 1.0 (niveles 143 y 183, dentro de los 700 instalados; cap. 08 §6.2) o se posponen a la 1.1 para reducir alcance del solver? Criterio (GDD §6 #6): 1.0 solo si el solver con estado temporal pasa los tests al cierre de la Fase 1; si no, 1.1 **sin mover los debuts** (143 y 183 siguen siendo canon en 9.1 y `mech_debut_level`).
6. **Auto-avance** en la pantalla de victoria: Cerrada: `victory_autonext_s` = 0 (int) en 02, 06, 07 y 08; el intersticial exige toque explícito en Siguiente (GDD §6 decisión 14); no hay valor alternativo de 2,5-3 s.
7. **Regla de no repetición T4** ("bajo una dosis oculta nunca se repite el color de encima"): Cerrada: se comunica al jugador en el tutorial del debut de Aguas turbias y el cap. 03 §8 la garantiza en el generador.
8. **Tiempo de la primera sesión**: aceptar objetivo 10-14 min para los niveles 1-10 (frente a 8-10 de research/05 [E]).
9. **Mochila de inicio**: Cerrada: primera tarjeta en la primera victoria con nivel ≥ 12 dentro de una sesión ≥ 2 (`eco_starter_first_show` = "L12_or_session2"; ventana 10-20 se mantiene; cap. 05 §4.1 y 06).
10. **Aguas turbias**: Cerrada: máximo 4 dosis ocultas / 24 realidades (`lvl_murky_max_hidden_units` = 4) y sin "Pista parcial" en 02, 03, 04 y 08.
11. **Una dosis por vertido (D-001) y sin resaltado de destinos (D-002)**: decididas por el dueño el 2026-10-06. D-001 quedó sustituida por D-005 (2026-10-08): P2, P3, P4 y P6 cerradas; P7 reescrita; P5 (dedo guía y Morpho frente a D-002) sigue abierta (`docs/dev/decisiones.md` §5).

## Dependencias con otros capítulos

Numeración canónica de capítulos según el glosario (§Dependencias): 00 Glosario · 01 Visión · 02 Núcleo · 03 Niveles · 04 Comodines · 05 Economía y monetización · 06 Meta y live-ops · 07 Arte · 08 Arquitectura y publicación · 09 Roadmap.

| Capítulo | Toma de este capítulo | Devuelve a este capítulo |
|---|---|---|
| 00 Glosario | Nombres de mecánicas (Hoja, Brote, Liana, Remolino, Barro, Lluvia, Piraña) confirmados con regla exacta; distinción Hoja (palma) vs Hoja escudo (Victoria amazonica); Cuia alta: debut 27 (cap 5) y cap 6 solo en hitos desde el 141 | — |
| 03 Niveles, generación y dificultad | Reglas R1-R16 como contrato del solver y del validador; transiciones y semántica de M1-M7 (03 §9 deriva de 9.1, no al revés); tabla de debuts de 9.1 y `mech_debut_level` como única fuente (14·27·43·63·83·103·123·143·183, bloqueadores en la posición 3 del ciclo); `futil` (con la excepción de dosis ocultas); `validar_turbio` con `lvl_murky_max_hidden_units` = 4 y regla T4 garantizada en 03 §8; restricciones de capacidad y de mecánicas por nivel; catálogo de `attrs` de 9.1 (sintaxis del cap. 08 §4.1); `rescue_max_per_level` = 1; rampa 1-12 de la sección 13 (excepciones de 1 vacío en 1 y 6; 03 la copia sin alterarla) | Pipeline, pesos de dificultad, primer Difícil 40 y primer Muy difícil 60 (§11/§12), ciclo de 20 de §12 (retos p6, p10, p14, p17, p19; recuperación p1-p2; hito p20) en el que la sección 9 sitúa los debuts, pools de variantes aliviadas, topes diarios; definición de fallo: ambos remiten al cap. 04 §5.2 |
| 04 Comodines, vidas y rescate | Desbloqueos escalonados (Marea 1, Cuia extra 8 con 1 unidad, Morpho 12 con 3 unidades, Pororoca 30, Luciérnaga ≥100); Marea ilimitada hasta el 10 y 3 por nivel desde el 11; vertido seguro en turbio (T5) y `hint_timeout_ms` sin "Pista parcial"; estados FAIL_RESOLVE / SKIP_RESOLVE de la sección 2 | Canon de la definición de fallo (§5.2; la sección 2 remite y resume); frase "qué bloquean 0 canoas" (§7.1); topes por nivel/día; reglas detalladas de cada comodín |
| 05 Economía y monetización | Ubicación de Cosecha doble (≥11), AD_GATE tras toque en Siguiente (≥22), hoja "sin saldo" del modal, Balsa de rescate; Mercado desde la victoria del 10 y Mochila de inicio en la primera victoria con nivel ≥ 12 en una sesión ≥ 2 (sección 13) | `rescue_price_bands` = `[[31,600]]` (600 fijo, sin tramos ni cohortes geográficas), escalera de la Crecida (§2.4), cadencia de intersticiales (§6), precios locales y de comodines |
| 06 Meta, retención y live-ops | Barras de Crecida/Reto del río/Canasta en victoria; distinción fallo duro / blando (canoas, Crecida, Hoja escudo; canon cap. 04 §5.2, referenciado en la sección 2); tarjeta Aguas turbias · Bonus (1.1); banner de mundo; tabla 1-12 de la sección 13 (06 §2.2 copia sin alterar sus columnas de cuias, colores, vacíos, par y tiempo, con el Reto del río tras la victoria del 7 y sin pre-prompt de push en la del 10); barra de la Crecida vertical en el borde derecho (sección 5, posición de 07 §7.4) | Reglas de Canoas, Crecida, Bitácora, Dato del río, pre-prompt de push (2.ª sesión, §8.1) |
| 07 Arte, temática y audio | Duraciones y curvas de la sección 4; tapa de calabaza; iconografía de M1-M7; Capi en orilla, Rosa en banner; confeti de guacamayo; maracá y toada; requisitos de accesibilidad de la sección 12 (lector de pantalla, fuente 130 %, interruptor de pista); Cuia alta de 6 solo en hitos desde el 141 | Orden canónico de la bandeja de comodines y posición de la barra de la Crecida, vertical en el borde derecho (§7.4, copiados en la sección 5); alturas de las variantes de cuia (§4.5); opacidad del patrón 20 % / 35 % en Alto contraste (§3); hojas de estilo, sprites, bancos de sonido |
| 08 Arquitectura técnica y publicación | Shader de líquido (arrays de colores/alturas, inclinación, onda), persistencia tras cada vertido, precarga N+1, presupuesto del solver en cliente (≤24 realidades en turbio), comportamiento de las claves de Remote Config que este capítulo usa; §13: sin azar en ninguna mecánica ni modal (Canasta visible, sin ruleta), textos del tutorial sin términos prohibidos; §14.2: suite "Accesibilidad: TalkBack recorre el tablero; fuente 130 % sin desbordes" | Tabla única de Remote Config (§9); esquema de nivel `af-level/1` (§4.1, ejemplo canónico del nivel 43) y `schemas/af-level-1.schema.json`; taxonomía de analítica (§8.2: el salto de Sumergir y teñir es `af_anim_skipped`); 700 niveles instalados (≥600 jugables; §6.2); implementación de Addressables, Play Asset Delivery, Firebase; revisión legal de textos |
| 09 Roadmap, equipo, KPIs y riesgos | Debuts de mecánicas de 9.1 para el plan de contenido (§1.1); niveles 1-12 construidos a mano en la Fase 1 | Fechas de entrega de mecánicas por versión |
