# Prueba completa del juego en el teléfono (2026-10-28, jornada 6)

El portátil sigue el encargo de la jornada 6 de `guion-prueba-telefono-completa.md`. La rama es `integracion-fase1-fase2` en `730abc06`, que trae `a2dd811f` (el encargo) y los carriles `tanda6/fx-j5-arranque` y `tanda6/fx-j5-ui`. Se prueba con el SM-S938B por depuración inalámbrica.

**Lo que entrega por ahora:** el bloque 0 y N6 (la pantalla del sistema hasta la intro y los arreglos de UI). **Ese punto falla y bloquea el Release definitivo:** en todos los arranques la pantalla del sistema la suelta el tope de 3 s (`cap`), no el juego (observación 1). Se avisó al dueño en cuanto salió.

Capturas y ráfagas en el portátil: `Logs/device/completa-j6/` (fuera de git). De las ráfagas se borraron los fotogramas en los que se veía la pantalla personal del teléfono.

## Resumen

| Bloque | Estado | Observaciones |
|---|---|---|
| 0. Preparación y builds | cumple | — |
| N6.1 La pantalla del sistema hasta la intro | **no cumple: `cap` en todos los arranques** | 1 |
| N6.2 Los arreglos de UI | cumple en lo probado; el Boto de la pausa, mudo | 2, 3 |
| 8. Economía, Mercado y Cántaro | cumple | 4, 5 |
| 12.1 Reto del río | cumple en lo probado | — |
| 18. Robustez (lo hecho) | cumple | — |
| Sesión con el dueño (17 y 16) | cumple, salvo 7 y 8 | 7, 8 |
| Sesión con el dueño (15, 18.3, 18.4, 18.6) | cumple, salvo 9 y 10 | **9 (alta)**, 10 |
| 19. Rendimiento (memoria y Morpho) | el Morpho cumple; la memoria pasa del presupuesto | 6 |

## Verificación (bloque 0)

- **Builds:**
  - `scripts/unity-build.ps1 -Method Dev` → `[AF.BuildDev] OK`, `dev.apk` de 76,97 MB;
  - `-Method Release -Rehearsal -Apk` → `[AF.BuildRelease] OK` y `[AF.LocReleaseGate] OK: textos listos para release`, con `rehearsal.aab` de 59,83 MB y `rehearsal.apk` de 65,34 MB.

  En la jornada 5 eran 76,96, 59,82 y 65,33 MB.
- **El primer intento del build se paró en la licencia** («No valid Unity Editor license found»). El dueño abrió Unity Hub y el segundo intento compiló bien.
- **Compuertas:**
  - en `dev.apk`: `check-manifest -Mode dev`, `check-16kb`, `check-no-unity-iap` y `check-no-applovin`, todas OK;
  - en el `rehearsal.aab` y el `rehearsal.apk`: esas mismas, más `check-release-classes`, todas OK;
  - en el AAB, además, `size-gate` y `check-no-unity-telemetry`, también OK.
- **.NET y esquema:**
  - `dotnet build dotnet/AmazonFill.slnx -warnaserror`: 0 errores;
  - `dotnet test`: 996 superadas y 6 omitidas, de 1.002;
  - `python tools/validate_schema.py`: 116/116 PASS.
- **Teléfono:** batería al 65 %, 30,6 °C, «No molestar» apagado.

## N6. Lo nuevo de la jornada 6

