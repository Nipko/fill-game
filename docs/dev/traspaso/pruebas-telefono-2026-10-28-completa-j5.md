# Prueba completa del juego en el teléfono (2026-10-28, jornada 5)

El portátil sigue el encargo de la jornada 5 de `guion-prueba-telefono-completa.md` (`7ca60990`) sobre `integracion-fase1-fase2` en `7ca60990`, posterior a `e0c3adf2`, con el SM-S938B por depuración inalámbrica. Esta jornada decide el lanzamiento (D-059).

**Lo que entrega:** el bloque 0, N5 entero y, de los pendientes:
- lo que quedaba del bloque 3;
- buena parte del 4 y del 7;
- de paso, el 8.5 (compra con 13-17) y parte del 9 y del 20.

El bloque 6 (muestra amplia con el jugador automático) está completo: 308 de 308 niveles ganados.

**Siguen pendientes:** los bloques 5, 8 a 19 (salvo lo de paso), 21, 22 y 23, y lo que hace el dueño.

Capturas y logcat en el portátil: `Logs/device/completa-j5/` (fuera de git). El logcat se lee filtrando `DebugAppCheckProvider`, «debug secret», «debug token», `appcheck:debugtokens` y `eyJ`. De las ráfagas se borraron los fotogramas en los que se veía la pantalla personal del teléfono detrás de la del sistema, y una captura de Ajustes que enseñaba el código de cuenta.

## Resumen

| Bloque | Estado | Observaciones |
|---|---|---|
| 0. Preparación y builds | cumple | — |
| N5.1 Intro de 3,5 s y el segundo de petróleo | la intro cumple; el petróleo liso sigue | 1, 2 |
| N5.2 Toque con la lista en movimiento | cumple en 5 listas; el Mercado no se desplaza | 3 |
| N5.3 Confirmación neutral al corregir la edad | cumple | — |
| N5.4 Tu fila de la Liga | cumple; con seudónimo no se pudo ver | 4, 5 |
| N5.5 Ensayo de release | cumple | — |
| 3. Núcleo (lo que quedaba) | cumple; sin probar dos dedos y 16 cuias | — |
| 4. Comodines | cumple en lo probado | — |
| 7. Perder, rendirse y reintentar | cumple en lo probado | 6 |
| 8.5 Banda 13-17 | cumple | — |
| 6. Muestra con el jugador automático | cumple (308/308) | — |

## Verificación (bloque 0)

| Comando | Resultado |
|---|---|
| `git pull origin integracion-fase1-fase2` | `7ca60990` («Encarga al portátil la jornada 5…») |
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 errores |
| `dotnet test dotnet/AmazonFill.slnx` | 996 superados, 6 omitidos (1.002) |
| `python tools/validate_schema.py` | 116/116 PASS |
| `pwsh -NoProfile -File scripts/unity-build.ps1 -Method Dev` | Código 0, `[AF.BuildDev] OK`: **`dev.apk` de 76,96 MB** (antes 76,94) |
| `ci/check-manifest.ps1 -Mode dev`, `ci/check-no-unity-iap.ps1`, `ci/check-no-applovin.ps1` y `bash ci/check-16kb.sh` sobre el `dev.apk` | OK los cuatro. `AdsSdkInitializer`: no aparece |
| `pwsh -NoProfile -File scripts/unity-build.ps1 -Method Release -Rehearsal -Apk` | Código 0: **`rehearsal.aab` de 59,82 MB** (antes 59,81) y **`rehearsal.apk` de 65,33 MB** (antes 65,32). **`[AF.LocReleaseGate] OK: textos listos para release`**, sin quejas |
| `size-gate`, `check-16kb.sh`, `check-manifest.ps1 -Mode dev`, `check-release-classes.ps1`, `check-no-unity-iap.ps1` y `check-no-applovin.ps1` sobre el AAB y el APK del ensayo | OK todos. `AdsSdkInitializer`: no aparece |
| `git status` tras los builds | limpio (`GooglePlayGameSettings.txt` ya no sale modificado) |

**Estado del teléfono:**
- Android 16;
- 1080 × 2340 sin override;
- densidad 420 (física 450);
- fuente 1,0;
- región `en-US`;
- sin «No molestar»;
- batería al 75 % y 36,3 °C al empezar.

## N5. Lo nuevo de la jornada 5

