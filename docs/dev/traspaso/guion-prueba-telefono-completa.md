# Guion de la prueba completa del juego en el teléfono

Lo escribió el integrador (escritorio) el 2026-10-25, a petición del dueño: **probar el juego entero, no solo lo nuevo**. Se mira cómo se comporta, los gráficos y las imágenes, los niveles, los comodines, el uso normal, perder y reintentar muchas veces, y todo lo que se pueda probar, **tarde lo que tarde**. Es para el portátil, con el SM-S938B (Android 16) y con el dueño al lado en las partes que necesitan ojos, oídos o ajustes del sistema.

**Punto de partida:** `integracion-fase1-fase2` en `cefbb9b7` o posterior. La última prueba en el teléfono fue sobre `a656cca4` (2026-10-23).

**Lo que entró desde entonces y nunca se ha visto en el teléfono:**
- Mercado oculto sin precios de Play y aviso de «tienda no disponible» (`6914b06f`);
- cliente de la Liga del río contra el backend de dev (`00a596df`, `ebe904ef`; su guion propio, `guion-prueba-telefono-liga.md`, se sigue entero en el bloque 12);
- Música y Sonido con un toque, en el inicio y en la pausa (`666045d0`);
- la franja de la tropa con 3 filas: suelo bajado 20 dp y avisos en la franja (`4c67ee85`);
- pulido de T6 (`f93652e9`):
  - tarjeta del Reto a 320 dp;
  - contador de Lluvia o Piraña junto a Reiniciar en dos líneas;
  - zona segura del anuncio y del calendario;
  - brillos del río sin asignaciones;
- tests fiables de T6 (`45529ef4`), que no cambian el juego pero sí las capturas.

**Lo que nunca se ha probado en ninguna prueba:**
- la pantalla 16:9;
- la fuente de Android de verdad;
- TalkBack;
- el modo avión;
- cambiar la hora;
- las skins de los hitos;
- la Hoja escudo;
- el Cántaro lleno;
- la ola de la Pororoca con Lluvia o Piraña;
- el `rehearsal.apk`;
- Remote Config aplicado;
- el audio escuchado de verdad.

Aquí entra todo.

## Encargo de la jornada 6 (escritorio, 2026-10-28)

**Punto de partida:** `integracion-fase1-fase2` en el commit que trae este encargo o uno posterior (`git pull`); el `dev.apk` y el ensayo de release se construyen de nuevo desde ahí. La jornada 5 (`pruebas-telefono-2026-10-28-completa-j5.md`) salió casi limpia: sus observaciones 1, 2, 5 y 6 se arreglan en los carriles `tanda6/fx-j5-arranque` y `tanda6/fx-j5-ui`, ya integrados; la 3 y la 4 no son defectos.

**Orden:** el bloque 0; N6 entero (lo nuevo); después, lo que la jornada 5 dejó pendiente (su bloque 6, el jugador automático, ya cumplió 308/308): los bloques 5, 8 a 19, 21, 22 y 23, y lo que hace el dueño (TalkBack, Reducir movimiento, dos dedos, la mirada a la intro). La hoja es `pruebas-telefono-<fecha>-completa-j6.md`. Las compras de verdad siguen fuera: se prueban en la pista interna con el AAB firmado (D-059).

**Antes de publicar:** el Release definitivo no se lanza hasta que N6 confirme en el teléfono la pantalla del sistema hasta la intro (`os_splash_release` = `signal` y `os_splash_by` = `intro` en la primera apertura, en frío y en el ensayo). Si sale `cap`, se anota con su ráfaga y se revierte quitando `AfSplashHold.androidlib`.

**Mientras tanto (el dueño, 2026-10-28):** N6.1 salió `cap` en todos los arranques (observación 1 de la hoja j6), y el escritorio lo arregla en el carril `tanda6/fx-j6-splash`. El portátil **sigue ya, con el build que tiene**, con el resto de N6 que no depende de la pantalla del sistema y con los bloques pendientes (5, 8 a 19, 21, 22 y 23, y lo que hace el dueño). Los ~3 s de más del arranque no son defecto nuevo: no se anotan otra vez. N6.1 (y Reducir movimiento, Inicio durante el icono y abrir en frío desde una notificación) se repite cuando llegue el build con el arreglo. Lo hecho se va añadiendo a la misma hoja `-j6.md`.

**Repetir N6.1 con el velo sin bloqueo (2026-10-28).** La primera entrega de la jornada 6 (`pruebas-telefono-2026-10-28-completa-j6.md`, observación 1) dio `cap` en todos los arranques: la retención del primer fotograma se esperaba a sí misma. El carril `tanda6/fx-j6-splash` la cambia por un velo que no bloquea (segundo diseño del añadido de D-051 del 2026-10-28 en `decisiones.md`): una vista nativa con `af_launch_window` encima de la ventana de Unity y, en Android 12+, la propia pantalla del sistema entregada a la actividad (`SplashScreenView`). Las dos se quitan cuando la intro ya está en la pantalla. El portátil hace `git pull`, construye de nuevo el `dev.apk` y el ensayo de release desde el commit que lo integra y **repite N6.1 entero** con los cuatro casos de abajo. **La compuerta**, en la primera apertura, en frío y en el ensayo:
- `os_splash_release` = `signal` y `os_splash_by` = `intro`;
- `os_splash_held_ms` < 3.000 (con `cap` y ≈ 3.000 sigue fallando);
- `proc_awake_ms` **antes** de `proc_os_splash_ms` (Unity arranca con el velo puesto; en la primera entrega iba ≈ 200 ms después);
- en las ráfagas, **ningún fotograma de petróleo liso** entre el icono y la escena, y el icono sin saltos;
- anota `os_splash_veil`: en este teléfono (Android 16) se espera `system`. Si sale `own`, Android no entregó su pantalla: mira entonces con más cuidado si el icono salta al pasar al velo propio.

Después, sigue con lo pendiente en el orden de arriba (el resto de N6 y los bloques que dejó la jornada 5), donde se quedó con el build anterior.

**El build que lo trae todo (2026-10-29).** El commit que integra `tanda6/intro-desde-logo` lleva también el velo: con ese build se repite N6.1 y se prueba el punto «La intro nace del logo» de N6 (el dueño juzga si queda «muy pro»). Además, **se repite la observación 3 de la hoja j6** (otra app con el foco al lanzar: ¿sale la intro o se queda en el petróleo?), porque el velo ya no toca el correr sin foco. Las observaciones 2, 4, 5 y 6 de la hoja j6 se arreglan en carriles aparte: no hace falta volver a anotarlas.

**Repetir el bloque 19.0 con la sonda nueva (observación 6 de la hoja j6; carril `tanda6/fx-j6-memoria`, `docs/dev/memoria.md`).** Cuando llegue el build que integra ese carril (`dev.apk` y ensayo de release), repite el desglose del bloque 19.0 en las mismas cuatro variantes (`dev.apk` 18+ en el 75 y el 200, <13 en la pantalla de bloqueo y el ensayo en el 1) y suma, en cada `dumpsys meminfo -d`, la línea `AF_PERF {"event":"mem"` del mismo momento (`lobby`, `level` y `map`): ahora lleva `unity_reserved_mib`, `unity_unused_mib`, `mono_heap_mib`, `gfx_mib`, `gfx_reserved_mib`, `tex_cur_mib`, `audio_mib`, `rt_mib` y `rt_top`, para repartir el `Unknown` y el `GL mtrack`. Anota en la hoja la tabla del 19.0 y, al lado, esas cifras. Lo esperado: `GL mtrack` unos 10-20 MB más bajo (el Android Blit Type pasa de Always a Auto: sin el búfer de pantalla completa) y lo demás igual. **Mira también que la imagen no cambia:** el arranque con la intro, el inicio, un nivel con vertidos, la pausa, Atrás, un anuncio de prueba a pantalla completa y volver de segundo plano, sin parpadeos, franjas negras ni la imagen estirada. Y tres casos más que tocan cómo se presenta el fotograma: (a) **el Mapa en reposo** unos segundos (pinta con `OnDemandRendering` a `perf_target_fps_menus`): sin parpadeo ni fotogramas viejos que se alternen, tampoco al tocar o desplazar; (b) **el paso de icono a intro** con el velo de `AfSplashHold`: igual que con el build anterior, sin negro ni petróleo liso nuevos; (c) **los bordes de la pantalla** (Safe Area con `androidRenderOutsideSafeArea: 1`): el fondo llega a los bordes y a la zona de la cámara, sin franjas. Si algo de eso falla, se anota con su captura y el escritorio vuelve a `Always`.

**Repetir 18.3 bloqueando más de 5 s (observación 9 de la hoja j6, 2026-10-29; carril `tanda6/fx-j6-bloqueo`).** El juego salía encima de la pantalla de bloqueo y se podía jugar sin desbloquear. La causa es la variante de desarrollo del reproductor de Unity, que lleva el `dev.apk` (pone `FLAG_SHOW_WHEN_LOCKED`, `FLAG_TURN_SCREEN_ON` y `FLAG_DISMISS_KEYGUARD` al crear la actividad); el nuevo plugin propio `AfLockGuard.androidlib` los quita (nota técnica del 2026-10-29 en `decisiones.md`). Con el build que lo trae, con el `dev.apk` y con el `rehearsal.apk`:
- en un nivel, bloquea el teléfono con el botón lateral y **espera al menos 30 s** (más que los 5 s de `lock_screen_lock_after_timeout`); al encender la pantalla debe salir **la pantalla de bloqueo, no el juego**, y tocar donde estaba el tablero no debe verter nada. Desbloquea: el juego sigue donde estaba, con su audio. Repítelo tres veces, una de ellas con la pantalla apagada sola por el tiempo de espera;
- con el juego encima, `adb shell dumpsys window windows`, en la ventana de `UnityPlayerGameActivity`: `fl=` **sin** los bits `0x80000`, `0x200000` ni `0x400000` (antes, `fl=0x80e90500`); y en `logcat` nada de `KeyguardService: setOccluded(true)` al encender la pantalla con el juego detrás;
- la sonda (solo en el `dev.apk`): `adb logcat -s AF_LOCK` escribe una línea en cada reanudación con `window_flags` (sin esos bits), `show_when_locked`, `turn_screen_on` y `dismiss_keyguard` a `false`, `activity_cleared` true, y `found` = `0x680000` con `cleared` ≥ 1 (lo que puso Unity y el plugin quitó). En el `rehearsal.apk` no hay sonda: solo cuenta lo que se ve;
- si sale un anuncio a pantalla completa, su pantalla es de Google y no se toca; basta con que, cerrado el anuncio, el bloqueo vuelva a funcionar.

**Comprobar los arreglos de las observaciones 2, 5 y 8 de la hoja j6** (carril `tanda6/fx-j6-ui`), con el build que los integre:
- **«Salto del Boto» en gris en la pausa** (observación 2): en un nivel con el Salto elegible (quinto fallo, < 300 semillas), con y sin tienda. Tocarlo hace latir su etiqueta, no abre la confirmación ni cobra, y sin tienda sale «Las compras no están disponibles ahora»; `AF_STATE missing_tap` con `"option":"pause_skip"`. Lo mismo en el **banner del Salto** de la entrada del nivel (tarjeta previa o tablero sin vertidos), que también estaba mudo: late «Te faltan {n} semillas» y `missing_tap` con `"option":"banner_skip"`. Con Reducir movimiento, solo se atenúan; con TalkBack (el dueño), dicen «Te faltan {n} semillas». Lo mismo **«+1 cuia» de la bandeja** sin stock y sin saldo (precio en rojo y «Te faltan {n}»): late su burbuja (con Reducir movimiento, solo el color), avisa sin tienda y `missing_tap` con `"option":"tray_add_cuia"`; no cobra ni añade la cuia.
- **Las pestañas del Mercado** (observación 5): al 100 % y al 130 % (`--ei af_font_scale 130`), en es-419, en y pt-BR, ninguna pareja de pestañas casi se toca: entre los textos queda al menos el hueco de un par de letras (12 dp). Capturas `b08/mercado-130-*.png` de nuevo.
- **Palabras partidas a 320 dp** (observación 8): con `wm density 540` y la letra al 130 %, en es-419, en y pt-BR, en un nivel con Aguas turbias (el 100: cinco comodines) y en el Mercado. Ninguna palabra se parte: «Reiniciar» y «Deshacer» encogen lo justo, solo por debajo de 360 dp (D-061) y nunca por debajo de 12 sp sin escala; a 360 dp no encogen. Las pestañas del Mercado, si no caben en una fila, van en dos filas de dos. Capturas como `b16/nivel.png` y `b16/mercado.png`.
- **TalkBack en los botones en gris** (el dueño): con 0 semillas, los botones en gris por el saldo se leen con «Te faltan {n} semillas» unido a su etiqueta, y la línea o la burbuja «Te faltan» no se lee otra vez suelta. Son el Salto del Boto de la pausa y de Aguas estancadas, «Continuar: +1 cuia» en Aguas estancadas y en ¿Seguro?, «+3 Deshacer», «Llenar canoas», y en la bandeja «+1 cuia», Pista, Pororoca, Luciérnaga y Marea.

**Repetir las ráfagas de N6.1 y de «La intro nace del logo» (observación 11 de la hoja j6, 2026-10-29; carril `tanda6/fx-j6-icono-salto`).** Al pasar de la pantalla del sistema a la intro, el icono saltaba un 12-16 % más pequeño (de ~486 a ~426 px) y el fondo cambiaba de (20,68,74) a (22,69,73); el dueño: «se ve como si fuera un error de sincronía». Las causas: Samsung pinta el icono del sistema a ~185 dp y la intro suponía los 160 dp de la guía; y en el espacio de color lineal el petróleo, puesto como color de vértice, se redondeaba a 8 bits en lineal. Ahora el plugin mide la vista del icono que entrega Android y la intro usa esos píxeles, y el petróleo de la intro sale de una textura (párrafo del 2026-10-29 en el añadido de D-051 «la intro nace del logo», `decisiones.md`). Con el build que lo trae, repite las ráfagas de N6.1 (primera apertura, tres en frío y el ensayo de release) a la mayor frecuencia que dé `screencap`, y en cada una compara **el último fotograma del sistema con el primero de la intro**. **La compuerta:**
- **el diámetro del círculo del icono, ±2 px**, y **su centro, ±2 px** (anota los dos fotogramas: diámetro y centro en px);
- **el fondo idéntico**: el petróleo, fuera del círculo, ±1 por canal en los dos (se espera (20,68,74) en ambos; mide en el PNG de `screencap`, no en un JPG);
- **el final de la intro igual de limpio:** compara también el último fotograma de la intro con el siguiente (cuando se quita la intro y queda la pantalla de arranque con TiTino): donde se ve el petróleo, ±1 por canal, sin cambio de tono;
- **un solo icono:** ningún fotograma en que el icono se vea a otro tamaño o «aparezca otra vez»;
- **la sonda** (`dev.apk`): en `AF_PERF boot_steps`, `os_splash_icon_px` ([ancho, alto]; en este teléfono, ≈ [486,486]) y `os_splash_icon_xy` ([izquierda, arriba]; su centro es xy + px / 2); en `AF_STATE brand_intro`, `morph_geometry` = `measured`, `morph_start_diam_px` y `morph_start_xy_px` ([x, y], el centro) iguales a los del sistema (±1 px), y `morph_start_diam_dp` ya no es 160 sino ≈ 185. `morph_geometry` = `guide` con `os_splash_veil` = `system` es un fallo: anótalo;
- **el juicio del dueño:** mira el arranque tres o cuatro veces en frío y dice si el paso del icono a la intro ya no «titila»; anota su opinión literal.

Con `os_splash_veil` = `own` (sin pantalla del sistema entregada) la caja es la de `af_launch_window` (160 dp centrados) y el criterio es el mismo.

### N6. Lo nuevo de la jornada 6

