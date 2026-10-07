# Retención, meta-juego y live-ops en sort puzzles: qué copiar para Amazon Fill

Fecha: 2026-10-05. Investigación de escritorio (fuentes 2025-2026 priorizadas). Convención: **[V]** = dato verificado, URL en "Fuentes"; **[E]** = estimación, inferencia propia o cifra de fuente secundaria cuyo original no pude abrir (paywall / PDF ilegible / límite de búsqueda).

## Resumen ejecutivo

- Puzzle es el género que mejor retiene en móvil: D1 ≈ 32 %, D7 ≈ 12 %, D30 ≈ 5 % (Mistplay vía Segwise) frente a medianas de toda la industria de D1 22-27 %, D7 3,4-3,9 % y D30 < 1 % (GameAnalytics 2025/2026). Meta mínima de soft launch para Amazon Fill: D1 ≥ 30 %, D7 ≥ 10 %, D30 ≥ 4 % [V para benchmarks, E para la meta].
- La sesión mediana en móvil es de 5-6 min y ~4 sesiones/día (GameAnalytics 2025); los top puzzle de EE. UU. llegan a ~38 min de juego diario (Naavik). El water sort es un juego de "micro-sesiones": diseñar niveles de 60-120 s y bucles de regreso de 3-4 veces al día [V/E].
- El subgénero sort explotó con la capa hybrid-casual: antes de 2023 todos los water sort juntos facturaban < 2 M USD/año; Magic Sort superó 40 M USD en su primer año (Deconstructor of Fun) y el subgénero Sort aportó el 13 % del crecimiento interanual de un género puzzle que superó 10 000 M USD de IAP en 2025 (Sensor Tower vía Naavik) [V].
- Los water sort "clásicos" (IEC Global, Guru) siguen siendo pobres en meta: undo limitado (5 gratis, luego anuncio), +1 recipiente por anuncio, eventos estacionales y temas. El líder en ingresos (Magic Sort) ya tiene equipos, álbum de cartas coleccionables, retos diarios, eventos, clasificaciones y actualizaciones de +100 niveles por versión [V].
- En casual, las mecánicas live-ops más usadas en 2025 son colecciones (75 %), carreras/races (68 %) y álbumes (66 %); win streak está en el 48 % de los títulos y creció > 50 % en el año (AppMagic) [V]. Son las primeras a copiar.
- Los calendarios de login se usan en casi todos los juegos; el modelo "si fallas un día vuelves a cero" casi no se usa ya (Mistplay). Duolingo midió +14 % de D7 con "apuestas de racha" y −5 % de pérdida de racha con un amuleto de fin de semana: el perdón de racha es la mejor práctica, no el castigo [V].
- Onboarding probado: Magic Sort no muestra anuncios hasta el nivel 22 y monetiza el fallo con una oferta "+1 recipiente" (Gamigion). En casual puzzle los boosters dentro del nivel aparecen hacia el nivel 7 y los pre-nivel hacia el 9 (Liquid & Grit, resumen público) [V/E].
- Las 1 estrella del género son monótonas: anuncios tras cada nivel (tema n.º 1 por mucho), anuncios de 30-60 s, anuncios pese a haber pagado "sin anuncios", niveles con colores ocultos "?", niveles percibidos como imposibles sin pagar, repetición después del nivel ~200, crashes/calentamiento. Las 5 estrellas: relajante/meditativo, "ejercita el cerebro", adictivo, sin cronómetro, jugable offline [V].
- Push: el opt-in en juegos hypercasual es de solo 23 % en iOS y 69 % en Android, con CTR de 0,8-1,05 % (Pushwoosh 2025). Pedir permiso con pantalla previa y después de la primera "victoria", y enviar tantos avisos como veces entra el jugador (OneSignal) [V].
- Volumen de contenido: los líderes anuncian 5 000+ (Infinite Joy, Water Sort Puz) y 10 000+ niveles (Water Sort Club); todos usan generación procedural con solver. Magic Sort añade 100 niveles por actualización y Guru publica una versión al mes [V]. Para nosotros: generador + solver desde el día 1, con los primeros 150-200 niveles curados a mano [E].
- Balance de monetización típico en puzzle lógico: ~30 % anuncios / 70 % IAP; en casual general ~50/50 (CAS.AI). Intersticiales solo en pausas naturales, 1 cada 3-5 min, y cada intersticial forzado de más cuesta D7 (guías de industria) [V/E].
- Propuesta: v1.0 con reto diario + calendario mensual + racha con perdón + 3 fondos-país + tienda mínima; v1.1 álbum de frutas y skins de recipientes; v1.2 eventos temporales, win streak y liga semanal; v2.0 equipos y álbum infinito por temporadas.

