# Medida del AAB y de 16 KB (tarea B2, tanda 4, ola 7)

Fecha: 2026-10-18 · Ejecutado por: agente (build-release) · Base: `integracion-fase1-fase2` en `cb2460e` (ola 6 cerrada) más FX8, en el worktree del carril Unity A · Fuentes: GDD 08 §6.1 y §12.1, CLAUDE.md (compuertas de 80 MB y 16 KB), `docs/setup/plan-instalacion.md` (el build de referencia de release es el del CI Linux x64).

## Veredicto provisional: cumple con los niveles 1-300 (falta C1b)

> **Actualización (B2b, 2026-10-19):** medida repetida con los niveles 1-700 (C1b) y las fuentes recortadas (FNT1) en la integración de la parte B de la ola 7: `game.aab` de **51,32 MB** y descarga en Play de **50,26 MB** como máximo; cumple las dos compuertas. Detalle en la sección «B2b» al final. Lo que sigue es la medida de B2a y se conserva como estaba.

**B2 no está terminada.** La tarea pide la medida con los 700 niveles de la ola 7 y depende de C1b, que no está en esta rama, como tampoco TJ3 ni TJ4, que van antes en el carril (`git log cb2460e..HEAD` solo trae FX8 y B2, y `tanda4/o7-c1b` no tiene commits sobre `cb2460e`). Lo que sigue es la medida de la base actual (B2a). Queda pendiente B2b: repetir `BuildGameAab`, `ci/size-gate.ps1 -Breakdown -RequireBundletool` y `ci/check-16kb.sh` sobre `game.aab` cuando C1b (y TJ3/TJ4) estén en la rama, y actualizar las tablas «Peso por grupo» y «Frente a B0». El tech-lead decide si deja B2 abierta o la parte así.

- **Tamaño:** el AAB del juego pesa **51,87 MB**, y su descarga en Play (`bundletool get-size total`) es de **50,80 MB** como máximo, frente al tope de 80 MB. El AAB de humo pesa 40,03 MB, con una descarga de 38,96 MB.
- **16 KB correcto** en `smoke.aab`, `smoke.apk` y `game.aab`. Las 7 `.so` de `arm64-v8a` están alineadas a 16384 en cada uno, y `zipalign -c -P 16 -v 4` sale bien en el APK y en el APK universal de cada AAB.
- **Manifiesto de `game.aab`:** `com.amazonfill.dev`, minSdk 26 y targetSdk 36. Permisos: `INTERNET` (sigue pendiente del dueño), `VIBRATE` (D-026) y el `DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION` propio de AndroidX.
- **Faltan los niveles 301-700.** El cliente solo trae los niveles 1-300 (`tutorial_v0` y los mundos 2-6). C1b, que registra los packs 7-9 (`pack_schema` 3, con la Tinaja), no está en esta base: el commit que pasó el encargo como C1b (`cb2460e`) es el cierre de la ola 6. Lo que añadan es **una estimación, no una medida**. El peso por nivel crece con el mundo: 0,87 KB en el mundo 2, 1,21 en el 3, 1,31 en el 4, 1,34 en el 5 y **1,39 KB en el 6** (`mundo_06_v1.json`: 139.470 B para 100 niveles, 11.603 B con deflate). Las muestras del mundo 7 de `tools/out/sample_*` dan 1,28 KB, menos que el mundo 6, así que no sirven de cota. Con 1,39 KB por nivel, los 400 niveles suman unos **0,56 MB sin comprimir** (≈ 0,6 MB si siguen creciendo y con los campos de la Tinaja) y **≈ 0,05 MB en el AAB** (0,12 KB por nivel con deflate). Aunque fueran el doble, no cambiarían el veredicto de tamaño, pero solo la medida de B2b lo confirma.

## Qué AAB mide la compuerta de tamaño

- **El del juego:** `AF.EditorTools.AudioAssetSetup.BuildGameAab`, que deja `Builds/Android/game.aab`, o el AAB de release del CI con las mismas escenas de Build Settings. Es el que lleva el audio, los packs de niveles, las tablas de textos y la escena `Game.unity`.
- **El de humo** (`AF.EditorTools.BuildSmoke`, `smoke.aab`) usa una escena mínima: no lleva el audio, los niveles ni los textos, y solo tiene las fuentes de `Resources/`. Pesa 11,84 MB menos y sirve para la compuerta de 16 KB, no para la de tamaño.
- Queda escrito en la cabecera de `ci/size-gate.ps1`. El job de CI (s30/s32) aún no existe y no se crea en B2.

