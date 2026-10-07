# Prototipo jugable v0 (teléfono del dueño)

Fecha: 2026-10-06 · Autor: tech-lead · Parte de `main` en `9b54134` (árbol limpio). Objetivo: GDD.md §9 punto 13. Los niveles 1-10 de 02 §13 deben poder jugarse en el SM-S938B y un agente debe poder verificarlos con `adb`.

**Dentro:**
- reglas R1-R16 sin mecánicas ni ocultas (02 §1);
- estados LOADING, INTRO, PLAYING, CELEBRATION, VICTORY_SCREEN y STUCK_MODAL (02 §2);
- tap-tap C1-C8 con la cola de 2 vertidos (02 §3);
- juice de 02 §4.1 y Sumergir y teñir simplificada y saltable (07 §5.1);
- Marea ilimitada (04 §2.1, contrato 17) y Volver a la orilla (04 §2.7);
- modal Aguas estancadas (02 §7) y victoria;
- HUD de 07 §7.4 con nivel, Marea y Volver a la orilla;
- progreso guardado, orientación vertical y nombre "Tintalú (dev)".

**Fuera:** tienda, anuncios, Semillas, vidas, sonido, arte final y shader `Liquid`; dedo guía, tooltips, Cuia extra y pista por inactividad; C9, C10 y la confirmación de reinicio; guardado a mitad de nivel (R8) y localización.

Los textos de la UI salen del glosario (§5.1, §10) y de 04 §6.5.

## a) API del núcleo (core-dev)

```csharp
// AF.Core.Rules (todo public; los static se suman a PourRules junto a IsLegal, Amount y Apply)
public readonly struct Pour : IEquatable<Pour> { int Source; int Target; int Amount; }   // propiedades de solo lectura
static bool IsFutile(BoardState s, int a, int b);                            // 02 §1.4 + ocultas (04 §5.1)
static int  CollectUsefulMoves(BoardState s, bool futileRuleEnabled, Pour[] buf); // legal y no fútil; orden (a, b) ascendente
static bool HasUsefulMove(BoardState s, bool futileRuleEnabled);             // sin asignaciones
static bool IsStuck(BoardState s, bool futileRuleEnabled);                   // !IsWon && !HasUsefulMove (R10, R13)
// AF.Core.Session
enum LevelStatus { Playing, Won, Stuck }
readonly struct PourResult { Pour Pour; bool TargetSealed; LevelStatus Status; }
sealed class LevelSession {
  LevelSession(BoardState initial, bool futileRuleEnabled);   // NotSupportedException si hay ocultas
  BoardState Initial, Current; LevelStatus Status; int AppliedPours; bool CanUndo;
  bool TryPour(int a, int b, out PourResult r); bool Undo() /* Marea */; void Restart() /* Volver a la orilla */ }
```

`BoardState` pasa a implementar `IEquatable<BoardState>` (compara `Packed` en orden). Las ramas de Lluvia y Remolino esperan a 02 §9.

**Invariantes:**
- I1: `Status` se recalcula tras cada cambio, en el orden de R10.
- I2: un vertido ilegal (R11) o con `Status ≠ Playing` devuelve false y no cambia nada.
- I3: `Undo` restaura el estado anterior exacto, reabre los sellos y resta 1 a `AppliedPours` (04 §2.1).
- I4: `Restart` deja `Current.Equals(Initial)` y el historial vacío (04 §2.7).
- I5 (R16): un vertido fútil conserva el multiconjunto de `Packed`.

**Tests compartidos:**
- `FutileRulesTests`: R13 sin vertidos legales, R13 solo con fútiles, R16 y los casos 1-3 de 04 §5.1.
- `LevelSessionTests`: I1-I4, R12, Marea desde `Stuck` vuelve a `Playing`, y las ocultas se rechazan.

## b) Datos de los niveles 1-10 (level-pipeline-dev)

`af-level/1` no admite niveles hechos a mano: `gen.source` no tiene ese valor y `bot_fail` exige el bot ×40. v0 usa un **subconjunto documentado** sin `pack_schema`, en `Assets/_Project/Data/Levels/tutorial_v0.json`, que Unity carga como TextAsset. Los campos que lleva tienen el mismo nombre, tipo y semántica que en 08 §4.1:

```json
{ "subset_of": "af-level/1", "pack_id": "tutorial_v0", "kind": "campaign", "world": 1, "world_id": "w01_estuario",
  "lap": 1, "water_base": "white", "levels": [ { "id": "m01_v1_n0001", "level": 1, "world": 1, "lap": 1,
  "water_base": "white", "palette": ["acai", "aguaje"], "units_per_color": 4, "caps": [4, 4, 4],
  "cuias": [[8, 8, 8, 4], [4, 4, 4, 8], []], "attrs": [null, null, null], "murky": false, "hidden": [],
  "difficulty": { "par": 3 }, "relief": [] } ] }
```

