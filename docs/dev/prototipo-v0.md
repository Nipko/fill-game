# Prototipo jugable v0 (teléfono del dueño)

Fecha: 2026-10-06 (actualizado el 2026-10-07 con las decisiones del dueño D-001, una dosis por vertido, y D-002, sin resaltado de destinos; actualizado el 2026-10-08 con D-005, vertido por tramo como Magic Sort, que sustituye a D-001; registro en `docs/dev/decisiones.md`) · Autor: tech-lead · Parte de `main` en `9b54134` (árbol limpio). Objetivo: GDD.md §9 punto 13. Los niveles 1-10 de 02 §13 deben poder jugarse en el SM-S938B y un agente debe poder verificarlos con `adb`.

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

Con D-001 se añaden las sobrecargas con `PourMode` (`LevelSession(initial, futileRuleEnabled, pourMode)`, `PourRules.Amount`, `Apply` y `CollectUsefulMoves` con modo, `SolverOptions.ForPipeline(mode)`; API de T1 en `docs/dev/decisiones.md` §3). Las firmas sin modo equivalen a `run`, el modo por defecto desde D-005, hasta que T6 las retire.

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

El nivel 1 copia literalmente 02 §13. Los niveles 2-10 se diseñan a mano (03 §4.4) en `tools/level-generator/specs/tutorial_v0.json`. `level-generator --handmade` emite el pack y `tools/out/tutorial_v0_report.md` en el modo de vertido de `--pour-mode run|single` (por defecto `run`, D-005: el par, la solución y el camino a Aguas estancadas son de ese modo, y el informe lo declara), y falla si algún nivel incumple alguna de estas condiciones:
- la tabla de 02 §13: cuias = colores + vacíos y sus tintes, cap 4, `U_c` = 4, debuts y 0 pares de riesgo;
- es resoluble (BFS sobre `PourRules`, ≤ 8 cuias) y su par `run` entra en el rango [P] de 02 §13 (pares del pack: 3, 6, 7, 8, 10, 10, 11, 13, 14 y 16);
- no está bloqueado al empezar;
- nivel 2: en `run`, hay un vertido parcial de una racha de 3 sobre un hueco de 2 (la lección de 02 §13); en `single`, de pruebas, la solución óptima contiene ≥ 3 vertidos seguidos con el mismo origen y destino;
- nivel 5: un color está repartido en 3 cuias;
- nivel 6: tiene un solo vacío.

El informe trae, por nivel, la solución óptima y el camino más corto a Aguas estancadas. Las soluciones nunca entran en el pack (08 §4.1). El pack no declara su modo y se genera en `run` (pregunta P7, reescrita): uno generado en `single` y jugado en `run`, o al revés, da pares falsos.

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
- Modo de vertido (D-005): `GameConfig.RulesPourMode` (`rules_pour_mode`, 08 §9, default `"run"`) se convierte con `PourModes.TryParse` al cargar cada nivel; un valor desconocido se trata como `run` con un aviso en el log.
- Selección (D-002): solo se eleva el origen; ninguna otra cuia cambia y no se resalta ningún destino.

