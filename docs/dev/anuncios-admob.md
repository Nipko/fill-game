# Anuncios con AdMob directo: Google Mobile Ads para Unity (D-049)

Estado: 2026-10-25, rama `tanda6/sdk-gma`. Decisión: D-049 y su añadido del 2026-10-25 (`docs/dev/decisiones.md`): AppLovin no acepta cuentas nuevas, así que los anuncios van con **AdMob directo** y el plugin **Google Mobile Ads para Unity**. Propuesta técnica verificada: `docs/dev/anuncios-gma-propuesta.md`. GDD: 08 §2.2 (`AF.Ads`), §2.3, §7 #2, §8.2, §9 (`ads_*`), §10.1-§10.2 y §13.3; 05 §6-§7; D-033 B12; D-042 (MAX, que quedó instalado sin servir: `docs/dev/anuncios-max.md`). **Añadido de D-049 del 2026-10-27: MAX sale del proyecto** (§8 bis).

## 1. Qué entra

| Paquete | Versión | Origen | SHA-256 del `.tgz` |
|---|---|---|---|
| `com.google.ads.mobile` (Google Mobile Ads for Unity) | **11.5.0** (SDK Android `play-services-ads` 25.4.0, UMP 4.0.0) | OpenUPM: `https://package.openupm.com/com.google.ads.mobile/-/com.google.ads.mobile-11.5.0.tgz` (revisión `12a1edb4…`, el tag `v11.5.0`) | `54aa0d8ea6ef33e52d527ffd93153b5ee9bc8b7384f06ae08d276e97a3cbb13b` (comprobado otra vez al copiarlo) |

- El `.tgz` está en `Packages/googleads/` con Git LFS (`.gitattributes`) y se referencia con `file:` en `Packages/manifest.json`, como Firebase (D-039). Pide `com.google.external-dependency-manager` 1.2.187; manda el **1.2.189** que trae Firebase (no entra `com.unity.external-dependency-manager`).
- **No entra:** Unity Ads ni ningún otro adaptador (no aprobados todavía; D-049, añadido); el SDK «Next-Gen».
- El plugin crea, la primera vez, `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset` (lo escribe `GoogleAdsProjectSetup`, §2) y `Assets/GoogleMobileAds/link.xml` (conserva sus ensamblados con el stripping High). Los dos se commitean tal como los deja Unity.

## 2. Ajustes del plugin (versionados)

Los fija `AF.Ads.Google.Editor.GoogleAdsProjectSetup.Apply` (menú «Amazon Fill/CI/Aplicar ajustes de Google Mobile Ads» o `-executeMethod`), nunca a mano (regla dura 3). Escribe en `GoogleMobileAdsSettings.asset` con `SerializedObject` (la clase del plugin es `internal`; si el asset no existe, lo crea con su `LoadInstance`):

