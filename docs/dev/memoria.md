# Memoria del juego: inventario, ajustes y presupuesto (2026-10-29)

Dos ramas: `tanda6/fx-j6-memoria` (sobre `58be8444`; §1-§6 y §8), por la observación 6 de la jornada 6 de la prueba completa (`docs/dev/traspaso/pruebas-telefono-2026-10-28-completa-j6.md`, bloque 19.0), y `tanda6/fx-j6b-memoria` (§7), por la observación 14 de la misma jornada. Siguen al informe del 2026-10-26 (`docs/dev/memoria-2026-10-26.md`: el HDR fuera, los lienzos del arte por código reutilizados, el pool del Reto al abrirlo, las fuentes sin copia en CPU y la sonda `AF_PERF mem`), que no se repite aquí.

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

El Mapa del río v2 (2026-11-08, observación 5 de la prueba de la 1.1): abrirlo y cerrarlo tres veces deja las mismas texturas, mallas, materiales y lienzos que la primera vuelta, y lo suyo (5,3 MiB de texturas, 0,5 MiB de lotes, 31.512 vértices) vuelve a cero al cerrar; sus lienzos por código piden 17,3 MiB de búferes por apertura y desde entonces se reutilizan mientras se abre (12,1 MiB nuevos). Cifras, hipótesis sobre `GL mtrack` y la medida del teléfono: añadido de D-069 del 2026-11-08 en `decisiones.md` y `MapMemoryCycleTests`.

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
- Cachés que no se vacían: ninguna grande. La intro de la marca suelta sus texturas al acabar (en el inicio no queda ninguna), el atlas de avatares se suelta al cerrar su pantalla, el pool del Reto al cerrarlo, y las del Mapa son 0,4 MiB. Desde el 2026-11-16 (§10), tras la 1.ª apertura del Mapa v2 se queda además un búfer de reserva de 8 MiB para las aperturas siguientes.

## 3. Ajustes del reproductor Android y de URP

| Ajuste | Estado | Efecto en memoria | Decisión |
|---|---|---|---|
| **Android Blit Type** | `Always` → **`Auto`** (en el teléfono no bajó nada: §7.2) | Con `Always`, Unity pinta cada fotograma en un búfer de color y profundidad del tamaño de la pantalla y lo copia a la superficie: 1080 × 2340 × (4 + 4) B ≈ 19 MiB de `GL mtrack`. Con `Auto`, solo cuando hace falta (escala de resolución, que el juego no usa) | **Aplicado** con `ProjectSetup` (`BlitType`), exigido por `Validate` (BuildSmoke, BuildDev y BuildRelease paran con 2 si vuelve). El fotograma es el mismo; lo comprueba el portátil (guion, encargo de la jornada 6) |
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

**Riesgos del Blit Type en `Auto` y cómo volver atrás.** Sin el búfer intermedio, Unity presenta el fotograma directamente en la superficie de Android, y eso puede notarse donde el juego no pinta cada fotograma o donde Android pone algo encima:
- el Mapa en reposo, que pinta con `OnDemandRendering` a `perf_target_fps_menus`: con `Always` el búfer intermedio guardaba el último fotograma; sin él, un driver podría alternar fotogramas viejos (parpadeo) al quedarse quieto, al tocar o al desplazar;
- el paso del icono a la intro con el velo de `AfSplashHold` (añadido de D-051), que se quita cuando el primer fotograma ya está en la pantalla;
- los bordes con el Safe Area y `androidRenderOutsideSafeArea: 1`: franjas en la zona de la cámara o en los bordes;
- además, los anuncios a pantalla completa, la pausa, Atrás y volver de segundo plano.

El portátil los mira en el teléfono (guion, encargo de la jornada 6). Si alguno falla, se vuelve atrás así: `ProjectSetup.BlitType = AndroidBlitType.Always`, `pwsh -NoProfile -File scripts/unity-build.ps1 -Method Setup` (deja `androidBlitType: 0` en `ProjectSettings.asset`) y se commitea ese cambio con el del test de `MemorySettingsTests`.

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

**Medido después (jornada 6, observación 14): no se cumplió.** `GL mtrack` siguió en 88,8-99,6 MB. Por qué, en §7.2.

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

- **R8 (Minify) en el release: aplicado en la 1.1** (D-059, añadido del 2026-11-06; rama `v11/r8-release`; `docs/dev/release.md` §8). `AndroidMinifyRelease` pasa a 1 por `ProjectSetup`, con las reglas `keep` de los plugins propios, las de los AAR de los SDK y las de Play Games en `AfKeepRules.androidlib`. En el ensayo, el dex baja de 28,98 MB a 12,07 MB sin comprimir (−58 %; de 4 a 2 `classes*.dex`) y la descarga de Play de 57,00 a 51,30 MB. Lo que eso baje el `.dex mmap` del teléfono (estimado −10 a −20 MB) **está sin medir**: lo dirá la sonda `AF_PERF mem` con el ensayo en el S938B, junto con la lista de humo de §8 de `release.md`.
- **IL2CPP Code Generation «Faster (smaller) builds»:** `libil2cpp.so` más pequeño (estimado −5 a −15 MB de `.so mmap`), con algo de coste de CPU en código genérico; pide medir los fotogramas.
- **Quitar profundidad y estencil** (§3): hasta −10 MB a esta resolución, revisando cada pantalla.
- **Bajar el pico del heap del arranque:** crear arte por código más tarde (al abrir cada pantalla) en lugar de en el bloque `services`; toca el arranque con la intro (D-051) y su fluidez.

**En un teléfono de clase A** (720 × 1600 a 2 px/dp, 3-4 GB), con este mismo build: el `EGL mtrack` baja a ~18 MB con la pantalla, pero las texturas del arte por código se quedan en ~40-50 MB, porque se pintan a una densidad fija y no a la del teléfono (§7.3). El código y el `Unknown` tampoco dependen de la pantalla. Estimado: **unos 600-620 MB de RSS y 490-510 MB de PSS**.

### Pregunta para el dueño

**Respondida el 2026-10-29 (D-060):** el dueño eligió la opción 1, «PSS ≤ 450 MB». 08 §5.4 ya lo recoge.

**Los 400 MB de RSS de 08 §5.4 no se alcanzan con este stack.** Un proyecto Unity vacío con IL2CPP ya rondaría los 250-300 MB de RSS en Android 14+ (estimación sin fuente: no se ha medido aquí), y Tintalú suma el código de 5 SDK (≈ 90-106 MB de `Code`), el `Unknown` de ~210 MB y las superficies de la pantalla. Aun con R8, el código más pequeño y sin profundidad (−25 a −45 MB entre los tres), el S938B quedaría en ~600 MB de RSS y un teléfono de clase A en ~540-560 MB. ¿Cuál prefieres?

