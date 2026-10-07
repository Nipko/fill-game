---
paths:
  - ".github/**"
  - "ci/**"
  - "scripts/**"
  - "Assets/_Project/Editor/CI/**"
---

# CI/CD, compuertas y scripts

Fuentes: GDD 08 §6 y §12-§14; `docs/setup/plan-instalacion.md` (pasos s26 y s30-s32, riesgos R1, R6, R9 y R13); `docs/setup/research/c-agentes-programadores-unity.md` §5.

- **Workflows solo en `.github/workflows/`** (GitHub no lee `ci/`). En `ci/` van scripts de compuerta; en `scripts/`, los envoltorios que usan los agentes.
- **Versiones fijadas, nunca `latest` ni etiquetas móviles:** `game-ci/unity-test-runner@v4.4.0`, `game-ci/unity-builder@v6.0.0` con `cliVersion: v0.1.72`, imagen `unityci/editor:ubuntu-6000.3.25f1-android-3`; acciones de GitHub por etiqueta exacta; .NET con `global-json-file: global.json`. El Editor se lee de `ProjectSettings/ProjectVersion.txt` (no se repite `unityVersion`).
- **Licencia Personal:** solo `UNITY_EMAIL` + `UNITY_PASSWORD` (cuenta dedicada sin 2FA) como secretos; `UNITY_LICENSE` (.ulf) no funciona en runners efímeros. Todos los jobs que activan Unity comparten `concurrency: { group: unity-personal-seat, cancel-in-progress: false }`.
- **Secretos:** solo como `${{ secrets.X }}`; nunca en `run:` con `echo`, nunca en logs ni artefactos. Los crea el dueño (`gh secret` está bloqueado para agentes). `permissions:` mínimos por workflow.
- **Coste (2.000 min/mes y 10 GiB de LFS):** el job `core` (`dotnet test`, `validate_schema.py`) en cada PR; Unity solo en PR listos (no borradores) y en `main`, con filtros de rutas, `cancel-in-progress` y cachés de `Library` y `.git/lfs`.
- **Job `core` sin telemetría:** `env:` con `DOTNET_CLI_TELEMETRY_OPTOUT: 1`, `TESTINGPLATFORM_TELEMETRY_OPTOUT: 1` (Microsoft.Testing.Platform, que llega con NUnit3TestAdapter 6.3.0) y `DOTNET_NOLOGO: 1`, igual que `.claude/settings.json`.
- **Compuertas que bloquean el build de release:** `pwsh ci/size-gate.ps1 <aab> -RequireBundletool` (≤ 80 MB, `bundletool get-size total`), `CHECK16_REQUIRE_ZIPALIGN=1 bash ci/check-16kb.sh <aab|apks>` (con `BUNDLETOOL_JAR` y java para el APK universal de un `.aab`), `targetSdk == 36`, `minSdk == 26`, IL2CPP ARM64 y `applicationId` distinto del provisional `com.amazonfill.dev`.
- **Nunca** `unity command eval` ni MCP en CI. Ningún AAB a Google Play (ni a la pista interna) con el `applicationId` definitivo `com.<estudio>.tintalu` hasta tener el dictamen de marca; los builds de prueba usan `com.amazonfill.dev`. Y nunca subir nada a Google Play sin la aprobación del dueño, que es quien publica.
- **Scripts:** PowerShell 7 (`#Requires -Version 7.2`) o bash con `set -uo pipefail`; mensajes ASCII (la consola de Windows corrompe acentos al capturar la salida); cabecera con uso y códigos de salida; fallar cerrado (si falta una herramienta exigida, código distinto de 0, nunca "OK"). Probar al menos las ramas de error antes de terminar.
- **Builds locales** en la máquina ARM64: sin verificar hasta la compuerta s26; el build de referencia es el del CI Linux x64.
