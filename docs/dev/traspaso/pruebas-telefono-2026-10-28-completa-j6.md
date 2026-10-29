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
15. **Baja. Al 130 %, las pestañas del Mercado mezclan tamaños de letra:** cada una encoge su texto por su cuenta. «Comodines» y «Boosters» salen más pequeños que «Cuias», «No ads» o «Sin anuncios» (`b08/pestanas-6.png`). En es-419 pasa también al 100 % («Comodines»). El hueco entre pestañas ya cumple.
16. **Baja (a decidir). El aviso «El Reto del río de hoy ya está listo» abre el inicio con la Bitácora encima, no el Reto.**
    - Al tocarlo, el jugador ve primero la Bitácora (el takeover de prioridad 1 del inicio) y tiene que cerrarla para llegar a la tarjeta del Reto.
    - 06 §8.3 no dice qué pantalla abre cada aviso. El guion pide «el sitio correcto».
    - Pregunta para el escritorio y el dueño: ¿un aviso de tipo `daily_ready` debería abrir el Reto, o al menos saltarse el takeover?
17. **Baja (a decidir: arte). Los banners de mundo repiten dibujo.**
    - Del mundo 3 al 9 el dibujo es el mismo (Capi con Rosa en la orilla, por código). Solo cambian la tira del mapa, el agua y los textos.
    - El del mundo 2 (nivel 11) es más corto: sin la tira del mapa ni «Ver mapa».
    - Si cada mundo debe tener su propio arte, es trabajo del artista. Si el del 2 debe llevar «Ver mapa», lo decide el escritorio (`b11/hoja-entrada.png`).
18. **Baja. Con el contenido acabado (`max_level` 700), el botón del Mapa dice «Jugar · Nivel 701».**
    - 06 (D-032, punto 6) pide «nunca «Jugar · Nivel 701»». El inicio sí dice «Jugar · Reto del río».
    - El botón del Mapa abre el Reto, así que solo falla la etiqueta (`b11/mapa-700-b.png`).

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
- Para medir el icono se usan las capturas en bruto a resolución completa: el ancho en la fila 1170, el alto en la columna 540 y un punto a 45° que distingue el cuadrado redondeado del círculo.
