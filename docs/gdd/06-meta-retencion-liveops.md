# Capítulo 06 · Meta, retención y live-ops

Fecha: 2026-10-06 (revisión de incidencias de GDD.md §7; actualizado el 2026-10-08 con la decisión del dueño **D-008**, bucle de partida como Magic Sort: §4.3, §4.7 y «Preguntas abiertas» 1; registro en `docs/dev/decisiones.md`; actualizado el 2026-10-11 con **D-007**, mecánicas del río, y **D-009**, dificultad escalonada, decididas por el dueño el 2026-10-08: §3, §4.7, §5.2, §5.3, «Preguntas abiertas» 11 y Dependencias; actualizado el 2026-10-12 (paso 2 del traspaso) con **D-011** (Cosecha doble que avanza sola: §4.7) y **D-012** (banner de mundo al cargar el 11, 41, 81 y 141: Resumen, §2.2, §2.3 y §4.5), con las preguntas 28 y 35 de GDD.md §6.3 en §5.2 y la 39 (sitio de la oferta del pack de skin) en §4.5, y con la lista única de estrenos de 02 §9.1: §3 (fila 43), §5.3 y Dependencias; actualizado el 2026-10-13 (tanda 3) con **D-013**, el Mapa del río (TiTino como protagonista, con T1-A y T6-A de su propuesta y la Q1-A de Frutas del río): §2.2, §2.3 (el primer Mapa en el 10), §4.4 (el Dato del río en la victoria, con la pregunta 45 de GDD.md §6.3), §4.5 (reescrita: el Mapa del río de 100 paradas), §4.6 (la página Ríos es el Muestrario), §4.7, §8.1, §8.3 (la voz de los avisos es TiTino), §9, §10 (fila 0: fondos de la 1.0), «Parámetros» y Dependencias, y **D-015**, la pregunta 39 decidida: (a), el banner de mundo sin precio y la oferta del pack de skin en el Mercado y en una tarjeta propia fuera del nivel y del Mapa (§4.5); actualizado el 2026-10-14 (tanda 3, T54) con **D-016**, TiTino (§2.2: en el arranque, sin palabras, y en el tutorial de los niveles 1-3, 6 y 12; Capi en el 8), y **D-017**, Frutas del río (§4.4: la línea de fruta desde el nivel 1; §4.5: la frase de «Fruta nueva»; §4.6: la página Frutas del Cuaderno con las fichas k/20), y **D-020**, las preguntas 28 y 35 (§5.2); actualizado el 2026-10-14 (T58) con **D-022**, la pregunta 45 (§2.3, paso 3: la línea 10 del açaí, en la ficha; §4.4: canales separados y una línea por victoria; Parámetros: `ui_fruit_line_every_n_levels`) y un retoque de la revisión de T54 (§3, fila 43; §5.3; Dependencias, fila 02: el 323 de la Tinaja en la lista única de estrenos, contrato 1, D-019, Q-Ti3-A); actualizado el 2026-10-19 (DC1) con **D-028**, las respuestas del dueño a las preguntas 19, 22-24, 53, 54, 57, 60, 61 (con P5) y 65 de GDD.md §6.3 (§2.2: P5 y el 12 sin dedo guía, 61; §2.3: el bocadillo del Mapa del 10, 60; §4.5: la fuente en la tarjeta, 57)) · Proyecto: Amazon Fill (nombre comercial **Tintalú**) · Nombres según `docs/gdd/00-glosario.md`. Convención de evidencia: **[V]** verificado en el informe citado, **[E]** estimación del informe, **[R]** reseñas de jugadores, **[NV]** no verificado, **[P]** propuesta de este capítulo a calibrar en soft launch. "(dirección N)" remite a las decisiones de dirección vinculantes.

## Resumen

La primera sesión lleva al jugador del nivel 1 al 10 en 10-14 minutos (tabla canónica de 02 §13) sin un solo anuncio ni oferta, enseña una cosa por nivel (vertido, capacidad, Marea, reserva de vacío, Volver a la orilla, Cuia extra) y termina con cuatro razones concretas para volver mañana: Canasta, Bitácora del río, Reto del río y el mundo 2 a la vista en el Mapa del río (su banner sale al cargar el nivel 11; D-012). La capa de retención de v1.0 son cuatro sistemas sin servidor (Reto del río con pool fechado, Bitácora de 30 casillas sin reinicio, Crecida con Hoja escudo, Mapa del río con skins por hito) y un calendario de desbloqueos que copia los hitos probados de Magic Sort (intersticial 22, bono diario y liga en la primera hora) adelantando lo que retiene y retrasando lo que cobra (canoas y Cuia de rescate al 31). v1.2 añade los eventos del glosario con una regla única de diseño justo (escudo, ninguna expiración por reloj, premio total mayor que una Cuia de rescate, nada de bots ni rachas que caducan de noche) y la Liga del río sobre Unity Leaderboards (en la configuración de equipo A de 09 §3.2, Regata, Encuentro de las Aguas y Río sin fin pasan a 1.3); v2.0 añade Aldeas con canoas compartidas y mensajes predefinidos, único punto que exige backend propio. Todo lo que da progreso se calcula en cliente y se reconcilia después; todo lo que da valor económico se valida en servidor.

## 1. Qué mueve la retención en este género (base de evidencia)

| Dato | Valor | Fuente |
|---|---|---|
| Retención puzzle (benchmark) | D1 31,85 % · D7 12,18 % · D30 5,35 % | research/05 §1 [V] |
| Metas de soft launch | D1 ≥30 % · D7 ≥10 % · D30 ≥4 % | dirección 19 |
| Sesión mediana / sesiones por día | 5-6 min / ~4 (Android > iOS); Magic Sort 4,8 sesiones/día | research/05 §1 [V]; research/09 §Ficha [V] |
| Adopción de mecánicas live-ops en casual 2025 | Colecciones 75 % · carreras 68 % · álbumes 66 % · win streak 48 % (+50 % interanual) | research/05 §3 [V] |
| Único lift duro publicado | Racha con apuesta: +14 % D7; amuleto de racha: −5 % de pérdida de racha (Duolingo) | research/05 §3 [V] |
| Hitos de Magic Sort | Intersticial L22 · primer fallo L19 · easing L26 · bono diario L34 · liga L39 · carrera L80 · evento largo L119 · evento nuevo cada ~2 meses · +100 niveles/semana | research/09 §Meta y live-ops, §Onboarding [V] |
| Debilidad admitida del líder | Live-ops "sub-optimizados" (CEO, jun-2026); todos sus eventos castigan el fallo; rachas que expiran de noche; premio de racha (300) < continue (900) | research/magic-sort/c §1, §2.1 [V]; research/09 §Qué copiar pero mejorar [V] |
| Push | Opt-in juegos hypercasual: Android 69 % / iOS 23 %; CTR 0,8-1,05 %; personalización básica +9 % CTR | research/05 §5 [V] |

Lectura que ordena este capítulo: D1 depende del núcleo y de la primera sesión; D7-D30 dependen de reto diario, calendario, racha y colección (research/05 §Implicaciones 1 [E]). Magic Sort retiene sin meta decorativo (Naavik lo describe como meta "ligero, para motivar el leaderboard", research/09 [V]); nuestra ventaja no es tener más sistemas, sino sistemas justos (research/09 §Por qué ganó 8 [V]).

## 2. Primera sesión minuto a minuto (niveles 1-12)

### 2.1 Reglas duras de la primera sesión

1. Cero intersticiales hasta el nivel 22 (dirección 13); cero ofertas de pago hasta la Mochila de inicio (ventana 10-20, dirección 14), cuya primera tarjeta se muestra en la **primera victoria con nivel ≥12 dentro de una sesión ≥2** (`eco_starter_first_show` = "L12_or_session2", 08 §9; mismo canon en 02 §13 y 05 §4.1): nunca en la primera sesión, siempre dentro de la ventana 10-20.
2. Primer anuncio recompensado voluntario: Cosecha doble en la victoria del nivel 11 (dirección 13).
3. Sin login visible, sin permiso de push y sin pantallas de texto en el arranque. Única pantalla previa al nivel 1: edad neutral + consentimiento (analítica / anuncios personalizados / diagnóstico, con "Rechazar" al mismo nivel que "Aceptar"; UMP en EEE/UK/CH, pantalla propia en BR/CO/PE/MX), ≤10 s en total (textos y bases legales en 05 §7 y en las checklists de publicación de 08 §13: no existe un capítulo "ASO y legal"; flujo de arranque en 08 §2.3; research/08 C5.4 [V]). Firebase Analytics permanece apagado hasta ese consentimiento (08 §2.3): por eso se pide antes del nivel 1 y no antes del primer anuncio, ya que el embudo de los niveles 1-10 (sección 2.4) debe poder medirse. MAX se inicializa después con los flags de consentimiento y edad; el primer anuncio (nivel 11) no abre ninguna pantalla adicional.
4. Aritmética fija: **cuias = colores + vacíos**. Dirección 4: 2 vacíos en 1-30 con excepciones tutoriales; las excepciones son los niveles **1 y 6** (1 vacío), según la tabla canónica de 02 §13, fuente única de los niveles 1-12 construidos a mano; la tabla 2.2 copia sin alterar sus columnas de cuias, colores, vacíos, par y tiempo objetivo y solo les añade los desbloqueos de meta. Colores por mundo según el canon de research/02 C3.3: mundo 1 (1-10) K 2→5 con debuts açaí y aguaje (1), taperebá (2), guanábana (4), bacaba (7); mundo 2 (11-40) K 6-7 con agua del Tapajós (11) y camu camu (21) [E]. `max_pares_riesgo = 0` en todo el tramo (dirección 3).
5. Nota sobre research/05 §Diseño de la primera sesión: su tabla mezcla filas incompatibles con la aritmética y con el canon (nivel 7 "6 recipientes, 5 colores, 2 vacíos" suma 7; nivel 10 "7 colores" supera el pool de 5 del mundo 1). La tabla siguiente toma la aritmética recalculada de 02 §13 y le añade los desbloqueos de meta; el orden de enseñanza de research/05 se conserva.
6. Tiempos [P]: la columna "Tiempo objetivo" de 02 §13 copiada sin alterar (30 s en el nivel 1 → 120 s en el 10; 02 la deriva de las guías de Magic Sort, 15 s-1,5 min en N1-5 y 2-6 min en N6-10, research/09 §Onboarding [V], recortada porque jugamos con 2 vacíos y par ≤18 en todo el tramo); **objetivo de la sesión 1-10: 10-14 min** incluida la secuencia del hito (02 §13; research/05 proponía 8-10 [E]). La sesión mediana del género es de 5-6 min [V], así que un 30-40 % de los jugadores cerrará la primera sesión entre los niveles 5 y 8: por eso cada nivel enseña una sola cosa y nada esencial (Bitácora, Reto) está después del 10.

### 2.2 Tabla minuto a minuto

| Min (acum.) | Nivel | Colores (debut) | Vacíos | Cuias | Par [P] | Fallo bot ×40 [P] | Qué aprende | Qué ve / qué se desbloquea | Semillas acumuladas |
|---|---|---|---|---|---|---|---|---|---|
| 0:00-0:25 | — | — | — | — | — | — | — | Splash de 1 pantalla (río + logotipo) con **TiTino colgado de su embaúba**, que despierta sin palabras (`sloth_wake`; D-016, T2-A): solo en la primera apertura, visible al menos 1,0 s [P] si el primer toque en el nivel 1 sigue por debajo de 20 s (09 §6); en las demás, `sloth_idle` hasta que termina la carga. Esa pantalla aún no existe en el código. Después, pantalla de edad neutral + consentimiento (regla 3) y carga del nivel 1 con Llenar y teñir (1,2-1,5 s, saltable; dirección 18), que sigue siendo de los miquitos | 0 |
| 0:25-0:55 | 1 | 2 (açaí, aguaje) | 1* | 3 | 3 | 0 % | Tap origen → tap destino; una cuia completa se sella (tapa de calabaza, maracá, háptico); "termina la cuia casi llena antes de apartar" | Dedo guía en los dos primeros toques, una ayuda explícita compatible con D-002 (P5 de `docs/dev/decisiones.md`, respondida el 2026-10-19: sí; D-028), con el retrato de TiTino junto a la instrucción (D-016; 02 §13); HUD solo con "Nivel 1"; Marea ya está en la barra, sin contador; al ganar aparece el contador de semillas con +20 | 20 |
| 0:55-1:40 | 2 | 3 (+taperebá) | 2 | 5 | 4-6 | 0-5 % | Vertido parcial con dedo guía (una racha de 3 sobre un hueco de 2; D-005; P5, sí: D-028); dos vacíos por primera vez: "reserva uno". Lección y par: 02 §13 | Tarjeta "Fruta nueva: Taperebá" (2 s, icono + patrón); confeti de guacamayo | 40 |
| 1:40-2:40 | 3 | 3 | 2 | 5 | 5-7 | 5-10 % | Deshacer | Tutorial de **Marea** la primera vez que un vertido aleja de la solución (distancia del solver +2) o al inicio del nivel 5 si no ocurrió; tooltip "Marea · Deshacer" y contador oculto "∞" (Marea ilimitada hasta el nivel 10, `undo_unlimited_until_level` = 10, 04 §2.1; desde el 11, 3 gratis por nivel con contador "3/3") | 60 |
| 2:40-3:55 | 4 | 4 (+guanábana) | 2 | 6 | 7-9 | 5-10 % | Cuatro colores; planificar dos pasos (liberar una cuia para recibir) | Fruta nueva: Guanábana; primera textura de patrón visible (blanco liso) | 80 |
| 3:55-5:10 | 5 | 4 | 2 | 6 | 9-11 | 5-10 % | Reunir un "color problema" repartido en 3 cuias (patrón de Magic Sort N5, research/09 [V]) | — | 100 |
| 5:10-6:40 | 6 | 4 | 1* | 5 | 10-13 | 5-10 % | Excepción tutorial: un solo vacío. Planificar 3 pasos sin dedo guía; reservar la cuia vacía hasta el final (Magic Sort lo enseña en L3, research/magic-sort/a §3 [V]) | Una frase de TiTino al inicio, con su retrato (D-016, T5-B; antes, Capi): "Guarda una cuia vacía: es tu espacio para pensar". Si entra en Aguas estancadas, primer modal con Marea (ilimitada) y Volver a la orilla; tooltip "Reiniciar es gratis, siempre, mismo tablero. Desde el nivel 31, quedarte sin vertidos cuesta una canoa" (texto canon de 04 §2.7) | 120 |
| 6:40-8:10 | 7 | 5 (+bacaba) | 2 | 7 | 10-13 | 5-10 % | Cinco colores (pool del mundo completo) | Fruta nueva: Bacaba. Tras ganar, presentación del Reto del río (tarjeta de 2 s: "Desde el nivel 10, un tablero nuevo cada día"; 02 §13); se desbloquea en el 10 | 140 |
| 8:10-9:50 | 8 | 5 | 2 | 7 | 12-15 | 5-10 % | Reiniciar sin castigo; qué es Aguas estancadas; Cuia extra como comodín planificado | Primer nivel con reinicio probable con 2 vacíos (02 §13): enseñanza de Volver a la orilla con el mismo tooltip del nivel 6 (Marea sigue ilimitada). Capi entra con una cuia en la cabeza y regala **1 Cuia extra al inventario** (04 §2.2, `extra_cuia_unlock_level` = 8; sin precio ni anuncio en el tutorial): "La próxima la encuentras en tu Canasta"; dedo guía en su primer uso; aparece el botón "+1 cuia" (150 semillas desde el 11) | 160 |
| 9:50-11:30 | 9 | 5 | 2 | 7 | 13-16 | ≤10 % | Consolidar | Variante de luz del fondo Estuario (alba → mañana; LUT, sin arte nuevo; 02 §13; research/02 C3.4 [E]) | 180 |
| 11:30-14:00 | 10 | 5 | 2 | 7 | 14-18 | ≤10 % | Hito de mundo; último nivel con Marea ilimitada | Secuencia de la sección 2.3 (cuatro pantallas, ≤30 s): Victoria; Canasta (50) + Bitácora día 1 (50) en una sola pantalla; Mapa del río con el Dato del río 1 (el banner del mundo 2 sale al cargar el 11; D-012); lobby con Reto del río desbloqueado y Mercado accesible (desde el 10: 01 §7.1, 05 §2.1). Sin pre-prompt de push (pasa a la 2.ª sesión, sección 8.1) | 300 |
| 14:00-16:00 | 11 | 6 (+agua del Tapajós) | 2 | 8 | 15-19 | 10-20 % | Un tinte puede ser "agua" (turquesa, patrón de líneas onduladas); el contador de Marea aparece en "3/3" (3 gratis por nivel, dirección 9) | Antes de la intro, banner "Mundo 2: Tapajós · Brasil (Pará) · Tapajós" (`world_banner`), que presenta Capi mientras Rosa asoma y se sumerge (D-012; agua base clara); Fruta nueva: Agua del Tapajós; en la victoria, botón **Cosecha doble** ("Ver video · x2") presentado una sola vez con un tooltip sin personaje [P]: con TiTino en la victoria, ninguna mascota presenta ni señala una oferta (D-013, T1-A); MAX ya inicializado con los flags del consentimiento del arranque (regla 3), sin pantalla previa | 320 (340 con x2) |
| 16:00-18:00 | 12 | 6 | 2 | 8 | 16-20 | 10-20 % | Pista | Aparece **Morpho**: TiTino presenta el botón con su retrato (D-016; antes, Capi): "¿Atascado? La morpho conoce el río", y se acreditan **3 Morphos al inventario** (04 §2.3, `hint_unlock_level` = 12; la primera, sin dedo guía: la presentación de TiTino con la cola hace de guía; pregunta 61 de GDD.md §6.3, (a), D-028); después 100 semillas o anuncio. Victoria: primera tarjeta de **Mochila de inicio** solo si esta es ya una sesión ≥2 (`eco_starter_first_show` = "L12_or_session2"; si no, en la primera victoria de la 2.ª sesión) | 340 (360 con x2) |

