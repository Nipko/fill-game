# Prueba de la ola 6 de la tanda 4 en el teléfono (2026-10-18): resultados para el escritorio

El portátil compiló `integracion-fase1-fase2` en `cb2460e` («Cierra la ola 6 de la tanda 4…»), instaló el `dev.apk` en el SM-S938B por depuración inalámbrica y siguió el «QUÉ PROBAR EN EL TELÉFONO» de `cb2460e`.

**Resultado: cumple, con un defecto de la sonda de desarrollo** (observación 1): las líneas largas de `AF_LAYOUT` siguen llegando cortadas al teléfono. No afecta al juego.

- **Cumplen:**
  - el Mapa del 10, también tras Inicio y tras cerrar la app (M4.2);
  - el Mapa desde la pausa, «Ver mapa» y el Mapa del fin de contenido (M4.3);
  - la ficha «Frutas del río» (F4);
  - Morpho con su presentación en el 12, la Pista y la pista por inactividad con su interruptor (MO1-MO3, TU1b);
  - los remates tras la toada y las placas de la Cuenca (FX7);
  - la victoria del 280 sin `step_budget` (FX6);
  - el remate de «Fruta nueva»;
  - la regresión.
- **Sin probar:**
  - la Tinaja (TJ2), el Brote de dos hojas y la Flor (VA1b): los paquetes del cliente acaban en el 300, así que el 323, el 343 y el 403 no existen todavía;
  - la respuesta honesta de la Pista («No encuentro»), que no se puede provocar en un tablero con solución.
- **Para el dueño:** decidir si se nota la temporada (SE2) y escuchar el audio.
- **El tamaño del APK del portátil** queda explicado (observación 2).

Las capturas, los vídeos y los logcat están en el portátil, en `Logs/device/tanda4o6/` (fuera de git).

## Verificación (portátil ARM64)

| Comando | Resultado |
|---|---|
| `git pull --ff-only` y `git lfs pull` | 48 commits, de `a9b0376` a `cb2460e` |
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 avisos, 0 errores |
| `dotnet test dotnet/AmazonFill.slnx` | 975 superados, 6 omitidos (981) |
| `python tools/validate_schema.py` | 103/103 PASS |
| `unity run … -executeMethod AF.EditorTools.BuildDev.BuildAndroidDev` | Código 0: `dev.apk` de 105.609.691 bytes (observación 2) |
| `adb install -r Builds/Android/dev.apk` (inalámbrico) | `Success` |

Los tests de Unity no se lanzaron en el portátil, porque tumban el adb del teléfono.

## Resultados

### 1 · Mapa del 10 (M4.2): cumple

- **Con el perfil limpio, 1-10 seguidos y «Siguiente»:**
  - `map_open` con `source: l10` y la vista Cuenca;
  - la canoa sube a Santarém (`stop_arrive` de la `s01`);
  - TiTino sale grande en su casa con el bocadillo «Yo vivo aquí, donde el río entra al mar.», que apunta a su cabeza;
  - después, el dato «Bajo el agua turbia de la desembocadura hay un arrecife que la ciencia describió en 2016» (`map.stop.estuario.dato_03`).
- **Placas y controles durante la secuencia:**
  - la placa «Mundo 2 · Tapajós · Nivel 11» va entera, por encima del bocadillo;
  - no sale «Norte arriba» (aparece al acabar la secuencia), ni ningún rótulo a medio tapar.
- **Al cerrar** (`map_close` con `map_seen_level: 10`) sale el saludo del inicio, «¡Buenos días! Ya tomé el sol. ¿Jugamos?», a las 10:09.
- **Se repite igual:**
  - saliendo con «Inicio» desde la victoria del 10;
  - cerrando la app tras ganar el 10 (`force-stop` y abrirla de nuevo);
  - también en cualquier perfil con `af_max_level` ≥ 10 que no la haya visto (sale sola sobre el inicio).
- Archivos: `h_mapa10.jpg`, `h_back10.jpg`.

### 2 · Mapa desde la pausa y «Ver mapa» (M4.3): cumple, con la observación 3

- **Pausa en el 41:**
  - trae «Seguir jugando», «Mapa del río», «Volver a la orilla» y «Salir»;
  - el Mapa abre con `source: pause` y «Volver al nivel»;
  - al volver, el tablero está como se dejó (3 vertidos).
- **«Ver mapa»** (`victory_map`) sale junto a «Inicio» en las victorias del 20 y del 300, y abre el Mapa con `source: victory` y «Volver».
- **Fin de contenido tras el 300:** con «Siguiente», `content_end` y el Mapa con `source: content_end` (Madre de Dios).
- Archivo: `h_m43.jpg`.

### 3 · Ficha «Frutas del río» (F4): cumple

- **El listado:** en Ajustes › «Sobre la Amazonía» › «Frutas del río», con `af_max_level 58`, salen Açaí, Agua del Tapajós y Achiote; las demás van en silueta con su nivel («Nivel 61»…).
- **La ficha del Açaí:**
  - `fact_page_open` con `unlocked: 20` y `from: settings`;
  - el nombre científico (*Euterpe oleracea*) y las 20 líneas numeradas;
  - las fuentes con su dominio (Embrapa, Mongabay, ter Steege et al. en *Science*, la Ley federal 15.330…).
- Archivos: `h_ajustes.jpg`, `h_ficha.jpg`, `h_ficha_fuentes.jpg`.

### 4 · Morpho (MO1-MO3, TU1b): cumple