El nivel 1 copia literalmente 02 §13. Los niveles 2-10 se diseñan a mano (03 §4.4) en `tools/level-generator/specs/tutorial_v0.json`. `level-generator --handmade` emite el pack y `tools/out/tutorial_v0_report.md`, y falla si algún nivel incumple alguna de estas condiciones:
- la tabla de 02 §13: cuias = colores + vacíos y sus tintes, cap 4, `U_c` = 4, debuts y 0 pares de riesgo;
- es resoluble y su par entra en el rango (BFS sobre `PourRules`, ≤ 8 cuias);
- no está bloqueado al empezar;
- nivel 2: hay un vertido parcial de una racha de 3 sobre un hueco de 2;
- nivel 5: un color está repartido en 3 cuias;
- nivel 6: tiene un solo vacío.

El informe trae, por nivel, la solución óptima y el camino más corto a Aguas estancadas. Las soluciones nunca entran en el pack (08 §4.1).

## c) Capa Unity (unity-gameplay-dev)

Se usan los módulos de 08 §2.2 y no un `AF.Game`, porque `Scripts/Game` queda fuera de los límites de este agente.

| asmdef (carpeta en `Scripts/`) | Referencias | Contenido |
|---|---|---|
| `AF.Levels` (sin motor) | AF.Core | lector JSON mínimo e invariante; `LevelPackReader` produce `BoardState`; acepta `subset_of` o `pack_schema` 1 |
| `AF.GameFlow` (sin motor) | AF.Core, AF.Levels | `LevelFlow` (02 §2), `TapController` (C1-C8), `IProgressStore` y las constantes de 08 §9 |
| `AF.Presentation` | AF.Core, UnityEngine.UI | HEX de 07 §3, sprites generados por código, `CuiaView`, cola de animación, intro y sellado (07 §5.3) |
| `AF.UI` (raíz) | todos + Unity.InputSystem | `GameBootstrap` (Canvas 360×780 con zona segura), `BoardLayout` (02 §5), HUD, modal, victoria, `PlayerPrefsProgressStore`, `DevProbe` |
| `AF.UI.Editor` (`UI/Editor`) | AF.UI | `GameSceneBuilder.CreateGameScene` |

**`CreateGameScene`:**
- genera `Assets/_Project/Scenes/Game.unity` con la cámara, un EventSystem con `InputSystemUIInputModule` y `GameBootstrap` enlazado al TextAsset;
- deja esa escena como la única de Build Settings.

**Detalles de la capa:**
- Los textos usan `UI.Text` con la fuente `LegacyRuntime.ttf`, sin importar TMP.
- Tests: `Tests/EditMode` (lector, `TapController`, `LevelFlow`) y `Tests/PlayMode` (gana el nivel 1).
- Progreso: se guarda en `af_v0_level` y, tras ganar el nivel 10, se vuelve al 1.

**DevProbe** (solo si `Debug.isDebugBuild`; JSON en una línea, InvariantCulture; píxeles con el origen arriba a la izquierda; se toca en el centro):
```
AF_LAYOUT {"screen":"level","level":3,"sw":1080,"sh":2340,"cuias":[{"i":0,"x":12,"y":600,"w":144,"h":528}],"buttons":[{"id":"undo",...}]}
AF_STATE {"event":"pour","t":5321,"level":3,"phase":"PLAYING","status":"playing","pours":4,"selected":-1,"move":[0,2,1],"cuias":[[8,8,8],[4,4,4,4],[4]]}
```
- `AF_LAYOUT` se escribe al dibujar cada nivel y al abrir `stuck` (`undo_modal`, `restart_modal`) y `victory` (`next`).
- `AF_STATE` se escribe en los eventos `load`, `ready`, `pour`, `reject`, `undo`, `restart`, `win` y `stuck`.
- El extra de intent `af_level` elige el nivel de arranque.

## d) Build de desarrollo (build-release)

`Assets/_Project/Editor/CI/` es de build-release, así que esta parte no puede ir a los otros tres agentes.
- `ProjectSetup`: `ProductName = "Tintalú (dev)"` y orientación vertical sin autorrotación. `Validate()` comprueba ambas cosas, así que `BuildSmoke` sigue funcionando.
- `BuildDev.BuildAndroidDev` construye `Builds/Android/dev.apk` como Development Build con las escenas de Build Settings. Códigos de salida: 0 OK · 2 ajustes · 3 sin Android · 4 build fallido · 5 falta `Game.unity`.

## e) Aceptación y comandos

