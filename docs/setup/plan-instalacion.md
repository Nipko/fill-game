# Plan de instalación y arranque técnico de Tintalú

Fecha: 2026-10-06 · Autor: dirección técnica (agente) · Estado: listo para ejecutar. Los pasos marcados como "dueño" esperan tu acción o tu confirmación en el chat.

**De dónde sale este plan:**
- Los informes `docs/setup/research/a…f`.
- Una verificación adversaria: dos verificadores por cada afirmación crítica. Todas las afirmaciones críticas visibles quedaron confirmadas: versión, changeset, módulos ARM64, requisitos de Play, PBL, Gradle/AGP y la deprecación del MCP de Unity. Solo se refutó un dato: el Hub es nativo ARM64 desde la 3.17.1, no desde la 3.19.0. No afecta, porque usamos la 3.22.2.

**Comprobado hoy en esta máquina:**
- winget ofrece Hub 3.22.2, Unity CLI 1.0.0-beta.12 (los dos como MSIX arm64) y .NET SDK 10.0.401 arm64.
- Git LFS ya está configurado de forma global.
- `gh` está autenticado con los permisos `repo` y `workflow`.
- El driver de la Adreno es el 31.0.137.0.
- El PATH de usuario todavía no incluye `platform-tools`.

## Decisión de motor y versión

- **Motor:** Unity 6.3 LTS, plan Personal, C#, URP 2D y Shader Graph. Se mantiene la decisión del GDD.
- **Versión exacta:** **6000.3.25f1, changeset `e1dba0a9aba4`** (24-09-2026, LTS, soporte hasta diciembre de 2027).
  - Los dos verificadores la confirmaron contra la API de releases.
  - Se fija en `ProjectSettings/ProjectVersion.txt` y el CI la lee de ahí.
  - Los parches 6000.3.x posteriores entran solo en un PR dedicado y con el CI en verde.
- **Descartadas:**
  - 6000.6.4f1: no es LTS, y es lo que instala `winget install Unity.Unity.6000`. No usar ese paquete.
  - 6000.0.x: su soporte termina este mes.
- **Arquitectura del editor: ARM64 nativo.**
  - Unity lo soporta oficialmente, y el módulo Android Build Support es el mismo que en x64.
  - El matiz: el OpenJDK 17.0.18+8 y el NDK r27c que trae ese módulo son binarios x64 que corren emulados con Prism. Unity solo dice que esos builds "might still work with x86 emulation", y nadie ha publicado un build IL2CPP de Android hecho desde un Snapdragon X.
  - Por eso el editor ARM64 queda para editar, Play Mode y tests. La compuerta del paso s26 decide si los builds locales sirven. El build de referencia para publicar será siempre el del CI en Linux x64.
