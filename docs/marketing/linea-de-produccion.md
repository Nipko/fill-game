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

## 1 bis. La duración (pregunta del dueño del 2026-11-05: «¿30 s es lo mejor?»)

**No.** La investigación no apoya 30 s como base para el orgánico. Datos de los 33 anuncios con cifra de la semilla 2026-45b: el alcance en la UE para Meta y los «me gusta» para TikTok, que no son comparables entre sí, así que solo sirven como orientación.

| Duración | Anuncios | Mediana | Máximo |
|---|---|---|---|
| ≤ 30 s | 13 | ~130 k | Water Sort 1,05 M (29 s) y Attack Hole 748 k (30 s) |
| 31-45 s | 7 | ~550 k | Block Blast TikTok 1,02 M (37 s) |
| 46-64 s | 13 | ~210 k | Magic Sort 2,15 M (56 s) y Royal Match 1,17 M (60 s) |

Qué significa:
- **Los largos (37-60 s) son anuncios pagados.** Ahí la red reparte por coste y el anunciante paga cada impresión. Además, la mitad de ese tiempo es una sola toma de juego en bucle. Por D-067 no hay pago hasta tener iOS, así que no son nuestro caso hoy.
- **En orgánico** (lo que vamos a publicar), los que funcionan van de **8 a 30 s**:
  - el meme de Block Blast dura 15 s y consiguió 2,25 M;
  - Party Bus, 8 s y 1,37 M;
  - el reto de Coloring Match, 22 s y 319 k.
  El algoritmo premia el porcentaje visto y la repetición, y un vídeo corto lo gana más fácil.
- **La duración la pide el formato, no una base fija:**
  - bucle (F2), 8-12 s;
  - reto (F4), 10-15 s;
  - meme (F7), 8-15 s;
  - atasco (F5), 15-20 s;
  - premio y cambio de fase (F1 y F3), 18-25 s.
- **Lo decide la medición, no la suposición:** con H10, a las 2 semanas se compara la retención de cada duración. Cuando haya pago (después de iOS), se cortan versiones de 30-45 s de los formatos que ganen.

Por eso se descartan los 30 s del himno (F8) como base.

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
| ~~F8~~ | ~~**Viaje sobre el himno**~~: **descartado por el dueño el 2026-11-05** (los 5 montados no se publican) | Merge and Dig 403 k | progreso | — | — | el miércoles pasa a F3 |

**Público:** el agua llega a mujeres de 35-64; el flujo y los rescates, a hombres de 25-44.
- Para el público de mujeres: F1, F2 y F3.
- Para el de hombres: F4, F5 y F6, con más reto y velocidad.
- Lo medimos con los KPIs de `estrategia.md` §6 en lugar de suponerlo.

## 3. Los primeros vídeos a publicar (lote 1, en es-419)

Orden pensado para salir con lo que ya existe y sumar formatos según lleguen las herramientas.

| Orden | Vídeo | Formato | Estado | Herramienta |
|---|---|---|---|---|
| — | ~~Los 5 del viaje (F8, 30 s)~~ | — | **descartados por el dueño** (2026-11-05); quedan en `C:\AI\marketing\revision\viaje-es` solo como archivo | — |
| 1 | **«Sella a los 2 s»**: nivel 50 desde el primer vertido que sella (a los 0,9 s) hasta la victoria; contador «Quedan», toques y «¡Sella!»; 24,7 s | F1 | **montado, `calidad_ok`** (`piezas/n1-01-sella`); espera tu visto bueno | `montar_himno.py` con `audio_juego` |
| 2 | **«De gris a color»**: sellado lento del nivel 42, 10 s, en bucle | F2 | por montar | `montar.py` y el bucle sin costura (H4) |
| 3 | **«¿Lo resuelves?»**: el nivel 46 del reto, 3 vertidos con toques y parado a 1 del final; «Quedan 1» y «¿Cuál es el último vertido?»; 11,5 s | F4 | **montado, `calidad_ok`** (`piezas/n1-03-lo-resuelves`); falta la parte 2 con la solución | `montar_himno.py` |
| 4 | **«Más difícil de lo que parece»**: tablero real con atasco, 18 s, con el toque dibujado | F5 | por grabar (`director.py` con la secuencia de `atasco.py`) | ya existe; H8 |
| 5 | **«El río sube»**: niveles 30, 70, 130, 160 y 220, 4 s cada uno y cada uno acaba en su vertido ganador; marea, palabras y toques; 22,2 s | F3 | **montado, `calidad_ok`** (`piezas/n1-05-el-rio-sube`) | `montar_himno.py` |
| 6 | **«Mismo río, dos jugadas»**, 18 s | F6 | por montar | falta la pantalla partida (H3) |
| 7 | **TiTino «Yo esperando a que vuelvas…»**, 10 s, tono ligero | F7 | por grabar | tomas de personajes (H6); voz narrada opcional (H7) |

