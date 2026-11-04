# Inventario del stack de producción (S0 de D-066)

Encargo: `docs/dev/traspaso/spark-marketing.md` §6, S0. Fecha: 2026-10-31. Lo que hay hoy en las dos máquinas de Spark, con licencias comerciales, tiempos medidos, lo que falta y su coste.

Convención: **[V]** medido o comprobado por nosotros · **[NV]** de una fuente que no pudimos confirmar en primera mano · **[P]** propuesta con la recomendada aplicada.

## 1. Máquinas

| Máquina | Para qué | GPU y memoria | Disco libre | Acceso |
|---|---|---|---|---|
| **Spark** (ASUS ProArt P14, Windows 11 ARM64) | Orquestación, teléfono por adb, montaje, edición, navegación para investigar | RTX Spark, 128 GB unificados (Windows ve ~112 GB como VRAM); bf16 ≈ 76 TFLOPS [V]; driver 617.42 | ~358 GB | Local |
| **5080** (`nipko`, Windows 11 x64) | Generación pesada: imagen, vídeo, voz y música | RTX 5080 16 GB, Ryzen 9 9900X, 62 GB de RAM; driver 616.64 | ~140 GB | SSH por Tailscale (`user@nipko`), ComfyUI por túnel en el puerto 8188 |

- Los tiempos están en la tabla de §3.
- **Cuándo usar cada una:** la 5080 es ~2× más rápida en lo que cabe en 16 GB, y ~5× en Wan 2.2 con SageAttention. El Spark solo gana en modelos que no caben en 16 GB.
- **Ojo con la 5080:** otra sesión de Claude corre allí las pruebas de Unity en Play Mode. Spark no toca sus procesos.

## 2. Lo instalado

### 2.1 Generación (ComfyUI 0.43.0 en las dos máquinas)

- **Spark:** ComfyUI portable `nvidia_arm64`, con Python 3.14.8 y torch 2.16 dev cu134.
- **5080:** ComfyUI portable `nvidia`, con Python 3.13.14, torch 2.14.1 cu130, triton-windows 3.8 y SageAttention 2.2.

| Modelo | Para qué | Licencia | Uso comercial (Parallext, EE. UU.) | En |
|---|---|---|---|---|
| Z-Image Turbo bf16 (11,5 GB) | Imágenes: fondos, escenografía, carteles. Escribe bien el español (tildes, ñ, ¡) [V] | Apache 2.0 | Sí | Las dos |
| Qwen-Image-2512 fp8 + Lightning 4 pasos (19 + 1,6 GB) | Imágenes. El texto en español sale mal (4 de 4 con errores [V]); no se usa para rótulos | Apache 2.0 | Sí | Las dos |
| Wan 2.2 14B imagen a vídeo, fp8 + lightx2v 4 pasos (2 × 13,3 GB) | Animar fondos y escenografía (ya usado en el vídeo de la ficha) | Apache 2.0 | Sí | Las dos |
| LTX-2.5 22B destilado int8 + escalador ×2 (20 + 14,3 GB del codificador Gemma) | Vídeo de texto a vídeo con audio ambiente, 5 s a 1280 × 704 | LTX-2 Community License: uso comercial gratis por debajo de 10 M US$ de ingresos anuales [NV] | Sí, bajo ese umbral | Las dos |
| Gemma 4 E2B int8 (4,8 GB) | Codificador de texto de las plantillas de ComfyUI | Apache 2.0 | Sí | Las dos |

**Reglas que ya aprendimos con estos modelos** (gráficos de la ficha, D-065):
- **Z-Image Turbo:**
  - funciona con cfg 1 y no admite prompt negativo;
  - escribir «sin casas» o «sin barcas» las añade, así que el prompt describe solo naturaleza;
  - se rompe por encima de unas 3 veces más ancho que alto.
- **LTX-2.5:** el audio sale bajo (media de −50 dB) y hay que normalizarlo.

### 2.2 Audio, vídeo y herramientas

