# Prueba completa del juego en el teléfono (2026-10-25, jornada 1)

El portátil sigue `guion-prueba-telefono-completa.md` sobre `integracion-fase1-fase2` en `17fe4697`, posterior a `cefbb9b7`, con el SM-S938B por depuración inalámbrica. La hoja se escribe por jornadas.

Capturas y logcat en el portátil: `Logs/device/completa/<bloque>/` y `Logs/device/completa/logcat-2026-10-25-<n>.txt` (fuera de git). El logcat se guarda filtrando `DebugAppCheckProvider`, «debug secret», «debug token», `appcheck:debugtokens` y `eyJ`, y al cerrar la jornada se comprueba que no queda ninguna de esas líneas.

## Resumen

| Bloque | Estado | Observaciones |
|---|---|---|
| 0. Preparación y builds | cumple, con 3 observaciones | 1, 2, 3 |
| 1. Primera sesión (1-12) | cumple; falta la mirada del dueño | 4, 5 |
| 2. Recorrido natural (13-60) | cumple, con 2 observaciones | 6, 7 |
| 4. Comodines | solo el 4.9 (Música y Sonido): cumple; el resto, pendiente | |
| 6. Muestra amplia (1-200 y 201-700) | cumple | 8 |
| 3. Núcleo: tablero, vertido y gestos | cumple; quedan 3 puntos sin probar | 9 |
| 5, 7-23 | pendientes (jornadas 2 y siguientes) | |

## Verificación (bloque 0)

| Comando | Resultado |
|---|---|
| `git pull origin integracion-fase1-fase2` | `17fe4697` («Añade el guion de la prueba completa del juego en el teléfono») |
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 errores |
| `dotnet test dotnet/AmazonFill.slnx` | 996 superados, 6 omitidos (1.002) |
| `python tools/validate_schema.py` | 116/116 PASS |
| `git status --ignored` en `Assets/_LocalSecrets/` | `google-services.json` presente e ignorado (no se abrió) |
| `pwsh -NoProfile -File scripts/unity-build.ps1 -Method Dev` | Código 0, `[AF.BuildDev] OK` en 413 s: **`dev.apk` de 72,78 MB** (69,41 MiB) |
| `ci/check-manifest.ps1 dev.apk -Mode dev` | OK |
| `ci/check-no-unity-iap.ps1 dev.apk` | OK: sin Unity IAP ni `prd.insights.analytics.unity3d.com` |
| `bash ci/check-16kb.sh dev.apk` | OK: 19 bibliotecas de 64 bits alineadas a 16 KB |
| `pwsh -NoProfile -File scripts/unity-build.ps1 -Method Release -Rehearsal` | Código 0, `[AF.BuildRelease] OK` en 782 s: **`rehearsal.aab` de 56,24 MB**, sin APK (observación 2) |
| `ci/size-gate.ps1 rehearsal.aab` | OK: 56,24 MB, dentro de 80 MB |
| `bash ci/check-16kb.sh rehearsal.aab` | OK |
| `ci/check-manifest.ps1 rehearsal.aab -Mode dev` | OK |
| `ci/check-release-classes.ps1 rehearsal.aab` | OK: sin clases prohibidas en release |
| `ci/check-no-unity-iap.ps1 rehearsal.aab` | OK |
| `adb install -r dev.apk` y `pm clear` | `Success` y `Success` |

**Estado del teléfono:**
- Android 16, 1080 × 2340 (`wm size`), densidad 420 (física 450);
- fuente 1,0, navegación por 3 botones (`navigation_mode` 0);
- región e idioma en-US, así que el juego sale en inglés en Automático;
- batería al 89 % y 26,3 °C.

Para el jugador automático se regeneraron con A* (modo `run`) las soluciones de 1-200, de 205-700 de cinco en cinco y de 691-700, porque `37a4b3a3` cambió los colores de algunos niveles. Son 315 soluciones y no falla ninguna.

## Bloque 1. Primer arranque y primera sesión (1-12): cumple

Recorrido con perfil limpio, jugando las soluciones vertido a vertido desde el teléfono, con capturas de cada paso.

