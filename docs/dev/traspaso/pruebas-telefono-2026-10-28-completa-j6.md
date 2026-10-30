# Prueba completa del juego en el teléfono (2026-10-28, jornada 6)

El portátil sigue el encargo de la jornada 6 de `guion-prueba-telefono-completa.md`. La rama es `integracion-fase1-fase2` en `730abc06`, que trae `a2dd811f` (el encargo) y los carriles `tanda6/fx-j5-arranque` y `tanda6/fx-j5-ui`. Se prueba con el SM-S938B por depuración inalámbrica.

**Lo que entrega por ahora:** el bloque 0 y N6 (la pantalla del sistema hasta la intro y los arreglos de UI). **Ese punto falla y bloquea el Release definitivo:** en todos los arranques la pantalla del sistema la suelta el tope de 3 s (`cap`), no el juego (observación 1). Se avisó al dueño en cuanto salió.

Capturas y ráfagas en el portátil: `Logs/device/completa-j6/` (fuera de git). De las ráfagas se borraron los fotogramas en los que se veía la pantalla personal del teléfono.

## Resumen

| Bloque | Estado | Observaciones |
|---|---|---|
| 0. Preparación y builds | cumple | — |
| N6.1 La pantalla del sistema hasta la intro | **no cumple: `cap` en todos los arranques** | 1 |
| N6.1 repetido (build `58be8444`) | la sonda cumple; el icono salta de tamaño | 11 |
| N6.2 Los arreglos de UI | cumple en lo probado; el Boto de la pausa, mudo | 2, 3 |
| 8. Economía, Mercado y Cántaro | cumple | 4, 5 |
| 12.1 Reto del río | cumple en lo probado | — |
| 18. Robustez (lo hecho) | cumple | — |
| Sesión con el dueño (17 y 16) | cumple, salvo 7 y 8 | 7, 8 |
| Sesión con el dueño (15, 18.3, 18.4, 18.6) | cumple, salvo 9 y 10 | **9 (alta)**, 10 |
| 19. Rendimiento (memoria y Morpho) | el Morpho cumple; la memoria pasa del presupuesto | 6 |
| Los arreglos de la hoja j6 (build `d3e601a2`, punto 10) | cumplen 4, 5, 7, 9, 10 y la sonda de 11; quedan 8 y la memoria; el icono cambia de forma | 12, 13, 14, 15 |
| Botones en gris, 14 y 13 (build `d3e601a2`, punto 11) | cumple: la 2 queda arreglada; el aviso abre la Bitácora | 16 |
| Bloques 5, 9, 10.1 y 11 (punto 12, lo hecho) | cumple; los banners repiten dibujo | 17 |
| Lo automatizable que quedaba (punto 13) | cumple; el Mapa del 700 dice «Nivel 701» | 18 |
| Arreglos de la segunda hoja j6 (build `ff3c49c0`, punto 14) | cumplen el icono y la bandeja; el aviso no abre el Reto; la memoria, por encima | 19, 20 |
| Lo que quedaba del punto 14 y los bloques 9, 10, 11, 12, 13, 14, 18 y 19 (punto 15) | cumple; la 15 queda arreglada; la Liga no se puede probar (App Check) | 20, 21, 22 |
| Arreglos de la tercera tanda j6 (build `da40034d`, punto 16) | cumplen el Mapa y el aviso; OpenGL ES 3 rinde igual y gasta 10-30 MB menos | 20 |

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

9. **N6.1 repetido con el build nuevo** (`58be8444`: el velo sin bloqueo y «la intro nace del logo»): **la compuerta de la sonda cumple, pero el icono salta de tamaño al pasar a la intro (observación 11).**
   - **Builds:**
     - `dev.apk`, 77,13 MB (`[AF.BuildDev] OK`);
     - ensayo, `[AF.BuildRelease] OK` y `[AF.LocReleaseGate] OK`: `rehearsal.aab` 59,98 MB y `rehearsal.apk` 65,49 MB.
   - **Compuertas** (`check-manifest -Mode dev`, `check-16kb`, `check-no-unity-iap` y `check-no-applovin` en los tres; `check-release-classes` en los dos del ensayo; `size-gate` y `check-no-unity-telemetry` en el AAB): todas OK.
   - **La sonda (`dev.apk`):**

     | Arranque | `TotalTime` | `os_splash_release` | `os_splash_held_ms` | `proc_awake_ms` | `proc_os_splash_ms` | `os_splash_veil` |
     |---|---|---|---|---|---|---|
     | Primera apertura | 356 | signal | 877 | 770 | 1.130 | system |
     | Frío 1 | 368 | signal | 866 | 725 | 1.145 | system |
     | Frío 2 | — | signal | 855 | 659 | 1.103 | system |
     | Frío 3 | 355 | signal | 901 | 692 | 1.165 | system |
     | Frío 4 | 379 | signal | 802 | 698 | 1.068 | system |
     | Frío 5 | 337 | signal | 791 | 621 | 1.036 | system |

     - En todos, `os_splash_by` = `intro`, el velo dura menos de 3.000 ms y **`proc_awake_ms` llega antes que `proc_os_splash_ms`**.
     - La intro: 3.500 ms, `visible_by` gpu, `motion_frames` 173-176, `motion_pauses` 0, `cut` false, p95 de 17,0-17,6 ms y `morph_mode` open con `morph_ms` ≈ 702.
   - **Las ráfagas:**
     - **Primera apertura** (`n6b-primera_hoja.jpg`): icono de +0,22 a +1,32 s, solo la cuia en +1,43, el círculo abriéndose en +1,58 y la escena en +1,69 s; «Tintalú» a los ~3,7 s. **Ningún fotograma de petróleo liso.** La escena llega ahora a ~1,7 s, frente a ~4,2 s con el build anterior.
     - **Pero el icono salta de tamaño** al pasar del sistema a la intro: en la ráfaga reducida mide 81 unidades mientras se ve y 68-72 en el primer fotograma de la intro (de ~486 a ~426 px en pantalla, un 12-16 % menos). El fondo pasa además de (20,68,74) a (22,69,73). Sale en la primera apertura, en las 4 ráfagas en frío legibles y en el ensayo.
     - **Ensayo de release** (`rehearsal.apk`, primera apertura, `n6b-ensayo_hoja.jpg`): `TotalTime` 133, sin petróleo liso, y el mismo salto (81 → 72).
   - **La opinión del dueño**, literal: «Mejoró, pero no está pro: aparece la primera imagen pero tiene luego otra que se ve y la hace como titilar y luego sigue el efecto… se ve como si fuera un error de sincronía, aparece la imagen y luego un momento se ve que la misma imagen aparece».
   - **La observación 3, repetida:** con RCN TV con el foco al lanzar (`mCurrentFocus` en RCN), **ahora sale la intro entera** (`brand_intro` completa, `signal`) y el foco pasa al juego. **Queda resuelta.**
   - **Sin probar con este build:** Reducir movimiento, Inicio durante el icono y abrir en frío desde una notificación.

