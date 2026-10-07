# Compuerta del día 1 (paso s26): build Android IL2CPP desde el editor ARM64

Fecha: 2026-10-06 (17:33-18:14, hora local) · Ejecutado por: agente · Plan: `docs/setup/plan-instalacion.md`, paso s26 y riesgo R1.

## Veredicto: PASA, con una salvedad de tiempo

- **3 de 3 builds en verde** (AAB + APK cada uno; código de salida 0; 0 errores; 0 `error CS`).
- **16 KB correcto:** `zipalign -c -P 16 -v 4` (build-tools 36.0.0) da "Verification successful" y `ci/check-16kb.sh` en modo CI da 14/14 `.so` alineadas a 16384 (APK y AAB) y 0 fallos.
- **Ajustes comprobados en el APK:** `minSdkVersion 26`, `targetSdkVersion 36`, `com.amazonfill.dev`, solo `arm64-v8a`.
- **Salvedad:** el build IL2CPP **limpio** tarda 25,4 min (AAB) y supera los 15 min orientativos de R1 y del informe `research/a` §Recomendación, punto 5 (esa cifra es una estimación [E], no un requisito del GDD). Los incrementales tardan 5-5,5 min. Decides tú si los 15 min son vinculantes; ver "Siguientes pasos".

## Entorno

| Elemento | Valor |
|---|---|
| Máquina | Windows 11 Home 10.0.26200, ARM64, Snapdragon X Plus (8 núcleos), 15,6 GB RAM (unos 3,3 GB libres al empezar; Chrome y otras sesiones abiertas) |
| Editor | `C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe`, 6000.3.25f1 (e1dba0a9aba4); el log dice `System architecture: arm64` y `Process architecture: arm64` |
| Lanzador | Unity CLI 1.0.0-beta.12: `unity run <repo> --architecture arm64 --log-file Logs/smoke-android-N.log --no-tail --timeout 5400 --non-interactive -- -nographics -buildTarget Android -executeMethod AF.EditorTools.BuildSmoke.BuildAndroid` (la CLI funcionó; no hizo falta el respaldo con Unity.exe en batchmode) |
| JDK | OpenJDK 17.0.18+8 de Unity (Temurin, x64, emulado por Prism) |
| NDK | r27c (`Pkg.Revision 27.2.12479018`), host `windows-x86_64` (emulado) |
| SDK | build-tools 36.0.0, compileSdk 36 |
| **Gradle** | **9.3.1** (el que trae Unity en `AndroidPlayer/Tools/gradle`; el log dice `Gradle Version: '9.3.1'`) |
| **Android Gradle Plugin** | **9.0.0** (log: `Android Plugin Version: '9.0.0'`; `build.gradle` y `unity.agpVersion` del proyecto exportado en `Library/Bee/Android/Prj/IL2CPP/Gradle`) |
| bundletool | 1.17.2 (`AndroidPlayer/Tools/bundletool-all-1.17.2.jar`) |
| Ajustes del build | IL2CPP, solo ARM64, minSdk 26, targetSdk 36, `applicationId com.amazonfill.dev`, productName "Amazon Fill (dev)", appCategory game, clave de depuración de Unity (sin keystore propio) |

Antes de empezar no había ningún Editor ni Android Studio abiertos; solo el Hub y su Licensing Client, que hacen falta. Al terminar no queda ningún daemon de Gradle ni ningún Editor.

## Tiempos y tamaños

| Build | Resultado | Total (reloj) | AAB | APK | AAB (bytes) | APK (bytes) |
|---|---|---|---|---|---|---|
| 1 (IL2CPP limpio) | OK, código 0 | 1589 s (26,5 min) | 1526,1 s | 42,3 s | 35.118.767 (33,49 MiB) | 35.203.344 (33,57 MiB) |
| 2 (incremental) | OK, código 0 | 434,6 s (7,2 min) | 325,8 s | 43,4 s | 35.118.776 (33,49 MiB) | 35.203.356 (33,57 MiB) |
| 3 (incremental) | OK, código 0 | 406,0 s (6,8 min) | 303,6 s | 45,4 s | 35.118.776 (33,49 MiB) | 35.203.356 (33,57 MiB) |

