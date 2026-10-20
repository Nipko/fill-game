# Crecida, Hoja escudo y Canasta: nota de diseño (B1a)

Nota del bloque B1a de la tanda 5 (`docs/dev/plan-tanda5.md` de `spark/auditoria-1-0`, §4; encargo §11 S2 de
`docs/dev/traspaso/spark-reto-del-rio.md`). Reúne lo que el GDD ya fija, dónde va en el cliente y lo que queda por decidir.
**No sustituye al GDD:** ante una contradicción mandan 06 §4.3-§4.4, 05 §2.1 y §2.4, 04 §5.2, §6.2, §8 y §10.1, 08 §4.3,
§8.2 y §9 y GDD.md §6.1 #13-#14. Lo marcado [P] es propuesta de esta nota y lo decide el dueño (por defecto, la recomendada).

Rama `spark/t5-b1a` (sale de `origin/integracion-fase1-fase2` `a759597`). Tareas: A0 (esta nota), A1 (configuración), A2
(Canasta), A3 (Crecida y Hoja escudo). A4 (el perfil: `Profile.cs`, `ProfileJson.cs`, `DailyCounters.cs`) va en otra rama;
aquí solo se describe su forma (§3).

## 1. Reglas (con su fuente)

### 1.1 Crecida (`win_streak`) y Hoja escudo (`streak_shield`)

| # | Regla | Fuente |
|---|---|---|
| C1 | Solo cuentan los niveles de **campaña** desde `streak_unlock_level` (25). El Reto del río y los eventos no la tocan. | 06 §4.3 (pseudocódigo, `if not nivel.es_campana: return`); 05 §2.4 (`if level < 25: return`) |
| C2 | Victoria: +1 a la barra (`wins`). | 06 §4.3; 05 §2.4 |
| C3 | Hito 3 → 300 semillas (`streak_milestones["3"]`, `source = streak`). | 05 §2.1 y §2.4; 08 §9 |
| C4 | Hito 6 → Canasta: 50 semillas (`eco_basket_seeds`) + 1 comodín del ciclo de 06 §4.4, visible y sin azar. | 06 §4.3 (fila «Hitos»); 05 §2.1 |
| C5 | Hito 10 → 1 Cuia extra al stock + **río crecido**; la barra vuelve a 0. Si la barra vuelve a 10 el mismo día, se vacía otra vez sin otra Cuia extra y el río crecido sigue (o vuelve a quedar) activo. | 05 §2.4; 06 §4.3; 04 §8 |
| C6 | Cada hito se paga **una vez por día local** (`streak_hits_once_per_day` = true). | 06 §4.3; 05 §2.4; 08 §9 |
| C7 | Tope diario de 600 semillas (`streak_daily_cap`) que suma **solo** las 300 del hito 3 y las 50 de la Canasta del hito 6. No cuentan: el x2 del río crecido, el comodín del hito 6 ni la Cuia extra del hito 10. Pago parcial hasta llenar el tope (`pay_streak_hit` de `tools/econ_sim.py`). | 06 §4.3 («Tope diario» [P]); 05 §2.4; 08 §9 |
| C8 | Río crecido: Cosecha doble **sin anuncio** en cada victoria de campaña hasta el siguiente fallo duro no escudado, también con la barra a 0. Se decide con el estado **anterior** a la victoria: la que paga el hito 10 no la lleva (en `econ_sim.on_win` el x2 va antes de sumar la racha). | 04 §2.8; 05 §2.1 (fila Cosecha doble); 05 §2.4; `econ_sim.py` `on_win` |
| C9 | Fallo duro (04 §5.2, con D-008) con `wins` ≥ 1 y Hoja escudo disponible: se gasta la hoja y nada más (la barra y el río no cambian). | 06 §4.3; 05 §2.4 `on_hard_fail`; 04 §6.2 `failHard` |
| C10 | Fallo duro sin escudo, o con la barra a 0: `wins` = 0 y río crecido apagado. Con la barra a 0 y el río crecido, el fallo apaga el río **sin gastar** la hoja. | 05 §2.4; 04 §6.2 y §8; `econ_sim.py` `on_fail` |
| C11 | No tocan la Crecida: fallo blando (reinicio con ≥ 8 vertidos), reinicio con < 8, Marea, Cuia de rescate (la conserva), cerrar la app, crash, Salto del Boto (ni la rompe ni la avanza), salir con el menú en 1-30, abrir el Mapa desde la pausa. `streak_restart_counts_as_fail` = false, fijado por la definición de fallo. | 04 §5.2; 06 §4.3; 06, pregunta abierta 1 |
| C12 | Cambio de día local: hoja escudo otra vez disponible (1 al día, `streak_shield_per_day`), hitos pagados y semillas del día a 0. **La barra y el río crecido no expiran por reloj.** El día solo avanza: con el reloj atrasado no se repone nada (como `DailyCounters.RollOver`). | 06 §4.3 `on_app_open`; 05 §2.4 `reset_daily`; 04 §8 y §10.1 |
| C13 | Kill switch `biz_feature_win_streak` (true): apagado, la Crecida no suma ni rompe y no hay x2 del río (Q-C11). | 08 §9 (fila `biz_feature_<sistema>`) |
| C14 | `streak_reminder_hours` (36) solo alimenta el push local «Crecida te espera»: se lee, no cambia la racha. | 06 §8.3; 08 §9 |

