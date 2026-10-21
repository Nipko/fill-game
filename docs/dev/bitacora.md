# Bitácora del río «1 de 3» y cola de takeovers (B4 de la tanda 5)

Nota del bloque B4 del Spark (rama `spark/t5-b4`, desde `origin/integracion-fase1-fase2` en `d994e586`). Recupera y termina el trabajo a medias que quedó sin commitear en el worktree `spark-t5-b4`. Fuentes: `docs/dev/plan-tanda5.md` §4 (fila B4) y §5 (Q-M1, Q-M2, Q-M3, Q-M4, Q-M6); D-033 y su añadido del 2026-10-20 («Bitácora "1 de 3"», puntos 1-7, y «Panel único de la victoria», puntos 8-11); GDD 06 §2.3, §4.2 y §4.7; 05 §2.3; 08 §2.2, §4.3, §8.2 y §9; glosario §10.

Lo marcado **[P]** es una propuesta del Spark con la opción recomendada de la pregunta de la sección 8; el dueño puede cambiarla. Ninguna cifra es nueva: salen de D-033 (tabla), de 06 §4.2 y de 08 §9.

## 1. Qué hay y dónde

**En la rama (archivos nuevos):**

| Archivo | Qué hace |
|---|---|
| `Scripts/LiveOps/LoginCalendarTable.cs` | La tabla «1 de 3» de D-033 (30 casillas × 3 opciones, el canon retocado y lo fijo de los hitos), su formato JSON `eco_calendar_table_v2` [P], `Parse`/`Validate` y la tasa de valor (`eco_calendar_locked_booster_seeds`). |
| `Scripts/LiveOps/LoginCalendarRules.cs` | Reglas sin estado: estado del día, día jugado, avance secuencial sin reinicio y ciclos, `days_missed_before`, sustitución de comodines bloqueados (punto 3) e ids de `af_calendar_claim`. |
| `Scripts/LiveOps/LoginCalendarRecord.cs` | El registro (`calendar` + `played_date` [P] + marcos) y `ILoginCalendarStore` con su versión en memoria. |
| `Scripts/LiveOps/LoginCalendarService.cs` | El servicio: `MarkPlayed`, `Peek`, `Claim` (guarda el avance y después paga con `Wallet.Apply`), Canastas de los días 7, 21 y 30 y el marco. |
| `Scripts/LiveOps/LoginCalendarConfig.cs` | `biz_feature_login_calendar`, `eco_calendar_locked_booster_seeds` (08 §9) y `eco_calendar_table_v2` [P] (D-033). |
| `Scripts/LiveOps/LoginCalendarProbeFormat.cs` | Líneas `AF_STATE` `calendar_claim`, `calendar_state` [P] y `takeover` [P]. |
| `Scripts/GameFlow/TakeoverQueue.cs` | La cola de takeovers (06 §4.7; `ui_max_takeovers_per_session`; sesiones de Q-M4). |
| `Scripts/GameFlow/LevelFlow.Calendar.cs` | El aviso `LevelPlayed` (nivel terminado: victoria o fallo duro, campaña o Reto). Sin llamar hasta B4-2-flow. |
| `Scripts/UI/LogbookScreen.cs`, `LogbookCard.cs`, `LogbookText.cs` | La libreta 5 × 6, la tarjeta con punto del inicio y sus claves de texto. |
| `Scripts/UI/GameBootstrap.Logbook.cs`, `LogbookRunner.cs`, `DevProbe.Logbook.cs` | El arranque (`SetUpLogbook`), el componente que lo avanza y las sondas. Sin llamar hasta B4-3-bootstrap. |
| `Scripts/LiveOps/Tests/LoginCalendar*Tests.cs`, `Tests/EditMode/TakeoverQueueTests.cs` | Tests EditMode (las cifras de D-033, las claves contra 08 §9). |
| `Tests/PlayMode/LogbookScreenTests.cs`, `LogbookBootstrapTests.cs` | Tests PlayMode de la libreta, la tarjeta y el arranque. |
| `docs/dev/traspaso/spark-enganches/B4-strings.csv` | Los 20 textos nuevos (sección 7). |
| `tools/econ_sim.py`, `tools/econ_variants.py`, `tools/out/econ_B.md`, `tools/out/econ_variants.md` | La Bitácora «1 de 3» en la simulación (sección 6). |