- **Arranque en frío:** `am start -W` con `TotalTime` de 676 ms. En logcat, `[AF.Ads] proveedor: not_configured` y 0 líneas de AppLovin o de Play Games del proceso del juego antes de la edad.
- **Edad:** tres botones iguales, y Atrás no la salta.
- **Consentimiento:** «Your privacy», y «Accept all» da `af_consent` `accepted`.
- **1:** el dedo guía en 0 → 2 (`lesson: first`), que se va al seguirlo; victoria «Perfect!» con +20.
- **2:** primero el dedo `partial_setup` (0 → 3) y, hecho ese vertido, `partial` (2 → 0), con la banda «tutorial_keep_one_empty».
- **3:** tras el vertido 1 → 3, que no acerca, sale `tutorial_undo` (Marea) y Marea lo deshace.
- **5:** avistamiento `saimiri_collinsi` (`first: true`).
- **6:** el tooltip `tutorial_restart_free_canoe` sobre Aguas estancadas; al volver a la orilla se va.
- **7:** la tarjeta «From level 10, a new board every day» sobre la victoria, sin taparla TiTino (`b01-l7-victoria_2.png`).
- **8:** el aviso de Capi («Capi brings you one more cuia…») y la Cuia extra, que añade la octava cuia. No se vio el dedo sobre «+1 cuia» (observación 5).
- **10:**
  - Canasta 9/10 → 10/10 (+50, `basket_open`);
  - «Next» lleva al inicio con la Bitácora, cuya página 1 ofrece tres premios (+60 / Tide ×2 / +30 y Tide ×1). Elegido +60 (`calendar_claim`);
  - «Continue» abre el Mapa (`map_open` `source: l10`, vista Cuenca, llegada a la parada 1);
  - el primer toque en «atrás» cierra la secuencia y el segundo, el Mapa;
  - el inicio queda con «River Logbook · Stamped today» y los discos de Música y Sonido bajo el engranaje.
- **11:** banner «World 2: Tapajós» (`world_banner_art` `wait_ms` 7) y tooltip de la Cosecha doble; vídeo simulado con `af_ad_request`, `af_ad_shown`, `af_ad_reward` y +20. Después pasa solo al 12.
- **12:** Morpho con 3 regalados. La pista propone 2 → 3 (`AF_PERF morpho`: A*, 23 nodos, 0,2 ms, cobrada).
- **Segunda sesión** (`force-stop` y reabrir): `session` `n: 2`, inicio en el nivel 13 con 370 semillas.
  - En la primera victoria (el 13), «Next» saca el pre-aviso. «Notify me» abre el diálogo de Android, que se concedió (`af_push_prompt` `native` `granted`; `POST_NOTIFICATIONS` `granted=true`).
  - No sale la Mochila de inicio (`takeover` `none`): la tienda no está disponible, y tiene sentido.
- **Excepciones:** 0 `E/Unity` en el bloque.
- **Sin probar:** la mirada del dueño (que se entiende sin leer y el ritmo); queda para él.

## Bloque 2. Recorrido natural de la campaña (13-60): cumple

Con el perfil del bloque 1, sin `af_max_level`. El jugador automático gana cada nivel y pulsa «Next»; si aparecen, cierra la tarjeta previa, el banner, la tarjeta de la Pororoca y los intersticiales. **48/48 ganados del 13 al 60, con 0 excepciones.** Registro por nivel en `Logs/device/completa/b02/natural.jsonl` y capturas de los hitos en `b02/`. El tramo «a mano» con fallos reales queda para el bloque 7.

- **Semillas por victoria:** 20 en Normal, 60 en Difícil (40, 46, 52, 58) y 80 en Muy difícil (60). Coinciden con `eco_reward_normal/hard/very_hard` de 08 §9 (20 / 60 / 80); el «40 en Normal» del guion es un error del guion.
- **Canastas:** el 20 da 50 y 3 Marea; el 30, 50 y Morpho; el 40, 50 y Cuia extra; el 50, 50 y 3 Marea; el 60, 50 y Morpho (`basket_open`). Todas con su llegada (`arrival`, `stop_arrive`).
- **Crecida:**
  - tarjeta de estreno en el 25 (`level_card` `debut` `win_streak`);
  - `streak_milestone` 3 en el 27 (+300), 6 en el 30 (cofre de 50) y 10 en el 34;
  - desde el 35, todas las victorias salen con `free_double_harvest` (Cosecha doble sin anuncio). Según 06 §3, el río crecido dura hasta el siguiente fallo duro no escudado.
