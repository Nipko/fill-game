# Estado del equipo Mac: Tintalú en iOS

Encargo: `docs/dev/traspaso/mac-ios.md` (D-067). La Mac mini (Apple silicon, 24 GB) empuja ramas `mac/*`; el escritorio las integra.

## Resumen

- **M0 hecha** (2026-10-31, rama `mac/m0-setup`): la Mac tiene Xcode 27.2, Unity 6000.3.25f1 arm64 con iOS Build Support, .NET 10.0.403, CocoaPods 1.17.0, PowerShell 7.6.9, git-lfs y gh. El proyecto compila con el Editor de macOS; `dotnet test`, EditMode y PlayMode en verde (con el arreglo de un test de audio). Informe: `docs/dev/mac-setup.md`.
- **M1 hecha** (2026-11-01, rama `mac/m1-hola-iphone`, sobre `mac/m0-setup`): Tintalú corre en el iPhone 16 Pro Max (iOS 26.6) por cable con el Apple ID gratuito. El dueño jugó los niveles 1-10; la Dynamic Island no tapa nada; el interruptor de silencio funciona. Pasos y arreglos: `docs/dev/mac-setup.md` §6.
- **Fallo abierto, del código común (no de iOS): Rosa sale como un cuadro blanco en el inicio** tras cerrar el banner de un mundo (abajo, M1). Espera la decisión del dueño o del integrador.
- **Rosa en blanco: arreglado** en `mac/fix-rosa-banner` (`043d0b0b`, desde la integración), con test y comprobado en el iPhone.
- **M3 en curso** (rama `mac/m3-storekit`, sobre `mac/m2-sistema`): el cliente de compras con StoreKit 2 por el mismo camino que Play (`docs/dev/iap.md` §8). Compila y conecta en el iPhone; la compra real espera a un usuario *Sandbox* (cuenta de pago) porque Xcode no activa el `.storekit` local en el iPhone. El iPhone con el backend real no vende hasta que el servidor verifique el App Store. Falta: esa verificación en el servidor, y los anuncios de iOS (ATT, SKAdNetwork, UMP).
- **M2 hecha** (rama `mac/m2-sistema`, sobre `mac/m1-hola-iphone`): hápticos, escala de texto, «Reducir movimiento», ajustes de la app, notificaciones locales, país desde el App Store, icono con sus apariencias de iOS y Firebase, todo probado en el iPhone.

## Para retomar (2026-11-01)

1. **Siguiente en M3:** decidir cómo verifica el servidor las compras del App Store (`iap.md` §8) y los anuncios de iOS. Guiones para compilar e instalar (locales, ignorados): `Logs/ios-scripts/ios-setup.sh` y `ios-all.sh <equipo> <udid> ['{"af_dev_push":"2"}']`.
2. **La firma del Apple ID gratuito caduca a los 7 días** (instalado el 2026-11-01). Para reinstalar: `mac-setup.md` §6 (exportar con `BuildIos.ExportXcode` y `xcodebuild` + `devicectl` desde una terminal normal, con el equipo de firma del dueño).
3. **En la Mac, solo local:** la rama `mac/prueba-rosa` (M1 + el arreglo de Rosa, solo para probar en el iPhone) y dos `stash`: el checkout a medias de cuando `git-lfs` no estaba en el PATH (sin valor) y los huecos de iconos de iOS que Unity añade a `ProjectSettings.asset`. Se pueden borrar.

## M2: plataforma (en curso) — 2026-11-01

Rama `mac/m2-sistema` desde `mac/m1-hola-iphone`.

