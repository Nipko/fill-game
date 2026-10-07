# Generación de niveles, solvers y curva de dificultad en water-sort (Amazon Fill)

Fecha: 2026-10-05. Investigación de producto. Convención: **[V]** = dato verificado con URL en la sección Fuentes; **[E]** = estimación o inferencia propia; **[S]** = visto solo en un fragmento de buscador, no leído en la fuente completa.

## Resumen ejecutivo

- Water-sort y ball-sort son el mismo problema desde el punto de vista de la resolubilidad y ambos son **NP-completos**; hallar la solución más corta es NP-completo incluso con muchos recipientes vacíos (Ito et al., FUN 2022 / TCS 2023) **[V]**. Consecuencia práctica: con 1-2 vacíos **no hay garantía teórica de resolubilidad**; cada nivel debe demostrarse con generación inversa o con solver.
- Cota teórica: con capacidad h y n colores, ⌈(h−1)/h·n⌉ recipientes vacíos bastan para que cualquier instancia sea resoluble (para h=4 y 10 colores: 8 vacíos). Los juegos comerciales usan 1-2, es decir, operan muy por debajo de la cota, y con k=1 existen instancias irresolubles **[V]**.
- Los solvers A* con heurística de "cortes de color" (color breaks) resuelven tableros de 10-14 tubos de capacidad 4 en **milisegundos a pocos segundos** si se canonicaliza el estado (ordenar tubos). Sin canonicalización el mismo problema sube a 495k estados y 54 s para 14 tubos **[V]**.
- La heurística más fuerte documentada es `max(cortes_de_color, Σ_c (tubos_que_contienen_c − 1))`: redujo 4,6× los estados explorados frente a A* básico en un tablero de 12 colores/14 tubos (869k→210k estados, 3,6 s→0,8 s) **[V]**.
- Dos familias de generador funcionan en producción: (a) **barajado inverso** desde el estado resuelto con movimientos inversos legales, que lleva prueba de resolubilidad incorporada (laicwben2: 18/30/46 movimientos inversos para fácil/medio/difícil) **[V]**; (b) **relleno aleatorio + solver** con rechazo (requiemformemories: DFS con tope de 400k nodos + beam search de ancho 1500) **[V]**. Ambos necesitan filtros anti-trivialidad.
- La métrica que mejor predice dificultad percibida no es el tamaño del tablero sino la **"estrechez"**: número de vacíos (1 vs 2), fragmentación de colores y densidad de decisiones no forzadas. Un repo lo cuantifica con un **bot casual sin anticipación** jugado 40 veces: Hard ≥75 % de fallos, Master ≥90 % **[V]**.
- Cambiar de 2 a 1 tubo vacío es el salto de dificultad más barato y más fuerte del género; los juegos comerciales lo usan como definición de "modo difícil" ("más colores, menos tubos vacíos") **[S]**.
- Los colores ocultos convierten el puzzle en parcialmente observable; los solvers estándar no lo soportan y las herramientas que lo hacen enumeran todas las permutaciones de las capas ocultas para ofrecer solo movimientos "seguros en todas las realidades" **[V]**. Es una variante válida pero debe garantizarse que exista un movimiento seguro.
- Los líderes casuales usan **intentos (media/mediana) por nivel**, no tasa de victoria, como KPI de dificultad; King añade "time to abandon" y "time to pass" y poda periódicamente los 100 niveles menos disfrutados **[V]**.
- Patrón "diente de sierra" verificado: en Candy Crush, a cada nivel muy difícil le siguen 5-6 niveles fáciles/medios **[V]**; en Royal Match los primeros 100 niveles tienen 8 niveles "hard" (39, 45, 49, 55, 65, 75, 85, 95; ~1,6 intentos) y 5 "super hard" (59, 69, 79, 89, 99; ~2,5 intentos) con nivel normal ≈1,2 intentos **[V]**.
- Un sistema de progresión publicado por un indie propone un ciclo de 20 niveles: 12 "comodidad", 5 "reto", 2 "recuperación", 1 "hito", con el reto siempre antes del hito y límites de salto (subida ≤2,2 bandas, bajada ≤3,4) **[V]**.
- Para producir miles de niveles: generar **offline**, con semilla determinista, puntuar, y enviar en paquetes; la generación en runtime impide calificar por percentiles, rompe el progreso si cambia el código y no permite QA **[V]**. 10.000 niveles de 14 tubos caben en <1 MB **[E]**.
- La dificultad adaptativa está probada: un marco "difficulty-aware" de predicción de churn mejoró retención en A/B (KDD 2021) **[V]**; un experimento de campo con ~300k jugadores/12 semanas dio puzzles más fáciles a jugadores en riesgo de churn **[S]**. La cifra "Candy Crush redujo churn 11 %" circula en blogs pero **no pude verificarla**.
- Benchmarks de retención del género puzzle (Mistplay 2026 vía Segwise): D1 31,85 %, D7 12,18 %, D30 5,35 % **[V]**.

## 1. Fundamentos computacionales

| Resultado | Enunciado | Fuente |
|---|---|---|
| Equivalencia | Una instancia es ordenable con movimientos de bolas ⇔ lo es con movimientos de agua | Ito et al. 2022 [V] |
| Complejidad | Ambos puzzles son NP-completos | idem |
| Solución más corta | NP-completo decidir si existe secuencia de longitud ≤ t incluso si la instancia es trivialmente resoluble (muchos vacíos) | idem, Cor. 12 |
| Vacíos suficientes | k(n,h) ≤ ⌈(h−1)/h · n⌉ (Teorema 16) | idem |
| Vacíos necesarios | k(n,h) ≥ ⌈19/64 · min{n,h}⌉ (Teorema 17) | idem |
| Caso h=2 | Con k≥2 todo es resoluble en O(n); con k=1 hay instancias irresolubles | idem, Teor. 13-14 |

Lectura para producto: con capacidad 4 y 8 colores, la teoría exige hasta 6 vacíos para garantizar resolubilidad; nosotros daremos 1-2. Por tanto **cada nivel debe traer prueba de resolubilidad** (ruta inversa o solución del solver) y nunca puede generarse "a ciegas" en el cliente.

## 2. Solvers: enfoques y costo real

### 2.1 Repositorios analizados

