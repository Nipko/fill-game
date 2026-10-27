# Remote Config, analítica y bus de eventos: nota del bloque B7

Bloque B7 de la tanda 5 (`docs/dev/plan-tanda5.md` §4; encargo §12 de `docs/dev/traspaso/spark-reto-del-rio.md`). Rama
`spark/t5-b7`, desde `origin/integracion-fase1-fase2` (`9eb593c`). Incluye el bus mínimo `AF.Events` (D-033, Q-T1 (a)).

**No sustituye al GDD.** Mandan 08 §2.1-§2.3 (módulos y arranque), 08 §8 (taxonomía única de analítica), 08 §9 (tabla única
de Remote Config), 04 §10.2 (congelación de precios y topes) y 04 §11 (parámetros que 08 §8.2 delega). Lo marcado [P] es
propuesta de esta nota; según la regla del dueño del 2026-10-19, va la recomendada (la (a)) mientras no diga otra cosa (§6).

## 1. Qué hay

| Pieza | Dónde | Motor | Depende de |
|---|---|---|---|
| Generador de `RemoteConfigDefaults.json` | `tools/remote-config/rc_defaults.py` (+ `test_rc_defaults.py`) | — | Python 3.14, biblioteca estándar |
| Generador de la plantilla de Firebase (§9) | `tools/remote-config/rc_template.py` (+ `test_rc_template.py`) → `tools/out/remote_config_template_{dev,prod}.json` | — | `rc_defaults.py` |
| Defaults compilados | `Assets/_Project/Data/Config/RemoteConfigDefaults.json` y `Scripts/RemoteConfig/RemoteConfigDefaults.Generated.cs` (generados) | — | — |
| `AF.Events` | `Scripts/Events/` | no | — |
| `AF.RemoteConfig` | `Scripts/RemoteConfig/` | no | — |
| `AF.RemoteConfig.Binding` [P] | `Scripts/RemoteConfig/Binding/` | no | RemoteConfig y las clases de cifras (Economy, Boosters, Ads, IAP, LiveOps, Platform…) |
| `AF.Analytics` | `Scripts/Analytics/` | no | Events, RemoteConfig |
| `AF.Analytics.Unity` | `Scripts/Analytics/Unity/` | sí (`Debug.Log`) | Analytics |
| `AF.Analytics.Adapters` [P] | `Scripts/Analytics/Adapters/` | no | Analytics, Ads, IAP, Economy |
| Tests | `Scripts/RemoteConfig/Tests/` (`AF.RemoteConfig.Tests`), `Scripts/Analytics/Tests/` (`AF.Analytics.Tests`) | EditMode | — |
| Ganchos | `docs/dev/traspaso/spark-enganches/B7-*.diff` (§5) | — | — |

Ningún archivo existente cambia en la rama: todo lo que toca archivos del escritorio va en los parches.

## 2. Remote Config

### 2.1 El JSON generado (08 §9: «`RemoteConfigDefaults.json` se genera a partir de esta tabla»)

`python tools/remote-config/rc_defaults.py` lee la tabla de 08 §9 y escribe el JSON y su copia en C#. `--check` no escribe y
sale con 1 si alguno difiere («identico (251 claves)» si no); `--self-test` prueba el lector en memoria y contra la tabla real.

Formato `af-rc-defaults/1` [P]: `{"format", "source", "generator", "params": {clave: {"type", "default"[, "pending"]}},
"pending_templates"}`, con las claves en el orden de 08 §9, 2 espacios, UTF-8 sin BOM y LF (`.claude/rules/data-json.md`).
Tipos: `int`, `float`, `bool`, `string`, `json` (`enum` → string; `map país→…` → json).

Cómo se leen las filas: las claves son lo que va entre comillas invertidas en la primera celda (`x_{a,b}` → `x_a`, `x_b`, y `x.{a,b}` → `x.a`, `x.b`;
`x_<s>` (`a`, `b`) → `x_a`, `x_b`; `_trail` sustituye el último tramo de la clave anterior); tipos y defaults se reparten por
« / »; del default se quitan negritas, comillas y un comentario final entre paréntesis («false (1.1: true)» → false).

Diez celdas no son un valor literal. Cada una tiene una **interpretación fijada al texto exacto de su celda**: si 08 §9 cambia
esa celda, el generador falla y pide revisarla, en vez de adivinar.

