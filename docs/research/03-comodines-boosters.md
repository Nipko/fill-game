# Comodines (boosters) en juegos water-sort / ball-sort: catálogo, obtención, economía y propuesta para Amazon Fill

Fecha: 2026-10-05. Investigación de escritorio sobre 12 títulos (fichas de tienda, centros de ayuda, guías, análisis de industria). Convención: **[V]** = verificado con URL en Fuentes; **[E]** = estimación propia; **[R]** = reportado por jugadores en reseñas o wikis (plausible, no confirmado por el desarrollador); **[S]** = solo en resumen de búsqueda cuya página no pude abrir.

## Resumen ejecutivo

- El núcleo de comodines del género es estable: **Deshacer, +1 recipiente, Pista y Reiniciar** aparecen en todos los líderes (IEC Global, Vnstart, ICEBEAR, SortPuz, Magic Sort, Flyfox). Barajar, revelar ocultos, saltar y autocompletar son diferenciadores o nicho. [V]
- **Deshacer** trae cupo gratuito por nivel (3-5) y se recarga con anuncio (+3 o +5) o moneda. La queja más repetida: un anuncio de 30 s por 5 deshacer es mal trato; las apps "sin spam" presumen 999 deshacer gratis. [V]
- **+1 recipiente** es el comodín que más dinero mueve: Magic Sort lo ofrece al quedarte bloqueado por **900 monedas** (~US$1,79 al precio del pack de 1.000) y Naavik atribuye a ese "pagar para continuar y conservar la racha" la mayoría de sus ingresos IAP. [V]
- **Pista** se vende en packs pequeños (3 por US$0,99; 10 por US$2,99) y por anuncio; algunas apps venden como "pista" un autocompletado del nivel, destructivo para la retención. [V]
- **Reiniciar** es gratis e ilimitado en todos los títulos revisados; cobrarlo sería anómalo. [V]
- **Barajar/Reorganizar colores** existe pero es minoritario: Magic Sort (Shuffle), Flyfox (shuffle), Get Color (packs de 10 usos por US$0,99). [V]
- **Revelar colores ocultos** no aparece como comodín explícito en ningún líder; los niveles con capas ocultas (Magic Sort ~nivel 100, Liquid Logic "Hidden Layer Mode") se resuelven con deshacer y +1 recipiente. Es un hueco que Amazon Fill puede ocupar con un comodín temático. [V/E]
- **Saltar nivel** casi no se ofrece de forma visible; cuando existe se condiciona (varios niveles entre saltos). Las apps "justas" lo citan como símbolo de monetización abusiva. [V]
- Economía típica: 5-30 monedas por nivel y comodines de 30-1.000. Hexa Sort (~5/nivel vs. 100 por un swap = 20 niveles) y Screw Jam (3 huecos extra de 225 a 550 monedas con ~30/nivel fijo) acumulan reseñas negativas: la relación "niveles para pagar un comodín" debe quedar entre 1 y 10. [R/E]
- Rewarded: eCPM EE. UU. ~US$15 (Mistplay); 9 de 10 jugadores interactúan y quienes lo hacen compran 4x más (Unity); Pangle recomienda 5-8 rewarded por 20-50 min de sesión. [V]
- Placements rewarded canónicos en puzzle (PocketGamer.biz): multiplicador post-nivel (x2-x20), moneda extra con tope 1-2/día, revivir, booster/pista, vida extra/ruleta. [V]
- Interstitials: los líderes retrasan el primero hasta el **nivel 22-24** (Magic Sort); las apps "justas" hasta el nivel 23 y 7 días. La queja dominante en IEC y otros: "anuncio entre cada nivel". [V]
- Live-ops ya probados: ruleta diaria (Screw Jam: 2 giros/día vía anuncio), ruleta cada 10 victorias desde el nivel 35 (Spring Sort), cofres (Nuts Puzzle), alcancía US$4,99 (Hexa Sort, Spring Sort), racha con 300 monedas por 3 victorias (Magic Sort, reportado). [V/R]
- Recomendación para Amazon Fill: 6 comodines (Pororoca = deshacer, Cuia extra = +1 recipiente, Guacamayo guía = pista, Aguacero = barajar, Ojo de Jaguar = revelar ocultos, Canoa rápida = saltar condicionado) más Reiniciar gratis y x2 de cosecha por anuncio. Detalle de costos en la sección final.

## 1. Contexto del subgénero (2025-2026)

| Dato | Valor | Fuente / estatus |
|---|---|---|
| Ingresos categoría "sort" antes de 2023 | < US$2 M/año | Deconstructor of Fun, feb-2026 [V] |
| Hexa Sort IAP mensual | pico ~US$4 M; estable ~US$1,5 M | Deconstructor of Fun [V] |
| Magic Sort | > US$4 M brutos en 2024 (lanzado jun-2024); US$80-100 K/día reportados a los pocos meses | Naavik; Gamigion [V] |
| Retención típica water sort | D1 40-50 %, D30 < 10 %; Hexa Sort D30 > 15 % | Naavik [V] |
| Top-50 grossing 2025 | Magic Sort y Screwdom escalaron al top 50 con el "playbook hybrid-casual" (core simple + IAP + live-ops + ads) | GameRefinery dic-2025 [V] |
| Puzzles hybrid-casual Q1-2025 | US$87 M (+67 % interanual); sort puzzle 5,6x interanual | AppMagic [S] — la página no se pudo abrir; tratar como no verificado |
| Fricción de dificultad | se intensifica desde el nivel 20-25 para empujar boosters | AppMagic [S] / coincide con Screw Jam "tras 20 niveles" [R] |

