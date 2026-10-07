# Evaluación y propuesta de mejora de Tintalú

Fecha: 7 de octubre de 2026. Base inspeccionada: `ef676a4`, prototipo Unity y capturas existentes. Dirección elegida por el dueño en esta revisión: **Amazonía ilustrada y cálida, con río, madera, frutas y fauna reconocible**.

Tintalú tiene un núcleo aprovechable, pero todavía debe demostrar una experiencia de juego atractiva y una identidad amazónica reconocible. Recomiendo concentrar el próximo hito en una muestra pequeña y completa: diez niveles revisados, un lugar bien representado, sonido, respuesta táctil y un primer descubrimiento del cuaderno. Después, una prueba de 30 niveles. La expansión a cientos de niveles y la tienda completa deben depender de esa prueba.

Esta revisión propone cambios de diseño; no modifica las reglas, el GDD canónico ni la economía del ejecutable. La elección de estilo sí procede directamente del dueño. Se conservan sus decisiones D-001, una dosis por vertido, y D-002, sin señalar destinos al seleccionar.

## Qué existe y qué falta demostrar

| Área | Evidencia actual | Consecuencia para el siguiente hito |
|---|---|---|
| Núcleo | Reglas, deshacer, reiniciar, sellado, solver A* y tutorial de diez niveles | Conservar y ampliar la verificación sobre esas mismas reglas |
| Interacción | Toque de origen y destino, animación y cola de vertidos | Medir comprensión, toques rechazados y cansancio con una dosis |
| Arte | Gráficos provisionales procedurales, patrones de color y UI funcional | Sustituir la dirección visual visible antes de producir más fondos |
| Contenido | Diez niveles implementados; generación y QA de campaña aún incompletos | No confundir el plan de 700 niveles con contenido disponible |
| Progresión | Se guarda el próximo nivel mediante PlayerPrefs | Falta conservar la partida en curso y probar recuperación |
| Monetización y meta | Diseño, tablas y simulaciones; tienda, vidas, anuncios y cuaderno fuera de v0 | Es el momento de corregir contratos antes de implementarlos |
| Experiencia sensorial | Sonido y arte final fuera de v0 | Una captura no permite juzgar el placer de verter; hace falta audio y prueba táctil |

Fuentes: [alcance del prototipo](../../dev/prototipo-v0.md), [decisiones del dueño](../../dev/decisiones.md), [informe del tutorial](../../../tools/out/tutorial_v0_report.md) y [auditoría técnica](gameplay-algoritmos.md). Se revisaron capturas del dispositivo y de pruebas visuales; esta evaluación no sustituye una nueva sesión de juego en teléfono ni una prueba presencial de competidores.

## Por qué la imagen actual no convence

La pantalla tiene legibilidad básica, pero su personalidad depende casi por completo de verde oscuro y unas palmas. El río aparece como una banda ondulada bajo las botellas; no se perciben la escala del agua, sus orillas ni la vida que ocurre allí. En el nivel 1, el anclaje inferior deja mucho espacio sin una función visual clara. En el nivel 7, la fila superior parece suspendida. Los patrones diferencian dosis, pero no presentan las frutas que les dan nombre.

Esto no es un fallo del núcleo: responde al arte provisional y también a decisiones de composición. `BoardLayout.Compute` coloca el espacio sobrante encima del tablero; `GameScreen` sitúa la banda del río bajo la última fila. Añadir más follaje sobre esa misma estructura no resolverá su falta de lugar, escala y materiales.

| Conservar | Mejorar |
|---|---|
| Líquidos con límites claros, patrones redundantes y cavidad recta | Menisco, brillo moderado y movimiento que comuniquen agua sin tapar dosis |
| Papel, fibra y madera como familias de materiales | Un muelle o soporte visible para cada fila, sombras de contacto y textura discreta |
| Selección únicamente en el origen | Respuesta inmediata al toque y rechazo comprensible, sin pistas encubiertas |
| Botones con texto | Deshacer, pista y reiniciar con siluetas distintas; enseñar nombres temáticos gradualmente |
| Fauna y frutas como identidad | Mostrar sus formas reconocibles en descubrimientos y márgenes, sin ocupar el tablero |