**DevProbe** (solo si `Debug.isDebugBuild`; JSON en una línea, InvariantCulture; píxeles con el origen arriba a la izquierda; se toca en el centro):
```
AF_LAYOUT {"screen":"level","level":3,"sw":1080,"sh":2340,"cuias":[{"i":0,"x":12,"y":600,"w":144,"h":528}],"buttons":[{"id":"undo",...}]}
AF_STATE {"event":"pour","t":5321,"level":3,"pour_mode":"run","phase":"PLAYING","status":"playing","pours":4,"selected":-1,"tap":2,"move":[0,2,2],"amount":2,"cuias":[[4],[3,3,3,3],[8,8]]}
```
- `AF_LAYOUT` se escribe al dibujar cada nivel, al abrir cada modal y cuando algo cambia de sitio (detalle abajo). Las pantallas se repiten: para tocar, usa la **última línea de la pantalla que está a la vista** (para el tablero, la última `level`). Cerrar un modal no vuelve a publicar lo que queda debajo: al cerrar la Pausa, al cancelar ¿Salir? o ¿Reiniciar?, al Reintentar con canoas sin «El río bajó» o al cerrar Sin canoas para seguir jugando, el tablero vuelve a verse sin una línea `level` nueva.
  - `level` (`undo`, `extra_cuia` desde el 8, `restart`, `pause`, y `level_card` y `reveal` si se ven) sale al final de la carga del nivel, después de los ganchos `*LevelLoaded` y con la bandeja ya refrescada (trae `extra_cuia` desde la primera línea, también al reabrir la app). Se repite cuando aparece o desaparece una ranura de la bandeja (Cuia extra o Revelar, que la recentran), al cambiar la pantalla y al acabar el re-layout de una cuia añadida (Cuia extra, rescate o Balsa): una sola línea nueva, con las N + 1 cuias en su sitio final; si en esos 600 ms se abrió un modal del nivel (la Pausa, ¿Salir? o ¿Reiniciar?), detrás se repite la línea del modal.
  - Modales del nivel: `pause`, `quit_confirm` y `restart_confirm` al abrirse; `level_card` (los ids `level_card`, el rectángulo de la tarjeta: cualquier toque la cierra, y `level_card_hito`, el de la fila del hito p20, solo cuando se ve: tarea M2.5; se repite si la tarjeta abierta, ya entrada, se recoloca con otra escala u otra pantalla) y `world_banner` (`"screen":"world_banner"`, el banner de mundo, con el id `world_banner` y el rectángulo de su tarjeta: cualquier toque lo cierra) al terminar su entrada (en la línea `level`, el id `world_banner` solo sale con la tarjeta a la vista, no mientras el banner espera su arte); `stuck` (los que se ven de `undo_modal`, `refill_video`, `refill_seeds`, `rescue`, `handful`, `balsa`, `restart_modal` y `give_up`) y `victory` (`next`, `home` y `double_harvest` si se ofrece) al terminar de subir el panel.
  - Embudo del nivel (D-008), al abrirse: `giveup_confirm` (`close`, `rescue` si se ofrece y `give_up`) y `level_failed` (`retry` y `close`), también al reabrir la app en ellos, y `no_canoes` (`close`, `extra_canoe_video`, `refill_canoes` e `infinite_canoes` si se ofrecen, y `home`), que se repite cada vez que rehace su layout (cambia el video, el saldo o lo que ofrece). Sin canoas no se guarda: la app se reabre en Nivel fallido o en el nivel.
  - Inicio: `lobby`, `content_end` y `no_canoes`.