\* Excepción tutorial con 1 vacío (dirección 4): niveles 1 y 6, según 02 §13 (el 1 porque con dos colores un segundo vacío lo vuelve trivial; el 6 para vivir una vez, con ayuda a mano, la restricción que será la norma desde el 31). Las columnas Colores, Vacíos, Cuias y Par y los tiempos por nivel están copiados sin alterar de 02 §13 (ver 02 §13 para el detalle de enseñanza por nivel). Bot casual ×40 según research/01 §6 [V]; los valores por nivel son objetivo de generación [P] dentro del 0-10 % que research/02 §7.2 fija para 1-10 [E]. Pares de riesgo: ninguno (los 7 primeros tintes del canon son mutuamente seguros, research/02 C3.2 [E]).

Con 340-360 semillas al nivel 12, el jugador puede pagar dos Cuias extra (150) o tres Morpho (100) antes de la primera dificultad real (1 vacío como palanca desde ~31; primer Difícil en el 40 y primer Muy difícil en el 60, 03 §12 [E]); el saldo positivo hasta el 100 es deliberado (research/04 C1 §5 [E]).

### 2.3 Secuencia de la victoria del nivel 10 (máximo cuatro pantallas, cada una se cierra con un toque)

| Orden | Pantalla | Duración | Contenido |
|---|---|---|---|
| 1 | Victoria | 2 s + toque | "¡Nivel completado!", +20 semillas, TiTino celebra si cabe (`sloth_cheer`; 02 §6; D-013, T1-A). Como el 10 lleva el Mapa del río en esta secuencia, TiTino solo hace el gesto en la victoria y se presenta en el Mapa |
| 2 | Canasta + Bitácora del río | hasta toque (≈8 s) | Mitad superior: cesta de fibra con el contenido **visible antes de abrir** (50 semillas); se abre con un toque (dirección 15). Mitad inferior: libreta abierta en la página 1 de 30; el jugador toca la casilla 1 (50 semillas) y la sella; texto: "Cada día que juegues, sella una página". Un solo botón "Seguir" |
| 3 | Mapa del río (con Dato del río 1) | hasta toque (≤10 s) | Primer Mapa (D-013, 2026-10-13; sección 4.5): el estuario ya está lleno, con su agua blanca; **TiTino se presenta** desde su casa del estuario, en un bocadillo («Yo vivo aquí, donde el río entra al mar.» [P], `map_l10_intro`; no cuenta en el tope de frases por sesión, `ui_sloth_lines_per_session_max`, porque es su presentación, una vez en la vida, pero sí en «nunca en dos pantallas seguidas» de D-016, punto 5: el saludo del inicio espera a la visita siguiente; pregunta 60 de GDD.md §6.3, decidida el 2026-10-19: (b); D-028), y da el **Dato del río 1** en la placa del Dato del río con su retrato (el arrecife que hay bajo el agua turbia de la desembocadura [V], `map.stop.estuario.dato_03`; fuente y asesor cultural; research/07 §Implicaciones 8); la canoa de Capi, con Rosa nadando delante, sube de Belém a Santarém y el Xingu se llena al pasar su boca; se ilumina la tarjeta **Reto del río** ("Hoy: Reto del río · Jugar"). La línea 10 de la serie del açaí (Frutas del río, Q1-A) no va en el bocadillo: queda revelada en su ficha (pregunta 45 de GDD.md §6.3, decidida el 2026-10-14: (a); D-022). El banner del mundo 2 (`world_banner`) no va aquí: sale al cargar el 11, antes de la intro (D-012, 2026-10-11) |
| 4 | Lobby | — | Botón grande "Jugar nivel 11"; tarjetas: Reto del río (nuevo), Bitácora (sellada hoy), Mercado, Mapa del río, Ajustes |

Regla: máximo **un** takeover por sesión fuera de esta secuencia de hito (research/magic-sort/c §Qué evitar [V]). Aquí se encadenan tres pantallas antes del lobby porque son el premio del primer mundo, pero cada una es de un toque y la suma no debe pasar de 30 s. El pre-prompt de push **no** va aquí: se muestra tras la primera victoria de la 2.ª sesión con el nivel 10 ya superado (`push_preprompt_session` = 2 y `push_preprompt_min_level` = 10, 08 §9; sección 8.1; canon para 02, 03 y 09), para no apilar una decisión de permiso sobre el hito y para que el pedido llegue a quien ya demostró volver.

### 2.4 Qué se mide en la primera sesión

Por nivel: abandono (jugadores que inician y no terminan ni vuelven en 24 h), reinicios, Mareas usadas, tiempo hasta victoria, Aguas estancadas alcanzadas. Alarmas [E, research/05 §Diseño de la primera sesión]: cualquier nivel 1-10 que pierda >5 % de los jugadores que lo inician; tiempo a primera interacción >20 s; aceptación del pre-prompt de push (2.ª sesión, sección 8.1) <50 % en Android (benchmark de opt-in 69 %, research/05 §5 [V]). Objetivo de sesión 1 [P]: ≥70 % de los instaladores terminan el nivel 5; ≥50 % terminan el nivel 10; ≥35 % vuelven el día 1 (meta D1 ≥30 %, dirección 19).

## 3. Calendario de desbloqueos por nivel (comparado con Magic Sort)

| Nivel | Qué se desbloquea en Tintalú | Versión | Magic Sort (referencia) | Estado de la referencia |
|---|---|---|---|---|
| 1 | Volver a la orilla (visible en el HUD, gratis, determinista) | 1.0 | Reiniciar existe; rechazar el continue = −1 vida y reinicio con tablero distinto | research/09 §Núcleo [V] |
| 3 | Marea (ilimitada en 1-10, `undo_unlimited_until_level` = 10; desde el 11, 3 gratis por nivel, `undo_free_per_level`; recarga por anuncio o 30 semillas; 04 §2.1) | 1.0 | Undo desde el inicio ("Wizard's Gear"), de pago, inutilizable en el bloqueo | research/09 §Comodines [V]/[NV] |
| 8 | Volver a la orilla enseñado (primer nivel con reinicio probable, 02 §13) · Cuia extra (1 regalada al inventario, `extra_cuia_unlock_level` = 8; 150 semillas desde el 11; 1 por nivel; 04 §2.2) | 1.0 | "+1 Bottle" pre-nivel por 900 monedas desde el inicio | research/09 §Comodines [V] |
| 10 | Canasta · Dato del río · Bitácora del río · Reto del río (presentado en el 7) · Mapa del río · Mercado (cuatro pantallas, sección 2.3) | 1.0 | Bono diario L34; daily puzzle sin nivel conocido | research/magic-sort/b §5.1 [V]; research/09 [NV] |
| 11 | Cosecha doble (primer anuncio recompensado, voluntario; MAX ya inicializado con el consentimiento del arranque, regla 2.1.3) | 1.0 | Un solo placement rewarded (vida extra) | research/09 §Anuncios [V] |
| 12 | Morpho (3 regalados, 04 §2.3; 100 semillas o anuncio) | 1.0 | Hint/Shuffle "herramienta barata" ~300 monedas | research/09 §Comodines [NV] |
| 2.ª sesión (nivel ≥10) | Pre-prompt de push tras la primera victoria de la 2.ª sesión con el nivel 10 ya superado (`push_preprompt_session` = 2, `push_preprompt_min_level` = 10, 08 §9; sección 8.1) | 1.0 | Momento del permiso de push no documentado | research/09 [NV] |
| 12 → 20 | Mochila de inicio 0,99 (una vez; primera tarjeta en la primera victoria con nivel ≥12 dentro de una sesión ≥2, `eco_starter_first_show` = "L12_or_session2"; se oculta tras el 20 o la compra) | 1.0 | Starter Bundle 0,99; nivel exacto desconocido, ventana 10-20 estimada | research/09 §Catálogo IAP [V]/[E] |
| 14 | Cuia chica (2 u. en el debut; de 1 u. en el 18 y de 3 u. en el 22; 02 §8) | 1.0 | Frasco de 1 unidad en L14 | research/magic-sort/a §3 [V] |
| 20 | Cántaro (alcancía 2,99, determinista, tope visible) · primera Canasta con comodín | 1.0 | Sin alcancía | research/09 [V] |
| 22 | Primer intersticial (solo tras victoria; ≥150 s; cada 2-3 niveles; tope 6/sesión) · **Sin anuncios** visible en el Mercado; tarjeta tras el 3.er intersticial visto o en D2 | 1.0 | Primer intersticial L22 (L24 en otra cohorte); sin SKU "sin anuncios" | research/09 §Anuncios, §Catálogo [V] |
| 25 | **Crecida** (racha de victorias) + **Hoja escudo** (`streak_unlock_level` = 25) | 1.0 | Broom Streak antes de L60; sin escudo; expira de noche | research/magic-sort/c §2.1 [V]/[E] |
| 27 | Cuia alta (5 u.; la de 6 u. solo en niveles hito desde el 141, primero el 160; 02 §8) | 1.0 | — | — |
| 30 | Pororoca (barajar; ~300 semillas; limitado) | 1.0 | Shuffle desde el inicio | research/09 [V] |
| 31 | **Canoas** (5, 25 min) · **Cuia de rescate** (600 fijo, `rescue_price_bands` = [[31,600]]; 1.er rescate del día 300; máximo 1 por nivel; 05) · **Balsa de rescate** 4,99 (1/día) · **Salto del Boto** (tras 5 fallos, `skip_min_fails`) · **Canoa extra** por anuncio · tutorial de Capi de una frase (`rescue_min_level` = 31) | 1.0 | Vidas y continue 900 desde el inicio | research/09 [V] |
| 40 | Primer nivel **Difícil** (rana dardo, marco ámbar, 60 semillas; `lvl_labels_from_level` = 40, 03 §12; primer **Muy difícil** en el 60, `lvl_very_hard_from_level`), con la etiqueta antes de jugar: chip y cinta «×3» en Jugar y en Siguiente, y tarjeta previa de 1 toque (D-009; 02 §10) · fin del mundo 2: skin Cuia de Santarém | 1.0 | Último nivel sin fallos L18, primer pico de dificultad L19 | research/09 §Onboarding [V] |
| 41 | Patrones de fruta activado por defecto · banner Manaos (agua negra) · **Liga del río** · **Travesía** · **Río sin fin** | 1.0 (patrones) · 1.2 (Liga, Travesía) · Río sin fin 1.2 en B / 1.3 en A (09 §3.2) | Leaderboards (Weekly Cup, país, global) en L39 | research/magic-sort/b §5.1 [V] |
| 43 | Primer bloqueador nuevo: **Hoja** (02 §9.1 y `mech_debut_level`; dirección 8); después, los de la lista única de 02 §9.1 (D-007): 63 Matapi · 83 Liana con fruta · 103 Remolino · 123 Barro · 143 Lluvia · 163 Brote · 183 Piraña · 323 Tinaja (M13, D-019: en la 1.0 si cumple su condición; si no, el mismo nivel en la 1.1), con Aguas turbias en el hito 100; con la 1.1, 223 Aguapé · 263 Palizada · 303 Lancha · 583 Cocha | 1.0 (1.1 las cuatro últimas) | Shell bottles L25; primer bloqueador realmente nuevo (corchos) L221 | research/09 §Mecánicas [V] |
| 51 | **Regata** (carrera de 48 h contra ≤20 jugadores) | 1.2 (B) / 1.3 (A) | Jetpack Race en L80 | research/magic-sort/b §5.1 [V] |
| 60 | Pase Expedición 6,99 (14 días; nivel ≥60, 05 §5) | 1.2 | Sky Jump etiquetado como "pase estacional" en L119 | research/magic-sort/b §5.1 [V] |
| 61 | **Encuentro de las Aguas** (requiere los dos tintes de agua; agua del Río Negro debuta en 61) | 1.2 (B) / 1.3 (A) | — | — |
| 100 | **Aguas turbias** solo en Difícil/Muy difícil (`lvl_murky_from_level` = 100; el hito 100 la estrena sin otra mecánica y con 1 cuia de 2 dosis ocultas, D-007, Q9) · **Luciérnaga** · tarjeta **Aguas turbias · Bonus** en el Reto del río | 1.0 (modo) · 1.1 (bonus) | Frasco "?" en ~L70 (guías) / muro percibido ~L100 (reseñas) | research/09 §Mecánicas [V]/[R] |
| 11+ | **Temporada de lluvias** (4 días, fechas por Remote Config) | 1.2 | — | — |
| cualquiera | **Cuaderno de campo** (retroactivo con lo ya descubierto) · logros de Play Games | 1.1 | Cartas coleccionables desde cofres (loot boxes) | research/09 §Social [V] |
| 31+ | **Aldea** (20-30 jugadores, Prestar una canoa, mensajes predefinidos) · **Río de la aldea** | 2.0 | Equipos con vidas compartidas y chat (13+); nivel de desbloqueo desconocido | research/09 §Social [V]/[NV] |

