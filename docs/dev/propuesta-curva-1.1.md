# Propuesta: curva de dificultad de la 1.1 (menos cuias, más retos)

- **Fecha:** 2026-11-06. **Autor:** diseño de niveles (agente). **Estado:** propuesta, sin decidir. No cambia el GDD, el código ni los datos.
- **Petición del dueño (literal):** «la estrategia de agregar y agregar más cuias no es viable, debemos ver cómo lo hacen otros, como Magic, que es como por ciclos, para no saturar solo de cuias sino de retos o de formas diferentes que debe superar en los niveles más arriba, colocando cuias congeladas que toca descongelarlas, tapadas, únicamente con colores, y de esta forma baja un poco la intensidad de tantas cuias y se vuelve más interesante, pero a la vez en la cúspide de esos niveles va subiendo de dificultad.»
- **Fuentes del repo:** `docs/gdd/02-nucleo-reglas-ux.md` (§5, §9.1, §9.2, §10, §11), `docs/gdd/03-niveles-generacion-dificultad.md` (§7, §9, §11, §12), `docs/gdd/08-arquitectura-tecnica-publicacion.md` (§4.4, §9), `docs/research/02-generacion-niveles-dificultad.md`, `docs/research/09-referencia-magic-sort.md`, `docs/research/magic-sort/*`, `docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md` (D-007), `docs/dev/decisiones.md` (D-007, D-009, D-027), `tools/level-generator/specs/campaign_v1.json` y `campaign_v2.json`, y los 700 niveles publicados.
- **Marcas:** [V] verificado con fuente (URL o archivo); [E] estimado; [NV] no verificado; [P] provisional, lo valida el dueño.

---

## 1. Resumen para el dueño

1. **Tiene razón con los números.** Desde el 301, el 70 % de los niveles tiene 13 o 14 cuias (278 de 400) y la media es 12,7. Hasta el 300 la cuenta subía nivel a nivel: 8 → 10 → 11 → 12 → 13.
2. **Las cuias de más no dan dificultad, dan trabajo.** En 301-700, los niveles de 13 cuias son los *fáciles* (fallo del bot 45 %) y los retos tienen 12 (68 %). Los tableros más grandes son los de comodidad: 11 colores con 2 cuias vacías.
3. **Ya tenemos el «diente de sierra» corto** (el ciclo de 20 de D-009, copiado del patrón de Magic Sort y Royal Match): respiro, retos que suben, remanso y pico. Lo que falta es lo que usted pide: que el **tamaño** también tenga ciclos, y que **los picos crezcan** con retos y no con cuias.
4. **Los picos de hoy no crecen.** Del hito 200 al 700, todos dan entre 90 % y 100 % de fallo del bot y la solución mínima no pasa de 24-34 movimientos. Lo único que sube pico a pico es el tamaño: 11-12 cuias en el 200-300, 13 en el 320-560 y 14 en el 580-700.
5. **Magic Sort mantiene los tableros más chicos.** En las fichas públicas de sus niveles 200-442 hay 10-12 frascos y 6-11 colores. Sus respiros bajan a 6 colores (el 420: 11 frascos, 6 colores, 2 vacíos). Lo difícil lo ponen los bloqueadores: corchos, candados, cortinas y cajas [V levelsolve].
6. **Propuesta de tamaño:** topes por papel en el ciclo. Respiros, 9-10 cuias; comodidad, 9-11; retos, 10-11 (12 como mucho); hitos, 12-13; y 14 solo en la **cúspide**, el último hito de cada mundo. En una muestra de 40 niveles la media baja de 12,7 a 10,3 cuias y de 10,7 a 8,7 colores; 38 de 40 niveles tienen 11 cuias o menos.
7. **Propuesta de dificultad:** cada pico sube un **escalón de presión** con palancas que no son cuias: un solo vacío, cuias congeladas, contenido oculto, cuántas mecánicas a la vez y la Cuia chica como único respiro. Dentro de cada mundo los hitos suben del escalón 3 al 7. El mundo siguiente vuelve a empezar desde un escalón más arriba: es un diente de sierra con dos escalas.
8. **Las tres mecánicas que pide ya están medio diseñadas:**
   - **(a) congelada = Aguapé** (M8, prevista para la 1.1 por D-007). Es el «hielo» de Magic Sort con tema del río: un grupo de 2-5 cuias recibe, pero no da, hasta que una del grupo sella. Propongo además el **Aguapé espeso**, que pide 2 sellos.
   - **(b) tapadas:** sacar Aguas turbias de los niveles difíciles. En los Normales, 1-2 dosis ocultas como mucho; en los retos y los hitos, hasta 4. Para la «cuia que llega tapada» está la **Lancha** (M11, como la cortina de Magic Sort).
   - **(c) un solo color = Hoja atada.** Ya existe desde el 283 y sale en 47 niveles. Hay otra lectura de su frase: «tapadas que solo se abren con un color». Eso ya lo hace el **Brote**. Se lo pregunto (P6).
9. **Los jugadores no pierden nada.** Un nivel ya jugado no cambia; la regla de 08 §4.4 lo prohíbe. La nueva curva se aplica desde el primer nivel que cada jugador no ha jugado y que esté entre el 301 y el 700. Del 701 en adelante nace con ella. Se puede apagar desde Remote Config.
10. **1.1:** curva nueva, Aguapé (y espeso), Lancha, más contenido oculto y más Hoja atada. **1.2:** Palizada (la «tapada con llave»), Cocha y las variantes encadenadas. Antes de publicarla, una prueba A/B de 2 semanas con la mitad de los jugadores.

---

## 2. Diagnóstico de la curva actual

### 2.1 Datos medidos

Script propio sobre los 700 niveles publicados (`tutorial_v0.json` y `mundo_02_v1.json` a `mundo_09_v1.json`, variante «b» de `campaign_v2.json`, sin la Tinaja), Python 3.14. Su salida está en el scratchpad de la sesión (`curva.py`, `niveles.json`); no se versiona.

- T = cuias del tablero.
- K = colores.
- Vacíos = cuias vacías de capacidad 4 o más.
- Bot = mediana de `difficulty.bot_fail`, el bot casual ×40.
- Par = mediana del par.
- Ocultas = niveles con `hidden`, y entre paréntesis las dosis ocultas.