- **App ID de Android** `ca-app-pub-2916158449453694~9925643748` (no es secreto; constante `GoogleAdUnits.AndroidAppId`). El pre-proceso del plugin (`ManifestProcessor`) lo escribe como `com.google.android.gms.ads.APPLICATION_ID` en el manifiesto de su `.androidlib`.
- SDK Android **Standard** (`play-services-ads` 25.4.0), pre-proceso de Gradle encendido (resuelve con EDM4U en cada build), opción de `packagingOptions` de coroutines encendida (la de fábrica; con AGP 9.0.0 el build pasó, sin aviso de #4106) y las optimizaciones `OPTIMIZE_INITIALIZATION` y `OPTIMIZE_AD_LOADING` encendidas.

`BuildDev` y `BuildRelease` llaman a `GoogleAdsProjectSetup.CheckForBuild` antes de construir: si el asset no está como debe, lo aplica; si aun así falla, el build se para con el código 2. Así ningún build sale con el App ID vacío (sin él, el plugin para el build, y `MobileAdsInitProvider` cerraría la app al arrancar). Lo prueban los tests EditMode `GoogleAdsProjectSetupTests`.

**MAX (historia):** hasta el 2026-10-27, `MaxProjectSetup.Apply` dejaba apagado el flujo de UMP de MAX (`ProjectSettings/AppLovinInternalSettings.json`) para que solo hubiera un aviso de consentimiento, el de Google. Con la salida de MAX (§8 bis) desaparecen `MaxProjectSetup`, ese JSON y la compuerta de `BuildDev` y `BuildRelease` sobre `AppLovinSettings.asset`; UMP 4.0.0 llega solo por el plugin de Google.

## 3. Bloques de anuncios

Constantes de `AF.Ads.GoogleAdUnits` (ninguno es secreto; 08 §9 no tiene claves de Remote Config para bloques, así que no se inventan: pregunta 1 de §11):

| Build | Recompensado | Intersticial |
|---|---|---|
| Desarrollo (`Debug.isDebugBuild`) o con el `applicationId` provisional `com.amazonfill.dev` (también el ensayo de release) | `ca-app-pub-3940256099942544/5224354917` | `ca-app-pub-3940256099942544/1033173712` |
| Release con el ID definitivo | `ca-app-pub-2916158449453694/1182085615` | `ca-app-pub-2916158449453694/7187673451` |

- Los de prueba son los **bloques de prueba oficiales de Google** para Android (comprobados el 2026-10-25 en https://developers.google.com/admob/android/test-ads y en el ejemplo del propio paquete 11.5.0). Así ningún build de prueba genera tráfico inválido en la cuenta de AdMob.
- La decisión es la función pura `GoogleAdUnits.Choose(developmentBuild, applicationId)`, probada en `GoogleAdServiceTests`. El arranque escribe en logcat `[AF.Ads] proveedor: google (bloques de prueba|bloques de produccion)` (hasta el 2026-10-27 terminaba en «; max: not_configured»), sin los ID.
- Un recompensado para las seis ubicaciones `rv_*` y un intersticial para `interstitial_post_level`, como con MAX.

### 3 bis. Bloques y mediación en la consola de AdMob (2026-10-27)

El bloque recompensado de producción que había en el código, `…/2877105387` («Tintalu Rewarded MAX»), resultó ser de formato **Intersticial recompensado** (comprobado el 2026-10-27 en la consola de AdMob). El juego lo carga como `RewardedAd`, así que en producción no se habría servido por no coincidir el formato. Con el dueño se creó un bloque nuevo de formato **Recompensado** y el código pasa a él.

| Bloque (consola de AdMob) | Formato | ID | Estado |
|---|---|---|---|
| «Tintalu Rewarded» | Recompensado (recompensa: 1 «Reward») | `ca-app-pub-2916158449453694/1182085615` | **En uso** (`GoogleAdUnits.ProductionRewarded`). SSV **todavía sin activar** |
| «Tintalu Interstitial MAX» | Intersticial | `ca-app-pub-2916158449453694/7187673451` | **En uso** (`GoogleAdUnits.ProductionInterstitial`) |
| «Tintalu Rewarded MAX» | Intersticial recompensado | `ca-app-pub-2916158449453694/2877105387` | **No usar: intersticial recompensado.** No está en el código ni en la lista blanca del SSV |

Grupos de mediación con **Unity Ads por bidding** (Android):

| Grupo | ID | Bloque de AdMob | Placement de Unity Ads |
|---|---|---|---|
| «Tintalú Recompensado Android» | 9122104719 | `…/1182085615` | `BP_Rewarded_Android` |
| «Tintalú Intersticial Android» | 3021919976 | `…/7187673451` | `BP_Interstitial_Android` |

- **Game ID de Unity Ads (Android):** 800412912. **ID de organización de Unity:** `20067668703382` (para `app-ads.txt`). Ninguno es secreto.
- **El adaptador de Unity Ads para Google Mobile Ads (3.21.1) espera la aprobación del dueño** (regla dura 4: sin aprobación, ni paquetes ni SDK). Hasta que entre en el build, los grupos de mediación existen en la consola pero Unity Ads no puja: AdMob sirve solo su demanda. Al integrarlo hay que repasar el manifiesto, el tamaño, el 16 KB, Data Safety (08 §13.3) y la web.
- **`app-ads.txt`:** la línea `unity.com, 20067668703382, DIRECT` está en `infra/web/src/static/app-ads.txt` **solo comentada**; se descomenta cuando entre el adaptador.
- **Pendiente del dueño:** cuando se despliegue la API, activar el SSV en el bloque **nuevo** `1182085615` con la URL `<ApiUrl>/v1/ssv` (`infra/README.md` §5 bis). La lista blanca del servidor (`infra/lib/config.ts`, `admobRewardedAdUnits`) ya es `1182085615` en prod y `1182085615` + `5224354917` en dev.

## 4. Código

| Ensamblado | Qué |
|---|---|
| `AF.Ads` (sin motor) | `GoogleAdService` (la lógica, sobre `IGmaPort`), `FakeGmaPort`, `GoogleAdUnits`. `AdConsentSettings` gana `Teen` (para `TagForUnderAgeOfConsent`); `AdImpression` gana la plataforma (`AdMob`; `appLovin` salió con MAX el 2026-10-27, y una red de la mediación de AdMob va en `ad_source`) |
| `AF.Ads.Google` (con motor) | `NativeGmaPort` (solo `UNITY_ANDROID && !UNITY_EDITOR`): `MobileAds`, `ConsentInformation`/`ConsentForm`, `RewardedAd` e `InterstitialAd`. Encola los avisos del SDK (pueden llegar en otro hilo) y los entrega en `Pump`, cada fotograma desde `GameBootstrap.TickAds`. `GoogleAdProvider` elige Google en el teléfono y el simulado en el editor |
| `AF.Ads.Google.Editor` | `GoogleAdsProjectSetup` |
| `AF.UI` | `GameBootstrap.Ads.cs`: `CreateAdProvider` usa `GoogleAdProvider`, envuelto por `WrapConsentAds` y `WrapAds` como siempre. Un build de desarrollo lanzado con el extra `af_ads_fill` sigue con el anuncio simulado (las pruebas automáticas en el teléfono) |
| `AF.Analytics.Adapters` | `AnalyticsAdService` sin cambios: `af_ad_request`, `af_ad_shown`, `af_ad_reward` (`granted=provisional`) y `ad_impression` con `ad_platform` = `AdMob`, `ad_source` = `AdSourceName` y `value` = micros / 10⁶ en la moneda del `AdValue`, mientras `ads_ilr_to_firebase` esté encendido |

Las reglas de cadencia, topes, esperas e intersticiales siguen en `AdPolicy` (05 §6; 08 §9 `ads_*`): el proveedor solo carga y muestra. La precarga (el intersticial a partir del nivel 21), los reintentos con espera exponencial y los márgenes de cierre (`RewardGrace` 1,5 s, `ResumeGrace` 3 s [P]) son los de MAX.

## 5. Orden de arranque, consentimiento y edad (08 §2.3 y §7; D-033 B12)

| Situación | Qué hace Google |
|---|---|
| Sin decisión (pantalla de edad o de consentimiento abierta) | Nada: ninguna llamada al plugin |
| `<13` (en el arranque o al corregir la edad) | Nunca se toca (ni `SetRequestConfiguration`, ni UMP, ni `Initialize`). Con la banda guardada, el arranque se para en `BlockedAtBoot` antes de crear los anuncios. Si se corrige a `<13` con Google ya iniciado, no se carga ni se muestra nada más (la escena se recarga bloqueada) |
| Decisión que permite anuncios | 1) `MobileAds.SetRequestConfiguration` (contenido máximo **G** siempre; con 13-17, `TagForUnderAgeOfConsent = True`); 2) UMP: `ConsentInformation.Update` (con `TagForUnderAgeOfConsent` con 13-17) y `ConsentForm.LoadAndShowConsentFormIfRequired`; 3) **solo si** `ConsentInformation.CanRequestAds()`, `MobileAds.Initialize`; 4) al avisar de que está listo, la precarga |
| UMP no resuelve (sin red en la primera sesión) | Sin `Initialize`; se vuelve a preguntar al volver a la app (`OnResume`) |
| 13-17 fuera del EEE/UK/CH | `TagForUnderAgeOfConsent` y cada petición con `npa=1` (`biz_under18_no_personalization`) |
| 13-17 con el país legal `US` (T-L5) | Lo de la fila anterior y, además, cada petición con el extra `rdp=1` (tratamiento restringido de datos), también si `biz_under18_no_personalization` está apagado |
| 13-17 con el perfil `ump` (EEE, Reino Unido, Suiza) | **Sin anuncios** (Q-MAX-6 (b), confirmada por el dueño en D-055, punto 4): ninguna llamada. Pregunta 2 de §11 |
| 18+ que no acepta «Anuncios personalizados», o sin país | `npa=1` en cada petición |
| 18+ que acepta | Sin `npa` |
| Perfil `ump` con 18+ | Sin `npa`: deciden las cadenas TCF del mensaje de UMP. Ajustes › Privacidad › opciones de anuncios llama a `ConsentForm.ShowPrivacyOptionsForm` si UMP lo pide (`PrivacyOptionsRequirementStatus.Required`) |