## 1. Benchmarks de retención y sesión (2025-2026)

| Métrica | Valor | Segmento | Fuente | Estado |
|---|---|---|---|---|
| D1 | 31,85 % | Puzzle | Mistplay vía Segwise | [V] |
| D7 | 12,18 % | Puzzle | Mistplay vía Segwise | [V] |
| D30 | 5,35 % | Puzzle | Mistplay vía Segwise | [V] |
| D1 / D7 / D30 | 29,3 / 5,9 / 1,4 % | Hyper-casual | Mistplay vía Segwise | [V] |
| D1 / D7 / D30 | 28-32 / 9-12 / 3,5-5 % | Casual+Puzzle Android | AppsFlyer Q3-2022 vía Game Growth Advisor | [V, dato antiguo] |
| D1 top 25 % | 26,5-27,7 % (iOS 31-33 %, Android 25-27 %) | Todos los géneros | GameAnalytics 2025 | [V] |
| D7 mediana / top 25 % | 3,4-3,9 % / 7-8 % | Todos | GameAnalytics 2025 | [V] |
| D28 | 75 % de los juegos < 3 % | Todos | GameAnalytics 2025 | [V] |
| D30 mediana / top 1 % | 0,68-0,79 % / 13-15 % | Todos | GameAnalytics 2026 vía Game Growth Advisor | [V] |
| D1 / D30 | 27 % / 5 % | Todos | Adjust 2025 | [V] |
| Sesión mediana / top 25 % | 5-6 min / 8-9 min | Todos | GameAnalytics 2025 | [V] |
| Sesiones por día | ~4 (Android > iOS) | Todos | GameAnalytics 2025 | [V] |
| Tiempo de juego diario mediana | 22 min | Todos | GameAnalytics 2025 | [V] |
| Sesión media puzzle | 23,9 min (2023) → 24,5 min (2024) | Puzzle | Adjust (blog puzzle) | [V, resumen de búsqueda] |
| Sesiones día 0 | 1,65 (hyper/hybrid 1,82) | Todos | Adjust 2026 / 2025 | [V] |
| Cuota puzzle | 10 % instalaciones, 13 % sesiones | Puzzle | Adjust 2026 | [V] |
| D90/D1 relativo; juego diario | ~25 %; ~38 min | Top puzzle iOS EE. UU. | Naavik | [V] |
| D7 top-25 casual | en descenso desde inicios de 2022; Tasty Travels ~22 % | Casual | Sensor Tower State of Gaming 2026 vía GDR | [V] |

Lectura: la industria entera baja (GameAnalytics reporta descensos año tras año en D1 y D7), pero puzzle se mantiene arriba. El verdadero diferencial de los líderes no está en D1 sino en D7-D30, que es donde actúan meta y live-ops.

## 2. Qué tienen realmente los water sort líderes

