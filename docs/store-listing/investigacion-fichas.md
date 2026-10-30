# Investigación: qué hacen las fichas de Google Play que funcionan (fase 1 de D-065)

Fecha: 2026-10-30 · Autor: equipo Spark (agente) · Encargo: `docs/dev/traspaso/spark-graficos-tienda.md` §1 · Para: el dueño y el integrador.

**Método.**
- **Muestra:** 13 fichas públicas de Google Play, vistas el 2026-10-30:
  - las ocho primeras de la búsqueda «water sort» en EE. UU.;
  - un *ball sort*, un *nut sort* y un *goods sort*;
  - y dos puzles de referencia por la calidad de su ficha, Royal Match y Woodoku.
- **Tiendas:** las mismas fichas en EE. UU. (`gl=US&hl=en`), Brasil (`gl=BR&hl=pt-BR`), México (`gl=MX&hl=es-419`) y Colombia (`gl=CO&hl=es-419`).
  - Colombia es igual que México en 10 de las 13; en las otras tres cambia el orden o la temporada.
- **Qué se miró:**
  - las 8 primeras capturas de teléfono por tienda;
  - si la ficha web tiene tráiler (`data-trailer-url`);
  - las descargas que Play muestra en EE. UU.
- **Dónde están las capturas de referencia:** solo en el equipo de Spark, no en el repositorio, porque son de terceros.
- **Limitaciones:**
  - la ficha web no enseña el gráfico de funciones, así que no se pudo estudiar [NV];
  - las fichas cambian por temporada: dos ya tenían Halloween en EE. UU.;
  - las cifras de los blogs de ASO van marcadas [NV].

## 1. Las 13 fichas

«1-3» son las capturas que se ven sin desplazar en el teléfono. «Texto»: dónde va el rótulo y cómo es. «Idiomas»: qué cambia en Brasil y México respecto a EE. UU.