- **La pantalla del sistema hasta la intro** (carriles `tanda6/fx-j5-arranque` y `tanda6/fx-j6-splash`; añadido de D-051 del 2026-10-28, el dueño: «si hagamoslo asi»; observaciones 1 y 2 de la jornada 5 y 1 de la jornada 6). El icono v2 sobre el petróleo se queda hasta que el primer fotograma de la intro ya está en la pantalla: se debe ver **icono → escena de la intro**, sin el petróleo liso de en medio. Lo hace el plugin `AfSplashHold.androidlib` con un velo que no bloquea el pintado de la ventana y un tope de 3 s.
  - **Cómo medirlo:** una ráfaga de capturas (`screencap` cada ~110 ms, con la marca de tiempo de cada una, desde el `am start`) en cuatro casos:
    1. **primera apertura** (`pm clear` y `logcat -c` antes);
    2. **en frío**, cinco veces (`force-stop` y lanzar);
    3. **con Reducir movimiento** (lo enciende el dueño): la escena sale quieta y entera;
    4. **en el ensayo de release** (`rehearsal.apk`), primera apertura y en frío. Ahí no hay líneas `AF_*`: solo cuenta la ráfaga.
  - **El criterio:**
    - **ningún fotograma de petróleo liso sin icono** entre la pantalla del sistema y la escena;
    - **ningún salto de posición ni de tamaño del icono** mientras se ve;
    - el paso del icono a la escena puede ser un corte o el fundido corto del sistema.

    Anota en qué momento se quita el icono y en qué momento sale la escena, en cada ráfaga.
  - **La sonda** (`AF_PERF boot_steps` del `dev.apk`), campos nuevos:
    - `os_splash_release`: `signal` esperado; `cap` quiere decir que el juego no lo soltó y lo quitó el tope (es un fallo, anótalo con su ráfaga); `none`, que no hubo velo;
    - `os_splash_by`: `intro` esperado;
    - `os_splash_held_ms`: lo que duró el velo desde que empezó la actividad (< 3.000);
    - `proc_os_splash_ms`: del arranque del proceso a quitar el velo; debe caer donde la ráfaga pasa del icono a la escena, y después de `proc_awake_ms`;
    - `os_splash_veil`: `system` (Android 12+ entregó su pantalla y se quedó encima) u `own` (solo el velo propio, con `af_launch_window`);
    - `os_splash_focus_ms`: siempre -1 desde el velo sin bloqueo, que no toca el foco.

    Anota también `proc_awake_ms`, `proc_visible_ms` y el `TotalTime` de `am start -W` y compáralos con la jornada 5. Con el velo, **`TotalTime` vuelve a lo de la jornada 5**: la ventana se pinta enseguida y el velo va encima. La escena debe llegar como entonces (~0,9 s en frío y ~2,1 s en la primera apertura), no 3 s más tarde.
  - **La intro:** desde «la intro nace del logo» (punto aparte, abajo) su primer fotograma es el icono, pero sus cifras de siempre no cambian: `anim_ms` 3.500, `visible_by` gpu, `motion_frames` ≈ 175 (ahora de la apertura del icono al nombre entero, 2,9 s), `motion_pauses` 0, `cut` false. Mira con el dueño si el comienzo del vertido se ve entero tras el icono o si el fundido del sistema se come un trozo; anota su opinión literal.
  - **El velo no toca el foco ni los toques.** Compruébalo con tres cosas:
    - tras el arranque, baja la cortina de notificaciones con el nivel en marcha: el juego se pausa como antes (`pause` en logcat, la música se para);
    - pulsa Inicio **durante** el icono y vuelve: la intro termina normal, sin `E/Unity` y sin el icono colgado;
    - con TalkBack (lo hace el dueño), el icono del arranque no se enfoca ni se lee.
  - **Android 8-11:** este teléfono no lo tiene. Si hay un emulador o un teléfono viejo, la ventana de arranque (el icono sobre el petróleo, `af_launch_window`) debe quedarse igual hasta la escena (`os_splash_veil` = `own`).
  - **Arranque sin intro** (`os_splash_by` = `boot_end`): ahí la sonda puede decir `os_splash_release: pending`, porque se escribe en el mismo fotograma en que se pide soltarlo; no es un cuelgue si la ráfaga muestra la escena. Anótalo si sale.
  - **Abrir en frío desde una notificación** (la del Reto del río, si hay; si no, un `am start` con el deep link o la intención de la notificación): durante el velo se ve el icono sobre el petróleo, nunca la pantalla anterior congelada. Ahí `os_splash_veil` puede decir `own`: fuera del lanzador, Android 13+ entrega su pantalla sin icono y el plugin la cambia por el velo propio.
- **Los arreglos de UI** (carril `tanda6/fx-j5-ui`; observaciones 5 y 6):
  - **«Continuar: +1 cuia» en gris** (observación 6; guion 8.2), con el `dev.apk`, desde el 31 y con 0 semillas:
    - **con la tienda falsa** (`--ez af_fake_store true`), en Aguas estancadas: tocar el botón gris hace latir «Te faltan {n} semillas» (crece un poco y vuelve en ~0,4 s); no se cobra nada, el modal sigue y no sale ningún aviso (debajo están Puñado y Balsa); `AF_STATE missing_tap` con `"option":"rescue","pulsed":true,"store":true`;
    - **sin tienda** (el build normal sin la app en Play Console, como en la jornada 5): el mismo latido y, arriba, el aviso «Las compras no están disponibles ahora», el de «Canoas infinitas» en Sin canoas; `AF_STATE store_unavailable_notice` y `missing_tap` con `"store":false`;
    - lo mismo en **¿Seguro?** («Rendirse» con una canoa en juego);
    - **con Reducir movimiento** (el dueño): la línea no crece, solo se atenúa y vuelve;
    - **con saldo** (≥ 300), el rescate de siempre;
    - con **TalkBack**, el botón gris se puede activar, responde igual y dice «Te faltan {n} semillas» (también «+3 Deshacer», que no tiene la línea a la vista);
    - **dos toques seguidos:** el latido vuelve a empezar y, sin tienda, salen dos avisos; no se cobra nada;
    - **¿Seguro? abierto y semillas que llegan** (si hay forma de sumarlas con el modal abierto): el rescate pasa de gris a disponible sin cerrar el modal;
    - en es-419, en y pt-BR, captura del aviso sobre el modal.
  - **Los otros botones en gris por falta de semillas** (mismo trato; `missing_tap` con su `option`), con y sin tienda:
    - **«+3 Deshacer · 30 semillas»** en Aguas estancadas, sin cupo de Marea, sin video (`--es af_ads_fill no_fill`) y con < 30 semillas: late su propia etiqueta, que lleva el precio (`refill_seeds`);
    - **Salto del Boto** en gris (rama D, quinto fallo en un nivel con < 300 semillas): late «Te faltan {n} semillas» y no se abre la confirmación (`skip`);
    - **«Llenar canoas»** en Sin canoas sin saldo, desde el nivel (Nivel fallido › Reintentar con 0 canoas) y desde el inicio (el contador en 0/5): late «Te faltan {n} semillas» y no llena nada (`refill_canoes`);
    - sin tienda, en los tres sale además el aviso «Las compras no están disponibles ahora»; también en el **Reto del río** con «+3 Deshacer» en gris;
    - con Reducir movimiento, solo se atenúan.
  - **Tu fila de la Liga sin seudónimo** (observación 5; `--ez af_league true --ez af_league_fake true --ei af_max_level 60`): «Gana un nivel para entrar en la Liga» se para antes de la columna de puntos, con aire, y si no cabe pasa a dos líneas y la fila crece. En es-419, en y pt-BR, al 100 % y al 130 % (6 capturas); en pt-BR, el «0» ya no toca el texto.
- **Las skins y el Cántaro con su dibujo** (carril `tanda6/fx-j6-skins`; observación 4 de la jornada 6 y 7 de la jornada 5). En la pestaña Cuias del Mercado (`--ei af_max_level 141`, con las skins ganadas, y `--ei af_max_level 46`), cada skin sale con su cuia sobre una ficha azul petróleo (el panel del tablero), en filas más altas y con el nombre centrado: la del tablero en pequeño, con su borde, su pie, su banda (bicolor en la del Encuentro, algo reforzada) y el asa de la Totuma, y con dos dosis de líquido; las cuatro se tienen que reconocer a primera vista; ninguna es ya un círculo de color. La tarjeta de la skin (con la venta de skins encendida) lleva la misma ficha, más alta, y el Cántaro, en su tarjeta y en su fila de Semillas, la tinaja del inicio, no un círculo marrón. En el Mapa, la tarjeta de la parada futura con «Premio del mundo · faltan N» enseña también la cuia de su skin (la píldora de la boya sigue con la mini cuia teñida: a 16 dp la ficha no se leería). Comprueba que la skin puesta en el Mercado es la que se ve en el tablero del nivel siguiente, y con TalkBack, que «Poner» dice el nombre de la skin («Cuia de Santarém, Poner»). Capturas de referencia del Editor: `docs/art/mercado-skins/`.
- **La intro nace del logo** (carril `tanda6/intro-desde-logo`; añadido de D-051 del 2026-10-28, el dueño: «si podriamos hacer que el logo se convierta en la animación actual, algo muy pro»). Va con el build que lleve también el velo nativo de `tanda6/fx-j6-splash`; con el build de ahora solo vale lo de la apertura y Reducir movimiento. El primer fotograma de la intro es el icono de la pantalla del sistema (el círculo de 160 dp con la vista del icono a 240 dp, centrados, sobre el petróleo); en ~0,8 s con un **corte sobre la cuia**: la cuia (con su chorro y sus aros) se queda y viaja a su sitio en la escena, el resto del icono se va al petróleo en ~180 ms, el círculo se abre en 0,7 s y la escena entra desde el petróleo alrededor de la cuia; el açaí se recoge hacia la cuia y a los 0,55 s empieza el vertido. La intro sigue en 3,5 s. Capturas de referencia del Editor: `docs/art/intro-desde-logo/`.
  - **Cómo medirlo:** ráfagas de `screencap` cada ~120 ms, con la marca de tiempo de cada una, desde el `am start` hasta el nombre entero (~5 s), en tres casos:
    1. **primera apertura** (`pm clear` y `logcat -c` antes);
    2. **en frío**, tres veces (`force-stop` y lanzar);
    3. **en el ensayo de release** (`rehearsal.apk`), primera apertura y en frío; ahí solo cuenta la ráfaga.
  - **El criterio, en cada ráfaga:**
    - **sin salto del velo al primer fotograma de la intro:** el mismo círculo, en el mismo sitio y del mismo tamaño (compara el último fotograma del velo con el primero de Unity: el centro y el diámetro del círculo, en píxeles, no deben moverse más de 2 px; anota los dos);
    - **la apertura es fluida:** el círculo crece sin tirones ni saltos, la cuia no salta en el corte y **nunca se ven dos ríos a la vez** (el del icono se va al petróleo antes de que entre el de la escena), ni ningún borde recto de la escena dentro del círculo;
    - **el açaí se recoge y vuelve a verter** como un solo gesto (sin quedarse quieto en medio).
  - **La sonda** (`AF_STATE brand_intro` del `dev.apk`), campos nuevos: `morph_mode` = `open`; `morph_ms` ≈ 700 (de empezar a moverse a cubrir la pantalla; mucho más quiere decir tirones o pausas); `morph_start_diam_dp` = 160,0; `morph_frames` ≈ 42 a 60 fps; `morph_units_per_dp` (en el SM-S938B, la densidad entre la escala del lienzo; 1,000 querría decir que no se leyó la densidad: es un fallo, anótalo); `morph_build_ms` (lo que costó preparar el icono antes del primer fotograma; anótalo en la primera apertura y en frío). `anim_ms` = 3.500; `motion_frames` (≈ 175), `frame_p95_ms` (≈ 16,7) y `motion_pauses` (0) como en la jornada 5, ahora con la apertura incluida. Con Android 12+, la pantalla del sistema lleva ahora fondo de icono (el mismo petróleo), así que siempre usa la caja de 240 dp con el círculo de 160 dp: compruébalo en la primera captura de la ráfaga.
  - **Con Reducir movimiento** (lo enciende el dueño): ni apertura ni recogida; el icono se funde en la escena quieta en ~200 ms (en la ráfaga, uno o dos fotogramas a medias) y el nombre entra con él. `morph_mode` = `fade` y `morph_ms` = -1.
  - **El dueño** mira la intro tres o cuatro veces en frío y dice si queda «muy pro»; anota su opinión literal y, si pide cambios, qué parte (el círculo, el fundido del icono, la recogida o el ritmo).
- **TalkBack al desplazar la Liga** (carril `tanda6/fx-j6-liga-alarma`; observación 7 de la jornada 6). Lo hace el dueño con TalkBack, en la Liga con el top 100 (`--ez af_league true --ez af_league_fake true --ei af_max_level 60`):
  - desplaza la lista con dos dedos hasta la mitad y **toca una fila con un dedo**: se lee el puesto y el nombre de esa fila, la que se ve, no la que estaba antes en ese sitio;
  - con el foco en una fila de arriba, desplaza hasta que salga del visor: cuando la lista para, el foco pasa a la primera fila que se ve, y deslizar a la derecha sigue hacia abajo en el orden que se ve (puesto n, n + 1…);
  - **un desplazamiento rápido:** ¿cuántas filas lee antes de parar? Lo esperado es ninguna mientras corre (el foco no salta de fila en fila) y una, la primera que se ve, al parar; anota lo que lee;
  - lo mismo, más corto, en el Mercado (Semillas al 130 %) y en Ajustes: tocar tras desplazar lee lo que se ve.
- **La música con una alarma y con otra app** (carril `tanda6/fx-j6-liga-alarma`; observación 10 de la jornada 6; D-026, añadido del 2026-10-29, el dueño: «hagamoslo asi entonces»; plugin propio `AfAudioFocus.androidlib`):
  - **sin música de otra app, la alarma:** en un nivel con la música sonando, el dueño pone una alarma a un minuto.
    - La sonda: al sonar, `AF_STATE audio_focus` con `state` `loss_transient`, `paused` true y `music_gain` 0.00; al apagarla, `state` `gain` y `music_gain` 1.00. Una notificación con sonido o la voz del navegador, si la hay, da `duck` con `music_gain` 0.20.
    - El oído del dueño: con la alarma, la música y los efectos del juego se callan; al apagarla, la música vuelve con un fundido corto. Con un `duck`, la música baja y sigue oyéndose.
  - **con música de otra app:** el dueño pone Spotify o YouTube Music y después abre el juego (en frío y volviendo de segundo plano). La otra música **sigue sonando**, la del juego no suena y los efectos sí (al tocar una cuia). La sonda da `audio_focus` con `state` `yielded`, `music_gain` 0.00 y `paused` false. Después para la otra música, pasa el juego a segundo plano y vuelve: la música del juego entra (`gain`).
  - **lo que no debe cambiar:** una llamada sigue parando la música y la devuelve al colgar; un anuncio de AdMob suena solo y, al cerrarlo, la música vuelve (`audio_focus` `gain` si el anuncio se quedó el foco); bajar la cortina y subirla no cambia el foco; con la Música apagada en Ajustes solo sale un `none`.
  - **si al volver de segundo plano sin otra música sale `yielded`**, el juego se confunde con su propio audio: anótalo con la sonda (es el riesgo de la nota de decisiones).

## Encargo de la jornada 5 (escritorio, 2026-10-28)

La jornada 4 (`pruebas-telefono-2026-10-27-completa-j4.md`) cubrió el bloque 0, N4 y casi todo N4b, y dejó 5 observaciones. Sus arreglos ya están integrados. **Esta jornada decide el lanzamiento:** el dueño acordó que, si no sale nada, se pasa al Release definitivo y a publicar (D-059; `docs/dev/release.md` §4 bis). Así que cualquier observación **bloqueante** o **alta** se anota con su traza y sus capturas, y se avisa en cuanto aparezca.

**Punto de partida:** `integracion-fase1-fase2` en `e0c3adf2` o posterior.