Lectura: el género pasó de "ads puros" a mixto IAP+IAA; la palanca que lo hizo posible fue convertir el bloqueo del nivel en un momento de venta (comodín de rescate), no añadir más comodines.

## 2. Catálogo de comodines observados

| Comodín | Qué hace | Dónde se vio | Observaciones |
|---|---|---|---|
| Deshacer | Revierte el último vertido | Todos | 3-5 gratis/nivel [R]; recarga +3/+5 por anuncio o moneda; packs IAP US$0,99-2,99 (Vnstart, Liquid Logic) [V] |
| +1 recipiente | Añade un recipiente vacío por el resto del nivel | IEC, Vnstart, ICEBEAR, Flyfox, Magic Sort, Rolling Panda | 1 por nivel vía anuncio; Magic Sort lo vende como "continuar" a 900 monedas [V/R] |
| Pista | Muestra el siguiente vertido correcto (solver) | IEC, Vnstart, Liquid Logic, Ball Sort Master, Get Color | Packs: 3 por US$0,99-1,99; 10 por US$2,99; 50 por US$9,99 [V] |
| Reiniciar | Vuelve al estado inicial | Todos | Gratis e ilimitado; Magic Sort cambia el tablero al reiniciar (queja) [R] |
| Barajar / Reorganizar | Redistribuye capas sin ordenar manteniendo solubilidad | Magic Sort, Flyfox, Get Color (10 usos US$0,99) | Minoritario; alternativa a reiniciar cuando ya se avanzó [V] |
| Revelar colores ocultos | Destapa capas "?" | Ningún líder como comodín explícito | Niveles ocultos sí existen (Magic Sort ~nivel 100; Liquid Logic Hidden Layer Mode; Flyfox "blind bags") [V] |
| Saltar nivel | Pasa al siguiente sin resolver | Raro; condicionado a varios niveles entre saltos [S] | Las apps "justas" lo citan como práctica a evitar [V] |
| Movimientos extra / Continuar | Seguir tras bloqueo | Magic Sort (900 monedas); Hexa Sort (+3 moves) | En water sort el "fallo" es quedarse sin vertidos válidos, no sin movimientos [V/R] |
| Autocompletar | Resuelve el nivel entero | Get Color (su "hint") | Mata la retención; no recomendado [V/E] |
| Congelar tiempo | Pausa cronómetro | Solo modos cronometrados [S] | Los líderes son sin tiempo; irrelevante al lanzamiento |
| Imán / Selector de color | Mueve todas las capas de un color | No visto en water sort; análogos Hammer (Hexa Sort, Screw Jam) | Equivalente posible: "extraer un color" [E] |
| x2 recompensa | Duplica monedas del nivel por anuncio | Hexa Sort, Spring Sort, Screw Jam | Placement rewarded de mayor adopción [V] |
| Vida extra | Repone intentos | Magic Sort (5 vidas, anuncio por vida); Hexa Sort (5, 30 min/vida) | Fricción adicional; opcional [R] |

## 3. Fichas por juego

### 3.1 Water Sort Puzzle — IEC Global (Australia)
- 4,7★/324 K (EE. UU.); 4,8★/17 K (México). Reiniciar gratis, sin tiempo ni penalización. Comodines: Deshacer, Pista, Botella extra. Reseña: un anuncio por 5 deshacer es "demasiado poca ayuda para la interrupción". [V]
- Moneda dura: **Diamantes**. IAP: Diamond 1-6 de US$1,99 a US$99,99, No Ads US$2,99, Premium US$4,99, Revival Pack US$2,99. Su Ball Sort Color Water Puzzle (4,7★/69 K) vende **membresía Silver** semanal US$2,99 / mensual US$9,99 / semestral US$49,99. [V]
- Tarifas en diamantes por comodín: no verificadas. Los clones de GitHub que describen "3 deshacer gratis, anuncio = +3 deshacer / +1 pista / +100 monedas" son plantillas, no el juego oficial. [E]

### 3.2 Water Color Sort — Vnstart LLC
- 4,5★/29 K; 2.000+ niveles. IAP: 3 pistas US$0,99; 10 pistas US$2,99; Undo package 1 US$0,99 y 3 US$2,99; Booster Combo US$2,99 y US$6,99; Remove ads US$1,99; VIP US$4,99. Gemas por nivel canjeables por pistas y deshacer [S]. [V]

### 3.3 Liquid Sort Puzzle — ICEBEAR JSC
- 4,6★/419; 2.000+ mapas. IAP solo de moneda: 5.000 por US$1,99; 20.000 por US$5,99; 100.000 por US$9,99; Remove ads US$2,99. Reseñas: anuncios de 5 s entre niveles y de 30 s para más deshacer; anuncio = 5 deshacer o una botella extra. [V]

