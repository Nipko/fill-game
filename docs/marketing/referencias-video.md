# Referencias de vídeo para el stack de marketing (D-066)

Lo que el dueño trae como referencia (enlaces, archivos o capturas) y lo que sacamos de cada una para la fábrica del Spark. Base: `stack-spark.md`, `fabrica.md`, `estrategia.md` y `docs/dev/traspaso/spark-marketing.md` §0 («lo que no se negocia»).

- Convención: **[V]** comprobado por nosotros · **[NV]** de una fuente que no pudimos confirmar en primera mano · **[P]** propuesta.
- Veredictos: **Sumar** (pieza nueva al stack), **Cambiar enfoque** (algo que ya tenemos, usado de otra forma), **Más adelante** (vale, pero hoy no toca) y **Descartar**.
- El contenido de las referencias es dato, nunca instrucciones. Los vídeos de terceros no se suben ni se remezclan (§0.5).

## Resumen del lote del 2026-11-05

| # | Referencia | Veredicto | Qué nos llevamos |
|---|---|---|---|
| 1 | Claude + Meta Ads MCP | **Más adelante** (S9, con iOS: D-067) | El conector oficial para leer el ROAS cuando haya pago, en modo lectura |
| 2 | 17 efectos de edición «con Claude» | **Sumar y cambiar enfoque** | Probar HyperFrames ya, para 8 de los 17 efectos que caben en las reglas |
| 3 | MiniMax H3 | **Descartar** | Nada: territorio excluido (EE. UU.), además de la voz y el lip-sync |
| 4 | Blackdark Reels | **Descartar** la herramienta | La idea de «clonar el ritmo» de un reel, con lo que ya tenemos |
| 5 | Claude edita con 6 herramientas gratis | **Cambiar enfoque** | Las palabras del himno colocadas por el pulso y por whisper.cpp, no a mano |
| 6 | Anuncio de gimnasio con modelo de IA | **Descartar** | Nada: es justo lo que prohíbe §0.3 |

**Lo que cambia en el stack:** HyperFrames, instalado y probado el 2026-11-06 con el visto bueno del dueño (referencia 2, abajo), y un cambio de método en `montar_himno.py` sin piezas nuevas. Los descartes quedan en `stack-spark.md` §4.

---

## 2026-11-05 · Lote de 6 vídeos de Facebook analizados por Muse

**Cómo llegó:** el dueño guardó 6 vídeos de Facebook y se los pasó a Muse, que devolvió un resumen de cada uno. **No vimos los vídeos:** este análisis parte del resumen de Muse. Contrastamos con búsquedas web las cifras que pesan en la decisión (el conector de Meta, la licencia de H3, Remotion y HyperFrames). Los 6 son reels verticales en español, de creadores que enseñan herramientas; 3 de ellos piden un comentario a cambio del material, así que son *lead magnets* (anzuelos para captar contactos).

Lo común: **Claude como orquestador** y modelos generativos o scripts como ejecutores. Ya trabajamos así: Claude dirige la fábrica, y `tomar.py`, `director.py`, `montar*.py`, `calidad.py` y `vision.py` ejecutan. Lo que estos vídeos añaden son piezas concretas, no una arquitectura nueva.

### 1. «Claude + Meta Ads», Felipe Vergara, 17 de junio

- **Qué se ve** (según Muse): una grabación de pantalla con Claude conectado a Meta Ads. Saca los datos de las campañas, marca en verde las rentables y en rojo las que no, analiza los conjuntos de anuncios y propone un plan. El gancho es el ahorro de tiempo («de horas a menos de 10 minutos»). El formato es un tutorial con el autor en cámara; no hay más datos de ritmo, rótulos ni audio.
- **Herramienta:** el **servidor MCP oficial de Meta Ads**, en `https://mcp.facebook.com/ads`.
  - Está en beta abierta desde el 29 de abril de 2026 y es gratis durante la beta [NV, varias fuentes coinciden].
  - Tiene 29 herramientas: campañas, catálogos, cuentas, diagnóstico de píxel y CAPI, e informes de rendimiento.
  - En Claude se añade como conector personalizado, con el inicio de sesión de Facebook del administrador de la cuenta publicitaria. El acceso se abre poco a poco: puede salir «conector desactivado» aunque la configuración termine.
  - Hay una fuente que da otra dirección (`mcp.meta.com/ads/<id de negocio>`); hay que comprobarla en la ayuda de Meta Business antes de usarla [NV].
  - La metodología «3 Q's» del autor es una skill suya; no la vimos [NV].
