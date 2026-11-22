# Prueba de la 1.1 en el teléfono (2026-11-07)

El portátil sigue `guion-prueba-telefono-1.1.md` en `integracion-fase1-fase2`, en el commit `62e06a0e`, que es el del encargo, con el SM-S938B. El teléfono empezó conectado por cable, pero se desconectaba a cada rato, así que se pasó a la depuración inalámbrica (`192.168.68.77`).

Capturas y logcat en el portátil: `Logs/device/v11/` (fuera de git).

**Lo que entrega:** los bloques 0 a 4. El 2.4, la tripulación, se hizo con un `dev.apk` nuevo, compilado desde `5dfa8efb`, que ya trae `v11/mapa-m2-4b-ajustes` (`17f4a25b`, integrado mientras se probaba).

## Resumen

| Bloque | Estado | Observaciones |
|---|---|---|
| 0. Preparación, builds y compuertas | cumple | 1 |
| 1. La curva 2 (301-700) | cumple | — |
| 2. El Mapa v2 (2.1-2.3, 2.5-2.7) | cumple, salvo el viaje demasiado rápido y la memoria | 2, 3, 4, 5 |
| 2.4 La tripulación (build con M2-4b) | se unen, pero no se distinguen | 8, 9 |
| 3. Liga, Aguas estancadas, borde a borde y orientación | cumple; Aguas estancadas, a aclarar | 6 |
| 4. R8, humo del ensayo de release | cumple | 1, 7 |

## Verificación (bloque 0)

- **Builds** (desde `62e06a0e`):
  - `scripts/unity-build.ps1 -Method Dev` da `[AF.BuildDev] OK` y un `dev.apk` de 75,42 MB;
  - `-Method Release -Rehearsal -Apk` da `[AF.BuildRelease] OK`, con `rehearsal.aab` de 79,87 MB y `rehearsal.apk` de 58,11 MB.
- **Compuertas:**
  - en los tres artefactos, `check-manifest -Mode dev`, `check-16kb`, `check-no-unity-iap`, `check-no-applovin` y `check-no-unity-telemetry` dan OK;
  - en el ensayo, `check-release-classes` da OK.
- **`size-gate`: OK, pero justo.** Da «archivo .aab sin BUNDLE-METADATA: 52.59 MB (archivo 79.87 MB; BUNDLE-METADATA 27.28 MB, que Play no sirve)». El archivo entero queda a 0,13 MB de los 80. En la jornada 6, el AAB medía 57,96 MB.
- **Instalación:** el `dev.apk` no se pudo instalar encima del que había, que alguien había instalado el 2026-11-01 desde otro equipo, por la firma: `INSTALL_FAILED_UPDATE_INCOMPATIBLE: … signatures do not match`. Se desinstaló, lo que borra el perfil igual que el `pm clear` del guion, y se instaló el nuevo. Perfil limpio: 18+ con el consentimiento aceptado (`analytics` true).

## 1. La curva 2 (301-700): cumple

Se jugó cada nivel con el jugador automático, con la solución del solver de AF.Core/AF.Solver del día. Todos se ganaron. El `id` sale en `AF_EVENT level_start` como `level_name` (no en `AF_STATE`), con `curve` 2 (`c2/`).

| Nivel | `level_name` | Banda, etiqueta y marco | Mecánicas | Cuias | Tarjeta |
|---|---|---|---|---|---|
| 303 | `m07_v1_n0303_c2` | comfort, Normal | `aguape;n=1` | 10 | sí, estreno |
| 347 | `m07_v1_n0347_c2` | comfort, Normal | `aguape;n=2` | 10 | sí, estreno |
| 363 | `m07_v1_n0363_c2` | comfort, Normal | `lancha;k=3` | 10 | sí, estreno |
| 306 | `m07_v1_n0306_c2` | challenge, **Difícil (ámbar)** | `hoja_atada`, `lluvia` | 11 | sí |
| 349 | `m07_v1_n0349_c2` | challenge, **Difícil (ámbar)** | `brote;n=2` | 12 | sí |
| 352 | `m07_v1_n0352_c2` | challenge, Normal, sin chip | `brote`, `remolino` | 11 | no |
| 672 | `m09_v1_n0672_c2` | challenge, Normal, sin chip | `liana`, `remolino` | 11 | no |
| 380 | `m07_v1_n0380_c2` | milestone, **Muy difícil (índigo)** | `aguape`, `pirana` | 13 | sí |
| 560 | `m08_v1_n0560_c2` | milestone, **Muy difícil (índigo)** | `aguape;n=2`, `flor`, `remolino` | 14 | sí |
| 700 | `m09_v1_n0700_c2` | milestone, **Muy difícil (índigo)** | `aguape;n=2`, `flor`, `hoja_atada` | 14 | sí |
| 374 | `m07_v1_n0374_c2` | comfort, Normal, 3 filas | `lancha;k=2` | 11 | no |
| 640 | `m09_v1_n0640_c2` | milestone, Muy difícil, 3 filas | `aguape;n=2`, `liana` | 13 | sí |
| 323 | `m07_v1_n0323_c2` | comfort, Normal | sin mecánicas | 10 | sí |

