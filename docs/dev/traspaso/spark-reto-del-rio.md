# Encargo al equipo Spark: el Reto del río (2026-10-20)

Escrito por la sesión tech-lead del equipo de escritorio para la sesión de Claude Code del **ASUS ProArt con NVIDIA «RTX Spark»** (el «Spark»). Decisión del dueño del 2026-10-20, literal: «Sí: encarga al Spark el Reto del río».

Léelo entero, junto con `CLAUDE.md`, antes de tocar nada. Si algo de aquí contradice el GDD, manda el GDD: para y pregunta (regla 10 de `CLAUDE.md`).

**Índice**
1. Quién eres y qué reglas te obligan
2. Git: tu rama y lo que nunca haces
3. Archivos que no tocas y enganches para el integrador
4. El Reto del río según el GDD
5. Decisiones abiertas (preguntas para el dueño)
6. Plan de tareas R0-R12
7. Cómo entregas
8. Frase para pegar en Claude Code del Spark

---

## 1. Quién eres y qué reglas te obligan

**Tu papel.**
- Eres un equipo de desarrollo más del proyecto. Trabajas en paralelo al equipo de escritorio.
- El escritorio es el **único integrador**: revisa, verifica y fusiona tu trabajo en `integracion-fase1-fase2` y en `main`. Tú solo empujas tus ramas `spark/*`.
- Te toca una sola funcionalidad: **el Reto del río** (`daily_challenge`). Todavía no está implementado ni planificado.

**Reglas que mandan.**
- `CLAUDE.md` manda entero, sobre todo sus **Reglas duras** 1-10:
  - el núcleo sin motor y determinista;
  - nada de YAML de Unity a mano;
  - nada nuevo sin aprobación del dueño: ni paquetes, ni SDK, ni permisos, ni versiones;
  - nunca secretos;
  - nada a Google Play;
  - un Editor por carpeta;
  - git sin force;
  - los agentes que programan no navegan;
  - ante una ambigüedad, preguntas.
- Las reglas por ruta de `.claude/rules/` (`core.md`, `data-json.md`, `localizacion.md`, `unity-assets.md`, `ci.md`) valen igual aquí.
- El orden de las fuentes de verdad es el de `CLAUDE.md`: GDD (`docs/GDD.md` y `docs/gdd/`), contratos de `docs/GDD.md` §7 y plan de instalación.

**Tu máquina (compruébala al empezar, tarea R0).**
- Arquitectura: `$env:PROCESSOR_ARCHITECTURE` en PowerShell (`ARM64` o `AMD64`).
  - Con ARM64: `-Architecture arm64` en `scripts/unity-compile.ps1` y `scripts/unity-test.ps1`, y `--architecture arm64` en `unity run` y `unity test` (lo que dice `CLAUDE.md`).
  - Con AMD64: `-Architecture x86_64` y `--architecture x86_64` (como el escritorio).
- Editor: usa **solo Unity 6000.3.25f1**. Si hay otra versión instalada, no la abras sobre el proyecto: lo actualizaría.
  - Si el Editor no está en `C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe`, pasa su ruta a los scripts con `-UnityExe <ruta>` o con la variable `UNITY_EDITOR_PATH`.
- Python: comprueba `python --version`. Si no es 3.14, usa `py -3.14` en todos los comandos de `CLAUDE.md`. Comprueba también que esa versión tenga `jsonschema`, pero **no lo instales tú**: si falta, pregunta al dueño.
- RAM: mídela con `(Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB`.
  - Por defecto, como mucho **2 Editores de Unity a la vez** (regla 7). Solo si mides de sobra (32 GB o más), puedes llegar a 3, y lo anotas en `spark-estado.md`.
  - Siempre **un Editor por carpeta**, y nunca batchmode sobre una carpeta con un Editor abierto.
- Primera vez:
  - `git lfs pull` tras el clon;
  - la primera importación de Unity tarda varios minutos;
  - la licencia la gestiona el dueño: nunca `unity auth` ni `unity license`.

**Cómo trabajas (flujo de `docs/setup/equipo-de-agentes.md` §3).**
- Cada tarea pasa por estos pasos:
  1. plan, si hace falta (tech-lead);
  2. implementación con su rol (`.claude/agents/<rol>.md`);
  3. pruebas;
  4. **code-reviewer**, con la fidelidad al GDD: cita capítulo y §;
  5. arreglos y una nueva revisión, hasta **APROBADO**.
- Una tarea no está hecha hasta pasar por todos esos pasos.
- Los subagentes con `isolation: worktree` (core-dev, level-pipeline-dev, unity-gameplay-dev) no pueden trabajar en un worktree que ya existe. Para trabajar en tu worktree, lanza `general-purpose` con «actúas como <rol>: lee .claude/agents/<rol>.md y sigue sus reglas, salvo la línea de isolation».
- Puedes paralelizar dentro del Spark con un worktree por tarea (`git worktree add .claude/worktrees/spark-rN spark/reto-del-rio-rN`). Sin pasarte de 3-4 agentes de C# puro ni del límite de Editores de arriba.
  - Para no reimportar Unity en cada worktree, siembra su `Library` copiándola con robocopy desde una carpeta que no tenga Unity en marcha.
- **Verificación completa de `CLAUDE.md` antes de cada push**, con la arquitectura y el Python de tu máquina:
  - `dotnet build dotnet/AmazonFill.slnx -warnaserror` y `dotnet test dotnet/AmazonFill.slnx`;
  - `python tools/validate_schema.py`;
  - `scripts/unity-compile.ps1`, y `scripts/unity-test.ps1` con `-Mode EditMode` y con `-Mode PlayMode`;
  - más la verificación propia de la tarea (sección 6).
  - Los recuentos de tests de `CLAUDE.md` pueden ir por detrás: tu referencia es la **línea base que midas en R0**, más tus tests nuevos.
  - Nunca digas «verde» sin haberlo ejecutado.

---

## 2. Git: tu rama y lo que nunca haces

- Al empezar:
  ```
  git fetch origin
  git switch -c spark/reto-del-rio origin/integracion-fase1-fase2
  git push -u origin spark/reto-del-rio
  ```
  Hoy `origin/integracion-fase1-fase2` está en `c5e1c58` o en un commit posterior.