- `AF_STATE` se escribe en los eventos `load`, `ready`, `pour`, `reject`, `undo`, `restart`, `win` y `stuck` (más `select` y `deselect`, de ayuda). Cada línea lleva `pour_mode` (`run` o `single`, el modo de la sesión del nivel); los eventos con vertido (`pour`, `undo`) llevan `move` = [origen, destino, cantidad] y la cantidad suelta en `amount` (en `run`, la racha movida; en `single`, siempre 1); `pours` cuenta vertidos aplicados.
- `AF_STATE load` lleva además los Patrones de fruta del nivel (T-E de la prueba del 2026-10-09): `"patterns":{"on":true,"opacity":0.20}`. `on` es el interruptor Patrones de fruta ya resuelto con el ajuste de Ajustes (`settings.patterns`, tarea L3): en Automático, activo desde el 41 (default de `ui_patterns_default_from_level`, 08 §9); con Siempre o Nunca, el ajuste, en cualquier nivel. Activo = icono de fruta en cada dosis visible. `opacity` es la opacidad del patrón que lleva el shader (`_PatternOpacity`): 0.20 con los Patrones activos (default de `ui_pattern_opacity`, 08 §9) y 0.15 sin ellos. Así, con Siempre o Nunca, `on` y `opacity` no siguen al número del nivel y no es un fallo. Con la cuia elevada, el icono sale en cualquier nivel (07 §3, regla 2), y el origen de un vertido lo conserva en el viaje y el caudal hasta que empieza su retorno (07 §5.2). El atlas de los 14 iconos se genera una vez por sesión, en la primera carga de nivel: en builds de desarrollo, `AF_ART fruit_icons <ms> ms` dice lo que cuesta en el teléfono (presupuesto en el Editor: 30 ms).
- **Ajustes (tarea L3; 02 §12; D-018 Q7-A):** el engranaje del inicio abre la pantalla de Ajustes (X o Atrás de Android la cierran; Atrás, a la primera también tras un arranque en frío: tarea FXT) con **Idioma** («Automático» y cada idioma con la UI completa; hoy Español y, en desarrollo, en-XA) y **Patrones de fruta** (Automático = activo desde el 41, Siempre o Nunca: `settings.patterns`, se aplica al momento al nivel cargado; con Nunca queda el patrón sutil del 15 %). Elegir un idioma lo guarda en `settings.language` y, si cambia el de la UI, reconstruye toda la UI volviendo al inicio, con Ajustes otra vez abierta en el idioma nuevo y la opción elegida marcada (FXT; sale otra vez `AF_STATE settings` con su `AF_LAYOUT`, y no `settings_closed`). Atrás de Android es un solo camino para las hojas del inicio: cierra Ajustes y vuelve de «El río sigue creciendo» y de Sin canoas; en el nivel, con la «Compra simulada», un anuncio o el aviso del perfil bloqueado no hace nada. Líneas: `AF_STATE settings` al abrir (`language`, `patterns`, `offered`), `settings_change` al elegir (`key` = `language`, `patterns` o `sloth_messages` de TI3, `value`; con `patterns`, `on` y `opacity` ya aplicados) y `settings_closed` al cerrarla (X o Atrás); `AF_LAYOUT` con `"screen":"settings"` y los ids `close`, `lang_auto`, `lang_<idioma>` y `patterns_<auto|on|off>`. Aún no existe Alto contraste (patrón al 35 % y contorno de dosis de 1,5 px).
- `AF_STATE loc` (L2b y L3, `DevProbe.Loc.cs`; al arrancar y al elegir idioma): `{"event":"loc","t":412,"locale":"es-419","source":"device","culture":"es-CO","device":"es-CO","device_src":"jni","numbers":"es-CO"}`. `locale` es el idioma resuelto (`es-419`, `en`, `pt-BR` o, solo en desarrollo, `en-XA`); `source`, de dónde sale (`dev`, el extra `af_lang` > `saved`, el ajuste Idioma > `device`, el teléfono > `default`, en; o `fallback` = es-419 [P] si ningún candidato tiene la UI completa: hoy, con un teléfono sin etiqueta o en un idioma distinto del español, porque en y pt-BR aún no la tienen); `culture`, la de .NET (vacía si es la invariante); `device` (la etiqueta que leyó el teléfono con `java.util.Locale` por el JNI de Unity; vacía si no dio ninguna), `device_src` (`jni`, `culture` o `manual`) y `numbers` (la fila de cifras en uso: la región del teléfono o, sin ella, es-CO [P]). Es la medida de IL2CPP: si `device` sale vacío en el teléfono, avisar.
- Tarjeta previa y banner de mundo (T13 y A4; el banner, en el 11, el 41, el 81 y el 141, al entrar con Jugar o Siguiente y solo en el primer intento), con líneas `AF_STATE` propias, en este orden:
  - `world_banner` al abrirse, en `LEVEL_CARD` y antes de la intro: `{"event":"world_banner","level":41,"world":3,"text":"Mundo 3: Manaos · Brasil (Amazonas) · Negro + Solimões","then":"intro","reduce_motion":false}`. `then`: lo que sale al cerrarlo, `"level_card"` (la tarjeta previa) o `"intro"`.
  - `world_banner_art` cuando su tarjeta empieza a entrar, con su arte (que se pinta y se pasa a Color32 en otro hilo para no parar la carga del nivel) o sin él: `{"event":"world_banner_art","level":41,"world":3,"async":true,"wait_ms":48,"mascots_ms":231.1,"main_ms":1.4,"fallback":"none"}`. `wait_ms` es lo que esperó la tarjeta, en ms de reloj real desde que el banner pidió el arte al abrirse (≈ 0 si se pidió antes: en la victoria del 10, el 40, el 80 y el 140, o al ofrecer «Jugar» con uno de esos niveles); `mascots_ms`, lo que costó pintar el arte (CPU del otro hilo); `main_ms`, lo que ocupó el hilo principal (crear las texturas y subirlas; presupuesto de 08 §5.4: ≤ 8 ms); y `fallback`, `"none"` con el arte o, si entró sin él (la tarjeta, la placa y el texto, con la viñeta del color plano del agua y sin Rosa ni Capi), `"error"` (el trabajo falló), `"queue"` (el grupo de hilos no lo aceptó) o `"timeout"` (no llegó en 800 ms [P]; lo que llegue después se descarta). Antes de esta línea solo se ve el velo y los toques se ignoran: el banner no se puede saltar sin verse.
  - `world_banner_closed` en el toque que lo cierra, antes de lo que sigue: `{"event":"world_banner_closed","level":41,"world":3,"next":"intro"}`, con los valores de `then`. El banner tarda 340 ms en salir (140 con Reducir movimiento) y solo después sale la línea `level_card` o empieza la intro; un segundo toque durante la salida la termina en el acto.
  - `level_card` al abrirse la tarjeta previa: tipo, nivel, etiqueta, estreno, líneas y tamaño del tutorial, iconos, «?», Luciérnaga y escala; `bite_demo` = `"animated"` (el mordisco animado del estreno de la Piraña, 183), `"still"` (con Reducir movimiento) o `"none"`. Desde M2.5, el hito p20 (02 §10; D-013, punto 8): `hito` (el id de la parada a la que se llega al ganar, o null si no hay fila: también cuando falta el nombre aprobado del lugar o la fila se omite), `hito_silhouette` (`png`, `shape`, `code` o `none`) y `hito_omitted` (true si la fila no cabe en el alto libre de la pantalla [P]). La línea se repite, con la misma tarjeta abierta, cuando se recoloca (otra escala u otra pantalla) y cambia algo de lo que anota: no es una tarjeta nueva.
