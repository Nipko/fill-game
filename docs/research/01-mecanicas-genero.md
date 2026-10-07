# Mecánicas del género water-sort / liquid-sort: reglas, variantes y especificación para Amazon Fill

Fecha de investigación: 2026-10-05. Convención: **[V]** = dato verificado con URL en la sección Fuentes; **[E]** = estimación o inferencia del investigador; **[NV]** = no se pudo verificar.

## Resumen ejecutivo

- El núcleo del género es idéntico en todos los líderes: recipientes de **4 segmentos**, se vierte **la racha superior de un mismo color** (tantas unidades como quepan) y solo sobre un recipiente **vacío o con el mismo color arriba**. Victoria cuando cada recipiente está vacío o lleno de un solo color. [V]
- Configuración estándar: **N colores + 2 recipientes vacíos** (5/7, 7/9, 10/12, 16/18 en los niveles de dificultad de un clon web documentado). Con solo +1 vacío la dificultad sube mucho. La teoría demuestra que **con 2 vacíos existen instancias irresolubles**, así que cada nivel debe validarse con un solver. [V]
- Ball Sort y Water Sort son **equivalentes en resolubilidad** (Ito et al., Theoretical Computer Science 2023): la diferencia es cuántas unidades se mueven por toque (1 bola vs toda la racha). Ambos son NP-completos incluso con 2 colores. [V]
- Control dominante: **tap-tap** (toca origen, toca destino). El recipiente seleccionado se eleva; al verter, se inclina hacia el destino y el líquido no rota con el vaso (shader). Hexa Sort usa drag. [V] Duraciones de animación no están publicadas; estimación 0,3-0,6 s por vertido. [E]
- Comodines estándar del género: **Deshacer** (IEC regala 5 por nivel, luego anuncio), **+1 recipiente vacío** (por anuncio o monedas; IEC lo ha reducido a 1/4 de tubo por anuncio), **Pista**, **Reiniciar gratis**, y en Magic Sort **Shuffle**. [V]
- Variantes que usan los líderes: **colores ocultos "?"** (solo se ve la capa superior, se revela al quedar expuesta), **recipientes de capacidad distinta / alargados**, **formas de botella nuevas y reglas de vertido modificadas** (Magic Sort), niveles marcados **"hard" y "super hard"**, **retos diarios**, **modo infinito**, eventos tipo jigsaw. [V]
- **Movimientos limitados y tiempo limitado existen** (Color Water Sort Challenge Edition en Switch: modos Classic/Timed/Hard) pero los líderes móviles los evitan explícitamente ("sin límite de tiempo ni penalizaciones"). [V]
- Deadlock: la buena práctica es mostrar **"No hay más movimientos"** solo cuando no queda ningún movimiento legal, no anunciar "sin solución" tras cada jugada, y que la pista automática se mantenga en silencio en posiciones perdidas. Al fallar se ofrece "+1 botella" (Magic Sort) o reinicio sin penalización. [V]
- Dificultad bien diseñada: no viene del tamaño del tablero sino de **lo apretado del espacio libre**. Un proyecto abierto mide la dificultad con un **jugador simulado greedy que juega 40 veces**; tasa de fallo ≥75 % = Hard, ≥90 % = Master. [V]
- Mercado: el subgénero Sort alcanzó **~1.000 M de descargas y ~280 M USD de IAP en 2025 (+176 % YoY)**; Magic Sort ~50 M USD y Hexa Sort ~20 M USD en 2025. Antes de 2023 el subgénero facturaba <2 M USD/año. [V]
- Benchmarks de Magic Sort: CPI Android ~0,50 USD (Q1 2025), D30 ~8 % (EE. UU.), 37,6 M instalaciones Android, 4,7★ con 669 K valoraciones en iOS, actualizaciones semanales con +100 niveles, anuncios a partir del nivel 22. [V]
- El subgénero "llenar el vaso" (Happy Glass, Draw To Fill, Fill The Cups) es **físico, con niveles hechos a mano y monetización casi solo por anuncios**; no es lo que buscamos como núcleo. Water sort permite generar miles de niveles por algoritmo y tiene economía IAP probada. [V]/[E]
- Recomendación central: núcleo water-sort ortodoxo (4 segmentos, +2 vacíos, tap-tap, deshacer, +1 recipiente, pista), con la **inmersión y teñido "en vivo" como animación de carga** y los comodines re-tematizados (afluente extra, remolino, etc.), sin tocar las reglas probadas.