Capturas de referencia: [nivel 1](visual/actual-nivel-1.png) y [nivel 7](visual/actual-nivel-7.png). Implementación relevante: `Assets/_Project/Scripts/UI/BoardLayout.cs`, `GameScreen.cs` y `Presentation/ProceduralArt.cs`.

## Dirección visual elegida

La propuesta **Orillas vivas** presenta un puzzle sobre un pequeño muelle, con el río como paisaje visible y un cuaderno de descubrimientos como recompensa de progreso. La ilustración cálida puede tener zonas claras y luminosas; el área situada detrás de los líquidos conserva valores y detalle controlados. La claridad se consigue mediante composición local, no oscureciendo todo el mundo.

![Concepto de gameplay y cuaderno para Orillas vivas](visual/orillas-vivas-concepto.png)

La imagen es una exploración generada, no una pantalla implementada ni un entregable de arte final. Incluye una pista futura y un cuaderno futuro. La silueta y las medidas de los frascos son orientativas; algunos motivos generados recuerdan cítricos y deben sustituirse por los frutos correctos. No sirve como lámina botánica, mapa geográfico ni prueba de legibilidad en teléfono. El [prompt guardado](visual/prompt-concepto.txt) permite reproducir la intención.

Para el primer mundo conservaría **Estuario, Pará**. Representaría una orilla concreta y documentada, sin combinar en una sola vista todos los hitos de Pará. El proyecto de referencia, Creciente, aporta la lectura del río, la canoa, los muelles, objetos cotidianos y el cambio de creciente a vaciante; su geografía de la triple frontera debe reservarse para el mundo correspondiente.

La Amazonía debe aparecer en cuatro niveles de la experiencia:

1. **Lugar:** horizonte fluvial, profundidad, vegetación y materiales de una escena reconocible.
2. **Acción:** seleccionar, verter, oír el líquido y cerrar una cuia; el paisaje reacciona con discreción al completar.
3. **Descubrimiento:** conocer una fruta, un animal o un objeto por jugar, con una ilustración y una frase opcional.
4. **Pertenencia:** elegir apariencias del muelle y del cuaderno; la colección base se obtiene jugando.

No trasladaría combate, plataformas, jefes ni toda la narrativa de Creciente. Tampoco usaría fauna como castigo permanente ni objetos culturales como decoración intercambiable. El [informe de identidad](identidad-amazonica.md) desarrolla objetos, regiones, dirección artística y fuentes.

### Revisión necesaria de la regla de contraste

GDD 07 §1.2 exige una diferencia mínima de 25 puntos de L* entre cada tinte y el fondo. Los L* publicados de los tintes cubren de 22 a 98 con separaciones pequeñas; los intervalos incompatibles se solapan y cubren todo el rango 0–100. Por tanto, ningún fondo uniforme puede satisfacer esa regla para la paleta completa. Un barrido de los 101 valores enteros también devuelve cero candidatos.

Sustituiría esa condición por comprobaciones del resultado compuesto: contorno y superficie de cada recipiente, separación de dosis, distinción entre blanco y vacío, patrones reconocibles, iconos y modos de visión del color. La validación decisiva debe incluir selección de dosis y colores en el teléfono, a tamaño real. Conservar catorce colores en el catálogo no obliga a mostrar los catorce simultáneamente.

## Una jugabilidad más satisfactoria

La unidad de calidad es una partida corta que se entiende, ofrece una decisión interesante y termina con una recompensa agradable. Una lista larga de mecánicas no garantiza esa experiencia.

**Enseñar antes de bloquear.** El nivel 1 tiene par 3 y el 2 salta a 9 en `single`. El nivel 6 permite alcanzar un bloqueo sin movimientos útiles en un solo vertido. Son datos del informe del tutorial, no tasas de fallo humanas. Revisaría los primeros niveles para enseñar una idea cada vez: dosis, espacio de reserva, deshacer, planificación y recuperación. Que el par suba siempre no es obligatorio: los niveles de descanso son útiles si son deliberados.

