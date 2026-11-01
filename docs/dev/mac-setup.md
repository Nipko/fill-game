# La Mac mini del equipo iOS (fase M0)

Informe de la fase M0 de `docs/dev/traspaso/mac-ios.md` (D-067): qué hay instalado en la Mac mini y si el proyecto compila y pasa sus tests con el Editor de macOS. Fecha: 2026-10-31. Rama `mac/m0-setup`, desde `origin/integracion-fase1-fase2` en `f10cba52`.

## 1. La máquina

| | |
|---|---|
| Equipo | Mac mini, Apple silicon (arm64), 24 GB de RAM, 460 GB de disco (368 GB libres tras instalar) |
| Sistema | macOS 27.2 (26B123) |
| Ruta del proyecto | `~/Desktop/projects/fill-game` (sin espacios) |
| iPhone de pruebas | iPhone 16 Pro Max (se conecta en M1) |

## 2. Versiones instaladas

| Herramienta | Versión | Cómo se instaló |
|---|---|---|
| Xcode | **27.2** (27B52), con la plataforma de iOS | App Store (el dueño); licencia aceptada |
| Unity Hub | 3.22.5 | `brew install --cask unity-hub` |
| Editor de Unity | **6000.3.25f1** (`e1dba0a9aba4`), arm64 nativo, en `/Applications/Unity/Hub/Editor/6000.3.25f1` | CLI del Hub: `install --version 6000.3.25f1 --changeset e1dba0a9aba4 --architecture arm64 --module ios` |
| iOS Build Support | 6000.3.25f1 | con el Editor (arriba) |
| Licencia de Unity | Personal, en el Hub con la cuenta del dueño | el dueño |
| .NET SDK | **10.0.403** (cumple `global.json`: 10.0.401 con `latestFeature`) | Homebrew, como dependencia de `powershell` |
| PowerShell | 7.6.9 | `brew install powershell` |
| CocoaPods | 1.17.0 | `brew install cocoapods` |
| Homebrew | 7.0.11 | instalador oficial (pidió la contraseña del dueño) |
| git | 2.54.0 (Apple Git-157) | Xcode |
| git-lfs | 3.8.1 (`git lfs install`; 480 archivos LFS descargados) | `brew install git-lfs` |
| gh | 2.103.3, sesión como `Nipko` (HTTPS, llavero) | `brew install gh` y `gh auth login --web` |
| Python | 3.9.6 del sistema, **sin** `jsonschema` | — (ver §5) |

**Decisión [P] (dueño, 2026-10-31): Xcode 27.2 en lugar de Xcode 26.** `mac-ios.md` §1 pide Xcode 26 con el SDK de iOS 26 (el mínimo de Apple para subir desde el 2026-04-28); el 27.2 lo cumple y, además, es el que hace falta si el iPhone tiene iOS 27. Sin verificar todavía: que Unity 6000.3.25f1 exporte un proyecto que compile en Xcode 27.2; se ve en M1. Si falla, se instala Xcode 26 al lado.

## 3. Verificación

| Comando | Resultado |
|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | correcto: 0 avisos, 0 errores |
| `dotnet test dotnet/AmazonFill.slnx` | **1.002: 996 pasan, 6 omitidos, 0 fallan** (1 min 6 s); igual que en el escritorio |
| `pwsh -NoProfile -File scripts/unity-compile.ps1 -Runner batchmode -UnityExe <Unity.app/Contents/MacOS/Unity> -TimeoutSec 3600` | **OK: compila**, 0 `error CS`; 17 avisos de nulabilidad (CS8602, CS8604, CS8625) en código ya existente: `GameScreen.TutorialTips.cs` y cuatro tests. Primera importación de `Library` incluida |
| `pwsh -NoProfile -File scripts/unity-test.ps1 -Mode EditMode -Runner batchmode -UnityExe <…> -TimeoutSec 7200` | **4.996: 4.989 pasan, 7 omitidos, 0 fallan**, 151 s (2 min 40 s en total) |
| `pwsh -NoProfile -File scripts/unity-test.ps1 -Mode PlayMode -Runner batchmode -UnityExe <…> -TimeoutSec 7200` | Primera pasada: **2.612: 2.234 pasan, 377 omitidos, 1 falla** (21 min 40 s). Con el arreglo del test (abajo): **2.612: 2.235 pasan, 377 omitidos, 0 fallan** (21 min 26 s; código 0) |
| `… -Mode PlayMode -Filter AudioFocusPlayTests …` (×3 antes y ×3 después del arreglo) | antes: 3 de 3 fallan igual; después: 4/4 pasan, 3 de 3 |

