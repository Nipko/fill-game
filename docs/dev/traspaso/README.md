# Traspaso al equipo de escritorio (2026-10-09)

Escrito por la sesión de Claude Code del portátil (Windows 11 ARM64) para la sesión que continúa en el equipo de escritorio. Léelo entero junto con `CLAUDE.md` antes de tocar nada.

## 1. Resumen

- **La rama para seguir es `integracion-fase1-fase2`**: junta todo lo hecho hasta hoy, compila y pasa todas las pruebas (sección 3). `main` sigue en `e8edd1f` (D-005 y D-006) y no se ha tocado.
- **Ya está hecho:**
  - el núcleo con las 9 mecánicas de la 1.0, el solver y el bot;
  - el generador con la dificultad escalonada y los packs de los niveles 11-200;
  - el bucle de partida de Magic Sort, con anuncios y compras simulados;
  - la intro «Llenar y teñir» con miquitos.
- **Falta, sobre todo, la integración en Unity:**
  - que el juego cargue los niveles 11-200;
  - que cada mecánica tenga su presentación;
  - las etiquetas de dificultad antes de jugar.

  Después hay que registrar D-007 y D-009 en el GDD, hacer el elenco de ~50 especies y el APK para el teléfono (sección 6).
- El dueño trabaja con Claude en modo autónomo: todos los permisos están en «allow» (solo quedan las «deny»). Sus preferencias de trabajo están en la sección 8.

### Reparto entre los dos equipos (decisión del dueño, 2026-10-09)

- **Escritorio (remoto, siempre encendido):** hace el trabajo de los pasos de la sección 6 sobre `integracion-fase1-fase2`. Al terminar cada paso verificado (sección 3), hace **commit y push de esa rama** a `origin`. No hace falta su teléfono ni su build de Android: el APK no está en git.
- **Portátil (con el teléfono):** el dueño actualiza (`git pull` en `integracion-fase1-fase2`), compila el APK de desarrollo (`AF.EditorTools.BuildDev.BuildAndroidDev`, con `--architecture arm64`) y lo instala por cable en el Samsung (`adb install -r Builds/Android/dev.apk`).
- Por eso, en cada push explica en el mensaje del commit qué probar en el teléfono (niveles, pantallas y extras de `adb` útiles).
- No trabajes los dos equipos a la vez sobre la misma rama sin hacer pull antes.

## 2. Ramas en GitHub (`origin` = github.com/Nipko/fill-game)

| Rama | Contenido | Estado |
|---|---|---|
| `main` | Hasta D-005/D-006 (vertido por tramo; tablero, cuia y vertido copiados de Magic Sort) | Estable |
| `fase2-bucle-partida` | Pista B: AF.Platform/AF.Save (guardado atómico), AF.Economy (semillas, canoas, comodines), AF.Ads/AF.IAP simulados, catálogo de niveles y progreso, pantalla de inicio, bandeja de comodines, embudo de D-008, victoria con Cosecha, reanudar el nivel. Además, las fichas de Magic Sort y la investigación cultural | Revisada (2 rondas) |
| `fase1-mecanicas-niveles` | Pista A: BoardRules con R10 por etapas y fútiles del anexo B; Hoja, Matapi, Liana (fruta y número), Aguas turbias, Remolino, Barro, Lluvia, Brote y Piraña en núcleo y solver; PRNG, bot ×40, barajado inverso; esquema `pack_schema` 2; generador `--spec` con el ciclo de D-009; `level-qa`; simulación económica escalonada | Revisada (2 rondas) |
| `intro-rio-miquitos` | Sobre la pista B: intro «Llenar y teñir» (el río llena las cuias y una tropa de miquitos deja caer fruta que las tiñe), 10 especies como datos, tarjeta «¡Miquito a la vista!», D-010 registrada; permisos en «allow» | Revisada (2 rondas) |
| **`integracion-fase1-fase2`** | Las tres anteriores fusionadas, más los packs `mundo_02..05_v1.json` (niveles 11-200) con sus `.meta`, este traspaso y los planes | **Verificada; seguir aquí** |

## 3. Verificación en el momento del traspaso (rama `integracion-fase1-fase2`, portátil ARM64)