## Comandos (escritorio x64, Unity 6000.3.25f1 x86_64, con el semáforo de Unity)

| Comando | Código | Resultado |
|---|---|---|
| `unity run <wt> --architecture x86_64 --log-file Logs/b2-smoke.log --no-tail --timeout 5400 --non-interactive -- -nographics -buildTarget Android -executeMethod AF.EditorTools.BuildSmoke.BuildAndroid` | 0 | AAB en 245,1 s y APK en 14,6 s, sin errores |
| `unity run <wt> --architecture x86_64 --log-file Logs/b2-game-aab.log ... -executeMethod AF.EditorTools.AudioAssetSetup.BuildGameAab` | 0 | `game.aab` en 35,7 s (incremental), más `Logs/audio-size.md` y `Logs/aab-size.md` |
| Ídem tras la revisión, con el recuento de assets por grupo y ruta (su log no se conservó: en `Logs/` solo queda `b2-game-aab.log`, del primer build) | 0 | `game.aab` de 51.869.066 bytes (51,87 MB, igual) y `Logs/aab-size.md` de nuevo (15:42) |
| `pwsh ci/size-gate.ps1 Builds/Android/smoke.aab -Breakdown -RequireBundletool -Bundletool <bundletool-all-1.17.2.jar del Editor>` | 0 | archivo 40,03 MB y Play 38,95-38,96 MB |
| `pwsh ci/size-gate.ps1 Builds/Android/game.aab -Breakdown -RequireBundletool -Bundletool <ídem>` | 0 | archivo 51,87 MB y Play 50,79-50,80 MB |
| `CHECK16_REQUIRE_ZIPALIGN=1 BUNDLETOOL_JAR=<ídem> ZIPALIGN=<build-tools 36.0.0> bash ci/check-16kb.sh smoke.aab smoke.apk` | 0 | 14 `.so` de 64 bits y 0 fallos |
| `CHECK16_REQUIRE_ZIPALIGN=1 ... bash ci/check-16kb.sh game.aab` | 0 | 7 `.so` de 64 bits y 0 fallos |

SHA-256:

- `smoke.aab`: `d5987d8f395045cebcb1024435d42f14b9c80ec8d594f376201375b014d92cff`
- `smoke.apk`: `f31d445a4481ad8e50b1a73894297cf2eaafa598e7a7643befb9b7c4f298c19b`
- `game.aab`: `cf3e0061ebe82928d7928c3ecfc1d0ef3c5b413fd2d0e9adb3d2f3f31ce2d489` (el del build tras la revisión, el que miden las tablas; el del primer build, del mismo tamaño, era `a49112cd…b004`)

Los tres están en `Builds/Android/`, que ignora git, y no se suben a ningún sitio.

Nota: `BuildSmoke` escribe `tamano=38.18 MB` en MiB. La compuerta, como Play Console, usa 1 MB = 1.000.000 bytes, así que son 40,03 MB.

## Peso por grupo

### Dentro del AAB (`ci/size-gate.ps1 -Breakdown`; MB comprimidos en el AAB / sin comprimir)

| Grupo | `game.aab` | `smoke.aab` |
|---|---|---|
| Código nativo (`lib/arm64-v8a`: il2cpp, unity, burst, c++) | 27,97 / 94,62 | 27,97 / 94,62 |
| Datos de Unity (`assets/bin`) | 21,16 / 49,48 | 9,32 / 31,33 |
| dex (Java/Kotlin) | 2,35 / 6,44 | 2,35 / 6,44 |
| Recursos y manifiesto de Android | 0,22 / 0,67 | 0,22 / 0,67 |
| Metadatos, firma y `root` | 0,07 / 0,17 | 0,07 / 0,17 |
| Cabeceras y directorio del ZIP | 0,09 | 0,09 |
| **Total** | **51,87** | **40,03** |

La última fila es el tamaño del archivo menos la suma de los `CompressedLength` de sus 493 entradas (491 en `smoke.aab`): 91.522 B en `game.aab` y 91.174 B en `smoke.aab`. Son las cabeceras locales y el directorio central del ZIP, que no pertenecen a ninguna entrada; `ci/size-gate.ps1 -Breakdown` ya la escribe. Las filas van redondeadas a dos decimales, así que su suma puede diferir del total en 0,01.

### Dentro de los datos de Unity (Build Report de `game.aab`; `Logs/aab-size.md`; MB empaquetados sin comprimir)

