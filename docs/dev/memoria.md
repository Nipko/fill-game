# Memoria del juego: inventario, ajustes y presupuesto (2026-10-29)

Rama `tanda6/fx-j6-memoria` (sobre `58be8444`). Origen: observación 6 de la jornada 6 de la prueba completa (`docs/dev/traspaso/pruebas-telefono-2026-10-28-completa-j6.md`, bloque 19.0). Sigue al informe del 2026-10-26 (`docs/dev/memoria-2026-10-26.md`: el HDR fuera, los lienzos del arte por código reutilizados, el pool del Reto al abrirlo, las fuentes sin copia en CPU y la sonda `AF_PERF mem`), que no se repite aquí.

## 1. El problema, con las cifras del teléfono

SM-S938B (Android 16, 12 GB, pantalla a 1080 × 2340), `dumpsys meminfo -d`, en MB:

| Variante | Graphics (GL mtrack) | EGL mtrack | Unknown | Code | TOTAL PSS | TOTAL RSS |
|---|---|---|---|---|---|---|
| `dev.apk`, 18+, nivel 75 | 138,1 (97,8) | 40,3 | 212,9 | 90,5 | 554,2 | 665,6 |
| `dev.apk`, 18+, nivel 200 | 138,0 (97,7) | 40,3 | 218,5 | 90,2 | 553,6 | 666,8 |
| `dev.apk`, <13 (pantalla de bloqueo) | 131,1 (90,8) | 40,3 | 218,4 | 96,2 | 554,5 | 670,8 |
| `rehearsal.apk`, 18+, nivel 1 | 136,8 (96,6) | 40,3 | 206,8 | 106,3 | 565,6 | 723,5 |

08 §5.4 fija **RSS ≤ 400 MB, texturas residentes ≤ 120 MB y heap gestionado ≤ 60 MB** en gama baja. Ocupa lo mismo en un nivel, en el inicio y en la pantalla de bloqueo de <13, que no arranca ningún SDK de anuncios: la memoria se fija en el arranque y no la ponen los anuncios ni el nivel. En las jornadas anteriores: 590-645 MB de PSS en la jornada 1 (del 75 al 200), 591 MB en la pantalla de edad en la jornada 3, 538 MB tras el 82 en la jornada 4 (ya con las bajadas del 2026-10-26), y en la jornada 5 un crecimiento lento de unos 0,25 MB por nivel en sesiones de 480 niveles (observación 8 de la j5; la línea `level` de la sonda, cada 10 niveles, lo vigila).

## 2. Inventario en el Editor

Prueba manual nueva `MemoryInventoryTests.Inventory_Moments` (`[Explicit]`, PlayMode): carga el juego con un perfil en el 74, pasa por el inicio, el Mapa, la pantalla de avatares y el nivel 75, y en cada momento escribe una línea `AF_MEMINV` por textura creada desde que empezó el test (tamaño, formato, mipmaps, si guarda copia en CPU y los bytes en la GPU), los totales por tipo de objeto (`Profiler.GetRuntimeMemorySizeLong`) y `Texture.currentTextureMemory`:

```
pwsh -NoProfile -File scripts/unity-test.ps1 -Mode PlayMode -Architecture x86_64 -Filter 'AF.Tests.PlayMode.MemoryInventoryTests' -LogFile Logs/meminv.log
grep AF_MEMINV Logs/meminv.log
```

**Límite:** el Editor en batchmode dibuja a 1,5 px/dp (el panorama sale de 810 × 1215 px); el S938B, a unos 2,6. El arte por código se pinta a la densidad de la pantalla, así que en el teléfono las texturas pesan más (la sonda dio `tex_mib` 48 en el Mapa en la jornada 4). El Editor no puede medir el heap de IL2CPP ni la memoria interna del motor del reproductor: eso lo mide la sonda en el teléfono (§5).

### 2.1 Recursos que crea el juego (Escritorio x64, 2026-10-29), en MiB

