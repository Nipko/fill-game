# Unity Editor en Windows 11 ARM64 (Snapdragon X Plus) para Aquazonia — estado a 6 de octubre de 2026

Tema A del setup. Investigación sin instalar nada: solo lectura web, la API pública de versiones de Unity, la API de GitHub/Docker Hub y comandos de solo lectura (`winget show`, consultas de registro y de versión). Leyenda: **[V]** verificada con URL o comando, **[E]** estimación, **[NV]** no verificable hoy.

Máquina: Windows 11 Home 25H2 (build 26200.9457), ARM64, Snapdragon X Plus X1P42100, Adreno X1-45 con driver 31.0.137.0 del 01-07-2026, 15,6 GB de RAM [V, registro y `Win32_VideoController` locales].

---

## Respuesta corta

- **Sí hay editor nativo Windows ARM64.** Existe desde el primer Unity 6 final, 6000.0.0f1 (29-04-2024); antes estuvo en beta en 2023.3 (enero de 2024). Hoy está en la LTS vigente **6000.3.25f1** (changeset **e1dba0a9aba4**, 24-09-2026) y en la última versión de soporte, **6000.6.4f1** (changeset **12bfff696524**, 01-10-2026). Los requisitos oficiales lo listan como plataforma de editor ("Arm64", Windows 11 21H2+), con limitaciones documentadas [V].
- **Unity Hub tiene build ARM64 nativa** desde la 3.19.0 (30-06-2026). La vigente es la **3.22.2** (02-10-2026), y `winget` ya entrega en esta máquina el `UnityHubSetup-3.22.2-arm64.msix`. Desde la 3.21.0 el Hub instala la nueva **Unity CLI** (`unity`, 1.0.0-beta.12), que también existe como MSIX ARM64 [V].
- **Arquitectura al instalar:** en Windows ARM, el Hub instala por defecto el editor ARM64 si no se le pasa ninguna opción. La opción `--architecture` está documentada solo para macOS. El fallo que descargaba x64 con `-a arm64` en Windows ARM (HUBX-1596) se corrigió en la Hub 3.17.0-beta.1. Recomendación: no pasar `-a` y comprobar la arquitectura después de instalar [V].
- **Módulos:** el editor ARM64 tiene exactamente la misma lista de módulos que el x64. No hay módulos exclusivos de x64. Incluye Android Build Support, OpenJDK 17.0.18+8, Android SDK & NDK Tools (NDK r27c, plataformas 34-37, build-tools 36.0.0) y Windows Build Support (IL2CPP). Ahora bien, el **OpenJDK y el NDK que se descargan para ARM64 son los binarios x64** (`open-jdk-win-x64`, `android-ndk-r27c-windows`), así que se ejecutan bajo la emulación Prism [V].
- **IL2CPP para Android desde un host ARM64:** Unity no lo prohíbe y distribuye el módulo, pero su documentación dice que "no soporta plataformas que no ofrecen SDK nativos para Windows Arm64" y que esos builds "pueden funcionar con emulación x86". Google no publica NDK para host Windows ARM64 (solo `windows-x86_64`). **No encontré ningún informe público, ni a favor ni en contra, de un build IL2CPP Android hecho desde un editor ARM64 en Snapdragon X** [NV]. Hay que comprobarlo con una prueba mínima el día 1.
- **Gráficos:** Unity **no soporta Vulkan en Windows on Arm**; quedan DX11 y DX12, y DX12 es el predeterminado para proyectos nuevos desde 6.1 [V]. Problemas conocidos en Snapdragon X: artefactos de URP con Render Graph (UUM-92264, cerrado como "posible driver"), bake de iluminación por GPU que falla en Adreno (UUM-113477, sin backport a 6.3) y reinicios completos del equipo que algunos usuarios evitaron desactivando "Automatic super resolution" de Windows [V]. No encontré problemas específicos de Shader Graph [NV].
- **El editor x64 bajo Prism funciona:** Unity 6000.3.4f1 incluso registra en el log si es "x64 Editor on an arm64 device", y hay usuarios con 6000.0.x x64 en Snapdragon X Elite [V]. Va más lento que el nativo, aunque no hay cifras publicadas [E/NV]. Es el plan B natural, sobre todo si un plugin del editor solo trae DLL x64.
- **Versión recomendada: Unity 6.3 LTS 6000.3.25f1, changeset e1dba0a9aba4.** Tiene soporte hasta diciembre de 2027, mientras que 6.0 LTS termina el 16-10-2026 [V]. La más reciente con ARM64 es 6000.6.4f1 (6.6, "Supported Update", la que el Hub marca como `recommended`), pero para producción manda la LTS [V].
- **Licencia Personal:** se activa solo iniciando sesión (Hub, en Settings › Licenses desde la 3.20, o `unity auth login` + `unity license activate --personal --accept-eula`). La activación por línea de comandos con `-serial` no se aplica a Personal. El modo batch funciona en local una vez activada [V].
- **En CI cambió la forma de activar:** Unity eliminó la activación manual (.ulf) para Personal. game-ci añadió el 05-09-2026 una estrategia `personal` (email + contraseña con el Licensing Client) que activa en 6000.6.0f1. El `game-ci/unity-builder@v4` + `UNITY_LICENSE` que trae el GDD ya no sirve para Personal: hay que pasar a `unity-builder@v6` (o game-ci/cli) con una cuenta Unity dedicada a CI y sin 2FA [V].

---

## 1. Editor nativo Windows ARM64: desde cuándo y en qué estado