**Lo que entró después de la jornada 4 y nunca se ha visto en el teléfono:**
- **La intro de 3,5 s, con el vertido fluido** (`6336136c` y `97e258a1`; añadido de D-051 del 2026-10-28, petición del dueño: «un poco más fluida… y dejarla un segundo más»):
  - el chorro llega al agua a los 0,8 s, el río se tiñe hasta los 2,5 s, «Tintalú» entra entre los 2,35 y los 2,9 s, y el emblema se sostiene 0,6 s;
  - la carga ya no corre mientras la intro se mueve, y el açaí avanza con un fundido, no por tramos;
  - la intro no se mueve hasta que la GPU confirma que se ve (`AsyncGPUReadback`, con un tope de 1,2 s).
  - **Observación 1 de la jornada 4 (≈ 1 s de petróleo liso en la primera apertura): solo diagnosticada.** Esta jornada la mide (N5.1).
- **El toque con la lista en movimiento** (`9dc83bc9`, observación 3): en las 6 listas desplazables, un toque con la lista aún en inercia la para sin activar la fila.
- **La confirmación neutral al corregir la edad** (`9dc83bc9`; añadido de D-033 del 2026-10-28, el dueño: «hagamos de acuerdo al estandar»). Revisión 3 de Gemini aplicada en `140cbbd3`.
- **Tu fila de la Liga** (`9dc83bc9`, observaciones 4 y 5): toda la fila abre «Tu avatar», y «Tú» sale una sola vez.
- **Sin cambios en el juego, para que no cuenten como observación:**
  - la excepción `GoogleMobileAdsNative.Common` del plugin de anuncios es ruido conocido (observación 2; `docs/dev/anuncios-admob.md` §8 ter);
  - D-059 (cómo se publica) y los arreglos de la CI.

### Orden de la jornada 5

1. **Bloque 0 entero**, con el `dev.apk` y el `rehearsal.apk` nuevos (`-Apk`) y todas sus compuertas, también `check-no-applovin`. Anota los tamaños: en la jornada 4 eran 76,94 MB, 59,81 MB y 65,32 MB.
   - En el **ensayo**, la compuerta de textos (`[AF.LocReleaseGate] OK` en el log del build) ya no debe quejarse de nada: no queda ninguna fila en `traducido`.
2. **Bloque N5, «Lo nuevo de la jornada 5»** (abajo).
3. **Los pendientes**, en orden:
   - los 3 puntos del bloque 3;
   - el bloque 4 entero, salvo el 4.9;
   - los bloques 5 y 7 a 23.
   - Lo que hace el dueño (TalkBack, Reducir movimiento, la hora, 320 dp y el 130 % del sistema, el icono monocromo y el cajón de apps, Ad Inspector, la cuenta sin SIM) se agrupa en una sesión con él al lado.
   - Bloque 21 (borrar la cuenta de verdad): al final de todo, como siempre.
4. **Hoja nueva:** `docs/dev/traspaso/pruebas-telefono-<fecha>-completa-j5.md`, con el mismo formato. Al final de la jornada, commit y push con el mensaje «Añade la prueba completa en el teléfono (jornada 5)», solo la hoja. Si no da para todo, se commitea lo hecho y la jornada 6 sigue donde quedó.
5. **Anuncios:** como en la jornada 4. Paso a paso con captura antes de cada toque, y los recorridos automáticos con `--es af_ads_fill fill|no_fill|error` (texto, no entero).
6. **No es de esta jornada:** la prueba de compras de verdad. La hace el dueño en la pista interna con el AAB del Release definitivo (`docs/dev/iap.md` §3; `release.md` §4 bis). Aquí el Mercado se sigue probando con la tienda simulada (`--ez af_fake_store true`) y con los bloques 8 y 9.

### N5. Lo nuevo de la jornada 5 (≈ 2 h 30 min)

Con el `dev.apk`, salvo donde se diga.

1. **La intro de 3,5 s y el segundo de petróleo** (la medida decide la pregunta abierta del punto 5 del añadido de D-051 del 2026-10-28):
   - **Primera apertura** con `pm clear`, `logcat -c` y una ráfaga de capturas (`screencap` cada ~110 ms, con la marca de tiempo de cada una):
     - anota **en qué momento empieza y acaba el petróleo liso**, si lo hay, y si en su lugar se ve la escena quieta (la cuia sobre el río);
     - de `AF_STATE brand_intro`: `first_frame_ms`, `visible_ms`, `visible_by` (`gpu` esperado; `cap` o `no_readback` también se anotan), `play_ms`, `motion_frames` (≈ 170-175), `frame_p95_ms` (≤ 20), `frame_max_ms` (≤ 40), `motion_pauses` (0), `shown_ms` (~3.500), `cut` (false), `loading_ms` y `wait_capped` (false);
     - de `AF_PERF boot_steps`: `awake_ms`, `proc_awake_ms`, `proc_visible_ms`, `visible_ms`, `play_ms` y `motion_hold_ms` (~2.900), y el `TotalTime` de `am start -W`.
     - **Cómo leerlo:**
       - si `proc_visible_ms` cae donde acaba el petróleo liso y `visible_ms` − `first_frame_ms` ronda 1 s, el hueco es de la GPU (sombreadores sin caché);
       - si `proc_awake_ms` ya llega al final del hueco, es el arranque del motor;
       - escribe tu conclusión en la hoja, con las cifras.
   - **Cinco arranques en frío** (`force-stop` y lanzar): las mismas cifras en una tabla, como en la jornada 4. Con la caché (`emblem_cache` hit) no debe haber petróleo liso.
   - **La mirada del dueño:** ¿el vertido se ve **fluido**? ¿Los 3,5 s se aprecian sin hacerse largos? ¿El açaí entra bien con el fundido? Anota su opinión literal.
   - **«Cargando…»** tras la intro en la primera apertura: si sale, cuánto dura (se espera ≈ 0,4 s o nada).
   - **Inicio durante el vertido y volver:** la intro termina, se llega a la edad o al nivel, `cut` false y 0 `E/Unity`.
   - **Reducir movimiento** (lo enciende el dueño): la escena quieta ~1 s, que puede alargarse un poco si la GPU tarda en confirmar.
   - **Primera apertura completa:** primer toque en el nivel 1 por debajo de 20 s (en la jornada 4, 11,0 s; ahora se espera algo más por el segundo extra).
2. **El toque con la lista en movimiento** (bloque 13, punto 0): lanza cada lista con un deslizamiento rápido y toca una fila mientras sigue moviéndose. **La lista se para y la fila no se activa**; el siguiente toque, con la lista quieta, sí la activa.
   - Pruébalo en las seis: Ajustes (sobre «Corregir mi edad»), la Liga, el Mercado, «Tu avatar», la hoja de la fruta y las muestras del Mapa.
   - Anota también el caso contrario: un toque con la lista **casi** parada que no se activó cuando debía. El umbral es [P], de 30 dp/s (`FlingStopScrollRect.StopTapSpeedDp`).
3. **La confirmación neutral al corregir la edad** (bloque 13, punto 2), en Ajustes › Privacidad › «Corregir mi edad»:
   - elegir **la misma** banda que la guardada: no sale confirmación y no se gasta la corrección;
   - elegir **otra** banda: sale «Corregir mi edad · Elegiste: {banda}. La edad solo se puede corregir una vez y no se puede deshacer.» con «Volver a elegir» y «Confirmar»;
     - captura de la confirmación con **las tres bandas**, en es-419, en y pt-BR (9 capturas). Deben ser **idénticas salvo la banda**: ni avisos para «Menos de 13» ni un botón con más peso;
     - en inglés debe decir «…and **this** can't be undone.» (revisión 3 de Gemini);
   - **«Volver a elegir»**, **Atrás** y la **X**: no se gasta la corrección (la fila sigue en Ajustes);
   - **«Confirmar»** con 13-17: se guarda, la fila desaparece y `af_settings_change` con `value=age`;
   - **«Confirmar» con «Menos de 13»:** bloquea, como siempre. Hazlo **solo en un perfil desechable** (`pm clear` antes y después), nunca en el de la prueba;
   - la pantalla de edad del **primer arranque** no lleva confirmación;
   - con TalkBack (el dueño): lee el título, la frase y los dos botones.
4. **Tu fila de la Liga** (`--ez af_league true --ez af_league_fake true --ei af_max_level 60`):
   - tocar **cualquier punto** de tu fila (el disco, el centro, una esquina) abre «Tu avatar»; tocar la fila de otro jugador no hace nada;
   - con seudónimo: el seudónimo como nombre y la chapa «Tú» **una vez**; sin seudónimo: solo la chapa «Tú» y «Gana un nivel para entrar en la Liga», centradas. En los tres idiomas.
5. **El ensayo de release (`rehearsal.apk`):**
   - la intro de 3,5 s, fluida;
   - la confirmación de edad;
   - la fila de la Liga;
   - 0 líneas `AF_*`.

## Encargo de la jornada 4 (escritorio, 2026-10-26)

**Actualización del 2026-10-27:** la jornada 4 aún no se ha hecho y la integración siguió avanzando. **Nuevo punto de partida:** `integracion-fase1-fase2` en `1048450d` o posterior. Todo lo de abajo (N4) sigue valiendo; se suma el bloque N4b.

**Lo que entró después y nunca se ha visto en el teléfono:**
- **Los personajes en «3D cartoon, juguete brillante»** (D-058; piloto `bfc69c56`, fase 2 `0e2d4f05` y `eb501998`, el delfín `fb71b335`; integrado en `ceffe37f`):
  - TiTino (todas las poses, de día y de noche), Capi, Rosa y el bufeo, la morpho, la fauna (miquitos, churuco) y los 16 avatares;
  - volumen, luz arriba a la izquierda, barniz, ojos con reflejos y sin el contorno grueso;
  - guía `docs/art/estilo-personajes-3d.md`; capturas de referencia en `docs/art/personajes-3d/piloto/despues/` y `docs/art/personajes-3d/fase2/despues/`.
- **Avatares del jugador** (D-056 y D-057; `e9212484`, `daebca84`; diseño en `docs/dev/avatares.md`): el disco del inicio desde el 10, «Tu avatar» con la rejilla, los 10 hitos, la chapa «Nuevo», el globo del bloqueado, la fila de Ajustes, la Liga y la línea de la Liga en la victoria.
- **«Enviar informes de fallos»** en Ajustes, activado por defecto (T-L6, D-057; `d9c003e6`, `2e4be455`, `cdbd047b`), y la frase de la pantalla de consentimiento que lo anuncia.
- **La Mochila de inicio de 48 h** se quita al llegar a cero con el Mercado o su tarjeta abiertos (T-L9; `d2aaa13e`).
- **Lanzamiento mundial** (D-057; `dd4ca621`, `972e8b58`, `fb78f26b`, `e03ca0ef`; `docs/dev/lanzamiento-mundial.md`):
  - el país de la cuenta de Play como fuente del país legal, entre la SIM y la región;
  - los nombres de país de CLDR en la Liga, en los tres idiomas;
  - los decimales de ISO 4217 en los precios;
  - 13-17 sin país conocido: sin analítica.
- **Play Age Signals 0.0.4** (`AfAgeSignals.androidlib`; T-L3 a T-L5; `40987339`, `a5b31226`, integrado en `71f2c2f8`): `consent_age_signal_source` = `self_declared+play_age_signals` por defecto; 0-12 bloquea; la banda de la señal nunca va a la analítica; `rdp=1` con 13-17 en EE. UU.
- **Anuncios** (añadidos de D-049 del 2026-10-27):
  - AppLovin MAX sale del build (`5980da99`, `0cb6036d`, integrado en `2cfa20aa`) y entra la compuerta `ci/check-no-applovin.ps1`;
  - Unity Ads 4.20.1 como red de mediación de AdMob, con el adaptador 3.21.1 y sin su inicializador automático (`7e5c12a3`, integrado en `5b6c1aa2`);
  - el recompensado de producción pasa al bloque `1182085615` (`cbcfdfc4`, integrado en `6b1ba42c`). Los builds de prueba siguen con los bloques de prueba de Google (`docs/dev/anuncios-admob.md` §3), así que ese bloque no se ve en el teléfono.
- **El icono y el logo con dos meandros** (variante B, añadido de D-045; `52e256bf`, integrado en `467ad06d`): el icono adaptativo, el monocromo, la ventana de lanzamiento y el emblema de la intro.
- **La revisión 2 de Gemini** en inglés y portugués de Brasil (añadido de D-054; `9ccbe4e2`): las 27 claves de los avatares y del interruptor, y «Vença um nível para entrar na Liga».
- **La web legal** (`a0fa231d`, `c95d6c5f`): Parallext, LLC, «Vigente desde: 30 de octubre de 2026» y Unity Ads en Créditos. Es la web, no la app: la app no abre enlaces.
- Arreglos de la CI y de los tests (`83ac9fa3`, `29278417`…): no cambian el juego, no hace falta probarlos.

La jornada 3 (`pruebas-telefono-2026-10-26-completa-j3.md`) cubrió el bloque 0 y N3. Sus observaciones están respondidas al final de esa hoja. Esta jornada empieza otra vez por el bloque 0 sobre la integración nueva, comprueba lo que entró y sigue con lo pendiente.

**Punto de partida:** `integracion-fase1-fase2` en `2ec9bdba` o posterior.

**Lo que entró después de la jornada 3 y nunca se ha visto en el teléfono:**
- **La intro de la marca entera** (`76bfd06f`; añadido de D-051 del 2026-10-26, decisión del dueño):
  - unos 2,5 s en cada arranque en frío, sin cortarse aunque la carga acabe antes;
  - «Cargando…» si la carga dura más;
  - no se repite al volver de segundo plano;
  - con Reducir movimiento sale quieta 1 s;
  - para las observaciones 1 y 2 de la jornada 3: el icono v2 en el fondo de la ventana de lanzamiento (sin petróleo liso), la escena guardada en disco a partir de la segunda apertura y la carga repartida en fotogramas para que la intro anime.
- **TiTino de noche** (`b76914b3`; añadido de D-050): de 20 a 6 duerme callado con un «Zzz». Al tocarlo se despierta, dice su mensaje y se vuelve a dormir. El saludo de regreso se guarda para la primera visita de día.
- **El viaje de Capi en el Mapa** (`1f0b981d`; D-053), en el Mapa del 10, en «Ver mapa» de la victoria de cada parada y en el fin de contenido:
  - la canoa se mueve desde el primer instante y sale a los 250 ms;
  - «Jugar»/«Volver» y «←» no aparecen hasta que Capi llega;
  - el toque acelera el viaje, no lo salta;
  - Atrás cierra el Mapa.

### Orden de la jornada 4

1. **Bloque 0 entero**, con el `dev.apk` y el `rehearsal.apk` nuevos (`-Apk`). Anota los tamaños: en la jornada 3 eran 76,07 MB, 59,10 MB y 64,62 MB (Unity Ads suma unos 2 MB y MAX resta; `docs/dev/anuncios-admob.md` §12.4).
   - Pasa también la compuerta sin AppLovin sobre los tres artefactos (0 OK · 1 lo lleva · 2 argumentos o ilegible):
     ```
     pwsh -NoProfile -File ci/check-no-applovin.ps1 Builds/Android/dev.apk
     pwsh -NoProfile -File ci/check-no-applovin.ps1 <aab>
     pwsh -NoProfile -File ci/check-no-applovin.ps1 <apk del ensayo>
     ```
   - En `check-manifest`, anota si aparece el inicializador de Unity Ads (`AdsSdkInitializer`): no debe estar.
2. **Bloque N4, «Lo nuevo de la jornada 4»** (abajo).
3. **Bloque N4b, «Lo nuevo del 2026-10-27»** (abajo, después de N4).
4. **Los pendientes:**
   - los 3 puntos sin probar del bloque 3;
   - el bloque 4 entero, salvo el 4.9;
   - los bloques 5 y 7 a 23, en orden, con lo que quedó de N1: Reducir movimiento del sistema, la mañana y el mediodía del inicio (bloque 15), el 16:9 (bloque 16) y los anuncios sin red o cerrados antes de tiempo (bloque 9).