| Comando | Resultado |
|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 avisos, 0 errores |
| `dotnet test dotnet/AmazonFill.slnx` | 707 superados, 4 omitidos |
| `python tools/validate_schema.py` | 40/40 PASS |
| `dotnet run --project tools/level-generator -- --handmade tools/level-generator/specs/tutorial_v0.json --out Assets/_Project/Data/Levels --report tools/out/tutorial_v0_report.md --check` | 141/141 PASS; pack idéntico |
| `dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/campaign_v1.json --levels 11-200 --out Assets/_Project/Data/Levels --report tools/out/campaign_v1_report.md --check` | Los 4 packs coinciden con el generado (≈ 340 s) |
| `pwsh -NoProfile -File scripts/unity-compile.ps1` | Compila |
| `pwsh -NoProfile -File scripts/unity-test.ps1 -Mode EditMode` | 1.193 tests: 1.191 pasan, 2 omitidos |
| `pwsh -NoProfile -File scripts/unity-test.ps1 -Mode PlayMode -TimeoutSec 1800` | 114 tests: 109 pasan, 5 explícitos (capturas) |

## 4. Preparar el equipo de escritorio

1. **Herramientas** (versiones fijadas en `CLAUDE.md`; nada nuevo sin el dueño):
   - Unity Hub y **Unity 6000.3.25f1** (changeset `e1dba0a9aba4`) con **Android Build Support** (OpenJDK, Android SDK y NDK). El dueño inicia sesión en el Hub y activa la licencia: los agentes nunca hacen `unity auth` ni `unity license activate`.
   - **.NET SDK 10.0.401** (`global.json` con `rollForward: latestFeature`).
   - **Python 3.14** y `pip install jsonschema`.
   - **Git** y **Git LFS** (`git lfs install`). Las fuentes, las imágenes y otros binarios van por LFS.
   - **PowerShell 7.2 o superior** (los scripts `.ps1` lo exigen).
   - **Unity CLI** (`unity`, beta; sirve para `run`, `test` y `recompile`) y, si se usa, `gh`.
   - **Android platform-tools** (`adb`) para instalar en el Samsung SM-S938B. **Usa un solo `adb`**: si hay dos instalados, se pelean.
2. **Clonar:**
   ```
   git clone https://github.com/Nipko/fill-game.git amazon-fill
   cd amazon-fill
   git lfs pull
   git checkout integracion-fase1-fase2
   ```
   Usa una ruta sin espacios.
3. **Arquitectura.** El portátil es ARM64 y los comandos de `CLAUDE.md` llevan `--architecture arm64`. **En un equipo x64:**
   - pasa `-Architecture x86_64` a `scripts/unity-compile.ps1` y a `scripts/unity-test.ps1`;
   - usa `--architecture x86_64` en `unity run` y `unity test`;
   - en x64 las DLL de Firebase, MAX y EDM4U sí cargan en el Editor (en ARM64 no), lo cual no afecta a lo actual: no hay SDK.
4. **Primera apertura.** Abre el proyecto una vez (Editor o `scripts/unity-compile.ps1`) para que Unity importe todo. Es normal que tarde.
5. **Hooks de git.** Los hooks (`pre-commit`, `post-checkout`, `post-merge`, `pre-push`) viven en `.git/hooks` del portátil y **no se clonan**. En el portátil llaman a la Unity CLI por el alias `%LOCALAPPDATA%\Microsoft\WindowsApps\unity.exe`. Sin ellos, verifica a mano con la sección 3 antes de cada commit.
6. **Avisos prácticos:**
   - Al ejecutar las pruebas, Unity reserializa `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset` y los `Nunito-Regular SDF*` de `Assets/_Project/Resources/AF/Fonts/`. Restáuralos con `git checkout --` antes de cada commit.
   - El build de desarrollo deja `Assets/Resources/PerformanceTest*` y cambia `ProjectSettings.asset` (`preloadedAssets`) mientras dura; al terminar los deshace solo.
   - Nunca dos procesos de Unity sobre la misma carpeta, ni batchmode con un Editor abierto.
   - Si el equipo se suspende, Unity se congela en mitad de una prueba. El portátil lo hacía de noche: deja el de escritorio sin suspensión mientras trabajan los agentes.

