# Tintalú: identidad amazónica y propuesta visual

Fecha: 2026-10-07. Documento de revisión y propuesta; no sustituye el GDD ni aprueba cambios de reglas, nombres, calendario, economía o alcance. Dirección indicada por el dueño durante esta revisión: **Amazonía ilustrada y cálida, con río, madera, frutas y fauna reconocible**.

## 1. Diagnóstico

La oportunidad no es añadir más hojas a los frascos. Es hacer visible una **vida junto al río**: un lugar concreto, objetos que pertenecen a ese lugar, agua que cambia y pequeños descubrimientos que dan sentido a seguir ordenando.

Se inspeccionaron `Logs/device/v01-fix-reposo.png` y `Logs/snapshots/02_board_l7.png`. Son capturas existentes, no una sesión nueva de juego. El prototipo permite reconocer las capas, sus patrones y los recipientes, pero la ambientación se lee como un bosque tropical esquemático: palmas de silueta repetida, relieves genéricos y una banda de agua sin orilla habitable. En el nivel 1 una gran zona superior queda vacía; en el 7 el tablero ocupa mejor el espacio, aunque la fila superior flota sin un soporte visual claro. El tono oliva uniforme transmite poca de la calidez, variedad material y vida que pide el dueño. Estas son observaciones visuales, no resultados de pruebas con usuarios.

El GDD ya contiene buenas bases: vidrio con cavidad recta, frutas con patrones, papel/madera/fibra, mundos situados, cuaderno y asesoría cultural. Lo que falta es convertir esa lista en una escena con jerarquía y una fantasía comprensible. La maqueta procedural todavía no materializa la acuarela de `docs/gdd/07-arte-tematica-audio.md` §1. No debe confundirse la limitación del prototipo con una validación o rechazo del arte final.

Puntos que conviene revisar antes de encargar producción:

| Hallazgo | Implicación | Propuesta |
|---|---|---|
| El paisaje y los recipientes se perciben como capas independientes | Se puede cambiar el fondo por cualquier bosque sin cambiar la identidad | Colocar el tablero sobre una superficie de trabajo junto a un muelle; río y orilla visibles a su alrededor |
| La regla de desaturación del fondo se aplica como una oscuridad casi global | Se pierde la calidez del entorno | Mantener el centro tranquilo y permitir luz, hojas y acentos más vivos en bordes y fondo lejano; cualquier cambio numérico del GDD se evalúa en imágenes reales |
| Los patrones son muy visibles en las capturas | Ayudan a distinguir, pero pueden dar apariencia de bloques impresos | Conservar señales redundantes y comparar dos intensidades; menisco, superficie y chorro deben comunicar líquido sin reducir lectura |
| Cinco tintes aparecen en el primer mundo, pero no hay una escena que enseñe su origen visual | Los nombres amazónicos pueden sentirse como etiquetas | Relacionar fruto, muestra de color y patrón en la tarjeta de descubrimiento y el cuaderno, sin poner frutas tapando cada dosis |
| El plan enumera 13 mundos, 14 tintes y muchas especies | Mucho contenido por producir antes de conocer el atractivo del conjunto | Validar una escena completa y varios tamaños de tablero antes de expandir el atlas |
| El documento usa nombres tradicionales para objetos híbridos de vidrio | Puede confundirse una invención funcional con una reproducción de artesanía | Explicar que el recipiente jugable es un diseño de ficción; mostrar las cuias reales y su contexto en la capa informativa |

Además, GDD 07 §1.2 exige que **cada** tinte difiera del fondo en al menos 25 puntos de L*. Con los valores publicados en §3 no existe un único fondo uniforme que cumpla para la paleta completa: los intervalos excluidos alrededor de los tintes cubren todo L*=0…100. Se comprobó también mediante barrido entero en PowerShell: cero candidatos. La salida de diseño no es oscurecer aún más toda la imagen. Conviene reemplazar esa condición por criterios que distingan texto, contorno del recipiente, dosis, menisco, patrón y señal de selección, y verificar cada combinación realmente utilizada. Esto es una propuesta de corrección del contrato visual, no un cambio aplicado al GDD.

## 2. Dirección principal: «Orillas vivas»

«Orillas vivas» es el nombre de trabajo de esta dirección artística, no una propuesta de cambio de marca. Fantasía: **ordenar colores del viaje y descubrir escenas de la vida fluvial**. El río, los objetos y el cuaderno conectan sesiones; la acción principal sigue siendo comparar, planear y verter.