5. **Hoja nueva:** `docs/dev/traspaso/pruebas-telefono-<fecha>-completa-j4.md`, con el mismo formato. Al final de la jornada, commit y push con el mensaje «Añade la prueba completa en el teléfono (jornada 4)», solo la hoja. Si la jornada no da para todo, se commitea lo hecho y la jornada 5 sigue donde quedó.
6. **Anuncios de Google:** paso a paso, con una captura antes de cada toque. Los recorridos automáticos, con el anuncio simulado: `--es af_ads_fill fill`, `no_fill` o `error`. **Corrección del 2026-10-27:** el extra es de texto (`LaunchOptions.ResolveAdFill` lo lee con `getStringExtra`), así que `--ei af_ads_fill 0/1` no se lee y deja los anuncios de Google; si la jornada 3 lo usó así, anótalo.

### N4. Lo nuevo de la jornada 4 (≈ 2 h)

1. **La intro entera, con el `dev.apk`:**
   - **Primera apertura** con perfil limpio (`pm clear`), con ráfaga de capturas (`screencap` cada ~110 ms):
     - entre la pantalla del sistema y la escena **se ve el icono centrado sobre el petróleo, no un petróleo liso**; el icono no salta de tamaño ni de sitio;
     - la intro **se ve moverse entera**: la cuia vierte, los aros, el açaí tiñe el río con meandros, entra «Tintalú» y el emblema se sostiene;
     - si sale «Cargando…», cuánto dura;
     - después TiTino despierta al menos 1 s y llega la pantalla de edad.
   - **Cinco arranques en frío** (`force-stop` y lanzar). De cada `AF_STATE brand_intro`, anota `intro_ms`, `shown_ms` (~3.500 desde el añadido de D-051 del 2026-10-28; ~2.500 antes), `cut` (false), `frames` (decenas), `first_frame_ms`, `start_ms`, `emblem_ms`, `emblem_cache` (`miss` la primera vez y `hit` las demás), `yields`, `loading_ms` y `wait_capped` (siempre false); desde el 2026-10-28, también `visible_ms`, `visible_by` (`gpu`), `play_ms`, `motion_frames` (≈ 170-175), `frame_p95_ms` (≤ 20) y `frame_max_ms` (≤ 40). Anota también la línea `AF_PERF boot_steps` (`awake_ms`, `start_ms`, los bloques y, desde el 2026-10-28, `motion_hold_ms`, `proc_awake_ms` y `proc_visible_ms`) y el `TotalTime` de `am start -W`. Con eso se sabe cuánto es arranque del motor y cuánto es nuestro.
   - **`load_ms` frente a los 790-826 ms de la jornada 3:** con la caché debería bajar.
   - **Al volver de segundo plano** (Inicio y volver) la intro **no** se repite; al cerrar la app y volver a abrirla, **sí**.
   - **Pausa a mitad:** en una primera apertura, pulsa Inicio durante la intro, vuelve y comprueba que llega al nivel 1 sin errores ni `E/Unity`.
   - **Reducir movimiento** (lo enciende el dueño): la escena sale quieta y se sostiene ~1 s.
   - **Tamaño de la caché:** `adb shell du -sh /data/data/com.amazonfill.dev/cache` (o `run-as`), que da unos 8-9 MB.
   - **Primera apertura completa:** el primer toque en el nivel 1 por debajo de 20 s.
2. **TiTino de noche** (con la hora cambiada a las 22:00, con el sí del dueño; después, otra vez la hora automática):
   - en el inicio, dormido en la rama con el «Zzz» subiendo y **sin bocadillo** (`AF_STATE titino_sleep` con `asleep` true y `"titino_asleep":true` en la línea `AF_LAYOUT lobby`);
   - al tocarlo, se despierta, dice un mensaje y a los ~4 s vuelve a dormirse con el «Zzz»;
   - con los mensajes de TiTino apagados en Ajustes, se despierta y se duerme sin hablar;
   - con TalkBack, dice «TiTino duerme»;
   - el saludo de regreso: con `last_seen_date` de hace 4 o más días, entrar de noche no saluda, y la primera visita de día sí lo hace;
   - de paso, la mañana (el saludo) y el mediodía (tomando el sol: tumbado, con la cabeza unida al cuerpo), que quedaron pendientes.
3. **El viaje de Capi en el Mapa:**
   - **Mapa del 10** (perfil limpio, jugando del 1 al 10): desde el primer instante, la canoa se balancea, la parada de destino late y suena el remo; Capi sale enseguida (`AF_STATE map_play_ready` con `first_move_ms` de unos 260); «Jugar · Nivel 11» y «←» **no se ven** hasta que llega a Santarém; después entran con un rebote.
   - **Un toque durante el viaje** lo acelera y no lo salta. **Atrás** cierra el Mapa sin dejarte atascado.
   - **«Ver mapa» en la victoria del 20** (o de cualquier `10k`): la canoa repite el viaje del tramo ganado y «Volver» sale al llegar. En logcat, un solo `stop_arrive`, `map_open` y `map_close`.
   - **Segundo plano a mitad del viaje:** al volver, el viaje termina y salen los botones (`by: resume`).
   - **Con TalkBack:** durante el viaje dice «Capi va río arriba. Toca para ir más rápido.» y al llegar anuncia el lugar y el botón.
   - **Desde la pestaña o la pausa**, el Mapa se abre como antes, con el botón desde el principio.
   - Capturas del principio, de la mitad y de la llegada.
4. **El ensayo de release (`rehearsal.apk`):** la intro entera, el icono en la ventana y el viaje del Mapa del 10 se ven igual que en desarrollo.

### N4b. Lo nuevo del 2026-10-27 (≈ 3 h 30 min)

Con el `dev.apk`, salvo donde se diga. Las capturas, con `adb exec-out screencap -p`, en `Logs/device/` con el nombre del punto. Los extras de lanzamiento solo valen en desarrollo.

1. **Icono y logo con dos meandros** (añadido de D-045):
   - **El icono en el lanzador:** el río con dos meandros, la cuia dentro de la zona segura, sin ruido en las orillas lejanas. Captura del cajón de apps.
   - **El monocromo** (lo enciende el dueño: iconos temáticos del sistema, si One UI los ofrece): el icono se lee a una tinta. Si el sistema no tiene la opción, anótalo.
   - **La ventana de lanzamiento:** el icono nuevo centrado sobre el petróleo (ráfaga de capturas, como en N4.1).
   - **El emblema de la intro:** el río con los dos meandros. En el primer arranque tras instalar, anota de `AF_STATE brand_intro` el `emblem_cache` (con el emblema nuevo, la primera vez debería ser `miss`) y el `emblem_ms`.
   - **Con el `rehearsal.apk`:** lo mismo.
2. **Los personajes de juguete brillante** (D-058). Compara cada sitio con su captura de referencia de `docs/art/personajes-3d/piloto/despues/` (`p3d_*`) y `docs/art/personajes-3d/fase2/despues/` (`f2_*`), y haz la captura del teléfono con el mismo nombre:
   - **TiTino en el inicio:** de mañana (`p3d_01_inicio`, nivel 31 a las 8:00), a mediodía tomando el sol y de noche dormido (`p3d_02_inicio_noche`, 22:00). La hora la cambia **el dueño**; después, otra vez la automática. Se puede juntar con N4.2.
   - **TiTino en el arranque, la victoria** (`p3d_05_victoria`, el hito del 40 con `--ei af_level 40`), **la banda de los tutoriales** (niveles 1-3 y 6) **y la ventanita del Mapa.**
   - **Capi y Rosa en el Mapa:** «Ver mapa» del 20 a mitad del viaje (`p3d_04_mapa_viaje`) y el bufeo guiando la canoa con `--ei af_max_level 305` (`f2_05_mapa_bufeo`).
   - **Rosa en el banner de mundo:** al asomar y en reposo (`f2_08_delfin`, `f2_08b_delfin_reposo`): la espuma tapa el corte con el agua, sin línea dura.
   - **La fauna:** la tropa del nivel 2 (`f2_03_intro_tropa`) y el churuco del 82 (`--ei af_level 82`, `f2_04_intro_churuco`).
   - **La morpho:** el botón del comodín y su vuelo.
   - **En cada sitio:** volumen, luz de arriba a la izquierda, brillo de barniz, ojos con reflejos, sin el contorno grueso de antes; sin bordes dentados, borrosos ni costuras (el párpado de TiTino dormido encaja con la cara).
   - **Alto contraste** (lo enciende **el dueño** en Ajustes del juego): repite el inicio, la tropa, el Mapa, la victoria y la Liga (las capturas `_ac`). Los personajes **no cambian** (guía §5); solo cambia el tablero.
   - **A 32 dp y a 24 dp:** los avatares en las filas de la Liga (32 dp, `f2_07_liga`) y en la línea de la Liga de la victoria (24 dp, `f2_06_victoria_avatar`); se reconocen por la silueta. Capturas recortadas de cerca.
   - **Fluidez:** ningún tirón la primera vez que sale cada personaje. Con `--ei af_bench_morpho 1`, anota las líneas `AF_PERF frames` de dos niveles con tropa y del 82, y compáralas con las de la jornada 1. Anota `AF_PERF mem` en el inicio y en el Mapa, y el PSS de `adb shell dumpsys meminfo com.amazonfill.dev` frente al de N3 (el tamaño de los atlas no cambia, guía §8).
3. **Avatares del jugador** (D-056; `docs/dev/avatares.md`). Perfil limpio (`pm clear`):
   - **Antes del 10** no hay avatar en el inicio. **Al ganar el 10**, sale el disco arriba a la izquierda con el punto «nuevo» (`AF_LAYOUT lobby` con el id `avatar`). En el primer arranque se asignó uno de los 6 libres al azar: `AF_STATE avatar` con `cause` `assign` y `chosen` false.
   - **Tocar el disco** abre «Tu avatar» (`AF_STATE avatar` con `cause` `open`; `AF_LAYOUT avatar` con `close` y `avatar_<id>`): la cabecera a 112 dp, la rejilla de 4 columnas, los 6 libres primero y luego los 10 de hito en silueta, con su chapa y el texto corto del hito. Captura, y compárala con `f2_02_avatares`.
   - **Tocar uno libre:** se elige en el acto, con rebote y el sonido de UI; `AF_STATE avatar` con `cause` `select` y `chosen` true. Al cerrar (`cause` `close`), el disco del inicio cambia y el punto se va.
   - **Tocar uno bloqueado:** sacudida, un globo con la frase entera del hito («Gana tu primer nivel Muy difícil.»…) y no se elige. Captura del globo.
   - **Desbloqueo por hito:** relanza con `--ei af_max_level 60` (y luego 80). Anota si sale `AF_STATE avatar` con `cause` `unlock`, la rana dardo y el jaguar (y el caimán) en `unlocked`, `new` true, el punto en el inicio y la chapa «Nuevo» en la rejilla. Si no se desbloquean así, anótalo: el diseño dice «≥ el nivel» para los perfiles anteriores.
   - **Ajustes:** la fila «Avatar» arriba del todo (`avatar_row` en `AF_LAYOUT settings`) abre la misma pantalla. Atrás de Android la cierra.
   - **Liga** (`--ez af_league true --ez af_league_fake true`, con `--ei af_max_level 60`): tu avatar en la fila fija de abajo, el de cada fila del top a 32 dp o el disco neutro, y tocar tu fila abre «Tu avatar» (`AF_STATE league_open` y `avatar` con `open`). Gana un nivel desde el 41: la victoria lleva el avatar a 24 dp en la línea «+n puntos de Liga».
   - **A 320 dp y al 130 %** (lo autoriza **el dueño**, como en el bloque 16; o `--ei af_font_scale 130`): la rejilla pasa a 3 columnas y los nombres caben en dos líneas.
   - **TalkBack** (lo enciende **el dueño**): el disco dice «Tu avatar: {animal}. Toca para cambiarlo» (con el punto, «Hay un avatar nuevo»); las celdas, «{animal}, elegido», «{animal}, nuevo», «{animal}. Toca para elegirlo» o «{animal}, bloqueado.» con la frase del hito; la cabecera, «{animal}. En la Liga: …». En las filas de la Liga el avatar **no** se lee.
   - **Reducir movimiento:** sin rebote ni sacudida, y el punto no late.
4. **Informes de fallos** (T-L6):
   - **Primer arranque** (`pm clear`): la pantalla de consentimiento dice «Los informes de fallos ayudan a mejorar el juego. Puedes desactivarlos en Ajustes.». Captura.
   - **Ajustes › «Enviar informes de fallos»**, bajo «Privacidad y datos» (`crash_reports` en `AF_LAYOUT settings`): encendido por defecto. Al apagarlo, `AF_STATE settings_change` con `crash_reports` y `false`; al encenderlo, `true`.
   - **Apagado y relanzado** (`force-stop` y abrir): sigue apagado. Vuelve a encenderlo al final.
   - Con TalkBack, la fila se lee como interruptor con su estado.
5. **Anuncios: AdMob con Unity Ads y sin MAX** (añadidos de D-049):
   - En el arranque, logcat dice `[AF.Ads] proveedor: google (bloques de prueba)` en el `dev.apk` **y también en el `rehearsal.apk`** (lleva `com.amazonfill.dev`). El bloque de producción `1182085615` solo va en el release con el ID definitivo: **no se puede ver en el teléfono.**
   - **Unity Ads no arranca antes de la edad:** con `pm clear` y `adb logcat -c`, guarda `adb logcat` hasta contestar la edad y el consentimiento. No debe salir ninguna línea con `UnityAds` antes de ese momento. Anota la primera que salga y en qué momento.
   - **Un recompensado y un intersticial de prueba** (paso a paso, con captura antes de cada toque): se ven y pagan. Anota la línea `AF_EVENT` de `ad_impression` con su `ad_source` y su `ad_format`. Con los bloques de prueba de Google la mediación de Unity Ads no entra: es lo esperado.
   - **Ad Inspector:** el juego **no tiene** un gancho para abrirlo. Solo si **el dueño** registra el teléfono como dispositivo de prueba en la consola de AdMob, con su gesto; si no, se salta.
   - **Sin AppLovin en logcat:** ninguna línea con `AppLovin` ni `applovin` en todo el arranque.
6. **Play Age Signals** (T-L3):
   - **Primer arranque con 18+:** anota la línea `AF_STATE consent` del paso `done`, con `age_band`, `band_from_signal`, `country` y `country_source`. Con la cuenta de Play de Colombia, lo esperado es **sin señal**: `band_from_signal` false y la banda autodeclarada. Si sale `[AF.AndroidAgeSignals] no se pudo pedir la señal de edad…`, anótalo entero: el juego sigue con la autodeclarada.
   - **Sin espera:** el nivel 1 no tarda más que en N4 por la señal.
   - **13-17 y luego <13** (con `pm clear` entre uno y otro): la banda, `analytics`, `npa`, `tfcd` y `rating` de `AF_STATE consent`, como en el bloque 1.
   - **El bloqueo 0-12 por la señal no se puede provocar en el teléfono:** no hay extra para fingir la señal; lo cubren los tests. Anótalo como no probado.
7. **País y mundo** (D-057):
   - **El país legal:** en `AF_STATE consent`, `country_source` es `sim` con la SIM puesta. Para ver `play`, haría falta un teléfono sin SIM y con la cuenta de Play conectada: solo si **el dueño** lo quiere probar (sin SIM o con otro teléfono). Anota lo que salga.
   - **Nombres de país en la Liga** (`--ez af_league true --ez af_league_fake true`, `--ei af_max_level 60`): en el top global, los países de fuera de la tabla de D-046 (FR, DE, GY, SR) salen con su nombre, no con el código. Repite con `--es af_lang en` y `--es af_lang pt-BR`. Captura de cada idioma.
   - **Decimales:** con `--ez af_fake_store true`, los precios del Mercado en COP salen sin decimales. Anota el texto exacto de dos precios. Otras monedas no se pueden ver sin una cuenta de Play de otro país.
   - **13-17 sin país:** no se puede provocar con la SIM puesta; lo cubren los tests.