- **Con nuestro stack:** nada que instalar. Es un conector de Claude que conecta y autoriza el dueño: los tokens son suyos (§0.8) y la cuenta, también.
- **Reglas:**
  - **D-067:** no hay publicidad pagada hasta que Tintalú esté en iOS, así que hoy no hay cuenta publicitaria que leer.
  - El conector sirve para anuncios, no para las métricas orgánicas de la página o de Instagram, que son lo que medimos ahora (`estrategia.md` §6).
  - Puede **crear y editar** anuncios. Las campañas que crea quedan en pausa hasta que un humano las active, pero el riesgo de tocar dinero existe. Cuando llegue S9, la propuesta es usarlo **solo para leer** (informes de ROAS y de creatividades) y que el dueño active siempre a mano [P].
  - En los comentarios del vídeo hay quejas de permisos que se piden una y otra vez y de desconexiones. No hay pruebas de bloqueos de cuentas por usarlo; es la herramienta oficial de Meta.
- **Veredicto: Más adelante.** Entra en S9 como la forma de leer el rendimiento de los anuncios de Meta y de elegir qué creatividades orgánicas pasan a pago. Se apunta en `stack-spark.md` §4.

### 2. «17 efectos de edición con Claude», Prompteafacil, 16 de octubre

- **Qué se ve** (según Muse):
  - **Formato:** pantalla dividida, con el autor hablando arriba y un teléfono con los ejemplos abajo.
  - **Hook:** la promesa de efectos «estilo CapCut» sin CapCut.
  - **Cierre:** pide comentar «prompts» o «Claude» para recibir un PDF con los prompts.
  - **No explica el método en el propio vídeo.** Por la forma de los efectos (tipografía, maquetas, gráficos animados), lo más probable es que Claude escriba animaciones en HTML o React y se graben a vídeo [P].
- **Con nuestro stack:** `efectos.py` ya dibuja 4 efectos con Pillow y numpy: H5 palabras, H1 contador, H2 marea y H8 toque. Los efectos 3D, de tipografía y de cámara de esta lista son caros de escribir en Pillow, y es justo lo que resuelve **HyperFrames**: HTML, CSS y GSAP a MP4 con Chrome sin ventana y FFmpeg, con skills para Claude Code. Ya estaba en `stack-spark.md` §4 como hueco sin instalar.
  - **Licencia:** Apache 2.0, sin cuotas por render ni umbral de ingresos [NV, varias fuentes coinciden].
  - **Tamaño:** paquete de npm y un Chrome sin ventana de ~150-200 MB [NV].
  - **Coste:** 0.
  - **Sin comprobar en el Spark:** que el Chrome sin ventana que baja corra nativo en Windows ARM64. Si no corre, el respaldo es el WSL arm64 que ya tiene Postiz, o la 5080.
  - **Remotion** hace lo mismo y es gratis para empresas de hasta 3 personas; por encima, unos 25 US$ por plaza al mes [NV]. Seguimos prefiriendo HyperFrames, sin umbral, salvo que falle en ARM64.
- **Los 17 efectos frente a nuestras reglas:**

  | Efecto | ¿Cabe? | Cómo, en Tintalú |
  |---|---|---|
  | Giro 3D | Sí | El teléfono con la toma real gira en 3D al entrar. La pantalla del juego no se toca |
  | Maqueta de iPhone | Sí, con un teléfono genérico | Un marco de teléfono **sin marca** (somos Android primero, y nada de logos de Apple) alrededor de la toma |
  | Zoom out | Sí | Del vertido al tablero entero, o del tablero al Mapa del río |
  | Carta con zoom | Sí | El Dato del río o la Bitácora que se acercan y se leen (texto real del juego) |
  | Glass type | Sí, y encaja con D-003 (vidrio) | Rótulos de vidrio esmerilado sobre el fondo del río |
  | Checklist | Sí, sin frases de §0.7 | «9 mundos ✓ · Reto diario ✓ · …», solo con lo que existe en la 1.0 |
  | UI world | Sí, con piezas reales | Cuias, frutas y botones recortados de capturas reales que flotan alrededor del teléfono |
  | Frame break | Sí, con cuidado | Una cuia o un render de personaje (D-058) que sale del marco del teléfono. Fuera del juego, como escenografía (§0.1) |
  | Vértigo | Quizá | El fondo de IA se aleja mientras el teléfono se acerca. Prueba barata con un fondo de Wan 2.2 |
  | Zoom digital | Sí, ya existe | `montar_himno.py` ya tiene `zoom` (recorta la pantalla sin tocar sus píxeles) y `empuje` (acercamiento lento). `fabrica.md` §3, que dice «nunca se recorta», se ha quedado atrás (corregido el 2026-11-06) |
  | Cover story | Solo con portada propia | Una «portada» de la Bitácora con nuestra marca. Nunca imitar una revista real, porque sugeriría prensa que no hubo |
  | Doodle ball | Poco | Garabatos a mano: chocan con «Selva brillante premium» |
  | Paper cut | No | D-003: sin papel ni acuarela |
  | Headline wall | No | Muro de titulares de prensa: serían inventados (§0.1, §0.7) |
  | Chart grow | No, por ahora | Una gráfica que crece necesita cifras con fuente (§0.7); hoy no tenemos cifras públicas |
  | Couch wall | No | Por lo que se describe, lleva a una persona en escena (§0.3) |
  | Green screen | No | Es un presentador recortado sobre el contenido (§0.3) |