Lectura: respecto a Magic Sort adelantamos lo que retiene (bono diario 34 → 10; liga 39 → 41 pero con Crecida en 25; carrera 80 → 51) y retrasamos lo que cobra (vidas y continue desde el inicio → 31; primera oferta en la 2.ª sesión). Los niveles de 1.2 y 2.0 son [P] y se revisan con el embudo del soft launch; la asignación 1.2 / 1.3 depende de la configuración de equipo (B: 2 programadores, alcance completo; A: 1 programador, Regata, Encuentro de las Aguas y Río sin fin pasan a 1.3; 09 §3.2).

## 4. Sistemas meta de v1.0

### 4.1 Reto del río (`daily_challenge`)

**Reglas.**

| Regla | Valor |
|---|---|
| Disponibilidad | Un tablero por día local desde el nivel 10; jugable offline (pool embebido); sin canoas, sin Cuia de rescate, sin efecto sobre la Crecida [P] |
| Origen del tablero | Pool offline pregenerado de 730 tableros **por banda** (3 bandas → 2.190 tableros validados contra `schemas/af-level-1.schema.json` con `kind: daily`, ≈1,4 MB minificados / ≈0,1 MB comprimidos; 03 §16, 08 §4.1 y §6.1) indexado por fecha (dirección 17; research/02 C4.2 [E], patrón Wordle [V]) |
| Bandas (canon 03 §16, tramos A/B/C; umbrales `lvl_daily_bracket_levels` = [41, 201]) | B1: nivel máximo <41 → K 6-7, 2 vacíos, ventana de bot 40-60 % · B2: 41-200 → K 8-9, 1 vacío, 60-80 % · B3: 201+ → K 10-11, 1 vacío, ≥75 %. Sin mecánicas con disparador ni Aguas turbias en el reto obligatorio; la variante turbia solo existe en la tarjeta Bonus (1.1). El premio es igual en las tres bandas |
| Reintentos | Ilimitados, deterministas (misma semilla), Marea y Cuia extra permitidas, Morpho permitida |
| Premio [P] | 60 semillas (`live_daily_challenge_reward` = {"seeds":60,"murky_bonus_mult":2}, 08 §9; 3 victorias normales; research/02 C4.2 propone "monedas de 2-3 niveles" [E]) + sello del día en el calendario del Reto; Cosecha doble disponible |
| Sellos del mes [P] | 20 sellos en un mes calendario → marco de fondo del país del mes (cosmético); 7 Retos en días consecutivos → 1 Pororoca (research/02 C4.2 [E]) |
| Aguas turbias · Bonus (1.1) | Desde el nivel 100 la tarjeta del Reto ofrece una segunda versión del mismo tablero con 2 cuias turbias verificadas (mismo valor que 03 §16) y premio doble (120, `murky_bonus_mult` 2); opcional, nunca sustituye al Reto normal (dirección 6). Interruptor `ui_murky_bonus_enabled` (08 §9): apagado en 1.0, encendido en 1.1 |
| Sustituciones | `daily_overrides` en Remote Config (fiesta nacional, evento): JSON `{fecha: id_tablero}`; cabe un año en un parámetro (research/02 C4.2 [V]) |
| Anti-reloj | Tablero siempre jugable; premio solo si `fecha_local > ultima_fecha_reclamada`; con red, si `\|fecha_local − fecha_servidor\| > 1 día`, premio "pendiente de validar" hasta reconectar (research/06 C6.1 [E]) |

**Pseudocódigo.**

```
function reto_de_hoy(jugador):
    banda = 1 if jugador.nivel_max < 41 else 2 if jugador.nivel_max < 201 else 3   # rc.lvl_daily_bracket_levels = [41, 201]
    fecha = fecha_local_dispositivo()
    if rc.daily_overrides[fecha] existe: return pool_overrides[rc.daily_overrides[fecha]]
    idx = (dias_desde(2026-01-01, fecha) + rc.live_daily_offset) mod 730
    return pool_diario[banda][idx]          # tablero con semilla y estado explícito en JSON

function reclamar_reto(jugador, fecha):
    if fecha <= jugador.ultima_fecha_reclamada: return SIN_PREMIO   # reloj atrasado o ya reclamado
    estado = PENDIENTE if (online and abs(fecha - fecha_servidor) > 1) else CONFIRMADO
    conceder(rc.live_daily_challenge_reward.seeds, sello(fecha), estado)      # 60
    jugador.ultima_fecha_reclamada = fecha
    if sellos_consecutivos(jugador) == 7: conceder(rc.live_daily_streak7_reward)   # 1 Pororoca
    if sellos_del_mes(jugador) == 20: conceder(marco_pais_del_mes)
```

**Pantalla.** Tarjeta en el lobby: "Reto del río · {fecha corta}", icono de la banda (una, dos o tres olas), estado (Jugar / Sellado), racha de días "{n}/7". Al abrir: tablero con el HUD normal; cabecera "Reto del río"; sin barra de Crecida. Al ganar: animación del sello cayendo sobre el calendario del mes (cuadrícula de 28-31), +60, botón Cosecha doble, y si procede la tarjeta "Aguas turbias · Bonus" con premio ×2.

### 4.2 Bitácora del río (`login_calendar`)

**Reglas.** 30 casillas que se sellan una por día; condición: haber jugado ≥1 nivel (campaña o Reto) ese día local. Reclamo **secuencial sin reinicio**: si el jugador falta un día, la siguiente casilla sigue esperando (patrón Unity Daily Rewards / Mistplay, research/04 C1 §3 [V]); al sellar la 30 empieza un ciclo nuevo con el arte del mundo actual. Reclamo manual (toque) para crear el momento; el takeover de la Bitácora es el único automático al abrir la sesión si hay casilla pendiente. Total por ciclo: **3.000 semillas** + 15 Marea + 3 Morpho + 3 Cuia extra + 1 Luciérnaga + 2 Canastas + 1 Canasta grande (dirección 12; research/04 C1 §3 [E]). El 53 % de las semillas está en los hitos 7/14/21/30 (modelo "zigzag con hitos", research/04 C1 §3 [V]).

| Día | Premio | Día | Premio | Día | Premio |
|---|---|---|---|---|---|
| 1 | 50 | 11 | 100 | 21 | **400 + Canasta** |
| 2 | 3 Marea | 12 | 1 Morpho | 22 | 100 |
| 3 | 50 | 13 | 100 | 23 | 1 Cuia extra |
| 4 | 1 Morpho | 14 | **300 + 1 Cuia extra** | 24 | 100 |
| 5 | 100 | 15 | 50 | 25 | 1 Morpho |
| 6 | 1 Cuia extra | 16 | 100 | 26 | 150 |
| 7 | **250 + Canasta** | 17 | 3 Marea | 27 | 3 Marea |
| 8 | 50 | 18 | 100 | 28 | 150 |
| 9 | 50 | 19 | 3 Marea | 29 | 1 Luciérnaga |
| 10 | 3 Marea | 20 | 150 | 30 | **650 + Canasta grande + marco de país** |

Verificación: 50+50+100+250+50+50+100+100+300+50+100+100+150+400+100+100+150+150+650 = 3.000. La tabla vive en Remote Config como `eco_calendar_table_v1` (08 §9; misma tabla en 05 §2.3) y se puede reemplazar sin actualización.

Reglas complementarias [P]: (1) un comodín aún no desbloqueado se convierte en semillas al reclamar (`eco_calendar_locked_booster_seeds`: 3 Marea → 30; Morpho, Cuia extra o Luciérnaga → 100), así la casilla nunca queda vacía para un jugador de nivel bajo; (2) **Canasta grande** (pendiente de confirmación en el glosario, pregunta 4): 150 semillas + 1 Cuia extra + 1 Morpho + 3 Marea, contenido visible; (3) placement rewarded "cofre diario" (`rv_daily_chest`, dirección 13; **v1.1**, `eco_calendar_double_ad_enabled` = false en 1.0 y true en 1.1, 08 §9): botón "Duplicar con video" solo en casillas de semillas que no sean hito, 1/día; impacto máximo +1.700 semillas/ciclo para quien siempre lo use, ≈+600 con una adopción del 35 % [E]; (4) el marco de país del día 30 es el del mundo donde está el jugador.

**Pantalla.** Libreta de campo abierta: 30 casillas en 5 filas × 6, las de hito más grandes; la de hoy pulsa; las pasadas llevan sello de tinta; pie "Página {k} de 30 · Ciclo {c}". El icono del lobby muestra un punto si hay casilla pendiente.

### 4.3 Crecida (`win_streak`) y Hoja escudo (`streak_shield`)

**Reglas.**

| Regla | Valor | Fuente |
|---|---|---|
| Desbloqueo | Nivel 25 (`streak_unlock_level`, 08 §9) | glosario §7.2; research/04 C1 §3 [E] |
| Qué cuenta | Solo niveles de campaña. Victoria: +1. **Fallo**: ver la tabla canónica de 04 §5.2 (este capítulo no redefine el término). Solo el **fallo duro** rompe la Crecida, salvo Hoja escudo disponible: en 1-30, salir de Aguas estancadas con Volver a la orilla desde el modal; desde el 31, rendirse (¿Seguro? → Nivel fallido) o salir del nivel con el menú, que cuestan la canoa (D-008). No son fallo: Marea desde el modal, Cuia de rescate aceptada (conserva la racha), reinicio voluntario con <8 vertidos, salir con el menú en 1-30, cerrar la app (el estado se guarda y la pantalla abierta reaparece), crash o pérdida de red, Salto del Boto (no la rompe ni la avanza). El Reto del río y los eventos no la tocan | 04 §5.2; dirección 7 y 12 |
| Fallo blando | Reinicio voluntario (Volver a la orilla) con ≥8 vertidos (`restart_confirm_min_pours`): **no gasta canoa, no rompe la Crecida**, pero suma 1 a `fails_level` y cuenta para el alivio "El río bajó" (3) y para Salto del Boto (5). Con <8 vertidos no cuenta para nada | 04 §5.2 |
| Hitos | Ver 05 §2.4 (fila única para todos los capítulos): 3 → 300 semillas · 6 → Canasta (contenido visible: 50 semillas + el siguiente comodín del ciclo de la sección 4.4, determinista, no aleatorio) · 10 → 1 Cuia extra (inventario) + estado **río crecido** (Cosecha doble sin anuncio). **Cada hito se paga una vez por día local** (`streak_hits_once_per_day` = true): si la racha se rompe (o la barra se vacía en el hito 10) y vuelve a 3 el mismo día, el contador sube pero el hito no se repite hasta el día siguiente. Al pagar el hito 10 la barra vuelve a 0; el río crecido sigue activo hasta el siguiente fallo duro no escudado, también con la barra a 0 (05 §2.4; así lo simula `tools/econ_sim.py`). Clave única `streak_milestones` = {"3":300,"6":"chest","10":"extra_cuia+rio_crecido"} (08 §9); el alias `eco_streak_rewards` no existe | dirección 12; 05 §2.4; research/04 C1 §3 [E] |
| Tope diario | 600 semillas (`streak_daily_cap`) sumando el hito 3 (300) y las 50 de la Canasta del hito 6; el bonus del río crecido (Cosecha doble sin anuncio) y los comodines de los hitos 6 y 10 no cuentan contra el tope (05 §2.4, 08 §9) [P] | 05 §2.4; research/04 C1 §3 [E] |
| Hoja escudo | 1 por día local (`streak_shield_per_day`, 08 §9); se aplica sola al primer fallo duro con Crecida ≥1 (04 §8; 05 §2.4): con la barra a 0 tras el hito 10 no se gasta y ese fallo duro apaga el río crecido; animación de la hoja de *Victoria amazonica* cubriendo la barra; texto del glosario §6.3 | dirección 12; research/09 §Mejorar [V] |
| Expiración por reloj | **Ninguna.** La Crecida no expira por cambio de día ni por horas o días sin jugar: solo cae por un fallo duro no escudado (04 §8; 05 §2.4 y guardarraíl de 05 §10). El push de la sección 8.3 es un recordatorio sin plazo | research/09 §Mejorar ("racha de 19 perdida por dormir") [R]; 04 §8; 05 §2.4 |
| Nivel aliviado | Si la variante "El río bajó" (dirección 7) se sirve tras 3 fallos (`relief_after_fails`; cuentan los duros y los blandos de 04 §5.2), la victoria en la variante cuenta igual para la Crecida | dirección 7; 04 §5.2 |

**Pseudocódigo.**

```
on_level_end(resultado, nivel):
    if not nivel.es_campana: return
    if resultado == VICTORIA:
        crecida.racha += 1
        crecida.ultima_victoria = ahora
        if crecida.racha in {3, 6, 10} and crecida.racha not in crecida.hitos_pagados_hoy:
            pagar_hito(crecida.racha)                                     # rc.streak_milestones; rc.streak_daily_cap (600) solo suma las 300 del hito 3 y las 50 de la Canasta del hito 6
            crecida.hitos_pagados_hoy.add(crecida.racha)                  # rc.streak_hits_once_per_day: cada hito una vez por día local
        if crecida.racha >= 10:                                           # hito 10, pagado ahora o ya cobrado hoy (sin nueva Cuia extra)
            crecida.estado = RIO_CRECIDO                                  # x2 sin anuncio hasta el siguiente fallo duro no escudado
            crecida.racha = 0                                             # la barra vuelve a 0 (05 §2.4; así lo simula tools/econ_sim.py)
    elif resultado == FALLO_DURO:                                         # solo el fallo duro de 04 §5.2; el fallo blando no llega aquí
        if crecida.racha >= 1 and crecida.escudo_disponible_hoy:          # Hoja escudo solo con Crecida ≥1 (04 §8; 05 §2.4)
            crecida.escudo_disponible_hoy = false
            mostrar("Hoja escudo")                                        # la racha y el río crecido no cambian
        else:                                                             # con la barra a 0 tras el hito 10 el escudo no se gasta y el río crecido termina
            crecida.racha = 0; crecida.estado = NORMAL
            mostrar("El río baja")

on_app_open():
    if dia_local_cambio():
        crecida.escudo_disponible_hoy = true
        crecida.semillas_hoy = 0
        crecida.hitos_pagados_hoy = {}
    # La racha NO se toca aquí: no existe expiración por reloj (04 §8; 05 §2.4).
    # crecida.ultima_victoria solo alimenta el recordatorio push de la sección 8.3.
```

**HUD.** Medidor vertical del río en el borde derecho (07 §7.4, dueño del HUD; 10 marcas; el agua sube con cada victoria y vuelve a 0 al pagar el hito 10; la nutria gigante chilla en cada hito, glosario §3.3); icono de hoja cuando el escudo está disponible; la pantalla de victoria replica la barra en grande con "Crecida {n}/10", el siguiente hito y su premio; en el lobby, widget lateral silencioso. La barra de evento bajo la cabecera queda para los eventos de 1.2 (02 §5).

