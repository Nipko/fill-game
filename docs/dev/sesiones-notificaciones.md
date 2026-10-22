# Sesiones, Regalo de regreso y notificaciones (B6 de la tanda 5)

Nota del bloque B6 del Spark (rama `spark/t5-b6`, desde `origin/integracion-fase1-fase2` en `9e5d13f`). Fuentes: `docs/dev/plan-tanda5.md` §4 (fila B6) y §5 (Q-M4, Q-M5); D-033; GDD 06 §8.1-§8.3, §9 y §11 (fila 1.0); 08 §2.2 (`AF.Platform`), §4.3 (`settings.push`, `stats.sessions`), §8.2 (`af_push_prompt`, `af_push_sent`, `af_push_open`, `af_comeback_gift`), §9 (`push_*`, `live_comeback_gift`) y Desviación 8; 09 #29.

Lo marcado **[P]** es una propuesta del Spark con la opción recomendada de la pregunta de la sección 8; el dueño puede cambiarla. Nada de esto inventa cifras: todas salen de 08 §9 o de las tablas de 06.

## 1. Qué hay y dónde

**En la rama (archivos nuevos):**

| Archivo | Qué hace |
|---|---|
| `Scripts/Platform/INotificationScheduler.cs` | Puente con las notificaciones locales del sistema (08 §2.2): permiso, programar, cancelar, entregadas y abiertas. Tipos `PushPermission`, `LocalNotification`, `DeliveredNotification`. |
| `Scripts/Platform/FakeNotificationScheduler.cs` | Implementación falsa (editor, tests y builds sin el paquete). |
| `Scripts/LiveOps/SessionRecord.cs`, `SessionTracker.cs` | `stats.sessions` con Q-M4 (a) y `stats.last_seen_at` / `last_win_at` [P]. |
| `Scripts/LiveOps/ComebackConfig.cs`, `ComebackRules.cs`, `ComebackService.cs` | Regalo de regreso (`live_comeback_gift`, 06 §9). |
| `Scripts/LiveOps/PushConfig.cs` | Las 5 claves `push_*` de 08 §9 y las horas silenciosas. |
| `Scripts/LiveOps/PushSettingsRecord.cs`, `PushPrePromptRules.cs`, `PushPermissionService.cs`, `IPushPrePromptPresenter.cs` | `settings.push`, reglas del pre-prompt (06 §8.1), acceso de Ajustes y el pre-prompt simulado. |
| `Scripts/LiveOps/NotificationType.cs`, `NotificationContext.cs`, `NotificationPlanner.cs`, `NotificationService.cs` | Catálogo de la 1.0, planificador puro y servicio de segundo plano / apertura (06 §8.2-§8.3). |
| `Scripts/LiveOps/SessionProbeFormat.cs` | Líneas `AF_STATE` de desarrollo (sección 6). |
| `Scripts/LiveOps/Tests/SessionComebackTests.cs`, `PushPermissionTests.cs`, `NotificationPlannerTests.cs`, `SessionProbeFormatTests.cs` | Tests EditMode (contrastan las claves y los defaults con la tabla de 08 §9). |
| `Scripts/UI/GameBootstrap.Sessions.cs`, `SessionRunner.cs` | Arranque de B6 y el componente que lleva el segundo plano, el cierre y las victorias. Sin llamar hasta el parche B6-bootstrap. |
| `docs/dev/traspaso/spark-enganches/B6-strings.csv` | Los 21 textos nuevos (sección 7). |

**En parches** (`docs/dev/traspaso/spark-enganches/`, LF, probados; orden de la sección 5): `B6-profile.diff`, `B6-bootstrap.diff` y, aparte, `B6-notifications-real.diff`.

## 2. Sesiones (Q-M4 (a))

- Una sesión es **un arranque en frío o una vuelta tras 30 minutos o más en segundo plano** (Q-M4 (a) de D-033). `SessionTracker.BackgroundGap` = 30 min; no es de 08 §9.
- `stats.sessions` (08 §4.3) sube en cada sesión. Lo leen el pre-prompt (`push_preprompt_session`), la Mochila de inicio de B5 (`eco_starter_first_show` = `L12_or_session2`) y la analítica.
- **[P] `stats.last_seen_at`** (UTC): se escribe al empezar cada sesión y al pasar a segundo plano. Mide los días sin sesión del Regalo de regreso (B6-Q1).
- **[P] `stats.last_win_at`** (UTC): la última victoria, de campaña o del Reto. Lo necesita «Crecida te espera» (36 h sin victoria, 06 §8.3).
- Un reloj que retrocede no abre sesión [P]. La reconstrucción de la UI al cambiar de idioma no cuenta otra sesión (el contador sigue vivo con el mismo perfil).