## 1. Referentes del género y cifras

| Juego | Editor | Plataforma / datos | Modelo y precios |
|---|---|---|---|
| Water Sort Puzzle | IEC Global Pty Ltd (Australia) | iOS 4,7★ / 324 K valoraciones; Android ~132 M instalaciones, 4,6★ / 1,34 M reseñas (MixRank) [V] | Ads + IAP: No Ads 2,99 $; Premium 4,99 $; diamantes 1,99-99,99 $; Revival Pack 2,99 $. 5 deshacer gratis por nivel. [V] |
| Water Sort Puzzle - Sort Color | Guru Smart Holding / Guru Puzzle Game (Singapur) | iOS 4,8★ / 130 K; Android 59,4 M instalaciones, 4,84★ / 606 K reseñas; lanzado 2021-12 [V] | Remove Ads 7,99 $; monedas solo para cosméticos; herramientas de rescate (deshacer, +1 tubo) exigen ver anuncios de 1:20-3 min según reseñas. [V] |
| Magic Sort! | Grand Games (Noruega) | iOS 4,7★ / 669 K; Android 37,6 M instalaciones, 4,5★ / 541 K; lanzado 2024-06-26; ~50 M USD en 2025 [V] | IAP-first: monedas 0,99-7,99 $; bundles 4,99-19,99 $; suscripción "Endless Portal" 5,99 $. Estimación 69 % IAP / 31 % ads. +100 niveles semanales. [V] |
| Hexa Sort | Lion Studios Plus (dev. gamebra.in + Mindstorm) | iOS 4,7★ / 219 K; ~20 M USD en 2025; pico 4 M USD/mes IAP en 2024, ~1,5 M/mes sostenido [V] | Coin packs 1,99 / 7,99 / 14,99 $; Piggy Bank 4,99 $; Safety Net 4,99 $; bundles 9,99 $; Spin Wheel 1,99 $. Meta de construcción de pueblo. [V] |
| Ball Sort Color Water Puzzle | IEC Global | iOS 4,7★ / 69 K; Android 4,6★ / 421.857 valoraciones [V] | No Ads 2,99 $; membresía Silver semanal 2,99 $, mensual 9,99 $, 6 meses 49,99 $. [V] |
| Get Color - Water Sort Puzzle | Tripledot Studios | Android 50 M+ descargas [V] | Ads + IAP [NV detalle] |
| Water Sort Puzzle: Color Sort | HDuo Fun Games (HK) | Android 12,6 M instalaciones, 4,40★ / 86 K [V] | 100 % anuncios; niveles especiales con tubos alargados; retos diarios, eventos jigsaw, historias. [V] |
| Color Water Sort: Challenge Edition | Nintendo Switch, 2025-01-01 | 5.200 niveles; modos Classic / Timed / Hard (movimientos limitados, 200 niveles) [V] | 2,99 $ + 3 DLC; 9 diseños de tubo, 5 tapas, 4 fondos; 25.000 monedas iniciales. [V] |

Contexto de mercado [V]: el puzzle móvil generó 9.700 M de descargas y 10.000 M USD de IAP en 2025 (+14 %). El subgénero Sort: ~1.000 M de descargas (segundo tras Match Pair) y ~280 M USD de IAP, +176,4 % YoY (Naavik, feb. 2026). Antes de 2023 el conjunto de sort puzzles facturaba <2 M USD/año (Deconstructor of Fun). El mercado es una "clase media democrática": una docena de títulos entre 5 y 15 M USD/año.

## 2. Reglas del núcleo (lo que hacen todos los líderes)

| Regla | Estándar observado | Fuente / estado |
|---|---|---|
| Capacidad del recipiente | 4 segmentos. Niveles duros usan 5 o más. Ball sort: 4 bolas por tubo. | lofiandgames, ChromaOracle, Ball Sort - Bubble Sort Puzzle [V] |
| Condición de vertido | Destino vacío **o** su capa superior del mismo color **y** con espacio libre. | IEC, Guru, Magic Sort (fichas de tienda) [V] |
| Cantidad vertida | Toda la racha consecutiva superior del mismo color, hasta llenar el destino; si no cabe todo, se vierte lo que cabe y el resto queda. Un vertido = 1 movimiento. | dev.to solver, ChromaOracle [V] |
| Ball sort | Mueve **una** bola por toque (algunos clones mueven la racha). Resolubilidad idéntica a water sort. | IEC Ball Sort, Ito et al. [V] |
| Recipiente vacío | Acepta cualquier color. | fichas en portugués/español [V] |
| Colores vs recipientes | n recipientes llenos de n colores + k vacíos; k = 2 es el estándar; k = 1 en variantes duras (proyecto Glittery: n+1 colores, n+2 botellas). | lofiandgames, Univ. Tartu [V] |
| Victoria | Todos los recipientes vacíos o monocromáticos y llenos. Los vacíos sobrantes simplemente quedan vacíos. | lofiandgames, ChromaOracle [V] |
| Recipiente completo | Se considera "sellado": no se puede verter desde él (los solvers lo podan; los juegos le ponen tapa). | dev.to, cosmético "caps" en varias fichas [V]/[E] |
| Movimientos inútiles | Verter un tubo monocolor a uno vacío solo recoloca el problema; los solvers lo descartan. El juego puede permitirlo, pero la pista nunca debe sugerirlo. | dev.to [V] |