1. **La intro de 3,5 s: cumple. El segundo de petróleo: sigue (observaciones 1 y 2).**
   - **Primera apertura** (`pm clear`, `logcat -c` y ráfaga de 60 capturas, `n5/burst_primera/`):
     - `am start -W`: `TotalTime` 660 ms.
     - `brand_intro`:
       - `first_frame_ms` 140, `visible_ms` 935, `visible_by` **gpu**, `play_ms` 936;
       - `motion_frames` **175**, `frame_p95_ms` **18,5**, `frame_max_ms` **26,1**, `motion_pauses` **0**;
       - `shown_ms` 3.500, `cut` false, `emblem_cache` miss, `loading_ms` 0, `wait_capped` false.
     - `boot_steps`: `awake_ms` 128, `proc_awake_ms` **1.520**, `proc_visible_ms` **2.492**, `visible_ms` 935, `play_ms` 936, `motion_hold_ms` 2.913, `load_ms` 4.778.
     - **Línea de tiempo** desde `am start`:

       | Tiempo | Qué se ve |
       |---|---|
       | 0,4-0,6 s | la pantalla del sistema con el icono |
       | **0,83-2,12 s** | **petróleo liso, sin icono** (≈ 1,3-1,4 s) |
       | desde 2,26 s | la escena quieta: la cuia sobre el río café, sin teñir |
       | 2,8-3,4 s | empieza a moverse suave |
       | 3,6-6,8 s | el vertido, el río que se tiñe y «Tintalú» con TiTino |
       | 7,8 s | la pantalla de edad |

       No sale «Cargando…»: tras la intro se queda la escena con TiTino hasta la edad.
     - **Conclusión:** el hueco tiene dos mitades.
       - **El arranque del motor:** el proceso empieza ~0,1 s tras `am start` y despierta en `proc_awake_ms` 1.520, o sea ~1,6 s. Va de 0,83 s a ~1,6 s, unos 0,8 s.
       - **La GPU:** el primer fotograma se pide a los ~1,77 s y llega a la pantalla hacia los 2,2 s; la lectura de la GPU lo confirma en `proc_visible_ms` 2.492.
       - `visible_ms` − `first_frame_ms` = **795 ms**. En frío, con caché, es de **~280 ms**.

       Así que, en la primera apertura, ~0,5 s del hueco es la GPU sin caché de sombreadores y ~0,8 s es el motor arrancando.
   - **Cinco arranques en frío** (`force-stop` y `am start -W`). En todos: `visible_by` gpu, `motion_frames` 175, `motion_pauses` 0, `shown_ms` 3.500, `cut` false, `emblem_cache` hit, `loading_ms` 0-1 y `wait_capped` false.

     | Arranque | `TotalTime` | `intro_ms` | `first_frame_ms` | `visible_ms` | `play_ms` | `frame_p95_ms` | `frame_max_ms` | `proc_awake_ms` | `proc_visible_ms` | `motion_hold_ms` | `load_ms` |
     |---|---|---|---|---|---|---|---|---|---|---|---|
     | 1 | 346 | 3.826 | 22 | 316 | 317 | 17,5 | 19,7 | 625 | 957 | 2.901 | 3.768 |
     | 2 | 322 | 3.813 | 18 | 300 | 302 | 17,3 | 17,7 | 594 | 904 | 2.908 | 3.737 |
     | 3 | 320 | 3.798 | 18 | 292 | 293 | 17,2 | 17,6 | 577 | 886 | 2.904 | 3.767 |
     | 4 | 318 | 3.816 | 21 | 306 | 308 | 17,3 | 19,6 | 572 | 895 | 2.909 | 3.803 |
     | 5 | 342 | 3.824 | 17 | 295 | 296 | 17,3 | 18,5 | 593 | 905 | 2.900 | 3.822 |

     Una ráfaga en frío (`n5/burst_frio/`) muestra que, **aun con caché, hay ~0,45 s de petróleo liso** (de 0,43 a 0,91 s) antes de la escena (1,05 s). Es menos que en la primera apertura, pero el guion esperaba ninguno (observación 2).
   - **Primera apertura completa**, con respuestas inmediatas: edad, consentimiento y primer toque en el nivel 1 a los **7,27 s de la app** (`first_touch_ms`), ≈ 8,9 s desde el lanzamiento. Cumple el objetivo de < 20 s.
   - **Inicio durante el vertido y volver:**
     - la intro termina (`shown_ms` 3.500, `cut` false, `motion_pauses` 1 por la pausa);
     - vuelve al nivel 1 (`resume` y `ready`);
     - 0 `E/Unity` del juego.
   - **La mirada del dueño** (fluidez, duración, el açaí con fundido): pendiente; no estaba. En las capturas, el vertido avanza por pasos regulares y sin saltos.
   - **Reducir movimiento:** sin probar (lo enciende el dueño).
