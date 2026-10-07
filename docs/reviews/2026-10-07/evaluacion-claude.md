# Evaluación de Claude a la revisión del 2026-10-07

Fecha: 2026-10-07 · Autor: Claude (tech-lead) · Base: `main` en `ef676a4` · Material evaluado: `README.md`, `identidad-amazonica.md`, `gameplay-algoritmos.md`, `competencia-economia.md` y `visual/` de esta carpeta.

La revisión la hizo otra IA a petición del dueño. Es material de entrada, no canon: cada afirmación se comprobó contra el código, el GDD y las salidas de `tools/` antes de adoptarla. La decisión visual del dueño queda registrada como D-003 en `docs/dev/decisiones.md`.

## 1. Veredicto

- **El diagnóstico de composición es correcto y señala el punto exacto del código.**
  - `BoardLayout.cs:120` ancla el bloque abajo: en el nivel 1 quedan vacíos 342 dp, el 44 % del alto.
  - `GameScreen.cs:740-748` dibuja una sola banda de río de 60 dp bajo la última fila, así que la fila superior del nivel 7 flota.
- **La regla de 07 §1.2 (cada tinte a ≥ 25 puntos de L* del fondo) es imposible.** Barrido de L* de 0 a 100 en pasos de 0,1 con los 14 tintes: ningún fondo la cumple. Con los cinco del Estuario solo valdría L* ≤ 7, un fondo casi negro.
- **El dueño rechazó la dirección «Orillas vivas»** (gouache, papel crema, cuaderno): «no parece un juego sino algo como libro». De ella se conserva la composición, no el acabado.
- **Hay errores:**
  - El HUD del concepto (Deshacer, Pista y Reiniciar en la bandeja) contradice 07 §7.4.
  - Los frascos tienen la silueta de cilindro con labio que prohíbe 07 §1.3.
  - La lista de secciones del GDD que habría que editar se queda corta: ver las consecuencias de D-003.
- **La cita de IPHAN no la pueden comprobar los agentes**, que no navegan.

## 2. Se adopta ahora: fase visual «Selva brillante premium»

Se busca el acabado de los líderes del género con identidad amazónica propia y sin copiar su trade dress (D-003). Todo se hace de forma procedural o con scripts de editor, sin paquetes nuevos y sin el Bloom del 2D Renderer (UUM-150232).

| # | Cambio | Criterio de aceptación |
|---|---|---|
| V1 | Composición: bloque de cuias centrado en el área del tablero, ventana al río arriba y una repisa de madera tallada por fila, con sombra de contacto | Cuia ≤ 48 dp (02 §5). `BoardLayoutTests` actualizados (hoy fijan `FootY` = 590). `ShiftToClear` y el anclaje de la intro (`_river.rectTransform`) siguen funcionando |
| V2 | Fondo del Estuario: cielo cálido, horizonte fluvial plano, palmeras de açaí y planos de vegetación saturada | Sin montañas, personas ni aldeas identificables (07 §8.1). Detrás de las cuias, la repisa o un panel controlan el valor |
| V3 | Río con gradiente de profundidad, orilla y brillos animados | Conserva su función en Sumergir y teñir (07 §5.1). El agua base no compite con los tintes |
| V4 | Vidrio premium: borde especular, reflejos y brillo de superficie; cavidad interior oscura y casi opaca | Cavidad recta, marcas de dosis y menisco intactos (07 §4). Ningún brillo tapa una marca. El test EditMode de contraste compuesto (D-003) pasa en verde |
| V5 | UI con Baloo 2 y Nunito, y botones con volumen de madera, hoja o piedra | Posiciones de 07 §7.4 sin cambios. Deshabilitado = desaturado y sin relieve, con texto ≥ 4,5:1 (07 §7.8). Controles ≥ 48 dp |
| V6 | Feedback: partículas al sellar y en la victoria, celebración breve y botón Siguiente estable | ≤ 60 partículas (`AnimConfettiMaxParticles`). `PourChainTests` en verde: el encadenado se conserva y el modelo sigue actualizándose antes que la vista |
| V7 | Guardas | Nada cambia con la selección ni con la legalidad de un destino (D-002; `SelectionShowsNoTargetsTests`). Las decoraciones no son `raycastTarget` y quedan fuera de las ranuras y del chorro. Ninguna canoa junto al HUD. Las guardas de v0 (nivel > 10, ocultas y mecánicas) siguen intactas |
| V8 | Comparación | Capturas antes y después de los niveles 1 y 7 en el mismo estado (`VisualSnapshotTests`). Capturas de estrés, solo de layout: 14 cuias de 28-34 dp y dos filas de Cuia alta (07 §4.5) |