Resultados teóricos relevantes para programar [V] (Ito, Kawahara, Minato et al., "Sorting Balls and Water", TCS 978, 2023; arXiv 2202.09495):
- Water sort y ball sort son NP-completos incluso con 2 colores; hallar la solución más corta es NP-completo incluso en instancias triviales.
- Equivalencia: una instancia es resoluble con movimientos de bola si y solo si lo es con movimientos de agua.
- Vacíos necesarios para que **toda** instancia sea resoluble: k(n,h) ≤ ⌈(h−1)/h · n⌉ y k(n,h) ≥ ⌈19/64 · min{n,h}⌉. Existen instancias irresolubles con k=2, h=3, n=9 y con k=3, h=4, n=12. Conclusión práctica: **k=2 no garantiza solución; hay que verificar cada nivel con un solver** o generarlo por mezcla inversa desde el estado resuelto.
- Con h=2 y tantos colores como tubos el problema es resoluble en O(n): útil para un tutorial de capacidad 2. [V]

## 3. Controles, animación y feedback

- **Selección**: tap sobre un recipiente lo eleva (ball sort: "la bola sube por encima del tubo"); segundo tap sobre el destino ejecuta el vertido; tap de nuevo sobre el origen lo deselecciona. Todos los líderes water-sort usan tap-tap; Hexa Sort usa drag de pilas. [V]
- **Vertido**: el origen se desplaza sobre el destino y se inclina; solo rota el vaso, no el líquido (shader con variable de inclinación); un line renderer dibuja el chorro; la capa superior del origen se escala hacia abajo mientras la del destino crece. [V] (Universidad de Tartu, proyecto Glittery Water Puzzle; snippets de desarrollo).
- **Duración**: ninguna fuente publica tiempos. Estimación razonable para que no frene al jugador: elevación 0,15-0,25 s, traslado+inclinación 0,25-0,35 s, caudal 0,3-0,5 s por 1-2 segmentos, retorno 0,25 s. Debe poder encolarse el siguiente tap durante la animación. [E]
- **Auto-tapado**: cuando un recipiente queda lleno de un solo color aparece una tapa y deja de ser seleccionable; las tapas son además objeto cosmético vendible (Switch: 5 tapas; Google Play: "temas, tapas y fondos"). [V]
- **Celebración**: confeti/"efectos satisfactorios" y sonido de agua son estándar de fichas de tienda; Hexa Sort vende "ASMR". [V]
- **Haptics**: existen y son configurables (un juego publicó un fix para que la vibración se apague de verdad; Sort Factory destaca "vibraciones hápticas sutiles"). [V]
- **Deshacer**: IEC da 5 por nivel y luego pide anuncio; Guru lo trata como "herramienta de rescate" tras anuncio; Magic Sort lo vende con monedas junto a Shuffle. [V]
- **Contador de movimientos**: no es estándar en los líderes móviles (Magic Sort y Guru presumen de "sin límite de movimientos"); el clon web lofiandgames no lo muestra. El solver abierto fija un "par" (solución más corta) por nivel, útil para métricas internas. [V]
- **Pista**: tras anuncio en IEC/Guru. Buena práctica abierta: la pista ilumina un recipiente solo si existe solución desde el estado actual; pista "inactiva" (idle nudge) tras unos segundos sin jugar. [V]
- **Accesibilidad**: la versión Apple Arcade incluye modo daltónico. [V] Con 12-16 colores esto será obligatorio para Amazon Fill (frutas con patrón/icono además de color). [E]

## 4. Variantes de nivel usadas por los líderes