10. **El build con los arreglos de la hoja j6** (2026-10-29, `d3e601a2`, que trae `b8d20b1b`, el encargo, y los carriles `fx-j6-icono-salto`, `fx-j6-ui`, `fx-j6-skins`, `fx-j6-memoria`, `fx-j6-bloqueo` y `fx-j6-liga-alarma`): **cumplen el bloqueo, la alarma y Spotify, TalkBack, las pestañas y las skins; la sonda del icono cumple, pero el icono cambia de forma en el relevo (observación 12); siguen las palabras partidas de la bandeja (13) y la memoria no baja (14).**
   - **Builds y compuertas:**
     - `dev.apk`, 77,18 MB (`[AF.BuildDev] OK`): `check-manifest -Mode dev`, `check-16kb`, `check-no-unity-iap`, `check-no-applovin` y `check-no-unity-telemetry`, todas OK.
     - Ensayo, `[AF.BuildRelease] OK`: `rehearsal.aab` 60,02 MB y `rehearsal.apk` 65,53 MB. Las mismas compuertas más `check-release-classes` y `size-gate` (≤ 80 MB), todas OK.
   - **N6.1, la sonda (`dev.apk`):**

     | Arranque | `TotalTime` | `os_splash_held_ms` | `proc_os_splash_ms` | `os_splash_icon_px` / `_xy` | `morph_start_diam_px` / `_xy_px` | `morph_start_diam_dp` |
     |---|---|---|---|---|---|---|
     | Primera apertura | 356 | 896 | 1.150 (`proc_awake_ms` 786) | [504,504] / [288,918] | 504 / [540,1170] | 192,0 |
     | Frío 1 | 336 | 888 | 1.139 | [504,504] / [288,918] | 504 / [540,1170] | 192,0 |
     | Frío 2 | 334 | 898 | 1.149 | [504,504] / [288,918] | 504 / [540,1170] | 192,0 |
     | Frío 3 | 348 | 877 | 1.140 | [504,504] / [288,918] | 504 / [540,1170] | 192,0 |

     - En todos: `signal`, `intro`, `os_splash_veil` system y `morph_geometry` **measured**. La caja que mide el sistema y la de la intro coinciden al píxel: centro (540, 1170) en las dos.
     - `morph_start_diam_dp` sale 192 (504 px / 2,625), no los ≈ 185 que esperaba el encargo, pero coincide con lo que mide Android en este teléfono. `morph_units_per_dp` 0,875.
     - La intro: `anim_ms` 3.500, `motion_frames` 175-176, `motion_pauses` 0, `morph_ms` 701-709 y `morph_frames` 44.
   - **N6.1, las ráfagas** (`screencap` en bruto a resolución completa, cada ~120 ms; `n6c-primera`, `n6c-frio1..3`, `n6c-final` y `n6c-ensayo`). Se compara el último fotograma del sistema con el primero de la intro:
     - **El fondo cumple:** (20,68,74) exacto en los dos y en todos los fotogramas del relevo. Ya no hay el (22,69,73) de antes.
     - **El centro cumple:** (539,5, 1169,5) en los dos.
     - **El tamaño no cumple, por la forma:** Samsung recorta el icono del sistema con su forma de **cuadrado redondeado** (squircle), que mide 497 × 498 px en los ejes (x 291-787, y 921-1418). El primer fotograma de la intro es un **círculo** de 504 px, que en la captura mide 502-510 × 502-510 px (x 283-796 según la ráfaga). En los ejes hay 5-13 px más, y en las diagonales las esquinas del cuadrado redondeado desaparecen de golpe: a 45°, el punto que era icono pasa a ser petróleo (observación 12). El dibujo de dentro (la cuia, el río) cae en el mismo sitio y a la misma escala.
     - **Un solo icono:** no hay ningún fotograma con el icono a otro tamaño aparte de ese cambio de forma. Tampoco hay petróleo liso ni negro.
     - **El final de la intro:** con el perfil 18+, la intro con el nombre pasa en un corte limpio al Nivel 1 (`hold_ms` 112, `sloth_idle`). No se ve la pantalla de TiTino ni petróleo en el fotograma siguiente, así que no hay tono que comparar. En la primera apertura pasa al consentimiento de la edad.
     - **Ensayo de release, primera apertura:** `TotalTime` 168, lo mismo (cuadrado redondeado de 497 px y luego círculo de 502 px), sin petróleo liso.
     - **El dueño**, tras 4 arranques en frío, literal: «sí está mejor».
   - **El bloqueo (18.3, observación 9): cumple.**
     - Con el juego encima, la ventana lleva `fl=0x80810500`, sin `0x80000`, `0x200000` ni `0x400000`.
     - `AF_LOCK`, en cada reanudación: `show_when_locked`, `turn_screen_on` y `dismiss_keyguard` false, `activity_cleared` true, `found` 0x680000 y `cleared` 1.
     - **`dev.apk`, tres veces:**
       - nivel 1, con el botón lateral, 35 s;
       - con el botón, unos 30 s (el dueño);
       - con la pantalla apagada sola por el tiempo de espera (`pmSleepReason` 2, el dueño).

       Al encender sale **la pantalla de bloqueo** (`isKeyguardShowing=true`, foco en `NotificationShade`), y un toque donde estaba el tablero no vierte nada. Nunca sale `setOccluded(true)`. Al desbloquear, el juego sigue donde estaba, con su música (`audio_focus` gain).
     - **Ensayo:** la misma ventana `fl=0x80810500` y, tras 35 s bloqueado y encendida la pantalla, el bloqueo.
   - **La alarma y Spotify (observación 10): cumple.**
     - **Alarma** (el dueño): al sonar, `loss_transient`, `paused` true y `music_gain` 0,00; al apagarla, `gain` y 1,00. El dueño: «sonó la alarma y no sonó más la música hasta que la apagué». Hubo además un `duck` (0,20) de 3 s al crear la alarma.
     - **Con Spotify sonando:** en frío y volviendo de segundo plano, `yielded`, `music_gain` 0,00 y `paused` false. Spotify sigue, la melodía del juego no suena y los efectos sí (el dueño). Con Spotify en pausa, al volver de segundo plano: `gain` y la música del juego entra.
     - **Tras un anuncio con premio de prueba** (Revelar, cerrado con la X): `gain` y la música vuelve.
   - **TalkBack (el dueño; observación 7 y botones en gris): cumple.**
     - En la Liga del top 100: tras desplazar, tocar una fila lee la que se ve. Al perder el foco de vista, pasa a la primera fila visible y sigue en orden. En un desplazamiento rápido solo lee una fila, al parar.
     - En el 100 con 0 semillas, los botones de la bandeja en gris dicen «Te faltan N semillas» con su nombre.
   - **Las pestañas del Mercado (observación 5): cumple** (`b08/mercado-{es-419,en,pt-BR}-{100,130}.png`, `b08/pestanas-6.png`). El hueco mínimo entre los textos de dos pestañas es de 16,4 dp (es-419 al 130 %); al 100 %, de 19-32 dp. El encargo pide ≥ 12 dp. Al 130 % cada pestaña encoge su texto por su cuenta y la fila mezcla tamaños (observación 15).
   - **Las skins (observación 4): cumple** (`b08/cuias-141-46.png`, `b08/cuias-zoom.png`). Cada skin es su cuia sobre una ficha petróleo, con dos dosis:
     - el vidrio, con cuerda;
     - Santarém, con banda crema y pie oscuro;
     - el Encuentro, con banda bicolor;
     - la Totuma, con asa.

     Ninguna es un círculo. «Poner» en Santarém da `skin_selected` `af_skin_m02`, y el nivel 142 sale con esa cuia (`b08/skin-santarem-tablero.png`). El Cántaro sale ya con su tinaja.
   - **320 dp con la letra al 130 % (observación 8): el Mercado cumple, la bandeja del nivel no** (observación 13).
     - Montaje: `wm density 540` y `--ei af_font_scale 130`; la densidad vuelve a 420 al terminar.
     - **El Mercado** (`b16/mercado-320-3.png`): en es-419 y pt-BR las pestañas van en dos filas de dos; en inglés, en una («No / ads» en dos líneas, entre palabras).
     - **El nivel 100** (`b16/nivel-320-3.png`, `b16/nivel-320-detalle.png`): «Reiniciar» y «Restart» caben, y «Te faltan / 150» pasa a dos líneas entre palabras. Pero en la bandeja se parten «Deshace/r» (es-419), «Desfaze/r» y «Mistura/r» (pt-BR).
   - **Botones en gris sin TalkBack, en el 100 con 0 semillas:**
     - **«+1 cuia»:** sin tienda, `store_unavailable_notice` y `missing_tap` `tray_add_cuia` con `pulsed` true y `store` false; con la tienda falsa, el mismo `missing_tap` con `store` true y sin aviso. **Cumple.**
     - **Barajar en gris:** `shuffle` `denied` `no_balance`, sin `missing_tap`. El encargo no le pide latido.
     - **El Salto del Boto de la pausa y del banner (observación 2): sin comprobar.** Cinco «Rendirse» en el 46 no contaron como fallos (la pausa no enseña el Salto; seguramente falta confirmar), y Unity dejó de escribir en logcat dos veces. Queda para la próxima sesión.
   - **La memoria (19.0 con la sonda nueva, observación 6): `GL mtrack` no baja** (observación 14). En KB (`b19b/meminfo-*.txt`); la sonda `AF_PERF mem` en MiB:

     | Variante | Java Heap | Native Heap | Code | Graphics (GL mtrack) | Private Other | System | TOTAL PSS | TOTAL RSS | Sonda: `unity_reserved` / `unity_unused` / `mono_heap` / `gfx` / `tex_cur` / `audio` / `rt` |
     |---|---|---|---|---|---|---|---|---|---|
     | `dev.apk` 18+, nivel 75 | 20.976 | 36.824 | 101.940 | 139.904 (99.616) | 270.696 | 36.215 | 611.883 | 778.240 | 243,3 / 133,8 / 52,9 / 97,2 / 51,8 / 9,8 / 0,0 |
     | `dev.apk` 18+, nivel 200 | 20.800 | 36.428 | 101.920 | 139.740 (99.452) | 264.128 | 36.074 | 604.634 | 770.888 | 243,3 / 133,7 / 38,7 / 95,4 / 50,4 / 9,8 / 0,0 |
     | `dev.apk` 18+, inicio | 25.972 | 35.776 | 102.036 | 134.816 (94.528) | 272.100 | 36.044 | 612.240 | 778.332 | 243,3 / 134,8 / 52,6 / 113,8 / 48,5 / 9,8 / 0,0 |
     | `dev.apk` 18+, Mapa | 21.260 | 35.524 | 102.368 | 137.980 (97.692) | 280.120 | 37.166 | 619.786 | 787.596 | 259,4 / 143,7 / 52,6 / 83,8 / 37,7 / 10,1 / — |
     | `dev.apk` <13 (bloqueado) | 13.588 | 31.620 | 83.772 | 129.064 (88.776) | 254.580 | 20.618 | 536.610 | 664.052 | 227,3 / — / 54,5 / 94,0 / 49,6 / — / — (`at` boot) |
     | `rehearsal.apk` 18+, nivel 1 | 12.280 | 37.212 | 82.680 | 137.112 (96.824) | 246.524 | 24.203 | 544.603 | 713.928 | sin sonda |

     - **`GL mtrack` queda igual que en la jornada 6** (de 88,8 a 99,6 MB, frente a 90,8-97,8 MB). El escritorio esperaba de 10 a 20 MB menos con el Blit Type en Auto.
     - **El PSS del `dev.apk` 18+ sube unos 50 MB** (605-620 MB, frente a 554 MB), sobre todo por `System` (36 MB, frente a 19) y `Code` (102 MB, frente a 90). El de <13 y el del ensayo bajan unos 20 MB.
     - **Todas las variantes pasan de los 450 MB de PSS de D-060.** Esa cifra es para un teléfono de clase A (3 GB); este es de 12 GB, así que aquí sirve de referencia.
     - `rt_mib` 0,0 con 2 `rt_top` diminutos: no hay render textures grandes. `unity_unused_mib` (~134 MB reservados por Unity sin usar) es la mayor parte del `Unknown`.
   - **La imagen con el Blit en Auto: cumple.**
     - Revisado: el arranque con la intro, el inicio, niveles con vertidos, la pausa, un anuncio de prueba a pantalla completa (`ui/anuncio-revelar.png`) y la vuelta de segundo plano. Nada de parpadeos, franjas negras ni imagen estirada.
     - **El Mapa en reposo:** 12 capturas seguidas, idénticas; tras desplazar, otras 12 también idénticas. Ningún fotograma viejo se alterna con el actual.
     - **Los bordes:** el fondo llega a los bordes y a la zona de la cámara.
   - **Sin hacer en esta tanda:**
     - el Salto del Boto (arriba);
     - «+3 Deshacer», «Llenar canoas» y «Continuar: +1 cuia» con este build;
     - Reducir movimiento;
     - Inicio durante el icono;
     - en frío desde una notificación;
     - los bloques 5, 9, 10-11, 12.2, 13, 14, el resto de 18 y 19, 21, 22 y 23.