- El extra de intent `af_level` elige el nivel de arranque.
- El extra de intent `af_rules_pour_mode` (string, `run` | `single`; solo builds de desarrollo en Android; propuesta [P] de `docs/dev/decisiones.md` §2.1.7) sustituye a `rules_pour_mode` hasta que exista `AF.RemoteConfig`. Desde D-005 sirve para probar `single`: `adb shell am start ... --es af_rules_pour_mode single`. Un valor desconocido se trata como `run` con aviso.
- Extras de la tanda 4 (`LaunchOptions`; como los demás, solo en builds de desarrollo en Android y nunca en release):
  - `af_lang` (string; L2b): el idioma de la UI, primero en la resolución (`af_lang` > `settings.language` > teléfono > en; D-023, punto 26). Solo vale un idioma con la UI completa o `en-XA` (la pseudolocalización: +35 % y todo entre corchetes); con otro valor, el automático. `--es af_lang en-XA` da `AF_STATE loc` con `"source":"dev"`. En los tests, `LaunchOptions.LanguageOverride`.
  - `af_mech_off` (string; KS1; D-020, pregunta 31 b): apaga los kill switches `mech_enabled.<id>` de 08 §9 de una lista separada por comas, con los ids `hoja`, `matapi`, `liana`, `remolino`, `barro`, `lluvia`, `brote`, `pirana` y `tinaja` (la Hoja atada cuenta como `hoja`, la Flor como `liana` y el cucharón como `tinaja` [P]): `--es af_mech_off hoja,matapi`. Un id desconocido se ignora con un aviso en el log. En el teléfono solo prueba el respaldo: `level_overrides` sale de la fuente local, `{}`, así que se juega el nivel publicado con su aviso [P] (D-023, punto 40); la sustitución con un override solo la prueban los tests EditMode. En los tests, `LaunchOptions.MechOffOverride` y `LevelOverridesJsonOverride`.
  - `af_font_scale` (int, en tanto por ciento; A1 [P]): la escala de texto global (`UiBuilder.TextScale`), `--ei af_font_scale 130`, para ver los textos al 130 % de 02 §12 mientras la escala de fuente de Android no llega al juego. Se lee una vez, al crear el primer texto, y se lleva a 100-130 %. En los tests, `LaunchOptions.FontScaleOverride` (float: 1,3 = 130 %; D-023, punto 28), que solo rellenan los tests (ningún extra de intent lo rellena) y se fija antes de cargar la escena.
  - `af_bench_morpho` (int; PF1; 03 §8, 08 §5.4, D-015 y pregunta 44 a): con `--ei af_bench_morpho 1`, `DevBenchRunner` (`DevBench.cs`, solo en el Editor y en builds de desarrollo: release ni lo compila) mide en un hilo aparte Morpho desde cero (`SafeMoves` con `hint_budget_nodes` = 40.000, `hint_weight` = 2 y el corte de `hint_timeout_ms` = 150 ms de reloj real) en el tablero inicial de cada nivel con tinaja y de Aguas turbias del 100 en adelante, y escribe por nivel `AF_PERF {"event":"morpho","level":329,"id":"m07_v1_n0329","realities":24,"nodes":8298,"ms":21.35,"outcome":"found","cancelled":false,"safe":10,"cuias":11,"colors":8,"tinaja_doses":5,"hidden":4,"cold_ms":21.22,"runs":3,"cut_runs":0,"timeout_ms":150}` (`ms`, la mediana de 3 consultas tras una en frío, `cold_ms`; `outcome`: `found`, `no_exit`, `some_reality_solvable`, `budget_exceeded`, `won` o `realities_rejected`) y al final `AF_PERF {"event":"morpho_summary",...,"nodes_per_ms":...,"pc_nodes_per_ms":1000,"factor":...,"assumed_factor":8,...}`: los nodos por ms de las consultas de ≥ 5.000 nodos (los retos 329, 332 y 340 de la muestra de la tinaja, las mismas de la calibración del generador) y `factor` = 1.000 (`TinajaWalks.PcNodesPerMs` [E]) ÷ esa cifra, el que sustituye al 8× [P] de `MorphoDeviceFactor`. Mientras se juega un nivel escribe además `AF_PERF {"event":"frames","level":106,"rows":3,"cuias":11,"frames":600,"avg_ms":16.67,"p95_ms":17.10,"max_ms":24.00,"fps":59.99,"late":0}` cada 600 fotogramas y al cambiar de nivel (`late`: fotogramas de más de 20 ms; el shader se mide en los de `rows` = 3). Los niveles con tinaja aún no están en el juego: se suben con `adb push Assets/_Project/Tests/EditMode/Fixtures/pack3_sample.json /sdcard/Android/data/com.amazonfill.dev/files/af_bench/` y `adb push tools/out/sample_tinaja/mundo_07_v1.json /sdcard/Android/data/com.amazonfill.dev/files/af_bench/` (el bench lee todos los `*.json` de esa carpeta; en el Editor, las dos muestras del repositorio). Luego `adb shell am start -S -n com.amazonfill.dev/com.unity3d.player.UnityPlayerGameActivity --ei af_bench_morpho 1 --ei af_level 106` y `adb logcat -d -s Unity | grep AF_PERF`. En el Editor (PC del dueño, Mono): 424 nodos/ms en las consultas pesadas (21-24 ms), factor 2,36. En los tests, `LaunchOptions.BenchMorphoOverride` y `DevBenchRunner.Create`.