| Juego (dev) | Niveles anunciados | Meta / live-ops visibles | Ayudas | Monetización | Rating |
|---|---|---|---|---|---|
| Water Sort Puzzle (IEC Global) | "cientos" en ficha; reseñas hablan de nivel 400+ | Eventos estacionales ("Sort Under the Full Moon"), contenido festivo, niveles "?" con colores ocultos | 5 undos gratis y luego anuncio; +1 recipiente por anuncio (ahora varios anuncios por recipiente según quejas) | Sin anuncios 2,99 USD; Premium 4,99; packs de diamantes 1,99-99,99; Revival 2,99 | 4,7 (324 K, iOS) [V] |
| Water Sort Puzzle - Sort Color (Guru) | no publicado | Game Center: clasificaciones y logros; actualización mensual | Undo y recipiente extra vía anuncio incluso tras pagar | Quitar anuncios 7,99 USD (solo intersticiales) | 4,8 (130 K, iOS) [V] |
| Magic Sort! (Grand Games) | no publicado; "+100 niveles" por versión | Equipos, cartas coleccionables/álbum, retos diarios, eventos, clasificaciones, reinos temáticos, caldero de boosts | Undo, Shuffle, oferta "+1 recipiente" al fallar; primer anuncio en nivel 22 | IAP 0,99-19,99 USD; 50 M+ descargas; ~1,4 M USD/mes solo EE. UU. (dic-2025) | 4,7 (669 K iOS); reseñas escritas 2,8 [V] |
| Water Sort Club (Gamovation) | 10 000+ | Reto diario, temas y fondos desbloqueables | sin detalle | Anuncios + IAP | — [V ficha] |
| Water Sort! Color Match (Infinite Joy) | 5 000+ | Diseños de botella y temas de fondo por recompensa | Undo, pista, botella extra | Anuncios + IAP | 4,2 (muestra pequeña) [V] |
| Water Sort! Color Sort Puzzle (offline) | no verificado | Reto diario con puzzle de recompensa oculta, eventos "jigsaw" (coleccionar imágenes), pase de temporada mensual | — | — | [E, resumen de ficha] |
| SortPuz 3D / Water Sort Puz | 1 500+ / 5 000+ | 9+ fondos y skins, tareas diarias, torneos | — | — | [V fichas] |

Conclusiones: (1) el estándar del género ya incluye reto diario, temas/fondos y skins de recipiente, es decir, nuestro diferencial visual (fondos-país, recipientes autóctonos) compite en un terreno que los jugadores esperan, no es un extra; (2) nadie de la cola larga tiene álbum, equipos ni clasificaciones vivas: ahí está el hueco que Magic Sort está cerrando con IAP; (3) los "colores ocultos" existen en los líderes y son un foco de quejas sistemático.

## 3. Meta y live-ops: adopción en casual y aplicabilidad

| Mecánica | Presencia en water sort líderes | Adopción / impacto medido | Aplicable a Amazon Fill |
|---|---|---|---|
| Reto diario | Sí (Water Sort Club, SortPuz, Magic Sort) | Base de la retención diaria; Naavik destaca el "daily jackpot" (Lava Quest) como conversor de nuevos en habituales | v1.0, "Reto del río" |
| Calendario de recompensas | Parcial | "Casi todos los top" lo tienen; login calendars +93 % interanual en midcore (AppMagic) | v1.0 |
| Racha (streak) de días | Poco visible | Duolingo: +14 % D7 con apuesta de racha, amuleto reduce 5 % pérdida de racha; DAU baja 5-10 % fines de semana | v1.0 con perdón |
| Win streak (victorias seguidas) | No en los clásicos | 48 % de títulos, +50 % en 2025; Liftoff la llama ya "estándar" | v1.2 |
| Colecciones / álbum | Solo Magic Sort | Colecciones 75 % y álbumes 66 % de casual; álbum "infinito" estacional es la tendencia (Naavik) | v1.1 álbum de frutas; v2.0 infinito |
| Skins de recipiente y fondos | Sí, en casi todos | Sin dato de impacto; es higiene del género | v1.0 fondos, v1.1 skins |
| Clasificaciones / carreras | Guru (Game Center), Magic Sort | Races 68 % casual; eventos competitivos +24 %; tendencia a "competición + logro personal" para que todos ganen algo | v1.2 liga semanal |
| Pase de temporada | Una ficha lo menciona | Sensor Tower lo clasifica como "feature event" de progresión | v1.2 pase mensual barato |
| Logros | Guru | Costo casi cero con Play Games Services | v1.1 |
| Equipos / cooperativo | Magic Sort | Royal Match capa win streak → co-op → álbum (Naavik) | v2.0 |
| Niveles bonus / modos | "?" ocultos (IEC) | Generan quejas si son obligatorios | v1.1 como opcional con premio |
| Eventos temporales | Estacionales (IEC) | Juegos top: 73→89 eventos/mes; casual añade ~0,7 mecánicas nuevas/mes; picos en Halloween, Navidad, San Valentín | v1.2 |
| Mascotas | No | Magic Sort usa gatos como personaje, no mascota funcional | v2.0 opcional (fauna amazónica) |

