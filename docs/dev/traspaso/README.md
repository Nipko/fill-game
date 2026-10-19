# Traspaso al equipo de escritorio (2026-10-09)

Escrito por la sesión de Claude Code del portátil (Windows 11 ARM64) para la sesión que continúa en el equipo de escritorio. Léelo entero junto con `CLAUDE.md` antes de tocar nada.

## 1. Resumen

> *Estado del 2026-10-19* (tanda 4: **ola 6 cerrada en `cb2460e`** y probada en el teléfono en `5d2a569` con resultado «cumple», `pruebas-telefono-2026-10-18-tanda4-ola6.md`; **ola 7, parte A, integrada en `6523058`** y probada en el teléfono en `c5e1c58` con resultado «cumple» y una observación, `pruebas-telefono-2026-10-19.md`; **parte B (C1b, C1c y D3) en su rama**; escrito por D3):
> - **Ola 7 por carriles** (ramas leídas el 2026-10-19; las de la parte A ya están en `integracion-fase1-fase2`):
>   - **Unity A** (`tanda4/o7-unity-a`, `45b721d`; parte A):
>     - FX8 (`ae863b2` y su revisión `6dfd080`), los pendientes de código de la ola 6. El fallo por comprobar de M3.5 (abajo, en el estado del 2026-10-18) se reprodujo (con visto 5 y max 12, la placa del mundo 3 en el sitio de la del mundo 2) y está arreglado: al cambiar los mundos futuros sin cambiar de vista, los rótulos se recolocan como al abrir. La ventanita del hito de la tarjeta previa y la tarjeta de la parada usan la temporada del primer nivel de la parada de destino (07 §2.1). Tras usar una Pista, TiTino no dice en la victoria las frases n.º 4 y 8 de su banco.
>     - B2 (`9a68a69` a `45b721d`; informe en `docs/setup/medida-aab-b2.md`): la medida del AAB del juego, con las cifras de «Tamaño», abajo. Es la B2a, con los niveles 1-300; la B2b (repetirla con 1-700) espera a que C1b esté en la rama.
>   - **Unity B** (`tanda4/o7-unity-b`, `9e2c7d5`; parte A):
>     - TJ3 (`0ae1eea`, `83f4bf3` y `cb7e483`): el gesto de sacar (su cuia viaja en arco a la boca, se llena y vuelve con unas gotas: 880 ms, como un vertido), el revelado en la boca, la suelta en la etapa nueva (f) de R10 (su cuia se descuelga y la vasija se vuelca boca abajo sobre el jirau), la intro con la Tinaja, Marea y reanudar sin formato nuevo, con `sfx_tinaja_scoop`, `sfx_tinaja_reveal` y `sfx_tinaja_release`.
>     - TJ4 (`096b82c`, `809e427` y `9e2c7d5`): la Luciérnaga con la tinaja (solo late la vasija y nunca cobra sin efecto), Morpho con tableros con tinaja, la tarjeta del estreno del 323, el consejo del 324 [P], el campo `tinaja` de `AF_STATE` y `ui_tinaja_show_contents` = false (D-019, Q-Ti5-A).
>     - Con TJ1-TJ4, la Tinaja está entera en el cliente, pero ningún pack publicado la usa todavía (C1c, abajo).
>   - **Núcleo** (`tanda4/o7-core`, `1429976`; parte A): FX9 estabiliza las pruebas «sin asignaciones» del bucle caliente, que fallaban de vez en cuando con la suite completa. `AllocationMeter`, solo dotnet, calienta el bloque y se queda con el mínimo de hasta 5 medidas, con los mismos umbrales: 20 de 20 ejecuciones seguidas sin fallos. Sin cambios en el núcleo. Las dos pruebas de Unity con una sola medida quedan anotadas en el CHANGELOG del paquete para una tarea aparte.
>   - **FX10** (`tanda4/o7-fx10`, `c313132`; parte A), las observaciones 1-3 de la prueba de la ola 6:
>     - ninguna línea de la sonda pasa de 960 bytes, y las que no caben salen partidas;
>     - las placas de los mundos futuros quedan enteras y fuera de la ventanita de casa;
>     - `BuildDev` borra antes el APK anterior y el de Gradle, así que el `dev.apk` ya no crece entre builds.
>   - **Geometría del Mapa** (`tanda4/o7-geo`; parte A; adelantadas de la ola 8):
>     - M5.1 (`5615cbb` a `cdff504`): la geometría de producción, con la tierra y el Casiquiare de Natural Earth y las líneas de HydroRIVERS en las dos vistas (desviación [P]: pregunta 65); `sources.json` lleva los hashes y las licencias, y las descargas quedan fuera de git;
>     - M5.1c (`484cfa5` a `4a64fbe`): el cliente con esa geometría. Las cintas no tienen dientes (puntos a 1,5 anchos del trazo y bisel donde el inglete pasaría de 1,25 [P]), la franja del viaje del 81 baja de 7,5 a 2,4 ms y las placas de la Cuenca sin sitio junto a su parada buscan en una rejilla de la zona visible.
>   - **Niveles 301-700** (`tanda4/o7-c1b`, `4023188`; parte B, sin integrar):
>     - GEN79 (`2232b9a` a `b54854a`) genera los mundos 7-9 con D-027, en sus dos variantes;
>     - C1b (`83a8a94` a `017f53f`) los publica en el cliente con la **variante b** (`published_variant` = "b" en `campaign_v2.json`; la a, en `tools/out/salida_a/`). El cliente llega al 700: banners del 301, el 421 y el 561, el Brote de dos hojas en el 343, la Flor en el 403 y el fin de contenido tras el 700; el inicio del 701 conserva el fondo y la ventanita de la parada 69;
>     - C1c (`ec3390c` a `4023188`) deja el 323 sin la Tinaja, porque la compuerta de PF1 no consta en verde (pregunta abierta de D-019; sección 6, paso 4), y juega el 323 real de la variante a de punta a punta en `Tinaja323EndToEndTests` (PlayMode).
>   - **Documentación** (`tanda4/o7-docs`): D3 (esta puesta al día).
>   - **Fuera de la tanda:** el Reto del río se encargó a un tercer equipo (`docs/dev/traspaso/spark-reto-del-rio.md`, en `integracion-fase1-fase2`), con las respuestas del dueño a sus R-Q1-R-Q10.
> - **Verificación de la parte A** (escritorio x64, `6523058`):
>   - `dotnet build -warnaserror`, 0 avisos; `dotnet test`, 981 (975 + 6 omitidos);
>   - `validate_schema.py`, 103/103; `stops.py --check` (70 paradas, 308 textos `map.*`), `geometry.py --check` (3 avisos de miradores sin texto) y `generar_mapa.py --check` OK; 176 tests de `tools/river-map`; `check_audio.py`, 173/173;
>   - Unity compila; EditMode, 2.744 (2.741 + 3 omitidos); PlayMode, 1.334 (1.175 + 159 explícitos de capturas);
>   - `dev.apk` de 64,81 MB, igual en dos builds seguidos.
> - **Tamaño (B2, 2026-10-18, niveles 1-300):**
>   - el AAB del juego (`AudioAssetSetup.BuildGameAab`) pesa **51,87 MB**, con 50,80 MB de descarga en Play, frente al tope de 80 MB; el de humo, 40,03 MB. 16 KB correcto en los dos;
>   - margen: 28,13 MB hoy; 17,11 MB con el arte de B0 a 1×; 6,58 MB con el arte a 2×. Con los SDK de 08 §6.1 (≈ 12-18 MB), a 1× queda entre 5,1 y −0,9 MB, y a 2× **no cabe** (85,4-91,4 MB). Hacen falta Addressables y PAD (08 §6.2), recortar las fuentes (6,88 MB sin comprimir, cuatro veces y media lo estimado) o las dos cosas. Lo medirá de verdad M5.2 con el arte;
>   - los niveles 301-700 suman ≈ 0,05 MB en el AAB (estimación; la medida es la B2b);
>   - **aviso de `LocReleaseGate`:** con `com.amazonfill.dev` solo avisa, pero encuentra **811 problemas** que pararían un build de release. Son 153 claves sin texto en `en` y 154 en `pt-BR`, 126 textos culturales de `strings.csv` en `pendiente_asesor` y 683 filas de `facts.csv` sin `aprobado_es`; cada idioma incompleto cuenta como un solo problema (1 + 1 + 126 + 683 = 811). Es lo esperable hasta el asesor y las traducciones.
> - **Qué probar en el teléfono con la parte B** (cuando la sesión principal integre C1b, C1c y D3; perfil limpio con `pm clear`; sondas en `docs/dev/prototipo-v0.md`):
>   1. **301-700 (C1b):** del 300 al 301 con «Siguiente», con el banner del 301 y el relevo de Rosa y el bufeo en `teotonio` en su viaje. Banners del 421 y del 561. Estrenos del Brote de dos hojas (343) y de la Flor (403), con su tarjeta. Los hitos de 14 cuias en 3 filas de 5/5/4 a 360 × 780 (por ejemplo, el 580 y el 620). Con `--ei af_max_level 699` y el 700 ganado, el Mapa del fin de contenido y «El río sigue creciendo»; el inicio del 701 conserva el fondo y la ventanita de TiTino.
>   2. **El 323 (C1c):** sale como nivel normal, sin la Tinaja ni su tarjeta (variante b).
>   3. **La tarjeta de la parada 32** (321-330): los Datos de Pará como «Otro dato» (D-022, pregunta 50).
>   4. **Del README de `tools/river-map` (M5.1), Mapa y banners de viaje:**
>      - al llegar a Santarém (victoria del 10) se llena el Tapajós y al llegar a Boca Manu (380), el Manu;
>      - el Japurá se llena en la llegada a Tefé (70) y no en la de Leticia (80);
>      - el nombre de Caballococha sale en el viaje del 120 y no en el del 130;
>      - los ríos de la Cuenca se ven como líneas limpias y no deshilachadas (capturas 13 y 15-18);
>      - el banner del viaje del 81 se monta sin tirones.
>   5. **Lo que quedó sin probar en la parte A:** la temporada en la tarjeta de la parada y en la del hito (FX8); TiTino en la victoria tras usar una Pista; la fluidez del banner del 41 y del 81, en vídeo; y que un segundo build seguido no crezca.
>   6. **Regresión:** 1-12, 41, 63, 100, 143, 183, 243 y 283, más 301, 343, 403 y 561, sin excepciones en logcat.
> - **Fallo conocido** (anterior a la ola 7; anotado en `6523058` y sin arreglar): tras «ponerse al día», el agua del último tramo no se ve si ese tramo necesita más cintas que el de partida (Mapa desde la pausa del 41 y del fin de contenido). La cinta nueva que crea `FrontRibbonAt` no pasa por `FitRibbon`. En la prueba de la parte A no se distinguió a simple vista: hace falta una captura ampliada del último tramo.
> - **Observación de la prueba de la parte A:** en la vista Mundo los ríos se ven quebrados, con tramos rectos y esquinas marcadas. Entra en la pregunta 65 (parte 3).
> - **Pendientes del dueño tras la ola 7:** en la sección 6, paso 4.
>
> *Estado del 2026-10-18* (tanda 4: **ola 5 cerrada en `9fc15f9`** y probada en el teléfono en `a9b0376` con resultado «cumple», `pruebas-telefono-2026-10-17-tanda4-ola5.md`; **ola 6 en integración**; escrito por M4.6):
> - **Integrado en la ola 5:**
>   - el carril del Mapa: las paradas 421-700 (M4.5b, k = 42-69), así que los datos del Mapa cubren ya 1-700;
>   - Unity A: los marcadores del Mapa y «ponerse al día» (M3.5), la tarjeta de la parada, el Muestrario y los miradores (M4.1), «Norte arriba» y el salto de Rosa en Tarapoto (M4.4) y TiTino en el inicio (TI6c);
>   - Unity B: la pantalla de arranque con TiTino (TI6d), la tarjeta «Fruta nueva» (F5), TiTino en Nivel fallido y tras Volver a la orilla con `stats.levels_won` (TI6b) y TiTino en el tutorial de 1-3 y 6 (TU1a);
>   - el audio definitivo: la entrega 1 de un agente de Gemini (173 WAV a 16 bits con su manifiesto de licencias, `ac837b0`) y AU3, que hace sonar el juego con ella y deja el audio por código de respaldo (`4476796`);
>   - la documentación de la ola 4 (FXD2) y la decisión del dueño del 2026-10-17 (opción A): con el norte arriba solo salen las placas de los 3 mundos futuros siguientes (`f2f5081`; D-013, punto 5).
> - **Verificación del cierre de la ola 5** (escritorio x64, `9fc15f9`):
>   - `dotnet build -warnaserror`, 0 avisos; `dotnet test`, 981 (975 + 6 omitidos);
>   - `validate_schema.py`, 103/103; `stops.py`, `geometry.py` y `generar_mapa.py --check` OK; 142 tests de `tools/river-map`; `check_audio.py`, 173/173;
>   - Unity compila; EditMode, 2.628 (2.625 + 3 omitidos); PlayMode, 1.151 (1.021 + 130 explícitos de capturas);
>   - `BuildDev.BuildAndroidDev` (x86_64) OK: `dev.apk` de 64,59 MB (el audio va en Vorbis), con los permisos de siempre (`INTERNET`, `VIBRATE` y el interno de Android).
>   - El `dev.apk` del portátil pesó 83,8 MB (observación 1 de la prueba): falta ver con `unzip -v` si allí el audio se importa sin comprimir.
> - **Ola 6 (2026-10-18), por carriles:**
>   - **Unity A:** FX6 (el presupuesto de los pasos cuenta desde el último vertido tocado: adiós al `step_budget` de la victoria del 280, observación 2 de la prueba de la ola 4; y `sting_fruit_new` con gancho), F4 (la ficha «Frutas del río» y «Sobre la Amazonía» en Ajustes), M4.2 (la secuencia del Mapa del 10) y M4.3 (el Mapa desde la pausa, «Ver mapa» en la victoria del `10k`, el fin de contenido y el modo `worlds`), revisadas en la rama `tanda4/o6-unity-a` (`6420aea`, la revisión de M4.3). SE2 (la LUT de temporada en los fondos, pregunta 32 b) ya está commiteada en esa rama (`56ce793`), con su revisión pendiente.
>   - **Unity B:** Morpho (MO1, la lógica en un hilo con el corte de `hint_timeout_ms`; MO2, la ranura «Pista», la mariposa y las respuestas honestas; MO3, la pista por inactividad y su interruptor en Ajustes) y TU1b (TiTino presenta a Morpho en el 12 y se acreditan 3 Morphos una sola vez), revisadas en `tanda4/o6-unity-b` (`83f69bd`, la revisión de MO3; `3e4b104` es su implementación, sin los cambios de la revisión). TJ2 (los overlays de la Tinaja y su cuia) seguía pendiente.
>   - **Ya en `integracion-fase1-fase2`:** FX7, los arreglos de la prueba de la ola 5 (`be61933`: los remates esperan lo que dura el clip de la toada, la sonda `AF_LAYOUT` del Mapa sale en partes de ≤ 3.900 bytes y las placas de la Cuenca no pisan los controles ni la canoa), y VA1b, adelantada de la ola 7 (`ff31706`: el Brote de dos hojas y la Flor de vitória-régia en el cliente, con sus tarjetas de estreno del 343 y el 403).
>   - **Documentación:** M4.6 (esta puesta al día; sondas en `docs/dev/prototipo-v0.md`, preguntas 55-64 en `docs/GDD.md` §6.3, entre ellas AU3 [P] (pregunta 64), el audio, sin número D hasta que el dueño lo ratifique).
> - **Dónde está cada función** (con la ola 6 integrada):
>   - **Mapa del río:** los datos de las paradas 0-69 (1-700) y el río de vidrio de 1-700; la pestaña del inicio desde el 10; marcadores (la canoa de Capi con sus banderas y el guía, Rosa o el bufeo en los mundos 7 y 8; mini cuias, baliza, boyas y la casa de TiTino), «ponerse al día», tarjeta de la parada, Muestrario y miradores, vistas Mundo y Cuenca y «Norte arriba»; el viaje de mundo dentro del banner (M3.6); y, en la ola 6, la secuencia del Mapa del 10, el Mapa desde la pausa sin salir del nivel, «Ver mapa» en la victoria del `10k` y el viaje del fin de contenido. Faltan el arte final (las siluetas, las banderas y las texturas son provisionales y no salen en release), la fuente de los datos en la tarjeta (pregunta 57) y la geometría de producción con Natural Earth y HydroRIVERS (M5.1, ola 8). El relevo de Rosa y el bufeo en `teotonio` ya está en el viaje del banner (M3.6: la canoa se para, entra el bufeo y se va Rosa) y en el guía del Mapa (`GuideAt`, M3.5), con sus tests, pero en el teléfono no se ve hasta que C1b publique el 301, porque el cliente acaba en el 300.
>   - **TiTino:** el rig provisional (TI4) y sus apariciones de D-016: la pantalla de arranque, el tutorial (1-3, 6 y, desde TU1b, la presentación de Morpho en el 12), la victoria en sus hitos (casi nunca cabe: pregunta 53), Nivel fallido, el aviso tras Volver a la orilla, la ventanita del inicio (poses por hora, saludo y Dato del perezoso) y la presentación del Mapa del 10. Sus frases y nombres, pendientes del asesor; su arte, del artista.
>   - **Frutas del río:** el calendario y las 20 líneas por fruta de 1-260 y las series de río de 261-300; la línea de la victoria con su prioridad (D-022); la tarjeta «Fruta nueva» (F5); y, desde F4, la ficha de solo lectura (desde la línea de la victoria y desde Ajustes › «Sobre la Amazonía»), con `af_fact_page_open` como sonda. Faltan las líneas 17-20 del cacao y del arazá y el visto bueno del asesor.
>   - **201-300:** se juegan en el cliente (C1a, ola 4), con el Cacao en el 201, la Liana de cuenta en el 243, la Hoja atada en el 283 y el fin de contenido tras el 300, que desde M4.3 pasa por el Mapa antes de «El río sigue creciendo». Los packs de 301-700 no se llegaron a generar en P7b-P7d (varias celdas de `campaign_v2.json` no colocaban); los generó GEN79 (ola 7, 2026-10-18) con las recomendaciones de la propuesta de los mundos 7-9 que aceptó el dueño (D-027), en sus dos variantes (la a en `Assets/_Project/Data/Levels/` y la salida B en `tools/out/salida_b/`), y no se publican hasta C1b (ola 7): la Tinaja (323), el Brote de dos hojas (343) y la Flor (403) solo se ven por ahora en los tests y en sus capturas.
>   - **Morpho:** desde el 12 (`hint_unlock_level`), la ranura «Pista» tras Cuia extra (Revelar pasa detrás), con stock, 100 semillas o vídeo; cobra solo cuando da un vertido; las tres respuestas sin vertido son honestas y gratis; la pista por inactividad pulsa el origen tras 12 s sin tocar, desde el 3, y se apaga en Ajustes. El factor de un teléfono de clase A sigue sin medir (8× [P]).
>   - **Audio:** suenan los 173 WAV de la entrega 1 (todo `"src":"clip"` en la prueba de la ola 5). Los mundos 10-13 siguen con el audio por código. Pendiente del asesor: 24 piezas, de ellas 17 con una especie o región sustituta, no amazónica (10 de la fauna y 7 efectos, entre ellos el boto); y comprobar de oído que en `amb_w01_fauna_02` no se entiende ninguna voz. Las tres [P] de AU3 esperan al dueño (pregunta 64), sin número D hasta que las ratifique. El audio pesa 16,91 MB en un AAB de 51,51 MB con la escena del juego (08 §6.1); el AAB de humo (`BuildSmoke`) no lo lleva, así que la compuerta de tamaño del CI debe medir uno con la escena del juego (pendiente de B2 y del CI).
> - **Qué probar en el teléfono con la ola 6** (cuando la sesión principal la integre; perfil limpio con `pm clear`; sondas en `docs/dev/prototipo-v0.md`, «Sondas de las olas 4-6»):
>   1. **Ficha (F4):** tocar la línea de fruta de una victoria abre la ficha (`fact_page_open` con `"from":"victory"`) y al cerrarla se vuelve a la victoria tal cual. También desde Ajustes › «Sobre la Amazonía» (`"from":"settings"`). Las líneas no reveladas, en silueta con «Nivel n».
>   2. **Mapa del 10 (M4.2):** del 1 al 10 con «Siguiente»: tras la victoria del 10 sale la secuencia (`map_open` con `"source":"l10"`): TiTino se presenta con su bocadillo, la placa del Dato del río 1 y la canoa sube a Santarém en ≤ 10 s; un toque la salta. Repetirlo pulsando Inicio en esa victoria y cerrando la app: sale una vez al entrar al inicio. «Jugar» abre el 11.
>   3. **Más entradas del Mapa (M4.3):** desde el 31, Pausa › «Mapa del río» y «Volver al nivel»: sin perder canoa ni reiniciar. «Ver mapa» en una victoria del `10k` (el 20): vuelve a la victoria. Con `--ei af_max_level 299` y el 300 ganado: el Mapa con el viaje del último tramo y después «El río sigue creciendo».
>   4. **Morpho (MO1-MO3 y TU1b):** en el 12, TiTino presenta la ranura «Pista» y la insignia pasa a «+3» (`hint` con `"phase":"gift"`); la mariposa vuela al origen y salta al destino, y las dos cuias laten. Si Morpho no da vertido («Desde aquí no hay salida…», «No veo un vertido seguro…» o «No encuentro un camino…»), la respuesta sale en su tarjeta, con Deshacer y Volver a la orilla, y no cobra (`AF_PERF morpho` con `"charged":false`). Desde el 3, sin tocar 12 s, pulsa un origen (`idle_hint`); Ajustes › «Pista por inactividad» la apaga.
>   5. **Arreglos de la ola 5 (FX7):** con `af_max_level 560`, la sonda del Mapa sale en partes (`"part":k,"parts":n`) y todas son JSON válido; con 58, en la Cuenca, ninguna placa pisa «Norte arriba»; los remates de la llegada empiezan al acabar la toada.
>   6. **Presupuesto de los pasos (FX6):** ganar el 280 a ritmo normal no saca `step_budget`.
>   7. **Si entran:** la LUT de temporada en un ciclo B (SE2) y los overlays de la Tinaja (TJ2; hasta C1b solo en los tests).
>   8. **Regresión:** 1-12 seguidos, 41, 63, 100, 143, 183 y 243, sin excepciones en logcat.
> - **Fallo por comprobar (M3.5, visto al revisar M4.2):** al rehacer las siluetas durante «ponerse al día», `BuildSilhouettes` reutiliza la caja de la silueta de un mundo para otro sin recolocarla: se vio «Mundo 3 · Manaos · Nivel 41» en el sitio del mundo 2, junto a Santarém. Hay que reproducirlo y, si se confirma, arreglarlo en una tarea del carril del Mapa.
>
> *Estado del 2026-10-16* (tanda 4, **ola 4 cerrada en `8e1d65c`**; plan y notas de la tanda en la sesión del escritorio):
> - **Integrado en `integracion-fase1-fase2`:** la pantalla del Mapa con el río de vidrio (M3.3 y M3.4), el viaje de cambio de mundo dentro del banner (M3.6), el mundo 6 (201-300) en el cliente con las series de río de 261-300 (C1a), el rig provisional de TiTino y TiTino en la victoria (TI4 y TI6a), `pack_schema` 3 en el cliente (TJ1), la Hoja atada y los estrenos de las variantes (VA1a), el bench de Morpho (PF1), los preparativos del generador y de level-qa para 301-700 (P7b-P7d: P7b y P7d con commits parciales y P7c sin empezar; los packs no colocaban con `campaign_v2.json` tal como estaba; los generó GEN79 en la ola 7 con D-027, sin publicar en el cliente hasta C1b), las paradas 201-420 del Mapa (M4.5a, k = 20-41; los datos del Mapa se quedan en las paradas 0-41 hasta M4.5b, `e0ada1e`) y el carril de audio: los arreglos de la prueba de la ola 3 (FX5), el audio provisional por código con sus 61 efectos, Sonido, Música y Vibración en Ajustes y los hápticos (AU1), y el ambiente por mundo y la música adaptativa provisionales (AU2). El dueño aprobó el permiso `VIBRATE` (D-026).
> - **Verificación del cierre** (escritorio x64, sobre `8e1d65c`): `dotnet build -warnaserror` con 0 avisos; `dotnet test`, 981 (975 + 6 omitidos); `validate_schema.py`, 103/103; `stops.py`, `geometry.py` y `generar_mapa.py --check` OK y 127 tests de `tools/river-map`; Unity compila; EditMode, 2.601 (2.598 + 3 omitidos); PlayMode, 981 (864 + 117 explícitos de capturas); `BuildDev.BuildAndroidDev` (x86_64) OK, con un `dev.apk` de 67,10 MB y los permisos `INTERNET` (ya estaba), `VIBRATE` (nuevo, aprobado) y el interno de Android.
> - **Canon de la ola 4 en el GDD** (FXD2, versión 1.13 de `docs/GDD.md`): las series de río de Frutas del río (`kind` = `river` [P], 08 §4.6 y contrato 31), la regla de los titulares del ciclo de debut (03 §9 y §12) y el estado del audio (07 §9).
> - **Pendientes del dueño tras la ola 4** (detalle en la sección 6, paso 4): las preguntas 52-54 de `docs/GDD.md` §6.3 (TiTino en la victoria); la insignia del nivel en otros idiomas; el rótulo del lugar sobre la silueta y la silueta invisible con 3 filas; la ventanita de casa con 0 canoas; la victoria con vista, que enseña poco sitio; el factor de Morpho en un teléfono de clase A; las líneas 17-20 del cacao y del arazá, sin redactar; y la curación de los mundos 6-9.
>
> *Estado del 2026-10-13:*
> - el paso 1 y el paso 2 de la sección 6 están hechos, y el paso 3 (elenco de ~50 especies) sigue pendiente;
> - de los arreglos de la prueba en el teléfono (paso 1b), la tanda 1 está integrada en `7d755c4` y probada en el teléfono el 2026-10-12 (cumple);
> - la tanda 2 está integrada en `integracion-fase1-fase2` en `94905b8`, con D-011 y D-012 implementadas enteras, y se probó en el teléfono el 2026-10-13 sobre `72a266a`: cumple, con seis observaciones (`docs/dev/traspaso/pruebas-telefono-2026-10-13.md`);
> - lo que la tanda 2 deja al dueño está en las preguntas 41-44 de `docs/GDD.md` §6.3 (decididas el 2026-10-13, D-015).
>
> *Decisiones del dueño del 2026-10-13* («Nos vamos por las recomendadas y necesitamos seguir avanzando»; registro en `docs/dev/decisiones.md`):
> - **D-013, el Mapa del río:** remontar el Amazonas hasta una de sus nacientes, con una parada cada 10 niveles, su fondo de tablero y TiTino como protagonista. Todas las preguntas de la sección 16 de `docs/research/mapa-del-rio/propuesta-mapa-del-rio.md` con su recomendación (Q4-D: el viaje de mundo va dentro del banner), más T1-A y T6-A de TiTino y Q1-A de Frutas del río. Sin implementar: fases F1-F9 de la propuesta;
> - **D-014, Aguas estancadas por bucle:** arregla el bucle sin salida del nivel 66 que encontró el dueño (02 R13 ampliada; se declara si el cierre de vertidos útiles se agota con ≤ 64 estados sin victoria). Con la pregunta 46, decidida el mismo día en la opción (a) (D-020), el cierre recorre todos los vertidos legales, porque el 66 tiene capacidades mixtas y con solo los útiles se declaraba el bloqueo en tableros que se ganan;
> - **D-015:** las preguntas 39-44 de `docs/GDD.md` §6.3, decididas con su recomendación. **Acción del dueño:** conseguir un teléfono de clase A (Mali-G52 o Adreno 610) para medir el shader del líquido (pregunta 44);
> - quedaron dos preguntas nuevas de `docs/GDD.md` §6.3: la 45, cómo comparten la victoria del nivel `10k` la llegada del Mapa, la línea de fruta y la frase de TiTino (abierta entonces; decidida el 2026-10-14 en la (a): D-022), y la 46, si el cierre de D-014 recorre también los vertidos fútiles (decidida: sí, D-020).
>
> *Estado del 2026-10-14* (tanda 3):
> - **Integradas en `integracion-fase1-fase2`** (`649e611`): **T50** (documentación de D-013, D-014 y D-015), **T51** (Aguas estancadas por bucle en `LevelSession` con `DeadEndProbe`, con la pregunta 46 en la opción (a): el cierre recorre todos los vertidos legales; en el 66 sale justo tras el vertido 9), **T52** (Unity: el aviso sube por encima de las burbujas «Te faltan», el aviso y la tarjeta se ocultan al abrirse un modal del embudo, el banner con escala de fuente y `wait_ms` ≈ 0 en el Editor, que falta medir en el teléfono, y el Remolino legible) y **T55** (núcleo de la Tinaja, M13).
> - *Decisiones del dueño del 2026-10-13* (segunda tanda): a la pregunta «¿Aplico también las recomendadas al resto que sigue abierto (TiTino T2/T4/T5, Frutas Q2-Q7, idiomas Q1-Q12 y tinaja Q-Ti1..Q-Ti7)?», respondió «Si»; a las 27-38 de `docs/GDD.md` §6.3, «Sí, aplica las recomendadas también»; y la 46 quedó en la recomendada. Registro en `docs/dev/decisiones.md`:
>   - **D-016, TiTino:** despierta sin palabras en la pantalla de arranque, acompaña el tutorial de 1-3, 6 y 12 y sale en un aviso tras Volver a la orilla en 1-30; nombre «TiTino»; ninguna mascota fija en el muelle;
>   - **D-017, Frutas del río:** 20 líneas por fruta desde el nivel 1, ficha para releer en la 1.0, sin premio, bajo «Inicio» en una ranura de alto fijo;
>   - **D-018, idiomas:** ES-419, PT-BR y EN en la 1.0 y los 11 de Magic Sort al salir al mundo; base de localización ya, sin paquetes; **aprobados** Unity Localization 1.5.13 en la fase 3 y la partida de traducción de 1.300-3.000 €;
>   - **D-019, la Tinaja (M13):** contenido oculto en niveles Normales con tope, estreno en el 323 de la 1.0 si cumple su condición;
>   - **D-020:** las preguntas 27-38 y la 46, con su recomendación.
> - **T54**, documentación: registra D-016 a D-020 y aplica sus lotes al GDD (solo `docs/`).
> - **Qué probar en el teléfono con la tanda 3** (falta): el `wait_ms` del banner de mundo al entrar con «Siguiente» tras ganar el 10, el 40, el 80 o el 140 (`AF_STATE world_banner_art`). Con T52 queda en ≈ 0 en el Editor; en el teléfono medía 66-111 ms el 2026-10-13 (observación 4 de `docs/dev/traspaso/pruebas-telefono-2026-10-13.md`; pregunta 43).
> - Quedan para el dueño: las preguntas 18, 19 y 21-26 de `docs/GDD.md` §6.3. La 45 y las 47-50 quedaron decididas el 2026-10-14 con su recomendación, y la 51, nueva, con «traducción con ia» (D-022).
> - **Aviso (2026-10-14, al verificar T54 sobre `649e611`):** `dotnet test` da 815 superadas, 4 omitidas y **1 fallo**, `TinajaTests.Tinaja_NeverReceives_M13r2` (esperaba `Playing` y la sesión sale en `Stuck`). El tablero de esa prueba no se puede ganar (el color 1 tiene 2 dosis, así que nunca llena una cuia de 4), y el cierre de D-014 (`DeadEndProbe`, T51) lo declara atascado al crear la `LevelSession`. Es un choque entre T51 y T55, no de la documentación; hay que arreglar la prueba (un tablero que se pueda ganar) en una tarea de núcleo.
>
> El resto de este resumen es el del traspaso del 2026-10-09.