- **Edad corregida después de UMP** (18+ → 13-17 en Ajustes): se reenvía `SetRequestConfiguration` con `TagForUnderAgeOfConsent`, se descarta el anuncio ya cargado y se recarga con `npa=1`; UMP no se vuelve a pedir (fuera del perfil `ump` manda nuestra pantalla, y con el perfil `ump` 13-17 no tiene anuncios).
- **RDP con 13-17 en EE. UU. (T-L5; D-057, añadido del 2026-10-27; `docs/legal/investigacion-legal-mundial.md` §3.3 y [F28]):** los anuncios no personalizados de Google siguen usando identificadores para la frecuencia, el fraude y los informes; para que los de un menor no cuenten como «compartir» en las leyes de los estados de EE. UU., cada petición lleva `rdp=1` (`AdRequest.Extras`, `NativeGmaPort.Load`) cuando la banda que se aplica es 13-17 y el país legal (el del perfil legal: SIM → región) es `US`. Lo calcula `AdConsentSettings.RestrictedDataProcessing` (`ConsentAdService.Apply(flags, país)`); si pasa a `rdp=1` con un anuncio ya cargado (p. ej. la señal de edad llega tarde), se descarta y se recarga. Tests en `GoogleAdServiceTests` (sección «T-L5»). La banda puede venir corregida por **Play Age Signals** (T-L3; `docs/dev/consentimiento.md` §3): entonces se aplica la más estricta de la declarada y la de la señal, que es un uso permitido de la señal (experiencia adecuada a la edad), nunca para segmentar.
- El mensaje de UMP solo aparece donde AdMob lo tiene publicado: el del RGPD (EEE, Reino Unido, Suiza) y el de los estados de EE. UU. En Brasil, Colombia, Perú y México manda nuestra pantalla (08 §7).
- **Contenido G (D-029):** por código (`MaxAdContentRating.G`) y en la consola de AdMob («Contenido máximo» = G, ya hecho).
- `MobileAdsInitProvider` (initOrder 100) se queda: en 25.4.0 solo valida el App ID (sin red ni ID de publicidad; propuesta §4). `DELAY_APP_MEASUREMENT_INIT` = true como refuerzo.