## 5. Cómo arrancar la sesión nueva

Abre Claude Code en la carpeta clonada, en la rama `integracion-fase1-fase2`, y pídele algo como:

> Lee CLAUDE.md y docs/dev/traspaso/README.md. Prepara y verifica el entorno (sección 3 del traspaso, con la arquitectura de este equipo) y continúa con el trabajo pendiente en el orden de la sección 6.

La sesión del portátil usaba la herramienta Workflow con equipos de agentes Opus (`.claude/agents/`): tech-lead → dev → revisión de código y de fidelidad → verificación adversaria de cada hallazgo → corrección. El guion del elenco (paso 3) está listo para relanzarlo.

## 6. Trabajo pendiente, en orden

### Paso 1 · Integración en Unity (la más urgente: el dueño quiere probar ya)

> **Progreso en el escritorio (x64).** El trabajo va en 5 olas de tareas pequeñas; cada ola se fusiona en esta rama ya revisada y verificada.
>
> **Ola 1 integrada el 2026-10-09:**
> - los niveles 1-200 cargan y se juegan con todas las mecánicas de la 1.0 en las reglas;
> - cada mecánica tiene un aspecto simple provisional;
> - las dosis turbias y el Barro se pintan bien;
> - la vista sigue al modelo en cada vertido;
> - AF_STATE informa de las mecánicas.
>
> **Ola 2 integrada el 2026-10-09:**
> - pasos de desbloqueo en el orden de R10 (con el punto fijo f′) y estados intermedios;
> - presupuesto de 2,8 s desde el toque;
> - persistencia de lo revelado (historial h2);
> - destellos, esquirlas y burbujas;
> - etiquetas D-009 en Jugar y en los modales.
>
> **Ola 3 integrada el 2026-10-10:**
> - etiquetas D-009 en el HUD y en Siguiente, chip de Aguas turbias y contador de Lluvia y Piraña;
> - aspecto y desbloqueo al estilo Magic Sort de Hoja, Brote, Matapi, Liana con fruta, Remolino, Barro, Lluvia y Piraña;
> - Aguas turbias en el shader (pardo con trama y «?»), revelado por la regla y frente de luz de la Luciérnaga;
> - tarjeta previa (apagada hasta la ola 4) y lógica de la Luciérnaga (regalo del 100, semillas, vídeo).
>
> **Ola 4 integrada el 2026-10-10:**
> - tarjeta previa de 1 toque (Difícil y Muy difícil) y tutorial de una pantalla en cada estreno, con el 100 combinado; las tarjetas quedan encendidas;
> - la Luciérnaga en la bandeja: ranura Revelar, selección de la cuia turbia, compra con stock, semillas o vídeo simulado, vuelo y aviso del regalo;
> - el Barro mate (arreglo de T16) y el banco de tests de mecánicas, que ahora se destruye en el acto.
>
> **Limpieza y verificación final (T18) el 2026-10-11:**
> - limpieza de los hallazgos menores de las revisiones (L1, L2a, L2b y L3): shader más barato en las bandas normales, Luciérnaga precalentada, tarjetas con 3 superficies compartidas, ganchos de GameScreen, avisos de la bandeja a 12 sp y tests más estrictos;
> - T18: los 14 pasos en verde (packs idénticos con `--check`, EditMode 1.390, PlayMode 322, 434 capturas, AAB de 36,97 MB, 16 KB correcto en AAB y APK, minSdk 26, targetSdk 36 y `dev.apk` de 66,23 MB).
>
> **El paso 1 queda cerrado** a falta de la prueba en el teléfono (portátil). Lo siguiente es el paso 2, y quedan los pendientes del dueño de abajo.
> - La caché de `LocalDateKey` (AF.Platform) la aprobó el dueño el 2026-10-11 y está aplicada (`.claude/rules/sdk.md`).
> - Los [P] que tiene que ratificar el dueño están en el informe de la sesión del 2026-10-11.
>
> Qué probar en el teléfono tras cada push: en el mensaje del commit.

Las tareas detalladas están en `docs/dev/traspaso/planes-integracion.json` (`integration_tasks` e `integration_notes_pista_A`). En resumen:

