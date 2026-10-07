# Revisión de jugabilidad y algoritmos de Tintalú

Fecha: 2026-10-07. Auditoría del árbol local. Documento de propuesta: no cambia el GDD, las decisiones del dueño, los contratos ni el código. Se mantienen **D-001: una dosis por vertido** y **D-002: sin destinos resaltados al seleccionar**.

## Dictamen

Hay un núcleo pequeño, determinista y comprobable sobre el que merece la pena seguir. Todavía no hay evidencia de que la experiencia completa sea divertida, de que la dificultad se entienda o de que una economía comercial encaje con este modo de juego. Lo prioritario es demostrar eso con un tramo corto y pulido antes de producir cientos de niveles, obstáculos y ofertas.

El principal riesgo no es que falten reglas: el GDD ya contiene muchas. Es ampliar el producto usando medidas del modo `run`, mientras se juega `single`, y confundir un tablero resoluble para A* con una experiencia comprensible y satisfactoria para una persona.

## Qué existe realmente

| Área | Implementado y comprobable | Diseñado o pendiente |
|---|---|---|
| Núcleo | Vertido de una dosis; modo `run` explícito; legalidad, sellado, victoria, fútiles; reinicio y deshacer | Mecánicas con atributos; memoria de revelado persistente |
| Juego | 10 tableros; selección tap-tap; cola/encadenado de animación; modal de bloqueo; victoria | Tutorial guiado, pista, Cuia extra, sonido, economía, vidas, mapa y metajuego |
| Solver | A* canónico; heurística específica de `single`; solución reproducida con reglas compartidas; distingue agotamiento de presupuesto de irresolubilidad | Servicio de pistas cancelable y acotado por tiempo/memoria; seguridad informacional de ocultas; mecánicas avanzadas |
| Contenido | Generador/verificador del tutorial hecho a mano; informe BFS exhaustivo + A* | Generador procedural C# y `level-qa` todavía son andamiaje; el bot casual ×40 no está portado |
| Guardado | Guarda el siguiente nivel al ganar; tras el 10 vuelve al 1 | Partida interrumpida, historial, inventario, transacciones y migraciones |

Evidencia: `docs/dev/prototipo-v0.md:7` y `:19`; `Assets/_Project/Scripts/GameFlow/LevelFlow.cs:61` rechaza niveles fuera del tramo de Marea ilimitada; `Packages/com.amazonfill.core/Runtime/AF.Core/Session/LevelSession.cs:53` rechaza ocultas; `Assets/_Project/Scripts/Levels/LevelPackReader.cs:217` rechaza mecánicas; `tools/level-generator/Program.cs:120` y `tools/level-qa/Program.cs:62` fallan cerrado cuando se solicita la parte no implementada. Esta delimitación es buena: evita fingir soporte.

## Hallazgos prioritarios

### 1. Recalibrar la experiencia para `single` antes del nivel 11

**Evidencia:** `tools/out/tutorial_v0_report.md:15` informa pares `single` 3, 9, 10, 12, 16, 17, 15, 17, 16, 19. En el nivel 2 toda solución óptima repite tres veces un mismo vertido; el nivel 6 puede bloquearse en un solo movimiento. `TapController.cs:71` deselecciona después de cada dosis, por lo que la solución óptima requiere dos toques por movimiento. Son decisiones actuales, no fallos del código.

**Consecuencia:** la dificultad cognitiva y el trabajo manual se separan. El salto 1→2 triplica el par, pero parte del aumento consiste en repetir una decisión. Un nivel de 19 dosis no es necesariamente más interesante que uno de 15. La animación ya encadena operaciones iguales (`BoardAnimator.cs:49` y `:137`); eso reduce traslados visuales, pero no los toques que debe hacer el jugador.

**Propuesta:** mantener D-001/D-002 y hacer el tutorial enseñable con una instrucción breve por lección: seleccionar/verter; una dosis por toque; reservar espacio; deshacer; planificar. No reintroducir destinos resaltados. Las ayudas que señalen origen/destino deben seguir siendo una petición explícita, conforme a la decisión pendiente P5. No imponer que el par aumente en todos los niveles: un valle intencional después del 6 puede ser buen diseño. Sí evitar saltos de carga sin enseñanza y comprobar comprensión antes del primer bloqueo deliberado.