| Hito | Fecha | Estado | Marca |
|---|---|---|---|
| Beta nativa Windows on Arm en Unity 2023.3 (que pasó a llamarse Unity 6), vía Hub 3.7+ | 30-01-2024 | Beta | [V] discussions.unity.com/t/938916 |
| 6000.0.0f1: primer Unity 6 final; la API de versiones ya incluye `WINDOWS/ARM64` (`UnitySetupArm64-6000.0.0f1.exe`) con módulo Android | 29-04-2024 | Final ("TECH") | [V] API de versiones |
| 6000.0.23f1: primer 6.0 LTS, también con instalador ARM64 | 16-10-2024 | LTS | [V] API de versiones |
| 6000.3.0f1: 6.3 LTS; el lightmapper CPU funciona en el editor Windows on Arm "usando emulación x64 en un proceso separado" | 03-12-2025 | LTS | [V] notas 6000.3.0f1 |
| 6000.3.4f1: el Editor.log indica si el editor es ARM64 o "x64 en un dispositivo arm64" | 14-01-2026 | LTS | [V] notas 6000.3.4f1 |
| 6000.3.25f1 (última 6.3 LTS) y 6000.6.4f1 (última 6.6), ambas con instalador ARM64 | 24-09-2026 / 01-10-2026 | LTS / Supported | [V] API de versiones |
| 6000.7.0b3 (beta) y 7000.0.0a7 (alfa), también con instalador ARM64 | 05/06-10-2026 | Beta / Alfa | [V] API de versiones |

Requisitos oficiales del editor ARM64 (6.3 y 6.6, idénticos): Windows 11 21H2 (build 22000) o posterior, CPU Arm64, GPU con DX10/DX11/DX12 (o Vulkan) y "drivers oficialmente soportados por el fabricante" [V sysreq 6.3/6.6]. El editor ARM64 necesita Windows 11 porque depende de la emulación x64, que solo existe en Windows 11: algunos componentes siguen siendo x64, por ejemplo el Package Manager y el baker de Enlighten (staff de Unity, feb. y nov. de 2025) [V discussions 1606124 y 1695327].

Limitaciones documentadas para Windows on Arm (sysreq 6.3 y 6.6) [V]:

| Limitación (texto de Unity) | Impacto en Aquazonia |
|---|---|
| "Unity doesn't support Vulkan for Windows on Arm" | El editor usará DX12 o DX11. Vulkan sigue disponible como API **del juego en Android**: eso es otra cosa |
| No soporta "platforms that don't provide native Windows Arm64 SDKs"; "Build for these platforms might still work with x86 emulation" | **Android cae aquí**: el NDK solo existe para host x86_64 (sección 4) |
| No soporta "packages with third-party binary dependencies that don't provide native Windows on Arm support" | Riesgo para SDK con DLL x64 del lado del editor (Firebase en Play Mode, etc.); ver Riesgos |
| CPU lightmapping no soportado (texto de 6.3) | Corregido en 6.3 con emulación x64 en un proceso aparte [V notas]; además es irrelevante para un juego 2D sin bake |
| Hub mínimo v3.7.0 Beta 1 | Cumplido de sobra con la 3.22.2 |

## 2. Unity Hub en ARM64 y elección de arquitectura

| Versión del Hub | Fecha | Cambio relevante | Marca |
|---|---|---|---|
| 3.7.0 | 2024 | Primer Hub capaz de instalar editores Windows on Arm (el propio Hub aún era x64) | [V] sysreq |
| 3.15.3 | 09-12-2025 | "Windows ARM64 Editors are now correctly identified" (antes salían como x64) | [V] notas Hub |
| 3.16.x | 2026 | Corregido UUM-138991: el Hub GUI instalaba la build x64 en Windows ARM64 (se reprodujo en la 3.16.3) | [V] issuetracker 3934 |
| 3.17.0-beta.1 | 03-2026 | Corregido HUBX-1596: `-a arm64`/`--architecture arm64` descargaba x64 en Windows 11 ARM | [V] issuetracker 4514 |
| 3.17.1 | 14-04-2026 | "Fixed the CLI would download the incorrect Editor architecture on ARM Windows" | [V] notas Hub |
| 3.18.0 | 06-05-2026 | Nueva **Unity CLI** independiente | [V] notas Hub |
| 3.19.0 | 30-06-2026 | **"Hub is now natively available for Windows ARM64"** | [V] notas Hub |
| 3.20.0 | 29-07-2026 | "Licenses has moved to Settings" | [V] notas Hub |
| 3.21.0 | 17-08-2026 | La Unity CLI viene con el Hub y se instala sola al primer arranque; gestión de módulos sin reinstalar | [V] notas Hub |
| 3.21.1 | 01-09-2026 | El Unity Licensing Client se ejecuta en nativo en Windows ARM64 | [V] notas Hub |
| 3.22.2 | 02-10-2026 | Versión vigente; CLI incluida 1.0.0-beta.12 | [V] notas Hub |

Comprobaciones locales de solo lectura (06-10-2026) [V]:

- `winget show --id Unity.UnityHub` devuelve la 3.22.2 con `Installer Url: …/hub/prod/3.22.2/UnityHubSetup-3.22.2-arm64.msix`.
- `winget show --id Unity.CLI` devuelve la 1.0.0-beta.12 con `…/cli/1.0.0-beta.12/unity-windows-arm64.msix`.
- `Unity.Unity.6000` en winget va en 6000.6.4f1 (instalador `UnitySetupArm64-6000.6.4f1.exe`) y **no contiene 6000.3.25f1**, así que winget no sirve para fijar la LTS.
- En mayo de 2026 la web de descarga ofreció por error solo el instalador x64; Unity publicó el enlace directo `https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup-arm64.exe` [V discussions 1721232].

**Cómo se elige la arquitectura:**