| Grupo | Assets | MB | % | Estimación de 08 §6.1 |
|---|---:|---:|---:|---|
| Niveles (packs 1-300 y `seasons_v1`) | 7 | 0,38 | 1,1 | ≈ 2,5 MB sin comprimir con los 700 niveles y el Reto del río (aquí faltan los 301-700, ≈ +0,56 MB estimados; ver el veredicto) |
| Textos, Mapa y datos (`Data/Loc`, `Map`, `Content`, `Fauna`) | 6 | 0,85 | 2,4 | — |
| Fuentes (atlas TMP de Nunito, Baloo 2 y LiberationSans) | 20 | 6,88 | 19,8 | ≈ 1,5 MB |
| Arte | 1 | 0,35 | 1,0 | Los gráficos siguen hechos por código; el arte encargado aún no ha llegado |
| Audio (173 clips de la entrega 1) | 174 | 16,91 | 48,7 | ≈ 7 MB (AU3 ya lo midió: 16,91) |
| Shaders y materiales | 8 | 0,09 | 0,2 | — |
| Escenas y scripts | 326 | 0,03 | 0,1 | — |
| Paquetes (URP, TextMeshPro, Input System…) | 2.326 | 5,92 | 17,0 | — |
| Recursos integrados de Unity | 3 | 3,33 | 9,6 | — |
| Otros | 12 | 0,03 | 0,1 | — |
| **Datos empaquetados** | 2.882 | **34,76** | 100 | |

La columna Assets cuenta las rutas distintas de cada grupo: un asset con entradas en dos grupos (en este build, uno de `Otros`) cuenta en los dos y una vez en el total, y las entradas sin ruta suman MB pero no cuentan como asset. Los MB empaquetados son los bytes serializados antes de comprimir el AAB: el audio Vorbis apenas se comprime, y los JSON y los atlas sí. Las fuentes pesan 6,88 MB sin comprimir, cuatro veces y media lo estimado: es el primer candidato a recortar si hace falta sitio.

## Frente a B0 y al AAB de 35 MB

| Escenario | AAB (MB) | Margen hasta 80 MB |
|---|---:|---:|
| AAB de humo de la compuerta del día 1 (2026-10-06) | 35,12 | 44,88 |
| AAB de humo de hoy (`smoke.aab`) | 40,03 | 39,97 |
| AAB del juego de hoy (`game.aab`, niveles 1-300, sin SDK ni arte) | 51,87 | 28,13 |
| + arte encargado según B0, a 1× en el build (11,02 MB) | 62,89 | 17,11 |
| + arte según B0, a 2× de entrega (21,55 MB) | 73,42 | 6,58 |
| + SDK nativos de 08 §6.1 (≈ 12-18 MB) sobre el escenario a 1× | 74,9-80,9 | 5,1 a −0,9 |
| + SDK sobre el escenario a 2× | 85,4-91,4 | **supera** |

- B0 sumó su estimación al AAB de humo de 35 MB, pero el AAB que cuenta es el del juego: hoy pesa 16,75 MB más que aquel, sobre todo por el audio.
- Las cifras de B0 son una cota superior (el AAB comprime algo los ASTC).
- Aun así, **con los SDK y el arte a 2× no cabe**, y a 1× queda justo. Hace falta Addressables y PAD (08 §6.2), recortar las fuentes, o las dos cosas.
- Lo medirá de verdad M5.2 al integrar el arte. Esta tabla solo avisa antes.

## Avisos de LocReleaseGate (B1) en los dos builds

Con `com.amazonfill.dev` la compuerta solo avisa. Encuentra **811 problemas** que pararían un build de release:

- **UI incompleta en `en`:** 153 claves de `strings.csv` sin texto.
- **UI incompleta en `pt-BR`:** 154 claves sin texto. Cada idioma cuenta como un problema.
- **Textos culturales de `strings.csv` en `pendiente_asesor`:** 126, por ejemplo `difficulty_subtitle_hard`, `fauna_sighted_card`, `firefly_gift`, `hint_no_safe_pour` e `iap_af_skin_m02_name`.
- **Filas de `facts.csv` sin `aprobado_es`:** 683 (681 en `pendiente_asesor` y 2 en `borrador`).

Lo esperable en esta fase: hasta el visto bueno del asesor cultural y las traducciones, un build de release con el `applicationId` definitivo saldría con el código 6.

## Cambios de B2

- **`ci/size-gate.ps1`:**
  - `-Breakdown` escribe el desglose de cada `.aab` por grupo: código nativo, datos de Unity, dex, recursos de Android, metadatos, cada asset pack y las cabeceras y el directorio del ZIP, para que el desglose sume el tamaño del archivo. Solo informa y no cambia el veredicto.
  - Un `.aab` ilegible con `-Breakdown` sale con 2 (consta en el uso y en el código 2 de la cabecera).
  - La cabecera dice qué AAB mide la compuerta.