1. **La pantalla del sistema hasta la intro: no cumple (observación 1).** Se midió con ráfagas de `screencap` cada ~120 ms, contadas desde el `am start`.

   **La sonda `boot_steps` (`dev.apk`):**

   | Arranque | `TotalTime` | `os_splash_release` | `os_splash_held_ms` | `proc_os_splash_ms` | `proc_awake_ms` | `proc_visible_ms` | `visible_ms` |
   |---|---|---|---|---|---|---|---|
   | Primera apertura | 3.538 | **cap** | 3.016 | 3.393 | 3.643 | 4.041 | 362 |
   | Frío 1 | 4.068 | **cap** | 3.001 | 3.786 | 4.108 | 4.815 | 665 |
   | Frío 2 | 3.368 | **cap** | 3.002 | 3.241 | 3.444 | 3.798 | 327 |
   | Frío 3 | 3.349 | **cap** | 3.005 | 3.248 | 3.458 | 3.861 | 384 |
   | Frío 4 | 3.344 | **cap** | 3.000 | 3.247 | 3.449 | 3.911 | 432 |
   | Frío 5 | 3.397 | **cap** | 3.011 | 3.271 | 3.482 | 3.937 | 439 |

   En todos, `os_splash_by` = `intro` y `os_splash_focus_ms` está entre 64 y 99.

   **La intro no cambia:**
   - 3.500 ms;
   - `visible_by` gpu;
   - `motion_frames` 175-176 y `motion_pauses` 0;
   - `cut` false;
   - en la primera apertura, p95 de 17,2 ms y máximo de 21,8 ms.

   **Lo que dicen las cifras:** **`proc_awake_ms` llega siempre unos 200 ms después de `proc_os_splash_ms`** (3.643 frente a 3.393, y 3.444 frente a 3.241). En la jornada 5 era de 1.520 ms en la primera apertura.

   Así que el juego no arranca mientras se retiene la pantalla del sistema, y lo que la suelta es el tope de 3 s, no la intro. Es un bloqueo mutuo: el plugin espera al primer fotograma de la intro, y Unity no despierta hasta que el plugin suelta.

   **Las ráfagas:**

   | Arranque | Icono | Petróleo liso sin icono | Escena de la intro |
   |---|---|---|---|
   | Primera apertura (`dev.apk`, `n6-primera_hoja.jpg`) | de +0,5 a +3,54 s | **de +3,66 a +4,04 s (4 fotogramas)** | +4,18 s |
   | Frío 2 (`dev.apk`, `n6-frio2_hoja.jpg`) | de +0,3 a +3,38 s | **de +3,53 a +3,78 s (3 fotogramas)** | +3,90 s |
   | Primera apertura del ensayo (`rehearsal.apk`, `n6-ensayo-primera_hoja.jpg`) | de +0,42 a +3,27 s | **de +3,40 a +3,77 s (4 fotogramas)** | +3,91 s |

   - El icono no salta de posición ni de tamaño mientras se ve.
   - **El petróleo liso sin icono sigue ahí**, de 0,25 a 0,5 s, en los tres casos.
   - **La escena llega unos 3 s más tarde que en la jornada 5:** antes, ~0,9 s en frío y ~2,1 s en la primera apertura.

   **Sin probar todavía:**
   - con Reducir movimiento (lo hace el dueño);
   - la cortina de notificaciones con el nivel en marcha;
   - Inicio durante el icono;
   - abrir en frío desde una notificación.

   Quedan a la espera de la decisión sobre el plugin.

2. **Los arreglos de UI: cumplen en lo probado; el Salto del Boto en la pausa, no (observación 2).** Se probaron con el `dev.apk`, 0 semillas y un jugador que se atasca a propósito.
   - **«Continue: +1 cuia» en gris, en No more pours · Still waters (el 36, con la tienda falsa):**
     - el toque hace latir «You need 300 more seeds»: en la ráfaga del toque, un fotograma con la línea más grande y el siguiente igual que antes (`ui-rescate-latido-zoom.png`);
     - `missing_tap` con `"option":"rescue","pulsed":true,"store":true`;
     - no cobra nada, el modal sigue abierto y no sale ningún aviso (debajo está «Handful of seeds · COP 3,900»).
   - **El mismo botón sin tienda (el 37):**
     - el aviso «Purchases aren't available right now» arriba (`store_unavailable_notice`);
     - `missing_tap` con `"store":false`;
     - el modal ya no lleva el Puñado (`ui-rescate-sintienda.png`).
   - **En «Are you sure?»** (Give up con una canoa en juego): con dos toques seguidos salen dos `store_unavailable_notice` y dos `missing_tap` `give_up_rescue`, y no se cobra nada (`ui-seguro-sintienda.png`).
   - **«+3 Undo · 30 seeds» en gris** (el 38, sin tienda, `af_ads_fill no_fill` y la Marea gastada): el aviso y `missing_tap` `refill_seeds` con latido. El modal enseña la etiqueta con el precio.
   - **«Refill canoes · 5/5 · 200» en gris** (0 canoas, sin tienda):
     - **desde el nivel** (Nivel fallido › Retry): el aviso, «You need 200 more seeds» que late y `missing_tap` `refill_canoes`, sin llenar nada;
     - **desde el inicio** (el contador 0/5): igual, con `level` 0.
   - **Salto del Boto en gris (el 46, quinto fallo con 0 semillas):**
     - el banner «Rosa can take you upriver · 300 / You need 300 more seeds» sale al cargar el sexto intento;
     - en la pausa, «Boto Leap · 300» sale en gris, pero **tocarlo no hace nada** (observación 2);
     - el caso de la rama D, en el modal de bloqueo, no se pudo ver porque el sexto intento, con el alivio, se ganó.
   - **Tu fila de la Liga sin seudónimo** (`af_league` y `af_league_fake`, `af_max_level` 60): **cumple** en es-419, en y pt-BR, al 100 % y al 130 % (`--ei af_font_scale 130`; `liga-filas.jpg`).
     - El texto se para antes de la columna de puntos y pasa a 2 líneas (al 130 %, a 3; en pt-BR, a 4) con la fila más alta.
     - En pt-BR el «0» ya no toca el texto.
   - **Sin probar (los hace el dueño):** con Reducir movimiento, con TalkBack, y el caso de «Are you sure?» cuando llegan semillas con el modal abierto.