- **Estrenos:**
  - 25, la Crecida;
  - 27, la Cuia alta de 5 (`debut` `alta`);
  - 30, la Pororoca (tarjeta `shuffle` `card`);
  - 31, las Canoas (`debut` `lives`);
  - 43, la Hoja (`debut` `hoja`);
  - 40, 46, 52 y 58, la tarjeta de dificultad Difícil; 60, la de Muy difícil.
  - **18 y 22: las cuias de 1 y de 3 aparecen sin tutorial de estreno** (observación 6). La del 14 (Cuia chica de 2) se vio en el bloque 1.
- **Intersticiales simulados:**
  - `allowed` en el 24, 31, 40, 48 y 58, con «Simulated ad · interstitial_post_level» y «Close»;
  - `blocked:BelowFirstLevel` hasta el 21;
  - `blocked:Warmup` en los primeros niveles tras cada arranque de la app (el jugador la reinició en el 17, el 25 y el 41);
  - `blocked:MinGap` en los demás.

  Cumple las reglas (≥ 150 s, primero desde el 22).
- **40:**
  - «Perfect!» con +120 (60 doblado por el río crecido);
  - la Canasta del Encuentro de las Aguas 10/10;
  - la frase de TiTino «World complete! You brought so much color.»;
  - `skin_unlock`.
- **41:**
  - banner de viaje «World 3: Manaos», con «Why is the Rio Negro black?» y «View map»;
  - patrones activados (`patterns.on` true, opacidad 0,20, frente a 0,15 apagados hasta el 40);
  - agua negra (`water: black`);
  - fruta nueva urucum.
- **60:** la placa «Very hard» y el premio de 80.
- **Sin probar en este bloque:**
  - la skin de Santarém vista en el tablero (bloque 11);
  - el botón «Liga» del inicio (bloque 12);
  - la música nocturna del 60 (bloque 23);
  - la mirada del dueño.

## Bloque 4.9. Música y Sonido con un toque: cumple

- **Pausa:** los discos de música y sonido van junto a la X (`pause_music`, `pause_sound`; `b04/b04-pausa-audio.png`). Apagar la música da `settings_change` `music` `false` `source: pause`.
- **Inicio:**
  - los discos van en columna bajo el engranaje y se ven tachados al apagar;
  - el de sonido da `settings_change` `sound` `false` `source: home`;
  - Ajustes muestra los dos apagados al momento (`settings`: `sound` y `music` en `false`);
  - al volver a tocarlos se encienden (`source: home`).
- **Vibración:** solo está en Ajustes.

## Bloque 6. Niveles: muestra amplia con el jugador automático: cumple

1. **Del 1 al 200, todos: 200/200 ganados.**
   - El 1-60 se jugó en los bloques 1 y 2. El 61-200, seguidos con «Next» en el mismo proceso, para medir la memoria.
   - Se cortó una vez en el 111, cuando otra app del teléfono pasó al frente; se retomó en el mismo nivel.
   - 0 excepciones de Unity.
2. **Del 201 al 700, uno de cada cinco (205-700), más 691-699: 108/108 ganados**, con `af_level` en cada uno.
   - El 255 falló la primera vez por el jugador automático: el toque que cerraba la tarjeta previa cayó sobre una cuia y la dejó seleccionada. Corregido el guion, se gana.
   - Tablero igual al pack y 0 excepciones.
3. **Viaje final del 700:**
   - victoria «Perfect!» con +160 (80 doblado) y «Another world complete! The river shines with you.»;
   - «Next» abre el Mapa con `map_journey` `by: time` (`elapsed_ms` 1.515): la canoa sube hasta el monte Mismi y el Muestrario marca 73/73;
   - al cerrar el Mapa sale `content_end` «The river keeps growing · 100 new levels are coming upriver in about 14 days» con «Back».

   La cabecera del Mapa dice «Ucayali» con la canoa en el Mismi (ya anotado el 2026-10-20).
4. **Memoria y temperatura** (`dumpsys meminfo` y `battery`, mismo proceso del 61 al 200):

   | Nivel | TOTAL PSS | Temperatura | Batería |
   |---|---|---|---|
   | 75 | 592,7 MB | 29,6 °C | 83 % |
   | 100 | 638,5 MB | 29,7 °C | 82 % |
   | 125 | 600,3 MB | 31,2 °C | 80 % |
   | 150 | 626,3 MB | 30,9 °C | 78 % |
   | 175 | 625,9 MB | 30,6 °C | 77 % |
   | 200 | 644,9 MB | 30,5 °C | 76 % |

   La memoria no crece sin parar: oscila entre 590 y 645 MB (observación 8).