- **La rama para seguir es `integracion-fase1-fase2`**: junta todo lo hecho hasta hoy, compila y pasa todas las pruebas (sección 3). `main` sigue en `e8edd1f` (D-005 y D-006) y no se ha tocado.
- **Ya está hecho:**
  - el núcleo con las 9 mecánicas de la 1.0, el solver y el bot;
  - el generador con la dificultad escalonada y los packs de los niveles 11-200;
  - el bucle de partida de Magic Sort, con anuncios y compras simulados;
  - la intro «Llenar y teñir» con miquitos.
- **Falta, sobre todo, la integración en Unity:**
  - que el juego cargue los niveles 11-200;
  - que cada mecánica tenga su presentación;
  - las etiquetas de dificultad antes de jugar.

  Después hay que registrar D-007 y D-009 en el GDD, hacer el elenco de ~50 especies y el APK para el teléfono (sección 6).
- El dueño trabaja con Claude en modo autónomo: todos los permisos están en «allow» (solo quedan las «deny»). Sus preferencias de trabajo están en la sección 8.

### Reparto entre los dos equipos (decisión del dueño, 2026-10-09)

- **Escritorio (remoto, siempre encendido):** hace el trabajo de los pasos de la sección 6 sobre `integracion-fase1-fase2`. Al terminar cada paso verificado (sección 3), hace **commit y push de esa rama** a `origin`. No hace falta su teléfono ni su build de Android: el APK no está en git.
- **Portátil (con el teléfono):** el dueño actualiza (`git pull` en `integracion-fase1-fase2`), compila el APK de desarrollo (`AF.EditorTools.BuildDev.BuildAndroidDev`, con `--architecture arm64`) y lo instala por cable en el Samsung (`adb install -r Builds/Android/dev.apk`).
- Por eso, en cada push explica en el mensaje del commit qué probar en el teléfono (niveles, pantallas y extras de `adb` útiles).
- No trabajes los dos equipos a la vez sobre la misma rama sin hacer pull antes.