### 3.4 SortPuz — Matchstick Castle
- 4,9★/48 K. IAP: Remove ads US$2,99; packs US$4,99-9,99; diamantes US$0,99-9,99. Queja: desbloqueos que exigen varios anuncios seguidos. [V]

### 3.5 Magic Sort! — Grand Games (Turquía)
- 4,7★/669 K, #4 Casual EE. UU. IAP: Starter US$0,99; 1.000 monedas US$1,99; 5.000 US$7,99; Endless Portal US$5,99; bundles US$4,99-19,99. [V]
- Comodines "Wizard's Gear": Deshacer, Shuffle, +1 botella, elegidos antes del nivel (queja). Al bloquearte, oferta "+1 more bottle" por **900 monedas** (reseñas App Store e iofreeonline coinciden). [V/R]
- Dificultad: Normal/Hard/Super Hard; colores ocultos y botellas tapadas hacia el nivel 100; tras el primer fallo en el nivel 26 el reintento quita un color (Naavik). Interstitials desde el nivel 22-24. [V]
- 5 vidas (anuncio por vida; equipos comparten vidas); cartas coleccionables; eventos cada ~2 meses; 3 clasificaciones. Racha: 300 monedas por 3 victorias seguidas (reportado). [V/R]

### 3.6 Hexa Sort — Lion Studios Plus
- 4,7★/219 K. IAP: Coin Pack US$1,99/7,99/14,99; Piggy Bank US$4,99; Safety Net US$4,99; Spin Wheel US$1,99; Hammer Bundle, Refresh Bundle, Free Lives Bundle y Tiny Hex Pack US$9,99. [V]
- Comodines: Martillo (elimina una pila), Swap, Refresh, Deshacer, +3 movimientos. Reportado: Swap 50 → 100 monedas; Refresh 10 → 30; ~5 monedas/nivel; x2 por anuncio; 5 vidas, 30 min por vida; sin anuncios 30 días por US$10. [R]
- Quejas: precios al alza sin subir recompensas, anuncios tras cada partida, rivales de liga percibidos como bots. [V]

### 3.7 Otros títulos revisados
- **Water Color Sort: Liquid Logic**: 5.000+ niveles; Undo packs US$0,99/1,99/2,99; Hint packs US$0,99/2,99/8,99; 1.000 monedas US$0,99; Hidden Layer Mode con 365 niveles diarios. [V]
- **Water Sort! 3D Color Match Puz** (Flyfox): 4,9★/13 K; único IAP Remove Ads US$3,99; colores ocultos y "blind bags"; deshacer/shuffle/hacer espacio; "2-3 anuncios al día". [V]
- **Water Sort – Color Puzzle Game** (文亮 刘): 5 deshacer por nivel; agotados, anuncio o esperar al siguiente nivel; anuncios desde el nivel 21. [V/R]
- **Get Color**: Hints 3/US$1,99, 10/US$2,99, 50/US$9,99; Rearrange Color 10 usos US$0,99 y 20 usos US$1,99; su pista "juega el nivel completo". [V]
- **Screw Jam** (Rollic): Extra Slot, Hammer (revela capa inferior), Double Toolbox. Precios reportados feb-2025: 3 huecos 225 → 550 monedas, 3 martillos ~350 → 1.000, 3 dobles ~375 → 750; 10-30 monedas/nivel; ruleta con 2 giros gratis/día vía anuncio (~20 monedas); muros desde el nivel ~240. [V/R]
- **Nuts Puzzle: Sort by Color**: stock inicial de 10 deshacer y 3 pernos extra; más vía monedas, anuncios, cofres (con o sin llave) y ruleta. [V]
- **Spring/Slinky Sort** (Moonee): ruleta cada 10 victorias desde el nivel 35; recompensas diarias crecientes; Piggy Bank US$4,99; Revive Pack US$3,99; pase de temporada. [V]
- **Super Water Sort / Sorted** (contra-ejemplo "sin spam"): 999 deshacer gratis, sin moneda premium, energía ni skip de pago; sin anuncios forzados 7 días, luego mínimo 13 niveles entre anuncios y nunca antes del nivel 23; 3.000-5.000 tableros verificados por BFS; Remove Ads US$2,99. [V]

## 4. Economía de la moneda blanda alrededor de los comodines

| Juego | Monedas por nivel | Costo comodín | Niveles para pagar uno | Estatus |
|---|---|---|---|---|
| Hexa Sort | ~5 (x2 con anuncio) | Swap 100; Refresh 30 | 20 / 6 (10 / 3 con x2) | [R]; jugadores lo califican de insuficiente |
| Screw Jam | 10-30, fijo por dificultad | 3 huecos extra 550; 3 martillos 1.000 | 18-55 / 33-100 | [R]; quejas por subida de precios |
| Magic Sort | 300 por racha de 3 victorias (reportado) | Continuar/+1 botella 900 | ~9 niveles, o US$1,79 vía pack de 1.000 | [R]/[V] |
| Nuts Puzzle | n/d | Inventario: 10 deshacer + 3 pernos de inicio | n/d (modelo de stock, no de precio) | [V] |
| Water Sort IEC | n/d (diamantes) | n/d | n/d | no verificado |
| Super Water Sort | sin moneda | 0 | 0 | [V] |