| Tipo | Inicio | Mapa | Avatares | Nivel 75 | Notas |
|---|---|---|---|---|---|
| Texturas (`Texture2D`) | 29,0 (126) | 29,4 (135) | 32,6 (137) | 35,0 (163) | Todas RGBA32 sin comprimir (el arte por código, D-052), sin mipmaps salvo cuatro. Copia en CPU: 0,1 MiB (dos fichas que miran los tests), más el atlas de avatares mientras su pantalla está abierta (1,7 MiB; se suelta al cerrarla) |
| RenderTexture | 0 | 0 | 0 | 0 | El juego no crea ninguno; los de URP, solo si la cámara pide un búfer intermedio (§3) |
| Audio (`AudioClip`) | 6,8 (173) | 6,8 | 6,8 | 7,7 | §2.3 |
| Textos (`TextAsset`) | 3,8 (26) | 3,8 | 3,8 | 3,8 | Packs de niveles (1,2 MB), tablas de textos (1,3 MB), pool del Reto (1,4 MB) |
| Mallas (`Mesh`) | 0,6 (391) | 0,8 | 0,8 | 0,8 | |
| UI (GameObject, RectTransform, CanvasRenderer, Image, TMP) | ≈ 3,5 | ≈ 4,0 | ≈ 4,0 | ≈ 4,4 | 2.450-3.100 objetos |
| **Total de recursos** | **≈ 44** | **≈ 45** | **≈ 48** | **≈ 52** | |

Entre paréntesis, cuántos objetos. Al cerrar el Mapa y los avatares sus texturas se van (el atlas de avatares desaparece al entrar en el nivel); el Mapa deja 0,4 MiB de iconos.

### 2.2 Texturas más grandes (inicio, Editor)

| Textura | px | Formato | Mips | MiB GPU | Nota |
|---|---|---|---|---|---|
| `bg_estuario_panorama` | 810 × 1215 | RGBA32 | no | 3,8 | Fondo del inicio; se ve a pantalla completa |
| `bg_estuario_muelle` | 810 × 1080 | RGBA32 | no | 3,3 | Ídem |
| `fauna_bradypus_variegatus_provisional` | 1024 × 512 | RGBA32 | sí | 2,7 | Atlas de TiTino por código, tamaño fijo, ya a media altura; los mipmaps hacen falta porque se ve pequeño en la rama |
| `arbol_embauba_provisional` | 1024 × 256 | RGBA32 | sí | 1,3 | Rama de TiTino; ídem |
| `river_streaks` | 720 × 400 | RGBA32 | no | 1,1 | |
| `foliage_*` (4) | 480-510 | RGBA32 | no | 3,8 | Esquinas del follaje |
| `ribbon_leaf`, `tray_240`, `modal_surface_*` (5), `home_branch`, `header_plate` | | RGBA32 | no | 5,1 | |
| El resto (≈ 110) | ≤ 300 px | RGBA32 | no | ≈ 8 | Botones, placas, iconos |

Las 5 fuentes TMP (Alpha8, 1024 × 768-832, ya sin copia en CPU desde el 2026-10-26) y `EmojiOne` (ETC2, 0,3 MiB) se cargan con la escena.

**Lectura:** las texturas del teléfono (unos 48 MB) **cumplen** el presupuesto de 120 MB. No hay texturas con copia en CPU que sobren, ni mipmaps que no se usen, ni RenderTextures del juego. Bajar la resolución del arte por código o comprimirlo (ASTC) cambiaría el aspecto (D-003, D-052), y no es donde está la memoria.

### 2.3 Audio (importadores)

| Grupo | Clips | Carga | WAV de origen | En memoria |
|---|---|---|---|---|
| Efectos (`sfx`, `sfx/fruit`, `sfx/pour`) y fauna | 110 | Decompress On Load, mono, precargados | 6,4 MiB | ≈ 6,4 MiB de PCM toda la sesión |
| Música (`music`) | 45 | Compressed In Memory (Vorbis 0,4), sin precarga | 267 MiB | Solo las capas que suenan (≈ 5 clips; < 1 MiB cada uno) |
| Stingers | 5 | Compressed In Memory, precargados | 0,7 MiB | < 0,2 MiB |
| Ambiente (`amb`) y una pista | 13 | Streaming | 70 MiB | Un búfer por pista |