## 2. Ramas en GitHub (`origin` = github.com/Nipko/fill-game)

| Rama | Contenido | Estado |
|---|---|---|
| `main` | Hasta D-005/D-006 (vertido por tramo; tablero, cuia y vertido copiados de Magic Sort) | Estable |
| `fase2-bucle-partida` | Pista B: AF.Platform/AF.Save (guardado atómico), AF.Economy (semillas, canoas, comodines), AF.Ads/AF.IAP simulados, catálogo de niveles y progreso, pantalla de inicio, bandeja de comodines, embudo de D-008, victoria con Cosecha, reanudar el nivel. Además, las fichas de Magic Sort y la investigación cultural | Revisada (2 rondas) |
| `fase1-mecanicas-niveles` | Pista A: BoardRules con R10 por etapas y fútiles del anexo B; Hoja, Matapi, Liana (fruta y número), Aguas turbias, Remolino, Barro, Lluvia, Brote y Piraña en núcleo y solver; PRNG, bot ×40, barajado inverso; esquema `pack_schema` 2; generador `--spec` con el ciclo de D-009; `level-qa`; simulación económica escalonada | Revisada (2 rondas) |
| `intro-rio-miquitos` | Sobre la pista B: intro «Llenar y teñir» (el río llena las cuias y una tropa de miquitos deja caer fruta que las tiñe), 10 especies como datos, tarjeta «¡Miquito a la vista!», D-010 registrada; permisos en «allow» | Revisada (2 rondas) |
| **`integracion-fase1-fase2`** | Las tres anteriores fusionadas, más los packs `mundo_02..05_v1.json` (niveles 11-200) con sus `.meta`, este traspaso y los planes | **Verificada; seguir aquí** |

## 3. Verificación en el momento del traspaso (rama `integracion-fase1-fase2`, portátil ARM64)

| Comando | Resultado |
|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 avisos, 0 errores |
| `dotnet test dotnet/AmazonFill.slnx` | 707 superados, 4 omitidos |
| `python tools/validate_schema.py` | 40/40 PASS |
| `dotnet run --project tools/level-generator -- --handmade tools/level-generator/specs/tutorial_v0.json --out Assets/_Project/Data/Levels --report tools/out/tutorial_v0_report.md --check` | 141/141 PASS; pack idéntico |
| `dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/campaign_v1.json --levels 11-200 --out Assets/_Project/Data/Levels --report tools/out/campaign_v1_report.md --check` | Los 4 packs coinciden con el generado (≈ 340 s) |
| `pwsh -NoProfile -File scripts/unity-compile.ps1` | Compila |
| `pwsh -NoProfile -File scripts/unity-test.ps1 -Mode EditMode` | 1.193 tests: 1.191 pasan, 2 omitidos |
| `pwsh -NoProfile -File scripts/unity-test.ps1 -Mode PlayMode -TimeoutSec 1800` | 114 tests: 109 pasan, 5 explícitos (capturas) |