## 3. `settings.push` y el pre-prompt (06 §8.1)

- **Campos:** `pre_prompt_shown` y `granted` (08 §4.3), más **[P] `pre_prompt_at`** y **[P] `second_prompt_shown`** para el segundo pre-prompt (B6-Q2). El bloque solo se escribe si deja su default.
- **Primero:** en la primera victoria (de campaña o del Reto) de la sesión `push_preprompt_session` (2) o posterior, con `push_preprompt_min_level` (10) ya superado **antes** de esa victoria. Así la victoria del nivel 10 nunca lo lleva (su secuencia, 06 §2.3). Si la 2.ª sesión empieza antes del 10, espera a la primera victoria posterior. Nunca en el arranque en frío.
- **Segundo y último** (si el primero no acabó en avisos): en la victoria que **alcanza** el hito 3 de la Crecida (la deja justo en 3) después del primero, o en la primera victoria desde 7 días después del primero, lo que llegue antes. Una racha que ya estaba en 3 o más al rechazar no cuenta (revisión de B6). Queda un caso: si el primero sale en la victoria que deja la Crecida en 3 y luego gana un Reto que no mueve la racha, el segundo saldría en esa victoria. Para cerrarlo haría falta guardar la racha del primero (otro campo [P]); se deja anotado. «Rechazar» es «Ahora no» o negar el diálogo nativo [P].
- **Después:** solo el acceso de Ajustes (`PushPermissionService.SetFromSettings`). Encenderlo pide el diálogo nativo si hace falta; si el teléfono lo tiene bloqueado, no cambia nada y la UI debe explicar cómo activarlo allí (`settings_notifications_blocked`). Apagarlo cancela los avisos programados.
- El diálogo nativo (`POST_NOTIFICATIONS`, Android 13+) solo sale tras «Avísame» o desde Ajustes.
- **`granted`** = el jugador dijo que sí y el sistema lo permite. Al abrir, se baja a false si el teléfono ya no los deja; nunca se sube solo (B6-Q3). En Android 12 o anterior no hay diálogo nativo, pero tampoco se avisa antes de «Avísame».
- **Pre-prompt simulado** (`SimulatedPushPrePrompt`): no dibuja nada y por defecto contesta «Ahora no», para que nunca llegue al diálogo nativo sin el toque del jugador. El arranque solo lo usa con `GameBootstrap.PrePromptOverride` (tests): hasta que la UI tenga su tarjeta no sale ningún pre-prompt, ni en desarrollo, para no gastar los del perfil de pruebas (revisión de B6). El disparador está hoy en la entrada a la pantalla de victoria; con la tarjeta se mueve a Siguiente o Inicio (B6-Q16).
- `af_push_prompt` (08 §8.2): `step` ∈ {`pre`, `native`}; `result` [P] ∈ {`accepted`, `declined`} / {`granted`, `denied`}; más `origin` [P] ∈ {`first`, `second`, `settings`} (B6-Q17). Hasta que exista `AF.Analytics` (B7), lo escucha la sonda.

## 4. Planificador de avisos (06 §8.2-§8.3)

`NotificationPlanner.Plan(context)` es puro: con el estado del jugador al pasar a segundo plano, la lista de avisos si no vuelve a abrir. Al abrir se cancela todo y al salir se vuelve a planificar (06 §8.2, «Canal técnico»). Tipos de la 1.0 (06 §11, fila 1.0; `type` [P] de `af_push_sent` / `af_push_open`, B6-Q18):

