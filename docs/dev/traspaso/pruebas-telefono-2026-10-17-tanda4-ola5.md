# Prueba de la ola 5 de la tanda 4 en el teléfono (2026-10-17): resultados para el escritorio

El portátil compiló `integracion-fase1-fase2` en `9fc15f9` («Cierra la ola 5 de la tanda 4…»), instaló el `dev.apk` en el SM-S938B por depuración inalámbrica y siguió el «QUÉ PROBAR EN EL TELÉFONO» de `9fc15f9`.

**Resultado: cumple.**
- Cumplen:
  - el audio definitivo por sondas (todo `src: clip`, ningún `synth`);
  - el rechazo con `af_dev_reject`;
  - los remates tras la toada;
  - la pantalla de arranque con TiTino y TiTino en el inicio;
  - el Mapa del río (marcadores, «ponerse al día», tarjeta de parada, Muestrario, «Norte arriba» con 3 placas y paradas hasta la 560);
  - la tarjeta «Fruta nueva»;
  - TiTino en el tutorial, en Nivel fallido y tras Volver a la orilla;
  - la regresión.
- **No hay defectos.**
- **Escuchar el audio queda para el dueño:** el portátil solo comprueba las sondas, no cómo suena (notas de fruta, chorro, desfase de las capas de música y voces en `amb_w01_fauna_02`).
- Hay seis observaciones y el estado del teléfono (al final).

Las capturas, los vídeos y los logcat están en el portátil, en `Logs/device/tanda4o5/` (fuera de git).

## Verificación (portátil ARM64)

| Comando | Resultado |
|---|---|
| `git pull --ff-only` y `git lfs pull` | 38 commits, de `18a20a2` a `9fc15f9` |
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 avisos, 0 errores |
| `dotnet test dotnet/AmazonFill.slnx` | 975 superados, 6 omitidos (981) |
| `python tools/validate_schema.py` | 103/103 PASS |
| `unity run … -executeMethod AF.EditorTools.BuildDev.BuildAndroidDev` | Código 0: `dev.apk` de 83.824.822 bytes (observación 1) |
| `adb install -r Builds/Android/dev.apk` (inalámbrico) | `Success` |

Notas:
- El build dejó el árbol limpio (los restos temporales del paquete de rendimiento, `Assets/Resources/PerformanceTestRunInfo.json` y `preloadedAssets` en `ProjectSettings.asset`, desaparecen al terminar).
- El build reinició el servidor de adb y el teléfono desapareció. Hizo falta que el dueño reactivara la depuración inalámbrica y reiniciar adb (`adb kill-server`).
- Los tests de Unity no se lanzaron en el portátil, porque tumban el adb del teléfono. Los recuentos de `9fc15f9` son los del escritorio.

## Resultados

### 1 · Audio definitivo (AU3): las sondas cumplen; falta escucharlo

- **Todo suena desde los WAV:**
  - más de 2.300 líneas `sfx` en toda la prueba, todas con `"src":"clip"`, y 0 con `synth`;
  - música con su clip (`mus_w01_bed`… `mus_w06_bed`, `mus_menu` en el inicio, `mus_w03_bed_night` y `mus_w04_bed_night` en los Muy difíciles);
  - ambiente con `amb_wNN_water` del mundo de cada nivel.
- **Capas de la música:** `layers` 0, 1 y 2 al sellar.
- **Lluvia según el nivel:**
  - ligera (`amb_rain_light`) en el 3, el 10 y el 81;
  - media (`amb_rain_medium`) en el 12, el 63 y el 143;
  - fuerte (`amb_rain_heavy`) en el 100 y el 243;
  - sin lluvia en los demás.
- **Rechazo** con `--ei af_dev_reject 1` en el 41: al tocar una cuia que no recibe suena `sfx_illegal`, sin ninguna línea `haptic`.
- **No hay sonda de la fauna** (va dentro del ambiente), así que la fauna de cada mundo solo se puede comprobar escuchando.

### 2 · Remates tras la toada: cumple, con la observación 3

Se ganó el 40, el 80, el 99 y el 50 y se pulsó «Siguiente». `sfx_win_toada` dura 2,55 s:

| Nivel | Toada | Remate | Separación |
|---|---|---|---|
| 40 → 41 | 17:05:04,550 | `sting_arrival` (Encontro) 06,797; `sting_world_banner` 11,947 | 2,25 s; 7,4 s |
| 80 → 81 | 17:05:45,398 | `sting_arrival` (Leticia) 47,763; `sting_world_banner` 51,909 | 2,37 s; 6,5 s |
| 99 → 100 | 17:06:20,850 | `sting_mech_card` (tarjeta previa) 27,773 | 6,9 s |
| 50 | 17:06:56,949 | `sting_arrival` (Manaos) 59,329 | 2,38 s |

- `sfx_fact` suena a 1,2 s de la toada, en la llegada, y no espera: no es un remate (`StingerId` no lo incluye).
- **Nivel fallido** en el 40: `sfx_canoe_lost` y `sting_level_failed`, sin toada (no hay victoria).

### 3 · Arranque e inicio con TiTino (TI6d, TI6c): cumple

- **Primer arranque con el perfil limpio:**
  - `boot` con `first_open: true`, `sloth: sloth_wake`, `sloth_ms: 1015`, `wake_fits: true` y `splash_ms` de 1.440-1.492 ms;
  - en el vídeo, tras el logo de Unity sale la cuia con la gota y TiTino colgado de la rama, que se despierta y levanta un brazo.
- **Arranques siguientes:** `sloth: sloth_idle` con `splash_ms` de 367-400 ms.
- **Inicio** con `af_max_level 58` (16:24, tarde):
  - TiTino cuelga en la ventanita, junto a «Casa · a unos 1,480 km»;
  - al tocarlo sale el Dato del perezoso («En Brasil, la embaúba se llama "el árbol del perezoso"») y se desvanece; los dos toques siguientes no lo repiten.
- **Sin comprobar:** el saludo (no salió en estos arranques) y las poses de otras horas.

### 4 · Mapa del río (M3.5, M4.1, M4.4, M4.5b): cumple, con las observaciones 4 y 5

- **Marcadores (M3.5),** con `af_max_level 58`:
  - la canoa de Capi con la bandera de Brasil y Rosa delante;
  - minicuias llenas en las paradas pasadas y vacías en las siguientes;
  - la baliza en la siguiente parada y la ventanita de la casa de TiTino.
- **«Ponerse al día» (M3.5):** con `af_max_level 68`, al abrir el Mapa la canoa avanza río arriba por las boyas desde la parada anterior. Hay `stop_arrive` de la `s06` en el 60.
- **Tarjeta de parada (M4.1):**
  - la de Manaos (`stop_s05`) trae «Manaos · Brasil · río Negro», la escena, el dato, «Otro dato», la fauna (sauim-de-coleira) y la fruta (Achiote);
  - la siguiente (`stop_s06`) dice «Nivel 61» y lleva la escena en silueta.
- **Muestrario (M4.1):** «Muestrario · 9/73» y «Datos del río · 12/128», con un estante por tramo (Estuario, Tapajós, Manaos, Trapecio, Loreto, Oriente…).
- **Vista Cuenca y «Norte arriba» (M4.4):**
  - con 58 salen las placas de los mundos 4-9 y, al girar con el norte arriba, solo las de los mundos 4, 5 y 6;
  - con 300, dos placas (mundos 8 y 9);
  - con 560, ninguna;
  - el giro dura menos de 1 s.
- **Paradas hasta la 700 (M4.5b):**
  - con `af_max_level 560`, el Mapa abre en la `s56` (mundo 9, «Ucayali · ¿Por qué el río dibuja tantas curvas?») con «Volver a la canoa», «Casa · a unos 3,980 km» y «Muestrario · 60/73»;
  - `map_load` tarda 4 ms (31 líneas y 70 rutas).
- **Sin comprobar:** el salto de Rosa con sus parientes en Tarapoto, que a esta escala no se distingue en las capturas.
- Archivos: `h_map58.jpg`, `h_map58_zoom.jpg`, `h_map_tarjetas.jpg`, `h_norte.jpg`, `h_catchup.jpg`.

### 5 · Tarjeta «Fruta nueva» (F5): cumple

- **Salen en su nivel:**
  - en el 4, Guanábana (`mode: dock`): «Guanábana: espinas blandas, pulpa blanca», en el muelle durante unos 3,7 s;
  - en el 7, Bacaba (`mode: band`);
  - en el 11, Agua del Tapajós (`band`, «Agua del Tapajós: el río de agua clara»).
- **No sale en el 2**, como dice TU1a: el tutorial tiene prioridad y solo hay una tarjeta por nivel.
- Suena `sfx_fruit_new`. No hay `sting_fruit_new` porque aún no tiene gancho, según `AudioClipIds.cs`.
- Archivos: `h_fruta4.jpg`, `h_tut.jpg`.