1. **Lector de `pack_schema` 2.** `LevelPackReader` (en `Assets/_Project/Scripts/Levels/`) solo acepta el 1. Tiene que leer:
   - los `attrs` de cada mecánica: `hoja`, `matapi`, `liana` con `key` o `n`, `remolino` con `partner`, `lluvia`, `brote` y `pirana`;
   - las dosis ocultas y el barro (color 15);
   - `caps`, `world`, `water_base`, `intro_mechanic` y las etiquetas.
2. **Catálogo con los packs nuevos.** `LevelCatalog` ya acepta varios packs: hay que registrar `mundo_02..05_v1.json` para jugar del 1 al 200, cada nivel con su mundo, su agua base y su especie de miquito.
3. **Presentación de cada mecánica**, con arte procedural en la línea de D-003 y la animación de desbloqueo como en Magic Sort (ficha `docs/research/magic-sort/ficha-magic-sort.md` §3: la marca de visto, el haz de luz, la tela que sube, el hielo que revienta, la llave que vuela al candado):
   - Hoja, Matapi, Liana con fruta y Liana con número;
   - Aguas turbias, con el glifo «?» que aún falta;
   - Remolino, Barro, Lluvia (con su contador en el HUD), Brote y Piraña (con su cueva).
4. **Tutorial de una pantalla** en el debut de cada mecánica (02 §9.2, regla 2) y **Luciérnaga regalada** en el estreno de Aguas turbias (nivel 100).
5. **Etiquetas de D-009 visibles antes de jugar:**
   - en los botones «Jugar» y «Siguiente», un chip «Difícil»/«Muy difícil» con su cinta ×3/×4;
   - en la tarjeta previa, los iconos de las mecánicas del nivel;
   - también en el HUD.
6. **Verificación completa** (sección 3), **commit y push**. El build de desarrollo y la prueba en el teléfono los hace el portátil (sección 1). En el portátil, por cable: `adb devices` lo muestra como `R5CY92CG81P`. Instala con `adb install -r Builds/Android/dev.apk` y recorre los niveles 1, 11, 41, 81, 101, 141 y 183 grabando con `adb shell screenrecord`.
   - **No uses** el extra `af_level_offset` para probar mundos: desplaza el número de nivel pero deja el tablero y el mundo del tutorial. Con los packs reales basta con `--ei af_level N`.

### Paso 1b · Arreglos de la prueba del dueño en el teléfono (2026-10-11): hazlos antes del paso 2

> *Estado del 2026-10-12: P1, P3 y P4 están hechos en la tanda 1 (integrada en `7d755c4`, con T-A, T-B, T-C, T-D y los criterios de la ola 3 de la hoja del 2026-10-09). P2 (avisos legibles), la tarjeta del miquito tras la tropa y el resto de la hoja (T-E, T-F, T-G, el banner de mundo y el tutorial de la Piraña) van en la tanda 2, en curso. Falta probarlo todo en el teléfono.*

El dueño probó el `dev.apk` (compilado sobre `80d37fb` o antes) y decidió **D-011** (`docs/dev/decisiones.md`). Las cuatro causas seguían en `7677085`. Tareas, causas con archivo y línea, pruebas y qué probar en el teléfono: `docs/dev/traspaso/pruebas-telefono-2026-10-11.md`.

- **P1:** el saldo no enseña la Cosecha hasta la pantalla de victoria, y la bandeja de comodines no se apaga antes de la celebración. La Cosecha se sigue cobrando en el instante lógico.
- **P2:** avisos y tarjeta «¡Miquito a la vista!» con duración por lectura (3-7 s). Ninguno sale durante la carga, la tarjeta previa o la intro (el regalo de Capi del nivel 8 hoy sale encima de la intro), y salen de uno en uno.
- **P3:** miquitos que entran trepando, sueltan igual de rápido, se quedan ≈ 1,5 s en reposo mientras ya se juega y se van en ≈ 0,5 s. T y el toque, sin cambios.
- **P4:** tras el vídeo de la Cosecha doble, el juego cuenta el doble y pasa solo al nivel siguiente.

### Paso 2 · Registrar D-007 y D-009 en el GDD