Recomiendo ilustración cálida de formas claras, próxima al gouache, con detalle selectivo: madera gastada, reflejos suaves, fibras sencillas, hojas de silueta reconocible y frutas bien observadas. La luz entra por un borde; el tablero mantiene una base estable y tranquila. La sensación buscada es estar sentado junto al agua con objetos agradables de tocar.

Tres matices para comparar dentro de la dirección elegida:

| Variante | Qué se vería | Ventaja | Riesgo | Uso propuesto |
|---|---|---|---|---|
| **A. Muelle ilustrado — recomendada** | Mesa/bandeja junto al río, madera cálida, una canoa al fondo, vegetación y frutas en los márgenes; frascos claros en el centro | Une sensación táctil, lugar y lectura del puzzle; permite reutilizar piezas | Exceso de vetas o perspectiva puede interferir con las capas | Tablero principal y pantalla de victoria |
| B. Cuaderno de la orilla | Acuarela, papel, muestras botánicas y dibujos que se completan | Da personalidad a colección, descubrimientos y textos | Como tablero completo puede parecer una lámina estática y alejar el agua | Cuaderno, tarjetas y mapa; conservarlo como lenguaje secundario |
| C. Ventana al río | Paisaje algo más luminoso, fauna con formas amplias, tablero sobre una base simple | Reconocimiento inmediato y buena presencia en una captura pequeña | Puede acercarse al cartoon tropical genérico o saturar toda la pantalla | Comparación de paleta y escenario; tomar su claridad, sin llenar de mascotas el HUD |

Las tres variantes deben mostrar **el mismo estado del mismo nivel**, con igual tamaño de recipientes, textos, capas y controles. Comparar imágenes con niveles distintos confunde preferencia artística con facilidad del puzzle.

El concepto visual preparado en esta revisión conjunta, `visual/orillas-vivas-concepto.png`, sirve para discutir ambiente, materiales y composición. Es una imagen conceptual nueva, no una captura del runtime ni una especificación final: algunas marcas generadas recuerdan cítricos y deben sustituirse por frutas identificadas correctamente; forma, escala y capacidad de recipientes se ajustarán a la geometría y las pruebas de lectura del juego.

### La composición que debe resolver el artista

1. **Río reconocible:** una franja con profundidad, orilla y reflejos; no solamente una onda decorativa atravesando las bases de los frascos.
2. **Superficie de juego:** una bandeja o mesa frontal con dos apoyos muy sutiles si hay dos filas. Las marcas de dosis permanecen horizontales e iguales; la perspectiva vive alrededor de la cavidad, no dentro.
3. **Lugar:** un solo gesto geográfico fuerte por escena —perfil del mercado, curva de orilla, muelle—, acompañado por pocos objetos. El paisaje no necesita reunir todos los hitos del mundo en una única vista supuestamente real.
4. **Vida:** Capi en su orilla y una segunda presencia ambiental contextualizada. Miradas, vuelos y animaciones no señalan una solución ni invaden ranuras táctiles.
5. **Jerarquía:** líquido primero, controles después, lugar tercero. La calidez está en la luz y los materiales, no en teñir todos los elementos del mismo marrón.

## 3. Qué tomar de Creciente y cómo trasladarlo

Fuentes de referencia: `../game-amazonas-2d/research/tematica.md`, `planificacion/02_catalogo_de_contenido.md`, `planificacion/00_vision_superior.md` y `planificacion/06_biblia_de_diseno.md`. En la tabla, **A** significa investigación o propuesta anterior del proyecto de referencia; **P** es la nueva aplicación propuesta a Tintalú. Ninguna A equivale automáticamente a validación local para Pará.

