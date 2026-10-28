# Lista de regresión de release (60 casos) · BORRADOR

- **Qué es:** la «Regresión de release» de 08 §14.3: 60 casos manuales que se pasan en el teléfono antes de cada build de release (pista interna, prueba cerrada y producción). Con D-059 (2026-10-28), la 1.0 no pasa por prueba cerrada y la pista interna es solo del dueño: la regresión se pasa con el ensayo de release (`BuildRelease.BuildAndroid -afRehearsal`) del mismo commit, antes de lanzar el Release firmado (`release.yml`), cuyo AAB el dueño prueba en la pista interna y promociona a producción. Cubre lo que pide 08 §14.3 (HUD, modal, Mercado, Bitácora, Canasta, Reto del río, cambio de idioma, notificaciones locales, segundo plano a mitad de vertido, pérdida de red durante compra y durante anuncio) y el resto de la 1.0. Borrador del bloque B13 de la tanda 5 (2026-10-20).
- **No sustituye** a las suites automáticas de 08 §14.2 (EditMode, PlayMode, `tools/level-qa`, capturas): aquí va lo que solo se ve en un teléfono real.
- **Dispositivos (08 §14.1):** como mínimo uno de clase A (gama baja, Android 8-11, 3 GB) y uno de clase C (Android 16, target API 36); las pantallas, 360 × 800 (20:9) y una 16:9. El teléfono de pruebas del dueño (Samsung SM-S938B, Android 16) es de clase C.
- **Cifras:** los defaults de la tabla única de Remote Config (08 §9). Si Remote Config cambia un valor, el caso usa el valor activo.
- **«Depende de»:** el bloque de la tanda 5 (`docs/dev/plan-tanda5.md` §4) o el SDK real que hace falta. Con la implementación falsa, el caso se prueba con el simulador; **para el release vale solo con el SDK real**.
- **Cómo se registra:** una copia por build en `docs/qa/` [P] con la versión, el dispositivo, OK / FALLO / N/A por caso y el enlace al fallo. En builds de desarrollo, `AF_STATE` y `AF_LAYOUT` en logcat ayudan a comprobar estados (`CLAUDE.md`).

## A. Instalación, arranque, edad y consentimiento

| ID | Pasos | Resultado esperado | Ref | Depende de |
|---|---|---|---|---|
| RR-01 | Instalación limpia; abrir el juego con red | Primera interacción en < 20 s (objetivo < 6 s); tras la edad y el consentimiento, el primer arranque entra en el nivel 1 | 08 §2.3 | B12 |
| RR-02 | Primer arranque; en la pantalla de edad, elegir «menos de 13»; cerrar y abrir | «Pide a un adulto que instale el juego»; no se crea sesión; sigue bloqueado al volver a abrir | 05 §7; 08 §2.3 | B12 |
| RR-03 | Instalación limpia; elegir 13-17 | Anuncios no personalizados (`npa`), `maxAdContentRating = G`; una compra pide la doble confirmación y la autenticación de Google Play | 05 §7; D-033 Q-T4 | B12; MAX; IAP |
| RR-04 | Con el teléfono en región BR, CO, PE o MX: pantalla de consentimiento; rechazar todo; luego, en Ajustes, aceptar la analítica | «Rechazar» al mismo nivel que «Aceptar»; con el rechazo, `npa` y Analytics apagado; el cambio en Ajustes se aplica y queda registrado (fecha, versión, país, decisión) | 05 §7; 08 §2.3 | B12; Firebase |
| RR-05 | Subir `consent_text_version` en Remote Config y reabrir | Vuelve a pedir el consentimiento una vez | 08 §9 | B12; B7 |
| RR-06 | Con el juego ya instalado, activar el modo avión, abrirlo y jugar 3 niveles | La campaña se juega; Remote Config usa los defaults; ningún error visible; el botón de Cosecha doble no aparece sin anuncio | 01 §2, pilar 3; 08 §2.3 | — |
| RR-07 | Actualizar desde el build de release anterior con un perfil avanzado (nivel > 100, semillas, comodines, Crecida) | El perfil migra (`profile_schema` n → n+1) sin perder nada; el juego abre el Inicio | 08 §4.4 | — |