**Separar esfuerzo de pensamiento.** Con una dosis por vertido, una solución puede requerir muchos toques repetidos sin nuevas decisiones. Mediría longitud de rachas de acciones equivalentes, proporción de decisiones forzadas y tiempo esperando animación. Mantendría una dosis y ausencia de destinos resaltados; mejoraría rapidez de respuesta, cadencia y continuidad visual sin introducir gestos que muevan varias dosis.

**Hacer atractivo el acto de verter.** Priorizar sonido breve de líquido, un final de vertido distinguible, pequeña vibración opcional y un cierre satisfactorio. El audio debe variar lo suficiente para no convertirse en un ruido repetitivo. Separar volumen de música y efectos; movimiento reducido y controles de patrones disponibles desde el inicio.

**Cuidar interrupciones y errores.** Guardar tablero e historial relevante al pausar; reabrir donde se dejó; confirmar un reinicio que destruya avance; distinguir rechazo de movimiento, falta de movimientos y búsqueda de solución sin resultado. Un timeout del solver no demuestra que el jugador esté perdido.

**Recompensar sin interrumpir.** Al ganar, una celebración breve y un botón siguiente estable. Los descubrimientos del cuaderno pueden abrirse cuando el jugador quiera. Una cadena de recompensa, oferta, evento y anuncio después de cada nivel dañaría el ritmo que estamos intentando mejorar.

## Algoritmos que deben resolverse antes de escalar

| Sistema | Contrato propuesto | Verificación necesaria |
|---|---|---|
| Identidad de nivel | Tablero, semilla, versión de reglas y modo de vertido vinculados | Reproducir y resolver exactamente lo que recibe el cliente |
| Solvencia | Todo nivel de campaña tiene solución sin compras ni anuncios | Guardar una solución testigo y reproducirla con AF.Core |
| Dificultad | Combinar complejidad de decisión, trampas, longitud y pruebas humanas | No usar únicamente par, colores o tasa de fallo de un bot |
| Variedad | Evitar repeticiones disfrazadas de permutación de colores o recipientes | Canonización más familias de patrones y ventanas de repetición |
| Pistas | Usar el estado actual y las mismas reglas; explicar el siguiente paso | `Solved`, `Unsolvable` y presupuesto agotado deben ser resultados distintos |
| Colores ocultos | El jugador dispone de una estrategia con lo que puede observar | Verificador de estados de conocimiento; resolver cada realidad por separado no basta |
| Ayuda adaptativa | Asistencia explícita y próxima selección entre niveles certificados | No cambiar el tablero en curso, ni vincular dificultad a historial de gasto |
| Economía | Entradas, gastos, abandono y falta de anuncios modelados por separado | Simulación con sesiones reales y sensibilidad; ningún resultado sintético prueba retención |
| Compras | Una concesión durable por transacción, con reconciliación | Cierre de app, red interrumpida, reintento, restauración y dos dispositivos |

La garantía de solución inicial no implica que cada decisión del jugador conserve solución. Deshacer y reiniciar deben permitir recuperarse sin que la solvencia dependa de pagar. El nivel servido debe permanecer reproducible durante todos los intentos de ese nivel.

La dificultad adaptativa debería recomendar ayuda, práctica o un siguiente nivel apropiado. No debería aprender a producir frustración para maximizar la probabilidad de compra. Una medida comercial debe ir acompañada de finalización, satisfacción, retorno y reclamaciones.

El [informe de algoritmos](gameplay-algoritmos.md) distingue problemas presentes, límites intencionados del prototipo y riesgos del diseño todavía sin implementar.

## Compras pequeñas con una razón para volver

Mi hipótesis comercial principal es que el jugador quiera seguir habitando ese mundo. Un cuaderno bonito, apariencias visibles y comodidad pueden ofrecer motivos para comprar; hace falta comprobar su valor con jugadores. Si la propuesta se apoya en necesitar un rescate cada vez más caro, el tema queda reducido a un envoltorio.

