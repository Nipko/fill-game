# Registro de decisiones del producto

Fecha de creación: 2026-10-06 · Autor: tech-lead · Parte de `main` en `8854993` (prototipo v0 jugable; árbol limpio).

Cada decisión del dueño que cambia reglas, UX o contratos del GDD se registra aquí, en formato ADR breve: fecha, estado, contexto, decisión, consecuencias, alternativas descartadas y cómo revertirla. El GDD sigue siendo la fuente de verdad (CLAUDE.md, "Fuentes de verdad"): este registro **no** sustituye la edición de los capítulos. Cada decisión lleva su tarea de documentación (sección 3, T5), y mientras esa tarea no se fusione, ante una contradicción entre el GDD y este registro manda la decisión fechada del dueño. Las entradas no se borran: si una decisión se revierte, se añade otra que la sustituye.

| ID | Fecha | Título | Estado |
|---|---|---|---|
| D-001 | 2026-10-06 | Una dosis por vertido | Sustituida por D-005 (2026-10-08). Antes: decidida por el dueño; implementada en el árbol de trabajo, sin commitear (T1-T3 y T5) y verificada en el SM-S938B el 2026-10-07 (T7: A7 y A8). Pendientes: commit y PR, T6 y las preguntas P2-P7 |
| D-002 | 2026-10-06 | Sin resaltado de destinos al seleccionar | Decidida por el dueño; implementada en el árbol de trabajo, sin commitear (T4 y T5) y verificada en el SM-S938B el 2026-10-07 (T7: B1-B2 y captura de B3). Pendientes: commit y PR y la revisión visual del dueño (B3) |
| D-003 | 2026-10-07 | Dirección visual «Selva brillante premium» | Decidida por el dueño; fase visual implementada con arte procedural provisional en `19177bd` y vista por el dueño en el SM-S938B el 2026-10-07 (de ahí D-004). Pendientes: P8-P10, los PNG de F0 y su integración (F6 de `docs/art/direccion-visual-v2.md`) y la edición del GDD. El punto 5, en lo que toca a la cavidad y el menisco, queda sustituido por D-006 (2026-10-08) |
| D-004 | 2026-10-07 | Panel azul petróleo, vertido a 38° y cuias más anchas con pocas cuias | Decidida por el dueño (respuestas a C16-C18 de `docs/art/direccion-visual-v2.md`); implementada en el árbol de trabajo, sin commitear (código, tests y documentación de «Archivos afectados»; EditMode y PlayMode en verde el 2026-10-07). Pendientes: commit y PR, revisión en el teléfono (`dev.apk`) y nueva medida de C19. Actualización del 2026-10-08: implementada en `b2d34de` (rama `d004-cuias-anchas`); la decisión 2 (38° fijos y labio a 0,6-1,6 u), el techo del vertido [P], la consecuencia «no cambia el número de filas» y el alto de la cuia (1:3, 6 u; pasa a 5,6 u) quedan sustituidos por D-006, que también matiza el recálculo a mitad de nivel: las filas se fijan al cargar y no se recalculan, solo el ancho; el resto sigue (panel azul petróleo y topes 64/56/48 dp) |
| D-005 | 2026-10-08 | Vertido por tramo, como Magic Sort | Decidida por el dueño; en implementación (C1, L1, U1, D1-D6) |
| D-006 | 2026-10-08 | Tablero, cuia y vertido como Magic Sort | Decidida por el dueño; en implementación (U2-U6, D1-D6). P12 (ranura de 42 dp en 16:9 con cuias añadidas) quedó decidida el 2026-10-19 en la (c) (D-030): 3 filas al cargar cuando T + 2 pasa de 14; en implementación: DC18 |
| D-007 | 2026-10-08 | Mecánicas del río: catálogo de 12, estrenos de la 1.0 y reglas finas | Decidida por el dueño (todas las recomendaciones de la propuesta de cultura ribereña, §(h), y las reglas finas Q-A, Q-B y Q-C). Implementada en el núcleo y el generador (pista A, integrada en `6faf7c3`) y en Unity (paso 1 del traspaso, hasta `ce0ced4`). Registrada y llevada al GDD el 2026-10-11 y el 2026-10-12 (paso 2), con la lista única de estrenos en 02 §9.1. Las preguntas 27-31 y 33-35 de `docs/GDD.md` §6.3 quedaron decididas el 2026-10-13 con su recomendación (D-020): Lluvia y Piraña en la 1.0, «Zafra» solo como nombre del hito 420, Revelar en la 3.ª ranura, el formato [P] de `pack_schema` 2, `level_overrides` como respaldo del kill-switch en la 1.0, la tabla de novedades de 201-1000 antes de generar 201-700, P1-P3 con la excepción del Remolino y solo la burbuja del revelado rítmico. Pendiente, sin bloquear la 1.0: la revisión del asesor cultural. C1-9 (02 R16 con Liana y capacidades mixtas; pregunta 24) quedó decidida el 2026-10-19 en la (b) para la 1.0 (D-028): el bloqueo es la regla de la 1.0 y la (a) se estudia para la 1.1. D-019 (2026-10-13) añade al catálogo una mecánica propia, la Tinaja (M13). El formato de las variantes de la 1.0 en `pack_schema` 3 (Hoja atada, Brote de dos hojas y Flor) se firmó el 2026-10-14 (D-023) |
| D-008 | 2026-10-08 | Bucle de partida como Magic Sort | Decidida por el dueño. Documentación aplicada el 2026-10-08 en `117f4be` (capítulos de «Archivos afectados»), dentro de la rama de código: **pendiente de la aprobación explícita del dueño antes de fusionar** o de un PR de documentación aparte (cambia el contrato 7). En implementación: pista B (B1-1-B1-6 y B2-1-B2-6). P14-P17 (los textos nuevos y «Sin canoas», Marea primera en el modal, la canoa al salir con 0 vertidos y cuándo sale «Canoas infinitas 1 h» en Sin canoas) se decidieron el 2026-10-19 con su recomendación (P17: A, siempre; D-029), y la 10.2 #20 de la propuesta de Magic Sort (punto 7), el mismo día, en la #20 C, misma semilla (D-030): sin pendientes del dueño. El número D-007 quedó reservado para las mecánicas especiales, en pausa; tiene su entrada desde el 2026-10-11 |
| D-009 | 2026-10-08 | Dificultad escalonada: ciclo de 20 reordenado, etiqueta antes de jugar y rotación de plantillas | Decidida por el dueño (Q1 A, Q2 sí, Q3 A y Q4 A de la misma propuesta). Implementada en el generador y en `tools/econ_sim.py` (pista A) y en Unity (paso 1). Registrada y llevada al GDD el 2026-10-11 (paso 2). Las cifras [sim] regeneradas con el ciclo nuevo (P-2) y la tabla de etiquetas de 03 §11 con la cuenta del ciclo quedaron aprobadas el 2026-10-19 (preguntas 25 y 26 de `docs/GDD.md` §6.3, (a); D-029) y están en el GDD. La fuente de la temporada del ciclo (pregunta 32: una tabla del cliente por ciclo) y la tabla de novedades de 201-1000 (pregunta 33: rehacerla antes de generar 201-700) quedaron decididas el 2026-10-13 (D-020); los nombres internos de las posiciones y de los hitos [P] esperan al asesor cultural. La pregunta 40 (la Cuia chica entre los 2 vacíos) quedó decidida el 2026-10-13 (D-015). D-013 (2026-10-13) cambia el contenido de la tarjeta previa del hito p20 (punto 2): añade «Si lo superas, llegas a {sitio}» |
| D-010 | 2026-10-08 | «Llenar y teñir»: el río llena las cuias y los miquitos las tiñen con fruta | Decidida por el dueño (idea y ampliación del 2026-10-08); guion `docs/research/intro-rio-miquitos/guion.md` aprobado para implementar. Implementada en la rama `intro-rio-miquitos` (`fbdf11e`, `e479cf0` y `8955ef3`), sin fusionar. Documentación aplicada el 2026-10-09 en la misma rama. Q11, Q14 y Q17 del guion y F7-F13 de la propuesta de fauna se decidieron el 2026-10-19 con su recomendación (D-029: Q11 en DC15 y Q14 en DC13; F7 y F11, cuando lo dé el asesor; F9, al backlog; F13, en la 1.1). Pendientes, sin bloquear: toda la fauna con el asesor cultural y el arte del artista. Los números D-007 y D-009 los usó la pista de mecánicas y niveles (rama `fase1-mecanicas-niveles`); tienen su entrada desde el 2026-10-11. **Ampliación del 2026-10-09** (elenco de unas 50 especies que tiñen, una nueva cada 20 niveles), registrada el 2026-10-11: pendiente del diseño del paso 3 del traspaso |
| D-011 | 2026-10-11 | Ajustes de la prueba en el teléfono: la victoria se cobra cuando se ve, avisos legibles, miquitos que se ven y Cosecha doble que avanza | Decidida por el dueño tras probar el `dev.apk` en el SM-S938B (tareas P1-P4 de `docs/dev/traspaso/pruebas-telefono-2026-10-11.md`). Implementada entera: los puntos 1, 3 y 4 en la tanda 1 (integrada en `7d755c4`), probada en el teléfono el 2026-10-12 con resultado «cumple» (`docs/dev/traspaso/pruebas-telefono-2026-10-12.md`); el punto 2 (avisos legibles) y la tarjeta del miquito tras la tropa en la tanda 2 (A2, `35301f9`; tanda integrada en `94905b8`), probada en el teléfono el 2026-10-13 con resultado «cumple» (`docs/dev/traspaso/pruebas-telefono-2026-10-13.md`). Llevada al GDD el 2026-10-12 (paso 2). Los [P] de la tanda 1 se ratificaron el 2026-10-13 (pregunta 38, D-020). Las preguntas 41 y 42, que dejó la tanda 2, quedaron decididas el 2026-10-13 (D-015); sus arreglos están en T52, integrada en `d2ce284` |
| D-012 | 2026-10-09 y 2026-10-11 | Prueba del 2026-10-09: estreno de la Piraña, banner de mundo, sacudida, patrones, guardado por vertido y marco del panel | Decidida por el dueño. Registrada y llevada al GDD el 2026-10-12 (paso 2). Implementada entera. En la tanda 1 (`7d755c4`), probados en el teléfono el 2026-10-12 con resultado «cumple»: el mordisco fallido visible, la marca de pareja del Remolino y el guardado por vertido con escritor en segundo plano (52 vertidos medidos: p95 de 2,15 ms y máximo de 3,67 ms). En la tanda 2 (`94905b8`), probados en el teléfono el 2026-10-13 con resultado «cumple»: la sacudida y los patrones e iconos (B3, `dd208a6`) y el banner de mundo y el tutorial de la Piraña con mordisco animado (A4, `94905b8`). El vertido que gana cuenta en «ninguno > 8 ms» y los [P] de la tanda 1 (entre ellos el tope de 1.000 ms de los guardados síncronos y el aire de la ficha del Remolino) quedaron decididos el 2026-10-13 (preguntas 37 y 38 de `docs/GDD.md` §6.3, D-020): en el teléfono, máximo de 4,83 ms en 216 vertidos. Las preguntas 39, 43 y 44 quedaron decididas el 2026-10-13 (D-015). D-013 (2026-10-13) cambia la decisión 2: el banner de mundo trae dentro el viaje de mundo (Q4-D). Los iconos de fruta procedurales son [P] del artista y del asesor cultural (07 §3) |
| D-013 | 2026-10-12 y 2026-10-13 | Mapa del río: remontar el Amazonas hasta donde nace | Decidida por el dueño (río arriba y un fondo por parada el 2026-10-12; la recomendación de cada pregunta de la sección 16 de la propuesta el 2026-10-13, con T1-A y T6-A de TiTino y Q1-A de Frutas del río). Documentación aplicada el 2026-10-13 (T50). **En implementación en la tanda 4** (estado del 2026-10-19, D3): los datos de las 70 paradas de 1-700 (M1.1, M4.5a y M4.5b) con la geometría de producción de Natural Earth y HydroRIVERS (M5.1 y M5.1c, adelantadas a la ola 7); el fondo por parada, la llegada en la victoria del `10k` y el rótulo del lugar (ola 2); la pantalla del Mapa con el río de vidrio, marcadores, «ponerse al día», tarjeta de la parada, Muestrario, miradores y «Norte arriba», y el viaje de mundo dentro del banner (olas 4 y 5); la secuencia del Mapa del 10 y las entradas desde la pausa, la victoria y el fin de contenido (ola 6); y las placas enteras de FX7, FX8 y FX10. En el cliente se juega hasta el 700 desde C1b (ola 7, en su rama). Faltan el arte final, la fuente del dato en la tarjeta (pregunta 57 de `docs/GDD.md` §6.3, decidida el 2026-10-19 en la (a): DC9 y DC10, D-028) y el arreglo del frente de agua tras «ponerse al día» (`FrontRibbonAt`). Las fuentes y las tolerancias de la geometría (pregunta 65) quedaron decididas el 2026-10-19 con las recomendadas (D-028): HydroRIVERS en las dos vistas, las excepciones medidas y `MundoZone` de 100 km. Cambia D-009 (punto 2) y D-012 (decisión 2); D-008 no cambia. Choques entre propuestas: pregunta 45 de `docs/GDD.md` §6.3, decidida el 2026-10-14 en la (a) (D-022). Todo nombre, dato y texto, pendiente del asesor cultural. Lo que dejaba abierto el punto 15 (T2, T4 y T5 de TiTino, Q2-Q7 de Frutas, Q1-Q12 de idiomas y Q-Ti1-Q-Ti7 de la tinaja) se decidió el 2026-10-13: D-016 a D-019. Las preguntas P-M1 a P-M8 de su plan de implementación se decidieron el 2026-10-14 con su recomendación (D-023) |
| D-014 | 2026-10-13 | Aguas estancadas por bucle (R13 ampliada) | Decidida por el dueño (opción recomendada de la hoja de pruebas del 2026-10-13). Documentación aplicada el 2026-10-13 (T50: 02 R13, R14 y R16). La pregunta 46 de `docs/GDD.md` §6.3 quedó decidida el 2026-10-13 en la opción (a), la recomendada (D-020): el cierre recorre todos los vertidos legales, fútiles incluidos, y así cumple R14 con capacidades mixtas. Implementada en el núcleo (T51, `DeadEndProbe` en `LevelSession`) e integrada en `1ad712c`, con el tablero del 66 como prueba de regresión. Resuelve para el jugador la pregunta 15 de 02; P1-P3 en lugar del anexo B se ratificaron el mismo día (pregunta 34, D-020) |
| D-015 | 2026-10-13 | Respuestas a las preguntas 39-44 de `docs/GDD.md` §6.3 | Decidida por el dueño (la recomendación de cada pregunta). Documentación aplicada el 2026-10-13 (T50) y completada el 2026-10-14 (T54: 02 §4 y 08 §5.1). Los arreglos de las preguntas 41 (punto 4), 42 y 43 (escala de fuente del banner) están en T52, integrada en `d2ce284`; sus [P] eran la pregunta 47 de `docs/GDD.md` §6.3, ratificada el 2026-10-14 (D-022). La medida del shader en un teléfono de clase A es una acción del dueño (pregunta 44) |
| D-016 | 2026-10-13 | TiTino: personaje principal | Decidida por el dueño (T1-A y T6-A con D-013; T2-A, T4-A y T5-B con su «Si» a las recomendadas; T3 resuelta: *Bradypus variegatus*). Documentación aplicada el 2026-10-14 (T54). **En implementación en la tanda 4** (estado del 2026-10-19, D3): el rig provisional (TI4) y las apariciones del plan (i) de `docs/research/personaje/propuesta-titino.md`: la victoria en sus hitos (TI6a; casi nunca cabía: preguntas 53 y 54, decididas el 2026-10-19 en la (a), D-028; con DC2, TiTino usa la franja de la victoria con vista), Nivel fallido y el aviso tras Volver a la orilla con `stats.levels_won` (TI6b), la ventanita del inicio (TI6c), la pantalla de arranque (TI6d), el tutorial de 1-3, 6 y 12 (TU1a y TU1b), la presentación del Mapa del 10 (M4.2; su bocadillo, fuera del tope de frases y dentro de «dos pantallas seguidas», pregunta 60, (b), D-028: DC5) y, desde FX8, sin las frases n.º 4 y 8 tras una Pista. Faltan su arte (del artista) y el visto bueno del asesor a sus frases. Nombre pendiente de la búsqueda de marca y del asesor cultural |
| D-017 | 2026-10-13 | Frutas del río | Decidida por el dueño (Q1-A con D-013; Q2-A a Q7-A con su «Si»). Documentación aplicada el 2026-10-14 (T54), con el contrato 31 (`af-facts/1`). **En implementación en la tanda 4** (estado del 2026-10-19, D3): el calendario y las 20 líneas por fruta de 1-260 (F1F2), las series de río de 261-300 (F1c y C1a), la línea de la victoria con su prioridad (F3, D-022), la ficha de solo lectura desde la victoria y desde Ajustes (F4) y la tarjeta «Fruta nueva» (F5). Faltan las líneas 17-20 del cacao y del arazá. Choques: pregunta 45 y pregunta 49 (claves y columnas de la tabla de textos) de `docs/GDD.md` §6.3, decididas el 2026-10-14 en la (a) (D-022): canales separados con una línea por victoria y prioridad, y dos familias de claves con una columna de estado por idioma. Todas las líneas, pendientes del asesor cultural |
| D-018 | 2026-10-13 | Idiomas y localización | Decidida por el dueño (Q1-A a Q12-A con su «Si»), con dos aprobaciones: el paquete Unity Localization 1.5.13 en la fase 3 del roadmap y la partida de traducción de la 1.0, de 1.300-3.000 €. Documentación aplicada el 2026-10-14 (T54), con el contrato 32 (tablas de textos). **En implementación en la tanda 4** (estado del 2026-10-19, D3), los niveles 1 y 2 de la sección 6 de `docs/research/idiomas/propuesta-idiomas.md`: la fachada `Loc` (L2a), `strings.csv` y `facts.csv` (L1 y M1.1t), la pseudo-localización y el inventario (L2b), `glossary.csv` (L2c), Ajustes con el idioma (L3), los desbordes en en-XA y al 130 % (L4) y la compuerta de release de los textos (B1). Faltan las traducciones y el asesor: en el build de B2 (2026-10-18), `LocReleaseGate` cuenta los problemas que pararían un release (detalle en `docs/dev/traspaso/README.md`, sección 1, «Tamaño»); y los niveles 3 y 4, con Unity Localization en la fase 3. Choques decididos el 2026-10-14 (D-022): la pregunta 49 de `docs/GDD.md` §6.3, en la (a), y la 51 (los datos de fruta y fauna con IA y revisión nativa, dentro de la partida aprobada), en la (b). Detalles de la implementación (idioma automático en es-419 y es-CO sin región [P], textos del Mapa en `facts.csv` y compuerta en el build): D-023 |
| D-019 | 2026-10-13 | Tinaja (M13): mecánica propia | Decidida por el dueño (Q-Ti1-A a Q-Ti6-A con su «Si»; Q-Ti7, solo si el asesor confirma el riesgo, y entonces A). Núcleo implementado en T55 (integrada en `451856e`). Documentación aplicada el 2026-10-14 (T54), con la ficha M13 en 02 §9.1. **Implementada en la tanda 4** (estado del 2026-10-19, D3): el esquema, el generador y level-qa (P1, P2a, P3 y FIXG, ola 1), el cliente (TJ1, `pack_schema` 3; TJ2, los overlays; TJ3, sacar, revelar y soltar; TJ4, la Luciérnaga, Morpho con la tinaja, la tarjeta del estreno y el consejo del 324 [P]) y el corte de Morpho por `hint_timeout_ms` (MO1). Choque: pregunta 50 de `docs/GDD.md` §6.3, decidida el 2026-10-14 en la (a) (D-022): sus Datos del río, como «Otro dato» de la parada k = 32. La máscara de la tinaja en `relief` y la Cuia de la casa fuera de la 1.0, decididas el 2026-10-14 (D-023). Todo nombre y texto, pendiente del asesor cultural. **Pregunta abierta (2026-10-19, C1c):** el 323 sale sin Tinaja (salida B, Q-Ti3) hasta que el dueño dé por buena la compuerta de PF1; el 323 real de la variante a se juega de punta a punta en un test |
| D-020 | 2026-10-13 | Respuestas a las preguntas 27-38 y 46 de `docs/GDD.md` §6.3 | Decidida por el dueño (la recomendación de cada pregunta). Documentación aplicada el 2026-10-14 (T54). La 46 ya está en el núcleo (T51, `1ad712c`). Tareas que dejaba, hechas en la tanda 4 (estado del 2026-10-19, D3): el cliente con `level_overrides` como respaldo del kill-switch (31: KS1, ola 2), la tabla de temporada del cliente (32: SE1 en el generador, SE2 con la LUT en los fondos y FX8 en las tarjetas), la tabla de novedades de 201-1000 antes de generar 201-700 (33: P4, con 301-700 generados en GEN79 por D-027) y los textos a 12 sp como mínimo (36: A1). La temporada falta verla en el teléfono |
| D-021 | 2026-10-14 | Miquitos por los lados en los tableros de 3 filas, alternando | Decidida por el dueño (idea propia, 2026-10-14). Frecuencia y restricciones [P] de este registro. Implementada en la rama (tanda 4, TL1), pendiente de la prueba en el teléfono. Las dos preguntas de su revisión se decidieron el 2026-10-19 (D-030): la velocidad del lanzamiento lateral, (a), excepción al tope de 250 dp/s hasta ~1.150 dp/s, de lado y por debajo del hombro (guion §4.4; 07 §5.1, fase d); y la barra de la Crecida, (c), que aparece al habilitarse el toque (requisito para cuando exista la barra) |
| D-022 | 2026-10-14 | Respuestas a las preguntas 45, 47-50 y 51 de `docs/GDD.md` §6.3 | Decidida por el dueño (la recomendación de la 45 y de las 47-50; la 51, «traducción con ia»: (b)). Documentación aplicada el 2026-10-14 (T58). Tareas que deja: la línea única de la victoria con su prioridad y la clave `ui_fruit_line_every_n_levels` [P] (45), la burbuja de Revelar sin aviso (48), la cabecera de las tablas de textos (49) y el «Otro dato» de la parada k = 32 (50) |
| D-023 | 2026-10-14 | Decisiones de detalle de la tanda 4: preguntas de sus planes y [P] del plan consolidado | Decidida por el dueño (la recomendación de cada pregunta, por «Sí, aplica las recomendadas también»): las P-M1 a P-M8 del plan del Mapa del río, las preguntas del plan consolidado de la tanda 4 (textos del Mapa en `facts.csv`, firma de las variantes de `pack_schema` 3, máscara de la tinaja en `relief`, Cuia de la casa fuera de la 1.0, idioma automático y región, alto de la llegada y tabla de D2) y los detalles [P] del plan. Documentación aplicada el 2026-10-14 (D1, que absorbe M1.7). En implementación: tanda 4. La tolerancia de la geometría del punto 20, con la de producción, quedó decidida el 2026-10-19 (pregunta 65, D-028) |
| D-024 | 2026-10-14 | Reglas de la Hoja atada: la capacidad de su llave y una por fruta | Decidida por el dueño (las recomendadas de la revisión de K3a, por «Sí, aplica las recomendadas también»). Documentación aplicada el 2026-10-14 (FIXD). En implementación: FIXK, en el núcleo; el generador y level-qa la heredan por `LevelValidation` |
| D-025 | 2026-10-14 | Un solo formato de cifras, también en los precios | Decidida por el dueño («unificar formatos», tras la prueba de la tanda 4). La regla (región del teléfono, Q8-A) es [P] de este registro. Implementada en FXT (puntos 1 y 2; el 3, lista la función para el adaptador de Unity IAP) |
| D-026 | 2026-10-16 | Audio provisional por código y permiso `VIBRATE` | Decidida por el dueño («lanza ya AU1 en paralelo a la ola 4, con los arreglos de niveles y graves, y apruebo el permiso VIBRATE para el háptico del sellado»). Implementada en AU1 (tanda 4, ola 4, carril de audio): `AF.Audio` y `AF.Audio.Unity`, los 61 efectos con su nivel, Ajustes de Sonido, Música y Vibración, los hápticos y el permiso en el manifiesto; AU2 (la misma ola) añadió el ambiente por mundo y la música adaptativa provisionales, también por código y detrás del ajuste Música [P]. Integrada en `bd649ac` y llevada a 07 §9 (FXD2, 2026-10-16). Pendiente: la prueba en el teléfono y los encargos al compositor y al diseñador de sonido (07 §9.5) |
| D-027 | 2026-10-18 | Mundos 7-9: las celdas, los K y los ajustes del generador para poder generarlos | Decidida por el dueño (todas las recomendaciones de la propuesta de los mundos 7-9: 25 celdas, K = 10 en los retos de 421-700, hitos de 14 cuias con K = 12 + 1 vacío, alivio hasta 13 cuias con la Tinaja y QA-PAR sin el par del rango en los estrenos de la Tinaja). Implementada en GEN79 (tanda 4, ola 7): especificación, generador, level-qa y los packs de 301-700 en sus dos variantes. Publicada en el cliente con la variante b en C1b (ola 7, en su rama; 122 niveles de curación en sus informes). Pendientes: la curación del dueño y el reflejo en 02 §5 y 03 §11, §12 y §17 (carril de documentación) |
| D-028 | 2026-10-19 | Respuestas a las preguntas 19, 22-24, 53, 54, 57, 60, 61 (con P5) y 65 de `docs/GDD.md` §6.3 | Decidida por el dueño (la recomendación de cada pregunta y, en la 65, de sus tres partes: «Acepto la opción recomendada […] y en las dos de M5.1»; el espacio del AAB y el permiso `INTERNET`, aparte). Documentación aplicada el 2026-10-19 (DC1). En implementación en la ola 7 de la tanda 4: DC2 (53 y 54), DC3 (19), DC4 (23), DC5 (60), DC6 (61 y P5), DC8 (65), DC9 y DC10 (57) y DC11 (24); la 22 ya estaba en el cliente |
| D-029 | 2026-10-19 | Respuestas a las preguntas con recomendación que seguían abiertas: 18, 21, 25, 26, 52, 55, 56, 58, 59 y 62-64 de `docs/GDD.md` §6.3, P7, P14-P17, el dedo guía de DC7, las partes 1, 3 (M5.1c) y 4 de la 66, Q11, Q14, Q17, F7-F13 y la propuesta de Magic Sort | Decidida por el dueño («resolvamos las preguntas, pero si tienen recomendadas vamos con esas»; para DC7, «sí»), después de D-028. Documentación aplicada el 2026-10-19 (DC1b): las cifras [sim] del ciclo de D-009 en el GDD, 03 §11 con la cuenta del ciclo, P17 A en 05, el agua negra #522500 y `af_fauna_sighted` en 08 §8.2. Código en la ola 7 de la tanda 4: DC6, DC7 y DC12-DC16. Fuera: el espacio del AAB, `INTERNET` y las preguntas sin recomendación (lista en la entrada), decididas el mismo día, después (D-030) |
| D-030 | 2026-10-19 | Respuestas a las 19 preguntas sin recomendación: la parte 2 de la 66 de `docs/GDD.md` §6.3, P12 de D-006, las dos de D-021, las #2, #9, #14 y #20 de la propuesta de Magic Sort y las observaciones de las pruebas y revisiones de las olas 2-6 | Decidida por el dueño («Acepto tus propuestas para las 19 preguntas sin recomendada: 1a, 2c, 3a, 4c, 5a, 6b, 7a, 8b, 9b, 10a, 11a, 12a, 13b, 14a, 15b, 16b, 17b, 18b y 19a»), después de D-029; las de Magic Sort, con la letra de la fuente: #2 A, #9 C, #14 C y #20 C. Documentación aplicada el 2026-10-19 (DC1c). Diez ya estaban aplicadas (la 11, en DC2); código en la ola 7 de la tanda 4: DC17-DC25. Fuera: el espacio del AAB y `INTERNET` |
| D-031 | 2026-10-19 | Reto del río: pool de 729 días, bandas, premio, anti-reloj y partida | Decidida por el dueño (R-Q1-R-Q10 con «Acepto tus propuestas»; R-Q1b, R-Q11-R-Q16 y R-Q12b con la recomendada; R-Q2b, 729 días; R-Q11b, 30 candidatos por fuente). Implementada por el equipo Spark (R0-R11, `spark/reto-del-rio`; el bloque (a) integrado en `fcbbd94`). Documentación aplicada con E11 (03, 05, 06 y 08). Pendientes: los enganches E1-E25, la integración y la prueba en el teléfono |
| D-032 | 2026-10-20 | Las 6 decisiones de 16:9 y del fin de contenido que dejó DC3 | Decidida por el dueño («Acepto tus recomendaciones para las 6 decisiones: compactar Aguas estancadas y la victoria en 16:9, aceptar 3, 4 y 5, y el botón «Jugar · Reto del río» (o «Jugar» sin Reto) tras el fin de contenido.»). Implementada en la ola 8: FX16a (Aguas estancadas compacto) y FX16b (victoria compacta y el botón del inicio). Documentación aplicada con FX16b (02 y 06). Pendiente: la prueba en el teléfono |

---

## D-001 · Una dosis por vertido

- **Fecha:** 2026-10-06.
- **Estado:** decidida por el dueño tras jugar el prototipo v0 en su teléfono (Samsung SM-S938B). Implementada y verificada en el teléfono el 2026-10-07, sin commitear (ver la tabla de arriba). **Sustituida por D-005 el 2026-10-08.**

**Contexto.** El núcleo aplica la regla canónica del género, que es también la del GDD: un vertido A→B mueve `cantidad(A, B) = min(racha visible de A, espacio de B)` (02 §1.2, R9). Si la racha no cabe entera, se vierte lo que cabe y el resto queda en A ("vertido parcial"). En el teléfono, el dueño pidió que al pasar a otro recipiente se mueva una sola dosis cada vez, aunque haya varias seguidas del mismo color (mecánica tipo *ball sort*).

**Decisión.**
1. Cada vertido mueve **una sola dosis**: la de la cima de A, aunque debajo haya más del mismo color. La legalidad del vertido no cambia.
2. Implementación indicada en el encargo del 2026-10-06:
   - parámetro del núcleo `PourMode { Single, Run }` con valor por defecto `Single`;
   - clave de Remote Config `rules_pour_mode` = `"single"` | `"run"`, con default `"single"`, para probar el otro modo sin tocar código.

   La clave nueva tiene que entrar en la tabla única de 08 §9 (pregunta P1).
3. El modo queda fijado al cargar cada nivel y no cambia a mitad de un nivel, porque el historial de Marea y el par dependen de él.

**Consecuencias.**
- Desaparece el vertido parcial en modo `Single`: siempre se mueve una dosis. El sellado (R5), la victoria (R12) y la legalidad no cambian.
- Hacen falta más vertidos y más toques por nivel. En el tutorial, el par sube entre ×1,0 (nivel 1) y ×1,7 (nivel 6): 8 de los 10 pares quedan fuera de los rangos [P] de 02 §13 (sección 2.1.4).
- El solver explora más estados: entre ×1 y ×34 de estados expandidos en el tutorial (sección 2.1.3).
- La lección del nivel 2 ("vertido parcial") deja de tener sentido y hay que sustituirla (sección 2.1.5).
- Marea deshace una dosis por uso. Además, `restart_confirm_min_pours` (8) se alcanza antes y `lluvia_k_pours`/`pirana_k_pours` pesan más. Todo esto exige recalibrar antes de los niveles 11+ (fuera de v0; pregunta P6).
- Tintalú se separa de la regla canónica del género (research/01 §2 [V]; 01 tabla de principios). Debe constar en 02 "Desviaciones de la dirección" y en GDD.md §2 (decisión 2).

**Alternativas descartadas.**
- **Mantener la racha** (`Run`): es la regla de los líderes del género, pero el dueño la descartó tras jugar.
- **`Single` sin conservar `Run`:** se perderían la prueba A/B por Remote Config y la paridad con `tools/levelgen_proto.py`, que vierte rachas.
- **Verter la racha con un gesto aparte** (mantener pulsado o arrastrar): añade un gesto, choca con C9/C10 de 02 §3.1 y contradice "de una en una".

**Cómo revertir.**
- **Antes de que exista `AF.RemoteConfig`:** poner `GameConfig.RulesPourMode = "run"`, regenerar el pack del tutorial con `--pour-mode run` (el par cambia) y revertir los textos de T5. Además, en la misma tarea: cambiar `PourModes.Default` a `PourMode.Run` (lo usan el default de `--pour-mode` y `TutorialPackTests`, que comprueba el par del pack en ese modo) o ajustar los tests que fijan `single` como modo por defecto (`LevelFlowTests.RulesPourModeDefault_IsSingle_AndEveryLevelLoadsInIt`, `GamePlayModeTests`, `PourChainTests`, `DevProbeTests` y `LaunchOptionsTests`). Si no, EditMode y PlayMode quedan en rojo.
- **Con Remote Config:** `rules_pour_mode = "run"` se aplica desde la siguiente carga de nivel. Ojo: los packs llevan el par calculado en `Single`, así que la reversión definitiva exige regenerarlos.
- **En todos los casos:** una decisión nueva en este registro que sustituya a D-001.

---

## D-002 · Sin resaltado de destinos al seleccionar

- **Fecha:** 2026-10-06.
- **Estado:** decidida por el dueño. Implementada y verificada en el teléfono el 2026-10-07, sin commitear (ver la tabla de arriba).

**Contexto.** Al seleccionar un origen, v0 dibuja un aro marfil al 40 % sobre cada destino legal. Viene de 07 §4.3 (fila "Destino válido (resaltado)") y del storyboard de 07 §5.2 (t = 0,00: "destinos legales con aro marfil"). El dueño pidió que no se muestre a dónde verter: elegir bien es parte del juego.

**Decisión.**
- Al seleccionar una cuia **no** cambia ninguna otra: no se resalta ni se sugiere ningún destino.
- La selección solo eleva el origen (16 dp, 02 §4.1).
- Las ayudas que señalan algo son explícitas y quedan fuera de v0:
  - la pista por inactividad (02 §3.4) solo señala un origen;
  - Morpho (04 §2.3) muestra origen y destino porque el jugador lo pide.

**Consecuencias.**
- Habrá más toques rechazados (sacudida, C5-C6) y más Marea. En v0 Marea es ilimitada, así que no afecta a la economía.
- El rechazo y el cambio de selección (C5) siguen informando, pero solo **después** de elegir.
- El lector de pantalla sigue anunciando "açaí seleccionado: elige destino" (02 §12), porque no sugiere ningún destino.
- El dedo guía de los niveles 1-2 (02 §13) muestra un destino. Es una ayuda puntual del tutorial y queda fuera de v0 (pregunta P5). *Actualización del 2026-10-19 (D-028):* P5 respondida, sí: el dedo guía y Morpho son ayudas explícitas compatibles con D-002; el dedo de 1-2 llega con DC6.

**Alternativas descartadas.**
- **Resaltar solo en el tutorial:** el dueño pidió quitarlo, sin excepción.
- **Interruptor de accesibilidad "Mostrar destinos":** exigiría una clave nueva en 08 §9 y no se ha pedido. No se inventa.
- **Resaltar solo tras N rechazos:** sería una pista encubierta, y para eso ya existen las ayudas explícitas.

**Cómo revertir.** Revertir el commit de T4 y restaurar las dos frases de 07 §4.3 y §5.2. No hay clave de Remote Config: reactivarlo es otra decisión.

---

## Impacto técnico

*Nota del 2026-10-08: las secciones 1-6 analizan D-001 y D-002. D-001 quedó sustituida por D-005: lo que aquí se dice de `single` como modo por defecto se conserva como historia y como descripción del modo de pruebas. Los estados de P1-P7 y R-1-R-6 están en las secciones 5 y 6.*

### 1. Estado de partida

- Verificado hoy en `main` (`8854993`):
  - `git status` vacío;
  - `dotnet build dotnet/AmazonFill.slnx -warnaserror`: 0 avisos y 0 errores;
  - `dotnet test dotnet/AmazonFill.slnx`: 143/143;
  - `python tools/validate_schema.py`: 23/23 PASS, sin cambios en `tools/out/schema_example_08_4_1.json`.
- Medición exploratoria:
  - script de Python fuera del repositorio que reimplementa 02 §1 sin ocultas ni mecánicas, con cap 4 y `U_c` = 4;
  - en modo racha reproduce **exactamente** `tools/out/tutorial_v0_report.md`: par, estados alcanzables, estados sin salida, camino a Aguas estancadas y estados expandidos por A* de los niveles 1-9;
  - sus cifras en modo `Single` son, por tanto, fiables como estimación, pero las tareas T1 y T2 deben reproducirlas con `AF.Core`/`AF.Solver` (criterio A5).

### 2.1 D-001: una dosis por vertido

#### 2.1.1 Reglas de 02 §1 que cambian

| Regla (02 §1) | Hoy (`Run`) | Con D-001 (`Single`, por defecto) | Texto a cambiar (T5) |
|---|---|---|---|
| `legal(A, B)` (§1.2) | Pseudocódigo de §1.2 | **Sin cambios.** No depende de la cantidad | — |
| `cantidad(A, B)` (§1.2) | `min(len(racha_visible(A)), espacio_efectivo(B))` | **1** si `legal(A, B)` | `cantidad(A, B) = 1 si rules_pour_mode == single; min(...) si run` |
| R6 racha visible | Fija la cantidad | Se conserva como definición: la usan `Run`, el bot (03 §6), Remolino en `Run` y la excepción de ocultas (04 §5.1). En `Single` no fija la cantidad | Nota en R6 |
| R9 vertido y parcial | Mueve la racha; si no cabe, vertido parcial; 1 movimiento | Mueve **una** dosis de la cima de A; **el vertido parcial no existe**; 1 movimiento | Reescribir R9 con los dos modos y citar D-001 |
| R7 / R10 (a) revelado | La cima oculta se revela cuando sale la racha | Igual: se revela cuando sale la **última** dosis de la racha visible, uno o más vertidos después | Sin cambio de texto; aclaración en R10 |
| R5 sellado, R12 victoria | — | **Sin cambios** | — |
| R13-R15 bloqueo y modal | — | **Sin cambios de código** (sección 2.1.2) | — |
| R16 garantía de fútiles | "El estado canónico no cambia con un movimiento fútil, salvo casa correcta" | Ya no es literal en `Single`: ver 2.1.2 | Reescribir R16 |
| Remolino M4 (§9.1) | `cantidad = min(racha, espacio(pareja))` | 1 | Nota en §9.1 |
| Barro M5 | El primer vertido lava el barro (`espacio_efectivo + 1`) | Igual, con una dosis | — |
| Lluvia M6 / Piraña M7 | Tic cada `k` vertidos | Con más vertidos por nivel el tic llega antes respecto al progreso: recalibrar `lluvia_k_pours` y `pirana_k_pours` (rango 2-5) antes de los niveles 143 y 183 | Nota en §9.1 (fuera de v0) |

#### 2.1.2 Fútiles y bloqueo en modo `Single`

**Hecho clave:**
- `legal(A, B)` y `futil(A, B)` no dependen de `cantidad`. Por tanto, para un mismo estado S, `movimientos_utiles(S)` (salvo el campo `Amount`) y `atascado(S)` son **idénticos** en los dos modos.
- `PourRules.IsLegal`, `IsFutile`, `HasUsefulMove` e `IsStuck` no necesitan el modo ni cambian.
- Lo que cambia es **qué estados se alcanzan** y qué significa un vertido fútil.

**Ejemplos** (cuias de abajo arriba; cap 4, `U_c` = 4):

| # | Estado y vertido | `Run` | `Single` | ¿Fútil? |
|---|---|---|---|---|
| E1 | A = [c] (única dosis, monocolor) → B vacía | Resultado idéntico al de `Single` | B = [c], A vacía: estado canónico igual | Sí, en ambos. R16 se cumple literalmente |
| E2 | A = [c, c, c] → B vacía | Mueve 3 dosis: estado canónico igual | Mueve 1: A = [c, c], B = [c]. El estado canónico **cambia** y la dispersión de c sube 1 (retroceso) | Sí, en ambos. Con capacidades iguales partir una monocolor nunca acerca a la victoria. R16 debe reformularse |
| E3 | Cuia chica (cap 2) = [c, c] → cuia estándar vacía | Mueve 2: "casa correcta" | Mueve 1: primer vertido de los dos que llevan c a su casa | No, en ambos (excepción `cap_A ≠ U_c ∧ cap_B = U_c`) |
| E4 | A = [c, c, c] ("sellable") → B = [x, c] | Mueve 2: A = [c], B = [x, c, c, c] | Mueve 1: A = [c, c], B = [x, c, c] | No: B no está vacía. Es un vertido **regresivo** pero útil a efectos de R13, en ambos modos |
| E5 | A = [x, c, c] → B vacía | Mueve 2 | Mueve 1: A = [x, c], B = [c]; hace falta otro para destapar x | No: A no es monocolor |
| E6 | A = [c, c] y B = [c] | A→B o B→A, juntando en un vertido | A→B y B→A se alternan (ciclos de longitud 2) | No. Los ciclos no son Aguas estancadas en ningún modo (R14; caso 3 de research/01 §7) |

**Decisiones de diseño técnico:**
- **No se amplía `futil`.** Declarar fútiles los vertidos regresivos (E4) exigiría el solver para decidir y rompería R14 ("nunca anunciar bloqueo mientras exista un movimiento útil"). R13 debe seguir siendo exacto y barato: O(T²) sin asignaciones.
- **R16 reformulada (propuesta para T5):** "Un vertido fútil nunca es necesario para ganar. En `run` deja el estado canónico igual (salvo la rama casa correcta). En `single` puede además partir una cuia monocolor en dos (E2), lo que con capacidades iguales solo aumenta la dispersión. Por eso R13 nunca declara bloqueo en una posición con progreso posible."
- **Con capacidades uniformes (`cap_i = U_c` para todo i y c, como en todo v0), la regla de fútiles no cambia el veredicto de R13 en ningún modo.** Prueba:
  1. Si existe un vertido fútil, existe una cuia vacía E.
  2. Si hay alguna cuia no vacía, no sellada y no monocolor, su vertido a E es útil.
  3. Si todas las cuias no vacías y no selladas son monocolor y dos comparten color, verter de una a otra es útil.
  4. Si no, cada color está en una sola cuia, que contiene sus `U_c` = `cap` dosis: está llena y monocolor, luego sellada, y el nivel está ganado.

  Por tanto, `futile_rule_enabled` solo importa con la Cuia chica (debut en el 14), la Cuia alta (27) y la poda del solver.
- **Poda del solver en `Single`:** podar los fútiles no cambia el par en los 10 niveles del tutorial (BFS con y sin poda: el mismo par en 10/10). Los estados sin salida (0, 0, 0, 0, 0, 1, 0, 3, 4, 0) y los caminos más cortos a Aguas estancadas (nivel 6: 1 vertido; nivel 8: 2) coinciden con `Run`. T1 lo convierte en un test de propiedad sobre tableros aleatorios.

#### 2.1.3 Solver

- **Igual que hoy:**
  - representación y codificación (`Cuia.Packed`, `BoardCanon`, hash FNV-1a de 64 bits);
  - los pares (A, B) legales de cada estado, así que el **factor de ramificación por nodo es el mismo**.
- **Lo que cambia es la transición:** la cantidad pasa a ser 1.
- **Corrección a "mismo espacio de estados":** el universo de configuraciones es el mismo, pero el **conjunto alcanzable crece**:
  - todo vertido `Run` de k dosis equivale a k vertidos `Single` legales seguidos;
  - por tanto, alcanzables(`Run`) ⊆ alcanzables(`Single`) y "resoluble en `Run`" implica "resoluble en `Single`" (los 10 niveles del tutorial siguen siendo resolubles);
  - `Single` alcanza además estados con una racha partida entre dos cuias (E2, E5);
  - la inversa no está garantizada.
- **Heurística actual** `H = max(h1, h2)` (03 §5, `SolverHeuristic`):
  - sigue siendo **admisible y consistente** en `Single`, porque un vertido de una dosis elimina como mucho un corte y saca un color de como mucho una cuia;
  - A* con `w = 1` sigue dando el par exacto **sin cambiar el código**;
  - pero informa menos: una racha de 3 cuesta 3 vertidos y `h1` la cuenta como 1.
- **Heurística propuesta para `Single` [P]:** `H_single = max(Σ_i (n_i − fondo_i), h2)`, donde `fondo_i` es la longitud de la racha del fondo de la cuia i.
  - Es admisible: toda dosis por encima de la racha del fondo tiene que moverse al menos una vez, y cada vertido `Single` mueve exactamente una.
  - Es consistente: cada vertido cambia la suma en −1, 0 o +1.
  - Se comprobó en todos los estados alcanzables de los 10 niveles: `H_single ≤ distancia` en el 100 %.
- **Profundidad y coste** (A* `w = 1` con poda de fútiles; los estados expandidos en `Run` coinciden con el informe actual en los niveles 1-9):

| Nivel | Par `Run` | Par `Single` | Estados alcanzables `Run` → `Single` | Expandidos `Run` | Expandidos `Single` con H | Expandidos `Single` con `H_single` |
|---|---|---|---|---|---|---|
| 1 | 3 | 3 | 6 → 8 | 5 | 5 | 5 |
| 2 | 6 | 9 | 42 → 114 | 25 | 75 | 74 |
| 3 | 7 | 10 | 109 → 170 | 37 | 108 | 101 |
| 4 | 8 | 12 | 179 → 375 | 52 | 251 | 222 |
| 5 | 10 | 16 | 481 → 805 | 157 | 744 | 692 |
| 6 | 10 | 17 | 36 → 72 | 25 | 55 | 40 |
| 7 | 11 | 15 | 563 → 922 | 19 | 641 | 477 |
| 8 | 13 | 17 | 742 → 1.478 | 319 | 1.056 | 947 |
| 9 | 14 | 16 | 1.361 → 1.961 | 435 | 937 | 462 |
| 10 | 16 | 19 | 2.479 → 3.634 | 78 | 2.488 | 1.176 |

- **Presupuestos:** en el tutorial no hay riesgo (≤ 2.488 expandidos frente a 300.000 del pipeline y 40.000 de `hint_budget_nodes`). En campaña (K de 8 a 14) hay que medir con `tools/solver-bench` (aún no existe) antes de 1.0 el impacto en Morpho (<50 ms), Pororoca y el pipeline (riesgo R-1).
- **Pipeline de 1.0** (fuera de v0; todavía no portado a C#; pregunta P6):
  - 03 §4.1-§4.2 (`MOVIMIENTOS_INVERSOS`): en `Single` m = 1, y se omite la condición "b no vacía y top(b) == c: continuar", porque el vertido directo mueve siempre exactamente 1. A cambio, el origen del vertido directo debe quedar con c arriba o vacío (r ≥ 2 o len(a) = 1) y b no puede quedar sellada.
  - 03 §5 `MOVIMIENTOS`: "cantidad 1".
  - 03 §6 bot: `puntos = n` y la penalización "vertido parcial" pierden sentido. Hay que redefinir la puntuación y recalibrar los umbrales de etiquetas (Difícil ≥ 75 %, Muy difícil ≥ 90 %).
  - 03 §4.3 y QA #2 (`par ≥ 2K`) y el índice D (`par − 2K`): las cifras [V-proto] de 03 §18 se midieron en `Run`.

#### 2.1.4 Par de cada nivel del tutorial

El par se recalcula en `Single` con el mismo solver y el mismo BFS. Comparación con los rangos [P] de 02 §13 y con los toques de la solución óptima (2 por vertido):

| Nivel | Par `Run` (pack actual) | Par `Single` | Rango [P] de 02 §13 | ¿Dentro? | Toques `Run` → `Single` |
|---|---|---|---|---|---|
| 1 | 3 | 3 | 3-3 | Sí | 6 → 6 |
| 2 | 6 | 9 | 4-6 | No | 12 → 18 |
| 3 | 7 | 10 | 5-7 | No | 14 → 20 |
| 4 | 8 | 12 | 7-9 | No | 16 → 24 |
| 5 | 10 | 16 | 9-11 | No | 20 → 32 |
| 6 | 10 | 17 | 10-13 | No | 20 → 34 |
| 7 | 11 | 15 | 10-13 | No | 22 → 30 |
| 8 | 13 | 17 | 12-15 | No | 26 → 34 |
| 9 | 14 | 16 | 13-16 | Sí | 28 → 32 |
| 10 | 16 | 19 | 14-18 | No | 32 → 38 |

Lecturas:
- La comprobación "par dentro del rango [P] de 02 §13" del generador fallaría en 8 de los 10 niveles.
- La rampa deja de ser monótona: 6 (17) > 7 (15) y 8 (17) > 9 (16).
- Los tiempos objetivo [P] de 02 §13 (30-120 s) suben con los toques.
- Hay que elegir entre dos caminos (pregunta P2): adoptar como rango los pares medidos o rediseñar los niveles 2-10 para una rampa monótona.

#### 2.1.5 Lecciones del tutorial (02 §13)

| Nivel | Lección actual | Con D-001 | Sustitución propuesta [P] |
|---|---|---|---|
| 1 | Estado literal; "termina la cuia casi llena antes de apartar"; dedo guía | Sigue valiendo (par 3 en ambos modos) | — |
| 2 | **"Vertido parcial con dedo guía: una racha de 3 sobre un hueco de 2"**; dos vacíos ("reserva uno") | **Deja de tener sentido:** no hay parcial | Lección "una dosis por vertido": la solución óptima en `Single` del tablero actual ya contiene **3 vertidos seguidos 2→4** (la racha de 3 açaí pasada dosis a dosis). Texto ≤ 2 líneas [P]: "Cada vertido pasa una sola dosis: repite para mover las demás." (pregunta P3). Se mantiene "reserva uno" |
| 3 | Marea la primera vez que un vertido aleja de la solución (distancia del solver +2) | Sigue valiendo. Vertidos útiles iniciales que no acercan: 2 de 6 en `Single` (4 de 6 en `Run`); la comprobación (≥ 1) pasa | — (fuera de v0: tooltips) |
| 5 | Color repartido en 3 cuias | Propiedad del tablero: no cambia | — |
| 6 | Un vacío; Aguas estancadas alcanzables | Estancadas en 1 vertido en ambos modos | — |
| 8 | Reinicio probable con 2 vacíos | Estancadas en 2 vertidos en ambos modos | — |
| 4, 7, 9, 10 | Debuts y rampa | Solo el par (2.1.4) | — |

En el generador (T2):
- la comprobación "nivel 2: vertido parcial de una racha de 3 sobre un hueco de 2" se aplica solo en `Run`;
- en `Single` la sustituye "nivel 2: la solución óptima contiene ≥ 3 vertidos seguidos con el mismo origen y destino".

En Unity (T3), `TutorialPackTests.Level2_OffersThePartialPourOfThreeOntoTwo` se sustituye por la comprobación equivalente.

#### 2.1.6 Encolado de toques y animación

**Modelo** (02 §3.2):
- no cambia: el toque se evalúa contra el modelo y `TryPour` aplica R9-R10 al instante;
- `queue_max_lag_pours` sigue en 2;
- `TapController` no cambia de lógica.

**C4** dice hoy: "si quedan dosis del mismo color en X tras un parcial, X no sigue seleccionada". En `Single` ese caso se vuelve el habitual. Se mantiene el comportamiento (el origen se deselecciona) y solo se reescribe el texto: "tras el vertido X no sigue seleccionada, aunque le queden dosis del mismo color". La alternativa de la selección pegajosa queda como pregunta P4.

**Cola visual:**
- La solución óptima en `Single` encadena vertidos idénticos: 7 en el nivel 6 y 3 en el 2.
- Un vertido de 1 dosis dura unos 660 ms: 120 de elevación + 200 de traslado + 180 de chorro + 160 de retorno.
- Un jugador rápido (un toque cada 300 ms) encola el tercer A→B cuando A tiene ya 2 trabajos pendientes, y `EnforceLag` hace saltar el más antiguo a su fotograma final (salto visible).

**Propuesta [P] para `BoardAnimator`, el "encadenado":**
- Si llega un vertido con el mismo (A, B) que el que está en curso y la cuia origen aún no ha empezado el retorno, se alarga el chorro `anim_pour_dose_ms` (100 ms) en lugar de encolar retorno, elevación y traslado.
- Tres dosis costarían entonces 120 + 200 + 3·100 + 80 + 160 = 860 ms, lo mismo que hoy un vertido `Run` de 3.
- No cambia claves ni defaults.

**Chorro:** siempre 1 dosis, 180 ms (`anim_pour_dose_ms` + `anim_pour_tail_ms`).

**Marea (04 §2.1):**
- deshace una dosis por uso;
- es ilimitada en los niveles 1-10, así que no afecta a v0;
- desde el 11 "3 por nivel" equivale a 3 dosis: recalibrar antes (pregunta P6);
- `restart_confirm_min_pours` (8; fallo blando, 04 §5.2) también.

**`DevProbe`:**
- en `AF_STATE` el tercer elemento de `move` vale siempre 1 en `Single`;
- `pours` cuenta dosis movidas;
- se propone añadir `"pour_mode":"single"` al evento `load`.

**Guardado a mitad de nivel** (R8; fuera de v0): el guardado debe incluir el modo; un cambio de Remote Config solo se aplica en la siguiente carga de nivel.

#### 2.1.7 Configuración y formato

- **08 §9:**
  - nueva fila `rules_pour_mode` | string | `"single"` | `single` · `run` | 02 | "Cantidad por vertido (D-001): una dosis o la racha visible (R9)";
  - la regla de nombres añade la familia `rules_` (variantes de reglas del núcleo, dueño 02);
  - `futile_rule_enabled` conserva su nombre.
- **v0 no tiene `AF.RemoteConfig`** (sdk-integrator; Firebase pendiente):
  - `GameConfig.RulesPourMode = "single"`, con un parser que convierte un valor desconocido en `single` con aviso en el log;
  - para probar `run` sin tocar código antes de que exista Remote Config, se propone [P] el extra de intent de desarrollo `af_rules_pour_mode` en `LaunchOptions`, solo en builds de desarrollo, igual que `af_level`.
- **Pack del tutorial:**
  - el par depende del modo;
  - en v0 **no se cambia el formato** (el subconjunto de `af-level/1` no tiene ese campo);
  - el generador recibe `--pour-mode single|run` (default `single`) y el informe declara el modo y lista también el par `Run` como referencia;
  - para 1.0, que el pack declare su modo es un cambio de contrato (`pack_schema` 2): pregunta P7.

### 2.2 D-002: sin resaltado de destinos

**Qué lo resalta hoy** (búsqueda en `Assets/_Project/Scripts`):

| Archivo | Líneas | Qué es | Acción |
|---|---|---|---|
| `Scripts/UI/GameScreen.cs` | 575-584 | `HighlightTargets(LevelFlow, int source)`: recorre las cuias, consulta `PourRules.IsLegal(state, source, i)` y llama a `SetTargetHighlight` | **Quitar el método** |
| `Scripts/UI/GameScreen.cs` | 350, 356, 367, 372, 419, 428 | Llamadas a `HighlightTargets` en `OnTap` (Selected, Deselected, Rejected, Poured), `OnWin` y `OnStuck` | **Quitar las 6 llamadas** |
| `Scripts/UI/GameScreen.cs` | 571 | `_views[i].SetTargetHighlight(false)` en `ClearLifts` | **Quitar la línea** |
| `Scripts/Presentation/CuiaView.cs` | 50 | Campo `_targetRing` | **Quitar** |
| `Scripts/Presentation/CuiaView.cs` | 512-514 | Creación de la imagen `TargetRing` en la construcción de la vista | **Quitar** |
| `Scripts/Presentation/CuiaView.cs` | 158-159 | Posición y tamaño de `_targetRing` en el layout | **Quitar** |
| `Scripts/Presentation/CuiaView.cs` | 258-265 | `SetTargetHighlight(bool)` (comentario "07 §4.3") | **Quitar** |

**Se conserva:**
- `RingColor` y `ProceduralArt.Ring`, que usa el aro de sellado `SealRing` (`_glow`, 07 §5.3, y la celebración, 07 §5.4);
- la elevación del origen en el *touch-down* (`OnCuiaDown`, que solo consulta `CanBeSource`: C1-C2);
- la sacudida de rechazo.

`DevProbe` (`AF_LAYOUT` y `AF_STATE`) no expone resaltados: no cambia.

**Texto del GDD:**
- 02 §3-§4 **no** menciona hoy el resaltado de destinos: el resaltado vive en 07. Para dejar la decisión explícita en el capítulo de reglas, T5 añade:
  - 02 §3.1, C1: "X se selecciona y se eleva; ninguna otra cuia cambia: no se resaltan ni se sugieren destinos (D-002)";
  - 02 §4.1, fila Selección: "…; el resto del tablero no cambia (sin resaltado de destinos)".
- 02 §3.4 ya dice que la pista "no muestra el destino" y 02 §12 ("elige destino") no sugiere ninguno: no cambian.
- 07 §4.3: se borra la fila "Destino válido (resaltado)".
- 07 §5.2, fila t = 0,00: se borra "; destinos legales con aro marfil".

---

## 3. Tareas por agente

**Orden y paralelismo:**
- Sin dependencias, en paralelo: T1 (C# puro, Editor solo para verificar), T4 (Editor) y T5 (documentación).
- Cuando T1 esté fusionada:
  - T2 (sin Editor);
  - T3 (Editor; empieza al terminar T4, para no superar 2 Editores).
- **T2 y T3 se fusionan en lote, una detrás de otra, con EditMode tras las dos.** Por separado, `main` queda en rojo: el pack nuevo trae el par `Single` y los tests de Unity resuelven con el modo que fija T3.
- T6 y T7 van al final.
- Ninguna tarea comparte archivos con otra que corra a la vez. T6 retoca archivos de T1, pero solo después de fusionar T1-T4.
- Cada tarea pasa por code-reviewer.

| # | Agente | Archivos (y solo esos) | Depende de | Qué hace |
|---|---|---|---|---|
| T1 | core-dev | `Packages/com.amazonfill.core/Runtime/AF.Core/Rules/{PourMode.cs (nuevo), CuiaRules.cs, PourRules.cs}`, `Runtime/AF.Core/Session/LevelSession.cs`, `Runtime/AF.Solver/Search/{SolverOptions.cs, AStarSolver.cs, SolverHeuristic.cs}`, `Tests/Editor/AF.Core.Tests/PourModeTests.cs` (nuevo), `Tests/Editor/AF.Solver.Tests/AStarSolverPourModeTests.cs` (nuevo), `dotnet/AF.Core.Tests/DotnetOnly/{PourModePropertyTests.cs (nuevo), HotLoopAllocationTests.cs}`, `README.md`, `CHANGELOG.md`, y los `.meta` que genere Unity | — | Ver "API de T1" |
| T2 | level-pipeline-dev | `tools/level-generator/{Program.cs, Handmade/*.cs, specs/tutorial_v0.json}` y, generados, `Assets/_Project/Data/Levels/tutorial_v0.json` y `tools/out/tutorial_v0_report.md` | T1; P2 y P3 | `--pour-mode single\|run` (default `PourModes.Default`); `StateGraph` y A* con el modo; informe con el modo, par `Single` y par `Run`; comprobación del nivel 2 según el modo (2.1.5); rangos de par según P2 (si se rediseña, nuevos tableros 2-10 y rangos propuestos como [P] en el PR) |
| T3 | unity-gameplay-dev | `Scripts/GameFlow/{GameConfig.cs, LevelFlow.cs, TapController.cs}` (en TapController solo el comentario de C4), `Scripts/UI/{LaunchOptions.cs, DevProbe.cs}`, `Scripts/Presentation/BoardAnimator.cs`, `Tests/EditMode/{TutorialPackTests.cs, LevelFlowTests.cs, TapControllerTests.cs, TestPacks.cs}`, `Tests/PlayMode/{GamePlayModeTests.cs, VisualSnapshotTests.cs}`, `Tests/PlayMode/PourChainTests.cs` (nuevo) | T1 (en lote con T2) | `GameConfig.RulesPourMode = "single"` y su parser; `LevelSession(..., mode)`; extra de intent `af_rules_pour_mode` [P]; `"pour_mode"` en `AF_STATE load`; encadenado de vertidos iguales [P]; tests con `SolverOptions.ForPipeline(mode de la sesión)`; sustituir el test del parcial del nivel 2 |
| T4 | unity-gameplay-dev | `Scripts/UI/GameScreen.cs`, `Scripts/Presentation/CuiaView.cs`, `Tests/PlayMode/SelectionShowsNoTargetsTests.cs` (nuevo) | — | Quitar lo de la tabla 2.2 y test PlayMode |
| T5 | Documentación (sesión principal, con aprobación del dueño por tocar el GDD y un contrato) | `docs/gdd/02-nucleo-reglas-ux.md` (§1.2, R6, R9, R10, R16, §3.1 C1 y C4, §4.1, §9.1 notas, §13 fila 2 y rangos, Parámetros, Desviaciones), `docs/gdd/07-arte-tematica-audio.md` (§4.3, §5.2), `docs/gdd/08-arquitectura-tecnica-publicacion.md` (§9: fila y regla de nombres), `docs/gdd/03-niveles-generacion-dificultad.md` (notas en §4.1, §5 y §6: "en `single`…", sin recalibrar), `docs/gdd/01-vision-posicionamiento.md` (principio 1), `docs/GDD.md` (§1 "Qué es", §2 decisiones 2 y 3), `docs/dev/prototipo-v0.md` (b y e), `docs/dev/decisiones.md` (estado) | P1-P5 respondidas | Llevar D-001 y D-002 al canon |
| T6 | core-dev | Los archivos de `Runtime/` de T1 | T1-T4 fusionadas | Retirar las sobrecargas heredadas sin modo y `SolverOptions.Pipeline` (que hoy equivalen a `Run`), para que nadie mezcle modos sin querer |
| T7 | Sesión principal, en `main` | Ninguno (verificación) | T2-T4 | Build de desarrollo y verificación en el teléfono (criterios A7, A8, B3) |

**API de T1** (propuesta; core-dev puede ajustar nombres justificándolo):

```csharp
// AF.Core.Rules: default(PourMode) == Single (D-001)
public enum PourMode { Single = 0, Run = 1 }
public static class PourModes {
  public const PourMode Default = PourMode.Single;
  public static bool TryParse(string? key, out PourMode mode);   // "single" | "run", ordinal, sin cultura
  public static string ToKey(PourMode mode);
}
static int       PourRules.Amount(BoardState s, int a, int b, PourMode mode);
static BoardState PourRules.Apply(BoardState s, int a, int b, PourMode mode);
static int       PourRules.CollectUsefulMoves(BoardState s, bool futileRuleEnabled, PourMode mode, Pour[] buf);
// IsLegal, IsFutile, HasUsefulMove e IsStuck NO cambian (2.1.2)
LevelSession(BoardState initial, bool futileRuleEnabled, PourMode pourMode);   // + propiedad PourMode
SolverOptions(int nodeBudget, int weight, bool futileRuleEnabled, PourMode pourMode);
static SolverOptions SolverOptions.ForPipeline(PourMode mode);
```

Las firmas actuales sin modo se mantienen **sin cambio de comportamiento** (equivalen a `Run`), para que `tools/` y Unity sigan compilando entre fusiones. Se documentan como heredadas y las retira T6. `SolverParityTests`, la paridad con el prototipo de Python, que vierte rachas, sigue igual. `H_single` se usa solo con `PourMode.Single`. Sin asignaciones en el bucle caliente (`.claude/rules/core.md`).

## 4. Criterios de aceptación medibles

**D-001**

| # | Criterio | Cómo se mide |
|---|---|---|
| A1 | `PourRules.Amount(s, a, b, PourMode.Single) == 1` para todo par legal y 0 para todo par ilegal; con `Run`, el resultado de hoy | `PourModeTests` sobre los 10 tableros del tutorial y ≥ 200 tableros aleatorios con T ≤ 6 (PRNG propio, semilla fija); los 143 tests actuales siguen en verde sin tocarlos |
| A2 | Los pares (A, B) de `CollectUsefulMoves` son idénticos en los dos modos; `Amount` = 1 en `Single` | Test de propiedad en `DotnetOnly`, en el mismo corpus |
| A3 | Descomposición: un vertido `Run` de k dosis equivale a k vertidos `Single` legales y seguidos que llegan al mismo estado | Test de propiedad en el mismo corpus |
| A4 | Con `Single`: par de A* (w = 1) = par del BFS exhaustivo, con y sin poda de fútiles; `H_single ≤ distancia` en todos los estados alcanzables; 0 bytes asignados en el bucle caliente | `AStarSolverPourModeTests`, `PourModePropertyTests` y `HotLoopAllocationTests` ampliado |
| A5 | `level-generator --handmade` (por defecto `single`) da todas las comprobaciones en PASS; dos ejecuciones dan el mismo SHA-256; el informe declara "modo de vertido: single" y, con los tableros actuales, reproduce los pares `Single` de 2.1.4 (3, 9, 10, 12, 16, 17, 15, 17, 16, 19) y los estados alcanzables | Comando de prototipo-v0 b, dos veces, más `sha256sum` |
| A6 | `GameConfig.RulesPourMode == "single"`; `LevelFlow` crea la sesión en `Single`; `TutorialPackTests`: par del pack = par A* en `Single`, la solución gana con `pours` = par, y el nivel 2 tiene ≥ 3 vertidos iguales seguidos | EditMode en verde |
| A7 | En el teléfono (build de desarrollo): todo `AF_STATE` con `"event":"pour"` tiene `move[2] == 1`; cada nivel 1-10 jugado con la solución del informe termina en `win` con `pours` = par `Single`; el evento `load` trae `"pour_mode":"single"` | `adb logcat -d -s Unity \| grep AF_STATE` y un script que cuenta los `move` con tercer valor distinto de 1 (debe dar 0) |
| A8 | Con `adb shell am start ... --es af_rules_pour_mode run`, el primer vertido de una racha mueve más de una dosis (nivel 2, vertido 0→3: `move[2] == 2`) | `AF_STATE` en logcat |
| A9 [P] | Encadenado: tres vertidos A→B tocados cada 300 ms dejan la vista en reposo ≤ 1.200 ms después del último toque, sin saltos de fotograma (contador de saltos de `BoardAnimator` = 0) | `PourChainTests` (PlayMode) |

**D-002**

| # | Criterio | Cómo se mide |
|---|---|---|
| B1 | Tras seleccionar un origen, ninguna otra cuia cambia: no existe ningún objeto `TargetRing` en la jerarquía y solo la cuia seleccionada tiene `IsLifted` | `SelectionShowsNoTargetsTests` (PlayMode), en los niveles 2 y 7 con al menos dos destinos legales |
| B2 | `HighlightTargets`, `SetTargetHighlight` y `TargetRing` no aparecen en `Assets/_Project/Scripts` | `grep -rn "HighlightTargets\|SetTargetHighlight\|TargetRing" Assets/_Project/Scripts` sin resultados |
| B3 | La captura con una cuia seleccionada muestra solo el origen elevado, sin aros | `VisualSnapshotTests` (explícito): `Logs/snapshots/03_selected_l7.png`; en el teléfono, `adb exec-out screencap -p > Logs/device/select_l7.png` tras tocar un origen; revisión visual del dueño |
| B4 | El aro de sellado y la celebración siguen funcionando | PlayMode en verde (`Level1_SolverSolution_ReachesVictory_AndNextOpensLevel2`) |

**Verificación obligatoria de cada tarea** (CLAUDE.md):
- T1: `dotnet build dotnet/AmazonFill.slnx -warnaserror` y `dotnet test` (143 + nuevos); `scripts/unity-test.ps1 -Mode EditMode` (genera los `.meta` de los archivos nuevos del paquete, que se commitean tal cual).
- T2: lo anterior más `python tools/validate_schema.py` (23/23) y el generador dos veces.
- T3 y T4: `scripts/unity-compile.ps1`, `unity-test.ps1 -Mode EditMode` y `-Mode PlayMode`.
- T7: el build de desarrollo de CLAUDE.md, `adb install -r` y A7, A8 y B3. Si `adb devices` sale vacío, se pide al dueño que reactive la depuración inalámbrica.

## 5. Preguntas para el dueño

| # | Pregunta | Recomendación | Bloquea | Estado (2026-10-08) |
|---|---|---|---|---|
| P1 | ¿Confirmas la clave `rules_pour_mode` (`"single"` \| `"run"`, default `"single"`) en la tabla única de 08 §9, con la nueva familia de prefijo `rules_`, y que `Run` quede solo como opción de prueba? | Sí | T5 (08 §9) | Respondida. Con D-005 el default de `rules_pour_mode` pasa a `run`; `single` queda como opción de pruebas |
| P2 | Par del tutorial en `Single`: ¿(A) mantener los 10 tableros y fijar como rango [P] su par medido (3, 9, 10, 12, 16, 17, 15, 17, 16, 19), aceptando la rampa no monótona, o (B) rediseñar los niveles 2-10 para una rampa monótona con rangos que propone level-pipeline-dev en el PR? | B | T2 | Cerrada por D-005: los pares en `run` (3, 6, 7, 8, 10, 10, 11, 13, 14, 16) entran en los rangos de 02 §13; no se rediseñan los niveles 2-10 |
| P3 | Nivel 2: ¿sustituir la lección del vertido parcial por "una dosis por vertido" con el texto [P] "Cada vertido pasa una sola dosis: repite para mover las demás."? | Sí | T2, T5 | Cerrada: el nivel 2 vuelve a la lección del vertido parcial (02 §13); el texto «Cada vertido pasa una sola dosis…» no se usa |
| P4 | Tras verter, ¿el origen se deselecciona (C4 actual, estilo *ball sort*) o sigue seleccionado si puede volver a verter al mismo destino (selección pegajosa, menos toques)? | Deseleccionar (C4 actual) | T3 (solo si cambia) | Cerrada: tras verter, el origen se deselecciona (C4 canónico) |
| P5 | El dedo guía de los niveles 1-2 (02 §13) y Morpho (04 §2.3) muestran un destino: ¿se mantienen como ayudas explícitas compatibles con D-002? | Sí | T5 | Respondida (D-028, 2026-10-19): sí; el dedo guía de 1-2, en DC6. Antes dependía de D-002, no de D-001 (Magic Sort no mostró mano guía en los niveles 387-389, ficha §2.10) |
| P6 | ¿Una dosis por vertido es también la regla de 1.0? Si es así, antes de la Fase 1 hay que recalibrar en `Single`: las cifras [V-proto] de 03 (`par ≥ 2K`, bot ×40 y etiquetas, índice D), Marea "3 por nivel", `restart_confirm_min_pours`, `lluvia_k_pours` y `pirana_k_pours` | Sí, con una tarea de recalibración aparte | Fase 1 (no v0) | Cerrada: `run` es la regla de 1.0; sin recalibración |
| P7 | En 1.0, ¿el pack declara el modo con el que se calculó el par (cambio de contrato: `af-level/2`, `pack_schema` 2)? | Sí | `schemas/` (no v0) | Reescrita: «Con `run` por defecto y `single` solo de pruebas, ¿el formato sigue sin declarar el modo (los packs se generan siempre en `run`) y se retira P7?». Respondida (D-029, 2026-10-19): sí; el formato no declara el modo y P7 se retira |

## 6. Riesgos

| # | Riesgo | Mitigación | Estado (2026-10-08) |
|---|---|---|---|
| R-1 | Presupuesto del solver en `Single` con K de 8 a 14: entre ×1 y ×34 de estados expandidos en el tutorial (nivel 7: 19 → 641); Morpho debe quedar < 50 ms con 40.000 nodos | `H_single` (hasta ×2 menos en el tutorial); medir con `tools/solver-bench` (aún no existe; tarea de core-dev o level-pipeline-dev) antes de 1.0 | Mitigado por D-005: en producción se juega en `run` |
| R-2 | Mezclar modos: un pack con par `Single` jugado en `Run` (o al revés) da pares falsos en QA y en automatización | Modo en el informe y en `AF_STATE load`; el modo solo cambia al cargar un nivel; T6 retira las firmas sin modo | Sigue para los packs de pruebas generados en `single` |
| R-3 | `main` en rojo entre T2 y T3 | Fusión en lote y EditMode tras las dos | — |
| R-4 | Saltos visibles en la cola con toques rápidos (3 o más vertidos iguales) | Encadenado (A9) | Con D-006 aplica a todo vertido: 870 ms de la salida al reposo con 1 dosis. La cola de 2 vertidos por cuia (02 §3.2) lo cubre; el encadenado solo actúa en `single` |
| R-5 | Más rechazos y frustración sin resaltado, sobre todo con 6-7 cuias | Marea ilimitada hasta el 10; vigilar la tasa de `reject` por nivel en `AF_STATE` durante la prueba del dueño | — |
| R-6 | Desviación del canon del género (research/01 §2): las cifras de investigación y de prototipo se midieron con rachas | P6; registrar la desviación en 02 y GDD.md | Desaparece con D-005: `run` vuelve a ser el modo por defecto |

---

## D-003 · Dirección visual «Selva brillante premium»

- **Fecha:** 2026-10-07.
- **Estado:** decidida por el dueño tras ver el prototipo v0.1 (`ef676a4`) y el concepto «Orillas vivas» de la revisión del 2026-10-07. Fase visual implementada con arte procedural provisional en `19177bd` (ver la tabla de arriba). Hay dos puntos pendientes de respuesta: la frontera exacta con el trade dress de los líderes (P8) y los umbrales de contraste (P9).

**Contexto.**
- **El canon vigente:**
  - 07 (Resumen y §1.1) fija el estilo «acuarela botánica sobre vidrio»: fondos desaturados y oscuros (S ≤ 35 %, V ≤ 55 %, 07 §1.2), paneles de papel marfil con grano y botones planos, sin bisel ni degradado (07 §7.2-7.3).
  - 07 §1.3 sustituye la «saturación alta y luz cálida “de cuento”» por esos fondos desaturados.
  - El dueño lo confirmó el 2026-10-06 (GDD.md §2, decisión 23, y §6.1, pregunta 12).
- **El prototipo v0.1** lo aproxima con arte procedural provisional (`ProceduralArt.cs`) y la fuente `LegacyRuntime.ttf`.
- **La revisión del 2026-10-07** (`docs/reviews/2026-10-07/`, hecha por otra IA a petición del dueño) propuso «Orillas vivas»: gouache, papel crema y cuaderno botánico, con el tablero sobre un muelle.
- **Al dueño no le gustan ni el aspecto actual ni ese concepto:** «no parece un juego sino algo como libro». Pide un enfoque «más de juego, con texturas», al nivel de los juegos más populares del género.
- Evaluación de la revisión: `docs/reviews/2026-10-07/evaluacion-claude.md`.

**Decisión.**
1. **La dirección visual es «Selva brillante premium».**
   - El nivel de acabado de los líderes del género: vidrio con brillos y reflejos, colores saturados, botones con volumen, luz cálida, texturas y partículas.
   - Con identidad amazónica propia: madera tallada, hojas, agua de río, frutas del Estuario y la fauna de 07 §2.2.
2. **Sustituye a «acuarela botánica sobre vidrio»** (07, Resumen y §1.1; GDD.md §2, decisión 23, y §6.1, pregunta 12).
3. **Se descarta la estética de papel, gouache y cuaderno de «Orillas vivas»**, incluida su variante B. Se conserva su composición útil:
   - una ventana al río en la franja superior;
   - una repisa o muelle de madera por fila, con sombra de contacto;
   - el bloque de cuias centrado en el área del tablero, no anclado abajo;
   - la bandeja de comodines abajo.
4. **Tipografía:** Baloo 2 (ExtraBold y Bold) para títulos y Nunito (ExtraBold, Bold y Regular) para el texto. Las dos tienen licencia SIL OFL, el dueño las aprobó y ya están copiadas en `Assets/_Project/Art/Fonts/`. Nunito SemiBold (07 §7.1) se sustituye por Bold.
5. **No cambian:**
   - D-001 y D-002: ningún brillo, partícula ni animación varía con la selección ni con la legalidad de un destino.
   - La cuia-garrafa, su cavidad recta, las alturas iguales por dosis, las marcas y el menisco (07 §4). *Las proporciones de la cavidad (0,80 del ancho; 4 de 5,6 u) y el menisco (elipse de 0,40 u): sustituidos por D-006 (2026-10-08); la silueta, la cavidad recta, las alturas iguales y las marcas siguen.*
   - La lista cerrada de trade dress prohibido de 07 §1.3: gatos, caldero, oro biselado, gemas, cintas rojas, calavera y silueta de frasco con labio.
   - Sin personas ni aldeas identificables (07 §8.1).
   - El Estuario como primer mundo con sus cinco tintes, y la paleta, los HEX y los patrones de 07 §3.
   - Las posiciones del HUD de 07 §7.4.
   - Como mucho 60 partículas en gama baja, texto con contraste ≥ 4,5:1 y controles ≥ 48 dp (07 §1.2 y §7.8).
   - Sin Bloom del 2D Renderer (UUM-150232): los brillos se hacen con sprites aditivos o shaders de UI.

**Consecuencias.**
- **Hay que reescribir 07 §1-§7:**
  - el Resumen;
  - §1.1: sale el papel; entran el vidrio brillante, la madera tallada, la hoja y la piedra;
  - §1.2: se retiran el límite de saturación del fondo, el grano de papel y la regla de L*;
  - §1.3: las filas de botones y de «saturación alta y luz cálida», con la frontera que fije P8;
  - §2: pinturas base y variantes;
  - §4.2: brillo y cavidad aislada;
  - §5: partículas y brillos de los storyboards;
  - §6: estilo de Capi y Rosa;
  - §7.1-7.3: contorno de texto, tokens sin papel marfil y botones con volumen.

  Fuera de 07 §1-§7 también cambian:
  - en 07: §10.2 (capturas «en papel marfil»), §11, las «Preguntas abiertas» 1 y 7 y las «Desviaciones de la dirección»;
  - en GDD.md: §2 (decisión 23), §6.1 (pregunta 12) y §9 (paso 11, el brief al artista «acuarela»);
  - 02 §12: «fondos desaturados y oscuros por diseño».

  La revisión listaba solo 07 §§1, 2, 4, 7, 9 y 11, y se quedaba corta. Lo edita la sesión principal en un lote de documentación aprobado por el dueño, después de que este acepte las capturas de la fase visual (P10).
- **La regla de contraste de L* se sustituye.**
  - Comprobado el 2026-10-07 con los L* de 07 §3: ningún fondo uniforme cumple «cada tinte ≥ 25 puntos de L* respecto al fondo». El barrido de 0 a 100 en pasos de 0,1 no da ningún candidato.
  - Con los cinco tintes del Estuario solo valdría L* ≤ 7, un fondo casi negro, lo contrario de esta dirección.
  - En su lugar se mide el resultado compuesto, en espacio lineal y sobre el fondo real que hay detrás del tablero. Umbrales [P], pendientes de P9:
    1. cada tinte del mundo frente a la cavidad vacía: ΔL* ≥ 15;
    2. el contorno de la cuia frente al fondo local: contraste ≥ 3:1 (WCAG 2.1, criterio 1.4.11, componentes no textuales);
    3. la cavidad vacía frente a la Guanábana: ΔL* ≥ 50;
    4. las dosis vecinas de un par de riesgo se siguen distinguiendo por patrón e icono (07 §3), no por L*.
  - Para cumplir 1 y 3 sobre un fondo claro, la cavidad pasa a ser oscura y casi opaca, independiente del fondo:
    - hoy es translúcida, con alfa 0,62 (`ProceduralArt.cs:322`);
    - opaca, su color actual da L* ≈ 12, es decir, ΔL* 20 con el Açaí (32) y 86 con la Guanábana (98).
  - Lo comprueba un test EditMode con los cinco tintes del Estuario.
- **El fondo deja de estar limitado a S ≤ 35 % y V ≤ 55 %.** Fuera de las cuias, el cielo, los bordes y la vegetación pueden ser saturados y luminosos. Detrás de las cuias, el valor lo controlan la repisa o un panel.
- **Código afectado en la fase visual** (unity-gameplay-dev):
  - `BoardLayout.cs` y `BoardLayoutTests.cs`;
  - `GameScreen.cs`: río, repisas y `ShiftToClear`;
  - `ProceduralArt.cs`: fondo, río, vidrio, botones y cavidad;
  - `UiBuilder.cs`: fuentes y estado deshabilitado;
  - `CuiaView.cs`: brillos;
  - `VisualSnapshotTests.cs`.

  Sin paquetes nuevos: las fuentes se cargan como `Font` o con TextMeshPro, que viene con uGUI en Unity 6, importando sus recursos esenciales por script. Ningún `.unity`, `.prefab`, `.mat` ni `.asset` se edita a mano.
- **El arte final sigue siendo del artista** (CLAUDE.md, «Humanos»). El procedural de los agentes es provisional y servirá de referencia para el brief nuevo.
- **Riesgo:** el brillo y la saturación pueden competir con los tintes. Se mitiga con las comprobaciones compuestas y con capturas antes y después de los niveles 1 y 7 en el mismo estado (`VisualSnapshotTests`), además de capturas de estrés de layout con 14 cuias.
- **Después de lo visual** (registrado, sin implementar): la fase de jugabilidad, recursos y detalle de cada elemento, con el backlog priorizado de la §3 de `docs/reviews/2026-10-07/evaluacion-claude.md`.

**Alternativas descartadas.**
- **Mantener la acuarela del GDD:** es lo que el dueño ve como «un libro».
- **«Orillas vivas» completa** (gouache, papel, cuaderno): el dueño la rechazó.
- **Vector plano** (la otra opción de la pregunta 12): no da la sensación de juego que se pide.
- **Copiar el acabado de Royal Match o Magic Sort** (UI dorada biselada, frascos con labio): riesgo de trade dress (07 §1.3; research/08 §4.1).

**Cómo revertir.**
- Revertir los commits de la fase visual: `ProceduralArt.cs`, `GameScreen.cs`, `BoardLayout.cs`, `UiBuilder.cs`, `CuiaView.cs` y sus tests vuelven al estado de `ef676a4`. Si ya se reescribió 07, restaurar su texto anterior.
- Las fuentes pueden quedarse: Baloo 2 y Nunito ya están en 07 §7.1.
- La regla de L* no vuelve en ningún caso, porque es imposible con cualquier dirección: hará falta otra sustitución.
- En todos los casos, añadir a este registro una decisión nueva que sustituya a D-003.

**Preguntas para el dueño (cerradas).**

| # | Pregunta | Recomendación | Bloquea |
|---|---|---|---|
| P8 | ¿Se permiten degradados, relieve, sombras suaves y contorno de texto no dorado en botones de madera, piedra y hoja, manteniendo prohibidos el oro, las gemas, las cintas rojas, los gatos, el caldero, la calavera y la silueta de frasco con labio? | Sí | Botones de la fase visual (V5) y 07 §1.3 |
| P9 | ¿Se sustituye la regla de L* de 07 §1.2 por las cuatro comprobaciones compuestas de arriba, con esos umbrales [P]? | Sí | Test de contraste (V4) y 07 §1.2 |
| P10 | ¿Se reescriben 07 y GDD.md y se encarga el brief nuevo al artista después de aceptar las capturas antes y después de la fase visual? | Sí | Lote de documentación y encargo al artista |

---

## D-004 · Panel azul petróleo, vertido a 38° y cuias más anchas con pocas cuias

- **Fecha:** 2026-10-07.
- **Estado:** decidida por el dueño tras ver en su teléfono (SM-S938B) el build de la fase visual de D-003 (`19177bd`). Respondió a las preguntas C16-C18 de `docs/art/direccion-visual-v2.md` (sección 11) con «azul petróleo, botellas más anchas y 38° está bien». Implementada en el árbol de trabajo, sin commitear (ver la tabla de arriba).

**Contexto.**
- La guía visual v2 dejó abiertas tres preguntas tras la revisión del 2026-10-07:
  - C16, tono del panel del tablero: la variante A «azul petróleo de río» o la B «caoba lacada», para elegir con dos capturas lado a lado (guía 4.6, paso F1).
  - C17, inclinación del vertido: con 38° (02 §4.1, 07 §5.2) y dos filas, el origen tapa la parte alta de la cuia vecina mientras vierte (≤ 400 ms). Los críticos proponían 75° fijos o una inclinación dinámica.
  - C18, ancho máximo de la cuia: 02 §5 y 07 §4.1 lo fijaban en 48 dp en todos los tableros. En el nivel 1 (3 cuias), eso dejaba cuias de 48 dp en ranuras de 112 dp y 196,5 dp de muelle vacío bajo el panel (360 × 780 dp).
- Los líderes del género agrandan los frascos en los primeros niveles (guía, C18).

**Decisión.**
1. **Panel (C16): variante A, «azul petróleo de río»**, tal como la vio el dueño en el build: degradado `panel_alto` #234452 → `panel_bajo` #142C36 (L* 27 → 17) y `cavidad` #102228 (`Palette.PanelA` en `19177bd`). La tabla de la guía en `19177bd` (secciones 2 y 4.6) daba todavía #1E3A44 → #173540, anterior a la ronda 2 de la revisión visual; el build ya usaba los valores de arriba, que son los que vio el dueño. Se descarta la variante B «caoba lacada» (#44302C → #372521, cavidad #251A17), que solo existía para la comparación de F1: sale del código, de las capturas y de los prompts.
2. **Vertido (C17): se mantienen los 38°** de 02 §4.1 y 07 §5.2, con una elevación de 0 a 1 u, de modo que el labio queda entre 0,6 y 1,6 u por encima del borde del destino (`BoardAnimator.MaxRaiseU`, revisión visual del 2026-10-07). Se acepta que, en tableros de dos filas, el origen tape la parte alta de la cuia vecina mientras vierte (≤ 400 ms). Se descartan los 75° fijos y la inclinación dinámica. Esta respuesta no cambia el código. *Sustituido por D-006 (2026-10-08): pose medida en Magic Sort y sin techo.*
3. **Cuias más anchas (C18):** el tope del ancho de la cuia depende del número de cuias T del tablero (las que hay en pantalla, incluidas Cuia extra y Cuia de rescate; 02 R1):
   - tope(T) = 64 dp con T ≤ 3, 56 dp con T = 4-5 y 48 dp con T ≥ 6 (como hasta ahora);
   - ancho = clamp(0,70 × ranura, 28, tope(T)); alto según la proporción 1:3 (6 u); u = ancho / 2;
   - a 360 dp, con ranura = (360 − 24) / cuias_por_fila: T = 3 → 64 × 192 dp, u = 32; T = 4 → 56 × 168 dp, u = 28; T = 5 → ranura de 67,2 dp y 0,70 × 67,2 ≈ 47 dp (el tope no actúa); T ≥ 6, sin cambios. *El alto: sustituido por D-006 (2026-10-08), que lo fija en 5,6 u (≈ 1:2,8; con T = 3, 64 × 179,2 dp). Los anchos y los topes siguen.*
   - el encaje vertical sigue mandando: si el bloque de filas no cabe, `BoardLayout.Compute` reduce el ancho (mínimo 28 dp);
   - tabletas: el tablero se centra con 480 dp de ancho como máximo y la cuia no supera tope(T);
   - si T cambia a mitad de nivel (Cuia extra, Cuia de rescate), el layout se recalcula con el T nuevo (re-layout animado de 300 ms, 02 §4.1). *Matizado por D-006 (2026-10-08): el número de filas se fija al cargar el nivel y no cambia; la cuia añadida va a la fila más corta y solo se recalcula el ancho.*

   Cambian 02 §5 y 07 §4.1 a la vez, como exige 07 §4.1.

**Consecuencias.**
- Tutorial v0 (los datos de `tutorial_v0.json` no cambian):
  - el nivel 1 (T = 3) pasa a cuias de 64 × 192 dp (u = 32);
  - los niveles 2, 3 y 6 (T = 5) siguen en ≈ 47 dp;
  - los niveles 4 y 5 (T = 6, dos filas de 3) y del 7 al 10 (T = 7, filas de 4 y 3) siguen en 48 dp;
  - ningún nivel del tutorial tiene T = 4.
- Nivel 1 a 360 × 780 dp: el panel pasa de 219 a 281 dp de alto (y = 206,5-487,5) y la banda libre del muelle, de 196,5 a 172,5 dp. No baja a ≈ 140 dp como estimaba C18: el bloque está centrado y crece hacia los dos lados. El panel incluye los 36 dp de margen superior de los tableros de una fila [P] (ver el techo del vertido, más abajo).
  - A 360 × 800: panel en 216,5-497,5 y banda de 182,5 dp.
  - A 360 × 640: panel en 136,5-417,5 y banda de 102,5 dp. El poste del muelle sigue visible (banda ≥ 64 dp).
  - El horizonte del panorama sube de 180,5 a 142,5 dp (`GameScreen.HorizonFor`).
- La u máxima pasa de 24 a 32 dp. Lo que se mide en u escala solo (vidrio, líquido, chorro, salpicaduras, pose del vertido). La selección sigue elevando la cuia 16 dp (02 §4.1), que son 0,5 u con la cuia de 64 dp.
- Texturas:
  - en el SM-S938B, el vidrio se hornea a 96 px/u con T ≤ 3 (antes, 72);
  - `ProceduralArt.MaxCuiaPixelsPerU` sube de 96 a 128 para las pantallas de 4 px/dp [P];
  - el aro y la tapa del sellado se generan para u = 32;
  - la fuente del artista a 4× del tamaño máximo pasa de 192 × 576 a 256 × 768 px por cuia de 4 (07 §4.1).
- **Techo del vertido y margen superior en una fila [P].** Son dos propuestas derivadas de C18, no respuestas del dueño. *Sustituido por D-006 (2026-10-08): pose medida en Magic Sort y sin techo.*
  - Con u = 32, el canto alto del origen inclinado sube 0,94 u (30 dp) sobre el borde del destino sin elevación: el labio a 0,6 u más 0,34 u del borde de fibra (`CuiaGeometry.RimHalfWidthU` = 0,55 u, inclinado 38°). Con 1 u de elevación sube 1,94 u (62 dp). Con el margen de 22 dp, una elevación de más de 0,25 u lo sacaba del panel por arriba.
  - **Margen superior:** en los tableros de una fila, el panel deja 36 dp sobre el bloque (`BoardLayout.PanelMarginTopOneRow`, ≈ 22 + 13,5 del marco). Sobre las cuias en reposo quedan 52 dp hasta el borde exterior del panel y 38,5 hasta el canto interior del marco. En dos y tres filas se mantienen 22 dp (`BoardLayout.PanelMargin`): con 36, el río del nivel 7 bajaría de 34,5 a 20,5 dp.
  - **Techo** (`GameScreen.PourCeilingFor`): el origen inclinado se queda 4 dp por debajo del canto interior del marco (`GameScreen.PourCeilingAirDp`: con menos de 1 dp la cuerda del labio rozaba el bisel y la cuia parecía apoyarse en el marco) si entre ese canto y la fila 1 elevada quedan ≥ 22 dp de cavidad (`GameScreen.PourCavityDp`; tableros de una fila, con 22,5 dp). Si no, el techo es el borde exterior del panel (dos y tres filas, 38 dp sobre las cuias en reposo). Nunca sube más de 4 dp dentro de la fila del HUD.
  - En el nivel 1, el techo deja ≈ 4,5 dp (≈ 0,14 u) de elevación y el labio sigue ≥ 0,6 u sobre el borde del destino; la elevación que lo pasaría se recorta. Sin elevación siempre cabe.
- **Tapado en tableros de una fila: aceptado.** En una fila, el vertido entre cuias no vecinas tapa la parte alta de la cuia del medio mientras vierte (captura `04b_pouring_l1`, nivel 1, cuia 0 → 2). Entra en la decisión 2: el dueño respondió «38° está bien» después de ver la captura del teléfono `v2-tel-rafaga-1` (nivel 1, vertido 0 → 2 con la cuia del medio tapada). Si al verlo con las cuias anchas prefiere otra cosa, se abre una decisión nueva.
- C19 (familia parda ≤ 40 % [P]) se vuelve a medir con las cuias de D-004 (`02b_board_l1`, `02d_board_l2`) y con el muelle pintado (F6).
- No cambian: las reglas, el solver, los pares, el formato de nivel, Remote Config (no hay clave nueva), D-001, D-002, D-003, las áreas táctiles (02 §3.3), el paso de fila ni el número de filas (02 §5). *El número de filas: sustituido por D-006 (2026-10-08), que fija 2 o 3 filas desde 11 cuias.*

**Alternativas descartadas.**
- Panel «caoba lacada» (B): cálido, pero el tablero queda más «mueble» que «agua» (guía 4.6).
- Inclinación de 75° fija, o dinámica: 38° + 12° × (4 − dosis que quedan), con tope de 80° (C17).
- Mantener el tope de 48 dp en todos los tableros (C18).
- Una clave de Remote Config para el tope: nadie la ha pedido y 08 §9 no la tiene.

**Cómo revertir.**
- Revertir los commits de la implementación (`BoardLayout.cs`, `ProceduralArt.cs`, `BoardAnimator.cs`, `CuiaGeometry.cs`, `GameScreen.cs` y sus tests) y restaurar el texto de 02 §5 y 07 §4.1, los dos a la vez.
- La variante B se recupera de `19177bd` (`Palette.PanelB`).
- En cualquier caso, añadir una decisión nueva que sustituya a D-004.

**Archivos afectados.**
- Documentación: `docs/gdd/02-nucleo-reglas-ux.md` (cabecera, §4.1 y §5), `docs/gdd/07-arte-tematica-audio.md` (cabecera, §4.1, §4.3 y §5.2), `docs/art/direccion-visual-v2.md`, `docs/art/prompts-chatgpt.md`, este registro y `CLAUDE.md`. `docs/GDD.md` no cambia: ninguna fila de §2 cita el tope de 48 dp, y la decisión 23 da 34 × 102 dp con 7 por fila, que sigue igual.
- Código: `Assets/_Project/Scripts/UI/{BoardLayout.cs, GameScreen.cs, GameBootstrap.cs}` y `Assets/_Project/Scripts/Presentation/{Palette.cs, ProceduralArt.cs, ProceduralArt.Scene.cs, BoardAnimator.cs, CuiaGeometry.cs}`. Las propuestas [P] están en `BoardLayout.PanelMarginTopOneRow` (margen superior de 36 dp en una fila), `GameScreen.PourCeilingFor` y `GameScreen.PourCavityDp` (techo del vertido), y `CuiaGeometry.RimHalfWidthU` (canto alto del borde de fibra, que usa el techo en `BoardAnimator`).
- Tests: `Assets/_Project/Tests/EditMode/{BoardLayoutTests.cs, VisualContrastTests.cs, LiquidVolumeTests.cs}` y `Assets/_Project/Tests/PlayMode/{PourPoseTests.cs, VisualGuardTests.cs, VisualSnapshotTests.cs}`.
- Sin cambios: `Assets/_Project/Data/Levels/tutorial_v0.json`, `Packages/com.amazonfill.core/`, `tools/`, `schemas/` y 08 §9.

---

## D-005 · Vertido por tramo, como Magic Sort

- **Fecha:** 2026-10-08.
- **Estado:** decidida por el dueño tras analizar su propia sesión de 20 min de Magic Sort: «sí, hagámoslo» y «hagámoslo como Magic». Su método: «no vamos a inventar la rueda: copiamos lo que funciona y cómo se hace, con nuestra temática». Responde la pregunta 4.1 (10.2 #1) de `docs/research/magic-sort/propuesta-tintalu.md`. **Sustituye a D-001.** En implementación (ver la tabla de arriba).

**Contexto.**
- Magic Sort y Love Water vierten el tramo entero: 2 dosis en t = 250 y 430, y 3 dosis en t = 598, 600 y 838 (`docs/research/magic-sort/ficha-magic-sort.md` §2.5; Love Water, ficha §10).
- Es la regla de todos los líderes del género (02, Desviaciones 3; research/01 §2 [V]).
- El coste de `single` ya estaba medido en este registro: par del tutorial ×1,0-×1,7 (§2.1.4) y solver con ×1-×34 de estados expandidos (§2.1.3).

**Decisión.**
1. Un vertido mueve la **racha superior visible del mismo color que cabe en el destino**, con vertido parcial (02 §1.2 y R9; modo `run`).
2. `rules_pour_mode` = `run` por defecto (08 §9) y `PourModes.Default = PourMode.Run` en `AF.Core`. `single` queda solo como opción de pruebas: por Remote Config y con el extra de desarrollo `af_rules_pour_mode`.
3. El modo sigue fijo al cargar el nivel y no cambia a mitad (D-001, punto 3).
4. El tutorial v0 se regenera en `run`. Pares: 3, 6, 7, 8, 10, 10, 11, 13, 14 y 16, dentro de los rangos [P] de 02 §13. La lección del nivel 2 vuelve a ser el vertido parcial.
5. D-002 no cambia: Magic Sort tampoco resalta destinos (ficha §2.10).

**Consecuencias.**
- Preguntas de §5: P2, P3, P4 y P6 cerradas; P7 reescrita; P5 sigue abierta.
- Las cifras [V-proto] de 03, medidas en `run`, vuelven a valer.
- No hay que recalibrar Marea «3 por nivel», `restart_confirm_min_pours`, `lluvia_k_pours` ni `pirana_k_pours`.
- R-1 (presupuesto del solver en `single`) deja de aplicar en producción.
- El encadenado de 02 §3.2 solo actúa en `single`. En `run`, con las reglas de v0 (sin mecánicas ni ocultas), dos vertidos A→B seguidos nunca son legales: o el origen cambia de cima o el destino queda lleno.
- La desviación 10 de GDD.md §8 y la desviación 3 de 02 quedan superadas.
- T6 pierde urgencia, porque las firmas heredadas sin modo equivalen a `Run`, que ahora es el modo por defecto. Sigue pendiente.

**Alternativas descartadas.**
- Mantener D-001 (recomendación A de la pregunta 4.1 de la propuesta).
- `single` por defecto y `run` en 1.0.
- Verter la racha con un gesto aparte (ya descartado en D-001).

**Cómo revertir.**
- `PourModes.Default = PourMode.Single` (y el orden del enum), `GameConfig.RulesPourMode = "single"`, regenerar el pack con `--pour-mode single`, los tests de U1 y los textos de D2-D6.
- Siempre con una decisión nueva que sustituya a D-005.

**Archivos afectados.**
- Núcleo (C1): `Packages/com.amazonfill.core/Runtime/AF.Core/Rules/{PourMode.cs, CuiaRules.cs, Pour.cs, PourRules.cs}`, `Runtime/AF.Core/Session/LevelSession.cs`, `Runtime/AF.Solver/Search/{AStarSolver.cs, SolverHeuristic.cs, SolverOptions.cs}`, sus tests (`PourModeTests`, `AStarSolverPourModeTests`, `PourModePropertyTests` y `HotLoopAllocationTests`), `README.md` y `CHANGELOG.md`.
- Generador (L1): `tools/level-generator/{Program.cs, Handmade/HandmadeLevel.cs, Handmade/HandmadeReport.cs, Handmade/HandmadeRunner.cs, Handmade/StateGraph.cs}`, la especificación `tools/level-generator/specs/tutorial_v0.json` y, generados, `Assets/_Project/Data/Levels/tutorial_v0.json` y `tools/out/tutorial_v0_report.md`.
- Unity (U1): `GameConfig.RulesPourMode`, los textos y comentarios de D-001 en `GameFlow/{LevelFlow.cs, TapController.cs}` y `UI/{LaunchOptions.cs, DevProbe.cs, GameBootstrap.cs}`, y los tests que fijaban `single` como modo por defecto (lista en D-001, «Cómo revertir»). `LaunchOptions.cs` y `GameBootstrap.cs` llevan además el gancho de tests `LaunchOptions.LevelPackJsonOverride`, que usan los tests de filas de D-006.
- Documentación (D1-D6): este registro, `docs/GDD.md`, `docs/gdd/{00, 01, 02, 03, 04, 06, 07, 08, 09}`, `docs/art/direccion-visual-v2.md`, `docs/dev/prototipo-v0.md`, la nota de respuestas de `docs/research/magic-sort/propuesta-tintalu.md` y `CLAUDE.md`.

---

## D-006 · Tablero, cuia y vertido como Magic Sort

- **Fecha:** 2026-10-08.
- **Estado:** decidida por el dueño junto con D-005 («sí, hagámoslo» al paquete de tablero y vertido). Responde las preguntas 10.2 #3, #4, #5, #6 y #7 y la 4.3.5 de `docs/research/magic-sort/propuesta-tintalu.md` (secciones 3 y 4). **Sustituye** la decisión 2 y el techo del vertido [P] de D-004, su consecuencia «no cambia el número de filas» y el punto 5 de D-003 en lo que toca a la cavidad y el menisco. En implementación (ver la tabla de arriba).

**Contexto.**
- Medidas de la ficha de Magic Sort: §2.1 (rejilla y filas), §2.2 (botella), §2.4 (selección) y §2.6 (pose y tiempos del vertido).
- Su traducción a nuestra escala en la propuesta, §3.1-§3.5.
- Material: `Logs/device/magicsort/analisis/` (`observar.json`, `ficha_workflow_result.json`, `ms_pour_sheet.png` con el vertido cada 100 ms y los fotogramas de `det_ms/` a 1080 × 2340).

**Decisión.**
1. **Pose del vertido.**
   - El centro de la boca del origen queda sobre el eje del destino (±0,1 u) y su punto más bajo, a 0,2-0,6 u del borde superior del destino. Con el centro de la boca a 0,85 u [P], el punto más bajo queda a 0,30-0,51 u entre 38° y 92°.
   - El origen llega a 38° (ficha §2.6, 3.100 ms) y gira alrededor de la boca: al terminar cada dosis, la inclinación es 92° − 9° × dosis que quedan en el origen (65° con 3, frente a los ≈ 66° que midió Magic Sort; ≈ 92° con 0).
   - El cuerpo queda del lado del que viene la botella (en la misma columna, hacia el centro del tablero). Si ese lado saliera del lienzo entre 38° y el ángulo final, al otro lado [P]. Vale para cualquier vertido, no solo en la misma columna.
   - Escala 1,00.
   - Mientras vierte, el origen se dibuja **delante de todo** (vecinas, marco del panel y HUD), como Magic Sort en t = 250. No hay techo.
   - Se retiran `GameScreen.PourCeilingFor`, `PourCavityDp`, `PourCeilingAirDp`, `BoardAnimator.TopLimitY`, `MaxRaiseU`, `LipClearanceU` y `BoardLayout.PanelMarginTopOneRow`, que existía solo por el techo.
2. **Tiempos** (08 §9).
   - `anim_lift_ms` 100, `anim_travel_ms` 300, `anim_pour_dose_ms` 280 (rango 80-300; Magic Sort, ≈ 285 ms por dosis), `anim_pour_tail_ms` 80 y `anim_return_ms` 300.
   - El chorro sigue arrancando al 70 % del viaje (02 §4.1). De la salida al reposo: 870 ms con 1 dosis (≈ 0,9 s), 1.150 ms con 2 y 1.430 ms con 3.
   - La vuelta va directa a la ranura. El rebote de Magic Sort (≈ 50 px bajo el reposo, visto en un solo vertido) no se copia porque no está confirmado. Se mantiene el rebote de 0,03 u de 07 §5.2.
3. **Cuia por dentro** (opción (a) de la propuesta, §3.4).
   - 1 dosis = 1 u. Cavidad recta de 1,60 × 4,00 u: 0,80 del ancho exterior de 2,00 u.
   - Alto exterior: 5,60 u. Las 4 dosis ocupan 0,714 del alto (≈ 1:2,8).
   - Reparto [P]: pie 0,30, base 0,30, cavidad 4,00, hombro 0,40, cuello 0,35 y borde 0,25 u. El labio queda en 5,35 u.
   - Cara superior del líquido en elipse de 0,40 u, más clara que el tinte (el mismo aclarado por tinte que el menisco actual).
   - Se mantienen el hombro, el cuello corto, el pie, el borde de fibra y la lista de trade dress de 07 §1.3 (nada de labio de botella).
   - Las demás capacidades miden 1,60 + cap u.
4. **Tablero.**
   - 1 fila con T ≤ 5 y 2 filas con T = 6-10, sin cambios.
   - Con T = 11-14: 2 filas (hasta 7 por fila) o 3 filas (≤ 5 por fila; reparto de arriba abajo 4/4/3, 4/4/4, 5/4/4 y 5/5/4), la que dé la cuia más ancha. A igualdad, 2 filas [P].
   - Con T = 15-16: 3 filas (5/5/5 y 6/5/5).
   - El número de filas se fija al cargar el nivel con las T cuias generadas y **no cambia a mitad de nivel**. Cuia extra y Cuia de rescate van a la fila más corta (a igualdad, la de más abajo [P]) y solo se recalcula el ancho.
   - Los topes de D-004 (64/56/48 dp) no cambian. El hueco entre filas sigue en 40 dp (repisa de D-003). No se reserva hueco para el vertido.
   - Se mantiene ≥ 8 dp entre la fila 1 elevada y el canto interior del marco (guía visual v2, §3.2) [P].
5. **Elevación al seleccionar:** 16 dp (Magic Sort sube ≈ 17 dp a nuestra escala).

**Consecuencias.**
- Tamaños de la cuia a 360 × 780 dp:

  | Cuias (T) | Filas | Cuia (dp) |
  |---|---|---|
  | 3 | 1 | 64 × 179,2 (u = 32) |
  | 5 | 1 | 47,04 × 131,7 |
  | 6-8 | 2 | 48 × 134,4 |
  | 11-15 | 3 | 46,9 × 131,3 (Magic Sort con 13-14 botellas: 45 × 138); **matizada por D-046:** 41,6 × 116,5 con la franja de la tropa |
  | 16 | 3 | 39,2 × 109,8 |

  - A 360 × 800 dp, con 3 filas: 48 dp con T = 11-12 y 47,04 dp con T = 13-15.
  - A 360 × 640 dp (16:9) ganan las 2 filas con T = 11-14 (39,2 y 33,6 dp); con T = 15-16, las 3 filas bajan a 30,2 dp.
  - Ancho de color con 13 cuias: 37,5 dp (hoy, 22,1).
  - Fuente del artista a 4×: 256 × 717 px por cuia de 4.
- Cambian 02 §3.2, §4, §4.1, §5 y §8; 07 §3 (regla 4), §4.1-§4.5, §5.2 y §7.4; 08 §5.2, §9 y §14.1; 00 §4; 01 §3.3; 03 (Dependencias, fila 07); 04 §2.2; 09 (Fase 0 de §2.3 y días 4-5 de §9); GDD.md §2 (decisiones 3 y 23); y la guía visual v2.

**Alternativas descartadas.**
- 38° fijos (D-004).
- La inclinación dinámica de C17: 38° + 12° × (4 − dosis), con tope de 80°.
- Reservar el hueco del vertido (≈ 43,6 dp con 11-15 cuias; propuesta §3.3).
- La opción (b) de la propuesta §3.4 (redefinir u).
- El hueco de 22 dp entre filas de Magic Sort.
- La elevación proporcional de ≈ 17 dp (propuesta 4.3.5).

**Cómo revertir.**
- Revertir los commits de U2-U5 y los textos de D2-D6, y restaurar el techo de `b2d34de`.
- Siempre con una decisión nueva que sustituya a D-006.

**Archivos afectados.**
- Código y tests: los de U2-U6, entre ellos los que retiran `GameScreen.PourCeilingFor`, `PourCavityDp`, `PourCeilingAirDp`, `BoardAnimator.TopLimitY`, `MaxRaiseU`, `LipClearanceU` y `BoardLayout.PanelMarginTopOneRow`.
- Documentación (D2-D6): `docs/GDD.md`, `docs/gdd/{00, 01, 02, 03, 04, 07, 08, 09}`, `docs/art/direccion-visual-v2.md`, la nota de respuestas de la propuesta y `CLAUDE.md`.

**Pregunta para el dueño (abierta hasta el 2026-10-19).** Surgió en la revisión del código de D-006. El registro no elige por el dueño (CLAUDE.md, regla 10). **Respondida el 2026-10-19** (D-030, pregunta 2): **(c)**, 3 filas al cargar cuando T + 2 pasa de 14 (T generadas ≥ 13), en cualquier proporción; tarea DC18 (`BoardLayout.RowsFor` y el caso en `BoardLayoutTests`).

| # | Pregunta | Opciones | Bloquea |
|---|---|---|---|
| P12 | En 16:9 (360 × 640 dp) ganan las 2 filas con 11-14 cuias generadas (02 §5). Como las filas no cambian a mitad de nivel (punto 4), con 13-14 cuias generadas la Cuia extra y la Cuia de rescate llevan la fila más corta a 8 cuias (14 + 1, 13 + 2 y 14 + 2): ranura de 42 dp y cuia de 29,4 dp, por debajo del mínimo táctil de 48 dp (02 §3.3; 07 §7.4). Antes de D-006, con 16 cuias se pasaba a 3 filas y la ranura medía 56 dp. ¿Qué se hace? | (a) Aceptar la ranura de 42 dp en ese caso y anotarlo como excepción en 02 §3.3, 02 §5 y 07 §7.4. (b) En 16:9, si la fila más corta ya tiene 7, abrir una 3.ª fila aunque no se fijara al cargar, como excepción al punto 4. (c) Decidir 3 filas al cargar cuando T + 2 pase de 14 | No bloquea v0 (el tutorial tiene T ≤ 7). Hay que responderla antes de los niveles con 13-14 cuias. Después, el caso entra en `BoardLayoutTests` |

---

## D-007 · Mecánicas del río: catálogo de 12, estrenos de la 1.0 y reglas finas

- **Fecha:** 2026-10-08 (respuestas del dueño a las preguntas de la propuesta y reglas finas Q-A, Q-B y Q-C del mismo día). Registrada el 2026-10-11.
- **Estado:** decidida por el dueño, que aceptó todas las recomendaciones de `docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md` §(h). La parte de dificultad de esa propuesta (Q1-Q4, el ciclo y la rotación) se registra aparte como D-009.
  - Implementada en el núcleo, el solver, el generador y `tools/level-qa` en la pista A (rama `fase1-mecanicas-niveles`, integrada en `6faf7c3`; packs 11-200 en `56f220d`) y en Unity en el paso 1 del traspaso (olas 1-4 y verificación T18, de `ecb7892` a `ce0ced4`).
  - Documentación aplicada el 2026-10-11 y completada el 2026-10-12 (paso 2 del traspaso; «Archivos afectados»).
  - **Toda la fauna, la flora, los lugares, los nombres de las mecánicas nuevas y los textos de los tutoriales quedan pendientes de revisión del asesor cultural** (07 §8.2; lista de la propuesta §(i)).
  - Pendientes que no bloquean la 1.0 (preguntas 24, 27-31 y 33-35 de `docs/GDD.md` §6.3): el bloqueo C1-9 (Consecuencias), la confirmación de Lluvia y Piraña en la 1.0 (punto 3), el formato de los respaldos del kill-switch (02 §9.2, regla 3), la tabla de novedades de 201-1000 con el alcance de Q8 (D-009, punto 8), las propiedades P1-P3 y el Remolino con Q-B (Consecuencias), el destino del revelado rítmico (punto 1), «Zafra» (pregunta 28), Revelar en la 3.ª ranura (pregunta 29) y el formato [P] de `pack_schema` 2 (pregunta 30).
  - *Actualización del 2026-10-19 (D-028):* la pregunta 24 quedó decidida en la (b) para la 1.0: el bloqueo C1-9 es la regla de la 1.0 y la (a) se estudia para la 1.1. Las demás de esta lista se decidieron el 2026-10-13 (D-020).
- **Sustituye:**
  - el debut del Brote en el 63 (contrato 1; 02 §9.1 y `mech_debut_level`): pasa al 163 y el Matapi ocupa el 63;
  - la Liana de número como regla de debut (02 §9.1 M3): la Liana debuta con fruta en el 83 y la de número pasa a variante en el 243;
  - la regla de fútiles de 02 §1.4, su copia de 04 §5.1 y el orden de R10 (02 §1.2), por los del anexo B de la propuesta con la respuesta Q-B;
  - en 03 §8, la frase «nunca en niveles con mecánicas con disparador», que Q14 A define.
- **No sustituye:**
  - las reglas de Hoja, Brote, Remolino, Barro, Lluvia y Piraña de 02 §9.1, salvo lo que añaden Q-A, Q-B, Q-C y Q18 B;
  - Aguas turbias (02 §11, T1-T7) ni su estreno en el hito 100 con 1 cuia turbia y la Luciérnaga regalada (04 §2.5);
  - D-002 (ninguna mecánica resalta destinos) ni D-005 (vertido por tramo);
  - el formato `af-level/1` ni el nombre de su esquema: sube a `pack_schema` 2 (contrato 21);
  - el criterio de GDD.md §6 #6 y de la decisión 12 de 09 §8 para Lluvia y Piraña (propuesta §(f): «Criterio de la 1.0 sin cambios»; punto 3).

**Contexto.**
- D-008 dejó reservados el número D-007 y la pregunta P13 para las mecánicas especiales, en pausa: el dueño pidió investigar antes la cultura ribereña y la dificultad escalonada. Esta entrada cierra P13, reservada por D-008.
- La investigación (`docs/research/cultura-ribera/investigacion.md`, web consultada el 2026-10-08) y la propuesta (misma carpeta) sintetizan dos propuestas puntuadas por jueces, con sus correcciones (anexo A). La propuesta termina con 18 preguntas cerradas (§(h)).
- Filtro de cada idea (b.0): copia una función probada de Magic Sort; tiene una regla exacta compatible con el vertido por tramo, con D-002, con el solver y con la generación por superposición; no lleva contadores a favor del jugador; se lee en un segundo; cada desbloqueo tiene animación y sonido; nada sagrado ni personas.
- Antes de programar el Remolino, la Lluvia y la Piraña, la pista A pidió tres reglas finas (Q-A, Q-B y Q-C).

**Decisión.**
1. **Catálogo de 12 mecánicas de campaña** (propuesta b.1 y b.2): las 7 del canon y 5 nuevas. La numeración M8-M12 es provisional. Aguas turbias es un modificador del canon y no cuenta entre las 12.

   | # | Mecánica | Función de Magic Sort que copia | Regla en una línea | Estreno | Versión |
   |---|---|---|---|---|---|
   | M1 | Hoja | Botella atada y frascos cubiertos | Vacío reservado a una fruta; cae con su primer vertido | 43 | 1.0 |
   | M10 | **Matapi** (nueva) | Frasco «?» (equivalencia [E]: la regla de Magic Sort no está observada en el dispositivo) | Nunca es origen: lo que entra no sale | 63 | 1.0 |
   | M3 | Liana con fruta | Tela con icono | Atada; se suelta al sellar una cuia de su fruta | 83 | 1.0 |
   | — | Aguas turbias (modificador) | Capa «?» | Dosis ocultas que se revelan al quedar arriba | 100 | 1.0 |
   | M4 | Remolino | Portales | Lo vertido entra por el fondo de la pareja | 103 | 1.0 |
   | M5 | Barro | Slime y caja regalo | Dosis inerte que acepta cualquier fruta al quedar arriba | 123 | 1.0 |
   | M6 | Lluvia | Caja con temporizador | Cada k vertidos cae agua del mundo | 143 | 1.0 |
   | M2 | Brote | Corcho por color | Cerrado no es origen; germina con su color | 163 | 1.0 |
   | M7 | Piraña | Temporizador enemigo | Cada k vertidos muerde la última cuia destino | 183 | 1.0 |
   | M8 | **Aguapé** (nueva) | Hielo por fila | Un grupo no es origen hasta que una de sus cuias sella | 223 | 1.1 |
   | M9 | **Palizada** (nueva; sustituye a la «Nasa») | Armario con candado y llave dentro del líquido | Troncos sobre 2 o 4 cuias; se abren cuando la dosis-llave queda arriba | 263 | 1.1 |
   | M11 | **Lancha** (nueva) | Cortina que cae con N vacíos | La cuia de carga atraca cuando hay v cuias vacías | 303 | 1.1 |
   | M12 | **Cocha** (nueva) | Temporizador como ventana que se cierra | En v vertidos deja de recibir; solo sirve de origen | 583 | 1.1 |

   - Regla exacta, presentación, desbloqueo, tutorial, base cultural y coste de cada una: propuesta b.2. Las del canon no cambian salvo lo que dicen los puntos 5 y 6.
   - **Variantes** (b.3), para que haya algo nuevo hasta el 1000: Liana de cuenta (243), Hoja atada (283), Brote de dos hojas (343), Flor de vitória-régia (403), Palizada en cadena (443), Llave en la lancha (520, una combinación sin regla nueva, que no es un estreno), Casiquiare (703), Lancha de vuelta (763) y Aguapé que crece (883).
   - **Fuera de la campaña** (b.4; Q10): el nido de tortuga, el defeso y la piracema (contadores a favor del jugador, que se gastan con vertidos fútiles), el tipiti, la Cuia de tacacá (cambia el sellado), el mijano, Dos aguas, la Fiesta de la fruta y las Mareas de equinoccio van a Travesías y eventos, cada una donde dice b.4; el Banzeiro se descarta (b.4: «No»). El revelado rítmico tampoco entra en la campaña, pero las fuentes no coinciden en adónde va: Q10 lo lleva a Travesías y b.4 solo conserva su animación de burbuja para Aguas turbias (pregunta 35 de GDD.md §6.3). Descartes: b.5 (Nasa, tronco hueco, tarrafa como hielo, curral, cacuri, friagem, gamitana, «Regla del río», «Repiquete» como subtítulo visible y la llave entre aguas turbias).
2. **Estrenos de la 1.0** (niveles 1-200; Q5 B):
   - Cuia chica 14 y Cuia alta 27, sin cambios (02 §8);
   - Hoja 43, Matapi 63 y Liana con fruta 83;
   - Aguas turbias 100, con estreno suave (Q9 sí): es lo único especial del nivel, con 1 cuia turbia de 2 dosis ocultas y 1 Luciérnaga regalada. Es la única excepción a «nada debuta en un reto ni en un hito» (D-009) y el único Muy difícil sin un segundo componente: para las «2 mecánicas activas» de 02 §10, una capacidad protagonista cuenta como componente [P] (d.4: 60 Ho+Ch y, con el bloqueo C1-9, 160 Ll+Al6);
   - Remolino 103, Barro 123, Lluvia 143, Brote 163 y Piraña 183.
   - Cada estreno de bloqueador (43-183) ocupa p3-p5 de su ciclo con 2 vacíos, la mecánica sola y un solo ejemplar (02 §9.2, regla 2). Las capacidades no siguen esa regla: la Cuia chica se estrena en la rampa a mano (14 y 18) y en p2 (22), la Cuia alta de 5 en p7-p8 (27 y 28) y la de 6 en el hito 160 (02 §8; d.4); Aguas turbias, en el hito 100 (p20).
3. **Alcance por versión** (Q8: (c) junto con (b′)):
   - la 1.0 lleva las mecánicas hasta el 200 (el canon más el Matapi) y cubre 201-700 con las variantes de coste S (Liana de cuenta, Hoja atada, Brote de dos hojas y Flor) y con combinaciones de lo que ya trae;
   - **1.1:** Aguapé 223, Palizada 263, Lancha 303 y Cocha 583. Llegan en un build, porque exigen código en `AF.Core` y subir `pack_schema`, y sustituyen niveles de 201-700 con `level_overrides` o con una vuelta nueva (08 §4.4). Regla de migración: si el jugador ya pasó el debut de una mecánica, se le sirve una vez su tablero de debut antes de usarla (03 §9, consecuencia 3);
   - las demás variantes dependen de mecánicas de la 1.1 (Palizada en cadena, Llave en la lancha, Lancha de vuelta y Aguapé que crece) o caen después del 700 (Casiquiare);
   - Lluvia y Piraña: sigue vigente el criterio de GDD.md §6 #6 y de la decisión 12 de 09 §8 (propuesta §(f): «Criterio de la 1.0 sin cambios»). La evidencia C3-3 de `Packages/com.amazonfill.core/README.md` lo cumple y la 1.0 las incluye por Q8 (b′), a falta de que el dueño lo confirme al cierre de la Fase 1, como reserva ese criterio (el README del núcleo: «La decisión es del dueño»; pregunta 27 de GDD.md §6.3).
4. **Respuestas del dueño** (propuesta §(h), todas con la recomendación; Q1-Q4 son de D-009):
   - **Q5 B:** Matapi en el 63 y Brote en el 163. La equivalencia del Matapi con el frasco «?» de Magic Sort sigue siendo [E]: el Matapi vale como mecánica propia.
   - **Q6 sí:** la Liana debuta con fruta (`key`) y la de número (`n`) pasa a variante (243).
   - **Q7 sí:** Aguapé, Palizada, Lancha y Cocha entran en la campaña, con kill-switch y respaldo.
   - **Q8:** (c) junto con (b′) (punto 3).
   - **Q9 sí:** el estreno de Aguas turbias en el 100 es lo único especial del nivel, con 2 dosis ocultas (punto 2).
   - **Q10 sí:** los contadores a favor del jugador van a Travesías y eventos, nunca a la campaña.
   - **Q11 A:** las temporadas de aguas altas y bajas solo se ven en la LUT del fondo, sin cartel (D-009, punto 6).
   - **Q12 no:** ni manatí en el elenco ni Rosa como llave de la Palizada; la corriente se lleva el aguapé y la llave es un pez genérico. Se revisa con el arte.
   - **Q13 sí:** se arreglan la regla de fútiles y el orden de R10 antes de programar mecánicas (anexo B; hecho en el núcleo, `AF.Core.Rules.BoardRules`).
   - **Q14 A:** una «mecánica con disparador» es una de liberación (Liana, Flor, Aguapé, Palizada, Lancha) o un contador (Lluvia, Piraña, Cocha, Aguapé que crece). Los filtros (Hoja, Brote, Matapi, Barro) sí pueden ir con Aguas turbias.
   - **Q15 sí:** se encarga la skin Cuia de Santarém a ASARISAN, con pago y crédito, si el asesor y el presupuesto lo permiten.
   - **Q16 B:** `tools/level-qa` mide las trampas tempranas con un tope por etiqueta: 0 en Normal y 1 en Difícil (D-009, punto 9).
   - **Q17 A:** el contenido de la Liana y de la Palizada se ve; ninguna mecánica tapa contenido.
   - **Q18 B:** los vertidos fútiles no avanzan los contadores de Lluvia y Piraña; el contador visible solo baja con vertidos útiles, y R16 se mantiene.
5. **Reglas finas** (respuestas del dueño a Q-A, Q-B y Q-C, 2026-10-08):
   - **Q-A:** una cuia del Remolino no vierte en su propia pareja: el vertido es ilegal (rechazo sin texto, R11).
   - **Q-B:** la exención de `futil` vale también para el origen si es un Remolino o una Lluvia con gotas pendientes, no solo como destino: el anexo B solo eximía como origen la Cocha cerrada, y Q-B añade el Remolino y la Lluvia con gotas pendientes. Así R14 se cumple con esas dos mecánicas.
   - **Q-C:** si la Piraña no puede morder (la cuia que recibió quedó sellada, es un Matapi o la propia cueva, o la cueva está llena), pierde ese mordisco y el contador sigue.
6. **Reglas del núcleo** (anexo B, Q13, Q-B y Q18 B; `AF.Core.Rules.BoardRules`):
   - **R10 por etapas:** (a) revelado de la nueva cima de A; (b) aperturas por recepción (Hoja, Brote); (c) sellado; (d) liberaciones por sellado (Liana); (e) contadores (Lluvia, Piraña), una sola vez por vertido y nunca en un fútil; (f) condiciones del tablero (Palizada y Lancha, en la 1.1); (f′) se repiten (c), (d) y (f) hasta que nada cambie; (g) victoria; (h) bloqueo. Marea restaura el estado anterior entero.
   - **`futil`:** no es fútil verter en una cuia que cambia al recibir (Hoja puesta, Brote cerrado, Matapi, Remolino, Lluvia con gotas pendientes) ni desde un origen exento (Remolino o Lluvia con gotas pendientes, Q-B; en la 1.1, la Cocha cerrada). En lo demás rige 02 §1.4, con la excepción de Aguas turbias.
   - **Restricciones:** como mucho un contador por nivel (Lluvia o Piraña, nunca juntas); las combinaciones prohibidas de la propuesta d.3 (Aguas turbias solo con filtros y capacidades; el Matapi nunca en un par de Remolino, como cuia de Lluvia ni como cueva de Piraña, ni con Barro u ocultas dentro; Lancha y Liana juntas no antes del 401; la dosis-llave de una Palizada nunca en un Remolino ni bajo una dosis de su color); la Pororoca deja fijas las cuias con mecánica cerrada y se deshabilita con Lluvia y Piraña. La implementación añade [P]: sin Remolino junto a la Piraña ni con dosis ocultas.

**Consecuencias.**
- **Contratos:** cambian el contrato 1 (la lista única de 02 §9.1: 14 Cuia chica · 27 Cuia alta · 43 Hoja · 63 Matapi · 83 Liana con fruta · 100 Aguas turbias · 103 Remolino · 123 Barro · 143 Lluvia · 163 Brote · 183 Piraña; la Liana de cuenta, variante en el 243; `mech_debut_level` y `mech_enabled` ganan `matapi`) y el contrato 21 (`pack_schema` 2: tipo `matapi`; Liana con `key` o con `n`, una y solo una; campos obligatorios por tipo; `intro_mechanic` con `chica`, `alta`, `matapi` y `murky` [P], y `difficulty.par_approx` [P]).
- **Piraña:** con el mordisco inmediato de Q-C la heurística `H` sigue siendo admisible y consistente y el par es exacto: no hacen falta `ceil(H/2)` ni `par_approx` (CHANGELOG del núcleo, C3-2).
- **Rendimiento** (README del núcleo, C3-3): en los 36 tableros de referencia de 12-14 cuias, A* llega a 186.346 expandidos, Morpho tarda 1,0 ms como máximo y el bot ×40, 4,8 ms. Con el presupuesto de 300.000 se agotan algunos tableros con Remolino, Lluvia y Piraña, que el pipeline descarta o regenera.
- **Bloqueo C1-9** (pregunta de 02 R16, abierta): la propiedad P1 de R14 falla con Liana y capacidades mixtas (contraejemplo en el CHANGELOG del núcleo). Provisionalmente, `LevelValidation` rechaza la Liana con capacidades mixtas (también con la cueva de la Piraña), y el calendario de la campaña cambia cuatro celdas de d.4 [P] para que la Liana nunca vaya con ellas: 94 Ma → Li y 95 Li+Al → Ma+Al, 160 Ll+Li+Al6 → Ll+Al6 y 198 Pi+Li → Pi+Ho (`tools/level-generator/specs/campaign_v1.json`, `calendar_note`). Las capacidades se quedan: la Cuia alta sigue en el 95 y la de 6, en el 160. Opciones: (a) ampliar `futil` y quitar el bloqueo; (b) mantenerlo. Lo deciden el tech-lead o el dueño. *Decidida el 2026-10-19 (pregunta 24, (b); D-028):* el bloqueo es la regla de la 1.0, ya no provisional; la (a) se estudia para la 1.1.
- **Propiedades P1-P3** (hallazgo de la pista A; CHANGELOG del núcleo, C1-2): el test literal del anexo B («no existe un ciclo de vertidos útiles que vuelva al mismo estado canónico») es falso ya en el juego base (A = [1, 2, 2, 2], B = [3, 3, 2]). La pista A lo sustituyó por tres propiedades que cada mecánica pasa antes de su debut, como garantía aplicada [P]: P1 (R14: todo estado atascado es irresoluble), P2 (R16 en `run`: un fútil entre dos cuias sin mecánica y de la misma capacidad no cambia el estado canónico) y P3 (ningún par monocolor ↔ vacía es útil en los dos sentidos). El dueño aceptó el anexo B con Q13, que incluye ese test entre sus pruebas, y no ha ratificado la sustitución (pregunta 34 de GDD.md §6.3).
- **Capacidades mixtas:** la poda de fútiles del solver puede alargar el par o dejar sin camino útil un tablero que se gana (C1-8; 3 de 309 tableros pequeños en `run`); R14 se cumple.
- **Remolino con Q-B frente a P3** (pregunta abierta del README del núcleo): con la exención de origen, X (Remolino monocolor) → vacía es útil y vacía → X entra por el fondo de la pareja, así que con la pareja vacía el tablero vuelve al mismo estado canónico (test `P3_Remolino_QB_AUsefulRoundTripCanReturnToTheSameCanonicalState`). P1 se cumple (R14), pero P3 no vale para el Remolino: en un estado cuyo único vertido útil sea ese vaivén, `atascado(S)` no se cumple y Aguas estancadas no sale hasta que el jugador cambie el tablero. Es el riesgo que el anexo B describe al eximir orígenes (pregunta 34 de GDD.md §6.3).
- **Kill-switch:** los respaldos por slot de 02 §9.2 (regla 3) no tienen todavía formato de pack ni de id. Hasta decidirlo, `mech_enabled` = false no puede sustituir los niveles de la 1.0 (pregunta 31 de GDD.md §6.3).
- **Paso 1 (Unity):** cada mecánica tiene aspecto procedural [P] (D-003) y desbloqueo al estilo Magic Sort con duraciones [P] (07 §5.6), y cada estreno, su tutorial de una pantalla (02 §8 y §9.1: los 11 textos caben en 2 líneas). El 100 regala la Luciérnaga.
- **Arte y audio:** unas 20 piezas nuevas entre sprites, animaciones y sonidos (propuesta §(g), 07). Todo el arte nuevo lo hace el artista.

**Alternativas descartadas.**
- Q5 A (opción mínima): el canon intacto y el Matapi en el 163, intercambiando Ma y Br en todas las columnas 61-200.
- Q8 (a), todas las mecánicas en la 1.0, y (b), las nuevas por `levels_remote` en la 1.1. La (b) no es posible: los niveles 1-700 van instalados, un paquete nuevo nunca cambia un nivel publicado y el cliente rechaza un `pack_schema` mayor (08 §4.4).
- Q17 C: tapar el contenido de la Liana y de la Palizada, como la tela de Magic Sort. Sería información oculta desde el 83 en niveles Normales.
- Q18 A: que los fútiles avancen los contadores. La Piraña quedaría sin bloqueo posible.
- La Nasa con pez, sustituida por la Palizada, y las demás ideas de b.5.
- Eximir en `futil` a cualquier origen con mecánica: el ir y venir monocolor contaría como útil y el modal de Aguas estancadas no saldría nunca (anexo B). Q-B lo limita al Remolino y a la Lluvia con gotas.

**Cómo revertir.**
- Debuts: cambiar el calendario de `campaign_v1.json` y regenerar los packs. Un pack publicado no cambia: hace falta una vuelta nueva o `level_overrides` (08 §4.4).
- Reglas del núcleo (R10, `futil`, Q-A, Q-B, Q-C y Q18 B): en `BoardRules` y en las mecánicas de `AF.Core`, con sus tests y las propiedades P1-P3.
- Siempre con una decisión nueva que sustituya a D-007.

**Archivos afectados.**
- **Núcleo, solver y herramientas** (pista A): `Packages/com.amazonfill.core/Runtime/AF.Core/{Rules, Mechanics, Board, Session, Determinism}`, `Runtime/AF.Solver/{Search, Bot, Generation, Murky}`, sus tests y `dotnet/AF.Core.Tests/DotnetOnly`; `tools/level-generator` (`--spec` y `specs/campaign_v1.json`), `tools/level-qa`, `schemas/af-level-1.schema.json` (`pack_schema` 2) y `tools/validate_schema.py`; los packs `Assets/_Project/Data/Levels/mundo_02_v1.json` a `mundo_05_v1.json` y el informe `tools/out/campaign_v1_report.md`.
- **Unity** (paso 1): `Assets/_Project/Scripts/{Levels, GameFlow, Boosters, Presentation, UI}` y sus tests (tareas del paso 1 en `docs/dev/traspaso/README.md` §6).
- **Documentación** (2026-10-11 y 2026-10-12, paso 2): este registro; `docs/GDD.md` §2, §3.3, §4, §5, §6 y §7; `docs/gdd/{00, 01, 02, 03, 04, 06, 07, 08, 09}`, con la lista única de estrenos de 02 §9.1 copiada en 02 (Parámetros), 08 §9, 00 §6.5 y el contrato 1; las descripciones de `mechanic_type` y `difficulty.par_approx` en `schemas/af-level-1.schema.json` (cambio editorial, sin subir `pack_schema`); `docs/dev/traspaso/README.md` (§6, paso 2: estado, y paso 4: C1-9; §7). Las cifras [sim] no cambian (D-009, Consecuencias).

---

## D-008 · Bucle de partida como Magic Sort

- **Fecha:** 2026-10-08.
- **Estado:** decidida por el dueño el 2026-10-08: «la estructura se copia de Magic Sort; las cifras salen de nuestro GDD». Su postura general: copiar lo que funciona sin suavizarlo, salvo por política de Google Play.
  - El número D-007 y la pregunta P13 quedan reservados para las mecánicas especiales, **en pausa**: el dueño pidió investigar antes la cultura ribereña y la dificultad escalonada. Nada de esta entrada depende de D-007. *Nota del 2026-10-11: D-007 y D-009 ya tienen su entrada, y D-007 cierra P13; D-009 colorea las cabeceras de ¿Seguro? y Nivel fallido con la etiqueta del nivel.*
  - Respuesta del dueño a la pregunta bloqueante 2 del plan de la fase (2026-10-08, transmitida por la sesión principal): AF.Platform, AF.Save, AF.Ads y AF.IAP se crean **solo con interfaces e implementaciones simuladas**. Sin SDK, sin cambios en `Packages/manifest.json` ni `packages-lock.json`, sin permisos de Android, sin claves y sin red. El dueño revisa el resultado al fusionar.
  - Documentación aplicada el 2026-10-08 en los capítulos de «Archivos afectados», en `117f4be`, dentro de la rama de código `fase2-bucle-partida`.
    - El plan de la fase dejaba estas ediciones del GDD (Dc2-Dc6) para después de las respuestas a P13-P16, como lote aprobado por el dueño. Por eso quedan **pendientes de su aprobación explícita antes de fusionar**. Si no las aprueba en el PR de la pista B, van en un PR de documentación aparte.
    - Qué tiene que aprobar:
      - el contrato 7 (04 §5.2): desde el 31, salir del nivel con el menú es fallo duro; cerrar la app sigue sin serlo;
      - 04 §6 y §7: el embudo con ¿Seguro? y Nivel fallido, que sustituye «Máximo un modal por bloqueo» (§6.6), y «Qué consume» (§7.1);
      - 08 §2.2, §2.3 y §8.2, y la columna «Uso» de tres filas de §9 (contrato 13), sin claves ni defaults nuevos;
      - 00, 02, 05, 06, 07, 09, GDD.md y la nota de respuestas de la propuesta, sin cifras nuevas.
    - Queda abierta una contradicción del GDD que estas ediciones no resolvieron: cuándo se ofrece «Canoas infinitas 1 h» en Sin canoas (P17). *Resuelta el 2026-10-19: P17 A (D-029); 05 §4.1 y §5 se alinearon con 04 §7.3 (DC1b).*
- **Sustituye:**
  - en 04 §6.6, la regla «Máximo un modal por bloqueo; nunca se apila un segundo pop-up»;
  - en 04 §5.2 (contrato 7), la fila «Salir al Mapa del río … no es fallo», solo cuando se sale con el menú (cerrar la app sigue sin ser fallo);
  - 04 §7.1, «Qué consume»;
  - desde el nivel 31, el pie «Volver a la orilla (gratis)» del modal (04 §2.7; glosario §5.1);
  - los pasos 2 y 3 que la propuesta §6.1-§6.2 marcaba como «no copiamos».
- **No sustituye:**
  - precios y cifras (05 §2, 08 §9);
  - la misma semilla al reintentar (02 §2; GDD.md §2 #11);
  - Marea como primera opción del modal (02 R15; 04 §6.2, regla 0);
  - ninguna oferta de pago antes del 31 (05 §10);
  - intersticiales solo tras victoria (05 §6.1).

**Contexto.**
- **Magic Sort** (ficha §4.3-§4.7 y §6):
  - «No Available Moves»: Play On por 900 o Give Up;
  - «Continue?»: «You will lose 1 life!», con Play On por 900 otra vez;
  - «Level Failed!»: el premio perdido y Try Again;
  - con 0 vidas, «More Lives»: Refill por 900 o +1 Life por anuncio, con temporizador;
  - salir del nivel también cuesta una vida («Are You Sure?»);
  - tras la victoria, una cadena de pantallas;
  - las vidas ilimitadas no quitan los intersticiales.
- **Nuestro canon:** un solo modal con Marea → Cuia de rescate → Volver a la orilla (04 §6), y canoas desde el 31 (04 §7).

**Decisión.**
1. **Embudo, paso a paso.** Cifras de 08 §9. «≥ 31» = `rescue_min_level`.
   - **Paso 0 · Detección.**
     - Cuándo: `atascado(S)`, con los fútiles (R13).
     - Qué pasa: la entrada se bloquea al instante. El modal se abre `stuck_modal_delay_ms` = 400 ms después de la última animación (R15).
   - **Paso 1 · Aguas estancadas** («No hay más vertidos»).
     - El tablero queda atenuado detrás; arriba, semillas y canoas; el HUD de partida y la bandeja se ocultan.
     - **(a) Marea primero** (04 §6.2, regla 0):
       - Deshacer, si hay cupo o stock (ilimitada en 1-10; 3 por nivel desde el 11);
       - si no, la recarga: «+3 Deshacer · Ver video» (`rv_undo_refill`: 4 al día, 60 s) o «+3 · 30 semillas».
     - **(b) Play On, desde el 31:** «Continuar: +1 cuia» (Cuia de rescate).
       - 600 semillas, o 300 en el primer rescate del día (`rescue_first_day_mult` 0,5).
       - Máximo 1 por nivel.
       - Solo se ofrece si un A* de 40.000 nodos sobre el estado + 1 cuia confirma que la compra cumple (04 §6.4).
       - Sin saldo, el botón sale en gris con «Te faltan {n} semillas», junto a:
         - Puñado, 1,99 (COP 3.900, 1.000 semillas);
         - Balsa de rescate, 4,99 (COP 9.900): 1 al día, oculta 48 h tras 2 rechazos, orden T3.
     - **(c) Pie:**
       - en 1-30, «Volver a la orilla (gratis)» reinicia sin coste (rama C de 04 §6.3);
       - desde el 31, «Rendirse» [P] lleva al paso 2.
     - Bajo el titular, la línea de canoa (`stuck_canoe_lost` o `stuck_canoe_lost_last`), oculta con Canoas infinitas.
   - **Paso 2 · ¿Seguro?** [P]. Desde el 31, tras «Rendirse».
     - Mensaje: «Perderás 1 canoa» («tu última canoa» si solo queda una).
     - Vuelve a ofrecer «Continuar: +1 cuia», al mismo precio y con las mismas condiciones; si el rescate no está disponible, no aparece.
     - La X y «Rendirse» confirman la rendición.
     - Con Canoas infinitas no hay canoa en juego y se pasa directo al paso 3 [P].
   - **Paso 3 · Nivel fallido** [P]. Tras confirmar la rendición.
     - Muestra «Nivel {n}» y «Premio: {20 · 60 · 80} semillas», según la etiqueta (`eco_reward_*`); ese premio no se cobra.
     - Botones: «Reintentar» [P] y X, que vuelve al inicio.
     - Al entrar se resuelve el fallo duro (04 §5.2 y `failHard` de 04 §6.2):
       - `fails_level` + 1;
       - desde el 31, −1 canoa, salvo con Canoas infinitas;
       - la Crecida, cuando exista, se rompe salvo con Hoja escudo.
     - «Reintentar» carga el mismo tablero, con la misma semilla.
     - Al tercer fallo carga `relief[0]` con el cartel «El río bajó», de 2.000 ms (`anim_river_lowered_banner_ms`; 03 §13).
   - **Paso 4 · Sin canoas** (04 §7.3; «Más canoas» es como lo llamó el dueño, pregunta P14).
     - Se abre con «Reintentar», «Jugar» o «Siguiente» sin canoas, desde el 31.
     - Opciones:
       - cronómetro «Tu próxima canoa vuelve en mm:ss»: 1 canoa cada 25 min, máximo 5;
       - «Canoa extra · Ver video», +1 (`rv_extra_canoe`: 3 al día, 90 s);
       - «Llenar canoas · 200 semillas», hasta 5/5;
       - «Canoas infinitas 1 h» (`af_infinite_canoes_1h`: 1,99, COP 3.900).
         - *Nota del 2026-10-08: cuándo se ofrece está pendiente de P17. 04 §7.3 la pone siempre, y así lo hace `NoCanoesScreen`; 05 §4.1 y §5, solo tras rechazar dos veces la Canoa extra en el día. El código no cambia hasta la respuesta. Respondida el 2026-10-19: A, siempre (D-029), como ya lo hacía `NoCanoesScreen`.*
     - En cuanto hay una canoa, vuelve a «Nivel fallido» con «Reintentar», como Magic Sort (t = 338) [P].
   - **Salir.** Desde la pausa, «Salir».
     - Desde el 31: «¿Salir del nivel? Perderás 1 canoa» [P] → fallo duro → «Nivel fallido» → inicio. Cuesta la canoa aunque no haya vertidos, como Magic Sort en t = 5 [P] (pregunta P16).
     - En 1-30 se sale sin coste.
     - Cerrar la app o un cuelgue no es fallo: el estado se guarda y se reabre tal cual (04 §5.2; 02 §14).
2. **Reiniciar por voluntad propia** no cambia: Volver a la orilla en el HUD o en la pausa, fuera del bloqueo, sigue siendo gratis.
   - Con ≥ 8 vertidos es un fallo blando: cuenta para «El río bajó» y para el Salto del Boto, pero no gasta canoa (04 §2.7 y §5.2).
3. **Victoria con una cadena corta.**
   - Celebración de 900 ms (`anim_celebration_ms`).
   - Pantalla de victoria (02 §6):
     - «¡Nivel completado!»;
     - Cosecha de +20, +60 o +80 semillas, acreditadas con un contador de 400 ms;
     - desde el 11, «Cosecha doble: x2 por video» (`rv_double_harvest`: 5 al día, 60 s); sin anuncio disponible, no aparece.
   - «Siguiente», sin auto-avance (`victory_autonext_s` = 0) → AD_GATE → nivel N+1.
   - «Inicio» → pantalla de inicio [P].
   - Como mucho 1 takeover por sesión.
   - Sin el calificativo «¡Perfecto!»: la pregunta 10.2 #25 sigue abierta. *Decidida el 2026-10-19 (D-029): «¡Perfecto!» solo cuando el intento se gana sin Cuia de rescate (tarea DC14).*
4. **Anuncios simulados detrás de AF.Ads**, sin SDK.
   - **Intersticial**, solo tras tocar «Siguiente» (05 §6.1 y §6.3):
     - nunca antes del nivel 22 ni en la sesión de instalación;
     - cada 3 niveles en 22-60 y cada 2 desde el 61;
     - ≥ 150 s desde el anterior y ≥ 120 s desde el inicio de la sesión;
     - como mucho 6 por sesión y 12 al día;
     - nunca en las 24 h siguientes a una compra ni a < 60 s de un recompensado.
   - **Nunca** al reintentar, al rendirse, en «Nivel fallido» ni en «Sin canoas», aunque Magic Sort lo haga (05 §10).
   - **Recompensados:** Marea, Canoa extra y Cosecha doble, con sus topes por ubicación y 15 al día en total.
   - **Las Canoas infinitas no quitan los intersticiales** (postura del dueño, 2026-10-08).
     - Solo los corta la regla de 24 h tras cualquier compra, que también se aplica a `af_infinite_canoes_1h` (05 §4.1).
5. **Compras simuladas detrás de AF.IAP**, sin Play Billing.
   - Los 13 SKU de 05 §4.1, con los precios locales de 05 §4.2.
   - Lo comprado se acredita con `Wallet.Apply`.
   - Una compra fallida o cancelada nunca descuenta.
6. **Pantalla de inicio** (06 §4.7), con la composición de Magic Sort (ficha §7.1: saldos arriba, botón central, pestañas abajo) y sin su trade dress (07 §1.3; D-003).
   - Botón grande «Jugar · Nivel {n}», con el chip Difícil o Muy difícil cuando toque [P].
   - Semillas.
   - Canoas «n/5» con cronómetro, desde el 31.
   - Ajustes.
   - Mercado, Mapa del río, Reto del río y Bitácora quedan ocultos hasta que existan.
7. **Reintentar con otra distribución, como Magic Sort (ficha §2.9): no se adopta.** El GDD no lo permite (02 §2; GDD.md §2 #11; 05 §10, «cambiar el tablero sin avisar»). La pregunta 10.2 #20 quedó decidida el 2026-10-19 en la #20 C, misma semilla, con «El río bajó» al 3.er fallo (D-030, pregunta 7 (a)).

**Consecuencias.**
- **Más presión y más ocasiones de compra:** una segunda oferta de rescate y la pérdida a la vista.
  - El ratio de presión y las cifras [sim] de 05 §3 no modelan la segunda oferta ni el coste de salir (contrato 18).
  - Se mantienen como referencia y se recalibran con el embudo real del soft launch [P].
- **Máquina de estados (02 §2):** nuevos estados GIVE_UP_CONFIRM, LEVEL_FAILED y NO_CANOES. FAIL_RESOLVE se dispara al confirmar la rendición.
- **Analítica (08 §8.2):** `af_stuck_choice` gana los valores `give_up`, `confirm_play_on` y `quit` [P].
- **Textos nuevos** [P] (pregunta P14):

  | Clave | Texto |
  |---|---|
  | `stuck_give_up` | Rendirse |
  | `giveup_confirm_title` | ¿Seguro? |
  | `giveup_confirm_canoe` | Perderás 1 canoa |
  | `level_failed` | Nivel fallido |
  | `level_failed_reward` | Premio: {n} semillas |
  | `retry` | Reintentar |
  | `quit_confirm` | ¿Salir del nivel? Perderás 1 canoa |

- **Persistencia:** el nivel en curso se guarda tras cada vertido (02 R8 y §14), para que cerrar la app no sea una salida gratis.
- **Remote Config:** sin claves nuevas en 08 §9.

**Alternativas descartadas.**
- Un solo modal y reinicio en 1 toque (04 §6.6; propuesta §6.1).
- Otra distribución en cada reintento: el GDD no la permite.
- Intersticial tras fallar o que corta la celebración (Magic Sort, t = 196 y 629).
- Que las Canoas infinitas quiten los anuncios.
- Quitar Marea del modal para igualarlo a Magic Sort (pregunta P15).

**Cómo revertir.**
- Revertir los commits de B2-2 y B2-3: el modal único de 04 §6.2 vuelve.
- Una constante de desarrollo, `GameConfig.StuckFunnel` [P], conserva la variante de un solo modal. No es una clave de Remote Config.
- Siempre con una decisión nueva que sustituya a D-008.

**Archivos afectados.**
- **Unity:**
  - `Assets/_Project/Scripts/{Platform,Save,Economy,Ads,IAP,Boosters,GameFlow,Levels,UI,Presentation}`;
  - `Assets/_Project/Data/Config/{iap_catalog,ad_placements}.json`;
  - `Assets/_Project/Tests/**`.
- **Documentación** (aplicada el 2026-10-08 en `117f4be`, pendiente de la aprobación explícita del dueño antes de fusionar, ver «Estado»; entre paréntesis, lo que se tocó además de la lista original):
  - 02 §2 y §7 (y §6, §14 y «Preguntas abiertas» 2);
  - 04 §2.7, §5.2, §6 y §7 (y §3, §4, §10.1, §11 y las secciones finales);
  - 05 §5 y §6.1 (y §2.4, §3.4, §6.3 y §10);
  - 06 §4.7 (y §4.3 y «Preguntas abiertas» 1);
  - 07 §5.5 y §7.5;
  - 08 §2.2 y §8.2 (y §2.3 y la columna «Uso» de tres filas de §9, sin claves nuevas);
  - 00 §2.2 y §10 (y §5.1, §6.3 y §6.4);
  - GDD.md §2 (#11, #14 y #15, y #17), §4 y §6.2 (contrato 7), con una nota en §7.1 y §9;
  - 09 §2.3 (y §1.1, filas #10, #13, #14 y #22);
  - la nota de respuestas de `docs/research/magic-sort/propuesta-tintalu.md`.
  - Nota de P17 (2026-10-08): 04 §7.3 y «Preguntas abiertas» 9; 05 §4.1, §5, «Conflictos detectados» y «Dependencias» (fila 04); y la fecha de cabecera de los dos. Solo señalan la contradicción; no cambian ninguna regla.
  - Pendientes: el resumen de la definición de fallo de 03 §13, la línea de decisiones de `CLAUDE.md` y, tras la respuesta a P17, alinear 04 §7.3 o 05 §4.1 y §5 (lote Dc4; hecho el 2026-10-19 en DC1b con P17 A: se alinearon 05 §4.1 y §5).

**Preguntas para el dueño** (no bloquean).

| # | Pregunta | Recomendación |
|---|---|---|
| P14 | ¿Apruebas los textos nuevos y que la pantalla de 04 §7.3 siga llamándose «Sin canoas» en vez de «Más canoas»? | Sí |
| P15 | ¿Se mantiene Marea como primera opción del modal (02 R15; 04 §6.2), aunque Magic Sort no la tenga? | Sí |
| P16 | Desde el 31, ¿salir del nivel cuesta una canoa también con 0 vertidos, como Magic Sort (t = 5)? | Sí |
| P17 | ¿Cuándo sale «Canoas infinitas 1 h» (`af_infinite_canoes_1h`, 1,99) en Sin canoas? El GDD se contradice. **A:** siempre, detrás de esperar, el video y las semillas, y sin cuenta atrás: así lo dibuja 04 §7.3, la listan sin condición 02 §2, 06 §4.7 y el paso 4 de esta decisión, y así lo hace ya `NoCanoesScreen`. **B:** solo al quedarse sin canoas tras rechazar la Canoa extra dos veces en el día, con 1 impresión contextual al día que caduca a los 10 min: así lo dicen 05 §4.1 y §5; habría que definir qué cuenta como «rechazar» y cambiar el código. En los dos casos sigue en el Mercado | A [P]: es lo que dicen los demás capítulos y lo que hace el código, y la caducidad de 10 min de 05 §5 choca con «Sin cuenta atrás de oferta» de 04 §7.3. En contra: Magic Sort no vende vidas ilimitadas en «More Lives» (ficha §4.5). Con A se corrigen 05 §4.1 y §5; con B, 04 §7.3, 02 §2, 06 §4.7, este paso 4 y el código. Se alinea en el lote Dc4; hasta la respuesta, el código no se toca |

**Respondidas el 2026-10-19** (D-029, con su recomendación): P14, P15 y P16, sí; P17, A. La 10.2 #20 de la propuesta (punto 7), sin recomendación aplicable, se decidió el mismo día en la #20 C, misma semilla (D-030). D-008 queda sin preguntas abiertas del dueño.

---

## D-009 · Dificultad escalonada: ciclo de 20 reordenado, etiqueta antes de jugar y rotación de plantillas

- **Fecha:** 2026-10-08 (respuestas del dueño a Q1-Q4 de la propuesta). Registrada el 2026-10-11.
- **Estado:** decidida por el dueño con la recomendación de cada pregunta: Q1 A, Q2 sí, Q3 A y Q4 A de `docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md` §(h). El resto de la propuesta (mecánicas) es D-007.
  - Implementada en la pista A: `tools/level-generator` (`--spec`, con el ciclo y el calendario de `specs/campaign_v1.json`), `tools/level-qa` y `tools/econ_sim.py` (tarea P-2, `0faf51e`). En Unity, en el paso 1 del traspaso: chip y cinta en Jugar y en Siguiente, tarjeta previa, HUD y cabeceras de ¿Seguro? y Nivel fallido.
  - Documentación aplicada el 2026-10-11 (paso 2 del traspaso).
  - **Las cifras [sim] regeneradas con el ciclo nuevo esperan la aprobación del dueño** (P-2; `docs/dev/traspaso/planes-integracion.json`). Hasta entonces el GDD conserva las de la corrida del 2026-10-06 (GDD.md §7.3, V5). *Aprobadas el 2026-10-19 (pregunta 25 de GDD.md §6.3, (a); D-029): el GDD cita ya la corrida con el ciclo nuevo (DC1b) y V5 está cerrada.*
  - Los nombres internos de las posiciones (Vaciante, Aguas bajas, Repiquete, Bajada, Remanso, Pico) y los nombres de hito son [P] y pasan por el asesor cultural.
  - *Actualización del 2026-10-13:* **D-013 cambia el contenido de la tarjeta previa del hito p20** (punto 2): añade «Si lo superas, llegas a {sitio}» (`map_hito_preview`, glosario §10) y la silueta del sitio al que se llega al ganarlo (02 §10; 07 §7.7). La pregunta 40 (la Cuia chica entre los 2 vacíos) quedó decidida con su recomendación (a) (D-015): la campaña v1 no cambia.
- **Sustituye:**
  - en 03 §12, los retos en p6, p10, p14, p17 y p19 (contrato 1, incidencia 20 de GDD.md §7.2): pasan a p6, p9, p12, p15 y p18, y p19 pasa a ser un remanso puro;
  - en 03 §11, la regla «el hito siempre va precedido de un reto»: el hito va precedido de un remanso, y este de un reto;
  - los retos con etiqueta de 41-59 (el modelo de 05 §3.1 usaba p6, p14 y p19): pasan a p6, p12 y p18, y p9 y p15 quedan suaves;
  - los retos suaves de 21-39 (26, 30, 34, 37 y 39): pasan a 26, 29, 32, 35 y 38;
  - en 02 §9, «nunca en reto (p6, p10, p14, p17, p19)»;
  - en 02 §10, la señalización previa (solo la tarjeta): se suman el chip y la cinta en Jugar y en Siguiente.
- **No sustituye:**
  - la cuenta del ciclo: 12 de comodidad, 5 retos, 2 de recuperación y 1 hito;
  - el primer Difícil en el 40 y el primer Muy difícil en el 60 (contrato 6);
  - los umbrales del bot ×40 (Difícil ≥ 75 %, Muy difícil ≥ 90 %) ni que la etiqueta la decide la medición, no el slot;
  - los premios de 20, 60 y 80 semillas (`eco_reward_*`);
  - el reintento con la misma semilla, «El río bajó» al tercer fallo y el Salto del Boto (c.6);
  - la excepción de debut de 03 §12 (2 vacíos, una sola mecánica y bot < 20 % en p3-p5).

**Contexto.**
- El dueño pidió una dificultad «escalonada» como la de Magic Sort: dientes de sierra con etiquetas antes de jugar, un respiro tras cada pico y plantillas que rotan y vuelven (traspaso §8).
- Magic Sort pone la etiqueta y el «x5» en el botón con el que se arranca el nivel y anuncia el pico tras un respiro, como en sus niveles 387 → 388 → 389 (`docs/research/magic-sort/ficha-magic-sort.md` §3.6).
- En el ciclo anterior, el reto p19 iba seguido del hito p20: dos niveles con etiqueta seguidos (anexo A de la propuesta).

**Decisión.**
1. **Ciclo de 20 reordenado** (c.3; Q1 A). Desde el 21, `p = ((n − 1) mod 20) + 1`:

   | p | Rol | Nombre interno [P] | Vacíos | Fuente | Ventana de bot (21-40 / 41-60 / 61+) | Etiqueta posible | Contenido |
   |---|---|---|---|---|---|---|---|
   | 1-2 | Recuperación | Vaciante | 2 | `walk`, K−1 | < 30 % | — | Puro |
   | 3-5 | Comodidad o estreno | Aguas bajas | 1 (2 hasta el 30 y en debuts) | `walk` | 20-60 % (< 20 % en debuts) | — | Debut, o 0-1 mecánicas |
   | **6** | Reto | Repiquete | 1 (2 en 21-39) | `fill`/`deep` | 50-70 / 60-80 / ≥ 75 | Difícil desde el 41 | 1-2 mecánicas |
   | 7-8 | Comodidad | Bajada | 1 | `walk` | 20-60 % | — | 0-1 mecánicas |
   | **9** | Reto | Repiquete | ídem | ídem | ídem | Difícil | 1-2 mecánicas |
   | 10-11 | Comodidad | Bajada | 1 | `walk` | 20-60 % | — | 0-1 mecánicas |
   | **12** | Reto | Repiquete | ídem | ídem | ídem | Difícil | 1-2 mecánicas |
   | 13-14 | Comodidad | Bajada | 1 | `walk` | 20-60 % | — | 0-1 mecánicas |
   | **15** | Reto | Repiquete | ídem | ídem | ídem | Difícil | 1-2 mecánicas |
   | 16-17 | Comodidad | Bajada | 1 | `walk` | 20-60 % | — | 0-1 mecánicas |
   | **18** | Reto | Repiquete | ídem | ídem | ídem | Difícil | 1-2 mecánicas |
   | **19** | Remanso | Remanso | 1 (2 hasta el 30) | `walk` | 20-40 % | — | **Puro** |
   | **20** | Hito | Pico | 1 | `deep`, K máximo | 75-85 / ≥ 90 / ≥ 90 | Difícil (40) · Muy difícil (60+) | 2 mecánicas (3 desde el 320) |

   Reglas del escalón (c.3):
   1. Nunca hay dos niveles con etiqueta seguidos: entre dos retos hay siempre dos niveles y p19 no lleva etiqueta.
   2. Tras cada nivel con etiqueta, comodidad: el índice D baja al menos una banda, dentro del −3,4 de 03 §12.
   3. Tras el hito, dos niveles de recuperación.
   4. Los retos suben dentro del ciclo: desde el 61, D(p6) ≤ D(p9) ≤ D(p12) ≤ D(p15) ≤ D(p18) < D(p20); en 41-59 solo cuentan los retos con etiqueta, D(p6) ≤ D(p12) ≤ D(p18) < D(p20).
   5. El pico se anuncia tras un respiro.
   6. Nada debuta en un reto ni en un hito. Única excepción: Aguas turbias en el hito 100 (D-007). Una combinación sin regla nueva no es un debut.
   7. En 41-59, los retos con etiqueta son p6, p12 y p18 (3 de 5); p9 y p15 son suaves. El 40 es el primer Difícil y el 60, el primer Muy difícil.

   La especificación de la campaña concreta las ventanas de los retos [P]: con etiqueta, 75-89 %; suaves, 50-74 % (`tools/level-generator/README.md`).
2. **La etiqueta se ve antes de jugar** (c.1; Q2 sí):

   | Dónde | Normal | Difícil | Muy difícil |
   |---|---|---|---|
   | Botón «Jugar · Nivel n» del inicio (D-008, punto 6) | Sin chip | Chip ámbar con la rana dardo y la cinta «×3» | Chip índigo con el jaguar, la cinta «×4» y el botón en variante nocturna |
   | Botón «Siguiente» de la victoria (D-008, punto 3; 02 §6) | Sin chip | Chip ámbar y cinta «×3» del nivel siguiente | Chip índigo y cinta «×4» del nivel siguiente |
   | Tarjeta previa de 1 toque | Ninguna; en un debut, el tutorial hace de tarjeta | Rana, «Difícil», «Aguas bravas», «Premio: 60» e iconos de las mecánicas del nivel [P] | Jaguar, «Muy difícil», «Noche de jaguar», «Premio: 80» e iconos de las mecánicas [P] |
   | HUD | — | Chip y marco ámbar | Chip y marco índigo, fondo de noche |
   | ¿Seguro? y Nivel fallido (D-008) | Cabecera normal | Cabecera ámbar | Cabecera índigo |

   - Siguiente lleva la etiqueta porque, con D-008, tras ganar se pasa directo al nivel siguiente sin ver el botón del inicio.
   - Un reto que mide menos del 75 % se publica sin chip y sin «×3» (c.2). Sin calavera.
3. **Premios** (c.2; Q3 A): 20, 60 y 80 semillas, mostrados como cinta «×3» y «×4» sobre las 20 de un nivel normal. El «×5» (Muy difícil a 100) exige antes rehacer `tools/econ_sim.py` y `tools/out/econ_*.md` (contrato 18).
4. **Subtítulos** (c.1; Q4 A): «Aguas bravas» en Difícil y «Noche de jaguar» en Muy difícil, los del canon (glosario §6.1); se apagan con `ui_show_difficulty_subtitles`. «Repiquete» queda como nombre interno de los retos.
5. **La señal de dificultad es la luz, no la altura del agua** (c.5): el agua alta es la Crecida, que es buena. Muy difícil lleva la variante noche y el fondo un 15 % más oscuro (02 §10); el Difícil puede llevar una luz de tarde [P]. Sin barra ni texto; la luz sigue a la etiqueta medida.
6. **Rotación de plantillas** (c.4 y d.1-d.2):
   - **Plantillas por posición** (c.4): p1-p2 puras, con 2 vacíos y K−1. p3-p5: en un ciclo de debut, la mecánica nueva sola en 2-3 tableros; en los demás, comodidad con la titular del ciclo. p7-p8, p10-p11, p13-p14 y p16-p17: 0-1 mecánicas, con al menos 3 de esos 8 niveles puros. Retos: según el ciclo de vida (abajo). p19: puro y más fácil que la media de comodidad. p20: mecánicas con 15 niveles o más desde su debut y que ya salieron en un reto; al final de un mundo, su firma.
   - **Temporadas** (d.1; Q11 A de D-007): cada ciclo es de aguas altas (A: Lluvia, Aguapé, Lancha, Liana y Flor) o de aguas bajas (B: Matapi, Palizada, Piraña y Cocha); Hoja, Hoja atada, Brote, Remolino, Casiquiare y Barro son de todo el año. La temporada solo cambia el peso de cada mecánica en la bolsa (×2 las de su familia, ×1 las de todo el año, ×0,5 las de la otra) y la LUT del fondo, sin cartel. No sigue el calendario real.
   - **Ciclo de vida de una mecánica** (d.1): C0, el debut (titular en p3-p5, en p7, sola en el reto p9 y en dos o tres retos más; como mucho 8 niveles en el ciclo); C+1, secundaria de la siguiente titular en 2-3 retos; C+2, descanso o una comodidad; C+3 o C+4, regreso en una combinación nueva; después, la bolsa general.
   - **Reglas de la bolsa** (d.2): al menos un 30 % de niveles puros por ciclo (p1, p2, p19 y 3 de comodidad); una mecánica en 6 niveles por ciclo como mucho (8 en su ciclo de debut) y nunca en 3 seguidos fuera del estreno; al menos una «primera vez» por ciclo (debut, variante, firma, primera combinación o forma de tablero nueva), con un registro versionado de combinaciones publicadas para que el test de reproducibilidad siga valiendo; toda mecánica vuelve al menos cada 3 ciclos de su temporada (120 niveles o menos) y las de todo el año, cada 2; como mucho 2 mecánicas por nivel antes del 300 y 3 después (3 solo en retos e hitos); un contador por nivel; temporadas alternas, nunca más de 2 ciclos seguidos de la misma, y el ciclo de debut de una mecánica toma su temporada; kill-switch con `mech_enabled.<id>`.
7. **Calendario 1-200** (d.4): la tabla de la propuesta, nivel a nivel, con los estrenos de D-007, los debuts de tinte, los retos suaves, los retos que apuntan a Difícil, los hitos que apuntan a Muy difícil y los nombres de hito [P], pendientes del asesor. La campaña la copia en `tools/level-generator/specs/campaign_v1.json`, salvo las cuatro celdas [P] del bloqueo C1-9 (D-007, Consecuencias). Cada titular sale 8 veces en su ciclo de debut y vuelve como secundaria en el ciclo siguiente.
8. **Regla para 201-1000** (d.5): la misma plantilla y las mismas reglas; cerca del 25 % de Difícil y del 5 % de Muy difícil desde el 201 (03 §11, sin cambios); 2 titulares por ciclo (de la temporada o de todo el año) y 1-2 invitadas, y una titular descansa en el siguiente ciclo de su temporada; las temporadas por ciclo de la tabla de d.5 (201 A · 221 A · 241 B…); nunca más de 60 niveles sin algo nunca visto; después del 1000, las vueltas reciclan las plantillas con las LUT de lluvia, noche y alba. La tabla de novedades de d.5 suponía otro alcance de la 1.0: con el (c) de Q8 (D-007) hay que rehacerla (pregunta 33 de GDD.md §6.3).
9. **Medición** (c.7), además de los KPI de 03 §15:
   - intentos medios por mecánica en su ciclo de debut: alarma si superan 2,5 en un nivel Normal; se sustituyen los slots con `level_overrides` y nunca se aplaza el estreno siguiente;
   - trampas tempranas (jugadas iniciales que llevan a Aguas estancadas en 2 vertidos o menos): tope 0 en Normal y 1 en Difícil (Q16 B de D-007);
   - acierto de etiqueta (retos que alcanzan el 75 %): si baja del 60 % en un ciclo, se revisa el generador de esa posición.

**Consecuencias.**
- **Simulación** (P-2, `0faf51e`): `tools/econ_sim.py` pasa a `CHALLENGE_POSITIONS` = {6, 9, 12, 15, 18} y a los retos con etiqueta p6, p12 y p18 en 41-59. La cuenta de etiquetas no cambia, pero el orden de los retos mueve las cifras [sim]. Algunas de las celdas que cita el GDD (05 §3 y sus copias en 01, 04, 09 y GDD.md) que cambian; es una selección, no la lista completa, que es el diff de `0faf51e` en `tools/out/econ_*.md`:
  - **Escenario B** (canon, `tools/out/econ_B.md`): saldo final del casual 1.842 → 1.912, del medio 15.510 → 15.856, del hábil 26.408 → 26.092 y del pagador ligero 8.393 → 8.843; ingreso gratuito total del casual 30.709 → 30.735, del medio 48.857 → 48.816 y del hábil 46.486 → 46.431; ingreso por victoria 61,4 → 61,5 (casual), 97,7 → 97,6 (medio), 93,0 → 92,9 (hábil) y 97,2 → 97,3 (pagador); Cuias de rescate pagables por día del hábil 2,57 → 2,56; Cuias de rescate usadas 72,5 → 72,3, 72,1 → 71,3, 45,0 → 45,4 y 87,5 → 86,7; bloqueos sin recursos del casual 5,9 → 5,6 por corrida, en el 73 → 74 % de las corridas, con el primero en el 249 → 260; **el primer bloqueo sin recursos del medio pasa del 87 al 44** (corridas afectadas, 2 → 1 %) y **el del pagador ligero, del 311 al 282** (corridas afectadas, 10 → 14 %; 0,4 → 0,5 bloqueos por corrida); neto diario en 101-500 del casual −11 → −9, del medio +316 → +325, del hábil +851 → +841 y del pagador +124 → +138; saldo del medio al cerrar el 100, 5.331 → 5.431 (sigue por encima de la palanca de 4.000 de 05 §3.5, el argumento de 05 §3.6 y de su pregunta 2); USD del pagador 3,54 → 3,64. Los ratios de referencia no cambian (7,7 / 7,2:1).
  - **Escenario A** (`tools/out/econ_A.md`): saldo final del medio 23.815 → 24.043, del hábil 32.107 → 31.727 y del pagador 14.323 → 14.739; **primer bloqueo sin recursos del pagador 110 → 258** y del medio 57 → 49. Los ratios de referencia no cambian (7,0 / 6,6:1).
  - **B con tramos** (`tools/out/econ_B_tramos.md`): referencia de 101-500, 10,2 → 10,3:1; casual con 21,3 bloqueos por corrida en el 100 % de las corridas → 20,7 en el 99 %, primer bloqueo 198 → 203 y saldo final 895 → 893; medio con 3,1 (45 %) → 2,9 (44 %), neto diario −34 → −23, primer bloqueo 352 → 355 y saldo final 4.224 → 4.670; hábil con saldo final 18.120 → 17.710 y neto +508 → +494; pagador con 12,9 (90 %) → 13,2 (92 %), primer bloqueo 305 → 314 y saldo final 1.536 → 1.584.
  - **Variantes** (`tools/out/econ_variants.md`): en V0, saldo final del medio 15.686 → 16.067 y del casual 1.876 → 2.014, y casual con bloqueo sin recursos 70 → 76 %; en las demás, los ratios se mueven 0,1 como mucho y los saldos y porcentajes, en la misma medida.
  - El detalle celda a celda, con las celdas que esta selección no recoge, es el diff de `0faf51e` en `tools/out/econ_*.md`. Las cifras del GDD se actualizan cuando el dueño las apruebe (V5; pregunta 25 de GDD.md §6.3). *Aprobadas el 2026-10-19 (D-029) y llevadas al GDD (DC1b).*
- **Campaña v1 medida** (`tools/out/campaign_v1_report.md`):
  - etiquetas: 31-60, 13,3 % Difícil y 3,3 % Muy difícil; 61-100, 20 % y 5 %; 101-200, 23 % y 5 %. Desde el 61 los cinco retos apuntan a Difícil (25 % por ciclo), por encima del 15 % y del 15-20 % de 03 §11: queda como pregunta abierta (pregunta 26 de GDD.md §6.3);
  - acierto de etiqueta por ciclo: 67-100 %; cada remanso queda por debajo de la media de comodidad de su ciclo (QA-REMANSO);
  - cuias y vacíos: en 31-100 hay 22 niveles con menos cuias que el mínimo de 03 §11 y en 101-200 el 64 % de las comodidades y los remansos se relajó, a 2 vacíos o con una Cuia chica de 2 (03 §4.4); el informe los deja para revisar;
  - la Cuia chica entre los 2 vacíos: el generador la cuenta como uno de ellos (02 §8; `tools/level-generator/README.md`, decisión [P]), así que el 23, el 26, el 29, el 35 y el 38 llevan un vacío de 4 y una Cuia chica donde la tabla del punto 1 pide 2 vacíos. Queda por ratificar (pregunta 40 de GDD.md §6.3; 03 §12).
- **Unity** (paso 1): chip y cinta en Jugar y en Siguiente; tarjeta previa de 1 toque (una sola en el 100, combinada con el tutorial de Aguas turbias y la línea T4); chip y marco en el HUD, con un velo que oscurece el fondo un 15 % en Muy difícil (la variante noche con LUT queda [P]); cabeceras de ¿Seguro? y Nivel fallido.
- **Pendiente técnico:** la temporada de cada ciclo no tiene todavía fuente de datos en el pack (la pista A la dejó solo en `campaign_v1.json`): la LUT de temporada necesita un campo nuevo, que subiría `pack_schema`, o una tabla del cliente (pregunta 32 de GDD.md §6.3).

**Alternativas descartadas.**
- Q1 B: el ciclo del canon (retos en p6, p10, p14, p17 y p19), con dos etiquetas seguidas en p19 y p20.
- Q3 B: el Muy difícil a 100 semillas («×5», como Magic Sort) sin rehacer la simulación.
- Q4 B: «Repiquete» como subtítulo visible, un regionalismo poco conocido.
- La «Regla del río» como cuarta señal de dificultad y subir el agua en los niveles difíciles: chocan con la barra de la Crecida, donde el agua alta es buena.
- Un cartel de temporada en p1 (Q11 B): competiría con «El río bajó».

**Cómo revertir.**
- Volver a las posiciones anteriores en `campaign_v1.json`, `tools/level-qa` y `tools/econ_sim.py`, regenerar `tools/out/econ_*.md` (contrato 18) y regenerar la campaña. Los niveles ya publicados no cambian: hace falta una vuelta nueva o `level_overrides` (08 §4.4).
- Siempre con una decisión nueva que sustituya a D-009.

**Archivos afectados.**
- **Herramientas** (pista A): `tools/level-generator` (`Campaign/`, `Generation/` y `specs/campaign_v1.json`), `tools/level-qa` (comprobaciones del ciclo, de banda, del remanso y de trampas), `tools/econ_sim.py` y `tools/out/econ_*.md` (P-2).
- **Unity** (paso 1): `Assets/_Project/Scripts/UI/{LobbyScreen, DifficultyChip, GameScreen.Labels, GameScreen.Funnel, GiveUpConfirm, LevelFailedScreen, LevelCardView}.cs` y `Assets/_Project/Scripts/GameFlow/{LevelCardInfo, LevelFlow.Cards}.cs`, con sus tests.
- **Documentación** (2026-10-11, paso 2): este registro; `docs/GDD.md` §2, §4, §6 y §7; `docs/gdd/{00, 02, 03, 04, 05, 06, 07, 08, 09}`, con solo una nota fechada en 05 §2.1 y §3.1. Las cifras [sim] de 01, 04, 05, 09 y GDD.md quedan para cuando el dueño las apruebe (V5); se aplicaron el 2026-10-19 (D-029, DC1b).

---

## D-010 · «Llenar y teñir»: el río llena las cuias y los miquitos las tiñen con fruta

- **Fecha:** 2026-10-08 (idea del dueño y su ampliación, y guion de la intro del mismo día). Registrada el 2026-10-09. La ampliación del 2026-10-09 (elenco de unas 50 especies que tiñen) se registró el 2026-10-11, al final de esta entrada.
- **Estado:** decidida por el dueño, con el guion `docs/research/intro-rio-miquitos/guion.md` aprobado para implementar. Implementada en la rama `intro-rio-miquitos` (`fbdf11e`, con las revisiones `e479cf0` y `8955ef3`), **sin fusionar en `main`**.
  - Las 16 preguntas del guion (§18) no tienen respuesta explícita una a una. La implementación aplica la recomendación de cada una, salvo Q11 y Q14. Aquí quedan como **recomendación aplicada, pendiente de confirmación** (tabla del final): no bloquean y el registro no elige por el dueño (CLAUDE.md, regla 10). *El 2026-10-19 el dueño aceptó las recomendadas (D-029): Q11 (DC15), Q14 (DC13) y Q17, y F7-F13 de la propuesta de fauna.*
  - **Toda la fauna, los nombres comunes, los datos de las tarjetas y la palabra «miquitos» (y su traducción) quedan pendientes de revisión del asesor cultural** (07 §8.2).
  - La propuesta de fauna (`docs/research/fauna/propuesta-fauna.md`) queda como **material de apoyo**, no canon, con sus preguntas abiertas (F1-F14; estado al final). Donde choca con el guion, manda el guion (punto 7).
  - Numeración: D-007 (mecánicas, debuts) y D-009 (dificultad escalonada) son de la pista de mecánicas y niveles (rama `fase1-mecanicas-niveles`, `tools/level-generator/README.md`), que todavía no ha traído sus entradas a este registro. El código de esta rama cita la intro como «pendiente de D-009» (`IntroAnimation.cs`, `MonkeyRig.cs`, `TroopLayer.cs` y `SightingCard.cs`): debe decir D-010. *Nota del 2026-10-11: D-007 y D-009 ya tienen su entrada.*
- **Sustituye:**
  - «Sumergir y teñir» (02 §4.2, 07 §5.1, 08 §5.3 y glosario §6.4): la caída de las cuias (fase a), el agua de inmersión por fila (guía visual v2, 6.11) y «un icono de fruta por dosis desde fuera de pantalla»;
  - la contradicción del orden del teñido (02 §4.2: «de abajo arriba»; 07 §5.1: «la de arriba primero»; el código teñía de arriba abajo): dentro de la cuia, de abajo arriba;
  - en Aguas turbias, «la dosis oculta recibe una gota parda y el ?» (07 §5.1);
  - `sfx_submerge` y `sfx_emerge` (07 §9.2).
- **No sustituye:**
  - la dirección 18 y sus claves de 08 §9 (`anim_tint_duration_ms_early` 1.400, `anim_tint_duration_ms_late` 800, `anim_tint_fast_from_level` 21, `anim_tint_skippable`, `anim_tint_skip_alarm_pct` 0,40): **sin claves, defaults ni tiempos nuevos**;
  - las fases a-d como cortes (300 + 400 + 250 + 450 ms y 150 + 250 + 150 + 250 ms);
  - el nivel en memoria antes de animar, el salto al primer toque y `af_anim_skipped` con `anim = tint`;
  - el fundido de 0,25 s con Reducir movimiento y el banner de mundo antes de la intro;
  - D-002: ningún miquito mira, señala ni reacciona a una cuia.

**Contexto.**
- **Idea del dueño (2026-10-08), literal:** «Quiero que plasmemos una idea única. El río Amazonas llena los recipientes: debemos hacerlo más visual, que se entienda mejor que eso pasa; y para poder pintar o revelar los colores, que unos miquitos tiren unas semillas de diferentes frutas autóctonas del Amazonas y los pinten de ese color; que sea muy visual y rápido. Investiguemos sobre ello y hagámoslo muy bien logrado.»
- **Ampliación del mismo día, literal:** «Podemos, a medida que suba de niveles, que diferentes miquitos de especies vayan apareciendo. Podemos explorar fuertemente la fauna del Amazonas para tener algunas cosas visuales bonitas y que hagan algo también.»
- **Lo que había:** «Sumergir y teñir», implementado en `IntroAnimation.cs` con `ImmersionWater.cs` (capturas `01_intro_l7` y `01b_intro_tint_l7`). El agua de inmersión nacía en la cara de la repisa sin un camino visible desde el río, así que no se entendía que el río llena, y la fruta caía «desde fuera de pantalla».
- **Método del dueño** (D-005): copiar lo que funciona y darle nuestra temática, aquí con base real, investigada y respetuosa.
- **Material:**
  - el guion (síntesis de dos guiones evaluados por jueces, con fuentes [V] y [E] en su §21);
  - la propuesta de fauna (elenco, 18 animales «que hacen cosas» y página Fauna del Cuaderno de campo);
  - `docs/research/cultura-ribera/investigacion.md` (pulso de inundación, F6).

**Decisión.**
1. **Nombre: «Llenar y teñir»** (EN «Fill & Tint» [P]; PT-BR «Encher e tingir» [P], con el asesor).
   - Sustituye a «Sumergir y teñir» en el glosario §6.4 y en todos los capítulos, y conserva el verbo de la marca (Tintalú, el tinte que colorea el agua).
   - Código: la clase `IntroAnimation` se mantiene; se añaden el planificador puro `IntroChoreography`, el catálogo `FaunaCatalog` y su resolución `IntroCast`. La analítica no cambia.
2. **El río llena, y se ve** (guion §6; canon en 02 §4.2 y 07 §5.1).
   - Las cuias están vacías y secas en su ranura desde el fotograma 0: no caen.
   - El río del fondo crece: se aclara y sus destellos se deslizan hacia el panel, sin cresta ni frente que avance (eso sería la Pororoca, comodín Barajar).
   - Desborda el canto superior del panel y corre por la madera del marco y de las repisas.
   - Un chorrito por cuia no vacía, desde la madera de encima, llena la cuia desde el fondo **solo hasta sus dosis**, con la gramática del vertido (D-006). Las vacías quedan secas.
   - Causalidad: el chorrito se abre ≤ 30 ms después de que el agua pase sobre el eje de la cuia, y el llenado empieza ≤ 50 ms después del contacto. Todo lleno antes de 700 / 400 ms.
   - El vaciado es vertical, nunca una ola lateral (icono de Marea).
3. **La fruta tiñe** (guion §5 y §7).
   - **Una fruta por racha visible**, como mucho 24. Con más rachas, una por cuia no vacía y la cuia florece de abajo arriba en cascada (25 ms entre rachas).
   - **Fruta, no semilla:** la semilla ámbar ovalada es el icono de la moneda (glosario §2.1). La forma en vuelo es de la familia del icono de accesibilidad del tinte (07 §3). Casos especiales: el achiote suelta semillitas rojas y angulosas; el Agua del Río Negro es una hoja seca que se hunde; el Agua del Tapajós, 3 gotas de una hoja sacudida. Nunca un grano de cacao suelto. Tabla por tinte en 07 §5.1.
   - Al tocar el agua, anillo en la superficie y floración OKLab de 100 ms desde el agua del río, igual para todos los tintes.
   - **Orden:** filas de arriba abajo; dentro de la fila, por pasadas; **dentro de la cuia, de abajo arriba** (Q4). La última floración termina exactamente en T.
   - **Aguas turbias:** las ocultas no reciben fruta ni nota y su «?» aparece en todas a la vez con el primer impacto. El calendario no depende del color real de una oculta.
4. **Los miquitos cambian de especie con el progreso** (ampliación del dueño; catálogo en 07 §6.4).
   - **Las especies son datos:** catálogo `Assets/_Project/Data/Fauna/miquitos_v0.json` (formato `af-fauna/0`) con esqueleto (A-D), modelo de suelta (`fila`, `cuelga` o `sacude`), paleta, tropa, rasgo de reposo, tramo de niveles, nombres, dato de la tarjeta y ranuras de arte. El código no conoce ninguna especie, y cambiar o añadir una no toca código. Sin Remote Config.
   - **Una especie por mundo, de géneros distintos**, porque los grandes ríos separan especies (Wallace 1852 [V]): mono ardilla (1-10), sagui plateado (11-40), sauim-de-coleira (41-80), churuco (81-140), huapo colorado (141-200), cotoncillo colorado (201-300), pichico emperador (301-420), maquisapa (421-700; recomendación de Q8), mono nocturno (701-820), saquí de cara blanca (821-920), mono ardilla de los Llanos como variante (921-960) y **sin miquitos en El origen** (961-1.000).
   - **Nunca la especie de otro mundo.** Pasa a **modo dosel** (sin tropa: el follaje se sacude y la fruta cae por detrás del HUD) en estos casos:
     - reintentos;
     - mundo 13;
     - sin franja libre de 44 dp entre el HUD y el panel (3 filas, pantallas de 640 dp);
     - especie sin arte final en un build de release.
   - La especie cambia **qué** se ve, nunca **cuánto** dura.
   - Tres modelos de suelta en lugar de un gesto a medida por especie. La identidad va en la silueta y en el rasgo de reposo.
5. **Tarjeta «¡Miquito a la vista!»** [P] (Q10).
   - Pone nombre a la especie **después** de la intro, en el primer nivel libre desde su «tarjeta desde» (07 §6.4), solo en primer intento.
   - Dura 2,5 s, no bloquea el toque y no tapa cuias ni botones. Una tarjeta por nivel, con prioridad tutorial → Fruta nueva → miquito. *Sustituido por D-011 (2026-10-11): dura según lo que hay que leer, 3-7 s [P] (punto 2), y sale cuando la tropa se ha ido (punto 3).*
   - Lenguaje de avistamiento: «a la vista», nunca «nuevo», «tu mono» ni «adoptar». Sin confeti.
   - Clave `fauna_sighted_card` (glosario §10).
6. **Representación: reglas duras** (guion §4.6; 07 §6.4).
   - Siempre libres, en su árbol y sin personas. Sin rasgos humanos ni pintura corporal.
   - No son guías: no hablan, no presentan el banner y no dan consejos.
   - No cruzan el río.
   - Las especies amenazadas hacen el mismo gesto neutro que las demás y nunca son premio.
   - La máscara del mono ardilla no se lee como calavera. Nombres vetados.
7. **Fauna fuera de la intro: no se decide aquí.**
   - La propuesta de fauna queda como material de apoyo con sus preguntas abiertas:
     - 18 animales «que hacen cosas»: ambiente con microinteracciones, celebraciones y presentación de mecánicas;
     - la página Fauna del Cuaderno de campo;
     - el puente en el Mapa del río.
   - **Donde choca con el guion, manda el guion:**
     - «¡Miquito a la vista!» en lugar de «¡Nuevo miquito!»;
     - modo dosel en lugar de la especie del mundo anterior cuando falta el arte;
     - sin lianas laterales con 3 filas (rompían la distancia de 8 dp a las cuias); *sustituido por D-021 (2026-10-14): tropa lateral alternando, con la distancia de 8 dp como restricción;*
     - el avistamiento se registraría al mostrar la tarjeta, no al hacer la intro;
     - tres modelos de suelta en lugar de nueve gestos;
     - el maquisapa en lugar del fraile en los mundos 8-9 (Q8).

**Consecuencias.**
- **Tiempos y Remote Config:** sin cambios (contrato 13). Si Remote Config cambia T (1.200-1.500 o 600-1.000 ms), todo el calendario se escala por T / 1.400 o T / 800. La versión larga o corta la decide la clave, no T.
- **Código** (ya en la rama; lista en «Archivos afectados»):
  - un planificador puro y determinista calcula el calendario una vez, en arrays preasignados (0 B por fotograma);
  - todas las capas de la intro van en Canvas anidados propios con `raycastTarget = false`;
  - se borra `ImmersionWater.cs`;
  - la escena se regenera con `GameSceneBuilder.CreateGameScene`.
- **Arte** (guion §12; 07 §6.4):
  - el artista dibuja las piezas por especie sobre 4 esqueletos (Q16), además de los retratos, 14 frutas, la rama y la liana;
  - coste: unos 30 días para las especies 1-5 [E], con prioridad para el mono ardilla;
  - mientras no haya PNG, miquito procedural solo en builds de desarrollo. El vídeo de la tienda y el soft launch no se graban con provisionales.
  - Las plantillas por esqueleto que deben preparar los agentes en `docs/art/` no existen todavía.
- **Audio:** `sfx_river_rise`, `sfx_fill` y `sfx_fruit_plip` [P] sustituyen a `sfx_submerge` y `sfx_emerge` (07 §9.2). Hoy no hay audio en el código; lo encarga el dueño (07 §9.5).
- **Analítica:** sin cambios en 08 §8.2. `af_fauna_sighted` queda reservado y **no se emite** hasta Q11, que exige la aprobación del dueño, una migración del perfil (`ProfileMigrations`) y la entrada en 08 §8.2. *Q11 aprobada el 2026-10-19 (D-029): la entrada de 08 §8.2 está hecha (DC1b) y la migración y la sonda, en DC15.*
- **Accesibilidad:** el ajuste Reducir movimiento todavía no existe en el perfil. La ruta ya está en el código: `LevelFlow.ReduceMotion` y `GameConfig.ReducedMotionIntroMs` = 250, que no es una clave de 08 §9.
- **Riesgos** (guion §15):
  - densidad de la versión larga (R1);
  - con 3 filas no se ve el río (R3);
  - repetición en tramos largos (R5), que mitiga la variación determinista por nivel;
  - representación (R8);
  - arte sin encargar (R9);
  - rendimiento en clase A sin dispositivo para medir (R11).
  - La prueba de pasillo con cinco personas (A11) sigue pendiente. Si falla con 3 filas, el respaldo es la cortina de agua (guion §6.3).

**Alternativas descartadas** (guion §0.2 y §18).
- Mantener «Sumergir y teñir»: ya no describe lo que pasa y no se ve que el río llena.
- «Río y fruta» como nombre: rompe el par de verbos.
- Inundación por fila desde los lados: no puede llenar una cuia cuya boca queda por encima.
- La cortina desde arriba como camino principal: queda de respaldo con 3 filas.
- Un frente que avanza por el río: se lee como la Pororoca.
- Una fruta por dosis: hasta 64 en 250 ms con 16 cuias.
- Semillas en lugar de fruta: chocan con el icono de la moneda.
- Solo monos ardilla con otra paleta: el jugador no nota el cambio.
- Nueve gestos a medida: no se perciben en 150 ms.
- La especie del mundo anterior cuando falta el arte: contradice la barrera de los ríos.
- «¡Nuevo miquito!»: suena a adquisición, y el elenco tiene especies amenazadas (sauim CR; churuco y huapo VU; maquisapa EN).
- Lianas laterales con 3 filas. *Sustituido por D-021 (2026-10-14).*
- Una versión de estreno de 1,8 s en el nivel 1: exigiría una clave nueva (Q12).

**Cómo revertir.**
- Revertir `fbdf11e`, `e479cf0` y `8955ef3` y el commit de documentación de esta entrada:
  - `ImmersionWater.cs` y la intro anterior de `IntroAnimation.cs` vuelven de `b137533`;
  - la escena se regenera con `GameSceneBuilder.CreateGameScene`;
  - el texto de 02 §4.2, 07 §5.1 y §9.2, 08 §5.3, el glosario y la guía visual vuelve a su versión anterior.
- Quitar solo la tropa y dejar el río y la fruta (modo dosel siempre; fase 2 del guion, §19) **no tiene interruptor hoy**: exige un cambio de código.
- Siempre con una decisión nueva que sustituya a D-010.

**Archivos afectados.**
- **Código, datos y tests** (rama `intro-rio-miquitos`, `fbdf11e`, `e479cf0` y `8955ef3`):
  - `Assets/_Project/Data/Fauna/miquitos_v0.json`;
  - `Assets/_Project/Scripts/GameFlow/{GameConfig.cs, LevelFlow.cs, SightingRule.cs}`;
  - `Assets/_Project/Scripts/Levels/{FaunaCatalog.cs, FaunaCatalogReader.cs, WorldTable.cs}`;
  - `Assets/_Project/Scripts/Presentation/{IntroAnimation.cs, IntroChoreography.cs, WaterStripGraphic.cs, BezierStripGraphic.cs, TroopLayer.cs, MonkeyRig.cs, ProceduralArt.Fauna.cs, ProceduralArt.Fruit.cs, GraphicPool.cs, ShaderClock.cs, AmbientLayer.cs, ArtSlots.cs, CuiaView.cs, LiquidGraphic.cs, ProceduralArt.cs}` (se borra `ImmersionWater.cs`);
  - `Assets/_Project/Scripts/UI/{GameScreen.cs, SightingCard.cs, UiText.cs, GameBootstrap.cs, LaunchOptions.cs, DevProbe.cs, Editor/GameSceneBuilder.cs}`;
  - `Assets/_Project/Shaders/Resources/{AF_UI_Liquid.shader, AF_UI_Stream.shader}`;
  - `Assets/_Project/Editor/CI/GeneratedArt.cs` y `Assets/_Project/Art/Generated/README.md`;
  - la escena `Assets/_Project/Scenes/Game.unity`, regenerada;
  - tests EditMode (`FaunaCatalogTests`, `IntroArtTests`, `IntroChoreographyTests`, `SightingRuleTests`, `ArtExportTests`) y PlayMode (`IntroTests`, `VisualSnapshotTests`, `PlayModeHelpers`).
- **Documentación** (aplicada el 2026-10-09 en la misma rama):
  - 02: cabecera, §2 (el estado `INTRO(llenar_y_tenir)`) y §4.2;
  - 07: cabecera, §2.2 (regla 5), §5.1, §6.3, §6.4 (nueva), §9.2, «Desviaciones» (3) y «Preguntas abiertas» 11;
  - 00: cabecera, §1.2, §3.3, §5.3 (alias), §6.4, §10 (`fauna_sighted_card`) y §11 (fauna);
  - 08: cabecera, §5.3 y el nombre en la columna «Uso» de §9, sin claves nuevas;
  - GDD.md §2 (decisión 22);
  - la guía visual v2 (revisión 5, tabla de capas, exclusión del panel, 6.11 y C9);
  - este registro;
  - solo el nombre en 01, 06, 09 y en el resto de GDD.md, 02, 07 y 08; en 04 §2.7, el nombre y una nota que remite a Q17.
  - Los históricos y el material de apoyo no se tocan: `docs/research/`, `docs/reviews/` y `docs/dev/prototipo-v0.md`.
- **Pendientes de documentación** (necesitan una respuesta del dueño o del asesor):
  - 06 §4.6 y el glosario §3.3: la página Fauna del Cuaderno de campo con los miquitos (F7; decidida el 2026-10-19, D-029: una nota en los dos hasta que lo dé el asesor);
  - 08 §8.2: `af_fauna_sighted` (Q11; hecho el 2026-10-19, D-029);
  - 07 §11: los días de arte de los miquitos y las frutas;
  - 07 §2.2: la columna «Fauna de ambiente» con la especie y los animales de ambiente de cada mundo (F9);
  - 07 §6.2: si Rosa salta o solo asoma (F11; decidida el 2026-10-19, D-029: una nota hasta que lo dé el asesor);
  - 04 §2.7 y el glosario §3.3: el hoatzin del mundo 11 (Q17; hecho el 2026-10-19, D-029: solo el vaciado);
  - la línea de decisiones de `CLAUDE.md`;
  - los comentarios «pendiente de D-009» del código.

**Preguntas para el dueño.** No bloquean la rama. Q1-Q16 son las del guion (§18), con su numeración; Q17 la añade este registro.

| # | Pregunta | Recomendación | Estado en el código | Bloquea |
|---|---|---|---|---|
| Q1 | Nombre de la animación | «Llenar y teñir» | Aplicada (y en el GDD) | — |
| Q2 | Cómo entra el agua | El río crece, desborda, corre por la madera y cae un chorrito en cada cuia | Aplicada | — |
| Q3 | ¿Se quita la caída de las cuias? | Sí | Aplicada | — |
| Q4 | Orden del teñido dentro de la cuia | De abajo arriba, con las filas de arriba abajo | Aplicada (02, 07 y 08 alineados) | — |
| Q5 | ¿Una fruta por racha o por dosis? | Por racha, como mucho 24; si no, una por cuia con cascada | Aplicada | — |
| Q6 | ¿Qué sueltan los miquitos? | La fruta del tinte; hoja en el Río Negro; gotas en el Tapajós; semillitas rojas solo en el achiote | Aplicada | — |
| Q7 | ¿Qué miquitos van apareciendo? | Géneros distintos por mundo (07 §6.4) | Aplicada | Encargo de arte |
| Q8 | Especie de los mundos 8-9 (421-700) | Maquisapa (*Ateles chamek*, EN), tras confirmar su presencia en el Madidi y su nombre por país. Alternativa: fraile (*Saimiri boliviensis*), del mismo género que el mono del nivel 1 | Aplicada (maquisapa en el catálogo) | No bloquea la 1.0 si Q9 es A |
| Q9 | ¿Cuántas especies en la 1.0, y qué pasa sin arte? | Las especies 1-5. Un mundo sin arte final va en modo dosel, nunca con la especie de otro | Aplicada la regla del modo dosel; las especies de la 1.0 dependen del encargo | Encargo al artista (unos 30 días [E]) |
| Q10 | Texto de la tarjeta | «¡Miquito a la vista!» | Aplicada | Texto final con el asesor |
| Q11 | ¿Cuándo se registra el avistamiento? | `af_fauna_sighted` al mostrar la tarjeta, desde la 1.0 | **No aplicada**: la tarjeta sale, pero no se guarda ni se emite nada. Exige tu aprobación del evento (08 §8.2) y una migración del perfil, en un PR aparte. **Decidida el 2026-10-19: sí (D-029)**; tarea DC15 | Cuaderno de campo (1.1) y el puente en el Mapa del río |
| Q12 | Duración del nivel 1 | 1.400 ms como el resto de 1-20; medir primero. Solo si la prueba de cinco personas lo pide, una versión de estreno de 1.800 ms con una clave nueva | Aplicada (1.400 ms) | — |
| Q13 | Reducir movimiento con especie | El miquito aparece quieto durante el fundido y 600 ms más | Aplicada en la ruta (el ajuste aún no existe) | — |
| Q14 | Agua negra (mundos 3 y 10) | Ámbar-té translúcido que mantenga los 29 puntos de L* frente al tinte 14, tras una captura de prueba | **No aplicada**: el preset sigue en #4A2C14 al 70 %. **Decidida el 2026-10-19: sí (D-029)**; hecha en DC13: #522500 al 70 % | Antes del mundo 3 (nivel 41) |
| Q15 | Estilizaciones de color y forma (bacaba rosa, guanábana, lulo, icono bol del açaí) | Mantener el canon como estilización, sin Datos del río que la afirmen; en vuelo, ramita de açaí. Revisar los tintes con el asesor para el Cuaderno | Aplicada | — |
| Q16 | Técnica de animación de los miquitos | Rig recortable en uGUI: piezas sobre una plantilla por esqueleto y animación por código | Aplicada (rig procedural de 8 piezas) | Encargo al artista y plantillas en `docs/art/` |
| Q17 | En el mundo 11, el hoatzin hace la animación de Volver a la orilla (glosario §3.3; 04 §2.7: «la animación la ejecuta el hoatzin»). ¿Solo vacía las cuias, o también suelta la fruta en la intro corta del reintento? | Solo el vaciado. La intro del reintento va en modo dosel, sin tropa, como en todos los mundos | Sin hoatzin en el código. **Decidida el 2026-10-19: solo el vaciado (D-029)** | Mundo 11 (nivel 821) |

**Preguntas de la propuesta de fauna** (material de apoyo; sección f). Las que el guion no resuelve (decididas el 2026-10-19 con su recomendación, D-029):

| # | Pregunta | Recomendación de la propuesta | Estado |
|---|---|---|---|
| F7 | ¿Se amplían el glosario §3.3 y 06 §4.6 con la página Fauna del Cuaderno de campo («Miquitos» y «Río y selva»)? | Sí, tras el asesor | **Decidida el 2026-10-19: sí, tras el asesor (D-029).** 06 §4.6 sigue con 12 animales y una nota hasta entonces |
| F9 | ¿Microinteracciones al tocar la fauna del fondo (como mucho 2 animales por mundo, fuera del panel, sin dar nada económico)? | Sí, midiendo que no aumenten los toques erróneos en las cuias | **Decidida el 2026-10-19: sí (D-029).** Al backlog: la especifica el tech-lead con la fauna del fondo |
| F10 | ¿La fauna reacciona al sellar una cuia? | No: solo en la victoria y los hitos (prudencia con D-002) | **Decidida el 2026-10-19: no (D-029)** |
| F11 | ¿Quién salta en las celebraciones? | El tucuxi; Rosa asoma y exhala, como el boto real. Cambia 07 §6.2, con el asesor | **Decidida el 2026-10-19 (D-029)**, con el asesor: nota en 07 §6.2; el código, cuando lo dé |
| F13 | ¿Algo nuevo a mitad de los mundos largos (6-11)? | Un segundo gesto de la misma especie en la 1.1 y, después, invitados opcionales | **Decidida el 2026-10-19 (D-029)**: en la 1.1 (09 §1.2). La variación determinista por nivel ya alterna el lado de entrada, quién lidera y el reposo |

Las demás preguntas de la propuesta quedan cubiertas por el guion:
- F1 es Q7, y F2 es Q6.
- F3 y F4: tarjeta tras la intro, en el siguiente nivel libre, de 2,5 s y sin bloquear. *Nota del 2026-10-12: D-011 cambia la duración (por lectura, 3-7 s [P]; punto 2) y el momento (cuando la tropa se ha ido; punto 3).*
- F5: las especies amenazadas entran, con sus reglas. Pendiente de confirmación como Q7.
- F6: sin miquitos en el mundo 13.
- F8 es Q11, y F12 es Q9.
- F14: el churuco en el mundo 4.

**Ampliación del 2026-10-09: elenco de unas 50 especies que tiñen.** Decisión del dueño, registrada el 2026-10-11 (traspaso §6, paso 3).
- **Ritmo:** una especie nueva cada 20 niveles (≈ 50 en 1-1.000), coordinada con los estrenos de mecánicas (D-007) para que no coincidan dos novedades en el mismo nivel.
- **Quién tiñe:** monos, aves y otros animales que de verdad dejan caer fruta (tucanes, guacamayos, oropéndolas, murciélagos fruteros, kinkajú…), no solo miquitos.
- **Cómo:** cada tipo con su forma de soltar la fruta (desde la rama, en vuelo, colgado, de noche), siempre en el mundo donde vive la especie.
- **Qué cambia de esta entrada cuando se diseñe el elenco:** el punto 4 (una especie de miquito por mundo, en 11 tramos) pasa a una especie nueva cada 20 niveles, y los tres modelos de suelta (`fila`, `cuelga` y `sacude`) tendrán que cubrir las formas de soltar de las aves y de los demás animales.
- **Qué no cambia:** T (1.400 / 800 ms) y las fases a-d de la intro (la entrada, el reposo y la salida de la tropa los fija D-011, punto 3), «nunca la especie de otro mundo», el modo dosel, las reglas duras de representación (punto 6), las especies como datos sin Remote Config y D-002.
- **Pendiente:**
  - el diseño del elenco (`docs/research/fauna/elenco-tinteros.md`), con el guion del equipo `docs/dev/traspaso/workflow-elenco-50-especies.js` (investigación, diseño, verificación biológica y técnica, implementación, revisión y documentación), que se lanza después de la integración en Unity;
  - hasta entonces sigue el catálogo `miquitos_v0.json` (10 especies y una variante);
  - la tarjeta «¡Miquito a la vista!» y la palabra «miquitos» no nombran aves ni murciélagos: su texto lo fijan el diseño del elenco y el asesor, no esta entrada;
  - cada especie, su nombre común y su forma de soltar la fruta, pendientes del asesor cultural (07 §8.2).

---

## D-011 · Ajustes de la prueba en el teléfono: la victoria se cobra cuando se ve, avisos legibles, miquitos que se ven y Cosecha doble que avanza

- **Fecha:** 2026-10-11.
- **Estado:** decidida por el dueño tras probar el `dev.apk` en el SM-S938B (compilado en el portátil sobre `80d37fb` o antes; las cuatro causas seguían en `7677085`). El punto 3 lo eligió entre tres opciones cerradas («Más presencia sin frenar», la recomendada). Tareas P1-P4 de `docs/dev/traspaso/pruebas-telefono-2026-10-11.md`.
  - *Actualización del 2026-10-12:* los puntos 1 y 4 están implementados en la tanda 1 (A1, integrada en `3035081`) y el punto 3, también (B1, `d8b9ca3`), salvo que la tarjeta «¡Miquito a la vista!» espere a que la tropa se vaya: B1 deja a la vista ese estado (`IntroAnimation.TroopVisible` y `TroopGoneMs`) y la tarjeta lo usa en la tanda 2. El punto 2 (avisos legibles) está en curso en la tanda 2 (A2). Toda la tanda 1 está en `7d755c4`, sin probar todavía en el teléfono.
  - Documentación aplicada el 2026-10-12 (paso 2 del traspaso): 02 §2, §4.2, §6 y §14; 04 §2.2; 05 §6.1; 06 §4.7; 07 §5.1, §5.4 y §6.4; 08 §5.3 y §9 (columna «Uso» de `victory_autonext_s`); GDD.md §2 #22; la nota de F3 y F4 de la tabla de D-010 y el guion de la intro (nota de cabecera, §4.5, §7.2, §7.3, §14 y los criterios A3, A8 y A15). Los [P] de la tanda 1 que tiene que ratificar el dueño están en la pregunta 38 de GDD.md §6.3.
  - *Actualización del 2026-10-13:* **todos los puntos están implementados.**
    - La tanda 1 (puntos 1, 3 y 4) se probó en el teléfono el 2026-10-12, sobre `7d755c4`, y cumple: P1, P3 y P4 de `docs/dev/traspaso/pruebas-telefono-2026-10-12.md`.
    - El punto 2 y la tarjeta del miquito tras la tropa (punto 3) llegaron con la tanda 2: A2, integrada en `35301f9`; la tanda entera está en `94905b8`. Falta probarlos en el teléfono (`docs/dev/traspaso/README.md`, «Qué probar en el teléfono (tanda 2)»).
    - **Duración por lectura:** `NoticeTiming.ReadMs` (AF.GameFlow, sin asignaciones) da 1.000 ms + 55 ms por carácter, entre 3.000 y 7.000 ms. Sustituye a `GameScreen.ToastMs` y a `SightingRule.CardMs`, que desaparecen. El regalo de Capi dura 5.455 ms y la tarjeta del mono ardilla, 4.960.
    - **Cola:** el aviso y la tarjeta comparten una cola FIFO de 4 huecos preasignados (`GameScreen.Notices.cs`). Solo sale algo en PLAYING, sin victoria ni bloqueo pendientes. Por eso esperan durante la carga, la tarjeta previa, el banner de mundo, la intro (con «El río bajó») y los modales del embudo. La cola no repite un texto que ya espera. Se vacía al cargar un nivel, al reintentar y al empezar la celebración. La capa de avisos va delante del tablero y del HUD, pero detrás de la capa de vertido, de los modales y de la pausa.
    - **Tarjeta del miquito:** queda pendiente hasta que la tropa se va (`IntroAnimation.TroopVisible`) y entonces entra en la cola. Mientras espera, las etiquetas del HUD siguen a la vista.
    - El cartel «El río bajó» y el rótulo del intento siguen fuera de la cola, con su propia duración.
    - **T-F.1:** la lista única de estrenos de 02 §9.1 está en el código como `SightingRule.MechanicDebutLevels`. Los niveles ocupados para la tarjeta (`MilestoneLevels`) se arman con ella, así que ya incluyen el 100 y el 163.
    - Lo que la tanda 2 deja al dueño está en las preguntas 41 y 42 de GDD.md §6.3: la cola no se vacía al volver de segundo plano, la tarjeta se pierde si se gana antes de que la tropa se vaya, y el aviso que ya se ve al abrirse un modal del embudo queda detrás de él.
  - *Actualización del 2026-10-13 (T50):* la tanda 2 se probó en el teléfono sobre `72a266a` y cumple (`docs/dev/traspaso/pruebas-telefono-2026-10-13.md`, punto 1: el regalo de Capi en el 8 dura ≈ 5,45 s y sale con `ready`; la tarjeta del 5 espera a que se vaya la tropa y dura ≈ 4,9 s; los avisos salen de uno en uno). «Nada encima de la victoria» no se puede forzar en el teléfono y lo cubre un test PlayMode. Las preguntas 41 y 42 quedaron decididas ese día (D-015): se ratifican los [P] de los avisos; el aviso que tapa la burbuja «Te faltan {n}» (41, punto 4) y el aviso que se oculta al abrirse un modal del embudo (42, opción b) van en T52.

**Contexto** (palabras del dueño, con la ortografía corregida):
- «Las semillas se suman antes de dar por ganador el juego […] deberían verse luego de terminar completamente y ahí sumarlo […] pareciera que no lo suma, que ya tenía eso. Pasa lo mismo con las ayudas, que desaparecen: aunque se sabe que se va a ganar, no es bueno darlo ya por hecho; debe ser hasta que ocurra.»
- «Los letreros que muestra salen y duran muy poco, no se alcanzan a leer: en el nivel 8 salió algo que ni me dio tiempo de leer, como también en un nivel anterior salió algo de monito que no leí todo.»
- «Los miquitos pueden tirar rápido las semillas al inicio del tablero, pero pueden demorar un poco más en entrar para verlos bien; también duran poco y no se aprecian bien.»
- «Cuando me pide que puedo ganar el doble por ver anuncio, cuando selecciono y veo el anuncio, debería ahí ya haber avanzado de nivel, pero pasa que veo el anuncio y luego vuelvo a Siguiente.»

**Decisión.**
1. **Nada se da por ganado antes de que se vea.**
   - La Cosecha se sigue acreditando en el instante lógico de la victoria, con el progreso y en la misma escritura del perfil, para no perderla si se cierra la app (02 §14).
   - Lo que cambia es la vista:
     - el saldo de arriba no la enseña hasta que la cuenta la fila de la Cosecha en la pantalla de victoria: ni durante el último vertido ni durante la celebración;
     - la bandeja de comodines se queda como estaba mientras termina el último vertido, y se apaga al empezar la celebración.
2. **Avisos legibles.**
   - Los avisos del tablero y la tarjeta «¡Miquito a la vista!» duran según lo que hay que leer: **1.000 ms + 55 ms por carácter visible, entre 3.000 y 7.000 ms** [P].
     - Ejemplos: el regalo de Capi (81 caracteres) dura ≈ 5,5 s; la tarjeta (título, nombre y dato) dura ≈ 5,5-6 s.
     - Sustituye a los 2,6 s de los avisos y a los 2,5 s de la tarjeta (D-010, punto 5; 07 §6.4; guion de la intro §4.5 y A15).
   - **Ningún aviso sale durante la carga, la tarjeta previa ni la intro:** espera a que se habilite el toque. Hoy el regalo de Capi del nivel 8 sale al cargar, encima de la intro y de los miquitos.
   - **Uno cada vez:** si coinciden dos (el regalo de Capi y la tarjeta del miquito, por ejemplo), el segundo espera a que se vaya el primero.
   - Siguen sin capturar toques.
   - El cartel «El río bajó» no cambia (2,0 s, 04 §2.7): alargarlo retrasaría cada reintento.
3. **Miquitos con más presencia, sin frenar el juego.**
   - **Entrada:** entran trepando por la rama en ≈ 500 ms mientras el río llena las cuias, también en la versión corta (desde el 21, primer intento), donde hoy ya están posados en t = 0.
   - **Suelta y T:** la fruta cae igual de rápido. T (1.400 / 800 ms), las fases a-d y el momento en que se habilita el toque no cambian.
   - **Reposo:** tras soltar se quedan ≈ 1.500 ms en la rama con un gesto natural de reposo mientras el jugador ya juega:
     - por ejemplo, comer la fruta, rascarse, mirar alrededor o balancear la cola;
     - el gesto rota de forma determinista por nivel, sobre la variación por nivel que ya existe (F13);
     - después salen trepando en ≈ 500 ms (hoy 300 ms).
   - **Reglas que se mantienen:** D-010 punto 6 (sin rasgos humanos: nada de saludar ni señalar) y D-002 (no miran, no señalan y no reaccionan a una cuia, a la selección ni a un vertido). La especie sigue cambiando qué se ve, nunca cuánto dura.
   - **Al saltar la intro** con un toque, la tropa se va como hoy. Al cargar otro nivel, desaparece.
   - **La tarjeta «¡Miquito a la vista!»** ocupa la misma franja que la rama: sale cuando la tropa se ha ido.
   - Sin tropa (modo dosel: reintentos, 3 filas sin franja, mundo 13) no cambia nada.
4. **La Cosecha doble avanza sola.**
   - Tras ver entero el vídeo de `rv_double_harvest`:
     - la fila cuenta de +N a +2N (400 ms) y las semillas llegan al saldo;
     - se ve ≈ 600 ms más;
     - el juego pasa solo al nivel siguiente por el mismo camino que «Siguiente»: AD_GATE, y después «El río sigue creciendo» o Sin canoas, como hoy.
   - En AD_GATE no sale intersticial: `AdPolicy` lo bloquea 60 s después de un recompensado (`AfterRewarded`).
   - Si el vídeo se cierra antes de tiempo (sin recompensa), se queda en la victoria como hoy.
   - No contradice `victory_autonext_s` = 0 (02 §2): no es un temporizador, sino la respuesta al toque del jugador.

**Consecuencias.**
- **Código** (todo en Unity; núcleo, solver, packs y Remote Config sin cambios): tareas P1-P4 de la hoja de pruebas, con sus pruebas EditMode y PlayMode.
- **Tiempos nuevos sin clave de Remote Config** [P]: duración de los avisos por lectura, entrada, reposo y salida de la tropa, y espera de la Cosecha doble. Van como constantes, igual que los de D-010.
- **GDD y guion** (tarea de documentación de la hoja):
  - 02 §2 y §6: tras la Cosecha doble, se avanza solo;
  - 07 §6.4: duración de la tarjeta, y la tropa que se queda en reposo;
  - el guion de la intro: §4.5, §7.1-§7.3 y A15;
  - la nota de la tabla de D-010 (F3 y F4: «de 2,5 s»).

**Alternativas descartadas.**
- **Acreditar la Cosecha al terminar la celebración:** se perdería si se cierra la app durante ella (02 §14).
- **Intro más larga para los miquitos:** frena cada nivel (opción que el dueño no eligió).
- **Miquitos que crecen con el progreso:** los primeros niveles se quedarían cortos, que es donde el dueño lo notó (tampoco la eligió).
- **Avisos que se cierran con un toque:** los avisos no capturan toques para no robar vertidos.

**Cómo revertir.** Volver a las constantes de antes (`GameScreen.ToastMs` = 2.600, `SightingRule.CardMs` = 2.500, `IntroChoreography.ExitMs` = 300 y la tropa posada en t = 0 en la versión corta), quitar la espera del saldo y de la bandeja y el avance tras la Cosecha doble. Desde la tanda 2, las dos primeras constantes ya no existen: revertir el punto 2 es volver a una duración fija en lugar de `NoticeTiming.ReadMs` y quitar la cola de `GameScreen.Notices.cs`. Siempre con una decisión nueva que sustituya a D-011.

**Archivos afectados** (previstos; la hoja de pruebas los detalla): `Assets/_Project/Scripts/GameFlow/{LevelFlow.cs, SightingRule.cs}`, `Assets/_Project/Scripts/UI/{GameScreen.cs, BoosterTray.cs, Toast.cs, SightingCard.cs, GameBootstrap.cs}`, `Assets/_Project/Scripts/Presentation/{IntroChoreography.cs, TroopLayer.cs, IntroAnimation.cs, MonkeyRig.cs}`, sus tests y la documentación de arriba. En la tanda 2 (A2, `35301f9`), además: `Assets/_Project/Scripts/GameFlow/NoticeTiming.cs`, `Assets/_Project/Scripts/UI/{GameScreen.Notices.cs, GameScreen.Labels.cs, GameScreen.Reveal.cs, PauseMenu.cs, QuitConfirm.cs, GiveUpConfirm.cs, LevelFailedScreen.cs, NoCanoesScreen.cs}` (en los cinco últimos, solo un acceso de solo lectura para los tests) y sus tests (`NoticeTimingTests`, `SightingRuleTests`, `NoticeTests` y `NoticeSnapshotTests`, entre otros). El estado se puso al día el 2026-10-13 en este registro, `docs/GDD.md` §6.3, 02 §4.2, 07 §6.4 y `docs/dev/traspaso/README.md`.

---

## D-012 · Prueba del 2026-10-09: estreno de la Piraña, banner de mundo, sacudida, patrones, guardado por vertido y marco del panel

- **Fecha:** 2026-10-09 (seis decisiones del dueño sobre la prueba de `80d37fb` en el teléfono, recogidas en la hoja de pruebas de ese día) y 2026-10-11 (sus respuestas a tres preguntas de la auditoría del paso 1b: (a) banner de mundo, (b) Remolino y (c) guardado). Registrada el 2026-10-12. Lleva el número 12 porque D-011 se registró antes.
- **Estado:** decidida por el dueño e implementada entera (estado puesto al día el 2026-10-13).
  - **Tanda 1** (integrada en `7d755c4`): la parte C de la decisión 1 (mordisco fallido visible; tarea B2), la respuesta (b) (marca de pareja del Remolino; B2) y la decisión 5 con la respuesta (c) (guardado por vertido con un escritor en segundo plano; C2, `060b518`).
    - Se probó en el teléfono el 2026-10-12 y cumple (`docs/dev/traspaso/pruebas-telefono-2026-10-12.md`: T-D y los overlays del 103 y el 183).
    - Del objetivo de la decisión 5 se midieron 52 vertidos de 3 niveles (el 143, el 43 y el 183; el objetivo pide ≥ 200). El p95 de `save_ms` fue de 0,29 ms sin los vertidos que ganan y de 2,15 ms con ellos, y el máximo, de 3,67 ms: ninguno pasó de 8 ms. Un force-stop a mitad del 43 dejó al reabrir el tablero del último vertido.
    - Sin probar en el teléfono: la Piraña con la cueva llena (observación 4 de esa hoja).
  - **Tanda 2** (integrada en `94905b8`), sin probar aún en el teléfono:
    - la decisión 3 (sacudida) y la decisión 4 (patrones e iconos), con B3 (`dd208a6`);
    - la parte D de la decisión 1 (tutorial con mordisco animado en la tarjeta del 183) y la decisión 2 con la respuesta (a) (banner de mundo), con A4 (`94905b8`).
    - Qué probar: `docs/dev/traspaso/README.md`, «Qué probar en el teléfono (tanda 2)».
  - La decisión 6 no cambia nada.
  - *Actualización del 2026-10-13 (T50):* la tanda 2 se probó en el teléfono el 2026-10-13 sobre `72a266a` y cumple (`docs/dev/traspaso/pruebas-telefono-2026-10-13.md`: la sacudida, los patrones e iconos, el banner de mundo en el 11, el 41, el 81 y el 141 con `main_ms` ≤ 2,3 ms y `fallback` `none`, y el tutorial de la Piraña). La medida de T-D llegó a 216 vertidos de 14 niveles: ninguno pasa de 8 ms en el hilo principal. Las preguntas 39, 43 y 44 quedaron decididas ese día (D-015), y **D-013 cambia la decisión 2**: con Q4-D, el banner trae dentro el viaje de mundo (≤ 4 s, saltable, con «Ver mapa»); lo demás de la decisión 2 sigue igual, también que lo presente Capi mientras Rosa asoma.
  - Documentación aplicada el 2026-10-12 (paso 2 del traspaso; «Archivos afectados»).
  - **El texto nuevo del tutorial de la Piraña y los nombres de mundos, países y ríos del banner quedan pendientes de revisión del asesor cultural** (07 §8.2).
- **Sustituye:**
  - la sacudida de rechazo de ±6 dp, 2 ciclos y 120 ms (contrato 24; GDD.md §2 #3 y §7.2 #30, 02 §3.1 y §4.1, 07 §4.3): pasa a ±8 dp, 3 ciclos y 240 ms (punto 3);
  - el banner de mundo «tras Siguiente… con Rosa guiando» (02 §6) y el banner del mundo 2 en el Mapa del río tras la victoria del 10 (06 §2.2 y §2.3): sale al cargar el primer nivel del mundo, antes de la intro, y lo presenta Capi (punto 2 y respuesta (a));
  - el tutorial del estreno de la Piraña como texto con el icono (02 §9.1 M7): enseña un mordisco animado (punto 1, D);
  - 02 §2, SUSPENDED («el estado ya está en disco tras cada vertido»), 02 §14 («el estado se guardó tras cada vertido») y 08 §11 (guardado solo al terminar el nivel, en la cartera y al pasar a segundo plano): el vertido entrega el perfil al escritor y se escribe en síncrono al pasar a segundo plano (decisión 5);
  - 02 §12: el icono pasa de «60 % del ancho de la dosis» a 0,45 u de alto, como 07 §3 (decisión 4).
- **No sustituye:**
  - las reglas de la Piraña (02 §9.1 M7, con Q-C y Q18 B de D-007) ni los niveles de la campaña: no se regeneran;
  - el texto del banner (`world_banner`, glosario §10) ni la tabla de 08 §9: no hay claves de Remote Config nuevas;
  - la opacidad del patrón y el nivel de 07 §3 y 08 §9 (`ui_pattern_opacity` = 0,20; `ui_patterns_default_from_level` = 41): la decisión 4 es «como el GDD»;
  - D-002 (nada se resalta al seleccionar, tampoco la marca de pareja), D-008 (se guarda tras cada vertido) ni la escritura atómica de 08 §11 (`.tmp`, `Flush(true)` y `File.Replace` con `.bak`);
  - el marco del panel bajo el HUD con 3 filas (guía visual v2 §3.2).

**Contexto.**
- El portátil probó `80d37fb` (olas 1 y 2 del paso 1) en el SM-S938B. La hoja del 2026-10-09 (copiada del mensaje del dueño del 2026-10-11) recoge 9 defectos fuera del plan (T-A a T-G), criterios para las olas 3 y 4, correcciones para el paso 2 y estas seis decisiones.
- Medidas que las motivan:
  - la sacudida de 120 ms y ±6 dp casi no se veía en el teléfono;
  - guardar en cada vertido costaba 3,5 ms de mediana, 5,6 de p95 y 23,2 de máximo (nivel 143), con 5 vertidos por encima de 8 ms, el tope de CPU del hilo principal de 08 §5.4. La causa: `LocalProfileStore.Save` hacía `Flush(true)` y `File.Replace` dentro del toque.
- La auditoría del 2026-10-11 dejó tres preguntas que el dueño respondió ese mismo día:
  - quién presenta el banner, si lo lleva el nivel 1 y si el del mundo 2 sale en el Mapa del río o en el nivel 11;
  - cómo se lee la pareja del Remolino entre filas no contiguas (en el 103, filas 1 y 3): A, canal por detrás del panel; B, la misma marca en las dos; C, las dos cosas;
  - si la tarea del guardado puede tocar `AF.Save` (`.claude/rules/sdk.md`: todo cambio ahí va en un PR aprobado por el dueño).
- Ese día el dueño aprobó también la caché de `LocalDateKey` en `AF.Platform`, aplicada en `7677085` (pregunta 20 de GDD.md §6.3, decidida).
- La presentación del mordisco perdido de Q-C y la marca de pareja del Remolino no estaban en el GDD: D-012 las fija (02 §4.1; 07 §4.3 y §5.6). El borrador del paso 2 (`6d04279`) decía «amaga» y «con varias parejas, unos puntos junto a la bajada las distinguen».

**Decisión.**
1. **Estreno de la Piraña (183): opción C + D.** No se regeneran niveles.
   - **C, mordisco fallido visible** (ola 3): cuando la Piraña no puede morder (Q-C), el cardumen sale de la cueva, llega a la tapa o al Matapi de la víctima, choca y vuelve, en ≈ 400 ms [P]; con la cueva llena, se asoma y vuelve a dormirse.
   - **D, tutorial con un mordisco animado** (ola 4): la tarjeta de estreno del 183 enseña, antes de jugar, cómo la piraña sale de la cueva y se lleva la dosis de la cima de la cuia que acaba de recibir. El texto añade que sellar a tiempo evita el mordisco [P, pendiente del asesor cultural], en 2 líneas como mucho a 360 dp y a 12 sp o más. Con Reducir movimiento, una versión quieta [P].
   - Capítulos: 02 §4.1 y §9.1 (M7), 07 §4.3, §5.6, §7.7 (la tarjeta del 183) y §11.
2. **Banner de mundo: opción A**, en la ola 4. Con la respuesta (a) del 2026-10-11:
   - sale al cargar el primer nivel de cada mundo salvo el Estuario (en la campaña 1-200: 11, 41, 81 y 141); **el nivel 1 no lleva banner**;
   - antes de Llenar y teñir y **solo en el primer intento** de ese nivel: ni al reintentar ni al reanudar; da igual que se entre con Jugar o con Siguiente [P];
   - es una tarjeta simple que se cierra con un toque: «Mundo {n}: {nombre} · {país} · {río}» (`world_banner`, glosario §10), con los nombres del glosario §8;
   - **la presenta Capi con la pata** (`capi_present`) **mientras Rosa asoma** (`rosa_surface`) **y se sumerge** al cerrarla (`rosa_dive`); con Reducir movimiento, quietos [P];
   - si el nivel tiene también tarjeta previa, primero va el banner [P]; los avisos esperan a que termine (D-011, punto 2).
   - Capítulos: 02 §2 y §6, 06 §2.2, §2.3 y §4.5, 07 §2.2 (regla 1) y §5.1; el texto ya está en el glosario §10.
   - El dueño no decidió si la tarjeta lleva la oferta del pack de skin que 05 §5 colgaba del «banner de mundo» desde el 41: pregunta 39 de GDD.md §6.3, citada en 05 §4.1 y §5 y en 06 §4.5.
3. **Sacudida de rechazo: ±8 dp, 3 ciclos y 240 ms** (T-G), sin amortiguar, como antes (seno de amplitud constante). Un toque válido durante la sacudida se atiende igual que antes. El latido del embudo del Matapi la acompaña (240 ms, dentro de los ≤ 300 ms [P] del criterio de la ola 3).
   - Capítulos: GDD.md §2 #3, §7.2 #30 y §9, 02 §3.1 y §4.1, 07 §4.3, 09 §1.1 y la guía visual v2.
4. **Patrones e iconos de fruta: como el GDD** (T-E; 07 §3, 02 §12 y 08 §9):
   - hasta el 40, el patrón del tinte va sutil, al 15 %; desde el 41, Patrones de fruta está activado por defecto: patrón al 20 % e icono de fruta en cada dosis visible, de 0,45 u de alto y del color de la trama del tinte [P];
   - en cualquier nivel, al levantar una cuia, sus dosis visibles muestran el icono, y al bajarla desaparece;
   - nunca lleva icono una dosis oculta, el Barro ni el agua base;
   - el icono es uno por tinte, según la columna «Icono de accesibilidad» de 07 §3, procedural [P] hasta el arte del artista;
   - mientras no exista la pantalla de Ajustes, el interruptor es una constante del cliente, activa desde el 41 [P].
   - Capítulos: GDD.md §2 #5, 02 §12, 07 §3 y 08 §5.1, §5.2 y §9 (en §9, solo la columna «Uso», sin claves nuevas).
5. **Guardado por vertido: se mantiene el objetivo de T-D.** En el hilo principal del teléfono, p95 < 5 ms y ningún guardado > 8 ms, medido en ≥ 200 vertidos de 3 niveles; un force-stop a mitad de nivel deja al reabrir el tablero del último vertido. Con la respuesta (c), la tarea puede tocar `AF.Save` (opción B de T-D):
   - el vertido serializa en el hilo principal (< 1 ms) y entrega los bytes a un único escritor en segundo plano con cola de 1, en la que gana el último;
   - el escritor hace la escritura atómica de 08 §11;
   - al terminar el nivel, en cada mutación de la cartera, al pasar a segundo plano y al cerrar la app, se espera al escritor y se escribe en síncrono;
   - si el escritor falla, el perfil sigue en memoria y se reintenta en el siguiente guardado;
   - `AF_PERF` mide en `save_ms` solo el hilo principal y añade `bg_ms`;
   - sin paquetes nuevos.
   - Capítulos: 02 §2 (SUSPENDED) y §14, 04 §5.2 y 08 §5.4 y §11.
6. **Marco del panel bajo el HUD con 3 filas: se deja como está** (8 dp; guía visual v2 §3.2). Ningún capítulo cambia.
7. **Remolino, respuesta (b) del 2026-10-11: opción C.** El canal une las bases de la pareja, también entre filas no contiguas, y las dos cuias llevan la misma marca de pareja, también cuando el nivel tiene un solo par; con dos pares, las marcas se distinguen entre sí. La marca está siempre a la vista, a ΔE00 ≥ 10 del turquesa de Marea (criterio de la ola 3 de la hoja del 2026-10-09), y nada cambia al seleccionar (D-002). La implementación (B2) la aleja también ΔE00 ≥ 10 de los 14 tintes [P].
   - Capítulos: 07 §4.3.

**Consecuencias.**
- **Código** (todo en Unity; núcleo, solver, packs, esquema y Remote Config sin cambios):
  - tanda 1, integrada en `7d755c4`: B2 (mordisco fallido de ≈ 400 ms, `PiranaOverlay.BiteLostStepMs`; ficha de pareja del Remolino, igual en las dos cuias, con `id + 1` puntos) y C2 (`AF.Save.ProfileWriter`, `ProfileSaveBatch` y los guardados síncronos de `GameBootstrap`, con un tope de espera de 1.000 ms [P]; la especificación de C2 dejó fuera `AF.Platform`, que no se tocó);
  - tanda 2, integrada en `94905b8`:
    - B3 (`dd208a6`), sacudida: `CuiaView.ShakeDp` = 8, `ShakeMs` = 240 y `ShakeCycles` = 3, sin amortiguar, con el latido del embudo del Matapi de 240 ms (`MatapiOverlay.PulseMs`).
    - B3, patrones: `Palette.PatternsDefaultFromLevel` = 41, `PatternOpacityOn` = 0,20 y `PatternOpacitySubtle` = 0,15. Se aplican al cargar el nivel con la propiedad `_PatternOpacity` del material del líquido. `AF_STATE load` lleva `patterns`.
    - B3, iconos: 14 iconos procedurales [P] en un atlas propio (`ProceduralArt.Icons.cs`). Los pinta el shader del líquido en cada dosis visible desde el 41 y en la cuia levantada en cualquier nivel, al 90 % [P] y del color de la trama del tinte.
    - A4 (`94905b8`), banner de mundo: `WorldBanners` (el primer nivel de cada mundo desde el 2: 11, 41, 81 y 141 en la 1.0) y la retención de LEVEL_CARD en `LevelFlow.Cards.cs`.
    - A4, arte del banner: `WorldBannerView` lleva a Capi, Rosa y la viñeta del río, procedurales [P]. Se pintan en un hilo del grupo y se piden antes, en la victoria del 10, el 40, el 80 y el 140 y al ofrecer «Jugar». Si el arte no llega en 800 ms [P], el banner entra sin él.
    - A4, textos [P] pendientes del asesor: los del banner (`UiText.World.cs`) y `UiText.TutorialPirana`.
    - A4, Piraña: la demostración animada de la tarjeta del 183 (`PiranaBiteDemo`).
    - A4, sondas: `world_banner`, `world_banner_art` y `world_banner_closed`.
- **Teléfono:** la tanda 1 se probó el 2026-10-12 (arriba, en «Estado»). Falta ver en el teléfono la sacudida, los patrones e iconos, el banner y el estreno de la Piraña (`docs/dev/traspaso/README.md`, «Qué probar en el teléfono (tanda 2)»).
- **Preguntas abiertas** (`docs/GDD.md` §6.3):
  - el vertido que gana el nivel escribe en síncrono (pregunta 37; en el teléfono tardó 3,25-3,67 ms, así que no pasó de 8 ms);
  - los [P] de la tanda 1, entre ellos el tope de 1.000 ms y el aire de la ficha del Remolino (pregunta 38);
  - el sitio de la oferta del pack de skin ahora que el banner de mundo es una tarjeta simple (pregunta 39);
  - de la tanda 2, el banner de mundo con su arte en otro hilo, su respaldo de 800 ms y los toques que se ignoran mientras espera el arte (pregunta 43);
  - y el shader del líquido, que con los iconos queda aún más por encima del presupuesto de la guía visual y no se ha medido en un teléfono de clase A (pregunta 44).
  - Los 14 iconos de fruta procedurales, su opacidad y su color quedan [P] para el artista y el asesor cultural (07 §3, «Iconos del cliente»).

**Alternativas descartadas.**
- Mantener la sacudida del contrato 24: casi no se veía en el teléfono.
- Remolino: A (solo el canal, por detrás del panel) y B (solo la marca en las dos cuias).
- Banner de mundo: presentado por Rosa, en el nivel 1 o, para el mundo 2, en el Mapa del río tras la victoria del 10 (respuesta (a)).
- Guardado: seguir escribiendo con `Flush(true)` y `File.Replace` dentro del toque.

**Cómo revertir.**
- Sacudida: las constantes de `CuiaView` (`ShakeDp`, `ShakeMs` y `ShakeCycles`).
- Banner: su regla de niveles (`WorldBanners`) y su tarjeta (`WorldBannerView` y `GameScreen.WorldBanner.cs`).
- Piraña: el paso `BiteLost` de `PiranaOverlay` y la demostración de la tarjeta del 183 (`PiranaBiteDemo`).
- Patrones e iconos: las constantes de `Palette`, la propiedad `_PatternOpacity` del material del líquido y el atlas de iconos (`ProceduralArt.Icons.cs`).
- Guardado: volver a la escritura síncrona en `ProfileSaveBatch` (C2).
- Remolino: la ficha de pareja de `RemolinoOverlay`.
- Siempre con una decisión nueva que sustituya a D-012.

**Archivos afectados.**
- **Código** (tanda 1): `Assets/_Project/Scripts/Presentation/Mechanics/{PiranaOverlay.cs, RemolinoOverlay.cs}`, `Assets/_Project/Scripts/Presentation/ProceduralArt.Remolino.cs`, `Assets/_Project/Scripts/Save/{ProfileWriter.cs, LocalProfileStore.cs, IProfileStore.cs}`, `Assets/_Project/Scripts/GameFlow/ProfileSaveBatch.cs`, `Assets/_Project/Scripts/UI/{GameBootstrap.cs, DevProbe.cs}`, sus tests y `docs/dev/prototipo-v0.md`.
- **Código** (tanda 2):
  - B3 (`dd208a6`): `Assets/_Project/Scripts/Presentation/{CuiaView.cs, LiquidGraphic.cs, Palette.cs, BoardAnimator.cs, ProceduralArt.cs, ProceduralArt.Icons.cs}`, `Assets/_Project/Scripts/Presentation/Mechanics/{MatapiOverlay.cs, MurkyMarks.cs}`, `Assets/_Project/Shaders/Resources/AF_UI_Liquid.shader` y `Assets/_Project/Scripts/UI/{GameScreen.cs, DevProbe.cs}`;
  - A4 (`94905b8`): `Assets/_Project/Scripts/GameFlow/{FlowPhase.cs, IWorldBannerListener.cs, LevelFlow.cs, LevelFlow.Cards.cs, SightingRule.cs, WorldBanners.cs}`, `Assets/_Project/Scripts/Presentation/{ProceduralArt.cs, ProceduralArt.Mascots.cs}` y `Assets/_Project/Scripts/UI/{DevProbe.cs, GameBootstrap.cs, GameScreen.Cards.cs, GameScreen.WorldBanner.cs, LaunchOptions.cs, LevelCardView.cs, PiranaBiteDemo.cs, UiText.Cards.cs, UiText.World.cs, WorldBannerView.cs}`;
  - en las dos, sus tests y `docs/dev/prototipo-v0.md`.
- **Documentación** (2026-10-12, paso 2): este registro; `docs/GDD.md` §2, §6.3, §7.2 y §9; `docs/gdd/{00, 02, 04, 05, 06, 07, 08, 09}`; la guía visual v2 (`docs/art/direccion-visual-v2.md`, revisión 6: la sacudida); `docs/dev/traspaso/README.md` (§6 y §7).
- **Documentación del estado** (2026-10-13, tanda 2): este registro; `docs/GDD.md` §6.3; 02 §4.2, §6 y §9.1; 07 §2.2, §3, §6.4 y §7.7; 08 §5.1; la guía visual v2 (revisión 7); `docs/dev/traspaso/README.md`.

---

## D-013 · Mapa del río: remontar el Amazonas hasta donde nace

- **Fecha:** 2026-10-12 (la idea del dueño y su decisión «río arriba», con un fondo por parada) y 2026-10-13 (respuestas a la sección 16 de la propuesta). Registrada el 2026-10-13.
- **Estado:** decidida por el dueño; en implementación en la tanda 4 (estado al día en la tabla de arriba).
  - El 2026-10-13 el dueño aceptó la recomendación de cada pregunta de la sección 16 de `docs/research/mapa-del-rio/propuesta-mapa-del-rio.md`, literal: «Nos vamos por las recomendadas y necesitamos seguir avanzando».
  - Su mensaje hablaba de «las 13 preguntas del Mapa del río», que son las de la primera versión (Q1-Q13). La revisión del 2026-10-13 añadió Q14-Q18, también con su recomendación, y se registran con la misma respuesta. (Entonces, el 2026-10-13) nada estaba implementado: si el dueño prefería otra opción en alguna de ellas, se cambiaba sin coste. Hoy ya hay implementación (ver el estado de D-013 en la tabla).
  - La sección 16 pide contestar a la vez **T1 y T6** de `docs/research/personaje/propuesta-titino.md` y la **Q1** de `docs/research/frutas-del-rio/propuesta-frutas-del-rio.md`. Se toman también con su recomendación: T1-A, T6-A y Q1-A (punto 15).
  - Documentación aplicada el 2026-10-13 (T50, solo `docs/`; «Qué cambia en el GDD»). Un choque entre las propuestas queda como pregunta 45 de `docs/GDD.md` §6.3 (decidida el 2026-10-14 en la (a): D-022).
  - Implementación: fases F1-F9 de la sección 13 de la propuesta, con F9 (paradas de 201-700) por Q14-A. El hito 80 de `tools/level-generator/specs/campaign_v1.json` pasa a «Lago de Tefé» [P] en F1, con su `--check` (es solo metadato: los packs no cambian).
  - **Todo nombre, dato y texto del Mapa queda pendiente del asesor cultural** (07 §8.2). «TiTino» queda además pendiente de la búsqueda de marca del abogado y de la pregunta T4 (abierta).
  - **2026-10-19 (decisión del dueño, al integrar la parte B de la ola 7; tarea FX13), literal:** «Opción 2: que el último tramo (691-700) apunte a la naciente (el Mismi) como meta, sin parada nueva; el fin de contenido hace el viaje final hasta la naciente.» Las paradas siguen siendo las 0-69 (niveles 1-700): la 70 (Jaú, mundo 10) no se añade, porque es de la 1.1 y no tiene datos verificados. Tras Tambo (parada 69, en el cauce), el frente, las 10 boyas y «Faltan N» de 691-700 van a la naciente, el primer punto del cauce de `river_map_v1.json` (la cabecera de HydroRIVERS junto a la quebrada Carhuasanta del nevado Mismi de `rio-amazonas.json`, a unos 3 km), sin tarjeta, sin mini cuia y sin llegada; al ganar el 700, el viaje del fin de contenido lleva la canoa de Tambo a la naciente. `Data/Map/` y `tools/river-map/` no cambian: la meta la deduce el cliente (`RiverMapReader.SourceGoal`). Texto nuevo [P]: `map_source_name` («El nevado Mismi», `pendiente_asesor`; con artículo y «nevado» en minúscula, como los nombres de parada que hacen de {place}), que sale en dos sitios: (1) como {place} de «Faltan N» (`map_stop_remaining`) en el inicio y en el Mapa; (2) como rótulo [P] junto al hito [P] de la naciente (un disco crema con aro azul petróleo en el punto de la naciente), en las vistas Mundo y Cuenca desde la llegada a Tambo (`max_level` ≥ 690) y en el fin de contenido; el hito se desvanece cuando la canoa llega a él. En el fin de contenido del 700, la cabecera dice solo el mundo de la última parada («Ucayali», nunca `world_w10_name`), sin `map_world_question`, que es la de la pregunta del mundo y no se reutiliza con otro sentido; y, sin nada que jugar, el conmutador Mundo/Cuenca va centrado en su fila [P]. Sin número D nuevo.
    - **Pregunta abierta (2026-10-19, revisión de FX13; sin número D nuevo):** en la vista Mundo, la canoa baja en pantalla hacia la naciente en el viaje final. Está en la pregunta 67 de `docs/GDD.md` §6.3, con sus opciones; hasta la respuesta del dueño, el cliente hace la recomendada (a), dejarlo así.
  - **2026-10-20 (pregunta 67 de `docs/GDD.md` §6.3, decidida en la (a); sin número D nuevo):** por la regla del dueño del 2026-10-19 para las preguntas con recomendación (D-029, literal: «si tienen recomendadas vamos con esas»), gana la recomendada (a): en la vista Mundo la canoa baja en pantalla hacia la naciente en el viaje final, fiel al mapa girado (`cw90`), sin girar el contenido del tramo de la meta. Ya aplicada en FX13 (integración C de la ola 7); sin cambios de código ni de datos. Descartadas: (b) girar el contenido en el tramo de la meta y (c) otra orientación.
- **Material:** la propuesta (revisión del 2026-10-13, con 41 hallazgos aplicados) y, en la misma carpeta, `rio-amazonas.json` (geometría, paradas, datos y fuentes), `mapa-muestra.svg` (el Mapa al ganar el 200) y `generar_mapa.py` (genera la muestra y valida el JSON con `--check`).
- **Sustituye:**
  - el Mapa del río de 13 paradas, una por mundo, en el que Rosa marca la posición (glosario §7.1; 06 §4.5): pasa a **100 paradas, una cada 10 niveles** (Q1-A);
  - el nacimiento en el «Marañón alto» (glosario §8, mundo 13; 07 §2.1): pasa al **nevado Mismi**, cabecera del Apurímac, presentado como «una de las nacientes» (Q7-A);
  - un fondo pintado por mundo (07 §2.1 y §11): en 1-200, **un fondo por parada** (Q5-A);
  - «Rosa te lleva río abajo» (glosario §3.2, §5.1 y §5.2; 01; 03 §13; 04 §2.6 y §6.5; 07 §10.4; GDD.md §1): «**río arriba**» (Q13-A);
  - Capi como mascota principal (glosario §3.1; 07 §6.1): **TiTino es el protagonista** y Capi conserva traer cuias (T1-A);
  - el Dato del río «cada 10 niveles, en la pantalla de la Canasta y presentado por Rosa» (glosario §6.4; 06 §4.4): sale en la victoria. Con Q1-A de Frutas, una línea por nivel; en el `10k`, la llegada a la parada, en la placa del Dato del río con el retrato de TiTino. Cómo comparten el hueco: pregunta 45;
  - **D-009, punto 2** (contenido de la tarjeta previa): la del hito p20 añade «Si lo superas, llegas a {sitio}» y la silueta del sitio (02 §10; 07 §7.7);
  - **D-012, decisión 2** (banner de mundo): con Q4-D, el banner del primer nivel de cada mundo trae dentro el viaje de mundo;
  - la fuente de los Datos del río de los mundos 1-5 (07 §2.2, regla 6: «los de la propuesta de D-007, §(e)»): los datos de llegada salen de `river_stops_v1.json`, cruzados con §(e) (Q16-A);
  - las ofertas «en el Mapa del río» (05 §4.1 y §5) y la entrada al Mercado desde el Mapa (04 §4): el Mapa va sin ofertas ni precios (Q10-A);
  - **matiza el contrato 25** (fondos de la 1.0; GDD.md §2 #20, 03 §17 y 08 §6.2): en 201-700 el fondo deja de ser el «río genérico» y pasa a ser el kit del mundo con el rótulo del sitio, sin héroe (punto 2), dentro del AAB mientras no haya Addressables; el «río genérico» queda de respaldo mientras falte el kit o el pack.
- **No sustituye** (sección 15 de la propuesta, comprobado contra el GDD):
  - el orden de los mundos, los packs 1-200, `WorldTable`, las semillas de nivel ni los debuts de tinte;
  - la economía: `tools/econ_sim.py` y `tools/out/econ_*.md` no cambian, y llegar a una parada no da semillas;
  - el alcance de la 1.0 (GDD.md §2 #20-21: niveles 1-700, mundos 1-9; Q14-A);
  - **D-008**: con Q4-D, la cadena corta de 02 §6 (Celebración → victoria → Siguiente → AD_GATE → N+1) no cambia. Solo habría cambiado con Q4-A, que no se eligió (sección 15 de la propuesta);
  - D-004 (panel azul petróleo), D-010 (miquitos) ni D-011 (sus reglas de lectura valen también para el rótulo del lugar);
  - D-012 en todo lo demás: el banner sale al cargar el 11, el 41, el 81 y el 141, solo en el primer intento, y lo presenta Capi mientras Rosa asoma. La tabla f.2 de la propuesta de TiTino lo daba a Rosa; D-012 es posterior y manda.

**Contexto.**
- **La idea del dueño** (2026-10-12, literal en la cabecera de la propuesta): un mapa sencillo pero ilustrativo, con la forma real del Amazonas, que muestre el avance cada 10 tableros, con ciudades y países y datos contados por TiTino; «algo como especie de Mario», único y entretenido.
- **La decisión de ese día:** «sí, entonces hagámoslo como está, río arriba, para llegar al nacimiento del río Amazonas». No cambian ni el orden de los mundos ni los packs.
- **Los fondos** (2026-10-12): «La imagen de fondo del tablero debe reflejar el sitio donde estamos en el mapa; debemos tener una por cada ciudad o sitio que estamos en el mapa, para hacerla más local o que se entienda que se está ahí».
- **Cómo se hizo la propuesta:** tres variantes completas y cuatro jueces. La base es B («tipo Mario»: una parada cada 10, con fondos por sitio), con el núcleo de C (el río de vidrio que se llena con su agua real) y el ancla de A (progreso deducido de `max_level`, victoria con vista, Capi remando y «Norte arriba»). Una revisión del 2026-10-13 aplicó 41 hallazgos, comprobados contra el repositorio y las fuentes.

**Decisión** (respuestas del 2026-10-13; las secciones son las de la propuesta).
1. **El viaje (2026-10-12; Q7-A).** Se remonta el Amazonas del Atlántico a una de sus nacientes, tablero a tablero, en seis actos, con una pregunta por mundo que responde un dato de llegada del mismo mundo (sección 2.1).
   - **Meta (Q7-A):** el nevado Mismi (quebrada Carhuasanta, cabecera del Apurímac), «una de las nacientes»: la más lejana con agua todo el año. Nunca «aquí nace el Amazonas»: la tarjeta del 1.000 cuenta la discusión (el Mantaro, la más lejana según Contos y Tripcevich, 2014, pero que se corta meses al año; el Marañón, la fuente histórica), y el Mantaro sale como mirador.
   - **Final:** al ganar el 1.000, el río despierta: una gota baja del nacimiento a la casa de TiTino en un vuelo de unos 8 s, saltable (sección 2.3). Así se cumple también la idea original del dueño, del nacimiento a la desembocadura, sin invertir la campaña. La vuelta 2 empieza en el Estuario.
2. **Alcance (Q14-A).** La 1.0 sigue siendo la del GDD: niveles 1-700, mundos 1-9.
   - El Mapa de la 1.0 lleva 70 paradas: datos [V] o [E] para k = 20-69 (fase F9, unos 5,5 días de agente), rutas por agua, banderas de Ecuador y Bolivia y los viajes de mundo del 200, el 300, el 420 y el 560.
   - En 201-700 el fondo es el kit del mundo (`bg_kit_w06` … `bg_kit_w09`, unos 8 días de artista) con el rótulo del sitio, sin héroe: **ahí no hay fondo propio por sitio**. Mientras no lleguen los kits o el pack, el «río genérico» del contrato 25, con el rótulo. Si al dueño le pesa, los héroes de esos mundos se piden para la 1.1, con sus packs.
3. **Paradas y cadencia (Q1-A).** La parada `k` (0-99) cubre los niveles `10k+1` … `10k+10`, y se llega a ella al ganar el `10k`.
   - Son 20 paradas en 1-200, 70 en 1-700 y 100 en total, con una boya por nivel. Coincide con la Canasta (06 §4.4) y con los primeros niveles de mundo de `WorldTable`, que son todos `10k+1`.
   - **Tipos:** gran parada (la primera de cada mundo: k = 0, 1, 4, 8 y 14 en 1-200), parada y cierre de mundo (el «castillo», con baliza y la skin como «Premio del mundo · faltan N», sin precio).
   - **Tabla de 1-200** (sección 5.1), **resumen de 201-1.000** (5.4) y **puntos de paso y miradores** (5.2): los miradores los revela el progreso, sin estado nuevo.
4. **El río de vidrio (Q2-A).** El Mapa es la cuenca real en vidrio. Cada victoria llena una décima del tramo con el agua real de ese río (blanca, negra o clara, según Sioli), los afluentes de paso se llenan enteros al pasar su boca y el agua nunca retrocede: ni la Crecida, ni un fallo, ni «El río bajó» la vacían. El vidrio vacío nunca parece sequía: azul hielo con brillo, sin barro ni grietas y sin la palabra «seco» (riesgo R3).
5. **Forma, geometría y países (Q15-A, Q11-A y Q8-A).**
   - **Orientación (Q15-A):** la cuenca va girada 90°, sin deformarla (Andes arriba, Atlántico abajo; 06 §4.5). En la vista Cuenca, el botón «Norte arriba» enseña la forma con el norte arriba, reducida a ≈ 0,76. Dos vistas: Mundo (por defecto) y Cuenca.
     - *Actualización del 2026-10-17 (decisión del dueño, opción A):* con el norte arriba solo salen las placas («Mundo N · Nivel M») de los **3 mundos futuros siguientes** al del jugador (los 3 primeros con número mayor que el actual que estén en los datos; menos si no quedan tantos). Las de los mundos más lejanos se funden con el giro, sin punto, y vuelven en su sitio con la vista de siempre. Motivo: con los datos de 1-700 (mundos 8 y 9) y max 10, las 7 placas de una línea (170-215 dp cada una) no caben en la zona de 328 dp a ≈ 0,76 sin pisarse, cerca de su punto y sin tapar puntos, y con los mundos 10-13 sería peor. No cambian las siluetas de los afluentes ni las placas de la vista Mundo y de la Cuenca girada. Constante `MapConfig.NorthUpFutureWorldPlates` = 3, sin clave de Remote Config.
   - **Geometría (Q11-A):** Natural Earth 1:10m (dominio público) para la vista Cuenca y HydroRIVERS (uso comercial con cita) para la vista Mundo, validadas a ≤ 2 dp de la vista contra `rio-amazonas.json`. **Las descarga el dueño una vez** (o un agente con su visto bueno: descargar es una acción que se aprueba) en `tools/river-map/source/`, con su SHA-256 y la nota de licencia. Mientras llegan, el desarrollo usa `rio-amazonas.json` de respaldo. OpenStreetMap no se usa (licencia ODbL).
     - *Actualización del 2026-10-19 (D-028, pregunta 65):* con la geometría de producción (M5.1), las líneas salen de HydroRIVERS **en las dos vistas**, simplificadas por vista (6 km en la Cuenca y 1 km en la vista Mundo), y de Natural Earth solo salen la tierra y el Casiquiare (parte 2, (a)); la tolerancia de 1 km de la vista Mundo rige a ≤ 100 km de una parada (`MundoZone`, parte 3, (a)); y las excepciones medidas de los puntos de control y de los sitios tierra adentro se aceptan tal como están, con la vía del investigador abierta para quitarlas (parte 1, (d)). Detalle: D-028 y 08 §4.5.
     - *Actualización del 2026-10-19 (D-030, pregunta 1 (a)):* las **paradas en una confluencia** (revisión de M5.1) se anclan en la boca del afluente: si el sitio queda a ≤ 5 km (`CONFLUENCE_KM`) de la boca de un afluente de su línea, la parada va en la `s` de la boca, y al llegar el afluente se llena (Santarém con el Tapajós, Boca Manu con el Manu y el resto de la lista de `tools/river-map/README.md`). Sin [P].
   - **Países (Q8-A):** sin fronteras en tierra. Cada país lleva su nombre en su región, y al entrar en él (y al volver) la canoa iza la bandera de cortesía. Ninguna bandera en tierra, en islas ni en zonas reclamadas (el Esequibo y la isla Santa Rosa). Bolivia, con la tricolor [P]; la bandera de Guyana y el uso de símbolos nacionales los revisan el abogado y el asesor.
6. **Personajes en el Mapa (Q3-A, Q17-A y Q18-A, atadas a T1-A).**
   - **Capi rema la canoa** cargada de cuias atadas con cuerda (`capi_paddle` [P], el bucle de `capi_rescue` reducido, sin pieza nueva). No habla en el Mapa.
   - **Rosa nada delante** y marca el camino; en Tarapoto salta con sus parientes, y en el 961 se despide al pie de los Andes (`rosa_farewell` [P]). **Con Q18-A**, Rosa se queda en los rápidos de Teotônio y en los mundos 7 y 8 guía un **bufeo boliviano** (el mismo rig con otra paleta, sin nombre propio), con un dato real; Rosa vuelve en el viaje del 560. Las dos cosas, con el asesor.
   - **TiTino firma desde su casa**, su embaúba del estuario, y **no viaja**. El dato de cada llegada sale en la **placa del Dato del río con su retrato** y su gesto `sloth_present`, nunca en bocadillo: su bocadillo (≤ 50 caracteres) queda para sus frases propias. Con la cámara lejos, sale en una ventanita de casa con «Casa · a unos N km». Dormita a los 30 s sin toques (`sloth_sleep`, que sustituye a `capi_sleep`).
   - **En el inicio (Q17-A),** TiTino solo sale en la ventanita de casa, sobre el fondo de la parada actual: él se queda en casa y tú estás lejos.
   - Ni Rosa ni TiTino salen en pantallas de pago (propuesta, 6.1, sección 9 y Q10-A; 07 §6.2 y §6.5). Capi conserva el rescate: llega con la Cuia de rescate al modal Aguas estancadas (`capi_rescue`, 07 §5.5).
7. **Cuándo sale solo el Mapa (Q4-D).**
   - **En el 10** (canon de 06 §2.3): el estuario ya lleno, TiTino se presenta y da el Dato del río 1, y la canoa sube a Santarém.
   - **En cada cambio de mundo** (40, 80 y 140; con Q14-A, también 200, 300, 420 y 560), el viaje va **dentro del banner de D-012** del primer nivel del mundo: ≤ 4 s, saltable y con «Ver mapa». Capi presenta, la canoa cruza por agua real en una franja del Mapa, se iza la bandera si cambia de país y la cabecera trae la pregunta del mundo. Sin pantalla nueva: en el 41, el 81 y el 141 no se apilan Mapa, banner y Fruta nueva, y la cadena corta de D-008 no cambia.
   - **Al acabarse el contenido** (700, con Q14-A): viaje hasta el último tramo publicado y la pantalla «El río sigue creciendo» (06 §9).
   - **Al ganar el 1.000:** el río despierta (punto 1).
   - Las demás llegadas, en la victoria (punto 8). Fuera de los hitos se respeta `ui_max_takeovers_per_session` = 1.
   - **Entradas:** el icono del inicio (oculto hasta el 10, con un punto si hay una llegada sin ver), la pausa (el Mapa se abre **encima del nivel en pausa**, «Jugar» pasa a «Volver al nivel», no es salir del nivel y **no gasta canoa**) y «Ver mapa» en la victoria del `10k` y en el banner. «Jugar» siempre abre el siguiente nivel.
8. **La victoria (Q6-A).**
   - **Llegada** (victoria del `10k`): una franja de 1,2 s en el hueco del Dato del río, bajo «Siguiente». El agua termina de llenar el tramo, la canoa llega, se llena la mini cuia del sitio y sale la placa «{lugar} · {país} · río {río}» con, debajo, la placa del dato de llegada con el retrato de TiTino. Un toque la completa.
   - **Demás victorias:** sin línea nueva del Mapa; la barra «Canasta k/10» que ya existe pasa a «Canasta · {sitio} k/10».
   - **Victoria con vista:** tras la ola de saltos, el panel y las cuias se desvanecen en 250 ms y se ve el sitio; en las llegadas, el fondo pasa al sitio nuevo en el mismo fundido. No añade tiempo: «Siguiente» sale a los 1,40 s, como hoy. Con Reducir movimiento, corte directo. Es la forma de ver el sitio en los tableros de 3 filas (34 de 60 en el Trapecio y 48 de 60 en Loreto), donde el panel tapa el horizonte, sin tocar el panel (D-004).
   - **Rótulo del lugar** en el primer nivel de cada parada («Lagos de Tarapoto · Colombia»), con las reglas de lectura de D-011: 1.000 ms + 55 ms por carácter, entre 3 y 7 s, uno cada vez, nunca durante la carga, la tarjeta ni la intro y sin capturar toques. No sale si el nivel tiene banner, Fruta nueva, tarjeta de miquito, estreno o tutorial.
   - **Tarjeta previa del hito p20** (cambia D-009, punto 2): «Si lo superas, llegas a {sitio}», con la silueta del sitio.
9. **Fondos de tablero por parada (Q5-A).**
   - **Capas:** el cielo y la hora por código (07 §2.1), el kit del mundo (un atlas de 8-10 piezas por mundo), un héroe bajo y ancho por sitio (≈ 240 × 80 dp: lo reconocible, en los 30 dp que se ven sobre el horizonte con 2 filas), las piezas por código (las líneas entre aguas de Santarém y del Encuentro) y la LUT (la temporada del ciclo de D-009, Difícil y Muy difícil, atardecer y alba).
   - **En 1-200:** 14 héroes pintados y 1 de reserva, 2 por código y 2 paradas solo con el kit. De los 18 sitios, 8 se reconocen por un hito propio, 2 por la línea entre aguas y 8 son «paisaje del tramo», que se reconocen por el rótulo, el ambiente y la tarjeta.
   - **Coherencia del agua:** el primer plano es siempre el `water_base` del mundo; el agua del sitio, si es otra, va en segundo plano.
   - **Cambio de fondo:** dentro de la carga del nivel `10k+1`, con un fundido de 250 ms y 0 ms más de carga o de intro. El panel azul petróleo (D-004) no cambia.
   - **Antes de encargar el arte,** VA-20 se valida en el teléfono con un héroe provisional (sección 10.6). Criterios nuevos VA-20 a VA-23 en `docs/art/direccion-visual-v2.md` §9.
10. **Coleccionable y economía (Q9-A y Q10-A).**
    - **Muestrario del río:** una mini cuia de vidrio por parada (silueta de D-006, nunca un tubo), llena del agua del sitio y con su tarjeta, que es el fondo de la parada en pequeño. Responde a la pregunta #16 de `docs/research/magic-sort/propuesta-tintalu.md` (Escenas del río). Además: banderas, miradores y Datos del río k/n.
    - **Sin economía nueva:** nada se compra en el Mapa, no hay ofertas, sobres ni azar, y llegar no da semillas. La Mochila de inicio sigue en la victoria (contrato 11). El Pase Expedición (1.2) va al Mercado o a un aviso del inicio sin TiTino a la vista. Al Mercado se entra desde el inicio. La skin sale como «Premio del mundo · faltan N», sin precio.
11. **Datos de llegada (Q16-A).** Salen de la propuesta, cruzados con §(e) de la propuesta de D-007: sus candidatos entran como dato u «Otro dato» de la tarjeta, y sus vetos se aplican (sección 5.1 bis). Van a `river_stops_v1.json`.
    - **Publicación:** solo [V], o [E] revisados por el asesor; ningún [NV]; antes de cerrar un texto se reabre su URL.
    - **Longitud:** el dato, ≤ 90 caracteres; las frases de TiTino y las preguntas, ≤ 50.
    - **Sensibilidad:** «Belén» nunca como nombre de parada, rótulo, nivel ni ranura (la parada 15 es «Río Itaya (Iquitos)»); sin samaúma, sin el mito de Eiffel y sin ruinas; el lago de Tefé, sin botos ni orillas secas (mortandad de 2023); Puerto Nariño, las casas del Itaya, São Gabriel, Shanay-Timpishka y Q'eswachaka, con el asesor.
12. **«¿Verdad o mito?» (Q12-A).** Experimento de la 1.1 detrás de `ui_map_quiz` = `off`; en la 1.0, el dato simple.
13. **Textos (Q13-A).** «Rosa te lleva río arriba, al siguiente nivel» (`stuck_skip`).
14. **Lo que se aprueba con el lote de documentación** (sección 16, «no son preguntas, pero hay que firmarlo»):
    - las 6 claves de Remote Config (08 §9): `ui_map_mode`, `ui_map_autopen`, `ui_bg_per_stop`, `ui_stop_label`, `ui_victory_view` y `anim_map_world_ms`, y `ui_map_quiz` (Q12-A, apagada);
    - los 3 eventos (08 §8.2): `af_map_open`, `af_map_close` y `af_stop_arrive`, y el `fact_id` de `af_river_fact_shown` como clave del dato;
    - el campo `progress.map_seen_level` del perfil (08 §4.3): entero, 0 por defecto, gana el máximo en la fusión de 08 §11; no exige migrar el perfil porque el esquema aún no se ha publicado (`Profile.cs`, `CurrentSchema` = 1);
    - los formatos `af-river-map/1` y `af-river-stops/1`: contratos 29 y 30 (GDD.md §7.4; sintaxis en 08 §4.5);
    - las descargas de Natural Earth y de HydroRIVERS (punto 5);
    - los textos nuevos del glosario §10;
    - y, cuando toque PAD, el paquete Addressables (08 §6.2), que se instala con el visto bueno del dueño en ese momento (regla dura 4). Hasta entonces, todo va en el AAB.
15. **Respuestas atadas** (sección 16 de la propuesta).
    - **T1-A (TiTino):** TiTino es el protagonista: la cara del juego, la victoria, el ánimo en Nivel fallido, el inicio, las notificaciones, la ficha y los anuncios. **Capi conserva traer cuias** (Cuia extra, Cuia de rescate, Canasta y Bitácora) y, con Q3-A, rema la canoa del Mapa. **Rosa, avanzar** (Salto del Boto, guía del Mapa y «El río bajó»). TiTino no viaja y nunca sale en pantallas de pago.
    - **T6-A (TiTino):** una sola línea de texto por victoria. Si coinciden una frase de TiTino y un dato, sale el dato y TiTino solo hace el gesto, salvo en los tres hitos (fin de mundo salvo el nivel 10, primer Difícil y primer Muy difícil), en los que habla TiTino y el dato pasa a la victoria siguiente.
    - **Q1-A (Frutas del río):** la serie de frutas **absorbe el Dato del río**: una línea por nivel en la victoria, con la misma etiqueta («Dato del río · Açaí 3/20») y el mismo evento; sale de la pantalla de la Canasta.
    - **Siguen abiertas** y no se dan por decididas: de TiTino, T2 (cómo entra en el arranque y en el tutorial), T4 (nombre y grafía) y T5 (si aparece durante la partida: tutorial y aviso de Volver a la orilla); de Frutas, Q2-Q7; de la propuesta de idiomas, Q1-Q12; de la tinaja, Q-Ti1-Q-Ti7. Mientras T2 y T5 estén abiertas, el arranque, el tutorial, el aviso de Volver a la orilla y la orilla con Capi del HUD (conflicto C4 de la guía visual) se quedan como están. El resto del lote f.3 de TiTino (el ajuste «Mensajes de TiTino» con la migración del perfil, las claves `ui_sloth_*`, los textos `sloth_line_*` y la hoja de modelo completa) queda para cuando se decidan.
    - *Actualización del 2026-10-14 (T54):* el dueño las decidió el 2026-10-13 con su recomendación («Si»): TiTino T2-A, T4-A y T5-B (D-016), Frutas Q2-Q7 (D-017), idiomas Q1-Q12 (D-018) y la tinaja Q-Ti1-Q-Ti7 (D-019). Con Q4-A de Frutas, la recomendación (a) de la pregunta 45 ya no supone nada abierto, pero la 45 sigue sin respuesta.
    - *Actualización del 2026-10-14 (T58):* la 45 quedó decidida en la (a) (D-022).

**Consecuencias.**
- **Choque entre propuestas** (pregunta 45 de GDD.md §6.3): la llegada del Mapa (secciones 6.2 y 9), la línea de fruta de Frutas Q1-A (§1.3 y §2.2, regla 7) y la excepción de los tres hitos de T6-A quieren el mismo hueco de la victoria del `10k` y no casan del todo: la clave que gobierna cada cadencia, qué pasa con la línea de fruta del `10k`, qué sale en los tres hitos y qué va en el bocadillo del Mapa del 10. Hasta que el dueño decida, el GDD describe los dos canales y remite a la pregunta. La opción recomendada, (a), deja en la ficha de la fruta las líneas que se desplazan (en eso se aparta del Mapa, que las pasa a la victoria siguiente: es la opción (d), que encadena desplazamientos), así que supone la Q4-A de Frutas (ficha en la 1.0), que sigue abierta: conviene contestar las dos a la vez. No bloquea: nada de esto está en el cliente.
  - *Actualización del 2026-10-14 (T58):* decidida en la (a) (D-022, punto 1): canales separados, `ui_river_fact_every_n_levels` = 10 para la llegada y `ui_fruit_line_every_n_levels` [P] para la línea de fruta, y una sola línea por victoria con prioridad (frase de TiTino en sus tres hitos > dato de llegada > línea de fruta); el dato de llegada desplazado sale en la victoria siguiente y la línea de fruta desplazada queda en la ficha. En el 10, la presentación de TiTino en el bocadillo del Mapa y la línea 10 del açaí en la ficha.
- **Código:** nada todavía. El plan de la sección 13 de la propuesta, sin escena nueva, sin prefabs, sin YAML a mano, sin paquetes nuevos, sin shader propio y sin tocar `Packages/com.amazonfill.core/`:
  - F1 (agente de C#): `RiverJourney` y `MapMoments` (AF.GameFlow, sin motor), `RiverStopCatalog` (AF.Levels), `river_stops_v1.json` de 1-200 y el campo `progress.map_seen_level`, con tests EditMode;
  - F2 (herramientas): `tools/river-map/` (Python 3.14, solo biblioteca estándar) con `--check`, que se añadirá a los comandos de verificación de `CLAUDE.md` cuando exista;
    - *Actualización del 2026-10-14 (D-023, punto 32):* los generadores siguen solo con la biblioteca estándar, y las pruebas validan los esquemas del Mapa con `jsonschema`, que ya está en el stack;
  - F3-F6 (`unity-gameplay-dev`, con Editor): fondos por parada (`StopBackdrop`), victoria (franja, barra, «Faltan N», tarjeta del hito y victoria con vista), la pantalla del Mapa (`RiverMapScreen`, `RiverRibbonGraphic`, `LandGraphic` y `PolylineCursor`) y las secuencias;
  - F7 (claves de RC y eventos en logcat), F8 (QA y medida del AAB) y F9 (paradas de 201-700).
- **Coste [E]** (sección 13): unos 25,5 días de agente (unos 19 en la versión mínima) más unos 5,5 de F9; unos +18 días de artista sobre el canon en 1-200 más 8 por los 4 kits de 201-700; calendario de 4-5 semanas con un agente de Editor y de unas 3 con dos (regla dura 7).
- **Peso y memoria:** los fondos de 1-200 ocupan unos 2,4-3,3 MiB y el Mapa, unos 1,7 MiB, menos de lo que 08 §6.1 ya reserva para los mundos 1-4. Todo queda residente (6,4-8,2 MiB), muy por debajo de los 120 MB de texturas de 08 §5.4. Se mide con `ci/size-gate.ps1` tras integrar los 3 primeros sitios, antes de encargar el resto.
- **Guardado:** un solo campo nuevo (`progress.map_seen_level`); todo lo demás (posición, llenado, muestras, miradores, afluentes y banderas) se deduce de `max_level`, `skipped_levels`, `WorldTable` y el catálogo.
- **Dependencias externas:** las descargas de Q11 (dueño); el asesor (datos, sitios marcados, Shanay-Timpishka en el mundo 9, que puede ser parada, vapor lejano o nada, y el bufeo boliviano); el abogado (banderas, Guyana y «TiTino»); el artista (la máscara de zonas del terreno para F2 y los héroes después de VA-20).

**Qué cambia en el GDD** (aplicado el 2026-10-13, T50):
- `docs/GDD.md`: versión 1.7; §1 (el Mapa, TiTino y «río arriba»); §2 #20 (contrato 25, matizado: fondos de 201-700) y #24; §3.1 (índice); §4 (fila «Tema y marca»); §6, §6.2 (fondos de la 1.0) y §6.3 (la 34 apunta a D-014, las 39-44 decididas y la 45 nueva); §7.2 (#18, contrato 25) y §7.4 (contratos 29 y 30); §9 (paso 16).
- 00 glosario: el resumen, §3.1-§3.3 y §3.4 (nueva: TiTino), §5.1-§5.2, §6.4, §7.1, §8 (mundos 9 y 13), §10 (claves del Mapa), §11 (identificadores del Mapa y de TiTino), «Parámetros» y la pregunta abierta 3 (el nombre de TiTino).
- 01: §1 (elevator pitch: TiTino y «río arriba»), §8.3 (el concepto de UA «Rosa te lleva río arriba») y los fondos de 201-700 (§5, diferencial 6, y §11, término «AAB / PAD»).
- 02: §2 (estado de la victoria), §4 (Celebración: TiTino, sin Capi), §5 (pausa y Capi), §6 (llegada, victoria con vista, barra de la Canasta, banner con el viaje, TiTino y una línea por victoria) y §10 (tarjeta del hito p20).
- 03 §13 (el Mapa, sin salir del nivel) y §17 (fondos de 201-700); 04 §2.6, §4 y §6.5 («río arriba»; el Mercado, desde el inicio).
- 05 §4.1 y §5 (la Mochila en la victoria, el Pase fuera del Mapa, la oferta de skin y el marco de los packs de skin).
- 06: §2.2, §2.3, §4.4-§4.7, §8, §9, §10 (fila 0: fondos de la 1.0), «Parámetros» y «Dependencias».
- 07: §2.1 (fondos por parada), §2.2 (reglas 1 y 6; fauna del mundo 4), §5.4, §5.5, §6.1-§6.3 (en 6.1, `capi_point` solo en el tutorial: en la victoria de los hitos celebra TiTino), §6.5 (nueva: TiTino), §7.6, §7.7, §9.1, §10, §11, «Parámetros», «Preguntas abiertas» 2 (fondos de 201-700) y «Dependencias» (fila 09).
- 08: §2.2, §3, §4.3, §4.5 (nueva), §6.1, §6.2 (fila `world_w05` … `world_w13`: los kits de 201-700), §8.2, §9, §11 y §13.1 (comprobación 17).
- 09: §1.1 (#20, #26 y #27), §2.3 (fase 3), §2.4 (F3), §5 (arte), §7 (R34) y §8, decisión 22 (fondos de 201-700).
- `docs/art/direccion-visual-v2.md`: revisión 8, §3.4, §9 (VA-20 a VA-23), §10.3 y §12 (TiTino celebra en el panel de victoria).
- Propuestas marcadas: Mapa (cabecera y sección 16), TiTino (estado, T1, T6 y f.1), Frutas (cabecera, Q1 y los datos k14 y k19 del Tapajós, que hay que cambiar) y `docs/research/magic-sort/propuesta-tintalu.md` (pregunta #16, Escenas del río, respondida).

**Alternativas descartadas.**
- Q1-B (un sitio cada 20) y Q1-C (13 paradas, el canon): se alejan del «cada 10 tableros» del dueño.
- Q4-A (el Mapa en una pantalla aparte tras AD_GATE en los cambios de mundo): apilaba Mapa, banner y Fruta nueva en el 41, el 81 y el 141 y cambiaba D-008. Q4-B (sin Mapa en los cambios de mundo) y Q4-C (en cada parada).
- Q5-B (héroe solo en las 7 ciudades, unos +5,5 días de artista) y Q5-C (un fondo por mundo).
- Q7-B (Marañón alto) y Q7-C (Mismi con corte directo).
- Q8-B (fronteras de Natural Earth) y Q8-C (sin banderas).
- Q10-B (las ofertas del canon en el Mapa), Q12-B y C, Q13-B, Q14-B (1.0 = 1-200), Q15-B (norte arriba por defecto), Q16-B (solo los candidatos de §(e)), Q17-B (inicio en el estuario) y Q18-B (Rosa sigue en los mundos 7 y 8).
- Q2-B (ruta pintada), Q3-B (Rosa narra), Q3-C (TiTino viaja: choca con sus reglas éticas, g.2), Q6-B y C, Q9-B y C, y Q11-B, C y D.
- T1-B (TiTino sustituye a Capi, rescate incluido), T1-C (TiTino solo anfitrión), T6-B (siempre TiTino), T6-C (los dos a la vez: rompe la cadena corta), Frutas Q1-B (conviven) y Q1-C (un dato cada 10).

**Cómo revertir.** Las claves de la sección 11.5 de la propuesta hacen de respaldo sin código aparte: `ui_map_mode` = `worlds` (13 paradas, como filtro del mismo catálogo), `ui_bg_per_stop` = false (el kit del mundo con el héroe de su gran parada en todo el mundo), `ui_map_autopen` = `l10`, `ui_stop_label` = false y `ui_victory_view` = false. Revertir la decisión entera (volver al Mapa de 13 paradas, al Marañón alto o a Capi como mascota principal) exige una decisión nueva que sustituya a D-013.

**Archivos afectados.**
- **Documentación** (2026-10-13, T50): este registro; `docs/GDD.md`; `docs/gdd/{00, 01, 02, 03, 04, 05, 06, 07, 08, 09}`; `docs/art/direccion-visual-v2.md`; `docs/dev/traspaso/README.md`; las propuestas del Mapa, de TiTino, de Frutas del río y de Magic Sort (marcas de lo decidido).
- **Código y datos** (previstos, F1-F9): `Assets/_Project/Data/Map/{river_map_v1.json, river_stops_v1.json}`, `Assets/_Project/Scripts/{GameFlow, Levels, Presentation, UI}`, `tools/river-map/` y `tools/level-generator/specs/campaign_v1.json` (el nombre del hito 80).

---

## D-014 · Aguas estancadas por bucle (R13 ampliada)

- **Fecha:** 2026-10-13. Registrada el 2026-10-13.
- **Estado:** decidida por el dueño: la opción recomendada de la hoja `docs/dev/traspaso/pruebas-telefono-2026-10-13.md` («Defecto encontrado por el dueño: bucle sin "Aguas estancadas" en el 66»), con su «Nos vamos por las recomendadas».
  - Documentación aplicada el 2026-10-13 (T50): 02 §1.4 (R13, R14 y R16), §2 (la transición a STUCK_MODAL) y «Preguntas abiertas» 15 y 16; 04 §5.1 (la condición del cliente en `afterPourAnimation`); 07 §5.5; `docs/GDD.md` §6.3 (preguntas 34 y 46); `docs/dev/traspaso/README.md` y una nota en la hoja `docs/dev/traspaso/pruebas-telefono-2026-10-13.md`.
  - En implementación en el núcleo (T51, `LevelSession`), con el tablero del 66 como prueba de regresión.
  - **Revisión del 2026-10-13 (T50): pendiente de la pregunta 46** de `docs/GDD.md` §6.3. El propio 66 es un contraejemplo: con la regla tal como se decidió, Aguas estancadas saltaría tras el vertido 6, cuando el tablero todavía se gana, y eso rompe R14 (P1), que la hoja de pruebas pedía «intacta» (Consecuencias, «El 66, contraejemplo de P1»). T51 no integra la regla hasta que el dueño responda.
  - **Actualización del 2026-10-14 (T54): la pregunta 46 quedó decidida el 2026-10-13 en la opción (a)**, la recomendada, cuando el dueño aplicó las recomendadas (D-020): el cierre recorre todos los vertidos legales, fútiles incluidos. Así está en `Session.DeadEndProbe` (T51, integrada en `1ad712c`): en el 66, Aguas estancadas sale justo tras el vertido 9 (`1>5`). El punto 1 de la decisión se corrige abajo; la primera redacción («con vertidos útiles») se conserva como historia. Documentación: 02 R13, R14 y R16, «Preguntas abiertas» 15 y 16, y `docs/GDD.md` §6.3 (preguntas 34 y 46).
- **Sustituye:**
  - en 02 R13, la única condición de Aguas estancadas («`movimientos_utiles(S)` vacío»): se añade la del bucle sin salida;
  - en 02 R14, «nunca anuncia "irresoluble" mientras exista un movimiento útil»: pasa a «mientras exista progreso posible (el tablero es resoluble)», que es lo que protege R14 (P1). La garantía no se rebaja: la regla ampliada tiene que cumplirla (pregunta 46).
- **No sustituye:**
  - `futil` del anexo B, Q-B ni Q18 B (los fútiles no avanzan contadores), ni el kill-switch `futile_rule_enabled`;
  - el solver (`AStarSolver`), el bot ×40, el generador, `tools/level-qa` ni los packs, que quedan idénticos byte a byte;
  - el modal Aguas estancadas, el embudo de D-008, R15 ni la definición de fallo de 04 §5.2: el bloqueo por bucle es Aguas estancadas a todos los efectos;
  - P1 (R14) como propiedad que se prueba: todo estado atascado es irresoluble.

**Contexto.**
- **El síntoma** (el dueño, en el teléfono, el 2026-10-13): «hay un error, no hay forma de seguir y queda en un loop».
- **La reproducción:** en el nivel 66, primer intento, los vertidos `3>6 4>3 7>4 0>5 1>5 2>0 2>1 5>2 1>5 2>1 0>7 1>2` dejan este tablero (cuias de 4 salvo la 5, de 2):

  ```
  0 [3,8,10]  1 [4,7]  2 [10,7,7,7]  3 sellada  4 sellada
  5 (cap 2) [4,4]  6 [3]  7 [8,8,10,10]  8 [3,3,8,1]  9 [1,1,1,4]
  ```

- **La causa:**
  - `LevelSession` lo da en `Playing`, con dos vertidos legales, `2>1` y `7>0`;
  - `AStarSolver` lo da por `Unsolvable` tras expandir solo 4 estados;
  - esos dos vertidos son vaivenes parciales (los tres rosas entre la 1 y la 2, el azul entre la 0 y la 7). No son fútiles según R13 y el anexo B, así que `atascado(S)` no se cumple y no sale Aguas estancadas;
  - el jugador da vueltas sin rescate ni rendición: solo le quedan Deshacer y Reiniciar a mano.
- **Es el caso que dejaba abierta la pregunta 15 de 02** (R16): el anexo B («no existe un ciclo de vertidos útiles que vuelva al mismo estado canónico») es falso ya en el juego base (CHANGELOG del núcleo, C1-2).
- **El 66 no es un nivel sin mecánicas** (`m03_v1_n0066` de `Assets/_Project/Data/Levels/mundo_03_v1.json`): tiene **capacidades mixtas**, con una Cuia chica de 2 en la 5 (`caps` = [4, 4, 4, 4, 4, 2, 4, 4, 4, 4]), y una **Hoja** en la 6 (llave: el color 3), que el primer vertido (`3>6`) abre. En el tablero final ya no queda ninguna mecánica activa, pero las capacidades siguen siendo mixtas: es justo la excepción de P2 (02 R16) que había que vigilar. La hoja de pruebas lo daba por «un nivel sin mecánicas activas».
- **Magic Sort** (ficha §2.8) solo declara el bloqueo sin vertidos legales; no está observado si detecta bucles.

**Decisión.**
1. **Regla:** con S en `Playing` según la regla actual, se recorre el cierre de estados alcanzables con **todos los vertidos legales, fútiles incluidos** (pregunta 46, decidida el 2026-10-13: (a); D-020). La primera redacción decía «con vertidos útiles (no fútiles: R13, anexo B, Q-B y Q18 B)», y el 66 mostró que con capacidades mixtas eso declara el bloqueo en tableros que se ganan (Consecuencias). **Si se agota con ≤ 64 estados [P] y ninguno es victoria, S queda en Aguas estancadas. Si pasa de 64, no se declara** (conservador).
2. **Dónde:** solo en la partida del cliente (`LevelSession`, AF.Core), donde hoy se evalúa el estado tras cada cambio. El solver, el bot, el generador y `level-qa` no cambian, y los packs son idénticos.
3. **El 66** queda como ejemplo en 02 R13 y como prueba de regresión del núcleo. Qué tiene que dar esa prueba depende de la pregunta 46 (Consecuencias).

**Consecuencias.**
- **Pregunta 15 de 02 y pregunta 34 de GDD.md §6.3:** queda resuelta **para el jugador**: un vaivén que no lleva a ninguna parte ya no retrasa el modal, tampoco el del Remolino con Q-B si su cierre es pequeño. **La propiedad del anexo B sigue abierta para el diseño:** ratificar P1-P3 en su lugar, con la excepción del Remolino, es la pregunta 34, que sigue pendiente.
- **El 66, contraejemplo de P1** (revisión de T50, 2026-10-13). Se comprobó simulando el pack con las reglas de 02 R9 y de `futil` (anexo B), contrastadas con `CuiaRules.IsFutile` y `HojaMechanic`; los 12 vertidos del dueño reproducen exactamente el tablero final.
  - **Con la regla decidida, Aguas estancadas saltaría tras el vertido 6 (`2>0`), no tras el 12.** El cierre de vertidos útiles ya se agota sin victoria tras el vertido 6 (50 estados), el 7 (22), el 8 (`5>2`, 22) y del 9 al 12 (4).
  - **Pero tras los vertidos 6, 7 y 8 el tablero se gana.** Tras el 8, por ejemplo, con `6>5 2>6 0>2 7>2 0>7 0>5 1>0 1>6 0>1 9>1 8>9 8>7 5>8`, que empieza por un fútil: `6>5` lleva el 3 suelto de la 6, de capacidad 4, a la Cuia chica vacía y libera una cuia de 4. Es la excepción de P2 con capacidades distintas (02 R16). Tras el 6 y el 7 también hay líneas ganadoras que pasan por `6>5`.
  - **El tablero solo es irresoluble a partir del vertido 9 (`1>5`):** desde ahí, el cierre con todos los vertidos legales, fútiles incluidos, tiene 4 estados y ninguno es victoria.
  - **Conclusión:** con capacidades mixtas, «el cierre de vertidos útiles se agota sin victoria» no equivale a «no hay progreso posible», así que la regla decidida rompe R14 (P1), que la hoja de pruebas pedía «intacta». La decisión tiene dos partes que se contradicen en el 66, y el dueño tiene que elegir: **pregunta 46** de GDD.md §6.3.
    - (a) El cierre recorre todos los vertidos legales, fútiles incluidos, con el mismo tope de 64 estados [P]. En el 66 salta justo tras el vertido 9, y P1 se cumple por construcción: un cierre completo sin victoria significa un tablero irresoluble.
    - (b) Solo vertidos útiles, aceptando falsos bloqueos con capacidades mixtas.
    - (c) El bucle, solo con capacidades iguales. No arregla el 66, que tiene capacidades mixtas.
    - **Recomendación: (a).** **Decidida el 2026-10-13: (a)** (D-020).
- **T51 (core-dev)**, que esperaba a la pregunta 46 para integrar (integrada en `1ad712c`, con la opción (a)):
  - lo implementa en `LevelSession`, con búferes fijos para 64 estados y sin asignaciones tras calentar (reglas duras 1 y 2: determinista, sin LINQ ni `new` por nodo);
  - el límite de 64 es una constante [P] del núcleo, no una clave de Remote Config; el coste objetivo es de menos de 1 ms por evaluación en un tablero de 14 cuias, dentro de los 8 ms de CPU del hilo principal de 08 §5.4;
  - pruebas del 66, según la respuesta:
    - con (a), el tablero final da Aguas estancadas y, en la secuencia del dueño, salta por primera vez justo tras el vertido 9 (`1>5`), no antes;
    - con (b), salta tras el vertido 6, y P1 deja de valer para la regla ampliada con capacidades mixtas;
    - con (c), el 66 no da Aguas estancadas;
  - pruebas comunes: un cierre de más de 64 estados no se declara y P1-P3 siguen en verde. Hay además una propiedad nueva, que lleva el 66 como caso: si el cierre da atascado, el solver exhaustivo da irresoluble. Con (a) se cumple por construcción; con (c), la propiedad lo comprueba con capacidades iguales; con (b), solo se exige con capacidades iguales;
  - con Aguas turbias, el núcleo conoce las dosis ocultas y el cierre usa el estado verdadero; los contadores de Lluvia y Piraña forman parte del estado.
- **Cliente y analítica:** sin cambios de pantalla; el modal, sus textos y `af_stuck_shown` son los de siempre.

**Alternativas descartadas.**
- Que R13 use el solver con un presupuesto pequeño (la alternativa de la hoja): depende de la poda y del presupuesto del solver y es más caro de ejecutar en cada vertido sin asignar memoria.
- Dejarlo como está: el jugador da vueltas sin salida y sin poder rendirse.
- Declarar el bloqueo también por encima de 64 estados: podría declararlo con progreso posible, contra R14.

**Cómo revertir.** Quitar la comprobación del cierre en la evaluación de `LevelSession` (T51). Siempre con una decisión nueva que sustituya a D-014.

**Archivos afectados.**
- **Documentación** (2026-10-13, T50): este registro; 02 §1.4 (R13, R14 y R16), §2 (la transición a STUCK_MODAL) y «Preguntas abiertas» 15 y 16; 04 §5.1 (`afterPourAnimation` y una precisión: el solver, el bot y `level-qa` siguen solo con `legalNonFutileMoves`); 07 §5.5; `docs/GDD.md` §6.3 (preguntas 34 y 46); `docs/dev/traspaso/README.md`; una nota en la hoja `docs/dev/traspaso/pruebas-telefono-2026-10-13.md` (el 66 tiene capacidades mixtas). Con la respuesta a la 46 (2026-10-14, T54): este registro, 02 §1.4 (pseudocódigo, R13, R14 y R16) y «Preguntas abiertas» 15 y 16, 04 §5.1, 07 §5.5 y `docs/GDD.md` §6.3.
- **Código** (T51, integrada en `1ad712c`): `Packages/com.amazonfill.core/Runtime/AF.Core/Session/LevelSession.cs`, `Session/DeadEndProbe.cs` (el cierre), sus tests compartidos y de `DotnetOnly`, y el README y el CHANGELOG del núcleo.

---

## D-015 · Respuestas del 2026-10-13 a las preguntas 39-44 de `docs/GDD.md` §6.3

- **Fecha:** 2026-10-13. Registrada el 2026-10-13.
- **Estado:** decidida por el dueño. El escritorio le pasó las preguntas 39-44 («Cada una lleva mi recomendación; si te valen, basta con "acepto las recomendaciones"») y el dueño respondió: «Nos vamos por las recomendadas y necesitamos seguir avanzando».
  - Documentación aplicada el 2026-10-13 (T50): `docs/GDD.md` §6.3 (las seis filas pasan a decididas), 02 §6, 03 §12 (pregunta 40), 05 §4.1, §5 y Dependencias, 06 §4.5, 07 §2.2, `docs/art/direccion-visual-v2.md` (§8, fila Shaders, con la excepción de la pregunta 44, y revisión 9) y este registro.
  - Lo que pide código lo hace **T52** [P]: el aviso que tapa las burbujas «Te faltan {n}» (pregunta 41, punto 4), el aviso que se oculta al abrirse un modal del embudo (42) y la escala de fuente del banner de mundo (43).
    - *Actualización del 2026-10-14 (T54):* T52 está integrada en `d2ce284`. Al abrirse Aguas estancadas, ¿Seguro?, Nivel fallido o Sin canoas, el aviso y la tarjeta del miquito a la vista se ocultan y se descartan, y la cola espera (42). El aviso sube por encima de las burbujas «Te faltan {n}» sin taparlas, con un compromiso [P] cuando no cabe con 3 filas (41.4). El banner admite la escala de fuente hasta el 130 % y su `wait_ms` tras «Siguiente» queda en ≈ 0 en el Editor (T52); falta medirlo en el teléfono con la tanda 3 (43). Lo que T52 dejó [P] (el compromiso del aviso y la luz de la pareja del Remolino) es la pregunta 47 de `docs/GDD.md` §6.3, y «Te faltan {n} semillas» de Revelar sin medios, un [P] del paso 1 (T14) sin pregunta, es la 48. *Actualización del 2026-10-14 (T58):* las dos quedaron decididas con su recomendación (D-022): la 47, ratificada; la 48, (b): sin aviso, responde la burbuja.
  - La medida del shader en un teléfono de clase A (44) es una **acción del dueño**: hace falta conseguir el teléfono.
- **Sustituye:** los [P] y las opciones abiertas de esas seis preguntas.
- **No sustituye:** D-011 ni D-012; las preguntas 37 y 38 siguen abiertas (decididas el 2026-10-13: D-020).

**Contexto.** Las preguntas 39 y 40 vienen del paso 2 (2026-10-12) y las 41-44, de la tanda 2 (2026-10-13). La prueba de la tanda 2 en el teléfono (2026-10-13, `docs/dev/traspaso/pruebas-telefono-2026-10-13.md`) cumple y aportó dos datos: el aviso tapa las burbujas «Te faltan» (observación 1, el punto 4 de la pregunta 41) y la medida del banner (pregunta 43: `main_ms` ≤ 2,3 ms, `fallback` `none` y `wait_ms` de 66-111 ms al entrar con «Siguiente», observación 4).

**Decisión.**
1. **Pregunta 39 → (a).** El banner de mundo va sin precio, y la oferta del pack de skin (`af_skin_*`, 1,99) va aparte: en el Mercado y en una tarjeta propia fuera del nivel. Con D-013 (Q10-A) el Mapa del río no lleva ofertas, así que el ejemplo «en el Mapa del río» de la opción (a) queda fuera: la tarjeta va fuera del nivel y fuera del Mapa, por ejemplo tras la primera victoria del mundo [P]. Capítulos: 05 §4.1 y §5 y 06 §4.5.
2. **Pregunta 40 → (a).** La Cuia chica ocupa uno de los vacíos del nivel (02 §8): la campaña v1 queda como está (el 23, el 26, el 29, el 35 y el 38 llevan un vacío de 4 y una Cuia chica).
3. **Pregunta 41 → se ratifican los cuatro [P]** de los avisos de la tanda 2.
   - El punto 2 (la tarjeta «¡Miquito a la vista!» se pierde si se gana antes de que la tropa se vaya) se revisa con Q11 del guion de la intro (el evento `af_fauna_sighted`, que sigue abierta).
   - El punto 4 (el aviso tapa la burbuja «Te faltan {n}» de la bandeja), ya visto en el teléfono, lo arregla **T52** [P].
4. **Pregunta 42 → (b).** Al abrirse Aguas estancadas, ¿Seguro?, Nivel fallido o Sin canoas, el aviso que esté a la vista se oculta, como en la celebración, y no vuelve; los que esperan en la cola siguen esperando a que se vuelva a jugar. Lo hace **T52**.
5. **Pregunta 43 → (a).** Se ratifican el banner con su arte en otro hilo, el respaldo de 800 ms [P] y los toques que se ignoran mientras espera el arte; la medida del teléfono del 2026-10-13 cumple (`main_ms` ≤ 2,3 ms y `fallback` `none` en el 11, el 41, el 81 y el 141). **Además, el banner admite la escala de fuente de 02 §12** (hasta el 130 %), como la tarjeta previa: tarea pequeña de **T52**, que también averigua el `wait_ms` de 66-111 ms al entrar con «Siguiente».
6. **Pregunta 44 → (a).** El shader del líquido se mide en un **teléfono de clase A** (Mali-G52 o Adreno 610; GPU ≤ 10 ms y 60 fps estables, 08 §5.4) antes de añadir más efectos al líquido, y se optimiza solo si no cumple. **Se acepta la excepción de 3 muestras** durante el revelado (unos 140 ms, del 0,64 al 0,85 del Dissolve). El SM-S938B es de gama alta y no sirve de referencia. **Hace falta un teléfono de clase A: acción del dueño.**

**Consecuencias.**
- **T52** (`unity-gameplay-dev`): las preguntas 41 (punto 4), 42 y 43 (escala de fuente), con sus tests PlayMode y capturas.
- **Dueño:** conseguir un teléfono de clase A para medir el shader (pregunta 44). Hasta entonces, nada nuevo en el líquido.
- **Sin cambios** en la campaña (pregunta 40), en el núcleo ni en Remote Config.

**Alternativas descartadas.** 39 (b) y (c); 40 (b), que regeneraba la campaña v1; 42 (a) y (c); 43 (b) y (c); 44 (b) y (c), y retrasar el icono del revelado al 47 %.

**Cómo revertir.** Con una decisión nueva que sustituya a D-015, pregunta a pregunta.

**Archivos afectados.**
- **Documentación** (2026-10-13, T50): este registro; `docs/GDD.md` §6.3; 02 §6; 03 §12; 05 §4.1, §5 y Dependencias; 06 §4.5; 07 §2.2; `docs/art/direccion-visual-v2.md` §8 (fila Shaders) y revisión 9. Completada el 2026-10-14 (T54): 02 §4 (la 42 como regla decidida y la 41, punto 4) y 08 §5.1 (la excepción de 3 muestras de la 44).
- **Código** (T52, integrada en `d2ce284`): los avisos (`GameScreen.Notices.cs`, `Toast.cs`), la bandeja (`BoosterTray.cs`), el banner de mundo (`WorldBannerView.cs` y `GameScreen.WorldBanner.cs`) y el Remolino (`RemolinoOverlay.cs`, `ProceduralArt.Remolino.cs`), con sus tests (`PhoneFixesTests`, `PhoneFixesSnapshotTests` y `WorldBannerArtTests`).

---

## D-016 · TiTino: personaje principal

- **Fecha:** 2026-10-13 (T1 y T6, con las preguntas del Mapa del río, D-013; T2, T4 y T5, con la respuesta «Si» del mismo día). Registrada el 2026-10-14 (T54).
- **Estado:** decidida por el dueño; en implementación en la tanda 4 (estado al día en la tabla de arriba).
  - Pregunta del escritorio: «¿Aplico también las recomendadas al resto que sigue abierto (TiTino T2/T4/T5, Frutas Q2-Q7, idiomas Q1-Q12 y tinaja Q-Ti1..Q-Ti7)?». Respuesta del dueño, literal: «Si». Gana la opción recomendada de la sección (j) de `docs/research/personaje/propuesta-titino.md`.
  - **T1-A y T6-A** constan en D-013 (punto 15); **T3** dejó de ser pregunta: la especie es *Bradypus variegatus* (perezoso de tres dedos de garganta marrón), salvo que el asesor cultural diga otra cosa.
  - Documentación aplicada el 2026-10-14 (T54, solo `docs/`; «Qué cambia en el GDD»). Implementación: plan (i) de la propuesta, pasos 3-10.
  - **«TiTino» queda pendiente de la búsqueda de marca** (el abogado incluye «TiTino» y «Titino» en la de Tintalú) **y del asesor cultural**, como todas sus frases y datos (07 §8.2).
- **Material:** la propuesta (2026-10-10, revisada con 36 hallazgos el mismo día).
- **Sustituye:**
  - lo que D-013 (punto 15) dejaba «como estaba» mientras T2 y T5 siguieran abiertas: el arranque, el tutorial, el aviso de Volver a la orilla y la orilla con Capi del HUD;
  - Capi en el tutorial (las frases de los niveles 3, 6 y 12; `capi_present` y `capi_point`), en Volver a la orilla (`capi_shrug`) y en el arranque (`capi_wave`): pasan a TiTino (`sloth_present`, `sloth_encourage` y `sloth_wake`). Capi conserva el 8, porque trae la cuia;
  - Capi como decoración fija en la orilla inferior durante la partida (02 §5; conflicto C4 de `docs/art/direccion-visual-v2.md`): **ninguna mascota fija en el muelle**, ni Capi ni TiTino, como Magic Sort;
  - la mirada de 0,3 s de Capi a la cuia que se sella (07 §5.3): TiTino nunca mira, señala ni reacciona a una cuia (D-002).
- **No sustituye:**
  - D-002 (sin resaltado) ni D-010: «Llenar y teñir» sigue siendo de los miquitos y TiTino no entra en ella (T2-C descartada);
  - D-008: TiTino nunca sale en Aguas estancadas, ¿Seguro?, Sin canoas, el Mercado ni ninguna oferta;
  - D-011: el aviso de TiTino tras Volver a la orilla es un aviso del tablero, así que dura por lectura y sale por la cola (punto 3);
  - D-012: el banner de mundo lo presenta Capi mientras Rosa asoma (la tabla f.2 de la propuesta lo daba a Rosa; D-012 es posterior y manda, como ya dijo D-013);
  - D-013: T1-A, T6-A, la placa del Dato del río con su retrato en el Mapa y la ventanita de casa en el inicio (Q17-A);
  - D-015 (pregunta 42): el aviso de TiTino se oculta al abrirse un modal del embudo, como cualquier aviso.

**Contexto.** La idea del dueño (2026-10-10, literal en la propuesta): un personaje principal, un perezoso tierno, que sea parte de la introducción y acompañe «de vez en cuando» al ganar o perder. D-013 lo hizo protagonista (T1-A) y fijó una sola línea de texto por victoria (T6-A). Quedaban cómo entra en la introducción (T2), el nombre (T4) y si aparece durante la partida (T5).

**Decisión.**
1. **T2-A: la introducción.**
   - **Arranque:** TiTino está en la pantalla de arranque que prevé el GDD (río y logotipo, 06 §2.2), colgado de su embaúba junto al logotipo, **sin palabras** (06 §2.1, regla 3). La primera vez, `sloth_wake` (abre los ojos, se estira y mira), visible **al menos 1,0 s [P]** solo en la primera apertura y solo si el primer toque en el nivel 1 sigue por debajo de 20 s (09 §6); se mide en el teléfono, y si ese segundo no cabe, TiTino puede no verse. En las demás aperturas, `sloth_idle`, que se corta cuando termina la carga. **Esa pantalla no existe todavía en el código:** hay que construirla antes (paso 6d de la propuesta).
   - **Tutorial:** acompaña con su retrato y `sloth_present` la instrucción de los niveles **1-3, 6 y 12**, tras la intro y cuando la tropa ya se fue; en el 8 habla Capi, que trae la cuia.
   - Sin prólogo: T2-B (≤ 8 s la primera vez) queda descartada y `ui_sloth_prologue_enabled` no se da de alta.
2. **T4-A: «TiTino», con la T mayúscula en medio.** Separa «Ti» (eco de Tintalú) de «Tino» y evita leer «Titi». No se traduce en ningún idioma. En el código, nunca el nombre: `sloth_*` y `bradypus_variegatus` (glosario §11).
3. **T5-B: durante la partida, solo en el tutorial y en un aviso tras «Volver a la orilla».**
   - En el tablero, nunca. Solo el retrato de la instrucción del tutorial (1-3, 6 y 12) y el aviso tras Volver a la orilla en los niveles 1-30, siempre fuera del panel del tablero, sin capturar toques (`raycastTarget = false`), sobre la franja del panorama si mide ≥ 64 dp y, si no, sobre el muelle, como la tarjeta del miquito (07 §6.4), y **nunca fijo en el muelle**.
   - **El aviso de Volver a la orilla:** retrato de 40 dp y una frase (`sloth_line_restart_*`; en 25-30, si el fallo duro rompió la Crecida, `sloth_line_streak_04`), con `sloth_encourage`, tras la intro corta del reinicio. Solo en el primer reinicio de cada nivel y solo si el nivel no tiene instrucción del tutorial (si la tiene, TiTino ya está en ella). Es una tarjeta de la familia del miquito: como mucho una tras la intro (guion de la intro §14). **Dura lo que fija D-011** (1.000 ms + 55 ms por carácter, entre 3 y 7 s) y sale por la cola de avisos, no los 2,0 s de la propuesta (d.5), que es anterior a D-011.
   - **Conflicto C4 cerrado:** ninguna mascota fija en el muelle durante la partida; Capi solo entra en escena para la Cuia extra, la Cuia de rescate y el banner de mundo.
4. **Lo que se aprueba con el lote de documentación** (f.3 y d.10 de la propuesta; no eran preguntas aparte):
   - el ajuste **«Mensajes de TiTino»** (`settings.sloth_messages`, booleano, `true` por defecto; apagado = solo gestos) y el índice de la última frase de cada momento (`sloth.last_line` [P], 8 enteros pequeños), que garantiza «nunca la misma frase que la última vista del mismo momento». Van en la misma migración del perfil que `settings.language` (D-018) y, mientras el esquema del perfil no se publique, se añaden con su default sin migrar, como `progress.map_seen_level` (D-013; 08 §4.3);
   - las claves de Remote Config (08 §9) `ui_sloth_enabled` = true, `ui_sloth_win_line_every_n` = 4, `ui_sloth_lines_per_session_max` = 4 y `ui_sloth_fail_enabled` = true [P];
   - `sloth_messages` como valor de `af_settings_change.key` (08 §8.2);
   - las claves de texto `sloth_line_<momento>_<nn>` (momentos `win`, `hard`, `fail`, `restart`, `streak`, `return`, `debut` y `milestone`) y `settings_sloth_messages` (glosario §10), y los datos del perezoso en la tabla de textos largos (forma de la clave: pregunta 49, decidida el 2026-10-14 en la (a), D-022: `fauna.bradypus_variegatus.dato_<nn>`, con puntos), todo en estado `pendiente_asesor`.
   - **No se da de alta** el evento opcional `af_mascot_line`: se pedirá si las pruebas A/B del soft launch lo necesitan.
5. **Frecuencia y topes:** los de d.6 y d.10 de la propuesta. Gesto en la victoria si cabe; frase en 1 de cada 4 victorias con el banco más específico, salvo en los tres hitos (fin de mundo salvo el 10, primer Difícil y primer Muy difícil), en los que habla siempre; una sola línea de texto por victoria (T6-A); frase en Nivel fallido solo en el primer fallo de cada nivel; como mucho 4 frases por sesión y nunca en dos pantallas seguidas; saludo de regreso y de la mañana, como mucho 1 al día; «Dato del perezoso» al tocarlo en el inicio, como mucho 1 por sesión. Con la línea de fruta en todas las victorias desde el nivel 1 (D-017, Q1-A y Q3-A), en la victoria TiTino solo habla en sus tres hitos (la advertencia de T6 en la propuesta). *Matiz del 2026-10-19 (D-028, pregunta 60, (b)):* el bocadillo `map_l10_intro` del Mapa del 10 no cuenta en el tope de 4 frases, pero sí en «nunca en dos pantallas seguidas» (06 §2.3).

**Consecuencias.**
- **Choques:** ninguno que pida pregunta. La duración del aviso la fija D-011 (punto 3); la regla «sin pantallas de texto en el arranque» se cumple porque TiTino no habla. La forma de las claves de los datos del perezoso dependía de la pregunta 49, decidida el 2026-10-14 en la (a) (D-022): con puntos.
- **Código** (plan (i) de la propuesta, sin paquetes, SDK, permisos ni versiones nuevas): lógica pura en AF.GameFlow (paso 3), rig procedural provisional solo en builds de desarrollo (4), textos con clave (5), ganchos de victoria, fallo, inicio, arranque y tutorial (6a-6e, cada uno con su PR, su interruptor y sus capturas), claves en `GameConfig` hasta dar de alta las de Remote Config (7) y arte final (9). **Las notificaciones (paso 8) siguen necesitando la aprobación del paquete de notificaciones locales y del permiso `POST_NOTIFICATIONS`**: esta decisión no la da.
- **Arte:** unas 22 animaciones de mascotas (10 de TiTino, 7 de Capi y 5 de Rosa, sin las del Mapa) y unos +5-6 días de artista por TiTino [E] (07 §11; 09 §5). Capi queda con 7 poses (`capi_idle`, `capi_present` en el banner de mundo de D-012, `capi_carry`, `capi_rescue`, `capi_wave` en la Bitácora, `capi_basket` y `capi_stamp`), más `capi_paddle` en el Mapa del río (D-013); la propuesta contaba 6 porque daba el banner a Rosa.
- **Dependencias externas:** el abogado (nombre), el asesor (frases, datos, nombres locales y si la embaúba o el yarumo tiene un uso ritual que la desaconseje) y el artista (hoja de modelo, después del asesor).
- **Datos del repositorio corregidos** (g.5 y d.5 de la propuesta): el sueño del perezoso en libertad (unas 9-10 h, no 15-18) en `docs/research/fauna/propuesta-fauna.md` y la cifra de Magic Sort sobre el IAP de la pantalla de derrota en `docs/research/09-referencia-magic-sort.md`.

**Qué cambia en el GDD** (aplicado el 2026-10-14, T54):
- `docs/GDD.md`: versión 1.8; §2 #22 y #24.
- 00: §3.1, §3.4, §10, §11, Parámetros (`ui_sloth_*`) y la pregunta abierta 3.
- 02: §4.2 (el aviso de TiTino tras Volver a la orilla), §5 (fila Capi y esquema), §6 (fila TiTino), §12 (ajuste y Reducir movimiento), §13 (TiTino en 1-3, 6 y 12; Capi en el 8) y Parámetros (fila 6 Victoria).
- 04: §2.1 (tutorial de Marea), §2.3 (presentación de Morpho) y §2.7 (aviso tras Volver a la orilla).
- 06: §2.2 (arranque y tutorial).
- 07: §5.3, §5.5, §6.1, §6.4, §6.5, §11 y Dependencias (fila 04: el aviso de TiTino en lugar de `capi_shrug`).
- 08: §4.3, §8.2 y §9 (las claves `ui_sloth_*`, sin rango: la propuesta solo fija los defaults).
- 09: §1.1 #27 y §5.
- `docs/art/direccion-visual-v2.md` §3.7 y §11 (C4); `docs/research/intro-rio-miquitos/guion.md` §4.6 y §14; `docs/research/fauna/propuesta-fauna.md` (c.2, fila 14, y c.3); `docs/research/09-referencia-magic-sort.md` §1; `docs/art/prompts-chatgpt.md` (`capi_idle` ya no va en el muelle; **el prompt de la hoja de modelo de TiTino que pedía f.3 queda pendiente**, con su hoja de modelo y después del asesor); la propuesta de TiTino (marcas de lo decidido).

**Alternativas descartadas.** T2-B (prólogo de ≤ 8 s) y T2-C (TiTino dentro de «Llenar y teñir»); T4-B («Titino») y T4-C (otro nombre ya); T5-A (nunca durante la partida) y T5-C (fijo en el muelle).

**Cómo revertir.** `ui_sloth_enabled` = false apaga a TiTino en remoto, y el ajuste «Mensajes de TiTino» deja solo los gestos. Revertir la decisión (otro nombre, otro papel o volver a Capi) exige una decisión nueva que sustituya a D-016.

**Archivos afectados.**
- **Documentación** (2026-10-14, T54): este registro y los de «Qué cambia en el GDD».
- **Código** (previsto, plan (i)): `Assets/_Project/Scripts/{GameFlow, Presentation, UI}`, el perfil (`Save/Profile.cs`) y sus tests; ningún cambio en `Packages/com.amazonfill.core/`.

---

## D-017 · Frutas del río

- **Fecha:** 2026-10-13 (Q1, con D-013; Q2-Q7, con la respuesta «Si» del mismo día). Registrada el 2026-10-14 (T54).
- **Estado:** decidida por el dueño; en implementación en la tanda 4 (estado al día en la tabla de arriba).
  - Misma pregunta y respuesta que D-016: gana la opción recomendada de cada pregunta de la sección 9 de `docs/research/frutas-del-rio/propuesta-frutas-del-rio.md`. **Q1-A** consta en D-013 (punto 15).
  - Documentación aplicada el 2026-10-14 (T54). Implementación: pasos F1-F9 de la sección 10 de la propuesta.
  - **Todas las líneas, nombres y etimologías quedan pendientes del asesor cultural** (07 §8.2); ninguna llega a la interfaz sin esa revisión.
- **Material:** la propuesta (2026-10-10, revisada el mismo día): 240 líneas en español de 12 series, con su fuente, y un banco para el cacao y el arazá.
- **Sustituye:**
  - el «Dato del río» como una frase cada 10 niveles (lo sustituyó D-013 con Q1-A); con Q2-A son 20 líneas por fruta, una por nivel;
  - la regla de cifras de la propuesta (§2.5: el traductor escribe la cifra en el formato de su idioma), por Q8-A de idiomas (D-018): las cifras con separador o decimales van con marcador `{n}`.
- **No sustituye:** la economía (completar una fruta no da semillas; `tools/econ_sim.py` no cambia), D-008 (la línea no alarga la cadena corta ni es un takeover), D-013 (la llegada del Mapa en el `10k`) ni la pregunta 45, que seguía abierta (decidida el 2026-10-14 en la (a): D-022).

**Contexto.** El pedido del dueño del 2026-10-09: ir explicando de qué es cada color, de a poco, unas 20 líneas por fruta, para aprender y que el juego tenga sentido. D-013 decidió la Q1-A (la serie absorbe el Dato del río) y dejó Q2-Q7 abiertas.

**Decisión.**
1. **Q1-A** (D-013): la serie absorbe el Dato del río: una línea en la victoria con la etiqueta «Dato del río · Açaí 3/20» y el evento `af_river_fact_shown`; sale de la pantalla de la Canasta.
2. **Q2-A: 20 líneas por fruta**, una por nivel, en un bloque de 20 por ciclo (D-009). Se puede bajar a 10 (una cada dos niveles) desde Remote Config, con la clave que gobierne la cadencia de la línea de fruta: cuál es dependía de la pregunta 45 (*Actualización del 2026-10-14 (T58):* `ui_fruit_line_every_n_levels` [P], con 2 = una cada dos niveles; D-022).
3. **Q3-A: desde el nivel 1** (el açaí abre el juego). Se vigila con la alarma del embudo 1-10 (06 §2.4: un nivel que pierda más del 5 %) y se apaga en remoto si molesta.
4. **Q4-A: ficha para releer en la 1.0.** Hoja inferior de solo lectura por tinte: icono, nombre en el idioma del jugador, nombre científico y «k/20»; las líneas reveladas, numeradas, y el resto en silueta con «Nivel {n}»; «Fuentes» al pie, como texto, sin abrir el navegador. Se abre desde la línea de la victoria (toda la línea es un botón de ≥ 48 dp) y desde Ajustes › «Sobre la Amazonía» › «Frutas del río». Lo revelado se deduce de `progress.max_level` y `progress.skipped_levels`: sin estado nuevo. **Evento nuevo aprobado:** `af_fact_page_open {tint_id, unlocked}` (08 §8.2).
5. **Q5-A: sin premio económico.** Al completar una serie, un sello visual (el contador 20/20 pasa a un sello con el icono de la fruta). En la 1.1, la opción C: el premio que el canon ya prevé para el Cuaderno de campo (300 semillas + marco [P] al completar la página Frutas; 06 §4.6).
6. **Q6-A: «Frutas del río»** es el nombre de la ficha y de la lista; en la victoria, la etiqueta es «Dato del río» (`river_fact`, ya traducida).
7. **Q7-A: la línea va bajo «Inicio»,** separada por un filete, en una **ranura de alto fijo por idioma y escala de texto** (el alto de la línea más larga de ese idioma a esa escala). «Siguiente» sube una sola vez (≈ 90 dp en español [E]) y queda en el mismo sitio en todos los niveles. Aparece a los 1,40 s, con un fundido de 200 ms, sin sonido ni confeti. Las medidas se cierran con capturas reales (F3).
8. **Lo que se aprueba con el lote de documentación** (10.1 y sección 6 de la propuesta):
   - el formato **`af-facts/1`** del calendario `Assets/_Project/Data/Content/frutas_del_rio_v1.json` (contrato 31 de `docs/GDD.md` §7.5; sintaxis en 08 §4.6), con la elección determinista de la línea de 6.4;
   - las claves de texto `fact_progress`, `fact_sheet_title`, `fact_locked`, `fact_starts_at`, `fact_sources` y `fact_a11y` (glosario §10), y las de contenido de cada serie (la línea k, la frase de la tarjeta «Fruta nueva» y el nombre visible del tinte; forma exacta: pregunta 49, decidida el 2026-10-14 en la (a), D-022: `fact.<tinte>.<kk>`, `fact.<tinte>.tag` y `tint.<tinte>.name`);
   - el `fact_id` de la línea de fruta en `af_river_fact_shown`: `<tinte>.<kk>` (`acai.03`; 08 §8.2), nunca el texto;
   - la longitud: ≤ 90 caracteres en español, Nunito Regular a 14 sp, nunca por debajo de 12 sp; y las reglas de redacción de 4.2 (nada de salud ni nutrición, nada sagrado ni ritual, nada de pintura corporal, alcohol, empresas ni marcas; los pueblos en presente cuando existen hoy).

**Consecuencias.**
- **Pregunta 45** (`docs/GDD.md` §6.3): sigue abierta, pero su recomendación (a) ya no supone nada abierto, porque la ficha existe en la 1.0. Hasta que se responda, `ui_river_fact_every_n_levels` sigue en 10 (08 §9). La propuesta (§10.1) pedía el default 1, que es la opción (b) de la 45.
  - *Actualización del 2026-10-14 (T58):* decidida en la (a) (D-022): `ui_river_fact_every_n_levels` se queda en 10 (la llegada) y la línea de fruta tiene su clave, `ui_fruit_line_every_n_levels` [P], con default 1.
- **Choque con idiomas** (D-018): la forma de las claves (con puntos o en `snake_case`) y las columnas de estado de las tablas de textos: pregunta 49. *Actualización del 2026-10-14 (T58):* decidida en la (a) (D-022): dos familias de claves y una columna de estado por idioma.
- **Datos del Mapa:** las líneas k14 y k19 del Agua del Tapajós repiten datos de llegada (D-013) y se cambian por otras del banco antes de publicar.
- **Código** (sección 10 de la propuesta): F1 (calendario y textos en la tabla) va **después del nivel 2 de idiomas** (CSV y fachada `Loc`), para que el texto nazca en la tabla; F2 (lógica pura, sin asignaciones por llamada); F3 (línea en la victoria, con capturas a 360 × 640 dp y pseudolocalización); F4 (ficha); F5 (tarjeta «Fruta nueva» y la lista de niveles ocupados, a la que aún le faltan las variantes 18, 22 y 160 y los hitos de 02 §13); F6 (eventos en la fachada de analítica simulada); F7 (asesor y traducción por lotes); F8, opcional (presencia del tinte en su bloque), y F9 (arte).
- **Arte** [E]: 14 escenas de ficha y el sello (7-14 días + 1 de artista); los iconos `fruta_<id>` ya los pedía D-010.
- **Traducción:** ≈ 3.300 palabras por idioma solo en las 12 series; entran en la partida de D-018 (Q12-A). **Asesor:** ≈ 4-8 h [E], dentro de sus 20-40 h.

**Qué cambia en el GDD** (aplicado el 2026-10-14, T54):
- `docs/GDD.md`: §2 #21 y #28, §6.3 (preguntas 45 y 49) y §7.5 (contrato 31).
- 00: §6.4, §10 y §11; 02: §6 (fila «Dato del río») y §12; 06: §4.4, §4.5 y §4.6; 07: §5.4 (el panel sin papel), §8.2, §9.2 (`sfx_fact`) y §11 (escenas de ficha); 08: §4.6 (nueva), §8.2, §9 y Preguntas abiertas 14 (con D-018); 09: §1.1 #20 y #28, y §5.
- La propuesta (marcas de lo decidido).

**Alternativas descartadas.** Q2-B (10 líneas) y Q2-C (5); Q3-B (desde el 11) y Q3-C (desde el 21); Q4-B (sin ficha hasta el Cuaderno); Q5-B (20-50 semillas) y Q5-C en la 1.0; Q6-B («Fruta a fruta») y Q6-C («Del árbol a la cuia»); Q7-B (encima de la Cosecha).

**Cómo revertir.** La clave de cadencia de la línea de fruta a 0 la apaga en remoto (`ui_fruit_line_every_n_levels`, por la pregunta 45, (a): D-022). Revertir la decisión exige una decisión nueva que sustituya a D-017.

**Archivos afectados.**
- **Documentación** (2026-10-14, T54): este registro y los de «Qué cambia en el GDD».
- **Datos y código** (previstos): `Assets/_Project/Data/Content/frutas_del_rio_v1.json`, `Assets/_Project/Data/Loc/facts.csv` y `Assets/_Project/Scripts/{GameFlow, Levels, UI}` (F1-F6), con sus tests.

---

## D-018 · Idiomas y localización

- **Fecha:** 2026-10-13 (respuesta «Si»). Registrada el 2026-10-14 (T54).
- **Estado:** decidida por el dueño; en implementación en la tanda 4 (estado al día en la tabla de arriba). Gana la opción A de cada pregunta de la sección 8 de `docs/research/idiomas/propuesta-idiomas.md` (Q1-Q12). Documentación aplicada el 2026-10-14 (T54). Implementación: niveles 1-4 de la sección 6 de la propuesta.
- **Material:** la propuesta (2026-10-10, revisada con 20 hallazgos el mismo día).
- **Aprobaciones explícitas del dueño** que lleva esta decisión (regla dura 4 de `CLAUDE.md`):
  1. **Paquete:** añadir **Unity Localization 1.5.13** (`com.unity.localization`) cuando llegue la **fase 3 del roadmap** (09 §2), detrás de la fachada `Loc` y con el CSV como fuente (Q3-A). Trae Addressables como dependencia, que ya está en el stack (2.10.3; `docs/GDD.md` §2 #26) y en D-013 (punto 14). Hasta la fase 3, nada nuevo en `Packages/manifest.json`; la instalación va en su propio PR, con el merge del dueño.
  2. **Gasto:** la partida de traducción de la 1.0 sube a **≈ 1.300-3.000 €** (borrador por IA con revisión nativa, con los datos de fruta y fauna; Q12-A), en lugar de los 600-1.500 USD de 09 §5.
- **Sustituye:**
  - `tools/loc-sync` con Google Sheets (08 §2.4): los textos viven en un CSV en el repositorio (Q4-A), porque la hoja exigiría credenciales que los agentes no pueden manejar (regla dura 5);
  - el formato fijo de cifras del glosario §10 («1.000» en ES, «1,000» en EN): pasa al de la región del teléfono (Q8-A);
  - el default `"es-419"` de `settings.language` (08 §4.3): pasa a `"auto"`, seguir al teléfono (Q7-A);
  - el orden de arranque de 08 §2.3 (`LoadLocale(system or saved)` antes que el perfil): manda la elección guardada.
- **No sustituye:** los títulos de ficha decididos (Tintalú con tilde en todo lo visible; glosario §1.3), la regla «sin pantallas de texto en el arranque» (06 §2.1) ni el límite de 80 MB del AAB.

**Contexto.** El pedido del dueño del 2026-10-09: tener cubierto el idioma «ahora que no ha crecido mucho», como lo hacen los juegos probados; «puede que no trabajarlo ahora, pero sí para tenerlo ahí».

**Decisión.**
1. **Q1-A: tres tandas** (no son las fases del roadmap).
   - **Tanda 1** (1.0 y soft launch, fase 3 del roadmap): **ES-419, PT-BR y EN**.
   - **Tanda 2** (lanzamiento mundial, antes de abrir Europa y Asia en `biz_country_enabled`): los 11 de Magic Sort, es decir, + FR, DE, IT, NL, TR, JA, KO y, **el último, RU** (en Rusia no hay cobros de Play ni anuncios de Google).
   - **Tanda 3** (según datos): ZH-Hant, ZH-Hans, ID, VI, PL y TH; AR e HI solo con tecnología de derecha a izquierda y de escrituras complejas. Criterio orientativo [P]: un idioma no soportado que pase del ≈ 3 % de los usuarios activos o de los ingresos durante 4 semanas.
2. **Q2-A: preparar ya la base, sin paquetes.**
   - Nivel 1 (≈ 1 día de agente): las cuatro trampas del código (frases pegadas a trozos, el `.Replace` que busca «semillas», los plurales fijos y el separador de miles fijo) y llevar a la tabla los nombres de `StoreCatalog` y los datos de `FaunaFact`.
   - Nivel 2 (≈ 2-3 días): `Assets/_Project/Data/Loc/strings.csv` y `facts.csv`; la fachada `Loc` (`Loc.T`, `Loc.Number`, `Loc.Changed`) en `AF.Localization`, sin motor; `UiText` como acceso tipado; el idioma de desarrollo elegible sin UI; el idioma falso `en-XA` en las pruebas de capturas; `FontSetup` que calcula los glifos desde el CSV, con su prueba de cobertura, y la prueba de `max_chars`.
   - Rigen las diez reglas de la sección 3.2 de la propuesta: toda cadena visible por clave; una frase completa por clave, con marcadores con nombre; plurales CLDR; números con `Loc.Number` y precios de la tienda; sin texto en imágenes; analítica y Remote Config con claves; sin `ToUpper` ni `ToLower` sobre texto visible; textos largos en una tabla aparte; `pendiente_asesor` en todo texto cultural; y avisos, notificaciones y lector de pantalla también por clave. La regla 1 (claves en `snake_case`, sin puntos) choca con las claves de contenido de D-013 y de D-017: pregunta 49 (decidida el 2026-10-14 en la (a), D-022: la regla 1 vale para toda la UI y las claves de contenido largo van con puntos).
   - La regla para los agentes `.claude/rules/localizacion.md` (6.1 de la propuesta) [P] va en un PR aparte, fuera de `docs/`.
3. **Q3-A: Unity Localization 1.5.13 en la fase 3**, detrás de la fachada y con el CSV como fuente. Un método de Editor versionado importa el CSV a las String Tables, que se commitean como generadas, y una prueba comprueba que coinciden. Aprobación del paquete: arriba.
4. **Q4-A: CSV en el repositorio.** A los traductores se les pasa una copia y se reimporta; un TMS (Gridly ≥ 50 €/mes, Lokalise ≥ 149 USD/mes) solo al pasar de unos 5 idiomas.
5. **Q5-A: cómo se traduce.** UI y tutorial: borrador por IA con glosario, contexto, límite de caracteres y capturas, y revisión de un nativo pagada. Datos de fruta y fauna: **borrador por IA y revisión nativa, como la UI** (pregunta 51, decidida por el dueño el 2026-10-14: (b); D-022). *Antes de D-022 decía «traducción humana completa y revisión nativa» (la propuesta, 5.2: «traducción o posedición humana completa»), que no cabía en la partida aprobada en Q12-A: esa partida supone borrador por IA y revisión nativa también en los datos, y la tanda 1 con traducción humana de todo, datos incluidos, cuesta ≈ 2.800-5.100 € (propuesta, 5.5; la opción C de Q12).* Ficha de la tienda: redacción nativa con palabras clave (ASO). Nunca traducción automática sin revisión en algo que vea el jugador.
6. **Q6-A: un solo español (ES-419) y un solo portugués (PT-BR)** para todos los países hasta tener datos; la ficha de España puede ser es-ES.
7. **Q7-A: idioma del teléfono con respaldo** (`es-*` → ES-419, `pt-*` → PT-BR, el resto → EN), **selector en Ajustes** (clave `language`; cada idioma con su propio nombre; solo los que tienen la UI completa) y **la elección guardada antes que la del teléfono**. Orden: línea de comandos (solo en desarrollo) → elección guardada → teléfono → EN. `settings.language` = `auto` por defecto. Sin pantalla de idioma al arrancar y, en la 1.0, sin el idioma por app de Android 13 (`localeConfig`). Cambiar de idioma vuelve al inicio y reconstruye la UI.
8. **Q8-A: números con el formato de la región del teléfono** (Colombia «1.000», México y Perú «1,000»), con una sola función (`Loc.Number`); en los datos de fruta, toda cifra con separador o decimales va con marcador `{n}`, para que coincida con la interfaz. Hay que medir en el teléfono los datos de cultura de IL2CPP [NV]. Cambia el glosario §10.
9. **Q9-A: alfabetos de la tanda 2.** Ruso con Nunito ExtraBold en los títulos (Baloo 2 no tiene cirílico); japonés y coreano con Noto Sans JP y KR, en atlas recortados a los caracteres de nuestras tablas y en un pack de fuentes de Play Asset Delivery que se descarga justo después de instalar (fast-follow); en coreano, el corte de línea moderno de TMP, activado en el Editor o con un método de Editor versionado; en francés, U+202F se sustituye por U+00A0. **Árabe, hebreo, hindi y tailandés quedan fuera hasta la tanda 3.** Cada fuente nueva se pide al dueño antes (regla dura 4).
10. **Q10-A: los datos de fruta llegan por lotes** a cada idioma, primero hasta donde llegan los jugadores; un dato sin traducir **no se muestra** (la ficha lo deja en silueta). La UI incompleta, en cambio, para un build de release.
11. **Q11-A: analítica del idioma, aprobada:** propiedades de usuario `device_language` (el del teléfono) y `app_language` (el del juego) (08 §8.1), para decidir la tanda 3.
12. **Q12-A: partida de traducción de la 1.0, ≈ 1.300-3.000 €:** EN y PT-BR con borrador por IA y revisión nativa, con los datos de fruta y fauna (también por IA con revisión nativa: pregunta 51, D-022), la revisión de estilo del español neutro y el LQA de 3 idiomas (sección 5.5 de la propuesta). Decisión de gasto aprobada (09 §5).

**Consecuencias.**
- **Choque:** la regla 1 de las claves y las columnas de estado del CSV, frente a D-013 y D-017: pregunta 49 de `docs/GDD.md` §6.3. *Actualización del 2026-10-14 (T58):* decidida en la (a) (D-022). Y Q5-A frente a Q12-A (la traducción humana completa de los datos no cabía en la partida): pregunta 51, decidida en la (b) (D-022). Las cifras de los datos de fruta (propuesta de Frutas, §2.5) las resuelve Q8-A, como ya prevé la propia propuesta de idiomas (§3.4).
- **Perfil:** `settings.language` = `auto` y `settings.sloth_messages` (D-016) entran en la misma migración a `profile_schema` 2 (y, si se aprueba Q11 del guion de la intro, el avistamiento de fauna), o sin migración mientras el esquema no se publique, como `progress.map_seen_level` (D-013).
- **Arranque (08 §2.3):** el idioma guardado vive en el perfil (o en una copia en PlayerPrefs, como hace el selector de Unity Localization); la carga síncrona en menos de 50 ms se mide en el teléfono con el paquete.
- **Código:** niveles 1 y 2 de la sección 6 (≈ 3-4 días de agente, sin paquetes y con Unity, así que cuentan para el límite de 2 Editores); nivel 3 en la fase 3 (paquete, perfil, selector, traducción, LQA y fichas); nivel 4 antes del lanzamiento mundial.
- **Coste de la tanda 2** [E]: ≈ 3.500-8.600 € con borrador por IA y revisión nativa. No se aprueba ahora: se decide con el lanzamiento mundial.
- **Datos corregidos:** Lokalise empieza en 149 USD/mes, no en 59 (09 §5); las 300-600 cadenas de 09 §1.1 #28 no incluían los datos de fruta ni de fauna.
- **Fuera:** la clave opcional `ui_languages_enabled` (nota 8 de la sección 9 de la propuesta) no se da de alta.

**Qué cambia en el GDD** (aplicado el 2026-10-14, T54):
- `docs/GDD.md`: §2 #21, #26, #28 y #30; §5 (fase 3); §7.5 (contrato 32).
- 00: §10 (regla de cifras y claves nuevas) y §11; 02: §12 (filas «Texto» e «Idioma»); 07: §6.4 (la tarjeta del miquito, a 12 sp); 08: §2.2 (`AF.Localization`), §2.3, §2.4 (`tools/loc-sync`), §4.3, §4.7 (nueva), §6.1, §8.1 y Preguntas abiertas 14 (con D-017); 09: §1.1 #28, §2.3 (fase 3), §3.4 (nueva: tandas 2 y 3), §5 y §7 (R35).

**Alternativas descartadas.** Q1 B (el conjunto de Royal Match), C (solo los 3) y D (sin ruso); Q2 B (ya) y C (en la fase 3); Q3 B (sistema propio) y C (I2 Localization); Q4 B (Google Sheets) y C (TMS ya); Q5 B y C; Q6 B; Q7 B; Q8 B y C; Q9 B y C; Q10 B y C; Q11 B; Q12 B y C.

**Cómo revertir.** Con una decisión nueva que sustituya a D-018, pregunta a pregunta. La fachada `Loc` permite cambiar la vía (paquete o sistema propio) sin tocar las pantallas.

**Archivos afectados.**
- **Documentación** (2026-10-14, T54): este registro y los de «Qué cambia en el GDD».
- **Código y datos** (previstos): `Assets/_Project/Data/Loc/{strings.csv, facts.csv, glossary.csv}`, `Assets/_Project/Scripts/` (`AF.Localization`, `UiText`, `StoreCatalog`, `StuckModal`, `AdOverlay`, `FontSetup` y `Save/Profile.cs`) y las pruebas de capturas; en la fase 3, `Packages/manifest.json` con el paquete aprobado.

---

## D-019 · Tinaja (M13): mecánica propia

- **Fecha:** 2026-10-13 (respuesta «Si»). Registrada el 2026-10-14 (T54).
- **Estado:** decidida por el dueño. Gana la opción recomendada de cada pregunta de la sección (g) de `docs/research/cultura-ribera/propuesta-tinaja.md`.
  - **Núcleo implementado** (T55, integrada en `451856e`): las secciones c.1 y c.2 de la propuesta con su ficha b.2 (`TinajaMechanic`, `CucharonMechanic`, el gancho `IMechanic.AllowsTarget`, la validación, la rama del bot, la causa del «sin vertido seguro» en `SafeMoves` y las búsquedas sin poda de fútiles en los niveles con tinaja). Los packs 1-200 no cambian.
  - Documentación aplicada el 2026-10-14 (T54), con la ficha M13 en 02 §9.1.
  - Pendientes: el generador, el esquema (`pack_schema` 3) y level-qa (T56), Unity (c.6 de la propuesta), el corte por `hint_timeout_ms` en el núcleo y el tope de ocultas por tipo de nivel en el pipeline.
  - **Todo nombre, texto y Dato del río, y el gesto de sacar, quedan pendientes del asesor cultural** (sección (h) de la propuesta).
  - **Pregunta abierta del dueño (2026-10-19, C1c; sin número D nuevo): el 323 sale sin Tinaja.** De la condición de Q-Ti3 (C1b), TJ4 está en verde (Morpho con corte por tiempo y la Luciérnaga con la tinaja, en `integracion-fase1-fase2` desde la ola 7), pero la compuerta de PF1 (el factor de Morpho medido en un teléfono de clase A con `--ei af_bench_morpho 1`, que sustituye a los 8× [P]) no consta en verde en este registro, en el traspaso ni en las pruebas del teléfono: el cliente publica la variante b de 301-700 (`published_variant` = "b"), con el 323 como nivel normal, hasta que el dueño dé esa compuerta por buena. El 323 real de la variante a se juega de punta a punta en `Tinaja323EndToEndTests` (PlayMode). **Para publicarla entonces:** `published_variant` = "a" en `tools/level-generator/specs/campaign_v2.json`, regenerar el registro (`--registry`) y los tres paquetes en `Data/Levels/`, `LevelCatalog.PublishedVariant` = `VariantA` y regenerar la escena con `GameSceneBuilder.CreateGameScene` (si el factor medido cambia, regenerar antes 301-700).
- **Material:** la propuesta (2026-10-10, revisada el mismo día; anexo 3).
- **Sustituye, solo para la Tinaja** (Q-Ti1-A):
  - 02 T1 (contenido oculto solo en Difícil o Muy difícil desde el 100) y T3 (≤ 3 dosis y nunca llena);
  - Q17 A de D-007 («ninguna mecánica tapa contenido»);
  - Q14 A de D-007 con 03 §8 (nada oculto en niveles con mecánicas con disparador): para la Tinaja, lo prohibido es «un disparador que depende de lo oculto»;
  - el nombre «Tinaja» de la skin 9 (Ucayali), que se renombra (Q-Ti4-A);
  - y, también en Aguas turbias, en 02 T5, 03 §8 y 04 §2.3 (a), la respuesta única de Morpho sin vertido seguro, por el orden de respuestas (c.10 de la propuesta; Consecuencias). *Corregido el 2026-10-14 (T58, revisión de T54): antes figuraba T5 en «No sustituye».*
- **No sustituye:** el contrato 8 (como mucho 4 ocultas y 24 realidades), T2, T4 ni T6; Q14 A y Q17 A para las demás mecánicas; D-002 (el jirau no es un resaltado); D-005; D-009 (la etiqueta sale del bot ×40); D-010; ni la economía.

**Contexto.** La idea del dueño del 2026-10-10: «un recipiente grande de barro que no permite ver qué mezcla tiene y puedo utilizarlo solo con un recipiente pequeño de dos porciones», con una trama y un algoritmo que la hagan justa. La propuesta partió de tres variantes y tres jueces, y de una revisión con verificación independiente.

**Decisión.**
1. **Q-Ti1-A: contenido oculto en niveles Normales, desde su estreno**, con:
   - **tope por tipo de nivel:** debut (p3-p5), comodidad y práctica, 3 dosis (2 ocultas, ≤ 2 realidades); retos, 4-5 dosis (3-4 ocultas, ≤ 6 o ≤ 24); hito, 5 dosis (4 ocultas, ≤ 24); recuperación (p1-p2) y remanso (p19), sin tinaja. Las 5 dosis quedan sujetas a la compuerta de coste de Morpho;
   - **ruta segura obligatoria** (RUTA_SEGURA, 03 §8), con paseos seguros y casuales en el generador;
   - **una suelta que solo depende del recuento visible** («×n» llega a 0), nunca del color de una oculta.

   Es la excepción, **solo para la Tinaja**, a T1, T3, Q17 A y Q14 A (con 03 §8).
2. **Q-Ti2-A: su cuia chica recibe cualquier fruta** de la tinaja, dos dosis en capas. Exige la rama mínima del bot: +16 solo si la cima de su cuia es de la fruta sacada (hecho en T55).
3. **Q-Ti3-A: estreno en el 323** (p3 del ciclo 321-340, aguas bajas, mundo 7 Madre de Dios) **en la 1.0**, si al generar 201-700 pasa sus pruebas y si **Morpho en el cliente (con corte por tiempo) y la Luciérnaga están listos**. Si no, en la **1.1**, con el mismo nivel: build nuevo, `level_overrides` y la regla de migración de 03 §9 (si el jugador ya pasó el 323, se le sirve una vez el tablero de estreno). Los packs 11-200 no cambian.
4. **Q-Ti4-A: la mecánica se llama «Tinaja»** (PT «Talha» y EN «Water Jar» [P]; su cuia, «su cuia chica» / «cuia pequena» / «small cuia» [P]; «Clay Jar» y «Pote de Barro» son el Cántaro). **La skin 9 se renombra** y se revisa con el asesor (territorio shipibo), que propone el nombre nuevo.
5. **Q-Ti5-A: no enseña qué frutas guarda:** solo la boca y «×n». Interruptor `ui_tinaja_show_contents` = false, para encenderlo si el soft launch muestra quejas de suerte (al solver no le cambia nada).
6. **Q-Ti6-A: se acepta que se pueda espiar reiniciando.** Volver a la orilla con menos de 8 vertidos es gratis y lo revelado sigue revelado (R7, T6, I5), como en Aguas turbias; el dilema de «mirar o enterrar» queda dentro de un intento. Se miden en el soft launch los reinicios con menos de 8 vertidos en niveles con tinaja. **«Intento» se define en 02 T6:** el que cuenta `attempt` = `fails_level` + 1 (04 §6.1), así que un reinicio con menos de 8 vertidos no abre uno nuevo. *Aclaración del 2026-10-14 (revisión de T54):* lo revelado sigue revelado también tras un reinicio con 8 o más vertidos (fallo blando, que sí abre un intento) y tras un fallo duro, hasta que se vuelve a cargar el nivel o se sirve la variante aliviada, que es otro tablero; así lo decían ya 04 §2.7 y `LevelSession.Restart`, y 02 T6 lo dice ahora de forma explícita. En la confirmación de reinicio («Perderás los vertidos de este intento») la palabra es coloquial: los vertidos desde el último reinicio.
7. **Q-Ti7: solo se pregunta si el asesor confirma el riesgo** de que el gesto de sacar (la cuia entra en la tinaja y sale llena) evoque el masato; en ese caso, **A:** se mantiene el gesto con el encuadre «solo agua», sin servir a nadie.
8. **La ficha b.2 de la propuesta entra en 02 §9.1 como M13** [P]: regla exacta, `legal` con el filtro de origen, `futil` con sus dos exenciones, R10 f, R14, Marea, Volver a la orilla, Pororoca, Luciérnaga, Morpho con su orden de respuestas, combinaciones, tutorial, aspecto, animación y cultura.
9. **Lo que se aprueba con el lote de documentación** (c.10 de la propuesta): `mech_debut_level.tinaja` = 323, `mech_enabled.tinaja` = true y `ui_tinaja_show_contents` = false (08 §9); `pack_schema` 3 con los tipos `tinaja` y `cucharon` e `intro_mechanic` = `tinaja` (contrato 21; los hace T56); `af_mechanic_intro.mech` = `tinaja` (08 §8.2); `MechanicType` 8 (`Tinaja`) y 9 (`Cucharon`), que no se persisten (glosario §11).

**Consecuencias.**
- **Morpho y la pista por inactividad responden en un orden fijo,** también en Aguas turbias (03 §8; 04 §2.3): si hay un vertido seguro en todas las realidades, lo proponen (Morpho cobra); si ninguna realidad tiene solución, «Desde aquí no hay salida: la Marea o la orilla», sin cobrar; si alguna la tiene pero ningún vertido es seguro, «No veo un vertido seguro: la luciérnaga ilumina la tinaja» (en Aguas turbias, «… ilumina una cuia»), sin prometer y sin cobrar, nunca con R = 1; si se agota `hint_timeout_ms`, «No encuentro un camino desde aquí», sin cobrar. La pista por inactividad no pulsa nada en los tres últimos casos. El núcleo ya expone la causa (`SafeMoves.Outcome`).
- **R13:** la nota [P] de la propuesta sobre el vaivén tras la suelta queda sustituida por D-014 (el cierre de ≤ 64 estados en `LevelSession`).
- **Choque:** los Datos del río candidatos de su estreno (de Pará) no tienen sitio con D-013 y D-017: pregunta 50. *Actualización del 2026-10-14 (T58):* decidida en la (a) (D-022): entran como «Otro dato» de la tarjeta de la parada k = 32 (321-330), con su lugar real y el visto bueno del asesor.
- **Pregunta 33** (D-020): el 323 entra en la tabla de novedades de 201-1000 que hay que rehacer antes de generar 201-700.
- **Pororoca:** sigue el canon con la pareja fija; si en el pipeline falla a menudo, el tech-lead la deshabilita mientras la tinaja tenga dosis, como con la Lluvia y la Piraña [P].
- **Coste [E]:** 16-23 días de agente en total (4-5 del núcleo, ya hechos), sin contar las piezas que comparte con Aguas turbias (Morpho y la pista en el cliente, la Luciérnaga, la Pororoca, la tarjeta y el dedo guía); arte (tinaja, jirau, asa de la cuia y 4 sonidos) y asesor.

**Qué cambia en el GDD** (aplicado el 2026-10-14, T54):
- `docs/GDD.md`: §2 #10 y #12; §6.2 (debuts) y §7.1 (fila 1), con el 323 en la lista (completado el 2026-10-14 en T58); §7.5 (contratos 1, 8, 21 y 27).
- 01: §4 (fila «Mecánicas nuevas») y Dependencias (fila 02), con el 323 en la lista única de estrenos (contrato 1, Q-Ti3-A; completado el 2026-10-14 en T58).
- 00: §4 (skin 9), §5.1 (la ranura de Revelar también con la tinaja), §5.3, §6.2, §6.5, §10, §11 y Parámetros (`ui_tinaja_show_contents`).
- 02: §1.1 (R7: lo revelado sigue revelado en todo el nivel), §1.2, §1.4 (`futil` y R10 f), §2 (la k del rótulo «Nivel N · intento k»), §3.1 (C9 de la tinaja), §4.1, §5, §8, §9.1 (M13), §9.2 (reglas 5, con el orden de respuestas de Morpho, 7 y 8), §11 (T1, T2, T3, T5 y T6, con qué es un intento y hasta cuándo sigue revelado lo revelado), §12 (etiqueta accesible y C9 de la tinaja: solo la boca y las capas ya reveladas), Parámetros (fila 9 Mecánicas) y Dependencias (fila 03: el 323 en la lista única de estrenos; completado el 2026-10-14 en T58).
- 03: §5, §6, §8, §9 y §13, más §11 (fila 201-400), «Preguntas abiertas» 5 y Dependencias (fila 02), con el 323 en la lista única de estrenos (completado el 2026-10-14 en T58); 04: §2.1 (la ranura de Revelar, también con la tinaja; completado el 2026-10-14 en T58), §2.3 (regla de uso y caso a: el orden de respuestas de Morpho), §2.4, §2.5, §2.7 (qué es un intento, la confirmación de reinicio y lo revelado), §5.1 y §6.1, más §2.6 (regla de uso: los estrenos que no se saltan), §4 (fila de los debuts) y Dependencias (fila 02), con el 323 en la lista única de estrenos (completado el 2026-10-14 en T58).
- 06: §3 (fila 43), §5.3 y Dependencias (fila 02), con el 323 en la lista única de estrenos (completado el 2026-10-14 en T58).
- 07: §2.2 (la skin 9, sin el nombre «Tinaja»), §4.3, §4.5, §5.6, §7.4 y §7.6 (la ranura y el icono de Revelar, también con la tinaja; completado el 2026-10-14 en T58), §8.1, §8.2, §9.2, §11 y Dependencias (fila 02: el 323 en la lista única de estrenos; completado el 2026-10-14 en T58); 08: §4.1, §4.4, §5.1, §8.2 y §9 (con la nota de que `lvl_murky_max_hidden_units` no rige la Tinaja); 09: §1.1 (#32; y #4, el 323 en la lista única de estrenos, completado el 2026-10-14 en T58), §1.2, §3.1 (la skin 9, sin el nombre «Tinaja»), §5 y §7 (R36).
- `docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md` (nota de la excepción) y la propuesta de la tinaja (marcas).

**Alternativas descartadas.** Q-Ti1-B (solo como Aguas turbias, en Difícil o Muy difícil); Q-Ti2-B (regla de color); Q-Ti3-B (directamente en la 1.1); Q-Ti4-B («Tinajón»); Q-Ti5-B (enseñar las frutas desde el estreno); Q-Ti6-B (lo revelado al sacar se pierde al reiniciar); Q-Ti7-B (la tinaja se inclina). Las variantes V2 (tinaja grande) y V3 (tinaja de la casa) quedan como variantes posteriores (d.4 de la propuesta).

**Cómo revertir.** `mech_enabled.tinaja` = false con `level_overrides` en la 1.0 (pregunta 31, D-020). Revertir la decisión exige una decisión nueva que sustituya a D-019.

**Archivos afectados.**
- **Documentación** (2026-10-14, T54): este registro y los de «Qué cambia en el GDD».
- **Código:** núcleo (T55, hecho): `Packages/com.amazonfill.core/Runtime/AF.Core/{Mechanics, Rules, Board}` y `AF.Solver/{Bot, Murky, Search}`, con sus tests. Previsto: `tools/level-generator`, `tools/level-qa`, `schemas/af-level-1.schema.json` y `tools/validate_schema.py` (T56), y la lista de c.6 en Unity.

---

## D-020 · Respuestas del 2026-10-13 a las preguntas 27-38 y 46 de `docs/GDD.md` §6.3

- **Fecha:** 2026-10-13. Registrada el 2026-10-14 (T54).
- **Estado:** decidida por el dueño.
  - Preguntas 27-38: el dueño contestó «Sí, aplica las recomendadas también»; gana la recomendación de cada fila.
  - Pregunta 46: el dueño aplicó las recomendadas el 2026-10-13, así que queda en la opción (a). Así la implementó T51 (`DeadEndProbe`, integrada en `1ad712c`).
  - Documentación aplicada el 2026-10-14 (T54): en `docs/GDD.md` §6.3 esas filas pasan a decididas, con la última columna al día.
- **Sustituye:** los [P] y las opciones abiertas de esas trece preguntas, y la primera redacción de D-014 (punto 1: «con vertidos útiles»).
- **No sustituye:** la 45 ni las 18, 19 y 21-26, que siguen abiertas (la 20 se decidió el 2026-10-11). La 45 se decidió el 2026-10-14 (D-022).

**Decisión.**
1. **27 → (a).** Lluvia y Piraña entran en la 1.0, con sus estrenos en el 143 y el 183. Cumple el criterio de la #6 de §6.1 (evidencia C3-3 del núcleo); la confirmación prevista para el cierre de la Fase 1 queda dada.
2. **28 → (a).** «Zafra», solo como nombre [P] del hito 420, pendiente del asesor cultural; no es un evento de 06.
3. **29 → se ratifica.** Mientras no haya Morpho ni Pororoca, Revelar va en la 3.ª ranura (Marea, Cuia extra y Revelar), con la regla del orden relativo de 07 §7.4; con los cinco comodines vuelve a la 5.ª.
4. **30 → se ratifica** el formato [P] de `pack_schema` 2: `chica`, `alta`, `matapi` y `murky` en `intro_mechanic`, y `difficulty.par_approx`, que queda sin uso (con Q-C el par de la Piraña es exacto).
5. **31 → (b).** En la 1.0, con `mech_enabled.<id>` = false, los niveles que usan esa mecánica se sustituyen con `level_overrides` (08 §4.4); el formato de respaldo por slot se fija con el build de la 1.1. Tarea: el cliente aún no lee `mech_enabled`.
6. **32 → (b).** La temporada del ciclo (aguas altas o bajas) sale de una **tabla del cliente por ciclo, copiada del calendario versionado** (`tools/level-generator/specs/campaign_v1.json`); no toca el formato del pack. Tarea de código.
7. **33 → (a).** Se rehace la tabla d.5 de novedades de 201-1000 (con Q8 c y b′ de D-007, la Tinaja en el 323 de D-019 y las variantes), con el **registro versionado de combinaciones publicadas** junto al calendario, **antes de generar 201-700**. Es tarea de planificación (tech-lead) y del pipeline (level-pipeline-dev).
   - *Actualización del 2026-10-14 (D2):* hecha, en la propuesta de mecánicas (`docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md`), d.5:
     - el calendario de 201-700, nivel a nivel, en dos variantes: **A**, con la Tinaja (estreno en el 323), y **B**, sin ella, que es la salida B de D-019, con un respaldo para cada celda. Las dos cumplen la regla de 60 niveles, con 34 como mucho entre dos primeras veces;
     - la tabla de 701-1000, orientativa y sin generar;
     - el formato [P] del registro de combinaciones, `tools/level-generator/specs/combinations_registry_v1.json`, que genera la tarea P4, con una entrada por conjunto de códigos nuevo.

     Se aplica la recomendación (la A, con la B guardada) por D-023, punto 10. La B se guarda en los tres mundos de 301-700 (tareas P7b, P7c y P7d), y la condición de D-019 se comprueba al integrar los mundos 7-9 en el cliente (tarea C1b). Cambian 03 §11, §12 y §17.
8. **34 → (a).** Se ratifican P1-P3 en lugar de la prueba del anexo B, con la excepción del Remolino en P3.
   - **Encaje con D-014:** D-014 no cambia esas propiedades del núcleo: P1-P3 se siguen probando igual (`FutilePropertyTests`, antes de cada estreno). Lo que hace D-014 es detectar **en la partida** (`LevelSession`, `DeadEndProbe`) el bucle sin salida, cuando el cierre de estados alcanzables con todos los vertidos legales se agota con ≤ 64 estados sin victoria. Así, el vaivén útil del Remolino con Q-B y el de la cuia ya suelta de la Tinaja ya no retrasan el modal cuando no llevan a ninguna parte. P1 se sigue exigiendo para la regla ampliada (`DeadEndProbePropertyTests`).
9. **35 → (a).** Del revelado rítmico solo queda la burbuja del revelado de Aguas turbias (02 §4.1); no hay Travesía con revelado por contador.
10. **36 → (a).** Ningún texto por debajo de 12 sp: modales (`ModalKit`), avisos (`Toast`) y la tarjeta «¡Miquito a la vista!» (`SightingCard`) pasan a 12 sp como mínimo, con dos líneas o cajas que crecen donde no quepa. Tarea de código pendiente.
11. **37 → (b).** El vertido que gana cuenta en «ninguno > 8 ms». En el teléfono (hoja del 2026-10-13): 216 vertidos en 14 niveles, 11 de ellos `sync`, con un máximo de 4,83 ms y un p95 de 1,48 ms; ya son más de los ≥ 200 que pedía la medida. Si un día alguno pasa de 8 ms, (c): entregar la victoria al escritor en el toque y confirmarla en síncrono al abrir la pantalla de victoria.
12. **38 → se ratifican** los cinco [P] de la tanda 1: los miquitos que no llegan a su sitio en la intro corta siguen posados desde t = 0; volver de segundo plano no cancela la espera del avance tras la Cosecha doble; el Brote sobresale 1,0 u; la ficha de pareja del Remolino (13 dp) no deja 2 dp de aire con el aro; y la espera al escritor en los guardados síncronos tiene un tope de 1.000 ms.
13. **46 → (a).** El cierre de D-014 recorre **todos los vertidos legales, fútiles incluidos**, con el mismo tope de 64 estados [P] y solo en la partida del cliente. P1 se cumple por construcción: un cierre completo sin victoria es un tablero irresoluble. En el 66, Aguas estancadas sale justo tras el vertido 9 (`1>5`). No depende de `futile_rule_enabled`: el kill-switch devuelve R13 al criterio clásico, pero el cierre sigue. Implementado en `DeadEndProbe` (T51).

**Consecuencias.**
- **Tareas que dejan:** el cliente lee `mech_enabled` y sustituye con `level_overrides` (31); la tabla de temporada del cliente (32); la tabla d.5 y el registro de combinaciones, antes de generar 201-700 (33); los mínimos de 12 sp (36).
- **Sin cambios** en el núcleo, el solver, los packs, la economía ni Remote Config.
- **Siguen abiertas en 6.3:** las 18, 19 y 21-26 (pasos 1 y 2), la 45 (D-013) y las nuevas 47-50. *Actualización del 2026-10-14 (T58):* la 45 y las 47-50 quedaron decididas, con la 51, en D-022; siguen abiertas las 18, 19 y 21-26.

**Qué cambia en el GDD** (aplicado el 2026-10-14, T54): `docs/GDD.md` (estado, §2 #9 y #12, §5, §6, §6.1 #6 y §6.3); 00 §5.1 (pregunta 29); 02 §1.4 (pseudocódigo, R13, R14 y R16), §5, §9.2 (regla 3), §12 y «Preguntas abiertas» 5 y 14-16; 03 §12 y «Preguntas abiertas» 10; 04 §2.5 y §5.1; 06 §5.2; 07 §2.2 (regla 6), §5.5, §6.4 y §7.4 (pregunta 29); 08 §5.4, §9 (`mech_enabled`) y «Preguntas abiertas» 11 y 12; 09 §1.1 #5, §2.3 (fase 1), §3.1 (fila «M6 Lluvia y M7 Piraña») y §8 (decisión 12); `docs/dev/traspaso/README.md`.

**Alternativas descartadas.** 27 (b); 28 (b); 31 (a); 32 (a); 33 (b); 34 (b); 35 (b); 36 (b); 37 (a) y (c); 46 (b) y (c).

**Cómo revertir.** Con una decisión nueva que sustituya a D-020, pregunta a pregunta.

**Archivos afectados.**
- **Documentación** (2026-10-14, T54): este registro y los de «Qué cambia en el GDD».
- **Código:** la 46, en `Packages/com.amazonfill.core/Runtime/AF.Core/Session/DeadEndProbe.cs` (T51, hecho); las tareas de las preguntas 31, 32, 33 y 36, sin asignar.

---

## D-021 · Miquitos por los lados en los tableros de 3 filas, alternando

- **Fecha:** 2026-10-14.
- **Matizada por D-046 (punto 3, 2026-10-23):** con 3 filas el tablero deja una franja de 56 dp sobre el panel (Q-T6T-4, decidida por el dueño el 2026-10-24) y la tropa sale en su rama en todos los niveles de 3 filas, en el primer intento; la tropa lateral de esta decisión (y el dosel) queda solo de respaldo donde esa franja no cabe sin bajar la cuia de 28 dp (16:9, 360 × 640 dp, y la Cuia alta de 6 en lienzos bajos). Implementado en la rama `spark/t6-tropa-3filas` (`BoardLayoutResult.TroopBand`).
- **Estado:** decidida por el dueño. La frecuencia y las restricciones son [P] de este registro y las puede ajustar el dueño. **Implementada en la rama, pendiente de la prueba en el teléfono** (tanda 4, TL1, rama `tanda4/o2-unity-b`):
  - `IntroPerch.Side` en `IntroChoreography`, `TroopLayer` y `MonkeyRig` (pose `Climb`); la frecuencia en `IntroSideSchedule` (`SideEveryN` = 3 [P]) con el ordinal del nivel entre los de 3 filas de su tramo (`GameScreen.ThreeRowOrdinal`, con la regla de filas del tablero);
  - con los paquetes 1-200 a 360 × 780 dp: 82 niveles de 3 filas en el 86-200, 28 con la tropa lateral (los primeros de los mundos 4 y 5: el 86 y el 143) y 54 en dosel;
  - un miquito por lado; cabe sin encoger por debajo de 28 dp, salvo la franja de la barra de la Crecida (ver abajo): con 4 cuias por fila queda un margen libre de 22-22,5 dp por lado y los miquitos miden 29-32 dp (churuco colgado y huapo trepando, a 1080 × 2340 y a 720 × 1600 px). Con 5 o 6 cuias por fila (13-16 cuias) el margen baja a 12-14 dp y no cabe: esos niveles, si los hay, vuelven a la regla de D-010 (hoy ninguno del 1-200);
  - tests: `IntroSideTroopScheduleTests` (EditMode), `IntroSideTroopTests` (PlayMode: calendario y T idénticos al dosel, ≥ 8 dp de toda cuia en todos los fotogramas muestreados, nada sobre el HUD ni la bandeja, nada recibe toques, 0 B por fotograma) y las capturas `IntroSideTroopSnapshotTests`.
  - revisión de TL1 (2026-10-15): la mano que lanza va por debajo del hombro (trepando, delante del pecho; colgado, la mano libre bajo la cabeza), nunca un lanzamiento por encima del hombro (guion §4.4; 07 §5.1, fase d); el colgado (churuco) cuelga justo debajo de su liana, que va a media anchura del margen, sobre la banda exterior del marco; perfil de luz (#F3E6C8, alfa 0,8, 0,8 dp hacia fuera y hacia arriba) y halo (18 × 30 dp, #F6E7C1, alfa 0,30, recortado en la línea de 8 dp de la cuia) para que la especie se distinga del marco; la liana del que trepa acaba en punta con 2 hojas;
  - **decidida el 2026-10-19 en la (a) (D-030, pregunta 3): se acepta la excepción, siempre de lado y por debajo del hombro; anotada en el guion §4.4 y en 07 §5.1. Antes, pendiente del dueño (revisión de TL1, 2026-10-15):** la fruta lanzada por la tropa lateral sale de la mano con una velocidad horizontal de unos 100 a 1.150 dp/s (a 360 × 780 y 360 × 800 dp, el 86 y el 143), por encima del tope de 250 dp/s de la suelta de la rama (guion §4.4; 07 §5.1, fase d). Con el calendario del dosel y la mano en el margen lateral no cabe por debajo: la columna de dentro de cada lado queda a unos 120 dp de la mano, que a 250 dp/s pide ≥ 480 ms de vuelo, y en la corta las 15 frutas de una mano caen entre 550 y 693 ms (una suelta cada 26 ms, una fruta en la mano cada vez). Pregunta: ¿se acepta para la tropa lateral una excepción al tope (impulso horizontal de hasta ~1.150 dp/s, siempre de lado y por debajo del hombro)? Si se acepta, se anota en el guion §4.4 y en 07 §5.1, citando D-021;
  - **decidida el 2026-10-19 en la (c) (D-030, pregunta 4): la barra aparece en T, al habilitarse el toque; hoy no existe en el cliente, así que es un requisito para la tarea que la construya (07 §7.4). Antes, pendiente del dueño (revisión de TL1, 2026-10-15):** el miquito de la derecha pisa la franja de la barra de la Crecida (07 §7.4: x 348-356, y 120-560, visible desde el nivel 25, así que en todos los niveles con tropa lateral) mientras baja y lanza; el punto 4 dice que nunca tapa el HUD. Si se excluye esa franja, a la derecha quedan 10,5 dp entre la línea de 8 dp de la cuia y la barra, y un miquito de 28 dp necesita 19,8 dp trepando o 21 dp colgado: no cabe sin encoger. Hoy la barra aún no está en Unity. Opciones para el dueño: (a) solo el miquito de la izquierda cuando la barra está visible; (b) que la tropa pueda pisar la franja de la barra durante la intro, como el marco (la intro no admite toques); (c) que la barra aparezca en T, al habilitarse el toque.

**Contexto.**
- D-010, punto 4, pasa la intro a **modo dosel** (sin tropa) cuando no hay una franja libre de 44 dp entre el HUD y el panel, que es lo que ocurre con 3 filas (11 cuias o más, D-006).
- Medido en los packs el 2026-10-14 (`docs/dev/traspaso/validacion-niveles-2026-10-14.md`): hay tableros de 3 filas desde el 86, y lo son **77 de los 100 niveles del 101-200**. La especie del mundo 4 (churuco) y la del 5 (huapo colorado) casi nunca se ven, y con más niveles irá a peor. Eso choca con la ampliación de D-010 (especies que van apareciendo, elenco de ~50).
- **El dueño, al verlo en el teléfono:** «Podríamos alternar, podemos mostrarlos de otra forma, por los lados, bajando y tirando; no en todos los niveles, pero sí para tenerlos presentes».

**Decisión.**
1. **Tropa lateral** en los tableros de 3 filas: los miquitos de la especie del mundo bajan por los lados de la pantalla, desde el follaje de las esquinas (trepando o colgados de una liana, según su modelo de suelta), y desde ahí lanzan la fruta a las cuias.
2. **Alternando, no en todos los niveles.** El resto de los niveles de 3 filas sigue en modo dosel. Frecuencia [P]:
   - siempre en el primer nivel de 3 filas de cada mundo (y de cada especie nueva del elenco);
   - después, en 1 de cada 3 niveles de 3 filas, de forma determinista por el número de nivel;
   - nunca en los reintentos, que van en dosel como hoy (D-010).
3. **Se mantiene de D-010:**
   - T (800 ms desde el 21) y las fases a-d no cambian; el toque se habilita a la misma hora;
   - la tropa entra mientras el río llena, lanza la fruta con el calendario de siempre y se va en cuanto se habilita el toque;
   - con 3 filas **no reposa ya en juego**, para no tapar cuias ni botones mientras se juega;
   - las reglas de representación (punto 6: sin rasgos humanos ni gestos de señalar);
   - D-002: no miran, no señalan y no reaccionan a una cuia;
   - la especie cambia qué se ve, nunca cuánto dura.
4. **Sustituye:** el descarte de «lianas laterales con 3 filas» del guion de la intro (§3, «Posaderos») y de D-010 (punto 7 y «Alternativas descartadas»). La razón del descarte (la distancia de 8 dp a las cuias) se convierte en restricción:
   - durante la intro, que no admite toques, la tropa puede pisar los márgenes laterales y el marco del panel;
   - nunca tapa una cuia (≥ 8 dp de cualquier cuia, la distancia del guion de la intro, §3, «Posaderos»), ni el HUD, ni la bandeja.
   - Si con el ancho real no cabe (margen lateral total de 24 dp, 12 por lado, 07 §4.1), **para y pregunta** con capturas antes de encoger los miquitos por debajo de `MinMonkeyDp` (28 dp).

**Consecuencias.**
- **Código** (Unity: `IntroChoreography`, `TroopLayer`, `MonkeyRig` y la decisión de posadero), con tests:
  - mismo calendario de fruta y misma T;
  - 0 B por fotograma;
  - nada recibe toques;
  - ninguna pieza a menos de 8 dp de una cuia;
  - la frecuencia, determinista.
- **Analítica y Remote Config:** sin cambios.
- **Arte:** usa las mismas piezas por especie; la liana lateral es procedural.
- **GDD:** 02 §4.2 y 07 §6.4 (posaderos y frecuencia), el guion de la intro (§3 y §7.1) y la tabla de D-010.

**Cómo revertir.** Volver al modo dosel en todos los tableros de 3 filas, siempre con una decisión nueva que sustituya a D-021.

---

## D-022 · Respuestas del 2026-10-14 a las preguntas 45, 47-50 y 51 de `docs/GDD.md` §6.3

- **Matizada por D-046 (punto 3) y su Q-T6T-3 (decidida por el dueño el 2026-10-24: «Bajar y mover avisos»):** con 3 filas, la franja de la tropa y el suelo bajado 20 dp, el aviso del tablero que ya no cabe entre el panel y la bandeja va a la franja sobre el panel (legible, D-011 punto 2), y las burbujas «Te faltan {n}» que pisarían el panel, también; el compromiso de la pregunta 47 (junto a la ficha o tapándola entera) sigue donde no hay franja (16:9).
- **Fecha:** 2026-10-14. Registrada el 2026-10-14 (T58).
- **Estado:** decidida por el dueño.
  - Preguntas 45 y 47-50: para todo lo pendiente, el dueño tiene dicho «Sí, aplica las recomendadas también» (2026-10-13), y lo aplicó a las preguntas del plan de la tanda 4 el 2026-10-14. Gana la recomendación de cada fila. La 45 venía del 2026-10-13 (D-013) y las 47-50, del 2026-10-14 (tanda 3 y registro de D-016 a D-019).
  - Pregunta 51, nueva: este registro la abre y la cierra. Es la contradicción de gasto de D-018 entre Q5-A y Q12-A. Se le preguntó al dueño con tres opciones: (a) subir la partida; (b) traducir los datos con IA y revisión nativa, dentro de 1.300-3.000 €; (c) traducción humana solo de 1-200. Respondió, literal: «traducción con ia». Se registra como (b).
  - Documentación aplicada el 2026-10-14 (T58): en `docs/GDD.md` §6.3 las seis filas pasan a decididas, con la última columna al día; la 51 entra ya decidida.
- **Sustituye:**
  - los [P] y las opciones abiertas de esas seis preguntas;
  - en D-018 (Q5-A), la traducción humana completa de los datos de fruta y fauna: pasan a borrador por IA con revisión nativa, como la UI;
  - en la propuesta del Mapa (§6.2 y §9), que la línea de fruta desplazada «pasa a la victoria siguiente» (la opción (d) de la 45): no sale en la victoria y queda en la ficha;
  - en la propuesta de Frutas (§2.2, regla 7), la línea 10 del açaí en el bocadillo del Mapa del 10: queda en la ficha.
- **No sustituye:** D-013 (la llegada cada 10 niveles, con `ui_river_fact_every_n_levels` = 10); T6-A (D-013 y D-016: una sola línea de texto por victoria, y el dato que TiTino desplaza sale en la victoria siguiente); D-017 (la ficha de Q4-A, la cadencia de Q2-A y la ranura de alto fijo de Q7-A); Q12-A de D-018 (la partida de 1.300-3.000 €, que no sube); D-011 (la cola de avisos y su lectura); ni D-015.

**Contexto.** Las preguntas 45 (D-013: la llegada del Mapa, la línea de fruta y la frase de TiTino quieren el mismo hueco de la victoria del `10k`), 47 y 48 (los [P] de T52 y un [P] del paso 1, T14), 49 (claves y columnas de las tablas de textos: D-013 y D-017 frente a D-018) y 50 (los Datos del río del estreno de la Tinaja, D-019) quedaron abiertas en `docs/GDD.md` §6.3 con T50 y T54. La revisión de T54 encontró además que D-018 registraba a la vez Q5-A (traducción humana completa de los datos) y Q12-A (1.300-3.000 €): según la propuesta de idiomas (5.5), la partida de Q12-A supone borrador por IA y revisión nativa también en los datos, y la tanda 1 con traducción humana de todo, datos incluidos, cuesta ≈ 2.800-5.100 € (la opción C de Q12), así que las dos no cabían juntas. Es la pregunta 51.

**Decisión.**
1. **45 → (a): canales separados y una sola línea por victoria, con prioridad.**
   - La llegada del Mapa, cada 10 niveles, con `ui_river_fact_every_n_levels` = 10; la línea de fruta, con su clave propia, `ui_fruit_line_every_n_levels` [P] (default 1; 0 = apagada; 2 = una cada dos niveles, Q2-A de D-017; 08 §9).
   - **Prioridad:** frase de TiTino en sus tres hitos (fin de mundo salvo el 10, primer Difícil y primer Muy difícil) > dato de llegada > línea de fruta.
   - Lo que se desplaza: el dato de llegada desplazado por una frase de TiTino sale en la victoria siguiente (T6-A), en lugar de la línea de fruta de esa victoria; una línea de fruta desplazada no sale en ninguna victoria y queda revelada en su ficha, que se deduce del progreso, y la tarjeta de la parada enseña la fruta de la serie. En el `10k` sale la llegada con su dato; en los hitos de TiTino (todos son `10k`), habla él y la franja de llegada lleva solo la placa del lugar. Si TiTino no puede hablar, la línea es la siguiente de la prioridad.
   - En el nivel 10, la presentación de TiTino en el bocadillo del Mapa y el Dato del río 1 en su placa; la línea 10 del açaí, en la ficha.
   - Supone la ficha de Frutas en la 1.0, que ya está decidida (Q4-A, D-017).
2. **47 → se ratifican** los dos [P] de T52 (`d2ce284`):
   - el aviso frente a las burbujas «Te faltan {n}»: sube lo justo por encima de ellas; con 3 filas, si no cabe entre la repisa de abajo y las burbujas, se ajusta a su texto en el tramo de la repisa sin fichas y, si tampoco cabe, va de su ancho de siempre y tapa enteras las fichas que alcanza, sin pasar del pie de la fila;
   - el Remolino legible: la cuia tocada se traga el chorro con un remolino en su superficie y la pareja se lee por su remolino, sin luz sobre su líquido ni su cavidad (guía visual v2 §2.5).
3. **48 → (b).** Al tocar Revelar sin stock, sin saldo y sin vídeo, no sale ningún aviso: responde la burbuja «Te faltan {n}» que ya se ve sobre la ranura, con un latido y el color del precio sin saldo (con Reducir movimiento, solo el color), sin texto nuevo. La (c), una hoja inferior «sin saldo», se decidirá con la propuesta de Magic Sort cuando existan el Mercado y esa hoja.
4. **49 → (a): dos familias de claves y una columna de estado por idioma.**
   - Claves de UI en `snake_case` y sin puntos (glosario §10 y regla 1 de la propuesta de idiomas). Claves de contenido largo (datos de fruta, del Mapa y de fauna) con puntos, como las aprobó D-013 y las fija Frutas: `fact.<tinte>.<kk>`, `fact.<tinte>.tag`, `tint.<tinte>.name`, `map.stop.<lugar>.dato_NN` y `fauna.bradypus_variegatus.dato_<nn>`. La fauna que ya existe en los datos conserva `fauna_<id>_dato`.
   - El `fact_id` de la analítica, como lo fijan D-013 (la clave entera del dato de llegada) y Frutas (`<tinte>.<kk>`).
   - `strings.csv` y `facts.csv` llevan la cabecera de Frutas, `key,es-419,en,pt-BR,context,max_chars,status,status_en,status_pt-BR,notes`, con `max_chars` del español y la regla de 3.7 de la propuesta de idiomas para los demás. Se acepta además una columna `args` [P] al final, para las cifras de los textos largos (Q8-A: el texto lleva `{n}` y el valor va en `args`).
5. **50 → (a).** Los Datos del río candidatos del estreno de la Tinaja (de Pará: las cuias de Aritapera, las vasijas de Quatipuru-Mirim, el barro que enfría el agua e Icoaraci) entran como «Otro dato» de la tarjeta de la parada del estreno (k = 32, niveles 321-330), dichos con su lugar real y con el visto bueno del asesor. No añaden texto a la victoria ni al tutorial.
6. **51 → (b): los datos de fruta y fauna se traducen con IA y una revisión nativa**, igual que la interfaz, dentro de la partida aprobada de 1.300-3.000 € (Q12-A, que no cambia). Cambia la parte de los datos de Q5-A de D-018. Nunca se publica traducción automática sin revisión, y un dato sin traducir o sin revisar no se muestra (Q10-A).

**Consecuencias.**
- **Código** (tanda 4; sin paquetes, SDK, permisos ni versiones nuevas):
  - la victoria decide su única línea con la prioridad de la 45, en lógica pura de AF.GameFlow, y `ui_fruit_line_every_n_levels` vive como constante de `GameConfig` [P] hasta que haya Remote Config en el cliente;
  - las tablas de textos nacen con la cabecera de 08 §4.7 y las dos familias de claves (49);
  - Revelar sin medios deja de sacar el aviso por la cola (`GameScreen.ShowRevealMissing`) y responde la burbuja (48);
  - las paradas de 201-700 llevan en la k = 32 los datos de Pará como «Otro dato» (50), en estado `pendiente_asesor`;
  - la 47 no pide nada: ya está en el cliente.
- **Contratos:** el 32 (tablas de textos) ya tiene la forma de sus claves y sus columnas (`docs/GDD.md` §7.5).
- **Remote Config:** alta de `ui_fruit_line_every_n_levels` [P] en 08 §9. Sin cambios en el núcleo, el solver, los packs ni la economía.
- **Presupuesto:** la partida de traducción de la 1.0 sigue en 1.300-3.000 € (09 §5).
- **Siguen abiertas en 6.3:** las 18, 19 y 21-26 (pasos 1 y 2).

**Qué cambia en el GDD** (aplicado el 2026-10-14, T58):
- `docs/GDD.md`: versión 1.9, estado, §2 #21 y #30, §6, §6.3 (45, 47-50, la 51, nueva, y la referencia a la 47 desde la 41), §7.5 (contrato 32) y §9 (pasos 17 y 18).
- 00: §6.4 (Dato del río), §10 (forma de las claves), §11 (TiTino y Frutas del río) y Parámetros (`ui_fruit_line_every_n_levels`).
- 02: §4.1 (el Remolino de T52, ratificado), §4.2 (el aviso frente a las burbujas, ratificado, y Revelar sin medios), §6 (esquema, filas «Dato del río» y TiTino), §9.1 (M13: los Datos del río de su estreno) y Parámetros (fila 6).
- 04: §2.5 (Revelar sin medios).
- 06: §2.3 (paso 3: la línea 10 del açaí, en la ficha), §4.4 y Parámetros.
- 07: §2.2 (regla 6: los datos de Pará), §5.4 y §6.5.
- 08: §4.6, §4.7 (claves y columnas), §9 (`ui_fruit_line_every_n_levels`) y Preguntas abiertas 14 (cerrada).
- 09: §1.1 (#20 y #28), §5 (traducción) y §7 (R35).
- `docs/dev/traspaso/README.md`: §1 (estado del 2026-10-13 y del 2026-10-14), §6 (paso 4) y §7 (la fila de D-022 y las de D-013, D-015 y D-017 a D-019).

**Alternativas descartadas.** 45 (b), (c) y (d); 47: un aviso que no llegue a la repisa (más estrecho y en dos líneas, o arriba, en la franja del panorama), el aviso detrás de la capa de las repisas y, para el Remolino, un filo de luz de 4 dp dentro de la cavidad de la pareja; 48 (a) y (c); 49 (b); 50 (b) y (c); 51 (a) y (c).

**Cómo revertir.** Con una decisión nueva que sustituya a D-022, pregunta a pregunta. `ui_fruit_line_every_n_levels` = 0 apaga la línea de fruta en remoto.

**Archivos afectados.**
- **Documentación** (2026-10-14, T58): este registro y los de «Qué cambia en el GDD».
- **Código** (previsto, tanda 4): `Assets/_Project/Scripts/{GameFlow, UI}` (la línea de la victoria y la burbuja de Revelar), `Assets/_Project/Data/Loc/{strings.csv, facts.csv}` y los datos de las paradas de 201-700, con sus tests.

---

## D-023 · Decisiones de detalle de la tanda 4: preguntas de sus planes y [P] del plan consolidado

- **Fecha:** 2026-10-14. Registrada el 2026-10-14 (D1, tanda 4).
- **Estado:** decidida por el dueño; en implementación (tanda 4).
  - Para todo lo pendiente, el dueño tiene dicho «Sí, aplica las recomendadas también» (2026-10-13), y lo aplicó el 2026-10-14 a las preguntas de los dos planes de la tanda 4, como en D-022. Gana la recomendación de cada pregunta.
  - **Fuentes:**
    - el plan del Mapa del río (tech-lead, revisión 2, posterior a T50): 37 tareas (M1.0-M5.5), las preguntas P-M1 a P-M8 y cuatro críticas comprobadas;
    - el plan consolidado de la tanda 4 (tech-lead, revisión 2, 2026-10-14): 101 tareas en 9 olas, su arquitectura común (los contratos comunes §1-§10), sus cuatro críticas, su lista de 30 decisiones que registrar y 18 acciones del dueño, de las que 12 son preguntas cerradas;
    - las notas de la tanda 4 de la sesión principal (2026-10-14: las respuestas del dueño a esas preguntas y la «Reanudación de la ola 1») y las revisiones de las tareas de la ola 1.
  - Los planes y las notas viven fuera del repositorio, en el espacio de trabajo de la sesión principal: **este registro es su copia**, y cada sección y cada punto citan su fuente. En las citas, «pregunta N» es la acción del dueño N del plan consolidado; «lista n.º N», el número N de su lista de decisiones que registrar; «arquitectura §N», la sección N de su arquitectura común; y «crítica N», su crítica N. Las críticas del plan del Mapa se citan con ese nombre.
  - Los detalles marcados [P] son del plan (tech-lead): el dueño los puede cambiar sin coste mientras no estén implementados.
  - Documentación aplicada el 2026-10-14 (D1, que absorbe la tarea M1.7 del plan del Mapa; «Qué cambia en el GDD»).
- **Sustituye:**
  - el archivo de textos del Mapa `Assets/_Project/Data/Map/river_texts_v1.json` (`af-river-texts/1`) que proponía P-M1 (a): los textos del Mapa van en `facts.csv` (contrato 32), como ya decía 08 §4.7 (punto 4);
  - en D-013 («Consecuencias», F2), «Python 3.14, solo biblioteca estándar» para todo `tools/river-map/`: los generadores siguen solo con la biblioteca estándar, y las pruebas validan los esquemas con `jsonschema`, que ya está en el stack (punto 32);
  - el nombre [P] del hito 80, «Puerto de Manaos», en la tabla d.4 de la propuesta de mecánicas y en el ejemplo de 07 §8.2: pasa a «Lago de Tefé» [P], como D-013 («Estado», implementación) ya pedía para `campaign_v1.json` (punto 21);
  - en el plan consolidado, lo que esperaba a la 45 y a la 50 (el árbitro en `Canon` «hasta la 45», la cadencia de la línea de fruta «fuera de 08 §9 hasta la 45» y el «Otro dato» de la parada k = 32 «reservado»): D-022 ya las decidió (puntos 22 y 38).
- **No sustituye:** D-013 a D-022, salvo la frase de D-013 (F2) citada arriba; la prioridad de la victoria y `ui_fruit_line_every_n_levels` (D-022, punto 1), las claves y columnas de las tablas de textos (D-022, punto 4) y los datos de Pará en la parada k = 32 (D-022, punto 5) siguen como están: aquí solo se dice cómo se implementan. No cambian Remote Config, la analítica, la economía ni las reglas del núcleo.

**Contexto.**
- El plan del Mapa dejó ocho preguntas cerradas, P-M1 a P-M8, y comprobó cuatro críticas. Su tarea de documentación, M1.7, llevaba al GDD lo que T50 no cubrió: la fila de 04 §5.2, los formatos de P-M1 y las claves de P-M2.
- El plan consolidado de la tanda 4 junta el Mapa, TiTino, Frutas del río, la base de idiomas, la Tinaja, las variantes de la 1.0 y los niveles 201-700. Deja 30 decisiones de detalle por registrar y 18 acciones del dueño: 12 preguntas cerradas y 6 acciones físicas.
- Las preguntas del plan consolidado que eran preguntas de `docs/GDD.md` §6.3 (la 45 y las 47-50) ya están en D-022; aquí van las demás.

**Decisión.**

*A. Respuestas a las preguntas del plan consolidado* (acciones del dueño del plan, con su numeración; respuestas del 2026-10-14, en las notas de la tanda 4).
1. **Pregunta 1 → sí.** G1: los límites del equipo, la regla de localización (`.claude/rules/localizacion.md`) y los comandos del Mapa en los agentes, las reglas y `CLAUDE.md`.
2. **Pregunta 2 → sí.** S12 va en un solo PR con el rol de `sdk-integrator`: AF.Save (ajustes, estado de TiTino y `progress.map_seen_level`), `IDeviceLocale` e IAP por clave (los nombres de la tienda, por clave de texto).
3. **Preguntas 3-6** (las 49, 47, 48, 45 y 50 de `docs/GDD.md` §6.3): en D-022, también la columna `args` [P] que preguntaba la 3 (D-022, punto 4).
4. **Pregunta 7 → sí.** Los textos del Mapa van en `facts.csv` (contrato 32; 08 §4.7), no en `river_texts_v1.json`: así entran en el flujo de traducción y en la compuerta de release. El bloque `map.*` de `facts.csv` lo regenera solo `tools/river-map/stops.py`.
5. **Pregunta 8 → se firma** la parte de las variantes de `pack_schema` 3:
   - `hoja_atada` con `key`;
   - `brote` con `n` ∈ {1, 2};
   - `flor` con `key` y `key2`, distintas;
   - `intro_mechanic` con `hoja_atada`, `brote_n2` [P], `flor` y `liana_n` [P].
   Con la parte de la tinaja (`tinaja`, `cucharon` e `intro_mechanic` `tinaja`, aprobada con D-019), `pack_schema` 3 entra en dos pasos, siempre antes de publicar ningún pack 3 (contrato 21; glosario §11; lista n.º 20; arquitectura §6). Los packs 1-200 no cambian.
6. **Máscara de la tinaja en `relief` (pregunta de la tarea P2a; notas de la tanda 4, «Reanudación de la ola 1») → (a).** Con `pack_schema` 3, `relief_board` admite `hidden` (el mismo `$defs/hidden`, como mucho 4 pares) si y solo si sus `attrs` llevan la tinaja, con la máscara de todas sus capas menos la boca; las variantes de Aguas turbias siguen sin `hidden`. Lo cambian el esquema, el generador y la frase de 08 §4.1 (P1), y lo comprueban level-qa (P3) y el cliente (TJ1). Es el contrato 21, y ningún pack 3 se ha publicado.
7. **Pregunta 9 → no.** La Cuia de la casa (V3 de la tinaja) no entra en la 1.0: D-019 la deja como variante posterior. La tarea K4 sale del plan (lista n.º 23).
8. **Pregunta 10 → sí.** Mientras EN y PT-BR no estén completos, el idioma automático se queda en es-419 [P]; si el teléfono no da región, las cifras van con es-CO [P].
9. **Pregunta 11 → (a)**, solo si las medidas de la ranura (punto 24) dicen que la llegada no cabe en el alto de la línea de fruta: se compacta la llegada (la fila del agua más una placa con lugar y dato, sin bienvenida aparte).
10. **Pregunta 12 → la recomendación de D2.** Cuando la tarea D2 entregue la tabla de novedades de 201-1000 con sus dos variantes (con la tinaja y sin ella) y 701-1000 orientativa, se aplica la variante que recomiende (pregunta 33, D-020).
11. **Preguntas 13-18:** son acciones del dueño, no decisiones:
    - el teléfono de clase A antes de cerrar la ola 4 (pregunta 44, D-015);
    - curar retos e hitos por mundo;
    - los encargos al artista, al asesor y al abogado;
    - las descargas de Natural Earth y HydroRIVERS (D-013, punto 5);
    - la prueba en el teléfono al cerrar cada ola;
    - aprobar los merges.

*B. Mapa del río* (plan del Mapa, con la recomendación de cada pregunta, la (a); en el plan consolidado, lista n.º 1-10, en el orden de los puntos 12-21).

12. **P-M1:** los extras [P] de los contratos 29 y 30, en 08 §4.5:
    - en las paradas: la raíz `{format, source, stops}`; `km_home`, `answers_world_question` y `country_name`; los tres nombres por convención, sin campo propio y con la forma de claves de la 49 (a): `map.stop.<id>.name`, `map.stop.<id>.river` y `map.country.<iso2>.name`; `map.stop.<id>.country` no es uno de ellos, sino la forma de la clave del campo `country_name` cuando no es null (en n = 13); `bg.code` con `codigo:<pieza>` o `kit:<pieza>`; `flag_raise` = BR en n = 0 (la bandera del primer Mapa); y `bg.pieces` cuando se pinten los kits;
    - en el Mapa: `labels[]` (los países en su región), `viewpoints[]` (los miradores, con `reveal_on`) y `passing[]` (los puntos de paso, con su dato);
    - la codificación de `pts`, `verts` y `tris`: base64 (RFC 4648) de uint16 little-endian, con el primer par absoluto y los demás como deltas módulo 65.536 (`tris`, índices absolutos);
    - los esquemas `schemas/af-river-stops-1.schema.json` y `schemas/af-river-map-1.schema.json` son, con 08 §4.5, la copia única;
    - **matiz:** sin `river_texts_v1.json` (punto 4).
13. **P-M2:** diez claves de UI del Mapa [P] en el glosario §10, pendientes del traductor y del asesor: `map_view_world`, `map_view_basin`, `map_label_andes`, `map_label_atlantic`, `map_back_to_level`, `map_open`, `map_card_other_fact`, `map_card_source`, `map_facts_count` y `map_l10_intro`. «Andes ↑» y «Océano Atlántico ↓» se pintan en Baloo 2, que tiene las flechas; el ✓ de la pregunta del mundo es un icono procedural, porque ninguna de las dos familias lo tiene.
14. **P-M3, provisional:** en los hitos de T6-A, la llegada lleva su dato en todos los `10k` (`ArrivalEverywhere`) solo mientras no exista la caja de TiTino. Al fusionar su tarea (TI6a), el valor por defecto pasa a `SlothHito`, porque T6-A está decidida (D-013, punto 15; D-016, punto 5), y la 45 también (D-022).
15. **P-M4:** si en la victoria del 10 se pulsa Inicio o se cierra la app, la secuencia del Mapa del 10 (06 §2.3) sale una vez al entrar al inicio, con `max_level` ≥ 10 y `progress.map_seen_level` < 10. No hace falta estado nuevo.
16. **P-M5 (retirada por D-052, 2026-10-25: el arte por código es el final y sale en release):** el arte provisional del Mapa (las siluetas de héroe, las banderas simplificadas y el retrato y la ventanita procedurales de TiTino) solo sale en el Editor y en los builds de desarrollo (`MapArtRule`, como `FaunaArtRule`). En release no se pinta hasta que exista su PNG, y nunca va en la ficha, las capturas ni la UA.
17. **P-M6:** el 31 lleva el rótulo del lugar, por la cola de avisos y después de su presentación de hito (el tutorial de 02 §13 es 1-12).
18. **P-M7:** la placa «{país} te da la bienvenida» (`map_country_welcome`) va en la franja de la victoria del 80 y del 140, antes de la placa del dato; el banner del 81 y del 141 solo iza la bandera.
19. **P-M8:** el guía del Mapa se deduce del mundo (el bufeo boliviano en los mundos 7 y 8) y el relevo ocurre en el punto de paso `teotonio` del tramo, sin campo nuevo en el contrato 30.
20. **Las críticas del plan:**
    - no se abre una pregunta para el choque entre la llegada y TiTino: es la 45 (D-022);
    - «río abajo» no se edita: T50 ya lo corrigió. El grep en `docs/gdd` y `docs/GDD.md` (2026-10-14) solo da menciones históricas («antes, "río abajo"») y, en 07, los troncos de la Palizada, que sí van río abajo;
    - el dato de llegada del 200 es `map.myth.source`, el `arrival_fact` de la parada 20 (Q16-A);
    - la tolerancia de la geometría es «≤ 2 dp de su vista (≤ 15 km en la vista Cuenca)», con rutas que no cruzan tierra.
    - *Actualización del 2026-10-19 (D-028, pregunta 65):* con la geometría de producción, la vista Mundo pide ≤ 1 km a ≤ 100 km de una parada (`MundoZone`), con las excepciones medidas aceptadas tal como están (08 §4.5).
21. **Hito 80, «Lago de Tefé» [P]**, en `tools/level-generator/specs/campaign_v1.json` (tarea M1.9; es solo metadato y los packs no cambian), en la tabla d.4 de la propuesta de mecánicas y en el ejemplo de 07 §8.2 (D-013, «Estado», implementación). El 80 se juega en la parada 7, el lago de Tefé, no en Manaos. Queda pendiente del asesor, como todo nombre de hito.

*C. La victoria* (plan consolidado: arquitectura §2 y lista n.º 11-13).

22. **Árbitro [P]** `VictoryLineArbiter` (AF.GameFlow, sin motor ni asignaciones): decide la única línea de cada victoria con la prioridad de la 45 (D-022).
    - Lleva el interruptor `RiverFactPolicy` {`Canon`, `SingleKey`}. `Canon` es la (a) y el valor por defecto; `SingleKey` (la (b): una sola clave y la llegada solo con la placa del lugar) queda como alternativa apagada.
    - La cadencia de la línea de fruta es `ui_fruit_line_every_n_levels` [P] = 1, ya en 08 §9 (D-022); en el cliente, una constante de `GameConfig` hasta que haya Remote Config.
    - Fuente: lista n.º 11 y arquitectura §2; lo que esperaba a la 45, ya decidida (D-022).
23. **Cuándo habla TiTino en un hito** (`SlothCanSpeak`): solo si `ui_sloth_enabled` es true, si sus mensajes están activos (`settings.sloth_messages`) y si cabe su caja (≥ 104 dp; propuesta de TiTino, d.4), medida antes de decidir la línea. Si no puede hablar, el hito es una victoria normal y la llegada lleva su dato (02 §6). Fuente: lista n.º 12 y arquitectura §2.
24. **Ranura del Dato del río:** alto fijo, el de la línea de fruta más larga (Q7-A de D-017), según el idioma, la escala de texto y el ancho. Se mide con cadenas sintéticas del peor caso: 90 caracteres en es-419 y 117 en en, pt-BR y en-XA. La llegada tiene que caber en ese alto; si no cabe, el punto 9. Fuente: lista n.º 13 y arquitectura §2.

*D. Idiomas y textos* (plan consolidado: arquitectura §1 y §3 y lista n.º 14-17 y 25; notas de la tanda 4 y revisión de la tarea L2a).

25. **Cifras de los textos largos:** van con `{n}` y el valor en la columna `args` [P] (D-022, punto 4; Q8-A). Ningún texto de `facts.csv` lleva cifras con separador ni decimales, y una prueba lo comprueba. Fuente: lista n.º 14 y arquitectura §1 («Formato»).
26. **Resolución del idioma:**
    - orden: el extra de desarrollo `af_lang` > `settings.language` (si no es `auto`) > el teléfono > `en`;
    - es-* → es-419, pt-* → pt-BR y el resto → en, solo hacia idiomas completos (`Loc.IsComplete`); si no lo es, es-419 [P] (punto 8);
    - el idioma del teléfono se lee con `java.util.Locale` por el JNI de Unity, en AF.UI, sin plugin ni permiso, con `CultureInfo` de respaldo; sin región, es-CO [P] (punto 8);
    - los números usan una tabla propia de separadores por región (Q8-A), con U+00A0 en lugar de U+202F;
    - fuente: lista n.º 15, arquitectura §1 («Idioma» y «Números») y crítica 4 (la región del teléfono).
27. **Compuerta de release de los textos** (08 §4.7) en el build (`LocReleaseGate`, tarea B1): avisa con `com.amazonfill.dev` y falla con cualquier otro `applicationId` si falta la UI de un idioma ofrecido o hay texto cultural sin `aprobado_es`. Fuente: lista n.º 16 y arquitectura §1 («Estados»). *Actualización del 2026-10-14 (FIXD):* también falla si una cadena de UI de un idioma ofrecido distinto del español no tiene su `status_<idioma>` = `revisado_nativo` (08 §4.7; D-018, Q5-A, y D-022, punto 6: nunca traducción automática sin revisión en algo que vea el jugador).
28. **Escala de texto de desarrollo y pruebas [P]** (`UiBuilder.TextScale` con `LaunchOptions.FontScaleOverride`), para probar al 130 %; la escala de fuente de Android aún no llega al juego. Fuente: lista n.º 17 y arquitectura §3 («Escala de texto»).
29. **Pseudolocalización `en-XA`:** alarga un 35 % [P] (08 §14.2 pide +30 % como mínimo, y la propuesta de idiomas, §3.7, da +30-40 % en las frases largas), con corchetes y solo letras de Latin-1, y solo en desarrollo y en las pruebas. Fuente: arquitectura §1 («Idioma»), la tarea L2a (punto 7) y su revisión, que pidió registrar el choque del 35 % con el +30 % de 08 §14.2.
30. **`glossary.csv`** se crea en su propia tarea (L2c; contrato 32). Fuente: lista n.º 25 y arquitectura §1 («Tres tablas»).
31. **Orden de las filas de las tablas de textos: ordinal** por clave (`sorted()` en Python y `StringComparer.Ordinal` en C#), en UTF-8 sin BOM y con LF (`.claude/rules/data-json.md`, G1; 08 §4.7). Fuente: las notas de la tanda 4 («Reanudación de la ola 1»), que sustituyen el «orden alfabético» de las especificaciones, y la revisión de G1.

*E. Herramientas, packs y núcleo* (plan consolidado: arquitectura §6 y §7, crítica 2 y lista n.º 18-22 y 28).

32. Los esquemas del Mapa se validan en `tools/river-map/test_schemas.py` con `jsonschema`, y `tools/validate_schema.py` sigue siendo solo de `af-level/1`. Los generadores de `tools/river-map/` usan solo la biblioteca estándar. Fuente: lista n.º 18 y arquitectura §7 (`tools/river-map`); sustituye a D-013 F2 en este punto («Sustituye»).
33. Las pruebas de `tools/` van como fixtures (`specs/tests/*.json` y `fixtures/QA-*.json`) y modos `--self-test`, sin proyectos de tests nuevos en la solución. Fuente: lista n.º 19 y arquitectura §6.
34. En el núcleo, los códigos [P] de las variantes, que no se persisten: 10 para la Hoja atada y 11 para la Flor en `MechanicType`. `MechanicSpec` gana la propiedad `Key2`. Dentro de la cuia, `key2` ocupa un campo de 4 bits de `Cuia.Packed` (bits 50-53) que reutiliza los de `n` y `k` (bits 50-55), que la Flor no usa (K3b). Fuente: lista n.º 20.
35. El solver consulta la cancelación cada 256 expansiones [P]. La compuerta de coste de Morpho usa un factor 8× [P] en una constante, hasta medirlo en un teléfono de clase A (tarea PF1); si la medida lo cambia, se regeneran los packs que dependen de ella. Fuente: lista n.º 21 y los riesgos del plan («Rendimiento»).
36. D2 entrega dos variantes del calendario de 201-700 (con la tinaja y sin ella), las dos con la regla de novedad de 60 niveles (NOVELTY-60: nunca más de 60 niveles sin algo nunca visto), y rehace la tabla de 701-1000 sin generar esos niveles. Fuente: lista n.º 22.
37. El tablero del 66 tiene un helper único (`Level66Board`) para las pruebas del núcleo y de dotnet, pero no para `StuckLoopTests`: es PlayMode y no puede referenciar `AF.Core.Tests`, cuyo asmdef es solo de Editor. Fuente: lista n.º 28 y crítica 2.

*F. Cliente* (plan consolidado: arquitectura §3 y lista n.º 24, 26 y 27).

38. **«Otro dato» de la parada k = 32:** ya decidido (D-022, punto 5): los datos de Pará del estreno de la Tinaja, con el asesor. Fuente: lista n.º 24, que lo reservaba hasta la 50.
39. **Cola de avisos:** pasa a un enum de tipos {`Text`, `Species`, `StopLabel`, `FruitNew`, `Sloth`, `Tutorial`}. Tras la intro, la prioridad es tutorial > Fruta nueva > miquito > rótulo, con una tarjeta por nivel como mucho (07 §6.4; guion de la intro, §14) y la duración de D-011. Fuente: lista n.º 26 y arquitectura §3 («Avisos»).
40. **Kill-switch (pregunta 31 (b), D-020):** si una mecánica está apagada y no hay `level_overrides` para ese nivel, se juega el nivel publicado y se registra un aviso [P], hasta el formato de respaldo de la 1.1. Fuente: lista n.º 27.

*G. Organización del trabajo* (plan consolidado: lista n.º 29 y 30, arquitectura §4 y crítica 3).

41. La investigación de los datos de 201-700 (M3.7) se parte por mundo, y la hace un subagente de documentación que no programa (regla dura 9). S1 y S2 van en un solo PR (S12; punto 2). Fuente: lista n.º 29 y crítica 3.
42. `Assets/_Project/Scenes/Game.unity` solo se regenera con `GameSceneBuilder.CreateGameScene`, en este orden: L2b → M1.2 → F1F2 → M3.3 → C1a → SE2 → C1b. Si dos coinciden, la segunda rebasa y regenera; nunca se resuelve YAML a mano. Fuente: lista n.º 30 y arquitectura §4.

**Consecuencias.**
- **Contratos:**
  - el 29 y el 30 ganan los extras [P] de P-M1 (08 §4.5; `docs/GDD.md` §7.4);
  - el 21 gana la firma de las variantes de `pack_schema` 3 y la máscara de la tinaja en `relief` (`docs/GDD.md` §7.5). El esquema lo cambian P2a, P1 (la máscara) y P2b (las variantes). En 08 §4.1, la firma de las variantes (filas `schema`, `attrs` e `intro_mechanic`) la escribe el carril de documentación (FIXD, 2026-10-14), porque P2b no puede tocar 08, y la frase de la máscara en `relief`, P1;
  - el 32 no cambia: los textos del Mapa ya iban en `facts.csv`.
- **Canon que no es decisión nueva:** la fila de 04 §5.2 «Abrir el Mapa del río desde la pausa» desarrolla D-013 (punto 7).
- **Sin cambios** en Remote Config, la analítica, la economía ni las reglas del núcleo (el núcleo gana las variantes, con la firma).
- **Sondas de desarrollo previstas** (`stop`, `arrival`, `stop_arrive`, `river_fact_shown` y `loc`, y el id `arrival` de `AF_LAYOUT`): en `docs/dev/prototipo-v0.md`.
- **Código:** las tareas de la tanda 4.

**Qué cambia en el GDD** (aplicado el 2026-10-14, D1):
- `docs/GDD.md`: versión 1.10, estado, §2 #9 (la firma de las variantes y `hidden` en el `relief_board` de los niveles con tinaja) y #11, §6.3 (entrada), §7.1 (fila 7), §7.4 (contratos 29 y 30), §7.5 (contrato 21) y §9 (paso 19).
- 00: §10 (las diez claves de P-M2 y dónde sale `map_country_welcome`, P-M7) y §11 (las variantes de `pack_schema` 3 y los nombres por convención del Mapa).
- 03: §13 (remite a la fila de 04 §5.2).
- 04: §5.2 (la fila del Mapa del río desde la pausa).
- 07: §8.2 (el hito 80, «Lago de Tefé»).
- 08: §4.1 (la firma de las variantes en las filas `schema`, `attrs` e `intro_mechanic`, FIXD, 2026-10-14; y la frase de la máscara de la tinaja en `relief`, P1), §4.5 (los extras de P-M1, la codificación, los esquemas, los nombres por convención, la clave de `country_name` y los textos en `facts.csv`), §4.7 (orden ordinal y LF) y §14.2 (pseudolocalización).
- La cabecera de los capítulos 00, 03, 04, 07 y 08 registra D-023 con estas secciones.
- Fuera del GDD: `docs/dev/prototipo-v0.md` (sondas previstas), la propuesta de mecánicas (`docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md`, d.4), `docs/dev/traspaso/README.md` (§7, la fila de D-023), este registro (la nota de D-013 en F2 y el estado de D-007, D-013, D-018 y D-019 en la tabla) y `.claude/rules/localizacion.md` (el `context` de los textos largos y el alcance de la regla).

**Alternativas descartadas.**
- P-M1 (b) y (c); P-M2 (b) y (c); P-M3 (b); P-M4 (b) y (c); P-M5 (b); P-M6 (b); P-M7 (b); P-M8 (b).
- Preguntas del plan: 7 (no), 8 (no se firma), 9 (sí), 10 (no) y 11 (b) y (c).
- Una pregunta nueva para el choque entre la llegada y TiTino, y editar otra vez «río abajo» (críticas del plan del Mapa).

**Cómo revertir.** Con una decisión nueva que sustituya a D-023, punto a punto. Los [P] los puede ajustar el dueño.

**Archivos afectados.**
- **Documentación** (2026-10-14, D1): este registro y los de «Qué cambia en el GDD».
- **Código y datos** (tanda 4): las tareas del plan consolidado; nada de esto está aún en el cliente.

---

## D-024 · Reglas de la Hoja atada: la capacidad de su llave y una por fruta

- **Fecha:** 2026-10-14. Registrada el 2026-10-14 (FIXD, tanda 4).
- **Estado:** decidida por el dueño; en implementación (FIXK, tanda 4).
  - Para todo lo pendiente, el dueño tiene dicho «Sí, aplica las recomendadas también» (2026-10-13), y lo aplicó el 2026-10-14 a las preguntas de la tanda 4, como en D-022 y D-023. Gana la recomendación de cada pregunta.
  - **Fuentes:** la revisión de la tarea K3a (la Hoja atada y el Brote de dos hojas en el núcleo), que pidió llevar las dos preguntas al dueño, y las notas de la tanda 4 de la sesión principal («Cierre de la ola 1»), con su respuesta. Como en D-023, las notas viven fuera del repositorio: este registro es su copia.
- **Sustituye:** nada del GDD. Cierra la pregunta abierta de dos Hojas atadas de la misma fruta que K3a dejó en el código (`HojaAtadaMechanic`) y convierte en decisión el [P] de su capacidad, que K3a añadió sin estar registrado.
- **No sustituye:** D-007 (la Hoja atada como variante de la 1.0, con estreno en el 283; propuesta b.3), D-023 (la firma `hoja_atada {key}`, punto 5), el precedente del Matapi de 02 §9.1 ni la pregunta 34 de D-020 (la excepción del Remolino en P3, que sigue siendo la única).

**Contexto.**
- K3a llevó al núcleo la Hoja atada de la propuesta de D-007 (b.3): un filtro de color persistente, que solo recibe su fruta (vacía o con ella en la cima), sí es origen y nunca se suelta. Como destino nunca es fútil (02 §1.4, anexo B: cambia al recibir).
- **Capacidad.** Al probar P1-P3 (02 R16) con capacidades mixtas, esa exención deja que la fruta vaya de su casa a una Hoja atada de otra capacidad y vuelva, con el mismo estado canónico: P3 falla. K3a lo resolvió con una regla de validación [P], «la Hoja atada tiene la capacidad `U_c` de su llave», con el precedente del Matapi («con dosis, su capacidad es la `U_c` de ese color [P]», 02 §9.1). La revisión de K3a encontró que era una regla nueva que el dueño no había visto (CLAUDE.md: no inventar reglas) y pidió llevársela.
- **Dos de la misma fruta.** Con dos Hojas atadas de la misma fruta, las dos de capacidad `U_c`, el vertido de una a otra es útil en los dos sentidos y P3 falla igual. Además, la cuenta de capacidades de 02 §8 de `LevelValidation`, contada solo por capacidad, tomaba la segunda por casa de otra fruta y dejaba pasar tableros irresolubles. Opciones de la revisión: (a) un límite, por fruta o por nivel; (b) una excepción de P3, como la del Remolino.

**Decisión.**
1. **La capacidad de la Hoja atada es la `U_c` de su llave** [P]: se ratifica la regla de validación de K3a. La Hoja atada es la casa de su fruta, como el Matapi con dosis.
2. **Como mucho una Hoja atada por fruta** en cada nivel (opción (a), por fruta): cierra P3 y la cuenta de capacidades de 02 §8 a la vez. No limita las Hojas atadas de frutas distintas, con las que P1-P3 se cumplen (K3a), y el estreno sigue con un solo ejemplar (02 §9.2, regla 2).

**Consecuencias.**
- **Código** (tanda 4; sin paquetes, SDK, permisos ni versiones nuevas): FIXK lo implementa en el núcleo (la validación de `HojaAtadaMechanic` y `LevelValidation`, que cuenta cada cuia de una sola fruta solo como casa de esa fruta). El generador (P5) y level-qa (P6, QA-VALID) lo heredan por `LevelValidation`, sin regla propia.
- **Calendario de 201-700:** con las dos reglas, K3a comprobó que P1-P3 se cumplen también con capacidades mixtas, así que el 354 (la Hoja atada con la Cuia alta) se queda como está. Su respaldo, por si una medida posterior lo tumbara, es el reto 346, dentro del C+3, con 3 invitadas en 341-360 como excepción declarada a la plantilla [P] (propuesta de D-009, d.5, punto 3; 03 §12).
- **Sin cambios** en el formato (`hoja_atada {key}`, D-023, punto 5: la capacidad ya va en `caps`), Remote Config, la analítica ni la economía. Ningún pack publicado lleva Hoja atada.

**Qué cambia en el GDD** (aplicado el 2026-10-14, FIXD):
- `docs/GDD.md`: versión 1.11, estado, §2 #12, §6.3 (entrada) y §9 (paso 19).
- 02: §9.1 («Variantes»).
- 03: §17 (comprobación 6).
- 08: §4.1 (fila `attrs`).
- La cabecera de los capítulos 02, 03 y 08 registra D-024 con estas secciones.
- Fuera del GDD: este registro (tabla y entrada) y la propuesta de mecánicas (`docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md`, d.5, punto 3: el 354 se queda y su respaldo cae en el C+3).

**Alternativas descartadas.** (b), una excepción de P3 para la Hoja atada; el límite de una Hoja atada por nivel, más estricto de lo que hace falta; y una Hoja atada de cualquier capacidad.

**Cómo revertir.** Con una decisión nueva que sustituya a D-024. Mientras no se publique ningún pack 3, sin coste en los datos.

**Archivos afectados.**
- **Documentación** (2026-10-14, FIXD): este registro y los de «Qué cambia en el GDD».
- **Código** (tanda 4, FIXK): `Packages/com.amazonfill.core/Runtime/AF.Core/Mechanics/HojaAtadaMechanic.cs` y `Board/LevelValidation.cs`, con sus tests y `dotnet/AF.Core.Tests/DotnetOnly/FutilePropertyTests.cs`.

---

## D-025 · Un solo formato de cifras, también en los precios

- **Fecha:** 2026-10-14.
- **Estado:** decidida por el dueño («unificar formatos») tras la prueba de la ola 1 de la tanda 4 (`docs/dev/traspaso/pruebas-telefono-2026-10-14-tanda4.md`, observación 4). Que la regla sea la de la región del teléfono es la [P] de este registro, coherente con Q8-A; el dueño la puede cambiar. **Implementada en FXT** (tanda 4, ola 3): puntos 1 y 2 con `Loc.Price` (`AF.Localization`, `LocPrice`), por la que pasan `LocalPrice`, `FakeStoreService`/`SimulatedPurchase.PriceText` y `GameServices.PriceText`; del punto 3 queda lista la función (`Loc.PriceFromMicros`) para cuando exista el adaptador de Unity IAP. Pendiente: 05 §4.2 y el glosario §10 (ver «Consecuencias»).

**Contexto.**
- Q8-A de D-018: las cifras del juego van con el formato de la región del teléfono, con una sola función (`Loc.Number`; sin región, es-CO [P]).
- Los precios de la tienda salen, en cambio, con el formato fijo de 05 §4.2 por moneda (`StoreCatalog`: «COP 3.900», «BRL 4,99»).
- Con el teléfono en en-US, el mismo botón mezcla los dos: «Puñado de semillas 1,000 · COP 3.900».

**Decisión.**
1. **Todas las cifras de la interfaz llevan un solo formato,** el de `Loc.Number` (región del teléfono; sin región, es-CO [P]): semillas, canoas, premios, contadores y **precios**.
2. **Precio = código ISO 4217 de la moneda + importe con ese formato** y los decimales de la moneda (0 en COP y MXN; 2 en el resto, 05 §4.2). Los importes de 05 §4.2 no cambian, solo su escritura. Ejemplos:

   | Teléfono | Botón |
   |---|---|
   | en-US | «1,000 · COP 3,900» |
   | es-CO | «1.000 · COP 3.900» |
   | es-MX | «1,000 · MXN 19» |
   | pt-BR | «1.000 · BRL 4,99» |
3. **Con la tienda real** (Unity IAP 5.4.4 y Play Billing), el precio se compone desde el importe y la moneda del producto (`priceAmountMicros` y el código de moneda), con la misma función, en lugar de la cadena ya formateada de Google Play [P]. Así la tienda simulada y la real se ven iguales.

**Consecuencias.**
- **Código:** `StoreCatalog`/`FakeStoreService.PriceText` y todos los textos con precio pasan por `Loc.Number`, con tests de las cuatro regiones de arriba. Más adelante, el adaptador de `AF.IAP`.
- **GDD:** 05 §4.2 (cómo se escriben los precios) y glosario §10 (formato de cifras).

**Cómo revertir.** Volver al formato fijo por moneda de 05 §4.2, siempre con una decisión nueva que sustituya a D-025.

## D-026 · Audio provisional por código y permiso `VIBRATE`

- **Fecha:** 2026-10-16.
- **Estado:** decidida por el dueño en el chat: «lanza ya AU1 en paralelo a la ola 4, con los arreglos de niveles y graves, y apruebo el permiso VIBRATE para el háptico del sellado». **Implementada en AU1** (tanda 4, ola 4, carril de audio), con el ambiente por mundo y la música adaptativa provisionales de AU2, en la misma ola y con el mismo criterio (por código, sin paquetes ni peso en el AAB); integradas en `integracion-fase1-fase2` en `bd649ac`. Pendiente: la prueba en el teléfono (las sondas `AF_STATE sfx` y `haptic` de `docs/dev/prototipo-v0.md`) y los encargos al compositor y al diseñador de sonido (07 §9.5), que sustituyen estos sonidos sin tocar los ganchos.

**Contexto.**
- El cliente no tenía ningún sonido (07 §9.2: «hoy el cliente no tiene audio») y Ajustes ocultaba Sonido, Música y Vibración (D-008: lo que no existe se oculta).
- El 2026-10-15 se añadieron la lista de sonidos (`docs/audio/lista-de-sonidos.md`, con la dirección «ASMR amazónico fresco» y la jerarquía de niveles de Magic Sort) y el laboratorio procedural (`docs/audio/laboratorio-sonidos.html`), con la síntesis de los 61 efectos.
- La revisión del laboratorio del 2026-10-16, renderizando sus 61 efectos, encontró:
  - un `tanh` final que igualaba los volúmenes: `sfx_select` salía a −7 dBFS en lugar de −36, la jerarquía quedaba en unos 4 dB y `sfx_confetti`, unos 20 dB por debajo de su objetivo;
  - siete efectos (`sfx_illegal`, `sfx_stuck`, `sfx_canoe_lost`, `sfx_tinaja_select`, `sfx_ui_back`, `sfx_map_pequepeque` y `sfx_sloth_nod`) y dos notas de fruta (Huito y Cacao) con el 78-99 % de la energía por debajo de 300 Hz, que el altavoz de un teléfono no reproduce;
  - la reverberación de dosel añadida a todo (un toque de 0,12 s duraba 0,44 s);
  - un `sfx_pour_loop` que era un sonido de 0,72 s y no un bucle.
- Los hápticos de 02 §4 y 07 §9.2 necesitan el permiso Android `VIBRATE`, y la regla dura 4 de `CLAUDE.md` exige su aprobación.

**Decisión.**
1. **Audio provisional por código ya,** con los algoritmos del laboratorio portados 1:1 a C# 9 sin motor (`AF.Audio`) y generados al arrancar en un hilo de trabajo (0 MB en el AAB), hasta que lleguen el compositor y el diseñador de sonido. Solo el módulo de audio integrado de Unity (`com.unity.modules.audio`, ya en el manifest): sin paquetes nuevos.
2. **Con los arreglos de la revisión:**
   - cada efecto a su pico objetivo (la columna dBFS del laboratorio), sin el `tanh` global;
   - al menos el 50 % de la energía por encima de 300 Hz, con armónicos o una octava;
   - la cola de dosel solo en los efectos que la llevan;
   - los gestos repetidos en ≤ 0,2 s;
   - el chorro como bucle continuo cuyo tono sigue al nivel de la cuia de destino.
3. **Se aprueba el permiso Android `VIBRATE`** para los hápticos de 02 §4 y 07 §9.2 (sellado, victoria, Capi, Boto y Atrás bloqueado), por un mecanismo versionado: un gancho de Editor que completa el manifiesto que genera Unity (`AF.Audio.Editor.AndroidVibratePermission`). Ningún otro permiso nuevo.
4. **Sonido, Música y Vibración vuelven a Ajustes,** activados por defecto (`audio_sfx_default_on`, `audio_music_default_on` y `ui_haptics_default_on`, 08 §9) y guardados en `settings.sound`, `music` y `haptics` (08 §4.3).
5. Los sonidos son **provisionales por código**, también los que el laboratorio llamaba «reales» (la piha y el boto): las grabaciones con licencia llegan después (07 §9.5).

**Consecuencias.**
- **Código:**
  - `AF.Audio` (sin motor): síntesis, catálogo de los 61 IDs con su nivel, mezcla, reglas y la fachada `AudioCues`;
  - `AF.Audio.Unity`: el servicio con su pool de `AudioSource`, el lecho de música provisional y los hápticos y el modo silencio por JNI;
  - los ganchos de una línea en la vista y en la UI;
  - las filas de Ajustes, las sondas `AF_STATE sfx` y `haptic` y `AF_PERF audio_build`.
- **Sin sonido al bajar la selección** (revisión de AU1): 02 §4.1 la deja sin audio y la variante grave de la lista de sonidos no la decidió el dueño, así que no se aplica. Queda como propuesta: si se aprueba, se anota aquí y en 02 §4.1.
- **Datos:** las claves `sound`, `music` y `haptics` de `strings.csv` (glosario §10).
- **GDD:** 07 §9 (el estado: audio provisional por código en el cliente). Completado tras la ola 4 (FXD2, 2026-10-16): la entrada de 07 §9 (la carpeta del código, los niveles, la regla de energía por encima de 300 Hz y el permiso), §9.1 y §9.3 (el ambiente y la música de AU2) y §9.4 (el ambiente va con el ajuste Música y baja con ella en el ducking [P]); y `docs/GDD.md` §9, paso 19 (`VIBRATE`, el único permiso nuevo de la tanda 4).
- **Lista de sonidos:** los IDs finales con su nivel.

**Alternativas descartadas.**
- **Esperar a las grabaciones:** el dueño quiere oír el juego en el teléfono ya.
- **Exportar WAV del laboratorio al AAB:** pesan y no se arreglan sin volver a exportarlos.
- **Hápticos con `Handheld.Vibrate`:** solo da una duración fija, sin los efectos predefinidos de Android.

**Cómo revertir.** Instalar el servicio mudo (`AudioCues.Install(null)`) y ocultar otra vez las tres filas de Ajustes; quitar el gancho del manifiesto retira `VIBRATE`. Siempre con una decisión nueva que sustituya a D-026.

---

## D-027 · Mundos 7-9: las celdas, los K y los ajustes del generador para poder generarlos

- **Fecha:** 2026-10-18. Registrada el 2026-10-18 (GEN79, tanda 4, ola 7).
- **Estado:** decidida por el dueño en el chat: «Acepto todas las recomendaciones de la propuesta de los mundos 7-9: las 25 celdas, K = 10 en los retos de 421-700, los hitos de 14 cuias con K = 12 + 1 vacío, el alivio hasta 13 cuias con Tinaja y eximir de QA-PAR los tableros del estreno de la Tinaja. Genera los mundos.» **Implementada en GEN79:** la especificación, el generador, level-qa y los packs de 301-700 en sus dos variantes. **Publicada en el cliente con la variante b en C1b** (ola 7, en su rama). Pendientes: la curación del dueño (122 niveles en la b, la publicada; 123 en la a; `tools/level-generator/README.md`) y el reflejo en el GDD (carril de documentación; ver «Qué cambia en el GDD»).
  - **Fuente:** la propuesta «Mundos 7, 8 y 9 (niveles 301-700): por qué no se generan y qué cambiar» (2026-10-18), con las medidas de cada opción en el equipo de escritorio. Vive fuera del repositorio, en el scratchpad de la sesión principal: este registro es su copia en lo que se decidió.
- **Sustituye:** en `tools/level-generator/specs/campaign_v2.json`, 25 celdas del calendario de d.5.3 (D-023, punto 10), el respaldo del 352 y el `k_plan` [P] de 421-700; el tope de cuias del alivio con la Tinaja [P] de P1. Contesta la pregunta de P7d (¿un hito de 14 cuias puede ir sin cuia vacía?): no.
- **No sustituye:** D-009 (el ciclo de 20, la escalera y la etiqueta por el bot ×40), D-019 (la Tinaja, su estreno en el 323 y la salida B), los estrenos de D-007 y D-023 ni el tope de 12 cuias de un nivel con la Tinaja (02 §5).

**Contexto.**
- Los packs `mundo_07_v1.json`, `mundo_08_v1.json` y `mundo_09_v1.json` nunca se generaron (P7b-P7d quedaron bloqueadas): el generador avanza ciclo a ciclo y se paraba en el primero sin colocación.
- Causas medidas: la escalera de D-009 (un reto temprano que solo sale Muy difícil o uno tardío que solo sale fácil impide colocar el ciclo, sobre todo con las Aguas turbias en p15); la Tinaja con el tope de 12 cuias, que con el Brote o el Brote de dos hojas baja a 8 colores y deja el 340 y el 352 sin dificultad; los retos de K = 11 y 12, que salen Muy difíciles en más del 80 % de los intentos; los hitos de 14 cuias del 640 y el 700, que no caben con K = 13; los niveles con la Tinaja de 12 cuias sin alivio (329, 338 y 340), y el par de los estrenos de la Tinaja (K ≤ 6), por debajo del par del rango en level-qa.

**Decisión.**
1. **25 celdas del calendario** (variante a; las demás no cambian): 340 Ti+Ho (MD), 346 B2+Ln (D), 352 B2+Re (D), 358 Ln+Re (D), 360 B2+Ln (MD), 366 Ho+Tu (D), 369 Pi+B2 (D), 380 Pi+Ho (MD), 386 Ba+Tu (D), 389 Li+Ha (D), 395 Li+Ba (D), 398 Ha+Ba (D), 415 Fl+Ll (D), 418 Ba+Tu (D), 460 Pi+Ba (MD), 509 Fl (D), 529 Ho+Re (D), 538 Ha+Re (D), 552 Ln+Re (D), 558 Fl (D), y los hitos 580, 600, 640 (Ma+Ba), 660 y 700 sin «K = 13». Casi todas reordenan los retos de un ciclo; ninguna quita un estreno ni una mecánica. El 352 ya no lleva la Tinaja, así que pierde su respaldo de `fallback` (18 respaldos; en 341-360 la Tinaja sale solo en el 355).
2. **K de los retos:** 10 en 421-700 (mundos 8 y 9); los del mundo 7 siguen con 11 (con 10, el ciclo 401-420 no colocaba). Las comodidades no cambian (11). La dificultad tardía viene de las mecánicas y la etiqueta la sigue midiendo el bot (D-009).
3. **Hitos de 14 cuias con K = 12 + 1 vacío** + la cuia a medias de su mecánica en el 580, 600, 640, 660 y 700: el jugador conserva siempre una cuia vacía, como en el resto del juego (la opción (b) de la propuesta; con la (a), la holgura repartida en cuias a medias de P7d, el hito no tendría ninguna; en el 580, el 640 y el 700 la cuia vacía la ocupa la red del Matapi). El 620 y el 680 siguen con K = 13 + 1 vacío. Todos los hitos desde el 580 siguen teniendo 14 cuias.
4. **Alivio con la Tinaja hasta 13 cuias:** la variante «una cuia vacía más» (03 §13) de un nivel con la Tinaja de 12 cuias puede tener 13, con la pareja en la misma fila; el nivel sigue con 12 como mucho (02 §5). 13 cuias caben en el tablero de D-006 (≤ 16 ranuras; 2 o 3 filas) y el cliente no parte la pareja (TJ2).
5. **QA-PAR exime del par del rango los tableros de estreno de la Tinaja** (los de ★Ti con K ≤ 6: el 323, el 324 y el 325), como ya pasaba con su K: su solución es más corta que el par objetivo de 201-400 (el 324 y el 325, 13 y 14 movimientos frente al mínimo de 15,4). El par recalculado y el par ≥ 2K siguen valiendo.

**Medidas** (GEN79, equipo de escritorio x64, Release):
- Los tres mundos se generan enteros en las dos variantes, en unos 17 minutos los seis a la vez (la variante a sola, en serie, unos 20). Tres ejecuciones dan los mismos bytes; coinciden con los hashes que midió la propuesta en las variantes a de los tres mundos y en la b del mundo 7, y de la b de los mundos 8 y 9 (sin hash en la propuesta, que los generó una vez cada uno) solo consta el determinismo propio.
- Registro de combinaciones: 98 entradas en la a y 94 en la b (antes, 102 y 96); `--validate-spec`, 24 reglas sin fallos en las dos, con el mismo hueco máximo entre primeras veces (34, del 526 al 560).
- Etiquetas publicadas: mundo 7, 29 Difícil y 6 Muy difícil de 120 niveles; mundo 8, 30 y 7 de 140; mundo 9, 27 y 7 de 140 (03 §11: 25 % y 5 %). Todos los hitos miden Muy difícil. 14 retos no llegan a Difícil y se publican sin la etiqueta (c.2; 03 §7): 369; 429, 452, 466, 469 y 486; 566, 569, 592, 615, 646, 658, 686 y 692. El 646 es el «primer reto con tres mecánicas» de d.5.2.
- level-qa: 0 fallos en los seis packs (las dos variantes de los tres mundos), con los avisos conocidos: QA-CUIAS (los estrenos de la Tinaja de 10 cuias, tableros de 14 cuias en 201-400 y retos de 11 cuias con K = 10 en 401-700), QA-WINDOW (los 14 retos sin etiqueta) y QA-BAND-Q (un hito en banda de reto con el mínimo cumplido).

**Consecuencias.**
- **Especificación y registro:** `campaign_v2.json` (celdas, `fallback`, `k_plan` y sus notas) y `combinations_registry_v1.json`, regenerado.
- **Código** (sin paquetes, SDK, permisos ni versiones nuevas): `Superposition.TinajaReliefMaxCuias` y `Relief.PlusEmpty` (generador); `TinajaChecks.Structure` con el tope de su variante en QA-RELIEF-VALID y `BaseChecks.TinajaDebutParExempt` en QA-PAR (level-qa), con sus fixtures y `--self-test`. La holgura repartida en cuias a medias (P7d) solo la usa el 680 en sus etapas de relajación; el mecanismo se queda y `--self-test holgura` lo prueba con la forma de antes de esta decisión.
- **Datos:** `Assets/_Project/Data/Levels/mundo_07_v1.json` a `mundo_09_v1.json` (variante a), `tools/out/salida_b/` (variante b), los informes de curación `tools/out/curation_w07.md` a `curation_w09.md` y la tabla de temporada `seasons_v1.json`, ampliada a 21-700. El cliente los registra en C1b.
- **Sin cambios** en el formato de nivel (`pack_schema` 3), Remote Config, la analítica ni la economía.

**Qué cambia en el GDD** (pendiente, carril de documentación):
- 03 §11 (K de los retos de 421-700 y los hitos de 14 cuias con K = 12 + 1 vacío, salvo el 620 y el 680) y §12 (el reordenamiento de retos frente a d.5.3, si se cita);
- 02 §5 (fila «Tinaja y su cuia»: el alivio, hasta 13 cuias);
- 03 §17, comprobación 2 (la excepción del par en los estrenos de la Tinaja);
- la propuesta de mecánicas (`docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md`, d.5.3 y d.5.4), si se quiere que su tabla siga siendo copia literal.

**Alternativas descartadas** (medidas en la propuesta): el respaldo Br+Ba del 340 en la a (coloca, pero no arregla el alivio del 329 y el 338), K = 11 en los retos de 421-700 (frágil: coloca en 1 de 4 combinaciones), K = 10 en los retos del mundo 7 (401-420 no coloca), hitos de 14 cuias con K = 13 y sin cuia vacía (bot al 100 % y el 700 no coloca), y publicar solo la variante b.

**Cómo revertir.** Con una decisión nueva que sustituya a D-027, cambiando la especificación y el generador y regenerando los packs de 301-700, el registro y los informes. Mientras C1b no los publique, sin coste para el jugador.

**Archivos afectados.**
- **Especificación y datos** (GEN79): `tools/level-generator/specs/campaign_v2.json`, `combinations_registry_v1.json`, los packs y los informes de arriba y `seasons_v1.json`; regenerados por el texto nuevo de sus informes (sin cambios en sus packs): `tools/out/curation_w06.md` (la lista aparte de los retos e hitos sin su etiqueta) y `tools/out/sample_tinaja_report.md` (el tope del alivio).
- **Código** (GEN79): `tools/level-generator/Generation/Superposition.cs`, `Metrics/Relief.cs`, `Generation/CampaignReport.cs`, `Generation/CurationReport.cs`, `Generation/SeasonTableWriter.cs` y los `--self-test` (`TinajaSelfTest`, `RegistrySelfTest`, `HolguraSelfTest` y `CurationSelfTest`); `tools/level-qa/Checks/BaseChecks.cs`, `TinajaChecks.cs`, `ReliefChecks.cs` y `RulesSelfTest.cs`, con dos fixtures nuevos.
- **Documentación** (GEN79): este registro, `tools/level-generator/README.md`, `tools/level-qa/README.md` y `docs/dev/traspaso/README.md`.

---

## D-028 · Respuestas del 2026-10-19 a las preguntas 19, 22-24, 53, 54, 57, 60, 61 (con P5) y 65 de `docs/GDD.md` §6.3

- **Fecha:** 2026-10-19. Registrada el 2026-10-19 (DC1, tanda 4, ola 7).
- **Matizada por D-046 (punto 3, 2026-10-23):** de la excepción de la pregunta 22 se retira la parte de la fila 1: con 3 filas el tablero deja la franja de la tropa (56 dp) entre el HUD y el panel, así que ni la fila 1 elevada ni su cielo entran en el HUD. Sigue valiendo solo donde esa franja no cabe (16:9 y la Cuia alta de 6 en lienzos bajos), con el tablero de antes. Con la franja y el suelo bajado 20 dp (Q-T6T-3, decidida por el dueño el 2026-10-24: «Bajar y mover avisos»), el aviso «Te faltan {n}» y el aviso del tablero que ya no caben bajo el panel van a la franja; la bajada del canal del Remolino sigue como excepción.
- **Estado:** decidida por el dueño en el chat, literal: «Acepto la opción recomendada en las preguntas 19, 22, 23, 24, 53, 54, 57, 60 y 61 (con P5) del GDD §6.3 y en las dos de M5.1; abre los carriles que salgan, de forma segura. El espacio del AAB y el permiso INTERNET los decido aparte.»
  - Gana la recomendación de cada fila de `docs/GDD.md` §6.3, tal como estaba escrita el 2026-10-19 (D3). «Con P5» cierra también la pregunta P5 de este registro (§5, D-002) con su recomendación: sí.
  - **«Las dos de M5.1»** se registran como la pregunta 65 (la geometría de producción del Mapa frente al canon), la que M5.1 dejó para el dueño con recomendación (notas de M5.1 en `tools/river-map/README.md`, `cdff504`), en sus tres partes: 1 (d), 2 (a) y 3 (a). Es la lectura del plan de la ola 7 (DC1). Las «Paradas en una confluencia [P]» de M5.1 son otra cosa (punto 2 de la pregunta 66) y no entran.
  - Documentación aplicada el 2026-10-19 (DC1): en `docs/GDD.md` §6.3 las diez filas pasan a decididas con la última columna al día, y la regla de cada respuesta va a su capítulo («Qué cambia en el GDD»). El código, en las tareas DC2-DC11 de la ola 7 (abajo).
- **Sustituye:**
  - los [P] y las opciones abiertas de esas diez preguntas y de P5;
  - en D-007 (Consecuencias, «Bloqueo C1-9»), el «provisionalmente»: el bloqueo es la regla de la 1.0;
  - en D-013 (punto 5, Q11-A), las líneas de la vista Cuenca de Natural Earth: salen de HydroRIVERS en las dos vistas (punto 10);
  - en 02 §12, la excepción del texto al 82 % en 16:9, que nunca se aceptó: deja de estar pendiente.
- **No sustituye:** D-002 (la confirma: el dedo guía y Morpho son ayudas explícitas); D-016, punto 5 (el tope de 4 frases por sesión y «nunca en dos pantallas seguidas»: la 60 solo dice cómo cuenta el bocadillo del Mapa del 10); T6-A (una sola línea de texto por victoria); D-022 (la prioridad de la 45 y la burbuja de Revelar sin medios de la 48); D-023, punto 20 (la tolerancia de ≤ 2 dp de cada vista, que se conserva con las excepciones medidas); ni el contrato 29.

**Contexto.** Las preguntas 19, 22, 23 y 24 son de los pasos 1 y 2 del traspaso (2026-10-11 y 2026-10-12); la 53 y la 54, de TI6a (ola 4 de la tanda 4, 2026-10-16); la 57, la 60 y la 61, de las olas 5 y 6 (M4.6, 2026-10-18); y la 65, de M5.1 (ola 7, 2026-10-19, D3). P5 venía de D-002 (2026-10-06): el dedo guía de los niveles 1-2 (02 §13) y Morpho (04 §2.3) muestran un destino, y D-002 prohíbe resaltar destinos al seleccionar. Las opciones de cada una están en su fila de `docs/GDD.md` §6.3.

**Decisión.**
1. **19 → (a): el texto no baja de 12 sp físicos en 16:9.**
   - En 16:9 el lienzo del juego (`CanvasScaler`) no escala por debajo de 1 dp físico por dp de lienzo, así que ningún texto queda por debajo de 12 sp físicos (02 §12), con una prueba de layout a 360 × 640 dp (02 §5).
   - Ya aplicado: nada; hoy el texto sale al 82 % en 16:9 (≈ 9,8 sp). Tarea: **DC3** (carril DC-U1), la última de la ola, porque cambia el lienzo de 16:9 en el que se capturan las demás.
2. **22 → se ratifican los tres [P] del paso 1:**
   - con 3 filas, la cuia elevada de la fila 1 se dibuja delante del HUD (su cielo entra en la fila del HUD y una cuia de 5 elevada tapa unos 2,5 dp del área táctil de Reiniciar); el layout no reserva ese cielo;
   - el aviso «Te faltan {n}» de dos líneas queda justo (2 dp sobre el panel en el 106);
   - la bajada del canal del Remolino sobre su propia repisa queda como excepción.
   - Ya aplicado: los tres, en el cliente desde el paso 1. Tarea: ninguna (solo documentación).
3. **23 → (a): vídeo primero también en Revelar.** Sin stock, Revelar ofrece primero el vídeo (`rv_reveal`) si lo hay y, si no, las semillas, como Marea (04 §2.5). Sin stock, saldo ni vídeo, la burbuja de la 48 (D-022) no cambia.
   - Ya aplicado: nada; hoy cobra primero con semillas (la (c)). Tarea: **DC4** (carril DC-U2).
   - Economía: sin cambios en `tools/econ_sim.py`, que modela `reveal` como `hint` y no distingue el orden de pago; ninguna cifra [sim] cambia. 05 no fija el orden de pago de la bandeja.
4. **24 → (b) para la 1.0: se mantiene el bloqueo C1-9.** La Liana nunca va con Cuia chica, Cuia alta ni Piraña (con capacidades distintas de 4, cueva de la Piraña incluida), y es la regla de la 1.0, ya no provisional (02 R16). La (a), ampliar `futil` y quitar el bloqueo con una regla nueva y sus pruebas de propiedad P1-P3, se estudia para la 1.1 (09 §1.2).
   - Ya aplicado: en el núcleo (`LevelValidation`, `LianaMechanic` y `FlorMechanic`), en el generador (`SpecValidator`, regla LIANA-CAPS) y en los packs, con las cuatro celdas del calendario (94, 95, 160 y 198). Ningún pack se regenera. Tarea: **DC11** (carril DC-P), que quita «provisional» de los textos y comentarios del núcleo, sin cambiar reglas.
5. **53 → (a): TiTino en la franja de la victoria con vista.** En la victoria con vista (02 §6; el tablero se desvanece), la caja de TiTino es la franja que va del pie de la cabecera a la cima de la cinta, sin el héroe del sitio del fondo (el trozo más alto de los dos que deja; sin héroe, la franja entera).
   - Tarea: **DC2** (carril DC-U1, `65af256` a `e30226e`, en su rama). Sus [P], en 02 §6 (fila TiTino) y 07 §5.4 (fila 0,80); sus medidas, en la última columna de la fila 53 de `docs/GDD.md` §6.3 (una sola copia de las cifras).
6. **54 → (a): se ratifican la regla y los tres [P] de TI6a.** Fuera de sus hitos, TiTino solo dice su frase si la ranura del Dato del río va vacía, si su caja mide ≥ 104 dp y en 1 de cada 4 victorias (`stats.levels_won`, TI6b); y (1) la rama entra en la fila 0,80 de 07 §5.4, con el panel; (2) en las cajas mínimas TiTino mide 64 dp con bocadillo y 32 con solo el gesto; (3) la garza blanca del fondo se funde mientras TiTino está a la vista.
   - Ya aplicado: la regla y los tres [P], desde TI6a. Tarea: **DC2**, que mide de nuevo las cajas con la franja de la 53 (punto 5): las cajas mínimas dejan de ser el caso habitual.
7. **57 → (a): la fuente del dato, en una fila `<clave del dato>.source` de `facts.csv`.** Una fila por dato con el nombre corto de la fuente («Ramsar», «SINCHI»), generada por `tools/river-map/stops.py` desde la `fuente` o las `fuentes` de la investigación, sin traducir los nombres propios y con su estado; es una regla más del contrato 32 (08 §4.7). La tarjeta de la parada la enseña bajo el dato (`map_card_source`; 06 §4.5 y 07 §8.2).
   - Ya aplicado: el cliente ya lee la fila, pero no existe ninguna. Tareas: **DC9** (carril DC-P: `stops.py`, sus tests y el bloque `map.*` de `facts.csv`) y **DC10** (carril DC-U1: la tarjeta, sin el `Ignore` de `StopCardTests`, con su captura).
8. **60 → (b): el bocadillo del Mapa del 10 no gasta el tope de frases, pero cuenta en «dos pantallas seguidas».** `map_l10_intro` no cuenta en `ui_sloth_lines_per_session_max` (es la presentación del personaje, una vez en la vida; 06 §2.3), pero sí en «nunca en dos pantallas seguidas»: el saludo del inicio espera a la visita siguiente.
   - Ya aplicado: nada; hoy no cuenta en ninguna (la (c), desde M4.2). Tarea: **DC5** (carril DC-U2).
9. **61 → (a), con P5 → sí.**
   - **61:** sin dedo guía en la primera Pista del 12: la presentación de TiTino con la cola sobre la ranura «Pista» hace de guía (06 §2.2; 02 §13; 04 §2.3).
   - **P5:** el dedo guía de los niveles 1-2 (02 §13) y Morpho (04 §2.3), que muestran un destino, se mantienen como ayudas explícitas compatibles con D-002 (02 §3.1).
   - Ya aplicado: el 12 ya va sin dedo desde TU1b (anotado [P]); el dedo de 1-2 no existe en el cliente. Tarea: **DC6** (carril DC-U2): el dedo guía de 1-2, y el [P] del 12, fuera de `TutorialInstruction` y de los tests. El dedo del primer uso de la Hoja (43) y del tutorial de la Tinaja (323), que piden las fichas M1 y M13 de 02 §9.1, es **DC7** (alcance confirmado el 2026-10-19; registro en D-029, DC1b).
10. **65 → (1) (d), (2) (a) y (3) (a).**
    - **Parte 1 (d):** se aceptan las excepciones medidas tal como están, sin margen: `PENDING_CONTROL_KM` (70 de los 129 puntos de control de la vista Mundo a más de 1 km, y la boca del Nhamundá en la Cuenca), `PENDING_SITE_KM` (los sitios tierra adentro) y la boca del Pacaya (`PENDING_MOUTH_KM`). `--check` falla si un dato se aleja más. La vía (c) sigue abierta: cuando el investigador corrija los puntos [E] y dé el atraque de los sitios tierra adentro, cada excepción que deje de hacer falta se quita.
    - **Parte 2 (a):** las líneas salen de HydroRIVERS en las dos vistas, simplificadas por vista con Douglas-Peucker (6 km en la Cuenca y 1 km en la vista Mundo), con las excepciones de `LINE_SRC` (el ramal del Pará, el Itaya y el Pacaya, de la investigación; el Casiquiare, de Natural Earth); la tierra sale de Natural Earth 1:10m. Se alinean 08 §4.5 (su «Origen» y el `src` de su ejemplo) y D-013, punto 5.
    - **Parte 3 (a):** la tolerancia de 1 km de la vista Mundo rige a ≤ 100 km de una parada de la 1.0 (`MundoZone`); fuera, la vista Mundo enseña la simplificación de la Cuenca, con sus quiebros lejos de las paradas (observación 1 de la prueba de la parte A, `docs/dev/traspaso/pruebas-telefono-2026-10-19.md`).
    - Ya aplicado: las tres, como [P], desde M5.1 (`cdff504`), sin cambiar el contrato 29 ni las rutas. Tarea: **DC8** (carril DC-P), que quita las marcas de pendiente del README y de los docstrings de `tools/river-map/`, sin cambiar datos.

**Fuera de esta decisión** (siguen pendientes del dueño):
- **el espacio del AAB** antes de los SDK y del arte a 2× (Addressables y PAD, fuentes recortadas o arte a 1×; B2, `docs/setup/medida-aab-b2.md`) y **el permiso `INTERNET`** (`CLAUDE.md`): el dueño los decide aparte;
- **las «Paradas en una confluencia [P]»** de M5.1 (punto 2 de la pregunta 66): siguieron [P] hasta que el dueño las decidió el mismo día, en la (a), ancladas en la boca (D-030);
- las demás preguntas abiertas de `docs/GDD.md` §6.3: la 18, la 21, la 25, la 26, la 52, la 55, la 56, la 58, la 59, las 62-64 y la 66 (todas menos la 66 se decidieron el mismo día con su recomendación, D-029; de la 66, las partes 1, 3 y 4).

**Consecuencias.**
- **Código** (ola 7 de la tanda 4; sin paquetes, SDK, permisos ni versiones nuevas): DC2, DC3 y DC10 (carril DC-U1: la victoria y el lienzo); DC4, DC5, DC6 y DC7 (carril DC-U2: Revelar, el Mapa del 10 y el tutorial); DC8, DC9 y DC11 (carril DC-P: las herramientas del Mapa y el núcleo).
- **Contratos:** el 32 suma la regla de las filas `<clave del dato>.source` (08 §4.7); el 29 no cambia (su `src` ya admitía `hydrorivers`).
- **Sin cambios** en Remote Config, la analítica, el formato de nivel, los packs ni la economía (ninguna cifra [sim]).

**Qué cambia en el GDD** (aplicado el 2026-10-19, DC1):
- `docs/GDD.md`: versión 1.16, estado, §6.3 (título, entrada y filas 19, 22, 23, 24, 53, 54, 57, 60, 61 y 65) y §9 (pasos 18 y 19).
- 00: §10 (la fila `<clave del dato>.source`).
- 02: §3.1 (D-002 y P5), §5 (los [P] de la 22 y la prueba de layout de la 19), §6 (fila TiTino, con la 53 y la 54), §9.1 (M1, la nota de P5), R16, §12 (fila Texto), §13 (fila 12, la nota de TiTino y la de «Par y lecciones») y Preguntas abiertas 11 y 13.
- 04: §2.3 (la primera Morpho del 12) y §2.5 (fila «Anuncio recompensado»).
- 06: §2.2 (filas 1, 2 y 12), §2.3 (el bocadillo del Mapa del 10) y §4.5 (la tarjeta con su fuente).
- 07: §4.3 (el canal del Remolino), §5.4 (filas 0,60 y 0,80) y §8.2 (la fuente en la tarjeta).
- 08: §4.5 (el `src` del ejemplo, `lines[].kind` y el «Origen») y §4.7 (la regla `.source` del contrato 32).
- 09: §1.2 (la (a) de la 24, a estudiar para la 1.1).
- En cada capítulo de esta lista, su línea de fecha («actualizado el 2026-10-19 (DC1) con D-028»).
- Este registro: la tabla, la fila P5 de §5, la nota de D-002 sobre el dedo guía y las notas de D-007, D-013 (punto 5), D-016 (punto 5) y D-023 (punto 20).
- `docs/dev/traspaso/README.md`: §6, paso 4 (las preguntas respondidas salen de los pendientes del dueño) y §7 (la fila de D-028 y la de D-016).

**Alternativas descartadas.** 19 (b); en la 22, que el layout reserve el cielo de la cuia elevada; 23 (b) y (c); 24 (a) en la 1.0 (queda para estudiar en la 1.1); 53 (b) y (c); 54 (b) y (c); 57 (b) y (c); 60 (a) y (c); 61 (b); P5, «no» (sin dedo guía en 1-2 y Morpho sin destino); 65: (1) (a) y (b) (la (c) sigue abierta como vía para quitar excepciones), (2) (b) y (3) (b)-(d).

**Cómo revertir.** Con una decisión nueva que sustituya a D-028, pregunta a pregunta. Ninguna toca datos publicados salvo la 57 (filas nuevas de `facts.csv`, que se regeneran con `stops.py`) y la 65 (que se rehace regenerando `river_map_v1.json` y `river_stops_v1.json` con otras opciones y probando de nuevo en el teléfono).

**Archivos afectados.**
- **Documentación** (2026-10-19, DC1): este registro y los de «Qué cambia en el GDD».
- **Código** (previsto, ola 7): `Assets/_Project/Scripts/{GameFlow, UI, Presentation}` (DC2-DC7 y DC10), `tools/river-map/` y el bloque `map.*` de `Assets/_Project/Data/Loc/facts.csv` (DC8 y DC9) y los textos del núcleo en `Packages/com.amazonfill.core/` (DC11), con sus tests; las listas exactas, en cada PR.

---

## D-029 · Respuestas del 2026-10-19 a las preguntas con recomendación que seguían abiertas

- **Fecha:** 2026-10-19, después de D-028. Registrada el 2026-10-19 (DC1b, tanda 4, ola 7).
- **Estado:** decidida por el dueño en el chat, literal: «resolvamos las preguntas, pero si tienen recomendadas vamos con esas». Y, para la pregunta de DC7 («¿el dedo guía entra también en el primer uso de la Hoja (43) y en el tutorial de la Tinaja (323)?»), la recomendada, «sí», queda aceptada.
  - Gana la recomendación de cada pregunta abierta que la tenía, tal como estaba escrita el 2026-10-19: en `docs/GDD.md` §6.3 (las filas 18, 21, 25, 26, 52, 55, 56, 58, 59 y 62-64 y las partes 1, 3 y 4 de la 66, que comparten la recomendación (a) de su fila), en este registro (P7 de §5, P14-P17 de D-008 y las preguntas de D-010) y en la propuesta de Magic Sort (`docs/research/magic-sort/propuesta-tintalu.md` §10.2). Donde la recomendación es condicional («tras el asesor», «cuando el banner lleve ese texto», «hasta simular B»), se registra la condición y no se cumple por adelantado.
  - Es un número aparte de D-028: otra frase del dueño, con otro alcance (todas las preguntas con recomendación, no una lista cerrada), y otro commit. La confirmación del alcance de DC7 va aquí y no en D-028, que solo dice P5.
  - Documentación aplicada el 2026-10-19 (DC1b): en `docs/GDD.md` §6.3 las doce filas pasan a decididas y la 66, salvo su parte 2, también, con la última columna al día; la regla de cada respuesta va a su capítulo («Qué cambia en el GDD»). El código que falta, en las tareas DC6, DC7 y DC12-DC16 de la ola 7.
- **Sustituye:**
  - los [P] y las opciones abiertas de las preguntas de abajo;
  - en 05 §4.1 y §5, la condición de dos rechazos de la Canoa extra y la caducidad de 10 min de «Canoas infinitas 1 h» (P17 A);
  - en 03 §11, el 15 % y el 15-20 % de Difícil de 61-200 (pregunta 26);
  - en 07 §2.1 y en la guía visual, el agua negra #4A2C14 (Q14);
  - en GDD.md §7.3, V5: las cifras [sim] del GDD pasan a la corrida con el ciclo de D-009 (`0faf51e`).
- **No sustituye:** D-002 (el dedo guía de la Hoja y de la Tinaja son ayudas explícitas, como el de 1-2); D-016, punto 5; D-023, punto 39 (una tarjeta por nivel tras la intro); D-007 Q17 A (ninguna mecánica tapa contenido); D-009 (el ciclo y sus etiquetas no cambian: la 26 solo alinea la tabla de 03 con la cuenta del ciclo); GDD.md §2 #11 (misma semilla al reintentar); 04 §7.1; los contratos 9 y 29.

**Contexto.** Tras D-028, el mismo día, quedaban en `docs/GDD.md` §6.3 las preguntas 18, 21, 25 y 26 (pasos 1 y 2 del traspaso), la 52 (TI6a), las 55, 56, 58, 59 y 62-64 (olas 5 y 6; M4.6) y la 66 (ola 7); en este registro, P7 de §5, P14-P17 de D-008 y las preguntas del guion de la intro y de la propuesta de fauna de D-010; y las de la propuesta de Magic Sort (§10.2) que D-005, D-006 y D-008 no respondieron. El plan de la ola 7 (DC1b) separó las que tenían recomendación de las que no la tenían o la tenían en conflicto con el canon.

**Decisión.**

*Preguntas de `docs/GDD.md` §6.3.*
1. **18 → (a): el «?» se apaga en las cuñas finas.** Con la cuia inclinada, el «?» de una dosis turbia se apaga en las cuñas de menos de 0,30 u y solo queda la trama, un instante: es una excepción de 07 §3, fila 13 (el Cacao junto al agua parda). Ya aplicada desde el paso 1.
2. **21 → se ratifican los [P] de la tarjeta previa y los tutoriales** del paso 1 (la tarjeta única del 100, «agua» en el 27, el Matapi sobre agua oscura, `_FaceDilate` 0,18, sin «Toca para continuar», la intro retenida en t = 0 y el velo de 0,55). Ya aplicados.
3. **25 → (a): se aprueban las cifras [sim] del ciclo de D-009** y se llevan a 05, 01, 04, 09 y `docs/GDD.md` (contrato 18; V5 cerrada). Selección de celdas en D-009 («Consecuencias»). Sin cambios en `tools/econ_sim.py` ni en `tools/out/` (`econ_sim.py --runs 300` reproduce `econ_B.md` byte a byte). Aplicada en la documentación (DC1b).
4. **26 → (a): 03 §11 pasa a la cuenta del ciclo** (el 25 % de Difícil de 05 §3.1 desde el 61, con el 5 % de Muy difícil) y se vigila el acierto de etiqueta (D-009, punto 9: si baja del 60 % en un ciclo, se revisa el generador de esa posición). El generador no cambia. Aplicada en la documentación (DC1b).
5. **52 → (a): la cadencia de TiTino cuenta `stats.levels_won`.** Ya aplicada (TI6b, `86318f8`). Falta verla en el teléfono.
6. **55 → (a): sin «Fruta nueva» en el 1 ni en el 2.** Gana la instrucción del tutorial (una tarjeta por nivel); la frase del taperebá se lee en su ficha y en la tarjeta de su parada. Ya aplicada (`SightingRule.FruitNewAt`); documentación en 06 §2.2 y §4.5 (DC1b); el comentario del código, en DC16.
7. **56 → (a): el tooltip de Marea del 3 sale tras el primer vertido que no acerca, sobre el muelle con la cola en Marea.** Ya aplicada (TU1a); documentación en 02 §13, 04 §2.1 y 06 §2.2 (DC1b); el [P] de `TutorialInstruction`, en DC6. El respaldo del 5 («al inicio del nivel 5 si no ocurrió») no entra: es una pregunta sin recomendación (abajo). Decidido el mismo día, después, en la (b): una vez en la vida, guardado en el perfil, con el respaldo del 5 (D-030, pregunta 15; DC23).
8. **58 → (a): el orden del resumen del mundo 8**, con las dos bajadas declaradas (`baja_por_confluencia`) y la excepción de `--check`. Ya aplicada; las marcas de «pregunta abierta» y [P] de `tools/river-map/` y `docs/research/mapa-del-rio/`, en DC12.
9. **59 → (a): la pregunta del mundo 9 en la cabecera del Mapa; la del arco V-VI, en el banner del 561 cuando el banner lleve ese texto [P].** Aplicada en el Mapa. El banner no tiene línea de pregunta: no hay tarea hasta que la tenga.
10. **62 → (a): se ratifican los [P] de la ola 5.** Ya aplicados.
11. **63 → (a): se ratifican los [P] de la ola 6**, con la pista por inactividad encendida por defecto. Ya aplicados. El glosario §10 quita el [P] a `fact_sheet_title`, `fact_locked`, `fact_starts_at`, `fact_sources` y `fact_a11y` y suma `settings_about_amazon` (DC1b).
12. **64 → (a): se ratifican las tres [P] del audio de la entrega 1**: la lluvia fija por nivel (50 % ligera, 35 % media y 15 % fuerte), los remates a −8 dB de su pico (con el ajuste Música y tras la toada) y los efectos en Vorbis 0,8. Ya aplicadas (AU3); falta escucharlas (acción del dueño). Era la «decisión con el siguiente número libre» que pedía la fila 64.

*Preguntas de este registro.*
13. **P7 (§5, D-001/D-005), reescrita → sí:** el formato no declara el modo (los packs se generan siempre en `run`) y P7 se retira. Ya aplicada.
14. **D-008, P14 → sí:** los textos nuevos del embudo y el nombre «Sin canoas». Sin [P] en 04 §6.5 y en el glosario §10 (DC1b); las notas de `strings.csv`, en DC16.
15. **D-008, P15 → sí:** Marea, primera opción del modal. Ya aplicada.
16. **D-008, P16 → sí:** desde el 31, salir del nivel cuesta una canoa también con 0 vertidos. Ya aplicada (`LevelFlow.Funnel`; el comentario, en DC16).
17. **D-008, P17 → A:** «Canoas infinitas 1 h» sale siempre en Sin canoas, detrás de esperar, la Canoa extra por vídeo y las semillas, y sin cuenta atrás, como 04 §7.3 y como lo hace ya `NoCanoesScreen`. 05 §4.1 y §5 se corrigen (el lote Dc4 que estaba pendiente; DC1b). D-008 queda sin pendientes del dueño.
18. **DC7 (P5 ampliada) → sí:** el dedo guía entra también en el primer uso de la Hoja (43) y en el tutorial de la Tinaja (323), como piden las fichas M1 y M13 de 02 §9.1, con el componente de DC6. Tarea: **DC7**.
19. **M5.1c → se ratifica el trazo suave** que propuso la revisión de M5.1: puntos a 1,5 anchos del trazo (`SpacingWidths`) y bisel donde el inglete pasaría de 1,25 (`BevelMiter`). Es la parte 3 de la pregunta 66 de `docs/GDD.md` §6.3. Ya aplicado; las marcas, en DC16 (tras FX11). `tools/river-map/README.md` no se toca aquí.
    - Con la misma recomendación de la fila 66, (a), se ratifican sus **partes 1 y 4**. Parte 1, las placas de los mundos futuros (FX10): la que no cabe de ningún modo no sale en la vista Mundo con ese encuadre, no cuenta como obstáculo y vuelve al arrastrar; en la Cuenca nunca se oculta. Parte 4, la Tinaja (TJ3 y TJ4): el consejo del 324 va en el aviso de texto, solo si el nivel lleva tinaja; y el «tic al empezar» de sacar (02 §4.1, b.2) sigue sin sonar ni tener id en 07 §9 y en la lista de sonidos: si se quiere, es una pieza nueva del encargo de sonidos, que no se encarga por adelantado. Ya aplicadas (`6523058`). Los textos nuevos de la Tinaja siguen pendientes del asesor.

*Preguntas de D-010 (guion de la intro y propuesta de fauna).*
20. **Q11 → sí:** `af_fauna_sighted` al mostrar la tarjeta, desde la 1.0, con el avistamiento guardado en el perfil (evento y migración aprobados). Tarea: **DC15**; alta del evento en 08 §8.2 (DC1b). La revisión de la pregunta 41 (2) queda resuelta con ella: sin tarjeta no hay avistamiento.
21. **Q14 → sí:** el agua negra, en ámbar-té translúcido que conserva los 29 puntos de L* frente al tinte 14, tras la captura de prueba. Hecha en **DC13**: #522500 al 70 % (ΔL* 29,4 frente al tinte 14; 31,5 compuesta sobre la cavidad). 07 §2.1 y la guía visual, al día (DC1b).
22. **Q17 → solo el vaciado:** en el mundo 11 el hoatzin solo hace el vaciado de Volver a la orilla, y la intro del reintento va en modo dosel, como en todos los mundos. Sin código hasta el mundo 11.
23. **F7 → sí, tras el asesor:** el glosario §3.3 y 06 §4.6 se amplían con la página Fauna (miquitos y río y selva) cuando lo dé el asesor; hasta entonces, una nota.
24. **F9 → sí:** microinteracciones con la fauna del fondo (como mucho 2 animales por mundo, fuera del panel, sin nada económico), midiendo que no aumenten los toques erróneos en las cuias. Al backlog, sin tarea en esta ola: lo especifica el tech-lead con la fauna del fondo.
25. **F10 → no:** la fauna no reacciona al sellar una cuia (solo en la victoria y los hitos).
26. **F11 → el tucuxi salta en las celebraciones; Rosa asoma y exhala.** Cambia 07 §6.2 **con el asesor**: una nota ahora; las poses y el código, cuando lo dé.
27. **F13 → un segundo gesto de la misma especie en la 1.1** (09 §1.2) y, después, invitados opcionales.

*Preguntas de la propuesta de Magic Sort (§10.2).*
28. **#8 → A** (Liana con la variante `key`): ya estaba en D-007.
29. **#10 → A:** Aguapé y Nasa con pez, al backlog tras el 183, con el asesor.
30. **#11 → A:** la frecuencia de intersticiales se mantiene y se vigila.
31. **#12 → se confirma:** clasificación G y categorías sensibles bloqueadas (apuestas, dinero real, trading y mercados de predicción) en todas las bandas de edad, y la prueba en el dispositivo de que «Close» responde al primer toque. 05 §7 lo exige en todas las bandas (DC1b); se implementa con MAX (sdk-integrator, fase de anuncios).
32. **#13 (grabaciones) → A:** es una acción del dueño.
33. **#16:** ya respondida por D-013.
34. **#17 → B:** el bono diario deja elegir 1 de 3 premios visibles.
35. **#18 → B:** un panel de «premios del día» tras ganar.
36. **#19 → C:** hitos de racha 3 · 6 · 10 hasta simular la B. Es lo que hace la Crecida.
37. **#21 → B:** un reloj diario común con un solo evento temporal.
38. **#22 → B:** sin Canoas infinitas en la Mochila de inicio. Como está.
39. **#23 → A:** la oferta temporal, como icono del inicio desde el 31, con cuenta atrás y sin abrirse sola.
40. **#24 → B:** level-qa mide las trampas tempranas con tope por etiqueta. Ya está (QA-EARLY-TRAPS; D-007, Q16 B).
41. **#25 → A:** «¡Perfecto!» solo cuando el intento se gana sin Cuia de rescate. Tarea: **DC14**; la regla, en 02 §6 y 07 §5.4, y la clave `victory_perfect` [P], en el glosario §10 (DC1b; si DC14 usa otra, se corrige allí).
- **#15 no se registra como aceptada:** la cerró D-007 Q17 A (ninguna mecánica tapa contenido), y D-019 llevó el contenido oculto a la Tinaja.
- **#17, #18, #21 y #23** son de sistemas que aún no existen (el bono diario de la Bitácora, el panel de premios, los eventos y las ofertas temporales): se registran ahora, con una nota de una línea en su sección (05 §5; 06 §4.2, §4.7 y §5.1), y el rediseño de esas secciones se hace cuando se planifiquen.

**Fuera de esta decisión** (siguen pendientes del dueño):
- **el espacio del AAB** (Addressables y PAD, arte a 1× y el margen de las fuentes de FNT1) y **el permiso `INTERNET`**: el dueño los decide aparte, como en D-028;
- las preguntas **sin recomendación o en conflicto con el canon**, que siguen abiertas con su [P]: las «Paradas en una confluencia [P]» de M5.1 (punto 2 de la pregunta 66 de `docs/GDD.md` §6.3; sus puntos 1, 3 y 4 sí entran: arriba, punto 19); P12 de D-006 (16:9 con 13-14 cuias); las dos de D-021 (la velocidad del lanzamiento lateral y la barra de la Crecida); las #2, #9, #14 y #20 de la propuesta de Magic Sort (reabren decisiones del canon); la insignia del nivel en otros idiomas (L4); el rótulo del lugar sobre la silueta y la silueta con 3 filas; la ventanita de casa con 0 canoas; la victoria con vista que enseña poco sitio; la temporada de SE2, que apenas se nota; el formato de las distancias; el respaldo del tooltip de Marea en el 5; los dos [P] de TiTino (las frases fijas que se repiten y la lectura del saludo de la mañana); el tope 0,7 del cielo de `StopBackdrop` (hoja 02f); y `profile_locked_title` (aviso o pantalla de bloqueo). **Decididas el mismo día, después, con la opción que propuso el plan de la ola 7 para cada una** (D-030);
- las «Preguntas abiertas» antiguas de los capítulos 00-09 que no tenían recomendación (negocio, legal y publicación): fuera de este barrido; se revisan en la fase que las necesite.

**Consecuencias.**
- **Código** (ola 7 de la tanda 4; sin paquetes, SDK, permisos ni versiones nuevas): DC13 (agua negra, hecha en su rama), DC14 («¡Perfecto!»), DC15 (avistamiento y `af_fauna_sighted`), DC16 (marcas «pendiente del dueño» de preguntas ya decididas), DC12 (marcas de la 58 en las herramientas del Mapa), DC6 (el [P] del 3) y DC7 (el dedo guía de la Hoja y de la Tinaja).
- **Analítica:** `af_fauna_sighted` {`species_id`, `level`, `first`} entra en 08 §8.2 (Q11).
- **Economía:** las cifras [sim] del GDD pasan a la corrida con el ciclo de D-009 (contrato 18); sin cambios en `tools/econ_sim.py` ni en `tools/out/`.
- **Sin cambios** en Remote Config, el formato de nivel ni los packs.

**Qué cambia en el GDD** (aplicado el 2026-10-19, DC1b):
- `docs/GDD.md`: versión 1.17, estado, convención [sim], §2 (#16), §3.3 (la fila de `econ_B_tramos.md`), §6.1 (#4, el sumidero), §6.2 (la Cuia de rescate), §6.3 (entrada, filas 18, 21, 25, 26, 52, 55, 56, 58, 59 y 62-64, y las partes 1, 3 y 4 de la 66), §7.1 (contrato 18), §7.3 (V5 cerrada), §8 (punto 3) y §9 (pasos 12 y 19).
- 00: §3.3 (nota de F7 y Q17), §6.4 («Sin canoas») y §10 (las claves de P14, las de la ficha sin [P], `settings_about_amazon` y `victory_perfect` [P]).
- 01: §7.2 (el primer bloqueo del casual).
- 02: §6 («¡Perfecto!»), §8 (el tutorial del 27, la 21), §9.1 (M1 y M13: el dedo guía, DC7), §13 (fila 3) y Preguntas abiertas 11 y 12.
- 03: §5 (P7), §11 (tabla y nota de vigilancia) y Preguntas abiertas 8 y 9.
- 04: §2.1, §2.5 (la tarjeta del 100, la 21), §2.7 (Q17), §5.2 y §6.2 (P16), §6.5 (P14), §7.3 (P17), §9 (cifras [sim]) y Preguntas abiertas 9.
- 05: Resumen, §3 (cifras [sim]), §4.1 y §5 (P17), §5 (#23), §7 (#12), Desviaciones, Conflictos, pregunta 2 y Dependencias.
- 06: §2.2 (filas 2 y 3), §4.2 (#17), §4.5 (la «Fruta nueva» desde el 4), §4.6 (F7), §4.7 (#18) y §5.1 (#21).
- 07: §2.1 y §3 (agua negra, Q14), §3 (fila 13, la 18), §5.4 («¡Perfecto!»), §6.2 (F11), §6.4 (Q11), §7.7 (la demostración del 183, la 21), §9 (la 64) y Preguntas abiertas 11.
- 08: §4.2 y §5.1 (agua negra), §5.4 (la fila «Audio»; la 64) y §8.2 (`af_fauna_sighted`).
- 09: §1.2 (F13).
- En cada capítulo de esta lista, su línea de fecha («actualizado el 2026-10-19 (DC1b) con D-029»).
- Fuera del GDD: la guía visual (revisión 11 y el token `agua_negra`), el guion de la intro (§18: Q11, Q14 y Q17), la propuesta de fauna (F7-F13), la nota de respuestas de la propuesta de Magic Sort y `docs/dev/prototipo-v0.md` (P7).
- Este registro: la tabla (D-008, D-009, D-010 y D-029), P7 de §5, las notas de D-008 (P14-P17, #20 y #25), D-009 (las cifras y las etiquetas) y D-010 (las tablas de Q y F y sus pendientes de documentación), y la lista de D-028 («Fuera de esta decisión»).
- `docs/dev/traspaso/README.md`: §6, paso 4 (las respondidas salen de los pendientes del dueño) y §7 (las filas de D-008, D-009, D-010, D-016, D-017 y D-029).

**Alternativas descartadas.** 18 (b); en la 21, la 62 y la 63, cambiar alguno de los [P]; 25 (b); 26 (b); 52 (b); 55 (b); 56 (b) y (c); 58 (b) y (c); 59 (b) y (c); 64 (b); P7, declarar el modo en el formato; P14-P16, «no»; P17 B; DC7, «no»; Q11 B y C; Q14 B; Q17, que el hoatzin también suelte la fruta; F7 B, F9 B, F10 B, F11 B y F13 C; y las opciones no recomendadas de #8, #10-#13, #17-#19 y #21-#25.

**Cómo revertir.** Con una decisión nueva que sustituya a D-029, pregunta a pregunta. La 25 se revierte volviendo a citar la corrida del 2026-10-06 (`git show 0faf51e^:tools/out/econ_*.md`); la 26, con la tabla anterior de 03 §11; Q14, con `Palette.WaterBlack` = #4A2C14. Ninguna toca packs ni datos publicados.

**Archivos afectados.**
- **Documentación** (2026-10-19, DC1b): este registro y los de «Qué cambia en el GDD».
- **Código** (ola 7): `Assets/_Project/Scripts/{GameFlow, UI, Presentation, Save}` (DC6, DC7, DC13-DC16), `Assets/_Project/Data/Loc/strings.csv` (DC14 y DC16), `tools/river-map/` y `docs/research/mapa-del-rio/` (DC12), con sus tests; las listas exactas, en cada PR.

---

## D-030 · Respuestas del 2026-10-19 a las 19 preguntas sin recomendación

- **Fecha:** 2026-10-19, después de D-029. Registrada el 2026-10-19 (DC1c, tanda 4, ola 7).
- **Estado:** decidida por el dueño en el chat, literal: «Acepto tus propuestas para las 19 preguntas sin recomendada: 1a, 2c, 3a, 4c, 5a, 6b, 7a, 8b, 9b, 10a, 11a, 12a, 13b, 14a, 15b, 16b, 17b, 18b y 19a.»
  - La numeración (1-19) y las letras son las de la lista de preguntas sin recomendación del plan de la ola 7 (`no_recommendation`, en el mismo orden que la lista «Fuera de esta decisión» de D-029, salvo la 11 y la 12, que allí van al revés). Ninguna letra contradice su pregunta.
  - En dos preguntas, las letras no casan con las de la fuente, la propuesta de Magic Sort (`docs/research/magic-sort/propuesta-tintalu.md` §10.2), que ordena sus opciones de otro modo. Aquí se registran **con la letra de la fuente** y la de D-030 entre paréntesis: la 6 (b), «mantener el canon», es **#9 C y #14 C** («no»); la 7 (a), «misma semilla», es **#20 C** (en la fuente, A es «variante sin aviso» y B, «elegir Mismo río u Otro río»). La 5 (a) coincide con **#2 A** (y con la pregunta 4.2 A de la fuente). En las demás, la letra casa con su fuente (P12 de D-006: a-c; las dos de D-021: a-c).
  - Es un número aparte de D-029: otra frase del dueño, con otro alcance. D-029 aceptó las preguntas *con* recomendación; esta elige entre las opciones de las que *no* la tenían, que D-029 dejaba abiertas.
  - Documentación aplicada el 2026-10-19 (DC1c). El código que falta, en las tareas DC17-DC25 de la ola 7.
- **Sustituye:**
  - los [P] y las opciones abiertas de las 19 preguntas de abajo;
  - en 02 §5, «ganan las 2 filas con 11-14 cuias» en 16:9 (pregunta 2);
  - en el guion de la intro (§4.4) y en 07 §5.1 (fase d), el tope de 250 dp/s para la tropa lateral (pregunta 3);
  - en la guía visual 3.5, el «mínimo 20 dp» de la insignia fuera del español (pregunta 8);
  - en 07 §2.1 y en la propuesta del Mapa (§10.5, punto 5), el rótulo «sobre el horizonte» con 1-2 filas (pregunta 9);
  - el 0,7 de `StopBackdrop` en el tercio de abajo del cielo con alba y atardecer (revisión de M1.4; pregunta 18).
- **No sustituye:** D-006, punto 4 (las filas se fijan al cargar y no cambian a mitad de nivel); D-002 (no se resalta destino); D-009, punto 6 (la temporada solo se ve en la luz, sin cartel); D-016, punto 5, y D-028 (la cadencia y los topes de TiTino); D-018, Q8-A, y D-025 (las cifras con la región del teléfono); GDD.md §2 #11 (misma semilla al reintentar); 04 §7.1 y la pregunta 13 de GDD.md §6.1 (sin canoas infinitas gratis); los contratos 9 (la pista por inactividad) y 29 (el formato del Mapa).

**Contexto.** D-028 y D-029 dejaron abiertas las preguntas sin recomendación aplicable o en conflicto con el canon (lista en D-029, «Fuera de esta decisión»): la parte 2 de la pregunta 66 de `docs/GDD.md` §6.3; P12 de D-006; las dos de D-021; las #2, #9, #14 y #20 de la propuesta de Magic Sort; y las observaciones de las pruebas en el teléfono de las olas 2, 3 y 5 y de las revisiones de L4, SE2, TU1a, TI6, M1.4 y S12. El plan de la ola 7 propuso una opción para cada una, sin recomendarla, y el dueño las aceptó todas.

**Decisión.**

| n | Pregunta (fuente) | Opción | Qué se registra | Estado |
|---|---|---|---|---|
| 1 | Paradas en una confluencia [P] (revisión de M5.1; `confluence_s`; parte 2 de la pregunta 66 de `docs/GDD.md` §6.3) | (a) | Santarém, Boca Manu y el resto de la lista se anclan en la boca del afluente (a ≤ 5 km, `CONFLUENCE_KM`), para que el Tapajós y el Manu se llenen al llegar. Sin [P] en 08 §4.5 y en D-013, punto 5 | Ya aplicada; marcas: DC17 (`tools/river-map`, hecha en su rama) y DC22 (tests de Unity) |
| 2 | P12 de D-006: 16:9 con 13-14 cuias | (c) | 3 filas al cargar cuando T + 2 > 14 (T generadas ≥ 13), en cualquier proporción: ninguna fila pasa de 7 cuias con la Cuia extra y la de rescate. Las filas siguen fijándose al cargar (D-006, punto 4). 02 §3.3 y §5 y 07 §7.4, sin la ranura de 42 dp | DC18 |
| 3 | D-021: velocidad del lanzamiento lateral | (a) | Excepción al tope de 250 dp/s para la tropa lateral: impulso horizontal de hasta ~1.150 dp/s, siempre de lado y por debajo del hombro. El tope sigue para la suelta desde la rama. Guion §4.4 y 07 §5.1 (fase d) y §6.4 | Ya aplicada; comentario del código: DC22 |
| 4 | D-021: barra de la Crecida frente al miquito de la derecha | (c) | La barra de la Crecida aparece en T, al habilitarse el toque (07 §7.4): durante la intro, que no admite toques, la tropa lateral puede pasar por su franja. Hoy la barra no existe en el cliente: es un requisito para la tarea que la construya | Sin código ahora (registro) |
| 5 | Magic Sort #2 (y su pregunta 4.2): pista por inactividad | (a), #2 A | Como está: desde el 3, en todos los niveles y encendida por defecto (02 §3.4; contrato 9 sin cambios) | Ya aplicada |
| 6 | Magic Sort #9 y #14: canoas infinitas cortas | (b), #9 C y #14 C | Se mantiene el canon: sin canoas infinitas gratis en la Bitácora ni en la Canasta grande, ni 15 min por un vídeo en Sin canoas (04 §7.1 y la pregunta 13 de GDD.md §6.1, sin cambios). No se simula | Ya aplicada |
| 7 | Magic Sort #20: reintento | (a), #20 C | Misma semilla (GDD.md §2 #11), con «El río bajó» al 3.er fallo, como hoy (D-008, punto 7) | Ya aplicada |
| 8 | L4: insignia del nivel en otros idiomas | (b) | La insignia va a 20 sp en es-419 y solo baja si no cabe, hasta un mínimo de 12 sp (guía visual 3.5; 02 §12); la placa no crece y el texto nunca se recorta | DC19 |
| 9 | Rótulo del lugar sobre la silueta (prueba de la ola 2, observación 1) | (b) | Como placa también con 1-2 filas, igual que con 3 (D-013, punto 8; propuesta del Mapa §10.5, punto 5; 07 §2.1) | DC24 |
| 10 | Silueta invisible con 3 filas (prueba de la ola 2, observación 2) | (a) | Se acepta: el sitio se ve en la victoria con vista y en el banner | Ya aplicada |
| 11 | Victoria con vista que enseña poco sitio (prueba de la ola 3, observación 2) | (a) | Nada más: basta con TiTino en la franja (DC2, D-028) | Aplicada en `tanda4/o7-dcu1` (DC2, `65af256`), pendiente de integrar |
| 12 | Ventanita de casa con 0 canoas (prueba de la ola 3, observación 1) | (a) | Se sigue mostrando en el inicio; solo se oculta con Sin canoas encima | Ya aplicada |
| 13 | SE2: la temporada apenas se nota | (b) | Reforzarla en lo que queda a la vista (el cielo y la franja), con las cifras de la revisión de SE2 medidas ahí: ΔE00 medio ≥ 5 frente al fondo sin temporada y ≥ 8 frente a la otra temporada, con 2 y 3 filas [P] (07 §2.1; D-009, punto 6). Sigue sin cartel | DC21 (las LUT y alfas finales, de su PR) |
| 14 | Formato de las distancias (prueba de la ola 5, observación 6) | (a) | Cifras con la región del teléfono (D-018 Q8-A y D-025, sin cambios) | Ya aplicada |
| 15 | Tooltip de Marea: respaldo del 5 | (b) | Una vez en la vida, guardado en el perfil, y al inicio del 5 si no salió en el 3 (02 §13; 06 §2.2). 08 §4.3: un campo nuevo [P] (nombre, de DC23) | DC23 (nombre del campo, de su PR) |
| 16 | TiTino: frases fijas que se repiten | (b) | Alternativas para las 4 fijas (n.º 21, 25, 29 y 30 de la propuesta de TiTino, e.4): 2 por frase [P], con estado `pendiente_asesor`. Glosario §10 y propuesta de TiTino, e.4 | DC25 (claves [P], de su PR); el asesor, acción del dueño |
| 17 | TiTino: saludo de la mañana | (b) | La primera apertura **dentro** de la franja 06:00-12:00 (hora local), con `sloth.last_greeting_date`: quien abre a las 05:30 y otra vez a las 07:00 tiene el saludo a las 07:00. Propuesta de TiTino, d.7 y d.10 | DC25 |
| 18 | `StopBackdrop`: cielo del alba y el atardecer | (b) | Opacidad ≤ 0,5 también en el tercio de abajo (plan del Mapa, M1.4, punto 1a, sin excepción). Si DC20 mide que el alba de la parada 19 y el atardecer de la 9 no se distinguen, se anota como observación para el dueño, sin inventar otra solución | DC20 |
| 19 | `profile_locked_title`: perfil en solo lectura | (a) | Aviso, y se sigue jugando sin guardar (08 §4.4; ya lo hace el cliente: `profile_locked_play`, captura `20_profile_locked`) | Ya aplicada; nota de `strings.csv`: DC22 |

- **Sin regla nueva inventada:** la 4 queda como requisito para cuando exista la barra de la Crecida; el nombre del campo de la 15 y las claves y los textos de la 16 son [P] de sus tareas (DC23 y DC25) y se corrigen aquí, en 08 §4.3 y en el glosario §10 si su PR los fija de otro modo; las cifras de la 13 son las de la revisión de SE2, [P] hasta que DC21 las mida.
- **El plan del Mapa** (M1.4) vive fuera del repositorio, como los demás planes de la tanda 4 (D-023): esta entrada es su copia para la 18.

**Fuera de esta decisión** (siguen pendientes del dueño):
- **el espacio del AAB** (Addressables y PAD, arte a 1× y el margen de las fuentes de FNT1) y **el permiso `INTERNET`**: el dueño los decide aparte, como en D-028 y D-029;
- las **acciones del dueño** del plan de la ola 7 (el teléfono de clase A, la curación de los mundos 6-9, las pruebas en el teléfono y la prueba visual de 30 min, el audio, el asesor cultural, que ahora suma los 8 textos nuevos de DC25, el artista, las grabaciones de Magic Sort, el abogado de marcas y los 15 min del build limpio);
- las «Preguntas abiertas» antiguas de los capítulos 00-09 que no tenían recomendación (negocio, legal y publicación): fuera de este barrido; se revisan en la fase que las necesite.

**Consecuencias.**
- **Código** (ola 7 de la tanda 4; sin paquetes, SDK, permisos ni versiones nuevas): DC17 (las marcas de la 1 en `tools/river-map`), DC18 (la 2: `BoardLayout.RowsFor`), DC19 (la 8: la insignia), DC20 (la 18: el cielo), DC21 (la 13: la temporada), DC22 (las marcas de la 1, la 3 y la 19), DC23 (la 15: el tooltip de Marea), DC24 (la 9: el rótulo) y DC25 (la 16 y la 17: TiTino).
- **Perfil:** un campo nuevo [P] (la 15; DC23), sin migración mientras el esquema no se publique, como `map_seen_level` (08 §4.3). La 17 usa `sloth.last_greeting_date`, que ya existe en el perfil del cliente.
- **Textos:** 8 claves nuevas de TiTino [P] (la 16; DC25), en es-419 con `pendiente_asesor`.
- **Sin cambios** en Remote Config, la analítica, el formato de nivel, los packs ni la economía (ninguna cifra [sim]: la 6 mantiene el canon y no se simula).

**Qué cambia en el GDD** (aplicado el 2026-10-19, DC1c):
- `docs/GDD.md`: versión 1.18, estado y §6.3 (entrada y la parte 2 de la 66; §2 #11, sin cambio).
- 00: §10 (las alternativas de TiTino y el formato de las cifras).
- 02: §3.3 y §5 (la 2), §3.4 (la 5) y §13 (la 15).
- 06: §2.2 (la 15).
- 07: §2.1 (la 9 y la 13), §5.1 (la 3 y la 4), §6.4 (la 3) y §7.4 (la 2 y la 4).
- 08: §4.3 (la 15), §4.4 (la 19) y §4.5 (la 1).
- En cada capítulo de esta lista, su línea de fecha («actualizado el 2026-10-19 (DC1c) con D-030»).
- Fuera del GDD: la guía visual (revisión 12 y 3.5: la 8), el guion de la intro (§4.4 y §7.1: la 3 y la 4), la propuesta de Magic Sort (la nota de respuestas: la 5, la 6 y la 7), la propuesta de TiTino (cabecera, d.7, d.10 y e.4: la 16 y la 17) y la del Mapa (la cabecera y §10.5, punto 5: la 1, la 9 y la 18).
- Este registro: la tabla (D-006, D-008, D-021, D-029 y D-030), P12 de D-006, el punto 7 y las preguntas de D-008, el punto 5 de D-013, el estado de D-021 y las listas «Fuera de esta decisión» de D-028 y D-029.
- `docs/dev/traspaso/README.md`: §1 (pendientes tras la ola 4), §6, paso 4 (las respondidas salen de los pendientes del dueño) y §7 (las filas de D-006, D-008, D-021, D-028, D-029 y D-030).

**Alternativas descartadas.** 1 (b); 2 (a) y (b); 3 (b); 4 (a) y (b); 5 (b) y (c), es decir, #2 B y C; 6 (a), es decir, #9 A y #14 A (y con la C quedan fuera también #9 B y #14 B); 7 (b), es decir, #20 B (y la #20 A, que el plan no ofrecía); 8 (a); 9 (a) y (c); 10 (b); 11 (b); 12 (b); 13 (a) y (c); 14 (b); 15 (a); 16 (a); 17 (a); 18 (a); 19 (b).

**Cómo revertir.** Con una decisión nueva que sustituya a D-030, pregunta a pregunta. Ninguna toca packs ni datos publicados; la 15 añade un campo al perfil que, mientras el esquema no se publique, se quita sin migrar.

**Archivos afectados.**
- **Documentación** (2026-10-19, DC1c): este registro y los de «Qué cambia en el GDD».
- **Código** (ola 7): `tools/river-map/` (DC17), `Assets/_Project/Scripts/{UI, Presentation, GameFlow, Save}` (DC18-DC25) y `Assets/_Project/Data/Loc/strings.csv` (DC22 y DC25), con sus tests; las listas exactas, en cada PR.

---

## D-031 · Reto del río: pool de 729 días, bandas, premio, anti-reloj y partida

- **Fecha:** 2026-10-19. Registrada el 2026-10-19 (enganche E11 del equipo Spark: `docs/dev/traspaso/spark-reto-del-rio.md` §5 y `docs/dev/traspaso/spark-estado.md`).
- **Estado:** decidida por el dueño en el chat, en cuatro respuestas del 2026-10-19:
  - **R-Q1-R-Q10:** «Acepto tus propuestas para R-Q1 a R-Q10 del Reto del río; déjalas en el encargo del Spark». La primera versión del encargo las fechaba el 2026-10-20; el encargo §5 y el reloj del Spark dicen 2026-10-19.
  - **R-Q1b, R-Q11-R-Q16, la aclaración de R-Q12 y R-Q12b:** «aplica las recomendaciones y pon trabajo en paralelo para avanzar mas rapido», con la regla del integrador «si tienen recomendada, vamos con esas» (encargo §5): la opción (a), la recomendada, de cada una.
  - **R-Q2b:** «Pool de 729 o 738 días» y, después, «729 días (Recomendado)».
  - **R-Q11b:** «Aceptar (Recomendado)».
  - **Confirmado por el integrador** (encargo §5): `IServerDate` y `FakeServerDate` van en AF.Platform (08 §2.2), no en AF.LiveOps.
  - **Implementada** por el equipo Spark (R0-R11) en `spark/reto-del-rio`: el bloque (a) (generador, QA y lector del pool) está integrado en `integracion-fase1-fase2` (`fcbbd94`); el pool publicado (R5, `3c40393`), la lógica (R6-R7b), la partida (R8), las pantallas (R9) y las sondas (R10), pendientes de integrar. Pendientes: los enganches E1-E25 de `spark-estado.md` y la prueba en el teléfono.

**Contexto.**
- El GDD fija el Reto del río (03 §16, 06 §4.1, 08 §4.1, §4.3, §8.2 y §9), pero deja huecos que el código necesita: la paleta y el agua de cada banda, qué pasa al cruzar la medianoche, si se reanuda, los campos del perfil, el formato de `daily_overrides`, la cadencia de la racha, los comodines, qué es `fail` y qué comprobaciones de la campaña valen en el pool (encargo §5, R-Q1-R-Q10).
- Al implementarlo salieron más: R-Q1 no cabía en B2 con K = 9 ni en B3 (con los tintes de su tramo hay al menos 3 pares de riesgo; `docs/dev/reto-del-rio.md` §1.1); con 1 vacío el bot falla 0,88-1,00 (03 §4.4), así que la ventana de B2 puede no alcanzarse; la reconciliación con el servidor, el intersticial y la Pororoca antes del nivel 30 no estaban decididos; y con 730 días y 9 mundos (730 mod 9 = 1) el mundo grabado en cada tablero deja de seguir a la fecha en la segunda vuelta del pool.

**Decisión.**

| Id | Decisión |
|---|---|
| R-Q1 | Cada banda usa solo los tintes ya estrenados en su tramo de la campaña: B1 (K 6-7) con los de 1-40 y `max_pares_riesgo` 0; B2 (K 8-9) con los de 1-80 y como mucho 1; B3 (K 10-11) con los de 1-200 y como mucho 2 (lo ajusta R-Q1b) |
| R-Q1b | (a) B2 solo con K = 8 y B3 con `max_pares_riesgo` libre, como la campaña desde el 201 (03 §4.5). B1 sin cambios |
| R-Q2 | El agua (`water_base`) y el fondo rotan por los mundos 1-9 según la fecha: el agua y el kit de fondo del mundo de ese día |
| R-Q2b | Pool de **729 días por banda** (81 × 9), 2.187 tableros: el día del pool es `(días_desde(2026-01-01) + live_daily_offset) mod 729` y el mundo del tablero, `1 + (día mod 9)`, que sigue a la fecha en todas las vueltas (con `live_daily_offset` constante, desplazado por él) |
| R-Q3 | Manda la fecha en que se empezó la partida: se sigue con ese tablero y se cobra ese premio aunque se gane pasada la medianoche |
| R-Q4 | La partida del Reto se reanuda al volver a abrir la app el mismo día; al cambiar de día se descarta |
| R-Q5 | El bloque `daily_challenge` del perfil añade la última fecha cobrada, el premio pendiente de validar y los sellos del mes (además de `last_date`, `result` y `streak_days`) |
| R-Q6 | `daily_overrides` = `{fecha: {banda: id del pool}}` |
| R-Q7 | La Pororoca sale cada 7 días seguidos ganados (cada racha completa); un día sin ganar rompe la racha; el marco de país de los sellos queda fuera hasta que existan cosméticos |
| R-Q8 | Los comodines son los de la campaña salvo los que el GDD excluye en el Reto (Cuia de rescate y Salto del Boto), y gastan el stock igual que en la campaña |
| R-Q9 | `result = fail` en `af_daily_challenge` es salir sin ganar ese día (lo concretan R-Q15 y R-Q16) |
| R-Q10 | La anti-repetición (03 §17 #9) y el tope de trampas tempranas (03 §17 #3) se miden en el pool y se informan como aviso, sin ser compuerta |
| R-Q11 | (a) La ventana del bot es compuerta: un día sin candidato dentro de la ventana no se publica hasta ajustar la especificación (fuente o K) |
| R-Q11b | El «máximo 30 candidatos» de 03 §4.4 se lee como **30 por fuente**, con respaldos en orden. No se relajan vacíos, K, capacidad ni ventanas |
| R-Q12 | (a) Al reconectar, la reconciliación no retira las semillas cobradas sin red |
| R-Q12 (aclaración) | (a) Al anular una fecha no se retiran semillas ni la Pororoca |
| R-Q12b | (a) Un premio pendiente solo se valida si `\|fecha − hoy_servidor\| ≤ 1 día`; los demás se anulan |
| R-Q13 | (a) Sin intersticial al terminar un Reto |
| R-Q14 | (a) La Pororoca de la racha entra en el stock y espera al nivel 30, que la desbloquea |
| R-Q15 | (a) `af_daily_challenge.result = fail` una vez por día, y solo si ese día termina sin victoria |
| R-Q16 | (a) Salir de la partida del Reto sin ganar la deja reanudable el mismo día; al cambiar de día se descarta (R-Q4) |

**Consecuencias.**
- **Datos:** `Assets/_Project/Data/Daily/daily_pool_v1.json`, 2.187 tableros y 1.430.135 bytes, generado por `tools/level-generator --daily` desde `tools/level-generator/specs/daily_v1.json` (no se edita a mano), con su informe `tools/out/daily_pool_v1_report.md`. `tools/level-qa --daily` lo da con 0 fallos y 1.678 avisos (los de R-Q10). Las fuentes de cada banda y `k_by_day` siguen siendo [P] de la especificación (03 §16).
- **Código** (archivos nuevos, sin paquetes, SDK, permisos ni versiones nuevas): `tools/level-generator/Daily/` y `tools/level-qa/Daily/`; `Scripts/LiveOps/` (asmdef AF.LiveOps, sin motor); `DailyPool` y `DailyPoolReader` en AF.Levels; `IServerDate`, `FakeServerDate` y `ClockSecurityConfig` (la fuente única de `sec_clock_max_drift_h`) en AF.Platform; `LevelFlow.Daily.cs` en AF.GameFlow; la tarjeta, la partida, el calendario de sellos y `DevProbe.Daily.cs` en AF.UI.
- **Formato de nivel:** sin cambios. El esquema admite `slot.day` 0-729 y el pool usa 0-728; el contrato 21 y `pack_schema` no cambian.
- **Remote Config:** sin claves nuevas; `daily_overrides` cambia de tipo (`{fecha: {banda: id}}`) con el mismo default `{}`.
- **Analítica:** sin eventos nuevos; R-Q15 y R-Q16 fijan cuándo sale `af_daily_challenge` (en la 1.0 no sale `abandon`). La Pororoca de la racha no tiene evento (08 §8.2 no lo define).
- **Economía:** sin cambios en `tools/econ_sim.py` ni en las cifras [sim] (05 §3.2 ya cuenta el Reto).

**Qué cambia en el GDD** (E11, en el mismo lote que este registro):
- `docs/GDD.md` §2, decisión 21, y la fila de la fase 3 (2.187 tableros; K 8 en B2); las incidencias cerradas de §6 (15 y 16) se quedan como historia;
- glosario (00), fila «Reto del río» (2.187 = 729 × 3; K 8 en B2);
- 01, «Dependencias» (2.187 tableros);
- 03 §4.4 (R-Q11b) y §16 (729 días y 2.187 tableros, B2 con K 8, paleta, agua, fuentes, compuerta y avisos), con las menciones de «Preguntas abiertas», 2, y «Dependencias»;
- 05 §6.1 (sin intersticial en el Reto, R-Q13);
- 06 §4.1 (índice mod 729, bandas, comodines, partida en curso, racha, sustituciones, reconciliación, anuncios y analítica; el pseudocódigo), con «Preguntas abiertas», 2, y «Dependencias»;
- 08 §2.2 (fila `AF.Levels`), §3, §4.1 (fila `kind` y la validación en `tools/level-qa`), §4.3 (bloque `daily_challenge`), §6.1, §6.2, §8.2 (`level_start` y `af_daily_challenge`), §9 (`daily_overrides` y `live_daily_offset`) y «Dependencias» (filas 03 y 06);
- 09, fila 19 de la tabla de decisiones, fase 3, F2 y «Dependencias» (2.187 tableros; K 8 en B2).

**Alternativas descartadas.**
- R-Q1b: (b) subir los topes (B2 a 3, B3 a 4); (c) mantener los topes y bajar K (B2 = 8, B3 = 9); (d) ampliar la paleta de B3 a 1-300 (no basta: el 13 añade 6-13 y 13-14).
- R-Q2b: que el cliente calcule el mundo con la fecha local (cambia el significado de `water_base`, contrato 21); aceptar el salto en la segunda vuelta; 738 días.
- R-Q11: (b) publicar el más cercano y avisar en QA; (c) ensanchar la ventana (cambia 03 §16).
- R-Q12: (b) anular las victorias cobradas sin red con fecha posterior a la del servidor (queda en el código detrás de `DailyReconcileMode`, sin usarse). Aclaración: (b) retirar las semillas que queden sin gastar.
- R-Q12b: (b) validar todo pendiente con fecha ≤ hoy del servidor.
- R-Q13: (b) intersticial como en la campaña. R-Q14: (b) otro premio en B1; (c) nada antes del 30. R-Q15: (b) un `fail` por salida. R-Q16: (b) salir cierra la partida.

**Cómo revertir.** Con una decisión nueva que sustituya a D-031. R-Q2b y R-Q11b obligan a regenerar el pool con la especificación cambiada (`--daily`), a cambiar el módulo del índice y a pasar `level-qa --daily`; R-Q12 (b) es una opción del código (`DailyReconcileMode`). Mientras el Reto no se publique, sin coste para el jugador.

**Archivos afectados.**
- **Herramientas y datos** (Spark, R2-R5): `tools/level-generator/specs/daily_v1.json`, `tools/level-generator/Daily/`, `tools/level-qa/Daily/` y `tools/level-qa/fixtures/daily/`, `tools/out/daily_sample/`, `tools/out/daily_sample_report.md`, `tools/out/daily_pool_v1_report.md` y `Assets/_Project/Data/Daily/daily_pool_v1.json`.
- **Cliente** (Spark, R6-R11): `Assets/_Project/Scripts/LiveOps/`, `Scripts/Levels/DailyPool*.cs`, `Scripts/Platform/IServerDate.cs`, `FakeServerDate.cs` y `ClockSecurityConfig.cs`, `Scripts/GameFlow/LevelFlow.Daily.cs`, `Scripts/UI/DailyChallenge*.cs`, `DailyStampCalendar.cs`, `DailyLevelStateStore.cs`, `DailyText.cs`, `GameBootstrap.Daily.cs` y `DevProbe.Daily.cs`, y sus tests (`Scripts/LiveOps/Tests/`, `Tests/EditMode/Daily*.cs` y `Tests/PlayMode/Daily*.cs`).
- **Documentación:** este registro, `docs/dev/reto-del-rio.md` (nota de diseño), `docs/dev/traspaso/spark-estado.md`, los capítulos de «Qué cambia en el GDD», `docs/dev/prototipo-v0.md` (sondas), `docs/dev/traspaso/README.md` y `CLAUDE.md` (E12).

---

## D-032 · Las 6 decisiones de 16:9 y del fin de contenido que dejó DC3

- **Fecha:** 2026-10-20. Registrada el 2026-10-20 (FX16b, tanda 4, ola 8).
- **Estado:** decidida por el dueño en el chat, literal: «Acepto tus recomendaciones para las 6 decisiones: compactar Aguas estancadas y la victoria en 16:9, aceptar 3, 4 y 5, y el botón «Jugar · Reto del río» (o «Jugar» sin Reto) tras el fin de contenido.»
  - Son las 6 preguntas que dejó abiertas el cierre de la ola 7 (`815b2e5`; DC3, el lienzo de 360 dp de ancho y alto mínimo de 640 de D-028, pregunta 19), cada una con la recomendación del plan de la ola 8.
  - Implementada en la ola 8: la 1 en FX16a (`StuckModal`) y la 2 y la 6 en FX16b; la 3, la 4 y la 5 no piden código. Pendiente: la prueba en el teléfono.
- **Sustituye:** los «pendiente del dueño (DC3)» de los tests `MinTextSizeTests.Stuck_130_16x9_InsideTheSafeArea_KnownIssue` y `VictoryArrivalTests.Victory_130_16x9_TheRibbonUnderTheHeader_KnownIssue` (explícitos y en rojo hasta la ola 8) y la tolerancia de `FactLineTests.AssertRibbonUnderTheHeader` para los casos que midió DC3; el «Jugar · Nivel 701» del inicio tras ganar el 700.
- **No sustituye:** D-028, pregunta 19 (12 sp físicos en 16:9 y la prueba de layout a 360 × 640); 02 §12 (ningún texto bajo 12 sp; objetivos de 48 dp); 07 §7.8 fuera de la victoria compacta (los CTA crecen en alto con la fuente hasta el 130 %); Q7-A de D-017 («Siguiente» en el mismo sitio en todas las victorias; en el escalón 3 con «Ver mapa», solo a lo alto: ver la nota [P] de abajo); DC2 (el hito de TiTino conserva su dato si TiTino no cabe); D-008, punto 6 (un solo «Jugar», sin pantalla nueva); 06 §9 («El río sigue creciendo»); D-031 (el Reto, desde `live_unlock_level_daily`).

**Contexto.** Con el lienzo de DC3 (360 × 640 dp en 16:9), el cierre de la ola 7 midió lo que ya no cabía: el panel de Aguas estancadas de la rama B tapaba 40,7 dp de las cuias del 31 al 100 % y medía 597 dp al 130 %, más que los 576 dp entre las zonas seguras; la victoria con la ranura del Dato del río subía la cinta por encima de la cabecera del HUD al 130 % (30,6 dp frente a 90 en el 127 en es-419; −3,4 en el 39; −23,4 en el 20 con SingleKey en en-XA) y, en en-XA con la Cosecha doble y el chip del 40, ya al 100 % (73 dp en el 39); TiTino no cabía en la victoria con ranura; la tarjeta del miquito y los avisos de TiTino no cabían en algunos tableros de 2 filas; el cambio de escala de DC3 alcanza también a 18:9, 19:9 y las tabletas; y el inicio, tras ganar el último nivel publicado, ofrecía «Jugar · Nivel 701», un nivel que no existe.

**Decisión.**

| n | Pregunta | Respuesta | Qué se registra |
|---|---|---|---|
| 1 | Aguas estancadas en 16:9 tapa el tablero | Compactar | El panel se compacta cuando el lienzo no deja sitio, para que en 16:9, al 100 % y al 130 %, el tablero quede entero a la vista y el panel dentro de la zona segura, sin bajar textos de 12 sp ni objetivos de 48 dp; a 360 × 780 nada cambia (02 §7; FX16a) |
| 2 | La victoria con la ranura del Dato del río en 16:9 (la cinta sube sobre la cabecera; al 130 % se sale) | Compactar | **Victoria compacta** (02 §6; FX16b): la cinta, la ranura y «Siguiente» quedan bajo la cabecera del HUD al 100 % y al 130 %, compactando solo cuando no hay sitio, por escalones [P]: (1) márgenes justos (marco de abajo de 8 dp, sin aire bajo la Cosecha, huecos de la ranura de 2 dp) y el título de la cinta en una línea hasta 16 sp antes de pasar a dos; (2) el texto de la ranura (el dato de la llegada y la línea de fruta) desde 12 sp, no 14, y la ranura casi a todo el ancho del panel (8 dp a cada lado; la línea de fruta, sin placa propia, conserva 8 dp de margen interior, así que su texto sigue a 16 dp del canto, con 8 de aire dentro del marco); (3) «Inicio» a la izquierda de «Siguiente», en su fila, «Siguiente» con el alto que pide su etiqueta (14-22 sp × la escala) y el chip del nivel siguiente a 2 dp sobre él; en la victoria con llegada, «Ver mapa» (D-013; 06 §4.5) va en la misma fila, entre «Inicio» y «Siguiente», si «Siguiente» conserva sitio para su etiqueta a 14 sp × la escala (la fila no cambia de alto). Si ni así cabe, hoy no sale: es una **pregunta abierta al dueño** (abajo), no una regla. El escalón se elige con la victoria más alta posible de esa pantalla, idioma y escala (la cinta con el título más alto, la Cosecha doble, el chip y la ranura), así que no depende de lo que lleve cada victoria y «Siguiente» queda a la misma altura en todas (Q7-A); en el escalón 3, en la victoria con «Ver mapa», «Siguiente» va más estrecho y desplazado a la derecha, con el mismo canto derecho [P] (abajo). Ningún texto baja de 12 sp ni ningún objetivo de 48 dp; 360 × 780 no cambia. Con la política del Dato del río por defecto, es-419 al 100 % en 16:9 no compacta; con SingleKey, que no es la de por defecto, llega al escalón 2 |
| 3 | TiTino no cabe en la victoria con ranura en 16:9 | Se acepta | Límite conocido: el hito de TiTino conserva su dato (la regla de DC2; 02 §6, fila TiTino) |
| 4 | La tarjeta del miquito y los avisos de TiTino no caben en tableros de 2 filas en 16:9 | Se acepta | Límite conocido: si no cabe, no sale (02 §5) |
| 5 | El cambio de escala de DC3 afecta a 18:9, 19:9 y las tabletas | Se acepta | Misma regla en todas las proporciones: 360 dp de ancho y alto mínimo de 640 dp (02 §5) |
| 6 | El botón del inicio tras acabar el contenido («Jugar · Nivel 701») | «Jugar · Reto del río» o «Jugar» | Con el Reto del río desbloqueado, «Jugar · Reto del río» (clave nueva `play_daily` [P], `borrador`) y, al tocarlo, el Reto, como su tarjeta (el tablero de hoy o, con el día sellado o pendiente, el calendario); sin él, «Jugar» a secas (`play`, ya aprobada) y, al tocarlo, «El río sigue creciendo», como ahora (06 §9; FX16b) |

- **Sin regla nueva inventada:** las cifras de los escalones de la 2 son [P] de FX16b (las midió con el catálogo real: el 127, el 34, el 39, el 20 con SingleKey, el 20 y el 80, en es-419 y en-XA, al 100 % y al 130 %). La victoria compacta conserva «Ver mapa» en la victoria del `10k` (D-013; 06 §4.5): en es-419 al 100 % y al 130 % sale siempre (al 130 % en 16:9, en la fila del escalón 3).
- **«Siguiente» a lo ancho en el escalón 3 [P] (revisión de FX16b, 2026-10-20).** Q7-A (D-017) fija que «Siguiente» sube una sola vez y queda en el mismo sitio en todos los niveles; la victoria compacta lo cumple a lo alto (la fila no cambia de alto con «Ver mapa») y con el mismo canto derecho, pero no a lo ancho: en el escalón 3, en la victoria con llegada y «Ver mapa» (la del `10k`; por ejemplo el 20 en es-419 al 130 % en 16:9), «Siguiente» empieza más a la derecha, para dejar sitio a «Ver mapa» y su hueco de 8 dp, y es así de más estrecho que en las demás (el 39, el 127…); su centro y el chip del nivel siguiente se desplazan con él. Es una propuesta [P] que se aparta de Q7-A a lo ancho, para que el dueño la confirme al probarla en el teléfono. La alternativa medible sería reservar el hueco de «Ver mapa» en todas las victorias del escalón 3 de esa pantalla, idioma y escala («Inicio» más ancho o un hueco vacío), que estrecharía «Siguiente» en todas.
- **Pregunta abierta al dueño (FX16b, 2026-10-20): «Ver mapa» en la fila del escalón 3 cuando no cabe.** Medido a 360 × 640 dp: solo en en-XA (el idioma falso, un 35 % más largo que el español) al 130 %, en las llegadas del 20 y el 80, «Inicio», «Ver mapa» y «Siguiente» no caben en la fila con «Siguiente» a 14 sp × la escala, y una fila aparte de 48 dp (con 8 de hueco) no cabe: en ese caso la victoria más alta posible mide 476,48 dp de un tope de 480, y con la fila serían 532. Mientras no se decida, en ese caso «Ver mapa» no sale (el Mapa sigue en el inicio y en la pausa); en es-419, en y pt-BR las tres etiquetas son de la longitud del español y caben. Opciones: (a) aceptar que no salga solo cuando no cabe (**recomendada**: no afecta a ningún idioma real de hoy, y la regla es la de la 4, «si no cabe, no sale»); (b) dejar que «Siguiente» baje hasta 12 sp en esa fila (sin medir); (c) otra entrada al Mapa en esa victoria. Si se elige la (a), se anota también en 06 §4.5 (las «Entradas» y la fila del Mapa del río) y en 02 §6, fila «Llegada».
- **Límite medido** (no es una decisión): con la política SingleKey, que no es la de por defecto, en en-XA al 130 % la victoria más alta posible (una llegada del `10k` con el chip del nivel siguiente) sigue sin caber por unos 7,5 dp (487,48 de un tope de 480); las victorias que hoy da el catálogo caben.

**Consecuencias.**
- **Código** (ola 8; sin paquetes, SDK, permisos ni versiones nuevas): FX16a (`StuckModal`) y FX16b (`GameScreen.cs`, `GameScreen.Victory.cs`, `GameScreen.Labels.cs`, `GameScreen.Arrival.cs`, `VictoryLineSlot`, `FactLine`, `ArrivalStrip`, `LobbyScreen`, `GameBootstrap` y `UiText`), con sus tests.
- **Textos:** una clave nueva, `play_daily` [P] (es-419 en `borrador`; en y pt-BR de `play` y `daily_challenge`, glosario §10).
- **Sin cambios** en Remote Config, la analítica, el formato de nivel, los packs, el perfil ni la economía.

**Qué cambia en el GDD** (FX16b, 2026-10-20): 02 §5 (la 4 y la 5), §6 (la 2 y la 3: fila «Victoria compacta») y §7 (la 1); 06 §9 (la 6). Fuera del GDD: este registro (la tabla) y `docs/dev/traspaso/README.md` (§6, paso 4, y §7).

**Alternativas descartadas.** Para la 1 y la 2, bajar la letra por debajo de 12 sp o dejar que el panel tape la cabecera o el tablero (los pendientes de DC3); para la 3 y la 4, buscarles otro sitio; para la 5, una regla propia para 18:9, 19:9 y tabletas; para la 6, dejar «Jugar · Nivel 701» o quitar el botón.

**Cómo revertir.** Con una decisión nueva que sustituya a D-032, punto a punto. La 2 se apaga sin datos que migrar (el escalón 0 es la victoria de siempre); la 6 deja una clave de texto sin uso.

**Archivos afectados.**
- **Código y datos** (FX16b): `Assets/_Project/Scripts/UI/{GameScreen.cs, GameScreen.Victory.cs, GameScreen.Labels.cs, GameScreen.Arrival.cs, VictoryLineSlot.cs, FactLine.cs, ArrivalStrip.cs, LobbyScreen.cs, GameBootstrap.cs, GameBootstrap.Daily.cs, UiText.cs}` y `Assets/_Project/Data/Loc/strings.csv`; tests en `Assets/_Project/Tests/PlayMode/` (`Fx16bTests`, `Fx16bSnapshotTests`, `VictoryArrivalTests`, `FactLineTests`, `ContentEndMapTests`) y el golden `Tests/EditMode/Golden/uitext_es419.txt`. FX16a, en su PR.
- **Documentación:** este registro, 02, 06 y `docs/dev/traspaso/README.md`.

---

### D-032, añadido del 2026-10-20: límites de 16:9 al integrar FX16a y FX16b

- **Estado:** aplicadas con la recomendada por la regla del dueño del 2026-10-19 («si tienen recomendadas vamos con esas»), **provisionales hasta la prueba en el teléfono**: si allí molestan, el dueño elige otra opción.
- **Aguas estancadas en 16:9 (FX16a):** se acepta como límite conocido («si no cabe, no sale», como los puntos 3 y 4 de esta decisión) que el panel compacto tape parte del tablero en el 31 rama B al 130 % (46,9 dp de 131,7 en es-419) y la tercera fila en el 640. Lo registra el explícito `StuckCompactTests.Stuck_16x9_NoCuiaCovered_KnownIssue`. Alternativas si molesta en el teléfono: (c) un modal más bajo con Puñado y Balsa plegados tras «Más opciones» (la siguiente recomendada); (b) reducir el tablero mientras el modal esté abierto, con tope de 28 dp por cuia; (d) dejar que el tablero suba sobre la cabecera del HUD.
- **«Ver mapa» en el escalón 3 de la victoria compacta (FX16b):** no sale cuando no cabe en la fila (solo pasa en en-XA al 130 %, en las llegadas del 20 y el 80); el Mapa sigue en el inicio y en la pausa. Se anota en 06 §4.5 y 02 §6.
- **«Siguiente» en el escalón 3 (FX16b):** con «Ver mapa» va más estrecho, con la misma altura y el mismo canto derecho [P] (se aparta de Q7-A de D-017 solo a lo ancho).

## D-033 · Tanda 5: respuestas del 2026-10-20 a las preguntas del plan (Q-N, Q-M, Q-E y Q-T)

- **Fecha:** 2026-10-20. Registrada el 2026-10-20 por el integrador (escritorio).
- **Estado:** decidida por el dueño en el chat el 2026-10-20, con el formulario de las 29 preguntas abiertas de `docs/dev/plan-tanda5.md` §5 (la recomendada venía marcada en cada una): «Q-N1: a · Q-N2: a · Q-N4: a · Q-N5: a · Q-N6: a · Q-N8: a · Q-M1: a · Q-M2: a · Q-M3: a · Q-M4: a · Q-M5: a · Q-M6: a · Q-M7: a · Q-M8: a · Q-M9: a · Q-M10: a · Q-E1: a · Q-E2: a · Q-E3: a · Q-E4: a · Q-E5: a · Q-E6: a · Q-T1: a · Q-T2: a · Q-T3: a · Q-T4: a · Q-T5: a · Q-T6: a · Q-T7: a». Q-N3, Q-N7 y Q-T8 están retiradas (el canon ya las decide; plan §5).
- **Aprobaciones explícitas de la regla 4 de `CLAUDE.md`** (el formulario las marcaba como tales): el paquete `com.unity.mobile.notifications` y el permiso Android `POST_NOTIFICATIONS` (Q-M5); que los agentes creen `.github/workflows/ci.yml` (s30, sin secretos, lo hace build-release y el dueño aprueba el merge; Q-T5); coverlet solo para `dotnet test` (Q-T7). La versión exacta de cada paquete la propone quien lo integre y se anota aquí antes del merge. El permiso `INTERNET` sigue pendiente de su propia decisión: esto no lo aprueba.

**Decisión.**

| Id | Decisión |
|---|---|
| Q-N1 | `anim_speed_factor_late` = 1,0 (D-006); se corrigen 08 §9 y 02 §4 |
| Q-N2 | V3: desde el 31 el generador produce siempre una variante `minus_color` para el alivio con Cuia extra |
| Q-N4 | Un solo ajuste «Alto contraste», con el patrón al 35 % (07 §3); se corrige 02 §12 |
| Q-N5 | El doble toque no tiene función (C3) |
| Q-N6 | El elenco de unas 50 especies va a la 1.1; la 1.0 sale con los 11 miquitos |
| Q-N8 | Curación de 1-200: los agentes preparan informes y el dueño decide en `curation_*.json` |
| Q-M1 | Bitácora «1 de 3» (D-029 #17 B): se diseña ya; el tech-lead propone 3 premios de igual valor y el dueño lo aprueba antes de B4 |
| Q-M2 | Panel único de premios de la victoria (D-029 #18 B): se diseña ya; propuesta del tech-lead aprobada por el dueño antes de B1b y B4 |
| Q-M3 | `calendar` = `{cycle, next_day, last_claim_date}` [P], anotado en 08 §4.3 |
| Q-M4 | Sesión = arranque en frío o vuelta tras ≥ 30 min en segundo plano |
| Q-M5 | Notificaciones push nativas con `com.unity.mobile.notifications` y `POST_NOTIFICATIONS` (aprobado) |
| Q-M6 | Marco de país del día 30: se guarda como derecho y se enseña en la 1.1 |
| Q-M7 | Frutas en 321-700: en la 1.0 solo el dato de llegada; series nuevas en la 1.1 |
| Q-M8 | La skin ganada se pone sola, con aviso, y se cambia en el Mercado |
| Q-M9 | `streak` = `{wins, river_full}`; los hitos pagados hoy y las semillas del día, en `daily` con nombres [P]; un solo `shield_available` (en `daily`); la Canasta sin bloque, deducida de `max_level`; se anota en 08 §4.3 |
| Q-M10 | La Crecida vive en AF.Economy, junto a la Canasta |
| Q-E1 | Ningún comodín antes del nivel (gana 04 §1); se corrige GDD.md §6.1 #4 |
| Q-E2 | El icono de oferta del inicio llega con la primera oferta temporal (1.1/1.2); B5 deja el hueco |
| Q-E3 | `geo_tier`: región del dispositivo (`IDeviceLocale`) y, cuando llegue el IAP, el país de la tienda |
| Q-E4 | Tarjeta de Canoas del 31: «Canoas: si te rindes o sales del nivel, pierdes una. Vuelven solas cada 25 minutos.» [P] |
| Q-E5 | «Restaurar compras» oculto hasta el adaptador real de IAP |
| Q-E6 | El Mercado abre ya con semillas, Mochila, Sin anuncios, Cántaro y Canoas 1 h (sin arte de skins) |
| Q-T1 | Bus mínimo `AF.Events` sin motor: publican GameFlow y Economy, consume Analytics |
| Q-T2 | Unity Localization 1.5.13 espera a la decisión de Addressables y PAD |
| Q-T3 | País del perfil legal sin servidor: SIM, luego región del teléfono; si no, perfil `default` con el tratamiento más estricto |
| Q-T4 | Compras de 13-17: doble confirmación en el juego + autenticación de Google Play (a confirmar con el abogado) |
| Q-T5 | Los agentes crean `ci.yml` (s30, sin secretos) con build-release; el dueño aprueba el merge |
| Q-T6 | Managed Stripping High ya, con `link.xml` y un build de prueba |
| Q-T7 | Cobertura ≥ 80 % medida con coverlet solo en `dotnet test` |

**Alternativas descartadas.** Las opciones (b) y (c) de cada fila de `docs/dev/plan-tanda5.md` §5.

**Cómo revertir.** Con una decisión nueva que sustituya la fila. Ninguna tiene coste para el jugador mientras no se publique la 1.0; Q-M5, Q-T5 y Q-T7 se revierten quitando el paquete, el permiso o el workflow.

**Qué cambia en el GDD** (lo lleva cada bloque al implementarlo): 08 §9 y 02 §4 (Q-N1), 02 §12 (Q-N4), 08 §4.3 (Q-M3, Q-M9), GDD.md §6.1 #4 (Q-E1), 06 §4.2 y §4.7 con los diseños aprobados (Q-M1, Q-M2), 08 §6 y §13 (Q-M5, Q-T5, Q-T6, Q-T7).

### D-033, añadido del 2026-10-20: diseño de la Bitácora «1 de 3» y del panel único de la victoria (Q-M1 y Q-M2)

- **Estado:** decidido por el dueño en el chat el 2026-10-20: «Bitácora y panel de la victoria: acepto las 11 recomendadas (1 a, 2 a, 3 sí, 4 sí, 5 sí, 6 sí, 7 sí, 8 a, 9 sí, 10 sí, 11 sí).» La propuesta la preparó el tech-lead sobre 02 §6, 05 §2.2-§2.3, 06 §2.3 y §4.2-§4.7 y 08 §4.3, §8.2 y §9. Desbloquea B4 y B1b.

**Bitácora «1 de 3».**
1. **Tasa de valor:** la de la Bitácora, `eco_calendar_locked_booster_seeds` (Marea = 30; cualquier otro comodín = 100; 06 §4.2, regla 1).
2. **Retoque de la tabla** (cambia cifras [sim]; se regenera `tools/out/econ_B.md`): las casillas 1, 3, 8, 9 y 15 pasan de 50 a 60 semillas; las 2, 10, 17, 19 y 27, de «3 Marea» a «3 Marea + 30»; las 20, 26 y 28 bajan de 150 a 100, y el día 30, de 650 a 600. Los totales del ciclo no cambian (3.000 semillas, 15 Marea, 3 Morpho, 3 Cuia extra, 1 Luciérnaga, 2 Canastas y 1 Canasta grande); el peso de los hitos baja del 53 % [E] al 51,7 %. Opciones de cada casilla, en orden fijo semillas / comodín / mezcla:

| Casillas | Valor | Semillas | Comodín | Mezcla |
|---|---|---|---|---|
| 1, 3, 8, 9, 15 y 2, 10, 17, 19, 27 | 60 | 60 | 2 Marea | 1 Marea + 30 |
| 5, 11, 13, 16, 18, 20, 22, 24, 26, 28 y 4, 12, 25 | 100 | 100 | 1 Morpho | 2 Marea + 40 |
| 6, 23 | 100 | 100 | 1 Cuia extra | 2 Marea + 40 |
| 29 | 100 | 100 | 1 Luciérnaga | 1 Morpho |
| 7 | 250 + Canasta | 250 | 2 Morpho + 50 | 5 Marea + 100 |
| 14 | 400 | 400 | 300 + 1 Cuia extra | 2 Morpho + 1 Cuia extra + 100 |
| 21 | 400 + Canasta | 400 | 2 Cuia extra + 2 Morpho | 1 Cuia extra + 1 Morpho + 200 |
| 30 | 600 + Canasta grande + marco | 600 | 2 Cuia extra + 1 Morpho + 300 | 3 Morpho + 300 |

   En los hitos 7, 14, 21 y 30 la Canasta, la Canasta grande y el marco van fijos en las tres opciones.
3. **Comodín bloqueado:** se sustituye por el siguiente desbloqueado del mismo valor (Morpho → Cuia extra → «2 Marea + 40»); nunca dos opciones iguales.
4. **Elección:** la casilla de hoy pulsa; al tocarla salen 3 tarjetas; un toque sella, sin confirmación [P]. Cerrar sin elegir la deja pendiente; nunca se elige sola.
5. **Cuándo se cobra:** al terminar el primer nivel del día (campaña o Reto, se gane o se pierda). Ese día la victoria lleva la fila «Bitácora · elige tu premio», que abre la libreta encima y vuelve. Si se ignora, la libreta sale sola al volver al Inicio, como el único takeover de la sesión (`ui_max_takeovers_per_session` = 1).
6. **Perfil:** solo `calendar` = `{cycle, next_day, last_claim_date}` [P] (Q-M3); la elección no se guarda y las casillas pasadas enseñan solo el sello. Cobro y avance de `next_day` en la misma transacción (`Wallet.Apply`) y la misma escritura.
7. **Marco de país:** derecho en `entitlements.frames` [P], junto a `entitlements.skins` (08 §4.3); el del mundo en que está el jugador; se enseña en la 1.1 (Q-M6).
- **Remote Config:** clave nueva `eco_calendar_table_v2` [P] (30 casillas × 3 opciones y el canon); la v1 queda de respaldo. **Analítica:** `af_calendar_claim` suma `choice` (`seeds`/`booster`/`mix`) y `cycle` [P]; `reward_value` es el valor equivalente.
- **econ_sim:** `CALENDAR` con la tabla retocada y `--calendar-pick canon|seeds|booster|mix` (por defecto `canon`); de paso, modelar las Canastas de los días 7, 21 y 30, que hoy faltan. Peor caso «siempre semillas»: +850 semillas por ciclo (estimación, no [sim]).

**Panel único de la victoria.**
8. **La Canasta se abre sola dentro del panel**, sin toque (cambia «se abre con un toque» de 06 §2.3): su contenido vuela al saldo y a la bandeja.
9. **Un toque en Siguiente** con animaciones pendientes las completa, enseña el estado final 250 ms [P] y sigue (D-011); sin auto-avance (`victory_autonext_s` = 0).
10. **Orden del panel:** título → Cosecha +N → hasta 3 barras (Crecida desde el 25; Canasta · {sitio} k/10; tercera barra rotativa con prioridad skin ganada > Bitácora lista > Reto) → Cosecha doble → Siguiente e Inicio → Dato del río. En 16:9 compacto (FX16b): escalón ≥ 2, Crecida y Canasta a media anchura [P]; escalón 3, 2 barras y chevron [P]. Tiempos: celebración 900 ms → Cosecha 0-400 ms → Crecida 400-900 ms → Canasta 900-1.500 ms; Siguiente a 1,40 s.
11. **Secuencia del nivel 10:** Victoria (con la Canasta) → libreta → Mapa → Inicio (como mucho 4 pantallas, ≤ 30 s). Siguen siendo pantallas aparte: el Mapa del 10, del 1.000 y del fin de contenido; «El río sigue creciendo»; banner de mundo, tarjeta previa y Fruta nueva; Sin canoas, AD_GATE y el pre-prompt de push; la libreta. El panel no es un takeover: `TakeoverQueue` aplica el orden de 06 §4.7 solo al entrar al Inicio.

**Qué cambia en el GDD** (lo lleva B4/B1b): 06 §4.2, §2.3, §4.4 y §4.7; 05 §2.3 y §3 (nueva corrida [sim]); 02 §6; 08 §4.3 (`calendar`, `entitlements.frames`), §8.2 (`af_calendar_claim`) y §9 (`eco_calendar_table_v2`).

### D-033, añadido del 2026-10-20: preguntas de B1a (con la regla «si tienen recomendada, vamos con esa»)

- **Estado:** aplicadas por el integrador con la regla del dueño del 2026-10-19 («si tienen recomendadas vamos con esas»); el dueño puede cambiarlas.
- **B1a-Q1:** la fusión en la nube (B8) de los campos nuevos de `daily`: `shield_available` por AND, `streak_hits` por unión y `streak_seeds` por máximo.
- **B1a-Q2:** un cambio que solo toca `streak` actualiza `economy_ts`.
- **Q-C2:** el Reto del río no lleva la Cosecha doble gratis del río crecido (se apoya en 06 §4.3, frente a una lectura literal de 04 §2.8); se anota en 04 §2.8.
- Las Q-C1-Q-C14 [P] de `docs/dev/crecida-canasta.md` §7 quedan con su (a), como las implementó B1a.

## D-034 · Publicación con Codex: la ficha de Play Console espera al dictamen de marca

- **Fecha:** 2026-10-20. Registrada el 2026-10-20 por el integrador.
- **Estado:** decidida por el dueño en el chat: «Ficha de Play Console: esperamos al dictamen de marca, como dice 08 §13.1 #1. Por ahora solo la cuenta de desarrollador y los borradores.»

**Decisión.**
- Un agente Codex (otra herramienta, en la rama `codex/publicacion`) prepara la publicación según `docs/dev/traspaso/codex-publicacion.md`: guía de Play Console, ficha en 3 idiomas, especificaciones de assets, política de privacidad, términos, página de eliminación de cuenta, `app-ads.txt`, web mínima, Data Safety, IARC y público, productos IAP, licencias y la lista de regresión de 60 casos. Sustituye al bloque B13 de la tanda 5, que sale del encargo del Spark. El integrador revisa y fusiona su rama.
- **Ahora:** solo el registro de la cuenta de desarrollador de Google Play (lo hace el dueño) y los borradores.
- **La app no se crea en Play Console hasta el dictamen favorable del abogado de marcas** (08 §13.1 #1 tal cual, sin cambios en el GDD), y ningún AAB se sube a Play (regla dura 6).

**Cómo revertir.** Con una decisión nueva del dueño que cambie 08 §13.1 #1.

### D-033, añadido del 2026-10-20: merge de B10 y versión de coverlet

- **Merge del CI aprobado por el dueño** (Q-T5): «Apruebo el merge de B10 con el ci.yml en integración y main. Yo creo los secretos UNITY_EMAIL y UNITY_PASSWORD en GitHub.» La variable de repositorio `AF_RELEASE_APPLICATION_ID` se crea solo tras el dictamen de marca (D-034); sin ella, el job de build construye un ensayo con la identidad de prueba.
- **coverlet** (Q-T7): `coverlet.msbuild` **10.0.1**, solo en `dotnet/AF.Core.Tests` y activo con `-p:CollectCoverage=true`; cobertura medida el 2026-10-20: 92,09 % de líneas (AF.Core 93,34 %, AF.Solver 90,34 %).
- **Stripping High** (Q-T6): AAB de ensayo de 46,13 MB con `Assets/Plugins/link.xml`. Pendientes de B10: el margen inferior de «Cancelar» en `StoreSheet` (parche en `docs/dev/traspaso/b10-storesheet-safe-area.patch`) y el retroceso predictivo, desactivado hasta probar Atrás en el teléfono con Android 16 (`docs/dev/traspaso/b10-safe-area-y-retroceso.md`).

### D-033, añadido del 2026-10-20: preguntas de B14 (con la regla «si tienen recomendada, vamos con esa»)

- **Estado:** aplicadas por el integrador con la regla del dueño del 2026-10-19; la (a) de cada una es lo que implementó B14 (`tanda5/b14`). El dueño puede cambiarlas.
- **B14-Q1:** los ids de skin son el SKU del pack (`af_skin_m02`…), el mismo que anota la compra; se parchea el ejemplo de 08 §4.3.
- **B14-Q2:** el perfil guarda `entitlements.skin_equipped` y `entitlements.frames` [P]; se anota en 08 §4.3.
- **B14-Q3:** en el Mapa se ven los puntos de paso ya recorridos y al tocarlos dan su nombre y su dato. Falta la fila `map.stop.obidos.dato_01` en `facts.csv` (para `tools/river-map` y el asesor).
- **B14-Q4:** la tarjeta de una parada futura enseña el contenido de su Canasta, semillas incluidas, sin precio (06 §4.5).
- **B14-Q5:** luz de dificultad [P] (`NightLut` 0,66/0,72/0,98 en Muy difícil; `AfternoonLut` 1/0,92/0,78 en Difícil) solo en el panorama, el cielo, el héroe y el agua (ΔE ≤ 4,5), sustituyendo al velo del 15 %; el inicio no la lleva. Provisional hasta la prueba en el teléfono; se corrigen 02 §10 y 07 §7.7.
- **B14-Q6:** al arrancar, las skins de los mundos ya cerrados se dan sin ponerse, sin aviso y sin `af_skin_unlock` (perfiles anteriores).
- **B14-Q7:** el álbum de frutas solo lo llena la campaña.
- **Nota para B3 (Salto del Boto):** debe llamar a `SkinService.ApplyLevelPassed`, y la marca de Canasta cobrada se deduce de `max_level ≥ 10k`, así que un `10k` saltado saldría marcado: revisarlo en B3.

### D-033, añadido del 2026-10-20: preguntas de B1b

- **B1b-Q10, decidida por el dueño en el chat:** «B1b-Q10: (c) sin barras de la Crecida y la Canasta en los hitos de TiTino (60, 80 y 140) cuando no cabe su frase; TiTino habla como dice D-016. La Canasta del 60 se abre y su contenido va al saldo sin barra.» D-016 (T6-A) no cambia.
- **B1b-Q1-Q9 y Q11**, aplicadas con su recomendada (a) por la regla del dueño del 2026-10-19 (detalle en `docs/dev/crecida-canasta.md` §9): barras a media anchura antes de compactar lo demás, también en los escalones 0 y 1; se acepta que las barras lleven es-419 al 100 % en 16:9 y en-XA al 130 % en 360 × 780 al escalón 1 (matiza D-032); si ni el último escalón cabe, las barras no salen; la línea de la Hoja escudo también en 25-30 (se corrige el pseudocódigo de 04 §6.2); con la barra a 0 y el río crecido, sin línea; el hito 10 deja la barra en 10/10 en la victoria y se vacía en el HUD siguiente; el contenido de la Canasta antes de abrirla se ve en el Mapa (B14) y en la barra solo al abrirse; el aviso de la hoja sale al volver a jugar tras la intro; la tercera barra rotativa queda como hueco hasta B4; la espera de 250 ms solo con un premio pendiente.

### D-033, añadido del 2026-10-20: preguntas de B13, B6, B3 y B2 del Spark (confirmadas con la recomendada)

- **Confirmación del dueño (2026-10-20, en el chat):** «las preguntas dejemos las recomendadas». Quedan decididas con su recomendada todas las de este añadido: B2-P4, P5, P6, P10, P13, P16 (se corrige 03 §9) y P18; B3-Q-S1 y Q-S9 (mínimo 31 siempre: el rango de 08 §9 pasa a 31-40), la Canasta de un `10k` saltado con aviso en el nivel siguiente; B6-Q1 a Q18 (Q9: Regreso > puntuales > diarios); y Q-B13-1 a Q-B13-23 (Q-B13-21: código largo y aleatorio distinto del UID, con confirmación y límite de peticiones; Q-B13-5: placa de madera clara o de vidrio de D-003). El estado de abajo, «pendientes de confirmar», queda como historia.

- **Estado:** **pendientes de confirmar por el dueño.** El Spark avanzó con la recomendada de cada una, marcada [P] en el código y en su nota; el integrador las registra aquí al fusionarlas (rama `integra/spark-t5b`) y **no las da por decididas**. Mientras el dueño no diga otra cosa, se mantiene la recomendada.
- **B13 (borradores de publicación; ahora los revisa y completa Codex, D-034):** las 23 preguntas Q-B13-1 a Q-B13-23, con sus opciones y la recomendada, están en `docs/store-listing/README.md` §3. Las de más peso:
  - Q-B13-1, -2 y -3: dominio de la página de borrado, titular legal en Play y correos; los fijan el dueño y el abogado, sin ninguna forma de «Amazon».
  - Q-B13-5: el texto de las capturas «en papel marfil» (07 §10.2) choca con D-003; recomendada: placa de madera clara o de vidrio del kit de UI.
  - Q-B13-21: el borrado por web solo con el ID visible permite borrar la cuenta de otro; recomendada: ID largo y aleatorio distinto del UID, con confirmación y límite de peticiones.
  - Q-B13-23: «14 frutas, 14 colores» frente a 12 frutas + 2 aguas; recomendada: «14 colores de frutas y ríos».
  - Audio: la música `mus_*` (también `music/menu.wav`, la del vídeo) es CC0 frente al encargo con cesión total de 07 §9.5; `amb_w01_fauna_02` lleva voces humanas; el autor «Tintalú Sound Design» usa la marca. Recomendada: el vídeo y los creativos no usan esa música hasta tener una pista con cesión total.
- **B6 (sesiones, Regalo de regreso y avisos; `docs/dev/sesiones-notificaciones.md`):**
  - **B6-Q9:** orden con 1 aviso al día: Regreso > puntuales (Crecida > canoas > nuevos niveles) > diarios (Reto > Bitácora); un puntual que pierde su día pasa una vez al siguiente [P]. Matiza la prioridad literal de 06 §8.2, con la que Nuevos niveles no saldría nunca.
  - **B6-Q1:** días sin sesión con `stats.last_seen_at` nuevo, en periodos de 24 h. **B6-Q2:** `settings.push` guarda `pre_prompt_at` y `second_prompt_shown`; el segundo pre-prompt sale en la victoria que alcanza el hito 3 después del primero o desde 7 días después. **B6-Q3:** no se avisa antes de «Avísame».
  - **B6-Q4:** «Racha de Retos» en la 1.0, como variante del Reto listo. **B6-Q5:** Regreso en D7, D14, D30 y D60 (textos de D14-D60 propuestos). **B6-Q6:** `source = event` para las semillas del regalo. **B6-Q7:** «Crecida te espera» con `{left}` y plural (06 §8.3 dice siempre «una victoria más»). **B6-Q8:** título «TiTino» y canal «Avisos del río». **B6-Q10:** 3 avisos por semana sin abrir, los diarios en días alternos. **B6-Q11:** un aviso en horas silenciosas pasa a las 09:00. **B6-Q12:** 1 al día siempre después del día 7. **B6-Q13:** Reto listo a las 09:30. **B6-Q14:** sin aviso de Crecida si ya pasaron las 36 h. **B6-Q15:** anti-reloj del regalo con `sec_clock_max_drift_h` (sin red, cambiar el reloj lo repite). **B6-Q16:** la tarjeta del pre-prompt, tras el panel de la victoria y antes de la Mochila. **B6-Q17 y Q18:** valores de `af_push_prompt` (`result`, `origin`) y de `type` de los avisos.
  - **Caso pendiente:** el segundo pre-prompt saldría en un Reto ganado que no mueve una racha que ya estaba en 3 cuando salió el primero; cerrarlo exige guardar la racha del primero (otro campo [P]).
  - **Fusión en la nube (B8):** `last_seen_at` y `last_win_at` por máximo, `pre_prompt_shown` y `second_prompt_shown` por OR, y `granted` del dispositivo [P].
  - **`com.unity.mobile.notifications`:** el parche aparte `B6-notifications-real.diff` propone la **2.4.3** (la que declara el Editor 6000.3.25f1). El paquete y `POST_NOTIFICATIONS` están aprobados (Q-M5), pero **no se aplicó en esta integración**: queda para una tarea propia (manifiesto, `versionDefines` `AF_MOBILE_NOTIFICATIONS`, `AndroidNotificationScheduler`, iconos del arte, y comprobar que no entran `SCHEDULE_EXACT_ALARM`, `USE_EXACT_ALARM` ni `RECEIVE_BOOT_COMPLETED`).
  - Falta UI (fuera de B6): la tarjeta del pre-prompt, la fila «Avisos» de Ajustes y el aviso del Regalo de regreso; sus claves ya están en `strings.csv`.
  - Integrador: `push_streak_waiting` lleva sus borradores EN y PT-BR con estado `traducido` (la tabla no admite `borrador` con texto); siguen siendo borradores para el revisor nativo.
- **B3 (Salto del Boto; `docs/dev/salto-del-boto.md` §3):**
  - **Q-S1:** el hueco se mide con el mayor de `skipped_levels` (sin campo nuevo); `since_last_skip` se escribe a 0 y no se lee. Consecuencia: corregir 02 §2, 04 §6.1-§6.2 y el ejemplo de 08 §4.3, y avisar a B7 (`af_skip_used.since_last_skip` = nivel − último salto) y a B8 (unión de `skipped_levels`).
  - **Q-S2:** N − último ≥ 10 (tras saltar el 40, el 50 ya puede). **Q-S3:** banner como franja con Rosa sobre el tablero mientras el intento no empezó. **Q-S4:** sin nivel siguiente o sin canoas para empezarlo, no se ofrece. **Q-S5:** la animación de Rosa (1,5 s) y su sonido esperan al arte y al audio. **Q-S6:** `stuck_skip` partido en etiqueta + precio + `skip_tagline` (toca la tabla de 04 §6.5). **Q-S7:** se ofrece en Aguas estancadas, la pausa y el banner; no en ¿Seguro?, Nivel fallido ni Sin canoas. **Q-S8:** «¿Saltar el nivel?», «Perderás la Cuia extra de este nivel» y «Saltar nivel», `pendiente_asesor`. **Q-S9:** `skip_min_level`: 05 dice «mínimo 31» y 08 §9 da 20-31; el cliente solo exige ≥ 1 (el revisor sugiere no bajar de `rescue_min_level`).
  - **Nueva del revisor:** ¿se enseña al jugador la Canasta que paga el salto de un `10k`? Hoy solo sale en la sonda. Opciones: (a) sí, con el aviso de la Canasta de la victoria en el nivel siguiente (recomendada del integrador); (b) solo el ✓ del Mapa.
  - **Nota de B14, resuelta al integrar:** el Salto llama a `SkinService.ApplyLevelPassed` en el mismo lote (`LevelFlow.OnSkipSkins`; test `SkipFlowTests.SkippingTheWorldClose_GivesItsSkin_InTheSameWrite`) y el aviso «{skin} · puesta» sale en el nivel siguiente. La marca de Canasta cobrada del Mapa se deduce de `max_level ≥ 10k` y **es correcta** en un `10k` saltado, porque el Salto paga su Canasta (E34 y K9; `SkipFlowTests.SkippingA10k_PaysItsBasket_AndEverythingGoesOutInOneWrite`): no hace falta cambio.
  - **Riesgo:** la rama D suma ~70 dp al panel de Aguas estancadas; el caso de 16:9 al 130 % con las ramas B y D a la vez está sin medir.
- **B2 (Pororoca; `docs/dev/pororoca.md` §5):** P-1 a P-19 con la (a). Las que piden al dueño:
  - **P-4:** «1 por nivel» cuenta en todos los intentos del nivel y al salir y volver (a); (b) por intento; (c) se pierde al salir, como la Cuia extra.
  - **P-5:** Volver a la orilla tras una Pororoca vuelve al tablero del nivel, sin devolver el tope (a); (b) al tablero barajado.
  - **P-6:** la hoja de confirmación solo al pagar con semillas (a); (b) también con el stock.
  - **P-10:** la ola en el hilo principal (a), y medir en el teléfono el peor caso (la ola que falla: hasta 41 A* de 40.000 nodos); (b) en un hilo de trabajo.
  - **P-13:** con la Tinaja con dosis, habilitada y medir en `level-qa` (a); (b) deshabilitada mientras tenga dosis.
  - **P-16:** la ola que falla sigue 04 §2.4: aviso, sin cobro, se puede reintentar (a); (b) 03 §9: se desactiva en el nivel. Hay que corregir uno de los dos capítulos.
  - **P-18:** con 5 ranuras en 360 dp, «Shuffle», «Embaralhar», y «Desfazer» y «Revelar» en pt-BR al 130 %, no caben a 12 sp: (a) decidir una etiqueta corta en en y pt-BR (p. ej. «Mix» y «Misturar») o la bandeja de 5 sin etiquetas; hasta entonces EN y PT-BR de `shuffle` quedan vacíos; (b) bajar de 12 sp (rompe 02 §12).
  - Las demás, con la (a): P-1 (Hoja atada y Flor cerrada, fijas), P-2 (20 intentos de Permutar), P-3 (detalles de la reconstrucción), P-7 (globo con Lluvia o Piraña), P-8 (VFX simplificado), P-9 (tarjeta del 30), P-11 (semilla y `*` en el historial h2), P-12, P-14 (también en el Reto), P-15 («Seguir»), P-17 (Remolino) y P-19 (tests PlayMode sin la Pororoca por defecto).
  - Hallazgos para el GDD: 04 §2.4 (P-2, P-4, P-5, P-7, P-15), 04 §10.1 y 08 §4.3 (marcas `*`, `o` y `p` del historial), 03 §9 frente a 04 §2.4 (P-16) y 08 §8.2 (`af_shuffle_result`, solo como sonda hasta B7).
- **Orden del arranque** (B3, B6 y B14 insertan tras `SetUpStreak();`): `SetUpStreak(); SetUpMeta(); SetUpSkip(); SetUpSessions();`. `ConnectTelemetry();` de B7, cuando llegue, va después de `SetUpSessions();` (el choque con `B7-bootstrap.diff` que avisan B3 y B6).

## D-035 · Backend híbrido: Firebase en el cliente móvil y AWS para la infraestructura propia

- **Fecha:** 2026-10-20. Registrada el 2026-10-20 por el integrador.
- **Estado:** decidida por el dueño en el chat: «Vamos con el híbrido: Firebase para Auth anónimo, Analytics, Crashlytics y Remote Config; AWS para la web, el CDN de contenido, las 5 funciones con DynamoDB y la 1.2. Infraestructura como código en infra/ y yo despliego. Registra D-035, propone CDK o Terraform y prepara el plan.» Sustituye en parte a 08 §2.4, §10, §11, §12.1 y §15.3 y a la dirección 20 en lo que dicen de Cloud Functions y Firestore.

**Contexto.** El GDD ponía todo el servidor en Firebase: Auth anónimo, cinco Cloud Functions (`rewardedSsv`, `verifyPurchase`, `restoreLedger`, `voidedPurchasesJob`, `deleteAccount`), el libro mayor `users/{uid}/purchases`, App Check con Play Integrity, Remote Config, Analytics y Crashlytics, y en la 1.2 Firestore, ligas y `rcProposalJob` con BigQuery. El juego no tiene servidor de juego: el backend solo valida dinero, identidad y borrado. El dueño tiene acceso a AWS y quiere montar ahí la infraestructura propia. Ninguna de esas piezas de servidor estaba escrita todavía (B8 hace la nube «sin SDK»), así que el cambio es barato ahora.

**Decisión.**
| Pieza | Dónde |
|---|---|
| Auth anónimo (UID estable), Analytics, Crashlytics y Remote Config | **Firebase** (Firebase Unity SDK 13.17.0, sin cambios de versión) |
| Web `tintalu.com`: política, términos, borrado de cuenta y `app-ads.txt` | **AWS** (S3 + CloudFront) |
| CDN del contenido remoto (Addressables: packs 701+, arte de mundos) | **AWS** (S3 + CloudFront) |
| Las 5 funciones de la 1.0 y el libro mayor de compras | **AWS** (funciones sin servidor y DynamoDB); autorizan con el token de Firebase Auth (JWT) y verifican Play Integrity en el servidor, en lugar de App Check |
| 1.2: guardado multiplataforma, ligas, eventos y datos para propuestas de Remote Config | **AWS** |
| Partida en la nube de Android (Play Games Saved Games) | Google, sin cambios |
| Infraestructura | **Como código en `infra/`**; los agentes la escriben y la validan sin credenciales; **el dueño despliega** con su sesión. Ningún agente ve claves de AWS ni de Firebase |

**Pendiente de aprobar por el dueño (regla 4 de `CLAUDE.md`):** la herramienta de infraestructura como código (propuesta en `docs/dev/plan-backend-aws.md`), la región, los servicios concretos y su coste, y los paquetes nuevos del cliente si hicieran falta (el cliente de Firebase Functions deja de usarse; las llamadas pasan a HTTPS).

**Qué cambia en el GDD** (cuando se apruebe el plan): 08 §2.2 (`AF.Backend`), §2.4 (`functions/` → `infra/` y el código de las funciones), §7 (lista de SDK: sin el módulo Functions), §10.1-§10.3 (App Check → verificación de Play Integrity en el servidor), §11 (libro mayor y borrado en DynamoDB), §12.1 (despliegue), §13.3 (Data Safety: AWS como encargado del tratamiento), §15.3 y 01 (coste por versión).

**Cómo revertir.** Con una decisión nueva. Mientras no haya nada desplegado, sin coste; después, migrando el libro mayor.

### D-035, añadido del 2026-10-20: respuestas del dueño al plan del backend (Q-AWS-1 a Q-AWS-12)

- **Estado:** decididas por el dueño en el chat, tras el formulario de `docs/dev/plan-backend-aws.md` §10: «sigamos las recomendaciones». Todas con la opción (a):
  - **Q-AWS-1:** AWS CDK v2 en TypeScript (herramienta aprobada).
  - **Q-AWS-2:** `sdk-integrator` pasa de `functions/**` a `infra/**`; `build-release` lleva `.github/workflows/infra.yml`.
  - **Q-AWS-3:** dos cuentas de AWS (dev y prod) en Organizations, con IAM Identity Center.
  - **Q-AWS-4:** integridad con el token de App Check de uso limitado, verificado en AWS contra el JWKS de App Check (sin paquete nuevo). Reinterpreta «Play Integrity en lugar de App Check» de D-035: por debajo sigue siendo Play Integrity.
  - **Q-AWS-5:** `sa-east-1` para datos, API y funciones; `us-east-1` solo para ACM y WAF de CloudFront y para Budgets.
  - **Q-AWS-6:** AWS Budgets de 10 USD al mes en dev y 50 en prod, con avisos al 50, 80 y 100 %.
  - **Q-AWS-7:** la clave JSON de la cuenta de servicio de Google, en Secrets Manager, la carga el dueño; federación sin claves (WIF) como mejora en la 1.1.
  - **Q-AWS-8:** el arte de los mundos sigue en Play Asset Delivery (08 §6.2); CloudFront solo para `levels_remote` y el catálogo. Corrige la fila «CDN del contenido remoto» de D-035.
  - **Q-AWS-9:** dev con los dominios de AWS; `tintalu.com` público solo tras el dictamen de marca.
  - **Q-AWS-10:** límites de peticiones de API Gateway y en DynamoDB en la 1.0; WAF solo si hay abuso.
  - **Q-AWS-11:** `af_account_delete` con `source = web` solo como métrica de CloudWatch.
  - **Q-AWS-12:** contrato nuevo en `docs/GDD.md` §7: `infra/contracts/v1` es la fuente única de la API.
- **Paquetes npm de `infra/` aprobados** (regla 4), con versión exacta fijada por el integrador tras comprobarla (al menos dos semanas de antigüedad) y anotada aquí: `aws-cdk-lib`, `aws-cdk`, `constructs`, `cdk-nag`, `typescript`, `esbuild`, `vitest`, `eslint`, `typescript-eslint`, `@aws-sdk/client-dynamodb`, `@aws-sdk/lib-dynamodb`, `@aws-sdk/client-secrets-manager`, `jose`, `ajv`, `aws-sdk-client-mock`.
- **Límites de los agentes y `CLAUDE.md`** se amplían en consecuencia (F0-3).
- **Versiones exactas fijadas por el integrador** (registro de npm consultado el 2026-10-20; todas con al menos dos semanas, dentro de la versión mayor vigente y con sus dependencias entre pares compatibles): `aws-cdk-lib` 2.272.0, `aws-cdk` 2.1144.0, `constructs` 10.8.1, `cdk-nag` 3.0.2, `typescript` **5.9.3** (no la 7.x: `typescript-eslint` 8.71.1 exige `<6.1.0`), `esbuild` 0.28.2, `vitest` 5.0.3, `eslint` 10.12.0, `typescript-eslint` 8.71.1, `@aws-sdk/client-dynamodb` 3.1146.0, `@aws-sdk/lib-dynamodb` 3.1146.0, `@aws-sdk/client-secrets-manager` 3.1146.0, `jose` 6.2.12, `ajv` 8.20.0, `aws-sdk-client-mock` 4.1.0, y de tipos `@types/node` 24.19.1 (como el runtime `nodejs24.x` de Lambda) y `@types/aws-lambda` 8.10.164. Se fijan sin rangos en `infra/package.json` y con `package-lock.json`. Node local del escritorio: 22.20.0 (vale para CDK y vitest; el runtime de Lambda lo confirma F0-R).

## D-036 · Aprobación del permiso Android INTERNET

- **Fecha:** 2026-10-20.
- **Estado:** aprobado expresamente por el dueño en este chat: «si aprobemos», en respuesta a la propuesta de aprobar únicamente `INTERNET`.

**Decisión.** Se aprueba el permiso Android `android.permission.INTERNET` para los servicios de red previstos del juego: anuncios, compras, Firebase y guardado en la nube. Sustituye el estado pendiente de este permiso en los registros anteriores; D-033 no lo había aprobado.

**Alcance.** Esta aprobación no autoriza por sí sola instalar o aprobar SDK, paquetes o versiones, crear cuentas, desplegar servicios ni publicar la app. Las aprobaciones previas de otros componentes conservan su propio alcance. No se cambia `ForceInternetPermission`, el manifiesto ni la configuración de Unity en esta tarea: según el estado documentado, Unity ya añade el permiso automáticamente.

**Cómo revertir.** Mediante una nueva decisión del dueño y evaluación del efecto sobre los servicios de red antes de modificar el permiso en la app.

### D-035, añadido del 2026-10-21: preguntas de F1-F2a del backend (con la regla «si tienen recomendada, vamos con esa»)

- **Estado:** aplicadas con su recomendada por la regla del dueño («sigamos las recomendaciones», 2026-10-20); son lo que implementa `backend/f1-f2a` (`897a077`, `358c8ea`). El dueño puede cambiarlas.
- **Código de borrado (Q-B13-21):** rota; cada `POST /v1/account/code` emite uno nuevo de 128 bits (Base32 de Crockford) que invalida el anterior; el servidor solo guarda su hash; tras reinstalar, el cliente pide otro [P].
- **Límite por IP en el borrado por web:** no en la 1.0; límite por ruta (2 rps, ráfaga 5) y contador por código; WAF con límite por IP en prod si hay abuso (Q-AWS-10). Riesgo anotado: el límite global por ruta deja que un atacante sature el borrado por web.
- **IP en los logs de acceso de la API:** se conserva 30 días (va en Data Safety); se corrige «logs sin PII» de `docs/dev/plan-backend-aws.md` §5; lo revisa el abogado.
- **Cuenta en prod:** `-c account` opcional y documentada (la síntesis de prod sin cuenta forma parte de la verificación).
- **Concurrencia reservada:** no en la 1.0 (las cuentas nuevas suelen tener límite de 10).
- **HSTS de 2 años con `includeSubDomains`:** se confirma en F5-1, antes de fijar el dominio de prod.
- **Rutas propuestas:** `/v1/purchases/verify`, `/v1/ledger/restore`, `/v1/ssv` (GET y POST hasta verificar el formato de MAX), `/v1/account/code`, `/v1/account/delete`, `/v1/account/delete-request` y `/v1/account/delete-confirm`, en `infra/contracts/v1/index.json`.

### D-035, añadido del 2026-10-21: preguntas de las 5 funciones (F2-3 a F2-8), con la regla «si tienen recomendada, vamos con esa»

- **Estado:** aplicadas con su recomendada por la regla del dueño («sigamos las recomendaciones»); integradas en `ae8c8ea` (rama `backend/f2b`, revisión final APROBADO sobre `d7d367a`). El dueño puede cambiarlas.
- **`af_piggy` y cifras de los packs:** el servidor devuelve semillas 0 más el ítem `piggy` (el cliente acredita lo acumulado) y los packs con los defaults de 08 §9; se aceptan los ids `rescue_cuia` e `infinite_canoes_min` [P].
- **Consumibles:** el servidor solo hace `acknowledge`; el consumo lo hace Unity IAP en el cliente (F3-3). Se corrige 08 §10.3 («y consume si consumible»).
- **Cuenta ofuscada:** en prod se exigirá en cuanto F3-3 llame a `setObfuscatedAccountId = UID`; hasta entonces se acepta con aviso y métrica.
- **Token anotado a otro UID** (cuenta borrada o reinstalación sin Play Games): se mantiene el 409; los no consumibles se recuperan desde Play.
- **Compras de prueba en prod:** se conceden y se marcan `test`.
- **Límites [P]:** 100 recompensas SSV por UID y día, 30 verificaciones por UID y hora, 10 códigos por UID y día, 5 `delete-request` por código y hora y 10 `delete-confirm` por token y 5 minutos.
- **Supuestos [NV]** que se confirman en F0-R o F4-2: firma S2S de MAX = hex(SHA-256(`event_id` + clave)) (usar una firma que cubra todos los parámetros si MAX la ofrece); formato del token de App Check; `products.get` sin precio (el libro mayor no guarda `price_micros` ni `currency`; desviación del plan §2 y de 08 §10.3).
- **Endurecimiento pendiente** (tarea F2c, de la revisión): exigir `-c firebaseAppIds` en el synth de prod; lápida `voided` para anulaciones de tokens aún no verificados; hacer cumplir `af_starter` una vez por cuenta en el servidor; cerrar las carreras residuales del borrado (condición en el Put del libro mayor o segunda pasada de `eraseAccount`). Y actualizar `aws-cdk-lib` cuando una versión corrija el aviso de `brace-expansion` (cambio de versión: lo aprueba el dueño).

### D-035, añadido del 2026-10-21: preguntas de F2c (endurecimiento), con la regla «si tienen recomendada, vamos con esa»

- **Estado:** aplicadas con su recomendada por la regla del dueño; integradas con `backend/f2c` (revisión final APROBADO sobre `923948d`). El dueño puede cambiarlas.
- **`af_starter` reembolsado o con contracargo:** no se puede volver a comprar (la marca por cuenta es permanente).
- **Errores del contrato v1:** la compra anulada y el segundo `af_starter` siguen como 400 `bad_request` con su mensaje, sin códigos nuevos.
- **Filas de compras que escapen del borrado** (índice `by_uid` eventualmente consistente): se acepta como riesgo residual, vigilado con la métrica `EraseSecondPass` y una alarma en `ops-stack` más adelante.
- **Reserva de `af_starter`:** 15 minutos [P].
- **Riesgos residuales aceptados** (detalle en `infra/README.md`, «Riesgos residuales conocidos (F2c)»): más de 15 min entre reservar y escribir; borrado de la cuenta durante una compra (queda reconocida sin conceder).
- **`infra.yml`** sintetiza prod con un id de app de ejemplo, porque prod exige `-c firebaseAppIds` desde F2c.
- **Sugerencia pendiente** (no afecta al dinero): en `shared/integrity.ts`, una `TransactionConflict` al guardar la caché lanza 500 después de gastar el token de App Check de un solo uso; registrarla y seguir sin guardar.

### D-035, añadido del 2026-10-21: preguntas de F3-1 y F3-2 (cliente AF.Backend), con la regla «si tienen recomendada, vamos con esa»

- **Estado:** aplicadas con su recomendada por la regla del dueño; integradas con `backend/f3-1` (revisión APROBADO sobre `a7a1bca`). El dueño puede cambiarlas.
- **`deferred_ack` (08 §10.3):** al vencer el plazo solo se hace `acknowledge` local más `deferred_ack`; **no se acredita** hasta que el servidor verifique la compra en un `restoreLedger` o en un reintento (el plazo se mide con el reloj del dispositivo, que se puede adelantar). Se corrige 08 §10.3 en F3-3.
- **URL de la API de dev (F4-2):** se escribe en `BackendEndpoints.Dev` en un commit (la URL de `execute-api` no es secreta).
- **`biz_backend_enabled` [P]:** se añade a 08 §9 con la integración del GDD y se conecta en F3-3 como interruptor de corte.
- **Riesgo para F3-3:** comprobar en el dispositivo que `SHA256.Create()` sobrevive al Managed Stripping High; si no, `SHA256Managed` o una entrada de `link.xml`.

### D-035, añadido del 2026-10-21: Firebase dev en la cuenta personal del dueño

- **Estado:** decisión del dueño en la sesión con Codex (registro en `docs/dev/traspaso/codex-estado.md`): crear Firebase dev desde su cuenta personal de Google, porque la cuenta empresarial no tiene permiso para crear proyectos (`resourcemanager.projects.create`) hasta que su administrador lo conceda.
- **Identificadores públicos de dev:** proyecto `fill-dev-e75a2` (el id `amazon-fill-dev` no estaba disponible); app Android `com.amazonfill.dev` con id `1:528273757148:android:45d5cf0ddb2e3021c93519`. Auth anónimo activado; App Check con Play Integrity registrado **sin enforcement**; Analytics activado (cuenta dedicada, ubicación EE. UU.); plan Spark sin coste.
- **AWS dev** (`AfDevApi`) ya autoriza con emisor y audiencia `fill-dev-e75a2` y admite ese id de app. Mientras `infra/lib/config.ts` siga con el default `amazon-fill-dev`, **todo despliegue de dev debe pasar** `-c firebaseProjectId=fill-dev-e75a2 -c firebaseAppIds=1:528273757148:android:45d5cf0ddb2e3021c93519`; se cambiará el default de dev en la próxima tarea de `infra/`.
- **Pendiente para prod:** crear Firebase prod en la cuenta empresarial (con el permiso del administrador) o decidir otra cosa; prod sigue sin bootstrap ni stacks.

### D-033, añadido del 2026-10-21: preguntas de B7 (Remote Config, analítica y bus `AF.Events`), con la regla «si tienen recomendada, vamos con esa»

- **Estado:** aplicadas con su recomendada, la (a), por la regla del dueño; integradas con `spark/t5-b7` (`17046f38`; revisión del Spark en `d2889a40`) y sus parches B7-*. El dueño puede cambiarlas. Detalle y opciones en `docs/dev/remote-config-analytics.md` §6.
- **Q-T1:** bus mínimo `AF.Events` sin motor; las interfaces de escucha que ya existían siguen.
- **Q-B7-1:** `AF.RemoteConfig` sin dependencias. Ver Q-B7-24 abajo: con D-035, `AF.Backend` es el cliente de la API en AWS, no Firebase.
- **Q-B7-2:** `AF.Events` sin dependencias; lo referencian Economy, GameFlow, Analytics y UI. Se añade su fila a 08 §2.2 en esta integración.
- **Q-B7-3:** dos ensamblados de composición (`AF.RemoteConfig.Binding` y `AF.Analytics.Adapters`) que solo usa `AF.UI`; se añade su fila a 08 §2.2 en esta integración.
- **Q-B7-4 / Q-B7-5:** «resto» en los mapas por país = la clave `"*"`; `ads_rv_cooldown_s_<placement>` para los seis placements de `ads_rv_cap_<placement>`.
- **Q-B7-6:** los defaults que remiten a otra tabla (`eco_calendar_table_v1`, `biz_kpi_*`, premios de liga y regata, `eco_price_tier_<country>`) quedan `null` + `pending`; cada bloque transcribe la suya a 08 §9.
- **Q-B7-7:** un valor remoto que no pasa `Validate()` devuelve la clase entera a `Defaults`. Ojo: las `Validate()` no comprueban los rangos de 08 §9; completarlas en sus bloques.
- **Q-B7-8:** `biz_maintenance_message` es una clave de `strings.csv` (vacía = aviso genérico).
- **Q-B7-9:** la analítica del Reto sigue como sonda `AF_STATE` (R10); su gancho, en un parche aparte.
- **Q-B7-10 a Q-B7-13:** `af_level_win.double_harvest` = estado en la victoria; `af_level_abandon.reason` solo `map`; los comunes de 08 §8.1 a todos los eventos, sin los null; `boosters_json` con `undo` y `extra_cuia`.
- **Q-B7-14:** `biz_feature_lives` apagado = las canoas no cuentan; `biz_feature_rescue` apagado = sin Cuia de rescate; los demás, al llegar su sistema.
- **Q-B7-15:** `anim_speed_factor_late` = 1,0. El GDD ya lo traía (integración de D-035), así que `B7-gdd.diff` no se aplicó: solo se regeneró el JSON.
- **Q-B7-16 a Q-B7-21:** `AF_EVENT {json}` en logcat solo en desarrollo y en el Editor; `session_id` GUID; booleanos `true`/`false`; `rc_variant` = `biz_experiment_tag`; recogida apagada hasta el consentimiento (los sumideros locales escriben); el JSON compilado como constante.
- **Q-B7-22 / Q-B7-23:** `pours` y `t_ms` se añaden a los eventos de comodines y del modal al engancharlos; `level_start.band` = `difficulty.band` del nivel.
- **[P] de la nota:** el formato `af-rc-defaults/1`, las interpretaciones de las diez celdas no literales de 08 §9, la congelación por `RcSnapshot`, la validación contra el catálogo, el sumidero `AF_EVENT` y la sesión.
- **Ajustes del integrador por D-035:** los defaults se regeneran desde 08 §9 actual (entra `biz_backend_enabled` = true, sale `sec_appcheck_enforce`; siguen 251 claves). `VerificationQueueConfig` (F3-1, `sec_purchase_deferred_ack_h`) queda en `NotBoundYet` del test del binder hasta que F3-3 conecte el backend en el arranque.
- **Q-B7-24 (nueva, del integrador):** 08 §2.2 sigue diciendo que `AF.RemoteConfig` depende de `Backend`, pero con D-035 `AF.Backend` es el cliente de la API propia en AWS y Remote Config va con Firebase en el cliente. Opciones: (a) **recomendada, aplicada en el código:** `AF.RemoteConfig` sin dependencias (o solo `Platform`, para `IServerDate`) y corregir esa celda de 08 §2.2 en la próxima edición del GDD; (b) que `AF.RemoteConfig` referencie `AF.Backend`.
- **Q-B7-25 (nueva, del integrador):** el común `net` (08 §8.1) se leía con `Application.internetReachability`, y eso hace que Unity añada `ACCESS_NETWORK_STATE` al manifiesto, un permiso no aprobado (regla dura 4; `ci/check-manifest.ps1 -Mode dev` falló con el dev.apk). Opciones: (a) **recomendada, aplicada:** no enviar `net` (null, se omite) hasta que el dueño apruebe `ACCESS_NETWORK_STATE`, que traerá igualmente el SDK de Firebase (F3-6); entonces se vuelve a leer y se añade a la lista blanca de `check-manifest.ps1`; (b) aprobarlo ya y enviar `net` desde ahora. **Respondida en D-037 con la (b):** `net` vuelve a enviarse y el permiso está en la lista blanca.
- **`IBackendSwitch` (F3-3) y `biz_backend_enabled`:** no se conecta todavía a Remote Config: `VerifiedStoreService` aún no está en el arranque (tarea con F3-5). Al conectarlo, basta un adaptador en `AF.RemoteConfig.Binding` que lea `biz_backend_enabled` del `RcSnapshot` (o fijar `BackendSwitch.BizBackendEnabled` al enlazar).
- **Pendientes de B7 tras integrar:** enlazar en `RcConfigBinder` `ShuffleConfig`, `SkipConfig`, `ComebackConfig`, `PushConfig` y `VerificationQueueConfig` (hoy usan sus `Defaults`) y `sec_ssv_grace_h` con B8; los ganchos que faltan (`af_boot`, `af_consent`, comodines, modal, canoas, Reto, Ajustes, `af_level_load`, `af_error`…); las pantallas «Actualiza Tintalú» y de mantenimiento.

### D-035, añadido del 2026-10-21: preguntas de F3-3 (compra verificada en el cliente), con la regla «si tienen recomendada, vamos con esa»

- **Estado:** aplicadas con su recomendada por la regla del dueño; integradas con `backend/f3-3` (revisión final APROBADO sobre `fd162926`). El dueño puede cambiarlas.
- **`purchase_queue` [P]:** la cola de compras pendientes es solo local (lleva `purchaseToken` en claro); la fusión de 08 §11 conserva la del dispositivo y no viaja en la nube.
- **Balsa acreditada tarde** (por la cola o al reabrir): la Cuia de rescate se pierde; el resto se acredita [P].
- **Unity IAP 5.4.4 [NV]:** si no permite reconocer un consumible sin consumirlo, se vuelve a decidir el `deferred_ack` al integrar el SDK (F3-6).
- **`biz_backend_enabled`:** ya dado de alta en 08 §9 (`bool`, `true` [P]); en el cliente es `IBackendSwitch` con default `true` hasta que B7 lo lea de Remote Config.
- **Cántaro recuperado sin sus semillas** (cierre entre el cobro y el primer guardado): se acredita con `eco_piggy_min_buy` [P].
- **Conectar `VerifiedStoreService` al juego** (`Resume` al arrancar y al volver a primer plano, `Pump` periódico y la UI de `Pending` y `VerifyFailed`): tarea de unity-gameplay-dev junto con F3-5.
- **Ejemplos `GPA.…` de `applied_purchases`** en 08 §4.3 y 04 §10.1: pasan a `token_hash` en la próxima tarea de documentación.
- **Mochila con la marca local puesta:** se acredita la que el servidor verifica; el servidor hace cumplir «una por cuenta» (F2c).

## D-037 · Aprobación del permiso Android ACCESS_NETWORK_STATE

- **Fecha:** 2026-10-21. Registrada el 2026-10-21 por el integrador.
- **Estado:** aprobado por el dueño en el chat: «Apruebo el permiso ACCESS_NETWORK_STATE para el común net de la analítica (Q-B7-25 b); añádelo a la lista blanca de check-manifest.»

**Decisión.** Se aprueba `android.permission.ACCESS_NETWORK_STATE`, que Unity añade al leer `Application.internetReachability`, para el parámetro común `net` de la analítica (08 §8.1; B7). Se añade a la lista blanca de `ci/check-manifest.ps1` y el común `net` vuelve a enviarse. Responde Q-B7-25 con la opción (b) y sustituye la (a) que se había aplicado al integrar B7 (`0d5965cc`).

**Alcance.** No aprueba otros permisos, SDK, paquetes ni versiones.

**Cómo revertir.** Con una decisión nueva: quitarlo de la lista blanca y dejar de leer la red en `GameBootstrap.Telemetry.cs`.

## D-038 · Con `biz_feature_lives` apagado, desde el 31 todo funciona como antes del 31

- **Fecha:** 2026-10-21. Registrada el 2026-10-21 por el integrador.
- **Estado:** decidida por el dueño en el chat, al cerrar la revisión de B7: «biz_feature_lives=false: desde el 31 todo como antes del 31: salir es gratis y no cuenta como fallo, sin ¿Seguro? ni Nivel fallido por salir y sin canoas; el rescate sigue con su propio interruptor.»

**Decisión.** Con el interruptor de 08 §9 `biz_feature_lives` en `false`, en los niveles 31 y siguientes el embudo de D-008 se comporta como en 1-30: no hay canoas (ni en la barra, ni en el modal, ni en los avisos), salir del nivel es gratis, no es fallo duro, no suma a `fails_level` y no abre ¿Seguro? ni Nivel fallido; «Rendirse» hace lo mismo que en 1-30. El rescate (Cuia y Balsa de rescate) no cambia: lo gobierna su propio interruptor, `biz_feature_rescue`. Con el interruptor en su valor por defecto, nada cambia. Sustituye la regla [P] aplicada en `tanda5/b7-fixes` (`d47ac8fe`), donde salir seguía siendo fallo duro y «Rendirse» iba directo a Nivel fallido.

**Cómo revertir.** Con una decisión nueva.

### D-033, añadido del 2026-10-21: preguntas de B8, B4 y B5 del Spark, con la regla «si tienen recomendada, vamos con esa»

- **Estado:** aplicadas con su recomendada por la regla del dueño; integradas en `integra/spark-t5c` con `spark/t5-b8`, `spark/t5-b4` y `spark/t5-b5` y sus parches. El dueño puede cambiarlas. Detalle y opciones en `docs/dev/nube-integridad.md` (B8), `docs/dev/bitacora.md` §8 (B4) y `docs/dev/mercado.md` §7 (B5).

**B8 (nube sin SDK, integridad, anti-fraude local y «Eliminar cuenta»).**
- **Q-B8-1:** `applied_purchases` por unión (08 §11, canon). La nota recomienda cambiar 08 §11 a «la lista del ganador + `restoreLedger`»: **pendiente del dueño** (no se aplica sin su decisión). Con F3-3 la lista guarda el `token_hash`; se escribe en orden ordinal y sin repetidos.
- **Q-B8-2:** la clave del HMAC sale de `integrity.device_first`.
- **Q-B8-3:** la huella sale de un id aleatorio por instalación (`af_install_id` en `PlayerPrefs`): sin identificador del dispositivo ni fila nueva de Data Safety.
- **Q-B8-4:** `locale` = el idioma de la UI resuelto en cada arranque.
- **Q-B8-5:** un perfil sin firma o migrado es coherente.
- **Q-B8-6 / Q-B8-7:** `daily` con fechas distintas, el bloque del día más reciente; empate total, el local.
- **Q-B8-8 / Q-B8-9:** revocar Sin anuncios quita el derecho; `ApplyLedger` en orden ordinal de token.
- **Q-B8-10:** la sospecha solo se mide (08 §10.1: sin castigo automático).
- **Q-B8-11:** con B6, `stats.last_seen_at` y `last_win_at` por máximo; en `settings.push`, «o» en los pre-prompts, máximo en `pre_prompt_at` y `granted` del dispositivo. Aplicada en `CloudMerge` al integrar.
- **Del integrador:** `purchase_queue` es solo local también en la nube de B8 (no se compara, no sube, no entra en la firma; D-035, añadido de F3-3). La fusión cubre además `entitlements.frames` y `album.fruits` por unión (B14), `calendar` (B4) y `offers` (B5). «Eliminar cuenta» deja preparado `GameBootstrap.ServerAccountDelete` para `IBackendClient.DeleteAccount` (F3-5) sin conectarlo; Ajustes no enseña el UID y el identificador para la web será el código de `GetAccountCode` (Q-B13-21). `RevocationConfig` (`sec_ssv_grace_h`) queda en `NotBoundYet` hasta que `LocalRevocation` llegue al arranque.

**B4 (Bitácora del río «1 de 3», cola de takeovers y pantalla 2 del 10).**
- **Q-B1:** `played_date` dentro de `calendar` (a). **Ojo:** va contra la letra de D-033, punto 6 («solo `calendar` = `{cycle, next_day, last_claim_date}`»); se aplica por la regla y se anota en 08 §4.3 [P]. Opciones: (a) `played_date` en `calendar` (aplicada); (b) solo en memoria (cerrar la app antes de sellar obliga a jugar otro nivel); (c) un campo en `daily`. **El dueño debe confirmarla.**
- **Q-B2:** la pantalla 2 del 10 es la libreta sola con las 3 tarjetas de la casilla 1; la Canasta, en la victoria.
- **Q-B3:** 30 semillas por Marea, como D-033 (06 §4.2 y `undo3` dicen 10): el dueño confirma con los datos de `econ_sim`.
- **Q-B4:** la 1.0 lee `eco_calendar_table_v2` (fila nueva [P] en 08 §9) y, si falta o no vale, la tabla compilada; la v1 no se lee.
- **Q-B5:** la Luciérnaga bloqueada sigue la cadena Morpho → Cuia extra → «2 Marea + 40»; se desbloquea con `max_level` ≥ 100 (constante hasta que `lvl_murky_from_level` llegue a `BoosterConfig`).
- **Q-B6:** marco de país `frame_wNN` por mundo. **Q-B7:** Canasta de los días 7 y 21 = `eco_basket_seeds` + el comodín de la próxima Canasta de nivel. **Q-B8:** `reward_type` según lo recibido; `choice` la tarjeta tocada; `reward_value` sin lo fijo (08 §8.2 al día). **Q-B9:** si ya se abrió la libreta en la sesión, no sale sola. **Q-B10:** sesiones de la cola con su reloj de 30 min. **Q-B12:** tarjeta encima de la del Reto con sus tres estados. **Q-B13:** sin «Duplicar con video» en la 1.0. **Q-B14:** `econ_sim` da la Bitácora desde el día 1.
- **Q-B11:** la fila «Bitácora · elige tu premio» la dibuja el escritorio en la tercera barra de `VictoryBars` con `LogbookVictoryRowDue` y `OpenLogbookFromVictory()`. **Pendiente (tarea aparte):** la barra aún no existe; hasta entonces la libreta sale sola al volver al Inicio (punto 5).
- **Q-B15:** regenerar `econ_A.md` y `econ_B_tramos.md` aparte: **pendiente del visto bueno del dueño**.
- **Q-B16:** el aviso «Bitácora sin sellar» de B6 se conecta en una tarea aparte con un texto sin premio; hasta entonces, apagado.
- **Fusión [P] (del integrador, propuesta de la nota):** `calendar` el registro con más avance (`cycle`, `next_day`) y `played_date` por máximo.
- **Del integrador:** `TakeoverConfig` y `LoginCalendarConfig` quedan en `NotBoundYet` (falta su tabla en `RcConfigBinder`).

**B5 (Mercado, ofertas y Cántaro con la tienda falsa).**
- **Q-B5-1:** venta de skins apagada hasta el arte final. **Q-B5-2:** ninguna tarjeta de pago salvo la Mochila antes de `rescue_min_level`. **Q-B5-3:** las 48 h de la Mochila desde su primera tarjeta. **Q-B5-4:** recordatorios en la primera victoria desde el 15 y desde el 19, con cupo. **Q-B5-5:** el Reto no llena el Cántaro. **Q-B5-6:** Mochila y Cántaro en la victoria; Sin anuncios y skin al volver al inicio. **Q-B5-7:** Cántaro en el inicio bajo Ajustes. **Q-B5-8:** `piggy.last_level`, `piggy.full_card` y `offers` [P] en 08 §4.3. **Q-B5-11:** lista T1 de la nota. **Q-B5-12:** el Cántaro con una compra pendiente no se vacía hasta acreditarse (F3-5 lo vacía al acreditar). **Q-B5-13:** la skin comprada se pone. **Q-B5-14:** contexto `shop` de la tarjeta de skin. **Q-B5-15:** `from` = `lobby`.
- **Q-B5-9:** `piggy` del ganador; `offers` con listas unidas y contadores por máximo. Aplicada en `CloudMerge` al integrar; de las fechas, la primera vista de la Mochila más antigua (para no alargar sus 48 h) y la última tarjeta Sin anuncios más reciente [P].
- **Q-B5-10:** un solo cupo, el de la `TakeoverQueue` de B4 (`TakeoverQueueBudget`). Aplicada al integrar.
- **Con D-033 ya decididas:** Q-E2, Q-E3, Q-E5 («Restaurar compras» oculto), Q-E6, Q-M4 y Q-M8.
- **Sin conectar:** `VerifiedStoreService` (F3-3) en el Mercado, con F3-5.

### D-033, añadido del 2026-10-21: respuestas del dueño a tres preguntas de B4 y B8

- **Estado:** decididas por el dueño en el chat (formulario), con la recomendada en las tres.
- **Q-B1 (B4):** «hoy ya terminaste un nivel» se guarda como `played_date` dentro de `calendar`, que queda `{cycle, next_day, last_claim_date, played_date}` [P]. Matiza el punto 6 del diseño aprobado de la Bitácora. Se anota en 08 §4.3.
- **Q-B8-1 (B8):** al fusionar dos perfiles, `applied_purchases` es **la lista del perfil ganador**, y `restoreLedger` (el servidor) devuelve las compras que falten para reacreditarlas. Cambia 08 §11, que decía «unión». Se implementa en `CloudMerge` con su test.
- **Q-B15 (B4):** se regeneran `tools/out/econ_A.md` y `econ_B_tramos.md`, que ya no se reproducían antes de B4, en una tarea aparte; antes de darlas por buenas se enseñan al dueño las cifras [sim] que cambien.
- **Q-B3 (B4):** Marea = 30 semillas en la tasa de la Bitácora, como fijó D-033 (tasa (a)); se corrige 06 §4.2 donde diga 10.
- **Aplicadas (integrador, rama `tanda5/t5d-verify`):** Q-B8-1 en `CloudMerge.Fields.cs` (sin `Profile.MergeAppliedPurchases`), con `voided_purchases` [P] también del ganador (con la unión, un token reembolsado solo en el perdedor y acreditado en la cartera del ganador quedaría marcado como retirado y `ApplyLedger` ya no lo retiraría); 08 §11 (tabla y `ResolveConflict`), 08 §4.3 (`played_date`, Q-B1) y 06 §4.2 (Q-B3) al día. **Para F3-5:** el cliente aún no llama a `restoreLedger` tras fusionar (`CloudSync` solo fusiona; la parte local es `LocalRevocation.ApplyLedger`): con Q-B8-1, sin esa llamada una compra hecha solo en el teléfono que pierde no se reacredita hasta conectarla.

### D-038, añadido del 2026-10-21: la Balsa con las canoas apagadas

- **Estado:** decidido por el dueño en el chat: «Balsa con las canoas apagadas: quita las Canoas infinitas de su contenido y de la línea del modal (queda «semillas y una cuia»), sin cambiar el precio; anótalo en D-038 y en 04 §6 y §7.2.»
- **Decisión.** Con `biz_feature_lives = false`, la Balsa de rescate no incluye las Canoas infinitas: su contenido y la línea del modal de Aguas estancadas quedan en «semillas y una cuia». **El precio no cambia.** Con el interruptor en su default, la Balsa no cambia.
- **Aplicada (integrador, rama `tanda5/t5d-verify`):** `StoreLedger.CreditedCanoes` quita los 30 min de la Balsa (compra simulada y grant del servidor) y la línea pasa a `stuck_balsa_499_content_no_canoes` [P] («Todo para seguir: semillas y una cuia», `borrador`); 04 §6, §6.5 y §7.2 al día. **Para F3-5:** `LocalRevocation.ApplyLedger` reacredita `entry.InfiniteCanoes` tal como viene del libro mayor, así que con las canoas apagadas una Balsa devuelta por `restoreLedger` daría las 30 min; al conectarlo, aplicar la misma regla (quien arma las entradas desde el catálogo, o un dato de rescate en `LedgerEntry`).

### D-033, añadido del 2026-10-21: las cifras [sim] de B4 al GDD (Q-B15)

- **Estado:** decidido por el dueño en el chat: «Apruebo poner al día en el GDD (05 §2.4, §3.2, Desviación 1 y docs/GDD.md) las cifras [sim] de econ_A.md, econ_B_tramos.md y las columnas B de econ_B.md tras B4.»
- **Decisión.** Las cifras [sim] del GDD pasan a la corrida con la Bitácora «1 de 3» de D-033 (B4): `tools/out/econ_B.md` de `10f66534` y `econ_A.md` y `econ_B_tramos.md` de `201f2b6c` (300 corridas por perfil, semilla 2026). Cierra la parte pendiente de Q-B15.
- **Aplicada (rama `tanda5/econ-gdd`):** 05 (Resumen, principio 3, §2.4, §3.1-§3.6, Desviación 1, Conflictos, preguntas 1 y 2 y Dependencias), 04 (§2.4, §2.5, §3, §9 y Dependencias), 01 (§7.2 y Dependencias), 09 (§8, decisión 15) y `docs/GDD.md` (convención [sim], §2 #16, §3.3, §6.1 #4, §6.2 #3, §7.2 #25 y #27 y §8 #3). Los ratios de referencia de B pasan de 7,7 / 7,2:1 a 7,6 / 7,1:1 y, con tramos, el de 101-500 de 10,3 a 10,1:1 (sigue en banda). **Ojo:** el ratio del casual en 31-100 pasa de 10,1:1 («sí», en banda) a 9,9:1 («bajo») en los dos escenarios; ninguna conclusión del GDD dependía de que estuviera en banda. Sin tocar: la tabla de 05 §3.6, que sale de `econ_variants.md` (regenerado también en `10f66534`), sigue con la salida anterior a B4 hasta su propio visto bueno.

### D-033, añadido del 2026-10-21: la tabla de 05 §3.6 y la Bitácora «1 de 3» en 05 §2.3 y §3.1

- **Estado:** decidido por el dueño en el chat: «Apruebo poner al día la tabla V0-V7 de 05 §3.6 con econ_variants.md tras B4 y revisar 05 §2.3 y §3.1 frente al canon de la Bitácora «1 de 3» de D-033.»
- **Decisión.** La tabla V0-V7 de 05 §3.6 pasa a `tools/out/econ_variants.md` de `10f66534` (200 corridas por perfil, semilla 2026, con la Bitácora «1 de 3»), y 05 §2.3 y §3.1 se ponen de acuerdo con el canon de la Bitácora «1 de 3» de este registro (añadido del 2026-10-20, puntos 1-7, con Q-B1, Q-B3 y Q-B4). Cierra la parte de §3.6 que dejó abierta el añadido anterior (las cifras [sim] de B4).
- **Aplicada (rama `tanda5/econ-gdd2`):** `econ_variants.md` se reproduce idéntico con `py -3.14 tools/econ_variants.py`. 05 §3.6 (tabla y lectura) y §3.3 (V3: 8,7 / 8,1:1); 05 §2.1 (fila de la Bitácora), §2.3 (tabla retocada, opciones, totales, hitos con Canasta, Canasta grande y marco, tasa Marea = 30 y comodín = 100, `played_date`, `eco_calendar_table_v2`), §3.1 (los comodines del canon en lugar de «los 12 comodines») y «Parámetros»; 06 §4.2 y «Parámetros», por coherencia (D-033 ya los listaba en «Qué cambia en el GDD»); la cabecera de 05 y la convención [sim] y §3.3 de `docs/GDD.md` sin la marca de pendiente. Ninguna conclusión de 05 §3.6 cambia de sentido: V1, V4, V5 y V7 siguen en banda en los dos tramos (10,1-13,0:1, antes 10,3-13,2:1), V2 y V3 siguen fuera, y el casual con bloqueo baja en V0 del 76 % al 70 % (el descuento del primer rescate del día libra al 30 % de sus corridas, antes al 24 %).

### D-033, añadido del 2026-10-21: el canon de las casillas 2, 10, 17, 19 y 27 de la Bitácora

- **Estado:** decidido por el dueño en el chat (formulario), con la recomendada: «Canon «1 Marea + 30»».
- **Contexto:** con la tasa de valor de la Bitácora (Marea = 30 semillas por unidad; Q-B3), el canon «3 Marea + 30» de esas cinco casillas valía 120, pero sus tres opciones (60 semillas, 2 Marea, 1 Marea + 30) valen 60.
- **Decisión:** el canon de las casillas 2, 10, 17, 19 y 27 pasa a **«1 Marea + 30»** (vale 60, igual que la opción de mezcla). El ciclo pasa de 15 a 5 Mareas sueltas; las semillas del ciclo no cambian. Se ponen al día la tabla compilada del cliente y el default de `eco_calendar_table_v2` (08 §9), `CALENDAR` de `tools/econ_sim.py`, las simulaciones `tools/out/econ_*.md` y las cifras [sim] del GDD que dependan de ellas, y las tablas de 05 §2.3 y 06 §4.2.
- **Aplicada (rama `tanda5/bitacora-canon`):** `LoginCalendarTable.Default` y sus tests (5 Mareas sueltas en el canon; el canon de cada casilla vale lo mismo que sus opciones), `CALENDAR` de `tools/econ_sim.py`, 05 §2.1, §2.3 y §3.1, 06 §4.2 (con la suma de verificación de las Mareas), 08 §9 (`eco_calendar_table_v2` y, de paso, `eco_calendar_locked_booster_seeds` como tasa de valor que sustituye el comodín bloqueado) y `docs/GDD.md` (versión 1.22 y convención [sim]). El default de `eco_calendar_table_v2` en `RemoteConfigDefaults.json` sigue en `null` con su `pending` (la tabla compilada del cliente es el respaldo; `rc_defaults.py --check` idéntico). **Ninguna cifra [sim] cambia:** `econ_B.md`, `econ_A.md`, `econ_B_tramos.md` y `econ_variants.md` se reproducen byte a byte con el canon nuevo, porque el simulador cuenta 3 Mareas sueltas como una unidad `undo` y su inventario de Marea nunca está vacío en un bloqueo que pide Marea (las Canastas lo llenan de sobra; 0 llamadas al anuncio de Marea en 300 corridas por perfil), así que las 10 Mareas menos por ciclo (unas 3,3 unidades `undo`) no cambian ninguna decisión ni la secuencia aleatoria de las corridas. El peso de los hitos (51,7 %) y las 3.000 semillas no cambian. De paso, 05 §3.6 y su pregunta abierta 2 dejan de proponer «Cuia extra y Morpho ofrecidos antes del nivel» (Q-E1).

### D-035, añadido del 2026-10-21: preguntas de F3-5, con la regla «si tienen recomendada, vamos con esa»

- **Estado:** aplicadas con su recomendada por la regla del dueño; integradas con `backend/f3-5` (revisión APROBADO). El dueño puede cambiarlas.
- **«Eliminar cuenta» sin servidor** (sin red o con `biz_backend_enabled` apagado): no se borra nada, se muestra «No se pudo eliminar» y se puede reintentar.
- **`account_code` en la fusión con la nube:** manda el del dispositivo; tras reinstalar se pide otro (se anota en la tabla de 08 §11).
- **Cántaro acreditado tarde:** se resta solo lo acreditado, para no perder lo que entró en el Cántaro después de la compra (la recomendada; sustituye la (a) que aplicó F3-5 de vaciarlo entero).
- **Cántaro devuelto por `restoreLedger` o anulado:** se usa `eco_piggy_min_buy` [P].
- **Intervalo de `Pump`:** 5 s [P].
- **Textos de las 5 claves nuevas** (`iap_pending_confirm`, `iap_verify_failed`, `market_store_unavailable`, `settings_account_code`, `settings_account_code_unavailable`): aprobados en es-419; se piden EN y PT-BR.
- **Banda de edad** hasta que llegue B12 (Spark): se trata como 13 o más, con el `TODO(B12)` en `PlayerUnder13()`. **Conectada al integrar B12 (2026-10-21):** `PlayerUnder13()` lee la banda guardada (`profile.consent.age_band`) o el bloqueo del arranque, y el `TODO` se quita (ver el añadido de D-033 de B12).

## D-039 · Integración del Firebase Unity SDK 13.17.0 (F3-6)

- **Fecha:** 2026-10-21. Registrada el 2026-10-21 por el integrador.
- **Estado:** aprobado por el dueño en el chat: «Apruebo integrar el Firebase Unity SDK 13.17.0 con solo Auth, App Check, Analytics, Crashlytics y Remote Config (sin Functions), detrás de sus interfaces y con implementaciones falsas en el editor (F3-6). El google-services.json de dev lo coloco yo fuera del repo.» Y en el formulario: los paquetes se descargan y se versionan (Git LFS, referenciados por `file:` en `Packages/manifest.json`); si Firebase Analytics añade `com.google.android.gms.permission.AD_ID`, se quita del manifiesto fusionado (`tools:remove`) hasta que lleguen los anuncios de MAX; `WAKE_LOCK` se aprueba si aparece.

**Decisión.**
- Paquetes (descargados por el integrador el 2026-10-21 de `https://dl.google.com/games/registry/unity/<paquete>/<paquete>-<versión>.tgz`), con su SHA-256:
  - `com.google.external-dependency-manager-1.2.189.tgz`: `67087dbbb5f26746f5e2456159558de35ac14162a4ece7632d3ed3bdc0bfc175`
  - `com.google.firebase.analytics-13.17.0.tgz`: `8f857d19e09794f6f50921d0af419cbcd86dfb94e53586a9f41270728ae1f8f8`
  - `com.google.firebase.app-13.17.0.tgz`: `d74830478ed10eab56d848d92484e23b3aa9b27cbd1c843c21745be04f57eaa5`
  - `com.google.firebase.app-check-13.17.0.tgz`: `976a99eab31448dbf77aebedabc65fb6d51bd0ccedc9e5fdf228776a41a256ea`
  - `com.google.firebase.auth-13.17.0.tgz`: `14df2538229dc129a35c6509a799970bf998f32386beba080b4567e8f8b80092`
  - `com.google.firebase.crashlytics-13.17.0.tgz`: `0911e76db75e0a6410493e3db2e609b619ab0386cd7e9d66689406c3c7ec5658`
  - `com.google.firebase.remote-config-13.17.0.tgz`: `1862f969c6a1f78496796379a7d703deeb877fb5b687b9ed9dd06ba5fa8a16ee`
- Solo `app`, `auth`, `app-check`, `analytics`, `crashlytics` y `remote-config`, más EDM4U 1.2.189 (lo trae Firebase; 08 §7 #4). **Sin** `functions` ni `firestore`.
- Cada SDK, detrás de su interfaz con implementación falsa en el editor (08 §2.1, principio 6); los reales solo en `UNITY_ANDROID && !UNITY_EDITOR`. La recogida de Analytics sigue apagada hasta el consentimiento (B12).
- `google-services.json` lo coloca el dueño en una ruta ignorada por git; **ningún agente lo lee, lo crea, lo imprime ni lo commitea** (regla 5).
- Permisos: `WAKE_LOCK` aprobado si lo añade el SDK; `AD_ID` se quita hasta MAX. Cualquier otro permiso nuevo para la integración y se pregunta.

**Cómo revertir.** Quitando los paquetes de `manifest.json` y las implementaciones reales; las interfaces y las falsas siguen.

### D-039, añadido del 2026-10-21: los permisos que trae Firebase y App Check en desarrollo

- **Estado:** decidido por el dueño en el chat: «F3-6: quita ACCESS_ADSERVICES_ATTRIBUTION, ACCESS_ADSERVICES_AD_ID y READ_GSERVICES; aprueba BIND_GET_INSTALL_REFERRER_SERVICE (lista blanca y Data Safety). App Check: depuración en desarrollo, Play Integrity en release.»
- **Se quitan** del manifiesto fusionado (`tools:node="remove"`), como `AD_ID`: `android.permission.ACCESS_ADSERVICES_ATTRIBUTION` y `android.permission.ACCESS_ADSERVICES_AD_ID` (Privacy Sandbox de anuncios, de Firebase Analytics) y `com.google.android.providers.gsf.permission.READ_GSERVICES` (de reCAPTCHA, que arrastra Firebase Auth; el acceso anónimo no lo usa: se comprueba en el teléfono).
- **Se aprueban** `com.google.android.finsky.permission.BIND_GET_INSTALL_REFERRER_SERVICE` (Install Referrer de Play, para saber el origen de cada instalación; servicio de la propia Play Store, no un SDK de atribución aparte: matiza 08 §7) y `android.permission.WAKE_LOCK` (ya aprobado si aparecía). Entran en la lista blanca de `ci/check-manifest.ps1` y en Data Safety (08 §13.3).
- **App Check:** proveedor de depuración en los builds de desarrollo (`DEVELOPMENT_BUILD`; el dueño registra su token de depuración en la consola de Firebase) y Play Integrity en release.

### D-033, añadido del 2026-10-21: la reserva del 20 % (Q-B11-7)

- **Estado:** el dueño delegó la decisión en el integrador: «escoje lo conveniente si toca esperar esperamos o no entiendo exactametne que estoy eligiendo si es bueno o malo para el proyecto». El integrador elige la (a), la recomendada por el Spark, y se lo explicó al dueño en el chat.
- **Decisión (a):** un pack de reserva por mundo con, para cada nivel con mecánica (los primeros que necesita el kill switch de D-020) y hasta el 20 %, otro tablero de su mismo slot sin esa mecánica y en su ventana, con ids y `pack_id` propios. Cambia `schemas/af-level-1.schema.json` (contrato 21, `pack_schema`) de forma aditiva; el catálogo solo usa esos packs desde `level_overrides` (nunca como niveles de campaña). Se genera y se pasa por level-qa como el resto de la campaña.
- **Motivo:** el seguro del kill switch funciona desde la 1.0, cuando más riesgo hay, y su peso es pequeño (JSON); sin reserva, apagar una mecánica en producción solo podría sustituir sus niveles por otros ya publicados.
- **Implementada (2026-10-21, rama `tanda5/reserva-20`):** `pack_schema` 4 (`kind` = `reserve`, `pack_id` `reserva_{mm}_v{v}`, `replaces`, sufijos `_r{k}` y `:r{k}`); `tools/level-generator --reserve` y `tools/level-qa --reserve`; 132 tableros en los mundos 3-9 (8, 12, 12, 20, 24, 28 y 28; el mundo 2 solo lleva capacidades); el cliente los carga solo para `level_overrides` y lee `mech_enabled.*` y `level_overrides` de Remote Config en el arranque, antes del catálogo (`RcLevelBinder`). Reglas [P] de la herramienta (orden por rondas de los kill switches, celda sin mecánicas con sus capacidades, misma etiqueta y `bot_fail` más cercano) en 03 §17 y `tools/level-generator/README.md`. GDD: 03 §2, §10 y §17; 08 §4.1 y §9; 02 §9.2 regla 3 y pregunta 14.

### D-033, añadido del 2026-10-21: preguntas de B12 (consentimiento y edad) del Spark, con la recomendada

- **Fecha:** 2026-10-21. Registrada el 2026-10-21 por el integrador.
- **Estado:** decisión del dueño en el chat: «Integra B12 del Spark con sus parches y la verificación completa, y aplica sus 16 preguntas con la recomendada.» Integradas en `integra/spark-b12` con `spark/t5-b12` (`b5a18d0e`) y sus parches. Detalle y opciones en `docs/dev/consentimiento.md` §4. Completa Q-T3 (a) y Q-T4 (a) de D-033. **Los textos legales siguen [P], en `borrador` y con la nota «pendiente del abogado»**; lo que el abogado confirme puede cambiar las respuestas marcadas abajo.

- **Q-B12-1:** la pantalla de edad neutral son **tres botones iguales**, uno por banda, sin ninguno marcado (no el año de nacimiento). Pendiente del abogado: la neutralidad para Google Play.
- **Q-B12-2:** al subir `consent_text_version` **solo se vuelven a pedir las finalidades**; la banda guardada se conserva (08 §7 permite una sola corrección).
- **Q-B12-3:** el bloqueo de `<13` **es definitivo** en el dispositivo: no se vuelve a preguntar ni con textos nuevos y no hay salida desde la pantalla (solo reinstalar).
- **Q-B12-4:** el diagnóstico (Crashlytics) es una **línea informativa sin interruptor** (interés legítimo, 08 §2.3), pendiente de la pregunta 6 de 08 al abogado. *Sin efecto desde D-057, añadido del 2026-10-27: interruptor «Enviar informes de fallos» en Ajustes (T-L6).*
- **Q-B12-5:** los textos legales van en `strings.csv` con el estado **`borrador`** y la nota «pendiente del abogado» (sin un estado nuevo en el contrato 32).
- **Q-B12-6:** la doble confirmación es **una pantalla más**: el toque en «Comprar» del juego, una confirmación con el precio de la tienda y después la hoja de Google Play. Pendiente del abogado, con Q-T4.
- **Q-B12-7:** con 13-17 **la analítica se ofrece** como finalidad que se puede aceptar (sin audiencias ni anuncios personalizados). Pendiente del abogado (ECA Digital).
- **Q-B12-8:** `profile.consent` **es del dispositivo**: la fusión con la nube conserva el del local y un teléfono nuevo vuelve a preguntar. Aplicada en `CloudMerge` al integrar (`merged.Consent = local.Consent`, con test).
- **Q-B12-9:** con el perfil `ump`, **la analítica en la pantalla propia y los anuncios por UMP**, hasta que el dueño configure UMP.
- **Q-B12-10:** el campo nuevo se llama **`consent.age_band_corrected`** (bool) [P]; se anota en 08 §4.3.
- **Q-B12-11:** `af_settings_change.value` con `key = consent` vale **`choices` o `age`** [P].
- **Q-B12-12:** el encargo del Spark decía que `INTERNET` no estaba aprobado; **lo aprobó D-036** (2026-10-20). Sin efecto en B12 (no usa red); se corrige el encargo.
- **Q-B12-13:** «el tratamiento más estricto» sin país es **el perfil `default` + `npa` sin elección** (la analítica se puede aceptar).
- **Q-B12-14:** la fila marcable se **extrae a una clase común de AF.UI al integrar**: `OptionRow` (`Scripts/UI/OptionRow.cs`), usada por Ajustes y por las finalidades de la pantalla de consentimiento.
- **Q-B12-15:** si cambia el perfil legal del país (otra SIM, un viaje), **se vuelve a preguntar** las finalidades con el perfil de ahora y la banda guardada.
- **Q-B12-16:** las capturas de referencia de los textos nuevos las saca **el integrador con `SnapshotKit`** (es-419 y en-XA al 130 %): `ConsentSnapshotTests` (explícita) con la edad, las finalidades, el bloqueo, la doble confirmación y Ajustes (también en 16:9).

- **Del integrador, al aplicar la nota §6 (sugerencias de la 4.ª revisión):** la 1 (`ConsentHoldsSession` retiene la sesión y los avisos mientras no haya una decisión vigente, también en el segundo de la pantalla de arranque de quien debe volver a decidir, con su test) y la 7 (Q-B12-14). Las demás (2-6 y 8) quedan abiertas para una tarea aparte.
- **Orden de arranque (del integrador):** `SetUpConsent` va tras `ConnectTelemetry` y **antes** de `ResumeBackend` (F3-5), para que los flags guardados lleguen a la analítica, los anuncios y la tienda antes de reanudar las compras; el decorador de la tienda de B12 envuelve la tienda verificada de F3-5 (`WrapStore(WrapConsentStore(CreateStore(...)))`).
- **Enganches con F3-6 (Firebase; D-039), a petición del coordinador de la integración:** (1) la decisión llega a la recogida propia de Firebase Analytics por `AnalyticsService.SetCollectionEnabled` (`ICollectionAwareSink`) y con la banda `<13` nunca se enciende; (2) con `<13` Crashlytics se apaga de forma explícita (`CrashReporting.Stop`: `IsCrashlyticsCollectionEnabled = false`, que persiste entre arranques), al bloquear y en cada arranque bloqueado; (3) «Eliminar cuenta y datos» llama también a `FirebaseAnalyticsSink.ResetData()` (08 §13.3). Con tests PlayMode (`ConsentBootTests` y `AccountAndCloudTests`).

### D-033, añadido del 2026-10-21: preguntas de B11 (tutorial, Sin canoas y contenido) del Spark, con la regla «si tienen recomendada, vamos con esa»

- **Fecha:** 2026-10-21. Registrada el 2026-10-21 por el integrador.
- **Estado:** decididas con la recomendada (la (a), ya aplicada en la rama) por la regla del dueño «si tienen recomendada, vamos con esa», salvo Q-B11-7, que ya decidió el añadido «la reserva del 20 %» de arriba. Integradas en `integra/spark-b11` con `spark/t5-b11` (`0fe3e99c`) y sus seis parches. Detalle y opciones en `docs/dev/tutorial-b11.md` §5. Respetan D-038 y su añadido de la Balsa: con `biz_feature_lives` apagado no hay tarjeta de Canoas, el tooltip del 6 y del 8 pierde la frase de las canoas y Sin canoas no se abre.

- **Q-B11-1:** el dedo del 2 **señala antes el vertido que abre la lección** y después la lección [P] (sonda `lesson = partial_setup`).
- **Q-B11-2:** con `biz_feature_lives` apagado, el tooltip del 6 y del 8 lleva **solo la primera frase**, «Reiniciar es gratis, siempre, mismo tablero.» (`tutorial_restart_free`).
- **Q-B11-3:** **se acepta la excepción del 680**: con la Cuia extra, su tercer fallo sigue con el tablero base; el resto de 31-700 lleva `minus_color` (segunda pasada de Q-N2 (a), presupuesto 2.000.000 [P]).
- **Q-B11-4:** texto del tooltip de la Cosecha doble del 11: **«Con un video, las semillas de este nivel valen el doble.»** [P].
- **Q-B11-5:** **lienzo al ancho del teléfono entre 320 y 360 dp**, con 1 dp físico por dp (toca `CanvasScale` de `GameScreen` y `LobbyScreen` y sus pruebas). **No aplicado en esta integración: tarea del escritorio aparte**; hasta entonces el lienzo sigue midiendo 360 dp.
- **Q-B11-6:** «25 minutos» y «nivel 31» van **literales** en los textos (Q-E4 (a) y 04 §2.7); si Remote Config cambia `canoes_regen_min` o `rescue_min_level`, hay que revisar los textos.
- **Q-B11-7:** decidida en el añadido «la reserva del 20 %» (un pack de reserva por mundo); **no se implementa aquí**: será otra tarea (hecha el 2026-10-21 en `tanda5/reserva-20`, con `level_overrides` enlazado a Remote Config).
- **Q-B11-8:** se quedan el id `home` de `AF_LAYOUT` y la fila `back_home`.
- **Q-B11-9:** la tarjeta del 31 habla **solo de las canoas** (el texto de Q-E4 (a)), sin línea de la Cuia de rescate.
- **Q-B11-10:** el dedo de la Cuia extra del 8 sale **en cada intento hasta usarla**.
- **Q-B11-11:** C10 **solo arrastra la cuia que se elevó en el touch-down sin selección** (también si otro dedo selecciona entre medias, C11); un arrastre ilegal no selecciona nada: Y se sacude (C6), sin C5.
- **Q-B11-12:** el tooltip de la Cosecha doble sale **solo en el 11**: si en esa victoria no hay anuncio, no sale nunca.

- **Quedan para otras tareas (del integrador):** enlazar `drag_pour_enabled`, `lvl_label_override` y `level_overrides` con Remote Config en el arranque; emitir `af_label_shown` (con `LevelCatalog.IsRelabeled` para `source`) y todas las elecciones de `af_no_canoes_choice` a la vez; Q-B11-5 (lienzo a 320 dp); Q-B11-7 (la reserva); los cambios propuestos al GDD de la nota §4 (02 §5 y §13, 03 §13 y §17, 04 §7.1 y 06 §2.2).

### D-033, añadido del 2026-10-21: cierre de B11 en el escritorio (rama `tanda5/b11-cierre`)

- **Estado:** aplicado por el integrador; detalle en `docs/dev/tutorial-b11.md` §7. Cierra, del añadido de B11, «Quedan para otras tareas» salvo `level_overrides` y Q-B11-7 (la reserva, otro carril), y la Q-B11 de B4 (la fila de la victoria ya existe).
- **Hecho:** `drag_pour_enabled` y `lvl_label_override` desde Remote Config (antes del catálogo); `af_label_shown` y todas las elecciones de `af_no_canoes_choice`; la fila «Bitácora · elige tu premio» (tercera barra, 48 dp); Q-B11-5 (a), lienzo de 320 a 360 dp con 1 dp físico por dp; los cambios al GDD de la nota §4 (sin 03 §17) y 05 §7 alineado con Q-T4 (a) y la UMP dentro de MAX (también 01 y 08, que aún decían «PIN»).
- **[P] del integrador, para confirmar:** (1) la fila no sale si con ella la victoria compacta necesitaría el escalón 3 (iría tras el chevron): hoy no sale a 360 × 640, y la libreta sale sola al inicio; (2) por debajo de 320 dp físicos el lienzo mide 320 dp escalado, y en un 16:9 de 320 dp manda el alto mínimo de 640 (lienzo de 360 × 640, texto por debajo de 12 sp físicos); (3) `af_label_shown` con cada intento, como `level_start`; (4) en el hito de TiTino que no cabe, se quita la fila antes que la Crecida y la Canasta; (5) al sellar desde la fila la victoria se recoloca y su escalón puede bajar.

### D-033, añadido del 2026-10-21: preguntas de la reserva del 20 %, con la regla «si tienen recomendada, vamos con esa»

- **Estado:** aplicadas con su recomendada por la regla del dueño; integradas con `tanda5/reserva-20` (revisión APROBADO). El dueño puede cambiarlas.
- **Q-R20-1:** el 20 % se reparte por rondas de mecánica, para que cada una tenga cubiertos sus primeros niveles.
- **Q-R20-2:** el tablero de reserva quita todas las mecánicas de su slot (también las Aguas turbias) y conserva las capacidades; un tablero cubre todos los kill switches de su slot.
- **Q-R20-3:** el tope del 20 % se redondea hacia abajo (8, 12, 12, 20, 24, 28 y 28 en los mundos 3-9; 132 tableros).
- **Q-R20-4:** en la 1.0, `level_overrides` solo se aplica cuando una mecánica está apagada (08 §9); sustituir niveles en alarma o por segmento (DDA, 03 §2 y §15) queda para otra tarea.
- **Q-R20-5:** los tableros de reserva no publican `difficulty.D` (no entran en el pool del rango).

### D-033, añadido del 2026-10-21: preguntas del cierre de B11, con la regla «si tienen recomendada, vamos con esa»

- **Estado:** aplicadas con su recomendada por la regla del dueño; integradas con `tanda5/b11-cierre` (revisión APROBADO). El dueño puede cambiarlas.
- **P1:** a 360 × 640 (16:9) la fila «Bitácora · elige tu premio» no sale (necesitaría el escalón 3 de la victoria compacta); ahí la libreta sale sola al volver al inicio.
- **P2:** en teléfonos de menos de 320 dp el lienzo mide 320 dp escalado; en un 16:9 de 320 dp manda el alto mínimo de 640 (lienzo 360 × 640).
- **P3:** `af_label_shown` sale con cada intento, como `level_start`.
- **P4:** al sellar desde la fila, la victoria se recoloca y «Siguiente» puede moverse una vez.

### D-033, añadido del 2026-10-21: notificaciones reales (Q-M5)

- **Estado:** el dueño ordenó en el chat: «Integra las notificaciones reales con com.unity.mobile.notifications 2.4.3 y POST_NOTIFICATIONS (D-033 Q-M5), desde el parche B6-notifications-real, con la verificación completa.»
- **Versión exacta aprobada:** `com.unity.mobile.notifications` **2.4.3** (la que declara el Editor 6000.3.25f1, propuesta por el Spark en B6), del registro de Unity. Permiso `android.permission.POST_NOTIFICATIONS` aprobado (Android 13+, con el pre-prompt de B6 antes de pedirlo). Entra en la lista blanca de `ci/check-manifest.ps1`.
- Si el paquete añade otros permisos (por ejemplo `RECEIVE_BOOT_COMPLETED` o `SCHEDULE_EXACT_ALARM`), se para y se pregunta; 08 §13 y Q-M5 piden alarmas inexactas.
- **Integración (rama `tanda5/notificaciones`, 2026-10-22):** el manifiesto fusionado del `dev.apk` y del ensayo `rehearsal.aab` solo gana `android.permission.POST_NOTIFICATIONS` y el receptor `com.unity.androidnotifications.UnityNotificationManager` (no exportado), con `com.unity.androidnotifications.exact_scheduling` = 0: **ningún otro permiso**, sin `RECEIVE_BOOT_COMPLETED`, `SCHEDULE_EXACT_ALARM` ni `USE_EXACT_ALARM` (los ajustes del paquete se quedan en sus valores por defecto). Sin `.so` nuevos (el paquete es Java). Peso: el `rehearsal.aab` pasa de 53,49 a 53,56 MB (+70.750 bytes; descarga de Play de 52,46-52,52 a 52,54-52,59 MB) y el `dev.apk`, +81.162 bytes. Programador en `Scripts/Platform/Notifications/` (`AF.Platform.Notifications`); detalle en `docs/dev/sesiones-notificaciones.md` §8. Consecuencia aceptada de no tener `RECEIVE_BOOT_COMPLETED`: tras reiniciar el teléfono los avisos programados se pierden hasta la siguiente salida de la app. Con <13, al bloquear se cancelan los avisos y la bandeja.

## D-040 · Integración de Unity IAP 5.4.4 (Play Billing), encargada al Spark

- **Fecha:** 2026-10-22. Registrada el 2026-10-22 por el integrador.
- **Estado:** aprobado por el dueño en el chat: «Encarga al Spark integrar Unity IAP 5.4.4 (Play Billing) tras IBillingClient, con la tienda falsa en el editor; apruebo el paquete com.unity.purchasing 5.4.4. Prepárale el encargo y la frase.»
- **Decisión:** `com.unity.purchasing` **5.4.4** exacta (plan de instalación; 08 §7), que trae Play Billing 8 o superior. La implementación real va detrás de `IBillingClient` (F3-3) solo en `UNITY_ANDROID && !UNITY_EDITOR`; el editor y los tests siguen con `FakeBillingClient`. La verificación sigue en el servidor (`VerifiedStoreService`, D-035): el cliente nunca acredita sin el `ok` o el `duplicate` del backend.
- **Permiso:** Play Billing añade `com.android.vending.BILLING`, que es inseparable de cobrar en Google Play. **Aprobado por el dueño el 2026-10-22:** «Apruebo el permiso com.android.vending.BILLING para Play Billing (D-040); que entre en la lista blanca de check-manifest cuando se integre Unity IAP.» Entra en la lista blanca de `ci/check-manifest.ps1` con la integración de Unity IAP; cualquier otro permiso nuevo, se para y se pregunta.
- **Encargo:** `docs/dev/traspaso/spark-reto-del-rio.md` §14.

### D-033, añadido del 2026-10-22: preguntas de B9 (accesibilidad) del Spark, con la regla «si tienen recomendada, vamos con esa»

- **Fecha:** 2026-10-22. Registrada el 2026-10-22 por el integrador.
- **Estado:** decididas con la recomendada (la (a), ya aplicada en la rama) por la regla del dueño «si tienen recomendada, vamos con esa». Integradas en `integra/spark-b9` con `spark/t5-b9` (`7f924192`) y sus cuatro parches (`B9-profile`, `B9-rc`, `B9-ui` y `B9-loc`). Detalle y opciones en `docs/dev/accesibilidad.md` §5. El dueño puede cambiarlas. **Quedan pendientes** (la (a) es «tarea aparte» o depende del dueño o de otros): Q-B9-6, Q-B9-12, Q-B9-13, Q-B9-14, Q-B9-17, y los cambios al GDD de Q-B9-15 y Q-B9-16.

- **Q-B9-1:** un cambio de la fuente del sistema con el juego abierto **se aplica al volver a abrirlo** (la escala se lee una vez por arranque).
- **Q-B9-2:** con la fuente del sistema por debajo del 100 %, el texto **se queda al 100 %** (≥ 12 sp, 02 §12).
- **Q-B9-3:** con el ajuste en `auto`, «Quitar animaciones» de Android **enciende Reducir movimiento**.
- **Q-B9-4:** «sin mover el dedo» en C9 es **< 24 dp**, el umbral de C10.
- **Q-B9-5:** Alto contraste **no enciende los iconos** (son de Patrones de fruta; 07 §3, regla 5 literal).
- **Q-B9-6:** el contorno de dosis de 1,5 px de 07 §3, regla 5, va en **una tarea aparte** con el shader y capturas (`A11yRules.HighContrastDoseOutlinePx` ya existe). **Pendiente:** hasta entonces Alto contraste solo sube el patrón al 35 %.
- **Q-B9-7:** una fruta sin nombre aprobado por el asesor se lee como **«fruta»** (`a11y_fruit_unnamed`).
- **Q-B9-8:** las «3 dosis dentro» de la Tinaja se leen **todas, con la boca**.
- **Q-B9-9:** el botón de comodín con precio o video lleva **solo la etiqueta** («Deshacer, 2 restantes»), sin el precio.
- **Q-B9-10:** el simulador es **Machado 2009 + CIEDE2000**, con informe sin umbral y QA manual (08 §14.3); no es compuerta.
- **Q-B9-11:** «{fruit} seleccionado» va **tal cual el texto de 02 §12**, también con frutas femeninas (pendiente del revisor de textos).
- **Q-B9-12:** el traslado inverso de Marea (250 ms) se multiplicará por el factor de Reducir movimiento (02 §4, «todas las duraciones») **en una tarea aparte**. **Pendiente:** hoy sigue fijo.
- **Q-B9-13:** los pares 7-10 (protanopía) y 5-14 (deuteranopía) del simulador **se añaden a la lista de pares de riesgo** de 07 §3 y del glosario §9, para que el generador los limite. **Pendiente del dueño y de otra tarea:** editar el GDD y el generador (y volver a generar si cambia algún tablero); no se toca en esta integración.
- **Q-B9-14:** la etiqueta de la cuia añadirá el nombre de la mecánica con las claves de sus nombres (08 §14.2) **en una tarea aparte**. **Pendiente:** hoy la etiqueta es la de 02 §12.
- **Q-B9-15:** los campos del perfil son **`settings.reduce_motion`** (`auto` | `on` | `off`) y **`settings.high_contrast`** (bool) [P]. **Pendiente:** anotarlos en 08 §4.3.
- **Q-B9-16:** `af_settings_change.key` admite **`reduce_motion` y `high_contrast`**. **Pendiente:** añadirlos a 08 §8.2.
- **Q-B9-17:** TalkBack con la lista de Ajustes que se desplaza (al 130 % o en pantallas bajas): la acción de desplazar del lector (`AccessibilityNode.scrolled`) va **en una tarea aparte**. **Pendiente:** hasta entonces las filas bajo el corte no se publican.
- **Del integrador:** el rechazo del arrastre C10 (B11, `GameScreen.Drag.cs`) tampoco sacude la cuia con Reducir movimiento, como el toque ilegal (02 §12). Los ayudantes de prueba `B9Loc`, `B9LocPlay` y `docs/dev/traspaso/spark-enganches/B9-strings.csv` se quedan: con `B9-loc.diff` aplicado no añaden ninguna fila.

### D-033, añadido del 2026-10-22: preguntas de la UI de los avisos, con la regla «si tienen recomendada, vamos con esa»

- **Estado:** aplicadas con su recomendada por la regla del dueño; integradas con `tanda5/notif-ui` (revisión APROBADO). El dueño puede cambiarlas.
- **Segundo pre-prompt con el hito 3 en la misma sesión que el primero:** (b) se recuerda que el hito 3 ya se alcanzó y el segundo sale en la primera victoria de la sesión siguiente. **Pendiente de implementar** (hoy se va a los 7 días).
- **Victoria del pre-prompt:** no sale ninguna oferta (no solo la Mochila) [P].
- **Entrada al inicio tras el pre-prompt:** no abre la libreta sola ni gasta el cupo de takeovers; sale en la siguiente entrada [P].
- **Analítica:** `origin` de `af_push_prompt` y la clave `push` de `af_settings_change` se dan de alta en 08 §8.2 y en `AnalyticsCatalog` en una tarea aparte.
- **Tope de espera del diálogo nativo tras «Avísame»:** 5 s con la app delante [P].
- El integrador confirma que la tarea podía tocar `docs/dev/prototipo-v0.md` y `docs/dev/sesiones-notificaciones.md` (las sondas se anotan en el mismo PR).

### D-033, añadido del 2026-10-22: preguntas de los arreglos de la prueba en el teléfono del 2026-10-22, con la regla «si tienen recomendada, vamos con esa»

- **Estado:** aplicadas con su recomendada por la regla del dueño mientras dormía; integradas con `tanda5/fx-telefono-1022` (revisión APROBADO). El dueño puede cambiarlas.
- **02 §13, fila 6:** el tooltip «Reiniciar es gratis…» va dentro del panel, sobre el botón (se corrige el texto del GDD en la próxima tarea de documentos).
- **En el 7, la tarjeta del Reto tiene prioridad sobre el gesto de TiTino** [P].
- **Prefijo `AF_EVENT_UNSENT`** en logcat para los eventos sin consentimiento (Firebase no recibe nada sin él).
- **Excepción a Q7-A:** en la victoria del 11, en pantallas bajas, «Siguiente» puede quedar algo más arriba [P].
- **Q-B9-18:** «Reducir movimiento» sigue como interruptor sí/no (con `auto` al arrancar hasta que el jugador lo toque).
- **Aviso «unity pipeline upgrade»** de `com.unity.pipeline` 0.8.0-exp.1: no se toca hasta que falle algo; actualizarlo sería un cambio de versión que aprueba el dueño.
- **Riesgos bajos que se dejan hasta verlos en el teléfono:** la tarjeta del Reto no se recoloca si TiTino cambia de caja después; en 16:9 el hueco del tooltip del 6 puede pasar Aguas estancadas a la disposición compacta.

### D-040, añadido del 2026-10-22: preguntas de la integración de Unity IAP (Q-IAP-n), con la regla «si tienen recomendada, vamos con esa»

- **Estado:** Q-IAP-2 a Q-IAP-7 aplicadas con su recomendada por la regla del dueño; integradas con `spark/t5-iap` (revisión APROBADO en el Spark) y sus parches `IAP-*`. El dueño puede cambiarlas. **Q-IAP-1 no está decidida.** Detalle: `docs/dev/iap.md` §4.
- **Q-IAP-1 · Telemetría de compras de Unity IAP a Unity: pendiente del dueño y del abogado; bloquea el soft launch.** Unity IAP 5.4.4 envía a Unity (Insights, `prd.insights.analytics.unity3d.com`) un evento por cada paso de cada compra con el SKU, el precio, el token y datos del dispositivo, sin pasar por nuestro consentimiento y sin interruptor en el cliente; no está en Data Safety ni en la política. Opciones: (a) seguir con Unity IAP para las pruebas internas y, antes del soft launch, que el dueño desactive la recogida de «Developer Data» en el panel de Unity si el proyecto se vincula (hoy no lo está), el abogado valore la fila de Data Safety y la política, y se mida en el teléfono con un proxy qué sale; (b) copiar el paquete como embebido y anular el emisor (modificar un paquete de Unity: mantenimiento y licencia por revisar); (c) sustituir Unity IAP por un plugin propio de Play Billing 9 (`.androidlib`) detrás del mismo `IBillingClient` (sin telemetría de terceros; más trabajo y una dependencia nueva que aprobar). Mientras tanto el código sigue como en (a) solo para pruebas internas; la fila de 08 §13.3 queda marcada pendiente.
- **Q-IAP-2 · `deferred_ack` de consumibles:** (a) con un consumible no se hace nada (Unity IAP 5.4.4 no reconoce sin consumir; si el servidor no verifica en 72 h, Play reembolsa y no se acreditó nada); con un no consumible, `ConfirmPurchase` (solo reconoce). Lo dice 08 §10.1 (parche de docs).
- **Q-IAP-3 · Monedas fuera de 05 §4.2:** (a) `LocalPrice` admite cualquier código ISO 4217 (`IAP-price.diff`, aplicado), con los decimales de `LocPrice`; queda para otra tarea fijar en D-025 los decimales de las monedas sin céntimos (CLP, JPY) y de las de tres decimales (KWD, BHD).
- **Q-IAP-4 · No consumibles con otro UID:** (a) tarea pendiente del backend (`infra/`): con un no consumible de otro UID que Play da como comprado por esta cuenta, el servidor reasigna el derecho o responde `duplicate`. Hasta entonces, cada arranque recibe un 409 de Sin anuncios en ese caso. El cliente nunca acredita sin el servidor.
- **Q-IAP-5 · Conectar Play antes de elegir la edad:** (a) solo con la edad decidida, nunca con la banda `<13` y con el UID de Firebase ya creado.
- **Q-IAP-6 · Paquetes que arrastra Unity IAP:** (a) se aprueban como parte de D-040 con su versión, tal como los resuelve Unity en `packages-lock.json`: `com.unity.services.core` 1.18.0, `com.unity.services.deployment` 1.7.2 y `com.unity.services.deployment.api` 1.1.3 (sin permisos Android; no se llama a `UnityServices.InitializeAsync`). También entran por Gradle Play Billing Library 9.0.0 (`com.android.billingclient:billing:9.0.0`) y dos *stubs* de `kotlin-stdlib-jdk7/jdk8` 1.8.22.
- **Q-IAP-7 · `Assets/Resources/BillingMode.json`:** (a) commiteado una vez (`{"androidStore":"GooglePlay"}`, con su carpeta), no ignorado.
- **Permisos del manifiesto fusionado:** el único nuevo es `com.android.vending.BILLING` (aprobado en D-040).

### D-040, añadido del 2026-10-22: dependencias de Unity IAP 5.4.4 (Q-IAP-6)

- **Estado:** aprobado por el dueño en la sesión del Spark (registro en `docs/dev/iap.md` §4, commit `1f3f468a` de `spark/t5-iap`): «Apruebo com.unity.services.core 1.18.0, com.unity.services.deployment 1.7.2 y com.unity.services.deployment.api 1.1.3, dependencias de Unity IAP 5.4.4 (Q-IAP-6); anótalo para D-040.»
- Quedan aprobadas esas tres versiones exactas, que trae `com.unity.purchasing` 5.4.4 y fija `packages-lock.json`. Q-IAP-1 (telemetría de compras a Unity) sigue pendiente del dueño y del abogado.

### D-040, añadido del 2026-10-22: Q-IAP-1 decidida, plugin propio de Play Billing en lugar de Unity IAP

- **Estado:** decidido por el dueño en el chat, tras leer el análisis de la licencia y de las opciones: «Q-IAP-1: opción (c). Apruebo com.android.billingclient:billing directo y quitar Unity IAP y sus dependencias; prepara el encargo al Spark y regístralo en D-040.»
- **Por qué:**
  - La licencia de `com.unity.purchasing` (su `LICENSE.md`) lo somete a los términos de los servicios de Unity Operate. Ahí Unity se declara responsable de los datos del servicio (§3.1) y puede usar los de cada jugador (§3.2). El consentimiento y la política de privacidad nos tocan a nosotros (§3.3). La licencia solo permite usar el SDK «como dice la documentación» (§1.1), así que parchearlo para anular el emisor (opción (b)) queda fuera de ella.
  - En Unity 6000.3, `PurchaseEventEmitter` envía cada compra por HTTP a `prd.insights.analytics.unity3d.com` sin mirar el consentimiento: solo lo anota dentro del evento.
  - La documentación de privacidad de IAP 5.4+ dice que los datos básicos se recogen siempre y que el producto no tiene servicio de consentimiento.
  - Su ficha de Data Safety declara historial de compras, identificadores, ubicación aproximada y diagnóstico, sin forma de que el jugador pida el borrado. No encaja con «Eliminar cuenta y datos» (B8). El panel de «Developer Data» de la opción (a) no está documentado para IAP.
- **Decisión:**
  - Se sustituye Unity IAP 5.4.4 por un plugin Android propio (`.androidlib`) sobre la **Play Billing Library 9.0.0** (`com.android.billingclient:billing:9.0.0`), que se declara como dependencia directa. Es la versión que ya entraba por Unity IAP; cambiarla exige otra aprobación.
  - El plugin va detrás del mismo puerto (`IPlayStorePort` bajo `PlayBillingClient` e `IBillingClient`). `VerifiedStoreService`, `BillingReadyStoreService`, `ConsentStoreService`, la tienda falsa del editor y la verificación en AWS no cambian.
  - Se quitan `com.unity.purchasing` 5.4.4 y las dependencias que traía y nadie más usa: `com.unity.services.core` 1.18.0, `com.unity.services.deployment` 1.7.2 y `com.unity.services.deployment.api` 1.1.3. Así **se retira la aprobación de Q-IAP-6**.
  - Se quitan también `Assets/Resources/BillingMode.json` (Q-IAP-7), el ensamblado `AF.IAP.UnityIap` y el símbolo `AF_UNITY_IAP`.
  - El permiso `com.android.vending.BILLING` sigue aprobado y en la lista blanca.
- **Consecuencias:**
  - Desaparecen la telemetría de compras a Unity y la búsqueda en el catálogo remoto de Unity (`StoreOverrideReverseLookupService`).
  - La fila de 08 §13.3 pasa a ser solo «Google Play Billing», sin terceros.
  - **Q-IAP-2 se reabre:** con la librería directa sí se puede reconocer un consumible sin consumirlo (`acknowledgePurchase`). El encargo lo hace como pedía Q-F3-3 3 y lo anota.
  - Q-IAP-3, Q-IAP-4 y Q-IAP-5 siguen como estaban.
- **Mientras tanto:** Unity IAP sigue en `integracion-fase1-fase2` hasta que se integre el reemplazo, solo para pruebas internas. Las compras reales necesitan además la ficha de Play Console, que espera al dictamen de marca (D-034).
- **Pendiente aparte (sin encargo):** comprobar qué envía el propio runtime de Unity 6.3 en un build Android (Engine Diagnostics o Insights, que el emisor de IAP menciona). Es tema del abogado y de Data Safety.
- **Encargo:** `docs/dev/traspaso/spark-reto-del-rio.md` §15.

### D-040, añadido del 2026-10-22: dependencias Maven de Play Billing Library 9.0.0 (Q-BILL-1)

- **Estado:** aprobado por el dueño en la sesión del Spark (registro en `docs/dev/iap.md` §4.1, commit `ea348c76` de `spark/t5-billing`): «Apruebo las dependencias Maven de com.android.billingclient:billing 9.0.0 de Q-BILL-1 con las versiones que resuelve Gradle; anótalo para D-040.»
- Quedan aprobadas las dependencias que declara el POM de `billing:9.0.0`, con la versión que pide el POM como mínimo y tal como las resuelve Gradle junto a Firebase (dev.apk, `:unityLibrary:dependencies`; tabla en `iap.md` §4.1):
  - `androidx.activity:activity` 1.2.3 → 1.6.0; `androidx.core:core` 1.15.0 → 1.15.0;
  - `com.google.android.datatransport:transport-api` 3.0.0 → 3.2.0, `transport-backend-cct` 3.1.8 → 3.3.0 y `transport-runtime` 3.1.8 → 3.3.0;
  - `com.google.android.gms:play-services-base` 18.5.0 → 18.10.1, `play-services-basement` 18.9.0 → 18.11.0, `play-services-location` 19.0.0 → 19.0.0 (con `play-services-places-placereport` 17.0.0) y `play-services-tasks` 18.2.0 → 18.4.1.
- Son las mismas que ya entraban con Unity IAP 5.4.4 (que traía `billing:9.0.0`); ninguna añade permisos. Las únicas que solo trae Play Billing son `play-services-location` y `places-placereport`. Subir `billing` o fijar otras versiones a mano exige otra aprobación.
- Con la integración de `spark/t5-billing` y sus parches `BILL-*` (rama `integra/spark-billing`), Unity IAP y sus tres dependencias de Unity Services salen de `integracion-fase1-fase2`.

### D-033, añadido del 2026-10-22: pendientes del dueño 2 a 12, todos con la recomendada

- **Estado:** decididos por el dueño en el chat: «Pendientes 2 a 12: todas con la recomendada.» El 1 es Q-IAP-1 (D-040).
- **2 · Pororoca en inglés y portugués (P-18 de `docs/dev/pororoca.md`):** la etiqueta corta es «Mix» / «Misturar». «Shuffle» y «Embaralhar» no caben en la ranura de 56 dp a 12 sp. Se anota en el glosario §10.
- **3 · Nombres geográficos (T2-Q7 y T2-Q8):** se permiten en los textos del juego «the Amazon» / «Amazônia», «Amazonas», Tocantins y Putumayo. La regla de evitar la cadena «Amazon» (glosario §1) vale para el nombre de la app y los identificadores, no para la geografía. El asesor cultural puede ajustarlos. `settings_about_amazon`: «About the Amazon» / «Sobre a Amazônia».
- **4 · Título de la ficha de frutas (T2-Q10):** «River Fruits» / «Frutas do Rio». Se anota en el glosario §10, con el que `glossary.csv` debe coincidir.
- **5 · Cuia del Encuentro:** nombre corto «Meeting Cuia» / «Cuia do Encontro», para la skin y el pack (`skin_af_skin_m03_name`, `iap_af_skin_m03_name`). La fila del Mercado no se ensancha.
- **6 · Nombres provisionales, hasta el asesor:**
  - «Totuma», igual en los tres idiomas (skin y pack m04);
  - «Mount Mismi» / «Nevado Mismi»;
  - «Big Basket» / «Cesta grande»;
  - PT «Nova espécie à vista!».
- **7 · Textos legales de B12 traducidos con IA:** se quedan como **borrador** hasta que los revise el abogado. No bloquean el selector de idioma, pero sí el lanzamiento en esos idiomas.
- **8 · Comodines en el glosario:** «Boosters» / «Reforços».
- **9 · `firebase-appcheck-debug` en release:** se excluye del AAB de release antes del primer AAB de producción. La compuerta comprueba que no esté. Solo dev la conserva: el ensayo (`-afRehearsal`) es variante release de Gradle y usa Play Integrity, así que tampoco la lleva. En el registro inicial de este punto el integrador escribió por error «dev y el ensayo la conservan»; lo corrigió al integrar `tanda5/appcheck-release`, sin cambiar la decisión del dueño («excluirla del release»).
- **10 · Q-B9-13, daltonismo:** los pares 7-10 (protanopía) y 5-14 (deuteranopía) entran en los pares de riesgo de 07 §3 y del glosario §9, y el generador los limita. Si cambia algún tablero publicado, se vuelve a generar con `level-qa` en verde. **Matizado por Q-B9-13 bis** (añadido siguiente).
- **11 · Desbordes al 130 %:** se lanza ya la tarea de las filas del Mercado, la pestaña «Sin anuncios» y el código de cuenta de Ajustes, en los tres idiomas.
- **11, consecuencia (integración de `tanda5/fx-130`):** Ajustes muestra el código de cuenta en grupos de 4 con guiones de Crockford. El valor guardado no lleva guiones y la app nunca envía el código. La futura web de borrado (D-035) debe quitar guiones y espacios y pasar a mayúsculas antes de llamar a `/v1/account/delete-request`, como ya exige `infra/contracts/v1/common.schema.json`. Recomendada aplicada: requisito con su test cuando se haga la web, sin tocar la Lambda.
- **12 · Tanda de pulido**, con los pendientes ya diagnosticados:
  - el contorno de Alto contraste (Q-B9-6);
  - el segundo aviso tras el hito 3 (UI de los avisos, (b));
  - la Marea con el factor de Reducir movimiento (Q-B9-12);
  - el nombre de la mecánica en la etiqueta de la cuia (Q-B9-14);
  - TalkBack en la lista de Ajustes que se desplaza (Q-B9-17);
  - los decimales de CLP, JPY, KWD y BHD en D-025 (Q-IAP-3).

### D-033, añadido del 2026-10-22: Q-B9-13 bis, cómo entran los pares del simulador de daltonismo

- **Estado:** el dueño eligió el punto 10 con la recomendada («Pendientes 2 a 12: todas con la recomendada»). El carril `tanda5/daltonismo-pares` encontró que aplicarlo al pie de la letra choca con el GDD. El integrador aplicó en cada paso la recomendada del carril, por la regla del dueño «si tienen recomendada, vamos con esa». Integrado con la revisión APROBADO. El dueño puede cambiarlo.
- **El problema:**
  - Contar 7-10 y 5-14 en `risk_pairs` y `max_pares_riesgo` hace imposibles los K que fija el GDD en 11-80 y en las bandas B1 y B2 del Reto (03 §4.5 y §16).
  - Desde el 201 pasaría además el tope de 10 del esquema.
  - Obligaría a regenerar alrededor del 90 % de lo publicado.
- **Decisión, (b) y (A):**
  - Los dos pares entran en 07 §3 y en el glosario §9 como **pares del simulador**, que no cuentan en el tope.
  - El generador evita que queden juntos en una cuia solo si una permutación alternativa de tintes (hasta 50, con la subsemilla 5) **mide exactamente igual**: el bot ×40, la ventana, el par, las trampas, la ramificación, la D y el alivio. Si no lo consigue, se queda la asignación de siempre.
  - `level-qa` da `QA-CVD-ADJ` y `QA-DAILY-CVD-ADJ` como fallo en el tablero base. Como aviso, en las variantes y en lo inevitable.
- **Resultado:**
  - Al regenerar solo cambian tintes en 23 packs: 0 cambios de forma, de `bot_fail`, de candidato o de D. El tutorial no cambia.
  - Contactos que se eliminan: los 14 del mundo 2; 753 de los 1.102 del Reto del río (quedan 349); en los mundos 3-9, solo entre 3 y 7 tableros por pack.
  - El resto se queda como aviso y lo distinguen los patrones y el icono de fruta (07 §3, reglas 1 y 2).
  - El detalle está en `docs/dev/accesibilidad.md` §2.6.
- **Descartado:** hacer el bot independiente del color (cambio del núcleo). Quitaría casi todos los contactos, pero cambiaría `bot_fail` y la curva de todos los packs. Solo si el dueño lo pide.
- **Del integrador:** el carril tocaba tests de Unity y `docs/gdd/**` porque el encargo lo pedía (GDD y tests).

### D-025, añadido del 2026-10-22: decimales de las monedas fuera de 05 §4.2 (Q-IAP-3)

- **Fecha:** 2026-10-22. Registrado por la tanda de pulido (D-033, punto 12) con la recomendada de Q-IAP-3 (D-040): fijar en D-025 los decimales de las monedas sin céntimos y de las de tres decimales. No cambia el texto de D-025: lo completa.
- **Decisión:** el punto 2 de D-025 («los decimales de la moneda: 0 en COP y MXN; 2 en el resto») suma, para las monedas que la tienda real puede dar fuera de las cinco de 05 §4.2 (`LocalPrice` admite cualquier código ISO 4217, Q-IAP-3):
  - **0 decimales** en **CLP** y **JPY** (monedas sin céntimos): «CLP 1.990», «JPY 160»;
  - **3 decimales** en **KWD** y **BHD**: «KWD 1.250» con el teléfono en en-US;
  - el resto sigue con 2.
- **Código:** `LocPrice.DecimalsOf` (y, por ella, `LocalPrice`, `MinorUnitsFromMicros` y `PlayBillingClient.TryLocalPrice`), con tests en `LocPriceTests`. Los importes llegan en micros de Play Billing y se redondean «la mitad hacia fuera» a esos decimales.
- **Abierto [P]:** otras monedas sin céntimos o con tres decimales de ISO 4217 (por ejemplo KRW, VND, OMR, JOD, TND) siguen con 2 hasta que se decida; solo importan si la cuenta de Play del jugador está en una de ellas.

### D-033, añadido del 2026-10-22: Q-B9-17 hecha (TalkBack en las listas que se desplazan)

- **Estado:** integrada con `tanda5/talkback-scroll`, con la revisión APROBADO. Sustituye al «Pendiente» de Q-B9-17 del añadido de B9.
- **Qué hace:** las filas de una lista que se desplaza se publican todas al lector, también las recortadas, con la acción `scrolled`, y el visor se desplaza hasta la fila enfocada. Es la (a), más la primera mitad de la (b): publicar las filas recortadas.
- **Alcance:** vale para toda `ScrollRect` vertical: Ajustes, el Mercado, la ficha de frutas y el Muestrario del Mapa. Las dos últimas no se pidieron, pero se dejan [P] por coherencia.
- **Pendiente:** probarlo en el teléfono con TalkBack. Queda por ver el marco de 1 px de las filas recortadas y el orden de recorrido.

### D-033, añadido del 2026-10-23: preguntas de los arreglos de la prueba en el teléfono del 2026-10-23, con la regla «si tienen recomendada, vamos con esa»

- **Estado:** aplicadas con su recomendada, por la regla del dueño. Integradas con `tanda5/fx-telefono-1023`, con la revisión APROBADO tras dos bloqueantes corregidos. El dueño puede cambiarlas.
- **T2-Q6, solo para el bloque `map.*` de `facts.csv`:**
  - las traducciones del Mapa se guardan aunque el español siga en `pendiente_asesor`, y solo se ven en desarrollo;
  - release sigue sin mostrarlas hasta `aprobado_es` + `revisado_nativo` (08 §4.7);
  - frutas y fauna siguen retenidas como antes;
  - las 313 filas que faltaban, traducidas con IA;
  - «Manaos» y «Leticia» sin traducir, como los topónimos del glosario §8.
- **Placas de la vista Cuenca al 130 %:** una placa que pisa otra o un país prueba otro sitio y luego dos líneas; si aun así no cabe, se oculta, dando prioridad a los mundos más cercanos. La del mundo siguiente nunca se oculta. Al principio del juego se ocultan entre 5 y 7 placas de los mundos 7 a 9.
- **Extra de desarrollo `--ez af_fake_store true`:**
  - solo con `DEVELOPMENT_BUILD`; en release no se lee;
  - pone la tienda simulada y el backend falso en toda la sesión, así que no sirve para probar App Check ni el borrado real;
  - documentado en `docs/dev/prototipo-v0.md`.
- **Regla nueva de `tools/loc/apply_translations.py`:** una traducción con menos U+00A0 entre cifra o marcador y su unidad que el es-419 es un conflicto. La excepción es el «%», que en en y pt va pegado.

### D-040, añadido del 2026-10-23: preguntas abiertas del plugin propio de Play Billing (Q-BILL-2 a Q-BILL-5)

- **Estado:** integradas con `spark/t5-billing`, con la revisión APROBADO tras corregir el GDD. La recomendada va aplicada donde la hay. Detalle en `docs/dev/iap.md` §4.1.
- **Q-BILL-2 · Registros de diagnóstico de la biblioteca a Google:** se declaran en la fila «Google Play Billing» de 08 §13.3 [E]. Pendiente del abogado.
- **Q-BILL-3 · Una sola oferta por producto:** la configura el dueño en Play Console.
- **Q-BILL-4 · Compras de varias unidades:** el plugin salta las que traen `getQuantity() != 1`. No se acreditan ni se reconocen, y Play las reembolsa. El dueño no activa las varias unidades en Play Console.
- **Q-BILL-5 · `sec_purchase_deferred_ack_h`:** la recomendada es bajarlo de 72 a 48 h en 08 §9 y regenerar los defaults. Con 72 h, el reconocimiento local puede llegar tarde, en el mismo plazo en que Play reembolsa. Se aplica en una tarea aparte (regla «si tienen recomendada, vamos con esa»). **Aplicada en `tanda5/bill-restos`:** 08 §9 dice 48, los defaults regenerados y `VerificationQueueConfig` compila 48; el rango sigue en 48-72.
- **Sin probar:** la compra real por JNI en un teléfono, con el modo de ensayo de `iap.md` §3. Hace falta antes de cualquier release, con los productos `af_*` dados de alta en Play Console (D-034).

- **Del integrador (2026-10-23), al integrar `tanda5/bill-restos`:**
  - Q-BILL-5 aplicada: default de 48 h y rango de 48-72 (Remote Config puede volver a subirlo).
  - Si la consulta de productos más reciente falla, el plugin se queda sin productos, como C#.
  - Pendiente menor: en `AfBilling.java`, el fallo síncrono de `queryProductDetailsAsync` no vacía el mapa, mientras que el asíncrono sí. Sin riesgo de dinero, porque C# se queda sin Play al recibir ERROR.

## D-041 · Parallext, LLC como desarrollador y responsable; dominio tintalu.com

- **Fecha:** 2026-10-23. Registrada por el integrador.
- **Estado:** decidido por el dueño en el chat:
  - «listo ya tengo el dominio tintalu.com en cloudflare para que iniciemos la construcción de las pagina, politicas y demas datos. El desarrollador de Tintalu es Parallext.com»;
  - «dejamos los datos de parallext, LLC con los datos de ella».
- **Decisión:**
  - **Desarrollador y responsable:** el desarrollador público y el responsable del tratamiento en la política de privacidad, los términos, la página de borrado y la web es **Parallext, LLC** (parallext.com), con sus datos. Ya no es AUTOMATION AI S A S.
  - **Dominio:** `tintalu.com`, comprado por el dueño y gestionado en Cloudflare. La web se construye ya (`infra/web/site`) y se despliega en la URL de CloudFront de dev. Conectar el dominio es un paso aparte que decide el dueño: lo público con la marca espera al dictamen (D-034, Q-AWS-9) mientras el dueño no diga otra cosa.
- **Datos de Parallext, LLC que faltan** (van como `[[DATO DEL DUEÑO]]` hasta que lleguen):
  - estado de constitución;
  - domicilio;
  - correo de contacto y de privacidad;
  - persona o cargo de protección de datos (LGPD);
  - representante en la UE o en Brasil, si el abogado lo pide.
- **Consecuencias que revisa el dueño:**
  - **Cuenta de Google Play:** está a nombre de AUTOMATION AI S A S (organización, Colombia). Si el desarrollador y vendedor es Parallext, LLC, la app debería publicarse desde una cuenta de Parallext, LLC: una cuenta nueva de organización o una transferencia de la app, que hoy es sencilla porque la app no existe (D-034). Lo decide el dueño, con el abogado si hace falta.
  - **Marca:** el titular previsto de la solicitud pasa a ser Parallext, LLC (EE. UU.). La vía de Madrid partiría de una solicitud base en la USPTO, no en Colombia. Encargo de Codex actualizado (`docs/dev/traspaso/codex-marca.md`).
  - **`applicationId` definitivo:** recomendado `com.parallext.tintalu`. Se fija solo tras el dictamen (regla dura 6).

### D-033, añadido del 2026-10-23: con la banda <13 ya guardada, el arranque no inicia ningún SDK

- **Estado:** integrado con `tanda5/menor13-arranque`, con revisión APROBADO tras corregir la privacidad de la web. Lo hallado al redactar la política: con `<13` guardado, Crashlytics se encendía un instante en cada arranque y Remote Config se descargaba.
- **Decisión** (recomendada del carril, por la regla del dueño):
  - el perfil se lee antes que cualquier SDK;
  - con `<13` guardado no hay Crashlytics a `true`, descarga de Remote Config (caché o defaults), Firebase en C#, nube, backend, tienda ni anuncios;
  - Crashlytics se apaga también por JNI y se borran los informes sin enviar;
  - el primer arranque, 13-17 y 18+ siguen igual.
- **Anotado** en 08 §2.3 y §13.1 #11, en la privacidad de la web y en `docs/dev/firebase-cliente.md` §4 bis.
- **Sin probar** en un teléfono con Firebase real.

## D-042 · Integración de AppLovin MAX (con UMP y adaptadores) y de Play Games (Saved Games)

- **Fecha:** 2026-10-23. Registrada por el integrador.
- **Estado:** aprobado por el dueño en el chat. A la propuesta «5. Anuncios: AppLovin MAX con UMP y sus adaptadores; 6. Guardado en la nube: Play Games 2.3.0», respondió: «2. Vamos con los dos.»
- **Decisión:** se integran, detrás de sus interfaces (`AF.Ads`, `AF.Save`/`ICloudSave`), con las implementaciones falsas en el editor y en los tests:
  - **AppLovin MAX Unity Plugin 8.6.6** (SDK Android 13.6.4), con la automatización de Google UMP y los adaptadores Google (AdMob), Unity Ads, Meta Audience Network y Mintegral, según el plan de instalación y 08 §7 #1-#2 y §13.1 #2. **Sin el plugin Google Mobile Ads.**
  - **Play Games plugin for Unity 2.3.0** (PGS v2), con inicio de sesión silencioso y Saved Games (08 §7 #5 y §11).
- **Condiciones** (regla dura 4):
  - las versiones exactas de los adaptadores, y cualquier dependencia que traigan, se anotan como añadido de D-042 con la aprobación del dueño antes de integrarlas;
  - **cada permiso Android nuevo se pregunta aparte.** Por ejemplo, `com.google.android.gms.permission.AD_ID`, que D-039 quitó «hasta MAX», o los de Privacy Sandbox. Ninguno entra sin un sí expreso;
  - con `<13`, nada de esto arranca (D-033, añadido del 2026-10-23);
  - con 13-17, anuncios no personalizados y contenido G (08 §2.3);
  - con el consentimiento de anuncios en «no», no hay personalización.
- **Bloqueos que no impiden integrar el código:**
  - las cuentas de AppLovin y AdMob, la clave del SDK de MAX y las unidades de anuncio son del dueño;
  - Play Games necesita la app en Play Console (D-034, a la espera del dictamen).

  Hasta entonces, los builds van con la implementación falsa o en modo de prueba, y las claves y los IDs reales se cargan fuera del repositorio.

### D-042, añadido del 2026-10-23: dependencias de Play Games 2.3.0

- **Estado:** aplicado con la recomendada del carril `tanda5/sdk-pgs`, por la regla del dueño «si tienen recomendada, vamos con esa». **El dueño puede cambiarlo.**
- **El paquete:** el plugin oficial solo se publica como `GooglePlayGamesPlugin-2.3.0.unitypackage` (sha256 `1b0ecc5e2fd5437e1c26863f63367f5c983452afc4eeae8a1e5dcaa07b9e7518`). Se versiona su carpeta `com.google.play.games/` tal cual, como un `.tgz` en `Packages/playgames/` (Git LFS), sin el EDM4U 1.2.182 que trae, porque ya está el 1.2.189 de Firebase.
- **Dependencias:**
  - **`com.google.android.gms:play-services-games-v2:22.1.0`** entra como dependencia obligatoria del plugin aprobado. No declara permisos. Añade `PlayGamesInitProvider`, dos actividades y un bloque `<queries>`.
  - **`com.google.android.gms:play-services-nearby:18.5.0` no entra.** Es Nearby Connections, que el juego no usa, y trae un servicio exportado de Exposure Notifications. Se excluye en `mainTemplate.gradle`, fuera de la zona de EDM4U, y se comprueba que no aparece en el manifiesto.
- **El blob de Saved Games:** se queda en los 800 KB de 08 §11 (`CloudSaveNames.MaxBlobBytes`). Queda como pregunta la discrepancia con 08 §7 #5, que habla de 3 MB por partida y 800 KB de portada.

### D-042, añadido del 2026-10-23: MAX integrado sin adaptadores; lo que espera al dueño

- **Estado:** integrado en `tanda5/sdk-max` por el integrador de SDK, **sin adaptadores**, pendiente de revisión. Nota: `docs/dev/anuncios-max.md`.
- **Qué entra:** AppLovin MAX Unity Plugin **8.6.6** (`com.applovin.mediation.ads`, SDK Android `com.applovin:applovin-sdk:13.6.4`) del registro UPM oficial de AppLovin, `.tgz` en `Packages/applovin/` con Git LFS, SHA-256 `908332af8989b8a239d1c470daf70f0925ea2cd0f72f94748bbbc4c7d228d1d4`; y `com.google.android.ump:user-messaging-platform:4.0.0`, que el plugin añade al encender su automatización de UMP (la que aprobó D-042). Quality Service apagado. Clave del SDK y unidades fuera del repositorio. Sin el plugin Google Mobile Ads.
- **Preguntas, con la recomendada ya aplicada:**
  - **Q-MAX-1 · Permisos que declara `applovin-sdk`:** `com.google.android.gms.permission.AD_ID`, `com.android.vending.CHECK_LICENSE` y `android.permission.ACCESS_WIFI_STATE`. Aplicado: (a) los tres quitados con `tools:node="remove"`, como D-039. Alternativas: (b) aprobar `AD_ID` (mejor eCPM en Android 13+; Data Safety declara el ID de publicidad) y seguir sin los otros dos; (c) aprobar los tres.
  - **Q-MAX-2 · Proveedor `AppLovinInitProvider`:** corre al iniciar el proceso y lee el ID de publicidad y el App Set ID antes del consentimiento. Aplicado: (a) quitado; MAX solo arranca con `InitializeSdk` tras consentir. [NV en el teléfono]
  - **Q-MAX-3 · Adaptadores:** la lista con versiones de `docs/dev/anuncios-max.md` §7 (Google 25.5.0.0, Unity Ads 4.21.0.1, Meta 6.22.0.1, Mintegral 17.1.91.0). Aplicado: ninguno hasta el sí del dueño.
  - **Q-MAX-4 · URL del mensaje de UMP:** `https://tintalu.com/privacidad/` y `/terminos/` [P], aunque el dominio aún no es público (Q-AWS-9). Aplicado: esas.
  - **Q-MAX-5 · Concesión con SSV:** el canon (08 §10.2) concede provisional al recibir la recompensa y la confirma o revoca el servidor. Aplicado: `AdOutcome.Rewarded` con `OnAdReceivedRewardEvent` y `custom_data = placement|nonce`; registrar el nonce con `LocalRevocation.RegisterProvisional` queda para F3-4 (`unity-gameplay-dev`) cuando el SSV esté configurado en el panel de MAX.
  - **Q-MAX-6 · UMP con 13-17 en el EEE, el Reino Unido y Suiza:** la automatización enseña el mensaje de UMP a todos al iniciar MAX. Aplicado, por orden del integrador tras la revisión: **(b) con 13-17 y el perfil `ump`, MAX no se inicia** (sin anuncios), como recomendada provisional hasta que hablen el dueño y el abogado. Alternativa: (a) iniciarlo con `npa` y aceptar que UMP enseñe su mensaje.
- **Tras la revisión (mismo día):** `BuildDev` y `BuildRelease` se paran si `AppLovinSettings.asset` lleva una clave o el Quality Service encendido; un release sin configuración no tiene anuncios (`UnavailableAdService`); margen para la recompensa que llega tras el cierre y al volver de segundo plano; `SetUserId` de MAX es global y así se dice en la web y en Data Safety.
- **Pendiente del dueño:** cuenta de AppLovin y AdMob, clave y unidades (`Assets/_LocalSecrets/applovin-max.json` o secretos del CI), mensaje de UMP en AdMob, contenido máximo G en las consolas, URL S2S del SSV en MAX.

## D-043 · Las claves de Remote Config con punto pasan a guion bajo

- **Fecha:** 2026-10-23. Registrada por el integrador.
- **Estado:** aprobado por el dueño en el chat. A la propuesta de renombrar las 20 claves con punto, que Firebase no admite, respondió: «hagamos la correciones».
- **Decisión:** en 08 §9, en el cliente y en los defaults generados:
  - `mech_enabled.<id>` pasa a `mech_enabled_<id>`;
  - `mech_debut_level.<id>` pasa a `mech_debut_level_<id>`.

  Así entran en la plantilla de Remote Config y los interruptores de cada mecánica se pueden accionar desde la consola.
- **Sin cambio de comportamiento:** los valores siguen siendo los mismos, y un valor remoto con el nombre viejo no existe, porque nunca se publicó.
- **Encargo:** carril `tanda5/rc-plantilla` (Q-RC-1).

## D-044 · Publicar ya la web en tintalu.com; Codex publica, completa los datos y lleva el logo y la marca

- **Fecha:** 2026-10-23. Registrada por el integrador.
- **Estado:** decidido por el dueño en el chat: «la pagina web y el resto del contenido ya esta, esta publicada? sino pasala a codex que se encargue de publicar y completar todo con respecto a eso, como tambien el logo de la app y lo que falta con la marca».
- **Decisión:**
  - **La web se publica ahora en `tintalu.com`**, sin esperar al dictamen de marca, por decisión expresa del dueño. Esto levanta para la web la espera de D-034, Q-AWS-9 y D-041. **Play Console** y el **`applicationId` definitivo** siguen esperando al dictamen.
  - **DNS:** se usa la opción A de `infra/README.md`. El DNS se queda en Cloudflare, con certificado de ACM en us-east-1 validado por CNAME.
  - **Prod:** la web pública va en el entorno de prod, con los stacks de web y CDN.
  - **Reparto del trabajo:**
    - el cambio de CDK lo hace el equipo de Claude (carril `tanda5/web-dominio`);
    - el despliegue, el DNS y los datos del dueño los hace Codex con el dueño (`docs/dev/traspaso/codex-web-marca.md`).
  - **Logo y marca:** Codex prepara el encargo del icono (07 §10.1) y lo que falta de la marca: completar la investigación en INPI y WIPO, y el paquete para el abogado.
- **Riesgo aceptado por el dueño:** usar «Tintalú» en público antes del dictamen.

## D-045 · Identidad oficial v2 de Tintalú: «El río que se tiñe»

- **Fecha:** 2026-10-23. Registrada por el integrador.
- **Estado:** decidido por el dueño en el chat. Pidió una versión propia: «la idea era que fuera el rio amazonas con su color caracteristico y la cuia vertia y se pinta rio abajo del color que vierte». Después corrigió dos cosas: la cuia «cierra en corte lineal» y el líquido no podía verterse «infinito». Al final eligió: «Sí, la B sustituye a la v1 de Codex».
- **Decisión:**
  - **La identidad oficial es la v2** (`docs/art/marca-oficial/v2/`, propuesta B). **Sustituye a la v1 de Codex** (`docs/art/marca-oficial/v1/` en la rama `codex/web-marca`), que queda como historial y no se integra como oficial.
  - **El concepto:** un río del Amazonas en su agua blanca (#B08A5A, glosario §9) corre de izquierda a derecha. Una cuia de vidrio vierte açaí (#6A2C91), y el río se tiñe **desde el impacto, río abajo**; río arriba sigue en su color. Fondo azul petróleo con la selva de la orilla y dos palmeras de açaí en silueta. Sin texto dentro del icono.
  - **Fiel al juego:**
    - la silueta de la cuia sale de `CuiaGeometry.cs` (fondo plano, pie y borde de fibra);
    - la inclinación y el líquido siguen D-006: quedan 2,6 de 3 dosis, el líquido queda horizontal y el chorro es finito y se adelgaza al caer;
    - las dosis llevan sus patrones de daltonismo;
    - el logo con el nombre usa Baloo 2 ExtraBold, la tipografía del juego.
  - **Piezas:**
    - el icono maestro: 1024 y Play 512;
    - Android: adaptativo (fondo y primer plano a 432 px, con el primer plano dentro de la zona segura), monocromo para el icono temático y clásico de 48 a 192;
    - el logo horizontal y vertical sobre fondo claro y oscuro.

    Todo se genera con `generar.py`, `exportar.py` y `hoja.py`, solo con la biblioteca estándar y Chrome sin interfaz.
  - **07 §10.1** se actualiza: el concepto A elegido cambia el fondo (de oliva a petróleo) y el río (de banda blanca al agua blanca que se tiñe río abajo).
- **Procedencia:** vectorial, generado por código por el agente integrador, no por IA de imagen. El acabado final puede encargarse al artista sobre esta composición.
- **Pendiente:**
  - las apariencias oscura y tintada de iOS y su catálogo de Xcode;
  - integrar el icono en Unity (Player Settings, adaptativo y monocromo) y en la web (favicon y logo);
  - probarlo en un teléfono.

  Codex lleva la web y el icono de la ficha (`docs/dev/traspaso/codex-web-marca.md`, «Actualización del 23-10-2026»).
- **Nota:** la estructura de publicación con Parallext, LLC y AUTOMATION AI S.A.S, que se propuso en el chat, todavía no está decidida. Cuando se decida, se registrará con el siguiente número libre.

## D-046 · Liga del río en la 1.0 (País y Global), controles directos de audio y sitio para la tropa con 3 filas

- **Fecha:** 2026-10-23. Registrada por el integrador.
- **Estado:** decidido por el dueño en el chat. Primero pidió: «unos controles directos para la musica y el sonido para apagarlos o prenderlo», «cuando tenemos muchas cuias quedan muy arriba y grandes lo que hace que no se vean los monitos, podemos dejarlas un poquito mas pequeñas o aprovechar un poco mejor la parte de abajo» y «una clasificación global y por paises que sepa como va uno». Después, a la propuesta del integrador: «agreguemos la liga como recomendas inicialmente pero pasemosla para esta primera versión y los demas recomendado».
- **Decisión:**
  1. **Liga del río en la 1.0, recortada:**
     - solo las vistas **Global** y **País**, con puntos semanales: el top 100, la posición exacta si el jugador está en él y, si no, el percentil;
     - **seudónimo automático** (animal y 4 cifras), sin texto libre;
     - el **país** sale de CloudFront o, si no, de la región del teléfono;
     - desbloqueo en el 41;
     - interruptor `live_league_enabled`, apagado hasta que haya backend en prod;
     - backend en **AWS** (D-035), sin Unity Leaderboards ni Cloud Code.

     La cubeta de 50, los tiers y los premios se quedan en la 1.2. Especificación y contrato en `docs/dev/liga.md`; cambia 06 §6, 08 §8.2, §9 y §11, 09 y GDD.md §2.
  2. **Música y Sonido con un toque:**
     - dos botones con icono (nota y altavoz) arriba en el menú de pausa y en el inicio, junto al engranaje de Ajustes;
     - guardan en el mismo sitio que las filas de Ajustes (`settings.music` y `settings.sound`), así que siempre coinciden;
     - la Vibración se queda solo en Ajustes;
     - nada en la barra de juego.

     Cambia 02 §5 (contenido de la pausa) y 07 §9.4.
  3. **Sitio para la tropa con 3 filas:**
     - el suelo del tablero baja unos 20 dp, aprovechando el margen sobre la bandeja y dejando unos 12 dp hasta ella;
     - con 3 filas las cuias encogen lo justo, unos 44 dp en lugar de 47 a 360 × 780, para abrir una franja de unos 44 dp sobre el panel;
     - la tropa sale en la rama en todos los niveles de 3 filas, en el primer intento, en lugar de la tropa lateral 1 de cada 3 (D-021) y del dosel;
     - los tableros de 10 cuias o menos no cambian.

     Matiza D-006 (tamaño con 3 filas), D-021 (la tropa lateral pasa a respaldo) y la excepción de D-028 (la fila 1 ya no entra en el HUD). Cambia 02 §5 y 07 §7.4.

     **Añadido del 2026-10-24 (respuestas del dueño en el chat a las preguntas del carril 2 del Spark, `spark/t6-tropa-3filas`):**
     - **Q-T6T-3 → «Bajar y mover avisos»:** con 3 filas el suelo baja 20 dp [P] y el panel queda a 11,5 dp [P] de la bandeja; las burbujas «Te faltan {n}» y el aviso del tablero que ya no caben debajo van a la franja (y, por extensión del integrador, el globo de Pista: aplicación de Q-T6T-3 [P], no dicho literalmente por el dueño) de la tropa, sobre el panel (02 §5).
     - **Q-T6T-4 → «Franja de 56 dp»:** la franja de la rama mide 56 dp [P] (con 44, el tamaño de 07 §6.4 dejaba la tropa rozar la fila del HUD); la tropa, a su tamaño, ya no entra en ella.
     - **Cifras medidas a 360 × 780:** franja de 56 dp, panel en y = 144-648,5 (11,5 dp sobre la bandeja), cuia de 41,6 × 116,5 dp con 11-15 cuias (antes 46,9 × 131,3). Donde la franja no cabe sin bajar de 28 dp (16:9 (360 × 640 dp; con 3 filas la franja cabe desde unos 666 dp de alto) y la Cuia alta de 6 en lienzos de menos de 750 dp de alto), el tablero de 3 filas es el de antes.
     - Quedan para el dueño, con la recomendada aplicada: Q-T6T-2, Q-T6T-6, Q-T6T-7 y el riesgo Q-T6T-5 (GDD.md §6.3, pregunta 68).
- **Reparto (pedido por el dueño: «manda en paralelo lo mas que se pueda», «encarga a codex algo grande», «envia a spark algun carril o dos para solucionar el espacio»):**
  - **Spark:** los puntos 2 y 3 (`docs/dev/traspaso/spark-audio-tablero.md`);
  - **Codex:** el backend de la Liga, su despliegue en dev con el dueño y los textos legales (`docs/dev/traspaso/codex-liga.md`);
  - **escritorio:** el cliente de la Liga y la integración.

### D-046, añadido del 2026-10-24: el Mercado sin precios de Play (prueba en el teléfono del 23, observación 1)

- **Estado:** aprobado por el dueño en el chat. Su respuesta, en inglés: «I approve the change to Scripts/IAP/: hide the Market without Play prices and show the notice. Keep the existing text and leave the rows without a price for another task.» Cubre el cambio en `Assets/_Project/Scripts/IAP/` que pide `.claude/rules/sdk.md`.
- **Decisión:**
  - El Mercado y todo lo que vende se comportan como «tienda no disponible» mientras ningún SKU tenga precio de Play.
  - Si una compra acaba en `StoreUnavailable`, sale el aviso que ya existe, `market_store_unavailable`.
  - Las filas sin precio de Play, cuando otros SKU sí lo tienen, quedan para otra tarea.
- **Carril:** `tanda6/fx-telefono-max`, aprobado por el code-reviewer.

### D-046, añadido del 2026-10-24: el cliente de la Liga en `Scripts/Backend/`

- **Estado:** aprobado por el dueño en el chat: «Apruebo el cambio en Scripts/Backend/ del cliente de la Liga (los endpoints /v1/league con el autorizador de siempre). Intégralo.» Cubre el visto bueno que pide `.claude/rules/sdk.md`.
- **Lo que entra:** el cliente de la Liga del río de la 1.0 (`tanda6/liga-cliente`, aprobado por el code-reviewer en dos vueltas) contra el backend de Codex (`codex/liga`, ya integrado y desplegado en dev). Con las decisiones del integrador:
  - desbloqueo fijo en 41 (`live_league_unlock_level` 41-41 en 08 §9, igual que el servidor);
  - «Otro país» para un país sin fila;
  - la Liga bloqueada sin la edad contestada y con <13.

  El integrador acepta las ediciones del GDD de la rama (06 §6, 08 §2.2, §4.3, §8.2 y §9, 09, GDD.md §2 y glosario), que siguen D-046 y `docs/dev/liga.md`.

## D-047 · Publicar sin dictamen ni registro de marca; el registro, cuando haya resultados

- **Fecha:** 2026-10-25. Registrada por el integrador.
- **Estado:** decidido por el dueño en el chat: «Vamos a irnos por ahora sin registro, ya buscamos suficiente y no encontramos nada sobre esto, mas sin embargo si no va bien es lo primero que haremos, porque no tiene sentido gastar mas dinero sino obtenemos buenos resultados.»
- **Decisión:**
  - No se contrata el dictamen del abogado de marcas ni se presenta la solicitud de registro por ahora. El dueño da por suficiente la búsqueda propia (`docs/legal/marca.md`, `docs/legal/nombres-candidatos.md`) y **acepta el riesgo** de tener que cambiar el nombre visible más adelante.
  - **Retira la condición previa del dictamen** de 08 §13.1 #1 y de D-034: el dueño puede crear la app en Play Console, fijar el `applicationId` definitivo, crear la ficha y conectar `tintalu.com` a la web pública cuando quiera.
  - **El `applicationId` definitivo** sigue sin la marca en el código y es inmutable tras la primera subida. **`com.parallext.tintalu`**, confirmado por el dueño el mismo día: «Sí, el applicationId definitivo es com.parallext.tintalu». Si un día cambia el nombre visible, el `applicationId` se queda.
  - **El registro de la marca** (clases 9 y 41, y Brasil, México y EE. UU. según `marca.md` §3 y §5) es el primer gasto en cuanto el juego dé buenos resultados, o antes si aparece un conflicto con el nombre.
  - Sigue igual: los agentes no suben nada a Google Play con ningún `applicationId` (regla dura 6, regla operativa); publica el dueño. `com.amazonfill.dev` sigue siendo provisional y el CI no lo deja llegar a release.
- **Qué cambia:** regla dura 6 y estado de la marca en `CLAUDE.md`; 08 §13.1 #1 (nota de D-047); `docs/dev/pendientes-del-dueno.md` §1 y orden sugerido; `docs/dev/release.md` (la variable `AF_RELEASE_APPLICATION_ID` y Play App Signing ya no esperan al dictamen). `docs/legal/paquete-abogado.md` queda guardado para el registro.
- **Cómo revertir:** con una decisión nueva del dueño.

### D-041, añadido del 2026-10-25: datos de Parallext, LLC y correos de tintalu.com

- **Estado:** datos dados por el dueño en el chat (punto 2 de las acciones humanas pendientes).
- **Datos:**
  - **Responsable:** Parallext, LLC, sociedad constituida en **Wyoming** (EE. UU.).
  - **Domicilio:** 7345 W Sand Lake Rd, Ste 210, Office 2812, Orlando, FL 32819, Estados Unidos.
  - **Correos:** soporte `support@tintalu.com` y privacidad `privacy@tintalu.com`, ya activos con Email Routing de Cloudflare. Sustituyen a `support@parallext.com` en la web, los textos legales y la ficha.
  - **Encargado de protección de datos (LGPD, Brasil):** Nir Levin Bermúdez Perea, con `privacy@tintalu.com`. El dueño acepta que su nombre salga en la política pública.
  - **Representante:** el dueño pidió «colocarlo si no hay problema». En Brasil la LGPD solo exige el encargado (ya es él). En la UE el representante del art. 27 del RGPD tiene que estar establecido en la UE, así que no puede ser él: **sin representante en la UE** mientras el juego no se dirija a la UE. `[[ABOGADO: revisar]]` cuando se contrate.
  - **Dominio:** conectar `tintalu.com` a la web de prod ya (D-044, `infra/README.md` «Conectar `tintalu.com`»); lo hace Codex con el dueño, con su sí a cada despliegue y cambio de DNS.

## D-048 · Parallext desarrolla y responde de los datos; AUTOMATION AI S.A.S publica en Google Play y cobra

- **Fecha:** 2026-10-25. Registrada por el integrador.
- **Estado:** decidido por el dueño en el chat: «Sí: Parallext desarrolla y es responsable de los datos; AUTOMATION AI S.A.S publica en Play y cobra. Regístralo.» Lo había adelantado a Codex el 23-10 (`docs/legal/estructura-publicacion.md` §1 y §3).
- **Decisión:**
  - **PARALLEXT, LLC** (Wyoming; D-041 y su añadido del 25-10): estudio, desarrollador público y **responsable del tratamiento** de los datos del juego (Firebase, AWS, soporte y borrados).
  - **AUTOMATION AI S.A.S** (Bogotá, Colombia): **editora en Google Play** con la cuenta de desarrollador ya verificada (2026-10-20) y **beneficiaria de los cobros** de Play.
  - La web, la política y los términos nombran a las dos con sus funciones (`estructura-publicacion.md` §4); el test `infra/test/web/site.test.ts` exige que AUTOMATION AI solo salga como editora y junto a Parallext, LLC.
- **Pendiente para el abogado** (`[[ABOGADO]]`): la licencia de Parallext a la editora (el DDA §11.1 de Google exige que quien publica tenga derecho a monetizar por cuenta propia), los datos a los que accede la editora y su papel en la política.
- **Matiza D-041** («ya no es AUTOMATION AI S A S»): sigue valiendo para el responsable de los datos y el desarrollador, no para la editora en Play.
- **Cómo revertir:** con una decisión nueva del dueño.

### D-048, añadido del 2026-10-25: AdMob cobra la S.A.S y AppLovin, Parallext

- **Estado:** hecho por el dueño en AdMob: «en admob asocie el perfil de pagos de play google osea la S.A.S».
- **Qué cambia:** los ingresos de anuncios de **AdMob** los cobra **AUTOMATION AI S.A.S**, con el mismo perfil de pagos de Google que Play, como los de Play. La cuenta de AdMob (App ID `ca-app-pub-2916158449453694~9925643748`, editor `pub-2916158449453694`) queda configurada como dice `docs/dev/anuncios-max.md` §10.
- **AppLovin MAX, a nombre de Parallext, LLC** (el dueño, en el chat: «parallext»):
  - la cuenta de AppLovin y lo que pague AppLovin (su red y las que liquide por ella) los cobra **Parallext, LLC**;
  - lo de AdMob sigue yendo a la S.A.S, porque con MAX cada red paga a su propia cuenta;
  - necesita una cuenta bancaria de la LLC en EE. UU.;
  - el formulario fiscal (W-9 de la LLC o W-8BEN del dueño, si la LLC es de un solo socio extranjero y no se considera entidad aparte) lo confirma el contador **antes** de rellenarlo.
- **Para el contador y el abogado** (`[[ABOGADO]]`):
  - la retención de EE. UU. sobre lo que paguen las redes a una empresa colombiana (Colombia no tiene convenio fiscal con EE. UU.);
  - que la licencia de Parallext a la editora (pendiente de arriba) cubra también los ingresos de anuncios, con el reparto: AdMob a la S.A.S y AppLovin a Parallext;
  - las declaraciones anuales de la LLC (formulario 5472), ya con ingresos.
- Parallext sigue siendo la responsable de los datos (D-048); el consentimiento de UMP y la lista de socios de AdMob se configuraron sin cambio por esto.

### D-044, añadido del 2026-10-25: la web pública en tintalu.com, desplegada

- **Estado:** hecho por el escritorio con la sesión de AWS del dueño y su sí al `cdk diff` («Sí: haz el bootstrap de CDK en sa-east-1 y us-east-1 y despliega AfProdGlobal y AfProdWeb con ese diff.»). El dueño creó los registros en Cloudflare.
- **Qué hay:** bootstrap de CDK en prod (sa-east-1 y us-east-1); `AfProdGlobal` (certificado de ACM de `tintalu.com` y `www`, emitido; aviso de presupuesto de 50 USD/mes a `support@tintalu.com`) y `AfProdWeb` (S3 + CloudFront, `-c webOnly=true`), con los contextos de `infra/README.md` «Conectar `tintalu.com`» y `alertEmail=support@tintalu.com`.
- **DNS en Cloudflare, todo «Solo DNS»:** los dos CNAME de validación de ACM (se quedan: renuevan el certificado), `tintalu.com` y `www` en CNAME a la distribución de CloudFront, y los MX, SPF y DKIM de Email Routing, que no se tocan.
- **Comprobado el 2026-10-25:** `https://tintalu.com` y `https://www.tintalu.com` dan 200 en la portada, la privacidad, los términos y el borrado de cuenta; el certificado cubre los dos nombres; HTTP redirige a HTTPS (301); HSTS, CSP y `X-Frame-Options` activas.
- **Siguiente despliegue de la web:** el mismo `cdk deploy AfProdGlobal AfProdWeb` con los mismos contextos (sin `-c domain` CloudFront perdería el dominio).

## D-049 · Anuncios con AdMob directo (Google Mobile Ads) en lugar de AppLovin MAX, que no acepta cuentas nuevas

- **Fecha:** 2026-10-25. Registrada por el integrador.
- **Estado:** decidido por el dueño en el chat: «si vamos con la opción 1» y, al plan de redes, «si».
- **Motivo:** al registrarse (a nombre de Parallext, LLC; añadido de D-048), AppLovin respondió: «We're currently not accepting new publisher accounts». Sin cuenta no hay clave del SDK ni unidades, así que MAX (D-042) no puede servir anuncios.
- **Decisión:**
  1. **Mediador: AdMob**, con el plugin **Google Mobile Ads para Unity**. Entra en lugar de MAX y retira, para los anuncios, el «sin plugin GMA» del plan de instalación y de 08 §7 #2.
     - La cuenta de AdMob ya está configurada: App ID, bloques, mensaje del RGPD, mensaje de EE. UU. y clasificación G (`anuncios-max.md` §10).
     - Cobra AUTOMATION AI S.A.S (añadido de D-048).
  2. **Consentimiento:** el UMP de Google, el mismo que ya se pedía con MAX. Sigue sin arrancar nada de anuncios antes de la edad y el consentimiento, y nada con <13 (08 §2.3; D-033 B12).
  3. **Recompensas verificadas:** el SSV de AdMob contra la API de AWS (`/v1/ssv`, D-035), en lugar del S2S de MAX.
  4. **Redes adicionales, como socios de mediación de AdMob,** una por una, cada una con su cuenta y su adaptador:
     - **Unity Ads**, la primera: el dueño crea la cuenta ya;
     - **Meta Audience Network** y **Mintegral**, tras publicar, porque piden la app en la tienda o revisión manual;
     - las demás, según los ingresos.
  5. **MAX queda guardado** detrás de la interfaz de `AF.Ads`, sin servir anuncios. Si AppLovin abre cuentas, el dueño decide si volver a MAX o añadir AppLovin como red dentro de AdMob. El dueño puede volver a pedir el alta a AppLovin, sin depender de ello.
- **Lo que sigue necesitando la aprobación del dueño** (regla 4):
  - la **versión exacta** del plugin de Google Mobile Ads, que propone el carril de integración con lo que trae por Gradle;
  - cada adaptador de red, con su versión;
  - los permisos `AD_ID` y `ACCESS_WIFI_STATE` (y `CHECK_LICENSE`, que se recomienda dejar fuera), pendientes de `anuncios-max.md` §8.
- **Pierde:** la demanda propia de AppLovin mientras no haya cuenta, sin cifra propia [E].
- **Toca después** (el carril de integración):
  - 08 §2.2, §7 #2, §10.2 y §13.3;
  - `anuncios-max.md`, que pasa a describir AdMob;
  - Data Safety;
  - la política (Google como socio de anuncios en lugar de AppLovin);
  - `app-ads.txt`, con la línea de AdMob y luego la de cada red.
- **Cómo revertir:** con una decisión nueva del dueño, por ejemplo si AppLovin acepta la cuenta.

### D-049, añadido del 2026-10-25: aprobada la versión de Google Mobile Ads y sus permisos

- **Estado:** aprobado por el dueño en el chat, sobre `docs/dev/anuncios-gma-propuesta.md`: «Apruebo Google Mobile Ads 11.5.0 con play-services-ads 25.4.0, UMP 4.0.0, work-runtime 2.7.0 y hsdp 2.0.1; AD_ID activado; ACCESS_WIFI_STATE, CHECK_LICENSE, FOREGROUND_SERVICE, RECEIVE_BOOT_COMPLETED y ACCESS_ADSERVICES_TOPICS fuera; MobileAdsInitProvider se queda y MAX sin su UMP. Lanza el carril.»
- **Aprobado:**
  - el plugin `com.google.ads.mobile` **11.5.0** de OpenUPM (SHA-256 `54aa0d8e…cbb13b`), con `file:` en `Packages/`;
  - `play-services-ads` **25.4.0**, UMP **4.0.0**, `androidx.work:work-runtime` **2.7.0** y `com.google.android.play:hsdp` **2.0.1**, con las demás dependencias de primer nivel de la propuesta, en las versiones que resuelva Gradle;
  - el permiso **`AD_ID`**, activado (se declara en Play Console y Data Safety);
  - `MobileAdsInitProvider` se queda (solo valida el App ID);
  - MAX sigue instalado, con su flujo de UMP apagado.
- **Fuera** (con `tools:node="remove"` cuando los traiga una dependencia):
  - `ACCESS_WIFI_STATE`, `CHECK_LICENSE`, `FOREGROUND_SERVICE`, `RECEIVE_BOOT_COMPLETED` y `ACCESS_ADSERVICES_TOPICS`;
  - `ACCESS_ADSERVICES_AD_ID` y `ATTRIBUTION`, que ya se quitan.
- **No aprobado todavía:** Unity Ads (3.21.1), a la espera de la cuenta.
- **Carril:** `tanda6/sdk-gma`.

## D-050 · Inicio nuevo: «el muelle del río»

- **Fecha:** 2026-10-25. Registrada por el integrador.
- **Estado:** decidido por el dueño en el chat. Tras ver el inicio («esta muy pobre, titino con una gota morada... no dice nada»), pidió «el artista seremos nosotros yo apruebo y sere ese entregamos algo listo para sacar encargate completo nada de espacios». Aprobó la maqueta del integrador: «Sí, apruebo la dirección del muelle del río; impleméntala completa y pásame capturas.»
- **Decisión:** el inicio (06 §4.7, fila Lobby) pasa a la composición «el muelle del río», sin cambiar la de D-008 (saldos arriba, botón central grande y pestañas abajo):
  1. **TiTino grande colgado de su rama** arriba a la izquierda, con un bocadillo que cambia: el saludo según la hora, el Reto pendiente, el dato del día o lo que falta para la Canasta. Respeta el ajuste de mensajes de TiTino y nunca promociona compras (D-013, Q10-A).
  2. **El río vivo** en lugar de la franja café plana: agua con reflejos, nenúfares y fauna que se mueve.
  3. **La canoa de Capi con Rosa** avanzando por una ruta hacia la **próxima parada** rotulada, con «faltan N niveles». Al tocarla se abre el Mapa del río.
  4. **La Crecida** como un poste de nivel en el agua, en lugar de la barra fina.
  5. **El muelle de madera** con «Jugar · Nivel N», el chip de dificultad y el **Cántaro** como tinaja de barro apoyada en el muelle.
  6. **Las tarjetas del día** en el muelle: Reto del río, Bitácora y Liga (si está activa).
  - Arriba se quedan los saldos, las canoas, Ajustes, Música y Sonido (D-046), y abajo las pestañas.
- **Arte:** el dueño hace de artista y aprueba. Todo el arte va **por código** (como el resto del juego), terminado y sin huecos para un artista externo. Si después quiere un acabado ilustrado en alguna pieza, se encarga aparte.
- **Condiciones:**
  - cabe a 360 × 780, en 16:9, a 320 dp y al 130 %, en es-419, en, pt-BR y en-XA;
  - con Reducir movimiento, todo queda quieto;
  - sin asignaciones por fotograma;
  - los textos nuevos van en `strings.csv` en los tres idiomas como borrador, y los nombres culturales siguen pendientes del asesor.
- **Aprobación del resultado:** antes de integrar, el dueño ve capturas del juego real.
- **Carril:** `tanda6/inicio-muelle`.
- **Cómo revertir:** con una decisión nueva del dueño.

### D-050, añadido del 2026-10-26: de noche TiTino duerme callado

- **Fecha:** 2026-10-26. Registrada por el integrador.
- **Estado:** decidido por el dueño en el chat, a la observación 3 de la jornada 3 de la prueba completa en el teléfono (`docs/dev/traspaso/pruebas-telefono-2026-10-26-completa-j3.md`: de noche TiTino dormía con `sloth_sleep` pero su bocadillo seguía rotando mensajes): «De noche TiTino duerme callado (solo un Zzz); si lo toco, se despierta un momento y dice su mensaje.»
- **Decisión** (06 §4.7, fila Lobby; `HomeSloth`):
  1. **De noche (20-6), mientras duerme:** sin bocadillo de mensajes ni saludo. El saludo de regreso no se dice ni se gasta: ni `sloth.last_greeting_date` ni `sloth.last_seen_date` cambian, así que sale en la primera visita de día [P]. Una visita de noche sin saludo pendiente sí se apunta en `sloth.last_seen_date`. En su lugar, un **«Zzz»** junto a la cabeza: tres zetas dibujadas por código (`HomeSlothZzz`, sin texto), que suben hacia la derecha y se desvanecen en un bucle de 3,6 s [P]. Con Reducir movimiento, quietas. No tapa la travesía, el letrero ni lo demás del inicio.
  2. **Al tocarlo de noche:** se despierta (`sloth_wake`, después `sloth_idle`) y dice el mensaje siguiente de la rotación, con las mismas reglas que de día (primero el Dato del día; nunca compras; respeta `settings.sloth_messages`). Cuando el mensaje se va, y como mínimo a los 4 s [P], vuelve a dormirse con su «Zzz». La rotación no vuelve sola hasta que amanece. Con los mensajes apagados, se despierta y vuelve a dormirse sin mensaje.
  3. **TalkBack:** dormido, lee «TiTino duerme» (`home_sloth_asleep_a11y`, borrador en es-419, en y pt-BR). De día, como antes.
  4. **Sondas:** `AF_STATE titino_sleep` (con `asleep`, `cause` y `note`) y `titino_asleep` en la línea `AF_LAYOUT lobby` (`docs/dev/prototipo-v0.md`).
- **Sin cambios:** el cielo del inicio sigue de día (D-050: el dueño no quiso ambiente nocturno), igual que la pose por la hora y lo que pasa de día.
- **Carril:** `tanda6/titino-noche`.
- **Cómo revertir:** con una decisión nueva del dueño.

## D-051 · Sin la pantalla «Made with Unity»; arranque con la marca

- **Fecha:** 2026-10-25. Registrada por el integrador.
- **Estado:** decidido por el dueño en el chat, a la propuesta del integrador: «si hagamoslo».
- **Decisión:**
  1. **Se quita la pantalla de Unity** (`PlayerSettings.SplashScreen.show` = false). Es opcional en todos los planes desde Unity 6. Se cambia en `AF.EditorTools.ProjectSetup` (método versionado; regla 3), y la comprobación de Player Settings del build de humo lo exige.
  2. **Pantalla del sistema** (Android 12+, la pone el sistema y no se quita): el icono v2 sobre el **fondo petróleo de la marca** (D-045), para que empalme con el juego.
  3. **Intro propia de 1 segundo como mucho:** el logo de Tintalú con la gota que tiñe el río (identidad v2), dibujada por código. Se muestra **mientras el juego carga**, nunca como espera añadida: si la carga acaba antes, se corta sin esperar. Con Reducir movimiento sale quieta, y ningún toque se pierde.
- **Carril:** `tanda6/arranque-marca`.
- **Cómo revertir:** con una decisión nueva del dueño.

### D-051, añadido del 2026-10-25: la pantalla de arranque con la marca v2

- **Estado:** pedido por el dueño (que hace de artista: D-050, D-052) el 2026-10-25: la pantalla de arranque seguía con el logotipo provisional (la gota morada sobre una cuia de madera, en un cielo de atardecer, con TiTino colgado de un palo gris) y «no dice nada y no es la marca». Dos pasadas más el mismo día, tras revisar las capturas: primero, faltaba la cuia que vierte y el río era una franja plana; después, el dueño: «hagamos que se entienda más el río Amazonas, con meandros; ya que acá tenemos más tamaño podemos hacer que se vea bien el río».
- **Decisión:** la intro y la pantalla de arranque (`BrandIntro`, `BootSplash`; 06 §2.2, D-016) son **«el Amazonas que se tiñe»**, y la pantalla de arranque es el último fotograma de la intro con el mundo alrededor, coherente con la identidad v2 (D-045) y con el inicio «el muelle del río» (D-050):
  1. **El río** (`BrandRiverScene`, compuesto por código para la pantalla vertical): agua blanca del Amazonas (#B08A5A) que serpentea con meandros desde el horizonte, fina entre la selva, hasta el primer plano, ancha; orillas de selva en copas, con su borde de matas; bancos de arena en media luna, pegados a la orilla interior de las curvas cerradas; una isla y ondas. El río nunca es más ancho que el 70 % del radio de su curva, para que la orilla interior no se pliegue (sin picos ni ganchos). Referencias: la propuesta A del icono (`bocetos/a-meandro.svg`) y el río del inicio. El horizonte va al 30 % del alto (36 % en 16:9) [P].
  2. **La identidad no cambia:** la cuia de vidrio del icono (silueta de `CuiaGeometry.cs` en capacidad 3, con Taperebá, Aguaje y Açaí, inclinada según D-006), su chorro en parábola, sus aros y las palmeras de açaí. `docs/art/marca-oficial/v2/exportar.py --unity-emblem <archivo> [--check]` las convierte desde el SVG de `generar.py` (con `emblema_unity.py`, solo biblioteca estándar) en capas de polígonos con sus colores, degradados, tramas y trazos (`BrandEmblem.Generated.cs`), y `BrandEmblem` las dibuja. La cuia vierte sobre un punto del río (al 36 % del cauce [P]) y **el açaí tiñe el cauce río abajo siguiendo los meandros**, hasta el primer plano, con el borde que se abre desde el impacto.
  3. **La intro** (D-051, punto 3) anima esa escena: el chorro baja de la boca de la cuia al agua (260 ms), salen los aros (hasta 560 ms) y el açaí avanza por el cauce en 10 tramos opacos (650 ms) [P]. Cada tramo acaba en una lengua orgánica (más adelantada en el centro, con el borde ondulado) y se enciende entero al llegar el frente: ni velos ni cortes rectos. Sigue sin alargar la carga y queda quieta con Reducir movimiento. Sus piezas son las de la pantalla de arranque (`BrandEmblemArt`, colocadas igual por `BrandEmblemImages`): al quitarse, no se mueve ni cambia de color nada.
  4. **El mundo alrededor** entra en 300 ms [P] al quitarse la intro (de golpe con Reducir movimiento): TiTino y su rama, al desvanecerse un velo con el petróleo y el halo de la intro; y, por encima del río, el nombre (con su sombra suave en la misma textura), el primer plano (matorral y follaje de las dos esquinas, en una sola textura: ninguna hoja deja ver a otra a medio fundido) y luciérnagas que laten. Se hace antes de medir el segundo de `sloth_wake` cuando hay espera (`BootSplash.PrepareWorld`) y, si no, en el primer fotograma tras la intro: en las aperturas que no esperan no cuesta nada.
  5. **El nombre del logo**, «Tintalú» en Baloo 2 ExtraBold en marfil (el logo «oscuro» de la v2), bajo la cuia, sobre el río, con una sombra suave para leerse: `exportar.py --unity-wordmark <archivo> [--check]` genera sus contornos (`BrandWordmark.Generated.cs`) y `BrandWordmark` los dibuja. Es un dibujo, no un texto (06 §2.1, regla 3). La ranura `ui_logo`, que nunca llegó a estar en `ArtSlots`, se retira con el logotipo provisional de la gota y la cuia de madera.
  6. **TiTino** cuelga de **la rama del inicio** (`ProceduralArt.HomeBranch`, D-050, a 1,3× [P]) en lugar de la rama gris de su rig, a la derecha (la cuia va a la izquierda), en el cielo: nunca tapa la selva del horizonte, el río, la cuia ni el chorro. Mide 140-160 dp (propuesta c.4), con `sloth_wake` en la primera apertura y `sloth_idle` en las demás. Sin TiTino (`ui_sloth_enabled` apagado o sin su arte), la pantalla sale igual, sin él ni su rama.
- **Sin cambios:** los tiempos (`load_ms`, `hold_ms`, el segundo de `sloth_wake` solo en la primera apertura y desde que se quita la intro), las sondas `AF_STATE boot` y `brand_intro`, el orden del lienzo (90), la zona segura y que el arte por código sale en todos los builds (D-052).
- **Coste:** la intro necesita la escena desde su primer fotograma, así que se dibuja al crear la pantalla, antes del primer fotograma de Unity y con la pantalla del sistema delante. El paisaje va a 1,75 px por dp como mucho (es blando por naturaleza); la cuia, el chorro y los aros, a la densidad de la pantalla. Con el pulido final (búferes compartidos, conversión directa a bytes, selva en pocas pasadas), en el Editor x64 a 3 px por dp cuesta unos 180 ms en 20:9 y 140 ms en 16:9 (antes, 280-330 ms). `BootSplash.EmblemTimings` da el desglose. Hay que medirlo en el teléfono.
- **Hallazgo (sin tocar):** `ProceduralArt.DrawLeaf` da NaN en la punta de las hojas lanceoladas y en bráctea (el seno de π es un negativo diminuto), y ese NaN borra una línea a lo ancho de la hoja en lo pintado antes en la misma textura. El primer plano del arranque solo usa hojas ovadas para esquivarlo. Corregirlo con `Max(0, …)` cambia las medidas de la fruta del Río Negro (la llave de la Hoja del 92 deja de llegar a su tope; `HojaBroteTests`), así que queda para una tarea propia.
  - **Corregido el 2026-10-26** (carril `tanda6/hojas-drawleaf`): `LeafWidthAt` acota el seno a ≥ 0 en las tres formas, y `DrawLeaf`, su sombra y `DrawLeafShape` no dibujan formas degeneradas ni mezclan píxeles no finitos (`LeafDrawingTests`). Las líneas borradas estaban en el follaje de las cuatro esquinas (frondas de açaí, hoja en flecha y heliconia), el manojo de la rama de la tropa y el pétalo de heliconia. La llave del 92 llegaba al tope solo porque `TiltedTop` contaba como opaca la banda de NaN de la punta de la hoja del Río Negro: corregida, la hoja girada no sube más que el medallón, la llave se queda en su sitio y `HojaBroteTests` ya no le exige el tope (sí al lulo). El arranque y el inicio siguen con hojas ovadas (ya aprobadas así). `ArtVersion` pasa a v2.2.2 para que la caché en disco no sirva el follaje viejo.
- **Carril:** `tanda6/arranque-v2`.
- **Cómo revertir:** con una decisión nueva del dueño.

### D-051, añadido del 2026-10-26: la intro de la marca se ve entera, unos 2,5 s, en cada arranque en frío

- **Estado:** decidido por el dueño en el chat el 2026-10-26, tras la jornada 3 de la prueba completa en el teléfono: «cuando abro el juego la animación es muy rápida, debemos dejarla más tiempo para apreciarla; también cuando se cierra la app y se vuelve a abrir no aparece la animación, debemos dejarla: es la marca, debe durar unos segundos, y si algo mientras decimos cargando o loading…». **Sustituye** la regla de D-051 (punto 3) y de su añadido del 2026-10-25 «la intro nunca alarga la carga y se corta al terminar».
- **Decisión** [P]:
  1. **En cada arranque en frío** (la primera apertura y siempre que la app se cierra y se vuelve a abrir), la intro se ve **entera y a su ritmo**: la cuia vierte, el chorro cae al agua (520 ms), salen los aros (hasta 1.250 ms), el açaí tiñe el Amazonas río abajo (hasta 1.800 ms), entra el nombre «Tintalú» bajo la cuia (1.650-2.000 ms) y se sostiene el emblema hasta **2.500 ms** (`BrandIntro.AnimMs`). Nunca se corta porque la carga acabe antes: el arranque la espera.
  2. **La carga va por debajo, repartida en fotogramas** (huecos seguros entre sus bloques: tras el perfil, el catálogo, TiTino, los anuncios y la pantalla del juego), para que la intro vaya fluida. Si la intro ya se vio entera y la carga sigue, sale **«Cargando…»** (clave `boot_loading`, es-419, en y pt-BR) con tres puntos de espera bajo el nombre, hasta que acabe; si la carga acaba antes, no se ve.
  3. **Al volver de segundo plano** (la app no se cerró) no se repite: no hay arranque en frío ni se recarga nada.
  4. **Reducir movimiento:** la escena entera, con el nombre, sale quieta y se sostiene 1 s (o lo que dure la carga, si es más).
  5. El segundo de `sloth_wake` de TiTino en la primera apertura sigue **después** de la intro, como hasta ahora; con la intro, el primer toque en el nivel 1 sigue muy por debajo de los 20 s (06 §2.2, 09 §6: en el teléfono, unos 2 s de arranque del motor + 2,5 s de intro + 1 s de TiTino).
  6. Sigue lo de la jornada 3 para la observación 1 (**nunca un petróleo liso largo** antes de la marca): el fondo de la ventana de lanzamiento lleva el icono v2 centrado, recortado en el círculo de 160 dp de la pantalla del sistema, hasta el primer fotograma del juego (`drawable/af_launch_window.xml`, generado por `exportar.py --android-xml`); el reloj de la intro cuenta desde su primer fotograma; y la escena rasterizada se guarda en una caché de disco (`BrandEmblemCache`, en `temporaryCachePath`, con clave por lienzo, densidad, huella de los datos generados y `buildGUID`) para que desde la segunda apertura se lea en vez de dibujarse (en el Editor: 180 ms dibujarla, 32 ms leerla).
- **Sondas:** `AF_STATE brand_intro` con `anim_ms` 2.500, `cut` false (true solo si el arranque se para antes), `loading_ms` (lo que se vio «Cargando…»), y, de la jornada 3, `first_frame_ms`, `start_ms`, `emblem_ms`, `emblem_cache` y `yields`; `AF_STATE boot` con `hold_ms` que incluye la espera a la intro; y `AF_PERF boot_steps` con los bloques de la carga (`docs/dev/prototipo-v0.md`).
- **Carril:** `tanda6/fx-j3`.
- **Cómo revertir:** con una decisión nueva del dueño.

## D-052 · El arte por código es el final: se retira P-M5

- **Fecha:** 2026-10-25. Registrada por el integrador.
- **Estado:** decidido por el dueño en el chat, al aprobar el inicio nuevo (D-050): «retiro P-M5: el arte por código es el final y sale en release».
- **Contexto:** P-M5 de D-023 (`MapArtRule`) y la regla hermana de la fauna (`FaunaArtRule`, guion de la intro §4.3 y §11) solo dejaban salir el arte provisional por código en el Editor y en los builds de desarrollo, a la espera de los PNG de un artista. Con D-050 el dueño hace de artista y el arte por código es el definitivo, así que en release se habrían quedado fuera TiTino (inicio, arranque y Mapa), las siluetas de héroe del Mapa, las banderas, la ventanita de casa, la tinaja, el cucharón y los miquitos.
- **Decisión:**
  1. **Se retira P-M5** (punto 16 de D-023) y la exigencia de PNG de la fauna: el arte por código sale en todos los builds, release incluido.
  2. **La ranura de `ArtSlots` sigue mandando:** si algún día llega un PNG, sustituye al dibujo por código. Las ranuras no desaparecen.
  3. **Se puede usar en la ficha, las capturas y la UA**, porque es el arte final.
- **Riesgo anotado:** las banderas simplificadas por código (Brasil, Colombia, Perú, Ecuador y Bolivia, sin escudos) salen ahora en release y en la ficha sin la revisión del abogado, aplazado por D-047; la embaúba y los nombres de fauna siguen pendientes del asesor cultural.
- **Carril:** `tanda6/arte-release`.
- **Cómo revertir:** con una decisión nueva del dueño.

## D-053 · El Mapa con viaje: «Jugar» espera a que Capi llegue y nada queda quieto al abrir

- **Fecha:** 2026-10-26. Registrada por el agente de Unity (carril `tanda6/mapa-viaje`).
- **Estado:** pedido por el dueño en el chat, tras la jornada 3 de la prueba completa en el teléfono: «cuando veo el mapa, luego de pasar los 10 niveles, pareciera que no pasa nada, pero está el botón para seguir de nivel y no deja ver la animación del Capi subiendo el río; es un atractivo y se pierde. Entonces no debería estar habilitado hasta que suba la sección correspondiente, y que se tenga la sensación de que va a pasar algo, porque por unos instantes no parece que pase algo.»
- **Cómo estaba (medido en el código y en los tests del 2026-10-26):**
  - en el Mapa del 10 (06 §2.3, fila 3), la canoa esperaba quieta en Belém mientras TiTino decía su bocadillo: 3.200 ms con voz (`NoticeTiming.ReadMs` de `map_l10_intro`) y 1.000 ms sin ella (`PresentOnlyMs`); «Jugar · Nivel 11» estaba a la vista desde el primer fotograma y un toque en cualquier sitio (también en él) saltaba la secuencia entera;
  - en «Ver mapa» de la victoria del `10k`, la franja ya había anotado la llegada como vista (`map_seen_level`), así que la canoa no se movía nada: ningún viaje;
  - en el fin de contenido, la canoa salía en el primer fotograma, pero «Volver» y «←» estaban ahí y un toque completaba el viaje.
- **Decisión:**
  1. **Mapas con viaje:** el del 10 (`l10`), «Ver mapa» de la victoria del `10k` (`victory`, que ahora repite el viaje del tramo recién ganado aunque la franja ya lo anotara, como el fin de contenido; sin `stop_arrive` nuevo) y el fin de contenido (`content_end`). El Mapa abierto desde la pestaña, la pausa o el banner sigue como antes (allí un toque completa «ponerse al día»); el viaje dentro del banner de mundo es otra pieza (≤ 4 s, saltable, 02 §6) y no cambia.
  2. **Sin tiempo muerto:** desde el primer fotograma la vista ya tiene la canoa de Capi (con Rosa o el bufeo delante) y el tramo (en la victoria, si el principio del tramo queda fuera, se corre lo justo); suena `sfx_map_paddle`, la canoa se balancea (± 4° [P]) y la mini cuia de la parada a la que va late; la canoa sale a los **250 ms [P]** (≤ 300 ms). En el Mapa del 10 la canoa ya no espera al bocadillo: sube mientras TiTino habla y la placa del Dato del río 1 sigue entrando al acabar el bocadillo.
  3. **«Jugar» (o «Volver») y «←» no están hasta que la canoa llega** a la parada del tramo: ocultos y sin responder durante el viaje, y fuera de `AF_LAYOUT`; al llegar entran con un fundido y un pequeño rebote (250 ms [P]), suena `sfx_map_sample` y TalkBack anuncia la llegada («{lugar} · {país} · río {río}», con el botón).
  4. **El toque no salta el viaje:** lo recibe un área sobre toda la pantalla (`map_journey`); en la anticipación hace salir la canoa ya y, remando, la acelera ×2 [P]. Tras la llegada, en el Mapa del 10 un toque salta lo que queda (la placa del dato), como antes.
  5. **Atrás de Android** durante el viaje cierra el Mapa, como antes (las llegadas que faltaban, saltadas; lo visto, anotado): Atrás nunca se bloquea, Android lo pide como salida y no se pulsa sin querer. **Reducir movimiento:** sin viaje (el fundido de «ponerse al día»), así que los botones están desde el principio. **Segundo plano:** al volver, el viaje salta al final (02 §14) y entran los botones. Esto vale para cualquier Mapa abierto: también completa «ponerse al día» en el de la pestaña y el de la pausa, con las llegadas que faltaban como saltadas (`skipped` = true en `af_stop_arrive`).
- **Sondas:** `AF_LAYOUT river_map` publica solo `map_journey` durante el viaje y `play` y `back` al llegar; `AF_STATE map_play_ready` (nueva) al habilitarse, con `first_move_ms`, `ready_ms`, `taps` y `by`; `AF_STATE map_journey` también con `source` `victory` (docs/dev/prototipo-v0.md).
- **Cambia el GDD:** 06 §2.3 (fila 3: «hasta toque» pasa a «la canoa sube enseguida y "Jugar · Nivel 11" sale al llegar») y 06 §4.5 (Entradas).
- **Cómo revertir:** con una decisión nueva del dueño; técnicamente, `RiverMapScreen.JourneySource` y las constantes `Journey*` de `RiverMapScreen.Journey.cs`.

## D-054 · La revisión de los textos la hace Gemini (español cultural, inglés y portugués de Brasil)

- **Fecha:** 2026-10-26. Registrada por el integrador.
- **Estado:** decidido por el dueño en el chat. Ante los 1.087 problemas de textos que paran el build de release (`LocReleaseGate`), dijo: «Pon el encargo a Gemini, que es mejor en idiomas».
- **Decisión:**
  1. La validación cultural del español (las filas `pendiente_asesor`) y la revisión del inglés y del portugués de Brasil (`traducido` → `revisado_nativo`), con la traducción de las filas de `facts.csv` que faltan, las hace un agente de Gemini. Encargo: `docs/dev/traspaso/gemini-revision-textos.md`.
  2. Sustituye, para la 1.0, al asesor cultural humano y a la revisión nativa humana (D-018, Q5-A; D-022). El dueño revisa una muestra y las filas corregidas o dudosas antes de que pasen a `aprobado_es`; las dudosas se quedan en `pendiente_asesor` y no se muestran en release.
- **Riesgo anotado:** es una revisión por IA, no humana. Un error cultural o de traducción puede llegar al jugador. Se puede volver a una revisión humana en una versión posterior sin cambiar el juego.
- **Cómo revertir:** con una decisión nueva del dueño.

### D-054, añadido del 2026-10-26: aprobación de la entrega de Gemini

- **Estado:** el dueño aprobó en el chat: «Apruebo los 12 cambios al español y las recomendaciones de T2-Q8 a T2-Q11 de Gemini. Integra la revisión con el modo de revisión de apply_translations, los tests y la compuerta LocReleaseGate.»
- **Entrega:** rama `spark/revision-textos`, commit `fcc37e10`, en `docs/dev/traspaso/gemini-revision/`. Son:
  - `revision_es.csv`: 1.083 filas, 1.071 `ok`, 12 `cambia` y 0 `duda`;
  - `strings_revision.csv` (542 filas) y `facts_revision.csv` (822 filas), en `revisado_nativo`;
  - `preguntas_t2.md`;
  - `resumen.md`.
- **Validación del integrador:**
  - **Formato:** UTF-8 sin BOM, LF, en orden ordinal y con las mismas claves que las tablas.
  - **Contenido:** iguales marcadores y plurales que el español, sin filas vacías y con el U+00A0 conservado. Las 10 celdas que superan `max_chars` × 1,3 ya eran así en las tablas y no las tocó.
  - **Fuentes:** 589 de las 820 filas de `facts.csv` citan una URL. El resto cita documentos internos (204 citan solo el glosario).
  - **Motivos de los `ok`:** son en gran parte frases de plantilla. La parte cultural queda como revisión de IA, aprobada por el dueño. El asesor humano puede revisarla en la 1.1.
- **Decisión:** se aplican los 12 cambios al español, las traducciones revisadas y T2-Q8 a T2-Q11 con la recomendación de Gemini. Eso incluye:
  - `Manaus` en `en` y `pt-BR` (T2-Q9), con `world_w11_name` localizado;
  - los textos del glosario §10 (T2-Q10);
  - las propuestas del `glosario.diff` (T2-Q11);
  - las dos líneas `borrador` de `fact.agua_tapajos.14` y `.19`, sustituidas por las que propone el resumen §4.2.
- **Encargo:** carril `tanda6/integra-gemini-textos`.
- **Del integrador (2026-10-26), al integrar `tanda6/integra-gemini-textos`:**
  - `LocReleaseGate` pasa de 1.087 a 0.
  - 1.085 filas quedan en `aprobado_es` y 2.728 celdas en `revisado_nativo`.
  - En los CSV de la entrega se editaron 4 celdas, para que el `--check` las dé por aplicadas: `world_w03_name`, `world_w11_name` y `fact.agua_tapajos.14` y `.19`.
  - Las `notes` de `strings.csv` y `facts.csv` siguen diciendo «Pendiente del asesor cultural»; se ponen al día en otra tarea.
  - Un cambio posterior del español de una fila revisada hace fallar `--check`: se cierra con una entrega nueva de revisión.

## D-055 · Música actual para la 1.0, AD_ID aprobado, 13-17 sin anuncios en el EEE, Reino Unido y Suiza, y lanzamiento mundial

- **Fecha:** 2026-10-27. Registrada por el integrador.
- **Estado:** respuestas del dueño en el formulario del chat:
  - TiTino de noche: «Calla mientras duerme»;
  - música: «Salir con la música actual»;
  - permisos de anuncios: «Aprobar AD_ID»;
  - 13-17 en el EEE, el Reino Unido y Suiza: «Sin anuncios, pero vamos a lanzar en todo el mundo, no solo america latina.»
- **Decisión:**
  1. **TiTino de noche (20-6)** duerme callado. Ya lo hace (añadido de D-050 del 2026-10-26, `HomeSloth.cs`). Queda comprobarlo en el teléfono, en la jornada 4.
  2. **Música:** la 1.0 sale con la música actual; la v2 queda para una actualización.
  3. **Permiso `com.google.android.gms.permission.AD_ID`:** aprobado (regla dura 4) para los anuncios de AdMob (D-049).
     - Solo se usa con el consentimiento de anuncios personalizados y con 18 años o más. Con 13-17, no personalizados; con <13 no arranca nada.
     - `ACCESS_WIFI_STATE` y `CHECK_LICENSE` siguen fuera.
     - Va a la lista blanca de `ci/check-manifest.ps1`, a Data Safety (08 §13.3, `docs/store-listing/data-safety.md`) y a la política de la web.
  4. **13-17 en el EEE, el Reino Unido y Suiza:** sin anuncios. Confirma la provisional Q-MAX-6 (b), ahora aplicada a AdMob.
  5. **Lanzamiento mundial:** la 1.0 se publica en todo el mundo, no solo en América Latina. Cambia los supuestos de 08 §13.1 (países), 05 §4.2 (precios), de la edad y el consentimiento por país (05 §7) y de la política. Esto queda pendiente:
     - el **abogado** revisa la edad de consentimiento digital por país (el RGPD art. 8 la sube a 16 años en algunos Estados), la COPPA en EE. UU. y el texto legal mundial;
     - los **precios** de los demás países, en Play Console (plantillas o conversión automática);
     - los **idiomas:** se mantienen es-419, en y pt-BR. El resto del mundo ve inglés.
     - Lo que el código ya resuelve por país (el consentimiento UMP en el EEE, el Reino Unido y Suiza, y los perfiles `biz_legal_profile_by_country`) se revisa en una tarea aparte.

## D-056 · Avatares del jugador en la 1.0; inicio de sesión con Facebook, para la próxima versión

- **Fecha:** 2026-10-27. Registrada por el integrador.
- **Estado:** respuestas del dueño en el chat. Pidió: «Quiero tengamos unos avatars predeterminados para escoger para el personaje del jugador, esto con respecto a la tematica, y podamos mostrarlo en la pantalla principal de inicio». En el formulario eligió:
  - cuándo: 1.0;
  - estilo: animales de la selva;
  - dónde: Inicio, Liga y Victoria/perfil;
  - desbloqueo: «Unos libres y otros por hitos».
- **Decisión. Avatares:**
  - un juego de **avatares predeterminados de animales de la selva** (retratos redondos), con el arte por código como el resto (D-052);
  - se eligen en el juego: unos están libres desde el principio y otros se desbloquean por hitos (mundos, Reto del río, Liga…);
  - se ven en el **inicio**, en la **Liga** (junto al seudónimo, guardado en el backend como un índice, sin datos personales) y en la **victoria o el perfil**.
  - Hasta ahora no existían: la Liga solo mostraba un seudónimo automático y la bandera (`docs/dev/liga.md` §1).
  - Diseño: `docs/dev/avatares.md` (carril `tanda6/avatares-diseno`), con las preguntas abiertas y su recomendada.
- **Para la próxima versión, sin hacer ahora:** iniciar sesión con Facebook para guardar el progreso, como Magic Sort.
  - **Qué se gana:**
    - recuperar la partida en otro teléfono, también entre Android e iOS, sin depender de la cuenta de Google;
    - funciones sociales (amigos en la Liga, invitar, regalar vidas o premios), que en juegos casuales suben la retención y la viralidad;
    - el premio por conectarse, como gancho.
  - **Qué cuesta:**
    - el SDK de Facebook (rastreadores, permisos, Data Safety y la revisión de la app por Meta);
    - la URL de borrado de datos que exige Meta;
    - las reglas de edad (13-17);
    - mantenimiento.
  - **Hoy** Play Games (Saved Games, D-042) ya guarda la partida en Android.
  - **Recomendación para la 1.1:** vincular primero Firebase Auth con Google (y con Apple si llega iOS); Facebook, solo si se hacen funciones sociales con amigos.

## D-057 · Lanzamiento mundial y avatares con las recomendadas; lo legal se investiga sin abogado

- **Fecha:** 2026-10-27. Registrada por el integrador.
- **Estado:** el dueño en el chat: «vamos con la recomendaciones, lancemos investigaciones si requieres saber algo mas de lo legal, pero vamos a lanzar con lo que investiguemos, por ahora no tenemos revisor legal, pero para eso tenemos todo el dominio de internet para investigar y copiarnos a los que ya lo hacen bien y lo que fallaron aprender de esos errores.»
- **Lanzamiento mundial** (`docs/dev/lanzamiento-mundial.md`, P1-P10, todas con la recomendada):
  1. una sola salida mundial, con la producción escalonada por porcentaje;
     - El escalonado y la prueba cerrada los sustituye D-059 (2026-10-28).
  2. la clave `consent_digital_age_by_country`, que apaga la analítica para 13-17 donde la edad de consentimiento digital pasa de 13;
  3. distribución en todos los países salvo Rusia y Bielorrusia (Vietnam e Indonesia, según la investigación);
  4. el país de la cuenta de Play como segunda fuente del país legal;
  5. precios con base en USD y la conversión de Play, con LatAm y EUR fijados a mano;
  6. la tanda 2 de idiomas, después de la 1.0;
  7. una tabla ISO completa de países para la Liga, en los tres idiomas;
  8. solo `sa-east-1` en la 1.0;
  9. ficha de Play con en-US por defecto, más es-419, es-ES, pt-BR y pt-PT;
  10. no hay abogado: en su lugar se hace una investigación propia (abajo).
- **Avatares** (`docs/dev/avatares.md`, Q-AV-1 a Q-AV-12, todas con la recomendada):
  - los 16 animales y 6 libres;
  - en el primer arranque, uno libre al azar;
  - en el inicio, arriba a la izquierda, desde el 10;
  - en la victoria, solo en la línea de puntos de la Liga;
  - el seudónimo es independiente del avatar;
  - un avatar bloqueado se ve en silueta, con su hito;
  - los 10 hitos propuestos;
  - el avatar de la Liga queda oculto si la Liga está apagada;
  - lo ven también 13-17;
  - perfil con cabecera y rejilla;
  - los marcos, en la 1.2;
  - **la anaconda se retira de los seudónimos nuevos** (glosario §3.3), sin cambiar los índices.
- **Lo legal, sin abogado:**
  - los marcadores `[[ABOGADO]]` se resuelven con una investigación propia en fuentes oficiales y en lo que hacen los juegos que lo hacen bien, aprendiendo también de sus sanciones y errores (`docs/legal/investigacion-*.md`);
  - lo que no se pueda resolver con fuentes queda como riesgo aceptado por el dueño;
  - la revisión de un abogado pasa a ser opcional y posterior.

### D-057, añadido del 2026-10-27: decisiones sobre la investigación legal

Fuente: `docs/legal/investigacion-legal-mundial.md` (§11, tareas T-L; §12, riesgos). El dueño respondió en el chat a cuatro preguntas. El resto sigue la recomendada, por su regla «si tienen recomendada vamos con esa».

- **Representante en la UE y en el Reino Unido (T-L7, R1):** «Aceptar el riesgo por ahora». Se lanza sin representante, en contra de la recomendación de la investigación. La política usa la variante «sin representante» de §9, fila 2. Se contrata si el juego crece en el EEE o en el Reino Unido.
- **Play Age Signals (T-L3, R7):** «Sí, integrarla». El dueño aprueba la dependencia nueva, Play Age Signals API 0.0.4, solo para la edad (regla dura 4). Va acompañada de T-L4: la banda que venga de la señal no se manda a Analytics. Hay que declarar en Play Console las clasificaciones por edad de los productos.
- **Países excluidos (T-L2, R8):** «RU, BY, VN e ID». Turquía se queda: se acepta R8 y se revisa si pasa del 3 % de las instalaciones. Lo configura el dueño en Play Console; 08 §13.1 lo recoge (T-L15).
- **Crashlytics (T-L6, R5):** «Sí, interruptor en Ajustes». Habrá un interruptor «Enviar informes de fallos» en «Privacidad y datos», activado por defecto. Sustituye la «línea sin interruptor» de Q-B12-4.
- **Con la recomendada:**
  - T-L5: `rdp=1` con 13-17 en EE. UU.;
  - T-L8: los textos de la web de §9;
  - T-L9: la tienda frente a la ACM;
  - T-L12: el país de la Liga;
  - T-L13: los avisos de licencia;
  - T-L15: el GDD;
  - riesgos R2 a R6 y R10 a R15, aceptados con sus medidas;
  - R9: firmar la licencia Parallext → S.A.S antes de publicar.
- **Las decide o hace el dueño:**
  - T-L10, el «≈ x €» informativo y los tamaños de pack; queda abierta;
  - T-L11, las consolas de Firebase, Analytics y AdMob;
  - T-L14, la ficha sin captación infantil.

### D-049, añadido del 2026-10-27: AppLovin MAX sale del build

- **Decisión del dueño, en el chat:** «Sí, quita AppLovin MAX del build de release; si AppLovin abre, entra como red dentro de AdMob».
- **Motivo:** AppLovin sigue sin aceptar cuentas nuevas. MAX iba en el AAB sin servir anuncios: con su detector de fallos nativo, con okhttp3 y teniendo que nombrarlo en Créditos y en la política (`docs/legal/avisos-de-licencia.md` §5.2).
- **Cambio:** el SDK de AppLovin MAX y sus dependencias salen del build de release. La interfaz de `AF.Ads` sigue igual. Una compuerta de la CI comprueba que el release no lleva `com.applovin`.
- **Si AppLovin abre cuentas:** entra como red de mediación dentro de AdMob, con su adaptador y su versión aprobados por el dueño. MAX no vuelve.
- **Sustituye** el punto 5 de D-049 («MAX queda guardado»).

### D-057, añadido del 2026-10-27: decisiones de la revisión del código mundial

Las toma el integrador con la recomendada, según la regla del dueño «si tienen recomendada vamos con esa». Fuente: la revisión de `tanda6/mundial-codigo`.

1. **El país legal llega al servidor de la Liga.** El país que resulta de la SIM, de la cuenta de Play o de la región va como `region` de la Liga (liga.md §3.3) y como `geo_tier` agregado en la analítica. Se corrigen los documentos que decían que el país de Play no sale del teléfono, la política de la web y Data Safety.
2. **Nombres de países de CLDR 47.0.** Se aceptan como fuente para la tabla de la Liga, con la misma excepción que las 21 filas de D-046 (`aprobado_es`/`revisado_nativo`, con la nota «Pendiente del asesor cultural»). El asesor revisará con prioridad los nombres con carga política: Taiwán, Palestina, Kosovo y Sáhara Occidental.
3. **13-17 sin país conocido:** no se ofrece la analítica, que es el tratamiento más estricto. Sustituye el «sin país, `*`» de T-L1.
4. **Lista T1 (T-M6):** se añaden Nueva Zelanda, Singapur, Hong Kong, Taiwán, Israel y los países del Golfo, como [P]. Se editan 04 §6.2 y 05 §6.3.

### D-049, añadido del 2026-10-27: Unity Ads como red de mediación de AdMob

- **Aprobación del dueño, en el chat:** «apruebo el adaptador 3.21.1». Es `com.google.ads.mobile.mediation.unity` 3.21.1, con el adaptador `com.google.ads.mediation:unity:4.20.1.0` y el SDK `com.unity3d.ads:unity-ads:4.20.1` (`docs/dev/anuncios-gma-propuesta.md` §7).
- **Cuentas, configuradas con el dueño el 2026-10-27:**
  - **Unity:** la organización 20067668703382 tiene el proyecto y la app «Tintalú» (Android, audiencia general, sin URL de tienda). El Game ID es 800412912 y los placements de bidding son `BP_Rewarded_Android` y `BP_Interstitial_Android`.
  - **AdMob:** hay dos grupos de mediación con Unity Ads por licitación: «Tintalú Recompensado Android» (9122104719) y «Tintalú Intersticial Android» (3021919976). El dueño aceptó las condiciones de procesamiento de datos y el acuerdo de licitación.
  - **Pendiente del dueño:**
    - poner Parallext, LLC como nombre de la organización de Unity, con su dirección y los datos de cobro;
    - añadir Unity Ads a los partners de los mensajes de RGPD y de EE. UU. en AdMob.
- **Bloque recompensado corregido:** `…/2877105387` era de formato «Intersticial recompensado» y el juego lo cargaba como Recompensado. Se creó `…/1182085615` (Recompensado), que pasa a ser el bloque de producción.
- **Condiciones de la integración:**
  - quitar el inicializador automático de Unity Ads, para que nada arranque antes de la edad y el consentimiento;
  - sin permisos nuevos;
  - la biblioteca nativa alineada a 16 KB;
  - el AAB por debajo de 80 MB.

## D-058 · Rediseño de los personajes: «3D cartoon», juguete brillante, por código

- **Fecha:** 2026-10-27. Registrada por el integrador.
- **Estado:** pedido por el dueño en el chat: «quiero que hagamos rediseño de los personajes, demosle mas estilo, como 3D cartoon, CGI estilizado, character y glozy toy aestethic, manda esa encargo a spark». A las tres preguntas respondió con las recomendadas: todos los personajes, por código en pseudo-3D y con un piloto primero.
- **Decisión:**
  1. TiTino, Capi, la fauna y los 16 avatares pasan a un acabado de personaje CGI estilizado de juguete brillante:
     - volumen;
     - luz principal arriba a la izquierda y luz de borde;
     - brillo de barniz;
     - ojos con reflejos;
     - sin el contorno de tinta grueso.
  2. **Se sigue dibujando por código (D-052):** sin modelos 3D, renders, IA de imágenes ni herramientas o paquetes nuevos. Se mantiene la dirección «Selva brillante premium» (D-003).
  3. **Lo hace el Spark** (`docs/dev/traspaso/spark-personajes-3d.md`) en dos fases:
     - un piloto: guía de estilo, TiTino, Capi, dos animales y capturas de antes y después, que **aprueba el dueño**;
     - después, el resto.
  4. **Sustituye** el «contorno de tinta de Capi» de los avatares (D-056 y `docs/dev/avatares.md`). Las APIs, los tamaños, los `ArtSlots` y los presupuestos de tiempo siguen igual.

### D-045, añadido del 2026-10-27: el río del logo con meandros (variante B)

- **Pedido del dueño, en el chat:** «realmente no se logra entender que es un rio, dejemos la idea pero agregando los meandro como hicimos en la pantalla de carga». Tras ver la hoja de comparación (`docs/art/marca-oficial/v2/bocetos/meandros/hoja-meandros.png`), eligió: «Vamos con la variante B (dos meandros con perspectiva)».
- **Decisión:**
  - el emblema mantiene la idea de D-045: la cuia de vidrio vierte açaí y el río se tiñe desde el impacto, río abajo;
  - el río pasa a dos meandros con perspectiva: dos curvas finas de agua blanca a lo lejos y una curva ancha de açaí hacia el primer plano, como en `BrandRiverScene`;
  - se regeneran desde `generar.py`:
    - el icono, con el adaptativo, el monocromo y el clásico;
    - los logos y los favicons;
    - el gráfico de funciones;
    - `AfIcon.androidlib`;
    - `BrandEmblem.Generated.cs`;
    - los recursos de la web.
- **A cuidar en el cambio:**
  - en el monocromo y a 48 px, las orillas lejanas no deben convertirse en ruido; se simplifican si hace falta;
  - la cuia debe quedar dentro de la zona segura de 66 dp.

### D-054, añadido del 2026-10-27: revisión 2 de Gemini, aprobada y aplicada

- **Aprobación del dueño, en el chat:** «Apruebo los 9 cambios de la revisión 2 de Gemini y también el cambio de league_me_no_points a «Vença um nível para entrar na Liga». Aplícala e intégrala.»
- **Entrega:** `docs/dev/traspaso/gemini-revision-2/`, del encargo `gemini-revision-textos-2.md`. Revisa las 27 claves nuevas (avatares e interruptor de informes de fallos):
  - pasan a `revisado_nativo` en inglés y portugués de Brasil;
  - cambian 9 celdas, 4 en inglés y 5 en portugués;
  - el español no cambia.
- **Fuera del encargo:** `league_me_no_points` en portugués pasa de «fase» a «nível». Se cambia también en la entrega 1, para que su `--check` siga cuadrando.
- **Herramienta:** `apply_translations.py --review` admite ahora revisiones parciales, sin `revision_es.csv` o sin `facts_revision.csv`.
- **Resultado:** no queda ninguna fila en `traducido`, así que la compuerta `LocReleaseGate` vuelve a quedar sin problemas en inglés y portugués de Brasil.

### D-035, añadido del 2026-10-27: Firebase prod con la app Android y AWS prod completo, desplegados

- **Estado:** hecho por el escritorio con el dueño, en sus sesiones de Firebase (cuenta empresarial) y de AWS (`af-prod`), con su sí literal al `cdk diff`: «Sí: despliega AWS prod completo (cdk deploy --all) con ese diff y esos contextos.»
- **Firebase prod (`tintalu`):** registrada la app Android `com.parallext.tintalu` («Tintalu Android (prod)»), ID `1:459140125724:android:810c83a65d0ff611f07bd9` (público). El dueño descargó `google-services.json` y lo guarda fuera del repositorio. Analytics: conservación de datos de eventos 2 meses y de usuario 14 meses (decisión del dueño). La plantilla de Remote Config (`tools/out/remote_config_template_prod.json`, 250 parámetros) la publica el dueño desde la consola.
- **AWS prod (cuenta `522982706995`):** `cdk deploy --all` con `env=prod`, `domain=tintalu.com`, `firebaseProjectId=tintalu` (el proyecto no se llama `amazon-fill-prod`, el valor por defecto), `firebaseAppIds=<el ID de arriba>`, `packageName=com.parallext.tintalu` y `alertEmail=support@tintalu.com`. Creados `AfProdData`, `AfProdApi`, `AfProdCdn` y `AfProdOps`; `AfProdWeb` añade la ruta del borrado de cuenta y vuelve a subir el sitio; `AfProdGlobal` sin cambios. Salidas: `ApiUrl` `https://vsublt4swh.execute-api.sa-east-1.amazonaws.com`, `CdnUrl` `https://d2dkpz8mz2w7s9.cloudfront.net`.
- **Comprobado:** `/v1/ssv` sin firma → 403; `POST /v1/purchases/verify` sin token → 401; `https://tintalu.com` → 200 y `config.json` → `{"deleteEnabled":true,"apiBase":""}`; `POST /v1/account/delete-request` vacío desde la web → 400; alarmas: 25 en OK y 4 sin datos.
- **SSV de AdMob:** activado en «Tintalu Rewarded» (`…/1182085615`) con `<ApiUrl>/v1/ssv`; la consola verificó la URL. Marcada «Aplicar a todas las redes en los grupos de mediación», para que las recompensas servidas por Unity Ads también se confirmen en el servidor.
- **Pendiente del dueño:**
  - cargar los secretos `af-prod/firebase-admin-service-account` y el de la API de Google Play desde archivos fuera del repositorio (`infra/README.md` §5); este último, cuando exista la app en Play Console;
  - confirmar la suscripción de las alarmas que llega a `support@tintalu.com`;
  - App Check con Play Integrity y la huella SHA-256 de la firma de Play, cuando exista la app en Play Console.
- **Pendiente técnico:** `release.yml` no coloca `google-services.json`, así que un release firmado hoy sale sin Firebase (`UnavailableBackendClient`: la tienda no vende). Hace falta un secreto de CI que cree el dueño y un paso que lo escriba en `Assets/_LocalSecrets/` antes del build.

## D-059 · Lanzamiento directo a producción al 100 %, con publicación administrada

- **Fecha:** 2026-10-28. Registrada por el integrador.
- **Estado:** decidido por el dueño en el chat. Ante la idea de subir el AAB a la prueba interna dijo: «NO quiero prueba interna, osea cuando se lance quiero que ya este disponible completamente». Cuando le explicaron la publicación administrada respondió: «si hagamoslo administrada». Ese mismo día, a la pregunta «¿Cómo probamos las compras antes del lanzamiento?» (sin pistas con jugadores, la primera compra real con Play Billing y su verificación en el backend, PAD y Play Games instalados desde Play no se verían antes de producción), respondió: «Pista interna, solo yo», con el botón «Usa la pista interna solo para mí (sin jugadores) para probar compras con licencia de prueba, PAD y Play Games antes de publicar; el lanzamiento público sigue siendo al 100 % con publicación administrada» (punto 5).
- **Decisión:**
  1. **Sin pistas de prueba con jugadores:** ni cerrada ni abierta, y sin escalonado. La pista interna se usa **solo con el dueño como único tester** (punto 5, decidido el mismo día) y el AAB que pasa esa prueba se promociona a la pista de producción de Play Console. La cuenta es de organización (Parallext), así que no se le aplica la prueba cerrada obligatoria de 12 probadores durante 14 días (research/06 §5 [V]; setup/b §6).
  2. **Al 100 % desde el primer día, sin escalonado:** se elimina el 5 % → 20 % → 100 %. La versión sale a todos los países de D-057 a la vez: todos los de Play salvo RU, BY, VN e ID, con Turquía incluida.
  3. **Publicación administrada** («Resumen de publicación» › «Publicación administrada» en Play Console), activada **antes** de enviar a revisión:
     - Google revisa la versión;
     - una vez aprobada, no sale hasta que el dueño pulsa «Publicar cambios»;
     - el dueño elige así el día y la hora del lanzamiento.
  4. **Qué sustituye a la prueba con testers** [P] (propuesta del integrador):
     - la regresión de release de 60 casos (`docs/qa/regresion-release.md`, 08 §14.3), en el teléfono con el ensayo de release del mismo commit, antes de lanzar `release.yml`;
     - la prueba completa en el teléfono por jornadas;
     - la prueba del dueño en la pista interna (punto 5);
     - después del lanzamiento, Crashlytics, las alertas de Play y Remote Config para apagar sistemas sin publicar (`biz_feature_*`, `mech_enabled_*`). Las métricas de calidad de 08 §13.1 #21 se miden ya en producción.
  5. **Pista interna solo con el dueño:**
     - el dueño es el **único tester**, nunca con jugadores;
     - prueba ahí las compras con licencia de prueba y su verificación en el backend, PAD y Play Games, con la app instalada desde Play (`docs/dev/iap.md` §3);
     - el AAB que pasa esa prueba se **promociona** de la pista interna a producción: es la misma versión y el mismo versionCode, así que no hace falta otro build;
     - se publica al 100 %, con la publicación administrada (puntos 2 y 3); el orden está en `docs/dev/release.md` §4 bis.
- **Riesgos (expuestos al dueño en el chat antes de su respuesta):** se le dijo «si sale un fallo en un modelo de teléfono que no hemos probado, le llega a todo el mundo a la vez; se puede detener una versión en Play, pero quien ya la instaló la conserva».
  - un fallo en un modelo que no se haya probado llega a todos los jugadores a la vez;
  - detener la versión en Play no la retira de quien ya la instaló;
  - UMP en un país del EEE (D-057) no se verá con jugadores reales antes del lanzamiento.
- **Cambia:**
  - 01 §8.1, el escalonado y la beta cerrada;
  - 08 §2.1 (punto 6) y §12.1 (etapa 5, el comentario del workflow y el texto sobre pistas y tránsito);
  - 08 §13.1: #1, #19, #20 y #22; §15.3 #4 y #7; y la tabla de Dependencias;
  - 08 Preguntas abiertas, la 3;
  - 08 §14.3, la pista interna y la prueba cerrada LatAm;
  - 09 §0.4 (Fases 2 y 4), §2.3 (Fases 2 y 4), §5, §6.1 y la pregunta 4 de Preguntas abiertas;
  - `GDD.md`: el resumen de fases (Fases 2 y 4) y los pasos 14 y 16 de §9;
  - `docs/qa/regresion-release.md`;
  - `docs/dev/iap.md` §3, pasos 1, 3 y 4;
  - `docs/dev/release.md` (introducción y §4 bis);
  - `docs/dev/pendientes-del-dueno.md`.
- **Versión que se sube** [P] (propuesta del integrador): la del commit que integre los arreglos de la jornada 4 de la prueba en el teléfono. El AAB `1.0.0 (10000)` del release 40630794359 no se sube, así que el versionCode 10000 sigue libre.

### D-033, añadido del 2026-10-28: confirmación neutral al corregir la edad (B12)

- **Fecha:** 2026-10-28. Registrada el 2026-10-28 por el agente de la rama `tanda6/fx-j4-ui`, a partir de la observación 3 de la jornada 4 en el teléfono (`docs/dev/traspaso/pruebas-telefono-2026-10-27-completa-j4.md`): un toque accidental en «Corregir mi edad» y el siguiente en «Under 13» (en español, «Menos de 13») bloquearon el perfil sin confirmar.
- **Pregunta al dueño (por el coordinador):** «¿Corregir la edad desde Ajustes debe pedir confirmación (neutral, igual para toda banda)?». **Respuesta del dueño:** «la primera hagamos de acuerdo al estandar».
- **Qué cambia en B12 (Q-B12-1 y 08 §7):**
  - al corregir la edad desde Ajustes, tras elegir una banda **distinta** de la guardada y antes de guardarla, sale una confirmación **neutral e igual para las tres bandas**: el mismo título, la misma frase con la banda tal como sale en la pantalla de edad («La edad solo se puede corregir una vez y no se puede deshacer») y los mismos dos botones, «Volver a elegir» y «Confirmar», del mismo tamaño;
  - no empuja ni disuade: nada de «¿seguro que eres menor?», ni avisos solo para `<13`, ni pistas de qué banda conviene;
  - «Volver a elegir» (y el Atrás de Android) no gasta la corrección; la X cierra sin cambiar nada;
  - la misma banda sigue sin gastar la corrección y sin confirmación; la pantalla de edad del primer arranque no lleva confirmación;
  - la analítica no cambia de forma: `af_settings_change` con `value = age` y `af_consent` con `step = changed`, solo al confirmar (08 §8.2).
- **Textos [P]:** `consent_age_confirm_title`, `consent_age_confirm_body`, `consent_age_confirm_back` y `consent_age_confirm_ok`, en `borrador` en español y en `traducido` en inglés y portugués (borrador por IA sin revisión nativa: la compuerta de release lo detiene hasta revisarlo); pendientes del abogado como los demás textos de B12 (Q-B12-5).
- **Se actualizan:** 08 §7, `docs/dev/consentimiento.md` §1.7 y el guion de la prueba completa en el teléfono.

### D-051, añadido del 2026-10-28: la intro de la marca dura unos 3,5 s y el vertido va fluido

- **Estado:** pedido por el dueño en el chat el 2026-10-28: «podemos dejar la animación del principio también un poco más fluida, se siente muy tosca el vertido como pocos fotogramas, y podemos dejarla un segundo más para apreciarla mejor». Cambia la duración del añadido del 2026-10-26 (2,5 s); lo demás de ese añadido sigue.
- **Decisión** [P]:
  1. **Duración: unos 3,5 s** (`BrandIntro.AnimMs`), un segundo más, **repartido** en el vertido y la entrada del nombre, no como pausa al final: el chorro cae al agua en 800 ms (antes 520), los aros hasta 1.750 ms (antes 1.250), el açaí tiñe el río hasta 2.500 ms (antes 1.800), «Tintalú» entra de 2.350 a 2.900 ms (antes 1.650-2.000) y el emblema se sostiene hasta 3.500 ms (antes 500 ms de sostén; ahora 600). Con Reducir movimiento, igual que antes: quieta 1 s.
  2. **El vertido, fluido.** Causa medida en el código: la intro se movía mientras la carga corría en bloques síncronos, y entre bloque y bloque solo se pintaba un fotograma; el vertido (los primeros ~0,5 s) caía entero dentro de la carga y se veía con 2-3 fotogramas (en el teléfono, 88 fotogramas en 2,5 s en la primera apertura y 114-117 con caché, casi todos después de la carga). Ahora el movimiento (del chorro al nombre entero, `BrandIntro.SettledMs`, 2.900 ms) empieza en un hueco de la carga y **la carga se para mientras se mueve**: cada fotograma sale a su ritmo, interpolado por tiempo real. Lo que queda de la carga va en el último tramo, con la escena quieta (el emblema y el nombre); si no cabe, sale «Cargando…» como hasta ahora. El reloj de la intro cuenta desde que empieza a moverse.
  3. **El açaí avanza sin saltos:** cada tramo del río entra mientras el frente lo cruza, en lugar de encenderse entero de golpe (10 tramos en 1,7 s eran pasos visibles).
  4. **Observación 1 de la jornada 4 (≈ 1 s de petróleo liso en la primera apertura):** el vertido no empieza hasta saber que su primer fotograma ya está en la pantalla (la GPU termina una lectura mínima pedida tras él; tope de 1,2 s), así que nunca corre sin verse; mientras, la escena quieta (la cuia sobre el río) es lo que hay en la pantalla. Lo que no depende del juego (el arranque del motor y, en la primera apertura, la compilación de los sombreadores sin caché del sistema) queda medido con las sondas nuevas; si resulta ser eso, rellenarlo exige tocar la pantalla del motor (ver la pregunta abierta del carril).
- **Coste en el arranque:** con caché, la carga cabe en el sostén y el arranque dura lo de la intro (unos 3,5 s más el primer hueco); en la primera apertura, lo que la carga no quepa en los 600 ms de sostén sale como «Cargando…» (unos 0,4 s con la carga de la jornada 4). El primer toque en el nivel 1 sigue muy por debajo de los 20 s (06 §2.2, 09 §6).
- **Sondas:** `AF_STATE brand_intro` con `anim_ms` 3.500 y, nuevos, `visible_ms` y `visible_by` (`gpu`, `cap` o `no_readback`), `play_ms`, `motion_frames`, `frame_p95_ms` y `frame_max_ms`; `AF_PERF boot_steps` con `visible_ms`, `play_ms`, `motion_hold_ms`, `proc_awake_ms` (del arranque del proceso al Awake) y `proc_visible_ms` (del arranque del proceso al primer fotograma de la intro en la pantalla). Valores esperados en el teléfono: `motion_frames` ≈ 170-175 a 60 Hz, `frame_p95_ms` ≤ 20, `frame_max_ms` ≤ 40, `motion_hold_ms` ≈ 2.900, `visible_by` `gpu`.
- **Carril:** `tanda6/fx-j4-intro`.
- **Cómo revertir:** con una decisión nueva del dueño.
