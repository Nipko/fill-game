# Visión, posicionamiento y modelo de negocio

**Proyecto:** Amazon Fill (nombre interno) · **Nombre comercial:** Tintalú (forma ASCII "Tintalu"; decisión del dueño del 2026-10-06, que sustituye a Aquazonia) · **Capítulo:** 01 Visión del GDD (numeración del glosario: 00 Glosario, 01 Visión, 02 Núcleo, 03 Niveles, 04 Comodines, 05 Economía, 06 Meta, 07 Arte, 08 Técnica, 09 Roadmap) · **Fecha:** 2026-10-06 (sección 9 actualizada el mismo día con el hallazgo de marca del INPI sobre Aquazonia y, después, con la decisión del dueño por Tintalú; actualizado el 2026-10-11 con los debuts de D-007 en §4 (fila «Mecánicas nuevas») y en Dependencias, y el 2026-10-12 con la lista única de estrenos de 02 §9.1 en las mismas dos filas, sin tocar las cifras [sim], que esperan la aprobación de las regeneradas con D-009: GDD.md §7.3, V5; actualizado el 2026-10-13 (tanda 3) con **D-013**, el Mapa del río: §1 (elevator pitch: TiTino y «río arriba»), §5 (diferencial 6: fondos de 201-700), §8.3 («Rosa te lleva río arriba», Q13-A) y §11 (término «AAB / PAD»); actualizado el 2026-10-14 (T58, revisión de T54) con el 323 de la Tinaja en la lista única de estrenos (contrato 1; D-019, Q-Ti3-A): §4 (fila «Mecánicas nuevas») y Dependencias (fila 02); actualizado el 2026-10-19 (DC1b) con **D-029**: el dueño aprobó las cifras [sim] regeneradas con el ciclo de D-009 (pregunta 25 de GDD.md §6.3, (a); V5 cerrada), y §7.2 cita las nuevas) · **Estado:** aprobado por el dueño del producto el 2026-10-06; nombre Tintalú decidido con riesgo de marca medio aceptado y pendiente del dictamen del abogado de marcas (sección 9).

Convención de evidencia: **[V]** verificado en el informe citado · **[E]** estimación del informe · **[R]** reseñas de usuarios · **[NV]** no verificado · **[S]** cifra de terceros no confirmada en la fuente primaria · **[P]** propuesta de este capítulo a calibrar en soft launch. "(dirección N)" remite al punto N de las decisiones de dirección vinculantes. Los nombres de juego, moneda, vidas, comodines, eventos, mundos y mascotas son los del glosario canónico (`docs/gdd/00-glosario.md`).

## Resumen

Tintalú es un water-sort canónico con la economía del match-3 (el modelo con el que Magic Sort! pasó de 2,8 M USD de IAP en 2024 a 45 M USD en 2025, research/09 §Ficha [V]) y una temática de la cuenca amazónica real: frutas como tintes, cuias de vidrio como recipientes, 13 mundos por país y agua negra, blanca o clara. Copia las reglas, los puntos de precio y el fail-state del líder y corrige sus seis quejas estructurales: rescate caro sin ruta gratuita, comodines inútiles en el bloqueo, tablero que cambia al reintentar, colores ocultos como azar, anuncios tras cada nivel y 609 MB de descarga. El modelo es IAP-first híbrido (ancla 1.000 semillas = 1,99 USD; Cuia de rescate 600 semillas fijas desde el nivel 31) con intersticiales solo desde el nivel 22, sin banner, sin cajas de pago y con precios por país. Lanza Android-first en CO/PE/MX, luego BR, luego US, con metas D1 ≥30 %, D7 ≥10 %, D30 ≥4 % y UA pagada en US solo con D7 ≥15 % y ARPDAU ≥0,08 USD (dirección 19).

## 1. Elevator pitch

1. **Tintalú** es el water-sort que ordena el agua del Amazonas teñida con sus frutas: viertes açaí, aguaje y camu camu entre cuias de vidrio hasta que cada una queda de un solo color, río arriba desde el Estuario de Belém hasta El origen en los Andes, acompañado por TiTino, un perezoso que lo sigue desde su árbol en la desembocadura, Capi, una capibara que trae cuias y rema la canoa, y Rosa, una boto que te lleva río arriba (D-013: el Mapa del río remonta el Amazonas hasta una de sus nacientes).
2. Toma el núcleo y la economía que ya probaron ~1.000 M de descargas y ~280 M USD de IAP del subgénero Sort en 2025 (research/01 §1 [V]) y arregla lo que las reseñas más castigan: el rescate abusivo, el reintento que cambia el tablero, los "?" sin lógica, el anuncio tras cada nivel y el peso de la app (research/09 §Qué copiar pero mejorar [V]; research/05 §6 [V]).
3. Nace para Android en América Latina, donde el líder histórico no está traducido al español ni al portugués y el líder en ingresos cobra tiers de Estados Unidos sin descuento (research/08 §1.1 [V]; research/09 §Catálogo IAP [V]): precios por país, ≤80 MB de descarga inicial y Pix, OXXO y PSE desde el día 1.

## 2. Pilares de diseño

| # | Pilar | Qué significa (lo que sí hacemos) | Qué NO haremos | Evidencia |
|---|---|---|---|---|
| 1 | **Copiar lo probado, mejorar donde fallan** | Reglas canónicas: capacidad 4, se vierte sobre vacío o mismo color, victoria por monocolor, sin límite de movimientos ni tiempo, tap-tap con encolado (dirección 2). Puntos de precio del género: 1.000 semillas = 1,99 USD, 300 por racha de 3 (dirección 12). Primer intersticial en el 22 (dirección 13). | No inventar reglas de vertido ni añadir contador de movimientos o cronómetro en campaña. D-005 (2026-10-08) devuelve la regla del género: el vertido mueve la racha visible que cabe, con vertido parcial, como Magic Sort; `single` queda solo para pruebas (02 §1.2; 08 §9; `docs/dev/decisiones.md`). No "mejorar" el núcleo: el 100 % de los 1.000 M de descargas de 2025 ya lo domina (research/01 §Implicaciones 1 [V]). | research/01 §2 [V]; research/09 §Qué copiar tal cual [V] |
| 2 | **El fallo es justo y el rescate cumple** | El solver garantiza solución sin comodines; etiquetas Difícil / Muy difícil honestas (dirección 5). Reintento determinista; tras 3 fallos se sirve variante aliviada avisando "El río bajó" (dirección 7). Marea usable en el bloqueo; Cuia de rescate a 600 fijo (no 900; sin tramos ni cohortes por país) con primer rescate del día −50 % y ruta gratuita por Cosecha doble, Reto del río y Bitácora del río (direcciones 9-12). | No cambiar el tablero sin avisar. No cobrar el rescate antes de permitir usar el inventario. No colores ocultos sin ≥1 vertido seguro garantizado. No etiquetas que engañen. | research/09 §Qué NO copiar [V]; research/09 §Dificultad (caída de ARPDAU del líder) [V]; research/05 §6 [V] |
| 3 | **Calma primero: micro-sesiones sin fricción** | Nivel en memoria antes de animar; Llenar y teñir 1,2-1,5 s en 1-20, 0,8 s después, saltable al primer toque (dirección 18). Cero intersticiales antes del 22, solo tras completar nivel, cada 2-3 niveles, ≥150 s, tope 6/sesión; sin banner (dirección 13). Volver a la orilla gratis e ilimitado. Campaña jugable offline. | No intersticial al inicio de nivel ni en reintento. No banner. No pop-ups apilados: máximo un takeover por sesión fuera del bloqueo. No pedir permiso de push en el arranque frío. | research/04 §1.1 [V]; research/05 §4-5 [V]; research/09 §Mejorar (pop-ups) [V] |
| 4 | **Amazonía real, legible y respetuosa** | 14 tintes de frutas y aguas reales con HEX, icono y patrón por capa (Patrones de fruta, dirección 3); tres aguas base (negra, blanca, clara) del shader; 13 mundos con país y río reales; Dato del río cada 10 niveles; asesoría cultural remunerada con crédito en pantalla (dirección 15). | No "chamán", "pajé", kené, Yuruparí, pintura ritual, pueblos no contactados ni pueblos como personajes o skins. No "Amazon" a solas en el nombre ni paleta naranja #FF9900 sobre negro. No hitos del Orinoco presentados como Amazonas: siempre "vecinos del Orinoco". | research/07 §3.2, §6.1, §6.2 [V]; research/08 §4.2 [V] |
| 5 | **Ligero y local** | AAB inicial ≤80 MB con fondos por Play Asset Delivery (direcciones 16, 20); minSdk 26 (>95 % del parque brasileño, research/08 §5.2 [V]); ES-419, PT-BR y EN completos; precios por país en Play Console para BR, MX, CO, PE, AR, CL, EC (dirección 14); SDKs mínimos (LGPD). | No 282-609 MB de descarga. No 30 trackers ni acceso a contactos. No tiers automáticos de tienda sin ajuste de poder adquisitivo. No chat de texto libre. | research/09 §Técnico [V]; research/08 §5.2-5.3 [V]; research/08 C5.4 [V] |

## 3. Público objetivo

### 3.1 Perfiles de jugador

Ningún informe aporta demografía verificada del jugador de water-sort; lo medible hoy es que Brasil y México dan a Magic Sort su mejor nota mundial (4,8★, research/09 §Ficha [V]) y que en Brasil juega el 75,3 % de la población, 36,5 % Gen Z y 54,9 % clase media (PGB 2026, research/08 §5.1 [V]). Los perfiles siguientes son hipótesis de diseño [P] que Firebase Analytics debe confirmar o refutar en el soft launch; la columna "KPI que lo valida" dice con qué.

| Perfil [P] | Quién es y qué busca | Dispositivo y conexión | Momentos de juego | Qué consume / qué paga | KPI que lo valida |
|---|---|---|---|---|---|
| **La ordenadora de pausa** (núcleo, ~60 % de DAU esperado) | Adulta que juega para desconectar; valora "relajante", "sin cronómetro", "ejercita la mente" (top 5 de elogios del género, research/05 §6 [V]) | Android gama baja-media (Moto G, Galaxy A, Redmi), 3-4 GB RAM, datos limitados (research/08 §5.2 [E]) | 3-5 micro-sesiones de 1-3 min: transporte, filas, antes de dormir | Rewarded voluntarios (Cosecha doble, Canoa extra); rara vez IAP; sensible a intersticiales | Sesiones/DAU ≥3,5; duración mediana 60-180 s; opt-in push Android ~69 % (research/05 §5 [V]) |
| **El coleccionista de rachas** (~25 %) | Vuelve cada día por la Bitácora del río, el Reto del río y la Crecida; le duele perder la racha | Android gama media; wifi en casa | 1-2 sesiones largas (8-15 min) al día, a hora fija | Primer pagador de Mochila de inicio y Sin anuncios; compra semillas para sostener la Crecida | D7 ≥10 %, D30 ≥4 %; reclamo de Bitácora ≥70 % de DAU; conversión diaria a pagador ≥0,3 % [P] |
| **El estratega de Muy difícil** (~10 %) | Busca los niveles Difícil / Muy difícil y Aguas turbias · Bonus (desde 1.1); tolera 10-20 intentos si el tablero es el mismo | Android gama media-alta o iOS (MX 28 %, BR 21 % de iOS, research/08 §5.1 [V]) | Sesiones de 15-30 min por la noche | Cuia de rescate, Balsa de rescate, Luciérnaga, skins de cuia | ARPPU mensual ≥8 USD [P]; ≥40 % del IAP desde el modal Aguas estancadas [P] |
| **El invitado de tier-1** (US/UK/CA/DE, llega por UA desde la fase 3) | Ya jugó Magic Sort o Royal Match; espera *juice*, etiquetas de dificultad y eventos | Android e iOS gama alta | Patrón Magic Sort: 4,8 sesiones/día de 1-3 min (research/09 §Ficha [V]) | IAP-first: 77-90 % del ingreso en mercados maduros (research/04 §1.5 [V]) | ARPDAU ≥0,08 USD; D7 ≥15 % (dirección 19) |