- **Plugin propio `Assets/_Project/Plugins/iOS/AfPlatform.mm`** (Objective-C, solo UIKit, sin permisos ni SDK): `AfHapticImpact` (`UIImpactFeedbackGenerator`) y `AfReduceMotionEnabled` (`UIAccessibilityIsReduceMotionEnabled`).
- **`IosHaptics`** (`AF.Audio.Unity`): ligero, medio y corto (ligero al 60 %), los tres de `HapticStrength`; lo da `AndroidHaptics.ForPlatform` en el iPhone.
- **`IosSystemScale`** (`AF.UI`): escala de fuente con `AccessibilitySettings.fontScale` (Dynamic Type) y «Reducir movimiento» como escala de animaciones 0; lo da `AndroidSystemScale.ForPlatform` en el iPhone.
- **`AndroidAppSettings.OpenNotificationSettings`** en el iPhone abre `app-settings:` (la ficha de la app en Ajustes).
- **Idioma:** sin cambios; en el iPhone la cultura de .NET ya da `es-CO` (`AF_STATE loc`, `device_src: culture`).
- **Probado por el dueño en el iPhone (2026-11-01):** hápticos al sellar y al ganar, Tamaño del texto más grande y Reducir movimiento.
- **Notificaciones locales** (`IosNotificationScheduler`, en `AF.Platform.Notifications`, con la parte de iOS del mismo `com.unity.mobile.notifications` 2.4.3: sin paquetes nuevos): hora UTC (disparador de calendario), permiso solo en `RequestPermission` (un rechazo en iOS es `Blocked`), entregados y abierto como en Android (`ScheduledNotificationLog` y `GetLastRespondedNotification`, con una marca para no contar dos veces el mismo). `DeviceNotifications` lo crea en el iPhone.
- **Ajustes del paquete en iOS** (`ApplyIosDefaults`, que también los valida): «Request Authorization on App Launch» pasa de `True` a `False` (06 §8.1: el permiso solo tras «Avísame» o desde Ajustes), y siguen apagadas las notificaciones remotas, su permiso al abrir y el entitlement Time Sensitive. Solo cambia la parte de iOS de `ProjectSettings/NotificationsSettings.asset`.
- **Extras de desarrollo en iOS:** `LaunchOptions.ReadIntentInt/Bool/String` leen en el iPhone las variables de entorno del arranque con el mismo nombre que los extras de Android, solo en builds de desarrollo: `xcrun devicectl device process launch --environment-variables '{"af_dev_push":"2"}' …`. Con `af_dev_push=2` el dueño vio el diálogo de permiso, el aviso a los 60 s y el juego abierto al tocarlo.
- **País legal desde el App Store** (D-057: en iOS ocupa el sitio del país de la cuenta de Play): plugin propio `Plugins/iOS/AfStorefront.swift` con StoreKit 2 (`Storefront.current`, de la plataforma: sin SDK ni permisos). StoreKit 1 (`SKPaymentQueue.storefront`) daba `nil` al arrancar sin observador de transacciones, comprobado en el iPhone. Como `Storefront.current` es asíncrono, el plugin deja el alfa-3 en las preferencias de la app (las de `PlayerPrefs` en iOS, clave `af_storefront_alpha3`) y `IosStorefrontCountry.SaveInto`, desde `PlayerPrefsPlayAccountCountry.ForPlatform`, lo pasa a alfa-2 con `CountryCodes` (los 249 códigos ISO 3166-1, comprobados uno a uno contra ICU con `uloc_getISO3Country`; `CountryCodesTests`, 22 casos) y lo guarda: vale **desde el arranque siguiente**, como el de Play en Android. En el iPhone: primer arranque `country_source: region` (CO), segundo `tienda=USA -> US`, `country_source: play`. **La cuenta del App Store del dueño es de EE. UU.**: con D-057 el iPhone de pruebas cuenta como jugador de EE. UU.
- **Icono definitivo de iOS** (lo que D-045 dejaba pendiente): el claro es el de siempre (`app-store-1024.png`); el oscuro, el mismo dibujo sin el cielo ni la selva sobre transparente (`generar.py`, `ios_oscuro`); el tintado, la monocroma oficial a tamaño completo (`ios_tintado`). `exportar.py --ios` exporta solo esos dos PNG (y ya encuentra Chrome en la Mac); los demás SVG salen idénticos. `BuildIos` monta las tres apariencias en el catálogo de Xcode (`luminosity` `dark` y `tinted`). El dueño eligió hacer variantes propias en vez de dejar que iOS las genere y las aprobó en el iPhone (oscuro).
- **Firebase en iOS** (2026-11-03; detalle en `docs/dev/firebase-cliente.md` §9): app de iOS `com.amazonfill.dev` registrada en `fill-dev-e75a2` (con la sesión de Chrome del dueño, a petición suya); el dueño descargó el plist a `Assets/_LocalSecrets/` (ignorado; los agentes ni lo listan). Las puertas `UNITY_ANDROID && !UNITY_EDITOR` de Auth, tokens, Remote Config, Analytics y Crashlytics pasan a `(UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR`; en `FirebaseBoot`, la lectura del plist con `NSBundle` (Xcode lo deja en binario: la primera versión, que lo leía como texto, daba `not_configured`) y App Attest en release; `AfCrashlytics.m` para la banda <13; las claves de consentimiento del `Info.plist` en `BuildIos`. En el iPhone: Firebase listo y `AF_EVENT` reales tras el consentimiento. Pendiente: el token de depuración de App Check de iOS cuando se pruebe la Liga o las compras.
- **Revisión (code-reviewer, 2026-11-03):** sin cambios en Android; claves del `Info.plist` y P/Invoke correctos. Corregido lo que bloqueaba: `IosConsentDefaultsTests` (paridad con los `meta-data` de `LauncherManifest.xml` y `SetPlistBool`) y `ApplyConsentDefaults` falla cerrado si falta el `Info.plist`.
- **Pendiente para el dueño o el integrador:**
  - **IDFV** apagado siempre en iOS (`GOOGLE_ANALYTICS_IDFV_COLLECTION_ENABLED=false`, [P]; no hay API que lo encienda tras el consentimiento): confirmar y llevarlo a `decisiones.md`;
  - **la app de iOS se registró en la consola de Firebase desde el navegador** a petición expresa del dueño: es una excepción a la regla dura 9 que conviene anotar en `decisiones.md`;
  - **App Attest** (release) necesita la capacidad `com.apple.developer.devicecheck.appattest-environment` en el proyecto de Xcode (cuenta de pago, M3-M5);
  - caso raro de Crashlytics en iOS: con «Enviar informes de fallos» apagado en una sesión en que Firebase quedó `Unavailable`, si en el arranque siguiente Firebase arranca después de `Stop()`, iOS no reaplica el apagado (Android sí, porque `FirebaseInitProvider` arranca solo); propuesta: un `FirebaseBoot.WhenStarted` pasivo;
  - valorar `GOOGLE_ANALYTICS_REGISTRATION_WITH_AD_NETWORK_ENABLED=false` (SKAdNetwork) hasta decidir la atribución de anuncios (M3);
  - `Assets/Editor Default Resources/CrashlyticsSettings.asset` (lo crea el editor de Crashlytics al pasar a iOS; sin secretos) sigue sin commitear: decidir si se versiona o se ignora.