**Por qué es justo frente a Broom Streak** (research/magic-sort/c §2.1 [V]/[E]): no expira de noche ni por reloj (solo por fallo duro no escudado), perdona un fallo al día, el hito 3 (300) iguala el precio del primer rescate del día (300) y la escalera completa (300 + Canasta ≈116 + Cuia extra 150 ≈ 566 semillas-equivalentes fijas, más el río crecido: +20/+60/+80 por victoria hasta el siguiente fallo duro no escudado) iguala las 600 semillas de una Cuia de rescate desde la segunda victoria con río crecido y supera el umbral de 720 de la sección 5.1 desde la octava [E]. El pago acelera (comprar un rescate mantiene la racha) pero no es la única vía.

### 4.4 Canasta (`chest_basket`) y Dato del río

- **Canasta**: cada 10 niveles de campaña (10, 20, 30…). Contenido **determinista por nivel y visible antes de abrir** (dirección 15; research/08 C5.3 [V]): nivel 10 → 50 semillas; después 50 semillas (`eco_basket_seeds`) + 1 comodín en ciclo fijo [3 Marea, 1 Morpho, 1 Cuia extra, 3 Marea, 1 Morpho] (`eco_basket_cycle` = ["undo3","hint","extra_cuia","undo3","hint"], 08 §9; niveles 20, 30, 40, 50, 60 y se repite); cada 100 niveles, Canasta grande. Valor esperado por Canasta ≈ 50 + 66 semillas-equivalentes (research/04 C1 §3 [E]). Nunca comprable ni abrible con semillas.
- **Dato del río** (D-013, 2026-10-13): sale de la pantalla de la Canasta y va a la **victoria**, bajo «Siguiente» (02 §6), con una sola línea de texto por victoria (T6-A de la propuesta de TiTino). Dos canales: en la victoria del nivel `10k`, el **dato de llegada** a la parada del Mapa del río, en la placa del Dato del río con el retrato de TiTino y su gesto (sección 4.5); y, con Q1-A de Frutas del río, la **línea de la serie de frutas** de cada nivel, que absorbe el Dato del río («Dato del río · Açaí 3/20»; propuesta de Frutas del río). Así se cierran los tres sitios que se contradecían (victoria, pantalla de la Canasta y Mapa del 10). **Con D-017 (2026-10-13):** la línea de fruta sale **desde el nivel 1** (el açaí abre el juego; Q3-A), 20 por fruta (Q2-A), y se vigila con la alarma del embudo 1-10 de la sección 2.4 (un nivel que pierda más del 5 %), con apagado remoto; se relee en la ficha «Frutas del río» (Q4-A), y completar una serie no da premio, solo un sello (Q5-A). **Canales separados y una línea por victoria** (pregunta 45 de GDD.md §6.3, decidida el 2026-10-14: (a); D-022): la llegada cada 10 niveles (`ui_river_fact_every_n_levels` = 10) y la línea de fruta con su propia clave (`ui_fruit_line_every_n_levels` [P], default 1; 0 la apaga), con prioridad: frase de TiTino en sus tres hitos > dato de llegada > línea de fruta. El dato de llegada desplazado por TiTino sale en la victoria siguiente; la línea de fruta desplazada no sale en la victoria y queda revelada en su ficha, y la tarjeta de la parada enseña la fruta de la serie (02 §6). Fuente: los datos de llegada de 1-200 están en `river_stops_v1.json`, cruzados con los candidatos y los vetos de la propuesta de D-007, §(e) (07 §2.2, regla 6; Q16-A); solo [V], o [E] revisados por el asesor cultural (dirección 15). Se relee en la tarjeta de la parada del Mapa; en 1.1 alimenta el Cuaderno de campo. Es contenido de retención y de marketing (research/07 §Implicaciones 8 [E]).

### 4.5 Mapa del río (`river_map`), skins por hito y tarjetas "Fruta nueva"

**Mapa del río: remontar el Amazonas hasta donde nace** (D-013, decisión del dueño del 2026-10-12 y del 2026-10-13; propuesta completa, con su tabla de paradas, sus fuentes y su muestra: `docs/research/mapa-del-rio/propuesta-mapa-del-rio.md`). Pantalla pasiva, sin navegación de saga ni estrellas (research/02 C3.4 [V]), sin puertas de tiempo ni de pago y sin rejugar: «Jugar» siempre abre el siguiente nivel. Todo nombre, dato y texto, pendiente del asesor cultural.

- **La fantasía.** Remontas el Amazonas, tablero a tablero, del Atlántico a una de sus nacientes, el nevado Mismi (cabecera del Apurímac; la más lejana con agua todo el año). Capi rema la canoa cargada de cuias, Rosa nada delante y TiTino, que vive en su árbol de la desembocadura y no viaja, firma el dato de cada sitio al que llegas. Seis actos, con una pregunta por mundo que responde un dato de llegada del mismo mundo («¿Por qué el río Negro es negro?»). Al ganar el 1.000, el río despierta: una gota baja del nacimiento a la casa de TiTino (≈ 8 s, saltable). La vuelta 2 empieza en el Estuario.
- **La forma real, en vidrio.** La cuenca real, girada 90° sin deformarla: los Andes arriba, el Atlántico abajo y el norte a la derecha, con un botón «Norte arriba» en la vista Cuenca. Dos vistas: Mundo (por defecto) y Cuenca. Cada nivel ganado llena el río una décima del tramo **con su agua real** (blanca, negra o clara); los afluentes de paso se llenan enteros al pasar su boca, y el de un mundo futuro solo en su boca, con su silueta «Mundo 7 · Nivel 301». Lo que falta va en vidrio azul hielo con brillo, que nunca parece sequía, y el agua nunca retrocede: ni la Crecida, ni un fallo, ni «El río bajó» la vacían. Un nivel saltado con el Salto del Boto no corta el agua: su boya lleva a Rosa. Geometría: Natural Earth y HydroRIVERS (08 §4.5).
- **Paradas.** Una cada 10 niveles: la parada `k` cubre los niveles `10k+1` … `10k+10` y se llega a ella al ganar el `10k` (20 en 1-200, 70 en 1-700 y 100 en total), con una boya por nivel. **Gran parada** (la primera de cada mundo), **parada** y **cierre de mundo** (con baliza y la skin del mundo como «Premio del mundo · faltan N», sin precio). Cada parada futura enseña el contenido de su Canasta (sección 4.4), y las cobradas llevan una marca. Entre paradas, **puntos de paso** con nombre (Óbidos, Parintins, Tabatinga…), que al tocarlos cuentan su dato, y **miradores** («?» fuera de la ruta) que revela el progreso. **Países:** sin fronteras en tierra; al cruzar de país por el río, la canoa iza la bandera de cortesía (Colombia al ganar el 80, el Perú al ganar el 140; con la 1.0 de 1-700, también Ecuador, Bolivia y el Perú otra vez). La dificultad no se muestra.
- **Llegada.** En la victoria del `10k`, una franja de 1,2 s bajo «Siguiente» con la canoa, la mini cuia del sitio, la placa del lugar y el dato de llegada (02 §6). En las demás victorias, la barra de la Canasta pasa a «Canasta · {sitio} k/10».
- **Cuándo sale solo** (Q4-D): en el 10 (sección 2.3: el primer Mapa); en cada cambio de mundo, el viaje va dentro del banner de mundo del primer nivel del mundo (≤ 4 s, saltable, con «Ver mapa»; 02 §6), sin pantalla nueva; al acabarse el contenido, con la pantalla «El río sigue creciendo» (sección 9); y al ganar el 1.000. Fuera de esos hitos, `ui_max_takeovers_per_session` = 1 se respeta, y `ui_map_autopen` (08 §9) puede dejarlo solo en el 10.
- **Entradas.** El icono del inicio (oculto hasta el 10, con un punto si hay una llegada sin ver; `progress.map_seen_level`, 08 §4.3); la pausa, que lo abre **encima del nivel en pausa**, con «Volver al nivel» en lugar de «Jugar» (no es salir del nivel y no gasta canoa); y «Ver mapa» en la victoria del `10k` y en el banner. Al abrirlo, las boyas ganadas desde la última vez se encienden en orden («ponerse al día», ≤ 1,5 s). Con 0 canoas sigue disponible. **El Mercado no se abre desde el Mapa** y el Mapa no lleva ofertas ni precios (Q10-A).
- **Personajes.** Capi rema (no habla); Rosa nada delante, salta con sus parientes en Tarapoto y se despide en el 961, al pie de los Andes; en los mundos 7 y 8 guía un bufeo boliviano (Q18-A). TiTino firma desde casa: el dato de cada llegada sale en la placa del Dato del río con su retrato, nunca en bocadillo; con la cámara lejos sale en una **ventanita de casa** con «Casa · a unos N km», y dormita a los 30 s sin toques.
- **Coleccionable: el Muestrario del río.** Una mini cuia de vidrio por parada, llena del agua del sitio, con su **tarjeta**: el fondo del tablero de esa parada en pequeño, con nombre, país, río y tipo de agua, el dato con su fuente (el nombre corto de la fila `<clave del dato>.source` de `facts.csv`, bajo el dato: 08 §4.7; pregunta 57 de GDD.md §6.3, decidida el 2026-10-19: (a); D-028), «Otro dato», el miquito del tramo y la fruta que debutó allí. Se llena solo; banderas, miradores y Datos del río k/n completan la colección. Sin economía: llegar no da semillas (la Canasta ya existe) y nada se compra en el Mapa.
- **Fondos por parada.** El tablero, el inicio, el banner y la tarjeta de cada parada enseñan el mismo sitio (07 §2.1). En los tableros de 3 filas, el sitio se ve en la victoria con vista, el inicio, el banner y el rótulo del lugar.
- **Sin estado nuevo salvo un campo:** todo se deduce de `max_level`, `skipped_levels`, `WorldTable` y el catálogo de paradas, más `progress.map_seen_level` (08 §4.3). Interruptor de respaldo: `ui_map_mode` = `worlds` vuelve a las 13 paradas del canon anterior como filtro de la misma pantalla (08 §9).
- **El banner de mundo** (D-012) sale al cargar el primer nivel de cada mundo salvo el Estuario (en 1-200: 11, 41, 81 y 141; el 1 no lo lleva), antes de Llenar y teñir y solo en el primer intento; lo presenta Capi mientras Rosa asoma y se sumerge (02 §6). Con D-013 trae dentro el viaje de mundo.
- **Skins por hito** (dirección 14; glosario §4): la skin de un mundo se gana al **completar su último nivel** (Cuia de Santarém al 40, Cuia del Encuentro al 80, Totuma al 140; en 1.1 Huingo al 200, etc.) y se vende como pack `af_skin_*` 1,99 en el Mercado desde que el jugador entra en ese mundo; desde el mundo 3 (nivel 41), además, en una tarjeta propia fuera del nivel y fuera del Mapa del río, por ejemplo tras la primera victoria del mundo [P] (pregunta 39 de GDD.md §6.3, decidida el 2026-10-13: (a), D-015; el banner de mundo va sin precio y el Mapa no lleva ofertas, D-013); en los mundos 1-2 la skin se ofrece únicamente en el Mercado, para que el nivel 11 no sume una oferta al banner de mundo, la Fruta nueva y la Cosecha doble (contenido exacto: 05 §5). El Mapa muestra la skin como «Premio del mundo · faltan N», sin precio. Cuia de vidrio es la skin base. Las skins no cambian la jugabilidad; la cavidad interior y las 4 marcas de dosis se mantienen (glosario §4).
- **Fruta nueva**: tarjeta de 2 s en el debut de cada tinte (niveles 1, 2, 4, 7, 11, 21, 41, 61, 81, 111, 141, 201, 301): icono, nombre en el idioma del jugador, muestra de color, patrón de accesibilidad y una línea, que es la frase de la tarjeta de su serie de Frutas del río (D-017; propuesta, 6.2; pendiente del asesor), por ejemplo "Açaí: la crema morada del Pará". Se registra como "descubierta" para que el Cuaderno de campo de 1.1 se llene de forma retroactiva.

### 4.6 Cuaderno de campo (`album`, v1.1): reglas anticipadas

Tres páginas: Frutas (14 tintes, con las mismas fichas k/20 de Frutas del río, que en la 1.0 ya se releen desde la victoria y desde Ajustes: D-017, Q4-A; al completar la página entera, el premio de abajo, que es la opción C de su Q5), Fauna (12 animales del elenco del glosario §3.3 y de research/07 §5), Ríos (el Muestrario: una muestra por parada, 100). Con D-013, la página Ríos es el **Muestrario del río** del Mapa (sección 4.5): cada muestra es una mini cuia con su tarjeta, que se llena sola y hacia atrás. Cada carta tiene una **condición de desbloqueo visible** ("Descubre el tinte", "Completa el mundo 4", "Sella 20 Retos en un mes", "Termina una Travesía"): colección por hitos, sin sobres, sin azar, sin compra (dirección 15; research/08 C5.3 [V]). Página completa → 300 semillas + marco [P]. Las colecciones están en el 75 % de los casual y los álbumes en el 66 % (research/05 §3 [V]); aquí sustituyen a las cartas de loot box de Magic Sort.

### 4.7 Pantallas de v1.0 (resumen para UI)