| Celda (primera clave) | Se escribe | Nota |
|---|---|---|
| `biz_country_enabled`: «CO, PE, MX = true; resto false» | `{"CO":true,"PE":true,"MX":true,"*":false}` | «resto» = `"*"` [P] (Q-B7-4). **D-057 (2026-10-27):** 08 §9 pasa a `{"*":true}` literal y la interpretación se retira |
| `biz_legal_profile_by_country` | `{"BR":"br_lgpd_eca",…,"EEE":"ump","UK":"ump","CH":"ump","*":"default"}` | idem; `EEE`, `UK` y `CH` tal cual los escribe 08 [P] |
| `biz_ua_paid_enabled_by_country` / `biz_ua_test_active`: «todos false / false» | `{"*":false}` / `false` | idem |
| `biz_kpi_targets` / `biz_kpi_alert_thresholds`: «01 §KPIs / 09 tabla 6.1» | `null` + `pending` | remiten a otra tabla (Q-B7-6) |
| `mech_debut_level_{…}`: «chica 14, alta 27, …» | un int por mecánica | lista «etiqueta valor» |
| `ads_rv_cap_<placement>`: «double_harvest 5 · undo_refill 4 · …» | `ads_rv_cap_double_harvest` = 5… | los placements salen del default |
| `ads_rv_cooldown_s_<placement>`: «60 (extra_canoe 90)» | 60 en los seis placements de la fila anterior; 90 en `extra_canoe` | [P] (Q-B7-5); coincide con `AdsConfig` |
| `eco_calendar_table_v1`: «tabla 05 §2.3 (…)» | `null` + `pending` | Q-B7-6 |
| `eco_price_tier_<country>` / `eco_model_version` | solo `eco_model_version`; la plantilla va a `pending_templates` | la plantilla no nombra países |
| `league_bucket_size` … (6 claves, 5 valores) | `league_prizes` y `regatta_prizes` con `null` + `pending` («tablas 06 §6 y §5.2») | Q-B7-6 |

Comprobaciones: `test_rc_defaults.py` (el JSON y el C# son los generados, cada clave con valor o con nota y ningún alias retirado
de 08 §9 en el JSON) y, en EditMode, `RemoteConfigDefaultsTests` (el C# compilado es el archivo byte a byte; todas las claves con
`[RemoteConfigKey]` del cliente están en el JSON) y `RcConfigBinderTests` (con los defaults compilados, cada clase de cifras es
exactamente su `Defaults`: el JSON de 08 §9 y el código no pueden separarse).

### 2.2 Acceso, origen y congelación

- `RemoteConfigService`: defaults compilados (`RemoteConfigDefaults.Compiled`, sin cargar ningún asset [P], Q-B7-21), caché del
  proveedor (`ActivateCached`, 08 §2.3), `FetchIfDue` con el intervalo mínimo de 12 h, `Activate(valores)` (como Firebase: clave
  → cadena), `Get<T>` / `TryGet<T>`, `Source` (`Default`/`Cached`/`Remote`, para `af_boot.rc_source`) y `Variant`
  (`biz_experiment_tag` → `rc_variant`, 08 §9). Pendiente con Firebase: llamar a `FetchIfDue` en el arranque, guardar la hora de la última descarga y, si se activa a mitad de sesión, usar `RcScreenFreezer` en los modales.
- Reglas: una clave fuera de 08 §9 no existe (`KeyNotFoundException`); `Get<T>` con un tipo que no sirve lanza; un valor
  remoto mal formado se descarta y queda el anterior (`Rejected`); un `rc_schema` mayor que el compilado hace ignorar el
  conjunto entero (08 §9); las claves remotas desconocidas se ignoran; una clave `pending` no tiene valor hasta que llega uno.
- `RC.Get<T>(key)` (08 §2.2): fachada estática sobre el servicio del arranque (`RC.Use`); sin arranque, los defaults.
- Congelación por pantalla (08 §2.2; 04 §10.2: «nunca cambian dentro de un modal abierto»): cada activación crea un
  `RcSnapshot` inmutable; `Freeze()` lo devuelve y `RcScreenFreezer` guarda el de cada pantalla abierta hasta cerrarla.
- `FakeRemoteConfigBackend`: el proveedor falso (08 §2.1, principio 6) con `Set`, `StoreFetched` y `Offline` para los tests.

### 2.3 Las clases de cifras leen de Remote Config

`RcConfigBinder.Bind(snapshot, appVersion)` devuelve `EconomyConfig`, `BoosterConfig`, `AdsConfig`, `StoreConfig`,
`DailyChallengeConfig`, `StreakConfig`, `BasketConfig` y `ClockSecurityConfig` con cada propiedad `[RemoteConfigKey]` leída
de su clave (88 claves). **Sin reflexión** (Managed Stripping High, B10): una tabla de asignaciones por clase; el test de
cobertura compara esa tabla con los atributos. Un valor que no se entiende deja el default de esa propiedad y se anota en
`Errors`; una clase que no pasa su `Validate()` vuelve entera a `Defaults` [P] (Q-B7-7). El Reto recibe la misma
`sec_clock_max_drift_h` en su copia (`DailyChallengeConfig.ClockSecurity`).

