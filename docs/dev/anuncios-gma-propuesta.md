# Propuesta: plugin Google Mobile Ads para Unity (D-049)

Estado: 2026-10-25, investigación del carril de AdMob sobre `integracion-fase1-fase2` (500587f9). **Solo propuesta**: no se ha tocado el repositorio, ni `Packages/manifest.json`, ni se ha lanzado ningún build. Decisión de origen: D-049 (AdMob directo en lugar de MAX). GDD: 08 §2.3 (arranque: nada de anuncios antes de la edad y el consentimiento, nada con <13), §7 #2 (hoy dice «sin plugin GMA»; D-049 lo retira), §10.2 (SSV).

Marcas: **[V]** verificado en el artefacto descargado (tgz, AAR, POM, bytecode con `javap`) o en la fuente oficial citada; **[E]** estimado o pendiente de comprobar en un build o en el teléfono.

Artefactos descargados (datos no fiables, solo inspeccionados) en la carpeta temporal de la sesión del escritorio del 2026-10-25 (no están en el repositorio). Estado: **propuesta pendiente de la aprobación del dueño** (D-049).

## 0. Resumen y recomendación

- **Plugin: Google Mobile Ads for Unity 11.5.0** (03-09-2026), como UPM `.tgz` desde OpenUPM (el canal que documenta Google), con `file:` en `Packages/` como Firebase y MAX. **No la 11.6.0** (13-10-2026), aunque es la última: su paquete UPM cambia la dependencia de `com.google.external-dependency-manager` (EDM4U de Google, el 1.2.189 que trae Firebase) por **`com.unity.external-dependency-manager` 2.1.0** (la EDM de Unity), que al convivir con EDM4U pide elegir una y sustituye la otra por un paquete «dummy» embebido en `Packages/`. Eso choca con Firebase 13.17.0, MAX 8.6.6 y Play Games 2.3.0, que dependen de EDM4U. [V]
- **Por Gradle:** `play-services-ads` **25.4.0** y `user-messaging-platform` **4.0.0** (la misma UMP que ya está por MAX), más `constraintlayout` 2.1.4, `lifecycle-process` 2.6.2 y `fragment` 1.7.1. [V]
- **Unity Ads (aparte):** `com.google.ads.mobile.mediation.unity` **3.21.1** (adaptador `com.google.ads.mediation:unity:4.20.1.0`, SDK `com.unity3d.ads:unity-ads:4.20.1`), la pareja que Google probó con la 11.5.0 y la última con `coherencelib` en versión fija. [V]
- **Lo que hay que neutralizar en el manifiesto** (como con MAX): `FOREGROUND_SERVICE` y `RECEIVE_BOOT_COMPLETED` (los trae WorkManager), `ACCESS_ADSERVICES_TOPICS` (nuevo), y, con Unity Ads, su inicializador de `androidx.startup` (`AdsSdkInitializer`), que corre al arrancar el proceso. `MobileAdsInitProvider` **solo valida el App ID** y se puede quedar. [V en manifiestos y bytecode; efecto en el teléfono E]
- **Sin bibliotecas nativas** en todo el árbol de GMA; con Unity Ads, una (`libunitycoherencenative.so`), alineada a 16 KB. [V]

## 1. Qué entra

| Paquete | Versión | Origen | SHA-256 del `.tgz` |
|---|---|---|---|
| `com.google.ads.mobile` (Google Mobile Ads for Unity) | **11.5.0** (03-09-2026; SDK Android `play-services-ads` 25.4.0, UMP 4.0.0) | OpenUPM, el registro que documenta Google: `https://package.openupm.com/com.google.ads.mobile/-/com.google.ads.mobile-11.5.0.tgz`. Construido desde `github.com/googleads/googleads-mobile-unity`, revisión `12a1edb48e7000e6b3531ada4bcbe23bae761065`, que es exactamente el tag `v11.5.0` [V] | `54aa0d8ea6ef33e52d527ffd93153b5ee9bc8b7384f06ae08d276e97a3cbb13b` (el `integrity` sha512 del registro y el `shasum` sha1 `0e7fbe0879502e9f16967990993b1ddd01d9aea7` coinciden) [V] |

