# Línea de producción de vídeos de Tintalú (2026-11-05)

Pedido del dueño: con lo que funciona en el mercado (cualquier género), dar forma a los primeros vídeos y fijar una línea clara para toda la producción, diciendo qué herramienta falta y cómo suplirla.

- **Base:** `tendencias/guiones-que-funcionan.md`, con 19 anuncios desmontados y sus cifras, y `tendencias/2026-45-ampliado.md`.
- **Marco:** `estrategia.md` (la parrilla semanal y los KPIs), `fabrica.md` y el plan de tomas (`C:\AI\marketing\plan\plan-tomas-y-videos.md`).
- **§0 sigue mandando:**
  - solo juego real;
  - ni personas ni dramas falsos;
  - sin cifras sin fuente;
  - sin «gratis», «sin anuncios», «#1» ni «el mejor».

## 1. Las seis reglas de montaje (de lo que funciona)

1. **Premio antes de 2 s.** El primer vertido que sella, o el primer cambio de color, cae entre 0,3 y 2 s. Nunca abre un logotipo ni la intro de la marca.
2. **Un solo eje por vídeo:** medidor, transformación, escala, reto o microhistoria. Se elige uno y se nombra en el brief (`eje`).
3. **Toma larga y pocos cortes.** Juego continuo de 8 a 25 s. Solo hay cortes entre el antegancho, el juego y el cierre, o al cambiar de fase cada 4-5 s.
4. **Un medidor a la vista:** la cuia que se llena, el número de nivel o un contador de vertidos o de cuias selladas.
5. **Texto mínimo y en su sitio.** Un rótulo de gancho de 2 s como mucho, o subtítulos de una palabra al ritmo. Los textos del propio juego («¡Sellada!», victoria) valen como subtítulos.
6. **Cierre corto:** 2-4 s de tarjeta final. En orgánico, a veces sin cierre (bucle o final abierto).

## 2. Los ocho formatos de Tintalú

Cada formato tiene su referencia medida, su eje y su material del banco de tomas (`C:\AI\marketing\tomas\2026-11-04`).

| # | Formato | Referencia (cifra) | Eje | Material real | Duración | Pilar y día |
|---|---|---|---|---|---|---|
| F1 | **Premio a los 2 s** | Block Blast 596 k e Ivy 549 k en la UE | medidor | `s1-llegada-*` (niveles enteros con victoria) | 20-25 s | Satisfying · lunes |
| F2 | **Sumergir y teñir** (antegancho de transformación) | Royal Match 1,17 M (0-6 s de limpieza) | transformación | `s2-sellado-lento-l42`, `af_capture_slowmo` y el nivel 42 sin HUD | 8-12 s, en bucle | Satisfying · jueves |
| F3 | **El río sube** (cambio de fase) | Block Blast TikTok 1,02 M; Attack Hole 748 k | escala o fase | `s1-banner-*` de 4-5 mundos, cortes cada 4-5 s y el Mapa al final | 20-25 s | Viaje · miércoles |
| F4 | **¿Lo resuelves?** (reto estático) | Candy Crush 585 k; Block Blast «easiest-looking move» 605 k | reto | `s2-reto-l46`, `s2-reto-l60` y `af_capture_stop_before 1`; la solución en la parte 2 | 10-15 s | Reto · martes |
| F5 | **Más difícil de lo que parece** (atasco real) | Coloring Match 319 k; Stacky Dash 948 k | reto | `atasco.py` (jugada obvia que bloquea) + `resolver.py` (la buena), con `director.py` | 15-20 s | Reto · viernes |
| F6 | **Mismo río, dos jugadas** (pantalla partida) | sortingsim 144 k en Instagram | reto | el Reto del río del día dos veces: solución larga y la mínima del solver | 15-20 s | Reto o comunidad · sábado |
| F7 | **Microhistoria de un personaje** (meme o rótulos) | Cuenta de Block Blast 2,25 M; Rent Please 239 k | microhistoria | TiTino en casa, Capi remando, Rosa río arriba, con un rótulo «relatable» por golpe | 8-15 s | Personajes · sábado (impares) |
| F8 | **Viaje sobre el himno** (los 5 ya montados) | Merge and Dig 403 k (bucle de progreso) | progreso | `s1-llegada-*` + Mapa + himno B con s33 | 30 s | Viaje · miércoles |

**Público:** el agua llega a mujeres de 35-64; el flujo y los rescates, a hombres de 25-44.
- Para el público de mujeres: F1, F2 y F3.
- Para el de hombres: F4, F5 y F6, con más reto y velocidad.
- Lo medimos con los KPIs de `estrategia.md` §6 en lugar de suponerlo.

## 3. Los primeros vídeos a publicar (lote 1, en es-419)

Orden pensado para salir con lo que ya existe y sumar formatos según lleguen las herramientas.

| Orden | Vídeo | Formato | Estado | Herramienta |
|---|---|---|---|---|
| 1-5 | **Los 5 del viaje** | F8 | montados y con QA aprobado (`C:\AI\marketing\revision\viaje-es`); esperan tu revisión | ya existe |
| 6 | **«Sella a los 2 s»**: nivel 50 o 70 desde un tablero casi resuelto, 22 s, sin texto, tarjeta final | F1 | por montar | `montar.py` (ya existe); falta el contador (H1), opcional en la v1 |
| 7 | **«De gris a color»**: sellado lento del nivel 42, 10 s, en bucle | F2 | por montar | `montar.py` + el bucle sin costura (H4) |
| 8 | **«¿Lo resuelves?»**: el nivel 46 del reto, parado a 1 vertido, 12 s, rótulo «¿Cuál es el último?» | F4 | por montar | `montar.py`; la parte 2 con la solución |
| 9 | **«Más difícil de lo que parece»**: tablero real con atasco, 18 s | F5 | por grabar (`director.py` con la secuencia de `atasco.py`) | ya existe |
| 10 | **«El río sube»**: cambio de mundo cada 4 s, 22 s | F3 | por montar | `montar_himno.py` con cortes secos; mejor con la transición de color (H2) |
| 11 | **«Mismo río, dos jugadas»** | F6 | por montar | falta la pantalla partida (H3) |
| 12 | **TiTino «cuando por fin…»** | F7 | por grabar | faltan las tomas de personajes (H6) |