`ShuffleConfig` (B2, `AF.Boosters`, 5 claves), `SkipConfig` (B3), `ComebackConfig` y `PushConfig` (B6, `AF.LiveOps`) y `VerificationQueueConfig` (F3-1, `AF.Backend`) no están aquí todavía. Ya están en la integración (`803e9723`) y en esta rama, que
las fusionó; falta añadir su tabla a `RcConfigBinder` (una línea por clave) y quitarlas de `NotBoundYet`. `RcConfigBinderTests` descubre por
reflexión, en los tests, **todos** los tipos con `[RemoteConfigKey]` de todo ensamblado cargado que referencie el de
`RemoteConfigKeyAttribute` (`AF.Platform`), más ese mismo, salvo los de pruebas (`RemoteConfigKeyScan`); una aserción exige que
estén al menos los 6 ensamblados de cifras de hoy, para que el escaneo no salga vacío en silencio. Cada
uno tiene que estar enlazado, con exactamente sus claves, o en la lista explícita `NotBoundYet` con su motivo; si no, la prueba
falla.

### 2.4 `biz_maintenance_*`, `biz_min_app_version` y `biz_feature_*` (08 §9)

`BizGates` sobre unos valores congelados:
- `biz_maintenance_mode`: «bloquea anuncios e IAP; el juego offline sigue» → `AdsEnabled` e `IapEnabled` false.
- `biz_min_app_version`: «por debajo, pantalla "Actualiza Tintalú" (compras y liga bloqueadas)» → `UpdateRequired` e
  `IapEnabled` false. Versiones por números (`1.0` = `1.0.0`; `Application.version` hoy es `1.0`); una mínima vacía o ilegible no
  bloquea [P].
- `biz_maintenance_message`: se expone tal cual; la vista la trataría como clave de `strings.csv` [P] (Q-B7-8).
- `biz_iap_enabled`, `biz_ads_enabled`, `biz_interstitials_enabled`, `biz_rewarded_enabled`: el binder los combina con lo
  anterior en `AdsConfig.Biz*` y `StoreConfig.BizIapEnabled`, que ya consumen `AdPolicy` y la tienda.
- Los ocho `biz_feature_<sistema>` (`BizGates.Feature`): `daily_challenge` y `win_streak` ya los leían sus clases;
  `lives` apaga las canoas (`Wallet.LivesEnabled`, parche B7-wallet) y `rescue` quita la Cuia de rescate
  (`rescue_max_per_level` = 0 en el binder) [P]; `login_calendar`, `murky`, `piggy` y `shop` quedan expuestos para cuando
  existan sus sistemas o su pantalla (B4, B5) [P] (Q-B7-14).
- La pantalla «Actualiza Tintalú» y el aviso de mantenimiento no se hacen aquí: son UI del escritorio y textos nuevos (§7).

## 3. Bus `AF.Events` (D-033, Q-T1 (a))

`EventBus`: síncrono, en memoria, sin motor; un canal por tipo, en orden de suscripción; publicar sin oyentes no asigna; un
oyente que lanza no corta a los demás ni al que publica (va a `HandlerFailed`) [P]. Eventos de dominio con datos simples (ids,
nunca texto): `LevelStarted`, `LevelWon`, `LevelFailed`, `LevelAbandoned` (los publica `LevelFlow`, parche B7-levelflow, y
`AppFlow` el abandono, parche B7-appflow) y `SeedsEarned` / `SeedsSpent` (los publica `Wallet.Apply`, parche B7-wallet).
Consume `AF.Analytics`. Las interfaces de escucha que ya existen (`ILevelFlowListener` y demás) siguen: el bus no las sustituye.

## 4. Analítica (08 §8)

- **Catálogo** (`AnalyticsCatalog`): los 68 eventos de 08 §8.2 con sus parámetros específicos, y los de 04 §11 donde 08 dice
  «Según capítulo 4 §11». `AnalyticsCatalogTests` lee las dos tablas del GDD en cada ejecución y exige que coincidan; ninguno
  pasa de 10 propios (08 §8.1). Los comunes (15) y las propiedades de usuario salen de 08 §8.1.
- **Servicio** (`AnalyticsService`): valida cada evento contra el catálogo (lo que no está se descarta y se anota en
  `Violations` [P]), añade los comunes de 08 §8.1 en su orden (los null no se envían; un específico con el mismo nombre manda)
  [P] (Q-B7-12) y entrega a los sumideros. **Consentimiento** (08 §2.3): los sumideros que envían fuera (`RequiresConsent`, el de
  Firebase, F3-6) solo reciben con `CollectionEnabled`, que empieza apagado y sigue al consentimiento de analítica (B12: la
  pantalla «Tu privacidad» y Ajustes › «Privacidad y datos», al momento); sin él solo escriben los locales [P] (Q-B7-20). Además,
  `FirebaseAnalyticsSink` tiene la recogida propia apagada (`SetAnalyticsCollectionEnabled`, `ICollectionAwareSink`).