| Herramienta | Versión | Licencia | En | Estado |
|---|---|---|---|---|
| FFmpeg (con NVENC h264/hevc/av1) | n9.0 | GPL (build gpl) | Las dos | Montaje, normalización a LUFS y recortes [V] |
| Pillow | 13 (dentro de ComfyUI) | HPND | Las dos | Composición de fotogramas y rótulos [V] |
| whisper.cpp + large-v3-turbo | b5454, CUDA 13.4 ARM64 | MIT | Spark | Subtítulos en 3 idiomas, en la GPU [V] |
| OBS Studio | 33.0 (Spark) y 32.1 (5080) | GPL-2.0 | Las dos | Captura de pantalla si hiciera falta |
| adb + `screenrecord` | platform-tools | — | Spark | Tomas reales del teléfono a 1080 × 2340 y 60 fps [V] |
| Node | 24.22 | — | Spark | Para las herramientas de montaje con HTML y para los playables |
| Python | 3.14.7 | — | Spark | Los scripts de la fábrica |

### 2.3 Lo que ya funciona de punta a punta [V]

Hecho y entregado en `spark/tienda-graficos` (D-065):
- **Toma real automatizada:** el teléfono juega solo, guiado por `AF_LAYOUT` y `AF_STATE` (adb `input tap`), con los extras `af_lang`, `af_level`, `af_seeds` y `af_max_level`, el modo demo y No molestar.
- **Composición:** fondos de IA alrededor de la pantalla real, rótulos en Baloo 2 y manifiesto de procedencia.
- **Vídeo:**
  - tomas reales con fondos de IA animados (Wan 2.2), cortes, cámara lenta y rótulos por idioma;
  - salidas en 9:16 y 16:9, de 30 y 15 s;
  - audio a −16 LUFS.
- **Lo que costó:** el vídeo de la ficha en 3 idiomas × 2 formatos × 2 duraciones, unos 2 min de montaje por idioma y ~2,3 min de GPU.

### 2.4 Publicación: Postiz autoalojado (Q-MK-3, añadido de D-066 del 2026-11-03)

Montado el 2026-11-03 en el Spark. Encargo: `docs/dev/traspaso/spark-marketing.md` §7.

- **Dónde:** Docker Engine 29.9.1 con Compose v5.6.0 dentro del Ubuntu 26.04 de WSL2 (arm64), que ya estaba instalado. No hace falta Docker Desktop.
- **Configuración versionada** en `fill-game-marketing/publicacion/`:
  - `docker-compose.yaml`: el oficial de Postiz con versiones fijadas;
  - `env.ejemplo`: la lista de las claves que pone el dueño;
  - `postiz.sh` (`arrancar`, `parar`, `reiniciar`, `estado`, `esperar`, `logs`, `prueba`);
  - `instalar-arranque.ps1`, que mantiene WSL vivo al iniciar sesión;
  - un `README.md`.
- **Servicios** (todas las imágenes arm64 [V]):

  | Servicio | Imagen | Para qué |
  |---|---|---|
  | `postiz` | `ghcr.io/gitroomhq/postiz-app:v2.25.3` (AGPL-3.0) | Panel, API y retornos OAuth, en `127.0.0.1:4007` |
  | `postiz-postgres`, `postiz-redis` | `postgres:17-alpine`, `redis:7.2` | Datos y colas de Postiz |
  | `temporal`, `temporal-postgresql`, `temporal-elasticsearch`, `temporal-ui` | `temporalio/auto-setup:1.28.1`, `postgres:16`, `elasticsearch:7.17.27`, `temporalio/ui:2.34.0` | Flujos de Postiz (obligatorio desde la 2.12). El panel de Temporal solo está en `127.0.0.1:8080` |
  | `cloudflared` (perfil `tunel`) | `cloudflare/cloudflared:2026.10.5` | Túnel `social.tintalu.com` → `http://postiz:5000`. Arranca solo cuando `.env` tiene el token |
  | `secretos` (de un solo uso) | la de Postiz | Genera la clave JWT y la contraseña de Postgres en un volumen de Docker la primera vez |

- **Sin secretos en git:**
  - las claves internas viven solo en el volumen `postiz_secretos` (permisos 600; nadie las ha leído);
  - las del dueño (el túnel, R2, Meta, Google y TikTok) van en `publicacion\.env`, que bloquea `.gitignore`;
  - `postiz.sh estado` solo dice si cada clave está «puesta» o «falta».
- **Seguridad:**
  - ningún puerto fuera de `127.0.0.1`, así que solo se entra desde fuera por el túnel;
  - `DISABLE_REGISTRATION=true`: solo vale el primer registro, el del administrador (lo crea el dueño), y desactiva el inicio de sesión con OAuth.