- **Reglas:** el PDF de prompts no hace falta: los efectos se escriben desde la descripción, y pedirlo exige comentar en el post del autor. Ninguno de los 8 efectos que caben toca los píxeles del juego.
- **Veredicto: Sumar y cambiar enfoque.**
  - **Prueba propuesta** [P], cuando el dueño dé el visto bueno a la descarga:
    1. instalar HyperFrames en `fill-game-marketing` (dependencias fijadas);
    2. hacer 3 plantillas (Giro 3D, Glass type y Zoom out) sobre una toma real de `tomas/2026-11-04`;
    3. medir el render de 30 s en 9:16;
    4. compararlas con la misma pieza en Pillow, en la hoja de contactos.

    Si gana, `montar*.py` deja a HyperFrames los efectos de capa y sigue haciendo el audio y la mezcla con FFmpeg.
  - **El zoom digital** no necesita decisión: ya está en `montar_himno.py`.
- **Prueba hecha el 2026-11-06** con el visto bueno del dueño (`fill-game-marketing`, commit `5cdb36a`, carpeta `hyperframes/`) [V]:
  - **Instalado:** `hyperframes` 0.8.186 y `gsap` 3.13.0, fijados (99 MB en `node_modules`). Se eligió una versión de hace 2 semanas, porque la 0.9.x sale varias veces al día.
  - **No hizo falta bajar ningún navegador:** usa el Chrome ARM64 del sistema, que corre nativo en el Spark. FFmpeg es el nuestro.
  - **Tiene telemetría activa por defecto.** Se apaga con `hf.sh` (`DO_NOT_TRACK` y `HYPERFRAMES_NO_TELEMETRY`), que también corta la comprobación de versiones y las instalaciones automáticas. `publish`, `cloud`, `lambda` y `auth` no se usan: suben a HeyGen o a terceros.
  - **La pieza** (`prueba-01`): 9:16 de 30 s sobre la toma real `s1-llegada-l30-es` y el himno B + s33, con Giro 3D (entrada y tapando el corte entre planos), Glass type (gancho y «¡Tintalú!» con la voz) y Zoom out a la tarjeta final.
  - **Render:** 62-107 s por pieza. Salida H.264 1080 × 1920 a 30 fps, −13,9 LUFS y −1,4 dBTP: cumple las pruebas técnica y de sonido de `fabrica.md` §5. `montar.py` tarda ~100 s, pero en dos formatos, así que el tiempo es parecido.
  - **Revisión visual con Qwen3-VL-8B:** pasa. Ve el juego en los 4 planos, también con el teléfono girado a media vuelta, sin marcas ni defectos. «¡Tintalú!» solo lo lee en el recorte de su caja, como pasa en `vision.py` con los rótulos sobre mucho texto del juego.
  - **Hallazgo para la fábrica:** la píldora que tapa «Development Build» en `montar_himno.py` es petróleo al 92 %, y al ampliar se transparenta «Developm…». En la prueba va opaca. Conviene hacer lo mismo en `montar_himno.py` [P].
  - **Qué gana frente a Pillow:** 3D real con perspectiva, desenfoque de fondo (`backdrop-filter`), curvas de GSAP y capturas de control (`snapshot`) en segundos. **Qué no hace:** el audio por eventos `AF_CAPTURE`, el manifiesto ni la cola. Eso sigue en `montar*.py`, que llamaría a HyperFrames para la capa visual [P].

### 3. «MiniMax H3, el modelo de vídeo open-source», BenCorde, 2 de noviembre

