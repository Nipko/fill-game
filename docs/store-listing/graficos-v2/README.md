# Gráficos de la ficha de Play, versión 2 (D-065)

Conceptos de capturas y gráfico de funciones para la ficha de Google Play, en es-419, en y pt-BR. Encargo: `docs/dev/traspaso/spark-graficos-tienda.md`. Investigación previa: `../investigacion-fichas.md`. El juego actual de la pista interna sigue en `../graficos/` y no cambia.

**Para elegir:** abre `index.html` en el móvil o en el escritorio. Es la hoja de contactos, con los conceptos lado a lado por idioma.

| Concepto | Estado | Carpeta |
|---|---|---|
| **A · Gameplay héroe** | **Hecho** (2026-10-30): 8 capturas × 3 idiomas + gráfico de funciones | `A/` |
| B · Panorámica del río | **Hecho** (2026-10-30), como variante de experimento (Q-GT-2) | `B/` |
| C · Cartel con personajes | **Hecho** (2026-10-30), como variante de experimento (Q-GT-2) | `C/` |

Los tres conceptos usan las mismas tomas reales y los mismos rótulos, así que una prueba de ficha entre ellos mide solo la presentación.

## A · Gameplay héroe

**Qué es.** La pantalla real del juego, grande y a sangre en su marco crema, ocupa el 75-82 % del alto. Arriba va un rótulo corto en la placa de madera. Detrás va un fondo del mundo, generado con IA, que cambia en cada captura.

**Por qué es así** (conclusiones de `../investigacion-fichas.md` §4):
- el juego en acción desde la 1 (12 de las 13 fichas líderes lo hacen);
- el tablero grande: el 76 % del alto, frente al ~40 % de `../graficos/`;
- rótulos de 2-4 palabras, una idea por captura, que ocupan como mucho el 10 % de la imagen (Google admite hasta el 20 %);
- un fondo distinto por mundo para que al desplazar se note la variedad, que en Tintalú es real: Brasil, Colombia, Ecuador, el Mapa y el muelle;
- texto y UI traducidos en los 3 idiomas (solo 3 de 13 líderes lo hacen).

| # | Archivo | Qué se ve (captura real del teléfono) | es-419 | en | pt-BR |
|---|---|---|---|---|---|
| 1 | `01-vertido-encuentro.png` | Nivel 42 (Encuentro de las Aguas) a mitad de un vertido, con el chorro cayendo en una cuia vacía | Vierte y tiñe el río | Pour and tint the river | Despeje e tinja o rio |
| 2 | `02-todo-encaja.png` | El mismo nivel resuelto: seis cuias selladas con su anillo y el confeti de plumas | ¡Todo encaja! | Everything clicks! | Tudo se encaixa! |
| 3 | `03-mapa-del-rio.png` | Mapa del río, vista Cuenca: la canoa en Manaos, los mundos y los países | Remonta el Amazonas | Travel up the Amazon | Suba o Amazonas |
| 4 | `04-dificil-tarapoto.png` | Nivel 129 (Difícil, Tarapoto, Colombia), 10 cuias, a mitad de un vertido | Difícil, pero justo | Hard, but fair | Difícil, mas justo |
| 5 | `05-titino-capi-rosa.png` | El inicio: TiTino en su rama, Capi en la canoa y Rosa en el río, con las tarjetas del Reto y la Bitácora | TiTino te acompaña | Meet TiTino | TiTino te acompanha |
| 6 | `06-patrones.png` | Nivel 241 (Yasuní, Ecuador), 11 cuias con los patrones de fruta | Patrones para todos los ojos | Patterns for every eye | Padrões para todos os olhos |
| 7 | `07-bitacora.png` | La Bitácora del río con la página del día sellada y su premio | Un reto nuevo cada día | A new challenge every day | Um desafio novo por dia |
| 8 | `08-reto-del-rio.png` | El tablero del Reto del río del día, al amanecer | Sin wifi, a tu ritmo | Offline, at your pace | Sem wi-fi, no seu ritmo |
| — | `feature-graphic.png` | Fotograma real de la intro de la marca (D-051): la cuia vierte y el río se tiñe de açaí, sobre un amanecer en el río generado con IA, con el logotipo v2 en su placa petróleo | (solo el nombre, en el logotipo) | | |

**Q-GT-3, rótulos [P], con la recomendada aplicada.**
- **Más cortos** que los de §6 de `../<idioma>/ficha.md`.
- **Iguales al canon:** 5 (con «Meet TiTino») y 6.
- **Nuevos** (1, 2, 3, 4, 7 y 8): la misma idea en los 3 idiomas. Las tildes, la ñ y el «¡» están comprobados en la imagen.
- **Ninguno lleva** cifras, «gratis», «nuevo» como reclamo ni llamadas a la acción. «Un reto nuevo cada día» describe una función (el Reto diario), no anuncia una novedad.
- Si el dueño los aprueba, el integrador pone al día `../<idioma>/ficha.md` §6.

