# Equipo de agentes programadores de Tintalú

Fecha: 2026-10-06 · Paso s11 de `docs/setup/plan-instalacion.md` · Formato verificado contra la documentación oficial de Claude Code (code.claude.com/docs: memory, sub-agents, permissions, settings, worktrees, costs, errors, interactive-mode).

**Decisión del dueño (2026-10-06):** todos los programadores son agentes de IA expertos (Claude Code con subagentes, modelo Opus). Los humanos son el dueño, un artista 2D freelance, un asesor cultural y un abogado de marcas.

**Archivos del equipo:**
- `CLAUDE.md`: instrucciones comunes. Se carga en cada sesión y en cada subagente.
- `.claude/agents/*.md`: los siete subagentes.
- `.claude/rules/*.md`: reglas por ruta. Se cargan cuando un agente lee o edita un archivo que casa con su `paths`.
- `.claude/settings.proposed.json`: permisos y variables de entorno propuestos (pendiente de tu aprobación, paso s12).
- `scripts/` y `ci/`: verificación de Unity y compuertas de build.

---

## 1. Quién es quién

Todos usan `model: opus`. Los que tienen worktree trabajan en una copia aislada del repositorio, en `.claude/worktrees/` y en su propia rama.

| Agente | Cuándo usarlo | Puede tocar | Herramientas | Aislamiento | Verifica con |
|---|---|---|---|---|---|
| `tech-lead` | Al empezar una funcionalidad, una fase o un cambio entre módulos; dudas de arquitectura | Nada (solo lectura) | Read, Grep, Glob, Bash (lectura) | Checkout principal | Cada tarea cita el GDD; `git status` |
| `core-dev` | Reglas R1-R16, BoardState, mecánicas, hash, PRNG, solver, bot, Aguas turbias | `Packages/com.amazonfill.core/**/*.cs`, `dotnet/AF.Core.Tests/DotnetOnly/` | + Edit, Write, PowerShell | Worktree | `dotnet build -warnaserror`, `dotnet test` (+ EditMode si existe Unity) |
| `level-pipeline-dev` | `tools/level-generator`, `tools/level-qa`, `schemas/`, paquetes de niveles, scripts Python | `tools/**`, `schemas/**`, `Assets/_Project/Data/Levels/` y `Daily/` | + Edit, Write, PowerShell | Worktree | `dotnet test`, `validate_schema.py`, `level-qa` |
| `unity-gameplay-dev` | Shader del líquido, UI/HUD, GameFlow, Levels, Boosters, Economy, LiveOps | `Assets/_Project/Scripts/{Presentation,UI,GameFlow,...}`, `Shaders/`, `Tests/` | + Edit, Write, PowerShell, Skill | Worktree con un solo Editor | `unity-compile.ps1`, `unity-test.ps1` EditMode y PlayMode |
| `sdk-integrator` | MAX/UMP, IAP, Firebase, Play Games, PAD, notificaciones, consentimiento, **solo con tu aprobación previa** | `Scripts/{Ads,IAP,Analytics,RemoteConfig,Save,Platform,Consent,Backend}`, `Plugins/Android`, `manifest.json` (pide permiso) | + Edit, Write, PowerShell, Skill | Worktree; `permissionMode: default` (pregunta todo) | Scripts de Unity, lista de privacidad, build en CI |
| `build-release` | Workflows, compuertas (80 MB, 16 KB, targetSdk), `scripts/`, `ci/`, métodos de build, LFS y merge de YAML | `.github/`, `ci/`, `scripts/`, `Assets/_Project/Editor/CI/` | + Edit, Write, PowerShell, Skill | Worktree | Ramas de error de cada script, `merge-setup --check` |
| `code-reviewer` | Siempre, después de cada tarea de un dev y antes de cada PR o merge | Nada (solo lectura) | Read, Grep, Glob, Bash | Checkout principal | Lee el diff completo y ejecuta él mismo las pruebas |

**Lo que no hace ningún agente:**
- Navegar por la web. Ningún subagente tiene WebFetch ni WebSearch: si hace falta investigar algo, lo hace la sesión principal y le pasa el resultado al agente.
- Lanzar otros agentes. Ninguno tiene la herramienta Agent: reparte el trabajo la sesión principal, con el plan del tech-lead.
- Tocar secretos, cuentas o licencias.