Dato de contexto [V]: alrededor del 70 % de los eventos live-ops están orientados a pagadores y casi todos son accesibles también a no pagadores (AppMagic). Para un juego con 70 % de ingresos por IAP esto es la regla; para uno con peso de anuncios, los eventos deben repartir también vistas de rewarded.

## 4. Onboarding y primera sesión en el género

- Primer anuncio: Magic Sort espera al nivel 22 [V]; los water sort ad-driven muestran intersticiales desde los primeros niveles y reciben las peores reseñas por ello [V]. Guías de industria recomiendan no introducir anuncios no solicitados hasta que el jugador esté "enganchado" (Supersonic) y, en juegos IAP-heavy, retrasarlos aún más (CAS.AI) [V].
- Primera oferta: la monetización por fallo ("te falta un movimiento: +1 recipiente") es la que usa Magic Sort, análoga a Candy Crush [V]. Para nosotros debe ser con moneda blanda o rewarded la primera vez, con precio real solo después.
- Boosters: aparecen uno a uno; en casual puzzle los de dentro del nivel hacia el nivel 7 y los pre-nivel hacia el 9 (Liquid & Grit; el informe completo está tras paywall, cifra tomada del resumen) [E]. El bucle central se enseña en unos 6 niveles [E, misma fuente].
- Tutorial: debe ser jugado, no leído; Supersonic cita un caso con 60 % de abandono en tutorial por introducir mecánicas demasiado rápido y confusión al dejar 1 min de juego libre sin objetivo [V].
- Permiso de push: pedirlo con pantalla previa de valor (OneSignal, Pushwoosh); nunca en el arranque frío.

## 5. Push y re-engagement

| Dato | Valor | Fuente |
|---|---|---|
| Opt-in hypercasual games | iOS 23,0 % / Android 69,0 % | Pushwoosh 2025 [V] |
| Opt-in rango todas las industrias | Android 55-91 %, iOS 23-80 % | Pushwoosh 2025 [V] |
| CTR hypercasual | iOS 0,82 % / Android 1,05 % | Pushwoosh 2025 [V] |
| CTR action games | iOS 0,46 % / Android 0,82 % | Pushwoosh 2025 [V] |
| Personalización básica | +9 % CTR | Pushwoosh [V] |
| Frecuencia recomendada | igual a la frecuencia de apertura del jugador | OneSignal [V] |

Qué funciona sin molestar [V/E]: (1) pantalla previa que explique el beneficio concreto ("te avisamos cuando tu reto diario esté listo") y disparar el diálogo nativo justo tras un momento de éxito (fin del nivel 10 o primer reto diario); (2) notificaciones con estado real del jugador: reto diario disponible, racha en riesgo a las 20:00 locales, recompensa del calendario sin reclamar, evento que termina en 24 h, nuevos niveles publicados; (3) una por día como máximo en D1-D7 y bajar a 2-3 por semana si no abre; (4) nada genérico tipo "te extrañamos". Android (donde lanzamos primero) permite construir el hábito; en iOS hay una sola oportunidad de pedir permiso.

## 6. Análisis de reseñas (síntesis, sin citas)

Fuentes: agregados de reseñas negativas y positivas del Water Sort Puzzle de IEC (AppSupports), del Water Sort Puzzle Sorting Game (JustUseApp, 13,5 K reseñas, 4,7), reseñas visibles del Guru Water Sort y resumen de sentimiento de Magic Sort (mwm.ai) [V].

Top 10 quejas (orden aproximado de frecuencia):
1. Anuncio tras cada nivel; el jugador siente que pasa más tiempo en anuncios que jugando (tema dominante, ~85+ menciones en una sola muestra).
2. Anuncios largos (30-60 s) y con cierres engañosos.
3. Siguen viendo anuncios tras pagar "sin anuncios" porque la compra solo cubre intersticiales y no las ayudas (IEC, Guru).
4. Cambios a peor: pasar de 1 anuncio por recipiente extra a 3-4 anuncios por recipiente.
5. Crashes, cuelgues, lag y calentamiento del teléfono en niveles altos (IEC, Infinite Joy).
6. Niveles "?" con colores ocultos: se percibe como azar, no lógica.
7. Niveles "imposibles" o diseñados para forzar la compra del recipiente extra (también en Magic Sort a partir del nivel ~50).
8. Repetición y falta de novedad después del nivel ~200.
9. Límite de 5 undos y botón de anuncio que no entrega la recompensa.
10. Soporte que no responde y anuncios de contenido inapropiado.