- Instalación propuesta: `.tgz` en `Packages/googleads/` con Git LFS y `"com.google.ads.mobile": "file:googleads/com.google.ads.mobile-11.5.0.tgz"` en `Packages/manifest.json` (D-039, como Firebase y MAX). Sin registro con ámbito en el manifiesto: así la versión no se mueve sola.
- Dependencias UPM de la 11.5.0: `com.google.external-dependency-manager` **1.2.187** (manda el 1.2.189 de Firebase, como con MAX) y `com.unity.modules.androidjni` 1.0.0 (ya está). `"unity": "2022.3"`. [V `package.json`]
- Alternativa `.unitypackage` (release de GitHub `GoogleMobileAds-v11.5.0.unitypackage`, SHA-256 `770c34bb7485e8db20a888cc008a14fdc94ffe8d34b4d2ee24434a7be91e08ce`): descartada, porque mete los archivos en `Assets/` y complica el control de versión. [V hash]
- Fuente de instalación: https://developers.google.com/admob/unity/quick-start (OpenUPM CLI, registro con ámbito `com.google` o `.unitypackage`) [V].

### 1.1 Por qué no la 11.6.0 (la última)

| | 11.5.0 | 11.6.0 |
|---|---|---|
| Fecha | 03-09-2026 | 13-10-2026 (12 días) |
| `play-services-ads` | 25.4.0 | 25.5.0 (mismo árbol de dependencias en el POM: cambia solo la propia versión) |
| UMP Android | 4.0.0 | 4.0.0 |
| minSdk del plugin y del SDK | 23 | 24 (nosotros 26: indiferente) |
| Dependencia de EDM en `package.json` | `com.google.external-dependency-manager` 1.2.187 (compatible con nuestro 1.2.189) | **`com.unity.external-dependency-manager` 2.1.0** |
| Instalador automático de EDM | no | sí (`EdmDependencyInstaller.cs`: si se importa como `.unitypackage`, añade la EDM de Unity al manifiesto en el primer arranque) |
| Unity mínima | 2022.3 | 2022.3 |

- La EDM de Unity 2.1.0 (registro de Unity, SHA-256 del `.tgz` `d805846bc49f3f8b8edfc293315b442923438cdcee397f4d3ce9b7b6216f862f`) trae sus propias `Google.JarResolver.dll`, `Google.VersionHandler*.dll` e `Google.IOSResolver.dll`, guarda su elección en `ProjectSettings/ExternalDependencyManagerSettings.json` y, si se elige, **reemplaza `com.google.external-dependency-manager` por un paquete «dummy» 2.1.0 embebido en `Packages/`** («This package is disabled. The Unity External Dependency Manager is currently active»). Su documentación dice que, si EDM4U está como dependencia de otro paquete UPM (es nuestro caso: Firebase), hay que dejarlo hasta que ese paquete migre. Además quitó el modo de resolución AAR. [V en `Documentation~/get-started-with-edm.md`, `Editor/EDMInstancesHandling.cs` y `CHANGELOG.md` del tgz]
- Conclusión: la 11.6.0 obligaría a decidir y probar un cambio de gestor de dependencias que afecta a Firebase 13.17.0, MAX y Play Games, y que toca `Packages/` y `ProjectSettings/`. No aporta nada que necesitemos (Picture-in-Picture, iOS 13.11, `play-services-ads` 25.5.0). **Recomendación: 11.5.0 y revisar cuando Firebase pase a la EDM de Unity.** [V hechos; recomendación propia]
- Notas oficiales: https://github.com/googleads/googleads-mobile-unity/releases/tag/v11.6.0 y /v11.5.0 [V].

### 1.2 Issues de GitHub relevantes