---

## 2. Antes de usar el equipo (una sola vez)

1. **s12, tú:** revisa `.claude/settings.proposed.json` (resumen en la sección 7). Si estás de acuerdo, renómbralo a `.claude/settings.json` (o di "aprobado" en el chat para que lo haga un agente). Hasta entonces los permisos no están activos, aunque `CLAUDE.md` y las reglas obligan igual.
2. **s13:** primer commit local (hecho: `88d3331`, ya en el remoto `origin`). Los agentes con `isolation: worktree` lo necesitan: sin un commit no hay rama de la que partir.
3. **Reinicia Claude Code** en la raíz del repositorio. Así carga `.claude/agents/`, que se creó durante esta sesión, y el nuevo PATH con `dotnet`. La primera vez acepta el diálogo de confianza de la carpeta: los permisos `allow` del proyecto solo se aplican cuando la carpeta es de confianza.
4. **Comprueba:**
   - `/agents` muestra los siete;
   - `/context` muestra `CLAUDE.md`;
   - `/permissions` muestra las reglas.
5. **Para trabajar con Unity:** la licencia Personal está activa y el proyecto Unity existe en el checkout principal desde el 2026-10-06 (s15-s25), donde `scripts/unity-*.ps1` ya compilan y pasan los tests. Los worktrees de los agentes parten de un commit, así que solo tienen `Assets/` y `ProjectSettings/` cuando el proyecto está commiteado (s29); sin eso, los scripts salen con el código 3 ("no encuentra el proyecto Unity") y `unity-gameplay-dev` no puede trabajar en su worktree. Recuerda: un Editor por carpeta y como máximo 2 abiertos.

---

## 3. Flujo de una tarea

```
tú (objetivo) → tech-lead (plan) → sesión principal reparte → agente dev en su worktree
     → pruebas del dev → code-reviewer → (cambios: vuelve al dev) → PR → tú apruebas el merge
```

1. **Objetivo.** Lo das tú en la sesión principal, por ejemplo: "Fase 1: reglas de vertido R1-R8 con tests".
2. **Plan.** `@agent-tech-lead descompón: <objetivo>`. Devuelve tareas pequeñas, cada una con agente, archivos, criterios de aceptación, verificación, dependencias y las decisiones que te tocan. Las decisiones bloqueantes se te preguntan antes de seguir.
3. **Reparto.** La sesión principal delega cada tarea en su agente: `@agent-core-dev <tarea 1 del plan, con criterios>`. Las tareas independientes pueden ir en paralelo (límites en la sección 6). Antes de repartir, la sesión principal debe estar en `main` y al día.
4. **Implementación.** El agente trabaja en su worktree, en una rama nueva que parte del HEAD local gracias a `worktree.baseRef: "head"`. Ejecuta su verificación obligatoria y hace commit en su rama. Nunca hace push. Al terminar entrega:
   - un resumen;
   - la rama y el worktree;
   - las pruebas con su resultado;
   - las preguntas abiertas.
5. **Revisión.** `@agent-code-reviewer revisa la rama <rama> (worktree <ruta>) contra la tarea <n>`. El revisor lee el diff completo, ejecuta las pruebas y devuelve **APROBADO** o **CAMBIOS NECESARIOS**.
6. **Correcciones.** Si hay cambios, pide a la sesión principal "continúa el trabajo de core-dev con estos hallazgos". El subagente se reanuda con todo su contexto (SendMessage). Luego se repiten los pasos 4 y 5.
7. **PR y merge:**
   - **Antes de tener repositorio remoto (s14):** el "PR" es la rama revisada. Con tu "ok", la sesión principal hace `git merge --no-ff <rama>` en `main` y luego borra el worktree (`git worktree remove`, que pide permiso).
   - **Con repositorio remoto:** se hace `git push -u origin <rama>` y `gh pr create` con descripción, pruebas y riesgos; los dos piden permiso. Después corre el CI (job `core` en cada PR; Unity solo en PR listos y en `main`). El merge (`gh pr merge`) siempre pide tu permiso.
8. **Cambios que solo tú apruebas:** los que tocan SDK, `Packages/manifest.json`, permisos de Android, versiones, CI o un contrato del GDD necesitan tu revisión explícita, además de la del code-reviewer.

---

## 4. Qué decides tú (y nadie más)