- **Serie completa:** 40,5 min. El plan estimaba entre 60 y 120.
- **Compuerta de tamaño:** `ci/size-gate.ps1 Builds/Android/smoke.aab` da 35,12 MB frente a 80 MB: OK (sin la medida de Play con bundletool).
- **SHA-256 del build 3:**
  - `smoke.aab`: `7503ceee440e032067f55e2f9497a4832fcd3cd18b8c90755bc8910f09c24154`
  - `smoke.apk`: `713c932f326df79f7212a7f1778fc8e0a528a4cdc8acfe86c389cfc44a125f4c`

**Dónde se va el tiempo del AAB limpio** (build 1, según `Logs/smoke-android-1.log`):

| Fase | Tiempo |
|---|---|
| Compilación de scripts para el player | 32 s |
| Bee, 1.ª pasada: stripping, conversión IL2CPP, runtime libil2cpp y `WritePlayDependencies` | 5 min 15 s |
| Bee, 2.ª pasada: compilación C++ y enlazado con clang emulado (899 nodos) | **12 min 57 s** |
| Gradle (JDK emulado) | 3 min 45 s |

En los incrementales, Bee rehace 66 nodos (3-4 min), porque la escena temporal se regenera en cada build, y Gradle tarda entre 33 y 92 s.

## 16 KB y bibliotecas nativas

- **`zipalign.exe -c -P 16 -v 4 Builds/Android/smoke.apk`** (build-tools 36.0.0): código 0, `Verification successful`.
  - Las `.so` aparecen como "OK - compressed": el manifiesto lleva `extractNativeLibs=true`.
  - Por eso la comprobación que de verdad cuenta es la alineación ELF.
- **`bash ci/check-16kb.sh Builds/Android/smoke.apk Builds/Android/smoke.aab`**: código 0, "OK: todo alineado a 16 KB".
  - Variables usadas: `CHECK16_REQUIRE_ZIPALIGN=1`, `ZIPALIGN`=build-tools 36.0.0, `LLVM_READELF` del NDK r27c y `BUNDLETOOL_JAR`=bundletool 1.17.2 con el JDK de Unity.
  - 14 bibliotecas de 64 bits comprobadas (7 del APK y 7 de `base/lib` del AAB), todas con un align mínimo de 16384, y 0 fallos.
  - El APK universal que genera bundletool desde el AAB también pasa `zipalign -c -P 16 -v 4`.
- **`.so` de `lib/arm64-v8a`** (no hay ninguna otra ABI):

| Biblioteca | Sin comprimir | Comprimida en el APK |
|---|---|---|
| `libil2cpp.so` | 65.482.464 B | 16.050.559 B |
| `libunity.so` | 20.048.384 B | 8.844.496 B |
| `libc++_shared.so` | 1.292.904 B | 406.563 B |
| `lib_burst_generated.so` | 533.688 B | 171.164 B |
| `libswappywrapper.so` | 226.560 B | 79.870 B |
| `libgame.so` | 152.448 B | 56.863 B |
| `libmain.so` | 6.696 B | 2.577 B |

## Manifiesto del APK (`aapt2 dump badging`, build-tools 36.0.0)

`package: name='com.amazonfill.dev' versionCode='1' versionName='1.0' compileSdkVersion='36'` · `minSdkVersion:'26'` · `targetSdkVersion:'36'` · `application-label:'Amazon Fill (dev)'` · `native-code: 'arm64-v8a'` · `launchable-activity: com.unity3d.player.UnityPlayerGameActivity` · `android:appCategory=0` (CATEGORY_GAME).

Permisos: `android.permission.INTERNET` y `com.amazonfill.dev.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION`, que es el de androidx. El de INTERNET lo añade Unity porque `ForceInternetPermission` está en 0 (Auto): algún paquete de la plantilla usa red. No lo pedimos nosotros; queda anotado para cuando apruebes los permisos.

## Dispositivo

`adb devices -l` (platform-tools 37.0.0 del PATH de usuario, el único adb que se usa): no hay ningún dispositivo conectado. Al terminar se paró el servidor adb, como estaba antes. **No se instaló nada.** El APK está listo para s27 (`adb install -r Builds/Android/smoke.apk`, lo hace el dueño por USB).

## Incidencias y desviaciones