- **GUI:** el Hub nativo en Windows ARM instala ARM64 por defecto, y el bug que instalaba x64 se corrigió en 3.16.x. La guía actual "Download and install the Editor" no documenta ningún selector de arquitectura en Windows [V docs.unity.com/en-us/hub/add-editor]. Que el diálogo actual permita elegir x64 [NV]: el anuncio de 2024 decía que se veían "ambas arquitecturas", pero eso era en la pestaña Pre-release.
- **Hub CLI:** `--architecture`/`-a` aparece documentado como "macOS only" [V hub-cli-reference]. Sin opción, el instalador usa ARM64 en Windows ARM (nota de HUBX-1596) [V].
- **Unity CLI:** el SKILL oficial describe `--architecture` (`x86_64`, `arm64`) como opción global y la usa para filtrar o desambiguar instalaciones lado a lado (`unity editors --installed --architecture arm64`, `unity modules list … --architecture arm64`) [V]. La referencia web de `unity install`, en cambio, dice "macOS only" [V]. Que `-a x86_64` instale el editor x64 en Windows ARM queda **[NV]**.
- **Comprobación tras instalar:** `unity editors --installed --format json` (campo de arquitectura) y la línea del Editor.log que Unity añadió en 6000.3.4f1 [V].

## 3. Módulos disponibles para el editor ARM64 (6000.3.25f1)

Fuente: API oficial `services.api.unity.com/unity/editor/release/v1/releases?version=6000.3.25f1`, descarga `WINDOWS / ARM64` [V]. Al comparar los conjuntos ARM64 y X86_64, ninguno tiene IDs que no estén en el otro: **no hay módulos exclusivos de x64** [V].

| Módulo (nombre) | ID exacto | Instalado aprox. | Binario de host | Marca |
|---|---|---|---|---|
| Editor ARM64 (`UnitySetupArm64-6000.3.25f1.exe`) | — | 7,3 GB (descarga 4,2 GB) | ARM64 nativo (con partes x64) | [V] |
| Android Build Support | `android` | 6,9 GB | mismo instalador para ARM64 y x64 | [V] |
| └ OpenJDK | `android-open-jdk-17.0.18+8` | 240 MB | **win-x64** → emulado | [V] |
| └ Android SDK & NDK Tools | `android-sdk-ndk-tools` | — | requiere aceptar la licencia de Google del SDK/NDK | [V] |
| ··└ Android NDK | `android-ndk-r27c` | 2,4 GB | `android-ndk-r27c-windows.zip` (**x86_64**) → emulado | [V] |
| ··└ CMake | `cmake-3.22.1` | 39 MB | x64 | [V] |
| ··└ Build Tools | `android-sdk-build-tools-36.0.0` | 143 MB | x64 | [V] |
| ··└ Platform Tools (adb) | `android-sdk-platform-tools-36.0.0` | 15 MB | x64 | [V] |
| ··└ Platforms | `android-sdk-platforms-34`, `-35`, `-36`, `-37.0` | ~430 MB | — | [V] |
| ··└ Command-line Tools | `android-sdk-command-line-tools-16.0` | 166 MB | Java | [V] |
| Windows Build Support (IL2CPP) | `windows-il2cpp` | 0,9 GB | — | [V] |
| iOS Build Support | `ios` | 1,4 GB | — (iOS se compila en Codemagic) | [V] |
| Otros: `appletv`, `visionos`, `linux-il2cpp`, `linux-mono`, `linux-server`, `mac-mono`, `mac-server`, `universal-windows-platform`, `webgl`, `windows-server`, `visualstudio` (VS Community 2026), `documentation`, `language-*` | — | — | — | [V] |

- **Total editor + Android con todos los hijos:** 7,1 GB de descarga y **17,6 GB instalados** [V, suma de la API]. Con 213 GB libres no hay problema de disco.
- **ID de OpenJDK:** la documentación del Hub usa el ID genérico `android-open-jdk` [V docs]. El manifiesto de 6000.3.25f1 usa el versionado `android-open-jdk-17.0.18+8` [V API]. No sé si el Hub acepta el alias genérico para esta versión [NV]. Por eso el comando recomendado usa `-m android` + módulos hijos, que no depende de ese ID.
- **Destinos:** el SDK, el NDK y el JDK se instalan dentro del editor (`Editor/Data/PlaybackEngines/AndroidPlayer/{SDK,NDK,OpenJDK}`) [V API]. No hace falta definir `ANDROID_HOME`. Recomiendo no apuntar Unity al SDK de Android Studio, para no mezclar versiones [E].

## 4. IL2CPP para Android desde un host Windows ARM64

| Pieza de la cadena | Binario que entrega Unity/Google | Ejecución en Snapdragon X | Marca |
|---|---|---|---|
| Editor / il2cpp | ARM64 (editor nativo) | Nativo, aunque Unity reconoce partes x64 (Package Manager, Enlighten) | [V] / il2cpp.exe nativo [NV] |
| NDK r27c (clang/lld) | Solo `windows-x86_64`; Google no publica host Windows ARM64 ni siquiera en r30 | **Emulado (Prism)** | [V] developer.android.com/ndk |
| Gradle + OpenJDK 17.0.18+8 | `open-jdk-win-x64` | **Emulado** | [V] API |
| Build-tools 36 / platform-tools | Windows x64 | **Emulado** | [V] API |
| JDK ya instalado en la máquina (Temurin 17.0.19) | `os.arch = amd64` | Emulado | [V] local |
| NDK 27.1 ya instalado (Android Studio) | `prebuilt/windows-x86_64` | Emulado | [V] local |

