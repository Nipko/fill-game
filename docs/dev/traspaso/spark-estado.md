# Estado del equipo Spark: el Reto del río

Encargo: `docs/dev/traspaso/spark-reto-del-rio.md` (de `origin/integracion-fase1-fase2`). Una entrada nueva arriba por tarea o bloque.

---

## 2026-10-19 · R3 · Modo `--daily` del generador: HECHA

- **Commits** (en `spark/reto-del-rio-r3`, fusionados con `--no-ff` en `spark/reto-del-rio` como `c87a181`):
  - `e8783b9`: arreglos menores de la revisión de R2;
  - `bb1dad2`: el modo `--daily`;
  - `ee48f52`: la muestra `tools/out/daily_sample/daily_pool_v1.json` (30 tableros, 20.144 bytes, SHA-256 `61cf6bef…3ac1`) y `tools/out/daily_sample_report.md`. Se commitea como las otras muestras de `tools/out/`.
- **Archivos nuevos** en `tools/level-generator/Daily/`: `DailyRunner.cs`, `DailyTints.cs` (copia adaptada de `TintAssigner`, que no cambia), `DailyPackWriter.cs`, `DailyReport.cs` y `DailyCommand.Generate.cs`.
- **Uso:**
  ```
  dotnet run --project tools/level-generator -- --daily --spec tools/level-generator/specs/daily_v1.json --bands 1,2,3 --days 0-729 --out <dir> [--report <md>] [--check] [--threads N]
  ```
  Códigos de salida: 0 (también con avisos), 1 (un día sin tablero o `--check` distinto), 2 (uso).
- **Verificación:**

  | Comando | Resultado |
  |---|---|
  | `--daily` de la muestra (bandas 1-3, días 0-9) | exit 0; `--check` idéntico con 20 hilos y con 1 |
  | `python tools/validate_schema.py --pack tools/out/daily_sample/daily_pool_v1.json` | PASS |
  | `python tools/validate_schema.py` | 103/103 |
  | `--self-test daily` | todo PASS: 3 días por banda idénticos byte a byte en paralelo y con 1 hilo; la semilla reproduce `d2_n0001` |
  | `--self-test` general | todo PASS |
  | `dotnet build` de `tools/level-generator` y de `dotnet/AmazonFill.slnx` con `-warnaserror` | 0 avisos |
  | `dotnet test` | 981 (975 + 6 omitidos) |

- **Pool completo** (3 × 730), generado fuera del repositorio: 6,1 s con 20 hilos y 1.445.059 bytes. Valida con `--pack` y es reproducible. **No se publica** (R5 bloqueada).
- **Muestra, por banda (10 días cada una):**

  | Banda | bot_fail mín · mediana · máx | par mín · mediana · máx | Fuera de ventana | Pares > tope |
  |---|---|---|---|---|
  | B1 | 0,400 · 0,438 · 0,600 | 13 · 16,5 · 21 | 0 | 0 |
  | B2 | 0,625 · 0,725 · 0,900 | 19 · 20,5 · 25 | 2 (`d2_n0002` 0,85, `d2_n0006` 0,90; los dos K 8 con `fill`) | 5 (los de K 9) |
  | B3 | 0,750 · 0,950 · 1,000 | 21 · 22,5 · 25 | 0 | 10 (todos) |

- **En el pool completo:**
  - B1: 13 días fuera de ventana (mínimo 0,30) y 231 con el respaldo `fill` E=2;
  - B2: 55 fuera de ventana y 365 por encima del tope de pares;
  - B3: 0 fuera de ventana y los 730 por encima del tope de pares.
- **code-reviewer:** APROBADO a la primera. Reprodujo la muestra byte a byte, comprobó el SHA y cruzó las 30 filas del informe con el pack (0 discrepancias). Las sugerencias menores van en R4.
- **[P] de R3:**
  - R-Q1b: si el tope no admite el K, el tablero lleva el **mínimo de pares posible** y queda marcado, sin cambiar K ni paleta;
  - R-Q11: si ningún candidato entra en la ventana, se guarda **el más cercano** y queda marcado;
  - cabecera con `empties_default` 1 y `max_risk_pairs` igual al máximo real, así que R4 comprueba el tope por banda contra la especificación;
  - `world` en cada tablero (opcional en `daily`);
  - un tablero por línea, como `PackWriter`;
  - `skippable` false (04 §2.6 e) y `hint_supported` true (06 §4.1; R-Q8).
