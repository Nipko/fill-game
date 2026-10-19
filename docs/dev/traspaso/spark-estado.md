# Estado del equipo Spark: el Reto del río

Encargo: `docs/dev/traspaso/spark-reto-del-rio.md` (de `origin/integracion-fase1-fase2`). Una entrada nueva arriba por tarea o bloque.

---

## 2026-10-19 · R8 · La partida del Reto en AF.GameFlow: código APROBADO, falta Unity

- **Commits:** `49501b4` y `535b92c` (arreglos de la revisión), en `spark/reto-del-rio-r8` (sale de R7b). **Empujada, pero sin fusionar** en `spark/reto-del-rio` hasta que pase Unity.
- **Archivos nuevos:** `Assets/_Project/Scripts/GameFlow/LevelFlow.Daily.cs` y `Assets/_Project/Tests/EditMode/DailyLevelFlowTests.cs` (18 tests), con sus `.meta`.
- **Cómo funciona (sin tocar archivos existentes):** `LevelFlow.CreateDaily(...)` monta un flujo propio. **E8 ya no hace falta como estaba previsto.**
  - Catálogo de un solo tablero, numerado con el nivel efectivo `max_level + 1` [P], así que los desbloqueos de `BoosterConfig` funcionan sin cambiar nada.
  - Progreso de solo lectura: ni `SaveWin` ni `SaveNextLevel`, y sin regalos de tutorial.
  - Cartera del Reto **obligatoria**, sobre el mismo `Profile`, con una copia de `EconomyConfig` (`eco_reward_*` = 0; `rescue_min_level` = ∞): sin canoas, sin rescate, sin fallo duro con coste y sin Cosecha.
  - Copia de `BoosterConfig` con la Marea no ilimitada y sin Cuia de rescate.
  - Sin `AdGate`, sin intersticial (R-Q13 [P]) y `StateStore` propio; rechaza el de campaña.
  - Un oyente intermedio entrega el evento `DailyResult` (Win/Exit/Restart) sin depender de AF.LiveOps.
  - Los tests de reflexión fallan si `EconomyConfig` o `BoosterConfig` ganan una clave que no se copia.
- **Verificación:**
  - autor, antes de que fallara la licencia: compila; EditMode 2.991 (2.987 + 3 omitidos y 1 fallo del test inestable `IntroArtTests`, que pasa al relanzarlo); **PlayMode 1.334 (1.175 + 159), 0 fallos**;
  - tras los arreglos (`535b92c`): `dotnet build -warnaserror` sin avisos y un arnés .NET fuera del repositorio con 18/18;
  - **pendiente:** `unity-compile` + EditMode (esperado 2.996: 2.993 + 3) + PlayMode, en cuanto vuelva la licencia.
- **code-reviewer:** primera ronda CAMBIOS NECESARIOS. Los tres bloqueantes:
  - sin cartera, el Reto caía en el embudo de D-008;
  - el comentario sobre la Crecida era falso;
  - el JSON interno no cumple af-level/1 y no lo decía.
  
  Segunda ronda: **APROBADO a falta de Unity**.
- **[P]:** nivel efectivo `max_level + 1`, congelado al empezar; sin regalos de tutorial; Marea con las 3 gratis y luego el stock; sin intersticial; salir borra la partida guardada del Reto (R-Q16 sigue abierta); modal de Aguas estancadas con Marea + Volver a la orilla; `DailyAttempt` vuelve a 1 al reanudar.
- **Enganches para el integrador (nuevos):**
  - **E19** · `Scripts/GameFlow/LevelFlow.Funnel.cs` y `FailResolve.cs`, **para cuando exista la Crecida** (06 §4.3: «si not nivel.es_campana: return»):
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
    - Hoy no hace falta: por esa vía no se cuenta nada de campaña (`stats.fails_hard` no cambia, y lo fija un test).
    - Prueba: el test del reinicio desde el modal del Reto comprueba que la Crecida del perfil no cambia, y un test de campaña, que sí se rompe.
  - **E20** · `Scripts/Levels/LevelDefinition.cs` y `LevelCatalog.cs` (limpieza): quita el paso por un JSON interno no conforme con el esquema.
    - `internal LevelDefinition WithLevel(int level) => new LevelDefinition(Id, level, World, WaterBase, _palette, Units, _caps, _cuias, _attrs, IsMurky, _hidden, Par, DifficultyLabel, _relief, IntroMechanic, Skippable, HintSupported, Band, ParApprox);`
    - `public static LevelCatalog ForBoard(LevelDefinition board, int level) => FromPack(new LevelPack("daily_play", "daily", board.World, "daily_play", board.WaterBase, new[] { board.WithLevel(level) }));`
    - Después, `DailyPackFor` se reduce a `LevelCatalog.ForBoard(board, effectiveLevel)`.
    - Prueba: `DailyLevelFlowTests` tal cual, con la comprobación «same».