- **Presentación en el 12** (perfil limpio, `af_max_level 11`):
  - `hint` con `phase: gift` y `units: 3`;
  - la franja `tutorial_morpho` de TiTino, «¿Atascado? La morpho conoce el río»;
  - el botón «Pista» lleva el «+3».
- **Pista:**
  - `sfx_hint`;
  - `hint` con `phase: pour` y `from: 2, to: 3` (A*, 23 nodos, 0,1 ms);
  - la mariposa vuela de la cuia 2 a la 3, con anillos en las dos;
  - el contador baja a «+2».
- **Pista por inactividad** (encendida por defecto en Ajustes):
  - `idle_hint` con `phase: pulse`, una vez, a los 12 s de la franja en el 12 y en el 13;
  - al apagarla, sale `settings_change` con `idle_hint: false` y en el 13 no hay ningún pulso en 24 s.
- Archivos: `h_m12.jpg`, `h_pista.jpg`.

### 5 · Tinaja (TJ2) y 6 · Brote de dos hojas y Flor (VA1b): sin probar

`levels_load` da 300 niveles: no hay 323, 343 ni 403 en los paquetes. Quedan para la ola 7.

### 7 · Temporada (SE2): la sonda cumple; el efecto, para el dueño

- **La sonda `stop`** da `season: A` en el 45 y el 55, y `season: B` en el 65, el 75 y el 185.
- **El efecto apenas se ve en el teléfono:** el tablero tapa casi todo el fondo y solo queda la franja del cielo.
  - El 65 se ve algo más cálido que el 45 (rojo − azul medio del cielo: 29 frente a 24).
  - Las paradas no son las mismas, así que la comparación es orientativa.
- **El agua es la misma.**
- Archivo: `h_temporada.jpg`.

### 8 · Arreglos de la prueba de la ola 5 (FX7): cumple salvo la sonda (observación 1)

- **Remates:**
  - en el 40, `sfx_win_toada` a las 10:23:44,700 y `sting_arrival` a las 47,246: 2,546 s, la duración del clip;
  - en el 280, la toada a las 10:26:07,077 y `sting_arrival` a las 13,532.
- **Cuenca con 58:** ninguna placa toca «Norte arriba» ni las banderas.
- **`AF_LAYOUT` del Mapa con 560:** con el Muestrario abierto sale partida (`part 1/2` y `2/2`), pero cada parte llega cortada (observación 1).
- Archivo: `h_fx7.jpg`.

### 9 · Victoria del 280 (FX6) y remate de «Fruta nueva»: cumple

- **El 280** se gana sin ninguna línea `step_budget`.
- **«Fruta nueva»** suena con su remate: `sting_fruit_new` junto a la tarjeta en el 4, el 7 y el 41. Tras el 280 no hubo tarjeta en el 281.

### 10 · Regresión: cumple

- **Seguidos con «Siguiente», con el perfil limpio:** del 1 al 10.
- **Sueltos con `af_level`:** el 11, el 12, el 41, el 63, el 100, el 143, el 183, el 243 y el 283.
- **19/19 ganados con la solución A*:**
  - 0 Aguas estancadas falsas;
  - el `reject` del 41 lo causa el toque de cerrar el banner, como en la ola 5.
- **Pantallas en su sitio:**
  - banner en el 11 y el 41;
  - tarjeta previa en el 63, el 100, el 143, el 183, el 243 y el 283;
  - llegada en el 100.
- **0 sonidos `synth`, 0 excepciones** y 0 líneas `E Unity` en los logcat de toda la prueba.

## Observaciones

1. **Defecto para el escritorio: en el teléfono, cada línea de la sonda se corta en unos 1.024 caracteres del mensaje** (1.066 con el prefijo de logcat).
   - En la ola 5 lo atribuí por error al tope de 4.068 bytes de logcat.
   - `DevProbe.MaxLayoutLineBytes` vale 3.900, así que FX7 parte en trozos que siguen cortándose. Hoy llegan rotas:
     - `river_map` con `af_max_level 560`: una línea sin partir y, con el Muestrario abierto, sus dos partes;
     - `settings`, con los botones `fruit_*`.
   - Con un tope de unos 1.000 bytes por parte se arreglaría.
   - Ninguna línea `AF_STATE` llega a ese largo en esta prueba.
2. **Para el escritorio: el `dev.apk` del portátil crece porque el build es incremental.** Gradle actualiza el APK en su sitio y deja huecos:
   - hay 37,6 MB de huecos entre entradas, de un total de 105,6 MB;
   - los datos comprimidos suman 67,8 MB, como los del escritorio;
   - el hueco mayor (21,65 MB) está delante de `sharedassets0.resource`;
   - un build limpio, o borrar `Builds/Android/dev.apk` antes, lo deja en su tamaño.
   - No es del juego.
3. **Para el escritorio: hay rótulos cortados por el borde del Mapa** (las placas de la Cuenca ya se arreglaron en FX7):
   - en la vista Mundo desde la pausa del 41, «Mundo 4 · Trapec…» se sale por la derecha;
   - en el Mapa del fin de contenido (300), «…adidi · Nivel 421» se sale por la izquierda y queda bajo la ventanita de TiTino.
4. **Para el dueño:** el efecto de la temporada (SE2) es sutil en el teléfono (ver 7).
5. **Para el dueño:** la pista por inactividad viene encendida por defecto.
6. **El teléfono:** batería al 85 % al acabar.