- **Cambios en el GDD** (piden tu aprobación; un agente los aplicará en un PR de documentación):
  1. En 08 §1.2 y §15, la versión pasa a 6000.3.25f1 ARM64, instalada con winget y la Unity CLI.
  2. .NET 8 pasa a .NET 10 LTS (08 §2.4, §12.1, §15; 03 §17; GDD §9 #3). .NET 8 deja de tener soporte el 10-11-2026.
  3. AF.Core y AF.Solver se mueven al paquete embebido `Packages/com.amazonfill.core/` y se añade `dotnet/`.
  4. CI:
     - Licencia: `UNITY_LICENSE` (.ulf) desaparece y se usan `UNITY_EMAIL` + `UNITY_PASSWORD` de una cuenta dedicada sin 2FA.
     - Acciones: `unity-test-runner@v4.4.0` y `unity-builder@v6.0.0`, con `cliVersion: v0.1.72`.
     - Ubicación: los workflows van en `.github/workflows/`.
  5. SDK:
     - Firebase 13.17.0 e IAP 5.4.4 (PBL 9; donde ponía "Billing 8" pasa a "8 o superior").
     - Sin el plugin Google Mobile Ads: MAX ya cubre AdMob y UMP.
     - Target API fijo en 36.
  6. Godot de referencia: 4.7.2/4.8.

## Pasos de instalación

Todo se ejecuta en PowerShell 7 desde `C:\Users\Nipko-lab\Desktop\projects\amazon-fill`. La salida estructurada trae cada comando completo con rutas absolutas.

**Quién hace cada paso:**
- **agente:** lo ejecuta sin preguntar.
- **dueño:** el paso pide credenciales, aceptar una licencia, confirmar el UAC o tomar una decisión. Puedes hacerlo tú o confirmar en el chat para que lo lance el agente. Iniciar sesión y escribir secretos lo haces siempre tú.

| id | Qué | Comando | Quién | Tamaño | Tiempo | Verificación |
|---|---|---|---|---|---|---|
| s01 | Comprobación previa | `git lfs version; gh auth status; (Get-PSDrive C).Free/1GB` | agente | — | 1 min | ≥40 GB libres |
| s02 | Git LFS global | `git lfs install` | agente | — | 1 min | `git config --global filter.lfs.process` |
| s03 | .NET SDK 10 | `winget install --id Microsoft.DotNet.SDK.10 --exact --source winget --architecture arm64 --accept-package-agreements --accept-source-agreements` | dueño (UAC) | 0,2 GB | 5 min | `dotnet --list-sdks` → 10.0.401 |
| s04 | Hub 3.22.2 + Unity CLI | `winget install --id Unity.UnityHub --exact --source winget …`; si falta `unity`: `winget install --id Unity.CLI --exact --source winget …` | dueño (acuerdos) | 0,3 GB | 5 min | `unity --version` → 1.0.0-beta.12 |
| s05 | Driver Adreno | Windows Update › Actualizaciones opcionales | dueño | 0,5 GB | 15 min | `Win32_VideoController` ≥ 31.0.170.0 |
| s06 | `platform-tools` en el PATH de usuario | Añade `%LOCALAPPDATA%\Android\Sdk\platform-tools` (ExpandString, con copia del PATH previo) | agente | — | 1 min | `adb version` |
| s07 | Repositorio git | `git init -b main; git config core.longpaths true` | agente | — | 1 min | `git rev-parse` |
| s08 | Archivos raíz | `.gitignore`, `.gitattributes`, `.editorconfig`; `dotnet new globaljson --sdk-version 10.0.401 --roll-forward latestFeature` | agente | — | 5 min | `Test-Path` |
| s09 | Proyectos .NET | `dotnet new buildprops/classlib/nunit/console/sln` + NUnit 3.14.0 | agente | 0,3 GB | 5 min | `dotnet sln … list` |
| s10 | Ajustar y probar el núcleo | Ediciones de §Andamiaje; `dotnet build dotnet/AmazonFill.slnx -warnaserror; dotnet test dotnet/AmazonFill.slnx` | agente | — | 15 min | Código de salida 0 |
| s11 | Configuración de agentes | `CLAUDE.md`, `.claude/agents|rules`, `scripts/*.ps1` | agente | — | 30 min | `CLAUDE.md` < 200 líneas |
| s12 | Permisos de agentes | Revisar `.claude/settings.proposed.json` y renombrarlo a `.claude/settings.json` al aprobarlo | dueño | — | 10 min | "aprobado" en el chat |
| s13 | Primer commit local | `git add -A; git commit` | agente | — | 2 min | Ningún secreto en `git ls-files` |
| s14 | Repositorio privado | `gh repo create <ORG>/amazon-fill --private --source . --remote origin --push` | dueño (organización) | — | 3 min | `visibility` = PRIVATE |
| s15 | Unity ID | `unity auth login` (navegador) | dueño | — | 3 min | `unity auth status` |
| s16 | Licencia Personal | `unity license activate --personal --accept-eula` | dueño (términos) | — | 2 min | `unity license status` |
| s17 | Simulacro de instalación | `unity install 6000.3.25f1 --changeset e1dba0a9aba4 --module android --child-modules --dry-run --format json` | agente | — | 2 min | Aparecen `android-open-jdk-17.0.18+8` y `android-ndk-r27c` |
| s18 | Editor + Android | `unity install 6000.3.25f1 --changeset e1dba0a9aba4 --module android --child-modules --yes --accept-eula --non-interactive` | dueño (EULA de Unity y Google, UAC) | 7,1 GB de descarga, 17,6 GB instalados | 45-90 min | `unity editors verify 6000.3.25f1 --architecture arm64` |
| s19 | Plantilla | `unity templates list --editor 6000.3.25f1 --type core --format json` | agente | — | 1 min | `com.unity.template.universal-2d` |
| s20 | Crear el proyecto | `unity projects create amazon-fill --path ../_unity-create --editor-version 6000.3.25f1 --template com.unity.template.universal-2d --non-interactive`; copiar `Assets`, `Packages/*`, `ProjectSettings` a la raíz | agente | 1-2 GB | 15 min | `ProjectVersion.txt` |
| s21 | Paquete pipeline | `unity pipeline install --project-path .` | agente | — | 3 min | En `manifest.json` |
| s22 | Primera importación + EditMode | `unity test . --mode EditMode --output Logs/editmode.xml --timeout 3600` | agente | 2-4 GB | 15-30 min | Salida 0; `.meta` creados |
| s23 | Merge YAML y hooks | `unity vcs doctor --fix; unity vcs merge-setup; unity vcs hooks install` | agente | — | 5 min | `merge-setup --check` → 0 |
| s24 | Skill de Unity local | `unity skill install claude-code --local --yes` | agente | — | 1 min | `.claude/skills/unity-cli` |
| s25 | Player Settings | `unity run . --log-file Logs/project-setup.log -- -executeMethod AF.EditorTools.ProjectSetup.ApplyAndroidDefaults` | agente | — | 20 min | minSdk 26, target 36, IL2CPP ARM64 |
| s26 | **Compuerta del día 1** | Tres veces `unity run … -executeMethod AF.EditorTools.BuildSmoke.BuildAndroid` (AAB + APK); luego `zipalign -c -P 16 -v 4` | agente | 3 GB | 60-120 min | 3 de 3 en verde; "Verification successful"; tiempos y Gradle registrados |
| s27 | En el teléfono | `adb install -r Builds/Android/smoke.apk` | dueño (USB) | — | 10 min | `pm list packages` |
| s28 | Prueba visual | `unity open .` y `unity open . --args -force-d3d11`, 30 min de Play Mode con cada uno | dueño | — | 70 min | Sin cuelgues ni artefactos |
| s29 | Commit Unity | `git add -A; git commit` | agente | — | 2 min | Los hooks pasan |
| s30 | CI del núcleo | `.github/workflows/ci.yml` (job `core`) por PR | agente | — | 20 min | `gh run list` en verde |
| s31 | Secretos del CI | Cuenta Unity sin 2FA; `gh secret set UNITY_EMAIL` y `UNITY_PASSWORD` | dueño | — | 15 min | `gh secret list` |
| s32 | CI de Unity | `unity-tests` + AAB en `main` | agente | — | 40 min | Ejecución en verde |

**Variables de entorno:**
- `ANDROID_HOME` y `ANDROID_SDK_ROOT` no se definen. Unity usa el SDK, el NDK y el JDK de su propio módulo, la misma cadena que usa el CI. El NDK 27.1 de Android Studio no vale: Unity exige r27c.
- `JAVA_HOME` sigue apuntando a Temurin, que se usa para `keytool`.
- El PATH de usuario solo gana `platform-tools` (paso s06).
- `.claude/settings.json` define, solo para los agentes: `UNITY_NON_INTERACTIVE=1`, `UNITY_NO_BANNER=1`, `DOTNET_CLI_TELEMETRY_OPTOUT=1`, `TESTINGPLATFORM_TELEMETRY_OPTOUT=1` y `DOTNET_NOLOGO=1`. El job `core` del CI (s30) define las mismas tres de .NET.
- `UNITY_NO_CRASH_REPORT` es opcional y lo decides tú.

**Reglas de uso:**
- Cierra Android Studio mientras Unity compila.
- El `applicationId` provisional es el neutral `com.amazonfill.dev` (antes `com.estudio.aquazonia`, descartado con la marca Aquazonia). Una compuerta del CI impide que llegue a una build de release. El definitivo, `com.<estudio>.tintalu`, espera al dictamen de marca.
- Ningún AAB a Play con el `applicationId` definitivo (`com.<estudio>.tintalu`) hasta tener el dictamen de marca.

## Andamiaje del repositorio

La raíz del repositorio es el proyecto Unity (GDD 08 §3). Unity solo importa `Assets/` y `Packages/`, así que el resto de carpetas no le afecta.

```
amazon-fill/
  CLAUDE.md  global.json  .gitignore  .gitattributes  .editorconfig
  .claude/{settings.json, agents/, rules/, skills/unity-cli/}
  .github/workflows/{ci.yml, unity.yml}
  Assets/_Project/{Art,Audio,Shaders,Prefabs,Scenes,Scripts,Data,Editor/CI,Tests}
  Packages/manifest.json
  Packages/com.amazonfill.core/           # ÚNICA copia del núcleo
    package.json
    Runtime/AF.Core/   AF.Core.asmdef (noEngineReferences: true), Polyfills/IsExternalInit.cs
    Runtime/AF.Solver/ AF.Solver.asmdef
    Tests/Editor/AF.Core.Tests/ AF.Solver.Tests/   # tests compartidos: NUnit 3.5 + C# 9
  ProjectSettings/                        # 6000.3.25f1 (e1dba0a9aba4)
  dotnet/  Directory.Build.props  BannedSymbols.txt  AmazonFill.slnx
           AF.Core/ AF.Solver/ (netstandard2.1, enlazan los .cs del paquete)
           AF.Core.Tests/ (net10.0, NUnit 3.14.0)  artifacts/ (ignorada)
  tools/   Directory.Build.props  level-generator/  level-qa/  *.py
  scripts/ unity-test.ps1 unity-compile.ps1    ci/ size-gate.ps1 check-16kb.sh
  schemas/  docs/  functions/ (más adelante)
```

**`.gitignore`:**
- Lo habitual de Unity: `Library`, `Temp`, `Obj`, `Build*`, `Logs`, `UserSettings`.
- Los `/*.csproj`, `/*.sln` y `/*.slnx` de la raíz, pero solo de la raíz, para que los de `dotnet/` sigan en el repositorio.
- `/dotnet/artifacts/`, `*.apk` y `*.aab`.
- Secretos: `*.jks`, `*.keystore`, `google-services.json`, `.env*`.
- De Claude Code: `.claude/worktrees/`, `.claude/settings.local.json` y `CLAUDE.local.md`.

**`.gitattributes`:**
- `* text=auto`.
- `eol=lf` para código, YAML de Unity, `.meta` y JSON.
- LFS (`filter=lfs diff=lfs merge=lfs -text`) para `png jpg psd tga wav ogg mp3 ttf otf fbx mp4 aar jar so dll`.
- Nunca LFS para código, YAML, `.meta` ni JSON.
- `unity vcs merge-setup` añade el driver `unityyamlmerge` y `doctor --fix` revisa todo.

**.NET:**
- Proyectos: `classlib -f netstandard2.1` (AF.Core, AF.Solver), `nunit` (AF.Core.Tests), `console` (level-generator, level-qa) y `dotnet new sln` (en .NET 10 genera `.slnx`).
- Versiones fijadas: NUnit 3.14.0, NUnit3TestAdapter 6.3.0, Microsoft.NET.Test.Sdk 18.10.1 y NUnit.Analyzers 3.9.0.
- `dotnet/Directory.Build.props` fija:
  - `LangVersion 9.0` (igual que Unity), con `ImplicitUsings` y `Nullable` desactivados.
  - `TreatWarningsAsErrors` y `ArtifactsPath`.
  - BannedApiAnalyzers 5.6.0, que bloquea `System.Random`, `string.GetHashCode`, LINQ y `DateTime.Now`.
- Los `Directory.Build.props` van en `dotnet/` y `tools/`, nunca en la raíz: allí afectarían a los `.csproj` que genera Unity.
- Hay que borrar `ImplicitUsings` y `Nullable` de los `.csproj` que crea la plantilla.

**Agentes:**
- **`CLAUDE.md`** (menos de 200 líneas) recoge:
  - las fuentes de verdad (manda el GDD);
  - el stack fijado y el mapa del repositorio;
  - los comandos de verificación obligatorios;
  - las reglas duras: no tocar a mano `.unity`, `.prefab`, `.meta` ni `ProjectSettings/`; nada de SDK ni permisos nuevos sin el dueño; nunca secretos; un Editor por carpeta y como máximo 2 abiertos;
  - qué decide cada humano.
- **`.claude/rules/`**, una regla por ruta:
  - `core.md`: C# 9, sin UnityEngine, determinista.
  - `unity-assets.md`: escenas y prefabs solo a través del Editor.
  - `data-json.md`: validar contra `schemas/`.
  - `sdk.md`: cambios solo con PR revisado por el dueño.
  - `ci.md`: versiones fijadas y jobs de Unity en serie.
- **`.claude/agents/`**, siete subagentes:

| Subagente | Ámbito | Modelo / aislamiento | Verifica con |
|---|---|---|---|
| `tech-lead` | Plan y división del trabajo; no edita | opus | Citas al GDD |
| `core-dev` | AF.Core y AF.Solver | opus, worktree | `dotnet build -warnaserror`, `dotnet test` |
| `level-pipeline-dev` | `tools/level-*`, `schemas/` | opus, worktree | `dotnet test`, `validate_schema.py` |
| `unity-gameplay-dev` | Shader del líquido, UI, GameFlow, Economy | opus, worktree con Editor propio | `unity recompile`, `unity test` |
| `sdk-integrator` | MAX/UMP, IAP, Firebase, Play Games tras interfaces con implementaciones falsas en el editor | opus, worktree, `permissionMode: default` | Build en CI y checklist de privacidad |
| `build-release` | Workflows, compuertas de 80 MB, 16 KB y targetSdk | opus, worktree | `merge-setup --check`, compuertas |
| `code-reviewer` | Revisa cada PR | opus, solo lectura | Ejecuta las pruebas del PR |

Decisión del dueño (2026-10-06): todos los subagentes usan Opus; volver a Sonnet requiere su aprobación (docs/setup/equipo-de-agentes.md §8).

**`.claude/settings.json`** (hoy existe como `.claude/settings.proposed.json`; lo revisas tú en s12 y, al aprobarlo, se renombra a `.claude/settings.json`):
- **allow:** `dotnet build/test/run`, `python tools/*`, `unity test/run/recompile/status/editors`, `unity license status` y los `git` locales.
- **ask:** `unity command *` (incluye `eval`), `unity install*`, `unity pipeline *`, `git push*`, `gh pr merge*` y editar `Packages/manifest.json` o `*.asset`.
- **deny:**
  - Editar `*.unity`, `*.prefab`, `*.meta` y `ProjectSettings/**`.
  - Leer `*.jks`, `*.keystore`, `google-services.json` y `.env*`.
  - `unity auth login*`, `unity license activate*`, `unity setup claude*`, `unity mcp configure*`, `gh secret *` y `git push --force*`.

**Paralelismo:** hasta 3-4 agentes de C# puro en worktrees de Claude Code y como máximo 2 con Editor (`unity vcs git worktree add <rama> --seed cache`).

**Verificación que ejecutan los agentes:**

```powershell
dotnet build dotnet/AmazonFill.slnx -warnaserror; dotnet test dotnet/AmazonFill.slnx
python tools/validate_schema.py
unity recompile --project-path .            # con Editor abierto; 7 = Safe Mode
unity test . --mode EditMode --output Logs/editmode.xml --timeout 900
unity test . --mode PlayMode --output Logs/playmode.xml   # antes de cada PR
# 0 verde · 8 tests fallidos · 6 sin veredicto (leer el log) · 2 argumentos mal
# Respaldo: Unity.exe -batchmode -nographics -projectPath . -runTests -testPlatform EditMode
#           -testResults Logs/editmode.xml -logFile Logs/editmode.log   (sin -quit)
```

En el CI, `dotnet test` corre en cada PR. Los tests compartidos se repiten en EditMode con game-ci cuando cambia el paquete. Antes de cada release corre una suite "golden" en un player IL2CPP.

**MCP para Unity: ninguno.**
- El puente es la Unity CLI con `com.unity.pipeline` 0.8.0-exp.1, usada desde la shell. Unity indica que así se gastan menos tokens que con MCP. El MCP de `com.unity.ai.assistant` está deprecado (confirmado).
- Si alguna vez hiciera falta MCP, la opción es `unity mcp configure claude-code --local`: es oficial y escucha solo en localhost.
- Evaluación de seguridad:
  - **Unity CLI:** el servidor escucha solo en localhost y `eval` va protegido con una clave por proyecto y siempre en "ask". Envía en cada ejecución un ping de uso anónimo que no se puede desactivar.
  - **CoplayDev/unity-mcp v10.3.0:** solo como plan B y sin instalar. Trae la telemetría activada por defecto (se apaga con `DISABLE_TELEMETRY=true`), y su `execute_code` permite que el propio llamante desactive los controles.
  - **IvanMurzak/Unity-MCP:** rechazado, porque por defecto enruta por la nube `ai-game.dev`.
  - **En general:** cualquier puente ejecuta C# con tus permisos de Windows, y las reglas "deny" no son un sandbox. Por eso los agentes que navegan no tienen `eval`, los que programan no tienen `WebFetch` y el CI nunca usa `eval`.
- El plugin oficial `unity setup claude` (ámbito de usuario) es decisión tuya. Si lo instalas, quita la skill local de s24 para no cargar dos copias.

## Marca

**Nombre vigente: Tintalú** (forma ASCII `Tintalu`; decisión del dueño del 2026-10-06). Aquazonia se descartó por la solicitud idéntica INPI 938306200 en Brasil (clase 41); lo que sigue bajo "Historia" es el cribado original de Aquazonia y se conserva como registro.
- **Riesgo de Tintalú: medio** (búsqueda preliminar, no asesoría legal): EL TINTAL en el IMPI de México (reg. 2586508, clase 41); los puzles "Tinta" de la App Store; el elemento TINTA, casi descriptivo y saturado en las clases 9 y 41. Faltan el INPI, INDECOPI y la SIC por consultar directamente.
- **Pasos para ti:** registrar hoy `tintalu.com`, `tintalu.app`, `tintalu.co` y `tintalu.com.br` y reservar los usuarios en redes; encargar al abogado de marcas la búsqueda en el INPI (clases 9, 28 y 41), el dictamen en México frente a EL TINTAL y la estrategia frente a TINTA en EE. UU.
- **Publicación:** ningún AAB a Play con el `applicationId` definitivo (`com.<estudio>.tintalu`) hasta tener el dictamen; los builds de prueba usan `com.amazonfill.dev`. Con tilde en todo lo visible, también en PT-BR.
- **Planes B, en orden:** Cuiazú, Botonda y Selvagua. Detalle en `docs/legal/nombres-candidatos.md`.

### Historia: cribado de Aquazonia (descartado el 2026-10-06)

**Riesgo global: medio.** Esto es una búsqueda preliminar, no asesoría legal.

**Situación:**
- **Brasil:** el INPI tiene la solicitud 938306200 "Aquazonia" (mixta, clase 41: periodismo y publicaciones en línea). Sigue pendiente y probablemente está ligada al proyecto periodístico "Aquazônia", que trata del agua amazónica, el mismo tema que el juego. Riesgo medio-alto en clase 41.
- **CO, PE, MX, EE. UU., UE y Madrid:** sin coincidencias.
- **Tiendas:** limpias.
- **Dominios:** `aquazonia.com` está ocupado; `.app` y `.co` están libres.

**Pasos para ti:**
1. Hoy: revisa la ficha del INPI y registra `aquazonia.app` y `aquazonia.co`.
2. Esta semana: pide a un abogado brasileño un dictamen sobre las clases 9 y 41, con una especificación estrecha o un acuerdo de convivencia. Presenta la marca en las clases 9 y 41 en el país del estudio. Perú va siempre por vía nacional.
3. Ningún AAB a Play hasta tener el dictamen, porque el `applicationId` no se puede cambiar.
4. Sin "Aquazônia" con tilde en PT-BR.
5. Presupuesto: 3.000-6.000 USD, por encima de los 800-2.500 que prevé el GDD.

Nada de esto bloquea la instalación.

## Riesgos abiertos y plan B

| # | Riesgo | Mitigación / plan B |
|---|---|---|
| R1 | Build IL2CPP Android desde ARM64 con NDK y JDK emulados: falla o tarda más de 15 min (sin verificar) | Lo decide s26. **B1:** editor x64 de la misma versión bajo Prism, solo para builds (instalador `UnitySetup64-6000.3.25f1.exe` + Android Support en otra carpeta, más `unity editors add`; que funcione `-a x86_64` en Windows no está verificado). **B2:** builds solo en CI Linux (`unityci/editor:ubuntu-6000.3.25f1-android-3`). **B3:** JDK 17 ARM64 de Microsoft para Gradle |
| R2 | Cuelgues del sistema con Unity 6 en Snapdragon X | Driver ≥ 31.0.170 (s05); desactivar "Automatic super resolution"; `-force-d3d11` |
| R3 | URP/Render Graph (UUM-92264); Bloom en el 2D Renderer da pantalla negra (UUM-150232) | Driver nuevo y `-force-d3d11`; sin Bloom 2D en 1.0 |
| R4 | DLL x64 en el editor (Firebase, EDM4U, MAX) | Implementaciones falsas en el editor; los SDK se prueban en dispositivo y en CI |
| R5 | Unity CLI en beta y pipeline experimental | Versiones fijadas; scripts de respaldo en batchmode |
| R6 | Licencia Personal en CI: 2FA, asiento retenido, flags no documentados, zona gris de los términos | Cuenta dedicada; `concurrency: unity-personal-seat`; `cliVersion` fijado; alternativa: Unity Build Automation |
| R7 | 15,6 GB de RAM | Como máximo 2 Editores; cerrar Android Studio |
| R8 | Dos `adb` (Unity 36.0.0 y Studio 37.0.0) | Usar uno solo |
| R9 | AGP 9 frente a plugins; EDM4U archivado el 26-10-2026; Gradle 9.1.0 o 9.3.1 sin confirmar | Compuertas de CI; `namespace` en los `.androidlib`; registrar la versión de Gradle en s26 |
| R10 | Target 37 obligatorio en agosto de 2027 | Probar en el primer trimestre de 2027; si falla, 6.7 LTS |
| R11 | Paridad del PRNG con `levelgen_proto.py` (MT19937) | Decides tú. Recomendación: PRNG nuevo y regenerar los JSON de referencia |
| R12 | `projects create` sobre una carpeta que ya existe; el Hub instala una segunda CLI | Crear en `../_unity-create` y copiar; `Get-Command unity -All` |
| R13 | Coste de CI y LFS (2.000 min, 10 GiB) | CI de Unity solo en PR listos y en `main`; cachés |

**Godot (plan C, no se instala).**
- Solo entra si salta un disparador del GDD: el líquido no llega a 60 fps en 3 semanas o Unity cambia el plan Personal. También si el editor ARM64 resulta inutilizable de forma persistente después de probar B1 y B2.
- La ruta sería Godot 4.8 con GDScript o C# net10, AdMob como mediador (Poing), GameAnalytics para Remote Config y PAD solo en modo install-time.
- El núcleo en netstandard2.1 y .NET 10 ya abaratan ese cambio.
