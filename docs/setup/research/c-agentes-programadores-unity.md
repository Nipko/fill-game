# Agentes de IA como programadores de Aquazonia en Unity 6: puente con el Editor, flujo sin Editor, convenciones, CLAUDE.md y subagentes, y CI con game-ci (investigación C)

Fecha: 2026-10-06 · Proyecto Amazon Fill (Aquazonia) · Alcance: cómo hacer que Claude Code y sus subagentes programen el juego Unity con eficacia y seguridad en la máquina del dueño (Windows 11 ARM64, Snapdragon X Plus, 15,6 GB RAM) y en GitHub Actions.

Convención de evidencia: **[V]** verificada hoy con la URL indicada (o con un comando de solo lectura ejecutado hoy en la máquina, p. ej. `winget show`), **[E]** estimación o inferencia del autor, **[NV]** no verificable hoy. Las cifras de estrellas y versiones de GitHub se leyeron de la API pública de GitHub el 2026-10-06. Nada se instaló ni se modificó en el sistema.

---

## Respuesta corta

- **Unity ya no tiene un "Unity MCP" dentro del Editor que convenga adoptar**: el servidor MCP del paquete `com.unity.ai.assistant` está **deprecado** y Unity indica sustituirlo por la **Unity CLI** (`unity`), cuyo modo `unity mcp` usa el mismo protocolo [V](https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli). La CLI controla un Editor abierto mediante el paquete **`com.unity.pipeline`** (0.8.0-exp.1, Unity 6.0+, API HTTP local) y además compila, prueba y construye en batchmode. Es **experimental/beta** (1.0.0-beta.12, 2026-09-30) [V](https://docs.unity.com/en-us/unity-cli/release-notes), gratis y con **instalador nativo Windows ARM64** en winget (`Unity.CLI`) [V, `winget show Unity.CLI`].
- **Opciones comunitarias** (2026-10-06): **CoplayDev/unity-mcp** es la más usada (14.732★, MIT, v10.3.0 del 2026-10-04, Python + uv, 48 herramientas, telemetría activada por defecto con opt-out); **IvanMurzak/Unity-MCP** (4.390★, Apache-2.0, 0.93.2, binario win-arm64, pero su configuración por defecto enruta por la nube `ai-game.dev`); **CoderGamester/mcp-unity** (1.920★, MIT, Node 18+, Unity 6+). Todas permiten **ejecutar C# arbitrario** en el Editor: son herramientas de máxima confianza, no un límite de seguridad [V, URLs en §1].
- **Recomendación de puente**: Unity CLI + `com.unity.pipeline` como única vía oficial, usada **como comandos de shell** (`unity recompile`, `unity test`, `unity command ...`), que la documentación de Unity describe como más rápidos y con menos tokens que MCP [V](https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli). CoplayDev (fijado a v10.3.0, telemetría desactivada) queda como plan B. No instalar dos puentes a la vez.
- **El 70-80 % del trabajo de los agentes no necesita el Editor** [E]: `AF.Core`, `AF.Solver` y `tools/` en C# puro se compilan y prueban con `dotnet test` en segundos. Para eso hay que instalar el **.NET 10 SDK** (no el 8 que cita el GDD: .NET 8 pierde soporte el **2026-11-10**) [V](https://devblogs.microsoft.com/dotnet/dotnet-8-9-end-of-support/); winget ofrece 10.0.401 nativo arm64 [V, `winget show Microsoft.DotNet.SDK.10`].
- **Sin MCP, el agente itera en batchmode**: `unity test <proyecto> --mode EditMode|PlayMode` devuelve **0** (todo pasa), **8** (tests fallidos) o **6** (no hubo veredicto: error de compilación, licencia, cuelgue) y no requiere el paquete pipeline [V](https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/references/build-run-test.md). Con `Unity.exe` directo: `-batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults r.xml -logFile log.txt`, **sin `-quit`** (no se admite mientras corren tests) [V](https://docs.unity3d.com/Manual/test-framework/reference-command-line.html). Un proyecto abierto en el Editor no puede abrirse a la vez en batchmode [V, fuente antigua](https://discussions.unity.com/t/error-it-looks-like-another-unity-instance-is-running-with-this-project-open/656599).
- **Convenciones**: Force Text y Visible Meta Files ya son el valor por defecto en Unity 6.3 [V](https://docs.unity3d.com/6000.3/Documentation/Manual/class-EditorManager.html); faltan `asmdef` con `noEngineReferences` para el núcleo, UnityYAMLMerge (`unity vcs merge-setup`), Git LFS **global** (no `--local`, que rompe los worktrees de Claude Code) [V](https://code.claude.com/docs/en/worktrees), datos en JSON/ScriptableObject y la regla dura de que los agentes **no editan a mano** `.unity`, `.prefab` ni `.meta`.
- **CLAUDE.md de menos de 200 líneas** [V](https://code.claude.com/docs/en/memory) + reglas por ruta en `.claude/rules/` + **7 subagentes** en `.claude/agents/` con `tools`, `model` e `isolation: worktree` [V](https://code.claude.com/docs/en/sub-agents). Cada worktree es un proyecto Unity independiente con su propio `Library/`; con 15,6 GB de RAM caben **como mucho 2 Editores** a la vez [E]; el trabajo en C# puro admite más worktrees en paralelo.
- **CI: game-ci cambió en septiembre de 2026**. Con licencia Personal basta **`UNITY_EMAIL` + `UNITY_PASSWORD`**; `UNITY_LICENSE` (.ulf) está ligado a la máquina que lo generó y **no funciona en runners efímeros** (game-ci/cli v0.1.63) [V](https://github.com/game-ci/cli/releases/tag/v0.1.63). Usar `game-ci/unity-test-runner@v4.4.0` y `game-ci/unity-builder@v6.0.0`, que ahora envuelven `game-ci/cli` y por defecto descargan la **última** CLI: hay que fijar `cliVersion` (última: v0.1.72, 2026-10-03) [V](https://github.com/game-ci/unity-builder/blob/v6.0.0/action.yml). La página de activación de game.ci todavía pide `UNITY_LICENSE` [V](https://game.ci/docs/github/activation): está desactualizada respecto a las notas de versión.
- **Coste CI**: 2.000 min/mes gratis en repos privados del plan Free y luego 0,006 USD/min en Linux x64 [V](https://docs.github.com/en/billing/concepts/product-billing/github-actions); caché de 10 GB gratis por repo; Git LFS 10 GiB de almacenamiento y 10 GiB de ancho de banda, y **las descargas LFS de Actions cuentan** [V](https://docs.github.com/en/billing/concepts/product-billing/git-lfs). Alcanza para ≈50-70 pipelines completos al mes [E] si los agentes no disparan CI en cada commit.
- **Máquina ARM64**: Unity 6000.3 (6.3 LTS) publica instaladores **Windows ARM64 con Android Build Support** (p. ej. 6000.3.25f1, 2026-09-24, changeset `e1dba0a9aba4`) [V](https://unity.com/releases/editor/whats-new/6000.3.25f1); Unity Hub 3.22.2 ya es MSIX arm64 [V, `winget show Unity.UnityHub`]. **No usar** `winget install Unity.Unity.6000`: hoy apunta a 6000.6.4f1, que no es LTS [V, `winget show Unity.Unity.6000`].

---

## 1. Puentes entre el agente y el Editor de Unity (MCP y CLI)

### 1.1 Lo oficial de Unity en octubre de 2026

| Componente | Estado | Versión / fecha | Qué hace | Evidencia |
|---|---|---|---|---|
| Servidor MCP dentro de `com.unity.ai.assistant` ("Unity MCP") | **Deprecado**: "Use the Unity command-line interface (CLI) instead" | Documentación 2.20.0-pre.2 | Relay en `~/.unity/relay/` por named pipe; escenas, assets, scripts, consola; conexiones externas requieren aprobación en Project Settings | [V](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.20/manual/integration/unity-mcp-overview.html) |
| **Unity CLI** (`unity`) | **Experimental / beta**; gratis y separado de Unity AI (sin créditos) | 1.0.0-beta.9 (2026-09-08) … **1.0.0-beta.12 (2026-09-30)**; winget publica el MSIX arm64 el 2026-10-01 | Instala Editores y módulos, crea/abre proyectos, `build`, `test`, `run`, `recompile`, `watch`, `command`, `mcp`, `skill`, `vcs`, `license`, `auth` | [V](https://docs.unity.com/en-us/unity-cli) · [V](https://docs.unity.com/en-us/unity-cli/release-notes) · [V](https://docs.unity.com/en-us/unity-cli/unity-cli-reference) · [V, winget] |
| **Unity Pipeline package** (`com.unity.pipeline`) | Experimental | **0.8.0-exp.1** | Se instala por proyecto (`unity pipeline install`); arranca un servidor HTTP local (puerto 7800 en la documentación) con dos servidores: Editor (Play, estado, recompilar, tests) y Runtime; comandos `editor_play`, `run_tests`, `screenshot`, `eval` | [V](https://docs.unity3d.com/Packages/com.unity.pipeline@0.8/index.html) · [V](https://docs.unity.com/en-us/unity-cli/unity-pipeline/unity-pipeline-package) · [V](https://unity.com/resources/unity-pipeline-cli-technical-walkthrough) |
| `unity mcp` | Soportado (no deprecado); reemplaza al MCP del Editor | Desde la beta; `unity mcp configure <cliente>` con `claude`, `cursor`, `vscode`, `windsurf` | MCP sobre el Pipeline; también añade el MCP del Issue Tracker de Unity (`--server issue-tracker`) | [V](https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli) · [V](https://docs.unity.com/en-us/unity-cli/release-notes) |
| Skills oficiales para agentes (`Unity-Technologies/skills`) | Repositorio activo (1.048★, licencia sin SPDX detectado) | 34 skills, entre ellas `unity-cli`, `ui-uitk`, `ui-ugui`, `implement-in-app-purchases`, `localization`, `manage-sprite-atlas`, `unity-package-management` | Instrucciones para Claude Code y otros; el paquete pipeline trae además una skill `unity-pipeline` que se copia con `unity skill install <cliente> --local` | [V](https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/SKILL.md) · [V, API GitHub] |

Datos clave de la skill oficial `unity-cli` (útiles para el CLAUDE.md) [V](https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/SKILL.md):

- Con un Editor abierto, **conducirlo** (`unity status`, `unity command`, `unity command eval '<C#>'`) en lugar de editar YAML de escenas o prefabs; "never hand-edit `.unity`, `.prefab`, or `.asset` YAML while a live Editor is reachable".
- Si el proyecto tiene errores de compilación, el Editor arranca en **Safe Mode**, el pipeline no carga y `unity recompile` sale con código 7: hay que leer el log, corregir y reiniciar.
- Con varios Editores abiertos, pasar siempre `--project-path`.
- Un sandbox del agente puede ocultar un Editor que sí está abierto: no concluir "no hay Editor" solo por `unity status`.
- Telemetría: informes de fallos por Sentry (desactivables con `UNITY_NO_CRASH_REPORT`) y **un ping anónimo de uso por ejecución independiente del consentimiento**.
- Códigos de salida: 0 éxito, 2 argumentos, 3 autenticación, 4 precondición (p. ej. sin licencia), 6 fallo del comando, **8 tests fallidos (solo `unity test`)**, 9 bloqueo de instalación.

### 1.2 Comparativa de opciones (datos de 2026-10-06)

| Opción | ★ / forks | Licencia | Última versión | Requisitos | Qué permite | Windows ARM64 | Red y telemetría | Ejecución de código |
|---|---|---|---|---|---|---|---|---|
| **Unity CLI + `com.unity.pipeline`** (oficial) | n/a (no es repo público) | Propietaria (términos de Unity) [V, winget] | CLI 1.0.0-beta.12; pipeline 0.8.0-exp.1 | Unity 6.0+ para conducir un Editor; Windows 10 21H1+ | Escenas/GameObjects/assets vía `eval` y comandos; `recompile`; `test` (EditMode/PlayMode, NUnit/JUnit, `--retries`, `--shard`, `--affected`); `build` (incl. firma Android); `screenshot`; logs; `vcs` (merge-setup, doctor, hooks, worktree con `Library` sembrado) | **Sí, nativo** (MSIX `unity-windows-arm64.msix`) [V, winget]; la beta.10 corrigió un fallo de detección de Git en Windows on Arm [V](https://docs.unity.com/en-us/unity-cli/release-notes) | Servidor solo en localhost por defecto [V](https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli); ping anónimo de uso siempre | `eval` = C# arbitrario, **protegido por una clave de acceso por proyecto** [V](https://unity.com/resources/unity-pipeline-cli-technical-walkthrough) |
| **CoplayDev/unity-mcp** ("MCP for Unity") | 14.732 / 1.535 | MIT | **v10.3.0 (2026-10-04)**; v10.2.0 (2026-09-01) | Unity 2021.3 LTS → 6.x; Python 3.10+ con `uv` | 48 herramientas: `manage_scene`, `manage_gameobject`, `manage_prefabs`, `read_console`, `run_tests`, `refresh_unity`, `execute_menu_item`, `execute_code`, `batch_execute`; validación Roslyn; multi-instancia; desde v10, generación de assets con claves propias (fal, OpenRouter) | Probable [E]: `uv` 0.12.23 tiene build `aarch64-pc-windows-msvc` [V, winget] y Python 3.14 arm64 ya está instalado; las ruedas de todas las dependencias Python en win-arm64 [NV] | HTTP por defecto en `http://localhost:8080/mcp`, solo loopback salvo opt-in [V](https://github.com/CoplayDev/unity-mcp/blob/main/SECURITY.md) · [V](https://github.com/CoplayDev/unity-mcp/blob/main/website/docs/architecture/transports.md); **telemetría anónima activada por defecto** hacia `api-prod.coplay.dev`, opt-out con `DISABLE_TELEMETRY=true` [V](https://github.com/CoplayDev/unity-mcp/blob/main/website/docs/architecture/telemetry.md) | `execute_code` con lista de patrones bloqueados que **el propio llamante puede desactivar** con `safety_checks=false` [V](https://github.com/CoplayDev/unity-mcp/blob/main/MCPForUnity/Editor/Tools/ExecuteCode.cs) |
| **IvanMurzak/Unity-MCP** ("AI Game Developer") | 4.390 / 388 | Apache-2.0 | 0.93.2 (2026-09-27) | Unity Editor y Runtime; CLI opcional con Node (`unity-mcp-cli`); **la ruta del proyecto no puede tener espacios** (la nuestra no los tiene) | 70+ herramientas (escena, prefabs, componentes, assets, tests, consola, profiler), `script-execute` con Roslyn, llamada a métodos por reflexión, herramientas propias con un atributo; también dentro del juego compilado | **Sí**: binario `win-arm64/gamedev-mcp-server.exe` [V](https://github.com/IvanMurzak/Unity-MCP) | Por defecto `unity-mcp-cli login` (OAuth) y la configuración apunta a un **endpoint en la nube** `https://ai-game.dev/mcp/p/<pin>`; se puede usar stdio local; autorización `none` por defecto en local; puerto 8080 [V](https://github.com/IvanMurzak/Unity-MCP) | C# arbitrario por Roslyn y reflexión |
| **CoderGamester/mcp-unity** | 1.920 / 246 | MIT | 1.5.0 (2026-09-03) | Node 18+, Unity 6+ | 30+ herramientas: `run_tests`, `recompile_scripts`, `get_console_logs`, `create_prefab`, `load_scene`, `update_component`, `batch_execute`, materiales | Probable [E] (Node 24 arm64 ya instalado) | WebSocket en puerto 8090 con token de 256 bits por proyecto; conexiones remotas desactivadas por defecto; instalación de paquetes desactivada por defecto [V](https://github.com/CoderGamester/mcp-unity) | `execute_menu_item` y edición de componentes; sin `eval` documentado [E] |
| **hatayama/unity-cli-loop** (uLoop) | 576 | MIT | v3.12.0 (2026-10-06) | Unity 2022.3+; v3 sin Node | CLI (no MCP nativo): `compile`, `run-tests`, `get-logs`, `screenshot`, `execute-dynamic-code`, simulación de input en Play Mode; 21 skills | Windows soportado; ARM64 [NV] | IPC por named pipes, sin puertos TCP [V](https://github.com/hatayama/unity-cli-loop) | `execute-dynamic-code` |

Ninguno de los cinco repositorios tiene avisos de seguridad publicados en GitHub a 2026-10-06 [V, API `security-advisories`]. Eso no prueba ausencia de fallos: en enero-febrero de 2026 se registraron más de 30 CVE en servidores y clientes MCP en general [V, fuente secundaria](https://www.heyuan110.com/posts/ai/2026-03-10-mcp-security-2026/).

### 1.3 Evaluación de seguridad para este equipo

| Riesgo | Por qué importa aquí | Mitigación |
|---|---|---|
| Ejecución de C# arbitrario en el Editor (`eval`, `execute_code`, `script-execute`) con los permisos del usuario de Windows | El Editor puede leer el keystore de subida, `google-services.json`, tokens de Git y archivos del dueño | `unity command eval` y equivalentes en la lista **ask** de Claude Code; nunca en CI; keystore y credenciales fuera del repositorio y en `deny` de lectura (§4.5) |
| Inyección de instrucciones (prompt injection) desde documentación web, issues, comentarios en assets o paquetes de terceros | Un agente que lee una página maliciosa y además tiene `eval` puede ejecutar lo que esa página pida | Separar el agente que navega/investiga (sin `eval`, sin escritura) de los que programan; los subagentes de código no tienen `WebFetch` |
| Tráfico a terceros | CoplayDev envía telemetría por defecto; IvanMurzak enruta por defecto por `ai-game.dev`; la Unity CLI envía un ping anónimo por ejecución | Unity CLI como puente; si se usa CoplayDev, `DISABLE_TELEMETRY=true` y transporte local; IvanMurzak solo en modo stdio local |
| Puertos locales abiertos (7800, 8080, 8090) | Cualquier proceso local o una página web podría intentar hablar con ellos | Unity Pipeline y CoplayDev escuchan solo en loopback por defecto [V]; no activar "LAN bind"; el `eval` del Pipeline requiere clave por proyecto [V] |
| Paquetes de puente que cambian rápido (CoplayDev publica casi cada semana) | Una actualización automática puede cambiar herramientas o permisos | Fijar versión exacta en `Packages/manifest.json` (git URL con `#v10.3.0`) y actualizar solo por PR revisado |
| Deny rules de Claude Code no son un sandbox | Las reglas `Edit`/`Read` no cubren subprocesos (un script de Python o el propio Editor pueden tocar cualquier archivo) [V](https://code.claude.com/docs/en/permissions) | Tratar las reglas como barandilla de comportamiento; la protección real es no tener secretos en la máquina de desarrollo más allá de lo imprescindible |

---

## 2. Flujo sin MCP: compilar y probar en batchmode

### 2.1 Comandos de referencia

| Objetivo | Unity CLI (preferido) | `Unity.exe` directo (respaldo sin CLI) | Notas |
|---|---|---|---|
| Tests EditMode | `unity test . --mode EditMode --output Logs/editmode.xml --report-format nunit,junit --timeout 900` | `Unity.exe -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log` | `unity test` invoca `-runTests` directamente, **no necesita el paquete pipeline** y no pasa `-quit` [V](https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/references/build-run-test.md) |
| Tests PlayMode | `unity test . --mode PlayMode --output Logs/playmode.xml` | igual con `-testPlatform PlayMode` | `-runSynchronously` solo vale para EditMode [V](https://docs.unity3d.com/Manual/test-framework/reference-command-line.html) |
| Filtrar | `--filter "AF.Core.Tests"`, `--retries 2`, `--rerun-failed`, `--shard 1/2` | `-testFilter`, `-testCategory`, `-assemblyNames`, `-retry`, `-repeat`, `-randomOrderSeed` | [V](https://docs.unity3d.com/Manual/test-framework/reference-command-line.html) |
| Solo tests afectados | `unity test . --affected --since origin/main` | — | **Poco útil aquí**: si el proyecto usa Addressables (Aquazonia sí), la CLI se niega a seleccionar y ejecuta la suite completa (`reason: addressables`) [V](https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/references/build-run-test.md) |
| ¿Compila? (Editor abierto) | `unity recompile --project-path .` | — | Responde en segundos sin generar artefactos; si el Editor arrancó ya roto (Safe Mode) sale con 7 [V](https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/SKILL.md) |
| ¿Compila? (sin Editor) | `unity run . --log-file Logs/compile.log -- -executeMethod AF.EditorTools.CI.CompileCheck` | `Unity.exe -batchmode -nographics -quit -projectPath . -logFile Logs/compile.log` | En batchmode, ante errores Unity sale con código 1; `-ignorecompilererrors` permite arrancar pese a ellos [V](https://docs.unity3d.com/6000.3/Documentation/Manual/EditorCommandLineArguments.html). No pasar `-batchmode`/`-quit`/`-projectPath` a `unity run` (los gestiona él) [V](https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/references/build-run-test.md) |
| Build Android | `unity build . --target Android ...` (con opciones de firma) | `Unity.exe -batchmode -quit -projectPath . -buildTarget android -executeMethod AF.EditorTools.CI.BuildAab -logFile Logs/build.log` | `-accept-apiupdate` recomendado en batchmode para evitar errores del API Updater [V](https://docs.unity3d.com/6000.3/Documentation/Manual/EditorCommandLineArguments.html) |

Rutas del Editor en Windows: `UnityYAMLMerge.exe` y el ejecutable viven bajo la carpeta de instalación del Editor (`...\Editor\Data\Tools\` para la herramienta de merge) [V](https://docs.unity3d.com/6000.3/Documentation/Manual/SmartMerge.html). Conviene envolver todo en `scripts/unity-test.ps1` y `scripts/unity-compile.ps1` para que los agentes llamen siempre a lo mismo, con la versión del Editor leída de `ProjectSettings/ProjectVersion.txt`.

### 2.2 Detectar errores de compilación en el log

- `-nographics` apaga los logs de salida salvo que se indique `-logFile` [V](https://docs.unity3d.com/6000.3/Documentation/Manual/EditorCommandLineArguments.html): **siempre** pasar `-logFile <ruta>` (o `-logFile -` para stdout).
- Patrón de error de C#: líneas `error CS\d{4}` con ruta y número de línea; Unity 6 también escribe "Scripts have compiler errors" [E, patrón estándar]. El script del agente debe devolver las 30 primeras líneas que casen, no el log completo (ahorra contexto).
- Para un veredicto estricto: un método de Editor `[InitializeOnLoad]` que escuche `CompilationPipeline.assemblyCompilationFinished` y llame a `EditorApplication.Exit(1)` si hay mensajes de error; `-executeMethod` debe salir con código distinto de 0 o lanzar excepción para señalar fallo [V](https://docs.unity3d.com/6000.3/Documentation/Manual/EditorCommandLineArguments.html).
- Códigos de `-runTests`: la documentación de Unity dice que **no hay una definición común** de códigos de salida [V](https://docs.unity3d.com/Manual/test-framework/reference-command-line.html); por eso conviene usar `unity test` (0/8/6 documentados) o leer el XML NUnit.

### 2.3 Tiempos típicos en esta máquina

| Operación | Tiempo esperado | Marca |
|---|---|---|
| `dotnet build` + `dotnet test` de `AF.Core`/`AF.Solver` (cientos de tests) | 5-30 s | [E] |
| `unity recompile` con el Editor abierto | segundos | [V, "in seconds"](https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/references/build-run-test.md) |
| Primera importación del proyecto (URP 2D + Firebase + MAX + IAP) | 8-20 min | [E] |
| Tests EditMode en batchmode con `Library` caliente (arranque del Editor + recarga de dominio + tests) | 1-3 min | [E] |
| Tests PlayMode en batchmode | 2-5 min | [E] |
| Pruebas en CI (game-ci) | 5-8 min | [V, GDD 08 §12.1 citando research/06] |
| AAB IL2CPP en CI | 15-30 min | [V, GDD 08 §12.1 citando research/06] |
| AAB IL2CPP local en Windows ARM64 | más lento que en x64 si parte de la cadena Android (NDK) corre emulada | [NV] |

### 2.4 Bucle recomendado para un agente sin abrir el Editor

1. Cambios en C# puro → `dotnet test tools/AmazonFill.Tools.sln` (segundos). Repetir hasta verde.
2. Cambios que tocan código Unity → si hay Editor abierto del mismo worktree: `unity recompile`; si no: `scripts/unity-compile.ps1` (batchmode con `-logFile`).
3. `unity test . --mode EditMode` → si sale 8, leer el XML y corregir; si sale 6, leer el log (compilación, licencia) y no reintentar a ciegas.
4. Solo al cerrar la tarea: `unity test . --mode PlayMode`.
5. Los `.meta` de archivos nuevos los genera Unity al importar (paso 2 o 3); el agente los **añade al commit tal cual**, nunca los escribe a mano.
6. Nunca ejecutar batchmode sobre una carpeta que tenga un Editor abierto: usar el Editor (CLI/pipeline) o un worktree distinto [V](https://discussions.unity.com/t/error-it-looks-like-another-unity-instance-is-running-with-this-project-open/656599).

---

## 3. Convenciones de proyecto que facilitan el trabajo de agentes

| Convención | Estado en Unity 6.3 | Qué hacer en Aquazonia | Por qué ayuda a los agentes |
|---|---|---|---|
| Asset Serialization = **Force Text** | Valor por defecto [V](https://docs.unity3d.com/6000.3/Documentation/Manual/class-EditorManager.html) | Mantener; comprobar en el primer commit | Diffs legibles; los agentes pueden **leer** escenas y prefabs aunque no los editen |
| Version Control = **Visible Meta Files** | Valor por defecto [V](https://docs.unity3d.com/6000.3/Documentation/Manual/class-VersionControlSettings.html) | Mantener; todo `.meta` va al repositorio | Los GUID estables evitan referencias rotas |
| **Assembly Definitions** por módulo | Disponible; `noEngineReferences: true` excluye `UnityEngine`/`UnityEditor` [V](https://docs.unity3d.com/6000.1/Documentation/Manual/assembly-definition-file-format.html) (página 6000.1; igual en 6.3 [E]) | `AF.Core` y `AF.Solver` con `noEngineReferences: true`; un `asmdef` por módulo de la tabla 2.2 del GDD; asmdef de tests por módulo | Compilación incremental más rápida; el compilador impide dependencias prohibidas (la UI nunca referenciada) en vez de confiar en el agente |
| **Núcleo compartido con .NET** | Unity 6.3 compila C# 9.0 (sin init-only setters ni covariant returns; no usar `record` en tipos serializados) [V](https://docs.unity3d.com/6000.3/Documentation/Manual/csharp-compiler.html) | `tools/AF.Core.csproj` y `tools/AF.Solver.csproj` (`netstandard2.1`, `LangVersion 9`) que **incluyen los mismos `.cs`** por `<Compile Include="..\Assets\_Project\Scripts\Core\**\*.cs" />`; tests NUnit que compilan en Unity (EditMode) y en `dotnet test` | Bucle de segundos sin Editor; una sola fuente de reglas para cliente, `level-generator` y `level-qa` |
| Código en **paquetes embebidos** (`Packages/com.amazonfill.*`) | Disponible | Opcional para `AF.Core`/`AF.Solver`; si se usa, añadir `testables` en `manifest.json` para que corran sus tests [E] | Límites claros; útil si el núcleo se reutiliza en otro juego |
| **UnityYAMLMerge** (smart merge) | Incluido en cada Editor [V](https://docs.unity3d.com/6000.3/Documentation/Manual/SmartMerge.html) | `unity vcs merge-setup` escribe `.gitattributes`, el driver de merge y hace una fusión de prueba; `--check` sale con 4 si falta trabajo (usable en CI) [V](https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/references/version-control.md) | Conflictos de escenas/prefabs resolubles cuando dos worktrees tocan el mismo prefab |
| **.gitignore** de Unity | — | Partir de `github/gitignore/Unity.gitignore` (descargar, no ejecutar) y añadir `.claude/worktrees/`, `CLAUDE.local.md`, `Logs/`, `*.keystore` [V](https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/SKILL.md) | `Library/`, `Temp/`, `obj/` fuera del repositorio |
| **Git LFS** para binarios | — | `git lfs install` **global** (con `--local`, los worktrees de Claude Code reciben punteros LFS en vez de archivos) [V](https://code.claude.com/docs/en/worktrees); patrones LFS para `.png`, `.psd`, `.wav`, `.ogg`, `.mp3`, `.ttf`, `.fbx`; **nunca** `.cs`, `.json`, `.unity`, `.prefab`, `.asset`, `.meta`, `.shadergraph` | El código y el YAML siguen siendo diffables y fusionables; `unity vcs doctor --fix` audita patrones LFS, ignores y versiones fijadas [V](https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/references/version-control.md) |
| **Hooks de integridad** | — | `unity vcs hooks install` (pre-commit, post-checkout, post-merge) [V](https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/references/version-control.md) | Detecta `.meta` huérfanos o faltantes antes del commit del agente |
| **Escenas pequeñas, prefabs pequeños** | — | 3 escenas (`Boot`, `Map`, `Game`) casi vacías; todo lo demás en prefabs con un solo dueño; UI por prefab de pantalla | Menos conflictos; los cambios de un agente quedan en un archivo acotado |
| **Datos en JSON / ScriptableObject** | — | Niveles, paleta, Remote Config, catálogo IAP y anuncios en JSON (ya decidido en GDD 08 §4) validados por esquema; ScriptableObjects solo como contenedores con referencias a assets | El agente edita y valida JSON sin Editor (`python tools/validate_schema.py`); menos YAML a mano |
| **Finales de línea** | — | `.gitattributes` con `* text=auto` y `eol=lf` para YAML de Unity y `.cs` [E] | Evita diffs fantasma entre agentes en Windows y CI Linux |
| **Versión del Editor fijada** | — | `ProjectSettings/ProjectVersion.txt` es la fuente; CI y scripts la leen; game-ci/cli la detecta sola [V](https://github.com/game-ci/unity-builder/releases) | Mismo Editor en local, worktrees y CI |

Esquema de ensamblados sugerido para el núcleo (texto del `asmdef`, ilustrativo [E]):

```json
{ "name": "AF.Core", "rootNamespace": "AF.Core", "references": [],
  "noEngineReferences": true, "autoReferenced": false,
  "overrideReferences": false, "allowUnsafeCode": false }
```

---

## 4. CLAUDE.md, reglas y subagentes para un equipo de juego formado por agentes

### 4.1 Estructura de `CLAUDE.md` (raíz, menos de 200 líneas)

La documentación de Claude Code pide **menos de 200 líneas por archivo**, secciones con títulos y viñetas, sin contradicciones; los `@imports` ayudan a organizar pero **no** reducen el coste de contexto; lo específico de una parte del código va a `.claude/rules/` con `paths:` [V](https://code.claude.com/docs/en/memory). Las instrucciones son contexto, no imposición: lo que deba bloquearse va en permisos o hooks [V](https://code.claude.com/docs/en/memory).

```markdown
# Aquazonia (amazon_fill) — instrucciones para agentes

## Qué es y fuentes de verdad
- Puzzle water-sort Android-first. Diseño: docs/GDD.md y docs/gdd/*.md (el GDD manda; si el código
  contradice al GDD, para y pregunta). Arquitectura: docs/gdd/08-arquitectura-tecnica-publicacion.md.
- Nombres: docs/gdd/00-glosario.md. IDs de código con prefijo af_. Nunca "amazon" en IDs públicos.

## Stack fijado
- Unity 6000.3.x LTS (ver ProjectSettings/ProjectVersion.txt), URP 2D, C# 9, IL2CPP ARM64.
- .NET 10 SDK para tools/ y tests del núcleo. Python 3.14 para tools/*.py.

## Mapa del repositorio
- Assets/_Project/Scripts/Core|Solver: C# puro, sin UnityEngine (asmdef noEngineReferences).
- Assets/_Project/Data: JSON validado contra schemas/. tools/: generador, QA, simulación.

## Comandos de verificación (obligatorios antes de declarar una tarea terminada)
- Núcleo:      dotnet test tools/AmazonFill.Tools.sln
- Esquemas:    python tools/validate_schema.py
- Unity:       pwsh scripts/unity-compile.ps1  ;  unity test . --mode EditMode
- Antes de PR: unity test . --mode PlayMode
- Exit 8 = tests fallidos (corrige); exit 6 = no hubo veredicto (lee Logs/*.log).

## Reglas duras
- No editar a mano .unity, .prefab, .meta ni ProjectSettings/: usa el Editor vía `unity command`
  o pide al humano. Los .meta los genera Unity; se commitean tal cual.
- No añadir paquetes, SDKs ni permisos Android sin aprobación del dueño.
- No leer ni escribir secretos (keystore, google-services.json, .env). No `git push --force`.
- Un Editor por carpeta: nunca batchmode sobre un proyecto abierto.

## Flujo de trabajo
- Una tarea = una rama = un worktree. PR pequeño con descripción, pruebas ejecutadas y riesgos.
- Commits en español, imperativo. El revisor (subagente code-reviewer) aprueba antes del merge.

## Humanos
- Dueño: decisiones de producto, licencias, cuentas, publicación. Artista 2D: todo arte nuevo.
- Asesor cultural: textos y nombres de fauna, flora, pueblos y lugares.
```

### 4.2 Reglas por ruta (`.claude/rules/`)

| Archivo | `paths:` | Contenido |
|---|---|---|
| `core.md` | `Assets/_Project/Scripts/Core/**`, `Assets/_Project/Scripts/Solver/**`, `tools/**/*.cs` | Prohibido `using UnityEngine`; determinismo (sin `System.Random` sin semilla, sin `DateTime.Now`); C# 9 sin init-only; cada regla del GDD 02 con su test |
| `unity-assets.md` | `Assets/**/*.unity`, `Assets/**/*.prefab`, `Assets/**/*.asset` | Solo lectura para agentes; cambios vía `unity command eval` con aprobación; describir el cambio para el humano si no hay Editor |
| `data-json.md` | `Assets/_Project/Data/**`, `schemas/**` | Validar con `tools/validate_schema.py`; `pack_schema` y semillas según GDD 08 §4.1 |
| `sdk.md` | `Assets/_Project/Scripts/{Ads,IAP,Analytics,RemoteConfig,Save,Platform}/**`, `Assets/Plugins/**` | Todo cambio requiere PR revisado por el dueño; nada de claves en código |
| `ci.md` | `.github/workflows/**`, `scripts/**` | Fijar versiones de acciones y `cliVersion`; no imprimir secretos; jobs de Unity serializados |

### 4.3 Subagentes propuestos (`.claude/agents/*.md`)

Campos disponibles: `name`, `description` (obligatorios), `tools`, `disallowedTools`, `model`, `permissionMode`, `skills`, `hooks`, `memory`, `isolation: worktree`, `maxTurns`, `mcpServers`, `effort`, entre otros; los de proyecto (`.claude/agents/`) tienen prioridad sobre los de usuario [V](https://code.claude.com/docs/en/sub-agents).

| Subagente | Ámbito | Herramientas | Modelo / aislamiento | Verificación que debe ejecutar | Límites |
|---|---|---|---|---|---|
| `tech-lead` | Planifica, divide tareas, mantiene el grafo de `asmdef` y el CLAUDE.md | Read, Grep, Glob, Bash de solo lectura | `opus`, sin worktree | Revisa que el plan cite secciones del GDD | No edita código |
| `core-dev` | `AF.Core`, `AF.Solver` y sus tests | Read, Edit, Write, Grep, Glob, Bash/PowerShell | `opus`, `isolation: worktree` | `dotnet test`; determinismo por semilla | Sin `UnityEngine`; sin `WebFetch` |
| `level-pipeline-dev` | `tools/level-generator`, `tools/level-qa`, `schemas/` | Igual que core-dev | `sonnet` u `opus`, worktree | `dotnet test`, `python tools/validate_schema.py`, `level-qa` sobre paquetes de muestra | No cambia el formato `af-level/1` sin subir `pack_schema` y avisar |
| `unity-gameplay-dev` | Presentation, UI, GameFlow, Boosters, Economy (código Unity) | Read, Edit, Write, Bash/PowerShell, skill `unity-cli` | `opus`; worktree propio **con su propio Editor** o el checkout principal si es el único Editor | `unity recompile`, `unity test --mode EditMode/PlayMode` | Escenas/prefabs solo vía Editor y con aprobación |
| `sdk-integrator` | Ads (MAX/UMP), IAP (Billing 8), Firebase, Play Games | Igual que gameplay, con `permissionMode: default` (pregunta todo) | `opus`, worktree | Build Android en CI + checklist de privacidad | Toda dependencia nueva la aprueba el dueño |
| `build-release` | `.github/workflows`, `scripts/`, tamaño del AAB, Gradle | Read, Edit, Write, Bash | `sonnet`, worktree | `actionlint` [E], `unity vcs merge-setup --check`, gate de 80 MB | Nunca toca secretos; no publica en Play sin el dueño |
| `code-reviewer` | Revisa cada PR contra GDD, reglas y tests | Read, Grep, Glob, Bash de solo lectura | `opus`, sin worktree | Ejecuta las pruebas del PR y lee el diff completo | No edita; bloquea si faltan tests |

Ejemplo de archivo (`.claude/agents/core-dev.md`, ilustrativo):

```markdown
---
name: core-dev
description: Implementa y prueba AF.Core y AF.Solver (C# puro, sin UnityEngine) y su espejo en tools/. Úsalo para reglas de vertido, mecánicas, solver, canonicalización y hash.
tools: Read, Grep, Glob, Edit, Write, Bash, PowerShell
model: opus
isolation: worktree
---
Eres el responsable del núcleo de reglas de Aquazonia.
- Fuente de verdad: docs/gdd/02-nucleo-reglas-ux.md y docs/gdd/08-arquitectura-tecnica-publicacion.md §2.
- Prohibido referenciar UnityEngine/UnityEditor. C# 9 (sin init-only setters ni records serializados).
- Antes de terminar: `dotnet test tools/AmazonFill.Tools.sln` en verde y un test por regla nueva.
- Si una regla del GDD es ambigua, detente y devuelve la pregunta; no inventes reglas.
- Entrega: resumen del cambio, tests añadidos, salida resumida de las pruebas.
```

### 4.4 Worktrees y trabajo paralelo con Unity

| Hecho | Consecuencia | Evidencia |
|---|---|---|
| `claude --worktree <nombre>` crea `.claude/worktrees/<nombre>/` en la rama `worktree-<nombre>`; los subagentes con `isolation: worktree` reciben un worktree temporal que se borra si no hay cambios | Paralelismo nativo; añadir `.claude/worktrees/` al `.gitignore` | [V](https://code.claude.com/docs/en/worktrees) |
| Un worktree es un checkout limpio: sin `Library/` ni archivos ignorados; `.worktreeinclude` copia ignorados que casen con sus patrones | Cada worktree que abra Unity reimporta todo (minutos) | [V](https://code.claude.com/docs/en/worktrees) |
| `unity vcs git worktree add <rama> --seed cache` crea un worktree con el Editor correcto y `Library` sembrado ("usable en minutos en vez de una reimportación completa") | Usar este comando para worktrees que necesiten Editor; los de C# puro pueden ser los de Claude Code | [V](https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/references/version-control.md) |
| Los subagentes con worktree parten por defecto de la rama por defecto del remoto (`worktree.baseRef: "fresh"`); `"head"` parte del HEAD local | Para tareas encadenadas sobre trabajo no publicado, poner `"head"` | [V](https://code.claude.com/docs/en/worktrees) |
| Git LFS instalado con `--local` deja punteros en los worktrees creados por Claude Code; solución: `git lfs pull` dentro, o instalar LFS global | Instalar LFS global | [V](https://code.claude.com/docs/en/worktrees) |
| Un Editor por carpeta; batchmode no puede abrir una carpeta ya abierta | Asignar a cada agente de Unity su worktree o turnos sobre el principal | [V, fuente antigua](https://discussions.unity.com/t/error-it-looks-like-another-unity-instance-is-running-with-this-project-open/656599) |
| 15,6 GB de RAM; cada Editor con un proyecto móvil mediano ocupa 2-4 GB | Máximo 2 Editores simultáneos (uno interactivo + uno en batchmode) más agentes de C# puro | [E] |

Práctica recomendada: **1 agente Unity a la vez por worktree con Editor (máx. 2)**, **hasta 3-4 agentes de C# puro** en worktrees de Claude Code, y el `tech-lead` en el checkout principal sin Editor.

### 4.5 Permisos y hooks en `.claude/settings.json`

Las reglas se evalúan en orden **deny → ask → allow**; una deny no admite excepciones por allow [V](https://code.claude.com/docs/en/permissions). Los hooks `PreToolUse` bloquean con código de salida 2 o con `permissionDecision: "deny"`; `PostToolUse` con `matcher: "Edit|Write"` sirve para compilar o formatear tras cada edición [V](https://code.claude.com/docs/en/hooks-guide). Propuesta (patrones a validar con `/permissions` [E]):

```json
{
  "permissions": {
    "allow": ["Bash(dotnet test *)", "Bash(dotnet build *)", "Bash(python tools/*)",
              "Bash(unity test *)", "Bash(unity recompile*)", "Bash(unity status*)",
              "Bash(git status*)", "Bash(git diff*)", "Bash(git log*)"],
    "ask":   ["Bash(unity command *)", "Bash(unity build *)", "Bash(unity install*)",
              "Bash(git push *)", "Edit(**/*.asset)", "Edit(Packages/manifest.json)"],
    "deny":  ["Edit(**/*.unity)", "Edit(**/*.prefab)", "Edit(**/*.meta)", "Edit(ProjectSettings/**)",
              "Read(**/*.keystore)", "Read(**/google-services.json)", "Read(./.env*)",
              "Bash(unity auth *)", "Bash(unity license *)", "Bash(git push --force*)"]
  },
  "worktree": { "baseRef": "head" }
}
```

Hook útil: `PostToolUse` sobre `Edit|Write` que, si el archivo está bajo `Scripts/Core` o `Scripts/Solver`, ejecute `dotnet build tools/AF.Core.csproj -v q` y devuelva solo los errores [E].

### 4.6 Puertas humanas

| Humano | Decide o entrega | Cómo lo piden los agentes |
|---|---|---|
| Dueño | Instalar software, iniciar sesión en Unity/Google/GitHub, activar la licencia Personal, crear secretos de CI, aprobar SDKs, publicar en Play | Issue o comentario en el PR con la acción exacta; los agentes **no** introducen contraseñas ni claves |
| Artista 2D | Sprites, atlas, fondos, iconos (PNG/PSD por LFS) | Issue con especificación (tamaño, ASTC, atlas destino); el agente solo integra |
| Asesor cultural | Nombres y textos de fauna, flora, lugares y pueblos | Tabla de localización marcada "pendiente de revisión" |

---

## 5. CI con game-ci en GitHub Actions y licencia Personal

### 5.1 Versiones vigentes (2026-10-06)

| Acción | Última versión | Fecha | Nota | Evidencia |
|---|---|---|---|---|
| `game-ci/unity-builder` | **v6.0.0** (también v5.0.1 y v4.8.2) | 2026-09-10 (v6); 2026-09-01 (v5.0.1/v4.8.2) | v6 es un envoltorio fino de `game-ci/cli`, mismas entradas y secretos que v5; v5.0.1/v4.8.2 corrigen el fallo de memoria compartida de Unity 6.6+ (`dockerShmSize`, 1025m por defecto) | [V](https://github.com/game-ci/unity-builder/releases) |
| `game-ci/unity-test-runner` | **v4.4.0** (v5.0.0-beta.1 en pre-release) | 2026-09-09 | También invoca `game-ci/cli`; restaura `UNITY_LICENSE`/`UNITY_LICENSE_FILE`; Node 24 | [V](https://github.com/game-ci/unity-test-runner/releases/tag/v4.4.0) |
| `game-ci/cli` | **v0.1.72** | 2026-10-03 | Activación Personal por cuenta desde v0.1.51; correcciones de devolución de asiento hasta v0.1.71 | [V](https://github.com/game-ci/cli/releases) |
| Imagen Docker | `unityci/editor:ubuntu-6000.3.25f1-android-3.2.2` existe | — | Etiquetas Android para 6000.3.21f1-6000.3.25f1 publicadas | [V, API Docker Hub](https://hub.docker.com/r/unityci/editor/tags) |

`cliVersion` vale `'latest'` por defecto en ambas acciones ("pin this for reproducible builds") [V](https://github.com/game-ci/unity-builder/blob/v6.0.0/action.yml). Con agentes que lanzan muchos PR, fijarlo es obligatorio.

### 5.2 Licencia Personal: qué cambió

- Unity eliminó la activación manual/offline para Personal: la guía de activación manual "doesn't support ... Unity Personal" y la licencia Personal se activa iniciando sesión en el Hub [V](https://docs.unity3d.com/Manual/ManualActivationGuide.html).
- game-ci/cli añadió la estrategia `personal`, que pide un asiento al servicio de licencias con `Unity.Licensing.Client --activate-all --include-personal --username ... --password ...` [V](https://github.com/game-ci/cli/pull/246) y, desde **v0.1.63**, activa en 2020.3, 2022.3, 6000.0 y 6000.6 "con `UNITY_EMAIL` + `UNITY_PASSWORD` solamente"; `UNITY_LICENSE` (.ulf) "no puede funcionar en un contenedor efímero en ninguna versión" [V](https://github.com/game-ci/cli/releases/tag/v0.1.63).
- Un asiento Personal no devuelto ("leaked seat") hace fallar **todas** las ejecuciones posteriores de la cuenta con "no available seats"; v0.1.66 corrige la devolución en Ubuntu [V](https://github.com/game-ci/cli/releases/tag/v0.1.66). Implicación [E]: **serializar** los jobs que activan Unity (un `concurrency` común) para no pedir dos asientos a la vez.
- 2FA en la cuenta Unity usada por CI: comportamiento con usuario+contraseña [NV]; probar en la primera ejecución y, si falla, decidir con el dueño.
- La documentación pública de game.ci aún enumera `UNITY_LICENSE`, `UNITY_EMAIL` y `UNITY_PASSWORD` para Personal y la ruta `C:\ProgramData\Unity\Unity_lic.ulf` [V](https://game.ci/docs/github/activation): seguir las notas de versión de la CLI (más recientes).

### 5.3 Flujo propuesto (corrige el extracto de GDD 08 §12.1)

```yaml
# .github/workflows/android.yml  (GitHub solo lee workflows en .github/workflows/, no en ci/)
name: android
on:
  pull_request:
    types: [opened, synchronize, ready_for_review]
    paths: ['Assets/**', 'Packages/**', 'ProjectSettings/**', 'tools/**', 'schemas/**', '.github/workflows/**']
  push: { branches: [main] }
  workflow_dispatch: {}
concurrency: { group: android-${{ github.ref }}, cancel-in-progress: true }

jobs:
  core:                                   # sin Unity: segundos, sin licencia
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4      # [E] verificar la versión mayor vigente
        with: { dotnet-version: '10.0.x' }
      - run: dotnet test tools/AmazonFill.Tools.sln -c Release
      - run: python tools/validate_schema.py

  unity-tests:
    needs: core
    if: github.event.pull_request.draft != true
    runs-on: ubuntu-latest
    concurrency: { group: unity-personal-seat, cancel-in-progress: false }   # un asiento a la vez [E]
    steps:
      - uses: actions/checkout@v4
      - run: git lfs ls-files -l | cut -d' ' -f1 | sort > .lfs-assets-id
      - uses: actions/cache@v3            # patrón de game.ci; [E] usar la versión mayor vigente
        with: { path: .git/lfs, key: lfs-${{ hashFiles('.lfs-assets-id') }} }
      - run: git lfs pull
      - uses: actions/cache@v3
        with:
          path: Library
          key: Library-Android-${{ hashFiles('Assets/**', 'Packages/**', 'ProjectSettings/**') }}
          restore-keys: Library-Android-
      - uses: game-ci/unity-test-runner@v4.4.0
        env:
          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
        with: { testMode: all, cliVersion: v0.1.72, githubToken: '${{ secrets.GITHUB_TOKEN }}' }

  build-android:
    needs: unity-tests
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    concurrency: { group: unity-personal-seat, cancel-in-progress: false }
    steps:
      # checkout + caché LFS + caché Library (igual que arriba, clave Library-Android-)
      - uses: game-ci/unity-builder@v6.0.0
        env:
          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
        with:
          targetPlatform: Android
          cliVersion: v0.1.72
          androidExportType: androidAppBundle
          androidKeystoreBase64: ${{ secrets.ANDROID_KEYSTORE_BASE64 }}
          androidKeystorePass: ${{ secrets.ANDROID_KEYSTORE_PASS }}
          androidKeyaliasName: ${{ secrets.ANDROID_KEYALIAS_NAME }}
          androidKeyaliasPass: ${{ secrets.ANDROID_KEYALIAS_PASS }}
          androidVersionCode: ${{ github.run_number }}
```

Patrón de caché LFS y de `Library` tomado de la guía de game.ci [V](https://game.ci/docs/github/getting-started); game.ci afirma que la caché de `Library` puede acelerar las pruebas "más de un 50 %" [V](https://game.ci/docs/github/test-runner). Para builds Android en runners estándar, la guía añade un paso de liberar disco (`jlumbroso/free-disk-space`) [V](https://game.ci/docs/github/getting-started). Las entradas `targetPlatform`, `androidExportType` (`androidAppBundle` para AAB), `androidKeystoreName`, `androidKeystoreBase64`, `androidKeystorePass`, `androidKeyaliasName`, `androidKeyaliasPass` y `androidVersionCode` existen en `unity-builder` v6.0.0 [V](https://github.com/game-ci/unity-builder/blob/v6.0.0/action.yml); al YAML de arriba le falta `androidKeystoreName` (nombre del archivo, p. ej. `user.keystore`), que se añade junto al Base64.

### 5.4 Coste

| Concepto | Valor | Evidencia |
|---|---|---|
| Minutos incluidos (repos privados) | Free 2.000/mes; Pro 3.000; Team 3.000; repos públicos gratis | [V](https://docs.github.com/en/billing/concepts/product-billing/github-actions) |
| Precio por minuto | Linux 2 núcleos x64 0,006 USD; Linux arm64 0,005; Windows 0,010; macOS 0,062 | [V](https://docs.github.com/en/billing/reference/actions-runner-pricing) |
| Caché de Actions | 10 GB por repo sin coste y 7 días de retención; ampliable de pago (Pro/Team/Enterprise) | [V, changelog 2025-11](https://github.blog/changelog/2025-11-20-github-actions-cache-size-can-now-exceed-10-gb-per-repository/) |
| Git LFS | Free/Pro y organizaciones Free: 10 GiB de almacenamiento y 10 GiB de ancho de banda; las descargas desde Actions cuentan | [V](https://docs.github.com/en/billing/concepts/product-billing/git-lfs) |
| Pipeline completo estimado | `core` 2-3 min + `unity-tests` 8-15 min + `build-android` 20-35 min ≈ 30-50 min | [E] |
| Capacidad gratuita | ≈40-60 pipelines con build al mes, o ≈150 ejecuciones solo de pruebas | [E] |
| Exceso típico | 1.000 min extra ≈ 6 USD | [E, cálculo con 0,006 USD/min] |

Ahorro: CI de Unity solo en PR listos (no borradores) y en `main`; filtros de rutas; `cancel-in-progress`; que los agentes ejecuten las pruebas en local antes de abrir el PR. Los runners de Windows ARM64 propios no sirven para game-ci, que usa contenedores Linux x64 [E].

### 5.5 Correcciones al GDD 08 §12.1

| GDD actual | Corrección | Motivo |
|---|---|---|
| Secretos `UNITY_LICENSE`/`UNITY_EMAIL`/`UNITY_PASSWORD`; el YAML usa solo `UNITY_LICENSE` | Solo `UNITY_EMAIL` + `UNITY_PASSWORD` | game-ci/cli v0.1.63 [V](https://github.com/game-ci/cli/releases/tag/v0.1.63) |
| `ci/android-build.yml` | `.github/workflows/android.yml` (en `ci/` solo scripts como `size-gate.sh`) | GitHub Actions solo lee `.github/workflows/` [E, comportamiento estándar] |
| `setup-dotnet 8` y "`.NET 8` consola" en `tools/level-generator` | .NET 10 | .NET 8 fin de soporte 2026-11-10; .NET 10 LTS hasta 2028-11-10 [V](https://devblogs.microsoft.com/dotnet/dotnet-8-9-end-of-support/) |
| `unity-test-runner@v4`, `unity-builder@v4` sin versión de la CLI | `@v4.4.0` y `@v6.0.0` con `cliVersion` fijado | `cliVersion: 'latest'` por defecto [V](https://github.com/game-ci/unity-builder/blob/v6.0.0/action.yml) |
| `unityVersion: 6000.x.yf1` | Omitir: la CLI lo detecta de `ProjectVersion.txt` | [V](https://github.com/game-ci/unity-builder/releases) |

---

## Recomendación para este equipo y esta máquina

**Decisión**: la Unity CLI oficial con `com.unity.pipeline` es el puente con el Editor, usada desde la shell (`unity ...`), no como servidor MCP. El núcleo se escribe primero en C# puro con `dotnet test`. La CI corre en GitHub Actions con game-ci fijado. CoplayDev/unity-mcp queda documentado como plan B, sin instalar.

**Fase 0 · dueño, ≈2 h (requiere cuentas y contraseñas; los agentes no lo hacen)**

1. `winget install Unity.UnityHub` (3.22.2, MSIX arm64 [V, winget]) → iniciar sesión con la cuenta Unity y activar la licencia **Personal** desde el Hub [V](https://docs.unity3d.com/Manual/ManualActivationGuide.html).
2. Comprobar `unity --version` (el Hub instala la CLI automáticamente [V](https://docs.unity.com/en-us/unity-cli)); si no aparece: `winget install Unity.CLI` (1.0.0-beta.12 arm64 [V, winget]).
3. Instalar desde el Hub (o con `unity install <versión> -m <módulo> --cm`, consultando antes `unity install-modules --list`; no se inventan aquí IDs de módulo) **el último parche de 6000.3 LTS** (hoy 6000.3.25f1 [V](https://unity.com/releases/editor/whats-new/6000.3.25f1)) en su build **Windows ARM64** con **Android Build Support** y sus submódulos (OpenJDK, Android SDK & NDK). No usar `winget install Unity.Unity.6000` (6000.6.4f1, no LTS) [V, winget].
4. `winget install Microsoft.DotNet.SDK.10` (10.0.401 arm64 [V, winget]).
5. `git lfs install` (global) [V](https://code.claude.com/docs/en/worktrees).
6. Crear el repositorio privado en la organización de GitHub y los secretos `UNITY_EMAIL`, `UNITY_PASSWORD` (y los de keystore cuando exista) desde la web de GitHub, nunca en el chat.
7. Opcional: añadir `%LOCALAPPDATA%\Android\Sdk\platform-tools` al PATH para que los agentes usen `adb install` en el teléfono de pruebas [E].

**Fase 1 · agentes con supervisión, ≈1 día**

8. Crear el proyecto Unity 6000.3 URP 2D en la raíz del repositorio (estructura de GDD 08 §3) con el Hub o `unity projects create ... --with-pipeline` (la beta.12 añade `com.unity.pipeline` al crear [V, winget release notes]); si ya existe, `unity pipeline install`.
9. `unity vcs doctor --fix`, `unity vcs merge-setup` y `unity vcs hooks install` [V](https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/references/version-control.md); `.gitignore` desde `github/gitignore/Unity.gitignore` más `.claude/worktrees/`, `CLAUDE.local.md`, `Logs/`.
10. `unity skill install <cliente> --local` para Claude Code (confirmar el nombre del cliente con `unity skill install --help`) [V, comando; nombre del cliente NV](https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/SKILL.md).
11. Escribir `CLAUDE.md`, `.claude/rules/*`, `.claude/agents/*` (los 7 de §4.3) y `.claude/settings.json` (§4.5); scripts `scripts/unity-compile.ps1` y `scripts/unity-test.ps1`.
12. Primer trabajo de agentes: portar `tools/levelgen_proto.py` a `AF.Core`/`AF.Solver` + `tools/level-generator` en .NET 10 con tests NUnit compartidos; después el workflow `.github/workflows/android.yml` con el job `core` y, cuando haya escena, `unity-tests`.

**Reglas de operación**

- Máximo 2 Editores abiertos; un agente Unity por worktree con Editor; agentes de C# puro en worktrees de Claude Code.
- `unity command eval` siempre con aprobación; nada de MCP ni `eval` en CI.
- Fijar versiones: Editor (`ProjectVersion.txt`), `com.unity.pipeline` (manifest), acciones de game-ci por etiqueta exacta y `cliVersion`.
- Revisión semanal de las notas de versión de la Unity CLI (beta) y de game-ci/cli (cambios casi diarios en septiembre de 2026).

---

## Riesgos y plan B

| Riesgo | Probabilidad / impacto | Señal | Plan B |
|---|---|---|---|
| La Unity CLI (beta) o `com.unity.pipeline` (experimental) cambian o fallan | Media / medio | Comandos que cambian de nombre o salidas JSON distintas | Versiones fijadas; scripts propios que llaman a `Unity.exe` en batchmode (§2.1, columna de respaldo); CoplayDev/unity-mcp v10.3.0 con `DISABLE_TELEMETRY=true` como puente alternativo |
| Plugins de terceros sin binarios nativos Windows on Arm (Firebase, EDM4U, MAX, Play plugins) | Media / alto en local | Errores de carga de DLL nativas en el Editor ARM64; Unity "doesn't support packages with third-party binary dependencies that don't provide native Windows on Arm support" [V](https://docs.unity3d.com/6000.3/Documentation/Manual/system-requirements.html) | La build de referencia es la de CI (Linux x64); en local, aislar los SDK tras interfaces con implementaciones simuladas para Editor; si bloquea el desarrollo, VM x64 en la nube o PC x64 |
| La cadena Android (NDK) corre emulada en ARM64 y las builds locales son lentas | Media / bajo | Builds locales >30 min | Construir solo en CI; en local, tests y Play Mode |
| Activación Personal en CI falla (2FA, cambios de Unity, asiento retenido) | Media / alto | "No seat available", "License activation strategy could not be determined" | `cliVersion` fijado y probado; jobs serializados; `game-ci return-license`; alternativa del GDD: Unity Build Automation [NV en este informe] |
| Prompt injection que acaba en `eval` | Baja / alto | Un agente propone ejecutar código que no pidió la tarea | `eval` en `ask`; agentes de código sin `WebFetch`; revisión del `code-reviewer` y del dueño en cambios de SDK |
| Coste de CI por exceso de PR de agentes | Media / bajo | >2.000 min a mitad de mes | PR en borrador sin CI de Unity; filtros de rutas; pruebas locales obligatorias; Team (3.000 min) si hiciera falta |
| Ancho de banda LFS agotado | Media / medio | Avisos de cuota LFS | Caché de `.git/lfs` en CI (§5.3); el artista sube solo finales; comprar paquete de datos si crece |
| Reimportaciones lentas por worktree | Alta / medio | 10-20 min por worktree nuevo | `unity vcs git worktree add --seed cache`; reutilizar worktrees con nombre; limitar a 2 con Editor |
| Conflictos en escenas/prefabs | Media / medio | Fusiones fallidas | UnityYAMLMerge configurado; un dueño por prefab/escena; escenas casi vacías |
| GUID duplicados o `.meta` perdidos | Media / alto | Referencias "Missing" | `deny` sobre `*.meta`; hooks de `unity vcs hooks`; revisar `git status` antes de commit |
| Unity 6.3 LTS no basta (p. ej. el líquido no llega a 60 fps) | Baja / alto | Prototipo del GDD | Alternativa Godot 4.x ya documentada en el GDD (no evaluada en este informe) |

---

## Fuentes

Unity (oficiales)
- https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli
- https://docs.unity.com/en-us/unity-cli
- https://docs.unity.com/en-us/unity-cli/release-notes
- https://docs.unity.com/en-us/unity-cli/use-unity-cli
- https://docs.unity.com/en-us/unity-cli/unity-cli-reference
- https://docs.unity.com/en-us/unity-cli/unity-pipeline/unity-pipeline-package
- https://docs.unity3d.com/Packages/com.unity.pipeline@0.8/index.html
- https://unity.com/resources/unity-pipeline-cli-technical-walkthrough
- https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.20/manual/integration/unity-mcp-overview.html
- https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/SKILL.md
- https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/references/build-run-test.md
- https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/references/version-control.md
- https://docs.unity3d.com/Manual/test-framework/run-tests-from-command-line.html
- https://docs.unity3d.com/Manual/test-framework/reference-command-line.html
- https://docs.unity3d.com/6000.3/Documentation/Manual/EditorCommandLineArguments.html
- https://docs.unity3d.com/6000.3/Documentation/Manual/class-EditorManager.html
- https://docs.unity3d.com/6000.3/Documentation/Manual/class-VersionControlSettings.html
- https://docs.unity3d.com/6000.3/Documentation/Manual/SmartMerge.html
- https://docs.unity3d.com/6000.3/Documentation/Manual/csharp-compiler.html
- https://docs.unity3d.com/6000.1/Documentation/Manual/assembly-definition-file-format.html
- https://docs.unity3d.com/6000.3/Documentation/Manual/system-requirements.html
- https://docs.unity3d.com/Manual/ManualActivationGuide.html
- https://unity.com/releases/unity-6/support
- https://unity.com/releases/editor/whats-new/6000.3.25f1
- https://unity.com/releases/editor/whats-new/6000.3.22f1
- https://unity.com/releases/editor/whats-new/6000.6.2f1
- https://discussions.unity.com/t/is-unity6-really-run-on-windowsarm64/1606124 (2025)
- https://discussions.unity.com/t/error-it-looks-like-another-unity-instance-is-running-with-this-project-open/656599 (antigua)

Puentes comunitarios
- https://github.com/CoplayDev/unity-mcp · /releases · /blob/main/SECURITY.md · /blob/main/website/docs/architecture/transports.md · /blob/main/website/docs/architecture/telemetry.md · /blob/main/MCPForUnity/Editor/Tools/ExecuteCode.cs
- https://github.com/IvanMurzak/Unity-MCP · /releases
- https://github.com/CoderGamester/mcp-unity
- https://github.com/hatayama/unity-cli-loop
- https://www.heyuan110.com/posts/ai/2026-03-10-mcp-security-2026/ (secundaria)

Claude Code
- https://code.claude.com/docs/en/sub-agents
- https://code.claude.com/docs/en/worktrees
- https://code.claude.com/docs/en/memory
- https://code.claude.com/docs/en/permissions
- https://code.claude.com/docs/en/hooks-guide

CI y costes
- https://game.ci/docs/github/activation
- https://game.ci/docs/github/getting-started
- https://game.ci/docs/github/test-runner
- https://game.ci/docs/github/builder/
- https://github.com/game-ci/unity-builder/releases · https://github.com/game-ci/unity-builder/blob/v6.0.0/action.yml
- https://github.com/game-ci/unity-test-runner/releases/tag/v4.4.0
- https://github.com/game-ci/cli/releases · https://github.com/game-ci/cli/releases/tag/v0.1.63 · https://github.com/game-ci/cli/releases/tag/v0.1.66 · https://github.com/game-ci/cli/pull/246
- https://hub.docker.com/r/unityci/editor/tags
- https://docs.github.com/en/billing/concepts/product-billing/github-actions
- https://docs.github.com/en/billing/reference/actions-runner-pricing
- https://docs.github.com/en/billing/concepts/product-billing/git-lfs
- https://github.blog/changelog/2025-11-20-github-actions-cache-size-can-now-exceed-10-gb-per-repository/ (2025)

.NET
- https://devblogs.microsoft.com/dotnet/dotnet-8-9-end-of-support/

Comprobaciones locales de solo lectura (2026-10-06): `winget show Unity.CLI` (1.0.0-beta.12, MSIX arm64, 2026-10-01), `winget show Unity.UnityHub` (3.22.2 arm64), `winget show Unity.Unity.6000` (6000.6.4f1, instalador Windows Arm64), `winget show Microsoft.DotNet.SDK.10` (10.0.401 win-arm64), `winget show astral-sh.uv` (0.12.23 aarch64); API pública de GitHub (estrellas, licencias, versiones y avisos de seguridad) y API de Docker Hub (etiquetas `unityci/editor`).