Patrones: racha con bonus (Magic Sort, 300 monedas por 3 victorias) y monetización de "no perder la racha" [R/V]; cofres con/sin llave (Nuts Puzzle) y alcancía US$4,99 (Hexa Sort, Spring Sort) [V]; ruleta diaria con 2 giros gratis vía anuncio (Screw Jam), cada 10 victorias desde el nivel 35 (Spring Sort) o vendida a US$1,99 (Hexa Sort) [V]; recompensas diarias crecientes multiplicables por video (Spring Sort, Liquid Logic) [V]. Subir precios de comodines con recompensas fijas (Hexa Sort, Screw Jam) es la causa más citada de reseñas de 1-2 estrellas sobre economía. [V]

## 5. Anuncios recompensados: placements, límites y benchmarks

| Métrica / práctica | Valor | Fuente |
|---|---|---|
| eCPM rewarded EE. UU. | ~US$15,15 (vs. interstitial US$12,65) | Mistplay [V] |
| Jugadores que interactúan con rewarded | 9 de 10; prefieren rewarded 4:1 frente a interstitial | Digiday / Mistplay [V] |
| Efecto sobre IAP | 4x más probabilidad de comprar; +326 % gasto medio tras interactuar | Unity vía Mistplay [V] |
| Tope recomendado (casual) | 5-8 rewarded por cada 20-50 min de juego | Pangle [V] |
| Engagement placement "movimientos extra" | 30,5 % | resumen de búsqueda [S] |
| Topes reales en juegos top | War and Wit: 10 anuncios/día; June's Journey: 1 gratis + 1 por anuncio al día; moneda premium cada 12 h (Dragon City) | GameRefinery [V] |
| Placements canónicos puzzle | multiplicador post-nivel (x2-x20), moneda extra 1-2/día con cuenta atrás, revivir, booster/pista, vida extra y ruleta misteriosa | PocketGamer.biz [V] |
| Ruleta rewarded | Two Dots: giro con +1/+3/+5 movimientos o boosters | GameRefinery [V] |

## 6. Qué comodines gustan y cuáles frustran

Mejor valorados: **Deshacer generoso** (argumento central de las apps "justas" y queja principal cuando se raciona); **+1 recipiente en el momento del bloqueo** (Naavik y Gamigion lo señalan como motor IAP de Magic Sort: rescata tiempo invertido, como "+5 moves" en Candy Crush); **x2 de monedas post-nivel** (percibido como trato justo, presente en toda la muestra hybrid-casual); **pista con solver** (Ball Sort Master la vende como "la función más única"). [V]

Generan frustración: anuncio de 30 s por 5 deshacer (IEC, ICEBEAR) [V]; subir precios sin subir recompensas (Hexa Sort, Screw Jam) [V/R]; elegir boosters antes del nivel y no poder usarlos tras perder sin pagar 900 monedas (Magic Sort) [V]; pista que resuelve el nivel entero (Get Color) [V/E]; cadenas de anuncios para un desbloqueo (SortPuz) y anuncios entre cada nivel (IEC MX, Hexa Sort) [V]; reiniciar que cambia el tablero (Magic Sort) [R].

## 7. Tabla comparativa de comodines por juego

Leyenda: G = cupo gratis; A = por anuncio recompensado; M = por moneda blanda; $ = pack IAP; — = no observado; ? = no verificado.

| Comodín | Water Sort (IEC) | Water Color Sort (Vnstart) | Liquid Sort (ICEBEAR) | SortPuz | Magic Sort | Hexa Sort | Flyfox 3D | Screw Jam |
|---|---|---|---|---|---|---|---|---|
| Deshacer | G?/A(5)/$Revival | G/A/$ (packs) | A(5)/M | G/A | M/$ (pre-nivel) | M | G/A | — (hueco extra cumple rol) |
| +1 recipiente | A | A/$ combo | A | A | M 900 (fail-state)/$ | — | A ("make space") | M 550/3 (Extra Slot) |
| Pista | A/M diamantes | $ 3/0,99; 10/2,99 | — | A | — | — | — | — |
| Reiniciar | G | G | G | G | G (tablero cambia) | G | G | G |
| Barajar | — | — | — | — | M | M (Refresh 30) | A/M | — |
| Revelar ocultos | — | — | — | — | — (niveles ocultos sin comodín) | — | — | M (Hammer revela capa) |
| Saltar nivel | — | — | — | — | — | — | — | — |
| Movimientos extra / continuar | $ Revival Pack | — | — | — | M 900 | M (+3 moves) | — | — |
| x2 recompensa | ? | ? | ? | ? | A | A | A | A |
| Vidas | — | — | — | — | 5, A | 5, 30 min | — | ? |
| Ruleta / cofre / alcancía | — | — | — | — | eventos, cartas | Spin $1,99, Piggy $4,99 | eventos | ruleta 2/día, cofres |

## Implicaciones para Amazon Fill

**Principio rector**: copiar el set probado (Deshacer, +1 recipiente, Pista, Reiniciar gratis, x2 por anuncio), añadir dos diferenciadores que el género no cubre (Revelar ocultos y Barajar con tema) y ofrecer Saltar solo condicionado. Evitar autocompletar, congelar tiempo (sin modo cronometrado) y vidas al lanzamiento.

