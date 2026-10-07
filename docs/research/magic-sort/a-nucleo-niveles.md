# Magic Sort (Grand Games) — Frente A: núcleo jugable y diseño de niveles

**Proyecto:** Amazon Fill · **Fecha:** 2026-10-05 · **Autor:** análisis de referencia competitiva (estilo DoF / Naavik)
**Objeto:** *Magic Sort! – Water Sorting Puzzle*, Grand Games A.Ş. (Estambul). Android `com.grandgames.magicsort` · iOS `id6499209744`.

> Convención: **[V]** = dato verificado con URL en "Fuentes"; **[E]** = estimación o inferencia del analista; **[R]** = procede de reseñas de usuarios (fiable como tendencia, no como cifra exacta). Los sitios de guías (levelsolve, sortpuzzlehelp, magicsort.app) documentan **una** variante de cada nivel; Magic Sort sirve variantes distintas por versión/plataforma, así que los números por nivel son ilustrativos, no canónicos.

---

## 1. Resumen ejecutivo

Magic Sort no reinventa el water‑sort: toma la regla clásica (verter solo sobre el mismo color o sobre vacío, capacidad 4) y la envuelve en producción "Dreamificada" tipo Royal Match, una curva de dificultad en diente de sierra con niveles **Hard / Super Hard** señalizados, y un *fail state* sin límite de movimientos cuyo único remedio inmediato es **"+1 frasco" por 900 monedas**. Esa oferta end‑of‑round es el motor IAP del juego **[V: Naavik, Gamigion]**. El juego añade contenido con cadencia semanal ("New 100 levels!" en 25 versiones entre el 21‑feb y el 4‑oct‑2026) **[V: App Store GB]**, cuenta con una docena de bloqueadores (frascos "?", corchos por color, candados, cajas regalo, cortinas, paneles de madera, frascos de tamaño variable) introducidos de forma gradual pero —según la crítica— **demasiado tarde y con poca novedad** **[V: Gamigion deconstruction]**. Las quejas se concentran en tres puntos: colores ocultos que convierten la estrategia en azar, picos de dificultad a partir de ~niveles 70‑100 y de nuevo ~200‑300, y una economía (10‑20 monedas por victoria vs 900 por frasco extra) que empuja al pago **[R]**.

Para Amazon Fill la lectura es clara: copiar el núcleo (reglas, capacidad 4, tap‑tap, oferta +1 frasco, cadencia de contenido, multiplicador de racha en pantalla de victoria), **mejorar** la introducción temprana de mecánicas, la legibilidad de los ocultos y la justicia percibida del fail state, y **evitar** la dependencia de la fuerza bruta y los eventos de racha que castigan al no pagador.

---

## 2. Reglas exactas del núcleo