| Pantalla | Elementos | Acceso | Notas |
|---|---|---|---|
| Lobby (pantalla de inicio) | **Composición de Magic Sort** (D-008, 2026-10-08; ficha §7.1): saldos arriba, botón central grande y pestañas abajo, sin su trade dress (07 §1.3; D-003). Botón grande «Jugar · Nivel {n}» (`level_n`; 60 % del alto visual), con el chip Difícil o Muy difícil y su cinta «×3» o «×4» cuando toque, y en Muy difícil el botón en variante nocturna (D-009; 02 §10) [P]; contador de semillas y de canoas «n/5» con cronómetro (31+); Ajustes; **fondo de la parada actual** del Mapa del río, el mismo del tablero (D-013), con «Faltan {n} niveles para {lugar}» (`map_stop_remaining`) bajo «Jugar»; **TiTino solo en la ventanita de casa**, su embaúba del estuario enmarcada, sobre el fondo de la parada (Q17-A: él se queda en casa y tú estás lejos; nunca tapa «Jugar» ni los saldos); tarjetas Reto del río y Bitácora, iconos Mercado / Mapa del río (el del Mapa, con un punto si hay una llegada sin ver), widget de Crecida (25+). Mientras un sistema no exista en el build (Mercado, Mapa del río, Reto del río, Bitácora), su entrada queda oculta (D-008) | Arranque; Inicio desde la victoria y X de Nivel fallido (D-008) | Máximo un takeover por sesión fuera de hitos (`ui_max_takeovers_per_session` = 1, 08 §9); orden de prioridad: Bitácora pendiente → evento que empieza o termina → oferta → resultado de liga. Tocar el contador de canoas en 0/5, o Jugar con 0 canoas desde el 31, abre Sin canoas (04 §7.3) |
| Reto del río | Tarjeta + calendario de sellos + tablero | Lobby | Ver 4.1 |
| Bitácora del río | 30 casillas, hoy pulsando, "Duplicar con video" (1.1) | Lobby / takeover | Ver 4.2 |
| Mapa del río | La cuenca real en vidrio (vista Mundo y vista Cuenca, «Norte arriba»), 100 paradas (una cada 10 niveles), canoa de Capi con Rosa delante, ventanita de casa de TiTino, cabecera con la pregunta del mundo, Muestrario y «Jugar · Nivel {n}» siempre a la vista; sin ofertas (D-013) | Lobby, pausa (encima del nivel, sin salir), «Ver mapa» de la victoria del `10k` y del banner; solo en el 10, en los cambios de mundo (dentro del banner), al acabarse el contenido y en el 1.000 | Ver 4.5; `OnDemandRendering` a 30 fps en reposo (08 §5.4) |
| Mercado | Pestañas Semillas · Comodines · Cuias (skins) · Sin anuncios; ambientación Ver-o-Peso (glosario §7.1) | Lobby, HUD (+); desde el nivel 10 (01 §7.1, 05 §2.1) | Catálogo en 05 §4-5 |
| Victoria | +semillas, Cosecha doble, Crecida, Canasta, Dato del río (llegada del Mapa del río en el `10k` y línea de fruta, una sola línea por victoria; D-013, 02 §6), victoria con vista, TiTino si cabe, «Inicio» [P] como botón secundario (D-008), "Siguiente" como CTA primario, con el chip y la cinta del nivel N + 1 si lleva etiqueta (D-009: tras ganar se pasa directo al siguiente sin ver el botón del inicio), **sin auto-avance por defecto** (`victory_autonext_s` = 0, int; canon de 02 §6: la oferta x2 exige una decisión y el intersticial solo se dispara tras el toque; un valor alternativo de 2,5 s solo sería admisible sin Cosecha doble visible); tras ver entero el vídeo de la Cosecha doble, el juego pasa solo al nivel siguiente por el camino de Siguiente, sin intersticial (D-011, 02 §6) | Fin de nivel | Intersticial, si toca, **después** de esta pantalla y de la animación, nunca por temporizador (dirección 13) |
| Tarjeta previa [P] | Difícil: rana dardo, «Difícil», subtítulo «Aguas bravas», «Premio: 60» con la cinta «×3» e iconos de las mecánicas del nivel. Muy difícil: jaguar, «Muy difícil», «Noche de jaguar», «Premio: 80» con la cinta «×4» e iconos. En un estreno, el tutorial de una pantalla hace de tarjeta; en el 100, una sola tarjeta combinada con el tutorial de Aguas turbias y la Luciérnaga regalada (04 §2.5). Se cierra con 1 toque | Al cargar un nivel con etiqueta o con estreno, antes de Llenar y teñir (estado LEVEL_CARD de 02 §2); no sale al reanudar ni, por defecto, al reintentar [P] | D-009 (Q2 sí); 02 §10; sin anuncio |
| Aguas estancadas | Marea (si cupo) · Cuia de rescate (31+) · al pie, Volver a la orilla (1-30) o «Rendirse» [P] (31+) | Sin vertidos legales | Glosario §5.1; embudo de D-008 en 04 §6 |
| ¿Seguro? [P] | «Perderás 1 canoa» y otra vez «Continuar: +1 cuia»; X y «Rendirse» confirman; cabecera ámbar en Difícil e índigo en Muy difícil (D-009) | «Rendirse», desde el 31 (D-008) | 04 §6.2 y §6.6; sin anuncio |
| Nivel fallido [P] | «Nivel {n}», «Premio: {n} semillas» (no se cobra), «Reintentar» [P] y X → Lobby; cabecera ámbar o índigo según la etiqueta (D-009) | Rendición confirmada o «Salir» con el menú, desde el 31 (D-008) | 04 §6.6; sin anuncio (05 §10) |
| Sin canoas | Cronómetro, Canoa extra por video, Llenar canoas, Canoas infinitas 1 h | «Reintentar», Jugar o Siguiente con 0 canoas, desde el 31 | 04 §7.3; sin anuncio (05 §10) |

## 5. Eventos

### 5.1 Reglas de evento justo (se aplican a todos)

| Regla | Especificación | Por qué |
|---|---|---|
| Premio > rescate | El premio total de completar un evento ≥ 1,2 × precio fijo de la Cuia de rescate (600 en todos los niveles desde el 31, `rescue_price_bands` = [[31,600]]; ≥ 720 semillas-equivalentes); en eventos de racha, el primer hito ≥ 300 (precio del primer rescate del día) y el total de la escalera, contando el bonus del río crecido, ≥ 720 [P] | Queja estructural de Magic Sort: continue 900 vs premios de 300 (research/magic-sort/c §1 [V]) |
| Escudo | Todo evento con racha usa la Hoja escudo diaria | research/09 §Mejorar [V] |
| Tolerancia | Nada expira de noche ni por reloj: una racha (Crecida o racha de evento) solo cae por fallo duro no escudado (sección 4.3; 04 §8; 05 §2.4). Los eventos con ventana cerrada (Travesía, Regata, Encuentro) avisan 24 h antes del cierre y el premio ganado se puede reclamar hasta 48 h después | research/09 §Mejorar [V] |
| Sin bots | Las carreras y ligas solo enfrentan a jugadores reales; si una cubeta queda incompleta, corre con los que haya y los premios escalan por posición relativa | Honestidad de marca; evita la percepción "pagar es la única forma de ganar" (research/magic-sort/c §2.1 [V]) |
| Un evento temporal a la vez | Además de los permanentes (Crecida, Reto, Liga) solo hay **un** evento de tiempo limitado activo; máximo un takeover por sesión | Pop-ups apilados son la queja n.º 8 del líder (research/magic-sort/c §7 [V]) |
| Sin pago para competir | El pago (Cuia de rescate, semillas) acelera; nunca compra puntos ni posiciones | research/09 §Qué NO copiar [V] |
| Offline | Progreso local; sincroniza al reconectar; ranking cacheado con "actualizado hace X"; premio reclamable hasta 48 h tras el cierre | research/06 C6.1 [E] |
| Sin azar de pago | Ningún premio de evento sale de una caja aleatoria; cofres gratuitos con contenido visible | dirección 15; research/08 C5.1-C5.3 [V] |

### 5.2 Ficha por evento

| Evento | Versión · desbloqueo | Duración · cadencia | Reglas | Premios [P] | Servidor |
|---|---|---|---|---|---|
| **Crecida** (`win_streak`) | 1.0 · nivel 25 | Permanente | Sección 4.3 | 300 · Canasta · 1 Cuia extra + río crecido; cada hito una vez por día local; tope 600/día | No |
| **Travesía** (`trail`) | 1.2 · nivel 41 | 7 días · 1 al mes, alternando con Encuentro de las Aguas | 10-15 niveles especiales con **una** regla alterada (p. ej. "todas las cuias son chicas", "cada 5 vertidos una cuia Lluvia se llena una dosis", "dos Remolinos fijos"); sin ranking, sin canoas, reintentos libres; tema de un país con fecha local | Hitos 3 / 6 / 10 niveles: 150 · 300 · 400 + skin de temporada (total 850 + skin) | No: definición en Remote Config + paquete de niveles por Addressables |
| **Regata** (`regatta`) | 1.2 (B) / 1.3 (A) · nivel 51 | 48 h · cada 2 semanas (jueves 12:00 UTC → sábado 12:00 UTC) | Carrera por niveles de campaña completados ("brazadas") contra ≤20 jugadores de la misma banda de progreso (51-200 / 201-500 / 501+); inscripción voluntaria en las primeras 24 h; fallar no resta | 1.º 1.000 semillas + Canoas infinitas 1 h + marco · 2.º 700 + 30 min · 3.º 500 + 30 min · 4-10 250 · 11-20 con ≥3 brazadas 100 | Sí, gestionado: Unity Leaderboards (`BucketSize` 20, `ResetConfig` 48 h) con puntuación escrita desde Cloud Code (research/06 C6.1 [V]) |
| **Encuentro de las Aguas** (`meeting_waters`) | 1.2 (B) / 1.3 (A) · nivel 61 | 10 días · cada ~2 meses | 12 niveles donde siempre están presentes Agua del Tapajós y Agua del Río Negro; K hasta 13-14 permitido (dirección 3); Patrones de fruta forzado activo; sin ranking | Hitos 4 / 8 / 12: 200 · 300 · **Cuia del Encuentro** (si no se posee) o variante dorada "Cuia del Encuentro · {año}" + 400 (total 900 + skin) | No |
| **Temporada de lluvias** (`rainy_season`) | 1.2 · nivel 11 | 4 días · fechas por Remote Config (≈ cada 6-8 semanas) | Fondos con variante de lluvia y crecida (LUT + partículas, research/02 C3.4 [E]); **Cosecha doble sin anuncio** en toda victoria de campaña (solo sobre la recompensa base 20/60/80); no afecta a hitos de Crecida | Implícito: x2 semillas 4 días | No: flag + ventana de fechas en Remote Config |
| **Río de la aldea** (`village_river`) | 2.0 · Aldea | 72 h · cada 2 semanas | Las victorias de todos los miembros llenan el río comunal; meta = 15 brazadas × miembros activos; cada miembro necesita ≥5 brazadas para cobrar; hitos 25/50/75/100 % | 100 · 150 · 200 · desborde: 500 + Canoas infinitas 1 h (total 950) | Sí: agregación en Firestore/Cloud Functions o Nakama (research/06 C6.3 [E]) |

Cadencia de **tipo** nuevo de evento: uno cada 6-8 semanas a partir de 1.2 (Magic Sort: cada ~2 meses, research/09 [V]); cadencia de **instancias**: un evento temporal cada semana en rotación (Travesía → Temporada de lluvias → Regata → Encuentro de las Aguas…), ver sección 10.

**Candidatas de la propuesta de mecánicas** (D-007, Q10 sí: los contadores a favor del jugador quedan fuera de la campaña y van a Travesías y eventos; propuesta b.4) [P]. Ninguna es canon todavía; todas pasan por el asesor cultural y las que tocan precios, por el dueño (05):
- **Travesías:** Nido de tortuga, con la regla «solo cuentan los sellos» (nunca «de charapa», que en el Perú también nombra a la gente del oriente); Piracema, en mundos brasileños.
- **Eventos:** Fiesta de la fruta (tableros con mucha dosis de un tinte, un tinte al mes, sin nombres de fiestas oficiales); Mareas de equinoccio (Pororoca más barata en marzo y septiembre); Defeso (evento de conservación, con el asesor); Casa de farinha (el tipiti, en el mundo 1, con el asesor); Hora del tacacá (capas en un orden fijo); Mijano (aguas bajas en Loreto); y «Dos aguas», la cuia de dos carriles, como mecánica del Encuentro de las Aguas con otro nombre.
- **Descartado:** el Banzeiro (posicional y cercano a la terra caída).
- **Decididos el 2026-10-13** (D-020): del revelado rítmico solo queda la burbuja del revelado de Aguas turbias (02 §4.1), sin Travesía con revelado por contador (pregunta 35 de GDD.md §6.3, (a)); y «Zafra» es solo el nombre [P] del hito 420, pendiente del asesor, no un evento de este capítulo (pregunta 28, (a)).

### 5.3 Detalle de Regata y Travesía (lo que implementa el programador)

**Regata.**

```
# inscripción (primeras 24 h de la ventana)
on_tap_participar(jugador):
    banda = banda_regata(jugador.nivel_max)                 # 51-200 / 201-500 / 501+
    leaderboard = f"regatta_{semana}_{banda}"               # BucketSize = 20, UpdateType = aggregate
    cloud_code.join(leaderboard, jugador.uid)               # el servidor asigna cubeta en orden de llegada

# puntuación (solo campaña, solo victorias, validada en servidor)
on_level_win(nivel):
    cola_offline.push({evento: "regatta", nivel, ts})
    if online: cloud_code.add_score(leaderboard, uid, +1, nivel, ts)   # rechaza > 40 brazadas/h, niveles no consecutivos, nivel > nivel_max+1

# cierre
scheduler(sabado 12:00 UTC): archivar cubetas; calcular premios por rango; marcar reclamables 48 h
```

Reglas de interfaz: widget lateral con la posición ("vas 4.º de 17"), sin takeover salvo al inscribirse y al cerrar; empates se resuelven por marca temporal de la última brazada; si al cierre de inscripción una cubeta tiene <20, corre igual (mínimo 2); los premios de 4-10 y 11-20 se adaptan a la fracción de la cubeta ocupada.

**Travesía.** Definición en Remote Config:

```json
{"id":"trail_2027_02_carnaval","world_theme":"w02_tapajos","rule":"all_small_cuias","levels_pack":"trail_2027_02.json",
 "start":"2027-02-12T12:00Z","end":"2027-02-19T12:00Z","milestones":[{"n":3,"seeds":150},{"n":6,"seeds":300},{"n":10,"seeds":400,"skin":"skin_season_carnaval"}]}
```

El paquete de niveles se descarga por Addressables al anunciarse (fast-follow) para que el evento sea jugable offline; los niveles se generan offline con el mismo pipeline de campaña y el solver verifica la regla alterada (dirección 17). Las reglas alteradas usan solo mecánicas ya introducidas en la campaña hasta ese nivel (catálogo y lista única de estrenos en 02 §9.1 y `mech_debut_level`: 14 Cuia chica · 27 Cuia alta · 43 Hoja · 63 Matapi · 83 Liana con fruta · 100 Aguas turbias · 103 Remolino · 123 Barro · 143 Lluvia · 163 Brote · 183 Piraña · 323 Tinaja (M13, D-019: en la 1.0 si cumple su condición; si no, el mismo nivel en la 1.1); la Liana de cuenta (con número) es una variante, en el 243; D-007).

### 5.4 Qué requiere servidor (resumen)

| Sistema | Servidor | Componente | Coste/mes [E] |
|---|---|---|---|
| Reto del río, Bitácora, Crecida, Canasta, Mapa, Cuaderno de campo, Travesía, Encuentro de las Aguas, Temporada de lluvias | No | Pool local + Remote Config + guardado en nube (PGS Saved Games + Firebase Auth anónimo) | 0 |
| Cosecha doble y demás rewarded | Sí, mínimo | Cloud Function SSV de AdMob | 0 (cupo gratuito) |
| Premios de evento en semillas (reclamo) | Sí, mínimo | Cloud Function que valida reclamo contra el estado archivado | 0-5 |
| Regata, Liga del río | Gestionado | Unity Leaderboards + Cloud Code + Cloud Scheduler | 0-10 (1.2: 15-40 en total) |
| Aldea, Prestar una canoa, Río de la aldea | Sí | Firestore + Functions (150-300) o Nakama autoalojado (60-120) + 0,5 FTE | 100-300 |

Fuente: research/06 C6.1-C6.3 [V]/[E]. Regla de diseño offline: progreso en cliente con reconciliación "última escritura gana" por campo y máximo para contadores; valor económico concedido o confirmado en servidor (research/06 C6 [V]).

## 6. Liga del río (`league`, v1.2)