Regla derivada: la primera sesión (niveles 1-10) se diseña para la ordenadora de pausa (cero anuncios, cero ofertas, Volver a la orilla gratis), la retención D7-D30 para el coleccionista (Bitácora, Reto del río, Crecida con Hoja escudo) y la monetización para el estratega y el invitado de tier-1 (Cuia de rescate desde el 31, Balsa de rescate 4,99).

### 3.2 Países y orden

| País | Papel en el plan | Android / iOS (móvil) | Métodos de pago en Google Play | Precio local del Puñado (1.000 semillas) | Evidencia |
|---|---|---|---|---|---|
| Colombia | Soft launch fase 1 (español, CPI bajo, PSE/Efecty) | Vendors "desconocido" 41,6 %; Apple 21,5 % | Tarjetas, Claro, **PSE** (Nequi, Nubank), **Efecty** (solo compras únicas) | 3.900 COP [E] | research/08 §5.2-5.3 [V]; research/04 §3.3 [E] |
| Perú | Soft launch fase 1 | Apple 14,4 %, Samsung 13,1 %, Xiaomi 12,3 % | Tarjetas, Claro, Entel (pospago) | 3,90 PEN [E] | research/08 §5.2-5.3 [V] |
| México | Soft launch fase 1 (mayor iOS de la región; efectivo) | 72,0 / 28,0 | Tarjetas, Telcel/AT&T, PayPal, Mercado Pago, **OXXO y 7-Eleven** (efectivo, no sirve para suscripciones) | 19 MXN [E] | research/08 §5.1-5.3 [V] |
| Brasil | Fase 2: mayor mercado (2,6 B USD, 9.º mundial, 2023) y marco legal más exigente | 78,8 / 21,2; Samsung 29,3, Motorola 16,1, Xiaomi 14,3 | Tarjetas, Elo, **Pix**, PicPay, Mercado Pago, PayPal | R$ 4,99 [E] | research/08 §5.1-5.3 [V]; research/08 C5 [V] |
| Estados Unidos (+ CA, GB, AU) | Fase 3: mercado de ingresos; UA pagada solo con D7 ≥15 % y ARPDAU ≥0,08 | Magic Sort #4 Casual gratis US | Tarjetas, saldo | 1,99 USD (ancla) | research/09 §Ficha [V]; dirección 19 |
| Argentina, Chile, Ecuador | Fase 2-3 con precio local (dirección 14); AR revisado mensualmente por inflación | Samsung 27,3 / Motorola 16,0 (AR) | AR: no detallado en la página oficial [NV] | AR equivalente a 0,99 USD [E] | research/08 §5.2-5.3 [V]; research/04 §3.3 [E] |

Comparativa que define la ventaja: Magic Sort cobra 1.000 monedas a R$ 12,90 / 39 MXN / 9.900 COP (tiers de Apple sin descuento), 2-3× el poder adquisitivo de EE. UU. (research/09 §Catálogo IAP [V]). Tintalú fija el Puñado en ≈50 % del ancla en BR/MX/CO/CL (nivel PricePush, research/04 §3.3 [V]) y revisa el tipo de cambio cada trimestre, porque las plantillas de precio de Play desaparecieron el 27-10-2025 y un override manual congela el país (research/04 §3.3 [V]).

### 3.3 Dispositivos de referencia

| Parámetro | Valor objetivo | Evidencia |
|---|---|---|
| Sistema | Android 8.0+ (minSdk 26), target API 36, páginas de 16 KB | dirección 20; research/08 §5.2 (Android 16 = 37,2 %, 15 = 21,4 %, 14 = 12,6 %, 13 = 12,2 % en BR) [V] |
| Hardware de referencia | 3-4 GB RAM, GPU Mali-G52 / Adreno 610, 64 GB compartidos con WhatsApp y fotos | research/08 §5.2 [E] |
| Pantalla | 19,5-20:9 (414×896, 412×915, 390×844, 360×800); hasta 16 cuias legibles a 360 × 780 dp: 3 filas de ≤ 5 desde 11 cuias (6/5/5 con 16; ≈ 47 dp con 11-15 y 39,2 dp con 16; D-006) y 2 filas de hasta 7 solo en 16:9 | research/08 §5.2 [V]; research/07 §4 [E] |
| Rendimiento | 60 fps en el hardware de referencia; teñido por shader, no por fluidos | research/08 §5.2 [E]; dirección 20 |
| Descarga | AAB inicial ≤80 MB (dirección 20) frente a 218-234 MB (Magic Sort Android), 282 MB (IEC), 609 MB (Magic Sort iOS) | research/09 §Técnico [V]; research/08 §1.1 [V] |
| iOS | Fase posterior; ATT opt-in gaming ~38-39 % (BR 51,4 %) | research/04 §2 [V] |

### 3.4 Momentos de juego

- Patrón del líder: 4,8 sesiones/día de 1-3 min (research/09 §Ficha [V]); mediana del mercado 5-6 min y ~4 sesiones/día (research/05 §1 [V]). Diseño: niveles de 60-120 s, nivel siguiente en memoria, animación saltable.
- Horarios LatAm: ningún dato verificado (research/08 §5.4 [NV]). Hipótesis a medir con Firebase durante 2 semanas antes de fijar live-ops por hora: picos 12-14 h y 20-23 h locales [E].
- Fines de semana: Duolingo mide caídas de DAU del 5-10 % (research/05 §3 [V]); la Hoja escudo (1 fallo perdonado al día) y la Bitácora sin reinicio absorben el fin de semana sin castigar.
- Push: pre-prompt propio en la **2.ª sesión**, tras la primera victoria con el nivel 10 ya superado (06 §8.1; nunca en el arranque frío ni en la secuencia del nivel 10); estado real del jugador (Reto del río listo, Crecida en riesgo, Bitácora sin reclamar); máximo 1/día en D1-D7 (research/05 §5 [V]).

## 4. Propuesta de valor: ellos / nosotros

| Dimensión | Magic Sort! (Grand Games) | Water-sort genéricos (IEC Global, Guru, Tripledot, SortPuz) | Tintalú |
|---|---|---|---|
| Reglas y control | Canónicas; tap-tap con encolado [V] | Canónicas; tap-tap [V] | Idénticas (dirección 2) |
| Fail-state | Bloqueo instantáneo, una sola oferta "+1 frasco" 900 monedas; rechazar = −1 vida, reinicio, racha perdida [V] | Reinicio gratis; "+1 tubo" por anuncio (IEC bajó a 1/4 de tubo) [V] | Modal Aguas estancadas con Marea (si queda cupo), Cuia de rescate 600 fijo (primer rescate del día 300; máx. 1 por nivel), Volver a la orilla gratis; nada de esto antes del nivel 31 (dirección 10) |
| Comodines en el bloqueo | Undo y Shuffle guardados **no** se pueden usar [V] | Undo limitado (5 gratis, luego anuncio) [V] | Marea usable en el bloqueo; ilimitada en los niveles 1-10 y 3 gratis por nivel desde el 11 (`undo_unlimited_until_level` = 10); recarga por anuncio o 30 semillas (dirección 9) |
| Ratio rescate / victoria | 900 : 10-20 ≈ 45 : 1 [E]; sin ruta gratuita a monedas [V] | Monedas solo cosméticas (IEC, Guru, Tripledot) [V/S] | 600 : 20 = 30 : 1 nominal; ≈8 : 1 sobre ingreso efectivo (~75/nivel, research/04 C1 §4 [E]); Cosecha doble, Reto del río, Bitácora y Crecida como ruta gratuita |
| Reintento | Variante distinta por intento; "−1 color" sin avisar en el 26 [V] | Varios barajan el nivel al reintentar [R] | Determinista (misma semilla); variante aliviada solo tras 3 fallos y avisada: "El río bajó" (dirección 7) |
| Colores ocultos | Frasco "?" desde ~L70; percepción de azar, insistente en BR/PT [V/R] | Niveles "?" obligatorios (IEC); queja n.º 6 del género [V] | Aguas turbias solo en Difícil / Muy difícil desde ~100, máx. 3 cuias turbias y 4 dosis ocultas por nivel (≤24 realidades; 02 §11 T2, `lvl_murky_max_hidden_units` = 4), ≥1 vertido seguro garantizado, Luciérnaga para revelar, bonus opcional con premio doble desde 1.1 (`ui_murky_bonus_enabled` = false en 1.0; dirección 6) |
| Etiquetas de dificultad | "Hard" / "Super Hard" con calavera; "imposibles sin booster" [V/R] | Sin etiquetas | Difícil (rana dardo, ámbar) / Muy difícil (jaguar, índigo); solver garantiza solución sin comodines; bot casual ×40 ≥75 % / ≥90 % (dirección 5) |
| Anuncios | Primer intersticial L22; "tras cada nivel" desde ~L50 [NV]; 67 % de reseñas negativas citan anuncios [V] | Intersticial tras cada nivel, al reiniciar y al pedir tubo; rewarded de 1-3 min (Guru) [V] | Ninguno antes del 22; solo tras completar nivel; cada 2-3 niveles; ≥150 s; tope 6/sesión; sin banner; rewarded ≤30 s con topes (dirección 13) |
| SKU sin anuncios | No existe [V] | 2,99 (IEC) a 7,99 (Guru); "sigo viendo anuncios" es la queja n.º 3 [V] | Sin anuncios 4,99: quita todos los intersticiales, conserva rewarded voluntarios, incluye bono de semillas (dirección 14) |
| Vidas | 5 corazones, 25-30 min; vida por rewarded y por equipo [V] | Sin vidas | 5 Canoas, 25 min, desde el nivel 31; Canoa extra por anuncio; Canoas infinitas temporizadas; Prestar una canoa en Aldeas v2.0 (dirección 11) |
| Racha | Broom Streak / Sky Jump expiran de noche; solo las sostiene quien paga [V/E] | No | Crecida con hitos 3 → 300 semillas · 6 → Canasta · 10 → 1 Cuia extra + río crecido; cada hito se paga una vez al día; tope diario 600 (05 §2.4, `streak_milestones`); Hoja escudo perdona 1 fallo/día; la racha no expira por reloj, solo por un fallo duro no escudado (04 §5.2; dirección 12) |
| Cofres y cartas | "Contains Loot Boxes" en ficha iOS; probabilidades no publicadas [V] | Rara vez | Canasta cada 10 niveles con contenido visible y siempre gratuita; Cuaderno de campo sin sobres de pago (dirección 15) |
| Social | Equipos con chat (13+), 3 leaderboards, cartas [V] | Game Center (Guru) | Liga del río (v1.2), Regata (v1.2), Aldea con mensajes predefinidos y Río de la aldea cooperativo (v2.0) |
| Localización y precios | 11 idiomas incl. ES y PT; tiers Apple sin descuento LatAm [V] | IEC sin ES ni PT en iOS [V] | ES-419, PT-BR, EN completos; precios por país en Play desde el día 1 |
| Peso y privacidad | 609 MB iOS / 218-234 MB Android; 30 trackers, 20 permisos [V] | 282 MB (IEC) [V] | AAB ≤80 MB + Play Asset Delivery; SDKs mínimos; sin contactos |
| Tema | Gatos magos, caldero, escobas (envoltorio; el motor es la economía) [V] | Sin tema | Amazonía real: frutas-tinte con nombre, cuias, 13 mundos por país, Capi y Rosa, Dato del río; el tema trabaja en creativos de UA y ficha, no en las reglas (research/08 §2 [V]) |
| Mecánicas nuevas | Primer bloqueador realmente nuevo ~L221; crítica "tarde y sin novedad" [V] | Capacidad variable y "?" | Estrenos de la 1.0 según la lista única de 02 §9.1 (`mech_debut_level`; D-007, decisión del dueño del 2026-10-08): 14 Cuia chica · 27 Cuia alta · 43 Hoja · 63 Matapi · 83 Liana con fruta · 100 Aguas turbias · 103 Remolino · 123 Barro · 143 Lluvia · 163 Brote · 183 Piraña · 323 Tinaja (M13, D-019: en la 1.0 si cumple su condición; si no, el mismo nivel en la 1.1); la Liana de cuenta (con número) es una variante, en el 243, y la Cuia alta de 6 sale en hitos desde el 141 (02 §8); en la 1.1, Aguapé 223, Palizada 263, Lancha 303 y Cocha 583 (dirección 8) |