- **Medios:** se guardan en local hasta que el dueño ponga las claves de R2 y cambie `STORAGE_PROVIDER=cloudflare`. Después, en el bucket `tintalu-media`, servido en `https://media.tintalu.com/`.
- **Comprobado el 2026-11-03 [V]:**
  - los 7 contenedores están sanos;
  - `http://127.0.0.1:4007/auth` responde 200;
  - con la imagen ya bajada, arranca en menos de 1 minuto;
  - el túnel registra 4 conexiones (sjc05 y sjc06) con la ruta `social.tintalu.com` → `http://postiz:5000`. A través de Cloudflare: `/auth` → 200 («Postiz Register») y `/api/` → 200.
- **Ojo, WSL se apaga solo** cuando no queda ningún proceso de Windows usándolo, y con él se paran los contenedores. `instalar-arranque.ps1` crea en la carpeta Inicio un acceso directo con `conhost --headless wsl -d Ubuntu --exec sleep infinity`. **Lo ejecuta el dueño:** el modo automático de Claude no deja crear tareas de inicio de sesión. Sin él, Postiz solo funciona mientras haya una sesión de WSL abierta.
- **Hecho por el dueño el 2026-11-03** [V]:
  - el túnel y su ruta;
  - el administrador de Postiz, con el registro ya cerrado (`can-register` → `false`);
  - las 4 redes conectadas y activas en Postiz:

    | Red | App | Modo |
    |---|---|---|
    | Facebook e Instagram | Meta `tintalu-postiz`, de tipo Negocio, en Live | Acceso estándar con las cuentas del dueño |
    | YouTube | Google Cloud `tintalu-postiz` | Prueba sin verificar: las subidas salen privadas y hay que reconectar cada 7 días |
    | TikTok | TikTok for Developers `Tintalu Postiz`, organización Parallext, tipo Other, en Sandbox **para siempre** (ver abajo) | Postiz envía en modo `UPLOAD`: el vídeo llega a los borradores de la app de TikTok y el dueño lo publica desde allí. `tintalu.com` está verificado con un TXT (añadido; no se tocó ningún registro existente) |

- **Los scopes de TikTok** que pide Postiz v2.25.3 en su código son `user.info.basic`, `user.info.profile`, `user.info.stats`, `video.list`, `video.upload` y `video.publish`. La guía de Postiz cita además `video.create`, que ya no existe en TikTok.
- **TikTok se queda en Sandbox y publica por borradores** (decisión del dueño, 2026-11-03):
  - **No se pide la revisión de Production.** Las pautas de TikTok rechazan las apps de uso privado (*"Apps must not be for private or personal use"*, App Review Guidelines). Las de Content Posting excluyen también las herramientas que suben contenido a las cuentas propias o del equipo (*"internal groups/private use"*). Postiz en el Spark es justo eso, y presentarlo como app pública sería falsear la solicitud.
  - **En cada publicación de TikTok**, Postiz usa `content_posting_method = UPLOAD` (ajuste de la red en Postiz). El vídeo llega a la bandeja de borradores; el dueño elige allí el sonido y lo publica en público. Así da el último visto bueno (Q-MK-5).
  - **Sin comprobar** [no V]: si una app en Sandbox puede dejar borradores en una cuenta pública. Lo dirá la primera prueba.
  - **Respaldo si falla:** el programador de TikTok Studio en la web, gratis y hasta 10 días de antelación. La fábrica deja los MP4 y el dueño los programa allí.
- **R2, hecho el 2026-11-03** [V]:
  - el bucket `tintalu-media` (ENAM), con el dominio propio `media.tintalu.com` (Active) y sin URL `r2.dev`;
  - un token limitado a ese bucket (Object Read & Write), con `STORAGE_PROVIDER=cloudflare`;
  - `postiz.sh prueba-r2` sube un archivo con el SDK de Postiz, lo lee por `https://media.tintalu.com/` (200, con el contenido correcto) y lo borra.
- **Pendiente:**
  1. `instalar-arranque.ps1`;
  2. las auditorías de YouTube (verificación de OAuth y auditoría de cumplimiento). Spark prepara el texto y el vídeo de demostración. TikTok no lleva revisión (ver arriba);
  3. conectar `cola.py` con la API de Postiz (S7).