> *Hecho el 2026-10-11 y el 2026-10-12 (rama `paso2/registrar-d007-d009`, solo documentación): D-007, D-009 y D-012 registradas en `docs/dev/decisiones.md`, con la ampliación de D-010 y el estado de D-011, y llevadas al GDD (§7 de este archivo), con la lista única de estrenos en 02 §9.1, salvo las cifras [sim] regeneradas con D-009, que esperan al dueño (V5). Lo que depende de la tanda 2 (D-011, punto 2; de D-012, el banner de mundo, el tutorial de la Piraña, la sacudida y los patrones) ya está en el GDD como canon, sin darlo por implementado: al integrarla, basta con poner al día su estado en `docs/dev/decisiones.md`. Los pendientes del dueño están en `docs/GDD.md` §6.3 (preguntas 18-38; la 20, decidida). El texto de abajo queda como historial.*

Ninguna de las dos tiene aún su entrada en `docs/dev/decisiones.md`. Están decididas por el dueño:

- **D-007, mecánicas (2026-10-08).** El dueño aceptó todas las recomendaciones de `docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md` §(h):
  - **Catálogo:** 12 mecánicas en total.
  - **1.0 (niveles 1-200):**
    - Cuia chica (14) y Cuia alta (27);
    - Hoja 43, Matapi 63 y Liana con fruta 83;
    - Aguas turbias 100, con estreno suave: sola, 1 cuia, 2 dosis ocultas y 1 Luciérnaga regalada;
    - Remolino 103, Barro 123, Lluvia 143, Brote 163 y Piraña 183;
    - la Liana con número pasa a variante en el 243.
  - **1.1:** Aguapé 223, Palizada 263, Lancha 303 y Cocha 583.
  - **Respuestas del dueño:** Q13 sí (arreglar fútiles y R10; hecho en el núcleo), Q14 A, Q17 A (el contenido se ve), Q18 B (los fútiles no avanzan contadores), Q10 sí y Q12 no.
  - **Reglas finas (2026-10-08):**
    - Q-A: el Remolino no vierte en su propia pareja;
    - Q-B: la exención de futil también vale para el origen si es un Remolino o una Lluvia con gotas;
    - Q-C: si la Piraña no puede morder, pierde ese mordisco.
- **D-009, dificultad escalonada:**
  - ciclo de 20 con retos en p6, p9, p12, p15 y p18, remanso en p19 e hito en p20;
  - etiqueta visible antes de jugar y premios de 20, 60 y 80 semillas;
  - subtítulo del Difícil: «Aguas bravas»;
  - calendario de rotación (d.4) y la regla d.5.
- Las ediciones del GDD que exigen están listadas en la propuesta §(g) y en los informes de la pista A: `tools/out/campaign_v1_report.md` y `Packages/com.amazonfill.core/README.md`.

### Paso 3 · Elenco de ~50 especies que tiñen (ampliación de D-010, 2026-10-09)

El dueño decidió:
- **Ritmo:** **una especie nueva cada 20 niveles** (≈ 50 en 1-1000), coordinada con los estrenos de mecánicas para que no coincidan dos novedades en el mismo nivel.
- **Quién tiñe:** **monos, aves y otros animales que de verdad dejan caer fruta** (tucanes, guacamayos, oropéndolas, murciélagos fruteros, kinkajú…).
- **Cómo:** cada tipo con su forma de soltar la fruta (desde la rama, en vuelo, colgado, de noche), siempre en el mundo donde vive la especie.

El guion del equipo está en `docs/dev/traspaso/workflow-elenco-50-especies.js`: investigación → diseño (`docs/research/fauna/elenco-tinteros.md`) → verificación biológica y técnica → implementación → revisión → docs. Se detuvo antes de producir nada; hay que relanzarlo con la herramienta Workflow (`args: {"root": "<ruta del repo>"}`), **después** de la integración, porque usa Unity.

Material de partida: `docs/research/fauna/propuesta-fauna.md` (10 especies y 18 animales «que hacen cosas») y `docs/research/intro-rio-miquitos/guion.md`.

### Paso 4 · Pendientes del dueño y del equipo