## 5. Siete diferenciales priorizados

Esfuerzo en semanas de programación como tamaño relativo (columna B de 09 §3) [P]; impacto sobre la métrica indicada [P salvo cita].

| Prioridad | Diferencial | Qué incluye exactamente | Esfuerzo | Impacto | Por qué | Versión |
|---|---|---|---|---|---|---|
| 1 | **Rescate justo** | Marea usable en el bloqueo; Cuia de rescate 600 fijo desde el 31 (`rescue_price_bands` = [[31,600]]) con primer rescate del día −50 % y máx. 1 por nivel; ruta gratuita (Cosecha doble desde el 11, Reto del río, Bitácora ≈3.000/mes, Crecida 3 → 300 · 6 → Canasta · 10 → 1 Cuia extra + río crecido, una vez al día, tope 600; 05 §2.4) calibrada frente al coste del rescate | Bajo (1-2 sem; es balance, no código nuevo) | Alto en conversión y nota: >50 % del IAP del líder sale de esta pantalla (research/09 §Por qué ganó 2 [V]) y su ARPDAU cae por el 45:1 sin ruta gratuita (research/09 §Dificultad [V]) | Copia el motor de ingresos y elimina su queja estructural | 1.0 |
| 2 | **Reintento determinista + alivio comunicado + etiquetas honestas** | Misma semilla en reintentos inmediatos; tras 3 fallos variante −1 color o +1 vacío con mensaje "El río bajó"; etiquetas Difícil / Muy difícil con solver que garantiza solución sin comodines; intentos medios por nivel en Remote Config | Bajo-medio (2-3 sem: solver y bot ya exigidos por la dirección 5) | Alto en confianza y D7: "el puzle no recarga igual" y "imposible sin booster" son quejas transversales (research/09 §Núcleo [V]; research/05 §6 [V]) | Convierte una palanca opaca del líder en una palanca visible y defendible | 1.0 |
| 3 | **Patrones de fruta por capa** | Icono y patrón por tinte en cada dosis (toggle Patrones de fruta, activado por defecto desde el 41, disponible siempre); `max_pares_riesgo` 0/1/2 por tramo; modo alto contraste | Bajo (1-2 sem de arte + shader) | Alto en accesibilidad y nota: "colores que no se distinguen" es queja recurrente de IEC (research/08 §6 [V]); una paleta de 14 nunca es segura solo por color (research/07 §3.2 [V]) | El tema resuelve de forma nativa un problema del género | 1.0 |
| 4 | **Anuncios tardíos y predecibles + Sin anuncios honesto** | Ningún intersticial antes del 22, nunca al inicio ni en reintento, cada 2-3 niveles, ≥150 s, tope 6/sesión; sin banner; Sin anuncios 4,99 que quita todos los intersticiales y conserva rewarded con bono de semillas | Bajo (configuración de MAX + un SKU) | Alto en nota y D1: 67 % de las reseñas negativas del líder citan anuncios (research/09 §Anuncios [V]); retrasar el primero mejoró D1 5-8 % (research/04 §1.1 [V]) | Ataca la queja n.º 1 del género sin renunciar al 20-23 % de ingreso publicitario | 1.0 |
| 5 | **LatAm-first: idiomas, precios, pagos y peso** | ES-419 / PT-BR / EN completos; fichas con experimentos por país; precios por país en Play (BR, MX, CO, PE, AR, CL, EC) a ≈50 % del ancla; consumibles y packs únicos compatibles con Pix/OXXO/PSE/Efecty; AAB ≤80 MB con PAD | Medio (3-4 sem: localización, Addressables, pricing) | Alto en LatAm: líder Android sin ES/PT en iOS; 1.000 monedas del líder cuestan 2-3× en poder adquisitivo (research/08 §1.1 [V]; research/09 §Catálogo IAP [V]); 83,5 % Android en Sudamérica (research/08 §5.1 [V]) | Mercado semilla con CPI 0,14-0,60 (research/04 §4 [V]) | 1.0 |
| 6 | **Identidad amazónica como creativo** | Llenar y teñir con agua negra/blanca/clara; fondos de los mundos 1-4 en la instalación (Estuario, Tapajós, Manaos, Trapecio) y de Loreto (141-200) por Play Asset Delivery, con fondo "río genérico" en 201-700 (mundos 6-9) hasta que lleguen sus packs (con D-013, el kit del mundo con el rótulo del sitio, y el «río genérico» de respaldo; 07 §2.1); cuias con skins por mundo; Capi y Rosa; Dato del río cada 10 niveles; historia del Encuentro de las Aguas en ficha y primer screenshot | Medio-alto (6-8 sem de arte; shader) | Medio en CPI y D14-D30: arte variable sobre mecánica constante abarata la UA (Pixel Flow, research/08 §2 [V]); la tematización por sí sola no retiene (research/08 §2 [V]) | Diferenciación en estantería y en playables, no en reglas | 1.0 (mundos 1-4 + Loreto; río genérico en 201-700), 1.1 (resto) |
| 7 | **Legal-by-design para Brasil** | Cero compras aleatorias (ni directas ni con semillas compradas); Canasta visible y gratuita; Cuaderno de campo sin sobres; sin chat libre; pantalla de consentimiento propia BR/CO/PE/MX; SDKs mínimos; IARC sin descriptor de azar | Medio (2-3 sem: consentimiento propio + revisión legal) | Medio-alto: evita el art. 20 del ECA Digital (vigente desde 17-03-2026; multa hasta 10 % de la facturación en Brasil o R$ 50 M) y la faixa 18 de ClassInd (research/08 C5.1-C5.3 [V]) | El líder declara "Contains Loot Boxes", chat 13+ y 30 trackers: exposición que nosotros no heredamos (research/09 §Qué NO copiar [V]) | 1.0 |

## 6. FODA

**Fortalezas**
- Hueco temático real: no existe ningún water-sort con temática Amazonas, selva o frutas tropicales en el App Store de EE. UU. ni de Brasil (research/08 §Resumen [V]).
- Núcleo y economía ya probados: no hay riesgo de diseño en reglas, precios ni fail-state (research/09 §Qué copiar tal cual [V]).
- Legibilidad nativa: cada color es una fruta con icono y patrón (research/07 §3.2 [E]); resuelve daltonismo y poca luz sin coste adicional.
- Cuenta de empresa en Google Play lista y entorno de desarrollo instalado (Windows 11, Git, Python, Node, Java 17, Android SDK; dirección 20); conocimiento del mercado hispano.
- Marco legal brasileño resuelto desde el diseño, no parcheado después (research/08 C5.5 [V]).

**Debilidades**
- Sin marca ni base de usuarios: "Tintalú" tiene cero reconocimiento y depende del descriptor del título para la búsqueda (research/08 §3.1 [V]).
- Equipo pequeño (agentes de IA en programación + 1 artista freelance) frente a los ~75 empleados y 103 M USD de financiación del líder (research/09 §Ficha [V]); la cadencia de +100 niveles cada 1-2 semanas exige el pipeline offline desde el día 1 (dirección 17).
- Editor 6000.3.25f1 ARM64 nativo con builds IL2CPP locales lentos: la compuerta del día 1 pasó el 2026-10-06, pero el build limpio tarda 26,5 min con el NDK emulado (08 §16.2), así que la referencia de release es el CI Linux; curva de Shader Graph, Addressables y PAD (dirección 20).
- eCPM LatAm bajo: rewarded ~1,8 / intersticial ~1,3 USD en Android (Appodeal Q4-24, research/04 §1.4 [V]) → ARPDAU de anuncios 3-5× menor que en US.
- Riesgo de que Llenar y teñir alargue la carga y reduzca sesiones por día si no se respeta el límite de 0,8 s desde el 21 (research/08 §7 [E]; dirección 18).

**Oportunidades**
- Líder Android (IEC) en declive (≈54 K instalaciones/mes) y sin ES/PT en iOS (research/08 §1.1 [V]).
- Líder en ingresos con tiers sin descuento LatAm, 609 MB, "Contains Loot Boxes" y quejas de anuncios: el 67 % de sus reseñas negativas (research/09 [V]).
- Brasil: 9.º mercado mundial (2,6 B USD), 75,3 % de la población juega; Pix y Mercado Pago para compras únicas (research/08 §5 [V]). México: OXXO/7-Eleven en efectivo.
- Experimentos de ficha localizados (hasta 5 idiomas) y hasta 50 fichas personalizadas por país en Play (research/08 §3.2 [V]).
- El CEO del líder admite live-ops "sub-optimizados": la ventana está en meta, eventos justos y onboarding, no en el core (research/09 §Por qué ganó 8 [V]).

**Amenazas**
- Fast-follow: Grand Games ya replica su fórmula en subgéneros vecinos (Block Out! Color Sort, 285,8 K valoraciones en meses) y Tripledot o Guru pueden lanzar skins de selva en semanas (research/08 §1.1 y §7 [V]).
- Queja de marca de Amazon.com si el nombre o el icono se acercan a su marca; retirada temporal sin contranotificación ágil (research/08 §4.2 [V]).
- Fatiga del subgénero: el ARPDAU del líder ha caído significativamente desde su pico (research/09 §Dificultad [V]).
- CPI en puzzle casual: ~2,00 Android / ~3,00 iOS global; US Android casual 1,50-3,50 (research/04 §4 [V]); el líder solo bajó a 0,50 con 8 playables y dos redes (research/09 §Por qué ganó 4 [V]).
- Regulación en movimiento: ECA Digital, ClassInd 2025, LFPDPPP mexicana nueva (2025) y reglamento peruano 2024 (research/08 C5.4 [V/E]); cualquier cambio de criterio de IARC puede obligar a un re-rating.
- Clones del propio Tintalú si funciona: registrar marca y diseño de la cuia (research/08 §7 [V]).

## 7. Modelo de negocio en una página

### 7.1 Fuentes de ingreso