pt-BR y en, cuando apruebes los de español (decisión del 2026-11-04).

## 4. La parrilla de cada semana

| Día | Formato | Pilar |
|---|---|---|
| Lunes | F1 | Satisfying |
| Martes | F4 | Reto |
| Miércoles | F3 o F8 | Viaje |
| Jueves | F2 | Satisfying |
| Viernes | F5 | Reto |
| Sábado | F7 (semanas impares) o F6 (pares) | Personajes o comunidad |
| Domingo | F1 lento o F2 largo | Satisfying |

- **Lunes:** el informe de tendencias pone al día las cifras y los guiones. Si un formato de fuera sube, se prueba como variación la semana siguiente.
- **A las 2 semanas,** cada formato se compara por la tasa de gancho y la retención (`estrategia.md` §6.3). El peor pierde su día en favor del mejor.

## 5. Herramientas: lo que hay y lo que falta

| Necesidad (de los guiones) | Hoy | Falta | Cómo suplirlo | Esfuerzo |
|---|---|---|---|---|
| Tomas reales a medida (nivel, mundo, piel, cámara lenta, parar antes del último vertido, Mapa) | `tomar.py`, `director.py` y los extras `af_capture_*` | — | — | — |
| Encontrar el «premio» y cortar en él | `momentos.py` y los eventos `AF_CAPTURE pour` | — | — | — |
| Montaje sobre el himno, rótulos y tarjeta final | `montar.py`, `montar_himno.py` y `plantillas.py` | — | — | — |
| Control de calidad y aprobación | `calidad.py`, `vision.py`, `cola.py` y `hoja_lote.py` | — | — | — |
| **H1. Contador en pantalla** (vertidos o cuias selladas) | no | sí | Rótulo animado en `plantillas.py`, alimentado por los eventos `pour` de la toma | pequeño |
| **H2. Transición por cambio de color** (barrido o fundido de paleta entre mundos) | solo cortes secos | sí | Filtro de ffmpeg (`xfade` con `wipe`/`fade` y un tinte del color del mundo) en `montar_himno.py` | pequeño |
| **H3. Pantalla partida** (dos tomas sincronizadas con contadores) | no | sí | Modo `partida` en `montar.py`: dos tomas escaladas a media pantalla, alineadas por el primer `pour` | medio |
| **H4. Bucle sin costura** | no | sí | Buscar con `momentos.py` un fotograma final parecido al inicial y fundir 6-10 fotogramas | pequeño |
| **H5. Subtítulos cinéticos de una palabra** al ritmo del himno o de la voz | rótulos fijos | sí | Plantilla de palabra grande (Baloo 2 ExtraBold) con un «pop» de escala, colocada en los golpes del himno (tiempos ya conocidos) | pequeño |
| **H6. Tomas de personajes** (TiTino, Capi, Rosa) para F7 | solo el muelle y el viaje de Capi en el Mapa | sí | 1) Grabar el inicio (muelle) y el Mapa con `af_capture_screen`. 2) Si hace falta más, un extra nuevo de E1 para poner a un personaje en primer plano: es un cambio del juego y lo decide el integrador | medio, con dependencia del juego |
| **H7. Voz narrada** para microhistorias (estilo Hole and Fill) | voz s33 de marca; Qwen3-TTS VoiceDesign en la 5080, sin integrar | parcial | Integrar Qwen3-TTS en la fábrica con una voz de diseño propia y etiqueta de IA. Antes, la decisión Q-MK-S0-1 por escrito | medio |
| **H8. Dedo o cursor que toca** (Bus Fever, Attack Hole) | no; el juego no muestra los toques | sí | Icono de toque dibujado (no una mano real) en las coordenadas de `AF_LAYOUT` en cada `pour`. **Necesita tu visto bueno**, porque §0 prohíbe personas y un dedo dibujado es dudoso | pequeño |
| **H9. Sonido del juego en la toma** | se reponen los efectos con los eventos `pour` | — | — | — |
| **H10. Medir lo publicado** (tasa de gancho y retención por formato) | la tabla `Post` de Postiz; sin analítica de vuelta | sí | S7 (`cola.py` con la API de Postiz) y lectura semanal de la analítica de cada red, a mano las dos primeras semanas | medio |

**Prioridad propuesta:**
1. H5, H1 y H2, que son pequeñas y mejoran F1, F3 y F8 de golpe.
2. H4 (F2).
3. H3 (F6).
4. H6 y H7, que dependen de decisiones.
5. H10 antes de la semana 3.

## 6. Decisiones que necesito del dueño

1. **El dedo dibujado (H8):** ¿vale un icono de toque en pantalla o lo consideramos «persona»?
2. **Voz narrada con IA (H7):** dejar por escrito Q-MK-S0-1 (la música y la voz de IA en marketing) en `decisiones.md`.
3. **Memes de personajes (F7):** ¿el tono «relatable» con TiTino y Capi encaja con la marca? Por ejemplo, «Yo esperando a que vuelvas al nivel 42» con TiTino en casa.
4. **El orden del lote 1:** revisar los 5 del viaje y aprobar que se monten los vídeos 6 a 10.