- **Sumideros**: `FakeAnalyticsSink` (tests) y `LogcatAnalyticsSink` (`AF.Analytics.Unity`): una línea
  `AF_EVENT {"event":"…",<específicos>,<comunes>}` por evento, solo en desarrollo y en el Editor [P] (Q-B7-16). Prefijo propio:
  las sondas `AF_STATE` de `DevProbe` no cambian y no se duplican.
  - **Sin consentimiento, `AF_EVENT_UNSENT {…}`** (observación 5 de `docs/dev/traspaso/pruebas-telefono-2026-10-22.md`): con la
    analítica apagada el sumidero de logcat sigue escribiendo (Q-B7-20 (a): es diagnóstico local, no sale del teléfono), pero con
    el prefijo `AF_EVENT_UNSENT` en lugar de `AF_EVENT`, para que se vea en el log que **ese evento no llegó a ningún sumidero
    que envía fuera** (Firebase no lo recibe: `AnalyticsService` no se lo entrega). `AF_EVENT` quiere decir «con consentimiento»:
    se entregó también a los sumideros que envían, si los hay (en el Editor y sin `google-services.json`, ninguno). El sumidero
    lo sabe por `ICollectionAwareSink`, como Firebase. Las propiedades de usuario siguen la misma regla. Con <13 (bloqueo de
    B12) no sale ninguna línea, como se vio en el teléfono. Test: `AnalyticsServiceTests.WithoutConsent_OnlyLogcat_AndItsLineSaysUnsent`.
- **Sesión** (`SessionTracker`, D-033, Q-M4 (a)): arranque en frío o vuelta tras ≥ 30 min en segundo plano; id aleatorio sin
  datos del jugador [P] (Q-B7-17).
- **Eventos emitidos con los parches** (§5): `level_start`, `af_level_win`, `af_level_fail`, `level_end`, `af_level_abandon`
  (`map`), `earn_virtual_currency`, `spend_virtual_currency` (todas las fuentes y sumideros de `Wallet.Apply`, también los del
  Reto), `af_ad_request`, `af_ad_shown`, `af_ad_reward` (envoltorio de `IAdService`), `af_iap_result` y `purchase` (envoltorio
  de `IStoreService`; `af_iap_offer_shown` queda listo en el adaptador para que la vista lo llame), `af_map_open`,
  `af_map_close`, `af_stop_arrive` (victoria y Mapa), `af_river_fact_shown` (victoria y secuencia del 10) y
  `af_fruit_discovered`.
- **Pendientes** (el catálogo ya los admite; falta su gancho): `af_boot`, `af_consent`, `af_wallet_snapshot`, los de
  comodines y del modal (`af_booster_*`, `af_stuck_*`, `af_skip_used`…), los de canoas, `af_streak_milestone` /
  `af_streak_shield_used` / `af_basket_open` (hoy sondas de B1a), `af_daily_challenge` y el `level_start` del Reto (hoy sondas
  de R10) (Q-B7-9), `af_settings_change`, `af_label_shown`, `af_world_enter`, `af_mechanic_intro`, `af_level_load`, `af_error` y el `level_end` del Salto del Boto (08 §8.2: «Victoria, fallo duro o salto»; con B3).
  Cada uno es una línea en el punto de su sonda, como los de §5; se propone hacerlos por zonas cuando el escritorio libere los
  archivos.

## 5. Ganchos (parches para el integrador)

En `docs/dev/traspaso/spark-enganches/`, LF, generados con `git diff` sobre `spark/t5-b7` y probados en un worktree aparte con
todos aplicados (§8). Tocan archivos distintos, así que aplican en cualquier orden con `git apply`, pero **compilan en este
orden**:

| # | Parche | Archivos | Qué hace |
|---|---|---|---|
| 1 | `B7-asmdef.diff` | `AF.Economy`, `AF.GameFlow`, `AF.UI`, `AF.Tests.EditMode`, `AF.Tests.PlayMode` (`.asmdef`) | Economy y GameFlow → `AF.Events`; UI → los seis ensamblados nuevos; los de test → Events, RemoteConfig, Binding y Analytics |
| 2 | `B7-wallet.diff` | `Economy/Wallet.cs` | `Wallet.Events` publica `SeedsEarned`/`SeedsSpent` tras `Changed`; `Wallet.LivesEnabled` (`biz_feature_lives`) en `CanoesActive` |
| 3 | `B7-levelflow.diff` | `GameFlow/LevelFlow.Events.cs` (nuevo), `LevelFlow.cs`, `LevelFlow.Victory.cs`, `LevelFlow.Funnel.cs`, `Tests/EditMode/TelemetryHooksTests.cs` (nuevo) | Una línea en `FinishIntro`, `Undo`, `ResolveWin` y `Fail`; el parcial publica los eventos del nivel (solo campaña) |
| 4 | `B7-appflow.diff` | `GameFlow/AppFlow.cs` | `ExitToLobby` publica el abandono si el nivel sigue sin resolver |
| 5 | `B7-gamescreen.diff` | `UI/UiAnalytics.cs`, `UI/GameScreen.Analytics.cs` (nuevos), `GameScreen.Arrival.cs`, `GameScreen.Victory.cs`, `GameScreen.Notices.cs` | Una línea junto a cada sonda: `af_stop_arrive`, `af_river_fact_shown`, `af_fruit_discovered` |
| 6 | `B7-rivermap.diff` | `UI/RiverMapScreen.Analytics.cs` (nuevo), `RiverMapScreen.CatchUp.cs`, `RiverMapScreen.Sequence.cs` | Idem en el Mapa |
| 7 | `B7-bootstrap.diff` | `UI/GameBootstrap.Telemetry.cs` (nuevo), `GameBootstrap.cs`, `GameBootstrap.Daily.cs`, `GameBootstrap.Streak.cs`, `Tests/PlayMode/TelemetryBootTests.cs` (nuevo) | `SetUpTelemetry` antes de la cartera; las cifras de RC a la cartera, el flujo, los anuncios, la tienda, el Reto y la Crecida; los envoltorios de anuncios y tienda; `ConnectTelemetry` tras `SetUpSessions` (la última `SetUp*`); `af_map_open`/`close`; la sesión en `OnApplicationPause` |
| 8 | ~~`B7-gdd.diff`~~ (retirado) | — | La integración ya corrigió `anim_speed_factor_late` = 1,0 en 08 §9 y 02 §4 (D-033, Q-N1; `f4f4bdee`, con D-035). La rama regeneró el JSON y su C# desde esa tabla (también `biz_backend_enabled` nueva y `sec_appcheck_enforce` retirada) |