1. **Cambiar la métrica a PSS en un teléfono de clase A:** PSS ≤ 450 MB medido con `dumpsys meminfo` (es lo que usa Android para decidir qué cerrar; el RSS cuenta páginas compartidas con otras apps). Recomendada.
2. Mantener RSS y subir el tope a ~600 MB.
3. Mantener 400 MB de RSS y abrir un carril para R8, el código más pequeño, quitar la profundidad y bajar el pico del arranque, sabiendo que no llega.

08 §5.4 no se toca hasta que respondas. Las texturas (≈ 48 MB frente a 120) ya cumplen; el heap gestionado (60 MB) se verá con `mono_heap_mib` de la sonda.

## 7. La observación 14 de la jornada 6: `GL mtrack` no bajó (rama `tanda6/fx-j6b-memoria`, 2026-10-29)

Lo medido en el teléfono (hoja j6, punto 10, «La memoria»): con el Blit en `Auto`, `GL mtrack` sigue en 88,8-99,6 MB; el PSS del `dev.apk` 18+ sube a 605-620 MB (por `System`, 36 MB frente a 19, y `Code`, 102 MB frente a 90); el ensayo da 545 MB y el <13, 537 MB. La sonda: `unity_reserved` 243 MiB, `unity_unused` ~134, `mono_heap` 39-53, `gfx` 84-114, `tex_cur` 38-52, `audio` ~10, `rt` 0.

### 7.1 Dónde están los ~95 MB de `GL mtrack`

`gfx_mib` (lo que Unity cuenta que ha pedido al driver) sale casi igual que `GL mtrack`: lo que hay ahí lo pidió Unity. Reparto razonado (no hay un contador por partida):

| Partida | MB | Cómo se sabe |
|---|---|---|
| Texturas | 38-52 | `tex_cur_mib`. Unas 130-165 texturas del arte por código (RGBA32; 29-35 MiB las del juego en el inventario), las 5 fuentes (4 MiB), EmojiOne, las de posproceso de URP (≈ 3 MB, ya fuera: §7.4) y las del motor |
| Profundidad y estencil de la pantalla | ≈ 9,6 | 1080 × 2340 × 4 B (D24S8). Con Vulkan la crea Unity aparte de la cadena de presentación y cuenta aquí; con OpenGL ES va en la superficie (`EGL mtrack`) |
| Búferes intermedios de URP | 0 | `rt_mib` 0,0 y el código de URP 2D (`Renderer2DRendergraph.GetRenderPassInputs`): sin posproceso, HDR, MSAA, escala ≠ 1, viewport parcial, conversión sRGB ni textura de capas, no crea `_CameraTargetAttachment` ni `_CameraDepthAttachment` |
| El resto, ≈ 30-45 | | Lo del dispositivo gráfico de Unity: los bloques de memoria del driver (Unity reparte cada bloque entre varios recursos y el sobrante cuenta), los búferes dinámicos de vértices, índices y constantes de la UI (por fotograma en vuelo), los de subida de texturas y los programas de shader |

Las texturas no tienen copia en CPU (salvo el atlas de avatares mientras su pantalla está abierta) ni mipmaps que no se usen (cuatro, a propósito: §2.2), y el juego no crea RenderTextures. **No queda una bajada segura en las texturas sin cambiar la imagen** (§7.5, pregunta 1).

### 7.2 Por qué el Blit en `Auto` no bajó nada

El cálculo de §4 suponía que con `Always` había un búfer de color y profundidad fuera de la pantalla. En el teléfono no estaba (o no contaba en `GL mtrack`). Lo más probable es que el juego pinte con **Vulkan**: la API gráfica está en automático (`m_BuildTargetGraphicsAPIs: []`) y Unity 6 prefiere Vulkan en un Adreno 830. Con Vulkan se pinta directamente en las imágenes de la cadena de presentación (`EGL mtrack` 40,3 MB ≈ 4 × 9,64 MiB), y el Blit Type solo decide si hay una copia en la ruta de OpenGL ES. No se puede comprobar desde el escritorio: desde esta rama, la línea `boot` de la sonda dice `gfx_api` (§7.4). **`Auto` se queda:** no cuesta nada y la imagen cumplió en el teléfono (hoja j6, punto 10).

### 7.3 `Unknown`, `unity_unused` y por qué el `dev.apk` sube por `System` y `Code`

- **`unity_reserved` 243 MiB = ~109 en uso + ~134 sin usar, igual desde `boot` hasta el nivel 200.** La memoria nativa de Unity se fija en el arranque y no vuelve. Lo probable es el pico del arranque:
  - cada textura nueva (`SetPixels` o `LoadRawTextureData` y `Apply(false, true)`) guarda su imagen RGBA32 en el asignador de Unity hasta que el hilo de render la sube;
  - el arranque crea ~35-50 MB de texturas en pocos fotogramas;
  - los bloques que quedan libres (16 MiB por defecto en los asignadores principal y de gráficos) se quedan reservados.

  **Solo las páginas tocadas cuentan en el PSS.** Cuánto de esos 134 es residente lo dicen `app_resident_mib` frente a `app_committed_mib` (ya están en la sonda; la hoja no los anotó) y los experimentos E2 y E3 de §7.6.
- **`MemorySettings`:** los tamaños de bloque se pueden cambiar (Project Settings › Memory Settings, `ProjectSettings/MemorySettings.asset`, hoy todo por defecto). Unity no tiene API pública para escribirlos; un método de `ProjectSetup` podría hacerlo con `SerializedObject` sobre `m_PlatformMemorySettings`, pero **no se toca a ciegas**. Primero se mide en el teléfono con los mismos ajustes como argumentos del reproductor (`-memorysetup-…`), que `UnityPlayerGameActivity` lee del extra `unity` del intent, sin build nuevo (E2 y E3). Si baja, un carril lo fija en `ProjectSetup` con su `Validate`.
- **`Code` no lo mueve el código del juego, sino el estado de compilación de ART.** El ensayo de release dio 106 MB de `Code` en la jornada 6 y 83 MB ahora, con la misma configuración: según el dex esté solo verificado, compilado por perfil o con el JIT caliente, `Code` cambia ±20 MB. El `dev.apk` lleva además la variante de desarrollo de `libunity.so` (más grande, con el perfilador), que el release no lleva.
- **`System`** es la parte del PSS de páginas compartidas con otros procesos: depende de cuántos procesos comparten cada biblioteca en ese momento (después de Spotify, una alarma o la Liga no es lo mismo). Los +17 MB del `dev.apk` no aparecen en el ensayo ni en el <13, y en este carril no hay nada en el código que los explique.
- **Lo propio del `dev.apk`** (el perfilador y sus asignadores dentro de `unity_reserved`, la variante de desarrollo) no va al release y no se persigue. **D-060 se juzga con el ensayo de release** (o el AAB), no con el `dev.apk`.
- **Corrección a §6:** el arte por código se pinta a una densidad fija (§7.5, pregunta 1), así que en clase A las texturas no bajan a ~28 MB: se quedan en ~40-50 MB. La estimación de clase A sube a **~490-510 MB de PSS**, por encima de los 450 de D-060.