## B. Partida y tutorial

| ID | Pasos | Resultado esperado | Ref | Depende de |
|---|---|---|---|---|
| RR-08 | Jugar los niveles 1-12 | Marea ilimitada en 1-10; Capi regala 1 Cuia extra en el 8; 3 Morphos en el 12; las instrucciones de TiTino en 1-3, 6 y 12 | glosario §5.1; 02 §13 | B11 |
| RR-09 | Verter una racha de 3 dosis sobre una cuia con hueco para 2; luego, un vertido ilegal | Vierte la racha que cabe (D-005); el ilegal sacude ±8 dp en 3 ciclos y 240 ms con `sfx_illegal`; al seleccionar no se resalta ningún destino (D-002) | 02 §4.1; D-002; D-005 | — |
| RR-10 | Completar una cuia y terminar el nivel | Tapa, maracá y háptico ligero al sellar; victoria cuando todas son de un color o están vacías | 07 §4.4; 02 §6 | — |
| RR-11 | Cargar un nivel del 1-20 y uno del 21+; tocar durante Llenar y teñir | 1,4 s en 1-20 y 0,8 s desde el 21; se salta al primer toque | 07 §5.1; 08 §5.3 | — |
| RR-12 | Volver a la orilla desde la pausa en el 35 con Crecida activa | Mismo tablero (misma semilla), gratis, sin gastar canoa ni romper la Crecida | glosario §5.1 | — |
| RR-13 | Fallar 3 veces seguidas el mismo nivel del 31+ | Al cuarto intento, «El río bajó»: el tablero aliviado, con aviso | 03; 08 §9 (`relief_after_fails` = 3) | — |
| RR-14 | Abrir el 40 (primer Difícil), el 60 (primer Muy difícil) y el 100 (Aguas turbias); usar la Luciérnaga | Etiqueta antes de jugar; en Aguas turbias, al menos un vertido seguro; la Luciérnaga revela las capas de una cuia | glosario §6; 02 §10 | — |
| RR-15 | Jugar el primer nivel de cada mecánica de la 1.0 (14, 27, 43, 63, 83, 103, 123, 143, 163, 183 y, si entra en la 1.0, la Tinaja del 323, que es condicional: 02 §9.1; D-019) | Tarjeta de desbloqueo con su texto; la mecánica funciona y la Marea la deshace | 02 §9.1; 07 §5.6 | — |

## C. HUD y pantallas

| ID | Pasos | Resultado esperado | Ref | Depende de |
|---|---|---|---|---|
| RR-16 | Nivel con 16 cuias en 360 × 800 y en 16:9 | 3 filas de ≤ 5 desde 11 cuias (6/5/5 con 16) en 19,5-20:9 y 2 filas de hasta 7 en 16:9; nada bajo los recortes ni la barra de navegación | D-006; 02 §5; 08 §14.1 | — |
| RR-17 | Revisar el HUD en un nivel con ocultos | Bandeja en el orden canónico (Marea, Cuia extra, Morpho, Pororoca y, con ocultos, Luciérnaga); controles ≥ 48 dp; Crecida en vertical en el borde derecho | glosario §5.1; 07 §7.4 | B2 |
| RR-18 | Fuente del sistema al 130 % en 360 px; abrir HUD, Aguas estancadas, Mercado y Ajustes | Sin desbordes ni solapes | 08 §14.2, «Accesibilidad» | B9 |
| RR-19 | Pausa › Mapa del río; «Volver al nivel» | El Mapa se abre encima del nivel en pausa y vuelve al mismo estado | 02 §5, cabecera | — |

## D. Modal Aguas estancadas y Canoas