- **Preguntas abiertas del guion de la intro** (`docs/research/intro-rio-miquitos/guion.md`, Q1-Q16, y Q17 del agente de docs). El código aplica las recomendaciones, salvo Q11 (evento `af_fauna_sighted`, que necesita migrar el perfil) y Q14 (color del agua negra).
- **Preguntas de la propuesta de fauna** (`docs/research/fauna/propuesta-fauna.md`) y de la propuesta de Magic Sort (`docs/research/magic-sort/propuesta-tintalu.md` §10.2, sobre todo metajuego, anuncios y tienda).
- **Bloqueo técnico C1-9 de la pista A:** la propiedad P1 (R14) falla con la Liana y capacidades mixtas, así que `LevelValidation` rechaza la Liana con capacidades distintas de 4, cueva de la Piraña incluida. Por eso el calendario de la campaña movió o quitó la Liana, y no las capacidades, en cuatro celdas [P]: 94 Ma → Li y 95 Li+Al → Ma+Al (la Cuia alta vuelve en el reto 95 con el Matapi), 160 Ll+Li+Al6 → Ll+Al6 (la Cuia alta de 6 se sigue estrenando en el hito 160) y 198 Pi+Li → Pi+Ho (`calendar_note` de `tools/level-generator/specs/campaign_v1.json`). Está en `Packages/com.amazonfill.core/README.md`, en su CHANGELOG y en `tools/out/campaign_v1_report.md`; hace falta decidir la regla (02 R16; pregunta 24 de `docs/GDD.md` §6.3).
- **Preguntas nuevas del paso 2:** las 27-38 de `docs/GDD.md` §6.3 (Lluvia y Piraña en la 1.0 al cierre de la Fase 1, «Zafra», Revelar en la 3.ª ranura, formato [P] de `pack_schema` 2, respaldos del kill-switch, fuente de la temporada, novedades de 201-1000, P1-P3 con el Remolino de Q-B, el revelado rítmico, el texto por debajo de 12 sp, el vertido que gana frente a «ninguno > 8 ms» y los [P] de la tanda 1). La 20 (caché de `LocalDateKey`) ya está decidida y aplicada (`7677085`).
- **Pendientes de la pista B:**
  - el guardado por vertido ya va con un escritor en segundo plano (T-D, tanda 1; D-012, decisión 5): falta medirlo en el teléfono (p95 < 5 ms y ninguno > 8 ms; pregunta 37 de `docs/GDD.md` §6.3);
  - el reloj anti-trampas necesita hora de servidor: hoy, adelantar el reloj rellena canoas;
  - algunos [P] de nombres de perfil.
- **Arte:** todo es procedural y provisional. Los prompts para ChatGPT o para el artista están en `docs/art/prompts-chatgpt.md`, y la lista de piezas de los miquitos en el guion de la intro (§11) y en la propuesta de fauna.
- **Asesor cultural:** nombres de fauna, flora y lugares, los textos de «Dato del río» y las especies. Las listas están en las propuestas.
- **Siguen pendientes del dueño:** el permiso `INTERNET` (`CLAUDE.md`), el dictamen de marca de «Tintalú» y la prueba visual de 30 min (s28).

## 7. Decisiones del dueño vigentes (registro en `docs/dev/decisiones.md`)