- **`Assets/_Project/Editor/CI/AabSizeReport.cs`** (nuevo): clasifica el contenido del Build Report por grupo y cuenta los assets por grupo y ruta. `AudioAssetSetup.BuildGameAab` lo escribe en `Logs/aab-size.md`, junto al informe del audio. Lo prueba `AabSizeReportTests` (EditMode).
- **`scripts/unity-test.ps1` y `scripts/unity-compile.ps1`:** si otro proceso tiene abierto el log o el XML de la ejecución anterior y no se puede borrar, usan un nombre único junto a él (`<nombre>-<fecha>-<pid><ext>`) y avisan. Esos procesos son `Unity.Licensing.Client` o el broker de la CLI. Antes salían con 1, y en `unity-compile.ps1` el 1 significa «errores de compilación». Los códigos de salida documentados no cambian. Al empezar borran los nombres alternativos de ejecuciones anteriores que ya no estén abiertos, para que no se acumulen en `Logs/`.

## Archivos fuera de la lista de B2

La tarea solo asignaba `ci/size-gate.ps1` y `BuildSmoke.cs` (este no hizo falta tocarlo). Quedan fuera de la lista, todos dentro del ámbito del rol build-release (`ci/**`, `scripts/**`, `Assets/_Project/Editor/CI/**`):

- `Assets/_Project/Editor/CI/AabSizeReport.cs` y `Assets/_Project/Tests/EditMode/AabSizeReportTests.cs` (nuevos, con sus `.meta`): el informe por grupo que pide la especificación («anotar el tamaño por grupo»). El Build Report no se puede leer desde `size-gate.ps1`.
- `Assets/_Project/Editor/CI/AudioAssetSetup.cs`: solo el gancho que escribe `Logs/aab-size.md` en `BuildGameAab`, que es el AAB que mide la compuerta.
- `scripts/unity-test.ps1` y `scripts/unity-compile.ps1`: **es un cambio aparte**, no de la medida. Hizo falta porque un `Unity.Licensing.Client` superviviente bloqueaba el log y los scripts salían con 1. Son infraestructura de todos los carriles: el tech-lead decide si entra con B2 o se saca a una tarea propia (va en un commit separado de la revisión).
- `docs/setup/medida-aab-b2.md` (este informe): es el informe de tamaño que pide el criterio de aceptación.

B2 no edita el GDD ni el traspaso: lo que hay que cambiar en ellos lo hace el carril de documentación (sección siguiente).

## Pendiente

- **Para el carril de documentación** (B2 no edita el canon):
  - GDD 08 §6.1, fila «Audio», y la cabecera del capítulo: anotar que B2 midió el AAB del juego el 2026-10-18 (`game.aab` de 51,87 MB y 50,80 MB de descarga en Play, con los niveles 1-300) y que la compuerta de tamaño del CI **deberá** medir ese AAB y no el de humo: el job aún no existe (s30). Mejor con la medida repetida con los niveles 1-700.
  - Traspaso (`docs/dev/traspaso/README.md`, paso 4, «La compuerta de tamaño del CI»): las mismas cifras y que con el arte a 2× de B0 y los SDK no cabe en 80 MB (sección «Frente a B0 y al AAB de 35 MB»).
- **B2b:** repetir esta medida con los niveles 1-700 cuando C1b (y TJ3/TJ4) estén en la rama: `BuildGameAab`, `ci/size-gate.ps1 -Breakdown -RequireBundletool` y `ci/check-16kb.sh` sobre `game.aab`, y actualizar el veredicto y las tablas «Peso por grupo» y «Frente a B0». Hasta entonces B2 no se da por cerrada en la integración.
- La medida de referencia es la del CI Linux x64 (s30/s32), con `-RequireBundletool` y `CHECK16_REQUIRE_ZIPALIGN=1`, sobre el AAB del juego.
- Del dueño:
  - decidir el permiso `INTERNET`;
  - decidir cómo se gana sitio antes de los SDK y del arte a 2×: Addressables y PAD, fuentes recortadas o arte a 1×.

## FNT1: fuentes recortadas sin perder ningún glifo (2026-10-19)

Tarea FNT1 de la ola 7, carril de fuentes (rama `tanda4/o7-fonts`, con B2 fusionada). Mide, recorta y vuelve a medir las fuentes, el primer candidato de B2. No decide nada sobre el resto del sitio (Addressables y PAD, arte a 1×): eso sigue siendo del dueño.

### De dónde salían los 6,88 MB