### Catálogo propuesto (6 comodines + 2 gratuitos)

| # | Nombre temático | Función | Desbloqueo | Gratis por nivel | Moneda blanda ("semillas") | Anuncio | Dónde en la UI | Por qué el nombre comunica la función |
|---|---|---|---|---|---|---|---|---|
| 1 | **Pororoca** | Deshacer 1 vertido (acumulable) | Nivel 1 | 3 | 30 por +3 | +3 por anuncio, sin tope pero con cooldown 60 s | Barra inferior, izquierda (posición estándar del género) | La pororoca es la ola amazónica que hace que el río fluya hacia atrás: literalmente "el agua vuelve". |
| 2 | **Cuia extra** | +1 recipiente vacío por el nivel | Nivel 8 (cuando hay ≥6 recipientes) | 0 | 150 | 1 por nivel vía anuncio; segunda solo con semillas | Barra inferior, centro; también como modal de rescate al quedar sin vertidos válidos ("¿Una cuia más?") | La cuia (calabaza ahuecada) es el cuenco amazónico cotidiano; "una cuia más" se entiende como "un recipiente más". |
| 3 | **Guacamayo guía** | Muestra el siguiente vertido correcto (solver) | Nivel 12 | Stock inicial 3 | 100 | 1 por anuncio, tope 3/día con cuenta atrás | Barra inferior, derecha | El guacamayo "habla" y vuela por encima: ve el tablero y te dice el siguiente paso. |
| 4 | **Aguacero** | Reorganiza las capas no ordenadas conservando la solubilidad | Nivel 30 | 0 | 200 | 1 por anuncio, tope 2/día | Barra inferior (aparece desde el nivel 30) | La lluvia torrencial remueve el río y mezcla los colores: barajar sin perder los recipientes ya completos. |
| 5 | **Ojo de Jaguar** | Revela todas las capas ocultas del nivel | Nivel 80 (inicio de niveles con capas ocultas, en línea con Magic Sort ~100) | 0 | 120 | 1 por anuncio, tope 2/día | Botón flotante solo en niveles con "?" | El jaguar ve en la oscuridad de la selva: destapa lo que el jugador no puede ver. |
| 6 | **Canoa rápida** | Salta el nivel actual | Solo tras 3 fallos/reinicios en el mismo nivel y ≥10 niveles desde el último salto | 0 | 300 | 1 por anuncio, tope 1/día | Modal post-fallo, debajo de "Cuia extra" | La canoa te lleva río abajo al siguiente tramo; condicionarla evita la percepción de "skip de pago". |
| — | **Volver al puerto** | Reiniciar el nivel con la misma configuración | Nivel 1 | Ilimitado | 0 | — | Esquina superior derecha | Reiniciar debe ser gratis y determinista (lección de Magic Sort). |
| — | **Cosecha doble** | x2 semillas del nivel | Nivel 5 | — | — | 1 anuncio post-nivel | Pantalla de victoria | Duplicar la "cosecha" de frutas amazónicas. |

Estimaciones [E] de economía inicial para calibrar en soft launch: 20 semillas por nivel (40 con Cosecha doble); así Pororoca x3 cuesta 1,5 niveles, Guacamayo 5, Ojo de Jaguar 6, Cuia 7,5, Aguacero 10 y Canoa 15. Esto mantiene todo entre 1 y 15 niveles, lejos de los 20-100 niveles de Hexa Sort y Screw Jam que generan reseñas negativas, y por debajo de los ~9 niveles de Magic Sort para el rescate (que convierte porque es raro, no barato). Nunca subir precios sin subir recompensas en la misma actualización.

### Monetización asociada (según lo observado)
- IAP de arranque: Remove Ads US$2,99 (rango observado US$1,99-4,99); packs de semillas US$0,99/1,99/4,99/9,99; starter "Paquete Río" US$0,99; combo de comodines "Kit del Chamán" US$2,99 y US$6,99 (calco de los Booster Combo de Vnstart); "Rescate" US$2,99 (equivalente al Revival Pack de IEC). Membresía semanal (IEC: US$2,99) y alcancía US$4,99 para una fase posterior.
- Interstitials: ninguno antes del nivel 22 y nunca entre niveles consecutivos en los primeros 50; tope rewarded 6 por 30 min de sesión (rango Pangle 5-8/20-50 min).
- Live-ops ligados a comodines: ruleta diaria "Rueda del río" (1 giro gratis + 1 por anuncio), racha "Crecida" (300 semillas por 3 victorias seguidas), cofre "Canasta" cada 10 niveles con comodines.
- Ofrecer el comodín en el momento del bloqueo, no antes del nivel (la pre-selección es la queja más clara de Magic Sort); aliviar un color tras fallos repetidos (Magic Sort lo hace en el nivel 26) reduce churn sin regalar el comodín.

## Preguntas abiertas