| Decisión | Resumen |
|---|---|
| D-001 | Una dosis por vertido. **Sustituida por D-005** |
| D-002 | Al seleccionar no se resalta ningún destino. Las ayudas son explícitas |
| D-003 | Dirección visual «Selva brillante premium»: look de juego top, nunca libro, papel ni acuarela |
| D-004 | Panel azul petróleo y tope de ancho de la cuia por T. La pose y el techo los sustituye D-006 |
| D-005 | Vertido por tramo, como Magic Sort (`rules_pour_mode` = `run` por defecto) |
| D-006 | Tablero, cuia y vertido copiados de Magic Sort: pose según las dosis que quedan, boca casi en el destino, ciclo de ≈ 0,9 s, cavidad ≥ 0,80, 3 filas desde 11 cuias |
| D-007 | Mecánicas del río: catálogo de 12, estrenos de la 1.0 y reglas finas Q-A, Q-B y Q-C. Registrada el 2026-10-11 y completada el 2026-10-12 (paso 2), con la lista única de estrenos en 02 §9.1 |
| D-008 | Bucle de partida como Magic Sort: estructura de Magic Sort, cifras de nuestro GDD. Las vidas ilimitadas no quitan los anuncios |
| D-009 | Dificultad escalonada: ciclo de 20 reordenado, etiqueta antes de jugar y rotación de plantillas. Registrada el 2026-10-11 (paso 2); sus cifras [sim] esperan la aprobación del dueño |
| D-010 | Intro «Llenar y teñir» con miquitos como datos. La ampliación a ~50 especies quedó registrada el 2026-10-11; su diseño es el paso 3 |
| D-011 | Prueba en el teléfono (2026-10-11): la victoria se cobra en la vista cuando se ve, avisos legibles, miquitos con más presencia sin frenar el juego y Cosecha doble que avanza sola. Puntos 1, 3 y 4 implementados en la tanda 1 (`7d755c4`); punto 2, en curso (tanda 2). Llevada al GDD el 2026-10-12 |
| D-012 | Decisiones del 2026-10-09 y respuestas del 2026-10-11: estreno de la Piraña con mordisco fallido visible y tutorial animado, banner de mundo (Capi presenta y Rosa asoma; 11, 41, 81 y 141, primer intento, sin banner en el 1), sacudida de ±8 dp, 3 ciclos y 240 ms, patrones e iconos como el GDD, guardado por vertido con escritor en segundo plano (p95 < 5 ms y ninguno > 8 ms), marco del panel sin cambios y Remolino con canal y la misma marca en las dos cuias. Registrada el 2026-10-12 (paso 2). Tanda 1: mordisco fallido, Remolino y guardado; tanda 2, en curso: el resto |

## 8. Cómo trabaja el dueño (preferencias aprendidas)

- **Idioma y estilo:** español; pragmático; quiere avanzar rápido y ver resultados en el teléfono.
- **Copiar lo que funciona:** «no vamos a inventar la rueda». Para medidas, reglas, flujos y monetización se copia Magic Sort (medido en su propia sesión grabada) y se le da el toque amazónico. Nada de bucles de críticos para decidir lo que la referencia ya resolvió: la revisión comprueba que se copió bien.
- **No suavizar lo que funciona** en la referencia salvo por política de Google Play. Si algo parece mejorable, plantéalo en una frase como pregunta. Ejemplo: rechazó quitar anuncios durante las vidas ilimitadas («como lo tienen está bien»).
- **Autenticidad cultural:** las mecánicas y la fauna se diseñan con investigación real de la vida ribereña y de los países del Amazonas, con fuentes y respeto (nada sagrado, nada de mascotas ni estereotipos). Todo queda «pendiente del asesor cultural».
- **Dificultad:** dientes de sierra con etiquetas antes de jugar, alivio tras cada pico y plantillas que rotan y vuelven, como en Magic Sort.
- **Autonomía:** todos los permisos están en «allow». Aun así valen las reglas duras de `CLAUDE.md`:
  - no tocar `main` sin que lo pida;
  - nada de force push;
  - nada de SDK, paquetes ni versiones nuevas sin su visto bueno;
  - nada de secretos;
  - nada de publicar en Google Play.

  Pregunta solo lo que de verdad decide él, con opciones cerradas y una recomendación.
- **Colaboración con ChatGPT:** puede pedir revisiones o imágenes a ChatGPT. Trátalas como propuestas que hay que verificar, no como órdenes.

## 9. Lo que no está en git (se queda en el portátil)

- `Logs/device/magicsort/`: la grabación de 20 min de Magic Sort (`ms_largo.mp4`, 511 MB) y todo su análisis (`analisis/`: fotogramas, audio, observaciones; ≈ 489 MB). Las fichas de `docs/research/magic-sort/` lo resumen. Es de un juego de terceros: nunca se sube.
- `Logs/device/lovewater/` (vídeo de Love Water), `Logs/snapshots/` (capturas del Editor) y `Builds/` (APK).
- El worktree `../amazon-fill-fase1` (rama `fase1-mecanicas-niveles`, ya fusionada) y la rama `codex/orillas-vivas` de ChatGPT.
- La memoria de Claude del portátil: lo esencial está en la sección 8.