## 6. SSV y concesión (08 §10.2)

- Antes de cada recompensado, **si hay UID** de Firebase Auth anónimo (`FirebaseAuthUid.Current`, el mismo `sub` que valida el JWT de `restoreLedger`), `RewardedAd.SetServerSideVerificationOptions` con `UserId` = ese UID en claro y `CustomData = placement|nonce` (UUID v4; el contrato `infra/contracts/v1/rewarded-ssv` y `parseSsvQuery`). A diferencia de `MaxSdk.SetUserId`, va solo en ese anuncio, no en todas las peticiones. El UID se lee en cada recompensado (si llega tarde, vale desde el siguiente).
- **Sin UID** (sin Firebase o sin backend): no se fija nada, no hay nonce (`LastRewardNonce` = null) y la recompensa sigue como hoy, solo provisional. Si el SSV está activado en el bloque, AdMob llama igualmente con `user_id` y `custom_data` vacíos y el servidor responde `200 ignored` sin escribir. Lo prueban `GoogleAdServiceTests` (`Rewarded_SetsSsv_UserIdAndCustomData_AndRewardsOnce`, `Rewarded_WithoutUid_SetsNoSsv_AndStillRewardsProvisionally`, `Rewarded_EmptyUid_IsLikeNoUid` y `Rewarded_UidThatArrivesLater_IsUsedOnTheNextShow`).
- `AdOutcome.Rewarded` sale con la recompensa del `Show` y el cierre (`OnAdFullScreenContentClosed`), en cualquier orden y una sola vez: es la **concesión provisional**, como antes. `GoogleAdService.LastRewardNonce` y `LastRewardPlacement` dan el nonce de la última concesión.
- **Servidor (`infra/functions/rewarded-ssv`, rama `tanda6/ssv-admob-web`):** `/v1/ssv` acepta el GET de AdMob (`ad_network`, `ad_unit`, `custom_data`, `reward_amount`, `reward_item`, `timestamp`, `transaction_id`, `user_id`, y al final `signature` y `key_id`) y verifica la **firma ECDSA P-256 sobre SHA-256** de la consulta cruda hasta antes de `&signature=` (sin reordenar ni decodificar; `rawQueryString` de API Gateway) con la clave pública del `key_id` de `https://www.gstatic.com/admob/reward/verifier-keys.json` (caché ≤ 24 h en la Lambda; un `key_id` desconocido la refresca una vez, como mucho una por minuto). Como las claves de Google son las mismas para todos los editores, **solo acepta los bloques recompensados propios** (lista blanca de `ad_unit` del entorno: `-c admobRewardedAdUnits`, por defecto `1182085615` en prod y además el de prueba de Google `5224354917` en dev; número o id completo); un bloque ajeno o sin `ad_unit` recibe `200 ignored` sin escribir ni gastar cupo (log `ssv_foreign_ad_unit`). El `transaction_id` hace de `event_id` (atado a un único UID y nonce). Además: `timestamp` de más de 24 h o de más de 10 min en el futuro → 400 `request_expired`; sin `user_id` o `custom_data` → `200 ignored`; idempotencia por nonce, TTL de 30 días, tope de 100 por UID y día y transacción contra UID borrado, como antes. Ya no hay secreto de MAX (`af-<env>/max-ssv-key` desaparece del stack). Contrato: `rewarded-ssv.request.schema.json` describe el callback de AdMob y la respuesta admite `ignored` (también en `AF.Backend`: `SsvCallback`, `SsvResponse.Ignored`). Tests: `infra/test/unit/rewarded-ssv-admob.test.ts` con un par de claves generado en el test, sin red. Después, el dueño activa el SSV en el bloque recompensado de AdMob (`infra/README.md` §5 bis).
- **Pendiente (F3-4, `unity-gameplay-dev`):** registrar cada concesión con su nonce (`LocalRevocation.RegisterProvisional`) para que `restoreLedger` la confirme o la revoque a las 24 h, cuando el SSV del servidor esté listo.