- **Notas para R9 y R10:**
  - `LevelNumber` es el nivel efectivo: no se enseña como «Nivel N» ni dispara tutoriales ni sondas de campaña; se usa `DailyBoardId`.
  - El HUD del Reto se suscribe a la cartera del Reto, y el saldo de la campaña hay que refrescarlo al volver.
  - La UI llama a `ExitDaily`, no a `AppFlow.ExitToLobby`.

## 2026-10-19 · ⚠ La licencia de Unity vuelve a fallar (pendiente del dueño, 2.ª vez)

- **Hacia las 13:35** los batchmode vuelven a salir con el código 6, con los mismos errores que la primera vez:
  - `Code 404 … Found 0 entitlement groups and 0 free entitlements`;
  - `'com.unity.editor.headless' was not found`;
  - `No valid Unity Editor license found`.
- **Patrón:** las dos veces, justo antes del fallo, arrancó un proceso nuevo de la Unity CLI, `unity.exe --internal-auth-broker-serve` (PID nuevo a las 12:09 y a las 13:35). Parece que, al renovar la sesión, la CLI se queda sin derechos para el modo headless. La pasada larga que acababa de terminar (bloque c, PlayMode 1.334) salió bien porque arrancó antes.
- **Qué afecta:** la verificación en Unity de R8 y de lo que sigue. Sigo escribiendo y revisando el código, pero no lo fusiono hasta pasarlo por Unity.
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
  - R-Q12 por defecto en (a), `KeepOfflineClaims`;
  - anular en la reconciliación deja la racha a 0, quita el sello y no retira semillas;
  - salir sin ganar cierra la partida, que no queda reanudable;
  - salir de un día ya ganado no emite `fail`;
  - `LevelStateSnapshot.Copy` va duplicada en AF.LiveOps, con un test por reflexión.
- **Pendiente menor** (va en un commit posterior):
  - documentar en `IDailyChallengeStore` que el registro y la cartera **tienen que vivir en el mismo `Profile`**, porque si no la garantía de «nunca dos pagos» se pierde (requisito de E4);
  - acotar `offline_claims`;
  - que `Reconcile` guarde solo si cambia algo;
  - la cita del test en `DailyChallengeRecord.cs:66`;
  - para R9: tras anular un pendiente con el reloj adelantado, la tarjeta sale «Sellada».

### Preguntas nuevas para el dueño (de la revisión de R7b)

| Id | Pregunta | Opciones (recomendada primero) | Qué bloquea |
|---|---|---|---|
| **R-Q12b** | Al validar un premio pendiente, ¿qué fechas se aceptan? Hoy se paga todo pendiente con fecha ≤ la del servidor, así que con red y el reloj atrasado a días no jugados se acaban pagando esos días. 06 §4.1 compara «> 1 día» | (a) solo si `|fecha − hoy_servidor| ≤ 1 día`; las demás se anulan; (b) toda fecha ≤ hoy del servidor (lo de ahora) | La rama de validación de R7b (es una línea) |
| **R-Q15** | `af_daily_challenge.result = fail`: ¿una vez por cada salida sin ganar, una por día, o solo si el día termina sin victoria? (R-Q9) | (a) uno por día, solo si ese día no se gana; (b) uno por salida (lo de ahora) | La sonda de R10 |
| **R-Q16** | Salir de la partida del Reto sin ganar: ¿la cierra (lo de ahora: solo se reanuda si se cierra la app; R-Q4) o la deja reanudable el mismo día? | (a) reanudable el mismo día; (b) se cierra | R8/R9 |
| **R-Q12** (aclaración) | En la rama (b) de R-Q12, ¿se retiran las semillas de una fecha anulada? El código no retira ni semillas ni la Pororoca | (a) no se retira nada; (b) se retiran las que queden sin gastar | Solo la rama (b) |

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
- **Enganche opcional** (E14): `DailyPoolReader` copia unas 90 líneas privadas de `LevelPackReader` (`ReadUnits`, `ReadBoard` y los ayudantes). Propuesta: extraerlas a una clase `internal` compartida de AF.Levels, y hacer `internal` el lector de mecánicas si el Reto llega a usarlas. Los tests de la copia protegen contra una divergencia.

## 2026-10-19 · R4 · QA del pool en `level-qa --daily`: HECHA · Bloque (a) listo para integrar