| Fuente | Qué es (nombre canónico) | Precio / regla | Cuándo aparece | Peso esperado en el IAP a 12 meses [P] | Evidencia |
|---|---|---|---|---|---|
| Packs de semillas | Puñado 1,99 (1.000) · Bolsa 4,99 (2.800) · Saco 9,99 (6.000) · Costal 19,99 (13.000) · Cosecha 49,99 (36.000) | Ancla 1.000 = 1,99 USD; precios localizados | Mercado desde el nivel 10; upsell desde Aguas estancadas sin saldo | 40-50 % (el sumidero principal es la Cuia de rescate) | dirección 12, 14; research/04 C1 §6 [E]; pack de 1,99 ≈40 % de primeras compras en CBJ (research/04 §3.2 [V]) |
| Balsa de rescate | Oferta IAP tras fallar: semillas + continuar el nivel + Canoas infinitas temporizadas, contenido determinista y visible | 4,99 USD; desde el 31; máx. 1/día | Modal Aguas estancadas sin saldo | 15-25 % | Oferta tras fallar >20 % del IAP en Color Block Jam y 33 % en Pixel Flow (research/04 §3.2 [V]); dirección 10 y 14 |
| Mochila de inicio | Pack único de entrada | 0,99 USD; ventana niveles 10-20; primera tarjeta en la primera victoria con nivel ≥12 dentro de una sesión ≥2 (`eco_starter_first_show` = "L12_or_session2"; 05 §4.1, 06 §2.2) | Una sola vez; nunca en la primera sesión | 5-10 % (primera compra) | dirección 14; Starter 0,99 del líder [V] |
| Sin anuncios | Quita intersticiales, conserva rewarded, bono de semillas | 4,99 USD | Tras el 3.er intersticial visto o en D2 (lo que llegue antes) | 5-10 % | dirección 14; research/05 §Primera sesión [E]; techo del género 7,99 (Guru) [V] |
| Cántaro | Alcancía determinista con tope visible | 2,99 USD; desde el nivel 20 | Se llena con victorias; se compra solo llena | 5-10 % | dirección 14; alcancía 1,99-4,99 en el género (research/04 §3.2 [V]) |
| Pase Expedición | Pase de 14 días | 6,99 USD | v1.2; desde el nivel 60 (05 §5) | 10-15 % desde 1.2 | dirección 14; pase 4,99-9,99 estándar (research/04 §3.2 [V]) |
| Canoas infinitas | Vidas infinitas temporizadas | En bundles y premios | Desde el 31 | Incluido en bundles | dirección 11 |
| Skins de cuia y mundo | Cuia de Santarém, Cuia del Encuentro, Totuma… | Se ganan por hitos **y** se venden en packs 1,99 | Mercado | 2-5 % | dirección 14 |
| Anuncios recompensados | Cosecha doble (x2), recarga de Marea, Morpho, Luciérnaga, Canoa extra, cofre diario | Topes 3-5/día por ubicación; cooldown 60-90 s; SSV | Primer rewarded voluntario en el nivel 11 | 60-70 % del ingreso publicitario | dirección 13; rewarded = 30-60 % del ingreso por ads (research/04 §1.2 [V]) |
| Intersticiales | Tras completar nivel | Desde el 22; cada 2-3 niveles; ≥150 s; tope 6/sesión | — | 30-40 % del ingreso publicitario | dirección 13; research/04 §1.1 [V] |
| Banner / app open | **No** | — | — | 0 | dirección 13 |

Mediación: AppLovin MAX con AdMob, Unity Ads, Meta y Mintegral; consentimiento UMP (GDPR) + pantalla propia para BR/CO/PE/MX; ATT en iOS; sin programa Families (dirección 13; research/08 C5.4 [V]).

### 7.2 Ancla de precios y lógica de la escalera

| Concepto | Valor | Equivalente USD nominal | Comparable del líder | Evidencia |
|---|---|---|---|---|
| Semilla | 1.000 = 1,99 USD → 1 semilla ≈ 0,002 USD | — | Idéntico (1.000 Coins 1,99) | dirección 12; research/09 §Catálogo IAP [V] |
| Victoria normal / Difícil / Muy difícil | 20 / 60 / 80 semillas | 0,04 / 0,12 / 0,16 | 10-20 [R]; bonus Hard no verificado [E] | dirección 12; research/04 C1 §1 [E] |
| Crecida (hitos, cada uno una vez al día) | 3 → 300 semillas · 6 → Canasta · 10 → 1 Cuia extra + río crecido (la barra vuelve a 0; el río crecido sigue activo hasta el siguiente fallo duro no escudado); tope diario 600, al que solo suman las 300 del hito 3 y las 50 semillas de la Canasta del hito 6; Hoja escudo 1 fallo/día (ver 05 §2.4) | 0,60 (hito 3) | 300 por racha de 3 [V] | dirección 12; 05 §2.4; research/09 §Economía [V] |
| Bitácora del río (30 casillas) | ≈3.000 semillas/mes | ≈5,97 | Calendario que "favorece vidas sobre monedas" [R] | dirección 12; research/04 C1 §3 [E] |
| Cuia de rescate | 600 fijo desde el nivel 31 (`rescue_price_bands` = [[31,600]]; sin tramos 750/900 ni cohorte 450 por país: el ajuste LatAm va en el precio local de los IAP); primer rescate del día 300; máx. 1 por nivel | 1,19 (0,60) | 900 ≈ 1,79 USD, sin descuento ni ruta gratuita [V/E] | dirección 10; 05 §3; research/09 §Economía [E] |
| Cuia extra (planificada) | 150; 1 por nivel con semillas (1 regalada en el nivel 8); solo con vertido legal | 0,30 | 900 como booster pre-nivel [V] | dirección 9 |
| Ratio rescate / ingreso | 600 : 20 = 30 : 1 nominal; ≈8 : 1 sobre ~75 semillas efectivas por nivel | — | 45 : 1 [E] | research/04 C1 §2 y §4 [E]; research/09 §Mejorar (objetivo 10-15 : 1) |
| Descuento por volumen | 2,0 ¢ (Puñado) → 1,4 ¢ (Cosecha) por 10 semillas | — | 1,6 ¢ en 5.000/7,99 [V] | research/04 C1 §6 [E] |

Regla de calibración que este capítulo fija y 05 Economía detalla: **ninguna fuente gratuita entrega Cuias de rescate como premio fijo**; la racha y el calendario pagan semillas y comodines de preparación (lección de Dream Games con "Super Duke", research/04 C1 §1 [V]). Primer muro de pago esperado del no pagador: research/04 C1 §5 [E] lo situaba en el nivel 180-220 (día 15-18); la simulación de **05 §3.4** con las tasas de fallo de 03 §15 y la Cuia de rescate a 600 fijo lo sitúa en ≈260 (≈ día 38 por interpolación [E]) para el perfil casual (74 % de las corridas) y casi nunca antes del 500 para el jugador medio que sostiene la Crecida; manda el embudo por nivel del soft launch (05 §3.4), no ninguna de las dos cifras.

### 7.3 Split esperado por región

| Región / fase | Ads | IAP | ARPDAU objetivo (USD) | Base de la estimación |
|---|---|---|---|---|
| LatAm Android (CO/PE/MX/BR), fases 1-2 | 55-65 % | 35-45 % | 0,012 piso / 0,015 meta (fase 1); 0,018-0,025 (fase 2) [P, 05 §8-§9] | Emergentes: anuncios 55-70 % del ingreso (research/04 §1.5 [V]); los ≈23 USD/1.000 DAU de research/04 §Estimación [E] incluyen banner y app open, prohibidos por la dirección 13; sin ellos, 1.000 DAU LatAm-heavy ≈ 14,9 USD/día → 0,015 (05 §8 [P]); la fase 2 sube a 0,018-0,025 con Brasil, Pix y la Cuia y la Balsa de rescate desde el 31 (05 §9 [P]) |
| Estados Unidos y tier-1 anglosajón, fase 3 | 25-35 % | 65-75 % | ≥0,05 orgánico; 0,08-0,12 con UA [P, 05 §9] | Magic Sort 77-80 % IAP con audiencia US (research/09 §Ficha [V]); 1.000 DAU US-heavy ≈ 48 USD/día en el escenario base de 05 §8 → ≈0,05, y 0,08-0,12 si la Cuia y la Balsa de rescate funcionan como en el líder (research/04 §Estimación [E]: ≈76 USD/día → 0,076 con banner y app open; 0,10-0,12 si la oferta tras fallar alcanza el 25 % del IAP); umbral de UA 0,08 (dirección 19) |
| Europa (ES, DE, FR, IT, UK) | 35-45 % | 55-65 % | 0,05-0,07 [P] | eCPM Europa rewarded ~5,1 / intersticial ~3,6 (research/04 §1.4 [V]); España a precio Eurozona (research/04 §3.3 [E]) |
| **Mezcla objetivo a 12 meses** (≈60 % del ingreso desde tier-1, 40 % desde LatAm) | **≈35 %** | **≈65 %** | **≈0,05-0,07** [P] | Entre el 59/41 del hybridcasual puzzle (Sensor Tower, research/04 §1.5 [V]) y el 80/20 del líder; coherente con "IAP-first híbrido" (dirección 13-14). El extremo bajo (≈0,05) corresponde al escenario base de 05 §8 sin banner ni app open (dirección 13) y una base de DAU mayoritariamente LatAm; el alto (≈0,07) exige que US pese ≥40 % del DAU y que la oferta tras fallar rinda como en el líder |

**Tabla única de metas de ARPDAU por fase:** los valores de ARPDAU de esta sección y de las compuertas de 8.1 son copia de la tabla **05 §9 "Metas de ARPDAU por fase"**, reflejada en el dashboard de **09 §6.1**. Si en algún momento difieren, prevalece 05 §9 y este capítulo se corrige; no se mantienen metas de ARPDAU propias aquí. Las cifras anteriores de este capítulo (0,025-0,035 en LatAm) procedían de los ≈23 USD/1.000 DAU de research/04 §Estimación, que incluyen banner y app open, formatos prohibidos por la dirección 13; por eso se sustituyen por 0,015-0,025.

Referencias de escala para el productor: 50 K DAU LatAm-heavy ≈ 22 K USD/mes netos (05 §8 [P], sin banner ni app open; research/04 §Estimación [E] daba ≈35 K con ambos formatos); 50 K DAU US-heavy ≈ 72 K USD/mes en el escenario base de 05 §8 [P] y ≈115 K USD/mes con los supuestos de research/04 §Estimación [E]. El líder, con 478 K DAU, facturaba 4,15-4,35 M USD/mes (research/09 §Por qué ganó 2 [V]); su ARPDAU medido por Sensor Tower es 0,16 (research/09 §Ficha [V]) y su RPD 1,70-1,84 USD.

### 7.4 Economía unitaria y costes

| Partida | Valor | Implicación | Evidencia |
|---|---|---|---|
| Comisión Google Play | 15 % hasta 1 M USD/año, 30 % después; suscripciones 15 % | IAP neto ≈85 % del bruto durante todo el primer año | research/04 §2 [V] |
| Comisión Apple (fase iOS) | Small Business Program 15 % con proceeds ≤1 M USD | Igual que Play en la escala prevista | research/04 §2 [V] |
| Impuestos LatAm | Google remite IVA en BR, MX, CL, CO; Brasil retiene IRRF + CIDE ≈25 % a desarrolladores pagados en moneda ≠ BRL [S]; IVA MX 16 %, AR/CL/CO/PE 18-22 % | Confirmar con contador antes del soft launch en BR; modelar ingreso brasileño neto ≈60-65 % del bruto [P] | research/04 §2 [V/S] |
| Ingreso publicitario | Llega neto de red, sin comisión de tienda | El 100 % del eCPM es nuestro | research/04 §2 [V] |
| CPI de referencia | LatAm 0,14-0,60; puzzle ~2,00 Android / ~3,00 iOS global; US Android casual 1,50-3,50; líder 0,50 Android con playables | UA pagada en LatAm solo como prueba de muestra; en US solo con LTV/CPI ≥1,5 | research/04 §4 [V]; research/09 §Por qué ganó 4 [V] |
| LTV de referencia | Ads-only LatAm D30 ≈0,12 USD; híbrido US-heavy D30 ≈0,65, D90 ≈0,9-1,1 USD | La UA en US exige LTV D90 ≥1 USD: solo se alcanza con Cuia de rescate + Balsa de rescate funcionando | research/04 §4 [E] |
| Equipo | Agentes de IA en programación (Claude Code con subagentes, modelo Opus; 09 §0) + artista 2D freelance + asesor cultural puntual + abogado de marcas | Decisión del dueño del 2026-10-06 (GDD §8, desviación 8); A/B quedan como histórico en 09 | dirección 21; research/07 §Implicaciones 7 [E] |
| Servicios | Unity 6.3 LTS 6000.3.25f1 (changeset e1dba0a9aba4) plan Personal; Firebase (Analytics, Crashlytics, Remote Config, Auth, App Check; 5 Cloud Functions en 1.0: SSV, verificación de compras, `restoreLedger`, `voidedPurchasesJob`, `deleteAccount`; 08 §2.4); GitHub Actions + game-ci; Codemagic para iOS | Coste fijo cercano a cero hasta decenas de miles de DAU; detalle en 08 Técnica | dirección 20; 08 §2.4 |