1. **Aviso del build (1 por build):** `com.unity.pipeline` registra "No RuntimePipelineConfig asset found… Pipeline will be disabled in Player builds". No afecta al build de humo.
2. **apkanalyzer:** `cmdline-tools/16.0/bin/apkanalyzer.bat` del SDK de Unity falla con `ClassNotFoundException` porque la ruta tiene espacios ("Program Files"). En su lugar usé `aapt2` 2.20 de build-tools 36.0.0, que el paso admite como alternativa.
3. **Licensing:** cada arranque registra "Code 10 while verifying Licensing Client signature… ignoring". Después la conexión con el Licensing Client 1.18.3 sale bien y la licencia funciona.
4. **Cambios de otro proceso durante la serie:**
   - A las 17:39, durante el build 1, otro proceso modificó `Assets/_Project/Editor/CI/BuildSmoke.cs` y `ProjectSetup.cs`: el menú pasó de "Aquazonia/CI" a "Amazon Fill/CI" y cambiaron los comentarios. Las constantes, la validación y los códigos de salida siguen iguales; los builds 2 y 3 compilaron esa versión y pasaron.
   - A las 17:37 también cambió `ci/check-16kb.sh`; la comprobación final usó la versión actual.
   - Nada de eso lo hizo este agente.
5. **Archivos que reescribió el propio Unity durante los builds:**
   - `ProjectSettings/ProjectSettings.asset` y `GraphicsSettings.asset`.
   - `Assets/Settings/UniversalRP.asset`, `UniversalRenderPipelineGlobalSettings.asset` y `DefaultVolumeProfile.asset`.
   - `ProjectSettings/SceneTemplateSettings.json`.
   - Como aún no tienen seguimiento en git, no se puede ver el diff. Los ajustes clave siguen igual: minSdk 26, target 36, ARM64, IL2CPP, `androidUseCustomKeystore: 0`. Hay que revisarlos en s29, antes del commit.
6. **Salidas ignoradas por git:** `Logs/smoke-android-{1,2,3}.log`, `Builds/Android/smoke.{aab,apk}` y la carpeta `Builds/Android/Amazon Fill (dev)_BurstDebugInformation_DoNotShip`. `Assets/_SmokeTemp` se borró al terminar cada build.

## Siguientes pasos

1. **s27: HECHO (2026-10-06 18:30).** Instalado por depuración inalámbrica (adb lo descubrió por mDNS; la IP:puerto que dio el dueño no respondió) en un Samsung SM-S938B (Galaxy S25 Ultra) con Android 16 (API 36), arm64-v8a. `adb install -r` correcto (33,6 MB); la app arranca y queda en primer plano (`UnityPlayerGameActivity`, pid estable). Logcat: Unity 6000.3.25f1, IL2CPP, arm64-v8a, Min API 26, Target API 36, 11 GB de RAM detectados; sin `FATAL` ni errores de `libil2cpp`/`libunity`. Solo dos mensajes ajenos al juego: `libpenguin.so not found` (sondeo del sistema Samsung) y `attributionTag not declared` (AppOps). La captura muestra el fondo por defecto de la cámara, lo esperado en la escena mínima. Captura en `Logs/smoke-device.png` (ignorada por git).
2. **s28 (dueño):** prueba visual de 30 min en Play Mode con DX12 y con `-force-d3d11`.
3. **Decisión sobre el tiempo del build limpio (dueño), sin cambiar nada todavía:**
   - Para iterar en local: el limpio solo se repite tras borrar `Library/` o cambiar de versión. Los incrementales tardan 5-7 min.
   - El build de referencia sigue siendo el del CI Linux x64 (s32, imagen `unityci/editor:ubuntu-6000.3.25f1-android-3`). En la práctica, **B2** (builds de release solo en CI) ya está en el plan.
   - **B3** (JDK 17 ARM64 de Microsoft) solo acortaría Gradle, unos 3,8 de los 25 min. No compensa salirse del JDK de Unity.
   - **B1** (editor x64 bajo Prism) no ayudaría: el cuello de botella es el clang del NDK, que se emula igual con cualquiera de los dos editores, y con B1 también se emularía il2cpp.
4. **s29: HECHO (2026-10-06).** Proyecto Unity commiteado y subido a `origin/main` junto con el renombrado interno y la marca Tintalú; los `.asset` reescritos por Unity entran como línea base.
5. **Más adelante (sin aprobar):** recortar paquetes de la plantilla que no se usan (Visual Scripting, etc.) para reducir `libil2cpp.so` y el tiempo de IL2CPP. Cada paquete que se quite o se añada pide tu aprobación.
6. **R9:** queda registrado Gradle 9.3.1 + AGP 9.0.0, a tener en cuenta para la compatibilidad de los SDK (EDM4U, Firebase, MAX) y el `namespace` en los `.androidlib`.