11. **Con el mismo build (`d3e601a2`): los botones en gris, el bloque 14 y el 13. Cumplen; la observación 2 queda arreglada.**
   - **El Salto del Boto en gris (observación 2): cumple.**
     - Cómo se llega: cinco fallos seguidos en el 47 con 0 semillas, con Pausa › Salir › Salir (cada uno da `fail_hard_canoe`).
     - Después se relanza sin `af_level` y con `--ei af_canoes 5`, y se toca Reintentar. `fails_level` se conserva (5) y las canoas vuelven a 5.
     - En ese sexto intento sale el banner «Rosa puede llevarte río arriba · 300 / Te faltan 300 semillas» (`ui/sexto47-entrada.png`), y la pausa enseña «Salto del Boto · 300» en gris (`ui/pausa-salto-latido.png`).
     - **Sin tienda:** tocar el banner da `store_unavailable_notice` y `missing_tap` `banner_skip` (`pulsed` true, `store` false); tocar el Salto en la pausa, lo mismo con `pause_skip`. Arriba sale «Las compras no están disponibles ahora». No se abre ninguna confirmación ni se cobra.
     - **Con la tienda falsa:** los dos dan `missing_tap` con `store` true y sin aviso.
     - **Por qué no salió antes** (punto 10): «Rendirse» pide confirmar («¿Seguro?», `giveup_confirm`) y sin confirmarlo no cuenta. Además, el jugador que se atasca ganó el 46 en el cuarto intento, con el alivio, y en un nivel ya superado el Salto se oculta (`LevelFlow.SkipOffer`: `LevelNumber <= LoadMaxLevel()`). No es un defecto.
   - **Los otros botones en gris, sin tienda: cumplen.**

     | Botón | Dónde | Resultado |
     |---|---|---|
     | «Llenar canoas» | Sin canoas, desde el nivel (Reintentar con 0 canoas) | `missing_tap` `refill_canoes` con el aviso |
     | «Llenar canoas» | Sin canoas, desde el contador del inicio (0/5) | lo mismo, con `level` 0 |
     | «Continuar: +1 cuia» | Aguas estancadas, en el 48 | `missing_tap` `rescue` y el aviso |
     | «+3 Deshacer · 30» (`refill_seeds`) | Aguas estancadas, en el 48, con `--es af_ads_fill no_fill` y las 3 Mareas gastadas | `missing_tap` `refill_seeds` y el aviso |

     En ninguno se cobra nada ni cambia el saldo.
   - **Bloque 14.1, avisos con `--ei af_dev_push 2`: cumple, salvo tocarlo** (queda para el dueño).
     - Al lanzar con el extra sale el diálogo del sistema «Allow Tintalú (dev) to send you notifications?». Se pulsó Allow; el permiso estaba sin conceder por el «Ahora no» del principio. Con el permiso concedido, `push_plan` pasa de `allowed` false a true, con 18 avisos.
     - El primero llega 1-2 min después de pasar a segundo plano, dentro de la ventana inexacta de la alarma (`RTC_WAKEUP` con ventana de 45 s, como deben ser las alarmas inexactas). Lleva el icono de la app y el canal `af_default`.
     - Los textos, con título «TiTino» en los tres idiomas:
       - es-419: «El Reto del río de hoy ya está listo. TiTino te espera en su rama.»;
       - en: «Today's River Challenge is ready. TiTino is waiting on his branch.»;
       - pt-BR: «O Desafio do Rio de hoje já está pronto. TiTino espera por você no galho dele.».
     - Los tres textos son los del catálogo de 06 §8.3, letra por letra.
     - **Al tocarlo (el dueño, con un aviso nuevo de las 13:16; el primero se canceló al abrir la app, como debe):** `push_sent` y `push_open` con `type` `daily_ready`. El juego abre **en el inicio con la Bitácora encima**, el takeover de prioridad 1, y la tarjeta del Reto queda debajo (observación 16).
   - **Bloque 13, Ajustes (lo hecho): cumple.**
     - **Privacidad:** con la analítica encendida en Privacidad y datos, los eventos salen como `AF_EVENT` (`af_consent`, `af_settings_change`, `level_start`). Al apagarla vuelven a `AF_EVENT_UNSENT`. Queda apagada, como estaba.
     - **Corregir mi edad, la confirmación neutral:**
       - «Menos de 13» y «De 13 a 17» enseñan la misma pantalla: «Corregir mi edad», «Elegiste: {banda}. La edad solo se puede corregir una vez y no se puede deshacer.», «Volver a elegir» y «Confirmar» (`b13/edad-menos13.png`, `b13/edad-13-17.png`).
       - «Volver a elegir» vuelve a la pantalla de edad.
       - La X cierra sin cambiar nada: la banda sigue en 18+ y la fila sigue en Ajustes.
       - Elegir la banda guardada (18 o más) cierra sin confirmación y sin gastar la corrección.
       - No se confirmó ninguna banda.
       - Al volver a Ajustes, la lista salta al principio: no es un defecto, pero se nota.
     - **Toque con la lista en movimiento:** lanzada la lista de Ajustes (60-80 ms), un toque sobre una fila mientras se mueve no la activa: ni cambio de selección ni `settings_change`. Con la lista parada, un toque sobre «Siempre» o «Mensajes de TiTino» sí la cambia. Se dejaron como estaban.
     - **Código de cuenta:** sale en grupos con guiones; no se copia aquí. Se borraron las capturas que lo enseñaban.
     - **Eliminar cuenta y datos, sin borrar:**
       - el primer paso avisa de lo que se borra («Las compras no se reembolsan»), con «Conservar mis datos» y «Continuar»;
       - el segundo, «¿Eliminar todo?», lleva la nota de Play Juegos («Pedimos borrar tu copia en Google Play Juegos…») y el botón «Eliminar»;
       - se cerró con la X. La espera visible solo se ve borrando de verdad (bloque 21).
   - **Sin hacer todavía:**
     - las demás filas de Ajustes, una a una (Sonido, Música, Vibración, Reducir movimiento, Alto contraste, Avisos bloqueados desde el teléfono, Datos del río);
     - los idiomas recorriendo todas las pantallas;
     - 14.2 y 14.3;
     - los bloques 5, 9, 10-11, 12.2, el resto de 18 y 19, 21, 22 y 23 (lo hecho de 5, 9, 10 y 11, en el punto 12).