Desglose del grupo «Fuentes» del Build Report de `game.aab` (sección nueva «Fuentes por asset» de `Logs/aab-size.md`, MB empaquetados sin comprimir):

| Asset | Antes | Después |
|---|---:|---:|
| Atlas de los 5 font assets del juego (Alpha8, 1024 × 1024 cada uno) | 5 × 1,049 = 5,24 | 2 × 0,852 + 3 × 0,787 = 4,07 |
| Atlas de `LiberationSans SDF` (TMP, 1024 × 1024) | 1,05 | — |
| `LiberationSans.ttf` (la reserva dinámica `LiberationSans SDF - Fallback` la arrastraba) | 0,35 | — |
| Tablas de glifos de los font assets (5 × 0,020 y LiberationSans 0,029) | 0,13 | 0,10 |
| Materiales (9 de estilo, 5 de los font assets y 4 de LiberationSans) | 0,02 | 0,01 |
| `PerfectDOSVGA437.ttf` (depuración de URP, paquete; fuera del alcance de FNT1) | 0,08 | 0,08 |
| **Total del grupo** (20 assets → 15) | **6,88** | **4,26** |

- Casi todo es píxel de atlas: 1 byte por píxel, sin compresión de textura. Las tablas de caracteres pesan poco.
- **Los atlas iban medio vacíos.** Cada fuente tiene 199-202 glifos a 56 pt con un margen de 13 px. Con el margen, ocupan 0,64-0,70 Mpx de los 1,05 Mpx del atlas fijo de 1024 × 1024.
- **LiberationSans no pintaba nada en el juego** y entraba entera en el build. Estaba en `Resources` y era la fuente por defecto de `TMP Settings`. Todos los textos usan su fuente de `UiFonts`, y cada carácter de las tablas está en el atlas de su familia (`FontSetup.AtlasGaps`, B1).

### Qué se recortó, y qué no

Lo genera `AF.EditorTools.FontSetup.ApplyAll`, idempotente. No hay YAML a mano.

1. **Atlas a medida, con el mismo muestreo y el mismo margen.** Cada fuente se queda con el atlas más pequeño de `FontSetup.AtlasSizes` en el que caben todos sus glifos: 1024 de ancho y de 512 a 1024 de alto, en pasos de 64.
   - Resultado: Baloo 2 ExtraBold y Bold y Nunito Regular, 1024 × 768; Nunito ExtraBold y Bold, 1024 × 832.
   - El campo de distancia de cada glifo es el mismo (56 pt, 13 px): el texto se ve igual.
   - Un alto que no es potencia de dos no cuesta nada: Alpha8 sin mipmaps, en OpenGL ES 3 y Vulkan.
2. **LiberationSans fuera del build.**
   - `TMP Settings` toma Nunito Regular SDF como fuente por defecto.
   - Los cuatro assets de LiberationSans pasan, con sus GUID, de `Assets/TextMesh Pro/Resources/Fonts & Materials/` a `Assets/TextMesh Pro/Fonts/`, fuera de `Resources`. La carpeta vacía se borra.
   - Al reserializar los ajustes, Unity escribe `m_ActiveFontFeatures` como `kern`. Es la conversión que TMP ya hacía al cargarlos (`TMP_Settings.instance`), así que el interletraje no cambia.
3. **Descartado, para no tocar el aspecto:**
   - **Menos margen.** Hasta 12 px en Baloo 2 (contorno y sombra, ≥ 0,20 em de la guía 5.6) y 9 px en Nunito ExtraBold (solo contorno, ≥ 0,15 em). Ahorraría un 7 % y un 25 % de sus atlas, pero cambia el grosor de la negrita sintética de TMP (`<b>`), que depende del margen.
   - **Menos muestreo.** Cambia el aspecto.
   - **Reservas dinámicas.** Hoy no hace falta ninguna: no queda ningún carácter raro fuera de los atlas.
4. Los juegos de caracteres siguen siendo ASCII, Latin-1, la tipografía de la guía y lo que piden las tablas (B1). El pseudoidioma en-XA ya solo usa Latin-1.

### Medida antes y después (escritorio x64, `AudioAssetSetup.BuildGameAab` y `ci/size-gate.ps1 -Breakdown -RequireBundletool`, bundletool 1.17.2 del Editor)