| Variante | Cómo funciona | Quién la usa | Estado |
|---|---|---|---|
| Colores ocultos "?" | Solo la capa superior de cada tubo es visible; las inferiores aparecen grises con "?" y se revelan al quedar expuestas tras un vertido. Una capa oculta no forma parte de la racha visible aunque resulte del mismo color. | IEC (quejas de usuarios), Magic Sort ("elementos ocultos", niveles hard/super hard), lofiandgames, ChromaOracle | [V] |
| Capacidades distintas | Tubos de 5+ unidades en niveles duros; "tubos alargados" en niveles especiales; Hoop Sort mezcla postes altos y bajos. | ChromaOracle, HDuo, Magic Sort ("nuevas formas de botella") | [V] |
| Reglas de vertido modificadas | Magic Sort las anuncia como mecánica avanzada; el detalle no está publicado. | Magic Sort | [V] existe / [NV] detalle |
| Movimientos limitados | Modo Hard con cupo de movimientos (200 niveles). | Switch Challenge Edition | [V]; líderes móviles lo evitan |
| Tiempo limitado | Modo Timed. | Switch Challenge Edition | [V]; líderes móviles lo evitan |
| Recipientes bloqueados | Tapas/"hoods" sobre tornillos en Nuts & Bolts Sort. En water sort no se verificó. | GOODROID Nuts And Bolts Sort | [V] en nut sort / [NV] en water sort |
| Niveles marcados "hard"/"super hard" | Etiqueta visible antes de jugar; es donde se consumen boosters. | Magic Sort | [V] |
| Reto diario | Un nivel especial al día con recompensa. | Magic Sort, HDuo | [V] |
| Modo infinito | Niveles generados sin fin; Magic Sort lo vende como suscripción (Endless Portal 5,99 $). | Magic Sort | [V] |
| Eventos / meta | Jigsaw events, historias y mini-aventuras (HDuo); equipos, carreras y "treasure drill" (Magic Sort, inspirado en Royal Match); construcción de pueblo con colecciones temáticas (Hexa Sort). | HDuo, Magic Sort, Hexa Sort | [V] |
| Cosméticos | Skins de tubo, tapas, fondos comprados con monedas ganadas jugando. | Guru, Switch, varios | [V] |

Hexa Sort (referencia de variante no-líquida) [V]: se arrastran pilas de hexágonos a una rejilla; pilas adyacentes del mismo color se apilan automáticamente "como imanes"; al alcanzar 10 fichas la pila desaparece; objetivo de nivel = recolectar N fichas; derrota al llenarse la rejilla. Deconstructor of Fun atribuye su éxito al auto-ordenado (libera carga mental) y al spawn continuo de piezas (convierte el puzzle estático en gestión). Esto es un género distinto; lo citamos solo como fuente de ideas de meta y economía.

## 5. Presentación de la progresión

- Número de nivel grande en la cabecera y lista lineal: es lo que usan IEC, Guru y los clones. [V]
- Estrellas: ninguna ficha de los líderes water-sort menciona estrellas por nivel; el género no las usa de forma estándar. [E a partir de ausencia en fichas]
- Mapa/meta: Magic Sort adopta presentación "premium" tipo Royal Match (equipos, eventos, cofres); Hexa Sort añade un pueblo que se construye con colecciones temáticas. Es el camino que distingue a los juegos de 20-50 M USD de los de solo anuncios. [V]
- Etiquetas de dificultad ("Hard", "Super Hard") antes de entrar al nivel. [V]
- Cadencia de contenido: Magic Sort publica +100 niveles cada semana; Switch Challenge Edition anuncia 5.200 niveles; otros clones "5.000+". [V]

## 6. Dificultad, generación y curva

