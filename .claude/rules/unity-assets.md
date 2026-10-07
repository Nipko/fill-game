---
paths:
  - "Assets/**/*.{unity,prefab,asset,meta,mat,anim,controller,overrideController,spriteatlas,spriteatlasv2,shadergraph,shadersubgraph,lighting,preset,mixer,physicsMaterial2D,playable,mask,brush}"
  - "Packages/**/*.meta"
  - "ProjectSettings/**"
  - "Packages/manifest.json"
  - "Packages/packages-lock.json"
---

# Assets serializados por Unity: solo a través del Editor

Fuentes: GDD 08 §3; `docs/setup/research/c-agentes-programadores-unity.md` §3 y §4.5; skill oficial `unity-cli`.

- Estos archivos son YAML (o JSON) que escribe Unity con GUID y referencias internas. **Léelos** para entender escenas y prefabs (Force Text está activo), pero **no los edites a mano**. Los permisos del proyecto (`.claude/settings.json`, activo cuando el dueño apruebe `.claude/settings.proposed.json`) bloquean `Edit` en `*.unity`, `*.prefab`, `*.meta` y `ProjectSettings/**`, y piden confirmación para `*.asset` y `Packages/manifest.json`. Mientras no estén activos, esta regla obliga igual.
- **Cómo se cambian:**
  1. Con un Editor abierto y la Unity CLI: `unity command ...` (incluido `eval`) siempre pide aprobación; explica qué hará el comando antes de lanzarlo.
  2. `ProjectSettings/`: con un método de Editor versionado en `Assets/_Project/Editor/` (p. ej. `AF.EditorTools.ProjectSetup.ApplyAndroidDefaults`) ejecutado con `unity run . -- -executeMethod ...`. Así el cambio queda en código revisable.
  3. Si no hay Editor: describe el cambio paso a paso (escena, objeto, componente, campo, valor) para que lo haga un humano o un agente con Editor, y déjalo en la entrega.
- **`.meta`:** los crea Unity al importar. Si añades un `.cs`, `.asmdef`, `.json` o asset nuevo, ejecuta `scripts/unity-compile.ps1` (o abre el Editor) para que aparezca su `.meta` y commitéalo tal cual. Nunca copies, inventes ni borres un `.meta` a mano: rompe GUID y referencias ("Missing").
- **Escenas y prefabs pequeños:** 3 escenas casi vacías (`Boot`, `Map`, `Game`); todo lo demás en prefabs con un solo dueño. Dos agentes no tocan el mismo prefab en paralelo.
- **Conflictos:** se resuelven con UnityYAMLMerge (`unity vcs merge-setup`, paso s23), nunca editando marcadores de conflicto en YAML.
- **`Packages/manifest.json` y `packages-lock.json`:** paquetes con versión exacta; añadir, quitar o subir un paquete requiere aprobación del dueño (ver `.claude/rules/sdk.md`).
- **Un Editor por carpeta, máximo 2 en la máquina:** nunca batchmode sobre una carpeta con un Editor abierto.