| Repo / fuente | Algoritmo | Heurística | Estado / simetrías | Rendimiento reportado |
|---|---|---|---|---|
| hkociemba/WaterBallSortPuzzleOptimalSolver (Pascal) | Híbrido A*/BFS, óptimo | no documentada | hash 32 bits, tabla de 2^32 bits (512 MB) | <1 s puzzles comerciales; 14 colores × 6 bloques en segundos [V] |
| Azkarayan, ITB 2025 (C++) | A* con closed list por string del tablero completo | cortes de color (admisible, consistente) | **sin** canonicalización de tubos | 8 tubos: 6.980 estados/0,48 s; 11 tubos: 170.193/17 s; 14 tubos: 494.745/53,9 s (i7-10700K) [V] |
| xsieux292/water-sort-puzzle | BFS y A* | `max(h1 cortes, h2 Σ(tubos con color c − 1))` | codificación 1 char/bloque, hash cacheado | 12 colores/14 tubos: 869.495 est./3.622 ms → 210.150/788 ms; 5 colores: 5.467/24 ms → 922/5 ms [V] |
| kang_wang (dev.to, JS ~300 líneas) | A* ponderado f=g+w·h; w=2 (40k expansiones) luego w=4 (400k); BFS 300k para probar irresolubilidad | cortes + dispersión (tubos por color − 1) | tubos ordenados y unidos como clave | 5 colores 0,3 ms; 8 colores 0,4 ms; 12 colores 1,5 ms (mediana); 1-2 movimientos subóptimo [V] |
| octo/watersort (Go) | A* óptimo | cota inferior en dos pasos (colores sobre otro color + distribución de colores de fondo) | — | nivel 105: 20.637 estados, 41 movimientos, 0,158 s [V] |
| awaisnaseer124 (Unity) | A* | cortes de color ("un vertido elimina a lo sumo un corte") | fusiona tableros que difieren solo en orden de tubos | nivel más difícil publicado probado óptimo en ~35.000 estados [V] |
| laicwben2/water-sort (TS) | A* acotado (100k estados, profundidad 100), solo en desarrollo | `max(0, segmentos − coloresÚnicos)` | clave canónica; deduplica destinos con misma firma | no publica tiempos [V] |
| requiemformemories/water-sort (JS) | DFS existencia (tope 400k nodos) + beam search (ancho 1500, prof. 250) | dispersión: cortes + (tubos por color − 1) | tubos ordenados lexicográficamente | 150 niveles regenerados en ~5 s [V] |
| ColinGJohnson/watersort-solver (Py) | A*, UCS, greedy, ponderado (aimacode) | cortes de color | — | capas ocultas no soportadas: requerirían MDP [V] |
| BeytullahKalay/WaterSort (Unity) | DFS en hilo de fondo | — | — | rechaza nivel si no se resuelve en 20.000 movimientos [V] |
| chromaoracle.com | BFS óptimo | — | — | >16 tubos tarda varios segundos; "modo misterio" enumera permutaciones de capas ocultas [V] |

### 2.2 Qué funciona (síntesis)

1. **Canonicalizar el estado** ordenando los tubos es la optimización más barata y la que más recorta (elimina las 10! permutaciones de 10 tubos). La diferencia entre el paper de ITB (sin canonicalizar: 54 s para 14 tubos) y los repos que sí lo hacen (milisegundos) lo confirma **[V]**.
2. **Poda de movimientos**: no verter desde un tubo ya completo; no mover un tubo monocolor a uno vacío; tratar todos los vacíos como un único destino; siempre verter la racha completa (`min(racha, espacio)`); descartar movimientos que devuelven al estado anterior **[V]** (kang_wang, requiemformemories, laicwben2).
3. **Heurística**: cortes de color es admisible y consistente; combinarla con dispersión (`Σ(tubos con c − 1)`) con `max` mantiene admisibilidad y recorta 4-5× **[V]**. Para generación masiva se puede usar A* ponderado (w=2) y aceptar 1-2 movimientos de subóptimo **[V]**.
4. **Representación compacta** **[E]**: 4 bloques × 4 bits = 16 bits por tubo; 14 tubos = 224 bits; clave canónica = tubos ordenados como enteros de 16 bits → hash de 64 bits. Un conjunto `visited` de 500k entradas cabe en pocos MB.
5. **Costo esperado para 10-14 tubos de capacidad 4** **[E a partir de [V]]**: óptimo con A* canonicalizado: 10³-10⁵ estados típicos, picos de 2×10⁵-5×10⁵ en instancias duras de 12-14 colores; <1 s por nivel en PC, lo que permite generar y puntuar decenas de miles de niveles por hora offline. En móvil, un A* ponderado (budget 40k nodos) sirve para pistas en <50 ms.

## 3. Generadores: inverso, aleatorio+solver e híbridos

| Técnica | Cómo | Pros | Contras | Evidencia |
|---|---|---|---|---|
| (a) Barajado inverso | Partir del tablero resuelto; aplicar k movimientos inversos cuya inversa sea un vertido legal; guardar la ruta como prueba | Resolubilidad garantizada por construcción; sin solver en runtime; reproducible por semilla | La ruta inversa no es la óptima ni refleja la dificultad; produce tableros triviales (deshacer-rehacer) si no se filtra; necesita medir complejidad aparte | laicwben2 [V]; Sokoban dev.to (pitfalls) [V] |
| (b) Relleno aleatorio + solver | Fisher-Yates sobre `colores×capacidad` bloques; repartir; rechazar tubos ya resueltos; verificar con DFS/A*; calcular par | Mezcla "natural" y densa; el par óptimo y las métricas salen del solver | Tasa de rechazo por irresolubles con 1 vacío; coste de solver por candidato; con 12+ colores el A* óptimo puede dispararse | requiemformemories [V]; xsieux292 (generación ~1 s para 10 colores con A*, >10 min con BFS) [V]; BeytullahKalay [V] |
| (c) Híbrido (recomendado) | Inverso con profundidad controlada y filtros + solver acotado para par, ramificación y trampas + bot casual para dificultad percibida | Garantía + medición; tasa de rechazo baja; dificultad calibrada | Más código en pipeline offline | awaisnaseer124 ("generación a lo largo de una curva desde semilla, conservando solo candidatos resolubles en el rango objetivo") [V] |

Parámetros concretos publicados:

- laicwben2 **[V]**: movimiento inverso legal = origen no vacío, destino no lleno y sin el mismo color arriba, solo se mueve parte de la racha superior, el origen tras extraer debe quedar vacío o mostrar el mismo color, y no se permite volcar todo el origen en un vacío (evita trivialidad). Candidatos ponderados por `complejidad + rand·2`, se muestrea entre los 3 mejores. Complejidad = `transiciones·2 + tubosMezclados·2 + tubosParciales`. Fácil: 4 colores, 2 vacíos, 18 inversos, complejidad ≥10. Medio: 6/2/30/≥18. Difícil: 8/2/46/≥27. Rechazo si está resuelto, complejidad baja o solución < colores×2. Semilla `water-sort:v2:{dificultad}:level:{n}`; hasta 960 intentos.
- requiemformemories **[V]**: `deal()` baraja y reparte en `colores + extra − vacíosIniciales` tubos; rechaza tubos ya monocolor llenos. Puntuación de ordenamiento: `fallo·30 + longitudSolución + colores·cap/4`.
- Lección Sokoban **[V]**: aun con generación inversa aparecieron niveles injugables; hizo falta validación posterior; 2-3 reintentos de media con tope de 15.

Cómo evitar estados iniciales triviales (todas las fuentes coinciden): rechazar si ya está resuelto, si algún tubo está completo monocolor, si la solución óptima < 2×colores, si hay pocas transiciones, y si el bot casual gana >90 % de las veces en rangos que no son tutorial.

## 4. Métricas de dificultad medibles

| Métrica | Cómo se calcula | Correlación con dificultad percibida | Fuente |
|---|---|---|---|
| Vacíos (2 vs 1) | parámetro | Muy alta: "la dificultad viene de lo estrecho del espacio, no del tamaño del tablero"; Hard/Master usan 1 extra; modo "Hard" comercial = más colores y menos vacíos | requiemformemories [V]; tiendas [S] |
| Par (mínimo de movimientos) | A*/BFS | Media: separa fácil de medio, pero tableros grandes con 2 vacíos "solo tardan más" | requiemformemories [V]; chromaoracle (12 vs 30+ movimientos) [V] |
| Tasa de fallo de bot casual | 40 partidas de política greedy con ruido (85 % mejor puntuación / 15 % aleatorio), tope 400 pasos; fallo = deadlock o tope | Alta por diseño: Hard ≥75 %, Master ≥90 % | requiemformemories [V] |
| Factor de ramificación / decisiones | media de movimientos legales no forzados por paso; suma de (opciones−1) | Alta en otro puzzle casual (sobrevive al contacto con jugadores; el tamaño no) | indiecoredev [V]; transferencia a water-sort [E] |
| Densidad de trampas | fracción de movimientos legales que llevan a deadlock (sin salida) | Plausible; ningún repo la publica para water-sort | [E] |
| Número de soluciones / estados | tamaño del grafo de estados alcanzable | Baja utilidad directa; costoso | [E] |
| Colores | parámetro | Media: escala el par, no la estrechez | [V] |
| Colores ocultos | capas tapadas hasta destaparlas | Alta pero introduce azar; usuarios lo describen como "cuestión de suerte" con 5 deshacer | [S]; chromaoracle [V] |
| Capacidad 5-6 | parámetro | Sube dificultad en Hard/Master (cap 4-6) | requiemformemories [V]; chromaoracle [V] |