## Bloque 3. Núcleo del juego: tablero, vertido y gestos: cumple

Con `af_level`, leyendo las sondas `AF_STATE` (`b03/*_logcat.txt`).

1. **Vertido por tramo (D-005), en el 2** (`[4,8,4,4] [3,3,3,4] [3,8,8,8] [] []`):
   - 0 → 3 vierte dos dosis en una vacía (`move [0,3,2]`);
   - 2 → 0 vierte 2 de una racha de 3 en un hueco de 2 y deja 1 (`[3,8]`);
   - el ilegal 1 → 0 (4 sobre 8) cambia la selección a la 0 (C5).
   - Con `af_dev_reject 1`, el toque en una vacía da `reject`. La sacudida no se pudo ver sin vídeo, y `sfx_illegal` no salió porque el teléfono estaba en «No molestar» (observación 9).
2. **Selección:** tocar el origen lo eleva (`select`) y tocarlo otra vez lo baja (`deselect`). Tocar una vacía sin nada elegido da `reject` (C2).
3. **Gestos:**
   - **toques muy rápidos:** unos 100 toques a ~10 por segundo sobre las 7 cuias del 7 dan 66 `select`, 12 `pour` y 78 `sfx`, sin excepciones ni bloqueos. Arrastrar, soltar en el fondo y mantener pulsado se vieron el 2026-10-22;
   - **sin probar:** dos dedos a la vez, que `adb input` no puede.
4. **Modo `single`:** con `af_rules_pour_mode single`, en el 5, el vertido 0 → 4 mueve una sola dosis (`amount 1`) de un par de 8.
5. **Disposición del tablero** (`AF_LAYOUT`, a 420 dpi; capturas `b03-disposicion-<nivel>.png`):

   | Nivel | Cuias | Filas | Zona táctil |
   |---|---|---|---|
   | 1 | 3 | 3 | 128 × 242 dp |
   | 2 | 5 | 5 | 77 × 187 dp |
   | 7 | 7 | 4 / 3 | 96 × 190 dp |
   | 11 | 8 | 4 / 4 | 96 × 190 dp |
   | 22 | 9 | 4 / 5 | 77 × 187 dp |
   | 63 | 10 | 5 / 5 | 77 × 187 dp |
   | 86 | 11 | 4 / 4 / 3 | 96 × 170 dp |
   | 123 | 12 | 4 / 4 / 4 | 96 × 170 dp |
   | 160 | 10 | 5 / 5 | 77 × 241 dp (cuia alta) |
   | 240 | 13 | 5 / 4 / 4 | 77 × 170 dp |
   | 335, 343, 600, 700 | 14 | 5 / 5 / 4 | 77 × 170 dp |

   - Ninguna cuia queda fuera de la pantalla y las filas siguen D-006 (3 filas desde 11).
   - `AF_LAYOUT` da la zona táctil, no el ancho del vidrio, así que el tope de D-004 (64 / 56 / 48 dp) no se puede medir con la sonda.
   - En el 160, con Lluvia, «Mix» sale en gris.
   - **Sin probar:**
     - la tropa en su franja de 56 dp (en las capturas a 1,5 s del inicio no se ve; queda para la revisión del dueño);
     - 15 y 16 cuias (bloque 7).
6. **Patrones de fruta** (Ajustes › Fruit patterns):

   | Ajuste | Nivel 30 | Nivel 41 |
   |---|---|---|
   | Always | `on` true, 0,20 | `on` true, 0,20 |
   | Never | `on` false, 0,15 | `on` false, 0,15 |
   | Automatic | apagado | encendido |

   - Al terminar se dejó en Automático.
   - **Sin probar** que se aplique «al nivel abierto»: Ajustes solo se abre desde el inicio.
   - En el inicio, «Never» queda por debajo del borde de Ajustes con los 5 idiomas; hay que desplazar para verlo. Está bien, solo se anota.
7. **Pista por inactividad:** con el ajuste encendido, a los ~12 s sin tocar en el 15 sale `idle_hint` `pulse` (`from: 5`); con el ajuste apagado, ninguna en 16 s. Se dejó encendida.

## Observaciones

