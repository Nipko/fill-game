# Plan de la tanda 5: qué le falta a la 1.0 (auditoría S1)

Encargo: `docs/dev/traspaso/spark-reto-del-rio.md` §11, S1 (equipo Spark, 2026-10-20). Rama `spark/auditoria-1-0`.

Auditado el código de `origin/integracion-fase1-fase2` en `a759597`, contra el GDD (`docs/GDD.md` §2, §6, §7 y `docs/gdd/00-09`) y `docs/dev/decisiones.md` (hasta D-031).

- **Método:** cuatro auditorías de solo lectura, una por área: núcleo y partida; meta y liveops; economía y monetización; técnica y publicación.
- **Evidencia:** la implementación y los tests, no los comentarios.
- **Recuentos de tests:** los de la integración (EditMode 3.057, PlayMode 1.383, dotnet 981). En la auditoría no se ejecutó ninguno.
- **Comprobación ejecutada:** `econ_sim.py --runs 300` frente a `tools/out/econ_B.md`, idéntico.

**Leyenda de estado:** HECHA · A MEDIAS · SIN EMPEZAR · 1.1 (fuera de la 1.0).

**Leyenda de bloqueo:**
- **Ya:** se puede hacer con el canon.
- **Dueño:** cuentas, permisos, SDK, decisiones, teléfono de pruebas.
- **Terceros:** artista, asesor cultural, abogado.

**Rutas:** `S/` = `Assets/_Project/Scripts/`; `T/` = `Assets/_Project/Tests/`; `Core/` = `Packages/com.amazonfill.core/Runtime/`.

---

## 1. Resumen

- **El núcleo está completo:**
  - reglas R1-R16;
  - las 8 mecánicas y las 4 variantes de la 1.0;
  - Aguas turbias y la Tinaja (aún sin publicar);
  - solver, bot, generador y QA;
  - 700 niveles en los mundos 1-9;
  - la presentación y el audio.
- **También está completo el meta alrededor del Mapa del río:** TiTino, fauna, Frutas del río, Muestrario, banner de mundo y el Reto del río.
- **Está hecha y probada toda la lógica de cliente del embudo de D-008:** cartera, canoas, Marea, Cuia extra, Morpho, Luciérnaga, Cuia de rescate, más anuncios y compras simulados.
- **Faltan siete sistemas de la 1.0 cuyo canon está cerrado** (se pueden hacer ya):
  - Crecida + Hoja escudo;
  - Canasta;
  - Bitácora del río;
  - Pororoca;
  - Salto del Boto;
  - notificaciones (solo la lógica);
  - regalo de regreso.
- **Falta la capa de tienda y ofertas:** Mercado, Mochila de inicio, tarjeta Sin anuncios, Cántaro y `offer_scheduler`.
- **Falta la base técnica:**
  - `AF.RemoteConfig`: solo hay defaults compilados; unas 130 claves de 08 §9 no existen;
  - `AF.Analytics`: 0 eventos reales; unos 15 de los ~70 de 08 §8.2 existen como sonda;
  - nube y fusión, integridad del perfil;
  - consentimiento y edad;
  - accesibilidad (TalkBack, escala de fuente del sistema, Reducir movimiento, Alto contraste);
  - compuertas de release.
- **Todo lo «online» real está bloqueado por el dueño:**
  - MAX y UMP;
  - Unity IAP;
  - Firebase (RC, Analytics, Auth, Functions, Crashlytics, App Check);
  - Play Games;
  - el permiso `INTERNET`;
  - Addressables y PAD;
  - los secretos del CI;
  - el keystore;
  - la marca y el `applicationId`.

---

## 2. Piezas de la 1.0 y su estado

### 2.1 Núcleo, partida y contenido (02, 03, 07)