3. **Bloque 8. Economía, Mercado y Cántaro: cumple (observaciones 4 y 5).** Con la tienda falsa (`--ez af_fake_store true`) y 5.000 semillas.
   - **Las cuatro pestañas** (`b08/mercado-0..3.png`):
     - **Semillas:** Cántaro (COP 5.900, en gris con «Se puede comprar desde 1,000 semillas»), Puñado 1.000 · 3.900, Bolsa 2.800 · 9.900, Saco 6.000 · 18.900, Costal 13.000 · 36.900 y Cosecha 36.000 · 94.900;
     - **Comodines:** solo «Canoas infinitas 1 h · COP 3,900»;
     - **Cuias:** vidrio («Puesta»), Santarém («Poner»), y Encuentro y Totuma como «Premio del mundo · faltan 20/80»;
     - **Sin anuncios:** COP 9.900 con «+500 semillas».
   - **Ponerse una skin:** Santarém → `skin_selected` `af_skin_m02`, y pasa a «Puesta».
   - **Comprar «Sin anuncios»:**
     - la hoja «Compra simulada» con el precio, «Comprar» y «Cancelar»;
     - `iap_verification` `verifying` → `credited`, `earn_virtual_currency` +500 una sola vez (5.060 → 5.560), `af_iap_result` ok y `purchase` 9.900 COP;
     - `no_ads` pasa a true y la fila dice «Ya no ves anuncios entre niveles».
   - **Comprar «Canoas infinitas 1 h»:** el mismo flujo, `canoes` -1 (`infinite` true) y el HUD con ∞ y 59:55.
   - **Reabrir la app:** `ledger_restore` con `recredited` 0 y `revoked` 0. El saldo sigue en 5.560 y las canoas siguen infinitas.
   - **Al 130 % en es-419, en y pt-BR** (`b08/mercado-130-*.png`): las pestañas en una o dos líneas y las filas más altas, sin cortes. En es-419, «Semillas» y «Comodines» casi se tocan (observación 5).
   - **El Puñado** ya se compró tres veces en la campaña 1-700 de la jornada 5. **El Cántaro lleno**, su tarjeta y la oferta de «Sin anuncios» también salieron allí.
   - **Sin probar:** que no salgan intersticiales tras «Sin anuncios» mientras los vídeos siguen (bloque 9).

4. **Bloque 12.1. Reto del río: cumple en lo probado** (`b12/`). El tablero de hoy (`d2_n0300`, banda 2) se resolvió con el solver sobre `daily_pool_v1.json`.
   - **La tarjeta del inicio:**
     - antes de jugar, «Reto del río · 28 oct / Jugar» (`daily_card` `play`);
     - a medias, `in_progress`;
     - ganado, «Sellado» con «1/7».
   - **El tablero del día:** `load` con `daily` `d2_n0300`. El rótulo dice «Reto del río», los comodines son los de siempre con sus precios y la etiqueta es `normal`.
   - **Segundo plano:** tras 3 vertidos, Inicio del sistema, 5 s y volver. **El tablero sigue igual.**
   - **Atrás con el tablero del Reto en juego no hace nada**, igual que en un nivel normal: es lo diseñado (`GameBootstrap`, «como en un nivel», 02 §3 y 04 §6.6).
   - **Ganar:**
     - `win` y `daily_challenge` `result` win con `streak_days` 1, y `earn_virtual_currency` `daily_challenge` +60 (5.560 → 5.620);
     - el calendario «Reto del río · 28 oct» con el 28 sellado, «+60», «Cosecha doble: x2 por video» e «Inicio», dentro de la zona segura (`b12/reto-fin.png`).
   - **Sin probar:**
     - perder y reintentar sin gastar canoas, porque el perfil tenía Canoas infinitas compradas;
     - la racha de 7 (necesita cambiar la fecha, bloque 15);
     - 320 dp (bloque 16).
   - **Bloque 12.2, la Liga contra el backend de dev:** sin hacer. Necesita `guion-prueba-telefono-liga.md` y el backend desplegado. La lista falsa, al 130 % y en los tres idiomas, quedó vista en N6.