- Si quieres, una rama por tarea: `spark/reto-del-rio-<id>` (por ejemplo, `spark/reto-del-rio-r3`), que sale de `spark/reto-del-rio` y vuelve a ella con `git merge --no-ff`. Empuja todas a origin.
- **Nunca:**
  - push a `integracion-fase1-fase2` ni a `main`;
  - `git push --force` ni `-f`, ni `+refspec`;
  - rebase de una rama ya empujada;
  - reescribir o borrar ramas ajenas (`tanda4/*` y demás);
  - crear repositorios remotos;
  - abrir PR a `main` (el integrador decide cómo fusiona).
- ¿Necesitas cambios nuevos de la integración? `git fetch origin` y `git merge origin/integracion-fase1-fase2` en tu rama. **No hagas rebase.** Resuelve tú los conflictos y vuelve a verificar.
- Commits en español, en imperativo («Añade el lector del pool del Reto del río»), y con esta última línea:
  ```
  Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
  ```
- Si un mensaje de merge va en un archivo, usa `git merge -F <archivo>`, porque `-F -` falla en Windows. Haz el push en un paso aparte, después de comprobar el merge con `git log -1`.
- Al terminar cada tarea:
  1. push de tu rama;
  2. una entrada en **`docs/dev/traspaso/spark-estado.md`**, en tu rama, con lo que pide la sección 7.

---

## 3. Archivos que no tocas y enganches para el integrador

En este escritorio trabajan a la vez los carriles de la ola 7 de la tanda 4: C1c, D3 y DC1 en la documentación; FX11; y DC2-DC25 en el cliente (Victoria, GameScreen, GameBootstrap, LobbyScreen, Ajustes, `strings.csv`, `facts.csv`, StopBackdrop, BoardLayout, el tutorial, SlothLines…). Esta lista sale de esos carriles y del `git diff --stat` de las ramas `tanda4/o7-c1b`, `o7-dcp`, `o7-dcu1`, `o7-dcu2`, `o7-docs`, `o7-fonts` y `o7-fx11` del 2026-10-19.

**Lista viva.** El integrador la actualiza en `origin/integracion-fase1-fase2`. Después de cada `git fetch`, léela con:
```
git show origin/integracion-fase1-fase2:docs/dev/traspaso/spark-reto-del-rio.md
```
Si el integrador libera un archivo, puedes tocarlo desde ese momento.

### 3.1 Regla general

- **Solo creas archivos nuevos.** Las únicas excepciones son las ediciones mínimas de la sección 3.3.
- Si un archivo existente que no está en la lista te parece imprescindible, no lo edites: describe el cambio como enganche y sigue.

### 3.2 No los toques (compartidos o en curso aquí)

**Documentación y configuración**
- `CLAUDE.md`, `docs/GDD.md`, `docs/gdd/**`, `docs/dev/decisiones.md`, `docs/dev/prototipo-v0.md` y `docs/dev/traspaso/README.md`, más los demás archivos de `docs/dev/traspaso/` que no sean tuyos.
- `.claude/**` (sobre todo `settings.json`), `.github/**`, `scripts/**`, `ci/**`, `global.json`, `dotnet/**`, `ProjectSettings/**`, `Packages/manifest.json` y `Packages/packages-lock.json`.

**Núcleo y esquema**
- `Packages/com.amazonfill.core/**`: el núcleo, que el carril o7-dcp cambia. El Reto no necesita reglas nuevas: usa `AF.Core` y `AF.Solver` tal como están, y `Prng.SeedFrom(string)` para la semilla.
- `schemas/af-level-1.schema.json`: ya admite `kind: daily`, con `slot`, `id` y `seed` diarios (líneas 72-85 y 387-396). Si crees que hace falta cambiarlo, para y pregunta: es el contrato 21 y sube `pack_schema`.
- `tools/validate_schema.py` y `tools/out/schema_example_08_4_1.json`.

**Herramientas**
- Los archivos existentes de `tools/level-generator/`, que o7-c1b está cambiando:
  - `Program.cs` (salvo la sección 3.3), `README.md`, `Generation/*.cs` (`Superposition.cs`, con su `RowOptions`, que se alinea tras C1c), `Metrics/*.cs`, `Campaign/*.cs` y `specs/*.json`.
  - Si necesitas otro comportamiento de una clase de ahí, envuélvela o cópiala en tu carpeta nueva; no la cambies.
- Los archivos existentes de `tools/level-qa/`: `Program.cs` (salvo la sección 3.3), `README.md`, `PackReader.cs`, `Checks/*.cs` y `fixtures/` existentes.
- `tools/river-map/**`, `tools/out/curation_*`, `tools/out/salida_a/**` y `tools/econ_*.py`.

**Datos**
- `Assets/_Project/Data/Levels/**` y `Assets/_Project/Data/Loc/**` (`strings.csv`, `facts.csv` y `glossary.csv`).
- `Assets/_Project/Data/Map/**`, `Data/Content/**` y `Data/Fauna/**`.
- `Assets/_Project/Scenes/Game.unity` y `Assets/_Project/Scripts/UI/Editor/GameSceneBuilder.cs`: no regeneres la escena en tu rama.

**Cliente: interfaz**
- `GameScreen*.cs`, `GameBootstrap*.cs`, `LobbyScreen.cs`, `SettingsScreen*.cs`, `BoardLayout.cs`, `BoosterTray.cs`, `HomeSloth.cs`, `RiverMapScreen*.cs`, `SlothNoticeCard.cs`, `TutorialFinger.cs`, `NoCanoesScreen.cs`, `UiText*.cs`, `LocTables.cs` y `DevProbe.cs`, más los `DevProbe.*.cs` que ya existen.

**Cliente: presentación, flujo y datos**
- `Scripts/Presentation/**`: `StopBackdrop.cs`, `Palette.cs`, `RiverRibbonGraphic.cs`, `ProceduralArt*.cs`…
- `Scripts/GameFlow/`: los archivos existentes (`SlothContext.cs`, `SlothLines.cs`, `TutorialInstruction.cs`, `SightingRule.cs`, `WorldBanners.cs`, `AppFlow.cs` y `LevelFlow*.cs`). Lo que necesites, en un parcial nuevo (sección 6, R8).
- `Scripts/Levels/` y `Scripts/Save/`: los archivos existentes, incluidos `LevelCatalog.cs`, `Profile.cs`, `ProfileJson.cs`, `ProfileMigrations.cs` y `LocalProfileStore.cs` (o7-dcu2).