### 7.5 Lo que no monetizamos (y por qué)

| No existe en Tintalú | Razón | Evidencia |
|---|---|---|
| Cajas, ruletas o sobres de pago (ni con semillas compradas) | Art. 20 del ECA Digital y faixa 18 de ClassInd; la clasificación es por build, geobloquear no sirve | research/08 C5.1-C5.3 [V]; dirección 15 |
| Banner y app open | Sesiones de 1-3 min no los justifican y degradan la nota | research/04 §1.3 [V]; dirección 13 |
| Moneda dura en 1.0 | Una sola escala (semillas) mantiene legible el valor de cada comodín | dirección 12 |
| Suscripción como pilar | Métodos en efectivo (OXXO, Efecty) y PSE no soportan suscripciones | research/08 §5.3 [V] |
| Pack de 99,99 USD | Fuera del catálogo de la dirección 14; las ofertas que venden en puzzle caen en 6-15 USD | research/04 §Resumen [V] |
| Venta de Cuia extra por anuncio | Dirección 9 y 13 la compran solo con semillas; el anuncio recarga Marea, Morpho, Luciérnaga y Canoa extra | glosario §5.1 (regla económica de Cuia extra: solo semillas) |
| Rescate ni Canoas antes del nivel 31 | Hábito antes de la fricción: el líder no muestra fails hasta el 18 ni anuncios hasta el 22 | research/09 §Por qué ganó 6 [V]; dirección 10-11 |

## 8. Estrategia de mercado

### 8.1 Fases y criterios de paso

Todas las métricas se leen por cohorte de instalación en Firebase; una fase no avanza hasta cumplir **todos** sus criterios "obligatorios" durante dos semanas consecutivas. Duraciones [P]. Las metas de ARPDAU de la columna "Criterios deseables" son las de la tabla única **05 §9** (reflejada en 09 §6.1): fase 1 piso 0,012 / meta ≥0,015; fase 2 ≥0,018 (rango 0,018-0,025); US orgánico ≥0,05; UA pagada en US ≥0,08 (dirección 19). Las compuertas no se leen de ninguna otra tabla.

| Fase | Países y plataforma | Duración [P] | Objetivo | Criterios obligatorios para pasar | Criterios deseables |
|---|---|---|---|---|---|
| 0 · Beta cerrada | Pruebas internas y cerradas de Play Console; 200-500 testers de CO/PE/MX [P] | 2-3 semanas | Estabilidad, embudo 1-30, legibilidad de la cuia en 360 px | Crash-free sessions ≥99,5 % [P]; ningún nivel 1-10 pierde >5 % de los jugadores (research/05 §Primera sesión [E]); tiempo a primera interacción <20 s; AAB ≤80 MB; ≥600 niveles jugables con solver y QA (dirección 17; 700 instalados en el AAB, mundos 1-9, 08 §6.2) | Nota interna de legibilidad de 14 tintes con simulador de daltonismo (research/07 §Preguntas abiertas) |
| 1 · Soft launch LatAm hispano | **Colombia, Perú, México**, Android | 4-6 semanas | Retención y primer fallo; validar Cuia de rescate desde el 31 | **D1 ≥30 %, D7 ≥10 %, D30 ≥4 %** (dirección 19); ≥3.000 instalaciones por país para leer D7 con ±2 pp [P]; nota ≥4,5★ [P]; intentos medios por nivel sin picos >3× la mediana del tramo [P] | ARPDAU ≥0,012 piso, 0,015 meta [P, 05 §9]; conversión diaria a pagador ≥0,3 % (bueno es >0,5 %, research/04 §3.2 [V]); 1.er fallo en el rango 18-22 como el líder (research/09 §Onboarding [V]) |
| 2 · Brasil | Brasil (pt-BR, Pix); se mantienen CO/PE/MX; se añaden AR, CL, EC con precio local | 4 semanas | Mayor mercado y marco legal más exigente; ficha PT-BR | Pantalla de consentimiento propia + política LGPD con DPO publicadas; cuestionario IARC: publicidad sí, compras sí, ítems aleatorios **no**, azar **no** (research/08 C5.5 [V]); D1/D7/D30 de la fase 1 se mantienen en BR; nota ≥4,6★ [P] | ARPDAU LatAm blended ≥0,018 (rango 0,018-0,025) [P, 05 §9]; share de Pix en IAP medido para la ficha personalizada |
| 3 · Lanzamiento global | US, CA, GB, AU + Europa; Android | Permanente | Ingresos; UA pagada | **UA pagada en US solo con D7 ≥15 % y ARPDAU ≥0,08 USD** (dirección 19); LTV D90 / CPI ≥1,5 (research/04 §4 [V]); 6-8 playables idénticos al juego listos (research/09 §Qué copiar 16 [V]) | ROAS D30 ≥15 % Android (mediana casual, research/04 §4 [V]); +100 niveles cada 1-2 semanas sostenidos durante 8 semanas (dirección 17) |
| 4 · iOS | Mismos países, por Codemagic | +8-12 semanas tras la fase 3 estable [P] | Capturar MX (28 % iOS) y BR (21 %) y tier-1 | Paridad de funciones; ATT tras el consentimiento GDPR; Small Business Program activo | ATT opt-in ≥35 % (benchmark gaming 38-39 %, research/04 §2 [V]) |

Presupuesto de muestra para la fase 1 [P]: 3.000 instalaciones por país a CPI LatAm 0,14-0,60 (research/04 §4 [V]) = 420-1.800 USD por país en Google Ads, suficiente para leer D7 con ±2 pp (binomial, p ≈ 0,10, 95 %). Es una prueba de medición, no una campaña de crecimiento: LatAm crece por ASO localizado y orgánico (research/08 §5.4 [E]).

### 8.2 Qué se mide en cada fase (mínimo)

| Bloque | Eventos / métricas | Para qué decisión |
|---|---|---|
| Embudo por nivel | `level_start` (Firebase), `af_level_win`, `af_level_fail` (08 §8.2) con intentos, Marea usadas, tiempo, dificultad, semilla | Recalibrar intentos medios por nivel vía Remote Config (dirección 5); detectar muros |
| Bloqueo y rescate | Rama del modal Aguas estancadas: Marea / Cuia de rescate / Volver a la orilla / upsell (Puñado, Balsa de rescate) | Validar el precio fijo de 600 de la Cuia de rescate (`rescue_price_bands` = [[31,600]]; dirección 10, 05 §3); sin tramos ni cohortes por país en semillas: el ajuste LatAm va en el precio local de los IAP |
| Anuncios | Impresiones por ubicación, fill rate, eCPM por país, D1/D7 por cohorte con y sin intersticial desde el 22 | Cadencia de intersticiales por Remote Config |
| Economía | Saldo medio de semillas al cerrar cada tramo (50, 100, 200); % de "bloqueados sin recursos" (objetivo ≤3 % al 50, ≈8 % al 100, 15-20 % al 200, research/04 C1 §5 [E]) | Recortar primero Crecida, después Bitácora; nunca subir precios con recompensa fija |
| Tienda | Ficha por país: conversión visita → instalación (puzzle: 16,7 % Play, 2 % iOS, research/04 §3.2 [V]); experimentos de icono (cuia vs fruta) y título | Variantes de ficha; "Amazonia Sort" solo como variante con opinión legal |

### 8.3 UA y ASO por fase

- **Fase 1-2 (LatAm):** ASO como motor. Títulos "Tintalú: Ordenar Agua" (21) y "Tintalú: Classificar Água" (25), con tilde; descripción con el gancho del Encuentro de las Aguas y la línea "Juego independiente inspirado en la cuenca amazónica; sin relación con Amazon.com, Inc."; 3 primeras capturas con una cuia vertiendo açaí sobre un fondo de país; vídeo de 15-20 s con Llenar y teñir (research/08 §Implicaciones 3 [E]). Fichas personalizadas: Brasil con Pix y portugués, México con OXXO (research/08 §3.2 [V]). Experimentos de ficha desde el día 1 (hasta 5 idiomas, research/08 §3.2 [V]).
- **Fase 3 (US):** UA concentrada "pegada al producto": 6-8 playables idénticos al juego, creativos emocionales con Capi y Rosa ("Capi te trae una cuia", "Rosa te lleva río arriba" (D-013, Q13-A), "carrera contra la Crecida"), umbrales de clics 1/3/10/20/∞ (research/09 §Por qué ganó 7 [V]). Redes: Google Ads, AppLovin, Meta desde el inicio y TikTok/Mintegral como prueba, para no depender de dos redes (research/09 §Mejorar [V]).
- **Siempre:** rating ≥4,7 como requisito de entrada al top 10 de búsqueda en ES y PT (research/08 §3.1 [V]); publicar cambios de balance en notas de versión; no someter a A/B la economía núcleo (Cuia de rescate, Canoas) (research/09 §Mejorar [V]).

## 9. Nombre comercial y riesgos de marca

### 9.1 Decisión: Tintalú (2026-10-06)

Nombre interno **Amazon Fill** (carpeta `amazon-fill`, repositorio `Nipko/fill-game`, código y paquete `com.amazonfill.core`, ensamblados `AF.*`, prefijo de eventos `af_`; formas canónicas en el glosario §11; el `applicationId` público definitivo es `com.<estudio>.tintalu`, que se fija tras el dictamen de marca, y hasta entonces los builds de prueba usan `com.amazonfill.dev`, 08 §13.1); nombre comercial **Tintalú** (TINTA + -LÚ), decidido por el dueño el 2026-10-06 en lugar de Aquazonia: evoca el teñido en vivo del agua con frutas, la mecánica estrella, sin contener la cadena "amazon"; el agua y la selva los aportan el subtítulo, el arte y los mundos (glosario §1.2). Se escribe **con tilde en todo lo visible** (sin ella se lee "tinTAlu") y en ASCII, "Tintalu", solo en dominios, usuarios de redes, IDs y búsquedas. Títulos de ficha: **"Tintalú: Water Sort Puzzle"** (26), **"Tintalú: Ordenar Agua"** (21), **"Tintalú: Classificar Água"** (25). El descriptor hace el trabajo de búsqueda, como en el 100 % de los líderes (research/08 §3.1 [V]); el topónimo se recupera en icono, descripción y primer screenshot.