5. **Bloque 19. Rendimiento (lo hecho): el Morpho cumple; la memoria pasa del presupuesto (observación 6).**
   - **19.0. Desglose de memoria** (`dumpsys meminfo -d`, en `b19/meminfo-*.txt`), en KB:

     | Variante | Java Heap | Native Heap | Code | Graphics (GL mtrack) | Private Other | System | Unknown (detalle) | TOTAL PSS | TOTAL RSS |
     |---|---|---|---|---|---|---|---|---|---|
     | `dev.apk`, 18+, nivel 75 | 22.524 | 27.404 | 90.464 | 138.120 (97.832) | 253.648 | 18.791 | 212.918 | 554.155 | 665.552 |
     | `dev.apk`, 18+, nivel 200 | 13.060 | 30.728 | 90.176 | 138.004 (97.716) | 259.576 | 18.830 | 218.502 | 553.618 | 666.784 |
     | `dev.apk`, <13 (bloqueado: «Ask an adult to install the game», país US por la SIM) | 16.008 | 31.624 | 96.212 | 131.108 (90.820) | 256.508 | 19.759 | 218.442 | 554.535 | 670.788 |
     | `rehearsal.apk`, 18+, nivel 1 | 9.580 | 36.620 | 106.348 | 136.848 (96.560) | 249.156 | 22.177 | 206.783 | 565.565 | 723.504 |

     - `.so mmap` ronda los 57 MB y `.dex mmap` los 33 MB.
     - `EGL mtrack` es de 40.288 KB en todas.
     - **Las cuatro variantes ocupan casi lo mismo**, también la de <13, que no arranca ningún SDK de anuncios y se queda en la pantalla de bloqueo.

     Así que lo grande no son los anuncios ni el nivel: son `Unknown` (~210 MB, memoria nativa anónima de Unity) y Graphics (~135 MB). 08 §5.4 fija RSS ≤ 400 MB y texturas ≤ 120 MB.
   - **19.3. Morpho en el teléfono** (`--ei af_bench_morpho 1 --ei af_level 106`):
     - 29 niveles medidos, del 100 al 700, todos con `outcome` found;
     - **máximo de 7,96 ms** (en frío, 7,97 ms) frente a un presupuesto de 150, con 3.561 nodos como máximo y `cut_runs` 0.
   - **Sin hacer todavía:** la sesión larga de 60 min (en la campaña 1-700 de la jornada 5 ya se midió el PSS cada 25 niveles), el arranque en frío y en caliente (espera al arreglo de la pantalla del sistema) y el tamaño en Almacenamiento.

6. **Bloque 18 (lo hecho): cumple.**
   - **18.1. Segundo plano** a medias del Reto, tras 3 vertidos: vuelve con el mismo tablero (bloque 12.1).
   - **18.2. Matar la app** (`am force-stop`) en el 50 y reabrir:
     - **a medias, tras 3 vertidos:** `profile_integrity` valid, vuelve al nivel 50 con `pours` 3 y el mismo tablero;
     - **en el vertido ganador**, matada 0,25 s después del último toque, antes de que salga la victoria: vuelve al inicio con `profile_integrity` valid, el nivel ganado y **el premio cobrado una sola vez** (0 → 20 semillas).
   - **Sin hacer todavía:** matarla en Nivel fallido, en Sin canoas, con un anuncio y con la hoja de compra. Las interrupciones del sistema, el modo avión y la batería baja los hace el dueño.