| | Antes (rama en 4eece23) | Después (FNT1) | Diferencia |
|---|---:|---:|---:|
| Fuentes en el Build Report (MB sin comprimir) | 6,88 | 4,26 | −2,62 |
| Datos de Unity empaquetados (Build Report) | 34,76 | 32,14 | −2,62 |
| Datos de Unity en el AAB (comprimidos / sin comprimir) | 21,16 / 49,48 | 20,52 / 46,86 | −0,64 / −2,62 |
| `game.aab` | 51,87 MB | 51,23 MB | −0,64 |
| Descarga en Play (`bundletool get-size total`, máx.) | 50,80 MB | 50,17 MB | −0,63 |

- Los atlas son sobre todo ceros y el AAB ya los comprimía bien. Por eso los 2,62 MB sin comprimir se quedan en unos 0,64 MB de descarga.
- El margen hasta 80 MB crece en 0,63 MB. En la tabla «Frente a B0», el escenario a 1× con los SDK pasa de 5,1 a −0,9 MB a 5,7 a −0,3 MB.
- SHA-256:
  - `game.aab` de antes: `9e77e4d0…a53c`;
  - `game.aab` de después: `4f03fcb6…f9b5`.
- `Builds/` y `Logs/` los ignora git.

### Ningún glifo perdido: pruebas y capturas

- **`FontTrimTests` (EditMode, nuevo):**
  - cada carácter de `strings.csv`, `facts.csv` y `glossary.csv` (es-419, en y pt-BR), de las cifras y de en-XA está en el atlas estático de su propia fuente, en las cinco, sin buscar en reservas (con ellas, TMP acabaría en la fuente por defecto y el carácter se pintaría con otra tipografía) y sin añadir glifos. Lo mismo con los símbolos fijos de `FontSetup`, que `FontSetup.Characters` mete siempre en los atlas: los que pone el código (`CodeSymbols`: ×, · y U+00A0, en las cinco) y las flechas → ↑ ↓ de `strings.csv` (`TitleCodeSymbols`, claves de `FontCoverage.TitleOnlyKeys`; solo en Baloo 2);
  - ✓ no está en ningún atlas: es un icono procedural;
  - los atlas conservan el muestreo y el margen, son estáticos, sin TTF y de ≤ 1024 × 1024, y suman menos píxeles que antes;
  - no queda ningún font asset ni TTF en una carpeta `Resources`, salvo los del juego;
  - `TMP Settings` apunta a Nunito Regular, sin reservas globales.
- **`AabSizeReportTests`:** cubre la sección «Fuentes por asset».
- **Capturas antes y después** (`FontTrimSnapshotTests`, nuevo, y `FontGlyphSnapshotTests` de B1, con gráficos):
  - pantallas: el inicio, Ajustes con «Sobre la Amazonía» y las frutas, la ficha del açaí abierta desde ahí (`fnt1_02b_ficha_fruta`, `FruitSheet`, con su nombre y sus datos), la tarjeta de Manaos en el Mapa y la victoria del 3 con la línea de fruta, en es-419 a 360 × 780 dp y al 100 % y en en-XA a 360 × 640 dp y al 130 %, más las hojas de glifos y el inicio y la victoria del 11 de B1: 18 capturas (más 5 de es-419 al 130 %, solo después; ver abajo);
  - el antes son las fuentes y `TMP Settings` de `4eece23` (LiberationSans dentro de `Resources`) con el mismo código de las pruebas; el después, las de FNT1. Están en `Logs/snapshots-antes/<locale>/` y `Logs/snapshots-despues/<locale>/`, con los mismos nombres (git los ignora);
  - las 18 pasan el escaneo de B1 antes y después: ningún carácter fuera del atlas de su fuente y ningún □;
  - es-419 al 130 % (revisión de FNT1): la tarea pide las capturas «en es-419 y en-XA al 130 %», y las 18 de arriba siguen la convención de las capturas del repositorio (es-419 al 100 % y en-XA al 130 % como peor caso). Para cubrir también la otra lectura, `Capture_Es419_640_130` hace las mismas 5 pantallas en es-419 a 360 × 640 dp y al 130 %, el tamaño real con más texto en español (`fnt1_*_640_130`, en `Logs/snapshots-despues/es-419/`). Solo hay **después**: pasan el escaneo (ningún carácter fuera del atlas y ningún □), y los cortes de línea más largos («Faltan 6 niveles para Floresta Nacional do Tapajós», «en Pará y Amapá» de la ficha) caben sin tocar el borde. El **antes** no se capturó: exigía cambiar en el worktree las fuentes commiteadas por las de `4eece23` y deshacerlo después, y el permiso del agente no lo dejó. Que un glifo esté en el atlas no depende de la escala (`FontTrimTests` cubre todos los caracteres) y el muestreo del SDF ya se compara al 130 % en en-XA (diferencia máxima 3-6 en las de texto). Queda para que el tech-lead confirme si basta así o si quiere también el antes de esas 5;
  - la comparación píxel a píxel, con el SHA-256 de cada PNG, está en `Logs/fnt1-diff.txt`, y las ampliaciones ×8 (antes, después y diferencia ×8) del inicio y de la victoria, en `Logs/fnt1-diff/`:
    - las 8 de texto de FNT1 (Ajustes, ficha, tarjeta y victoria): 0,015-0,047 % de píxeles distintos, diferencia máxima de 3 a 6 niveles de 255 y ninguno por encima de 16. Es el muestreo del mismo campo de distancia en otra posición del atlas: a simple vista no se distingue;
    - `fnt1_01_inicio`: 0,038 % y 0,067 %. Los únicos píxeles con más de 16 niveles (77 y 39, hasta 102) están en x 148-168, y 315-356: el brazo animado de TiTino en su ventanita, no el texto;
    - las hojas de glifos de B1: 0,071-0,096 %, máximo 5-7; `b1_02_inicio_l4`: 0,021 % y 0,027 %, máximo 17 y 11 (un solo píxel por encima de 16);
    - `b1_03_victoria_l11`: 4,6 % en es-419 (máximo 187) y 0,49 % en en-XA (máximo 7). Son las hojas que caen en la victoria, que esa prueba gana en tiempo real; no hay ninguna en el texto.