| Elemento | Magic Sort | Estado |
|---|---|---|
| Control | Tap en frasco origen → tap en frasco destino. Sin arrastre. | [V: ficha App Store/Play] |
| Regla de vertido | Solo se vierte el **bloque superior** (todas las unidades contiguas del mismo color de arriba). Destino válido: vacío **o** mismo color arriba, **y** con espacio. Si el espacio no basta, se vierte lo que cabe (regla estándar del género). | [V: magicsort.app guía; levelsolve] |
| Capacidad | **4 unidades** en el frasco estándar. Existen frascos de **1, 2 y 3** unidades ("small flask", "medium", "mixers" de 2‑3 segmentos) y frascos "altos" (tall) con más capacidad; el tamaño variable es parte del diseño de niveles, no un extra. | [V: levelsolve L280, L331, L442; magicsort.app; sortpuzzlehelp "capacity 4"] |
| Condición de victoria | Todos los frascos monocolor **o** vacíos. No hay estrellas por movimientos ni penalización por exceso de movimientos. | [V: magicsort.app] |
| Límite de movimientos / tiempo | **Ninguno** en niveles estándar. El juego "mantiene la calma de no tener temporizadores ni límites de movimientos". | [V: Gamigion /magic/; iofreeonline] |
| Frascos por nivel | Crece de 3 (N1) a ~14 (N30); en el medio juego 10‑13 frascos en 2‑3 filas; hasta 11‑12 celdas activas con elementos ocultos. | [V: magicsort.app tabla 1‑30; levelsolve] |
| Colores | 2 (N1) → 10 (N30) → 8‑11 colores en niveles 200‑500. | [V: idem] |
| Vacíos iniciales | Normalmente **1**; a veces 2‑3 en niveles de respiro; algunos niveles con 0 vacíos "limpios" (el hueco está dentro de un frasco parcialmente lleno) o con huecos **bloqueados por candado** que se liberan a mitad de nivel. | [V: magicsort.app; levelsolve L221, L331] |
| Fail state | No se "pierde" por movimientos: se pierde cuando no queda **ningún vertido legal**. En ese instante aparece la oferta de continuar con **+1 frasco por 900 monedas**. Si se rechaza, se pierde **1 vida** y se reinicia el nivel. | [V: Naavik; Gamigion; R: App Store US/AU] |
| Vidas | **5 corazones**; recarga ~25‑30 min por vida; vidas por anuncio (vídeo largo) y por petición al equipo; periodos de vidas infinitas de 15‑30 min como recompensa y de 6‑48 h en bundles. | [R: App Store; V: Gamigion shop] |
| Comodines | **Undo**, **Shuffle**, **+1 Bottle**. Se compran en bundles en cantidades iguales de los tres. Crítica recurrente: al quedarte sin movimientos **no puedes usar Undo/Shuffle** sin pagar antes el continue. | [V: Gamigion shop; R] |
| Monedas por victoria | ~10‑20 monedas por nivel; bonus de racha (≈300 monedas por 3 victorias seguidas, según reseñas). Ratio 900:20 ≈ 45 victorias por un frasco extra. | [R: Play/App Store] |
| Anuncios | Interstitials empiezan en el **nivel 22**; rewarded para vidas y monedas. El juego es IAP‑first (ingreso IAP ≈ 3,3× el de ads). | [V: Gamigion /magic/; Gamigion "Monetization Engine"] |

**Dificultad dinámica verificada:** Naavik documenta que al fallar el nivel 26 en el primer intento, el segundo intento **elimina un color** del tablero **[V]**. Las reseñas y los sitios de guías confirman que "el puzle cambia al reintentar" y que "las disposiciones varían entre iOS/Android y versiones" **[R; V: magicsortlevel, magicsort.net]**. Interpretación **[E]**: niveles con plantilla + generación de variantes, con selección de variante por intento (y probablemente por cohorte A/B), no niveles únicos hechos a mano.

---

## 3. Progresión de la primera media hora (una variante, niveles 1‑30)

| Nivel | Frascos | Colores | Vacíos | Novedad |
|---|---|---|---|---|
| 1 | 3 | 2 | 1 | Tutorial: "termina el frasco casi lleno antes de aparcar" |
| 3 | 5 | 3 | 1 | — |
| 6‑9 | 6‑7 | 4 | 1‑2 | — |
| 10 | 8 | 5 | 1 | Frasco "ayudante" pequeño en fila inferior |
| 13‑15 | 9 | 6 | 1 | — |
| 14 | 9 | 5 | 1 | Frasco gris de **1 unidad** ("shuttle lane"): primer frasco de capacidad reducida |
| 16‑19 | 10 | 6‑7 | 1‑2 | — |
| 20‑23 | 11 | 8 | 1 | — |
| 22 | — | — | — | **Primer interstitial** |
| 24 | 12 | 8 | 3 | Nivel de respiro |
| 25 | 10‑12 | 6‑9 | 1 | "Shell bottles" (frascos cubiertos que hay que "romper") ocupan una fila |
| 26 | 12 | 9 | 1 | Nivel con **ajuste dinámico** al fallar (‑1 color) |
| 28‑30 | 13‑14 | 9‑10 | 1‑2 | — |