**Tests**
- Los **archivos de test existentes** (`TestPacks.cs`, `PlayModeHelpers.cs`, `SnapshotKit.cs`, `AF.Tests.EditMode.asmdef`…): úsalos, pero no los edites. Crea tus propios archivos de test.

### 3.3 Ediciones mínimas permitidas en tu rama

Ninguna rama de la ola 7 toca estos archivos, o el cambio es una línea en un punto fijo. Cada edición va en **un commit propio** que empieza por «Enganche:» y se anota en `spark-estado.md`.

1. `tools/level-generator/Program.cs`: un único bloque de 3-6 líneas que despache `--daily` (y `--self-test daily`) a tu clase nueva (`Daily/DailyCommand.Run(args)`), insertado justo antes de `if (args.Length >= 1 && args[0] == "--parity")`.
2. `tools/level-qa/Program.cs`: lo mismo para `--daily` (`Daily/DailyQaCommand.Run(args)`), antes de la lectura de argumentos de la campaña.
3. `Assets/_Project/Scripts/UI/AF.UI.asmdef`: añadir `"AF.LiveOps"` a `references`.
4. `Assets/_Project/Tests/PlayMode/AF.Tests.PlayMode.asmdef`: añadir `"AF.LiveOps"` a `references`, si tus tests de PlayMode lo necesitan.

### 3.4 Enganches para el integrador (no los apliques tú)

Lo que necesites de un archivo de la sección 3.2 lo dejas preparado en archivos propios y lo anotas en `spark-estado.md`, en una sección **«Enganches para el integrador»**. Cada enganche lleva:
- el archivo;
- el parche mínimo exacto: un bloque de diff unificado o «inserta tras la línea que contiene X»;
- por qué hace falta;
- cómo se prueba.

Enganches previstos:
- **Inicio** (`LobbyScreen.cs`): la tarjeta o pestaña «Reto del río». D-008 la deja oculta hasta que exista; 06 §4.1 «Pantalla» y §4.7. Lo único del enganche es llamar a tu clase nueva `DailyChallengeCard`.
- **Arranque** (`GameBootstrap.cs`): crear `DailyChallengeService` y `DailyChallengeScreen`, pasarles el pool, el perfil, la cartera y el reloj, y conectar la victoria del Reto. Mételo en un parcial nuevo tuyo, `GameBootstrap.Daily.cs`, con un único método `SetUpDaily()`. El enganche es la llamada a ese método y el campo serializado `dailyPool` (TextAsset).
- **Escena** (`GameSceneBuilder.cs`): cargar `Assets/_Project/Data/Daily/daily_pool_v1.json` en ese campo, como hace con `river_stops`. El integrador regenera `Game.unity`.
- **Perfil** (`Profile.cs`, `ProfileJson.cs` y `ProfileMigrations.cs`): el bloque `daily_challenge` de 08 §4.3, con los campos que decida el dueño (pregunta R-Q5). Tu código usa la interfaz `IDailyChallengeStore`, y el enganche es su implementación sobre el perfil.
- **Textos**: las claves nuevas no van a `strings.csv`. Van a tu archivo `docs/dev/traspaso/spark-strings-reto.csv`, con la misma cabecera:
  ```
  key,es-419,en,pt-BR,context,max_chars,status,status_en,status_pt-BR,notes,args
  ```
  Claves en `snake_case`, marcadores con nombre, `context` y `max_chars` siempre, y `status` = `borrador`. Lo que sea cultural, `pendiente_asesor`. Las traducciones que falten quedan vacías (`.claude/rules/localizacion.md`). El integrador las añade a `strings.csv`. En tus tests, usa una tabla de prueba propia.
- **Tutorial**: la tarjeta de 2 s «Desde el nivel 10, un tablero nuevo cada día» tras ganar el 7 (02 §13; 06 §2.2). Hazla como clase nueva; el enganche es la llamada desde la victoria del 7.
- **Sin canoas** (`NoCanoesScreen.cs`): la línea «Mientras esperas: Jugar el Reto del río» (04 §7.3) y `af_no_canoes_choice` = `daily_challenge` (04 §11). Opcional: al final del plan.
- **Documentación**: lo que el integrador debería añadir a `docs/dev/prototipo-v0.md` (las sondas nuevas), al README del traspaso y a `CLAUDE.md` (el mapa del repositorio y los comandos), como texto propuesto.

---

## 4. El Reto del río según el GDD

Fuentes:
- `docs/GDD.md` §2 #9 y #21;
- 03 §16 (y §4.4, §4.5 y §17);
- 06 §4.1 (y §2.2, §2.3, §4.7 y §12);
- 05 §2.1 y §3.2;
- 04 §2.4, §2.6, §5.2, §7.1, §7.3 y §11;
- 08 §2.2, §2.4, §3, §4.1, §4.3, §6.1, §6.2, §8.2, §9 y §10.1;
- glosario (00) §10 y §11.

Contratos de `docs/GDD.md` §7 que lo tocan: el 21 (formato `af-level/1`, con `kind: daily`), la incidencia residual 5 de `docs/GDD.md` §7.2 («Esquema frente a 03 §16», cerrada), la tabla única de Remote Config (08 §9) y la taxonomía única de analítica (08 §8.2).

**Qué es.** Un tablero por día local, desde el nivel 10, que se juega sin red (06 §4.1, «Disponibilidad»; `live_unlock_level_daily` = 10; 06 §2.3, pantalla 4: el inicio lo muestra desbloqueado tras ganar el 10).
- Sale de un pool offline pregenerado e indexado por fecha:
  ```
  reto(fecha, tramo) = pool[tramo][(días_desde(2026-01-01) + live_daily_offset) mod 730]
  ```
  (03 §16; el mismo cálculo en 06 §4.1).
- Con `daily_overrides` (Remote Config), una fecha concreta puede llevar otro tablero.