12. **Con el mismo build: bloques 5, 9, 10 y 11 (lo hecho). Cumplen; los banners de mundo repiten dibujo (observación 17).**
   - **Bloque 5, los estrenos de mecánicas** (`b05/hoja-estrenos.png`). Al entrar en cada estreno sale su tarjeta de una pantalla, con el icono y una frase que se entiende sin el GDD: 43 Hoja, 63 Matapi, 83 Liana con fruta, 100 Aguas turbias con la Luciérnaga (en la tarjeta del Muy difícil), 103 Remolino, 123 Barro, 143 Lluvia, 163 Brote, 183 Piraña (con la mini escena del mordisco), 243 Liana de cuenta, 283 Hoja atada, 343 Brote de dos hojas y 403 Flor de vitória-régia.
     - **El 323 (Tinaja):** sale como nivel normal, sin tarjeta ni mecánica (mundo 7, agua blanca). Es lo que dice la variante B de D-019.
     - **`--es af_mech_off hoja` en el 43:** el registro dice «mech_enabled apagadas: mech_enabled_hoja… 0 niveles con override y 113 sin él (se sirven publicados, con aviso al cargarlos)». El 43 sale con su Hoja y su tarjeta, que es lo que pide el guion.
     - **Sin hacer:** jugar cada estreno a mano y con el jugador automático, los niveles de después, y los contadores del 194 y el 200 con en-XA.
   - **Bloque 9, los vídeos con premio: 4 de 5 cumplen.** En cada uno salen `af_ad_request` `filled`, `af_ad_shown`, `af_ad_reward` e `ad_impression` (AdMob de prueba), con el premio una sola vez. Al cerrar el anuncio, la música vuelve (`audio_focus` gain).
     - **Morpho** (Pista con «+1 ▶», en el 43, `rv_hint`): la pista sale al cerrar.
     - **Luciérnaga** (Revelar con «+1 ▶», en el 100, `rv_reveal`, punto 10): una de stock.
     - **Canoa extra** (Sin canoas tras Reintentar con 0, `rv_extra_canoe`): las canoas pasan de 0 a 1.
     - **Cosecha doble** (victoria del 43, `rv_double_harvest`): de 20 a 40 semillas, `doubled` true. Después pasa solo al 44, y el intersticial queda bloqueado por el calentamiento (`blocked:Warmup`).
     - **La recarga de Marea con vídeo, sin hacer:** el jugador que se atasca no se atascó en el 44 (no conoce la regla de la Hoja) ni en el 42 (lo ganó). El botón en gris sin vídeo ya cumple (punto 11).
     - Sin hacer: cerrar el vídeo antes de tiempo, el teléfono en silencio, la zona segura, los intersticiales, el tope de 15 y `af_ads_fill 0`.
   - **Bloque 10.1, la Bitácora:**
     - con un nivel ganado hoy, tocar la página 1 enseña «Elige tu premio»: +60, Marea ×2, o +30 con Marea ×1;
     - elegido +60, las semillas pasan de 60 a 120, y sale «Página sellada. La siguiente, mañana» con el sello en la casilla 1 (`b09/bitacora1.png`, `b09/bitacora2.png`);
     - antes de ganar nada hoy, tocar la página no hacía nada;
     - hasta sellarla, la Bitácora sale como takeover en cada arranque.
   - **Bloque 11.3, los banners de mundo** (`b11/hoja-entrada.png`), en el 11, 41, 81, 141, 201, 301, 421 y 561. Cada banner lleva su mundo, «Mundo N: {lugar}», la pregunta del mundo, el país y el río, y «Ver mapa». El texto está completo en es-419. Dos cosas (observación 17):
     - el dibujo, Capi con Rosa en la orilla, **es el mismo en los mundos 3 a 9**; solo cambian la tira del mapa y el agua;
     - el del mundo 2 (nivel 11) no lleva la tira del mapa ni «Ver mapa».
   - **Bloque 11.6, los fondos:** los tableros del 11 al 561 (`b11/n*-tablero.png`) tienen fondo, ninguno rosa, negro ni vacío. La línea de la fruta sale arriba en 41, 81, 141, 201 y 301.
   - **Sin hacer:**
     - que el banner salga solo en el primer intento;
     - 11.1, 11.2, 11.4 y 11.5;
     - una captura por mundo con su rótulo;
     - 10.2-10.4.

13. **Con el mismo build: lo que quedaba automatizable. Cumple, salvo el botón del Mapa en el 700 (observación 18).**
   - **La recarga de Marea con vídeo** (`rv_undo_refill`, en el 48): se gastan las 3 Mareas deshaciendo y rehaciendo el último vertido. Sale «recarga con vídeo» junto a «+3 · semillas». Dan `af_ad_request` `filled`, `af_ad_shown` y `af_ad_reward`, y la Marea vuelve a 3. **Con esto, los cinco vídeos con premio cumplen.**
   - **Sin relleno** (`--es af_ads_fill no_fill`; el guion dice `--ei af_ads_fill 0`, pero el extra es texto): en el 100, el «+1 ▶» de Revelar desaparece y queda su precio en semillas, sin botones de vídeo vacíos (`b09/bandejas-s.png`). En Aguas estancadas solo queda «+3 · 30 semillas» (punto 11).
   - **Los intersticiales** (del 61 al 90 en una sesión con el jugador automático, `b09/total/`):
     - el calentamiento los bloquea hasta el 65 (`blocked:Warmup`, 120 s);
     - después salen en el 66, 73, 80 y 87 (`allowed`), y entre medias `blocked:MinGap`. A ~21 s por nivel, la separación de 150 s manda sobre el «cada 2 niveles» del 61 (08 §9);
     - se vieron 4, con `af_ad_request` y `af_ad_shown`, sin ninguna excepción;
     - sin probar: el tope de 6 por sesión, el de 12 al día y el de 15 vídeos al día (hace falta una sesión más larga), y que no salgan en el Reto, en un reintento ni tras rendirse.
   - **El banner de mundo solo en el primer intento:** en el 141 sale al entrar; tras Salir y Reintentar ya no sale (`b11/141-reintento.png`).
   - **Contadores de Lluvia (194) y Piraña (200) junto a Reiniciar** (`b05/contadores-zoom.png`):
     - en en-XA, al 100 % y al 130 %, «[Réíñíçíár …]» va en dos líneas y el contador queda a su izquierda, sin tocarlo;
     - en es-419, debajo, a la derecha;
     - `hud_label`: `counter` lluvia (3) y piraña (4).
   - **Sonido, Música y Vibración:** al apagarlos salen tres `settings_change` (false). Al reabrir la app siguen apagados. Se volvieron a encender (`b13/s0..s2`).
   - **en-XA** (`xa/xa-hoja.png`): inicio, cuatro pantallas de Ajustes, Mapa y las cuatro pestañas del Mercado con la tienda falsa. Todo entre corchetes, nada vacío ni recortado. Los precios de la tienda («COP 5,900») van sin corchetes, como deben.
   - **El Mapa con `af_max_level`** 25, 58, 140, 200, 400 y 700 (`b11/mapas-hoja.png`):
     - `map_open` y, desde el 58, `stop_arrive` con el viaje hasta la parada (50, 140, 200 y 400);
     - cabecera con el mundo y su pregunta, el Muestrario (6/73 a 73/73), Mundo y Cuenca, y «Jugar · Nivel N»;
     - **en el 700, el Mapa dice «Jugar · Nivel 701»** (observación 18). El inicio dice bien «Jugar · Reto del río», y el botón del Mapa abre el Reto (`d3_n0301`).
   - **Bloque 19, los arranques y el tamaño:**
     - tres en frío con el perfil 18+: `TotalTime` 312-332 ms, `proc_awake_ms` ~600, `proc_visible_ms` ~930-950 y `splash_end_ms` ~3.870 (intro y carga);
     - datos de la app 23 MB (`run-as du`: `no_backup` 8,8 MB, `cache` 6,9 MB y el resto < 0,5 MB); el `dev.apk`, 77 MB.
   - **Sin hacer todavía:**
     - la sesión de 60 min, la batería y la temperatura (19);
     - el resto de 18;
     - 12.2 (la Liga contra el backend de desarrollo);
     - 10.2-10.4, 11.4 y 11.5;
     - cerrar un vídeo antes de tiempo, el silencio y la zona segura;
     - con el dueño: 21, 22 y 23.