Fuente: tabla de magicsort.app y páginas de levelsolve **[V]**. Tiempos estimados por el sitio de guías: 15 s–1,5 min (N1‑5), 2‑6 min (N6‑10), 4‑8 min (N11‑15), hasta 8‑25 min (N26‑30). Gamigion reporta que su analista completó **60 niveles el primer día** **[V]**, coherente con sesiones de 1‑3 min por nivel.

---

## 4. Mecánicas especiales y nivel de introducción

| Mecánica (nombre en guías) | Comportamiento | Primer nivel documentado |
|---|---|---|
| Frasco de capacidad reducida (1‑3 u.) | Actúa como buffer; obliga a planificar qué color "le pertenece". | ~14 **[V]** |
| Frascos cubiertos / "shell" / residuo blanco | Columnas con tapa o residuo que se convierten en frasco utilizable solo cuando cae el color correcto; funcionan como "almacén semicerrado". | 25‑60 **[V: L25, L43, L60, L100]** |
| **Frasco "?" (Magic bottle)** | Vacío sin color asignado: el **primer** color vertido lo fija; si entra un segundo color distinto queda bloqueado en una pila de dos colores. Variante: frascos con contenido oculto por "?" que se revela al destapar. | ~70 (6 frascos "?" en fila superior) **[V: L70]**; tips oficiales en App Store Story |
| Paneles de madera (doors) | Ocultan una segunda fase del tablero; se abren por acción del jugador — "el puzle es cronometrar la revelación". | 150 **[V]** |
| **Corchos por color** (corked) | Tapa con un color‑llave; solo se abre al verter ese color encima; debajo hay un color revelado. Pueden llevar numerito (cantidad de vertidos necesarios). | 221 (6 a la vez) **[V]** |
| Huecos con candado dorado | Frascos vacíos bloqueados que se liberan a mitad de nivel. | 221 **[V]** |
| Cortina de teatro | Oculta frascos extra; cae **sola** cuando hay N vacíos en la fila (insignia "1", "3"). | 331 **[V]** |
| Caja regalo | Se abre con **cualquier** vertido; debajo hay un frasco multicolor; verter el color equivocado lo deja "varado". | 400 **[V]** |
| Caja fuerte de madera con candado de color | Como corcho, pero el color‑llave puede salir de **otra** caja: cadenas de desbloqueo. | 442 **[V]** |
| Caja central con **temporizador**; tubos "grandes" inferiores; tubos estrechos | Mencionados en guía del nivel 1000 (super hard). | ~1000 **[V: magicsortlevel]** |
| Otros nombres citados por guías sin nivel exacto | "wraps", "spray cans", "frozen tiles / frost bases" (hielo), "key tags / scroll‑key tiles" (llaves), "star magic flasks". | — **[V: menciones en levelsolve; nivel no confirmado]** |

Temática de capítulos (App Store Story): tres gatos traviesos; capítulos con escobas voladoras, **portales** y **slime pegajoso** **[V]** — lo que sugiere que portales y slime son también bloqueadores de nivel (**[E]**: portal = teletransporte de líquido entre frascos; slime = capa que ralentiza/bloquea).

**Juicio de la crítica [V: Gamigion deconstruction]:** "las mecánicas nuevas se introducen tarde y carecen de novedad". La tabla lo confirma: el primer bloqueador realmente nuevo (corchos) llega al nivel ~221; entre 100 y 220 el juego vive de frascos cubiertos y tamaño variable.

---

## 5. Niveles Hard y Super Hard