| Issue | Estado | Qué es | Efecto para nosotros |
|---|---|---|---|
| [#4157](https://github.com/googleads/googleads-mobile-unity/issues/4157) | abierto | Con AGP 9 (Unity 6000.4.4+), el `namespace` `com.google.unity.ads` duplicado entre `googlemobileads-unity.aar` y `GoogleMobileAdsPlugin.androidlib` rompía el build | El mantenedor dice que lo arregla la 11.2.0. En la 11.5.0 el `.androidlib` declara `package="com.google.unity.ads.plugin"` y el AAR `com.google.unity.ads`: distintos [V en el tgz]. Unity 6000.3.25f1 saca el `namespace` del atributo `package` (así lo hace con `FirebaseApp.androidlib` en nuestro build) [V]. Falta probarlo con nuestro AGP 9.0.0 [E] |
| [#4106](https://github.com/googleads/googleads-mobile-unity/issues/4106) | abierto | `packaging_options.gradle` usa `packagingOptions` (DSL antigua); Unity 6 pide renombrarlo a `packaging` | Lo aplica el ajuste «Enable kotlinx coroutines packaging option» (por defecto encendido). AGP 9 podría rechazar la DSL antigua [E]. Si el build falla o avisa, se apaga ese ajuste (ya tenemos coroutines de Firebase sin conflicto) |
| [#4432](https://github.com/googleads/googleads-mobile-unity/issues/4432) | abierto | Framework iOS | No aplica (Android) |

## 2. Lo que trae por Gradle

EDM4U escribe en `mainTemplate.gradle` lo de `GoogleMobileAds/Editor/GoogleMobileAdsDependencies.xml` y `GoogleUmpDependencies.xml` (repositorio `https://maven.google.com/`, que ya está en `settingsTemplate.gradle`):

| Dependencia declarada | Versión | Fuente |
|---|---|---|
| `com.google.android.gms:play-services-ads` | **25.4.0** | `GoogleMobileAdsDependencies.xml` [V] |
| `androidx.constraintlayout:constraintlayout` | 2.1.4 | ídem [V] |
| `androidx.lifecycle:lifecycle-process` | 2.6.2 | ídem [V] |
| `androidx.fragment:fragment` | 1.7.1 | ídem [V] |
| `com.google.android.ump:user-messaging-platform` | **4.0.0** | `GoogleUmpDependencies.xml` [V] |

El pre-proceso `AndroidBuildPreProcessor.UpdateGmaDependency` reescribe esa línea en cada build con la constante `CurrentVersion = "25.4.0"` (o con el SDK «Next-Gen» `ads-mobile-sdk` 1.4.0 si se elige en los ajustes; por defecto **Standard**). [V código]

**Árbol de primer nivel** (POM de Google Maven, https://dl.google.com/android/maven2/com/google/android/gms/play-services-ads/25.4.0/play-services-ads-25.4.0.pom y `play-services-ads-api/25.4.0`):

| Viene de | Arrastra | Versión pedida | En el proyecto hoy | Resultado en Gradle (gana la más alta) |
|---|---|---|---|---|
| `play-services-ads` 25.4.0 | `play-services-ads-api` | `[25.4.0]` (fija) | no | 25.4.0 [V] |
| | `play-services-ads-identifier` | 18.0.0 | 17.1.0 (AppLovin) y la de Firebase | 18.0.0 [E] |
| | `play-services-appset` | 16.0.1 | 16.0.0 (AppLovin) | 16.0.1 [E] |
| | `play-services-basement` / `-tasks` | 18.9.0 / 18.2.0 | basement 18.11.0 en la caché de Gradle | 18.11.0 [E] |
| | `androidx.privacysandbox.ads:ads-adservices(-java)` | 1.0.0-beta05 | no | 1.0.0-beta05 [V POM] |
| | `androidx.webkit:webkit` | 1.12.1 | no | 1.12.1 [E] |
| | `androidx.browser:browser` | 1.8.0 | 1.4.0 (AppLovin) | 1.8.0 [E] |
| | `androidx.datastore:datastore`, `androidx.core:core` 1.10.1, `collection`, `listenablefuture` | — | core 1.15.0 | las más altas [E] |
| | `kotlin-stdlib` 2.1.0, `kotlinx-coroutines-android/core` 1.8.0 | — | Kotlin 2.2.x y coroutines 1.10.2 en la caché (Firebase) | las del proyecto [E] |
| `play-services-ads-api` 25.4.0 | `androidx.work:work-runtime` | **2.7.0** | **no** (nuevo) | 2.7.0 [V POM; E resolución] |
| | `play-services-measurement-sdk-api` | 20.1.2 | 23.2.0 (Firebase Analytics) | 23.2.0 [E] |
| | `com.google.android.play:hsdp` | **2.0.1** | **no** (nuevo; trae `appcompat-resources` 1.4.1) | 2.0.1 [V POM] |
| | `user-messaging-platform` | 4.0.0 | 4.0.0 (MAX) | 4.0.0 [V] |

**Choques con lo que ya está:**
- **Firebase 13.17.0**: solo subidas de versión menores hacia lo que ya trae Firebase (basement, measurement, Kotlin, coroutines). Ninguna bajada. [E: confirmar con `gradlew :launcher:dependencies` en el primer build]
- **MAX 8.6.6 con UMP 4.0.0**: misma UMP, sin choque. `ads-identifier`, `appset` y `browser` suben. Mientras MAX siga instalado, **conviene apagar su automatización de UMP** para que no haya dos flujos de consentimiento (MAX ya no se inicializa por D-049, así que no debería mostrarse; [E]).
- **Play Billing 9.0.0 y PGS 2.3.0**: sin dependencias comunes salvo `basement`/`base`, que ya resuelve Gradle. [E]
- El plugin trae `next_gen_exclusions.gradle` y `next_gen_resolution_strategy.gradle`, que **solo se aplican con el SDK Next-Gen** (y la segunda solo con AGP < 8.1): con Standard no tocan nada. [V `GradleProcessor.cs`]

## 3. minSdk, compileSdk y AGP

| Dato | Valor | Fuente |
|---|---|---|
| minSdk de `play-services-ads(-api)` 25.4.0 y UMP 4.0.0 | 23 | `uses-sdk` de los AAR [V] |
| minSdk del plugin (`.androidlib` y `googlemobileads-unity.aar`) | 23 | manifiestos del tgz [V] |
| Requisitos publicados | API mínima 24 o superior, target 35 o superior (la página ya habla de la 11.6.0) | https://developers.google.com/admob/unity/quick-start [V] |
| Nuestro proyecto | minSdk 26, targetSdk 36, compileSdk 36, AGP 9.0.0, Gradle 9.3.1 | `Library/Bee/.../gradle.properties` del último build [V] |
| Cambia Player Settings | Solo si minSdk < 23 (`AndroidBuildPreProcessor` lo sube); con 26 no toca nada | `AndroidBuildPreProcessor.cs` [V] |
| compileSdk que exige el SDK | No lo publica Google para 25.4.0 [no verificado]; con 36 no se espera problema [E] | — |
| AGP 9 | ver §1.2 (#4157 y #4106) [E hasta el primer build] | — |

## 4. Manifiesto: lo que añade cada AAR

**`play-services-ads-api` 25.4.0** (el que lleva todo; `play-services-ads` 25.4.0 solo declara `uses-sdk`):

| Elemento | Qué es | Propuesta |
|---|---|---|
| `INTERNET`, `ACCESS_NETWORK_STATE` | Red | Ya aprobados (D-036, D-037) |
| `com.google.android.gms.permission.AD_ID` | ID de publicidad (Android 13+). También lo declaran `play-services-ads-identifier` y Unity Ads | Hoy **quitado** en `LauncherManifest.xml` (D-039, D-042). Sin él, AdMob sirve con ID a ceros: menos ingresos, sin personalización. **Pregunta al dueño** (D-049 lo deja pendiente) |
| `ACCESS_ADSERVICES_AD_ID`, `ACCESS_ADSERVICES_ATTRIBUTION` | Privacy Sandbox | Ya **quitados** (D-039) |
| `ACCESS_ADSERVICES_TOPICS` | Privacy Sandbox, Topics API | **Nuevo: quitarlo** (tampoco está en la lista blanca de `check-manifest.ps1`) |
| `RECEIVE_BOOT_COMPLETED` con `tools:node="remove"` | Quita el que trae WorkManager | Bien. Añadir también nuestro `remove` en `LauncherManifest.xml` para no depender del orden de fusión (CLAUDE.md: fuera) [E] |
| `<queries>`: VIEW https, Custom Tabs, INSERT calendario, sms, tel | Abrir anuncios | Se quedan |
| Actividades `AdActivity`, `OutOfContextTestingActivity`, `NotificationHandlerActivity`; servicio `AdService` | Funcionamiento | Se quedan |
| Proveedor `com.google.android.gms.ads.MobileAdsInitProvider` (`initOrder` 100) | Corre al iniciar el proceso. En 25.4.0 **solo** lee los `meta-data`, comprueba que el App ID cumple `^ca-app-pub-[0-9]{16}~[0-9]{10}$` (si falta o no vale, **lanza `IllegalStateException` y la app se cierra al arrancar**) y registra en el log los flags `OPTIMIZE_*`. No inicia el SDK, ni red, ni lee el ID de publicidad | **Se puede quedar** (no contradice 08 §2.3 y protege de un build sin App ID). Quitarlo con `tools:node="remove"` también sería posible [E] [V con `javap` sobre `MobileAdsInitProvider` y su delegado `zzev`] |
| `meta-data` `OPTIMIZE_INITIALIZATION` y `OPTIMIZE_AD_LOADING` = `true` | Inicialización y carga en hilos de fondo | Se quedan (el plugin los respeta salvo que se apaguen en sus ajustes) |
| `<property android.adservices.AD_SERVICES_CONFIG>` | — | **No lo trae** 25.4.0 [V] |

**`androidx.work:work-runtime` 2.7.0** (nuevo, de `ads-api`):

| Elemento | Propuesta |
|---|---|
| `WAKE_LOCK` | Ya está (Firebase) |
| `FOREGROUND_SERVICE` | **Nuevo: quitarlo** con `tools:node="remove"`. Con targetSdk 34+ Play pide declarar los tipos de servicio en primer plano; GMA no lo necesita para anuncios [E] |
| `RECEIVE_BOOT_COMPLETED` | Fuera (ver arriba) |
| `androidx.startup.InitializationProvider` con `androidx.work.WorkManagerInitializer` | Inicializa WorkManager (su base de datos) al arrancar: no es código de anuncios. Quitarlo puede hacer fallar a GMA cuando pide `WorkManager.getInstance`. **Propuesta: dejarlo** [E] |
| Servicios `SystemAlarmService`, `SystemJobService`, `SystemForegroundService` y 8 receptores | Se quedan (no exportados) |

**Otros AAR:** `play-services-ads-identifier` 18.0.0: `AD_ID`. `play-services-appset` 16.0.1, `ads-adservices(-java)` 1.0.0-beta05 (`uses-library android.ext.adservices`, opcional), `webkit`, `browser`, `datastore`, UMP 4.0.0: nada más. `hsdp` 2.0.1: una actividad no exportada `HsdpShimActivity` y `queries` de `com.android.vending` [V; para qué la usa GMA, no documentado: E]. El `.androidlib` del plugin: `INTERNET` y `uses-library org.apache.http.legacy` (opcional). [V]

**Consentimiento y edad (08 §2.3):**
- Google: llamar a UMP (`ConsentInformation.Update`, `ConsentForm.LoadAndShowConsentFormIfRequired`, `CanRequestAds`) **antes** de `MobileAds.Initialize`, que puede precargar anuncios; los flags `tagForChildDirectedTreatment` / `tag_for_under_age_of_consent` también antes de inicializar. https://developers.google.com/admob/unity/quick-start y https://developers.google.com/admob/unity/privacy [V]
- Con eso y el proveedor inocuo, el esquema de 08 §2.3 se mantiene: nada de GMA se inicializa hasta `Ads.InitAsync` tras la edad y el consentimiento, y nunca con <13. [V en el bytecode del proveedor; E en el teléfono]
- `DELAY_APP_MEASUREMENT_INIT`: el plugin define la constante pero **no la escribe** [V `ManifestProcessor.cs`]. Como `ads-api` depende del SDK de medición (el mismo de Firebase Analytics, que ya arranca apagado por `meta-data`), propongo añadir `com.google.android.gms.ads.DELAY_APP_MEASUREMENT_INIT = true` en `LauncherManifest.xml` como refuerzo [E].

## 5. Bibliotecas nativas y 16 KB

| Artefacto | `.so` | 16 KB |
|---|---|---|
| Plugin 11.5.0 (`googlemobileads-unity.aar`, DLL gestionadas) | ninguna | — [V] |
| `play-services-ads`, `ads-api`, `ads-identifier`, `appset`, `ads-adservices(-java)`, UMP, `work-runtime`, `hsdp`, `webkit`, `browser`, `datastore` | ninguna | — [V] |
| Unity Ads: `coherencelib` 0.1.0 (§7) | `libunitycoherencenative.so` (arm64, 493 KB) | `p_align` 16384 en todos los `LOAD`: **alineada** [V, cabeceras ELF leídas] |

Las DLL del plugin son ensamblados .NET gestionados: no hay DLL nativas x64 para el Editor ARM64 [E: comprobar que el Editor ARM64 las carga]. El editor de GMA usa `GooglePlayServices` (EDM4U) [V].

## 6. Tamaño aproximado

| Bloque | `classes.jar` sin comprimir | Estimación en el AAB |
|---|---|---|
| `play-services-ads` 25.4.0 | 6,7 MB | |
| `play-services-ads-api` 25.4.0 | 1,1 MB | |
| UMP 4.0.0 (ya está por MAX) | 0,4 MB | 0 |
| `work-runtime` 2.7.0 (+ Room y SQLite) | 0,4 MB (+ ~0,5) | |
| `hsdp`, `webkit`, `browser`, plugin, `constraintlayout`, `fragment` | ~1 MB | |
| **Total GMA** | ~9-10 MB | **+4 a +6 MB** [E: R8 y la compresión; MAX con 1,6 MB de jar sumó 2-3 MB] |
| Unity Ads 4.20.1 (§7) | 4,0 MB + `.so` 0,5 MB + okhttp/protobuf ~1,5 MB | **+2 a +3 MB** [E] |

Referencia: `rehearsal.aab` = 55,77 MB el 2026-10-23 (`anuncios-max.md` §8). Con GMA y Unity Ads, unos 62-65 MB, bajo el tope de 80 MB [E]. Si se quitara MAX, unos 2-3 MB menos.

## 7. Mediación de Unity Ads (propuesta, se aprueba aparte)

| Red | Paquete UPM (versión) | Lo que pide por Gradle | Lo que arrastra (primer nivel) | Riesgos |
|---|---|---|---|---|
| Unity Ads | `com.google.ads.mobile.mediation.unity` **3.21.1** (23-09-2026, OpenUPM; tag `UnityAds/v3.21.1` de `googleads-mobile-unity-mediation`: «Built and tested with the Google Mobile Ads Unity Plugin version 11.5.0») | `com.google.ads.mediation:unity:4.20.1.0` (Google Maven) y `com.unity3d.ads:unity-ads:4.20.1` (Maven Central) | Adaptador: `play-services-ads` 25.4.0 (la misma), `kotlin-stdlib` 1.9.0, `annotation` 1.5.0. SDK: `okhttp` 3.12.13, `protobuf-kotlin-lite` 3.25.6, `work-runtime-ktx` 2.7.0, `play-services-cronet` 18.0.1 (sin `.so`), `androidx.startup` 1.1.1, `lifecycle-process`/`runtime-ktx` 2.6.1, `activity-ktx`, `core-ktx`, `webkit` 1.6.1, `datastore`, coroutines 1.10.2, Kotlin 2.1.21 y **`com.unity3d.coherence:coherencelib` 0.1.0 (fija)** | **Inicializador al arrancar:** registra `com.unity3d.services.core.configuration.AdsSdkInitializer` en `androidx.startup`, que guarda el contexto y lanza una corrutina al iniciar el proceso, antes del consentimiento. Hay que quitarlo (`<meta-data ... tools:node="remove"/>` dentro de `InitializationProvider`) y comprobar en el teléfono que el adaptador inicia Unity Ads igual [V bytecode; E efecto]. Permisos: `AD_ID`, `ACCESS_ADSERVICES_TOPICS` y `_ATTRIBUTION` (se quitan como los de arriba). Consentimiento propio de Unity Ads con `UnityAds.SetConsentMetaData("gdpr.consent", …)` del paquete [V API; uso E] |

- SHA-256 de los `.tgz` (OpenUPM; `integrity` sha512 coincide): 3.21.1 `b0213c58c0fc0ffd048a92e8ff61100f93233e3c236a6078d5f1be6c52bbf9f4`; 3.22.0 `a85144f75ea121a78e0e0e9d5603aee2046e05c5461a867bafafba9033eb2bc7`. [V]
- **Por qué no la 3.22.0** (23-10-2026, dos días): está probada con la 11.6.0, trae Unity Ads 4.21.0, y el POM de 4.21.0 pide **`coherencelib` con rango `[0.2.0,0.3.0)`** (versión no fija). 0.2.0 también está alineada a 16 KB [V], pero el rango puede cambiar sin aviso. [V POM]
- El paquete declara `"com.google.ads.mobile": "11.0.0"` como mínimo (lo cumple la 11.5.0) y trae un `build.gradle` en su raíz que Unity no usa (es el script de empaquetado de Google) [V].
- Changelog del adaptador Android: https://github.com/googleads/googleads-mobile-android-mediation/blob/main/ThirdPartyAdapters/unity/CHANGELOG.md [no leído en detalle].

## 8. SSV de AdMob (referencia para el backend)

AdMob llama por GET a la URL de SSV del bloque con la query `ad_network, ad_unit, custom_data (opcional), reward_amount, reward_item, timestamp, transaction_id, user_id (opcional)` y, al final, `signature` y `key_id`. Se firma con **ECDSA sobre SHA-256** (firma DER, en base64 URL-safe [E]) **todo lo que va antes de `&signature=`, tal como llega**. Las claves públicas están en `https://www.gstatic.com/admob/reward/verifier-keys.json` (se eligen por `key_id` y se cachean como mucho 24 h). El cliente pone `user_id` y `custom_data` con `ServerSideVerificationOptions` antes de mostrar el anuncio. Fuente: https://developers.google.com/admob/android/ssv [V salvo la codificación base64, E].

## 9. Configuración: App ID y ajustes del plugin

| Ajuste | Dónde | Cómo acaba en el build |
|---|---|---|
| App ID Android `ca-app-pub-2916158449453694~9925643748` (no es secreto) | `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`, campo `adMobAndroidAppId` (menú Assets › Google Mobile Ads › Settings). El asset lo crea `GoogleMobileAdsSettings.LoadInstance()` si no existe | `ManifestProcessor` (pre-build) escribe `com.google.android.gms.ads.APPLICATION_ID` en el `AndroidManifest.xml` del `.androidlib` del paquete, y Gradle lo fusiona. **Si el App ID está vacío, el build se para** («Android Google Mobile Ads app ID is empty») [V código] |
| `enableGradleBuildPreProcessor` (por defecto sí) | ídem | Asegura que existen `mainTemplate.gradle` y `gradleTemplate.properties` (ya existen), no toca minSdk (26 ≥ 23) y llama a `PlayServicesResolver.ResolveSync` en cada build. La entrada de Jetifier solo se aplica en Unity 2021-2022 [V código] |
| `enableKotlinXCoroutinesPackagingOption` (por defecto sí) | ídem | Añade `apply from: packaging_options.gradle` a `unityLibrary` y `launcher` (#4106) [V] |
| `disableOptimizeInitialization` / `disableOptimizeAdLoading` (por defecto no) | ídem | Dejan los flags `OPTIMIZE_*` en `true` |
| SDK Android (Standard o Next-Gen) | ídem; por defecto Standard | Standard = `play-services-ads` 25.4.0 |

- **El asset es YAML de Unity: regla dura 3.** Propongo fijarlo con un método de Editor versionado, como `MaxProjectSetup.Apply` (`anuncios-max.md` §2): cargar el asset con `AssetDatabase` y escribir los campos con `SerializedObject` (la clase es `internal`). Valores: App ID Android, Standard, optimizaciones encendidas y la opción de `packagingOptions` según salga el primer build. [propuesta]
- **No obliga a tocar `ProjectSettings/` a mano** con la 11.5.0: no hay ajustes del plugin en `ProjectSettings/` y no cambia Player Settings con minSdk 26. Sí reescribe el bloque de EDM4U de `mainTemplate.gradle` (como ya hace) y crea `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`. [V código] (Con la 11.6.0 sí aparecería `ProjectSettings/ExternalDependencyManagerSettings.json`, §1.1.)
- El pre-proceso escribe en archivos del propio paquete (`GoogleMobileAdsDependencies.xml` y el manifiesto del `.androidlib`) dentro de `Library/PackageCache`: funciona con OpenUPM, pero hay que comprobarlo con `file:` y en el CI Linux [E].

## 10. Preguntas para el dueño (con recomendación)

1. **Versión del plugin:** ¿11.5.0 (EDM4U de Google, `play-services-ads` 25.4.0) o 11.6.0 (EDM de Unity 2.1.0, `play-services-ads` 25.5.0)? **Recomiendo 11.5.0.**
2. **Lo que arrastra por Gradle** (§2), con dos dependencias nuevas que no traía nada: `androidx.work:work-runtime` 2.7.0 y `com.google.android.play:hsdp` 2.0.1. ¿Aprobadas? **Recomiendo sí** (las impone `play-services-ads-api`; no se pueden excluir sin romper el SDK [E]).
3. **`AD_ID`:** ¿se activa para los anuncios? **Recomiendo sí**, porque sin él AdMob pierde ingresos y la app ya bloquea a los <13. El ID solo se usa tras el consentimiento y con 13-17 los anuncios van no personalizados. Hay que declararlo en Play Console y en Data Safety.
4. **`ACCESS_WIFI_STATE` y `CHECK_LICENSE`:** GMA 25.4.0 no los pide [V]. **Recomiendo dejarlos fuera** (solo los pedía AppLovin).
5. **`FOREGROUND_SERVICE`, `RECEIVE_BOOT_COMPLETED` y `ACCESS_ADSERVICES_TOPICS`:** **recomiendo quitarlos** con `tools:node="remove"`, como los de D-039 y D-042.
6. **`MobileAdsInitProvider`:** **recomiendo dejarlo**: solo valida el App ID, y es mejor que la app falle en la primera prueba si falta.
7. **MAX:** ¿se queda instalado (D-049, punto 5) con su automatización de UMP apagada? **Recomiendo apagarla** para que solo haya un flujo de UMP. Quitar MAX del todo es otra decisión.
8. **Unity Ads (aparte):** ¿3.21.1 (Unity Ads 4.20.1, `coherencelib` fija) quitando su `AdsSdkInitializer`? **Recomiendo sí**, cuando Unity Ads tenga cuenta y el App ID esté integrado.

## 11. Lo que no pude verificar

- Que la 11.5.0 compile con nuestro AGP 9.0.0 / Gradle 9.3.1 (#4157 y #4106). Hace falta un build `-Method Dev` en una rama.
- Las versiones resueltas de verdad (`gradlew dependencies`): las de §2 marcadas [E] se deducen de la caché de Gradle y del último build.
- El manifiesto fusionado real (si el `remove` de `RECEIVE_BOOT_COMPLETED` de `ads-api` basta) y que los anuncios cargan con el `AdsSdkInitializer` de Unity Ads quitado.
- El compileSdk mínimo de `play-services-ads` 25.4.0 (Google no lo publica en la guía de Unity).
- El peso real en el AAB (§6).
- Para qué usa GMA `hsdp`.
