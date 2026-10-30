# Capítulo 4 · Comodines, Canoas y Cuia de rescate

Fecha: 2026-10-06 (revisión cruzada; versión anterior 2026-10-05; actualizado el 2026-10-08 con la decisión del dueño **D-008**, bucle de partida como Magic Sort: §2.7, §3, §5.2, §6, §7, §10.1 y §11, y con la nota de la pregunta P17 en §7.3; alineado el mismo día con lo construido en la pista B y con Magic Sort: panel de Aguas estancadas sin ilustración ni `stuck_rescue_copy` en §6.5-§6.6 y cronómetro de canoas fuera de la cabecera de partida en §7.1, ambos [P]; registro en `docs/dev/decisiones.md`; actualizado el 2026-10-11 con **D-007**, mecánicas del río, y lo construido en el paso 1 del traspaso: Resumen, §2.1, §2.2, §2.4, §2.5, §2.6, §4, §5.1 y Dependencias; actualizado el 2026-10-12 (paso 2 del traspaso) con D-011 (el aviso del regalo de Capi espera a la intro: §2.2), D-012 (guardado por vertido con escritor en segundo plano: §5.2), la lista única de estrenos de 02 §9.1 y lo que pidió la revisión: §2.5 (Inventario y HUD), §2.6, §4, §5.1 y Dependencias; actualizado el 2026-10-13 con **D-013**, el Mapa del río («río arriba» en §2.6 y §6.5; el Mercado, desde el inicio y no desde el Mapa, en §4), y **D-014**, Aguas estancadas por bucle (§5.1, pendiente de la pregunta 46 de GDD.md §6.3); actualizado el 2026-10-14 (tanda 3, T54) con **D-016**, TiTino (§2.1 y §2.3: TiTino presenta Marea en el 3 y Morpho en el 12; §2.7: su aviso tras Volver a la orilla), **D-019**, la Tinaja (§2.3: orden de respuestas de Morpho, en la regla de uso y en el caso a; §2.4; §2.5; §2.7: qué es un intento, la confirmación de reinicio y lo revelado tras un fallo blando o duro; §5.1; §6.1: qué es un intento), y **D-020**, las preguntas 29, 31 y 46 (§2.5 y §5.1), con la nota de `stuck_skip` de §6.5 fuera de la celda del texto; actualizado el 2026-10-14 (T58) con **D-022** (§2.5: Revelar sin medios responde con la burbuja, sin aviso, pregunta 48) y dos retoques de la revisión de T54 (§2.1 y §2.5: la ranura de Revelar también con la tinaja; §2.6, §4 y Dependencias: el 323 de la Tinaja en la lista única de estrenos, contrato 1, D-019, Q-Ti3-A); actualizado el 2026-10-14 (D1) con **D-023**, las decisiones de detalle de la tanda 4 (§5.2: la fila «Abrir el Mapa del río desde la pausa», que no es salir del nivel ni fallo, D-013, punto 7, y su lectura para 02 y 03); actualizado el 2026-10-19 (DC1) con **D-028**, las respuestas del dueño a las preguntas 19, 22-24, 53, 54, 57, 60, 61 (con P5) y 65 de GDD.md §6.3 (§2.3: la primera Pista sin dedo guía, 61, y P5; §2.5: vídeo primero en Revelar, 23); actualizado el 2026-10-19 (DC1b) con **D-029**, las recomendadas del dueño (§2.1: el tooltip de Marea del 3 tras el primer vertido que no acerca y sobre el muelle, pregunta 56; §2.5: la tarjeta combinada del 100 con el regalo «+1 Luciérnaga», ratificada, pregunta 21; §2.7: el hoatzin del mundo 11, Q17; §5.2 y §6.2: P16; §6.5: los textos de P14; §7.3: P17 A; §9: las cifras [sim] del ciclo de D-009, pregunta 25; Preguntas abiertas 9); actualizado el 2026-10-21 con **D-038**, `biz_feature_lives` apagado: desde el 31 todo funciona como en 1-30 (§5.2, §6, §7.2 y Parámetros); actualizado el 2026-10-21 con **D-033**, B3-Q-S1: el hueco del Salto del Boto se mide con `skipped_levels`, no con `since_last_skip` (§2.6, §5.2, §6.1-§6.3, §10.1 y §11), y con **D-035**, F3-3: `applied_purchases` guarda el `token_hash` (§10.1); actualizado el 2026-10-21 con **D-033** (Q-B15): el ingreso efectivo por victoria y los ratios de §2.4, §2.5, §3 y §9 y la fila 05 de Dependencias toman las cifras [sim] de la corrida con la Bitácora «1 de 3» (05 §3.2) · Proyecto Amazon Fill (nombre comercial Tintalú) · Convención de evidencia: **[V]** verificado en el informe citado, **[E]** estimación del informe, **[R]** reseñas de jugadores, **[NV]** no verificado, **[P]** propuesta de este capítulo a calibrar en soft launch. "(dirección N)" remite a las decisiones de dirección vinculantes. Nombres según `docs/gdd/00-glosario.md`.

## Resumen

Tintalú copia el set de ayudas probado del género (Deshacer, +1 recipiente, Pista, Reiniciar gratis, x2 por anuncio) y añade tres piezas que el líder no tiene: Barajar con resolubilidad garantizada, Revelar turbio y Saltar condicionado. El modal de bloqueo **Aguas estancadas** es el motor de ingresos heredado de Magic Sort (>50 % de su IAP sale de la pantalla de derrota, research/09 §Por qué ganó 2 [V]) corregido en sus tres quejas estructurales: Marea (deshacer) se puede usar en el bloqueo, el reintento es determinista y el precio del rescate (600 semillas fijas desde el nivel 31, `rescue_price_bands` = [[31,600]]; 300 el primero del día; máximo 1 por nivel) deja un ratio de presión de 15,8:1 sobre el ingreso en nivel frente al 45:1 del líder (9,8:1 casual · 6,1:1 medio sobre el ingreso efectivo simulado en 05 §3.2). Las **Canoas** (5 vidas, 25 min) y la Cuia de rescate debutan juntas en el nivel 31; antes de eso fallar solo reinicia gratis. El capítulo fija para cada comodín la regla exacta de uso, los casos borde con el solver y con las ocho mecánicas de la 1.0 (Hoja, Matapi, Liana con fruta, Remolino, Barro, Lluvia, Brote, Piraña; D-007), el árbol de decisión completo del bloqueo con sus wireframes (desde el 31, con el embudo de D-008: Aguas estancadas → ¿Seguro? → Nivel fallido → Sin canoas), el esquema de inventario sincronizado por Play Games Saved Games y la lista de eventos de analítica a instrumentar.

## 1. Principios del sistema de ayudas

| # | Principio | Origen |
|---|---|---|
| 1 | **El comodín se ofrece en el momento de la necesidad, nunca antes del nivel.** La preselección de boosters ("Wizard's Gear") es la queja n.º 1 de Magic Sort: los boosters guardados no sirven en la derrota (research/09 §Comodines [V]; research/03 §6 [V]). | dirección 9 y 10 |
| 2 | **Una sola moneda (semillas) y ningún comodín aleatorio.** Sin ruleta, sin cofres de pago; Canasta y Bitácora del río entregan comodines con contenido **determinista por nivel/día y visible antes de abrir** (ciclo fijo de la Canasta, capítulo 06 §4.4; research/08 C5.1 [V]). | dirección 12 y 15 |
| 3 | **Deshacer generoso y usable en el bloqueo.** El cupo de 3 gratis por nivel más recarga barata evita la queja "un anuncio de 30 s por 5 deshacer" (research/03 §3.1 y §3.3 [V]). | dirección 9 |
| 4 | **La compra cumple.** Una cuia vacía de capacidad 4 resuelve casi siempre el nivel ("de super hard a super easy", research/magic-sort/b §5 [R]); si el solver detecta que ni con una cuia más hay salida, no se vende (§6.4). | research/09 §Qué copiar 3 |
| 5 | **Regla de HUD:** icono + etiqueta funcional ("Deshacer", "Pista", "Barajar"); el nombre temático va en el tooltip de primer uso, en el Mercado y en la animación. | glosario §0.2 |
| 6 | **Nada de autocompletar ni de pista que resuelva el nivel entero** (Get Color, research/03 §2 [V/E]): Morpho da un movimiento por uso. | research/03 §Implicaciones |
| 7 | **Nunca subir un precio sin subir la recompensa en la misma versión** (causa n.º 1 de reseñas de 1-2 estrellas en Hexa Sort y Screw Jam, research/03 §4 [V]). Los precios de este capítulo bajan por Remote Config; para subirlos hace falta una versión con más semillas por victoria. | research/03 §4 |
| 8 | **Determinismo:** mismo tablero al reiniciar; la variante aliviada solo tras 3 fallos y siempre anunciada con "El río bajó". | dirección 7 |

### 1.1 Dos modelos de inventario

| Modelo | Comodines | Cómo se muestra | Persistencia |
|---|---|---|---|
| **Cupo por nivel** | Marea (3 gratis) | Badge numérico "3" sobre el botón; al llegar a 0 el badge muestra "+3" con el icono de video o el precio | Se restaura con Volver a la orilla y al cargar cualquier nivel |
| **Stock consumible** | Marea comprada (`undo_stock`), Morpho (`hint_stock`), Luciérnaga (`reveal_stock`), Cuia extra (`extra_cuia_stock`), Pororoca (`shuffle_stock`: solo la que premian 7 Retos del río seguidos, §2.4) | Badge "+N" en color semilla junto al cupo; el botón consume primero el cupo gratis y luego el stock | Vive en el perfil; sobrevive al nivel, al reinicio y al cambio de dispositivo (§10) |
| **Sin stock (solo semillas)** | Salto del Boto, Cuia de rescate | El botón muestra el precio en semillas; con saldo insuficiente, el precio en gris y "Te faltan N" | — |
| **Temporizado** | Canoas infinitas | Cronómetro en la cabecera en lugar del contador 5/5 | UTC de fin (`infinite_canoes_until`) |

## 2. Fichas de comodines

### 2.1 Marea · Deshacer (`undo`)

| Campo | Especificación |
|---|---|
| Función exacta | Revierte el último vertido legal y restaura el **estado exacto** anterior: unidades, sellos (una Cuia sellada por ese vertido vuelve a abrirse), estados de mecánicas (una Hoja o un Brote abiertos por ese vertido se cierran; una Liana soltada por ese vertido se vuelve a atar; una dosis robada por Piraña o añadida por Lluvia como consecuencia del vertido se revierte). Lo único que **no** se revierte es el conocimiento: las unidades reveladas de Aguas turbias siguen visibles (research/01 Especificación 8 [V]). Se puede encadenar hasta vaciar el historial. |
| Regla de uso | Disponible en cualquier estado con historial no vacío, **incluido el bloqueo**: en el modal Aguas estancadas es la primera opción si queda cupo o stock; si no, muestra "+3 por video" o "+3 por 30 semillas" (dirección 9; research/03 C2.2 Regla 0 [E]). Deshacer desde el modal cierra el modal y no cuenta como fallo. No deshace comodines (Cuia extra, Pororoca, Luciérnaga): tras una Pororoca el historial se vacía y el botón queda inactivo hasta el siguiente vertido. |
| Desbloqueo y tutorial | Botón visible desde el nivel 1; tutorial en el **nivel 3** [P] (research/02 §Curva [E] propone el 3): tras el primer vertido que **no acerca** a la solución (en el tablero del 3 ningún vertido útil aleja, así que la letra «+2» de 02 §13 no saldría nunca), TiTino señala el botón con su retrato y el tooltip "Marea · Deshacer: toca para devolver el agua", sobre el muelle, pegado a la bandeja y con la cola en el eje de Marea (si no cabe con la cola, sin ella) (pregunta 56 de GDD.md §6.3, (a), decidida el 2026-10-19 en D-029; D-016, T5-B: TiTino en el tutorial; antes, Capi). Ese primer uso no descuenta cupo. |
| Usos gratis | **3 por nivel**, restaurados al reiniciar y al cargar el nivel (dirección 9; research/03 C2.4 [E]). Niveles 1-10: cupo ilimitado (`undo_unlimited_until_level` = 10, excepción de tutorial a la dirección 9 adoptada por todo el GDD; 3 por nivel desde el 11, 02 §13) para no castigar el aprendizaje (research/05 §Implicaciones 2 propone ilimitado hasta el 20; se acorta al 10 para que el jugador conozca la recarga antes del primer rewarded del nivel 11). |
| Costo en semillas | **30 semillas = +3 usos** al stock (≈0,06 USD a 0,002 USD/semilla, research/04 C1 §2 [E]); sin tope de compra. |
| Anuncio recompensado | Ubicación `rv_undo_refill`: **+3 usos**, tope **4/día**, cooldown **60 s** [P dentro de la banda 3-5/día y 60-90 s de la dirección 13]. Si no hay fill (`NO_FILL`), se oculta el botón de video y queda solo el precio (research/03 C2.2 [V SDK]). |
| Inventario y HUD | Ranura 1 (extremo izquierdo) de la bandeja cumbuca, según el **orden canónico de 07 §7.4**: [Deshacer · Marea] [+1 cuia · Cuia extra] [Pista · Morpho] [Barajar · Pororoca] (+ [Revelar · Luciérnaga] solo en niveles con ocultos: Aguas turbias y, desde D-019, la Tinaja); este capítulo no define posiciones propias. Coincide con la posición estándar del género (research/03 §Catálogo). Badge: cupo gratis en blanco; stock comprado como "+N" en ámbar semilla. Botón gris con historial vacío. |
| VFX / SFX | El chorro vuelve hacia arriba a la cuia de origen en 250 ms [P] (animación inversa del vertido; valor único, 02 §14 y `BoardAnimator.UndoTransferMs`), una ola pequeña retrocede en la superficie de la cuia de destino; si se deshace un sello, la tapa de calabaza salta con un "pop" seco. Las demás cuias que cambiaron (la gota, el mordisco, la Liana que se vuelve a atar) saltan a su estado. Sonido: splash inverso de 250 ms + maracá suave (research/07 §6.3 [V]). Háptico ligero. Hoy el cliente no tiene audio ni háptico. |
| Casos borde | (a) Solver: no interviene. (b) Remolino: un vertido a través del portal se deshace como un solo movimiento; como el destino no cambió (lo vertido entró por el fondo de la pareja), no hay traslado: el origen y la pareja saltan a su estado anterior sin animación [P] (02 §14). (c) Lluvia y Piraña: la gota o el mordisco que provocó el vertido están en el registro del movimiento y se revierten junto con él, con el contador; un vertido fútil no los avanzó (Q18 B de D-007). (d) Cuia de rescate: el historial previo al rescate se conserva; deshacer movimientos anteriores no elimina la cuia añadida. (e) Al reanudar la app, el historial se restaura desde el guardado del nivel (§10). (f) SSV: la recarga por video se concede de forma provisional; si el servidor la revoca, se descuenta del stock (nunca por debajo de 0) y se marca `fraud_flag` (research/06 C6.1 [V]). |

### 2.2 Cuia extra · +1 cuia (`extra_cuia`)

| Campo | Especificación |
|---|---|
| Función exacta | Añade **una cuia vacía de capacidad 4** (siempre 4, aunque el nivel use Cuia chica o alta) en la fila más corta (a igualdad, la de más abajo [P]); el número de filas se fija al cargar el nivel con las T generadas y no cambia (D-006; 02 §5): solo se recalcula el ancho; el máximo absoluto es **16 cuias en pantalla** (02 R1 y §5: T ≤ 14 generadas, filas de 02 §5; 03 §11: "ningún nivel generado supera las 14 cuias"; 08 §5.4), de ahí que solo puedan añadirse 2 cuias por nivel (T ≤ 14 + 1 Cuia extra + 1 Cuia de rescate ≤ 16). Si algún día el generador publicara hitos de 15-16 cuias, 02 §5 tendría que crecer a 6/6/6 (18 ranuras) antes de tocar este tope. La cuia añadida nunca es turbia, nunca está atada (Liana), tapada (Hoja/Brote), bajo Lluvia ni conectada a un Remolino, no es un Matapi y no contiene Barro. |
| Regla de uso | **Comodín planificado**: solo mientras quede ≥1 vertido legal no fútil (si no queda, lo que se ofrece es la Cuia de rescate, §6). **1 por nivel** por cualquier vía (semillas o stock) [P; la dirección 9 fija "1 por nivel con semillas" y este capítulo extiende el tope al stock para que dos cuias planificadas no trivialicen el nivel]. **Persiste** al reiniciar el mismo nivel y al cerrar la app (research/03 C2.4 [E]; corrige la regla 17 de research/01 que la eliminaba al reiniciar). Desaparece al ganar, saltar o abandonar el nivel. Tope total de cuias añadidas por nivel: **1 Cuia extra + 1 Cuia de rescate = 2** (`rescue_max_per_level` = 1, capítulo 02 §7: T ≤ 14 + 2 ≤ 16 ranuras). |
| Desbloqueo y tutorial | **Nivel 8** [P] (research/03 §Catálogo propone el 8 "cuando hay ≥6 recipientes"; research/05 §Primera sesión regala una en el 8): Capi entra desde la orilla izquierda (su orilla decorativa, 02 §4.1 y §5) con una cuia sobre la cabeza y la deja; se acredita **1 Cuia extra gratis al stock** con el texto "Capi te trae una cuia más para este nivel. La próxima la encuentras en tu Canasta". El aviso no sale durante la carga, la tarjeta previa ni la intro: espera a que se habilite el toque y dura según lo que hay que leer (D-011, punto 2; 02 §4.2) [P]. El Mercado no se menciona porque abre en el nivel 10 (05 §2.1); hasta entonces la única vía de Cuia extra es el stock regalado. No hay anuncio ni precio en el tutorial (research/05 §Primera sesión [E]: "introduce la ayuda, no el precio"). |
| Usos gratis | 1 en el tutorial; después solo por Canasta (**niveles 40 y 90 de cada 100**: ciclo fijo [3 Marea, Morpho, Cuia extra, 3 Marea, Morpho] desde el nivel 20, capítulo 06 §4.4; contenido determinista y mostrado antes de abrir, dirección 15; sustituye al 15 % probabilístico de research/04 C1 §3 [E]) y Bitácora del río (días 6, 14, 23). |
| Costo en semillas | **150** (dirección 9) ≈ 0,30 USD; equivale a 3,9 victorias de ingreso en nivel o a 2,4 victorias del casual / 1,5 del medio sobre el ingreso efectivo de 05 §3.2 (§9). |
| Anuncio recompensado | **No** (glosario §5.1, regla económica de Cuia extra: solo semillas; la dirección 9 y 13 no incluyen cuia por anuncio). Razón: es el comodín que más baja la dificultad (research/02 §Implicaciones [E]: "equivale a bajar un escalón entero") y la ruta por video lo convertiría en rescate gratuito diario que canibaliza la Cuia de rescate. |
| Inventario y HUD | Ranura 2 de la bandeja cumbuca, a la derecha de Marea (orden canónico 07 §7.4); etiqueta "+1 cuia"; badge de stock "+N" o precio "150". En los niveles 1-7 su ranura no existe y la bandeja se centra (07 §7.4); desactivado (gris) cuando ya se usó en el nivel o cuando no queda vertido legal. |
| VFX / SFX | Capi llega caminando desde la orilla izquierda (02 §4.1; 0,6 s), coloca la cuia, las demás se reacomodan con easing (0,3 s); salpicadura corta al tocar el río de fondo. Sonido: pasos en arena + tintineo de vidrio + chasquido corto de capibara. Háptico medio. Capi sale de pantalla: nunca permanece fijo en el centro inferior (glosario §3.1). |
| Casos borde | (a) Solver: el estado pasa a T+1 cuias; Morpho y la verificación de rescate recalculan sobre el nuevo estado. (b) Variante aliviada "El río bajó": si el jugador ya tiene Cuia extra aplicada, la variante servida es la de **−1 color**, nunca la de +1 vacío, y la cuia comprada se conserva. (c) Nivel con 0 vacíos iniciales (diseño "limpio" de research/09 §Núcleo [V]): permitido; la Cuia extra es la única ayuda que crea vacío. (d) Cuia de rescate tras Cuia extra: permitida (segunda y última cuia añadida del nivel: 14 + 2 = 16 ranuras). (e) Pororoca: la Cuia extra vacía se conserva como vacío en la reconstrucción inversa. |

### 2.3 Morpho · Pista (`hint`)

| Campo | Especificación |
|---|---|
| Función exacta | Llama al solver (A* ponderado, presupuesto 40.000 nodos, <50 ms en móvil, research/02 §Lecturas 5 y Complemento 4 [E sobre V]) desde el estado actual y resalta **origen y destino del primer movimiento** de la solución encontrada durante 3 s o hasta que el jugador vierta. Un movimiento por uso; nunca autocompleta (research/03 §2 [V/E]). |
| Regla de uso | Solo con ≥1 vertido legal (en Aguas estancadas no hay nada que señalar: no aparece en el modal). Cooldown 5 s entre pistas [P]. Si no hay vertido que proponer, Morpho responde con honestidad en el **orden** de 02 T5 (D-019, 2026-10-13), con botones Deshacer / Volver a la orilla, y **en ningún caso se cobra** (research/01 Especificación 14 [V]): "Desde aquí no hay salida: la Marea o la orilla" cuando el solver demuestra que no hay solución; "No encuentro un camino desde aquí" cuando se agota el presupuesto (`hint_budget_nodes` o `hint_timeout_ms`, 08 §9) antes de decidir; en Aguas turbias y con la Tinaja, además, "No veo un vertido seguro…" (caso borde a). |
| Desbloqueo y tutorial | **Nivel 12** [P] (research/03 §Catálogo). Al cargar el nivel 12 TiTino presenta el botón con su retrato y `sloth_present` (D-016; antes, Capi): "¿Atascado? La morpho conoce el río" y se acreditan **3 Morphos** al stock. La primera Pista va sin dedo guía: la cola de TiTino sobre la ranura «Pista» hace de guía (pregunta 61 de GDD.md §6.3, decidida el 2026-10-19: (a); D-028). Que Morpho muestre origen y destino es una ayuda explícita compatible con D-002 (02 §3.1; P5 de `docs/dev/decisiones.md`, respondida: sí). Pista por inactividad (02 §3.4; research/01 Especificación 15): activa en **todos los niveles** desde el 3, sin límite superior de nivel; tras `hint_idle_delay_s` (12 s) sin toques, una cuia de origen de la ruta del solver pulsa una vez (escala 1,04, 600 ms), sin costo y nunca en posición perdida. No canibaliza a Morpho: el pulso solo señala el origen, no muestra el destino ni cobra; Morpho añade el destino. Claves únicas `hint_idle_from_level` = 3 y `hint_idle_delay_s` = 12 (08 §9; el interruptor "Pista por inactividad" de Ajustes, 02 §12, la apaga en el dispositivo); este capítulo no tiene claves propias de pista por inactividad. |
| Usos gratis | 3 en el tutorial; después Canasta (**niveles 30, 60, 80, 110, 130…**: ciclo fijo de 5 Canastas desde el nivel 20, capítulo 06 §4.4; contenido determinista y visible, dirección 15) y Bitácora (días 4, 12, 25). |
| Costo en semillas | **100** (dirección 9) ≈ 0,20 USD; 2,6 victorias de ingreso en nivel (1,6 del casual / 1,0 del medio sobre el ingreso efectivo, §9). |
| Anuncio recompensado | Ubicación `rv_hint`: 1 pista, tope **3/día**, cooldown **60 s** [P]. |
| Inventario y HUD | Ranura 3 de la bandeja cumbuca, a la derecha de Cuia extra (orden canónico 07 §7.4); etiqueta "Pista"; badge de stock o precio/video. |
| VFX / SFX | Una morpho azul despega del botón, vuela hasta la cuia de origen (0,4 s), se posa y luego salta a la de destino dejando una estela de destello azul estructural; ambas cuias pulsan con un anillo claro. Sonido: aleteo suave + campanilla. Sin háptico. |
| Casos borde | (a) **Aguas turbias**: el solver enumera las realidades posibles de las capas ocultas, **≤ 24** (hasta `lvl_murky_max_hidden_units` = 4 dosis ocultas por nivel, 02 T2 / 03 §8 / 08 §9; research/02 Complemento 4.5 [E]: 4 bloques = 24; el límite anterior de 6 dosis / 720 realidades queda descartado porque rompía el presupuesto del solver en cliente), con memo por estado canónico y presupuesto `hint_budget_nodes` (40.000) **por realidad**, y devuelve solo un movimiento seguro en todas. Las realidades ya descartadas por capas reveladas (Luciérnaga, vertidos, Pororoca) no se enumeran, así que R baja a lo largo del nivel. Si no existe movimiento seguro desde el estado actual (la garantía del generador cubre los estados de la ruta segura, no cualquier estado del jugador), Morpho responde en este **orden** (D-019, 2026-10-13; 02 T5; también con la Tinaja): si ninguna realidad tiene solución (también con R = 1), "Desde aquí no hay salida: la Marea o la orilla", con Deshacer y Volver a la orilla; si alguna la tiene pero ningún vertido es seguro en todas, "No veo un vertido seguro: la luciérnaga ilumina una cuia" ("… la tinaja" con tinaja), sin prometer nada y nunca con R = 1; en los dos casos **no cobra**: así nunca manda comprar una Luciérnaga inútil en una posición perdida. Si la enumeración agota `hint_timeout_ms` (150 ms, 03 RC) antes de terminar, Morpho responde "No encuentro un camino desde aquí" (03 §5) y **no cobra**; no existe "Pista parcial" por número de bloques. [P: la alternativa de devolver en el timeout el siguiente vertido seguro de `safe_path` exige que el capítulo 03 incluya `safe_path` en el pack publicado, cosa que hoy no hace (03 §10).] (b) Mecánicas: el nivel declara `hint_supported` en su JSON; si el solver del cliente aún no modela una mecánica (p. ej. Remolino en su primera versión), el botón se desactiva con el tooltip "La morpho no conoce este tramo todavía" y el nivel no puede etiquetarse Difícil (dependencia con 03). (c) Tras Cuia extra o Cuia de rescate la pista se calcula con la cuia añadida. (d) Si el jugador no ejecuta la pista y mueve otra cosa, la pista se apaga; no hay reembolso. |

### 2.4 Pororoca · Barajar (`shuffle`)

| Campo | Especificación |
|---|---|
| Función exacta | **Reconstrucción inversa** sobre las cuias no selladas (research/02 Complemento 4.4 [E sobre V]): las Cuias selladas quedan fijas; los bloques de las cuias restantes forman un estado resuelto parcial sobre el que se aplican k movimientos inversos legales con los mismos vacíos, k = max(12, 3 × colores restantes), con los filtros anti-trivialidad (no volcar todo en un vacío; rechazar si el resultado es igual al tablero actual o deja una cuia monocolor llena). Verificación: una llamada de A* acotado (<50 ms) exige **par nuevo ≤ par actual + 2**; si en 20 intentos no se cumple, se usa Permutar + verificar; si también falla, no se cobra y aparece "La ola no pudo remover el río"; la Pororoca no se desactiva y se puede volver a intentar, aunque la ola es determinista y el mismo tablero vuelve a fallar hasta que cambie (D-033, B2-P16; también en niveles con mecánicas, 03 §9). Conserva colores, número de vacíos y capacidades por cuia. |
| Regla de uso | Solo mientras quede ≥1 vertido legal (no está en el modal de bloqueo: a 300 semillas sería un rescate barato que canibaliza la Cuia de rescate; Magic Sort tampoco lo permite tras el bloqueo, research/09 §Comodines [NV]). **1 por nivel y 3 por día** [P] (dirección 9: "limitado"). Vacía el historial de Marea. Requiere hoja de confirmación de una línea: "La ola remueve el río sin tocar las cuias llenas. 300 semillas. ¿Seguir?" (regla general: confirmación para gastos ≥200 semillas [P]). |
| Desbloqueo y tutorial | **Nivel 30** [P] (research/03 §Catálogo), un nivel antes de Canoas y rescate, para que el jugador conozca la última herramienta de preparación antes de la primera herramienta de pago del bloqueo. Tutorial: tarjeta de una pantalla con la animación en bucle; no se regala uso. |
| Usos gratis | **1 por 7 Retos del río seguidos** (`live_daily_streak7_reward` = "shuffle", 08 §9; 06 §4.1; 05 §2.1), que entra en `shuffle_stock` (§1.1, §10.1); no aparece en Canasta ni Bitácora (en research/04 C1 "3 Pororoca" significa 3 Marea según la tabla de alias del glosario §5.3). Los topes de 1 por nivel y 3 por día rigen también para el stock: usar la Pororoca guardada cuenta igual que comprarla. |
| Costo en semillas | **300** (dirección 9: ~300) ≈ 0,60 USD; 7,9 victorias de ingreso en nivel (4,8 del casual / 3,0 del medio sobre el ingreso efectivo, §9). |
| Anuncio recompensado | No. |
| Inventario y HUD | Ranura 4 de la bandeja cumbuca, a la derecha de Morpho (orden canónico 07 §7.4), desde el nivel 30; etiqueta "Barajar"; badge de stock "+N" si hay Pororoca guardada (consume primero el stock), si no, precio visible. |
| VFX / SFX | Una ola grande cruza la pantalla de izquierda a derecha (1,2 s); las cuias no selladas se elevan, el líquido gira dentro de cada una y al pasar la ola los colores ya están en su nueva disposición; las selladas no se mueven y quedan en primer plano. Sonido: rumor grave creciente + rompiente; háptico fuerte. No saltable (es el feedback de una compra). |
| Casos borde | (a) **Aguas turbias**: la ola **revela todas las capas ocultas** del nivel (research/02 Complemento 4.5 [E]: la reconstrucción inversa en cliente no puede garantizar "≥1 vertido seguro" con capas ocultas, y revelarlas elimina el problema). A 300 semillas equivale a 2,5 Luciérnagas, así que no la canibaliza en niveles con 1 cuia turbia. (b) Hoja, Brote, Liana, Matapi y Barro: las cuias con la mecánica cerrada quedan **fijas** (se tratan como selladas; el Matapi deja fijo su contenido, propuesta de D-007 b.2 M10, regla 8); si quedan <2 colores movibles el botón se desactiva con el tooltip "Hay poca agua libre para la ola". (c) Remolino: las dos cuias del portal participan normalmente. (d) Lluvia y Piraña: la Pororoca se deshabilita en los niveles que las llevan, porque tienen estado temporal (D-007; 02 §9.2 y 03 §9) [P]. En la 1.1, las cuias bajo un Aguapé quedan fijas y la Cocha deshabilita la Pororoca (propuesta de D-007, b.2 M8 y M12). (e) Cuia extra: se conserva como vacío. (f) La Pororoca nunca cambia la etiqueta de dificultad ni la recompensa del nivel. (g) **Tinaja** (D-019): la ola revela todas sus capas; la tinaja y su cuia colgada quedan fijas con su contenido y solo se rebarajan las dosis libres (si su cuia ya bajó del jirau, entra en el barajado como una Cuia chica normal). Mientras la tinaja tenga dosis no existe el «estado resuelto parcial» de la reconstrucción inversa, así que caerá a menudo en Permutar + verificar o en «La ola no pudo remover el río»: se mide en el pipeline y, si falla a menudo, se deshabilita mientras la tinaja tenga dosis [P]. |

### 2.5 Luciérnaga · Revelar (`reveal`)

| Campo | Especificación |
|---|---|
| Función exacta | Revela **todas las unidades ocultas de una cuia turbia** elegida por el jugador: toca el botón (entra en modo selección, las cuias turbias pulsan), toca la cuia; tocar fuera cancela sin costo. Lo revelado permanece visible tras Marea y tras Volver a la orilla (research/03 C2.4 [E]; research/01 Especificación 8 [V]). |
| Regla de uso | Solo en niveles con modificador `murky`: los de Aguas turbias y, desde D-019, los de la Tinaja, que revela todas las capas ocultas de la tinaja (Aguas turbias: Difícil/Muy difícil desde el nivel 100 (`lvl_murky_from_level`), máx. `lvl_murky_max_cuias` = 3 cuias turbias y `lvl_murky_max_hidden_units` = 4 dosis ocultas por nivel, dirección 6; 02 T2 / 08 §9). No está en el modal de bloqueo (una unidad oculta nunca es la superior, así que revelar no crea vertidos). Sin tope por nivel: con máximo 3 cuias turbias el tope natural es 3. |
| Desbloqueo y tutorial | Debuta en el **nivel 100** (`lvl_murky_from_level`; hito Muy difícil del ciclo, 03 §9/§12) con **1 sola cuia turbia** y regala **1 Luciérnaga** al stock con la tarjeta: "Aguas turbias: algunas capas están ocultas. Una luciérnaga ilumina las de una cuia". Con D-007 (Q9) el estreno lleva la información oculta mínima: 2 dosis ocultas y ninguna otra mecánica en el nivel. En el cliente del paso 1 la tarjeta es **una sola**, combinada con la tarjeta previa del Muy difícil: el tutorial «El agua turbia esconde frutas. Debajo nunca se repite la fruta de encima.», con la línea T4 obligatoria (02 §11), la línea de la Luciérnaga y el regalo «+1 Luciérnaga», ratificado el 2026-10-19 (pregunta 21 de GDD.md §6.3; D-029). Los textos nombran fauna y fruta: pendientes del asesor cultural. |
| Usos gratis | 1 en el debut; Bitácora día 29; desde 1.1, Aguas turbias · Bonus del Reto del río puede dar 1 [P] (la tarjeta está apagada en 1.0: `ui_murky_bonus_enabled` = false, 08 §9). |
| Costo en semillas | **120** [P dentro del rango 100-150 de la dirección 9] ≈ 0,24 USD; 3,2 victorias de ingreso en nivel (1,9 del casual / 1,2 del medio sobre el ingreso efectivo, §9). |
| Anuncio recompensado | Ubicación `rv_reveal`: 1 revelado, tope **3/día**, cooldown **60 s** [P]. Es el diferenciador frente a todo el género, donde no existe comodín de revelar (research/03 §2 [V]). El vídeo se pide con un toque desde la bandeja, en PLAYING: es la excepción explícita de 02 §14 para los comodines de la bandeja (también `rv_undo_refill` y `rv_hint`). Sin stock, ofrece primero el vídeo si lo hay y, si no, las semillas, como Marea (pregunta 23 de GDD.md §6.3, decidida el 2026-10-19: (a); D-028). Sin stock, saldo ni vídeo, **no saca aviso**: responde la burbuja «Te faltan {n}» de su ranura, que ya se ve, con un latido y el color del precio sin saldo (con Reducir movimiento, solo el color), sin texto nuevo (pregunta 48 de GDD.md §6.3, decidida el 2026-10-14: (b); D-022). El cliente del paso 1 aún saca el aviso «Te faltan {n} semillas» por la cola de avisos: tarea de código pendiente. |
| Inventario y HUD | Ranura 5 de la bandeja cumbuca, a la derecha de Pororoca, solo en niveles con ocultos: Aguas turbias y, desde D-019, la Tinaja (orden canónico 07 §7.4: con 5 ranuras los botones pasan de 64 a 56 dp; ningún comodín flota fuera de la bandeja); etiqueta "Revelar"; badge de stock o precio/video. Se oculta si ya no quedan unidades ocultas (p. ej. tras Pororoca). En el cliente del paso 1, sin Morpho ni Pororoca todavía, la bandeja tiene como mucho 3 ranuras de 64 dp (Marea, Cuia extra y Revelar) y Revelar va en la 3.ª; vuelve a la 5.ª cuando lleguen los otros dos (02 §5; pregunta 29 de GDD.md §6.3, ratificada el 2026-10-13: D-020). Su modo de selección es una ayuda explícita compatible con D-002: solo laten las cuias turbias (o la tinaja, D-019) mientras el jugador usa la Luciérnaga (02 §3.1). |
| VFX / SFX | Una luciérnaga sale del botón, se posa en el borde de la cuia elegida y su luz baja por el vidrio: el agua parda se aclara de arriba abajo en 0,6 s (600 ms, `MurkyMarks.RevealFireflyMs`; 02 §4.1) y cada "?" gira y muestra el icono de fruta. Sonido: campanilla cálida + grillos nocturnos cortos; háptico ligero. Hoy el cliente no tiene audio ni háptico. |
| Casos borde | (a) Solver: tras revelar, el conjunto de movimientos seguros crece y Morpho vuelve a ser útil. (b) Aguas turbias · Bonus (premio doble; llega en 1.1, `ui_murky_bonus_enabled` = false en 1.0): Luciérnaga permitida sin penalizar el bonus [P]; pregunta abierta 4. (c) Si la cuia turbia es además Hoja/Brote cerrada, revelar muestra las capas pero no la abre. (d) Si el jugador paga con semillas y la app se cierra durante la animación, la revelación se guarda antes de descontar (orden: aplicar → guardar → cobrar), de modo que nunca se cobra sin entregar. |

### 2.6 Salto del Boto · Saltar nivel (`skip`)

| Campo | Especificación |
|---|---|
| Función exacta | Marca el nivel actual como superado **sin recompensa** (0 semillas, sin avance ni rotura de la Crecida, cuenta para la Canasta de cada 10 niveles) y carga el siguiente. El nivel saltado queda marcado en el Mapa del río con el icono de Rosa; en 1.0 no se puede volver a jugar (campaña lineal). |
| Regla de uso | Tres condiciones simultáneas (dirección 9): **≥5 fallos en el mismo nivel** (fallos duros y blandos, §5.2), **≥10 niveles desde el último salto** y nivel ≥31 [P: antes del 31 la variante aliviada del tercer fallo y el reinicio gratuito bastan, y se mantiene limpio el tramo sin sumideros]. El nivel debe ser `skippable=true`: los niveles que introducen una mecánica no se saltan (estrenos según la lista única de 02 §9.1 y `mech_debut_level`: 14 Cuia chica · 27 Cuia alta · 43 Hoja · 63 Matapi · 83 Liana con fruta · 100 Aguas turbias · 103 Remolino · 123 Barro · 143 Lluvia · 163 Brote · 183 Piraña · 323 Tinaja (M13, D-019: en la 1.0 si cumple su condición; si no, el mismo nivel en la 1.1); la Liana de cuenta (con número) es una variante, en el 243; D-007). En los packs, `skippable` = false solo en el nivel de `intro_mechanic` (QA-DEBUT de `tools/level-qa`). Confirmación de una línea. |
| Desbloqueo y tutorial | Sin tutorial propio: la primera vez que se cumplen las condiciones, el modal Aguas estancadas añade la opción con la frase "Rosa te lleva río arriba, al siguiente nivel" (D-013, Q13-A: antes, «río abajo») y el icono de Rosa aparece también en el menú de pausa. |
| Usos gratis | Ninguno. |
| Costo en semillas | **300** (dirección 9) ≈ 0,60 USD. |
| Anuncio recompensado | No (la dirección 9 fija solo semillas; research/03 §Catálogo proponía 1/día por video, descartado para que el salto siga siendo raro). |
| Inventario y HUD | No vive en la barra: aparece en el modal Aguas estancadas (tercera opción, §6.6 wireframe D), en el menú de pausa y como banner en la pantalla previa del nivel cuando es elegible ("Rosa puede llevarte río arriba · 300"). |
| VFX / SFX | Rosa emerge en el río de fondo con una exhalación, las cuias del jugador flotan corriente abajo y la cámara acompaña hasta el siguiente tablero (1,5 s). Sonido: exhalación de delfín + agua; háptico medio. Dato del río opcional al llegar: "El boto es el mayor delfín de río y está En Peligro" (research/07 §5 [V]). |
| Casos borde | (a) No consume canoa ni rompe la Crecida. (b) Saltar un nivel Muy difícil (hito) está permitido si es `skippable`; 03 decide qué hitos se protegen. (c) Si el nivel saltado tenía Cuia extra aplicada, se pierde sin reembolso (aviso en la confirmación). (d) El hueco desde el último salto se mide con `skipped_levels` (el mayor por debajo del nivel), que se guarda en el perfil y se sincroniza por unión (§10); `since_last_skip` se escribe a 0 y no se lee (D-033, B3-Q-S1). (e) El Reto del río y los eventos no admiten Salto del Boto. |

### 2.7 Volver a la orilla · Reiniciar (`restart`)

| Campo | Especificación |
|---|---|
| Función exacta | Restaura el estado inicial del nivel con la **misma semilla**: mismo tablero, misma asignación de capas ocultas (dirección 7; research/09 §Mejorar "layout cambia al reintentar" [R]). Vacía el historial; restaura los 3 Marea gratis; **conserva** la Cuia extra aplicada y las capas reveladas; no reembolsa stock consumido ni semillas. |
| Regla de uso | Gratis, ilimitado, fuera del bloqueo (botón en la cabecera y en el menú de pausa). En el modal Aguas estancadas solo es el pie en los niveles 1-30, con el texto "Volver a la orilla (gratis)". **Desde el 31 el pie del modal es «Rendirse» [P]** (D-008, 2026-10-08), que lleva a ¿Seguro? y a Nivel fallido (§6.2). El coste en canoas del bloqueo se anuncia en una línea de estado aparte ("Travesía perdida: −1 canoa", §6.5), nunca atribuido al reinicio, para no romper la promesa del tooltip del primer modal del tutorial (02 §13, nivel 6: "Reiniciar es gratis, siempre, mismo tablero. Desde el nivel 31, quedarte sin vertidos cuesta una canoa"). Nunca va seguido de intersticial (dirección 13). Si el tablero lleva ≥8 vertidos desde el último reinicio [P] pide confirmación de una línea: "¿Volver a la orilla? Perderás los vertidos de este intento" con atajo a Deshacer si queda cupo; con <8 vertidos reinicia al instante. En ese texto «intento» es coloquial (los vertidos desde el último reinicio), no el `attempt` de 02 T6. **Intento** (D-019, Q-Ti6-A; 02 T6): el que cuenta `attempt` = `fails_level` + 1 (§6.1); un reinicio con <8 vertidos no es fallo y no abre un intento nuevo, y uno con ≥8 (fallo blando) sí lo abre. **Lo revelado sigue revelado** en los dos casos y tras un fallo duro, hasta que se vuelve a cargar el nivel o se sirve la variante aliviada, que es otro tablero (02 T6; con la Tinaja se puede espiar sacando y reiniciando: aceptado y medido en el soft launch). **Aviso de TiTino** (D-016, T5-B; niveles 1-30): tras la intro corta del reinicio, un aviso con su retrato de 40 dp y una frase (`sloth_line_restart_*`; en 25-30, si el fallo duro rompió la Crecida, `sloth_line_streak_04`), solo en el primer reinicio de cada nivel y solo si el nivel no tiene instrucción del tutorial; sustituye a `capi_shrug`, sale por la cola de avisos y dura por lectura (D-011; 02 §4.2), sin capturar toques. |
| Fallo blando y fallo duro | **Fallo duro** (§5.2; D-008): en 1-30, salir de Aguas estancadas con Volver a la orilla desde el modal; desde el 31, confirmar la rendición (Rendirse → ¿Seguro? → Nivel fallido) o salir del nivel con el menú de pausa. Cerrar la app nunca es fallo. Desde el nivel 31 cuesta 1 Canoa y rompe la Crecida salvo Hoja escudo. **Fallo blando** [P]: reinicio voluntario con ≥8 vertidos; no cuesta Canoa ni rompe la Crecida (la racha solo cuenta victorias y fallos duros), pero **sí cuenta** para la variante aliviada y para Salto del Boto. Razón: el jugador que ve venir el bloqueo y reinicia a tiempo no debe pagar una canoa (reiniciar es gratis, dirección 2), pero sí debe recibir el alivio del tercer fallo. |
| Variante aliviada | Desde el nivel 31, al tercer fallo consecutivo (duro o blando) el siguiente intento carga la variante `relief[0]` pregenerada en el JSON del nivel (−1 color o +1 vacío, elegida por el generador) con el banner "El río bajó: esta vez hay un color menos" / "…una cuia más" durante 2,0 s (`anim_river_lowered_banner_ms` = 2000, 08 §9) antes de la animación Llenar y teñir. La variante se mantiene hasta ganar el nivel; el contador de fallos se reinicia al ganar. Segundo escalón (ambos alivios) al sexto fallo: parámetro `relief_second_step_fails`, apagado por defecto [P]. Antes del 31 el nivel se publica con `relief` = `[]` (obligatoria con `minItems` 1 solo desde el 31: 03 §13, 08 §4.1, `schemas/af-level-1.schema.json`): el reintento sirve siempre el mismo tablero, sin "El río bajó", y `fails_level` se cuenta igual. |
| VFX / SFX | Las cuias se vacían en el río y la animación Llenar y teñir corre a 0,8 s (saltable con un toque, dirección 18). En el mundo 11 la animación la ejecuta el hoatzin (glosario §3.3). Desde D-010, la intro del reintento va en modo dosel, sin tropa de miquitos (02 §4.2); en el mundo 11 el hoatzin solo hace el vaciado, y la intro del reintento sigue en modo dosel, como en todos los mundos (Q17 de D-010, decidida el 2026-10-19 en D-029; sin código hasta el mundo 11). Sonido: agua que se vacía + maracá. |
| Casos borde | (a) Reiniciar durante una animación encolada: se cancela la cola y se reinicia tras el vertido en curso. (b) «Reintentar» con 0 canoas desde Nivel fallido (D-008): abre la pantalla Sin canoas (§7.3); el nivel queda "en curso" y, en cuanto hay una canoa, se vuelve a Nivel fallido con «Reintentar» [P]. (c) Cerrar la app desde un estado atascado (Aguas estancadas, ¿Seguro? o Nivel fallido abiertos): el estado se guarda tal cual, no se cobra canoa; al volver, la misma pantalla reaparece (§5.2). Salir del nivel con el menú de pausa es fallo duro desde el 31 y sale sin coste en 1-30 (D-008). (d) Iniciar o reiniciar un nivel con 0 canoas desde cualquier punto (Jugar, Siguiente, Reintentar en Nivel fallido, cabecera): abre Sin canoas (§7.1 "Qué bloquea"). |

### 2.8 Cosecha doble · x2 (`rv_double_harvest`)

No es un comodín de tablero, pero es la ubicación recompensada que fija el ritmo de ingresos: desde el **nivel 11** (dirección 13), en la pantalla de victoria, duplica las semillas del nivel (20→40, 60→120, 80→160). Tope **5/día**, cooldown **60 s** [P]. Durante "río crecido" (Crecida hito 10; activo hasta el siguiente fallo duro no escudado, §8) y en Temporada de lluvias se aplica sin video. 05 §6 detalla la ubicación (`ads_rv_double_harvest_from_level`, 08 §9); aquí solo cuenta para los topes globales de la tabla §3.

## 3. Tabla resumen

| Comodín | Etiqueta HUD | ID | Función | Desbloqueo | Gratis | Semillas | Video (tope/día · cooldown) | Usable en bloqueo | Persistencia | Victorias para pagarlo (ingreso efectivo 63 casual / 98 medio por victoria, 05 §3.2; §9) |
|---|---|---|---|---|---|---|---|---|---|---|
| **Marea** | Deshacer | `undo` | Revierte el último vertido | 1 (tutorial 3) | 3/nivel (ilimitado en 1-10) | 30 = +3 | `rv_undo_refill` 4 · 60 s | **Sí** | Cupo por nivel + stock | 0,5 / 0,3 |
| **Cuia extra** | +1 cuia | `extra_cuia` | +1 cuia vacía cap. 4 | 8 (1 gratis) | Canasta niveles 40 y 90 de cada 100 (ciclo fijo), Bitácora 6/14/23 | 150 | No | No (pasa a ser Cuia de rescate) | Stock; aplicada persiste al reiniciar | 2,4 / 1,5 |
| **Morpho** | Pista | `hint` | Siguiente movimiento del solver | 12 (3 gratis) | Canasta niveles 30, 60, 80… (ciclo fijo), Bitácora 4/12/25 | 100 | `rv_hint` 3 · 60 s | No | Stock | 1,6 / 1,0 |
| **Pororoca** | Barajar | `shuffle` | Reconstrucción inversa, par ≤ actual+2 | 30 | 1 por 7 Retos del río seguidos | 300 (1/nivel, 3/día, también con stock) | No | No | Stock | 4,8 / 3,0 |
| **Luciérnaga** | Revelar | `reveal` | Revela una cuia turbia | 100 (1 gratis) | Bitácora 29 | 120 | `rv_reveal` 3 · 60 s | No | Stock; revelado persiste | 1,9 / 1,2 |
| **Salto del Boto** | Saltar nivel | `skip` | Pasa al siguiente nivel sin premio | 31 + 5 fallos + 10 niveles | — | 300 | No | Sí (3.ª opción) | — | 4,8 / 3,0 |
| **Volver a la orilla** | Reiniciar | `restart` | Mismo tablero | 1 | Ilimitado | 0 | — | En 1-30 (enlace al pie); desde el 31 el pie es «Rendirse» (D-008) | — | 0 |
| **Cuia de rescate** | Continuar: +1 cuia | `continue_cuia` | +1 cuia tras el bloqueo | 31 | — | 600 fijas (`rescue_price_bands` = [[31,600]]; 300 el 1.º del día; **máx. 1/nivel**, `rescue_max_per_level` = 1) | No | Es el bloqueo (Aguas estancadas y, otra vez, ¿Seguro?; D-008) | Aplicada persiste | 9,6 / 6,1 (600) · 4,8 / 3,0 (300) |
| **Cosecha doble** | x2 | `rv_double_harvest` | Duplica semillas del nivel | 11 | — | — | 5 · 60 s | — | — | — |
| **Canoa extra** | +1 canoa | `rv_extra_canoe` | +1 Canoa en Sin canoas | 31 | — | — | 3 · 90 s | — | — | — |

Las posiciones de la bandeja siguen el orden canónico de 07 §7.4 (ver §2). Topes y cooldowns por ubicación: `ads_rv_cap_<placement>` / `ads_rv_cooldown_s_<placement>` (08 §9). Tope global de recompensados: **15/día** [P] (`ads_rv_global_cap_day`) como red de seguridad sobre los topes por ubicación (Pangle recomienda 5-8 por 20-50 min, research/03 §5 [V]; con 4 sesiones/día de ~6 min el jugador medio no llega al tope global).

## 4. Calendario de introducción

| Nivel | Qué aparece | Cómo se enseña | Por qué ahí |
|---|---|---|---|
| 1 | Marea visible (sin tutorial) | — | Posición estándar del género (research/01 §3 [V]) |
| 3 | Tutorial Marea | Vertido tentador → tooltip de Capi | research/02 §Curva [E] |
| 8 | Cuia extra (1 gratis) | Capi la trae; sin precio; remite a la Canasta, no al Mercado | research/03 y research/05 [E]; boosters en nivel hacia el 7-9 (research/05 §4 [E]) |
| 10 | Mercado accesible | Entrada desde el inicio (06 §4.7); el Mapa del río no lleva ofertas ni entrada al Mercado (D-013, Q10-A); sin tutorial propio | 05 §2.1: ninguna oferta antes del 10; primera vez que un comodín muestra precio fuera del nivel |
| 11 | Cosecha doble | Botón x2 en victoria | dirección 13 |
| 12 | Morpho (3 gratis) | Tarjeta de Capi | research/03 [E] |
| 14 · 27 | Cuia chica (14, cap 2); Cuia alta (27, cap 5; cap 6 solo en hitos desde el 141) | Tutorial de una pantalla al debut (02 §8) | dirección 4; 02 §8 |
| 25 | **Crecida + Hoja escudo** | Tarjeta de la Crecida al cargar el 25; la Hoja escudo aparece como estado de la barra y en el toast del primer fallo cubierto | 06 §3-4.3; seis niveles antes de las Canoas, para que el jugador conozca la racha y su escudo antes de que un fallo duro cueste además una canoa |
| 30 | Pororoca | Tarjeta con animación | research/03 [E]; último comodín de preparación antes del rescate |
| 31 | **Canoas + Cuia de rescate** | Tarjeta "Canoas" al cargar el 31; el primer bloqueo real muestra el modal completo (la línea de Hoja escudo ya es conocida desde el 25) y, si el jugador se rinde, ¿Seguro? y Nivel fallido (D-008) | dirección 10 y 11; D-008 |
| 31+ | Salto del Boto | Aparece al cumplir condiciones | dirección 9 |
| 40 | Primer Difícil, con la etiqueta antes de jugar | Chip y cinta «×3» en Jugar y en Siguiente, y tarjeta previa de 1 toque (02 §10; D-009) | 03 §11/§12; D-009 |
| 43-323 | Debuts de mecánicas (niveles no `skippable`; lista única de 02 §9.1, D-007): 43 Hoja, 63 Matapi, 83 Liana con fruta, 103 Remolino, 123 Barro, 143 Lluvia, 163 Brote, 183 Piraña y 323 Tinaja (M13, D-019: en la 1.0 si cumple su condición; si no, el mismo nivel en la 1.1) | Tutorial de una pantalla al debut (02 §9.1), que hace de tarjeta previa; Marea, Morpho y Pororoca ya conocen cada mecánica (§2) | 02 §9.1 y `mech_debut_level`, fuente única de debuts |
| 100 | Luciérnaga (1 gratis) | Debut de Aguas turbias en el hito Muy difícil del 100, con 1 cuia turbia de 2 dosis ocultas y sin otra mecánica (D-007, Q9); una sola tarjeta combinada (§2.5) | dirección 6; 03 §9/§12 (`lvl_murky_from_level` = 100) |

## 5. Detección del bloqueo

### 5.1 Vertidos legales no fútiles

Una sola función `futil(A, B)` vive en `AF.Core` (`AF.Core.Rules.BoardRules`) y la usan **cliente, bot casual y solver** sin reimplementarla: 02 §1.4 es la definición canónica, con la excepción de Aguas turbias, y este capítulo la copia. Desde D-007 (Q13, con la respuesta Q-B del dueño) es la del anexo B de la propuesta de mecánicas. Tres criterios distintos de "fútil" (cliente, generador, solver) harían que el cliente declarase Aguas estancadas en un estado que el solver considera con progreso, o al revés, y dejarían falsa la garantía R16 de 02.

```
function futil(A, B):                                        # AF.Core · única implementación (02 §1.4: anexo B de D-007 con Q-B)
    if B.changesOnReceive: return false                      # Hoja puesta, Brote cerrado, Matapi, Remolino, Lluvia con gotas pendientes
    if A.exemptAsSource: return false                        # Q-B: Remolino o Lluvia con gotas pendientes; anexo B: la Cocha cerrada (1.1)
    if A.hasHiddenUnits: return false                        # Aguas turbias: verter revela información, nunca es fútil
    # solo puede ser fútil "monocolor visible → vacía"; todo lo demás progresa o es ilegal
    if not (A.isMonochromeVisible and not A.isSealed and B.isEmpty): return false
    c = topColor(A)
    casa_correcta_B   = (B.cap == U[c])                      # B puede sellar c (su capacidad = dosis totales del color)
    casa_incorrecta_A = (A.cap != U[c])                      # A no puede sellar c (p. ej. Cuia chica de 2 con 2 dosis, U_c = 4)
    if casa_incorrecta_A and casa_correcta_B: return false   # llevar la fruta a la cuia donde puede sellar sí progresa
    return true                                              # solo recoloca el problema (research/01 regla 13); es legal, pero no avanza los contadores (Q18 B)

function legalNonFutileMoves(state):
    moves = []
    for A in state.cuias:
        for B in state.cuias:
            if legal(A, B) and not futil(A, B):               # `legal` de 02 §1.2 (filtro de origen de la Tinaja, su cuia como destino, Remolino por el fondo de la pareja, Barro, Hoja, Matapi, Liana…)
                moves.append((A, B))                          # misma función que bot y solver
    # Lluvia: la gota no es un movimiento del jugador y no cuenta
    return moves

function afterPourAnimation(state):                          # solo en la partida del cliente (LevelSession)
    if state.isSolved(): return WIN
    if legalNonFutileMoves(state).isEmpty() or bucle_sin_salida(state):   # 02 §1.4 y R13: `aguas_estancadas`; el bucle, con D-014 (cierre con todos los vertidos legales: pregunta 46, (a))
        wait(400 ms)                                          # el jugador ve el tablero final
        openStuckModal(buildContext(state))
```

Precisiones:

- **Bucle sin salida** (D-014, 2026-10-13; 02 §1.4 y R13): en la partida del cliente, Aguas estancadas sale también cuando el cierre de estados alcanzables con **todos los vertidos legales, fútiles incluidos**, se agota con ≤ 64 estados [P] y ninguno es victoria (`bucle_sin_salida`, 02 §1.4); si pasa de 64, no se declara. Es la única condición que el cliente añade a `legalNonFutileMoves`: el solver, el bot casual, el generador y `tools/level-qa` siguen solo con `legalNonFutileMoves` (y `futil` sigue siendo una sola función), y los packs no cambian. El cierre recorre todos los vertidos legales para cumplir R14 con capacidades mixtas (pregunta 46 de `docs/GDD.md` §6.3, decidida el 2026-10-13: (a); D-020); está integrado (`DeadEndProbe`, T51) y el nivel 66 es el ejemplo y la prueba de regresión (02 R13).
- **Tinaja** (D-019): la copia de `legal` del cliente, el bot y el solver lleva el filtro de origen de la tinaja (solo vierte en su cuia, antes de cualquier filtro del destino) y su cuia como destino exclusivo (recibe de su tinaja cualquier fruta mientras cuelga); `futil` exime como origen a la tinaja y a su cuia colgada (02 §1.4), o declararía fútil la última dosis sacada. Los tests de regresión (1-4) se repiten con una tinaja (núcleo, T55).
- **Aguas turbias**: una cuia con racha superior monocolor pero unidades ocultas debajo **no** es fútil al verterla en un vacío, porque revela información. Sin esta excepción el detector declararía bloqueo en tableros resolubles.
- **Casa correcta** (02 §1.4, R16): mover una cuia monocolor a una vacía **sí** progresa cuando A no puede sellar ese color y B sí (`cap_A ≠ U_c` y `cap_B = U_c`). Es el caso de la Cuia chica: 2 dosis de un color en una Cuia chica de 2 con una cuia estándar vacía como único destino **no es Aguas estancadas**.
- **Destinos que cambian al recibir** (Hoja puesta, Brote cerrado, Matapi, Remolino y Lluvia con gotas pendientes) nunca son fútiles, y por Q-B tampoco lo es el origen Remolino o Lluvia con gotas pendientes: el estado canónico cambia (02 §1.4). Eximir a cualquier origen con mecánica haría que el ir y venir monocolor contara siempre como útil y el modal no saliera nunca. Un vertido fútil es legal, pero **no avanza los contadores** de Lluvia ni de Piraña (Q18 B de D-007).
- El kill-switch `futile_rule_enabled = false` (02 RC) devuelve el criterio clásico "sin vertidos legales" a todos los consumidores a la vez, porque la función es una sola.
- **Deduplicación de vacíos** (solo en solver y bot, nunca en el cliente): dos destinos vacíos son intercambiables únicamente si comparten capacidad y atributo de mecánica, `(cap, attr)`. Podar "un solo destino vacío" sin esa condición volvería a ocultar la rama casa correcta (dependencia con 03 §5 MOVIMIENTOS).
- **Tests de regresión obligatorios** (`AF.Core.Tests`): (1) "Cuia chica monocolor → cuia estándar vacía no es bloqueo": Cuia chica de capacidad 2 con 2 dosis de c (U_c = 4), una cuia estándar vacía, resto sin destino legal → `legalNonFutileMoves ≠ ∅` y `atascado = false`; (2) su inverso, "cuia estándar con 2 dosis de c → otra cuia estándar vacía es fútil" (`cap_A = cap_B = U_c`); (3) "cuia monocolor visible con dosis ocultas → vacía no es fútil"; (4) "monocolor → vacía bajo Lluvia con gotas pendientes, o conectada a Remolino, no es fútil" (anexo B de la propuesta de D-007). Los cuatro corren también contra el solver de 03 con el mismo estado JSON. El núcleo añade el caso de la Hoja del anexo B («verter el resto de una fruta en su Hoja vacía no es fútil»), los contraejemplos de Q-B con Remolino y con Lluvia y las propiedades P1-P3 de 02 R16 con cada mecánica (`FutilePropertyTests`; CHANGELOG del núcleo).

### 5.2 Qué cuenta como fallo

Esta tabla es la **definición canónica de fallo** para todo el GDD: 02 §2 ("Definición de fallo") y §7, 03 §13 y 06 §4.3 remiten aquí y no redefinen el término. De ella dependen canoas, Crecida, "El río bajó" y Salto del Boto, así que el jugador debe poder predecirla con una sola regla: **desde el 31, solo cuesta canoa rendirse tras quedarse sin vertidos o salir del nivel con el menú (D-008); reiniciar por voluntad propia nunca cuesta canoa ni rompe la Crecida, y cerrar la app nunca es fallo.**

*Actualizada el 2026-10-08 por D-008: la fila «Salir al Mapa del río o cerrar la app con el modal abierto» se sustituye por las dos filas de salida y la de cierre de la app. Ampliada el 2026-10-14 (D1) con la fila del Mapa del río desde la pausa, que D-013 (punto 7) no deja como salida del nivel.*

| Evento | ¿Canoa? (≥31) | ¿Rompe Crecida? | ¿Cuenta para "El río bajó" (3) y Salto (5)? |
|---|---|---|---|
| **Fallo duro** (D-008): en 1-30, Volver a la orilla desde Aguas estancadas (STUCK_MODAL); desde el 31, confirmar la rendición («Rendirse» → ¿Seguro? → X o «Rendirse»), que lleva a Nivel fallido | Sí, −1 (salvo Canoas infinitas) | Sí, salvo Hoja escudo disponible; al romperla termina también el río crecido (§8) | Sí (`fails_level` +1) |
| **Salir del nivel con el menú**, desde el 31 (D-008): pausa → «Salir» → «¿Salir del nivel? Perderás 1 canoa» → Nivel fallido → inicio. Cuesta la canoa aunque no haya vertidos (P16, decidida el 2026-10-19 en D-029) | Sí, −1 (salvo Canoas infinitas) | Sí, salvo Hoja escudo disponible | Sí (`fails_level` +1) |
| Salir del nivel con el menú en 1-30 | No | No | No |
| **Abrir el Mapa del río desde la pausa** (D-013, punto 7): se abre encima del nivel en pausa y «Jugar» pasa a «Volver al nivel»; no es salir del nivel | No (no gasta canoa) | No | No (no cuenta como fallo) |
| **Fallo blando** [P]: Volver a la orilla voluntario con ≥ `restart_confirm_min_pours` (8) vertidos | No | No | Sí (`fails_level` +1) |
| Reinicio voluntario con <8 vertidos | No | No | No |
| Marea desde el modal | No | No | No |
| Cuia de rescate (en Aguas estancadas o en ¿Seguro?) | No | No (la conserva) | No |
| Cerrar la app, con o sin Aguas estancadas, ¿Seguro? o Nivel fallido abiertos | No (cada vertido se entrega al escritor en segundo plano y el perfil se escribe en síncrono al pasar a segundo plano y al cerrar la app, 08 §11; al volver, la misma pantalla reaparece; 02 §14) | No | No |
| Crash de la app o pérdida de red | No | No | No |
| Salto del Boto | No (no gasta canoa) | No (no la rompe ni la avanza) | Reinicia `fails_level` a 0 y añade el nivel a `skipped_levels` (D-033, B3-Q-S1) |

Lectura para 02 y 03: cerrar la app nunca es fallo. En 1-30, solo lo es la salida del modal con Volver a la orilla. Desde el 31, lo son la rendición confirmada y la salida del nivel con el menú (D-008); abrir el Mapa del río desde la pausa no es salir del nivel ni fallo (D-013). El umbral del fallo blando es `restart_confirm_min_pours` (8), no 5. Salto del Boto nunca cuenta como fallo para la Crecida.

**Con `biz_feature_lives` apagado (D-038, 2026-10-21; 08 §9):** desde el 31 la tabla se lee como en 1-30: salir del nivel con el menú es gratis y no es fallo (no suma a `fails_level`), y el fallo duro es la salida del modal con Volver a la orilla (rama C, sin canoa). No hay «Rendirse», ¿Seguro? ni Nivel fallido.

## 6. Aguas estancadas: árbol de decisión completo

**Embudo de D-008 (decisión del dueño del 2026-10-08).** Desde el 31 el bloqueo sigue la estructura de Magic Sort en cuatro pasos, uno tras otro:
1. **Aguas estancadas** (este modal): Marea primero, «Continuar: +1 cuia» y, al pie, «Rendirse» [P].
2. **¿Seguro?** [P]: «Perderás 1 canoa» y otra vez «Continuar: +1 cuia», con el mismo precio y las mismas condiciones.
3. **Nivel fallido** [P]: el premio que no se cobra, «Reintentar» [P] y X → inicio.
4. **Sin canoas** (§7.3), si se pulsa «Reintentar» sin canoas.

En 1-30 solo existe el paso 1, con «Volver a la orilla (gratis)» al pie (rama C). Las cifras no cambian (08 §9). Ninguna de estas pantallas lleva anuncio (05 §10).

**Con `biz_feature_lives` apagado (D-038):** desde el 31 tampoco hay canoas, así que el embudo es el de 1-30: solo el paso 1, sin línea de canoa y con «Volver a la orilla (gratis)» al pie (rama C) en lugar de «Rendirse». El rescate (Cuia y Balsa de rescate) se sigue ofreciendo: lo gobierna `biz_feature_rescue`, y el resto de ramas desde el 31 (A, A', B, D, E, F) se arman igual. **La Balsa de rescate sin Canoas infinitas** (D-038, añadido del 2026-10-21): su contenido queda en Cuia de rescate + 1.000 semillas + 1 Cuia extra, y su línea en «semillas y una cuia» (`stuck_balsa_499_content_no_canoes` [P], §6.5); **el precio no cambia**. Con el interruptor en su default (true), nada cambia.

### 6.1 Entradas del contexto

```
ctx = {
  level, difficulty, murky,
  attempt      = fails_level + 1,                 # «intento» de 02 T6 (D-019): un reinicio con <8 vertidos no abre uno nuevo
  pours        = vertidos de este intento,
  saldo        = semillas,
  undo_free, undo_stock, rewarded_ready[undo],
  canoes, infinite_canoes_active,
  streak, shield_available,            # Crecida y Hoja escudo
  rescues_today, rescues_level,
  af_rescue_shown_today, af_rescue_declined_days,   # Balsa de rescate: contadores de estado, no eventos de analítica (§11)
  geo_tier ∈ {T1, T3},                 # T1: US/CA/Europa occ./AU/JP/KR + NZ/SG/HK/TW/IL/Golfo (AE, SA, QA, KW, BH, OM) [P] (D-057, decisiones de la revisión, punto 4) · T3: LatAm salvo CL/UY y el resto (research/04 §Segmentación)
  no_ads,                              # compró Sin anuncios (conserva recompensados)
  last_skip     = max(l in skipped_levels if l < level) or none,   # D-033, B3-Q-S1: el hueco sale de skipped_levels, no de since_last_skip
  skip_gap      = level - last_skip if last_skip else level,       # Q-S2: tras saltar el 40, el 50 ya puede
  skip_eligible = fails_level ≥ 5 and skip_gap ≥ 10 and level.skippable and level ≥ 31,
  rescue_solvable                      # A* acotado sobre state + 1 cuia (§6.4); en Aguas turbias, sobre el estado REAL (capas ocultas incluidas)
}
```

### 6.2 Algoritmo

```
function buildStuckModal(ctx):
    opts = []
    # Regla 0 (todas las ramas): Deshacer primero si hay cupo o stock
    if ctx.undo_free + ctx.undo_stock > 0:
        opts.add(UNDO(label="Deshacer · Marea", badge=ctx.undo_free + ctx.undo_stock))
    else:
        opts.add(UNDO_REFILL(video if ctx.rewarded_ready and cap_ok("rv_undo_refill") else none,
                             seeds=30))

    if ctx.level < 31:
        return Modal(title="No hay más vertidos", opts, footer=RESTART(free=true))   # wireframe C

    base  = band(rc.rescue_price_bands, ctx.level)   # clave única [[31,600]] → 600 fijo en todo tramo y geo (dirección 10; 05 §3.3); sin rescue_base_price
    price = base
    if ctx.rescues_today == 0 and ctx.rescues_level == 0:
        price = round(base * rc.rescue_first_day_mult)   # 0,5 → 300 (primer rescate del día)
    if ctx.rescues_level ≥ rc.rescue_max_per_level (1) or not ctx.rescue_solvable:   # 1/nivel: 14 + extra + rescate ≤ 16 ranuras (02 §7)
        rescue = none
    elif ctx.saldo ≥ price:
        rescue = RESCUE(price, discount = price < base)                          # wireframe A
    else:
        rescue = RESCUE_DISABLED(price, missing = price - ctx.saldo)             # wireframe B
        upsell = []
        balsa_ok = ctx.af_rescue_shown_today < rc.af_rescue_daily_cap (1) and ctx.af_rescue_declined_days < rc.af_rescue_hide_after_declines (2)
        if ctx.geo_tier == T3:
            upsell = [PUÑADO_199] + ([BALSA_499] if balsa_ok else [])
        else:
            upsell = ([BALSA_499] if balsa_ok else []) + [PUÑADO_199]
    opts.add(rescue); opts.add_all(upsell)

    if ctx.skip_eligible:
        opts.add(SKIP(price=300))                                               # wireframe D

    canoe_line  = CANOE_LOST_LINE(last = (ctx.canoes == 1)) if not ctx.infinite_canoes_active else none   # bajo el titular: "Travesía perdida: −1 canoa" / "…tu última canoa"
    shield_line = SHIELD_LINE if rc.stuck_show_shield_status and ctx.streak ≥ 1 else none                  # sobre el pie
    footer = GIVE_UP                                   # D-008: "Rendirse" [P] desde el 31 (antes, "Volver a la orilla (gratis)"); el coste es del bloqueo, no del reinicio
    return Modal(title="No hay más vertidos", canoe_line, opts, shield_line, footer)

function onChoice(choice, ctx):
    switch choice:
      UNDO:            pop history; close modal                                 # sin fallo
      UNDO_REFILL_AD:  show rewarded(placement="rv_undo_refill"); on reward: undo_stock += 3 (provisional, SSV)
      UNDO_REFILL_SEED: saldo -= 30; undo_stock += 3
      RESCUE:          saldo -= price; rescues_today++; rescues_level++; addCuia(cap=4, clean)
                       streak preserved; log; close modal                        # "la compra cumple"
      PUÑADO_199:      IAP; on success re-render modal with new saldo (same price)
      BALSA_499:       IAP; on success: apply RESCUE at no seed cost + 1000 seeds + extra_cuia_stock += 1 + infinite_canoes 30 min   # contenido fijado en 05 §4.1 / 09 decisión 16
                       af_rescue_shown_today = 1; on cancel: af_rescue_declined_days++ (reset on purchase)
      SKIP:            saldo -= 300; markSkipped(level)  # skipped_levels += level; since_last_skip = 0, que no se lee (D-033, B3-Q-S1)
                       load next level
      RESTART:         failHard(ctx)                                            # solo en 1-30 (rama C)
      GIVE_UP:         if ctx.infinite_canoes_active: failHard(ctx)             # D-008: sin canoa en juego, directo a Nivel fallido [P]
                       else: openGiveUpConfirm(ctx)                             # paso 2 [P]

function buildGiveUpConfirm(ctx):                     # paso 2 · ¿Seguro? [P] (D-008), desde el 31
    canoe_line = "Perderás 1 canoa" if ctx.canoes > 1 else "Perderás tu última canoa"
    play_on = rescue                                   # el mismo RESCUE del paso 1: mismo precio y mismas condiciones;
                                                       # none si el rescate no está disponible (ramas A' y F): no aparece
    return Modal(title="¿Seguro?", canoe_line, play_on, close=X, footer=GIVE_UP)

function onGiveUpConfirm(choice, ctx):
    switch choice:
      RESCUE:          como en onChoice                                         # cierra el embudo y la partida sigue
      X, GIVE_UP:      failHard(ctx)                                            # confirma la rendición

function onQuitFromMenu(ctx):                         # pausa › "Salir" (D-008)
    if ctx.level < rc.rescue_min_level: goHome(); return                        # 1-30: sin coste y sin fallo
    if confirm("¿Salir del nivel? Perderás 1 canoa"): failHard(ctx)         # también con 0 vertidos (P16, D-029)
    
function failHard(ctx):                               # 1-30: Volver a la orilla desde el modal · ≥ 31: al entrar en Nivel fallido (D-008)
    fails_level++
    if ctx.level ≥ rc.rescue_min_level and not infinite_canoes_active: canoes -= 1   # canoas solo desde el 31 (§5.2, §7.1)
    if streak ≥ 1 and shield_available:
        shield_available = false; toast("Una hoja gigante te cubre: este fallo no rompe tu Crecida")
    else:
        streak = 0; rio_crecido = false                # con la barra a 0 tras el hito 10 el escudo no se gasta y el río crecido termina (05 §2.4, 06 §4.3; §8)
    if ctx.level < rc.rescue_min_level: retry(ctx)     # 1-30: reinicio directo, sin Nivel fallido (rama C)
    else: openLevelFailed(ctx)                         # paso 3 [P]: "Nivel {n}", "Premio: {20|60|80} semillas" (no se cobra), "Reintentar" [P] y X → inicio

function retry(ctx):                                   # "Reintentar" en Nivel fallido, o el reinicio de 1-30
    if ctx.level ≥ rc.rescue_min_level and canoes == 0 and not infinite_canoes_active:
        openNoCanoesScreen(); return                   # paso 4 (§7.3); con una canoa vuelve a Nivel fallido [P]
    if fails_level ≥ rc.relief_after_fails (3) and level.relief != [] and not level.reliefApplied:   # relief = [] antes del 31 (03 §13, 08 §4.1): mismo tablero
        level.loadVariant("relief[0]"); banner("El río bajó: …", anim_river_lowered_banner_ms)
    restartSameSeed()
```

### 6.3 Tabla de ramas

| Rama | Condición | Opción 1 | Opción 2 | Opción 3 | Pie | Resultado esperado |
|---|---|---|---|---|---|---|
| C · tutorial | nivel < 31 | Deshacer (o recarga) | — | — | Volver a la orilla (gratis, sin canoa) | Reinicio con el mismo tablero; sin "El río bajó" antes del 31 (`relief` = `[]`, 03 §13) |
| A · con saldo | nivel ≥ 31, saldo ≥ precio, rescate resoluble | Deshacer | **Continuar: +1 cuia** 300 (1.º del día) o 600 | — | «Rendirse» [P] (D-008; antes, Volver a la orilla) + línea "Travesía perdida: −1 canoa" | Conversión principal; "rescate → victoria" >85 % (research/03 C2.5 [E]) |
| S · ¿Seguro? [P] (D-008) | nivel ≥ 31, tras «Rendirse», con canoa en juego | Continuar: +1 cuia, al mismo precio y con las mismas condiciones del paso 1; no aparece si el rescate no está disponible | — | — | X o «Rendirse» → Nivel fallido (fallo duro) | Segunda oferta dentro del mismo bloqueo; no es takeover (05 §5) |
| A' · 2.º bloqueo del mismo nivel (sin rescate) | rescues_level = 1 (`rescue_max_per_level` = 1: ya hay 16 ranuras posibles en pantalla) | Deshacer (o recarga) | — | Salto si elegible | Igual | **Solo Marea y el pie** (Rendirse desde el 31): no existe multi-continue (`rescue_max_per_level` = 1, 02 §7, 05 §3.3 y 08 §9); se renuncia al multi-continue de Candy Crush (research/03 C2.1 [V]) para no superar el layout de 16 cuias |
| B · sin saldo · T3 | saldo < precio, LatAm | Deshacer (video primero si hay fill) | Continuar en gris "Te faltan N" | Puñado 1,99 local → Balsa de rescate 4,99 (si elegible) | Igual | Entrada ultrabaja primero: PPP 50 % (research/04 §3.3 [V]) |
| B' · sin saldo · T1 | saldo < precio, tier-1 | Deshacer | Continuar en gris | Balsa de rescate 4,99 (si elegible) → Puñado 1,99 | Igual | La oferta tras fallar es 17-34 % de las primeras compras en CBJ/Pixel Flow (research/03 C2.1 [V]) |
| B'' · Sin anuncios | no_ads = true | Como T1/T3 según geo; el video de Marea siempre en 2.ª posición | | | | Sin anuncios conserva recompensados (dirección 14) |
| D · 5 fallos | fails_level ≥ 5, ≥ 10 niveles desde el último de `skipped_levels` (D-033, B3-Q-S1), skippable | Deshacer | Continuar (según saldo) | **Salto del Boto 300** | Igual | Salida para el muro; raro por diseño |
| E · sin fill | `NO_FILL` / `MEDIATION_NO_FILL` en recarga de Marea | Deshacer por 30 semillas (sin botón de video) | Continuar | — | Igual | Reintentar carga con backoff hasta 64 s (research/03 C2.2 [V SDK]) |
| F · irresoluble con +1 | `rescue_solvable = false` | Deshacer (si 0 cupo: 1 recarga gratis de +1 uso [P]) | — | Salto si elegible | Rendirse (Volver a la orilla en 1-30) | No se vende lo que no cumple; evento `af_rescue_unsolvable` para revisar el nivel |
| G · 1 canoa | canoes = 1 | Igual que su rama | | | Igual; la línea de estado dice "Travesía perdida: tu última canoa" | Tras rendirse → Nivel fallido; «Reintentar» → Sin canoas (D-008) |
| H · Canoas infinitas | activo | Igual que su rama | | | Igual; sin línea de canoa | Sin presión de canoa: «Rendirse» va directo a Nivel fallido [P] (D-008); la Crecida sigue en juego |

### 6.4 Verificación "la compra cumple"

Antes de mostrar la opción Continuar, el cliente ejecuta A* acotado (`hint_budget_nodes` = 40.000 nodos) sobre `state + 1 cuia vacía de capacidad 4`. Resultado `solvable` → se ofrece; `unsolvable` (demostrado) → rama F; `timeout` → se ofrece (el presupuesto no demostró nada). Coste objetivo: <50 ms en cliente, siempre dentro de los `stuck_modal_delay_ms` (400 ms) previos al modal.

**En Aguas turbias la verificación corre sobre el estado real**, con las capas ocultas incluidas: el cliente conoce la asignación verdadera (02 R2: `color` conocido, `visible = false`), así que no hay realidades que enumerar y el coste es el mismo que en un nivel limpio. `unsolvable` en la realidad verdadera → rama F **sin opción de compra**, aunque alguna realidad consistente con lo visible fuera resoluble: vender una Cuia de rescate que no resuelve el tablero real sería exactamente la queja "la compra no cumple" que prohíben el principio 4 y research/09 §Qué copiar 3. La enumeración de realidades (≤ 24, `lvl_murky_max_hidden_units` = 4; 02 T2) queda reservada a Morpho (§2.3), que sí debe razonar solo con lo que el jugador ve porque su salida es visible; la verificación de rescate usa la realidad verdadera, que el cliente conoce, y no enumera. La verificación no muestra nada al jugador: solo decide si el botón aparece (filtra como mucho el bit "ni con una cuia más", que la rama F ya comunica en texto).

La rama F debe ser excepcional (<0,5 % de bloqueos); si un nivel la dispara más, el nivel sale del pool vía Remote Config (research/02 §Implicaciones [E]). En niveles `murky` se vigila por separado (`af_rescue_unsolvable` lleva `murky` entre los parámetros comunes de 08 §8.1): la realidad verdadera es una sola, así que la rama F puede ser algo más frecuente que en niveles limpios [P].

### 6.5 Textos de UI (ES-419)

| Clave | Texto |
|---|---|
| `stuck_title` | No hay más vertidos |
| `stuck_subtitle` | Aguas estancadas |
| `stuck_rescue_cta` | Continuar: +1 cuia |
| `stuck_rescue_copy` | ¡Capi llega al rescate con una cuia más! Sigue desde aquí. |
| `stuck_rescue_discount` | Primer rescate del día: −50 % |
| `stuck_rescue_missing` | Te faltan {n} semillas |
| `stuck_undo` | Deshacer · Marea |
| `stuck_undo_refill_ad` | +3 Deshacer · Ver video |
| `stuck_undo_refill_seeds` | +3 Deshacer · 30 semillas |
| `stuck_pack_199` | Puñado de semillas · 1.000 · {precio_local} |
| `stuck_balsa_499` | Balsa de rescate · {precio_local} · Todo para seguir: semillas, una cuia y canoas infinitas |
| `stuck_balsa_499_content_no_canoes` [P] | Con `biz_feature_lives` apagado (D-038, añadido del 2026-10-21), la segunda parte: Todo para seguir: semillas y una cuia |
| `stuck_skip` | Salto del Boto · 300 · Rosa te lleva río arriba, al siguiente nivel |
| `stuck_restart` | Volver a la orilla (gratis) |
| `stuck_canoe_lost` | Travesía perdida: −1 canoa |
| `stuck_canoe_lost_last` | Travesía perdida: tu última canoa |
| `stuck_shield_ready` | Hoja escudo lista: este fallo no rompe tu Crecida |
| `stuck_shield_used` | Sin Hoja escudo hoy: este fallo rompe tu Crecida de {n} |
| `stuck_unsolvable` | Ni una cuia más salva este tramo. Prueba la Marea o vuelve a la orilla. |
| `river_lowered_color` | El río bajó: esta vez hay un color menos |
| `river_lowered_cuia` | El río bajó: esta vez hay una cuia más |
| `shield_toast` | Una hoja gigante te cubre: este fallo no rompe tu Crecida |
| `rescue_applied` | ¡Una cuia más! Sigue desde aquí |
| `stuck_give_up` | Rendirse |
| `giveup_confirm_title` | ¿Seguro? |
| `giveup_confirm_canoe` | Perderás 1 canoa |
| `level_failed` | Nivel fallido |
| `level_failed_reward` | Premio: {n} semillas |
| `retry` | Reintentar |
| `quit_confirm` | ¿Salir del nivel? Perderás 1 canoa |

Las siete claves de D-008 (`stuck_give_up` a `quit_confirm`) las aprobó el dueño el 2026-10-19, con el nombre «Sin canoas» (P14 de `docs/dev/decisiones.md`, D-029). `stuck_skip` dice «río arriba» desde D-013 (Q13-A: antes, «río abajo»). En 1-30 el pie del modal usa `stuck_restart` ("Volver a la orilla (gratis)"); desde el 31 usa `stuck_give_up` [P] (D-008). `stuck_canoe_lost` / `stuck_canoe_lost_last` son líneas de estado bajo el titular desde el nivel 31, ocultas con Canoas infinitas; sustituyen a las antiguas `stuck_restart_canoe` / `stuck_restart_last_canoe`, que atribuían al reinicio un coste que es del bloqueo y contradecían el tooltip del primer modal del tutorial (02 §13, nivel 6: "Reiniciar es gratis, siempre, mismo tablero"). `stuck_rescue_copy` no se muestra en el panel de Aguas estancadas (§6.6) [P]; queda solo para la llegada de Capi de 07 §5.5 (`ui_mascot_rescue_anim`), pendiente de alinear con este panel en el lote Dc5.

### 6.6 Wireframes (360 × 780, el tablero queda a la vista y atenuado; sin cuenta atrás)

**Panel sin ilustración (alineación con D-008, 2026-10-08) [P].** Aguas estancadas sigue la composición de «No Available Moves» de Magic Sort (ficha §4.4, t = 404 y 777): el saldo arriba, el tablero entero a la vista y atenuado al 70 % (07 §5.5; sube lo justo para que el panel no lo tape) y las opciones en el panel de abajo, sin ilustración ni la copia `stuck_rescue_copy`, para que el jugador vea qué deshacer. Sustituye a la ilustración de 96 px de los wireframes A-D («Capi en canoa con una cuia», «Capi en la orilla», y Capi con Rosa en D) y a la copia de Capi del modal de 02 §7. ¿Seguro? y Nivel fallido sí llevan su ilustración (canoa volcada y cesta de semillas vacía), como el corazón roto de «Continue?» y las monedas de «Level Failed!» (ficha §4.4).

**A · Con saldo, primer rescate del día (nivel ≥31)**

```
+--------------------------------------------+
| (S) 1.240                     (C) 4/5      |   cabecera: semillas, canoas
|  . . tablero a la vista, atenuado . . .    |   al 70 % (07 §5.5); el panel no lo tapa
|  +--------------------------------------+  |
|  |          ~ Aguas estancadas ~        |  |   subtítulo pequeño
|  |        NO HAY MAS VERTIDOS           |  |   titular
|  |     Travesía perdida: -1 canoa       |  |   línea de estado (≥31; oculta con Canoas infinitas)
|  |                                      |  |   sin ilustración (D-008) [P]
|  |  +--------------------------------+  |  |
|  |  |  <- Deshacer · Marea      [2]  |  |  |   Regla 0: secundaria (marfil), 48 px
|  |  +--------------------------------+  |  |
|  |  +--------------------------------+  |  |
|  |  |  CONTINUAR: +1 CUIA            |  |  |   CTA primaria (ámbar), 56 px
|  |  |  (S) 300   ~600~   -50 % hoy    |  |  |
|  |  +--------------------------------+  |  |
|  |   Hoja escudo lista: este fallo      |  |   línea de estado (RC)
|  |   no rompe tu Crecida                |  |
|  |                                      |  |
|  |   Rendirse [P]                       |  |   enlace al pie, 40 px; lleva a ¿Seguro? (D-008; antes, «Volver a la orilla (gratis)»)
|  +--------------------------------------+  |
+--------------------------------------------+
```

**B · Sin saldo suficiente, geo T3 (LatAm)**

```
+--------------------------------------------+
| (S) 140                       (C) 3/5      |
|  +--------------------------------------+  |
|  |          ~ Aguas estancadas ~        |  |
|  |        NO HAY MAS VERTIDOS           |  |
|  |     Travesía perdida: -1 canoa       |  |
|  |  +--------------------------------+  |  |
|  |  |  +3 Deshacer · Ver video       |  |  |   Regla 0: ruta gratuita primero
|  |  +--------------------------------+  |  |
|  |  +--------------------------------+  |  |
|  |  |  CONTINUAR: +1 CUIA   (S) 600  |  |  |   gris; sin descuento (ya hubo rescate hoy)
|  |  |  Te faltan 460 semillas        |  |  |
|  |  +--------------------------------+  |  |
|  |  +--------------------------------+  |  |
|  |  |  Puñado · 1.000 (S) · $ 3.900  |  |  |   precio local (COP) desde Unity IAP
|  |  +--------------------------------+  |  |
|  |  +--------------------------------+  |  |
|  |  |  Balsa de rescate · $ 9.900    |  |  |   1/día; contenido visible
|  |  |  semillas + cuia + canoas inf. |  |  |
|  |  +--------------------------------+  |  |
|  |   Rendirse [P]                       |  |
|  +--------------------------------------+  |
+--------------------------------------------+
```

En geo T1 las dos tarjetas de compra se invierten (Balsa de rescate arriba) y la recarga de Marea por video baja a la segunda posición si el jugador tiene saldo para 30 semillas.

**C · Niveles 1-30 (sin canoas, sin rescate)**

```
+--------------------------------------------+
| (S) 310                                    |   sin contador de canoas antes del 31
|  +--------------------------------------+  |
|  |        NO HAY MAS VERTIDOS           |  |
|  |  +--------------------------------+  |  |
|  |  |  <- Deshacer · Marea      [1]  |  |  |
|  |  +--------------------------------+  |  |
|  |  +--------------------------------+  |  |
|  |  |  Volver a la orilla (gratis)   |  |  |   botón, no enlace: es la salida normal
|  |  +--------------------------------+  |  |
|  +--------------------------------------+  |
+--------------------------------------------+
   Al reiniciar: el mismo tablero, siempre (antes del 31 relief = []: sin "El río bajó"; 03 §13).
   Desde el 31, tras el 3.er fallo: [ banner 2,0 s ]  "El río bajó: esta vez hay un color menos"
   (anim_river_lowered_banner_ms, 08 §9)
```

**D · Quinto fallo (Salto del Boto elegible)**

```
+--------------------------------------------+
| (S) 820                       (C) 2/5      |
|  +--------------------------------------+  |
|  |          ~ Aguas estancadas ~        |  |
|  |        NO HAY MAS VERTIDOS           |  |
|  |     Travesía perdida: -1 canoa       |  |
|  |  +--------------------------------+  |  |
|  |  |  <- Deshacer · Marea      [3]  |  |  |   Regla 0: secundaria (marfil), 48 px
|  |  +--------------------------------+  |  |
|  |  +--------------------------------+  |  |
|  |  |  CONTINUAR: +1 CUIA   (S) 600  |  |  |   CTA primaria (ámbar), 56 px
|  |  +--------------------------------+  |  |
|  |  +--------------------------------+  |  |
|  |  |  Salto del Boto       (S) 300  |  |  |   3.ª opción, estilo secundario
|  |  |  Rosa te lleva al siguiente    |  |  |
|  |  +--------------------------------+  |  |
|  |   Sin Hoja escudo hoy: este fallo    |  |
|  |   rompe tu Crecida de 4              |  |
|  |   Rendirse [P]                       |  |
|  +--------------------------------------+  |
+--------------------------------------------+
```

**Paso 2 · ¿Seguro? [P] (D-008; desde el 31, tras «Rendirse»)**

```
+--------------------------------------------+
| (S) 1.240                     (C) 4/5      |   saldo arriba; tablero atenuado detrás
|  +--------------------------------------+  |
|  |            ¿SEGURO?             [X]  |  |   la X confirma la rendición
|  |       [ canoa volcada ]              |  |   ilustración
|  |        Perderás 1 canoa              |  |   "Perderás tu última canoa" con 1
|  |  +--------------------------------+  |  |
|  |  |  CONTINUAR: +1 CUIA   (S) 600  |  |  |   mismo precio y condiciones del paso 1;
|  |  +--------------------------------+  |  |   no aparece si el rescate no está disponible
|  |   Rendirse [P]                       |  |   confirma la rendición → Nivel fallido
|  +--------------------------------------+  |
+--------------------------------------------+
   Con Canoas infinitas no hay canoa en juego: «Rendirse» pasa directo a Nivel fallido [P].
```

**Paso 3 · Nivel fallido [P] (D-008; tras confirmar la rendición o salir con el menú)**

```
+--------------------------------------------+
| (S) 1.240                     (C) 3/5      |   la canoa ya se descontó (failHard)
|  +--------------------------------------+  |
|  |            NIVEL 34             [X]  |  |   la X vuelve al inicio
|  |          NIVEL FALLIDO               |  |
|  |      [ cesta de semillas vacía ]     |  |   ilustración
|  |      Premio: 20 semillas             |  |   20 · 60 · 80 según la etiqueta; no se cobra
|  |  +--------------------------------+  |  |
|  |  |  REINTENTAR [P]                |  |  |   mismo tablero y semilla; "El río bajó" al 3.er fallo;
|  |  +--------------------------------+  |  |   con 0 canoas abre Sin canoas (§7.3)
|  +--------------------------------------+  |
+--------------------------------------------+
   Sin intersticial antes ni después (05 §10).
```

El orden vertical es siempre el de `stuck_order_t1` / `stuck_order_t3` (08 §9: "undo,rescue,…"; Regla 0 de §6.2): Marea → Cuia de rescate → ofertas → Salto del Boto → pie (Volver a la orilla en 1-30; Rendirse desde el 31, D-008); la jerarquía visual de la CTA primaria (ámbar, 56 px) no lo altera.

Reglas de interacción: el botón Atrás de Android no cierra el modal ni ¿Seguro? (vibración corta); en Nivel fallido y en Sin canoas hace lo mismo que su X (tabla completa de Atrás en 02 §3.5; D-063: observación 21 de la jornada 6, decisión del dueño del 2026-10-30). La única salida sin elegir es el menú de pausa → "Salir": en 1-30 guarda el estado atascado y sale sin coste; desde el 31 pide «¿Salir del nivel? Perderás 1 canoa» [P] y es fallo duro (§5.2; D-008). Cerrar la app con el modal abierto no es fallo: al volver, el modal reaparece (§5.2). El precio mostrado se congela al abrir el modal aunque Remote Config cambie durante la sesión. *Sustituido por D-008 (2026-10-08): «Máximo un modal por bloqueo; nunca se apila un segundo pop-up» (research/09 §Mejorar "máximo un takeover" [V]). Desde el 31 el bloqueo encadena hasta cuatro pantallas, una tras otra: Aguas estancadas → ¿Seguro? → Nivel fallido → Sin canoas. Son el mismo bloqueo: ninguna cuenta como takeover (05 §5) ni lleva anuncio (05 §10).*

## 7. Canoas (vidas)

### 7.1 Reglas

| Regla | Valor | Fuente |
|---|---|---|
| Máximo | **5** Canoas | dirección 11; Magic Sort 5 vidas (research/magic-sort/b §2.1 [V]) |
| Regeneración | **1 cada 25 min** mientras haya menos de 5; cronómetro mm:ss junto al contador en la pantalla de inicio (06 §4.7), en el saldo grande del embudo (D-008, paso 1) y de la victoria, y en Sin canoas (§7.3). La cabecera de partida lleva «n/5» sin cronómetro [P] (alineación con D-008: Magic Sort no enseña vidas ni temporizador durante la partida, ficha §2.3; la píldora de Canoas es la de 07 §7.4) | dirección 11; research/magic-sort/b §2.1 [V]; D-008 |
| Debut | **Nivel 31** (`rescue_min_level`), con 5/5 y tarjeta de 1 toque antes del nivel, solo al entrar en la campaña: "Canoas: si te rindes o sales del nivel, pierdes una. Vuelven solas cada 25 minutos." [P] (Q-E4 (a), D-033; con D-008 la canoa se pierde al rendirse o al salir del nivel). Solo habla de las canoas, sin línea de la Cuia de rescate (Q-B11-9 (a)); «25 minutos» y «nivel 31» van literales en los textos, así que si Remote Config cambia `canoes_regen_min` o `rescue_min_level` hay que revisarlos (Q-B11-6 (a)); con `biz_feature_lives` apagado no sale (D-038; D-033, añadido de B11). Antes del 31 la cabecera no muestra canoas y fallar solo reinicia. | dirección 10 y 11 |
| Qué consume | **D-008 (2026-10-08):** solo el fallo duro (§5.2): rendirse tras el bloqueo (¿Seguro? → Nivel fallido) o salir del nivel con el menú, desde el 31. Nunca el reinicio voluntario, un crash ni un cierre de la app (las reseñas de Magic Sort castigan "crashes que cuestan vidas", research/09 §Técnico [R]): el nivel en curso se guarda tras cada vertido (02 R8 y §14), así que cerrar la app no es una salida gratis. *Sustituye a «Solo el fallo duro (§5.2). Nunca el reinicio voluntario, el abandono, un crash ni un cierre de la app»* | dirección 2; D-008 |
| Qué bloquea | Con **0 canoas no se puede iniciar ni reiniciar un nivel de campaña**: Jugar (inicio o Mapa del río), Siguiente (pantalla de victoria), Reintentar (Nivel fallido; D-008) y Volver a la orilla (cabecera) abren Sin canoas (§7.3). Una partida ya cargada nunca se interrumpe por canoas: el nivel en curso sigue jugable hasta ganar o bloquearse (si una sincronización deja el contador en 0 a mitad de nivel, se termina igual). La **Cuia de rescate no requiere canoa**: con 0 canoas y saldo se compra y se sigue. Reto del río, Mapa del río, Mercado, Bitácora del río, Canasta y Cuaderno de campo siguen disponibles. Misma frase en 02 §7 | dirección 11 (fija cantidad y regeneración, no el efecto; precisión de este capítulo, §Desviaciones 7); research/magic-sort/b §2.1 [V] |
| Qué no requiere canoas | Reto del río, eventos (Travesía, Encuentro de las Aguas), Río sin fin [P] | glosario §7 |
| Reloj | Basado en hora UTC del dispositivo contrastada con la hora del servidor del último fetch de Remote Config; si la diferencia supera `sec_clock_max_drift_h` (24 h; clave global de 08 §9, que absorbe la antigua `canoes_clock_max_drift_h`) las canoas no regeneran hasta reconectar (research/06 C6.1 [E], misma regla que el Reto del río) | research/06; 08 §9 |
| Canoa extra | Ubicación `rv_extra_canoe`: **+1 Canoa**, solo en la pantalla Sin canoas; tope **3/día**, cooldown **90 s** [P] | dirección 11 y 13 |
| Llenar canoas | 5/5 por **200 semillas** [P] desde la pantalla Sin canoas (≈0,40 USD; Royal Match cobra 900 monedas, research/04 C1 [S]); `canoes_refill_seeds_enabled` | precisión, §Desviaciones |
| Canoas infinitas | Temporizadas: 30 min (Balsa de rescate), 60 min (IAP `af_infinite_canoes_1h`, ID canónico de 05 §4.1 y 00 §11, en Sin canoas y Mercado, bundles del Mercado, premios de Regata y Liga del río 1.2; la casilla 30 de la Bitácora **no** las incluye: 05 §2.3 / 06 §4.2). La Crecida **no** otorga Canoas infinitas: su hito 6 es la Canasta (05 §2.4). Durante el periodo los fallos duros no consumen canoa; ∞ y el tiempo que queda sustituyen al contador (en la cabecera de partida, solo ∞ [P]); el tiempo corre en tiempo real aunque la app esté cerrada | dirección 11 y 14; research/magic-sort/b §2.1 [NV 15-30 min] |
| Prestar una canoa | Aldea, v2.0; el hueco de UI queda reservado pero oculto en 1.0 | dirección 11 |

### 7.2 Interacción con el bloqueo

- Con 1 canoa, la línea de estado del modal avisa "Travesía perdida: tu última canoa" (`stuck_canoe_lost_last`) y ¿Seguro? repite «Perderás tu última canoa» [P] (D-008). Desde el 31 el pie es «Rendirse» [P]: el coste es del bloqueo, no del reinicio. Al pulsar «Reintentar» con 0 canoas se abre Sin canoas; el nivel queda en curso y, en cuanto hay una canoa, vuelve a Nivel fallido con «Reintentar» [P] (§7.1 "Qué bloquea"; §7.3).
- La Cuia de rescate nunca consume canoa: es la segunda razón de compra tras conservar la Crecida (pérdida triple de Magic Sort: nivel, racha, vida, research/magic-sort/b §5 [V]). Con D-008 se ofrece dos veces en el mismo bloqueo: en Aguas estancadas y en ¿Seguro?, al mismo precio y con las mismas condiciones; sigue siendo 1 rescate por nivel como máximo.
- Canoas infinitas activas: la línea "Travesía perdida" no se muestra, «Rendirse» pasa directo a Nivel fallido [P] (D-008) y la presión recae solo en la Crecida y el tiempo invertido. Las Canoas infinitas **no quitan los intersticiales** (05 §6.1; postura del dueño del 2026-10-08).
- `biz_feature_lives` apagado (D-038; 08 §9): las canoas desaparecen en todos los niveles (ni la cabecera, ni el modal, ni los avisos, ni Sin canoas) y desde el 31 todo funciona como en 1-30: salir es gratis y no es fallo, y el pie del modal es «Volver a la orilla (gratis)» (§6, rama C). No es lo mismo que Canoas infinitas: con ellas el embudo de D-008 sigue. El rescate sigue con `biz_feature_rescue`, y la Balsa de rescate no da Canoas infinitas: queda en «semillas y una cuia», con el mismo precio (D-038, añadido del 2026-10-21; §6).

### 7.3 Pantalla Sin canoas

```
+--------------------------------------------+
|                SIN CANOAS                  |
|      [ 5 canoas volcadas en la orilla ]    |
|   Tu próxima canoa vuelve en  18:42        |
|                                            |
|  +--------------------------------------+  |
|  |  Canoa extra · Ver video      +1(C)  |  |   rv_extra_canoe (si hay fill y cupo)
|  +--------------------------------------+  |
|  +--------------------------------------+  |
|  |  Llenar canoas · 200 (S)      5/5    |  |   RC: canoes_refill_seeds_enabled
|  +--------------------------------------+  |
|  +--------------------------------------+  |
|  |  Canoas infinitas 1 h · {precio}     |  |   IAP af_infinite_canoes_1h (05 §4.1)
|  +--------------------------------------+  |
|  [ Pedir una canoa a tu Aldea ]  (oculto en 1.0)
|                                            |
|  Mientras esperas: Jugar el Reto del río > |   no consume canoas
|  Volver al Mapa del río                    |
+--------------------------------------------+
```

Se abre desde el 31 con 0 canoas al pulsar «Reintentar» en Nivel fallido (`retry`, §6.2), Jugar o Siguiente, con Volver a la orilla desde la cabecera y con el toque sobre el contador 0/5 (D-008); nunca interrumpe una partida en curso (§7.1 "Qué bloquea"). Si se abrió desde Nivel fallido, en cuanto hay una canoa (espera, video, semillas o compra) vuelve a Nivel fallido con «Reintentar», como Magic Sort (t = 338) [P]. Nunca muestra intersticial (05 §10; D-008). Orden de opciones: esperar (siempre visible, con cronómetro), video, semillas, IAP, Aldea. Sin cuenta atrás de oferta. En geo T3 el video va primero; en T1 el orden es el mismo (la presión de vidas pesa poco en la conversión del rescate sin la racha, research/03 C2.1 lectura 3 [V]).

*P17 (`docs/dev/decisiones.md`, D-008), decidida el 2026-10-19 en la A (D-029): «Canoas infinitas 1 h» sale siempre, como en este wireframe, detrás de esperar, la Canoa extra por vídeo y las semillas, y sin cuenta atrás; 05 §4.1 y §5 ya no la condicionan a dos rechazos de la Canoa extra ni a una caducidad de 10 min. Así lo hace `NoCanoesScreen`.*

## 8. Crecida y Hoja escudo como colateral del rescate

- **Crecida** (`win_streak`): debuta en el **nivel 25** junto con la Hoja escudo (06 §3; §4 de este capítulo), seis niveles antes de Canoas y rescate; cada victoria consecutiva sube la barra (vertical, en el borde derecho del HUD: 07 §7.4). Escalera canónica de hitos (ver 05 §2.4; clave única `streak_milestones`, 08 §9): **3 → 300 semillas · 6 → Canasta · 10 → 1 Cuia extra + "río crecido"** (Cosecha doble sin video); **cada hito se paga una vez al día local** (`streak_hits_once_per_day`). **Tras el hito 10 la barra vuelve a 0** y el río crecido **sigue activo hasta el siguiente fallo duro no escudado** (`failHard` de §6.2: con la barra a 0, un fallo duro apaga el río crecido sin gastar la Hoja escudo, igual que en 05 §2.4 y en `tools/econ_sim.py`). Tope diario 600 semillas (`streak_daily_cap`): suma **solo** el hito 3 (300) y las 50 semillas de la Canasta del hito 6; el bonus del río crecido y los comodines de los hitos 6 y 10 no cuentan (dirección 12; research/04 C1 §3 [E]). La Cuia de rescate **conserva** la racha; el fallo duro la rompe salvo Hoja escudo. Frente al coste del rescate: el hito 3 cubre el primero del día (300 ≥ 300) y la escalera completa suma ≈566 semillas-equivalentes (300 + Canasta ≈116, 05 §2.1 + Cuia extra 150), más el río crecido (06 §4.3; glosario §7.2 regla transversal; valores de 05 §2.4).
- **Hoja escudo** (`streak_shield`): 1 por día (reinicio a las 00:00 locales), se aplica **automáticamente** al primer fallo duro del día con Crecida ≥1; toast "Una hoja gigante te cubre: este fallo no rompe tu Crecida". No salva la canoa (solo la racha). El modal muestra su estado en una línea (`stuck_show_shield_status`, por defecto activo): se pierde algo de presión de venta a cambio de la honestidad que la dirección prioriza y que el género castiga cuando falta (rachas que "se reinician solas", research/09 §Mejorar [V]). Si el A/B de soft launch muestra >15 % menos conversión del rescate con la línea visible, se apaga por Remote Config sin tocar la regla.
- Persistencia de la Crecida entre sesiones: no expira de noche (el río crecido tampoco); expira solo con un fallo duro sin escudo (research/09 §Mejorar "racha de 19 perdida por dormir" [R]). 06 §4.3 fija el resto del evento con los valores de 05 §2.4.

## 9. Ratio de presión

Definición: `ratio = precio del rescate ÷ semillas ganadas por victoria`. Magic Sort: 900 ÷ 10-20 → **45:1** sin racha, ≈9:1 con racha perfecta (300/3 = 100 por nivel efectivo) (research/09 §Economía [R/E]); un fallo cuesta de facto 1,35-1,80 USD (research/magic-sort/b §2.2 [E]). Objetivo de research/09 §Mejorar: **10-15:1**.

Ingreso en nivel por victoria con el ciclo de 20 niveles de la dirección 5 (12 comodidad + 2 recuperación a 20, 5 reto etiquetados Difícil a 60, 1 hito Muy difícil a 80; supuesto [P] de que todo nivel de reto lleva etiqueta Difícil, lo confirma 03 §11; primer Difícil en el 40 y primer Muy difícil en el 60, 03 §11/§12):

```
(14 × 20 + 5 × 60 + 1 × 80) / 20 = 660 / 20 = 33 semillas/victoria
+ Canasta cada 10 niveles: 50 semillas / 10 = 5 semillas/victoria   (el comodín de la Canasta no se cuenta)
= 38 semillas/victoria de ingreso en nivel
```

El ingreso **efectivo** por victoria (todas las fuentes: Cosecha doble, Crecida con hitos una vez al día, Bitácora, Canasta, Reto del río) lo fija la simulación de 05 §3.2 (escenario B, corrida con el ciclo de D-009 y la Bitácora «1 de 3» de D-033, `10f66534`, de `tools/econ_sim.py` con las tasas de fallo de 03 §15, aprobada el 2026-10-21 en D-033, Q-B15; antes, la de `0faf51e`, D-029; `eco_model_version` = "B-2026-10-06e"; salida `tools/out/econ_B.md` [sim]); este capítulo toma esas cifras sin recalcularlas: **63 casual · 98 medio no pagador · 93 hábil · 98 pagador ligero** semillas/victoria (62,7 · 98,4 · 93,4 · 97,9; las columnas «efectivo» de abajo se calculan con estas cifras sin redondear).

| Perfil (05 §3.1) | Victorias/día (05 §3.2) | Ingreso en nivel/victoria | Ingreso efectivo/victoria (05 §3.2 [sim]) | Ratio 600 (en nivel · efectivo) | Ratio 300, 1.º del día (en nivel · efectivo) | Rescates pagables/día con ingreso gratuito (05 §3.2) |
|---|---|---|---|---|---|---|
| **Casual sin racha** (ve pocos videos, no sostiene la Crecida) | 6,7 | 38 | 63 | **15,8 · 9,6** | 7,9 · 4,8 | 0,70 |
| **Medio no pagador** (referencia) | 12,4 | 38 | 98 | 15,8 · 6,1 | 7,9 · 3,0 | 2,04 |
| **Hábil con racha** (Hoja escudo cubre su fallo diario) | 16,6 | 38 | 93 | 15,8 · 6,4 | 7,9 · 3,2 | 2,58 |
| **Pagador ligero** (Mochila, Puñado, Balsa, Cántaro; 8 USD/30 días) | 13,9 | 38 | 98 | 15,8 · 6,1 | 7,9 · 3,1 | 2,27 |
| **Magic Sort** (referencia) | — | 10-20 [R] | 100 (racha perfecta: 300/3) | **45 · 9** | — | — |

05 §3.2 mide el ratio de presión por banda sobre el ingreso efectivo: 31-100 casual 9,9 · medio 6,1 · hábil 6,8; 101-500 casual 9,2 · medio 5,9 · hábil 6,1; referencia (media de los tres no pagadores) 7,6 / 7,1. Esa tabla es la fuente única de la calibración; las columnas "efectivo" de arriba son su lectura a precio nominal (600 ÷ ingreso efectivo).

Costo en dinero del rescate para el pagador ligero: 600 semillas × 0,002 = **1,19 USD** (300 → 0,60 USD) al precio del Puñado en tier-1; con precios localizados al ~50 % PPP (research/04 §3.3 [V]) ≈ 0,60 / 0,30 USD en BR/CO/MX/PE, frente a 1,35-1,80 USD por fallo en Magic Sort sin descuento LatAm (research/magic-sort/b §3.2 [V]).

Lectura: el ratio **en nivel** (la métrica comparable con el 45:1 del líder) queda en 15,8:1, en el borde alto del objetivo; con el primer rescate del día cae a 7,9:1. El ratio sobre el ingreso efectivo (6,1-9,6:1 a precio nominal; 5,9-9,9 por banda y 7,1-7,6 de referencia en 05 §3.2) es más generoso que el objetivo, lo que es intencional: el saldo del casual es positivo hasta el nivel 100 y pasa a neto negativo en 101-500 (−6 semillas/día, primer bloqueo sin recursos ≈ nivel 267, 05 §3.2 [sim]) por la cadencia de dificultad, no por precios; el medio y el hábil prácticamente no se bloquean sin recursos hasta el 500 (medio: 0,0 bloqueos por corrida, con alguno solo en el 1 % de las corridas; hábil: ninguno; 05 §3.2 [sim]). Palanca de calibración: si en soft launch la tasa de "bloqueados sin recursos" (sin canoa por video, sin ≥300 semillas) supera los objetivos de research/04 C1 §5 (≤3 % al nivel 50, ≈8 % al 100, 15-20 % al 200), **bajar** `rescue_price_bands` a `[[31,450]]` por Remote Config tras una revisión de diseño, porque el rango admisible sin ella en 08 §9 es 600 fijo (un solo precio global: sin cohorte geográfica de 450, el ajuste LatAm va en el precio local de los IAP, 05 §4.2); nunca subir a 750/900 sin subir las recompensas por victoria en la misma versión (principio 7; 05 §3.2 muestra que los tramos 600/750/900 bloquean al casual en el 99 % de las corridas).

Ratios de los comodines planificados: sobre 38/victoria en nivel, Marea +3 = 0,8 · Morpho 2,6 · Luciérnaga 3,2 · Cuia extra 3,9 · Pororoca 7,9 · Salto del Boto 7,9; sobre el ingreso efectivo (63 casual / 98 medio, 05 §3.2), Marea 0,5 / 0,3 · Morpho 1,6 / 1,0 · Luciérnaga 1,9 / 1,2 · Cuia extra 2,4 / 1,5 · Pororoca 4,8 / 3,0 · Salto del Boto 4,8 / 3,0 (columna de la tabla §3). Todos dentro del rango 1-10 que research/03 §Resumen fija para no repetir las reseñas de Hexa Sort (20) y Screw Jam (18-55) [R/E].

## 10. Inventario entre versiones y plataformas

### 10.1 Esquema del inventario en el guardado

```json
{
  "inventory_schema": 1,
  "economy_ts": "2026-10-05T14:03:11Z",
  "seeds": 1240,
  "canoes": { "count": 4, "next_regen_at": "2026-10-05T14:21:00Z", "infinite_until": null },
  "stock": { "undo": 6, "hint": 2, "reveal": 1, "extra_cuia": 0, "shuffle": 0 },
  "daily": { "date_local": "2026-10-05", "rv_undo_refill": 1, "rv_hint": 0, "rv_reveal": 0,
             "rv_extra_canoe": 0, "rv_double_harvest": 2, "rv_total": 3,
             "rescues_today": 1, "af_rescue_shown": 0, "af_rescue_declined_days": 0,
             "shuffles_today": 0, "shield_available": false },
  "progress": { "max_level": 87, "since_last_skip": 0, "skipped_levels": [63] },
  "level_state": { "level": 88, "seed": "amazonfill:v1:m04:v1:n0088", "variant": "base",
                   "fails_level": 1, "pours": 23, "extra_cuias": 1, "rescues_level": 0,
                   "undo_free": 2, "revealed_cuias": [5], "history": "<movimientos comprimidos>",
                   "stuck": false },
  "applied_purchases": ["3b1f…e07a", "9c42…51d8"]
}
```

El blob completo se mantiene <100 KB (research/06 §Guardado [E]) frente al límite de 3 MB de Play Games Saved Games [V]. `level_state` no guarda el tablero: referencia el nivel por su `seed` con el formato canónico de 08 §4.1 (`amazonfill:v1:m{mm}:v{v}:n{nnnn}`, con ceros a la izquierda; esquema `af-level/1`, `schemas/af-level-1.schema.json`) y por `variant ∈ {base, plus_empty, minus_color}` (la entrada de `relief[]` servida), más el historial comprimido; el formato del nivel es el de 08 §4.1 y este capítulo no define ejemplos propios. Con D-008, `level_state` tiene que reabrir también ¿Seguro? y Nivel fallido, no solo el modal: el booleano `stuck` no basta y el guardado debe llevar la fase del nivel (02 §2 y §14) [P]. `applied_purchases` guarda el `token_hash` de cada compra (SHA-256 del `purchaseToken` en hexadecimal, 64 caracteres; en el ejemplo, abreviado), nunca el `orderId` `GPA.…` ni el token en claro, en orden ordinal y sin repetidos (D-035, F3-3). `progress.since_last_skip` se escribe a 0 y no se lee: el hueco del Salto del Boto sale de `skipped_levels` (D-033, B3-Q-S1; §6.1).

### 10.2 Reglas de sincronización

| Situación | Regla |
|---|---|
| Plataforma de guardado | Android 1.0: Play Games Services v2 Saved Games + Firebase Auth anónimo para un UID estable (research/06 C6.1 [V]). iOS (después): el guardado multiplataforma en AWS (D-035; 08 §11), en lugar de iCloud Key-Value Store o Firestore; el cambio Android→iOS exige vincular cuenta (Google Sign-In o Apple) en la versión iOS, porque el UID anónimo no cruza plataformas. |
| Conflicto entre dispositivos | Resolución por **nivel máximo + `economy_ts`** (research/06 §Guardado [V]). Para consumibles (semillas, stock, canoas) se toma el bloque del blob con `economy_ts` más reciente; **nunca se suman** (evitaría duplicar stock jugando en dos dispositivos). Contadores `daily` se fusionan con `max` por clave para que los topes de video y de rescate no se dupliquen por dispositivo. |
| Compras | **Sin anuncios** (no consumible): lo restaura Play en cualquier dispositivo con la misma cuenta. **Semillas, Balsa de rescate, Canoas infinitas** (consumibles): la función `verifyPurchase` de la API en AWS (`purchases.products:get`, `acknowledge` en servidor, `setObfuscatedAccountId` = UID; el consumo lo hace el cliente) escribe cada `purchaseToken`, como hash, en el libro mayor de DynamoDB (D-035; 08 §2.4 y §10.3; research/06 C6.1 [V]); al restaurar, `restoreLedger` (`POST /v1/ledger/restore`, con el ID token de Firebase Auth; 08 §2.4 y §11) devuelve las compras del UID con su estado y toda compra ausente de `applied_purchases` se vuelve a acreditar. Así las semillas sobreviven a un dispositivo perdido aunque el blob no se haya subido. |
| Recompensas por video | Concesión provisional en cliente; confirmación por el callback S2S de MAX a la función `rewardedSsv` de la API en AWS (D-035; 08 §10.2), que el cliente lee en `restoreLedger` con los nonces pendientes; la revocación resta stock sin bajar de 0 y marca `fraud_flag` (research/06 C6.1 [V]). No se guardan en el libro mayor: el valor unitario (≤0,06 USD) no justifica reconciliación. |
| Estado del nivel en curso | `level_state` viaja en el blob: cambiar de dispositivo a mitad de nivel restaura tablero, historial, Cuia extra aplicada y capas reveladas. Si el nivel ya no existe en el pool del dispositivo destino (versión distinta), se descarta `level_state` y se carga el nivel por su número desde el pool local (misma semilla si el pool no cambió). |
| Versiones de la app | `inventory_schema` solo avanza; cada versión incluye migraciones `n → n+1`. Un comodín nuevo (p. ej. Luciérnaga en un jugador que viene de una versión sin Aguas turbias) entra con stock 0 y recibe su unidad gratis en el debut. Si un comodín se elimina, su stock se convierte en semillas al precio del Mercado (nunca se pierde valor comprado). |
| Precios y Remote Config | Precios y topes se leen de la caché de RC al abrir cada modal o botón; nunca cambian dentro de un modal abierto. Sin conexión se usan los últimos valores cacheados y, si no hay ninguno, los defaults compilados de `RemoteConfigDefaults.json`, generado desde la tabla única de 08 §9 (research/02 Complemento 4 [V]). |
| Offline | Todo comodín pagado con semillas o stock funciona sin red; los botones de video se ocultan y queda solo el precio (research/06 C6.1 [V]). Las compras exigen red. |

## 11. Métricas a instrumentar

Nombres exactos de la taxonomía única de 08 §8.2 (prefijo `af_` en todo evento propio; los eventos recomendados de Firebase conservan su nombre): esta tabla fija los parámetros específicos de los eventos que 08 §8.2 delega aquí ("Según capítulo 4 §11") y copia sin cambios los que 08 §8.2 define (`af_level_fail`, `af_river_lowered`, `af_streak_shield_used`, `af_ad_*`, `earn_virtual_currency`, `spend_virtual_currency`, `purchase`). Parámetros comunes: los de 08 §8.1, que `AF.Analytics` adjunta a todo evento de juego (`level`, `world`, `difficulty`, `murky`, `attempt`, `seeds_balance`, `canoes`, `streak`, `geo_tier`, `no_ads`, `app_version`, `content_version`, `rc_variant`, `session_id`, `net`); los eventos de tablero añaden además `pours` (vertidos del intento) y `t_ms` (tiempo desde el inicio del nivel), como `af_level_fail` en 08 §8.2. Este capítulo no usa ningún nombre de evento fuera de 08 §8.2: el modal Aguas estancadas se mide solo con `af_stuck_shown` / `af_stuck_choice` / `af_stuck_outcome`, la Hoja escudo con `af_streak_shield_used` y el fallo con `af_level_fail` más `level_end` {`success`, `level_name`} de Firebase (victoria, fallo duro o salto; 08 §8.2). `af_rescue_shown_today` (`daily.af_rescue_shown` en §10.1) y `af_rescue_declined_days` son contadores de estado de la Balsa de rescate (§6.1), no eventos.

| Evento | Parámetros específicos | Pregunta que responde |
|---|---|---|
| `af_booster_used` | `booster ∈ {undo, extra_cuia, hint, shuffle, reveal, skip}`, `source ∈ {free, stock, seeds, ad}`, `price`, `state_hash` | Uso por nivel y por vía; detecta niveles que exigen comodín |
| `af_booster_denied` | `booster`, `reason ∈ {no_balance, cap_level, cap_day, no_fill, unsupported, not_eligible}` | Dónde la ayuda no llega y por qué |
| `af_hint_result` | `solver ∈ {found, unsolvable, timeout}`, `realities (int, Aguas turbias)`, `nodes`, `ms`, `executed_hint (bool)` | Calidad del solver en cliente (timeouts en turbios → revisar `hint_timeout_ms` o el nivel); pistas que el jugador ignora |
| `af_shuffle_result` | `method ∈ {inverse, permute, failed}`, `par_before`, `par_after`, `tries`, `revealed_murky (int)` | Que la Pororoca nunca castigue (par_after ≤ par_before + 2) |
| `af_stuck_shown` | `rescue_price`, `discount (bool)`, `rescue_solvable`, `undo_available (bool)`, `options_order`, `skip_eligible`, `shield_status` | Frecuencia de bloqueo por nivel y rama servida |
| `af_stuck_choice` | `choice ∈ {undo, undo_ad, undo_seeds, rescue, pack_199, balsa_499, skip, restart, exit}` y, con D-008, `give_up`, `confirm_play_on` y `quit` [P]; `position`, `ms_to_choice` | Aceptación por rama, geo y saldo; cuántos compran en ¿Seguro? tras rendirse |
| `af_stuck_outcome` | `result ∈ {reward_received, no_fill, purchase_ok, purchase_cancel, rescued_then_win, rescued_then_fail}` | "Rescate → victoria" objetivo >85 % (research/03 C2.5 [E]) |
| `af_rescue_unsolvable` | `state_hash`, `seed` | Niveles a retirar del pool |
| `af_level_fail` | `kind ∈ {hard, soft}`, `canoe_lost (bool)`, `streak_broken (bool)`, `shield_used (bool)`, `relief_served (bool)` | Curva real de fallos; disparo de "El río bajó" |
| `af_river_lowered` | `variant ∈ {minus_color, plus_empty}` (tipos de `relief[]`, 08 §4.1), `fails_level` | Eficacia del alivio (victoria en el intento siguiente) |
| `af_canoes_depleted` | `wait_remaining_s` | Cuántos llegan a 0 y cuándo |
| `af_no_canoes_choice` | `choice ∈ {wait, ad, seeds_refill, iap_infinite, daily_challenge, map}` | Conversión de la pantalla Sin canoas |
| `af_canoes_wait_return` | `waited_s`, `returned (bool)` | Cuántos vuelven tras esperar; abandono por vidas |
| `af_infinite_canoes_start` / `_end` | `source`, `minutes`, `fails_during` | Valor percibido de las Canoas infinitas |
| `af_streak_shield_used` | `streak_at_use` | Uso diario del escudo |
| `af_skip_used` | `fails_level`, `since_last_skip` (= nivel − último salto de skipped_levels, o el nivel si no hay; D-033, B3-Q-S1), `level_tag` | Dónde está el muro |
| `spend_virtual_currency` (Firebase) | `virtual_currency_name = seeds`, `item_name ∈ {undo_refill, extra_cuia, hint, shuffle, reveal, skip, continue_cuia, canoes_refill, skin_*}`, `value` | Sumideros reales por ítem |
| `earn_virtual_currency` (Firebase) | `virtual_currency_name = seeds`, `source ∈ {win, hard, very_hard, double_harvest, streak, calendar, basket, daily_challenge, piggy, iap, refund, event}`, `value` | Fuentes reales; alimenta el ratio de presión realizado |
| `af_ad_request` / `af_ad_shown` / `af_ad_reward` | `placement ∈ {rv_undo_refill, rv_hint, rv_reveal, rv_extra_canoe, rv_double_harvest}` con `format = rewarded`; `result ∈ {filled, no_fill, error}` en `af_ad_request` y `granted ∈ {provisional, confirmed, revoked}` en `af_ad_reward` (resto de parámetros en 08 §8.2) | Fill y topes por ubicación; recompensas revocadas por SSV |
| `purchase` (Firebase) | `transaction_id`, `value`, `currency`, `items` (en este capítulo: `af_coins_2`, `af_rescue`, `af_infinite_canoes_1h`), `context ∈ {stuck, no_canoes, shop}` | Primera compra por contexto (referencia: 17-34 % desde la oferta tras fallar, research/03 C2.1 [V]) |

KPIs derivados y alarmas iniciales [P]: bloqueos por intento por nivel (alarma si un nivel Normal supera 40 %); aceptación de rescate por rama y geo; rescate → victoria >85 %; **ratio de presión realizado** = `rescue_price / mediana(earn por victoria en nivel)` por cohorte (objetivo 10-16); "bloqueados sin recursos" por tramo (≤3 % al 50, ≈8 % al 100, 15-20 % al 200, research/04 C1 §5 [E]); Marea por nivel (mediana >4 en Normal → revisar nivel); pistas ignoradas >50 % → revisar el resaltado; `af_shuffle_result` con `method = failed` >2 % → revisar k; `af_rescue_unsolvable` >0,5 % → retirar nivel; D1/D7 de rescatados frente a no rescatados; tiempo medio de espera en Sin canoas y tasa de retorno (<60 % → revisar regeneración de 25 min). Todo cruzado con `rc_variant` para los experimentos de precio.

## Parámetros configurables (Remote Config)

**Ver 08 §9**: es la única tabla de Remote Config del GDD (clave, tipo, default compilado en `RemoteConfigDefaults.json` y rango). Este capítulo no repite valores: describe el comportamiento en §2-§8 y usa exactamente los nombres de 08 §9. Claves que este capítulo justifica (dueño 04) o consume:

- **Marea**: `undo_free_per_level`, `undo_unlimited_until_level` (= 10, contrato de tutorial: ilimitada en 1-10 y 3 por nivel desde el 11), `undo_refill_amount`, `undo_refill_seed_price`.
- **Cuia extra**: `extra_cuia_unlock_level` (8, con 1 unidad regalada), `extra_cuia_seed_price`, `extra_cuia_max_per_level`.
- **Morpho y solver**: `hint_unlock_level` (12) y `hint_tutorial_units` (3 unidades regaladas), `hint_seed_price`, `hint_budget_nodes` (por realidad en Aguas turbias), `hint_weight`, `hint_timeout_ms` (al agotarse no se cobra); pista por inactividad `hint_idle_from_level` (= 3) y `hint_idle_delay_s` (= 12), en todos los niveles sin tope superior (02 §3.4).
- **Pororoca**: `shuffle_unlock_level`, `shuffle_seed_price`, `shuffle_max_per_level`, `shuffle_max_per_day`, `shuffle_par_slack`, `shuffle_budget_nodes`, `shuffle_reveals_murky`.
- **Luciérnaga y Aguas turbias**: `reveal_seed_price`; límites `lvl_murky_max_cuias` y `lvl_murky_max_hidden_units` (= 4 dosis ocultas / ≤24 realidades; dueño 03); tarjeta Aguas turbias · Bonus `ui_murky_bonus_enabled` (false en 1.0, true en 1.1; dueño 00).
- **Salto del Boto**: `skip_seed_price`, `skip_min_fails`, `skip_min_gap_levels`, `skip_min_level`.
- **Volver a la orilla y alivio**: `restart_confirm_min_pours` (umbral del fallo blando, §5.2), `relief_after_fails`, `relief_mode`, `relief_second_step_fails`, `anim_river_lowered_banner_ms`.
- **Bloqueo y Cuia de rescate**: `futile_rule_enabled`, `stuck_modal_delay_ms`, `rescue_min_level`, `rescue_price_bands` (clave única del precio; = `[[31,600]]`: 600 fijas, sin `rescue_base_price` ni tramos 750/900 ni cohorte geográfica), `rescue_first_day_mult`, `rescue_max_per_level` (= 1: T ≤ 14 generadas + 1 Cuia extra + 1 Cuia de rescate ≤ 16 ranuras, 02 R1 y §5), `rescue_check_solvable`, `stuck_order_t1`, `stuck_order_t3`, `stuck_show_shield_status`, `af_rescue_daily_cap`, `af_rescue_hide_after_declines`.
- **Canoas**: `canoes_max`, `canoes_regen_min`, `canoes_refill_seeds_enabled`, `canoes_refill_seed_price`; reloj `sec_clock_max_drift_h` (clave global de 08).
- **Recompensados**: `ads_rv_cap_<placement>` y `ads_rv_cooldown_s_<placement>` para `undo_refill`, `hint`, `reveal`, `extra_canoe`, `double_harvest`; `ads_rv_global_cap_day`; `ads_rv_double_harvest_from_level`.
- **Crecida y Hoja escudo**: `streak_unlock_level`, `streak_milestones`, `streak_hits_once_per_day` y `streak_daily_cap` (dueño 05, aquí consumidas; el tope suma solo el hito 3 y las 50 semillas de la Canasta del hito 6, §8); `streak_shield_per_day` (dueño 04).
- **Canasta**: `eco_basket_cycle` (dueño 06) y `eco_basket_seeds` (dueño 05).
- **Kill switches** (dueño 09): `biz_feature_lives` (apagado, desde el 31 todo como en 1-30: D-038, §6 y §7.2), `biz_feature_rescue`.

Alias retirados que este capítulo usaba y ya no existen (08 §9): `rescue_base_price`, `rescue_first_of_day_mult`, `relief_fails_threshold`, `idle_nudge_s` / `idle_nudge_from_level` (y las antiguas `hint_idle_enabled` / `hint_idle_until_level`), `murky_max_hidden_units`, `undo_refill_ad_cap_day`, `hint_ad_cap_day`, `reveal_ad_cap_day`, `canoes_extra_ad_cap_day` y sus `_cooldown_s`, `rv_global_cap_day`, `canoes_clock_max_drift_h`; SKU `af_infinite_canoes_60` → `af_infinite_canoes_1h`.

## Desviaciones de la dirección

Ninguna. Precisiones que la dirección no fijaba y que este capítulo cierra (no contradicen ningún punto):

1. **Fallo blando** (§5.2): el reinicio voluntario con ≥8 vertidos cuenta para "El río bajó" y para Salto del Boto, pero no cuesta canoa ni rompe la Crecida. Compatible con "reiniciar gratis e ilimitado" (dirección 2) y con el alivio tras 3 fallos (dirección 7).
2. **Salto del Boto desde el nivel 31** (`skip_min_level`): la dirección 9 no fija nivel; se alinea con Canoas y rescate para que el tramo 1-30 no tenga ningún sumidero de semillas ligado al fallo (dirección 10).
3. **Cuia extra limitada a 1 por nivel por cualquier vía** (dirección 9 dice "1 por nivel con semillas"); el stock gratuito no puede sumar una segunda. **Cuia de rescate máx. 1 por nivel** (la dirección no fija tope; lo fija el capítulo 02 §7 para que T ≤ 14 + Cuia extra + Cuia de rescate ≤ 16 ranuras de layout). En un segundo bloqueo del mismo nivel solo quedan Marea, Salto del Boto si es elegible y el pie (Volver a la orilla en 1-30; «Rendirse» desde el 31, D-008).
4. **Pororoca revela las capas ocultas** (research/02 Complemento 4.5): la reconstrucción inversa en cliente no puede garantizar el vertido seguro con capas ocultas (dirección 6); revelarlas mantiene la garantía. Luciérnaga sigue siendo la vía barata para una sola cuia.
5. **Llenar canoas con 200 semillas**: fuente de canoas no listada en la dirección 11 (que no es exhaustiva); tras interruptor de Remote Config (`canoes_refill_seeds_enabled`). La alternativa anterior de este capítulo "Canoas infinitas 15 min en el hito 6 de la Crecida" queda **retirada**: la escalera canónica de 05 §2.4 fija el hito 6 en Canasta y la Crecida no otorga Canoas infinitas.
6. **Marea ilimitada en los niveles 1-10** (la dirección 9 fija 3 gratis por nivel sin excepción de tutorial); research/05 §Implicaciones 2 proponía ilimitada hasta el 20. Cerrada como excepción de tutorial de todo el GDD (pregunta abierta 6; 02 §13): `undo_unlimited_until_level` = 10 y 3 por nivel desde el 11, con 1 Cuia extra regalada en el 8 y 3 Morphos en el 12.
7. **Qué bloquea tener 0 canoas** (§7.1): no se inicia ni reinicia un nivel de campaña; la dirección 11 fija cantidad y regeneración, no el efecto. La Cuia de rescate no requiere canoa y el resto del juego sigue abierto.
8. **Dueño de la definición de fallo** (§5.2) y del calendario de ayudas (§4): este capítulo; 02 §2 y §7, 03 §13 y 06 §2.2-4.3 remiten aquí. La promesa "Reiniciar es gratis, siempre" se mantiene literal: el modal nunca atribuye la canoa al reinicio (§6.5).
9. **Embudo de fallo de D-008** (decisión del dueño del 2026-10-08): desde el 31, el pie del modal es «Rendirse» y lleva a ¿Seguro? y a Nivel fallido, y salir del nivel con el menú es fallo duro. La dirección 2 («reiniciar gratis e ilimitado») se mantiene: el reinicio voluntario sigue siendo gratis y cerrar la app nunca es fallo.

## Preguntas abiertas para el dueño del producto

1. **Línea de Hoja escudo en el modal** (§8): ¿se muestra desde 1.0 (honestidad) o se deja apagada hasta medir su efecto en la conversión del rescate?
2. **Contenido de la Balsa de rescate** (4,99): **Cerrada**: Cuia de rescate aplicada al instante + 1.000 semillas + 1 Cuia extra (al stock) + Canoas infinitas 30 min (capítulo 05 §4.1; 09 decisión 16), coherente con la frase del glosario "semillas, una cuia y canoas infinitas". Queda descartada la variante anterior de este capítulo (800 semillas + 3 Marea) y la de research/04 (800 + cuia + 5 deshacer).
3. **Llenar canoas con semillas** (200): ¿se mantiene o las canoas solo se obtienen por tiempo, video, premios y bundles?
4. **Luciérnaga en Aguas turbias · Bonus**: ¿se permite sin tocar el premio doble, o cada uso lo reduce? No bloquea 1.0: la tarjeta llega en 1.1 (`ui_murky_bonus_enabled` = false en 1.0 y true en 1.1, 08 §9); hay que decidirlo antes de 1.1.
5. **Segundo escalón de alivio** al sexto fallo (`relief_second_step_fails`): ¿se activa en soft launch o se espera a ver la tasa de Salto del Boto?
6. **Marea ilimitada hasta el nivel 10** frente a 3 por nivel desde el 1 (desviación-precisión 6): **Cerrada**: Marea ilimitada en 1-10 (`undo_unlimited_until_level` = 10) y 3 por nivel desde el 11; con 1 Cuia extra regalada en el 8 y 3 Morphos en el 12 (excepciones de tutorial a la dirección 9; 02 §13).
7. **Rescate con descuento del primer día en LatAm**: **Cerrada**: un solo precio global de 600 semillas fijas desde el 31 (`rescue_price_bands` = [[31,600]], sin `rescue_base_price`, sin tramos 750/900 ni cohorte de 450 por geografía); el ajuste LatAm va en el precio local de los IAP (05 §4.2). Si el soft launch obliga a bajar, se baja la banda única (§9).
8. **Sin anuncios cruzado Android ↔ iOS**: ¿se honra el derecho comprado en Play dentro de la versión iOS mediante el libro mayor (en AWS desde D-035; práctica común, pendiente de revisión de políticas) o cada tienda vende el suyo?
9. **Cerrada (2026-10-19, D-029). Embudo de D-008**: los textos nuevos y el nombre «Sin canoas» (P14), Marea como primera opción del modal (P15), la canoa al salir con 0 vertidos (P16) y «Canoas infinitas 1 h» siempre en Sin canoas (P17 A, §7.3; 05 §4.1 y §5 alineados), todas con la recomendada (`docs/dev/decisiones.md`).

## Dependencias con otros capítulos

| Capítulo | Este capítulo necesita | Este capítulo entrega |
|---|---|---|
| 00 Glosario | Nombres, IDs, frases de una línea, textos de UI; SKU `af_infinite_canoes_1h` como ID canónico (00 §11) | Claves de texto de §6.5 (`stuck_canoe_lost` / `stuck_canoe_lost_last` ya figuran en la tabla §10 del glosario) y frase canónica de Volver a la orilla, ya adoptada en glosario §5.1: "Vuelve al inicio del nivel. Gratis, siempre, mismo tablero. Desde el nivel 31, quedarte sin vertidos cuesta una canoa" |
| 02 Núcleo y reglas | Función única `futil(A, B)` en `AF.Core` (02 §1.4: anexo B de D-007 con Q-B, con la excepción turbia de §5.1); `legal` de 02 §1.2, con los bloqueos de origen y destino de Hoja, Brote, Liana, Matapi y el Barro en la cima, y la rama del Remolino; máximo de cuias en pantalla (16 = 14 generadas + 2) y layout de 2 o 3 filas (D-006); debuts de mecánicas (lista única de 02 §9.1 / `mech_debut_level`: 14 Cuia chica · 27 Cuia alta · 43 Hoja · 63 Matapi · 83 Liana con fruta · 100 Aguas turbias · 103 Remolino · 123 Barro · 143 Lluvia · 163 Brote · 183 Piraña · 323 Tinaja (M13, D-019: en la 1.0 si cumple su condición; si no, el mismo nivel en la 1.1); la Liana de cuenta (con número) es una variante, en el 243) y flags `skippable`, `hint_supported` por nivel; excepción de 02 §14 para el vídeo que se pide desde la bandeja; Marea a 250 ms y sin animación en el Remolino (02 §14); Cuia alta cap 5 desde el 27 y cap 6 en hitos desde el 141 (02 §8); tabla del tutorial 1-12 (02 §13); pista por inactividad (02 §3.4; claves `hint_idle_from_level` / `hint_idle_delay_s` de 08 §9); Aguas turbias con 4 dosis ocultas / ≤24 realidades (02 T2) | Detección de bloqueo (§5.1), **tabla canónica de fallo** (§5.2, a la que remiten 02 §2 y §7: cerrar la app no es fallo; desde el 31, rendirse y salir del nivel con el menú sí lo son, D-008; Salto del Boto no rompe la Crecida); embudo de D-008 (§6.2) para los estados GIVE_UP_CONFIRM, LEVEL_FAILED y NO_CANOES de 02 §2, regla "Qué bloquea 0 canoas" (§7.1, replicada en 02 §7), tooltip del primer modal del tutorial (02 §13, nivel 6): "Reiniciar es gratis, siempre, mismo tablero. Desde el nivel 31, quedarte sin vertidos cuesta una canoa"; calendario de ayudas (§4: Cuia extra 8 con 1 gratis, Mercado 10, Morpho 12 con 3, Marea ∞ hasta el 10 y 3/3 desde el 11); comportamiento de Marea sobre cada mecánica |
| 03 Niveles, generación y dificultad | Variantes `relief[]` pregeneradas por nivel (lista, 08 §4.1; obligatoria con `minItems` 1 desde el 31 y `[]` antes, `schemas/af-level-1.schema.json`; 03 §13); garantía "≥1 vertido seguro" en Aguas turbias; primer Difícil 40 y primer Muy difícil 60 (03 §11/§12) y el ciclo de D-009 (desde el 61 los 5 retos apuntan a Difícil; en 41-59, solo p6, p12 y p18; la etiqueta la decide la medición); módulo solver único `AF.Solver` (pista, Pororoca, verificación de rescate) | Presupuesto de nodos del solver en cliente, regla `par_after ≤ par_before + 2`, eventos `af_rescue_unsolvable` y `af_hint_result` (08 §8.2) para retirar niveles; uso de la misma `futil` en 03 §5 MOVIMIENTOS con deduplicación de vacíos solo por `(cap, attr)`; umbral de fallo blando ≥8 vertidos (`restart_confirm_min_pours`) para 03 §13 (b), que remite a §5.2 |
| 05 Economía y monetización (semillas) | 20/60/80 por victoria, Canasta, Bitácora, Crecida (escalera 3 → 300 · 6 → Canasta · 10 → 1 Cuia extra + río crecido, una vez al día, tope 600/día: 05 §2.4); ingreso efectivo por victoria de la simulación (05 §3.2: 63 casual / 98 medio) | Precios de todos los sumideros, topes por nivel y día, ratio de presión por perfil (§9); `rescue_price_bands` = [[31,600]] como clave única del precio (§6.2); `rescue_max_per_level` = 1 (cerrado: 05, 08 §9 y este capítulo coinciden) |
| 05 Economía y monetización (anuncios e IAP) | SKUs `af_coins_2`, `af_rescue`, `af_infinite_canoes_1h` (ID canónico de 05 §4.1 y 00 §11, inmutable en Play Console), precios locales (el ajuste LatAm va en el precio local de los IAP, no en el precio en semillas del rescate), mediación MAX, SSV, política de intersticiales | Ubicaciones recompensadas con topes y cooldowns (§3; claves `ads_rv_*` de 08 §9), orden de upsell por geo (§6.3), contenido de la Balsa de rescate (cerrado: 05 §4.1) |
| 06 Meta, retención y live-ops | Reglas completas de Crecida y Hoja escudo (06 §4.3 con los valores de 05 §2.4), Bitácora, Canasta (ciclo fijo `eco_basket_cycle`, 06 §4.4), Reto del río (sin canoas); Crecida + Hoja escudo en el nivel 25 (06 §3) | Interacción racha ↔ rescate (§8), Canoas infinitas como premio de eventos (§7.1), definición de fallo para la Crecida (§5.2, con la que 06 ya cerró su pregunta abierta 1), calendario de ayudas de §4 (06 §2.2 remite a 02 §13: "Morpho 3 usos", "Mercado desde el 10", "Marea ∞ hasta el 10") |
| 07 Arte, temática y audio | Orden canónico de la bandeja de comodines (07 §7.4, adoptado en §2 sin posiciones propias); iconos de los comodines, Capi y Rosa, tapa de calabaza, agua parda de Aguas turbias; banco de sonidos (maracá, splash, exhalación de boto) | Especificación de VFX/SFX por comodín (§2) y wireframes del modal y de Sin canoas (§6.6, §7.3) |
| 08 Arquitectura técnica y publicación | Play Games Saved Games, Firebase Auth anónimo, las 5 funciones del backend en AWS (`rewardedSsv`, `verifyPurchase`, `restoreLedger`…, 08 §2.4; D-035), tabla única de Remote Config (08 §9), esquema de nivel `af-level/1` (08 §4.1, `schemas/af-level-1.schema.json`) y taxonomía única de analítica (08 §8.1-§8.2) | Esquema del blob de inventario (§10.1), reglas de fusión (§10.2), parámetros específicos de los eventos que 08 §8.2 delega en §11 (`af_booster_used` / `af_booster_denied`, `af_hint_result`, `af_shuffle_result`, `af_stuck_*`, `af_rescue_unsolvable`, `af_skip_used` y eventos de Canoas), comportamiento que justifica las claves `undo_*`, `extra_cuia_*`, `hint_*`, `shuffle_*`, `reveal_*`, `skip_*`, `restart_*`, `rescue_*`, `stuck_*`, `af_rescue_*`, `canoes_*` (sin defaults propios) |
| 08 §13 Checklists de publicación · 01 Visión §9 (ASO y legal) | Confirmación de que ningún comodín ni cofre es "ítem aleatorio de pago" (ECA Digital art. 20); textos legales de las compras | Catálogo de comodines sin aleatoriedad de pago; Canasta y Bitácora con contenido visible |
