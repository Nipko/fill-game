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
| K1 | Cada 10 niveles de **campaña** (10, 20, 30…), desde `live_unlock_level_daily` (10). | 06 §4.4; 05 §2.1; 08 §9 (`live_unlock_level_daily`: «Desbloquea … Canasta») |
| K2 | Nivel 10: solo 50 semillas. | 06 §4.4; 05 §2.1 |
| K3 | 20, 30, 40…: 50 semillas (`eco_basket_seeds`) + 1 comodín de `eco_basket_cycle` = `["undo3","hint","extra_cuia","undo3","hint"]`, con índice `(L/10 − 2) mod 5`: 20 → 3 Marea, 30 → Morpho, 40 → Cuia extra, 50 → 3 Marea, 60 → Morpho, y se repite. Así salen Cuia extra en 40 y 90 de cada 100 y Morpho en 30, 60, 80, 110… | 06 §4.4; 04 §3 (columna «Gratis»); 08 §9 |
| K4 | `undo3` = 3 Mareas al stock (`stock.undo` += 3). | 06 §4.4 («3 Marea»); 04 §2.1 |
| K5 | Cada 100 niveles (100, 200…): **Canasta grande** = 150 semillas + 1 Cuia extra + 1 Morpho + 3 Marea. Sustituye a la normal de ese nivel (Q-C4). | GDD.md §6.1 #13 (decidida el 2026-10-06); 06 §4.4 |
| K6 | Contenido determinista por nivel y visible antes de abrir; nunca comprable ni abrible con semillas; sin probabilidades. | 06 §4.4; 08 §9 (`eco_basket_cycle`: IARC «ítems aleatorios: no») |
| K7 | Pago con `Wallet.Apply`: `Transaction.Earn(SeedSource.Basket, n)` + `Transaction.GrantStock(…)`. | 08 §2.2 (`AF.Economy`: toda mutación por `Wallet.Apply`) |
| K8 | Sin estado propio: la Canasta de nivel se deduce de `max_level` (la del nivel ganado se paga en su victoria). | 06 §4.5 («sin estado nuevo salvo un campo»); Q-M9 (a) |

**Ojo con `tools/econ_sim.py`:** su ciclo de la Canasta de nivel usa `BASKET_CYCLE[(k − 1) % 5]` con `k = L/10`, que da Marea en
el 10 y Morpho en el 20: va **un hueco por detrás** de 06 §4.4 y 04 §3 (y da comodín en el 10). El cliente sigue al GDD. Además,
la simulación da el comodín del hito 6 con un contador propio (`streak_chests`), y el cliente lo deduce de `max_level` (Q-C3).
Para el integrador (hallazgo para 05 §3): decidir si se corrige la simulación; el efecto en `econ_B.md` es pequeño (un comodín
cambia de sitio en el ciclo) y aquí no se toca `tools/`. Tampoco se copia de la simulación que la hoja escudo del día se dé solo
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
| `Assets/_Project/Scripts/Economy/StreakRules.cs` | `StreakState` (POCO), `StreakOutcome` (valor) y `StreakRules` (`OnVictory`, `OnHardFail`, `RollDay`). |
| `Assets/_Project/Scripts/Economy/EconomyJsonLite.cs` | Lector mínimo de los dos JSON de RC (un objeto plano y una lista de cadenas): `AF.Economy` no ve `AF.Levels` ni los internos de `AF.Save`. |
| `Assets/_Project/Tests/EditMode/StreakBasketConfigTests.cs`, `BasketRulesTests.cs`, `StreakRulesTests.cs` | Tests (EditMode; el asmdef `AF.Tests.EditMode` ya referencia `AF.Economy`). |

Todo sin motor, sin reloj del sistema y sin azar (`EconomyConfigTests.EconomyAssembly_HasNoEngine…` ya lo vigila en la carpeta).
`OnVictory`, `OnHardFail`, `BasketRules.For` y `HitSix` no asignan memoria (devuelven valores `struct`); las transacciones
(`Transaction` es una clase) se piden aparte, con `AppendTransactions`.

**Quién llama (enganches, §6):** `LevelFlow.Victory` (tras `Transaction.Win`) llama a `RollDay` y `OnVictory`; aplica el x2
gratis (Q-C1), las transacciones del hito y la Canasta de nivel. `FailResolve` (tras `Transaction.HardFail`) llama a `RollDay` y
`OnHardFail`, y su resultado alimenta `af_level_fail` (`streak_broken`, `shield_used`) y el toast de la hoja.

## 3. Perfil (Q-M9 (a); lo implementa A4)

- `streak` = `{ "wins": int, "river_full": bool }`, **nullable y omitido** en el JSON hasta su primera escritura (la primera
  victoria de campaña desde el 25); sin migración, como `map_seen_level` (08 §4.3: `profile_schema` 1 aún no publicado).