Top 5 elogios: relajante/meditativo, incluso para la ansiedad; sentir que ejercita la lógica y la memoria; adictivo "un nivel más"; sin cronómetro ni castigo, jugable offline; apto para toda la familia y satisfacción audiovisual del vertido (ASMR). Observación: la brecha entre rating de tienda (4,7) y reseñas escritas (2,8 en Magic Sort) indica que la fricción monetaria llega tarde, cuando el jugador ya está enganchado; copiar el enganche, no la fricción.

## 7. Niveles y cadencia de contenido

- Volumen anunciado: 1 500+ (SortPuz 3D), 5 000+ (Infinite Joy, Water Sort Puz), 10 000+ (Water Sort Club); IEC solo dice "cientos" pero sus jugadores superan el nivel 400 [V].
- Método: niveles iniciales a mano, luego generador procedural con solver que descarta niveles no resolubles (una implementación pública usa 20 000 movimientos como tope de búsqueda) [V, nota de desarrollador indie].
- Cadencia: Magic Sort publica versiones muy seguidas con "+100 niveles" cada una (la ficha mostraba actualización de hace 21 h) [V]; Guru publica mensualmente [V]; el ritmo general de live-ops en top games subió de 73 a 89 eventos/mes en 2025 [V].
- La queja "se repite después del 200" muestra que el generador por sí solo no basta: hace falta variar reglas (más recipientes, capacidad distinta, recipientes tapados, un color bloqueado) cada 20-30 niveles [E].

## Implicaciones para Amazon Fill

1. **Capa de retención antes que capa visual.** Fondos-país y recipientes autóctonos son esperados por el género; lo que mueve D7-D30 son reto diario, calendario, racha y colección. La animación de sumergir y teñir debe durar ≤ 2 s y ser saltable desde la segunda carga, porque el jugador juega micro-sesiones de 60-120 s [E].
2. **No copiar el modelo de 5 undos + anuncio.** Es la queja central del género. Propuesta: undo ilimitado hasta el nivel 20; después 3 undos gratis por nivel y el resto por moneda blanda (semillas) o rewarded. El recipiente extra ("tinaja de reserva") siempre por rewarded la primera vez al día, luego 1 por moneda, sin pedir 3-4 anuncios.
3. **Intersticiales tarde y predecibles.** Ninguno antes del nivel 12 ni en la primera sesión; después, máximo 1 cada 3 niveles o cada 3-5 min, siempre en pantalla de victoria, nunca al reiniciar. Medir ARPDAU vs D7 por cohorte en ventanas de 7-14 días (CAS.AI) y parar donde D7 caiga.
4. **"Sin anuncios" honesto.** La compra debe cubrir intersticiales y banners y dar una asignación diaria de ayudas (p. ej., 3 tinajas/día) para no repetir la queja n.º 3. Precio ancla Android LatAm 2,99-3,99 USD; en Tier-1 4,99 [E a partir de precios observados 2,99-7,99].
5. **Colores ocultos solo opcionales.** El modo "aguas turbias" (colores ocultos) se ofrece como reto bonus con recompensa doble, nunca en la ruta principal.
6. **Accesibilidad de color.** Una reseña recurrente es daltonismo: cada fruta amazónica lleva un icono o patrón además del color (açaí, camu camu, aguaje, copoazú, cocona, arazá, tucumã, maracuyá, guaraná, pitanga: 10 tintes con silueta propia).
7. **Economía 30/70 como objetivo, no de salida.** Arrancamos ad-driven con rewarded como formato primario (CAS.AI, guías); IAP crece con álbum, skins y pase. Piggy bank y oferta de fallo ("+1 tinaja por 0,99") entran después de medir dónde falla la gente.
8. **Generador + solver desde el día 1**, con 150-200 niveles curados y cambio de regla cada 20-30 niveles. Objetivo de lanzamiento: 1 000 niveles jugables, meta 5 000 anunciables a los 6 meses [E].
9. **Cadencia de contenido fija**: +100 niveles y 1 evento temático cada 2 semanas, con picos en fechas locales (Carnaval, Inti Raymi, Día de la Amazonía 5-sep, Navidad) además de Halloween y San Valentín.
10. **Push con estado real** y pantalla previa; en Android el 69 % acepta, en iOS planificar para ~25 %.

