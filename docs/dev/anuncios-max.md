# Anuncios con AppLovin MAX (D-042)

> **D-049 (2026-10-25):** AppLovin no acepta cuentas nuevas de editor, así que los anuncios irán con **AdMob directo** (plugin Google Mobile Ads), con el SSV de AdMob y Unity Ads como primera red adicional. MAX queda integrado pero sin servir anuncios y, desde el carril `tanda6/sdk-gma`, con su flujo de UMP apagado (§2). **La integración de AdMob está en `docs/dev/anuncios-admob.md`**; esta nota queda como referencia de MAX. La cuenta de AdMob ya configurada está en §10.

Estado: 2026-10-23, rama `tanda5/sdk-max`. Decisión: D-042 (integración de MAX con UMP y adaptadores). GDD: 08 §2.2 (`AF.Ads`), §2.3, §7 #1-#2, §8.2 (eventos de anuncios), §9 (`ads_*`, `biz_*`), §10.1-§10.2 (SSV) y §13.3; 05 §6-§7; D-033 B12 (consentimiento); D-035 (SSV en AWS). Integra el MAX **sin adaptadores**: los adaptadores esperan la aprobación del dueño (§7).

## 1. Qué entra

| Paquete | Versión | Origen | SHA-256 del `.tgz` |
|---|---|---|---|
| `com.applovin.mediation.ads` (AppLovin MAX Unity Plugin) | **8.6.6** (SDK Android `com.applovin:applovin-sdk:13.6.4`) | Registro UPM oficial de AppLovin: `https://unity.packages.applovin.com/com.applovin.mediation.ads/-/com.applovin.mediation.ads-8.6.6.tgz` (el mismo código que la release `release_8_6_6` de GitHub) | `908332af8989b8a239d1c470daf70f0925ea2cd0f72f94748bbbc4c7d228d1d4` (el `integrity` sha512 y el `shasum` sha1 del registro coinciden) |

