# Estado del equipo Spark: el Reto del río

Encargo: `docs/dev/traspaso/spark-reto-del-rio.md` (de `origin/integracion-fase1-fase2`). Nota de diseño: `docs/dev/reto-del-rio.md`.

Orden de este archivo (encargo §9, punto 5): arriba el resumen; después, una entrada por tarea o bloque, de la más reciente a la más antigua; al final, la única sección «Enganches para el integrador» (E1-E20).

---

## 2026-10-20 · T2 · Decisiones del dueño sobre las preguntas de la traducción con IA

El dueño respondió, literal: «las preguntas t2 hagamos lo recomendado». Se aplica la opción recomendada de cada una. T2-Q6 y T2-Q7 salieron después, de la revisión de T2, y también se resuelven con la opción recomendada:

| Id | Pregunta | Decisión (la recomendada) |
|---|---|---|
| T2-Q1 | El bloque `map.*` de `facts.csv`: `tools/river-map/stops.py` escribe `en` y `pt-BR` vacíos al regenerar | (a) `stops.py` conserva las columnas `en` y `pt-BR` y sus estados. Va como **parche probado**, porque `tools/river-map/**` es del integrador (encargo §3.2). Se traducen las frases `map.*` que solo usan nombres del glosario |
| T2-Q2 | Las 8 claves de P14 (glosario §10: EN/PT a la espera del dueño) | (a) Se aprueban sus textos y se aplica `strings_p14.csv` |
| T2-Q3 | Lugares, países, ciudades y ríos, comodines (Marea, Morpho, Luciérnaga), mecánicas y fauna sin EN/PT en el glosario | (a) Se añaden al glosario: los exónimos habituales de países, ciudades y ríos, y propuestas para comodines, mecánicas y fauna, con lo cultural en `pendiente_asesor` / propuesta. Va como **parche probado** de `Data/Loc/glossary.csv` y del glosario §10. Las traducciones que dependen de esos nombres, en un archivo aparte que se aplica con ese parche |
| T2-Q4 | Términos que se tradujeron sin entrada en el glosario: Cosecha doble (Double Harvest / Colheita dupla), Aguas estancadas (Still waters / Águas paradas), Muestrario (Samples / Mostruário) y «Sobre la Amazonía» (About the Amazon / Sobre a Amazônia) | (a) Pasan al glosario, en el mismo parche que T2-Q3 |
| T2-Q5 | `LocReleaseGate.Evaluate` no exige `revisado_nativo` en un build de release, aunque 08 §4.7 lo pide | (a) Se añade la regla: en release, una cadena de UI en `en` o `pt-BR` sin `revisado_nativo` es aviso o bloqueo, con el mismo nivel que los demás avisos de la puerta (D-018, Q5-A). Va como **parche probado** |
| T2-Q6 | Frases de `facts.csv` con el español aún en `pendiente_asesor` (08 §4.7: una traducción solo vale con `aprobado_es`; `FactCatalogTests` exige los estados vacíos) | (a) **Retener**: la entrega de `facts.csv` queda guardada y se aplica cuando el asesor apruebe el español. El script no escribe filas por debajo de `aprobado_es` y detecta un español cambiado |
| T2-Q7 | «Amazonía» / «the Amazon» / «a Amazônia» y regiones (trópico americano, América tropical, alta Amazonia…) sin entrada en el glosario; el glosario §1 evita la cadena «Amazon» por la marca de Amazon.com | (a) Se dejan **vacías** y anotadas en `pendientes.csv`, hasta que el dueño decida con el asesor la entrada del glosario |

## 2026-10-19 · Entrega final: R0-R12 en `spark/reto-del-rio`, verificación completa VERDE

Verificación en `spark-int`, sobre la rama con la parte del Spark de R12 (`ac54d82`) y los parches guardados (`bb5b163`, `fa5209e`):

| Comando | Resultado |
|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | código 0 |
| `dotnet test dotnet/AmazonFill.slnx` | 981 (975 + 6) |
| `python tools/validate_schema.py` | 103/103 |
| `unity-compile.ps1 -Architecture arm64` | OK |
| `unity-test.ps1 -Mode EditMode … -TimeoutSec 3600` | **3.058 (3.055 + 3 omitidos), 0 fallos** |
| `unity-test.ps1 -Mode PlayMode … -TimeoutSec 3600` | **1.383 (1.216 + 167 omitidos), 0 fallos** (39 min) |
| Capturas de R11 (`DailyChallengeSnapshotTests`, sin `-nographics`) | 4/4; `AF_FIT` = 0 textos fuera de su caja en es-419, es-419 16:9, en-XA y en-XA 16:9; `AF_STAMP` = desvío 0,00 dp |

- **Recuentos de referencia del Spark para el integrador:**
  - sin los enganches: EditMode **3.058**, PlayMode **1.383**;
  - con los enganches aplicados (R12): EditMode 3.082 y PlayMode 1.394.
- **Bloques listos para integrar:**
  - (a) ya integrado;
  - (b) el pool;
  - (c) lógica;
  - (d) partida y pantallas;
  - (e) los enganches, con sus parches en `docs/dev/traspaso/spark-enganches/` y en `docs/dev/traspaso/spark-parches-docs.md`.
- **Fuera de alcance o pendiente:**
  - E1, E6 y E7 sin preparar (motivo en la entrada de R12);
  - E16 por regenerar;
  - E19, E20 y E25, para más adelante.
  - Las capturas son de prueba; falta la prueba en el teléfono, que necesita los enganches aplicados en la integración.

## 2026-10-19 · R12 · Enganches para el integrador: parches exactos y PROBADOS · Bloque (e) listo

**Probados contra:** `origin/integracion-fase1-fase2` en **`bc65242`**, con `spark/reto-del-rio` fusionada (R0-R11 y la parte del Spark de R12). Worktree temporal `r12-full`, no empujado, HEAD `c4ba4a1`.

### Dónde están y en qué orden se aplican

Carpeta **`docs/dev/traspaso/spark-enganches/`**: diffs unificados con LF fijado (`.gitattributes` propio).

1. Fusiona `spark/reto-del-rio`. Ya trae la parte del Spark de R12 (`ac54d82`): la tarjeta con el título en dos líneas y 100 dp, los `max_chars` del CSV, `DailyUiLogicTests` con la regla `VisibleLength`, y `DailyProbeFormat.EarnSeeds` con `source` (para E9) y `DailyChallengeService.Config`.
2. Aplica **`R12-all.diff`**: todos los enganches juntos, 26 archivos. `git apply --check` da OK sobre `ac54d82`.

   O bien, uno a uno y en este orden:
   - `E4.diff`;
   - `E2.diff`, `E22.diff` y `E23.diff` (los tres tocan `GameBootstrap.cs`);
   - `E3.diff` y `E3-scene.diff`;
   - `E5.diff` y `E5-fix.diff`;
   - `E9.diff`;
   - `E21.diff` y `E24.diff`;
   - `E-tests.diff`.
3. **Mejor que aplicar `E3-scene.diff`:** regenera `Game.unity` con `AF.UI.Editor.GameSceneBuilder.CreateGameScene` tras aplicar `E3.diff`. El diff de la escena solo añade `dailyPool` (el guid del `.meta` del pool) y renumera los fileID.
4. Después, que Unity genere los `.meta` de los archivos nuevos (`ProfileDailyChallengeStoreTests.cs`, `DailyDoubleHarvestTests.cs`, `DailyHooksTests.cs` y `ProfileDailyChallengeStore.cs`) y commitéalos tal cual.

### Qué hace cada uno

| # | Archivos | Qué |
|---|---|---|
| E2 | `GameBootstrap.cs` | `SetUpDaily();` tras `_lobby.Bind(_app, _services);` |
| E3 | `Editor/GameSceneBuilder.cs` (+ `Game.unity` regenerada) | `DailyPoolPath` y `ConfigureDailyPool(dailyPool)` tras `ConfigureRiverStops`; aviso si falta el pool |
| E4 | `Save/Profile.cs`, `Save/ProfileJson.cs`, nuevo `LiveOps/ProfileDailyChallengeStore.cs`, `GameBootstrap.Daily.cs` (fábrica por defecto) | Bloque `daily_challenge` (nota §10). Se omite mientras es null, así que el golden del perfil no cambia. `CurrentSchema` 1, sin migración (08 §4.3). Un tipo equivocado o un `in_progress` sin `date` o `id` es corrupción. El almacén usa el mismo `Profile` y el mismo lote (`ProfileSaveBatch.Durable`) que la cartera del Reto: nunca dos pagos. 17 tests |
| E5 | `Data/Loc/strings.csv`, `LocTablesTests` (+ tests del Reto) | Las 24 filas de `spark-strings-reto.csv` en orden ordinal (`data-json.md`); `LocTablesTests` añade `DailyText.NewKeys`; los tests del Reto leen ya `strings.csv` |
| E9 | `Economy/Transaction.cs`, `Wallet.cs`, `DailyChallengeScreen.cs`, `DailyStampCalendar.cs`, tests | `Transaction.DoubleHarvestDaily(seeds, level)` [P en la firma: lleva el nivel para el mínimo `ads_rv_double_harvest_from_level` = 11]. Solo con el premio confirmado, una vez por victoria, con el tope diario y el cooldown de `rv_double_harvest` de AF.Ads (compartidos con la campaña [P]) y como mucho seeds × `murky_bonus_mult` (120). Paga solo si el anuncio se vio entero. Sonda `earn_virtual_currency` `source=double_harvest` |
| E21 | `GameScreen.cs` | `if (isShort && !flow.IsDaily)`: el Reto no enseña «Nivel N · intento k» |
| E22 | `GameBootstrap.cs` (opcional) | Atrás cierra el calendario del Reto; con el tablero en juego no hace nada |
| E23 | `GameBootstrap.cs` | Al volver de segundo plano, la vista del Reto salta al modelo, limpia la selección y reinicia la pista por inactividad |
| E24 | `DevProbe.cs` | `AppendDailyBoard(sb, flow);` tras las 4 líneas `"level"`: el Reto añade `"daily":"d{b}_n{nnnn}"` |
| tests | nuevo `Tests/PlayMode/DailyHooksTests.cs` (+ ajustes en `DailyChallengeTests`) | E2, E3, E4, E5, E9, E21-E24 de punta a punta con la escena real: el pool real, el almacén del perfil y las tablas |

### Verificación (r12-full, con todo aplicado)

| Comando | Resultado |
|---|---|
| `dotnet build -warnaserror` | código 0 |
| `dotnet test` | 981 (975 + 6) |
| `validate_schema.py` | 103/103 |
| `unity-compile.ps1 -Architecture arm64` | OK |
| EditMode | **3.082** (3.078 + 3; 1 fallo, el inestable `IntroArtTests.FruitFlight…`) |
| PlayMode | **1.394 (1.227 + 167), 0 fallos**; `PseudoLocOverflowTests` en-XA 130 % pasa tras el arreglo de la tarjeta |
| Capturas de R11 | 31 PNG, `AF_FIT` sin ningún texto fuera de su caja (es-419 a 780 no salió por un error del entorno con la ruta larga; se repite en `spark-int`) |

- **code-reviewer:** primera ronda CAMBIOS NECESARIOS (faltaban la sonda `double_harvest` y el test del anuncio cerrado antes de tiempo, y PlayMode tenía un fallo real). Segunda ronda **APROBADO**.

### Lo que no se preparó

- **E1** (`LobbyScreen`, opcional): no hace falta, porque la tarjeta va en su propio lienzo desde `GameBootstrap.Daily.cs`. Requisito si se mueve: en 16:9, al menos 8 dp bajo el bocadillo de TiTino (`r11_01_card_play_16x9`).
- **E6** (tarjeta de 2 s tras ganar el 7): falta una clave de texto nueva y el diseño de la tarjeta. Punto de enganche: `GameScreen.OnVictoryScreen` con `flow.LevelNumber == 7`.
- **E7** (Sin canoas, opcional): necesita una clave nueva, `af_no_canoes_choice = daily_challenge` y un flujo nuevo en AppFlow.
- **E16** (level-qa enlaza `Daily/`): el parche probado en el bloque (a) quedó desfasado tras R5. Hay que regenerarlo sobre la versión actual (descripción en su ficha).

### Para el dueño (E11; no bloquean)

- El Reto comparte con la campaña el tope de 5 al día y el cooldown de la Cosecha doble [P].
- Cuando exista la Crecida: ¿la Cosecha doble sin video del «río crecido» (04 §2.8) vale también en el Reto? Va con E19 y E25.

## 2026-10-19 · R0-R11 juntas: verificación completa VERDE · Bloque (d) (R8-R11) listo para integrar

- **Qué hay:** la rama principal (R0-R10) más R11, la nota con la tabla de días de 729 corregida (`09b11cd`) y los usos `--days 0-728`.
- **Resultados** (worktree `spark-int`):
  - `dotnet build -warnaserror` OK y `dotnet test` 981;
  - `validate_schema.py` 103/103;
  - `unity-compile` OK;
  - EditMode **3.057** (3.053 + 3, 1 fallo: el de tiempo de `IntroArtTests.FruitFlight…`, inestable y ajeno).