- `AF_PERF` (T-D) se escribe tras cada vertido con lo que costó guardar el perfil: `AF_PERF {"event":"pour","level":143,"writes":1,"save_ms":0.62,"bg_ms":4.10,"sync":false}`. `save_ms` es solo el hilo principal (serializar el perfil y entregarlo al escritor de segundo plano); `bg_ms`, la última escritura terminada del escritor (`.tmp`, fsync y `File.Replace` con `.bak`; casi siempre la del vertido anterior). `sync` = true marca un vertido cuya escritura fue síncrona: el que gana es fin de nivel, espera al escritor y escribe en el toque, así que su `save_ms` incluye el disco. Objetivo del dueño (decisión 5 del 2026-10-09; hoja de T-D: ≥ 200 vertidos en 3 niveles): en todos los vertidos, también los de `sync` = true, p95 de `save_ms` < 5 ms y ninguno > 8 ms. Pendiente del dueño: si el vertido que gana (`sync` = true, uno por nivel) puede quedar fuera de «ninguno > 8 ms»; hasta que conteste, cuenta. `AF_PERF levels_load` sale una vez al arrancar. `AF_PERF slot_measure` (F3) sale al cargar un nivel cuando las medidas de la ranura del Dato del río de la victoria no salen de la caché (la primera vez por idioma, escala y ancho): `{"event":"slot_measure","level":3,"locale":"es-419","scale":1.00,"ms":18.40,"fruit_ms":16.10,"slot_dp":139}`; `ms` es todo lo que costó medir la ranura en el hilo principal y `fruit_ms`, la línea de fruta más alta del calendario. Se mide al cargar el nivel para que la primera victoria no lo pague al subir el panel.
- **Sondas de la tanda 4** (Mapa del río, Frutas del río e idiomas; D-013, D-017, D-018 y D-023). Las que aún no existen se fijan aquí para que las tareas que las escriben compartan el formato, y la que añada o cambie un campo lo anota aquí en su mismo PR. Tras la ola 2 (`0f67f1f`) ya se escriben `stop` (M1.4), `arrival`, `stop_arrive`, `river_fact_shown` y la línea `AF_LAYOUT arrival` (M1.6) y `stop_label` (M2.3); tras la ola 3, `fruit_line` y la línea de fruta en `river_fact_shown` (F3); las de idioma y Ajustes (`loc`, `settings`, `settings_change` y `settings_closed`, ola 1) están arriba. Previstas: los ids `home_window` y `stop_remaining` del inicio (M2.1). Salen solo en builds de desarrollo, como las demás (cada función con su parcial `DevProbe.<Función>.cs`), y son eventos de `AF_STATE` con los campos que se indican:
  - `stop` al aplicar el fondo de la parada en la carga de un nivel (fondos por parada; 07 §2.1): `level`, `stop` (la k de la parada), `id` (el id del sitio), `world`, `hero` (el héroe que se pinta, o null), `lut` (`dusk`, `dawn` o null), `water` (el `water_base` del mundo), `fade` (si hubo fundido de 250 ms: no en la primera carga ni con Reducir movimiento) y `build_ms` (lo que costó construirlo; presupuesto: ≤ 8 ms).
    - M1.4 (`GameScreen.Stop.cs`) la escribe con esos campos: `hero` es la ranura (`bg_hero_<sitio>`, con su PNG o su silueta provisional) o la pieza del kit (`kit:<pieza>`) que se pinta, y null sin héroe (las piezas por código de Santarém y el Encuentro, o en release sin PNG); `fade` es un booleano; `build_ms` mide en el hilo principal la composición y las mallas de sus gráficos (sin el lote del Canvas, que hace el motor al pintar). Campos añadidos: `hero_src` (`png`, `shape` o `none`), `shape` (la silueta de `StopShapes`: desde M2.4, la propia de cada sitio; o null), `code` (la pieza por código o del kit, o null) y `change` (`cut`, `fade` o `same`; `same` también cuando no hay parada ni antes ni después). Ejemplo: `{"event":"stop","t":1234,"level":91,"stop":9,"id":"leticia_atardecer","world":4,"hero":"bg_hero_leticia","lut":"dusk","water":"white","fade":false,"build_ms":0.31,"hero_src":"shape","shape":"leticia","code":null,"change":"cut"}`.
    - Hoja de VA-20 (M2.4; direccion-visual-v2 §9): `StopSheetSnapshotTests` (explícito, con gráficos) es el único fixture que escribe las 20 composiciones `02f_board_stop_00`-`19` (pantalla completa, tablero de 2 filas del nivel 7 en el nivel `10k + 7`) y la hoja `02f_board_stops_sheet.png` de 5 × 4 que las reutiliza: de cada una, solo la banda del héroe (x de 40 a 320 dp; de 48 dp sobre el horizonte a 32 bajo él) a 1,5 px/dp, en casillas de 420 × 120 px con el número de la parada; borde ámbar (#E0A030) en las de asesor obligatorio (11 y 15 por 07 §2.1; 7 por la propuesta del Mapa §10.3, fila 7, que 07 §2.1 no marca: lo confirma el dueño). La banda sale de una segunda captura sin la cabecera (pausa, nivel, monedas, Canoas y Reiniciar) ni el follaje del marco, para que se vea la silueta entera y sin texto del juego; las 20 composiciones a pantalla completa siguen con ellos. `StopSnapshotTests` (M1.4) solo escribe `02b_board_l1` y `02f_board_stop_15_l151`. La leyenda sale también en el log, en las líneas `AF_SHEET <nn> <id> <forma>`:

      | n | id del sitio | forma | |
      |---|---|---|---|
      | 00 | belem | two_spires | |
      | 01 | santarem | codigo:dos_aguas_clara_blanca | pieza por código |
      | 02 | alter_do_chao | alter_do_chao | |
      | 03 | flona_tapajos | flona_tapajos | |
      | 04 | encontro | codigo:dos_aguas_negra_blanca | pieza por código |
      | 05 | manaos | dome | |
      | 06 | anavilhanas | anavilhanas | |
      | 07 | tefe | lago_tefe | asesor obligatorio |
      | 08 | leticia | leticia | |
      | 09 | leticia_atardecer | leticia | `dusk` |
      | 10 | amacayacu | amacayacu | |
      | 11 | puerto_narino | puerto_narino | asesor obligatorio; **alternativa de 07 §2.1: solo la boca, sin el pueblo; la canónica, con el pueblo lejano, la decide el asesor** |
      | 12 | tarapoto | tarapoto | |
      | 13 | rio_frontera | beacon | pieza del kit |
      | 14 | iquitos | iquitos | |
      | 15 | itaya | rafts | asesor obligatorio; reserva `casa_de_fierro` en `02f_board_stop_15_reserva` |
      | 16 | allpahuayo_mishana | allpahuayo | |
      | 17 | nauta | orilla_de_nauta | pieza del kit |
      | 18 | pacaya_samiria | pacaya_samiria | |
      | 19 | pacaya_alba | pacaya_samiria | `dawn` |
  - `arrival` cuando empieza la franja de llegada en la victoria del `10k` (02 §6): `level`, `stop`, `id`, `fact_id` (la clave del dato de llegada, o null si la franja lleva solo la placa del lugar, como en los hitos en los que habla TiTino; D-022) y `welcome` (true con la bienvenida al país, en el 80 y el 140; D-023, P-M7).
    - M1.6 (`DevProbe.Map.cs`) la escribe con `phase` = `start` cuando empieza la franja (al terminar de subir el panel, a los 250 ms de la victoria, o antes con el toque que la adelanta) y otra vez con `phase` = `complete` cuando termina, con `by` = `time` (pasaron sus 1.200 ms), `tap` (un toque la completó) o `close` (la victoria se cerró antes con Siguiente, Inicio o el avance tras la Cosecha doble; entonces puede no haber línea `start`). Campos añadidos: `kind` (`arrival`, la llegada completa; `place`, solo la placa del lugar; o `deferred`, el dato desplazado del `10k+1`, que lleva `stop` = -1 e `id` vacío), `world`, `reduce_motion`, `shown_dp` (el alto de lo que se ve) y `slot_dp` (el de la ranura). `fact_id` también es la clave del dato desplazado.
  - `stop_arrive`, con los parámetros de `af_stop_arrive` (08 §8.2): `{"event":"stop_arrive","stop_id":"s02","world":2,"level":20,"view":"victory","skipped":false}`. `stop_id` es el id de parada `s{nn}` (glosario §11) y `view`, `victory` o `map` (al ponerse al día en el Mapa).
    - M1.6 la escribe una vez por llegada (no en el dato desplazado), al terminar la franja: `skipped` = true si un toque la completó o la victoria se cerró antes. Añade `stop` (la k de la parada) detrás de `stop_id`: `{"event":"stop_arrive","t":41250,"stop_id":"s02","stop":2,"world":2,"level":20,"view":"victory","skipped":false}`.
  - `river_fact_shown`, con los parámetros de `af_river_fact_shown` (08 §8.2), desde un solo punto, al salir de la victoria con Siguiente o Inicio: `{"event":"river_fact_shown","level":20,"fact_id":"map.stop.alter_do_chao.dato_01","dismissed_ms":4210}`. El `fact_id` es la clave del dato de llegada o, en la línea de fruta, `<tinte>.<kk>` (`acai.03`); `dismissed_ms`, el tiempo hasta el toque en Siguiente o, si se sale con Inicio, hasta el toque en Inicio. 08 §8.2 define el `dismissed_ms` del evento solo hasta «Siguiente»: la sonda mide también la salida con Inicio, y la tarea que dé de alta el evento real alinea 08 §8.2 o lo pregunta al dueño.
    - M1.6 la escribe desde el punto común de la ranura del Dato del río (`GameScreen.Victory.cs`, `SendRiverFactShown`), que comparte F3 cuando añada la línea de fruta: una vez por victoria, al cerrarla con Siguiente (también con el avance tras la Cosecha doble, que pasa por AD_GATE) o con Inicio, y solo si la ranura enseñó un dato: el de llegada o el desplazado del `10k+1` (sin línea cuando la franja lleva solo la placa del lugar). Lleva `t`, y `dismissed_ms` cuenta, en ms enteros, desde que se abre la pantalla de victoria (la línea `AF_STATE victory`, cuando empieza a subir el panel) hasta el toque que la cierra: `{"event":"river_fact_shown","t":45460,"level":20,"fact_id":"map.stop.alter_do_chao.dato_01","dismissed_ms":4210}`.
    - F3 (`GameScreen.Victory.cs`) la escribe también para la línea de fruta, con `fact_id` = `<tinte>.<kk>`: `{"event":"river_fact_shown","t":9120,"level":3,"fact_id":"acai.03","dismissed_ms":4200}`. En la línea de fruta, `dismissed_ms` se mide desde que entra la línea (con «Siguiente», a los 600 ms de la pantalla de victoria: la fila 1,40 de 07 §5.4) hasta el toque, y es 0 si se cierra antes (propuesta de Frutas §2.10: «tiempo entre que aparece la línea y el toque en Siguiente»; umbrales de lectura ≥ 4.000 ms y de alarma < 2.000 ms). El dato de llegada, que se ve desde la subida del panel, lo mide desde la pantalla de victoria. Pendiente: el dato desplazado del `10k+1` (M1.6), que también entra con «Siguiente», lo mide todavía desde la pantalla de victoria (600 ms de más); la tarea que dé de alta el evento real lo alinea. Como el árbitro da una sola línea por victoria (T6-A), nunca salen las dos en la misma victoria.
  - `fruit_line` (F3, `DevProbe.Fruit.cs`) cuando sale la línea de fruta en una victoria (al abrirse la pantalla de victoria; entra con «Siguiente», a los 1,40 s): `level`, `fact_id` (`<tinte>.<kk>`), `key` (`fact.<tinte>.<kk>`), `k` y `n` (la línea de la serie y sus líneas), `sealed` (la última de la serie: el contador pasa a sello), `wrapped` (el contador bajó a una segunda fila de la cabecera), `reduce_motion`, `text_dp` (el tamaño del texto con la escala), `shown_dp` (el alto de lo que se ve) y `slot_dp` (el de la ranura): `{"event":"fruit_line","t":7801,"level":1,"fact_id":"acai.01","key":"fact.acai.01","k":1,"n":20,"sealed":false,"wrapped":false,"reduce_motion":false,"text_dp":14.0,"shown_dp":74,"slot_dp":139}`. No sale en los `10k` con dato de llegada, en el 10 con el Mapa ni en un idioma sin la línea traducida. La línea no recibe toques hasta F4 (la ficha): no añade ningún id a `AF_LAYOUT victory`.
  - `stop_label` cuando sale el rótulo del lugar (D-013, punto 8; propuesta del Mapa §10.5, punto 5): `level`, `stop`, `text` y `ms` (lo que dura, por `NoticeTiming.ReadMs`).
    - M2.3 (`GameScreen.Notices.cs`) la escribe al enseñarlo (no al encolarlo), con esos campos y además `stop_id` (`s{nn}`), `id` (el id del sitio), `mode` (`horizon`, sobre el horizonte con 1-2 filas, o `plate`, la placa bajo el HUD con 3) y `rows`: `{"event":"stop_label","t":5425,"level":31,"stop":3,"stop_id":"s03","id":"flona_tapajos","text":"Floresta Nacional do Tapajós · Brasil","ms":3035,"mode":"horizon","rows":2}`. Sale en 31, 51, 71, 91, 101, 121, 131, 151, 161, 171, 181 y 191, solo en el primer intento y nunca al reanudar.
  - `AF_LAYOUT`: la línea `victory` suma el id `arrival`, con el rectángulo de la franja de llegada, solo en las victorias que la llevan.
    - M1.6 escribe además una línea propia, `"screen":"arrival"`, con el id `arrival` (toda la ranura: un toque completa la franja), cuando empieza la franja.
  - `AF_LAYOUT` del inicio (prevista; M2.1, 06 §4.7): la línea `lobby` suma, tras sus botones (`play`, `canoes` si se ve y `settings`), los ids `stop_remaining` (el rectángulo del texto «Faltan N niveles para…», `map_stop_remaining`) y `home_window` (el marco de la ventanita de casa, con su «Casa · a unos {km} km», `map_home_distance`), cada uno solo cuando se ve: ninguno de los dos con Sin canoas, la hoja de la tienda o una oferta encima, y `stop_remaining` tampoco con el fin de contenido ni en un nivel sin parada. Tienen el formato de los demás (`{"id":"home_window","x":…,"y":…,"w":…,"h":…}`), pero no se tocan (`raycastTarget` = false): sirven para comprobar con `adb` que no tapan Ajustes, los saldos ni Jugar.

## d) Build de desarrollo (build-release)

`Assets/_Project/Editor/CI/` es de build-release, así que esta parte no puede ir a los otros tres agentes.
- `ProjectSetup`: `ProductName = "Tintalú (dev)"` y orientación vertical sin autorrotación. `Validate()` comprueba ambas cosas, así que `BuildSmoke` sigue funcionando.
- `BuildDev.BuildAndroidDev` construye `Builds/Android/dev.apk` como Development Build con las escenas de Build Settings. Códigos de salida: 0 OK · 2 ajustes · 3 sin Android · 4 build fallido · 5 falta `Game.unity`.

## e) Aceptación y comandos

1. `dotnet build -warnaserror` y `dotnet test` en verde (39 + nuevos); `validate_schema.py` sigue en 23/23.
2. Generar el pack dos veces da el mismo SHA-256; el informe tiene los 10 niveles en verde y al menos un camino de bloqueo.
3. `unity-compile` devuelve 0; EditMode y PlayMode en verde; `BuildDev` devuelve 0; `check-16kb.sh` OK.
4. En el teléfono la etiqueta es "Tintalú (dev)" y `sw < sh`.
5. Jugando con `input tap` según el informe, los niveles 1-10 terminan en `win` con `pours` igual al par del pack (en `run`: 3, 6, 7, 8, 10, 10, 11, 13, 14 y 16), `load` con `"pour_mode":"run"`, y "Siguiente" abre el nivel siguiente (D-005).
6. Un toque ilegal da `reject` sin cambiar `pours`. Un toque en la intro da `ready` en menos de 50 ms sin seleccionar. Sin tocar, de `load` a `ready` pasan ≤ 1,5 s.
7. Siguiendo el camino de bloqueo llega `stuck`, y `AF_LAYOUT stuck` aparece ≥ 400 ms después (R15). `undo_modal` vuelve a `playing` y `restart` deja las cuias como al inicio.
8. Si se gana el 3 y se hace `force-stop`, al relanzar se abre el nivel 4.
9. Con `--es af_rules_pour_mode single`, `load` trae `"pour_mode":"single"` y en el nivel 2 el vertido 0→3 mueve 1 dosis (`move[2] == 1`); sin el extra, 2 (`move[2] == 2`; criterio A8 invertido por D-005).
10. Al seleccionar un origen ninguna otra cuia cambia: sin aros ni resaltados en los destinos (D-002, criterios B1-B3).

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