| Tipo (`type`) | Disparador | Fuente |
|---|---|---|
| Reto listo (`daily_ready`) | 09:30 local de cada día; hoy solo si aún no son las 09:30 y no jugó [P] | 06 §8.3 (09:30-11:00), B6-Q13 |
| Racha de Retos (`daily_streak`) | Sustituye al Reto listo del día que cerraría 7 seguidos [P] | 06 §8.3 («Día 6 de 7»), B6-Q4 |
| Bitácora sin sellar (`logbook_pending`) | 20:00 local; hoy solo si no la selló. Sin datos hasta B4 | 06 §8.3 |
| Crecida te espera (`streak_waiting`) | `streak_reminder_hours` (36) desde la última victoria, racha ≥ 3; una vez; si ya pasaron, nada [P] | 06 §8.3, B6-Q14 |
| Canoas llenas (`canoes_full`) | Solo si sale con 0 canoas, al llegar al tope | 06 §8.3 |
| Nuevos niveles (`new_levels`) | Si terminó el contenido, a `live_content_end_message.next_batch_eta_days` (14) | 06 §8.3 y §9 |
| Regreso (`comeback_d7`, `_d14`, `_d30`, `_d60`) | A los 7, 14, 30 y 60 días [P] | 06 §8.2 y §9, B6-Q5 |

Reglas:
1. **Horas silenciosas** (`push_quiet_hours` = 22-09, hora local del dispositivo): un aviso que cae dentro se mueve a las 09:00 [P] (B6-Q11).
2. **Un aviso por día** (`push_daily_cap_d0_d7` = 1) en todo el plan [P] (B6-Q12). Si coinciden, gana el Regreso, luego los puntuales (Crecida > canoas > nuevos niveles) y luego los diarios (Reto listo > Bitácora): la prioridad de 06 §8.2 dentro de cada grupo [P] (B6-Q9 (b)). Un puntual que pierde su día (contra el Regreso o por un tope) pasa una vez al día siguiente, a la misma hora [P]. Así, con los valores por defecto, Nuevos niveles (día 14, el del Regreso D14) sale el día 15.
3. **Sin abrir 7 días** (`push_weekly_cap_lapsed` = 3): desde el día 7, como mucho 3 por semana; los diarios, en días alternos desde el 7 [P]; el Regreso y los puntuales cuentan para el tope pero no esperan al día alterno (B6-Q10).
4. **Parar a los 30 días**, salvo el Regreso (06 §8.2).
5. **Determinista**: el mismo contexto da el mismo plan; ids estables (`(tipo + 1) × 1000 + día`).
6. El desfase local se toma fijo al planificar (un cambio de horario a mitad de plan mueve los avisos una hora) [P].
7. Un aviso sin texto en el idioma del jugador no se programa (D-018: nunca otro idioma) y no gasta el cupo del día: el planificador lo quita antes de los topes. La variante D14/D30/D60 sin traducir usa el texto del D7.
8. El Regreso no se avisa antes de `live_comeback_gift.days`: nombra el regalo (06 §8.2, «Estado real»).

`NotificationContext` lo rellena el arranque (`GameBootstrap.BuildNotificationContext`) con cada fuente única: el Reto (`DailyChallengeService` y `daily_challenge`), la Crecida (`StreakService`), las canoas (`Wallet`), el fin de contenido (el catálogo y `GameConfig`) y los nombres de mundo (`world_wNN_name`). La Bitácora queda apagada hasta B4.

## 5. Regalo de regreso (06 §9)

- Días sin sesión = periodos completos de 24 h desde `stats.last_seen_at` [P] (no días del calendario: no dependen de la zona ni del cambio de hora).
- Segmentos de 06 §9: «En riesgo» (3-6, nada), «Dormido» (`live_comeback_gift.days` = 7 a 13), «Perdido» (14-29), «Churn» (30 o más). Desde «Dormido»: 200 semillas + 1 Cuia extra al stock + canoas a su tope, todo por `Wallet.Apply`. Desde «Perdido», además, `ShowReminder` (la frase de TiTino y la etiqueta honesta del siguiente nivel; las pinta la UI).
- **Hoja escudo activa:** no se paga. La repone el cambio de día de la Crecida (`StreakRecord.RollOver`, 06 §4.3), que siempre ocurre tras 7 días fuera [P]. Lo comprueba un test.
- **Canoas:** la cartera no tiene una transacción de «llenar gratis»; se hacen `AddCanoe` hasta el tope. En la práctica casi nunca hace falta (se regeneran solas en 25 min cada una).
- **Nunca dos veces por la misma ausencia:** `SessionTracker` guarda `last_seen_at` = ahora antes de pagar, en el mismo perfil que la cartera. Si la app muere a medias, se pierde lo que faltaba, nunca se repite.
- **Anti-reloj** [P]: con la hora del servidor y un desfase mayor que `sec_clock_max_drift_h`, no hay regalo esa sesión (B6-Q15). Sin red, se acepta la hora local (08 §10.1).
- Fuente de las semillas en `earn_virtual_currency`: `event` [P] (B6-Q6). `af_comeback_gift.days_away` sale de `ComebackOutcome.DaysAway`.
- Sin Cuia de rescate ni Continuar, sin cambiar el tablero pendiente y sin misiones de pago (06 §9, «Reglas»).