8. **Textos de la revisión 2 de Gemini** (añadido de D-054), con `--es af_lang en` y con `--es af_lang pt-BR`:
   - «Tu avatar» (cabecera, celdas, globo de un bloqueado) y la fila «Avatar» de Ajustes;
   - la fila de los informes de fallos y la frase del consentimiento;
   - en pt-BR, la Liga sin puntos: «Vença um nível para entrar na Liga»;
   - ningún texto cortado, ni en otro idioma, ni vacío. Capturas.
9. **La web legal** (opcional, **el dueño** en el navegador del teléfono): en tintalu.com, la política y los términos dicen «Vigente desde: 30 de octubre de 2026» y Parallext, LLC, y Créditos nombra Unity Ads. Solo si el dueño ya desplegó la web nueva.
10. **El ensayo de release (`rehearsal.apk`):** el icono, el emblema, los personajes del inicio y del Mapa, el disco del avatar y «Tu avatar» se ven igual que en desarrollo (D-052: ningún hueco de arte), y el interruptor de informes de fallos está en Ajustes.

## Encargo de la jornada 3 (escritorio, 2026-10-26)

La jornada 2 (`pruebas-telefono-2026-10-25-completa-j2.md`) cubrió el bloque 0, N1 y N2. Sus observaciones están respondidas al final de esa hoja. Esta jornada empieza otra vez por el bloque 0 sobre la integración nueva, comprueba lo que entró y sigue con lo pendiente.

**Punto de partida:** `integracion-fase1-fase2` en `9024fb39` o posterior.

**Lo que entró después de la jornada 2 y nunca se ha visto en el teléfono:**
- **Arreglos de la jornada 2** (`80bec2e8`):
  - el Mercado queda oculto sin precios también en un build sin backend (observación 4: se veía en el ensayo);
  - tras la edad y el consentimiento con un perfil sin estrenar, se va al nivel 1 aunque la app se cerrara en la pantalla de edad.
- **TiTino pulido** (`3f8955a8`):
  - pelaje con mechones, codos y tres garras;
  - la cara con las bandas de los ojos;
  - las poses tumbadas con la cabeza unida al cuerpo;
  - su atlas a 1024 × 512, generado una vez y en paralelo.
- **La pantalla de arranque con la marca v2** (`9a90eb8b`, añadido de D-051):
  - la cuia de vidrio vierte açaí sobre el Amazonas con meandros y lo tiñe río abajo;
  - el nombre «Tintalú»;
  - TiTino a la derecha, en la rama del muelle.

  La intro de la gota se sustituye por esa escena animada (650 ms como mucho) y el arranque es su último fotograma, con un fundido de 300 ms.

### Orden de la jornada 3

1. **Bloque 0 entero**, con el `dev.apk` y el `rehearsal.apk` nuevos (`-Apk`). Anota los tamaños: los de la jornada 2 eran 75,98 MB, 59,01 MB y 64,52 MB.
2. **Bloque N3, «Lo nuevo de la jornada 3»** (abajo).
3. **Los pendientes:**
   - los 3 puntos sin probar del bloque 3;
   - el bloque 4 entero, salvo el 4.9;
   - los bloques 5 y 7 a 23, en orden.

   Entre ellos, lo que quedó sin probar en N1:
   - Reducir movimiento del sistema;
   - la noche del inicio (bloque 15);
   - el 16:9 (bloque 16);
   - los anuncios sin red y cerrados antes de tiempo (bloque 9).
4. **Hoja nueva:** `docs/dev/traspaso/pruebas-telefono-<fecha>-completa-j3.md`, con el mismo formato.
   - Commit y push al final de la jornada con el mensaje «Añade la prueba completa en el teléfono (jornada 3)», solo con la hoja.
   - Si no da para todo, se commitea lo hecho y la jornada 4 sigue donde quedó.
5. **Anuncios de Google:** paso a paso, con una captura antes de cada toque. Los recorridos automáticos, con `--ei af_ads_fill 0/1` (incidente 3 de la jornada 2).

### N3. Lo nuevo de la jornada 3 (≈ 1 h 30 min)

1. **Arranque nuevo, con el `dev.apk`:**
   - **Primera apertura** con perfil limpio (`pm clear`):
     - la intro: la cuia vierte, salen los aros y el açaí avanza por el río con meandros;
     - después el arranque completo: TiTino despierta en su rama a la derecha, «Tintalú» se lee nítido y no hay huecos ni líneas raras;
     - capturas de la intro a mitad (si se puede cazar) y del arranque.
   - **Cinco arranques en frío** (`force-stop` y lanzar). De cada `brand_intro` y `boot` anota `intro_ms`, `load_ms`, `shown_ms`, `cut` y `frames`. Compáralo con la jornada 2: `load_ms` era de 719-756 ms (1.121 en la primera apertura). **Mide cuánto ha crecido.** En el Editor la escena cuesta unos 180 ms antes del primer fotograma. Si en el teléfono la pantalla del sistema se nota claramente más larga, es una observación **alta**.
   - En la primera apertura, `hold_ms` y `sloth_ms`: TiTino se ve al menos 1,0 s desde que empieza el fundido.
   - **Memoria:** `adb shell dumpsys meminfo com.amazonfill.dev`, el PSS total en el inicio tras el arranque. Compáralo con los 600-650 MB de la jornada 1.
   - **Reducir movimiento** (el dueño lo enciende): la intro sale quieta y el arranque entra sin fundido.
2. **TiTino pulido** (capturas de cerca, con `adb exec-out screencap`):
   - **El inicio:**
     - de mañana, el saludo;
     - a mediodía, tomando el sol: tumbado en la rama, con la cabeza unida al cuerpo;
     - de noche, dormido (bloque 15, con la hora cambiada).
   - **La victoria** (`--ei af_level` 40).
   - **Los tutoriales:** su retrato en la banda de los niveles 1-3 y 6.
   - **La ventanita del Mapa.**
   - En cada sitio: sin bordes dentados ni borrosos, y la mancha del lomo discreta.
   - **El primer momento en que sale TiTino** (el arranque o el inicio): ningún tirón visible.
3. **Arreglos de la jornada 2:**
   - **Mercado en el ensayo:** con el `rehearsal.apk`, ni el icono del Mercado ni el Cántaro en el inicio; ninguna pantalla «Purchases aren't available right now» al tocar ese sitio.
   - **Nivel 1 tras cerrar en la edad,** con el `dev.apk` y también con el `rehearsal.apk`:
     1. `pm clear`;
     2. arrancar y, en la pantalla de edad, cerrar la app (`force-stop`);
     3. volver a abrir;
     4. contestar la edad y el consentimiento: **va al nivel 1**, no al inicio.
4. **El ensayo de release (`rehearsal.apk`):** el arranque nuevo y TiTino pulido se ven igual que en desarrollo (D-052). Ningún hueco de arte.
## Encargo de la jornada 2 (escritorio, 2026-10-25)

La jornada 1 (`pruebas-telefono-2026-10-25-completa.md`) cubrió los bloques 0, 1, 2, 6, buena parte del 3 y el punto 4.9. Sus observaciones están respondidas al final de esa hoja, y el guion ya está corregido con ellas. Esta jornada **vuelve a empezar por el bloque 0** sobre la integración nueva y luego sigue con lo pendiente.

**Punto de partida:** `integracion-fase1-fase2` en `7815342d` o posterior.

**Lo que entró después de la jornada 1 y nunca se ha visto en el teléfono:**
- **D-049: anuncios de AdMob directo** con Google Mobile Ads 11.5.0 (`adb977a4`). Ya no son anuncios simulados. Con `com.amazonfill.dev` salen **siempre los bloques de prueba de Google**, con la etiqueta «Test Ad». Detalle en `docs/dev/anuncios-admob.md`.
- **D-051: arranque con la marca** (`a26e7d9f`): sin la pantalla «Made with Unity», la pantalla del sistema con el icono sobre el petróleo `#14444A` y la intro de la gota de açaí (650 ms como mucho, solo mientras carga).
- **D-050: el inicio nuevo, «el muelle del río»** (`689943fb`). Lo describe la fila Lobby de 06 §4.7, y sus ids están en `prototipo-v0.md` (línea `lobby` y `lobby_cards`).
- **D-052: el arte por código sale también en release** (`5c3d5392`): TiTino, la casa y la ventanita, las siluetas de héroe del Mapa, las banderas simplificadas, la tinaja, el cucharón y los miquitos. Antes, en release, no se pintaban.

### Orden de la jornada 2

1. **Bloque 0 entero**, con el `dev.apk` y el `rehearsal.apk` nuevos (`-Apk`). Anota los tamaños: si el AAB del ensayo crece mucho respecto a los 55,94 MB, es por Google Mobile Ads; compáralo con el tope de 80.
2. **Bloque N1, «Lo nuevo»** (abajo), con el `dev.apk`.
3. **Bloque N2, «D-052 en el ensayo de release»** (abajo), con el `rehearsal.apk`. Desinstala antes el de desarrollo si comparten paquete.
4. **Los pendientes de la jornada 1:**
   - los 3 puntos sin probar del bloque 3;
   - el bloque 4 entero, salvo el 4.9;
   - los bloques 5 y 7 a 23, en orden.

   En el bloque 9, los anuncios son ahora **de prueba de Google**, no simulados. Mira los cambios en N1.4. Con `--ei af_ads_fill 0/1` el build de desarrollo sigue usando el simulado, útil para el punto 4 del bloque 9 y para el jugador automático.
5. **Hoja nueva:** `docs/dev/traspaso/pruebas-telefono-<fecha>-completa-j2.md`, con el mismo formato. Commit y push al final de la jornada con el mensaje «Añade la prueba completa en el teléfono (jornada 2)», solo la hoja. Si la jornada no da para todo, se commitea lo hecho y la jornada 3 sigue donde quedó.

### N1. Lo nuevo, con el `dev.apk` (≈ 2 h)

1. **Arranque (D-051):**
   - Arranca en frío 5 veces (`adb shell am force-stop` y lanzar). En ninguna sale «Made with Unity». La pantalla del sistema muestra el icono v2 sobre el petróleo y empalma sin destello blanco ni negro. Después viene la intro de la gota, y luego el inicio o la pantalla de edad.
   - Por cada arranque, la línea `AF_STATE` `brand_intro`: `intro_ms` coincide con `load_ms` de `boot` (la intro nunca alarga la carga), y `anim_ms` es 650. Anota `intro_ms`, `cut` y `frames`.
   - Con Reducir movimiento (el dueño lo enciende en Accesibilidad): la intro sale quieta (`reduce_motion` true).
   - Un toque durante la intro no se pierde ni hace nada raro.
   - Primera apertura con perfil limpio (`pm clear`): TiTino despierta (`sloth_wake`) después de que se quite la intro.
2. **El inicio, «el muelle del río» (D-050).** Mira la línea `lobby` de `AF_LAYOUT` y haz una captura de cada caso:
   - **TiTino colgado de su rama** (`home_window`, `titino`):
     - el bocadillo cambia cada ~5 s entre el saludo, «¡Hoy hay Reto del río nuevo!», lo que falta para la Canasta, la distancia a casa y el dato del día;
     - nunca habla de compras;
     - al tocarlo sale `sloth_look` y el mensaje siguiente;
     - **antes del nivel 10** solo tiene el mensaje que anima a jugar, así que tocarlo lo repite (por diseño: 06 §4.7, fila Lobby, D-050);
     - de noche (bloque 15, con la hora cambiada) duerme.
   - **El río:** el color del agua cambia con el mundo; se ven nenúfares, un pez que salta, un ave y ondas. Prueba con `--ei af_max_level` 4, 58 y 140, y alguno de los mundos 10-13.
   - **La canoa de Capi con Rosa y el poste de la próxima parada** (`voyage_canoe`, `voyage_stop`, `stop_remaining`, con «Faltan n niveles»):
     - **antes del nivel 10** la canoa y el poste con «Faltan n niveles» se dibujan, pero no se pueden tocar (sin `voyage_canoe` en la línea `lobby`); es por diseño (06 §4.7, fila Lobby, D-050);
     - solo desde el nivel 10, tocar la canoa o el letrero abre el Mapa (`af_map_open` con `source` `lobby`);
     - el bocadillo puede apartarlos unos segundos, y está aceptado.
   - **La Crecida como poste de nivel** (`crecida`), desde el 25, con la racha y los hitos.
   - **El muelle:**
     - «Jugar»;
     - el Cántaro como tinaja de barro (`piggy_chip`);
     - las tarjetas del día (`lobby_cards`): `daily_card` con la fecha y 0/7, `logbook_card` y `league_card` si la Liga está activa;
     - **antes del nivel 10**, una caja de cuias en lugar de tarjetas.
   - **Sin canoas, la hoja de la tienda o una oferta encima:** ni la travesía ni TiTino salen.
   - **Letra al 130 %** (`--ef af_font_scale 1.3`) y, en el bloque 16, en 16:9: las tarjetas pueden perder la fecha y la línea de estado (aceptado), pero TalkBack sigue diciéndolas. Nada se solapa sin remedio.
   - Idiomas `en` y `pt` (`af_lang`): ningún texto cortado ni `[clave]`.
3. **Volver al inicio desde un nivel, la victoria y el Mapa** varias veces seguidas: ningún parpadeo, ningún salto de la canoa ni del bocadillo, y 0 `E/Unity`.
4. **Anuncios de prueba de Google (D-049).** Sustituye la parte de vídeos e intersticiales del bloque 9:
   - Al arrancar, logcat dice `[AF.Ads] proveedor: google (bloques de prueba)` (sin «; max: not_configured» desde que MAX salió del proyecto, añadido de D-049 del 2026-10-27). Si dice «produccion», es **bloqueante**: para y avisa.
   - Con perfil 18+ en Colombia no sale el mensaje de UMP (manda nuestra pantalla). Anota si sale.
   - Cosecha doble (victoria del 11) y los demás vídeos con premio: el anuncio de prueba de Google («Test Ad») se ve entero y luego se cierra. Se dan el premio una vez y las líneas `af_ad_request`, `af_ad_shown` y `af_ad_reward`. Si lo cierras antes de tiempo, no hay premio.
   - Intersticial a partir del 22, con la cadencia del bloque 2.3 y en los mismos sitios prohibidos.
   - **Sin red** (modo avión, con el sí del dueño): los botones de vídeo se apagan o desaparecen con su motivo; el juego no se cuelga y al volver la red se recuperan.
   - **Perfil <13** (`pm clear` y edad <13): logcat no muestra ninguna línea del SDK de Google Ads ni de UMP.
   - **Nunca toques un anuncio para abrir el anunciante.** Aunque sean de prueba, la regla de la cuenta es no hacer clics propios.
5. **D-052 en desarrollo:** los niveles con tinaja (`--ei af_level 323`) y con cucharón, el Mapa con sus siluetas y banderas, y la intro de los miquitos se siguen viendo igual que en la jornada 1. Es lo mismo que antes; aquí solo se confirma que no se rompió nada.

### N2. D-052 en el ensayo de release, con el `rehearsal.apk` (≈ 45 min)

El ensayo no es un build de desarrollo: no hay sondas `AF_*` ni extras de `LaunchOptions`. Todo va por capturas y a mano, y **es la prueba de que en release se ve el arte por código**.
1. **Arranque:** sin «Made with Unity», con la pantalla del sistema en petróleo, la intro de la gota y TiTino despertando en la primera apertura. Antes de D-052 no salía en release.
2. **El inicio:** TiTino colgado de su rama con su bocadillo, el río, el muelle y la caja de cuias (antes del 10).
3. **Juega a mano del 1 al 10:**
   - la tropa de miquitos en la intro de los niveles que la tienen, con su tarjeta «¡Miquito a la vista!» la primera vez;
   - TiTino en los tutoriales (1-3 y 6) y Capi en el 8;
   - la tarjeta de nivel con la silueta de la parada;
   - el primer Mapa en el 10, con la casa de TiTino, la ventanita, las siluetas de héroe y las banderas.