- **M2 hecha.**
- Android no cambia: lo nuevo va detrás de `UNITY_IOS` o de `RuntimePlatform.IPhonePlayer`.
- **Verificación:** EditMode 5.018 con el país de la tienda (5.011 pasan, 7 omitidos, 0 fallan; 4.996 con las notificaciones); PlayMode 2.612 (2.235 pasan, 377 omitidos, 0 fallan) con los hápticos y la escala; export, `xcodebuild` firmado e instalación en el iPhone, correctos.

## M1: «Hola, iPhone» — 2026-11-01

Rama `mac/m1-hola-iphone` desde `mac/m0-setup`.

- **Código** (solo `Assets/_Project/Editor/CI/`, que no entra en el juego):
  - `ProjectSetup.ApplyIosDefaults` y `ValidateIos` (`ProjectSetup.Ios.cs`; `ProjectSetup` pasa a `partial`): `com.amazonfill.dev`, iOS 15, solo iPhone, IL2CPP ARM64, Metal, stripping High, firma automática sin equipo en el repositorio. `ProjectSettings/ProjectSettings.asset` lleva el resultado: solo cambian claves de iPhone.
  - `BuildIos.ExportXcode` (`Builds/iOS/Xcode`, Development) y su post-proceso para Xcode 27.2: el iOS mínimo de los Pods, Crashlytics sin `GoogleService-Info.plist`, la sandbox de scripts, el App ID de AdMob de ejemplo y el icono provisional (`mac-setup.md` §6).