2. **El toque con la lista en movimiento: cumple en 5 de 6 listas (observación 3).**
   - **Cómo se probó:** un deslizamiento de 70 ms y, en el mismo `adb shell`, un toque; luego dos capturas a +0,1 s y +0,7 s. Si la lista se paró con el toque, no cambian.

     | Lista | Con la lista en inercia | Quieta, el mismo toque |
     |---|---|---|
     | Ajustes | se para (0 eventos) | activa la fila (`settings_change` `language` en-XA; se volvió a Automático) |
     | Ajustes sobre «Corregir mi edad» | no abre la edad | — |
     | Liga | se para: **0,01 %** de píxeles cambian (control sin toque: **9,70 %**) | sin eventos (las filas de otros no hacen nada) |
     | «Tu avatar» | se para: 0,01 %, sin elegir | elige (`af_avatar_select` dart_frog) |
     | Hoja de la fruta | se para: 0,00 % | las filas no son botones |
     | Muestras del Mapa | se para: 0,00 % | — |
     | **Mercado** | **no se desplaza**: las pestañas Semillas y Cuias caben enteras, también al 130 % | el toque abre la compra (lo correcto con la lista quieta) |

   - **Casi parada:** no se dio ningún toque que no se activara cuando debía.
3. **La confirmación neutral al corregir la edad: cumple.**
   - **La misma banda guardada** (18+ sobre 18+): cierra sin confirmación y la fila sigue en Ajustes.
   - **Otra banda:** sale «Correct my age · You chose: {banda}. Your age can only be corrected once, and **this** can't be undone.», con «Choose again» y «Confirm».
     - **9 capturas** (`n5-conf-<banda>-<idioma>.png`): «Under 13», «13 to 17» y «18 or older» en en, es-419 y pt-BR. La de 18+ se hizo en un perfil desechable que empezó en 13-17.
     - Comparando píxeles, **solo cambia la línea del texto**: sin avisos para «Menos de 13» y con los dos botones del mismo tamaño y color.
     - En es: «Elegiste: … La edad solo se puede corregir una vez y no se puede deshacer.» con «Volver a elegir»/«Confirmar».
     - En pt-BR: «Você escolheu: … A idade só pode ser corrigida uma vez e não dá para desfazer.» con «Escolher de novo»/«Confirmar».
   - **«Choose again»** vuelve a la elección de edad. **Atrás** desde la confirmación vuelve a la elección, y otro Atrás la cierra. **La X** cierra (`consent` `step` none). En los tres casos la fila «Corregir mi edad» sigue: no se gasta.
   - **«Confirm» con 13-17** (perfil desechable desde 18+):
     - `af_settings_change` con `key` consent y `value` age;
     - `consent` `done` con `age_band` 13-17;
     - la fila desaparece de Ajustes.
   - **«Confirm» con «Menos de 13»** (otro perfil desechable): `consent` `blocked` y «Ask an adult to install the game»; sigue bloqueado al relanzar. Después, `pm clear`.
   - **El primer arranque:** al elegir 13-17 en la pantalla de edad se pasa al consentimiento sin confirmación.
   - **Sin probar:** TalkBack (el dueño).
4. **Tu fila de la Liga: cumple; con seudónimo no se pudo ver (observaciones 4 y 5).**
   - **Tocar la fila abre «Tu avatar»** (`avatar` `cause` open) en el disco, el centro, la esquina superior derecha, la inferior derecha y la izquierda.
   - Tocar la fila de otro jugador (y = 1.100) no hace nada.
   - **Sin seudónimo:** «Tú» / «You» / «Você» sale **una sola vez**, con «Gana un nivel para entrar en la Liga», centrado, en los tres idiomas (`n5-liga-fila-seudonimo-<idioma>.png`).
   - **Con seudónimo:** no se pudo ver. Gané el 61 y el 62 con la Liga falsa (`league_win` 10 puntos, aceptado), pero la Liga falsa no suma tus puntos y la fila siguió en 0 (observación 4).