La simulación actual ya da una alerta concreta: en 300 corridas del perfil casual, el **73 %** tiene al menos un momento en que quiere rescatar y no tiene saldo; el perfil medio termina con **15.510 semillas** de saldo medio. Son escenarios del [modelo B](../../../tools/out/econ_B.md), no usuarios observados. No significan que el 73 % abandone ni que el juego sea imposible gratis. Sí muestran que ajustar un precio no resuelve por sí solo la distribución de recursos.

Propondría validar tres familias de producto antes de construir trece SKU:

- **Bienvenida:** contenido fijo, precio pequeño y descripción completa, ofrecido después de que se haya entendido el juego.
- **Apariencias de orilla y cuaderno:** visibles antes de comprar, sin efectos sobre solvencia, dificultad ni información del tablero. Debe existir una colección gratuita atractiva.
- **Sin anuncios intersticiales:** compra permanente si finalmente se introducen esos anuncios; explicar por separado los vídeos opcionales y sus recompensas.

Los precios del [informe comercial](competencia-economia.md) son hipótesis de validación, no precios aplicados ni una previsión de ingresos. La fase de diversión debe probarse primero sin intersticiales; introducir publicidad después requiere medir de nuevo la experiencia.

También corregiría antes de implementar dos contratos del GDD: que un comodín propio quede inutilizable precisamente al bloquearse, y que la visibilidad de una protección dependa de cuánto convierta. El jugador debe poder anticipar qué compró, cuándo puede usarlo y qué conserva.

La arquitectura de compras merece el mismo cuidado que los precios. El diseño actual describe confirmar o consumir antes de escribir el registro, y reconciliar consumibles a partir de compras y un perfil seleccionado por fecha. Esto necesita una transacción idempotente y un registro de concesiones y gastos: restaurar una compra no debe reponer algo ya gastado ni perder lo recién comprado. Son riesgos del diseño futuro, no fallos de una tienda ya operativa.

## Qué aprender de otros juegos

La [comparación de competencia](competencia-economia.md) separa juegos de ordenar líquidos, referentes adyacentes y afirmaciones de sus fichas oficiales. La función de la comparación es extraer criterios verificables: rapidez para empezar, claridad del siguiente objetivo, lectura del tablero, variedad de problemas, satisfacción al completar y valor visible de la colección.

No conocemos los algoritmos internos de dificultad, los ingresos por jugador ni la conversión de estos títulos. Sus capturas y descripciones no demuestran por sí solas que una función produzca retención. Antes del soft launch haría sesiones comparables de los primeros 20 niveles de varios referentes, registrando toques, tiempo de espera, anuncios, ofertas y recuperación del bloqueo. Esas partidas aún no se han realizado en esta revisión.

## Alcance de la siguiente versión de prueba

| Hito | Entrega | Condición para avanzar |
|---|---|---|
| Revisión de contratos | Documento breve con decisiones sobre tutorial, ayudas, datos de nivel y recuperación | No quedan contradicciones de `single`, solvencia y propiedad de recursos |
| Una orilla completa | Fondo, soportes, recipientes, líquidos, UI, audio y diez niveles revisados | Legible y agradable en teléfono; ninguna decoración invade interacción |
| Progreso con sentido | Treinta niveles, un capítulo compacto, cuaderno básico y guardado de partida | Los jugadores entienden qué hacen y por qué quieren jugar otro nivel |
| Economía de ensayo | Recursos y tienda de prueba, sin dinero real, con las tres familias y escenarios de nivel 31+ | No aparece un bloqueo de progreso ligado a falta de saldo o anuncios |
| Compras de prueba | Integración en entorno de pruebas y reconciliación | Concesión exactamente una vez, restauración correcta y recuperación de errores |
| Expansión | Pipeline y contenido adicional | Producción repetible, dificultad calibrada y experiencia validada |

