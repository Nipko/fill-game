# Estado del equipo Mac: Tintalú en iOS

Encargo: `docs/dev/traspaso/mac-ios.md` (D-067). La Mac mini (Apple silicon, 24 GB) empuja ramas `mac/*`; el escritorio las integra.

## Resumen

- **M0 hecha** (2026-10-31, rama `mac/m0-setup`): la Mac tiene Xcode 27.2, Unity 6000.3.25f1 arm64 con iOS Build Support, .NET 10.0.403, CocoaPods 1.17.0, PowerShell 7.6.9, git-lfs y gh. El proyecto compila con el Editor de macOS; `dotnet test`, EditMode y PlayMode en verde (con el arreglo de un test de audio). Informe: `docs/dev/mac-setup.md`.
- **M1 hecha** (2026-11-01, rama `mac/m1-hola-iphone`, sobre `mac/m0-setup`): Tintalú corre en el iPhone 16 Pro Max (iOS 26.6) por cable con el Apple ID gratuito. El dueño jugó los niveles 1-10; la Dynamic Island no tapa nada; el interruptor de silencio funciona. Pasos y arreglos: `docs/dev/mac-setup.md` §6.
- **Fallo abierto, del código común (no de iOS): Rosa sale como un cuadro blanco en el inicio** tras cerrar el banner de un mundo (abajo, M1). Espera la decisión del dueño o del integrador.
- **Siguiente: M2 Plataforma** (Firebase iOS, notificaciones, hápticos, escala de texto, idioma, ajustes, país desde la tienda, icono definitivo).

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