No asumir que las lecciones están ya en pantalla: el propio alcance de v0 excluye dedo guía y tooltips. La ausencia de explicación impide interpretar un atasco del usuario como dificultad del puzzle con suficiente confianza.

### 2. La medida de dificultad aún no representa el juego actual

**Evidencia:** `docs/gdd/03-niveles-generacion-dificultad.md:232` reconoce que la puntuación del bot usa cantidad movida y penaliza el vertido parcial; ambos términos pierden significado en `single`. `:248` define un índice D con pesos propuestos; `:234` pide calibración humana futura. `tools/out/tutorial_v0_report.md:10` declara que el bot ×40 no se calcula. Los tiempos y tasas del prototipo Python corresponden a `run`.

**Propuesta técnica:** publicar internamente tres medidas separadas antes de combinarlas:

1. **Carga de ejecución:** par de dosis, mínimo de toques, longitud de secuencias repetidas y tiempo de animación. Comparar también `par_single / par_run` como indicador diagnóstico de repetición, sin usarlo como dificultad pública.
2. **Dificultad de decisión:** proporción de movimientos que conservan solución, profundidad hasta que se manifiesta un error, necesidad de abrir espacio y ciclos que no progresan. El número de nodos de A* sirve para coste computacional; no es una medida de diversión.
3. **Experiencia observada:** primer vertido correcto, rechazos por toque, deshacer, reinicios voluntarios, bloqueos, tiempo activo, abandono y capacidad de explicar la regla con palabras propias.

Portar bots con estrategias distintas —juntar colores, conservar vacíos, visión de pocos pasos y una proporción de elecciones aleatorias— y contrastarlos con personas. Los 40 intentos dan una resolución de 2,5 puntos porcentuales; un resultado de 30/40 cae justo en 75 %, con incertidumbre importante. Los casos cercanos al umbral necesitan más muestras o revisión; la etiqueta y la recompensa no deberían cambiar por ruido de muestreo. Los pesos nuevos, rangos y umbrales requieren aprobación como cambio de diseño.

### 3. El modo no está ligado al contrato del pack

**Evidencia:** `tools/level-generator/Program.cs:25` declara que el pack no lleva modo; `LevelFlow.cs:168` carga el tablero con el modo de la sesión. `SolverOptions.cs:58` y `:68`, `LevelSession.cs:70` y `PourRules.cs:84` aún conservan APIs heredadas que eligen `run`. La T6 ya está identificada en `docs/dev/decisiones.md:322`.

**Riesgo:** el juego visible usa `single` correctamente, pero una herramienta o futuro servicio de pistas puede omitir el parámetro y calcular un par/ruta con otra regla. No he encontrado evidencia de que esa mezcla esté ocurriendo en el flujo actual; es una vía de regresión concreta.

**Propuesta:** resolver P7, versionar pack/reglas, incorporar el modo al identificador de análisis y guardado, rechazar incompatibilidades y retirar las APIs implícitas al cerrar T6. Un experimento `run` necesita su pack y sus métricas. No basta cambiar una clave remota.

Hay además una copia desactualizada a corregir con la decisión documental: `docs/gdd/03-niveles-generacion-dificultad.md:292` describe Remolino solo con racha, mientras `docs/gdd/02-nucleo-reglas-ux.md:337` ya diferencia `single` y `run`. Esa mecánica aún no existe en el cliente.

### 4. Ocultas: demostrar una estrategia con información visible

**Evidencia de diseño, no bug de runtime:** el pseudocódigo `RUTA_SEGURA` de `docs/gdd/03-niveles-generacion-dificultad.md:262` enumera realidades, exige que cada realidad tenga solución después del movimiento (`:272`) y continúa por el estado real usando su par (`:274`). La garantía se afirma sobre esa ruta (`:277`). Actualmente `LevelSession` rechaza ocultas.

