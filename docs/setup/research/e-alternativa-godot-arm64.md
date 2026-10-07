# Tema E: Godot 4.x como alternativa en Windows 11 ARM64 (Snapdragon X Plus) para Aquazonia

Fecha de la investigación: 2026-10-06. Autoría: agente de investigación (Claude Code), a petición del dueño.
Alcance: solo lectura e investigación. No se instaló nada ni se modificó el sistema; las comprobaciones locales se hicieron con comandos de solo lectura (`winget show/search`, `Get-AppxPackage`, `java -XshowSettings`, lectura de cabeceras PE).

Leyenda: **[V]** verificada con fuente (URL en la línea o en "Fuentes"); **[V-local]** comprobada hoy en esta máquina con un comando de solo lectura; **[E]** estimación razonada; **[NV]** no verificable con las fuentes disponibles.

---

## Respuesta corta

- **Versión:** la última estable es **Godot 4.7.2**, publicada el 18-08-2026. Godot 4.8 va por la **dev7** del 29-09-2026, que congela funciones. Las dos variantes del editor, estándar y .NET (C#), tienen **build nativo oficial para Windows arm64**, y winget ofrece instalador arm64 de ambas (`GodotEngine.GodotEngine` y `GodotEngine.GodotEngine.Mono`, 4.7.2) [V][V-local].
- **C#/.NET en ARM64:** el editor .NET arm64 necesita el **.NET SDK arm64**, porque "las binarias .NET no funcionan bajo emulación". Godot 4.7 pide .NET 8 o superior, y **.NET 9 o superior para exportar a Android**. C# en Android sigue siendo **experimental**. El soporte de .NET 9 **termina el 10-11-2026**. Godot 4.8 sube el mínimo a **net10.0** (PR fusionado el 26-09-2026, todavía sin versión estable) [V].
- **Exportación Android desde este PC:** es viable con lo que ya está instalado (JDK 17, Android SDK en `%LOCALAPPDATA%\Android\Sdk`, build-tools 36.1.0 y android-36). La plantilla de 4.7.2 trae `targetSdk 36`, `compileSdk 36`, `minSdk 24` (se puede subir a 26), AGP 8.6.1 y Gradle 8.11.1. Soporta páginas de 16 KB desde Godot 4.5. Las herramientas de Android (aapt2, zipalign, adb) y el JDK actual son **x64/x86 y corren bajo emulación Prism** [V][V-local].
- **SDKs de monetización y servicios:**
  - Bien cubiertos: **AdMob + UMP** (Poing v5.1.0, 13-09-2026, con adaptadores de mediación AppLovin, Meta, Mintegral y Unity Ads, y paridad C#), **Play Billing** (plugin 3.3.0 con PBL 9.1.0; 3.2.0 con PBL 8.3.0) y **Play Games Saved Games** (3.4.0) [V].
  - Débiles: el plugin oficial de **AppLovin MAX para Godot no se actualiza desde el 24-04-2025**. **Firebase** solo tiene Analytics y Crashlytics, a través de un plugin pequeño, y le faltan Remote Config y Functions nativos. **Play Asset Delivery** solo funciona en modo *install-time* [V].
- **Shader del líquido:** los shaders `canvas_item` de 4.7 tienen `TIME`, `hint_screen_texture` y **uniforms por instancia** (hasta 16, sin texturas). Para 2D y gama baja se recomienda el renderer **Compatibility** (OpenGL). Si llega a 60 fps en el dispositivo de referencia es **[NV] hasta prototipar**.
- **Tamaño:** el motor ocupa unos **20-35 MB de descarga arm64** antes de arte [E]. El tope de 80 MB del AAB es alcanzable con las plantillas oficiales [E].
- **Riesgo propio de esta máquina:** la ruta OpenGL de Windows ARM (capa *OpenGLOn12*, instalada aquí en la versión 1.2608.3.0) tiene historial de cierres inesperados del editor con el renderer Compatibility (issue #106853, abierto). Se mitiga forzando ANGLE [V][V-local].
- **Unity en ARM64:** **sí tiene soporte oficial.** Unity 6000.3.25f1 (línea 6.3 LTS, 24-09-2026) y 6000.6.4f1 publican editor Windows ARM64 **y módulo Android Build Support para ARM64**. Hub 3.18.2 corrigió en junio de 2026 la instalación en ARM64, y hay **plugin oficial de Unity para Claude Code** desde el 09-09-2026 [V]. No encontré ningún bloqueo documentado para compilar Android desde ARM64. Los riesgos que quedan son menores: IL2CPP compila C++ con NDK x64 emulado, y hay DLLs x64 de terceros en el editor [E].
- **Veredicto:** **Unity 6 LTS sigue siendo el motor principal; la máquina ARM64 no justifica cambiar.** Godot queda como plan B documentado y **no conviene instalarlo ahora**. Si se activa, la ruta sería Godot 4.8 con GDScript (o C# net10 tras una prueba corta), AdMob como mediador en lugar de MAX, y GameAnalytics o un plugin propio para Remote Config.

---

## 1. Editor Godot 4.x nativo en Windows ARM64 (con y sin C#/.NET)

### 1.1 Versiones y binarios

| Dato | Valor | Estado |
|---|---|---|
| Última estable | **4.7.2-stable**, 18-08-2026 (4.7 salió el 18-06-2026; 4.7.1 el 14-07-2026) | [V] godotengine.org/download/archive |
| Siguiente | **4.8-dev7**, 29-09-2026, con congelación de funciones; la beta viene después | [V] godotengine.org/article/dev-snapshot-godot-4-8-dev-7 |
| 4.8 estable | No anunciada. Por la cadencia, noviembre o diciembre de 2026 | [E] |
| Editor estándar arm64 | `Godot_v4.7.2-stable_windows_arm64.exe.zip`, 84.263.370 bytes | [V] GitHub release 4.7.2-stable (API) |
| Editor .NET arm64 | `Godot_v4.7.2-stable_mono_windows_arm64.zip`, 114.880.839 bytes | [V] mismo origen |
| Plantillas de exportación | estándar `.tpz` 1.281.349.702 bytes; .NET `.tpz` 1.202.598.411 bytes | [V] mismo origen |
| winget | `GodotEngine.GodotEngine` 4.7.2 y `GodotEngine.GodotEngine.Mono` 4.7.2, ambos con instalador **arm64** (zip portable) | [V-local] `winget show --architecture arm64` |
| .NET SDK arm64 en winget | `Microsoft.DotNet.SDK.9` 9.0.318 (win-arm64) y `Microsoft.DotNet.SDK.10` 10.0.401 (win-arm64) | [V-local] |
| Desde cuándo hay editor ARM oficial | Lo confirma el cierre del issue #91021 (02-11-2024): "veo el editor Windows ARM en la página de descargas" | [V] github.com/godotengine/godot/issues/91021 |

### 1.2 Requisitos .NET (C#)

| Requisito | Detalle | Estado |
|---|---|---|
| Arquitectura | "The .NET binaries run only on Windows systems with the matching architecture, running under emulation is not supported". En este PC hace falta el **editor .NET arm64 y el .NET SDK arm64** | [V] godotengine.org/download/windows |
| Versión .NET | "Godot 4.5 requires .NET 8 or later, but exporting to Android requires .NET 9 or later" (texto vigente en la doc 4.7) | [V] docs c_sharp_basics |
| Estado en Android/iOS | "as of Godot 4.2… experimental and some limitations apply" | [V] mismo doc |
| Ciclo de vida .NET | .NET 8 LTS y .NET 9 STS terminan soporte el **10-11-2026**; .NET 10 LTS llega hasta el 14-11-2028 | [V] dotnet.microsoft.com support policy |
| Plantilla Android .NET | 4.7 usa `net9.0` para Android. El issue #118989 pidió pasar a una LTS y lo cerró el **PR #123738 "Upgrade packages and minimum TFM required to net10.0"**, fusionado el 26-09-2026 con hito **4.8** | [V] GitHub |
| Limitaciones C# en Android | runtime Mono *linux-bionic*; solo arm64/x64; "Android bindings are not available, so some APIs (such as SSL) will crash" (artículo de enero de 2024) | [V] artículo de 2024; si sigue vigente en 4.7: [NV]. En 2025 un mantenedor confirmó que HTTPS funcionaba con .NET 9 (issue #107992) [V] |
| iOS con C# | NativeAOT, exportación solo desde macOS (Codemagic lo cubre) | [V] artículo de 2024 |
| Web con C# | no soportado (no aplica a Aquazonia) | [V] |

### 1.3 Problemas conocidos de Godot en Windows ARM / Adreno

| Issue | Estado | Relevancia para este PC | Mitigación |
|---|---|---|---|
| #106853 "Windows ARM64 OpenGL/Compatibility renderer crashes (OpenGLOn12 regression)" | **abierto** (abierto el 26-05-2025; último comentario el 17-10-2025: el fallo está en el *Compatibility Pack* de Microsoft) | **Alta.** Aquí está instalado `Microsoft.D3DMappingLayers 1.2608.3.0` [V-local]. Si esta versión trae el fallo: [NV] | Forzar ANGLE: `--rendering-driver opengl3_angle` (lo indica un mantenedor) o el ajuste de proyecto `rendering/gl_compatibility/driver.windows = opengl3_angle` [V] |
| Lógica de arranque en ARM | En ARM64 Godot activa siempre el *fallback* a ANGLE, pero solo cambia a ANGLE si el WGL nativo es menor que 3.3 o si el dispositivo está en `force_angle_on_devices`. Con OpenGLOn12 instalado, WGL informa 4.x y **usa OpenGLOn12** | Explica por qué este PC puede caer en el fallo anterior | Lo mismo [V] código `display_server_windows.cpp` 4.7.2 |
| #111712 "Windows Arm64 v4.5.1 doesn't register keystrokes" (Snapdragon X Elite) | abierto, sin reproducir en Linux | Baja: juego táctil 2D, y es de 4.5.1 | Probar en 4.7.2 [E] |
| #118806 cierre de 4.7-dev5 en Snapdragon X Plus al abrir un proyecto Compatibility de 4.6.1 | cerrado por falta de MRP (25-05-2026) | Indica que hay usuarios con Snapdragon X Plus usando 4.6/4.7 en Compatibility | Ninguna [V] |
| #111495 Adreno 618 con D3D12 no arranca | abierto | Baja: otra GPU, más antigua (7c Gen 2) | Ninguna [V] |
| Adreno X1-45 en concreto | sin reportes encontrados | No hay datos | [NV] |

Los renderers Forward+ y Mobile del editor usan en Windows `vulkan` por defecto, con *fallback* a `d3d12` (`rendering/rendering_device/driver.windows`) [V] `main/main.cpp` 4.7.2. Para un juego 2D de gama baja, el proyecto se crearía con **Compatibility**.

---

## 2. Exportación a Android desde host Windows ARM64

### 2.1 Requisitos de Godot 4.7.2 frente a esta máquina

| Componente | Lo que pide Godot 4.7.2 | En esta máquina (06-10-2026) | Comentario |
|---|---|---|---|
| JDK | "OpenJDK 17", "Higher versions… also supported" [V doc] | Temurin 17.0.19 **x64** (`os.arch = amd64`) bajo Prism [V-local] | Funciona emulado. Opcional: **Microsoft OpenJDK 17 aarch64** (`Microsoft.OpenJDK.17` 17.0.20.1, MSI windows-aarch64 en winget) [V-local] daría Gradle nativo [E: más rápido]. Temurin no tiene instalador arm64 en winget [V-local] |
| Ruta del SDK | Ajuste del editor `export/android/android_sdk_path`: usa `ANDROID_HOME` y, si no existe, `%LOCALAPPDATA%/Android/Sdk` [V código `export.cpp`] | SDK en esa ruta; `ANDROID_HOME` sin definir | Godot lo encuentra sin tocar nada. `java_sdk_path` sale por defecto de `JAVA_HOME` [V código] |
| Build-tools | Plantilla: `buildTools '36.1.0'` [V `config.gradle`]; la doc dice 35.0.1 [V doc, desfasada] | 35.0.0 / 36.0.0 / **36.1.0** / 37.0.0 | OK |
| Platform | `compileSdk 36` [V]; la doc dice "Android 35" | android-36 y android-36.1 | OK. AGP puede descargar el 35 si algo lo pide [E] |
| Platform-tools | 35.0.0 o superior [V doc] | instalado (adb x86 fuera del PATH) [V-local] | OK |
| Command-line tools | "latest" [V doc] | no figura en el inventario | [NV] si hace falta para la exportación con Gradle; instalarlo desde el SDK Manager solo si Godot lo pide |
| NDK / CMake | Doc: NDK r28b (28.1.13356709) y CMake 3.10.2 [V doc]; plantilla 4.7.2: `ndkVersion '29.0.14206865'` [V] | NDK 27.1.12297006 | **Solo hacen falta para compilar el motor o plantillas propias**; con las plantillas oficiales no se compila C++ [E] |
| Gradle / AGP | Wrapper **Gradle 8.11.1**, **AGP 8.6.1**, Kotlin 2.1.21, Java 17 [V] | se descarga solo | Primera exportación lenta [E] |
| Binarios del SDK | n/a | `aapt2.exe` x64, `zipalign.exe` y `adb.exe` x86, `clang.exe` del NDK x64 (`windows-x86_64`) [V-local, cabecera PE] | Todo corre bajo Prism; Windows 11 24H2 emula x86/x64 en modo usuario [V learn.microsoft.com] |
| Target / min SDK | `DEFAULT_TARGET_SDK_VERSION = 36`, `DEFAULT_MIN_SDK_VERSION = 24`; ambos se pueden cambiar en el preset [V `export_plugin.cpp`] | n/a | Fijar `min_sdk = 26` en el preset (GDD) |
| 16 KB | PR #106358 "Add support for 16 KB page sizes, update to NDK r28b", fusionado el 15-05-2025, hito 4.5; las librerías nativas van sin comprimir [V] | n/a | Cumple. Para .NET con Gradle, el issue #110262 se cerró en 4.5 [V] |
| AAB | "All new apps… after August 2021 must be an AAB", requiere *Use Gradle Build* [V doc] | n/a | Exportar AAB con Gradle |
| Keystore de debug | El editor lo **genera solo** con `keytool` del JDK [V código `export_plugin.cpp`] | keytool x64 disponible | Sin acción |
| Keystore de release | `keytool -genkeypair …`; "The keystore password and the key password currently have to be the same" [V doc] | n/a | Guardar fuera del repo y en secretos de CI |
| CLI y CI | `godot --headless --export-release "Android" out.aab` y `--install-android-build-template` [V doc]. Imagen `abarichello/godot-ci` con etiqueta 4.7.2-stable, publicada el 18-08-2026 (1.136★) [V] | n/a | CI en Linux x64 sin ARM de por medio |

### 2.2 Lectura

- **Godot no compila C++ al exportar con las plantillas oficiales:** empaqueta el PCK y ejecuta Gradle. Es una ventaja real en un host ARM64 donde el NDK es x64 emulado [E]. Unity, con IL2CPP, sí compila C++ con el NDK en cada build de release [E].
- **Los cuellos de botella esperables son la primera ejecución de Gradle y aapt2 bajo emulación** [E]. Con 15,6 GB de RAM y Gradle 8.11 no hay problema de memoria [E].
- **Cambiar a un JDK arm64 nativo es opcional** y solo afecta al rendimiento. AGP 8.6.1 no exige las extensiones de CPU que pide aapt2 desde AGP 8.12, así que la plantilla de 4.7.2 no se ve afectada [E].

---

## 3. Ecosistema de SDKs en Godot 4.x (estado a octubre de 2026)

| Necesidad (GDD) | Opción en Godot | Última versión / fecha | Señales de mantenimiento | Calidad / riesgo |
|---|---|---|---|---|
| **AdMob + UMP** | `poingstudios/godot-admob-plugin` | **v5.1.0, 13-09-2026**; nightly 5.2.0 del 01-10-2026. GMA Android 1.4.0 (*Next-Gen*), iOS 13.9.0; `compileSdk 35`; Godot 4.4 o superior, "testing through 4.7.2" [V] | 631★, último push el 01-10-2026 [V-local `gh api`] | **Alta** [E]. UMP con *Privacy Options Form* (regulación de EE. UU.), revenue por impresión (`on_ad_paid`), anuncios simulados en el editor, **paridad C#** [V] |
| **Mediación** (GDD: MAX con AdMob, Unity Ads, Meta, Mintegral) | AdMob como **mediador** con adaptadores Poing: `applovin`, `meta`, `mintegral`, `unity_ads`, `ironsource`, `pangle`, `moloco`, `vungle`… (17 carpetas en `platforms/android/src/mediation`) [V-local `gh api`] | v5.0.0 (21-07-2026) añadió "Full Mediation Support (17+ Networks)" [V] | Igual que arriba | **Media-alta** [E]. El GDD dice "sin mediación con bidding" y eso **está desfasado**. Que cada adaptador soporte bidding a través del plugin: [NV] |
| **AppLovin MAX** | `AppLovin/AppLovin-MAX-Godot` (oficial) | **1.2.0 del 24-04-2025**; último push el 16-05-2025 [V] | 9 issues abiertos: iOS falla con Godot 4.4/4.5 (#60, #61, #66) y falta documentación C# (#59) [V] | **Baja.** Unos 17 meses sin publicar nada. **No usar** [E] |
| **Play Billing 8+** | `godot-sdk-integrations/godot-google-play-billing` | **3.3.0, 27-07-2026 (PBL 9.1.0)**; 3.2.0, 15-03-2026 (PBL 8.3.0); Godot 4.2 o superior [V] | 264★, push el 25-09-2026 [V-local] | **Alta** [E]. Play exige PBL 8 o superior desde el 31-08-2026, con prórroga posible hasta el 01-11-2026; PBL 9.1.0 salió el 18-06-2026 [V] |
| **Firebase Analytics + Crashlytics** | `godot-x/firebase` (Godotx Firebase) | **3.1.0, 22-07-2026**; Godot 4.7; firebase-analytics 23.2.0, crashlytics 20.1.0 (+ndk), messaging 25.1.1; minSdk 24; Gradle obligatorio y hay que editar `build.gradle` [V] | 42★, 4 issues [V-local] | **Media** [E]: funciona, pero depende de muy pocas personas |
| **Remote Config** | No está en Godotx [V]. Alternativas: `GameAnalytics/GA-SDK-GODOT` **3.1.2 (06-10-2026)**, oficial de GameAnalytics, con Remote Configs y A/B según la investigación del GDD [V release; la función, según research/06]; o un plugin Kotlin propio sobre el SDK de Firebase RC [E] | | | **Media.** Romper con "Firebase para todo" o mantener un plugin propio, cosa que los agentes pueden hacer [E]. Godot 4.7 "permite implementar interfaces Java desde GDScript", lo que simplifica el código de enlace [V] |
| **Cloud Functions** | Llamadas HTTPS (`HTTPRequest`) o `GodotNuts/GodotFirebase` (SDK REST en GDScript: Auth, Firestore, Functions, Storage), 6.0.6 del 27-05-2026 [V] | | 686★, 29 issues [V-local] | **Media** [E]. Las 5 funciones del GDD son HTTPS, así que es viable [E] |
| **Play Games Saved Games** | `godot-sdk-integrations/godot-play-game-services` | **v3.4.0, 20-07-2026**; PGS v2 21.0.0; Godot 4.3 o superior; *snapshots* (Saved Games) con señal de borrado [V] | 279★ [V-local] | **Alta** [E] |
| **Play Asset Delivery** | Nativo: al exportar AAB, **todo el PCK va en un único asset pack *install-time*** (`assetPackInstallTime`) [V código 4.7.2 y PR #52527 de 2021]. *Fast-follow* y *on-demand*: **no hay soporte nativo** ni PR abierto que lo añada [V búsqueda en el repo] | | | **Baja para el plan del GDD** (arte de mundos 5-13 en *fast-follow*). Se necesitaría un módulo Gradle propio con la Play Asset Delivery Library [E]. En Unity, *Addressables for Android* cubre los 3 modos [V] |
| Leaderboards (GDD 1.2: Unity Leaderboards) | Leaderboards de Play Games, en el mismo plugin | | | Sustituto natural [E] |

**Conclusión del bloque:** con Godot se puede publicar un F2P con anuncios y compras en Play, pero el stack del GDD (MAX + Firebase completo + PAD *fast-follow*) **no se traslada tal cual**. Cambiaría a AdMob como mediador, GameAnalytics o un plugin propio para Remote Config, y PAD *install-time* o un desarrollo propio.

---

## 4. Shader 2D del líquido (`canvas_item`) y rendimiento en gama baja

| Aspecto | Estado en Godot 4.7 | Fuente |
|---|---|---|
| Built-ins útiles | `TIME`, `UV`, `SCREEN_UV`, `sampler2D … : hint_screen_texture`, modos `blend_*` y `unshaded` | [V] doc canvas_item_shader 4.7 |
| Uniforms por instancia | "available in both `canvas_item` (2D) and `spatial`", máximo práctico 16, "do not support textures or arrays" | [V] doc shading_language 4.7 |
| Renderer recomendado | Compatibility (OpenGL) para "older mobile devices"; para 2D "usually good enough" | [V] doc renderers 4.7 |
| Guía de Google sobre Godot | developer.android.com/games/engines/godot/godot-renderers (actualizada el 26-02-2026) **sigue describiendo GLES2/GLES3 de Godot 3** | [V]. No sirve como guía para 4.x |
| Regresiones | #100576: regresión de rendimiento 2D en Android en 4.4-dev4 (cerrado en enero de 2025) | [V]. Hay que fijar la versión y medir en cada actualización [E] |

Diseño propuesto si se activa el plan B [E]:
- Un `Sprite2D` o `ColorRect` por tubo (16 como máximo) con un shader de texto que dibuje las capas desde *instance uniforms*: colores en `vec4` y alturas empaquetadas en `vec4`, más una ola con `TIME`.
- El vertido se anima con un tween sobre los uniforms.
- **Evitar `hint_screen_texture`**, porque fuerza una copia de pantalla y en gama baja cuesta *fill-rate*.
- Partículas con `CPUParticles2D`: Compatibility no tiene *compute*.

La carga es pequeña (pocas decenas de quads), así que el límite será el *fill-rate* de la GPU de gama baja y no el motor [E]. **Que llegue a 60 fps en el dispositivo de referencia es [NV] hasta prototipar**, en Unity igual que en Godot. El disparador (a) del GDD sigue siendo el criterio correcto.

---

## 5. Tamaño de APK/AAB

| Dato | Valor | Estado |
|---|---|---|
| Prueba de un mantenedor con Godot 4.4 (demo `dodge_the_creeps`) | APK sin Gradle 25,8 MB; con Gradle 74,7 MB; con `compress_native_libraries` 26,9 MB | [V] issue #104137 (marzo de 2025) |
| Desde 4.5 | la opción de comprimir librerías nativas desaparece (requisito de 16 KB) | [V] PR #106358 |
| Librería Android 4.7.2 en Maven (`godot-4.7.2.stable.aar`, todas las ABIs) | 103.251.267 bytes | [V] repo1.maven.org (HEAD) |
| Motor arm64 en descarga desde Play (AAB, división por ABI, compresión de Play) | ~20-35 MB antes de arte | [E] |
| Coste extra del runtime .NET (Mono + BCL recortada) | ~+10-20 MB | [E, baja confianza] |
| Reducción con plantilla propia | `disable_3d=yes` "about 15%", perfiles de build, `optimize=size_extra`, LTO | [V] doc optimizing_for_size 4.7. Exige compilar el motor con SCons y el NDK (x64 emulado aquí; mejor en CI Linux) [E] |
| Tope ≤ 80 MB del GDD | alcanzable con plantillas oficiales y arte ASTC/ETC2 [E]. Sin PAD *fast-follow*, todo el arte de 1.0 cuenta en la instalación [E] | |
| Unity como referencia | no medido en este informe | [NV]. Ver el informe del tema Unity |

---

## 6. Factor "todos los programadores son agentes de IA"

| Aspecto | Unity 6 | Godot 4.7/4.8 |
|---|---|---|
| Integración oficial con Claude Code | **Plugin oficial de Unity para Claude Code** (09-09-2026): 29 skills, Unity CLI y control del editor vía MCP [V] | Solo MCP de comunidad (varios en 2026) [V por búsqueda; calidad: NV]. Poing incluye "AI Skills" en `addons/admob/skills` [V] |
| Formato de escenas | YAML con GUID/fileID: editable, pero frágil para agentes [E] | `.tscn`, `.tres` y `.gd` en texto legible, con diffs limpios [E] |
| Build sin GUI | batchmode, con activación de licencia en CI (game-ci) [E] | `--headless --export-release` sin licencias [V] |
| Corpus de entrenamiento | C#/Unity muy abundante [E] | GDScript 2.0 menos abundante; los agentes confunden a veces la sintaxis de Godot 3 y 4 [E] |
| Compartir el núcleo C# del GDD (AF.Core/AF.Solver) | directo [E] | Directo **solo con Godot .NET** (en Android, experimental). Con GDScript habría que portar las reglas y validarlas con vectores dorados JSON [E] |

---

## 7. Veredicto comparado para ESTE proyecto y ESTA máquina

### 7.1 Matriz de riesgo

P = probabilidad, I = impacto (B = bajo, M = medio, A = alto). Todas las valoraciones P×I son **[E]**; la evidencia que las respalda se marca en la columna correspondiente.

| # | Riesgo | Unity 6.3 LTS en ARM64 (P×I) | Godot 4.7/4.8 en ARM64 (P×I) | Evidencia clave |
|---|---|---|---|---|
| 1 | El editor no arranca o es inestable en Snapdragon X Plus / Adreno X1-45 | M×A | M×A | Unity: editor ARM64 oficial; Hub 3.18.2 corrigió la instalación (06-2026); reporte de cierre al cargar proyectos en 6.2 (11-2025) [V]. Godot: #106853 abierto, mitigable con ANGLE [V] |
| 2 | No se puede compilar Android desde el host ARM64 | B×A | B×A | Unity: módulo Android Build Support para Windows ARM64 en 6000.3.25f1 [V]. Godot: Gradle y JDK sin C++ [V/E] |
| 3 | Builds lentos (emulación) | M×M (IL2CPP + NDK x64) | B×B | NDK y aapt2 x64 en este PC [V-local] |
| 4 | Mediación del GDD (MAX) | B×M [E; SDK de MAX para Unity no verificado aquí] | A×M: MAX Godot abandonado. Hay que pasar a AdMob como mediador | [V] releases |
| 5 | Firebase completo (Analytics, Crashlytics, RC, Functions) | B×M. DLLs de escritorio x64 en el editor ARM64: [NV] | M×M: sin RC; plugin pequeño | [V] |
| 6 | Billing 8+ a tiempo | B×A | B×A | Plugin con PBL 9.1.0 [V] |
| 7 | Saved Games de Play Games | B×M [E] | B×M | Plugin 3.4.0 [V] |
| 8 | PAD *fast-follow* para mundos 5-13 | B×M | A×M | Unity: Addressables for Android [V]. Godot: solo *install-time* [V] |
| 9 | Compartir AF.Core/AF.Solver en C# | B×A | GDScript: A×M (portar). C#: M×A (experimental; .NET 9 termina el 10-11-2026; net10 solo en 4.8) | [V] |
| 10 | Líquido a 60 fps en gama baja | M×A [NV] | M×A [NV] | Prototipo obligatorio (disparador a) |
| 11 | AAB ≤ 80 MB | B×M | B×M | [E] |
| 12 | iOS después (Codemagic) | B×M | GDScript B×M; C# M×M (NativeAOT) | [V] artículo de 2024 |
| 13 | Productividad de los agentes | B×M | M×M | Plugin oficial de Unity [V] |
| 14 | Cambios de licencia o términos | M×M (disparador b del GDD) | B×B (MIT) | [E] |
| 15 | Coste de cambio respecto al GDD actual | n/a | A×A tras el mes 2 (presentación, UI y SDKs; con GDScript también el núcleo) | GDD §8 [E] |

### 7.2 Veredicto

1. **Hay que ser imparcial: Unity en Windows ARM64 está oficialmente soportado en 2026, incluido el módulo Android para ARM64**, y no encontré ningún issue abierto que bloquee compilar Android desde ARM64 [V]. Godot también funciona nativo en ARM64, pero tiene sus propios fallos de drivers en Windows ARM [V]. **La máquina ARM64, por sí sola, no es razón para cambiar de motor.**
2. **Godot gana en simplicidad del toolchain en este host:** no compila C++, el editor es un zip portable, exporta sin licencia y es MIT. **Pierde en el encaje con el stack del GDD:** MAX, Firebase RC, PAD *fast-follow* y C# compartido en Android.
3. **Recomendación: mantener Unity 6 LTS** (línea 6.3 LTS) como motor principal y **Godot como plan B documentado, sin instalarlo ahora.**
4. **Si Unity falla en ARM64, el primer escalón no es Godot.** Primero se compila Android en CI Linux x64 (GitHub Actions + game-ci) y se sigue editando en local [E]. Godot solo entra si fallan los disparadores del GDD (shader o licencia) o si el editor de Unity en ARM64 es inutilizable de forma persistente.

---

## Recomendación para este equipo y esta máquina

1. **No instalar Godot ni el .NET SDK por este tema.** Según el GDD, el arranque es Unity. Instalar Godot ahora suma unos 1,4 GB (editor más plantillas) y mantenimiento sin beneficio inmediato [E].
2. **Decisiones de arquitectura que abaratan el plan B sin coste para Unity** [E]:
   - Que **AF.Core/AF.Solver apunten a `netstandard2.1`**, que compila en Unity y en cualquier .NET 8/9/10 de Godot. Mantener los **vectores dorados JSON** (`af-level/1`) como pruebas de paridad, por si un día hay que portar las reglas a GDScript.
   - Encapsular cada SDK tras una interfaz (`IAds`, `IAnalytics`, `IRemoteConfig`, `IStore`, `ICloudSave`) con implementación simulada en el editor. Esto también cubre el riesgo de DLLs x64 de terceros en el editor ARM64 de Unity [E/NV].
3. **Prueba de humo de Unity en ARM64 el día 1** (corresponde a otro tema, pero condiciona este): Hub 3.18.2 o superior, editor 6.3 LTS arm64 con módulo Android, y un AAB vacío con IL2CPP ARM64 y target 36 compilado en local. Se mide el tiempo. **Si queda bloqueado más de 2 días, se activa la compilación en CI.** Si el editor sigue siendo inutilizable, se activa la prueba de Godot.
4. **Prueba de Godot (1 día, solo si se activa).** Pasos previstos, que **no se han ejecutado**:
   - `winget install --id GodotEngine.GodotEngine -a arm64` (GDScript). Si se elige C#: `GodotEngine.GodotEngine.Mono` y `Microsoft.DotNet.SDK.10` (4.8 o superior) o `Microsoft.DotNet.SDK.9` (solo 4.7.x, que termina soporte el 10-11-2026).
   - Opcional: `Microsoft.OpenJDK.17` arm64, y apuntar `export/android/java_sdk_path` a ese JDK.
   - En el editor: *Manage Export Templates*, luego *Project → Install Android Build Template*. En el preset: *Use Gradle Build*, AAB, solo `arm64-v8a`, `min_sdk 26`, `target_sdk 36`, y keystore de release con la misma contraseña para el almacén y la clave.
   - Renderer **Compatibility**. Si el editor se cierra solo, `rendering/gl_compatibility/driver.windows = opengl3_angle`.
   - Criterio de éxito: AAB firmado, instalado con adb en el dispositivo de referencia, con el shader del líquido a 60 fps estables.
5. **Si el plan B se activa después de que salga 4.8 estable**, preferir **4.8**: net10 y mejoras acumuladas. Lenguaje: **GDScript** si se activa antes de 4.8 estable; **C# con net10** solo tras una prueba que confirme anuncios, billing y HTTPS en Android [E].
6. **Correcciones para el GDD** (§8, "Alternativa Godot"):
   - La versión de referencia pasa a ser 4.7.2/4.8, no 4.5/4.6.
   - Poing ya tiene mediación (adaptadores AppLovin, Meta, Mintegral y Unity Ads), aunque el GDD diga "sin mediación con bidding".
   - El MAX oficial para Godot está estancado.
   - Existe PBL 9.1.0; donde dice "PBL 8" debe leerse "8 o superior".
   - PAD en Godot es solo *install-time*.

---

## Riesgos y plan B

| Riesgo | Probabilidad / impacto [E] | Señal temprana | Mitigación / plan B |
|---|---|---|---|
| El editor de Unity ARM64 es inestable con el proyecto | M / A | Cierres al cargar o en Play Mode durante la semana 1 | 1) Actualizar el parche de la LTS y Hub 3.18.2 o superior. 2) Compilar en CI Linux x64. 3) Abrir caso en el issue tracker. 4) Prueba de Godot |
| Plugins nativos de terceros en Unity sin ARM64 (editor) | M / M | `DllNotFoundException` en el editor | Interfaces con implementación simulada en el editor; probar SDKs reales en dispositivo |
| Si el plan B se activa: OpenGLOn12 tumba el editor de Godot | M / M | Cierre en el *splash* | `--rendering-driver opengl3_angle` o el ajuste de proyecto. Volver a una versión anterior del Compatibility Pack solo con autorización del dueño, porque modifica el sistema |
| Si el plan B se activa: C# en Android (experimental, .NET 9 termina soporte) | M / A | Fallos de runtime en Android, APIs sin bindings | GDScript para el juego y C# solo en `tools/level-generator`; o esperar a 4.8 con net10 |
| Si el plan B se activa: sin MAX ni RC en Firebase | A / M | n/a (se sabe de antemano) | AdMob como mediador con Poing v5; GameAnalytics o plugin Kotlin propio para RC; Functions vía HTTPS |
| Si el plan B se activa: sin PAD *fast-follow* | A / M | AAB por encima del tope | Todo *install-time* mientras quepa (límite de Play de unos 1 GB *install-time* según el foro de 2024 [V, dato antiguo]); módulo Gradle propio para *fast-follow* [E] |
| Rendimiento del líquido en gama baja (ambos motores) | M / A | Menos de 60 fps en el dispositivo de clase A | Disparador (a) del GDD. Simplificar el shader (sin copia de pantalla, menos *samples*) antes de cambiar de motor |
| Builds lentos bajo Prism | M / B | Más de 10 minutos por AAB en local [E] | JDK arm64; builds de release en CI |

---

## Fuentes

Godot (oficial)
- https://godotengine.org/download/windows/ : variantes arm64 estándar y .NET; .NET sin soporte bajo emulación.
- https://godotengine.org/download/archive/ : fechas de 4.5.x, 4.6.x, 4.7.x y 4.8-dev7.
- https://godotengine.org/article/dev-snapshot-godot-4-8-dev-7/ : congelación de funciones de 4.8.
- https://godotengine.org/releases/4.7/ : GABE estable; interfaces Java desde GDScript.
- https://github.com/godotengine/godot/releases/tag/4.7.2-stable : nombres y tamaños de assets (consultado vía `gh api`).
- https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html : .NET 8+, .NET 9+ para Android, estado experimental.
- https://godotengine.org/article/platform-state-in-csharp-for-godot-4-2/ : limitaciones de C# en Android e iOS (enero de 2024).
- https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_android.html : JDK, SDK, keystore, AAB.
- https://docs.godotengine.org/en/stable/tutorials/editor/command_line_tutorial.html : exportación headless.
- https://docs.godotengine.org/en/stable/tutorials/rendering/renderers.html : Compatibility para gama baja y 2D.
- https://docs.godotengine.org/en/stable/tutorials/shaders/shader_reference/canvas_item_shader.html
- https://docs.godotengine.org/en/stable/tutorials/shaders/shader_reference/shading_language.html : *instance uniforms* en canvas_item.
- https://docs.godotengine.org/en/stable/engine_details/development/compiling/optimizing_for_size.html
- https://github.com/godotengine/godot/blob/4.7.2-stable/platform/android/java/app/config.gradle : AGP 8.6.1, SDK 36/24, build-tools 36.1.0, NDK 29.0.14206865.
- https://github.com/godotengine/godot/blob/4.7.2-stable/platform/android/java/gradle/wrapper/gradle-wrapper.properties : Gradle 8.11.1.
- https://github.com/godotengine/godot/blob/4.7.2-stable/platform/android/export/export_plugin.cpp : `assetPackInstallTime`, SDK por defecto, keytool.
- https://github.com/godotengine/godot/blob/4.7.2-stable/platform/android/export/export.cpp : ruta del SDK por defecto, `JAVA_HOME`.
- https://github.com/godotengine/godot/blob/4.7.2-stable/platform/windows/display_server_windows.cpp : *fallback* a ANGLE en ARM.
- https://github.com/godotengine/godot/blob/4.7.2-stable/main/main.cpp : drivers por defecto en Windows.
- Issues y PRs: https://github.com/godotengine/godot/issues/106853 · /issues/111495 · /issues/111712 · /issues/118806 · /issues/91021 · /issues/118989 · /pull/123738 · /issues/107992 · /pull/106358 · /issues/110262 · /issues/104137 · /issues/100576 · /pull/52527
- https://forum.godotengine.org/t/is-play-asset-delivery-supported-in-godot-4-2/55900 : PAD solo *install-time* (2024).
- https://repo1.maven.org/maven2/org/godotengine/godot/4.7.2.stable/ : tamaño del AAR.

Plugins
- https://github.com/poingstudios/godot-admob-plugin/releases : v5.0.0 y v5.1.0.
- https://github.com/AppLovin/AppLovin-MAX-Godot/releases : 1.2.0 del 24-04-2025. Issues en https://github.com/AppLovin/AppLovin-MAX-Godot/issues
- https://github.com/godot-sdk-integrations/godot-google-play-billing/releases
- https://github.com/godot-sdk-integrations/godot-play-game-services/releases
- https://github.com/godot-x/firebase
- https://github.com/GodotNuts/GodotFirebase
- https://github.com/GameAnalytics/GA-SDK-GODOT/releases
- https://github.com/abarichello/godot-ci

Android, Google, Microsoft
- https://developer.android.com/google/play/billing/release-notes : PBL 9.1.0; requisito de PBL 8 o superior al 31-08-2026.
- https://developer.android.com/guide/playcore/asset-delivery/integrate-unity : Addressables for Android, 3 modos.
- https://developer.android.com/games/engines/godot/godot-renderers : desfasada (Godot 3).
- https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core : fin de soporte de .NET 8, 9 y 10.
- https://learn.microsoft.com/en-us/windows/arm/apps-on-arm-x86-emulation : Prism.

Unity (para la comparación)
- https://docs.unity3d.com/Manual/system-requirements.html : Windows 11 21H2+ en ARM64; sin Vulkan en Windows on Arm; binarios de terceros.
- https://unity.com/releases/editor/whats-new/6000.3.25f1 : editor ARM64 y Android Build Support para ARM64.
- https://unity.com/releases/editor/whats-new/6000.6.4f1
- https://docs.unity.com/en-us/engine/6000.6/manual/whats-new/unity63 : "New in Unity 6.3 LTS".
- https://discussions.unity.com/t/unity-hub-for-windows-arm64-does-not-run/1721449 : arreglo en Hub 3.18.2 (06-2026).
- https://discussions.unity.com/t/does-unity-really-support-arm64-based-processers/1695327 : 11-2025.
- https://discussions.unity.com/t/is-unity6-really-run-on-windowsarm64/1606124 : 02-2025, componentes emulados.
- https://unity.com/blog/unity-plugin-for-claude-code : 09-09-2026.

Comprobaciones locales de solo lectura (06-10-2026): `winget search godot`, `winget show GodotEngine.GodotEngine[.Mono] --architecture arm64`, `winget show Microsoft.OpenJDK.17 / Microsoft.DotNet.SDK.9 / .10 --architecture arm64`, `Get-AppxPackage Microsoft.D3DMappingLayers` (1.2608.3.0 Arm64), `java -XshowSettings:properties` (Temurin 17.0.19, `os.arch = amd64`), lectura de la cabecera PE de `aapt2.exe` (x64), `zipalign.exe` (x86), `adb.exe` (x86) y del `clang.exe` del NDK 27.1 (x64).