- **Las tarjetas de estreno** (`c2/montaje-1.png`) explican cada mecánica en una línea, con su dibujo:
  - el Aguapé: «sujeta estas cuias: puedes verter en ellas, pero no sacar. Sella una y se suelta»;
  - el Aguapé espeso: «sella dos cuias del grupo para soltarlo»;
  - la Lancha: «atraca cuando tengas 3 cuias vacías: trae fruta para terminar».

  En el tablero, la planta se ve bajo las cuias y la Lancha en su muelle, en la fila de abajo. Si se entiende sin el texto lo tiene que decir el dueño.
- **El 323 de Aguas turbias** sale como Normal y sin mecánicas en la sonda (las dosis ocultas no salen en `mech`).
- **Del 301 al 320 seguidos** (jugador automático): los 20 se ganan, todos `_c2`.
  - Los retos Difícil caen cada tres niveles: el 306, el 309, el 312, el 315 y el 318.
  - El 320 es hito, Muy difícil.
  - En medio, niveles comfort con Aguapé o Lluvia a ratos, y el 302 de recovery.
  - Sobre el ritmo: no hay muros para el jugador automático. La sensación de un jugador la tiene que decir el dueño.
- **A 360 dp** (`wm density` 480, y vuelta a 420; `c2/montaje-360dp.png`): el 700 y el 560 (14 cuias), el 380 (13) y el 349 (12) caben sin tocarse y sin salirse.
- **0 `E/Unity`** en todo el bloque. Las únicas «Exception» son las del aviso de Play Games (observación 1).

## 2. El Mapa del río v2

1. **El río vivo (M2-1), con `af_max_level 66`: cumple** (`mapa/mapa-66.png`).
   - Pantalla completa, con el río, su corriente, las orillas y la selva.
   - El Mapa ocupa unos dos tercios de la pantalla, y el conmutador Mundo/Cuenca y el Muestrario van en una sola fila.
   - Se desplaza bien con el dedo. No hay sonda de fotogramas en el Mapa: los 60 fps son a ojo.
   - La canoa de Capi, con su bandera, tapa el rótulo de un pueblo, del que solo se lee «Coa» (observación 2).
2. **Pueblos de paso (M2-2): cumple** (`mapa/pueblo.png`, `mapa/cuenca.png`).
   - Los rótulos se leen.
   - Al tocar Itacoatiara sale su ficha: «Itacoatiara · Brasil · río Amazonas», con su flecha para volver.
   - En la vista Cuenca salen los mundos con su nivel («Mundo 4 · Trapecio · Nivel 81»…), los países y «Norte arriba».
3. **Capi al remo (M2-3): el viaje es demasiado rápido** (observación 3).
   - Se ganó el 70 (`af_max_level 69 --ei af_level 70`) y luego el 80, y se abrió el Mapa con «Ver mapa» (`victory_map`).
   - La sonda da `map_journey` del 60 al 70 en `ms` 1.500, y `map_play_ready` a los 1.748 ms.
   - El dueño lo miró las dos veces. Literal: «es demasiado rápido la subida de Capi, no sé si estaba en cámara rápida» y «por ser tan rápido no se apreciaron gestos ni notarlo».
   - **Preguntas abiertas:** si marea la cámara y si el ritmo de los gestos se siente bien. **No se pueden contestar,** porque el viaje es tan corto que no se ve ni la cámara ni los gestos. En la ráfaga de capturas tampoco se distinguen las burbujas.
   - **«Faltan N»:** dentro del rótulo de la parada siguiente, en dos líneas, cuando la parada está a la vista: «Lago de Tefé · faltan 4 niveles». Pero tras el viaje del 70, con la parada siguiente fuera de la vista, sale una placa arriba a la izquierda: «Faltan 10 niveles para Leticia». Queda además la placa «Premio del mundo · faltan N», encima del río (observación 4).