14. **El build de los arreglos de la segunda hoja j6** (`ff3c49c0`, que trae `1a6abaf8`, el encargo, y los carriles `fx-j6b-icono-forma`, `fx-j6b-bandeja`, `fx-j6b-memoria` y `fx-j6b-aviso-destino`): **cumplen la forma del icono (12) y la bandeja (13); el aviso no abre el Reto (observación 19); la memoria sigue por encima de D-060 y E1 es el experimento que más baja (20).**
   - **Builds y compuertas:**
     - `dev.apk`, 74,96 MB (`[AF.BuildDev] OK`);
     - ensayo, `[AF.BuildRelease] OK`, `rehearsal.aab` 57,91 MB y `rehearsal.apk` 63,42 MB;
     - `check-manifest -Mode dev`, `check-16kb`, `check-no-unity-iap`, `check-no-applovin` y `check-no-unity-telemetry` en los dos, y `check-release-classes` en el ensayo: todas OK;
     - `size-gate` del AAB: OK, en el punto 15.
   - **N6.1, la forma del icono (observación 12): cumple.**
     - **La sonda** (primera apertura): `morph_shape` squircle, `morph_shape_n` 2,67, `morph_shape_px` [493,9, 538,8], `morph_shape_fit_px` 1,2, `os_splash_mask_ms` 0,97, `signal`, `intro`, `system` y `held` 1.121 ms. En frío, `held` 840-1.014 ms.
     - **Las ráfagas** (`n6d-*`, en bruto; ancho en la fila 1170, alto en la columna 540, y alcance en las cuatro diagonales desde el centro):
       - el último fotograma del sistema mide 496 × 498 px, con diagonales de 190-191 px;
       - el primero de la intro, 498 × 500 px, con diagonales de 190-192 px. Coinciden dentro de ±2 px en los ejes y en las diagonales;
       - después el eje crece (508-524, 596-690, ~1.000) y las diagonales se quedan en 190-191 hasta que el círculo las pasa: **el borde no retrocede**;
       - igual en la primera apertura y en los tres arranques en frío.
     - **El dueño**, tras 4 arranques en frío, literal: «está bien».
     - **Con Reducir movimiento** (el dueño activó «Reduce animations» y luego lo quitó): `reduce_motion` true, `morph_mode` fade, `morph_ms` -1, `motion_frames` 0 y `signal`. Sale el icono de 496-498 px y luego la escena quieta, sin saltos.
     - **Inicio durante el icono:** pulsado Inicio a los 0,5 s y vuelta a los 5 s, la intro termina normal (`anim_ms` 3.500, `cut` false), sin `E/Unity` ni el icono colgado. El velo lo soltó el tope (`cap`, 3.000 ms) porque la app estaba en segundo plano; no es un cuelgue.
     - **El ensayo:** cumple, en el punto 15.
   - **La bandeja a 320 dp con la letra al 130 % (observación 13): cumple** (`b16/bandeja-320b.png`).
     - En el 100, en es-419, en y pt-BR, ya no se parte ninguna palabra: «Deshacer», «Desfazer», «Misturar» y «Barajar» salen enteras, más pequeñas que «Pista» o «Revelar».
     - El dueño, sobre la bandeja con letras de distinto tamaño: «sí está bien».
     - 411 dp y las pestañas del Mercado a un solo tamaño (observación 15): cumplen, en el punto 15.
   - **El aviso del Reto (D-062, observación 16): no cumple** (observación 19). Tres casos, con el dueño tocando el aviso:
     - **En segundo plano dentro de un nivel** (el 100): sigue en el nivel; `push_open` con `type` `daily_ready` y `dest` `none`, que es lo que dice el guion;
     - **En segundo plano en el inicio:** el toque llega (`onNewIntent`, y el aviso se quita), pero no sale `push_open`: el juego vuelve al inicio con la Bitácora encima;
     - **En frío**, cerrado desde Recientes: arranca desde el aviso (`START … (has extras)` desde el propio paquete), con `push_sent` y sin `push_open`, y abre el inicio con la Bitácora;
     - en los tres, el permiso de avisos se volvió a conceder en el diálogo del sistema, porque la primera apertura (`pm clear`) lo había quitado.
   - **La memoria con los experimentos (observación 14), `b19c/`.** Preparación de cada medida: `cmd package compile -m speed-profile -f`, `force-stop`, lanzar con `--ei af_mem_os 1` y un minuto quieto. La sonda del arranque da `gfx_api` Vulkan, `gfx_device` Adreno (TM) 830, y `screen_px` y `render_px` 1080 × 2340. En E2 y E3, `unity_args` con los argumentos completos.

     **Nivel 75** (`dumpsys meminfo -d`, en KB; `EGL mtrack` es 40.288 en todos):

     | Experimento | Java Heap | Native Heap | Code | Graphics (GL mtrack) | Private Other | System | TOTAL PSS | Sonda `os_pss_mib` / `os_graphics_mib` / `gfx_mib` |
     |---|---|---|---|---|---|---|---|---|
     | E0, la base | 36.660 | 42.508 | 88.800 | 136.964 (96.676) | 263.344 | 37.708 | **610.628** | 503,1 / 140,8 / 94,4 |
     | E1, `-force-gles` | 20.324 | 36.888 | 100.192 | 124.932 (84.644) | 234.952 | 37.750 | **559.762** | 491,4 / 137,2 / 49,9 |
     | E2, bloques de 4 MiB | 20.820 | 40.068 | 95.564 | 136.964 (96.676) | 256.140 | 37.787 | **592.051** | 501,2 / 140,8 / 94,4 |
     | E3, E2 más los temporales | 19.816 | 40.044 | 89.676 | 136.964 (96.676) | 259.072 | 36.780 | **586.976** | 500,4 / 140,8 / — |

     - **E0, el inicio:** PSS 598.183 y `GL mtrack` 91.972, unos 3-5 MB por debajo de la jornada 6, como se esperaba al quitar el posproceso.
     - **El ensayo de release, nivel 75** (retomado, sin sonda): E0 **530.027** (`GL mtrack` 93.756, Code 79.272, Private Other 225.788) y E1 **494.404** (`GL mtrack` 75.804, Code 80.100, Private Other 215.132). Con `-force-gles` la imagen se ve igual (`b19c/ensayo-E1.png`).
     - **Lo que dicen:**
       - E1 (OpenGL ES) es el que más baja: −51 MB en el `dev.apk` y −36 MB en el ensayo, por `GL mtrack` (−12 a −18 MB), Private Other y Graphics;
       - E2 y E3 bajan 19-24 MB, casi todo en Private Other, dentro del ruido entre arranques;
       - nada baja de los 450 MB de D-060 en este teléfono (observación 20).
     - **Medidas que no valen:** en E1-E3, la del «inicio» y la del «Mapa» se hicieron en el nivel 75. Al relanzar, el juego retoma el nivel a medias del experimento anterior; lo dice la sonda (`at` level). Se repitieron en el punto 15. Tampoco hay `AF_PERF frames` con `af_bench_morpho` en E1.
     - **La imagen sin posproceso:** el arranque, el inicio, los niveles y la victoria se ven como antes.