- **Producto:** prioridades, alcance de cada fase y preguntas abiertas del GDD (`docs/GDD.md` §6). Por ejemplo, la paridad del PRNG con el prototipo (riesgo R11) y los pendientes V3 y V4.
- **Cuentas y accesos:** Unity (`unity auth`, licencia Personal), GitHub (organización, repositorio, secretos del CI), Google Play Console, Firebase, AppLovin; cuenta dedicada sin 2FA para el CI.
- **Dinero y firma:** pagos, créditos de uso (`/usage-credits`), keystore de subida y Play App Signing.
- **Dependencias:** añadir o actualizar un SDK, un paquete, un permiso de Android o una versión (Unity, .NET, NUnit, game-ci).
- **Configuración de los agentes:**
  - aprobar `.claude/settings.json`;
  - cambiar el modelo de un agente (por ejemplo, volver a Sonnet; ver la sección 8);
  - ampliar los permisos.
- **Publicación:**
  - cualquier subida a Google Play;
  - el `applicationId` definitivo (después del dictamen del abogado de marcas);
  - el texto de la ficha y la clasificación IARC.
- **Con los otros humanos:** encargos al artista y al asesor cultural, y su visto bueno.

---

## 5. Cómo lanzar a cada agente

**Dentro de una sesión** (lo habitual): menciona al agente con `@` para garantizar que se usa:
```
@agent-tech-lead descompón la Fase 1 del GDD 09 §2 en tareas para el equipo
@agent-core-dev implementa la tarea 3 del plan: R4 y R5 (02 §1) con tests compartidos
@agent-level-pipeline-dev crea el esqueleto de tools/level-qa que valide un paquete contra el esquema
@agent-unity-gameplay-dev implementa la cola de 2 vertidos del tap-tap (02 §3.2) en GameFlow
@agent-sdk-integrator integra Unity IAP 5.4.4 tras AF.IAP con fake de editor (aprobado por el dueño el <fecha>)
@agent-build-release crea .github/workflows/ci.yml con el job core (plan s30)
@agent-code-reviewer revisa la rama <rama> contra la tarea 3
```
También sirve pedirlo en lenguaje natural ("usa el subagente core-dev para..."), pero entonces Claude decide si delega.

**Toda una sesión como un agente:** útil para trabajos largos en paralelo, en terminales separadas.
```
claude -n core-prng --worktree core-prng --agent core-dev
claude -n revision --agent code-reviewer
```
`--worktree <nombre>` crea `.claude/worktrees/<nombre>/` en la rama `worktree-<nombre>`, y `-n` le pone nombre a la sesión para poder reanudarla.

**Agentes de Unity con un Editor propio y `Library` precargada:** crea el worktree con la CLI de Unity (pide permiso). Así no tienes que esperar 10-20 minutos de reimportación:
```
unity vcs git worktree add <rama> --seed cache
cd <ruta que indique la CLI>
claude -n hud --agent unity-gameplay-dev
```

---

## 6. Paralelismo y recursos de la máquina

- **C# puro:** hasta 3-4 agentes a la vez (`core-dev`, `level-pipeline-dev`, `build-release`). `dotnet test` tarda segundos y no abre Unity.
- **Con Unity:** como máximo **2 Editores** en total, contando el interactivo y los de batchmode (15,6 GB de RAM; cada Editor usa entre 2 y 4 GB). Un Editor por carpeta y nunca batchmode sobre una carpeta abierta: los scripts lo detectan y salen con el código 5.
- **Primera compilación en un worktree nuevo:** importa todo el proyecto (10-20 min). Usa `-TimeoutSec 3600`.
- Cierra Android Studio mientras Unity compila.
- **Limpieza:**
  - un worktree sin cambios se borra solo;
  - uno con cambios se queda hasta que se fusiona y se borra con `git worktree remove` (pide permiso);
  - la limpieza periódica de Claude Code solo borra worktrees de subagentes sin trabajo pendiente (ni cambios ni commits sin publicar) y más antiguos que `cleanupPeriodDays` (30 días por defecto). Mientras no haya repositorio remoto, todos los commits cuentan como "sin publicar": tras cada merge, borra el worktree a mano.

---

## 7. Permisos propuestos (`.claude/settings.proposed.json`)

