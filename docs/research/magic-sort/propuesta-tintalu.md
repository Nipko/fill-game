# Tintalú frente a Magic Sort!: qué copiamos, cómo y qué mejoramos

**Proyecto:** Amazon Fill (Tintalú) · **Fecha:** 2026-10-07 (revisada el 2026-10-08) · **Tipo:** propuesta, **no canon** (CLAUDE.md, fuente 4). No edita el GDD ni las decisiones D-001 a D-004. Los cambios al GDD van como lista en la sección 10, para que el dueño los apruebe.

> **Respuestas del dueño (2026-10-08).** 10.2 #1 → B, `run` (D-005); #3 → A, pero sin reservar el hueco del vertido (D-006, punto 4); #4 → A, opción (a) de §3.4 (D-006, punto 3); #5 → B, pose medida y sin techo (D-006, punto 1); #6 → A, boca a 0,2-0,6 u; #7 → A, ≈ 0,9 s (D-006, punto 2); 4.3.5 → se mantienen 16 dp. Siguen abiertas #2 y #8-#25. Registro en `docs/dev/decisiones.md`. El resto del documento no se reescribe: es histórico.

> **Respuestas del dueño (2026-10-08, D-008: bucle de partida como Magic Sort).** Se copia la estructura del embudo de §6.1 con nuestras cifras:
> - Los pasos 2 («Continue?») y 3 («Level Failed!») de §6.1, que esta propuesta no copiaba, ahora se copian: «¿Seguro?» con una segunda oferta de rescate y «Nivel fallido» con el premio que no se cobra.
> - Por eso dejan de valer las filas de §6.2 sobre la misma oferta dos veces y el premio que no se cobra, y el punto 8 de §1 («un solo modal… reinicio en 1 toque»).
> - La mejora del paso 6 (cargar el nivel tras la Canoa extra) no se adopta: se vuelve a «Nivel fallido» con «Reintentar» [P], como Magic Sort.
> - Desde el 31, salir del nivel cuesta una canoa.
> - #20 → no se adopta otra distribución en cada reintento (D-008, punto 7): el GDD no lo permite. La pregunta sigue abierta.
> - #25 sigue abierta (sin «¡Perfecto!»).
> - Postura del dueño, ya anotada en #14: las Canoas infinitas no quitan los intersticiales.
> - Preguntas nuevas P14-P16 en `docs/dev/decisiones.md`.

> **Respuestas del dueño (2026-10-19, D-029: «si tienen recomendadas vamos con esas»).** De 10.2: #8 → A (ya en D-007); #10 → A (Aguapé y Nasa al backlog tras el 183, con el asesor); #11 → A (intersticiales como están, vigilando); #12 → confirmada (G y categorías sensibles bloqueadas en todas las bandas y prueba del Close: 05 §7); #13 → A, acción del dueño (grabaciones); #16 → ya respondida por D-013; #17 → B (bono diario: elegir 1 de 3 premios visibles); #18 → B (un panel de «premios del día» tras ganar); #19 → C (hitos 3 · 6 · 10 hasta simular B; es lo que hace la Crecida); #21 → B (un reloj diario común con un solo evento temporal); #22 → B (sin canoas infinitas en la Mochila de inicio); #23 → A (oferta temporal como icono del inicio desde el 31, con cuenta atrás y sin abrirse sola: 05 §5); #24 → B (ya en level-qa: QA-EARLY-TRAPS, D-007 Q16 B); #25 → A («¡Perfecto!» solo sin Cuia de rescate: 02 §6, tarea DC14). #17, #18, #21 y #23 son de sistemas que aún no existen: quedan registradas, con una nota en 05 y 06. **#15 no se acepta: ya estaba cerrada** por D-007 Q17 A (ninguna mecánica tapa contenido) y por D-019, que llevó el contenido oculto a la Tinaja. Siguen abiertas, sin recomendación aplicable, #2, #9, #14 y #20 (reabren decisiones del canon). P14-P17 de D-008 quedaron decididas el mismo día con su recomendación (P17: A).

**Base.**
- Observación: `docs/research/magic-sort/ficha-magic-sort.md` (en adelante, «la ficha»), con la grabación de 20 min del dueño (niveles 387-389), la hoja de vertido por adb y el vídeo de Love Water.
- Canon: `docs/GDD.md` (§2, §4, §6, §7), capítulos 00, 02, 03, 04, 05, 06, 07 y 08 §9, y `docs/dev/decisiones.md` (D-001 a D-004 y sus preguntas P1-P10). Composición actual del tablero: `docs/art/direccion-visual-v2.md` §3.2, `BoardLayout.Compute` y `GameScreen.PourCeilingFor`.
- Método del dueño (2026-10-07): «no vamos a inventar la rueda: copiamos lo que funciona y cómo se hace, con nuestra temática, y lo mejoramos».

**Convenciones.**
- «t = N s» es el segundo de la grabación de Magic Sort (MS). En la sección 9, t es el segundo del vídeo de Love Water (LW). «Hoja de vertido, N ms» remite a la captura por adb con una viñeta cada 100 ms.
- **[P]**: propuesta de este documento, a calibrar. **Cálculo**: cuenta hecha aquí con datos de la ficha o del GDD, no es dato del juego. **Deducido**: inferencia de la ficha. **No observado**: no se ve en el material.
- **Escala.** Paso las medidas de MS a nuestra escala de referencia: 360 × 780 dp = 1080 × 2340 px, es decir, **3 px por dp** (02 §5). Es justo la resolución de la grabación. La ficha usa 411 dp de ancho; aquí no.
- **u (Tintalú)** = altura de una dosis = ancho de la cuia / 2 (07 §4.1; D-004). Esta propuesta no cambia esa definición.
- **u (MS)** = 1/6 del alto exterior de la botella, para comparar a igual proporción (69 px a 2340; 57 px a 1920). Una dosis de MS mide ≈ 1,1 u (MS).
- Las propuestas de forma de la cuia se dan en **fracciones del ancho y del alto exterior**, no en u (sección 3.4).
- Nombres del glosario (`docs/gdd/00-glosario.md`). Lo que toca fauna, flora o pueblos y no está en el glosario va marcado **[P] · pendiente de revisión cultural**.
- **Lo que no se copia nunca:** la presentación de MS (gatos, caldero, corcho dorado, oro biselado, calaveras, botella con labio, armarios naranja con candados de colores). Se copian reglas, cifras, tiempos y sincronías (07 §1.3; D-003).
- Las referencias G1-G23 y F1-F25 son los ganchos y fricciones de la ficha §12.

---

## 1. Resumen para el dueño (12 puntos)

