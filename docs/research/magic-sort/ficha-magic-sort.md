# Magic Sort!: ficha de observación (niveles 387-389)

**Proyecto:** Amazon Fill (Tintalú) · **Fecha:** 2026-10-07 · **Tipo:** material de apoyo, **no canon** (CLAUDE.md, fuente 4) · **Objeto:** *Magic Sort!* (Grand Games), jugado por el dueño en su teléfono Android.

> **Para qué sirve.** Decisión de método del dueño (2026-10-07): «no vamos a inventar la rueda: copiamos lo que funciona y cómo se hace, con nuestra temática, y lo mejoramos». Esta ficha recoge **solo lo que se vio o se oyó** en una sesión real, para que el equipo pueda copiar lo que funciona. No cambia el GDD ni las decisiones D-001 a D-004 (`docs/dev/decisiones.md`). Donde un dato choca con ellas, se dice como comparación, no como propuesta.
>
> **Convenciones.**
> - «t = N s» es el segundo de la grabación de Magic Sort. En §10, t es el segundo del vídeo de Love Water.
> - Sin marca: visto u oído. **Deducido:** inferencia razonada a partir de lo visto. **No observado:** no se ve en el material.
> - «1920» y «2340» son las dos alturas de pantalla medidas (ver §1.1). «% W» es fracción de los 1080 px de ancho; «% H», del alto. Los dp suponen 411 dp de ancho.
> - Los textos del juego van entre «» tal como salen, en inglés.

---

## Lo esencial

1. **Núcleo clásico y limpio.** Tocar origen y destino. Se vierte el **tramo entero** del color de arriba (2 y 3 dosis de una vez: t = 250, 598, 600, 838). Capacidad 4. Sin límite de movimientos ni de tiempo. **Al seleccionar no se resalta ningún destino** (t = 232).
2. **Vertido rápido y legible.** La botella sube 44 px (a 1920 de alto), vuela, se inclina con la boca justo sobre el eje del destino y vuelve. Se inclina de unos 66° (le quedan 3 dosis; hoja de vertido) a unos 90-92° (se vacía; t = 250). De salida a reposo: 0,9-1,0 s (hoja de vertido).
3. **Cada color completado destapa algo:** telas que se retiran, hielo que estalla, llaves que vuelan a un candado y armarios que revientan en astillas. Cada desbloqueo tiene su animación y su sonido.
4. **El fallo es por bloqueo, siempre.** El juego detecta solo que no hay vertido legal (16 veces). Ofrece una botella extra por 900 monedas dos veces seguidas («No Available Moves» y «Continue?»). Luego viene «Level Failed!», que enseña el premio perdido.
5. **Economía tensa a propósito.** Un nivel paga 20 monedas (100 en SUPER HARD, mostrado al perder). Deshacer y el comodín de flechas cruzadas (barajar, deducido por el icono; nunca se usó) cuestan 300; la botella extra, 900. El jugador pagó 900 una vez (t ≈ 406) y se quedó sin poder usar nada el resto de la sesión (255 monedas).
6. **Vidas:** máximo 5, una cada 30 min (deducido de 4 lecturas). Con 0 vidas: «Refill» 900 o «+1 Life» con un anuncio. Los eventos regalan **vidas ilimitadas** por minutos, que se suman.
7. **Anuncios:** 3 intersticiales (≈30 s cada uno; uno jugable) y 1 recompensado (+1 vida). El botón «Close» no respondió a algunos toques (t = 212, 638, 652). Unos 140 s de los 1207 (≈ 11-12 %) son anuncios (§1.2). No hay anuncio para continuar ni para doblar premios. No hay banner.
8. **Metajuego denso:** dos rachas (Broom Streak y Magic Streak x1-x100), clasificación (Duck Festival), carrera (Boat Rush), equipos (Team Quest), bonus diario de chisteras, Journey con premios cada 10 niveles y temáticas por tramos, cartas coleccionables. Tras cada victoria hay una cadena de 6 a 9 pantallas (premio, eventos, regalos y a veces un anuncio) antes del nivel siguiente (§4.6).
9. **Reintentar no repite el nivel:** cada intento sale con otra distribución, elegida de un conjunto que se repite (4 en el 387; al menos 5 en el 389).
10. **Sin música en la grabación** (estaba desactivada en ajustes, t = 4). Hay 20 tipos de efecto de sonido catalogados (§9).

---

## 1. Fuente y método

### 1.1 Material

| Fuente | Qué es | Uso en la ficha |
|---|---|---|
| Grabación del dueño (2026-10-07) | 1207 s (20 min 7 s), 1080 × 2340, Android Samsung, opción «mostrar toques» activa. Empieza en el nivel 387. | Toda la ficha; los t salen de aquí |
| Fotogramas `ov/` | 540 px de ancho, uno cada 2 s | Línea de tiempo |
| Fotogramas `det_ms/` | 1080 × 2340 en 65 instantes pedidos | Textos exactos y correcciones |
| `observar.json`, claves `ms[0..5]` | Observaciones de 6 agentes, un tramo cada uno | Base. Las transcripciones a resolución completa prevalecen |
| Análisis de audio (`audio/`) | STFT y correlación de forma de onda con plantillas | §9 |
| Medidas | numpy y PIL sobre PNG y JPEG | §2 |
| Capturas por adb (`Logs/device/ms-01…07`) | Mismo teléfono con la pantalla forzada a 1080 × 1920 (densidad 420), nivel 387, **antes** de la grabación | Medidas a 1920; vidas «5 Full»; colores |
| Hoja de vertido (`ms_pour_sheet.png`) | Un vertido en viñetas cada 100 ms | Pose y tiempos del vertido |
| Love Water (`lw/`, `det_lw/`) | 174 s, iPhone, sin audio, nivel 143 | §10 |

### 1.2 Qué pasó en la sesión

| Nivel | Intentos (inicio, t) | Resultado | Tramo |
|---|---|---|---|
| 387 | 6: t = 0, 9, 90, 144, 228, 342 | 1 abandono (t = 5); 4 bloqueos (t = 84, 139, 193, ≈283); 1 victoria tras pagar «Play On» 900 (t ≈ 406; gana en t = 476) | t = 0-494 |
| 388 (hielo) | 1: t = 496 | Victoria sin ayudas (t = 626) | t = 496-682 |
| 389 SUPER HARD | 12: t = 684, 752, 782, 806, 894, 938, 976, 986, 1024, 1066, 1076, 1172 | 11 bloqueos y 1 abandono (t = 1174) | t = 684-1178 |

Después: Home (t = 1178), mapa Journey (t = 1188-1202) y Home (t = 1204). Monedas: 1115 al empezar, 255 al acabar. Cuatro anuncios (§6).

**Reparto del tiempo de la sesión** (cálculo, no dato del juego). Método: cada fotograma de `ov/` (cada 2 s; cada 1 s hasta t = 28) se clasificó por su color (barra de comodines visible, negro de anuncio, escena de Home) y por la línea de tiempo de `observar.json`. Error de unos ±2 s por transición.

| Categoría | Qué incluye | Segundos (≈) | % de 1207 s |
|---|---|---|---|
| Tablero | Jugar y pensar, entradas del tablero, botella extra | ≈ 897 | ≈ 74 % |
| Anuncios | t = 196-227, 290-336, 630-660 y 906-936, con la salida a Google Play y al escritorio (t = 928-934) | ≈ 137-144 | ≈ 11-12 % |
| Fallo y salida | «No Available Moves», «Continue?», «Level Failed!», «More Lives», «Are You Sure?» y el menú de ajustes | ≈ 81 | ≈ 7 % |
| Meta y victoria | Animación de victoria, «PERFECT!», pantallas de eventos, Daily Bonus, «Congratulations!», Home (≈ 29 s) y Journey (≈ 16 s) | ≈ 77 | ≈ 6 % |
| Ofertas y tienda | Heavenly Deal y tienda (t = 1082-1091) | ≈ 8 | < 1 % |

- Del último vertido al tablero siguiente: 20 s en el 387 y 58 s en el 388, 30 s de ellos de anuncio (§4.6).
- Por nivel: el 387 ocupa t = 0-494 (6 intentos); el 388, t = 496-682 (1 intento, 130 s de juego); el 389, t = 684-1178 (12 intentos).

### 1.3 Límites del material

- La grabación empieza en el nivel 387: **no se ve el onboarding** ni la introducción de ninguna mecánica.
- Muestras de imagen cada 2 s (salvo los 65 instantes a resolución completa y la hoja de vertido). Lo que dura menos de 2 s puede no verse.
- Música desactivada por el jugador (t = 4). Los anuncios no tienen audio en la grabación.
- La zona de vídeo de los anuncios sale en negro puro (t = 200, 634).
- Hay notificaciones de otras apps encima del juego (t = 214, 326, 1028-1032). No se transcriben.
- Las barras de Android aparecen a ratos (t = 0, 210, 225, 652). No son del juego.

### 1.4 Relación con la investigación web

`docs/research/09-referencia-magic-sort.md` y `docs/research/magic-sort/a-c` resumen fuentes web (guías, reseñas, Sensor Tower). Esta ficha es **observación directa**. Las discrepancias entre las dos están en §11.2.

---

## 2. Tablero y control

### 2.1 Escala y disposición

- **El juego escala con el alto de la pantalla.** HUD, tablero y barra se dibujan centrados en x = 540 y anclados arriba. Medida a 2340 = 1,219 × medida a 1920 (error ≤ 2 px). Se comprobó con 6 elementos: cofre, engranaje, píldora, barra, botones y botellas (ms-02 frente a t = 234 s y t = 342 s).
- Consecuencia: en % del alto todo coincide en los dos formatos. En % del ancho, a 2340 todo es un 22 % mayor. La botella mide 42,2 dp de ancho a 1920 y 51,4 dp a 2340. A 2340 el cofre queda a 16 px del borde izquierdo.
- No reserva hueco para las barras de Android: con ellas visibles (t = 0) el cofre y la fila 1 están donde en modo inmersivo (t = 234).

**Orden vertical** (en % del alto; igual a 1920 y a 2340; ms-02 y t = 234):

| Elemento | % H |
|---|---|
| Cofre «Broom Streak» | 6,3-15,5 |
| Engranaje | 7,7-13,3 |
| Píldora del nivel | 15,3-20,1 |
| Fila 1 | 24,3-42,0 |
| Fila 2 | 44,9-62,6 |
| Fila 3 | 65,6-83,2 |
| Barra de comodines | 87,1-97,2 |
| Margen inferior | 2,8 |

El bloque de botellas queda centrado entre la píldora y la barra: a 1920, su centro está en y = 1032,5 y el del hueco libre en y = 1029 (53,8 % H).

**Rejilla:**

| Medida | A 1920 | A 2340 |
|---|---|---|
| Paso horizontal | 131,75 px (12,2 % W; 50,1 dp) | 160,6 px (14,9 % W; 61,1 dp) |
| Hueco entre botellas | 21 px (0,19 del ancho de la botella) | 26 px |
| Paso entre filas | 396 px (1,16 alturas de botella) | 481,5 px |
| Hueco entre filas | 55 px | 66 px |
| De la píldora a la fila 1 | 82 px | 99 px |
| De la fila 3 a la barra | 75 px | 92 px |

- Las filas están a la misma altura en los niveles 387, 388 y 389. A 2340: fila 1 en y = 570-984, fila 2 en y = 1051-1465 y fila 3 en y = 1533-1947 (t = 234, 496, 698, 1154).
- Todas las filas usan la misma botella y el mismo paso, y se centran en x = 540. Solo cambian los márgenes:

| Botellas en la fila | Ancho a 2340 | Margen por lado | Visto en |
|---|---|---|---|
| 4 | 617 px (57,1 % W) | 231 px | t = 496, 698, 1154 |
| 5 | 778 px (72,0 % W) | 151 px | t = 234 |
| 6 | 938 px (86,9 % W) | 71 px | t = 698, 812 |

- La fila corta del 387 (3 botellas) ocupa las columnas 2-4 de la fila de 5 (ms-02; t = 0).
- Todos los tableros vistos tienen 3 filas. Niveles de 2 o 4 filas, o filas de 7 botellas: **no observado**.

**Disposición por nivel:**

| Nivel | Botellas | Filas | Elementos especiales | t |
|---|---|---|---|---|
| 387 | 13 (14 tras «Add Bottle») | 5/5/3 (5/5/4) | 1 botella atada vacía (fila 1, posición 3); tela en el centro de la fila 2; tela en las 3 botellas de la fila 3; capas «?» en la fila 2 | 0, 342, 408 |
| 388 | 13 | 5/4/4 | Hielo bajo las filas 1 y 3; 8 botellas con 2 capas «?» cada una | 496 |
| 389 | 14 | 4/6/4 | Armario grande (4 botellas) arriba y 2 armarios (2 botellas cada uno) abajo; llaves; botellas atadas; «?» | 684, 1154 |

- **Colores:** el 387 y el 388 terminan con 11 botellas tapadas, así que tienen 11 colores (t = 478: 11 llenas y 3 vacías; t = 628: 11 llenas y 2 vacías). Los colores del 389: no contados.
- **Capacidad:** 4 dosis en todas las botellas vistas.
- **Sin límite** de movimientos ni de tiempo. No hay contador de movimientos (t = 190).

### 2.2 La botella

| Parte | Medida a 1920 (ms-02) | Proporción |
|---|---|---|
| Contorno exterior | 111 × 341 px (10,3 % W; 17,8 % H; 42,2 × 129,8 dp) | alto/ancho = 3,07 |
| Contorno a 2340 (t = 234, 342) | 135 × 415 px (12,5 % W; 17,7 % H; 51,4 × 157,9 dp) | 3,07 |
| Labio (reborde de la boca) | 61 × ≈30 px | 0,55 del ancho del cuerpo |
| Cuello | 45 px de ancho y solo ≈5 px de alto | 0,41 del cuerpo |
| Hombro | 36 px de alto | 0,11 de la altura |
| Cuerpo recto | 247 px | 0,72 de la altura |
| Fondo redondeado | 25 px | — |
| Pared de vidrio | ≈6 px por lado | — |
| Ancho del líquido | 99 px | 0,89 del cuerpo |
| Dosis | 62,75 px (76,5 px a 2340) | 0,184 de la altura |
| Las 4 dosis | 251 px, del fondo interior al final del hombro | 0,74 de la altura |
| Cara superior del líquido | elipse de 24 px de alto | ≈0,4 de dosis |
| Aire con la botella llena | 54 px hasta lo alto del labio | — |
| Corcho (a 2340, t = 1154) | 52-57 px de ancho; parte dorada visible de 26-30 px; asoma 10-12 px sobre el labio | 0,40 del cuerpo |
| Tela que la tapa (ms-02) | 123 px de ancho; su punta queda 10 px sobre el labio | 12 px más ancha que la botella |

