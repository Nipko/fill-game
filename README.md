# Amazon Fill (nombre comercial: Aquazonia)

**Amazon Fill** es el nombre interno (carpeta, repositorio, código, prefijo de eventos `af_`) de **Aquazonia**, un juego móvil de ordenar agua (*water-sort*) con temática de la cuenca amazónica real: 14 tintes que son frutas y aguas con nombre, cuias de vidrio como recipientes, 13 mundos por país y río, y dos mascotas, Capi (capibara) y Rosa (boto). Copia el núcleo y la economía probados del género, corrige las quejas estructurales del líder (rescate caro sin ruta gratuita, tablero que cambia al reintentar, colores ocultos como azar, anuncios tras cada nivel) y nace Android-first para América Latina con precios por país.

**Quién lo hace:** todo el código lo escriben agentes de IA (Claude Code con subagentes, modelo Opus; ver `CLAUDE.md` y `.claude/agents/`). Las personas son el dueño del producto (producto, cuentas, licencias, firmas y pagos), un artista 2D freelance, un asesor cultural y un abogado de marcas (`docs/gdd/09-roadmap-equipo-kpis-riesgos.md` §0).

## Estructura de carpetas

La raíz del repositorio es también la raíz del proyecto Unity. Unity solo importa `Assets/` y `Packages/`; el resto de carpetas no le afecta. Las marcadas con *(tras la licencia)* aparecen cuando el dueño active la licencia Personal de Unity y los agentes creen el proyecto.

```
amazon-fill/
  README.md                 este archivo
  CLAUDE.md                 instrucciones para los agentes (fuentes de verdad, stack, comandos, reglas duras)
  global.json               fija el .NET SDK 10.0.401
  .gitignore  .gitattributes  .editorconfig     # Git LFS para binarios; nunca para código, YAML, .meta ni JSON
  .claude/                  settings.proposed.json (permisos propuestos; pasa a settings.json al aprobarse en s12),
                            agents/ (7 subagentes), rules/ (reglas por ruta)
  .github/workflows/        (más adelante, s30-s32) ci.yml (job core: dotnet test + esquema) y unity.yml (tests de Unity y AAB)
  Assets/                   (tras la licencia) _Project/{Art,Audio,Shaders,Prefabs,Scenes,Scripts,Data,Editor/CI,Tests}
  ProjectSettings/          (tras la licencia) ProjectVersion.txt = Unity 6000.3.25f1 (e1dba0a9aba4)
  Packages/
    manifest.json           (tras la licencia) paquetes de Unity con versión fijada
    com.aquazonia.core/     ÚNICA copia del núcleo: Runtime/AF.Core (sin UnityEngine), Runtime/AF.Solver,
                            Tests/Editor (tests compartidos con dotnet, subconjunto NUnit 3.5, C# 9)
  dotnet/                   Aquazonia.slnx; AF.Core y AF.Solver (netstandard2.1, enlazan los .cs del paquete),
                            AF.Core.Tests (net10.0, NUnit 3.14.0); Directory.Build.props, BannedSymbols.txt;
                            artifacts/ (bin/obj, ignorada)
  tools/
    level-generator/        CLI .NET 10 del generador de niveles (spec.json -> mundo_{nn}_v{v}.json + report.csv)
    level-qa/               CLI .NET 10 de QA de paquetes de niveles (compuerta de CI)
    levelgen_proto.py       prototipo del generador/solver de niveles (sin dependencias externas)
    econ_sim.py             simulación de la economía de semillas (escenarios A, B y tramos del rescate)
    econ_variants.py        sensibilidad de 8 variantes: precio del rescate frente a fuentes gratuitas
    validate_schema.py      valida schemas/af-level-1.schema.json y el ejemplo de 08 §4.1 (23 casos)
    out/  out_inverse/      salidas del prototipo y de la simulación (cifras [sim]) y registros de la revisión
  scripts/                  unity-test.ps1, unity-compile.ps1: envoltorios de Unity que usan los agentes
  ci/                       size-gate.ps1 (AAB <= 80 MB), check-16kb.sh (alineación de 16 KB)
  schemas/
    af-level-1.schema.json  JSON Schema canónico y única copia del formato de nivel af-level/1 (08 §4.1)
  docs/
    GDD.md                  documento maestro: resumen, decisiones clave, índice, hoja de ruta,
                            preguntas del dueño (decididas el 2026-10-06), incidencias y próximos pasos
    gdd/                    glosario + 9 capítulos de diseño (00 … 09)
    research/               9 informes de investigación de diseño + magic-sort/ (frentes a, b, c)
    setup/                  plan-instalacion.md (plan aprobado), equipo-de-agentes.md (cómo trabajar con los
                            subagentes) y research/ (informes a-f de la puesta en marcha)
    legal/                  marca.md: seguimiento de la marca, dominios, abogados, plazos y applicationId
  functions/                (más adelante) Cloud Functions en TypeScript
```

## Cómo empezar