4. **La tripulación (M2-4, con el `dev.apk` de `5dfa8efb`, que trae M2-4b): se unen, pero no se distinguen** (observaciones 8 y 9). Cada compañero se une una sola vez por perfil, así que para que lo viera el dueño se usaron perfiles nuevos con `pm clear`.
   - **Tucán** (`af_max_level 21`): al abrir el Mapa en la vista Mundo, Capi hace ♥ (`mapa/tucan.png`). El Mapa abre muy alejado, con la canoa diminuta. **Pregunta 5 de M2-4, el dueño:** «vi el tucán muy pequeño, el remo es casi indistinguible; ahí debería hacerse como un zoom y luego volver a verlo al tamaño normal».
   - **Tucuxi:** al ganar el 60 desde el 59, el viaje del 50 al 60 da `map_journey` `end` con `crew_joined` tucuxi, en 1,5 s. El dueño, en la segunda vez: «no se ve claro, no sé que sea un tucuxi, no se ve bien». Pide lo mismo: un zoom al trayecto y volver a la vista normal.
   - **El río ya recorrido** (`mapa/rio-recorrido.png`; el dueño: «el río de atrás respecto al recorrido se ve raro»): el tramo hecho se dibuja como una franja marrón oscura con puntos encima del agua. Donde se une al río claro sale un bloque oscuro de borde recto, como un parche cuadrado (observación 9).
   - **Rosa y el tucuxi junto a la parada:** van diminutos bajo la canoa, y la canoa con la bandera tapa el rótulo «Anavilhanas» (lo que avisaba el escritorio para el 56; observación 2).
   - **Miquito:** en el viaje del 60 al 70, sin compañero nuevo (`crew_joined` null), a la escala del Mapa no se distingue ningún miquito en los árboles de la ráfaga (`mapa/viaje70b.png`).
   - **Cambio de mundo, el 40:** sin ver. El perfil ya había pasado por el 41, y el banner se cerró al instante (`world_banner_closed`, `next` intro). Hace falta un perfil nuevo que llegue al 41 por primera vez.
5. **Reducir movimiento: cumple.** Con la fila de Ajustes encendida y el 90 ganado, `map_journey` termina al instante, con `by` reduce (`elapsed_ms` 0). Después se apagó.
6. **TalkBack (el dueño): cumple.** Lee los rótulos de los pueblos y anuncia la parada siguiente con lo que falta, y no lee los gestos de Capi. «Se une al viaje» queda para el 2.4.
7. **Memoria** (`--ei af_mem_os 1`, un minuto quieto en cada sitio, `mapa/mem/`):

   | | Inicio antes | Mapa abierto | Inicio tras cerrar el Mapa |
   |---|---|---|---|
   | TOTAL PSS (KB) | 560.269 | 616.272 | 610.772 |
   | `GL mtrack` (KB) | 79.980 | 110.232 | 104.616 |
   | Sonda `os_pss_mib` / `tex_mib` | 499,0 / 47,5 | 589,0 / 40,4 | 599,5 / 40,4 |

   - **Las texturas de Capi y de la tripulación** (`capi_tex_kib` 512 y `crew_tex_kib` 128, unos 0,6 MiB, no los +6 MiB que esperaba el guion) **se sueltan al cerrar el Mapa:** en `map_close` ya no salen.
   - **Pero el PSS y `GL mtrack` no vuelven** (observación 5): tras cerrar se quedan unos 50 MB y 25 MB por encima del inicio de antes.
   - **Frente al Mapa de la 1.0** (2026-10-30, OpenGL ES 3: PSS 574-583 MB, `GL mtrack` 82 MB), el v2 suma unos 35 MB de PSS y 28 MB de `GL mtrack`.

## 3. Lo demás de la 1.1

