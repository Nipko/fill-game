# Prueba completa del juego en el teléfono (2026-10-26, jornada 3)

El portátil sigue el encargo de la jornada 3 de `guion-prueba-telefono-completa.md` sobre `integracion-fase1-fase2` en `32bd71f1`, posterior a `9024fb39`, con el SM-S938B por depuración inalámbrica. Esta entrega cubre el bloque 0 y N3 (lo nuevo de la jornada 3).

Siguen pendientes:
- los 3 puntos del bloque 3;
- el bloque 4 salvo el 4.9;
- los bloques 5 y 7 a 23, con lo que quedó de N1 (Reducir movimiento del sistema, la noche con la hora cambiada, el 16:9 y los anuncios sin red o cerrados antes de tiempo).

Capturas y logcat en el portátil: `Logs/device/completa-j3/` (fuera de git). El logcat se guarda filtrando `DebugAppCheckProvider`, «debug secret», «debug token», `appcheck:debugtokens` y `eyJ`, y al final no queda ninguna de esas líneas.

## Resumen

| Bloque | Estado | Observaciones |
|---|---|---|
| 0. Preparación y builds | cumple | — |
| N3.1 Arranque nuevo | cumple, con 2 observaciones | 1, 2 |
| N3.2 TiTino pulido | cumple, con 2 observaciones | 3, 4 |
| N3.3 Arreglos de la jornada 2 | cumple | — |
| N3.4 El ensayo de release | cumple | — |

## Verificación (bloque 0)

| Comando | Resultado |
|---|---|
| `git pull origin integracion-fase1-fase2` | `32bd71f1` («Encarga al portátil la jornada 3 de la prueba completa en el teléfono») |
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 errores |
| `dotnet test dotnet/AmazonFill.slnx` | 996 superados, 6 omitidos (1.002) |
| `python tools/validate_schema.py` | 116/116 PASS |
| `pwsh -NoProfile -File scripts/unity-build.ps1 -Method Dev` | Código 0 en 800 s: **`dev.apk` de 76,07 MB** (antes 75,98) |
| `ci/check-manifest.ps1 -Mode dev`, `ci/check-no-unity-iap.ps1` y `bash ci/check-16kb.sh` sobre el `dev.apk` | OK los tres |
| `pwsh -NoProfile -File scripts/unity-build.ps1 -Method Release -Rehearsal -Apk` | Código 0: **`rehearsal.aab` de 59,10 MB** (antes 59,01; 998 s) y **`rehearsal.apk` de 64,62 MB** (antes 64,52) |
| `ci/size-gate.ps1`, `check-16kb.sh`, `check-manifest.ps1 -Mode dev`, `check-release-classes.ps1` y `check-no-unity-iap.ps1` sobre el AAB y el APK del ensayo | OK todos |
| `git status` tras los builds | limpio |

**Estado del teléfono:**
- 1080 × 2340 sin override, densidad 420;
- batería al 72 % y 26,4 °C;
- estaba bloqueado y en «No molestar»; el dueño lo desbloqueó y quitó «No molestar» antes de N3.

## N3. Lo nuevo de la jornada 3

1. **Arranque nuevo (D-051, marca v2): cumple, con las observaciones 1 y 2.**
   - **Primera apertura** (`pm clear`): `brand_intro` `intro_ms` 1.178 = `load_ms`, `shown_ms` 650, `cut` false, `frames` 3. `boot`: `sloth_wake`, `sloth_ms` 1.015 y `hold_ms` 1.087, así que **TiTino se ve más de 1,0 s** desde el fundido.
   - En la jornada 2 la primera apertura cargaba en 1.121 ms: **+57 ms**.
   - **Ráfaga de capturas** de la primera apertura (`n3/burst_intro/`, `screencap` cada ~110 ms):
     - la pantalla del sistema con el icono (0,0-0,11 s);
     - **petróleo liso, sin icono ni animación, de 0,25 a 1,95 s** (unos 1,7 s; observación 1);
     - desde 2,07 s, la escena de la marca ya compuesta: la cuia de vidrio vierte açaí sobre el Amazonas con meandros, el río teñido de violeta y, a la derecha, «Tintalú» y TiTino en su rama.

     La animación de la intro no se llega a ver moverse: solo pinta 3 fotogramas.
   - **Cinco arranques en frío:**

     | Arranque | `TotalTime` | `intro_ms` = `load_ms` | `shown_ms` | `cut` | `frames` |
     |---|---|---|---|---|---|
     | 1 | 301 ms | 790 | 343 | true | 3 |
     | 2 | 326 ms | 810 | 356 | true | 3 |
     | 3 | 292 ms | 809 | 346 | true | 3 |
     | 4 | 291 ms | 826 | 366 | true | 3 |
     | 5 | 296 ms | 818 | 364 | true | 3 |

     En la jornada 2 eran 719-756 ms: **unos +70 ms (+9 %)** (observación 2).
   - **Memoria en la pantalla de edad tras arrancar:** 591,5 MB de PSS (704,1 MB de RSS), como los 590-645 MB de la jornada 1.
   - **Sin probar:** Reducir movimiento del sistema (lo enciende el dueño).
   - `screenrecord` falla en este teléfono con la pantalla bloqueada («UNASSIGNED_LAYER_STACK»), así que se usó la ráfaga de capturas.
