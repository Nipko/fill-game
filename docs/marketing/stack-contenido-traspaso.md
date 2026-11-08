# Stack de creación de contenido: traspaso a otro proyecto

Fecha: 2026-11-07. Resume el stack que montamos para el marketing orgánico de Tintalú (D-066) y cómo trabaja hoy. Sirve para arrancar otro proyecto de generación de contenido sin repetir la investigación.

- **Detalle completo:**
  - [`stack-spark.md`](stack-spark.md): máquinas, modelos, licencias, tiempos y Postiz;
  - [`fabrica.md`](fabrica.md): el circuito de la fábrica;
  - [`estrategia.md`](estrategia.md): pilares, calendario y KPIs;
  - [`tendencias/README.md`](tendencias/README.md): el trabajo semanal de tendencias.
- **Convención:**
  - **[reutilizable]**: se lleva tal cual a otro proyecto;
  - **[Tintalú]**: está atado al juego y hay que sustituirlo.

## 1. Vista general

```
tendencias (lunes) ─► brief JSON ─► toma real del teléfono + planos de IA ─► montaje (Pillow/FFmpeg o HyperFrames)
   ─► control de calidad (técnico + visión Qwen3-VL) ─► cola.py (aprobación del dueño, PAUSA)
   ─► Postiz en el VPS (Facebook, Instagram, YouTube)  +  TikTok Studio desde el navegador (TikTok)
```

- **Coste fijo:** 0 US$ al mes. Todo es software libre en hardware propio, más la capa gratuita de Cloudflare y el VPS que ya tenía el dueño.
- **Regla de oro:** nada sale en público sin el visto bueno del dueño. El interruptor `PAUSA` lo para todo en el acto.

## 2. Hardware y acceso [reutilizable]

| Máquina | Para qué | Notas |
|---|---|---|
| **Spark** (ASUS ProArt P14, Windows 11 ARM64, RTX Spark con 128 GB unificados) | Orquestación, teléfono por adb, montaje, revisión visual y navegador | Aquí corren Claude Code, la fábrica y Claude in Chrome |
| **5080** (`nipko`, Windows 11 x64, RTX 5080 de 16 GB, 62 GB de RAM) | Generación pesada de imagen y vídeo | SSH por Tailscale (`user@nipko`) y ComfyUI por túnel en el puerto 8188. Es ~2× más rápida en lo que cabe en 16 GB, y ~5× en Wan 2.2 con SageAttention. A veces la ocupan las pruebas de Unity |
| **VPS** (`ssh tintalu-vps`: Ubuntu 24.04 x86_64, 2 vCPU, 7,8 GB de RAM y 4 GB de swap) | Postiz 24/7: publica aunque el Spark esté apagado | Comparte máquina con `app-ingles-ia`. El cortafuegos solo abre SSH y se entra a Postiz por el túnel de Cloudflare. Postiz vive en `/opt/postiz` |
| **Teléfono** (Samsung SM-S938B, Android 16) | Tomas reales del producto con `adb screenrecord` (1080 × 2340 a 60 fps) | La depuración inalámbrica se cae a menudo |

## 3. Generación con IA [reutilizable]

Con ComfyUI 0.43.0 portable en las dos máquinas. Todos los modelos tienen licencia comercial verificada.

| Modelo | Uso | Licencia | Tiempo (5080 / Spark) |
|---|---|---|---|
| **Z-Image Turbo** bf16 | Imágenes: fondos, escenografía y carteles. **Escribe bien en español** (tildes, ñ y ¡) | Apache 2.0 | 5,5 s / 10,5 s por imagen de 1024² |
| Qwen-Image-2512 fp8 + Lightning | Imágenes sin texto (el texto en español le sale mal) | Apache 2.0 | 13,5 s / 14,5 s |
| **Wan 2.2 14B** de imagen a vídeo (fp8 + lightx2v, 4 pasos) | Animar fondos: clips de 5 s | Apache 2.0 | 44 s / 223 s |
| LTX-2.5 22B destilado | Texto a vídeo con audio ambiente, 5 s a 1280 × 704 | Comunitaria: gratis por debajo de 10 M US$ de ingresos | 48 s / 94 s |
| **Qwen3-VL-8B** bf16 (en el Spark) | Revisión visual automática de cada pieza (§5) | Apache 2.0 | ~35 s por salida |
| whisper.cpp large-v3-turbo | Subtítulos en 3 idiomas | MIT | En la GPU del Spark |