### 7.4 Lo aplicado en esta rama

| Cambio | Ahorro estimado | Imagen | Cómo |
|---|---|---|---|
| **El 2D Renderer sin `PostProcessData`** | ≈ 3-4 MB entre `GL mtrack` (10 texturas de grano de película de 256 KiB cada una y 2 de SMAA) y `Unknown` (los shaders `UberPost`, `Bloom`, `FinalPost`, `TemporalAA`, `LensFlare…`, `…DepthOfField`, `CameraMotionBlur`, `PaniniProjection` y `LutBuilder…`, ≈ 1,3 MB serializados), y en el APK: medido en el build de desarrollo, las texturas de los recursos del build bajan de 7,5 a 4,7 MB, los shaders de 2,3 a 1,1 MB y el `dev.apk` de 73,6 a 71,5 MiB | Igual: la cámara no tiene posproceso (sin `UniversalAdditionalCameraData` serializado, `renderPostProcessing` es false). URP 2D comprueba el null y sin datos no crea los pases de posproceso ni la LUT | `ProjectSetup.RendererPostProcessData` = false, aplicado con `SerializedObject` sobre `m_PostProcessData` de cada `Renderer2DData` y exigido por `Validate` (BuildSmoke, BuildDev y BuildRelease paran con 2 si vuelve). Al guardarlo, Unity reescribió `Renderer2D.asset` con su formato actual: quitó los campos antiguos de shaders y materiales, que URP 17 ya lee de los ajustes globales |
| **Sonda: con qué se pinta** (línea `boot`) | — | — | `gfx_api`, `gfx_device`, `gfx_version`, `render_thread`, `screen_px`, `render_px` (si difiere de `screen_px`, hay búfer intermedio), `msaa` y `unity_args`: los argumentos del reproductor del extra `unity`, para que cada medida diga con qué se tomó |
| **Sonda: el resumen de `dumpsys meminfo` en cada línea** (`--ei af_mem_os 1`) | — | — | `os_pss_mib`, `os_java_mib`, `os_native_mib`, `os_code_mib`, `os_graphics_mib`, `os_private_other_mib`, `os_system_mib` y `os_read_ms`, con `android.os.Debug.getMemoryInfo`, en el mismo instante que los contadores de Unity. Va con su extra porque la lectura tarda decenas de ms |
| **Inventario** (`MemoryInventoryTests`, manual) | — | — | En el Mapa, los avatares y el nivel, `from_lobby` y `from_lobby_mib`: cuánto arte del inicio sigue vivo (candidata 3 de §7.5). En cada momento, los shaders cargados (`shaders`, `shaders_mib`) |

**Total aplicado: ≈ 3-4 MB.** No cambia el orden de magnitud: lo grande está en el motor (§7.3) y en lo que pide decisión (§7.5).

### 7.5 Lo que queda, con quién decide

**Aplicado en la 1.1 (rama `v11/memoria-clase-a`, §9):** la pregunta 1 con la opción (a) y el punto 3, corregido (el panorama, el muelle, el follaje y TiTino los pinta también el nivel: lo propio del inicio son ≈ 1,5 MiB, no 13-15).

**Pregunta 1 para el dueño (calidad visible en gama baja).** El arte por código se pinta a una densidad fija (fondos a 2,25 px/dp; follaje, UI y personajes a 3 px/dp; superficies de los modales a 1,5), no a la del teléfono. En clase A (≈ 2 px/dp) ocupa lo mismo que en el S938B (≈ 40-50 MB) y la GPU lo reduce al pintarlo. Pintarlo como mucho a la densidad del teléfono bajaría en clase A unos 15-25 MB de `GL mtrack` (follaje y UI, −55 % de píxeles; fondos, −20 %), sin cambio en el S938B. El píxel final cambia algo: el arte se pinta ya pequeño en lugar de reducirse al dibujarse. Además, la revisión visual del 2026-10-07 pidió subir la densidad porque las orillas salían en escalera al ampliarse. ¿Lo pruebo?
- (a) sí, con un tope de densidad igual a la del teléfono y la revisión del dueño a 2 px/dp (`wm density 320` en el S938B lo simula). Recomendada: es lo único de las texturas que mueve D-060 en clase A;
- (b) no: la densidad fija se queda.

**Candidatas técnicas (no cambian la imagen, pero piden un build de prueba en el teléfono), para un carril tras los experimentos de §7.6:**

2. **Sin profundidad ni estencil en la pantalla** (`PlayerSettings.Android.disableDepthAndStencilBuffers` y `m_UseDepthStencilBuffer` del 2D Renderer a 0): −9,6 MB a 1080 × 2340 (−4,4 MB a 720 × 1600).
   - Revisado en el código: el juego no usa `Mask` (solo `RectMask2D`, sin estencil), ni `SpriteMask`, ni luces 2D, ni shaders que escriban o lean profundidad (los `AF_UI_*` van con `ZWrite Off` y `ZTest [unity_GUIZTestMode]`), y todos los lienzos son Screen Space Overlay.
   - **Pero** el pase de la UI de URP (`DrawScreenSpaceUIPass`) ata la profundidad de la pantalla, y sin ella el resultado en Vulkan no se puede asegurar desde el escritorio (pantalla negra o errores de validación posibles). No se aplica sin probarlo en el teléfono.
3. **Soltar el arte del inicio mientras se juega:** las 126 texturas del inicio siguen vivas en el nivel (`from_lobby_mib` 28,9 en el inventario, con los botones y las placas que el nivel también usa); de ellas, el arte de escena propio del inicio (el panorama, el muelle, los 4 follajes, el río y la rama de TiTino) son ≈ 13-15 MiB. Al volver al inicio se leerían de la caché en disco (`Cached`, guía 4.9; unos 20-40 ms en el teléfono). Baja la memoria durante la partida (la que Android mira al pasar a segundo plano), no el pico del inicio.
4. **Los tamaños de bloque de los asignadores de Unity** (§7.3), si E2 o E3 lo justifican.
5. Las de §6 (R8 en el release, IL2CPP «Faster (smaller) builds», bajar el pico del arranque) siguen igual. La cadena de presentación con 2 imágenes (−9,6 MB de `EGL mtrack`) empeora el ritmo de fotogramas: decisión del dueño, si hiciera falta.

### 7.6 Experimentos para el portátil (sin build nuevo)

Todos con el mismo `dev.apk` y la misma preparación, para que `Code` no cambie entre medidas:

```
adb shell cmd package compile -m speed-profile -f com.amazonfill.dev
adb shell am force-stop com.amazonfill.dev
adb shell am start -n com.amazonfill.dev/com.unity3d.player.UnityPlayerGameActivity --ei af_mem_os 1 [-e unity "<argumentos>"]
```