## 4. Preparar el equipo de escritorio

1. **Herramientas** (versiones fijadas en `CLAUDE.md`; nada nuevo sin el dueño):
   - Unity Hub y **Unity 6000.3.25f1** (changeset `e1dba0a9aba4`) con **Android Build Support** (OpenJDK, Android SDK y NDK). El dueño inicia sesión en el Hub y activa la licencia: los agentes nunca hacen `unity auth` ni `unity license activate`.
   - **.NET SDK 10.0.401** (`global.json` con `rollForward: latestFeature`).
   - **Python 3.14** y `pip install jsonschema`.
   - **Git** y **Git LFS** (`git lfs install`). Las fuentes, las imágenes y otros binarios van por LFS.
   - **PowerShell 7.2 o superior** (los scripts `.ps1` lo exigen).
   - **Unity CLI** (`unity`, beta; sirve para `run`, `test` y `recompile`) y, si se usa, `gh`.
   - **Android platform-tools** (`adb`) para instalar en el Samsung SM-S938B. **Usa un solo `adb`**: si hay dos instalados, se pelean.
2. **Clonar:**
   ```
   git clone https://github.com/Nipko/fill-game.git amazon-fill
   cd amazon-fill
   git lfs pull
   git checkout integracion-fase1-fase2
   ```
   Usa una ruta sin espacios.
3. **Arquitectura.** El portátil es ARM64 y los comandos de `CLAUDE.md` llevan `--architecture arm64`. **En un equipo x64:**
   - pasa `-Architecture x86_64` a `scripts/unity-compile.ps1` y a `scripts/unity-test.ps1`;
   - usa `--architecture x86_64` en `unity run` y `unity test`;
   - en x64 las DLL de Firebase, MAX y EDM4U sí cargan en el Editor (en ARM64 no), lo cual no afecta a lo actual: no hay SDK.
4. **Primera apertura.** Abre el proyecto una vez (Editor o `scripts/unity-compile.ps1`) para que Unity importe todo. Es normal que tarde.
5. **Hooks de git.** Los hooks (`pre-commit`, `post-checkout`, `post-merge`, `pre-push`) viven en `.git/hooks` del portátil y **no se clonan**. En el portátil llaman a la Unity CLI por el alias `%LOCALAPPDATA%\Microsoft\WindowsApps\unity.exe`. Sin ellos, verifica a mano con la sección 3 antes de cada commit.
6. **Avisos prácticos:**
   - Al ejecutar las pruebas, Unity reserializa `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset` y los `Nunito-Regular SDF*` de `Assets/_Project/Resources/AF/Fonts/`. Restáuralos con `git checkout --` antes de cada commit.
   - El build de desarrollo deja `Assets/Resources/PerformanceTest*` y cambia `ProjectSettings.asset` (`preloadedAssets`) mientras dura; al terminar los deshace solo.
   - Nunca dos procesos de Unity sobre la misma carpeta, ni batchmode con un Editor abierto.
   - Si el equipo se suspende, Unity se congela en mitad de una prueba. El portátil lo hacía de noche: deja el de escritorio sin suspensión mientras trabajan los agentes.

## 5. Cómo arrancar la sesión nueva

Abre Claude Code en la carpeta clonada, en la rama `integracion-fase1-fase2`, y pídele algo como:

> Lee CLAUDE.md y docs/dev/traspaso/README.md. Prepara y verifica el entorno (sección 3 del traspaso, con la arquitectura de este equipo) y continúa con el trabajo pendiente en el orden de la sección 6.

La sesión del portátil usaba la herramienta Workflow con equipos de agentes Opus (`.claude/agents/`): tech-lead → dev → revisión de código y de fidelidad → verificación adversaria de cada hallazgo → corrección. El guion del elenco (paso 3) está listo para relanzarlo.

## 6. Trabajo pendiente, en orden

### Paso 1 · Integración en Unity (la más urgente: el dueño quiere probar ya)

> **Progreso en el escritorio (x64).** El trabajo va en 5 olas de tareas pequeñas; cada ola se fusiona en esta rama ya revisada y verificada.
>
> **Ola 1 integrada el 2026-10-09:**
> - los niveles 1-200 cargan y se juegan con todas las mecánicas de la 1.0 en las reglas;
> - cada mecánica tiene un aspecto simple provisional;
> - las dosis turbias y el Barro se pintan bien;
> - la vista sigue al modelo en cada vertido;
> - AF_STATE informa de las mecánicas.
>
> **Ola 2 integrada el 2026-10-09:**
> - pasos de desbloqueo en el orden de R10 (con el punto fijo f′) y estados intermedios;
> - presupuesto de 2,8 s desde el toque;
> - persistencia de lo revelado (historial h2);
> - destellos, esquirlas y burbujas;
> - etiquetas D-009 en Jugar y en los modales.
>
> **Ola 3 integrada el 2026-10-10:**
> - etiquetas D-009 en el HUD y en Siguiente, chip de Aguas turbias y contador de Lluvia y Piraña;
> - aspecto y desbloqueo al estilo Magic Sort de Hoja, Brote, Matapi, Liana con fruta, Remolino, Barro, Lluvia y Piraña;
> - Aguas turbias en el shader (pardo con trama y «?»), revelado por la regla y frente de luz de la Luciérnaga;
> - tarjeta previa (apagada hasta la ola 4) y lógica de la Luciérnaga (regalo del 100, semillas, vídeo).
>
> **Ola 4 integrada el 2026-10-10:**
> - tarjeta previa de 1 toque (Difícil y Muy difícil) y tutorial de una pantalla en cada estreno, con el 100 combinado; las tarjetas quedan encendidas;
> - la Luciérnaga en la bandeja: ranura Revelar, selección de la cuia turbia, compra con stock, semillas o vídeo simulado, vuelo y aviso del regalo;
> - el Barro mate (arreglo de T16) y el banco de tests de mecánicas, que ahora se destruye en el acto.
>
> **Limpieza y verificación final (T18) el 2026-10-11:**
> - limpieza de los hallazgos menores de las revisiones (L1, L2a, L2b y L3): shader más barato en las bandas normales, Luciérnaga precalentada, tarjetas con 3 superficies compartidas, ganchos de GameScreen, avisos de la bandeja a 12 sp y tests más estrictos;
> - T18: los 14 pasos en verde (packs idénticos con `--check`, EditMode 1.390, PlayMode 322, 434 capturas, AAB de 36,97 MB, 16 KB correcto en AAB y APK, minSdk 26, targetSdk 36 y `dev.apk` de 66,23 MB).
>
> **El paso 1 queda cerrado** a falta de la prueba en el teléfono (portátil). Lo siguiente es el paso 2, y quedan los pendientes del dueño de abajo.
> - La caché de `LocalDateKey` (AF.Platform) la aprobó el dueño el 2026-10-11 y está aplicada (`.claude/rules/sdk.md`).
> - Los [P] que tiene que ratificar el dueño están en el informe de la sesión del 2026-10-11.
>
> Qué probar en el teléfono tras cada push: en el mensaje del commit.

Las tareas detalladas están en `docs/dev/traspaso/planes-integracion.json` (`integration_tasks` e `integration_notes_pista_A`). En resumen:

1. **Lector de `pack_schema` 2.** `LevelPackReader` (en `Assets/_Project/Scripts/Levels/`) solo acepta el 1. Tiene que leer:
   - los `attrs` de cada mecánica: `hoja`, `matapi`, `liana` con `key` o `n`, `remolino` con `partner`, `lluvia`, `brote` y `pirana`;
   - las dosis ocultas y el barro (color 15);
   - `caps`, `world`, `water_base`, `intro_mechanic` y las etiquetas.
2. **Catálogo con los packs nuevos.** `LevelCatalog` ya acepta varios packs: hay que registrar `mundo_02..05_v1.json` para jugar del 1 al 200, cada nivel con su mundo, su agua base y su especie de miquito.
3. **Presentación de cada mecánica**, con arte procedural en la línea de D-003 y la animación de desbloqueo como en Magic Sort (ficha `docs/research/magic-sort/ficha-magic-sort.md` §3: la marca de visto, el haz de luz, la tela que sube, el hielo que revienta, la llave que vuela al candado):
   - Hoja, Matapi, Liana con fruta y Liana con número;
   - Aguas turbias, con el glifo «?» que aún falta;
   - Remolino, Barro, Lluvia (con su contador en el HUD), Brote y Piraña (con su cueva).
4. **Tutorial de una pantalla** en el debut de cada mecánica (02 §9.2, regla 2) y **Luciérnaga regalada** en el estreno de Aguas turbias (nivel 100).
5. **Etiquetas de D-009 visibles antes de jugar:**
   - en los botones «Jugar» y «Siguiente», un chip «Difícil»/«Muy difícil» con su cinta ×3/×4;
   - en la tarjeta previa, los iconos de las mecánicas del nivel;
   - también en el HUD.
6. **Verificación completa** (sección 3), **commit y push**. El build de desarrollo y la prueba en el teléfono los hace el portátil (sección 1). En el portátil, por cable: `adb devices` lo muestra como `R5CY92CG81P`. Instala con `adb install -r Builds/Android/dev.apk` y recorre los niveles 1, 11, 41, 81, 101, 141 y 183 grabando con `adb shell screenrecord`.
   - **No uses** el extra `af_level_offset` para probar mundos: desplaza el número de nivel pero deja el tablero y el mundo del tutorial. Con los packs reales basta con `--ei af_level N`.

### Paso 1b · Arreglos de la prueba del dueño en el teléfono (2026-10-11): hazlos antes del paso 2

> *Estado del 2026-10-13:*
> - *La tanda 1 está integrada en `7d755c4` y se probó en el teléfono el 2026-10-12: cumple. Lleva P1, P3 y P4, con T-A, T-B, T-C, T-D y los criterios de la ola 3 de la hoja del 2026-10-09.*
> - *La tanda 2 está integrada en `94905b8`: P2 (avisos legibles), la tarjeta del miquito tras la tropa y T-F.1 con A2 (`35301f9`); T-E y T-G con B3 (`dd208a6`); y el banner de mundo y el tutorial de la Piraña con A4 (`94905b8`).*
> - *T-F.2 (nota de `planes-integracion.json`) se hizo en el paso 2.*
> - *La tanda 2 se probó en el teléfono el 2026-10-13 (`72a266a`): cumple. Resultados y observaciones en `docs/dev/traspaso/pruebas-telefono-2026-10-13.md`.*

El dueño probó el `dev.apk` (compilado sobre `80d37fb` o antes) y decidió **D-011** (`docs/dev/decisiones.md`). Las cuatro causas seguían en `7677085`. Tareas, causas con archivo y línea, pruebas y qué probar en el teléfono: `docs/dev/traspaso/pruebas-telefono-2026-10-11.md`.

- **P1:** el saldo no enseña la Cosecha hasta la pantalla de victoria, y la bandeja de comodines no se apaga antes de la celebración. La Cosecha se sigue cobrando en el instante lógico.
- **P2:** avisos y tarjeta «¡Miquito a la vista!» con duración por lectura (3-7 s). Ninguno sale durante la carga, la tarjeta previa o la intro (el regalo de Capi del nivel 8 hoy sale encima de la intro), y salen de uno en uno.
- **P3:** miquitos que entran trepando, sueltan igual de rápido, se quedan ≈ 1,5 s en reposo mientras ya se juega y se van en ≈ 0,5 s. T y el toque, sin cambios.
- **P4:** tras el vídeo de la Cosecha doble, el juego cuenta el doble y pasa solo al nivel siguiente.

**Tanda 1 probada en el teléfono el 2026-10-12** (`main` en `7d755c4`): P1, P3, P4, T-A, T-B, T-C, T-D y los overlays del 103, 163 y 183 cumplen. Hay cinco observaciones que no bloquean (`AfterRewarded` y la cueva llena sin probar en el teléfono, y los tests de Unity que tumban el adb). Resultados y medidas: `docs/dev/traspaso/pruebas-telefono-2026-10-12.md`.

#### Qué probar en el teléfono (tanda 2)

**Probada en el teléfono el 2026-10-13** (`main` en `72a266a`): los puntos 1-5 cumplen y, de los pendientes de la tanda 1, `AfterRewarded` y la medida de T-D (216 vertidos) también. Sin probar: «nada encima de la victoria», que no se puede forzar en el teléfono, y la Piraña con la cueva llena. Observaciones para el dueño (pregunta 41: el aviso tapa las burbujas «Te faltan»; y cómo se lee el Remolino) y para el escritorio: `docs/dev/traspaso/pruebas-telefono-2026-10-13.md`.