### 6 · TiTino en el tutorial, en Nivel fallido y tras Volver a la orilla (TU1a, TI6b): cumple

- **Tutorial con el retrato de TiTino** (`mode: band`):
  - en el 1, «Termina la cuia casi llena antes de apartar»;
  - en el 2, «Hay dos cuias vacías: reserva una para el final»;
  - en el 6, «Guarda una cuia vacía: es tu espacio para pensar».
- **Marea del 3:**
  - con un vertido que no acerca (1 → 3) sale `tutorial_undo` en `mode: dock`, con `over_undo: true`;
  - dice «Marea · Deshacer: toca para devolver el agua», con la cola que señala «Deshacer»;
  - con la solución A* no sale, porque todos sus vertidos acercan.
- **Tras Volver a la orilla en el 20** (dos veces): `sloth_restart` con `sloth_line_restart_01`, «Volver a empezar también es avanzar», durante 3 s.
- **Nivel fallido en el 40** (Rendirse y confirmar):
  - `sloth_fail` con `sloth_encourage` y `sloth_line_fail_01`;
  - TiTino cuelga junto a la cesta: «Casi. Respira, mira el río y otra vez.»;
  - las canoas bajan de 5/5 a 4/5.
- **No comprobé la cadencia de 1 de cada 4 por partidas ganadas:** las dos veces del 20 salieron con el mismo `stats.levels_won`.
- Archivos: `h_tarjetas.jpg`, `h_fallido.jpg`.

### 7 · Regresión: cumple

- **Seguidos con «Siguiente», con el perfil limpio:**
  - del 1 al 10;
  - el 11 y el 12, relanzados por `af_level 11` tras el corte de logcat de la observación 2.
- **Sueltos con `af_level`:** el 41, el 63, el 100, el 143, el 183 y el 243.
- **18/18 ganados con la solución A*:**
  - 0 rechazos del juego (el `reject` del 41 lo causó el toque de cerrar el banner, al caer sobre una cuia vacía sin selección);
  - 0 Aguas estancadas falsas.
- **Pantallas en su sitio:**
  - banner en el 11 y el 41;
  - tarjeta previa en el 63, el 100, el 143, el 183 y el 243;
  - llegada en el 100.
- **0 excepciones** y 0 líneas `E Unity` en los logcat de toda la prueba.

## Observaciones

1. **Para el escritorio: el `dev.apk` del portátil pesa 83.824.822 bytes, frente a los 64,59 MB del escritorio** y los 66,76 MB de la ola 4 en el portátil.
   - `assets/bin/Data/sharedassets0.resource` va sin comprimir (*Stored*) con 16.890.528 bytes; serán los 173 WAV, que en los `.meta` están en Vorbis (`compressionFormat: 1`, `quality: 0.8`).
   - ¿Pesa el APK del escritorio lo mismo con `unzip -v`? Si no, el portátil importa el audio de otra forma.
   - La compuerta de 80 MB es del AAB de release, no de este APK.
2. **Para el portátil (no es del juego): el flujo de `adb logcat` por wifi se cortó una vez en el 11.** El juego seguía respondiendo. El guion ahora reconecta logcat y pone tiempo límite a cada llamada de adb.
3. **Para el escritorio: los remates de la llegada empiezan entre 0,17 y 0,31 s antes del final del WAV de la toada,** que dura 2,55 s.
   - Esos últimos 0,3 s están por debajo de −40 dB, así que no se oye solapamiento.
   - Si se quiere esperar al final exacto del archivo, `ToadaLeftMs` debería usar la duración del clip.
4. **Para el escritorio: con `af_max_level 560`, la línea `AF_LAYOUT` del Mapa supera el tope de 4.068 bytes de logcat y llega cortada,** así que no es JSON válido. Las herramientas de prueba que leen los botones del Mapa no la reconocen. Habría que partir la sonda o quitar los botones de las paradas.
5. **Para el dueño, en la vista Cuenca con 58:** la placa «Mundo 5 · Loreto · Nivel 141» roza por arriba el botón «Norte arriba», pero se lee entera.
6. **Para el dueño: las distancias salen con separador de miles inglés** («a unos 1,480 km», «3,980 km»), porque la sonda `loc` da `locale: es-419` con `numbers: en` (el teléfono está en en-US). ¿Es lo esperado según D-018, o deberían seguir el idioma del juego?
7. **El teléfono:** batería al 72 % al acabar.