- **PlayMode: sin veredicto (código 6, licencia).** Hacia las 19:15 vuelve el 404 de derechos (`com.unity.editor.headless`). Es la 4.ª caída, de nuevo unas 2 h después de la última revisión del dueño.
- **Por separado sí hay PlayMode en verde:** R10 sobre R9 (1.354) y R11 sobre la integración con R5 (1.373). Falta la combinación de las dos.
- **PlayMode, tras la revisión de la licencia por el dueño:** **1.383 (1.216 + 167 omitidos)**, 0 fallos (43 min). Los omitidos incluyen los `[Explicit]` de capturas.
- `spark/reto-del-rio` avanza hasta esta combinación y se empuja. También entran los parches de documentación E11/E12 aprobados (`docs/dev/traspaso/spark-parches-docs.md`, `571fdc2`) y los textos 0-728 (`7501c5e`), que solo tocan documentos y textos de ayuda.
- **Recuentos de referencia del Spark:** EditMode 3.057; PlayMode 1.383.

## 2026-10-19 · R11 · Capturas y prueba de punta a punta: HECHA (APROBADO en 2.ª ronda; arreglo del sello en `171b404`)

- **Commit:** `41d5beb` en `spark/reto-del-rio-r11` (sale de la integración `8327d23`), más el arreglo del sello en curso.
- **Archivos:**
  - nuevo `Assets/_Project/Tests/PlayMode/DailyChallengeSnapshotTests.cs`: `[Explicit]`, `SnapshotKit`, 360 × 780 dp y 16:9, es-419 y en-XA;
  - arreglos en `DailyChallengeCard.cs` (título de 192 dp y estado en dos líneas) y `DailyStampCalendar.cs` (el sello que cae, por encima de las casillas vecinas; la caja del +60, de 42 dp).
- **Cómo se lanzan:** sin `-nographics` (`unity-test.ps1` lo fuerza):
  ```
  unity test . --mode PlayMode --architecture arm64 --filter AF.Tests.PlayMode.DailyChallengeSnapshotTests --output Logs/r11-snap.xml --non-interactive -- -logFile Logs/r11-snap.log
  ```
  - Los PNG van a `Logs/snapshots/es-419/` y `Logs/snapshots/en-XA/` (ignorado por git).
  - Cada captura deja en el log una línea `AF_FIT` por texto visible. Hoy dicen «0 textos fuera de su caja» en es-419, es-419 16:9, en-XA y en-XA 16:9.
  - Los textos salen de `strings.csv` + `facts.csv` + `docs/dev/traspaso/spark-strings-reto.csv`, como hará E5.
- **Verificación:** capturas 4/4; EditMode 3.057 (3.053 + 3, 1 fallo del test inestable `IntroArtTests`); PlayMode **1.373** (1.206 + 167 omitidos: los 4 `[Explicit]` nuevos), 0 fallos.

### Capturas y qué revisar (40 PNG: 10 × 2 formatos × 2 idiomas; los de 16:9 llevan `_16x9`)

| Archivo | Qué muestra | Qué revisar |
|---|---|---|
| `r11_01_card_play` | Inicio con la tarjeta «Reto del río · 19 oct», Jugar, 1 ola, 0/7 | La fecha corta por región; que el título no toque el sello. **En 16:9 la tarjeta queda pegada al bocadillo de TiTino (0-3 px): requisito de E1** |
| `r11_02_header` | Partida del Reto | Cabecera «Reto del río», sin «Nivel N» ni barra de Crecida. Ajeno: en en-XA, «Reiniciar» se sale del botón (`GameScreen`) |
| `r11_03_card_continue` | Tarjeta «Seguir» tras salir sin ganar | R-Q16 (a) |
| `r11_04_calendar_falling` | Octubre con sellos previos y el del 19 a media caída | Que el sello caiga **centrado en su casilla** (arreglado en `171b404`; `AF_STAMP`: desvío 0,00 dp); el contador del HUD ya marca 60 antes del «+60» |
| `r11_05_calendar_reward` | El sello posado y «+60» con la semilla | El «+60» queda algo a la izquierda del centro [P] |
| `r11_06_card_sealed` | Tarjeta «Sellado», 1/7 | El sello verde de los CTA con el aro crema (sin rojo; guía v2 §7) |
| `r11_07_calendar_month` | Calendario abierto desde la tarjeta sellada | Sin premio ni sello que cae; el hueco del «+60» queda reservado |
| `r11_08_calendar_pending` | Victoria con el servidor +48 h | «Premio pendiente de validar», sin +60, sello translúcido |
| `r11_09_card_pending` | Tarjeta «Pendiente de validar» | En en-XA, en dos líneas sin pisar la racha; ¿se lee bien el sello pendiente (disco claro)? |
| `r11_10_calendar_pororoca_locked` | Racha de 7 (del 12 al 19), +60 y «+1 Pororoca · se desbloquea en el nivel 30» | R-Q14 (a); en 16:9 el panel tapa la parte de arriba de la bandeja de comodines con el velo |

- **Además:** no hay cabecera de días de la semana en el calendario ([P] de R9); en el inicio de estas capturas no sale la pastilla de Canoas, por el perfil de prueba (`Wallet.NewProfile`), no por el Reto.
- **E1 (requisito nuevo):** en 16:9, dejar al menos 8 dp bajo el bocadillo de TiTino, o subir o compactar la tarjeta. Se comprueba con `r11_01_card_play_16x9`.

## 2026-10-19 · R10 · Sondas de desarrollo y analítica: HECHA

- **Commits:** `a99fe5d`, `b1c0a9b` (fusión de R9), `83e43dc` (los `.meta` que generó Unity) y `4025c1a` (arreglos de la revisión), en `spark/reto-del-rio-r10`; fusionados con `--no-ff` en `spark/reto-del-rio` (`8ab5346`).
- **Archivos:**
  - nuevo `Scripts/UI/DevProbe.Daily.cs`, un parcial de `DevProbe` que escribe por `DevProbe.Write` solo con `DevProbe.Enabled`;
  - cambios en `GameBootstrap.Daily.cs`, `DailyChallengeScreen.cs` y `DailyChallengeCard.cs` (archivos del Spark);
  - nuevo `Tests/PlayMode/DailyProbeTests.cs` (10 tests).
- **Sondas:**
  - `daily_challenge`: `win` al reclamar (nunca con `NoReward`); `fail` desde `DayEnded`, una vez por día;
  - `level_start`: al terminar Llenar y teñir, o al reanudar sin intro; una vez por partida ([P]: Volver a la orilla no lo repite);
  - `earn_virtual_currency`: al cobrar, y al abrir la app si se confirman cobros pendientes;
  - `daily_card`: en cada recálculo de la tarjeta.
  - `t_ms` es lo jugado desde que se abrió, y null si se reanudó [P]. La Pororoca no tiene evento (08 §8.2 no lo define).
- **Verificación** (worktree `spark-r10`): compila; EditMode 3.040 (3.036 + 3, 1 fallo del test inestable); **PlayMode 1.354 (1.195 + 159), 0 fallos**; el revisor, `DailyProbeTests` 10/10.
- **code-reviewer:** primera ronda CAMBIOS NECESARIOS (`level_start` salía antes de la intro, contra 08 §8.2, y faltaban tests); segunda ronda **APROBADO**.
- **E10:** no hace falta (todo sale de archivos del Spark). Queda E24 (`DevProbe.cs`, `AppendDailyBoard`).
- **Texto propuesto para `docs/dev/prototipo-v0.md` (E12):**
  > **Sondas del Reto del río** (R10; `DevProbe.Daily.cs`, formato en `AF.LiveOps/DailyProbeFormat.cs`). Solo en builds de desarrollo, como `AF_STATE` de una línea, con claves e identificadores:
  > - `{"event":"daily_card","t":…,"state":"hidden|in_progress|pending_validation|sealed|play","date":"AAAA-MM-DD","band":0-3,"day":…,"board":"d{b}_n{nnnn}"|null,"streak":n,"override":id|null,"override_ignored":bool}`: cada vez que se recalcula la tarjeta del inicio. Con la tarjeta oculta, `band` 0 y `board` null.
  > - `{"event":"level_start","t":…,"level_name":"d{b}_n{nnnn}","variant":"base","band":b}`: al terminar Llenar y teñir, o al reanudar sin intro; una vez por partida. Las líneas de partida (`load`, `ready`, `pour`…) llevan en `level` el nivel efectivo del Reto; con E24, además, `"daily":"d{b}_n{nnnn}"`.
  > - `{"event":"daily_challenge","t":…,"date":…,"result":"win|fail","t_ms":n|null,"murky_bonus":false,"streak_days":n}`: `win` al reclamar el premio; `fail` una vez por día, al descartar la partida de un día no ganado (R-Q15).
  > - `{"event":"earn_virtual_currency","t":…,"virtual_currency_name":"seeds","source":"daily_challenge","value":n}`: las semillas del premio, pagadas ahora.
  >
  > Para seguirlo: `adb logcat -s Unity | grep -E '"event":"(daily_card|level_start|daily_challenge|earn_virtual_currency)"'`.

## 2026-10-19 · R5, decisiones (R-Q12b, R-Q15, R-Q16), formato de sondas y R9 fusionados: verificación completa VERDE

- **Qué entra** en `spark/reto-del-rio`, por avance rápido desde `spark/reto-del-rio-int`:
  - **R5** (`71593ae`, `fbbb935`, `c266a46`, `c903537`, `d1fbe7e`, `0f436ef`): el pool publicado, 729 días × 3 bandas = 2.187 tableros, 1.430.135 bytes. code-reviewer: **APROBADO** (dos rondas).
  - **Rama de decisiones** (`42555cf`, `8115b74`, `f816d1c`, `e46348c`, `d0a9019`): R-Q16, R-Q15, R-Q12b y `DailyProbeFormat`. code-reviewer: **APROBADO** (dos rondas).
  - **R9** (`373c6e2`, `b0ada3a`, `0e7c276`, `fbb9893`, `896d894`, `b9a9e8c`, `d107d7e`): las pantallas. Los dos commits «Enganche:» son los asmdef de §3.3. code-reviewer: **APROBADO** (dos rondas).
  - La nota, al día (`85fe0a3`), y los enganches E21-E25 (`8327d23`).
- **Verificación** (worktree `spark-int`, `8327d23`):

  | Comando | Resultado |
  |---|---|
  | `dotnet build dotnet/AmazonFill.slnx -warnaserror` | código 0 |
  | `dotnet test dotnet/AmazonFill.slnx` | 981 (975 + 6) |
  | `python tools/validate_schema.py` | 103/103 |
  | `level-generator --self-test` (todas) | todo PASS |
  | `level-qa --daily Assets/_Project/Data/Daily/daily_pool_v1.json` | 2.187 tableros, **0 fallos**, 1.678 avisos (R-Q10) |
  | `unity-compile.ps1 -Architecture arm64` | OK |
  | `unity-test.ps1 -Mode EditMode … -TimeoutSec 3600` | **3.057 (3.054 + 3 omitidos)**, 0 fallos |
  | `unity-test.ps1 -Mode PlayMode … -TimeoutSec 3600` | **1.369 (1.206 + 163 omitidos)**, 0 fallos (41 min) |

- **Recuentos de referencia del Spark desde aquí:** EditMode 3.057; PlayMode 1.369. Los del integrador tras el bloque (a) eran 2.861 y 1.359.
- **Bloque (d)** (R8-R11, partida y pantallas): R8 y R9 están dentro; faltan R10 (sondas, en arreglos de revisión) y R11 (capturas, en curso).

## 2026-10-19 · ⚠ La licencia de Unity falla por 3.ª vez (pendiente del dueño)

- **Hacia las 16:45** los batchmode nuevos vuelven a salir con el código 6: `Found 0 entitlement groups`, `'com.unity.editor.headless' was not found`, `No valid Unity Editor license found`. Los que ya estaban en marcha (el PlayMode de `spark/reto-del-rio` y el de `spark-r9`) siguen corriendo.
- **Patrón:** falla entre 1 y 2 h después de cada revisión del dueño (12:10, 13:35 y 16:45). Esta vez no se reinició el proceso `--internal-auth-broker-serve` (el último es de las 14:38), así que la hipótesis de la 2.ª caída no se confirma. Lo más probable es que caduque el token de la sesión y no se renueve para el modo headless. Habría que mirar en Unity Hub si la licencia es una suscripción por sesión o una licencia Personal sin activación offline.
- **Qué queda frenado:** la verificación en Unity de la combinación de R5 con la rama de decisiones (worktree `spark-int`, `85fe0a3`).
  - La parte .NET y de herramientas ya está en verde: build sin avisos, `dotnet test` 981, `validate_schema` 103/103, `--self-test` del generador todo PASS, y `level-qa --daily` del pool de 2.187 tableros con 0 fallos y 1.678 avisos (R-Q10).
  - Las tres piezas pasaron Unity por separado: R5 (EditMode 2.995, 1 fallo del test inestable que pasa al relanzarlo), la rama de decisiones (3.023, ídem) y la rama principal (EditMode 3.006 en verde).
- **No he usado `unity auth` ni `unity license`** (regla dura 5).

## 2026-10-19 · Decisión del dueño: R-Q11b, 30 candidatos por fuente

- **La pregunta:** para que los 729 días de cada banda entren en la ventana del bot, R5 prueba hasta 30 candidatos **por fuente** y, si ninguno entra, pasa a la siguiente.
  - B1: `fill` E=2 → `deep` → `walk` → `deep` de 60.000 nodos, hasta 120 candidatos por día. Solo 23 días necesitan respaldo.
  - B2: `deep` → `fill` E=1, al revés que la tabla de 03 §4.4 para K ≤ 8. Con `fill` primero, 96 días salían por encima de la ventana.
- **El dueño respondió «Aceptar (Recomendado)»:** el «máximo 30 candidatos» de 03 §4.4 se lee como 30 por fuente, con respaldos en orden. No se relajan vacíos, K, capacidad ni ventanas.
- **Para el integrador (E11):** anotarlo en 03 §4.4 o §16 y en `decisiones.md`.
- **Nota sobre R-Q2b:** con `live_daily_offset` ≠ 0 (mod 9) el mundo sigue rotando a diario con la fecha, desplazado por el offset. Propuesta [P]: documentarlo en 08 §9, en la fila de `live_daily_offset` (E11).