**Límite lógico:** que para cada realidad exista alguna solución con conocimiento completo no demuestra que exista una estrategia común que pueda elegirse usando solo lo revelado. Las soluciones de dos realidades pueden exigir decisiones incompatibles antes de que el jugador logre distinguirlas. El algoritmo descrito tampoco recorre todas las ramas de observación futuras. Esto es una garantía no demostrada; no prueba que los cinco ejemplos del prototipo estén mal.

**Propuesta:** un verificador offline AND/OR sobre conjuntos de estados compatibles con lo visto. En cada nodo OR se elige un movimiento usando esa información; el nodo AND exige una continuación ganadora para cada observación posible después de verter/revelar. Se rechazan ciclos sin progreso y resultados desconocidos. Se conserva un certificado para QA, separado del pack público si así sigue el contrato. Morpho debe elegir la misma pista ante estados indistinguibles y nunca desempatar usando información real que el jugador aún desconoce.

El límite de 24 realidades acota la enumeración, pero no garantiza por sí solo el tiempo de búsqueda. Medir la verificación completa en el dispositivo, incluyendo todas las llamadas a A* y estados desviados de la ruta original. Posponer ocultas hasta tener la máscara de conocimiento, undo/reinicio que conservan revelados y esta prueba.

### 5. Presupuesto agotado no equivale a «no hay salida»

**Evidencia:** el núcleo lo diferencia bien: `AStarSolver.cs:142` devuelve `BudgetExceeded` y `:151` solo devuelve `Unsolvable` cuando agota la búsqueda. `SolveStatus.cs:12` documenta esa diferencia. Sin embargo, `docs/gdd/04-comodines-vidas-rescate.md:66` prescribe «Desde aquí no hay salida» cuando no encuentra solución dentro del presupuesto. Es un contrato futuro; Morpho no está en v0.

**Propuesta:** mantener tres resultados hasta la UI: solución encontrada, irresolubilidad demostrada, búsqueda inconclusa. Para el último: «No pude calcular una pista ahora», sin cobro. No activar rescates de pago ni etiquetar el tablero de imposible por un timeout. El comportamiento de rescate de `04:340` ya distingue los tres; armonizar el texto y la decisión de oferta sin prometer que una compra garantiza solución cuando no está probado.

Para integrar el solver al cliente faltan cancelación y límites globales: `SolverOptions.cs:31` solo ofrece nodos/peso/regla/modo; `AStarSolver.cs:315` puede crecer arrays al añadir hijos y `:350` duplica almacenamiento. El test de cero asignaciones mide un solver ya calentado (`HotLoopAllocationTests.cs:89`), no su primer crecimiento. No es un problema demostrado de rendimiento en los 10 tableros; debe medirse antes de poner pistas en móvil. Usar una tarea por instancia, memoria reservada acotada, cancelación al cambiar nivel/estado, y descartar respuestas que pertenezcan a otra revisión de tablero.

### 6. Persistencia suficiente para una demo, insuficiente para compras

**Evidencia:** `PlayerPrefsProgressStore.cs:18` guarda solo `af_v0_level`; `LevelFlow.cs:386` lo actualiza al ganar. `GameBootstrap.cs:182` sincroniza la vista al volver de segundo plano, pero no persiste la partida en curso. Es una exclusión explícita de v0, no una regresión. Reiniciar ejecuta directamente `LevelFlow.cs:331`, sin la confirmación futura.

**Propuesta:** antes de vender o limitar deshacer, guardar de forma recuperable el estado lógico, modo/versión, historial, conocimiento revelado, variante de alivio, recursos consumidos, intento y transacciones pendientes. Persistir el commit lógico, no un frame de la animación. Probar cierres después del cobro, después de la concesión, durante victoria, en el modal y con un resultado de anuncio duplicado. Distinguir progreso alcanzado de último nivel jugado y dejar de envolver al nivel 1 al finalizar el contenido comercial.

### 7. La simulación económica no predice intención de compra

**Evidencia:** `tools/econ_sim.py:88` deriva tasas de fallo de objetivos de diseño; `:214` define perfiles fijos y `:244` fija probabilidades de compra; `:445` determina el fallo sin jugar el tablero. `docs/gdd/05-economia-monetizacion.md:238` reconoce que no modela abandono, elasticidad de precio ni demanda dependiente del saldo.