**En parches** (`docs/dev/traspaso/spark-enganches/`, LF, probados; orden de la sección 7): `B4-1-profile.diff`, `B4-2-flow.diff` y `B4-3-bootstrap.diff`.

## 2. Reglas (06 §4.2 con D-033, puntos 1-7)

- **30 casillas**, una por día local, **secuenciales y sin reinicio**: si el jugador falta, la siguiente casilla espera; al sellar la 30 empieza otro ciclo (`cycle` + 1).
- **Condición** (punto 5): haber terminado hoy un nivel, de campaña o del Reto, ganado o con fallo duro. El fallo blando (04 §5.2) y salir sin terminar no cuentan. El día jugado se guarda en `played_date` [P] (Q-B1), para que la casilla siga lista si se cierra la app.
- **Desbloqueo**: con el Reto (`live_unlock_level_daily` = 10, de `DailyChallengeConfig`) y su interruptor `biz_feature_login_calendar`. Antes, la tarjeta no existe (D-008).
- **Una página por día**: con `last_claim_date` = hoy, o en el futuro (reloj atrasado), no se sella otra.
- **Elección** (punto 4): la casilla de hoy pulsa; al tocarla salen 3 tarjetas (semillas / comodín / mezcla); un toque sella, sin confirmación [P]. Cerrar sin elegir la deja pendiente; nunca se elige sola.
- **Comodín bloqueado** (punto 3): el siguiente desbloqueado del mismo valor. Morpho → Cuia extra → «2 Marea + 40»; Luciérnaga → Morpho → Cuia extra → «2 Marea + 40» [P] (Q-B5). Si la opción sustituida queda igual que otra (una sin nada bloqueado o una anterior), da otro paso de su cadena: nunca dos opciones iguales. Solo con la Cuia extra bloqueada podría acabarse la cadena, y con los defaults no ocurre (se desbloquea en el 8 y la Bitácora en el 10). Lo desbloqueado se mide con el nivel que el jugador va a jugar (`max_level` + 1 [P], como el nivel efectivo del Reto); la Luciérnaga, desde `lvl_murky_from_level` = 100.
- **Lo fijo de los hitos** (punto 2): días 7 y 21, una Canasta = `eco_basket_seeds` + el comodín de la próxima Canasta de nivel (la regla del hito 6 de la Crecida, `BasketRules.HitSix`) [P] (Q-B7); día 30, la Canasta grande (150 + 1 Cuia extra + 1 Morpho + 3 Marea) y el marco de país del mundo en que está el jugador (`frame_wNN` [P], Q-B6), que se guarda en `entitlements.frames` y se enseña en la 1.1 (Q-M6).
- **Cobro** (punto 6): primero se guarda el avance de `next_day` (y el marco); después se paga con `Wallet.Apply` (semillas con `source = calendar`). En el juego el almacén y la cartera son el mismo perfil y el arranque lo envuelve en un lote síncrono: una sola escritura.
- **Tasa de valor** (punto 1): `eco_calendar_locked_booster_seeds` = `{"undo3":30,"other":100}` leída como «Marea = 30; cualquier otro comodín = 100» **por unidad** [P] (Q-B3). Es la única lectura con la que las tres opciones de la tabla de D-033 valen lo mismo.

## 3. Pantallas