| Elemento y fuente anterior | Aplicación al tablero/HUD | Aplicación a progresión y audio | Condición |
|---|---|---|---|
| Muelle, canoa, remo y barcazas. Catálogo §§3, 8; Biblia §§7, 13 | Bandeja de madera, amarre y remo fuera de la zona útil; icono de canoa ya existente | Viaje entre escenas; agua golpeando madera, remo y crujidos suaves | A→P. Seleccionar referencias de la región de cada mundo; no copiar una embarcación turística como si fuera universal |
| Regla/estaca del río, marcas de agua en pilotes. Biblia §§2.3, 14 | Inspiración para la barra de Crecida ya prevista; escala sencilla y explícitamente de progreso | Mostrar el mismo muelle en otro estado al alcanzar un hito | A→P. La barra representa una racha de juego, no metros ni una medición hidrológica real |
| Creciente y vaciante. Temática §1; Biblia §2 | Estados de fondo con centro igualmente legible | Una escena conocida revela otras orillas/reflejos; audio cambia de mezcla entre escenas | A→P. Nada sube dentro del tablero durante una decisión; el prototipo no simula estaciones ni usa calendario real |
| Aguaje/canangucha, açaí, camu camu, copoazú y arazá. Temática §2; Catálogo §5 | Fruto + muestra + patrón en descubrimientos; no introducir todos por decorar | Familia de fichas: planta, fruto, lugar y relación con el agua | A→P. Aguaje y canangucha no son dos especies. Especie, nombres y presencia local se validan por ficha |
| Victoria, hojas amplias, flores y palmas. Temática §2; Biblia §16 | Siluetas en los márgenes, con reservas espaciales para botellas elevadas | Pequeña escena de observación; variación diurna/nocturna solo cuando se documente | A→P. No usar una hoja como soporte realista para el jugador ni confundirla con el sello o un bloqueo del puzzle |
| Canasta y objetos de provisiones. Catálogo §§4–5; Biblia §11 | Canasta ya canónica para recompensas; madera/fibra como materiales | Recompensa con contenido visible y una ficha, en la misma pantalla | A→P. Sin nueva moneda, fabricación o inventario de alimentos en la prueba visual |
| Capibara, bufeo, perezoso, aves y mariposas. Temática §3; Biblia §8 | Un personaje principal y apariciones ambientales pequeñas; especies por región | Avistamientos y páginas del cuaderno; llamadas solo con licencia y ubicación revisadas | A→P. Fauna observable, no mascotas comprables/capturadas ni obstáculos que haya que maltratar |
| Casas, pasarelas y techos con color; referencias del Km 11. Biblia §16 | Aprender el uso de turquesa, madera y luz cálida en objetos contemporáneos | Base visual más habitable que una selva vacía | A→P. Las instalaciones de Gecko/Tacana no se trasladan a Pará; no copiar logos o edificios identificables |
| Cuaderno «En el juego / En la vida real». Visión §11; Biblia §15 | Una ilustración y una frase después del hito, nunca sobre una elección | Explicar la licencia del tinte y la realidad del río; consultar después | A→P. Reusar el patrón editorial, no las voces/personajes/compromisos de financiación de Creciente |
| Paisaje sonoro en capas. Biblia §17 | Vertido claro sobre ambiente discreto; todo estado útil tiene equivalente visual | Río + entorno + acento musical regional | A→P. No importar el carimbó a Leticia ni tomar una grabación de Leticia como paisaje de Pará |

La lección más transferible de Creciente es **conectar un objeto, un lugar y un fenómeno**, no su catálogo de controles. Cananguchal → palma → fruto → fauna → ficha es una familia coherente. Canoa + liana + bicicleta aérea + enemigos + tubing son varios juegos nuevos si se convierten aquí en mecánicas.

## 4. Reglas de lectura que deben sobrevivir al rediseño

- Mantener la cavidad recta, alturas iguales por dosis, marcas y labio funcional de GDD 07 §4. El borde cálido y el pie pueden cambiar; el contenido no se vuelve opaco para parecer cerámica auténtica.
- Probar color y patrón juntos. Un patrón bonito que desaparece en un recipiente de 34 dp de ancho no resuelve accesibilidad. No aumentar la densidad de ornamentación como sustituto del contraste.
- La selección eleva **solo el origen**. Ni fauna, ni brillo, ni línea de agua, ni encuadre deben indicar destinos válidos por accidente (D-002).
- La animación comunica **una dosis** por acción (D-001). El chorro se detiene al terminar esa dosis aunque debajo haya líquido del mismo color.
- Un control bloqueado debe seguir pareciendo un control y tener un estado distinguible; conservar etiquetas funcionales como «Deshacer». El nombre temático no puede ser la única instrucción.
- Con tres recipientes, centrar el conjunto dentro de un área de tablero adaptable; no intentar llenar todo el alto con tres objetos gigantes. Con siete o catorce, medir nuevamente etiquetas, espacio del dedo y elevación.
- Mantener decoraciones fuera de la máscara del líquido, del trayecto de vertido y de los controles. Una canoa no debe parecer un botón de vidas si solo es fondo.
- Las variantes tarde/noche deben conservar lectura, zonas táctiles y señales. «Muy difícil» no debe ser visualmente difícil de ver.

Medidas ya previstas por el GDD que sirven de base: controles ≥48 dp, texto con contraste ≥4,5:1, escala de texto hasta 130 %, patrones y reducir movimiento (`docs/gdd/07-arte-tematica-audio.md` §7.8). La elección estilística no valida automáticamente estos requisitos: se comprueban sobre el render final.