## 3. Tiempos medidos (en caliente) [V]

| Tarea | 5080 | Spark |
|---|---|---|
| Z-Image, 1 imagen de 1024² (8 pasos) | 5,3-5,8 s | 10,5 s |
| Qwen-Image Lightning, 1328² (4 pasos) | 13,5 s | 14,5 s |
| Wan 2.2 imagen a vídeo, 640², 81 fotogramas (5 s) | 43,6 s | 223 s (sin SageAttention) |
| Wan 2.2 de un fondo de 544 × 960 o 960 × 544 | 66-68 s | — |
| LTX-2.5, 5 s a 1280 × 704 con audio | 48,3 s (67,5 en frío) | 94 s (116 en frío) |
| Montaje de 30 s en 9:16 + 16:9 con Pillow y FFmpeg | — | ~100 s |

**Capacidad orientativa por día de GPU de la 5080:** ~6.000 imágenes, o ~1.200 clips de Wan de 5 s, o ~1.700 de LTX-2.5. Sobra para el volumen de §4 del encargo. El cuello de botella son las tomas reales y la revisión, no la GPU.

## 4. Lo que falta y cuánto cuesta [P]

Todo con pesos de licencia comercial verificada (investigación del 2026-10-31, fuentes en §5). Ninguno se ha bajado todavía: **cada descarga necesita el permiso del dueño**.