- Parámetros por dificultad (clon web lofiandgames) [V]: Fácil 5 colores / 7 tubos; Medio 7 / 9; Difícil 10 / 12; Muy difícil 16 / 18 (siempre +2 vacíos; sube la profundidad de mezcla). Otra guía de desarrollo sitúa la progresión típica entre 3 colores / 4 botellas y 10 colores / 12 botellas. [V]
- Pipeline abierto (requiemformemories/water-sort, 150 niveles) [V]: (1) un solver demuestra que el nivel es resoluble y fija el par; (2) un "jugador casual" simulado (elige movimientos de aspecto bueno, sin mirar adelante) lo juega 40 veces; la tasa de fallo es la puntuación de dificultad; (3) se ordenan los niveles de menor a mayor dentro de cada tier. Umbrales: Hard ≥75 % de fallo; Master ≥90 %. 5 tiers × 30 niveles.
- Coste de resolver [V]: un BFS/A* maneja 12 colores con tubos de 4-5 explorando 40.000-400.000 estados; viable en tiempo de build, no en el dispositivo durante la partida salvo con poda.
- Principio de diseño [V]: la dificultad proviene de lo apretado del espacio libre, no del tamaño; un tablero grande con muchos vacíos es largo pero fácil; uno pequeño con un solo vacío obliga a planificar varios movimientos.
- Dos estrategias de generación [V]: (a) mezcla inversa desde el estado resuelto (garantiza solución); (b) relleno aleatorio + solver que filtra. (a) es más rápida; (b) produce dificultad más controlable si se mide con el jugador simulado. [E]
- Señales de curva mal hecha (reseñas IEC) [V]: niveles "prácticamente idénticos" tras el nivel 200; en el 120+ algunos niveles toman 20-30 minutos; quejas por colores ocultos que "eliminan la estrategia".

## 7. Casos borde: deadlock, "no hay más movimientos", reinicio

Tres estados distintos que el motor debe diferenciar [V]/[E]:
1. **Sin movimientos legales**: ningún par origen→destino cumple la regla. Es el único caso en que un juego bien diseñado muestra la tarjeta "No hay más movimientos" con opciones Reiniciar / Deshacer / +1 recipiente. [V]
2. **Solo movimientos fútiles**: queda algún vertido legal pero todos recolocan sin progreso (monocolor → vacío). Hay que tratarlo como el caso 1 o la partida se vuelve un bucle. [E]
3. **Irresoluble pero con movimientos**: el solver sabe que no hay salida, pero el jugador aún puede mover. La PR analizada (Evgeny-/water-sort) elimina el aviso automático "no hay solución desde aquí" tras cada jugada porque frustraba; la pista responde con honestidad solo si el jugador la pide, y la pista automática por inactividad se mantiene en silencio. [V]

Qué hacen los líderes [V]: reinicio gratuito e ilimitado "en cualquier momento" (IEC, Guru); Magic Sort ofrece "+1 botella" al fallar (monetización en el punto de dolor); IEC redujo el premio por anuncio de un tubo completo a 1/4 de tubo (los usuarios lo notaron y se quejaron: el cambio funciona como palanca pero afecta la valoración).

## 8. Subgénero "llenar recipientes" (Happy Glass, fill-the-cup) vs water sort

| Criterio | Fill-the-cup (Happy Glass, Draw To Fill, Fill The Cups, Coffee Cupping) | Water sort / liquid sort |
|---|---|---|
| Mecánica | Dibujar líneas o inclinar para que el agua con física llegue al vaso hasta una marca | Lógica discreta: verter rachas de color entre tubos |
| Niveles | Hechos a mano, con física; difícil generar miles | Generables por algoritmo con solver; miles de niveles sin arte nuevo |
| Monetización observada | Hipercasual: anuncios, pocas IAP | Híbrido: anuncios + boosters + suscripción; subgénero de 280 M USD |
| Riesgo técnico | Simulación de fluidos, rendimiento en gama baja | Shaders simples por capas; sin física |
| Encaje con "Amazon Fill" | El nombre sugiere llenar, pero el concepto del dueño (ordenar colores de frutas, comodines, miles de niveles) es water sort | Encaja al 100 % |

Veredicto: núcleo water sort. La idea de "llenar" se expresa en la **animación de carga del nivel** (los recipientes se sumergen en el río, se tiñen con el color de fruta y emergen) y, si se quiere, en un **mini-juego de bonus** físico cada X niveles, nunca como mecánica principal. [E]

## Implicaciones para Amazon Fill