- **Existen y están etiquetados** en la UI como "Hard" y "Super Hard"; Naavik habla de "balanceo tras bambalinas de niveles hard y super hard" y los jugadores los nombran constantemente **[V: Naavik, Gamigion; R]**.
- **Señalización visual:** no hemos encontrado una fuente textual que describa el color. **[E]**: siguiendo la convención Royal Match / Zen Match que Grand Games heredó, Hard = marco/fondo **morado** y Super Hard = **rojo oscuro**, con cartel previo al nivel. Pendiente de confirmar con captura.
- **Cadencia:** no publicada. Evidencia indirecta **[V: levelsolve, 442 niveles clasificados por la comunidad]**: 15 fáciles, 61 medios, **202 hard, 164 expert** — y un tramo **43‑112 íntegramente "expert"**. Las reseñas sitúan los muros en ~70‑99 y en ~200‑300 **[R]**. **[E]**: cadencia aproximada de 1 Hard cada 5‑8 niveles y 1 Super Hard cada 15‑25 en el medio juego, con densidad creciente.
- **Qué cambia [V/E]:** más colores (9‑11), un solo vacío real, frascos "?" múltiples (6 en el N70), menos frascos "ancla" ya empezados, y bloqueadores encadenados. El jugador percibe que "no hay forma de estrategizar, solo azar" **[R]**.
- **Recompensa extra:** las reseñas mencionan más monedas y mayor avance en eventos; no hemos verificado la cifra. Royal Match da ×2‑×3 de monedas en Hard/Super Hard **[E]**.
- **Respiro:** verificado que tras tramos duros hay niveles "puros" sin gimmicks y con 2 vacíos (N200, N280, N420) **[V]** — el diente de sierra existe por diseño.

---

## 6. Volumen de niveles y cadencia de actualización

| Dato | Valor | Estado |
|---|---|---|
| Versiones iOS 21‑feb → 4‑oct‑2026 | **25 versiones**, todas con la nota "New 100 levels! Bug Fixes & Improvements" (≈ 1 cada 9 días) | [V: App Store GB] |
| Versión actual | iOS 32.92 (4‑oct‑2026, 609,2 MB); Android 0.0.3293 (3/4‑oct‑2026, APK/XAPK 218‑234 MB) | [V: App Store; uptodown; apkcombo] |
| Niveles documentados por guías | levelsolve: **1.158 publicados** (447 con solución); magicsort.app: 1.038+; iofreeonline (abril 2026): 594+ | [V] |
| Total real | No publicado. Si la nota es literal, el juego añade ~400 niveles/mes y superaría ampliamente los 2.000‑3.000; las guías van muy por detrás. | [E] |

Gamigion atribuye el salto a 120 K USD/día (marzo 2025) precisamente a "actualizar rigurosamente con más y más niveles: el diseño de niveles y la frecuencia de contenido son la columna vertebral del género" **[V]**.

---

## 7. Controles, feedback y "juiciness"

- **Tap‑tap** sin arrastre; los niveles son "rápidos, con feedback responsivo y puzles claros y legibles", "casi a nivel Royal Match" **[V: Naavik]**.
- **Encadenado de inputs:** las reseñas elogian que se pueden **encolar movimientos sin esperar la animación** de vertido **[R: App Store US]** — clave para la sensación de velocidad.
- **Duración de la animación:** no documentada. **[E]** ~0,3‑0,5 s por vertido con el frasco inclinándose sobre el destino; el encolado evita que el ritmo lo marque la animación.
- **Frasco completado:** efecto de cierre/brillo. **[E]** tapón o halo + partículas; sin fuente textual.
- **Pantalla de victoria [V: Gamigion "Loved this little UI detail"]:** muestra el **multiplicador de racha** con escalones **×2 → ×5 → ×10 → ×25 → ×100**, y el mismo multiplicador reaparece en la **barra de progreso del evento**. El doble emplazamiento hace la racha tangible y recuerda lo que se pierde al fallar. Además: monedas ganadas, barras de progreso de eventos (Broom Streak, Jetpack Race, Sky Jump, Magic Streak), progreso del reto diario y cartas coleccionables **[V: Naavik; R]**.
- **Ambientación:** tres gatos como guías, capítulos temáticos (escobas, portales, slime) **[V: App Store Story]**; gráficos "vibrantes" y animaciones "suaves" son el elogio visual más repetido **[R]**.
- **Haptics / sonido:** no documentados en fuentes textuales; **[E]** presentes (estándar en el nicho).

---

## 8. Dificultad: curva, generación y ajustes