- **Libreta** (`LogbookScreen`, orden 32): 30 casillas en 5 filas × 6, las de hito 3 dp más grandes por lado [P]; las pasadas, solo con el sello (la elección no se guarda, punto 6); la de hoy pulsa (900 ms, ±8 % [P]); pie «Página {k} de 30 · Ciclo {c}». Al sellar, el sello cae como en el calendario del Reto y debajo sale lo recibido.
- **Tarjeta del inicio** (`LogbookCard`): título, una línea de estado y el punto con la casilla de hoy lista (06 §4.2 «Pantalla»). En un lienzo propio, justo encima de la tarjeta del Reto [P] (Q-B12). Tocarla abre la libreta.
- **Takeover** (punto 5; 06 §4.7): al entrar al inicio, si la casilla de hoy está lista, la libreta sale sola, **como mucho una vez por sesión** (`ui_max_takeovers_per_session` = 1). Si el jugador ya la vio en la sesión (tarjeta, secuencia del 10 o fila de la victoria), no vuelve a salir sola [P] (Q-B9).
- **Secuencia del nivel 10** (06 §2.3, pantalla 2; punto 11): Victoria → libreta (con «Cada día que juegues, sella una página» y un solo botón «Seguir») → Mapa → Inicio. No gasta el cupo de takeovers. La Canasta del 10 ya no está en esta pantalla: se abre sola en el panel de la victoria (punto 8).
- **Fila de la victoria** «Bitácora · elige tu premio» (punto 5; tercera barra rotativa del punto 10): es del panel de B1b. B4 deja los ganchos `GameBootstrap.LogbookVictoryRowDue` y `OpenLogbookFromVictory()` (Q-B11).

## 4. Cola de takeovers y sesiones

- `TakeoverQueue` aplica el orden de 06 §4.7 (Bitácora pendiente → evento → oferta → liga) **solo al entrar al inicio** (punto 11). En la 1.0 solo existe la Bitácora; los demás tipos se registran igual cuando lleguen.
- Sesión (Q-M4 (a)): arranque en frío o vuelta tras ≥ 30 min en segundo plano. `LogbookRunner.OnApplicationPause` lleva la cuenta. La reconstrucción por idioma (`RebuildUi`) no es una sesión nueva: la cola pasa a la escena nueva, como las sesiones de B6.
- Cuando se integre B6, la cola puede seguir las sesiones de su `SessionTracker` (`BeginSession`) en lugar de su reloj propio (Q-B10).

## 5. Perfil, analítica y Remote Config

- **Perfil** (B4-1-profile; Q-M3 (a) [P]): `calendar` = `{cycle, next_day, last_claim_date, played_date}`; se omite hasta el primer guardado. Un tipo equivocado es corrupción (es economía); una clave que falta toma su default y una desconocida se ignora, así que el bloque antiguo de ejemplo de 08 §4.3 (`month`, `claimed_days`) se lee como un ciclo 1 en la casilla 1. Los marcos, en `entitlements.frames` (ya existe por B14). Sin migración: el esquema 2 no se ha publicado. **Fusión en la nube (B8)**: proponer [P] el registro con más avance (`cycle`, `next_day`) y la unión de los marcos.
- **Analítica** (`af_calendar_claim`, 08 §8.2 con D-033): `day`, `reward_type` (`seeds`, `booster` o `mix` según lo que trae el premio recibido [P], Q-B8), `reward_value` (el valor equivalente de la opción, sin lo fijo [P]), `days_missed_before`, más `choice` y `cycle` de D-033. Hasta `AF.Analytics` (B7) va como `AF_STATE calendar_claim`, con `reward_json` y `extras_json` solo en la sonda.
- **Remote Config**: `eco_calendar_table_v2` [P] es la única clave nueva (D-033); la v1 queda en 08 §9 de respaldo y esta build no la lee (Q-B4). `eco_calendar_double_ad_enabled` sigue con su única fuente en `AF.Ads` (`rv_daily_chest`, 1.1). Ningún alias retirado (`calendar_table_v1`, `logbook_*`, `max_takeovers_per_session`).

## 6. `econ_sim` (D-033, «econ_sim»)