15. **Con el mismo build (`ff3c49c0`): lo que quedaba del punto 14 y lo automatizable de los bloques 9, 12 y 19. Cumple; la observación 15 queda arreglada.**
   - **`size-gate` del `rehearsal.aab`: OK**, 57,91 MB de 80 (sin bundletool, solo el tamaño del archivo).
   - **N6.1 en el ensayo: cumple** (`n6r-cold`, `n6r-cold2`, `n6r-cold3`; tres arranques en frío en bruto). El último fotograma del sistema mide 496 × 498 px con diagonales de 190-191 px; el primero de la intro, 498 × 498 y 500 × 500, con diagonales de 190-192. Después el eje crece (505, 652, 756, 818…) y el borde no retrocede. `TotalTime` 125-237 ms. El ensayo no escribe las sondas `AF_`.
   - **La memoria del inicio y del Mapa, repetida (`b19d/`).** Antes se sacó del nivel (Pausa › Salir) y se selló la Bitácora de hoy para que no tapara el inicio. Los primeros intentos no valen y se guardaron aparte (`b19d-invalido/`): la Bitácora tapaba el inicio y el toque al Mapa no llegaba. La sonda confirma `at` lobby y `at` map.

     | Experimento | Inicio, TOTAL PSS (`GL mtrack`) | Mapa, TOTAL PSS (`GL mtrack`) | Sonda del Mapa, `os_pss_mib` / `gfx_mib` |
     |---|---|---|---|
     | E0, la base | 587.557 (89.752) | 588.470 (92.232) | 564,3 / 79,2 |
     | E1, `-force-gles` | **566.889** (79.928) | **575.606** (81.984) | 545,1 / 34,6 |
     | E2, bloques de 4 MiB | 593.420 (89.752) | 592.729 (92.232) | 569,8 / 79,2 |
     | E3, E2 más los temporales | 592.166 (89.752) | 592.038 (92.360) | 568,4 / 79,2 |

     E1 vuelve a ser el único que baja: 13-21 MB, de ellos ~10 MB de `GL mtrack`. E2 y E3 no cambian nada. Ninguno baja de 450 MB (observación 20).
   - **Las pestañas del Mercado (observación 15): cumple** (`merc/pestanas-411-320.png`, `merc/mercado-*.png`). Al 130 %, a 411 dp y a 320 dp (`wm density` 540 y vuelta a 420), en es-419, en y pt-BR, las cuatro pestañas salen con el mismo tamaño de letra en cada fila. A 411 dp, «Sin anuncios» y «Sem anúncios» van en dos líneas. A 320 dp, es-419 y pt-BR van en dos filas de dos, y en inglés en una.
   - **La bandeja a 411 dp con la letra al 130 %: cumple** (`merc/bandeja-411.png`, nivel 62). «Deshacer», «Undo» y «Desfazer», «+1 cuia», «Pista», «Hint» y «Dica», «Barajar», «Mix» y «Misturar» salen enteras.
   - **Bloque 9.1, cerrar el vídeo antes de tiempo y la zona segura** (`b09b/`, la Pista del 63 sin semillas, `rv_hint`):
     - **el anuncio de prueba de Google no deja cerrarlo antes de tiempo:** no hay X hasta «Reward granted», y Atrás al primer segundo no hace nada. A los ~6 s, Atrás sí cierra, pero el premio ya se había dado (`af_ad_reward` `provisional`). Así que «cerrar antes de tiempo: sin premio» no se puede probar con el simulado;
     - **el premio llega una sola vez** en los dos intentos (`af_ad_request` `filled`, `af_ad_shown` y `af_ad_reward`);
     - **zona segura:** el rótulo «Test Ad» y la X de «Reward granted» quedan bajo la barra de estado, a la derecha y lejos del recorte de la cámara, e «Install» lejos de la barra de gestos. Una notificación emergente de otra app tapó la X unos segundos (es del sistema, no del juego; la captura se borró).
     - Las líneas salen como `AF_EVENT_UNSENT`, porque el perfil de 18+ se rehízo rechazando el consentimiento (punto 14).
   - **Bloque 9.2, nunca en el Reto ni al reintentar: cumple** (`b09b/inter_retry_logcat.txt`, `lc-reto.txt`, `win_next_logcat.txt`).
     - En el 66, pasado el calentamiento (175 s), salir (fallo) y «Reintentar» no consultan el intersticial: no hay `ad_check` ni anuncio;
     - **ganar el Reto de hoy** (`d2_n0301`, resuelto con el solver: 19 vertidos) tampoco: `daily_challenge` `win` sin `ad_check`;
     - **de control**, ganar el 66 y «Siguiente» da `ad_check` `blocked:Cadence`, y ganar el 67, `allowed` y el anuncio simulado «interstitial_post_level · intersticial» con «Cerrar».
   - **Bloque 12.1, el Reto de hoy:** abierto desde la tarjeta del inicio; tras 3 vertidos, a segundo plano y vuelta, mismo tablero; ganado, sella el 29 en el calendario con «+60» y «Cosecha doble: x2 por video». La racha pasa a 1 (`streak_days` 1).
   - **La Bitácora de hoy:** la página 1 se selló con «+60» (`calendar_claim`), y desde entonces el inicio abre sin el takeover.
   - **Bloque 19.1, sesión larga: cumple** (`b19e/`). 60 minutos seguidos con el jugador automático, del 68 al 216 (149 niveles), con Marea, Morpho, Luciérnaga, Cuia extra, Volver a la orilla y Cosecha doble por turnos, y 0 `E/Unity` ni excepciones. Cada 15 minutos:

     | Minuto | TOTAL PSS (KB) | Batería | Temperatura | Estado térmico |
     |---|---|---|---|---|
     | 0 | 524.468 | 87 % | 35,2 °C | 0 |
     | 15 | 583.409 | 85 % | 33,4 °C | 0 |
     | 30 | 599.367 | 83 % | 32,4 °C | 0 |
     | 45 | 593.545 | 81 % | 31,9 °C | 0 |
     | 60 | 609.868 | 79 % | 31,5 °C | 0 |

     - El PSS sube unos 85 MB en la primera media hora y luego se queda en 590-610 MB: no crece sin parar. En el 75, 100… 200, el jugador da 540-611 MB.
     - **8 % de batería en una hora** y el teléfono se enfría (no estaba cargando). `dumpsys gfxinfo` no da datos con Unity.
   - **Bloque 19.2, `AF_PERF`:** en 2.979 vertidos, `save_ms` da mediana 0,62 ms (p95 3,74, máximo 13,32) y `bg_ms`, mediana 3,84 ms (p95 6,32, máximo 15,42). El Morpho (14 veces) espera 16-17 ms, frente a un tope de 150. En la sesión, el arranque da `visible_ms` 304 y `os_splash_held_ms` 649.
   - **Bloque 9.2 y 9.3, los topes de los intersticiales: cumplen.**
     - **De sesión (6):** en la sesión larga salen 6 (`allowed`), separados por `blocked:MinGap`, y después 106 veces `blocked:SessionCap`. Además, 5 `blocked:Warmup` al principio y 2 `blocked:AfterRewarded`.
     - **Del día (12):** el perfil llevaba 10 (el `pm clear` del punto 14 borró los 4 de la tarde). En dos sesiones más salen 3 y 2, y desde el 251, `blocked:DailyCap` (`b09c/`, `b09d/`).
     - En el 250, en lugar del intersticial, sale la oferta de «Sin anuncios» (`af_iap_offer_shown`, `context` `noads_prompt`).
     - Sin probar: el tope de 15 vídeos al día. Con el anuncio de prueba, cada vídeo tarda ~30 s y cada sitio tiene además su propio tope.
   - **Bloques 10 y 11, de la sesión larga** (`AF_STATE`):
     - **Canastas:** se abren en el 70, 80… 250, con 50 semillas y un comodín. En el 100 y el 200 sale la grande: 150 semillas, Marea, Morpho y Cuia extra.
     - **Crecida:** sus hitos son el 3 (300 semillas), el 6 (cofre con 50 semillas y Morpho) y el 10 (Cuia extra y río crecido). Después, cosecha doble gratis en cada victoria.
     - **Skin de hito:** al ganar el 140, `af_skin_m04` (`world_milestone`) se pone sola.
     - **Frutas nuevas** (`fruit_new`): cupuaçu en el 81, lulo en el 111, jenipapo en el 141 y cacao en el 201.
     - **Banners de mundo:** se cruzaron 3, todos en el primer intento.
   - **Bloques 18.1 y 18.2, segundo plano y matar la app** (`b18/`, nivel 260): **cumplen.**
     - **Inicio justo después de tocar el destino:** al volver, el vertido está hecho (`pours` 3) y el tablero sigue igual.
     - **`force-stop` a mitad de un vertido:** al reabrir, `profile_integrity` `valid` y el mismo nivel con los 4 vertidos (`load` y `ready` con `pours` 4).
     - **Inicio en la victoria:** al volver sigue la victoria, con «+160» y el saldo de 13.040.
     - **`force-stop` en la victoria:** reabre en el inicio del 261 con 13.040 semillas. El premio se cobra una sola vez.
     - **`force-stop` un segundo después de lanzar (en la carga):** reabre bien y `valid`.
     - Al volver con `am start` sin el extra `af_lang`, el juego pasa al idioma del teléfono (inglés). Es por el extra de desarrollo, no un defecto.
   - **Bloque 18.7, Atrás en cada pantalla** (`b18/atras.png`): **cumple, salvo la observación 21.**
     - Desde Ajustes, el Mapa, el Mercado y Avatar, vuelve al inicio.
     - **En el nivel, la pausa, «¿Salir del nivel?» y Nivel fallido no hace nada.** Tampoco en el inicio: no sale de la app ni pide confirmación.
     - Atrás doble y rápido no da ninguna excepción (0 `E/Unity`).
   - **Bloque 13, las filas que faltaban: cumplen** (`b13/`).
     - **«Reducir movimiento» y «Alto contraste»:** se encienden y se apagan al tocarlas (`settings_change`) y se mantienen al reabrir.
     - **El efecto del alto contraste** en el 262 (`b13/hc-comparado.png`): el patrón pasa de 0,2 a 0,35 (`patterns.opacity`) y las frutas se distinguen mejor.
     - **«Datos del río»:** la fila del açaí abre «Frutas del río» (`fact_page_open`, `from` settings) con su nombre científico, 10 datos numerados y, al final, las fuentes con su dominio (Embrapa, Mongabay, ter Steege et al.…). Más abajo en Ajustes, «Sobre la Amazonía» enseña los datos del río con su «Fuente:».
   - **Bloque 13.3, idiomas** (`b13/idiomas-{es-419,en,pt-BR}.png`): **cumple, salvo la observación 22.** En los tres idiomas se recorrieron el inicio, Ajustes (3 pantallas), el Mapa, el Mercado con la tienda falsa, el calendario del Reto, la Bitácora, el nivel, la pausa, «¿Salir del nivel?» y Nivel fallido. Ningún texto vacío, cortado ni en otro idioma.
   - **Bloque 14.3 y la Liga con <13: cumplen** (`b14/`). Tras `pm clear`, con `--ez af_league true --ei af_dev_push 2`, al elegir «Menos de 13» sale «Pide a un adulto que instale el juego» (`consent` `blocked`, país US por la SIM). Tras 75 s en segundo plano, 0 avisos del paquete, ningún diálogo de permiso y 0 peticiones a `/v1/league`.
   - **Bloque 12.2, la Liga contra el backend de dev: no se puede probar.** Con `--ez af_league true`, el botón «Liga» sale y la Liga se abre (`af_league_open`), pero responde `league_unavailable` `offline` con la red bien. En logcat aparece `Firebase.FirebaseException … code: 403 body: App attestation failed`.
     - Cada `pm clear` crea un secreto de depuración de App Check nuevo, y ese no está en la lista de Firebase.
     - El portátil ni lee ni guarda ese secreto.
     - Lo tiene que registrar el dueño en la consola de App Check del proyecto de dev, con el perfil que quede. Después se repite el guion de la Liga, salvo el modo avión, que lo hace el dueño.
   - **Bloque 14.2, el aviso real al día siguiente:** sin probar. El perfil se rehízo (18+, consentimiento rechazado, progreso hasta el 262) y aún no tiene el permiso de avisos.
   - **El teléfono queda así:** el `dev.apk` instalado con un perfil 18+ en el inicio, y el progreso hasta el 262 puesto con `af_max_level`. Densidad 420, letra 1,0, animaciones encendidas y TalkBack apagado.