2. **TiTino pulido: cumple, con las observaciones 3 y 4.**
   - **Bandas del tutorial:** el retrato de TiTino en las del nivel 1 («Finish the almost-full cuia…») y del 2 («There are two empty cuias…»), con la cara, las bandas de los ojos y el pelaje, sin bordes dentados (`n3/n3-banda-l1.png`, `-l2.png`). La del 3 solo sale tras un vertido que no acerca y la del 6 con Aguas estancadas; no se cazaron.
   - **El inicio de noche (00:27):** `sloth_sleep`, tumbado en la rama, con la cabeza unida al cuerpo, los ojos cerrados, mechones en el pelaje y tres garras (`n3-inicio-noche.png`). Mientras duerme sigue hablando («There's a new River Challenge today!»), y el cielo del inicio es de día (observación 3).
   - **Victoria del 40:** TiTino colgado de la rama, con hojas, sin bordes dentados (`n3-victoria-40.png`).
   - **Ventanita del Mapa:** sale en las capturas del Mapa (`r-mapa.png`, vista Cuenca).
   - **Sin probar:** la mañana (el saludo) y el mediodía (tomando el sol), que piden cambiar la hora (bloque 15).
3. **Arreglos de la jornada 2: cumple.**
   - **Nivel 1 tras cerrar en la edad:**
     - con el `dev.apk`: `pm clear`, cinco arranques cerrados en la pantalla de edad, y al contestar 18+ y «Accept all», `load` del nivel 1;
     - con el `rehearsal.apk`: cerrar en la edad, volver a abrir y contestar lleva al nivel 1 con la banda de TiTino (`n3r/r-tras-consentimiento.png`).
   - **Mercado en el ensayo:** tras jugar del 1 al 10, el inicio del nivel 11 **no muestra el icono del Mercado ni el Cántaro** (`n3r/r-inicio-11.png`). En la jornada 2 se veía el icono en el mismo sitio.
4. **El ensayo de release: cumple.**
   - El arranque nuevo y TiTino pulido (dormido en el inicio, de noche) se ven como en desarrollo.
   - Del 1 al 10, la Bitácora y el primer Mapa sin huecos de arte.
   - Logcat del proceso: 0 líneas `AF_*` y, como errores, solo `E/firebase` / `E/Unity`: `clazz`, la clase de depuración de App Check que se quita en release, lo esperado.

## Observaciones

1. **Alta. En la primera apertura se ven unos 1,7 s de petróleo liso**, sin icono ni animación, entre la pantalla del sistema y la escena de la marca. La intro animada de 650 ms no se ve moverse: `frames` 3, y la escena aparece ya compuesta. El guion pide anotarlo como alta si la pantalla del sistema se nota claramente más larga, y 1,7 s de color plano sí se notan.
2. **Media. La carga crece unos 70 ms** (790-826 ms frente a 719-756 en la jornada 2, +9 %), y la de la primera apertura, 57 ms (1.178 frente a 1.121). Encaja con los ~180 ms de la escena en el Editor.
3. **Baja. De noche TiTino duerme (`sloth_sleep`) pero su bocadillo sigue rotando mensajes**, y el cielo del inicio sigue de día. Hay que decidir si de noche calla o se oscurece.
4. **Baja. La mancha del lomo de TiTino** (naranja, en la pose tumbada) se ve bastante a tamaño normal; el guion pide que sea discreta. Es para la revisión del dueño.

## Respuesta del escritorio a la jornada 3 (2026-10-26)

1. **Los 1,7 s de petróleo liso en la primera apertura:** es un defecto y se trata en el carril `tanda6/fx-j3`. El carril mide qué parte es arranque del motor y qué parte es nuestra, y busca que nunca se vea un color plano largo. Las vías son tres: pintar antes el primer fotograma de la marca, poner el emblema en el fondo de la ventana de lanzamiento y guardar en disco la escena ya dibujada.
2. **La carga crece unos 70 ms:** va en el mismo carril. Con la escena guardada en disco, solo la primera apertura pagaría el dibujo.
3. **TiTino habla dormido y el cielo sigue de día:** el cielo de día está aceptado, porque el dueño no quiso ambiente nocturno (D-050). Que hable dormido queda como decisión del dueño.
4. **La mancha del lomo:** ya se hizo más pequeña y más apagada en el pulido. Queda para la mirada del dueño en el bloque 22.

La jornada 4 sigue con lo pendiente (bloque 3, bloque 4 y bloques 5 y 7-23) sin esperar a ese carril.