| Experimento | `-e unity "…"` | Qué responde |
|---|---|---|
| E0, base | (nada) | La referencia del día |
| E1, OpenGL ES | `-force-gles` | Cuánto de `GL mtrack` es propio de Vulkan; confirma `gfx_api` |
| E2, bloques de 4 MiB | `-memorysetup-main-allocator-block-size=4194304 -memorysetup-thread-allocator-block-size=4194304 -memorysetup-gfx-main-allocator-block-size=4194304 -memorysetup-gfx-thread-allocator-block-size=4194304` | Cuánto del `Unknown` es sobrante de los bloques de 16 MiB (`unity_unused_mib`, `os_private_other_mib`) |
| E3, E2 y los temporales | lo de E2 más `-memorysetup-temp-allocator-size-main=4194304 -memorysetup-job-temp-allocator-block-size=1048576` | Si los asignadores temporales suman |

En cada uno, en el inicio, el Mapa y el nivel 75 (un minuto quieto en cada sitio), la línea `AF_PERF {"event":"mem"` (con `os_*`, `unity_*`, `app_resident_mib`, `app_committed_mib`, `gfx_*` y `tex_cur_mib`) y `dumpsys meminfo -d`. En E1, además, la imagen y los fotogramas de un nivel (`AF_PERF frames` con `--ei af_bench_morpho 1`). El ensayo de release, con E0 y el mejor de E1-E3: los argumentos del reproductor también valen en release (sin sonda: solo `dumpsys`).

**Esperado:** con este build, `GL mtrack` ≈ 3 MB menos que en la jornada 6 (sin las texturas de posproceso) y lo demás igual. E1-E3 dicen qué carril de §7.5 (2-4) vale la pena.

### 7.7 Verificación (escritorio x64, 2026-10-29)

- `scripts/unity-build.ps1 -Method Setup -Architecture x86_64`: OK (`posproceso=Renderer2D:False`); cambia solo `Assets/Settings/Renderer2D.asset`.
- `scripts/unity-compile.ps1 -Architecture x86_64`: compila.
- EditMode: 4.837 (4.830 + 7 omitidos), 0 fallos. PlayMode entero (`-TimeoutSec 7200`, por el cambio en el render): 2.436 (2.068 + 368 omitidos o explícitos), 0 fallos, 37 min.
- `MemoryInventoryTests.Inventory_Moments` (manual): pasa; mismas texturas que en §2 (126 en el inicio, 163 en el nivel), `from_lobby_mib` 28,9 en el Mapa, los avatares y el nivel.
- `dotnet build -warnaserror`: 0 avisos; `dotnet test`: 1.002 (996 + 6 omitidos); `validate_schema.py`: 116/116.
- `scripts/unity-build.ps1 -Method Dev -Architecture x86_64`: OK, `dev.apk` de 71,5 MiB, sin `FilmGrain`, `AreaTex` ni `UberPost` en el informe del build; `ci/check-manifest.ps1 Builds/Android/dev.apk -Mode dev`: OK.
- La sonda nueva y la imagen sin posproceso solo se ven en el teléfono (guion, encargo de la jornada 6, «La memoria otra vez, con experimentos»).

### 7.8 Los experimentos en el teléfono y OpenGL ES 3 por defecto (rama `tanda6/fx-j6c-gles`, 2026-10-29)

**Lo medido** (hoja j6, punto 14 y observación 20; `b19c/`). La sonda: `gfx_api` Vulkan, `gfx_device` Adreno (TM) 830, `screen_px` y `render_px` 1080 × 2340 (sin búfer intermedio, como se esperaba). `dev.apk`, nivel 75, `dumpsys meminfo -d` en KB (`EGL mtrack` 40.288 en todos):

| Experimento | Java Heap | Native Heap | Code | Graphics (GL mtrack) | Private Other | System | TOTAL PSS | `os_pss` / `os_graphics` / `gfx` (MiB) |
|---|---|---|---|---|---|---|---|---|
| E0, la base | 36.660 | 42.508 | 88.800 | 136.964 (96.676) | 263.344 | 37.708 | **610.628** | 503,1 / 140,8 / 94,4 |
| E1, `-force-gles` | 20.324 | 36.888 | 100.192 | 124.932 (84.644) | 234.952 | 37.750 | **559.762** | 491,4 / 137,2 / 49,9 |
| E2, bloques de 4 MiB | 20.820 | 40.068 | 95.564 | 136.964 (96.676) | 256.140 | 37.787 | **592.051** | 501,2 / 140,8 / 94,4 |
| E3, E2 y los temporales | 19.816 | 40.044 | 89.676 | 136.964 (96.676) | 259.072 | 36.780 | **586.976** | 500,4 / 140,8 / — |

- **El ensayo de release, nivel 75** (sin sonda): E0 **530.027** (`GL mtrack` 93.756) y E1 **494.404** (`GL mtrack` 75.804): −36 MB.
- **E0 en el inicio:** 598.183 de PSS y `GL mtrack` 91.972, unos 3-5 MB por debajo de la jornada 6, lo esperado al quitar el posproceso (§7.4).
- **Lectura:**
  - E1 es el único que baja de verdad: `GL mtrack` −12 a −18 MB y Private Other −10 a −28 MB. `gfx_mib` pasa de 94 a 50: con OpenGL ES el driver lleva por su cuenta parte de lo que con Vulkan reservaba Unity en bloques (§7.1).
  - E2 y E3 (bloques de 4 MiB) bajan 19-24 MB, casi todo en Private Other y dentro del ruido entre arranques: **los tamaños de bloque no se tocan** (la candidata 4 de §7.5 queda descartada por ahora).
  - Nada baja de 450 MB en el S938B, que no es de clase A.
- **No valen** las medidas de E1-E3 del inicio y del Mapa: se tomaron en el nivel 75, porque al relanzar el juego retomó el nivel a medias. Y **falta el rendimiento con OpenGL ES** (`AF_PERF frames` con `af_bench_morpho` en E1).

**Decisión (añadido de D-060 del 2026-10-29):** OpenGL ES 3 primero y Vulkan de respaldo en Android, a condición de que el rendimiento no empeore.
- Lo aplica `ProjectSetup.AndroidGraphicsApis` (`SetUseDefaultGraphicsAPIs(Android, false)` y `SetGraphicsAPIs(Android, [OpenGLES3, Vulkan])`), lo exige `Validate` y lo vigila `MemorySettingsTests.AndroidGraphicsApis_OpenGles3First_VulkanAsFallback`. En `ProjectSettings.asset` solo cambia `m_BuildTargetGraphicsAPIs`.
- Vulkan se queda detrás, en vez de dejar solo OpenGL ES 3, por dos razones: el teléfono sin OpenGL ES 3 arranca igual, y el portátil puede comparar con el mismo APK (`-e unity "-force-vulkan"`).

**Revisado que nada dependa de Vulkan:**