| Bloque | T mín-mediana-máx | Niveles con T ≥ 12 / ≥ 13 | K mediana (rango) | Con mecánica | Con 2+ mecánicas | Ocultas | Difícil / Muy difícil | Bot | Par |
|---|---|---|---|---|---|---|---|---|---|
| 1-50 | 3-8-10 | 0 / 0 | 7 (2-7) | 5 | 0 | 0 (0) | 2 / 0 | 23 % | 15 |
| 51-100 | 8-10-11 | 0 / 0 | 8 (7-9) | 23 | 5 | 1 (2) | 10 / 3 | 45 % | 17 |
| 101-150 | 10-11-12 | 3 / 0 | 9 (8-9) | 29 | 8 | 3 (6) | 12 / 2 | 35 % | 20 |
| 151-200 | 10-11-12 | 6 / 0 | 9 (8-9) | 30 | 10 | 3 (7) | 11 / 3 | 40 % | 19 |
| 201-250 | 11-12-13 | 41 / 1 | 10 (9-10) | 28 | 10 | 2 (4) | 11 / 2 | 38 % | 21 |
| 251-300 | 11-12-13 | 38 / 2 | 10 (9-10) | 29 | 12 | 3 (8) | 12 / 2 | 46 % | 21,5 |
| 301-350 | 12-13-14 | 50 / 43 | 11 (10-11) | 29 | 11 | 1 (3) | 12 / 2 | 44 % | 23 |
| 351-400 | 12-13-14 | 50 / 42 | 11 (10-11) | 29 | 12 | 2 (3) | 12 / 3 | 49 % | 23,5 |
| 401-450 | 12-13-13 | 50 / 32 | 11 (10-11) | 29 | 11 | 2 (2) | 11 / 2 | 45 % | 22 |
| 451-500 | 11-13-13 | 47 / 33 | 11 (10-11) | 28 | 13 | 3 (6) | 10 / 3 | 50 % | 23 |
| 501-550 | 11-13-13 | 48 / 29 | 11 (10-11) | 27 | 11 | 2 (3) | 12 / 2 | 50 % | 23 |
| 551-600 | 11-13-14 | 49 / 31 | 11 (10-12) | 27 | 12 | 3 (6) | 10 / 3 | 46 % | 23 |
| 601-650 | 11-13-14 | 48 / 36 | 11 (10-13) | 26 | 13 | 1 (3) | 10 / 2 | 46 % | 23 |
| 651-700 | 11-13-14 | 48 / 32 | 11 (10-13) | 28 | 13 | 3 (5) | 10 / 3 | 51 % | 23 |

Los datos que dio el dueño se confirman, con dos matices. Las «turbias» son 1-3 niveles por bloque de 50 y solo en retos o hitos: 29 niveles en total, el 4 %, con 1-3 dosis ocultas cada uno. La Tinaja no está publicada.

**Uso de cada mecánica en los 700:** Brote 80 · Hoja 66 · Barro 63 · Liana (con fruta y de cuenta) 61 · Matapi 58 · Remolino 54 · Hoja atada 47 · Piraña 37 · Lluvia 34 · Flor 18.

### 2.2 Por posición del ciclo de 20 (301-700, 20 niveles por posición)

| p | Papel (03 §12) | T | K | Vacíos | Mecánicas | Bot | D |
|---|---|---|---|---|---|---|---|
| 1-2 | Recuperación | 12 | 10 | 2 | 0 | 11-14 % | −0,7 |
| 3-5 | Comodidad | 13 | 11 | 1-2 | 0-1 | 34-46 % | −0,13 |
| 6, 9, 12, 15, 18 | Reto | **12** | **10** | 1 | 1-2 | 78-85 % | 0,26 → 0,76 |
| 7-8, 10-11, 13-14, 16-17 | Comodidad | **13** | **11** | 1-2 | 0-1 | 38-51 % | −0,2 a 0,06 |
| 19 | Remanso | 13 | 11 | 2 | 0 | 36 % | −0,3 |
| 20 | Hito | 13-14 | 11-13 | 1 | 2-3 | 100 % | 1,07 |

**Fallo del bot según el tamaño del tablero (301-700):**

| T | Niveles | Bot (mediana) |
|---|---|---|
| 11 | 10 | 84 % |
| 12 | 112 | 68 % |
| 13 | 264 | 45 % |
| 14 | 14 (hitos) | 88 % |

### 2.3 Qué dicen los datos

1. **El tamaño no fabrica dificultad.** La dificultad la ponen el vacío único y las mecánicas; las 13 cuias de las comodidades solo alargan el nivel. Viene de D-027: los retos de 421-700 bajaron a K = 10 porque con 11-12 colores salían Muy difíciles, y las comodidades se quedaron con 11 colores + 2 vacíos.
2. **No hay ciclos de tamaño.** Del 301 al 700 todas las posiciones tienen 12-13 cuias; ni el respiro ni el remanso bajan de 12. En Magic Sort los respiros bajan de verdad (sección 3).
3. **Los picos están saturados en la medida.** El bot casual da 90-100 % en todos los hitos desde el 200, así que no distingue un pico del siguiente. El par de los retos tampoco sube: 21-26 en cada ciclo de 301-700, con un pico de 34 en 381-400. El único crecimiento real es T: 14 cuias desde el 580. Hace falta otra medida para que los picos puedan crecer de forma comprobable (sección 6.4).
4. **La variedad ya es alta.** Hay mecánica en el 56 % de los niveles, y de 2 o más en el 24 %. Lo que falta son los **tipos de reto** que pide el dueño: congelar, tapar y abrir con un color. Hoy las ocultas están limitadas a 29 niveles por la regla T1 de 02 §11.
5. **Lo que ya está bien** y se conserva: el ciclo de 20 de D-009, con nunca dos etiquetas seguidas, el remanso antes del hito y la comodidad tras cada reto; la etiqueta decidida por el bot; los estrenos de 2-3 tableros fáciles; la bolsa de mecánicas por temporada; y el tope de 2 mecánicas antes del 300 y 3 después.

---

## 3. Cómo lo hacen Magic Sort y otros referentes

### 3.1 Magic Sort (Grand Games)

**Tamaño de los tableros (fichas de levelsolve: composición del tablero [V]; la dificultad de esas fichas es una nota de la comunidad, no la etiqueta del juego):**

| Nivel | Frascos | Colores | Vacíos al empezar | Bloqueadores | Nota comunitaria / movimientos objetivo | Fuente |
|---|---|---|---|---|---|---|
| 150 | sin dato | 6 | sin dato | Paneles de madera que tapan frascos | expert / 44 | [V] https://levelsolve.com/magic-sort/level/150 |
| 200 | ~9-10 | 10 | 1 | Ninguno | hard / 34 | [V] https://levelsolve.com/magic-sort/level/200 (choca con research/09, que lo da como respiro de 2 vacíos) |
| 221 | 12 | 8 | 0 libres; 2 con candado que se abren a mitad | 6 corchos de color que tapan un color | hard / 34 | [V] https://levelsolve.com/magic-sort/level/221 |
| 280 | 10 | 8 | 0 (uno casi vacío) | Ninguno | hard / 38 | [V] https://levelsolve.com/magic-sort/level/280 |
| 331 | 12 | 11 | 0 | Cortina que cae con 3 vacíos en la fila | expert / 50 | [V] https://levelsolve.com/magic-sort/level/331 |
| 400 | 10 posiciones | 9 | 1 (chico) | 3 cajas regalo que tapan frascos | medium / 30 | [V] https://levelsolve.com/magic-sort/level/400 |
| 420 | 11 | **6** | 2 | Ninguno (respiro) | medium | [V] https://levelsolve.com/magic-sort/level/420 |
| 442 | 10 | 9 | 2 | 2 cajas con candado de color en cadena | hard / 36 | [V] https://levelsolve.com/magic-sort/level/442 |

**Lectura:**
- Entre el 150 y el 442, Magic Sort no pasa de 12 frascos en la muestra [V].
- Sus picos (331, 221) tienen 0 vacíos libres y bloqueadores que los liberan a mitad del nivel, no más frascos [V].
- Sus respiros caen a 6 colores con 2 vacíos [V].
- Nuestra escala general de research/09, con 10-13 frascos en el medio juego y 8-11 colores en 200-500 [V], cuadra con la muestra.