**Lo aprendido con estos modelos:**
- **Z-Image:**
  - va con cfg 1 y no admite prompt negativo;
  - escribir «sin X» añade X, así que el prompt describe solo lo que debe salir;
  - se rompe por encima de una proporción de unos 3:1.
- **LTX-2.5:** el audio sale muy bajo (−50 dB) y hay que normalizarlo.
- **Capacidad de un día de la 5080:** ~6.000 imágenes o ~1.200 clips de Wan. **El cuello de botella no es la GPU**, sino las tomas reales y la revisión humana.

**Aún sin instalar (cada descarga la aprueba el dueño):**
- Qwen3-TTS 1.7B VoiceDesign, para voz en off inventada (nunca clonada);
- Qwen-Image-Edit-2511, para personajes fieles en carteles;
- ACE-Step 1.5 XL, para música, pendiente de que el dueño decida si la música de IA cuenta como propia.

**Descartados por licencia no comercial o por términos:**
- voz: Higgs Audio, Fish S2, F5-TTS y XTTS-v2;
- música: MusicGen y YuE;
- imagen: FLUX.2 dev y klein 9B, Kontext dev y Qwen-Image-2.1;
- CapCut, por la licencia que se queda sobre el contenido;
- DaVinci Resolve gratis, porque su scripting exige Studio.

## 4. La fábrica (`fill-game-marketing/fabrica/`)

