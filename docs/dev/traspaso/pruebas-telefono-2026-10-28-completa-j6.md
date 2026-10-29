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

## Observaciones

1. **Bloqueante para el Release definitivo. La pantalla del sistema la suelta el tope de 3 s en todos los arranques (`os_splash_release` = `cap`), no la intro.**
   - Pasa en la primera apertura, en los 5 arranques en frío y en el ensayo de release.
   - Unity no despierta hasta que se suelta (`proc_awake_ms` ≈ `proc_os_splash_ms` + 200 ms), así que el arranque se alarga unos 3 s.
   - El petróleo liso de entre medias, que era lo que se quería quitar, sigue ahí: 3 o 4 fotogramas, de 0,25 a 0,5 s.

   El encargo dice qué hacer en este caso: «Si sale `cap`, se anota con su ráfaga y se revierte quitando `AfSplashHold.androidlib`». **La reversión la decide y la integra el escritorio**; el portátil no toca el código.
2. **Baja. «Boto Leap» en gris en la pausa no responde al toque:** ni latido, ni `missing_tap` ni aviso. Pasó en el 46, quinto fallo, con 0 semillas y sin tienda. El toque llega a la ventana, pero el juego no escribe nada. Es el mismo «toque mudo» de la observación 6 de la jornada 5, en un botón que el arreglo no cubrió (`PauseMenu`). El encargo lo pedía en la rama D, que no se pudo ver.
3. **Baja. Con otra app con el foco, Tintalú se queda en el petróleo liso.** Pasó con RCN TV visible a la vez (`topResumedActivity` en otra pantalla) al lanzar la app: Unity escribió `m_RunWithoutFocus: false` y se paró en `UnloadTime` más de 40 s, sin intro. Al cerrarla el dueño, siguió normal. Hay que mirarlo de nuevo con el arreglo de la pantalla del sistema, que toca justo el correr sin foco.

## Notas de la herramienta (no son defectos del juego)

- Tras cada build de Unity el demonio de `adb` se reinicia y el teléfono vuelve con otro puerto (hoy, 41391).
- `af_font_scale` es un entero (`--ei af_font_scale 130`): con `--ef 1.3` no hace nada.
- El primer toque en el inicio recién cargado, o en un modal que se está abriendo, a veces se pierde. Sale bien al repetirlo.
- Para atascarse a propósito, el jugador elige en cada paso el vertido que deja menos vertidos posibles. El de movimientos al azar no se atascaba en el 35.