| Qué | Resultado |
|---|---|
| Shaders propios (`AF_UI_Additive`, `Glass`, `Liquid`, `Silhouette`, `Stream`) | `#pragma target` 2.0-3.0, sin `only_renderers`, cómputo ni `SV_VertexID`: compilan para OpenGL ES 3 (ya lo hacían: estaba en la lista automática) |
| Formatos de textura | RGBA32 (arte por código), Alpha8 (fuentes), ETC2 (EmojiOne): todos de OpenGL ES 3 |
| Espacio de color lineal | Necesita OpenGL ES 3.0, que es el mínimo de la lista |
| Blit Type en `Auto` | Con OpenGL ES, Unity solo copia cuando hace falta (escala de resolución, que no se usa). E1 ya se midió con `Auto`: misma imagen (`b19c/ensayo-E1.png`) |
| `AfSplashHold`, `AfLockGuard`, `AfAudioFocus`, `AfIcon` | Java con API de la plataforma, sin nada de gráficos. El velo es una vista de Android encima de la ventana de Unity: no depende de la API |
| Manifiesto (`aapt2 dump xmltree` del `dev.apk` nuevo) | `<uses-feature android:glEsVersion="0x00030000">` (obligatorio, como ya estaba: OpenGL ES 3 estaba en la lista automática) y `android.hardware.vulkan.version` con `required="false"` (lo pone Unity por tener Vulkan en la lista; no filtra teléfonos en Play). Ningún requisito nuevo. `ci/check-manifest.ps1` no mira `uses-feature` y sigue en OK (§7.9) |

**Riesgos que solo mide el teléfono:**
- el rendimiento: el Morpho y los fotogramas del vertido y de la intro (`motion_pauses`, `frame_max_ms`);
- el primer fotograma y el arranque, porque OpenGL ES compila los shaders al usarlos por primera vez;
- en otros teléfonos, los drivers de OpenGL ES de gama baja (Mali antiguos) suelen ser más maduros que los de Vulkan, pero no se ha probado ninguno.

### 7.9 Verificación de la rama `tanda6/fx-j6c-gles` (escritorio x64, 2026-10-29)

- `scripts/unity-build.ps1 -Method Setup -Architecture x86_64`: OK (`graficos=OpenGLES3>Vulkan`); en `ProjectSettings.asset` solo cambia `m_BuildTargetGraphicsAPIs` (`m_APIs: 0b00000015000000`, OpenGL ES 3 y Vulkan; `m_Automatic: 0`).
- `scripts/unity-compile.ps1 -Architecture x86_64`: compila.
- EditMode con `-Filter "ProjectSetup|Memory"`: 26/26, 0 fallos (con el test nuevo).
- `scripts/unity-build.ps1 -Method Dev -Architecture x86_64`: OK, `dev.apk` de 71,5 MiB (igual que antes: los shaders de OpenGL ES 3 y de Vulkan ya iban los dos).
- `ci/check-manifest.ps1 Builds/Android/dev.apk -Mode dev`: OK.
- PlayMode con `-Filter "BrandIntro|BootSplash|MemoryProbe"`: 98 (97 + 1 omitido), 0 fallos. El Editor pinta con Direct3D: la API de Android solo se ve en el teléfono.

## 8. Verificación del carril anterior (escritorio x64, 2026-10-29)

- `dotnet build dotnet/AmazonFill.slnx -warnaserror`: 0 errores, 0 avisos. `dotnet test`: 1.002 (996 + 6 omitidos), 0 fallos.
- `python tools/validate_schema.py`: 116/116 PASS.
- `scripts/unity-build.ps1 -Method Setup -Architecture x86_64`: OK (`blit=Auto`); cambia solo `androidBlitType: 0 → 2` en `ProjectSettings.asset`.
- `scripts/unity-compile.ps1 -Architecture x86_64`: compila.
- EditMode: 4.818 (4.811 + 7 omitidos), 0 fallos. PlayMode (`-TimeoutSec 7200`): 2.359 (1.997 + 362 omitidos o explícitos), 0 fallos, 42 min.
- `MemoryInventoryTests.Inventory_Moments` (manual): pasa; sus cifras son las del §2.
- `scripts/unity-build.ps1 -Method Dev -Architecture x86_64`: OK, `Builds/Android/dev.apk` (73,6 MiB). `ci/size-gate.ps1` solo acepta un `.aab` y BuildDev no lo genera: el APK de desarrollo pesa 73,6 MiB, por debajo de los 80 MB. `ci/check-manifest.ps1 Builds/Android/dev.apk -Mode dev`: OK.
- El cambio del Blit Type no se ve en el Editor (es un ajuste del reproductor Android): la comprobación visual la hace el portátil en el teléfono.

## 9. Clase A: el tope de densidad, el arte del inicio y el Mapa (rama `v11/memoria-clase-a`, 2026-11-16)

Por la medida del Moto G23 (`docs/dev/traspaso/pruebas-telefono-2026-11-15-moto-g23.md`, el primer teléfono de clase A): el ensayo de release da 484-516 MB de TOTAL PSS frente a los ≤ 450 de D-060, con `Graphics` en 85-102 MB (sube con el Mapa y no baja al volver al nivel) y `Code` en 100-115; el `dev.apk`, 71 MB de heap gestionado frente a ≤ 60. El dueño fue con las recomendadas de §7.5: la pregunta 1 (a), el punto 3 y mirar el Mapa. Los puntos 2 (profundidad y estencil) y 4 (asignadores), y la cadena de presentación, no se tocan: piden el teléfono o una decisión.

### 9.1 El arte por código con el tope de densidad del teléfono (§7.5, pregunta 1, opción (a))