1. Tarifas en diamantes de los comodines de IEC Global: no verificadas; requiere sesión de juego o datos de SensorTower/AppMagic.
2. Cifras AppMagic Q1-2025 (US$87 M, 5,6x) y umbral "nivel 20-25": solo en resumen de búsqueda.
3. Monedas por nivel en Magic Sort (300 por racha de 3) y Hexa Sort (~5): solo reportes de jugadores.
4. ¿Vidas/corazones sí o no? Suben IAP, pero rompen la promesa "relax" del género.
5. ¿Moneda única (semillas) o doble (semillas + diamantes como IEC/SortPuz)?
6. Nombre del recipiente: "cuia" es portugués; en español se dice "totuma"/"tutuma". Decidir por mercado o inventar un nombre.
7. Viabilidad técnica de "Aguacero" (barajar conservando solubilidad) con el generador elegido; Super Water Sort verifica tableros por BFS, lo que sugiere que es factible.
8. Políticas de Google Play sobre ruleta/cofres (loot boxes) por país, especialmente Brasil.

## Fuentes

- https://www.deconstructoroffun.com/blog/2026/2/6/sort-puzzles-how-a-new-subgenre-is-born
- https://naavik.co/digest/why-peak-games-investors-backed-grand-games/
- https://www.gamigion.com/magic/
- https://friendlygamedev.com/game-reviews/magic-sort/
- https://www.iofreeonline.com/IOS/game/Magic-Sort.html
- https://apps.apple.com/us/app/magic-sort/id6499209744
- https://www.gamerefinery.com/mobile-game-market-review-december-2025/
- https://appmagic.rocks/blog/hybridcasual-q1-2025/?hl=en (no se pudo abrir; cifras tomadas del resumen de búsqueda)
- https://apps.apple.com/us/app/water-sort-puzzle/id1514542157
- https://apps.apple.com/mx/app/water-sort-puzzle/id1514542157
- https://apps.apple.com/us/app/ball-sort-color-water-puzzle/id1550569446
- https://unity.com/resources/iec-games-water-sort-puzzle-offerwall
- https://apps.apple.com/us/app/water-color-sort/id1560591638
- https://apps.apple.com/us/app/liquid-sort-puzzle/id1536906582
- https://apps.apple.com/us/app/sortpuz-water-sort-puzzles/id1560298214
- https://apps.apple.com/us/app/water-color-sort-liquid-logic/id1670852519
- https://apps.apple.com/app/id6758611499 (Water Sort! 3D Color Match Puz, Flyfox)
- https://apps.apple.com/us/app/water-sort-color-puzzle-game/id1568411880
- https://apps.apple.com/us/app/water-sort-color-sort-puzzle/id6471082531 (Get Color)
- https://apps.apple.com/us/app/hexa-sort/id6463127238
- https://apps.apple.com/us/app/hexa-sort/id6463127238?see-all=reviews
- https://writerparty.com/party/hexa-sort-walkthrough-tips-cheats-and-strategy-guide/
- https://rollic.helpshift.com/hc/en/10-screw-jam/faq/256-what-are-the-power-ups-boosters/?p=web
- https://www.iofreeonline.com/IOS/game/Screw-Jam.html
- https://www.bubbleshooter.net/game/nuts-puzzle-sort-by-color/
- https://apps.apple.com/app/id6747435794 (Spring/Slinky Sort, Moonee)
- https://superwatersort.com/
- https://sortedballsort.com/
- https://happyvials.com/guides/how-to-play-water-sort-puzzle
- https://www.pocketgamer.biz/rewarded-video-placements-to-boost-a-puzzle-games-revenue/
- https://www.gamerefinery.com/rewarded-video-ad-placements-interesting-implementations-from-across-the-market/
- https://business.mistplay.com/resources/rewarded-ads-stats/
- https://www.pangleglobal.com/resource/27805
- https://play.google.com/store/apps/details?id=ballsort.ball.sort.puzzle.game.colorpuzzle.puzzlegame.matchpuzzle.colorgame.master (ruleta diaria; solo resumen de búsqueda)
- https://apps.apple.com/us/app/water-sort-color-pouring-game/id1662420900 (Rolling Panda; solo resumen de búsqueda)

## Complemento 2: Árbol de decisión del momento de bloqueo (rescate)

Añadido 2026-10-05. Reconcilia los cuatro flujos de los docs 01 (regla 13: tarjeta Deshacer / Afluente extra / Reiniciar), 03 (modal "¿Una cuia más?"), 04 (`af_rescue` 4,99, máx. 1/día, rewarded primero en tier-3) y 05 (semillas o rewarded la primera vez, precio después) en un único árbol implementable. Convención: [V] verificado, [E] estimación o decisión de diseño, [NV] no verificable en esta sesión.

### C2.1 Qué hace el género en el fail-state