| ID | Pasos | Resultado esperado | Ref | Depende de |
|---|---|---|---|---|
| RR-20 | Bloquearse en un nivel del 11-30 con Marea disponible; usar la Marea | Titular «No hay más vertidos»; Marea, Cuia de rescate y, al pie, «Volver a la orilla (gratis)»; la Marea deshace y cierra el modal | glosario §5.1; 04 §6 | — |
| RR-21 | Bloquearse en el 31+; Rendirse › ¿Seguro? | Línea «Travesía perdida: −1 canoa»; «Nivel fallido» y −1 canoa | glosario §10; D-008 | — |
| RR-22 | En el 31+, Cuia de rescate dos veces en el mismo día (en niveles distintos) | La primera del día cuesta 300 y la siguiente 600; como mucho 1 por nivel; aceptar no rompe la Crecida | glosario §5.1 | — |
| RR-23 | Bloquearse en el 31+ sin semillas para el rescate | Oferta Puñado 1,99 o Balsa de rescate (máx. 1 al día, contenido visible) | glosario §5.1; 05 §4.1 | B5; IAP |
| RR-24 | Gastar las 5 canoas; esperar 25 min con la app cerrada | Pantalla Sin canoas con cronómetro, Canoa extra por video, Llenar canoas (200 semillas) y Canoas infinitas 1 h; «Mientras esperas: Jugar el Reto del río»; a los 25 min vuelve 1 | 04 §7.3; 08 §9 | B11 |
| RR-25 | En el 31+, salir del nivel con el menú; en otra prueba, cerrar la app a mitad del nivel | Salir avisa «¿Salir del nivel? Perderás 1 canoa» y la cobra; cerrar la app no es fallo | glosario §2.2; D-008 | — |

## E. Comodines

| ID | Pasos | Resultado esperado | Ref | Depende de |
|---|---|---|---|---|
| RR-26 | En el 11+, gastar las 3 Mareas y recargar con 30 semillas | 3 gratis por nivel; +3 por 30 semillas o por video | 08 §9 (`undo_*`) | — |
| RR-27 | Comprar una Cuia extra (150) y reiniciar el nivel | 1 por nivel; sigue tras reiniciar el mismo nivel | glosario §5.1 | — |
| RR-28 | Usar la Morpho y luego la Pororoca | La Morpho marca el siguiente vertido del solver; la Pororoca baraja sin tocar las cuias llenas y el tablero sigue resoluble | glosario §5.1 | B2 |
| RR-29 | Fallar 5 veces el mismo nivel del 31+; Salto del Boto | Disponible solo tras 5 fallos, 300 semillas, como mucho 1 cada 10 niveles; no gasta canoa ni rompe la Crecida | 08 §9 (`skip_*`) | B3 |

## F. Victoria, Crecida y Canasta

| ID | Pasos | Resultado esperado | Ref | Depende de |
|---|---|---|---|---|
| RR-30 | Ganar un nivel; tocar Siguiente mientras se animan las barras | Orden del panel: título, Cosecha, hasta 3 barras, Cosecha doble, Siguiente e Inicio, Dato del río; el toque completa las animaciones, enseña 250 ms el estado final y sigue; sin auto-avance | D-033 #9 y #10 | B1b |
| RR-31 | Cosecha doble desde el 11 con y sin anuncio precargado | Sin anuncio, el botón no aparece; con anuncio, «+N → +2N» y avanza solo al terminar el video | 02 §6; D-011 | MAX |
| RR-32 | Desde el 25, ganar 3, 6 y 10 seguidos el mismo día | Hitos: 300 semillas; Canasta; 1 Cuia extra y río crecido; tope diario de 600; cada hito una vez al día; tras el 10 la barra vuelve a 0 | 05 §2.4 | — |
| RR-33 | Con la Hoja escudo disponible, fallo duro con Crecida activa | El escudo se gasta y la Crecida no se rompe | 05 §2.4 | — |
| RR-34 | Ganar un nivel múltiplo de 10 | La Canasta se abre sola dentro del panel; su contenido, el del ciclo `eco_basket_cycle` más 50 semillas, visible y sin azar | D-033 #8; 08 §9 | — |
| RR-35 | Reinstalar y restaurar el progreso en la nube tras pasar el nivel 30 | No se vuelven a cobrar Canastas ya cobradas (se deducen de `max_level`) | D-033 Q-M9 | B8; PGS |

## G. Bitácora del río