| Parámetro | Valor [P] | Nota |
|---|---|---|
| Desbloqueo | Nivel 41 | Magic Sort L39 (research/magic-sort/b §5.1 [V]) |
| Ciclo | Semanal; reset lunes 12:00 UTC (07:00 Bogotá/Lima, 09:00 Brasilia) mediante `ResetConfig` cron; archivo de la semana anterior con `Archive` | research/06 C6.1 [V] |
| Tres vistas | **Semanal**: mi cubeta de 50 jugadores de la misma banda de progreso (`BucketSize` 50) · **País**: top 100 + mi posición en el leaderboard del país de la cuenta de Play (banderas de los países amazónicos destacadas) · **Global**: top 100 + mi posición | research/09 §Qué copiar 13 [V] |
| Puntos | Victoria normal 10 · Difícil 20 · Muy difícil 30 · Reto del río 20 · nivel de Travesía 10; `UpdateType` aggregate | — |
| Antifraude | Puntuación escrita solo desde Cloud Code con App Check (Play Integrity); rechaza >40 victorias/hora, niveles no alcanzables y saltos de nivel; jugadores marcados se ocultan de las vistas | research/06 C6.1 [V] |
| Tiers (`TieringConfig` por rango) | Oro 1-3 · Plata 4-10 · Bronce 11-25 · Participante 26-50 con ≥50 puntos | — |
| Premios (cubeta semanal) | Oro: 1.000 semillas + Canoas infinitas 1 h + marco "Liga" (1.º además Cuia extra ×2) · Plata: 500 + 30 min · Bronce: 250 · Participante: 100 | Oro ≥ 1,2 × rescate (regla 5.1) |
| País y Global | Solo cosmético: marco "Mejor del país" / "Mejor del río" para el top 10 semanal; sin semillas (evita incentivar granjas de cuentas) | — |
| Offline | Puntos encolados y enviados al reconectar; ranking cacheado con marca temporal; premio reclamable 48 h tras el cierre | research/06 C6.1 [E] |
| Riesgo | Unity Leaderboards es "gratis por tiempo limitado"; alternativa PGS Leaderboards sin cubetas | research/06 C6.1 [V]/[NV] |

El diseño "competición + logro personal" (todos los participantes con ≥50 puntos cobran algo) sigue la tendencia que research/05 §3 [V] describe para que nadie termine la semana sin premio.

## 7. Aldeas (`village`, v2.0)

| Regla | Valor [P] |
|---|---|
| Desbloqueo | Nivel 31 (existen canoas) |
| Tamaño | 20-30 miembros; crear o unirse (abierta / con código); nivel mínimo configurable por el líder |
| Identidad sin UGC | Nombres de Aldea compuestos desde listas blancas ("{Fauna} del {Río}": "Nutrias del Tapajós"); nombre de jugador = alias de Play Games o alias generado ("Nutria 4821"); sin texto libre en ningún campo → sin obligaciones de moderación de UGC y sin descriptor "chat" en la clasificación (dirección 15; research/08 C5 [V]) |
| Prestar una canoa | Un miembro con 0 canoas pide; cada miembro puede prestar 1 canoa por petición; máximo 5 canoas recibidas por petición y 1 petición cada 4 h; prestar da 5 semillas al donante (tope 50/día) |
| Mensajes predefinidos | 12 frases localizadas automáticamente al idioma de quien lee (ventaja frente al chat libre en una comunidad ES/PT/EN): "Gracias", "¿Alguien me presta una canoa?", "¡Buena remada!", "Voy por el nivel {n}", "Vamos por el río de la aldea"… + 8 emojis de fauna; límite 20 mensajes/día por jugador |
| Río de la aldea | Evento cooperativo de 72 h (sección 5.2) |
| Moderación | Expulsión por el líder; abandono libre; reporte solo de nombre de Aldea (generado) y de comportamiento (spam de peticiones) |
| Backend | Firestore (grupos, agregados en Functions) o Nakama (grupos, chat de frases, torneos) según research/06 C6.3 [E]; 0,5 FTE live-ops/backend |

Magic Sort cobra 13+ en iOS por "Messaging and Chat" y las reseñas elogian equipos que regalan vidas (research/magic-sort/c §2.2 [V]): copiamos la generosidad y eliminamos el chat libre.

## 8. Notificaciones push

### 8.1 Permiso

1. Nunca en el arranque frío (research/05 §4 [V]). Pre-prompt propio tras la **primera victoria de la 2.ª sesión** con el nivel 10 ya superado (regla canónica de este capítulo para 02, 03 y 09; `push_preprompt_session` = 2 y `push_preprompt_min_level` = 10 en 08 §9; si la 2.ª sesión empieza antes del 10, se espera a la primera victoria posterior al 10), con beneficio concreto: TiTino, que firma las notificaciones desde D-013 (T1-A; Capi solo se nombra cuando trae algo): "¿Te avisamos cuando tu Reto del río esté listo y tu Bitácora espere?" · botones "Avísame" / "Ahora no". Se saca de la secuencia del nivel 10 (sección 2.3) para no apilar una decisión de permiso sobre el hito del primer mundo; si coincide con la primera tarjeta de Mochila de inicio, el pre-prompt va primero y la Mochila pasa a la siguiente victoria. El diálogo nativo (Android 13+ `POST_NOTIFICATIONS`; iOS en fase 2) solo se dispara si el jugador toca "Avísame": en iOS hay una sola oportunidad (research/05 §5 [V]).
2. Si rechaza: segundo y último pre-prompt al alcanzar el hito 3 de la Crecida o a los 7 días, lo que ocurra primero; después, solo un acceso en Ajustes.
3. Objetivo [P]: aceptación del pre-prompt ≥60 % en Android (benchmark de opt-in 69 %, research/05 §5 [V]); iOS planificar con ~25 %.

### 8.2 Cadencia y reglas

| Regla | Valor [P] | Fuente |
|---|---|---|
| Frecuencia | Igual a la frecuencia de apertura del jugador; máximo 1/día en D0-D7; 2-3/semana si no abre en 7 días; parar a los 30 días salvo los mensajes de regreso de la sección 9 | research/05 §5 [V] (OneSignal) |
| Horas silenciosas | 22:00-09:00 hora local del dispositivo | research/05 §5 [E] |
| Prioridad si coinciden | Premio por reclamar > evento que termina > Reto listo > Bitácora > Crecida te espera > canoas llenas > nuevos niveles | — |
| Estado real | Cada mensaje nombra algo que existe ahora (premio, racha, nivel); nada genérico tipo "te extrañamos" | research/05 §5 [V]/[E] |
| Personalización | Nombre del mundo, nivel, cantidad exacta: +9 % CTR | research/05 §5 [V] |
| Canal técnico | **1.0: todos locales** (Unity Mobile Notifications; 08 Desviaciones 2 y 8): los que dependen del estado del dispositivo (canoas, Bitácora, Crecida, Reto) se programan al pasar a segundo plano y se cancelan al abrir; los "remotos" se emulan en local: "Nuevos niveles" se agenda al cerrar la app con `live_content_end_message.next_batch_eta_days` cuando el jugador alcanzó el último nivel publicado; "Regreso" D7/D14/D30 se agendan al pasar a segundo plano y se cancelan al abrir; "Premio por reclamar" se agenda al salir con un premio pendiente. **FCM y push segmentados por evento desde 1.2** (eventos, premios de liga, Aldea) | dirección 20; 08 §7 y Desviaciones 2, 8 |
| Medición | `af_push_sent / af_push_open` con `type`; CTR objetivo ≥1 % Android (benchmark 1,05 %) | research/05 §5 [V] |

### 8.3 Catálogo de mensajes (ES-419 · PT-BR · EN)

| Tipo | Disparador | ES-419 | PT-BR | EN |
|---|---|---|---|---|
| Reto listo | 09:30-11:00 local, si no jugó hoy | El Reto del río de hoy ya está listo. TiTino te espera en su rama. | O Desafio do Rio de hoje já está pronto. TiTino espera por você no galho dele. | Today's River Challenge is ready. TiTino is waiting on his branch. |
| Crecida te espera | 36 h sin victoria con racha ≥3 y hito 10 no alcanzado; recordatorio sin plazo (la Crecida no caduca, sección 4.3) | Tu Crecida de {n} victorias te espera: una victoria más y llegas al hito {m}. | Sua Enchente de {n} vitórias espera por você: mais uma vitória e você chega ao marco {m}. | Your {n}-win Rising River is waiting: one more win and you reach milestone {m}. |
| Bitácora sin sellar | 20:00 local, si hay casilla pendiente | Hoy no has sellado tu Bitácora del río: {premio} te esperan. | Você ainda não carimbou seu Diário do Rio hoje: {prêmio} esperam por você. | You haven't stamped today's River Logbook: {reward} is waiting. |
| Canoas llenas | Solo si salió con 0 canoas; al llegar a 5/5 | Tus 5 canoas están listas para zarpar. | Suas 5 canoas estão prontas para zarpar. | Your 5 canoes are ready to set off. |
| Evento termina (1.2) | 24 h antes del cierre, si participa y no completó | La Travesía termina mañana: te faltan {n} niveles para la skin. | A Travessia termina amanhã: faltam {n} níveis para a skin. | The Expedition Trail ends tomorrow: {n} levels to go for the skin. |
| Liga cierra (1.2) | Sábado, si está en Oro/Plata o a ≤20 puntos de subir | La Liga del río cierra en 24 h: vas {puesto}.º en tu grupo. | A Liga do Rio fecha em 24 h: você está em {posição}º no seu grupo. | River League closes in 24 h: you're #{rank} in your group. |
| Premio por reclamar (1.2) | Al cierre, si ganó algo (local: se agenda al salir con un premio pendiente) | Tu premio de la Liga del río te espera: {premio}. | Seu prêmio da Liga do Rio está esperando: {prêmio}. | Your River League prize is waiting: {reward}. |
| Racha de Retos | Día 6 de 7 | Un Reto más y completas 7 seguidos: te espera una Pororoca. | Mais um Desafio e você completa 7 seguidos: uma Pororoca espera por você. | One more Challenge for 7 in a row: a Pororoca is waiting. |
| Nuevos niveles | Solo a quien terminó el contenido disponible (local en 1.0: agendado al cerrar la app con `live_content_end_message.next_batch_eta_days`) | Río arriba hay 100 niveles nuevos: el mundo {nombre} te espera. | Rio acima há 100 níveis novos: o mundo {nome} espera por você. | 100 new levels upriver: {name} is waiting. |
| Regreso (D7) | 7 días sin sesión (local: D7/D14/D30 se agendan al pasar a segundo plano y se cancelan al abrir) | Capi te guardó una Cuia extra y 200 semillas. El río sigue donde lo dejaste (nivel {n}). | Capi guardou uma Cuia extra e 200 sementes para você. O rio continua onde você parou (nível {n}). | Capi saved you a Cuia extra and 200 seeds. The river is right where you left it (level {n}). |
| Canoa prestada (2.0) | Un miembro presta | {nombre} te prestó una canoa. | {nome} emprestou uma canoa para você. | {name} lent you a canoe. |

Redacción según glosario §10: tuteo en ES-419, "você" en PT-BR, imperativo en EN, sin exclamaciones dobles, sin emojis en el título. **Con D-013 (2026-10-13, T1-A) la voz de los avisos es TiTino**, con su retrato como icono grande del aviso; Capi solo se nombra cuando trae algo (el regalo de Cuia extra del regreso). Los textos PT-BR y EN que cambian son borrador para el revisor nativo; los del Mapa del río no añaden avisos.

## 9. Re-engagement y ofertas de regreso

| Segmento | Definición | Acción al volver (gratis, siempre) | Oferta (si aplica) | Canal |
|---|---|---|---|---|
| En riesgo | 3 días sin sesión | Nada especial; Bitácora muestra su casilla pendiente | — | Push "Reto listo" o "Bitácora" (1) |
| Dormido | 7-13 días | **Regalo de regreso** (`live_comeback_gift`, 08 §9): 200 semillas + 1 Cuia extra (inventario) + canoas 5/5 + Hoja escudo activa; Crecida intacta (no expira por reloj, sección 4.3) con mensaje "Tu Crecida de {n} sigue en pie"; Bitácora sin reinicio ("Te esperaban {k} páginas") [P] | **Mochila de regreso** 1,99 [P, nombre pendiente de glosario] (`live_comeback_offer_enabled`): 1.200 semillas + Canoas infinitas 1 h (`af_infinite_canoes_1h`); una vez, 48 h, solo si nivel ≥31 (v1.1) | Push local "Regreso (D7)", agendado al pasar a segundo plano (08) |
| Perdido | 14-29 días | Regalo de regreso + tutorial de recordatorio de 1 frase (TiTino desde D-013, T1-A; antes, Capi: "¿Recuerdas? Toca una cuia y luego otra") + el siguiente nivel se marca con etiqueta honesta de dificultad | Mochila de regreso si no se mostró | Push local D14 (variante del D7 con el nombre del mundo) |
| Churn | ≥30 días | Igual que perdido; si hay contenido nuevo desde su última sesión, el mensaje lo nombra | — | Push locales a los 30 y a los 60 días (agendados al salir); después silencio |
| Final de contenido | Nivel máximo = último nivel publicado | Pantalla "El río sigue creciendo", que es el Mapa del río con los ríos que vienen en vidrio: la canoa llega al último tramo publicado y TiTino dice su frase de fin de contenido (D-013; 700 en la 1.0): fecha estimada de los próximos 100 niveles (`live_content_end_message.next_batch_eta_days`), Reto del río, Río sin fin (1.2; 1.3 en la configuración A) | — | Push local "Nuevos niveles" agendado al cerrar la app (en 1.0 no hay FCM; 08 Desviación 8) |

Reglas: el regalo de regreso no incluye Cuia de rescate ni Continuar (research/04 C1 §1 [V], lección Super Duke de Dream Games); no se cambia el tablero del nivel pendiente (dirección 7); no hay "misión de regreso" que exija pagar. Impacto esperado [P]: en el género no hay cifras públicas de reactivación; medir tasa de reactivación por segmento y D7 de reactivados.

## 10. Cadencia de contenido y calendario de live-ops

**Niveles.** Lanzamiento con los niveles **1-700 instalados** en el AAB (`levels_install`, mundos 1-9; 08 §6.1-6.2, canon de entrega para este capítulo), que cubren los ≥600 jugables con QA completa de la dirección 17; los 150-200 primeros ajustados a mano; después +100 niveles cada 1-2 semanas por `levels_remote` (mundos 10-13, niveles 701-1.040, anunciados con `content_version`, sin build ni revisión de tienda) hasta completar la vuelta 1 (~8 semanas) y luego vueltas con variantes de luz y crecida (dirección 17; research/02 C3.4 [E]). Los paquetes `mundo_{nn}_v{vuelta}.json` se entregan por Addressables remoto (Unity CCD o Cloudflare R2) sin pasar por tienda; los fondos de los mundos 1-4 van install-time y el de Loreto (141-200) por Play Asset Delivery on-demand desde 1.0; los niveles 201-700 (mundos 6-9) usan el fondo "río genérico" hasta que lleguen sus packs por PAD, y ese mismo fondo es el respaldo si un pack no está descargado (dirección 16; research/02 C4.6 [V]; 08 §6.2). Magic Sort publica 100 niveles cada ~9 días (research/09 §Por qué ganó 5 [V]).

**Eventos.** Un tipo nuevo cada 6-8 semanas desde 1.2; una instancia de evento temporal por semana en rotación; Temporada de lluvias y Travesía ancladas a fechas locales: Carnaval (feb), Día Mundial del Agua (22-mar), Día Mundial del Medio Ambiente (5-jun) o, previa revisión del asesor cultural, festas juninas / São João (24-jun, Belém: solo fogatas, banderines y comida, sin componente religioso), Día de la Amazonía (5-sep, Brasil), Çairé de Alter do Chão (sep), Halloween, Navidad, San Valentín (research/05 §Implicaciones 9 [E]; research/07 §1 [V] para Çairé). Ninguna ceremonia religiosa o ritual como ancla (dirección 15; glosario §0.3): se descarta Inti Raymi (fiesta incaica al sol, andina y ritual). Audio y arte de los eventos de Manaos: toada amazónica de estilo propio, sin referencia a Parintins ni a sus asociaciones (Garantido/Caprichoso, glosario §3.3); el mismo texto rige en 07 §2.2.