| App (descargas EE. UU.) | Capturas 1-3 (EE. UU.) | Formato y rasgos | Idiomas | Tráiler |
|---|---|---|---|---|
| [Magic Sort!](https://play.google.com/store/apps/details?id=com.grandgames.magicsort) · Grand Games (10M+) | 1, tablero de 24 frascos con un vertido; 2, tubos altos a medio llenar; 3, mapa de eventos con premio | Gameplay a sangre, sin marco de teléfono. Banda morada abajo (~20 %) con la mascota gato y el caldero. Rótulos de 2 palabras en mayúsculas con exclamación («SORT WATER!»). Las 4 y 8 son ilustración (oro, gatos, logotipo) | Ninguno: inglés en todas | No |
| [Water Sort Puzzle - Sort Color](https://play.google.com/store/apps/details?id=water.sort.puzzle.game.color.sorting.free) · Guru (50M+) | 1, vertido en primer plano sobre fondo negro; 2, nivel 777 con 32 tubos; 3, otro vertido | Gameplay plano y oscuro, rótulo blanco abajo de 2-3 palabras («Tap to Pour», «2000+ Levels»). La 7 es una llamada a la acción («Play Now») | Rótulo traducido (PT y ES); UI en inglés; cambia el orden por tienda | Sí |
| [Get Color - Water Sort Puzzle](https://play.google.com/store/apps/details?id=com.zm.watersort) · Tripledot (50M+) | 1, vertido en arco sobre tablero lleno; 2, recipiente especial (pato) en el centro; 3, obstáculos de enredadera | **Cada captura con otro fondo y otro color de banda** (azul, morado, verde, naranja): se lee como variedad de mundos. Rótulos de 2-3 palabras en mayúsculas («SORT, POUR, RELAX», «NO TIMER, PURE ZEN»). Cierra con «50M+ PLAYERS» y «FREE TO PLAY» | Ninguno | Sí |
| [Water Sort! 3D](https://play.google.com/store/apps/details?id=water.sort.master.color.match.offline.puzzle) · Flyfox (1M+) | 1, recipiente especial gigante (copa de helado) en el centro; 2, conejo de vidrio con vertido; 3, cuencos de corazón | Fondo nocturno de playa común a todas; banda de ola con salvavidas; rótulo de 2-3 palabras. El recipiente especial grande es el gancho | Ninguno | No |
| [Water Match - ASMR Water Sort](https://play.google.com/store/apps/details?id=com.red.sortingstore.android) · Youyoutang (10M+) | 1, jarrón de vidrio de loto rosa; 2, tetera y rana de vidrio con vertido; 3, gato de vidrio llenándose | Recipiente decorativo grande en el centro de un tablero real; banda morada con rótulo sensorial («CALM WATERS», «POUR & RELAX»). Solo 5 capturas de teléfono | **Rótulos adaptados, no calcados** (PT «Águas calmas», ES «Aguas tranquilas»); UI sin texto | Sí |
| [Water Sort Inc.](https://play.google.com/store/apps/details?id=com.magic.potion.sort.puzzle) · HM Games (500K+) | 1, tablero con un frasco brillando al completarse; 2, vertido con estela; 3, corazones grandes | Sin rótulos: solo gameplay neón con efectos | — (sin texto) | Sí |
| [Wizard Water Sort](https://play.google.com/store/apps/details?id=com.etermax.liquid) · etermax (5K+, reciente) | 1, ilustración de un niño mago con un gato y el logotipo; 2, tablero simple; 3, ventana de victoria con monedas | Abre con un personaje humano (no aplicable: 07 §8.1). Rótulo en banda morada con personaje | **Todo traducido: UI y rótulo** | Sí |
| [Water Sort Puzzle](https://play.google.com/store/apps/details?id=com.gma.water.sort.puzzle) · IEC (100M+) | EE. UU.: 1, tubos altos con vertido; 2, corazón grande; 3, vertido. Banda de color con una gota mascota | Mascota (gota de agua) en la banda de todas | Brasil y México tienen **otro juego, de Halloween**, sin rótulos y con el logotipo en la 1 | Sí |
| [Ball Sort Puzzle](https://play.google.com/store/apps/details?id=ball.sort.puzzle.color.sorting.bubble.games) (50M+) | 1-3, tableros de bolas sobre fondo negro | Gameplay crudo, sin rótulos; la 6 es la victoria | — (sin texto); cambia el orden en México | Sí |
| [Nut Sort](https://play.google.com/store/apps/details?id=com.zm.nuts) (10M+) | 1, columnas altas de tuercas; 2, una tuerca volando; 3, tablero mixto | Banda morada con rótulo de 3-5 palabras («PASS HUNDREDS OF LEVELS»). La 7 es un personaje con llamada a la acción («INSTALL NUT SORT NOW») | Ninguno | No |
| [Goods Sort](https://play.google.com/store/apps/details?id=com.veraxen.goodssort) · Veraxen (1M+) | EE. UU. (Halloween): 1, estanterías con una calabaza gigante saliendo del tablero; 2, murciélago con hamburguesas; 3, fantasma | **Un objeto del juego ampliado sale del tablero real**; rótulo de una palabra («Match», «Sort») | Brasil y México: el juego normal, sin Halloween, con el rótulo y la UI traducidos («¡Relájate!», «Clasifica», «Nivel») | No |
| [Royal Match](https://play.google.com/store/apps/details?id=com.dreamgames.royalmatch) · Dream Games (100M+) | 1, el rey en peligro sobre el tablero; 2, un tiburón; 3, rocas que caen | Gameplay real con el personaje en apuros y una mano señalando la jugada. Rótulo de acción y meta en mayúsculas («SAVE THE KING!») | Rótulo traducido (PT). En México y Colombia, capturas horizontales de dos paneles [NV: puede ser el juego de tableta] | No |
| [Woodoku](https://play.google.com/store/apps/details?id=com.tripledot.woodoku) · Tripledot (100M+) | 1, logotipo arriba y tablero con piezas que salen en 3D; 2, combo ×5 con piezas volando; 3, el «cerebro» | Tablero real realzado: las piezas salen del plano con luz. **Una sola paleta (madera) en todas, ligada al icono**; banda de madera con rótulo de 4-6 palabras | **Todo traducido: UI y rótulo** | Sí |

## 2. Lo que sale de la muestra

- **La captura 1 enseña el juego en 12 de 13.** La excepción es Wizard Water Sort, que abre con una ilustración y es la de menos descargas. Las tres primeras son gameplay en 11 de 13.
- **Formato:**
  - ninguna usa marco de teléfono ni panorámica continua en el teléfono;
  - todas son capturas sueltas, verticales y a sangre;
  - el gameplay ocupa el 70-80 % del alto y el rótulo va en una sola banda, casi siempre abajo.
- **Rótulos:** 11 de las 13 llevan rótulo, y 9 de esas 11 usan de 1 a 4 palabras (Nut Sort llega a 5; Woodoku, a 6). Dominan el beneficio emocional («relax», «zen», «calm») y el reto («Can you sort it?»). Las cifras de niveles («1000+») son frecuentes.
- **El gancho visual:**
  - en water sort, **un recipiente o un momento especial grande en el centro**: la copa, el loto, el corazón, el pato;
  - en los mejores fuera del género, **el juego real realzado**: piezas que salen en 3D (Woodoku), un objeto ampliado (Goods Sort), el personaje en apuros (Royal Match).
- **Variedad al desplazar:** Get Color cambia el fondo y el color de la banda en cada captura. Woodoku hace lo contrario: una sola paleta coherente con el icono. Las dos funcionan.
- **Personajes:**
  - la mascota va en la banda en 3 de 13 (el gato de Magic Sort, la gota de IEC y el mago de Wizard); Nut Sort saca su personaje solo en la 7;
  - las ilustraciones de personaje van al final (capturas 4-8), no al principio.
- **Idiomas:**
  - 6 de 13 traducen el rótulo (Water Sort Puzzle de Guru, Water Match, Wizard, Goods Sort, Royal Match y Woodoku);
  - solo 3 traducen también la UI (Wizard, Woodoku y Goods Sort);
  - las que no traducen nada son las de estética más «genérica» del género.
- **Tráiler:** 8 de 13 tienen vídeo en la ficha web.
- **Lo que hoy prohíbe Play y siguen haciendo** (Google, guía de recursos gráficos): «FREE TO PLAY» y «50M+ PLAYERS» (Get Color), «Play Now» (Water Sort Puzzle de Guru), «INSTALL NUT SORT NOW» (Nut Sort), y una mano que toca la pantalla (Royal Match; Google pide evitar «dedos tocando el dispositivo»).

## 3. Lo que dicen las guías, contrastado

| Afirmación | Fuente | ¿Se cumple en la muestra? |
|---|---|---|
| En puzles casuales, la captura 1 enseña el bucle de juego; esconderlo baja la conversión | AppFollow (sin datos) [NV]; dev.to («real gameplay») | Sí: 12 de 13 |
| Las 3 primeras deciden; «~70 % del peso» | AppTweak; AppFollow, cita de un experto [NV] | Sin datos propios; coherente con que casi todas pongan lo mejor delante |
| Rótulos cortos y de beneficio, menos de 5 palabras, legibles a ~60 pt | AppTweak, AppFollow [NV] | Sí: 9 de las 11 con rótulo usan 1-4 palabras |
| El texto superpuesto no pasa del **20 % de la imagen** | Google, guía de recursos gráficos | Las bandas ocupan ~15-20 % |
| Prohibido «Best», «#1», «Top», «New», «Discount», «Sale», «Million Downloads», premios, rankings, precios y llamadas a la acción («Download/Install/Play/Try now») | Google, guía de recursos gráficos | Varios líderes lo incumplen (§2): no es un modelo a seguir |
| Mostrar la experiencia real del juego; nada de dedos tocando el dispositivo | Google | Sí, salvo la mano de Royal Match |
| Traducir los rótulos y la marca en cada idioma (la UI del juego no es obligatoria) | Google | Solo 6 de 13 |
| Vídeo con juego real en los primeros 10 s, ≥ 80 % de imágenes del juego, sin anuncios; solo se reproducen solos los primeros 30 s | Google | — |
| Gráfico de funciones: lo importante en el centro, nada clave en los bordes, colores vivos (evitar blanco, negro y gris oscuro), sin dispositivos ni marcas ajenas, que no repita el icono | Google | No visible en la web [NV] |
| Experimentos de ficha en Play: hasta 2 variantes de icono, gráfico, capturas y descripciones | dev.to, que cita la ayuda de Play Console [NV] | — |

**Un choque con el encargo.**
- Google pide el foco del gráfico de funciones **en el centro**. El encargo pide no poner nada importante en el centro, porque ahí va el botón de reproducir si hay vídeo.
- **[P] Recomendada aplicada:** el foco va en el centro ancho y el logotipo, un poco a la izquierda del eje, fuera del círculo central (~25 % del alto) donde va el botón de reproducir. Así se cumplen las dos cosas.

## 4. Conclusiones para Tintalú (accionables)

1. **La captura 1 es un tablero real en pleno vertido, mucho más grande que hoy.**
   - Hoy la pantalla de 16:9 se reduce dentro de un marco de madera, y el tablero ocupa ~40 % del alto.
   - Hay que recortar al tablero y al chorro y llevarlo al 70-80 % del alto, a sangre y sin marco de teléfono.
   - Es el cambio con más peso de todos (concepto A).
2. **Rótulos de 2-4 palabras, una idea por captura, de beneficio y en una sola banda que no pase del 20 %.**
   - Los textos actuales de §6 tienen de 3 a 7 palabras, así que conviene acortarlos.
   - Propuesta [P], la misma idea en los 3 idiomas:
     - «Ordena el agua del Amazonas» → «Vierte y tiñe el río»;
     - «Remonta el Amazonas hasta donde nace» → «Remonta el Amazonas».
   - La versión final, en el README de `graficos-v2/` de cada concepto.
3. **Un momento especial grande en el centro de la 1-2, sacado del juego real.**
   - Puede ser la cuia que vierte con el agua tiñéndose, o el Encuentro de las Aguas de dos colores.
   - Se amplía y se realza con luz (IA alrededor, no encima de la UI): es nuestro «recipiente especial».
   - No se inventa: el chorro y el tinte existen en el juego (D-065, punto 2).
4. **Una tanda de fondos por mundo**, en lugar de una sola paleta: el Estuario, el Encuentro de las Aguas, la selva, los Andes.
   - Se hacen con IA alrededor del tablero real, como Get Color. Tenemos nueve mundos reales, así que la variedad es verdad y no decorado.
   - La banda del rótulo va siempre en la madera y el petróleo de la marca, para que el conjunto se reconozca (lección de Woodoku).
5. **Sin marco de teléfono ni marco de madera alrededor de la pantalla.**
   - Ningún líder lo usa, y quita espacio al tablero.
   - El marco se queda en la placa del rótulo (Q-B13-5 (a)).
6. **Personajes después de enseñar el juego.**
   - TiTino, Capi, Rosa y los miquitos van desde la captura 2 (miquitos celebrando) y en la 5, nunca como ilustración en la 1.
   - Nunca como mascota fija en la banda: Capi fijo en el centro inferior es trade dress excluido (07 §1.3).
   - Para el concepto C (personajes en la 1-2), la muestra lo desaconseja: la única que abre con personaje es la que menos convierte [NV, una sola app]. **[P] Recomendada:** C solo como variante de experimento, no como ficha principal.
7. **Traducir todo, rótulo y UI. Ya lo hacemos y es una ventaja.** Solo 3 de 13 lo hacen entero.
   - El rótulo se adapta, no se calca, como Water Match («Aguas tranquilas» y «Águas calmas»).
   - Calidad nativa en es-419 y pt-BR, que es donde los líderes ni traducen.
8. **Ninguna cifra, «gratis», «nuevo» ni llamada a la acción, aunque los líderes lo hagan.**
   - Tampoco «1000+ niveles» como promesa de cantidad. No está prohibido, pero ninguna cifra nuestra está en firme para publicar.
   - Nada de manos ni dedos sobre la pantalla.
   - Nada de temporada (Halloween), que Google pide evitar o cambiar enseguida.
9. **La panorámica continua (concepto B) no la usa ningún líder en teléfono.**
   - Es diferenciadora, pero no está probada y parte el tablero entre capturas, justo lo contrario de la conclusión 1.
   - **[P] Recomendada:** B como variante de experimento; cada captura debe seguir siendo legible sola, con su tablero entero dentro de una sola captura.
10. **El vídeo vale la pena** (8 de 13 lo tienen; Google lo destaca en juegos).
    - Debe abrir con el vertido en los primeros 3 s, con al menos el 80 % de juego real.
    - El gráfico de funciones hace de portada: foco en el centro y logotipo fuera del círculo del botón (§3).
    - Es la fase 3, después de A.

## 5. Lo que no podemos copiar

- **Trade dress de los líderes** (07 §1.3 y `assets-tienda.md` §1):
  - gatos, caldero, oro biselado, gemas, cintas rojas, calavera y silueta de frasco con labio;
  - Capi fijo en el centro inferior con un objeto;
  - nada de la marca Amazon.
- **La firma visual de Magic Sort, Get Color y Nut Sort:** banda morada con letras doradas en mayúsculas y un montón de monedas de oro. Lo nuestro es madera, vidrio y petróleo (D-003, D-045).
- **Las llamadas a la acción y las cifras de jugadores o descargas** que siguen usando Get Color, Nut Sort y Water Sort Puzzle de Guru.
- **Personas en la ilustración**, como el mago de Wizard Water Sort (07 §8.1).
- **Recipientes o mecánicas que no tenemos:**
  - recipientes con formas de objeto (pato, gato, corazón), tubos gigantes, tuercas, cronómetros;
  - la idea se toma, pero lo ampliado es siempre una cuia o un momento que existe (D-065, punto 2).
- **Contenido de temporada.**
- **Imágenes de terceros** como referencia para la IA o en el repositorio, y prompts «al estilo de» cualquiera de estas apps (encargo §2).

## 6. Fuentes

- Las 13 fichas de Google Play enlazadas en §1, vistas el 2026-10-30 en EE. UU., Brasil, México y Colombia.
- Google, [Añadir recursos de vista previa para mostrar tu app](https://support.google.com/googleplay/android-developer/answer/9866151) (requisitos y recomendaciones de capturas, gráfico de funciones y vídeo).
- AppTweak, [How to optimize your app screenshots](https://www.apptweak.com/en/aso-blog/how-to-optimize-your-app-screenshots) [NV para sus cifras].
- AppFollow, [ASO screenshots best practices](https://appfollow.io/blog/aso-screenshots-best-practices) [NV: el «70 %» es una cita de un experto].
- A+ Games Studio, [App Store Optimization for Games: a 2026 guide](https://dev.to/aplusgamesstudio/app-store-optimization-for-games-a-2026-guide-140a) [NV].