## 2026-10-19 · Decisión del dueño: R-Q2b, pool de 729 días

- **El problema:** el agua y el fondo rotan «según la fecha» (R-Q2), pero con un pool de 730 días y 9 mundos (730 mod 9 = 1), un mundo grabado en cada tablero deja de seguir a la fecha a partir de la segunda vuelta.
- **Opciones presentadas:** el cliente calcula el mundo con la fecha local (cambia el significado de `water_base`, contrato 21); aceptar el salto; o un pool múltiplo de 9.
- **El dueño eligió** «Pool de 729 o 738 días» y, después, **«729 días (Recomendado)»**:
  - 81 × 9 = **729 días por banda, 2.187 tableros**;
  - el día del pool va de 0 a 728 y cabe en el esquema actual, sin tocar el contrato 21;
  - el índice pasa a `(días_desde(2026-01-01) + live_daily_offset) mod 729`;
  - el mundo del tablero es `1 + (día mod 9)`, que sigue a la fecha en todas las vueltas (con `live_daily_offset` constante, desplazado por ese offset).
- **Para el integrador (E11):** cambia 03 §16 y 06 §4.1, que dicen 730 y 2.190, y el pool de 08 §4.1 y §6.2.

## Resumen (2026-10-19)

- **Rama:** `spark/reto-del-rio`, en `752bb55`. Ya lleva fusionado `origin/integracion-fase1-fase2` (`6377345`).
- **Tareas hechas** (commits de la tarea · fusión en `spark/reto-del-rio`):

  | Tarea | Commits | Fusión | Revisión |
  |---|---|---|---|
  | R0 · Arranque y línea base | — (base `2db3a07`) | — | — |
  | R1 · Nota de diseño | `3e0063e`, `4968d1a` | (en la rama) | APROBADO (2.ª ronda) |
  | R2 · Especificación y autoprueba | `da6a9bb`, `99d35f7` | `3ca28dc` | APROBADO |
  | R3 · `--daily` del generador | `e8783b9`, `bb1dad2`, `ee48f52` | `c87a181` | APROBADO |
  | R4 · `level-qa --daily` | `32b8cc4`, `cafe8c4`, `ed109a3`, `8afdd0e` | `ba57160` | APROBADO (2.ª ronda) |
  | R6 · Lector del pool | `a724b43`, `28be4a2` | `afb58a2` | APROBADO (tras Unity) |
  | R7a · Calendario | `065d0e6` | `24d3f18` | APROBADO |
  | R7b · Premio, anti-reloj y racha | `a83ec1c`, `880f7dc` | `f0fb4c8` | APROBADO |
  | R8 · Partida del Reto en `LevelFlow` | `49501b4`, `535b92c` | `c0149b6` | APROBADO (Unity en verde) |
  | Enganche E15 + parte de E17 | `752bb55` | (en la rama) | — |

- **Bloques:**
  - **(a)** R0-R4 y R6: **integrado** en `integracion-fase1-fase2` (`fcbbd94`, `0d844cf`, `bc65242`) y traído de vuelta (`6377345`).
  - **(c)** R6-R7b (lector y lógica, sin pantalla): **listo para integrar**, verificado en `0a1b3ac`.
  - **R8:** fusionada en `spark/reto-del-rio` y verde en Unity (`535b92c`); sin integrar todavía.
- **Preguntas para el dueño:** **ninguna abierta.** R-Q1-R-Q10, R-Q1b y R-Q11-R-Q16 están decididas (entrada «Decisiones del dueño»). El integrador las lleva a `docs/dev/decisiones.md` (E11).
- **Qué falta:**
  - **R5** (en curso): R-Q1b en `daily_v1.json` (B2 `k: [8, 8]` sin `deep`, B3 sin tope), R-Q11 como error en el generador, R-Q2 con la fecha y no con el día del pool; rehacer la muestra y su informe; generar y publicar el pool (encargo §9, puntos 1-3).
  - **Aplicar en el código** las decisiones nuevas: R-Q12b en `DailyChallengeRules.Reconcile`; R-Q16 en `LevelFlow.ExitDaily` (hoy borra la partida); R-Q15 en la sonda (R10).
  - El pendiente menor de R7b (entrada de R7b).
  - R9 (pantallas), R10 (sondas), R11 (capturas y punta a punta) y R12 (enganche final: parches de E1-E7, E9, E10 y E12).
  - Enganches pendientes del integrador: E11, E13 (opcional), E14 (opcional), E16, E17 (README), E18 (punto 2), E19 (cuando exista la Crecida), E20.
- **Riesgo del entorno:** la licencia de Unity ha fallado dos veces (12:10 y 13:35, las dos con un `--internal-auth-broker-serve` nuevo de la CLI). Las dos se resolvieron; si vuelve, se avisa al dueño y no se fusiona nada sin pasar Unity.

---

## 2026-10-19 · Enganche E15 y parte de E17: HECHO (`752bb55`)

- **Commit:** `752bb55` «Enganche: incluye daily en el --self-test sin nombre del generador y documenta --daily en las dos herramientas», en `spark/reto-del-rio`. Punto 6 de las notas del integrador (encargo §9).
- **Archivos:** `tools/level-generator/Program.cs`, `tools/level-generator/README.md` y `tools/level-qa/Program.cs`.
  - E15 completo: el `--self-test` sin nombre del generador ejecuta también `daily`; `|daily` en `Usage`, «, holgura o daily» en el mensaje de error; `--daily` en el README y en la ayuda.
  - E17, la parte de `tools/level-qa/Program.cs`: `--daily` y `--self-test [key2|reglas|variante|daily]` en `Usage`, y el mensaje. **Queda el README de level-qa**, ligado a E16 (su texto describe QA-DAILY-REPRO con el proceso hijo de hoy).
  - Parches probados contra `origin/integracion-fase1-fase2` (`bc65242`).
- **Verificación:**
  - `dotnet run --project tools/level-generator -c Release -- --self-test` → **435 PASS, 0 FAIL**, exit 0 (incluye «daily»);
  - builds de `tools/level-generator` y `tools/level-qa` → sin avisos;
  - `dotnet run --project tools/level-qa -- --self-test bogus` → el mensaje de uso nombra «daily».

## 2026-10-19 · El integrador fusionó el bloque (a); fusión de vuelta en `spark/reto-del-rio` (`6377345`)

- **Integración** (encargo §9, «Bloque (a) integrado»): R0-R4 y R6 están en `integracion-fase1-fase2`:
  - `fcbbd94`: integra el bloque (a) (nota de diseño, especificación y generador del pool, QA del pool y lector en AF.Levels);
  - `0d844cf`: pone al día los recuentos;
  - `bc65242`: anota en el encargo la integración y lo que queda por ajustar.
- Revisado sin bloqueantes y verificado en el escritorio x64; la muestra de 30 tableros regenerada allí sale idéntica byte a byte.
- **Recuentos de referencia del integrador:** EditMode **2.861 (2.858 + 3)**, PlayMode **1.359 (1.196 + 163)**, `dotnet test` **981 (975 + 6)**.
- **Fusión:** `6377345` «Fusiona origin/integracion-fase1-fase2 en spark/reto-del-rio (bloque (a) integrado)», como pide el encargo §9.
- **Lo que pidió el integrador** (encargo §9, «Pendiente para ti»): puntos 1-3 → R5 (resumen); punto 4 (quitar «abierta» y [P] de lo decidido) y punto 5 (ordenar este archivo) → esta revisión de la nota y del estado; punto 6 → `752bb55`. También corrigió el punto 1 de §3.3 (E18).

## 2026-10-19 · Decisiones del dueño sobre las preguntas nuevas (R-Q1b, R-Q11-R-Q16)

El dueño respondió, literal: «aplica las recomendaciones y pon trabajo en paralelo para avanzar mas rapido». El integrador, en el encargo §5: «si tienen recomendada, vamos con esas». Se aplica la opción recomendada (la primera) de cada pregunta. El integrador debe llevarlas a `docs/dev/decisiones.md` junto con R-Q1-R-Q10 (E11). El integrador confirmó además que `IServerDate` y `FakeServerDate` van en **AF.Platform** (encargo §5).

| Id | Decisión |
|---|---|
| R-Q1b | (a) **B2 solo con K = 8** (cambia 03 §16 y 06 §4.1, que dan K 8-9) y **B3 con `max_pares_riesgo` libre**, como la campaña desde el 201 (03 §4.5). B1 sin cambios |
| R-Q11 | (a) La ventana del bot **es compuerta**: un día fuera de ventana no se publica hasta ajustar la especificación (fuente o K, que son [P]); QA-DAILY-BOT sigue siendo fallo |
| R-Q12 | (a) Al reconectar no se retira nada de lo cobrado sin red (`KeepOfflineClaims`, el default actual) |
| R-Q12 (aclaración) | (a) Al anular una fecha no se retiran semillas ni la Pororoca |
| R-Q12b | (a) Un pendiente solo se valida si `\|fecha − hoy_servidor\| ≤ 1 día`; los demás se anulan |
| R-Q13 | (a) Sin intersticial al terminar un Reto |
| R-Q14 | (a) La Pororoca de la racha entra en el stock y espera al nivel 30, en que se desbloquea |
| R-Q15 | (a) `af_daily_challenge.result = fail` **una vez por día**, y solo si ese día termina sin victoria |
| R-Q16 | (a) Salir de la partida del Reto sin ganar la deja **reanudable el mismo día**; al cambiar de día se descarta (R-Q4) |

**Estado en el código:** R-Q12, su aclaración, R-Q13 y R-Q14 (el pago) ya los cumple. Pendientes: R-Q1b y R-Q11 en el generador (R5); R-Q12b en `DailyChallengeRules.Reconcile`; R-Q16 en `LevelFlow.ExitDaily`; R-Q15 en la sonda (R10); el aviso de R-Q14 en la UI (R9). Detalle en la nota §1.6.

## 2026-10-19 · R8 · La partida del Reto en AF.GameFlow: HECHA (Unity en verde tras la revisión de la licencia)

- **Commits:** `49501b4` y `535b92c` (arreglos de la revisión), en `spark/reto-del-rio-r8` (sale de R7b). Empujada y, tras pasar Unity, **fusionada** en `spark/reto-del-rio` (`c0149b6`; la verificación se anotó en `9122e71`).
- **Archivos nuevos:** `Assets/_Project/Scripts/GameFlow/LevelFlow.Daily.cs` y `Assets/_Project/Tests/EditMode/DailyLevelFlowTests.cs` (18 tests), con sus `.meta`.
- **Cómo funciona (sin tocar archivos existentes):** `LevelFlow.CreateDaily(...)` monta un flujo propio. **E8 ya no hace falta como estaba previsto.**
  - Catálogo de un solo tablero, numerado con el nivel efectivo `max_level + 1` [P], así que los desbloqueos de `BoosterConfig` funcionan sin cambiar nada.
  - Progreso de solo lectura: ni `SaveWin` ni `SaveNextLevel`, y sin regalos de tutorial.
  - Cartera del Reto **obligatoria**, sobre el mismo `Profile`, con una copia de `EconomyConfig` (`eco_reward_*` = 0; `rescue_min_level` = ∞): sin canoas, sin rescate, sin fallo duro con coste y sin Cosecha.
  - Copia de `BoosterConfig` con la Marea no ilimitada y sin Cuia de rescate.
  - Sin `AdGate`, sin intersticial (R-Q13, decidida (a)) y `StateStore` propio; rechaza el de campaña.
  - Un oyente intermedio entrega el evento `DailyResult` (Win/Exit/Restart) sin depender de AF.LiveOps.
  - Los tests de reflexión fallan si `EconomyConfig` o `BoosterConfig` ganan una clave que no se copia.
- **Verificación:**
  - autor, antes de que fallara la licencia: compila; EditMode 2.991 (2.987 + 3 omitidos y 1 fallo del test inestable `IntroArtTests`, que pasa al relanzarlo); **PlayMode 1.334 (1.175 + 159), 0 fallos**;
  - tras los arreglos (`535b92c`): `dotnet build -warnaserror` sin avisos y un arnés .NET fuera del repositorio con 18/18;
  - **sobre `535b92c`, tras volver la licencia:** `unity-compile` OK; EditMode **2.996**: 2.992 + 3 omitidos y 1 fallo, el de tiempo de `IntroArtTests.FruitFlight…`, que al relanzarlo con `-Filter AF.Tests.EditMode.IntroArtTests` pasa 9/9; **PlayMode 1.334 (1.175 + 159), 0 fallos**. Fusionada en `spark/reto-del-rio`.
- **code-reviewer:** primera ronda CAMBIOS NECESARIOS. Los tres bloqueantes:
  - sin cartera, el Reto caía en el embudo de D-008;
  - el comentario sobre la Crecida era falso;
  - el JSON interno no cumple af-level/1 y no lo decía.

  Segunda ronda: **APROBADO a falta de Unity** (y Unity, después, en verde).
