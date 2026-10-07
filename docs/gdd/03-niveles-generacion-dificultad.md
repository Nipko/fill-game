# Capítulo 3 · Generación de niveles, solver y curva de dificultad

Fecha: 2026-10-06 (revisión cruzada; primera versión 2026-10-05) · Proyecto Amazon Fill (nombre comercial Aquazonia) · Convención de evidencia: **[V]** verificado en el informe citado, **[E]** estimación del informe, **[R]** reseñas, **[NV]** no verificado, **[P]** propuesta de este capítulo a calibrar en soft launch, **[V-proto]** medido ejecutando `tools/levelgen_proto.py` en el equipo del dueño (Windows 11, Python 3.14.3, un solo hilo); se reproduce con el comando indicado en la sección 18. Los nombres siguen el glosario (`docs/gdd/00-glosario.md`).

## Resumen

Todos los niveles de campaña se producen offline con un pipeline determinista por semilla (generador → solver A* → métricas → índice D → colocación en el ciclo de 20 → curación → JSON → QA → publicación) y el cliente solo lee estado explícito; Remote Config reordena, sustituye y alivia sin nueva build. El prototipo Python escrito para este capítulo demostró tres hechos que cambian el diseño del informe de investigación: el barajado inverso estricto solo crea cortes de color y muere a profundidad 12-27, por lo que con 2 vacíos produce tableros fáciles, con 1 vacío tableros de dificultad dispersa y, con búsqueda en profundidad, tableros Difíciles/Muy difíciles de 1 vacío con garantía de solución en ~0,5 s; el relleno aleatorio con 1 vacío es resoluble en el 13 % (6 colores) → 0 % (12 colores) de los casos, así que los tableros de 1 vacío de 10-12 colores salen del inverso profundo, no del relleno; y la prueba de irresolubilidad con 1 vacío cuesta 1-8 ms, lo que hace barato el rechazo. La dificultad se mide con el bot casual ×40 (Difícil ≥75 %, Muy difícil ≥90 %) y se califica por cuantiles dentro del rango; el ciclo de 20 niveles fija posiciones exactas de reto (6, 10, 14, 17, 19), recuperación (1-2) e hito (20). Reintento determinista, alivio comunicado tras 3 fallos, Aguas turbias con ruta segura demostrada por enumeración de realidades (≤ 4 dosis ocultas, ≤ 24 realidades), 629 B por nivel (62,9 KB por pack de 100, 4,2 KB comprimido) y un plan de 700 niveles instalados (mundos 1-9; 08 §6.2) al lanzamiento con 200 curados.

## 1. Hechos que fijan el diseño

| # | Hecho | Consecuencia de diseño | Evidencia |
|---|---|---|---|
| 1 | Water-sort es NP-completo; con capacidad 4 y n colores hacen falta hasta ⌈3n/4⌉ vacíos para garantizar resolubilidad; con 1 vacío existen instancias irresolubles | Ningún nivel se genera "a ciegas": cada nivel lleva prueba (solución del solver o ruta inversa) | research/02 §1 [V] |
| 2 | Canonicalizar el estado (ordenar cuias) baja 14 cuias de 54 s a milisegundos; `max(cortes, dispersión)` recorta 4,6× los estados | Clave canónica obligatoria en solver, visitados y anti-repetición | research/02 §2.2 [V] |
| 3 | Un movimiento inverso legal solo puede **crear** un corte de color, nunca eliminarlo; una capa colocada sobre otro color queda fija para el resto del paseo | El paseo inverso aleatorio muere a profundidad 12-20 (mediana) / 26 (máx.) para 5-11 colores: la "profundidad" es un tope, no un objetivo; se devuelve el estado válido más profundo | [V-proto] sección 4.1 |
| 4 | Relleno aleatorio con 1 vacío: aceptación 13 % (K=6), 6,8 % (7), 3,2 % (8), 1,0 % (9), 0,27 % (10), 0,07 % (11), 0 % (12 en 1.500 candidatos); con 2 vacíos 100 % (75/75) | Los tableros de 1 vacío con K ≥ 9 salen del inverso profundo; el relleno aleatorio se reserva a 2 vacíos y a 1 vacío con K ≤ 8 | [V-proto] sección 4.2 |
| 5 | Probar irresolubilidad con 1 vacío cuesta 1-8 ms (espacio alcanzable minúsculo); resolver con 2 vacíos y 10 colores cuesta 0,1-4 s en Python | El rechazo es barato; el coste del pipeline lo marcan los tableros de 2 vacíos y las variantes de alivio | [V-proto] sección 5 |
| 6 | La dificultad percibida la predice la estrechez (1 vs 2 vacíos) y la densidad de decisiones, no el tamaño del tablero; el bot casual ×40 es el medidor publicado (Hard ≥75 %, Master ≥90 %) | 1 vacío como palanca principal desde ~31; bot casual como métrica de etiqueta | research/02 §4 [V]; dirección 4 y 5 |
| 7 | Royal Match: normal ≈1,2 intentos, hard ≈1,6, super hard ≈2,5; Candy Crush poda los 100 peores niveles de forma recurrente | KPI = intentos medios por nivel y abandono en nivel; poda mensual | research/02 §5-6 [V] |
| 8 | Colores ocultos "= azar" es la queja n.º 1 de Magic Sort; las herramientas que lo soportan enumeran permutaciones de las capas ocultas | Aguas turbias solo con ruta segura demostrada (sección 8) | research/09 §Mejorar [V]; research/02 §4 [V] |

Tres valores de K distintos (dirección 3; research/02 C3.2; 02 R2): **K_motor = 16** (4 bits por unidad; índices 1-14 = tintes, 15 = Barro, 16 reservado; Lluvia no tiene ID propio: usa los tintes agua del mundo, 10 o 14), **K_paleta = 14**, **K_ruta ≤ 12**; K = 13 solo en hitos desde el mundo 9 (Ucayali, nivel 561), en tableros de 14 cuias (13 llenas + 1 vacío); K = 14 solo en el evento Encuentro de las Aguas (sección 11), porque 14 colores + 1 vacío superan las 14 cuias generadas que fija 02 R1.

## 2. Arquitectura del pipeline offline

```
specs del rango  ──►  GENERADOR (3 fuentes por banda)  ──►  SOLVER A* (par, solución)  ──►  MÉTRICAS
(K, E, caps, mecánicas,     walk / deep / fill                 rechazo si par < 2K            bot ×40, ramificación,
 ventana de bot, pool                                                                          trampas, complejidad
 de tintes, max_pares)
      ──►  ÍNDICE D + CUANTILES  ──►  COLOCACIÓN EN SLOTS (ciclo 20)  ──►  VARIANTES DE ALIVIO
           dentro del pool del rango     salto máx. +2,2 / −3,4 bandas        +1 vacío / −1 color
      ──►  VALIDADOR  ──►  CURACIÓN (1-200 a mano)  ──►  EMPAQUETADO JSON  ──►  QA  ──►  PUBLICACIÓN
           paleta, debut, pares,                              mundo_{nn}_v{vuelta}.json     checklist     AAB / Addressables /
           K ≤ k_max, ≤14 cuias                               (sin campos de QA)                          Remote Config overrides
```

| Etapa | Entrada | Salida | Herramienta | Coste medido o estimado |
|---|---|---|---|---|
| Especificación | Tabla de la sección 11 y ciclo de la 12 | `spec` por slot: K, E, caps, mecánicas, ventana de fallo de bot, pool de tintes, `max_pares_riesgo` | YAML por mundo | — |
| Generación | `spec`, semilla `amazonfill:v1:m{mm}:v{v}:n{nnnn}` (ceros a la izquierda, 08 §4.1) | Estado inicial | `walk` 1-3 ms/intento; `deep` 0,4-0,6 s; `fill` 1 ms-4 s/candidato | [V-proto] |
| Solver | Estado | Par, solución, estados expandidos | A* canónico, presupuesto 300.000 | mediana 18 ms, media 182 ms, máx. 1,5 s (Python) [V-proto] |
| Métricas | Estado + solución | fallo bot, ramificación, trampas, complejidad | bot ×40 (20-63 ms), trampas (0,16-0,22 s) | [V-proto] |
| Índice D y cuantiles | Pool completo del rango (≥3× los slots) | D, banda, etiqueta | script | ms |
| Colocación | Pool calificado + plantilla del ciclo | nivel → slot | script con restricciones (sección 12) | ms |
| Alivio | Nivel colocado | Variante `+1 vacío` (siempre resoluble) y `−1 color` (verificada) | solver | mediana 1,1 s; sin resolver `+1 vacío` baja a <0,3 s [V-proto] |
| Validador | Nivel + paleta global | aceptado / rechazo con motivo | script | ms |
| Curación | Niveles 1-200 y todos los hitos | Ajustes manuales re-verificados | editor interno (Unity) | 20-25 niveles/día por diseñador [E] |
| Empaquetado | Niveles aceptados | `mundo_{nn}_v{vuelta}.json` minificado, sin `qa` ni `solution` | script | 629 B/nivel; 62,9 KB/100 [V-proto] |
| QA | Pack | Checklist de la sección 17 firmada | 2 pasadas humanas en retos e hitos; 1 en comodidad | ~3 min/nivel [E] |
| Publicación | Pack aprobado | AAB (1-700, mundos 1-9), Addressables remotos (701+), `level_overrides` en RC (08 §6.2) | CI (GitHub Actions) | — |

Reglas transversales: (1) la **semilla** fija al generador, al bot y a los muestreos de trampas: regenerar un nivel con la misma versión del generador produce el mismo JSON (test de CI: hash idéntico); (2) el pack lleva `gen_version` y el cliente nunca regenera: si cambia el código del generador, cambia el pack, no el nivel que ya tiene el jugador (research/02 §5 [V]); (3) `level_overrides` en Remote Config mapea `slot → level_id` para sustituir un nivel por otro del pool o por su variante aliviada sin build; el pool publicado lleva 20 % de niveles de reserva por rango para estas sustituciones [P].

## 3. Representación del estado y canonicalización

```
# Tipos
Cuia      = lista de capas de abajo arriba, cada capa un índice de motor 1..K_motor (1-14 tintes, 15 Barro, 16 reservado); no existe el valor "sin capa": la longitud la da len
Estado    = tupla de T cuias + contadores        # el orden visual lo da el JSON; el solver usa la forma canónica
                                                 # contadores solo con Lluvia/Piraña: vertidos mod k, gotas_pendientes, ultimo_destino (02 §9.2)
caps[i]   = capacidad de la cuia i (1..6)        # 4 por defecto; Cuia chica 1-3; Cuia alta 5-6
units[c]  = unidades totales del color c         # 4 por defecto; 5-6 para la "fruta abundante" alojada en Cuia alta; el agua de Lluvia cuenta sus gotas pendientes (02 R3)
attr[i]   = {tipo, key, partner, n, k}           # mecánicas (sección 9, reglas de 02 §9.1): n = sellos (Liana) o gotas programadas (Lluvia), k = ritmo en vertidos (Lluvia, Piraña); vacío en niveles puros; las dos cuias de un Remolino se canonicalizan como unidad

# Codificación compacta en memoria (BoardState, canon 08 §4.1): len (3 bits, 0-6) + un nibble por unidad con el valor tinte − 1 (0-15)
pack(cuia)       = len(cuia) ‖ Σ_k (capa[k] − 1) << 4·k    # caben los 16 índices sin reservar el 0 para "sin capa"; cap 4 → 3 + 16 bits; 14 cuias ≈ 34 bytes. El JSON nunca usa esta forma ni la hexadecimal
hash_estado      = hash64( ordenar( (caps[i], attr[i], pack(cuia_i)) para i en 0..T−1 ), contadores )   # ultimo_destino como índice canónico

# Predicados (02 R4, R5, R12)
racha(cuia)      = (color_superior, longitud de la racha superior contigua y visible)
sellada(i)       = len(cuia_i) == caps[i] ∧ monocolor ∧ todas las capas visibles    # "Cuia sellada" = LLENA, monocolor y visible (02 R5)
vacia_util(i)    = len(cuia_i) == 0
victoria(estado) = ∀ i: vacia_util(i) ∨ sellada(i)                                  # 02 R12
canon(estado)    = tupla ordenada de (caps[i], attr[i], cuia_i) + contadores        # elimina las T! permutaciones de cuias
forma(estado)    = canon tras renombrar colores por orden de primera aparición  # invariante a permutar colores: anti-repetición
```