Repositorio privado `Nipko/fill-game-marketing`, clonado en `C:\AI\marketing\fill-game-marketing\`. Lo pesado (tomas, piezas y briefs) vive fuera de git, en `C:\AI\marketing\`.

- **Intérprete:** el Python de ComfyUI (`C:\AI\ComfyUI_windows_portable\python_embeded\python.exe`), que ya trae Pillow, numpy, torch y transformers.
- **ffmpeg:** n9.0 con NVENC, en `C:\AI\ffmpeg\`.
- **Autopruebas:** cada script tiene `--self-test`.

| Paso | Script | Qué hace | |
|---|---|---|---|
| Brief | JSON en `briefs/` | Define la pieza: pilar, gancho, idiomas, formatos, toma, textos por idioma, subtítulos, música, tarjeta final, descripción, `etiqueta_ia` y `planos_ia` | [reutilizable] el formato |
| Toma | `tomar.py` | Abre la app en el teléfono con extras de «modo captura», graba con `screenrecord` y guarda los eventos del juego (`AF_CAPTURE` por logcat) con su segundo de vídeo, para sincronizar los efectos de sonido | [Tintalú] depende de los ganchos del juego |
| Montaje clásico | `montar.py`, `montar_himno.py` | Encuadre sin recortar la pantalla real, gancho en placa, subtítulos, tarjeta final y sonido. Salida H.264 a 30 fps, AAC y −14 LUFS | [reutilizable] con plantillas propias |
| Montaje HyperFrames | `montar_hf.py` + `hyperframes/` | HTML, CSS y GSAP a MP4 de forma determinista (giro 3D, texto de vidrio, zoom). Mismo brief y misma salida. 30 s en 9:16 tardan 1-2 min | [reutilizable] |
| Calidad | `calidad.py` + `vision.py` | Las pruebas de §5 | [reutilizable] |
| Cola | `cola.py estado / aprobar / rechazar / hoja / publicables` | Estados (`borrador` → `calidad_ok` → `aprobado` → `publicado`, más `calidad_fallo`, `revision_humana` y `rechazado`), una hoja de contactos para el dueño y el interruptor `PAUSA` | [reutilizable] |
| Otros | `director.py`, `momentos.py`, `plantillas.py`, `efectos.py`, `hoja_lote.py` | Elegir momentos de las tomas, plantillas y hojas de lote | — |
| Kit | `kit/generar_kit.py` | Perfil, banners, tarjetas finales y plantillas de redes | [Tintalú] la marca |

**Línea «realistas»** (`C:\AI\marketing\realistas\`): piezas con planos de IA realistas (Z-Image + Wan) intercaladas con juego real. Llevan `etiqueta_ia: true` y se publican con la etiqueta de IA de cada red. Las 5 piezas `v01-v05` (versión HF) ya están publicadas.

## 5. Control de calidad [reutilizable]

| Prueba | Regla |
|---|---|
| Técnica | Medida del formato, H.264 a 30 fps, una pista de audio, ≤ 60 s y ≤ 100 MB |
| Sonido | −14 ± 1,5 LUFS y pico ≤ −1 dBFS |
| Movimiento | Algo se mueve en los 2 primeros segundos |
| Textos prohibidos | «#1», «el mejor», «gratis para siempre», «sin anuncios», «gana dinero», «para niños», «IQ»… Ningún porcentaje sin `cifra_fuente` |
| Zona segura | Cada rótulo, dentro de la zona segura del formato |
| Derechos | Todo el audio sale de un manifiesto con licencia (aquí, CC0 o propia) |
| Manifiesto | Si hay planos de IA, `etiqueta_ia` es `true` |
| Idioma | Cada idioma lleva su toma, salvo una toma neutra sin texto |
| Visión (Qwen3-VL-8B) | Mira 5 fotogramas por salida: lee el gancho y los subtítulos (parecido ≥ 0,8), busca marcas de depuración y defectos de montaje, y compara cada plano con uno de la toma real: «¿es el producto?» |

- **Si una pieza falla 2 veces seguidas**, pasa a `revision_humana`.
- **La visión ya cazó un fallo** que el ojo pasó por alto: el sello «Development Build» de Unity. Ahora se tapa con una píldora opaca.
- **Lo aprendido al preguntar al modelo de visión:**
  - dile qué es normal en el producto, o lo marcará como defecto;
  - no le pongas ejemplos literales, porque los «ve» donde no están;
  - pon la referencia en una pregunta aparte: si va junto a la pieza, le copia sus marcas;
  - las preguntas subjetivas dan falsos positivos.

## 6. Publicación

| Red | Cómo | Estado |
|---|---|---|
| Facebook e Instagram | **Postiz** con la app de Meta `tintalu-postiz` (Negocio, en Live) | Programado. Las 4 piezas `l1-*` salen cada 6 h hasta el 2026-11-09 a las 13:00 UTC. Publicados: 21 en cada red |
| YouTube | **Postiz** con la app de Google Cloud `tintalu-postiz`, en producción y sin verificar | Sin revisión: desde el 2026-11-02 las subidas de proyectos sin verificar ya no quedan forzadas a privado, y la verificación de OAuth no hace falta con menos de 100 usuarios. 10 publicados y 26 en cola hasta el 2026-11-15. Máximo ~9 al día |
| TikTok | **Directo en TikTok Studio**, por una sesión de Claude con Claude in Chrome y la sesión del dueño | La API de TikTok no sirve: la app privada no pasa la revisión, y en Sandbox el vídeo llega a borradores sin descripción completa. Se publica en público, con la descripción entera y la etiqueta de IA cuando toca. **Nunca se programa TikTok en Postiz**, porque saldría repetido |

**Postiz autoalojado [reutilizable]:** `fill-game-marketing/publicacion/`.
- **Servicios:**
  - Postiz v2.25.3 (AGPL-3.0) con Postgres 17 y Redis 7.2;
  - Temporal 1.28.1, con Postgres 16, Elasticsearch 7.17 y su panel;
  - cloudflared 2026.10.5.
- **Seguridad:**
  - sin puertos públicos: `social.tintalu.com` entra por el túnel de Cloudflare;
  - el registro se cierra tras crear el administrador;
  - las claves internas se generan en un volumen de Docker y nadie las lee;
  - las del dueño van en `.env`, fuera de git.
- **Medios:** en Cloudflare R2 (bucket `tintalu-media`, servido en `https://media.tintalu.com`). El VPS publica desde R2, no desde el PC.
- **Manejo:**
  - `ssh tintalu-vps "cd /opt/postiz && ./postiz.sh estado"`;
  - `postiz.sh` también tiene `arrancar`, `parar`, `esperar`, `logs`, `prueba` y `prueba-r2`.