- **[P]:** nivel efectivo `max_level + 1`, congelado al empezar; sin regalos de tutorial; Marea con las 3 gratis y luego el stock; modal de Aguas estancadas con Marea + Volver a la orilla; `DailyAttempt` vuelve a 1 al reanudar.
- **Ya no son [P]:** sin intersticial (R-Q13 (a)). Salir **borra** hoy la partida guardada del Reto, pero R-Q16 (a) pide dejarla **reanudable el mismo día**: pendiente en `ExitDaily` (`StateStore?.Clear()`).
- **Enganches nuevos:** E19 (guarda de la Crecida en `FailResolve`) y E20 (`WithLevel` y `LevelCatalog.ForBoard`), con sus parches en «Enganches para el integrador».
- **Notas para R9 y R10:**
  - `LevelNumber` es el nivel efectivo: no se enseña como «Nivel N» ni dispara tutoriales ni sondas de campaña; se usa `DailyBoardId`.
  - El HUD del Reto se suscribe a la cartera del Reto, y el saldo de la campaña hay que refrescarlo al volver.
  - La UI llama a `ExitDaily`, no a `AppFlow.ExitToLobby`.
  - R-Q15: `DailyPlayOutcome.Exit` llega en cada salida; la sonda emite `fail` una sola vez por día, y solo si el día termina sin victoria.

## 2026-10-19 · ⚠ La licencia de Unity vuelve a fallar (2.ª vez; resuelto: R8 pasó Unity después)

- **Hacia las 13:35** los batchmode vuelven a salir con el código 6, con los mismos errores que la primera vez:
  - `Code 404 … Found 0 entitlement groups and 0 free entitlements`;
  - `'com.unity.editor.headless' was not found`;
  - `No valid Unity Editor license found`.
- **Patrón:** las dos veces, justo antes del fallo, arrancó un proceso nuevo de la Unity CLI, `unity.exe --internal-auth-broker-serve` (PID nuevo a las 12:09 y a las 13:35). Parece que, al renovar la sesión, la CLI se queda sin derechos para el modo headless. La pasada larga que acababa de terminar (bloque c, PlayMode 1.334) salió bien porque arrancó antes.
- **Qué afectó:** la verificación en Unity de R8, que se hizo al volver la licencia (entrada de R8).
- **No he usado `unity auth` ni `unity license`** (regla dura 5).

## 2026-10-19 · Bloque (c) (R6-R7b: lector y lógica del Reto, sin pantalla): listo para integrar

Verificación completa de `spark/reto-del-rio` en `0a1b3ac`:

| Comando | Resultado |
|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | código 0 |
| `dotnet test dotnet/AmazonFill.slnx` | 981 (975 + 6), código 0 |
| `python tools/validate_schema.py` | 103/103 |
| `unity-compile.ps1 -Architecture arm64` | OK |
| `unity-test.ps1 -Mode EditMode … -TimeoutSec 3600` | **2.978 (2.975 + 3 omitidos)**, 0 fallos |
| `unity-test.ps1 -Mode PlayMode … -TimeoutSec 3600` | **1.334 (1.175 + 159 omitidos)**, 0 fallos |

En total, 46 min. El bloque solo añade archivos nuevos: AF.LiveOps y sus tests, `DailyPool*` en AF.Levels y `IServerDate`, `FakeServerDate` y `ClockSecurityConfig` en AF.Platform. Ningún asmdef existente cambia todavía; `AF.UI.asmdef` += `AF.LiveOps` llegará con R9.

## 2026-10-19 · R7b · Premio, anti-reloj y racha: HECHA

- **Commits** (en `spark/reto-del-rio-r7b`, fusionados con `--no-ff` en `spark/reto-del-rio` como `f0fb4c8`):
  - `a83ec1c`: sugerencias de la revisión de R7a;
  - `880f7dc`: R7b.
- **Archivos nuevos:**
  - `Scripts/Platform/IServerDate.cs`, `FakeServerDate.cs` y `ClockSecurityConfig.cs`, donde ahora vive `sec_clock_max_drift_h`. Así es la única fuente y AF.Economy (Canoas) también puede leerla;
  - `Scripts/LiveOps/IDailyChallengeStore.cs`, `DailyChallengeRecord.cs`, `InMemoryDailyChallengeStore.cs`, `DailyChallengeRules.cs` y `DailyChallengeService.cs`;
  - tests (`DailyFixture`, `DailyChallengeRulesTests` 30, `DailyChallengeServiceTests` 18, `DailyChallengeStoreTests` 10).
- **Cambios en R7a:** `DailyOverrides.TryParse` (un valor no válido se queda en `{}`); `DailySlot.RequestedOverrideId` e `IsOverrideIgnored`, para que la sonda anote una sustitución ignorada.
- **Verificación** (worktree `spark-r7b`):
  - `unity-compile.ps1` → OK;
  - EditMode → **2.978 (2.975 + 3 omitidos)**, 0 fallos (70 tests nuevos);
  - `dotnet build -warnaserror` → 0 avisos.
  - El revisor lo repitió: 2.974 + 3 y 1 fallo, el de tiempo de `IntroArtTests.FruitFlight…` (inestable y ajeno); todos los de LiveOps en verde. Además, `dotnet test` 981 y `validate_schema.py` 103/103.
- **code-reviewer:** **APROBADO** a la primera. Comprobó contra 06 §4.1 y 08 §10.1:
  - nunca dos premios por fecha ni premio con el reloj atrasado;
  - «pendiente» solo con red y desfase de más de 24 h;
  - el orden registrar → guardar → pagar;
  - la racha y la Pororoca en los días 7 y 14;
  - los sellos del mes;
  - R-Q3, R-Q4 y R-Q9;
  - R-Q12 detrás de `DailyReconcileMode`, con las dos ramas probadas;
  - los estados de la tarjeta en el orden de la nota §8.
- **[P] de R7b:**
  - `pending` y `offline_claims` son listas de `{date, streak}`: la nota §5 y §10 ya está al día;
  - anular en la reconciliación deja la racha a 0 y quita el sello;
  - salir de un día ya ganado no emite `fail`;
  - `LevelStateSnapshot.Copy` va duplicada en AF.LiveOps, con un test por reflexión.
- **Lo que era [P] y ya está decidido:**
  - R-Q12 por defecto en (a), `KeepOfflineClaims`: **decidida (a)**;
  - anular no retira semillas: **decidida** (aclaración de R-Q12, (a): ni semillas ni la Pororoca);
  - salir sin ganar cierra la partida, que no queda reanudable: **R-Q16 decidió lo contrario** ((a), reanudable el mismo día): pendiente en el código;
  - la validación de pendientes con fecha ≤ hoy del servidor: **R-Q12b (a)** pide `|fecha − hoy_servidor| ≤ 1 día`: pendiente en `Reconcile`.
- **Pendiente menor** (va en un commit posterior):
  - documentar en `IDailyChallengeStore` que el registro y la cartera **tienen que vivir en el mismo `Profile`**, porque si no la garantía de «nunca dos pagos» se pierde (requisito de E4);
  - acotar `offline_claims`;
  - que `Reconcile` guarde solo si cambia algo;
  - la cita del test en `DailyChallengeRecord.cs:66`;
  - para R9: tras anular un pendiente con el reloj adelantado, la tarjeta sale «Sellada».

### Preguntas nuevas para el dueño (de la revisión de R7b): TODAS DECIDIDAS

| Id | Pregunta | Opciones (recomendada primero) | Qué bloqueaba | Estado |
|---|---|---|---|---|
| **R-Q12b** | Al validar un premio pendiente, ¿qué fechas se aceptan? Hoy se paga todo pendiente con fecha ≤ la del servidor, así que con red y el reloj atrasado a días no jugados se acaban pagando esos días. 06 §4.1 compara «> 1 día» | (a) solo si `\|fecha − hoy_servidor\| ≤ 1 día`; las demás se anulan; (b) toda fecha ≤ hoy del servidor (lo de ahora) | La rama de validación de R7b (es una línea) | **DECIDIDA: (a)** |
| **R-Q15** | `af_daily_challenge.result = fail`: ¿una vez por cada salida sin ganar, una por día, o solo si el día termina sin victoria? (R-Q9) | (a) uno por día, solo si ese día no se gana; (b) uno por salida (lo de ahora) | La sonda de R10 | **DECIDIDA: (a)** |
| **R-Q16** | Salir de la partida del Reto sin ganar: ¿la cierra (lo de ahora: solo se reanuda si se cierra la app; R-Q4) o la deja reanudable el mismo día? | (a) reanudable el mismo día; (b) se cierra | R8/R9 | **DECIDIDA: (a)** |
| **R-Q12** (aclaración) | En la rama (b) de R-Q12, ¿se retiran las semillas de una fecha anulada? El código no retira ni semillas ni la Pororoca | (a) no se retira nada; (b) se retiran las que queden sin gastar | Solo la rama (b) | **DECIDIDA: (a)** |

## 2026-10-19 · R7a · Calendario del Reto en AF.LiveOps: HECHA

- **Commit:** `065d0e6`, en `spark/reto-del-rio-r7a`; fusionado con `--no-ff` en `spark/reto-del-rio` (`24d3f18`).
- **Archivos nuevos** en `Assets/_Project/Scripts/LiveOps/`:
  - `AF.LiveOps.asmdef` (sin motor; `autoReferenced: false`; referencia AF.Core, Levels, Platform, Save y Economy);
  - `DailyChallengeConfig.cs` (las 9 claves de 08 §9 con sus defaults);
  - `DailyOverrides.cs` (`{fecha: {banda: id}}`, R-Q6);
  - `DailyCalendar.cs` (día del pool con módulo positivo, banda, desbloqueo, sustituciones y `DailySlot`);
  - `Tests/` (asmdef y 57 tests, entre ellos uno que lee la tabla de 08 §9 del Markdown y compara nombres, tipos y defaults).
- **Verificación** (worktree `spark-r7a`):
  - compila;
  - EditMode → 2.908 (2.905 + 3 omitidos), 0 fallos, en la pasada del revisor; en la del autor, 1 fallo del test inestable `IntroArtTests`, que pasa al relanzarlo;
  - `dotnet test` 981 y `validate_schema.py` 103/103.
- **code-reviewer:** **APROBADO** a la primera, sin bloqueantes. Las sugerencias (`TryParse`, sustitución ignorada visible, `sec_clock_max_drift_h` en AF.Platform) se aplicaron en R7b.
- **Para el integrador:** 08 §9 sigue diciendo `daily_overrides` = `json {fecha: level_id}`; R-Q6 lo cambia a `{fecha: {banda: id}}` (E11). El test solo compara el tipo base `json` y el default `{}`.

## 2026-10-19 · Verificación completa de `spark/reto-del-rio` en `ba57160` (R0-R4 y R6): VERDE

| Comando | Resultado |
|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | código 0, 0 avisos |
| `dotnet test dotnet/AmazonFill.slnx` | 981 (975 + 6 omitidos), código 0 |
| `python tools/validate_schema.py` | 103/103 PASS |
| `dotnet run --project tools/level-generator -- --self-test daily` | todo PASS |
| `dotnet run --project tools/level-qa -- --self-test daily` | todo PASS |
| `scripts/unity-compile.ps1 -Architecture arm64` | OK |
| `scripts/unity-test.ps1 -Mode EditMode -Architecture arm64 -TimeoutSec 3600` | **2.851 (2.848 + 3 omitidos)**, 0 fallos: la línea base de 2.744 más 107 de R6 |
| `scripts/unity-test.ps1 -Mode PlayMode -Architecture arm64 -TimeoutSec 3600` | **1.334 (1.175 + 159 omitidos)**, 0 fallos, igual que la línea base (32 min) |

En total, 45 min. Durante parte del tiempo hubo otro batchmode en un worktree: como mucho 2-3 a la vez, en carpetas distintas, con 64 GB de RAM (el encargo §1 permite 3 con 32 GB o más).

## 2026-10-19 · R6 · Lector del pool en AF.Levels: HECHA

- **Commits:** `a724b43` (el lector y 66 tests) y `28be4a2` (los arreglos de la revisión y 41 tests más), en `spark/reto-del-rio-r6`; fusionados con `--no-ff` en `spark/reto-del-rio` (`afb58a2`).
- **Archivos nuevos:**
  - `Assets/_Project/Scripts/Levels/DailyPool.cs` y `DailyPoolReader.cs`;
  - `Assets/_Project/Tests/EditMode/DailyPoolTestPacks.cs` y `DailyPoolReaderTests.cs` (107 tests);
  - sus `.meta`, generados por Unity.
- **Qué hace:**
  - `DailyPoolReader.Read(json)` es estricto: pide las 3 bandas con 730 días cada una. `Read(json, DailyPoolCoverage.PresentBands)` existe solo para tests y herramientas.
  - Rechaza con `LevelFormatException`: `kind` distinto de `daily`; `pack_schema` fuera de 1-3; `slot`, `id` o `seed` incoherentes; `level`; `solution`, `safe_path` o `qa`; `relief` que no está vacío; `world` fuera de 1-13; bandas incompletas; repetidos.
  - [P] Rechaza también mecánicas, `murky`, `hidden` e `intro_mechanic` (nota §13).
  - `Find(band, day)`, `FindById`, `BandOf` y `DayOf` no asignan memoria (comprobado con `GC.GetAllocatedBytesForCurrentThread`).
  - El revisor leyó los 30 tableros de la muestra de R3 con el lector: todos se construyen y validan. Un tablero literal de R3 (`d1_n0000`) va en un test, para que un cambio de formato del generador rompa el cliente.
- **Verificación** (worktree `spark-r6`, ARM64):
  - `unity-compile.ps1` → OK;
  - `unity-test.ps1 -Mode EditMode -TimeoutSec 3600` → **2.851 (2.848 + 3 omitidos), 0 fallos**;
  - `dotnet build … -warnaserror` → 0 avisos.