### Pendiente

- Del dueño, si quiere más sitio de las fuentes, con nueva revisión visual: el margen mínimo de la guía (−7 % en Baloo 2 y −25 % en Nunito ExtraBold de sus atlas) o quitar `PerfectDOSVGA437.ttf` (0,08 MB) desactivando la depuración en tiempo de ejecución de URP.
- La medida de referencia sigue siendo la del CI Linux x64 (s30/s32), y B2b la repetirá con los niveles 1-700.

## B2b: medida con los niveles 1-700 y FNT1 (2026-10-19)

Base: el carril de los mundos 7-9 (`tanda4/o7-c1b`) con GEN79, C1b y C1c, la parte A de la ola 7 (`integracion-fase1-fase2` en `80b26b4`: TJ3/TJ4, FX8, B2, FX9, FX10, M5.1 y M5.1c), FX11 y FNT1 fusionados, y el arreglo de la fusión de los tests del fin de contenido. Escritorio x64, Unity 6000.3.25f1 x86_64 con el semáforo de Unity, bundletool 1.17.2 y build-tools 36.0.0 del Editor. Ejecutado por un agente (unity-gameplay-dev, integración).

### Veredicto: cumple

- **Tamaño:** `game.aab` pesa **51,32 MB** (51.321.880 B) y su descarga en Play (`bundletool get-size total`) es de **50,24-50,26 MB**, frente al tope de 80 MB.
- **16 KB correcto:** las 7 `.so` de `arm64-v8a` están alineadas a 16384, y `zipalign -c -P 16 -v 4` sale bien en el APK universal.
- **Manifiesto:** `com.amazonfill.dev`, minSdk 26 y targetSdk 36. Permisos: `INTERNET` (sigue pendiente del dueño), `VIBRATE` (D-026) y el `DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION` propio de AndroidX. Ninguno nuevo.
- **Los niveles 301-700 confirman la estimación de B2a:** los tres packs (`mundo_07_v1.json`, `mundo_08_v1.json` y `mundo_09_v1.json`, variante b publicada) suman 575.503 B (0,58 MB sin comprimir, 1,44 KB por nivel; B2a estimaba ≈ 0,56-0,6 MB) y añaden unos 0,06 MB comprimidos al AAB.

### Comandos

| Comando | Código | Resultado |
|---|---|---|
| `unity run <wt> --architecture x86_64 --log-file <wt>/Logs/b2b-game-aab.log --no-tail --timeout 5400 --non-interactive -- -nographics -buildTarget Android -executeMethod AF.EditorTools.AudioAssetSetup.BuildGameAab` | 0 | `game.aab` en 269,0 s, 0 errores y 1 aviso, más `Logs/audio-size.md` y `Logs/aab-size.md` |
| `pwsh ci/size-gate.ps1 Builds/Android/game.aab -Breakdown -RequireBundletool -Bundletool <bundletool-all-1.17.2.jar del Editor>` | 0 | archivo 51,32 MB y Play 50,24-50,26 MB |
| `CHECK16_REQUIRE_ZIPALIGN=1 BUNDLETOOL_JAR=<ídem> ZIPALIGN=<build-tools 36.0.0>/zipalign.exe bash ci/check-16kb.sh Builds/Android/game.aab` | 0 | 7 `.so` de 64 bits y 0 fallos |
| `java -jar <bundletool> dump manifest --bundle Builds/Android/game.aab` | 0 | el manifiesto de arriba |