16. **El build de los arreglos de la tercera tanda j6** (`da40034d`, que trae `57b93c10`, el encargo, y los carriles `fx-j6c-mapa-701`, `fx-j6c-aviso-abre` y `fx-j6c-gles`): **cumplen el Mapa (18) y el aviso (19). OpenGL ES 3 rinde como Vulkan y gasta 10-30 MB menos, pero no baja de 450 MB (20).**
   - **Builds y compuertas:**
     - builds: `dev.apk` de 74,98 MB (`[AF.BuildDev] OK`), y el ensayo con `[AF.BuildRelease] OK` y `[AF.LocReleaseGate] OK`: `rehearsal.aab` de 57,92 MB y `rehearsal.apk` de 63,43 MB;
     - en los tres: `check-manifest -Mode dev`, `check-16kb`, `check-no-unity-iap`, `check-no-applovin` y `check-no-unity-telemetry`, todas OK;
     - en el ensayo: `check-release-classes` OK, y `size-gate` del AAB OK.
   - **El Mapa con el contenido acabado (observación 18): cumple** (`c/mapa-700.png`, `--ei af_max_level 700`). El botón del Mapa y el del inicio dicen «Jugar · Reto del río», «Play · River Challenge» y «Jogar · Desafio do Rio», nunca «Nivel 701». Al llegar, el Mapa se abre solo en la Cuenca, con «Muestrario · 73/73».
   - **El aviso del Reto (14.1, observación 19): cumple en los cuatro casos** (`c/aviso/`). Antes se volvió a conceder el permiso desde Ajustes › Avisos (`push_prompt` `granted`, origen settings), porque el `pm clear` del punto 15 lo había quitado. Todos con `--ei af_dev_push 2`, el Reto sin jugar hoy, y el dueño tocando el aviso:

     | Caso | Qué pasa | `AF_STATE` |
     |---|---|---|
     | En segundo plano dentro del 263 | sigue en el nivel | `push_intent` id 1000 y `push_open` `dest` `none` |
     | En segundo plano en el inicio, mismo id (1000) | abre el Reto de hoy (`d3_n0302`) | `push_open` `dest` `daily` y `load` con `daily` |
     | En frío, cerrado desde Recientes | arranca (`session` `cold`) y abre el Reto | `push_intent` id 1001 y `push_open` `dest` `daily` |
     | Desde Recientes tras `am kill`, sin tocar ningún aviso | abre el inicio y no cuenta el aviso otra vez | `push_intent` `rejected` `not_fired` y ningún `push_open` |

     - Con la Bitácora ya sellada hoy no hubo takeover, así que no se vio el caso del aviso que cierra la Bitácora encima.
     - En frío desde el aviso, el juego sale en inglés porque no lleva el extra `af_lang`.
     - Al publicar el aviso con el proceso muerto, Android arranca el proceso en segundo plano (el receptor de avisos), sin la actividad.
   - **OpenGL ES 3 frente a `-force-vulkan` (observación 20), con el mismo `dev.apk`.** La sonda da `gfx_api` OpenGLES3 por defecto y Vulkan con `-force-vulkan`.
     - **Memoria** (`c/mem2/`, alternando V, G, V, G; un minuto quieto, saliendo antes del nivel y con la Bitácora sellada), TOTAL PSS en KB (`GL mtrack`):

       | | OpenGL ES 3 | Vulkan |
       |---|---|---|
       | Inicio | 578.389 y 558.183 (79.896) | 585.030 y 586.948 (89.752) |
       | Mapa | 582.983 y 574.046 (82.040) | 599.893 y 600.235 (92.360) |
       | Nivel 75 (`c/mem/`, una vez) | 582.613 (85.208) | 597.168 (96.396) |

       OpenGL ES 3 gasta 10-30 MB menos, unos 10 MB de ellos en `GL mtrack`. La primera medida del inicio con Vulkan (610.732) y la del Mapa no valen: la Bitácora del día nuevo tapaba el inicio, y el toque al Mapa no llegaba.
     - **Arranque en frío** (`am start -W`, dos veces cada uno): OpenGL ES 3 da `TotalTime` 214 y 177 ms, `visible_ms` 283 y `os_splash_held_ms` 614-621; Vulkan, 175 ms, `visible_ms` 297-319 y `os_splash_held_ms` 636-688.
     - **La intro:** 185-188 fotogramas en sus 3.500 ms con los dos, sin cortes, y `first_frame_ms` 23-27.
     - **Morpho** (`--ei af_bench_morpho 1 --ei af_level 106`, 29 niveles): OpenGL ES 3, máximo de 7,29-7,30 ms y mediana de 1,2; Vulkan, máximo de 6,81-7,65 ms y mediana de 1,22-1,26.
     - **Fotogramas en el 106:** 60,07 fps, p95 16,65 ms y 0 tarde con los dos.
     - Así, cambiar a OpenGL ES 3 no cuesta rendimiento en este teléfono, pero sigue lejos de los 450 MB de D-060.
   - **El teléfono queda así:** el `dev.apk` de este build, con un perfil 18+ y el permiso de avisos concedido, en el inicio.

## Observaciones

1. **Bloqueante para el Release definitivo. La pantalla del sistema la suelta el tope de 3 s en todos los arranques (`os_splash_release` = `cap`), no la intro.**
   - Pasa en la primera apertura, en los 5 arranques en frío y en el ensayo de release.
   - Unity no despierta hasta que se suelta (`proc_awake_ms` ≈ `proc_os_splash_ms` + 200 ms), así que el arranque se alarga unos 3 s.
   - El petróleo liso de entre medias, que era lo que se quería quitar, sigue ahí: 3 o 4 fotogramas, de 0,25 a 0,5 s.

   El encargo dice qué hacer en este caso: «Si sale `cap`, se anota con su ráfaga y se revierte quitando `AfSplashHold.androidlib`». **La reversión la decide y la integra el escritorio**; el portátil no toca el código.
2. **Baja (resuelta con el build `d3e601a2`, punto 11). «Boto Leap» en gris en la pausa no responde al toque:** ni latido, ni `missing_tap` ni aviso. Pasó en el 46, quinto fallo, con 0 semillas y sin tienda. El toque llega a la ventana, pero el juego no escribe nada. Es el mismo «toque mudo» de la observación 6 de la jornada 5, en un botón que el arreglo no cubrió (`PauseMenu`). El encargo lo pedía en la rama D, que no se pudo ver.
3. **Baja (resuelta con el build `58be8444`, punto 9). Con otra app con el foco, Tintalú se queda en el petróleo liso.** Pasó con RCN TV visible a la vez (`topResumedActivity` en otra pantalla) al lanzar la app: Unity escribió `m_RunWithoutFocus: false` y se paró en `UnloadTime` más de 40 s, sin intro. Al cerrarla el dueño, siguió normal. Hay que mirarlo de nuevo con el arreglo de la pantalla del sistema, que toca justo el correr sin foco.
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
11. **Media (la juzgó el dueño: «no está pro»). Al pasar de la pantalla del sistema a la intro, el icono salta un 12-16 % más pequeño y el fondo cambia de tono un poco:** de ~486 a ~426 px y de (20,68,74) a (22,69,73). Se ve como si la imagen parpadeara o apareciera dos veces. La compuerta pide «ningún salto de posición ni de tamaño del icono». Sale en todos los arranques del `dev.apk` y en el ensayo. Lo demás de N6.1 cumple: `signal`, sin petróleo liso y Unity antes del velo.

   **Con el build `d3e601a2` (punto 10):** el tamaño, el centro y el fondo ya coinciden. Queda el cambio de forma (observación 12). El dueño: «sí está mejor».

   Con ese mismo build cumplen también la 4, la 5, la 7, la 9 y la 10 (punto 10), y la 2 (punto 11).