| Juego | Oferta en el bloqueo | Vía gratuita en la tarjeta | Repetición | Estatus |
|---|---|---|---|---|
| Magic Sort | "+1 bottle" por 900 monedas al detectar que no queda vertido legal; rechazar = −1 vida, reinicio y pérdida de racha; sin saldo → tienda (1.000 por 1,99 USD) | Ninguna: el único rewarded da vidas; Undo/Shuffle guardados no se pueden usar | Sin tope conocido mientras haya monedas | [V] Naavik, Gamigion, FriendlyGameDev; tope [NV] |
| Candy Crush Saga | "Extra Moves" +5 por 10 lingotes (10 lingotes = 1,99 USD; 0,99 en oferta); el continue es la 2.ª compra más frecuente en EE. UU. | Lingotes ganados en eventos | Cada continue sucesivo es una compra distinta | [V] wiki Candy Crush |
| Color Block Jam (Rollic) | Fail offer 4,99 USD: >20 % del IAP total y 27 % de las primeras compras (el pack de 1,99 capta ~40 %) | — | [NV] | [V] AppMagic, 18-mar-2026 |
| Pixel Flow (sort, Loom/Scopely) | Fail offer 5,99 USD: ≈33 % del IAP y 34 % de las primeras compras; ~14 M de 44,5 M USD | — | [NV] | [V] AppMagic |
| Screwdom / Rollic (Gecko Out, Knit Out, Drop Away) / Coin Sort / Happy Screw Trip | "Keep up" 4,99 = 14,5 % del IAP y 17 % de primeras compras; Rollic >25 %; Coin Sort 19,4 %; revive pack ~5 % (sin presión de vidas) | — | [NV] | [V] AppMagic |
| Two Dots | Rewarded → ruleta con +1/+3/+5 movimientos o booster; si sale booster, continuar sigue costando moneda | Sí (anuncio) | Odds no publicadas | [V] GameRefinery |
| Water Sort IEC / ICEBEAR / Guru | +1 tubo por anuncio (IEC lo rebajó a 1/4 de tubo); Revival Pack 2,99 USD (IEC) | Sí; Guru exige vídeos de 1:20-3 min | 1 tubo por anuncio y nivel [R] | [V] fichas; docs 01/05 |

Lecturas: (1) ningún líder publica topes por nivel o por día; los de C2.3 son decisión propia. (2) El rescate es la compra que **convierte al primer pagador** (17-34 % de primeras compras): necesita versión IAP, no solo semillas. (3) Sin presión de vidas la oferta pesa poco (Happy Screw Trip, 5 %); nuestra presión es la racha "Crecida" y el tiempo invertido. (4) Tener ruta gratuita en la tarjeta (Two Dots, IEC) no anula la venta; no tenerla (Magic Sort) es la queja estructural del líder.

### C2.2 Árbol unificado [E]

Disparador: el motor detecta "sin movimientos legales" o "solo movimientos fútiles" (01, regla 13). Se abre **una sola tarjeta**, sin cuenta atrás, con hasta tres opciones grandes y "Volver al puerto" (reiniciar) como enlace gratuito al pie. Entradas: `geo_tier` (T1 = EE. UU., Canadá, Europa occidental, AU/JP/KR; T3 = LatAm salvo Chile/Uruguay, como en 04), `fails_level`, `saldo` de semillas, `no_ads`, `rewarded_ready` (anuncio precargado), `cuia_ad_usada` (en este nivel), `rescue_hoy`.

Regla 0 (todas las ramas): si hay Pororoca en stock, la primera opción es **Deshacer gratis**; con stock 0, "Deshacer: +3 por anuncio o 30 semillas". Corrige la queja n.º 1 de Magic Sort.

| Rama | Opción 1 (CTA) | Opción 2 | Opción 3 | Nota |
|---|---|---|---|---|
| T3 · fallo 1 | Cuia extra por **anuncio** (si `rewarded_ready` y no usada) | Cuia por 150 semillas | Deshacer | Sin IAP en el primer bloqueo (04, 05) |
| T3 · fallo 2 | Cuia por semillas (2.ª del nivel: 300) | **Rescate** 4,99 local si `rescue_hoy` = 0 | Deshacer | Primera aparición del IAP (04) |
| T3 · fallo ≥3 | Canoa rápida por anuncio (1/día) o 300 semillas si ≥10 niveles desde el último salto | Rescate si no mostrado hoy | Cuia por semillas | Reintento con −1 color, como Magic Sort en el nivel 26 [V hecho / E uso] |
| T1 · fallo 1 · saldo ≥150 | Cuia por 150 semillas | Cuia por anuncio (si fill) | Deshacer | Moneda blanda primero; el rewarded se reserva para x2 |
| T1 · fallo 1 · saldo <150 | **Rescate** 4,99 si `rescue_hoy` = 0 | Cuia por anuncio | Deshacer | "Pago primero" (04) solo sin saldo |
| T1 · fallo ≥2 | Rescate si no mostrado hoy; si no, semillas | Cuia por anuncio | Canoa desde el 3.er fallo | |
| Comprador No-Ads | Como T1 con el anuncio siempre en 2.ª posición | | | No-Ads conserva rewarded (04) |
| Sin fill (`ERROR_CODE_NO_FILL` = 3 / `ERROR_CODE_MEDIATION_NO_FILL` = 9) | Ocultar el botón de anuncio; cuia a **100 semillas** (precio de disculpa) | Rescate según geo/fallo | Deshacer | Reintentar carga con backoff exponencial hasta 64 s (MAX); precargar al iniciar el nivel y renovar cada hora (AdMob) [V SDK / E precio] |

### C2.3 Topes [E, con anclas]