### 1.2 Canasta (`chest_basket`)

| # | Regla | Fuente |
|---|---|---|
| K1 | Cada 10 niveles de **campaña** (10, 20, 30…), desde `live_unlock_level_daily` (10), al **superar** el nivel `10k`: ganándolo o saltándolo con el Salto del Boto (K9). | 06 §4.4; 05 §2.1; 08 §9 (`live_unlock_level_daily`: «Desbloquea … Canasta») |
| K2 | Nivel 10: solo 50 semillas. | 06 §4.4; 05 §2.1 |
| K3 | 20, 30, 40…: 50 semillas (`eco_basket_seeds`) + 1 comodín de `eco_basket_cycle` = `["undo3","hint","extra_cuia","undo3","hint"]`, con índice `(L/10 − 2) mod 5`: 20 → 3 Marea, 30 → Morpho, 40 → Cuia extra, 50 → 3 Marea, 60 → Morpho, y se repite. Así salen Cuia extra en 40 y 90 de cada 100 y Morpho en 30, 60, 80, 110… | 06 §4.4; 04 §3 (columna «Gratis»); 08 §9 |
| K4 | `undo3` = 3 Mareas al stock (`stock.undo` += 3). | 06 §4.4 («3 Marea»); 04 §2.1 |
| K5 | Cada 100 niveles (100, 200…): **Canasta grande** = 150 semillas + 1 Cuia extra + 1 Morpho + 3 Marea. Sustituye a la normal de ese nivel (Q-C4). | GDD.md §6.1 #13 (decidida el 2026-10-06); 06 §4.4 |
| K6 | Contenido determinista por nivel y visible antes de abrir; nunca comprable ni abrible con semillas; sin probabilidades. | 06 §4.4; 08 §9 (`eco_basket_cycle`: IARC «ítems aleatorios: no») |
| K7 | Pago con `Wallet.Apply`: `Transaction.Earn(SeedSource.Basket, n)` + `Transaction.GrantStock(…)`. | 08 §2.2 (`AF.Economy`: toda mutación por `Wallet.Apply`) |
| K8 | Sin estado propio: la Canasta de nivel se deduce de `max_level` y `skipped_levels`. La del nivel `10k` se paga al superarlo: en su victoria (E30) o en su Salto del Boto (K9, E34). El Mapa del río (06 §4.5: «las cobradas llevan una marca») deduce la marca de cobrada igual, con `max_level` y `skipped_levels`, sin campo nuevo. | 06 §4.5 («sin estado nuevo salvo un campo»); Q-M9 (a) |
| K9 | Un nivel `10k` saltado con el Salto del Boto **también paga su Canasta**: el salto marca el nivel como superado «sin recompensa (0 semillas, sin avance ni rotura de la Crecida, cuenta para la Canasta de cada 10 niveles)». Se paga el mismo `BasketRules.For(10k, live_unlock_level_daily)` (la función es pura: no distingue victoria de salto), con `source = basket`. El salto no toca la Crecida (C11), así que no hay hito 6 por saltar. Enganche E34 (B3). | 04 §2.6 («Función exacta»); 04 §5.2 (fila Salto del Boto) |