12. **Baja (el dueño: «sí está mejor»). En el relevo, el icono pasa de cuadrado redondeado a círculo.**
    - One UI recorta el icono adaptativo de la pantalla del sistema con su forma de cuadrado redondeado: 497 × 498 px en los ejes, en la caja de 504 px que mide el plugin. El primer fotograma de la intro dibuja el círculo de 504 px.
    - En la captura, el borde se mueve de 5 a 13 px en los ejes, y en las diagonales las esquinas desaparecen de un fotograma al siguiente. La compuerta pide «el diámetro ±2 px».
    - El tamaño, el centro, el dibujo de dentro y el fondo ya coinciden.
    - Para calcarlo habría que leer la forma de la máscara del sistema (`SplashScreenView.getIconView()` o el `Path` de `AdaptiveIconDrawable.getIconMask()`), o empezar la intro con esa forma y abrirla al círculo.
    - Sale igual en el `dev.apk` y en el ensayo.
13. **Media (la observación 8, sin arreglar en la bandeja). A 320 dp con la letra al 130 %, la bandeja del nivel parte palabras:** «Deshace/r» (es-419), «Desfaze/r» y «Mistura/r» (pt-BR), en el nivel 100 (`b16/nivel-320-detalle.png`). El HUD («Reiniciar») y el Mercado ya cumplen. Por D-061, las etiquetas de la bandeja deberían encoger hasta caber, sin bajar de 12 sp.
14. **Media (la observación 6, sin el efecto esperado). Con el Android Blit Type en Auto, `GL mtrack` no baja**: sigue entre 88,8 y 99,6 MB, igual que antes. El escritorio esperaba de 10 a 20 MB menos.
    - El PSS del `dev.apk` 18+ sale ahora en 605-620 MB, frente a 554 MB en la jornada 6, por `System` y `Code`. El ensayo da 545 MB y el <13, 537 MB.
    - Todo pasa de los 450 MB de D-060. Esa cifra es para la clase A; este teléfono es de 12 GB.
    - La imagen no cambia con Auto: sin parpadeos, franjas ni fotogramas viejos. Así que no hace falta volver a `Always` por la imagen, pero tampoco se gana memoria en este teléfono.
15. **Baja (resuelta con el build `ff3c49c0`, punto 15). Al 130 %, las pestañas del Mercado mezclan tamaños de letra:** cada una encoge su texto por su cuenta. «Comodines» y «Boosters» salen más pequeños que «Cuias», «No ads» o «Sin anuncios» (`b08/pestanas-6.png`). En es-419 pasa también al 100 % («Comodines»). El hueco entre pestañas ya cumple.
16. **Baja (a decidir). El aviso «El Reto del río de hoy ya está listo» abre el inicio con la Bitácora encima, no el Reto.**
    - Al tocarlo, el jugador ve primero la Bitácora (el takeover de prioridad 1 del inicio) y tiene que cerrarla para llegar a la tarjeta del Reto.
    - 06 §8.3 no dice qué pantalla abre cada aviso. El guion pide «el sitio correcto».
    - Pregunta para el escritorio y el dueño: ¿un aviso de tipo `daily_ready` debería abrir el Reto, o al menos saltarse el takeover?
17. **Baja (a decidir: arte). Los banners de mundo repiten dibujo.**
    - Del mundo 3 al 9 el dibujo es el mismo (Capi con Rosa en la orilla, por código). Solo cambian la tira del mapa, el agua y los textos.
    - El del mundo 2 (nivel 11) es más corto: sin la tira del mapa ni «Ver mapa».
    - Si cada mundo debe tener su propio arte, es trabajo del artista. Si el del 2 debe llevar «Ver mapa», lo decide el escritorio (`b11/hoja-entrada.png`).
18. **Baja (resuelta con el build `da40034d`, punto 16). Con el contenido acabado (`max_level` 700), el botón del Mapa dice «Jugar · Nivel 701».**
    - 06 (D-032, punto 6) pide «nunca «Jugar · Nivel 701»». El inicio sí dice «Jugar · Reto del río».
    - El botón del Mapa abre el Reto, así que solo falla la etiqueta (`b11/mapa-700-b.png`).
19. **Media (resuelta con el build `da40034d`, punto 16). Con D-062, el aviso del Reto no abre el Reto, ni en caliente desde el inicio ni en frío** (punto 14).
    - El toque llega a la app: `onNewIntent` en caliente, y en frío `START … (has extras)` con el aviso quitado. Pero no sale `push_open`, y el juego abre el inicio con la Bitácora encima.
    - Con la app en un nivel sí sale `push_open` (`dest` `none`).
    - Una pista para el escritorio: `AndroidNotificationScheduler.TakeOpenedType` descarta la intención si `intent.Id == s_lastIntentId`, y los avisos usan siempre el `id` 1001. Eso explicaría el caso en caliente, pero no el de en frío, que es un proceso nuevo. También puede ser que `GetLastNotificationIntent()` vuelva vacío cuando se lee.
20. **Media (a decidir, D-060). La memoria sigue por encima de 450 MB de PSS en todos los experimentos** (punto 14).
    - En el nivel 75: el `dev.apk` 560-611 MB y el ensayo 494-530 MB.
    - `-force-gles` (E1) es lo que más baja (−36 a −51 MB) y la imagen no cambia. Los bloques de 4 MiB (E2, E3) bajan poco.
    - Lo que haga falta lo decide el escritorio con el dueño: OpenGL ES por defecto, más recortes o medir en un teléfono de clase A, que es donde cuenta D-060.
    - **En el inicio y el Mapa** (punto 15): E0 588 MB, E1 567-576 MB, E2 y E3 592-593 MB. Otra vez solo baja E1 (13-21 MB).
21. **Baja (a decidir). El botón Atrás de Android no hace nada en el nivel ni en el inicio** (punto 15, 18.7).
    - El guion pide que en el nivel abra la pausa. Hoy no la abre, ni tampoco cierra la pausa, «¿Salir del nivel?» ni Nivel fallido.
    - En el inicio no sale de la app ni pide confirmación.
    - 04 §6.6 solo dice que Atrás no cierra el modal de Aguas estancadas ni el «¿Seguro?». Para el Reto, esta hoja ya lo dio por diseñado («como en un nivel», `GameBootstrap`).
    - Pregunta para el escritorio: ¿Atrás en el nivel debe abrir la pausa, como dice el guion, y en la pausa volver al juego?
22. **Baja. En pt-BR los miles salen con coma, como en inglés:** «1,000 sementes», «2,800 sementes» y el saldo «13,040» (`b13/idiomas-pt-BR.png`). En portugués de Brasil se escribe «1.000». En es-419 también sale «1,000 semillas», que vale para México pero no para Colombia ni Argentina. Los precios de la tienda falsa («COP 5,900») tienen el mismo formato.

## Notas de la herramienta (no son defectos del juego)

- Tras cada build de Unity el demonio de `adb` se reinicia y el teléfono vuelve con otro puerto (hoy, 41391).
- `af_font_scale` es un entero (`--ei af_font_scale 130`): con `--ef 1.3` no hace nada.
- El primer toque en el inicio recién cargado, o en un modal que se está abriendo, a veces se pierde. Sale bien al repetirlo.
- Para atascarse a propósito, el jugador elige en cada paso el vertido que deja menos vertidos posibles. El de movimientos al azar no se atascaba en el 35.
- **`ci/size-gate.ps1` reinicia el demonio de `adb`**, y con eso el teléfono se cae de la lista. Se vuelve con `adb connect` al puerto de `adb mdns services` (hoy, 36507).
- **`input keyevent 224` a veces no despierta la pantalla** si está en AOD (`mWakefulness=Dozing`). Mientras tanto el foco sigue en la app, así que `isKeyguardShowing=false` no quiere decir que el juego esté encima: hay que mirar `mWakefulness` y repetir.
- **Para llegar al quinto fallo:** Pausa › Salir › Salir cuenta como fallo y es rápido. «Rendirse» en Aguas estancadas pide confirmar. Al relanzar con `af_level`, `fails_level` vuelve a 0; sin `af_level` (y con `af_canoes 5`), se conserva. Hay que usar un nivel no superado.
- **`AF_LAYOUT` de un nivel solo sale al cargarlo**, no al reintentar ni al reanudar. El jugador automático que sigue en el mismo nivel tiene que reutilizar la disposición guardada.
- **La Bitácora tapa el inicio en cada arranque** hasta que se sella la página del día; hay que cerrarla antes de tocar el inicio.
- **El botón X de Nivel fallido** está en (915, 696) con la tarjeta del nivel: el `close` del `AF_LAYOUT` de `level_failed` sale a veces con otra `y` (549).
- **Un script que se cae sin cerrar su `adb logcat`** deja el proceso vivo y la tubería abierta, y el `timeout` de la consola no lo corta. Hay que matar esos `adb logcat` a mano, sin tocar el servidor.
- **El intersticial es el simulado del juego** («Anuncio simulado · interstitial_post_level», con «Cerrar»). Los vídeos con premio, en cambio, son los de prueba de AdMob.
- Para medir el icono se usan las capturas en bruto a resolución completa: el ancho en la fila 1170, el alto en la columna 540 y un punto a 45° que distingue el cuadrado redondeado del círculo.