Requisitos ya instalados en la máquina de desarrollo (Windows 11 ARM64): .NET SDK 10.0.401, Python 3.14 con `jsonschema` ≥ 4.18, Git LFS (global), `gh`, Unity Hub 3.22.2 y Unity CLI 1.0.0-beta.12. El detalle está en `docs/setup/plan-instalacion.md`.

**Núcleo en C# (no necesita Unity).** Desde la raíz del repositorio:

```
dotnet build dotnet/Aquazonia.slnx -warnaserror
dotnet test dotnet/Aquazonia.slnx
python tools/validate_schema.py                     # 23/23 PASS; código 1 si falla
```

Si `dotnet` no está en el PATH de una terminal nueva, en PowerShell: `& "C:\Program Files\dotnet\dotnet.exe" test dotnet/Aquazonia.slnx` (o antepón `C:\Program Files\dotnet` al PATH).

**Proyecto Unity.** Todavía no existe. Se crea con Unity 6000.3.25f1 (editor ARM64) **después de que el dueño inicie sesión y active la licencia Personal** (`unity auth login`, `unity license activate --personal --accept-eula`; pasos s15-s16 del plan). Después los agentes crean el proyecto URP 2D, pasan la compuerta del día 1 (tres builds Android IL2CPP en verde y prueba en el teléfono; `docs/gdd/08-arquitectura-tecnica-publicacion.md` §16) y verifican con:

```
unity test . --mode EditMode --output Logs/editmode.xml
unity test . --mode PlayMode --output Logs/playmode.xml     # antes de cada PR
```

**Herramientas de diseño en Python** (Python 3.14; solo `validate_schema.py` necesita `pip install "jsonschema>=4.18"`; las salidas `econ_*.md` se reproducen byte a byte con la semilla 2026):

```
python tools/levelgen_proto.py --levels 20 --bot-runs 40 --quiet                     # lote híbrido -> tools/out/
python tools/levelgen_proto.py --levels 20 --bot-runs 40 --size-run 100 --murky --quiet --out tools/out_inverse
python tools/econ_sim.py --runs 300 > tools/out/econ_B.md                                       # escenario B (canon)
python tools/econ_sim.py --runs 300 --scenario A --no-revenue > tools/out/econ_A.md             # escenario A
python tools/econ_sim.py --runs 300 --rescue 600,750,900 --no-revenue > tools/out/econ_B_tramos.md
python tools/econ_variants.py > tools/out/econ_variants.md                                      # 8 variantes (05 §3.6)
```

Reglas que conviene conocer antes de tocar nada: no se editan a mano `.unity`, `.prefab`, `.meta` ni `ProjectSettings/`; no se añaden SDK ni permisos sin el dueño; ningún secreto entra en el repositorio; y **ningún AAB se sube a Google Play hasta tener el dictamen de marca** (`docs/legal/marca.md`).

## Cómo leer la documentación en 15 minutos

1. **5 min**: `docs/GDD.md`, secciones 1 (resumen ejecutivo) y 2 (tabla de decisiones clave). Con eso se conoce qué es el juego, por qué debería funcionar y qué está decidido.
2. **4 min**: `docs/gdd/00-glosario.md`, resumen y secciones 5-6: nombres de comodines, rescate, dificultad y moneda. Todo el GDD usa esos nombres.
3. **3 min**: `docs/GDD.md`, secciones 5, 6 y 9: hoja de ruta con el equipo de agentes, las 14 preguntas del dueño (decididas el 2026-10-06, salvo el dictamen de marca y el contador) y los próximos pasos.
4. **3 min**: el capítulo de tu área. Programación: `02` (reglas) y `08` (arquitectura, máquina ARM64, CI y la tabla única de Remote Config). Diseño de niveles: `03`. Economía: `04` y `05`. Retención: `06`. Arte y audio: `07`. Producción: `09`. Puesta en marcha: `docs/setup/plan-instalacion.md`.

La investigación (`docs/research/` y `docs/setup/research/`) es material de apoyo: cada afirmación de los capítulos cita el informe y la sección de origen con la marca [V] verificado, [E] estimación o [P] propuesta.

## Estado

GDD 1.3 — 2026-10-06: **aprobado por el dueño para producción** (recomendaciones aceptadas el 2026-10-06; coherencia verificada: 28/28 contratos). Versiones anteriores: 1.2, 1.1 (2026-10-06) y 1.0 (2026-10-05).

- Puesta en marcha en curso: .NET 10, Unity Hub, Unity CLI, Git LFS y `adb` instalados y verificados; el Editor 6000.3.25f1 ARM64 con Android Build Support se está instalando. Pendiente del dueño: iniciar sesión en Unity y activar la licencia Personal; después se crea el proyecto Unity.
- El andamiaje del núcleo en .NET (`Packages/com.aquazonia.core`, `dotnet/`, `tools/level-generator`, `tools/level-qa`) y la configuración de los agentes están escritos: `dotnet build -warnaserror` sin avisos y 39/39 tests en verde; todavía no hay commits ni repositorio remoto.
- Marca: Aquazonia condicionada al dictamen de un abogado de marcas en Brasil (solicitud INPI 938306200); dominios `aquazonia.app` y `aquazonia.co`.
- Siguientes pasos: `docs/GDD.md` §9.
