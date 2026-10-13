# Prueba de la tanda 1 en el teléfono (2026-10-12): resultados para el escritorio

El portátil compiló `main` en `7d755c4` («Integra B2…»), instaló el `dev.apk` en el SM-S938B (Android 16, por cable) y probó por adb la tanda 1. Las pruebas usaron `AF_STATE` y `AF_LAYOUT` de logcat, toques con `adb shell input tap` y ráfagas de capturas en el teléfono (unas 5 por segundo).

**Resultado: todo lo de la tanda 1 cumple.** No hay defectos nuevos que corregir. Quedan cinco observaciones (final de la hoja); ninguna bloquea.

Las capturas, las ráfagas, los mp4 y los logcat están en el portátil, en `Logs/device/tanda1/` (fuera de git).

## Verificación (portátil ARM64)

| Comando | Resultado |
|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 avisos, 0 errores |
| `dotnet test dotnet/AmazonFill.slnx` | 714 superados, 4 omitidos (718) |
| `python tools/validate_schema.py` | 40/40 PASS |
| `pwsh -NoProfile -File scripts/unity-compile.ps1 -TimeoutSec 3600` | Compila (código 0) |
| `pwsh -NoProfile -File scripts/unity-test.ps1 -Mode EditMode` | 1.464 tests: 1.462 pasan, 0 fallan, 2 omitidos |
| `pwsh -NoProfile -File scripts/unity-test.ps1 -Mode PlayMode -TimeoutSec 3600` | 381 tests: 345 pasan, 0 fallan, 36 omitidos (413 s) |
| `unity run … -executeMethod AF.EditorTools.BuildDev.BuildAndroidDev` | `[AF.BuildDev] OK`: `dev.apk` de 62,82 MB (65.871.743 bytes), 182 s de build del APK, 1 aviso |
| `adb install -r Builds/Android/dev.apk` | `Success` |

Unity no reserializó las fuentes TMP: el árbol quedó limpio.

Para ganar los niveles por adb, una herramienta temporal (fuera del repo) sacó con `AStarSolver` (`run`, w = 1) la solución de cada nivel. En todos coincide con el par publicado. Así ganamos los niveles 2, 11, 43, 143 y 183.

## Resultados

### P1 · La Cosecha y la bandeja no se adelantan a la victoria: cumple

- **Nivel 2 (Cosecha 20, saldo de 740 antes de ganar).** Ráfaga desde el último toque hasta la victoria:
  - el saldo de la cabecera sigue en **740** durante todo el último vertido y la celebración;
  - en la pantalla de victoria sube con la cuenta de la fila: 740 → 744 → 752 → 760, mientras la Cosecha va de +0 a +20.
- **Bandeja.** «Deshacer» sigue azul mientras dura la animación del último vertido (hasta las 15,779). Se pone gris en la captura de las 15,958, al empezar la celebración: la victoria llegó a las 16,868, 900 ms después.
- **Archivos:** `p1_l2_hoja.jpg`, `p1_l2_antes.png` y `p1_l2_victoria.png`.

### P4 · La Cosecha doble avanza sola: cumple

- **Nivel 11.** Secuencia de eventos:
  - `victory` con `double_offered: true`;
  - toque en `double_harvest`;
  - anuncio simulado (`ad shown`), y «Cerrar» → `ad_closed completed`;
  - `double_harvest`: `harvest` 40 y saldo de 780 a 800;
  - **1,0 s después**, `auto_next` → `ad_gate` → `load` del **nivel 12**, sin tocar nada.
- **Pantalla:** la fila cuenta +21 → +32 → +40 y el saldo 781 → 792 → 800, con «Inicio» desactivado durante la espera.
- **No sale intersticial.** El motivo es otro bloqueo: `blocked:BelowFirstLevel` en el 11 y `blocked:Warmup` en el 143, donde también se probó. `AfterRewarded` no llegó a decidir (observación 1).
- **Archivos:** `p4_hoja.jpg`, `p4_victoria.png` y `p4_anuncio.png`.

### P3 · Miquitos con más presencia sin frenar el juego: cumple