El tramo de 30 niveles es un hito de validación de diversión propuesto; todavía no cambia los rangos y mundos del GDD. No valida vidas y rescates: ambos debutan desde el 31 en el diseño actual. La prueba económica necesita escenarios específicos del 31 en adelante y saldos preparados de prueba, además de la simulación; no exige producir cientos de niveles antes de probar esas situaciones. No movería Leticia al inicio ni inventaría un mundo nuevo para encajar la muestra. Primero se aprueba la segmentación editorial; después se actualizan los capítulos afectados de forma conjunta.

Dejaría para después las ligas, varios eventos simultáneos, pase, ofertas de rescate, variantes de muchas capacidades, mecánicas con temporizador, agua oculta y el catálogo completo de catorce tintes en un mismo tablero. No se eliminan del horizonte del proyecto: deben ganarse su lugar con evidencia de que mejoran el núcleo.

## Pruebas de producto y criterios de decisión

Haría dos rondas exploratorias de 8 a 12 personas del público objetivo, incluyendo principiantes, habituados al género y personas que necesiten mayor contraste. Es una muestra cualitativa para encontrar problemas, no para estimar retención o conversión del mercado. Probar también un Android de capacidad modesta; el Samsung de pruebas actual no representa toda la audiencia.

| Pregunta | Tarea y observación | Decisión que permite |
|---|---|---|
| ¿Se reconoce la Amazonía? | Mostrar la pantalla sin título y preguntar qué lugar y objetos reconoce | Ajustar identidad por reconocimiento, no por cantidad de hojas |
| ¿Se entiende una dosis? | Nivel 1 sin explicación verbal del moderador; registrar dudas y toques inválidos | Revisar onboarding |
| ¿Pensar es agradable o tedioso? | Jugar varios niveles y señalar cuándo la decisión ya estaba tomada | Reducir pasos repetitivos y esperas |
| ¿Se puede corregir un error? | Error voluntario, deshacer, reiniciar y reabrir app | Validar recuperación y guardado |
| ¿Se distingue el tablero? | Blanco frente a vacío, tintes próximos y recipientes llenos | Ajustar patrones y contornos |
| ¿Interesa volver? | Ofrecer otro nivel o explorar el cuaderno sin inducir la respuesta | Evaluar núcleo y recompensa |
| ¿La compra se entiende? | Mostrar una oferta de prueba y pedir que explique contenido y duración | Corregir valor y claridad antes de cobrar |

Metas de decisión propuestas para esa fase: cero pérdida de partida reproducible, cero nivel sin solución testigo, cero concesión duplicada en pruebas de compra y ningún obstáculo crítico de lectura. Para diversión y comprensión, documentar observaciones por persona y rediseñar si un problema se repite; no convertir una muestra pequeña en porcentajes de mercado.

En una beta mayor, registrar versión de nivel y reglas, inicio y fin de partida, intento, tiempo activo, vertidos, rechazos, deshacer, bloqueo, ayuda ofrecida y aceptada, regreso y cierre. Medir compras por cohorte y por exposición elegible, y evaluar incertidumbre. Los objetivos D1/D7/ARPDAU del GDD siguen siendo metas, no resultados demostrados ni garantía de rentabilidad.

## Entregables y estado

- [Auditoría de jugabilidad y algoritmos](gameplay-algoritmos.md).
- [Identidad amazónica y adaptación desde Creciente](identidad-amazonica.md).
- [Competencia, economía y compras](competencia-economia.md).
- [Concepto visual](visual/orillas-vivas-concepto.png), [prompt](visual/prompt-concepto.txt) y dos capturas actuales preservadas.

Verificación de la base: compilación .NET completada con cero errores y avisos; **235 de 235 pruebas .NET correctas**, sin casos omitidos. El informe de algoritmos conserva los comandos, el fallo inicial de comunicación con testhost y el reintento autorizado que pasó. No se ejecutaron pruebas Unity ni una nueva sesión en teléfono.

No se han modificado escenas, paquetes, reglas, niveles, SDK ni configuración de monetización. La siguiente implementación debe partir de los contratos revisados y de una orilla completa, con criterios de aceptación antes de ampliar contenido.