## 7. Gradle: lo que resolvió de verdad

Medido con `gradlew :launcher:dependencies --configuration debugRuntimeClasspath --offline` sobre el proyecto que exportó el build de desarrollo del 2026-10-25 (Gradle 9.3.1, AGP 9.0.0):

| Dependencia | Pedida | Resuelta |
|---|---|---|
| `play-services-ads` / `play-services-ads-api` | 25.4.0 | 25.4.0 |
| `user-messaging-platform` | 4.0.0 | 4.0.0 |
| `androidx.work:work-runtime` | 2.7.0 | 2.7.0 (con `room-runtime` 2.2.5) |
| `com.google.android.play:hsdp` | 2.0.1 | 2.0.1 |
| `play-services-ads-identifier` | 18.0.0 (17.1.0 de AppLovin) | 18.0.0 |
| `play-services-appset` | 16.0.1 (16.0.0 de AppLovin) | 16.0.1 |
| `play-services-basement` / `-tasks` / `-base` | 18.9.0 … | 18.11.0 / 18.4.1 / 18.10.1 (los de Firebase) |
| `play-services-measurement-sdk-api` | 20.1.2 | 23.2.0 (Firebase Analytics) |
| `androidx.privacysandbox.ads:ads-adservices(-java)` | 1.0.0-beta05 | 1.1.0-beta11 |
| `androidx.browser` / `androidx.webkit` | 1.8.0 / 1.12.1 | 1.8.0 / 1.12.1 |
| `androidx.datastore` | 1.0.0 | 1.1.7 |
| `constraintlayout` / `lifecycle-process` / `fragment` | 2.1.4 / 2.6.2 / 1.7.1 | iguales |
| `kotlin-stdlib` / `kotlinx-coroutines-android` | 2.1.0 / 1.8.0 | 2.1.0 / 1.9.0 |
| Billing / AppLovin / Play Games / Firebase Analytics | 9.0.0 / 13.6.4 / 22.1.0 / 23.2.0 | sin cambios (AppLovin salió el 2026-10-27, §8 bis) |