- En `daily` (04 §10.1), que ya se reinicia entero con `date_local`:
  - `streak_hits` [P]: máscara de los hitos pagados hoy (1 = hito 3, 2 = hito 6, 4 = hito 10);
  - `streak_seeds` [P]: semillas de la Crecida pagadas hoy (para el tope de 600);
  - un solo `shield_available`, **el de `daily`** (ya existe: `DailyData.ShieldAvailable`; `DailyCounters.RollOver` lo pone en
    true). Se quita `shield_available` del bloque `streak` de 08 §4.3 (hallazgo del plan §6).
- `DailyCounters.RollOver` pone `streak_hits` y `streak_seeds` a 0 (enganche E28).
- La Canasta, **sin bloque**: se deduce de `max_level`.
- `StreakState` (en memoria) junta esas piezas: `Wins` y `RiverFull` (de `streak`), `ShieldAvailable`, `HitsToday`,
  `SeedsToday` y `DayLocal` (de `daily`). El adaptador sobre el perfil (`ProfileStreakStore` [P], ola 3) copia de y hacia el
  perfil y guarda con el mismo `IProfileStore` que la cartera, en el mismo lote, para que el pago y el hito no se separen.

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

Hasta que exista `AF.Analytics`, cada evento sale como línea `AF_STATE {"event":…}` en builds de desarrollo (formato de
`DevProbe`, tope de 960 bytes). Lo implementa la ola 3 (`StreakProbeFormat` [P]); aquí solo se fija qué sale:

| Línea | Parámetros | Cuándo |
|---|---|---|
| `af_streak_milestone` | `milestone ∈ {3, 6, 10}`, `reward_type` (`seeds`, `chest`, `extra_cuia+rio_crecido`), `reward_value` (semillas pagadas, `contents_json` de la Canasta o `1`) | Hito **pagado** (no el que se salta por la regla de una vez al día) |
| `af_streak_shield_used` | `streak_at_use` | Fallo duro cubierto por la hoja |
| `af_basket_open` | `level`, `contents_json` | Canasta de nivel (Q-C13) |
| `earn_virtual_currency` | `source ∈ {streak, basket, double_harvest}`, `value` | Hito 3 y Canasta del hito 6 (`streak`), Canasta de nivel (`basket`), x2 del río crecido (`double_harvest`); lo emite `Wallet.Apply` |
| `streak_fail` [P] | `level`, `wins_before`, `river_full_before`, `shield_used`, `streak_broken`, `river_ended` | Cada fallo duro de campaña desde el 25 (para cruzar con `af_level_fail`) |
| `streak_state` [P] | `wins`, `river_full`, `shield`, `hits`, `seeds_today`, `day` | Tras cada cambio del estado (victoria, fallo duro, cambio de día) |

`af_level_fail` (08 §8.2) toma `streak_broken` y `shield_used` de `StreakOutcome` (Q-C7). `contents_json` [P] es estable:
`{"kind":"normal","seeds":50,"undo":3}`, con `kind` ∈ {`normal`, `grand`, `streak`}, `seeds` siempre y después solo los
comodines con unidades, en el orden `undo`, `hint`, `reveal`, `extra_cuia`, `shuffle`.

## 6. Enganches previstos (E26+; parches probados en `docs/dev/traspaso/spark-enganches/` en la ola 3)

| Id | Archivo | Qué |
|---|---|---|
| E26 | `Scripts/Save/Profile.cs` (A4) | `StreakData { Wins, RiverFull }` nullable en `Profile.Streak`; `DailyData.StreakHits` y `StreakSeeds` |
| E27 | `Scripts/Save/ProfileJson.cs` (A4) | Leer y escribir `streak` (omitido si es null) y `daily.streak_hits` / `streak_seeds` |
| E28 | `Scripts/Save/DailyCounters.cs` (A4) | `RollOver` pone `StreakHits` y `StreakSeeds` a 0 |
| E29 | `Scripts/GameFlow/FailResolve.cs` | Tras `Transaction.HardFail`: `RollDay` + `OnHardFail(level, campaign)`; `FailOutcome` lleva `ShieldUsed` y `StreakBroken`. Absorbe E19 (`campaign: !IsDaily`) |
| E30 | `Scripts/GameFlow/LevelFlow.Victory.cs` | En `ResolveWin`, tras `Transaction.Win`: `RollDay` + `OnVictory(level, !IsDaily)`; con `FreeDoubleHarvest`, `Transaction.DoubleHarvest` sin vídeo y `_harvestDoubled` = true (el botón x2 no se ofrece, Q-C9); las transacciones del hito; `BasketRules.For(level, live_unlock_level_daily)` y sus transacciones |
| E31 | `Scripts/GameFlow/LevelFlow.Funnel.cs` | `Fail()` pasa `campaign: !IsDaily` (E19) y el resultado de la Crecida al oyente (toast de la hoja, consuelo de TiTino) |
| E32 | `Scripts/UI/GameBootstrap.cs` | Crear `StreakRules` con el adaptador del perfil, `StreakConfig` y `BasketConfig` (en un parcial nuevo `GameBootstrap.Streak.cs` con un único `SetUpStreak()`) |
| E33 | `Scripts/GameFlow/SlothContext.cs` | `SlothVictoryFacts`: `streak` = `StreakOutcome.Wins` y `streakMilestone` = `MilestonePaid` (banco `streak` de TiTino, d.6) |
| E25 | (vista del Reto) | Ya anotado: sin barra de la Crecida en el Reto (Q-C2) |