- **Sin ramas `UNITY_IOS` en el juego:** todo lo nativo (Play Billing, Google Mobile Ads, Firebase, Play Games, notificaciones, hápticos, edad, país) ya va detrás de `#if UNITY_ANDROID && !UNITY_EDITOR`, así que en iOS entran solas las implementaciones falsas. La consola del iPhone lo confirma: `[AF.Ads] proveedor: editor`, sin compras ni nube, y `AF_EVENT_UNSENT` en la analítica.
- **En el iPhone** (consola con `devicectl --console`; capturas en `Logs/device-ios/`, ignorado): intro de la marca, 700 niveles en 196 ms, es-419 por el teléfono en es-CO, consentimiento con la pantalla de edad, niveles 1-10 jugados por el dueño, Mapa e inicio.
- **Observaciones:**
  1. **Rosa en blanco en el inicio (fallo del código común).** `WorldBannerView` llama a `ProceduralArt.ReleaseBannerArt()` al cerrarse, que destruye el sprite `_rosa`, y ese sprite es el mismo `ProceduralArt.Rosa` que usan el inicio (`HomeVoyage.Apply`), el Mapa, la pausa y `SkipUi`. El inicio queda apuntando a un sprite destruido y uGUI pinta un rectángulo blanco hasta que la app se reinicia. Reproducido con una instalación limpia: banner «Mundo 2» (`world_banner_closed`, t = 261 s) y, al volver al inicio, el cuadro junto a la canoa de Capi. En Android tiene que pasar igual al cerrar el banner del mundo 2. **No arreglado en esta rama** (cambia el comportamiento de Android; el encargo pide no tocarlo sin el integrador). Arreglo recomendado [P]: que el banner use su propia copia de Rosa (o que `ReleaseBannerArt` no destruya el sprite compartido), con un test PlayMode que cierre el banner y mire el sprite de `HomeVoyage`.
  2. **El arte por código tarda en el primer arranque:** `AF_ART procedural 3649 ms` (el panorama y el muelle, 590 y 887 ms); desde el segundo, con la caché en disco, 772 ms. Arranque total ≈ 8,9 s la primera vez.
  3. **iOS pide permiso para buscar dispositivos en la red local** al abrir: es la conexión del Profiler de Unity de los builds Development (`Player connection … UDP target info broadcast`). Se puede denegar; no sale en release.
  4. 60 fps (08 §5.4); no se probó a 120 Hz.
- **Comandos y resultados:**
  - `ApplyIosDefaults`: `[AF.ProjectSetup] OK (iOS) … bundleId=com.amazonfill.dev … iosMinimo=15.0, dispositivos=iPhoneOnly, backend=IL2CPP, graficos=Metal`.
  - `BuildIos.ExportXcode`: código 0, `resultado=Succeeded` (los 3 «errores» son el aviso del App ID de iOS de Google Mobile Ads).
  - `xcodebuild … CODE_SIGNING_ALLOWED=NO build` (genérico): `** BUILD SUCCEEDED **`; firmado para el iPhone: código 0; `devicectl device install` y `process launch`: instalado y abierto.
  - `pwsh -NoProfile -File scripts/unity-test.ps1 -Mode EditMode -Runner batchmode -UnityExe <…>`: 4.996 (4.989 pasan, 7 omitidos, 0 fallan).
  - `… -Mode PlayMode …`: 2.612 (2.235 pasan, 377 omitidos, 0 fallan).