**El pool** (03 §16; 08 §4.1, fila `kind`; 08 §3 y §6.2):
- **2.190 tableros = 3 bandas × 730 días**, en el pack `daily_pool_v1`: `Assets/_Project/Data/Daily/daily_pool_v1.json`, en el grupo `levels_install`.
- Esquema `af-level/1`, con:
  - `kind: daily` en la cabecera;
  - `slot {band 1-3, day 0-729}` en lugar de `level`;
  - `id` `d{b}_n{nnnn}`, con el patrón `^d[1-3]_n[0-9]{4}$`;
  - `seed` `amazonfill:v1:daily:b{b}:d{ddd}`, con el patrón `^amazonfill:v1:daily:b[1-3]:d[0-9]{3}$`.
  - Con ceros a la izquierda: cuatro cifras en el `id` y tres en la semilla (`d2_n0417` / `amazonfill:v1:daily:b2:d417`).
- La cabecera `daily` no exige `world`, `world_id`, `lap` ni `water_base`: el pool mezcla mundos y **cada tablero declara su `water_base`**.
- Se publica **sin métricas** (`difficulty` es opcional), con `relief` = `[]` y nunca en hexadecimal. Pesa unos 1,4 MB minificado y unos 0,1 MB comprimido [E].
- La semilla reproduce el tablero (08 §4.1; 03 §17 #10).

**Bandas** (03 §16; 06 §4.1; `lvl_daily_bracket_levels` = `[41, 201]`, según el nivel máximo del jugador):

| Banda | Nivel máximo | K | Vacíos | Fallo del bot |
|---|---|---|---|---|
| B1 | < 41 | 6-7 | 2 | 40-60 % |
| B2 | 41-200 | 8-9 | 1 | 60-80 % |
| B3 | 201+ | 10-11 | 1 | ≥ 75 % |

- **Sin mecánicas con disparador ni Aguas turbias** en el Reto obligatorio.
- La tarjeta Aguas turbias · Bonus es de la 1.1 (`ui_murky_bonus_enabled` = false en la 1.0): no se hace ahora.
- El premio es igual en las tres bandas.

**Reglas de juego** (06 §4.1):
- **Reintentos ilimitados y deterministas**, con la misma semilla. Ojo: no es «un intento por día»; lo que es diario es el **premio**.
- Marea, Cuia extra y Morpho permitidas.
- **Sin canoas** (04 §7.1: el Reto del río «no requiere canoas» y sigue disponible con 0 canoas).
- **Sin Cuia de rescate.**
- **Sin efecto sobre la Crecida** [P] (06 §4.3: «El Reto del río y los eventos no la tocan»).
- **Sin Salto del Boto** (04 §2.6, caso borde e).
- Sin DDA (03 §14).
- Sin alivio, porque `relief` = `[]`.
- Pantalla (06 §4.1):
  - el tablero con el HUD normal, la cabecera «Reto del río» y sin barra de Crecida;
  - en el inicio, una tarjeta «Reto del río · {fecha corta}» con el icono de la banda (una, dos o tres olas), el estado (Jugar / Sellado) y la racha «{n}/7»;
  - al ganar, el sello cae sobre el calendario del mes (cuadrícula de 28-31), +60 y el botón Cosecha doble.

**Premio y economía** (06 §4.1; 05 §2.1, fila «Reto del río»; 08 §9):
- **60 semillas**, de `live_daily_challenge_reward` = `{"seeds":60,"murky_bonus_mult":2}`, más el sello del día. Admite Cosecha doble.
- **7 Retos en días seguidos → 1 Pororoca** (`live_daily_streak7_reward` = `"shuffle"`, que entra en `shuffle_stock`; 04 §2.4).
- 20 sellos en un mes → marco de fondo del país del mes (cosmético): pregunta R-Q7.
- En la cartera: `SeedSource.DailyChallenge` de `Assets/_Project/Scripts/Economy/Transaction.cs` (valor `daily_challenge` de `earn_virtual_currency`). La economía simulada ya cuenta el Reto (05 §3.2; `tools/out/econ_B.md`): no toques `econ_sim.py`.

**Fecha local y anti-reloj** (06 §4.1, filas «Anti-reloj» y pseudocódigo; 08 §10.1; 04 §7.1, «Reloj»):
- El tablero de hoy se elige con la fecha local del dispositivo: `IClock.LocalDate()` y `ClockExtensions.LocalDateKey()` de `Assets/_Project/Scripts/Platform/IClock.cs`.
- El tablero siempre se puede jugar.
- **El premio, solo si `fecha_local > ultima_fecha_reclamada`.** Si el reloj va hacia atrás o el premio ya se cobró, no hay premio.
- Con red, si la fecha local y la del servidor difieren más de un día, el premio queda **«pendiente de validar»** hasta reconectar. La hora del servidor sale de la cabecera `Date` del último fetch de Remote Config: `Clock.ServerOffset` y `sec_clock_max_drift_h` = 24 (08 §10.1).
- Sin red, se acepta la fecha local y se reconcilia al reconectar.

**Al cambiar de día:** el índice avanza solo con la fecha local, así que la tarjeta pasa a «Jugar» con el tablero nuevo. Qué pasa con una partida empezada antes de medianoche no lo fija el GDD: pregunta R-Q3.

**Remote Config** (08 §9, la única tabla; claves con su default):

| Clave | Default |
|---|---|
| `biz_feature_daily_challenge` | true (interruptor general) |
| `live_unlock_level_daily` | 10 |
| `lvl_daily_bracket_levels` | `[41, 201]` |
| `live_daily_offset` | 0 |
| `daily_overrides` | `{}` (`{fecha: level_id}`) |
| `live_daily_challenge_reward` | `{"seeds":60,"murky_bonus_mult":2}` |
| `live_daily_streak7_reward` | `"shuffle"` |
| `ui_murky_bonus_enabled` | false |
| `sec_clock_max_drift_h` | 24 |

- Aún no hay `AF.RemoteConfig` en el cliente. Sigue el patrón del proyecto: una clase de configuración con propiedades `[RemoteConfigKey("…")]` y su default compilado, como `Assets/_Project/Scripts/Economy/EconomyConfig.cs` o `Boosters/BoosterConfig.cs`.
- Un test que lea 08 §9 y compruebe nombres y defaults, como `Assets/_Project/Tests/EditMode/EconomyConfigTests.cs`.
- Los alias retirados (`daily_offset`, `daily_reward_seeds`, `daily_bands`…; 06, Parámetros) no se usan.

**Analítica** (08 §8.2, la taxonomía única):
- `af_daily_challenge {date, result ∈ win/fail/abandon, t_ms, murky_bonus (bool), streak_days}`;
- `level_start`, con `level_name` = `d{b}_n{nnnn}`;
- `earn_virtual_currency`, con `source = daily_challenge`.
- Aún no hay `AF.Analytics`: van como sonda `AF_STATE` en un parcial nuevo `DevProbe.Daily.cs`, con el patrón de `DevProbe.Fauna.cs` de la rama `tanda4/o7-dcu2`. Solo identificadores, nunca texto visible.

**Dónde vive cada cosa** (08 §2.2):
- `AF.Levels`: el pool del Reto, indexado por fecha.
- `AF.LiveOps`: el Reto (pool por fecha, `daily_overrides`, `live_daily_offset`), que depende de Economy, Levels, RemoteConfig, Platform (`IClock`) y Save, y lo usan UI y Analytics.
  - `Scripts/LiveOps/` aún no existe: créalo como asmdef nuevo, `AF.LiveOps`, con `noEngineReferences: true` y `autoReferenced: false`, igual que `AF.GameFlow`.
  - Sus tests van en `Scripts/LiveOps/Tests/AF.LiveOps.Tests.asmdef`, con el patrón de `Scripts/Save/Tests/AF.Save.Tests.asmdef`.
  - Crear un asmdef propio no es un paquete nuevo: está en la estructura de 08 §3.

**Servidor: nada ahora.**
- Las cinco Cloud Functions de la 1.0 (08 §2.4: `rewardedSsv`, `verifyPurchase`, `restoreLedger`, `voidedPurchasesJob` y `deleteAccount`) **no incluyen el Reto**: el pool es local (08 §2.1, principio 4).
- Lo único «de servidor» es la fecha del servidor para el anti-reloj, que llegará con Firebase y Remote Config. Ponla detrás de una interfaz `IServerDate` en `AF.LiveOps`, con una implementación falsa para el editor y los tests (`FakeServerDate`: sin red, o con una fecha fijada por el test), como manda `CLAUDE.md`, «DLL x64 en el Editor».
- No crees `functions/`, no instales nada y no toques Firebase: necesita cuentas y secretos del dueño.

---

## 5. Decisiones abiertas (preguntas para el dueño)

En `docs/GDD.md` §6.3 no hay ninguna pregunta abierta sobre el Reto: las bandas y el tamaño del pool están cerrados (06, Preguntas 2; 03, Preguntas 2; `docs/GDD.md` §7.1, incidencias 15 y 16). Pero el GDD deja huecos que el código necesita.

**Regla:** si una decisión no tiene recomendación en el GDD, **para esa pieza** y anótala en `spark-estado.md` como pregunta para el dueño (regla 10), con las opciones y su efecto. Sigue con las tareas que no dependen de ella. No inventes reglas ni números.

| Id | Hueco | Dónde | Qué bloquea |
|---|---|---|---|
| R-Q1 | **La paleta de cada banda.** Hasta el 10 solo se han estrenado 5 tintes, y hasta el 40, 7 (glosario, tabla de mundos), pero B1 pide K 6-7 desde el nivel 10. ¿El Reto puede mostrar tintes que el jugador aún no ha visto (por ejemplo, todos los de debut por debajo del umbral superior de la banda) o no? ¿Qué `max_pares_riesgo` tiene cada banda? Va de 0 en 1-40 a 2 en 81-200, y desde el 201 no tiene límite (03 §4.5) | 03 §4.5 y §16 | R5 (el pool definitivo). R2 y R3 dejan la paleta como parámetro de la especificación |
| R-Q2 | **El agua del tablero** (`water_base`, que cada tablero declara) y el fondo de la pantalla del Reto: ¿rotan los mundos, siguen la parada del jugador u otra cosa? | 08 §4.1, fila `kind`; 06 §4.1 | R5 (`water_base`) y R9 (el fondo) |
| R-Q3 | **El cambio de día con una partida empezada:** si empieza a las 23:55 y gana a las 00:05, ¿qué tablero sigue y con qué fecha se reclama el premio? | 06 §4.1, pseudocódigo | R7b y R8 |
| R-Q4 | **Guardar y reanudar una partida del Reto** al cerrar la app: `level_state` es de campaña (04 §10.1) | 04 §5.2; 08 §4.3 | R8 (reanudar). Sin decisión, el Reto no se reanuda y se anota |
| R-Q5 | **Los campos del perfil:** 08 §4.3 da `daily_challenge {last_date, result, streak_days}`, pero las reglas piden también la última fecha reclamada, el premio pendiente de validar y los sellos del mes | 08 §4.3; 06 §4.1 | R12 (el enganche del perfil). R7 trabaja con `IDailyChallengeStore` |
| R-Q6 | **`daily_overrides`:** ¿a qué apunta el id (`pool_overrides` no está definido; 08 §9 dice `{fecha: level_id}`) y cómo se combina con las tres bandas? | 06 §4.1; 08 §9 | Solo la parte de sustituciones de R7a |
| R-Q7 | **La racha de 7 y los sellos del mes:** ¿la Pororoca sale cada 7 días seguidos o solo la primera vez? ¿Qué rompe la racha? El marco de país de los 20 sellos depende de cosméticos que no existen | 06 §4.1 | Parte de R7b. Sin decisión, el marco queda fuera del alcance y se anota |
| R-Q8 | **Comodines en el Reto:** Pororoca y Luciérnaga no salen en la lista de permitidos. ¿Y el stock se gasta como en la campaña? | 06 §4.1, fila «Reintentos» | Parte de R8 |
| R-Q9 | **`result = fail` en `af_daily_challenge`:** sin canoas ni fallo duro, ¿qué cuenta como fallo? | 08 §8.2; 04 §5.2 | La sonda de R10 |
| R-Q10 | **Las comprobaciones de campaña en el pool:** ¿se aplican al Reto la anti-repetición (03 §17 #9) y el tope de trampas tempranas (03 §17 #3)? El GDD no lo dice | 03 §17 | R4. Mide, informa como aviso y pregunta: no lo hagas compuerta |

**Decididas por el dueño el 2026-10-20** (respondió, literal: «Acepto tus propuestas para R-Q1 a R-Q10 del Reto del río; déjalas en el encargo del Spark»). Ya no bloquean nada; aplícalas tal cual y regístralas en `spark-estado.md` para que el integrador las lleve a `docs/dev/decisiones.md`:

| Id | Decisión |
|---|---|
| R-Q1 | Cada banda usa solo los tintes ya estrenados en su tramo de la campaña: B1 (K 6-7) con los de 1-40 y `max_pares_riesgo` 0; B2 (K 8-9) con los de 1-80 y como mucho 1; B3 (K 10-11) con los de 1-200 y como mucho 2 |
| R-Q2 | El agua (`water_base`) y el fondo rotan por los mundos 1-9 según la fecha: el agua y el kit de fondo del mundo de ese día |
| R-Q3 | Manda la fecha en que se empezó la partida: se sigue con ese tablero y se cobra ese premio aunque se gane pasada la medianoche |
| R-Q4 | La partida del Reto se reanuda al volver a abrir la app el mismo día; al cambiar de día se descarta |
| R-Q5 | El bloque `daily_challenge` del perfil añade la última fecha cobrada, el premio pendiente de validar y los sellos del mes (además de `last_date`, `result` y `streak_days`) |
| R-Q6 | `daily_overrides` = `{fecha: {banda: id del pool}}` |
| R-Q7 | La Pororoca sale cada 7 días seguidos ganados (cada racha completa); un día sin ganar rompe la racha; el marco de país de los sellos queda fuera hasta que existan cosméticos |
| R-Q8 | Los comodines son los de la campaña salvo los que el GDD excluye en el Reto (Cuia de rescate y Salto del Boto), y gastan el stock igual que en la campaña |
| R-Q9 | `result = fail` en `af_daily_challenge` es salir sin ganar ese día |
| R-Q10 | La anti-repetición (03 §17 #9) y el tope de trampas tempranas (03 §17 #3) se miden en el pool y se informan como aviso, sin ser compuerta |

Lo que sí está decidido no se pregunta: el desbloqueo tras ganar el 10 (06 §2.3; `live_unlock_level_daily`), los reintentos ilimitados, que no gasta canoas, que no toca la Crecida y el premio de 60 (secciones anteriores).

---

## 6. Plan de tareas R0-R12

Cada tarea dura como mucho 1-1,5 días y se cierra con implementación, code-reviewer y arreglos, la verificación completa (sección 1), el push y su entrada en `spark-estado.md`.

**Tamaños:** S ≈ 0,5 día · M ≈ 1 día · L ≈ 1,5 días.

### R0 · Arranque y línea base (S; sesión principal)

- **Pasos:**
  1. Arquitectura, RAM, Python y Editor (sección 1).
  2. Rama `spark/reto-del-rio` (sección 2).
  3. Verificación completa de `CLAUDE.md` **antes de cambiar nada**.
- **Acepta:**
  - todos los comandos en verde, con sus recuentos anotados como línea base en `spark-estado.md`;
  - si algo sale en rojo en la base, para y anótalo: no es tuyo arreglarlo.

### R1 · Nota de diseño del Reto (S; tech-lead)

- **Archivo:** `docs/dev/reto-del-rio.md`, en tu rama.
- **Contenido:**
  - los módulos (`AF.Levels`: pool; `AF.LiveOps`: lógica; `AF.GameFlow`: partida; `AF.UI`: pantallas);
  - las interfaces (`IDailyChallengeStore`, `IServerDate`);
  - los estados de la tarjeta;
  - los enganches previstos;
  - las preguntas R-Q1 a R-Q10, con opciones.
- **Acepta:** cita capítulo y § en cada regla; el code-reviewer comprueba la fidelidad al GDD.
- **Depende de:** R0.

### R2 · Especificación del pool y autoprueba (S; level-pipeline-dev)

- **Archivos nuevos:**
  - `tools/level-generator/specs/daily_v1.json`: por banda, K, vacíos, ventana del bot, fuente, la paleta como parámetro (marcada [P] hasta R-Q1) y el agua (marcada [P] hasta R-Q2);
  - `tools/level-generator/Daily/DailySpec.cs`: el lector, con la validación de la especificación;
  - `Daily/DailyIds.cs`: `id` y `seed` con su padding, y `day` → `id`/`seed`;
  - `Daily/DailyCommand.cs`, con `--self-test daily`.
- **Edición permitida:** el despacho en `Program.cs` (sección 3.3, punto 1).
- **Fuente por banda:** la análoga de 03 §4.4 (2 vacíos: `walk` o `fill` E=2; 1 vacío: `deep` con K ≥ 9 o `fill` E=1), con rechazo por la ventana del bot (máximo 30 candidatos). Marcada [P] en la especificación y en la nota de R1.
- **Acepta:**
  - `--self-test daily` comprueba los patrones de 03 §16 (`d2_n0417` ↔ `amazonfill:v1:daily:b2:d417`, los días 0 y 729, la banda 1-3) y que una especificación mal formada se rechace con un mensaje claro.
- **Verificación:**
  ```
  dotnet run --project tools/level-generator -- --self-test daily
  ```
  más la verificación completa.
- **Depende de:** R1.

### R3 · Modo `--daily` del generador (L; level-pipeline-dev)

- **Archivos nuevos:**
  - `Daily/DailyRunner.cs`: genera las bandas y los días pedidos (`--bands 1,2,3 --days 0-729`), con `Prng.SeedFrom(seed)` y los generadores y el bot existentes, que envuelve sin cambiarlos;
  - `Daily/DailyPackWriter.cs`: la cabecera `kind: daily`, `pack_id` `daily_pool_v1`, `slot`, `water_base` por tablero, `relief: []` y sin `difficulty`. JSON minificado, con orden estable de claves y campos;
  - `Daily/DailyReport.cs`: el informe `.md` con bot_fail, par y K por banda, los tiempos y los rechazos. El informe sí lleva las métricas; el pack no.
- **Acepta:**
  - **determinista**: dos ejecuciones dan un JSON idéntico byte a byte (lo prueba la autoprueba con 3 días por banda);
  - el JSON valida con `python tools/validate_schema.py --pack <archivo>`;
  - sin mecánicas ni `murky`/`hidden`;
  - `par ≥ 2K`;
  - las cuias son colores + vacíos (02 R1).
- **Verificación:**
  ```
  dotnet run --project tools/level-generator -- --daily --spec tools/level-generator/specs/daily_v1.json --bands 1,2,3 --days 0-9 --out tools/out/daily_sample --report tools/out/daily_sample_report.md
  ```
  más `--self-test daily`, `validate_schema.py --pack` y la verificación completa.
- **Depende de:** R2.

### R4 · QA del pool en `level-qa --daily` (M; level-pipeline-dev)

- **Archivos nuevos:**
  - `tools/level-qa/Daily/DailyQaCommand.cs` y `Daily/DailyChecks.cs`, con identificadores `QA-DAILY-*`;
  - fixtures en `tools/level-qa/fixtures/daily/`: un pack válido y uno por cada fallo.
- **Edición permitida:** el despacho en `Program.cs` (sección 3.3, punto 2).
- **Comprobaciones:**
  - el esquema;
  - `slot` coherente con `id` y `seed` (lo pide la `description` del esquema);
  - 730 días por banda, sin huecos ni duplicados;
  - K, vacíos y ventana del bot de cada banda;
  - resoluble con A* (03 §17 #1);
  - `par ≥ 2K` (03 §17 #2);
  - sin mecánicas ni ocultas;
  - `relief` vacío y sin `difficulty`, `solution`, `safe_path` ni `qa`;
  - la semilla reproduce el tablero (03 §17 #10);
  - como mucho 14 cuias.
- La anti-repetición y las trampas tempranas, solo como aviso (R-Q10).
- **Acepta:** `--self-test` en verde con todos los fixtures (cada fixture malo falla con su id).
- **Verificación:**
  ```
  dotnet run --project tools/level-qa -- --daily tools/out/daily_sample/daily_pool_v1.json
  ```
  más la autoprueba y la verificación completa.
- **Depende de:** R3.

### R5 · Generar y publicar el pool (S-M; level-pipeline-dev)

- **Salidas:**
  - `Assets/_Project/Data/Daily/daily_pool_v1.json`, con 2.190 tableros (su `.meta` lo genera Unity al importar; commitéalo tal cual);
  - `tools/out/daily_pool_v1_report.md`.
- **Acepta:**
  - `level-qa --daily` en verde;
  - menos de 2 MB sin comprimir (03 §16: ≈1,4 MB [E]);
  - la regeneración reproduce el archivo.
- Hasta que el dueño responda, el cliente prueba con packs construidos en código (como `Assets/_Project/Tests/EditMode/TestPacks.cs`, en un archivo de test tuyo). No publiques en `Assets/` un pool con una paleta inventada.
- **Depende de:** R4 y de las respuestas a R-Q1 y R-Q2.

### R6 · Lector del pool en `AF.Levels` (M; unity-gameplay-dev, C# sin motor)

- **Archivos nuevos:**
  - `Assets/_Project/Scripts/Levels/DailyPool.cs`: tableros por banda y día, `Find(band, day)` y `FindById(id)`, sin asignaciones en la consulta;
  - `DailyPoolReader.cs`, con el `JsonParser` existente de `Levels/Json/`: rechaza `kind` distinto de `daily`, `slot` incoherente con `id`/`seed`, bandas incompletas y `pack_schema` mayor que el que conoce, con `LevelFormatException`;
  - tests EditMode en un archivo nuevo.
- Reutiliza `LevelDefinition` si sirve. Si te obliga a cambiar `LevelPackReader.cs` o `LevelDefinition.cs`, envuélvelos y anota el enganche.
- **Acepta:** lee un pool de prueba y el real (cuando exista); los casos malos fallan con un mensaje claro.
- **Depende de:** R1. Puede ir en paralelo con R2-R5.

### R7a · Calendario del Reto en `AF.LiveOps` (M; unity-gameplay-dev)

- **Archivos nuevos:**
  - `Assets/_Project/Scripts/LiveOps/AF.LiveOps.asmdef`;
  - `DailyChallengeConfig.cs`: las claves de la sección 4, con `[RemoteConfigKey]`;
  - `DailyCalendar.cs`:
    - banda por nivel máximo con `lvl_daily_bracket_levels`;
    - día del pool `(días_desde(2026-01-01) + live_daily_offset) mod 730`, también con fechas anteriores a 2026-01-01 y offsets negativos (módulo positivo);
    - desbloqueo con `live_unlock_level_daily` y `biz_feature_daily_challenge`;
  - `Tests/AF.LiveOps.Tests.asmdef` y sus tests.
- **Acepta:**
  - el 2026-01-01 da el día 0 y el 2026-10-19, el día 291;
  - los umbrales 40/41 y 200/201;
  - un test de claves y defaults contra 08 §9.
- Las sustituciones (`daily_overrides`) solo se implementan si R-Q6 tiene respuesta. Si no, se dejan leídas y sin efecto, con un test que lo diga.
- **Depende de:** R6.

### R7b · Premio, anti-reloj y racha (M; unity-gameplay-dev)

- **Archivos nuevos:**
  - `LiveOps/DailyChallengeRules.cs`: `reclamar_reto` de 06 §4.1 con `IClock`, `IServerDate` y `sec_clock_max_drift_h`. Premio confirmado o pendiente; nunca dos veces la misma fecha; nunca con el reloj atrasado;
  - `IDailyChallengeStore.cs` y `InMemoryDailyChallengeStore.cs`;
  - `IServerDate.cs` y `FakeServerDate.cs`;
  - el pago con `Wallet.Apply` y `SeedSource.DailyChallenge`; la Pororoca de los 7 días, al stock `shuffle`.
- **Acepta:**
  - tests con `ManualClock` (`Scripts/Platform/ManualClock.cs`): reloj atrasado, el mismo día, el día siguiente, un desfase de más de 24 h con red y sin red, y la reconciliación;
  - lo que dependa de R-Q3, R-Q5 y R-Q7 queda señalado como pendiente, sin inventar.
- **Depende de:** R7a.

### R8 · La partida del Reto en `AF.GameFlow` (L; unity-gameplay-dev)

- **Archivo nuevo:** `Assets/_Project/Scripts/GameFlow/LevelFlow.Daily.cs`, un parcial de `LevelFlow`. Cambia lo mínimo para que una partida en modo Reto:
  - no gaste ni pida canoas;
  - no toque la Crecida, el alivio (`fails_level`), el Salto del Boto, la Cuia de rescate ni el progreso de campaña;
  - permita reintentos ilimitados;
  - devuelva el resultado al llamador (que es `AF.LiveOps`/UI, no `GameFlow`).
- Si hace falta tocar `LevelFlow.cs`, `AppFlow.cs` u otro archivo existente, no lo hagas: describe el cambio exacto como enganche. Si sin él la tarea no puede funcionar, para y anótalo.
- **Acepta:** tests EditMode nuevos que recorren una victoria, un bloqueo y un reinicio en modo Reto sin efectos de campaña. Los comodines, según R-Q8.
- **Depende de:** R7a. Para la reanudación, también de R-Q4.

### R9 · Pantallas del Reto en Unity (L; unity-gameplay-dev)

- **Archivos nuevos** en `Assets/_Project/Scripts/UI/`:
  - `DailyChallengeCard.cs`: la tarjeta del inicio (fecha corta con el formato de la región vía `Loc`, las olas de la banda, Jugar / Sellado y «{n}/7»);
  - `DailyChallengeScreen.cs`: la cabecera «Reto del río» sobre el tablero, sin barra de Crecida;
  - `DailyStampCalendar.cs`: la cuadrícula del mes con el sello que cae, +60 y Cosecha doble con el anuncio simulado existente;
  - `GameBootstrap.Daily.cs`: un parcial con `SetUpDaily()` (enganche en la sección 3.4).
- Textos: solo claves, en `docs/dev/traspaso/spark-strings-reto.csv`. Gráficos provisionales por código, como el resto del prototipo (D-003: «Selva brillante premium»; guía `docs/art/direccion-visual-v2.md`), sin arte nuevo.
- **Acepta:** PlayMode nuevo (`Assets/_Project/Tests/PlayMode/DailyChallengeTests.cs`) que abre la tarjeta, juega un tablero de prueba hasta ganar, ve el sello y el +60, y comprueba que no cambian canoas, Crecida ni progreso.
- **Depende de:** R7b y R8. El fondo, de R-Q2.

### R10 · Sondas de desarrollo y analítica (S; unity-gameplay-dev)

- **Archivo nuevo:** `Assets/_Project/Scripts/UI/DevProbe.Daily.cs`, con:
  - `AF_STATE daily_challenge` (los parámetros de `af_daily_challenge`);
  - `level_start` con `level_name` `d{b}_n{nnnn}`;
  - `earn_virtual_currency` con `source=daily_challenge`;
  - el estado de la tarjeta, para jugar con `adb`.
- **Acepta:** test EditMode del formato de cada línea. `result = fail`, según R-Q9.
- **Depende de:** R9.

### R11 · Capturas y prueba de punta a punta (M; unity-gameplay-dev)

- **Archivos nuevos:** `Assets/_Project/Tests/PlayMode/DailyChallengeSnapshotTests.cs`, con `[Explicit]`, `SnapshotKit` y 360 × 780 dp y 16:9 (como `BootSplashSnapshotTests.cs`):
  - la tarjeta en sus estados;
  - la cabecera;
  - el calendario con sellos;
  - es-419 y en-XA.
- **Pasos:** lánzalas sin `-nographics` y guarda los PNG en `Logs/snapshots/` (ignorado). Pon en `spark-estado.md` la lista de capturas y qué revisar.
- **Acepta:** las capturas salen y el texto no se sale de su caja; la suite normal sigue en verde.
- **Depende de:** R9 y R10.

### R12 · Enganche final para el integrador (M; sesión principal + code-reviewer)

- **Pasos:**
  1. Reúne en `spark-estado.md` **todos los parches** de la sección 3.4, completos y exactos.
  2. **Pruébalos** en un worktree temporal que salga de `origin/integracion-fase1-fase2` con tu rama fusionada: aplica los parches, compila y pasa EditMode y PlayMode. No empujes ese worktree.
  3. Anota en `spark-estado.md` el resultado y el hash exacto de la integración contra la que probaste.
- **Acepta:** el code-reviewer aprueba los parches y la prueba del worktree temporal sale en verde.
- **Depende de:** todo lo anterior. El perfil, de R-Q5.

### Orden y paralelismo

- R0 → R1 → (R2 → R3 → R4 → R5) en paralelo con (R6 → R7a → R7b → R8 → R9 → R10 → R11) → R12.
- El carril de herramientas no necesita Unity. El del cliente necesita un Editor.
- Si R-Q1 o R-Q2 siguen sin respuesta al llegar a R5, sigue con el cliente y deja R5 para el final.

---

## 7. Cómo entregas

**Cuándo avisas.** Al terminar un **bloque que se pueda integrar solo**, aunque el resto siga a medias:
- (a) R2-R4: el modo diario del generador y su QA;
- (b) R5: el pool publicado;
- (c) R6-R7b: el lector y la lógica del Reto, sin pantalla;
- (d) R8-R11: la partida y las pantallas;
- (e) R12: los enganches.

Cada bloque, verificado y empujado en `spark/reto-del-rio`.

**Qué dejas en `docs/dev/traspaso/spark-estado.md`** (en tu rama; una entrada nueva arriba por tarea o bloque):
- las tareas hechas y el hash del commit de cada una;
- la rama y si está empujada;
- **los comandos de verificación con sus resultados reales**: recuentos, códigos de salida y tiempos, y cualquier aviso;
- el veredicto del code-reviewer y lo que se arregló;
- lo marcado [P] (propuesto, sin decisión del dueño);
- las preguntas para el dueño (R-Qn), con opciones y qué bloquean;
- los enganches para el integrador, con parches exactos;
- los riesgos.

**Qué hace el integrador.** El escritorio hace `git fetch`, revisa tu rama con su code-reviewer, verifica, aplica los enganches y fusiona en `integracion-fase1-fase2` cuando su ola lo permite. Si encuentra algo, lo anota en este documento o en `spark-estado.md` de la integración: léelo tras cada `git fetch`.

**No esperes confirmación** para seguir con la siguiente tarea de tu plan. Solo paras la pieza concreta que dependa de una pregunta abierta, de un archivo prohibido o de algo que el GDD no decide.

---

## 8. Frase para pegar en Claude Code del Spark

```text
Eres el equipo Spark del proyecto Tintalú: haz git fetch, lee CLAUDE.md y docs/dev/traspaso/spark-reto-del-rio.md de origin/integracion-fase1-fase2 y sigue ese documento al pie de la letra para implementar el Reto del río en la rama spark/reto-del-rio, empezando por R0 y sin esperar confirmación entre tareas; para solo en lo que el documento manda parar y anótalo en docs/dev/traspaso/spark-estado.md.
```