Fuera del género, King mide intentos, pass rate por 100 intentos, reshuffles, time to abandon y time to pass **[V]**; Tactile (Lily's Garden) modela la distribución de movimientos restantes con una binomial negativa que ajusta ~85 % de 4.000 niveles y correlaciona ρ=0,83 con la tasa de completado; cambiar el límite de movimientos mueve ~2 % la tasa **[V]**. Agentes RL predicen la tasa de completado humana por nivel (el mejor predictor son los movimientos del 5 % de mejores partidas del agente) y combinar datos de cohortes con datos simulados da las estimaciones más precisas **[V]**.

## 5. Curva de dificultad en puzzles casuales exitosos

| Juego / fuente | Patrón | Datos |
|---|---|---|
| Candy Crush Saga | Pico y valle | Nivel muy difícil seguido de 5-6 fáciles/medios; >18.700 niveles (mayo 2025); millones pasaron el nivel 10.000; "niveles locamente difíciles nunca compensan a largo plazo"; poda recurrente de los 100 peores niveles [V] |
| Royal Match (niveles 1-100) | Aprender → construir → pico → alivio | 8 hard (39, 45, 49, 55, 65, 75, 85, 95; ~1,6 intentos), 5 super hard (59, 69, 79, 89, 99; hasta 2,5 intentos), normal ≈1,2; movimientos 30-38 (1-20), 25-30 (21-60), 23-27 (61-100); 11 mecánicas nuevas en 91 niveles, cada una con 2-3 tableros de aprendizaje [V] |
| Guía Gamigion | Rampa | 1-20 fácil, 20-30 primer pico, 50+ progresión y "paywalls"; intentos 1-3 = fácil, 20-35 = difícil [V] |
| Mobile Game Doctor 2025 | Curva de sierra | ciclo alivio → construcción → clímax/jefe → repetir; 20 niveles/semana ≈ 5 h de juego; "siempre debe sentirse superable sin boosters" [V] |
| Game Developer (tile puzzles) | Reordenar tras soft launch | 40-80 niveles/mes en live-ops; KPI = intentos medios/medianos, no pass rate [V] |
| indiecoredev | Ciclo de 20 | 12 comodidad / 5 reto / 2 recuperación / 1 hito; ancla de cuantil 0,08→0,82 con forma 1,1; 6 niveles de calibración; hitos antes del nivel 16 tratados como comodidad; salto máx. +2,2 / −3,4 bandas [V] |

Escalado de colores a lo largo de miles de niveles: ningún título publica su tabla; lo observable en tiendas y herramientas es 2-3 colores con 4-6 tubos al inicio, 6 colores con 8-10 tubos a mitad de campaña, hasta 10-12 colores y 1 vacío en "grandmaster" **[V/S]**. Los listados comerciales anuncian 400+, 2.500+, 3.000+ y 5.000+ niveles **[V/S]**, lo que solo es viable con generación procedural con semilla y puntuación automática; King en cambio produce a mano con asistencia de IA **[V]**.

### Generación: cliente con semilla vs servidor vs mezcla

- Cliente con semilla (BeytullahKalay, laicwben2): barato, infinito, pero sin calificación comparativa, QA ni posibilidad de reordenar; cambiar el código cambia el nivel 137 de todos **[V]**.
- Precalculado offline y enviado en paquetes (indiecoredev): 392 tableros ≈ 2,4 MB minificado incluyendo diccionario; permite verificar, puntuar en percentiles y versionar (`MODEL_VERSION`) **[V]**.
- Mezcla (awaisnaseer124): niveles 1-11 a mano, 12-50 generados desde semilla a lo largo de una curva **[V]**. Es el modelo que recomendamos.

## 6. Abandono por nivel y recalibración

- KPIs probados: intentos por nivel (media y mediana), pass rate por 100 intentos, time to abandon, time to pass, churn en el nivel **[V]**. Mobile Game Doctor añade movimientos restantes al ganar y % de objetivo completado **[V]**.
- Proceso: soft launch → funnel por nivel → reordenar/retocar → A/B con cohortes de cientos de jugadores → rebalanceo en vivo **[V]**.
- Dificultad adaptativa: el marco "difficulty-aware" (KDD 2021) derivó dificultad percibida personalizada, mejoró la predicción de churn y en A/B online elevó retención y engagement de forma significativa (sin cifras públicas en el resumen) **[V]**. Un experimento de campo de ~300.000 jugadores en 12 semanas dio puzzles progresivamente más fáciles a jugadores con alto riesgo de churn **[S, no pude acceder al artículo]**. King tiene patentes de ajuste dinámico de dificultad basado en churn predicho **[S]**.
- Benchmark de retención puzzle (Mistplay 2026): D1 31,85 %, D7 12,18 %, D30 5,35 %; match-3: 32,65 / 13,98 / 7,15 **[V]**.

## 7. Diseño recomendado para Amazon Fill

### 7.1 Pipeline (offline, determinista)

```
GENERAR_NIVEL(spec, semilla):
  # spec: colores C, capacidad H (4-6), vacios K (1-2), tubosExtra, profundidadInversa D, variantes
  rng = PRNG(semilla)                       # semilla = hash("amazonfill:v1:"+rango+":"+n)
  resuelto = [tubo lleno de color c for c in C] + [vacio]*K
  estado = resuelto; ruta = []; vistos = {canon(estado)}
  repetir D veces:
    cands = movimientos_inversos_legales(estado)      # criterios laicwben2 (sec. 3)
    cands = [m for m in cands if canon(aplicar(estado,m)) not in vistos]
    ordenar cands por complejidad(aplicar(estado,m)) + rng()*2; elegir entre los 3 mejores
    estado = aplicar(estado, m); ruta.append(inverso(m)); vistos.add(canon(estado))
  si resuelto(estado) o algun_tubo_monocolor_lleno(estado): RECHAZAR
  si complejidad(estado) < minComplejidad(spec): RECHAZAR
  sol = ASTAR(estado, H, presupuesto=300k)            # heuristica max(cortes, dispersion), canon = tubos ordenados
  si sol == None: RECHAZAR (presupuesto) ; par = len(sol)
  si par < 2*C: RECHAZAR
  fallo = BOT_CASUAL(estado, partidas=40)             # politica greedy con ruido, 85/15, tope 400 pasos
  ramif = decisiones_medias(sol)                      # media de (movimientos legales no forzados) a lo largo de la ruta optima
  trampas = fraccion_movimientos_a_deadlock(estado, muestreo=200 estados de la ruta y vecinos)
  D = 0.35*z(fallo) + 0.25*z(par - 2C) + 0.20*z(ramif) + 0.10*z(trampas) + 0.10*[K==1]
  devolver {estado comprimido, par, fallo, ramif, trampas, D, variantes, semilla}

ASTAR(s0): open=heap[(h(s0),0,s0)]; g={canon(s0):0}
  mientras open: s=pop; si resuelto(s): return ruta
    para m in movimientos_legales_podados(s):          # sin tubo completo como origen, sin monocolor->vacio,
      t=aplicar(s,m); k=canon(t)                       # un solo destino vacio, racha completa, sin volver atras
      si k not in g or g[s]+1 < g[k]: g[k]=g[s]+1; push((g[k]+h(t), ...))
  h(t) = max(cortes_de_color(t), Σ_c (tubos_con_c − 1))

COLOCAR_EN_CAMPAÑA(pool, slots):                     # ritmo de 20 (indiecoredev)
  para cada slot con tono in {comodidad x12, reto x5, recuperacion x2, hito x1}:
    objetivo = cuantil(ancla(slot) + ventana(tono))
    elegir del pool el nivel no usado mas cercano, respetando salto max (+2.2/-3.4 bandas) y anti-repeticion
```

Los pesos de D son una propuesta inicial **[E]**; se recalibran contra intentos reales tras el soft launch (regresión de intentos medianos sobre las cinco métricas).

### 7.2 Curva propuesta por rangos

| Rango | Colores | Tubos totales | Vacíos | Capacidad | Par objetivo | Fallo bot (normal / hard) | Cadencia hard | Variantes activas |
|---|---|---|---|---|---|---|---|---|
| 1-10 (tutorial) | 3→5 | 5-7 | 2 | 4 | 4-10 | 0-10 % / — | ninguna | ninguna; 1 afluente, 1 país; nivel 3 enseña deshacer, nivel 7 enseña tubo extra |
| 11-50 | 5-7 | 7-9 | 2 | 4 | 10-20 | 10-35 % / 40-60 % | hard en 20, 30, 40, 50 | cambio de país cada 10 niveles; sin ocultos |
| 51-200 | 6-9 | 8-11 | 2 (hard: 1) | 4 (cap 5 ocasional desde 150) | 18-30 | 25-50 % / 60-80 % | hard cada 7-10, super hard cada 20 (≥85 %) | colores ocultos 1 capa desde ~80 solo en niveles marcados; "frutas raras" (cap 5) |
| 201-1000 | 8-11 | 10-13 | 2 (hard: 1) | 4-5 | 25-45 | 35-60 % / 75-90 % | ciclo 20: 12 normal, 5 hard, 2 alivio, 1 hito (≥90 %) | ocultos hasta 2 capas; cap 5-6 en hitos; eventos de afluente |
| 1000+ | 9-12 | 11-14 | 2 (hard/hito: 1) | 4-6 | 30-55 | 40-65 % / 80-95 % | mismo ciclo; amplitud constante, no crecimiento monótono | rotación de todas las variantes; DDA para jugadores en riesgo |

Reglas de la tabla: nunca dos hard seguidos; el hito siempre va precedido de un reto, nunca de un valle (si no, deja de ser el más difícil del tramo **[V]**); tras cada hito, 2 niveles con fallo de bot <30 % **[V, patrón Candy Crush]**; las mecánicas nuevas (oculto, cap 5) debutan en 2-3 tableros fáciles **[V, Royal Match]**.

### 7.3 Economía de ayudas ligada a la dificultad

- Deshacer, tubo extra y pista son las tres ayudas estándar del género; los líderes las dan por anuncio (5 deshacer gratis, luego anuncio; tubo extra por anuncio) y reservan las monedas para cosméticos **[V]**. Para Amazon Fill: tubo extra con 1 vacío equivale a bajar un escalón entero de dificultad, así que su precio debe ser el mayor de las tres.
- Recompensas proporcionales a la carga de decisión, no planas (ejemplo publicado: 6 monedas + 0,30×carga; 8 en fácil, 35 en experto), y bonos por mejora de par pagados solo como delta para evitar farmear niveles fáciles **[V]**.
- Instrumentación mínima por nivel: intentos, deshacer usados, tubo extra usado, pista usada, deadlocks, reinicios, tiempo, abandono. Umbrales iniciales **[E]**: mediana de intentos >4 en un nivel "normal" o abandono >3 % → sustituir por otro nivel del pool en el mismo slot vía remote config, sin actualización de app.

## Implicaciones para Amazon Fill

1. **Generar offline, nunca en el cliente.** Pipeline en Node/Python que produce lotes de 1.000 niveles con semilla, los puntúa (par, fallo de bot, ramificación, trampas) y los empaqueta; el cliente solo lee JSON. 10.000 niveles × ~60 bytes de tablero + metadatos ≈ 0,6-1 MB **[E]**. Versionar el paquete (`MODEL_VERSION`) para no romper el progreso.
2. **Híbrido inverso + solver + bot.** Copiar los criterios de movimiento inverso de laicwben2 y las profundidades 18/30/46 como punto de partida; extender a 60-90 inversos para 10-12 colores **[E]**; validar con A* canonicalizado (heurística `max(cortes, dispersión)`) y medir el fallo del bot casual con la política documentada (40 partidas, 85/15).
3. **La palanca principal es 1 vs 2 vacíos**, no añadir colores. En Amazon Fill el "tubo extra" comprable debe aparecer como una vasija adicional que llega flotando por el río; su valor percibido es máximo en niveles con 1 vacío.
4. **Representación de nivel**: array de tubos, cada uno con 4-6 índices de color (0-11) y máscara de capas ocultas; los índices se mapean a frutas amazónicas por paleta de país (açaí, camu camu, copoazú, aguaje, cocona, etc.). La animación de "sumergir y teñir" al cargar es puramente presentacional: el estado inicial ya viene resuelto por el pipeline.
5. **Colores ocultos solo como variante marcada** y solo si el generador comprueba que existe al menos un movimiento seguro en todas las permutaciones (enumeración tipo chromaoracle). Limitar a 1-2 capas por nivel y nunca en los dos primeros tubos que el jugador toca.
6. **Progresión por ritmo de 20**, con hitos cada 20 y hard cada 5-7 tras el nivel 50, replicando la cadencia de Royal Match (hard ≈1,6 intentos, super hard ≈2,5) adaptada a water-sort, donde "intentos" se traduce en reinicios + deadlocks.
7. **KPI de dificultad = intentos medianos por nivel y abandono en nivel**, no win rate. Dashboard por nivel desde el soft launch en Android; poda mensual de los 50-100 niveles con peor "time to abandon".
8. **DDA ligera desde el día 1**: si un jugador falla 3 veces un nivel normal, el siguiente nivel del mismo slot se sirve con 2 vacíos en vez de 1, o con un nivel del pool de D menor. Es la intervención que la literatura y los líderes han validado.
9. **Paletas verificadas**: ocho paletas vetadas por distinguibilidad de color es práctica publicada; para 12 frutas hay que validar contraste y daltonismo antes de fijar el arte.
10. **Cómputo**: el pipeline offline de 10.000 niveles con A* óptimo cabe en una tarde de PC (mediana <0,1 s por nivel; colas de 1-5 s en 12-14 tubos) **[E a partir de [V]]**. En cliente, la pista usa A* ponderado con presupuesto de 40k nodos.

## Preguntas abiertas

- No encontré datos públicos de tasa de abandono por nivel específicos de water-sort; todo lo verificado viene de match-3 y collapse. Hay que medirlo en soft launch.
- La correlación entre factor de ramificación y dificultad percibida está verificada en un puzzle de palabras, no en water-sort; los pesos de la fórmula D son una hipótesis a calibrar.
- La cifra "Candy Crush redujo churn 11 % con IA" no pude verificarla; el experimento de campo de 300k jugadores (ScienceDirect 2025) solo lo vi en un fragmento. Decisión de producto: cuánto invertir en DDA en v1.
- Colores ocultos: ¿los incluimos en v1? Añaden profundidad pero introducen azar y quejas documentadas; requieren solver para observabilidad parcial.
- Capacidad 5-6 y tubos "bloqueados" (variantes de los líderes) no están cubiertos por los repos abiertos; habría que extender el solver y probar que no disparan el coste.
- ¿Niveles 1-11 hechos a mano (como awaisnaseer124) o también generados con restricciones? Recomiendo a mano para controlar el tutorial de deshacer/tubo extra.
- Dueño del producto debe decidir la cadencia de niveles nuevos (20/semana es el estándar publicado) y si el pool se amplía por paquete remoto o por actualización.
- La tabla de curva propuesta (colores/tubos por rango) no tiene fuente comercial directa; está inferida de tiendas, herramientas y repos. Validar con soft launch.

## Fuentes

- https://arxiv.org/abs/2202.09495 — Ito et al., "Sorting Balls and Water: Equivalence and Computational Complexity" (FUN 2022; TCS 978, 2023)
- https://ar5iv.labs.arxiv.org/html/2202.09495 — versión HTML (Teoremas 13, 14, 16, 17; Corolario 12)
- https://github.com/hkociemba/WaterBallSortPuzzleOptimalSolver
- https://informatika.stei.itb.ac.id/~rinaldi.munir/Stmik/2024-2025/Makalah2025/Makalah-IF2211-Strategi-Algoritma-2025%20(39).pdf — Azkarayan, "A* Algorithm Application for Solving the Water Sort Puzzle" (ITB, 2025)
- https://github.com/xsieux292/water-sort-puzzle
- https://dev.to/kang_wang_375088cb68739bf/how-i-built-a-water-sort-puzzle-solver-in-about-300-lines-of-javascript-5edi
- https://pkg.go.dev/github.com/octo/watersort
- https://github.com/awaisnaseer124/water-sort-puzzle
- https://github.com/laicwben2/water-sort (README, src/game/generator.ts, src/game/solver.ts)
- https://github.com/requiemformemories/water-sort (README, tools/gen_levels.js, tools/engine.js)
- https://github.com/ColinGJohnson/watersort-solver
- https://github.com/BeytullahKalay/WaterSort
- https://github.com/cemasma/water-sort-puzzle-solver
- https://chromaoracle.com/water-sort-puzzle-solver
- https://chromaoracle.com/guides/color-sort-strategy-guide
- https://dev.to/yurukusa/i-built-a-procedural-sokoban-generator-in-one-day-heres-why-it-kept-making-unsolvable-levels-5191
- https://dev.to/indiecoredev/rating-puzzle-difficulty-by-decisions-3k5b
- https://dev.to/indiecoredev/difficulty-rates-a-stage-progression-places-it-5243
- https://dev.to/indiecoredev/generate-levels-offline-not-at-runtime-4ihj
- https://dev.to/indiecoredev/pricing-an-economy-off-its-difficulty-model-5bcj
- https://www.gamedeveloper.com/design/candy-crush-saga-a-sweet-journey-into-monetization
- https://mobilegamer.biz/how-king-defines-a-good-candy-crush-saga-level-and-why-it-constantly-prunes-the-bad-ones/
- https://www.news4jax.com/business/2025/05/11/how-candy-crush-uses-ai-to-keep-players-coming-back-to-its-puzzles/
- https://gdcvault.com/play/1026879/Blockers-Analyzing-Difficulty-Drivers-in — King, GDC Summer 2020
- https://www.gamigion.com/royal-match-level-design-insights/
- https://www.gamigion.com/how-to-design-difficulty-in-puzzle-games/
- https://www.deconstructoroffun.com/blog/2021/3/21/royal-match-the-new-king-from-turkey
- https://mobilegamedoctor.com/2025/05/30/illuminating-level-creation-for-free-to-play-puzzle-games/
- https://www.gamedeveloper.com/design/smart-casual-the-state-of-tile-puzzle-games-level-design-part-1
- https://arxiv.org/abs/2107.03305 y https://ar5iv.labs.arxiv.org/html/2107.03305 — Kristensen et al., "Statistical Modelling of Level Difficulty in Puzzle Games" (Lily's Garden)
- https://arxiv.org/abs/2306.14626 — "Estimating player completion rate in mobile puzzle games using reinforcement learning"
- https://arxiv.org/abs/2401.17436 — Kristensen & Burelli, "Difficulty Modelling in Mobile Puzzle Games" (2024)
- https://arxiv.org/abs/2008.12937 — Roohi et al., "Predicting Game Difficulty and Churn Without Players" (Angry Birds Dream Blast)
- https://arxiv.org/pdf/2209.13495 — "Personalized Game Difficulty Prediction Using Factorization Machines"
- https://dl.acm.org/doi/abs/10.1145/3447548.3467277 — "A Difficulty-Aware Framework for Churn Prediction and Intervention in Games" (KDD 2021)
- https://www.sciencedirect.com/science/article/abs/pii/S0167811625000060 — experimento de campo DDA (solo fragmento; acceso denegado)
- https://segwise.ai/blog/mobile-gaming-app-user-retention-strategies — benchmarks Mistplay 2026
- https://apps.apple.com/us/app/water-sort-puzzle/id1514542157 — IEC Global
- https://apps.apple.com/us/app/water-sort-puzzle-sort-color/id1597897013 — Guru Smart Holding
- https://apps.apple.com/us/app/id1582157018 — Essendon Studios (400+ niveles)
- https://www.nintendo.com/us/store/products/color-water-sort-switch/ — 2.500+ niveles, modos Classic/Timed/Hard
- https://play.google.com/store/apps/details?id=water.sort.puzzle.wood.color.sort.brain.train.test.watersort.ball.sort.logic.game — fragmento: 3.000+/5.000+ niveles, Normal vs Hard
- https://forum.defold.com/t/sort-ball-puzzles-game/71684

## Complemento 4: Pipeline de contenido para reto diario, modo infinito y comodín Barajar (solver en cliente vs. pool pregenerado; ocultos y capacidades mixtas)

Añadido 2026-10-05. Convención: **[V]** verificado con URL, **[E]** estimación. El presupuesto de búsquedas web estaba agotado; este complemento se apoya en lectura directa de documentación oficial (Firebase, Google Play, Apple, Unity) y de fuentes ya citadas.

### 4.1 Respuesta corta

La regla "nunca generar en cliente" (sección 7) protege a los **niveles de campaña**: numerados, calibrados por percentiles y reordenables en vivo. No aplica a los tres flujos preguntados, porque el **barajado inverso garantiza resolubilidad por construcción sin solver** (sección 3) y lo único que se pierde al generar en el dispositivo es la calibración fina, prescindible en "sin fin" y en Barajar.

| Flujo | v1.0 | Origen del tablero | ¿Servidor? | Garantía |
|---|---|---|---|---|
| Reto del río (diario) | Sí | Pool offline de 365-730 tableros embebido, **indexado por fecha**; sustituciones por Remote Config | No (RC opcional) | Pipeline offline |
| Río sin fin (suscripción) | Sí, versión pool | Pool extra de ~1.000 tableros no numerados en orden `PRNG(semillaJugador)`; v1.1: generador inverso en cliente | No | Offline; en v1.1 por construcción |
| Aguacero (Barajar), nivel 30+ | Sí | **Reconstrucción inversa en el dispositivo** sobre las vasijas no resueltas | No | Por construcción; A* acotado solo para par |

### 4.2 Reto del río: pool fechado sin servidor

- Precedente masivo: Wordle original corría 100 % en cliente con 2.315 soluciones embebidas y `solución = días_desde(2021-06-19) mod 2315` **[V]**; su único defecto fue que las respuestas futuras eran legibles **[V]**, irrelevante en water-sort. NYT lo llevó a servidor en 2022 por curación editorial, no por necesidad técnica **[V]**.
- Diseño **[E]**: `reto(fecha) = poolDiario[(días_desde(epoch) + offsetVersión) mod 730]`; 730 × ~80 bytes ≈ 60 KB. Mismo pipeline de 7.1 con D fijado en la banda "reto" y país/afluente rotando por semana.
- Sustituciones fechadas (fiesta nacional, evento): parámetro JSON `daily_overrides` en Firebase Remote Config. Límites **[V]**: 3.000 parámetros, 2.000 condiciones, 1.000.000 caracteres sumando valores, 300 versiones; un año de retos (~37 k caracteres) cabe en un solo parámetro **[E]**. Unity Remote Config equivale (`GetJson`) y expone el origen `Default/Cached/Remote`, donde `Cached` = valores de una sesión anterior **[V]**.
- Offline: el reto se juega igual (pool local, fecha del reloj). Firebase fija 12 h de intervalo mínimo de fetch, aplica throttling (antes del SDK 17: 5 fetch/60 min) y exige defaults in-app porque "no hay que depender de la red" **[V]**. Anti-reloj **[E]**: guardar `ultimoDiaReclamado`; fecha anterior = sin recompensa; con red, contrastar con la cabecera `Date` del fetch.
- Recompensa **[E]**: referencias verificadas sin cifras: Sudoku.com da trofeos únicos por reto diario y medallas por eventos estacionales **[V]**; Microsoft Solitaire publica un reto diario por cada uno de sus 5 juegos y eventos de 5-30 retos **[V]**. Propuesta: reto completado = sello en el calendario del mes + monedas de 2-3 niveles normales; 20 sellos/mes = cosmético del país del mes; racha de 7 días = 1 Aguacero. Calibrar en soft launch.

### 4.3 Río sin fin: pool primero, generador después

- v1.0: pool de 1.000 tableros con metadatos D, en orden por semilla de jugador (≈50-80 h **[E]**), ampliable por paquete remoto (4.6).
- v1.1: generador inverso (parámetros laicwben2) en hilo de fondo + A* ponderado acotado solo para par y anti-trivialidad. Presupuesto de referencia **[V]**: w=2 con 40.000 expansiones, reintento w=4 con 400.000, BFS de 300.000 estados para demostrar irresolubilidad; en Node 22 con 2 vacíos, 12 colores = 1,5 ms mediana / 26 ms peor caso, 8 colores = 0,4 / 2 ms. En Android de gama media estimo 5-10× más lento: < 300 ms peor caso **[E]**; BeytullahKalay ya corre DFS en hilo de fondo en Unity con tope de 20.000 movimientos **[V]**. Tope propuesto **[E]**: 100 ms por intento, 3 intentos, luego pool.

### 4.4 Aguacero (Barajar) conservando la solubilidad

1. **Reconstrucción inversa (recomendada) [E sobre V]**: las vasijas selladas quedan fijas; los bloques de las no resueltas suman exactamente H por color, es decir, forman un estado resuelto parcial; aplicar k movimientos inversos legales con los mismos vacíos y los filtros de laicwben2 (no volcar todo en un vacío; rechazar si queda igual al tablero actual o con un tubo monocolor lleno). k = max(12, 3 × colores restantes) **[E]**. Coste de microsegundos, sin solver; la ruta inversa sirve de pista de emergencia.
2. **Permutar + verificar**: Fisher-Yates sobre los bloques no resueltos + A* a 40.000 nodos, hasta 20 reintentos. Con 1 vacío existen instancias irresolubles (sección 1) **[V]**, así que la tasa de rechazo puede ser alta; solo como alternativa.

Regla de producto **[E]**: Aguacero no altera colores ni vacíos, y el par post-barajado debe ser ≤ par actual + 2 (una llamada de A* acotado, < 50 ms) para que el comodín nunca castigue.

### 4.5 Colores ocultos y capacidad 5-6

- Capacidad: ni el barajado inverso ni la heurística de cortes dependen de H=4; los repos abiertos lo fijan por código, no por algoritmo. chromaoracle resuelve frascos "de 5 o más" y tarda "unos segundos" con 16+ frascos **[V]**. En cliente: cap 5-6 solo con ≤ 12 vasijas y 2 vacíos **[E]**.
- Ocultos: chromaoracle genera cada disposición válida de las capas ocultas, resuelve todas y devuelve solo los movimientos correctos en cualquier realidad **[V]**. Coste combinatorio: ≤ 4 bloques ocultos distintos = ≤ 24 disposiciones (ms); 8 bloques = 40.320 **[E]**. Decisión: ocultos **solo en pools pregenerados** (campaña y diario, verificados offline con "existe movimiento seguro"); nunca en río sin fin generado en cliente. Aguacero sobre un nivel con ocultos **revela todas las capas** (la lluvia limpia el barro), coherente con el tema y elimina el problema.

### 4.6 Entrega de pools tras el lanzamiento

| Canal | Qué permite | Límites verificados | Encaje |
|---|---|---|---|
| Firebase / Unity Remote Config | JSON pequeño, condiciones por país/versión, caché offline | 1 M caracteres, 3.000 parámetros, fetch mín. 12 h **[V]** | Overrides diarios, flags, umbrales DDA |
| Addressables + Unity CCD | Catálogo remoto con versión/hash; buckets/releases/badges (`latest`); sin pasar por tienda | Solo lo remoto es actualizable; bundles locales no **[V]** | Pools de 1.000+ niveles, temporadas |
| Play Asset Delivery | install-time / fast-follow / on-demand en el AAB | 4 GB base+install-time, 30 GB on-demand/fast-follow, 100 packs, 34 GB total **[V]** | Arte por país; no para contenido fechado (va con la versión del bundle) |
| iOS On-Demand Resources | Equivalente Apple | **Deprecado desde iOS 27**, migrar a Background Assets **[V]** | iOS con Background Assets o CDN |
| CDN estático (JSON + ETag) | Lo más simple | — | Alternativa a CCD fuera de Unity **[E]** |

Para el programador **[E]**: un único módulo solver en C# (estado canónico, poda, `max(cortes, dispersión)`, A* ponderado con presupuesto parametrizable) sirve a pista, Aguacero y río sin fin (40 k nodos) y al pipeline offline (300 k). Estimación: 2-3 días solver con tests, 1-2 días generador inverso parametrizado en H y vacíos, 1 día indexado por fecha y recompensas. **Sin servidor propio en v1.0**; Remote Config gratuito basta.

### Fuentes del complemento 4

- https://firebase.google.com/docs/remote-config/quotas-limits
- https://firebase.google.com/docs/remote-config/parameters
- https://firebase.google.com/docs/remote-config/use-config-android
- https://firebase.google.com/docs/remote-config/loading
- https://docs.unity3d.com/Packages/com.unity.remote-config@4.0/manual/CodeIntegration.html
- https://docs.unity3d.com/Packages/com.unity.addressables@2.3/manual/ContentUpdateWorkflow.html
- https://docs.unity3d.com/Packages/com.unity.addressables@2.3/manual/RemoteContentDistribution.html
- https://docs.unity.com/ugs/manual/ccd/manual/UnityCCD
- https://developer.android.com/guide/playcore/asset-delivery
- https://support.google.com/googleplay/android-developer/answer/9859372
- https://developer.apple.com/help/app-store-connect/reference/on-demand-resources-size-limits
- https://reichel.dev/blog/reverse-engineering-wordle.html
- https://en.wikipedia.org/wiki/Wordle
- https://dev.to/kang_wang_375088cb68739bf/how-i-built-a-water-sort-puzzle-solver-in-about-300-lines-of-javascript-5edi
- https://chromaoracle.com/water-sort-puzzle-solver
- https://apps.apple.com/us/app/sudoku-com-number-games/id1193508329
- https://en.wikipedia.org/wiki/Microsoft_Solitaire_Collection
- https://apps.apple.com/us/app/water-sort-puzzle/id1514542157 (ficha sin reto diario ni barajar; deshacer 5 gratis, tubo extra por anuncio, offline)
- https://apps.apple.com/us/app/water-sort-puzzle-sort-color/id1597897013 (idem; monedas solo cosméticos)
- https://apps.apple.com/us/app/ball-sort-puzzle-color-game/id1494648714 (idem)

## Complemento 3: Estructura de mundos, rango de niveles por país y número máximo de colores que soporta el generador

Fecha: 2026-10-05. Cierra la discrepancia entre 01 (K=16, lista lineal, estrellas como pregunta abierta), 02 (K=12, hard cada 10 y luego ciclo de 20), 05 (fondos en los niveles 1/50/120, álbum de 10 frutas × 3 páginas) y 07 (13 mundos y 14 tintes sin rangos de nivel). Método: la cuota de WebSearch de la sesión estaba agotada, así que la verificación se hizo con WebFetch sobre fichas de tienda, Wikipedia, guías de niveles y repositorios; las wikis Fandom de Candy Crush, Toon Blast y Royal Match devolvieron HTTP 402 y no se pudieron leer. Convención: **[V]** verificado con URL, **[E]** estimación o cálculo propio.

### C3.1 Qué hace el género: tamaño de "mundo", presentación y techo de colores

| Dato | Valor | Fuente |
|---|---|---|
| Episodio en match-3 | Candy Crush Saga: 15 niveles por episodio; estrellas según puntuación; nivel 2.000 en sep-2016 | Wikipedia [V] |
| Áreas en Royal Match (niveles 1-100) | 5 mundos × 20 niveles; cambio de escenario en los niveles 1, 10, 21, 41, 61 y 81 (áreas cortas al inicio); 12.400+ niveles en nov-2025 con lote nuevo cada 2 semanas | Gamigion, Wikipedia [V] |
| Agrupación en el líder water-sort | Magic Sort: lista lineal numerada, sin estrellas ni mapa decorativo; las guías comunitarias lo trocean en bloques de 50 (1-50, 51-100…) con hitos en 50/100/150/200/300/400; 1.158 niveles documentados; "+100 niveles" por versión | levelsolve, App Store, doc 09 [V] |
| Presentación en IEC, Guru, Spica, Nutron | Número de nivel y lista lineal; Nutron Labs usa "páginas" de niveles (se puede saltar dentro de la página, no a la siguiente); ninguna ficha menciona estrellas; Guru etiqueta "Easy/Hard" | App Store [V] |
| Fondos y skins | Se compran con monedas ganadas por nivel: Spica (medidor de regalo al 100 % → monedas → diseños), Guru (fondos y formas de tubo), Switch (tubos, fondos, tapas), SortPuz (9+ fondos y skins), Water Sort Club ("temas desbloqueables", 10.000+ niveles) | fichas [V] |
| Colores por fase | Inicio 2-3 colores / 4-6 frascos / 6-10 vertidos; medio 6 colores / 8-10 frascos / 18-30 vertidos; final 40+ vertidos con capas ocultas; capacidad 4, "5 o más" en difíciles | chromaoracle [V] |
| Colores en Magic Sort | 2 → 10 en el nivel 30; 8-11 en 200-500; 9-11 con 1 vacío en hard/super hard | doc 09 [V] |
| Techo en clones web | Fácil 5 colores/7 tubos, Medio 7/9, Difícil 10/12, Muy difícil 16/18 | doc 01 [V] |
| Techo de solvers | Óptimo con 14 colores × 6 bloques "en segundos" (hkociemba); UI de solver hasta 20 tubos (chromaoracle); los repos de referencia de la sección 2 prueban 12 colores/14 tubos | [V] |

Lectura: ningún líder water-sort usa mapa de saga ni estrellas; el "mundo" es un cambio de fondo/skin, y los líderes de match-3 cambian de escenario cada 10-20 niveles al inicio y cada 15-20 después. El techo jugado en ruta principal es 10-12 colores; 14-16 solo aparece en modos "muy difícil" de clones y en solvers.

### C3.2 Decisión de K: tres números distintos, no uno

- **K_motor = 16**: 4 bits por capa (sección 2.2). Límite técnico de la representación; nunca se juega. Es el "16" de 01.
- **K_paleta = 14**: los 12 tintes de fruta + 2 aguas de 07, todos con patrón e icono. El álbum de 05 pasa de "10 frutas × 3 páginas" a 14 tintes ordenados por mundo de debut.
- **K_ruta ≤ 12** en la ruta principal (curva de 02 y techo observado en Magic Sort y clones). **13-14 solo en niveles hito (1 de cada 20) a partir del mundo 9 y en eventos**; así el jugador medio nunca ve los dos pares más peligrosos de 07 a la vez.

Restricción que obliga a cambiar el plan **[E, cálculo propio sobre la tabla de 07]**: el grafo de pares de riesgo (4-5, 5-6, 6-13, 13-14, 14-4, 3-12, 12-11, 11-10, 8-9, 1-2) tiene un conjunto máximo de tintes mutuamente seguros de **7** (por ejemplo guanábana, taperebá, aguaje, bacaba, açaí, agua Tapajós y camu camu). Por tanto la regla de 07 "ningún par de riesgo antes del nivel 200" es incompatible con los 6-9 colores que 02 pide en 51-200. Regla sustituta, que el generador recibe como parámetro `max_pares_riesgo` por nivel: 0 en 1-40, 1 en 41-80, 2 en 81-200, libre desde 201; el patrón de accesibilidad es obligatorio desde el nivel 41 (antes es textura sutil). El generador elige los K colores del pool del mundo respetando ese tope, de modo que el pool puede ser mayor que K.

### C3.3 Tabla maestra nivel → mundo → paleta (vuelta 1, niveles 1-1.000) [E]

| Mundo (país) | Niveles | n | K ruta | Tintes que debutan (nivel exacto) | Pool | Pares máx |
|---|---|---|---|---|---|---|
| 1 Estuario (BR, Pará) | 1-10 | 10 | 2→5 | açaí 1, aguaje 1, taperebá 2, guanábana 4, bacaba 7 | 5 | 0 |
| 2 Bajo Amazonas-Tapajós (BR) | 11-40 | 30 | 6-7 | agua Tapajós 11, camu camu 21 | 7 | 0 |
| 3 Manaos (BR) | 41-80 | 40 | 7-8 | urucum 41, agua Río Negro 61 (Encuentro de las Aguas) | 9 | 1 |
| 4 Trapecio (CO) | 81-140 | 60 | 8-9 | cupuaçu 81, lulo 111 | 11 | 2 |
| 5 Loreto (PE) | 141-200 | 60 | 9 | jenipapo 141 | 12 | 2 |
| 6 Oriente (EC) | 201-300 | 100 | 9-10 | cacao 201 | 13 | libre |
| 7 Madre de Dios (PE) | 301-420 | 120 | 10-11 | arazá 301 (paleta completa) | 14 | libre |
| 8 Madidi (BO) | 421-560 | 140 | 10-11 | — (capacidad 5 "fruta rara" en hitos) | 14 | libre |
| 9 Ucayali (PE) | 561-700 | 140 | 10-12 | — (hitos con 13-14 colores) | 14 | libre |
| 10 Alto Negro (VE/BR) | 701-820 | 120 | 10-12 | — | 14 | libre |
| 11 Escudo de Guayana (GY/SR/GF) | 821-920 | 100 | 10-12 | — | 14 | libre |
| 12 Invitados Orinoco (VE/CO) | 921-960 | 40 | 10-12 | — (skin de evento; etiquetado "vecinos del Orinoco") | 14 | libre |
| 13 El origen (PE, Andes) | 961-1.000 | 40 | 10-12 | — (hitos 13-14) | 14 | libre |

Cambios respecto a 07: bacaba sube al mundo 1 y camu camu al mundo 2 (ambos sin pares de riesgo), lulo baja al mundo 4 (es además la fruta colombiana) y jenipapo al 5; así los 7 primeros tintes no contienen ningún par de riesgo. La curva de 07 (4 → 7 → 9 → 11 → 14 en los mundos 1/2/3/6/9) queda en 5 → 7 → 9 → 13 → 14 de pool, con K jugado por debajo. Respecto a 05: el nivel 10 queda en 5 colores (no 7) y los tres fondos del MVP pasan a Pará (1), Tapajós (11) y Manaos (41); como la 1.0 lleva 1.000 niveles generados, necesita 5 fondos base (mundos 1-5, hasta el nivel 200) y un fondo "río genérico" para 201+ hasta la 1.1. Respecto a 02: el tramo 51-80 baja a 7-8 colores; la cadencia hard/hito (ciclo de 20 desde el nivel 50) no cambia y es independiente del mundo. Las longitudes 10/30/40/60/60/100/120/140 siguen el patrón Royal Match de áreas cortas al inicio y multiplican la sensación de viaje en la primera semana (3 países antes del nivel 81).

### C3.4 Presentación y reciclaje de mundos desde el nivel 1.001

- **Presentación**: número de nivel grande y lista lineal, el estándar del género **[V]**; **sin estrellas** (ningún líder water-sort las usa; Candy Crush sí, pero es match-3 con puntuación). El "mapa de río" existe solo como pantalla de progreso pasiva: un río que sube de la desembocadura a los Andes con 13 paradas, la posición actual, los fondos y skins desbloqueados y los "datos del río" coleccionados; no es navegación tipo saga y no bloquea nada. Al entrar al primer nivel de cada mundo se muestra un banner de país + afluente + agua base.
- **Reciclaje**: cada vuelta repite los 13 mundos en el mismo orden con 80 niveles por mundo (1.040 niveles por vuelta). Vuelta 2 (1.001-2.040) = lluvia + crecida; vuelta 3 (2.041-3.080) = noche + vaciante; vuelta 4 = alba + crecida; después se repite el ciclo de variantes. La dificultad se mantiene en amplitud constante (K 10-12, ciclo de 20), como ya fija 02. Las variantes son LUT de iluminación, partículas de lluvia y altura de la banda de agua (la crecida tapa playas e igapós; la vaciante descubre arenales), de modo que 13 pinturas base producen 52 fondos derivados sin pintar 52 veces **[E]**. Es el mismo recurso que usan los líderes con eventos estacionales ("Sort Under the Full Moon" en IEC, Halloween en Hexa Sort) **[V]**.
- **Conteo de arte por mundo**: 1 fondo base, 1 skin de recipiente (tabla de 07), 1 agua base (negra/blanca/clara), 3 variantes de luz y 2 hidrológicas. Vuelta 1 completa = 13 fondos + 13 skins; 1.0 = 5 fondos + 1 genérico + 2 skins; 1.1 = +8 fondos y +11 skins.

### C3.5 Especificación para el generador y los paquetes JSON

- Un paquete por mundo y vuelta (`mundo_03_v1.json`), con cabecera `{mundo, pais, vuelta, variante, agua_base, pool:[ids de tinte], k_min, k_max, max_pares_riesgo, cap, vacios, ciclo20:true}` y los niveles con los campos de la sección 7.1 (estado comprimido, par, fallo de bot, ramificación, trampas, D, semilla). Semilla `amazonfill:v1:m{mundo}:v{vuelta}:n{nivel}`.
- Paleta como tabla global `tinte_id → {hex, patron, fruta, mundo_debut, nivel_debut}` con los 14 tintes de 07 y la lista de 10 pares de riesgo.
- El validador rechaza un nivel si usa un tinte con `nivel_debut` mayor que el del nivel, si el número de pares de riesgo presentes supera `max_pares_riesgo`, si K > `k_max` o si K_motor > 16. En hitos y eventos `k_max` puede subir a 14; el resto de la ruta principal nunca pasa de 12.
- Tamaño: 1.040 niveles por vuelta × ~60-80 bytes ≈ 70-85 KB por vuelta antes de comprimir **[E]**; cabe cualquier número de vueltas en el binario o por remote config.

Preguntas que quedan para el dueño del producto: (1) si el mundo 12 "Invitados Orinoco" va en la ruta lineal (como aquí) o solo como evento de temporada; (2) si vale la pena jugar 13-14 colores en hitos dado el riesgo de accesibilidad, o fijar K=12 absoluto y dejar las dos aguas como tintes de evento; (3) las longitudes de mundo y los niveles exactos de debut son hipótesis coherentes con los benchmarks, no datos de ningún water-sort comercial: validar con el embudo por nivel del soft launch.

Fuentes del complemento:

- https://en.wikipedia.org/wiki/Candy_Crush_Saga — 15 niveles por episodio; estrellas por puntuación
- https://en.wikipedia.org/wiki/Royal_Match — 12.400+ niveles (nov-2025), nuevos cada dos semanas
- https://www.gamigion.com/royal-match-level-design-insights/ — 5 mundos × 20 niveles; áreas nuevas en 1, 10, 21, 41, 61, 81
- https://levelsolve.com/magic-sort/ — 1.158 niveles, agrupación por bloques de 50, tramo 43-112 expert
- https://chromaoracle.com/water-sort-puzzle-solver — colores/frascos por fase; hasta 20 tubos; modo misterio
- https://github.com/hkociemba/WaterBallSortPuzzleOptimalSolver y http://kociemba.org/themen/waterball/colorsort.html — óptimo con 14 colores × 6 bloques
- https://apps.apple.com/us/app/water-sort-puzzle/id1514542157 — IEC: "hundreds of levels", niveles numerados
- https://apps.apple.com/us/app/water-sort-puzzle-sort-color/id1597897013 — Guru: Easy/Hard; fondos y formas de tubo por monedas; 130 K valoraciones
- https://apps.apple.com/us/app/ball-sort-puzzle/id1494648714 — Spica: más colores con los niveles; temas por monedas; 233 K valoraciones
- https://apps.apple.com/us/app/magic-sort/id6499209744 — "+100 niveles" por versión; etiquetas hard/super hard
- https://apps.apple.com/us/app/hexa-sort/id6463127238 — áreas del mundo como meta; tema de Halloween
- https://apps.apple.com/us/app/id6447139347 — Nutron Labs: "páginas" de niveles; fondos por monedas
- https://apps.apple.com/app/id1580876589 — SortPuz 3D: 9+ fondos y skins
- https://cafebazaar.ir/app/com.gamovation.watersort?l=en — Water Sort Club: 10.000+ niveles, temas desbloqueables
- https://www.nintendo.com/us/store/products/color-water-sort-switch/ — 2.500+ niveles; modos classic/timed/hard; tubos, fondos y tapas por monedas
- Documentos internos: 01-mecanicas-genero.md (sec. 5 y parámetros de clones), 05-retencion-liveops-meta.md (MVP y primera sesión), 07-tematica-amazonas.md (sec. 3.2 y 7), 09-referencia-magic-sort.md (escala de colores por nivel)