- `CALENDAR` con el canon retocado y las tres opciones; `--calendar-pick canon|seeds|booster|mix` (por defecto `canon`); las Canastas de los días 7 y 21 y la Canasta grande del 30, que faltaban. Las Mareas del cliente se cuentan sueltas: 3 Mareas = una unidad «undo» del inventario del simulador.
- Regenerados: `tools/out/econ_B.md` (la comprobación de `CLAUDE.md` da «idéntico») y `tools/out/econ_variants.md`. Cambian cifras [sim] (05 §2.3 y §3): por ejemplo, saldo final del casual 1.912 → 2.227, corridas del casual con bloqueo sin recursos 74 % → 69 %, fuente `bitacora` del medio 3.577 → 3.974. El integrador actualiza las cifras citadas en el GDD.
- Con cada elección (300 corridas): saldo final del casual 2.227 (canon), 2.877 (siempre semillas), 975 (siempre comodín), 1.234 (siempre mezcla); casual con bloqueo sin recursos 69 %, 52 %, 96 % y 89 %. «Siempre comodín» queda peor para el casual porque el simulador no usa todo lo que recibe: es un dato para Q-B3.
- **Aviso:** `tools/out/econ_A.md` y `econ_B_tramos.md` **ya no se reproducían** con el código de antes de B4 (comprobado con `git show HEAD:tools/econ_sim.py`). B4 no los regenera (Q-B15).

## 7. Parches y orden de aplicación

Aplicar con `git apply` desde la raíz, en este orden:

1. **`B4-1-profile.diff`**: `Profile.cs` (`Profile.Calendar` y `CalendarData`), `ProfileJson.cs` (escritura tras `streak`, solo si existe; lectura), `LiveOps/ProfileLoginCalendarStore.cs`, `LiveOps/Tests/ProfileLoginCalendarStoreTests.cs` y, en `GameBootstrap.Logbook.cs`, `LogbookStoreFactory` = `CreateProfileLogbookStore` (el bloque del perfil por la vía síncrona del lote, como el Reto).
2. **`B4-2-flow.diff`**: dos llamadas en archivos del escritorio: `OnLevelPlayedCalendar(true)` en `LevelFlow.ResolveWin` (tras `OnWinSkins()`, dentro del lote) y `OnFailCalendar(kind)` en `LevelFlow.Fail` (tras `OnHardFailStreak`); y `Tests/EditMode/LevelPlayedHookTests.cs`.
3. **`B4-3-bootstrap.diff`** (después del 1 y el 2): en `GameBootstrap.cs`, `SetUpLogbook();` tras `SetUpStreak();` y la libreta antes del Mapa del 10 (`if (_app.L10MapDue && !OpenLogbookBeforeL10Map())`); en `PlayModeHelpers.cs`, `NoLogbookUnlessSet()` (los tests que no tratan de la Bitácora la apagan, como la secuencia del 10); y `Tests/PlayMode/LogbookHooksTests.cs`.
4. **Textos:** las filas de `B4-strings.csv` a `strings.csv`. Sin ellas, en desarrollo se ve «[clave]».

**Choques conocidos:** `B3-ui.diff`, `B6-bootstrap.diff` y `B7-bootstrap.diff` también añaden una línea tras `SetUpStreak();`. Orden propuesto: `SetUpSkip();` (B3), `SetUpLogbook();` (B4), `SetUpSessions();` (B6), `ConnectTelemetry();` (B7). B6 (`push_logbook_pending`, con `{reward}`) puede leer la casilla de mañana con `LoginCalendarService.Peek`. B8 tiene que cubrir `calendar` en la integridad y la fusión del perfil.

**Lo que el integrador debería anotar en el GDD:**
- 06 §4.2: el rediseño «1 de 3» (la tabla de D-033 con sus opciones, la sustitución y la cadena de la Luciérnaga [P], la condición «nivel terminado», el cobro y el takeover); quitar la nota «se rediseña cuando se planifique».
- 06 §2.3, pantalla 2: solo la libreta (la Canasta, en la victoria; punto 8), con la casilla 1 «1 de 3».
- 05 §2.3 y §3: la tabla retocada y las cifras [sim] nuevas.
- 08 §4.3: `calendar` = `{cycle, next_day, last_claim_date, played_date}` [P], con el ejemplo.
- 08 §8.2: `af_calendar_claim` con `choice` y `cycle` y los valores de `reward_type`.
- 08 §9: la fila `eco_calendar_table_v2` (json; default: la tabla de D-033; dueño 05) y `RemoteConfigDefaults.json` cuando exista.

## 8. Preguntas para el dueño (con la recomendada, aplicada como [P])