- **Commits** (en `spark/reto-del-rio-r4`, fusionados con `--no-ff` en `spark/reto-del-rio` como `ba57160`):
  - `32b8cc4`: detalles de la revisión de R3;
  - `cafe8c4`: la QA;
  - `ed109a3`: «Enganche:» del despacho en `tools/level-qa/Program.cs`;
  - `8afdd0e`: arreglos de la revisión.
- **Archivos nuevos:**
  - `tools/level-qa/Daily/DailyChecks.cs` (21 ids `QA-DAILY-*`) y `DailyQaCommand.cs`;
  - `tools/level-qa/Daily/Shared/DailySpec.cs` y `DailyIds.cs`: copias byte a byte de las del generador; la autoprueba falla si divergen;
  - `tools/level-qa/fixtures/daily/`: 1 fixture válido y 18 malos.
- **Uso:**
  ```
  dotnet run --project tools/level-qa -- --daily <pack> [--spec <daily_v1.json>] [--partial] [--no-reproduce]
  ```
  `--self-test daily` prueba todos los fixtures.
- **Severidad:**
  - **fallo:** esquema (vía `validate_schema.py --pack`), slot ↔ id/seed, 730 días por banda, K/vacíos/capacidad, ventana del bot (R-Q11 abierta), A*, par ≥ 2K, tope de pares **por banda según la especificación** (R-Q1b abierta), sin mecánicas ni ocultos, `relief` vacío y campos prohibidos, ≤ 14 cuias, < 2 MB (siempre), y que la semilla reproduzca el tablero entero (proceso hijo del generador, 15 min de tope);
  - **solo aviso (R-Q10):** anti-repetición (forma y Hamming, con la vuelta del ciclo 729 → 0) y trampas tempranas.
- **Detalles:**
  - Con `--partial` una muestra nunca pasa por pool completo: siempre sale el aviso «no es el pool completo».
  - El despacho de `Program.cs` va antes del `--self-test` genérico, por la misma razón que en R2.
- **Verificación:**
  - `level-qa --self-test daily` → 30 PASS; las autopruebas que ya había → todo PASS;
  - QA de la muestra con `--partial` → exit 1: 15 `QA-DAILY-RISK` (los K 9 de B2 y todo B3) y 2 `QA-DAILY-BOT` (`d2_n0002`, `d2_n0006`). Es lo esperado mientras R-Q1b y R-Q11 sigan abiertas;
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

### Bloque (a) (R2-R4: el modo diario del generador y su QA): **listo para integrar**

Está en `spark/reto-del-rio` y no toca Unity. Enganches del bloque:
- **E15** · `tools/level-generator/Program.cs` (líneas 133-143 en `ba57160`): que el `--self-test` sin nombre ejecute también `daily`:
  ```diff
           int holgura = HolguraSelfTest.Run();
  -        return Math.Max(Math.Max(Math.Max(tinaja, seasons), Math.Max(variants, registry)), Math.Max(curationTest, holgura));
  +        int daily = AmazonFill.Tools.LevelGenerator.Daily.DailyCommand.Run(new[] { "--self-test", "daily" });
  +        return Math.Max(Math.Max(Math.Max(Math.Max(tinaja, seasons), Math.Max(variants, registry)), Math.Max(curationTest, holgura)), daily);
  ```
  - Añadir `|daily` a la línea 33 del texto `Usage` (`--self-test [tinaja|…|holgura|daily]`) y «, holgura o daily» al mensaje de la línea 176.
  - Documentar `--daily` en `tools/level-generator/README.md`.
  - Prueba: `dotnet run --project tools/level-generator -c Release -- --self-test` incluye «daily» y sale con 0.
- **E16** · `tools/level-qa/level-qa.csproj`: enlazar las fuentes diarias del generador en lugar de copiarlas:
  ```xml
  <Compile Include="../level-generator/Daily/*.cs" Exclude="../level-generator/Daily/DailyCommand*.cs" LinkBase="Shared/Daily" />
  ```
  y borrar `tools/level-qa/Daily/Shared/` (más la comprobación de identidad de su autoprueba). Con eso, QA-DAILY-REPRO puede generar en memoria con `DailyRunner` en lugar de lanzar un proceso hijo. Prueba: `dotnet run --project tools/level-qa -- --self-test daily`.
- **E17** · `tools/level-qa/Program.cs` y `README.md`: el `--self-test` sin nombre de level-qa solo ejecuta `key2`, así que no hace falta sumarle `daily`. Basta con documentarlo: en `Usage` (línea 27) `--self-test [key2|reglas|variante|daily]`, en el mensaje de la línea 97 «key2, reglas, variante o daily», y `--daily` en `tools/level-qa/README.md`.
- **E18** · encargo §3.3, puntos 1 y 2: decir «antes del `--self-test` genérico» en lugar de «antes de `--parity`» o «antes de la lectura de argumentos de la campaña».