**Cómo se hizo** (procedencia completa en `A/manifest.json`):
1. **Tomas:** en el teléfono de pruebas (Samsung SM-S938B), con el `dev.apk` instalado el 2026-10-30 a las 10:19, que es posterior al último cambio de código de `integracion-fase1-fase2` (06:57).
   - **Cómo se llegó a cada pantalla:** `adb shell input tap` guiado por `AF_LAYOUT` y `AF_STATE`, con los extras `af_lang`, `af_level`, `af_seeds` y `af_max_level`.
   - **Momentos en movimiento** (vertidos y victoria): son fotogramas de `adb shell screenrecord`.
   - **Barra de estado:** el modo demo de Android estuvo activo durante las tomas; al terminar, el teléfono se dejó como estaba.
2. **Los píxeles del juego no se tocan:** solo se recortan (siempre por encima de la barra de comodines, así que no sale la marca «Development Build») y se escalan.
3. **Fondos:** Z-Image Turbo (Apache 2.0), en ComfyUI 0.43.0 sobre la RTX 5080.
   - Prompts de pura naturaleza, sin imágenes de referencia ni «al estilo de».
   - Se descartaron las semillas que sacaban casas, barcas o personas.
4. **Montaje:** con Pillow, en el equipo de Spark. Rótulos en Baloo 2 ExtraBold (OFL, del juego), crema sobre madera.

## B · Panorámica del río

**Qué es.** Un solo río generado con IA que cruza las 8 capturas de izquierda a derecha, del estuario a los Andes. Al deslizarlas en Play se lee como un viaje.
- **El viaje:** la canoa de Capi (arte real del juego) avanza un poco en cada captura.
- **Cada captura se sostiene sola:** el tablero entero va dentro de su captura y no se parte entre dos (conclusión 9 de la investigación). La pantalla real ocupa ~61 % del alto, para que se vea el río.
- Mismas tomas y mismos rótulos que A.

**Lo que conviene mirar:**
- **Nitidez:** la panorámica se generó a 2.560 × 768 y se amplió ×3,4. Z-Image se rompe por encima de unas 3 veces más ancho que alto. Le puse un desenfoque leve para que se lea como fondo lejano.
- **El orden del río no es el geográfico exacto:** los Andes salen en el centro, no al final.
- **Ningún líder de la muestra usa este formato,** así que la recomendada es probarlo como variante (Q-GT-2).

## C · Cartel con personajes

**Qué es.** Las capturas 1 y 2 son carteles: TiTino cuelga de su rama entre el rótulo y la pantalla, y Rosa y Capi en su canoa nadan debajo. La pantalla real va en el centro, sin que nada la tape. Las capturas 3-8 son las de A, idénticas.

**Los personajes no los dibujó la IA.** Son los renders reales del juego (D-058), recortados del fondo plano de `docs/art/personajes-3d/fase2/despues/p3d_11_lamina_personajes.png`. Así son fieles a los modelos por construcción (encargo §2). La IA solo hizo el fondo.

**[P] Q-GT-5, con la recomendada aplicada.**
- El encargo sugería renders y poses nuevas con IA. Preferí el arte real: con las herramientas de hoy, una IA redibujaría los personajes con diferencias, y para generarlos fieles haría falta Qwen-Image-Edit, un modelo nuevo de unos 20 GB.
- Las poses son las del juego (TiTino colgado, Rosa nadando, Capi remando).

**Lo que conviene mirar:**
- La pantalla real ocupa algo menos (~55 % del alto) para dejar sitio a los personajes.
- Los personajes del juego son de estilo «juguete brillante», y los fondos de IA, de selva más realista. Se nota algo el cambio de estilo.

## Lo que conviene mirar antes de subir A

- **Fechas a la vista:**
  - la 5 enseña la tarjeta «Reto del río · 30 oct»;
  - la 8, «Reto del río» y su tablero del día.
  - Google pide evitar lo que caduca. **[P] Recomendada:** dejarlo, porque es la UI real y no promete nada; si molesta, se repite la toma el día de la subida.
- **La Bitácora (7):** en es-419 dice «Página 1 de 30» y en en y pt-BR, «Page 2» y «Página 2». Es el estado real del perfil: la página del día se selló en la toma en español.
- **El Mapa (3), vista Cuenca:** las placas de los mundos se amontonan arriba (ya anotado en `../graficos/README.md`).
- **Las semillas (1.250-1.310):** vienen del extra de desarrollo `af_seeds`, para que el contador no salga en 0. Es un valor que un jugador tiene de verdad.
- **Revisión cultural** (07 §8.2):
  - los fondos de IA representan lugares reales: el Encuentro de las Aguas, la playa de río del Tapajós y lagos de Colombia;
  - no tienen personas, aldeas ni símbolos;
  - el asesor cultural debería verlos antes de producción.
- **Gráfico de funciones (Q-GT-1):**
  - el logotipo y la intro van a los lados del centro, para que no los tape el botón de reproducir si hay vídeo;
  - el logotipo horizontal incluye el icono, como el gráfico actual de la v2.
- **Reproducir:** el montaje y las tomas se hicieron con scripts del equipo de Spark, que no entran en el repositorio (encargo: nada de paquetes nuevos). Los parámetros de cada imagen están en el manifiesto.