- Una botella sin completar no tiene tapa, solo el labio (ms-02).
- Comprobación de la dosis: al verter 1 dosis, el destino sube 62 px (hoja de vertido, 3300-3800 ms). En ms-03, 2 dosis cian miden 125 px.
- Todas las botellas de los tres niveles miden lo mismo.

### 2.3 HUD de partida

- **Arriba a la izquierda:** el cofre azul y dorado con una escoba, el rótulo «Broom Streak» y el contador «0/2» (t = 0, 938). En el 388, en su lugar, sale el saldo «235» (t = 496). En el 387 el HUD mostró «Broom Streak 0/2» (ms-02; t = 0, 234, 342) aunque Home decía «Finished» (ms-01; t = 7, 88, 488). Qué decide si el hueco muestra la racha o el saldo: **no observado** (§7.2).
- **Arriba a la derecha:** engranaje morado.
- **Centro:** píldora «Level N» azul oscura con borde morado. En SUPER HARD lleva encima 3 calaveras, marco rojo oscuro y una cinta roja «SUPER HARD» (t = 698).
- **Abajo:** barra con 3 comodines y su precio (§5.2).
- **No se ven** vidas, ni temporizador, ni contador de movimientos. Las monedas solo se ven en los popups (t = 5, 84) salvo en el 388.

| Elemento | A 1920 | A 2340 |
|---|---|---|
| Píldora «Level 387» | 322 × 93 px (29,8 % W; 122,5 × 35,4 dp); borde ≈8 px; texto 226 × 36 px | 392 × 113 px (36,3 % W) |
| Cofre «Broom Streak» | 191 × 177 px (17,7 % W) | — |
| Engranaje | 106 × 109 px con sombra; cara morada 92 × 87 px | — |
| Barra de comodines | 552 × 194 px (51,1 % W; 210 × 74 dp); a 53 px del borde inferior | 673 × 236 px (62,3 % W); a 64 px |
| Botón de comodín | 138 px de ancho (52,5 dp); paso 178 px; hueco 40 px | ≈168 px |
| Precio | moneda de 49 px + píldora crema de 82 × 37 px; tapa el borde inferior del botón | píldora de 100 × 47 px |
| HUD de SUPER HARD (calaveras, píldora y cinta) | — | 395 × 209 px (t = 698); deja 29 px hasta el armario; el tablero no se mueve |

### 2.4 Selección

- Al tocar una botella, **solo esa botella sube**. No hay brillo, contorno ni cambio de escala, y ninguna otra botella cambia (t = 232, 598).
- Sube 44 px a 1920 (0,13 de su altura; 16,7 dp) y 52 ± 2 px a 2340 (0,125; 19,8 dp). Fuentes: hoja de vertido (1300-1400 ms) y t = 12, 186, 232, 942.
- La subida tarda 100 ms o menos (entre las viñetas de 1300 y 1400 ms ya está completa).
- Se queda arriba hasta el segundo toque: en la hoja sigue elevada de 1400 a 2800 ms.
- Si se vuelve a tocar, baja (sonido DESELECT; t = 562, 690, 870).
- Qué pasa al tocar un destino no válido: **no observado**. No se encontró ningún sonido propio para ese caso.
- Sonido SELECT: «tic» de cristal de 110 ms (§9).

### 2.5 Vertido: la regla

- Segundo toque en el destino. **Deducido:** el destino es válido si está vacío o si su color de arriba es el mismo. El juego declara bloqueo con huecos libres cuando ningún color de arriba coincide (t = 404 y t = 777).
- **Se mueven de una vez todas las dosis seguidas del color de arriba:**

| t | Qué pasa |
|---|---|
| 250 | 2 dosis moradas entran en la botella atada: pasa de 1 a 3 (t = 252) |
| 430 | 2 rojas pasan a una botella vacía |
| 598 | 3 rojas pasan a una botella vacía |
| 600 | 3 moradas sobre 1 morada: la botella se completa |
| 838 | 3 verde oscuro: el destino queda lleno |

- Vertido parcial (cuando el tramo no cabe entero en el destino): **no observado** en esta grabación.
- Para comparar: D-001 de Tintalú es una dosis por vertido. Love Water también vierte el tramo (§10).

### 2.6 Vertido: pose y tiempos

Hoja de vertido (captura por adb a 1920; botella llena que vierte 1 dosis cian de la fila 1 a la fila 2; una viñeta cada 100 ms):

| ms | Estado | Ángulo desde la vertical |
|---|---|---|
| 1300 → 1400 | Selección: sube 44 px | 0° |
| 1400-2800 | Sigue elevada, esperando el segundo toque | 0° |
| 2900 | Sale hacia el destino | 12° |
| 3000 | En vuelo | 24° |
| 3100 | La boca ya está sobre el destino | 38° |
| 3200 | Se ve el chorro | 52° |
| 3300 | Chorro; el destino sube | 62° |
| 3400 | Máximo observado | 66° |
| 3500 | Ya no hay chorro; burbujas en el destino | 52° |
| 3600 | Vuelta | 31° |
| 3700 | Vuelta, ≈50 px por debajo del reposo | 11° |
| 3800 | En su hueco, en reposo | 0° |

- **Duraciones** (resolución de 100 ms): viaje hasta la boca del destino 200-300 ms; chorro ≈300 ms; vuelta ≈300 ms. **De la salida al reposo: 0,9-1,0 s.**
- El nivel del destino sube unos 22 px cada 100 ms (de 3300 a 3600 ms). Cálculo: ≈285 ms por dosis.
- **Boca:** cae sobre el eje del destino con ±3 px de error: x = 806 frente a 803 (hoja), 538 frente a 540 (t = 250) y 622 frente a 620 (t = 776). El borde inferior de la boca queda 5-20 px sobre el labio del destino a 1920 (hoja), 13 px a 2340 en t = 250 y unos 37 px en t = 776.
- **Cuerpo:** la botella origen se dibuja **delante** de sus vecinas y no cambia de tamaño (mejor ajuste: escala 1,00, entre 0,96 y 1,08). A 66°, su centro queda unos 150 px al lado del eje del destino. En t = 250 tapa parte del rótulo «Level 387».
- **Deducido** (2 casos: hoja y t = 250): el cuerpo queda del lado del que viene la botella, y la boca apunta hacia el destino.
- **Ángulo según el llenado:** unos 66° con la botella casi llena vertiendo 1 dosis (hoja; t = 776). Unos 92° al vaciarse con 2 dosis: la boca queda un poco por debajo del fondo (t = 250). **Deducido** de 3 casos: cuanto más vacía queda, más se inclina.
- Dentro de la botella inclinada, el líquido queda horizontal (t = 250).
- **Chorro:** cinta fina del mismo color, algo inclinada, con la punta afilada al empezar (t = 250). Pasa por delante de la cuerda de la botella atada.
- **Destino:** no se mueve en todo el vertido. Su superficie hace burbujas o espuma del color (hoja, 3500-3700 ms; t = 80, 838).
- **Vuelta:** directa a su hueco, no a la posición elevada (hoja, 3700-3800 ms).
- Del sonido SELECT al inicio del sonido del vertido pasan 0,87 s de mediana (incluye el tiempo que tarda el jugador en tocar el destino).
- Ritmo del jugador: 3,1 s de mediana entre vertidos (p10 1,54 s; p90 9,6 s; mínimo 0,99 s). En la sesión hubo 221 vertidos, 244 selecciones, 19 deselecciones y 36 botellas completadas.
- Si se pueden encolar toques durante la animación: **no confirmado**. Pista: en t = 24 ya se ve el segundo vertido antes de su sonido de selección (24,74 s).
- Para comparar: D-004 de Tintalú fija 38°.

### 2.7 Sellado (botella completada)

- Con 4 dosis del mismo color, la botella recibe un **corcho dorado** y se queda en su sitio: no desaparece (t = 26-27, 362, 538-540).
- **Efecto:** una espiral o hélice de destellos del color del líquido alrededor de la botella: blanca y rosa en el marrón (t = 26), verde en el lima (t = 82), azul (t = 438, 572), violeta (t = 600) y dorada en el amarillo (t = 476, 538). A veces son dos cintas que se cruzan en X (t = 852). Después el corcho cae desde arriba (t = 540, 572).
- **Sonido** COMPLETE: empieza con el vertido. El «¡plop!» del corcho llega siempre 1,15 s después.
- **Consecuencias:** puede retirar una tela (§3.3) o romper el hielo (§3.4). Tras el último corcho de un nivel, la victoria llega 0,61 s después (§4.6).
- Si se puede tocar o mover una botella tapada: **no observado**. Deducido: queda fuera de juego.

### 2.8 Detección de bloqueo

- Es automática. Si no queda ningún vertido legal, sale «No Available Moves» (16 veces; la primera en t = 84).
- Aparece 1,32-1,6 s después del inicio del último vertido, es decir, al acabar la animación (audio).
- Cuenta como bloqueo aunque queden huecos: en t = 777 hay huecos libres, pero ningún color de arriba coincide y no hay botellas vacías. Igual en t = 404.
- Puede llegar muy pronto: tras 1 o 2 vertidos, a unos 6 s de empezar el intento (t = 894-900, 976-982, 1066-1072).
- Si detecta bucles sin progreso (movimientos posibles pero inútiles): **no observado**.
- Lo que viene después está en §4.4.

### 2.9 Variantes del mismo nivel en cada reintento

- **Nivel 387:** cuatro distribuciones con la misma plantilla (botella atada en la fila 1, posición 3; tela en el centro de la fila 2; 3 telas abajo). No son un simple cambio de unos colores por otros: el contenido es distinto (t = 9).

| Variante | Intentos (t) | Rasgos |
|---|---|---|
| A | 0, 90, 228 | Cuerda morada; tela central con frasco azul; telas de abajo verde oscuro, rosa y morado |
| B | 9 | Cuerda marrón; tela central marrón |
| C | 144 | Cuerda lavanda; telas de abajo amarilla, rosa y lima |
| D | 342 | Cuerda lavanda; telas de abajo morada, rosa y naranja (bases roja, negra y verde) |

- La variante A se repite exacta (t = 234 es igual que t = 0). Orden de los intentos: A, B, A, C, A, D.
- **Nivel 389:** al menos 5 disposiciones de salida (t = 684, 752, 782, 806, 894) que se repiten. La de t = 684 vuelve en t = 938, 1024 y 1076; la de t = 976 repite la de t = 894, y la de t = 986 la de t = 806. Se mantienen los armarios y sus candados. Cambian los colores, la botella que lleva cada llave y el color de la cuerda de las botellas atadas (azul, marrón, roja).
- Si las variantes son un conjunto fijo o se generan, y cómo se elige la siguiente: **no observado**.
- Si el juego quita un color tras fallar: **no observado** (las 4 variantes del 387 tienen 13 botellas y la misma plantilla).
- Al empezar cada intento, el tablero entra deslizándose desde la derecha (t = 9, 228).

### 2.10 Sin resaltado de destinos ni ayudas implícitas

- Al seleccionar no se resalta ningún destino (t = 232, 598). Coincide con D-002 de Tintalú.
- No se vio ninguna pista automática, mano guía ni sugerencia de jugada en estos niveles.
- Las únicas ayudas son los comodines de pago (§5.2) y la botella extra (§3.7).

---

## 3. Mecánicas especiales

| Mecánica | Nivel | Qué hace | Se libera con | Sonido propio |
|---|---|---|---|---|
| Capa oculta «?» | 387-389 | Oculta el color de una dosis | Que la dosis quede arriba | No |
| Botella atada | 387, 389 | Cuerda y etiqueta con un frasco de un color | No se desata (no observado) | No |
| Tela con icono | 387 | Tapa una botella entera | Completar una botella del color del icono | Sí (campanilla y swoosh) |
| Hielo | 388 | Encierra la base de una fila entera | Completar una botella dentro del bloque | Sí (estallido) |
| Armario con candado | 389 | Tapa 2 o 4 botellas | Llave del mismo color | Sí (candado y madera) |
| Llave en el líquido | 389 | Va dentro de una dosis | Que esa dosis quede arriba | (el del candado) |
| SUPER HARD | 389 | Nivel difícil anunciado, con premio mayor | — | No |
| Botella añadida | 387 | +1 botella vacía | Pagar 900 monedas | No aislado |

### 3.1 Capas ocultas «?»

