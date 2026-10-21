# Firebase en el cliente (F3-6)

Estado: 2026-10-21. Decisión: D-039 (integración del Firebase Unity SDK 13.17.0), con D-035 (backend híbrido), D-036 (`INTERNET`) y D-037 (`ACCESS_NETWORK_STATE`). Plan: `docs/dev/plan-backend-aws.md` §7 (F3-6). GDD: 08 §2.1 (principio 6), §2.3, §7 #4 y #7, §8, §10 y §13.3.

## 1. Qué entra

| Paquete | Versión | Para qué |
|---|---|---|
| `com.google.firebase.app` | 13.17.0 | Núcleo |
| `com.google.firebase.auth` | 13.17.0 | UID anónimo; su ID token autoriza el API de AWS |
| `com.google.firebase.app-check` | 13.17.0 | Token de uso limitado (Play Integrity por debajo) para `verifyPurchase` y `deleteAccount` (Q-AWS-4 (a)) |
| `com.google.firebase.analytics` | 13.17.0 | Analítica, apagada hasta el consentimiento |
| `com.google.firebase.crashlytics` | 13.17.0 | Fallos (diagnóstico; 08 §2.3 y pregunta abierta 6) |
| `com.google.firebase.remote-config` | 13.17.0 | Remote Config con los defaults compilados |
| `com.google.external-dependency-manager` | 1.2.189 | EDM4U, el que trae Firebase (08 §7 #7) |

Sin Functions ni Firestore (D-035). Los `.tgz` están en `Packages/firebase/` con Git LFS (`.gitattributes`) y se referencian con `file:` en `Packages/manifest.json`; sus SHA-256 están en D-039. `Packages/packages-lock.json` lo regenera Unity.

**Gradle.** EDM4U añade las bibliotecas Android al build parcheando las plantillas de Gradle de `Assets/Plugins/Android/` (`mainTemplate.gradle`, `settingsTemplate.gradle` y `gradleTemplate.properties`, copiadas sin cambios de las de 6000.3.25f1). Las bibliotecas (`firebase-analytics`, `firebase-auth`, `firebase-appcheck-playintegrity`, `firebase-crashlytics-ndk`, `firebase-config`…) las descarga Gradle de Google Maven en el build, como el resto del build de Android.

## 2. Dónde va `google-services.json` (lo coloca el dueño)

Regla dura 5: **ningún agente lo lee, lo crea, lo imprime ni lo commitea.**

- Ruta: **`Assets/_LocalSecrets/google-services.json`** (el de la app `com.amazonfill.dev` del proyecto `fill-dev-e75a2`; D-035).
- Lo cubren en `.gitignore` las reglas `/[Aa]ssets/_LocalSecrets/` y `/[Aa]ssets/_LocalSecrets.meta` (después de la regla que versiona los `.meta`, para que ganen), además de la regla general `google-services.json`. Compruébalo con `git check-ignore -v Assets/_LocalSecrets/google-services.json`.
- El editor de Firebase lo convierte en cada build en `Assets/Plugins/Android/FirebaseApp.androidlib/` (recursos con las claves del proyecto), también ignorado. Crashlytics deja su id de build en `Assets/Plugins/Android/FirebaseCrashlytics.androidlib/` (ignorado). EDM4U puede crear `Assets/GeneratedLocalRepo/` (ignorado).
- Prod: no hay proyecto de prod todavía (D-035, «Pendiente para prod»). Un build de release con el de dev no sirve para Play.

## 3. Qué pasa sin el archivo

El build de desarrollo **compila y funciona igual que antes de F3-6**:

- `FirebaseBoot.IsConfigured` mira en el teléfono si el build lleva el recurso `google_app_id` (sin red ni SDK). Sin él, todo va con las implementaciones falsas: `FakeBackendClient` (compras simuladas que se acreditan), `FakeRemoteConfigBackend` (defaults compilados), sin sumidero de Firebase Analytics y sin Crashlytics.
- En un build de desarrollo con una configuración que no es la de dev (`fill-dev-e75a2` y `1:528273757148:android:45d5cf0ddb2e3021c93519`) pasa lo mismo (`config_mismatch`): el API de dev rechazaría sus tokens.
- Un release sin Firebase (o sin la URL de prod, vacía hasta F5-1) usa `UnavailableBackendClient`: la tienda no vende.
- El editor (ARM64 o x64) y los tests **siempre** van con las falsas (08 §2.1, principio 6): `BackendEndpoints.Current` es vacía en el editor y el código del SDK solo se compila con `UNITY_ANDROID && !UNITY_EDITOR`.

## 4. Con el archivo (build de desarrollo)

- **Backend:** `HttpBackendClient` contra `BackendEndpoints.Dev` (`https://4tbucfarv9.execute-api.sa-east-1.amazonaws.com`; D-035, añadido de F3-1 y F3-2) con el ID token de Firebase Auth anónimo (`FirebaseUser.TokenAsync`) y, en las rutas con `integrity: true`, un token de App Check de uso limitado (`GetLimitedUseAppCheckTokenAsync`, Play Integrity). App Check va sin refresco automático.
- **Remote Config:** al arrancar se activa la caché propia (`af_rc_cache.json` en `persistentDataPath`, que se lee sin esperar a Firebase); luego se descarga en segundo plano (intervalo mínimo de 12 h) y lo descargado se aplica en el **arranque siguiente**.
- **Analytics:** el sumidero existe pero no recibe nada hasta el consentimiento (B12). El manifiesto lo deja apagado también por su cuenta (§5).
- **Crashlytics:** se enciende en el arranque (diagnóstico), sin UID.

## 5. Manifiesto y permisos

`Assets/Plugins/Android/LauncherManifest.xml` (Custom Launcher Manifest; plantilla de 6000.3.25f1 más lo de F3-6):

- `com.google.android.gms.permission.AD_ID` con `tools:node="remove"` (D-039: fuera hasta MAX).
- `firebase_analytics_collection_enabled`, `google_analytics_adid_collection_enabled` y `google_analytics_default_allow_{analytics_storage, ad_storage, ad_user_data, ad_personalization_signals}` en `false`; con el consentimiento, `FirebaseAnalyticsSink` llama a `SetAnalyticsCollectionEnabled(true)` y concede solo `analytics_storage`.
- `firebase_crashlytics_collection_enabled` en `false`: lo enciende `CrashReporting.Start()` en el arranque.

Permisos que deja el manifiesto fusionado: ver el informe de F3-6 y `ci/check-manifest.ps1`.

## 6. Código

| Ensamblado | Qué |
|---|---|
| `AF.Backend.Firebase` (`Scripts/Backend/Firebase/`) | `FirebaseBoot` (arranque único, App Check con Play Integrity antes que nada, ids públicos de dev), `FirebaseAuthTokenProvider`, `AppCheckIntegrityTokenProvider`, `FirebaseTokenProviders` |
| `AF.Backend.Unity` | `BackendFactory` usa los proveedores reales cuando existen; `BackendEndpoints.Dev` |
| `AF.Analytics.Firebase` (`Scripts/Analytics/Firebase/`) | `FirebaseAnalyticsSink` (booleanos como 0/1, Q-B7-18 (a)), `CrashReporting` |
| `AF.RemoteConfig.Firebase` (`Scripts/RemoteConfig/Firebase/`) | `FirebaseRemoteConfigBackend`, `RemoteConfigBackends`, `RcCacheFile` |

`AF.Analytics` gana `ICollectionAwareSink`: `AnalyticsService` dice el estado del consentimiento a los sumideros que recogen por su cuenta al añadirlos y en cada cambio. El arranque (`GameBootstrap.Telemetry.cs`) elige el proveedor de Remote Config, lanza la descarga, enciende Crashlytics y añade el sumidero de Firebase.

## 7. Pendiente

- El dueño: colocar `google-services.json` (§2) y, para App Check con Play Integrity en un APK instalado por `adb`, registrar la huella SHA-256 del certificado de firma de `dev.apk` en la app Android de Firebase y revisar los requisitos de Play Integrity de App Check (§8).
- B12: la pantalla de consentimiento y edad debe llamar a `AnalyticsService.SetCollectionEnabled` y, con la banda < 13, no encenderla.
- Eliminar cuenta: llamar a `FirebaseAnalyticsSink.ResetData()` (08 §13.3: `resetAnalyticsData()`); el borrado del usuario de Firebase Auth lo hace el servidor.
- Unity IAP (otra tarea): `setObfuscatedAccountId` = UID de Firebase Auth.
- Prod: proyecto Firebase de prod y su `google-services.json`.

## 8. Qué probar en el teléfono

Ver el informe de F3-6 («QUÉ PROBAR EN EL TELÉFONO»).