1. `dotnet build -warnaserror` y `dotnet test` en verde (39 + nuevos); `validate_schema.py` sigue en 23/23.
2. Generar el pack dos veces da el mismo SHA-256; el informe tiene los 10 niveles en verde y al menos un camino de bloqueo.
3. `unity-compile` devuelve 0; EditMode y PlayMode en verde; `BuildDev` devuelve 0; `check-16kb.sh` OK.
4. En el teléfono la etiqueta es "Tintalú (dev)" y `sw < sh`.
5. Jugando con `input tap` según el informe, los niveles 1-10 terminan en `win` con `pours` igual al par, y "Siguiente" abre el nivel siguiente.
6. Un toque ilegal da `reject` sin cambiar `pours`. Un toque en la intro da `ready` en menos de 50 ms sin seleccionar. Sin tocar, de `load` a `ready` pasan ≤ 1,5 s.
7. Siguiendo el camino de bloqueo llega `stuck`, y `AF_LAYOUT stuck` aparece ≥ 400 ms después (R15). `undo_modal` vuelve a `playing` y `restart` deja las cuias como al inicio.
8. Si se gana el 3 y se hace `force-stop`, al relanzar se abre el nivel 4.

Además de los comandos de verificación de CLAUDE.md:
```
dotnet run --project tools/level-generator -- --handmade tools/level-generator/specs/tutorial_v0.json --out Assets/_Project/Data/Levels --report tools/out/tutorial_v0_report.md
unity run <repo> --architecture arm64 --log-file Logs/<x>.log --non-interactive -- -nographics -executeMethod <ProjectSetup.ApplyAndroidDefaults | AF.UI.Editor.GameSceneBuilder.CreateGameScene>
unity run <repo> --architecture arm64 --log-file Logs/build-dev.log --no-tail --timeout 5400 --non-interactive -- -nographics -buildTarget Android -executeMethod AF.EditorTools.BuildDev.BuildAndroidDev
adb install -r Builds/Android/dev.apk; adb shell wm size; adb shell cmd package resolve-activity --brief com.amazonfill.dev
adb shell am start -n com.amazonfill.dev/<actividad> --ei af_level 1; adb logcat -d -s Unity | grep -E "AF_(LAYOUT|STATE)"; adb shell input tap <x> <y>
```

## f) Reparto (sin archivos compartidos)

| # | Agente | Archivos | Depende de |
|---|---|---|---|
| T1 | core-dev | `Runtime/AF.Core/{Rules,Board,Session}/*.cs`, `Tests/Editor/AF.Core.Tests/{FutileRules,LevelSession}Tests.cs`, `README.md`, `CHANGELOG.md` | — |
| T2 | level-pipeline-dev | `tools/level-generator/**`, `Data/Levels/tutorial_v0.json` y `tools/out/tutorial_v0_report.md` (generados) | T1 (diseño en paralelo) |
| T3 | build-release | `Editor/CI/{ProjectSetup,BuildDev}.cs` y el `ProjectSettings.asset` que regenera Unity | — |
| T4 | unity-gameplay-dev | `Scripts/{Levels,GameFlow}/**`, `Tests/EditMode/**` | T1 |
| T5 | unity-gameplay-dev | `Scripts/Presentation/**` | T4 |
| T6 | unity-gameplay-dev | `Scripts/UI/**`, `Tests/PlayMode/**`, `Game.unity`, `EditorBuildSettings.asset` y el `.meta` del JSON (generados) | T2, T3, T5 |
| T7 | sesión principal, en `main` | verificación (e) en el teléfono | T1-T6 |

T1, T2 y T3 pueden ir en paralelo. Nunca más de 2 Editores abiertos. Cada tarea pasa por code-reviewer.

## Preguntas para el dueño (ninguna bloquea v0)

1. ¿Se añade `"hand"` a `gen.source` y se admite `difficulty` sin `bot_fail` en los niveles hechos a mano? Sería cambio de contrato y subiría `pack_schema` a 2.
2. Los niveles 1-4 de 02 §13 incumplen `par ≥ 2·K` (08 §4.1). ¿Queda exento el tutorial?
3. 08 §2.2 hace que GameFlow dependa de UI, pero el principio 2 lo prohíbe. ¿Se acepta la UI como raíz?
4. ¿Se acepta PlayerPrefs provisional, fuera de `AF.Save`?

## Riesgos

- La "ú" del nombre acaba en rutas de Gradle y Burst. Si falla, se usa "Tintalu (dev)".
- `sw×sh` tiene que coincidir con `wm size` para que los toques de `adb` caigan donde deben.
- Unity reescribe `.asset`: cada rama commitea solo los que cambia su método.
- Newtonsoft solo llega como dependencia transitiva: no se usa.