## 6. Arranque, sondas y lo que falta de UI

- `GameBootstrap.SetUpSessions()` (parcial nuevo) crea los servicios con el perfil, la cartera y el reloj de `Start`, cuenta el arranque en frío y añade `SessionRunner`. El runner lleva `OnApplicationPause`, `OnApplicationQuit` y, cada fotograma, la entrada en la pantalla de victoria (campaña y Reto) para `last_win_at` y el pre-prompt.
- **Sondas** (`AF_STATE`, solo en desarrollo): `session`, `comeback_gift`, `push_prompt`, `push_plan`, `push_sent` y `push_open` (formato en `SessionProbeFormat`). El integrador puede añadirlas a `docs/dev/prototipo-v0.md`.
- **Falta UI** (fuera de B6, toca archivos del escritorio):
  - la tarjeta real del pre-prompt, después del panel de la victoria y antes de la Mochila (D-033 #11: pantalla aparte, no takeover);
  - la fila «Avisos» de Ajustes (`SettingsScreen`), con `GameBootstrap.Push.SetFromSettings`;
  - el aviso del Regalo de regreso (`GameBootstrap.LastComeback`), con `comeback_streak_line` y, desde B4, `comeback_logbook_line`, y `comeback_reminder_line`.

## 7. Parches y orden de aplicación

Aplicar con `git apply` desde la raíz, en este orden:

1. **`B6-profile.diff`**: `Profile.cs` (`SettingsData.Push` con `PushSettingsData`; `StatsData.LastSeenAt` / `LastWinAt`), `ProfileJson.cs` (escritura solo fuera del default; lectura tolerante), `LiveOps/ProfileSessionStore.cs` (`ProfileSessionStore` y `ProfilePushSettingsStore`) y `LiveOps/Tests/ProfileSessionStoreTests.cs`. Sin migración: el esquema 2 aún no se ha publicado (08 §4.3).
2. **`B6-bootstrap.diff`** (después del 1): `SetUpSessions();` en `GameBootstrap.Start`, tras `SetUpStreak();`; `UI/GameBootstrap.SessionsProfile.cs` (el gancho `CreateProfileSessionStores`); `"AF.LiveOps"` en `AF.Tests.EditMode.asmdef`; y `Tests/EditMode/GameBootstrapSessionsTests.cs`.
3. **`B6-notifications-real.diff`** (aparte; **no** entra en las pruebas generales). Antes, el integrador anota la versión en D-033. Ver la sección 8.
4. **Textos:** las filas de `B6-strings.csv` a `strings.csv`. Sin ellas los avisos no se programan, porque falta su texto.

**Choques conocidos con otros bloques:**
- `B3-ui.diff`, `B6-bootstrap.diff` y `B7-bootstrap.diff` añaden cada uno una línea justo después de `SetUpStreak();` en `GameBootstrap.cs`. El integrador lo resuelve a mano con este orden: `SetUpSkip();` (B3), después `SetUpSessions();` (B6) y después `ConnectTelemetry();` (B7).
- **Riesgo con B8** (en curso):
  - `B8-profile.diff` toca `Profile.cs` y `ProfileJson.cs` en otros puntos. Hoy aplica (`--check`) encima de B6-profile.
  - Su integridad y su fusión en la nube tienen que cubrir `settings.push` y `stats.last_seen_at` / `last_win_at`.
  - Propuesta para la fusión [P]:
    - `last_seen_at` y `last_win_at`, por máximo;
    - `pre_prompt_shown` y `second_prompt_shown`, por OR;
    - `granted`, el del dispositivo.

Lo que el integrador debería anotar en el GDD: 08 §4.3 (`settings.push.pre_prompt_at` y `second_prompt_shown`, `stats.last_seen_at` y `last_win_at`); 08 §8.2 (`result` y `origin` de `af_push_prompt`, valores de `type`); 06 §8.3 (texto con `{left}` de «Crecida te espera», textos del D14/D30/D60); 06 §8.2 (los [P] de la sección 4).

## 8. La implementación real (`B6-notifications-real.diff`)

**Estado (2026-10-22, rama `tanda5/notificaciones`): integrada** con la orden del dueño del añadido de D-033 del 2026-10-21. Lo que cambia respecto al parche:
- El programador vive en su propio ensamblado, `Scripts/Platform/Notifications/AF.Platform.Notifications.asmdef` (como los de Firebase), el único que referencia `Unity.Notifications.Android` y que lleva el `versionDefines` de `AF_MOBILE_NOTIFICATIONS` (2.4.3). AF.UI solo ve `DeviceNotifications` (`Available`, `Create`, `Poll`, `CancelAllOnDevice`), sin `#if` del paquete.
- La guarda es `AF_MOBILE_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR`: el cuerpo se compila en el build de Android (comprobado con `BuildDev`) y en el editor sigue el falso.
- La hora va en UTC (el paquete la convierte con `ToUniversalTime`), no en hora local: un cambio de zona después de programar no mueve el aviso (cierra el riesgo 5).
- `ShouldAutoCancel` = true (el aviso se quita al tocarlo); la intención ya contada se recuerda por proceso, para que la reconstrucción por idioma no repita `af_push_open`.
- **Banda <13 (B12):** al bloquear (en el arranque o al corregir la edad) se cancela todo lo programado y la bandeja (`GameBootstrap.CancelNotificationsForBlock`); el diálogo nativo nunca sale sin «Avísame». Con 13-17 y 18+ los avisos son los mismos, siempre con su opt-in: son recordatorios del estado del juego, sin datos ni anuncios.
- **Ajustes del paquete:** se quedan en sus valores por defecto (sin `ProjectSettings/NotificationsSettings.asset`): «Schedule at exact time» apagado (`exact_scheduling` = 0 en el manifiesto) y «Reschedule on Device Restart» apagado. El manifiesto fusionado del `dev.apk` solo gana `POST_NOTIFICATIONS` y el receptor `UnityNotificationManager` (no exportado); `ci/check-manifest.ps1` lo admite y hace fallar, con una pista, `SCHEDULE_EXACT_ALARM`, `USE_EXACT_ALARM`, `RECEIVE_BOOT_COMPLETED` y `REQUEST_IGNORE_BATTERY_OPTIMIZATIONS`.
- **Para probarlo en el teléfono** sin la tarjeta del pre-prompt ni la fila de Ajustes (que siguen faltando en la UI): el extra de desarrollo `af_dev_push` (`docs/dev/prototipo-v0.md`).
- Siguen pendientes: el icono pequeño de silueta y el grande de TiTino (arte; sin ellos Android usa el icono de la app) y la UI del pre-prompt y de «Avisos».

Lo que sigue es la nota original del Spark.

**Qué trae:**
- `Packages/manifest.json`: `"com.unity.mobile.notifications": "2.4.3"`.
- `Scripts/UI/AF.UI.asmdef`: la referencia `Unity.Notifications.Android` y un `versionDefines` que define `AF_MOBILE_NOTIFICATIONS` con el paquete.
- `Scripts/UI/AndroidNotificationScheduler.cs`: el `INotificationScheduler` real, todo dentro de `#if AF_MOBILE_NOTIFICATIONS && UNITY_ANDROID`. Usa un canal `af_default` [P], alarmas inexactas, el diálogo nativo con `PermissionRequest` por sondeo y la intención de apertura para `af_push_open`. Los programados se apuntan en `PlayerPrefs` para `af_push_sent`.
- `Scripts/UI/GameBootstrap.Notifications.cs`: los ganchos `CreatePlatformScheduler` (solo fuera del editor) y `TickPlatformScheduler`.

**La versión propuesta: 2.4.3.** Los documentos del repositorio no fijan ninguna (08 §7 y §15 solo nombran «Mobile Notifications»). 2.4.3 es la que el propio Editor 6000.3.25f1 instalado declara para este paquete en su lista de paquetes (`Editor/Data/Resources/PackageManager/Editor/manifest.json`: `minimumVersion` y `version` 2.4.3). Es un archivo local; no se navegó. El integrador la anota en D-033 antes de aplicar el parche, y Unity regenera `packages-lock.json`.

**`POST_NOTIFICATIONS`:** el paquete declara el permiso en el manifiesto que fusiona Gradle para el target API 36. El parche no añade un manifiesto propio. Hay que comprobarlo en el APK de desarrollo:
- `aapt2 dump permissions Builds/Android/dev.apk` (o `bundletool dump manifest` en el AAB) debe listar `android.permission.POST_NOTIFICATIONS`;
- y **no** `SCHEDULE_EXACT_ALARM`, `USE_EXACT_ALARM` ni `RECEIVE_BOOT_COMPLETED`, que no están aprobados.

**Cómo lo he dejado listo sin el paquete:**
- todo el código del paquete va dentro de `#if AF_MOBILE_NOTIFICATIONS`. Sin el paquete, el símbolo no existe, la rama compila y sigue el programador falso;
- en el worktree temporal apliqué el parche sin la línea del manifiesto: compiló, y Unity generó los `.meta` que lleva el diff;
- lo que no he podido compilar es el cuerpo del `#if`, que necesita el paquete. Está escrito contra la API de la serie 2.x.

**Riesgos:**
1. La API del cuerpo no está compilada. Si algo de 2.4.3 cambió de nombre, falla al aplicar el parche, en el editor con el target Android; se arregla allí. Las piezas que usa: `AndroidNotificationCenter.Initialize`, `RegisterNotificationChannel`, `SendNotificationWithExplicitID`, `CancelAllScheduledNotifications`, `CancelAllDisplayedNotifications`, `CheckScheduledNotificationStatus`, `GetLastNotificationIntent` y `UserPermissionToPost`, y las clases `PermissionRequest`, `AndroidNotification` y `AndroidNotificationChannel`.
2. Si el ensamblado `Unity.Notifications.Android` no compila en el editor, la guarda pasa a `&& !UNITY_EDITOR`.
3. **Ajustes del paquete** (Project Settings › Mobile Notifications, YAML: los cambia un humano en el Editor):
   - «Schedule at exact time» = `Never`, sin alarmas exactas (Q-M5);
   - «Reschedule on Device Restart» apagado, porque añadiría `RECEIVE_BOOT_COMPLETED`, que no está aprobado. Los avisos se pierden al reiniciar el teléfono hasta la siguiente salida de la app;
   - que nada pida el permiso al arrancar (06 §8.1).
4. **Icono pequeño:** sin un icono blanco de silueta registrado en esos ajustes, Android usa el de la app (puede salir como un cuadrado). El icono grande con el retrato de TiTino (06 §8.3) es arte del artista. Los dos se registran en el Editor.
5. `FireTime` se pasa en hora local (`ToLocalTime`) en el momento de programar. Un cambio de zona después mueve el aviso.
6. **Detección de entregados:** es aproximada (`CheckScheduledNotificationStatus` ≠ `Scheduled` con la hora vencida). Uno que el sistema descartó se puede contar como entregado. Solo afecta a la analítica.
7. **Compuertas:** fila de Data Safety (08 §13.3 ya la tiene: «Unity Mobile Notifications · ninguno (local)»), `ci/check-16kb.sh` sobre el APK (el paquete es Java, sin `.so` esperados) y `ci/size-gate.ps1`.
8. Con `INTERNET` sin aprobar no cambia nada: todo es local.

## 9. Preguntas para el dueño (con la recomendada, aplicada como [P])

| Id | Pregunta | Opciones | Recomendada |
|---|---|---|---|
| B6-Q1 | Dónde se miden los días sin sesión del Regalo de regreso | (a) `stats.last_seen_at` nuevo (UTC, al empezar sesión y al salir); (b) `sloth.last_seen_date` (solo fecha, D-016); (c) `economy_ts` | (a) |
| B6-Q2 | Qué guarda `settings.push` para el segundo pre-prompt | (a) `pre_prompt_at` + `second_prompt_shown`; (b) un contador `pre_prompt_count` + `pre_prompt_at`. En las dos, «rechazar» es «Ahora no» o negar el diálogo nativo, y el segundo también sale en una victoria | (a) |
| B6-Q3 | ¿Se avisa antes del «Avísame»? (Android 12 o anterior no tiene diálogo) | (a) no: solo con `granted`, y al abrir solo se baja; (b) en Android ≤ 12, desde el principio | (a) |
| B6-Q4 | «Racha de Retos» en la 1.0 (está en el catálogo de 06 §8.3 sin marca de versión, pero no en los 6 tipos de 06 §11) | (a) sí, como variante del Reto listo a su hora; (b) no en la 1.0 | (a) |
| B6-Q5 | Días del Regreso y sus textos | (a) D7, D14, D30 y D60 (06 §9, «Churn»), con D14/D30/D60 = «variante del D7 con el nombre del mundo»; (b) solo D7/D14/D30 (06 §8.2 y §11; 08 Desviación 8) | (a) |
| B6-Q6 | `source` de `earn_virtual_currency` para el regalo | (a) `event`, de la lista actual de 08 §8.2 (`af_comeback_gift` ya lo distingue); (b) añadir `comeback` a 08 §8.2 y a `SeedSource` (parche de `AF.Economy`) | (a) |
| B6-Q7 | «Crecida te espera» dice «una victoria más» aunque falten más (06 §8.2, «Estado real») | (a) texto con `{left}` y plural; (b) avisar solo cuando falta 1 (racha 5 o 9); (c) el texto literal | (a) |
| B6-Q8 | Título de los avisos y nombre del canal de Android | (a) «TiTino» y «Avisos del río»; (b) el nombre del juego (tras el dictamen de marca) | (a) |
| B6-Q9 | Con 1 aviso al día y la prioridad literal de 06 §8.2, el Reto listo de cada día (prioridad mayor) haría que **Nuevos niveles no saliera nunca** y que Crecida te espera casi nunca saliera (tipos de la 1.0, 06 §11; 08 Desviación 8) | (a) literal del GDD (esos avisos se pierden); (b) los puntuales ganan a los diarios el día que caen y, si pierden contra el Regreso o un tope, pasan una vez al día siguiente; (c) entender «coinciden» como la misma franja horaria | (b) (cambiada tras la revisión) |
| B6-Q10 | Reparto de los 3 por semana sin abrir | (a) días alternos desde el 7, el Regreso incluido; (b) días fijos (7, 10, 13…) | (a) |
| B6-Q11 | Un aviso que cae en las horas silenciosas | (a) se mueve a las 09:00; (b) se descarta | (a) |
| B6-Q12 | ¿`push_daily_cap_d0_d7` vale después del día 7 desde la instalación? | (a) sí, 1 al día siempre («igual a la frecuencia de apertura»); (b) sin tope diario desde el día 8 | (a) |
| B6-Q13 | Hora de «Reto listo» (franja 09:30-11:00) | (a) 09:30, y hoy nada si ya pasó; (b) una hora al azar de la franja (rompe el determinismo) | (a) |
| B6-Q14 | «Crecida te espera» cuando ya pasaron las 36 h al salir | (a) nada; (b) avisar pronto | (a) |
| B6-Q15 | Anti-reloj del regalo | (a) con la hora del servidor y un desfase > `sec_clock_max_drift_h`, sin regalo esa sesión, y **ese regalo se pierde** (`last_seen_at` ya avanzó); sin red, adelantar el reloj 7 días y volverlo a poner repite el regalo; (b) dejarlo «pendiente de validar» como el Reto (08 §10.1), con un campo más en el perfil; (c) sin comprobación | (a) |
| B6-Q16 | Cuándo sale la tarjeta del pre-prompt | (a) tras el panel de la victoria, al pulsar Siguiente o Inicio, como pantalla aparte (D-033 #11) y antes de la Mochila (06 §8.1); el disparador de `TickSessions` (hoy, la entrada a la victoria) se mueve allí con la tarjeta; (b) dentro del panel | (a) (UI fuera de B6) |
| B6-Q17 | Valores de `af_push_prompt` | (a) `result` ∈ {`accepted`, `declined`, `granted`, `denied`} y `origin` ∈ {`first`, `second`, `settings`}; (b) solo `step` y `result` | (a) |
| B6-Q18 | Valores de `type` de `af_push_sent` / `af_push_open` | (a) los de la sección 4 (`daily_ready`…`comeback_d60`); (b) otros | (a) |

## 10. Verificación

Los comandos y sus resultados están en el informe del bloque (entrada de `spark-estado.md`, la escribe el integrador).