## Roadmap de meta

| Versión | Qué incluye | Justificación (impacto esperado) |
|---|---|---|
| **1.0 MVP** | Core loop con generador+solver (1 000 niveles, 200 curados); undo (ilimitado <20, luego 3 + semillas); tinaja extra por rewarded; reiniciar gratis; **Reto del río** diario con calendario mensual de 30 casillas; **racha** de días con 1 "hoja de protección" semanal (perdón); moneda blanda (semillas); 3 fondos-país por progreso (Brasil nivel 1, Perú 50, Colombia 120); tienda mínima: Sin anuncios + 2 packs de semillas; push con pre-prompt (reto listo, racha en riesgo); Play Games Services logros básicos; analítica de embudo por nivel | Reto diario y calendario son la base de D1-D7 en todo el género; racha con perdón replica el único dato duro de lift (+14 % D7 en Duolingo, [E] al trasladar a puzzle); 3 fondos dan sensación de viaje sin coste de contenido |
| **1.1** (+4-6 semanas) | **Álbum de frutas amazónicas**: cada color se "descubre" al completar niveles y llena una página por país (colección finita, 10 frutas × 3 páginas); **skins de recipiente** (cuia, tinaja, mocahua, totuma) por semillas y por hitos; modo bonus "aguas turbias" opcional; 2 fondos más (Ecuador, Bolivia); logros completos | Colecciones están en el 75 % de los casual (AppMagic) y son la capa de D14-D30; skins son el primer IAP cosmético sin tocar dificultad |
| **1.2** (+8-10 semanas) | **Eventos de tiempo limitado** (crecida del río, 3-4 días, con mecánica de nivel alterada); **win streak** de victorias seguidas con multiplicador de semillas; **liga semanal** por puntos con recompensas por tramo personal + ranking (modelo competición+logro); **Pase Expedición** mensual de bajo precio (2,99-4,99) con pista gratuita y premium; oferta de fallo "+1 tinaja" con semillas y luego precio; segmentación de push por evento | Win streak +50 % de adopción en 2025 y D7; competitivos +24 %; pase y oferta de fallo son las dos palancas IAP probadas en Magic Sort; eventos cada 2 semanas sostienen D30 |
| **2.0** | **Equipos/tribus** (cooperativo con meta compartida y chat mínimo); **álbum infinito por temporadas** (frutas raras, fauna); **jackpot diario** tipo Lava Quest; nuevas mecánicas (afluente bloqueado, tinaja tapada, capacidad variable); mascota de fauna amazónica opcional; cosméticos de temporada; ofertas personalizadas por comportamiento | Es la capa larga de Royal Match/Magic Sort (streak → co-op → álbum); el álbum infinito convierte un hito temporal en motor permanente (Naavik); solo tiene sentido con DAU suficiente para equipos |

## Diseño de la primera sesión (objetivo 8-10 min, niveles 1-10)