## 2026-10-19 · Licencia de Unity: RESUELTO

El dueño revisó la licencia en Unity Hub. Desde entonces Unity compila y pasa los tests (R6 en verde). La entrada de abajo queda como historial.

## 2026-10-19 · ⚠ BLOQUEO: la licencia de Unity dejó de valer en este equipo (pendiente del dueño)

- **Desde** unas 12:10 (hora local) ningún batchmode arranca: `unity-compile.ps1` y `unity-test.ps1` salen con el código 6.
- **Log** (`.claude/worktrees/spark-r6/Logs/compile.log`):
  - `Error: Code 404 … Found 0 entitlement groups and 0 free entitlements matching requested entitlement ids`;
  - `'com.unity.editor.headless' was not found`;
  - `No valid Unity Editor license found`.
- **Qué no es la causa:** el aviso «Code 10 while verifying Licensing Client signature» ya salía en las pasadas buenas de la línea base (10:55 y 11:33), cuando sí se resolvían los derechos. Unity Hub (3.22.3) y la CLI (1.0.0-beta.13) no han cambiado de versión.
- **Hipótesis:** se retiró o caducó el derecho de la licencia en el servidor. Por ejemplo, el asiento está en uso en otro equipo, o la licencia no incluye `com.unity.editor.headless`.
- **No he usado `unity auth` ni `unity license`** (regla dura 5).
- **Lo que necesito del dueño:** revisar la licencia de Unity de este equipo en Unity Hub (Preferencias › Licencias). Cuando vuelva a funcionar, avisa al Spark para repetir las pruebas de Unity pendientes:
  - R6 en el worktree `spark-r6`: compilar y pasar EditMode (esperado 2.851: 2.848 + 3 omitidos);
  - todo lo que haya del cliente desde R7.
- **Qué afecta:** todo el carril del cliente (R6-R11) queda sin verificar en Unity. Sigo escribiendo el código y lo compruebo fuera de Unity, pero no lo doy por terminado ni lo fusiono hasta pasar Unity. El carril de herramientas (R2-R4) no lo necesita.

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

- **Pool completo** (3 × 730), generado fuera del repositorio: 6,1 s con 20 hilos y 1.445.059 bytes. Valida con `--pack` y es reproducible. **No se publica** (R5 bloqueada).
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
  - R-Q1b: si el tope no admite el K, el tablero lleva el **mínimo de pares posible** y queda marcado, sin cambiar K ni paleta;
  - R-Q11: si ningún candidato entra en la ventana, se guarda **el más cercano** y queda marcado;
  - cabecera con `empties_default` 1 y `max_risk_pairs` igual al máximo real, así que R4 comprueba el tope por banda contra la especificación;
  - `world` en cada tablero (opcional en `daily`);
  - un tablero por línea, como `PackWriter`;
  - `skippable` false (04 §2.6 e) y `hint_supported` true (06 §4.1; R-Q8).
- **Para el dueño (datos de R-Q11):**
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
- **Desviación del encargo (§3.3, punto 1):** el bloque del despacho va **antes del `--self-test` genérico** (tras `--validate-spec`), no justo antes de `--parity`: el genérico solo reconoce subcomandos fijos y daría error de uso con `--self-test daily`. Son 5 líneas, en su commit «Enganche:». El revisor lo da por correcto y necesario. Propongo al integrador corregir §3.3 punto 1 del encargo.
- **Errata:** el mensaje de `da6a9bb` dice «43 especificaciones mal formadas»; son 44. No lo reescribo.
- **[P] en `daily_v1.json`:** paleta de B2 y B3 (R-Q1b), regla del agua `1 + (día mod 9)` (R-Q2), `k_by_day` (día par → K menor, impar → mayor) y lo que haga R3 con los días fuera de ventana (R-Q11). Los tintes y los pares de riesgo se leen de `campaign_v2.json` (`tints_from`), sin copiarlos.

## 2026-10-19 · R1 · Nota de diseño: HECHA