4. **Ningún hueco donde debería haber arte:** rectángulos vacíos, colores planos o una parada sin silueta. Cada uno es una observación **alta**, con su captura.
5. **Anuncios en el ensayo:** con el ID provisional también salen bloques de prueba (`GoogleAdUnits`). Si sale un anuncio sin la etiqueta «Test Ad», es **bloqueante**: para y avisa.

## Reglas de la prueba

- **Orden:** sigue los bloques en orden. Entre bloques puede pasar un día; la hoja se escribe a medida y se commitea al final de cada jornada. Así, si se corta, el escritorio ya tiene lo hecho.
- **Hoja de resultados:** `docs/dev/traspaso/pruebas-telefono-<fecha>-completa.md`, con el formato de las anteriores:
  - verificación;
  - resultados por bloque: **cumple**, **no cumple** o **sin probar**, con la captura o la línea de logcat;
  - observaciones numeradas.

  Cada observación lleva gravedad: **bloqueante** (rompe, pierde progreso o dinero, cuelga), **alta** (se ve mal o confunde a cualquiera), **media** o **baja** (detalle).
- **Capturas y logcat:** van en `Logs/device/completa/<bloque>/`, fuera de git. El nombre de cada captura dice qué es: `b05-hoja-43-tutorial.png`.
- **El logcat de toda la prueba**, en otra consola y por jornadas:
  ```
  adb logcat -v time > Logs/device/completa/logcat-<fecha>-<n>.txt
  ```
  Filtra siempre las líneas de `DebugAppCheckProvider`, «debug secret», «debug token», `appcheck:debugtokens` y `eyJ`, y comprueba al final de cada jornada que no queda ninguna (incidente del 2026-10-22).
- **Nada de secretos en la hoja:** ni tokens, ni UID, ni URL firmadas, ni el contenido de `Assets/_LocalSecrets/`.
- **Ajustes del sistema del teléfono:**
  - `wm size` y `wm density`, el tamaño de fuente, TalkBack, el modo avión, la hora, el ahorro de energía y la navegación por gestos o por botones **los cambia el dueño o se cambian con su sí en el momento**;
  - se anota el valor de antes y **se deja como estaba** al terminar cada bloque (`adb shell wm size reset`, `adb shell wm density reset`, hora automática otra vez).
- **Depuración inalámbrica:** si `adb devices` sale vacío, pide al dueño que la reactive y no insistas.
- **Excepciones:** «0 `E/Unity`» es un criterio de **todos** los bloques. Al final de cada bloque:
  ```
  adb logcat -d | grep -E "E Unity|E/Unity|Exception|MissingMethod|TypeLoad|NullReference"
  ```
  Cada excepción es una observación, con su traza. **Excepción conocida, que no cuenta:** `FileNotFoundException … 'GoogleMobileAdsNative.Common'`, que lanza y atrapa el propio plugin de Google Mobile Ads en cada arranque con anuncios (`docs/dev/anuncios-admob.md` §8 ter, 2026-10-28).
- **Sondas:** `AF_LAYOUT`, `AF_STATE`, `AF_EVENT` y `AF_PERF`, descritas en `docs/dev/prototipo-v0.md`. Para tocar, usa la última línea `AF_LAYOUT` de la pantalla que está a la vista.
- **Jugador automático:** el de las pruebas anteriores, con la solución A* y en modo `run`. Para los bloques de pérdida y de uso, **juega a mano** o con vertidos malos a propósito: el jugador automático nunca se bloquea.
- **Reiniciar con vertidos hechos** pide confirmación («¿Volver a la orilla?», con Seguir jugando / Deshacer · Marea / Volver a la orilla): tenlo en cuenta en el jugador automático.
- **`GooglePlayGameSettings.txt` modificado tras un build:** si `git diff ProjectSettings/GooglePlayGameSettings.txt` no muestra cambios de contenido, ejecuta una vez `git add --renormalize ProjectSettings/GooglePlayGameSettings.txt` y `git checkout -- ProjectSettings/GooglePlayGameSettings.txt`: el árbol de trabajo pasa al fin de línea nativo que fija `.gitattributes` (`text !eol`, de `6914b06f`) y deja de salir.
- **Actividad:** `com.amazonfill.dev/com.unity3d.player.UnityPlayerGameActivity`. Los extras (`af_level`, `af_max_level`, `af_lang`, `af_font_scale`, `af_fake_store`, `af_league`, `af_league_fake`, `af_dev_push`, `af_dev_reject`, `af_mech_off`, `af_ads_fill`, `af_seeds`, `af_canoes`, `af_rules_pour_mode`) están en `LaunchOptions.cs` y en `prototipo-v0.md`. Úsalos para llegar rápido a cada caso, pero haz **al menos un recorrido natural** (bloque 2).

## 0. Preparación y builds (≈ 1 h 30 min)

1. **Actualiza y verifica:**
   ```
   git pull origin integracion-fase1-fase2
   dotnet build dotnet/AmazonFill.slnx -warnaserror
   dotnet test dotnet/AmazonFill.slnx
   python tools/validate_schema.py
   ```
2. **Build de desarrollo**, con `Assets/_LocalSecrets/google-services.json` como el 23 (sin abrirlo; comprueba solo que existe con `git status --ignored`):
   ```
   pwsh -NoProfile -File scripts/unity-build.ps1 -Method Dev
   pwsh -NoProfile -File ci/check-manifest.ps1 Builds/Android/dev.apk -Mode dev
   pwsh -NoProfile -File ci/check-no-unity-iap.ps1 Builds/Android/dev.apk
   bash ci/check-16kb.sh Builds/Android/dev.apk
   ```
   - Anota el tamaño del `dev.apk`. El escritorio midió unos 72,6 MB el 23.
   - El build deja `ProjectSettings/GooglePlayGameSettings.txt` cambiado solo en los finales de línea: descártalo. Si no aparece, es que el arreglo de `6914b06f` funciona; anótalo.
3. **Build de ensayo de release** (sin firmar con la clave de subida, que no existe todavía):
   ```
   pwsh -NoProfile -File scripts/unity-build.ps1 -Method Release -Rehearsal -Apk
   ```
   Sobre el AAB y el APK que deja, en `Builds/Android/`:
   ```
   pwsh -NoProfile -File ci/size-gate.ps1 <aab>
   bash ci/check-16kb.sh <aab>
   pwsh -NoProfile -File ci/check-manifest.ps1 <aab> -Mode dev
   pwsh -NoProfile -File ci/check-release-classes.ps1 <aab>
   pwsh -NoProfile -File ci/check-no-unity-iap.ps1 <aab>
   ```
   Anota los tamaños: el AAB pesó 55,94 MB en el escritorio, con un tope de 80. El ensayo se instala en el bloque 20.
4. **Instala el de desarrollo con un perfil limpio:**
   ```
   adb install -r Builds/Android/dev.apk
   adb shell pm clear com.amazonfill.dev
   adb logcat -c
   ```
5. **Estado del teléfono:** anota la versión de Android, el tamaño de pantalla (`adb shell wm size`), la densidad, la escala de fuente, el tipo de navegación (gestos o botones), la región, el idioma y el nivel de batería.

## 1. Primer arranque y primera sesión (niveles 1-12) (≈ 1 h)

Con el perfil limpio y **a mano**, como un jugador nuevo. El dueño mira la pantalla. Guía: 06 §2.2, la primera sesión minuto a minuto.

1. **Arranque en frío:** mide el tiempo con `adb shell am start -W -n com.amazonfill.dev/com.unity3d.player.UnityPlayerGameActivity` (anota `TotalTime`).
   - Pantalla de carga, sin pantalla negra larga ni parpadeos.
   - En logcat, `[AF.Ads] proveedor: not_configured` y ninguna línea de AppLovin ni de Play Games antes de la edad.
2. **Edad:** «¿Cuántos años tienes?» con tres botones iguales. Atrás no la salta. Elige 18+.
3. **Consentimiento:** «Tu privacidad», con las dos casillas sin marcar y «Rechazar todo» y «Aceptar todo» al mismo nivel. Elige «Aceptar todo».
4. **Niveles 1-10:**
   - **1:** el dedo guía sobre el primer vertido; el vertido por tramo (D-005); la elevación del origen, sin resaltar destinos (D-002); el sellado al llenar una cuia de un color; la victoria.
   - **2:** el dedo sale tras el primer vertido, cuando la lección ya es posible (DC6). La banda de TiTino.
   - **3:** el tooltip de Marea tras un vertido que no acerca. Marea ilimitada hasta el 10.
   - **6:** el tooltip «Reiniciar es gratis…» encima de Aguas estancadas, con el pico sobre «Volver a la orilla (gratis)».
   - **7:** la tarjeta del Reto del río ~1 s después de la victoria, bajo TiTino, sin que la tape.
   - **8:** la Cuia extra regalada, con el dedo sobre «+1 cuia». El dedo espera a que se vaya el aviso de Capi (no sale con otro aviso a la vista): no la uses antes. Úsala y comprueba que el tablero se recoloca con N + 1 cuias.
   - **10:** la secuencia de la victoria (06 §2.3, cuatro pantallas como mucho, cada una se cierra con un toque): Canasta (+50), Bitácora con tres premios (elige uno y se sella), «Seguir» abre el Mapa del río (`source: l10`) y vuelta al inicio con «Bitácora · Sellada hoy».
5. **Niveles 11 y 12:**
   - **11:** banner del mundo 2, Tapajós (`world_banner`, con su arte, y `wait_ms` ≈ 0 si se pidió en la victoria del 10). Cosecha doble con el tooltip, sin tapar «Cosecha +20»; vídeo simulado y +20.
   - **12:** Morpho, con 3 regalados y su tarjeta. Usa una pista: señala un vertido que lleva a la solución.
6. **Segunda sesión:** `am force-stop` y vuelve a abrir.
   - Vuelves al inicio con el progreso intacto.
   - En la primera victoria, con el 10 superado, sale el pre-aviso de notificaciones al pulsar «Siguiente». Esta vez di «Sí»: sale el diálogo nativo de Android 13+ y lo aceptas (`af_push_prompt`).
   - En la primera victoria con nivel ≥ 12 en la sesión 2 sale la Mochila de inicio (05 §5) o no sale porque la tienda no está disponible. Anota cuál de las dos y si tiene sentido.
7. **Lo que mira el dueño:** que se entiende sin leer, que el ritmo no aburre ni agobia y que nada parece roto o provisional. Sus frases van tal cual como observaciones.

## 2. Recorrido natural de la campaña (1-60 a mano y con el jugador automático) (≈ 3-4 h)

Sigue con el perfil del bloque 1, sin `af_max_level`: es la prueba de que el progreso se encadena bien.

1. **Del 13 al 60**, alternando: un tramo a mano (unos 10 niveles por hora, con fallos reales) y otro con el jugador automático.
2. **En cada nivel**, de paso:
   - la línea `AF_STATE load` y `win`;
   - la tarjeta previa cuando toca;
   - el fondo de la parada;
   - el rótulo del lugar;
   - TiTino en la victoria si cabe;
   - las semillas: `earn_virtual_currency` con la cantidad de 05.
3. **Hitos del camino**, con una captura de cada uno:
   - **14:** Cuia chica de 2, con su tutorial de una pantalla (estreno de 02 §8). **18:** de 1. **22:** de 3. Estas dos no son estrenos (la lista única de 02 §9.1 solo tiene el 14 y el 27), así que salen sin tutorial.
   - **20:** primera Canasta con comodín y el Cántaro visible.
   - **22 en adelante:** intersticial simulado tras la victoria: 1 cada 3 niveles, ≥ 150 s desde el anterior, nunca en un reintento ni tras rendirse. Anota cada intersticial con su nivel y su hora.
   - **25:** tarjeta de la Crecida y la barra. Gana 3, 6 y 10 seguidos y anota los hitos (`streak_milestone` 3 = +300, 6 = cofre, 10 = Cuia extra y río crecido).
   - **27:** Cuia alta de 5, con su tutorial.
   - **30:** tarjeta de la Pororoca.
   - **31:** tarjeta de las Canoas. Desde aquí, **cada fallo cuesta una canoa** (bloque 7).
   - **40:** primer Difícil, con el chip y la cinta «×3» en Jugar y en Siguiente, la tarjeta previa de la rana dardo, el premio de 60 y la **skin Cuia de Santarém** al ganarlo. Comprueba que se ve en el tablero.
   - **41:** banner de Manaos, los Patrones de fruta activados (`patterns.on` true, opacidad 0,20) y el botón «Liga» en el inicio si la Liga está encendida por Remote Config (si no, en el bloque 12).
   - **43:** estreno de la Hoja (bloque 5).
   - **60:** primer Muy difícil: el jaguar, «Noche de jaguar», el premio de 80 con la cinta «×4» y la variante nocturna de la música.
4. **El Mapa en cada parada** (cada 10 niveles): la llegada, el dato del río, la fruta nueva, la canoa que avanza y «Premio del mundo · faltan N».

## 3. Núcleo del juego: tablero, vertido y gestos (≈ 1 h)

Con `af_level` para ir directo.

1. **Vertido por tramo (D-005):**
   - una racha de 3 en un hueco de 2 vierte 2 y deja 1;
   - verter en una cuia vacía;
   - verter sobre el mismo color;
   - un vertido ilegal sobre otro color o una cuia llena, que cambia la selección (C5);
   - con `--ei af_dev_reject 1`: rechazo con sacudida y `sfx_illegal`, sin háptico.
2. **Selección:** tocar el origen lo eleva y tocarlo otra vez lo baja. Tocar una cuia vacía sin nada elegido es un rechazo (C2). No se resalta ningún destino (D-002).
3. **Gestos:**
   - arrastrar de una cuia a otra vierte;
   - soltar en el fondo cancela;
   - mantener pulsada una cuia muestra el bocadillo con sus frutas, sin seleccionarla;
   - dos dedos a la vez sobre dos cuias: no se cuelga ni hace dos vertidos raros;
   - toques muy rápidos, 10 por segundo durante 10 s sobre cuias distintas: sin excepciones; las ráfagas de `step_budget` valen, no son un fallo.
4. **Modo `single`:** con `--es af_rules_pour_mode single`, el 5: cada vertido mueve una dosis.
5. **Disposición del tablero** (D-004, D-006 y la franja de la tropa). Una captura por caso, con el ancho de cuia de `AF_LAYOUT`:

   | Caso | Niveles |
   |---|---|
   | T ≤ 3 cuias | 1 |
   | 5-7 cuias | 2, 7 |
   | 8-10 cuias, 2 filas | 11, 22, 63 |
   | 11 cuias, 3 filas | 86 |
   | 12 cuias | 123 |
   | 13 cuias | 240 |
   | 14 cuias | 335, 343, 600, 700 |
   | Cuia alta de 6 | 160 |

   - En los de 3 filas, la **tropa** entra por su franja de 56 dp, sin pisar la fila del HUD ni el panel, y el panel queda a ~11,5 dp de la bandeja.
   - **15 y 16 cuias:** en un nivel de 14 cuias (el 335), añade la Cuia extra y después rescata (bloque 7) para llegar a 16. Deben quedar 6/5/5, sin cuias fuera de la pantalla y todas tocables.
6. **Patrones de fruta:** Ajustes › Patrones en Automático (el 41 activo, el 30 inactivo), Siempre y Nunca. Se aplica al momento, también al nivel abierto.
7. **Pista por inactividad** (Ajustes, `settings_idle_hint`): sin tocar durante el tiempo configurado, ¿sale la pista? Con el ajuste apagado, no.

## 4. Comodines (≈ 2 h)

Para cada uno: con saldo, sin saldo, con vídeo simulado y, donde aplique, en el bloqueo. Usa `--ei af_seeds <n>` para fijar el saldo. Guía: 04 §2 y §3.