1. **Diente de sierra verificado:** tramos "expert" largos (43‑112) intercalados con niveles puros de respiro (200, 280, 420) **[V]**.
2. **Dificultad adaptativa verificada** al menos en el early game (N26: segundo intento con un color menos) **[V: Naavik]**.
3. **Variantes de nivel:** las guías publican "Version 1 / 2 / 5" para un mismo nivel y advierten que el layout cambia por versión y plataforma; los jugadores se quejan de que el puzle "no recarga igual" y no pueden refinar estrategia **[V; R]**. **[E]**: generación procedural a partir de plantillas (número de colores, tamaños, bloqueadores) con *solver* que garantiza solubilidad y fija el "target moves" (17, 26, 34, 50… en las guías).
4. **Ajustes tras quejas:** no hay evidencia pública de cambios de balance anunciados; las notas de versión son boilerplate. El dato duro es que el **ARPDAU ha caído significativamente desde su pico inicial** (Sensor Tower vía Gamigion) **[V]**, y el propio Gamigion lo vincula a "cuando el equilibrio comodidad/ruptura de flujo falla".
5. **Percepción de control:** la crítica central (Gamigion): "el desarrollador controla tu progreso, no tú; la suerte pesa demasiado comparado con match‑3; el juego te **fuerza** a pagar en vez de **motivarte** a pagar" **[V]**.

---

## 9. Métricas de contexto (para calibrar la ambición)

| Métrica | Valor | Fuente |
|---|---|---|
| Ingresos primer año | > 40 M USD (2025) | DoF [V] |
| Ingreso diario | 80‑100 K USD/día (ene‑2025) → 120 K USD/día (mar‑2025), nº 1 del género sort por IAP | Gamigion [V] |
| 30 días a mayo‑2026 | 478 K DAU; 3,3 M USD IAP + 1 M USD ads; ARPU 8,68 USD; 43 % del IAP del top‑10 sort con 3,4 % del DAU | Gamigion [V] |
| Ranking género (2026) | nº 2 por ingresos tras Pixel Flow; **campeón de descargas** sort en septiembre 2026 (AppMagic) | Gamigion [V] |
| Compañía | Series B 70 M USD (mayo‑2026), total 103 M; ~75 empleados; 50 M+ descargas; Magic Sort nº 1 descargas iOS EE. UU. | PG.biz, mobilegamer [V] |
| Store | iOS 4,7★ (669 K, EE. UU.); Android 10 M+, 4,5★ (~555 K reseñas agregadas) | App Store; apkcombo [V] |
| Benchmarks retención del nicho | D1 40‑50 %, D30 < 10 %; Hexa Sort > 15 % D30; Hexa Away 4 % D30 "insuficiente para un juego IAP" | Naavik; Gamigion [V] |

---

## 10. Reseñas: qué elogian y qué critican del gameplay

**Elogios (≈30‑40 % de las reseñas visibles) [R]:** bucle relajante y adictivo; gráficos limpios y animaciones suaves; posibilidad de encadenar movimientos; pocos anuncios hasta ~nivel 60 ("un solo anuncio hasta el 62"); interfaz simple; equipos y cartas coleccionables; "el mejor del género" según varios.

**Quejas (≈60‑70 %) [R]:**
1. **Colores ocultos = azar**: "adivinas; si fallas no hay movimientos y no puedes usar comodines".
2. **Picos de dificultad** en ~70‑99 y ~200‑300; etiquetas que no reflejan la dificultad real; un usuario con 16 intentos en un nivel.
3. **Economía**: 900 monedas por +1 frasco vs 10‑20 por victoria (300 por racha de 3); "pay to play" en Super Hard.
4. **Fail state injusto**: al quedarte sin movimientos no puedes usar Undo/Shuffle sin pagar el continue.
5. **Vidas**: 5 vidas y ~25 min por recarga; anuncios largos por vida extra.
6. **Reintento cambia el puzle**: imposible aprender el nivel.
7. **Eventos de racha** que solo mantienen los que pagan; premio de evento inferior al gasto (7 € por un premio menor, Gamigion).
8. Bugs puntuales: comodín consumido sin efecto; cambios de posición en leaderboard.

---

## Qué copiar / qué mejorar / qué evitar para Amazon Fill