- **Qué cambia.** `ProceduralArt.DensityCap` (px por dp del lienzo; 0 = sin tope) lo fija `GameBootstrap.ArtDensityCap()` al crear el arte (`new ProceduralArt(agua, tope)`; el arte guarda su tope en `BuiltDensityCap` y lo vuelve a poner al reutilizarse en otra escena, `Activate`, para que las piezas que pinta más tarde no mezclen densidades). Decisión [P] del integrador tras la revisión: **solo en teléfonos** (`OrientationPolicy.IsPhone` con el lado menor en dp; en tabletas, plegables abiertos y Chromebooks, que giran y cambian de ventana, sin tope), y en un teléfono, la escala del lienzo de la **pantalla entera en vertical** (`Screen.currentResolution`, el mayor de `GameScreen.CanvasScale` con los lados en un orden y en el otro), no la de la ventana: la pantalla dividida no lo baja (una ventana de 720 × 800 daría 1,25 px/dp) y arrancar en horizontal da lo mismo. Redondeado hacia arriba a 0,05 (`GameBootstrap.ArtDensityCapFor`). Cada pieza se pinta a `min(densidad fija, tope)`: `UiPxPerDp` (3), el panorama y el muelle (2,25), el follaje (3) y las superficies de los modales (1,5) pasan a propiedades con `ProceduralArt.Capped`. Como cada sprite lleva PPU = 100 × su densidad, sus medidas en dp y sus bordes 9-slice no cambian: la UI ya escala por dp.
  - En el SM-S938B y en cualquier pantalla de 3 px/dp de lienzo o más, el tope es 3 o más: **nada cambia** (el nivel sale idéntico byte a byte, §9.4).
  - En clase A (720 × 1600 a 2 px/dp), la UI, el follaje y los personajes se pintan a 2 px/dp (−56 % de píxeles), y el panorama y el muelle a 2 (−21 %); las superficies de los modales siguen a 1,5.
  - **Se quedan a su densidad fija** los atlas de tamaño fijo en px, que tienen sus celdas y sus multiplicadores calculados con 3 px/dp (`ProceduralArt.MaxUiPxPerDp`): la fauna y TiTino (`fauna_bradypus_variegatus_provisional` y `arbol_embauba_provisional`, 4 MiB), las frutas en vuelo, el rig de Capi y la tripulación del Mapa, los glifos, la píldora y el disco de la fruta de las Aguas turbias, y la cuia del Mercado en grande. Y los sprites de tamaño fijo en px, que antes llevaban el PPU con el tope sin pintarse con él (la revisión lo encontró: con tope 2 la píldora de las Aguas turbias se deformaba): los iconos de los comodines (+1 cuia, vídeo, cerrar, Luciérnaga, Morpho, la ola), los de Deshacer y Reiniciar, el engranaje, el infinito, los de Música y Sonido, la mano y la flecha del dedo del tutorial, la miniatura de las skins y la nube y la gota del chip de la Lluvia (`HudCounterChip`). También los destellos del río (2 px/dp fijos), la intro de la marca (`BootSplash` y `BrandEmblemArt`, que se sueltan al acabar) y el vidrio de las cuias, que ya iba a la densidad de la pantalla (`CuiaPixelsPerUFor`).
  - Sin densidad conocida (el Editor sin pantalla simulada con densidad, los tests que pintan arte suelto) no hay tope: los tests de siempre ven el arte de siempre. La clave del arte compartido lleva el tope. Las cachés estáticas de los chips (`HudCounterChip`, `DifficultyChip`, `MurkyChip`) guardan la densidad con la que se pintaron y se repintan si cambia; cambiar `DensityCap` a mano con arte vivo pintado a otra densidad avisa en desarrollo. El interruptor de los tests es `GameBootstrap.ArtDensityCapEnabledForTests`. Tests: `DensityCapTests` (EditMode).
- **Medido en el Editor** (`MemoryInventoryTests`, manuales; `Inventory_Moments_ClassA` simula 720 × 1600 a 2 px/dp con 48 px de recorte arriba). Texturas que crea el juego, en MiB (entre paréntesis, cuántas):

| Momento | Antes (densidad fija: igual en cualquier pantalla) | Después, clase A | Después, 3 px/dp |
|---|---|---|---|
| Inicio | 29,4 (128) | **20,7** (122) | 29,4 (igual) |
| Mapa | 35,1 | **26,2** | 35,1 |
| Avatares | 32,9 | **24,4** | 32,9 |
| Nivel 75 | 35,3 (165) | **26,4**; **24,9** con §9.2 | 35,3; ≈ 33,3 con §9.2 |
| Generación del arte al arrancar (`AF_ART procedural`, Editor) | 2.231-2.270 ms | **1.289-1.310 ms** | igual |

  Son **−8,7 MiB** de texturas en clase A (−30 %), menos que los 15-25 MB estimados en §7.5: los atlas de tamaño fijo (≈ 6 MiB, TiTino sobre todo), las superficies de los modales (1,5 px/dp, ≈ 2 MiB) y los destellos del río no bajan. En el teléfono, la cifra se verá en `tex_cur_mib` y en `GL mtrack`. De paso, el arranque pinta ≈ 40 % menos (en el Editor, −0,9 s de CPU en el bloque `services`), y con ello asigna menos basura en el heap gestionado.
- **Lo que bajaría más:** pintar los atlas de tamaño fijo con el tope (otra tarea: celdas, pivotes y los multiplicadores de `SlothRig` y `MonkeyRig` en función de la densidad; ≈ −2,2 MiB más en clase A).

### 9.2 El arte propio del inicio, suelto mientras se juega (§7.5, punto 3)

- **Corrección a §7.5:** el panorama, el muelle, los cuatro follajes y el atlas de TiTino **no** son propios del inicio: el nivel los pinta también (`GameScreen`: el fondo de la escena, y TiTino). El inventario lo dice ahora con `lobby_only` (texturas del inicio vivas en el nivel que ninguna `Image` ni `RawImage` fuera del lienzo del inicio tiene): 27 texturas y 2,0 MiB en clase A. De ellas, las que pinta solo el inicio son los destellos del río (`river_streaks`, 720 × 400, 1,1 MiB), la rama de TiTino (`home_branch`), la caja de cuias (`dock_crate`) y piezas del río de pocos KiB; el resto son variantes (los iconos «apagados», el atlas de patrones) que el nivel pone en sus Image cuando hace falta.
- **Aplicado:** al entrar en un nivel, `LobbyScreen` quita los destellos, la rama y la caja de sus Image y `ProceduralArt.ReleaseHomeScene` destruye sus texturas; al volver al inicio se repintan con los búferes de los lienzos en préstamo (`PixelBuffers`, como el Mapa), y la línea `AF_ART home_scene <ms>` (desarrollo) y `LobbyScreen.LastHomeSceneRebuildMs` dicen lo que costó. Mientras la intro del arranque tiene la rama de TiTino (`BootSplash`), no se suelta (`LobbyScreen.HomeSceneHeld`).
- **Medido:** nivel 75 en clase A, 26,4 → **24,9 MiB** (−1,5 MiB; ≈ −2 MiB a 3 px/dp). Repintarlo al volver: **59,8 ms** en el Editor a 3 px/dp (el primer repintado de la sesión, sin caché en disco; a 2 px/dp, menos): **por debajo de los ~100 ms**. En el Moto G23 puede ser 2-4 veces más (≈ 120-240 ms), en el fotograma en que aparece el inicio; lo dice la línea `AF_ART home_scene` del `dev.apk`. Los destellos del río (1,1 MiB, lo más caro) van ahora por la caché en disco del panorama (`Cached`, guía 4.9): en el teléfono, desde el segundo arranque, volver al inicio los lee de disco en lugar de repintarlos (en el Editor la caché no se usa, así que la cifra del Editor es la de repintar).
- Test: `HomeSceneReleaseTests` (se suelta al jugar un nivel y el Reto, con cada Image sin sprite; el panorama se queda; se repinta al volver con los mismos sprites que el arte; no se suelta mientras la intro tiene la rama).

### 9.3 El Mapa: lo que retiene al cerrarlo