Capacidades mixtas (02 R5, R12 y §8; dirección 2, "recipiente completo se sella"): una cuia está **sellada** solo si está **llena** (`len == caps[i]`), es monocolor y todas sus capas son visibles; "monocolor" en la dirección 2 significa monocolor **y llena**, como en todos los líderes del género. Con capacidad 4 y 4 unidades por color es exactamente "llena de su color". **Cuia alta**: el generador aloja en **cada** Cuia alta una **fruta abundante** con `units[c] == caps[i]` (5 o 6), la única que puede sellarla; una fruta de 4 unidades puede entrar y salir de ella, pero cuatro dosis de esa fruta no la llenan ⇒ no sella y el jugador tiene que sacarla a una cuia de capacidad 4 (02 §8). **Cuia chica** (1-3): `caps[i] < units[c]` para todo color ⇒ nunca sella ⇒ termina vacía. El validador comprueba (QA #5) que toda Cuia alta lleva su fruta abundante y que para cada color existe al menos una cuia con `caps[i] == units[c]`. Así el solver, el motor y la UI usan el mismo predicado que el cliente (02 R5/R12) y no existen niveles "sin victoria posible" (research/01 Especificación 22 [E]).

## 4. Generadores

El prototipo midió que ninguna fuente sirve para todas las bandas: se usan tres y la banda del slot decide cuál (tabla 4.4).

### 4.1 Barajado inverso con "mejor prefijo" (`walk`)

```
MOVIMIENTOS_INVERSOS(S):                          # inverso exacto de un vertido legal b→a
  para cada cuia a no vacía: (c, r) = racha(a); entera = (r == len(a))
    para m en 1..r:
      si m == r y no entera: continuar            # extraer toda la racha dejaría otro color arriba: el vertido directo sería ilegal
      para cada cuia b ≠ a:
        si caps[b] − len(b) < m: continuar        # no cabe
        si b no vacía y top(b) == c: continuar    # el vertido directo debe mover exactamente m (b sin c arriba)
        si b vacía y entera y m == r: continuar   # volcar una monocolor entera en un vacío = vertido directo fútil
        emitir (a → b, m)

WALK(K, E, caps, units, tope, semilla, compl_min):
  rng = PRNG(semilla)
  S = [K cuias llenas de su color] + [E vacías]; vistos = {canon(S)}; mejor = ∅
  repetir hasta 'tope' veces:
    cands = [(aplicar(S,m), rng()·2 − 4·[vacíos(S') < E]) para m en MOVIMIENTOS_INVERSOS(S) si canon(S') ∉ vistos]
    si cands = ∅: romper                           # el paseo muere: solo se crean cortes, nunca se deshacen
    S = uno de los 3 mejores por puntuación; vistos += canon(S)
    si ¬victoria(S) ∧ ∄ sellada ∧ complejidad(S) ≥ compl_min ∧ vacíos(S) == E: mejor = S
  devolver mejor                                  # None → reintentar con otra sub-semilla (hasta 200)
complejidad(S) = 2·transiciones + 2·cuias_mezcladas + cuias_parciales      # laicwben2 (research/02 §3 [V])
```

Medido [V-proto]: 1-3 ms por intento; profundidad alcanzada mediana 12-20, máx. 26; con 200 intentos los dos lotes de 20 niveles nunca fallaron (1-21 intentos por nivel); fallo de bot 0,00-0,47 con 2 vacíos y 0,03-0,97 con 1 vacío; par ≈ 2K-2,4K. Es la fuente de comodidad y recuperación (con rechazo por ventana de bot, sección 4.4), de Río sin fin y de Pororoca (garantía por construcción, sin solver).

### 4.2 Barajado inverso en profundidad (`deep`)

Misma regla de movimiento inverso, pero el paseo se explora como DFS con retroceso hasta `max_nodes` estados y se devuelve el estado válido más profundo. Medido [V-proto] con 20.000 nodos: 0,4-0,6 s por nivel; profundidad 18-27; fallo de bot 0,68-0,97 para K = 8-11 con 1 vacío; par 17-25 (≈ 2K+1..3). Es la fuente de reto e hito con 1 vacío para K ≥ 9, donde el relleno aleatorio no produce tableros resolubles, y conserva la prueba de resolubilidad por construcción.

```
DEEP(K, E, caps, units, max_nodes, semilla, compl_min):
  pila = [(S0, ∅, 0)]; vistos = {canon(S0)}; mejor = (0, None); nodos = 0
  mientras pila ≠ ∅ y nodos < max_nodes:
    (S, cands, d) = cima(pila)
    si cands = ∅ sin calcular: cands = barajar(MOVIMIENTOS_INVERSOS(S) no vistos)
    si cands vacío: desapilar; continuar
    S' = aplicar(S, cands.pop()); vistos += canon(S'); nodos += 1
    si válido(S') y d+1 > mejor.prof: mejor = (d+1, S')
    apilar (S', ∅, d+1)
  devolver mejor.estado
```

### 4.3 Relleno aleatorio + rechazo (`fill`)

```
FILL(K, E, caps, units, semilla, max_cand):
  rng = PRNG(semilla)
  repetir max_cand veces:
    unidades = [c repetido units[c] veces para c en 1..K]; Fisher-Yates(unidades, rng)
    repartir en las K primeras cuias respetando caps; añadir E vacías
    si ∃ sellada: continuar
    sol = ASTAR(S, presupuesto 300.000)
    si sol.estado == resuelto ∧ par ≥ 2K: devolver S, sol
  devolver None
```

Medido [V-proto]: con 2 vacíos todo candidato fue resoluble (K = 8: 30/30, K = 10: 45/45) y el fallo de bot se reparte de 0,07 a 1,00 con par 22-34 (≈ 3K): es la fuente de mayor par y de "tableros densos" para reto e hito con 2 vacíos y para el tramo 11-30. Con 1 vacío la aceptación cae con K (tabla de la sección 1, fila 4) y el coste por nivel aceptado es 1-3 s hasta K = 9 (candidatos de 1-8 ms) y 18 s para K = 10 (3.000 candidatos, 8 aceptados): se permite hasta K = 8 por simplicidad. Anclas (dos unidades inferiores iguales en 3-4 cuias) no mejoraron la aceptación (0,40 % vs 0,27 % en K = 10) [V-proto].

### 4.4 Fuente por banda

| Banda del slot | Vacíos | K | Fuente | Ventana de fallo de bot objetivo | Razón |
|---|---|---|---|---|---|
| Recuperación | 2 | K del rango −1 | `walk` | < 30 % | Candy Crush: 2 niveles fáciles tras cada pico (research/02 §5 [V]) |
| Comodidad | 2 (1-30) · 1 (31+) con 2 en el 30 % de los slots; salvo debuts: 2 vacíos, bot < 20 % (02 §9.2 regla 2; sección 12) | K del rango | `walk` (K ≤ 8) · `walk`/`deep` corto (K ≥ 9) | 20-60 % (debuts: < 20 %) | Fuente barata y garantizada |
| Reto (sin etiqueta, 21-40) | 2 | K del rango | `fill` E=2 | 50-70 % | Densidad alta sin estrechez |
| Reto Difícil (41+) | 1 | K del rango | `deep` (K ≥ 9) · `fill` E=1 (K ≤ 8) | ≥ 75 % | Estrechez + garantía |
| Hito Muy difícil (60+) | 1 | K máx. del rango (13 desde 561, en 14 cuias; 14 solo en el evento Encuentro de las Aguas) | `deep`; alternativa `fill` E=2 con K+1 (nunca por encima de 14 cuias) | ≥ 90 % | Pico del tramo |
| Tutorial 1-10 | 2 (excepciones de 02 §13: niveles 1 y 6 con 1 vacío) | 2-5 | A mano en el editor + verificación; la tabla de 02 §13 (cuias = colores + vacíos) se copia sin alterar | 0-10 % | Enseña Marea (3; ilimitada hasta el 10, `undo_unlimited_until_level`), Volver a la orilla (6 si hay Aguas estancadas; 8 primer reinicio probable), Cuia extra gratis (8); resto de hitos en 02 §13 (research/05 §Primera sesión) |

Rechazo por ventana: la fuente genera candidatos hasta que `bot_fail` cae en la ventana del slot (máximo 30 candidatos; coste 20-60 ms de bot por candidato). Si ningún candidato entra, la comodidad relaja en este orden: añadir una Cuia chica de 2 unidades como "medio vacío", luego pasar a 2 vacíos; el reto y el hito relajan hacia `fill` con 2 vacíos y K+1. La necesidad quedó medida: los ocho tableros de 1 vacío del lote híbrido dieron fallo de bot 0,88-1,00 sin importar la fuente, así que un slot de comodidad con 1 vacío sin rechazo saldría etiquetado Difícil (sección 18.1).

### 4.5 Elección de tintes

El generador trabaja con índices 1..K y el empaquetador asigna tintes del pool del mundo (glosario §9): se eligen K tintes del pool con `nivel_debut ≤ nivel`, maximizando la presencia del tinte que debuta en el mundo durante sus 3 primeros niveles, y rechazando combinaciones cuyo número de pares de riesgo (4-5, 5-6, 6-13, 13-14, 14-4, 3-12, 12-11, 11-10, 8-9, 1-2) supere `max_pares_riesgo` del nivel (0 en 1-40, 1 en 41-80, 2 en 81-200, libre desde 201; dirección 3). El conjunto máximo de tintes mutuamente seguros es 7 (research/02 C3.2 [E]), por eso 41-80 juega 7-8 colores con un par permitido. Dentro de una cuia, el empaquetador evita además que dos tintes de un par de riesgo queden adyacentes cuando `max_pares_riesgo` > 0, permutando la asignación índice → tinte (hasta 50 permutaciones; si ninguna lo logra, se acepta y se marca `risk_adjacent: true` para la curación) [P].

## 5. Solver A*/IDA*

```
H(S) = max( h1 = Σ_cuias (segmentos − 1),            # cortes de color: un vertido elimina a lo sumo uno
            h2 = Σ_colores (cuias que contienen c − 1) )   # dispersión: un vertido saca un color de a lo sumo una cuia
# Ambas admisibles y consistentes → A* con w=1 devuelve el par exacto (research/02 §2.2 [V]).

MOVIMIENTOS(S):                                       # legales y no fútiles = movimientos_utiles(S) de 02 §1.4
  para i con cuia_i no vacía y no sellada: (c, r) = racha(i); vacío_usado = {}   # por (capacidad, attr)
    para j ≠ i con espacio = caps[j] − len(j) > 0:
      si cuia_j vacía:
        si futil(i, j) o vacío_usado[(caps[j], attr[j])]: saltar   # `futil` única de AF.Core (02 §1.4): mono→vacío solo recoloca, salvo "casa correcta" (caps[j] == units[c] ≠ caps[i]) o destino Lluvia/Remolino; los vacíos equivalentes se expanden una sola vez
        vacío_usado[(caps[j], attr[j])] = verdadero
      sino si top(j) ≠ c: saltar
      emitir (i → j, min(r, espacio))                                            # siempre la racha completa que cabe
# `legal`, `cantidad`, `espacio_efectivo` y `futil` son las de 02 §1.2 y §1.4 (misma implementación en AF.Core para solver, bot, Aguas estancadas y Morpho); el solver no mantiene una poda propia. Con mecánicas (sección 9) se usan las versiones extendidas de 02.

ASTAR(S0, presupuesto, w):
  abierto = heap[(w·H(S0), 0, S0)]; g = {canon(S0): 0}; padre = {}
  mientras abierto:
    (f, gs, S) = pop_min; si gs > g[canon(S)]: continuar          # entrada obsoleta
    si victoria(S): devolver reconstruir(padre, S)                # par = gs
    expandidos += 1; si expandidos > presupuesto: devolver PRESUPUESTO
    para m en MOVIMIENTOS(S): S' = aplicar(S, m); k = canon(S')
      si k ∉ g o gs+1 < g[k]: g[k] = gs+1; padre[k] = (S, m); push((gs+1 + w·H(S'), gs+1, S'))
  devolver IRRESOLUBLE                                            # espacio agotado: prueba de irresolubilidad
```

| Uso | Lugar | Presupuesto | Peso w | Resultado | Medido / estimado |
|---|---|---|---|---|---|
| Par y solución de campaña | Pipeline offline | 300.000 expandidos | 1 | Óptimo | mediana 18 ms, máx. 1,5 s (K=10, E=2, 18.291 estados) [V-proto]; en C# 10-30× menos [E] |
| Prueba de irresolubilidad (rechazo, trampas) | Pipeline | 3.000-300.000 | 1 | Agotamiento del espacio | 1-8 ms con 1 vacío [V-proto] |
| Morpho (pista) | Cliente, hilo de fondo | 40.000 (por realidad en Aguas turbias; solo vertidos seguros, sección 8) | 2 | Siguiente movimiento | mediana 2-3 ms, máx. 32 ms, subóptimo medio +0,2-0,5 (máx. +3) [V-proto]; <50 ms en móvil [E] |
| Pororoca (verificación del par tras barajar) | Cliente | 40.000 | 2 | par' ≤ par + 2 | <50 ms [E] |
| Río sin fin (par y anti-trivialidad) | Cliente | 40.000 → reintento 400.000 (research/02 C4.3 [V]) | 2 → 4 | Par aproximado | <300 ms peor caso en gama media [E] |
| Aguas turbias (seguridad por realidad) | Pipeline | 20.000 por realidad | 1 | Resoluble sí/no | 0,7-8,5 s por nivel con 12 realidades (Python) [V-proto] |

IDA* se reserva como variante del cliente si la memoria del `visited` fuese un problema en gama baja (14 cuias × (3 + 16) bits ≈ 34 bytes por clave con la codificación de la sección 3; 40.000 claves ≈ 1,4 MB más overhead; no se espera necesitarlo) [E]. El cliente ejecuta el solver en un hilo (Unity `Task`/Job) y cancela por tiempo (`hint_timeout_ms`); si se agota, Morpho responde con "No encuentro un camino desde aquí" y ofrece Marea o Volver a la orilla (research/01 Especificación 14).

## 6. Bot casual ×40

```
PUNTUACION_CASUAL(S, m=(i→j, n)):                 # "lo que parece bueno", sin anticipación
  puntos = n                                       # mover más unidades
  si cuia_j no vacía: puntos += 4                  # juntar con el mismo color
     si monocolor(j) y len(j)+n == caps[j]: puntos += 10      # sella una cuia (llena y monocolor, 02 R5)
  sino: puntos −= 5                                # gastar un vacío parece caro
  si n < racha(i): puntos −= 3                     # vertido parcial
  si deja expuesto un color con destino disponible: puntos += 2
  devolver puntos

BOT(S, rng, tope=400, p_greedy=0,85):
  repetir tope veces:
    si victoria(S): devolver ÉXITO
    M = MOVIMIENTOS(S); si M = ∅: devolver FALLO (Aguas estancadas)
    m = argmax PUNTUACION + rng()·0,5 con prob. p_greedy, si no uniforme en M
    S = aplicar(S, m)
  devolver FALLO (tope)
fallo_bot = fallos en 40 partidas con semilla derivada del nivel / 40
```

Parámetros de requiemformemories (40 partidas, 85/15, tope 400) (research/02 §4 [V]). La función de puntuación es la del prototipo [P]: debe congelarse en C# antes de calificar el primer pack y recalibrarse una sola vez contra los intentos medios reales tras 2 semanas de soft launch (regresión de intentos medianos sobre fallo_bot, par−2K, ramificación, trampas y [E==1]); cualquier cambio posterior exige recalcular etiquetas de todo el pool, nunca de un pack aislado. Coste medido: 20-63 ms por nivel (más en tableros largos) [V-proto].

## 7. Métricas e índice D

| Métrica | Definición | Rango observado [V-proto] | Peso en D |
|---|---|---|---|
| `bot_fail` | Fallos del bot casual / 40 | 0,00-1,00 | 0,35 |
| `par` − 2K | Movimientos óptimos menos el mínimo de rechazo | −1 … +14 | 0,25 |
| `branching` | Media de vertidos legales no fútiles a lo largo de la solución óptima | 2,2-7,9 | 0,20 |
| `traps` | Fracción de vertidos legales (desde los estados de la ruta óptima, ≤120 pares muestreados) que llevan a un estado sin solución, confirmado por A* agotado o deadlock inmediato | 0,00-0,33 | 0,10 |
| `[E == 1]` | Indicador de estrechez | 0/1 | 0,10 |
| `complexity` | 2·transiciones + 2·mezcladas + parciales (filtro, no entra en D) | 28-66 | — |

```
D(nivel) = 0,35·z(bot_fail) + 0,25·z(par − 2K) + 0,20·z(branching) + 0,10·z(traps) + 0,10·[E == 1]
z(x) = (x − μ_rango) / σ_rango          # estadísticos del POOL del rango (≥ 3× los slots), no del pack
banda(nivel) = cuantil de D dentro del pool del rango: recuperación ≤ 0,10 · comodidad ≤ 0,70 · reto ≤ 0,95 · hito > 0,95
etiqueta(nivel) = Muy difícil si bot_fail ≥ 0,90 · Difícil si ≥ 0,75 · Normal en otro caso   # solo el bot decide la etiqueta
```

Pesos de research/02 §7.1 [E]. La banda coloca; la etiqueta se muestra y paga (20 / 60 / 80 semillas, dirección 12). Un nivel en slot de reto cuyo bot_fail quede por debajo de 0,75 se publica sin etiqueta (es honesto) y la curación decide si se sustituye por otro del pool.

## 8. Aguas turbias: generación con ruta segura

Semántica (glosario §6.2; research/01 Especificación 7 y 23): en una cuia turbia solo la capa superior es visible; las demás se muestran como agua parda con "?" y se revelan al quedar expuestas. Una capa oculta no forma parte de la racha visible aunque coincida de color. Límites (02 T2; claves de 08 §9): solo niveles etiquetados Difícil/Muy difícil desde el 100 (`lvl_murky_from_level`); máximo 3 cuias turbias (`lvl_murky_max_cuias`) y **máximo 4 dosis ocultas** por nivel (`lvl_murky_max_hidden_units` = 4; **≤ 24 realidades**, que el cliente enumera en <50 ms para Morpho, pista por inactividad y verificación de rescate). El límite anterior de 6 dosis / 720 realidades queda descartado: solo era tratable offline y rompía el presupuesto del solver en cliente (08 §2.2). La realidad verdadera cumple la **regla T4** de 02 §11 (bajo una dosis oculta nunca se repite el color de la dosis inmediatamente superior), que el tutorial comunica al jugador y que el generador garantiza (si el tablero base la viola, el nivel se rechaza); nunca en Río sin fin ni en niveles con mecánicas con disparador (sección 9). No existe una "Pista parcial": Morpho propone solo vertidos seguros o no cobra (abajo).

```
RUTA_SEGURA(S_real, turbias, prof_oculta):
  ocultas = {(i, capa) : i ∈ turbias, capa bajo la superior hasta prof_oculta}
  si ¬T4(S_real, ocultas): devolver RECHAZAR                                  # T4 (02 §11): ninguna posición oculta repite el color de la capa inmediatamente superior
  S = S_real; ruta = []
  repetir hasta 400 veces:
    si victoria(S): devolver OK, ruta
    ocultas = {(i,capa) ∈ ocultas : capa < len(cuia_i) − 1}                 # lo expuesto queda revelado
    visible = S con 0 en las posiciones ocultas
    multiconjunto = colores reales de las posiciones ocultas                   # el jugador lo conoce: units[c] por color (`units_per_color`)
    R = asignaciones(ocultas, multiconjunto, T4)                                # realidades consistentes con lo visible, con units_per_color y con T4 (02 §11 validar_turbio); ≤ 24, si más → RECHAZAR nivel
    seguros = [m ∈ MOVIMIENTOS(visible) : ∀ r ∈ R, ASTAR(aplicar(r, m)) == resuelto]   # memo por canon
    si seguros = ∅: devolver FALLO                                              # nivel rechazado
    m = el seguro con menor par en S_real; ruta += m; S = aplicar(S, m)
```

Garantía: en cada estado de la ruta existe ≥ 1 vertido que no puede arruinar la partida sea cual sea la realidad oculta (dirección 6). Morpho en un nivel turbio devuelve **solo** vertidos seguros, evaluados sobre las realidades aún posibles (lo revelado reduce R); si el jugador se desvió de la ruta y no existe ningún vertido seguro desde el estado actual, Morpho responde "La morpho no ve bajo el agua turbia: ilumina una cuia con Luciérnaga" (04 §2.3 caso a) y **no cobra**; nunca propone un vertido que pueda arruinar la partida en alguna realidad (honestidad, direcciones 5 y 6). Si la enumeración agota `hint_timeout_ms`, responde "No encuentro un camino desde aquí" y tampoco cobra (sección 5). Cada respuesta sin vertido seguro queda registrada en `af_hint_result` (taxonomía de 08 §8.2; parámetros de 04 §11, entre ellos `realities`) para calibrar `lvl_murky_max_hidden_units` [P]. Luciérnaga revela todas las capas de una cuia turbia y por tanto reduce R; Pororoca en un nivel turbio revela todas las capas antes de barajar (la ola limpia el barro; research/02 C4.5 [E]). Medido [V-proto]: 5 de 5 niveles de 8 cuias con 2 cuias turbias × 2 capas ocultas (4 dosis, el máximo canónico) tuvieron ruta segura; 12 realidades máximas; 0,7-8,5 s por nivel en Python (en C# < 1 s [E]). El campo `hidden` del JSON (lista `[cuia, capa]`, 08 §4.1) lista las posiciones ocultas y `safe_path` se guarda solo para QA.

Modo bonus **Aguas turbias · Bonus** (1.1; `ui_murky_bonus_enabled` = false en 1.0 y true en 1.1, 08 §9; 06 §4.1): el mismo tablero del Reto del río con 2 cuias turbias verificadas y premio doble, ofrecido como tarjeta opcional (sección 16); comparte generador y verificación.

## 9. Capacidades mixtas y mecánicas especiales: semántica para el solver derivada de 02 §9.1

El catálogo, la regla exacta y el nivel de debut de cada mecánica los fija el capítulo 02 (dirección 8): la columna "Regla exacta" de esta tabla **copia literalmente 02 §9.1** (contrato del solver y del validador) y la columna "Debut" copia `mech_debut_level` de 02 §Parámetros; cualquier cambio se hace primero en 02 y se propaga aquí. Regla general: la clave canónica incluye `(cap, tipo, key, partner, n, k, flags)` de cada cuia (campos de `attrs`, sección 10) más los contadores de Lluvia y Piraña (sección 3); un disparador que dependa solo del contenido (número de cuias selladas) no añade estado.

| Mecánica (glosario §6.5) | Regla exacta (02 §8 y §9.1) | Estado extra | Cambio en MOVIMIENTOS / victoria | Heurística | Generación | Coste solver [E] | Debut (02 `mech_debut_level`) |
|---|---|---|---|---|---|---|---|
| Cuia chica (1-3 u.) | Búfer: `cap < U_c` para todo color ⇒ nunca sella ⇒ debe terminar vacía. Cuenta como "vacío" para la aritmética del nivel, pero de valor reducido | ninguno | `caps[i]` | sin cambio | `walk`/`deep`/`fill` nativos | ×1 | 14 (cap 2) · 18 (cap 1) · 22 (cap 3) |
| Cuia alta (5-6 u.) + fruta abundante | Llega con una **fruta abundante**: un color con `U_c = cap` que solo puede sellar en ella. Las frutas de 4 dosis pueden entrar y salir, pero una Cuia alta llena de 4 dosis de otra fruta no está llena ⇒ no sella (R5): el jugador tiene que sacarla | ninguno | `caps`, `units` | sin cambio (h1, h2 válidas) | nativos | ×1-2 (más estados) | 27 (cap 5) · 141 (cap 6) |
| **Hoja** (M1) | Cuia **vacía** cubierta por una hoja de palma con el icono de un color c. Solo acepta vertidos de c mientras tenga hoja (`legal` en 02 §1.2); con el primer vertido de c la hoja se desprende y la cuia pasa a ser normal. No es origen mientras tenga hoja (está vacía). Es un "vacío reservado" | **1 bit por Hoja** (hoja presente; no se deriva del contenido: una Hoja abierta y vaciada de nuevo ya no tiene hoja) | filtro en destino: `top(i) == c ∧ espacio > 0`; flag en `apply` | h1/h2 siguen siendo cotas inferiores | base + superposición sobre un vacío + verificación A* | ×1-1,5 | 43 |
| **Brote** (M2) | Cuia con 1-3 dosis visibles y un tapón-brote en la boca cuyo color es el de su cima. **No es origen** mientras esté cerrado; como destino sigue la regla normal (mismo color). Al recibir un vertido de su color, el brote germina (se abre) y la cuia queda normal | **1 bit por Brote** (tapón presente) | filtro en origen; flag en `apply` | sin cambio | superposición + verificación | ×1,5-2 | 63 |
| **Liana** (M3) | Cuia (vacía o con contenido visible) atada por una liana con un número n (1-2). No es origen ni destino. Se desata en el instante en que el jugador **sella** n cuias desde el inicio del intento. Reiniciar vuelve a atarla | ninguno en la clave canónica (derivado: número de cuias selladas en el estado, que solo crece; el cliente guarda el contador, 02 §9.2) | filtro en origen y destino hasta `selladas(S) ≥ n` | h2 no cuenta la cuia atada como vacío disponible; sigue siendo cota | superposición + verificación | ×1 | 83 |
| **Remolino** (M4) | **Par** de cuias unidas por debajo. Tocar una como destino siempre significa remolino: la racha vertida entra por el **fondo** de la pareja, de cualquier color (no aplica la regla de mismo color); requiere `espacio(pareja) ≥ 1` y pareja no sellada; `cantidad = min(racha, espacio(pareja))`. Como origen, cada cuia del par vierte su cima con la regla normal. Ambas sellan como cualquier cuia; si una sella, la otra ya no puede recibir | ninguno, pero la **clave canónica conserva el emparejamiento** (`partner` en `attr`, simétrico en ambas; las dos cuias del par se ordenan como unidad, sección 3) | **transición distinta**, no una redirección cosmética: inserción al fondo de `pareja(j)`; legalidad sobre la pareja; destino Remolino nunca fútil (02 §1.4) | sin cambio (un vertido sigue eliminando ≤ 1 corte y sacando un color de ≤ 1 cuia) | superposición + verificación (el inverso extrae la racha del fondo de la pareja, 02 §9.2) | ×1 | 103 |
| **Barro** (M5) | Dosis inerte (índice 15) en posición 2 o 3 de una cuia de 4. No se puede verter (no es un color, corta rachas). Cuando queda **expuesta** en la cima, la cuia acepta **cualquier** color: el primer vertido lava el barro y ocupa su lugar (`espacio_efectivo + 1`). Con barro en la cima la cuia no es origen | ninguno (el barro es una capa más) | `legal`: destino con barro expuesto acepta cualquier color; `cantidad` usa `espacio_efectivo + 1`; con barro en la cima no es origen; victoria sin cambio: una cuia con barro no está sellada ni vacía, hay que lavarlo | h1 cuenta el barro como corte y resta 1 por cada barro presente (el lavado puede eliminar dos cortes en un vertido); h2 ignora el color 15 | superposición (sustituye la capa 2 o 3 por 15 y retira una unidad de ese color) + verificación | ×1 | 123 |
| **Lluvia** (M6) | Cuia abierta al cielo con nube y `gotas_pendientes`. Cada `lluvia_k_pours` (3) vertidos del jugador cae **una dosis del tinte agua del mundo** (Agua del Tapajós, tinte 10, en aguas blanca/clara; Agua del Río Negro, tinte 14, en negra) **encima** de lo que haya, sin regla de color, si hay espacio; si está llena, la gota espera al siguiente vertido. `U_agua` = dosis iniciales + gotas programadas = 4. La cuia Lluvia solo sella con agua; si el agua sella en otra cuia, la Lluvia debe terminar vacía. Al caer la última gota la nube se va | **sí**: `gotas_pendientes` y contador `vertidos mod k`, ambos en la clave canónica; **añade unidades** (el tinte 16 no se usa: 02 R2) | tick en `apply` cada k vertidos (orden 02 R10: tras el sellado, antes de victoria); destino Lluvia nunca fútil; `units[agua] = 4` cuenta las gotas pendientes (02 R3) | h1/h2 sobre el estado actual (una gota nunca elimina cortes: cota válida) | superposición + verificación con solver extendido; prohibida con Aguas turbias; en fuente inversa las gotas se generan hacia atrás (quitar agua de la cima cada k, 02 §9.2) | ×2-3 | 143 |
| **Piraña** (M7) | Una piraña ronda el tablero con contador de `pirana_k_pours` (4). Al cumplirse, **muerde** la dosis superior de la **última cuia destino** del jugador (si no está sellada) y la lleva a su **cueva** (una cuia marcada, normalmente Cuia chica de 2). Si la cueva está llena, la piraña duerme hasta que se vacíe una dosis. El jugador vierte desde la cueva con la regla normal. Sellar la cuia destino en el vertido n·k evita el mordisco | **sí**: contador `vertidos mod k` y `ultimo_destino` (índice canónico de la cuia), ambos en la clave canónica | tick en `apply` cada k vertidos (orden 02 R10); la cueva es origen normal y, como Cuia chica, termina vacía; victoria sin cambio | h1/h2 dejan de ser cotas estrictas (un mordisco puede juntar color sin gastar vertido): el par con Piraña se calcula con A* w=1 y se marca aproximado en el JSON [P] | superposición (cueva = Cuia chica de 2 vacía) + verificación; prohibida con Aguas turbias | ×2-3 | 183 |
| Aguas turbias | Sección 8 | posiciones ocultas (no afectan al solver de la realidad) | sin cambio | sin cambio | enumeración de ≤ 24 realidades | ×\|R\| (≤ 24) | 100 (hito Muy difícil; `lvl_murky_from_level`) |

Consecuencias: (1) **Hoja y Brote añaden 1 bit por cuia; Lluvia añade `gotas_pendientes` y el contador de vertidos; Piraña añade el contador y `ultimo_destino`**: los cuatro entran en `hash_estado` y en `canon` (sección 3); Liana se deriva del número de cuias selladas; Remolino no añade estado pero cambia la transición y su canonicalización conserva el emparejamiento (no es cosmético); solo Barro es de pura representación (02 §9.2). (2) Toda mecánica se genera como **superposición sobre un tablero base** (`walk`/`deep`/`fill`) seguida de verificación con el solver extendido: la garantía por construcción del inverso no sobrevive a los disparadores, así que ninguna mecánica entra en Río sin fin, y Pororoca en un nivel con mecánicas rebaraja solo las unidades libres y exige re-verificación con A* a 40.000 nodos (si falla, Pororoca se desactiva en ese nivel con el texto "La ola no puede entrar aquí"); en niveles con Lluvia o Piraña Pororoca está deshabilitada (02 §9.2) [P]. (3) Cada mecánica debuta en **2-3 tableros fáciles consecutivos** (comodidad o recuperación, fallo de bot < 20 %, 2 vacíos; 02 §9.2 regla 2; patrón Royal Match, research/02 §5 [V]) con un solo ejemplar y sin otras mecánicas, a partir de su nivel canónico, y ningún reto ni hito la usa antes de esos tableros. Los siete bloqueadores de 02 §9.1 (43, 63, 83, 103, 123, 143, 183) caen por construcción en la **posición 3** del ciclo de la sección 12 (`p = ((n − 1) mod 20) + 1`) y ocupan las posiciones 3-5 como comodidad, con 2 vacíos y ventana de bot < 20 % (la única excepción de la plantilla, nota de la sección 12); Cuia chica (14) y Cuia alta (27) debutan en el tramo 1-30 construido a mano, fuera del ciclo, y la Cuia alta de 6 solo aparece en hitos desde el 141 (primero: 160; 02 §8). Cadencia (02 §9): un bloqueador cada 20 niveles de 43 a 143 y el séptimo en 183; desde el 183 solo combinaciones (máximo 2 mecánicas distintas por nivel antes del 300 y 3 después, 02 §9.2) y mecánicas nuevas a definir en live-ops (dirección 8).

## 10. Formato JSON del nivel

**Ver 08 §4.1.** El esquema único `af-level/1` (cabecera de pack, campos de nivel y ejemplo) lo fija 08 §4.1 y se formaliza en `schemas/af-level-1.schema.json` (raíz del repositorio, única copia; JSON Schema draft 2020-12), que `tools/level-generator` emite y `tools/level-qa` valida; este capítulo es dueño de la **semántica** de los campos de generación y dificultad y no define variantes propias ni conserva un ejemplo distinto. Resumen idéntico al canon, para el generador y el validador:

- `id` `m{mm}_v{v}_n{nnnn}` y `seed` `amazonfill:v1:m{mm}:v{v}:n{nnnn}`, ambos con ceros a la izquierda (`m03_v1_n0043` / `amazonfill:v1:m03:v1:n0043`, el ejemplo de 08 §4.1: debut de Hoja); el test de reproducibilidad por hash exige el padding exacto. Las variantes aliviadas no tienen `id` propio. El pool del Reto del río usa su propio formato de `id` y `seed` (sección 16).
- `cuias` = listas de **índices de motor** de `tints.json` (1-14 tintes, 15 = Barro, 16 reservado), de abajo arriba, `[]` vacía; `palette` solo lista los tintes presentes (K = longitud) para validar debuts y `k_max`, **no define índices**. `caps` (1-6) y `units_per_color` (4; objeto `{"13": 5}` para la fruta abundante; el agua de Lluvia cuenta sus gotas pendientes, 02 R3).
- `attrs[]`, misma longitud que `cuias`, `null` sin mecánica; `{type ∈ hoja/brote/liana/remolino/lluvia/pirana, key, partner, n, k}` con los campos que usa cada mecánica según 02 §9.1 (`n` = gotas y `k` = ritmo de Lluvia; `k` de Piraña) y `n` = sellos necesarios de Liana; si falta `k` rigen `lluvia_k_pours` / `pirana_k_pours`; el Barro no es un `attr` (`type` no admite `barro`): va como capa 15 en `cuias`.
- `murky` y `hidden` = lista `[cuia, capa]` (índices 0-based), limitados por `lvl_murky_max_cuias` (3) y `lvl_murky_max_hidden_units` (4).
- `difficulty {label, bot_fail, par, branching, traps, complexity, D, band}` (sección 7; obligatorio en campaña y eventos, opcional en `kind: daily`, sección 16; `D` es un número sin límites, suma ponderada de z-scores que puede ser negativa; `band ∈ comfort / challenge / recovery / milestone`), `risk_pairs` (≤ `max_risk_pairs` de la cabecera), `gen {tool, version, source ∈ walk/deep/fill, depth/candidates}`.
- `relief` = **lista** de tableros completos `{type ∈ plus_empty/minus_color, caps, cuias, attrs?, removed?, par, bot_fail}` (`par` entero ≥ 1, o `null` solo en `plus_empty`, sección 13), con al menos una variante desde el nivel 31 (el esquema lo exige con `minItems` 1 mediante `if`/`then`; `[]` antes) y ordenada por prioridad con la regla de la sección 13 (`relief_mode`): el cliente sirve `relief[0]` al tercer fallo consecutivo (`relief_after_fails`) y `relief[1]` solo si `relief_second_step_fails` > 0.
- `solution`, `safe_path` y `qa` existen solo en el pipeline; `tools/level-qa` falla si aparecen en el pack publicado. En memoria `BoardState` empaqueta cada cuia como `len` (3 bits) + un nibble por unidad con `tinte − 1` (sección 3).

Tamaño [V-proto]: 567-715 B por nivel minificado (mediana 642 B con métricas completas); pack de 100 niveles de 7-11 cuias: 62.877 B minificado, 4.231 B gzip (las claves y la paleta se repiten; Addressables comprime con LZ4/LZMA). Extrapolación [E]: 700 niveles instalados ≈ 440 KB sin comprimir / ≈ 30 KB comprimidos (≈ 1 MB / 0,07 MB con los dos tableros de `relief`, 08 §4.1); 1.040 niveles (vuelta 1) ≈ 650 KB / 45 KB. El límite de 80 MB del AAB (dirección 20) no se ve afectado. Un pack por mundo y vuelta (`mundo_03_v1.json`) con la cabecera de 08 §4.1 (`pack_schema`, `schema_file`, `pack_id`, `kind`, `generator`, `world`, `world_id`, `lap`, `water_base`, `pool`, `k_min`, `k_max`, `max_risk_pairs`, `cap_default`, `empties_default`, `cycle20`, `levels[]`; research/02 C3.5).

## 11. Curva de dificultad por rangos

| Rango | Mundos | Colores K | Cuias totales | Vacíos | Capacidades | Mecánicas activas (debut) | % Difícil / % Muy difícil | Par objetivo | `max_pares_riesgo` |
|---|---|---|---|---|---|---|---|---|---|
| 1-10 | 1 Estuario | 2→5 | 3-7 | 2 (1 en los niveles 1 y 6; tabla de 02 §13) | 4 | Tutorial (02 §13): Marea (3; ilimitada hasta el 10), Volver a la orilla (6 si hay Aguas estancadas; 8 primer reinicio probable), Cuia extra gratis (8) | 0 / 0 | 3-18 (02 §13) | 0 |
| 11-30 | 2 Tapajós | 6-7 | 8-10 | 2 (3 en respiros) | 4; Cuia chica 2 u. (14), 1 u. (18), 3 u. (22); Cuia alta 5 u. + fruta abundante (27) | Cuia chica (14), Cuia alta (27); Marea 3/nivel desde el 11, Morpho con 3 unidades regaladas (12), Pororoca (30); retos suaves sin etiqueta (bot 50-70 %) en 26 y 30 | 0 / 0 | 10-18 | 0 |
| 31-60 | 2 Tapajós · 3 Manaos (41) | 7 → 7-8 | 9-11 | 1 (2 en respiros y recuperación) | 4 + chica + alta (5 u.) | Hoja (43); Canoas y Cuia de rescate desde 31 | 10-13 / 3 (primer Difícil 40, primer Muy difícil 60) | 14-22 | 0 (31-40) · 1 (41-60) |
| 61-100 | 3 Manaos · 4 Trapecio (81) | 7-8 → 8-9 | 10-12 | 1 (2 en 30 % de comodidad) | 4 + chica + alta (5 u.) | Brote (63), Liana (83); Aguas turbias en Difícil/Muy difícil desde el hito 100 | 15 / 5 | 16-26 | 1 (61-80) · 2 (81-100) |
| 101-200 | 4 Trapecio · 5 Loreto (141) | 8-9 → 9 | 10-13 | 1 | 4 + chica + alta; Cuia alta 6 u. en hitos desde el 141 (primero 160) | Remolino (103), Barro (123), Lluvia (143), Piraña (183); Aguas turbias en Difícil/Muy difícil | 15-20 / 5 | 18-30 | 2 |
| 201-400 | 6 Oriente · 7 Madre de Dios (301) | 9-10 → 10-11 | 11-13 | 1 (2 en recuperación) | 4-6 | Combinaciones de 2 mecánicas desde 221 (máx. 2 antes del 300, 3 después; 02 §9.2) | 25 / 5 | 22-36 | libre |
| 401-700 | 8 Madidi · 9 Ucayali (561) | 10-11 → 10-12 | 12-14 (hitos de 14 cuias: K = 13 + 1 vacío desde 561) | 1 | 4-6 | Todas; mecánica nueva cada 40-60 (a definir en live-ops) | 25 / 5 | 25-42 | libre |
| 701-1000 | 10 Alto Negro · 11 Escudo de Guayana (821) · 12 Invitados del Orinoco (921) · 13 El origen (961) | 10-12 | 12-14 (hitos de 14 cuias, K = 13 + 1 vacío) | 1 | 4-6 | Rotación completa; Encuentro de las Aguas en Alto Negro (dos aguas): K = 14 solo en ese evento, con Cuia extra y Cuia de rescate deshabilitadas (`rescue_max_per_level` = 0 en ese modo) | 25 / 5 | 28-48 | libre |
| 1000+ | Vuelta 2 en adelante (variantes lluvia/noche/alba) | 10-12 | 12-14 | 1 | 4-6 | Rotación; amplitud constante, sin crecimiento monótono | 25 / 5 | 30-50 | libre |

Fuentes: colores y mundos de research/02 C3.3 (canon por dirección 16); vacíos de la dirección 4; mecánicas, capacidades y niveles de debut de 02 §8 y §9.1 (`mech_debut_level`, dirección 8), copiados en la sección 9; par de research/02 §7.2 ajustado a los pares medidos (`walk` ≈ 2,2K, `fill` ≈ 3K) [V-proto]; porcentajes derivados del ciclo de la sección 12 [P]. Reglas: nunca dos Difícil consecutivos; el hito siempre va precedido de un reto; tras cada hito, dos niveles con fallo de bot < 30 %; los respiros "puros" (2 vacíos, sin mecánicas) ocupan las posiciones de recuperación (patrón Magic Sort 24/200/280/420, research/09 §Tipos de nivel [V]). **Ningún nivel generado supera las 14 cuias** (02 R1): los hitos de 13 colores llevan 13 cuias llenas + 1 vacío, y con Cuia extra y Cuia de rescate el máximo en pantalla es 16 (02 §5 y §14). K = 14 queda para el evento Encuentro de las Aguas (14 colores + 1 vacío = 15 cuias en tres filas), donde Cuia extra y Cuia de rescate se deshabilitan (`rescue_max_per_level` = 0 en ese modo) para no superar las 16 ranuras.

## 12. Ciclo de 20 niveles

Los niveles 1-20 son una rampa de tutorial diseñada a mano. Desde el 21, cada ciclo cubre los niveles `20k+1 … 20k+20` y la posición es `p = ((n − 1) mod 20) + 1`.

| Posición p | Tono | Vacíos | Fuente | Ventana de bot (21-40 / 41-60 / 61+) | Etiqueta posible |
|---|---|---|---|---|---|
| 1, 2 | Recuperación | 2 | `walk`, K−1 | < 30 % | — |
| 3, 4, 5 | Comodidad (los 2-3 tableros de debut de cada mecánica de 02 §9.1 ocupan estas posiciones; ver nota bajo la tabla) | 1 (2 hasta el 30 y en debuts) | `walk` | 20-60 % (< 20 % en debuts) | — |
| 6 | Reto | 1 (2 hasta el nivel 30) | `fill` E=2 (21-40) · `deep`/`fill` E=1 (41+) | 50-70 / 60-80 / ≥ 75 | Difícil desde 41+ |
| 7, 8, 9 | Comodidad | 1 (2 hasta el nivel 30) | `walk` | 20-60 % | — |
| 10 | Reto | 1 (2 hasta el nivel 30) | ídem p6 | ídem | Difícil |
| 11, 12, 13 | Comodidad | 1 | `walk` | 20-60 % | — |
| 14 | Reto | 1 (2 en el ciclo 21-40, sección 4.4) | ídem p6 | ídem | Difícil |
| 15, 16 | Comodidad | 1 | `walk` | 20-60 % | — |
| 17 | Reto | 1 (2 en el ciclo 21-40, sección 4.4) | ídem p6 | ídem | Difícil |
| 18 | Comodidad | 1 | `walk` | 20-60 % | — |
| 19 | Reto | 1 (2 en el ciclo 21-40, sección 4.4) | ídem p6 | ídem | Difícil |
| 20 | Hito | 1 | `deep` K máx. | 75-85 (21-40) · ≥ 90 (41+) | Difícil (40) · Muy difícil (60+) |

Vacíos en el ciclo 21-40: todas las posiciones ≤ 30 (niveles 21-30) usan 2 vacíos (dirección 4); **el primer nivel con 1 vacío es el 31** (comodidad, p11). Los retos sin etiqueta de 31-39 conservan 2 vacíos porque su fuente es `fill` E=2 (sección 4.4: densidad sin estrechez); el hito 40, primer Difícil, es el primer reto con 1 vacío. Debuts de mecánicas (sección 9, consecuencia 3): los siete bloqueadores de 02 §9.1 (43, 63, 83, 103, 123, 143, 183) caen en la posición 3 y ocupan las posiciones 3-5 como comodidad. **Excepción de debut**: en los 2-3 tableros de debut de cada mecánica de 02 §9.1 (p3-p5 de los ciclos 41, 61, 81, 101, 121, 141 y 181) rigen 2 vacíos, una sola mecánica con un solo ejemplar y ventana de bot < 20 % (02 §9.2 regla 2); es la única excepción de la plantilla. No hay reto suave en el nivel 19 [P]: los niveles 1-20 son rampa a mano y los primeros retos sin etiqueta son el 26 y el 30, por lo que el KPI "primer fallo real" de 09 §6.1 se lee en 22-30 (verde) y 18-22 / 30-35 (amarillo).

Cuenta: 12 comodidad (3-5, 7-9, 11-13, 15-16, 18) + 5 reto (6, 10, 14, 17, 19) + 2 recuperación (1-2) + 1 hito (20) (research/02 §5, indiecoredev [V]). El salto de dificultad entre niveles consecutivos se limita a +2,2 bandas de cuantil al subir y −3,4 al bajar [V, indiecoredev]. Calendario de etiquetas: 21-40 sin etiquetas (retos suaves), **40 = primer Difícil**, 41-60 retos Difícil con 2-3 de los 5 alcanzando ≥ 75 %, **60 = primer Muy difícil**; desde 61 los 5 retos apuntan a ≥ 75 % y el hito a ≥ 90 %; la etiqueta la decide la medición, no el slot. Esto da 4 Difícil y 1 Muy difícil en los primeros 60 niveles frente a los 8 + 5 de Royal Match en 100 (research/02 §5 [V]) y respeta "cero rescates antes del 31" (dirección 10): los retos de 21-30 se fallan y se reinician gratis.

## 13. Reintento determinista y alivio tras 3 fallos

Definición de **fallo**: **ver 04 §5.2** (tabla canónica; este capítulo no la redefine). Resumen idéntico al canon: (a) **fallo duro** = Volver a la orilla desde Aguas estancadas (STUCK_MODAL): gasta canoa desde el 31 (salvo Canoas infinitas), rompe la Crecida salvo Hoja escudo y suma `fails_level`; (b) **fallo blando** = Volver a la orilla voluntario con ≥ 8 vertidos (`restart_confirm_min_pours`): suma `fails_level` (cuenta para el alivio y para Salto del Boto) pero **nunca** gasta canoa ni rompe la Crecida; un reinicio con < 8 vertidos, Marea desde el modal, una Cuia de rescate aceptada, salir al Mapa del río o cerrar la app con el modal abierto (el estado se guarda y el modal reaparece) y un crash **no** son fallo; Salto del Boto no gasta canoa y reinicia `fails_level` a 0. El contador `fails_level` vive en el guardado, se reinicia al ganar el nivel y persiste entre sesiones.

| Fallos consecutivos | Qué se sirve | Mensaje | Fuente |
|---|---|---|---|
| 0-2 | **El mismo tablero** (misma semilla, mismo orden visual, misma Cuia extra si se compró) | — | dirección 7; research/09 §Mejorar [V] |
| 3 (`relief_after_fails`) | Variante aliviada pregenerada `relief[0]` (tablero completo +1 vacío o −1 color; 08 §4.1); conserva etiqueta y recompensa | Tarjeta **"El río bajó"**: "El río bajó: esta vez hay una cuia más" / "…un color menos" (`anim_river_lowered_banner_ms`, 2,0 s) | dirección 7; glosario §6.3 |
| 4 | Misma variante aliviada (`relief[1]` solo si `relief_second_step_fails` > 0; apagado en 1.0) | — | |
| 5+ (`skip_min_fails`) | Variante aliviada + oferta de **Salto del Boto** (`skip_seed_price` 300 semillas, máx. 1 cada `skip_min_gap_levels` = 10 niveles) | "Rosa te lleva río abajo, al siguiente nivel." | dirección 9 |

Regla de orden de `relief[]` (`relief_mode` = `auto`; el generador coloca en `relief[0]` el tipo elegido) [P]: **+1 vacío** por defecto cuando el nivel tiene 1 vacío (el jugador conserva el tablero que ya aprendió; es la misma sensación que Cuia extra; resoluble por construcción, sin solver); **−1 color** cuando el nivel ya tiene 2 vacíos o cuando +1 vacío deja el fallo de bot por encima de `bot_fail − 0,20` (el alivio debe notarse). La variante −1 color elimina las `units[c]` unidades del color que deja el par más alto entre las eliminaciones resolubles (A* a 100.000); medido [V-proto]: siempre existió al menos un color eliminable con solución. Coste: la variante +1 vacío **no se resuelve** en el pipeline (con 3 vacíos y 10 colores A* a 100.000 se agotó y costó 99 s en Python); se publica con `par` nulo en su entrada de `relief[]` (resoluble por construcción; su `bot_fail` sí se mide) y se calcula con A* ponderado solo si QA lo pide. El `par` nulo solo se admite en `plus_empty`: toda entrada `minus_color` lleva `par` entero ≥ 1 (`schemas/af-level-1.schema.json`, 08 §4.1). La variante −1 color cuesta K resoluciones (mediana 1,1-2,5 s en Python, hasta 150 s en tableros `fill` de 2 vacíos y 10 colores): se calcula solo para niveles con 2 vacíos o cuando +1 vacío no alivia, y con A* ponderado w=2. Desde el nivel 31 `relief[]` lleva al menos una variante (el esquema lo exige con `minItems` 1; QA #8; `[]` antes) y las variantes van en el mismo pack (+2 × ~600 B por nivel desde el 31) [E].

## 14. Dificultad adaptativa

| Señal | Umbral [P] | Acción | Límite |
|---|---|---|---|
| `fails_level` | 3 | Variante aliviada comunicada (sección 13) | 1 escalón; nunca silenciosa |
| `fails_level` | 5 | Oferta de Salto del Boto | 1 cada 10 niveles |
| Riesgo de churn: ≥ 4 fallos en los últimos 5 niveles, o 2 sesiones seguidas terminadas en fallo (**DDA personal, 1.2**; apagada en 1.0 con `lvl_dda_enabled = false`, 09 §1.2) | activo | Los 2 siguientes slots de comodidad se sirven desde el cuartil inferior de D del pool del rango (`level_overrides` personal, sin cambiar el tablero de nadie más). **Nunca en silencio**: al cargar el nivel sustituido aparece la misma tarjeta "El río bajó" del alivio con el texto "El río bajó en este tramo" (dirección 7) y se registra `af_dda_applied {level, reason: churn_risk, variant: pool_low_D}` (08 §8.2) | Máx. 2 intervenciones por ciclo de 20; solo slots de comodidad; solo con `lvl_dda_enabled = true` (1.2) |
| Riesgo de churn + slot de reto próximo (1.2, misma clave) | activo | Se mantiene el reto (la etiqueta es parte del contrato); se activa Hoja escudo si está disponible | — |
| Jugador que gana 10 seguidos sin Marea | — | **Nada** (no se sube la dificultad) | — |

Qué NO se toca (dirección 7 y 5; research/09 §Qué NO copiar [V]): el tablero de un nivel en curso o reintentado antes del 3.er fallo; las etiquetas Difícil/Muy difícil y sus umbrales; las recompensas y precios (la economía núcleo no se somete a A/B ni a personalización); los niveles 1-20 (se miden y se corrigen a mano); el Reto del río, Regata, Liga del río y eventos (tablero idéntico para todos); nunca se **sube** la dificultad a nadie. Toda intervención se registra (`af_dda_applied {level, reason, variant}`, 08 §8.2), se comunica con la tarjeta "El río bajó" y tiene interruptor en Remote Config (`lvl_dda_enabled`). **En 1.0 la DDA personal está apagada** (`lvl_dda_enabled = false`; 09 §1.2 y §3.2 P3 la programan para 1.2): durante el soft launch todos los jugadores juegan el mismo tablero en el mismo slot, condición necesaria para que el embudo por nivel (sección 15) calibre las etiquetas y para no experimentar en opaco (research/09 §Mejorar); en 1.0 solo operan las dos primeras filas (alivio comunicado y Salto del Boto), que no dependen de esta clave. Base: el marco "difficulty-aware" mejoró retención en A/B (KDD 2021, research/02 §6 [V]); la cifra "−11 % de churn" no está verificada [NV].

## 15. KPIs por nivel y umbrales de alarma

| KPI | Definición | Normal: objetivo / alarma | Difícil: objetivo / alarma | Muy difícil: objetivo / alarma | Fuente del ancla |
|---|---|---|---|---|---|
| Intentos medios (y mediana) | Intentos hasta ganar entre quienes ganan | 1,2-1,5 / > 2,5 | 1,6-2,2 / > 4 | 2,5-3,5 / > 6 | Royal Match 1,2 / 1,6 / 2,5 (research/02 §5 [V]); umbrales [P] |
| Abandono en nivel | % de jugadores que inician el nivel y no inician el siguiente en 7 días | < 1,5 % / > 3 % | < 3 % / > 5 % | < 4 % / > 7 % | research/02 §7.3 [E]; niveles 1-10 alarma > 5 % (research/05 §Primera sesión [E]) |
| Tiempo a victoria (mediana) | Tiempo activo por intento ganador | 1-10: < 1,5 min · 11-30: 1-3 · 31-100: 2-5 · 101+: 3-8; alarma > 2× | ídem | ídem | tiempos de guías de Magic Sort (research/09 §Onboarding [V]); [P] |
| Tasa de Aguas estancadas | Bloqueos por intento | < 0,3 / > 0,6 | < 0,7 / > 1,2 | < 1,0 / > 1,6 | [P] |
| Uso de comodines por intento | Marea, Cuia extra, Morpho, Pororoca, Luciérnaga (orden de la bandeja, 07 §7.4) | registro | registro | registro | research/02 §7.3 |
| Rescates por 100 intentos y conversión rescate → victoria | Cuia de rescate aceptada; victoria tras rescate | — / conversión < 85 % alarma | ídem | ídem | research/03 C2.5 [E] |
| Tasa de alivio | % de jugadores que ven "El río bajó" en el nivel | < 5 % / > 15 % | < 15 % / > 30 % | < 25 % / > 40 % | [P] |
| Coherencia bot ↔ humanos | Correlación de Spearman entre `bot_fail` e intentos medianos en el rango | ≥ 0,6 tras 2 semanas; si < 0,4 → recalibrar sección 6 | | | [P] |

Eventos de analítica (Firebase): los de la taxonomía única de 08 §8.2, con sus nombres y parámetros exactos (este capítulo no define eventos propios): `level_start`, `af_level_win`, `af_level_fail`, `af_level_abandon` y `level_end {success, level_name}` para el embudo, los intentos (parámetro común `attempt`, 08 §8.1) y el tiempo a victoria (`t_ms`); `af_stuck_shown` / `af_stuck_choice` / `af_stuck_outcome` para Aguas estancadas y rescate → victoria; `af_booster_used` para los comodines; `af_river_lowered` para la tasa de alivio; `af_dda_applied` y `af_label_shown`. Proceso: revisión semanal del embudo por nivel; un nivel en alarma se sustituye por otro del pool del mismo slot vía `level_overrides` (sin build) y se marca para curación; poda mensual del 5 % de niveles con peor tiempo hasta abandono (King poda los 100 peores, research/02 §5 [V]).

## 16. Reto del río y Río sin fin

**Reto del río** (1.0): pool offline pregenerado e indexado por fecha, `reto(fecha, tramo) = pool[tramo][(días_desde(2026-01-01) + live_daily_offset) mod 730]` (`live_daily_offset`, 08 §9; mismo cálculo que 06 §4.1), con tres tramos por progreso del jugador [P]: A = banda 1 (nivel máximo < 41: K 6-7, 2 vacíos, ventana 40-60 %), B = banda 2 (41-200: K 8-9, 1 vacío, 60-80 %), C = banda 3 (201+: K 10-11, 1 vacío, ≥ 75 %); 3 × 730 = 2.190 tableros (pack `daily_pool_v1`) en el esquema `af-level/1` (`schemas/af-level-1.schema.json`, 08 §4.1) con `kind: daily`, `slot {band, day}` en lugar de `level`, `id` `d{b}_n{nnnn}` (patrón `^d[1-3]_n[0-9]{4}$`) y `seed` `amazonfill:v1:daily:b{b}:d{ddd}` (patrón `^amazonfill:v1:daily:b[1-3]:d[0-9]{3}$`), con b = banda 1-3 y el día 0-729 con ceros a la izquierda (cuatro cifras en el `id`, tres en la `seed`; p. ej. `d2_n0417` y `amazonfill:v1:daily:b2:d417`); `difficulty` es opcional en `kind: daily` y el pool se publica sin métricas, con `relief` vacío (`[]`) y nunca en hexadecimal; ≈ 1,4 MB minificados / ≈ 0,1 MB comprimidos [E]; umbrales de tramo en `lvl_daily_bracket_levels` = `[41, 201]`. Sin mecánicas con disparador ni Aguas turbias en el reto obligatorio; la tarjeta Aguas turbias · Bonus (1.1; `ui_murky_bonus_enabled` = false en 1.0, 08 §9) usa el mismo tablero con 2 cuias turbias verificadas. `daily_overrides` en Remote Config sustituye fechas concretas (fiestas, eventos); el premio se reconcilia con la fecha del servidor del último fetch para evitar el cambio de reloj (research/02 C4.2 [V/E]).

**Río sin fin** (1.2+): generación en el dispositivo por barajado inverso (`walk` con 100 ms de presupuesto; si la complejidad no alcanza el mínimo, `deep` con 10.000 nodos), K = min(12, K del nivel máximo del jugador), vacíos alternando 2/1, sin mecánicas ni turbias, par por A* ponderado (40.000 nodos, w = 2); si tres intentos no producen un tablero válido en 300 ms, se sirve del pool de reserva de 1.000 tableros (research/02 C4.3 [E]). Como el inverso garantiza solución por construcción, no hace falta solver para publicar el tablero; el solver solo mide.

**Pororoca** (barajar): reconstrucción inversa sobre las cuias no selladas con los mismos vacíos, k = max(12, 3 × colores restantes) movimientos inversos, rechazo si el resultado iguala al tablero actual o deja una cuia sellada; se exige par' ≤ par + `shuffle_par_slack` (2) con A* a `shuffle_budget_nodes` (40.000) (research/02 C4.4 [E]).

## 17. Plan de producción y QA

| Hito | Contenido | Trabajo | Equipo de 1 programador + 1 artista | Equipo de 2 programadores + 1 artista |
|---|---|---|---|---|
| Pipeline v1 | Solver C# compartido (`AF.Core` + `AF.Solver` en `Packages/com.aquazonia.core`, C# 9 y netstandard2.1, sin `UnityEngine`, compilados también por `dotnet/` con .NET 10; 08 §2.4), generadores `walk`/`deep`/`fill`, bot, métricas, D, colocación, JSON, validador, CLI .NET en `tools/level-generator` (+ `tools/level-qa`) | 2-3 días solver con tests + 1-2 días generador + 1 día métricas y empaquetado (research/02 C4 [E]) + 2 días mecánicas y turbias [E] | 7-9 días | 5-6 días (solver y generador en paralelo) |
| Generación 1.0 | 1.040 niveles (vuelta 1) + 20 % reserva + variantes de alivio | Python: 1,3-3,6 s/nivel mediana → 25-60 min para 1.040 más colas de minutos en hitos de 2 vacíos [V-proto]; C#: minutos | 0,5 día | 0,5 día |
| Curación | Niveles 1-200 a mano (tutorial 1-20 desde cero; 21-200 ajuste de retos, hitos, debuts) | 20-25 niveles/día [E] | 8-10 días (diseñador = programador 1) | 8-10 días (en paralelo) |
| QA de nivel | 700 niveles instalados: 2 pasadas en retos/hitos (210), 1 en el resto (490) | ~3 min/nivel → ~46 h [E] | 6 días (tester externo) | 6 días |
| Lanzamiento | 700 niveles instalados en el AAB (`levels_install`, mundos 1-9; cubre los ≥ 600 jugables de la dirección 17; 08 §6.2); 701-1.040 en Addressables remotos (`levels_remote`) | — | — | — |
| Cadencia | +100 niveles cada 1-2 semanas: generar (1 h), curar los 30 retos/hitos (2 días), QA (1 día), publicar por Addressables + RC | 3-4 días/pack | 1 persona a tiempo parcial | 1 persona a tiempo parcial |

Fondos de 1.0: los mundos 1-4 van pintados en la instalación (dirección 16) y Loreto (141-200) llega por Play Asset Delivery; los niveles 201-700 (mundos 6-9) usan el fondo "río genérico" con la LUT de agua del mundo hasta que lleguen sus packs (07 §11; 08 §6.2).

**Checklist de QA por nivel** (checks automáticos de `tools/level-qa`, que hacen fallar el CI, salvo donde se indica "humana"):

| # | Comprobación | Criterio |
|---|---|---|
| 1 | Resolubilidad | Solución de A* guardada y re-ejecutada desde el JSON publicado (sin campos de pipeline) |
| 2 | Par | `par ≥ 2K`; par dentro de la ventana del rango ±30 % |
| 3 | Dificultad | `bot_fail` en la ventana del slot (< 0,20 en debuts, sección 12); etiqueta coherente con el umbral |
| 4 | Paleta | Todos los tintes con `nivel_debut ≤ nivel`; K ≤ `k_max` del mundo; `risk_pairs ≤ max_pares_riesgo`; `risk_adjacent` revisado (humana) |
| 5 | Estructura | Vacíos según rango; **≤ 14 cuias siempre** (02 R1); ninguna cuia sellada inicial; `units` coherente con `caps`: para cada color existe una cuia con `caps[i] == units[c]` y toda Cuia alta aloja una fruta abundante con `units[c] == caps[i]` (02 §8); una Cuia alta de 6 solo sella con una fruta de `U_c = 6`; una fruta de 4 unidades dentro de una Cuia alta debe salir a una cuia de capacidad 4; una Cuia chica (1-3) debe terminar vacía; sellada = llena ∧ monocolor ∧ visible (sección 3); `atascado(S0)` = false con la `futil` única de AF.Core (02 §1.4 y §9.2 regla 1) |
| 6 | Mecánicas | Debut (02 `mech_debut_level`) anterior o en este nivel con un solo ejemplar; solver extendido confirma solución; Hoja con color llave presente; Barro solo en posición 2-3 de una cuia de 4; Lluvia con `units[agua] = 4` contando gotas; Piraña con cueva marcada; sin Pororoca en Lluvia/Piraña |
| 7 | Aguas turbias | ≤ 3 cuias turbias (`lvl_murky_max_cuias`), ≤ 4 dosis ocultas (`lvl_murky_max_hidden_units`); la realidad verdadera cumple T4 (02 §11) y `R` se enumera solo sobre realidades consistentes con T4 (≤ 24); `safe_path` verificada en cada estado de la ruta; no coincide con Lluvia/Piraña |
| 8 | Alivio | `relief` con al menos una variante desde el nivel 31 (tablero completo; el esquema lo exige con `minItems` 1 mediante `if`/`then`; `[]` antes; 08 §4.1); la variante −1 color resuelta, con `par` entero (`par` nulo solo en `plus_empty`, sección 13); alivio perceptible: `bot_fail` baja ≥ 0,20 en cada entrada; si el base tiene `bot_fail` < 0,20 (debuts), basta `par` menor en `minus_color` y el vacío extra en `plus_empty` |
| 9 | Anti-repetición | `forma(estado)` distinta de los 200 niveles anteriores y posteriores; distancia de Hamming entre `cuias` ≥ 25 % con los 20 vecinos |
| 10 | Reproducibilidad | Regenerar con `seed` + `gen_version` produce hash idéntico (test de CI) |
| 11 | Esquema y tamaño | Validación contra `schemas/af-level-1.schema.json` (`af-level/1`, 08 §4.1; única copia del esquema), más la igualdad de longitud de `caps`, `cuias` y `attrs`, que el esquema no puede expresar; pack ≤ 100 KB por 100 niveles sin `relief` |
| 12 | Legibilidad (humana) | Partida completa sin comodines en retos e hitos; comprobar que la cuia que debuta un tinte es visible en la fila superior; Patrones de fruta activados muestran icono en cada capa |
| 13 | Mensajes | "El río bajó" aparece en la variante; etiqueta y subtítulo temático correctos en ES/EN/PT |

## 18. Prototipo: resultados reales

Archivo: `C:/Users/Nipko-lab/Desktop/projects/amazon-fill/tools/levelgen_proto.py` (920 líneas, sin dependencias externas; fuentes `walk`, `deep` y `fill`, solver A*, bot, métricas, alivio, Aguas turbias y empaquetado). Comandos ejecutados el 2026-10-05 en Windows 11, Python 3.14.3, un hilo:

```
python tools/levelgen_proto.py --levels 20 --bot-runs 40 --quiet                 # lote híbrido (walk/deep/fill)
python tools/levelgen_proto.py --levels 20 --bot-runs 40 --size-run 100 --murky --quiet --out tools/out_inverse   # lote solo inverso + tamaño + turbias
```

Salidas: `tools/out/levels_proto.json`, `tools/out/example_7_cuias.json`, `tools/out/report.md`, `tools/out_inverse/levels_size_100.json`, `tools/out_inverse/murky_demo.json`.

### 18.1 Lote híbrido de 20 niveles (7-12 cuias)

Lote de 20 niveles con las tres fuentes (8 `walk`, 6 `deep`, 6 `fill`), 440,6 s en total [V-proto]. La banda es el cuantil de D dentro de este lote mezclado (K 5-11) y es solo ilustrativa; en producción el cuantil se calcula sobre el pool de un rango.

| # | Fuente | K | Vacíos | Cuias | Intentos gen | Par | Estados A* | t solve (ms) | Fallo bot | Ramif. | Trampas | Compl. | D | Etiqueta | Alivio −1 color (par) | t total (s) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | walk | 5 | 2 | 7 | 5 | 11 | 347 | 14 | 0,00 | 4,45 | 0,00 | 28 | −0,83 | Normal | 2 (9) | 0,26 |
| 2 | walk | 6 | 2 | 8 | 3 | 15 | 1.403 | 76 | 0,47 | 5,20 | 0,00 | 38 | −0,15 | Normal | 4 (14) | 1,91 |
| 3 | walk | 6 | 1 | 7 | 6 | 12 | 49 | 2 | 0,97 | 2,50 | 0,10 | 34 | −0,03 | **Muy difícil** | 5 (11) | 0,08 |
| 4 | walk | 7 | 2 | 9 | 2 | 19 | 2.865 | 179 | 0,10 | 3,32 | 0,02 | 48 | −0,60 | Normal | 6 (17) | 6,37 |
| 5 | walk | 8 | 2 | 10 | 3 | 16 | 2.574 | 158 | 0,12 | 6,12 | 0,00 | 42 | −0,57 | Normal | 5 (15) | 5,38 |
| 6 | walk | 9 | 2 | 11 | 1 | 18 | 2.865 | 231 | 0,38 | 7,22 | 0,00 | 50 | −0,20 | Normal | 6 (17) | 10,51 |
| 7 | walk | 10 | 2 | 12 | 2 | 21 | 6.764 | 524 | 0,25 | 7,43 | 0,00 | 56 | −0,22 | Normal | 4 (19) | 21,29 |
| 8 | walk | 11 | 1 | 12 | 3 | 22 | 204 | 13 | 0,95 | 2,86 | 0,19 | 58 | +0,07 | **Muy difícil** | 1 (20) | 4,21 |
| 9 | deep | 7 | 1 | 8 | 1 | 18 | 122 | 6 | 0,95 | 2,17 | 0,13 | 48 | +0,20 | **Muy difícil** | 6 (17) | 1,21 |
| 10 | deep | 8 | 1 | 9 | 1 | 19 | 173 | 9 | 0,88 | 2,11 | 0,15 | 52 | +0,07 | **Difícil** | 6 (18) | 1,40 |
| 11 | deep | 9 | 1 | 10 | 1 | 19 | 230 | 13 | 0,88 | 2,47 | 0,13 | 54 | −0,03 | **Difícil** | 1 (18) | 2,14 |
| 12 | deep | 10 | 1 | 11 | 1 | 23 | 221 | 13 | 1,00 | 3,17 | 0,26 | 62 | +0,42 | **Muy difícil** | 6 (22) | 3,34 |
| 13 | deep | 11 | 1 | 12 | 1 | 27 | 174 | 13 | 1,00 | 2,70 | 0,40 | 72 | +0,61 | **Muy difícil** | 5 (26) | 8,64 |
| 14 | deep | 10 | 1 | 11 | 1 | 22 | 382 | 22 | 0,97 | 3,32 | 0,32 | 60 | +0,39 | **Muy difícil** | 4 (21) | 3,35 |
| 15 | fill | 6 | 2 | 8 | 1 | 19 | 1.868 | 92 | 0,07 | 3,05 | 0,00 | 44 | −0,53 | Normal | 3 (15) | 3,52 |
| 16 | fill | 7 | 1 | 8 | 53 | 19 | 139 | 6 | 1,00 | 2,53 | 0,27 | 48 | +0,48 | **Muy difícil** | 5 (17) | 0,87 |
| 17 | fill | 8 | 2 | 10 | 1 | 26 | 6.241 | 396 | 0,85 | 2,85 | 0,03 | 60 | +0,40 | **Difícil** | 6 (23) | 48,54 |
| 18 | fill | 9 | 1 | 10 | 26 | 25 | 144 | 7 | 0,95 | 1,60 | 0,03 | 64 | +0,24 | **Muy difícil** | 4 (23) | 3,60 |
| 19 | fill | 10 | 2 | 12 | 1 | 32 | 13.141 | 1.056 | 0,97 | 3,66 | 0,05 | 76 | +0,77 | **Muy difícil** | — | 157,39 |
| 20 | fill | 10 | 2 | 12 | 1 | 32 | 5.167 | 428 | 0,80 | 2,88 | 0,05 | 78 | +0,51 | **Difícil** | 4 (28) | 156,54 |

| Fuente | n | Fallo bot medio | Fallo bot mín.-máx. | Par medio | Par / 2K | t generación mediana (s) | t total mediana (s) |
|---|---|---|---|---|---|---|---|
| `walk` | 8 | 0,41 | 0,00-0,97 | 16,8 | 1,09 | 0,01 | 4,80 |
| `deep` | 6 | 0,95 | 0,88-1,00 | 21,3 | 1,17 | 0,47 | 2,74 |
| `fill` | 6 | 0,78 | 0,07-1,00 | 25,5 | 1,53 | 0,01 | 26,07 |

Resumen del lote [V-proto]: A* óptimo mediana 18 ms, media 163 ms, máx. 1.056 ms (13.141 estados); bot ×40 mediana 63 ms; trampas 0,22 s; alivio mediana 2,52 s (la variante −1 color cuesta K resoluciones; los 157 s de los niveles 19-20 son −1 color con 2 vacíos y 10 colores más el +1 vacío agotado, ambos corregidos en la sección 13); pipeline completo mediana 3,56 s por nivel; Morpho (A* w=2, 40.000 nodos) mediana 3 ms, máx. 21 ms, +0,5 movimientos de subóptimo medio (máx. +3). Lecturas: (1) **el número de vacíos domina el fallo del bot**: los ocho tableros de 1 vacío del lote, de cualquier fuente, dieron 0,88-1,00, y los de 2 vacíos 0,00-0,47 salvo los `fill` densos (0,80-0,97); por eso la comodidad de 31+ con 1 vacío exige rechazo por ventana (sección 4.4) o una Cuia chica como medio vacío; (2) `deep` es la fuente más barata para Difícil/Muy difícil garantizados; (3) `fill` con 2 vacíos da el par más alto (≈ 3K) y es la fuente de los hitos "largos"; (4) D y la etiqueta discrepan en tableros pequeños (nivel 3: bot 0,97, D −0,03) porque par − 2K y ramificación penalizan el tamaño: D coloca, el bot etiqueta.

### 18.2 Lote solo inverso (`walk`, mejor prefijo), 20 niveles

| # | K | Vacíos | Cuias | Intentos gen | Par | Estados A* | t solve (ms) | Fallo bot | Ramif. | Trampas | Compl. | Etiqueta | t total (s) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 5 | 2 | 7 | 5 | 11 | 347 | 16 | 0,00 | 4,45 | 0,00 | 28 | Normal | 0,26 |
| 2 | 6 | 1 | 7 | 1 | 12 | 80 | 2 | 0,10 | 2,33 | 0,04 | 32 | Normal | 0,08 |
| 3 | 6 | 2 | 8 | 2 | 14 | 678 | 36 | 0,00 | 4,43 | 0,00 | 36 | Normal | 0,88 |
| 4 | 7 | 1 | 8 | 3 | 14 | 110 | 8 | 0,50 | 2,79 | 0,03 | 40 | Normal | 0,24 |
| 5 | 7 | 2 | 9 | 13 | 15 | 1.963 | 114 | 0,00 | 4,80 | 0,00 | 38 | Normal | 2,71 |
| 6 | 8 | 1 | 9 | 8 | 17 | 184 | 11 | 0,72 | 3,00 | 0,16 | 46 | Normal | 0,80 |
| 7 | 8 | 2 | 10 | 2 | 18 | 2.142 | 125 | 0,05 | 5,28 | 0,00 | 46 | Normal | 5,60 |
| 8 | 9 | 1 | 10 | 21 | 18 | 159 | 8 | 0,60 | 2,39 | 0,12 | 52 | Normal | 0,55 |
| 9 | 9 | 2 | 11 | 9 | 21 | 5.055 | 372 | 0,45 | 5,81 | 0,03 | 54 | Normal | 17,94 |
| 10 | 10 | 1 | 11 | 12 | 19 | 214 | 12 | 0,68 | 3,11 | 0,31 | 54 | Normal | 1,55 |
| 11 | 10 | 2 | 12 | 2 | 24 | 18.291 | 1.513 | 0,33 | 4,50 | 0,03 | 62 | Normal | 99,54 (alivio +1 vacío agotó 100k) |
| 12 | 11 | 1 | 12 | 2 | 22 | 326 | 20 | 0,80 | 3,18 | 0,10 | 62 | **Difícil** | 4,48 |
| 13 | 5 | 2 | 7 | 5 | 12 | 86 | 5 | 0,12 | 4,67 | 0,00 | 32 | Normal | 0,19 |
| 14 | 7 | 1 | 8 | 2 | 16 | 114 | 4 | 0,57 | 2,25 | 0,08 | 42 | Normal | 0,43 |
| 15 | 8 | 1 | 9 | 15 | 12 | 72 | 3 | 0,03 | 2,58 | 0,19 | 38 | Normal | 0,14 |
| 16 | 9 | 1 | 10 | 4 | 18 | 162 | 8 | 0,90 | 3,06 | 0,33 | 50 | **Muy difícil** | 1,13 |
| 17 | 10 | 1 | 11 | 1 | 21 | 423 | 26 | 0,90 | 2,76 | 0,29 | 56 | **Muy difícil** | 3,78 |
| 18 | 10 | 2 | 12 | 7 | 23 | 13.272 | 1.131 | 0,57 | 7,87 | 0,02 | 60 | Normal | 65,71 |
| 19 | 11 | 1 | 12 | 2 | 22 | 546 | 36 | 0,93 | 3,50 | 0,31 | 62 | **Muy difícil** | 6,33 |
| 20 | 8 | 2 | 10 | 3 | 18 | 2.928 | 199 | 0,53 | 6,11 | 0,00 | 46 | Normal | 7,45 |

Resumen [V-proto]: A* óptimo mediana 18 ms, media 182 ms, máx. 1.513 ms (18.291 estados); bot ×40 mediana 20 ms; trampas 0,16 s; alivio mediana 1,07 s (99 s en el peor caso por resolver +1 vacío, corregido en la sección 13); pipeline completo mediana 1,34 s por nivel; pista A* w=2 (40.000 nodos) mediana 2 ms, máx. 32 ms, +0,2 movimientos de subóptimo medio (máx. +1). Lectura: las trampas aparecen solo con 1 vacío (0,10-0,33) y son el rasgo que separa los Muy difícil de los Normal con el mismo K; la ramificación es mayor con 2 vacíos (4,4-7,9) sin que eso dificulte al bot.

### 18.3 Experimentos de generador (tiempos reales)

| Experimento | Resultado |
|---|---|
| Paseo inverso aleatorio, 40 intentos por (K, E) | Profundidad mediana 13 (K=5) → 19-20 (K=10-11), máx. 26; aceptación tras filtros 8-30 %; 1-4 ms/intento |
| Paseo inverso con selección greedy por complejidad (research/02 §3) | Muere a profundidad ~8: la selección por complejidad agota la movilidad; descartado |
| DFS inverso 20.000 nodos, K=8/10/11, E=1 (3 semillas cada uno) | Profundidad 18-27; fallo bot 0,68-0,97; par 17-25; 0,4-0,6 s |
| Relleno aleatorio E=1 (400 candidatos; K=10: 3.000; K=12: 1.500) | Aceptación 13,0 / 6,8 / 3,2 / 1,0 / 0,27 / 0,07 / 0 % para K = 6…12; candidato 1-8 ms mediana; aceptados con fallo bot 0,80-1,00 y par 15-33 |
| Relleno aleatorio E=2, K=8 y K=10 (30 y 45 candidatos) | 100 % resolubles; fallo bot 0,07-1,00; par 22-34; 0,1-4,0 s por candidato |
| Aguas turbias, 5 niveles de 8 cuias, 2 turbias × 2 capas | 5/5 con ruta segura; 12 realidades máx.; 0,7-8,5 s |
| Tamaño | 100 niveles: 62.877 B minificado (629 B/nivel), 4.231 B gzip |

## Parámetros configurables (Remote Config)

**Ver 08 §9**, única tabla de Remote Config del GDD (clave, tipo, default compilado, rango y dueño); este capítulo describe el comportamiento y no repite valores. Claves cuyo valor justifica este capítulo (dueño 03 en 08 §9), agrupadas por uso:

| Uso | Claves (nombres exactos de 08 §9) | Sección |
|---|---|---|
| Sustitución y catálogo de niveles | `level_overrides`, `content_version`, `daily_overrides`, `lvl_daily_bracket_levels` | 2, 15, 16 |
| Alivio "El río bajó" | `relief_after_fails`, `relief_mode`, `relief_second_step_fails` | 13 |
| Etiquetas | `lvl_labels_from_level`, `lvl_very_hard_from_level`, `lvl_label_thresholds`, `lvl_label_override` | 7, 11, 12 |
| Aguas turbias | `lvl_murky_from_level`, `lvl_murky_max_cuias`, `lvl_murky_max_hidden_units` (= 4 por la revisión del 06-10: ≤ 24 realidades) | 8 |
| Dificultad adaptativa | `lvl_dda_enabled` (= false en 1.0 por la revisión del 06-10; 1.2), `lvl_dda_risk_window`, `lvl_dda_risk_fails`, `lvl_dda_max_per_cycle` | 14 |
| Solver en cliente | `hint_budget_nodes`, `hint_weight`, `hint_timeout_ms`; `shuffle_budget_nodes`, `shuffle_par_slack`, `shuffle_reveals_murky`; `lvl_endless_gen_budget_ms`, `lvl_endless_fallback_pool` | 5, 8, 16 |
| Paneles | `lvl_kpi_alarm_attempts` (no cambia el juego) | 15 |

Claves de otros capítulos que este usa con su nombre canónico: `mech_debut_level.*`, `mech_enabled.*` y `lluvia_k_pours` / `pirana_k_pours` (02; rigen si `attrs[i].k` falta, sección 10); `restart_confirm_min_pours`, `skip_min_fails`, `skip_min_gap_levels`, `skip_seed_price`, `rescue_min_level`, `rescue_max_per_level`, `undo_unlimited_until_level`, `extra_cuia_unlock_level`, `hint_unlock_level` (04); `streak_*` (05); `live_daily_offset` (06; sección 16); `ui_murky_bonus_enabled` (00; false en 1.0, true en 1.1; secciones 8 y 16); `anim_river_lowered_banner_ms` (04). Alias retirados que este capítulo ya no nombra (sustituidos por la clave `lvl_*` o la indicada): `murky_from_level`, `murky_max_cuias`, `murky_max_hidden_units`, `labels_from_level`, `very_hard_from_level`, `label_thresholds`, `dda_*`, `daily_bracket_levels`, `kpi_alarm_attempts`, `endless_*`, `shuffle_max_par_delta` → `shuffle_par_slack`, `skip_after_fails` → `skip_min_fails`.

## Desviaciones de la dirección

Ninguna respecto a las 22 decisiones de dirección. Dos desviaciones respecto al informe de investigación research/02 §7.1, justificadas con medición: (1) el pipeline del informe fija una profundidad inversa objetivo (18/30/46, extendida a 60-90) y selección greedy por complejidad; el prototipo demostró que el paseo inverso estricto muere a profundidad 12-27 y que la selección por complejidad lo mata a ~8, así que la profundidad pasa a ser un tope, se adopta "mejor prefijo" y DFS profundo, y el relleno aleatorio se incorpora como fuente para tableros densos con 2 vacíos; (2) el informe propone generar los niveles 1-11 a mano y el resto por curva: aquí se curan a mano 1-200 (dirección 17). Dos precisiones, no desviaciones: (a) la condición de victoria para capacidades mixtas se formaliza como "llena, monocolor y visible" (02 R5 y R12; sección 3), lo que la dirección 2 llama "recipiente completo se sella"; (b) la dirección 3 permite 13-14 colores en hitos desde el mundo 9, pero 14 colores + 1 vacío exigen 15 cuias y 02 R1 limita a 14 cuias generadas: los hitos de campaña juegan K = 13 y K = 14 queda para el evento Encuentro de las Aguas (sección 11).

## Preguntas abiertas para el dueño del producto

1. **Cerrada** (coherencia con 02 R1 y §14): ningún nivel generado supera 14 cuias; los hitos desde el 561 juegan 13 colores + 1 vacío en 14 cuias; K = 14 solo en el evento Encuentro de las Aguas, con Cuia extra y Cuia de rescate deshabilitadas (`rescue_max_per_level` = 0 en ese modo) para no superar las 16 ranuras en pantalla. Queda por confirmar con el dueño únicamente si el evento acepta ese tablero de 15 cuias en tres filas o se juega también con 13 colores.
2. **Cerrada** para 1.0 (revisión del 06-10): Reto del río con tres tramos por progreso (A/B/C = bandas 1-3, K 6-7 / 8-9 / 10-11) y 2.190 tableros = 3 × 730 (sección 16, 06 §4.1, 08 §4.1), en lugar de un tablero único para todos: los tramos protegen al jugador nuevo a costa de fragmentar la conversación social ("¿resolviste el de hoy?"). Queda como propuesta para 1.2 un tablero único por tramo en la Liga del río.
3. **Cerrada** (revisión del 06-10): primer Difícil en el 40 y primer Muy difícil en el 60 (4 + 1 etiquetas en 60 niveles; `lvl_labels_from_level` = 40, `lvl_very_hard_from_level` = 60); 05 y `tools/econ_sim.py` (`FIRST_HARD_LEVEL` = 40, `FIRST_VERY_HARD_LEVEL` = 60) ya usan esta cadencia. Una cadencia más densa tipo Magic Sort (tramo 43-112 "expert") queda como hipótesis para después del soft launch.
4. Alivio por defecto "+1 vacío" (misma imagen del tablero) frente a "−1 color" (tablero más corto): confirmar la preferencia o pedir A/B solo sobre el tipo de alivio (no sobre su existencia).
5. **Cerrada**: la sección 9 copia literalmente la columna "Regla exacta" de 02 §9.1 y los debuts de `mech_debut_level` (43 · 63 · 83 · 103 · 123 · 143 · 183; Cuia chica 14, Cuia alta 27 y de 6 en hitos desde el 141); cualquier cambio futuro se hace primero en 02 y se propaga aquí. Con esos valores todos los bloqueadores caen en la posición 3 del ciclo, así que el ajuste de plantilla que se pedía para 34 y 106 ya no existe.
6. ¿Se publica el histograma de intentos de la comunidad junto a la etiqueta (research/09 §Mejorar) desde 1.0 o se espera a tener 2 semanas de datos?
7. **Cerrada** (08 §2.4): pipeline en C# (consolas .NET 10 `tools/level-generator` + `tools/level-qa`, que comparten con Unity `AF.Core` y `AF.Solver` del paquete `Packages/com.aquazonia.core`, sin `UnityEngine`) para que pista, Pororoca, Río sin fin y pipeline usen el mismo código; `tools/levelgen_proto.py` queda como prototipo de referencia (paridad de hash y par en el test de portado).

## Dependencias con otros capítulos

| Capítulo | Toma de este capítulo | Devuelve a este capítulo |
|---|---|---|
| 00 Glosario | Nombres y IDs (Aguas turbias, Difícil/Muy difícil, mecánicas, mundos, paleta, `max_pares_riesgo`), numeración de capítulos | Validación de rangos de nivel y debuts |
| 02 Núcleo jugable, reglas y UX | Predicados de victoria/sellado (sección 3, copiados de R5 y R12), detección de Aguas estancadas (= `movimientos_utiles(S) = ∅`, 02 R13) y `futil` única (02 §1.4), semántica de mecánicas para el solver (sección 9, copiada de 02 §9.1), tabla del tutorial 1-10 (02 §13, copiada sin alterar), límites de Aguas turbias (T2: 4 dosis / 24 realidades) | Catálogo definitivo de mecánicas (02 §9.1), niveles de debut (`mech_debut_level`: 14 · 27 · 43 · 63 · 83 · 103 · 123 · 143 · 183), T ≤ 14 generadas y layout de hasta 16 cuias en pantalla (02 R1, §5, §14) |
| 04 Comodines, Canoas y Cuia de rescate | **Definición canónica de fallo (04 §5.2)** y `fails_level` (`restart_confirm_min_pours` = 8), presupuestos de Morpho/Pororoca/Luciérnaga, regla de Morpho en Aguas turbias (solo vertidos seguros; sin seguro → mensaje de 04 §2.3 a, sin cobro), regla "par' ≤ par + `shuffle_par_slack`", Salto del Boto (`skip_min_fails` = 5, `skip_min_gap_levels` = 10), Cuia de rescate desde `rescue_min_level` = 31 | **Cerrada**: topes de comodines en 08 §9 (Marea ilimitada en 1-10 y 3/nivel desde el 11; 1 Cuia extra y 1 Cuia de rescate por nivel) y comportamiento de Cuia extra en la variante aliviada (04 §2.2, caso b: con Cuia extra aplicada se sirve la variante −1 color, nunca +1 vacío). Pendiente: qué se sirve en ese caso cuando `relief[]` solo lleva `plus_empty` (sección 13) |
| 05 Economía y monetización | Recompensas por etiqueta (20/60/80) y su relación con `bot_fail`; precio fijo de la Cuia de rescate (600) y del Salto del Boto (300); tasas de fallo de la sección 15 para `tools/econ_sim.py` | **Cerrada**: topes de rescate (600 semillas fijas desde el 31, primer rescate del día a 300, máximo 1 por nivel; `rescue_price_bands` = `[[31,600]]`) y metas de ARPDAU (≥ 0,015 LatAm fase 1; ≥ 0,08 US antes de UA; 05 §9) |
| 06 Meta, retención y live-ops | Reto del río por tramos (bandas K 6-7 / 8-9 / 10-11 de la sección 16, canon) y pool fechado de 2.190 tableros, Aguas turbias · Bonus (1.1), Río sin fin, reglas de eventos sin DDA, Crecida (05 §2.4) y Hoja escudo en la señal de riesgo | Calendario de eventos y niveles especiales (Travesía, Encuentro de las Aguas) |
| 07 Arte, temática y audio | Layout de 14 cuias generadas + Cuia extra + Cuia de rescate (16 en pantalla, tres filas si T ≥ 15; 02 §5), icono de etiqueta, tarjeta "El río bajó", "?" de Aguas turbias, Patrones de fruta, fondo "río genérico" en 201-700 (mundos 6-9; Loreto 141-200 por PAD; sección 17) | Restricción final de cuias por pantalla |
| 08 Arquitectura técnica y publicación | Esquema `af-level/1` (08 §4.1 y `schemas/af-level-1.schema.json`) con la semántica de la sección 10: `attrs` `{type, key, partner, n, k}` sin `barro`, `D` sin límites (sección 7), `par` nulo solo en `plus_empty` y `relief` con al menos una variante desde el 31 (sección 13), `id`/`seed` del pool diario y `difficulty` opcional en `kind: daily` (sección 16); codificación `len` + (tinte − 1) (sección 3), 700 niveles instalados / 701+ en `levels_remote` (08 §6.2), claves de Remote Config (08 §9, única tabla), `AF.Core`/`AF.Solver`/`tools/level-generator`/`tools/level-qa` (08 §2.4), eventos de analítica (taxonomía única de 08 §8.2, con `af_dda_applied` y `af_label_shown`), solver en hilo de fondo, CI de reproducibilidad | Límites de tamaño y formato final de Addressables; esquema de guardado de `fails_level` y variantes |
| 09 Roadmap, equipo, KPIs y riesgos | Plan de la sección 17 (días por tamaño de equipo), cadencia +100, debuts de la sección 9 para 09 §1.1, lectura del KPI "primer fallo real" de 09 §6.1 (sin reto suave en el 19; sección 12) | Fechas y asignación |