| Min | Nivel | Qué pasa | Enseña / mide |
|---|---|---|---|
| 0:00-0:20 | — | Splash de 1 pantalla con el río; sin login, sin permiso de push; carga del nivel 1 con animación de sumergir y teñir (1,5 s) | Tiempo a primera interacción < 20 s |
| 0:20-1:00 | 1 | 3 recipientes, 2 colores (açaí, aguaje), 2 movimientos; dedo-guía en el primer toque; vertido con sonido de agua | Tocar origen → destino |
| 1:00-1:45 | 2 | 4 recipientes, 3 colores, 3-4 movimientos; sin guía; confeti y +10 semillas al terminar (primer "ajá") | Regla de capacidad |
| 1:45-2:40 | 3 | 4 recipientes, 3 colores, 5-6 movimientos | Planificar 2 pasos |
| 2:40-3:40 | 4 | 5 recipientes, 4 colores; aparece el botón **Deshacer** con tooltip la primera vez que un vertido deja un recipiente peor | Undo ilimitado por ahora |
| 3:40-4:40 | 5 | 5 recipientes, 4 colores, 1 recipiente vacío; primera fruta "descubierta" (teaser del álbum, 1 pantalla) | Anticipa v1.1 sin implementarlo |
| 4:40-5:40 | 6 | 6 recipientes, 5 colores; tras ganar, presentación del **Reto del río** (bloqueado hasta nivel 10) | Enseña que hay un porqué para volver |
| 5:40-6:45 | 7 | 6 recipientes, 5 colores, 2 vacíos; primer nivel con reinicio probable | Botón Reiniciar sin castigo |
| 6:45-7:50 | 8 | 7 recipientes, 6 colores; si el jugador se bloquea, aparece la **tinaja extra** gratis (sin anuncio) una vez | Introduce la ayuda, no el precio |
| 7:50-8:50 | 9 | 7 recipientes, 6 colores, 1 vacío; cambio de fondo a otro tramo del río (misma Brasil) | Progreso visual |
| 8:50-10:00 | 10 | 8 recipientes, 7 colores; pantalla de hito: calendario día 1 reclamado + "¿quieres que te avise cuando el reto esté listo?" (pre-prompt push) + cofre de 50 semillas | Permiso push tras éxito; fin de sesión con razón para volver mañana |

Reglas de la primera sesión: 0 intersticiales; 0 ofertas de pago; el primer rewarded voluntario solo desde el nivel 11 (duplicar semillas); el primer intersticial no antes del nivel 12 y nunca en la misma sesión que la instalación; la oferta "Sin anuncios" aparece tras el tercer intersticial visto o en D2, lo que llegue antes. Medir por nivel: tasa de abandono, reinicios, undos, tiempo; umbral de alarma si algún nivel 1-10 pierde > 5 % de los jugadores.

## Preguntas abiertas

- Prioridad real de ingresos: ¿arrancamos ad-driven (más fácil de escalar en LatAm Android) o IAP-first como Magic Sort? Define si el pase y la oferta de fallo entran en 1.2 o se adelantan.
- No pude verificar el número exacto de niveles ni las descargas de Google Play del Water Sort de IEC (fichas truncadas, AppBrain con 403); los conteos de la cola larga (5 000-10 000) son autodeclarados.
- Las cifras de "boosters en nivel 7/9" y "6 niveles para el bucle" provienen del resumen público de un informe de pago (Liquid & Grit); habría que confirmarlas o medirlas en nuestro propio funnel.
- No existe (que haya encontrado) un dato público de lift de retención por calendario diario en puzzle; los únicos datos duros son de Duolingo. Los porcentajes tipo "+15-30 % D7" que circulan en blogs no citan fuente y los descarto.
- Sesión media puzzle de Adjust (~24 min) y mediana de GameAnalytics (5-6 min) no son comparables (media vs mediana, muestras distintas); hay que decidir cuál usar como KPI interno.
- Costes/licencias de música y sonido ASMR del agua, y si se usarán nombres reales de frutas y países (sin marcas registradas).
- Requisitos de Play Games Services y Game Center para logros/clasificaciones en un juego offline-first: ¿sincronización en nube desde 1.0?
- Equipos (2.0) requieren backend y moderación de chat; decidir si se externaliza.

## Fuentes