- **Medido en el Editor** (`MapMemoryCycleTests.Diagnose_RetainedAfterClose_AndOpenCost`, manual, nivel 58, tres vueltas): tras cerrar, del Mapa queda solo su jerarquía oculta, igual en las tres vueltas: 616 GameObjects y 1.796 componentes (784 KiB en Unity), 55 mallas de textos TMP (193 KiB) y los 9 iconos de la 1.0 (0,4 MiB, §2.1). La malla compartida de UGUI (`Shared UI Mesh`) se queda en 4 vértices: no retiene la geometría del Mapa. Las texturas, los lotes de los lienzos, los `CanvasRenderer` activos y los vértices vuelven a cero (como ya decían `MapMemoryCycleTests` y el añadido de D-069), sin RenderTexture ni atlas de fuentes nuevos.
- **Coste de abrir** (la llamada y los dos fotogramas siguientes, Editor): la 1.ª apertura, 381-399 ms (lee y decodifica la geometría, construye la jerarquía y las capas del v2); la 2.ª y la 3.ª, 230-248 ms.
- **Decisión [P]: no se destruye la jerarquía al cerrar.** Soltaría ≈ 1 MiB de objetos de Unity (memoria de CPU, no `Graphics`), y cada reapertura costaría como la primera: ≈ +150 ms en el Editor (≈ +0,5 s en el Moto G23), con el riesgo de rehacer una pantalla de ~20.000 líneas que hoy se abre y se cierra con la misma referencia (235 usos en los tests). No explicaría los +16-17 MB de `Graphics` que no bajan en el Moto G23.
- **Hipótesis para el teléfono** (el Editor no ve la memoria del driver): el Moto G23 lleva una Mali-G52 (OpenGL ES 3 por D-060), y el driver guarda en una reserva por proceso las páginas de las texturas y los búferes que se liberan, para reutilizarlas: siguen contando en `GL mtrack`. El Mapa v2 crea y destruye en cada apertura ≈ 5,5 MiB de texturas (sus atlas: el río vivo, los pueblos, el rig de Capi y la tripulación) y sube la geometría de ≈ 31.500 vértices de sus lienzos; se suman los programas de sombreado que se compilan la primera vez. Nada de eso se suelta destruyendo la jerarquía. Lo bajaría que el Mapa no cree y destruya texturas (guardar sus atlas entre aperturas, con la memoria fija que eso supone) o atlas más pequeños; antes hay que medirlo (§9.5, punto 3).

### 9.4 Capturas (360 dp, densidad 2 y 3)

`DensityCapSnapshotTests` (manual, con gráficos: `unity test . --mode PlayMode --architecture x86_64 --filter AF.Tests.PlayMode.DensityCapSnapshotTests`) deja en `Logs/snapshots/es-419/density_{2|3}x_{inicio|nivel}_{fija|tope}.png` el inicio y el nivel 58 a 720 × 1600 (2 px/dp) y a 1080 × 2400 (3 px/dp), con el arte a la densidad fija (`_fija`, con `GameBootstrap.ArtDensityCapEnabledForTests` = false) y con el tope (`_tope`). A 3 px/dp el nivel sale idéntico byte a byte y el inicio solo cambia por el avatar al azar del perfil; a 2 px/dp no se ve diferencia a tamaño real (el arte ya llegaba a la pantalla a 2 px/dp, reducido por la GPU). **Pendiente:** la revisión del dueño en el teléfono de clase A, o con `wm density 320` en el S938B (§7.5, pregunta 1 (a)).

### 9.5 Lo que tiene que medir el Moto G23 para cerrar D-060

Con el ensayo de release y el `dev.apk` de esta rama, OpenGL ES 3, ART compilado (`adb shell cmd package compile -m speed-profile -f <paquete>`) y un minuto quieto en cada sitio, `dumpsys meminfo -d`:

1. **TOTAL PSS del ensayo** en la orilla (el inicio, que faltó), en el nivel 106 al arrancar, en el Mapa, en el nivel tras volver del Mapa y un minuto después: la tabla de la hoja del 2026-11-15, para comparar. El criterio de D-060: ≤ 450 MB en todos.
2. **`Graphics` y `GL mtrack`** en esos momentos y, en el `dev.apk`, `tex_cur_mib`: lo esperado son unos −9 MB de texturas frente al 2026-11-15 (§9.1) y −1,5 MB más en el nivel (§9.2).
3. **El Mapa:** tres vueltas de abrir y cerrar con `--ei af_mem_os 1` (el guion del añadido de D-069 del 2026-11-08), con el v2 y con el de la 1.0 (`--ez af_map_v1 true`). Si `GL mtrack` sube con la 1.ª apertura y no baja, pero no crece en la 2.ª y la 3.ª, es la reserva del driver (§9.3): decisión del dueño entre aceptarla o guardar los atlas del Mapa entre aperturas.
4. **`Code`** con ART compilado frente al recién instalado (100-115 MB): cuánto es transitorio.
5. **El heap gestionado** (`peak_mono_heap_mib` del `dev.apk`) tras el tutorial y el Mapa: el arranque pinta ≈ 40 % menos (§9.1), así que debería bajar de los 71,2.
6. **`AF_ART home_scene`** al volver de un nivel (§9.2) y **`AF_ART procedural`** en el arranque.
7. **La imagen:** el inicio, un nivel, el Mapa y los modales a 2 px/dp, para la revisión del dueño (§9.4).

## 10. El heap gestionado del Mapa v2 por vueltas (rama `v11/memoria-mapa-heap`, 2026-11-16)

Por la observación 2 de la prueba r2b de la 1.1 (`docs/dev/traspaso/pruebas-telefono-2026-11-16-1.1-r2b.md` §4): en el S23 Ultra, tres vueltas de abrir y cerrar el Mapa v2 desde la pausa del 67 dieron un PSS de 594,5 → 604,5 → 604,8 → 615,5 MB, con `map_after` limpio (texturas, renderers y vértices a 0, los mismos `obj_*`, `GL mtrack` fijo) y el salto de la 3.ª vuelta en el heap gestionado (`mono_heap_mib` 59,8 → 78,3). La 1.0 quedó estable (557,7 → 557,8 → 556,0).

### 10.1 Lo medido en el Editor (escritorio x64)

`MapMemoryCycleTests.Diagnose_ManagedHeapCycles` (manual: `scripts/unity-test.ps1 -Mode PlayMode -Architecture x86_64 -Filter 'AF.Tests.PlayMode.MapMemoryCycleTests.Diagnose_ManagedHeapCycles' -LogFile Logs/mapheap.log`, y `grep AF_MAPHEAP`), nivel 58, 360 × 780 dp: por vuelta, el heap usado que deja la llamada que abre el Mapa (`Profiler.GetMonoUsedSizeLong`, tras `GC.Collect`), lo que piden y lo que crean los lienzos del arte por código (`PixelBuffers`) y el heap usado tras `GC.Collect` al cerrar. Las vueltas 1-4 sueltan la reserva antes de abrir (como antes de este cambio); las 5-8 la usan.