1. **No inventar reglas**: capacidad 4, racha completa, +2 vacíos, tap-tap, auto-tapado, reinicio gratis. Cada desviación aleja al jugador que ya domina el género (el 100 % de los 1.000 M de descargas de 2025).
2. **Recipiente autóctono transparente**: la silueta debe ser casi cilíndrica y de boca ancha, porque las capas deben leerse como 4 segmentos iguales; una tinaja de base ancha deforma la lectura de volúmenes. Probar con 4 segmentos marcados por líneas sutiles en el vidrio. [E]
3. **Teñido en vivo = carga del nivel, no mecánica**: sumergir y teñir dura 1-1,5 s máximo y debe poder saltarse con un tap (los jugadores encadenan 20-40 niveles por sesión). Precalcular el estado y animar solo la presentación. [E]
4. **Paleta de frutas**: necesitamos 16 colores distinguibles para los tiers altos (açaí, cupuaçu, camu-camu, maracuyá, guaraná, copoazú, aguaje, arazá, cocona, pitanga, etc.). Validar contraste y añadir icono/patrón de fruta en modo daltónico; con 10+ colores la confusión verde/teal/lima es el error clásico. [E]
5. **Comodines re-tematizados con la misma función**: Deshacer (="volver la corriente"), +1 recipiente (="afluente extra": un tubo vacío adicional; opcionalmente de capacidad reducida como IEC), Pista (="guía ribereña"), Shuffle/remezcla (Magic Sort) y Saltar nivel. Economía: 5 deshacer gratis/nivel como IEC; +1 recipiente por anuncio recompensado o monedas; Shuffle solo por monedas.
6. **Variantes por fases**: niveles 1-30 solo núcleo; 30-100 introducir etiqueta "Difícil"; 100+ colores ocultos ("agua turbia del río") en niveles marcados; 200+ recipientes de capacidad 5-6 ("tinaja grande"); reto diario desde el día 2; modo infinito ("río sin fin") como suscripción. Evitar movimientos/tiempo limitados en la ruta principal; reservarlos para eventos opcionales.
7. **Pipeline de niveles**: generador + solver + jugador simulado (40 partidas, tasa de fallo) en build; exportar JSON con par y dificultad; etiquetar automáticamente "Difícil" ≥75 % y "Muy difícil" ≥90 %. Garantiza "miles de niveles" sin diseño manual y una curva medible.
8. **Deadlock UX**: detectar (a) sin movimientos legales y (b) solo movimientos fútiles; mostrar una tarjeta única con Deshacer / Afluente extra / Reiniciar. No anunciar "irresoluble" de forma proactiva. Pista solo si existe solución.
9. **Fondos por país**: la rotación de fondos (Brasil, Perú, Colombia, Ecuador, Bolivia, Venezuela, Guyana, Surinam) sigue la lógica de Pixel Flow que Deconstructor of Fun señala como ventaja de UA: arte variable sobre mecánica constante. Cada 20 niveles un país nuevo; el fondo es además el creativo de anuncio. [E]
10. **Monetización de referencia**: No Ads 2,99-7,99 $; packs de monedas 0,99-7,99 $; bundle de rescate ~2,99 $; suscripción 5,99 $/mes para infinito + sin anuncios. Anuncios intersticiales a partir del nivel ~20 (Magic Sort empieza en el 22). Las reseñas negativas de IEC muestran el límite: anuncios tras cada nivel y de más de 30 s destruyen la valoración.

## Especificación de reglas lista para programar