- **El toque se habilita igual que antes** (de `load` a `ready`): 1,40 s y 1,39 s en el 2, y 0,78 s en el 21. Son las T de siempre: 1.400 y 800 ms.
- **Nivel 2** (larga, modelo «fila»):
  - un miquito baja trepando desde el follaje de la izquierda y la tropa entra por la rama desde los dos lados, durante el llenado (≈ 0,4 s después de `load`);
  - sueltan la fruta;
  - tras `ready` siguen sentados en la rama, y uno se come las uvas;
  - se van caminando hacia los lados ≈ 1,4 s después de `ready` y desaparecen ≈ 1,8 s después.
- **Nivel 21** (corta, modelo «cuelga»):
  - entran colgados desde las esquinas justo tras `load` (antes ya estaban posados en t = 0);
  - sueltan la fruta;
  - siguen colgados en reposo tras `ready`;
  - se van ≈ 1,3 s después y desaparecen ≈ 1,7 s después.
- Antes se veían ≈ 1,1 s en la corta; ahora, ≈ 2,4 s.
- **Archivos:** `p3_l2_rama.jpg`, `p3_l2_salida.jpg`, `p3_l21_rama.jpg`, `p3_l2.mp4` y `p3_l21.mp4`.

### T-A · «+1 cuia» al reabrir: cumple

- **Nivel 100** con `--ei af_seeds 500`: vertido 0→1, `force-stop` y reabrir sin extras.
  - Llegan `load`, `resume` y `ready`.
  - En el mismo milisegundo sale `AF_LAYOUT level` con las 10 cuias y `undo` (324, 2100), `extra_cuia` (540, 2100) y `reveal` (756, 2100).
  - En la primera captura del juego (76 ms después de `load`) ya se ven Deshacer, «+1 cuia» y Revelar, sin hueco.
- **Tras usar «+1 cuia»**, `force-stop` y reabrir: «+1 cuia» sale en gris.
- **Archivos:** `ta_bandeja.jpg`, `ta_reabierto.png` y `ta_reabierto_usada.png`.

### T-B · `AF_LAYOUT` fiel: cumple

- **Tras «+1 cuia» en el 100:** una sola `AF_LAYOUT level` nueva a los 0,5 s, con 11 cuias y todas con w > 0. Sus posiciones coinciden píxel a píxel con las de la app reabierta (es la posición final); las ranuras de abajo miden 168 px.
- **Toques:** el centro publicado de la 8 da `select` con `selected 8`. El de la 10 da `reject` con `tap 10`: es la cuia añadida, vacía, y no puede ser origen (observación 2).
- **Nivel 40** con `--ei af_canoes 1 --ei af_seeds 0`, vertidos 1→3 y 1→7:
  - `stuck`: `undo_modal`, `rescue`, `handful`, `balsa` y `give_up`;
  - `give_up` → `giveup_confirm`: `close`, `rescue` y `give_up`;
  - `give_up` → `fail_hard_canoe` y `level_failed`: `retry` y `close`;
  - `retry` → `no_canoes`: `close`, `extra_canoe_video`, `refill_canoes`, `infinite_canoes` y `home`.
- **Reabrir en ¿Seguro?** tras `force-stop`: vuelve a salir `giveup_confirm`.

### T-C · Ninguna cuia cambia de fila: cumple

- **Nivel 31** con `--ei af_seeds 5000 --ei af_canoes 5`:
  - al empezar, 4/4: `[0 1 2 3]` arriba y `[4 5 6 7]` abajo;
  - «+1 cuia» (150 semillas) deja 4/5, con la 8 abajo a la derecha;
  - atasco con 7→4, 2→8, 7→8, 7→2, 6→7, 5→6, 5→7 y 6→5;
  - el rescate (300 semillas, el primero del día) deja 5/5, con la 9 arriba a la derecha y una sola `AF_LAYOUT level`.
- Ninguna de las cuias 0-8 cambia de fila.
- **Archivos:** `tc_inicio.png`, `tc_extra.png`, `tc_rescate.png` y `tc_rescate.mp4`.

### T-D · Guardado por vertido: cumple con margen

- Se jugaron enteros el 143, el 43 y el 183: 52 líneas `AF_PERF pour`.