Está bien como está: pasar los efectos a Compressed In Memory ahorraría unos 5 MiB pero decodificaría en cada vertido (latencia y CPU en gama baja). Sin cambios.

### 2.4 Heap gestionado y cachés

- Lo medido el 2026-10-26 sigue: el arranque asigna 54-59 MB de basura y deja unos 19 MB vivos; con IL2CPP el heap reservado no baja, así que el pico del arranque se queda toda la sesión.
- Cachés que no se vacían: ninguna grande. La intro de la marca suelta sus texturas al acabar (en el inicio no queda ninguna), el atlas de avatares se suelta al cerrar su pantalla, el pool del Reto al cerrarlo, y las del Mapa son 0,4 MiB.

## 3. Ajustes del reproductor Android y de URP

| Ajuste | Estado | Efecto en memoria | Decisión |
|---|---|---|---|
| **Android Blit Type** | `Always` → **`Auto`** | Con `Always`, Unity pinta cada fotograma en un búfer de color y profundidad del tamaño de la pantalla y lo copia a la superficie: 1080 × 2340 × (4 + 4) B ≈ 19 MiB de `GL mtrack`. Con `Auto`, solo cuando hace falta (escala de resolución, que el juego no usa) | **Aplicado** con `ProjectSetup` (`BlitType`), exigido por `Validate` (BuildSmoke, BuildDev y BuildRelease paran con 2 si vuelve). El fotograma es el mismo; lo comprueba el portátil (guion, encargo de la jornada 6) |
| HDR de URP | apagado (2026-10-26) | Sin búfer intermedio | Ya hecho |
| MSAA de URP | 1× (apagado) | Ninguno | Ya estaba. El 2D con sprites antialiasados no lo necesita; un test lo vigila |
| Escala de render | 1,0 | Otra escala pediría un búfer intermedio y cambiaría el aspecto | Sin cambios |
| Búfer intermedio de URP 2D (`_CameraTargetAttachment`) | no se crea | URP 2D lo crea con HDR, posproceso, MSAA con resolución explícita, escala ≠ 1, textura de capas de orden, sombras volumétricas o capturas: nada de eso está encendido (la cámara no tiene posproceso y los lienzos son Screen Space Overlay) | Sin cambios. Si apareciera, `rt_top` de la sonda lo dice con su tamaño |
| Profundidad y estencil (`disableDepthAndStencilBuffers` = 0; `useDepthStencilBuffer` del 2D Renderer = 1) | encendidos | Hasta un búfer de pantalla (≈ 10 MiB a 1080 × 2340) | **Sin cambios:** los shaders de UI del juego (`AF_UI_*`) llevan el estencil de las máscaras de UI y el 2D Renderer lo usa para las Sprite Mask. Quitarlo pide revisar cada pantalla; queda como candidato |
| Búfer de pantalla de 32 bits | encendido | 16 bits ahorraría la mitad de la superficie, pero con bandas de color | Sin cambios (D-003) |
| Incremental GC | encendido | Con IL2CPP (Boehm) el incremental reparte la recogida; apagarlo no baja el heap reservado de forma fiable y trae tirones de 5-30 ms | Sin cambios |
| Tamaño del heap de IL2CPP | sin ajuste en Unity 6 | El heap crece con el pico de asignación y no vuelve al sistema | Se baja reduciendo la basura del arranque (2026-10-26) |
| Graphics Jobs | por defecto | Algún hilo más, sin efecto apreciable en la memoria | Sin cambios |
| Ajustes de memoria de Unity (`MemorySettings.asset`) | por defecto | Bloques de los asignadores; su sobrante se ve en `unity_unused_mib` | Sin cambios hasta ver la sonda |
| Swapchain (`vulkanNumSwapchainBuffers` = 3) | 3 | El `EGL mtrack` de 40 MB son ~4 superficies de 9,6 MiB | Sin cambios: con 2 el ritmo de fotogramas empeora |