1. **Marea (deshacer):**
   - ilimitada en 1-10;
   - 3 gratis por nivel desde el 11, con el contador visible;
   - agotada: recarga por vídeo simulado (`rv_undo_refill`) y por 30 semillas;
   - con `--ei af_ads_fill 0` (sin relleno): solo por 30 semillas, sin el botón de vídeo (rama E);
   - funciona dentro de Aguas estancadas;
   - el tope de vídeos del día (4) y la espera de 60 s.
2. **Cuia extra:**
   - con stock;
   - sin stock, por 150 semillas;
   - una por nivel;
   - **se conserva al reiniciar** el nivel;
   - no se puede usar en el bloqueo, donde pasa a ser la Cuia de rescate.
3. **Morpho (pista):**
   - con stock, por 100 semillas y por vídeo (`rv_hint`, 3 al día);
   - no se puede usar en el bloqueo;
   - en Aguas turbias (100, 106) solo propone vertidos seguros o no cobra;
   - en un nivel con `hint_supported` falso: el globo de Pista sale en su sitio (en el 135, en la franja si pisaría el panel).
4. **Pororoca (barajar):**
   - en el 30: confirmación por 300 semillas, `shuffle` `shuffled` y el botón en gris en ese nivel;
   - una por nivel y tres al día;
   - **en un nivel con Lluvia (143) o con Piraña (183): deshabilitada**, con su motivo;
   - en un nivel con mecánicas (43, 63): baraja solo lo libre. Si falla, sale «La ola no pudo remover el río», no cobra y se puede repetir;
   - salir y volver al nivel después de barajar: el tablero barajado se conserva o no, según 04 §2.4. Anota cuál pasa.
5. **Luciérnaga (revelar):**
   - en el 100 regala 1 y su tarjeta combinada con Aguas turbias;
   - revela una cuia turbia y **lo revelado se conserva** al reiniciar;
   - por 120 semillas y por vídeo (`rv_reveal`).
6. **Salto del Boto:** sale 5 veces del 32 (`fails_level: 5`). Comprueba el banner de Rosa y «Salto del Boto · 300» en la pausa y en Aguas estancadas (rama D). Al confirmar, cobra 300, carga el 33, registra `af_skip_used` y no da premio. **No vuelve a ofrecerse hasta 10 niveles después.**
7. **Volver a la orilla (reiniciar):** gratis siempre, con el mismo tablero. En 1-30, sin canoa. Desde el 31, desde la pausa, **¿cuesta canoa?** Comprueba lo que dice 04 §2.7 y anótalo.
8. **Cosecha doble:**
   - el botón ×2 en la victoria desde el 11, que duplica las semillas del nivel;
   - tope de 5 al día;
   - con la Crecida en 10, `free_double_harvest` sin botón;
   - en el Reto del río: comprueba si existe y qué hace.
9. **Música y Sonido con un toque (nuevo):**
   - en la pausa, los discos junto a la X;
   - en el inicio, en columna bajo el engranaje;
   - apagar uno en la pausa lo apaga en Ajustes y en el inicio, y viceversa;
   - tachados cuando están apagados;
   - `AF_STATE settings_change` con `source` `pause`, `home` o `settings`;
   - la Vibración solo está en Ajustes.

## 5. Mecánicas del río: cada estreno y una combinación (≈ 2 h 30 min)

En cada estreno:
- el **tutorial de una pantalla**, que hace de tarjeta previa;
- que se entiende sin leer el GDD;
- que la mecánica hace lo que dice 02 §9.1;
- las líneas `AF_STATE step` de sus pasos, sin `snapped` raros;
- ganar el nivel a mano **y** con el jugador automático.

Luego, un nivel posterior de esa mecánica mezclado con otra.

| Mecánica | Estreno | Después |
|---|---|---|
| Hoja | 43 | 52 |
| Matapi | 63 | 72 |
| Liana con fruta | 83 | 92 |
| Aguas turbias + Luciérnaga | 100 | 115, 135 |
| Remolino | 103 | 112 |
| Barro | 123 | un reto de 121-140 |
| Lluvia (contador) | 143 | 152 |
| Brote | 163 | 172 |
| Piraña, con el mordisco animado (`bite_demo`) | 183 | 192 |
| Liana de cuenta | 243 | 318 |
| Hoja atada | 283 | 354 |
| Brote de dos hojas | 343 | 420 |
| Flor de vitória-régia | 403 | 498 |

**Además:**
- **Contador de Lluvia o Piraña junto a Reiniciar en dos líneas** (nuevo): en el 194 y el 200 con `--es af_lang en-XA` al 100 y al 130 %, el contador va a la izquierda de la píldora, sin tocarla. En es-419, a la derecha.
- **Interruptores de emergencia:** `--es af_mech_off hoja` en el 43, que se juega el nivel publicado con su aviso.
- **Tinaja (323):** no va en la campaña v1 (variante B de D-019). Anota qué sale en el 323.

## 6. Niveles: muestra amplia con el jugador automático (≈ 3-4 h, sin vigilancia)

Con el jugador automático, perfil con `af_max_level` alto y `af_level` en cada uno. Cuenta los ganados y las excepciones.

1. **Todos los del 1 al 200,** uno tras otro.
2. **Del 201 al 700, uno de cada cinco:** 205, 210… 700.
3. **Todos los del 691 al 700, y el viaje final del 700:**
   - `map_journey` con `by: time`;
   - la pantalla de fin de contenido (`content_end`), con un texto que tiene sentido;
   - qué pasa al tocar «Siguiente» después.
4. **Cada 50 niveles:** `adb shell dumpsys meminfo com.amazonfill.dev` (anota el `TOTAL PSS`) y la temperatura (`adb shell dumpsys battery`). La memoria no debe crecer sin parar.
5. **Resultado:** N/N ganados, los que fallaron con su `id` y la línea `AF_STATE` final. Cualquier nivel que el jugador automático no gane es una observación alta.

## 7. Perder, rendirse y reintentar muchas veces (≈ 2 h 30 min)

**A mano**, con vertidos malos a propósito. Guía: 04 §6 (ramas) y §7 (canoas), y D-008. Una captura de cada modal.

1. **Rama C (antes del 31):** bloquéate en el 25 y en el 30. Sale Aguas estancadas con Deshacer y «Volver a la orilla (gratis)», sin canoa y sin «El río bajó».
2. **Rama A (desde el 31, con saldo):** bloquéate en el 35 con 1.000 semillas.
   - Sale «Continuar: +1 cuia» por **300** (el primero del día) y Deshacer, con «Rendirse» al pie y «Travesía perdida: −1 canoa».
   - Rescata: la cuia de rescate entra y se puede ganar.
   - Bloquéate otra vez en otro nivel: ahora cuesta **600**.
3. **Rama S (¿Seguro?):** «Rendirse», y sale ¿Seguro? con «Perderás 1 canoa» y el rescate otra vez. X o «Rendirse» → Nivel fallido, con −1 canoa.
4. **Rama A′ (segundo bloqueo del mismo nivel tras rescatar):** solo Deshacer y el pie. No hay un segundo rescate.
5. **Rama B (sin saldo):** con `--ei af_seeds 0`:
   - «Continuar» en gris con «Te faltan N»;
   - la oferta del Puñado o la Balsa: sin tienda, ¿qué sale?;
   - con `--ez af_fake_store true`, la compra simulada y el rescate.
   - En niveles de 3 filas (el 86), la burbuja «Te faltan N» va a la franja si pisaría el panel.
6. **Rama D (5 fallos):** ya vista en el bloque 4.6.
7. **Rama F (irresoluble con +1):** si encuentras un bloqueo en el que el rescate no vale, no se ofrece y sale `af_rescue_unsolvable`.
8. **Rama G (1 canoa):** «Travesía perdida: tu última canoa». Tras rendirte, «Reintentar» lleva a Sin canoas.
9. **Sin canoas:**
   - el cronómetro;
   - «Canoa extra» por vídeo simulado (3 al día, cada 90 s);
   - «Llenar canoas» y «Canoas infinitas 1 h» (sin tienda: ¿qué sale?; con la tienda falsa: compra y canoas infinitas, rama H, en la que rendirse va directo a Nivel fallido);
   - «Mientras esperas: Jugar el Reto del río», que no gasta canoas;
   - «Volver al Mapa del río»;
   - Atrás vibra (`haptic_back_blocked`) y no cierra.
10. **Regeneración de canoas:** con 3 de 5 canoas, espera 25 min (o cambia la hora en el bloque 15) y comprueba que sube una. La ventanita del inicio muestra el tiempo.
11. **Reintentar muchas veces el mismo nivel:** en un Difícil (el 46 o el 66), **falla y reintenta 10 veces seguidas**.
    - Desde el fallo que diga 03 §13, «El río bajó» trae una variante aliviada (`relief`), más fácil y con el mismo aspecto.
    - Tras 5 fallos, Rosa y el Salto del Boto.
    - Las canoas bajan una por fallo.
    - Nada se acumula mal: los contadores, la memoria y el tiempo de carga siguen igual al décimo intento que al primero.
12. **Salir del nivel:** desde la pausa, «Salir» en el 35. Sale ¿Salir? y, al confirmar, desde el 31 cuenta como fallo y cuesta una canoa. Antes del 31 no cuesta nada.
13. **La Crecida al perder:**
    - con racha y Hoja escudo, el primer fallo lo cubre el escudo (toast) y la racha sigue;
    - el segundo fallo la rompe;
    - un rescate comprado protege la racha (04 §8).

## 8. Economía, Mercado y Cántaro (≈ 1 h 30 min)

1. **Saldo:**
   - cada ganancia y cada gasto de semillas sale en el HUD y en `earn_virtual_currency` o `spend_virtual_currency`, con su cantidad;
   - premios de victoria: 20 en Normal, 60 en Difícil y 80 en Muy difícil (`eco_reward_*` de 08 §9), dobles con el río crecido;
   - el saldo nunca baja de 0 ni cambia al reabrir la app.
2. **Mercado sin tienda (nuevo, `6914b06f`):** con el build normal, con Firebase y Play Billing reales, la app no está en Play Console.
   - El icono del Mercado y el Cántaro **ocultos** en el inicio.
   - Si alguna pantalla lleva a una compra (rescate sin saldo, Sin canoas, Mochila de inicio), sale el aviso de «tienda no disponible», no un toque mudo como el 23.
3. **Mercado con la tienda falsa**, con `--ez af_fake_store true`:
   - las pestañas Semillas, Comodines, Cuias (skins) y Sin anuncios, con los precios de la tienda falsa;
   - comprar el Puñado: `iap_verification` `verifying` → `credited` y +1.000 una sola vez;
   - comprar un comodín y una skin, y ponerse la skin;
   - **Sin anuncios:** después no salen intersticiales, pero los vídeos con premio siguen;
   - reabrir la app: `ledger_restore` sin abonos repetidos;
   - al 130 % en es, en y pt-BR: las pestañas en dos líneas, las filas que crecen y el desplazamiento.
4. **Cántaro** (con la tienda falsa):
   - visible desde el 20;
   - se llena con las victorias;
   - con ≥ 1.000, tocar el icono abre la oferta;
   - **lleno (3.000):** sale una tarjeta, una sola vez. Para llegar rápido, gana muchos niveles con el jugador automático o pide al escritorio un extra si no hay forma.
5. **Banda 13-17:**
   - cada compra pide «¿Confirmas la compra?» con el precio;
   - en Privacidad no está el interruptor de anuncios.
6. **Tarjetas de oferta:**
   - Sin anuncios tras el tercer intersticial;
   - la skin del mundo tras la primera victoria del mundo 3.

   Como mucho **1 oferta por sesión** (`ui_max_takeovers_per_session`). Ninguna oferta antes del 10 ni en la primera sesión.

## 9. Anuncios simulados (≈ 45 min)

1. **Vídeos con premio**, en sus cinco sitios: Cosecha doble, recarga de Marea, Morpho, Luciérnaga y Canoa extra.
   - Cada uno: `af_ad_request` `filled`, `af_ad_shown` y `af_ad_reward`, y el premio una sola vez.
   - Cerrar el vídeo antes de tiempo: sin premio (si el simulado lo permite).
   - Con el teléfono en silencio: el juego se calla durante el anuncio y vuelve después.
   - Zona segura del anuncio (nuevo): el rótulo y «Cerrar» no quedan bajo la barra de gestos ni bajo la cámara.
2. **Intersticiales:**
   - la cadencia del bloque 2.3;
   - el tope de 6 por sesión;
   - nunca en el Reto del río, en un reintento, tras rendirse ni en Sin canoas.
3. **Topes:** el global de 15 vídeos al día. Al llegar, los botones de vídeo desaparecen o se apagan con un motivo.
4. **Sin relleno:** con `--ei af_ads_fill 0`, sin botones de vídeo ni pantallas vacías.

## 10. Bitácora, Canastas, Crecida y Dato del río (≈ 1 h)

1. **Bitácora:**
   - la página de hoy se sella una vez al día con un premio a elegir entre tres;
   - las casillas especiales: Cuia extra en el día 6, Morpho en el 4 y Luciérnaga en el 29 (05 §2.3);
   - la página del día siguiente, cambiando la fecha (bloque 15);
   - al 130 % y en los tres idiomas.
2. **Canastas:** en el 10 (50), el 30 y el 60 (Morpho), el 40 y el 90 (Cuia extra) y el 100 (la grande). Animación, premio y saldo.
3. **Crecida:**
   - la barra y sus hitos;
   - el escudo;
   - el río crecido tras el 10 (¿se ve en el fondo?);
   - la racha rota al perder (bloque 7.13).
4. **Dato del río:**
   - una sola línea por victoria;
   - Ajustes › «Datos del río» muestra el dato con su fuente;
   - Ajustes › «Sobre la Amazonía» (`settings_about_amazon`).

## 11. Mapa del río, frutas, banners y skins (≈ 1 h 30 min)

1. **Mapa:**
   - las vistas Mundo y Cuenca, con «Norte arriba»;
   - la canoa de Capi con Rosa delante;
   - la ventanita de casa de TiTino;
   - la cabecera con la pregunta del mundo, en los tres idiomas y sin textos vacíos (observación 1 del 23);
   - «Jugar · Nivel N» siempre a la vista;
   - el Muestrario de frutas;
   - las placas de la Cuenca **al 130 % en inglés**, que se montaban (observación 2 del 23);
   - Atrás cierra el Mapa.
2. **Con `af_max_level`** en 25, 58, 140, 200, 400 y 700: el Mapa se pone al día, con el viaje animado (y sin él con Reducir movimiento: `by: reduce`).
3. **Banners de mundo** en el 11, el 41, el 81 y el 141, y en el 201, el 301, el 421 y el 561 si existen: su arte, el texto y que salen solo en el primer intento.
4. **Skins de los hitos** al ganar el 40, el 80 y el 140: la tarjeta, la skin puesta en el tablero y en el Mercado.
5. **Frutas del río:** la tarjeta «Fruta nueva» la primera vez que sale cada fruta (`af_fruit_discovered`) y su línea en la victoria.
6. **Fondos:**
   - en 1-200, el de la parada;
   - en 201-700, el kit del mundo con el rótulo;
   - ninguno rosa, negro o vacío.

   Una captura de un nivel de cada mundo, del 1 al 9.

## 12. Reto del río y Liga del río (≈ 2 h)

1. **Reto del río:**
   - se desbloquea en el 10;
   - la tarjeta del inicio: Jugar, Pendiente de validar y Seguir;
   - el tablero del día;
   - el rótulo «Nivel N · intento k»;
   - ganarlo, que sella el calendario;
   - perderlo y reintentar, sin gastar canoas;
   - Atrás dentro del Reto;
   - irse a segundo plano en mitad y volver;
   - **el calendario de sellos** y la zona segura del calendario (nuevo);
   - la racha de 7 Retos seguidos regala una Pororoca (cambiando la fecha, en el bloque 15);
   - **a 320 dp** (bloque 16): la tarjeta de 272 dp que no pisa la Crecida.