1. **El estreno de la Liga: cumple** (`otros/liga-estreno4.png`).
   - Con `pm clear`, `--ez af_league true --ez af_league_fake true --ei af_max_level 41` y el 42 ganado, primero salen el aviso previo de los avisos, la Bitácora del día y el Mapa con el viaje.
   - Al volver al inicio, en el primer momento tranquilo, sale «¡Llegó la Liga del río!», con la copa, «Ver la Liga» y «Luego» (`league_debut`, `source` lobby).
   - Tras «Luego» y relanzar, no vuelve (`takeover` `none`).
2. **Aguas estancadas solo al repetir: a aclarar** (observación 6). En el 95, en el primer intento (`attempt` 1, sin ganar antes) y repitiéndolo ya ganado, la misma secuencia de 3 vertidos (2>8, 4>9, 3>9) saca Aguas estancadas en los dos casos (`stuck` y `stuck_modal`). Es lo que hace el código del añadido de D-014 del 2026-11-06, pero no lo que dice el guion.
3. **Borde a borde: cumple** (`otros/b2b.png`; barra de estado 0-96 px, de navegación 2214-2340 px, recorte de la cámara 0-96 px).
   - En el inicio, el nivel, el Mapa, el Mercado y Ajustes, el fondo llega bajo las dos barras y nada que se toque queda debajo.
   - En el inicio, el rótulo «Mercado» del puesto queda justo en el borde de la barra de navegación, pero el botón está por encima.
4. **Orientación (el dueño): cumple.** Con la rotación automática encendida y el teléfono de lado, el juego no gira, ni en el inicio, ni en un nivel, ni en la intro. Sin tableta ni plegable: sin probar.

## 4. R8: humo del ensayo de release: cumple

El dueño recorrió a mano el `rehearsal.apk`, con `pm clear`, porque el ensayo no escribe las sondas `AF_` y el jugador automático no sirve. El registro del recorrido, filtrado, está en `r8/logcat-r8.txt`.

- **Arranque con la intro, consentimiento (18+ y aceptar) y un nivel ganado** (pasó del 10): cumple.
- **Un anuncio de prueba:** el vídeo de la Pista (la mariposa con ▶) sale y se cierra. Unity Ads «Initialized successfully». Los intersticiales empiezan en el 61, y las canoas no hicieron falta.
- **El Mercado no aparece:** con `com.amazonfill.dev` fuera de Play no hay tienda, y sin tienda se oculta (8.2, como en la jornada 6). «Precios de Play o no disponible» se comprobará con el AAB firmado en la pista interna.
- **La Liga no aparece:** va detrás de `live_league_enabled`, en `false` por defecto (08 §9), y el ensayo no lleva el extra de desarrollo.
- **Play Games:** el inicio de sesión silencioso falla con `ApiException: 10` (observación 1). No es de R8.
- **Ajustes › Eliminar cuenta y datos,** hasta «¿Eliminar todo?», sin confirmar: cumple.
- **El aviso programado:** no llegó en los minutos en segundo plano. Sin el extra de desarrollo, el primero va a su hora (el `daily_ready` de las 9:30), y las alarmas son inexactas. Queda sin probar en esta sesión (observación 7).
- **El veredicto:** en el proceso de Tintalú (pid 17425), 0 `FATAL EXCEPTION` y 0 `ClassNotFoundException`, `NoSuchMethodError`, `NoClassDefFoundError` o `AndroidJavaException`. La única `ClassNotFoundException` del registro es de Outlook, otra app del teléfono.

## Observaciones

1. **Media (cuentas). Play Games no inicia sesión ni en el `dev.apk` ni en el ensayo de este portátil:** `[Play Games Plugin 2.1.0] ERROR: Authentication failed - com.google.android.gms.common.api.ApiException: 10`. El código 10 es `DEVELOPER_ERROR`: casi siempre, la huella SHA-1 de la clave con la que se firma no está dada de alta en la consola de Play Games para `com.amazonfill.dev`.
   - La app que había en el teléfono la había instalado otro equipo el 2026-11-01, con otra firma.
   - Los dos equipos firman con claves de depuración distintas. Hay que dar de alta las dos huellas, o firmar los dos con la misma.