### Copiar (funciona y está probado)
- **Reglas canónicas**: capacidad 4, vertido del bloque superior, destino vacío o mismo color, victoria por monocolor/vacío, **sin límite de movimientos ni tiempo**. Es la base de la "calma" que monetiza.
- **Tap‑tap con encolado de inputs** y animaciones que nunca frenan al jugador.
- **Fail state = sin vertidos legales → oferta inmediata "+1 recipiente"** por moneda blanda, con fallback a perder una vida. Es el motor IAP del líder.
- **Frascos de tamaño variable** desde ~N14: añade profundidad sin nuevas reglas.
- **Diente de sierra explícito** con niveles Hard/Super Hard señalizados y niveles de respiro "puros" después.
- **Dificultad adaptativa** (quitar un color al reintentar en early game).
- **Pantalla de victoria con multiplicador de racha ×2→×100** visible también en la barra del evento.
- **Cadencia de contenido**: 100 niveles/semana es el estándar del nicho; planificar pipeline procedural + solver desde el día 1.
- Anuncios intersticiales **no antes del nivel ~20**; IAP‑first.

### Mejorar (donde Magic Sort recibe las quejas)
- **Introducir bloqueadores antes** (primer bloqueador de verdad en N30‑50, uno nuevo cada 40‑60 niveles) y con **lógica amazónica legible**: hoja que tapa (cubierto), semilla que germina al recibir su color (corcho), liana/raíz que ata recipientes (candado), remolino de río (portal), hielo/barro, cesta tejida de tamaño variable, frutas que tiñen.
- **Ocultos sin azar puro**: cuando haya recipientes ocultos, garantizar que el solver exige **≥1 vertido seguro** siempre disponible, o mostrar pistas parciales (silueta del color, "1 de 3 revelado") y un mini‑comodín "Linterna/Luciérnaga" barato para revelar uno.
- **Fail state más justo**: permitir **Undo en el momento del bloqueo** (1 gratis cada N niveles) y precio del "+1 recipiente" en relación 10‑15:1 con las monedas por victoria, no 45:1; primer continue del día con descuento.
- **Reintento consistente**: misma semilla en el reintento inmediato (para que la gente aprenda), nueva variante solo tras 3 fallos, y comunicar el ajuste ("hemos quitado un color").
- **Señalización de dificultad honesta** (Hard/Super Hard con histograma de intentos de la comunidad o "tiempo medio"), más recompensa visible (×2/×3 monedas) por superarlos.
- **Juiciness con identidad**: tinte de fruta al cargar nivel, agua de río que se aclara al completar, pájaro/ave que canta al cerrar un recipiente, haptics por vertido.
- **Peso de la app**: 609 MB en iOS es un freno en Android LatAm; objetivo < 150 MB con assets bajo demanda.

### Evitar
- Dependencia de la **fuerza bruta** en Super Hard (reseñas: "no hay estrategia posible").
- **Eventos de racha** cuyo fallo anula el progreso de los no pagadores sin alternativa (ofrecer variantes por tiempo o por puntos acumulados).
- **Mecánicas nuevas solo a partir del nivel 200+**.
- Economía 900/20 que se percibe como "forzar" en vez de "motivar" el pago; el líder ya muestra caída de ARPDAU por este desequilibrio.
- Notas de versión genéricas; comunicar los cambios de balance ayuda a la percepción de justicia.

---

## Fuentes