Por qué no "Amazon Fill": Amazon prohíbe incorporar sus marcas en nombres de producto y exige disclaimer; las tiendas retiran por queja del titular sin contranotificación ágil; las 9 apps con "Amazon" en el título del App Store de EE. UU. son de AMZN Mobile o licenciatarios; buscar "amazon" devuelve apps de Amazon (research/08 §4.2 [V]; research/07 §6.2 [V/E]). Amazon.com ganó además el TLD .amazon frente a los 8 países de la OTCA y opera Amazon Games desde 2012, también en móvil (research/07 §6.2 [V]).

Por qué no "Amazonia Sort" como nombre de lanzamiento: contiene la cadena "amazon"; topónimo + descriptor es marca débil o no registrable [E, principio general, research/08 §4.3]; convive en Play con "Amazonia Bingo – Social Casino" y "Casino Amazon" (glosario §1.2.1 [V búsqueda]), ruido indeseable para un título que debe clasificarse sin descriptor de azar. Queda como variante de experimento de ficha solo con opinión favorable de un abogado de marcas.

**Estado (2026-10-06): Tintalú decidido por el dueño, con riesgo medio aceptado.** El cribado preliminar de 48 candidatos (`docs/legal/nombres-candidatos.md` y su verificación adversaria; no es asesoría legal) le dio la mejor puntuación del jurado (83/100). No encontró ninguna app ni marca "Tintalu" en Google Play, App Store, USPTO, SIC ni IMPI, y según RDAP están libres `tintalu.com`, `.app`, `.co`, `.com.br`, `.games` y `.net` [V]. Riesgos que el dueño acepta [E]: (1) en México, EL TINTAL (IMPI reg. 2586508, clase 41, entretenimiento) está contenida entera en el nombre y el IMPI podría citarla de oficio, aunque es rebatible; (2) en la App Store existen "Tinta: Color Logic Puzzle" (Nicolas Zanette, 2026, puzle de colores) y "Tinta: Coloring Book & Paint", con riesgo de confusión y de uso previo en EE. UU.; (3) TINTA es casi descriptivo y está saturado en las clases 9 y 41 (TINTA CLUB DEL LIBRO, tinta.org, TINTA de Tinta Group, TINTAZUL…), así que la marca protegería el conjunto y no "Tinta-"; (4) "El Tintal" es una zona conocida de Bogotá y en Brasil el nombre suena a "Tintas Lu" (tiendas de pintura); (5) YouTube @Tintalu ya está reservado (canal vacío creado el 07-04-2026), TikTok parece libre e Instagram y X están sin verificar; (6) faltan el INPI, INDECOPI y la SIC por consultar directamente. El plan: registrar hoy `tintalu.com`, `tintalu.app`, `tintalu.co` y `tintalu.com.br` y reservar los usuarios en redes; encargar al abogado de marcas la búsqueda en el INPI (clases 9, 41 y 28), el dictamen en México frente a EL TINTAL y la estrategia en EE. UU. frente a TINTA; **ningún AAB a Google Play con el `applicationId` definitivo (`com.<estudio>.tintalu`) hasta el dictamen**, porque el ID es irreversible tras la primera subida (los builds de prueba usan `com.amazonfill.dev`). Si el dictamen falla, planes B por orden: Cuiazú (riesgo bajo), Botonda (riesgo bajo) y Selvagua (bajo fuera de Brasil, medio en Brasil por SELVA). El seguimiento vivo (solicitudes, plazos, abogados, dominios y redes) está en `docs/legal/marca.md`.

**Historia: por qué se descartó Aquazonia (2026-10-06).** Aquazonia (AQUA + -ZONIA) fue el nombre comercial desde el 2026-10-05 y el 2026-10-06 quedó primero condicionado al dictamen de Brasil. Se conserva el hallazgo como registro: la búsqueda preliminar de marca y dominios (`docs/setup/research/f-marca-aquazonia.md`, que no es asesoría legal) encontró un antecedente idéntico: la solicitud **INPI 938306200 "Aquazonia"** (Brasil; marca mixta; clase 41: fotografía, periodismo, guías y revistas en línea, publicación de libros), de Visualidades Comunicação e Design Ltda, presentada el 11-03-2025, con el plazo de oposición vencido el 24-05-2025 y **pendiente de examen de fondo, no concedida** al 04-10-2026 [V: TMview, ficha BR500000938306200]. Encaja con el proyecto periodístico **"Aquazônia"** de Ambiental Media sobre las aguas de la Amazonía, activo desde 2021-2022 [V]; el vínculo entre titular y proyecto es probable, no comprobado [E]. Fuera de Brasil no hay ningún "AQUAZONIA" en CO, PE, MX, US, UE ni Madrid, en ninguna clase; las tiendas están limpias; `aquazonia.com` está ocupado (aparcado, vence el 25-05-2027) y `.app` y `.co` están libres [V]. **Riesgo global: medio** (medio-alto en Brasil en la clase 41, medio en Brasil en la clase 9, bajo en CO, PE, MX y US) [E]. El plan de entonces era seguir con Aquazonia en CO/PE/MX, condicionar Brasil al dictamen de un abogado brasileño, registrar `aquazonia.app` (canónico) y `aquazonia.co` y no subir ningún AAB a Play hasta el dictamen. Ese mismo día el dueño descartó Aquazonia por esta solicitud idéntica y eligió Tintalú (párrafo anterior); la historia completa está en `docs/legal/marca.md`.

### 9.2 Riesgos de marca y mitigaciones

| Riesgo | Probabilidad / impacto [E] | Mitigación concreta | Responsable y momento |
|---|---|---|---|
| Queja de Amazon.com por el nombre, el icono o la descripción | Baja con "Tintalú"; alta con "Amazon Fill"; impacto alto (retirada temporal, ASO y reseñas perdidas) | Nombre acuñado; icono sin sonrisa/flecha ni naranja #FF9900 sobre negro; nombre de desarrollador sin ninguna forma de "Amazon"; disclaimer en la descripción (research/08 §4.2 [V]) | Dueño del producto antes del primer build público |
| México: EL TINTAL (IMPI reg. 2586508, clase 41, servicios de entretenimiento, vigente), contenida entera en "Tintalú" | **Media**; impacto medio: el IMPI examina anterioridades de oficio y una cita es plausible; es rebatible por la impresión de conjunto, pero retrasaría el registro en México [E] | Dictamen del abogado en México antes de presentar, con los argumentos preparados (también frente a TINTALAIRE, reg. 1971738, clase 41, edición); especificación estrecha de juegos | Dueño + abogado de marcas; esta semana; seguimiento en `docs/legal/marca.md` |
| App Store: "Tinta: Color Logic Puzzle" (Nicolas Zanette, 2026, puzle de colores en ES/PT/EN) y "Tinta: Coloring Book & Paint" | **Media**; impacto medio-alto en la fase US: confusión en la tienda, nombre que parezca derivado y posibles derechos por uso previo sin registro (*common law*) en EE. UU. [E] | Estrategia del abogado en EE. UU. frente a TINTA antes de presentar en la USPTO; usar siempre "Tintalú" completo, nunca "Tinta" a solas; icono, logotipo y capturas sin parecido con esas apps; repetir la búsqueda en las tiendas antes de cada lanzamiento | Dueño + abogado con licencia en EE. UU.; antes de la fase 3 |
| Elemento TINTA casi descriptivo y saturado en las clases 9 y 41 (TINTA CLUB DEL LIBRO, tinta.org, TINTA de Tinta Group, TINTAZUL…) | **Alta** de que convivan otras marcas "Tinta-"; impacto bajo-medio: la marca protegería el conjunto "Tintalú", no "Tinta-" [E] | Solicitud denominativa del conjunto TINTALÚ más el logotipo; no reclamar "Tinta"; vigilar TINTAL\* y TINTALU\* en TMview cada mes | Abogado de marcas, con la solicitud |
| Bases sin consultar directamente: INPI (Brasil), INDECOPI (Perú) y SIC (Colombia) | Desconocida [NV] | Búsqueda del abogado en el INPI en las clases 9, 41 y 28 (y la 2, porque en PT "tinta" es pintura) y consultas directas en INDECOPI y SIC; solicitud en las clases 9 y 41 en el país del estudio tras el dictamen y después CO, PE (siempre por vía nacional: no está en Madrid), MX, BR y US; **ningún AAB a Play con el ID definitivo hasta el dictamen**; presupuesto 3.000-6.000 USD [E] | Dueño + abogado de marcas; dictamen en 1-2 semanas [E] |
| Connotaciones: "El Tintal", zona conocida de Bogotá; en Brasil, "Tintas Lu" (tiendas de pintura); en inglés británico, "loo" (váter) | Baja; impacto bajo: bromas o búsquedas que devuelven otras cosas [E] | El descriptor de género en el título ("Water Sort Puzzle", "Ordenar Agua", "Classificar Água"); vigilar reseñas y búsquedas en CO y BR durante el soft launch | Dueño del producto |
| Usuarios en redes: YouTube @Tintalu ya reservado por un tercero (canal vacío creado el 07-04-2026); TikTok probablemente libre; Instagram y X sin verificar | Media; impacto bajo mientras el canal siga vacío | Reservar hoy TikTok, Instagram y X y una variante en YouTube; vigilar el canal @Tintalu y reclamarlo por la vía de marca cuando haya registro, si se activa con contenido confuso [E] | Dueño; hoy |
| Imitación de trade dress de Magic Sort (gatos, caldero, vidrio con la misma silueta, UI dorada biselada, calavera) | Media si el artista toma referencias directas; impacto alto | Cuia-garrafa propia con cavidad recta; Capi nunca fijo en el centro inferior con un objeto; rana dardo y jaguar en lugar de calavera; ficha redactada desde cero (research/09 §Qué NO copiar [V]; research/08 §4.1 [V]) | Artista 2D con revisión del dueño en cada hito de arte |
| Nombres de frutas y lugares reales | Nula: términos genéricos o indicaciones geográficas; nadie puede impedir decir "açaí" para un color (research/08 §4.3 [E]) | No usar marcas de bebidas ni denominaciones de origen como certificación | — |
| Clones de Tintalú si funciona | Media a 6-12 meses | Registro de marca y de diseño de la cuia; repositorio con historial del generador como prueba de creación independiente (research/08 §4.1 [E]) | Desde el primer commit |
| Tilde de "Tintalú" | **Media**: sin tilde se lee "tinTAlu", y dominios, usuarios e IDs no la admiten [E] | **"Tintalú" con tilde en todo lo visible** (logotipo, títulos de tienda, UI y materiales, también en PT-BR) y "Tintalu" solo en dominios, usuarios de redes, IDs y búsquedas (glosario §1.2; cierra de nuevo la pregunta 4 del glosario). *Histórico:* con Aquazonia se decidió no usar nunca "Aquazônia", la grafía exacta del proyecto periodístico brasileño (setup/f, paso 7 [E]) | Dueño del producto |
| Dominios aún sin registrar | Baja-media hasta registrarlos: hoy están libres según RDAP `tintalu.com`, `.app`, `.co`, `.com.br`, `.games` y `.net` [V] | Registrar hoy `tintalu.com`, `tintalu.app`, `tintalu.co` y `tintalu.com.br` (este, con CPF o CNPJ, a través del abogado brasileño); `tintalu.games`, `tintalu.net` y la variante fonética `tintaloo.com` como defensivos opcionales. Un dominio no da derechos de marca. *Histórico:* con Aquazonia el `.com` estaba ocupado y el canónico iba a ser `aquazonia.app` | Dueño; hoy (2026-10-06), a confirmar en `docs/legal/marca.md` |

### 9.3 Plan de contingencia