pt-BR y en, cuando apruebes los de español (decisión del 2026-11-04). Cada vídeo, una vez montado, pasa por tu visto bueno antes de ir a la cola. Nada se publica sin él.

## 4. La parrilla de cada semana

| Día | Formato | Pilar |
|---|---|---|
| Lunes | F1 | Satisfying |
| Martes | F4 | Reto |
| Miércoles | F3 | Viaje |
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
| **H1. Contador en pantalla** (vertidos o cuias selladas) | **hecho 2026-11-05** (`efectos.py`, `"contador"` en el brief) | — | Rótulo animado en `plantillas.py`, alimentado por los eventos `pour` de la toma | pequeño |
| **H2. Transición por cambio de color** (barrido o fundido de paleta entre mundos) | **hecho 2026-11-05**: la «marea» (`"entrada"` de un plano) | — | Filtro de ffmpeg (`xfade` con `wipe`/`fade` y un tinte del color del mundo) en `montar_himno.py` | pequeño |
| **H3. Pantalla partida** (dos tomas sincronizadas con contadores) | no | sí | Modo `partida` en `montar.py`: dos tomas escaladas a media pantalla, alineadas por el primer `pour` | medio |
| **H4. Bucle sin costura** | no | sí | Buscar con `momentos.py` un fotograma final parecido al inicial y fundir 6-10 fotogramas | pequeño |
| **H5. Subtítulos cinéticos de una palabra** al ritmo del himno o de la voz | **hecho 2026-11-05** (`efectos.py`, `"palabras"` en el brief) | — | Plantilla de palabra grande (Baloo 2 ExtraBold) con un «pop» de escala, colocada en los golpes del himno (tiempos ya conocidos) | pequeño |
| **H6. Tomas de personajes** (TiTino, Capi, Rosa) para F7 | solo el muelle y el viaje de Capi en el Mapa | sí | 1) Grabar el inicio (muelle) y el Mapa con `af_capture_screen`. 2) Si hace falta más, un extra nuevo de E1 para poner a un personaje en primer plano: es un cambio del juego y lo decide el integrador | medio, con dependencia del juego |
| **H7. Voz narrada** para microhistorias (estilo Hole and Fill) | voz s33 de marca; Qwen3-TTS VoiceDesign en la 5080, sin integrar | parcial | Integrar Qwen3-TTS en la fábrica con una voz de diseño propia y etiqueta de IA. Antes, la decisión Q-MK-S0-1 por escrito | medio |
| **H8. Toque dibujado** (Bus Fever, Attack Hole) | **hecho 2026-11-05** (`efectos.py`, `"toques": true` en el brief) | — | Icono de toque dibujado (no una mano real) en las coordenadas de `AF_LAYOUT` en cada `pour`. **Necesita tu visto bueno**, porque §0 prohíbe personas y un dedo dibujado es dudoso | pequeño |
| **H9. Sonido del juego en la toma** | se reponen los efectos con los eventos `pour` | — | — | — |
| **H10. Medir lo publicado** (tasa de gancho y retención por formato) | la tabla `Post` de Postiz; sin analítica de vuelta | sí | S7 (`cola.py` con la API de Postiz) y lectura semanal de la analítica de cada red, a mano las dos primeras semanas | medio |

**Prioridad (actualizada el 2026-11-05 con las decisiones del dueño):**
1. ~~H5, H1 y H2~~: hechas el 2026-11-05 (`fabrica.md` §3.1).
2. ~~H8~~ (hecho el 2026-11-05) y H4, el bucle (vídeo 2).
3. H3 (vídeo 6).
4. H6 y H7 (vídeo 7). Ya no dependen de decisiones.
5. H10 antes de la semana 3.

## 6. Decisiones del dueño (todas tomadas el 2026-11-05; registro en `decisiones.md`, añadido de D-066)

1. **Toque dibujado (H8):** sí, como icono estilizado (un círculo que pulsa o un puntero plano), nunca una mano realista.
2. **Q-MK-S0-1:** voz y música de IA permitidas, siempre con la etiqueta de cada red. La voz es diseñada y no clona a nadie.
3. **Memes de personajes (F7):** sí, con tono ligero, sin burla ni presión para volver.
4. **Lote 1:** los 5 del viaje quedan descartados. Se rehace con los formatos F1-F7 y con la duración de §1 bis.