- **Commits:** `3e0063e` (la nota) y `4968d1a` (junta dos comentarios duplicados), en `spark/reto-del-rio`.
- **Archivo:** `docs/dev/reto-del-rio.md`.
- **code-reviewer:** primera ronda CAMBIOS NECESARIOS (6 bloqueantes: el contrato del almacén no seguía a `ILevelStateStore`; la ventana del bot era a la vez aviso y compuerta; la reconciliación sin red se daba por regla; las opciones de R-Q1b estaban incompletas; faltaba dónde viven los tests que ven AF.LiveOps; cinco citas imprecisas). Arreglados todos; segunda ronda **APROBADO**.
- **Para el integrador:**
  - **Confirma** que `IServerDate` y `FakeServerDate` van en **AF.Platform** (08 §2.2 le da el «`Date` del servidor para el reloj») y no en AF.LiveOps, como decía el encargo §4. Nota §1.2.
  - Las decisiones R-Q1-R-Q10 llevan fecha 2026-10-20 en el encargo; el reloj de este equipo dice 2026-10-19. Confírmalo al llevarlas a `decisiones.md`.
  - Ediciones del GDD que piden R-Q4-R-Q8 (08 §4.3, 08 §9 tipo de `daily_overrides`, 06 §4.1): nota §1.3 y enganche E11.

### Preguntas nuevas para el dueño (nota §1.6)

| Id | Pregunta | Opciones (recomendada primero) | Qué bloquea |
|---|---|---|---|
| **R-Q1b** | R-Q1 no se puede cumplir: B2 con K = 9 tiene 3 pares de riesgo (tope 1) y B3, con los tintes de 1-200, al menos 3 con K = 10 y 4 con K = 11 (tope 2). Cálculo en la nota §1.1 | (a) B2 solo con K = 8 y B3 con `max_pares_riesgo` libre, como la campaña desde el 201; (b) subir los topes: B2 a 3 y B3 a 4; (c) mantener los topes y bajar K (B2 = 8, B3 = 9); (d) ampliar la paleta de B3 a 1-300 (no basta) | Paleta de R3 (va como parámetro [P]) y la publicación del pool (R5) |
| **R-Q11** | Si en 30 candidatos ningún tablero entra en la ventana del bot de su banda (03 §4.4 midió 0,88-1,00 de fallo con 1 vacío; B2 pide 60-80 %) | (a) la ventana es compuerta: el día no se publica hasta ajustar la especificación; (b) se publica el más cercano y QA avisa; (c) ensanchar la ventana (cambia 03 §16). Mientras tanto, (a) | R5 |
| **R-Q12** | Al reconectar, qué hace la reconciliación con las victorias cobradas sin red (08 §10.1) | (a) nada: no se retiran semillas; (b) anular las de fecha posterior a la del servidor (sello y racha) | Solo esa rama de R7b (irá detrás de una opción) |
| **R-Q13** | ¿Intersticial al terminar un Reto? (05 §6.1 solo habla de la campaña) | (a) no; (b) como en la campaña | R8/R9 (sin decisión: no hay) |
| **R-Q14** | La Pororoca de la racha puede llegar a B1 antes del nivel 30, que la desbloquea (04 §2.4) | (a) entra en el stock y espera al 30; (b) otro premio en B1; (c) nada antes del 30 | Solo un aviso en la UI de R9 |

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

---

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

### Pendiente del dueño (para desbloquear R0)

1. **.NET SDK 10.0.401** para Windows ARM64 en `C:\Program Files\dotnet` (lo fija `global.json`, con `rollForward: latestFeature`).
2. **`jsonschema`** en Python 3.14 (`py -3.14 -m pip install jsonschema`).
3. **Unity 6000.3.25f1** (changeset `e1dba0a9aba4`), Editor **Windows ARM64**, con el módulo Android, en `C:\Program Files\Unity\Hub\Editor\6000.3.25f1\`, desde el Hub (la CLI no instala; `CLAUDE.md`, «Stack fijado»). La licencia la activa el dueño.
   - El 6000.3.26f1 x86_64 que hay instalado **no se usa**: abrirlo sobre el proyecto lo actualizaría.
4. Avisar al Spark cuando esté para repetir R0 y medir la línea base.

### Decisiones del dueño registradas (para que el integrador las lleve a `docs/dev/decisiones.md`)

Las R-Q1 a R-Q10 de la sección 5 del encargo, decididas por el dueño el 2026-10-20 («Acepto tus propuestas para R-Q1 a R-Q10 del Reto del río; déjalas en el encargo del Spark»). Se aplicarán tal cual cuando se desbloquee el trabajo:

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

### Riesgos y notas

- Las fechas: el encargo y estas decisiones van fechados el 2026-10-20; el reloj de esta máquina dice 2026-10-19.
- `git switch -c … origin/integracion-fase1-fase2` deja como upstream la integración; el push con `-u origin spark/reto-del-rio` lo corrige para que ningún `git push` vaya a `integracion-fase1-fase2`.

### Enganches para el integrador

Ninguno todavía.