1. **Cuias más grandes en los tableros grandes, con hueco para verter.** Con 13-14 botellas, MS usa 3 filas de 3-6 y botellas de **45 × 138 dp** a nuestra escala (t = 234, 698). Nosotros, 2 filas de 7 con cuias de **34 × 102 dp** (02 §5). Propongo 3 filas de ≤ 5 desde 11 cuias, reservando el hueco del vertido sobre la fila 1: la cuia pasa a **≈ 43,6 dp** (+28 % con 13-14 cuias). Sin ese hueco, el origen entraría en la fila del HUD (sección 3.3). Cambia una consecuencia fijada en D-004 (el número de filas) y depende del ángulo (punto 4).
2. **Más líquido dentro de la cuia.** En MS el líquido ocupa el 89 % del ancho de la botella y el 74 % de su alto (ms-02). En nuestra cuia, el 65 % y el 67 % (07 §4.1). Propongo un ancho interior ≥ 0,80 del exterior y 4 dosis ≈ 0,72 del alto exterior, sin copiar la silueta con labio. Reabre el punto 5 de D-003 (pregunta 4.4).
3. **Vertido más pausado, con la boca casi tocando el destino.** MS tarda 0,9-1,0 s de la salida al reposo, sin contar la subida, y deja la boca a 0,1-0,55 u (MS) del destino (hoja de vertido; t = 250, 776). Nosotros, ≈ 0,54 s de la salida al reposo (0,66 s con la subida de 120 ms) y 0,6-1,6 u (02 §4.1; D-004). Propongo ≈ 0,8-0,9 s y la boca a 0,2-0,6 u; esto último cambia la decisión 2 de D-004. La cola de toques evita que un ciclo más largo frene al jugador.
4. **El ángulo es decisión tuya.** MS inclina **66°** cuando a la botella le quedan 3 dosis y **≈ 92°** cuando se vacía (hoja de vertido; t = 250). Love Water hace lo mismo (55° a 78-80°). Nosotros, 38° fijos (D-004). Con 38° el agua de una cuia casi vacía no llegaría a la boca. Pero la pose medida reabre la inclinación dinámica que D-004 descartó y no cabe bajo el techo del vertido de D-004 (sección 3.3). Recomiendo verlas lado a lado en el teléfono (pregunta 4.3.3).
5. **D-001: nueva evidencia para P6.** MS y LW vierten el tramo entero (t = 250, 598, 600, 838; LW t = 132-134). D-001 está decidida; P6 pregunta si sigue en 1.0. Recomiendo jugar 10 min en `run` antes de confirmar o sustituir D-001. Si no hay preferencia clara, mantener D-001 (pregunta 4.1). El recuento de dosis por vertido de MS es una estimación de confianza media y no es el argumento principal.
6. **D-002 coincide.** MS no resalta destinos ni da pistas en 387-389 (t = 232, 598). Queda una duda: nuestra pista por inactividad desde el nivel 3; reducirla revierte el contrato 9 (pregunta 4.2).
7. **«Cada color completado destapa algo».** Es el gancho de las mecánicas de MS (telas, hielo, armarios; G1). Lo copiamos con lo que ya tenemos: Liana que se suelta al sellar una fruta concreta (= tela), Hoja (= botella atada). Y dos ideas para después: Aguapé (= hielo) y Nasa con pez (= armario y llave), [P] y con revisión cultural.
8. **Fallo: copiamos el momento, no la presión.** Bloqueo automático, +1 cuia que cumple, saldo a la vista: igual que MS. Pero un solo modal (MS ofrece 900 monedas dos veces seguidas, t = 84-86), Marea primero, 600 semillas (300 la primera del día) y reinicio en 1 toque (MS: 3 toques). Desde el 31, quedarse sin vertidos y volver a la orilla también cuesta 1 canoa, igual que en MS. La diferencia: se avisa antes de decidir y reiniciar por voluntad propia no cuesta (04 §5.2).
9. **Anuncios y meta sin patrones oscuros.** MS pone un intersticial tras fallar (t = 196) y otro que corta la celebración (t = 629). Su «Close» no respondió a algunos toques (t = 212, 638, 652) y anuncia trading (Kalshi). Nosotros: solo tras victoria y con toque; Close probado como requisito de política; clasificación G y categorías sensibles bloqueadas en todas las bandas (hoy 05 §7 solo lo exige en 13-17). En meta copiamos primero las rachas (ya son la Crecida). Las vidas ilimitadas cortas como premio gratis reabren una exclusión del canon (04 §7.1) y piden otra simulación (pregunta 10.2 #9). Las de 15 min a cambio de un anuncio, que pediste, van en la pregunta 10.2 #14: en la grabación no aparecen.
10. **Audio: copiar la sincronía y la mezcla.** El gesto es lo más bajo (−41 a −27 dBFS) y el premio lo más alto (monedas −10,5): 20-28 dB entre uno y otro. El corcho cae siempre 1,15 s después del inicio del sonido del vertido que completa, que arranca con la botella ya inclinada (≈ inicio del chorro). Nos faltan sonidos de deseleccionar, del botón Jugar, de comodín sin saldo y de racha perdida. La música y la vibración de MS no se observaron: hay que grabarlas.
11. **Temáticas desbloqueables.** MS esconde una escena de historia cada 20 niveles (1-99) con una pestaña «USE» en su mapa Journey (t = 1190-1202). Propongo escenas de Capi y Rosa por tramo, galería en el Mapa del río, «Usar» como fondo del lobby y la siguiente en silueta (sección 7.4; pregunta 10.2 #16).
12. **Lo que el canon descarta y MS usa como gancho.** Contenido oculto en niveles normales (sección 5.4), bono diario con elección, cadena de premios tras ganar, multiplicador de racha, variante en cada reintento y reloj común de eventos (sección 7.3). Cada uno va como pregunta cerrada, con el § del GDD que habría que reabrir.

---

## 2. Tabla maestra

Columnas: elemento de MS → qué hace (con cifra y t) → versión Tintalú → qué dice hoy el GDD → estado → prioridad.
- **Estado:** coincide · difiere · falta (el GDD no lo trata) · excluido (el canon lo descarta a propósito). «Canon; pregunta» = el canon lo decidió antes del método del dueño y vuelve a preguntarse.
- **Prioridad:** **v0.2** (siguiente prototipo, niveles 1-10) · **1.0** · **después** (1.1 o más).
- Las filas nuevas llevan los números 70-73, para no mover las referencias a las filas 1-69.

### 2.1 Tablero, control y vertido

| # | Elemento de MS | Qué hace exactamente | Versión Tintalú | GDD hoy | Estado | Prioridad |
|---|---|---|---|---|---|---|
| 1 | Capacidad 4 | 4 dosis en todas las botellas vistas (t = 0, 496, 684) | Cuia de 4 | 02 R1 | Coincide | — |
| 2 | Vertido por tramo | Mueve todas las dosis seguidas del color de arriba: 2 en t = 250 y 430; 3 en t = 598, 600 y 838 | Hoy, una dosis (`single`) | 02 §1.2 R9; D-001 | Difiere | v0.2 (pregunta 4.1; P6) |
| 3 | Sin límite de movimientos ni tiempo | Sin contador (t = 190) | Igual | 02 §1, R12 | Coincide | — |
| 4 | Selección | Solo sube la botella: 52 px a 2340 (**17 dp**; 0,125 del alto) en ≤ 100 ms. Sin brillo ni escala (t = 232; hoja de vertido 1300-1400 ms) | Sube 16 dp en 120 ms; iconos de fruta visibles | 02 §4.1; 07 §3 regla 2 | Coincide | — |
| 5 | Sin resaltado de destinos | Ninguna otra botella cambia (t = 232, 598) | Igual | 02 §3.1 C1; D-002 | Coincide | — |
| 6 | Deseleccionar con sonido | Vuelve a tocar y baja, con un «tok» de 90 ms (t = 562, 690, 870) | `sfx_deselect` [P] | 02 §4.1 (deselección sin audio) | Falta | v0.2 |
| 7 | Ciclo de vertido | Viaje 200-300 ms, chorro ≈ 300 ms, vuelta ≈ 300 ms: **0,9-1,0 s** de la salida al reposo, sin la subida (hoja de vertido, 2900-3800 ms) | ≈ 0,8-0,9 s de la salida al reposo [P] | 02 §4.1: 200 + 180 + 160 ≈ 540 ms de la salida al reposo (660 ms con la subida de 120) | Difiere | v0.2 (sección 3.2) |
| 8 | Ángulo de vertido | **66°** con 3 dosis restantes; **≈ 92°** al vaciarse (hoja de vertido, 3400 ms; t = 250, 776) | 38° (D-004) o pose medida | 02 §4.1; 07 §4.3 y §5.2; D-004 (descartó la inclinación dinámica) | Difiere | v0.2 (pregunta 4.3.3) |
| 9 | Boca sobre el destino | Sobre el eje (±3 px) y 5-37 px sobre el labio: **0,1-0,55 u (MS)** (hoja; t = 250, 776) | 0,2-0,6 u [P] | 0,6-1,6 u (D-004, decisión 2; 07 §5.2) | Difiere | v0.2 (pregunta 4.3.4) |
| 10 | Líquido horizontal en la botella inclinada | t = 250 | Igual (el shader no rota el líquido) | 02 §4.1; 07 §4.3 | Coincide | — |
| 11 | Origen por delante y sin escalar | Se dibuja delante de las vecinas, escala 1,00 (t = 250) | Igual, bajo el techo del vertido | D-004 (techo) | Coincide | — |
| 12 | Destino quieto con burbujas | Burbujas del color en la superficie (hoja, 3500-3700 ms; t = 80, 838) | Onda y burbujas | 07 §4.2 | Coincide | — |
| 13 | Sellado | Corcho dorado; espiral de destellos **del color del líquido**; el corcho cae a **+1,15 s del inicio del sonido del vertido** que completa, que arranca con la botella ya inclinada (≈ inicio del chorro; t = 26, 476, 538, 852; audio) | Tapa de calabaza con destellos del tinte; tapa a +1,15 s del inicio del chorro [P] | 07 §4.4 y §5.3: aro marfil; tapa a +0,10-0,25 s tras la 4.ª dosis | Difiere | v0.2 |
| 14 | Bloqueo automático | Detecta que no hay vertido legal (16 veces). Aviso a 1,32-1,6 s del inicio del último vertido (t = 84; audio) | Aguas estancadas | 02 R13 y R15 (400 ms tras la animación) | Coincide | v0.2 |
| 15 | Bucles sin progreso | No detecta movimientos inútiles: **no observado** | Regla de fútiles | 02 R13 y R16 | Mejora nuestra | — |
| 16 | Variante nueva en cada reintento | 4 variantes en el 387 (orden A, B, A, C, A, D) y ≥ 5 en el 389, sin aviso (t = 0, 9, 144, 342) | Misma semilla; alivio al 3.er fallo con «El río bajó» | GDD §2 #11; 02 §2; 04 §2.7; 05 §10 | Difiere (canon; pregunta 10.2 #20) | — |
| 17 | Entrada del tablero | Desliza desde la derecha con un «fuuush», 0,24 s tras el botón (t = 9, 228; audio) | Sumergir y teñir | 02 §4.2; dirección 18 | Difiere (decidido) | — |
| 18 | Filas y tamaño de botella | Siempre 3 filas de 3-6; misma botella en los 3 niveles: **45 × 138 dp** (t = 234, 496, 698, 1154). Sin techo: el origen tapa el rótulo del nivel (t = 250) | 3 filas de ≤ 5 desde 11 cuias, con el hueco del vertido reservado: ≈ 43,6 dp [P] | 02 §5: 2 filas de hasta 7; 34 × 102 dp. D-004 fija el número de filas y el techo del vertido | Difiere | 1.0 (pregunta 4.3.1) |
| 19 | Proporción de la botella | Alto/ancho = 3,07 (ms-02) | Cuia 1:3 | 07 §4.1 | Coincide | — |
| 20 | Líquido ancho | Líquido = 0,89 del ancho; 4 dosis = 0,74 del alto (ms-02) | Ancho interior ≥ 0,80 del exterior; 4 dosis ≈ 0,72 del alto exterior [P] | 07 §4.1: 1,30 de 2,00 u (0,65); 4,00 de 6,00 u (0,67). D-003, punto 5 | Difiere | v0.2 (pregunta 4.4) |
| 21 | Cara superior en elipse | Elipse más clara de ≈ 0,4 dosis de alto (ms-02) | Elipse de ≈ 0,4 de dosis [P] | 07 §4.2: menisco de 2-3 px. D-003, punto 5 | Difiere | v0.2 (pregunta 4.4) |
| 22 | HUD de partida | Cofre de racha «0/2», engranaje, píldora «Level N». **No** muestra vidas; las monedas solo en el 388, en el hueco de la racha (t = 0, 496, 938) | Pausa, Nivel, Semillas, Canoas, Volver a la orilla; Crecida a la derecha | 02 §5; 07 §7.4 | Difiere (mejora nuestra: saldo visible) | — |
| 23 | Barra de comodines | 3 botones con precio: Deshacer 300, flechas cruzadas 300 (barajar, deducido por el icono), Añadir botella 900. 224 × 79 dp, a 21 dp del borde (t = 0, 234) | Bandeja cumbuca, 4-5 ranuras | 02 §5; 07 §7.4 | Coincide en forma | — |
| 24 | Fondo oscuro detrás de las botellas | Cielo azul marino ≈ #03082e con estrellas (t = 230) | Panel azul petróleo (#234452 → #142C36) | D-004; guía visual v2 | Coincide en función (MS valida D-004) | — |

### 2.2 Mecánicas especiales

| # | Elemento de MS | Qué hace exactamente | Versión Tintalú | GDD hoy | Estado | Prioridad |
|---|---|---|---|---|---|---|
| 25 | Capa oculta «?» | Dosis negra con «?», se revela al quedar arriba. Hasta 3 seguidas (t = 712); 16 ocultas en el 388, un nivel normal (t = 496) | Aguas turbias | 02 §11: solo Difícil/Muy difícil desde el 100; ≤ 3 cuias y ≤ 4 dosis | Difiere (pregunta 10.2 #15) | — |
| 26 | Revelado de «?» | El «?» se deforma y se disuelve con destellos de estrella en < 2 s; sin sonido (t = 238, 564) | Revelado con destellos (< 2 s) y gota [P] | 02 §4.1 y 07 §4.3: 300 ms | Difiere | 1.0 |
| 27 | Botella atada | Cuerda y etiqueta con un frasco de color. Solo acepta ese color (deducido, t = 888); puede verter (t = 776); la cuerda sigue tras sellar (t = 26, 362) | Hoja | 02 §9.1 M1 (43): vacía, no es origen, se abre con el primer vertido | Difiere en parte | 1.0 (sección 5) |
| 28 | Tela con icono | Tapa una botella entera. Se retira al completar una botella del color del icono: ✓ verde, haz de luz y la tela sube (t = 27, 56, 130, 362, 398) | **Liana con fruta** [P]: se suelta al sellar una cuia de esa fruta. Si además tapa el contenido: pregunta 10.2 #15 | 02 §9.1 M3 (83): se suelta al sellar n cuias, sin color | Difiere | 1.0 (pregunta 10.2 #8) |
| 29 | Hielo por fila | Bloque de hielo en la base de una fila. Recibe vertidos (t = 502). Revienta toda la fila al completar una botella dentro (t = 504, 508-510) | **Aguapé** [P] · pendiente de revisión cultural | No existe (Brote, M2, es por cuia) | Falta | Después |
| 30 | Armario con candado y llave | Tapa 2 o 4 botellas. La llave va dentro de una dosis; al quedar arriba vuela al candado de su color y el armario revienta (t = 696, 712, 812-814) | **Nasa** con **pez** [P] · pendiente de revisión cultural | No existe | Falta | Después |
| 31 | SUPER HARD | Anunciado en el botón de Home: rojo, «x5» (t = 666). Calaveras y cinta en el tablero (t = 684). Premio mostrado 100 frente a 20 (t = 748; ×5 deducido; no se cobró ninguno) | Muy difícil (jaguar, 80 semillas) | 02 §10; 07 §7.7 | Coincide (sin calavera) | 1.0 |
| 32 | Botella añadida | Aparece en la fila que tiene hueco, con anillo de chispas, y la fila se recentra ≈ 27 dp (t = 402-410) | Cuia extra / Cuia de rescate en la fila más corta, recentrada; el número de filas no cambia a mitad de nivel [P] | 02 §8: «al final de la última fila»; D-004: re-layout con el T nuevo | Difiere | 1.0 |

### 2.3 Fallo, victoria, vidas y anuncios

| # | Elemento de MS | Qué hace exactamente | Versión Tintalú | GDD hoy | Estado | Prioridad |
|---|---|---|---|---|---|---|
| 33 | «No Available Moves» | Saldo arriba; «Play On» 900 (+1 botella) y «Give Up». Sin anuncio ni deshacer (t = 84, 404) | Aguas estancadas: Marea → Cuia de rescate 600 (300) → Volver a la orilla | 02 §7; 04 §6 | Difiere (mejora nuestra) | v0.2 (rama C) / 1.0 |
| 34 | «Continue?» | Segunda oferta de 900 con «You will lose 1 life!» (t = 86, 194, 746) | No existe: un solo modal | 04 §6.6 («máximo un modal por bloqueo») | Mejora nuestra | — |
| 35 | «Level Failed!» | Enseña el premio perdido («Rewards: 20» o 100) y «Try Again» (t = 6,5, 748) | No existe: reinicio directo | 02 §2; 04 §2.7 | Mejora nuestra | — |
| 36 | Reintentar sin pagar | 3 toques: «Give Up», X y «Try Again», y −1 vida (ficha §4.4) | 1 toque: Volver a la orilla. Desde el 31, quedarse sin vertidos y volver desde el modal también cuesta 1 canoa (salvo Canoas infinitas); se avisa en el modal antes de decidir | 04 §5.2; §6.5-§6.6 | Mejora nuestra (menos toques; mismo coste en vidas) | — |
| 37 | Vidas | 5 como máximo (ms-01); **una cada 30 min, deducido de 4 lecturas y a confirmar** (t = 88, 288, 488, 664); −1 al rendirse o salir | Canoas: 5, una cada 25 min, desde el 31; solo el fallo duro cuesta | 04 §7.1; 08 §9 (`canoes_regen_min` = 25) | Difiere (más generoso) | 1.0 |
| 38 | «More Lives» | Con 0 vidas: «Refill» 900 o «+1 Life» con un anuncio (t = 287-289) | Sin canoas: Canoa extra por anuncio, Llenar 200 semillas, Canoas infinitas 1 h | 04 §7.3 | Coincide | 1.0 |
| 39 | Vuelta del anuncio de vida | Vuelve a «Level Failed!» y pide otro toque (t = 338) | Cargar el nivel directamente [P] | 04 §7.3 (no lo fija) | Falta | 1.0 |
| 40 | Vidas ilimitadas como premio | Se suman y corren en tiempo real (∞ 15m + 30m = 44:55, t = 682). Premio de Daily Bonus, eventos, rachas y Journey | Canoas infinitas cortas como premio gratis [P] | 04 §7.1: solo Balsa, IAP y premios de 1.2; la casilla 30 de la Bitácora y la Crecida las excluyen a propósito | Excluido (04 §7.1) | 1.0 (pregunta 10.2 #9) |
| 70 | Vidas ilimitadas por anuncio | **No existe en MS** (confirmado por el dueño el 2026-10-08): las ∞ 15m son de la chistera del Daily Bonus, gratis («Tap to Claim», t = 671,48 → 675,34), y el único recompensado es «+1 Life» (t = 288). Lo que el dueño vio como «no del todo» es que, con ∞ activo, siguen saliendo intersticiales (t = 906-936 con ∞ 44:55 desde t = 682) | Canoas infinitas 15 min por un anuncio recompensado [P] | 05 §6.2: el único recompensado de canoas es `rv_extra_canoe` (+1 Canoa); 05 §4.1: ∞ solo en el IAP de 1,99 y en la Balsa; 04 retiró «Canoas infinitas 15 min» del hito 6 de la Crecida | Falta (petición del dueño) | 1.0 (pregunta 10.2 #14) |
| 71 | Victoria y cobro | «PERFECT!» también tras pagar el rescate (t = 482). Monedas sobre un pedestal. Las +20 se cobran al llegar a Home, con conteo y el sonido HOME_COINS (2,57 s; −10,5 dBFS; t = 488-492, 664-666). Sin botón de doblar (t = 482, 662) | Semillas acreditadas en la pantalla de victoria, con contador y `sfx_seeds_tick` (canon). Calificativo honesto: «¡Perfecto!» solo sin Cuia de rescate [P]. Cosecha doble por anuncio | 07 §5.4; 02 §6; 05 §6.2 (`rv_double_harvest`) | Difiere (cobro en la victoria; calificativo sin definir) | 1.0 (pregunta 10.2 #25) |
| 41 | Intersticial tras fallar | Tras «Try Again» (t = 196) y tras «Level Failed!» (t = 906, jugable) | Nunca | 05 §6.1; 05 §10 | Mejora nuestra | — |
| 42 | Intersticial al ganar | Corta la animación de victoria a los 1,4 s (t = 629,34) | Solo tras el toque en Siguiente | 02 §2 AD_GATE; 07 §5.4 | Mejora nuestra | — |
| 43 | Frecuencia | 3 intersticiales en 20 min, separados 434 s y 276 s (ficha §6.3); unos 140 s de anuncios en 1207 s (ficha §1.2) | Mínimo 150 s, cada 2-3 niveles | 05 §6.1; 08 §9 | Difiere (podemos mostrar más) | 1.0 (pregunta 10.2 #11) |
| 44 | «Close» que no responde | No respondió a algunos toques (t = 212; 638 y 652); en t = 328 tardó unos 8 s en cerrarse (si hizo falta otro toque: no observado); un jugable sin X hasta el final (t = 906-936) | Close probado al primer toque, como requisito de política [P] | 05 §6.1 (cierre a los 5 s; política de Play) | Falta la prueba | 1.0 |
| 45 | Anunciantes | Kalshi (trading y predicciones) en 3 de 4 anuncios (t = 196, 290, 630) | Clasificación G y categorías sensibles bloqueadas en todas las bandas [P] | 05 §7: solo la banda 13-17 (`npa=1`, `maxAdContentRating = G`) | Difiere en parte | 1.0 |
| 46 | Recompensados | Solo «+1 Life» (t = 288). Sin anuncio para continuar ni para doblar | 5 ubicaciones (Cosecha doble, Marea, Morpho, Luciérnaga, Canoa extra) | 05 §6.2 | Difiere (más generoso) | — |
| 47 | Banner | No hay (ficha §6.3) | No hay | 05 §6.1 | Coincide | — |

### 2.4 Economía y tienda

| # | Elemento de MS | Qué hace exactamente | Versión Tintalú | GDD hoy | Estado | Prioridad |
|---|---|---|---|---|---|---|
| 48 | Premio por nivel | 20 monedas; 100 mostradas en SUPER HARD (t = 482, 662, 748) | 20 / 60 / 80 semillas | 05 §2.1 | Coincide | — |
| 49 | Precios de comodines | 300 / 300 / 900 = 15 / 15 / 45 victorias (t = 0); el central es el de flechas cruzadas (barajar, deducido por el icono) | Marea 30 (+3), Morpho 100, Cuia extra 150, Pororoca 300 | 05 §2.2 | Difiere (Pororoca coincide) | — |
| 50 | Rescate | 900 monedas, 45 victorias. El jugador pagó una vez (1115 → 215, t ≈ 406) y pasó unos 13 min con menos de 300 monedas (t ≈ 408-1206), sin poder pagar ningún comodín | 600 semillas; 300 la primera del día | 04 §6; 05 §2.2; contrato 4 | Difiere (mejora nuestra) | 1.0 |
| 51 | Precio de la moneda | 1.000 monedas = COP 9.500 en Google Play (t = 1084). El GDD cita 9.900 COP, el precio de Apple (05 §4.2) | Puñado: 1.000 semillas = COP 3.900 (41 % de 9.500; 39 % de 9.900) | 05 §4.2 | Difiere (mejora nuestra) | 1.0 |
| 52 | Pack de inicio | Starter Bundle COP 5.000: 1.000 monedas, ∞ 1 h, 3 comodines, «90% OFF» (t = 1084) | Mochila de inicio 0,99 USD (COP 1.900): 500 semillas, 2 Cuia extra, 3 Morpho; sin canoas | 05 §4.1 | Difiere (contenido distinto; sección 6.4) | 1.0 (pregunta 10.2 #22) |
| 53 | «Sin anuncios» | Dentro del Apprentice Bundle, COP 22.000, con 2.000 monedas, ∞ 3 h y 3 comodines («ADS» tachado; deducido; t = 1084) | Sin anuncios 4,99 USD (COP 9.900): sin intersticiales + 500 semillas | 05 §4.1 | Difiere (lote frente a SKU suelto) | 1.0 |
| 54 | Oferta emergente | Heavenly Deal en mitad del intento, «90% OFF», cuenta atrás 2h 35m; al cerrarla, la tienda (t = 1082-1090). Disparador no observado | Comodín sin saldo → hoja inferior con la ruta gratuita primero y el Puñado solo desde el 31, sin cuenta atrás [P] | 04 §1.1 («Te faltan N»); 05 §5 y §10 (nada de pago antes del 31 salvo la Mochila) | Falta | 1.0 |
| 73 | Oferta temporal en Home | El caldero con arcoíris de Home es la oferta Heavenly Deal (deducido) y lleva su cuenta atrás (3h 23m antes de la grabación; 2h 34m en t = 1170) | Icono de oferta en el lobby con su tiempo, que no se abre solo [P] | 05 §5; 06 §4.7 (máx. 1 takeover por sesión) | Falta | 1.0 (pregunta 10.2 #23) |

### 2.5 Metajuego

| # | Elemento de MS | Qué hace exactamente | Versión Tintalú | GDD hoy | Estado | Prioridad |
|---|---|---|---|---|---|---|
| 55 | Broom Streak | Cofre con contador en el tablero («0/2»); hitos 2, 5 y 8; activo 14 h (t = 0, 670). Su estado no cuadra con una regla simple (ficha §7.2) | Crecida (barra en el borde derecho) | 06 §4.3; 07 §7.4 | Coincide | 1.0 |
| 56 | Magic Streak | «Beat levels without fail»; x1 → x5 → x10 → x25 → x100; baja a x1 al primer fallo y lo enseña en el fallo (t = 484, 666, 748-750) | Crecida (hitos 3 · 6 · 10) con Hoja escudo | 05 §2.4; 06 §4.3 | Difiere (canon; pregunta 10.2 #19) | 1.0 |
| 57 | Duck Festival | 1 pato por victoria; clasificación con podio; «Reward Stages: 1/20»; etapa 1 = ∞ 30m (t = 666-680) | Liga del río con etapas [P] | 06 §6 (1.2) | Coincide en parte | Después |
| 58 | Boat Rush | Carrera de 12 etapas en un mapa de río de selva (t = 492) | Regata | 06 §5.2 (1.2) | Coincide | Después |
| 59 | Team Quest | Equipos (t = 494) | Aldea | 06 §7 (2.0) | Coincide | Después |
| 60 | Daily Bonus | Elegir 1 de 9 chisteras; se revelan las otras (t = 672-676) | Bitácora del río, determinista | 06 §4.2; 05 §7 (sin azar) | Difiere (canon; pregunta 10.2 #17) | — |
| 61 | Journey: premios | Premios cada 10 niveles con lista visible; el del 390, a un nivel (t = 1188) | Canasta cada 10 + Mapa del río + mundos | 06 §4.4-§4.5 | Coincide | 1.0 |
| 72 | Journey: temáticas | Una escena de historia cada 20 niveles (1-99) con «USE»; ninguna en 360-420 (t = 1188-1202). Qué hace «USE»: no observado | Escenas de Capi y Rosa por tramo, galería, «Usar» como fondo del lobby y la siguiente en silueta [P] | 06 §4.5 (paradas futuras en silueta y skins por hito; no hay escenas) | Falta | 1.0 (pregunta 10.2 #16) |
| 62 | Cartas | Fundas con insignia de cartas en premios (t = 670, 674) | Cuaderno de campo sin sobres | 06 §4.6 (1.1) | Coincide sin azar | Después |
| 63 | Cadena tras ganar | 6-9 pantallas; 20 s (387) y 58 s (388) del último vertido al nivel siguiente (ficha §4.6). Devuelve ∞ 44:55 cuando quedaba 1 vida (t = 664-682) | Máx. 1 takeover por sesión | 06 §2.3, §4.7; 05 §5 | Difiere (canon; pregunta 10.2 #18) | — |
| 64 | Home | 12 iconos de evento sin nombre, solo «Join», «Finished» o tiempo (t = 1178) | Lobby con tarjetas con nombre | 06 §4.7 | Mejora nuestra | — |
| 65 | Temporizadores sincronizados | Los eventos activos acaban a la vez: 14h 45m en t = 484-494; 14h 42m en t = 668-678; 14h 34m en t = 1178 | Un evento temporal a la vez | 06 §5.1 | Difiere (canon; pregunta 10.2 #21) | — |

### 2.6 Audio

| # | Elemento de MS | Qué hace exactamente | Versión Tintalú | GDD hoy | Estado | Prioridad |
|---|---|---|---|---|---|---|
| 66 | Catálogo | 20 efectos; muestras idénticas salvo vertido, madera y hielo (2 muestras distintas) (ficha §9.2) | Catálogo de la sección 8 | 07 §9.2 | Coincide en parte | v0.2 / 1.0 |
| 67 | Sincronía | Corcho a +1,15 s del inicio del sonido del vertido (≈ inicio del chorro); consecuencias atadas al corcho; victoria 0,61 s tras el último corcho (ficha §9.3) | Igual [P] | 07 §5 (sin offsets de audio) | Falta | v0.2 |
| 68 | Mezcla | Gesto de −41 a −27 dBFS; interfaz −30; consecuencias −16 a −26; premios −10,5 a −14,7 (ficha §9.5) | Igual en relativo [P] | 07 §9.4 (sin jerarquía entre efectos) | Falta | 1.0 |
| 69 | Música y vibración | Música no observada: estaba apagada (t = 4). Vibración no observada (hay interruptor, t = 4) | Lechos por mundo; háptica de 02 §4.1 | 07 §9.3; 02 §4.1 | — | Grabar |

### 2.7 Ganchos observados y qué conservamos

En lugar de una columna más en las tablas 2.1-2.6, que ya tienen siete, esta tabla cruza cada gancho de la ficha §12.1 con sus filas. La sección 7.1 sí lleva la columna.

| Gancho (ficha §12.1) | Filas | Qué conservamos |
|---|---|---|
| G1 Cada color completado destapa algo | 28, 29, 30; sección 5.1 | Liana con fruta, Aguapé y Nasa: cada sellado libera algo, con animación y sonido atados a la tapa |
| G2 La base bajo la tela deja adivinar el contenido | 25, 28; sección 5.4 | Liana y Nasa que tapan dejando ver la dosis del fondo (pregunta 10.2 #15) |
| G3 Capas «?» con revelado | 25, 26; sección 5.4 | Revelado con destellos; Aguas turbias antes del 100 solo si se aprueba 5.4 |
| G4 Cadena de descubrimiento | 30 | Nasa con pez (después) |
| G5 El «casi» | 33 | El modal deja ver el tablero detrás; la Cuia de rescate cumple |
| G6 Mecánica nueva tras un nivel duro | 29-31; 02 §9 | Ya está: cada debut cae en p3, tras la recuperación p1-p2 del ciclo de 20 (sección 5.5) |
| G7 Racha y atasco hacen tentador pagar | 33, 55, 56 | La línea de la Crecida en el modal (04 §6.5), sin segunda oferta |
| G8 Celebración plena aunque se pague | 71 | Celebrar siempre; calificativo honesto (pregunta 10.2 #25) |
| G9 La oferta llega en el momento justo | 33 | Cuia de rescate en el modal, después de Marea |
| G10 Al fallar se enseña lo que se pierde | 35 | Se dice antes de decidir, una vez |
| G11 Racha visible durante la partida | 22, 55 | Barra de la Crecida en el borde derecho |
| G12 Multiplicador que cae al fallar | 56; sección 7.3 | Pregunta 10.2 #19 |
| G13 Variante en cada reintento | 16; sección 7.3 | Pregunta 10.2 #20 |
| G14 Reintento inmediato | 36 | Volver a la orilla en 1 toque |
| G15 La cadena de premios rescata la sesión | 40, 63, 70; sección 7.3 | Preguntas 10.2 #9, #14 y #18 |
| G16 El bonus diario enseña lo que no salió | 60; sección 7.3 | Pregunta 10.2 #17 |
| G17 Competición social | 57 | Liga del río (1.2) |
| G18 El nivel difícil se vende como reto | 31 | Etiqueta Difícil / Muy difícil en el botón Jugar del lobby |
| G19 «Solo me falta uno» | 61, 72 | Próxima Canasta y escena siguiente con los niveles que faltan |
| G20 Siempre hay algo pendiente | 64, 65 | Tarjetas con nombre; reloj común (pregunta 10.2 #21) |
| G21 Vidas ilimitadas que quitan la barrera | 40, 70 | Preguntas 10.2 #9 y #14 |
| G22 Microrrecompensa en cada sellado | 13 | Tapa con destellos del tinte |
| G23 Ofertas con cuenta atrás | 54, 73 | Icono de oferta en el lobby que no interrumpe (pregunta 10.2 #23) |

---

## 3. Tablero y control: valores a copiar

### 3.1 Medidas de MS en nuestra escala

Todo a 1080 × 2340 (t = 234, 342, 496, 698, 1154) salvo donde se indica la hoja de vertido (1080 × 1920). Paso a dp dividiendo por 3.

| Medida | MS en px | MS en nuestra escala | Tintalú hoy | Fuente nuestra |
|---|---|---|---|---|
| Botella (13-14 en pantalla) | 135 × 415 | **45 × 138 dp**; u (MS) ≈ 23 dp | 34 × 102 dp con 7 por fila; 39 × 117 con 6 | 02 §5; 07 §4.1 |
| Botellas por fila | 3-6 (5/5/3, 5/4/4, 4/6/4) | — | hasta 7 | 02 §5 |
| Paso horizontal | 160,6 | 53,5 dp | ranura = 336 / n (48 dp con 7) | 02 §5 |
| Hueco entre botellas | 26 (0,19 del ancho) | 8,7 dp | 0,30 de la ranura (≈ 14 dp con 7 por fila) | 02 §5 |
| Hueco entre filas | 66 | 22 dp | 40 dp (16 de elevación + 24) más la repisa de 15 dp | 02 §5; guía v2 §3.2 |
| Filas | y = 570-984 · 1051-1465 · 1533-1947 | y = 190-328 · 350-488 · 511-649 dp | panel centrado entre 88 y 620 dp | guía v2 §3.2 |
| Bloque de botellas | 1377 de alto, centrado entre píldora y barra | 459 dp, centrado | centrado (D-003) | D-003 |
| Hueco sobre la fila 1 | Unos 100 px (33 dp) hasta la píldora; el origen la tapa al verter (t = 250) | — | Techo del vertido (D-004) | `GameScreen.PourCeilingFor` |
| Barra de comodines | 673 × 236, a 64 del borde | 224 × 79 dp, a 21 dp | 328 × 80 dp, y = 660-740 | 07 §7.4 |
| Elevación al seleccionar | 52 (0,125 del alto) | **17 dp** (0,75 u (MS)) | 16 dp (0,5 u con la cuia de 64; 0,73 u con la de 43,6) | 02 §4.1; D-004 |
| Líquido / ancho de botella | 99 / 111 (ms-02, a 1920) | 0,89 | 1,30 / 2,00 u = 0,65 | 07 §4.1 |
| 4 dosis / alto de botella | 251 / 341 (ms-02) | 0,74 | 4,00 / 6,00 u = 0,67 | 07 §4.1 |
| Cara superior del líquido | elipse de 24 px (dosis de 62,75) | 0,38 dosis | menisco de 2-3 px | 07 §4.2 |
| Boca sobre el labio del destino | 5-20 px a 1920 (hoja); 13 px (t = 250) y 37 px (t = 776) a 2340 | **0,1-0,55 u (MS)** | 0,6-1,6 u | D-004; 07 §5.2 |
| Ángulo máximo | — | 66° con 3 dosis restantes; ≈ 92° al vaciarse | 38° fijos | D-004 |

### 3.2 Tiempos del vertido

Fuente MS: hoja de vertido (una viñeta cada 100 ms, resolución ±100 ms). Fuente nuestra: 02 §4.1 y 08 §9.

| Fase | MS | Tintalú hoy | Recomendación [P] | Nota |
|---|---|---|---|---|
| Subida al seleccionar | ≤ 100 ms (1300 → 1400 ms) | `anim_lift_ms` 120 | 100 | Diferencia mínima |
| Viaje e inclinación hasta la boca | 200-300 ms (2900 → 3100 ms) | `anim_travel_ms` 200 | 250 | — |
| Chorro (1 dosis) | ≈ 300 ms (3200 → 3400 ms); el destino sube ≈ 285 ms por dosis | `anim_pour_dose_ms` 100 + `anim_pour_tail_ms` 80 = 180 | 200 + 80 = 280 | **Fuera del rango de 08 §9** (80-150 para la dosis): hay que ampliarlo |
| Vuelta | ≈ 300 ms (3500 → 3800 ms), directa al hueco | `anim_return_ms` 160 | 250-300 | En 3700 ms la botella pasa ≈ 50 px por debajo del reposo: un rebote. Visto en un solo vertido: confirmar a 60 fps antes de copiarlo |
| De la salida al reposo (sin la subida) | **0,9-1,0 s** | ≈ 0,54 s (0,66 s con la subida) | ≈ 0,75-0,8 s con los tiempos de arriba (cálculo: el chorro arranca al 70 % del viaje). Para llegar a 0,9 s, viaje y vuelta de 300 ms | Comparar siempre sin la subida |
| Del inicio del último vertido al aviso de bloqueo | 1,32-1,6 s (audio) | fin de la animación + `stuck_modal_delay_ms` 400 ≈ 1,0 s | Mantener | Coincide en la práctica |

**Por qué un ciclo más largo no frena.**
- El ritmo medio del jugador en MS fue de un vertido cada 3,1 s (mediana; p10 1,54 s). La animación no es el cuello de botella: lo es pensar.
- Nuestro modelo es la verdad y la vista lo persigue, con cola de 2 vertidos por cuia (02 §3.2). Un toque nunca espera a la animación.
- En modo `single` el «encadenado» de 02 §3.2 alarga el chorro cuando se repite el mismo vertido. Con un chorro de 280 ms se nota más su valor.

### 3.3 Disposición del tablero

**Qué vemos en MS.** Las 13-14 botellas van siempre en 3 filas de 3-6, con la misma botella en los tres niveles (t = 234, 496, 698, 1154). No se vio ninguna fila de 7 ni ningún tablero de 2 filas. Los tableros con pocas botellas: **no observado** (la grabación empieza en el 387). MS no reserva hueco para el vertido: en t = 250 el origen tapa parte del rótulo «Level 387».

**Qué hacemos hoy (02 §5; D-004).** 1 fila hasta 5 cuias; 2 filas de 6 a 14 (hasta 7 por fila); 3 filas solo con 15-16. D-004 dejó fijado que no cambian «el paso de fila ni el número de filas (02 §5)». También puso un techo al vertido: el origen inclinado nunca sube más de 4 dp dentro de la fila del HUD, y en 2 y 3 filas no pasa del borde exterior del panel (`GameScreen.PourCeilingFor`).

**Cálculo con `BoardLayout.Compute` y `GameScreen.PourCeilingFor`** (360 × 780 dp; zona del tablero de 88 a 620 dp; filas de ≤ 5; marco de 13,5 dp). A 38° y sin elevación, el canto alto del origen sube 0,94 u sobre el borde del destino (0,6 u de labio + 0,34 u del borde de fibra; D-004):

| Caso (T = 11-15, 3 filas) | Ancho de cuia | Qué pasa al verter sobre la fila 1 |
|---|---|---|
| `Compute` tal cual | 46,8 dp | El bloque ocupa los 532 dp: empieza en y = 88, justo bajo el HUD. El panel se recorta (y = 80) y el techo queda en 84 dp. El canto alto del origen llega a y ≈ 82: 6 dp dentro de la fila del HUD (D-004 admite 4) y sin margen para elevar |
| Máximo con 38° y el techo de D-004 | ≈ 46,4 dp | El canto alto queda justo en el techo (84 dp). Panel recortado bajo el HUD; elevación 0 |
| Máximo con 38° y el panel entero | **≈ 43,6 dp** | El panel empieza en y = 80 sin recortar; quedan ≈ 13 dp (≈ 0,6 u) para elevar el origen |
| Pose B de 4.3.3 (65° con 3 dosis restantes) | ≤ 36 dp | El canto alto sube ≈ 3,7 u sobre el borde del destino (cálculo: 0,3 u de boca + 6 u × cos 65° + 1 u × sen 65°). Solo cabe si el techo pasa a ser el del HUD. Bajo el borde del panel no cabe en ningún tablero de 2 o 3 filas, tampoco hoy con 2 filas de 7 (≈ 25 dp por encima) |

**Ganancia sobre hoy** (cálculo; con 38°):

| Cuias (T) | Hoy: filas → ancho de cuia | 3 filas con el panel entero (≈ 43,6 dp) | 3 filas recortando el panel (≈ 46,4 dp) |
|---|---|---|---|
| 6-10 | 3+3 … 5+5 → 47-48 dp | Igual (no cambia) | — |
| 11-12 | 6+5, 6+6 → 39 dp | +12 % | +19 % |
| 13-14 | 7+6, 7+7 → 34 dp | +28 % | +36 % |
| 15 | 5+5+5 → 46,8 dp: **hoy ya**, con el mismo problema de techo (solo se da con Cuia extra o Cuia de rescate) | −7 % (se reserva el hueco) | −1 % |
| 16 | 6+5+5 → 39 dp | Igual | — |

- Lo que propongo: 3 filas de ≤ 5 desde 11 cuias, con el ancho que deje el hueco del vertido sobre la fila 1. Con el panel entero, **≈ 43,6 dp**. A 13 y 14 cuias reproduce las filas de MS: 5/4/4 en el 388 (t = 496) y 5/5/4 en el 387 tras añadir la botella (t = 408).
- La regla general: **elegir entre 2 y 3 filas la que dé la cuia más ancha después de reservar el hueco del vertido**, con ≤ 5 por fila en 3 filas (6 solo con 16). En 19,5:9 y 20:9 ganan las 3 filas; en 16:9 (360 × 640 dp), las 2.
- El número de filas se decide con las T cuias generadas y **no cambia a mitad de nivel**: la Cuia extra o la Cuia de rescate van a la fila más corta. Ejemplo: T = 10 (5 + 5) más una Cuia extra no pasa a 4 + 4 + 3; queda 6 + 5. Hoy D-004 recalcula el layout con el T nuevo.
- **Retiro lo de «no cambia D-004».** Esta propuesta modifica una consecuencia fijada en D-004 (el número de filas) y obliga a reservar el hueco del techo. Hace falta una decisión nueva (sección 4.3.1).
- No copiamos el hueco de 22 dp entre filas de MS: la repisa de D-003 (15 dp bajo el pie) más la elevación (16 dp) necesitan los 40 dp actuales.

### 3.4 La cuia por dentro

**Qué vemos en MS (ms-02).** El líquido ocupa el 89 % del ancho de la botella y el 74 % de su alto. Pared de vidrio de ≈ 6 px por lado. Cara superior en elipse de 0,38 dosis.

**Qué hacemos hoy (07 §4.1-§4.2).** Cavidad de 1,30 u de ancho en una cuia de 2,00 u (65 %). 4 dosis = 4,00 de 6,00 u (67 %). Menisco de 2-3 px.

**Propuesta [P]** (en fracciones del exterior):
- Ancho interior **≥ 0,80 del ancho exterior** (hoy 0,65) y las 4 dosis **≈ 0,72 del alto exterior** (hoy 0,67), recortando hombro y cuello. La cavidad sigue recta y las 4 dosis miden lo mismo.
- Cara superior del líquido en elipse de **≈ 0,4 de dosis**, más clara que el tinte. Hoy, menisco de 2-3 px.
- **Cómo escribirlo en u** (hay que elegir una):
  - (a) Mantener 1 dosis = 1 u: cavidad de 4,00 u, alto exterior ≈ 5,6 u (hoy 6,00) e interior ≥ 1,60 u. La proporción pasa de 1:3 a ≈ 1:2,8 y cambia todo lo que se mide desde el alto (`HeightU`, la tabla de 07 §4.1 y `CuiaGeometry`). Las cifras de D-004 en u (labio a 0,6-1,6 u, `BoardAnimator.MaxRaiseU`, `CuiaGeometry.RimHalfWidthU`) siguen valiendo. Además la cuia es más baja, lo que deja más hueco a las 3 filas (cálculo pendiente).
  - (b) Redefinir u (por ejemplo, u = alto/6 como hoy, con la dosis a ≈ 1,08 u): cambian D-004 (labio a 0,6-1,6 u, `MaxRaiseU`, `LipClearanceU`), `CuiaGeometry.RimHalfWidthU` (0,55 u), el quad del shader (1,30 × 4,00 u) y la tabla de 07 §4.1.
  - Recomiendo (a): no toca las cifras de D-004.
- Con la cuia de 34 dp de hoy, el líquido mide 22 dp de ancho. Con la cuia de 43,6 dp y el interior al 0,80 mediría ≈ 35 dp: **+58 %** de ancho de color (cálculo). Es la mejora de legibilidad más barata del documento.
- **Choca con D-003.** El punto 5 de D-003 dice que no cambian «la cuia-garrafa, su cavidad recta, las alturas iguales por dosis, las marcas y el menisco (07 §4)». Recortar hombro y cuello y pasar el menisco a elipse lo reabre (pregunta 4.4).
- **Límite de trade dress:** se mantienen el hombro, el cuello corto, el pie y el borde de fibra de la cuia-garrafa. Nada de labio de botella (07 §1.3).
- Afecta a 07 §4.1, §4.2 y §4.5, al quad del shader, a `ProceduralArt` y al brief del artista.

### 3.5 Pose del vertido

**Qué vemos en MS** (hoja de vertido; t = 250, 776):
- La boca cae sobre el eje del destino con ±3 px de error y su borde inferior queda a 0,1-0,55 u (MS) del labio del destino.
- El cuerpo queda del lado del que viene la botella; la boca apunta al destino (deducido, 2 casos).
- 66° con la botella casi llena (le quedan 3 dosis); ≈ 92° al vaciarse con 2 dosis (no le queda ninguna): la boca queda por debajo del fondo. Deducido de 3 casos: cuanto más vacía queda, más se inclina. Ajuste lineal (cálculo): **ángulo ≈ 92° − 9° × dosis que quedan**.
- La botella no cambia de tamaño (escala 1,00) y se dibuja delante de las vecinas. No tiene techo: tapa el rótulo del nivel (t = 250).

**Qué hacemos hoy (D-004).** 38° fijos, labio a 0,6-1,6 u del destino, techo del vertido bajo el marco del panel.

**Por qué importa (deducido de la geometría de 07 §4.1).** La cavidad termina a 4,6 u y el labio está a 5,75 u. A 38° el labio queda muy por encima del agua de una cuia con 1-3 dosis: el chorro sale de una cuia cuya agua no llega a la boca. MS y LW suben el ángulo justo para que el agua llegue. El dueño ya aceptó el tapado de la cuia vecina con 38°; con 66-92° también se tapa.

**Hueco que pide cada pose (cálculo, sección 3.3).** Con 38°, el canto alto sube 0,94 u sobre el borde del destino; con 65°, ≈ 3,7 u. La pose medida no cabe bajo el techo de D-004 en tableros de 2 y 3 filas.

**Recomendación.** Es una decisión del dueño (pregunta 4.3.3). La boca más cerca (0,2-0,6 u) es independiente del ángulo y ayuda en los dos casos: el origen sube menos y choca menos con el techo del vertido.

---

## 4. Conflictos con decisiones del dueño (preguntas cerradas)

No decido por el dueño. Cada pregunta trae opciones, una recomendación y el porqué. Toda aprobación que cambie una decisión del registro necesita una decisión nueva que la sustituya (`docs/dev/decisiones.md`, «Cómo revertir»).

### 4.1 D-001 (una dosis por vertido): nueva evidencia para P6

**Estado.** D-001 está decidida por el dueño. Sigue abierta su pregunta P6: «¿Una dosis por vertido es también la regla de 1.0?», con la recomendación «Sí» (`docs/dev/decisiones.md` §5). Esta sección aporta datos para responder P6; no reabre D-001 por sí sola.

**Datos.**
- MS mueve el tramo entero: 2 dosis (t = 250, 430), 3 dosis (t = 598, 600, 838). Un vertido de 3 completa una botella (t = 600).
- LW también vierte el tramo (LW t = 132-134).
- **Estimación de confianza media** (audio; el número de dosis se comprobó con fotogramas en solo 12 casos, ficha §9.2): de 221 vertidos, ≈ 174 de 1 dosis (79 %), 37 de 2 y 10 de 3. Con una dosis por vertido harían falta ≈ 57 vertidos más, ≈ 278 en lugar de 221 (≈ +26 %, cálculo). Es una referencia, no el argumento principal.
- Nuestro canon ya midió el coste: par del tutorial ×1,0-×1,7 y solver con ×1-×34 estados (`docs/dev/decisiones.md` §2.1.3-§2.1.4).

**Pregunta 4.1 (para P6).** ¿Qué regla rige en 1.0?
- **A.** Mantener D-001 (`single`): respuesta «Sí» a P6. Se mantiene el «encadenado» de 02 §3.2.
- **B.** `run`: el tramo visible que cabe, con vertido parcial (la regla de MS y LW). Hace falta una decisión nueva que sustituya a D-001 («Cómo revertir»), regenerar los packs con `--pour-mode run` y cambiar el default de `rules_pour_mode` en 08 §9 (hoy `"single"`).

**Recomendación: jugar 10 minutos en `run` en el teléfono antes de confirmar o sustituir D-001. Si no hay preferencia clara, A (la decisión vigente).**
- Por qué merece la prueba: es la regla de los dos juegos observados y la de todos los líderes (02 Desviaciones 3); un vertido grande que sella es un momento de premio (t = 600); conserva las cifras [V-proto] de 03, medidas en `run`, y abarata el solver.
- Por qué A sigue por defecto: el dueño la prefirió jugando, da identidad propia (tipo *ball sort*) y ya está implementada y verificada en el teléfono. Si queda A, recomiendo el chorro de 280 ms con encadenado (sección 3.2), para que pasar una racha dosis a dosis se sienta fluido.

### 4.2 D-002 (sin resaltado de destinos): coincide

- MS no resalta destinos ni cambia ninguna otra botella al seleccionar (t = 232, 598). LW tampoco (LW t = 11). **No hay conflicto.**
- MS no mostró ninguna pista automática, mano guía ni sugerencia en 387-389 (ficha §2.10). Sus únicas ayudas son de pago. Cómo enseña en los primeros niveles: **no observado**.

**Pregunta 4.2 (relacionada).** ¿Qué hacemos con la pista por inactividad (02 §3.4, contrato 9: desde el nivel 3, a los 12 s, sin tope)?
- **A.** Mantenerla como está.
- **B.** Solo en los niveles 3-30 [P] y apagable en Ajustes (ya lo es). Revierte el contrato 9 (GDD §7.1, incidencia 9) y necesita una clave nueva con otro nombre: no se puede reutilizar el alias retirado `hint_idle_until_level` (08 §9: «pista por inactividad solo en 1-10»).
- **C.** Apagada por defecto; el jugador la enciende en Ajustes.

**Recomendación: B.** En el tutorial ayuda a no abandonar. Más adelante, el argumento es la preferencia del dueño por un reto sin ayudas que no se piden (CLAUDE.md, «Decisiones del dueño»: «No reintroduzcas ayudas implícitas»), y lo visto en MS a esa altura. No es un argumento de D-002: D-002 clasifica la pista por inactividad como ayuda explícita, porque «solo señala un origen».

### 4.3 D-004 (tamaños y pose)

**4.3.1 ¿Tres filas de ≤ 5 cuias desde 11 cuias, reservando el hueco del vertido sobre la fila 1?**
- **A.** Sí: cuias de ≈ 43,6 dp con 11-15 cuias y el panel entero (≈ 46,4 dp si se acepta el panel recortado bajo el HUD; sección 3.3). El número de filas no cambia a mitad de nivel.
- **B.** No: se mantienen 2 filas de hasta 7 (34-39 dp).

**Recomendación: A, pero decidir antes 4.3.3.** MS usa 45 dp a nuestra escala con 13-14 botellas y nunca filas de 7. Con 38°, A gana un 28 % de ancho con 13-14 cuias. Con la pose B de 4.3.3, las 3 filas casi no ganan nada (≤ 36 dp). **A modifica una consecuencia fijada en D-004** («No cambian: … el paso de fila ni el número de filas (02 §5)») y la regla de D-004 de recalcular el layout cuando cambia T: hace falta una decisión nueva que sustituya esa parte de D-004. Cambian 02 §5 y 07 §4.1 a la vez (07 §4.1 lo exige), 07 §7.4, 00 §4 y 04 §2.2 (sección 10.1). No afecta al tutorial (T ≤ 7).

**4.3.2** Movida a la sección 4.4: la cavidad no es de D-004, sino de D-003.

**4.3.3 ¿Qué ángulo de vertido?**
- **A.** 38° fijos (D-004, «38° está bien»).
- **B.** Pose medida en MS: ≈ 92° − 9° × dosis que quedan en el origen (66° con 3, ≈ 92° con 0) [P].

**Recomendación: ver A y B lado a lado en el teléfono antes de decidir. Elegir B solo si se aceptan sus dos costes.**
- A favor de B: es la pose de los dos juegos observados (LW: 55° a 78-80°), y con 38° el agua de una cuia casi vacía no llega a la boca (sección 3.5). Al decidir D-004 el dueño no tenía estas medidas: C17 solo comparaba 38° con 75° fijos o con una fórmula sin medir (38° + 12° × (4 − dosis), tope de 80°).
- Coste 1: **B reabre una alternativa que D-004 descartó** (la inclinación dinámica). Hace falta una decisión nueva que sustituya la decisión 2 de D-004.
- Coste 2: con 65° el origen sube ≈ 3,7 u sobre el borde del destino. No cabe bajo el techo de D-004 en ningún tablero de 2 o 3 filas (sección 3.3). B exige dejar que el origen pase sobre el marco o el HUD durante el vertido (≤ 400 ms), como hace MS en t = 250, y renunciar a casi todo el ancho de 4.3.1.

**4.3.4 ¿Bajamos la boca a 0,2-0,6 u sobre el destino (hoy 0,6-1,6 u)?**
- **A.** Sí [P].
- **B.** No.

**Recomendación: A.** Es lo medido en MS (0,1-0,55 u (MS)). El chorro es más corto y legible y el origen sube menos hacia el techo. Cambia la decisión 2 de D-004 («el labio queda entre 0,6 y 1,6 u», `BoardAnimator.LipClearanceU`): hace falta una decisión nueva.

**4.3.5 (menor) ¿Elevación proporcional?** MS sube 0,125 del alto de la botella (0,75 u (MS)). Nosotros, 16 dp fijos: 0,5 u con la cuia de 64 dp. **Recomendación:** mantener 16 dp. Con cuias de 43,6 dp ya son 0,73 u, muy cerca de MS. No merece una decisión aparte.

### 4.4 D-003 (silueta de la cuia y menisco)

Ensanchar la cavidad, recortar hombro y cuello y pasar el menisco a elipse **reabre el punto 5 de D-003** («No cambian: … La cuia-garrafa, su cavidad recta, las alturas iguales por dosis, las marcas y el menisco (07 §4)»). No es de D-004. Si se aprueba, hace falta una decisión nueva que sustituya esa parte de D-003.

**Pregunta 4.4.** ¿Ensanchamos la cavidad y pasamos el menisco a elipse?
- **A.** Sí: ancho interior ≥ 0,80 del exterior; 4 dosis ≈ 0,72 del alto exterior, escrito con 1 dosis = 1 u y la cuia a ≈ 5,6 u de alto (sección 3.4); cara superior en elipse de ≈ 0,4 de dosis.
- **B.** No: 0,65 y 0,67 como hoy y menisco de 2-3 px.

**Recomendación: A.** El color es lo que se juega; MS da al líquido el 89 % del ancho. Se mantienen la cavidad recta y las alturas iguales por dosis, y no se copia el labio de botella (07 §1.3).

---

## 5. Mecánicas especiales: equivalente amazónico

### 5.1 Lo que copiamos de las mecánicas de MS, sean cuales sean

1. **Cada sellado destapa algo** (G1). Telas (t = 27, 362, 398), hielo (t = 504), armarios (t = 712, 814). El jugador ve el siguiente objetivo sin texto: el icono de color dice qué completar.
2. **Cada desbloqueo tiene animación y sonido propios**, atados al golpe del corcho (sección 8).
3. **Mejora nuestra:** MS no explica ninguna regla en pantalla (ficha §3.2; F15). Nosotros mantenemos el tutorial de una pantalla en cada debut (02 §9.1).
4. **Contenido oculto: decisión del dueño** (sección 5.4). MS tapa botellas enteras (tela, armario) y usa «?» en niveles normales. Hoy nuestro canon solo oculta en Aguas turbias (02 §11). No lo presento como mejora: es una pregunta.

### 5.2 Equivalencias

| Mecánica de MS | Regla observada (t) | Equivalente Tintalú | Mecánica del GDD más cercana (debut) | Qué cambia | Prioridad |
|---|---|---|---|---|---|
| Capa «?» | Se revela al quedar arriba; hasta 3 seguidas; en niveles normales (t = 238, 496, 712) | **Aguas turbias** | Aguas turbias (100, solo Difícil/Muy difícil) | Copiar el revelado con destellos (< 2 s, t = 238): **cambia** 02 §4.1 y 07 §4.3, que hoy fijan 300 ms. Cuándo aparece: pregunta 5.4 | 1.0 |
| Botella atada (cuerda y etiqueta) | Solo acepta su color (deducido, t = 888); puede verter (t = 776); la cuerda sigue tras sellar (t = 26, 362); en el 389 empieza con dosis mezcladas (t = 1154) | **Hoja**: hoja de palma con el icono de la fruta | Hoja (43): vacía, no es origen, se abre con el primer vertido de su fruta | Mantener la Hoja como está para el debut. Copiar la legibilidad de la etiqueta: icono de fruta grande y claro (t = 238). La versión con contenido mezclado del 389: idea para después | 1.0 |
| Tela con icono | Tapa la botella; se retira al completar una botella del color del icono: ✓ verde, haz de luz desde la botella sellada, la tela sube (t = 56, 362, 398) | **Liana con fruta** [P]: una liana envuelve la cuia con una fruta colgando; se seca y cae al sellar una cuia de esa fruta. Hoy el contenido se ve; taparlo dejando ver la dosis del fondo, como la tela (t = 0), es la pregunta 5.4 | Liana (83): se suelta al sellar n cuias, sin color | Añadir la variante con fruta (`key`) junto a la de número (`n`). El esquema ya tiene `key` en `attrs`, pero su descripción dice «Tinte que abre la Hoja o tinte agua del mundo de la Lluvia»: hay que cambiarla, y la semántica en 02 §9.1 y 03 §9 (contratos 1 y 21) | 1.0 (pregunta 10.2 #8) |
| Hielo por fila | Encierra la base de una fila; recibe vertidos (t = 502); al completar una botella dentro revienta toda la fila (t = 504, 508). Levantar las congeladas: no observado | **Aguapé** [P] · pendiente de revisión cultural (planta flotante de la várzea). Cubre la base de una fila; sus cuias reciben vertidos y no son origen; al sellar una cuia del grupo, el aguapé suelta toda la fila | Brote (63) es por cuia y se abre con su color | Mecánica nueva (M8): tipo nuevo `aguape` en `mechanic_type`, que sube `pack_schema` (contrato 21). «Barro» no sirve de nombre: es la dosis inerte (M5). «Niebla» sugiere información oculta. Debut propuesto: 223 (posición 3 del ciclo; contrato 1 y `mech_debut_level`) | Después |
| Armario con candado y llave | Tapa 2 o 4 botellas; la llave va en una dosis; al quedar arriba vuela al candado de su color; el armario se abre o revienta (t = 696, 712, 812, 950) | **Nasa** [P] (trampa de fibra trenzada que envuelve 2 o 4 cuias, con un nudo de color) y **pez** [P] de ese color dentro de una dosis. Al quedar arriba, el pez salta y suelta la nasa. Pendientes de revisión cultural (pesca ribereña y fauna). Alternativa para la nasa: tronco hueco [P], también pendiente | Ninguna | Mecánica nueva (M9): tipo nuevo `nasa` y marca de dosis en `af-level/1` (hoy `attrs` es por cuia): sube `pack_schema` (contrato 21). Si la nasa deja ver el contenido o lo tapa: pregunta 5.4. Debut propuesto: 263 (contrato 1) | Después |
| SUPER HARD | Anunciado antes de jugar; premio mostrado 100 frente a 20 (×5, deducido; no se cobró ninguno) (t = 666, 748) | **Muy difícil** (jaguar) | Muy difícil (60) | Copiar que la etiqueta se vea ya en el botón Jugar del lobby [P] | 1.0 |
| Botella añadida | En la fila con hueco y recentrada (t = 402-410) | **Cuia extra / Cuia de rescate** con Capi | 02 §8 (al final de la última fila) | Colocarla en la fila más corta y recentrar [P]. El número de filas se decide con las T cuias generadas y no cambia a mitad de nivel (sección 3.3) | 1.0 |

### 5.3 Mecánicas del GDD cruzadas con lo visto en MS

| Mecánica del GDD | Debut | Algo parecido en MS (387-389) | Lectura |
|---|---|---|---|
| Cuia chica | 14 | No: todas las botellas vistas tienen capacidad 4 | Sin datos |
| Cuia alta | 27 | No | Sin datos |
| Hoja (M1) | 43 | Botella atada | Validada como idea: MS la usa en los tres niveles |
| Brote (M2) | 63 | Hielo (por grupo, no por cuia) | Parecida; el Aguapé sería su versión de grupo |
| Liana (M3) | 83 | Tela | Propuesta: añadir el disparador por fruta |
| Remolino (M4) | 103 | **No observado** | Sin datos |
| Barro (M5) | 123 | **No observado** | Sin datos |
| Lluvia (M6) | 143 | **No observado** | Sin datos |
| Piraña (M7) | 183 | **No observado** | Sin datos |
| Aguas turbias | 100 | Capa «?» (en todos los niveles) | Pregunta 5.4 |

- MS ya combina 3 mecánicas en un nivel (389: armarios, llaves, botellas atadas y «?»; t = 684). Nuestro tope es de 2 antes del 300 (02 §9.2, regla 4). Se mantiene.
- Hace falta grabar otros tramos de MS para ver más mecánicas (ficha §11.1).

### 5.4 Contenido oculto como gancho general (pregunta)

**Qué vemos en MS.** Los seis tramos de `observar.json` señalan el contenido oculto como gancho (ficha §12.1, G1-G4): capas «?» y telas que esconden botellas enteras; la base bajo la tela deja adivinar el contenido (t = 0). El 387 tapa cuatro botellas enteras (t = 0). El 388, un nivel normal sin etiqueta, lleva 16 dosis ocultas (t = 496) y se ganó al primer intento (t = 496-626). La investigación web recoge la otra cara: el «?» percibido como azar es la queja n.º 1 del líder (02 §11, T4; GDD §4, columna «Evitamos»).

**Qué dice hoy el canon.** Aguas turbias solo en Difícil y Muy difícil desde el 100 (02 §11, T1), con ≤ 3 cuias y ≤ 4 dosis ocultas por nivel (T2: así el solver del cliente enumera ≤ 24 realidades en < 50 ms), vertido seguro garantizado (T5) y Luciérnaga desde el ~100. La Liana y la Hoja dejan ver el contenido.

**Pregunta 5.4.** ¿El contenido oculto pasa a ser un gancho general?
- **A.** Mantener 02 §11 tal cual.
- **B.** Adelantar Aguas turbias a niveles normales con tope [P]: desde el 40-60, ≤ 2 dosis ocultas por cuia y los límites de T2-T5 (≤ 3 cuias, ≤ 4 dosis por nivel, vertido seguro). Además, que la Liana y la Nasa tapen la cuia entera dejando ver la dosis del fondo, como la tela de MS (t = 0).
- **C.** Solo la parte de tapar: Liana y Nasa que tapan la cuia dejando ver la dosis del fondo; Aguas turbias como hoy.

**Recomendación: C ahora y B a prueba después.**
- Tapar una cuia que no se puede jugar hasta liberarla da curiosidad (G1, G2) sin el azar de la queja n.º 1: el jugador no decide a ciegas sobre ella, solo planifica.
- Adelantar las «?» a niveles normales es el cambio de más riesgo: obliga a adelantar también Luciérnaga (debut ~100), a rehacer la cadencia de 02 §9 y a medirlo con el abandono por nivel en el soft launch.
- La etiqueta «mejora nuestra» queda retirada hasta que decidas.
- Qué toca si se aprueba: 02 §11 (T1), 02 §9.1 (Liana), 03 §8-§9, 04 §2.5 (Luciérnaga), 08 §9 (`lvl_murky_from_level`), el esquema si cambia algún tipo (contrato 21) y `tools/level-qa`.

### 5.5 Ritmo de dificultad observado

| Nivel de MS | Etiqueta | Intentos (t) | Resultado | Lectura |
|---|---|---|---|---|
| 387 | Ninguna | 6 (t = 0-476) | 1 abandono y 4 bloqueos antes de pagar 900 (t ≈ 406); gana tras pagar (t = 476) | Muy difícil sin aviso (F1) |
| 388 | Ninguna | 1 (t = 496-626) | Gana al primer intento, en 130 s, sin ayudas | Respiro tras el pico, con mecánica nueva (hielo; G6) |
| 389 | SUPER HARD | 12 (t = 684-1172) | 11 bloqueos y 1 abandono. Bloqueos a los ≈ 6 s tras 1-2 vertidos (t = 894-900, 976-982, 1066-1072). Intento D casi ganado: 6 botellas y 2 de 3 armarios (t = 806-888; G5) | Pico anunciado; sin botellas vacías al empezar (t = 938) |

**Frente a nuestro canon.**
- 02 §10: Difícil (bot ≥ 75 % de fallo, 1 vacío, K+1) y Muy difícil (≥ 90 %, hasta 2 mecánicas), siempre etiquetados y con solución sin comodines verificada por el solver. El 387 de MS sería un Difícil o un Muy difícil sin etiqueta: nosotros no lo permitimos.
- 03 §12: ciclo de 20 con retos en p6, p10, p14, p17 y p19, recuperación en p1-p2 e hito en p20. MS hace lo mismo a pequeña escala: pico (387) → respiro con novedad (388) → pico anunciado (389). Coincide con nuestra cadencia.
- Bloqueos inmediatos: 02 §9.2, regla 1, rechaza los tableros bloqueados al inicio (`atascado(S0)`), pero no los que se bloquean tras 1-2 vertidos. Los niveles 6 y 8 del tutorial llegan a Aguas estancadas en 1 y 2 vertidos a propósito, para enseñar el modal (`docs/dev/decisiones.md` §2.1.2; 02 §13).

**Pregunta 5.5.** ¿`tools/level-qa` debe vigilar los tableros que se bloquean en ≤ 2 vertidos con alguna jugada inicial?
- **A.** Rechazarlos siempre, salvo los niveles 6 y 8 del tutorial.
- **B.** Medirlos como métrica de QA («trampas tempranas»: jugadas iniciales que llevan a Aguas estancadas en ≤ 2 vertidos), con un tope por etiqueta [P]: 0 en Normal, ≤ 1 en Difícil, sin tope en Muy difícil.
- **C.** No.

**Recomendación: B.** Una trampa así en un Muy difícil es parte del reto; en un Normal es la frustración de F14. Se mide en el modo de vertido vigente (`single` o `run`), porque el número de vertidos cambia con el modo.

---

## 6. Fallo, vidas y monetización

### 6.1 El embudo de MS y nuestra versión

| Paso de MS (t) | Qué pasa | Nuestra versión | Cifras nuestras | Copiamos · suavizamos |
|---|---|---|---|---|
| 1. Último vertido → «No Available Moves» a 1,32-1,6 s (t = 84, 404, 777) | Saldo arriba; tablero visible; se ocultan HUD y comodines. «Play On» 900 = +1 botella. «Give Up». Los botones entran por los lados (t = 777, 1018) | Aguas estancadas (02 §7; 04 §6) | Marea primero; Cuia de rescate 600 semillas (300 la primera del día); Volver a la orilla (desde el 31, volver desde el modal cuesta 1 canoa, 04 §5.2) | **Copiamos:** momento, tablero visible, saldo visible, +1 cuia que cumple, botones que entran por los lados [P]. **Suavizamos:** Marea antes de pagar; precio |
| 2. «Continue?» (t = 86, 746, 1022) | Segunda oferta de 900 con corazón roto y «You will lose 1 life!». Solo se rechaza con la X | No existe | — | **No copiamos.** Un solo modal por bloqueo (04 §6.6). La canoa se avisa en una línea de estado desde el 31 |
| 3. «Level Failed!» (t = 6,5, 142, 748) | Tarjeta con «Rewards: 20» (100 en SUPER HARD), «Try Again» y X; en el 389, panel de racha perdida | No existe | — | **No copiamos** la pantalla. Lo que está en juego ya se dice antes de decidir: «Sin Hoja escudo hoy: este fallo rompe tu Crecida de 4» (04 §6.5) |
| 4. «Try Again» → otro tablero; a veces un intersticial antes (t = 196) | Variante distinta, sin aviso | Volver a la orilla: mismo tablero; «El río bajó» al 3.er fallo | — | **No copiamos** el anuncio (05 §10). El cambio sin aviso: pregunta 10.2 #20 |
| 5. Con 0 vidas → «More Lives» (t = 287-289) | «Refill» 900 o «+1 Life» con un anuncio | Sin canoas (04 §7.3) | Canoa extra por anuncio (3/día, 90 s); Llenar canoas 200 semillas; Canoas infinitas 1 h a 1,99 USD; esperar 25 min | **Copiamos** el anuncio de +1. **Suavizamos:** 200 semillas en vez de 900 |
| 6. Anuncio (≈ 47 s, t = 290-336) → «Level Failed!» → «Try Again» | Dos toques más tras el anuncio | Tras la recompensa, cargar el nivel directamente [P] | — | **Mejora** (04 §7.3) |

Reintentar sin pagar en MS cuesta 3 toques («Give Up», X, «Try Again») y −1 vida. En Tintalú, 1 toque (Volver a la orilla). Desde el 31, quedarse sin vertidos y volver a la orilla desde el modal también cuesta 1 canoa (salvo Canoas infinitas), igual que en MS. La diferencia: el coste se avisa en el modal antes de decidir, y reiniciar por voluntad propia nunca se cobra (04 §5.2).

Medidas de las pantallas de MS (tarjeta, botones, precio, orden vertical): ficha §4.8. Las de nuestro modal están en 07 §7.5 (328 × 300 dp, anclado abajo).

### 6.2 Qué no copiamos (patrones oscuros y política)

| Patrón de MS (t) | Por qué no | Regla nuestra |
|---|---|---|
| La misma oferta de 900 dos veces seguidas (t = 84-86, 1018-1022) | Insiste en vender tras un «no» | Un modal por bloqueo (04 §6.6) |
| «You will lose 1 life!» con vidas ilimitadas activas (t = 803, 973, 1174) | Mensaje que no cuadra con las ∞ activas. Si resta una vida real: no observado (ficha §11.1) | La línea de canoa se oculta con Canoas infinitas (04 §6.2) |
| «Rewards: 100» que no se cobra, enseñado tras perder (t = 748, 904) | Aversión a la pérdida después de decidir | Lo que está en juego se dice antes, una vez |
| Intersticial tras fallar y otro que corta la celebración (t = 196, 629) | Queja n.º 1 del género (05 §6.1) | Solo en `VICTORY_SCREEN`, tras el toque en Siguiente (05 §6.3) |
| «Close» que no responde a algunos toques (t = 212; 638 y 652); en t = 328 tardó unos 8 s; jugable sin X hasta el final (t = 906-936) | Cierre engañoso. La política de Play exige poder cerrar (05 §6.1: cierre a los 5 s) | Requisito de política: prueba en dispositivo de que Close responde al primer toque en cada red de la mediación [P] |
| Anuncios de trading y predicciones (Kalshi, t = 196, 290, 630) | Los anuncios deben ajustarse a la clasificación de la app (ClassInd 14 esperado; Everyone/Teen en Play, 05 §7). Hoy 05 §7 solo limita la banda 13-17 (`npa=1`, `maxAdContentRating = G`) | Extender `maxAdContentRating = G` a todas las bandas y bloquear en MAX y AdMob apuestas, dinero real, trading y mercados de predicción [P] |
| Oferta con «90% OFF» y cuenta atrás en mitad del intento (t = 1082) | Interrumpe la partida y presiona con el reloj | Comodín sin saldo → hoja inferior con la ruta gratuita primero (vídeo para Marea y Morpho; «Te faltan N»); el Puñado solo desde el 31 y nunca en la primera sesión (05 §5 y §10); sin cuenta atrás ni takeover [P] |
| Comodines a 300-900 con un saldo de 255 y sin ruta gratuita a mano (t = 714-1206). Las unidades gratis estaban en chisteras no elegidas del Daily Bonus (t = 674) y en el premio de Journey del 390, a un nivel (t = 1188) | El jugador pasó unos 13 min con menos de 300 monedas (t ≈ 408-1206) | Ruta gratuita real (05 §1, principio 3) |

### 6.3 Qué cambia en nuestra economía

**Comparación de precios** (cálculo; MS a COP 9.500 por 1.000 monedas en Google Play, t = 1084; Tintalú a COP 3.900 por 1.000 semillas, 05 §4.2):

| Concepto | MS | Tintalú | Tintalú / MS |
|---|---|---|---|
| Rescate tras bloqueo | 900 = COP 8.550 = 45 victorias | 600 = COP 2.340 = 30 victorias (300 = COP 1.170 la primera del día) | 27 % (14 %) |
| Deshacer (deducido por el icono) | 300 = COP 2.850 | Marea: 3 gratis por nivel; +3 por 30 = COP 117 | — |
| Barajar (deducido por el icono) | 300 = COP 2.850 | Pororoca 300 = COP 1.170 | 41 % |
| +1 recipiente planificado | 900 = COP 8.550 | Cuia extra 150 = COP 585 | 7 % |
| Llenar vidas | 900 = 45 victorias | 200 = 10 victorias | 22 % |
| Pack de inicio | COP 5.000 | Mochila 0,99 USD = COP 1.900 | 38 % |

- El GDD cita otro precio de MS: 9.900 COP por 1.000 monedas, el de Apple (05 §4.2, de research/09). Con 9.900, los porcentajes cambian poco: el Puñado sería el 39 % en lugar del 41 %.

**Lo que se mantiene.** Ancla 1.000 semillas = 1,99 USD (05 §1). Rescate a 600 fijo y 300 el primero del día (contrato 4). 20/60/80 por victoria. Ningún cambio de precio sale de esta observación.

**Lo que propongo cambiar [P].**
1. **Canoas infinitas cortas como premio gratis (reabre una exclusión).** En MS son el premio estrella de rachas, eventos, bono diario y Journey (t = 670-686), y el jugador volvió con 1 vida a jugar 8 min sin límite (t = 682 → 1170).
   - **Hoy el canon las excluye a propósito.** 04 §7.1: «la casilla 30 de la Bitácora no las incluye: 05 §2.3 / 06 §4.2. La Crecida no otorga Canoas infinitas». La Canasta grande se decidió el 2026-10-06 como 150 semillas + 1 Cuia extra + 1 Morpho + 3 Marea (GDD §6.1, pregunta 13).
   - Propuesta: Canoas infinitas de 15-30 min en algunas casillas de la Bitácora del río y en la Canasta grande. Reabre 04 §7.1 y la pregunta 13 de GDD §6.1.
   - Hay que decir si sustituyen casillas de la Bitácora (que hoy suman 3.000 semillas, `eco_calendar_table_v1`) o se suman a ellas.
   - **Sí tocan la economía.** `tools/econ_sim.py` modela las canoas: con 0 canoas termina la sesión (líneas 418-427). Cambian los intentos, los ingresos y las cifras [sim] (contrato 18). Antes de decidir: nueva corrida de `econ_sim.py` y `econ_variants.py` y regenerar `tools/out/econ_*.md`. Pregunta 10.2 #9.
2. **Regeneración de canoas.** MS: 30 min, deducido de 4 lecturas y a confirmar (ficha §4.5). La fuente del GDD («~25 min», research/magic-sort/b, [V] con 2 reseñas) puede ser de otra versión del juego. Se mantiene 25 en `canoes_regen_min`. No propongo corregir aún GDD §4 ni 04 §7.1: basta anotar «MS, versión de 2026-10: 30 min, deducido» hasta confirmarlo leyendo el temporizador en 30:00.
3. **Toque en un comodín sin saldo** (falta en 04 §1.1 y 05 §5).
   - Desde el nivel 31 y nunca en la primera sesión: hoja inferior que ofrece primero la ruta gratuita (vídeo para Marea y Morpho si hay cupo; «Te faltan N») y después el Puñado, sin cuenta atrás.
   - Antes del 31, solo la ruta gratuita. Cuia extra (nivel 8) y Morpho (nivel 12) se desbloquean antes, y el canon prohíbe toda oferta de pago antes del 31 salvo la Mochila (05 §10) y antes del 10 o en la primera sesión (05 §5).
   - Hay que aclarar si la hoja cuenta para el tope de 1 takeover por sesión (`ui_max_takeovers_per_session`). Propongo que no, porque la abre el jugador.
   - Sonido: `sfx_no_funds` [P] (sección 8.2).
4. **Guardarraíles de anuncios: requisito, no opción.** Extender a todas las bandas `maxAdContentRating = G` y el bloqueo de apuestas, dinero real, trading y mercados de predicción (hoy 05 §7 solo lo aplica a 13-17), y probar el Close en el dispositivo (05 §6.1, §6.3 y §10).
5. **Frecuencia de intersticiales.** MS mostró 3 en 20 min a un jugador que fallaba mucho (separación 276 y 434 s). Nuestro mínimo de 150 s permite más. Recomiendo mantener las cifras y vigilar intersticiales por minuto en el soft launch, con el dato de MS (≈ 0,15/min) como referencia (pregunta 10.2 #11).

### 6.4 Tienda Tintalú frente a MS

**MS (t = 1084; ficha §5.4 y §4.8).** Dos lotes grandes arriba (Starter y Apprentice), tres packs de monedas en fila y «More Offers» plegado; detrás, el tablero oscurecido. La oferta Heavenly Deal vive además como icono del caldero en Home, con cuenta atrás de horas (3h 23m antes de la grabación; 2h 34m en t = 1170; deducido).

| Producto | Contenido | Precio (COP) | COP por moneda o semilla, sin contar extras (cálculo) |
|---|---|---|---|
| MS · Starter Bundle | 1.000 monedas, ∞ 1 h, 3 comodines; «90% OFF» | 5.000 | 5,0 |
| MS · Heavenly Deal (oferta emergente) | 1.200 monedas, ∞ 1 h, 3 comodines; «90% OFF»; 2h 35m | 5.000 | 4,2 |
| MS · Apprentice Bundle | 2.000 monedas, «ADS» tachado (sin anuncios, deducido), ∞ 3 h, 3 comodines | 22.000 | 11,0 |
| MS · 1.000 monedas | — | 9.500 | 9,5 |
| MS · 5.000 monedas | — | 36.500 | 7,3 |
| MS · 10.000 monedas | — | 73.000 | 7,3 (sin descuento sobre el de 5.000) |
| Mochila de inicio (`af_starter`, niveles 10-20) | 500 semillas, 2 Cuia extra, 3 Morpho; sin canoas | 1.900 | 3,8 |
| Puñado (`af_coins_2`) | 1.000 semillas | 3.900 | 3,9 |
| Bolsa (`af_coins_3`) | 2.800 semillas | 9.900 | 3,5 |
| Saco (`af_coins_4`) | 6.000 semillas | 18.900 | 3,2 |
| Costal (`af_coins_5`) | 13.000 semillas | 36.900 | 2,8 |
| Cosecha (`af_coins_6`) | 36.000 semillas | 94.900 | 2,6 |
| Sin anuncios (`af_noads`, desde el 22) | Sin intersticiales para siempre + 500 semillas | 9.900 | — |
| Cántaro (`af_piggy`, desde el 20) | 1.000-3.000 semillas acumuladas | 5.900 | 5,9-2,0 |
| Balsa de rescate (`af_rescue`, solo en el modal) | Rescate + 1.000 semillas + 1 Cuia extra + ∞ 30 min | 9.900 | 9,9 |
| Canoas infinitas 1 h (`af_infinite_canoes_1h`) | 60 min | 3.900 | — |
| Packs de skin (`af_skin_*`) | Skin + fondo + 300 semillas | 3.900 | — |

Precios en COP de la tabla de 05 §4.2; contenidos de 05 §4.1. Monedas y semillas se comparan unidad a unidad porque los dos juegos pagan 20 por nivel normal.

- MS no da descuento por volumen por encima de 5.000; nosotros sí (+12 % a +44 %, 05 §4.1).
- Los lotes de MS mezclan monedas, ∞ y comodines. Los nuestros no llevan canoas, salvo la Balsa.
- **«Sin anuncios» difiere:** MS lo mete en un lote de COP 22.000 con 2.000 monedas, ∞ 3 h y 3 comodines (deducido); nuestro `af_noads` cuesta COP 9.900 y solo añade 500 semillas.
- **El pack de inicio difiere:** el Starter de MS da 1.000 monedas + ∞ 1 h + 3 comodines por COP 5.000; nuestra Mochila, 500 semillas + 2 Cuia extra + 3 Morpho, sin canoas, por COP 1.900 y solo en la ventana 10-20.

**Boceto del Mercado [P]** (pestaña Semillas, de arriba abajo; las pestañas son las de 06 §4.7):

```
+--------------------------------------------+
| Mercado                  [ 1.240 semillas ]|
| [Semillas]  Comodines   Cuias  Sin anuncios|
| +----------------------------------------+ |
| | Tarjeta grande: Mochila (niveles 10-20)| |  o «Sin anuncios» desde el 22
| +----------------------------------------+ |
| +----------------------------------------+ |
| | Tarjeta grande: Cántaro (si >= 1.000)  | |
| +----------------------------------------+ |
| [ Puñado ]   [ Bolsa ]   [ Saco ]          |  3 packs en fila, como MS
|  3.900        9.900       18.900           |
| (  Más packs  v )                          |  plegado: Costal y Cosecha
+--------------------------------------------+
```

- Copiamos de MS: dos tarjetas grandes arriba, tres packs en fila y el resto plegado (ficha §4.8: tarjetas al 95 % del ancho; packs de ≈ 30 % cada uno).
- No copiamos el sello «90% OFF» con cuenta atrás ni la oferta que salta en mitad del intento (sección 6.2).

**Preguntas** (10.2 #22 y #23):
- ¿La Mochila incluye Canoas infinitas 1 h, como el Starter de MS? Ojo: la Mochila se vende en los niveles 10-20 y las canoas debutan en el 31 (04 §7.1). Las ∞ solo tendrían sentido si se guardan para después del 31.
- ¿Hay una oferta temporal visible como icono en el lobby, con su cuenta atrás y sin interrumpir la partida, como el caldero de MS? Debe respetar 05 §10 (ninguna oferta de pago antes del 31 salvo la Mochila). Si no se abre sola, no cuenta como takeover (`ui_max_takeovers_per_session` = 1).

---

## 7. Metajuego: qué copiar primero

### 7.1 Orden propuesto

| Orden | De MS (t) | Gancho que conservamos (ficha §12.1) | Versión Tintalú | Estado en el GDD | Prioridad |
|---|---|---|---|---|---|
| 1 | Racha visible en el tablero (Broom Streak «0/2», t = 0, 938) y racha que se enseña al perder (Magic Streak, t = 748) | G11 y G7: recuerda en cada nivel lo que hay en juego | **Crecida** con Hoja escudo: barra en el borde derecho y línea en el modal | 06 §4.3; 04 §6.5 | 1.0 (ya está) |
| 2 | Vidas ilimitadas como premio gratis, que se suman (t = 676-682) | G21 y G15: quitan la barrera de reintentar sin regalar monedas | **Canoas infinitas** de 15-30 min en Bitácora y Canasta grande [P] | Excluido en 04 §7.1 (pregunta 10.2 #9) | 1.0 |
| 3 | Premios cada 10 niveles con la lista a la vista (Journey, t = 1188); el del 390, a un nivel | G19: «solo me falta uno» | **Canasta** con su contenido visible y la próxima en el Mapa del río [P] | 06 §4.4-§4.5 | 1.0 |
| 4 | Temáticas por tramos con «USE» (Journey, t = 1190-1202) | G19 aplicado a algo que se colecciona | **Escenas del río** desbloqueables (sección 7.4) [P] | Falta (pregunta 10.2 #16) | 1.0 |
| 5 | Nivel difícil anunciado antes de jugar, con premio mayor (t = 666) | G18: se vende como reto, no como castigo | Etiqueta **Difícil / Muy difícil** también en el botón Jugar del lobby [P] | 02 §10; 07 §7.7 | 1.0 |
| 6 | Clasificación con etapas de premio para todos (Duck Festival, «Reward Stages: 1/20», t = 678) | G17: todos cobran algo por jugar | **Liga del río** con etapas por puntos [P] | 06 §6 (1.2) | Después |
| 7 | Carrera por niveles (Boat Rush, 12 etapas, t = 492) | Objetivo corto con gran premio | **Regata** | 06 §5.2 (1.2) | Después |
| 8 | Equipos (Team Quest, t = 494) | Social | **Aldea** | 06 §7 (2.0) | Después |

### 7.2 Qué no copiamos

Solo presentación y patrones que no son ganchos (las prácticas que sí lo son van en 7.3):
- **Iconos de Home sin nombre** (12 iconos, t = 1178; F21). Nosotros: tarjetas con nombre (06 §4.7).
- **Fundas de cartas como premio** (t = 670, 674). Cuaderno de campo sin sobres (06 §4.6; 05 §7).
- **Mapa de carrera con río de selva, piedras con estrella y boyas** (Boat Rush, t = 492). Es su arte: nuestra Regata usa nuestro río y nuestros mundos.

### 7.3 Prácticas que el canon descarta y MS usa como gancho (preguntas)

El canon las descartó antes del método del dueño («copiamos lo que funciona»). Las devuelvo como preguntas cerradas, con el dato de MS delante.

| # (10.2) | Práctica de MS (t) | Gancho | A. Copiar tal cual | B. Versión Tintalú (sin azar ni presión) | C. No copiar (canon) | Qué habría que reabrir | Recomendación |
|---|---|---|---|---|---|---|---|
| 17 | Bono diario: elegir 1 de 9 chisteras; se enseñan las otras 8 (t = 672-674) | G16 | Elección al azar con premios ocultos | Elegir 1 de 3 premios **visibles** (elección real, sin azar), además de la casilla de la Bitácora; la casilla de mañana, destacada | Bitácora tal cual | 05 §7 (sin azar; «azar simulado: no» en IARC y ClassInd), 06 §4.2, dirección 15 | B |
| 18 | Cadena de 6-9 pantallas tras ganar, que devuelve ∞ 44:55 con 1 vida (t = 482-496; 662-682) | G15 | La cadena entera | Un solo panel de «premios del día» tras la victoria, con todos los avances y un toque para cobrar | Máx. 1 takeover por sesión | 05 §5 (`offer_scheduler`), 06 §2.3 y §4.7, `ui_max_takeovers_per_session` | B |
| 19 | Multiplicador x1-x100 que cae a x1 al fallar (t = 484, 748-750) | G12 | Multiplicador de premio que se pierde entero | Crecida con multiplicador visible de las semillas del nivel (x1 → x2 → x3) que baja un escalón al fallar, con Hoja escudo | Hitos 3 · 6 · 10 | 02 §6, 05 §2.4, 06 §4.3, `streak_milestones` (contratos 5 y 19) y las cifras [sim] (contrato 18) | C hasta correr `econ_sim.py` con B |
| 20 | Variante nueva en cada reintento, sin aviso (t = 9, 144, 342) | G13 (y F9) | Variantes sin aviso | Al reintentar, el jugador elige: «Mismo río» (misma semilla) u «Otro río» (otra variante de la misma plantilla, avisada) | Misma semilla; «El río bajó» al 3.er fallo | GDD §2 #11; GDD §4 («Evitamos: cambiar el tablero sin avisar»); 02 §2; 04 §2.7; 05 §10; 03 (pool de variantes con su par) | B |
| 21 | Eventos con un temporizador común (14h 45m en t = 484-494; 14h 42m en t = 668-678; 14h 34m en t = 1178) | G20 | Varios eventos a la vez con el mismo reloj | Un solo reloj diario común («Nuevo día en 14 h») para el Reto, la Bitácora y el evento activo; sigue habiendo un solo evento temporal | Un evento temporal a la vez, sin reloj común | 06 §5.1 | B |

- **17:** la elección da el momento de «a ver qué me toca» sin azar; ver los otros premios da ganas de volver (G16). Cambia la regla, no la cantidad: la Bitácora sigue sumando 3.000.
- **18:** juntar los avances en un panel conserva la lluvia de premios (G15) sin la cadena de 20-58 s (F25).
- **19:** el multiplicador mueve semillas: sin simulación no se puede valorar (contrato 18).
- **20:** «Otro río» copia la esperanza de que el siguiente salga mejor (G13) sin el cambio a ciegas que impide aprender (F9). Exige que el generador produzca variantes validadas con su par.
- **21:** un reloj común da urgencia sin apilar eventos (queja n.º 8 del líder, 06 §5.1).

### 7.4 Temáticas desbloqueables

**Qué vemos en MS** (ficha §7.8; t = 1190-1202):
- Una escena ilustrada cada 20 niveles en el tramo 1-99 («Magic Trouble» 1-19, «Broom Flight» 20-39, una sin nombre visible bajo el 65 y «Slimey» 80-99), al principio de su tramo en el mapa Journey.
- Son escenas de historia con el elenco de gatos, presentadas como fotos con cinta adhesiva y una pestaña «USE».
- Entre 360 y 420 no hay tarjetas (t = 1188). La escena del Home (el teatro) no está entre las vistas.
- **No observado:** qué hace «USE», si la siguiente escena se adelanta bloqueada o en silueta, y si siguen más allá del 99.

**Qué tenemos hoy.** El Mapa del río con 13 paradas, paradas futuras en silueta con «Nivel {n}», skins por hito con la cuenta de niveles que faltan y un Dato del río cada 10 niveles (06 §4.4-§4.5). El lobby usa el fondo del mundo actual (06 §4.7). No hay escenas de historia ni nada que el jugador elija y aplique.

**Propuesta [P].**
- **Una escena por tramo de 20 niveles**, que se gana en el hito del ciclo (p20, 03 §12): Capi y Rosa en un momento del río del mundo actual. Pendiente de revisión cultural (07 §8), sin personas ni aldeas identificables (07 §8.1; D-003 punto 5).
- **Galería en el Mapa del río:** las escenas ganadas, como fotos de un álbum.
- **«Usar»** [P; en MS no se sabe qué hace]: la escena elegida pasa a ser el fondo del lobby.
- **La próxima escena en silueta, con «Faltan N niveles»** [P; no observado en MS]: el gancho «solo me falta uno» (G19) aplicado a algo que se colecciona.
- **Coste:** 1 ilustración cada 20 niveles. En 1.0 propongo 10 escenas (niveles 1-200, la zona hecha a mano) y, desde el 201, 1 por mundo. Lo encarga el dueño al artista (07 §11; 09).

**Pregunta 10.2 #16.**
- **A.** Escenas por tramo con galería, «Usar» como fondo del lobby y la siguiente en silueta.
- **B.** Solo la galería en el Mapa del río, sin «Usar» ni silueta.
- **C.** No: mantener las skins por hito y los Datos del río.

**Recomendación: A**, empezando por las 10 escenas de 1-200. Es lo que el dueño pidió (temáticas ocultas que den ganas de jugar) y reutiliza el Mapa del río. Cambia 06 §4.5 y §4.7, 07 §11 (lista de assets) y el brief del artista.

---

## 8. Audio: sonidos a producir

### 8.1 Reglas que copiamos de MS

1. **Una muestra por acción**, con variantes al azar donde se repite mucho: vertido (≈ 11 variantes de 1 dosis), madera rota (≥ 4) y hielo (2 muestras distintas) (ficha §9.2).
2. **Sincronía fija con el corcho** (ficha §9.3).
   - El sonido de completar arranca con el sonido del vertido (0-0,14 s después) y el corcho suena a **+1,15 s desde el inicio de ese sonido**.
   - Ese sonido llega con la botella ya inclinada (≈ inicio del chorro); en los fotogramas el chorro se ve hasta 0,5 s antes.
   - Las consecuencias cuelgan del corcho: la tela suena con él y su barrido llega a +1,5-2,1 s (la fuente da dos medidas: +1,65-2,1 s y +1,5-1,65 s); el hielo, a +1,12 s. La victoria, a 0,61 s del último corcho.
   - En nuestra animación, anclar la tapa al inicio del chorro, no al inicio de la animación ni a la salida de la cuia. Desde la salida serían ≈ +1,4-1,6 s (cálculo con la hoja de vertido). El desfase exacto: medirlo con un vídeo a 60 fps.
3. **Jerarquía de mezcla** (RMS de pico en 32 ms; ficha §9.5): gesto repetido de −41 a −27 dBFS (selección −38,5; vertidos de −41 a −27); interfaz −30; consecuencias −26 a −16; premios −14,7 a −10,5. **Entre el gesto y el premio, 20-28 dB.** El sonido más fuerte de MS no es un premio: es un golpe sin atribuir de −10 dBFS antes de una oferta (t = 1081,79). No lo copiamos (`sfx_no_funds`, sección 8.2).
4. **Un sonido por acción (02 §12).** MS deja sin sonido los popups de fallo y el revelado de «?» (ficha §9.4). Nosotros mantenemos `sfx_stuck` (suave, sin golpe dramático, 07 §9.2), la gota del revelado (02 §4.1) y el sonido de rechazo, siempre por debajo de los premios.

### 8.2 Lista

- Columna «Nivel (MS)»: el de MS como referencia relativa, no como valor final. «—» = MS no tiene ese sonido o no tiene nivel medido; entonces va una cifra [P].
- IDs de 07 §9.2; los nuevos van con [P]. Ningún ID nuevo repite uno del canon (07 §9.2 ya usa `sfx_reveal` para Luciérnaga, `sfx_restart` para Volver a la orilla, `sfx_basket_open` para la Canasta y `sfx_streak_up` para el hito de la Crecida).
- Columna «Háptica»: la de 02 §4.1 y 07 §9.2 donde existe; [P] donde no. Qué vibra en MS: **no observado** (ficha §11.1; pregunta 10.2 #13).

| Sonido Tintalú | Disparador | Carácter | Sincronía (de MS, con t) | Nivel (MS) | Háptica | Prioridad |
|---|---|---|---|---|---|---|
| `sfx_select` | Toque en el origen | Toque de agua corto afinado al tinte (07 §9.2) | En el *touch-down*. MS: «tic» de cristal de 110 ms (t = 11,64) | −38,5 (el gesto más bajo; algunos vertidos bajan a −41) | Ninguna (02 §4.1) | v0.2 |
| `sfx_deselect` [P] | Volver a tocar la cuia elevada | «Toc» grave de madera hueca, ≈ 90 ms | Al bajar. MS DESELECT (t = 562,21) | −35,5 | Ninguna [P] | v0.2 |
| `sfx_pour` (sustituye a start/loop/end) [P] | Vertido | Gorgoteo de agua en vidrio. **Una sola muestra por vertido**, 8-11 variantes. Que el tono suba con el llenado del destino (07 §9.2) queda como mejora propia | Arranca con el chorro. MS: 1 dosis 390-590 ms; 2 dosis 710-810; 3 dosis 870-1090 (t = 12,84; 70,87; 586,46) | −41 a −27 | Tick ligero al inicio del chorro (02 §4.1) | v0.2 |
| `sfx_illegal` | Vertido ilegal | Madera neutra (07 §9.2) | Con la sacudida. MS: no observado | — (≈ −35 [P]) | Ninguna (02 §4.1) | v0.2 |
| `sfx_seal` | Vertido que sella | Sonajero de semillas que crece + «toc» de la tapa de calabaza | Arranca con el vertido; la tapa suena a +1,15 s del inicio del chorro (≈ inicio del sonido del vertido), cuando cae. MS COMPLETE ≈ 1,6 s (t = 25,61; 475,5) | −17,5 | Ligera, 20 ms con la tapa (`sfx_haptic_seal`, 07 §9.2) | v0.2 |
| `sfx_murky_reveal` [P] | Revelado de una dosis turbia | Gota suave (02 §4.1) | Al aclararse. MS: sin sonido (t = 238, 564) | — (bajo [P]) | Ninguna | 1.0 |
| `sfx_liana_free` [P] | Se suelta una Liana | Campanilla de madera en el golpe de la tapa + barrido de hojas a +0,5 s | MS CLOTH_REVEAL: «ding» con el corcho (+1,15-1,2 s) y «fsss» a +1,5-2,1 s (t = 54,99) | −26 | Ligera [P] | 1.0 |
| `sfx_hoja_open` [P] | Se abre una Hoja | Hoja seca que se desprende (02 §4.1) | Con el golpe de la tapa. MS: la botella atada no tiene sonido propio | — (≈ −26 [P]) | Ninguna (02 §4.1) | 1.0 |
| `sfx_aguape_break` [P] | Se suelta el aguapé | Desgarro vegetal + salpicadura | MS ICE_SHATTER a +1,12 s, ≈ 700 ms, 2 muestras distintas (t = 503,33) | — (sin nivel medido; ≈ −20 [P]) | Ligera [P] | Después |
| `sfx_nasa_open` [P] + `sfx_fish_jump` [P] | El pez suelta la nasa | Salto de pez + fibra que se abre | MS: UNLOCK a 0,58-0,68 s del inicio del vertido; WOOD_BREAK 0,87 s después; tic final a +1,65 s. Cadena de ≈ 3 s (t = 695,05; 695,92) | −25 / −19,5 | Ligera [P] | Después |
| `sfx_stuck` | Aguas estancadas | Zumbido grave de agua quieta, sin golpe dramático (07 §9.2) | Con el modal. MS NO_MOVES: glissando descendente de 1,29 s a 1,32-1,6 s del inicio del último vertido (t = 82,9) | −16 (el nuestro, por debajo de `sfx_seal` [P]) | Ninguna (02 §4.1) | v0.2 |
| `sfx_play` [P] | Botones grandes que arrancan un nivel: Jugar y Siguiente | Burbuja que sube, «blup-blup» | MS POP_A, 370 ms (t = 8,73). Los botones grandes no usan el clic pequeño | −18 | Ninguna [P] | 1.0 |
| `sfx_restart` | Volver a la orilla (modal y HUD) | Agua que se recoge + madera (07 §9.2) | MS: «Try Again» suena con POP_A (t = 750,54) | −18 | Ninguna | 1.0 |
| `sfx_submerge` | Entra el tablero (Sumergir y teñir) | Chapuzón grave + burbujas (07 §9.2) | 0,24 s tras `sfx_play`. MS WHOOSH_B (t = 8,97) | −18,6 | Ninguna | v0.2 |
| `sfx_ui_tap` | Botones pequeños, X, cerrar | Madera seca de 40 ms | MS UI_CLICK (t = 3,46) | −30 | Ninguna | 1.0 |
| `sfx_no_funds` [P] | Toque en un comodín sin saldo (abre la hoja de 6.3) | «Toc» de madera hueca, corto y suave | Con la hoja. MS: probablemente el golpe de 312 Hz y 110 ms que precede al Heavenly Deal (t = 1081,79; confianza baja) | ≈ −30 [P]. MS −10: no copiamos un golpe a −10 dBFS en un momento de frustración | Ninguna | 1.0 |
| `sfx_win_toada` | Victoria | Toada corta (07 §9.2) | A 0,6 s de la última tapa. MS: fanfarria de 5,88 s con golpe a +0,95 s y estallido a +3,6 s (t = 477,26) | −14,5 | Media (02 §4.1, celebración) | v0.2 |
| `sfx_basket_open` | Canasta (07 §9.2) | Fibra que se abre + frutas | MS CONGRATS (t = 675,34) | −14,7 | Ligera [P] | 1.0 |
| `sfx_streak_up` | Hito de la Crecida (07 §9.2) | Nutrias | Al pagar el hito. MS: no tiene sonido de racha que sube | — (≈ −15 [P]) | Ligera [P] | 1.0 |
| `sfx_reward` [P] | Premio sin ID propio: casilla de la Bitácora, escena de 7.4 | Campanilla de premio, ≈ 0,6 s | MS CONGRATS (t = 675,34) | −14,7 | Ligera [P] | 1.0 |
| `sfx_seeds_tick` | Semillas que llegan al contador | Impacto + caída de semillas en la cesta | MS HOME_COINS: impacto y ≈ 5 tics en 2,57 s (t = 488,26). Es el premio más fuerte de MS; el golpe sin atribuir de 312 Hz llega a −10 | −10,5 | Ninguna [P] | 1.0 |
| `sfx_streak_lost` [P] | La Crecida cae (fallo duro sin escudo) | Agua que baja, dos glissandos descendentes, ≈ 0,4 s | MS STREAK_LOST (t = 748,00; confianza baja-media) | −26 | Ninguna | 1.0 |
| `sfx_event_token` [P] | Ficha de evento que vuela al icono (Liga, Regata) | «Plip» + campanilla | MS DUCK_DING, 410 ms (t = 677,99) | −14 | Ninguna [P] | Después |
| `sfx_capi_paddle` | Cuia de rescate | Remo + chapoteo (07 §9.2) | Con la llegada de Capi. MS: «Add Bottle» sin sonido aislado | — | Ligera (02 §4.1) | 1.0 |
| Música | Lechos por mundo (07 §9.3) | — | **No observada en MS** (apagada, t = 4) | — | — | Grabar |

---

## 9. En qué nos separamos de Love Water

LW es otro water-sort (iPhone, nivel 143, 174 s, sin audio). Comparte con MS la capacidad 4, el vertido por tramo (t = 132-134) y la ausencia de resaltado (t = 11). Lo que **no** repetimos:

| Rasgo de LW (t del vídeo de LW) | Qué hacemos | Dónde está en el GDD |
|---|---|---|
| Banner fijo que rota cada 5-10 s con tragaperras, «WIN REAL CASH», bingo y brókers (t = 21-174) | Sin banner; categorías sensibles bloqueadas [P] | 05 §6.1; sección 6.2 |
| Widget de terceros «$500 · EARN CA$H» (t = 11-20) | Ningún SDK de recompensas de terceros | 05 §7 (9 SDK) |
| Dos tooltips en inglés solapados y con una falta, en un iPhone en español (t = 10-14) | Un mensaje por nivel, ≤ 2 líneas, en ES-419, EN y PT-BR revisados | 02 §13; 02 §12 |
| Líquido plano en franjas, sin menisco ni brillo (t = 7) | Cara superior en elipse, menisco, brillo y burbujas | 07 §4.2; sección 3.4 |
| 12 colores con pares casi iguales y oscuros sobre fondo oscuro, sin símbolos (t = 7) | 14 tintes con patrón e icono; pares de riesgo limitados; Patrones de fruta | 07 §3; 02 §12 |
| Vaso de cerveza genérico con un reflejo (t = 7) | Cuia-garrafa propia | 07 §4 |
| Rejilla de 5 × 3 vasos flotando sobre fondo plano (t = 7) | Panel con repisas de madera por fila y sombra de contacto | D-003; guía v2 |
| Fondo de stock cortado por un morado liso (t = 7) | Fondos propios en los mundos 1-5; «río genérico» en los mundos 6-9 hasta que lleguen sus packs | 07 §2.1; contrato 25 |
| Vaso fantasma «+1» con insignia de anuncio dentro de la rejilla (t = 7) | Los comodines viven solo en la bandeja; antes del desbloqueo su ranura no existe | 07 §7.4 |
| Deshacer a «0» con insignia de anuncio (t = 7) | Marea ilimitada en 1-10 y 3 gratis por nivel después | 04 §2.1 |
| Vaso que cruza filas casi en horizontal (78-80°) y se sale de la pantalla (t = 14, 150) | Techo del vertido bajo el marco; boca a 0,2-0,6 u [P] | D-004; sección 3.5 |
| Vaso completado igual que los demás (t = 95, 134, 174) | Cuia sellada con tapa de calabaza | 07 §4.4 |
| Misma celebración para todos los colores (t = 95, 134, 161) | Destellos del color del tinte al sellar [P] | 07 §5.3; fila 13 |
| Dificultad solo por cantidad, sin mecánicas en el 143; pausas de 10-29 s (t = 15-131) | Siete mecánicas antes del 185 y etiquetas honestas | 02 §9.1; 02 §10 |
| HUD sin coherencia de color (t = 10-15) | UI kit con tokens | 07 §7 |
| «LOADING..» sin marca (t = 5-6) | «Cargando…» con el lazo de río solo si tarda > 300 ms | 02 §2 |
| Icono 3D que no se parece al juego (t = 0) | Icono con la cuia del juego | 07 §10.1 |

**Lo que sí tomamos de LW** (ficha §10.4):
- Entra directo al puzle: tras una pantalla de carga en t = 5-6, el tablero sale en t = 7.
- Su ciclo de tap-tap fluye a ≈ 1 s por vertido (t = 145-164).
- **Franja de aire visible** (t = 7): cada vaso deja una franja oscura vacía en la boca, sobre las dosis, y el espacio libre se lee de un vistazo. Valorar una franja equivalente en la cuia [P]; hoy la cavidad vacía se pinta con el fondo del mundo × 0,82 (07 §4.2). Va con la pregunta 4.4.
- **Desbloqueo por evento** (t = 10): una función bloqueada que se abre jugando el evento («Play event to unlock»). Para 1.2: una función o un marco que se gana en la Regata o en la Liga del río [P] (06 §5-§6).
- El botón de reiniciar siempre a la vista (t = 7): ya lo tenemos (Volver a la orilla en el HUD, 07 §7.4).

---

## 10. Cambios propuestos al GDD y preguntas para el dueño

### 10.1 Cambios propuestos (no aplicados)

Ninguno se aplica sin el visto bueno del dueño. Los que dependen de una pregunta llevan su número.

| Capítulo y § | Cambio | Depende de |
|---|---|---|
| `docs/dev/decisiones.md` | Una decisión nueva por cada aprobación que sustituya a otra («Cómo revertir»): a D-001 si gana `run` (4.1); al punto 5 de D-003 si se aprueba la cavidad (4.4); a D-004 si se aprueban las 3 filas (4.3.1), la pose B (4.3.3) o la boca a 0,2-0,6 u (4.3.4) | 4.1, 4.3, 4.4 |
| GDD.md §2 #2 y §8 (desviación 10) | Regla por defecto `run` | 4.1 |
| GDD.md §2 #23 | «34 × 102 dp con 7 por fila» → cuia de ≈ 43,6 dp con 11-15 cuias en 3 filas | 4.3.1 |
| GDD.md §4 | **Corregir:** el reintento de MS cambia de variante en cada intento, sin aviso (ficha §2.9). **Añadir dato observado** (no es corrección: el canon ya dice que el primer bloqueador nuevo de MS llega hacia el 221, lo que es compatible): bloqueadores en 387-389 (tela, hielo, armario); vidas cada 30 min, deducido y a confirmar (versión de 2026-10). Añadir filas de pose, tamaño de botella y sincronía de audio | — |
| GDD.md §6.1, pregunta 13 | Reabrir el contenido de la Canasta grande si entran Canoas infinitas | 10.2 #9 |
| 00 §4 | «Dos filas de hasta 7 cuias» → regla de 2 o 3 filas | 4.3.1 |
| 01 y 05 §4.2 | Precio de MS en Android: «Google Play CO: 9.500 COP por 1.000 monedas (t = 1084)», junto al de Apple (9.900) | — |
| 02 §1.2 (R9), §13 (nivel 2) y Desviaciones 3 | Modo `run` por defecto; lección del vertido parcial en el nivel 2 | 4.1 |
| 08 §9 | Default de `rules_pour_mode` = `"run"` si gana B | 4.1 |
| 02 §3.4; 08 §9; contrato 9 | Pista por inactividad solo en 3-30, con una clave nueva (no el alias retirado `hint_idle_until_level`) | 4.2 |
| 02 §4.1 y 08 §9 | Tiempos: subida 100, viaje 250, dosis 200 + cola 80, vuelta 250-300 ms. Ampliar el rango de `anim_pour_dose_ms` (hoy 80-150). Pose y altura de la boca. Audio en la deselección | 4.3.3, 4.3.4, 10.2 #7 |
| 02 §4.1 y 07 §4.3 | Revelado de Aguas turbias: de 300 ms a < 2 s con destellos; audio `sfx_murky_reveal` | — |
| 02 §5 y 07 §4.1 (a la vez, como exige 07 §4.1); 07 §7.4; 04 §2.2 | 3 filas de ≤ 5 desde 11 cuias, con el hueco del vertido reservado sobre la fila 1; el número de filas se decide con las T cuias generadas y no cambia a mitad de nivel. 07 §7.4 dice hoy «3 filas solo si > 14 cuias» y 04 §2.2 «abre una tercera fila» | 4.3.1 |
| 02 §7, 07 §5.5 y §7.5 | Los botones del modal entran por los lados y el tablero se ve detrás (hoy el modal «sube desde abajo») | — |
| 02 §8 | Cuia extra y Cuia de rescate en la fila más corta, recentrada, sin cambiar el número de filas | 4.3.1 |
| 02 §9.1, 03 §9; contratos 1 y 21 | Liana con fruta (`key`) además de con número (`n`); cambia la descripción de `key` en el esquema; casos nuevos en `tools/validate_schema.py` | 10.2 #8 |
| 02 §9 y 00 §6.5; contratos 1 y 21 | Reservar Aguapé (M8) y Nasa con pez (M9) para después del 183, tras la revisión cultural (aguapé, nasa, pez y tronco hueco siguen pendientes). Tipos nuevos `aguape` y `nasa` en `mechanic_type` y dosis marcadas en `af-level/1`: sube `pack_schema` (08 §4.1) | 10.2 #10 |
| 02 §10 y 06 §4.7 | Etiqueta Difícil / Muy difícil también en el botón Jugar del lobby | — |
| 02 §11 (T1), 02 §9.1, 03 §8-§9, 04 §2.5, 08 §9 | Contenido oculto como gancho general | 10.2 #15 |
| 03 y `tools/level-qa` | Métrica de «trampas tempranas» con tope por etiqueta | 10.2 #24 |
| 04 §1.1 y 05 §5 | Toque en un comodín sin saldo → hoja inferior con la ruta gratuita primero; el Puñado solo desde el 31 y nunca en la primera sesión; sin cuenta atrás | — |
| 04 §7.1 (y GDD §4) | Dato de MS: 30 min, deducido, versión de 2026-10. Corregir la fuente solo cuando se confirme | — |
| 04 §7.3 | Tras la Canoa extra por anuncio, cargar el nivel directamente | — |
| 04 §7.1, 05 §2.1 y §2.3, 06 §4.2 y §4.4, GDD §6.1 pregunta 13; `tools/econ_sim.py`, `tools/econ_variants.py` y `tools/out/econ_*.md` | Canoas infinitas de 15-30 min en la Bitácora y la Canasta grande: reabre una exclusión decidida; nueva corrida de la simulación | 10.2 #9 |
| 04 §7.3, 05 §6.2, 08 §9 | Canoas infinitas 15 min por anuncio (`rv_infinite_canoes_15` [P]; evento `af_infinite_canoes_start` con `source = rv`, que ya existe en 04 §11) | 10.2 #14 |
| 05 §4.1 y §5; 06 §4.7 | Orden del Mercado; ¿∞ 1 h en la Mochila?; oferta temporal como icono del lobby | 10.2 #22, #23 |
| 05 §6.1, §6.3, §7 y §10 | `maxAdContentRating = G` y bloqueo de apuestas, dinero real, trading y mercados de predicción en todas las bandas; prueba de que «Close» responde al primer toque | 10.2 #12 |
| 05 §6.1 | Dato de referencia: MS, 3 intersticiales en 20 min (276 y 434 s) | 10.2 #11 |
| 05 §7, 06 §4.2, 06 §2.3 y §4.7, 06 §5.1, 02 §6, 05 §2.4, 06 §4.3, GDD §2 #11, 02 §2, 04 §2.7, 05 §10 | Prácticas descartadas por el canon (sección 7.3) | 10.2 #17-#21 |
| 06 §4.5 | El Mapa del río muestra la próxima Canasta y cuántos niveles faltan | — |
| 06 §4.5 y §4.7, 07 §11 | Temáticas desbloqueables: escenas, galería, «Usar» y silueta | 10.2 #16 |
| 06 §6 | Liga del río con etapas de premio por puntos | — |
| 07 §4.1, §4.2 y §4.5 | Ancho interior ≥ 0,80 del exterior y 4 dosis ≈ 0,72 del alto exterior (1 dosis = 1 u; alto ≈ 5,6 u); elipse de ≈ 0,4 de dosis | 4.4 |
| 07 §4.3 y §5.2 | Pose y boca medidas (07 §4.3 fija hoy «Inclinación 38°») | 4.3.3, 4.3.4 |
| 07 §5.3 | Sellado: destellos del color del tinte y tapa a +1,15 s del inicio del chorro (≈ inicio del sonido del vertido), no de la salida de la cuia; el desfase exacto, con un vídeo a 60 fps | — |
| 07 §9.2 | Sonidos nuevos: `sfx_deselect`, `sfx_pour` (una muestra con variantes), `sfx_play`, `sfx_murky_reveal`, `sfx_no_funds`, `sfx_reward` (solo premios sin ID propio), `sfx_streak_lost`, `sfx_liana_free`, `sfx_hoja_open`; después `sfx_aguape_break`, `sfx_nasa_open`, `sfx_fish_jump`, `sfx_event_token`. Volver a la orilla usa `sfx_restart`; la Canasta, `sfx_basket_open`; el hito, `sfx_streak_up` | — |
| 07 §9.4 | Jerarquía de mezcla entre efectos (20-28 dB del gesto al premio) y sincronía con la tapa | — |
| 07 §1.3 | Añadir a la lista de trade dress lo visto: corcho dorado, telas rosa pálido, armarios naranja con candados de colores, botones morados con borde dorado | — |

### 10.2 Preguntas para el dueño

Todas cerradas, con recomendación. Orden sugerido: 5 antes que 3 (el ángulo condiciona las filas).

| # | Pregunta | Opciones | Recomendación |
|---|---|---|---|
| 1 | Regla de vertido en 1.0 (4.1; nueva evidencia para P6) | A mantener D-001 (`single`) · B `run` (decisión nueva que sustituye a D-001; packs regenerados; default de `rules_pour_mode`) | Probar `run` 10 min; si no hay preferencia clara, A |
| 2 | Pista por inactividad (4.2) | A como está · B solo 3-30 (revierte el contrato 9; clave nueva) · C apagada por defecto | B |
| 3 | Filas con 11-15 cuias (4.3.1) | A 3 filas de ≤ 5 con el hueco del vertido (≈ 43,6 dp; decisión nueva sobre D-004) · B 2 filas de 6-7 | A, después de responder la 5 |
| 4 | Cavidad de la cuia (4.4; reabre el punto 5 de D-003) | A ≥ 0,80 del ancho y ≈ 0,72 del alto exterior, con elipse · B como hoy | A |
| 5 | Ángulo de vertido (4.3.3) | A 38° · B pose medida (reabre la inclinación dinámica descartada en D-004 y el techo del vertido) | Verlas lado a lado; B solo si aceptas que el origen salga del panel y renuncias a casi todo el ancho de la 3 |
| 6 | Altura de la boca (4.3.4) | A 0,2-0,6 u (cambia la decisión 2 de D-004) · B 0,6-1,6 u | A |
| 7 | Ciclo de vertido (sección 3.2) | A copiar MS: ≈ 0,8-0,9 s de la salida al reposo · B mantener ≈ 0,54 s (0,66 s con la subida) | A |
| 8 | Liana con fruta (sección 5.2) | A añadir la variante `key` · B solo número `n` | A |
| 9 | Canoas infinitas cortas gratis (6.3; reabre 04 §7.1 y la pregunta 13 de GDD §6.1) | A en la Bitácora (sustituyendo casillas) y la Canasta grande · B lo mismo, pero sumando a la Bitácora · C no | Decidir tras una nueva corrida de `econ_sim.py` y `econ_variants.py`. Si la simulación no empeora los bloqueos sin recursos, A, sustituyendo casillas de comodines para no tocar las 3.000 semillas |
| 10 | Aguapé y Nasa con pez | A al backlog tras el 183, con el asesor cultural · B descartar | A |
| 11 | Frecuencia de intersticiales | A mantener y vigilar · B subir `ads_interstitial_min_gap_s` a ≥ 240 s ya | A |
| 12 | Guardarraíles de anuncios (requisito de cumplimiento: 05 §6.1 y §7) | Confirmar: G en todas las bandas, categorías bloqueadas y prueba del Close (no hay alternativa: es política) | Confirmar |
| 13 | Grabaciones que faltan (ficha §11.1) | A grabar: música activada (2-3 min); un vertido parcial y un destino no válido a 60 fps; la primera hora con una cuenta nueva; Journey completo pulsando «USE»; 2 min con vibración anotando qué vibra · B no | A |
| 14 | Canoas infinitas 15 min a cambio de un anuncio recompensado [P; no existe en MS: sus ∞ 15m son gratis y lo que «no es del todo» es que con ∞ activo siguen los intersticiales, ficha §6.5]. Postura del dueño (2026-10-08): copiar a MS; las canoas infinitas permiten reintentar sin límite durante su tiempo, pero no quitan los intersticiales | A en Sin canoas, como alternativa a la Canoa extra (+1) · B un botón «Ver vídeo» que alarga o duplica una recompensa de canoas infinitas de la Bitácora o la Canasta (solo si la 9 sale A o B) · C no. Con A o B: tope 1/día, nivel ≥ 31, placement `rv_infinite_canoes_15` [P], evento `af_infinite_canoes_start` con `source = rv` | A, tras simularlo con la 9. Si lo viste en MS en otra partida, grabarlo en la sesión de la 13 |
| 15 | Contenido oculto como gancho general (5.4) | A mantener 02 §11 · B Aguas turbias en niveles normales desde el 40-60 (≤ 2 dosis por cuia) + Liana y Nasa que tapan · C solo Liana y Nasa que tapan dejando ver el fondo | C ahora; B a prueba después |
| 16 | Temáticas desbloqueables (7.4) | A escenas por tramo, galería, «Usar» como fondo del lobby y la siguiente en silueta · B solo galería · C no | A, con 10 escenas en 1-200. **Respondida el 2026-10-13 por D-013** (Mapa del río): las tarjetas de parada (el fondo de cada parada en pequeño) y el Muestrario del río, con una parada cada 10 niveles (20 en 1-200); «Usar como fondo del inicio» queda para la 1.1 [P] |
| 17 | Bono diario con elección (7.3) | A chisteras al azar · B elegir 1 de 3 premios visibles · C Bitácora tal cual | B |
| 18 | Pantallas tras ganar (7.3) | A cadena entera · B un panel de «premios del día» · C 1 takeover | B |
| 19 | Multiplicador de racha (7.3) | A x1-x100 que cae a x1 · B multiplicador que baja un escalón, con escudo · C hitos 3 · 6 · 10 | C hasta simular B |
| 20 | Variante en cada reintento (7.3) | A variante sin aviso · B «Mismo río» u «Otro río», a elección · C misma semilla | B |
| 21 | Reloj común de eventos (7.3) | A varios eventos con el mismo reloj · B un reloj diario común con un solo evento temporal · C como hoy | B |
| 22 | ¿Canoas infinitas 1 h en la Mochila de inicio? (6.4) | A sí, guardadas hasta el 31 · B no | B: la Mochila se vende antes de que existan las canoas |
| 23 | ¿Oferta temporal como icono del lobby, sin interrumpir? (6.4) | A sí, desde el 31, con cuenta atrás y sin abrirse sola · B no | A |
| 24 | Bloqueos tempranos en `tools/level-qa` (5.5) | A rechazar siempre (salvo niveles 6 y 8) · B medir con tope por etiqueta · C no | B |
| 25 | Calificativo de victoria (fila 71) | A «¡Perfecto!» solo sin Cuia de rescate · B siempre «¡Nivel completado!», sin calificativo | A |