5. **El ensayo de release (`rehearsal.apk`): cumple.**
   - Desinstalado el `dev.apk` e instalado el ensayo, la primera apertura lleva a la edad.
   - **Los extras de desarrollo se ignoran:** con `--ei af_level 300 --ez af_fake_store true --ei af_seeds 99999`, el juego empieza en el nivel 1 con 0 semillas.
   - **La intro de 3,5 s** en frío (ráfaga `n5r/burst_frio/`):
     - ~0,35 s de petróleo liso (de 0,69 a 0,92 s);
     - la escena desde 1,04 s;
     - el vertido avanza regular durante ~3,5 s.
   - Del 1 al 10 jugados por coordenadas.
     - El 11 no se ganó: mis coordenadas guardadas no cuadran con su tablero; es cosa de la herramienta.
     - La Bitácora y el Mapa del 10 salen.
     - El inicio del 11 lleva el disco del avatar.
   - **La confirmación de edad:** «Correct my age · You chose: 13 to 17…» igual que en desarrollo; la X la cierra sin gastar.
   - **La fila de la Liga:** sin probar. El ensayo no enseña la tarjeta de la Liga en el inicio (sin los extras de Liga falsa, que en release no hacen nada).
   - **Logcat del proceso:** **0 líneas `AF_*`**, y como errores solo el esperado `E/firebase` y `E/Unity` `clazz` de `DebugAppCheckProviderFactory`.
   - Después se volvió a instalar el `dev.apk` con un perfil 18+ limpio.

## Pendientes

1. **Bloque 3 (lo que quedaba): cumple.**
   - **La tropa en su franja** (el 335, de 3 filas, `b03-335-a.png` a 0,8 s): los miquitos entran entre la fila del HUD y el panel, sin pisar ninguno. Uno queda en parte detrás de la píldora «Hard».
   - **15 cuias** (el 335 con la Cuia extra por 150 semillas): 5/5/5, todas a la vista y tocables (`b03-335-extra.png`); el botón «+1 cuia» pasa a gris.
   - **16 cuias:** sin probar. Hace falta quedarse bloqueado en el 335 con la extra y rescatar.
   - **El sonido del rechazo**, ya sin «No molestar», en el 2 con `af_dev_reject 1`:
     - tocar una vacía sin nada elegido da `reject` con `sfx_illegal` y sin háptico;
     - 2 de una racha de 3 en un hueco de 2 deja 1 (`[3,8,8,8]` → `[3,8]`);
     - verter en una vacía mueve la racha;
     - verter sobre una cuia llena se rechaza (`reject`).
   - **Sin probar:** dos dedos a la vez (`adb input` no puede; lo hace el dueño).
2. **Bloque 4. Comodines: cumple en lo probado.**
   - **Marea:**
     - ilimitada en 1-10: cinco deshacer seguidos en el 5;
     - desde el 11, el número blanco con las gratis y la chapa «+3» con el stock;
     - agotada con relleno: el botón lleva «+3 ▶» y el vídeo simulado `rv_undo_refill` da `af_ad_request` filled, `af_ad_shown`, `af_ad_reward` y +3;
     - agotada **sin relleno** (`af_ads_fill no_fill`): el botón lleva el precio «30» y cobra 30 (`spend_virtual_currency` `undo_refill`, de 100 a 70) con +3, sin botón de vídeo.

     El cupo gratis vuelve a 3 al relanzar la app con el nivel a medias. Es lo que dice 04 §2.1 («se restaura… al cargar cualquier nivel»), así que no es observación.

     **Sin probar:** dentro de Aguas estancadas, y el tope de 4 vídeos al día con la espera de 60 s.
   - **Cuia extra:** en el 335, por 150 semillas, una por nivel (el botón se apaga). Sin probar: con stock, que se conserve al reiniciar y en el bloqueo.
   - **Morpho:** en el 15, por 100 semillas, marca 5 → 2 (`hint` `phase` pour). Sin probar: con stock, por vídeo, en Aguas turbias y con `hint_supported` falso.
   - **Pororoca:**
     - en el 30, la confirmación «The wave stirs the river without touching full cuias. 300 seeds. Continue?»;
     - al confirmar, cobra 300 y da `shuffle` `shuffled` (`used_level` 1, método `inverse`, `par` de 15 a 11), y «Mix» pasa a gris;
     - **en el 143 (Lluvia) y el 183 (Piraña), deshabilitada:** `shuffle` `denied`, `detail` unsupported, con el globo «The wave doesn't come with rain or piranhas»;
     - **salir y volver** (relanzar sin extras): el tablero barajado **se conserva** y «Mix» sigue en gris.

     Sin probar: los 3 al día, y en un nivel con mecánicas (43, 63).
   - **Salto del Boto** (el 46 como nivel frontera, `af_max_level` 45):
     - tras 5 fallos, al cargar el sexto intento sale el banner **«Rosa can take you upriver · 300»**;
     - en la pausa, **«Boto Leap · 300»** (`pause_skip`);
     - la confirmación «Skip the level? Rosa takes you upriver, to the next level» con «Skip level · 300»;
     - al confirmar, cobra 300, carga el 47 y registra `af_skip_used` con `fails_level` 5 y `since_last_skip` 46, sin premio.

     Con el 46 por debajo del nivel máximo (`af_max_level` 60) **no se ofrece**: el código lo oculta en los niveles ya superados (`LevelFlow.Skip.cs`), que es lo diseñado.

     Sin probar: en Aguas estancadas (rama D) y que no vuelva a ofrecerse hasta 10 niveles después.
   - **Sin probar:** la Luciérnaga (4.5), «Volver a la orilla» desde el 31 (4.7), la Cosecha doble (4.8; su vídeo de prueba ya se vio en la jornada 4) y Música y Sonido (4.9 ya hecho).