**Analistas / industria**
- Deconstructor of Fun, "Sort Puzzles: How a New Subgenre is Born" (feb‑2026): https://www.deconstructoroffun.com/blog/2026/2/6/sort-puzzles-how-a-new-subgenre-is-born
- Naavik, "Grand Games and Leveling Up Mobile Casual Puzzle": https://naavik.co/digest/why-peak-games-investors-backed-grand-games/
- Gamigion, "Game Deconstruction: Magic Sort by Grand Games": https://www.gamigion.com/game-deconstruction-magic-sort-by-grand-games/
- Gamigion, "Magic Sort Analysis: Grand Games' New Treasure": https://www.gamigion.com/magic/
- Gamigion, "What's Wrong With Magic Sort's Game Shop?": https://www.gamigion.com/whats-wrong-with-magic-sorts-game-shop/
- Gamigion, "Loved this little UI detail in Magic Sort!": https://www.gamigion.com/loved-this-little-ui-detail-in-magic-sort/
- Gamigion, "The Monetization Engine Behind Magic Sort's Rise": https://www.gamigion.com/the-monetization-engine-behind-magic-sorts-rise/
- Gamigion, "Magic Sort continues to scale to $120K per day": https://www.gamigion.com/magic-sort-by-grand-games-continues-to-scale-to-120k-per-day/
- Gamigion, "Pixel Flow, Magic Sort, Knit Out: Why Players Can NOT Stop": https://www.gamigion.com/pixel-flow-magic-sort-knit-out-why-players-can-not-stop/
- Gamigion, "Sort Puzzle September WINNERS?": https://www.gamigion.com/sort-puzzle-september-winners/
- Gamigion, "Hexa Away Breakdown: 6th Biggest Sort Puzzler": https://www.gamigion.com/hexa-away-breakdown-6th-biggest-sort-puzzler/
- Gamigion, "The 'Why' Behind Grand Games' $30M Investment": https://www.gamigion.com/the-why-behind-grand-games-30m-investment/
- TechCrunch (9‑ene‑2025): https://techcrunch.com/2025/01/09/grand-games-the-turkish-gaming-startup-behind-pouring-game-magic-sort-drinks-up-30m/
- PocketGamer.biz, Series B 70 M USD: https://www.pocketgamer.biz/grand-games-raises-70m-series-b-to-scale-hybrid-casual-gaming/
- mobilegamer.biz, data digest "Grand lands $70m": https://mobilegamer.biz/data-digest-sensor-tower-inmobi-and-smartlys-big-buys-grand-lands-70m-huge-applovin-numbers-more/
- Balderton, anuncio Series A: https://www.balderton.com/news/grand-games-raises-30m-series-a-to-build-genre-defining-mobile-games/

**Fichas de tienda**
- App Store EE. UU.: https://apps.apple.com/us/app/magic-sort/id6499209744
- App Store EE. UU., reseñas: https://apps.apple.com/us/app/magic-sort/id6499209744?see-all=reviews
- App Store Reino Unido (historial de versiones): https://apps.apple.com/gb/app/magic-sort/id6499209744
- App Store Australia, reseñas: https://apps.apple.com/au/app/magic-sort/id6499209744?see-all=reviews
- App Store España: https://apps.apple.com/es/app/magic-sort/id6499209744
- App Store Story (editorial, consejos oficiales): https://apps.apple.com/au/iphone/story/id1873942358
- Google Play: https://play.google.com/store/apps/details?id=com.grandgames.magicsort&hl=en_US
- APKCombo (espejo Play, versiones): https://apkcombo.com/magic-sort/com.grandgames.magicsort/
- Uptodown (versiones Android): https://com-grandgames-magicsort.en.uptodown.com/android

**Guías y agregadores de reseñas**
- levelsolve (mecánicas por nivel, distribución de dificultad): https://levelsolve.com/magic-sort/ y páginas /level/1, 10, 14, 25, 43, 60, 70, 100, 150, 180, 200, 221, 280, 331, 400, 420, 442
- magicsort.app, walkthrough 1‑30 y guías: https://magicsort.app/blog/complete-walkthrough-magic-sort-levels-1-30 · https://magicsort.app/blog/master-magic-sort-beginners-guide · https://magicsort.app/blog/advanced-strategies-magic-sort · https://magicsort.app/levels
- sortpuzzlehelp (capacidad 4, variantes): https://sortpuzzlehelp.com/magic-sort-answers/
- magicsortlevel, nivel 1000: https://magicsortlevel.com/levels/1000
- iofreeonline (resumen de reseñas, vidas, 900 monedas): https://www.iofreeonline.com/IOS/game/Magic-Sort.html
- Glotier (estadística de reseñas): https://glotier.com/games/magic-sort
- mwm.ai (ficha): https://mwm.ai/apps/magic-sort/6499209744