SHA-256 de `game.aab`: `e21ff7ee7c1fc6081d403a0beadb22e164032ebc4b3feff06b7f1e0e9ef67b7c` (en `Builds/Android/`, que ignora git).

### Peso por grupo

Dentro del AAB (`ci/size-gate.ps1 -Breakdown`; MB comprimidos en el AAB / sin comprimir):

| Grupo | B2a (1-300) | FNT1 (1-300) | B2b (1-700 y FNT1) |
|---|---|---|---|
| Código nativo (`lib/arm64-v8a`) | 27,97 / 94,62 | — | 28,00 / 94,68 |
| Datos de Unity (`assets/bin`) | 21,16 / 49,48 | 20,52 / 46,86 | 20,58 / 47,47 |
| dex (Java/Kotlin) | 2,35 / 6,44 | — | 2,35 / 6,44 |
| Recursos y manifiesto de Android | 0,22 / 0,67 | — | 0,22 / 0,67 |
| Metadatos, firma y `root` | 0,07 / 0,17 | — | 0,07 / 0,17 |
| Cabeceras y directorio del ZIP | 0,09 | — | 0,09 |
| **Total** | **51,87** | **51,23** | **51,32** |

FNT1 no publicó su desglose completo, solo la fila de los datos de Unity (sección FNT1). El código nativo crece 0,03 MB con el código de la ola 7 (TJ3/TJ4, C1b y C1c).

Dentro de los datos de Unity (Build Report, `Logs/aab-size.md`; MB empaquetados sin comprimir):

| Grupo | Assets | B2a | B2b | Diferencia |
|---|---:|---:|---:|---:|
| Niveles (packs 1-700 y `seasons_v1`) | 10 | 0,38 | 0,96 | +0,58 |
| Textos, Mapa y datos | 6 | 0,85 | 0,87 | +0,02 |
| Fuentes (FNT1) | 15 | 6,88 | 4,26 | −2,62 |
| Arte | 1 | 0,35 | 0,35 | 0 |
| Audio | 174 | 16,91 | 16,91 | 0 |
| Shaders y materiales | 8 | 0,09 | 0,09 | 0 |
| Escenas y scripts | 331 | 0,03 | 0,03 | 0 |
| Paquetes | 2.326 | 5,92 | 5,92 | 0 |
| Recursos integrados de Unity | 3 | 3,33 | 3,33 | 0 |
| Otros | 12 | 0,03 | 0,03 | 0 |
| **Datos empaquetados** | 2.885 | **34,76** | **32,74** | **−2,02** |

Las fuentes pesan lo mismo que en FNT1: los textos de la ola 7 no añadieron ningún carácter fuera de los atlas (`FontTrimTests`, en verde en este árbol).

### Frente a B0, con el AAB de hoy

| Escenario | AAB (MB) | Margen hasta 80 MB |
|---|---:|---:|
| AAB del juego (`game.aab`, niveles 1-700 y FNT1, sin SDK ni arte) | 51,32 | 28,68 |
| + arte encargado según B0, a 1× en el build (11,02 MB) | 62,34 | 17,66 |
| + arte según B0, a 2× de entrega (21,55 MB) | 72,87 | 7,13 |
| + SDK nativos de 08 §6.1 (≈ 12-18 MB) sobre el escenario a 1× | 74,3-80,3 | 5,7 a −0,3 |
| + SDK sobre el escenario a 2× | 84,9-90,9 | **supera** |

El veredicto de B2a no cambia: con los SDK y el arte a 2× no cabe, y a 1× queda justo. Los 700 niveles cuestan menos de una décima de MB en el AAB.

### Avisos de LocReleaseGate (B1)

Con `com.amazonfill.dev` solo avisa: **814 problemas** que pararían un build de release (811 en B2a; no se desglosaron los 3 de más). Lo esperable hasta el visto bueno del asesor cultural y las traducciones.

### Pendiente

- La medida de referencia sigue siendo la del CI Linux x64 (s30/s32), con `-RequireBundletool` y `CHECK16_REQUIRE_ZIPALIGN=1`, sobre el AAB del juego.
- Para el carril de documentación: GDD 08 §6.1 y el traspaso, con estas cifras (B2b sustituye a B2a como la medida de la ola 7).
- Del dueño, lo mismo que en B2a: el permiso `INTERNET` y cómo se gana sitio antes de los SDK y del arte a 2×.