7. **Sesión con el dueño (TalkBack, animaciones, dos dedos, 320 dp): cumple, salvo las observaciones 7 y 8.**
   - **TalkBack** (lo encendió el dueño):
     - **el botón gris «Continuar: +1 cuia»** (el 36, 0 semillas, tienda falsa) lee «Te faltan 300 semillas». El doble toque lo activa: 4 `missing_tap` `rescue` con `pulsed` true, sin cobrar nada;
     - **la Liga:** tu fila se lee entera con «Tu avatar». El doble toque sobre ella abre «Tu avatar», y sobre la fila de otro no abre nada. **Al desplazar la lista, el foco se queda en el orden que llevaba y lee un número de puesto que no es el de la fila que se ve** (observación 7);
     - **Corregir mi edad:** se leen el título, la frase y los dos botones. El dueño confirmó «De 13 a 17»: `consent` `done` con `age_band` 13-17, `npa` true y `double_confirm` true. El perfil de pruebas se rehace con `pm clear`.
   - **Quitar animaciones** (las tres escalas de animación del sistema a 0 por `adb`, con el permiso del dueño; después se devolvieron a 1.0):
     - el juego lo lee (`reduce_motion` true);
     - el toque al rescate gris da `missing_tap` `pulsed` true, pero **la línea no crece**: en la ráfaga solo cambia dentro del ancho del texto (x 238-641, frente a 191-688 con animaciones), se aclara un poco y vuelve en el tercer fotograma (`rm/`).
   - **Dos dedos** a la vez sobre el tablero (el 20, el dueño): nada raro. Hubo 14 `select`, 8 `pour` y 1 `reject` normal, sin `E/Unity`.
   - **320 dp y letra del sistema al 130 %** (`wm density 540` y `font_scale` 1.3, con el permiso del dueño; después se volvieron a 420 y 1.0), en `b16/`:
     - el inicio, la Liga (con la fila «Tú» en 4 líneas) y el Mercado se ven bien;
     - **pero hay palabras partidas a mitad** (observación 8): en el nivel, «Reinicia/r» bajo el botón y «Deshace/r» en el comodín; en el Mercado, la pestaña «Comodine/s».

8. **Sesión con el dueño (fecha, interrupciones, sin red e icono): cumple, salvo las observaciones 9 y 10.**
   - **El icono** en el cajón de apps: se ve bien, según el dueño. El monocromo no se miró.
   - **Bloque 15, un día más** (el dueño quitó la fecha automática y puso el 29; en `b15/`):
     - el Reto pasa al tablero del 29 (`daily_card` `d2_n0301`, día 301);
     - las canoas se regeneran de 0 a 5;
     - el escudo de la Crecida se renueva (`shield` true);
     - la Bitácora queda en `waiting` hasta jugar un nivel. Tras ganar el 61 pasa a `ready`; la tarjeta del inicio dice «Today's reward!» y la libreta abre la **página 2 de 30**. Al elegir «Tide ×2»: `calendar_claim` día 2, `booster` `{"undo":2}`, `sealed` y `next_day` 3.
   - **Volver al 28** (la fecha automática otra vez): `profile_integrity` valid. La Bitácora sigue sellada (`next_day` 3, sin otra página que reclamar) y el Reto vuelve al tablero del 28, sin errores de Unity.
   - **Sin probar:** la racha de 7 Retos, que necesita siete días.
   - **Bloque 18.3, interrupciones** (el dueño, en el nivel 62):
     - la llamada, la cortina y apagar y encender la pantalla: el juego sigue donde estaba y el audio vuelve;
     - **con la alarma, la música del juego sigue sonando de fondo** (observación 10);
     - **con el teléfono bloqueado más de 5 s, al encender la pantalla sale el juego encima del bloqueo y se puede jugar** (observación 9). Con menos de 5 s es normal: el teléfono tiene `lock_screen_lock_after_timeout` = 5000.
   - **Bloque 18.4 y 18.6, modo avión y ahorro de energía** (el dueño): se juegan niveles y el Reto sin red y con el ahorro activado, todo bien. El Mercado no se ve porque sin tienda está oculto, como debe (8.2).

## Observaciones

1. **Bloqueante para el Release definitivo. La pantalla del sistema la suelta el tope de 3 s en todos los arranques (`os_splash_release` = `cap`), no la intro.**
   - Pasa en la primera apertura, en los 5 arranques en frío y en el ensayo de release.
   - Unity no despierta hasta que se suelta (`proc_awake_ms` ≈ `proc_os_splash_ms` + 200 ms), así que el arranque se alarga unos 3 s.
   - El petróleo liso de entre medias, que era lo que se quería quitar, sigue ahí: 3 o 4 fotogramas, de 0,25 a 0,5 s.

   El encargo dice qué hacer en este caso: «Si sale `cap`, se anota con su ráfaga y se revierte quitando `AfSplashHold.androidlib`». **La reversión la decide y la integra el escritorio**; el portátil no toca el código.