## 4. Antes y después (estimado)

| Partida (S938B, `dev.apk`, nivel 75) | Antes (j6) | Después (estimado) | Por qué |
|---|---|---|---|
| `GL mtrack` | 97,8 MB | ≈ 78-88 MB | Sin el búfer de color y profundidad de `Always` (10-19 MiB según dónde cuente el driver la profundidad de la superficie) |
| `EGL mtrack` | 40,3 MB | 40,3 MB | La superficie de la ventana, igual |
| Graphics | 138,1 MB | ≈ 118-128 MB | |
| de ellas, texturas | ≈ 48 MB (`tex_mib` de la j4) | igual | Cumplen las ≤ 120 MB de 08 §5.4 |
| Unknown | 212,9 MB | igual | §6 |
| TOTAL PSS | 554 MB | ≈ 535-545 MB | |
| TOTAL RSS | 666 MB | ≈ 647-656 MB | |

## 5. La sonda

`AF_PERF mem` (`DevProbe.Memory.cs`, solo en desarrollo) sale ya en `boot`, en cada `lobby`, en el primer nivel de la sesión y cada 10 niveles (`level`), al abrir el Mapa (`map`) y al abrir el Reto (`daily`). Desde esta rama lleva también, al final y antes de los miembros propios del momento:

- `tex_cur_mib`: `Texture.currentTextureMemory`;
- `unity_unused_mib`: lo que Unity tiene reservado sin usar (`Profiler.GetTotalUnusedReservedMemoryLong`);
- `gfx_reserved_mib`, `audio_mib`, `mesh_mib`, `profiler_mib`, `app_resident_mib` y `app_committed_mib`: los contadores del perfilador del mismo nombre (`null` si la plataforma no los tiene);
- `rt_mib`, `rt_count` y `rt_top`: los RenderTexture vivos y los cuatro mayores con nombre y tamaño (si la cámara usara un búfer intermedio, saldría aquí).

Con lo que ya tenía (`mono_used_mib`, `mono_heap_mib`, `unity_alloc_mib`, `unity_reserved_mib`, `gfx_mib`, `sys_mib`, `tex_mib`), el portátil puede cruzar cada momento con `dumpsys meminfo -d`:

- `Unknown` ≈ `mono_heap` (heap de IL2CPP) + `unity_reserved` (asignadores de Unity) + lo de los SDK y del runtime de IL2CPP que no se ve en Unity;
- `GL mtrack` ≈ `tex_cur` + `rt` + búferes de mallas y del driver (`gfx`, `gfx_reserved`).

El encargo está en el guion (encargo de la jornada 6, «Repetir el bloque 19.0 con la sonda nueva»).

## 6. Lo que queda por encima del presupuesto

Tras esta rama, el S938B seguiría en unos **650 MB de RSS y 540 MB de PSS**. Las causas probables, de mayor a menor:

1. **`Unknown`, ~210 MB:** memoria nativa anónima del proceso. No la ponen los recursos del juego (el inventario suma unos 45-50 MiB en el Editor, que en el teléfono están sobre todo en `GL mtrack`). Lo probable: el heap de IL2CPP con el pico del arranque, los asignadores de Unity con sus bloques reservados, el runtime de IL2CPP (metadatos de tipos de 5 SDK) y la memoria nativa de las bibliotecas que se cargan aunque no se inicialicen (la variante <13 no baja). La sonda nueva lo reparte.
2. **`Code`, 90-106 MB, y el `.so mmap` (~57 MB) y `.dex mmap` (~33 MB):** el código mapeado de `libil2cpp.so`, `libunity.so` y los SDK (AdMob, Firebase, Play Games, Play Billing). En RSS cuenta también lo compartido con el sistema.
3. **`EGL mtrack`, 40 MB:** las superficies de la ventana, proporcionales a la pantalla.
4. **`GL mtrack` restante, ~30 MB** además de las texturas: mallas de UI, programas de shader y lo del driver.

**Qué lo bajaría, con decisión del dueño (no aplicado):**

