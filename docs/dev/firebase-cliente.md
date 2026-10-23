# Firebase en el cliente (F3-6)

Estado: 2026-10-21; orden del arranque con la banda <13 guardada, 2026-10-22 (§4 bis). Decisión: D-039 (integración del Firebase Unity SDK 13.17.0) y su añadido del 2026-10-21 (permisos y App Check en desarrollo), con D-035 (backend híbrido), D-036 (`INTERNET`) y D-037 (`ACCESS_NETWORK_STATE`). Plan: `docs/dev/plan-backend-aws.md` §7 (F3-6). GDD: 08 §2.1 (principio 6), §2.3, §7 #4 y #7, §8, §10 y §13.3.

## 1. Qué entra

| Paquete | Versión | Para qué |
|---|---|---|
| `com.google.firebase.app` | 13.17.0 | Núcleo |
| `com.google.firebase.auth` | 13.17.0 | UID anónimo; su ID token autoriza el API de AWS |
| `com.google.firebase.app-check` | 13.17.0 | Token de uso limitado para `verifyPurchase` y `deleteAccount` (Q-AWS-4 (a)): Play Integrity en release, proveedor de depuración en desarrollo |
| `com.google.firebase.analytics` | 13.17.0 | Analítica, apagada hasta el consentimiento |
| `com.google.firebase.crashlytics` | 13.17.0 | Fallos (diagnóstico; 08 §2.3 y pregunta abierta 6) |
| `com.google.firebase.remote-config` | 13.17.0 | Remote Config con los defaults compilados |
| `com.google.external-dependency-manager` | 1.2.189 | EDM4U, el que trae Firebase (08 §7 #7) |

Sin Functions ni Firestore (D-035). Los `.tgz` están en `Packages/firebase/` con Git LFS (`.gitattributes`) y se referencian con `file:` en `Packages/manifest.json`; sus SHA-256 están en D-039. `Packages/packages-lock.json` lo regenera Unity.

**Gradle.** EDM4U añade las bibliotecas Android al build parcheando las plantillas de Gradle de `Assets/Plugins/Android/` (`mainTemplate.gradle`, `settingsTemplate.gradle` y `gradleTemplate.properties`, copiadas de las de 6000.3.25f1 y parcheadas por EDM4U en sus bloques «Android Resolver … Start/End»). Las bibliotecas (`firebase-analytics`, `firebase-auth`, `firebase-appcheck-playintegrity`, `firebase-crashlytics-ndk`, `firebase-config`…) las descarga Gradle de Google Maven en el build, como el resto del build de Android.

## 2. Dónde va `google-services.json` (lo coloca el dueño)

Regla dura 5: **ningún agente lo lee, lo crea, lo imprime ni lo commitea.**

- Ruta: **`Assets/_LocalSecrets/google-services.json`** (el de la app `com.amazonfill.dev` del proyecto `fill-dev-e75a2`; D-035).
- Lo cubren en `.gitignore` las reglas `/[Aa]ssets/_LocalSecrets/` y `/[Aa]ssets/_LocalSecrets.meta` (después de la regla que versiona los `.meta`, para que ganen), además de la regla general `google-services.json`. Compruébalo con `git check-ignore -v Assets/_LocalSecrets/google-services.json`.
- El editor de Firebase lo convierte en cada build en `Assets/Plugins/Android/FirebaseApp.androidlib/` (recursos con las claves del proyecto) y en la copia de escritorio `Assets/StreamingAssets/google-services-desktop.json`, las dos ignoradas (con sus `.meta`). Crashlytics deja su id de build en `Assets/Plugins/Android/FirebaseCrashlytics.androidlib/` (ignorado). EDM4U puede crear `Assets/GeneratedLocalRepo/` (ignorado).
- Prod: no hay proyecto de prod todavía (D-035, «Pendiente para prod»). Un build de release con el de dev no sirve para Play.

## 3. Qué pasa sin el archivo

El build de desarrollo **compila y funciona igual que antes de F3-6**:

- `FirebaseBoot.IsConfigured` mira en el teléfono si el build lleva el recurso `google_app_id` (sin red ni SDK). Sin él, todo va con las implementaciones falsas: `FakeBackendClient` (compras simuladas que se acreditan), `FakeRemoteConfigBackend` (defaults compilados), sin sumidero de Firebase Analytics y sin Crashlytics.
- En un build de desarrollo con una configuración que no es la de dev (`fill-dev-e75a2` y `1:528273757148:android:45d5cf0ddb2e3021c93519`) pasa lo mismo (`config_mismatch`): el API de dev rechazaría sus tokens.
- Un release sin Firebase (o sin la URL de prod, vacía hasta F5-1) usa `UnavailableBackendClient`: la tienda no vende.
- El editor (ARM64 o x64) y los tests **siempre** van con las falsas (08 §2.1, principio 6): `BackendEndpoints.Current` es vacía en el editor y el código del SDK solo se compila con `UNITY_ANDROID && !UNITY_EDITOR`.

## 4. Con el archivo (build de desarrollo)

- **Backend:** `HttpBackendClient` contra `BackendEndpoints.Dev` (`https://4tbucfarv9.execute-api.sa-east-1.amazonaws.com`; D-035, añadido de F3-1 y F3-2) con el ID token de Firebase Auth anónimo (`FirebaseUser.TokenAsync`) y, en las rutas con `integrity: true`, un token de App Check de uso limitado (`GetLimitedUseAppCheckTokenAsync`). App Check va sin refresco automático.
- **App Check (D-039, añadido del 2026-10-21):** con `DEVELOPMENT_BUILD` (`BuildDev`), el proveedor de depuración (`DebugAppCheckProviderFactory`, de `firebase-appcheck-debug`, que ya trae Firebase); en release (también el ensayo `-afRehearsal`), Play Integrity. El juego nunca escribe tokens en los logs; el secreto de depuración lo escribe el SDK de Android, y solo en los builds de desarrollo, que son los únicos que instalan ese proveedor. La biblioteca `firebase-appcheck-debug` solo va en los builds de desarrollo (D-033, añadido del 2026-10-22, punto 9); ver «`firebase-appcheck-debug` fuera del release», más abajo.

### `firebase-appcheck-debug` fuera del release

Firebase la declara en `AppCheckDependencies.xml` y EDM4U la escribe en su bloque de `mainTemplate.gradle` para todos los builds. La quita `Assets/Plugins/Android/settingsTemplate.gradle`, fuera de los bloques «Android Resolver» de EDM4U: un `gradle.beforeProject` que excluye `com.google.firebase:firebase-appcheck-debug` de todas las configuraciones `release*` de Gradle de todos los módulos (`launcher` y `unityLibrary`).

- **Por qué por tipo de build de Gradle:** Unity construye la variante `debug` con Development Build (`BuildDev`: `assembleDebug`) y la `release` sin él (`BuildRelease`, con y sin `-afRehearsal`: `bundleRelease` y `assembleRelease`; también `BuildSmoke`). Es el mismo criterio que el código: `FirebaseBoot` solo toca `DebugAppCheckProviderFactory` con `DEVELOPMENT_BUILD`, que Unity define exactamente en los Development Builds. Así no hace falta ninguna propiedad ni ningún gancho de C#, y vale igual en el menú del Editor, en `unity-build.ps1` y en game-ci.
- **Por qué fuera de EDM4U:** EDM4U reescribe sus bloques de `mainTemplate.gradle` en cada resolución. La exclusión actúa al resolver el grafo, no sobre la línea que declara la dependencia, así que sigue valiendo aunque EDM4U la vuelva a escribir o cambie su versión.
- **El SDK nativo lo tolera:** `libFirebaseCppApp` intenta cachear la clase de depuración al crear App Check, pero ignora el resultado (comprobado en el desensamblado de 13.17.0: `AppCheckInternal` llama a `CacheDebugProviderMethodIds` y a `CachePlayIntegrityProviderMethodIds` sin mirar lo que devuelven). En release deja en logcat una línea de error de Firebase («Java class com/google/firebase/appcheck/debug/DebugAppCheckProviderFactory not found…») y App Check sigue con Play Integrity. Con la biblioteca desaparece también su `FirebaseAppCheckDebugRegistrar` del manifiesto fusionado.
- **Compuerta:** `ci/check-release-classes.ps1` lee los `.dex` del AAB o del APK y falla si alguno define clases de `com.google.firebase.appcheck.debug` (o de otro paquete de su lista de prohibidos en release; `-ForbidPackage` añade más). Va en el job `build-android` del CI justo después de `check-manifest`, en el ensayo y en el release. Autotest: `pwsh -NoProfile -File ci/check-release-classes.ps1 --self-test`.
- **Comprobado (2026-10-22, escritorio x64, sobre 0ad1de46):** `rehearsal.aab` 60,22 MB (antes 60,23 MB: la biblioteca pesa unos 4 KB en el AAB) y `rehearsal.apk` 65,67 MB, los dos sin clases de `com.google.firebase.appcheck.debug` (17.167 clases en 2 `.dex`) y con `check-manifest -Mode dev`, `size-gate` y `check-16kb` en verde; el manifiesto fusionado ya no lleva `FirebaseAppCheckDebugRegistrar`. `dev.apk` (77,00 MB) conserva sus 15 clases. Falta verlo en el teléfono: que el ensayo con `google-services.json` obtiene tokens de App Check con Play Integrity.

### Registrar el token de depuración de App Check (lo hace el dueño, una vez por instalación)

1. Instala `dev.apk` con `google-services.json` y abre el juego con red. La primera petición con integridad lo dispara (por ejemplo, «Eliminar cuenta y datos» en Ajustes; puede fallar esa primera vez).
2. Lee el secreto en logcat: `adb logcat -d | findstr /C:"debug token:"` (Windows) o `adb logcat -d | grep "debug token:"`. La etiqueta es el nombre completo de la clase (`com.google.firebase.appcheck.debug.internal.DebugAppCheckProvider`), así que `-s DebugAppCheckProvider` no encuentra nada (comprobado en el SM-S938B el 2026-10-22). Antes, `adb logcat -c` y abrir la app. Es un UUID: **no lo pegues en el chat, en el repositorio ni en issues**; es una credencial de dev.
3. Consola de Firebase → proyecto `fill-dev-e75a2` → App Check → Apps → app Android `com.amazonfill.dev` → menú ⋮ → **Manage debug tokens** → **Add debug token**: pega el secreto y ponle un nombre (por ejemplo, el teléfono).
4. Vuelve a probar. El secreto vive en los datos de la app: si la desinstalas o borras sus datos, sale uno nuevo y hay que registrarlo otra vez (y borrar el viejo de la consola).
- **Remote Config:** al arrancar se activa la caché propia (`af_rc_cache.json` en `persistentDataPath`, que se lee sin esperar a Firebase); luego se descarga en segundo plano (intervalo mínimo de 12 h) y lo descargado se aplica en el **arranque siguiente**.
- **Analytics:** el sumidero existe pero no recibe nada hasta el consentimiento (B12). El manifiesto lo deja apagado también por su cuenta (§5).
- **Crashlytics:** se enciende en el arranque (diagnóstico), sin UID, salvo con la banda <13 guardada (§4 bis).

## 4 bis. Orden del arranque y la banda <13 (B12; 08 §7 y §13.1 #11; 05 §7)

Con <13 la 1.0 bloquea el juego y no recoge datos. Por eso el arranque (`GameBootstrap.Start`) **lee el perfil antes de iniciar ningún SDK** y decide con `ConsentPolicy.StartsDataServicesAtBoot` (false solo con la banda <13 guardada):

1. Pantalla de arranque (un fotograma).
2. Perfil: `LoadProfile`, la firma (`VerifyBootProfile`), los extras de desarrollo y, **si no hay <13 guardado**, la fusión con la nube (`SyncProfileAtBoot`). La banda se vuelve a mirar tras la fusión.
3. Remote Config (`SetUpRemoteConfig`): los defaults compilados y la caché local (`af_rc_cache.json`, sin red ni SDK). La descarga (`FetchIfDue`, solo con un proveedor `IFetchingRemoteConfigBackend`, que es Firebase) **solo sin <13 guardado**.
4. Catálogo de niveles (lee `mech_enabled.*`, `level_overrides` y `lvl_label_override`), textos, lote de guardado.
5. `SetUpTelemetry`: bus y `AnalyticsService` (sumidero de logcat). **Sin <13 guardado:** `CrashReporting.Start()` y el sumidero de Firebase Analytics (apagado hasta el consentimiento). **Con <13:** ni lo uno ni lo otro.
6. Cartera y progreso; después `BlockedAtBoot`: con <13, `Under13Firebase` (la analítica a false en los sumideros que haya y `CrashReporting.Stop()`), cancela los avisos locales que hubiera (`DeviceNotifications.CancelAllOnDevice`; no programa ni pide nada), enseña «Pide a un adulto que instale el juego» y el arranque se para.
7. Solo sin bloqueo: anuncios, tienda y cliente del backend (`CreateStore`), pantallas, consentimiento, `ResumeBackend` y, con la edad ya decidida y no <13, Play Billing (`StartPlayBilling`). Firebase Auth anónimo y App Check solo se piden desde las llamadas al backend.

Con la banda <13 guardada, por tanto, **nunca** se llama a `FirebaseBoot` (ni a `FirebaseApp.CheckAndFixDependenciesAsync` ni a `SetAppCheckProviderFactory`): no se inician el Firebase de C#, Auth, App Check, Analytics ni la descarga de Remote Config, ni las notificaciones, Play Billing o el backend. `CrashReporting.Stop()` sin `FirebaseBoot` arrancado apaga Crashlytics en el SDK de Android por JNI (`FirebaseCrashlytics.getInstance().setCrashlyticsCollectionEnabled(false)`, que persiste) y borra los informes sin enviar (`deleteUnsentReports()`); con `FirebaseBoot` ya arrancado (al declarar <13 en el primer arranque o al corregir la edad en Ajustes) lo hace por el SDK de C#. `CrashReporting` aplica siempre la **última** petición al estar Firebase listo: un `Start()` pendiente no puede encenderlo tras un `Stop()`.

**Primer arranque, sin edad:** como antes, Crashlytics y Remote Config arrancan (sin UID) hasta que se declara la edad. Al declarar <13, `BlockSession` apaga Crashlytics y la analítica en el acto y la escena se recarga ya con <13 (paso 2).

**Lo que Firebase hace por su cuenta al iniciar el proceso** (antes de cualquier línea de C#; [E], según la documentación de los SDK de Android, sin comprobar en el teléfono): `FirebaseInitProvider` (de `firebase-common`, por fusión de manifiestos) crea el `FirebaseApp` por defecto con la configuración de `google-services.json`, y con él se inician los componentes «eager»: Crashlytics instala su manejador de fallos (también el NDK) y Analytics su servicio de medición. Ninguno envía nada mientras su recogida esté apagada: el manifiesto deja `firebase_crashlytics_collection_enabled` y `firebase_analytics_collection_enabled` en `false`, y lo que el código fija con `setCrashlyticsCollectionEnabled` y `setAnalyticsCollectionEnabled` persiste y manda sobre el manifiesto. Crashlytics apagado sigue guardando en el disco los fallos que ocurran, sin enviarlos; con <13 se borran en cada arranque en frío (`deleteUnsentReports`). Remote Config, Installations, Auth y App Check no hacen nada hasta que alguien los llama.

## 5. Manifiesto y permisos

`Assets/Plugins/Android/LauncherManifest.xml` (Custom Launcher Manifest; plantilla de 6000.3.25f1 más lo de F3-6):

- Con `tools:node="remove"`: `com.google.android.gms.permission.AD_ID` (D-039: fuera hasta MAX; con MAX integrado sigue fuera hasta que decida el dueño, D-042 Q-MAX-1, junto con `CHECK_LICENSE`, `ACCESS_WIFI_STATE` y el proveedor `AppLovinInitProvider`: `docs/dev/anuncios-max.md` §8) y, por el añadido del 2026-10-21, `android.permission.ACCESS_ADSERVICES_ATTRIBUTION` y `android.permission.ACCESS_ADSERVICES_AD_ID` (Privacy Sandbox de anuncios, de `play-services-measurement-api`) y `com.google.android.providers.gsf.permission.READ_GSERVICES` (de `com.google.android.recaptcha`, que arrastra `firebase-auth`; el acceso anónimo no lo usa).
- Aprobados y en la lista blanca de `ci/check-manifest.ps1` (D-039 y su añadido): `android.permission.WAKE_LOCK` y `com.google.android.finsky.permission.BIND_GET_INSTALL_REFERRER_SERVICE` (Install Referrer de Play), los dos de `play-services-measurement` (Firebase Analytics).
- `firebase_analytics_collection_enabled`, `google_analytics_adid_collection_enabled` y `google_analytics_default_allow_{analytics_storage, ad_storage, ad_user_data, ad_personalization_signals}` en `false`; con el consentimiento, `FirebaseAnalyticsSink` llama a `SetAnalyticsCollectionEnabled(true)` y concede solo `analytics_storage`.
- `firebase_crashlytics_collection_enabled` en `false`: lo enciende `CrashReporting.Start()` en el arranque, salvo con la banda <13 guardada (§4 bis).

Permisos que deja el manifiesto fusionado de `dev.apk`: `INTERNET`, `ACCESS_NETWORK_STATE`, `VIBRATE`, `WAKE_LOCK`, `BIND_GET_INSTALL_REFERRER_SERVICE` y el propio `com.amazonfill.dev.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION` (`ci/check-manifest.ps1 -Mode dev` en verde).

### Tamaño y compuertas (2026-10-21, escritorio x64)

| Artefacto | Antes (19caed28) | Con Firebase |
|---|---|---|
| `dev.apk` (BuildDev) | 61,98 MB | 69,30 MB |
| `rehearsal.aab` (`-afRehearsal`) | 47,34 MB; descarga de Play 46,45-46,46 MB | 53,38 MB; descarga de Play 52,37-52,42 MB |

Los dos pasan `ci/check-manifest.ps1 -Mode dev`, `ci/check-16kb.sh` (18 bibliotecas arm64, también las `libFirebaseCpp*.so` y las de Crashlytics) y, el AAB, `ci/size-gate.ps1` (≤ 80 MB).

## 6. Código

| Ensamblado | Qué |
|---|---|
| `AF.Backend.Firebase` (`Scripts/Backend/Firebase/`) | `FirebaseBoot` (arranque único, App Check con Play Integrity antes que nada, ids públicos de dev), `FirebaseAuthTokenProvider`, `AppCheckIntegrityTokenProvider`, `FirebaseTokenProviders` |
| `AF.Backend.Unity` | `BackendFactory` usa los proveedores reales cuando existen; `BackendEndpoints.Dev` |
| `AF.Analytics.Firebase` (`Scripts/Analytics/Firebase/`) | `FirebaseAnalyticsSink` (booleanos como 0/1, Q-B7-18 (a)), `CrashReporting` |
| `AF.RemoteConfig.Firebase` (`Scripts/RemoteConfig/Firebase/`) | `FirebaseRemoteConfigBackend`, `RemoteConfigBackends`, `RcCacheFile` |

`AF.Analytics` gana `ICollectionAwareSink`: `AnalyticsService` dice el estado del consentimiento a los sumideros que recogen por su cuenta al añadirlos y en cada cambio. El arranque (`GameBootstrap.Telemetry.cs`) elige el proveedor de Remote Config, lanza la descarga, enciende Crashlytics y añade el sumidero de Firebase; con la banda <13 guardada, solo lo primero (§4 bis).

## 7. Pendiente

- El dueño: colocar `google-services.json` (§2) y registrar el token de depuración de App Check (§4). Para release (tras el dictamen y con el proyecto de prod): Play Integrity enlazado en Play Console y la huella SHA-256 de la firma de Play en Firebase.
- B12: la pantalla de consentimiento y edad debe llamar a `AnalyticsService.SetCollectionEnabled` y, con la banda < 13, no encenderla.
- Eliminar cuenta: llamar a `FirebaseAnalyticsSink.ResetData()` (08 §13.3: `resetAnalyticsData()`); el borrado del usuario de Firebase Auth lo hace el servidor.
- Unity IAP (otra tarea): `setObfuscatedAccountId` = UID de Firebase Auth.
- Prod: proyecto Firebase de prod y su `google-services.json`.

## 8. Qué probar en el teléfono

Ver el informe de F3-6 («QUÉ PROBAR EN EL TELÉFONO»).