2. **Baja. «Boto Leap» en gris en la pausa no responde al toque:** ni latido, ni `missing_tap` ni aviso. Pasó en el 46, quinto fallo, con 0 semillas y sin tienda. El toque llega a la ventana, pero el juego no escribe nada. Es el mismo «toque mudo» de la observación 6 de la jornada 5, en un botón que el arreglo no cubrió (`PauseMenu`). El encargo lo pedía en la rama D, que no se pudo ver.
3. **Baja. Con otra app con el foco, Tintalú se queda en el petróleo liso.** Pasó con RCN TV visible a la vez (`topResumedActivity` en otra pantalla) al lanzar la app: Unity escribió `m_RunWithoutFocus: false` y se paró en `UnloadTime` más de 40 s, sin intro. Al cerrarla el dueño, siguió normal. Hay que mirarlo de nuevo con el arreglo de la pantalla del sistema, que toca justo el correr sin foco.
4. **Baja. Las skins de la pestaña Cuias son círculos de color lisos, sin el dibujo de la cuia** (vidrio, Santarém, Encuentro, Totuma). Es lo mismo que la vasija del Cántaro (observación 7 de la jornada 5). Así, el jugador no ve qué está eligiendo.
5. **Baja. Al 130 % en es-419, las pestañas «Semillas» y «Comodines» del Mercado casi se tocan** (`b08/mercado-130-es-419.png`).
6. **Media. La memoria pasa del presupuesto de 08 §5.4:** RSS de 666 a 724 MB frente a ≤ 400 MB, y Graphics de ~135 MB frente a texturas ≤ 120 MB. Ocupa lo mismo en un nivel, en el ensayo de release y en la pantalla de bloqueo de <13, sin SDK de anuncios. Lo grande es `Unknown` (~210 MB) más `GL mtrack` (~97 MB). En este teléfono, de 12 GB, no da problemas, pero en uno de 3-4 GB, el público de América Latina, Android podría cerrarla en segundo plano.
7. **Media (accesibilidad). Con TalkBack, al desplazar la lista de la Liga, el número de puesto que se lee no es el de la fila que se ve:** el foco se queda en el orden que llevaba antes de desplazar (lo vio el dueño).
8. **Media. A 320 dp con la letra del sistema al 130 % se parten palabras a mitad:** «Reinicia/r» (HUD del nivel), «Deshace/r» (comodín) y «Comodine/s» (pestaña del Mercado), en es-419 (`b16/nivel.png`, `b16/mercado.png`).
9. **Alta. El juego se muestra encima de la pantalla de bloqueo y se puede jugar sin desbloquear** (lo vio el dueño dos veces, esperando más de 5 s).
   - La ventana de `UnityPlayerGameActivity` lleva `fl=0x80e90500`, es decir, `FLAG_SHOW_WHEN_LOCKED`, `FLAG_TURN_SCREEN_ON` y `FLAG_DISMISS_KEYGUARD`. El sistema escribe `KeyguardService: setOccluded(true)`.
   - El manifiesto fusionado no lleva `showWhenLocked`, y nuestro código tampoco lo llama. En el `dev.apk`, `dexdump` lo encuentra en el reproductor de Unity: `Lcom/unity3d/player/A;` llama a `Activity.setShowWhenLocked` y `Activity.setTurnScreenOn`. También está en `com.google.android.gms.ads.internal.overlay.zzm`, pero solo para la pantalla de un anuncio.
   - Hay que ver qué ajuste de Unity lo activa y quitarlo.
10. **Media. Con una alarma sonando, la música del juego no se para ni baja:** sigue de fondo (lo oyó el dueño, nivel 62). La llamada sí la para.

## Notas de la herramienta (no son defectos del juego)

- Tras cada build de Unity el demonio de `adb` se reinicia y el teléfono vuelve con otro puerto (hoy, 41391).
- `af_font_scale` es un entero (`--ei af_font_scale 130`): con `--ef 1.3` no hace nada.
- El primer toque en el inicio recién cargado, o en un modal que se está abriendo, a veces se pierde. Sale bien al repetirlo.
- Para atascarse a propósito, el jugador elige en cada paso el vertido que deja menos vertidos posibles. El de movimientos al azar no se atascaba en el 35.