**Ritmo:**
- Diente de sierra por diseño: tramos duros largos y respiros «puros» sin bloqueadores en el 24, 200, 280 y 420 [V, research/09, levelsolve].
- Etiquetas Hard y Super Hard: los jugadores dicen que caen «cada pocos niveles» [NV, reseñas de App Store, https://apps.apple.com/us/app/magic-sort/id6499209744].
- Nuestra grabación de 387-389: pico sin etiqueta, después un respiro con mecánica nueva (hielo) y después un Super Hard (`docs/research/magic-sort/propuesta-tintalu.md` §5.5) [V, observación propia].

**Catálogo y estreno de bloqueadores:**

| Bloqueador | Estreno | Fuente |
|---|---|---|
| Frasco chico | ~14 | [V] research/09 |
| Frascos cubiertos | 25-60 | [V] research/09 |
| Frasco «?» | ~70 | [V] research/09 |
| Paneles de madera | 150 | [V] research/09 |
| Corchos de color y candados dorados | 221 | [V] research/09 |
| Cortina | 331 | [V] research/09 |
| Caja regalo | 400 | [V] research/09 |
| Caja fuerte en cadena | 442 | [V] research/09 |
| Hielo | visto en el 388 | [V] observación propia |
| Portales, slime, llaves, spray | sin nivel | [NV] |

- La App Store resume su diseño así: cada etapa trae «bloqueadores nuevos», como frascos cubiertos, frascos que solo aceptan un color y huecos con candado. También da el consejo de revelar primero los frascos con una sola incógnita [V] (App Store Story, https://apps.apple.com/nz/iphone/story/id1873942358).
- Un solver de terceros enumera los objetos que reconoce: etiquetas, llave-candado, tubos cortos con número de recargas, tubos ocultos, colores ocultos, bases de piedra o niebla, **segmentos de hielo** y cajas [V que el solver lo afirma; NV sobre el juego] (https://alternativeto.net/software/magic-sort-solver/about/).
- **Frecuencia:** en la grabación de 387-389, los tres niveles llevaban bloqueadores, y el 389 llevaba 3 a la vez [V, observación propia]. La crítica de Gamigion: «las mecánicas nuevas se introducen tarde y carecen de novedad»; entre el 100 y el 220 el juego vive de frascos cubiertos y tamaño variable [V] (https://www.gamigion.com/game-deconstruction-magic-sort-by-grand-games/).
- **Lo que no copiamos:**
  - el «?» en niveles normales sin ruta segura (la queja n.º 1 del líder; 02 §11);
  - los picos sin etiqueta (387);
  - los bloqueos a los 1-2 vertidos.

### 3.2 Otros referentes

| Juego | Dato | Fuente |
|---|---|---|
| Royal Match (100 primeros niveles) | 8 «hard» (39, 45, 49, 55, 65, 75, 85, 95) y 5 «super hard» (59, 69, 79, 89, 99); un nivel normal ≈1,2 intentos, uno hard ≈1,6 y uno super hard ≈2,5; 11 mecánicas en 91 niveles, cada una con 2-3 tableros de aprendizaje | [V] research/02 §5 |
| Royal Kingdom vs Royal Match | Hard o Super Hard cada 10 niveles, con un nivel fácil después de cada Super Hard | [NV] la página devolvió 403; resumen del buscador: https://medium.com/@chrisjameswallace/royal-kingdom-the-next-puzzle-hit-from-dream-games-c7395e26b185 |
| Candy Crush | Tras cada nivel muy difícil, 5-6 fáciles o medios | [V] research/02 §1 |
| indiecoredev (puzzle casual) | Ciclo de 20: 12 comodidad, 5 reto, 2 recuperación y 1 hito; saltos de +2,2 / −3,4 bandas; las decisiones pesan más que el tamaño del tablero | [V] https://dev.to/indiecoredev/difficulty-rates-a-stage-progression-places-it-5243 · https://dev.to/indiecoredev/rating-puzzle-difficulty-by-decisions-3k5b |
| ELIMINATION (puzzle de palabras, artículo académico) | El generador produce a propósito una curva en diente de sierra, medida con ~1.000 jugadores | [V según el resumen del buscador; NV leído] https://api.deepai.org/publication/elimination-from-design-to-analysis |
| Water Sort Puzzle (IEC Global) y clones | Escalan sobre todo con más tubos y colores, con niveles «mystery» con «?» en algunos clones; sin bloqueadores en el clásico | [V] research/09 (App Store id1514542157); [NV] el «?» en clones |
| Ball Sort / Sort It 3D | No encontré fuentes fiables de sus obstáculos (bolas congeladas, tubos con candado): los resultados son páginas SEO | [NV] |
| Guías de dificultad de water-sort | Lo difícil viene del orden de los movimientos y del uso del único vacío, no de las reglas | [E] https://playwatersort.pages.dev/blog/water-sort-tips-for-hard-levels |

### 3.3 Lo que sacamos

1. **El tamaño se mueve en ciclos y se mantiene por debajo de ~12** salvo en algunos picos. Los respiros bajan de verdad en colores [V Magic Sort].
2. **Los picos los hacen los bloqueadores y la falta de vacíos**, no más frascos [V: 221 y 331].
3. **Novedad constante:** algo nuevo cada ~60-100 niveles, con 2-3 tableros de aprendizaje [V Royal Match]. Nosotros ya lo exigimos: «nunca más de 60 niveles sin algo nunca visto», D-009 punto 8.
4. **Picos cada 5-10 niveles y un nivel fácil tras cada pico** [V Royal Match y Candy Crush]: ya es nuestro ciclo de 20.

---

## 4. Propuesta para Tintalú

### 4.1 Dos escalas de diente de sierra

**Escala corta (el ciclo de 20 de D-009, sin cambios de orden):** recuperación en p1-p2, comodidad o estreno en p3-p5, retos en p6, p9, p12, p15 y p18, remanso en p19 e hito en p20. Las reglas del escalón siguen: nunca dos etiquetas seguidas, comodidad tras cada reto, retos que suben dentro del ciclo y hito tras remanso.

**Lo nuevo de la escala corta: el tamaño también sube y baja.** Topes por papel [P]:

| Papel | Posiciones | T (cuias) | K (colores) | Vacíos | Mecánicas |
|---|---|---|---|---|---|
| Respiro (recuperación y remanso) | p1, p2, p19 | 8-10 | 6-8 | 2 | 0 |
| Estreno | p3-p5 del ciclo de debut | 9-10 | 7-8 | 2 | 1, un solo ejemplar |
| Comodidad | p3-p5, p7-p8, p10-p11, p13-p14, p16-p17 | 9-11 | 7-9 | 1-2 | 0-1 |
| Reto | p6, p9, p12, p15, p18 | 10-11 (12 como mucho) | 9-10 | 1 | 1-2 (3 desde el 301) |
| Hito | p20 | 12-13 | 10-12 | 1 | 2-3 |
| **Cúspide** | el último hito de cada mundo (firma) | 13-14 | 11-12 | 1 | 3 |

- Cumple lo que pidió el dueño: volver a 8-11 la mayor parte del tiempo y guardar 12-14 para los picos. 15 de las 20 posiciones quedan en 11 cuias o menos.
- **Más cuias no es lo mismo que más colores.** El trabajo del jugador crece con K, porque cada color son 4 dosis que mover. Por eso el tope es por T y por K a la vez.
- **Evidencia de que los retos de 10-11 cuias siguen siendo retos:** en 101-200 los retos tenían T = 11 y K = 9-10, y el bot falló el 76-84 % (mediana de p6, p12 y p18) [V, datos publicados]. En Magic Sort, el 280 (10 frascos, 8 colores, 0 vacíos) es «hard» [V levelsolve].
- **Ventaja de presentación:** con T ≤ 10 el tablero siempre va en 2 filas de ≤ 5 (02 §5). Las cuias se ven más anchas, dentro de los topes de ancho de D-004, y los grupos del Aguapé nunca se parten, porque con 11-14 el número de filas depende del teléfono (02 §5).

**Escala larga (nueva): un macrociclo por mundo, con la cúspide en la firma.** Cada hito sube un **escalón de presión** respecto al anterior del mismo mundo; el mundo siguiente vuelve a bajar y arranca un escalón más arriba que el anterior. Ejemplo para el mundo 7 (301-420, 6 ciclos):

| Hito | 320 | 340 | 360 | 380 | 400 | **420 (cúspide)** |
|---|---|---|---|---|---|---|
| Escalón | 3 | 4 | 5 | 5 | 6 | 7 |
| T | 13 | 13 | 13 | 13 | 13 | 14 |

En el mundo 8 (421-560) los hitos van del 4 al 8, y en el 9 (561-700), del 5 al 9.

**El escalón de presión** [P] es la suma de las palancas que lleva el nivel. Ninguna es «una cuia más»:

| Palanca | 0 | 1 | 2 |
|---|---|---|---|
| Vacíos | 2 | 1 | 1, y ocupado por un Aguapé o sustituido por una Cuia chica |
| Congeladas (Aguapé) | ninguna | 1 grupo de 2-3 | 2 grupos, o 1 Aguapé espeso |
| Ocultas | ninguna | 1-2 dosis | 3-4 dosis (tope de T2) |
| Mecánicas distintas | 1 | 2 | 3 |
| Disparador o contador (Liana, Lluvia, Piraña, Lancha) | no | sí | dos que se encadenan (1.2) |
| Colores K del hito | ≤ 10 | 11 | 12 (solo en la cúspide) |

- Los retos llevan 1-3 escalones; los hitos, de 3 a 9 según el mundo.
- Así el pico crece aunque T se quede en 13: en el 340, 2 grupos de Aguapé y 3 dosis turbias; en el 420, además, 12 colores y 3 mecánicas.
- El bot sigue poniendo la etiqueta (D-009): el escalón **no** sustituye al bot, solo guía al generador.
- Para comprobar que los picos crecen hace falta la medida de la sección 6.4.

### 4.2 Mecánicas nuevas

Los nombres son los de D-007 (glosario §6.5), que aún no ha revisado el asesor. Todo nombre o variante nuevo es «provisional, lo valida el dueño».

#### (a) Cuia congelada: **Aguapé** (M8, ya en el GDD para la 1.1)

- **Por qué no «hielo»:** en la Amazonía no hay hielo, y D-007 eligió la mata de aguapé que flota y sujeta las cuias (`propuesta-mecanicas-dificultad.md`, b.2 M8). Hace la función del hielo de Magic Sort (el 388), del que copia la regla [V, observación propia]. Si el dueño prefiere ver «hielo» o «escarcha» literal, es la pregunta P4.
- **Regla (02 §9.1 M8):**
  - una mata cubre la base de un grupo de 2-5 cuias contiguas;
  - las cuias del grupo **reciben** con la regla normal, pero **no son origen**;
  - **se descongela** todo el grupo en cuanto una cuia del grupo **sella** (R10, etapa d).
  - El contenido se ve.
- **Qué ve el jugador:** una alfombra de hojas redondas con flores lavanda hasta media altura de las cuias. Al soltarse, la mata se parte en trozos que se alejan flotando, en ~700 ms, con sonido de agua que se abre.
- **Variante nueva [P]: Aguapé espeso.** Un grupo de 4-5 cuias que necesita **2 sellos dentro del grupo**; la mata se aclara con el primero y se va con el segundo. Es la palanca «2» de congeladas sin añadir cuias. Coste S: el umbral (`n` = 1 o 2) va en `attrs`, y la suelta se deduce del número de cuias selladas del grupo, sin bits nuevos.
- **Con las reglas R1-R16:**
  - **R4-R5:** una cuia del grupo puede sellar recibiendo; al sellar queda cerrada como cualquier otra, y es justo lo que suelta el grupo.
  - **R10:** la suelta va en la etapa (d), con las Lianas: en el mismo vertido que sella.
  - **R12:** al final las cuias del grupo tienen que estar selladas o vacías.
  - **R13-R14 y `futil`:** mientras no se suelte, la cuia del grupo no es origen, así que no hay vaivén. Como destino, verter en ella nunca es fútil si así se acerca a sellar; se mantiene la regla normal de `futil`, sin exención (anexo B de D-007).
  - **Validador:** al menos una cuia del grupo puede sellar solo recibiendo; el nivel no empieza atascado.
  - **Marea:** si deshace el sello, vuelve la mata (se deduce del estado).
  - **Pororoca:** las cuias bajo la mata quedan fijas.
  - **Cuia extra y Cuia de rescate:** son normales.
- **Combinaciones [P]:** el Aguapé es un **filtro de origen** como el Matapi o el Brote. Propongo permitirlo con Aguas turbias, porque la ruta segura lo cubre igual que a los filtros de la 1.0. No va con la Tinaja (D-019) ni con la Palizada en la misma fila (02 §9.2, regla 7).
- **Estreno:** el GDD lo ponía en el 223. Con el alcance de la sección 5 (201-300 no cambia) pasa al **303**, en p3-p5 del ciclo 301-320, con 2 vacíos, solo y con un ejemplar. Los jugadores que ya pasaron el 303 lo ven con la regla de migración de 08 §4.4: su tablero de estreno, una vez, antes del primer nivel que lo use.

#### (b) Más tapadas

**Lo que hay:** Aguas turbias (02 §11; `murky` y `hidden`).
- Las capas ocultas se ven como agua parda con un «?» y se revelan al quedar en la cima (R7). Es exactamente la variante «colores ocultos que se revelan al quedar arriba».
- Va con ruta segura: hay al menos un vertido seguro en todas las realidades posibles.
- Desde la regla T1, solo en Difícil y Muy difícil; hoy son 29 niveles (4 %).

**Propuesta [P] (cambia T1 de 02 §11):**

| Tipo de nivel | Desde | Frecuencia por ciclo de 20 | Dosis ocultas | Cuias turbias |
|---|---|---|---|---|
| Recuperación, remanso | — | 0 | 0 | 0 |
| Comodidad (Normal) | 301 | 2-3 | 1-2 | 1 |
| Reto | 101 (como hoy) | 2 (hoy, como mucho 2) | 2-4 | ≤ 2 |
| Hito | 220 (como hoy) | 1 de cada 2 ciclos | 3-4 | ≤ 3 |

- Con eso, el ~20 % de los niveles de 301-700 lleva algo oculto, frente al 4 % de hoy. Magic Sort lo lleva en casi todos [V, grabación 387-389].
- Se mantienen el tope de 4 dosis y 3 cuias por nivel (T2: ≤ 24 realidades, que Morpho enumera en < 50 ms), la ruta segura (T5) y la prohibición de combinarlas con disparadores y contadores (Q14 A).
- **Estreno en Normal:** 2 tableros fáciles (323-324 en la muestra) con 1 dosis oculta y la línea del tutorial de T4.
- **Variante [P], sin regla nueva: «Brote turbio».** Un Brote, que solo se abre con su color, con 1-2 dosis ocultas bajo el tapón. Es la «tapa de color» de los corchos de Magic Sort (221), que esconden un color [V levelsolve]. Ya lo permiten 02 §9.2 regla 7 y T1 en retos; en Normal depende de esta propuesta.

**Cuias que llegan tapadas: Lancha** (M11, en el GDD para la 1.1).
- La cuia de carga, con 1-3 dosis visibles, **no es origen ni destino** hasta que el tablero tiene v cuias vacías; entonces atraca.
- Es la cortina de Magic Sort (331) [V levelsolve: cae con 3 vacíos].
- Premia consolidar, no se puede explotar con vertidos fútiles y no añade azar.
- Estreno propuesto: **363**, en p3-p5 del ciclo 361-380 (el GDD lo ponía en el 303, ahora ocupado por el Aguapé).

**Tapada con llave: Palizada** (M9). Queda para la 1.2 (sección 7). Es la más cara: cada dosis-llave necesita una marca, hay cadenas, y el riesgo de llave enterrada es el bloqueo del 389 de Magic Sort.

**Tapa total (la «tela» de Magic Sort, que esconde la cuia entera).** El dueño la descartó en D-007 (Q17 C). No la propongo para la 1.1; si la quiere, es la pregunta P7.

#### (c) «Únicamente con colores»

Hay tres lecturas razonables:

| Lectura | Qué es | Estado en Tintalú | Coste |
|---|---|---|---|
| **A. Cuia de un solo color** | Solo acepta un color concreto, siempre | **Hoja atada** (variante de la 1.0, estreno en el 283, 47 niveles): solo recibe su fruta, sí es origen y nunca se suelta. Como mucho una por fruta y su capacidad es la `U_c` de su fruta (D-024) | 0: solo la spec. Propongo subirla a 4-6 niveles por ciclo, y 2 en algunos retos (329) |
| **B. Tapada que solo se abre con un color** | El corcho de color de Magic Sort | **Brote**: la tapa del color de su cima se abre al recibir ese color (80 niveles). El **Brote de dos hojas** pide 2 dosis. La **Hoja** se abre con el primer vertido de su fruta | 0 (el Brote turbio de (b) añade el «tapado») |
| C. Niveles «solo de colores» | Niveles puros, sin mecánicas | Ya son p1, p2, p19 y 3 comodidades por ciclo (≥ 30 %, D-009) | 0 |

- **Recomendación:** A como lectura principal, con B a través del Brote turbio. Las dos ya existen en el núcleo, así que la 1.1 lo resuelve con la especificación.
- Si el dueño pensaba en otra cosa, por ejemplo una **cuia que solo acepta un color y además está congelada**, es una combinación de Hoja atada y Aguapé sin regla nueva.
- Pregunta P6.

### 4.3 Reparto de las mecánicas que ya existen

| Mecánica | Hoy (700 niveles) | Propuesta 301-700 | Por qué |
|---|---|---|---|
| Hoja atada | 47 | 4-6 por ciclo, 2 por nivel en algún reto | Es el «un solo color» que pide el dueño |
| Brote, Hoja | 80, 66 | Igual; el Brote, también turbio | Filtros baratos que se leen bien |
| Barro | 63 | Menos en comodidad; en retos, con el Aguapé | Es «más trabajo» más que reto |
| Liana (con fruta y de cuenta) | 61 | Igual | Ya libera a mitad de nivel, como el candado de Magic Sort (221) |
| Remolino | 54 | Menos (2-3 por ciclo) | Cuesta leerlo con tableros chicos y tiene poca relación con los retos nuevos |
| Matapi | 58 | Igual; con el Aguapé en retos | Un sumidero; con el Aguapé hace «vacío útil» |
| Lluvia, Piraña | 34, 37 | Retos e hitos; un contador por nivel (como hoy) | Palanca de contador |
| Flor | 18 | Igual | — |
| Aguapé (nueva) | — | 6-8 en su ciclo de debut, después 3-5 por ciclo | La palanca principal de los picos |
| Lancha (nueva) | — | 8 en su ciclo, después 2-3 por ciclo | Llega con contenido a mitad de nivel |
| Ocultas | 29 (17 en 301-700) | ~80 niveles en 301-700 | Tabla de (b) |

Se mantienen las reglas de la bolsa de D-009: ≥ 30 % de niveles puros por ciclo, ninguna mecánica en 3 niveles seguidos fuera del estreno, 6 niveles por ciclo como mucho, temporadas y una novedad cada ≤ 60 niveles.

### 4.4 Muestra: 40 niveles de 301-340 (dos ciclos)

Se pidió «un ciclo y medio»: son 40 niveles, es decir, dos ciclos completos de 20.

- T = K + vacíos (+ Cuia chica).
- El bot ×40 es la ventana del slot, con la etiqueta de 02 §10: 75-89 % Difícil y ≥ 90 % Muy difícil.
- «Sube» es un reto; «Pico» es un hito.

| Nivel | p | Papel | T | K | Vacíos | Mecánicas | Palanca de presión | Bot ×40 |
|---|---|---|---|---|---|---|---|---|
| 301 | 1 | Respiro | 9 | 7 | 2 | — | — | < 30 % |
| 302 | 2 | Respiro | 10 | 8 | 2 | — | — | < 30 % |
| 303 | 3 | Estreno | 9 | 7 | 2 | Aguapé (grupo de 2) | novedad | < 20 % |
| 304 | 4 | Estreno | 10 | 8 | 2 | Aguapé (grupo de 2) | novedad | < 20 % |
| 305 | 5 | Estreno | 10 | 8 | 2 | Aguapé (grupo de 3) | novedad | < 20 % |
| 306 | 6 | Sube (reto) | 10 | 9 | 1 | Brote + Hoja atada | 1 vacío | 75-89 % |
| 307 | 7 | Comodidad | 10 | 8 | 2 | Aguapé (grupo de 2) | — | 20-60 % |
| 308 | 8 | Comodidad | 10 | 9 | 1 | — | — | 20-60 % |
| 309 | 9 | Sube (reto) | 10 | 9 | 1 | Aguapé (grupo de 3), sola | 1 vacío + congelada | 75-89 % |
| 310 | 10 | Comodidad | 11 | 9 | 2 | — | — | 20-60 % |
| 311 | 11 | Comodidad | 10 | 8 | 2 | Hoja | — | 20-60 % |
| 312 | 12 | Sube (reto) | 10 | 9 | 1 | Aguapé + Barro | 1 vacío + congelada | 75-89 % |
| 313 | 13 | Comodidad | 9 | 8 | 1 | — | — | 20-60 % |
| 314 | 14 | Comodidad | 11 | 8 | 2 + 1 chica | Matapi | — | 20-60 % |
| 315 | 15 | Sube (reto) | 11 | 10 | 1 | Brote + 2 dosis turbias | ocultas | 75-89 % |
| 316 | 16 | Comodidad | 10 | 8 | 2 | — | — | 20-60 % |
| 317 | 17 | Comodidad | 11 | 9 | 2 | Liana de cuenta | — | 20-60 % |
| 318 | 18 | Sube (reto) | 11 | 10 | 1 | Aguapé (grupo de 4) + Hoja atada | congelada grande | 75-89 % |
| 319 | 19 | Respiro (remanso) | 9 | 7 | 2 | — | — | 20-40 % |
| 320 | 20 | Pico (hito) | 13 | 12 | 1 | Aguapé + Brote + Lluvia | escalón 3 | ≥ 90 % |
| 321 | 1 | Respiro | 9 | 7 | 2 | — | — | < 30 % |
| 322 | 2 | Respiro | 10 | 8 | 2 | — | — | < 30 % |
| 323 | 3 | Comodidad | 10 | 8 | 2 | 1 dosis turbia (Normal) | novedad: oculta en Normal | < 20 % |
| 324 | 4 | Comodidad | 10 | 8 | 2 | 1-2 dosis turbias (Normal) | novedad | < 20 % |
| 325 | 5 | Comodidad | 11 | 9 | 2 | Hoja atada | — | 20-60 % |
| 326 | 6 | Sube (reto) | 10 | 9 | 1 | Aguapé (grupo de 3) + 2 dosis turbias | congelada + ocultas | 75-89 % |
| 327 | 7 | Comodidad | 10 | 8 | 2 | Remolino | — | 20-60 % |
| 328 | 8 | Comodidad | 11 | 10 | 1 | — | — | 20-60 % |
| 329 | 9 | Sube (reto) | 11 | 10 | 1 | Hoja atada ×2 + Barro | 1 vacío | 75-89 % |
| 330 | 10 | Comodidad | 10 | 8 | 2 | — | — | 20-60 % |
| 331 | 11 | Comodidad | 10 | 9 | 1 | Brote | — | 20-60 % |
| 332 | 12 | Sube (reto) | 11 | 9 | 1 + 1 chica | Aguapé (grupo de 3) + Matapi | vacío útil = chica | 75-89 % |
| 333 | 13 | Comodidad | 9 | 8 | 1 | — | — | 20-60 % |
| 334 | 14 | Comodidad | 10 | 8 | 2 | Liana con fruta | — | 20-60 % |
| 335 | 15 | Sube (reto) | 11 | 10 | 1 | Hoja + 3 dosis turbias | ocultas | 75-89 % |
| 336 | 16 | Comodidad | 10 | 8 | 2 | — | — | 20-60 % |
| 337 | 17 | Comodidad | 11 | 10 | 1 | Aguapé (grupo de 2) | — | 20-60 % |
| 338 | 18 | Sube (reto) | 11 | 10 | 1 | Brote + Piraña | contador | 75-89 % |
| 339 | 19 | Respiro (remanso) | 9 | 7 | 2 | — | — | 20-40 % |
| 340 | 20 | Pico (hito) | 13 | 12 | 1 | 2 grupos de Aguapé + 3 dosis turbias + Barro | escalón 4 | ≥ 90 % |

**Resumen de la muestra frente a lo publicado en 301-340:**

| | Muestra | Publicado |
|---|---|---|
| Media de T | 10,3 | 12,9 |
| Mediana de T | 10 | 13 |
| Media de K | 8,7 | 10,9 |
| Niveles con T ≤ 11 | 38 de 40 | 0 de 40 |
| Niveles con T ≥ 13 | 2 (los hitos) | 35 de 40 |
| Niveles con mecánica u ocultas | 26 | 23 |

**Riesgos de la muestra [E]:**
- Con K = 9 y 1 vacío, un reto sin palanca puede quedarse por debajo del 75 % del bot. La salida es la de D-009: se publica sin etiqueta, o la curación elige otro tablero del pool.
- Hay que medir cuántos retos colocan con estos topes antes de fijarlos (fase F2).
- 323-324 rompen la regla de debut «una sola mecánica nueva en p3-p5» solo si las ocultas en Normal cuentan como estreno; propongo tratarlas como estreno.

### 4.5 Ritmo de contenido y economía

Con menos colores, cada nivel es más corto [E: ~par 19 frente a 23, porque el par es ≈ 2,2·K con `walk`; 03 §18]. Eso tiene dos efectos:
- el jugador avanza más niveles por hora: más Semillas por hora y más intersticiales por hora;
- se gastan antes los 700 niveles.

Antes de publicar hay que pasar `tools/econ_sim.py` con la duración nueva por nivel. Propongo **no tocar el premio por nivel** (P12) y que el contenido de 701+ (`levels_remote`) llegue a su ritmo de 08 §6.2.

---

## 5. Qué pasa con los jugadores que ya avanzaron

**Regla fija (08 §4.4):** un paquete nuevo **nunca** cambia el tablero base de un nivel publicado. Los cambios solo entran con `level_overrides` o con una vuelta nueva, y el cliente rechaza un `pack_schema` mayor del que conoce.

| Opción | Qué cambia | Para quién | Pros | Contras |
|---|---|---|---|---|
| **A. Solo 701+** | Los mundos 10-13 nacen con la curva nueva | Todos, al llegar al 701 | Cero riesgo para lo publicado | El 70 % de 301-700 se queda con 13-14 cuias: el problema sigue donde están los jugadores |
| **B. 301-700 futuros + 701+ (recomendada)** | Packs de sustitución para 301-700, que el cliente 1.1 aplica **solo a los niveles por encima del máximo que el jugador había alcanzado** al abrir la 1.1 por primera vez | Cada jugador, desde su siguiente nivel no jugado | Arregla la zona saturada sin tocar lo jugado; se puede apagar | Más trabajo de generación y curación (400 niveles) y un selector en el cliente |
| C. 201-700 completo | También 201-300 (T de 11-13, media 11,8) | Igual que la B | Curva uniforme | +100 niveles de trabajo por poca ganancia (201-300 ya ronda 12) |

**Detalle de la B [P]:**
1. **Perfil:** al primer arranque de la 1.1, una migración (`profile_schema` + 1, 08 §4.4) guarda `curve2_from_level` = máximo alcanzado + 1. Es inmutable: los niveles por debajo siguen siendo los de la 1.0, también al rejugarlos desde el Mapa.
2. **Packs:** `mundo_07` a `mundo_09` de sustitución, más la parte de 301-320 del mundo 6, generados con una `campaign_v3.json` nueva. El `id` es nuevo para no romper la regla de 08 §4.4 (propuesta técnica en la sección 6.6). Mismos números de nivel, mismos hitos y paradas del Mapa (D-013) y mismas firmas de mundo.
3. **Remote Config:** `lvl_curve2_enabled` (bool, por defecto `true` en la 1.1), que también sirve de **segmento A/B**: con `false` se juega la 1.0. `level_overrides` sigue valiendo encima. No se meten 400 entradas en `level_overrides`, que no está hecho para eso.
4. **Guardado en la nube (Play Games, Saved Games):** `curve2_from_level` viaja con el perfil. Si se fusionan dos dispositivos, gana el **menor** valor: más niveles nuevos y nunca un nivel ya jugado que cambia.
5. **Estrenos:** si el jugador ya pasó el 303 o el 363 al llegar la 1.1, la regla de migración de 08 §4.4 le sirve una vez el tablero de estreno del Aguapé o de la Lancha antes de su primer nivel con esa mecánica.
6. **Lo que no cambia:** progreso, Semillas, racha, Liga, Mapa y Reto del río. El número del nivel es el mismo; solo cambia el tablero.
7. **Clientes 1.0:** no se enteran. Los packs nuevos (`pack_schema` 5) solo los pide el cliente 1.1, y los mundos 10+ con mecánicas de la 1.1 exigen la 1.1 (`biz_min_app_version` para esos packs, o una versión `pack_schema` 3 de reserva si se quiere servirlos a la 1.0; recomiendo exigir la 1.1).

**Dato que falta:** cuántos jugadores han pasado del 300 desde la publicación (2026-10-31). Sale de `af_level_*` en Firebase Analytics, que puede leer el dueño. Si fueran casi ninguno, la B equivale a cambiar 301-700 para todos.

---

## 6. Impacto técnico

### 6.1 Núcleo (`Packages/com.amazonfill.core`, AF.Core y AF.Solver)

| Pieza | Aguapé (+ espeso) | Lancha | Ocultas en Normal | Curva de tamaño |
|---|---|---|---|---|
| Reglas (`BoardRules`, 02 §1.2-1.4) | Filtro de origen mientras el grupo no tenga n sellos; suelta en R10 (d), deducida del estado | Filtro de origen y destino hasta tener v vacías; atraque en R10 (f), 1 bit que Marea restaura | Ninguna (ya existe) | Ninguna |
| Estado y `Cuia.Packed` | id de grupo (2-3 bits) y n (1 bit) en los bits libres | 1 bit «atracada» y v en `attrs` | — | — |
| Clave canónica y hash de 64 bits | La clave suma `group` y `n`; **los ids de grupo se renumeran en orden canónico** (las cuias se ordenan al canonicalizar, 03 §3). Los hashes de los niveles de la 1.0 no deben cambiar: golden tests | Suma el bit de atraque | — | — |
| Solver A* | Heurísticas h1 y h2 sin cambio (un filtro no rompe la admisibilidad); comprobarlo con P1-P3 | Igual | — | — |
| Bot ×40 | Usa `legal`: sin cambio | Sin cambio | Sin cambio | — |
| `DeadEndProbe` / Aguas estancadas (R13) | Una cuia bajo mata sigue siendo destino: hay vertidos legales; probar que «bloqueado hasta sellar» con todos los demás llenos se detecta | Antes de atracar la carga no cuenta como vertido posible; probar el estado «faltan vacías y no hay vertidos útiles» | — | — |
| Validación (`LevelValidation`) | Al menos una del grupo puede sellar solo recibiendo; no atascado en S0; grupo contiguo | v ≥ vacíos iniciales + 1; una lancha por nivel | Ruta segura (ya existe) | — |
| Tests | Propiedad P1-P3 con capacidades mixtas (lección de C1-9 con la Liana), Marea, Pororoca fija | Ídem | — | — |

Tamaño [E]: Aguapé con espeso, M; Lancha, S-M. Lo hace core-dev.

### 6.2 Formato `af-level/1` y esquema (`schemas/af-level-1.schema.json`)

- **`pack_schema` 5** [P] (contrato 21): `attrs.type` admite `aguape` (`group` obligatorio, `n` 1-2 opcional) y `lancha` (`k` = v obligatoria). Campo nuevo `group` en `attrs`. `intro_mechanic` admite `aguape`, `aguape_n2` y `lancha`.
- Campo de nivel opcional **`rows`** [P]: el reparto fijo de filas cuando hay grupos de Aguapé y T ≥ 11 (02 §5: un grupo nunca se parte entre filas).
- `difficulty.pressure` (entero 0-9, opcional) [P]: el escalón de 4.1, para el informe y la analítica. No decide la etiqueta.
- `kind` para los packs de sustitución de la curva (sección 6.6).
- `murky` en niveles con `label` normal ya valida en el esquema. La restricción de T1 la comprueba `level-qa`, no el esquema; cambia la regla de QA.
- Comprobación: `python tools/validate_schema.py`, con ejemplos nuevos para el tipo 5.

### 6.3 Generador (`tools/level-generator`)

- **Spec nueva `campaign_v3.json`:** `t_plan` y `k_plan` **por posición del ciclo** (hoy por rango; D-027), los topes de la tabla de 4.1, el escalón de presión por hito (`pressure_plan` por mundo) y los debuts nuevos (Aguapé 303, Lancha 363).
- **Superposición:** paso para el Aguapé, que elige 2-5 cuias contiguas sin mecánica, con al menos una que pueda sellar recibiendo, y verifica con A*. Paso para la Lancha, que saca una cuia del tablero base y la sube a la carga (b.2 M11).
- **Ocultas en comodidad:** tope de 1-2 dosis, con ruta segura y «S0 sin trampas».
- **Colocación:** el D-027 mostró que un ciclo no coloca si un reto sale fuera de su ventana. Con K más bajo hay que medir antes cuántos retos llegan al 75 % con cada palanca (fase F2) y ajustar `pressure_plan`.
- **Reserva del 20 %:** los slots con Aguapé o Lancha necesitan tablero de reserva sin esa mecánica (kill switches `mech_enabled_aguape` y `mech_enabled_lancha`), con los mismos topes de T.
- **Registro de combinaciones:** regenerar `combinations_registry` y pasar `--validate-spec` (NOVELTY-60, MAX-2 y las reglas de temporada).

### 6.4 QA (`tools/level-qa`) y una medida nueva

- Comprobaciones nuevas:
  - `QA-T-ROLE`: T y K dentro del tope de su papel;
  - `QA-AGUAPE` (grupo contiguo, sellable, `rows` coherente);
  - `QA-LANCHA`;
  - `QA-MURKY-NORMAL` (≤ 2 dosis en comodidad, ninguna en recuperación ni remanso).
- **Medida nueva para que los picos puedan crecer:** el bot casual satura en los hitos (100 %). Propongo un **bot atento ×40** [P]: casual con lookahead de 2 vertidos y sin los vertidos que dejan el tablero bloqueado de inmediato. Solo va en el informe (no en la etiqueta), para comprobar que `bot_atento(hito n+1) ≥ bot_atento(hito n)` dentro de un mundo. Lo hace AF.Solver (core-dev) y lo usa level-qa.
- **Trampas tempranas:** sumar la métrica de la pregunta 5.5 de `propuesta-tintalu.md` (opción B): 0 en Normal y ≤ 1 en Difícil. Importa más con congeladas.

### 6.5 Reto del río

Sin cambios: su pool no lleva mecánicas ni ocultos (03 §16) y sus bandas ya tienen tableros chicos. Opcional: revisar si la banda 3 se siente tan saturada como la campaña (fuera de este alcance).

### 6.6 Cliente (Unity)

- **AF.Levels:** selector de curva con `curve2_from_level` y `lvl_curve2_enabled`; carga de packs con `pack_schema` 5; migración del perfil.
- **IDs de los packs de sustitución [P]:** no pueden ser `m07_v1_n0306`, porque 08 §4.4 lo prohíbe, ni `v2`, que es la vuelta 2 del río. Propuesta: `kind` = `curve`, `pack_id` `curva_{mm}_v1`, id `m{mm}_v1_n{nnnn}_c2` y seed `amazonfill:v1:m{mm}:v1:n{nnnn}:c2`, con `replaces` como en la reserva. Lo decide el tech-lead con el GDD 08 §4.1.
- **Presentación con arte por código (D-052):**
  - Aguapé: la mata, su versión espesa y la suelta en ~700 ms;
  - Lancha: el muelle, la lancha que se acerca con cada vacía y el atraque en ~1,4 s;
  - icono de las mecánicas en la tarjeta del Difícil.
  - Sonidos: `docs/audio/lista-de-sonidos.md` ya tiene entradas del Aguapé; faltan las de la Lancha si no están.
  - El arte definitivo lo hace el artista.
- **Layout:** con T ≤ 10 siempre 2 filas; con `rows` en los datos, respetarlo. La Cuia extra y la de rescate van a la fila más corta y no parten grupos (02 §5).
- **Tutoriales de estreno** (una pantalla con dedo guía, 02 §9.1). Textos [P] que ya están en el GDD:
  - Aguapé: «El aguapé sujeta estas cuias: puedes verter en ellas, pero no sacar. Sella una y se suelta.»
  - Lancha: «La lancha atraca cuando tengas 3 cuias vacías: trae fruta para terminar.»
  - Nuevo [P] para el espeso: «Este aguapé es espeso: sella dos cuias del grupo para soltarlo.»
  - Las ocultas en Normal reutilizan la línea T4.
  - Todo en `strings.csv` (es, en, pt) y con la revisión de D-054.
- **Morpho y Luciérnaga:** ya funcionan con ocultas; con las ocultas en Normal la Luciérnaga se ofrece en más niveles (04 §2.5, sin cambio de precio).
- **Remote Config (08 §9, única tabla):**
  - `mech_enabled_aguape` y `mech_enabled_lancha`;
  - `mech_debut_level_aguape` (303) y `mech_debut_level_lancha` (363);
  - `lvl_curve2_enabled`;
  - `lvl_murky_normal_max_hidden_units` (2).
  - Regenerar con `tools/remote-config/rc_defaults.py`.
- **Analítica:** `af_level_start` y `af_level_end` suman `curve` (1 o 2) y `pressure`, para la prueba A/B (09 §6).

### 6.7 GDD que cambiaría (lo edita el carril de documentación tras decidir)

- 02: §5 (filas fijas por `rows`), §9.1 (Aguapé espeso; debuts del Aguapé 303 y la Lancha 363), §9.2 (combinación Aguapé + turbias) y §11 (T1, ocultas en Normal desde el 301).
- 03: §9 (filas de Aguapé y Lancha), §11 (tabla por rangos con topes por papel), §12 (topes de T por posición y macrociclo por mundo) y §17 (comprobaciones nuevas).
- 08: §4.1 (`pack_schema` 5, `rows`, `pressure`, `kind` `curve`), §4.4 (la curva 2) y §9 (claves nuevas).
- Glosario §6.5 (Aguapé espeso [P]) y GDD.md §2 (decisión nueva).

---

## 7. Plan por fases

Tamaños en días de agente [E]; el orden es el recomendado.

| Fase | Qué | Quién | Tamaño | Versión |
|---|---|---|---|---|
| F0 | Decisiones del dueño (sección 8) y cambios al GDD | Dueño; documentación | S (1-2) | 1.1 |
| F1 | **Medición previa sin código nuevo:** generar con la spec de topes por papel y las mecánicas de la 1.0 un pool de prueba de 301-400 y medir cuántos retos y hitos colocan; dato de jugadores > 300 | level-pipeline-dev; dueño (Analytics) | S-M (2-3) | 1.1 |
| F2 | Núcleo: Aguapé (+ espeso) y Lancha, con P1-P3, Marea, `DeadEndProbe` y golden de hash; bot atento | core-dev | M (5-7) | 1.1 |
| F3 | Esquema `pack_schema` 5, generador (spec v3, superposición, reserva) y level-qa (comprobaciones nuevas, ocultas en Normal) | level-pipeline-dev | M-L (6-9) | 1.1 |
| F4 | Cliente: selector de curva y migración del perfil, presentación y tutoriales, Remote Config y analítica | unity-gameplay-dev | M (5-7) | 1.1 |
| F5 | Generar 301-700 de sustitución, su reserva y el registro; curación del dueño | level-pipeline-dev; dueño | L (generación ~20 min por variante; la curación, del dueño) | 1.1 |
| F6 | Prueba en el teléfono y A/B 50/50 por `lvl_curve2_enabled` durante 2 semanas: abandono por nivel, intentos, sesiones, Semillas por hora y retención D7 | Dueño; analítica | 2 semanas | 1.1 |
| F7 | Mundos 10-13 (701-1000) con la curva nueva | level-pipeline-dev | L | 1.1 o 1.2, según la demanda |
| — | **1.2:** Palizada (+ en cadena + llave en la lancha), Cocha, Lancha de vuelta, Aguapé que crece; tapa total si P7 = sí; ocultas por encima de 4 si Morpho lo aguanta | todos | L | 1.2 |

**Orden recomendado:** F0 → F1 (si F1 muestra que los retos con K 9-10 no colocan, se ajustan los topes antes de escribir código) → F2 ∥ F3 → F4 → F5 → F6. F7 cuando haga falta contenido.

---

## 8. Preguntas para el dueño

| # | Pregunta | Opciones | Recomendada |
|---|---|---|---|
| P1 | ¿Qué niveles cambian con la curva nueva? | A. Solo 701+ · **B. 301-700 desde el siguiente nivel no jugado de cada jugador, y 701+** · C. 201-700 | **B** |
| P2 | ¿Topes de cuias por papel (respiro 8-10, comodidad 9-11, reto 10-11, hito 12-13, cúspide 13-14)? | **A. Sí** · B. Más suaves (comodidad hasta 12) · C. Más duros (todo ≤ 11 salvo la cúspide) | **A** |
| P3 | ¿Dónde está la «cúspide»? | **A. En la firma de cada mundo (420, 560, 700…), con los hitos del mundo subiendo hasta ella** · B. Cada 100 niveles | **A** |
| P4 | ¿Cómo se ve la cuia congelada? | **A. Aguapé (mata de hojas que la sujeta), como en D-007** · B. Hielo o escarcha literal | **A** (en la Amazonía no hay hielo; misma regla) |
| P5 | ¿La congelada recibe vertidos? | **A. Sí, solo no da (como el hielo de Magic Sort y el Aguapé del GDD)** · B. Ni recibe ni da (eso ya lo hace la Liana) | **A** |
| P6 | ¿Qué significa «únicamente con colores»? | **A. Cuia que solo acepta un color (Hoja atada, más frecuente)** · B. Tapa que solo se abre con un color (Brote, y Brote turbio) · C. Niveles solo de colores, sin mecánicas | **A, con B como variante** |
| P7 | ¿Cuánto contenido oculto? | **A. Aguas turbias también en Normal desde el 301, 1-2 dosis, ~20 % de niveles** · B. Como hoy, solo en Difícil y Muy difícil · C. A más la «tela» que tapa la cuia entera (deshace Q17 C de D-007) | **A** (C, en todo caso, en la 1.2) |
| P8 | ¿La Lancha (cuia que llega a mitad de nivel) entra en la 1.1? | **A. Sí, estreno en el 363** · B. En la 1.2 | **A** |
| P9 | ¿La Palizada (tapada con llave) entra en la 1.1? | A. Sí · **B. En la 1.2** | **B** (es la más cara y la de más riesgo de bloqueo) |
| P10 | ¿Probarla con la mitad de los jugadores antes de darla a todos? | **A. Sí, A/B de 2 semanas con `lvl_curve2_enabled`** · B. Para todos desde el primer día | **A** |
| P11 | ¿El Reto del río cambia? | **A. No** · B. Revisar la banda 3 con los mismos topes | **A** |
| P12 | Los niveles serán más cortos: ¿se toca el premio por nivel? | **A. No; se vigilan Semillas por hora e intersticiales con `econ_sim.py`** · B. Bajar el premio | **A** |
| P13 | ¿Se añade el «Aguapé espeso» (pide 2 sellos)? | **A. Sí, desde el ciclo 341-360 como variante** · B. No | **A** (palanca de los picos sin cuias) |

---

## Anexo: cómo se midió

- Script: `curva.py` (scratchpad de la sesión). Lee los 9 packs de campaña, cuenta T = `len(cuias)`, K = `len(palette)`, vacíos = cuias vacías con `caps ≥ 4`, mecánicas = tipos de `attrs` más el Barro (tinte 15) y ocultas = `len(hidden)`. Toma `bot_fail`, `par`, `D`, `band` y `label` de `difficulty`.
- La muestra de 4.4 se comprobó con un segundo script: T = K + vacíos (+ chica) en las 40 filas, media 10,275 y mediana 10.
- No se ejecutaron los comandos de verificación del repo: no se tocó código, datos ni el GDD.