## 5. Cultura, naturaleza y precisiones pendientes

**La cuia de vidrio es una invención del juego.** Hoy se confirmó en IPHAN que los Modos de Fazer Cuias do Baixo Amazonas fueron inscritos en 2015 y que la práctica reúne saberes de artesanas ribereñas, preparación del fruto y aplicación de cumatê. Esto cierra una duda marcada [NV] en la investigación actual, pero no valida una botella transparente como reproducción tradicional ni concede autorización sobre diseños de artesanas. Conviene usar un nombre funcional en la explicación al jugador y distinguir el objeto real en el cuaderno. Un eventual cambio del nombre canónico requiere decisión del dueño. [IPHAN, registro de las cuias](https://bcr.iphan.gov.br/acoes-de-salvaguarda/registro-dos-modos-de-fazer-cuias-do-baixo-amazonas-como-patrimonio-cultural-do-brasil/).

**El agua oscura o sedimentaria no es agua sucia que haya que convertir en turquesa.** NASA explica el color del Negro por materia vegetal y el del Solimões por sedimentos; su confluencia está en Manaus. La inferencia de diseño es conservar colores del río y separar cualquier mecánica de capas ocultas de un relato de contaminación. Ordenar frascos no debe presentarse como un método real de purificación o restauración. [NASA, Meeting of the Waters](https://science.nasa.gov/earth/earth-observatory/meeting-of-the-waters-79111/).

Otras comprobaciones necesarias antes de arte/texto final:

| Tema | Estado de esta revisión | Acción |
|---|---|---|
| Especies y nombres de frutas | Heredados de los dos proyectos; no se revalidó todo el inventario | Ficha por especie con nombre local, parte representada, referencia y revisión; evitar afirmaciones nutricionales innecesarias |
| Color real de pulpa frente a color del tinte | El GDD ya estiliza por legibilidad | Decir «color inspirado en…» cuando corresponda; una muestra gráfica no debe atribuir a una fruta un jugo inexistente |
| Distribución local y conducta de fauna | Investigación anterior; no valida su presencia simultánea en cada fondo | Elegir dos presencias para el primer mundo y validarlas con referencias de la región |
| Geografía | El GDD agrupa hitos amplios por mundo | Una escena no es un collage presentado como vista real; usar varias viñetas o una composición declaradamente ilustrada |
| Casas y vida cotidiana | Creciente ofrece referencias específicas; GDD 07 §8.1 excluye personas | Dar vida con objetos y arquitectura pública sin personajes humanos nuevos; si se quieren habitantes, proponer explícitamente revisar ese criterio del GDD |
| Música | Géneros son referencias culturales, no una biblioteca intercambiable | Encargar un primer tema situado en Pará; registrar autoría, uso y licencia; no prometer una identidad panamazónica a partir de un solo instrumento |
| Saberes, patrones y lenguas | Pendientes de colaboración donde se usen | Evitar motivos étnicos genéricos y contenido ritual convertido en mercancía; revisión remunerada según GDD 07 §8 |
| Relación con conservación | Es una intención temática | No anunciar donaciones, rescates reales o beneficios ecológicos por una compra sin un programa concreto verificable |

Hay afirmaciones y etiquetas [V] heredadas que necesitan edición, no repetición automática. Por ejemplo, research/07 §3.1 contiene el literal contradictorio «Andino-amazónico (NO de Sudamérica)» para el lulo. Este documento no lo corrige en canon: lo señala como evidencia de que la fuente de apoyo necesita revisión antes de convertirse en texto público.

## 6. Una muestra completa y viable: Estuario, Pará

Recomiendo conservar **Estuario como primer mundo** en esta prueba. Es el orden actual del GDD; así podemos evaluar arte y sensación sin mezclar el rediseño visual con un cambio de geografía, frutas, desbloqueos y campaña. Lo aprendido en Leticia sirve como método y sensibilidad, no como un conjunto de edificios que colocar en Belém. La elección posterior de iniciar en la triple frontera sería otra decisión de producto.

Paquete propuesto para encargar y probar, no presupuesto aprobado:

| Entrega | Alcance concreto |
|---|---|
| Escena principal | Un muelle/orilla de Pará con fondo por planos, cielo, agua y superficie de tablero; un hito reconocible y pocos objetos |
| Variante ambiental | Una segunda luz/altura ambiental sobre la misma composición; se muestra entre escenas, sin afectar reglas del nivel |
| Recipiente | Uno terminado con estados vacío, seleccionado, vertiendo y sellado; mismo interior canónico |
| Líquidos | Los cinco tintes ya previstos para Estuario: Açaí, Aguaje, Taperebá, Guanábana y Bacaba; revisar su asociación ilustrada y nombres locales antes de ficha final |
| Vida ambiental | Capi y una presencia secundaria de la orilla validada; máximo una animación ambiental protagonista por momento |
| UI | HUD, bandeja, pausa, victoria y bloqueo con la misma familia de materiales; no producir aún todo el Mercado |
| Descubrimiento | Una tarjeta de fruta y una ficha con «En el juego / En la vida real»; maqueta del cuaderno sin adelantar automáticamente su versión canónica |
| Audio | Un ambiente, un tema discreto y familia de seleccionar/verter/rechazar/sellar/victoria; probar silencio y reducir movimiento |
| Prueba comercial visual | Una apariencia completa de ejemplo con vista previa y contenido exacto: borde/base, bandeja y tarjeta. Hipótesis de valor, sin precio ni demanda afirmados |

Usar los diez niveles existentes para la muestra jugable. Añadir composiciones de estrés **solo como prueba de layout** —dos filas de siete recipientes, colores próximos, capacidades distintas y texto grande—, sin declararlas nuevo contenido de campaña. Nada de nuevos SDK, permisos, sistemas de navegación, combate o fabricación para demostrar el estilo.

### Cómo saber si mejoró

Prueba propuesta, todavía no ejecutada: comparación de las capturas actuales con el mismo nivel rediseñado; orden alternado entre participantes. Incluir jugadores de puzzles y personas familiarizadas con la región, y una revisión específica de accesibilidad. Una muestra pequeña sirve para descubrir problemas; no prueba retención ni conversión comercial.

Registrar:

1. En cinco segundos: qué lugar sugieren las imágenes, qué se puede tocar y cuál parece ser el objetivo. Registrar palabras espontáneas antes de mencionar «Amazonía».
2. En el mismo tablero: lectura de capas, errores de selección, tiempo hasta el primer vertido y confusión entre vacío/blanco/patrón. El rediseño no debe aumentar errores de lectura.
3. Tras unos niveles: qué recuerdan —objeto, fruta, animal, río— y si comprenden una dosis por vertido sin ayuda de destinos.
4. Con el teléfono y texto grande: dedo sobre recipiente, etiquetas completas, pantalla bajo distintas luces, sonido apagado, patrones y reducir movimiento.
5. Ante la apariencia opcional: qué incluye, si perciben cambios de reglas y qué elegirían. La preferencia declarada no se contabiliza como compra.

Criterio de avance propuesto: conservar comprensión y legibilidad, obtener una asociación amazónica más específica que «jungla» y que el dueño prefiera la escena en juego, no solamente un arte promocional. Corregir primero lo que falla; producir el segundo mundo después.

## 7. Decisiones que deja preparadas esta revisión

- Dirección visual recomendada: **A, Muelle ilustrado**, con calidez de C y cuaderno de B.
- Unidad de producción inicial: un mundo terminado y probado; el plan de 13 mundos queda como horizonte, no como trabajo inmediato.
- Tema trasladado de Creciente: vida fluvial, objetos situados, estados del río y descubrimiento contextualizado.
- Núcleo conservado: ordenar líquidos, una dosis, ayudas explícitas, interior legible; cualquier mecánica temática nueva necesita diseño y evaluación independientes.
- Economía visual a explorar: objetos y apariencias deseables que no vendan legibilidad ni representación cultural sin contexto. No se afirma que esta opción convierta mejor que otras.
- Actualización canónica pendiente si se adopta: GDD 07 §§1, 2, 4, 7, 9 y 11; GDD 06 §§4.5–4.6 para cualquier cambio real de meta; glosario si cambia un nombre. Esta revisión no realizó esos cambios.

## 8. Alcance de verificación

Se leyeron CLAUDE.md, las secciones citadas de arte, meta e investigación temática, las decisiones D-001/D-002 y la documentación de Creciente. Se inspeccionaron dos PNG existentes y se abrieron fuentes primarias de IPHAN y NASA el 2026-10-07. El repositorio de referencia se consultó en lectura. Solo se añadió este documento.

No se jugó una compilación en esta subtarea, no se generó arte final, no se validaron especies in situ ni preferencias con usuarios y no se midió conversión. No se ejecutaron compilaciones ni pruebas de runtime: el cambio es exclusivamente una revisión de diseño. Las propuestas, cantidades de producción y criterios de prueba de este documento no son resultados medidos.