- **Para el dueño (datos de R-Q11):**
  - B3 sale casi siempre ≥ 0,90 (mediana 0,95): entra en «≥ 75 %», pero son tableros muy difíciles;
  - B2 con K 8 y `fill` se pasa de la ventana en 55 de 730 días.

## 2026-10-19 · R2 · Especificación del pool y autoprueba: HECHA

- **Commits:** `da6a9bb` (especificación, lector, ids y autoprueba) y `99d35f7` («Enganche:» del despacho en `tools/level-generator/Program.cs`), en `spark/reto-del-rio-r2`; fusionados con `--no-ff` en `spark/reto-del-rio` (`3ca28dc`).
- **Archivos nuevos:** `tools/level-generator/specs/daily_v1.json`, `tools/level-generator/Daily/DailySpec.cs`, `DailyIds.cs`, `DailyCommand.cs`.
- **Verificación** (en el worktree `spark-r2`):
  - `dotnet run --project tools/level-generator -- --self-test daily` → 86 PASS, código 0;
  - `dotnet run --project tools/level-generator -c Release -- --self-test` (las autopruebas que ya había) → 314 PASS y todas «todo PASS», código 0, 49 s;
  - `dotnet build tools/level-generator -warnaserror` → 0 avisos; `dotnet build dotnet/AmazonFill.slnx -warnaserror` → 0 avisos, código 0;
  - `dotnet test dotnet/AmazonFill.slnx` → 981 (975 + 6 omitidos), código 0;
  - `python tools/validate_schema.py` → 103/103 PASS, sin cambios en `tools/out/schema_example_08_4_1.json`;
  - Unity no aplica (la tarea solo toca `tools/`).
- **code-reviewer:** APROBADO a la primera, sin bloqueantes. Comprobó las bandas de 03 §16, las fuentes de 03 §4.4, el agua del glosario §8, las regex del esquema (la autoprueba las lee del esquema) y los 2.190 ids y semillas. Sugerencias menores (estado de la paleta de B1 y un mensaje de error) van en R3.
- **Desviación del encargo (§3.3, punto 1):** el bloque del despacho va **antes del `--self-test` genérico** (tras `--validate-spec`), no justo antes de `--parity`: el genérico solo reconoce subcomandos fijos y daría error de uso con `--self-test daily`. Son 5 líneas, en su commit «Enganche:». El revisor lo da por correcto y necesario. Propongo al integrador corregir §3.3 punto 1 del encargo.
- **Errata:** el mensaje de `da6a9bb` dice «43 especificaciones mal formadas»; son 44. No lo reescribo.
- **[P] en `daily_v1.json`:** paleta de B2 y B3 (R-Q1b), regla del agua `1 + (día mod 9)` (R-Q2), `k_by_day` (día par → K menor, impar → mayor) y lo que haga R3 con los días fuera de ventana (R-Q11). Los tintes y los pares de riesgo se leen de `campaign_v2.json` (`tints_from`), sin copiarlos.

## 2026-10-19 · R1 · Nota de diseño: HECHA

- **Commits:** `3e0063e` (la nota) y `4968d1a` (junta dos comentarios duplicados), en `spark/reto-del-rio`.
- **Archivo:** `docs/dev/reto-del-rio.md`.
- **code-reviewer:** primera ronda CAMBIOS NECESARIOS (6 bloqueantes: el contrato del almacén no seguía a `ILevelStateStore`; la ventana del bot era a la vez aviso y compuerta; la reconciliación sin red se daba por regla; las opciones de R-Q1b estaban incompletas; faltaba dónde viven los tests que ven AF.LiveOps; cinco citas imprecisas). Arreglados todos; segunda ronda **APROBADO**.
- **Para el integrador:**
  - **Confirma** que `IServerDate` y `FakeServerDate` van en **AF.Platform** (08 §2.2 le da el «`Date` del servidor para el reloj») y no en AF.LiveOps, como decía el encargo §4. Nota §1.2.
  - Las decisiones R-Q1-R-Q10 llevan fecha 2026-10-20 en el encargo; el reloj de este equipo dice 2026-10-19. Confírmalo al llevarlas a `decisiones.md`.
  - Ediciones del GDD que piden R-Q4-R-Q8 (08 §4.3, 08 §9 tipo de `daily_overrides`, 06 §4.1): nota §1.3 y enganche E11.

### Preguntas nuevas para el dueño (nota §1.6)

