# Estado del equipo Mac: Tintalú en iOS

Encargo: `docs/dev/traspaso/mac-ios.md` (D-067). La Mac mini (Apple silicon, 24 GB) empuja ramas `mac/*`; el escritorio las integra.

## Resumen

- **M0 hecha** (2026-10-31, rama `mac/m0-setup`): la Mac tiene Xcode 27.2, Unity 6000.3.25f1 arm64 con iOS Build Support, .NET 10.0.403, CocoaPods 1.17.0, PowerShell 7.6.9, git-lfs y gh. El proyecto compila con el Editor de macOS; `dotnet test`, EditMode y PlayMode en verde (con el arreglo de un test de audio). Informe: `docs/dev/mac-setup.md`.
- **Siguiente: M1 «Hola, iPhone»**: `BuildIos.ExportXcode`, `ProjectSetup.ApplyIosDefaults`, ramas `UNITY_IOS` con las implementaciones falsas, y el juego en el iPhone 16 Pro Max por cable con el Apple ID gratuito.

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

- Fusionar `mac/m0-setup`: `docs/dev/mac-setup.md`, este archivo y el arreglo de `Assets/_Project/Tests/PlayMode/AudioFocusPlayTests.cs`. Conviene correr `AudioFocusPlayTests` en el escritorio para confirmar que sigue en verde allí.
- Para `CLAUDE.md` (cuando el integrador lo crea oportuno): en la Mac, los scripts de `scripts/` funcionan con `-Runner batchmode -UnityExe <ruta del Editor de macOS>`.