| Medida | n | Mediana | p95 | Máximo |
|---|---|---|---|---|
| `save_ms` sin `sync` | 49 | 0,16 ms | 0,29 ms | 1,25 ms |
| `save_ms` de los vertidos que ganan (`sync: true`) | 3 | 3,57 ms | — | 3,67 ms (3,25 / 3,57 / 3,67) |
| `save_ms`, todas | 52 | 0,17 ms | 2,15 ms | 3,67 ms |
| `bg_ms` | 51 | 3,59 ms | 4,96 ms | 6,62 ms |

- Ninguno pasa de 8 ms. El objetivo (p95 < 5 ms) se cumple con o sin las líneas `sync` (observación 3).
- **`force-stop` a mitad del 43** (tras 8 vertidos) y reabrir: el `load` trae exactamente el tablero del último `pour`. Con el resto de la solución, el nivel se gana.
- **Archivos:** `td_perf.json` y `td_logcat.txt`.

### Overlays: cumplen

- **103:** la pareja del Remolino (cuias 2 y 8, `remolino;pair=0`, en las filas 1 y 3) lleva **la misma ficha**, un cuadrado con un punto, junto a la espiral. Un canal discontinuo las une a través de las filas (`ov_l103.png` y `ov_l103_fichas.png`).
- **163:** el Brote (cuia 4) se ve sin zoom: un capullo rojo, del color de la cima, con hojas verdes. Cubre la boca y sobresale ≈ 1 u (`ov_l163.png`).
- **183** (`pirana;k=4` en la cuia 8): `lost 256` en los vertidos 4, 8, 12 y 16. En la ráfaga del vertido 4 (`ov_l183_p4_zoom.jpg`):
  - la 11 se sella;
  - el cardumen sale de la cueva y cruza hasta la tapa de la 11;
  - choca, con un destello;
  - vuelve a la cueva en ≈ 0,6 s, y el contador de la cueva se reinicia.
  - El caso «cueva llena» no ocurre con esta solución (`cave 0` en todos los vertidos): sin probar (observación 4).

## Observaciones (ninguna bloquea)

1. **`AfterRewarded` sin probar en el teléfono.** Tras la Cosecha doble no salió intersticial, pero por otros bloqueos: `BelowFirstLevel` en el 11 y `Warmup` en el 143. Si se quiere verlo en el teléfono, hay que ganar un nivel ≥ 22 con la sesión ya «caliente» y usar la Cosecha doble. Si no, basta con el test PlayMode que lo cubra.
2. **La hoja del 2026-10-09 (T-B) decía «tocar la 10: select 10».** Tras «+1 cuia» en el 100, la 10 es la cuia añadida y está vacía, así que lo correcto es `reject` con `tap 10`. La comprobación de que el toque cae en su cuia vale igual. Para comprobar `select`, usa una cuia con líquido, por ejemplo la 8.
3. **`sync` y el objetivo de T-D.** La hoja de la tanda 1 dice que se excluyan las líneas `sync: true`. `DevProbe.SaveCost` dice que siguen contando para el objetivo. Las dos lecturas cumplen (p95 0,29 ms y 2,15 ms), así que basta con unificar el texto.
4. **Sin probar en el teléfono:** la Piraña con la cueva llena («se asoma y se vuelve a dormir»). Hace falta un nivel o una secuencia donde muerda antes, o el test PlayMode correspondiente.
5. **Los tests de Unity tumban el adb del teléfono.** Al lanzar `unity-test.ps1` con el teléfono conectado, el Editor arrancó su adb y reinició el servidor (el riesgo de «dos adb» de CLAUDE.md). El teléfono desapareció de `adb devices` hasta desconectar el cable y desactivar y activar la depuración USB. En el portátil, haz las pruebas por adb antes o después de los tests de Unity, nunca a la vez.

## Lo que sigue (tanda 2, aún no está en `main`)

- **Avisos de 3-7 s y en cola:** comprobar la tarjeta «¡Miquito a la vista!» con la tropa ya ida, y el regalo de Capi en el 8 con perfil limpio (`adb shell pm clear com.amazonfill.dev`).
- **Patrones e iconos desde el 41:** 63, 40, 100 y 7.
- **Sacudida de 240 ms:** 43 y 63.
- **Banner de mundo:** 41 sí, 42 no.
- **Tutorial de la Piraña:** 183.