Cada regla de comandos existe en dos versiones, `Bash(...)` y `PowerShell(...)`, porque los agentes usan las dos shells en Windows. Las reglas de archivos (`Edit(...)`, `Read(...)`) valen para todas las herramientas. El archivo se generó con un script para que las dos listas sean idénticas: 96 reglas allow, 148 ask y 67 deny. Orden de evaluación: **deny → ask → allow**. La primera regla que casa decide, y un allow nunca abre un deny.

**Sin preguntar (allow):**
- `dotnet build/test/run`, `dotnet --info`, `dotnet --list-sdks`;
- `python tools/*`;
- `pwsh -NoProfile -File scripts/*` y `ci/*`; `bash ci/*`;
- Unity CLI: `test`, `run`, `recompile`, `status`, `editors` (consulta), `license status`, `auth status`, `list`, `vcs status` y `vcs merge-setup --check`;
- git local: `status`, `diff`, `log`, `show`, `branch`, `add`, `commit`, `switch`, `fetch`, `worktree list`;
- `gh` de solo lectura (PR, checks, runs, issues).

**Siempre preguntan (ask):**
- **Unity CLI:**
  - `unity command`/`cmd` (incluye `eval`, que ejecuta C# en el Editor);
  - `install`, `install-modules` y cualquier `--allow-install`;
  - `pipeline`, `build`, `open`, `close`, `watch`;
  - subcomandos que modifican Editores (`editors add/default/prune/upgrade`, `editors module add/remove`, `editor`), `install-path --set`;
  - `uninstall`, `self-update`, `hub`;
  - `license return`, `config set/unset/proxy`, `analytics`, `bug`, `skill install`;
  - los `vcs` que cambian git (`hooks`, `doctor --fix`, `merge-setup` sin `--check`, `git ...`).
- **git y GitHub:**
  - `git push` (también con `git -C`), `reset --hard`, `clean`, `rebase`, `restore`, `checkout --`;
  - borrar ramas o worktrees, `commit --no-verify`, `git config`, `lfs migrate`;
  - `gh pr create/merge/close`, `gh repo create`, `gh release`, `gh workflow run`.
- **Ediciones:** `*.asset`, `Packages/manifest.json` y `packages-lock.json`, `global.json`, `Directory.Build.props`, `BannedSymbols.txt`, `CLAUDE.md`, `.gitignore`, `.gitattributes`.

**Bloqueadas (deny):**
- editar `*.unity`, `*.prefab`, `*.meta` y `ProjectSettings/**`;
- leer secretos: `*.jks`, `*.keystore`, `keystore.properties`, `*.p12`, `*.pfx`, `*.pepk`, `*.ulf`, `google-services.json`, `GoogleService-Info.plist`, `.env*`, `C:\ProgramData\Unity`, `~/.ssh`, `~/.git-credentials` y la configuración de `gh`;
- `unity auth login/switch/logout/revoke`, `unity license activate`, `unity setup`, `unity mcp`, `unity shell`, `unity self-uninstall`;
- `gh secret`, `gh auth token/login/refresh/logout`, `gh repo delete`;
- cualquier `git push` forzado (`--force`, `-f`, `+rama`).

**Además:**
- `disableBypassPermissionsMode: "disable"`: nadie puede saltarse los permisos con `--dangerously-skip-permissions`.
- `env`: `UNITY_NON_INTERACTIVE=1`, `UNITY_NO_BANNER=1`, `DOTNET_CLI_TELEMETRY_OPTOUT=1`, `TESTINGPLATFORM_TELEMETRY_OPTOUT=1` (telemetría de Microsoft.Testing.Platform, que llega con NUnit3TestAdapter 6.3.0), `DOTNET_NOLOGO=1`. `UNITY_NO_CRASH_REPORT` lo decides tú y no está incluido.
- `worktree.baseRef: "head"`: los worktrees parten de tu HEAD local, así una tarea encadenada ve el trabajo aún no publicado.

**Límites de los permisos.** Son una barandilla, no un sandbox:
- Una regla `Bash(git push *)` no reconoce formas alternativas como `/usr/bin/git push`.
- Las reglas `Read` no alcanzan a subprocesos como Python o el propio Editor de Unity, que pueden leer cualquier archivo con tus permisos de Windows.

La protección real es tener en la máquina solo los secretos imprescindibles, y fuera del repositorio. Otro matiz: el `permissionMode: default` de `sdk-integrator` solo se respeta si la sesión principal está en modo Manual, Plan o dontAsk. En los modos acceptEdits o auto, el subagente hereda el modo de la sesión.

El JSON se validó con `python -m json.tool` y contra el esquema oficial `https://json.schemastore.org/claude-code-settings.json`, con 0 errores.

---

## 8. Límites de uso de Opus y cómo reanudar

**Cómo funcionan los límites:**
- Con suscripción de claude.ai hay una ventana de sesión (unas 5 horas, móvil) y otra **semanal**. Las dos se comparten entre todos los modelos y con el chat de Claude.
- Además puede aparecer un límite propio de Opus: `You've hit your Opus limit · resets ...`.
- Siete agentes Opus trabajando en paralelo consumen el límite semanal mucho más rápido que una sola sesión. La documentación avisa de que un pico de trabajo intenso, como un gran reparto en paralelo, puede agotar la cuota semanal antes de que se reinicie la ventana de sesión.

**Para gastar menos:**
- Una tarea por subagente, con un prompt concreto (archivos, criterios y comandos) en lugar de "mira todo el repositorio".
- Las ediciones triviales las hace la sesión principal, sin lanzar un subagente.
- No lances más de 3-4 agentes a la vez.
- `/clear` entre tareas que no tienen relación. `CLAUDE.md` corto: se carga en cada subagente.
- `/usage` muestra cuánto queda y cuándo se reinicia. Para verlo siempre, añade `rate_limits` a la línea de estado.

**Cuando se agota el límite:**
- **Límite de sesión:** la CLI espera sola en la sesión abierta y continúa la tarea al reiniciarse (`Continuing automatically at ...`; `Esc` cancela). En la app de escritorio hay que marcar "Auto-continue when limits reset". Deja la sesión abierta: los permisos se siguen pidiendo, así que la tarea puede quedar parada en una pregunta.
- **Límite semanal:** no hay espera automática si el reinicio está a más de 24 horas. Ese día no se puede seguir con Opus.
- **Límite propio de Opus:** cambiar de modelo permitiría seguir, pero tu decisión es usar solo Opus. Las opciones son esperar al reinicio, comprar créditos de uso (`/usage-credits`, es tu decisión) o autorizar de forma temporal `model: sonnet` en los agentes de menor riesgo. El plan original lo preveía para `level-pipeline-dev` y `build-release`: basta con cambiar el campo `model:` en su archivo, y se revierte después.

**No se pierde trabajo:**
- Cada agente hace commit en su rama al terminar cada paso verificado.
- Los worktrees con cambios no se borran.
- Las transcripciones de subagentes se guardan 30 días.

**Cómo reanudar:**
| Qué | Cómo |
|---|---|
| La última conversación en esta carpeta | `claude --continue` |
| Una sesión con nombre | `claude --resume core-prng` (o `/resume` dentro de Claude) |
| Una sesión que trabajaba en un worktree | `claude --worktree core-prng --resume`, desde la raíz del repositorio |
| Un subagente que se quedó a medias | En la sesión principal: "continúa el trabajo de core-dev donde lo dejó"; se reanuda con su contexto completo |
| Ver qué quedó abierto | `git worktree list`, `git branch`, `git log --oneline main..<rama>` |

---

## 9. Mantenimiento del equipo

- **Cambios en agentes, reglas o `CLAUDE.md`:** se hacen por PR, porque los agentes los leen como instrucciones. Editar `CLAUDE.md` pide permiso, y `.claude/` es una ruta protegida de Claude Code.
- **Herramientas en beta:** una vez por semana, revisa las notas de versión de la Unity CLI (`unity changelog`) y de game-ci. Fija la versión nueva solo en un PR de `build-release` con el CI en verde.
- **Al crear el proyecto Unity (hecho el 2026-10-06):**
  - s21: `unity pipeline install`;
  - s23: `unity vcs merge-setup` y `unity vcs hooks install`;
  - s24: `unity skill install claude-code --local`. Si instalas el plugin oficial `unity setup claude` (decisión tuya), quita la skill local para no cargar dos copias.
- **Puente con el Editor:** sin MCP. Si algún día hiciera falta, la opción oficial es `unity mcp configure claude-code --local`, que escucha solo en localhost. Está bloqueada (deny) hasta que la apruebes.