Sin bajadas de versión ni choques: las incidencias del plugin con AGP 9 (#4157 `namespace` y #4106 `packagingOptions`) no aparecieron en el build.

## 8. Manifiesto, permisos, 16 KB y tamaño

`Assets/Plugins/Android/LauncherManifest.xml` (máxima prioridad en la fusión):

- **`AD_ID` activado** (ya no se quita; aprobado en el añadido de D-049 y confirmado en D-055, punto 3). El ID de publicidad solo personaliza con 18+ y el consentimiento de anuncios personalizados (o lo que decida UMP en el EEE/UK/CH); con 13-17, sin consentimiento o sin país, `npa=1` y, con 13-17, `TagForUnderAgeOfConsent`; con `<13` ningún SDK de anuncios; con 13-17 en el EEE/UK/CH, sin anuncios (§5; tests `GoogleAdServiceTests.AdvertisingId_PersonalizedOnlyForAdultsWhoConsent_D055` y `UmpProfile_AdultCorrectedToTeen_StopsLoadingAndShowing_D055`). Con `npa`, Google sigue pudiendo leer el ID para límites de frecuencia, informes agregados y fraude [NV]. Firebase Analytics sigue sin leerlo (`google_analytics_adid_collection_enabled` = false).
- **Fuera** con `tools:node="remove"`: `ACCESS_WIFI_STATE` y `CHECK_LICENSE` (ya estaban), `FOREGROUND_SERVICE` y `RECEIVE_BOOT_COMPLETED` (de `work-runtime`), `ACCESS_ADSERVICES_TOPICS`, `ACCESS_ADSERVICES_AD_ID` y `ACCESS_ADSERVICES_ATTRIBUTION` (Privacy Sandbox), `READ_GSERVICES`. Sigue quitado `PlayGamesInitProvider` (`AppLovinInitProvider` ya no existe: §8 bis).
- `com.google.android.gms.ads.DELAY_APP_MEASUREMENT_INIT` = true.
- `ci/check-manifest.ps1` admite `AD_ID` en su lista blanca (autotest: 57 casos, con `AD_ID` aprobado y `ACCESS_ADSERVICES_TOPICS` y `FOREGROUND_SERVICE` rechazados; D-055 suma 2: `aapt2: ACCESS_WIFI_STATE sigue fuera (D-055)` y `aapt2: CHECK_LICENSE sigue fuera (D-055)`); cualquier otro permiso sigue fallando.

**Manifiesto fusionado de `dev.apk` (2026-10-25):**

- Permisos: `INTERNET`, `ACCESS_NETWORK_STATE`, `VIBRATE`, `BILLING`, **`AD_ID`**, `WAKE_LOCK`, `BIND_GET_INSTALL_REFERRER_SERVICE`, `POST_NOTIFICATIONS` y el propio `DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION`.
- Proveedores: `com.google.android.gms.ads.MobileAdsInitProvider` (initOrder 100), `com.google.firebase.provider.FirebaseInitProvider` (initOrder 100) y `androidx.startup.InitializationProvider` (sin initOrder) con `WorkManagerInitializer`, `EmojiCompatInitializer`, `ProcessLifecycleInitializer` y `ProfileInstallerInitializer`.
- `meta-data`: `com.google.android.gms.ads.APPLICATION_ID` = el App ID de §2, `OPTIMIZE_INITIALIZATION` y `OPTIMIZE_AD_LOADING` = true, `DELAY_APP_MEASUREMENT_INIT` = true. `uses-library` opcionales `android.ext.adservices` y `org.apache.http.legacy`.

**Compuertas (2026-10-25, escritorio x64, `unity-build.ps1 -Method Dev` y `-Method Release -Rehearsal`):** el plugin y su árbol no traen bibliotecas nativas (19 bibliotecas de 64 bits, todas alineadas a 16 KB, igual que antes). El ensayo de release lleva el mismo manifiesto (con `debuggable=false`).

| Artefacto | Tamaño | `check-manifest -Mode dev` | `check-release-classes` | `check-no-unity-iap` | `check-16kb` | `size-gate` |
|---|---|---|---|---|---|---|
| `dev.apk` | 76.011.517 B (72,49 MiB) | OK | lleva `firebase-appcheck-debug` (esperado en desarrollo) | OK | OK (19) | — |
| `rehearsal.aab` | 59.035.955 B (59,04 MB) | OK | OK (4 `.dex`, 28.679 clases) | OK | OK (19) | OK (≤ 80 MB; sin `bundletool`, solo el archivo) |

- **Peso de Google Mobile Ads:** unos **+3,1 MB** en el AAB del ensayo (55,94 MB antes, en `integracion-fase1-fase2`) y +3,2 MB en `dev.apk` (~72,8 MB antes); unas 9.000 clases más en el `.dex` del release. MAX seguía dentro (2-3 MB) aunque no sirviera; salió el 2026-10-27 (§8 bis).

## 8 bis. AppLovin MAX sale del proyecto (añadido de D-049 del 2026-10-27)

Decisión del dueño: «Sí, quita AppLovin MAX del build de release; si AppLovin abre, entra como red dentro de AdMob». Rama `tanda6/sin-max`.

- **Fuera:** `Packages/applovin/` (el `.tgz` de MAX 8.6.6 y su línea de `Packages/manifest.json` y `.gitattributes`), `Assets/MaxSdk/` (con `AppLovinSettings.asset`), `ProjectSettings/AppLovinInternalSettings.json`, `com.applovin:applovin-sdk:13.6.4` de `mainTemplate.gradle` y `AndroidResolverDependencies.xml` (este lo regenera EDM4U), `AF.Ads.Max` (`MaxAdProvider`, `NativeMaxSdkPort`, `MaxBuildConfig`, `MaxProjectSetup`), `MaxAdService`, `FakeMaxSdkPort`, `UnavailableAdService` (solo lo usaba MAX) y sus tests (EditMode: `MaxAdServiceTests`, `MaxBuildConfigTests`, `AdProviderChoiceTests`, `MaxProjectSetupTests`; PlayMode: `MaxConsentBootTests`). `BuildDev` y `BuildRelease` ya no comprueban los ajustes de MAX. El proveedor `AppLovinInitProvider` ya no se quita en `LauncherManifest.xml` porque no llega.
- **Se queda:** la interfaz de `AF.Ads` y `GoogleAdService`, que hereda `Backoff` (2, 4 … 64 s; test `Backoff_DoublesFrom2sUpTo64s`). `AdImpression` usa `AdMob` por defecto y ya no tiene `appLovin`: si AppLovin entra como red de la mediación de AdMob, sale en `ad_source` con `ad_platform` = `AdMob`, así que no se pierde ningún identificador que lea la analítica.
- **Restos en `Library`:** Unity reexporta el proyecto Gradle de `Library/Bee` sin vaciar `unityLibrary/src/main/res`, y el `res/raw/applovin_settings.json` que dejaba el post-proceso de MAX siguió entrando en el primer `dev.apk` y el primer `rehearsal.aab` incrementales (la compuerta nueva los rechazó). Lo borra ahora `AF.EditorTools.RemovedSdkLeftovers` (`IPostGenerateGradleAndroidProject`, con `af_max_config.xml` de `MaxBuildConfig`), que también cubre el CI, que restaura `Library` de una caché anterior.
- **Compuerta:** `ci/check-no-applovin.ps1 <aab|apk|carpeta>` (0 OK · 1 lo lleva · 2 argumentos o ilegible; `--self-test`, 11 casos) falla si una entrada se llama `*applovin*` o si el contenido lleva `Lcom/applovin/` (el SDK y su okhttp3 renombrado), `com.applovin.` (ASCII o UTF-16) o el plugin de C# (`MaxSdkBase`, `AppLovinMax.`). Va en `ci.yml` (job de build) y `release.yml`, tras `check-no-unity-iap`.

**Antes y después** (escritorio x64, `unity-build.ps1 -Method Dev` y `-Method Release -Rehearsal`; «antes» = los builds de `tanda6/ad-id` del 2026-10-27, con el mismo juego de SDK):

| Artefacto | Antes | Después | Diferencia |
|---|---|---|---|
| `dev.apk` | 76.146.974 B | 74.392.717 B | −1.754.257 B (−1,75 MB) |
| `rehearsal.aab` | 59.180.140 B (59,18 MB) | 57.727.293 B (57,73 MB) | −1.452.847 B (−1,45 MB) |
| Clases del release (`check-release-classes`) | 28.681 en 4 `.dex` | 26.775 en 3 `.dex` | −1.906 |
| Bibliotecas nativas de 64 bits (`check-16kb`) | 19 | 18 (sin `libapplovin-native-crash-reporter.so`) | −1 |

- **Permisos** (`check-manifest -Mode dev`, OK en los dos): los mismos antes y después: `INTERNET`, `ACCESS_NETWORK_STATE`, `VIBRATE`, `BILLING`, `AD_ID`, `WAKE_LOCK`, `BIND_GET_INSTALL_REFERRER_SERVICE`, `POST_NOTIFICATIONS` y `DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION`. `AD_ID` sigue porque lo pide Google Mobile Ads (D-049, D-055).
- `check-no-applovin`: falla antes (84 hallazgos en `dev.apk` y 94 en el ensayo: `com/applovin`, la `.so`, recursos `applovin_*`, el plugin de C#), OK después. `check-release-classes`: OK en el ensayo; `dev.apk` lleva `firebase-appcheck-debug`, como siempre en desarrollo. `check-no-unity-iap`, `check-16kb` y `size-gate` (≤ 80 MB, solo el archivo): OK.

## 9. Data Safety y política

- Fila de Google Mobile Ads en 08 §13.3 (D-049); MAX, fuera del build desde el 2026-10-27 (§8 bis). `docs/store-listing/data-safety.md` puesto al día.
- **La web (`infra/web`), puesta al día en `tanda6/ssv-admob-web`.** Textos que cambian en `infra/web/src/content/*/privacy.html`, `terms.html` (sección 6) y `credits.html`: «Hoy el juego no muestra anuncios» pasa a anuncios de **Google AdMob** (no AppLovin MAX) tras la edad y el consentimiento, nunca con `<13`, no personalizados con 13-17, mensaje de UMP de Google en el EEE/UK/CH y en los estados de EE. UU.; el **ID de publicidad** se usa para los anuncios (permiso `AD_ID`); Google como socio de anuncios en lugar de AppLovin, con el enlace a su política; las redes de la mediación de AdMob, «todavía no». `app-ads.txt` (`infra/web/src/static/app-ads.txt`): la línea de AdMob `google.com, pub-2916158449453694, DIRECT, f08c47fec0942fa0`.

## 10. Lo que hace el dueño

1. Desplegar la API con el nuevo `rewardedSsv` y activar el SSV en el bloque recompensado **nuevo** `ca-app-pub-2916158449453694/1182085615` («Tintalu Rewarded») con la URL `<ApiUrl>/v1/ssv` (`infra/README.md` §5 bis; §3 bis y §6). No en el antiguo `2877105387`, que es un intersticial recompensado.
2. Vincular la app de AdMob a la ficha de Play cuando exista; publicar `app-ads.txt` con la línea de AdMob.
3. Declarar el ID de publicidad en Play Console («Uso del ID de publicidad»: publicidad) y en Data Safety.
4. Si se vincula AdMob con Firebase en la consola, AdMob registra `ad_impression` por su cuenta: entonces apagar `ads_ilr_to_firebase` en Remote Config para no contarlas dos veces (pregunta 3 de §11).
5. Probar en el teléfono con un `dev.apk` (bloques de prueba de Google), nunca con clics propios en anuncios reales.
6. Aprobar (o no) el adaptador de Unity Ads 3.21.1 para la mediación de AdMob (§3 bis); con él, descomentar la línea de Unity en `app-ads.txt`.
7. **Play Console (T-L3, Texas):** declarar las clasificaciones por edad de los productos de la app (<13, 13-15, 16-17, 18+) en la sección de las leyes de las tiendas de EE. UU. [F24]. Lo declara el dueño; el código no lo puede hacer.

## 11. Preguntas abiertas

1. **Bloques en el código (resuelta el 2026-10-26: el dueño aprueba las constantes y se actualiza `.claude/rules/sdk.md`):** 08 §9 no tiene claves de Remote Config para los bloques de anuncios y `.claude/rules/sdk.md` dice «ningún ID de unidad privada en el código»; la tarea del dueño dice que no son secretos y pide constantes. Recomendación: dejarlos como constantes (cambian con un build) y actualizar la regla de `sdk.md`.
2. **13-17 con el perfil `ump` (Q-MAX-6 (b); decidida por el dueño en D-055, punto 4: sin anuncios):** con Google, UMP con `TagForUnderAgeOfConsent` no enseña el formulario a un menor y AdMob sirve anuncios limitados, así que el motivo de Q-MAX-6 (b) desaparece. Se mantiene «sin anuncios» porque así está decidido. Recomendación: preguntar al abogado si se pueden servir anuncios limitados a 13-17 en el EEE/UK/CH.
3. **`ad_impression` doble** si se vincula AdMob con Firebase (§10 punto 4).