- **Fuera del commit:** `ProjectSettings/GvhProjectSettings.xml` (EDM4U) y `Assets/Editor Default Resources/CrashlyticsSettings.asset` (lo crea el plugin de Crashlytics al pasar a iOS).

## M0: la Mac lista — 2026-10-31

Rama `mac/m0-setup` desde `origin/integracion-fase1-fase2` (`f10cba52`).

- **Instalado** (con el dueño; detalle en `mac-setup.md` §2): Homebrew 7.0.11; `git-lfs`, `gh`, `cocoapods` y `powershell` con Homebrew (el .NET SDK 10.0.403 llegó como dependencia de PowerShell); Unity Hub 3.22.5 con `brew install --cask`; el Editor 6000.3.25f1 (`e1dba0a9aba4`) con iOS Build Support por la CLI del Hub. Xcode 27.2 lo puso el dueño.
- **Decisión [P] del dueño:** Xcode 27.2 en lugar de 26 (cumple el mínimo de Apple). Se comprueba en M1 que el proyecto exportado compile en él.
- **Comandos y resultados:**
  - `dotnet build dotnet/AmazonFill.slnx -warnaserror`: 0 avisos, 0 errores.
  - `dotnet test dotnet/AmazonFill.slnx`: 1.002 (996 pasan, 6 omitidos, 0 fallan).
  - `pwsh -NoProfile -File scripts/unity-compile.ps1 -Runner batchmode -UnityExe /Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity -TimeoutSec 3600`: OK, 0 `error CS` (17 avisos de nulabilidad ya existentes).
  - `pwsh -NoProfile -File scripts/unity-test.ps1 -Mode EditMode -Runner batchmode -UnityExe <…> -TimeoutSec 7200`: 4.996 (4.989 pasan, 7 omitidos, 0 fallan).
  - `… -Mode PlayMode …`: primera pasada 2.612 (2.234 pasan, 377 omitidos, **1 falla**); tras el arreglo, 2.612 (2.235 pasan, 377 omitidos, 0 fallan).
- **Arreglo de un test:** `AudioFocusPlayTests.Alarm_PausesMusicAndEffects_Gain_BringsThemBack_Duck_LowersTheMusic` fallaba siempre en la Mac por una carrera con el fundido de 0,5 s (esperaba el 90 % y luego pedía el 100 %). Ahora espera al final del fundido. Solo cambia el test; `mac-setup.md` §3 lo explica.
- **Sin tocar:** `ProjectSettings/ProjectSettings.asset` y `ProjectSettings/GvhProjectSettings.xml`, que Unity reescribe al abrir el proyecto en la Mac (`mac-setup.md` §5), quedan fuera del commit.

## Enganches para el integrador

- Fusionar `mac/m1-hola-iphone` (incluye `mac/m0-setup`): `ProjectSetup.Ios.cs`, `BuildIos.cs` (con sus `.meta`), `ProjectSetup` como `partial` y las claves de iPhone de `ProjectSettings.asset`. En Windows sin el módulo iOS compila igual: no usa `UnityEditor.iOS.Xcode`.
- **Decidir el arreglo de Rosa en blanco** (M1, observación 1): es del código común y afecta a Android.

- Fusionar `mac/m0-setup`: `docs/dev/mac-setup.md`, este archivo y el arreglo de `Assets/_Project/Tests/PlayMode/AudioFocusPlayTests.cs`. Conviene correr `AudioFocusPlayTests` en el escritorio para confirmar que sigue en verde allí.
- Para `CLAUDE.md` (cuando el integrador lo crea oportuno): en la Mac, los scripts de `scripts/` funcionan con `-Runner batchmode -UnityExe <ruta del Editor de macOS>`.