- https://gamedevreports.substack.com/p/gameanalytics-mobile-gaming-benchmarks (GameAnalytics 2025 benchmarks, resumen)
- https://gamegrowthadvisor.com/blog/2026-03-17-mobile-game-kpis-benchmarks-2026/ (compilación con fuentes GameAnalytics 2026, AppsFlyer, Liftoff)
- https://segwise.ai/blog/mobile-gaming-app-user-retention-strategies (retención por género, datos Mistplay)
- https://gamedevreports.substack.com/p/adjust-mobile-games-insights-report (Adjust 2025)
- https://gamedevreports.substack.com/p/adjust-gaming-app-insights-report (Adjust 2026)
- https://www.adjust.com/blog/puzzle-games-trends-strategies/ (Adjust, sesión puzzle)
- https://gamedevreports.substack.com/p/sensor-tower-state-of-gaming-2026 (Sensor Tower State of Gaming 2026, resumen)
- https://naavik.co/digest/what-leading-match-3-and-merge-games-do-differently/ (Naavik con datos Sensor Tower: puzzle > 10 B USD, Sort 13 % del crecimiento, ARPDAU)
- https://naavik.co/digest/live-ops-trends-powering-mobile-puzzle/ (Naavik, convergencia live-ops en puzzle)
- https://gamedevreports.substack.com/p/appmagic-mobile-live-ops-in-2025 (AppMagic Live Ops 2025)
- https://liftoff.ai/2025-casual-gaming-apps-report/ (Liftoff & Singular 2025)
- https://gamedevreports.substack.com/p/liftoff-and-singular-casual-games (resumen Liftoff 2025)
- https://sensortower.com/blog/2025-q4-unified-top-5-puzzle-games-revenue-us-64bc1644e1714cfff1e615fc (Sensor Tower Q4 2025, Magic Sort)
- https://sensortower.com/blog/top-grossing-mobile-games-live-ops-strategies-2025-report (Sensor Tower, capas de live-ops)
- https://www.deconstructoroffun.com/blog/2026/2/6/sort-puzzles-how-a-new-subgenre-is-born (Deconstructor of Fun, subgénero sort)
- https://www.gamigion.com/magic/ (Gamigion, análisis Magic Sort: primer anuncio nivel 22, oferta de fallo)
- https://www.gamigion.com/magic-sort-by-grand-games-continues-to-scale-to-120k-per-day/ (Gamigion, escalado Magic Sort)
- https://apps.apple.com/us/app/magic-sort/id6499209744 (ficha Magic Sort: features, IAP, "+100 niveles")
- https://mwm.ai/apps/magic-sort/6499209744 (Magic Sort: descargas, brecha rating/reseñas)
- https://apps.apple.com/us/app/water-sort-puzzle/id1514542157 (ficha Water Sort Puzzle, IEC Global)
- https://appsupports.co/1514542157/water-sort-puzzle/negative-reviews (agregado de reseñas negativas, IEC)
- https://justuseapp.com/en/app/1593801388/water-sort-puzzle-sorting-game/reviews (agregado de reseñas)
- https://apps.apple.com/us/app/water-sort-puzzle-sort-color/id1597897013 (ficha Water Sort Puzzle - Sort Color, Guru)
- https://cafebazaar.ir/app/com.gamovation.watersort?l=en (ficha Water Sort Club: 10 000+ niveles, temas)
- https://cafebazaar.ir/app/game.water.sort.puzzle.android?l=en (ficha Water Sort! Color Match Puzzle, Infinite Joy)
- https://play.google.com/store/apps/details?id=water.sort.color.puzzle.offline.games&hl=en_IN (ficha Water Sort! Color Sort Puzzle; solo resumen de búsqueda)
- https://apps.apple.com/app/id1580876589 (SortPuz 3D: niveles, skins, torneos)
- https://www.pushwoosh.com/blog/push-notification-benchmarks/ (Pushwoosh 2025, CTR por industria)
- https://www.pushwoosh.com/blog/increase-push-notifications-opt-in/ (Pushwoosh 2025, opt-in por plataforma e industria)
- https://onesignal.com/blog/push-notifications-messaging-for-game-developers/ (OneSignal, frecuencia y pre-prompt)
- https://blog.duolingo.com/how-streaks-keep-duolingo-learners-committed-to-their-language-goals/ (Duolingo, A/B de rachas)
- https://business.mistplay.com/resources/daily-login-rewards (Mistplay, diseño de calendarios y perdón de racha)
- https://supersonic.com/learn/blog/optimizing-ftue/ (Supersonic, FTUE)
- https://www.liquidandgrit.com/designing-streamlined-ftues-to-maximize-early-retention/ (Liquid & Grit, FTUE; informe de pago)
- https://cas.ai/?p=9604 (CAS.AI, monetización híbrida: mezcla anuncios/IAP, frecuencia)
- https://www.nexd.com/blog/what-are-mobile-interstitial-ads-and-how-do-they-work/ (frecuencia de intersticiales; resumen de búsqueda)
- https://kalaybeytullah.itch.io/water-sort (nota de desarrollador: generador + solver)