1. **Media. Textos que pararían un release.** El build de ensayo avisa: «`[AF.LocReleaseGate]` … Problemas de textos que pararían un build de release: 1087». Con `com.amazonfill.dev` no bloquea, pero con la identidad definitiva sí.
2. **Media. El ensayo de release solo deja `rehearsal.aab`.** No hay `rehearsal.apk` para instalar en el bloque 20; hará falta sacar un APK universal del AAB.
3. **Baja. `ProjectSettings/GooglePlayGameSettings.txt` sigue apareciendo modificado** tras cada build, solo por los finales de línea: el arreglo de `6914b06f` no lo evita. Se descartó.
4. **Baja. Reiniciar con vertidos hechos pide confirmación** («Back to shore? You'll lose this attempt's pours», con Keep playing / Undo · Tide / Back to shore). Es nuevo; está bien, pero conviene saberlo para los guiones.
5. **Media. En el 8 no se vio el dedo sobre «+1 cuia»** en los ~6 s entre el aviso de Capi y el uso de la cuia, ni la sonda `tutorial_tip` `extra_cuia_finger`, que sí salía el 2026-10-22. Puede que el dedo espere a que termine el aviso.
6. **Media. Sin tutorial de estreno en el 18 (Cuia de 1) ni en el 22 (Cuia de 3).** Las cuias aparecen (`caps` 1 y 3), pero sin tarjeta previa ni `tutorial`; el guion y 02 §8 piden un tutorial de una pantalla en cada estreno.
7. **Baja. El guion da «40 en Normal»** como premio de victoria; el juego da 20, que es lo de 08 §9 (`eco_reward_normal` = 20). Hay que corregir el guion.
8. **Baja. La memoria del juego ronda los 600-650 MB de PSS** en el SM-S938B durante una sesión larga. Es estable, pero alta para un puzle 2D; en un teléfono de 3-4 GB (clase A) conviene medirla.
9. **Informativa. El teléfono estuvo en «No molestar»** (`zen_mode` 1) hasta las ~04:20. Con él, el juego no emite efectos ni las sondas `sfx` y `haptic`: por eso 0 `sfx` en los bloques 0-2 y 6. El dueño lo quitó y desde entonces salen. El bloque 23 (audio) se hará sin «No molestar».

## Respuesta del escritorio a la jornada 1 (2026-10-25)

1. **Textos que pararían un release (1.087):** no es un fallo, es la compuerta de D-018 (`LocReleaseGate`).
   - Con el ID definitivo, un release se para mientras haya:
     - texto cultural sin `aprobado_es`: 263 cadenas de `strings.csv` y 820 de `facts.csv` en `pendiente_asesor`;
     - traducciones en inglés o portugués sin `revisado_nativo`: todas están en `traducido`.
   - Es el pendiente humano del asesor cultural y de la revisión nativa (puntos 6 y 8 de la ronda del dueño). **Bloquea el primer release:** se lleva al dueño como decisión.
2. **El ensayo sin APK:** error del guion. Falta `-Apk` (`unity-build.ps1 -Method Release -Rehearsal -Apk` deja `rehearsal.aab` y `rehearsal.apk`). Corregido en el guion.
3. **`GooglePlayGameSettings.txt` modificado:** el `.gitattributes` de `6914b06f` (`text !eol`) solo actúa cuando el archivo se vuelve a sacar del repositorio. El guion explica cómo hacerlo una vez (renormalizar y `checkout`).
4. **Confirmación de Volver a la orilla:** es correcta (D-008). Anotada en el guion.
5. **El dedo del 8:** por diseño. Espera a que se vaya el aviso de Capi y no sale con otro aviso a la vista (`GameScreen.TutorialTips`). Con la cuia usada a los ~6 s, no llegó a salir. Corregido en el guion: hay que esperar al aviso.
6. **Sin tutorial en el 18 y el 22:** por diseño. La lista única de estrenos (02 §9.1) solo tiene la Cuia chica en el 14 y la Cuia alta en el 27. Las de 1 y 3 son la misma Cuia chica con otra capacidad, y el texto del tutorial («solo guarda dos dosis») no les valdría. Corregido en el guion.
7. **«40 en Normal»:** error del guion; son 20 (`eco_reward_normal`). Corregido.
8. **Memoria de 600-650 MB de PSS:** se abre un análisis en el escritorio (audio, texturas generadas por código y cachés) para bajarla antes de medir en un teléfono de gama baja.
9. **«No molestar»:** anotado. Para el bloque 23, sin él.