1. Un nivel es un conjunto de T recipientes; cada recipiente tiene capacidad C (por defecto 4; permitido 2-6 por recipiente, configurable por nivel).
2. El estado de un recipiente es una pila de 0..C unidades; cada unidad tiene un color c ∈ {1..K} o es desconocida (oculta) con color real asignado pero no visible.
3. En el estado inicial hay exactamente C unidades de cada color en total; K recipientes están llenos y E = T − K están vacíos (por defecto E = 2; E = 1 permitido en niveles "Muy difícil").
4. El jugador selecciona un recipiente origen A con tap; A se eleva. Un segundo tap sobre A lo deselecciona; un tap sobre otro recipiente B intenta el vertido A→B.
5. Vertido legal A→B si y solo si: A no está vacío, A no está sellado, B ≠ A, B no está lleno, y (B está vacío o color_superior(B) == color_superior(A)).
6. Cantidad vertida = min(longitud de la racha superior de A del mismo color visible, espacio libre de B). Si la racha es mayor que el espacio, se vierte parcialmente y el resto queda en A.
7. Una unidad oculta no forma parte de la racha visible aunque su color real coincida; se revela (pasa a visible) en el momento en que queda como unidad superior de su recipiente.
8. Cada vertido legal cuenta como 1 movimiento y se apila en el historial para Deshacer. Deshacer restaura el estado exacto, incluida la visibilidad de unidades ocultas (lo revelado permanece revelado).
9. Si un vertido es ilegal, el recipiente A vuelve a su posición con una animación corta y un sonido neutro; no se consume movimiento ni se muestra texto.
10. Un recipiente queda **sellado** cuando contiene exactamente C unidades del mismo color visible. Se le dibuja tapa, no puede ser origen ni destino y emite feedback (sonido + haptic ligero).
11. El nivel se completa cuando todos los recipientes están vacíos o sellados. Los recipientes vacíos sobrantes son válidos.
12. Al completar: bloquear input, reproducir celebración (confeti + sonido + haptic), mostrar recompensa y botón "Siguiente"; auto-avanzar tras 2-3 s si el jugador no toca. [E en tiempos]
13. Tras cada movimiento el motor calcula el conjunto de vertidos legales **no fútiles** (se excluye verter un recipiente monocolor completo o parcial a uno vacío cuando es el único tipo de movimiento). Si el conjunto está vacío → estado "atascado": mostrar tarjeta "No hay más movimientos" con Deshacer / Afluente extra / Reiniciar.
14. El motor nunca anuncia "irresoluble" de forma espontánea. La Pista llama al solver desde el estado actual: si hay solución, ilumina origen y destino del primer movimiento; si no la hay, informa que no existe salida y ofrece Deshacer o Reiniciar.
15. Pista automática por inactividad (opcional, ≥8 s sin input) solo si el solver confirma solución; nunca en posición perdida.
16. Reiniciar es gratuito e ilimitado, restaura el estado inicial del nivel (misma semilla) y vacía el historial. Los boosters consumidos no se devuelven.
17. Comodín **Afluente extra** (+1 recipiente): añade un recipiente vacío de capacidad C (o C/2 según configuración de economía) al final de la fila; máximo 1-2 por nivel; se elimina al reiniciar.
18. Comodín **Deshacer**: 5 usos gratuitos por nivel; a partir del sexto, anuncio recompensado o monedas. Deshacer tras "atascado" cierra la tarjeta.
19. Comodín **Remezcla** (Shuffle): reordena aleatoriamente las unidades no selladas respetando la regla 3 y exige que el solver confirme solución antes de aplicarla; solo por monedas.
20. Comodín **Saltar nivel**: disponible tras N fallos/reinicios en el mismo nivel (sugerido N = 3); por monedas o anuncio.
21. El generador produce niveles por mezcla inversa o relleno aleatorio y obliga a: solución existente (solver), par = longitud mínima, dificultad = tasa de fallo de un jugador simulado greedy en 40 partidas. Etiquetas: Normal <75 %, Difícil 75-89 %, Muy difícil ≥90 %.
22. Capacidades mixtas: cuando C varía por recipiente, la regla 10 usa la C propia de cada recipiente; el total de unidades por color debe coincidir con la capacidad del recipiente que lo albergará (o el nivel declara explícitamente qué recipientes pueden quedar parcialmente llenos como "vacíos" al final). [E: regla de diseño propia para evitar niveles sin victoria posible]
23. Nivel con colores ocultos: al inicio solo la unidad superior de cada recipiente es visible; la UI muestra "?" sobre las ocultas. El generador garantiza que exista una estrategia segura desde el estado inicial (comprobar que el primer movimiento recomendado es válido bajo todas las permutaciones de ocultos o aceptar riesgo controlado). [E]
24. Las animaciones de vertido deben ser interrumpibles por el siguiente tap (se encola el siguiente movimiento; el estado lógico se actualiza al instante y la vista alcanza al modelo).
25. Persistencia: guardar estado del nivel en curso tras cada movimiento (cierre de app sin pérdida), número de nivel, boosters, monedas y niveles diarios completados.

## Preguntas abiertas

- **Duración exacta de animaciones** de elevación y vertido en IEC/Guru/Magic Sort: no publicada; hay que medirla grabando pantalla (recomendado antes de fijar los valores de la regla 24).
- **"Reglas de vertido modificadas" de Magic Sort**: existen según su ficha, pero no se halló descripción pública; conviene jugar hasta nivel 100-150 para catalogarlas.
- **Recipientes bloqueados/con tapa en water sort**: verificado solo en nut sort (GOODROID). Decidir si lo adoptamos como variante propia ("tinaja con tapa de hoja") sin referente directo.
- **Estrellas o puntuación por nivel**: no son estándar en el género; decisión del dueño de producto si queremos un sistema de 3 estrellas (p.ej. por movimientos ≤ par) para alimentar la meta.
- **Afluente extra completo vs 1/4 de capacidad**: IEC pasó a 1/4 por anuncio con quejas visibles; falta A/B interno para elegir.
- **Capacidad 4 vs recipiente autóctono**: hay que validar en prototipo si una tinaja/cuia/maceta amazónica transparente mantiene legible la lectura de 4 segmentos; de lo contrario usar un vaso cilíndrico con ornamentación étnica en bordes y base.
- **Cifras de descargas de Google Play**: las fichas de Play bloquean el scraping; los números de MixRank/AppGoblin son estimaciones de terceros aunque coherentes entre sí.
- **Anuncios desde el nivel 22 (Magic Sort)**: dato de un análisis temprano (Gamigion, 2024); puede haber cambiado.
- **Hexa Sort reglas exactas (umbral 10)**: tomado de fichas de clones y de un análisis de Mistplay; la ficha oficial de Lion Studios no detalla el umbral.