El calendario siguiente corresponde a la configuración de equipo **B** de 09 §2.1 (2 programadores); en la configuración **A** (1 programador), 1.2 llega +12-14 semanas tras 1.1, recortada a P1 (Travesía, Liga del río, Temporada de lluvias, Pase Expedición), y Regata, Encuentro de las Aguas y Río sin fin pasan a **1.3** (09 §3.2).

| Semana desde el soft launch | Contenido | Live-ops | Versión |
|---|---|---|---|
| 0 | Niveles 1-700 instalados (mundos 1-9; ≥600 con QA completa); fondos 1-4 + Loreto (141-200) por PAD; "río genérico" en 201-700 (con D-013, el kit del mundo con el rótulo del sitio, y el «río genérico» de respaldo; 07 §2.1); skins 1-4 | Reto del río, Bitácora, Crecida, Canasta, Dato del río | 1.0 |
| 2 | +100 (701-800, por `levels_remote`) | — | 1.0.x |
| 4 | +100 (900) | Primer ajuste de curva por embudo (Remote Config) | 1.0.x |
| 6 | +100 (1.000); fondos 6-8 por PAD | Cuaderno de campo (retroactivo), logros, Aguas turbias · Bonus, skins 5-13, Mochila de regreso | 1.1 |
| 8 | +40 (1.040: fin de la vuelta 1, mundo 13) | — | 1.1.x |
| 10-12 | Vuelta 2 inicia (+100/2 sem) | Travesía 1 (tema Tapajós), Liga del río, Pase Expedición, Temporada de lluvias 1 | 1.2 |
| 14 | +100 | Regata 1 (1.3 en A) | 1.2 |
| 16 | +100 | Encuentro de las Aguas 1 (tema Manaos) (1.3 en A) | 1.2 |
| 18 | +100 | Travesía 2 (tema Trapecio, Colombia); Río sin fin (1.3 en A) | 1.2.x |
| 20 | +100 | Temporada de lluvias 2 | 1.2.x |
| 22 | +100 | Regata 2 | 1.2.x |
| 24 | +100 | Encuentro de las Aguas 2; cierre del calendario de 6 meses y plan del siguiente | 1.2.x |
| 26+ | +100/2 sem | Aldeas y Río de la aldea cuando DAU ≥ 20-50 k (research/06 C6.3 [E]) | 2.0 |

Temas por país rotan en el orden de los mundos (Pará → Tapajós → Manaos → Colombia → Perú → Ecuador → Bolivia…) para que el arte de evento reutilice el fondo y la skin del mundo correspondiente.

## 11. Roadmap de meta con impacto esperado y esfuerzo

Esfuerzo en semanas-persona de programación para la capa meta (no incluye núcleo, generador ni monetización) y semanas de artista 2D. Equipo vigente: agentes de IA en programación + 1 artista freelance (09 §0; GDD §8, desviación 8); las semanas de programación se leen como tamaño relativo del trabajo. Impacto en puntos porcentuales sobre la línea base de 1.0; son [P] salvo donde se cita el dato.

| Versión | Qué incluye (este capítulo) | Impacto esperado | Esfuerzo 1 prog (configuración A, 09 §2.1) | Esfuerzo 2 progs (configuración B) | Artista | Backend |
|---|---|---|---|---|---|---|
| **1.0** | Primera sesión, Reto del río (3 bandas), Bitácora, Crecida + Hoja escudo, Canasta, Dato del río, Mapa del río, 4 skins por hito, Fruta nueva, push (pre-prompt + 6 tipos, **todos locales en 1.0**: Reto listo, Crecida te espera, Bitácora sin sellar, Canoas llenas, Nuevos niveles, Regreso D7/D14/D30; 08), regalo de regreso, Cántaro (hook), analítica de embudo | Línea base: D1 ≥30 %, D7 ≥10 %, D30 ≥4 % (dirección 19); la racha con perdón es el único sistema con lift publicado (+14 % D7 relativo en Duolingo, research/05 §3 [V], extrapolación [E]) | 7-8 sem | 4-5 sem | 6-8 sem (fondos 1-4, 4 skins, Capi/Rosa, UI de Bitácora/Mapa) | 5 Cloud Functions (SSV, verificación de compras, `restoreLedger`, `voidedPurchasesJob`, `deleteAccount`; 08); sin FCM; 0-10 USD/mes |
| **1.1** (+4-6 sem) | Cuaderno de campo, logros, Aguas turbias · Bonus, skins 5-13, fondos 6-8 por PAD, Mochila de regreso, duplicar casilla con video | D7 +0,5-1 pp · D30 +0,5-1 pp (colecciones en 75 % de los casual, research/05 §3 [V]) | 3-4 sem | 2-3 sem | 6-8 sem | Igual |
| **1.2** (B: +8-10 sem con alcance completo; A: recortada a P1, +12-14 sem, con Regata, Encuentro de las Aguas y Río sin fin en **1.3**; 09 §3.2) | Travesía, Liga del río, Temporada de lluvias, Pase Expedición (hooks de meta), push segmentado por evento (FCM), Canoas infinitas como premio; Regata, Encuentro de las Aguas y Río sin fin (1.2 solo en B) | D7 +1-2 pp · D30 +1-1,5 pp (carreras 68 %, win streak +50 % interanual, competitivos +24 %, research/05 §3 [V]) | 10-12 sem | 6-7 sem | 4-6 sem (variantes lluvia, marcos, skin de temporada, Cuia del Encuentro dorada) | Unity Leaderboards + Cloud Code + Scheduler; 15-40 USD/mes; 0,25 FTE |
| **2.0** | Aldea, Prestar una canoa, mensajes predefinidos, Río de la aldea, identidad sin UGC | D30 +1-2 pp (capa larga Royal Match/Magic Sort: racha → co-op → álbum, research/05 §3 [V]); solo con DAU suficiente para llenar Aldeas | 10-14 sem | 6-8 sem | 3-4 sem | Firestore/Functions o Nakama; 100-300 USD/mes; 0,5-1 FTE |

Criterio de avance: 1.1 solo si D1 ≥28 % en el soft launch (si no, el problema está en núcleo y primera sesión, no en la meta); 1.2 solo si D7 ≥9 %; 2.0 solo con DAU ≥20 k sostenidos (research/06 C6.3 [E]). UA pagada en US solo con D7 ≥15 % y ARPDAU ≥0,08 USD (dirección 19).

## 12. KPIs y dashboard mínimo

Herramientas: Firebase Analytics con exportación a BigQuery (plan Blaze) y un panel en Looker Studio; Crashlytics; Remote Config para actuar (dirección 20). Cohortes por país, versión y fecha de instalación; lectura semanal.

| KPI | Definición | Objetivo | Alarma | Fuente del benchmark |
|---|---|---|---|---|
| D1 / D7 / D30 | Retención clásica por cohorte de instalación | ≥30 / ≥10 / ≥4 % | <28 / <8 / <3 % | dirección 19; puzzle 31,85 / 12,18 / 5,35 % (research/05 §1 [V]) |
| Sesiones por DAU | Sesiones/día | ≥3,5 | <3 | ~4 género; 4,8 Magic Sort (research/05 §1, research/09 [V]) |
| Duración mediana de sesión | Minutos | 5-8 | <4 | 5-6 min mediana (research/05 §1 [V]) |
| Niveles por DAU | Victorias de campaña/día | ≥8 | <5 | 12 en el modelo del jugador medio (research/04 C1 §4 [E]) |
| Embudo por nivel | Abandono, intentos medianos, reinicios, Mareas, Aguas estancadas por nivel | Abandono ≤3 % en normal; ≤5 % en 1-10 | Mediana de intentos >4 en normal → sustituir nivel vía Remote Config | research/02 §7.3 [E]; research/05 [E] |
| Reto del río | % DAU elegibles que lo completan | ≥35 % | <20 % | [P] |
| Bitácora | % DAU que sella; % que llega al día 7 | ≥80 % / ≥40 % | <60 % / <25 % | [P] |
| Crecida | Distribución de racha máxima diaria; % que alcanza 3; escudos usados/DAU | ≥50 % alcanza 3 | <30 % | [P] |
| Push | Opt-in; CTR por tipo | ≥60 % Android; CTR ≥1 % | <45 %; <0,5 % | Android 69 %, CTR 1,05 % (research/05 §5 [V]) |
| Reactivación | % de dormidos (7-13 d) que vuelven en 7 días tras el push | ≥8 % | <4 % | [P] |
| Eventos (1.2) | Participación (% DAU elegibles), finalización, premio reclamado | ≥40 % / ≥25 % / ≥90 % | <25 % / <15 % / <75 % | [P] |
| Liga (1.2) | % DAU elegibles con ≥50 puntos; distribución de tiers | ≥50 % | <30 % | [P] |
| Rewarded | Vistas/DAU; % DAU que ve ≥1 | 1,2-2,0; ≥40 % | <0,8 | 9 de 10 interactúan; retienen 2-7× (research/04 §1.2 [V]) |
| Intersticiales | Impresiones/DAU; D7 de la cohorte con tope alto vs bajo | ≤3,0 | D7 cae >1 pp al subir el tope → bajar | dirección 13; research/04 §1.1 [V] |
| ARPDAU | Ingreso neto/DAU | ≥0,015 LatAm en el soft launch 1 (piso 0,012) · 0,018-0,025 en el soft launch 2 (+BR) · ≥0,05 US orgánico · ≥0,08 US antes de UA pagada | <0,012 LatAm (piso de 05 §9) | 05 §9 (tabla única de metas) [P]; dirección 19 |
| Conversión diaria a pagador | Pagadores únicos/DAU | ≥0,3 % | — | >0,5 % es bueno fuera de RPG (research/04 [V]) |
| Bloqueados sin recursos | % de jugadores que fallan sin canoa ni saldo, por tramo | ≤3 % al 50; ≈8 % al 100; 15-20 % al 200 | >5 % al 50 | research/04 C1 §5 [E] |
| Rescate → victoria | % de Cuias de rescate que terminan en victoria | >85 % | <75 % | research/03 C2.5 [E] |
| Crash-free users | — | ≥99,5 % | <99 % | crashes cuestan vidas y reseñas (research/magic-sort/c §7 [V]) |

**Eventos de analítica** (taxonomía única de 08 §8.2, que fija nombres, parámetros y momento de disparo; este capítulo no usa nombres fuera de ella). Firebase: `level_start`, `level_end {success, level_name}`, `earn_virtual_currency`, `spend_virtual_currency`. Meta de 1.0: `af_daily_challenge {date, result, t_ms, murky_bonus, streak_days}` (Reto del río) · `af_calendar_claim {day, reward_type, reward_value, days_missed_before}` (Bitácora del río) · `af_streak_milestone {milestone, reward_type, reward_value}` · `af_level_fail` con `streak_broken` y `shield_used` (ruptura de la Crecida) · `af_streak_shield_used {streak_at_use}` · `af_basket_open {level, contents_json}` (Canasta) · `af_river_fact_shown {fact_id, dismissed_ms}` (Dato del río) · `af_push_prompt {step ∈ {pre, native}, result}` (pre-prompt y diálogo nativo, sección 8.1) · `af_push_sent` / `af_push_open {type}` · `af_fruit_discovered {tint_id}` · `af_skin_unlock {skin_id, source}` · `af_comeback_gift {days_away}`. Eventos de 1.2: `af_event_join` / `af_event_milestone` / `af_event_claim {event_id, n}` · `af_league_rank {week, bucket_rank, tier}`. Los logros (1.1) y las Aldeas (2.0) darán de alta sus eventos en 08 §8.2 cuando se especifiquen. Dashboard mínimo: 1 página de retención por cohorte, 1 de embudo por nivel, 1 de meta (Reto/Bitácora/Crecida), 1 de monetización, 1 de push/reactivación; alarmas por correo cuando se cruzan los umbrales.

## Parámetros configurables (Remote Config)

**Tabla única de Remote Config: 08 §9** (`RemoteConfigDefaults.json`): clave, tipo, default compilado, rango y dueño. Este capítulo no repite valores; usa exactamente los nombres de 08 §9 y describe el comportamiento que cada clave gobierna.

| Clave (08 §9) | Comportamiento que gobierna (sección de este capítulo) |
|---|---|
| `live_unlock_level_daily` | Nivel que desbloquea Reto del río, Bitácora, Mapa del río, Canasta y Mercado (2.3, 3) |
| `streak_unlock_level` | Debut de la Crecida y la Hoja escudo (3, 4.3) |
| `rescue_min_level` | Canoas, Cuia de rescate y Salto del Boto (3); dueño 04 |
| `live_unlock_level_league` / `_trail` / `_regatta` / `_meeting` / `_village` | Desbloqueos de 1.2 y 2.0 (3, 5, 6, 7) |
| `eco_starter_first_show` | Primera tarjeta de Mochila de inicio: "L12_or_session2" (2.1, 3); dueño 05 |
| `push_preprompt_session` / `push_preprompt_min_level` / `push_quiet_hours` / `push_daily_cap_d0_d7` / `push_weekly_cap_lapsed` | Sesión y nivel ya superado del pre-prompt (primera victoria de la 2.ª sesión con el 10 superado, 8.1), horas silenciosas y topes de envío (8.1-8.2); todas las notificaciones son locales en 1.0 |
| `daily_overrides` / `live_daily_offset` / `lvl_daily_bracket_levels` | Sustituciones fechadas, desplazamiento del índice y umbrales de banda del Reto del río (4.1); los umbrales son de 03 §16 |
| `live_daily_challenge_reward` / `live_daily_streak7_reward` | Premio del Reto (semillas y multiplicador del bonus turbio) y premio por 7 Retos seguidos (4.1) |
| `ui_murky_bonus_enabled` | Tarjeta Aguas turbias · Bonus (4.1): apagada en 1.0, encendida en 1.1; dueño 00 |
| `eco_calendar_table_v1` / `eco_calendar_locked_booster_seeds` / `eco_calendar_double_ad_enabled` | Bitácora: 30 casillas, conversión de comodines bloqueados y ubicación `rv_daily_chest` (false en 1.0, true en 1.1) (4.2) |
| `streak_milestones` / `streak_hits_once_per_day` / `streak_daily_cap` / `streak_shield_per_day` | Escalera de la Crecida (fila única de 05 §2.4), hitos una vez al día, tope diario y Hojas escudo por día (4.3) |
| `streak_reminder_hours` / `streak_restart_counts_as_fail` | Push local "Crecida te espera" sin efecto sobre la racha (8.3); el reinicio voluntario no rompe la racha (false, fijado por la definición canónica de fallo de 04 §5.2) |
| `relief_after_fails` / `restart_confirm_min_pours` | Alivio "El río bajó" y umbral del fallo blando (4.3); dueños 03 y 04 |
| `eco_basket_cycle` / `eco_basket_seeds` | Ciclo determinista y semillas de la Canasta (4.4) |
| `live_events_schedule` / `live_event_active` | Calendario de eventos de 1.2 y definición del evento temporal activo (5.2-5.3, 10) |
| `league_bucket_size` / `regatta_bucket_size` / `league_points` / `league_prizes` / `regatta_prizes` / `league_anticheat_max_wins_per_hour` | Liga del río y Regata (5.3, 6) |
| `live_comeback_gift` / `live_comeback_offer_enabled` | Regalo de regreso y Mochila de regreso (9) |
| `ui_max_takeovers_per_session` | Un solo pop-up fuera de hitos (2.3, 4.7, 5.1); dueño 05 |
| `live_content_end_message` | Pantalla "El río sigue creciendo" y push local "Nuevos niveles" (9, 10) |
| `victory_autonext_s` | Pantalla de victoria sin auto-avance: 0 (int) (4.7); dueño 02 |
| `ui_map_mode` / `ui_map_autopen` / `ui_bg_per_stop` / `ui_stop_label` / `ui_victory_view` / `anim_map_world_ms` | Mapa del río de D-013 (4.5): 100 paradas o el filtro de 13; cuándo sale solo; fondo por parada o el de la gran parada del mundo; rótulo del lugar; victoria con vista; tope del viaje de mundo en la pantalla del Mapa (al acabarse el contenido y con «Ver mapa»; dentro del banner de mundo manda el tope de ≤ 4 s de Q4-D) |
| `ui_map_quiz` | «¿Verdad o mito?» del Mapa del río (D-013, Q12-A): experimento de la 1.1, `off` en la 1.0 |
| `ui_river_fact_every_n_levels` / `ui_fruit_line_every_n_levels` [P] | Cadencias del Dato del río (4.4; pregunta 45 de GDD.md §6.3, decidida el 2026-10-14: (a); D-022): la llegada con su dato, cada 10 niveles, como las paradas (dueño 00), y la línea de fruta, en cada victoria salvo cuando manda una línea de más prioridad (02 §6); 0 la apaga y 2 la deja en una cada dos niveles (D-017, Q2-A) |