| Vía | Por nivel | Por día | Ancla |
|---|---|---|---|
| Deshacer en la tarjeta | Ilimitado con stock; recarga +3 por anuncio, cooldown 60 s | Dentro del tope global rewarded (5/día en 04; Pangle 5-8 por 20-50 min) | IEC/ICEBEAR: 5 undos y luego anuncio |
| Cuia por anuncio | 1 | Tope global rewarded | IEC: 1 tubo por anuncio |
| Cuia por semillas | 2 (150, luego 300) | Sin tope | Candy Crush multi-continue; Magic Sort sin tope [NV] |
| Rescate IAP 4,99 | 1 impresión | 1 (04); rechazado 2 días seguidos → ocultar 48 h | Pixel Flow / CBJ no publican topes |
| Canoa rápida | 1, requiere ≥3 fallos | 1 por anuncio; con semillas, ≥10 niveles entre saltos | 03 |
| Reiniciar | Ilimitado | Ilimitado | Todos los líderes |

### C2.4 Comodines ya consumidos al reiniciar [E]

Reiniciar restaura el **mismo tablero** (determinista; Magic Sort lo baraja y recibe quejas). La cuia extra obtenida por anuncio, semillas o Rescate **persiste todo el nivel**, incluidos reinicios y cierres de la app (se guarda en el estado del nivel); sin esto el rescate deja de "cumplir". Los 3 Pororoca gratis del nivel se restauran al reiniciar; los comprados viven en inventario y no se reembolsan. Guacamayo y Aguacero son consumibles sin reembolso. Las capas reveladas por Ojo de Jaguar siguen visibles tras reiniciar el mismo nivel. Los 5 deshacer y las 800 semillas del Rescate van al inventario y sobreviven al nivel. Contraejemplo: en Magic Sort los boosters elegidos antes del nivel se pierden al fallar [V].

### C2.5 Registro de la conversión por rama

- Nivel: `level_start` / `level_end(level_name, success=false)` (Firebase) y evento de progresión `Fail` con `progression01=pais`, `progression02=nivel_N`, `progression03=intento_n` (GameAnalytics; lo dinámico va en `value`).
- Tarjeta: `rescue_shown {level, attempt, geo_tier, saldo, no_ads, rewarded_ready, order="ad|seeds|iap", variant}`; `rescue_choice {choice ∈ undo, cuia_ad, cuia_seeds, rescue_iap, canoa_ad, canoa_seeds, restart, dismiss; position; ms}`; `rescue_outcome {result ∈ reward_received, no_fill, purchase_ok, purchase_cancel}`; `level_end(success=true, rescued=true)`.
- Anuncios: evento `ads` con `adAction` show / failed_show / reward_received, `adType=rewarded_video`, `adPlacement=rescue_cuia`, `adFailShowReason` no_fill / offline (GameAnalytics).
- Economía: `spend_virtual_currency(virtual_currency_name=semillas, item_name=cuia_rescue, value=150)`; `purchase(transaction_id, items=af_rescue)`.
- KPIs: bloqueos por nivel; aceptación por rama y geo; conversión de `af_rescue` (referencia 27-34 % de primeras compras en CBJ/Pixel Flow); "rescate → victoria" objetivo >85 %; D1/D7 de rescatados vs. no rescatados. Remote Config: `rescue_order_t1`, `rescue_order_t3`, `rescue_iap_daily_cap`, `rescue_fail_threshold`, `cuia_seed_price`, `cuia_second_price` (más `extra_tube_free_after_fails`, ya en 06). Precios locales por país vía Play Console (precio por país o plantilla) [V].

No verificable en esta sesión: topes por nivel/día de Magic Sort, Pixel Flow y Color Block Jam; continue de Royal Match (900 monedas, dato de mercado no re-verificado); tabla de precios mínimos por país de Google Play (la página no renderizó la tabla); si IEC conserva el tubo extra al reiniciar.

Fuentes del Complemento 2:
- https://appmagic.rocks/blog/fail-offers (leído en navegador; artículo del 18-03-2026)
- https://naavik.co/digest/why-peak-games-investors-backed-grand-games/
- https://www.gamigion.com/magic/
- https://friendlygamedev.com/game-reviews/magic-sort/
- https://candycrush.fandom.com/wiki/Gold
- https://www.gamerefinery.com/rewarded-video-ad-placements-interesting-implementations-from-across-the-market/
- https://www.pocketgamer.biz/rewarded-video-placements-to-boost-a-puzzle-games-revenue/
- https://apps.apple.com/us/app/hexa-sort/id6463127238?see-all=reviews
- https://developers.google.com/admob/android/rewarded
- https://developers.google.com/android/reference/com/google/android/gms/ads/AdRequest
- https://support.applovin.com/en/max/android/ad-formats/rewarded-ads/
- https://docs.gameanalytics.com/events-metrics-and-filtering/event-types/progression-events
- https://docs.gameanalytics.com/events-metrics-and-filtering/event-types/ad-events
- https://firebase.google.com/docs/reference/android/com/google/firebase/analytics/FirebaseAnalytics.Event
- https://support.google.com/analytics/answer/9267735
- https://support.google.com/googleplay/android-developer/answer/1153481
- Docs internos: docs/research/01-mecanicas-genero.md (regla 13), 04-monetizacion-ads-iap.md (`af_rescue`, segmentación por tier), 05-retencion-liveops-meta.md, 09-referencia-magic-sort.md (fail flow de Magic Sort)