Los `.meta` de los archivos nuevos van dentro de cada parche (los generó Unity en el worktree de prueba).

**Al aplicarlos:**
- con `git apply` dentro del repositorio, o con `git -c core.autocrlf=false apply` (los parches van en LF y el árbol, también);
- desde la fusión de `803e9723` (con B14, B3 y B6 ya integrados), `ConnectTelemetry();` va justo después de `SetUpSessions();`,
  la última llamada `SetUp*` de `Start` (el parche se regeneró por eso): el bus se conecta con todo ya construido. Si se añade
  otra `SetUp*` en ese bloque, `ConnectTelemetry();` sigue siendo la última.

Cómo se prueban: `TelemetryHooksTests` (EditMode: la cartera publica con su fuente y sumidero; las canoas con `lives` apagado;
inicio, victoria, fallo duro, segundo intento y abandono del flujo; la cadena del flujo a 08 §8.2 sin violaciones) y
`TelemetryBootTests` (PlayMode: la escena real arranca con la caché de RC en la cartera, un nivel ganado da `level_start`,
`earn_virtual_currency` (25 semillas con `eco_reward_normal` = 25 en la caché), `af_level_win` y `level_end` con los comunes, la
recogida apagada y la línea `AF_EVENT_UNSENT` en el log, porque la escena de prueba arranca sin consentimiento).

## 6. Preguntas (todas con la recomendada aplicada, [P])