- **Postura oficial:** Unity no prohíbe Android desde un host ARM64. Distribuye el módulo para ARM64 con los mismos binarios x64 y deja el resultado a la emulación ("might still work with x86 emulation") [V sysreq].
- **Evidencia pública de builds IL2CPP Android desde editor ARM64 en Snapdragon X:** no encontré ninguna en Unity Discussions, Issue Tracker ni búsqueda abierta, ni de éxito ni de fallo [NV]. No hay issues abiertas con ese título en el Issue Tracker [NV, el buscador no permite filtrar].
- **Prism:** es el emulador de Windows 11 24H2+ (esta máquina tiene la 25H2), "optimizado y afinado específicamente para Snapdragon". Algunas funciones de rendimiento requieren hardware de la serie Snapdragon X. Solo emula modo usuario [V MS Learn]. Clang, Java y Gradle son modo usuario, así que en principio son compatibles [E].
- **Rendimiento:** un build IL2CPP con clang emulado será más lento que en un x64 nativo equivalente; calculo entre 1,3 y 2 veces más [E, sin cifras publicadas]. Si Gradle es el cuello de botella, se puede probar un JDK 17 ARM64 nativo: `winget` ofrece `Microsoft.OpenJDK.17` 17.0.20.1 en MSI `windows-aarch64`, mientras que Temurin 17 no tiene instalador arm64 [V local]. Usar un JDK que no es el de Unity va contra su recomendación por defecto [E]; solo como optimización tras la prueba mínima.
- **Requisitos Android que 6.3 LTS cumple:**
  - minSdk 26 > mínimo de Unity (API 25) [V docs 6.3].
  - target 36: la plataforma 36 viene en el módulo y Unity 6.3 permite apuntar a API 35 y 36 [V].
  - Páginas de 16 KB: Unity las soporta [V docs 6.3].
  - NDK r27c [V].
- **Conclusión:** hasta la prueba mínima del día 1, compilar IL2CPP Android en esta máquina está **sin verificar [NV]**.

## 5. Gráficos en el editor: Adreno X1-45, DX11/DX12/Vulkan, URP y Shader Graph

| API en el editor | Estado en Windows on Arm | Marca |
|---|---|---|
| DX12 | Soportado; predeterminado para proyectos Windows nuevos desde 6.1 | [V] What's new 6.1 |
| DX11 | Soportado (DX10/11/12 en requisitos); se fuerza con `-force-d3d11` | [V] sysreq; uso del flag en Discussions |
| Vulkan | **No soportado** por Unity en Windows on Arm | [V] sysreq |

Problemas conocidos en Snapdragon X:

| Problema | Versiones | Estado | Workaround | Relevancia para URP 2D | Marca |
|---|---|---|---|---|---|
| UUM-92264: artefactos de textura en Play Mode con URP en ARM Windows (Snapdragon X Elite X1E78100); categoría DirectX11 | 6000.0.26f1-31f1; ports 6.3/6.4/6.5/6.6 | Cerrado 28-05-2026: "no pudimos reproducir… sospechamos del driver de la GPU" | Actualizar el driver; Render Graph Compatibility Mode (en 6.3 está deprecado y oculto tras el define `URP_COMPATIBILITY_MODE`) | Media: era una muestra 3D, pero 2D también usa Render Graph | [V] issuetracker 17523; notas 6000.3.0f1 |
| UUM-113477: "Generate Lighting" falla en Snapdragon X Elite + Adreno | 6000.0.50f1+ | Se corregirá en 6.7, **sin backport a 6.3/6.5** (20-08-2026) | — | Baja (2D sin bake) | [V] issuetracker 19749 |
| Reinicios o cuelgues completos del PC al usar Unity 6.x en Surface/Snapdragon X | 6000.0.33-63, 6000.2.7, 6000.3.0-3.2 (ARM64) | Sin solución oficial; staff sin hardware de prueba (06-01-2026) | Desactivar Windows › Sistema › Pantalla › Gráficos › "Automatic super resolution" (funcionó en 6.0.63, no en 6.3); actualizar drivers | Alta si ocurre | [V] discussions 1696485 |
| Crash al entrar en Play Mode en proyectos Learn (Adreno X1-85), 6.3 y 6.5 | jul-2026 | Abierto, sin staff | Mismo consejo de ASR y drivers | Media | [V] discussions 1730919 |
| Unity Version Control (`com.unity.collab-proxy`) no carga DLL x64 en ARM Windows | 6.0/6.2/6.3 | Won't fix (12-2025) | Usar Git + LFS (ya decidido) | Nula | [V] issuetracker 3546 |
| Shader Graph específico de Snapdragon X | — | No encontré informes | — | — | [NV] |

- **Driver local:** Adreno 31.0.137.0 del 01-07-2026 [V local]. Qualcomm publicó el paquete 2026.08.2 el 03-09-2026, con el driver 31.0.170.0 para Snapdragon X de primera generación [V, fuente secundaria comss.ru]. Recomiendo actualizarlo por Windows Update/OEM o por el Qualcomm Software Center antes de la prueba mínima. Es acción del dueño porque modifica el sistema.
- **Corrección Adreno en 6000.3.14f1:** arregló varios fallos de Vulkan en Adreno **Android** (UV incorrectos en Adreno 7xx, crash con atributos de vértice no usados) [V notas]. Afecta al juego en dispositivos, no al editor.

## 6. Alternativa: editor x64 bajo la emulación Prism