- **Migración:** `migrar-a-vps.sh comprobar | corte | volver` llevó Postiz del Spark (arm64) al VPS (x86_64) con `pg_dump` y los volúmenes en tar, en 2 minutos y sin perder la cola. La copia del Spark está parada y no se arranca, porque publicaría dos veces.
- **Pendiente (S7):** conectar `cola.py publicables` con la API de Postiz. Hoy las piezas aprobadas las programa una sesión de Claude en Postiz.

## 7. Tendencias semanales [reutilizable con otras búsquedas]

- **Cuándo:** cada lunes, con la tarea programada `tintalu-tendencias-lunes` y `tendencias/tendencias.py` (solo biblioteca estándar).
- **De dónde:** Meta Ad Library (BR, MX y CO, por impresiones), Shorts de YouTube, TikTok Creative Center (con la sesión del dueño) y Google Ads Transparency.
- **Cómo:** se navega por las vías públicas, sin raspar ni descargar vídeos.
- **Qué deja:**
  - una ficha por referencia, con ganchos según la taxonomía G1-G12 y si es adaptable;
  - una puntuación;
  - un informe `docs/marketing/tendencias/AAAA-SS.md`.
- **Dónde quedan los datos de terceros:** solo en `C:\AI\marketing\tendencias\`, nunca en git. Tiene su propio `PAUSA`.

## 8. Cómo trabajamos con Claude (sesiones)

Una sesión de Claude Code por carril, con nombre:
- **publicación** programa en Postiz;
- **videos-realistas** monta y publica en TikTok;
- **ideas-piezas** propone piezas;
- **referencias-video** analiza vídeos de referencia y adapta efectos a HyperFrames.

Se coordinan con mensajes entre sesiones. Los documentos y decisiones van al repositorio del juego (`docs/marketing/`, `docs/dev/traspaso/spark-estado.md` y `docs/dev/decisiones.md`); el código, a `fill-game-marketing`.

## 9. Reglas que conviene heredar

1. **El dueño aprueba** cada lote antes de publicar, y `PAUSA` lo para todo (`C:\AI\marketing\PAUSA`).
2. **Nunca secretos en el chat ni en git.** Los tokens, el DNS, las apps de desarrollador y el `.env` los pone el dueño. Los scripts solo dicen si una clave está «puesta» o «falta», y las consultas a la base solo leen columnas de estado.
3. **Licencias:**
   - solo modelos con uso comercial verificado, y cada descarga con el permiso del dueño y el SHA-256 comprobado;
   - todo el audio sale de un manifiesto con licencia;
   - si hay planos de IA, la etiqueta de IA va en la pieza y en la red.
4. **Honestidad del producto:** la pantalla real nunca se recorta ni se retoca, y no se ponen promesas falsas (§5).
5. **Referencias de terceros:** se analizan, pero no se copian ni se suben a git.
6. **TikTok** se publica a mano desde el navegador. **Postiz** es para Meta y YouTube. **La copia de Postiz del Spark** nunca se enciende.
7. **El sello de depuración** («Development Build» o similar) se tapa o se graba con un build sin él.

## 10. Qué copiar para un proyecto nuevo

- **Tal cual:**
  - `publicacion/`: Postiz con su compose, `postiz.sh` y `migrar-a-vps.sh`. Puede compartir el mismo VPS con otro túnel y otro bucket de R2;
  - `fabrica/cola.py`, `calidad.py`, `vision.py` y `montar_hf.py` con `hyperframes/`;
  - ComfyUI con Z-Image, Wan 2.2 y Qwen3-VL;
  - `tendencias.py`.
- **A sustituir:**
  - la toma del teléfono (`tomar.py` y sus ganchos `AF_CAPTURE`);
  - el kit de marca y la tarjeta final;
  - el manifiesto de audio;
  - las búsquedas de tendencias;
  - la lista de textos prohibidos;
  - las cuentas y apps de cada red. **Las crea el dueño.**