- El `.tgz` está en `Packages/applovin/` con Git LFS (`.gitattributes`) y se referencia con `file:` en `Packages/manifest.json`, como los de Firebase (D-039). Pide `com.google.external-dependency-manager` 1.2.182; manda el 1.2.189 que ya trae Firebase.
- **Sin el plugin Google Mobile Ads** (08 §7 #2) y **sin adaptadores**.
- **Lo que trae por Gradle** (EDM4U lo escribe en `mainTemplate.gradle`):
  - `com.applovin:applovin-sdk:13.6.4` (Maven Central), que arrastra `play-services-ads-identifier` 17.1.0, `play-services-appset` 16.0.0 y `androidx.browser` 1.4.0;
  - `com.google.android.ump:user-messaging-platform:4.0.0` (Google Maven): lo añade el preproceso del plugin a `Assets/MaxSdk/AppLovin/Editor/Dependencies.xml` porque el flujo de términos y privacidad con UMP está encendido (08 §7 #2); arrastra `play-services-basement` 18.9.0.

## 2. Ajustes del plugin (versionados)

Los fija `AF.Ads.Max.Editor.MaxProjectSetup.Apply` (menú «Amazon Fill/CI/Aplicar ajustes de AppLovin MAX» o `-executeMethod`), nunca a mano:

- `Assets/MaxSdk/Resources/AppLovinSettings.asset`: **Quality Service apagado** (su plugin de Gradle manda la clave del SDK a `api2.safedk.com` en cada build e instrumenta la app; además puede romper el build con AGP 9, 08 §7 #1) y **la clave del SDK vacía**.
- `ProjectSettings/AppLovinInternalSettings.json`: flujo de términos y privacidad **con la automatización de Google UMP** encendido; política `https://tintalu.com/privacidad/` y términos `https://tintalu.com/terminos/` [P: el dominio aún no es público, Q-AWS-9]; sin la alerta previa de términos en el RGPD; sin geografía de depuración.
- `MaxEditorGuard` (`[InitializeOnLoad]`) apaga el actualizador automático del plugin en las preferencias del Editor de cada máquina: la versión está fijada (regla dura 4).

## 3. Clave del SDK y unidades de anuncio (fuera del repositorio)

Regla dura 5. Las pone el build, no el código ni el repositorio. **La clave nunca se escribe en el Integration Manager de AppLovin** (ni en `AppLovinSettings.asset`): ese archivo está versionado. Lo vigilan:

- `MaxProjectSetup.CheckForBuild`, que llaman `BuildDev` y `BuildRelease` antes de construir: si `AppLovinSettings.asset` lleva una `sdkKey` o el Quality Service encendido, el build se para con el código 2 («ajustes distintos de los fijados»), sin escribir la clave en el log;
- los tests EditMode `MaxProjectSetupTests` (`MaxProjectSetup.Validate()` es null; el `.asset` versionado tiene `sdkKey` vacía y `qualityServiceEnabled: 0`).

Dónde va la clave:

- **Local:** `Assets/_LocalSecrets/applovin-max.json` (carpeta ignorada por git, como `google-services.json`), que coloca el dueño:
  ```json
  { "sdk_key": "…", "android_rewarded_unit": "…", "android_interstitial_unit": "…" }
  ```
- **CI:** las variables `AF_MAX_SDK_KEY`, `AF_MAX_ANDROID_REWARDED_UNIT` y `AF_MAX_ANDROID_INTERSTITIAL_UNIT` (secretos de GitHub, los crea el dueño); ganan al archivo.
- `MaxBuildConfig` (`IPostGenerateGradleAndroidProject`, después del de AppLovin) escribe la clave en `res/raw/applovin_settings.json` del proyecto de Gradle y las unidades en `res/values/af_max_config.xml` (`af_max_configured`, `af_max_rewarded_unit`, `af_max_interstitial_unit`). Nunca las escribe en el log.
- Una unidad de recompensados para las seis ubicaciones `rv_*` y una de intersticiales para `interstitial_post_level`: el ID de la ubicación del juego va como `placement` de MAX.

**Sin configuración:** el build de desarrollo y el editor siguen con los anuncios simulados (`FakeAdService`, D-008); un release, sin anuncios (`UnavailableAdService`: los botones de video se ocultan y no hay intersticial). La decisión es la función pura `MaxAdProvider.Choose`, probada en `AdProviderChoiceTests`. El arranque escribe en logcat `[AF.Ads] proveedor: max|not_configured|config_unreadable`.

## 4. Código

| Ensamblado | Qué |
|---|---|
| `AF.Ads` (sin motor) | `MaxAdService` (la lógica, sobre `IMaxSdkPort`), `FakeMaxSdkPort`, `AdConsentSettings` e `IAdConsentTarget`, `AdRewardCustomData` (SSV), `AdImpression` e `IAdImpressionSource` (ILR), `UnavailableAdService`. `ConsentAdService` reenvía cada decisión y las opciones de UMP al proveedor |
| `AF.Ads.Max` (con motor, `MaxSdk.Scripts`) | `NativeMaxSdkPort` (solo `UNITY_ANDROID && !UNITY_EDITOR`), `MaxConfig` (lee los recursos del build por JNI) y `MaxAdProvider` (elige MAX, simulado o ninguno) |
| `AF.Ads.Max.Editor` | `MaxBuildConfig`, `MaxProjectSetup`, `MaxEditorGuard` |
| `AF.UI` | `GameBootstrap.Ads.cs`: `CreateAdProvider`, envuelto por `WrapConsentAds` y `WrapAds` como siempre |
| `AF.Analytics.Adapters` | `AnalyticsAdService` emite `ad_impression` (`ad_platform` = `appLovin`, `ad_source`, `ad_format`, `ad_unit_name`, `value`, `currency`) con `ads_ilr_to_firebase` encendido |

Las reglas de cadencia, topes y cooldowns siguen en `AdPolicy` (05 §6; 08 §9).

## 5. Consentimiento y edad (08 §2.3 y §7; D-033 B12; D-042)

| Situación | Qué hace MAX |
|---|---|
| Sin decisión (pantalla de edad o de consentimiento abierta, textos nuevos, otro perfil legal) | Nada: ni `InitializeSdk` ni ninguna otra llamada |
| `<13` (en el arranque o al corregir la edad) | Nunca se inicia. Con la banda guardada, el arranque se para en `BlockedAtBoot` antes de crear los anuncios. Si se corrige a `<13` con MAX ya iniciado, `SetHasUserConsent(false)`, `SetDoNotSell(true)` y no se carga ni se muestra nada más (la escena se recarga bloqueada) |
| 13-17 fuera del EEE/UK/CH | `SetHasUserConsent(false)` y `SetDoNotSell(true)` **antes** de `InitializeSdk` (`npa`, con `biz_under18_no_personalization`) |
| 13-17 con el perfil `ump` (EEE, Reino Unido, Suiza) | **MAX no se inicia** (Q-MAX-6 (b), recomendada provisional hasta que hablen el dueño y el abogado): la automatización de UMP enseñaría su mensaje de consentimiento a un menor. También con `biz_under18_no_personalization` apagado, que por lo demás trataría a 13-17 como 18+ y le aplicaría el perfil `ump`. Si la edad se corrige a 13-17 con MAX ya iniciado, `npa` y ningún anuncio más |
| 18+ que acepta «Anuncios personalizados» | `SetHasUserConsent(true)`, `SetDoNotSell(false)` y después `InitializeSdk` |
| 18+ que no acepta o sin país | `SetHasUserConsent(false)`, `SetDoNotSell(true)` |
| Perfil `ump` (EEE, Reino Unido, Suiza) con 18+ | No se fijan: `InitializeSdk` enseña el mensaje de UMP (automatización de MAX) y sus cadenas TCF v2 deciden. Ajustes › Privacidad › opciones de anuncios llama a `MaxSdk.CmpService.ShowCmpForExistingUser` |

- **Contenido G (D-029):** MAX 13 no tiene API para el contenido máximo. Se fija en las consolas: en AdMob, «Contenido máximo de los anuncios» = G para la app; en el resto de redes, su equivalente (acción del dueño, [NV] por red). Vale para todas las bandas.
- **TFCD:** MAX 13 quitó `SetIsAgeRestrictedUser`; con `<13` simplemente no se inicia [NV].
- **Por qué Q-MAX-6 (b):** con el perfil `ump` y 13-17, UMP enseñaría su mensaje al iniciar aunque el juego forzara `npa` (no se puede apagar por usuario: la automatización es del build). Hasta la decisión, esos jugadores no ven anuncios (los botones de video se ocultan).

## 6. SSV y concesión (08 §10.2; D-035)

- Antes de cada recompensado: `SetUserId(UID)` con el UID de Firebase Auth anónimo si existe (`FirebaseAuthUid.Current`; sin Firebase, no se manda y el SSV no podrá confirmar) y `ShowRewardedAd(unidad, placement, custom_data)` con `custom_data = placement|nonce` (UUID v4 en minúsculas; el contrato `infra/contracts/v1/rewarded-ssv` y `parseSsvQuery`).
- `AdOutcome.Rewarded` sale con `OnAdReceivedRewardEvent` al cerrar el anuncio: es la **concesión provisional** de 08 §10.2, como la del simulado. El servidor la confirma con `/v1/ssv` y `restoreLedger`.
- **Orden de los callbacks:** si `OnAdReceivedRewardEvent` llega después de `OnAdHiddenEvent`, se espera `MaxAdService.RewardGrace` (1,5 s [P]); sin recompensa, `Skipped`. Un `Hidden` repetido o una recompensa tras el cierre no conceden dos veces. Al volver de segundo plano con un anuncio aún pendiente, si MAX no avisa en `ResumeGrace` (3 s [P]), se suelta (con la recompensa solo si llegó). Los márgenes corren en `GameBootstrap.TickAds`, cada fotograma, sin asignaciones mientras no toca.
- **`SetUserId` es global en MAX:** desde el primer recompensado, el UID acompaña a todas las peticiones de MAX, no solo al SSV [NV: qué envía AppLovin en cada petición; lo revisa el abogado]. Está así en la web y en 08 §13.3.
- **Pendiente (F3-4, `unity-gameplay-dev`):** registrar cada concesión con su nonce (`LocalRevocation.RegisterProvisional`, que hoy nadie llama) para que `restoreLedger` la confirme o la revoque a las 24 h. Se activa cuando el SSV esté configurado en el panel de MAX; si no, todo se revocaría. `MaxAdService.LastRewardNonce` y `LastRewardPlacement` dan el nonce de la última concesión.
- ~~El dueño configura en el panel de MAX la URL S2S de `/v1/ssv` y la clave en Secrets Manager (`AF_SECRET_MAX_SSV`; `infra/README.md`).~~ **Retirado por D-049:** `/v1/ssv` verifica ahora el SSV de AdMob (firma ECDSA de Google, sin secreto); el secreto `af-<env>/max-ssv-key` y `maxSignature.ts` ya no existen (`docs/dev/anuncios-admob.md` §6). Si MAX volviera a servir, haría falta otra vez su verificación.

## 7. Adaptadores (pendientes de aprobación)

No se integran hasta el sí del dueño (D-042, condición 1). Propuesta: los paquetes UPM más recientes del registro de AppLovin a 2026-10-23, compatibles con MAX 8.6.6, con lo que traen por Gradle (POM de Maven):

| Red | Paquete UPM (versión) | Lo que pide por Gradle | Lo que arrastra (primer nivel) | Riesgos |
|---|---|---|---|---|
| Google AdMob | `com.applovin.mediation.adapters.google.android` **25050000.0.0** (17-09-2026) | `com.applovin.mediation:google-adapter:[25.5.0.0]` | `play-services-ads` 25.5.0 → `play-services-ads-api`, `play-services-ads-identifier` 18.0.0, `play-services-appset` 16.0.1, `androidx.privacysandbox.ads:ads-adservices(-java)` 1.0.0-beta05 (permisos `ACCESS_ADSERVICES_*` [E]), `androidx.webkit` 1.12.1, `androidx.browser` 1.8.0, `androidx.datastore`, **Kotlin stdlib 2.1.0 y coroutines 1.8.0** | Pide el App ID de AdMob (`ca-app-pub-…`) en `AppLovinSettings` (el post-proceso lo escribe en el manifiesto): es del dueño. Añade `OPTIMIZE_INITIALIZATION` y `OPTIMIZE_AD_LOADING` |
| Unity Ads | `com.applovin.mediation.adapters.unityads.android` **4210001.0.0** (22-10-2026, tiene un día: anterior estable a confirmar) | `com.applovin.mediation:unityads-adapter:4.21.0.1` | `com.unity3d.ads:unity-ads` 4.21.0 → `okhttp` 3.12.13, `protobuf-kotlin-lite` 3.25.6, `work-runtime-ktx` 2.7.0, `play-services-cronet` 18.0.1, `androidx.startup`, `lifecycle-process`, Kotlin 2.1.21, y **`com.unity3d.coherence:coherencelib` con rango `[0.2.0,0.3.0)`** (versión no fija) | El rango de versión; WorkManager y `androidx.startup` (inicialización al arrancar el proceso, como el proveedor de AppLovin) |
| Meta Audience Network | `com.applovin.mediation.adapters.facebook.android` **6220001.0.0** (02-10-2026) | `com.applovin.mediation:facebook-adapter:[6.22.0.1]` | `com.facebook.android:audience-network-sdk` 6.22.0 → `play-services-basement` 18.5.0, `androidx.browser` 1.9.0 | Meta exige su propio flag de procesado limitado de datos y no sirve a menores [E] |
| Mintegral | `com.applovin.mediation.adapters.mintegral.android` **17019100.0.0** (20-10-2026) | `com.applovin.mediation:mintegral-adapter:17.1.91.0` y `androidx.recyclerview:recyclerview:1.2.1` | `com.mbridge.msdk.oversea:mbridge_android_sdk` 17.1.91 desde **un repositorio Maven propio** (`https://dl-maven-android.mintegral.com/repository/mbridge_android_sdk_oversea`) | Repositorio de terceros en el build; sus `.so` son el riesgo habitual de 16 KB (08 §7 #1) |

SHA-256 de los `.tgz` UPM consultados (solo para la propuesta; no están en el repositorio): Google `494083597202fd3a4d0eec85c9ddf26f206ced6691f4dabbe051a16099bc29f3`, Unity Ads `5041d030e4520aab00f59cd16f388c3cdc3e3b7c3abd58b1cff1bbd60d326eb8`, Meta `0bbdb9b0b3a54b58f48bc981789de2de14b897eb3f455ec932923c81d650443b`, Mintegral `80ade8b5ab715b070b8fc200209e84b8db8939d559ad8e90d30df5df99c06804`. Se integrarían como el plugin: `.tgz` en `Packages/applovin/` con `file:`, uno por uno y con `check-manifest`, `check-16kb` y `size-gate` tras cada uno.

## 8. Manifiesto, permisos, 16 KB y tamaño

Medido el 2026-10-23 en el escritorio x64 (`unity-build.ps1 -Method Dev` y `-Method Release -Rehearsal`, sin la configuración de MAX), con el informe de fusión de Gradle (`manifest-merger-blame-*-report.txt`).

**Lo que declara `com.applovin:applovin-sdk:13.6.4`** en su manifiesto:

| Elemento | Qué es | Qué se hizo |
|---|---|---|
| `android.permission.INTERNET`, `ACCESS_NETWORK_STATE`, `VIBRATE` | Red y vibración | Ya aprobados (D-036, D-037; `VIBRATE` en la lista blanca) |
| `com.google.android.gms.permission.AD_ID` | ID de publicidad de Google en Android 13+ (también lo trae `play-services-ads-identifier`) | **Quitado** (`tools:node="remove"`, ya estaba por D-039). Pregunta al dueño |
| `com.android.vending.CHECK_LICENSE` | Licencias de Google Play (LVL): AppLovin lo usa para leer datos de la instalación de Play [E] | **Quitado**. Pregunta al dueño |
| `android.permission.ACCESS_WIFI_STATE` | Estado de la wifi, para Open Measurement (verificación de visibilidad de los anuncios) | **Quitado**. Pregunta al dueño |
| Proveedor `com.applovin.sdk.AppLovinInitProvider` (`initOrder` 101) | Corre al iniciar el proceso, antes de cualquier línea de C#: crea el rastreador del ciclo de vida de AppLovin y, en un hilo, lee el ID de publicidad y el App Set ID (comprobado con `javap` en 13.6.4) | **Quitado** (`tools:node="remove"` en `LauncherManifest.xml`): contradice «MAX solo tras consentir» (08 §2.3). MAX se inicia desde `InitializeSdk`. [NV en el teléfono: que los anuncios cargan igual] |
| Actividades de anuncios y del depurador de mediación, servicios `FullscreenAdService` y `AppRestartDuringAdDetectionService`, `queries` de Custom Tabs | Funcionamiento del SDK | Se quedan |

UMP 4.0.0 no añade permisos. El manifiesto fusionado de `dev.apk` y de `rehearsal.aab` queda con los mismos permisos que antes de MAX: `INTERNET`, `ACCESS_NETWORK_STATE`, `VIBRATE`, `BILLING`, `WAKE_LOCK`, `BIND_GET_INSTALL_REFERRER_SERVICE`, `POST_NOTIFICATIONS` y el propio `DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION` (`ci/check-manifest.ps1 -Mode dev` en verde).

**Compuertas (2026-10-23):**

| Artefacto | Tamaño | `check-manifest -Mode dev` | `check-release-classes` | `check-no-unity-iap` | `check-16kb` | `size-gate` |
|---|---|---|---|---|---|---|
| `dev.apk` | 72,23 MB | OK | lleva `firebase-appcheck-debug` (esperado en desarrollo) | OK | OK (19 bibliotecas) | — |
| `rehearsal.aab` | 55,77 MB | OK | OK (3 `.dex`, 19.679 clases) | OK | OK (19 bibliotecas) | OK (≤ 80 MB; sin `bundletool`, solo el archivo) |

- **16 KB:** la única biblioteca nativa nueva es `libapplovin-native-crash-reporter.so` (843 KB sin comprimir, arm64), alineada a 16 KB. Es el **informe de fallos nativo de AppLovin**: dato de diagnóstico para Data Safety.
- **Peso de MAX:** unos 2-3 MB en el AAB (las clases de `applovin-sdk`, 1,6 MB en el `.jar`, el `.so` y sus recursos) [E: no hay un build de referencia del mismo commit sin MAX].

## 9. Data Safety

Fila de AppLovin MAX en 08 §13.3 al día. La web (`infra/web/src/content/*/privacy.html` y `terms.html`) dice lo que hace el código: anuncios de AppLovin MAX tras la edad y el consentimiento, nunca con `<13`, no personalizados con 13-17, UMP en el EEE/UK/CH, y las redes mediadas como «todavía no». `app-ads.txt` sigue con los marcadores.

## 10. Lo que hace el dueño

1. Cuenta de AppLovin, la app `com.amazonfill.dev` (pruebas) y, tras el dictamen, la definitiva; la clave del SDK y dos unidades (recompensado e intersticial) de Android en `Assets/_LocalSecrets/applovin-max.json` o en los secretos del CI.
2. AdMob › Privacidad y mensajes: el mensaje del RGPD (UMP) para la app; y «Contenido máximo» = G. **Hecho el 2026-10-25** (cobra AUTOMATION AI S.A.S con el perfil de pagos de Play; añadido de D-048). Ninguno de estos datos es secreto:

   | Dato | Valor |
   |---|---|
   | ID de editor | `pub-2916158449453694` |
   | App ID (Android, «Tintalú», sin tienda vinculada) | `ca-app-pub-2916158449453694~9925643748` |
   | Bloque recompensado `Tintalu Rewarded MAX` (sin SSV de AdMob) | `ca-app-pub-2916158449453694/2877105387` |
   | Bloque intersticial `Tintalu Interstitial MAX` (sin ofertas de socios) | `ca-app-pub-2916158449453694/7187673451` |

   - **Mensaje del RGPD** «Tintalú RGPD»:
     - en español, inglés y portugués;
     - botones Consentir, Gestionar opciones y No consentir, este en todos los países;
     - sin el «Cerrar» que no da consentimiento;
     - socios publicitarios: la lista de uso habitual (198, con Google y AppLovin Corp.).
   - **Mensaje de los estados de EE. UU.:** en inglés y español (es-419).
   - **Clasificación del contenido:** G.
   - **Sin mediación en AdMob:** media MAX.
   - **Pendiente:**
     - vincular la tienda cuando la app esté en Play;
     - la línea de AdMob en `app-ads.txt` (`google.com, pub-2916158449453694, DIRECT, f08c47fec0942fa0`), que se publica con la de AppLovin;
     - el App ID en el juego y los bloques en MAX, con la integración del adaptador de AdMob (§7).
3. ~~Panel de MAX: la URL S2S del SSV (`/v1/ssv` de la API de dev) y su clave en Secrets Manager.~~ Ya no aplica (D-049): el SSV es el de AdMob (`infra/README.md` §5 bis).
4. Decidir los permisos de §8 y la lista de adaptadores de §7.
5. Probar en el teléfono con unidades de prueba (modo de prueba de MAX, dispositivo de prueba en el panel), nunca con clics propios en anuncios reales.