| Id | Pregunta | Opciones (la primera, recomendada y aplicada) |
|---|---|---|
| Q-B7-1 | 08 §2.2 pone `AF.RemoteConfig` → `Backend`, que no existía | (a) sin dependencias hasta que haga falta; (b) crear ya un `AF.Backend` vacío. Desde F3-1 (`a38600a4`, D-035) existe `AF.Backend`, el cliente de la API de AWS; `AF.RemoteConfig` sigue sin referenciarlo porque hoy no lo usa |
| Q-B7-2 | `AF.Events` no tiene fila en 08 §2.2 (solo el principio 3 de §2.1) | (a) sin dependencias; lo referencian Economy, GameFlow, Analytics y UI; añadir su fila a 08 §2.2; (b) meterlo dentro de `AF.Platform` |
| Q-B7-3 | El enlace de las cifras y los envoltorios de anuncios e IAP necesitan ver a la vez Remote Config o Analytics y los módulos | (a) dos ensamblados de composición (`AF.RemoteConfig.Binding`, `AF.Analytics.Adapters`) que solo usa `AF.UI`; ningún archivo del escritorio cambia; (b) que cada módulo referencie `AF.RemoteConfig`/`AF.Analytics` (08 §2.2 lo permite) y lea sus claves él mismo, con un parche por módulo |
| Q-B7-4 | Cómo se escribe «resto» en los mapas por país de 08 §9 | (a) la clave `"*"`; (b) un objeto `{"default":…, "by_country":{…}}`; (c) la lista ISO completa (inventaría países) |
| Q-B7-5 | `ads_rv_cooldown_s_<placement>` = «60 (extra_canoe 90)» no nombra los placements | (a) los seis de la fila `ads_rv_cap_<placement>` (como `AdsConfig`); (b) solo los cinco de la 1.0 (sin `daily_chest`) |
| Q-B7-6 | Defaults que remiten a otra tabla (`eco_calendar_table_v1`, `biz_kpi_*`, `league_prizes`, `regatta_prizes`, `eco_price_tier_<country>`) | (a) `null` + `pending`, y cada bloque transcribe la suya a 08 §9 cuando la implemente (B4 la Bitácora, con `eco_calendar_table_v2` de D-033); (b) transcribirlas ya desde 05 §2.3, 05 §4.2, 06 §5.2/§6, 01 y 09 |
| Q-B7-7 | Un valor remoto que no pasa `Validate()` | (a) la clase entera vuelve a `Defaults` y se anota; (b) solo esa clave vuelve a su default. **Ojo:** las `Validate()` no comprueban los rangos de 08 §9, solo lo que cada clase valida (p. ej. `EconomyConfig` no limita `eco_reward_*` a 15-30 / 45-90 / 60-120): un valor fuera del rango de 08 §9 que `Validate()` acepte se usa tal cual. Se propone completarlas en sus bloques, o que el generador copie la columna «Rango» al JSON y el binder la aplique |
| Q-B7-8 | `biz_maintenance_message` es «string»; Remote Config lleva claves, nunca texto (`.claude/rules/localizacion.md`) | (a) es una clave de `strings.csv`; vacía = aviso genérico; (b) texto libre por idioma |
| Q-B7-9 | Analítica del Reto (`level_start` con `d{b}_n{nnnn}`, `af_daily_challenge`) | (a) sigue como sonda `AF_STATE` (R10) y su gancho va en un parche aparte sobre `DailyChallengeScreen`; (b) que el `LevelFlow` del Reto publique también por el bus |
| Q-B7-10 | `af_level_win.double_harvest` | (a) el estado en el instante de la victoria (el x2 del río crecido); la Cosecha doble por vídeo sale como `earn_virtual_currency` `double_harvest`; (b) enviar `af_level_win` al salir de la victoria, con el x2 final |
| Q-B7-11 | `af_level_abandon.reason` | (a) solo `map` (pausa › «Salir» en 1-30; 02: «salir al Mapa del río no es fallo»); `app_background` y `crash_resume` esperan a que se defina cuándo una salida a segundo plano es abandono; (b) `app_background` en cada pausa con un nivel abierto |
| Q-B7-12 | Comunes de 08 §8.1 «a todo evento de juego» | (a) a todos los eventos, sin los null (fuera del nivel no hay `level`); (b) solo a los de tablero |
| Q-B7-13 | `boosters_json` | (a) `{"undo":n,"extra_cuia":n}` con lo que sabe el flujo, solo lo > 0; (b) añadir Morpho, Luciérnaga y Pororoca cuando sus ganchos lo cuenten |
| Q-B7-14 | Qué apaga cada `biz_feature_<sistema>` (08 §9 y 09 solo dicen «kill switch por sistema») | (a) `lives` = las canoas no cuentan (como antes del 31), `rescue` = sin Cuia de rescate; los demás, al llegar su sistema; (b) definirlos todos ya en 09 |
| Q-B7-15 | `anim_speed_factor_late` (D-033, Q-N1: «se corrigen 08 §9 y 02 §4») | Cerrada: la integración corrigió 08 §9 y 02 §4 (`f4f4bdee`) y `B7-gdd.diff` se retiró |
| Q-B7-16 | Formato del sumidero de logcat | (a) `AF_EVENT {json plano}`, solo desarrollo y Editor; (b) dentro de `AF_STATE`. *Desde el 2026-10-22 (observación 5 de la prueba en el teléfono):* sin consentimiento de analítica, `AF_EVENT_UNSENT {json plano}` (§4); quien filtre por `AF_EVENT ` con espacio solo ve las enviadas |
| Q-B7-17 | `session_id` | (a) GUID aleatorio; (b) el instante de inicio en ms |
| Q-B7-18 | Booleanos | (a) `true`/`false` en el evento; el sumidero de Firebase los pasará a 0/1; (b) 0/1 ya en el catálogo |
| Q-B7-19 | `rc_variant` | (a) `biz_experiment_tag` tal cual (08 §9); (b) además la condición de Firebase |
| Q-B7-20 | Consentimiento sin pantalla | (a) recogida apagada hasta B8/consentimiento; los sumideros locales escriben; (b) encendida en desarrollo |
| Q-B7-22 | 04 §11: «los eventos de tablero añaden además `pours` y `t_ms`», que no están en las celdas de cada evento | (a) añadirlos al catálogo de los eventos de comodines y del modal (`af_booster_*`, `af_hint_result`, `af_shuffle_result`, `af_stuck_*`, `af_rescue_unsolvable`, `af_skip_used`) al engancharlos, y anotarlo en 08 §8.2; (b) seguir la letra de las celdas |
| Q-B7-23 | `level_start.band` (08 §8.2) en la campaña | (a) mandar `difficulty.band` del nivel (03 §12: `comfort`, `challenge`, `recovery`, `milestone`), y en el Reto su banda (`1`-`3`); sin banda en el paquete, no se manda; (b) no mandarlo en la campaña (solo el Reto) |
| Q-B7-21 | Cómo llega el JSON al cliente | (a) compilado como constante (`RemoteConfigDefaults.Generated.cs`, mismo generador; el test exige que sea el archivo byte a byte); (b) `TextAsset` en la escena (pide tocar `GameSceneBuilder` y regenerar `Game.unity`) |