- **code-reviewer:** primera ronda CAMBIOS NECESARIOS (faltaban tests de varias reglas y se aceptaba `world` 0). Segunda ronda: código bien, pero la aprobación quedó condicionada a pasar Unity, que estaba bloqueado por la licencia. Pasó en verde tras la revisión del dueño: **APROBADO**.
- **Test inestable** (no es del Spark): `IntroArtTests.FruitFlight_Has14DistinctSprites_InOneTexture_GeneratedWithin10ms` falló una vez por tiempo (10,42 ms con un tope de 10 ms) en este equipo ARM64. Al relanzarlo con filtro pasó.
- **Enganche opcional E14** (extraer los ayudantes que `DailyPoolReader` copia de `LevelPackReader`): en «Enganches para el integrador».

## 2026-10-19 · R4 · QA del pool en `level-qa --daily`: HECHA · Bloque (a) listo para integrar

- **Commits** (en `spark/reto-del-rio-r4`, fusionados con `--no-ff` en `spark/reto-del-rio` como `ba57160`):
  - `32b8cc4`: detalles de la revisión de R3;
  - `cafe8c4`: la QA;
  - `ed109a3`: «Enganche:» del despacho en `tools/level-qa/Program.cs`;
  - `8afdd0e`: arreglos de la revisión.
- **Archivos nuevos:**
  - `tools/level-qa/Daily/DailyChecks.cs` (21 ids `QA-DAILY-*`) y `DailyQaCommand.cs`;
  - `tools/level-qa/Daily/Shared/DailySpec.cs` y `DailyIds.cs`: copias byte a byte de las del generador; la autoprueba falla si divergen (E16 las sustituye por enlaces);
  - `tools/level-qa/fixtures/daily/`: 1 fixture válido y 18 malos.
- **Uso:**
  ```
  dotnet run --project tools/level-qa -- --daily <pack> [--spec <daily_v1.json>] [--partial] [--no-reproduce]
  ```
  `--self-test daily` prueba todos los fixtures.
- **Severidad:**
  - **fallo:** esquema (vía `validate_schema.py --pack`), slot ↔ id/seed, 730 días por banda, K/vacíos/capacidad, ventana del bot (R-Q11, decidida (a): es compuerta), A*, par ≥ 2K, tope de pares **por banda según la especificación** (R-Q1b, decidida (a): B3 sin tope), sin mecánicas ni ocultos, `relief` vacío y campos prohibidos, ≤ 14 cuias, < 2 MB (siempre), y que la semilla reproduzca el tablero entero (proceso hijo del generador, 15 min de tope);
  - **solo aviso (R-Q10):** anti-repetición (forma y Hamming, con la vuelta del ciclo 729 → 0) y trampas tempranas.
- **Detalles:**
  - Con `--partial` una muestra nunca pasa por pool completo: siempre sale el aviso «no es el pool completo».
  - El despacho de `Program.cs` va antes del `--self-test` genérico, por la misma razón que en R2.
- **Verificación:**
  - `level-qa --self-test daily` → 30 PASS; las autopruebas que ya había → todo PASS;
  - QA de la muestra con `--partial` → exit 1: 15 `QA-DAILY-RISK` (los K 9 de B2 y todo B3) y 2 `QA-DAILY-BOT` (`d2_n0002`, `d2_n0006`). Era lo esperado mientras R-Q1b y R-Q11 seguían abiertas; con R-Q1b (a) en la especificación (B2 sin K 9, B3 sin tope) los RISK dejan de aplicarse, y los BOT siguen siendo fallo (R-Q11 (a));
  - QA del pool completo (generado fuera del repositorio) → exit 1:

    | Id | Fallos | Avisos | Por banda |
    |---|---|---|---|
    | QA-DAILY-RISK | 1.095 | — | B2: 365 · B3: 730 |
    | QA-DAILY-BOT | 68 | — | B1: 13 · B2: 55 |
    | QA-DAILY-REPEAT-SHAPE | — | 414 | B1: 133 · B2: 103 · B3: 178 |
    | QA-DAILY-EARLY-TRAPS | — | 1.172 | B1: 353 · B2: 669 · B3: 150 |

    Pasan DAYS, SIZE, SCHEMA y REPRO (los 2.190 tableros se reproducen).
  - builds con `-warnaserror` → 0 avisos; `dotnet test` → 981 (975 + 6); `validate_schema.py` → 103/103.
- **code-reviewer:** primera ronda CAMBIOS NECESARIOS (faltaban los tests de los avisos de R-Q10, y `--partial` apagaba la compuerta de tamaño). Arreglado; segunda ronda **APROBADO**.
- **Para el dueño (R-Q10):** EARLY-TRAPS avisa en 1.172 de 2.190 tableros, porque con 1 vacío y la etiqueta «normal» el tope de la campaña es 0. Si algún día pasa a compuerta, el Reto necesitará su propio tope.

### Bloque (a) (R2-R4: el modo diario del generador y su QA): listo para integrar (ya integrado: `fcbbd94`)

Está en `spark/reto-del-rio` y no toca Unity. Sus enganches (E15-E18) están en «Enganches para el integrador».

## 2026-10-19 · Licencia de Unity: RESUELTO

El dueño revisó la licencia en Unity Hub. Desde entonces Unity compila y pasa los tests (R6 en verde). La entrada de abajo queda como historial.

## 2026-10-19 · ⚠ BLOQUEO: la licencia de Unity dejó de valer en este equipo (resuelto: entrada de arriba)

- **Desde** unas 12:10 (hora local) ningún batchmode arranca: `unity-compile.ps1` y `unity-test.ps1` salen con el código 6.
- **Log** (`.claude/worktrees/spark-r6/Logs/compile.log`):
  - `Error: Code 404 … Found 0 entitlement groups and 0 free entitlements matching requested entitlement ids`;
  - `'com.unity.editor.headless' was not found`;
  - `No valid Unity Editor license found`.
- **Qué no es la causa:** el aviso «Code 10 while verifying Licensing Client signature» ya salía en las pasadas buenas de la línea base (10:55 y 11:33), cuando sí se resolvían los derechos. Unity Hub (3.22.3) y la CLI (1.0.0-beta.13) no han cambiado de versión.
- **Hipótesis:** se retiró o caducó el derecho de la licencia en el servidor. Por ejemplo, el asiento está en uso en otro equipo, o la licencia no incluye `com.unity.editor.headless`.
- **No he usado `unity auth` ni `unity license`** (regla dura 5).
- **Lo que necesitaba del dueño:** revisar la licencia de Unity de este equipo en Unity Hub (Preferencias › Licencias). Cuando volviera a funcionar, repetir las pruebas de Unity pendientes:
  - R6 en el worktree `spark-r6`: compilar y pasar EditMode (esperado 2.851: 2.848 + 3 omitidos);
  - todo lo que hubiera del cliente desde R7.
- **Qué afectaba:** todo el carril del cliente (R6-R11) quedaba sin verificar en Unity. Seguí escribiendo el código y comprobándolo fuera de Unity, sin darlo por terminado ni fusionarlo hasta pasar Unity. El carril de herramientas (R2-R4) no lo necesita.

## 2026-10-19 · R3 · Modo `--daily` del generador: HECHA

- **Commits** (en `spark/reto-del-rio-r3`, fusionados con `--no-ff` en `spark/reto-del-rio` como `c87a181`):
  - `e8783b9`: arreglos menores de la revisión de R2;
  - `bb1dad2`: el modo `--daily`;
  - `ee48f52`: la muestra `tools/out/daily_sample/daily_pool_v1.json` (30 tableros, 20.144 bytes, SHA-256 `61cf6bef…3ac1`) y `tools/out/daily_sample_report.md`. Se commitea como las otras muestras de `tools/out/`.
- **Archivos nuevos** en `tools/level-generator/Daily/`: `DailyRunner.cs`, `DailyTints.cs` (copia adaptada de `TintAssigner`, que no cambia), `DailyPackWriter.cs`, `DailyReport.cs` y `DailyCommand.Generate.cs`.
- **Uso:**
  ```
  dotnet run --project tools/level-generator -- --daily --spec tools/level-generator/specs/daily_v1.json --bands 1,2,3 --days 0-729 --out <dir> [--report <md>] [--check] [--threads N]
  ```
  Códigos de salida: 0 (también con avisos), 1 (un día sin tablero o `--check` distinto), 2 (uso).
- **Verificación:**

  | Comando | Resultado |
  |---|---|
  | `--daily` de la muestra (bandas 1-3, días 0-9) | exit 0; `--check` idéntico con 20 hilos y con 1 |
  | `python tools/validate_schema.py --pack tools/out/daily_sample/daily_pool_v1.json` | PASS |
  | `python tools/validate_schema.py` | 103/103 |
  | `--self-test daily` | todo PASS: 3 días por banda idénticos byte a byte en paralelo y con 1 hilo; la semilla reproduce `d2_n0001` |
  | `--self-test` general | todo PASS |
  | `dotnet build` de `tools/level-generator` y de `dotnet/AmazonFill.slnx` con `-warnaserror` | 0 avisos |
  | `dotnet test` | 981 (975 + 6 omitidos) |

- **Pool completo** (3 × 730), generado fuera del repositorio: 6,1 s con 20 hilos y 1.445.059 bytes. Valida con `--pack` y es reproducible. **No se publica** (R5).
- **Muestra, por banda (10 días cada una):**

  | Banda | bot_fail mín · mediana · máx | par mín · mediana · máx | Fuera de ventana | Pares > tope |
  |---|---|---|---|---|
  | B1 | 0,400 · 0,438 · 0,600 | 13 · 16,5 · 21 | 0 | 0 |
  | B2 | 0,625 · 0,725 · 0,900 | 19 · 20,5 · 25 | 2 (`d2_n0002` 0,85, `d2_n0006` 0,90; los dos K 8 con `fill`) | 5 (los de K 9) |
  | B3 | 0,750 · 0,950 · 1,000 | 21 · 22,5 · 25 | 0 | 10 (todos) |

- **En el pool completo:**
  - B1: 13 días fuera de ventana (mínimo 0,30) y 231 con el respaldo `fill` E=2;
  - B2: 55 fuera de ventana y 365 por encima del tope de pares;
  - B3: 0 fuera de ventana y los 730 por encima del tope de pares.
- **code-reviewer:** APROBADO a la primera. Reprodujo la muestra byte a byte, comprobó el SHA y cruzó las 30 filas del informe con el pack (0 discrepancias). Las sugerencias menores van en R4.
- **[P] de R3:**
  - cabecera con `empties_default` 1 y `max_risk_pairs` igual al máximo real, así que R4 comprueba el tope por banda contra la especificación;
  - `world` en cada tablero (opcional en `daily`);
  - un tablero por línea, como `PackWriter`;
  - `skippable` false (04 §2.6 e) y `hint_supported` true (06 §4.1; R-Q8).
- **Lo que era [P] y ya está decidido:**
  - R-Q1b: si el tope no admite el K, el tablero lleva el **mínimo de pares posible** y queda marcado, sin cambiar K ni paleta. Con R-Q1b (a) (B2 con K = 8, mínimo 1 = tope; B3 sin tope) el caso deja de darse;
  - R-Q11: si ningún candidato entra en la ventana, se guarda **el más cercano** y queda marcado. Con R-Q11 (a) pasa a ser **error** del generador (R5, en curso; encargo §9, punto 2).
- **Datos de R-Q11 (ya decidida):**
  - B3 sale casi siempre ≥ 0,90 (mediana 0,95): entra en «≥ 75 %», pero son tableros muy difíciles;
  - B2 con K 8 y `fill` se pasa de la ventana en 55 de 730 días.

## 2026-10-19 · R2 · Especificación del pool y autoprueba: HECHA

- **Commits:** `da6a9bb` (especificación, lector, ids y autoprueba) y `99d35f7` («Enganche:» del despacho en `tools/level-generator/Program.cs`), en `spark/reto-del-rio-r2`; fusionados con `--no-ff` en `spark/reto-del-rio` (`3ca28dc`).
- **Archivos nuevos:** `tools/level-generator/specs/daily_v1.json`, `tools/level-generator/Daily/DailySpec.cs`, `DailyIds.cs`, `DailyCommand.cs`.
- **Verificación** (en el worktree `spark-r2`):
  - `dotnet run --project tools/level-generator -- --self-test daily` → 86 PASS, código 0;
  - `dotnet run --project tools/level-generator -c Release -- --self-test` (las autopruebas que ya había) → 314 PASS y todas «todo PASS», código 0, 49 s;
  - `dotnet build tools/level-generator -warnaserror` → 0 avisos; `dotnet build dotnet/AmazonFill.slnx -warnaserror` → 0 avisos, código 0;
  - `dotnet test dotnet/AmazonFill.slnx` → 981 (975 + 6 omitidos), código 0;
  - `python tools/validate_schema.py` → 103/103 PASS, sin cambios en `tools/out/schema_example_08_4_1.json`;
  - Unity no aplica (la tarea solo toca `tools/`).
- **code-reviewer:** APROBADO a la primera, sin bloqueantes. Comprobó las bandas de 03 §16, las fuentes de 03 §4.4, el agua del glosario §8, las regex del esquema (la autoprueba las lee del esquema) y los 2.190 ids y semillas. Sugerencias menores (estado de la paleta de B1 y un mensaje de error) van en R3.
- **Desviación del encargo (§3.3, punto 1):** el bloque del despacho va **antes del `--self-test` genérico** (tras `--validate-spec`), no justo antes de `--parity`: el genérico solo reconoce subcomandos fijos y daría error de uso con `--self-test daily`. Son 5 líneas, en su commit «Enganche:». El revisor lo da por correcto y necesario. El integrador ya lo corrigió en el encargo §9 (E18).
- **Errata:** el mensaje de `da6a9bb` dice «43 especificaciones mal formadas»; son 44. No lo reescribo.
- **[P] en `daily_v1.json`:** regla del agua `1 + (día mod 9)` (R-Q2; R5 la cambia a la fecha, encargo §9, punto 3) y `k_by_day` (día par → K menor, impar → mayor). Los tintes y los pares de riesgo se leen de `campaign_v2.json` (`tints_from`), sin copiarlos.
- **Lo que era [P] y ya está decidido:** paleta de B2 y B3 (R-Q1b (a)) y lo que haga el generador con los días fuera de ventana (R-Q11 (a)); los dos se aplican en R5.