- **R8 (Minify) en el release:** `AndroidMinifyRelease` está en 0. Reduciría el `.dex mmap` (estimado −10 a −20 MB), pero pide reglas `keep` para los plugins propios que se llaman por JNI (`AfBilling`, `AfSplashHold`, `AfAgeSignals`, `AfIcon`) y probar todos los SDK en el teléfono. Tarea de build-release.
- **IL2CPP Code Generation «Faster (smaller) builds»:** `libil2cpp.so` más pequeño (estimado −5 a −15 MB de `.so mmap`), con algo de coste de CPU en código genérico; pide medir los fotogramas.
- **Quitar profundidad y estencil** (§3): hasta −10 MB a esta resolución, revisando cada pantalla.
- **Bajar el pico del heap del arranque:** crear arte por código más tarde (al abrir cada pantalla) en lugar de en el bloque `services`; toca el arranque con la intro (D-051) y su fluidez.

**En un teléfono de clase A** (720 × 1600 a 2 px/dp, 3-4 GB), con este mismo build, lo que depende de la pantalla baja: las texturas del arte por código a ~28 MB, el `EGL mtrack` a ~18 MB y el búfer de `Always` ya no está. Estimado: **unos 580-600 MB de RSS y 470-490 MB de PSS**. El código y el `Unknown` no dependen de la pantalla.

### Pregunta para el dueño

**Los 400 MB de RSS de 08 §5.4 no se alcanzan con este stack.** Un proyecto Unity vacío con IL2CPP ya ronda los 250-300 MB de RSS en Android 14+, y Tintalú suma el código de 5 SDK (≈ 90-106 MB de `Code`), el `Unknown` de ~210 MB y las superficies de la pantalla. Aun con R8, el código más pequeño y sin profundidad (−25 a −45 MB entre los tres), el S938B quedaría en ~600 MB de RSS y un teléfono de clase A en ~540-560 MB. ¿Cuál prefieres?

1. **Cambiar la métrica a PSS en un teléfono de clase A:** PSS ≤ 450 MB medido con `dumpsys meminfo` (es lo que usa Android para decidir qué cerrar; el RSS cuenta páginas compartidas con otras apps). Recomendada.
2. Mantener RSS y subir el tope a ~600 MB.
3. Mantener 400 MB de RSS y abrir un carril para R8, el código más pequeño, quitar la profundidad y bajar el pico del arranque, sabiendo que no llega.

08 §5.4 no se toca hasta que respondas. Las texturas (≈ 48 MB frente a 120) ya cumplen; el heap gestionado (60 MB) se verá con `mono_heap_mib` de la sonda.

## 7. Verificación (escritorio x64, 2026-10-29)

- `dotnet build dotnet/AmazonFill.slnx -warnaserror`: 0 errores, 0 avisos. `dotnet test`: 1.002 (996 + 6 omitidos), 0 fallos.
- `python tools/validate_schema.py`: 116/116 PASS.
- `scripts/unity-build.ps1 -Method Setup -Architecture x86_64`: OK (`blit=Auto`); cambia solo `androidBlitType: 0 → 2` en `ProjectSettings.asset`.
- `scripts/unity-compile.ps1 -Architecture x86_64`: compila.
- EditMode: 4.818 (4.811 + 7 omitidos), 0 fallos. PlayMode (`-TimeoutSec 7200`): 2.359 (1.997 + 362 omitidos o explícitos), 0 fallos, 42 min.
- `MemoryInventoryTests.Inventory_Moments` (manual): pasa; sus cifras son las del §2.
- `scripts/unity-build.ps1 -Method Dev -Architecture x86_64`: OK, `Builds/Android/dev.apk` (73,6 MiB). `ci/size-gate.ps1` solo acepta un `.aab` y BuildDev no lo genera: el APK de desarrollo pesa 73,6 MiB, por debajo de los 80 MB. `ci/check-manifest.ps1 Builds/Android/dev.apk -Mode dev`: OK.
- El cambio del Blit Type no se ve en el Editor (es un ajuste del reproductor Android): la comprobación visual la hace el portátil en el teléfono.