La tanda 2 está en `integracion-fase1-fase2` desde `94905b8`. Los criterios salen de la hoja del 2026-10-09 (T-E y T-G), de `pruebas-telefono-2026-10-11.md` (P2) y de los resúmenes de A2, B3 y A4. Los eventos de logcat están en `docs/dev/prototipo-v0.md` §c y en `Assets/_Project/Scripts/UI/DevProbe.cs`. Apunta los resultados en una hoja nueva, como `pruebas-telefono-2026-10-12.md`, y deja las capturas en `Logs/device/tanda2/` (fuera de git).

**Antes de empezar**
- **Las pruebas por adb, nunca a la vez que los tests de Unity** (observación 5 de `pruebas-telefono-2026-10-12.md`). `unity-test.ps1` arranca el adb del Editor y reinicia el servidor. El teléfono desaparece de `adb devices` hasta que se desconecta el cable y se desactiva y activa la depuración USB. Haz las pruebas por adb antes o después de los tests.
- Compila el `dev.apk` desde `94905b8` o posterior e instálalo (sección 1).
- Abrir un nivel: `adb shell am start -S -n com.amazonfill.dev/com.unity3d.player.UnityPlayerGameActivity --ei af_level N` (extras `--ei af_seeds` y `--ei af_canoes`, y `--es af_ads_fill no_fill` para que no haya vídeo). Perfil limpio: `adb shell pm clear com.amazonfill.dev`.
- Logcat: `adb logcat -s Unity | grep -E 'AF_STATE|AF_LAYOUT|AF_PERF|AF_ART'`. Para tocar, usa los centros de la última `AF_LAYOUT` de la pantalla que está a la vista.
- Los avisos y la tarjeta «¡Miquito a la vista!» no escriben línea propia. Mide cuándo salen y cuánto duran con `adb shell screenrecord` o con ráfagas de capturas, contra el `t` de `AF_STATE ready`.
- En el portátil ARM64, `WorldBannerArtTests` comprueba en EditMode el presupuesto de 8 ms del hilo principal, medido en el escritorio x64 [P]. Si falla allí, anótalo.

**1. Avisos de 3-7 s, de uno en uno (A2; D-011, punto 2)**
- **Regalo de Capi en el 8**, con perfil limpio (`pm clear` y `--ei af_level 8`) y sin tocar durante la intro. `AF_STATE extra_cuia_gift` sale en la carga, pero el aviso no se ve durante la intro. Sale con `AF_STATE ready`, sigue a la vista a los 5 s y se va hacia los 5,5 s (5.455 ms: 81 caracteres). Si se salta la intro con un toque, sale en ese momento.
- **«¡Miquito a la vista!» en el 5**, con perfil limpio y en el primer intento. No sale con `ready`: sale cuando la tropa ya se ha ido de la rama (≈ 1,5 s de reposo y ≈ 0,5 s de salida; si se saltó la intro, ≈ 100 ms después del toque). Dura ≈ 5 s (4.960 ms, el mono ardilla). Con la tarjeta a la vista, un toque en una cuia da `select`: no captura toques.
- **Luciérnaga en el 100:** `AF_STATE reveal` con `"phase":"gift"` en la carga. «+1 Luciérnaga» sale después de la tarjeta y de la intro, con `"phase":"gift_toast"`, y dura 3 s.
- **De uno en uno:** si coinciden dos avisos (por ejemplo, la tarjeta del miquito y «Te faltan {n}» de Revelar sin semillas), el segundo sale cuando se va el primero, sin pisarlo. **Abre el nivel con `--es af_ads_fill no_fill`:** con vídeo disponible, Revelar sin stock ni semillas abre el vídeo (`rv_reveal`) en lugar de «Te faltan {n}» (observación 3 de `pruebas-telefono-2026-10-13.md`). Por ejemplo, en el 135 con perfil limpio: `--ei af_level 135 --ei af_seeds 300 --es af_ads_fill no_fill`.
- **Nada encima de la victoria:** gana el 8 con el aviso de Capi a la vista. El aviso se oculta al empezar la celebración y no vuelve en la pantalla de victoria.
- Mira también si el aviso tapa la burbuja «Te faltan {n}» de «+1 cuia» cuando coinciden (pregunta 41 de `docs/GDD.md` §6.3, punto 4).

**2. Patrones e iconos de fruta (B3; D-012, decisión 4)**
- `AF_STATE load` trae `"patterns":{"on":false,"opacity":0.15}` en el 40 y el 7, y `"patterns":{"on":true,"opacity":0.20}` en el 63, el 100 y el 123. La primera carga de la sesión escribe `AF_ART fruit_icons <ms> ms` (en el Editor, ≤ 30 ms): anótalo.
- **63:** cada dosis visible lleva su icono, centrado y derecho, y el patrón se ve más marcado que en el 40. En un vertido, los iconos siguen a la banda al inclinarse, y el origen los conserva hasta que empieza su retorno.
- **40:** sin icono en reposo y con el patrón sutil.
- **7, con la cuia levantada:** el icono sale solo en la cuia levantada, ya al apoyar el dedo, y desaparece al bajarla. Ninguna otra cuia cambia (D-002).
- **100:** las dosis turbias van sin icono (solo la trama y el «?») y no chocan con la Luciérnaga.
- **123:** el Barro va sin icono. Con 3 filas, el icono mide ≈ 10 dp: comprueba que se lee.
- A tamaño real, en el 63 y el 123, anota para el artista (07 §3, «Iconos del cliente») si la Guanábana (1) y el Aguaje (4) se confunden, si los ganchos de la Guanábana parecen «judías» o si la hoja hundida del Río Negro (14) no se lee. El icono aparece y desaparece sin fundido: di si molesta.

**3. Sacudida de rechazo (B3; D-012, decisión 3)**
- Toca como origen la Hoja cerrada en el 43 y el Matapi en el 63, y una cuia vacía o una sellada en cualquier nivel. Las cuias con mecánica salen en el campo `mech` de `AF_STATE load`.
- Cada toque da `AF_STATE reject` con su `tap`. La cuia se sacude ±8 dp (24 px en el SM-S938B), 3 ciclos en 240 ms, sin amortiguar, y vuelve a su sitio exacto. En el 63, el embudo del Matapi late a la vez (240 ms).
- Un toque válido justo después, con la sacudida en marcha, funciona: `select` y, con el destino, `pour`.
- Grábalo con `adb shell screenrecord` o con una ráfaga de capturas.

**4. Banner de mundo (A4; D-012, decisión 2)**
- **41 sí, 42 no.** Al cargar el 41 por primera vez sale, antes de la intro, `AF_STATE world_banner` con `{"level":41,"world":3,"text":"Mundo 3: Manaos · Brasil (Amazonas) · Negro + Solimões","then":"intro","reduce_motion":false}`. En el 42 no sale ninguna línea `world_banner`.
- **También en el 11, el 81 y el 141**, entrando con Siguiente tras ganar el 10, el 40, el 80 y el 140, y desde Jugar. Sus textos [P, pendientes del asesor] son «Mundo 2: Tapajós · Brasil (Pará) · Tapajós», «Mundo 4: Trapecio · Colombia · Amazonas / Putumayo» y «Mundo 5: Loreto · Perú · Marañón-Ucayali». `then` dice qué viene después, `level_card` o `intro`; en estos cuatro niveles, `intro`, porque ninguno lleva tarjeta previa.
- **Arte:** cuando la tarjeta empieza a entrar sale `AF_STATE world_banner_art`, con `async`, `wait_ms`, `mascots_ms`, `main_ms` y `fallback`.
  - `main_ms` ≤ 8 ms: es lo que ocupa el hilo principal (08 §5.4).
  - `fallback` = `"none"`: Capi presenta con la pata y Rosa asoma.
  - `wait_ms` ≈ 0 si el arte se pidió antes (Siguiente tras ganar el 10, el 40, el 80 o el 140, o Jugar desde el inicio). Si no, es lo que tardó el pintado, como mucho 800 ms.
  - Anota `mascots_ms`, el pintado en el otro hilo (en el Editor, ≈ 230 ms).
  - Si sale `"timeout"`, `"error"` o `"queue"`, el banner entró sin Capi ni Rosa: anótalo con su `wait_ms` (pregunta 43).
- **Toques:** antes de `world_banner_art` solo se ve el velo y los toques no hacen nada. Después sale `AF_LAYOUT` con `"screen":"world_banner"`, y un toque en cualquier punto, por ejemplo en el centro del rectángulo `world_banner`, lo cierra:
  - sale `AF_STATE world_banner_closed` con `"next":"intro"` (o `"level_card"`);
  - Rosa se sumerge y la tarjeta sale en 340 ms; solo después empieza «Llenar y teñir» o sale la tarjeta previa (`level_card`);
  - un segundo toque durante la salida la termina en el acto.
- **Solo en el primer intento:** no vuelve a salir al reintentar (Reiniciar o Volver a la orilla), al salir al inicio y volver con Jugar, ni tras `force-stop` y reabrir.
- Mientras dura el banner, la fase es `LEVEL_CARD`, la misma de la tarjeta previa; lo distinguen `world_banner` y `world_banner_closed`. Ningún aviso sale encima.

**5. Tutorial de la Piraña en el 183 (A4; D-012, decisión 1, D)**
- En el primer intento, `AF_STATE level_card` trae `"bite_demo":"animated"`.
- En la tarjeta, la dosis llega con un chorro, que sale de detrás del marco, a la cuia de la derecha. La piraña sale de la cueva (la Cuia chica de 2, a la izquierda) y se lleva la dosis de la cima. La demostración dura ≈ 2,35 s, no se repite, no deja ningún anillo quieto y termina con la tarjeta abierta.
- El texto ocupa 2 líneas: «Cada cuatro vertidos, la piraña muerde la cuia que acabas de llenar. Sellarla a tiempo evita el mordisco.» [P, pendiente del asesor].
- Un toque cierra la tarjeta y empieza la intro.

**6. Pendientes de la tanda 1 (opcionales)**
- Observación 1, `AfterRewarded`: gana un nivel ≥ 22 con la sesión ya «caliente» y usa la Cosecha doble. Tras el vídeo no debe salir intersticial.
- Observación 4: la Piraña, con la cueva llena, se asoma y vuelve a dormirse.
- Completar la medida de T-D hasta ≥ 200 vertidos en 3 niveles (`AF_PERF pour`; pregunta 37).

### Paso 2 · Registrar D-007 y D-009 en el GDD

> *Hecho el 2026-10-11 y el 2026-10-12 (rama `paso2/registrar-d007-d009`, solo documentación): D-007, D-009 y D-012 registradas en `docs/dev/decisiones.md`, con la ampliación de D-010 y el estado de D-011, y llevadas al GDD (§7 de este archivo), con la lista única de estrenos en 02 §9.1, salvo las cifras [sim] regeneradas con D-009, que esperan al dueño (V5). Lo que depende de la tanda 2 (D-011, punto 2 y la tarjeta del miquito tras la tropa; de D-012, el banner de mundo, el tutorial de la Piraña, la sacudida y los patrones) ya está en el GDD como canon, sin darlo por implementado: al integrarla, basta con poner al día su estado en `docs/dev/decisiones.md`. Los pendientes del dueño están en `docs/GDD.md` §6.3 (preguntas 18-40; la 20, decidida). El texto de abajo queda como historial.*
>
> *Puesto al día el 2026-10-13 (rama `paso2/estado-tanda2`, solo documentación):*
> - *la tanda 2 está integrada (`94905b8`), así que D-011 y D-012 constan como implementadas enteras en `docs/dev/decisiones.md`;*
> - *lo que hace el cliente, con sus [P], está en 02 §4.2, §6 y §9.1, 07 §2.2, §3, §6.4 y §7.7, 08 §5.1 y la guía visual v2 (revisión 7);*
> - *las desviaciones y decisiones de los agentes que el dueño debe conocer son las preguntas 41-44 de `docs/GDD.md` §6.3, y la 37 recoge la medida del teléfono.*

Ninguna de las dos tiene aún su entrada en `docs/dev/decisiones.md`. Están decididas por el dueño:

- **D-007, mecánicas (2026-10-08).** El dueño aceptó todas las recomendaciones de `docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md` §(h):
  - **Catálogo:** 12 mecánicas en total.
  - **1.0 (niveles 1-200):**
    - Cuia chica (14) y Cuia alta (27);
    - Hoja 43, Matapi 63 y Liana con fruta 83;
    - Aguas turbias 100, con estreno suave: sola, 1 cuia, 2 dosis ocultas y 1 Luciérnaga regalada;
    - Remolino 103, Barro 123, Lluvia 143, Brote 163 y Piraña 183;
    - la Liana con número pasa a variante en el 243.
  - **1.1:** Aguapé 223, Palizada 263, Lancha 303 y Cocha 583.
  - **Respuestas del dueño:** Q13 sí (arreglar fútiles y R10; hecho en el núcleo), Q14 A, Q17 A (el contenido se ve), Q18 B (los fútiles no avanzan contadores), Q10 sí y Q12 no.
  - **Reglas finas (2026-10-08):**
    - Q-A: el Remolino no vierte en su propia pareja;
    - Q-B: la exención de futil también vale para el origen si es un Remolino o una Lluvia con gotas;
    - Q-C: si la Piraña no puede morder, pierde ese mordisco.
- **D-009, dificultad escalonada:**
  - ciclo de 20 con retos en p6, p9, p12, p15 y p18, remanso en p19 e hito en p20;
  - etiqueta visible antes de jugar y premios de 20, 60 y 80 semillas;
  - subtítulo del Difícil: «Aguas bravas»;
  - calendario de rotación (d.4) y la regla d.5.
- Las ediciones del GDD que exigen están listadas en la propuesta §(g) y en los informes de la pista A: `tools/out/campaign_v1_report.md` y `Packages/com.amazonfill.core/README.md`.

### Paso 3 · Elenco de ~50 especies que tiñen (ampliación de D-010, 2026-10-09)

El dueño decidió:
- **Ritmo:** **una especie nueva cada 20 niveles** (≈ 50 en 1-1000), coordinada con los estrenos de mecánicas para que no coincidan dos novedades en el mismo nivel.
- **Quién tiñe:** **monos, aves y otros animales que de verdad dejan caer fruta** (tucanes, guacamayos, oropéndolas, murciélagos fruteros, kinkajú…).
- **Cómo:** cada tipo con su forma de soltar la fruta (desde la rama, en vuelo, colgado, de noche), siempre en el mundo donde vive la especie.

El guion del equipo está en `docs/dev/traspaso/workflow-elenco-50-especies.js`: investigación → diseño (`docs/research/fauna/elenco-tinteros.md`) → verificación biológica y técnica → implementación → revisión → docs. Se detuvo antes de producir nada; hay que relanzarlo con la herramienta Workflow (`args: {"root": "<ruta del repo>"}`), **después** de la integración, porque usa Unity.

Material de partida: `docs/research/fauna/propuesta-fauna.md` (10 especies y 18 animales «que hacen cosas») y `docs/research/intro-rio-miquitos/guion.md`.

**Tropa lateral en los tableros de 3 filas (D-021, 2026-10-14): hazla antes del elenco o junto a él.**
- Hoy, con 3 filas, la intro va en modo dosel sin tropa. Pasa en 77 de los 100 niveles del 101-200, así que las especies de los mundos 4 y 5 casi no se ven.
- El dueño decidió que los miquitos bajen por los lados (trepando o colgados de una liana, desde el follaje de las esquinas) y lancen la fruta, **alternando**:
  - siempre en el primer nivel de 3 filas de cada mundo o especie;
  - en 1 de cada 3 de los demás [P];
  - nunca en los reintentos.
- **Restricciones:**
  - T, las fases y el calendario de la fruta no cambian;
  - sin reposo ya en juego;
  - nada a menos de 8 dp de una cuia, ni sobre el HUD ni sobre la bandeja;
  - D-002 y las reglas de representación de D-010.
- Si no cabe, para y pregunta con capturas.
- Detalle en `docs/dev/decisiones.md`, D-021. Agente: `unity-gameplay-dev`, con revisión visual en el teléfono.

### Paso 4 · Pendientes del dueño y del equipo