`<…>` es `/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity`.

**El fallo de PlayMode y su arreglo.** `AudioFocusPlayTests.Alarm_PausesMusicAndEffects_Gain_BringsThemBack_Duck_LowersTheMusic` (observación 10 de la jornada 6) fallaba siempre en la Mac: «vuelve al 100 %», esperado 0,708 ± 0,02 y medido 0,638. Era una carrera del test, no del juego: tras la alarma esperaba a que la música pasara del 90 % del fundido de 0,5 s (`AudioMix.AdFadeMs`) y, dos fotogramas después de quitar la atenuación, pedía el 100 %. En la Mac el Editor da más fotogramas por segundo, el test sale justo al 90 % (0,638 = 0,90 × 0,708) y el fundido no ha terminado. El arreglo, solo en el test, espera al final del fundido (`> 0.999f * AudioMix.MusicLevel`). El comportamiento del juego, en Android y en iOS, no cambia.

**Recuento frente al escritorio.** `CLAUDE.md` da 4.948 en EditMode y 2.547 en PlayMode; la rama base de hoy tiene más tests (4.996 y 2.612), así que la diferencia es de la base, no de la Mac.

## 4. Los scripts en macOS

- **Funcionan tal cual** `scripts/unity-compile.ps1` y `scripts/unity-test.ps1` con PowerShell 7.6 de Homebrew, si se les pasa `-Runner batchmode -UnityExe <ruta del Editor de macOS>`. Sin `-UnityExe` no encuentran el Editor: buscan `Unity.exe` en `Program Files` y la Unity CLI, que en la Mac no está. No hizo falta ninguna versión `.sh`.
- **Mejora posible (rama aparte, no hecha):** que los dos scripts busquen solos `/Applications/Unity/Hub/Editor/<versión>/Unity.app/Contents/MacOS/Unity` en macOS, sin cambiar nada en Windows.
- La Unity CLI (`unity`) no está instalada en la Mac. No hace falta para M0; instalarla sería una herramienta nueva y queda a decisión del dueño.
- El log de licencias dice `Access token is unavailable; failed to update` y aun así la licencia vale (el mismo aviso que en Windows, observación 7 de la hoja del 2026-10-22).

## 5. Pendientes y observaciones

1. **Unity reescribe dos archivos de `ProjectSettings/` al abrir el proyecto en la Mac:**
   - `ProjectSettings.asset`: añade los huecos vacíos de los iconos de iPhone y iPad (`m_BuildTarget: iPhone`), porque ahora está iOS Build Support;
   - `GvhProjectSettings.xml` (EDM4U): quita un `<projectSetting />` vacío repetido.

   Ninguno va en esta rama: los iconos de iOS se ponen en M1 con `ProjectSetup.ApplyIosDefaults`, y el primero irá con ellos.
2. **El Hub instaló también Unity 6000.6.7f1 con WebGL** (lo propuso por defecto al abrirlo). No lo usa el proyecto; el dueño puede quitarlo desde Unity Hub › Installs para recuperar espacio.
3. **Python 3.14 con `jsonschema`** (lo pide `CLAUDE.md` para `tools/*.py`) no está. M0 y M1 no lo necesitan; si hiciera falta, `brew install python@3.14` y `jsonschema` en un entorno virtual, con permiso del dueño.
4. **CocoaPods avisa de que la terminal no usa UTF-8.** Para `pod install` (M1/M2) se pondrá `LANG=en_US.UTF-8` en el propio comando; añadirlo a `~/.zprofile` lo decide el dueño.
5. **Android Build Support** no está instalado en la Mac (opcional según `mac-ios.md` §1).