2. **Liga del río:** sigue **entero** `guion-prueba-telefono-liga.md` (primer contacto, una victoria, sin conexión, con 13-17 y con <13), contra el backend de dev. Además:
   - con `--ez af_league true --ez af_league_fake true`: la lista de 100, el país, el percentil y que el botón «Liga» casa con la columna de audio del inicio sin solaparse;
   - la Liga al 130 % y en los tres idiomas;
   - las banderas;
   - TalkBack sobre la lista (bloque 17);
   - **tu fila, fija abajo** (observaciones 4 y 5 de la jornada 4): tocarla en cualquier sitio (los puntos, el nombre, la línea de abajo) abre «Tu avatar», y tocar la fila de otro no abre nada; con TalkBack se lee entera con «Tu avatar» al final; sin seudónimo («Gana un nivel para entrar en la Liga»), «Tú» sale **una sola vez**, y con seudónimo, el seudónimo y la chapa «Tú» (también en en y pt-BR).

## 13. Ajustes, privacidad y cuenta (≈ 1 h)

Recorre **cada fila** de Ajustes y comprueba que hace lo que dice, al momento y tras reabrir la app:
- Sonido, Música y Vibración;
- Idioma: Automático, Español, English, Português y en-XA;
- Patrones;
- Pista por inactividad;
- Mensajes de TiTino (`settings_sloth_messages`);
- Reducir movimiento;
- Alto contraste;
- Avisos (encendido, apagado y bloqueado desde el teléfono, con «Abrir ajustes del teléfono»);
- Privacidad y datos;
- Corregir mi edad;
- Datos del río y Sobre la Amazonía;
- Código de cuenta (en grupos con guiones);
- Eliminar cuenta y datos.

**Además:**
0. **Toque con la lista en movimiento** (observación 3 de la jornada 4): lanza la lista de Ajustes con un deslizamiento rápido y, mientras sigue moviéndose, toca una fila (por ejemplo, «Corregir mi edad»). La lista se para y la fila **no** se activa; el toque siguiente, con la lista quieta, sí. Lo mismo en la Liga, el Mercado, «Tu avatar», la hoja de la fruta y las muestras del Mapa.
1. **Privacidad:** apagar la analítica hace que las líneas pasen a `AF_EVENT_UNSENT`. Encenderla las devuelve a `AF_EVENT`.
2. **Corregir mi edad:** a 13-17, una sola vez; después desaparece. Antes, la **confirmación neutral** (añadido de D-033 del 2026-10-28):
   - elegir otra banda enseña «Corregir mi edad», «Elegiste: {banda}. La edad solo se puede corregir una vez y no se puede deshacer.», «Volver a elegir» y «Confirmar»; **la misma pantalla** al tocar «Menos de 13», «De 13 a 17» o «18 o más» (solo cambia la banda): captura de las tres;
   - «Volver a elegir» y Atrás vuelven a la pantalla de edad y la fila «Corregir mi edad» sigue en Ajustes; la X cierra sin cambiar nada;
   - elegir la banda guardada cierra sin confirmación y sin gastar la corrección;
   - con TalkBack (bloque 17) se leen el título, la frase y los dos botones;
   - en el primer arranque, la pantalla de edad no pide confirmación.
3. **Idiomas:** cambia a cada uno y recorre el inicio, un nivel, la victoria, el Mapa, la Bitácora, el Reto, la Liga, el Mercado (con la tienda falsa) y Ajustes. **Ningún texto vacío, cortado o en otro idioma.** Con en-XA, todo entre corchetes y nada recortado.
4. **Eliminar cuenta: se hace al final de la prueba, en el bloque 21**, no aquí. Aquí solo se abre, se ve la espera visible y la nota de Play Juegos, y se cancela.

## 14. Notificaciones (≈ 45 min, más esperas)

1. Con `--ei af_dev_push 2`: el primer aviso llega a los 60 s de pasar a segundo plano, con su texto y su icono.
   - Tocarlo abre el juego en el sitio correcto.
   - En los tres idiomas.
2. **Avisos reales sin el extra:** deja la app cerrada con el Reto sin jugar y comprueba al día siguiente si llega «tu Reto del río está listo» (06 §8.3), sin pasar del tope del día.
3. **Con <13:** ningún aviso ni diálogo de permiso.

## 15. Tiempo y fechas (≈ 1 h; el dueño cambia la hora)

Con la hora automática apagada **por el dueño**, y encendida otra vez al terminar:
1. **Día siguiente** (+1 día):
   - Bitácora nueva;
   - Reto nuevo;
   - la racha del Reto sigue;
   - topes de vídeos y de rescate a 300 reiniciados;
   - la Crecida sigue (no caduca de noche).
2. **+2 días sin jugar:** el Regalo de regreso (06 §9).
3. **+7 días seguidos jugando el Reto:** la Pororoca de regalo.
4. **Hora hacia atrás (−1 día):** el anti-reloj de las canoas y de la Bitácora no regala nada ni rompe el perfil (`GuardedClock`).
5. **Hora del día:** TiTino dormido de noche (20-6), con sol entre las 10 y las 16 y el saludo de la mañana entre las 6 y las 12.
6. **Semana de la Liga:** al pasar del lunes a las 12:00 UTC, la semana cambia.

## 16. Pantallas y tamaños (≈ 1 h 30 min; el dueño autoriza `wm`)

Con `adb shell wm size` y `wm density`, anotando los valores de antes y **reseteándolos al final**:
1. **16:9:** `wm size 1080x1920`.
   - El inicio, los niveles de 3 filas (el 240 y el 335, sin franja y con la tropa lateral), el 640 y su panel;
   - Aguas estancadas (el 31 y el 640, al 130 %);
   - la hoja de compra, el calendario y el anuncio.
2. **320 dp de ancho:** `wm density` hasta que `AF_LAYOUT` dé ~320 dp.
   - El inicio con la tarjeta del Reto de 272 dp y la Crecida al lado;
   - la píldora en dos líneas al 130 %;
   - la columna de audio y la Liga.
3. **Pantalla alta** (360 × 800): el inicio y un nivel de 3 filas.
4. **Navegación con 3 botones** en lugar de gestos: ningún botón bajo la barra.
5. **Rotación:** girar el teléfono no gira el juego ni lo rompe.
6. **Modo ventana o pantalla dividida**, si One UI lo deja: anota qué pasa.

## 17. Accesibilidad (≈ 1 h 30 min; el dueño enciende TalkBack)

1. **TalkBack:**
   - el inicio, un nivel (cada cuia anuncia sus frutas), la victoria, Ajustes, la Liga y el Mercado;
   - los discos de audio: «Música: activada»…;
   - las listas (Q-B9-17);
   - ¿se puede jugar un nivel fácil solo con TalkBack?
2. **Fuente del sistema al 130 % y al máximo** (Ajustes de Android, no el extra): el juego la toma o no; anótalo. Con `af_font_scale 130`, las pantallas del bloque 13.3 sin cortes.
3. **Reducir movimiento:**
   - la victoria sin confeti ni hojas;
   - el viaje del Mapa saltado;
   - el banner más rápido;
   - el mordisco de la Piraña quieto.
4. **Alto contraste:** el patrón a 0,35 y que se distinguen los pares de riesgo.
5. **Daltonismo:** con el simulador de Android (Opciones de desarrollador › Simular espacio de color), en deuteranopía y protanopía, ¿se distinguen las frutas de un nivel del 41 en adelante, con los patrones? Captura.

## 18. Robustez: interrupciones, segundo plano y red (≈ 2 h)

1. **Segundo plano** (botón de inicio y volver):
   - en mitad de un vertido;
   - durante el sellado;
   - en la victoria;
   - con un anuncio simulado;
   - con la hoja de compra falsa abierta;
   - en Aguas estancadas;
   - en el viaje del Mapa;
   - en la carga.

   Al volver, todo sigue donde estaba, sin premios duplicados ni perdidos.
2. **Matar la app** (`am force-stop`) en esos mismos momentos y reabrir:
   - el nivel en curso se recupera (o se reinicia según el GDD);
   - el saldo es el mismo;
   - `profile_integrity: valid`;
   - en Nivel fallido o Sin canoas, la app vuelve a Nivel fallido o al nivel.
3. **Interrupciones del sistema:**
   - una llamada (el dueño se llama desde otro teléfono);
   - una alarma;
   - una notificación de otra app;
   - bajar la cortina;
   - apagar la pantalla y encenderla.

   El audio se pausa y vuelve.
4. **Modo avión** (el dueño):
   - arranque sin red (`"net":"offline"` en los comunes) y jugar 5 niveles;
   - el Reto sin red;
   - la Liga sin red (bloque 12);
   - el Mercado con la tienda falsa sin red;
   - volver la red: `restoreLedger`, la cola de la Liga y Remote Config.
5. **Red mala:** con el Wi-Fi a medias, si se puede, que ninguna pantalla se quede esperando sin salida.
6. **Ahorro de energía y batería baja** (el dueño activa el modo ahorro): el juego sigue fluido y sin cortes de audio.
7. **Atrás de Android en cada pantalla:**
   - inicio (¿sale de la app?, ¿pide confirmación?);
   - nivel (abre la pausa);
   - cada modal;
   - Mapa, Reto, Liga, Ajustes y Mercado;
   - pantallas de edad y consentimiento (no se saltan).

   Doble Atrás rápido: sin excepciones.
8. **Actualizar encima:** instala el `dev.apk` del 23 (si lo conservas, o compílalo en `a656cca4`), juega 20 niveles, instala el de hoy con `adb install -r` **sin** `pm clear` y comprueba que el progreso, el saldo y los ajustes siguen. Si no tienes el viejo, anótalo como sin probar.

## 19. Rendimiento, memoria, batería y temperatura (≈ 1 h 30 min, casi sin vigilancia)

0. **Desglose de memoria (lo pide el análisis del escritorio de la jornada 1, observación 8):** en los niveles 75 y 200 de la sesión larga, guarda **entero** `adb shell dumpsys meminfo -d com.amazonfill.dev` (no solo el TOTAL PSS) en `Logs/device/completa/b19/meminfo-<nivel>-<variante>.txt`. Anota en la hoja Java Heap, Native Heap, Code, Graphics, Private Other y System, y del detalle `.so mmap`, `.dex mmap`, `GL mtrack`, `EGL mtrack` y `Unknown`. Repítelo en tres variantes para aislar las causas:
   - el `dev.apk` con 18+ (anuncios de prueba de Google, si ya lleva AdMob);
   - el mismo `dev.apk` con un perfil <13 (ningún SDK de anuncios arranca);
   - el `rehearsal.apk` (sin modo Development) con 18+.
   Referencia: 08 §5.4 fija RSS ≤ 400 MB, texturas ≤ 120 MB y heap gestionado ≤ 60 MB.
1. **Sesión larga:** 60 min seguidos de juego (jugador automático más ratos a mano). Cada 15 min, anota:
   - `dumpsys meminfo` (`TOTAL PSS`);
   - `dumpsys battery` (nivel y temperatura);
   - `dumpsys gfxinfo com.amazonfill.dev` (fotogramas lentos, si da datos con Unity).
2. **`AF_PERF`:** `save_ms` y `bg_ms` de los vertidos (objetivo de `prototipo-v0.md`) y `world_banner_art`.
3. **Morpho en el teléfono:** `--ei af_bench_morpho 1 --ei af_level 106` y anota las líneas `AF_PERF morpho` (presupuesto: 150 ms).
4. **Arranque:** en frío y en caliente (`am start -W`), tres veces cada uno.
5. **Tamaño en el teléfono:** Información de la app › Almacenamiento (app, datos y caché) al principio y al final de la prueba.
6. **Sensación:** el dueño dice si en algún momento se calentó, se trabó o fue lento.

## 20. Build de ensayo de release (≈ 45 min)

1. `adb uninstall com.amazonfill.dev` (antes, apunta tu progreso: se pierde) e instala el APK del ensayo.
2. **Comprueba:**
   - no hay líneas `AF_LAYOUT`, `AF_STATE`, `AF_EVENT` ni `AF_PERF`;
   - los extras de desarrollo **no hacen nada**: `--ei af_level 300`, `--ez af_fake_store true` y `--ei af_seeds 99999` se ignoran;
   - el nombre de la app y el icono;
   - la edad y el consentimiento;
   - los niveles 1-12 a mano, sin excepciones;
   - Firebase en logcat sin errores, si el ensayo lleva `google-services.json`.
3. Vuelve a instalar el `dev.apk` para el bloque 21.

## 21. Cierre: eliminar la cuenta de verdad (≈ 20 min)

Con el perfil de dev de toda la prueba y el backend de dev (`google-services.json`):
1. Ajustes › «Eliminar cuenta y datos» › paso 1 › paso 2.
   - La nota de Play Juegos.
   - **La espera visible** mientras borra (arreglo de `tanda5/borrado-espera`).
   - `af_account_delete` `requested` → `confirmed` → `done`.
   - El juego vuelve a la primera ejecución, con la pantalla de edad.
2. Tras el borrado: la Liga ya no te lista (tu seudónimo desaparece de la pestaña Global tras recargar) y el saldo vuelve a 0.
3. Comprueba que el logcat de toda la prueba no lleva secretos (regla de arriba).

## 22. Gráficos e imágenes: revisión del dueño (≈ 1 h, se puede repartir por bloques)

Con las capturas de toda la prueba y el teléfono en la mano, el dueño marca lo que se ve mal. La guía es la dirección visual «Selva brillante premium» (D-003, `docs/art/direccion-visual-v2.md`):
- **Vidrio de las cuias:** el líquido con su brillo, las dosis separadas y el chorro que cae en el eje del destino (D-006).
- **Sellado:** la tapa o el efecto, y el confeti de la victoria.
- **TiTino:** sus poses, que no tape textos y que no salga en tableros de 2 o más filas cuando no cabe.
- **La tropa de miquitos:** su entrada y su franja.
- **Fondos de cada parada y cada mundo:** brillos y polen del río, noche y lluvia.
- **Tarjetas, banners, skins y el icono v2:** el inicio y el cajón de apps, Recientes y el icono temático monocromo.
- **Textos:** tamaño mínimo, contraste y sin cortes.
- **Nada provisional que no deba estarlo:** rectángulos sin arte, colores planos de relleno o textos en inglés de desarrollo.

**Cada cosa que no guste** va como observación con su captura, aunque sea de gusto: así se decide después.

## 23. Audio: escucha del dueño (≈ 45 min)

Con auriculares y sin ellos. La lista de qué escuchar está en `prototipo-v0.md`, «Qué escuchar (AU3)»:
- el agua de cada mundo y la lluvia ligera, media y fuerte;
- un animal cada 20-45 s;
- en `amb_w01_fauna_02` (niveles 1-10) no se entiende ninguna voz;
- el lecho de música con sus capas al sellar, sin desfasarse tras varios bucles;
- la variante nocturna en el 60, el 80 y el 100;
- el chorro que sube de tono;
- la nota de cada fruta al elegirla;
- el tema del menú;
- el volumen relativo: ningún efecto molesta ni tapa la música;
- en los mundos 10-13, el audio por código, que suena peor por diseño;
- en logcat, todas las líneas `sfx` con `"src":"clip"` en 1-700: `grep synth` debería salir vacío en los mundos 1-9.

## Qué devolver

1. **La hoja** `pruebas-telefono-<fecha>-completa.md` con:
   - la verificación del bloque 0;
   - una tabla resumen: bloque, cumple, no cumple o sin probar, y el número de observaciones;
   - el detalle por bloque;
   - las observaciones numeradas con su gravedad.
2. **Commit y push** de la hoja al final de cada jornada, en `integracion-fase1-fase2`, con el mensaje «Añade la prueba completa en el teléfono (jornada N)». Solo la hoja, nada más.
3. **Al terminar**, un resumen de las 10 observaciones más graves arriba de la hoja.

Con la regla vigente (correcciones en la misma ola), el escritorio lanza los arreglos en cuanto llega cada jornada, sin esperar al final.