3. **Bloque 7. Perder, rendirse y reintentar: cumple en lo probado (observación 6).** Para bloquearme a propósito usé un jugador que llena las vacías con colores sueltos.
   - **Rama C (el 25):** «No more pours · Still waters», «Undo · Tide (6)» y «Back to shore (free)», sin canoa ni «El río bajó». Además, «Leaf Shield ready: this loss doesn't break your Rising River».
   - **Rama A (el 35, con 1.000 semillas):**
     - «Trip lost: −1 canoe»;
     - «Continue: +1 cuia» por **300** (600 tachado, «First rescue today: −50 %»);
     - «Undo · Tide» y «Give up» al pie.

     El rescate cobra 300 y entra la cuia de rescate (`cuias_count` 10).
   - **Segundo rescate del día** (el 36): cuesta **600**.
   - **Rama B (0 semillas):** «Continue» en gris con «You need 600 more seeds». Tocarlo no hace nada (observación 6).
   - **Rama G (1 canoa):** «Trip lost: your last canoe».
   - **Rama S:** «Give up» abre «Are you sure? You'll lose your last canoe», con el rescate otra vez y «Give up». Al confirmar:
     - Nivel fallido con canoas 0/5;
     - el escudo de la Crecida cubre el fallo (`shield_used` true, la racha sigue).
   - **Sin canoas** (Retry con 0):
     - el cronómetro;
     - «Refill canoes · 5/5» por 200 (en gris sin saldo);
     - «Endless Canoes 1 h · COP 3,900»: sin tienda sale el toast «Purchases aren't available right now» (`store_unavailable_notice`), no un toque mudo;
     - «While you wait: Play the River Challenge», que carga el Reto sin gastar canoas;
     - «Back to the River Map»;
     - **Atrás vibra** (`haptic_back_blocked`) y no cierra;
     - con los anuncios de prueba de Google, también sale «Extra canoe · Watch video +1»;
     - con 1.800 semillas, «Refill canoes» cobra 200 y vuelven las 5.
   - **Reintentar muchas veces** (el 46, Difícil, 10 intentos en dos tandas):
     - las canoas bajan una por fallo (5 → 0) y al sexto intento sale Sin canoas;
     - **tras el tercer fallo, `river_lowered`** y el cuarto intento lleva `relief` 0 (una cuia más, `relief_served` true);
     - tras 5 fallos, Rosa y el Boto (bloque 4).
   - **Sin probar:** la rama A′ (mi jugador se perdió en el 35, que tiene una cuia alta de 5), la F, la regeneración de canoas (25 min), salir desde la pausa y la Crecida rota al segundo fallo.
4. **De paso, bloque 8.5 (banda 13-17): cumple.**
   - En un perfil corregido a 13-17, con la tienda falsa, comprar el Puñado pide «Confirm the purchase? Handful for COP 3,900. Google Play will then ask you to confirm the payment.», con Cancelar y Comprar.
   - El consentimiento de 13-17 solo lleva la casilla de analítica, sin la de anuncios.