## 7. Huecos y preguntas (Q-C1-Q-C13) [P]

Todas con recomendada (la primera). Ninguna reabre una decisión del dueño.

| Id | Hueco | Opciones (recomendada primero) |
|---|---|---|
| Q-C1 | Cómo se cobra el x2 del río crecido | **(a)** se aplica solo al ganar, sin botón, con el estado anterior a la victoria (la que paga el hito 10 no lo lleva, como `econ_sim`); (b) un botón «x2 gratis» que hay que tocar |
| Q-C2 | El Reto del río y el río crecido | **(a)** el Reto no suma ni rompe la Crecida y **no** lleva el x2 gratis del río (su Cosecha doble es la de vídeo); (b) el x2 gratis vale también en el Reto (la pregunta del dueño anotada en `spark-estado.md`, E19/E25) |
| Q-C3 | Qué comodín trae la Canasta del hito 6 | **(b)** el de la **próxima Canasta de nivel**, deducido de `max_level`, sin estado nuevo; (a) un contador propio de Canastas del hito, como `streak_chests` de `econ_sim` (campo nuevo en el perfil) |
| Q-C4 | La Canasta grande y la normal del mismo nivel | **(a)** la grande sustituye a la normal de 100, 200… y ocupa su ranura (el ciclo no se desplaza: 110 sigue siendo Morpho); (b) se pagan las dos |
| Q-C5 | «Próxima Canasta de nivel» para el hito 6 | **(a)** la del primer múltiplo de 10 mayor que `max_level` tras la victoria (como mínimo el 20); si es un múltiplo de 100, el comodín del ciclo en esa ranura (`undo3` en el 100), nunca la grande; (b) la grande entera |
| Q-C6 | `streak_shield_per_day` = 2 (rango 1-2) | **(a)** en la 1.0, una sola hoja al día con el bool de `daily`, valga lo que valga (un test lo fija); (b) un contador de hojas en `daily` |
| Q-C7 | `streak_broken` de `af_level_fail` | **(a)** solo si se perdió algo: barra ≥ 1 o río crecido; (b) en todo fallo duro sin hoja |
| Q-C8 | Forma de los hitos de hoy en `daily` y su fusión entre dispositivos (04 §10.2 fusiona `daily` con `max` por clave) | **(a)** `streak_hits` como máscara (1/2/4), fusionada con OR, y `streak_seeds` con `max`; (b) tres bools |
| Q-C9 | El botón x2 por vídeo con río crecido | **(a)** no se ofrece (la Cosecha ya va doblada) y no gasta `ads_rv_cap_double_harvest`; (b) se ofrece además (×3) |
| Q-C10 | El tope de 600 cuando no cabe un hito entero | **(a)** pago parcial (`min(cantidad, hueco)`, como `pay_streak_hit`); el hito queda cobrado; el comodín de la Canasta del 6 se da entero; (b) todo o nada |
| Q-C11 | `biz_feature_win_streak` = false, o `streak_unlock_level` subido por encima del nivel actual | **(a)** se congela: no suma ni rompe, sin x2 del río y sin barra; el estado guardado se conserva; (b) se borra |
| Q-C12 | Dónde vive el contenido de la Canasta grande | **(a)** constante [P] en `BasketRules` (GDD.md §6.1 #13), sin clave de RC; (b) clave nueva (alta en 08 §9) |
| Q-C13 | Analítica de las dos Canastas | **(a)** `af_basket_open` solo para la de nivel; la del hito 6 va en `af_streak_milestone` (`reward_type` = `chest`, `reward_value` = `contents_json`) y sus 50 semillas con `source = streak` (cuentan en el tope); si en la misma victoria hay las dos (p. ej. hito 6 al ganar el 30), se pagan las dos; (b) `af_basket_open` también para la del hito |

## 8. Hallazgos para el GDD (los lleva el integrador)

- **08 §4.3:** `streak` sin `shield_available` y con `{wins, river_full}`; `daily.streak_hits` y `daily.streak_seeds` [P] (Q-M9, Q-C8).
- **08 §2.2:** quitar «Crecida» de la fila de `AF.LiveOps` (Q-M10).
- **05 §3 / `tools/econ_sim.py`:** el ciclo de la Canasta de nivel va un hueco por detrás de 06 §4.4 y 04 §3, y la hoja del día depende del nivel al empezar el día (§1.2).
- **Glosario:** falta la entrada «Canasta grande» (GDD.md §6.1 #13; plan §6).