Si una tienda retira la ficha por queja de marca: (1) responder con la prueba de la búsqueda de antecedentes y el registro propio; (2) si la queja prospera, renombrar a la variante registrada de respaldo (ver pregunta abierta 2) sin tocar el `applicationId` (`com.<estudio>.tintalu`, inmutable en Play) ni los identificadores internos de Amazon Fill (`com.amazonfill.*`, `AF.*`, `af_`; glosario §11), porque el nombre visible, los textos y el icono viven en Addressables y en la ficha, no en el ID de la app; (3) la base instalada conserva progreso y compras (Play Games Saved Games). Coste estimado del renombrado tras el lanzamiento: 2-3 semanas de ASO y creativos [P], frente a cero si se registra antes de la fase 0.

**Contingencia de la marca Tintalú (2026-10-06):**
- **Si el dictamen es desfavorable** (en el INPI, en México frente a EL TINTAL o en EE. UU. frente a TINTA): se pasa a los planes B por orden, **Cuiazú** (riesgo bajo, provisional: falta el INPI), **Botonda** (riesgo bajo en las dos revisiones del cribado) y **Selvagua** (bajo fuera de Brasil, medio en Brasil por SELVA), cada uno con su propia búsqueda formal (`docs/legal/nombres-candidatos.md`, `docs/legal/selva-fill-cribado.md`). Como ningún AAB habrá salido con `com.<estudio>.tintalu`, el cambio antes del lanzamiento cuesta textos, logotipo y dominios, no la app.
- **Si el dictamen solo pone en duda un país** (por ejemplo, México por EL TINTAL): título y ficha localizados para ese país con una marca alternativa (Play admite títulos por idioma y fichas por país), sin retrasar los demás mercados; decisión del dueño con el abogado [P].
- **Si un titular de "Tinta" reclama en una tienda**: responder con el registro propio y la impresión de conjunto distinta; como último recurso, renombrar antes de la fase US [P].
- **Regla del `applicationId`:** hasta el dictamen los builds de prueba usan `com.amazonfill.dev`, que el CI no deja llegar a una build de release, y no se sube a Google Play ningún AAB con el ID definitivo `com.<estudio>.tintalu`, tampoco a la pista interna. El definitivo se fija con el dictamen favorable (o con el del plan B).
- "Igarapé" sigue reservado como posible nombre de evento, pero necesita su propia búsqueda: como palabra sola está libre en las clases 9/41, aunque hay marcas compuestas en la clase 41 en Brasil y es un sustantivo común en portugués (setup/f §Riesgos [V/E]).
- *Histórico (Aquazonia):* la contingencia anterior era específica de Brasil (título PT-BR sin "Aquazonia" y una marca alternativa solo para Brasil si el INPI concedía 938306200), con el ID provisional `com.estudio.aquazonia`. Quedó sin objeto al descartarse Aquazonia el 2026-10-06.

## 10. Riesgos principales y mitigaciones