**Lectura correcta:** sirve para balances y sensibilidad bajo supuestos, no para afirmar que los jugadores comprarán ni para elegir la frustración óptima. La recalibración de `single`, D-002, deshacer y tutorial debe preceder a cualquier ajuste comercial basado en esos resultados.

**Propuesta:** mantener la solvencia sin consumibles pagados como gate por nivel; medir victoria sin compra, repetición voluntaria y retención además de ingresos. La regla de dificultad y el algoritmo de selección de tablero no deben usar historial de compra o propensión a gastar. Para monetizar, primero demostrar valor en continuidad, presentación y colección; evaluar compras deterministas y cosméticas como hipótesis de producto, sin convertir errores del solver o límites de recursos en presión artificial.

## Secuencia técnica recomendada

| Orden | Entrega concreta | Criterio para pasar |
|---|---|---|
| 1 | Vertical de 10 niveles con dirección visual candidata, feedback, explicación breve y D-001/D-002 intactas | Sesiones observadas con nuevos jugadores; comprensión y legibilidad verificadas; disfrute de la acción repetida, no solo capacidad de terminar |
| 2 | Contrato de modo + guardado de partida + cierre T6 | Reanudar y reproducir exactamente un intento; pack y solver nunca mezclan reglas |
| 3 | Generador C# y `level-qa` funcionales; bot adaptado; corpus curado pequeño | Reproducibilidad, solución verificada en `single`, solución sin compras y métricas de carga humana; todos los estados críticos reproducibles |
| 4 | Tramo 11-30 con una novedad por lección y recuperación deliberada | Retención y dificultad medidas por nivel; calibrar cupo de Marea antes de monetizarlo |
| 5 | Pistas explícitas y alivio comunicado; primera mecánica temática | Prueba de latencia/memoria, resultado desconocido sin cobro, mismas reglas que el cliente |
| 6 | Economía instrumentada y prueba de ofertas; ampliación de contenido | Balance con datos reales, transacciones idempotentes, compras opcionales; decisiones sobre futuro catálogo separadas de dificultad |
| 7 | Ocultas y mecánicas de disparador | Garantía de estrategia observable, canon y estado completos, pruebas específicas y límites comprobados |

No propongo producir de entrada los 700 niveles planificados. Un catálogo pequeño validado reduce el coste de corregir `single`, tutorial, arte y economía simultáneamente.

## Pruebas y límites de esta revisión

- Inspección de GDD 02–05, decisiones D-001/D-002, runtime, informes generados, generador y QA.
- `dotnet build dotnet/AmazonFill.slnx -warnaserror`: código 1 inicial, con «0 advertencias / 0 errores» y sin diagnóstico suficiente. No se considera verde.
- Reintento diagnóstico: `dotnet build dotnet/AmazonFill.slnx -warnaserror --no-restore --verbosity normal -m:1`: **código 0, 0 advertencias, 0 errores**. El servidor compartido de Roslyn dio timeouts de conexión; la compilación de respaldo terminó correctamente en 87 s. No se cambiaron versiones ni configuración.
- `dotnet test dotnet/AmazonFill.slnx --no-build --no-restore -m:1 --verbosity minimal`: la primera ejecución aislada se anuló al no conectar `vstest` con `testhost` en 90 s; no produjo un resultado de casos. Se repitió una sola vez fuera del sandbox, con aprobación automática, para permitir la comunicación local: **código 0; 235/235 correctas, 0 fallidas, 0 omitidas, 5 s**.
- `git diff --check`: código 0. El único archivo de trabajo creado por esta revisión es este informe; compilación/pruebas produjeron sus salidas normales ignoradas en `dotnet/artifacts/`.
- No se ejecutó Unity, ni se generaron packs, ni se cambió código o GDD. No se repitió la simulación económica, porque no se alteraron cifras ni el modelo. La tabla del tutorial es evidencia del informe existente, no una nueva medición de personas.

La batería .NET cubre Core/Solver, incluyendo comparación con BFS y propiedades en ambos modos. No sustituye pruebas Unity, pruebas de teléfono, accesibilidad ni playtests. Existen pruebas de selección sin destinos y de encadenado, pero no se presentan aquí como ejecutadas.