- **Regla.** La dosis sale como un segmento negro con un «?» gris. Se revela cuando queda arriba, al verter la dosis que la tapaba (t = 28, 238, 456, 564). Si coincide con la de debajo, se funden en un bloque (t = 238).
- **Cuántas:** hasta 3 seguidas en una botella (389: t = 712, 842, 882). En el 388, las 8 botellas con «?» tienen exactamente 2, en el medio; la dosis del fondo se ve de color (t = 496).
- Bajo una tela, la base que asoma es negra si la dosis de abajo es una «?» (t = 0).
- Una dosis con llave se ve de color aunque esté entre dos «?» (t = 698).
- **Visual.** El «?» se deforma y se disuelve sobre el color nuevo, con 3 o 4 destellos blancos en forma de estrella (t = 238; no son burbujas). En otros casos, un pequeño remolino de destellos y signos «?» que se deshacen (t = 564, 956).
- **Tiempo:** menos de 2 s desde el vertido (t = 236 → 238).
- **Sonido:** ninguno propio (audio).
- Color de la capa: negro casi puro (≈#04010d en ms-02).

### 3.2 Botella atada con cuerda y etiqueta

- **Presentación.** Una cuerda trenzada en el cuello, del mismo color que el frasco dibujado en una etiqueta de papel crema. La etiqueta no lleva texto ni número (t = 238, 342, 698, 1154).
- **En el 387:** empieza vacía (t = 0). El jugador solo le echa su color (t = 13, 234, 346-362). Al llenarse recibe el corcho y la cuerda sigue puesta (t = 26, 184, 362).
- **En el 389:** empieza con dosis mezcladas. En las tres botellas atadas de t = 1154, el color de la cuerda coincide con la dosis del fondo (azul oscuro, rojo y cian). Hay dos tonos de azul distintos.
- **Puede verter:** t = 776, 862, 1150-1154.
- **Recibir otro color:** probablemente no. En t = 888-889 el único vertido aparente (azul a una atada naranja con 2 huecos) no se permite y sale «No Available Moves». No confirmado: los dos azules podrían ser distintos.
- La cuerda cambia de color con la variante: morada, marrón y lavanda en el 387; azul, marrón, roja, naranja y cian en el 389.
- **Sonido:** ninguno propio. La regla no se explica en pantalla.
- **Deducido:** es una botella que solo acepta el color de su etiqueta.

### 3.3 Botella tapada con tela

- **Presentación.** Una tela rosa pálido (≈#f4d4d9) con pliegues cubre la botella entera y lleva dibujado un frasco del color pedido. Por debajo asoma la base: el color de la dosis del fondo, o negro si es una «?» (t = 0).
- **Regla.** Al completar una botella del color del icono, la tela se retira (marrón t = 27; azul t = 58; rosa t = 130-132; lavanda t = 362; rosa t = 390; morado t = 398; naranja t = 402-404). Deducido: mientras está tapada no se juega (no se vio ningún toque sobre ella).
- **Secuencia visual:** marca verde ✓ sobre el icono (t = 56, 130, 402); un haz de luz baja desde la botella recién completada hasta la tela (t = 362); la tela se aclara y sube como un fantasma, con estrellas doradas (t = 398). Dura 1-2 s.
- **Debajo** suele haber 1 color arriba y 3 «?» (t = 28, 132, 362, 390).
- **Sonido** CLOTH_REVEAL: un «ding» limpio (1050 y 2100 Hz) en el momento del corcho (+1,15-1,2 s) y un «fsss» de tela de +1,5 a +2,1 s. La fuente da dos medidas del «fsss»: +1,65-2,1 s (catálogo) y +1,5-1,65 s (notas de tiempo). Sonó 8 veces.

### 3.4 Hielo (nivel 388)

- **Presentación.** Un bloque de hielo azul celeste facetado, continuo, envuelve la mitad inferior de las botellas de una fila entera. Las botellas se ven escarchadas. **No tiene contador, número, icono ni grietas** (t = 496).
- Medidas a 2340: fila 1 en x = 146-934 (788 px; 73 % W), y = 570-996; fila 3 en x = 224-858 (634 px), y = 1533-1964. Sobresale unos 5 px por cada lado.
- **Regla.** Las botellas congeladas reciben vertidos (t = 502). Si se pueden levantar: **no observado** (el jugador solo levantó botellas de la fila libre). Al completar una botella dentro del bloque, revienta el hielo de toda la fila: la fila 1 al completar el marrón (t = 504) y la fila 3 al completar el azul claro (t = 508-510). Las «?» siguen tapadas.
- Otro modo de romperlo: **no observado**.
- **Visual:** esquirlas azules de cristal que salen despedidas por media pantalla (t = 504).
- **Sonido** ICE_SHATTER: «crash» brillante de banda ancha, unos 700 ms, en el momento del corcho (+1,12 s). Las dos roturas usan muestras distintas.

### 3.5 Armarios de madera con candado y llaves dentro del líquido (nivel 389)

- **Presentación.** Armarios de tablones claros con marco naranja y una cinta vertical central donde cuelga un candado de color (naranja, amarillo o azul). El grande de arriba tapa 4 botellas y mide 642 × 457 px a 2340 (exactamente 4 pasos de rejilla). Los dos de abajo tapan 2 botellas cada uno y miden 310 × 464 px, separados 12 px (t = 696, 812).
- **La llave** es un icono 3D brillante dibujado sobre la banda de una dosis concreta, a menudo la del fondo (t = 684, 698, 806).
- **Regla.** Cuando la dosis con la llave queda arriba, la llave sale por la boca de la botella y vuela al candado de su color. El armario se abre y sus botellas entran en juego. La dosis se queda en la botella; solo se va la llave. Visto en t = 696 (naranja), 712 (azul), 812-814 (naranja), 840 (amarilla), 950, 968, 1034-1036, 1056-1058, 1118-1120 y 1144-1146.
- **Encadena:** detrás de un armario hay más «?», más llaves y botellas atadas (t = 698, 952).
- Si la dosis con la llave queda bajo un color sin destino, el nivel se bloquea (t = 888).
- **Visual de la llave:** brilla, sale con una estela de chispas (rosas en t = 812) y a veces sube hasta la cinta «SUPER HARD» antes de bajar al candado (t = 1056).
- **Visual del armario:** dos variantes. (a) Las puertas giran hacia fuera como bisagras, el candado cae girando con el arco suelto y salen chispas doradas (t = 764, 950). (b) Las puertas revientan en astillas de madera que vuelan por toda la pantalla, incluso sobre la barra de comodines (t = 712, 814, 968).
- **Sonido:** UNLOCK («clic-clinc» metálico, 690 ms), 0,58-0,68 s después del inicio del vertido que libera la llave; WOOD_BREAK (crujido y astillas, 1,35-1,9 s, al menos 4 variantes), 0,87 s después del UNLOCK; y un tic final muy débil. La cadena entera dura unos 3 s.

### 3.6 Niveles SUPER HARD y etiqueta «x5»

- **Se anuncian antes de jugar.** Tras ganar el 388, el botón de Home pasa de verde «Level 388» a rojo «Super Hard / Level 389», con una etiqueta violeta «x5» colgada de una anilla (t = 666).
- **En el tablero:** 3 calaveras (la del centro, más grande) sobre la píldora, marco rojo oscuro y cinta roja «SUPER HARD» (t = 684).
- **En los popups:** «Level Failed!» es una tarjeta roja (t = 748; en los niveles normales es morada, t = 6,5). «Are You Sure?» lleva la cabecera «SUPER HARD» (t = 1174). La banda de «Continue?» es granate (t = 746, 803; en el 387 era morada, t = 86).
- **Premio mostrado:** «Rewards: 100» (t = 748), frente a 20 en los niveles normales (t = 6,5 y 482). **Deducido:** la «x5» es el multiplicador de premio del SUPER HARD (20 × 5 = 100).
- La «x5» **no** es el multiplicador de Magic Streak: sigue en x5 con la racha en x1 (t = 904, 1178). En «Try Again» la etiqueta es azul marino (t = 904). Su significado exacto: **no observado**.
- **Diseño visto:** al empezar, las 6 botellas visibles tienen 3 dosis cada una y no hay ninguna vacía (t = 938). Hay bloqueos a los 6 s.

### 3.7 Botella añadida («Add Bottle»)

- Solo se vio como «Play On» en «No Available Moves» (t = 404-408). La botella vacía aparece en la fila que tiene sitio (la fila 3 pasa de 3 a 4) dentro de un anillo de chispas morado y azul.
- La fila se recentra: las 3 botellas que ya estaban se mueven medio paso, unos 80 px a 2340 (t = 402 frente a t = 410).
- El comodín de la barra (900) no se usó.

---

## 4. Flujo de partida y de fallo

### 4.1 Entrar en un nivel

- Desde Home se pulsa el botón grande del nivel: verde «Level N», o rojo «Super Hard / Level N» (t = 7, 666).
- Sonido POP_A («blup-blup») y, 0,24 s después, WHOOSH_B (entrada del tablero).
- El tablero entra deslizándose desde la derecha con el HUD ya puesto (t = 9, 228). Otra vez, primero baja desde arriba el rótulo «Level 387» sobre el fondo vacío y luego aparecen las botellas (t = 340-342).
- No hay pantalla de carga, objetivo ni resumen previo.

### 4.2 Pausa y ajustes

- El engranaje despliega una columna de 4 botones redondos, sin texto, debajo de él: sonido (altavoz verde), música (nota con barra roja: desactivada), vibración y salir (botón rojo con puerta y flecha). El tablero se oscurece (t = 4, 1173).

### 4.3 Salir del nivel

- Salir → «Are You Sure?» con un corazón roto, «You will lose 1 life!», botón rojo «Quit» y X roja. Arriba a la izquierda sale el saldo (t = 5; en t = 1174 con la cabecera «SUPER HARD»).
- «Quit» → «Level Failed!» (t = 6, 1176). Su X → Home (t = 6,5 → 7).

### 4.4 Derrota por bloqueo

```
último vertido
 └─ +1,3-1,6 s: «No Available Moves» (saldo visible; HUD de partida oculto)
     ├─ «Play On» 900 (pestaña «Add Bottle») → +1 botella y sigue el intento (t = 404-408)
     └─ «Give Up»
         └─ «Continue?»: «You will lose 1 life!» · «Play On» 900 · X
             ├─ «Play On» → no observado
             └─ X → «Level Failed!»: «Rewards: N» · «Try Again» · X
                 ├─ «Try Again» → tablero con otra variante (a veces antes un anuncio, §6)
                 │     └─ con 0 vidas → «More Lives» (t = 288)
                 └─ X → Home (t = 6,5; probable en t = 1169)
```

- **«No Available Moves»** (t = 84, 139, 404, 777, 1018). Título blanco crema con contorno dorado grueso y borde marrón. Botón verde «Play On» con moneda y «900», con una pestaña amarilla «Add Bottle» y una etiqueta azul colgante con el icono «+botella». Botón naranja «Give Up» con un corazón roto. Los dos botones entran deslizándose: el verde por la izquierda y el naranja por la derecha (t = 777, 1018). Arriba sale el saldo. Se ocultan el cofre, la píldora, el engranaje y los comodines (t = 404, 777). **No hay opción de anuncio** (t = 404).
- **«Continue?»** (t = 86, 194, 746, 803, 973, 1022, 1064, 1168). Banda de lado a lado con el título y una X roja redonda; panel crema con un corazón roto grande; texto «You will lose 1 life!»; un único botón verde «Play On» con moneda y «900». Sin anuncio. Solo se rechaza con la X.
- **«Level Failed!»** (t = 6,5, 142, 195, 286, 338, 748, 904, 974, 1023, 1065, 1169). Tarjeta con borde dorado, cabecera «Level N», franja con corazón roto y «Level Failed!», «Rewards:» con un montón de monedas sobre un pedestal y la cifra (20, o 100 en SUPER HARD), botón verde «Try Again» y X roja. En el 389, bajo la tarjeta sube desde abajo el panel de Magic Streak (t = 748, 974).
- Las monedas de «Rewards» **no se cobran** al perder: el saldo no cambia (t = 338, 904).
- «You will lose 1 life!» sale aunque haya vidas ilimitadas activas (t = 803, 973, 1174; ilimitadas desde t = 682).
- Entre t = 86 y t = 88 no se ve «Level Failed!»: el tablero se funde hacia Home. En el resto de casos sí aparece. Si en t = 86-88 duró menos de 1 s: **no observado**.
- Para reintentar sin pagar tras un bloqueo hacen falta 3 toques: «Give Up», X y «Try Again».

### 4.5 Vidas y «More Lives»

- **Máximo: 5.** La captura por adb anterior a la grabación muestra «5» y «Full» (`Logs/device/ms-01-actual.png`).
- Se pierde 1 vida al rendirse o al salir. Empezar un intento no gasta vida y ganar tampoco: la vida conseguida con un anuncio sigue ahí tras ganar (t = 488).
- **Regeneración: 1 vida cada 30:00.** Deducido: cuatro lecturas cuadran con un único ciclo que empieza en t ≈ 27 s (28:58 en t = 88; 25:39 en t = 288; 22:19 en t = 488; 19:22 en t = 664).

| t | Hecho | Vidas | Temporizador |
|---|---|---|---|
| Antes (ms-01) | Home | 5 «Full» | — |
| ≈ 0-5 | Antes de salir | 4 (deducido) | — |
| 5 | «Quit» | 3 (visto en t = 7) | 00:20 (t = 7) |
| ≈ 27 | Se regenera una | 4 (deducido) | vuelve a 30:00 (deducido) |
| 86 | Derrota | 3 (t = 88) | 28:58 (t = 88) |
| 142 y 195 | Derrotas | 2 y 1 (deducido) | — |
| 286 | Derrota | 0 (t = 288) | 25:39 (t = 288) |
| 290-336 | Anuncio «+1 Life» | 1 (deducido) | — |
| 476 | Gana el 387 | 1 (t = 488) | 22:19 (t = 488) |
| 664 | Home tras ganar el 388 | 1 | 19:22 |
| 676 y 680 | Reclama ∞ 15m y ∞ 30m | ∞ 44:55 (t = 682) | — |
| 1170 | Home | ∞ 36:47 | — |
| 1206 | Fin | ∞ 36:11 | — |

- **«More Lives»** (t = 287-289). Sale al pulsar «Try Again» con 0 vidas (deducido: no se ve el toque). Fondo negro puro. Tarjeta morada con borde dorado, cabecera «More Lives» y X. Panel crema con rayos y un corazón rojo con «0». «Time to next life» con un reloj de aguja animada y «25:39». Botón verde «Refill» con moneda y «900»: no dice cuántas vidas da. Botón magenta con una claqueta de vídeo, un corazón «+1» y «Life». Saldo arriba.
- El jugador elige «+1 Life» (deducido) → anuncio (t = 290-336) → vuelve a «Level Failed!» (t = 338), no al tablero, y sin mensaje de recompensa → «Try Again» → tablero (t = 340-342).
- **Vidas ilimitadas.** El contador cambia a un corazón con «∞» y una cuenta atrás (t = 682). Se suman (15m + 30m = 44:55) y bajan en tiempo real (36:47 en t = 1170; 36:11 en t = 1206).
- En Home hay botones «+» verdes junto a las monedas y las vidas (no se abrieron).

### 4.6 Victoria y cadena de pantallas

**Nivel 387** (ganado tras pagar la botella extra):

| t | Pantalla | Detalle |
|---|---|---|
| 476 | Tablero | Última botella (amarilla) con espiral dorada |
| 477,26 | — | Empieza el jingle de victoria (5,88 s) |
| 478 | Animación | Tablero y HUD oscurecidos, no ocultos. Caldero morado con aro dorado; «SORT» delante y «MAGIC» detrás |
| 480 | Animación | «MAGIC» ya encima; fuegos artificiales |
| 482 | Victoria | Tarjeta morada: «Level 387», «PERFECT!», «Rewards:» 20, botón «Continue» y X. Sin opción de doblar con anuncio |
| 484-486 | Magic Streak | Pantalla completa del evento (hitos 1-3; luego 14-16) |
| 488 | Home | 215 monedas; 1 vida y 22:19; el botón aún dice «Level 387»; trofeo 1/50 |
| 488-492 | Home | Las +20 monedas llegan al contador (215 → 235) |
| 490 | Broom Streak | «Finished»; solo botón «Continue» |
| 492 | Boat Rush | Popup; el jugador toca la X |
| 494 | Team Quest | Popup con «Continue» |
| 496 | Tablero 388 | Qué se tocó entre t = 494 y 496: no observado |

**Nivel 388** (ganado sin ayudas):

| t | Pantalla | Detalle |
|---|---|---|
| 626 | Tablero | Último vertido (rojo) |
| 627,92 | — | Empieza el jingle |
| 628 | Animación | Caldero morado; entra la «M» de «MAGIC» |
| 629,34 | — | El anuncio corta la animación y el jingle |
| 630-660 | Anuncio | Intersticial (§6) |
| 659,83 | — | El jingle sigue donde se quedó |
| 662 | Victoria | «Level 388», «PERFECT!», «Rewards:» 20, «Continue». Debajo, la franja de Magic Streak con el sombrero en x5 |
| 664 | Home | 235 monedas; 1 vida y 19:22; botón «Level 388»; barra Magic Streak 1/5 en x1, con la tira x10/x25/x100 moviéndose; trofeo 2/50 |
| 666 | Home | Destellos en el contador: 235 → 255. Racha en x5. El botón pasa a rojo «Super Hard / Level 389» con «x5». Un pato vuela del escenario al icono de Duck Festival |
| 668 | Home | El icono del pato muestra «36» y un globo «1» |
| 670 | Broom Streak | Activo (14h 42m), con X |
| 672-674 | Daily Bonus | Elige una chistera: ∞ 15m. Se revelan las otras 8 |
| 676 | Premio | «Congratulations!», ∞ 15m, «Tap to Claim» |
| 678 | Duck Festival | Barra 1/1 que estalla sobre el premio de 30m |
| 680 | Premio | «Congratulations!», ∞ 30m |
| 682 | Home | ∞ 44:55; 255 monedas |
| 682,98 | — | Toca «Level 389» |
| 684 | Tablero 389 | — |

- Del último vertido al tablero siguiente: 20 s en el 387 y 58 s en el 388 (30 s de ellos, anuncio).
- Las dos victorias dicen «PERFECT!», también la que se ganó pagando la botella extra (t = 482). Si hay otros calificativos: **no observado**.
- Las monedas se cobran al llegar a Home, con animación, no en el popup (t = 488-492, 664-666).
- Si cada pantalla de la cadena se abre sola o por un toque: **no observado**, salvo la X de Boat Rush (t = 492).

### 4.7 Adónde lleva cada botón tras un fallo

| Botón | Destino | t |
|---|---|---|
| «Try Again» en «Level Failed!» | Tablero directo, con otra variante (a veces con un anuncio antes) | 142-144, 195-228, 748-752, 1023-1024 |
| X en «Level Failed!» | Home | 6,5 → 7; probable en 1169 → 1170 |
| «Try Again» con 0 vidas | «More Lives» | 288 |

### 4.8 Medidas de las pantallas de fallo, oferta y victoria

Medidas con numpy y PIL sobre fotogramas a 1080 × 2340 (`det_ms/`) salvo la tienda y la victoria, que salen de `ov/` (540 px) multiplicadas por 2. Precisión: ±5-10 px (bordes con brillo y JPEG). «% W» sobre 1080 px; «% H» sobre 2340 px; dp a 411 dp de ancho (2,63 px por dp). Las x y las y son píxeles a 1080 × 2340.

| Pantalla (t) | Elemento | Caja (x · y) | Tamaño | % W · % H | dp |
|---|---|---|---|---|---|
| «No Available Moves» (404) | Saldo | 44-316 · 190-270 | 272 × 80 | 25 % · 8,1-11,5 % | 104 × 30 |
| | Título | 103-980 · 424-521 | 877 × 97 (letras de ≈ 66 px) | 81 % · 18,1-22,3 % | 334 × 37 |
| | Pestaña «Add Bottle» | 107-442 · 1749-1824 | 335 × 75 | 31 % · 74,7-77,9 % | 127 × 29 |
| | «Play On 900» (con borde) | 24-532 · 1817-2040 | ≈ 510 × 222 | 47 % · 77,6-87,2 % | 194 × 84 |
| | «Give Up» (con borde) | 550-1063 · 1817-2039 | ≈ 513 × 222 (letras de ≈ 36 px) | 47 % · 77,6-87,1 % | 195 × 84 |
| «Continue?» (803) | Banda a lo ancho | 0-1080 · 484-1640 | 1080 × 1156 | 100 % · 20,7-70,1 % | 411 × 440 |
| | X | 935-1044 · 453-558 | 109 × 105 | 10 % | 41 |
| | Corazón roto | 325-757 · 756-1070 | 432 × 314 | 40 % · 32,3-45,7 % | 164 × 119 |
| | «You will lose 1 life!» | 223-855 · 1138-1191 | 632 × 53 | 59 % · 48,6-50,9 % | 240 × 20 |
| | «Play On 900» (con borde) | 197-883 · 1302-1537 | 686 × 235 | 64 % · 55,6-65,7 % | 261 × 89 |
| «Level Failed!» SUPER HARD (748) | Tarjeta | 82-997 · ≈ 570-1760 | 915 × ≈ 1190 | 85 % · 24,4-75,2 % | 348 × 453 |
| | Calaveras y cinta «SUPER HARD» | ≈ 390-530 en y | — | 16,7-22,6 % | — |
| | Píldora «Level 389» | ≈ 210-870 · 560-720 | ≈ 660 × 160 | 61 % | 251 × 61 |
| | X | 905-1020 · 583-694 | 115 × 111 | 11 % | 44 |
| | Franja «Level Failed!» | 205-876 · 740-915 | 671 × 175 (letras de ≈ 48 px) | 62 % · 31,6-39,1 % | 255 × 67 |
| | Monedas y pedestal | ≈ 300-760 · 1051-1340 | ≈ 460 × 290 («100» de 69 px) | 43 % · 44,9-57,3 % | 175 × 110 |
| | «Try Again» (con borde) | 227-820 · 1416-1651 | 593 × 235 | 55 % · 60,5-70,6 % | 226 × 89 |
| | Panel de Magic Streak | 0-1080 · ≈ 1990-2340 | 1080 × ≈ 350 | 100 % · 85-100 % | — |
| «More Lives» (289) | Tarjeta | 64-1017 · ≈ 360-1757 | 953 × ≈ 1397 | 88 % · 15,4-75,1 % | 363 × 532 |
| | Cabecera «More Lives» | ≈ 150-909 · 320-520 | ≈ 759 × 200 | 70 % · 13,7-22,2 % | 289 × 76 |
| | X | 927-1036 · 376-481 | 109 × 105 | 10 % | 41 |
| | Panel crema (corazón y reloj) | 147-931 · 578-1050 | 784 × 472 | 73 % · 24,7-44,9 % | 298 × 180 |
| | «Refill 900» | 247-835 · 1127-1322 | ≈ 590 × 195 | 54 % · 48,2-56,5 % | 224 × 74 |
| | «+1 Life» (anuncio) | 251-831 · 1416-1610 | ≈ 580 × 194 | 54 % · 60,5-68,8 % | 221 × 74 |
| Heavenly Deal (1082) | Tarjeta | 36-1043 · ≈ 480-2030 | 1007 × ≈ 1550 | 93 % · 20,5-86,8 % | 383 × 590 |
| | Título «Heavenly Deal» | 178-960 · 389-500 | 782 × 111 | 72 % · 16,6-21,4 % | 298 × 42 |
| | X | 905-1053 · 449-557 | 148 × 108 | 14 % | 56 |
| | «Ends in: 2h 35m» | 286-798 · 583-672 | 512 × 89 | 47 % · 24,9-28,7 % | 195 × 34 |
| | Ilustración | ≈ 640-1440 en y | — | 27-62 % | — |
| | Sello «90% OFF» | 35-230 · 1323-1518 | 195 × 195 | 18 % | 74 |
| | Franja de contenido | 86-1020 · 1449-1681 | 934 × 232 | 86 % · 61,9-71,8 % | 355 × 88 |
| | Botón «COP 5,000» (con borde) | ≈ 240-840 · 1715-1950 | ≈ 600 × 235 (cifra de 74 px) | 56 % · 73,3-83,3 % | 228 × 89 |
| Tienda (1084, de `ov/`) | Cabecera «Shop» | 360-720 · 0-252 | 360 × 252 | 33 % · 0-10,8 % | 137 × 96 |
| | Starter Bundle | 24-1054 · 240-≈ 820 | 1030 × ≈ 580 | 95 % · 10,3-35 % | 392 × 221 |
| | Su botón de precio | 672-1016 · 666-772 | 344 × 106 | 32 % | 131 × 40 |
| | Apprentice Bundle | 24-1054 · ≈ 886-1324 | 1030 × ≈ 438 | 95 % · 37,9-56,6 % | 392 × 167 |
| | 3 packs de monedas | 34-1046 · 1386-1844 | 3 tarjetas de ≈ 330 px | 94 % · 59,2-78,8 % | 3 × 126 |
| | Botón de cada pack | — · 1706-1802 | 266 × 96 | 25 % | 101 × 37 |
| | «More Offers» | 256-822 · 1890-2026 | 566 × 136 | 52 % · 80,8-86,6 % | 215 × 52 |
| Victoria (482, de `ov/`) | Tarjeta | 82-996 · ≈ 540-1760 | 914 × ≈ 1220 | 85 % · 23-75,2 % | 348 × 464 |
| | Cabecera «Level 387» | 200-888 · 532-708 | 688 × 176 | 64 % · 22,7-30,3 % | 262 × 67 |
| | X | 906-1020 · 582-696 | 114 × 114 | 11 % | 43 |
| | «PERFECT!» | 256-824 · 784-870 | 568 × 86 | 53 % · 33,5-37,2 % | 216 × 33 |
| | Monedas y pedestal | 396-680 · 1052-1290 | 284 × 238 | 26 % · 45-55,1 % | 108 × 91 |
| | «Continue» (con borde) | 228-852 · 1418-1660 | 624 × 242 | 58 % · 60,6-70,9 % | 237 × 92 |

**Patrón común** (deducido de las 7 pantallas):
- La tarjeta ocupa el 85-93 % del ancho y empieza entre el 15 % y el 25 % del alto. El fondo es el tablero oscurecido o negro puro («More Lives»).
- El botón principal mide el 54-64 % del ancho y unos 195-242 px de alto (8-10 % H; 74-92 dp), siempre verde y en la mitad inferior de la tarjeta (entre el 48 % y el 83 % del alto). Con dos opciones, van lado a lado al 47 % («No Available Moves») o apiladas con el mismo ancho («More Lives»).
- El precio va dentro del botón, con la moneda; en las ofertas de dinero real, la cifra mide unos 74 px.
- La X roja mide 109-148 px y monta sobre la esquina superior derecha de la tarjeta.
- El saldo sale siempre arriba a la izquierda (25 % W).
- Orden vertical: título o cabecera → imagen grande (corazón, monedas, ilustración) → texto de lo que está en juego → botón.

---

## 5. Economía

### 5.1 Monedas

Solo se vio una moneda: **monedas de oro** (moneda blanda). No hay gemas ni otra moneda dura.

| Fuente de monedas | Cantidad | t |
|---|---|---|
| Nivel normal ganado | 20 | 482, 662 |
| Nivel SUPER HARD (se muestra al perder) | 100 | 748 |
| Daily Bonus (chisteras no elegidas) | 300 o 400 | 674 |
| Magic Streak, hito 16 | 500 | 486 |
| Journey, nivel 10 | monedas (cantidad no visible) | 1191 |
| Tienda | ver §5.4 | 1084 |

### 5.2 Comodines

| Comodín | Icono en la barra | Precio | Usado en la sesión | Fuentes gratis vistas |
|---|---|---|---|---|
| Deshacer (deducido por el icono) | Flecha circular (antihoraria) | 300 | No; su función no se observa | Journey 360 y 390 y 420; packs y Heavenly Deal (x1) |
| Barajar (deducido por el icono) | Dos flechas cruzadas | 300 | No; su función no se observa | Daily Bonus (x1 y x2); Journey 390 y 420; packs (x1) |
| Añadir botella | «+» y un frasco | 900 | No (sí como «Play On») | Daily Bonus (x1); Journey 370 y 400; packs (x1) |

- Los comodines no llevan contador de unidades: solo la píldora con el precio (t = 0, a resolución completa). **Deducido:** el jugador no tenía unidades. Cómo se ven con unidades: **no observado**.
- En la oferta Heavenly Deal y en Journey, el deshacer se dibuja como una flecha dorada que gira en sentido horario (t = 1082, 1188). En la tienda sale con el icono de la barra (t = 1084). **Deducido:** es el mismo comodín.
- Con 255 monedas no se puede pagar ninguno (t = 714-1206).

### 5.3 Rescates

| Oferta | Dónde | Precio | Qué da | t |
|---|---|---|---|---|
| «Play On» con «Add Bottle» | «No Available Moves» | 900 monedas | +1 botella vacía; se sigue el intento | 84, 404 |
| «Play On» | «Continue?» | 900 monedas | Seguir sin perder la vida. Qué ayuda da: no observado | 86 |
| «Refill» | «More Lives» | 900 monedas | Vidas (no dice cuántas) | 288 |
| «+1 Life» | «More Lives» | 1 anuncio | +1 vida | 288 |

Solo se pagó una vez: «Play On» en t ≈ 406-408 (saldo de 1115 a 215).

### 5.4 Tienda («Shop», t = 1084 y t = 1090)

| Producto | Contenido | Precio | Notas |
|---|---|---|---|
| Starter Bundle | 1 000 monedas; ∞ 1h; deshacer x1; barajar x1; añadir botella x1 | COP 5,000 | Sello rojo «90% OFF». Tarjeta naranja con un sombrero de mago lleno de monedas |
| Apprentice Bundle | 2 000 monedas; icono «ADS» tachado; ∞ 3h; deshacer x1; barajar x1; añadir botella x1 | COP 22,000 | Tarjeta blanca y azul con un saco rojo de monedas. **Deducido:** «ADS» tachado = sin anuncios |
| Pack de monedas | 1 000 | COP 9,500 | — |
| Pack de monedas | 5 000 | COP 36,500 | — |
| Pack de monedas | 10 000 | COP 73,000 | — |
| «More Offers» | Botón rosa con una flecha verde hacia abajo | — | No se abrió |

- Cabecera «Shop», saldo 255 y X roja. Detrás se ve el tablero oscurecido.
- Precios en pesos colombianos (la cuenta de Google Play del dueño).

### 5.5 Ofertas emergentes: Heavenly Deal (t = 1082)

- **Contenido:** 1 200 monedas; deshacer x1; barajar x1; añadir botella x1; ∞ 1h. Precio **COP 5,000**. Sello «90% OFF». Cuenta atrás «Ends in: 2h 35m». X roja.
- **Ilustración:** un hámster con túnica azul de estrellas que sostiene monedas, un gato naranja con capa rosa y varita de estrella, y un gato azul con gafas que abre una bolsa de monedas. Camino de arcoíris hacia una montaña de oro, con nubes.
- **Disparador: no observado.** Sale con el intento sin tocar (el tablero de t = 1081 es el inicial) y justo después de un golpe sonoro fuerte de 312 Hz (t = 1081,79). Hipótesis: tocar un comodín sin monedas.
- Al cerrarla con la X se abre la tienda (t = 1084). La tienda vuelve a abrirse en t = 1090, tras otro golpe igual (t = 1089,77).
- **Deducido:** el icono del caldero con arcoíris de Home es esta oferta. Su temporizador marca 2h 53m en t = 7 y 2h 34m en t = 1170, lo que cuadra con 2h 35m en t = 1082.

### 5.6 Cálculos a partir de los precios (no son datos del juego)

- Precio por moneda: COP 9,5 en el pack de 1 000; COP 7,3 en los de 5 000 y 10 000.
- Una botella extra (900 monedas) equivale a unos COP 8,550 al precio del pack pequeño, o COP 6,570 al de los grandes.
- 900 monedas = 45 niveles normales ganados; 300 = 15.
- El Starter Bundle da 1 000 monedas y extras por la mitad de precio que el pack de 1 000 monedas solo. El Heavenly Deal da 1 200 monedas y extras por el mismo precio que el Starter.

### 5.7 Saldo del jugador a lo largo de la sesión

| t | Saldo | Motivo |
|---|---|---|
| Antes (ms-01) | 1115 | — |
| 5 → 406 | 1115 | Sin gastos (visto en t = 5, 84, 194, 288, 404, 406) |
| ≈ 406-408 | 215 | Paga «Play On» 900 en el 387 (visto en t = 488) |
| 488 → 492 | 235 | +20 por ganar el 387, cobrado en Home |
| 664 → 666 | 255 | +20 por ganar el 388 |
| 666 → 1206 | 255 | Sin cambios: 11 bloqueos y 1 abandono en el 389 |

Balance: ganó 40 monedas y gastó 900 en 20 minutos.

### 5.8 Vidas ilimitadas

| Fuente | Duración | Cómo se consigue | t |
|---|---|---|---|
| Daily Bonus | 15m | Elegir una chistera y «Tap to Claim», sin anuncio | 672-676 |
| Duck Festival, etapa 1 | 30m | «Tap to Claim» | 678-680 |

Ofrecidas y no cobradas: Magic Streak hitos 1 y 3 (30m), hito 15 (5h) y barra 0/5 (30m); Broom Streak hito 2 (15m); Journey nivel 410 (30m); Starter Bundle y Heavenly Deal (1h); Apprentice Bundle (3h). El botón «Join» de Boat Rush lleva un corazón ∞; qué da: **no observado**.

---

## 6. Publicidad

### 6.1 Anuncios vistos

| # | t | Tipo | Disparador | Duración | Cierre | Anunciante |
|---|---|---|---|---|---|---|
| 1 | 196-227 | Intersticial a pantalla completa | «Try Again» en «Level Failed!» tras el 4.º intento perdido del 387 (toque visto en t = 195) | ≥ 31 s | «Close» aparece en t = 200. Un toque en «Close» (t = 212) no hace nada. El jugador pulsa Atrás de Android (t ≈ 224 y t = 227). Tablero en t = 228 | Kalshi (app de mercados de predicción) |
| 2 | 290-336 | Recompensado | «+1 Life» en «More Lives» (deducido) | ≈ 47 s | «Close» visible desde t ≈ 294. Toque en «Install» en t = 304 (Google Play no se ve después). Toque en «Close» en t = 328; el anuncio sigue unos 8 s más y acaba en t ≈ 336. Si hizo falta otro toque: no observado | Kalshi |
| 3 | 630-660 | Intersticial | Victoria del 388: corta la animación de victoria (t = 629,34) | ≈ 30 s | «Close» aparece entre t = 632 y 634. Toques en t ≈ 638, 652 y 658. Se cierra en t ≈ 660 | Kalshi (otro texto) |
| 4 | 906-936 | Intersticial jugable | Tras «Level Failed!» del 5.º bloqueo seguido del 389 (t = 904; botón no visto) | ≈ 30 s | Sin X hasta la tarjeta final (t = 936). De t = 920 a 924 hay una píldora con un icono de saltar y «Google Play». Un toque en el cubo (t = 926) y en t = 928 se abre la hoja de Google Play. El jugador sale al escritorio y vuelve (t = 930-934). Tablero en t = 938 | CubeAway - 3DPuzzle (GOODROID) |

### 6.2 Cómo se ven y se comportan

- **Kalshi:** pantalla negra con la zona de vídeo en negro puro (valor 0 a resolución completa). Debajo, texto promocional sobre predicciones y trading, valoración «4.7», «100K Reviews», tarjeta con logo verde, «Google Play» y botón «Install». Icono AdChoices abajo a la derecha. «Close» en una píldora gris arriba a la izquierda (t = 200, 634).
- **Sin cuenta atrás**, barra de progreso ni texto de recompensa en ninguno de los cuatro (t = 213, 328, 634).
- **«Close» no respondió a algunos toques** en dos anuncios: t = 212 (hizo falta el botón Atrás de Android, t = 227) y t = 638 y 652 (se cerró hacia t = 660, tras un tercer toque en t = 658). En t = 328 el anuncio siguió unos 8 s tras el toque; si hizo falta otro toque: **no observado** («Close» estaba a la vista desde t ≈ 294, así que tampoco se sabe si fue el primer toque). Si «Close» estaba inactivo hasta el final del vídeo: **no observado** (se ve igual).
- **CubeAway:** cubo 3D gris lleno de flechas, una mano tutorial que toca una flecha resaltada y el texto «Tap the arrow!». Icono «i» abajo a la izquierda. Tarjeta final con «GET», «Interested», «Not interested» y una X pequeña (t = 936). Que el toque de t = 926 abriera la tienda es probable, no probado.
- Rompen el modo inmersivo: salen las barras de Android (t = 210, 225, 306, 652).
- El audio de la grabación está en silencio durante los anuncios.
- Tras el anuncio 2 el juego vuelve a «Level Failed!» (t = 338). Tras el 1 y el 4 vuelve directo al tablero (t = 228, 938).

### 6.3 Frecuencia

- Intersticiales en t = 196, 630 y 906: separados 434 s y 276 s. El primero llega a los 3 min 16 s de grabación.
- **Sin anuncio** en el resto de «Try Again» observados: t = 142-144, 338-342, 748-752, 780-782, 804-806, 892-894, 974-976, 984-986, 1023-1024, 1065-1066 y 1074-1076. El último, 168 s después del anuncio 4.
- **Deducido:** hay un límite de frecuencia por tiempo. La regla exacta: **no observada**.
- **No se vio ningún banner** ni en el tablero ni en Home.

### 6.4 Anuncios recompensados

- Solo uno: **«+1 Life»** en «More Lives» (t = 288). Da 1 vida.
- **No hay anuncio** para continuar, para la botella extra, para doblar el premio de victoria ni para comodines (t = 404, 482, 662, 803).

### 6.5 Vidas ilimitadas de 15 minutos y anuncios: lo que se ve

- Las ∞ 15m del Daily Bonus se reclamaron con «Tap to Claim», **sin anuncio**: elección de chistera en t = 671,48 (clic), «Congratulations!» en t = 675,34 y nada entre medias (t = 672-676).
- ∞ 15m también es premio del hito 2 de Broom Streak (t = 670); no se cobró.
- El dueño lo confirmó el 2026-10-08: las ∞ 15m que vio son las de la chistera del Daily Bonus (minuto 11:10, t = 670-676), gratis.
- **«No del todo»: las vidas ilimitadas no quitan los anuncios.** Con ∞ activo (∞ 44:55 desde t = 682, ∞ 36:11 en t = 1206) el jugador siguió viendo intersticiales: el de t = 906-936 llegó tras el 5.º bloqueo seguido del 389, con vidas ilimitadas en marcha. Las ∞ quitan el coste en vidas del fallo, no la publicidad.
- En esta grabación no aparece ningún caso de vidas ilimitadas que se paguen con un anuncio recompensado.

---

## 7. Metajuego y retención

### 7.1 Home

**Escena:** escenario de teatro con telón granate y rosa. Un gato mago con chistera magenta y túnica rosa de estrellas hace el truco de serrar a un gato en dos cajas (la cabeza asoma de una caja y las patas de la otra). Un gato gris con gafas mira asustado tras el telón. Los personajes están animados (t = 7, 488, 1178).

**Barra superior:** avatar del jugador (gato con sombrero), monedas con «+» verde, vidas con «+» verde y temporizador, engranaje. Debajo, la barra de Magic Streak (§7.3).

**Iconos laterales** (t = 1178, a resolución completa):

| Posición | Icono | Texto | Qué es |
|---|---|---|---|
| Izquierda 1 | Trofeo con laureles | «2/50» | No identificado. Sube 1 por nivel ganado (0/50 en t = 7, 1/50 en t = 488, 2/50 en t = 664; deducido) |
| Izquierda 2 | Pato de goma | «36», «14h 34m» | Duck Festival (36 = puesto) |
| Izquierda 3 | Objeto rosa redondo con banda dorada sobre nubes | «Join» | No identificado |
| Izquierda 4 | Gato en escoba | «14h 34m» («Finished» en t = 7 y 488) | Broom Streak |
| Izquierda 5 | Pergamino con lacre rojo, que alterna con un mapa del tesoro | «Join», globo «!» | No identificado (aparece en t = 664) |
| Izquierda 6 | Timón dorado con gema | «Join» | No identificado. Hipótesis: Boat Rush (tema náutico) |
| Derecha 1 | Tridente sobre agua | «Finished» | No identificado |
| Derecha 2 | Rueda de madera con remaches y gema | «0/3», «14h 34m» | No identificado |
| Derecha 3 | Montón de monedas | «SALE», «4d 2h» | Oferta de tienda (no abierta) |
| Derecha 4 | Caldero con arcoíris | «2h 34m» | Heavenly Deal (deducido, §5.5) |
| Derecha 5 | Taladro sobre vagoneta de monedas | «2d 2h», globo «!» | No identificado |
| Derecha 6 | Calendario | «0/31» | No identificado (¿premio de inicio de sesión mensual?) |

- **Botón central:** «Level N» grande, verde (rojo «Super Hard» con etiqueta «x5» en el 389).
- **Barra inferior de 5 pestañas:** tienda (puesto con toldo y globo rojo «!»), trofeo, castillo «Home» (seleccionada y elevada), estandarte con tres siluetas de personas (equipo) y cartas con cara de gato (colección). Solo «Home» lleva texto. No se abrió ninguna salvo la tienda (§5.4).
- **Los iconos cambian:** antes de la grabación (ms-01) había menos (trofeo, gato en escoba «Finished», timón «Join», tridente «Finished», SALE «4d 3h», caldero «3h 23m», taladro «2d 3h», calendario). En t = 7 ya están el objeto rosa «Join» y la rueda «0/3». En t = 664 aparecen el pato y el pergamino.
- Los textos bajo los iconos son solo estados («Join», «Finished»), contadores y temporizadores. **Ningún icono dice el nombre del evento.**

### 7.2 Broom Streak

- **En el tablero:** cofre azul y dorado con una escoba, «Broom Streak» y «0/2» (t = 0, 938). No aparece en el 388 (t = 496).
- **Pantalla propia:** una torre de piedra a la izquierda con un raíl vertical e hitos 0 (verde), 2 (morado), 5 (rosa) y 8 (morado; la vista está cortada por arriba). Hitos por encima del 8: **no observados**. Cofres azules y dorados en los balcones de los hitos 2, 5 y 8, cada uno con su botón «i». Banderín rojo bajo el 5. Un gato con capa roja de estrellas vuela en escoba junto al hito 0, sobre cielo azul y hierba (t = 670).
- **Premio del hito 2:** ∞ 15m + una funda marrón con una insignia en forma de carta y el número «2» (t = 670). Contenido de los cofres 5 y 8: **no observado**.
- **Dos estados:** «Finished» con solo el botón «Continue» (t = 490), y activo con «14h 42m» y X (t = 670).
- **Estado a lo largo de la sesión** (no cuadra con una regla simple):
  - Antes de la grabación, Home dice «Finished» (ms-01), pero el HUD del 387 enseña «Broom Streak 0/2» (ms-02).
  - Durante el 387 pasa lo mismo: HUD con «Broom Streak 0/2» (t = 0, 234, 342) y Home con «Finished» (t = 7, 88, 488).
  - En el 388 el HUD enseña el saldo (t = 496) y la pantalla del evento dice «Finished» (t = 490).
  - Tras ganar el 388, el evento está activo con 14h 42m (t = 668, 670) y el HUD del 389 vuelve a enseñar «Broom Streak 0/2» (t = 684, 938).
  - Qué decide si el hueco del HUD enseña la racha o el saldo, y qué hace que el evento pase de «Finished» a activo: **no observado**.
- **Regla:** no hay texto en pantalla (los «i» no se abrieron). **Deducido** por el nombre y los hitos: victorias seguidas. La regla de entrada y de reinicio: **no observada**.

### 7.3 Magic Streak

- **Regla en pantalla:** «Beat levels without fail to get more rewards!» (t = 484).
- **Pantalla:** estantería con sombreros de mago (azul, morado, verde, rosa y rojo), una pluma y pociones; botones «i» y X; temporizador «14h 45m»; tira de multiplicadores **x1, x5, x10, x25, x100** (el activo, en naranja); barra «0/5» con un sombrero morado a la izquierda y un corazón ∞ «30m» al final; lista vertical de hitos con candado (t = 484-486).
- **Hitos vistos:** 1: ∞ 30m; 2: funda marrón con insignia de cartas «2»; 3: ∞ 30m; 14: estuche magenta con herrajes dorados y gema morada, insignia de cartas «5»; 15: ∞ 5h; 16: 500 monedas. Asoma el candado de un hito 17. Hitos 4-13: **no observados**.
- **Lo que pasó:** x1 y 0/5 tras ganar el 387 (t = 484, 488). x5 tras ganar el 388 (t = 662, 666), con la barra en 1/5 (t = 664). En la primera derrota del 389 el sombrero baja de x5 a x1 (t = 748 → 750; sonido de pérdida). La barra sigue en 1/5 tras las derrotas (t = 1178).
- **Dónde se ve:** barra fija arriba en Home (sombrero con placa roja «xN», «n/5», ∞ 30m y temporizador), franja bajo «PERFECT!» (t = 662) y panel bajo «Level Failed!» en el 389 (t = 748).
- **Cuándo aparece la barra:** no está en Home en t = 7 ni en t = 88. Aparece en t = 488, después del popup de t = 484, que sigue a la primera victoria de la grabación. Por qué aparece entonces (inicio del evento, primera victoria del día u otra regla): **no observado**.
- Qué multiplica el «xN» y qué cuenta la barra: **no observado**. La regla de entrada y de reinicio de la racha: **no observada** (solo se ve que baja a x1 al primer fallo, t = 748 → 750).

### 7.4 Duck Festival (clasificación)

- **Pantalla** (t = 678): paisaje de cuento con un castillo; podio de 3 puestos con avatares de gato; banda roja con la barra de patos «1/1», el temporizador «14h 42m» y «Reward Stages: 1/20»; lista de jugadores con puesto, avatar, nombre, equipo (en rojo, si lo tiene) y patos. La fila del jugador va en verde: puesto 36, 1 pato. Los rivales de los puestos 31-35 también tienen 1 pato.
- **Regla deducida:** 1 pato por nivel ganado (tras ganar el 388, un pato vuela al icono, t = 666).
- **Premio de la etapa 1:** ∞ 30m, reclamado con «Tap to Claim» (t = 678-680). Premios por puesto: **no observado**.

### 7.5 Boat Rush (carrera)

- **Popup** (t = 492): «Beat 12 stages to win the grand prize!», temporizador «14h 45m», botones «i» y X.
- **Mapa:** río de selva con piedras con estrella como casillas, boyas con banderín rojo, islas con cofres (verde; rosa y azul con un timón; y el gran premio, un cofre morado con ancla sobre monedas y perlas) y el avatar del jugador en la isla de salida.
- Botón verde «Join» con un corazón ∞. El jugador toca la X (t = 492). Qué cuenta como etapa: **no observado**.

### 7.6 Team Quest (equipos)

- **Popup** (t = 494): «Join a team to compete in the Team Quest!», temporizador «14h 45m», botón «Continue», botones «i» y X. Ilustración: dos gatos exploradores (uno azul con gafas y un mapa, otro naranja con prismáticos rosas) en unas ruinas de piedra con lianas y una ventana a la noche.
- El jugador no tiene equipo (sin nombre de equipo en t = 678). En Duck Festival hay jugadores con equipo (dos clubes con nombre mexicano).

### 7.7 Daily Bonus (chisteras)

- **Pantalla:** «Daily Bonus!», cuadrícula de 3 × 3 chisteras granates con banda dorada y una máscara de gato, y el texto «Pick a Magical Hat to get your daily reward!». X roja (t = 672).
- Al elegir, la chistera se vuelve rosa brillante con un halo y el premio flota encima. Las demás muestran su contenido atenuado (t = 674):

| | Columna 1 | Columna 2 | Columna 3 |
|---|---|---|---|
| Fila 1 | Funda roja con gema | 300 monedas | Barajar x2 |
| Fila 2 | Añadir botella x1 | Funda negra con cara de gato | **∞ 15m (elegida)** |
| Fila 3 | 400 monedas | Funda verde | Barajar x1 |

- Se reclama gratis con «Congratulations!» y «Tap to Claim» (t = 676).

### 7.8 Journey (mapa de progreso)

- **Pantalla** (t = 1188-1202): pergamino vertical con bocetos de botellas; vara central que se rellena de verde hasta el marcador del nivel actual («389»); a la izquierda, placas «Level N»; a la derecha, su premio. Los cobrados llevan borde verde y ✓.
- **Premios cada 10 niveles**, más algunos intermedios:

| Nivel | Premio | Estado |
|---|---|---|
| 420 | Deshacer x1 + barajar x1 | Pendiente |
| 410 | ∞ 30m | Pendiente |
| 400 | Añadir botella x1 | Pendiente |
| 390 | Deshacer x1 + barajar x1 | Pendiente (a 1 nivel) |
| 380 | ∞ | ✓ |
| 370 | Añadir botella | ✓ |
| 360 | Deshacer | ✓ |
| 75 | ∞ | ✓ |
| 70 | Añadir botella | ✓ |
| 65 | Deshacer + barajar | ✓ |
| 15 | ∞ | ✓ |
| 10 | Monedas | ✓ |

- **Temáticas por tramos de niveles** (t = 1190-1202; `ov/` f_011900, f_011940 y `det_ms/` d_011910):

| Tarjeta | Rango | Dónde está en el mapa | Escena | t |
|---|---|---|---|---|
| «Magic Trouble» | «Level 1 - 19» | Abajo del todo, bajo el nivel 10 | Gatos en un cuarto con un caldero morado que desborda baba rosa | 1192 |
| «Broom Flight» | «Level 20 - 39» | Sobre el nivel 15 | Gato con capa sobre una escoba en un bosque | 1192 |
| Sin nombre visible (la tarjeta queda cortada) | No visible | Bajo el nivel 65 | Gatos en escoba sobre un cielo al atardecer | 1190 |
| «Slimey» | «Level 80 - 99» | Sobre el nivel 75 | Gatos y baba rosa en un cuarto | 1190 |

- **Ritmo:** una tarjeta cada 20 niveles en el tramo 1-99, colocada al principio de su tramo (el mapa sube de abajo arriba). La tarjeta sin nombre bajo el 65 cuadra con un tramo 60-79 (deducido). Entre 360 y 420 no hay ninguna tarjeta (t = 1188): si las temáticas siguen más allá del 99 o se espacian: **no observado**.
- **Qué son:** escenas de historia con el elenco de gatos, presentadas como fotos con un trozo de cinta adhesiva en la esquina y una pestaña rosa «USE» a la izquierda. Los rótulos «Top» y «Bottom» son capas fijas en la pantalla, no etiquetas de tarjeta.
- La escena de Home (el teatro con el truco de serrar a un gato, t = 7 y 1187) **no coincide con ninguna tarjeta vista**.
- **No observado:** qué hace «USE» (deducido: aplica esa escena a algo, ¿el fondo de Home?), si la temática siguiente se adelanta bloqueada o en silueta, y si las tarjetas sin desbloquear se ven en el mapa (todas las vistas están en tramos ya superados).
- Qué control de Home abre Journey: **no observado** (t = 1187).

### 7.9 Colección de cartas

- Pestaña de la barra inferior con cartas con cara de gato (no abierta).
- **Deducido:** las fundas con insignia de cartas son sobres de cartas: marrón «2» (Magic Streak hito 2; Broom Streak hito 2), magenta «5» (Magic Streak hito 14), roja con gema, negra con cara de gato y verde (Daily Bonus). Contenido: **no observado**.

### 7.10 Clasificaciones y equipos

- Clasificación con podio y lista en Duck Festival (§7.4). La pestaña del trofeo no se abrió.
- Equipos: Team Quest (§7.6) y la pestaña del estandarte. No se abrieron.

### 7.11 Temporizadores

| Temporizador | Antes (ms-01) | t = 7 | t ≈ 484-494 | t = 664-678 | t = 1170-1178 |
|---|---|---|---|---|---|
| Rueda «0/3» (sin identificar) | No está | 14h 53m | 14h 45m (t = 488) | 14h 42m (t = 668) | 14h 34m |
| Magic Streak | No está | No está: sin barra en Home (t = 7 y 88) | 14h 45m (t = 484, 488) | 14h 42m (t = 668) | 14h 34m |
| Broom Streak | «Finished» | «Finished» | «Finished» (t = 488, 490) | 14h 42m (t = 668, 670) | 14h 34m |
| Boat Rush | «Join» sin tiempo | «Join» sin tiempo | 14h 45m (popup, t = 492) | «Join» sin tiempo | «Join» sin tiempo |
| Team Quest | — | — | 14h 45m (popup, t = 494) | — | — |
| Duck Festival (pato) | No está | No está | No está | 14h 42m (t = 668, 678) | 14h 34m |
| «SALE» | 4d 3h | 4d 2h | 4d 2h | 4d 2h | 4d 2h |
| Caldero con arcoíris (Heavenly Deal, deducido) | 3h 23m | 2h 53m | 2h 45m | 2h 42m | 2h 34m |
| Taladro | 2d 3h | 2d 2h | 2d 2h | 2d 2h | 2d 2h |

- Los eventos activos terminan a la vez: 14h 45m en t = 484-494, 14h 42m en t = 668-678 y 14h 34m en t = 1178. En t = 7 solo la rueda «0/3» marca tiempo (14h 53m): Broom Streak dice «Finished» y Magic Streak no está. A qué hora terminan: **no observado**.
- Todas las cuentas atrás siguen el tiempo real.

---

## 8. Dirección visual y efectos

### 8.1 Home

- Escenario de teatro de magia con gatos (§7.1). Iluminación cálida de focos, telón granate y rosa, suelo de madera.
- Iconos de evento redondos con un aro de color y una cinta inferior con el estado. Los avisos son globos rojos con «!» blanco.
- Botón de nivel grande y centrado bajo la escena. Barra inferior morada con el castillo de Home más alto que las demás pestañas.

### 8.2 Tablero

- **Fondo:** cielo nocturno azul marino profundo, liso, con estrellas pequeñas que titilan (t = 230). Color medido ≈#03082e, algo más claro arriba (≈#0b1057). Sin decorado ni soporte bajo las botellas: flotan.
- **Botellas:** vidrio oscuro translúcido con un borde azul que brilla (de ≈#4687c5 a ≈#19428b). Cuello corto y labio marcado (§2.2).
- **Líquido:** colores sólidos y saturados, con la cara superior en elipse más clara. Colores medios medidos en ms-02 (captura PNG del 387):

| Color | Muestra |
|---|---|
| Lima | ≈#70a502 |
| Azul | ≈#1616bf |
| Verde oscuro | ≈#0b3c01 |
| Rosa | ≈#fe6fd7 |
| Morado | ≈#5f1e8e |
| Cian | ≈#0198d8 |
| Marrón | ≈#70322b |
| Naranja | ≈#e45900 |
| Lavanda | ≈#5d64a6 |
| Amarillo | ≈#fcc006 |
| Rojo (base bajo una tela) | ≈#9e0602 |
| Capa «?» | ≈#04010d |

El 389 añade magenta o ciruela y celeste (t = 698).

- **Corcho dorado** en las botellas completas.
- **Telas** rosa pálido (≈#f4d4d9) con pliegues, como sábanas de fantasma.
- **Madera:** armarios de tablones claros con marco naranja; candados y llaves de colores, brillantes y en 3D.
- **Hielo:** bloque azul celeste facetado.

### 8.3 Popups y botones

- **Popups:** tarjeta morada (roja en SUPER HARD) con borde dorado grueso; cabecera en una píldora morada; paneles interiores crema; X roja redonda con borde dorado arriba a la derecha. Fondo: tablero oscurecido, cielo oscuro con estrellas, o negro puro («More Lives», t = 287).
- **Bandas a lo ancho** para «Continue?» (morada o granate).
- **Ilustración grande** en los popups de evento y de oferta (Team Quest, Boat Rush, Heavenly Deal).

| Color de botón | Uso | t |
|---|---|---|
| Verde | Acción principal: «Level N», «Try Again», «Continue», «Play On», «Refill», «Join», compra con precio | 7, 6,5, 482, 84, 288, 492, 1084 |
| Rojo | X de cerrar, «Quit», botón de nivel SUPER HARD | 5, 666 |
| Naranja | «Give Up» | 84 |
| Magenta | «+1 Life» (anuncio, con claqueta de vídeo) | 288 |
| Rosa | «More Offers» | 1084 |
| Morado | Comodines y paneles | 0 |
| Píldora crema | Precios en monedas y contador de saldo | 0, 5 |

### 8.4 Tipografía

- Sans-serif redondeada y gruesa, blanca o crema, con contorno oscuro.
- Títulos grandes con contorno dorado y borde marrón («No Available Moves», t = 404).
- «PERFECT!» en degradado amarillo y naranja (t = 482). «Congratulations!» en dorado y curvado (t = 676).
- Todos los textos del juego están en inglés, aunque la tienda cobra en pesos colombianos (t = 1084).

### 8.5 Efectos y partículas

| Efecto | Descripción | t |
|---|---|---|
| Entrada del tablero | Las botellas entran deslizándose desde la derecha; a veces el rótulo baja primero desde arriba | 9, 228, 340 |
| Selección | La botella sube; nada más | 12, 232 |
| Vertido | Vuelo, inclinación, chorro fino; burbujas en el destino | 250, hoja |
| Botella completa | Espiral o hélice de destellos del color; corcho que cae | 26, 476, 852 |
| Revelado de «?» | El «?» se deforma y se disuelve con destellos de estrella | 238, 564 |
| Tela | ✓ verde en el icono; haz de luz; la tela sube como un fantasma con estrellas doradas | 56, 362, 398 |
| Hielo | Esquirlas azules por media pantalla | 504 |
| Llave | Brillo y estela de chispas hasta el candado | 812, 1034 |
| Armario | Puertas que se abren con chispas doradas, o estallido de astillas por toda la pantalla | 764, 814 |
| Botella extra | Anillo de chispas morado y azul | 408 |
| Victoria | Oscurecido, caldero, logo «MAGIC SORT», fuegos artificiales | 478-480 |
| Monedas | Destellos sobre el contador al cobrar | 666 |
| Pato | Vuela del escenario al icono del evento | 666 |
| Home | Personajes animados; el tridente salpica agua; el pergamino se abre en mapa | 664-666, 1180 |
| Popups | Botones que entran desde los lados; panel de racha que sube desde abajo | 777, 748 |

---

## 9. Audio

### 9.1 Música

- **No hay música en la grabación.** El 69,6 % de las ventanas de 10 ms son silencio digital y no hay energía tonal sostenida entre efectos.
- El motivo es la configuración del jugador: en t = 4 el menú de ajustes muestra el sonido activado y la música con una barra roja (desactivada). **La música de Magic Sort no se puede conocer con esta grabación.**
- Los anuncios no tienen audio en la grabación (silencio total en t = 196-226, 290-336, 630-659 y 906-936).

### 9.2 Catálogo de sonidos

Los efectos son muestras digitales idénticas en cada repetición (salvo donde se indica que hay variantes). Nivel = RMS de pico en ventanas de 32 ms.

| Sonido | Disparador | Veces | Duración | Tono | Carácter y nivel | Ejemplo (t) | Confianza |
|---|---|---|---|---|---|---|---|
| SELECT | Tocar una botella (sube) | 244 | 110 ms | Sin fundamental; formantes en 875, ≈2000 y ≈3000 Hz | «Tic» de cristal seco y suave; −38,5 dBFS (el gesto más bajo; algunos vertidos bajan a −41) | 11,64; 868,16 | Alta |
| DESELECT | Volver a tocar la botella elevada (baja) | 19 | 90 ms | Grave, pico en 156 Hz | «Tok» apagado; −35,5 dBFS | 562,21; 870,24 | Media-alta |
| UI_CLICK | Botones pequeños, cierres, X, «Give Up», «Continue», «Tap to Claim», elegir chistera | 59 | 40 ms | Clic de banda ancha, pico 328-343 Hz | Seco y corto; −30 dBFS | 3,46; 1020,56 | Alta |
| POUR 1 dosis | Vertido de 1 dosis (una sola muestra continua, no un sonido por dosis) | 174 | 390-590 ms (≈11 variantes) | Ruido, centroide 1,9-3,6 kHz | Burbujeo «glu-glu»; de −41 a −27 dBFS | 12,84; 240,4 | Alta para el disparador; media para el número de dosis |
| POUR 2 dosis | Vertido de 2 dosis | 37 | 710-810 ms (≈10 variantes) | Ruido, centroide 2,0-3,9 kHz | Burbujeo más largo; de −40 a −31 dBFS | 70,87; 741,29 | Media |
| POUR 3 dosis | Vertido de 3 dosis | 10 | 870-1090 ms (≈6 variantes) | Ruido, centroide 2,1-2,5 kHz | Burbujeo continuo largo; de −39 a −31 dBFS | 586,46; 739,25 | Media |
| COMPLETE | Vertido que completa una botella | 36 | ≈1600 ms | Brillo armónico 0,3-2,5 kHz que crece; corcho con golpe grave de 110-120 Hz a +1,15 s | Acorde «mágico» que remata en «¡plop!»; −17,5 dBFS | 25,61; 475,5 | Alta |
| CLOTH_REVEAL | Se retira una tela | 8 | ≈1100 ms | Campanilla 1050 + 2100 Hz; swoosh de ruido | «Ding» + «fsss»; −26 dBFS | 54,99; 401,03 | Alta |
| ICE_SHATTER | Se rompe un bloque de hielo | 2 | ≈700 ms | Ruido 0,2-8 kHz | «Crash» de cristal; nivel sin medir | 503,33; 508,87 | Media |
| NO_MOVES | Sale «No Available Moves» | 16 | 1290 ms | Glissando descendente de ≈1 kHz a 0,5 kHz y golpe de ≈250 Hz | Cascada «de decepción»; −16 dBFS | 82,9; 1018,47 | Alta |
| POP_A | Botones verdes grandes de jugar («Level N», «Try Again») y vuelta de un anuncio | 19 | 370 ms | Dos glissandos ascendentes 0,3-1,8 kHz y tono de ≈187 Hz | «Blup-blup»; −18 dBFS | 8,73; 682,98 | Media-alta |
| WHOOSH_B | Entra el tablero (0,24 s tras POP_A) | 18 | 790 ms | Ruido que sube hasta 8 kHz y baja | «Fuuush»; −18,6 dBFS | 8,97; 683,18 | Alta |
| UNLOCK | Se abre un candado | 15 | 690 ms | Clics y parciales metálicos de 3,2 a 5,7 kHz; golpe de 406 Hz | «Clic-clinc… clonc»; −25 dBFS | 695,05; 811,98 | Alta |
| WOOD_BREAK | Se rompen las puertas (0,87 s tras UNLOCK) | 15 | 1350-1900 ms (≥ 4 variantes) + tic final | Crash, centroide ≈2 kHz | Crujido y astillas; −19,5 dBFS | 695,92; 1118,97 | Alta |
| CONGRATS | Popup «Congratulations! / Tap to Claim» | 2 | 590 ms | Golpe + parciales de campana de 1,4 a 3,6 kHz | Campanilla de premio; −14,7 dBFS | 675,34; 679,54 | Alta |
| DUCK_DING | Barra de Duck Festival llena | 1 | 410 ms | «Plop» de ≈800 Hz y acorde de campana | «Plip-ding»; −14 dBFS | 677,99 | Media |
| STREAK_LOST | Probablemente Magic Streak baja de x5 a x1 (sin confirmar) | 2 | 360 ms | Dos glissandos descendentes | «Fiuu» de pérdida; −26 dBFS | 748,00; 749,45 | Baja-media |
| LOUD_THUMP | Sin atribuir; precede al Heavenly Deal y a la tienda | 4 | 110 ms | Pico de 312 Hz | «Bum» grave, el más fuerte (−10 dBFS) | 1081,79; 1089,77 | Baja |
| VICTORY | Nivel superado (0,61 s tras el último corcho) | 2 | 5880 ms | Glissando 0,5-2 kHz, golpe a +0,95 s (logo), estallido a +3,6 s (fuegos) | Fanfarria con fuegos artificiales; −14,5 dBFS | 477,26; 627,92 | Alta |
| HOME_COINS | Las monedas del premio llegan al contador de Home | 2 | 2570 ms | Impacto y ≈5 tics de moneda a 3,1-3,2 y 6,7 kHz | «¡Tchin!» + «tic-tic-tic»; −10,5 dBFS | 488,26; 664,66 | Alta |

- La confianza es la del análisis de audio de `observar.json`. El recuento de dosis por vertido (174 / 37 / 10) sale de la duración de cada muestra y se comprobó con fotogramas en solo 12 casos: tómese como estimación de confianza media.

### 9.3 Cadenas de tiempo

- Del toque al SELECT: 0,1-0,3 s (no se separa la latencia del juego de la del grabador).
- Completar: el sonido arranca con el sonido del vertido (0-0,14 s después). Corcho a +1,15 s desde el inicio de ese sonido. Ojo: el sonido del vertido empieza con la botella ya inclinada; en los fotogramas el chorro se ve hasta 0,5 s antes. Desde la salida de la botella, el corcho llega por tanto más tarde (≈ +1,4-1,6 s si el sonido arranca con el chorro; cálculo con la hoja de vertido). El desfase exacto: medirlo a 60 fps. La tela suena con el corcho y su swoosh de +1,5 a +2,1 s (la fuente da dos medidas: +1,65-2,1 s y +1,5-1,65 s). El hielo, a +1,12 s.
- Llave y puertas: UNLOCK a 0,58-0,68 s del inicio del vertido; madera a +0,87 s; tic final a +1,65 s. Unos 3 s en total.
- «No Available Moves»: 1,32-1,6 s tras el inicio del último vertido.
- Victoria: 0,61 s tras el último corcho. Si un anuncio la corta, sigue después donde se quedó (t = 629,34 → 659,83).

### 9.4 Lo que no suena

- No tienen sonido propio: los popups «Continue?», «Level Failed!» y «More Lives»; el revelado de una «?»; la cuerda de la botella atada; el destape de las chisteras no elegidas.
- No se encontró sonido para un destino no válido.
- Las dosis no suenan una a una y el tono no sube al llenarse la botella.

### 9.5 Mezcla

- El gesto repetido es lo más bajo: selección −38,5 dBFS; vertidos de −41 a −27 (la variante más baja de vertido queda por debajo de la selección).
- Interfaz y consecuencias, en medio: clic −30; candado −25; madera −19,5; completar, POP, WHOOSH y bloqueo de −16 a −18.
- Los premios son lo más fuerte: «Congratulations» −14,7; victoria −14,5; monedas −10,5. Entre el gesto y el premio hay de 20 a 28 dB.
- El sonido más fuerte de la sesión no es un premio: el golpe sin atribuir de 312 Hz (LOUD_THUMP, −10 dBFS, confianza baja), justo antes del Heavenly Deal y de la tienda (t = 1081,79 y 1089,77).

---

## 10. Love Water como contraejemplo

### 10.1 Qué es

- *Love Water*: otro juego de ordenar agua, grabado en un iPhone con iOS en español. 174 s, sin audio. Todo el vídeo es el nivel 143 (12 colores, 14 vasos y una ranura «+1»), que no se completa. No se ven victoria, menú, tienda ni anuncios a pantalla completa (t = 0-174).
- Comparte con Magic Sort: capacidad 4, vertido del tramo entero (3 dosis lavanda de una vez, t = 132-134), sin resaltar destinos al seleccionar (t = 11) y sin límite de movimientos ni de tiempo.

### 10.2 Rasgos concretos que no debemos repetir

| Rasgo | Lo que se ve | t |
|---|---|---|
| Publicidad de casino y dinero rápido | Banner inferior fijo que rota cada 5-10 s: tragaperras, «WIN REAL CASH», bingo, brókers, mercados de predicción y apps de dinero rápido, varios en español. Banner de 788 × 122 px (320 × 50 pt) | 21-174 |
| Widget de dinero de terceros | Barra «$500 · EARN CA$H» de BestApps con un punto de aviso; ocupa el hueco del banner y rota con él (probablemente publicidad, no determinado) | 11-20 |
| Tooltips mal hechos | Dos tooltips azules en inglés que se solapan (uno corta el texto del otro) y tapan la fila superior al empezar; uno tiene una falta de gramática («it's» por «its») | 10-14 |
| Líquido plano | Bandas de color sólido de ≈80 px por dosis, sin degradado, menisco ni brillo propio; parecen franjas de bandera | 7 |
| Colores confusos | 12 colores de saturación máxima con pares casi iguales (burdeos, frambuesa y rosa; morado oscuro y lavanda; verde oscuro y lima; crema y mostaza; marrón y naranja) y colores oscuros sobre fondo casi negro (#4c006f, #8f002e y #05611e sobre #211930). Sin símbolos para daltónicos | 7 |
| Recipiente genérico | Vaso de cerveza tipo pilsner (371 px de alto; boca 127 px; cintura 103 px), contorno gris de 2-3 px y un solo reflejo blanco vertical | 7 |
| Rejilla rígida sin soporte | 5 × 3 vasos idénticos flotando sobre un fondo plano | 7 |
| Fondo de stock | Ilustración genérica de oasis nocturno en el tercio superior, cortada por un morado negro liso (#211930) en el resto | 7 |
| Venta dentro del puzle | Un vaso fantasma «+1» con insignia de anuncio fijo en el hueco 15 de la rejilla | 7 |
| Deshacer a 0 | Botón de deshacer con «0» e insignia de anuncio en un nivel largo (no se usa en el vídeo) | 7 |
| Vertido que tapa el tablero | El vaso cruza filas por encima de otros vasos y llega casi en horizontal (78-80°); parte se sale de la pantalla | 14, 150 |
| Vaso completado sin cambio | Tras la celebración, el vaso se ve igual que los demás: sin tapa, sello ni brillo | 95, 134, 174 |
| Celebración genérica | Las mismas serpentinas lima, amarillas, cian y verdes para todos los colores | 95, 134, 161 |
| Dificultad solo por cantidad | Más colores y vasos, sin mecánicas nuevas en el nivel 143; pausas de 10, 16, 25 y 29 s sin mover | 15-131 |
| HUD sin coherencia | Píldora roja coral (#ed2a54), avatar de otro estilo (volcán al atardecer), botón gris, tooltips azul eléctrico (#0065ff) y barra verde menta | 10-15 |
| Carga sin marca | Pantalla con solo «LOADING..» sobre morado liso | 5-6 |
| Icono que no se parece al juego | Vaso cilíndrico en 3D brillante, mientras el juego es 2D plano | 0 |

### 10.3 Correcciones a lo que decía `observar.json` sobre Love Water

- Las serpentinas **no** tapan el HUD: pasan por detrás de la píldora, el avatar, la insignia y el título. Orden de capas: fondo < vasos < celebración < HUD (t = 134, 161).
- El ángulo de vertido depende del llenado: ≈55° con el vaso lleno (t = 133) y 78-80° con 2 dosis (t = 150).
- La selección sube ≈60 px a 1080 de ancho (≈16 % del vaso), no 20-30 px (t = 128, 132).
- Que *Nut Sort* sea del mismo estudio: **no observado**.

### 10.4 Lo que sí funciona en Love Water

| Acierto | Lo que se ve | t |
|---|---|---|
| Entrada directa | El tablero sale unos 5 s después de abrir la app, sin menús | 2-7 |
| Franja de aire visible | Cada vaso deja una franja oscura vacía en la boca, sobre las dosis: el espacio libre se lee de un vistazo | 7 |
| Reiniciar siempre a mano | Fila fija de 4 botones bajo el tablero: menú, reiniciar, deshacer y +vaso | 7 |
| Evento con desbloqueo | Avatar del evento con temporizador «6d» y fichas «5»; tooltip «Complete Event 6 to get it's unique rewards!». Arriba a la derecha, una función bloqueada que se abre jugando el evento: «Play event to unlock» | 10 |
| Ciclo fluido | Unos 1 s por vertido; rachas de 15 vertidos seguidos | 145-164 |
| Vertido legible | Chorro visible, llenado desde abajo y líquido más o menos nivelado en el vaso inclinado | 132-134 |
| Sin resaltar destinos | Igual que D-002 de Tintalú | 11 |

- Valoración de `observar.json` (no medida): el fondo nocturno de poco brillo cansa poco la vista y deja destacar los colores (t = 7).
- Qué abre la función bloqueada y qué premios da el evento: **no observado**.

---

## 11. Lo que no se pudo observar y cómo conseguirlo

### 11.1 Preguntas abiertas

| Pregunta | Por qué no se vio | Cómo conseguirlo |
|---|---|---|
| ¿Cómo es la música? | Desactivada en ajustes (t = 4) | Grabar 2-3 min con la música activada en Home, en el tablero y en la victoria |
| ¿Qué hace el comodín de flechas cruzadas? | No se usó; 255 monedas no alcanzan | Usarlo con monedas o con una unidad gratis del Daily Bonus o de Journey (nivel 390); grabar |
| ¿Cómo se ven y animan deshacer y añadir botella como comodines, y cómo se ve el inventario? | No se usaron | Igual que arriba |
| ¿Qué da «Play On» en «Continue?» y en qué se diferencia del de «No Available Moves»? | Nunca se pagó ahí | Pagarlo una vez y grabar |
| ¿Cuántas vidas da «Refill»? | No se pagó | Pagarlo o abrir el «i» si lo hay |
| ¿Hay vertido parcial cuando el tramo no cabe? | No se dio el caso | Preparar la jugada en un nivel fácil y grabarla |
| ¿Qué pasa al tocar un destino no válido? | No se vio; no hay sonido propio | Tocar a propósito un destino inválido con la grabación activa |
| ¿Se pueden encolar toques durante la animación? | Muestras cada 2 s | Grabar a 60 fps con «mostrar toques» y tocar rápido |
| Regla exacta de la botella atada (¿solo recibe su color?) | No se probó otro color | Intentar verter otro color en una atada vacía; grabar a 60 fps |
| ¿Se pueden levantar botellas congeladas o tapadas por tela? | El jugador no lo intentó | Tocarlas y grabar |
| Animaciones en detalle (revelado de «?», llave, armario, tela, victoria) | Muestras cada 2 s | Grabar a 60 fps o sacar fotogramas cada 50 ms de esos tramos |
| ¿Cómo se eligen las variantes de cada reintento? ¿Se quita un color al fallar? | Solo 6 y 12 intentos | Fallar muchas veces el mismo nivel y anotar cada variante |
| ¿Qué dispara el Heavenly Deal y la tienda? | No se ve el toque (t = 1081, 1089) | Tocar un comodín sin monedas con «mostrar toques» activo |
| ¿Qué significa «x5»? ¿Se cobran 100 al ganar un SUPER HARD? | No se ganó ninguno | Ganar un SUPER HARD y mirar el cobro en Home |
| ¿Qué multiplica Magic Streak y qué cuenta su barra? | Los «i» no se abrieron | Abrir el «i» de Magic Streak y ganar varios niveles seguidos |
| Contenido de cofres y fundas (Broom Streak, Magic Streak, Daily Bonus) | No se abrió ninguno | Ganar los hitos o elegir una funda en el Daily Bonus |
| ¿Qué son los iconos de Home sin identificar (objeto rosa, pergamino, timón, tridente, rueda 0/3, taladro, calendario, trofeo 2/50)? | No se abrieron | Tocar cada uno y capturar su pantalla |
| Pestañas de clasificación, equipo y colección; «More Offers» | No se abrieron | Abrirlas y capturar |
| ¿Qué control abre Journey y qué hace «USE»? ¿Se adelanta la temática siguiente? ¿Hay temáticas más allá del 99? | Sin toque visible (t = 1187); el jugador no pulsó «USE»; entre 360 y 420 no hay tarjetas (t = 1188) | Grabar Journey completo, despacio y de arriba abajo; pulsar «USE» y ver qué cambia en Home o en el tablero; mirar si la escena siguiente sale bloqueada o en silueta |
| ¿Qué decide si el HUD muestra Broom Streak o el saldo? ¿Cuándo entra y se reinicia cada racha? | El HUD y Home se contradicen (t = 0 frente a t = 7; §7.2) | Anotar el HUD y el icono de Home al empezar cada nivel durante un día entero |
| ¿Qué vibra (vertido, sellado, bloqueo, botones)? | El menú de ajustes tiene un interruptor de vibración (t = 4), pero la grabación no capta la vibración | El dueño juega 2 min con la vibración activada y anota qué acciones vibran y con qué fuerza |
| ¿Qué da el «Join» de Boat Rush y qué cuenta como etapa? | El jugador cerró el popup | Unirse y jugar 2-3 niveles |
| Regla del límite de intersticiales | Solo 3 en 20 min | Sesión más larga anotando la hora de cada anuncio y de cada «Try Again» |
| ¿«Close» del anuncio está inactivo hasta acabar el vídeo? | La zona de vídeo sale negra; el botón se ve igual | Grabar con otro método o anotar en vivo cuándo responde |
| ¿Las vidas ilimitadas evitan perder vidas reales al fallar? | El contador de vidas normales no se ve durante ∞ | Mirar el contador cuando acaben las ∞ tras varias derrotas |
| **Vidas ilimitadas de 15 min que exigen un anuncio** | En esta grabación las ∞ 15m fueron gratis (t = 676) | Si el dueño lo vio en otra sesión, grabarlo y anotar dónde sale |
| Onboarding, primeros niveles, primer anuncio, tutoriales de cada mecánica | La grabación empieza en el 387 | Instalar en un dispositivo o cuenta nueva y grabar la primera hora |
| Otras mecánicas, capacidades de botella o tableros de 2 o 4 filas | Solo se vieron 3 niveles | Grabar niveles de otros tramos |
| Love Water: victoria, intersticial, menú, tienda, uso de deshacer y «+1» | El vídeo no completa el nivel | Otro vídeo que complete un nivel |

### 11.2 Discrepancias con la investigación web

| Tema | Investigación web | Esta grabación |
|---|---|---|
| Regeneración de vidas | ≈25 min por vida (`magic-sort/b-economia-monetizacion.md`, reseñas) | 30:00 por vida (4 lecturas que cuadran; §4.5) |
| SKU «sin anuncios» | «No existe» (`09-referencia-magic-sort.md`; `b-economia-monetizacion.md`) | El Apprentice Bundle (COP 22,000) lleva un icono «ADS» tachado (t = 1084). Que signifique «sin anuncios» es deducido |
| Dificultad adaptativa | El segundo intento quita un color (09) | No se vio quitar un color. Cada reintento elige otra variante de la misma plantilla (§2.9) |
| Rewarded de vida | Sale «tras varios fails sin pagar» (`b-economia`) | Sale en «More Lives» al quedarse con 0 vidas (t = 288) |
| Precio de las monedas | 1 000 monedas = 1,99 USD (09) | 1 000 monedas = COP 9,500 en la cuenta del dueño (t = 1084). Moneda distinta: no se pueden comparar sin tipo de cambio |
| Precio de las monedas en COP | 1.000 monedas = 9.900 COP, «tiers de Apple» (research/09; citado en GDD 01 y 05 §4.2) | En Google Play Colombia: COP 9,500 (t = 1084). El GDD cita el precio de Apple; para Android vale el de Play (propuesta §10.1) |
| Continuar | 900 monedas (09) | Coincide: 900 (t = 84, 86) |

---

## 12. Ganchos y fricciones observados

Recogidos de las claves `hooks` y `friction` de los seis tramos de `observar.json` (tramo 1: t = 0-228; 2: 190-346; 3: 340-510; 4: 514-710; 5: 714-938; 6: 928-1206), comprobados contra esta ficha. El hecho es observado; **que enganche o que frustre es deducido** (interpretación de los observadores, sin datos de jugadores).

### 12.1 Ganchos

| # | Gancho | Lo que se ve (t) | Tramos |
|---|---|---|---|
| G1 | Cada color completado destapa algo | Tela que sube, hielo que estalla, armario que revienta (t = 27, 362, 398, 504, 712, 814) | 1, 3, 5, 6 |
| G2 | La base bajo la tela deja adivinar el contenido y empuja a planificar | Color o negro de la dosis del fondo bajo cada tela (t = 0) | 1 |
| G3 | Capas «?»: curiosidad y una pequeña recompensa al revelarse | Revelados con destellos (t = 238, 564, 956); el 388, un nivel normal, lleva 16 dosis ocultas (t = 496) | 1, 2, 4, 6 |
| G4 | Cadena de descubrimiento | Llave → candado → armario → más «?», más llaves y botellas atadas (t = 696-698, 950-952, 1034-1036) | 6 |
| G5 | El «casi» | Intento D del 389: 6 botellas completas y 2 de 3 armarios abiertos antes del bloqueo, y entonces «Play On 900» (t = 806-888) | 5 |
| G6 | Mecánica nueva justo después de un nivel duro | Hielo en el 388, tras el 387 (t = 496) | 3 |
| G7 | La racha y el atasco con 1 vida hacen tentador pagar | Paga 900 de 1115 con 1 vida (t = 404-408) | 3 |
| G8 | Celebración plena aunque se haya pagado | «PERFECT!» tras la botella extra (t = 482). También es fricción (F24) | 3 |
| G9 | La oferta llega en el momento justo y con la ayuda a la vista | «Play On 900» con la pestaña «Add Bottle» en el bloqueo (t = 84, 404) | 1 |
| G10 | Al fallar se enseña lo que se pierde | «Rewards: 20» (t = 6,5) y «Rewards: 100» (t = 748) | 1, 2, 6 |
| G11 | Racha visible durante la partida | Cofre «Broom Streak 0/2» en el HUD (t = 0, 938) | 1, 2 |
| G12 | Multiplicador que cae al fallar y se enseña en el fallo | Magic Streak x1-x100; el sombrero baja de x5 a x1 (t = 484, 748-750) | 4, 5, 6 |
| G13 | Cada reintento trae una variante («esta vez me toca uno mejor») | Variantes A-D del 387 (t = 9, 144, 342). También es fricción (F9) | 1, 2, 5, 6 |
| G14 | Reintento inmediato | «Try Again» lleva al tablero sin carga ni anuncio en 4 de 5 casos (t = 752, 782, 806, 894) | 5 |
| G15 | La cadena de premios tras ganar rescata la sesión | Con 1 vida, el jugador vuelve con ∞ 44:55 (t = 664-682) | 4 |
| G16 | El bonus diario enseña lo que no salió («ganas de volver mañana») | Las 8 chisteras no elegidas se destapan (t = 672-674) | 4 |
| G17 | Competición social | Clasificación del Duck Festival con nombres y clubes con nombre mexicano; puesto 36 (t = 678) | 4 |
| G18 | El nivel difícil se vende como reto con premio mayor | Botón rojo «Super Hard» con «x5»; calaveras (t = 666, 684) | 4, 6 |
| G19 | «Solo me falta uno» | Premio de Journey en el 390, a un nivel (t = 1188) | 6 |
| G20 | Siempre hay algo pendiente | Iconos con temporizador, contadores y globos «!» en Home (t = 7, 664, 1178) | 1, 4, 6 |
| G21 | Vidas ilimitadas que quitan la barrera de reintentar | ∞ desde t = 682 hasta el final | 6 |
| G22 | Microrrecompensa en cada botella completa | Espiral de destellos y corcho (t = 26, 476, 852) | 1, 5 |
| G23 | Ofertas con cuenta atrás tras varias derrotas | Heavenly Deal «Ends in: 2h 35m», «90% OFF» (t = 1082) | 6 |

### 12.2 Fricciones

| # | Fricción | Lo que se ve (t) | Tramos |
|---|---|---|---|
| F1 | Nivel 387 muy difícil sin etiqueta | 1 abandono y 4 bloqueos antes de pagar (t = 5-404) | 1, 2, 3 |
| F2 | El HUD no enseña monedas ni vidas | El saldo solo sale cuando se pide pagar (t = 0, 84); en el 389 tampoco (t = 938) | 1, 3, 4 |
| F3 | Continuar cuesta casi todo el saldo | 900 de 1115 (t = 84, 404) | 1 |
| F4 | Rendirse exige tres pasos con la misma oferta dos veces | «No Available Moves» → «Continue?» → «Level Failed!» (t = 84-86, 1018-1022) | 1, 5, 6 |
| F5 | Salir desde ajustes también cuesta una vida | t = 5 | 1 |
| F6 | Intersticial tras fallar y otro que corta la celebración | t = 196, 629 | 1, 2, 4 |
| F7 | «Close» que no responde al toque | t = 212, 638, 652 | 1, 2, 4 |
| F8 | Anunciante de trading y predicciones en un puzle casual | Kalshi (t = 196, 290, 630) | 1, 2, 4 |
| F9 | Lo aprendido no sirve en el reintento | Variantes distintas (t = 9, 144, 342, 752) | 1, 2, 5, 6 |
| F10 | Flujo irregular tras fallar | A veces Home (t = 7, 88), a veces el tablero (t = 144) | 1 |
| F11 | Tras el anuncio de vida vuelve a «Level Failed!» | Un toque más (t = 338) | 2 |
| F12 | Con 0 vidas, espera dura | 25:39 si no se ve el anuncio ni se pagan 900 (t = 288) | 2 |
| F13 | Muro de monedas | Con 255 no alcanza para nada (t = 714-1206) | 4, 5, 6 |
| F14 | Bloqueos casi inmediatos en SUPER HARD | A los ≈ 6 s, tras 1-2 vertidos, sin botellas vacías al empezar (t = 894-900, 976-982, 1066-1072) | 5, 6 |
| F15 | Reglas de las mecánicas sin explicar | Botella atada, tela, hielo (t = 342, 496, 888) | 3, 4, 5 |
| F16 | La botella atada provoca bloqueos poco legibles | t = 888-890 | 5 |
| F17 | Mensaje que no cuadra | «You will lose 1 life!» con ∞ activas (t = 803, 973, 1174) | 6 |
| F18 | Premio que no se cobra | «Rewards: 100» tras perder (t = 748, 904) | 6 |
| F19 | Ofertas en mitad del intento | Heavenly Deal y tienda (t = 1082-1092) | 6 |
| F20 | Anuncio jugable que saca a Google Play | X pequeña; el jugador sale por el escritorio (t = 926-936) | 5, 6 |
| F21 | Home sobrecargado | ≈ 12 iconos sin nombre (t = 488, 1178) | 3, 6 |
| F22 | Botón de Home sin actualizar | Sigue en «Level 387» tras ganar (t = 488) | 3 |
| F23 | Pausas largas sin ayuda con las «?» | t = 574-586, 602-610 | 4 |
| F24 | «PERFECT!» tras pagar: elogio vacío | t = 482 | 3 |
| F25 | Cadena larga de pantallas tras ganar | ≈ 12 s de popups en el 387 (t = 482-496); 58 s con anuncio en el 388 (t = 626-684) | 3, 4 |

---

## Anexo. Dónde está el material

Todo en `Logs/device/magicsort/` (ignorado por git; no se sube porque son capturas de un juego de terceros):

- Grabación original: `ms_largo.mp4` (20 min, 1080 × 2340, con sonido).
- Observaciones de los 6 tramos, Love Water y audio: `analisis/observar.json`; transcripciones, medidas y revisión: `analisis/ficha_workflow_result.json`.
- Fotogramas: `analisis/ov/f_NNNNNN.jpg` (540 px, cada 2 s; NNNNNN = t × 10), `analisis/det_ms/d_NNNNNN.jpg` (1080 × 2340) y hojas de contactos en `analisis/sheets_ms/`. Love Water: `analisis/lw/`, `analisis/det_lw/` y `analisis/sheets_lw/` (vídeo original en `Logs/device/lovewater/love-water.mp4`).
- Audio: `analisis/audio/` (WAV a 16 kHz, `final_hits.json`, `pours.json`, espectrogramas en `png/catalog/`).
- Medidas: scripts en `analisis/meas_ms/`; recortes en `analisis/crops7/`, `analisis/zz/` y `analisis/lwz2/`.
- Hoja de vertido (cada 100 ms): `analisis/ms_pour_sheet.png`.

Partida jugada por adb en el mismo teléfono (pantalla forzada a 1080 × 1920): `Logs/device/ms-01-actual.png` … `ms-07.png`, `ms_pour.mp4` y `ms_play1.mp4`.