2. **Baja. En el Mapa, la canoa de Capi con su bandera tapa el rótulo de lo que tiene debajo.** Pasa con un pueblo, del que se lee «Coa», con `af_max_level 66` (`mapa/mapa-66.png`), y con la parada siguiente, «Amacayacu · faltan 5 niveles», en el 96 (`otros/b2b-mapa.png`).
3. **Media (el dueño). El viaje de Capi es demasiado rápido:** 1,5 s para diez niveles (`map_journey` `ms` 1.500). No deja ver los gestos (remar, saludar, levantar el remo, amarrar la muestra), ni las burbujas, ni juzgar si la cámara marea. Con ese tiempo, las dos preguntas abiertas de M2-3 no se pueden contestar.
4. **Baja. «Faltan N» sale en una placa arriba a la izquierda cuando la parada siguiente queda fuera de la vista:** «Faltan 10 niveles para Leticia», tras el viaje del 70 (`mapa/viaje70.png`). El guion dice que va en el rótulo de la parada, no en una placa. Queda además la placa «Premio del mundo · faltan N», encima del río y de Fonte Boa.
5. **Media (D-060). El Mapa v2 sube la memoria y no la devuelve al cerrarlo:** PSS de 560 a 616 MB, que se queda en 611, y `GL mtrack` de 80 a 110 MB, que se queda en 105. Las texturas de Capi y de la tripulación sí se sueltan, y la sonda `tex_mib` no sube (40,4). Lo que no baja está en `GL mtrack` y `os_graphics`. Frente al Mapa de la 1.0, unos +35 MB de PSS.
6. **A decidir (guion o código). «Aguas estancadas solo al repetir» no hace lo que dice el guion.**
   - El código (añadido de D-014 del 2026-11-06, `142ff066`) da Aguas estancadas cuando **todo vertido legal lleva a un tablero ya visto en el intento**, sin mirar si el nivel ya se ganó.
   - El guion pide que no salga en el primer intento y sí al repetir un nivel ganado.
   - En el 95, la misma secuencia lo saca en los dos casos, como manda el código.
   - Hay que decidir cuál vale.
7. **Baja. En el ensayo no se vio llegar un aviso programado.** Sin `af_dev_push`, el primero va a su hora y las alarmas son inexactas. Se puede comprobar dejando el ensayo instalado hasta el día siguiente, como el 14.2 de la jornada 6.

8. **Media (el dueño). El tucán y el tucuxi no se distinguen al unirse al viaje:** el Mapa los enseña muy alejados y el viaje dura 1,5 s. El remo de Capi casi no se ve. Propuesta del dueño: cuando se une un compañero, hacer un zoom a la canoa y al trayecto y luego volver a la vista normal.
9. **Baja (el dueño). El tramo de río ya recorrido se ve raro:** una franja marrón oscura con puntos sobre el agua, y al final un bloque oscuro de borde recto sobre el río claro (`mapa/rio-recorrido.png`).

## Notas de la herramienta (no son defectos del juego)

- **Con el cable, el teléfono se cae de `adb` una y otra vez.** También se cayó en cuanto empezó el build del ensayo, porque el build de Unity usa su propio `adb`. Por Wi-Fi fue estable.
- El `id` de la curva 2 está en `AF_EVENT level_start`, como `level_name`, junto a `curve`, `band` y `variant`. En `AF_STATE load` no aparece.
- Con TalkBack encendido, los toques de `adb shell input tap` no pulsan los botones: abrió el Mapa el dueño.

## Respuesta del integrador (2026-11-08)

- **Observación 6: vale el código.** El guion se equivocaba. «Solo al repetir» del añadido de D-014 del 2026-11-06 se refiere a repetir tableros dentro del intento, no a repetir un nivel ya ganado. Lo visto en el 95 es correcto, y el guion está corregido (punto 3.2).
- **Observaciones 2, 3 y 4:** las arregla el carril `v11/mapa-j1-arreglos`: un viaje más lento, rótulos que esquivan la canoa y «Faltan N» sin placa.
- **Observación 5:** la estudia el carril `v11/mapa-j1-memoria`, que también amplía la sonda de memoria.
- **Observación 1:** es del dueño. Hay que dar de alta en la consola de Play Games, para `com.amazonfill.dev`, la huella SHA-1 de la clave de depuración del portátil, o firmar los dos equipos con la misma.
- **Observación 7:** dejar el ensayo instalado hasta el día siguiente.
- **2.4 (la tripulación):** con el build nuevo, en cuanto estén los dos carriles.