| ID | Pasos | Resultado esperado | Ref | Depende de |
|---|---|---|---|---|
| RR-36 | Primer nivel del día (ganar o perder) | La victoria lleva «Bitácora · elige tu premio»; 3 tarjetas; un toque sella; cerrar sin elegir la deja pendiente | D-033 #4 y #5 | B4 |
| RR-37 | Ignorar la Bitácora y volver al Inicio; volver otra vez en la misma sesión | La libreta sale sola una vez; no hay otro takeover en la sesión (`ui_max_takeovers_per_session` = 1) | D-033 #5; 08 §9 | B4 |
| RR-38 | Adelantar el reloj del teléfono un día y volver | No se sellan dos casillas en el mismo día; con deriva > 24 h, el premio queda «pendiente de validar» | 08 §10.1 | B4; B8 |
| RR-39 | Casillas 7, 14, 21 y 30 | La Canasta, la Canasta grande y el marco van fijos en las tres opciones | D-033 #2 | B4 |

## H. Reto del río

| ID | Pasos | Resultado esperado | Ref | Depende de |
|---|---|---|---|---|
| RR-40 | Ganar el 7 y llegar al 10 | Tarjeta de 2 s «Desde el nivel 10, un tablero nuevo cada día» tras el 7; el Reto aparece en el Inicio desde el 10 | 02 §13; 06 §2.2 | — |
| RR-41 | Jugar el Reto en dos teléfonos con la misma fecha; cambiar la fecha | El mismo tablero para la misma fecha; uno nuevo a medianoche local | 03 §16; D-031 | — |
| RR-42 | Ganar el Reto con y sin Aguas turbias; volver a jugarlo | 60 semillas, el doble con Aguas turbias; un premio al día; sin Cosecha doble gratis del río crecido | 08 §9; D-033 Q-C2 | — |

## I. Mercado, compras y anuncios

| ID | Pasos | Resultado esperado | Ref | Depende de |
|---|---|---|---|---|
| RR-43 | Abrir el Mercado | Semillas, Mochila (ventana 10-20), Sin anuncios, Cántaro y Canoas 1 h; precios tal como los da la tienda; «Restaurar compras» oculto hasta el adaptador real | D-033 Q-E5 y Q-E6 | B5 |
| RR-44 | Con licencia de prueba: comprar, cancelar y pagar con un método diferido (`PENDING`) | Compra ok: se acredita una vez; cancelar: nada; `PENDING`: no se concede hasta confirmarse | 08 §10.3; §14.3 | IAP; Firebase |
| RR-45 | Activar el modo avión durante una compra | «Confirmando…»; no se concede dos veces; reintento con backoff y en el siguiente arranque | 08 §10.3; §14.3 | IAP; Firebase |
| RR-46 | Reembolsar una compra de prueba | Voided Purchases la marca; en el siguiente `restoreLedger` se retiran las semillas o el derecho, sin bajar de 0 | 08 §10.1; §14.3 | IAP; Firebase |
| RR-47 | Comprar Sin anuncios; jugar 10 niveles en el 61+ | Ningún intersticial; los recompensados siguen; supresión de 24 h tras cualquier compra | 05 §6; 08 §9 | B5; IAP; MAX |
| RR-48 | Jugar del 1 al 70 sin compras, midiendo los intersticiales | Ninguno antes del 22; solo tras tocar Siguiente; nunca al empezar ni al reintentar; cada 3 niveles en 22-60 y cada 2 desde el 61; ≥ 150 s entre dos; ≤ 6 por sesión | 05 §6.1; 08 §9 | MAX |
| RR-49 | Activar el modo avión durante un anuncio recompensado | Sin recompensa si no se vio entero; la concedida queda provisional y se confirma o se revoca a las 24 h | 08 §10.2; §14.3 | MAX; Firebase |
| RR-50 | Ver un anuncio de cada red de la mediación | «Close» responde al primer toque; contenido apto (G), sin apuestas ni dinero real | 05 §7, D-029 | MAX |

## J. Idioma, accesibilidad y Mapa del río