| Pieza | § | Estado | Evidencia | Qué falta | Bloqueo |
|---|---|---|---|---|---|
| Reglas R1-R16, `run`/`single`, R10 por etapas, sello, `futil`, D-014, C1-9 | 02 §1 | HECHA | `Core/AF.Core/Rules/*`, `Session/DeadEndProbe.cs`; `PourRulesTests`, `R10OrderTests`, `FutileRulesTests`, `DeadEndProbeTests`, `LianaTests` | — | — |
| Capacidades chica/alta 5/6 | 02 §8 | HECHA | packs 14/18/22/27/160; `BoardLayoutTests` | — | — |
| 8 mecánicas y 4 variantes | 02 §9.1 | HECHA | `Core/Mechanics/*`, `S/Presentation/Mechanics/*Overlay.cs`; tests por mecánica | — | — |
| Aguas turbias | 02 §11 | HECHA | `Core/AF.Solver/Murky/*`, `RevealBooster.cs`; `MurkyValidationTests`, `RevealFlowTests` | — | — |
| Tinaja (M13) | 02 §9.1 | A MEDIAS | `TinajaMechanic`, `GameScreen.Tinaja.cs`; `Tinaja323EndToEndTests` | Publicarla: el 323 sale con la variante b | Dueño (PF1, teléfono de clase A) |
| Solver, bot ×40, generador, QA | 03 §2-§10, §17 | HECHA | `Core/AF.Solver/**`, `tools/level-*`; `tools/out/level_qa_report.md` (11-200: 190 niveles, 0 fallos); 201-700, la compuerta de `level-qa` de `tools/out/curation_w06.md`-`curation_w09.md` | CI de reproducibilidad (#10) | Dueño (CI) |
| Campaña 1-700 | 03 §11, §17 | A MEDIAS | `Data/Levels/*` | Curación a mano de 11-200 (#12-#13); QA humana externa de 201-700 (~40 h; 09 §1.1 #7); reserva del 20 % de 03 §17 («1.040 niveles + 20 % reserva»), que es una tarea de generación | Curación y QA humana: dueño. Reserva: ya (B11) |
| Ciclo de 20, etiquetas, temporadas | 03 §11-§12 | HECHA | `SeasonTable.cs`; QA-LADDER, QA-LABEL | — | — |
| `lvl_label_override` | 02 §10; 03 §7 | SIN EMPEZAR | — | Lector local y su aplicación | Ya |
| Kill switch `mech_enabled` → `level_overrides` | 02 §9.2; GDD.md §6.3 #31; D-020 | A MEDIAS | `LevelOverrides.cs`; `LevelOverridesTests` | `level_overrides` reales por RC (en la 1.0 el respaldo es `level_overrides`; el formato por slot llega con la 1.1); la reserva del 20 %, en la fila de la campaña | Dueño (Firebase) |
| Alivio «El río bajó» | 03 §13 | A MEDIAS | `FailResolve.ChooseRelief`; `FailResolveTests` | Caso V3: Cuia extra con `relief` solo `plus_empty` (18 niveles) | Q-N2 |
| DDA | 03 §14 | 1.1 (apagada) | `lvl_dda_enabled` = false | — | — |
| Salto del Boto | 02 §2, §7; 04 §2.6 | SIN EMPEZAR | Solo campos (`since_last_skip`, `skipped_levels`, `Skippable`, `skip_seed_price`) | Elegibilidad, rama D del modal, pausa, banner, Rosa en el Mapa, claves `skip_min_*` | Ya |
| Pororoca | 04 §2.4; 03 §16 | SIN EMPEZAR | Ranura `TraySlot.Shuffle`, stock (el Reto ya la regala) | Reconstrucción inversa + A* (AF.Solver), comodín, topes, ola de 1,2 s, tarjeta del 30 | Ya |
| Máquina de estados y embudo de D-008 | 02 §2, §7 | HECHA (sin el Salto) | `LevelFlow*.cs`, `StuckModal.cs`, `LevelFailedScreen.cs`, `NoCanoesScreen.cs`; `StuckModalTests`, `FailFunnelTests` | Pantalla «Cargando…» (packs remotos) | Dueño (Addressables) |
| Guardado por vertido y reanudar | 02 §2, §14 | HECHA | `LevelFlow.Resume.cs`, `ProfileWriter.cs`; `SaveGuardTests`, `LevelStateTests` | — | — |
| Toque, encolado, C1-C8 | 02 §3 | HECHA | `TapController.cs`, `BoardAnimator.cs`; `TapControllerTests` | — | — |
| C9: mantener pulsado para ver los nombres | 02 §3.1, §12 | SIN EMPEZAR | `TapController.cs:14` lo deja fuera | Pulsación de 400 ms | Ya |
| C10: arrastre (`drag_pour_enabled`) | 02 §3.1; GDD §2 #3 | SIN EMPEZAR | — | Arrastre de ≥ 24 dp | Ya |
| Pista por inactividad, animaciones, Llenar y teñir | 02 §3.4, §4 | HECHA | `IdleHint.cs`, `BoardAnimator`, `IntroChoreography`; tests | `anim_speed_factor_late` vale 1,0 en el código y 0,8 en 08 §9 | Q-N1 |
| Shader, chorro, audio, hápticos, orientación | 02 §4-§5; 07 §5, §9 | HECHA | `AF_UI_Liquid.shader`, `PourStream.cs`, `S/Audio/*`; `LiquidVolumeTests`, `AudioClipsTests` | Medida en gama baja; escucha en el teléfono | Dueño; asesor (audio) |
| HUD y filas | 02 §5 | A MEDIAS | `BoardLayout.cs`; `BoardLayoutTests` | 16:9 con 13-14 cuias en 3 filas (DC18); texto al 82 % (DC3); test a 320 dp | DC18/DC3: **escritorio**. 320 dp: ya |
| Victoria | 02 §6 | HECHA (sin Canasta ni Crecida) | `GameScreen.Victory.cs`, `VictoryLineArbiter.cs`; `VictoryTests` | Barra de la Canasta | Ver §2.2 |
| Difícil y Muy difícil | 02 §10; 07 §7.7 | A MEDIAS | `GameScreen.Labels.cs`, `LevelCardView.cs` | Noche con LUT y luz de tarde [P] | Ya, pero toca `StopBackdrop` (**escritorio**): B14 ✱ |
| Tutorial 1-12 | 02 §13 | A MEDIAS | `TutorialInstruction.cs`, `TutorialFinger.cs`; `TutorialFingerTests`, `DebutFingerTests` | Ver el detalle debajo | Ya (textos [P] del asesor) |
| Tarjetas de estreno | 02 §8, §9.1 | HECHA | `tutorial_*` en `strings.csv` | Textos del asesor; traducciones (T2) | Asesor |
| Patrones de fruta | 02 §12; 07 §3 | HECHA | `Palette.PatternOpacityFor`; `FruitPatternsTests` | — | — |
| Alto contraste / modo daltónico | 02 §12; 07 §3 | SIN EMPEZAR | — | Ajuste y opacidad | Q-N4 |
| QA con simulador de daltonismo | 07 §7.8; 08 §14.3 | SIN EMPEZAR | — | Test con un simulador de los tres tipos sobre el shader real; las matrices de Machado son una propuesta [P] (el GDD no fija el método) | Ya (B9) |
| Reducir movimiento | 02 §4, §12 | A MEDIAS | `LevelFlow.ReduceMotion` (fontanería) | Ajuste, `settings.reduce_motion`, `ui_reduce_motion_default`, escala de animación de Android | Ya |
| Escala de fuente del sistema (≤ 130 %) | 02 §12 | A MEDIAS | `PseudoLocOverflowTests` al 130 %; solo se activa con un extra de desarrollo | Leer `fontScale` de Android | Ya |
| Lector de pantalla (TalkBack) | 02 §12; 07 §7.8 | SIN EMPEZAR | El módulo `com.unity.modules.accessibility` ya está en el manifiesto | Etiquetas, foco, anunciar la selección | Ya: 02 §12 lo fija completo (cuias, botones y selección); lo prueba el dueño |
| Elenco de unas 50 especies | 07 §6.4; GDD §2 #22 | SIN EMPEZAR | 11 miquitos (`miquitos_v0.json`) | Diseño, datos y rigs | Artista y asesor (Q-N6) |

Detalle del tutorial 1-12 (02 §13), lo que falta:
- el dedo del nivel 2 no sale en el teléfono (`pruebas-telefono-2026-10-20.md`, observación 3);
- el dedo guía en el primer uso de la Cuia extra del 8;
- el tooltip «Reiniciar es gratis…» del 6 y del 8;
- la tarjeta del Reto tras el 7 (T1/E6, en integración);
- la luz del alba y la mañana del 9 [P];
- la tarjeta de Canoas del 31 (Q-E4).

### 2.2 Meta, retención y liveops (06, 07)

| Pieza | § | Estado | Evidencia | Qué falta | Bloqueo |
|---|---|---|---|---|---|
| Pantalla de arranque con TiTino | 06 §2.2 | HECHA | `BootSplash.cs`; `BootSplashTests` | Arte final | Artista |
| Primera sesión 1-12 | 06 §2.2; 02 §13 | HECHA (huecos del tutorial, en §2.1) | `TutorialFinger`, `TutorialMorphoTests` | Tooltip de la Cosecha doble en el 11 | Ya |
| Secuencia de la victoria del 10 | 06 §2.3 | A MEDIAS | `L10SequenceRule`; `Level10MapTests` | Pantalla 2 (Canasta + Bitácora); tarjetas Bitácora y Mercado | Depende de la Canasta y la Bitácora (Q-M1, Q-M2) |
| Inicio | 06 §4.7; D-008 | A MEDIAS | `LobbyScreen.cs`, `HomeWindow.cs`, tarjeta del Reto; `LobbyTests` | Icono del Mercado (B5); tarjeta Bitácora y cola de un takeover por sesión, `ui_max_takeovers_per_session` (B4); widget de la Crecida (B1b) | Ya (choca con `AppFlow` y el inicio) |
| Mapa del río | 06 §4.5; D-013 | HECHA (con huecos) | `RiverMapScreen*.cs`, `RiverJourney.cs`; `RiverMapScreenTests`, `MapCatchUpTests`… | Vista previa de la Canasta en las paradas futuras y marca en las cobradas; «Premio del mundo · faltan N»; tocar un punto de paso para leer su dato | Ya, en B14 ✱ (la Canasta, tras B1a) |
| Fin de contenido en el 700 | 06 §9-§10 | A MEDIAS | `AppScreen.ContentEnd`; `ContentEndMapTests` | Frase de TiTino; entrada al Reto | **Escritorio** (en curso) |
| Banner de mundo, Muestrario, fauna, TiTino | D-012, D-016, D-029 | HECHA | `WorldBannerView`, `RiverMapScreen.Samples.cs`, `SightingRule.cs`, `SlothLines.cs`; tests | TiTino recibe `streak = 0` (falta la Crecida); arte; asesor | Artista y asesor |
| Frutas del río | 06 §4.4-§4.5; D-017 | A MEDIAS | `FactSchedule.cs`, `FruitSheet.cs`; `FactScheduleTests` | Líneas 17-20 del cacao y el arazá; 321-700 sin línea (Q-M7); `album.fruits` (08 §4.3) y `af_fruit_discovered` (08 §8.2) | Textos: asesor; lo demás, ya (B14) |
| Reto del río | 06 §4.1; D-031 | HECHA | `AF.LiveOps/*`, `DailyChallengeScreen.cs`; unos 324 tests | E6/E7 (T1, en integración); hora del servidor y RC reales | Firebase (dueño) |
| **Crecida + Hoja escudo** | 06 §4.3; 05 §2.4; 04 §8 | SIN EMPEZAR | Restos: `DailyData.ShieldAvailable`, `sfx_streak_*`, banco `streak` de TiTino | Reglas (3/6/10, una vez al día, tope de 600, río crecido, escudo diario), su estado en el perfil (Q-M9), Cosecha doble sin vídeo, `streak_*`, `af_streak_*` (B1a); barra del HUD y réplica en la victoria, widget del inicio, línea del escudo en el modal, tarjeta del 25 (B1b) | Ya (B1a); la interfaz, Q-M2 y el escritorio (B1b) |
| **Canasta** | 06 §4.4; 05 §2.1 | SIN EMPEZAR | `Transaction.Basket`, `sfx_basket_open` | Reglas (`eco_basket_cycle`, `eco_basket_seeds`, Canasta grande cada 100: 150 semillas + 1 Cuia extra + 1 Morpho + 3 Marea, GDD.md §6.1 #13) y `af_basket_open` (B1a); pantalla de abrir y barra «Canasta · {sitio} k/10» (B1b); vista previa en el Mapa (B14) | Ya (B1a); la interfaz, Q-M2 y el escritorio (B1b) |
| **Bitácora del río** | 06 §4.2; 05 §2.3 | SIN EMPEZAR | Pestaña oculta en `LobbyScreen` | Reglas (`eco_calendar_table_v1`), `profile.calendar` (Q-M3), libreta 5×6, tarjeta con punto, takeover, `af_calendar_claim` | Ya (Q-M1) |
| Notificaciones locales y pre-prompt | 06 §8; 08 §2.2 | SIN EMPEZAR | No hay paquete ni código | Planificador puro + implementación falsa; pre-prompt; `settings.push` | Lógica: ya. Paquete y `POST_NOTIFICATIONS`: dueño (Q-M5) |
| Sesiones (`stats.sessions`) | 06 §2.1, §8.1 | A MEDIAS | Se lee y escribe, pero nadie lo incrementa | Contarlas | Q-M4 |
| Regalo de regreso | 06 §9 | SIN EMPEZAR | Solo el saludo de TiTino | 200 semillas, Cuia extra, canoas 5/5, Hoja escudo; segmentos 7/14/30; `live_comeback_gift` | Ya |
| Skins de cuia por hito | 06 §4.5; 07 §4.6 | A MEDIAS (solo datos) | `StoreCatalog`, `entitlements.skins` | Pintarlas en `CuiaView`, ganarlas en 40/80/140, elegirlas (B14); tarjeta de oferta (B5) | Arte final: artista. Provisional: ya (B14; Q-M8) |
| Ajustes | 02 §12 | A MEDIAS | `SettingsScreen*.cs`; `SettingsScreenTests` | Notificaciones, Eliminar cuenta, Créditos, consentimiento, Reducir movimiento, Alto contraste | Varias (ver sus filas) |
| Créditos | 07 §8.2 | SIN EMPEZAR | — | Pantalla con las licencias (fuentes OFL, Natural Earth, HydroRIVERS) | Licencias: ya. Nombres: dueño |
| «Sobre la Amazonía» | 07 §8.2 | A MEDIAS | `SettingsScreen.About.cs` (solo Frutas) | Datos del río con su fuente | Ya (B14); textos: asesor |
| Eventos, liga, Cuaderno de campo, logros | 06 §5-§6, §11 | 1.1-1.3 | — | — | — |

### 2.3 Economía y monetización (04, 05)

| Pieza | § | Estado | Evidencia | Qué falta | Bloqueo |
|---|---|---|---|---|---|
| Cartera, transacciones, `economy_ts` | 05 §1-2; 04 §10.1 | HECHA | `Wallet.cs`, `Transaction.cs`; `WalletTests` | `applied_purchases` | Ya |
| Cosecha y Cosecha doble | 05 §2.1; 04 §2.8 | HECHA (anuncio simulado) | `LevelFlow.Victory.cs`; `VictoryTests`, `DailyDoubleHarvestTests` | Cosecha doble sin vídeo con el río crecido | Crecida |
| Marea, Cuia extra, Morpho, Luciérnaga | 04 §2.1-§2.3, §2.5 | HECHA | `S/Boosters/*`; `BoosterTests`, `MorphoBoosterTests`, `RevealFlowTests` | Revocación por SSV | Backend |
| Pororoca y Salto del Boto | 04 §2.4, §2.6 | SIN EMPEZAR | Ver §2.1 | — | Ya |
| Embudo de D-008 y Cuia de rescate | 04 §6; 05 §5 | HECHA (IAP simulado) | `StuckModalModel.cs`, `RescueBooster.cs`; `StuckModalTests`, `StuckFunnelTests` | Línea del escudo; rama D; el geo fijo en T3 | Q-E3 |
| Canoas | 04 §7.1 | HECHA | `Canoes.cs`; `CanoesTests` | Contraste con la hora del servidor (`IServerDate` falso ya existe) | Ya con la implementación falsa |
| Sin canoas | 04 §7.3 | A MEDIAS | `NoCanoesScreen.cs` | «Jugar el Reto» (T1/E7, en integración); «Volver al Mapa» | Ya |
| Mercado | 05 §4; 06 §4.7 | SIN EMPEZAR | Pestaña oculta; `StoreSheet` es la hoja simulada | Pantalla con semillas, Mochila, Sin anuncios, Cántaro, Canoas 1 h | Ya (las skins esperan al arte, Q-E6) |
| Catálogo de 13 SKU | 05 §4.1 | HECHA (simulada) | `StoreCatalog.cs`, `StoreLedger.cs`; `FakeStoreTests` | Adaptador real | Dueño (Unity IAP, Play Console, `INTERNET`) |
| Mochila, Sin anuncios, Cántaro | 05 §4.1, §5 | A MEDIAS (solo la compra) | `FakeStoreTests.Starter_/NoAds_/Piggy_` | Ventana 10-20 y temporizador; tarjeta tras el 3.er intersticial; llenado por victoria e icono del HUD | Ya |
| `offer_scheduler` | 05 §5 | SIN EMPEZAR | — | Una oferta por sesión fuera del bloqueo | Ya |
| Intersticiales y recompensados | 05 §6 | HECHA (simulada) | `AdPolicy.cs`, `AdLedger.cs`; `AdPolicyTests` | `ads_rv_max_wait_fill_ms`; MAX real | Dueño |
| MAX, UMP, edad, PIN de compras | 05 §6.3, §7; 08 §2.3 | SIN EMPEZAR | Solo `IAdService` | Todo | Dueño (SDK); abogado (textos) |
| Precios por región | 05 §4.2 | HECHA (parcial) | `StorePrices`; `FakeStoreTests` | CLP, ARS, EC, EUR (fuera del soft launch) | — |
| Verificación de recibos y restaurar | 04 §10.2; 05 §4.1 | SIN EMPEZAR | — | Todo | Firebase, IAP (Q-E5) |
| Anti-fraude en cliente | 04 §10.2 | SIN EMPEZAR | — | Revocación local y reloj con implementación falsa | Ya (SSV real: backend) |
| Analítica de economía | 04 §11; 08 §8.2 | SIN EMPEZAR | Los ids existen (`EconomyIds`) | Ver §2.4 (AF.Analytics) | Ya (envío real: Firebase) |

### 2.4 Técnica y publicación (08, 09)

| Pieza | § | Estado | Evidencia | Qué falta | Bloqueo |
|---|---|---|---|---|---|
| Núcleo y grafo de asmdef | 08 §2.1-2.2 | HECHA | 16 asmdef de runtime (28 con los de tests y de Editor); tests de asmdef | — | — |
| `AF.Events` | 08 §2.1 p.3 | SIN EMPEZAR (se usan interfaces de escucha) | `ILevelFlowListener` y similares | Bus o ratificar las interfaces | Q-T1 |
| `AF.RemoteConfig` | 08 §2.2, §9 | A MEDIAS | 78 claves con `[RemoteConfigKey]` en 6 configuraciones (`AdsConfig`, `BoosterConfig`, `EconomyConfig`, `StoreConfig`, `DailyChallengeConfig`, `ClockSecurityConfig`), con tests contra 08 §9; unas 45 constantes sin atributo | Módulo, `RemoteConfigDefaults.json`, `RC.Get<T>`, congelación por pantalla, proveedor falso; unas 130 claves | Ya; el fetch, Firebase |
| `AF.Analytics` | 08 §8; 09 §1.1 #30 | SIN EMPEZAR (solo sondas) | `DevProbe*.cs`, `DailyProbeFormat.cs` | Módulo, sumidero, parámetros comunes, eventos de nivel, embudo, comodines, economía, anuncios, IAP, guardado, rendimiento y errores; corte por consentimiento | Ya; el envío, Firebase |
| Guardado local y migraciones | 08 §4.4, §11 | HECHA | `LocalProfileStore.cs`, `ProfileMigrations.cs`; `LocalProfileStoreTests`, `ProfileMigrationsTests` | — | — |
| Campos del perfil de 08 §4.3 | 08 §4.3 | A MEDIAS | `Profile.cs` | `uid`, `consent`, `calendar`, `piggy`, `album`, `applied_purchases`, `integrity`, `locale`, `first_app_version`, `country_first`, `settings.push` | Ya, cada uno con su sistema; `locale`, `first_app_version` y `country_first`, en B8 |
| Nube (PGS) y Auth | 08 §11 | SIN EMPEZAR | — | Fusión pura con sus 8 casos, `ICloudSave` con implementación falsa | Fusión: ya. PGS y Auth: dueño |
| Eliminar cuenta | 08 §11 | SIN EMPEZAR | `IProfileStore.Delete()` | Ajustes en 2 pasos, `deleteAccount` | Local: ya (B8). Resto: Functions y abogado |
| Cloud Functions (5) | 08 §2.4 | SIN EMPEZAR | — | — | Dueño (Firebase, npm, Play API) |
| Anti-reloj | 08 §10.1 | A MEDIAS | `IServerDate`/`FakeServerDate` (solo el Reto) | Canoas y topes diarios | Ya con la implementación falsa |
| App Check / Play Integrity | 08 §10.1 | SIN EMPEZAR | — | — | Dueño |
| HMAC del perfil | 08 §4.3, §10.1 | SIN EMPEZAR | — | `integrity`, detectar saltos | Ya: 08 §4.3 fija `hmac` con clave derivada del dispositivo (B8) |
| Localización | 08 §4.7 | A MEDIAS | `Loc*.cs`; `LocTablesTests`, `PseudoLocOverflowTests` | Traducciones (T2 entregada); `LocReleaseGate` sin `revisado_nativo` (parche de T2); Unity Localization (Q-T2) | Revisión nativa y asesor |
| Addressables, PAD y presupuesto del AAB | 08 §6 | SIN EMPEZAR (el tamaño sí se mide) | `AabSizeReport.cs`; AAB del juego de 51,87 MB (`docs/dev/traspaso/README.md:36`) | Grupos y packs remotos | Dueño (espacio del AAB) |
| Rendimiento | 08 §5.4, §14.2 | A MEDIAS | `targetFrameRate = 60`, `OnDemandRendering` en el Mapa, sondas `AF_PERF` | `OnDemandRendering` en el Mercado, `af_perf_sample`, prueba nocturna de fps p5 ≥ 55 | Teléfono de clase A (dueño) |
| Stripping y APIs | 08 §5.4 | A MEDIAS | `ProjectSetup.cs`; `managedStrippingLevel` por defecto | Stripping High con `link.xml`; GLES3 frente a Vulkan | Ya (Q-T6) |
| Compuertas de CI | 08 §12.1 | A MEDIAS | `size-gate.ps1`, `check-16kb.sh`, `BuildSmoke`/`BuildDev` | Comprobar el manifiesto fusionado (target/min, permisos, billing, `appCategory = Game`); que `com.amazonfill.dev` no llegue a release; `BuildRelease`; retroceso predictivo y *safe area* (08 §13.1 #4) | Ya (B10) |
| Workflows s30/s32 | 08 §12.1 | SIN EMPEZAR | No existe `.github/` | `ci.yml` (s30), `unity.yml` (s32) | s30: Q-T5. s32: secretos (dueño) |
| Firma, release, `applicationId` | 08 §13.1; regla dura 6 | SIN EMPEZAR | — | — | Dueño y abogado |
| Ficha de Play, privacidad, Data Safety, IARC, `LICENSES.md` | 08 §13 | SIN EMPEZAR | — | Borradores de la ficha, Data Safety e IARC; política de privacidad, página web de eliminación de cuenta y `app-ads.txt` (08 §13.1 #2) | Borradores: ya (B13). Firma: abogado y dominio |
| Assets de tienda (icono, gráfico, capturas, vídeo) | 07 §10; 08 §13.1 #5 | SIN EMPEZAR | — | Arte, y las especificaciones y textos de las capturas | Arte: artista. Especificaciones y textos: ya (B13) |
| Lista de regresión de release (60 casos) | 08 §14.3 | SIN EMPEZAR | — | Redactar los casos | Ya (B13) |
| Consentimiento y edad | 08 §2.3, §7; 05 §7 | SIN EMPEZAR | — | Pantalla de edad neutral, bandas, menores de 13, consentimiento por país, flags | Lógica y pantallas: ya (textos [P]). Legal: abogado. UMP: dueño (Q-T3, Q-T4) |
| Crashlytics | 08 §7 | SIN EMPEZAR | — | — | Dueño |
| Kill switches y versión mínima | 08 §9 `biz_*` | A MEDIAS | 5 `biz_*` | `biz_maintenance_*`, `biz_min_app_version`, el resto de `biz_feature_*` | Ya, tras RC |
| Cobertura ≥ 80 % (F1) | 09 §2.3 | SIN EMPEZAR | No se mide | coverlet | Q-T7 |
| KPIs y panel | 09 §6 | SIN EMPEZAR | — | — | Dueño (Firebase, BigQuery) |

---

## 3. Bloqueado por el dueño o por terceros

| Bloqueo | Quién | Qué frena |
|---|---|---|
| Permiso `INTERNET` y aprobación de los SDK | Dueño | MAX y adaptadores, UMP, Unity IAP 5.4.4, Firebase (RC, Analytics, Auth, Functions, Crashlytics, App Check), Play Games |
| Cuentas: AppLovin, AdMob, Play Console, Firebase, PGS | Dueño | Todo lo «online» real; Functions (`rewardedSsv`, `verifyPurchase`, `restoreLedger`, `voidedPurchasesJob`, `deleteAccount`) |
| Espacio del AAB (Addressables y PAD) | Dueño | Packs 701+, fondos remotos, Unity Localization (Q-T2) |
| Secretos del CI (s31) y keystore | Dueño | Workflow s32, firma, build de release |
| Dictamen de marca y `applicationId` | Abogado y dueño | Pista interna de Play, build de release |
| Textos legales: consentimiento, edad, privacidad, Data Safety | Abogado | Versión final del consentimiento y de la ficha |
| Paquete `com.unity.mobile.notifications` y permiso `POST_NOTIFICATIONS` | Dueño (Q-M5) | Push nativo |
| Teléfono de clase A | Dueño | Tinaja del 323 (PF1), medidas de fps y memoria, TalkBack |
| Arte final | Artista | Skins definitivas, TiTino, fondos, elenco de especies, assets de tienda (07 §10; 08 §13.1 #5) |
| Revisión cultural y nativa | Asesor y revisores nativos | Nombres y textos `pendiente_asesor`, frases de fruta (retenidas en T2), release en `en` y `pt-BR` |
| Curación a mano de 11-200 | Dueño o tester | Comprobaciones #12-#13 de 03 §17 |
| QA humana externa de 201-700 (~40 h) | Dueño (contratarla) | Publicar 201-700: 09 §1.1 #7 la pide además de la QA automática (03 §17) |
| `level_overrides` reales por RC | Dueño (Firebase) | El kill switch de mecánicas en producción (GDD.md §6.3 #31; D-020) |

---

## 4. Tanda 5: bloques que se pueden hacer ya

**Archivos que el escritorio toca ahora:**
- el lienzo 16:9 en `GameScreen*.cs` y `BoardLayout.cs`;
- los fondos en `StopBackdrop`;
- las traducciones en `strings.csv`;
- el final del 700 en el Mapa (`RiverMapScreen*.cs`) y en `AppFlow.cs`.

En cada bloque, ✱ marca un choque con esos archivos. Lo que choca se entrega como parche probado en `docs/dev/traspaso/spark-enganches/`; los textos nuevos, en un CSV aparte.

| # | Bloque | Días | Tareas | Archivos | Depende de | Choques |
|---|---|---|---|---|---|---|
| **B1a** | **Crecida + Hoja escudo + Canasta: reglas, perfil, RC, analítica como sonda y tests** | 2 | 1. Reglas sin motor de la Crecida (06 §4.3; 05 §2.4; 04 §8): `on_level_end` con victoria y fallo duro, hitos 3/6/10 pagados una vez por día local, tope de 600, río crecido, Hoja escudo diaria; Cosecha doble sin vídeo con el río crecido. 2. Reglas de la Canasta (06 §4.4): cada 10 niveles, `eco_basket_seeds` + `eco_basket_cycle`, Canasta grande cada 100 (150 semillas + 1 Cuia extra + 1 Morpho + 3 Marea, GDD.md §6.1 #13), contenido determinista y visible, pago con `Wallet.Apply`. 3. El estado en el perfil según Q-M9 (sin bloque `basket`: ver Q-M9). 4. Las claves `streak_*` y `eco_basket_*` de 08 §9 con `[RemoteConfigKey]` y su test contra 08 §9. 5. `af_streak_milestone`, `af_streak_shield_used` y `af_basket_open` (08 §8.2) como sonda de logcat (el sumidero real llega con B7). 6. TiTino con la racha real (`SlothContext`). | Nuevos, sin tocar archivos del escritorio: la Canasta en AF.Economy (`S/Economy/Basket*.cs`, 08 §2.2); la Crecida donde diga 08 §2.2, que la nombra en AF.Economy y en AF.LiveOps (Q-M10; recomendado `S/Economy/Streak*.cs`); tests EditMode. Ganchos como parches probados: `FailResolve.cs`, `LevelFlow.Victory.cs`, `Profile.cs`/`ProfileJson.cs`, `SlothContext.cs` | Q-M9, Q-M10 (las dos con recomendada) | Ninguno |
| **B1b** | **Crecida y Canasta: la interfaz** | 2 | 1. Barra vertical de la Crecida (02 §5; 07 §7.4: x = 348-356, y = 120-560), que aparece en T, al habilitarse el toque, y cuya franja puede pisar la tropa lateral (D-030, pregunta 4 (c); D-021). 2. Réplica en la victoria, barra «Canasta · {sitio} k/10» y pantalla de abrir la Canasta: dependen del panel único de premios del día (D-029 #18; Q-M2). 3. Línea del escudo en el modal (`StuckModalModel`/`StuckModal`). 4. Tarjeta de la Crecida en el 25 (04 §4). 5. Widget del inicio (06 §4.7). | Nuevos: `S/UI/StreakBar.cs`, `S/UI/BasketOpen*.cs`, tests. Parches cuando el escritorio libere `GameScreen`: `GameScreen*` ✱, `StuckModal*`, `LobbyScreen.cs`; textos en un CSV aparte (`strings.csv` ✱) | B1a; Q-M2 | `GameScreen` ✱, `strings.csv` ✱ |
| B2 | Pororoca | 3 | 1. Reconstrucción inversa + A* (`shuffle_par_slack` = 2) y respaldo de permutar, en AF.Solver. 2. Comodín: desde el 30, 1 por nivel, 3 al día, fuera del modal, sin Lluvia ni Piraña, con Tinaja. 3. Ranura, ola de 1,2 s, confirmación ≥ 200, tarjeta del 30. | AF.Solver (parche: `Packages/` es del integrador, §3.2), `S/Boosters/ShuffleBooster.cs`, parches de `BoosterTray.cs` ✱ y `GameScreen` ✱ | — | `GameScreen`, `BoosterTray` ✱ |
| B3 | Salto del Boto | 1,5 | Elegibilidad (`skip_min_*`), rama D, pausa, banner, Rosa en el Mapa, `last_skip_level`, sin canoa ni Crecida | `S/Boosters/SkipBooster.cs`; parches de `StuckModalModel`, `LevelFlow.Funnel`, `Profile`, `RiverMapScreen` ✱ | B1a (para no tocar la Crecida) | Mapa ✱, `strings.csv` ✱ |
| B4 | Bitácora + cola de takeovers + pantalla 2 del 10 | 3 | Reglas (`eco_calendar_table_v1`), libreta 5×6, tarjeta con punto, takeover al abrir, `TakeoverQueue` | `S/LiveOps/LoginCalendar*.cs`, `S/UI/LogbookScreen.cs`, `S/GameFlow/TakeoverQueue.cs`; parches de `AppFlow` ✱ y `LobbyScreen` | B1a (Canasta en la pantalla 2); Q-M1, Q-M2, Q-M3, Q-M6 | `AppFlow` ✱ |
| B5 | Mercado y ofertas con implementaciones falsas | 3 | Pantalla del Mercado (05 §4; 06 §4.7); Mochila (ventana 10-20, 48 h); tarjeta Sin anuncios; llenado del Cántaro e icono; tarjeta de skin del mundo desde el 41; `offer_scheduler` | `S/UI/MarketScreen.cs`, `S/IAP/Offers*.cs`; parches de `LobbyScreen` y del HUD ✱ | B6 (sesiones) para la Mochila; Q-E2 (icono de la oferta), Q-E3, Q-E5, Q-E6 | HUD ✱ |
| B6 | Sesiones, regalo de regreso, notificaciones (solo la lógica) | 2 | Contar `stats.sessions` (Q-M4); `live_comeback_gift`; `NotificationPlanner` + `INotificationScheduler` falso; pre-prompt simulado; `settings.push` | `S/LiveOps/Comeback*.cs`, `NotificationPlanner.cs`, `S/Platform/INotificationScheduler.cs`; parche de `GameBootstrap` | Q-M4 | Poco |
| B7 | `AF.RemoteConfig` + `AF.Analytics` con implementaciones falsas | 3 | `RC.Get<T>`, `RemoteConfigDefaults.json` generado desde 08 §9 con su test, congelación por pantalla; catálogo de 08 §8.2, sumidero de logcat, parámetros comunes, eventos de nivel, embudo, economía, anuncios e IAP; `biz_maintenance_*`, `biz_min_app_version` y el resto de `biz_feature_*` (08 §9) | `S/RemoteConfig/`, `S/Analytics/` (asmdef nuevos), `tools/` (generador); parches en `LevelFlow`, `Wallet`, `GameBootstrap` | Q-T1 | Los dos módulos, ninguno. Los ganchos de los eventos de pantalla, sí: `GameScreen*` ✱ (hoy emiten las sondas de la victoria, las tarjetas, las etiquetas, el banner de mundo y los avisos: `GameScreen.Arrival/Cards/Labels/WorldBanner/Notices.cs`), `RiverMapScreen*` ✱ (sondas del Mapa) y `AppFlow` ✱ (pantallas, sesión y corte por consentimiento) |
| B8 | Nube sin SDK + integridad + anti-fraude local + Eliminar cuenta en local | 2,5 | Fusión pura con sus 8 casos; `ICloudSave` falso; `applied_purchases`; HMAC con clave derivada del dispositivo (08 §4.3); reloj del servidor en canoas y topes; revocación local; `locale`, `first_app_version` y `country_first` (08 §4.3); «Eliminar cuenta y datos» en Ajustes en 2 pasos con el borrado local (`IProfileStore.Delete()`; 08 §11) | `S/Save/CloudMerge.cs`, `ICloudSave.cs`; parches de `Profile`/`ProfileJson`, `Canoes.cs`, `SettingsScreen*` | — | Ninguno (textos nuevos en un CSV aparte) |
| B9 | Accesibilidad | 3 | Reducir movimiento y Alto contraste (ajuste y perfil); escala de fuente del sistema; C9 (mantener pulsado); test del simulador de daltonismo (07 §7.8; 08 §14.3; el método, [P]); TalkBack completo como fija 02 §12 (etiquetas, foco, selección) | `S/UI/A11y/*`, `S/Platform/AndroidSystemScale.cs`; parches de `SettingsScreen*`, `TapController`, `GameScreen` ✱ | Q-N4 | `GameScreen` ✱ |
| B10 | Compuertas de release + CI s30 | 2,5 | `ci/check-manifest` (target/min, permisos, billing, `appCategory = Game`, id de prueba), `BuildRelease.cs` sin firma, stripping High + `link.xml`, `ci.yml` (s30); retroceso predictivo y UI dentro del *safe area* (08 §13.1 #4) | `ci/`, `S/../Editor/CI/` (son de build-release: parches), `.github/` | Q-T5, Q-T6 | Ninguno |
| B11 | Tutorial, Sin canoas y contenido | 1,5 | Dedo del 2; dedo de la Cuia extra del 8; tooltip del 6 y del 8; tarjeta de Canoas del 31; tooltip de la Cosecha doble del 11; «Volver al Mapa» en Sin canoas; reserva del 20 % para `level_overrides` en la generación (03 §17); V3; `lvl_label_override`; test a 320 dp; C10 (arrastre) | Parches de `TutorialInstruction`, `GameScreen` ✱, `NoCanoesScreen`; `tools/level-generator` | Q-N2, Q-E4 | `GameScreen` ✱ |
| B12 | Consentimiento y edad (con implementaciones falsas) | 2,5 | Pantalla de edad neutral y bandas, consentimiento por país, `profile.consent`, flags hacia anuncios, analítica e IAP, cambio desde Ajustes | `S/UI/ConsentScreen.cs`, `S/Platform/Consent*.cs`; parches de `AppFlow` ✱ y `Profile` | Q-T3, Q-T4; textos [P] (abogado después) | `AppFlow` ✱ |
| B13 | Borradores de publicación (solo documentación) | 1,5 | Ficha de Play en 3 idiomas, Data Safety, IARC, `LICENSES.md`, Créditos; política de privacidad, página web de eliminación de cuenta y `app-ads.txt` (08 §13.1 #2); especificaciones y textos de los assets de tienda para el artista (07 §10; 08 §13.1 #5); lista de regresión de release de 60 casos (08 §14.3) | `docs/store-listing/`, `docs/qa/`, `LICENSES.md` | — (el arte, del artista; la firma, del abogado) | Ninguno |
| B14 | Meta pendiente | 2,5 | 1. Skins por hito con arte provisional: ganarlas en 40/80/140, pintarlas en `CuiaView`, elegirlas (06 §4.5; 07 §4.6; Q-M8). 2. Mapa: tocar un punto de paso para leer su dato, vista previa de la Canasta en las paradas futuras y marca en las cobradas, «Premio del mundo · faltan N» (06 §4.5). 3. «Sobre la Amazonía»: datos del río con su fuente (07 §8.2). 4. `album.fruits` (08 §4.3) y `af_fruit_discovered` (08 §8.2) como sonda. 5. Noche con LUT de Difícil y Muy difícil (07 §7.7 [P]) | `S/Economy/SkinRewards.cs`, tests; parches de `CuiaView`, `SettingsScreen.About.cs`, `Profile`, `RiverMapScreen*` ✱, `StopBackdrop` ✱ | B1a (la Canasta en el Mapa); Q-M8 | `RiverMapScreen` ✱, `StopBackdrop` ✱, `strings.csv` ✱ |

**Orden recomendado:** B1a → B2 → B3 → B6 → B4 → B5 → B7, con B8, B10 y B13 en paralelo, porque no chocan con nada. B1b (tras Q-M2), B9, B11, B12 y B14 cuando el escritorio libere `GameScreen`, `AppFlow`, `RiverMapScreen` y `StopBackdrop`.

**S2 (el primer bloque):** **B1a, Crecida + Hoja escudo + Canasta (reglas, perfil, RC, sonda y tests).** Es el que más acerca la 1.0: la Crecida es el sistema de retención central de 06 §4.3, el modelo económico ya cuenta con ella y con la Canasta (05 §3; `econ_sim.py`), y TiTino y el modal la esperan. Cumple las tres condiciones de S2 (`spark-reto-del-rio.md` §11):
- **(a) No depende de nada bloqueado:** son reglas sin motor con los defaults de 08 §9 compilados y la analítica como sonda; no necesita SDK, cuentas, `INTERNET` ni arte.
- **(b) Sin preguntas abiertas sin recomendada:** el canon está cerrado en 06 §4.3, 05 §2.4, 04 §8, 06 §4.4 y GDD.md §6.1 #13; las dos preguntas que deja, Q-M9 (dónde va el estado) y Q-M10 (en qué módulo), tienen recomendada. La interfaz, que sí depende de Q-M2, queda en B1b.
- **(c) Toca lo mínimo el escritorio:** archivos nuevos y parches probados a `FailResolve`, `LevelFlow.Victory`, `Profile` y `SlothContext`, ninguno de la lista del escritorio.

B8, B10 y B13 tampoco tocan el escritorio, y los dos módulos de B7 tampoco (sus ganchos sí); pero B1a acerca más la 1.0 y, como ellos, no toca ningún archivo del escritorio.

**Fuera de la tanda 5** (con su motivo):
- precios en CLP, ARS, EC y EUR: fuera del soft launch (05 §4.2);
- Unity Localization 1.5.13: espera a la decisión de Addressables y PAD (Q-T2);
- el elenco de unas 50 especies: artista y asesor (Q-N6);
- todo lo de §3 (SDK, cuentas, CI con secretos, firma, arte final, QA humana externa).

---

## 5. Preguntas para el dueño

Lo que el GDD deja sin decidir. La primera opción de cada fila es la recomendada.

| Id | Pregunta | Opciones (recomendada primero) | Qué frena |
|---|---|---|---|
| Q-N1 | `anim_speed_factor_late`: el código usa 1,0 (D-006, tiempos de Magic Sort) y 08 §9 y 02 §4 dicen 0,8 | (a) fijar 1,0 y corregir el GDD; (b) volver a 0,8 | Nada (es una discrepancia) |
| Q-N2 | V3: alivio con la Cuia extra cuando `relief` solo trae `plus_empty` (18 niveles desde el 31) | (a) generar siempre `minus_color` desde el 31; (b) servir `plus_empty` si T + 2 ≤ 16; (c) sin alivio, documentado | B11 |
| Q-N3 | *Retirada* (ver las notas bajo la tabla) | — | — |
| Q-N4 | Contraste del patrón: 02 §12 dice «50 %»; 07 §3 dice 35 % en Alto contraste | (a) un solo ajuste «Alto contraste» al 35 % y corregir 02 §12; (b) dos ajustes | B9 |
| Q-N5 | Doble toque (02, pregunta 4) | (a) sin función (C3); (b) atajo al único destino legal | — |
| Q-N6 | Elenco de unas 50 especies | (a) en la 1.1, y la 1.0 sale con los 11 miquitos; (b) en la 1.0 | — |
| Q-N7 | *Retirada* (ver las notas bajo la tabla) | — | — |
| Q-N8 | Curación de 1-200 «a mano» | (a) los agentes preparan informes y el dueño decide en `curation_*.json`; (b) el QA automático basta | — |
| Q-M1 | Bitácora: **la decisión está tomada** (D-029 #17, B: el bono diario deja elegir 1 de 3 premios visibles; nota en 06 §4.2, que «se rediseña cuando se planifique»). Falta el diseño y su versión | (a) diseñarlo ya para la 1.0: el tech-lead propone los tres premios de cada casilla, de igual valor en semillas-equivalentes, para no mover las cifras [sim] de 05 §2.3, y el dueño lo aprueba antes de B4; (b) la 1.0 sale con las 30 casillas de 06 §4.2 («hasta entonces la Bitácora sigue como está») y el «1 de 3» llega en la 1.1; (c) «1 de 3» solo en los hitos 7, 14, 21 y 30 | B4 |
| Q-M2 | Victoria: **la decisión está tomada** (D-029 #18, B: tras ganar, los premios del día van juntos en un solo panel; nota en 06 §4.7, «se diseña cuando se planifique»). Falta el diseño | (a) diseñarlo ya: un panel en la victoria con la Cosecha, la Cosecha doble, la réplica de la Crecida, la barra «Canasta · {sitio} k/10» y la Canasta que se abre dentro del panel, sin pantalla aparte; propuesta del tech-lead que aprueba el dueño antes de B1b y B4; (b) mantener en la 1.0 la cadena de 06 §2.3 y la pantalla de abrir aparte (**reabre D-029 #18**) | B1b, B4 |
| Q-M3 | Esquema de `calendar` en el perfil (el de 08 §4.3 no encaja con el reclamo secuencial) | (a) `{cycle, next_day, last_claim_date}` [P], anotado en 08 §4.3; (b) el de 08 tal cual | B4 |
| Q-M4 | Qué cuenta como sesión | (a) arranque en frío o vuelta tras ≥ 30 min en segundo plano (como `session_start` de Firebase); (b) solo el arranque en frío | B5, B6 |
| Q-M5 | Paquete `com.unity.mobile.notifications` y permiso `POST_NOTIFICATIONS` (alarmas inexactas) | (a) aprobarlos; (b) la 1.0 sin push | Push nativo |
| Q-M6 | Marco de país (día 30 de la Bitácora) sin pantalla de perfil | (a) guardarlo como derecho y enseñarlo en la 1.1; (b) enseñarlo en la ventanita de casa; (c) quitarlo | B4 |
| Q-M7 | Frutas del río en 321-700 (el calendario acaba en el 320) | (a) en la 1.0, solo el dato de llegada; series nuevas en la 1.1; (b) series nuevas ya (asesor); (c) reciclar | — |
| Q-M8 | Skin ganada: ¿se pone sola? | (a) sí, con aviso, y se cambia en el Mercado; (b) nunca | B14 |
| Q-M9 | Esquema del perfil de la Crecida y la Canasta. 08 §4.3 define `streak` solo con `{wins, shield_available, river_full}`, pero 06 §4.3 necesita también los hitos pagados hoy y las semillas del día (para el tope de 600). No hay bloque `basket`: 06 §4.5 dice «sin estado nuevo salvo un campo» (`progress.map_seen_level`, que ya existe) y la versión anterior de este plan lo inventaba. Además, `shield_available` está duplicado: en `daily` (04 §10.1; `Profile.cs:220`, `DailyData.ShieldAvailable`) y en `streak` (08 §4.3) | (a) `streak` = `{wins, river_full}`; los hitos pagados hoy y las semillas del día, en `daily` (04 §10.1), que ya se reinicia con `date_local`, con nombres [P] (p. ej. `streak_hits_today`, `streak_seeds_today`); un solo `shield_available`, el de `daily`; la Canasta, sin bloque: se deduce de `max_level` (cada 10 niveles, 06 §4.4), como el resto del Mapa, sin más campo que `map_seen_level`; se anota en 08 §4.3; (b) todo en `streak` (`wins`, `river_full`, `shield_available`, hitos y semillas de hoy y su fecha), y se quita `daily.shield_available` (cambia 04 §10.1 y `Profile.cs`); (c) como (a), más un campo [P] para una Canasta ganada y aún sin abrir si la app se cierra en su pantalla | B1a |
| Q-M10 | Módulo de la Crecida: 08 §2.2 la nombra en AF.Economy («Crecida y Hoja escudo») y en AF.LiveOps («Crecida»); la Canasta solo en AF.Economy | (a) AF.Economy, junto a la Canasta, porque sus hitos pagan con `Wallet.Apply`, y se quita de la fila de AF.LiveOps; (b) AF.LiveOps, con AF.Economy solo para el pago | B1a |
| Q-E1 | Comodines antes del nivel. El sumidero cosmético por semillas va a la 1.1 (09 §1.2), así que eso no se pregunta. Queda un choque: GDD.md §6.1 #4 dice «Cuia extra y Morpho ofrecidos antes del nivel» y 04 §1, principio 1, «el comodín se ofrece en el momento de la necesidad, nunca antes del nivel» | (a) gana 04 §1: ningún comodín se ofrece antes del nivel, y se corrige GDD.md §6.1 #4; (b) ofrecerlos antes del nivel solo como sumidero de semillas, como excepción escrita a 04 §1, principio 1 | Nada con (a); B5 con (b) |
| Q-E2 | Icono de la oferta en el inicio: **la decisión está tomada** (D-029 #23, A: la oferta temporal va como icono del inicio desde el 31, con cuenta atrás y sin abrirse sola; nota en 05 §5). Falta saber qué oferta lo usa en la 1.0: ninguna oferta de 05 §5 tiene cuenta atrás después del 31 (la Mochila caduca en el 20 o a las 48 h; las skins, al ganar la skin por hito, sin reloj) | (a) el icono llega con la primera oferta temporal (la Mochila de regreso, 1.1, 09 §1.2, o el Pase Expedición, 1.2, 05 §5), y en la 1.0 B5 deja su hueco en el inicio; (b) en la 1.0, la oferta de skin del mundo (desde el 41) en el icono, con los niveles que faltan en lugar de la cuenta atrás [P]; (c) dar a la oferta de skin una cuenta atrás de 48 h (cambia la caducidad de 05 §5) | B5 |
| Q-E3 | Origen de `geo_tier` (hoy fijo en T3) | (a) región del dispositivo (`IDeviceLocale`) y el país de la tienda cuando llegue el IAP; (b) solo el país de la tienda; (c) IP | B5 |
| Q-E4 | Texto de la tarjeta de Canoas del 31 (el de 04 §7.1 quedó obsoleto con D-008) | (a) «Canoas: si te rindes o sales del nivel, pierdes una. Vuelven solas cada 25 minutos.»; (b) otro | B11 |
| Q-E5 | «Restaurar compras» antes del IAP real | (a) oculto hasta el adaptador real; (b) visible y desactivado | B5 |
| Q-E6 | Mercado sin el arte de las skins | (a) Mercado ya con semillas, Mochila, Sin anuncios, Cántaro y Canoas 1 h; (b) esperar | B5 |
| Q-T1 | Bus `AF.Events` (08 §2.1, principio 3) frente a las interfaces de escucha | (a) bus mínimo sin motor, que publican GameFlow y Economy y consume Analytics; (b) ratificar las interfaces; (c) rehacerlo todo como bus | B7 |
| Q-T2 | Unity Localization 1.5.13 (trae Addressables) | (a) esperar a la decisión de Addressables y PAD; (b) instalarlo ya | — |
| Q-T3 | País del perfil legal sin servidor | (a) SIM, luego región del teléfono, y si no, perfil `default` con el tratamiento más estricto; (b) esperar al servidor | B12 |
| Q-T4 | «PIN de compras» para 13-17 | (a) doble confirmación en el juego + la autenticación de Google Play (a confirmar con el abogado); (b) PIN propio | B12 |
| Q-T5 | ¿Pueden los agentes crear `.github/workflows/ci.yml` (s30, sin secretos)? | (a) sí, lo hace build-release y el dueño aprueba el merge; (b) no | B10 |
| Q-T6 | Managed Stripping High (08 §5.4) | (a) aplicarlo ya, con `link.xml` y un build de prueba; (b) esperar a los SDK | B10 |
| Q-T7 | Cobertura ≥ 80 % de la compuerta F1 | (a) aprobar coverlet solo para `dotnet test`; (b) quitar la cifra | — |
| Q-T8 | *Retirada* (ver las notas bajo la tabla) | — | — |

**Retiradas** (ya decididas por el canon; se dejan los ids para no renumerar):
- **Q-N3** (niveles de respaldo del kill switch): GDD.md §6.3 #31 y D-020 (y 02, pregunta 14) decidieron que en la 1.0 los niveles de una mecánica apagada se sustituyen con `level_overrides` (08 §4.4) y el formato de respaldo por slot se fija con la 1.1. Lo que queda son los `level_overrides` reales por RC (dueño, Firebase) y la reserva del 20 % de 03 §17, que es una tarea de generación (B11).
- **Q-N7** (TalkBack en la 1.0): 02 §12 ya lo fija completo: la etiqueta de cada cuia con sus dosis, la `contentDescription` de cada botón, el orden del foco y el anuncio de la selección (B9).
- **Q-T8** (clave del HMAC): 08 §4.3 ya dice «`hmac` con clave derivada del dispositivo», que disuade la edición manual y no es seguridad real. Volver a firmar al restaurar desde la nube va con la fusión de B8.

---

## 6. Hallazgos para el GDD (los lleva el integrador)

- **02 §4.2:** sigue diciendo que Revelar sin medios saca un aviso, pero el código ya responde con la burbuja de D-022 (`GameScreen.Reveal.cs:185`).
- **04 §7.1:** el texto de la tarjeta de Canoas quedó obsoleto con D-008 (Q-E4).
- **08 §9 frente a 02 §4:** `anim_speed_factor_late` (Q-N1).
- **08 §4.3:** el esquema de `calendar` (Q-M3).
- **08 §4.3 frente a 04 §10.1:** `shield_available` está en `streak` y en `daily`, y `streak` no tiene los hitos pagados hoy ni las semillas del día que pide 06 §4.3 (Q-M9).
- **08 §4.3:** remite a una «sección 10.6» que no existe (la sección 10 llega a 10.3).
- **08 §2.2:** la Crecida figura en AF.Economy y en AF.LiveOps (Q-M10).
- **GDD.md §6.1 #4 frente a 04 §1, principio 1:** «Cuia extra y Morpho ofrecidos antes del nivel» frente a «nunca antes del nivel» (Q-E1).
- **06, pregunta abierta 4, y 06 §4.2, regla (2):** siguen pidiendo confirmar la Canasta grande, pero GDD.md §6.1 #13 la decidió el 2026-10-06 (150 semillas + 1 Cuia extra + 1 Morpho + 3 Marea, «con nombre en el glosario»). La entrada sigue faltando en el glosario, cuya pregunta 8 sigue abierta.
- **Pororocas sin uso:** el Reto del río ya regala Pororocas al stock, que no se pueden usar hasta B2.