## 2026-10-19 · R1 · Nota de diseño: HECHA

- **Commits:** `3e0063e` (la nota) y `4968d1a` (junta dos comentarios duplicados), en `spark/reto-del-rio`.
- **Archivo:** `docs/dev/reto-del-rio.md`.
- **code-reviewer:** primera ronda CAMBIOS NECESARIOS (6 bloqueantes: el contrato del almacén no seguía a `ILevelStateStore`; la ventana del bot era a la vez aviso y compuerta; la reconciliación sin red se daba por regla; las opciones de R-Q1b estaban incompletas; faltaba dónde viven los tests que ven AF.LiveOps; cinco citas imprecisas). Arreglados todos; segunda ronda **APROBADO**.
- **Para el integrador:**
  - `IServerDate` y `FakeServerDate` van en **AF.Platform** (08 §2.2 le da el «`Date` del servidor para el reloj») y no en AF.LiveOps, como decía el encargo §4. **Confirmado** por el integrador (encargo §5). Nota §1.2.
  - Las decisiones R-Q1-R-Q10 llevan fecha 2026-10-20 en la primera versión del encargo; el reloj de este equipo dice 2026-10-19 (el encargo §5 dice ahora 2026-10-19). Confírmalo al llevarlas a `decisiones.md`.
  - Ediciones del GDD que piden R-Q4-R-Q8 (08 §4.3, 08 §9 tipo de `daily_overrides`, 06 §4.1): nota §1.3 y enganche E11.

### Preguntas nuevas para el dueño (nota §1.6): TODAS DECIDIDAS

| Id | Pregunta | Opciones (recomendada primero) | Qué bloqueaba | Estado |
|---|---|---|---|---|
| **R-Q1b** | R-Q1 no se puede cumplir: B2 con K = 9 tiene 3 pares de riesgo (tope 1) y B3, con los tintes de 1-200, al menos 3 con K = 10 y 4 con K = 11 (tope 2). Cálculo en la nota §1.1 | (a) B2 solo con K = 8 y B3 con `max_pares_riesgo` libre, como la campaña desde el 201; (b) subir los topes: B2 a 3 y B3 a 4; (c) mantener los topes y bajar K (B2 = 8, B3 = 9); (d) ampliar la paleta de B3 a 1-300 (no basta) | Paleta de R3 (va como parámetro [P]) y la publicación del pool (R5) | **DECIDIDA: (a)** |
| **R-Q11** | Si en 30 candidatos ningún tablero entra en la ventana del bot de su banda (03 §4.4 midió 0,88-1,00 de fallo con 1 vacío; B2 pide 60-80 %) | (a) la ventana es compuerta: el día no se publica hasta ajustar la especificación; (b) se publica el más cercano y QA avisa; (c) ensanchar la ventana (cambia 03 §16) | R5 | **DECIDIDA: (a)** |
| **R-Q12** | Al reconectar, qué hace la reconciliación con las victorias cobradas sin red (08 §10.1) | (a) nada: no se retiran semillas; (b) anular las de fecha posterior a la del servidor (sello y racha) | Solo esa rama de R7b (irá detrás de una opción) | **DECIDIDA: (a)** |
| **R-Q13** | ¿Intersticial al terminar un Reto? (05 §6.1 solo habla de la campaña) | (a) no; (b) como en la campaña | R8/R9 | **DECIDIDA: (a)** |
| **R-Q14** | La Pororoca de la racha puede llegar a B1 antes del nivel 30, que la desbloquea (04 §2.4) | (a) entra en el stock y espera al 30; (b) otro premio en B1; (c) nada antes del 30 | Solo un aviso en la UI de R9 | **DECIDIDA: (a)** |

## 2026-10-19 · R0 · Arranque y línea base: HECHA (desbloqueada)

El dueño pidió «instala lo necesario para trabajar». Instalado:
- `jsonschema` 4.26.0 en Python 3.14 (`py -3.14 -m pip install jsonschema`);
- .NET SDK 10.0.401 ARM64 (`winget install Microsoft.DotNet.SDK.10 --version 10.0.401 --architecture arm64`; instalador oficial de download.microsoft.com con el hash verificado);
- Unity 6000.3.25f1 ARM64 (changeset `e1dba0a9aba4`) con Android Build Support, OpenJDK, SDK y NDK, con Unity Hub 3.22.3 en modo headless (`-- --headless install --version 6000.3.25f1 --changeset e1dba0a9aba4 --architecture arm64 --module android --childModules`). La Unity CLI no se usó para instalar (`CLAUDE.md`). La licencia ya estaba activa en el equipo.

**Línea base** (`spark/reto-del-rio` en `2db3a07`, igual que `origin/integracion-fase1-fase2` `80b26b4` más el archivo de estado):

| Comando | Resultado | Tiempo |
|---|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | código 0, 0 avisos | 18 s |
| `dotnet test dotnet/AmazonFill.slnx` | 981 (975 + 6 omitidos), código 0 | 3 min 9 s |
| `python tools/validate_schema.py` | 103/103 PASS, código 0, sin cambios | — |
| `pwsh -NoProfile -File scripts/unity-compile.ps1 -Architecture arm64` | OK, código 0 (primera importación incluida) | 8 min |
| `pwsh -NoProfile -File scripts/unity-test.ps1 -Mode EditMode -Architecture arm64` | 2.744 (2.741 + 3 omitidos), 0 fallos, código 0 | 6 min |
| `pwsh -NoProfile -File scripts/unity-test.ps1 -Mode PlayMode -Architecture arm64 -TimeoutSec 3600` | 1.334 (1.175 + 159 omitidos), 0 fallos, código 0 | 22 min |

- Coinciden con los recuentos de `CLAUDE.md`.
- **Aviso:** con el límite por defecto (900 s), PlayMode sale con el código 6 (sin veredicto) a los 15 min, en mitad de `MapMarkersTests`: en este equipo tarda 21 min. Uso `-TimeoutSec 3600`, el valor que el propio script recomienda para una primera importación.
- Máquina: ARM64, 64 GB de RAM. Con 32 GB o más el encargo permite 3 Editores a la vez; me quedo en **2** salvo que haga falta.

## 2026-10-19 · R0 · Primer intento: BLOQUEADA por el entorno (resuelto en la entrada de arriba)

**Rama:** `spark/reto-del-rio`, creada desde `origin/integracion-fase1-fase2` en `80b26b4` («Deja en el encargo del Spark las respuestas del dueño a R-Q1-R-Q10 del Reto del río») y empujada a `origin`. `git lfs pull` hecho sin errores.

**Máquina (sección 1 del encargo):**

| Comprobación | Comando | Resultado |
|---|---|---|
| Equipo | `Win32_ComputerSystem.Model` / `Win32_Processor.Name` | ProArt P14 H7407BA · NVIDIA RTX Spark N1X (20 núcleos) |
| Arquitectura | `$env:PROCESSOR_ARCHITECTURE` | `ARM64` → `-Architecture arm64` / `--architecture arm64` |
| RAM | `(Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB` | 64,0 GB (de sobra: se podrían usar hasta 3 Editores; aún no aplica) |
| Python | `python --version` | 3.14.7 (ARM64) ✔ |
| `jsonschema` | `python -c "import jsonschema"` / `py -3.14 -m pip show jsonschema` | **✘ No instalado** (`ModuleNotFoundError`) |
| .NET SDK | `dotnet --list-sdks` | **✘ Ningún SDK.** Solo el host y los runtimes 8.0.32 (`Microsoft.NETCore.App`, `WindowsDesktop.App`). `global.json` pide 10.0.401 |
| Editor de Unity | `Test-Path "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe"` | **✘ No existe.** En `Hub\Editor` solo está `6000.3.26f1` (`unity editors` lo da como x86_64). El encargo prohíbe abrir otra versión sobre el proyecto |
| Unity CLI | `unity --version` | 1.0.0-beta.13 ✔ (alias `%LOCALAPPDATA%\Microsoft\WindowsApps\unity.exe`) |
| git LFS | `git lfs version` | 3.7.1 (windows arm64) ✔ |

**Verificación completa de `CLAUDE.md` (paso 3):** no se puede ejecutar ninguno de los comandos obligatorios:
- `dotnet build dotnet/AmazonFill.slnx -warnaserror` y `dotnet test`: falla el arranque del SDK («A compatible .NET SDK was not found. Requested SDK version: 10.0.401»).
- `python tools/validate_schema.py`: necesita `jsonschema`.
- `scripts/unity-compile.ps1` y `scripts/unity-test.ps1` (EditMode y PlayMode): necesitan Unity 6000.3.25f1 ARM64.

No hay línea base: **sin recuentos**. No he instalado nada (regla dura 4 de `CLAUDE.md`; el encargo, sección 1: «no lo instales tú: si falta, pregunta al dueño»).

**Me paro aquí**, como manda R0 («si algo sale en rojo en la base, para y anótalo»). R1-R12 dependen de R0 y no he empezado ninguna.

### Pendiente del dueño (para desbloquear R0; hecho, entrada de arriba)