## 7. Para el integrador (sin aplicar)

- `CLAUDE.md` (mapa y comandos): añadir `Scripts/Events/`, `Scripts/RemoteConfig/` (con `Binding/`), `Scripts/Analytics/` (con
  `Unity/` y `Adapters/`), `Data/Config/RemoteConfigDefaults.json` (generado: no editar a mano) y `tools/remote-config/`; y los
  comandos `python tools/remote-config/rc_defaults.py --check` y `python -m unittest discover -s tools/remote-config -p "test_*.py"`
  («solo si tocas 08 §9 o Remote Config»).
- `docs/dev/prototipo-v0.md`: la línea `AF_EVENT {json}` en logcat (desarrollo), junto a `AF_STATE` y `AF_LAYOUT`.
- 08 §2.2: filas de `AF.Events` (Q-B7-2) y, si se acepta Q-B7-3 (a), de los dos ensamblados de composición.
- Textos nuevos que harán falta (no van en este bloque): «Actualiza Tintalú» y el aviso de mantenimiento, con su pantalla en el
  inicio (`LobbyScreen`, del escritorio).

## 8. Verificación

La de `CLAUDE.md` en `spark/t5-b7` y en un worktree temporal con los parches aplicados (siete desde que se retiró `B7-gdd.diff`); resultados en el informe del
bloque.

## 9. Publicar la plantilla en Firebase (guía del dueño)

Hasta ahora ningún proyecto de Firebase (dev `fill-dev-e75a2`, prod `tintalu`) tiene parámetros publicados: el juego funciona
con los defaults compilados. Esta sección deja lista la publicación. **Los agentes no tocan la consola ni la nube: publica el
dueño.**

### 9.1 La plantilla

`python tools/remote-config/rc_template.py` lee 08 §9 con el mismo lector que `rc_defaults.py` y escribe la plantilla en el
formato de la API REST de Remote Config v1 (`{"conditions": [], "parameters": {clave: {"defaultValue": {"value": …},
"description": …, "valueType": …}}, "parameterGroups": {}}`):

- `tools/out/remote_config_template_dev.json` → proyecto `fill-dev-e75a2`;
- `tools/out/remote_config_template_prod.json` → proyecto `tintalu`.

08 §9 no distingue valores por entorno, así que **las dos son iguales**. `--check` no escribe y sale con 1 si alguna difiere de
lo que generaría; `--self-test` prueba la herramienta en memoria y contra la tabla real; `test_rc_template.py` comprueba los
tipos, que cada valor es el default de `RemoteConfigDefaults.json`, que no hay claves fuera de 08 §9, los límites y la forma de
la API. Cada vez que cambie 08 §9 se regeneran las dos (con `rc_defaults.py`) y se vuelven a publicar (§9.4).

- **Valores:** exactamente los defaults compilados, escritos como el cliente se los da a Firebase
  (`FirebaseRemoteConfigBackend.DefaultsFor`): `int` y `float` → `NUMBER` (un float entero va sin «.0», como el «R» de .NET, y
  nunca con exponente: `0.00001`, no `1E-05`),
  `bool` → `BOOLEAN` (`true`/`false`), `string` (y `enum`) → `STRING`, `json` (y `map país→…`) → `JSON` compacto. El cliente
  trata un valor remoto igual al default como si no hubiera llegado: **publicar la plantilla no cambia el comportamiento**.
- **Descripción:** «Rango 08 §9: <rango>. <uso>» de la fila, cortada a 256 caracteres. Es la chuleta para editar en la consola.
- **Quedan fuera (250 de 256 parámetros van en la plantilla; D-056 y D-057 añadieron `ui_avatars_enabled` y `consent_digital_age_by_country`)** las 6 claves sin valor literal en 08 §9 (`biz_kpi_targets`,
  `biz_kpi_alert_thresholds`, `eco_calendar_table_v1`, `eco_calendar_table_v2`, `league_prizes`, `regatta_prizes`; Q-B7-6):
  no hay valor que publicar. La herramienta las nombra en cada ejecución.
- **Nombres de clave:** Remote Config solo admite letras inglesas, cifras y «_», empezando por letra o «_», hasta 256
  caracteres. Por eso D-043 renombró `mech_enabled.<id>` y `mech_debut_level.<id>` a `mech_enabled_<id>` y
  `mech_debut_level_<id>`: los interruptores de corte de cada mecánica ya se pueden accionar desde la consola. Si 08 §9 volviera
  a tener una clave no admitida, `rc_template.py` la dejaría fuera (y la nombraría), y el cliente tampoco se la daría a Firebase
  (`FirebaseRemoteConfigBackend.IsValidKey`, el mismo criterio).