| Hueco | Recomendada | Licencia | Tamaño aprox. | Coste | Para qué tarea |
|---|---|---|---|---|---|
| Voz en off neutra en es-419, pt-BR y en (Q-MK-6) | **Qwen3-TTS 1.7B VoiceDesign**: voces inventadas a partir de una descripción, sin clonar a nadie. Alternativa: Chatterbox Multilingual V3, que tiene ajustes `es-mx-latam` y `pt-br` y lleva marca de agua; nunca con `audio_prompt`, que es clonar | Apache 2.0 / MIT | ~4 GB [NV] / ~2 GB [NV] | 0 | S5: el pilar «El viaje» |
| Música para anuncios distinta del juego | **ACE-Step 1.5 XL** (nativo en ComfyUI). Respaldo: Stable Audio 3 (gratis por debajo de 1 M US$ de ingresos) | MIT | ~10 GB [NV] | 0 | **No se usa hasta que lo apruebe el dueño** (Q-MK-S0-1) |
| Editar imagen con personajes fieles (poses de cartel, D-058) | **Qwen-Image-Edit-2511** en fp8. Para variantes rápidas, FLUX.2 klein 4B | Apache 2.0 | ~20 GB / ~13 GB | 0 | S3 y S5: personajes en cartel. Hay que evitar FLUX.2 klein 9B, FLUX.2 dev, Kontext dev y Qwen-Image-2.1, que no son comerciales |
| Visión local para el control de calidad (§5 del encargo) | **Qwen3-VL-8B** en la 5080, para comparar fotogramas con capturas reales y leer el texto de la imagen. Gemma 4 26B en el Spark para la revisión fina | Apache 2.0 | ~9 GB (Q8) / ~16 GB | 0 | S4 |
| Montaje programático con plantillas | **HyperFrames** (HTML/CSS/GSAP a MP4, de forma determinista, pensado para agentes) sobre FFmpeg. Remotion es gratis solo hasta 3 empleados | Apache 2.0 | npm | 0 | S3 y S4: plantillas de 9:16, 1:1 y 16:9 con zona segura y subtítulos |
| Programador de publicaciones con API (Q-MK-3) | ~~Buffer Essentials (~20 US$/mes)~~. **Cambiado el 2026-11-03 (añadido de D-066): Postiz autoalojado en el Spark**, con las APIs oficiales de Meta, YouTube y TikTok, Cloudflare Tunnel y R2 (§2.4). Buffer queda de respaldo | AGPL-3.0 (Postiz) | ~4 GB de imágenes Docker [V] | 0 | S7. **Ya montado** (§2.4). Las apps de desarrollador, los tokens y el DNS los pone el dueño |
| Sesión en TikTok for Business | Para ver todos los Top Ads de TikTok Creative Center: sin iniciar sesión solo se ven 4 | — | — | 0 | S1, trabajo semanal. **La cuenta la crea el dueño** (Q-MK-2) |
| Repositorio de la fábrica | `fill-game-marketing`, privado (Q-MK-7) | — | — | 0 | S4 a S9. **Ya existe** (lo creó el dueño, D-067). Clon en el Spark: `C:\AI\marketing\fill-game-marketing\`, con `tendencias/` y los scripts de la ficha (`tienda/`). Los datos de terceros siguen fuera de git |

- **Total mensual fijo: 0 US$.** Postiz, Cloudflare Tunnel y R2 (capa gratuita) sustituyen a Buffer (~20 US$/mes). El resto es software libre en hardware propio. Si Postiz no se sostiene, el respaldo es Buffer Essentials (~20 US$/mes [NV]), con el dueño de acuerdo.
- **Descargas:** ~45 GB en total. Caben en las dos máquinas (358 y 140 GB libres).

**Descartados, con su motivo:**
- **Pesos no comerciales:** Higgs Audio v3, Fish Audio S2, F5-TTS y XTTS-v2 (voz); MusicGen y YuE2-3B (música); FLUX.2 dev y klein 9B, FLUX.1 Kontext dev y Qwen-Image-2.1 (imagen).
- **VibeVoice:** retirado por Microsoft.
- **CapCut:** sus términos ceden una licencia amplia sobre el contenido, y su música no vale para anuncios.
- **DaVinci Resolve gratis:** desde la 21.1, el scripting exige Studio.

## 5. Preguntas [P]

- **Q-MK-S0-1, ¿la música generada con IA cuenta como «música propia» (§0.5)?**
  - **Recomendada:** sí, con ACE-Step 1.5 XL (MIT), una pista por concepto y anotada en el manifiesto. Así tenemos música con cesión total para los anuncios mientras llega la del encargo de 07 §9.5.
  - **No está aplicada:** §0 es lo que «no se negocia» y ampliarlo lo decide el dueño.
  - **Mientras tanto:** los lechos y temas del juego (CC0) y las librerías comerciales de TikTok y YouTube.
- **Q-MK-S0-2, las descargas de §4.**
  - **Recomendada:** bajar Qwen3-TTS, Qwen3-VL-8B y Qwen-Image-Edit-2511 en la semana 2 (S3 y S4), y ACE-Step si se aprueba Q-MK-S0-1. Todo en la 5080, comprobando el SHA-256, como en la fase 0.
  - Cada descarga se pide al dueño antes de hacerla.

## 6. Fuentes

- **Nuestras medidas:** las sesiones del 2026-10-27, el 2026-10-28 y el 2026-10-30 en el Spark y la 5080, y la entrega del vídeo de la ficha (`docs/store-listing/video/manifest.json` en `spark/tienda-graficos`).
- **Licencias:**
  - Qwen3-TTS: github.com/QwenLM/Qwen3-TTS
  - Chatterbox: huggingface.co/ResembleAI/chatterbox
  - ACE-Step 1.5 XL: huggingface.co/ace-step/acestep-v15-xl-base y blog.comfy.org
  - Stability Community License: stability.ai/license
  - Qwen-Image-Edit: comfyui-wiki.com
  - FLUX.2: github.com/black-forest-labs/flux2/wiki
  - Gemma 4 con Apache 2.0: opensource.googleblog.com, 2026/03
  - Remotion: remotion.dev/docs/license
  - HyperFrames: github.com/heygen-com/hyperframes
  - Resolve 21.1: notas de Blackmagic
  - CapCut: capcut.com/clause/material-license-agreement
  - Buffer (respaldo): buffer.com/pricing
  - Postiz: docs.postiz.com (instalación con Docker Compose y referencia de configuración) y github.com/gitroomhq/postiz-docker-compose
  - Ayrshare: ayrshare.com/pricing
- **Requisitos de los playables** (para S6):
  - Google Ads: support.google.com/google-ads/answer/9981650
  - Unity: docs.unity.com/grow/en-us/acquire/creatives/playable/specifications
  - TikTok: ads.tiktok.com/help, «Pangle ad assets»
  - Meta: la especificación oficial pide iniciar sesión [NV]
  - **Resumen:** objetivo ≤ 2 MB, para que valga en todas las redes. Un solo código fuente con un adaptador del botón de la tienda por red (`ExitApi.exit()`, `mraid.open()`, `FbPlayableAd.onCTAClick()` y `window.openAppStore()`).