5. **De paso, bloque 7.9 y bloque 9 (anuncios):** el vídeo simulado de la Marea (arriba) y la pantalla «Simulated ad · rv_undo_refill · rewarded», con la cuenta atrás y «Close», que se habilita al terminar.

6. **Bloque 6. Muestra con el jugador automático: cumple, 308/308.**
   - **Qué hace:** cada nivel se carga con `af_level` y se juega con la solución del solver. Recorre:
     - los niveles 1-200 seguidos;
     - del 205 al 690, de 5 en 5;
     - del 691 al 700.

     Estos son 307 niveles más el 335 repetido.
   - **El corte:** en el 187 el teléfono perdió la depuración inalámbrica. Al reactivarla volvió con otro puerto, y la muestra siguió desde el 188.
   - **308/308 ganados.** En todos ellos:
     - el tablero cargado coincide con el pack;
     - la etiqueta de dificultad coincide con la del pack;
     - no hubo ni un `reject`;
     - no hubo excepciones de Unity.
   - **Tarjetas de mecánica:** salieron en el primer nivel de cada mecánica y en sus repasos (14, 25, 27, 31, 40, 43…, 698, 700).
   - **Banners de mundo:** salieron en el 11, 41, 81 y 141. Los inicios de los mundos 6 a 9 (201, 301, 421 y 561) no entraban en la muestra de 5 en 5, así que esos banners quedan para el recorrido completo.
   - **Ritmo:** mediana de 23,0 s por nivel y máximo de 31,2 s, con el vertido y la victoria incluidos.
   - **Memoria y temperatura** en dos tramos (1 h 10 min y 50 min), con muestras cada 8 min:
     - PSS entre 381 y 560 MB, sin crecer de un tramo a otro;
     - batería del 92 al 75 % y del 100 al 89 %;
     - temperatura máxima de 36,6 °C.

## Observaciones

1. **Alta. La primera apertura sigue enseñando ≈ 1,3 s de petróleo liso**, de 0,83 a 2,12 s, entre la pantalla del sistema con el icono y la escena. Las cifras dicen que ~0,8 s es el arranque del motor (`proc_awake_ms` 1.520) y ~0,5 s es la GPU sin caché de sombreadores (`visible_ms` − `first_frame_ms` = 795 ms, frente a ~280 ms con caché). La intro de 3,5 s se ve entera y fluida (175 fotogramas, p95 de 18,5 ms y 0 pausas). La pantalla de edad llega a los 7,8 s (en la jornada 4, 4,6 s), porque la carga ya no corre mientras la intro se mueve.
2. **Media. En los arranques en frío también hay ~0,45 s de petróleo liso** (de 0,43 a 0,91 s) antes de la escena, con la caché del emblema (hit). En el ensayo de release, ~0,35 s. El guion esperaba que con caché no hubiera.
3. **Baja. El Mercado no se puede probar con el toque en movimiento:** sus listas caben enteras, también al 130 %, así que nunca hay inercia. No es un fallo; solo queda sin cubrir.
4. **Baja. La Liga falsa (`af_league_fake`) no suma tus puntos:** tras ganar dos niveles (`league_win` aceptado), tu fila sigue en 0 y sin seudónimo. El caso «con seudónimo» de N5.4 no se puede ver con la Liga falsa; hace falta la Liga contra el backend de dev (bloque 12.2).
5. **Baja. En pt-BR, el «0» de los puntos de tu fila casi toca el texto** «Vença um nível para entrar na Liga», que llega hasta el borde derecho (`n5-liga-fila-seudonimo-pt-BR.png`).
6. **Baja. Sin saldo y sin tienda, «Continue: +1 cuia» en gris no hace nada al tocarlo.** La línea «You need 600 more seeds» ya explica por qué, pero el guion (8.2) pide un aviso en vez de un toque mudo cuando una pantalla lleva a una compra. En Sin canoas, «Endless Canoes» sí avisa con el toast.

## Notas de la herramienta (no son defectos del juego)

- Mi jugador por coordenadas del ensayo no gana el 11: las posiciones guardadas son de otro tablero.
- La primera prueba del Salto del Boto se hizo con el 46 ya superado (`af_max_level` 60) y no salía. Con el 46 como frontera sale bien.
- La primera prueba de la Pororoca relanzaba con `af_level`, que vuelve a cargar el nivel. Relanzando sin extras, el tablero barajado se conserva.