| Aspecto | Editor ARM64 nativo | Editor x64 emulado | Marca |
|---|---|---|---|
| Disponibilidad 6000.3.25f1 | `UnitySetupArm64-6000.3.25f1.exe` | `UnitySetup64-6000.3.25f1.exe` | [V] API |
| ¿Funciona? | Sí (oficial) | Sí: hay usuarios en Snapdragon X Elite con 6000.0.26f1 x64 emulado, y Unity registra "x64 Editor on an arm64 device" desde 6000.3.4f1 | [V] |
| Rendimiento del editor | Mejor | Peor (todo emulado, incluido el compilador de shaders y Burst) | [E] sin cifras de Unity |
| Plugins de editor con DLL x64 (p. ej. Firebase en Play Mode) | Pueden no cargar ("expected ARM64 architecture, but was x64") | Cargan | [V] precedente UUM-113503; por SDK [NV] |
| Cadena Android (NDK, JDK, Gradle) | Emulada igualmente | Emulada | [V] |
| Vulkan en editor | No | No (Unity no soporta Vulkan en WoA) | [V] |
| Instalar las dos de la misma versión lado a lado | — | La Unity CLI admite `--architecture` para desambiguar instalaciones lado a lado; con el Hub CLI en macOS falló (HUBX-1477, won't fix) | [V]/Windows [NV] |

## 7. Versión exacta recomendada hoy

| Línea | Versión | Changeset | Fecha | Estado | Fin de soporte | ARM64 | Marca |
|---|---|---|---|---|---|---|---|
| **6.3 LTS (recomendada)** | **6000.3.25f1** | **e1dba0a9aba4** | 24-09-2026 | LTS | dic-2027 (Enterprise/Industry +1 año) | Sí | [V] API + unity.com/releases/unity-6/support |
| 6.6 (más reciente con ARM64) | 6000.6.4f1 | 12bfff696524 | 01-10-2026 | Supported Update (`recommended=true` en el Hub) | hasta la siguiente versión | Sí | [V] API |
| 6.0 LTS | 6000.0.84f1 | 78ab6fc243d5 | 16-09-2026 | LTS | **16-10-2026** | Sí | [V] endoflife.date (secundaria) + página de soporte |
| 6.7 | 6000.7.0b3 | ad717268ad45 | 05-10-2026 | Beta | — | Sí | [V] API |

- **Deep link del Hub:** `unityhub://6000.3.25f1/e1dba0a9aba4` [V API].
- **Imágenes de CI disponibles:** game-ci ya publica `unityci/editor:ubuntu-6000.3.25f1-android-3` (y `-3.2.2`) y `windows-6000.3.25f1-android-3`, fechadas el 24-09-2026 [V Docker Hub].
- **Notas útiles de 6000.3.25f1:** Gradle 9.1.0 → 9.3.1 y una corrección de un deadlock de compilación de shaders con Graphics Jobs [V notas].
- **Fijar la versión en el repo:** `ProjectSettings/ProjectVersion.txt` (`m_EditorVersion: 6000.3.25f1`, `m_EditorVersionWithRevision: 6000.3.25f1 (e1dba0a9aba4)`). Actualizar a parches 6000.3.x más nuevos solo con un PR dedicado [E].

## 8. Comandos de instalación headless (Windows ARM64)

> Ninguno de estos comandos se ejecutó; son la propuesta para el paso de instalación. Hay dos pasos que **solo puede hacer el dueño**:
> - Iniciar sesión con su Unity ID.
> - Aceptar la licencia de Google del Android SDK/NDK, que es lo que implica `--accept-eula` según el manifiesto del módulo `android-sdk-ndk-tools`.

**8.1 Hub y CLI**

```powershell
# Hub 3.22.2 ARM64 (winget elige el MSIX arm64 en esta máquina) [V]
winget install --id Unity.UnityHub --source winget
# Unity CLI: viene con el Hub ≥3.21 al primer arranque [V]; o por separado (MSIX arm64) [V]:
winget install --id Unity.CLI --source winget
unity --version          # esperado: 1.0.0-beta.12
```

**8.2 Opción recomendada: Unity CLI** [V sintaxis en docs.unity.com/en-us/unity-cli y Unity-Technologies/skills]

```powershell
unity auth login                                   # navegador: inicia sesión el dueño
unity license activate --personal --accept-eula    # licencia Personal (acepta los términos de Personal)
unity install 6000.3.25f1 --changeset e1dba0a9aba4 --module android --child-modules --yes --accept-eula
unity editors --installed --format json            # comprobar version 6000.3.25f1 y arquitectura arm64
unity modules list 6000.3.25f1 --architecture arm64 --format json
```

- **Sin `--architecture`:** en Windows ARM el valor por defecto es arm64 [V HUBX-1596]. Para forzar x64 (plan B) existiría `-a x86_64`, pero en Windows está [NV].
- **`--child-modules`:** instala OpenJDK `android-open-jdk-17.0.18+8` y `android-sdk-ndk-tools` con sus hijos (NDK r27c, CMake 3.22.1, build-tools 36.0.0, platform-tools 36.0.0, plataformas 34/35/36/37.0, cmdline-tools 16.0) [V API].

**8.3 Alternativa: Hub CLI clásico** [V docs.unity.com/en-us/hub/use-hub-cli; ruta para el instalador NSIS `UnityHubSetup-arm64.exe`]

```powershell
& "C:\Program Files\Unity Hub\Unity Hub.exe" -- --headless install `
  --version 6000.3.25f1 --changeset e1dba0a9aba4 `
  --module android --childModules
# Variante con IDs explícitos (los del manifiesto 6000.3.25f1) [V API]:
#   --module android android-open-jdk-17.0.18+8 android-sdk-ndk-tools --childModules
# Añadir módulos a un editor ya instalado:
& "C:\Program Files\Unity Hub\Unity Hub.exe" -- --headless install-modules --version 6000.3.25f1 -m android --childModules
& "C:\Program Files\Unity Hub\Unity Hub.exe" -- --headless editors -i
```

- **No usar `--architecture arm64` con el Hub CLI en Windows:** la documentación lo marca "macOS only" [V]. Fue la causa del bug HUBX-1596, corregido en 3.17.0-beta.1 [V].
- **Ruta del `Unity Hub.exe` si el Hub se instala por MSIX (winget):** no verificada; puede no existir en `C:\Program Files\Unity Hub` [NV]. Con MSIX, usar la Unity CLI.

## 9. Licencia Personal: activación local y en CI

| Escenario | Cómo | Funciona con Personal | Marca |
|---|---|---|---|
| Activación en esta máquina | Iniciar sesión con Unity ID en el Hub (Settings › Licenses desde 3.20) o `unity auth login` + `unity license activate --personal --accept-eula` | Sí | [V] Manual 6.3 "ManagingYourUnityLicense"; Unity-Technologies/skills |
| `Unity.exe -batchmode -serial … -username … -password …` | Ruta de serial (Plus/Pro) | **No**: "The following procedures don't apply to Unity Personal" | [V] Manual 6.3 |
| Batchmode / `unity build` / `unity run` en local tras activar | Usa la licencia activa de la máquina | Sí | [V] receta "CI: activate a license, then build" de la Unity CLI; matriz de game-ci |
| Cuenta de servicio (`UNITY_SERVICE_ACCOUNT_ID/SECRET`) | Sirve para autenticarse | **No activa Personal**: "Unity's licensing backend does not accept service-account tokens for license activation" | [V] skills/auth-license-cloud.md |
| Activación manual offline (.alf → .ulf) | license.unity3d.com/manual | **Eliminada para Personal**: "Offline activation is available only for Enterprise and Industry seats" | [V] game-ci/cli PR #246 |
| game-ci con `UNITY_LICENSE` (.ulf) | Lo que dice todavía game.ci/docs/github/activation y el GDD (08 §12.1) | Ya no se puede obtener un .ulf para una licencia gratuita | [V] PR #246 vs docs game.ci |
| game-ci estrategia `personal` (`UNITY_EMAIL` + `UNITY_PASSWORD`, sin `UNITY_LICENSE` ni `UNITY_SERIAL`) | `Unity.Licensing.Client --activate-all --include-personal`; devuelve el puesto al salir (trap EXIT) | Sí: la matriz mide "6000.6.0f1 · personal ✅" (Licensing Client 1.18.3) | [V] PR #246 (fusionado 05-09-2026), PR #265 |
| `game-ci/unity-builder` | v6.0.0 (10-09-2026) es una capa fina sobre game-ci/cli (latest v0.1.72, 03-10-2026); v4.8.2/v5.0.1 añaden el arreglo de memoria compartida para 6.6+ | Personal por email/contraseña: en v6 [E, por herencia de game-ci/cli]; en v4/v5 [NV] | [V] releases |

Condiciones de la estrategia `personal` en CI [V PR #246]:

- **Sin 2FA:** si la cuenta pide 2FA o verificación de dispositivo, el login falla.
- **Flags no documentados:** `--include-personal` y `--return-ulf` pueden cambiar sin aviso.
- **Cuenta dedicada:** game-ci recomienda usar una cuenta Unity solo para CI, porque compartir un puesto Personal con credenciales de CI es "zona gris" de los términos.
- **Devolver siempre el puesto:** un puesto no devuelto "rompe todas las ejecuciones posteriores de la cuenta".
- **Límite de activaciones:** cuántas máquinas admite a la vez una licencia Personal [NV].
- **Elegibilidad del plan:** Unity Personal se aplica con menos de 200.000 USD de ingresos o financiación en los últimos 12 meses [V unity.com/products].

---

## Recomendación para este equipo y esta máquina

Los programadores son agentes (Claude Code y subagentes). La instalación tiene que ser scriptable, verificable por comandos y con los pasos humanos reducidos al mínimo. Los pasos humanos son: login, aceptar licencias/EULA, secretos de CI y actualización de drivers.

1. **Motor y versión: Unity 6.3 LTS 6000.3.25f1 (e1dba0a9aba4), editor ARM64 nativo.** No 6.6 (no es LTS) ni 6.0 (fin de soporte el 16-10-2026). Fijarla en `ProjectVersion.txt` y en el CI [V].
2. **Hub 3.22.2 ARM64 + Unity CLI 1.0.0-beta.12** (ambos por winget, MSIX arm64) [V]. Los agentes operan Unity con `unity …` (`install`, `install-modules`, `editors`, `build`, `run`, `test`, `license status`), con `--format json`/`--non-interactive`. El Hub queda para el dueño. La CLI sigue en beta: fijar la versión y registrar `unity --version` en el log de setup [V].
3. **Pasos del dueño (no delegables):**
   - (a) Iniciar sesión con su Unity ID y activar Personal.
   - (b) Aceptar la licencia de Google del SDK/NDK (`--accept-eula`) o hacerlo desde el Hub.
   - (c) Actualizar el driver Adreno (31.0.137 → paquete 2026.08.2) y, si aparecen cuelgues, desactivar "Automatic super resolution" de Windows.
   - (d) Crear la cuenta Unity de CI sin 2FA y cargar `UNITY_EMAIL`/`UNITY_PASSWORD` como secrets.
4. **Instalación:** `unity install 6000.3.25f1 --changeset e1dba0a9aba4 --module android --child-modules --yes --accept-eula`, sin `--architecture`. Después, comprobar con `unity editors --installed --format json` y con la línea de arquitectura del Editor.log. No añadir `ios` todavía (Codemagic) ni `visualstudio` (se usa Cursor). No definir `ANDROID_HOME` global para Unity.
5. **Prueba mínima obligatoria el día 1 (compuerta antes de escribir gameplay)**, con criterios medibles:

   | Prueba | Criterio de aceptación |
   |---|---|
   | Proyecto vacío URP 2D | Plantilla `com.unity.template.universal-2d` [V skill unity-cli] |
   | Build Android | IL2CPP, solo ARM64, minSdk 26, target 36, AAB: termina 3 veces seguidas sin error |
   | Tiempos | Registrar el tiempo del build IL2CPP limpio e incremental; aceptable si el limpio < 15 min [E] |
   | Páginas de 16 KB | `zipalign -c -P 16 -v 4` sobre el APK extraído del AAB, sin fallos |
   | Dispositivo | El APK se instala con el `adb` de Unity y arranca |
   | Shader Graph | Shader de sprite + escena en Play Mode con DX12 y con `-force-d3d11` durante 30 min, sin artefactos ni cuelgues |

6. **Si el build Android IL2CPP falla en el editor ARM64:** pasar al plan B1 (editor x64 de la misma versión) **solo para builds**, sin abandonar el editor ARM64 para editar.
7. **CI:**
   - Actualizar el GDD (08 §12.1): pasar de `game-ci/unity-builder@v4` + `UNITY_LICENSE` a `game-ci/unity-builder@v6.0.0` con `cliVersion` fijado (p. ej. `v0.1.72`) y la estrategia `personal`.
   - Usar las imágenes `unityci/editor:ubuntu-6000.3.25f1-android-3` [V].
   - Los builds de release se hacen siempre en CI Linux x64, lo que además neutraliza el riesgo del host ARM64.
8. **Para el equipo de agentes (fuera del alcance estricto del tema A):**
   - Unity publica un plugin oficial para Claude Code (`/plugin marketplace add Unity-Technologies/unity-agent-plugin`, `/plugin install unity@unity-agent-plugin`) y skills (`unity-cli`) que manejan el editor vía `unity mcp`/`unity command` [V README]. Merece una evaluación aparte.
   - Instalarlo es decisión del dueño.

## Riesgos y plan B

| # | Riesgo | Prob. | Impacto | Señal temprana | Mitigación / Plan B | Marca |
|---|---|---|---|---|---|---|
| R1 | El build IL2CPP Android no funciona (o es demasiado lento) con NDK/JDK emulados desde el editor ARM64 | M [E] | A | Falla la prueba mínima del día 1 | **B1:** editor x64 6000.3.25f1 bajo Prism solo para builds. **B2:** builds solo en CI Linux (game-ci) y editor local para edición/Play Mode. **B3:** JDK 17 ARM64 nativo (Microsoft OpenJDK 17.0.20.1 aarch64) si el cuello es Gradle | [NV] / B1-B3 [V] disponibilidad |
| R2 | Cuelgues o reinicios del sistema con Unity 6.x en Snapdragon X | M | A | Reinicios en sesiones largas | Actualizar el driver; desactivar "Automatic super resolution"; probar `-force-d3d11`; si persiste, editor x64 | [V] informes de usuarios |
| R3 | Artefactos de URP/Render Graph en el editor (UUM-92264) | B-M | M | Texturas que parpadean al mover la cámara | Driver nuevo; `-force-d3d11`; el define `URP_COMPATIBILITY_MODE` solo como diagnóstico (está deprecado) | [V] |
| R4 | SDK con DLL x64 del lado del editor (Firebase en Play Mode, herramientas de MAX/EDM4U) que no cargan en el editor ARM64 | M [E] | M | "expected ARM64 architecture, but was x64" en la consola | Probar cada SDK al integrarlo; stubs/mocks en el editor (el núcleo AF.Core es C# puro); editor x64 para esas pruebas | [V] cláusula de sysreq + precedente UUM-113503; por SDK [NV] |
| R5 | El Hub/CLI instala la arquitectura equivocada o etiqueta mal la instalada | B (corregido) | B | `unity editors -i` muestra x64 | No pasar `-a`; verificar; reinstalar | [V] HUBX-1596, UUM-138991 |
| R6 | Personal en CI se rompe (Unity cambia flags no documentados, 2FA, puesto no devuelto) | M | M | Job falla en la activación | Cuenta CI dedicada sin 2FA; fijar `cliVersion`. Alternativas: Unity Build Automation (citada en research/06) o runner self-hosted con sesión del Hub activa (`skipActivation`/`providerStrategy=local-system` en v6) | [V] PR #246, action.yml v6 |
| R7 | Unity CLI en beta (1.0.0-beta.12) con cambios de flags | M | B | Un comando de los agentes deja de funcionar | Fijar la versión; el Hub CLI clásico como respaldo | [V] |
| R8 | RAM (15,6 GB) justa con editor + Gradle + clang emulados + Android Studio abierto | M [E] | B | Builds muy lentos o OOM de Gradle | Cerrar Android Studio durante los builds; limitar la memoria de Gradle | [E] |
| R9 | Unity deja de priorizar Windows on Arm (staff sin hardware de prueba; UVCS "won't fix") | B-M | M | Más issues "not reproducible" | Mantener operativo el plan B2 (CI Linux) desde la semana 1; Godot 4.x sigue como alternativa del GDD (soporte ARM64 de Godot en Windows [NV] aquí) | [V] señales |

## Fuentes

Fuentes primarias de Unity:
- API oficial de versiones de Unity (6000.3.25f1, 6000.6.4f1, 6000.0.0f1, 6000.0.23f1, listados LTS/últimas): https://services.api.unity.com/unity/editor/release/v1/releases?version=6000.3.25f1
- Notas de 6000.3.25f1: https://unity.com/releases/editor/whats-new/6000.3.25f1
- Notas de 6000.3.0f1 (lightmapper CPU vía emulación x64, `URP_COMPATIBILITY_MODE`): https://unity.com/releases/editor/whats-new/6000.3.0f1
- Notas de 6000.3.4f1 (log ARM64/x64): https://unity.com/releases/editor/whats-new/6000.3.4f1
- Soporte de Unity 6 (6.3 LTS hasta dic-2027; 6.0 hasta oct-2026): https://unity.com/releases/unity-6/support
- Calendario de soporte (secundaria): https://endoflife.date/unity

Requisitos y documentación del motor:
- Requisitos 6.3: https://docs.unity3d.com/6000.3/Documentation/Manual/system-requirements.html
- Requisitos 6.6: https://docs.unity3d.com/Manual/system-requirements.html
- Requisitos 6.4: https://docs.unity3d.com/6000.4/Documentation/Manual/system-requirements.html
- Requisitos 6.7 beta: https://docs.unity.com/en-us/engine/6000.7/manual/get-started/install-and-upgrade/getting-started-installing-unity/system-requirements
- Requisitos Android 6.3: https://docs.unity3d.com/6000.3/Documentation/Manual/android-requirements-and-compatibility.html
- What's new 6.1 (DX12 por defecto): https://docs.unity3d.com/6000.1/Documentation/Manual/WhatsNewUnity61.html
- Licencias por línea de comandos (Personal no aplica): https://docs.unity3d.com/6000.3/Documentation/Manual/ManagingYourUnityLicense.html
- Plan Personal (elegibilidad): https://unity.com/products

Unity Hub y Unity CLI:
- Notas del Hub (3.15.3 a 3.22.2): https://unity.com/unity-hub/release-notes
- Hub CLI: https://docs.unity.com/en-us/hub/use-hub-cli
- Referencia del Hub CLI (IDs de módulos): https://docs.unity.com/en-us/hub/hub-cli-reference
- Instalar el editor con el Hub: https://docs.unity.com/en-us/hub/add-editor
- Referencia de la Unity CLI: https://docs.unity.com/en-us/unity-cli/unity-cli-reference
- Notas de la Unity CLI: https://docs.unity.com/en-us/unity-cli/release-notes
- Uso de la Unity CLI: https://docs.unity.com/en-us/unity-cli/use-unity-cli
- Skill oficial `unity-cli`: https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/SKILL.md
- Licencias en la skill: https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/references/auth-license-cloud.md
- Instalación de editores en la skill: https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/references/editors-install.md
- Plugin oficial para agentes: https://github.com/Unity-Technologies/unity-agent-plugin

Unity Issue Tracker:
- HUBX-1596 (`-a arm64` en Windows ARM): https://issuetracker.unity.com/issues/4514
- UUM-138991 (GUI instalaba x64): https://issuetracker.unity.com/issues/3934
- UUM-113503 (UVCS en ARM): https://issuetracker.unity.com/issues/3546
- UUM-92264 (URP en ARM Windows): https://issuetracker.unity.com/issues/17523
- UUM-113477 (lighting en Adreno): https://issuetracker.unity.com/issues/19749

Unity Discussions:
- Beta Windows on Arm, ene-2024: https://discussions.unity.com/t/native-windows-on-arm-beta-support-in-the-unity-editor-is-now-available/938916
- ¿Unity 6 corre en ARM64?, feb-2025: https://discussions.unity.com/t/is-unity6-really-run-on-windowsarm64/1606124
- Soporte ARM64, nov-2025: https://discussions.unity.com/t/does-unity-really-support-arm64-based-processers/1695327
- Cuelgues en portátiles ARM, nov-2025 a ene-2026: https://discussions.unity.com/t/unity-6-x-crashes-entire-windows-arm-laptop/1696485
- Hub para Windows ARM, may-2026: https://discussions.unity.com/t/unity-hub-for-windows-arm/1721232
- Crash de Learn en Snapdragon, jul-2026: https://discussions.unity.com/t/unity-learn-essentials-crashes-on-snapdragon-arm-laptop/1730919
- Texturas URP en Snapdragon, nov-2024: https://discussions.unity.com/t/weird-texture-errors-due-to-urp/1561279

Android, Microsoft y Qualcomm:
- NDK (solo host Windows x86_64): https://developer.android.com/ndk/downloads
- Host tags del NDK: https://developer.android.com/ndk/guides/other_build_systems
- Prism (Microsoft Learn): https://learn.microsoft.com/en-us/windows/arm/apps-on-arm-x86-emulation
- Driver Adreno 2026.08.2 (secundaria): https://www.comss.ru/page.php?id=21921

game-ci:
- PR #246 (estrategia `personal`): https://github.com/game-ci/cli/pull/246
- PR #265 (matriz de licencias): https://github.com/game-ci/cli/pull/265
- PR #270: https://github.com/game-ci/cli/pull/270
- PR #290: https://github.com/game-ci/cli/pull/290
- Releases de unity-builder: https://github.com/game-ci/unity-builder/releases
- action.yml de v6.0.0: https://raw.githubusercontent.com/game-ci/unity-builder/v6.0.0/action.yml
- Activación en game.ci: https://game.ci/docs/github/activation
- Imágenes Docker: https://hub.docker.com/r/unityci/editor/tags?name=6000.3.25f1

Comandos locales de solo lectura (06-10-2026):
- `winget show --id Unity.UnityHub`
- `winget show --id Unity.CLI`
- `winget show --id Unity.Unity.6000 --versions`
- `winget show --id Microsoft.OpenJDK.17 --architecture arm64`
- `java -XshowSettings:properties -version`
- `Get-CimInstance Win32_VideoController`
- Registro `HKLM\…\CurrentVersion`