Se mantienen D-001, D-002, el Estuario con sus cinco tintes, los HEX y los patrones (15 %, 20 % y 35 %).

## 3. Fase de jugabilidad: backlog priorizado

Empieza cuando el dueño acepte las capturas de V8. Es la fase de jugabilidad, recursos y detalle de cada elemento.

| Prioridad | Ítem | Criterio de aceptación |
|---|---|---|
| 1 | T6: retirar las firmas heredadas sin modo (core-dev) | Ningún uso fuera de los tests; `dotnet test` y EditMode en verde |
| 2 | Guardar el tablero, el historial y el modo tras cada vertido (R8, 02 §1) | Al cerrar la app a mitad de nivel y reabrirla, vuelve el mismo estado; test PlayMode |
| 3 | Una instrucción por lección (≤ 2 líneas, 02 §13): el texto de P3 en el nivel 2, el tooltip de Aguas estancadas en el 6 y el dedo guía en el 1 y el 2 si se aprueba P5 | Visibles en los niveles 1, 2, 3 y 6. B1 sigue en verde |
| 4 | Tarjeta «Fruta nueva» (niveles 1, 2, 4 y 7; GDD 06) e icono de fruta al seleccionar (07 §3) | Aparece una vez por fruta, dura 2 s y no bloquea el toque |
| 5 | Audio de seleccionar, verter, rechazar, sellar y victoria, con volúmenes separados | Un sonido distinto por acción (02 §12). Vibración solo si el dueño aprueba el permiso `VIBRATE` |
| 6 | Medir toques y rechazos con `AF_STATE` en una sesión del dueño | Tabla por nivel con toques, rechazos, usos de Marea y tiempo. Sirve para responder P2 y P4 |
| 7 | Métricas informativas en el informe del generador: % de vertidos que conservan la solución, racha repetida máxima y par `single` frente a par `run` | El informe es determinista: mismo SHA-256 en dos ejecuciones. No cambian pesos ni umbrales (P6) |
| 8 | Capi como marcador procedural, fuera de las zonas táctiles | No es `raycastTarget` y no mira ninguna cuia. El arte final es del artista |
| 9 | Lote de documentación: Remolino en 03 §9 y los tres resultados del solver en 04 §2.3 | Aprobado por el dueño |

## 4. Se adopta más adelante

- **Patrones:** reevaluar su intensidad sobre el render final, con pruebas de accesibilidad.
- **Solver en el cliente:**
  - cancelación y límite de tiempo;
  - la discrepancia entre `hint_weight` como float (08 §9) y el peso entero de `SolverOptions`;
  - medir `Solve` en frío y crear `tools/solver-bench` antes de Morpho.
- **Ocultas:** verificador AND/OR sobre estados de conocimiento (la mecánica debuta en el nivel 100).
- **Bot:** más de 40 intentos cerca del umbral del 75 %.
- **Generador procedural en C#:** conservar la solución testigo; familias de patrones.
- **Economía:** recalcular el 73 % y las 15.510 semillas cuando se decida PR-3, con abandono e intentos medidos.
- **Comercio (Fase 2):**
  - escribir el libro mayor de forma idempotente antes del acknowledge/consume (08 §10.3);
  - tomar el saldo y los tokens del mismo blob (08 §11);
  - sin acknowledge local a las 72 h;
  - estados de IAP 5.4.4 comprobados por sdk-integrator;
  - pruebas de vinculación con Play Games.