| # | Riesgo | Señal temprana | Mitigación | Dueño |
|---|---|---|---|---|
| 1 | Dependencia del fail-state como motor de IAP ("forzar en vez de motivar"): el ARPDAU del líder cae por esta causa (research/09 §Dificultad [V]) | % de bloqueados sin recursos >5 % al nivel 50 u >10 % al 100; reseñas con "imposible" | Rescate justo (sección 5, #1); Cuia de rescate a 600 fijo (sin tramos 750/900 ni cohortes por país); alivio comunicado tras 3 fallos; recortar Crecida/Bitácora antes que tocar precios (research/04 C1 §5 [E]) | Diseño de economía |
| 2 | eCPM LatAm bajo convierte el soft launch en pérdida contable | ARPDAU <0,012 en la fase 1 (piso de 05 §9) | Aceptarlo: la fase 1 mide retención, no ingresos; el ingreso llega con la fase 3; consumibles localizados a ≈50 % del ancla compensan parte (research/04 §1.4, §3.3 [V/E]) | Producción |
| 3 | CPI tier-1 por encima del LTV | LTV D90/CPI <1,5 en la primera semana de UA | Gate duro de la dirección 19; playables idénticos al juego; apagar UA y volver a orgánico | Marketing |
| 4 | Fast-follow de Grand/Tripledot/Guru con skin de selva | Fichas nuevas con "jungle/amazon/fruit" en las keywords | Velocidad: ≥600 niveles jugables al lanzar (700 instalados, 08 §6.2) y +100 cada 1-2 semanas; registro de marca; diferenciales que exigen diseño (Marea en bloqueo, Aguas turbias seguras, Legal-by-design) y no solo arte | Dueño del producto |
| 5 | Incumplimiento legal en Brasil (ECA Digital, ClassInd 18, LGPD) | Pregunta de IARC respondida con "ítems aleatorios"; falta de DPO | Cero compras aleatorias; Canasta visible; consentimiento propio; abogado brasileño confirma autoridad del ECA Digital y tabla IARC 2025 (research/08 C5.5 [V]) | Legal / dueño |
| 6 | Nota de tienda <4,5 por anuncios o bugs de rewarded | Reseñas que citan anuncios >20 % de las negativas | Cadencia por Remote Config con kill switch; SSV en Cloud Functions para que el rewarded siempre pague; crash-free ≥99,5 % | Programación |
| 7 | Llenar y teñir alarga la carga y baja sesiones/día | Sesiones/DAU <3 en cohortes con animación completa | 0,8 s desde el 21, saltable al primer fotograma, solo con el nivel en memoria (dirección 18); A/B de duración | Programación |
| 8 | Cadencia de contenido no sostenible (curación del dueño y QA con tester externo; 09 R32) | Retraso >1 semana en el paquete de +100 niveles | Generación 100 % offline con semilla y JSON; QA automatizada con solver + bot ×40; primeros 150-200 a mano, el resto por pipeline (dirección 17) | Producción |
| 9 | Insensibilidad cultural (símbolos sagrados, pueblos como skins) | Comentario del asesor cultural o reseña en BR/PE | Lista prohibida del glosario §0.3; asesoría remunerada con crédito en pantalla; revisión antes de cada mundo nuevo (dirección 15) | Arte / asesor |
| 10 | Editor 6000.3.25f1 ARM64 nativo: compuerta del día 1 superada el 2026-10-06, pero con un build IL2CPP limpio de 26,5 min en local (08 §16.2); curva de Shader Graph, Addressables y PAD | Prototipo de vertido no listo en 3 semanas | Alternativa documentada Godot 4.8 (plan C) (dirección 20); prototipo del shader de líquido como primer hito técnico | Programación |
| 11 | Inflación y tipo de cambio (AR, y en menor medida CO/BR) | Precio local <40 % o >60 % del ancla | Revisión mensual de ARS y trimestral de CO/MX/BR/PE (research/04 §Otras decisiones [E]) | Producción |
| 12 | Métodos de pago en efectivo no soportan suscripción | Pase Expedición con conversión <1 % en MX/CO | Pase como compra única de 14 días, no suscripción (dirección 14); suscripción solo donde haya tarjeta (research/08 §5.3 [V]) | Economía |

## 11. Glosario de términos de negocio

| Término | Definición operativa en este GDD |
|---|---|
| **IAP** | Compra dentro de la app (packs de semillas, Balsa de rescate, Sin anuncios, Cántaro, Pase Expedición, skins). Bruto = lo que paga el jugador; neto = tras comisión de tienda e impuestos. |
| **IAP-first híbrido** | Modelo en que el IAP aporta la mayoría del ingreso y los anuncios complementan (objetivo ≈65/35 a 12 meses). |
| **Fail-state offer / oferta tras fallar** | Oferta que se dispara al no quedar vertido legal: Cuia de rescate (semillas) y Balsa de rescate (IAP). |
| **Rewarded (anuncio recompensado)** | Vídeo voluntario que entrega un premio verificado por servidor (SSV): Cosecha doble, recarga de Marea, Morpho, Luciérnaga, Canoa extra, cofre diario. |
| **Intersticial** | Anuncio a pantalla completa no solicitado; en Tintalú solo tras completar nivel desde el 22. |
| **eCPM** | Ingreso por 1.000 impresiones de anuncio; LatAm Android rewarded ~1,8 USD, US juegos hasta 30 USD. |
| **Fill rate** | % de peticiones de anuncio que reciben un anuncio; si no hay fill, el jugador debe poder pagar con semillas. |
| **Mediación** | Capa (AppLovin MAX) que subasta cada impresión entre redes (AdMob, Unity Ads, Meta, Mintegral). |
| **DAU / MAU** | Usuarios activos diarios / mensuales. |
| **D1 / D7 / D30** | % de instalaciones que vuelven el día 1, 7 y 30 tras instalar; metas 30 / 10 / 4 %. |
| **ARPDAU** | Ingreso neto diario por usuario activo diario; metas por fase en la tabla única 05 §9 (LatAm 0,015 → 0,018-0,025; US orgánico ≥0,05); umbral de UA en US 0,08 USD. |
| **ARPPU** | Ingreso medio por usuario pagador en el periodo. |
| **RPD** | Ingreso por descarga acumulado (líder: 1,70-1,84 USD). |
| **Conversión a pagador** | % de usuarios activos que compran en el día o en la vida; >0,5 % diario es bueno en puzzle. |
| **LTV** | Valor de vida de un usuario a D30/D90/D180; debe superar el CPI en ≥1,5×. |
| **CPI** | Coste por instalación en UA pagada. |
| **IPM** | Instalaciones por 1.000 impresiones de anuncio de UA; mide la calidad del creativo. |
| **ROAS** | Ingreso recuperado sobre el gasto de UA a D7/D30. |
| **UA** | Adquisición de usuarios pagada (Google Ads, AppLovin, Meta, TikTok, Mintegral). |
| **ASO** | Optimización de la ficha de tienda: título, descripción corta, capturas, icono, experimentos. |
| **Playable** | Anuncio interactivo que reproduce el juego real; umbrales de clics 1/3/10/20/∞. |
| **Soft launch** | Lanzamiento limitado por país para medir retención y economía antes del lanzamiento global. |
| **Cohorte** | Grupo de usuarios por fecha de instalación, país o variante de Remote Config. |
| **Remote Config** | Parámetros de Firebase que cambian el balance sin publicar versión (cadencia de anuncios, precio de rescate, umbrales). |
| **SSV** | Server-side verification: Cloud Function que confirma que el rewarded se vio antes de pagar el premio. |
| **Kill switch** | Parámetro de Remote Config que apaga anuncios, IAP o una función ante un incidente. |
| **AAB / PAD** | Android App Bundle (≤80 MB inicial) y Play Asset Delivery (fondos de los mundos 5-13 bajo demanda; en 1.0 solo Loreto, 141-200, y los niveles 201-700 usan el fondo "río genérico" hasta que lleguen sus packs; con D-013, el kit del mundo con el rótulo del sitio, y el «río genérico» de respaldo: 08 §6.2). |
| **minSdk / target API** | Versión mínima de Android soportada (26) y versión declarada (36). |
| **IARC / ClassInd** | Cuestionario de clasificación por edad de Google Play; ClassInd es la autoridad brasileña (14 esperado). |
| **ECA Digital** | Lei 15.211/2025 (Brasil): prohíbe cajas de recompensa de pago en juegos accesibles a menores. |
| **LGPD / LFPDPPP / Ley 1581** | Leyes de datos de Brasil, México y Colombia; exigen consentimiento propio fuera del UMP. |
| **UMP / ATT** | Plataforma de consentimiento de Google (GDPR) y permiso de rastreo de Apple (iOS). |
| **PPP** | Paridad de poder adquisitivo; base del precio local ≈50 % del ancla en BR/MX/CO/CL. |
| **Tier-1 / tier-3** | Mercados de alto eCPM e IAP (US, CA, GB, AU, DE) / mercados de bajo eCPM (LatAm salvo CL/UY). |
| **Hybridcasual** | Juegos de CPI casual con economía de IAP; benchmark de ARPDAU 0,15-0,50. |
| **Moneda blanda / dura** | Semillas (se ganan jugando y se compran) / no existe en 1.0. |
| **Sumidero / fuente** | Dónde se gastan las semillas (Cuia de rescate, comodines) / dónde se ganan (victorias, Crecida, Bitácora, Canasta, Cosecha doble). |

## Parámetros configurables (Remote Config)

**Ver 08 §9**, la única tabla de Remote Config del GDD (clave, tipo, default compilado, rango y capítulo dueño); este capítulo no repite defaults ni valores. Posee las compuertas de mercado, los kill switches y el registro de negocio (prefijo `biz_`, dueño 01); los valores de economía, anuncios y dificultad los fijan sus capítulos. Claves de este capítulo y qué gobiernan:

- `biz_market_phase`: fase de mercado (`closed_beta` · `soft_launch_latam` · `soft_launch_br` · `global` · `ios`); habilita funciones por fase (p. ej. Balsa de rescate solo desde `soft_launch_latam`) y segmenta cohortes.
- `biz_country_enabled`: compuerta por país además de la distribución de Play Console (fase 1: CO, PE, MX).
- `biz_iap_enabled` · `biz_ads_enabled` · `biz_interstitials_enabled` · `biz_rewarded_enabled`: kill switches de toda la tienda, de todos los anuncios (los recompensados pasan a cobrar semillas), solo de intersticiales (único interruptor de intersticiales) y solo de recompensados.
- `biz_legal_profile_by_country`: perfil legal por país (pantalla de consentimiento propia en BR/CO/PE/MX, UMP en EEE/UK/CH, `default` en el resto); selecciona la pantalla de consentimiento y el tratamiento de edad.
- `biz_under18_no_personalization`: anuncios no personalizados (`npa`), `maxAdContentRating = G` y PIN de compras cuando el jugador declara 13-17; la banda <13 queda bloqueada en 1.0 por `consent_under13_policy` (dueño 08), no por esta clave.
- `biz_disclaimer_in_store_copy` · `biz_store_title_variant` · `biz_store_icon_variant`: solo registro de la ficha activa (línea "sin relación con Amazon.com, Inc.", variante de título, variante de icono) para correlacionar instalaciones; `biz_store_title_variant` absorbe el antiguo `store_title_variant` del glosario (alias retirado).
- `biz_ua_paid_enabled_by_country`: UA pagada por país; se activa en US solo al cumplir D7 ≥15 % y ARPDAU ≥0,08 (dirección 19).
- `biz_kpi_targets`: metas D1/D7/D30 y compuertas de 8.1 para el panel interno; solo lectura, no cambia el juego.

## Desviaciones de la dirección

Ninguna. Las decisiones de este capítulo (perfiles, split por región, fases, presupuesto de muestra, Remote Config de negocio) están marcadas [P] y encajan en las direcciones 1, 10-17 y 19-22. Donde la investigación difiere de la dirección, prevalece la dirección y se anota aquí para trazabilidad:

- research/08 §Implicaciones 6 proponía soft launch en CO + PE y US solo con D7 ≥20 %; se aplica la dirección 19 (CO/PE/MX → BR → US; UA en US con D7 ≥15 % y ARPDAU ≥0,08).
- research/04 §Implicaciones proponía un pack de 0,99 con 450 semillas y un pack de 99,99; se aplica el catálogo de la dirección 14 (Mochila de inicio 0,99 como pack único; techo 49,99).
- research/05 §Implicaciones 7 proponía arrancar ad-driven y añadir la oferta de fallo después; se aplica IAP-first híbrido desde 1.0 con la Cuia de rescate desde el nivel 31 (direcciones 10, 13, 14).

## Preguntas abiertas para el dueño del producto

1. **Confirmar el nombre comercial** (la propuesta era "Aquazonia") y autorizar la búsqueda de antecedentes (USPTO, EUIPO/TMview, INPI, SIC, INDECOPI, IMPI; clases 9 y 41) y el registro denominativo + logotipo antes de la beta cerrada. Presupuesto y país de registro principal (país del estudio + Brasil). **Decidida (2026-10-06): Tintalú**, con riesgo medio aceptado; Aquazonia se descartó por la solicitud idéntica INPI 938306200 (sección 9.1). Siguiente paso: dominios y redes hoy y dictamen del abogado de marcas (INPI en las clases 9, 41 y 28; México frente a EL TINTAL; EE. UU. frente a TINTA); presupuesto de marca 3.000-6.000 USD; seguimiento en `docs/legal/marca.md`.
2. **Nombre de respaldo registrado**: ¿se registra también una segunda marca (p. ej. "Igarapé") para el plan de contingencia 9.3, o se asume el riesgo de renombrar sin marca previa? **Decidida (2026-10-06):** no registrar aún; "Igarapé" queda reservado como nombre de evento y, si hiciera falta, se busca antes de adoptarlo (9.3). **Actualizada el mismo día con Tintalú:** los planes B por orden son Cuiazú, Botonda y Selvagua; ninguno se registra hasta que el dictamen sobre Tintalú lo pida.
3. **Split objetivo**: ¿se acepta ≈65 % IAP / 35 % ads a 12 meses como meta de negocio, o se prefiere un objetivo más ad-heavy en LatAm durante el primer año?
4. **Presupuesto de soft launch**: 420-1.800 USD por país en Google Ads para 3.000 instalaciones de medición en CO/PE/MX (fase 1). ¿Se aprueba, o la fase 1 es 100 % orgánica (más lenta: 8-12 semanas para la misma muestra)?
5. **Umbral de nota de tienda** para pasar de fase: 4,5★ (fase 1) y 4,6★ (fase 2) son propuestas; el top 10 de búsqueda en ES/PT exige ≥4,7 (research/08 §3.1 [V]). ¿Se endurece?
6. **Argentina**: precio en equivalente USD con revisión mensual, o excluirla hasta la fase 3 por la volatilidad y la falta de datos de métodos de pago (research/08 §5.3 [NV]).
7. **Contador y fiscalidad**: confirmar el tratamiento de la retención brasileña (~25 % [S]) y la residencia fiscal del estudio antes de modelar el ingreso neto de BR.
8. **Asesoría cultural**: tarifa fija o porcentaje, y en qué hito entra (recomendado: antes del arte final del mundo 1 Estuario).

## Dependencias con otros capítulos

| Capítulo | Qué toma de este capítulo | Qué devuelve a este capítulo |
|---|---|---|
| 00 Glosario | Todos los nombres (Tintalú, semillas, Canoas, Capi, Rosa, cuia, comodines, eventos, sistemas, mundos) | Confirmación de nombres pendientes (Capi, Rosa, "cuia" invariante); IDs de código (00 §11: nombre interno Amazon Fill, prefijo `af_`, SKU `af_infinite_canoes_1h`) |
| 02 Núcleo y reglas UX | Pilares 1-3 (reglas canónicas, fallo justo, calma); catálogo de mecánicas nombradas (Hoja, Matapi, Liana, Remolino, Barro, Lluvia, Brote, Piraña; en la 1.1, Aguapé, Palizada, Lancha y Cocha; D-007) | Debuts canónicos para creativos y ficha (lista única de 02 §9.1, `mech_debut_level`: 14 Cuia chica · 27 Cuia alta · 43 Hoja · 63 Matapi · 83 Liana con fruta · 100 Aguas turbias · 103 Remolino · 123 Barro · 143 Lluvia · 163 Brote · 183 Piraña · 323 Tinaja (M13, D-019: en la 1.0 si cumple su condición; si no, el mismo nivel en la 1.1)); Cuia alta de 5 desde el 27 y de 6 solo en hitos desde el 141 (02 §8); tutorial 1-10 (02 §13); Aguas turbias 4 dosis / 24 realidades (02 §11 T2) |
| 03 Niveles, generación y dificultad | Metas de muro de pago (≤3 % al 50, ≈8 % al 100, 15-20 % al 200); etiquetas honestas como compromiso de marca | Intentos medios por nivel y tasas de fallo reales (03 §15) para recalibrar las metas de la sección 8.2; primer Difícil 40 y primer Muy difícil 60 (03 §11/§12); 2.190 tableros del Reto del río (03 §16) |
| 04 Comodines, canoas y rescate | Cuia de rescate 600 fijo desde el 31, primer rescate del día 300, máx. 1 por nivel; Marea ilimitada en 1-10 y 3 por nivel desde el 11; regla "ninguna fuente gratuita entrega Cuias de rescate" | Fichas de comodines y excepciones del tutorial (1 Cuia extra regalada en el 8, 3 Morphos en el 12); definición de fallo (04 §5.2); SKU `af_infinite_canoes_1h` (04 §7) |
| 05 Economía y monetización | Ancla 1.000 = 1,99; banda objetivo del ratio de presión 10-15:1 (05 §1.1); referencia actual 7,2-7,7:1 [sim, 05 §3.2]; split por región (7.3); catálogo de SKUs (7.1); lo que no monetizamos (7.5); kill switches `biz_*` | **Tabla única de metas de ARPDAU por fase (05 §9)**, que 7.3 y 8.1 copian sin modificar; escalera de la Crecida (05 §2.4, `streak_milestones`); muro de pago simulado (05 §3.4); tabla final de fuentes y sumideros; eCPM y conversión reales por país; decisión sobre test de Sin anuncios a 6,99 |
| 06 Meta, retención y live-ops | Perfiles 3.1 y sus KPI; metas D1/D7/D30; Hoja escudo y eventos no competitivos como promesa de marca | Calendario de eventos por fase de mercado (Temporada de lluvias, Travesía, Encuentro de las Aguas); permiso de push en la 2.ª sesión (06 §8.1); primera tarjeta de Mochila de inicio (06 §2.2) |
| 07 Arte, temática y audio | Pilar 4 (Amazonía real y respetuosa); lista prohibida; cuia-garrafa; Capi y Rosa; Llenar y teñir ≤0,8 s; icono sin elementos de Amazon | Hojas de estilo para capturas, icono y playables; orden canónico de la bandeja de comodines (07 §7.4) |
| 08 Arquitectura técnica y publicación | AAB ≤80 MB, minSdk 26, PAD, Firebase, MAX, SSV; perfiles legales por país; iOS en fase 4; títulos de ficha, disclaimer, variante de ficha "Amazonia Sort", planes B de nombre (Cuiazú, Botonda, Selvagua), riesgos de marca (9.2), cuestionario IARC y consentimiento propio para la checklist de publicación (08 §13) | Tabla única de Remote Config (08 §9) con los defaults de las claves `biz_*`; 700 niveles instalados y 5 Cloud Functions (08 §2.4, §6.2); `applicationId` `com.<estudio>.tintalu` (de prueba `com.amazonfill.dev` hasta el dictamen de marca); umbrales de crash/ANR de Play Console para la fase 0; coste real de servicios por versión; textos legales por país |
| 09 Roadmap, equipo, KPIs y riesgos | Fases de mercado y criterios de paso (8.1); equipo asumido; cadencia +100 niveles cada 1-2 semanas | Calendario vigente del equipo de agentes (09 §0.4); A/B como histórico; hitos de arte por mundo; dashboard de KPIs (09 §6.1) que refleja 05 §9 |

"ASO y legal" no es un capítulo del GDD: vive en la sección 9 de este capítulo (nombre, marca, ficha, planes B) y en 08 §13 (checklist de publicación y legal). El resultado de la búsqueda de marca se registra en 9.2 y la nota de tienda por fase en 8.1.