- **Prueba cruzada:** `FirebaseRemoteConfigBackendTests` (EditMode) lee `remote_config_template_dev.json` y la compara, valor a
  valor, con `DefaultsFor`.

### 9.2 Desarrollo (`fill-dev-e75a2`): importar en la consola

1. Regenera y comprueba: `py -3.14 tools/remote-config/rc_template.py --check` («identico (249 parámetros; …)»).
2. Consola de Firebase → proyecto `fill-dev-e75a2` → **Remote Config**. Descarga **siempre** antes una copia de lo publicado
   (aunque creas que está vacío), y guárdala fuera del repositorio con la fecha: menú ⋮ → **Descargar el archivo de
   configuración actual** (*Download current config file*).
3. Menú ⋮ → **Publicar desde un archivo** (*Publish from a file*) → elige `tools/out/remote_config_template_dev.json` →
   revisa el resumen → **Publicar**. Ojo: **sustituye la plantilla entera**, incluidas las condiciones y los valores
   condicionales que hubiera en la consola.
4. Comprueba en el teléfono con el build de desarrollo: el juego debe comportarse igual (los valores publicados son los
   compilados; la descarga se aplica en el arranque siguiente, §2.2).

### 9.3 Con la CLI de Firebase (alternativa)

La CLI publica la plantilla con `firebase deploy --only remoteconfig`, que lee la ruta de `firebase.json`. Para no añadir un
`firebase.json` al repositorio, hazlo desde una carpeta temporal fuera de él:

```
mkdir C:\tmp\rc-dev ; cd C:\tmp\rc-dev
copy <repo>\tools\out\remote_config_template_dev.json .
# firebase.json con este contenido:
#   { "remoteconfig": { "template": "remote_config_template_dev.json" } }
firebase remoteconfig:get --project fill-dev-e75a2 -o copia_antes.json     # copia de lo publicado
firebase deploy --only remoteconfig --project fill-dev-e75a2
firebase remoteconfig:versions:list --project fill-dev-e75a2               # la versión nueva, arriba
```

Volver atrás: consola → Remote Config → **Historial de cambios** → versión anterior → **Revertir**, o
`firebase remoteconfig:rollback --project fill-dev-e75a2` (con la versión que da `versions:list`; `--help` muestra la opción).

### 9.4 Producción (`tintalu`): todavía no

- **No publiques en `tintalu` hasta que esté registrada la app de prod** (con el `applicationId` definitivo, que espera el
  dictamen de marca: `CLAUDE.md`, regla dura 6) y su `google-services.json`. Sin app no hay cliente que la lea, y publicar antes
  solo deja una plantilla que nadie ha probado contra un build de release.
- Cuando toque: el mismo procedimiento de §9.2 o §9.3 con `remote_config_template_prod.json` y `--project tintalu`, después
  de probarlo en dev con la misma versión de la plantilla (las dos deben salir de la misma ejecución de `rc_template.py`).
- Después de publicar, un cambio de 08 §9 que mueva un default **no llega** a los jugadores con solo subir un build: el valor
  publicado manda sobre el compilado. Cada cambio de 08 §9 = regenerar, probar en dev y volver a publicar en los dos proyectos.

### 9.5 Cambiar un valor sin romper 08 §9

- **Solo dentro del rango de 08 §9** (está al principio de la descripción del parámetro). Un valor fuera del rango es un cambio de
  diseño: primero se cambia 08 §9 (con su capítulo dueño) y después la plantilla, nunca al revés.
- **Respeta el tipo y la forma:** `NUMBER` con punto decimal y sin separador de miles (`0.5`, no `0,5`); en las claves `int`, un
  entero sin decimales (`3`, no `3.0`); `BOOLEAN` solo `true`/`false`; `JSON` válido y con la misma forma que el default (mismas
  claves; las listas, con elementos del mismo tipo). El cliente descarta un valor que no entiende y se queda con el anterior
  (`Rejected`, §2.2), así que un error de tipo no rompe el juego, pero el cambio no llega; el rango, en cambio, el cliente no lo
  comprueba.
- **Nunca subas `rc_schema`** por encima del que lleva el build publicado: el cliente ignora entero un conjunto con un esquema
  mayor (08 §9).
- No crees claves nuevas en la consola: una clave fuera de 08 §9 no existe para el cliente (se ignora). Las claves nuevas
  nacen en 08 §9 y llegan con `rc_defaults.py` y `rc_template.py`.
- Para valores por país, experimentos o pruebas (condiciones de Firebase), crea la condición en la consola y añade el valor
  condicional sobre el parámetro existente; el valor por defecto de la plantilla sigue siendo el de 08 §9. Recuerda que volver a
  publicar desde un archivo (§9.2, paso 3) borra esas condiciones si no están en el archivo.
- Los cambios se aplican en el arranque siguiente a la descarga, y la descarga es como mucho cada 12 h (08 §9): un cambio tarda
  hasta un día en llegar a todos.