Alias retirados que este capítulo ya no usa (lista de 08 §9): `unlock_level_*`, `starter_first_show`, `push_preprompt_level`, `daily_bands`, `daily_offset`, `daily_reward_seeds`, `daily_murky_bonus_multiplier`, `daily_streak7_reward`, `logbook_*`, `eco_streak_rewards`, `streak_daily_cap_seeds`, `streak_shields_per_day`, `chest_cycle`, `event_active`, `comeback_*`, `max_takeovers_per_session`, `content_end_message`. Los parámetros de anuncios, precios y topes de comodines son de 05 y 04 (defaults también en 08 §9).

## Desviaciones de la dirección

Ninguna respecto a las 22 decisiones de dirección. Tres matices sobre el encargo de este capítulo, documentados para que el dueño del producto los confirme:

1. **Permiso de push "tras la primera victoria"**: se pide tras la primera victoria de la 2.ª sesión con el nivel 10 ya superado (para que existan Reto del río y Bitácora de los que avisar), no tras la del nivel 1 ni dentro de la secuencia del nivel 10: en el nivel 1 no hay nada concreto que anunciar y en el 10 ya se encadenan Canasta, Bitácora y Mapa; las guías del género recomiendan el pre-prompt tras un momento de éxito con beneficio tangible y nunca en frío (research/05 §4-5 [V]). Coste asumido: el Reto y la Bitácora del día 2 no se anuncian por push a quien no vuelve por sí mismo; D1 depende del núcleo y de la primera sesión (sección 1). Configurables la sesión (`push_preprompt_session` = 2) y el nivel mínimo (`push_preprompt_min_level` = 10), 08 §9. Esta regla es canon para 02 §13, 03 y 09 (incidencia 15 de GDD.md).
2. **Mochila de inicio**: la dirección 14 abre la ventana en el nivel 10; research/05 pide cero ofertas de pago en la primera sesión. Se resuelve mostrándola en la primera victoria con nivel ≥12 dentro de una sesión ≥2 (`eco_starter_first_show` = "L12_or_session2", canon compartido con 02 §13 y 05 §4.1); dentro de la ventana 10-20 y sin romper la regla de la primera sesión.
3. **Marea ilimitada en los niveles 1-10**: la dirección 9 fija 3 Mareas gratis por nivel sin excepción de tutorial. Este capítulo adopta la excepción que 04 documenta como su propia desviación (04 §Desviaciones 6, `undo_unlimited_until_level` = 10; contrato común a todos los capítulos junto con la Cuia extra regalada en el 8 y los 3 Morphos del 12) para que el contador del nivel 3 y el calendario de la sección 3 digan lo mismo que 04; desde el nivel 11 rige la dirección 9. Si el dueño prefiere 3 desde el nivel 1, basta `undo_unlimited_until_level = 0` y la fila del nivel 3 vuelve a "3/3".

## Preguntas abiertas para el dueño del producto

1. **Definición de fallo para la Crecida**. Cerrada: tabla canónica de 04 §5.2. Solo el fallo duro (Volver a la orilla desde Aguas estancadas) rompe la Crecida, salvo Hoja escudo; el reinicio voluntario con ≥8 vertidos es fallo blando (cuenta para `fails_level`, alivio y Salto del Boto, pero no gasta canoa ni rompe la Crecida); salir al Mapa no es fallo. `streak_restart_counts_as_fail` queda en false (08 §9) y deja de ser una elección de diseño pendiente. *Matizada por D-008 (2026-10-08): desde el 31, rendirse y salir del nivel con el menú son fallo duro y rompen la Crecida salvo Hoja escudo; cerrar la app sigue sin ser fallo.*
2. **Reto del río por bandas**. Cerrada: tres bandas según 03 §16 (K 6-7 / 8-9 / 10-11; 730 × 3 = 2.190 tableros; umbrales `lvl_daily_bracket_levels` = [41, 201]). El tablero único para la conversación social se reconsideraría, si acaso, con las Aldeas de 2.0.
3. **Regalos de tutorial**. Cerrada: se mantienen como ítems de inventario, 1 Cuia extra regalada en el nivel 8 y 3 Morphos en el 12 (02 §13, 04 §2.2-2.3; excepción de tutorial a la dirección 9 documentada en 04 §Desviaciones y aquí en Desviaciones 3).
4. **Canasta grande** (día 30 de la Bitácora y cada 100 niveles): confirmar nombre y contenido propuesto (150 semillas + 1 Cuia extra + 1 Morpho + 3 Marea) y añadirlo al glosario.
5. **Mochila de regreso** 1,99 (oferta de regreso, v1.1): confirmar que entra al catálogo y fijar su nombre en el glosario.
6. **Duplicar casilla de la Bitácora con video** como placement "cofre diario". Cerrada en lo que fija el canon: la ubicación `rv_daily_chest` entra en 1.1 (`eco_calendar_double_ad_enabled` = false en 1.0; 05 §6.2 deja cinco ubicaciones rewarded en 1.0; 09 §3.1). Queda solo medir en 1.1 el impacto estimado (≈+600 semillas/mes por jugador medio [E]).
7. **Cuia del Encuentro** es a la vez la skin del mundo 3 (nivel 80) y el premio del evento Encuentro de las Aguas: ¿se acepta la variante dorada anual para quien ya la tiene, o el evento premia otra cosa?
8. **Premio de racha vs rescate**. Cerrada: escalera fija de 05 §2.4 (3 → 300 semillas · 6 → Canasta · 10 → 1 Cuia extra + río crecido; cada hito una vez al día local; tope 600; `streak_milestones`) y Cuia de rescate a 600 fijo desde el 31, 300 el primer rescate del día (`rescue_price_bands` = [[31,600]], sin tramos ni cohortes geográficas). La regla "premio > coste de un continue" se aplica al total de la escalera (≥720 semillas-eq contando el río crecido) y al primer hito ≥300 (primer rescate del día); los hitos 6 (Canasta ≈116) y 10 (Cuia extra 150 + río crecido) valen menos que un rescate por sí solos y se pagan una vez por día.
9. **Regata sin bots**: aceptar cubetas incompletas con premios proporcionales a cambio de no simular rivales. Alternativa: inscripción global sin bandas de progreso para llenar más rápido.
10. **Meta decorativo** (reconstruir ecosistema por país, research/09 §Mejorar [V]): no está en la dirección; ¿se evalúa para 2.x como palanca de D30 (Hexa Sort >15 % D30 con pueblo coleccionable, research/09 [V])?
11. **Fechas de live-ops locales**: confirmar con el asesor cultural el uso del Çairé, del Día de la Amazonía y de las festas juninas / São João (24-jun) como temas (si São João no pasa la revisión, queda solo el Día Mundial del Medio Ambiente, 5-jun), y descartar cualquier fiesta con marcas de asociaciones o carácter ritual. La investigación que acompaña a D-007 pide tratar las fiestas religiosas (Círio de Nazaré, Çairé, Marujada) solo por el ángulo de la comida o la cosecha (07 §8.1), y la lista del asesor de su propuesta deja fuera el Çairé y el Círio (§(i)).

## Dependencias con otros capítulos

Numeración canónica de capítulos según el glosario (00 Glosario · 01 Visión · 02 Núcleo · 03 Niveles · 04 Comodines · 05 Economía · 06 Meta · 07 Arte · 08 Técnica · 09 Roadmap); no existe un capítulo "ASO y legal": sus temas viven en 05 §7, 08 §13 y 01 §9.

| Capítulo | Toma de este capítulo | Entrega a este capítulo |
|---|---|---|
| 00 Glosario | Todos los nombres usados | Alta de Canasta grande y Mochila de regreso si se aprueban; numeración canónica de capítulos |
| 01 Visión y posicionamiento | Calendario de desbloqueos (3), primera sesión (2) y metas de retención (12) | Metas de negocio y fases de mercado (01 §7-8); nombre comercial y riesgos de marca (01 §9) |
| 02 Núcleo, reglas y UX | Desbloqueos de meta sobre los niveles 1-12 (Cuia extra regalada en el 8, Morpho en el 12, secuencia del nivel 10, pre-prompt de push en la 2.ª sesión y no en el nivel 10) | Tabla canónica de los niveles 1-12 (02 §13: excepciones de 1 vacío en 1 y 6, Marea en el 3, presentación del Reto del río en el 7, Volver a la orilla en el 6/8, Mochila de inicio con `eco_starter_first_show`), copiada sin alterar en 2.2; debuts de mecánicas (lista única de 02 §9.1 y `mech_debut_level`: 14 Cuia chica · 27 Cuia alta · 43 Hoja · 63 Matapi · 83 Liana con fruta · 100 Aguas turbias · 103 Remolino · 123 Barro · 143 Lluvia · 163 Brote · 183 Piraña · 323 Tinaja (M13, D-019: en la 1.0 si cumple su condición; si no, el mismo nivel en la 1.1); la Liana de cuenta (con número) es una variante, en el 243; D-007) y capacidades (02 §8: Cuia chica 14, Cuia alta de 5 desde el 27 y de 6 solo en hitos desde el 141) para el calendario de la sección 3 y las reglas alteradas de Travesía; pantalla de victoria sin auto-avance (`victory_autonext_s` = 0, 02 §6) |
| 03 Niveles, generación y dificultad | Especificación del pool del Reto del río (730 × 3 bandas = 2.190 tableros), paquetes de Travesía y Encuentro de las Aguas; fallo de bot objetivo por nivel en 1-12 (2.2; par y aritmética de cuias, colores y vacíos según 02 §13) | Pipeline que produce los pools fechados; bandas del Reto (03 §16: K 6-7 / 8-9 / 10-11, `lvl_daily_bracket_levels` = [41, 201]); posiciones de Difícil/Muy difícil/hito del ciclo de D-009 (retos p6, p9, p12, p15 y p18, remanso p19, hito p20); primer Difícil 40 (`lvl_labels_from_level`) y primer Muy difícil 60 (`lvl_very_hard_from_level`), 03 §12 |
| 04 Comodines, vidas y rescate | Regalos de tutorial, comodines de Bitácora y Canasta, topes de la Crecida, conversión de comodines bloqueados | Definición canónica de fallo (04 §5.2: fallo duro / fallo blando ≥8 vertidos / cerrar la app no es fallo; desde el 31, rendirse y salir del nivel con el menú sí, D-008); Marea ilimitada en 1-10 (`undo_unlimited_until_level` = 10) y 3 por nivel desde el 11; Cuia de rescate desde el 31 (`rescue_min_level`), máximo 1 por nivel; precios finales, topes por día y reglas de inventario (persistencia de Cuia extra al reiniciar) |
| 05 Economía y monetización | Ventanas de Mochila de inicio, Sin anuncios, Cántaro, Balsa de rescate, Pase Expedición, Mochila de regreso; placement "cofre diario" (1.1); Canoas infinitas (`af_infinite_canoes_1h`) como premio de evento | Escalera de la Crecida (05 §2.4, fila única; `streak_milestones`, `streak_hits_once_per_day`, `streak_daily_cap`); Cuia de rescate a 600 fijo (`rescue_price_bands` = [[31,600]]; primer rescate del día 300); catálogo de SKUs y contenidos (05 §4-5); reglas de intersticiales y topes rewarded (05 §6) y política de supresión tras IAP; metas de ARPDAU (05 §9); textos y bases legales del consentimiento por país (05 §7) |
| 07 Arte, temática y audio | Pantallas de Bitácora, Mapa del río, Crecida (medidor, hoja), Fruta nueva, Canasta visible, marcos de país, skin de temporada, Cuia del Encuentro dorada; animaciones de TiTino (victoria, Nivel fallido, inicio y Mapa), Capi (regalo, rescate y canoa del Mapa) y Rosa (Mapa); fondos por parada y piezas del Mapa del río (D-013; 07 §2.1 y §11); literal de la toada amazónica sin referencia a Parintins (10 = 07 §2.2) | HUD del nivel (07 §7.4, dueño del HUD: barra vertical de la Crecida en el borde derecho, replicada en grande en la victoria); hojas de estilo; LUT de variantes de luz y lluvia; textos verificados de los Datos del río con el asesor cultural |
| 08 Arquitectura técnica y publicación | Lista de lo que necesita servidor (5.4), Unity Leaderboards, Cloud Code, Scheduler, notificaciones locales en 1.0 y FCM en 1.2, Addressables/PAD para paquetes de evento; Canasta determinista y visible, Aldeas sin UGC y textos de push en tres idiomas para IARC/ClassInd y Data Safety (08 §13) | Tabla única de Remote Config (08 §9: nombres, tipos, defaults y rangos, con `victory_autonext_s` = 0 int, `eco_calendar_double_ad_enabled` = false en 1.0, `ui_murky_bonus_enabled` = false en 1.0 / true en 1.1 y `push_preprompt_session` = 2 / `push_preprompt_min_level` = 10); taxonomía única de analítica (08 §8.2, que da de alta los eventos de meta de la sección 12: `af_fruit_discovered`, `af_skin_unlock`, `af_comeback_gift`, `af_event_join` / `af_event_milestone` / `af_event_claim`, `af_league_rank`, `af_push_sent` y `af_push_open`); 5 Cloud Functions (SSV, verificación de compras, `restoreLedger`, `voidedPurchasesJob`, `deleteAccount`); entrega de niveles (1-700 instalados, `levels_remote`, 08 §6.2); guardado en nube, cola offline, App Check, exportación a BigQuery; checklists de publicación, consentimiento y textos legales (08 §13) |
| 09 Roadmap, equipo, KPIs y riesgos | Roadmap de meta por versión (11), calendario de live-ops (10), KPIs de retención (12) | Configuraciones de equipo A/B (09 §2.1) y alcance de 1.1/1.2 por configuración (09 §3.1-3.2: en A, Regata, Encuentro de las Aguas y Río sin fin pasan a 1.3); umbrales de KPI de 09 §6.1 |