## Fuentes

- https://apps.apple.com/us/app/water-sort-puzzle/id1514542157
- https://mixrank.com/playstore/apps/com.gma.water.sort.puzzle
- https://appsupports.co/1514542157/water-sort-puzzle/negative-reviews
- https://apps.apple.com/us/app/water-sort-puzzle-sort-color/id1597897013
- https://appgoblin.info/apps/water.sort.puzzle.game.color.sorting.free
- https://play.google.com/store/apps/details?id=water.sort.puzzle.game.color.sorting.free&hl=en_US
- https://apps.apple.com/us/app/magic-sort/id6499209744
- https://appgoblin.info/apps/com.grandgames.magicsort
- https://play.google.com/store/apps/details?id=com.grandgames.magicsort&hl=en_US
- https://www.gamigion.com/magic/
- https://magicsort.app/
- https://apps.apple.com/us/app/hexa-sort/id6463127238
- https://www.costcenter.net/post/the-hexa-sort-competitor-analysis
- https://apps.apple.com/us/app/-/id6736754592
- https://apps.apple.com/us/app/-/id6499307202
- https://apps.apple.com/us/app/ball-sort-color-water-puzzle/id1550569446
- https://game-solver.com/ball-sort-color-water-puzzle/
- https://apps.apple.com/app/id1548462326
- https://apps.apple.com/us/app/id6447139347
- https://play.google.com/store/apps/details?id=com.zm.watersort&hl=en_US
- https://appgoblin.info/apps/water.sorting.games.liquid.color.sort.puzzle
- https://www.nintendo.com/us/store/products/color-water-sort-challenge-edition-switch/
- https://www.iofreeonline.com/IOS/game/Water-Sort-Puzzle-Get-Color.html
- https://www.deconstructoroffun.com/blog/2026/2/6/sort-puzzles-how-a-new-subgenre-is-born
- https://naavik.co/digest/how-niche-subgenres-are-reshaping-the-mobile-puzzle-market/
- https://www.brsoftech.com/blog/develop-water-sort-puzzle-game/
- https://www.capermint.com/blog/water-sort-puzzle-bottle-game/
- https://arxiv.org/abs/2202.09495
- https://ar5iv.labs.arxiv.org/html/2202.09495
- https://www.iss.is.tohoku.ac.jp/publications/2023-01-tcs/
- https://www.lofiandgames.com/water-sort-puzzle
- https://chromaoracle.com/water-sort-puzzle-solver
- https://chromaoracle.com/guides/water-sort-strategy
- https://chromaoracle.com/guides/color-sort-strategy-guide
- https://github.com/requiemformemories/water-sort
- https://dev.to/kang_wang_375088cb68739bf/how-i-built-a-water-sort-puzzle-solver-in-about-300-lines-of-javascript-5edi
- https://github.com/Evgeny-/water-sort/pull/10
- https://arcadino.com/game/water-sort-puzzle/
- https://courses.cs.ut.ee/2023/cg/fall/Main/Project-GlitteryWaterPuzzle
- https://codecanyon.net/item/water-sort-color-sorting-complete-unity-source-code/36043495
- https://playwatersort.org/blog/water-sort-tips-for-hard-levels
- https://apps.apple.com/us/app/-/id1534546146
- https://appgoblin.info/apps/6767797111
- https://apps.apple.com/us/app/id6748220732
- https://play.google.com/store/apps/details?id=jp.co.goodroid.hyper.nutsandboltssort&hl=en_US
- https://apps.apple.com/us/app/-/id6749339826
- https://selamisafabalik.itch.io/fill-the-cups
- https://playfulbytes.itch.io/coffee-cupping
- https://www.gamepressure.com/games/happy-glass/zf5703
- https://apps.apple.com/br/app/water-sort-game-puzzle-%C3%A1gua/id1330476152
- https://apps.apple.com/hn/app/water-sort-juego-de-puzzle/id6511226266