1. **.NET SDK 10.0.401** para Windows ARM64 en `C:\Program Files\dotnet` (lo fija `global.json`, con `rollForward: latestFeature`).
2. **`jsonschema`** en Python 3.14 (`py -3.14 -m pip install jsonschema`).
3. **Unity 6000.3.25f1** (changeset `e1dba0a9aba4`), Editor **Windows ARM64**, con el módulo Android, en `C:\Program Files\Unity\Hub\Editor\6000.3.25f1\`, desde el Hub (la CLI no instala; `CLAUDE.md`, «Stack fijado»). La licencia la activa el dueño.
   - El 6000.3.26f1 x86_64 que hay instalado **no se usa**: abrirlo sobre el proyecto lo actualizaría.
4. Avisar al Spark cuando esté para repetir R0 y medir la línea base.

### Decisiones del dueño registradas (para que el integrador las lleve a `docs/dev/decisiones.md`)

Las R-Q1 a R-Q10 de la sección 5 del encargo, decididas por el dueño el 2026-10-20 («Acepto tus propuestas para R-Q1 a R-Q10 del Reto del río; déjalas en el encargo del Spark»). Se aplicarán tal cual cuando se desbloquee el trabajo:

| Id | Decisión |
|---|---|
| R-Q1 | Cada banda usa solo los tintes ya estrenados en su tramo de la campaña: B1 (K 6-7) con los de 1-40 y `max_pares_riesgo` 0; B2 (K 8-9) con los de 1-80 y como mucho 1; B3 (K 10-11) con los de 1-200 y como mucho 2 (ajustado después por R-Q1b) |
| R-Q2 | El agua (`water_base`) y el fondo rotan por los mundos 1-9 según la fecha: el agua y el kit de fondo del mundo de ese día |
| R-Q3 | Manda la fecha en que se empezó la partida: se sigue con ese tablero y se cobra ese premio aunque se gane pasada la medianoche |
| R-Q4 | La partida del Reto se reanuda al volver a abrir la app el mismo día; al cambiar de día se descarta |
| R-Q5 | El bloque `daily_challenge` del perfil añade la última fecha cobrada, el premio pendiente de validar y los sellos del mes (además de `last_date`, `result` y `streak_days`) |
| R-Q6 | `daily_overrides` = `{fecha: {banda: id del pool}}` |
| R-Q7 | La Pororoca sale cada 7 días seguidos ganados (cada racha completa); un día sin ganar rompe la racha; el marco de país de los sellos queda fuera hasta que existan cosméticos |
| R-Q8 | Los comodines son los de la campaña salvo los que el GDD excluye en el Reto (Cuia de rescate y Salto del Boto), y gastan el stock igual que en la campaña |
| R-Q9 | `result = fail` en `af_daily_challenge` es salir sin ganar ese día |
| R-Q10 | La anti-repetición (03 §17 #9) y el tope de trampas tempranas (03 §17 #3) se miden en el pool y se informan como aviso, sin ser compuerta |

### Riesgos y notas

- Las fechas: el encargo y estas decisiones van fechados el 2026-10-20; el reloj de esta máquina dice 2026-10-19.
- `git switch -c … origin/integracion-fase1-fase2` deja como upstream la integración; el push con `-u origin spark/reto-del-rio` lo corrige para que ningún `git push` vaya a `integracion-fase1-fase2`.

---

## Enganches para el integrador

Única sección de enganches (encargo §3.4). Cada uno: archivo, parche o descripción exacta, por qué, cómo se prueba y estado. Los que dependen de las pantallas (E1-E7, E9, E10, E12) llevan su parche exacto en **R12**; aquí van descritos.

| # | Estado | Archivo |
|---|---|---|
| E1 | Opcional · no preparado (la tarjeta va en su lienzo; requisito de 16:9 en R12) | `Scripts/UI/LobbyScreen.cs` |
| E2 | **Parche probado** (R12, `spark-enganches/E2.diff`) | `Scripts/UI/GameBootstrap.cs` |
| E3 | **Parche probado** (R12, `E3.diff` + regenerar escena) | `Scripts/UI/Editor/GameSceneBuilder.cs` |
| E4 | **Parche probado** (R12, `E4.diff`) | `Scripts/Save/Profile.cs`, `ProfileJson.cs`, `ProfileMigrations.cs` |
| E5 | **Parche probado** (R12, `E5.diff` + `E5-fix.diff`) | `Data/Loc/strings.csv` |
| E6 | No preparado: falta clave y diseño (R12) | Victoria del nivel 7 |
| E7 | Opcional · no preparado (R12) | `Scripts/UI/NoCanoesScreen.cs` |
| E8 | **Ya no hace falta** | (iba en `Scripts/GameFlow/LevelFlow*.cs`) |
| E9 | **Parche probado** (R12, `E9.diff`) | `Scripts/Economy/Transaction.cs`, `Wallet.cs` |
| E10 | **Ya no hace falta** (R10 sale de archivos del Spark) | `Scripts/UI/DevProbe.cs` (si hace falta) |
| E11 | **Parches probados** (`spark-parches-docs.md`, 12 diffs, aplican sobre `bc65242`) | `docs/dev/decisiones.md`, `docs/gdd/03`, `05`, `06`, `08` |
| E12 | **Parches probados** (`spark-parches-docs.md`) | `docs/dev/prototipo-v0.md`, `docs/dev/traspaso/README.md`, `CLAUDE.md` |
| E13 | Opcional | `Assets/_Project/Tests/EditMode/AF.Tests.EditMode.asmdef` |
| E14 | Opcional | `Scripts/Levels/LevelPackReader.cs`, `DailyPoolReader.cs` |
| E15 | **Aplicado** (`752bb55`) | `tools/level-generator/Program.cs`, `README.md` |
| E16 | Pendiente · el parche del bloque (a) quedó desfasado tras R5: regenerar | `tools/level-qa/level-qa.csproj` y `tools/level-qa/Daily/` |
| E17 | **Aplicado en parte** (`752bb55`); queda el README | `tools/level-qa/Program.cs`, `README.md` |
| E18 | Punto 1 corregido por el integrador (encargo §9); queda el punto 2 | Encargo §3.3 |
| E19 | Pendiente, para cuando exista la Crecida | `Scripts/GameFlow/LevelFlow.Funnel.cs`, `FailResolve.cs` |
| E20 | Pendiente (limpieza) | `Scripts/Levels/LevelDefinition.cs`, `LevelCatalog.cs` |
| E21 | **Parche probado** (R12, `E21.diff`) | `Scripts/UI/GameScreen.cs` (`OnIntroStarted`) |
| E22 | **Parche probado** (R12, `E22.diff`; opcional) | `Scripts/UI/GameBootstrap.cs` (`PressAndroidBack`) |
| E23 | **Parche probado** (R12, `E23.diff`) | `Scripts/UI/GameBootstrap.cs` (`OnApplicationPause`) |
| E24 | **Parche probado** (R12, `E24.diff`) | `Scripts/UI/DevProbe.cs` |
| E25 | Para cuando exista la Crecida | barra de la Crecida en la vista del Reto |

### E1 · Inicio: la tarjeta del Reto (pendiente; parche en R12)

- **Archivo:** `Assets/_Project/Scripts/UI/LobbyScreen.cs`.
- **Qué:** crear `DailyChallengeCard` (clase nueva de R9) en el inicio y refrescarla al mostrarlo. Mientras el sistema no exista en el build, oculta (D-008).
- **Por qué:** 06 §4.1 «Pantalla» y §4.7; encargo §3.4.
- **Prueba:** PlayMode de R9-R11 (los cinco estados de la nota §8).

### E2 · Arranque: `SetUpDaily()` (pendiente; parche en R12)

- **Archivo:** `Assets/_Project/Scripts/UI/GameBootstrap.cs`.
- **Qué:** campo `[SerializeField] TextAsset dailyPool;` y la llamada a `SetUpDaily()` (parcial nuevo `GameBootstrap.Daily.cs`) tras crear perfil, cartera y reloj.
- **Por qué:** encargo §3.4; crea el pool, `DailyChallengeService` y las pantallas, y conecta la victoria.
- **Prueba:** PlayMode de R11 (punta a punta).

### E3 · Escena: el pool en `dailyPool` (pendiente; parche en R12)

- **Archivo:** `Assets/_Project/Scripts/UI/Editor/GameSceneBuilder.cs`.
- **Qué:** asignar `Assets/_Project/Data/Daily/daily_pool_v1.json` a `dailyPool`, como con `river_stops`; el integrador regenera `Game.unity` (regla dura 3).
- **Por qué:** 08 §3; 08 §6.2.
- **Prueba:** regenerar la escena y PlayMode de R11.

### E4 · Perfil: bloque `daily_challenge` y `ProfileDailyChallengeStore` (pendiente; parche en R12)

- **Archivos:** `Assets/_Project/Scripts/Save/Profile.cs`, `ProfileJson.cs` (y `ProfileMigrations.cs` si hiciera falta; 08 §4.3 permite un campo nuevo sin migración mientras el esquema no se publique).
- **Qué:** el bloque de la nota §10 (`last_date`, `result`, `streak_days`, `last_claimed_date`, `pending`, `offline_claims`, `stamps`, `in_progress`) y `ProfileDailyChallengeStore : IDailyChallengeStore`.
- **Requisito:** el registro y la cartera **en el mismo `Profile`**, para no pagar dos veces la misma fecha (R7b, pendiente menor); acotar `offline_claims` (solo sirve a la rama (b) de R-Q12, no la decidida).
- **Por qué:** 08 §4.3; R-Q4, R-Q5, R-Q16.
- **Prueba:** ida y vuelta del JSON del perfil y los tests de `DailyChallengeStoreTests` sobre el almacén del perfil.

### E5 · Textos (pendiente; filas en R9-R12)

- **Archivo:** `Assets/_Project/Data/Loc/strings.csv`.
- **Qué:** copiar las filas de `docs/dev/traspaso/spark-strings-reto.csv` (misma cabecera, `status = borrador`; lo cultural, `pendiente_asesor`).
- **Por qué:** 08 §4.7; `.claude/rules/localizacion.md`.
- **Prueba:** los tests de localización que ya validan `strings.csv`.

### E6 · Tarjeta de 2 s tras ganar el 7 (pendiente; parche en R12)

- **Archivo:** la victoria del nivel 7 (punto exacto en R12).
- **Qué:** llamar a la clase nueva con «Desde el nivel 10, un tablero nuevo cada día».
- **Por qué:** 06 §2.2; 02 §13.
- **Prueba:** PlayMode, ganar el 7.

### E7 · Sin canoas (opcional; parche en R12)

- **Archivo:** `Assets/_Project/Scripts/UI/NoCanoesScreen.cs`.
- **Qué:** la línea «Mientras esperas: Jugar el Reto del río» y `af_no_canoes_choice = daily_challenge`.
- **Por qué:** 04 §7.3; 04 §11.
- **Prueba:** PlayMode con 0 canoas.

### E8 · Modo Reto en `LevelFlow` (ya no hace falta)

- Iba en `LevelFlow.cs`, `LevelFlow.Victory.cs`, `LevelFlow.Funnel.cs` y `LevelFlow.Resume.cs`: sin `SaveWin` ni `Transaction.Win`, nivel efectivo, Marea no ilimitada, sin canoas ni Cuia de rescate, reanudar.
- **Por qué ya no:** R8 lo hizo sin tocar archivos existentes, con `LevelFlow.CreateDaily(...)` en `LevelFlow.Daily.cs` (catálogo de un tablero con el nivel efectivo, progreso de solo lectura, cartera y configs copiadas, `StateStore` propio). Lo que queda va en E19 y E20.

### E9 · Cosecha doble del Reto (pendiente; parche en R12)

- **Archivos:** `Assets/_Project/Scripts/Economy/Transaction.cs`, `Wallet.cs`.
- **Qué:** una transacción para duplicar el premio del Reto (p. ej. `Transaction.DoubleHarvestDaily(int seeds)`, 60 → 120 [P]), con `source = double_harvest`.
- **Por qué:** `Transaction.DoubleHarvest(int level, RewardTier tier)` duplica la Cosecha de un nivel de campaña y `Transaction.Earn` rechaza `SeedSource.DoubleHarvest` (nota §1.4). 04 §2.8; 06 §4.1. Hasta entonces, el botón del Reto queda desactivado.
- **Prueba:** test de economía del pago doble solo con premio CONFIRMADO.

### E10 · Sondas desde los puntos de evento (pendiente; parche en R12)

- **Archivo:** `Assets/_Project/Scripts/UI/DevProbe.cs` (si hace falta).
- **Qué:** llamar a `DevProbe.Daily.cs` (R10) desde los puntos de evento; `fail` una vez por día (R-Q15).
- **Por qué:** 08 §8.2.
- **Prueba:** líneas `AF_STATE` en el build de desarrollo y los tests de `DailyProbeFormat`.

### E11 · GDD y decisiones (pendiente)

- **Archivos:** `docs/dev/decisiones.md`; `docs/gdd/03` §16; `docs/gdd/05` §6.1; `docs/gdd/06` §4.1; `docs/gdd/08` §4.3, §8.2 y §9.
- **Qué:**
  - llevar a `decisiones.md` R-Q1-R-Q10, R-Q1b, R-Q11-R-Q16 (con la aclaración de R-Q12) y la confirmación de `IServerDate` en AF.Platform; confirmar la fecha de R-Q1-R-Q10 (2026-10-20 en la primera versión del encargo; 2026-10-19 en este equipo);
  - 08 §9: `daily_overrides` pasa de `json {fecha: level_id}` a `{fecha: {banda: id}}` (R-Q6); el default `{}` no cambia;
  - 08 §4.3: el bloque `daily_challenge` de la nota §10 (R-Q4, R-Q5, R-Q16);
  - 06 §4.1: la racha «cada 7» (R-Q7), los comodines (R-Q8), las bandas (R-Q1b), la validación de pendientes (R-Q12b) y `pool_overrides` (R-Q6);
  - 03 §16: B2 con K 8 y B3 sin tope (R-Q1b); la ventana del bot es compuerta (R-Q11);
  - 05 §6.1: sin intersticial en el Reto (R-Q13); 08 §8.2: `fail` una vez por día (R-Q15).
- **Por qué:** `docs/gdd/**` está prohibido para el Spark; el GDD manda (`CLAUDE.md`). Nota §1.3.
- **Prueba:** `DailyChallengeConfigTests` (lee la tabla de 08 §9 y solo compara el tipo base `json` y el default `{}`) sigue en verde; `python tools/validate_schema.py` 103/103.

### E12 · Documentación (pendiente; texto en R12)

- **Archivos:** `docs/dev/prototipo-v0.md`, `docs/dev/traspaso/README.md`, `CLAUDE.md`.
- **Qué:** las sondas nuevas; `Scripts/LiveOps/` y `Data/Daily/` en el mapa; `--daily` y `--self-test daily`; los recuentos nuevos.
- **Por qué:** encargo §3.4.
- **Prueba:** revisión.

### E13 · `AF.Tests.EditMode.asmdef` += `AF.LiveOps` (opcional)

- **Archivo:** `Assets/_Project/Tests/EditMode/AF.Tests.EditMode.asmdef`.
- **Qué:** añadir `"AF.LiveOps"` a `references`.
- **Por qué:** solo si se quieren en EditMode los tests de AF.UI que tocan el Reto; hoy van en `AF.LiveOps.Tests` (EditMode) y en PlayMode (nota §4).
- **Prueba:** `unity-compile.ps1` y EditMode.

### E14 · Extraer los ayudantes de `DailyPoolReader` (opcional)

- **Archivos:** `Assets/_Project/Scripts/Levels/LevelPackReader.cs`, `DailyPoolReader.cs`.
- **Qué:** `DailyPoolReader` copia unas 90 líneas privadas de `LevelPackReader` (`ReadUnits`, `ReadBoard` y los ayudantes). Extraerlas a una clase `internal` compartida de AF.Levels, y hacer `internal` el lector de mecánicas si el Reto llega a usarlas.
- **Por qué:** una sola copia. Mientras tanto, los tests de la copia protegen contra una divergencia.
- **Prueba:** EditMode (`DailyPoolReaderTests`, 107, y los de `LevelPackReader`).

### E15 · `daily` en el `--self-test` sin nombre del generador (APLICADO en `752bb55`)

- **Archivos:** `tools/level-generator/Program.cs` y `README.md`.
- **Parche** (el aplicado; en `ba57160` eran las líneas 133-143):
  ```diff
           int holgura = HolguraSelfTest.Run();
  -        return Math.Max(Math.Max(Math.Max(tinaja, seasons), Math.Max(variants, registry)), Math.Max(curationTest, holgura));
  +        int daily = AmazonFill.Tools.LevelGenerator.Daily.DailyCommand.Run(["--self-test", "daily"]);
  +        return Math.Max(Math.Max(Math.Max(Math.Max(tinaja, seasons), Math.Max(variants, registry)), Math.Max(curationTest, holgura)), daily);
  ```
  - `|daily` en `Usage` (`--self-test [tinaja|…|holgura|daily]`) y «, holgura o daily» en el mensaje de error; `--daily` en la ayuda y en `tools/level-generator/README.md`.
- **Por qué:** encargo §9, punto 6.
- **Prueba:** `dotnet run --project tools/level-generator -c Release -- --self-test` → 435 PASS, 0 FAIL, exit 0, con «daily».

### E16 · `level-qa.csproj` enlaza `tools/level-generator/Daily/` (pendiente; parche probado)

- **Archivos:** `tools/level-qa/level-qa.csproj`; `tools/level-qa/Daily/DailyQaCommand.cs` y `DailyChecks.cs` (comentarios); borrar `tools/level-qa/Daily/Shared/DailySpec.cs` y `DailyIds.cs`.
- **Parche completo** (probado): `C:\Users\nir_l\AppData\Local\Temp\claude\C--Users-nir-l-projects-fill-game\f846b4a6-1ce3-41af-a4ae-c95773cc7080\scratchpad\E16-short.diff` (carpeta temporal de la sesión: si se pierde, lo esencial es esto). Lo esencial:
  ```diff
  --- a/tools/level-qa/level-qa.csproj
  +++ b/tools/level-qa/level-qa.csproj
  @@ -31,4 +31,11 @@
       <Compile Include="../level-generator/Generation/*.cs" Exclude="../level-generator/Generation/SeasonTableWriter.cs" LinkBase="Shared/Generation" />
     </ItemGroup>
  +
  +  <!-- Reto del río (modo diario de level-qa): el mismo lector de la especificación (DailySpec), los mismos ids y semillas (DailyIds)
  +       y la misma generación (DailyRunner, DailyTints y DailyPackWriter) que el generador, enlazados, para que QA-DAILY-REPRO
  +       regenere el pool en memoria. La entrada de la línea de comandos del generador (DailyCommand*.cs) se queda fuera. -->
  +  <ItemGroup>
  +    <Compile Include="../level-generator/Daily/*.cs" Exclude="../level-generator/Daily/DailyCommand*.cs" LinkBase="Shared/Daily" />
  +  </ItemGroup>
   </Project>
  ```
  En `DailyQaCommand.cs`:
  - `Reproduce(pack, spec, tints)` regenera en memoria en lugar de lanzar `dotnet run` del generador:
    ```csharp
    List<int> bands = slots.Select(l => l.Band!.Value).Distinct().Order().ToList();
    List<int> days = slots.Select(l => l.Day!.Value).Distinct().Order().ToList();
    var dailyTints = new DailyTints(tints, spec);
    DailyRun run = DailyRunner.Run(spec, dailyTints, bands, days, Environment.ProcessorCount);
    // run.Errors → «el generador falla: …»; DailyPackWriter.CheckBoard(b) → «el generador rechaza su tablero: …»;
    // sin tableros → «el generador no da ningun tablero».
    JsonNode regen = JsonNode.Parse(PackWriter.Bytes(DailyPackWriter.Write(spec, dailyTints, run.Boards)))!;
    // y la misma comparación de antes: JsonNode.DeepEquals de cada tablero con el regenerado.
    ```
  - `RunChecks` pierde el parámetro `specPath`; se borran `GeneratorProject`, `ReproTimeout`, `TimedOut`, `Ranges`, `RepoRoot` y `RunDotnet`, y los `using` de `System.ComponentModel` y `System.Text`; se añade `using AmazonFill.Tools.LevelGenerator.Generation;`.
  - En `--self-test daily` salen las comprobaciones de identidad de `Daily/Shared/`, la de `Ranges` y la de `RepoRoot`.
  - Los comentarios de cabecera de `DailyQaCommand.cs` y `DailyChecks.cs` pasan a describir la reproducción en memoria.
- **Por qué:** una sola copia de `DailySpec` y `DailyIds`; QA-DAILY-REPRO sin proceso hijo ni tope de 15 min.
- **Prueba:** `dotnet build tools/level-qa -warnaserror` y `dotnet run --project tools/level-qa -- --self-test daily` (todo PASS).

### E17 · `level-qa`: documentar `daily` (APLICADO EN PARTE en `752bb55`)

- **Archivos:** `tools/level-qa/Program.cs` (hecho) y `tools/level-qa/README.md` (pendiente).
- **Hecho:** en `Usage`, `--daily <daily_pool_v1.json> [--spec <daily_v1.json>] [--partial] [--no-reproduce]` y `--self-test [key2|reglas|variante|daily]`; el mensaje de error con «key2, reglas, variante o daily». El `--self-test` sin nombre de level-qa solo ejecuta `key2`, así que no se le suma `daily`.
- **Pendiente:** `--daily` en `tools/level-qa/README.md`. Va con E16: el texto tiene que describir QA-DAILY-REPRO tal como quede (en memoria con E16; hoy, proceso hijo).
- **Prueba:** `dotnet run --project tools/level-qa -- --self-test bogus` → el mensaje de uso nombra «daily» (comprobado).

### E18 · Corrección del encargo §3.3 (punto 1 hecho; queda el punto 2)

- **Archivo:** `docs/dev/traspaso/spark-reto-del-rio.md` §3.3.
- **Punto 1** (`tools/level-generator/Program.cs`): el despacho va «antes del `--self-test` genérico», no «justo antes de `--parity`». **Corregido** por el integrador en el encargo §9.
- **Punto 2** (`tools/level-qa/Program.cs`): cambiar «antes de la lectura de argumentos de la campaña» por «antes del `--self-test` genérico (que rechazaría «daily»)», que es donde está (R4).
- **Por qué:** el `--self-test` genérico solo reconoce subcomandos fijos y daría error de uso con `--self-test daily`.
- **Prueba:** revisión del texto.

### E19 · Guarda de la Crecida en `FailResolve` (pendiente; para cuando exista la Crecida)

- **Archivos:** `Assets/_Project/Scripts/GameFlow/LevelFlow.Funnel.cs` y `FailResolve.cs`.
- **Parche:**
  ```diff
  @@ private void Fail(FailKind kind)
  -        FailOutcome outcome = FailResolve.Resolve(_attempt, kind, LevelNumber, _wallet);
  +        FailOutcome outcome = FailResolve.Resolve(_attempt, kind, LevelNumber, _wallet, campaign: !IsDaily);
  ```
  ```diff
  -        public static FailOutcome Resolve(LevelAttempt attempt, FailKind kind, int level, Wallet? wallet)
  +        public static FailOutcome Resolve(LevelAttempt attempt, FailKind kind, int level, Wallet? wallet, bool campaign = true)
  ...
               canoeLost = wallet.Apply(Transaction.HardFail(level)).CanoeLost;
           }
  +        if (!campaign)
  +        {
  +            return new FailOutcome(kind, attempt.FailsLevel, canoeLost); // 06 §4.3: el Reto no rompe la Crecida ni gasta la Hoja escudo
  +        }
  ```
- **Por qué:** 06 §4.3 («si not nivel.es_campana: return»). Hoy no hace falta: por esa vía no se cuenta nada de campaña (`stats.fails_hard` no cambia, y lo fija un test).
- **Prueba:** el test del reinicio desde el modal del Reto comprueba que la Crecida del perfil no cambia, y un test de campaña, que sí se rompe.

### E20 · `LevelDefinition.WithLevel` y `LevelCatalog.ForBoard` (pendiente; limpieza)

- **Archivos:** `Assets/_Project/Scripts/Levels/LevelDefinition.cs` y `LevelCatalog.cs`.
- **Parche:**
  - `internal LevelDefinition WithLevel(int level) => new LevelDefinition(Id, level, World, WaterBase, _palette, Units, _caps, _cuias, _attrs, IsMurky, _hidden, Par, DifficultyLabel, _relief, IntroMechanic, Skippable, HintSupported, Band, ParApprox);`
  - `public static LevelCatalog ForBoard(LevelDefinition board, int level) => FromPack(new LevelPack("daily_play", "daily", board.World, "daily_play", board.WaterBase, new[] { board.WithLevel(level) }));`
  - Después, `DailyPackFor` (en `LevelFlow.Daily.cs`) se reduce a `LevelCatalog.ForBoard(board, effectiveLevel)`.
- **Por qué:** quita el paso por un JSON interno que no cumple el esquema af-level/1 (lo señaló la revisión de R8).
- **Prueba:** `DailyLevelFlowTests` tal cual, con la comprobación «same».

### E21 · Rótulo «Nivel N · intento k» en el Reto (pendiente; R9)

- **Archivo:** `Assets/_Project/Scripts/UI/GameScreen.cs`, en `OnIntroStarted` (línea ~996).
- **Parche:**
  ```diff
                   StartIntro(flow, durationMs, isShort);
  -                if (isShort)
  +                if (isShort && !flow.IsDaily)
                   {
                       // 02 §2 (INTRO): «En reintento muestra Nivel N · intento k», …
  ```
- **Por qué:** en el Reto, cada Volver a la orilla enseña «Nivel {nivel efectivo} · intento k», y 06 §4.1 pide la cabecera «Reto del río» sin «Nivel N». Hasta que se aplique, el defecto se ve en el teléfono.
- **Prueba:** en `DailyChallengeTests.Restart_IsFreeAndUnlimited_SameBoard`, tras `PressRestart()` y un fotograma, `Assert.That(daily.View!.BannerText, Is.Null.Or.Not.Contains("Nivel"))`. En la campaña, el test de reintento que ya existe sigue viendo «Nivel N · intento k».

### E22 · Atrás con el Reto abierto (opcional; R9)

- **Archivo:** `Assets/_Project/Scripts/UI/GameBootstrap.cs`, en `PressAndroidBack()`, justo después del bloque del Mapa del río (`if (_mapScreen != null && _mapScreen.Visible) { … }`) y antes de `if (_app.Screen == AppScreen.Level)`.
- **Parche:**
  ```diff
  +            // Reto del río (R9): Atrás cierra su calendario como «Inicio»; con el tablero del Reto en juego no hace nada, como en un
  +            // nivel (02 §3; 04 §6.6). HandleBack devuelve false si no hay calendario abierto.
  +            if (_dailyScreen != null && _dailyScreen.IsOpen)
  +            {
  +                return _dailyScreen.HandleBack();
  +            }
  +
               if (_app.Screen == AppScreen.Level)
  ```
- **Por qué:** con el Reto abierto, AppFlow sigue en Lobby, así que sin la guarda Atrás caería en `PressBack()` del inicio, que está oculto. Con ella, Atrás solo cierra el calendario; en el tablero no hace nada, como en la campaña.
- **Prueba:** `AndroidBack_DoesNothingOnTheBoard_ButClosesTheCalendar` cubre la lógica de `HandleBack`. Con el enganche, `boot.PressAndroidBack()` da false con el tablero y true con el calendario, y el inicio sigue oculto mientras se juega.

### E23 · Volver de segundo plano en el Reto (pendiente; R9)

- **Archivo:** `Assets/_Project/Scripts/UI/GameBootstrap.cs`, en `OnApplicationPause` (al volver de segundo plano), justo después del bloque `if (_flow != null && _screen != null && InLevel) { … }`.
- **Parche:**
  ```diff
  +            // Reto del río (R9; 02 §14): su vista salta al modelo y la selección se limpia, como la de campaña.
  +            if (_dailyScreen?.Flow != null && _dailyScreen.View != null)
  +            {
  +                _dailyScreen.View.SnapToModel();
  +                _dailyScreen.View.SettleReveal();
  +                _dailyScreen.View.IdleHintBackFromBackground();
  +                _dailyScreen.Flow.ClearSelection();
  +            }
  ```
- **Por qué:** `InLevel` es false durante el Reto (AppFlow está en Lobby). Sin esto, al volver de segundo plano la vista del Reto se queda a mitad de animación, con la cuia seleccionada y sin reiniciar la cuenta de la pista por inactividad (02 §14; 02 §3.4; 04 §2.3). La persistencia no cambia: `PersistNow` ya guarda el lote del perfil.
- **Prueba:** test PlayMode que abre el Reto, toca una cuia (queda seleccionada), llama a `boot.SendMessage("OnApplicationPause", false)` y afirma `daily.Flow.Selected == -1` y `daily.View.IsViewIdle`.

### E24 · Las sondas de partida del Reto, marcadas (pendiente; R9-R10)

- **Archivo:** `Assets/_Project/Scripts/UI/DevProbe.cs` (prohibido para el Spark; `DevProbe` es `sealed`).
- **Parche:** justo después de cada una de las cuatro líneas `sb.Append(",\"level\":").Append(flow.LevelNumber.ToString(CultureInfo.InvariantCulture));` (hoy en las líneas 92 `AnimSkipped`, 361 `FormatState`, 488 `FormatFunnel` y 585 `FormatVictory`):
  ```diff
  +            AppendDailyBoard(sb, flow);
  ```
  `AppendDailyBoard` ya está en `DevProbe.Daily.cs` (R10): en el Reto añade `,"daily":"d{b}_n{nnnn}"`; en la campaña no añade nada.
- **Por qué:** la vista del Reto usa su propia `DevProbe`, pero las líneas `load`, `ready`, `pour`… salen con `"level"` igual al nivel efectivo (`max_level + 1`), que no es un nivel de campaña. Con la marca se distinguen por adb (08 §8.2 pide `level_name = d{b}_n{nnnn}` para el Reto). Las líneas `Event` de `GameScreen*.cs` (`idle_hint`, `hint`, `tinaja_tip`) quedan sin marca; basta la línea `load`.
- **Prueba:** en PlayMode, abrir el Reto y comprobar que `load`, `ready` y `pour` llevan `"daily":"…"`, y que en la campaña no aparece. Documentarlo en `docs/dev/prototipo-v0.md` (E12), por si algún script de adb da por hecho que `level` es ≥ 1.

### E25 · Sin barra de la Crecida en el Reto (para cuando exista)

- **Qué:** cuando exista la barra de la Crecida en la vista de partida, ocultarla con la misma guarda que E21 (`flow.IsDaily`). 06 §4.1 pide el tablero del Reto sin barra de Crecida, y 06 §4.3 dice que el Reto no la toca. Va junto con E19. En el código, `DailyChallengeScreen.cs` lo anota como «E21»: la numeración correcta es E25.
