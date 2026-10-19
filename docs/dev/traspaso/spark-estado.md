# Estado del equipo Spark: el Reto del río

Encargo: `docs/dev/traspaso/spark-reto-del-rio.md` (de `origin/integracion-fase1-fase2`). Una entrada nueva arriba por tarea o bloque.

---

## 2026-10-19 · R0 · Arranque y línea base: **BLOQUEADA** (falta el entorno)

**Rama:** `spark/reto-del-rio`, creada desde `origin/integracion-fase1-fase2` en `80b26b4` («Deja en el encargo del Spark las respuestas del dueño a R-Q1-R-Q10 del Reto del río») y empujada a `origin`. `git lfs pull` hecho sin errores.

**Máquina (sección 1 del encargo):**

| Comprobación | Comando | Resultado |
|---|---|---|
| Equipo | `Win32_ComputerSystem.Model` / `Win32_Processor.Name` | ProArt P14 H7407BA · NVIDIA RTX Spark N1X (20 núcleos) |
| Arquitectura | `$env:PROCESSOR_ARCHITECTURE` | `ARM64` → `-Architecture arm64` / `--architecture arm64` |
| RAM | `(Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB` | 64,0 GB (de sobra: se podrían usar hasta 3 Editores; aún no aplica) |
| Python | `python --version` | 3.14.7 (ARM64) ✔ |
| `jsonschema` | `python -c "import jsonschema"` / `py -3.14 -m pip show jsonschema` | **✘ No instalado** (`ModuleNotFoundError`) |
| .NET SDK | `dotnet --list-sdks` | **✘ Ningún SDK.** Solo el host y los runtimes 8.0.32 (`Microsoft.NETCore.App`, `WindowsDesktop.App`). `global.json` pide 10.0.401 |
| Editor de Unity | `Test-Path "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe"` | **✘ No existe.** En `Hub\Editor` solo está `6000.3.26f1` (`unity editors` lo da como x86_64). El encargo prohíbe abrir otra versión sobre el proyecto |
| Unity CLI | `unity --version` | 1.0.0-beta.13 ✔ (alias `%LOCALAPPDATA%\Microsoft\WindowsApps\unity.exe`) |
| git LFS | `git lfs version` | 3.7.1 (windows arm64) ✔ |

**Verificación completa de `CLAUDE.md` (paso 3):** no se puede ejecutar ninguno de los comandos obligatorios:
- `dotnet build dotnet/AmazonFill.slnx -warnaserror` y `dotnet test`: falla el arranque del SDK («A compatible .NET SDK was not found. Requested SDK version: 10.0.401»).
- `python tools/validate_schema.py`: necesita `jsonschema`.
- `scripts/unity-compile.ps1` y `scripts/unity-test.ps1` (EditMode y PlayMode): necesitan Unity 6000.3.25f1 ARM64.

No hay línea base: **sin recuentos**. No he instalado nada (regla dura 4 de `CLAUDE.md`; el encargo, sección 1: «no lo instales tú: si falta, pregunta al dueño»).

**Me paro aquí**, como manda R0 («si algo sale en rojo en la base, para y anótalo»). R1-R12 dependen de R0 y no he empezado ninguna.

### Pendiente del dueño (para desbloquear R0)

1. **.NET SDK 10.0.401** para Windows ARM64 en `C:\Program Files\dotnet` (lo fija `global.json`, con `rollForward: latestFeature`).
2. **`jsonschema`** en Python 3.14 (`py -3.14 -m pip install jsonschema`).
3. **Unity 6000.3.25f1** (changeset `e1dba0a9aba4`), Editor **Windows ARM64**, con el módulo Android, en `C:\Program Files\Unity\Hub\Editor\6000.3.25f1\`, desde el Hub (la CLI no instala; `CLAUDE.md`, «Stack fijado»). La licencia la activa el dueño.
   - El 6000.3.26f1 x86_64 que hay instalado **no se usa**: abrirlo sobre el proyecto lo actualizaría.
4. Avisar al Spark cuando esté para repetir R0 y medir la línea base.

### Decisiones del dueño registradas (para que el integrador las lleve a `docs/dev/decisiones.md`)

Las R-Q1 a R-Q10 de la sección 5 del encargo, decididas por el dueño el 2026-10-20 («Acepto tus propuestas para R-Q1 a R-Q10 del Reto del río; déjalas en el encargo del Spark»). Se aplicarán tal cual cuando se desbloquee el trabajo:

| Id | Decisión |
|---|---|
| R-Q1 | Cada banda usa solo los tintes ya estrenados en su tramo de la campaña: B1 (K 6-7) con los de 1-40 y `max_pares_riesgo` 0; B2 (K 8-9) con los de 1-80 y como mucho 1; B3 (K 10-11) con los de 1-200 y como mucho 2 |
| R-Q2 | El agua (`water_base`) y el fondo rotan por los mundos 1-9 según la fecha: el agua y el kit de fondo del mundo de ese día |
| R-Q3 | Manda la fecha en que se empezó la partida: se sigue con ese tablero y se cobra ese premio aunque se gane pasada la medianoche |
| R-Q4 | La partida del Reto se reanuda al volver a abrir la app el mismo día; al cambiar de día se descarta |
| R-Q5 | El bloque `daily_challenge` del perfil añade la última fecha cobrada, el premio pendiente de validar y los sellos del mes (además de `last_date`, `result` y `streak_days`) |
| R-Q6 | `daily_overrides` = `{fecha: {banda: id del pool}}` |
| R-Q7 | La Pororoca sale cada 7 días seguidos ganados (cada racha completa); un día sin ganar rompe la racha; el marco de país de los sellos queda fuera hasta que existan cosméticos |
| R-Q8 | Los comodines son los de la campaña salvo los que el GDD excluye en el Reto (Cuia de rescate y Salto del Boto), y gastan el stock igual que en la campaña |
| R-Q9 | `result = fail` en `af_daily_challenge` es salir sin ganar ese día |
| R-Q10 | La anti-repetición (03 §17 #9) y el tope de trampas tempranas (03 §17 #3) se miden en el pool y se informan como aviso, sin ser compuerta |

### Riesgos y notas

- Las fechas: el encargo y estas decisiones van fechados el 2026-10-20; el reloj de esta máquina dice 2026-10-19.
- `git switch -c … origin/integracion-fase1-fase2` deja como upstream la integración; el push con `-u origin spark/reto-del-rio` lo corrige para que ningún `git push` vaya a `integracion-fase1-fase2`.

### Enganches para el integrador

Ninguno todavía.