| Id | Pregunta | Opciones (recomendada primero) | Qué bloquea |
|---|---|---|---|
| **R-Q1b** | R-Q1 no se puede cumplir: B2 con K = 9 tiene 3 pares de riesgo (tope 1) y B3, con los tintes de 1-200, al menos 3 con K = 10 y 4 con K = 11 (tope 2). Cálculo en la nota §1.1 | (a) B2 solo con K = 8 y B3 con `max_pares_riesgo` libre, como la campaña desde el 201; (b) subir los topes: B2 a 3 y B3 a 4; (c) mantener los topes y bajar K (B2 = 8, B3 = 9); (d) ampliar la paleta de B3 a 1-300 (no basta) | Paleta de R3 (va como parámetro [P]) y la publicación del pool (R5) |
| **R-Q11** | Si en 30 candidatos ningún tablero entra en la ventana del bot de su banda (03 §4.4 midió 0,88-1,00 de fallo con 1 vacío; B2 pide 60-80 %) | (a) la ventana es compuerta: el día no se publica hasta ajustar la especificación; (b) se publica el más cercano y QA avisa; (c) ensanchar la ventana (cambia 03 §16). Mientras tanto, (a) | R5 |
| **R-Q12** | Al reconectar, qué hace la reconciliación con las victorias cobradas sin red (08 §10.1) | (a) nada: no se retiran semillas; (b) anular las de fecha posterior a la del servidor (sello y racha) | Solo esa rama de R7b (irá detrás de una opción) |
| **R-Q13** | ¿Intersticial al terminar un Reto? (05 §6.1 solo habla de la campaña) | (a) no; (b) como en la campaña | R8/R9 (sin decisión: no hay) |
| **R-Q14** | La Pororoca de la racha puede llegar a B1 antes del nivel 30, que la desbloquea (04 §2.4) | (a) entra en el stock y espera al 30; (b) otro premio en B1; (c) nada antes del 30 | Solo un aviso en la UI de R9 |

## 2026-10-19 · R0 · Arranque y línea base: HECHA (desbloqueada)

El dueño pidió «instala lo necesario para trabajar». Instalado:
- `jsonschema` 4.26.0 en Python 3.14 (`py -3.14 -m pip install jsonschema`);
- .NET SDK 10.0.401 ARM64 (`winget install Microsoft.DotNet.SDK.10 --version 10.0.401 --architecture arm64`; instalador oficial de download.microsoft.com con el hash verificado);
- Unity 6000.3.25f1 ARM64 (changeset `e1dba0a9aba4`) con Android Build Support, OpenJDK, SDK y NDK, con Unity Hub 3.22.3 en modo headless (`-- --headless install --version 6000.3.25f1 --changeset e1dba0a9aba4 --architecture arm64 --module android --childModules`). La Unity CLI no se usó para instalar (`CLAUDE.md`). La licencia ya estaba activa en el equipo.

**Línea base** (`spark/reto-del-rio` en `2db3a07`, igual que `origin/integracion-fase1-fase2` `80b26b4` más el archivo de estado):

| Comando | Resultado | Tiempo |
|---|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | código 0, 0 avisos | 18 s |
| `dotnet test dotnet/AmazonFill.slnx` | 981 (975 + 6 omitidos), código 0 | 3 min 9 s |
| `python tools/validate_schema.py` | 103/103 PASS, código 0, sin cambios | — |
| `pwsh -NoProfile -File scripts/unity-compile.ps1 -Architecture arm64` | OK, código 0 (primera importación incluida) | 8 min |
| `pwsh -NoProfile -File scripts/unity-test.ps1 -Mode EditMode -Architecture arm64` | 2.744 (2.741 + 3 omitidos), 0 fallos, código 0 | 6 min |
| `pwsh -NoProfile -File scripts/unity-test.ps1 -Mode PlayMode -Architecture arm64 -TimeoutSec 3600` | 1.334 (1.175 + 159 omitidos), 0 fallos, código 0 | 22 min |

- Coinciden con los recuentos de `CLAUDE.md`.
- **Aviso:** con el límite por defecto (900 s), PlayMode sale con el código 6 (sin veredicto) a los 15 min, en mitad de `MapMarkersTests`: en este equipo tarda 21 min. Uso `-TimeoutSec 3600`, el valor que el propio script recomienda para una primera importación.
- Máquina: ARM64, 64 GB de RAM. Con 32 GB o más el encargo permite 3 Editores a la vez; me quedo en **2** salvo que haga falta.

---

## 2026-10-19 · R0 · Primer intento: BLOQUEADA por el entorno (resuelto en la entrada de arriba)

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