**Ojo con `tools/econ_sim.py`:** su ciclo de la Canasta de nivel usa `BASKET_CYCLE[(k − 1) % 5]` con `k = L/10`, que da Marea en
el 10 y Morpho en el 20: va **un hueco por detrás** de 06 §4.4 y 04 §3 (y da comodín en el 10). No es un hallazgo nuevo: 05 §3.4
ya lo recoge entre lo que la simulación no modela («entrega ya en la Canasta del nivel 10 el primer comodín del ciclo»). El
cliente sigue al GDD. La simulación tampoco modela la Canasta grande, y 05 §3.4 pide volver a correr el script cuando se apruebe:
ya está decidida (GDD.md §6.1 #13). Esa corrida queda **pendiente** (no se hace aquí: no se toca `tools/`). Además, la simulación
da el comodín del hito 6 con un contador propio (`streak_chests`), y el cliente lo deduce de `max_level` (Q-C3). Tampoco se copia de la simulación que la hoja escudo del día se dé solo
si el nivel ya era ≥ 25 al empezar el día (`start_day`): el GDD la repone en cada cambio de día (C12).

## 2. Módulos (Q-M10 (a): todo en `AF.Economy`)

08 §2.2 nombra la Crecida en `AF.Economy` («Crecida y Hoja escudo») y en `AF.LiveOps` («Crecida»). Con la recomendada de Q-M10,
todo va en **`AF.Economy`**, junto a la Canasta, porque sus hitos pagan con `Wallet.Apply` y `AF.Economy` no puede depender de
`AF.LiveOps` (el grafo va al revés). Se quita «Crecida» de la fila de `AF.LiveOps` (hallazgo para el integrador, plan §6).

| Archivo (nuevo) | Contenido |
|---|---|
| `Assets/_Project/Scripts/Economy/StreakConfig.cs` | Claves `streak_*` y `biz_feature_win_streak` con `[RemoteConfigKey]` y los defaults de 08 §9; `StreakMilestones` (`streak_milestones`). |
| `Assets/_Project/Scripts/Economy/BasketConfig.cs` | `eco_basket_cycle` y `eco_basket_seeds`. |
| `Assets/_Project/Scripts/Economy/BasketRules.cs` | `BasketRules.For`, `HitSix`, el contenido de la Canasta grande (constante [P], Q-C12) y `BasketContents` (valor). |
| `Assets/_Project/Scripts/Economy/StreakRules.cs` | `StreakOutcome` (valor) y `StreakRules` (`OnVictory`, `OnHardFail`, `RollDay`) sobre el `StreakRecord` de A4 (`StreakRecord.cs`, `IStreakStore.cs`, `InMemoryStreakStore.cs`). |
| `Assets/_Project/Scripts/Economy/StreakService.cs` (A5) | `StreakService` (`ApplyWin`, `ApplyHardFail`, `ApplySkip`, `Peek`): une las reglas, el `IStreakStore` (`Load` al empezar, `Save` antes de pagar), el reloj y `Wallet.Apply`; devuelve `StreakServiceResult` (lo pagado y los eventos `StreakServiceEvents`) y `StreakSnapshot` (HUD y sonda). Tests: `StreakServiceTests.cs` |
| `Assets/_Project/Scripts/Economy/EconomyJsonLite.cs` | Lector mínimo de los dos JSON de RC (un objeto plano y una lista de cadenas): `AF.Economy` no ve `AF.Levels` ni los internos de `AF.Save`. |
| `Assets/_Project/Tests/EditMode/StreakBasketConfigTests.cs`, `BasketRulesTests.cs`, `StreakRulesTests.cs` | Tests (EditMode; el asmdef `AF.Tests.EditMode` ya referencia `AF.Economy`). |

Todo sin motor, sin reloj del sistema y sin azar (`EconomyConfigTests.EconomyAssembly_HasNoEngine…` ya lo vigila en la carpeta).
`OnVictory`, `OnHardFail`, `BasketRules.For` y `HitSix` no asignan memoria (devuelven valores `struct`; la lista de hitos de
`StreakRecord` solo reserva su arreglo la primera vez y `RollOver` la vacía sin soltarlo); las transacciones
(`Transaction` es una clase) se piden aparte, con `AppendTransactions`.

**Quién llama (enganches, §6):** el flujo solo llama a `StreakService`, que hace `RollDay`, la regla, `Save` y los pagos.
La lógica del flujo está en el parcial `Scripts/GameFlow/LevelFlow.Streak.cs` (A6), con el punto de inyección
`LevelFlow.UseStreak(StreakService)`; en los archivos existentes solo hay las llamadas del parche
`spark-enganches/S2-flow.diff`. `LevelFlow.Victory` (`ResolveWin`, tras `Transaction.Win`) llama a `OnWinStreak`, que llama a
`ApplyWin` con el `max_level` leído antes de `SaveWin`; el servicio aplica el x2 gratis (Q-C1), las transacciones del hito y la
Canasta de nivel. `LevelFlow.Funnel.cs` (`Fail`, tras `FailResolve.Resolve`, que ya aplicó `Transaction.HardFail`) llama a
`OnHardFailStreak`, que llama a `ApplyHardFail` solo en el fallo duro, **antes** de `ILevelFlowListener.OnFailResolved`;
`FailResolve` y `FailOutcome` no cambian. La vista lee `LevelFlow.LastStreakResult` en `OnFailResolved` (y en `OnWin`): de
`LastStreakResult.Streak` salen `streak_broken` y `shield_used` de `af_level_fail` (Q-C7) y el toast de la hoja. El Salto del
Boto llama a `LevelFlow.OnSkipStreak`, que llama a `ApplySkip` (E34). Los tests del flujo que necesitan las llamadas van dentro de S2 (`StreakFlowHookTests`); el de
integración con el perfil (`ProfileSaveBatch`, `ProfileStreakStore` y la cartera: una escritura por victoria y por fallo duro)
necesita también S1 y va en `spark-enganches/S5-flow-profile.diff`, que se aplica después de S1 y S2. El servicio no es reentrante: un oyente de `Wallet.Changed` no debe llamarlo.

## 3. Perfil (Q-M9 (a); lo implementa A4)

- `streak` = `{ "wins": int, "river_full": bool }`, **nullable y omitido** en el JSON hasta su primera escritura (la primera
  victoria de campaña desde el 25); sin migración, como `map_seen_level` (08 §4.3: `profile_schema` 1 aún no publicado).
- En `daily` (04 §10.1), que ya se reinicia entero con `date_local`:
  - `streak_hits` [P]: lista de los hitos pagados hoy (p. ej. `[3, 6]`; forma de A4);
  - `streak_seeds` [P]: semillas de la Crecida pagadas hoy (para el tope de 600);
  - un solo `shield_available`, **el de `daily`** (ya existe: `DailyData.ShieldAvailable`; `DailyCounters.RollOver` lo pone en
    true). Se quita `shield_available` del bloque `streak` de 08 §4.3 (hallazgo del plan §6).
- `DailyCounters.RollOver` pone `streak_hits` y `streak_seeds` a 0 (enganche E28).
- La Canasta, **sin bloque**: se deduce de `max_level`.
- `StreakRecord` (A4, `Scripts/Economy/StreakRecord.cs`) junta esas piezas: `Wins` y `RiverFull` (de `streak`),
  `ShieldAvailable`, `HitsToday`, `SeedsToday` y `DateLocal` (de `daily`), con `RollOver` solo hacia delante. `StreakRules`
  trabaja sobre ese mismo objeto (un solo estado). Lo cargan y guardan `IStreakStore` e `InMemoryStreakStore` (A4) y, en el
  juego, el adaptador sobre el perfil del parche `S1-profile.diff`, con el mismo `IProfileStore` que la cartera, en el mismo
  lote, para que el pago y el hito no se separen.

## 4. Remote Config (08 §9; nombres exactos)

| Clave | Tipo | Default | Rango | Uso en el cliente |
|---|---|---|---|---|
| `biz_feature_win_streak` | bool | true | — | Kill switch de la Crecida (Q-C11) |
| `streak_unlock_level` | int | 25 | 20-40 | Debut (C1) |
| `streak_milestones` | json | `{"3":300,"6":"chest","10":"extra_cuia+rio_crecido"}` | hito 3: 200-300; 6 y 10 fijos | Si 6 o 10 no son esos literales, o el 3 sale de 200-300, o el JSON no se lee: el default entero |
| `streak_hits_once_per_day` | bool | true | — | C6 (con false, los hitos se repiten y solo los frena el tope) |
| `streak_daily_cap` | int | 600 | 300-900 | C7 |
| `streak_shield_per_day` | int | 1 | 1-2 | 1.0: una sola hoja al día con un bool, aunque valga 2 (Q-C6) |
| `streak_restart_counts_as_fail` | bool | false | — | Fijado a false por 04 §5.2; `Validate` rechaza true |
| `streak_reminder_hours` | int | 36 | 24-48 | Push local; se lee, no se usa aquí |
| `eco_basket_cycle` | json | `["undo3","hint","extra_cuia","undo3","hint"]` | — | K3; ficha = id del comodín (`undo`, `hint`, `reveal`, `extra_cuia`, `shuffle`) con unidades opcionales (`undo3`) |
| `eco_basket_seeds` | int | 50 | 30-50 | K2-K3 y la Canasta del hito 6 |
| `live_unlock_level_daily` | int | 10 | 8-12 | K1. **No se declara en `AF.Economy`:** su única fuente es `DailyChallengeConfig` (`AF.LiveOps`); `BasketRules.For` lo recibe como argumento |

Alias retirados que no se usan: `eco_streak_rewards`, `streak_daily_cap_seeds`, `streak_shields_per_day`, `streak_hit3_seeds`,
`streak_hit10_seeds`, `unlock_level_streak`, `chest_cycle`, `chest_seeds`, `chest_first_seeds`, `eco_basket_table`.

## 5. Analítica como sonda (08 §8.2; el sumidero real llega con B7)

`streak_fail` y `streak_state` son **solo sondas de desarrollo** (logcat), no eventos de 08 §8.2: si algún día van a analítica,
primero hay que darlos de alta en esa taxonomía única. Hasta que exista `AF.Analytics`, cada evento de la tabla sale como línea `AF_STATE {"event":…}` en builds de desarrollo (formato de
`DevProbe`, tope de 960 bytes). Lo implementa A7: `Scripts/Economy/StreakProbeFormat.cs` (sin motor; tests
`StreakProbeFormatTests`) y `Scripts/UI/DevProbe.Streak.cs`; el formato exacto está al final de esta sección. Qué sale:

| Línea | Parámetros | Cuándo |
|---|---|---|
| `af_streak_milestone` | `milestone ∈ {3, 6, 10}`, `reward_type` (`seeds`, `chest`, `extra_cuia+rio_crecido`), `reward_value` (semillas pagadas, `contents_json` de la Canasta o `1`) | Hito **pagado** (no el que se salta por la regla de una vez al día) |
| `af_streak_shield_used` | `streak_at_use` | Fallo duro cubierto por la hoja |
| `af_basket_open` | `level`, `contents_json` | Canasta de nivel (Q-C13) |
| `earn_virtual_currency` | `source ∈ {streak, basket, double_harvest}`, `value` | Hito 3 y Canasta del hito 6 (`streak`), Canasta de nivel (`basket`), x2 del río crecido (`double_harvest`); lo emite `Wallet.Apply` |
| `streak_fail` [P] | `level`, `wins_before`, `river_full_before`, `shield_used`, `streak_broken`, `river_ended` | Cada fallo duro de campaña desde el 25 (para cruzar con `af_level_fail`) |
| `streak_state` [P] | `cause`, `level`, `wins`, `river_full`, `shield`, `hits`, `seeds_today`, `date`, `enabled`, `active`, `free_double_next` | Al arrancar y tras cada cambio del estado (victoria, fallo duro, cambio de día) |

`af_level_fail` (08 §8.2) toma `streak_broken` y `shield_used` de `StreakOutcome` (Q-C7). `contents_json` [P] es estable:
`{"kind":"normal","seeds":50,"undo":3}`, con `kind` ∈ {`normal`, `grand`, `streak`}, `seeds` siempre y después solo los
comodines con unidades, en el orden `undo`, `hint`, `reveal`, `extra_cuia`, `shuffle`.

**Formato de la sonda (A7).** Todas son `AF_STATE {"event":…,"t":…,…}` en una línea, con los campos en este orden y solo en
builds de desarrollo. Los nombres de las líneas reflejan los eventos de 08 §8.2 sin el prefijo `af_` (como `daily_challenge` en
el Reto): `streak_milestone`, `streak_shield_used` y `basket_open`. `streak_fail`, `streak_state` y `free_double_harvest` son
solo de la sonda [P]. `reward_value` y `contents_json` salen como cadenas JSON (con las comillas escapadas), igual que los
parámetros de analítica.
- `streak_state`: `cause` (`start` al crear el servicio, `day` al cambiar el día local, `win` o `hard_fail` tras un evento que
  contó), `level` (el nivel consultado con `StreakService.Peek`: el siguiente de la campaña, `max_level` + 1), `wins`,
  `river_full`, `shield`, `hits` (lista creciente de los hitos pagados hoy), `seeds_today`, `date` (`yyyy-MM-dd` o null),
  `enabled` (`biz_feature_win_streak`), `active` (hay barra en ese nivel) y `free_double_next` (la siguiente victoria lleva el x2
  del río).
- `streak_fail`: `level`, `wins_before`, `river_full_before`, `shield_used`, `streak_broken`, `river_ended`.
- `streak_shield_used`: `level` [P] (solo de la sonda: no está en 08 §8.2), `streak_at_use` (la barra que salvó la hoja).
- `free_double_harvest`: `level`, `seeds` (las que entraron por la Cosecha doble del río crecido).
- `streak_milestone`: `level` [P] (solo de la sonda: no está en 08 §8.2), `milestone`, `reward_type`, `reward_value` (solo con el hito pagado).
- `basket_open`: `level`, `contents_json` (Canasta de nivel, en la victoria o en el Salto del Boto).

Un resultado del servicio sale siempre en el orden `streak_fail`, `streak_shield_used`, `free_double_harvest`,
`streak_milestone`, `basket_open` y, si cambió el estado, `streak_state`. La sonda no toca `GameScreen`: `StreakProbeRunner`
(un componente que añade `SetUpStreak`, solo en desarrollo) mira cada fotograma si `LevelFlow.LastStreakResult` es otro objeto
y, si lo es, escribe sus líneas en el primer fotograma después de producirse. En la victoria salen después de `AF_STATE win`
(de `ResolveWin`) y **antes** de `victory`, que la vista escribe tras el drenado y la celebración (hasta unos 4 s después); en el
fallo duro, después de `fail_hard` y de `level_failed`. `earn_virtual_currency` de la Crecida y la Canasta no sale aquí: la tabla lo deja a `Wallet.Apply`, con el sumidero
de analítica (B7).

## 6. Enganches previstos (E26+; parches probados en `docs/dev/traspaso/spark-enganches/` en la ola 3)

| Id | Archivo | Qué |
|---|---|---|
| E26 | `Scripts/Save/Profile.cs` (A4) | `StreakData { Wins, RiverFull }` nullable en `Profile.Streak`; `DailyData.StreakHits` y `StreakSeeds` |
| E27 | `Scripts/Save/ProfileJson.cs` (A4) | Leer y escribir `streak` (omitido si es null) y `daily.streak_hits` / `streak_seeds` |
| E28 | `Scripts/Save/DailyCounters.cs` (A4) | `RollOver` pone `StreakHits` y `StreakSeeds` a 0 |
| E29 | `Scripts/GameFlow/LevelFlow.Funnel.cs` (parche `S2-flow.diff`; lógica en `LevelFlow.Streak.cs`) | En `Fail()`, tras `FailResolve.Resolve` (que ya aplicó `Transaction.HardFail`) y antes de `_listener.OnFailResolved`: `OnHardFailStreak(kind)`, que solo en el fallo duro llama a `StreakService.ApplyHardFail(LevelNumber, !IsDaily)` (el servicio hace `Load`, `RollDay`, `OnHardFail` y `Save`) y deja el resultado en `LevelFlow.LastStreakResult`. Cubre los dos caminos del fallo duro: el pie del modal en 1-30 (`Restart`) y `FailToLevelFailed` desde el 31 (`ConfirmGiveUp`, `PressGiveUp` sin canoa en juego, `ConfirmQuit`). `FailResolve.cs` y `FailOutcome` **no cambian**: `af_level_fail` (Q-C7) lee `streak_broken` y `shield_used` de `LastStreakResult.Streak` en `OnFailResolved`. Absorbe E19 (`campaign: !IsDaily`) |
| E30 | `Scripts/GameFlow/LevelFlow.Victory.cs` | En `ResolveWin`: `maxLevelBefore` (`max_level`) se lee **antes** de `_progress.SaveWin(LevelNumber, LevelNumber + 1)` (`LevelFlow.Victory.cs:141` hoy), porque `SaveWin` lo sube; tras `Transaction.Win`, `StreakService.ApplyWin(level, !IsDaily, maxLevelBefore, _harvestTier)`. El servicio paga dentro el x2 gratis del río, las transacciones del hito y la Canasta de nivel (`BasketRules.For(level, live_unlock_level_daily)` si `level > maxLevelBefore`). Con el x2 del río aplicado (`StreakServiceEvents.FreeDoubleHarvest`) y `_harvestBase` > 0, `_harvestDoubled` = true y el botón x2 no se ofrece (Q-C9); `DoubleHarvestSeeds` se suma a `_harvest`. El x2 gratis del río no pasa por la compuerta `ads_rv_double_harvest_from_level` (Q-C14): sin E35, `Wallet.Apply` lo rechazaría con `NotUnlocked` si esa clave subiera por RC por encima del nivel |
| E34 | `Scripts/Boosters/SkipBooster.cs` / `Scripts/GameFlow/LevelFlow.Funnel.cs` (B3) | Al confirmar el Salto del Boto, `LevelFlow.OnSkipStreak()` (ya en `LevelFlow.Streak.cs`), que llama a `StreakService.ApplySkip(level, true)`: paga la Canasta de nivel si es un `10k` de campaña (K9), igual que en la victoria, y no toca la Crecida. Fuera de campaña (Reto) o con el nivel ya superado (`level <= max_level`) devuelve null. **Contrato:** B3 lo llama **dentro del mismo `using (Saves())` en el que guarda el salto y antes de guardarlo**: dentro, para que la Canasta y el progreso salgan en una sola escritura del perfil (`OnSkipStreak` pide `SaveBatch.RequireDurable()`); antes, porque compara con el `max_level` anterior al salto |
| E35 | `Scripts/Economy/Transaction.cs`, `Wallet.cs` | El x2 del río crecido, `Transaction.DoubleHarvestRiver(level, tier)` [P] (`internal`: solo lo pide la Crecida; los tests lo ven por `Scripts/Economy/AssemblyInfo.cs`, `InternalsVisibleTo("AF.Tests.EditMode")`), con `source = double_harvest`, la marca `Transaction.RiverFull` y sin la compuerta de nivel del vídeo (Q-C14). Parche probado `spark-enganches/S4-wallet.diff`: también cambia `StreakOutcome.AppendTransactions` (único punto donde se crea el x2 del río) para usar la variante y quita de `StreakServiceTests` el test del rechazo sin parche. **Se aplica después de fusionar `spark/t5-b1a`**, porque reescribe archivos de la rama (`StreakRules.cs`, `StreakService.cs`, `StreakServiceTests.cs`). Sin el parche, `StreakService` paga el x2 con `Transaction.DoubleHarvest`, que la cartera rechaza solo si RC sube `ads_rv_double_harvest_from_level` por encima del nivel. El nombre `RiverFull` se queda; la Temporada de lluvias (04 §2.8) podría reutilizar la misma marca como una Cosecha doble `Free` sin compuerta [P] |
| E31 | `Scripts/GameFlow/LevelFlow.Funnel.cs` (el mismo punto de E29, parche `S2-flow.diff`) | `Fail()` pasa `isCampaign = !IsDaily` (E19; el Reto no cuenta) y el resultado de la Crecida llega al oyente por `LevelFlow.LastStreakResult`, ya escrito cuando se llama a `OnFailResolved` (toast de la hoja, consuelo de TiTino). El fallo blando no llama a la Crecida: `LastStreakResult` queda en null |
| E32 | `Scripts/UI/GameBootstrap.cs` (parche `S3-bootstrap.diff`, A7; **necesita S1**: orden S1 → S4 → S2 → S5 → S3) | `GameBootstrap.Streak.cs` (parcial nuevo, A7) crea el `StreakService` con `SetUpStreak()`: el almacén, la `Wallet` de campaña (`batch.Durable`), el `IClock` de `Start` (`LaunchOptions.ClockOverride` o el del sistema) y `live_unlock_level_daily` de `DailyChallengeConfig` (la del Reto ya creado, o su default), con `StreakConfig.Defaults` y `BasketConfig.Defaults` (aún no hay `AF.RemoteConfig`), y lo inyecta con `LevelFlow.UseStreak(service)` en el flujo de campaña (el del Reto no lo lleva: no cuenta). El parche añade en `Start`, justo después de `SetUpDaily();`, la llamada `SetUpStreak();` (antes de `_app.Start`, para que el nivel reanudado ya tenga el servicio), y el archivo `GameBootstrap.StreakProfile.cs`, que implementa el método parcial `CreateProfileStreakStore` con `new ProfileStreakStore(profile, batch.Durable, clock)` (S1): el mismo perfil y la misma vía síncrona del lote que la cartera. Sin el parche, el método parcial no tiene cuerpo: en desarrollo, almacén en memoria (con aviso) y en release, sin Crecida. Tests: `GameBootstrapStreakTests` (el parche cambia sus dos últimos para el almacén del perfil y la victoria en una sola escritura). La vista lee `LevelFlow.LastStreakResult`; las sondas, `DevProbe.Streak.cs` (§5) |
| E33 | `Scripts/GameFlow/SlothContext.cs` | `SlothVictoryFacts`: `streak` = `LastStreakResult.Streak.Wins` y `streakMilestone` = `LastStreakResult.Streak.MilestonePaid` (el `StreakServiceResult` del `StreakService`; banco `streak` de TiTino, d.6) |
| E25 | (vista del Reto) | Ya anotado: sin barra de la Crecida en el Reto (Q-C2) |

### 6.1 Nota para B1b

La vista previa del premio del hito 6 (barra del HUD, réplica en la victoria) calcula `BasketRules.HitSix` con el **nivel en que
caerá el hito**: el nivel actual + (6 − `wins`) − 1 victorias más. No con el `max_level` de ahora, que puede caer en otra decena y
mostrar otro comodín.

- La fórmula solo vale con `wins` ≤ 5. Con `wins` de 6 a 9, el siguiente 6 cae después de vaciar la barra en el 10 (dentro de
  10 − `wins` + 6 victorias) y, si el hito 6 ya se cobró hoy (`streak_hits_once_per_day`), puede no pagarse: la vista previa no
  promete un premio que la regla de una vez al día no daría (mira `StreakSnapshot.HitPaid(6)`).
- La vista previa se recalcula en cada cambio del estado (victoria, fallo duro, cambio de día) y también tras un Salto del Boto,
  que mueve el nivel actual sin tocar la barra.

## 7. Huecos y preguntas (Q-C1-Q-C14) [P]

Todas con recomendada (la primera). Ninguna reabre una decisión del dueño.

| Id | Hueco | Opciones (recomendada primero) |
|---|---|---|
| Q-C1 | Cómo se cobra el x2 del río crecido | **(a)** se aplica solo al ganar, sin botón, con el estado anterior a la victoria (la que paga el hito 10 no lo lleva, como `econ_sim`); (b) un botón «x2 gratis» que hay que tocar |
| Q-C2 | El Reto del río y el río crecido | **(a)** el Reto no suma ni rompe la Crecida y **no** lleva el x2 gratis del río (su Cosecha doble es la de vídeo); (b) el x2 gratis vale también en el Reto (la pregunta del dueño anotada en `spark-estado.md`, E19/E25). **Ojo:** la (a) se aparta de una lectura literal de 04 §2.8, que dice «durante río crecido… se aplica sin video» sin limitarlo a la campaña; la (a) lo apoya en 06 §4.3 («el Reto del río y los eventos no la tocan») |
| Q-C3 | Qué comodín trae la Canasta del hito 6 | **(b)** el de la **próxima Canasta de nivel**, deducido de `max_level`, sin estado nuevo; (a) un contador propio de Canastas del hito, como `streak_chests` de `econ_sim` (campo nuevo en el perfil) |
| Q-C4 | La Canasta grande y la normal del mismo nivel | **(a)** la grande sustituye a la normal de 100, 200… y ocupa su ranura (el ciclo no se desplaza: 110 sigue siendo Morpho); (b) se pagan las dos |
| Q-C5 | «Próxima Canasta de nivel» para el hito 6 | **(a)** la del primer múltiplo de 10 mayor que `max_level` tras la victoria (como mínimo el 20); si es un múltiplo de 100, el comodín del ciclo en esa ranura (`undo3` en el 100), nunca la grande. El servicio lo calcula con `BasketRules.HitSix(Math.Max(level, maxLevelBefore))` (`StreakRules.OnVictory(level, campaign, progressLevel)`). Como 06 §4.5 no deja rejugar («sin rejugar: "Jugar" siempre abre el siguiente nivel»), hoy `level` = `maxLevelBefore` + 1 y da lo mismo que `HitSix(level)`; un test fuerza un nivel rejugado; (b) la grande entera |
| Q-C6 | `streak_shield_per_day` = 2 (rango 1-2) | **(a)** en la 1.0, una sola hoja al día con el bool de `daily`, valga lo que valga (un test lo fija); (b) un contador de hojas en `daily` |
| Q-C7 | `streak_broken` de `af_level_fail` | **(a)** solo si se perdió algo: barra ≥ 1 o río crecido; (b) en todo fallo duro sin hoja |
| Q-C8 | Forma de los hitos de hoy en `daily` y su fusión entre dispositivos (04 §10.2 fusiona `daily` con `max` por clave, que no vale para una lista) | **(a)** `streak_hits` como lista de hitos (`[3, 6]`, la forma de A4), fusionada por unión, y `streak_seeds` con `max`; (b) tres bools |
| Q-C9 | El botón x2 por vídeo con río crecido | **(a)** no se ofrece (la Cosecha ya va doblada) y no gasta `ads_rv_cap_double_harvest`; (b) se ofrece además (×3) |
| Q-C10 | El tope de 600 cuando no cabe un hito entero | **(a)** pago parcial (`min(cantidad, hueco)`, como `pay_streak_hit`); el hito queda cobrado; el comodín de la Canasta del 6 se da entero; (b) todo o nada |
| Q-C11 | `biz_feature_win_streak` = false, o `streak_unlock_level` subido por encima del nivel actual | **(a)** se congela: no suma ni rompe, sin x2 del río y sin barra; el estado guardado se conserva; (b) se borra |
| Q-C12 | Dónde vive el contenido de la Canasta grande | **(a)** constante [P] en `BasketRules` (GDD.md §6.1 #13), sin clave de RC; (b) clave nueva (alta en 08 §9) |
| Q-C13 | Analítica de las dos Canastas | **(a)** `af_basket_open` solo para la de nivel; la del hito 6 va en `af_streak_milestone` (`reward_type` = `chest`, `reward_value` = `contents_json`) y sus 50 semillas con `source = streak` (cuentan en el tope); si en la misma victoria hay las dos (p. ej. hito 6 al ganar el 30), se pagan las dos; (b) `af_basket_open` también para la del hito |
| Q-C14 | El x2 gratis del río frente a `ads_rv_double_harvest_from_level` (11; si RC lo subiera por encima de `streak_unlock_level`, `Wallet.Apply` lo rechazaría con `NotUnlocked`) | **(a)** el x2 del río no pasa por esa compuerta: es la Cosecha doble sin anuncio de 04 §2.8, no la ubicación de vídeo (E35); (b) se respeta la compuerta y, por debajo de ella, no hay x2 del río |

## 8. Hallazgos para el GDD (los lleva el integrador)

- **08 §4.3:** `streak` sin `shield_available` y con `{wins, river_full}`; `daily.streak_hits` y `daily.streak_seeds` [P] (Q-M9, Q-C8).
- **08 §2.2:** quitar «Crecida» de la fila de `AF.LiveOps` (Q-M10).
- **05 §3 / `tools/econ_sim.py`:** el desfase del ciclo de la Canasta ya está en 05 §3.4. Quedan pendientes volver a correr la simulación con la Canasta grande (05 §3.4; GDD.md §6.1 #13) y la hoja del día, que depende del nivel al empezar el día (§1.2).
- **Glosario:** falta la entrada «Canasta grande» (GDD.md §6.1 #13; plan §6).