- **DDA (1.2):** una frase que prohíba usar el historial de gasto.

## 5. Se descarta

- **La estética de papel, gouache y cuaderno de «Orillas vivas» y la variante B:** el dueño las rechazó. El Cuaderno de campo (1.1) se mantiene, con el acabado nuevo.
- **El HUD del concepto:** contradice 07 §7.4 y v0 no tiene pista.
- **Los frascos del concepto:** se queda la cuia-garrafa de `CuiaGeometry.cs`.

## 6. Decide el dueño

| # | Pregunta | Recomendación |
|---|---|---|
| P8 | ¿Se permiten degradados, relieve, sombras suaves y contorno de texto no dorado en botones de madera, piedra y hoja, manteniendo prohibidos el oro, las gemas, las cintas rojas, los gatos, el caldero, la calavera y la silueta de frasco con labio? | Sí. Bloquea V5 |
| P9 | ¿Se sustituye la regla de L* de 07 §1.2 por las cuatro comprobaciones compuestas de D-003, con sus umbrales [P]? | Sí. Bloquea el test de V4 |
| P10 | ¿Se reescribe 07 y se encarga el brief nuevo al artista (GDD.md §9, paso 11) después de aceptar las capturas de V8? | Sí: así no se encarga ni se edita dos veces |
| PR-1 | P2: ¿rampa monótona (opción B) o valles deliberados? | Rediseñar solo el nivel 2 (el salto de 3 a 9) y dejar como descansos las bajadas 6→7 y 8→9, con rangos [P] medidos |
| PR-2 | ¿La pista por inactividad (02 §3.4: desde el nivel 3, tras 12 s) sigue activada por defecto? | No. Apagada por defecto y activable en Ajustes, de acuerdo con «ayudas solo explícitas» |
| PR-3 | ¿Se puede usar la Cuia extra guardada en Aguas estancadas (1 por nivel y como máximo 16 cuias)? | Sí. En `econ_sim`, 5,77 de los 5,91 bloqueos sin recursos del perfil casual ocurren con stock. Hay que recalibrar 04 §6 y 05 §2.2 |
| PR-4 | ¿Se retira la palanca que oculta el estado de la Hoja escudo si baja la conversión (04 §8)? | Sí, por honestidad (05 §1, principio 6) |
| PR-5 | ¿Se condiciona la producción hacia los 700 niveles a validar antes un tramo de 30 (08 §6.2, 09 §1)? | Sí, como compuerta, sin cambiar todavía el objetivo |
| PR-6 | ¿Selección pegajosa (P4)? | Mantener C4 hasta tener los datos de la prioridad 6 |
| PR-7 | Nombre «cuia» y registro de IPHAN | Que lo revisen el asesor cultural y la sesión principal. No bloquea lo visual |

## 7. Verificación

Comprobaciones propias de esta evaluación:

- Barrido de L* con los valores de 07 §3 (Python): ningún candidato con la paleta completa; con el Estuario, solo L* de 0 a 7.
- Lectura de `BoardLayout.cs:120`, `GameScreen.cs:36, 356 y 715-747`, `ProceduralArt.cs:42`, `GameConfig.cs:73` y de los tests PlayMode que existen.
- Las demás cifras proceden de las evaluaciones verificadas del día, que citan su archivo y su línea.

Comandos ejecutados el 2026-10-07 sobre `ef676a4` más este documento y D-003:

- `dotnet build dotnet/AmazonFill.slnx -warnaserror`: 0 avisos y 0 errores.
- `dotnet test dotnet/AmazonFill.slnx`: 235/235.
- `python tools/validate_schema.py`: 23/23 PASS, sin cambios en `tools/out/`.

No se ejecutaron los scripts de Unity, porque esta tarea no tocó código ni archivos de Unity.