| Vuelta | Asigna al abrir (KiB) | Lienzos: piden / nuevos (KiB) | Usado tras GC frente a la 1.ª (KiB) |
|---|---|---|---|
| 1.ª de la sesión (sin reserva) | 14.924-17.500 | 17.732-19.242 / 12.356-13.294 | 0 |
| 2.ª-4.ª, antes | 12.432-12.448 | 17.732 / **12.356** | +8 a +52 |
| 5.ª-8.ª, con la reserva | **908-928** | 17.732 / **836** | +116 a +172 (≈ 25 por vuelta) |

- **El heap usado vuelve**: no hay fuga del Mapa. Tras la 1.ª apertura se quedan ≈ 2,5-2,8 MiB de estructuras del Mapa (la geometría leída y la jerarquía, §9.3) y nada crece salvo unos 25 KiB por vuelta, que en los tests son las líneas de la sonda que guarda el registro de Unity Test Framework (≈ 5.600 caracteres de log por vuelta, con su pila); en el teléfono no se guardan. No se encontraron listas que crezcan, cachés estáticas, delegados sin quitar ni cadenas por apertura que expliquen otra cosa.
- **Lo que sí pasaba: el pico de cada apertura.** Abrir el Mapa pedía al heap unos 12,4 MiB nuevos, casi todo búferes de lienzos de 16 bytes por píxel: el del atlas de los pueblos (1024 × 512, 8 MiB) y el del río vivo (512², 4 MiB) no se servían uno a otro porque se piden en ese orden (el de 4 MiB no cabe 8 MiB) y, al acabar de abrir, `EndPooling` los soltaba. Fuera del arranque nadie los reutilizaba: cada apertura pedía otros 12 MiB. En el Editor el heap (≈ 1,5 GiB) no se mueve, pero con IL2CPP y el GC incremental, una petición de 8 MiB que llega mientras no se ha recogido la basura de la vuelta anterior (o con el hueco libre partido) hace crecer el heap, que no se devuelve: es lo que se vio en el S23 en dos de las tres vueltas.
- **El resto de la apertura** asigna ≈ 0,9 MiB (los lienzos pequeños, de menos de 128 × 128 px o de menos de 1/8 de la reserva, como los Andes y la tripulación, y lo demás de abrir): no compensa tocarlo.

### 10.2 Lo aplicado

- **La reserva del Mapa** (`PixelBuffers.EndPooling(true)` en `GameBootstrap.OpenMapPooled`): al acabar de abrir, el búfer libre más grande (el de los pueblos, 8 MiB) se queda de reserva en lugar de soltarse; la apertura siguiente lo presta (`BeginPooling`) y los lienzos grandes lo reutilizan uno tras otro (el río vivo, los primeros planos, Capi y los pueblos). Desde la 2.ª apertura, los lienzos crean **836 KiB** nuevos en vez de 12.356 y la apertura asigna **≈ 0,9 MiB** en vez de 12,4. Si el inicio repinta su escena (§9.2) con la reserva guardada, también la usa y la devuelve. `PixelBuffers.ReserveBytes` la cuenta. Con un aviso de memoria baja del sistema (`Application.lowMemory`, una sola suscripción por dominio, que no se duplica aunque la recarga de dominio esté desactivada) `PixelBuffers.ReleaseReserveForLowMemory` la suelta, también si está prestada, y deja una marca: el `EndPooling(true)` siguiente (el de la apertura en curso o el de la próxima) no la vuelve a guardar; la guarda la apertura normal posterior. `PixelBuffers.ReleaseReserve` la suelta sin marca (tests). Tests EditMode de la reserva en `MemorySettingsTests` (`Reserve_*`: la más grande se queda y nada más; se presta y se reutiliza a cero; vuelve sin crecer tras el repintado del inicio; crece con un lienzo mayor; soltada mientras está prestada; dos `BeginPooling` seguidos; y la memoria baja).
- **Decisión [P] del integrador (2026-11-16): se aceptan los 8 MiB vivos tras la 1.ª apertura del Mapa, y se confirma en el teléfono (§10.3).** Guardar 8 MiB de heap gestionado de por vida tras la 1.ª apertura del Mapa: Con IL2CPP el heap no se devuelve: tras la 1.ª apertura esos 8 MiB ya están reservados en el heap y, en el teléfono, sus páginas ya cuentan en el PSS, así que la reserva no suma PSS frente a hoy; a cambio, quita el crecimiento por vueltas (+10 MB en la 1.ª y en la 3.ª en el S23). La alternativa, forzar `GC.Collect` antes de abrir, cuesta una pausa entera del GC en cada apertura y no evita que un hueco de 8 MiB se parta con objetos pequeños; la de pintar los lienzos en `Color32` (4 bytes por píxel) cambiaría el resultado de las mezclas (otra tarea, con capturas). Con el Mapa sin abrir en la sesión no hay reserva.
- **Test** (`MapMemoryCycleTests.ThreeRounds_TheManagedHeapUsedAfterGc_DoesNotGrow_AndTheBigBuffersAreReused`, PlayMode): tres vueltas; la reserva es el búfer de los pueblos (8 MiB) y no crece; desde la 2.ª, menos de 1,5 MiB de búferes nuevos; ningún búfer libre fuera de la reserva; el heap usado tras GC de la 3.ª, a ≤ 512 KiB del de la 1.ª (medido: ≈ 30-50 KiB). Medido en ese test: 12.356 → 836 → 836 KiB nuevos.
- **Lo que no cambia:** la retención de la jerarquía y de la geometría (§9.3), `GL mtrack` (el lado gráfico, que la r2b vio fijo) y los ≈ 50 MB que el v2 suma frente a la 1.0 tras la 1.ª apertura (≈ 23,5 MB de `os_graphics_mib`, el resto del heap y nativo: la decisión de D-060 sigue abierta).

### 10.3 Lo que tiene que medir el teléfono

Con el `dev.apk` de esta rama, el guion de la r2b (§4: `pm clear`, `--ei af_mem_os 1 --ei af_max_level 66`, la base y cada vuelta en el nivel 67, pausa › «Mapa» y atrás, un minuto quieto, `dumpsys meminfo -d`), mejor **cinco vueltas** que tres:

1. **`mono_heap_mib`** de `map_after` en cada vuelta: puede subir en la 1.ª (la reserva y las estructuras del Mapa), pero **no después** (criterio: ±1 MB de la 1.ª en la 2.ª-5.ª).
2. **TOTAL PSS** tras cada vuelta: a ±3 MB de la 1.ª en la 2.ª-5.ª (el criterio de D-069). Se espera la subida de la 1.ª vuelta (≈ +10 MB) y luego plano.
3. **`GL mtrack`** y `os_graphics_mib`, como en la r2b (fijos).
4. Lo mismo con la 1.0 (`--ez af_map_v1 true`) como referencia: con la 1.0 no se pintan el río vivo, los pueblos, Capi ni la tripulación, así que la reserva queda pequeña o no existe.