- **Qué se ve** (según Muse): una presentación con demos de H3. Genera vídeo con voz realista, lip-sync y música en una sola pasada, desde texto, imagen o un vídeo de referencia. El vídeo dice que corre con 8 GB y que Fal lo hace 50× más rápido.
- **Matices** (las fuentes coinciden [NV]):
  - **Licencia:** pesos abiertos con la MiniMax H3 Community License, que no es open source. **Excluye EE. UU., la UE, el Reino Unido y Corea del Sur**: ahí hay que pedir licencia a MiniMax, que alega pleitos de copyright en EE. UU.
  - **Uso comercial:** gratis por debajo de 20 M US$ de ingresos, con la obligación de poner «MiniMax H3» en la interfaz del producto.
  - **Partes cerradas:** la pasada a 2K solo está en su API.
  - **Requisitos:** 33 B parámetros y ~42,5 GB de pesos. Con 8 GB solo corre con cuantización agresiva y peor calidad.
- **Con nuestro stack:** cabría en el Spark (128 GB), pero no hace falta. Para escenografía animada ya tenemos Wan 2.2 (Apache 2.0) y LTX-2.5 con audio ambiente (gratis por debajo de 10 M US$).
- **Reglas:**
  - **Parallext, LLC es de EE. UU.:** sin licencia aparte no podemos usarlo. Ya estaba descartado en la investigación del 2026-10-25.
  - Lo que lo distingue (personas que hablan con lip-sync) choca con §0.3 y con «sin personas».
- **Veredicto: Descartar.** Se añade a los descartados de `stack-spark.md` §4.

### 4. «Blackdark Reels», Blackdarketing, 2 de noviembre

- **Qué se ve** (según Muse): un anuncio animado, no una demo. Promete 4 «poderes» para Claude:
  1. escribir el guion con gancho, problema y solución;
  2. hablar con tu voz;
  3. animar cada escena palabra por palabra;
  4. clonar el estilo de un reel en uno nuevo.

  No enseña el producto funcionando, ni el precio, y pide comentar «REELS».
- **Con nuestro stack:**
  - **El guion** ya lo escribe Claude con la taxonomía de ganchos G1-G12 de `investigacion-creatividades.md`.
  - **Las palabras animadas** ya las hace H5 en `efectos.py`.
  - **«Clonar el estilo»** podemos hacerlo con lo que hay, solo para estudiar referencias y sin subir ni remezclar nada (§0.5) [P]:
    - FFmpeg detecta los cortes (`select='gt(scene,0.3)'` o `scdet`) y saca el ritmo de cortes;
    - whisper.cpp saca los tiempos del texto hablado o cantado;
    - Qwen3-VL-8B describe el tipo de plano y de rótulo en cada corte.

    El resultado sería una «receta» en JSON (tiempos de corte, duración del gancho y lugar de los rótulos) que el brief usaría como plantilla. Es un script de unas 150 líneas, sin descargas. Puede alimentar la entrada de cada referencia de este documento.
- **Reglas:** «hablar con tu voz» es clonar la voz del dueño. No nos hace falta: tenemos la voz s33 y Qwen3-TTS VoiceDesign, sin clonar a nadie (`stack-spark.md` §4).
- **Veredicto: Descartar la herramienta**, sin verificar y de caja negra. **La idea de la receta** queda como propuesta [P].

### 5. «Claude edita vídeos con 6 herramientas gratis», Soy Enrique Rocha, 13 de octubre

- **Qué se ve** (según Muse): un tutorial en español con Claude Code de «director» y 6 herramientas que ejecutan. El flujo: planificar, transcribir, quitar silencios, reencuadrar, poner gráficos y exportar. Pide comentar el tipo de vídeo para recibir la configuración.
- **Herramienta por herramienta, frente a lo nuestro:**

  | Herramienta | Licencia | ¿La tenemos? | Veredicto |
  |---|---|---|---|
  | Claude Code de director | — | Sí: Claude dirige la fábrica, y las líneas `AF_CAPTURE` le dicen qué pasa y cuándo, sin adivinarlo del vídeo [V] | Ya lo hacemos, y mejor informado |
  | Whisper | MIT | Sí: whisper.cpp con large-v3-turbo en la GPU del Spark [V] | **Cambiar enfoque** (abajo) |
  | Silero VAD (voz o silencio) | MIT, ~2 MB | No | Descartar: no grabamos a nadie hablando; la voz y la música las ponemos nosotros y sabemos dónde caen |
  | Apple Vision (sujeto centrado) | Propietaria, solo macOS e iOS | No | Descartar: no hay sujeto que seguir, y la fábrica nunca recorta el tablero. La Mac mini es para iOS (D-067) |
  | Remotion (rótulos, tarjetas, stickers) | Gratis hasta 3 personas; luego de pago | No | En su lugar, HyperFrames (referencia 2) |
  | FFmpeg | GPL | Sí, n9.0 con NVENC [V] | Ya lo usamos |