| ID | Pasos | Resultado esperado | Ref | Depende de |
|---|---|---|---|---|
| RR-51 | Ajustes › Idioma: es-419 → en → pt-BR → auto, a mitad de una partida y en el Mapa | Cambia al momento sin reiniciar; nunca se mezclan idiomas; un texto largo sin traducir no se muestra | D-018; `.claude/rules/localizacion.md` | — |
| RR-52 | Con la región en Colombia y luego en México, ver semillas ≥ 1.000 y una distancia del Mapa | «1.000» en Colombia y «1,000» en México, con la misma función de formato | glosario §10; D-018 Q8-A | — |
| RR-53 | Activar Patrones de fruta, Alto contraste y Reducir movimiento | Patrón e icono en cada capa; el patrón al 35 % con Alto contraste; Llenar y teñir acortado | 02 §12; D-033 Q-N4 | B9 |
| RR-54 | Recorrer un tablero con TalkBack | Cada cuia anuncia posición, capacidad, capas visibles y ocultas y mecánica; la bandeja, icono y etiqueta | 08 §14.2 | B9 |
| RR-55 | Ganar el 10 y un nivel múltiplo de 10 en otro país | Victoria → libreta → Mapa → Inicio (≤ 4 pantallas, ≤ 30 s) en el 10; en el `10k`, placa de llegada, «Ver mapa» y la bandera izada al cruzar de país | D-033 #11; 06 §4.5 | B1b; B4 |

## K. Notificaciones, segundo plano, nube y cuenta

| ID | Pasos | Resultado esperado | Ref | Depende de |
|---|---|---|---|---|
| RR-56 | Segunda sesión con el nivel 10 superado: ganar un nivel | Pre-prompt del juego firmado por TiTino; solo si se acepta, el permiso nativo `POST_NOTIFICATIONS`; si se rechaza, un segundo y último pre-prompt en el hito 3 de la Crecida o a los 7 días (lo que llegue antes) y después solo el acceso en Ajustes | 06 §8.1; 08 §9 (`push_*`) | B6 |
| RR-57 | Dejar el Reto sin jugar y la Crecida en riesgo; cerrar la app | Notificaciones locales del catálogo de 06 §8.3 que nombran algo real (Reto listo, Crecida), nunca entre las 22:00 y las 9:00, como mucho 1 al día en D0-D7; se cancelan al abrir; tocarla abre su pantalla | 06 §8.2; 08 Desviaciones 2 y 8 | B6 |
| RR-58 | Mandar la app a segundo plano a mitad de un vertido (botón de inicio y llamada entrante); matar la app y abrirla | Al volver, el vertido está aplicado o no, nunca a medias; tras matarla, el mismo tablero y el mismo saldo (guardado por vertido) | 08 §11; §14.3 | — |
| RR-59 | Jugar en el teléfono A hasta el 30; abrir con la misma cuenta de Play Games en el B (que iba por el 20) | Gana el de mayor `max_level`; los consumibles no se suman; las compras se reacreditan | 08 §11 | B8; PGS; Firebase |
| RR-60 | (a) En una instalación con una cuenta de prueba, Ajustes › «Eliminar cuenta y datos». (b) En una segunda instalación con otra cuenta de prueba, apuntar su código de cuenta de Ajustes (con guiones) y borrarla desde la página web. (c) Caso negativo (Q-B13-21, resuelta en D-035): intentar borrar desde la web con un código inventado o ya rotado | (a) Dos pasos de confirmación con el aviso de que las compras no se reembolsan; el juego vuelve a la primera ejecución. (b) La web borra esa cuenta de prueba y no toca la primera; Saved Games y los datos del teléfono, según Q-B13-22. (c) La web rechaza el código y no borra nada; la web normaliza guiones, espacios y mayúsculas (D-033, añadido del 2026-10-22, punto 11) | 08 §11; `infra/web/src/content/*/delete.html` | B8; Firebase; web |

## Fuera de esta lista (08 §14.3, otras fases)

- **Pista interna** (con D-059: sigue valiendo, solo con el dueño como único tester; `docs/dev/iap.md` §3)**:** restauración en un segundo dispositivo, PAD con Wi-Fi apagado, consentimiento por país con VPN, ATT (iOS).
- **Prueba cerrada LatAm** (con D-059: no la hay en la 1.0; se ve en la prueba completa en el teléfono y, tras el lanzamiento, en Crashlytics y Analytics)**:** crash-free ≥ 99,5 % [P], FPS en clase A, legibilidad de los 14 tintes con simulador de daltonismo, embudo 1-30, comprensión de «El río bajó» y de las etiquetas.