- **Preguntas del guion de la intro, de la propuesta de fauna y de la propuesta de Magic Sort: decididas el 2026-10-19** las que tenían recomendación (D-029): Q11 (`af_fauna_sighted` y la migración del perfil, en DC15), Q14 (el agua negra #522500, hecha en DC13) y Q17 del guion; F7-F13 de la fauna (F7 y F11, cuando lo dé el asesor; F9, al backlog; F13, en la 1.1); y de Magic Sort, #8, #10-#13, #17-#19 y #21-#25 (#25, «¡Perfecto!», en DC14). Siguen abiertas, sin recomendación aplicable, las #2, #9, #14 y #20 de Magic Sort (abajo, «tras la ola 7»).
- **Bloqueo técnico C1-9 de la pista A:** la propiedad P1 (R14) falla con la Liana y capacidades mixtas, así que `LevelValidation` rechaza la Liana con capacidades distintas de 4, cueva de la Piraña incluida. Por eso el calendario de la campaña movió o quitó la Liana, y no las capacidades, en cuatro celdas [P]: 94 Ma → Li y 95 Li+Al → Ma+Al (la Cuia alta vuelve en el reto 95 con el Matapi), 160 Ll+Li+Al6 → Ll+Al6 (la Cuia alta de 6 se sigue estrenando en el hito 160) y 198 Pi+Li → Pi+Ho (`calendar_note` de `tools/level-generator/specs/campaign_v1.json`). Está en `Packages/com.amazonfill.core/README.md`, en su CHANGELOG y en `tools/out/campaign_v1_report.md`. **Decidida el 2026-10-19** (pregunta 24 de `docs/GDD.md` §6.3, (b); D-028): el bloqueo es la regla de la 1.0 y la (a) se estudia para la 1.1; DC11 quita «provisional» de los textos del núcleo.
- **Preguntas del paso 2:** las 27-38 de `docs/GDD.md` §6.3 quedaron **decididas el 2026-10-13** con su recomendación (D-020), y la 39 y la 40, el mismo día (D-015). Tareas que dejan: el cliente con `level_overrides` como respaldo del kill-switch (31), la tabla de temporada del cliente (32), **la tabla de novedades de 201-1000 y el registro de combinaciones antes de generar 201-700** (33) y los textos a 12 sp como mínimo (36). La 20 (caché de `LocalDateKey`) ya está decidida y aplicada (`7677085`).
- **Preguntas nuevas de la tanda 2** (2026-10-13): las 41-44 de `docs/GDD.md` §6.3. Son los [P] de los avisos, el aviso que ya se ve al abrirse un modal del embudo, el banner de mundo con su arte en otro hilo y su respaldo de 800 ms, y el shader del líquido por encima del presupuesto de la guía visual. Los iconos de fruta procedurales quedan [P] para el artista y el asesor (07 §3, «Iconos del cliente»). **Decididas el 2026-10-13, con las 39 y 40** (D-015); sus arreglos van en T52, y la medida del shader necesita un teléfono de clase A (acción del dueño).
- **Pregunta de D-013** (2026-10-13): la 45 de `docs/GDD.md` §6.3, sobre la victoria del nivel `10k` (llegada del Mapa, línea de fruta y frase de TiTino), quedó **decidida el 2026-10-14: (a)** (D-022): canales separados (`ui_river_fact_every_n_levels` = 10 para la llegada y `ui_fruit_line_every_n_levels` [P] para la línea de fruta) y una sola línea por victoria con prioridad (frase de TiTino en sus tres hitos > dato de llegada > línea de fruta); la línea de fruta desplazada queda en la ficha de la fruta (Q4-A de Frutas, D-017).
- **Pregunta de D-014**: la 46 quedó **decidida el 2026-10-13: (a)** (D-020); T51 está integrada con ella.
- **Preguntas nuevas del 2026-10-14** (`docs/GDD.md` §6.3), **decididas el mismo día con su recomendación** (D-022): la 47 (los [P] de T52: el compromiso del aviso con 3 filas y la luz de la pareja del Remolino), **se ratifican**; la 48 («Te faltan {n} semillas» de Revelar sin medios, un [P] del paso 1), **(b)**: responde la burbuja «Te faltan {n}» que ya se ve, sin aviso ni texto nuevo; la 49 (claves y columnas de las tablas de textos: Frutas y el Mapa frente a idiomas), **(a)**: dos familias de claves y una columna de estado por idioma; y la 50 (los Datos del río del estreno de la Tinaja), **(a)**: entran como «Otro dato» de la tarjeta de la parada k = 32 (321-330), con el visto bueno del asesor. Y la **51**, nueva (D-018 recogía a la vez la traducción humana completa de los datos y la partida de 1.300-3.000 €, que no cabían juntas): el dueño respondió «traducción con ia», **(b)**: los datos de fruta y fauna se traducen con IA y revisión nativa, como la interfaz, dentro de esa partida. Lo que dejan para el código está en D-022 («Consecuencias»).
- **Tareas que dejan D-016 a D-019** (detalle en sus entradas de `docs/dev/decisiones.md`): la base de localización sin paquetes (niveles 1 y 2 de la propuesta de idiomas), antes de que Frutas del río y el elenco de especies escriban texto; el generador, el esquema y level-qa de la Tinaja (T56); los ganchos de TiTino (plan (i) de su propuesta, con la pantalla de arranque, que hay que construir); y los pasos F1-F9 de Frutas del río.
- **Pendientes de la pista B:**
  - el guardado por vertido ya va con un escritor en segundo plano (T-D, tanda 1; D-012, decisión 5). Se midió en el teléfono el 2026-10-12, en 52 vertidos, y cumple: p95 de 2,15 ms y máximo de 3,67 ms. El 2026-10-13 se midieron 216 vertidos (máximo 4,83 ms) y el dueño decidió la pregunta 37: el vertido que gana cuenta (D-020);
  - el reloj anti-trampas necesita hora de servidor: hoy, adelantar el reloj rellena canoas;
  - algunos [P] de nombres de perfil.
- **Arte:** todo es procedural y provisional. Los prompts para ChatGPT o para el artista están en `docs/art/prompts-chatgpt.md`, y la lista de piezas de los miquitos en el guion de la intro (§11) y en la propuesta de fauna.
- **Asesor cultural:** nombres de fauna, flora y lugares, los textos de «Dato del río» y las especies. Las listas están en las propuestas.
- **Siguen pendientes del dueño:** el permiso `INTERNET` (`CLAUDE.md`), el dictamen de marca de «Tintalú» (que ahora incluye «TiTino»), la prueba visual de 30 min (s28) y conseguir un teléfono de clase A (pregunta 44).
- **Pendientes del dueño tras la ola 4 de la tanda 4** (2026-10-16, `8e1d65c`; las pruebas en el teléfono de las olas 2 y 3 están en `pruebas-telefono-2026-10-15-tanda4-ola2.md` y `-ola3.md`):
  - **la insignia del nivel en otros idiomas:** fuera del español no cabe en su placa a 20 sp (en en-XA se sale; `LevelBadge_EnXA_KnownIssue`, explícito, falla hasta que se decida). Opciones: rediseñar la fila del HUD o bajar el mínimo de la guía visual 3.5 a 12 sp (L4, ola 3);
  - **el rótulo del lugar sobre la silueta** (con 1-2 filas tapa el héroe durante sus 3 s: subirlo por encima de la silueta o dejarlo como placa también ahí) y **la silueta invisible con 3 filas** (el panel del tablero tapa la banda del horizonte en 131, 151, 171 y 191): observaciones 1 y 2 de la prueba de la ola 2;
  - **la ventanita de casa con 0 canoas:** hoy solo se oculta con Sin canoas encima, como dice su sonda; ¿debe ocultarse también en el inicio? (observación 1 de la prueba de la ola 3);
  - **la victoria con vista enseña poco sitio:** con el tablero fuera, la silueta queda en la banda del horizonte, a la altura del HUD, y debajo solo hay agua y orilla lisas (observación 2 de la prueba de la ola 3; va con la silueta invisible con 3 filas y con la pregunta 53, decidida el 2026-10-19: D-028, DC2);
  - **el factor de Morpho en un teléfono de clase A** (Mali-G52 o Adreno 610): el bench de PF1 (`--ei af_bench_morpho 1`, líneas `AF_PERF morpho`) mide el factor que sustituye a los 8× [P] de la compuerta de coste de Morpho; con el mismo teléfono se mide el shader del líquido (pregunta 44);
  - **las líneas 17-20 del cacao y del arazá**, sin redactar (8 líneas: `fact.cacao.17`-`20` y `fact.araza.17`-`20`); las escribe el carril de documentación y pasan por el asesor;
  - **la curación de los mundos 6-9** (201-700): el informe del mundo 6 está en `tools/out/curation_w06.md` (P7a); los mundos 7-9 (P7b-P7d) se publican en el cliente con C1b.

- **Pendientes del dueño tras las olas 5 y 6 de la tanda 4** (2026-10-18; la prueba de la ola 5 en el teléfono está en `pruebas-telefono-2026-10-17-tanda4-ola5.md`):
  - **Escuchar el audio en el teléfono:** las notas de fruta y el chorro (calidad Vorbis 0,8), que las capas de la música no se desfasen tras varios bucles y que en `amb_w01_fauna_02` no se entienda ninguna voz. El portátil solo comprobó las sondas.
  - **El asesor cultural:** las 24 piezas de audio con `advisor_review` pendiente (entre ellas el boto y la piha del banner; 17 con especie o región sustituta, no amazónica, 10 de la fauna y 7 efectos, entre ellos el boto; la piha no es sustituta), los textos nuevos de las olas 5 y 6 (`map_l10_intro`, las frases de TiTino, `tutorial_morpho`, los `hint_*` que nombran fauna, `tutorial_brote_n2` y `tutorial_flor`) y la embaúba de la ventanita.
  - **Formato de las distancias** (observación 6 de la prueba de la ola 5): «a unos 1,480 km» con el teléfono en en-US es lo que manda Q8-A de D-018 (cifras con la región del teléfono) y D-025; FX7 no lo cambió. Si el dueño prefiere que sigan al idioma del juego, es una decisión nueva.
  - **La compuerta de tamaño del CI** (pendiente del CI, s30): debe medir el AAB del juego, el de `AF.EditorTools.AudioAssetSetup.BuildGameAab`, y no el de humo, que no lleva el audio, los niveles ni los textos (08 §6.1; ya consta en la cabecera de `ci/size-gate.ps1`). La medida de B2 y su margen están en la sección 1, «Tamaño», y en `docs/setup/medida-aab-b2.md`.
  - **El 323 sale sin Tinaja** (C1c, 2026-10-19; pregunta abierta de `docs/dev/decisiones.md`, D-019): TJ4 está en verde, pero la compuerta de PF1 (el factor de Morpho en un teléfono de clase A) no, así que 301-700 se publican con la variante b (salida B de Q-Ti3) hasta que el dueño la dé por buena. Entonces: `published_variant` = "a" en `campaign_v2.json`, regenerar el registro y los paquetes de 301-700, `LevelCatalog.PublishedVariant` = `VariantA` y regenerar la escena.
  - **Sigue pendiente de antes:** el teléfono de clase A para el factor de Morpho y el shader (pregunta 44), las líneas 17-20 del cacao y del arazá y la curación de los mundos 6-9. Las preguntas 18, 21, 25, 26, 52, 55, 56, 58, 59 y 62-64 se respondieron el 2026-10-19 (D-029; abajo).

- **Pendientes del dueño tras la ola 7 de la tanda 4** (2026-10-19; la prueba de la parte A está en `pruebas-telefono-2026-10-19.md`). **Respondidas el 2026-10-19** (D-028; «Acepto la opción recomendada en las preguntas 19, 22, 23, 24, 53, 54, 57, 60 y 61 (con P5) del GDD §6.3 y en las dos de M5.1»): las 19, 22-24, 53, 54, 57, 60 y 61, con P5, y la 65 (la geometría de producción del Mapa, en sus tres partes), todas con su recomendación; sus tareas, DC2-DC11, van en la ola 7. **Respondidas también el 2026-10-19, después** (D-029; «resolvamos las preguntas, pero si tienen recomendadas vamos con esas»): todas las que tenían recomendación, cada una con la suya: las 18, 21, 25, 26, 52, 55, 56, 58, 59 y 62-64 de `docs/GDD.md` §6.3 y la parte 3 de la 66 (el trazo de M5.1c); P7 y P14-P17 (P17: A) de `docs/dev/decisiones.md`; el dedo guía de la Hoja (43) y de la Tinaja (323), en DC7; y las del guion de la intro, la propuesta de fauna y la propuesta de Magic Sort (arriba). Con ellas, las cifras [sim] del GDD son ya las del ciclo de D-009 (V5 cerrada). Sus tareas: DC6, DC7 y DC12-DC16.
  - **La pregunta 66 de `docs/GDD.md` §6.3, partes 1, 2 y 4:** las placas de los mundos futuros (FX10), las paradas en una confluencia de M5.1 y el consejo y el tic de la Tinaja; D-029 solo decidió la parte 3.
  - **Las preguntas sin recomendación o en conflicto con el canon** (siguen abiertas, con su [P]; la lista está en D-029, «Fuera de esta decisión»): las paradas en una confluencia de M5.1; P12 de D-006 (16:9 con 13-14 cuias); las dos de D-021 (la velocidad del lanzamiento lateral y la barra de la Crecida); las #2, #9, #14 y #20 de la propuesta de Magic Sort; la insignia del nivel en otros idiomas (L4); el rótulo del lugar sobre la silueta y la silueta invisible con 3 filas; la ventanita de casa con 0 canoas; la victoria con vista que enseña poco sitio; la temporada de SE2, que apenas se nota; el formato de las distancias; el respaldo del tooltip de Marea en el 5; los dos [P] de TiTino (las frases fijas que se repiten y la lectura del saludo de la mañana); el tope 0,7 del cielo de `StopBackdrop` (hoja 02f); y `profile_locked_title` (aviso o pantalla de bloqueo).
  - **Acciones del dueño:** escuchar el audio de la entrega 1 en el teléfono (64), probar en el teléfono las olas en curso (también la cadencia de TiTino de la 52) y la prueba visual de 30 min (s28), las grabaciones de Magic Sort (#13) y los encargos al asesor (entre ellos F7, F11 y Q17), al artista y al abogado de marcas.
  - **La atribución de HydroRIVERS:** leer su acuerdo de licencia (en hydrosheds.org; no viene en la descarga), fijar el texto de atribución que exige y decidir dónde va en el juego, que aún no tiene pantalla de créditos (README de `tools/river-map`, notas de M5.1).
  - **El 323 con la Tinaja** (arriba, «El 323 sale sin Tinaja»): depende del factor de Morpho en un teléfono de clase A.
  - **La curación de los mundos 7-9:** los informes de la variante publicada, la b (`tools/out/curation_w07.md` a `curation_w09.md`, 122 niveles, 13 retos sin su etiqueta), con C1b.
  - **El sitio en el AAB antes de los SDK y del arte a 2×:** Addressables y PAD, fuentes recortadas o arte a 1× (B2; sección 1, «Tamaño»). Y el permiso `INTERNET`, que sigue pendiente. El dueño dijo el 2026-10-19 que los decide aparte.

## 7. Decisiones del dueño vigentes (registro en `docs/dev/decisiones.md`)

| Decisión | Resumen |
|---|---|
| D-001 | Una dosis por vertido. **Sustituida por D-005** |
| D-002 | Al seleccionar no se resalta ningún destino. Las ayudas son explícitas |
| D-003 | Dirección visual «Selva brillante premium»: look de juego top, nunca libro, papel ni acuarela |
| D-004 | Panel azul petróleo y tope de ancho de la cuia por T. La pose y el techo los sustituye D-006 |
| D-005 | Vertido por tramo, como Magic Sort (`rules_pour_mode` = `run` por defecto) |
| D-006 | Tablero, cuia y vertido copiados de Magic Sort: pose según las dosis que quedan, boca casi en el destino, ciclo de ≈ 0,9 s, cavidad ≥ 0,80, 3 filas desde 11 cuias |
| D-007 | Mecánicas del río: catálogo de 12, estrenos de la 1.0 y reglas finas Q-A, Q-B y Q-C. Registrada el 2026-10-11 y completada el 2026-10-12 (paso 2), con la lista única de estrenos en 02 §9.1 |
| D-008 | Bucle de partida como Magic Sort: estructura de Magic Sort, cifras de nuestro GDD. Las vidas ilimitadas no quitan los anuncios. P14-P17, decididas el 2026-10-19 con su recomendación (P17: «Canoas infinitas 1 h» siempre en Sin canoas; D-029) |
| D-009 | Dificultad escalonada: ciclo de 20 reordenado, etiqueta antes de jugar y rotación de plantillas. Registrada el 2026-10-11 (paso 2); sus cifras [sim] y la tabla de etiquetas de 03 §11 con la cuenta del ciclo, aprobadas el 2026-10-19 (D-029) |
| D-010 | Intro «Llenar y teñir» con miquitos como datos. La ampliación a ~50 especies quedó registrada el 2026-10-11; su diseño es el paso 3. Q11, Q14, Q17 y F7-F13, decididas el 2026-10-19 con su recomendación (D-029) |
| D-011 | Prueba en el teléfono (2026-10-11): la victoria se cobra en la vista cuando se ve, avisos legibles, miquitos con más presencia sin frenar el juego y Cosecha doble que avanza sola. Implementada entera. Los puntos 1, 3 (salvo la tarjeta del miquito tras la tropa) y 4 van en la tanda 1 (`7d755c4`), probada en el teléfono el 2026-10-12: cumple. El punto 2 y esa tarjeta van en la tanda 2 (`94905b8`), probada en el teléfono el 2026-10-13: cumple. Llevada al GDD el 2026-10-12. Las preguntas 41 y 42 se decidieron el 2026-10-13 (D-015) y sus arreglos están en T52; la 38, el mismo día (D-020) |
| D-012 | Decisiones del 2026-10-09 y respuestas del 2026-10-11: estreno de la Piraña con mordisco fallido visible y tutorial animado, banner de mundo (Capi presenta y Rosa asoma; 11, 41, 81 y 141, primer intento, sin banner en el 1), sacudida de ±8 dp, 3 ciclos y 240 ms, patrones e iconos como el GDD, guardado por vertido con escritor en segundo plano (p95 < 5 ms y ninguno > 8 ms), marco del panel sin cambios y Remolino con canal y la misma marca en las dos cuias. Registrada el 2026-10-12 (paso 2) e implementada entera. La tanda 1 (`7d755c4`: mordisco fallido, Remolino y guardado) se probó en el teléfono el 2026-10-12 y cumple. La tanda 2 (`94905b8`: sacudida, patrones e iconos, banner de mundo y tutorial de la Piraña) se probó en el teléfono el 2026-10-13 y cumple. Las preguntas 37 y 38 se decidieron el 2026-10-13 (D-020) y las 39, 43 y 44, el mismo día (D-015). D-013 cambia la decisión 2: el banner trae dentro el viaje de mundo |
| D-013 | Mapa del río (2026-10-12 y 2026-10-13): remontar el Amazonas hasta una de sus nacientes (el nevado Mismi), con el río real en vidrio que se llena con su agua, una parada cada 10 niveles con su fondo de tablero (kit del mundo y héroe del sitio en 1-200), el viaje de mundo dentro del banner (Q4-D) y la victoria con vista. TiTino es el protagonista (T1-A), Capi trae cuias y rema la canoa, y Rosa guía; una sola línea de texto por victoria (T6-A); la serie de frutas absorbe el Dato del río (Frutas Q1-A). Cambia el texto de la tarjeta del hito p20 (D-009) y el banner de mundo (D-012); D-008 no cambia. En implementación en la tanda 4: datos de las paradas de 1-700 y pantalla del Mapa con marcadores, tarjeta de la parada, Muestrario, «Norte arriba», Mapa del 10 y entradas desde la pausa, la victoria y el fin de contenido (olas 2-6). El 2026-10-17 el dueño decidió además que, con el norte arriba, solo salgan las placas de los 3 mundos futuros siguientes (opción A; punto 5). Pregunta 45 de `docs/GDD.md` §6.3 (un choque entre propuestas), decidida el 2026-10-14 en la (a) (D-022). Lo que dejaba abierto (TiTino T2, T4 y T5, Frutas Q2-Q7, idiomas y tinaja) se decidió el 2026-10-13: D-016 a D-019 |
| D-014 | Aguas estancadas por bucle (2026-10-13): también hay bloqueo cuando el cierre de estados alcanzables con **todos los vertidos legales**, fútiles incluidos, se agota con ≤ 64 estados sin victoria (02 R13; pregunta 46 en la opción (a), D-020), solo en la partida del cliente (`LevelSession`, `DeadEndProbe`). Arregla el bucle del nivel 66. Integrada (T51, `1ad712c`) |
| D-015 | Respuestas del 2026-10-13 a las preguntas 39-44 de `docs/GDD.md` §6.3, todas con su recomendación: oferta de skin fuera del banner y del Mapa; la Cuia chica cuenta como vacío; los [P] de los avisos, ratificados; el aviso se oculta al abrirse un modal del embudo; el banner con su arte en otro hilo y con escala de fuente; el shader se mide en un teléfono de clase A, con la excepción de 3 muestras aceptada. Código en T52, integrada en `d2ce284`; sus [P] son la pregunta 47, ratificados el 2026-10-14 (D-022). El teléfono de clase A lo consigue el dueño |
| D-016 | TiTino (2026-10-13): despierta sin palabras en la pantalla de arranque (que hay que construir), acompaña el tutorial de 1-3, 6 y 12 y sale en un aviso tras Volver a la orilla en 1-30, fuera del tablero; nombre «TiTino»; ninguna mascota fija en el muelle; ajuste «Mensajes de TiTino». En implementación en la tanda 4 (plan (i) de su propuesta): rig provisional, victoria, arranque, tutorial (1-3, 6 y 12), Nivel fallido, aviso tras Volver a la orilla, inicio y Mapa del 10 (olas 4-6). Preguntas 53, 54 y 60 (D-028) y 52 (D-029) de `docs/GDD.md` §6.3, decididas el 2026-10-19 |
| D-017 | Frutas del río (2026-10-13): 20 líneas por fruta desde el nivel 1, ficha para releer en la 1.0 (`af_fact_page_open`), sin premio (un sello), «Frutas del río» con la etiqueta «Dato del río», bajo «Inicio» en una ranura de alto fijo; formato `af-facts/1`. En implementación en la tanda 4: calendario, línea de la victoria, «Fruta nueva» y, desde F4 (ola 6), la ficha y «Sobre la Amazonía» en Ajustes. Pregunta 55 de `docs/GDD.md` §6.3, decidida el 2026-10-19 en la (a) (D-029). Preguntas 45 y 49, decididas el 2026-10-14 en la (a) (D-022) |
| D-018 | Idiomas (2026-10-13): ES-419, PT-BR y EN en la 1.0, los 11 de Magic Sort al salir al mundo (el ruso, el último) y más según datos; base de localización ya, sin paquetes (CSV, fachada `Loc`, idioma falso en las capturas); idioma del teléfono con selector; cifras con la región. **Aprobados:** Unity Localization 1.5.13 en la fase 3 y la traducción de la 1.0 a 1.300-3.000 €. Pregunta 49, decidida el 2026-10-14 en la (a), y los datos de fruta y fauna, con IA y revisión nativa dentro de esa partida (pregunta 51, (b)): D-022 |
| D-019 | Tinaja, M13 (2026-10-13): mecánica propia con contenido oculto en niveles Normales (con tope, ruta segura y suelta por recuento visible), su cuia chica recibe cualquier fruta, estreno en el 323 de la 1.0 si cumple su condición (si no, 1.1), no enseña su contenido, se acepta espiar reiniciando. Núcleo hecho (T55); generador, esquema y level-qa en T56. Pregunta 50, decidida el 2026-10-14 en la (a) (D-022) |
| D-020 | Respuestas del 2026-10-13 a las preguntas 27-38 y 46 de `docs/GDD.md` §6.3, todas con su recomendación: Lluvia y Piraña en la 1.0, «Zafra» solo como nombre, Revelar en la 3.ª ranura, `pack_schema` 2, `level_overrides` como respaldo, temporada en una tabla del cliente, tabla de novedades antes de 201-700, P1-P3, solo la burbuja, 12 sp, el vertido que gana cuenta, [P] de la tanda 1 y el cierre de D-014 con todos los vertidos legales |
| D-021 | Miquitos por los lados en los tableros de 3 filas, alternando (siempre en el primer nivel de 3 filas de cada mundo y en 1 de cada 3 de los demás [P]), sin cambiar T ni tapar cuias. Implementada en la rama de la tanda 4 (TL1, `tanda4/o2-unity-b`), pendiente de la prueba en el teléfono y de dos respuestas del dueño (ver su entrada en `docs/dev/decisiones.md`) |
| D-022 | Respuestas del 2026-10-14 a las preguntas 45, 47-50 y 51 de `docs/GDD.md` §6.3: la 45 y las 47-50 con su recomendación y la 51 con la respuesta del dueño, «traducción con ia». Canales separados y una sola línea por victoria, con prioridad (frase de TiTino > dato de llegada > línea de fruta; `ui_fruit_line_every_n_levels` [P]) (45, (a)); los [P] de T52, ratificados (47); Revelar sin medios responde con la burbuja «Te faltan {n}», sin aviso (48, (b)); dos familias de claves y una columna de estado por idioma (49, (a)); los Datos del río de Pará, como «Otro dato» de la parada k = 32 (50, (a)); los datos de fruta y fauna, con IA y revisión nativa dentro de 1.300-3.000 € (51, (b)). Documentación aplicada (T58); el código, en la tanda 4 |
| D-023 | Decisiones de detalle de la tanda 4 (2026-10-14), con su recomendación: P-M1 a P-M8 del plan del Mapa (extras del formato del Mapa, diez claves de UI, el Mapa del 10 tras Inicio, arte provisional solo en desarrollo, rótulo en el 31, bienvenida al país en la victoria del 80 y del 140 y relevo de Rosa en Teotônio) y las preguntas del plan consolidado (textos del Mapa en `facts.csv`, firma de las variantes de `pack_schema` 3, máscara de la tinaja en `relief`, Cuia de la casa fuera de la 1.0, idioma automático en es-419 y es-CO sin región, llegada compactada si no cabe y la variante de D2), más los detalles [P] del plan. Hito 80: «Lago de Tefé» [P] |
| D-024 | Reglas de la Hoja atada (2026-10-14): la capacidad de su llave y una Hoja atada por fruta, con las recomendadas de la revisión de K3a. Documentación aplicada (FIXD) e implementada en el núcleo (FIXK, ola 1 de la tanda 4); el generador y level-qa la heredan por `LevelValidation` |
| D-025 | Un solo formato de cifras, también en los precios: todo con `Loc.Number` (región del teléfono; sin región, es-CO [P]) y los precios como moneda + importe con ese formato. **Implementada en FXT** (`Loc.Price`; el adaptador de Unity IAP compondrá con `Loc.PriceFromMicros`) |
| D-026 | Audio provisional por código (2026-10-16): los 61 efectos del laboratorio portados a C# y generados al arrancar, con los arreglos de su revisión (cada uno a su nivel, graves al 50 % por encima de 300 Hz, cola de dosel solo donde va, gestos ≤ 0,2 s y el chorro como bucle que sube con el llenado), Sonido, Música y Vibración en Ajustes y el permiso `VIBRATE` aprobado para los hápticos. **Implementada en AU1**, con el ambiente por mundo y la música adaptativa provisionales de AU2 (detrás del ajuste Música [P]); integrada en `bd649ac` y llevada a 07 §9 (FXD2). Desde AU3 (ola 5, `4476796`), el juego suena con los 173 WAV de la entrega 1 y el audio por código queda de respaldo; la prueba de la ola 5 lo comprobó por sondas (cumple) y falta escucharlo |
| D-027 | Mundos 7-9 (2026-10-18): las 25 celdas, K = 10 en los retos de 421-700, los hitos de 14 cuias con K = 12 + 1 vacío (salvo el 620 y el 680), el alivio hasta 13 cuias con la Tinaja y QA-PAR sin el par del rango en sus estrenos, todo de la propuesta de los mundos 7-9. Implementada en GEN79 y publicada en el cliente con la variante b (C1b, ola 7, en su rama); falta la curación del dueño |
| D-028 | Respuestas del 2026-10-19 a las preguntas 19, 22-24, 53, 54, 57, 60, 61 (con P5) y 65 de `docs/GDD.md` §6.3, todas con su recomendación: el texto a 12 sp físicos en 16:9 (19); los [P] del HUD, la bandeja y el tablero, ratificados (22); vídeo primero en Revelar (23); el bloqueo C1-9 como regla de la 1.0 (24); TiTino en la franja de la victoria con vista y los [P] de TI6a, ratificados (53 y 54); la fuente del dato en filas `<clave>.source` de `facts.csv` (57); el bocadillo del Mapa del 10 fuera del tope de frases y dentro de «dos pantallas seguidas» (60); el 12 sin dedo guía y el dedo de 1-2 como ayuda explícita (61 y P5); y la geometría del Mapa con HydroRIVERS en las dos vistas, las excepciones medidas y `MundoZone` de 100 km (65). Documentación aplicada (DC1); el código, en DC2-DC11 (ola 7). Fuera: el espacio del AAB, `INTERNET` y las paradas en una confluencia |
| D-029 | Respuestas del 2026-10-19, después de D-028, a todas las preguntas con recomendación que seguían abiertas («resolvamos las preguntas, pero si tienen recomendadas vamos con esas»), cada una con la suya: las 18, 21, 25, 26, 52, 55, 56, 58, 59 y 62-64 de `docs/GDD.md` §6.3 y la parte 3 de la 66 (entre ellas, las cifras [sim] del ciclo de D-009 en el GDD y 03 §11 con la cuenta del ciclo); P7 y P14-P17 (P17: A); el dedo guía de la Hoja y de la Tinaja (DC7); M5.1c; Q11, Q14 y Q17 del guion de la intro; F7-F13 de la fauna; y las de la propuesta de Magic Sort (#25: «¡Perfecto!» sin Cuia de rescate). Documentación aplicada (DC1b); el código, en DC6, DC7 y DC12-DC16 (ola 7). Fuera: el espacio del AAB, `INTERNET` y las preguntas sin recomendación |

## 8. Cómo trabaja el dueño (preferencias aprendidas)

- **Idioma y estilo:** español; pragmático; quiere avanzar rápido y ver resultados en el teléfono.
- **Copiar lo que funciona:** «no vamos a inventar la rueda». Para medidas, reglas, flujos y monetización se copia Magic Sort (medido en su propia sesión grabada) y se le da el toque amazónico. Nada de bucles de críticos para decidir lo que la referencia ya resolvió: la revisión comprueba que se copió bien.
- **No suavizar lo que funciona** en la referencia salvo por política de Google Play. Si algo parece mejorable, plantéalo en una frase como pregunta. Ejemplo: rechazó quitar anuncios durante las vidas ilimitadas («como lo tienen está bien»).
- **Autenticidad cultural:** las mecánicas y la fauna se diseñan con investigación real de la vida ribereña y de los países del Amazonas, con fuentes y respeto (nada sagrado, nada de mascotas ni estereotipos). Todo queda «pendiente del asesor cultural».
- **Dificultad:** dientes de sierra con etiquetas antes de jugar, alivio tras cada pico y plantillas que rotan y vuelven, como en Magic Sort.
- **Autonomía:** todos los permisos están en «allow». Aun así valen las reglas duras de `CLAUDE.md`:
  - no tocar `main` sin que lo pida;
  - nada de force push;
  - nada de SDK, paquetes ni versiones nuevas sin su visto bueno;
  - nada de secretos;
  - nada de publicar en Google Play.

  Pregunta solo lo que de verdad decide él, con opciones cerradas y una recomendación.
- **Colaboración con ChatGPT:** puede pedir revisiones o imágenes a ChatGPT. Trátalas como propuestas que hay que verificar, no como órdenes.

## 9. Lo que no está en git (se queda en el portátil)

- `Logs/device/magicsort/`: la grabación de 20 min de Magic Sort (`ms_largo.mp4`, 511 MB) y todo su análisis (`analisis/`: fotogramas, audio, observaciones; ≈ 489 MB). Las fichas de `docs/research/magic-sort/` lo resumen. Es de un juego de terceros: nunca se sube.
- `Logs/device/lovewater/` (vídeo de Love Water), `Logs/snapshots/` (capturas del Editor) y `Builds/` (APK).
- El worktree `../amazon-fill-fase1` (rama `fase1-mecanicas-niveles`, ya fusionada) y la rama `codex/orillas-vivas` de ChatGPT.
- La memoria de Claude del portátil: lo esencial está en la sección 8.