- **Cambio de enfoque propuesto** [P]: hoy, las palabras de H5 (`"palabras"` del brief de `montar_himno.py`) se colocan **a mano** con `desde` y `hasta`. Con dos piezas que ya tenemos, se colocarían solas:
  1. **El pulso del himno:** el ritmo B va a 95 BPM, un tiempo cada 0,632 s. Los cortes de plano, el «pop» de cada palabra y el latido del contador (H1) caen en el tiempo más cercano. Se calcula con numpy, sin descargas.
  2. **La voz s33 con whisper.cpp:** los tiempos por palabra (`--max-len 1`, salida JSON) de `voz_s33_tratada.wav` dicen dónde se canta «Tintalú». Ahí van el logotipo o la tarjeta final.

  Ahorra el ajuste a mano de cada brief y hace que todas las piezas del himno caigan a tiempo.
- **Reglas:** el vídeo trata de recortar a una persona que habla. Nosotros no tenemos persona (§0.3) y no recortamos el juego (`fabrica.md` §3).
- **Veredicto: Cambiar enfoque.** Sin piezas nuevas; es trabajo de la fábrica.

### 6. «Anuncio de gimnasio hecho con IA», Hecho con IA, 25 de octubre

- **Qué se ve** (según Muse): un tutorial.
  1. Fotos del gimnasio real.
  2. Gemini mete a una modelo de fitness generada y borra a las personas reales.
  3. ChatGPT escribe el prompt: acciones, diálogo y 9:16.
  4. Google Flow con **Veo 3.1 Lite** genera a la modelo hablando del gimnasio y de la mensualidad.

  Cuesta 10-15 créditos por clip, con 50 gratis al día [NV].
- **Con nuestro stack:** podríamos componer escenografía generada alrededor de material real, y ya lo hacemos: fondos de Z-Image y Wan 2.2 alrededor de la pantalla real (D-065). Para eso no hace falta Veo, que es de pago, en la nube y con los términos de Google.
- **Reglas:** choca de frente con varias.
  - **§0.3:** es una persona sintética haciendo de portavoz.
  - **«Sin voz»:** habla con una voz generada que no es neutra.
  - **«Sin personas»** en el GDD.
  - **Etiquetado de IA (§0.4):** no deja claro que es IA.
- **Veredicto: Descartar.**

### Lo que se pide al dueño

1. ~~El visto bueno para la prueba de HyperFrames~~: dado el 2026-11-06; prueba hecha (arriba). Queda decidir si `montar_himno.py` pasa a usarlo para la capa visual.
2. ~~¿Se permite un zoom breve sobre el juego?~~ Ya existe en `montar_himno.py` (`zoom` y `empuje`).
3. **¿Hago las palabras y los cortes al pulso del himno** (referencia 5) y la receta de ritmo de las referencias (referencia 4)? Ninguna de las dos necesita descargas.
4. **Meta Ads MCP**, cuando llegue S9: conectarlo en modo solo lectura. Lo conecta el dueño con su cuenta.

### Fuentes de la verificación (2026-11-05)

- **Meta Ads MCP:** [guía de los 29 tools](https://pasqualepillitteri.it/en/news/1707/official-meta-ads-mcp-claude-29-tools-2026), [conectarlo a Claude](https://admanage.ai/blog/connect-meta-ads-mcp-to-claude), [comparativa con el endpoint alternativo](https://claudefa.st/blog/tools/mcp-extensions/meta-ads-mcp-comparison).
- **MiniMax H3:** [NYU Shanghai RITS](https://rits.shanghai.nyu.edu/ai/minimax-ships-h3-weights-with-the-us-and-eu-excluded/), [The Batch](https://www.deeplearning.ai/the-batch/minimaxs-state-of-the-art-video-model-is-only-minimally-open), [Runpod](https://www.runpod.io/blog/minimax-h3-the-open-weight-omni-modal-video-model-and-what-it-takes-to-run-it).
- **Remotion:** [licencia](https://remotion.dev/docs/license) y [precios](https://www.remotion.dev/docs/license/pricing).
- **HyperFrames:** [análisis](https://themenonlab.blog/blog/heygen-hyperframes-video-as-code) y [guía](https://autoae.online/blog/what-is-hyperframes).