| Id | Pregunta | Opciones | Recomendada (aplicada) |
|---|---|---|---|
| Q-B1 | ¿Dónde se guarda «hoy ya terminó un nivel»? D-033, punto 6, dice «solo `calendar` = `{cycle, next_day, last_claim_date}`» | (a) `played_date` dentro de `calendar`; (b) solo en memoria (cerrar la app antes de sellar obliga a jugar otro nivel); (c) un campo en `daily` | (a) |
| Q-B2 | La pantalla 2 del 10 (06 §2.3) habla de «la casilla 1 (50 semillas)» y de la Canasta en la mitad superior | (a) la libreta sola con las 3 tarjetas de la casilla 1 (60 / 2 Marea / 1 Marea + 30), la Canasta en la victoria (puntos 8 y 11); (b) mantener la Canasta en esa pantalla | (a) |
| Q-B3 | Cuánto vale una Marea en la tasa de valor. D-033, punto 1, dice «Marea = 30», que cuadra las opciones (2 Marea = 60), pero 06 §4.2, regla 1, y la clave `undo3` dicen «3 Marea → 30», que cuadra el canon («3 Marea + 30» = 60) y el precio de la tienda (3 deshacer por 30) | (a) 30 por Marea, como D-033 (implementado); (b) 10 por Marea (`undo3`): las opciones de Marea de la tabla pasan a 6, 3, 6 y 15 Mareas (cambia la tabla, por RC) | (a), por ser lo aprobado; el dueño confirma con los datos de la sección 6 |
| Q-B4 | ¿Qué lee la 1.0? | (a) `eco_calendar_table_v2` y, si falta o no vale, la tabla compilada; la v1 no se lee; (b) leer también la v1 y convertirla | (a) |
| Q-B5 | La Luciérnaga bloqueada (D-033, punto 3, solo da la cadena de Morpho) | (a) Morpho → Cuia extra → «2 Marea + 40», desbloqueada desde `lvl_murky_from_level` (100); (b) directamente «2 Marea + 40» | (a) |
| Q-B6 | Id del marco de país | (a) `frame_wNN` por mundo (01-13, `WorldTable`); (b) por país | (a) |
| Q-B7 | Contenido de la Canasta de los días 7 y 21 | (a) `eco_basket_seeds` + el comodín de la próxima Canasta de nivel (regla del hito 6 de la Crecida); (b) solo semillas; (c) la Canasta de nivel de su ranura del ciclo | (a) |
| Q-B8 | `reward_type` de `af_calendar_claim` | (a) `seeds`, `booster` o `mix` según lo que trae el premio recibido (con la sustitución); `choice` dice la tarjeta tocada; `reward_value` sin lo fijo; (b) `reward_type` = la tarjeta | (a) |
| Q-B9 | Si el jugador ya abrió la libreta en la sesión (tarjeta, secuencia del 10, victoria), ¿sale sola al volver al inicio? | (a) no; (b) sí, si la casilla sigue pendiente | (a) |
| Q-B10 | Sesiones de la cola | (a) reloj propio de 30 min hasta integrar B6 y después las de su `SessionTracker`; (b) esperar a B6 | (a) |
| Q-B11 | La fila «Bitácora · elige tu premio» del panel de la victoria | (a) la dibuja el escritorio en `VictoryBars` (tercera barra rotativa) con los ganchos de B4; (b) otro parche de B4 | (a) |
| Q-B12 | Dónde va la tarjeta de la Bitácora en el inicio y sus textos de estado | (a) encima de la del Reto, con «Juega un nivel para sellar» / «Sella la página de hoy» / «Sellada hoy»; (b) como pestaña de la barra inferior | (a) |
| Q-B13 | «Duplicar con video» (1.1) | (a) fuera de la 1.0 (`eco_calendar_double_ad_enabled` = false) | (a) |
| Q-B14 | `econ_sim` da la Bitácora desde el día 1, sin el desbloqueo del 10 | (a) dejarlo así (era así antes de B4); (b) modelar el desbloqueo | (a) |
| Q-B15 | `econ_A.md` y `econ_B_tramos.md` ya no se reproducían antes de B4 | (a) regenerarlos aparte, con el visto bueno del dueño; (b) dejarlos | (a) |

## 9. Verificación

Ver la entrada de B4 en `docs/dev/traspaso/spark-estado.md`.
