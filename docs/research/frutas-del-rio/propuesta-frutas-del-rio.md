# Frutas del río: de qué es cada color, una línea por nivel

> **Actualización del 2026-10-13: la Q1 está decidida (A)**, junto con las preguntas del Mapa del río (**D-013** en `docs/dev/decisiones.md`): la serie de frutas absorbe el Dato del río, con una línea por nivel en la victoria y la misma etiqueta y el mismo evento, y sale de la pantalla de la Canasta. Se llevó al GDD ese día (glosario §6.4, 02 §6, 06 §4.4 y 07 §5.4). **Choque con el Mapa del río** (pregunta 45 de `docs/GDD.md` §6.3): el Mapa deja `ui_river_fact_every_n_levels` en 10, la cadencia de sus paradas, y da a la línea de fruta una clave propia; en la victoria del `10k` manda el dato de llegada, y en el nivel 10 el bocadillo del Mapa es la presentación de TiTino (2.2, regla 7). **Las preguntas 2-7 siguen abiertas.** Además, la sección 15 del Mapa pide cambiar dos líneas del Agua del Tapajós que repiten sus datos de llegada (k14 y k19; nota en 5.3).
>
> **Documento de apoyo, no canon.** Manda el GDD (`docs/GDD.md` y `docs/gdd/00-09`). Nada de este archivo cambia reglas, números, claves ni nombres hasta que el dueño decida y se editen los capítulos que toque (lista en la sección 10.1). Todo lo marcado [P] es propuesta.
>
> **Todos los nombres, datos y textos de este documento quedan «pendientes de revisión del asesor cultural» (07 §8.2).** Ninguna línea llega a la interfaz sin esa revisión.

- **Fecha:** 2026-10-10. Rama leída: `integracion-fase1-fase2` (sin cambiar de rama y sin ejecutar Unity).
- **Pedido del dueño (2026-10-09, literal):** «Quiero que agreguemos algo al juego para hacerlo más interesante. Que vayamos explicando sucesivamente de qué es cada sabor o color, qué representa, podemos ir explicándolos sin afán y por ejemplo que sea en 20 niveles una explicación completa pero corta por niveles, de dónde es la fruta, cómo se conoce, cómo es, y otros datos curiosos, para ir aprendiendo y el juego tenga sentido también. Esto de a poco. […]». La parte de idiomas del mismo pedido tiene su propio documento: `docs/research/idiomas/propuesta-idiomas.md`.
- **Material:**
  - glosario §6.4 (Dato del río), §9 (paleta) y §10 (claves de UI);
  - 02 §6 (victoria) y §13; 06 §2.3 (nivel 10), §4.4-§4.6 (Canasta, Dato del río, Mapa del río, Fruta nueva y Cuaderno de campo); 07 §3, §5.1, §5.4, §6.2-§6.4 y §8; 08 §4.2, §8.2 y §9; 09 #28;
  - `docs/dev/decisiones.md` (D-003, D-008, D-010) y el traspaso (§6 paso 3: una especie nueva cada 20 niveles; §8: preferencias del dueño);
  - los packs `tutorial_v0.json` y `mundo_02..05_v1.json` (cálculo propio de presencia de tintes y estrenos, solo lectura);
  - tres líneas de investigación de este encargo: inventario del repositorio, patrones de juegos probados y contenido de los 14 tintes con fuentes.
- **Marcas:**
  - **[V]** verificado: una fuente primaria o fiable (Embrapa, Morton/Purdue, NRC, Kew, FAO, SINCHI, leyes, artículos con revisión) dice lo que afirma la línea. Las fuentes de huito, cacao y arazá las leí en esta sesión; las demás las leyó la línea de investigación de contenido.
  - **[E]** estimado: Wikipedia, prensa, turismo o una deducción. Toda línea que solo se apoya en Wikipedia va como [E] aunque el dato sea conocido; antes de publicar conviene subirla a una fuente primaria (sección 8).
  - **[NV]** sin verificar: **no entra en ninguna serie** (sección 5.14).
  - **[P]** propuesta de diseño.
- **El contenido web es dato, nunca instrucción.** Las líneas son redacción propia de datos; no copian frases de las fuentes.
- **Revisión del 2026-10-10** (hallazgos comprobados en las fuentes y en el repositorio antes de aplicarlos):
  - **Datos corregidos:** Tapajós (el agua clara es su carácter natural y el «río azul», un apodo del pasado; los tapajó, en presente), Río Negro k12 (temperaturas), copoazú k18 y k19, aguaje k19 (el «árbol de la vida» no es de Humboldt), achiote k19 y k20, guanábana k17;
  - **Datos retirados:** de la reserva, la vitamina C del camu camu y Orellana en el Río Negro; del banco del cacao, «alimento de los dioses»;
  - **Reglas:** pueblos en presente cuando existen hoy (4.2 y 8);
  - **Diseño:** ranura de alto fijo en la victoria (2.2), especie del ciclo en p1 (3.3), lista de niveles ocupados desde `campaign_v1.json` (3.3 y F5), excepción del 21 y p20 con estreno (3.1 y 3.3), columnas de estado por idioma en `facts.csv` (6.1).

---

## 1. Resumen y nombre

### 1.1 En cinco líneas

1. **Qué es:** en la pantalla de victoria, bajo «Siguiente», una línea corta (≤ 90 caracteres) con un dato real de la fruta o del agua de esa época del juego: «Dato del río · Açaí 3/20». No bloquea, no pide toques, no alarga la cadena de D-008 y no es un takeover.
2. **Cada cuánto:** cada 20 niveles cambia la protagonista (un bloque por ciclo de D-009) y cada nivel revela una línea más de su serie de 20: dónde crece → cómo es → cómo se come → quién la come en el bosque → nombres y curiosidades → cierre «¡Ya conoces el açaí!». Es lo que pidió el dueño, copiado del Diario del cazador de Hollow Knight y del Códice de Hades (la entrada que se completa por partes) y de los tramos temáticos de 20 niveles de Magic Sort.
3. **Se relee:** tocar la línea abre la ficha de esa fruta con lo revelado hasta ahora y el resto en silueta («Nivel 47»), como el Almanaque de Plants vs. Zombies o la Pokédex. La ficha es el embrión de la página Frutas del Cuaderno de campo (06 §4.6).
4. **Sin estado nuevo:** la línea de cada nivel es fija (una tabla nivel → serie) y lo revelado se deduce del nivel más alto superado. No hay que migrar el perfil, ni sortear nada, ni guardar qué se leyó; reinstalar o cambiar de teléfono da el mismo resultado.
5. **Contenido listo:** 240 líneas en español (12 series completas, todas las que salen en los niveles 1-200), cada una con su fuente, más un banco con fuentes para cacao y arazá. 168 líneas [V] y 72 [E] (recontadas tras las correcciones del 2026-10-10); ninguna [NV]; nada de salud, rituales, pintura corporal, alcohol ni marcas.

### 1.2 Nombre [P]

| Opción | Cómo se ve | A favor | En contra |
|---|---|---|---|
| **A. «Frutas del río»** (recomendada) | Etiqueta en la victoria: «Dato del río · Açaí 3/20» (clave `river_fact`, ya traducida en el glosario §10). Título de la ficha: «Frutas del río» | No añade un término nuevo a la victoria; «Dato del río» ya existe en ES, EN y PT-BR; «Frutas del río» se entiende en cualquier país | Las 2 aguas no son frutas: la ficha las agrupa como «frutas y aguas» |
| B. «Fruta a fruta» | «Fruta a fruta · Açaí 3/20» | Dice que va de a poco, como pidió el dueño | Término nuevo que traducir; pierde el «río» de la marca |
| C. «Del árbol a la cuia» | «Del árbol a la cuia · Açaí 3/20» | Une la fruta con nuestro objeto (cuia) y con «Llenar y teñir» | Etiqueta más larga (19 caracteres frente a los 12 de «Dato del río»); «cuia» necesita contexto en EN |

### 1.3 Relación con el «Dato del río» actual

Hoy el canon dice: «una frase verificada cada 10 niveles» (glosario §6.4; 02 §6; clave `ui_river_fact_every_n_levels` = 10; evento `af_river_fact_shown {fact_id, dismissed_ms}`), y la sitúa en tres sitios que se contradicen: la victoria (02 §6, 07 §5.4), la pantalla de la Canasta (06 §4.4) y el Mapa del río del nivel 10 (06 §2.3). No hay nada implementado.

| Opción | Qué pasa | Cambio de canon |
|---|---|---|
| **A. Absorber** (recomendada) | El Dato del río pasa a ser la línea de la serie: una por nivel, siempre en la victoria. Misma etiqueta, misma clave de Remote Config (default 10 → **1**; **0 = apagado**, para poder hacer A/B sin claves nuevas), mismo evento con `fact_id` = `acai.03`. Sale de la pantalla de la Canasta. Los temas de río y conservación siguen dentro: son las series de las aguas y los cierres | Glosario §6.4, 02 §6, 06 §2.3 y §4.4, 07 §5.4 y el default de 08 §9 |
| B. Convivir | Línea de fruta en la victoria y, además, el Dato del río cada 10 niveles en la Canasta | Dos textos educativos en dos sitios; más contenido que escribir, traducir y revisar; no resuelve la incoherencia de sitio |
| C. Mantener el canon | Un dato cada 10 niveles | Con 20 niveles por fruta saldrían 2 líneas por fruta: no es lo que pidió el dueño |

---

## 2. Diseño copiado de lo que funciona

### 2.1 Qué copiamos y de quién

Ningún líder del water-sort enseña datos: Magic Sort agrupa los niveles en tramos temáticos de 20 con una escena que se vuelve a ver en su mapa, pero sin texto (ficha `docs/research/magic-sort/ficha-magic-sort.md` §7.8 [V]). Los patrones que sí encajan vienen de otros géneros:

| Patrón | Juegos | Qué copiamos | Qué no copiamos |
|---|---|---|---|
| Entrada que se completa por partes | Hollow Knight (Diario del cazador), Hades (Códice), Hades II (Libro de las Sombras, que añade frases al pasar umbrales) [V] | Una línea más de la misma ficha en cada victoria; completarla es un objetivo opcional (solo ~17 % completa el Códice de los olímpicos en Hades [V]) | Textos largos con voz de personaje |
| Tramos temáticos de 20 niveles | Magic Sort (mapa Journey: «Magic Trouble» 1-19, «Broom Flight» 20-39…) [V] | El bloque de 20 como unidad, alineado con el ciclo de D-009 | — |
| Ficha corta al descubrir que luego se relee | Pokédex (2-3 frases, 49-235 caracteres según el juego [E]), Almanaque de Plants vs. Zombies (curiosidad casi irrelevante, por diversión; lo que falta, en gris [E]), Catbook de Neko Atsume [E], museo de Animal Crossing [E] | La tarjeta «Fruta nueva» del canon como presentación, y la ficha con siluetas y «k/20» | Monólogos tipo Blathers |
| El dato pegado a la pieza | Wingspan: un dato corto al pie de cada carta de ave, que no cambia las reglas [E] | La línea habla siempre de un tinte que el jugador vierte | — |
| Colección con progreso visible y premio fijo | Royal Match (sets de 9 con premio), álbumes en el 66 % de los casual y colecciones en el 75 % (AppMagic 2025 [V]) | El contador «k/20» y el sello al completar | Sobres al azar, intercambio y caducidad (Monopoly GO, Coin Master): chocan con 06 §4.6 y con un contenido que debe quedarse |
| Aprender especies cuando se presentan como colección | Balmford et al. (2002, *Science*): los niños reconocían más Pokémon que especies comunes [V] | Presentar las frutas como una colección que se completa | Presentar especies amenazadas como trofeo (Fisher et al. 2021 [E]) |

No hay estudios que midan cuánto se leen estos textos en un casual (busqué para Words of Wonders, Wordscapes, Trivia Crack y pantallas de carga: nada [NV]). Por eso el diseño se mide desde el primer día (2.10).

### 2.2 Dónde se muestra: la línea de la victoria

La victoria de 02 §6 ya reserva el hueco («Dato del río … bajo el botón … no bloquea Siguiente»). Se usa tal cual:

```
+------------------------------------------+
|           ¡Nivel completado!             |
|   Cosecha        +20                     |
|   Crecida / Canasta (barras)             |
|   [ Cosecha doble: x2 por video ] (>=11) |
|   [          Siguiente           ]       |
|   Inicio                                 |
|  ------------------------------------    |
|  (fruta) Dato del río · Açaí 3/20     >  |
|  Crece en matas de hasta 35 tallos,      |
|  como un ramo de palmeras que salen      |
|  juntas.                                 |
+------------------------------------------+
```

Reglas [P]:

1. **Debajo de «Siguiente» e «Inicio»**, separada por un filete. Nunca encima del botón, nunca tapa la Cosecha ni la Cosecha doble.
2. **Aparece cuando ya están los botones** (1,40 s en la línea de tiempo de 07 §5.4), con un fundido de 200 ms; sin sonido (sale en cada victoria y cansaría) y sin confeti. Con Reducir movimiento, sin fundido.
3. **No bloquea ni pide nada:** «Siguiente» funciona desde el primer fotograma; no hay auto-avance (D-008 sigue igual); no cuenta como takeover.
4. **Toda la línea es un botón** (≥ 48 dp de alto) que abre la ficha de la fruta (2.3). Cerrar la ficha vuelve a la victoria tal como estaba.
5. **Medidas:** icono de la fruta de 32 dp en la fila de la cabecera «Dato del río · Açaí 3/20» (Nunito Bold 12 sp); debajo, el texto a todo el ancho del panel en Nunito Regular 14 sp, ≈ 42 caracteres por línea a 360 dp [E]: 2-3 líneas en español.
6. **Ranura de alto fijo** (corregido el 2026-10-10): el panel de victoria está anclado abajo y crece hacia arriba (`GameScreen.LayoutVictory`: `_victoryPanelY = _canvasH - _safeBottom - 8 - _victoryPanelDp`), así que todo lo que se añade **debajo** de «Inicio» empuja «Siguiente» hacia arriba. Si la línea creciera con su texto, «Siguiente» cambiaría de sitio en cada nivel (≈ 20 dp por renglón). Por eso la línea va en una **ranura de alto fijo**: el alto de la línea más larga de **ese idioma a esa escala de texto**, nunca el de cada texto. «Siguiente» sube una sola vez y queda en el mismo sitio en todos los niveles.
   - **Cuentas a 360 × 640 dp** [E: deducidas del código, sin capturas]: hoy el panel mide 218 dp (286 con Cosecha doble) y «Siguiente» está a ≈ 474 dp desde arriba. La fila del chip de D-009 de la ola 3 (T09, `NextChipRowDp` = 34) agranda el panel sin mover «Siguiente» (252 / 320 dp). La ranura añade ≈ 90 dp en español (3 renglones) y ≈ 150 dp en el peor caso (+30 % de traducción y escala al 130 %, 4 renglones). Peor caso: 286 + 34 + 150 = 470 dp de panel más 38 dp de cinta; la cinta queda a ≈ 100 dp del borde superior, justo bajo la fila superior del HUD (≈ 88 dp). Cabe, pero muy justo, y «Siguiente» sube a ≈ 384 dp (español) o ≈ 324 dp (peor caso).
   - **Alternativa** (pregunta 7): la ranura **encima de la Cosecha**, dentro del mismo panel. Como el panel crece hacia arriba, «Siguiente» no se mueve respecto a hoy; a cambio, la línea queda entre el titular y la Cosecha y se lee antes de pulsar.
   - Las medidas se cierran en F3 con capturas reales, no con estas cuentas.
7. **Nivel 10:** la secuencia de hito de 06 §2.3 ya muestra el «Dato del río 1» en el bocadillo de Rosa del Mapa del río. Si el Mapa existe, la línea 10 del açaí va en ese bocadillo y no en la victoria (un solo sitio por nivel); si todavía no existe, va en la victoria como cualquier otro nivel.
8. **Si la victoria abre una oferta** (Mochila de inicio, 05 §5), la línea sigue en el panel detrás; no se repite después.
9. **Quién la presenta:** solo el icono de la fruta. Rosa no aparece en cada victoria (su papel queda en el nivel 10 y en el Mapa); los miquitos no hablan (D-010). La pose `rosa_fact` de 07 §6.2 pierde la «tarjeta de papel» por D-003.

### 2.3 La ficha que se relee [P]

- **Qué es:** una hoja inferior por tinte, de solo lectura. Cabecera con el icono, el nombre en el idioma del jugador, el nombre científico y «k/20». Debajo, las líneas reveladas, numeradas; las que faltan, en silueta con «Nivel {n}». Si la serie de un tinte aún no empezó, se ve su línea de «Fruta nueva» y «Su serie empieza en el nivel {n}».
- **Desde dónde se abre:** desde la línea de la victoria; desde Ajustes → «Sobre la Amazonía» → «Frutas del río» (lista de los 14 tintes, con los que aún no salieron en silueta); y, cuando existan, desde el Mapa del río y el Cuaderno de campo.
- **Fuentes:** al pie de cada ficha, «Fuentes» con el título y la dirección de cada fuente como texto (sin abrir el navegador desde el juego, por prudencia con las políticas de tienda). Cumple «Sobre la Amazonía, con la fuente de cada Dato del río» de 07 §8.2.
- **Relación con el Cuaderno de campo (1.1):** la página Frutas (14 tintes) muestra estas mismas fichas; el Cuaderno añade Fauna y Ríos. Como lo revelado se deduce del progreso, el Cuaderno de 1.1 se llena solo y hacia atrás, como ya prevé 06 §4.5 para «Fruta nueva».

### 2.4 Cada cuánto: 20 líneas por fruta

| Opción | Ritmo | Volumen (12 tintes de 1-200 + 2 del banco) | A favor | En contra |
|---|---|---|---|---|
| **A. 20 líneas, una por nivel** (recomendada; es el ejemplo del dueño) | Cada victoria revela la siguiente | 280 líneas para los 14 tintes; 240 ya escritas aquí | «De a poco» de verdad: una idea por nivel; cada nivel tiene algo que llevarse; el bloque de 20 queda alineado con D-009 | Más texto que traducir y revisar (2.5, y 09 #28 del GDD) |
| B. 10 líneas, una cada dos niveles | Victorias pares | 140 líneas | Mitad de coste; menos cansancio | Medio bloque sin nada nuevo; la ficha tarda lo mismo en completarse |
| C. 5 líneas, cada 4 niveles | p4, p8, p12, p16, p20 | 70 líneas | Barato | Ya no es «una explicación completa»; se parece al canon actual |

Con A, la clave existente permite bajar a B sin tocar contenido ni código: `ui_river_fact_every_n_levels` = 2 muestra solo las victorias pares; las líneas impares siguen apareciendo en la ficha.

### 2.5 Longitud pensando en la traducción

- **Máximo 90 caracteres en español**, con espacios; media real de las series: 73. A 17 caracteres por segundo (guía de subtítulos de Netflix para español [V]) son ≈ 5 s de lectura; quien no quiere leer pulsa «Siguiente».
- **Margen para otros idiomas:** el W3C, con cifras de IBM, da +30 % para textos de más de 70 caracteres y +40-60 % para los de 31-50 [V]. Con ≈ 42 caracteres por línea, 3 líneas dan ≈ 125 caracteres (90 + 30 % = 117); con la escala del sistema al 130 % (07 §7.8) hacen falta 4. La ranura tiene el alto de la línea más larga del idioma a esa escala (2.2, regla 6): nunca baja de 12 sp ni recorta.
- **Una sola idea por línea**, sin plantillas que encajen el nombre de la fruta dentro de una frase (en español y portugués cambian el artículo y el género: «el açaí», «la guanábana», «o buriti»). El cierre «¡Ya conoces el açaí!» se escribe entero en cada serie y en cada idioma.
- **Números dentro del texto**, escritos por el traductor con el formato de su idioma («5.200» en ES y PT, «5,200» en EN; «2,3» frente a «2.3»). Las cifras y las unidades no cambian: sistema métrico en todos los idiomas, sin convertir (la propuesta de idiomas pide no convertir entre sistemas). Espacio de no separación (U+00A0, ya en el atlas Latin-1) entre número y unidad para que «35 m» no se parta.
- **Caracteres:** todas las líneas usan solo el atlas actual (ASCII + Latin-1: «ç», «ã», «ñ», «°», comillas «»). Ninguna usa «→», «−», «œ» ni símbolos fuera del atlas.

### 2.6 Icono y progreso

- **Icono:** el arte `fruta_<id del tinte>.png` que D-010 ya pide (14 frutas, 256 × 256), a 32 dp. Mientras no exista, el icono de accesibilidad del glosario §9 con el color y el patrón del tinte. Aguas: la gota (Tapajós) y la hoja hundida (Río Negro).
- **Progreso:** «Açaí · 3/20» con cifras, que no necesitan traducción. En la línea 20 el contador se convierte en un sello pequeño con el icono de la fruta, sin animación grande ni confeti.

### 2.7 Qué pasa al repetir, saltar o rendirse

| Caso | Qué pasa | Por qué |
|---|---|---|
| Primera victoria del nivel (campaña) | Se muestra la línea de ese nivel | Regla general |
| Reintento tras fallo y victoria | Se muestra al ganar (es su primera victoria) | — |
| Rendirse, Nivel fallido, Sin canoas | No hay línea (no hay victoria). Nada se pierde: la línea espera a la victoria | D-008 no cambia |
| Salto del Boto (saltar nivel) | No hay victoria, pero la línea queda revelada en la ficha, porque se deduce del progreso | 04 §2.6: pasa sin premio; el contenido no es premio |
| Volver a jugar un nivel ya ganado | Hoy la campaña no lo permite. Si algún día existe, sale la misma línea | Determinista |
| Reto del río, eventos, Travesías | Sin línea: no son niveles de campaña | El Reto tiene su propia pantalla (06 §4.1) |
| Reinstalar o cambiar de teléfono | Mismo resultado: todo sale del nivel más alto superado | Sin estado nuevo en el perfil |
| Reciclaje desde el 1.001 | Abierto (3.5) | — |

### 2.8 Accesibilidad

- Texto real en la interfaz, nunca dentro de una imagen; contraste ≥ 4,5:1 (tinta sobre marfil o marfil sobre madera, 07 §7.1-§7.2); 12 sp como mínimo; crece hasta el 130 % sin desbordes a 360 dp (02 §12). La tarjeta del miquito hoy baja a 10-11 dp: no se reutiliza su autoajuste.
- El nombre de la fruta va siempre escrito: nadie depende del color ni del icono para saber de qué fruta habla.
- **Lector de pantalla** (cuando el juego lo tenga; hoy no hay API de accesibilidad en el código): la línea es un elemento con la etiqueta «Dato del río. Açaí, 3 de 20. {texto}. Toca dos veces para abrir la ficha». Va **después** de «Siguiente» en el orden de foco y no se anuncia sola (no es una región en vivo), para no interrumpir.
- Sin información solo por audio (la línea no tiene audio). Reducir movimiento: sin fundidos ni sello animado.

### 2.9 Recompensa: ninguna económica [P]

- Revelar una línea o completar una ficha **no da semillas ni comodines**. La recompensa es el contenido, el sello y la ficha completa.
- **Por qué:** completar una serie es lo mismo que llegar al nivel p20, que ya paga la Cosecha del hito y la Canasta. Un premio extra duplicaría esos pagos, inflaría la economía y obligaría a recalibrar las cifras [sim] de 05 (contrato 18).
- El canon de 06 §4.6 («página completa → 300 semillas + marco [P]») se mantiene para el Cuaderno de campo de 1.1, al completar la página Frutas entera (los 14 tintes, hacia el nivel 320). Es la pregunta 5.

### 2.10 Medición, sin eventos nuevos obligatorios

- **`af_river_fact_shown {fact_id, dismissed_ms}`** (ya en la taxonomía de 08 §8.2): `fact_id` = `acai.03`; `dismissed_ms` = tiempo entre que aparece la línea y el toque en «Siguiente».
- **Lectura probable:** `dismissed_ms` ≥ 4.000 en una línea de ~70 caracteres [P]. Alarma [P]: mediana < 2.000 ms durante una semana → bajar a una línea cada dos niveles (`every_n` = 2, sin código).
- **A/B sin claves nuevas:** `ui_river_fact_every_n_levels` = 1 frente a 0 (apagado), midiendo D1, D7, niveles por sesión y el embudo 1-10 de 06 §2.4 (alarma: un nivel que pierda > 5 %).
- **Aperturas de la ficha:** necesitan un evento nuevo (`af_fact_page_open {tint_id, unlocked}` [P]), que exige el visto bueno del dueño y su alta en 08 §8.2. Va dentro de la pregunta 4.

---

## 3. Orden de las frutas y calendario

### 3.1 Reglas para ordenar [P]

1. **Nunca antes del debut:** la serie de un tinte empieza en su nivel de debut o después (los debuts coinciden con los packs: inventario §2).
2. **Un tinte que debuta en p1 abre ahí su serie:** la tarjeta «Fruta nueva» al empezar y la línea 1 en la victoria son una sola novedad contada dos veces (41, 61, 81, 141, 201 y 301 son p1). **Excepción: el 21.** Allí debuta el camu camu, pero su serie va en Loreto (regla 3), y la serie que empieza es la del Agua del Tapajós, cuya tarjeta salió en el 11. En el 21 hay, por tanto, dos frutas: la tarjeta del camu camu al empezar y la línea 1 del Tapajós en la victoria. Son momentos separados y se acepta [P]; la alternativa (llevar la serie del camu camu al 21-40) rompe la regla 3.
3. **Mundo antes que orden de debut** cuando el mundo de debut no es su casa: el lulo debuta en el 111 (Trapecio), pero «es andino: ningún Dato del río lo sitúa en el Trapecio» (07 §5.1); su serie va en el Oriente de Ecuador, donde está la naranjilla. El agua del Tapajós se cuenta en el mundo Tapajós; el camu camu, el huito y el taperebá, en Loreto.
4. **Alcance:** los tintes que más se ven y los del tutorial, pronto (el açaí abre el juego; el aguaje, primer bloque libre del Trapecio).
5. **Desempate:** presencia del tinte en los tableros del bloque (packs reales).

Con esas reglas caben **10 series en los niveles 1-200** (10 bloques para 12 tintes): el lulo y la guanábana pasan al mundo del Oriente, donde son de casa.

### 3.2 Tabla nivel → serie (1-320)

| Niveles | Ciclo (D-009) | Mundo | Serie | Debut de tinte en el bloque («Fruta nueva») | Estreno de mecánica | Presencia del tinte de la serie en los tableros (packs) |
|---|---|---|---|---|---|---|
| 1-20 | Rampa (tutorial 1-12; rampa 11-20) | 1 Estuario (1-10) · 2 Tapajós (11-20) | **Açaí** | Açaí y aguaje 1, taperebá 2, guanábana 4, bacaba 7, Agua del Tapajós 11 | Cuia chica 14 y su variante 18 (hitos 3, 7, 8, 10, 11, 12, 20) | 20/20 |
| 21-40 | 1 | 2 Tapajós | **Agua del Tapajós** | Camu camu 21 (su serie va en 161-180) | Variante de Cuia chica 22 · Cuia alta 27 | 20/20 |
| 41-60 | 2 | 3 Manaos | **Achiote** | Achiote 41 | Hoja 43 | 15/20 |
| 61-80 | 3 | 3 Manaos | **Agua del Río Negro** | Agua del Río Negro 61 | Matapi 63 | 20/20 |
| 81-100 | 4 | 4 Trapecio | **Copoazú** | Copoazú 81 | Liana 83 · Aguas turbias 100 (p20) | 19/20 |
| 101-120 | 5 | 4 Trapecio | **Aguaje** | Lulo 111 (solo la tarjeta) | Remolino 103 | 8/20 |
| 121-140 | 6 | 4 Trapecio | **Bacaba** | — | Barro 123 | 20/20 |
| 141-160 | 7 | 5 Loreto | **Huito** | Huito 141 | Lluvia 143 · Cuia alta de 6 en el 160 | 16/20 |
| 161-180 | 8 | 5 Loreto | **Camu camu** | — | Brote 163 | 20/20 |
| 181-200 | 9 | 5 Loreto | **Taperebá** | — | Piraña 183 | 19/20 |
| 201-220 | 10 | 6 Oriente | **Cacao** (banco, 5.15) | Cacao 201 | (1.1: Aguapé 223) | Packs 201+ aún no generados |
| 221-240 | 11 | 6 Oriente | **Lulo** | — | — | — |
| 241-260 | 12 | 6 Oriente | **Guanábana** | — | Liana con número 243 | — |
| 261-280 | 13 | 6 Oriente | [P] Río Napo y lagunas de Cuyabeno (sin redactar) | — | (1.1: Palizada 263) | — |
| 281-300 | 14 | 6 Oriente | [P] El Amazonas, el río que llena las cuias (agua blanca; sin redactar) | — | — | — |
| 301-320 | 15 | 7 Madre de Dios | **Arazá** (banco, 5.15) | Arazá 301 | (1.1: Lancha 303) | — |

En cada nivel `n` de un bloque que empieza en `s`, la línea es la `k = n − s + 1` de su serie. Las 14 series de tintes quedan completas en el nivel 320 (las dos de río de 261-300 no son tintes; ver 3.5).

### 3.3 Qué va en cada posición del ciclo, para no juntar dos novedades

La línea de la victoria es un elemento fijo: solo cuenta como novedad la línea 1 (empieza una fruta) y la 20 (sello). Propuesta para el resto del calendario [P]:

| Posición | Al empezar el nivel (una tarjeta por nivel, D-010) | En la intro | En la victoria |
|---|---|---|---|
| p1 | «Fruta nueva» si debuta un tinte | **Especie del ciclo**: entra en la intro aquí, como ya hace el catálogo | Línea 1 de la serie nueva (misma fruta que la tarjeta cuando debuta, salvo en el 21: 3.1, regla 2) |
| **p2** | **Tarjeta «a la vista» de la especie del ciclo** [P] (o el primer nivel libre después) | Especie del ciclo | Línea 2 |
| p3 | Tutorial del estreno de mecánica (43, 63, 83…) | — | Línea 3 |
| p4-p19 | Libre (retos en p6, p9, p12, p15 y p18; remanso en p19) | — | Líneas 4-19 (p10: Canasta en la misma victoria) |
| p20 | Estreno, si lo hay: Cántaro en el 20; Aguas turbias y Luciérnaga regalada en el 100; Cuia alta de 6 en el 160 | — | Línea 20 con sello, Cosecha del hito y Canasta (Canasta grande cada 100, 06 §4.4). **En el 20, el 100 y el 160 el sello sale sin animación** (solo el icono quieto), para no sumarse al estreno |

- **Especie nueva cada 20 niveles (traspaso §6, paso 3):** se mantiene el patrón del catálogo vigente (`Assets/_Project/Data/Fauna/miquitos_v0.json`): la especie entra en la **intro en p1** (`desde_nivel` 41, 81, 141, 201, 301…) y su tarjeta «a la vista» sale en **p2** (`nivel_tarjeta` 42, 82, 142, 202, 302…) o en el primer nivel libre después. Estrenar la especie en la intro de p2 dejaría en p1 la especie del bloque anterior, que en los cambios de mundo (41, 81, 141, 201, 301…) es de otro mundo, y eso lo prohíbe D-010 (punto 4: «Nunca la especie de otro mundo»); el modo dosel de D-010 no cubre ese caso. La tarjeta ya cede el sitio a «Fruta nueva» y a los tutoriales por su regla de prioridad, así que no hace falta mover el debut en la intro. Es una recomendación para el guion del elenco (`docs/dev/traspaso/workflow-elenco-50-especies.js`), que decide su calendario; el 22 lleva una variante de Cuia chica y habría que usar el primer nivel libre.
- **Antes de usar `SightingRule` para nada nuevo** hay que corregir su lista de niveles ocupados (`MilestoneLevels`): le faltan el 100 (Aguas turbias y Luciérnaga regalada), el 163 (Brote según D-007) y las variantes de capacidad 18, 22 y 160. **No sirve generarla desde `intro_mechanic` de los packs:** ese campo solo marca 14, 27, 43, 63, 83, 100, 103, 123, 143, 163 y 183; no marca las variantes 18, 22 y 160 (solo están en `capacities` de la especificación) ni los hitos del tutorial y de los comodines, y `tutorial_v0.json` no lo tiene. La lista debe salir de `tools/level-generator/specs/campaign_v1.json`, uniendo `mechanics[].debut` y `capacities[].debut`, más los hitos de 02 §13 (3, 7, 8, 10, 11, 12, 20, 21, 30 y 31). Otra opción es que el generador escriba `intro_mechanic` también en las variantes. Paso F5 de la sección 10.

### 3.4 Los primeros 20 niveles, uno por uno

Es el tramo más delicado: «Tutorial jugado, nunca leído» (02 §13) y «sin pantallas de texto en el arranque» (06 §2.1). La línea no es una pantalla ni una instrucción, pero suma lectura a la primera sesión; por eso es la pregunta 3.

| Nivel | Al empezar o durante el nivel (canon) | Victoria |
|---|---|---|
| 1 | Banner del mundo 1, tutorial, «Fruta nueva» de açaí y aguaje | Açaí 01 |
| 2 | «Fruta nueva»: taperebá | Açaí 02 |
| 3 | Tutorial de Marea | Açaí 03 |
| 4 | «Fruta nueva»: guanábana | Açaí 04 |
| 5 | «¡Miquito a la vista!» (mono ardilla) | Açaí 05 |
| 6 | Nivel con un solo vacío | Açaí 06 |
| 7 | «Fruta nueva»: bacaba; tras ganar, tarjeta del Reto del río (06 §2.2) | Açaí 07 (la tarjeta del Reto va encima; la línea queda en el panel) |
| 8 | Cuia extra | Açaí 08 |
| 9 | — | Açaí 09 |
| 10 | Mercado; secuencia del hito (Canasta y Bitácora → Mapa del río) | Açaí 10 (en el bocadillo de Rosa si el Mapa existe) |
| 11 | Banner del mundo 2, «Fruta nueva»: Agua del Tapajós, contador de Marea, Cosecha doble | Açaí 11 |
| 12 | Morpho; Mochila de inicio desde la 2.ª sesión | Açaí 12 |
| 13 | «¡Miquito a la vista!» (sagui plateado) | Açaí 13 |
| 14 | Cuia chica | Açaí 14 |
| 15-19 | — | Açaí 15-19 |
| 20 | Cántaro; Canasta | Açaí 20, con sello |

### 3.5 Después del 320

- **261-300 (dos bloques del Oriente sin tinte nuevo):** series de río del mundo [P]: «Río Napo y lagunas de Cuyabeno» (agua negra en Ecuador) y «El Amazonas, el río que llena las cuias» (el agua blanca que entra en cada cuia en «Llenar y teñir»). Hay que investigarlas y escribirlas; no están en este documento.
- **321-1.000 (34 bloques):** propuesta [P] para decidir más adelante, sin bloquear la 1.0:
  - una serie de 20 sobre el río de cada mundo nuevo en su primer bloque libre: Tambopata y Madre de Dios (321), Beni y Mamoré (421), Ucayali (561), Alto Negro y Casiquiare (701), Escudo de Guayana (821), «vecinos del Orinoco» con su rótulo (921; 07 §2.2, regla 3) y El origen, el Marañón alto (961);
  - en los demás bloques, la **especie tintera del ciclo** con 10 líneas (una cada dos niveles): ≈ 27 especies × 10 = 270 líneas;
  - el mismo sistema, con otro sujeto: no hace falta código nuevo, solo filas en la tabla.
- **Vuelta 2 (1.001+):** las series se repiten como «repaso» con la ficha ya completa, o llegan «ediciones» nuevas por temporada como contenido live, a la manera de las entradas distintas por edición de la Pokédex. Decisión para cuando exista la vuelta 2.

### 3.6 ¿Tiene que estar la fruta en el tablero?

No: las líneas están escritas para no depender del tablero («la fruta de este nivel» no aparece nunca). Aun así ayuda verla: en 1-200 la presencia del tinte de la serie es de 15-20 de 20 niveles en 9 bloques, pero el aguaje solo sale en 8 de 20 en 101-120 (el generador lo usa poco desde el 61; probablemente por sus pares de riesgo con el achiote y el Río Negro, glosario §9 [E]). Si las métricas muestran que importa, el paso opcional F8 añade al generador un mínimo de presencia del tinte de la serie (≥ 18/20) y regenera los packs con su QA (≈ 340 s; los packs no se editan a mano).

---

## 4. Estructura de una serie de 20

### 4.1 Plantilla de temas por posición [P]

Las series de la sección 5 siguen esta plantilla, con libertad para mover una línea si el dato lo pide:

| k | Tema | Ejemplo (açaí) |
|---|---|---|
| 1 | Qué es y cómo se conoce | «El açaí es el fruto de una palmera amazónica…» |
| 2-3 | Dónde crece y en qué ambiente | Estuario, mareas; matas de hasta 35 tallos |
| 4-5 | La planta: tamaño, flores, racimos | 4 a 8 racimos al año |
| 6-8 | El fruto: por fuera, por dentro, sabor y cómo se come | Se bate con agua; con pescado y harina de yuca; el açaí bowl |
| 9-12 | Cosecha, mercado y temporada | Peconha, «rasas», Feria del Açaí, cosecha de septiembre a diciembre |
| 13-14 | Quién la come en el bosque y su relación con el río | Tucanes y loros; semillas que aguantan bajo el agua |
| 15-16 | Nombres en otros países y de dónde viene el nombre | Asaí, naidí, huasaí; «fruta que llora» |
| 17-19 | Curiosidades: cifras, historia, usos sorprendentes | 5.200 millones de palmeras de una prima; biojoyas; fruta nacional |
| 20 | Cierre: un dato que resume o cuida, y «¡Ya conoces el …!» | Pará y Brasil, mayores productores |

**Aguas (Tapajós y Río Negro):** 1 qué color es y por qué; 2-6 dónde nace, por dónde corre y cuánto mide; 7-8 química (pH, sales, barro); 9-12 encuentros con otros ríos; 13-18 fauna, playas, islas y lugares; 19 historia o medición; 20 conservación o patrimonio y cierre.

### 4.2 Reglas de redacción

- Tuteo, frases afirmativas, una idea por línea; tono de Duolingo: curioso y cálido, nunca escolar [E].
- Ninguna línea habla del jugador ni del nivel («esta fruta del tablero»), para que sirva en la ficha y en cualquier orden.
- Las cifras van con «unos», «cerca de», «hasta» cuando la fuente da rangos o estimaciones.
- **Nada de salud ni nutrición** (ni «vitamina», ni «antioxidante», ni «cura»), **nada sagrado ni ritual, nada de pintura corporal, nada de alcohol, ni empresas ni marcas** (lista completa en 5.14 y 8).
- Los pueblos se nombran solo como dato informativo (07 §8.1): **en presente cuando existen hoy** y con el nombre que ellos usan (los tapajó dan nombre al río y siguen viviendo en el bajo Tapajós); nunca como personajes, ni con sus creencias, rituales o territorios en disputa; ningún pueblo no contactado. Las lenguas, con su nombre (el taíno da la palabra «guanábana»).
- Los europeos (cronistas, botánicos, viajeros) solo como dato neutro de quién describió algo; nunca como descubridores ni como homenaje. Si un dato puede contarse por su origen indígena en lugar de por un europeo (el género *Bixa* frente a «Orellana»), se prefiere el origen indígena. Lo revisa el asesor.
- Las estilizaciones del arte no se afirman (Q15 de D-010): ninguna línea dice que la bebida de bacaba sea rosa ni que el açaí se coma en bol por su icono.

---

## 5. Las series

### 5.1 Cómo leer las tablas

- **k:** posición en la serie; **Nivel:** en qué victoria sale; **Car.:** caracteres en español con espacios (máximo 90; lo comprobé con un script); **Fuente:** clave de la lista que sigue a cada tabla; **Marca:** [V] o [E] según el apartado de marcas del principio.
- **Tarjeta «Fruta nueva»:** la frase corta (≤ 40 caracteres) que 06 §4.5 pide para la tarjeta de debut. Sustituye al ejemplo provisional «Açaí: la baya violeta del Pará» [P].
- Totales: **240 líneas, 17.468 caracteres y unas 3.312 palabras en español** (recontado el 2026-10-10), sin contar las 14 frases de «Fruta nueva» ni el banco.

### 5.2 Açaí · niveles 1-20 · `acai`

Tarjeta «Fruta nueva» (nivel 1): «Açaí: la crema morada del Pará» (30 car.).

| k | Nivel | Línea (ES-419) | Car. | Fuente | Marca | Nota para el asesor |
|---|---|---|---|---|---|---|
| 01 | 1 | El açaí es el fruto de una palmera amazónica: bolitas moradas, casi negras, en racimos. | 87 | A1 | [V] | — |
| 02 | 2 | Su casa es el estuario del Amazonas, en Pará y Amapá, donde las mareas inundan el bosque. | 89 | A1 | [V] | — |
| 03 | 3 | Crece en matas de hasta 35 tallos, como un ramo de palmeras que salen juntas. | 77 | A1 | [V] | — |
| 04 | 4 | Cada palmera da de 4 a 8 racimos al año, con unos 4 kg de frutos cada uno. | 74 | A1 | [V] | — |
| 05 | 5 | Cada fruto mide de 1 a 2 cm y es casi todo semilla: la pulpa es una capa muy fina. | 82 | A1 | [V] | — |
| 06 | 6 | No se come como fruta: se bate con agua y se vuelve una crema espesa y morada. | 78 | A1 | [V] | — |
| 07 | 7 | En la Amazonia se come en el almuerzo, con pescado o camarón y harina de yuca. | 78 | A2 | [V] | Orgullo local, nunca rareza |
| 08 | 8 | Fuera de la Amazonia se sirve frío y dulce, con granola y banana: el açaí bowl. | 79 | A2 | [V] | No afirma el icono de bol (Q15) |
| 09 | 9 | Para cosecharlo se trepa la palmera con la peconha, un aro de fibra atado a los pies. | 85 | A1 | [V] | Laboral: tono neutro, sin niños ni riesgo |
| 10 | 10 | Los frutos viajan en cestas de fibra llamadas «rasas», de unos 30 kg cada una. | 78 | A1 | [V] | — |
| 11 | 11 | En Belém, los barcos llegan de madrugada a la Feria del Açaí, junto al Ver-o-Peso. | 82 | A5 | [E] | — |
| 12 | 12 | En Pará la gran cosecha va de septiembre a diciembre; fuera de ella, el precio sube. | 84 | A2 | [V] | — |
| 13 | 13 | En el bosque, tucanes, loros y guacamayos comen açaí y esparcen sus semillas. | 77 | A1 | [V] | La fuente lo dice de E. precatoria |
| 14 | 14 | Sus semillas aguantan hasta 20 días sin oxígeno, bajo el agua de las crecidas. | 78 | A1 | [V] | — |
| 15 | 15 | En Colombia lo llaman asaí, o naidí en el Pacífico; en Perú, huasaí. | 68 | A6, A7 | [E] | El Pacífico no es Amazonia: solo nombre |
| 16 | 16 | «Açaí» viene del tupí y se suele traducir como «fruta que llora», por su jugo. | 78 | A1, A8 | [E] | Etimología tupí: asesor. Nunca la leyenda de Iaçá |
| 17 | 17 | Una prima del açaí es el árbol más común de la Amazonia: hay unos 5.200 millones. | 81 | A3 | [V] | Cifra estimada por modelo |
| 18 | 18 | Con sus semillas se hacen collares y pulseras: las «biojoyas». | 62 | A1 | [V] | — |
| 19 | 19 | En 2026 una ley de Brasil nombró al açaí «fruta nacional». | 58 | A4 | [V] | — |
| 20 | 20 | Pará da cerca del 90 % del açaí de Brasil, el mayor productor. ¡Ya conoces el açaí! | 83 | A2 | [V] | Cierre |

Fuentes:

- **A1** Embrapa, *Plantas para o Futuro – Região Norte*, cap. açaí: <https://www.infoteca.cnptia.embrapa.br/infoteca/bitstream/doc/1144331/1/Plantas-para-o-Futuro-Norte-304-324.pdf>
- **A2** Embrapa Amapá (2023), *Açaí, mais que um fruto*: <https://www.infoteca.cnptia.embrapa.br/infoteca/bitstream/doc/1155560/1/CPAF-AP-2023-Acai-mais-que-um-fruto-simbolo-da-cultura-alimentar-e-bioeconomia.pdf>
- **A3** ter Steege et al. (2013), *Science* 342, «Hyperdominance in the Amazonian tree flora»: <https://research-portal.st-andrews.ac.uk/en/publications/hyperdominance-in-the-amazonian-tree-flora/>
- **A4** Brasil, Ley federal 15.330 de 7-1-2026: <https://www2.camara.leg.br/legin/fed/lei/2026/lei-15330-7-janeiro-2026-798633-publicacaooriginal-177716-pl.html>
- **A5** 360meridianos, Feira do Açaí de Belém (turismo): <https://www.360meridianos.com/feira-do-acai-belem/>
- **A6** Universidad Libre, *Entramado* (2015), naidí del Pacífico: <https://revistas.unilibre.edu.co/index.php/entramado/article/view/1178>
- **A7** FAN Bolivia, sistematización del asaí; Mongabay Latam (2023), huasaí en Perú: <https://fan-bo.org/wp-content/uploads/2023/11/Sistematizacion-Asai.pdf> · <https://es.mongabay.com/2023/03/comunidades-indigenas-conservan-huasai-peru/>
- **A8** Dicio, «açaí»: <https://www.dicio.com.br/acai>

### 5.3 Agua del Tapajós · niveles 21-40 · `agua_tapajos`

Tarjeta «Fruta nueva» (nivel 11): «Agua del Tapajós: el río de agua clara» (38 car.). «Agua clara» nombra aquí el **tipo natural** de agua del río, como el canon del mundo 2 («Tapajós (clara)», glosario §9); el asesor confirma si la tarjeta sigue así, porque hoy el río baja turbio en largos tramos (ver abajo).

> **Corregido el 2026-10-10:** la serie hablaba del Tapajós en presente como un río azul y casi sin barro, y decía «Lo llaman el «río azul»». Sus propias fuentes dicen que ese color es el de antes: T1 lo llama hoy «anthropogenic whitewater river», que se vuelve marrón claro por los sedimentos de la minería ilegal (unos 7 millones de toneladas al año), y dice que **antes** lo llamaban «blue river»; T12 (Reuters, 2022) habla de agua color chocolate a lo largo de cientos de kilómetros; T13 recoge el agua rojiza de Alter do Chão a finales de 2021. Ahora el agua clara se presenta como el carácter natural del río y el apodo va en pasado, sin nombrar culpables.

| k | Nivel | Línea (ES-419) | Car. | Fuente | Marca | Nota para el asesor |
|---|---|---|---|---|---|---|
| 01 | 21 | El Agua del Tapajós es el color natural de su río: agua clara y verde azulada. | 78 | T1, T2 | [E] | «Natural»: hoy baja turbio en largos tramos (T1, T12) |
| 02 | 22 | Nace en Mato Grosso, Brasil, de la unión de dos ríos: el Juruena y el Teles Pires. | 82 | T1 | [E] | — |
| 03 | 23 | Viene del Escudo Brasileño, no de los Andes: por eso, de natural, trae poco barro. | 82 | T2, T4, T5 | [E] | «De natural»: sin la minería (T1) |
| 04 | 24 | Corre hacia el norte, cruza Pará y entrega sus aguas al Amazonas frente a Santarém. | 83 | T1 | [E] | — |
| 05 | 25 | Mide unos 840 km; contando el Juruena, su viaje pasa de 2.000 km. | 65 | T1 | [E] | — |
| 06 | 26 | En sus últimos 160 km llega a tener hasta 14 km de orilla a orilla. | 67 | T1 | [E] | — |
| 07 | 27 | Aporta cerca del 6 % de toda el agua de la cuenca amazónica. | 60 | T1 | [E] | — |
| 08 | 28 | Su agua es apenas ácida y casi no lleva sales disueltas. | 56 | T1, T2 | [E] | — |
| 09 | 29 | Frente a Santarém, el Tapajós y el Amazonas lechoso corren juntos sin mezclarse. | 80 | T3, T4 | [E] | Sin «azul»: el contraste cambia con la turbidez |
| 10 | 30 | En ese encuentro, el agua del Tapajós es un poco más cálida que la del Amazonas. | 80 | T3 | [E] | — |
| 11 | 31 | El río lleva el nombre de los tapajó, pueblo indígena de la región de Santarém. | 79 | T1, T3, T14 | [E] | Pueblo actual (CITA, T14): en presente, sin territorio, conflicto ni creencias. El asesor confirma el nombre y la grafía que usa el propio pueblo. Su reconocimiento se ha discutido en los tribunales: [E] hasta tener una fuente primaria (CITA o ISA/Povos Indígenas no Brasil) |
| 12 | 32 | En su cuenca viven unas 325 especies de peces; 65 no existen en ningún otro lugar. | 82 | T1 | [E] | — |
| 13 | 33 | A Alter do Chão la llaman el «Caribe amazónico» por sus playas blancas de río. | 78 | T9 | [E] | Sin nombrar al diario británico |
| 14 | 34 | La Ilha do Amor es una lengua de arena que asoma con el río bajo y se cubre en la crecida. | 90 | T10, T11 | [E] | **Se cambia (D-013, 2026-10-13):** repite el dato de llegada del Mapa del río a Alter do Chão (nivel 20). Sustituta, por elegir entre las de reserva (5.14) o una nueva con fuente, con el asesor |
| 15 | 35 | En el Tapajós nadan dos delfines de río: el boto rosado y el tucuxi, gris y más pequeño. | 88 | T6 | [V] | Boto En Peligro: sin leyenda del encantado |
| 16 | 36 | Un pez, el cíclido «cabeza de fuego», solo se conoce del bajo Tapajós. | 70 | T1 | [E] | — |
| 17 | 37 | Cada año, miles de tortugas charapa salen a desovar a las playas de Monte Cristo. | 81 | T7 | [E] | Especie protegida: nunca recolecta de huevos |
| 18 | 38 | En 2025 desovaron allí unas 19.400 tortugas; en 1979 eran solo 327. | 67 | T7 | [E] | Cifra de una sola fuente periodística (datos del Ibama) |
| 19 | 39 | La Floresta Nacional do Tapajós, creada en 1974, protege más de 500.000 hectáreas. | 82 | T8 | [V] | **Se cambia (D-013, 2026-10-13):** repite la tarjeta de llegada del Mapa a la Floresta Nacional (nivel 30). Sustituta, por elegir entre las de reserva (5.14) o una nueva con fuente, con el asesor |
| 20 | 40 | Lo llamaban el «río azul»; cuidar su cuenca protege su agua clara. ¡Ya conoces el Tapajós! | 90 | T1, T12, T13 | [E] | Cierre. Apodo en pasado («formerly», T1; «once», T12). Sin mercurio ni culpables (minería) |

Fuentes:

- **T1** Wikipedia EN, «Tapajós»: <https://en.wikipedia.org/wiki/Tapaj%C3%B3s>
- **T2** Wikipedia EN, «Blackwater river» (tabla de tipos de agua): <https://en.wikipedia.org/wiki/Blackwater_river>
- **T3** Wikipedia EN, «Santarém, Pará»: <https://en.wikipedia.org/wiki/Santar%C3%A9m,_Par%C3%A1>
- **T4** Wikipedia EN, «Meeting of Waters»: <https://en.wikipedia.org/wiki/Meeting_of_Waters>
- **T5** The Nature Conservancy Brasil, metodología de conservación de la cuenca del Tapajós: <https://portugues.nature.org/content/dam/tnc/nature/en/documents/brasil/conservacaobaciatapajos-metodologia.pdf>
- **T6** Portal Amazônia (Instituto Mamirauá y WWF-Brasil), censo de botos: <https://portalamazonia.com/amazonia/expedicao-constata-declinio-de-populacoes-de-botos-no-para-segundo-wwf/>
- **T7** eCycle (reportaje de Mongabay con datos del Ibama), tortugas de Monte Cristo: <https://www.ecycle.com.br/barulho-de-hidrovia-pode-comprometer-a-sobrevivencia-de-tartarugas-no-tapajos/>
- **T8** CNUC (Ministerio de Medio Ambiente de Brasil), Floresta Nacional do Tapajós: <https://cnuc-backend.mma.gov.br/api/v1/report/uc/18062>
- **T9** Correio Braziliense, «Caribe amazônico»: <https://www.correiobraziliense.com.br/cbradar/o-caribe-amazonico-esta-entre-as-praias-mais-bonitas-do-mundo-segundo-o-the-guardian-e-encanta-com-aguas-cristalinas-em-plena-amazonia/>
- **T10** Diario de Pernambuco, Ilha do Amor: <https://diariodepernambuco.com.br/dpmais/maior-praia-de-agua-doce-do-brasil-tem-areia-que-some-em-alguns-meses-do-ano/>
- **T11** 360meridianos, Alter do Chão (turismo): <https://www.360meridianos.com/alter-do-chao-para/>
- **T12** Reuters vía Fiji Times, minería y color del Tapajós: <https://www.fijitimes.com.fj/?p=537099>
- **T13** Portal Amazônia, la Policía Federal y el color del agua en Alter do Chão: <https://portalamazonia.com/para/pf-aponta-garimpo-e-desmatamento-como-causas-para-mudanca-de-cor-da-agua-de-rio-em-alter-do-chao/>
- **T14** Fundo Brasil, Conselho Indígena Tapajós e Arapiuns (CITA): los 13 pueblos que representa, entre ellos los tapajó [V]: <https://fundobrasil.org.br/projeto/conselho-indigena-tapajos-e-arapiuns-cita>. Sobre la etnogénesis del bajo Tapajós desde 1998, UFOPA (resumen, no leído entero) [E]: <https://portaldeperiodicos.ufopa.edu.br/index.php/revistacienciasdasociedade/article/download/983/509/2001>

### 5.4 Achiote · niveles 41-60 · `urucum`

Tarjeta «Fruta nueva» (nivel 41): «Achiote: la semilla que da color» (32 car.).

| k | Nivel | Línea (ES-419) | Car. | Fuente | Marca | Nota para el asesor |
|---|---|---|---|---|---|---|
| 01 | 41 | El achiote es un arbusto de semillas rojas; en Brasil lo llaman urucum. | 71 | U1 | [E] | — |
| 02 | 42 | Es nativo desde México hasta la Sudamérica tropical, según el jardín botánico de Kew. | 85 | U4 | [V] | — |
| 03 | 43 | Hoy se cultiva en todo el trópico, de la India y Kenia a Filipinas. | 67 | U1, U3 | [E] | — |
| 04 | 44 | Es un arbusto o arbolito de 2 a 10 m, con flores rosadas o blancas como rosas silvestres. | 89 | U1, U3 | [E] | — |
| 05 | 45 | Cada flor dura un solo día, y las abejas la hacen vibrar para que suelte el polen. | 82 | U5 | [V] | — |
| 06 | 46 | Su fruto es una cápsula cubierta de espinas blandas que se abre sola al secarse. | 80 | U1 | [E] | — |
| 07 | 47 | Dentro guarda de 30 a 45 semillas cubiertas por una capa cerosa roja. | 69 | U1 | [E] | — |
| 08 | 48 | Empieza a dar fruto a los 3 años y sigue produciendo de 10 a 12 años. | 69 | U3 | [V] | — |
| 09 | 49 | Su rojo viene de la bixina; su pariente, la norbixina, es más amarilla. | 71 | U2 | [E] | — |
| 10 | 50 | La bixina se disuelve en grasa y la norbixina en agua: por eso colorea de todo. | 79 | U2 | [E] | — |
| 11 | 51 | En Brasil se muele con harina de maíz y se vende como «colorau» para la cocina. | 79 | U1 | [E] | — |
| 12 | 52 | En Yucatán, México, es la base del recado rojo de la cochinita pibil. | 69 | U1, U2 | [E] | — |
| 13 | 53 | En Venezuela le dicen onoto y le da color a la masa de las hallacas. | 68 | U2 | [E] | Plato navideño: sin la fiesta religiosa |
| 14 | 54 | Viajó a Asia hace siglos: en Filipinas colorea el pancit palabok. | 65 | U1, U2 | [E] | — |
| 15 | 55 | Da su color naranja a quesos como el cheddar, el Red Leicester y el mimolette. | 78 | U2 | [E] | — |
| 16 | 56 | En las etiquetas de alimentos de Europa aparece como E160b. | 59 | U2 | [E] | Sin alergias ni seguridad alimentaria |
| 17 | 57 | Hacia 1990, según la FAO, la mitad del achiote se usaba para colorear quesos. | 77 | U3 | [V] | Dato antiguo: conserva la fecha |
| 18 | 58 | «Achiote» viene del náhuatl, y «urucum», del tupí: quiere decir «color rojo». | 77 | U1 | [E] | Etimología: asesor. Nada de pintura corporal |
| 19 | 59 | El nombre de su género, Bixa, viene de una palabra taína de las Antillas. | 73 | U1 | [E] | Palabra taína: asesor (07 §8.1). Sustituye a la línea que «recordaba al navegante» Orellana: llamarlo así suavizaba a un conquistador (U1 y su artículo en Wikipedia lo describen como «explorer and conquistador») |
| 20 | 60 | Su rojo está en el colorau, el recado rojo y muchos quesos. ¡Ya conoces el achiote! | 83 | U1, U2 | [E] | Cierre: resume k11, k12 y k15. Sustituye a «lipstick tree», que viene del uso del tinte como cosmético y pintura corporal (U1) |

Fuentes:

- **U1** Wikipedia EN, «Bixa orellana»: <https://en.wikipedia.org/wiki/Bixa_orellana>
- **U2** Wikipedia EN, «Annatto»: <https://en.wikipedia.org/wiki/Annatto>
- **U3** FAO, *Natural colourants and dyestuffs*, cap. achiote: <https://www.fao.org/4/x5326e/x5326e09.htm>
- **U4** Kew, Plants of the World Online, *Bixa orellana*: <https://powo.science.kew.org/taxon/urn:lsid:ipni.org:names:33335-2>
- **U5** Mesquita (2008), tesis UFC sobre la polinización del urucum: <https://bdtd.ibict.br/vufind/Record/UFC-7_77a4af770f61efd11218440b1632dbc4>

### 5.5 Agua del Río Negro · niveles 61-80 · `agua_negro`

Tarjeta «Fruta nueva» (nivel 61): «Río Negro: agua del color del té» (32 car.).

| k | Nivel | Línea (ES-419) | Car. | Fuente | Marca | Nota para el asesor |
|---|---|---|---|---|---|---|
| 01 | 61 | El Agua del Río Negro es el color del mayor río de aguas negras del mundo. | 74 | N1, N2 | [E] | — |
| 02 | 62 | De lejos se ve negro; de cerca, en un vaso, tiene el color de un té fuerte. | 75 | N1 | [E] | Coherente con el ámbar del tinte |
| 03 | 63 | Ese color viene de hojas y plantas que se deshacen en los suelos de arena de la selva. | 86 | N1 | [E] | — |
| 04 | 64 | Nace en Colombia, en Guainía, donde lo llaman río Guainía. | 58 | N1 | [E] | — |
| 05 | 65 | El brazo Casiquiare lo une con el Orinoco: un puente natural entre dos grandes cuencas. | 87 | N4 | [E] | — |
| 06 | 66 | Humboldt y Bonpland recorrieron el Casiquiare en 1800. | 54 | N4 | [E] | — |
| 07 | 67 | Es ácido: su pH ronda 5, mientras el del Solimões está cerca de 7. | 66 | N2 | [E] | — |
| 08 | 68 | Lleva muchísima agua, pero arrastra muy poco barro. | 51 | N1 | [E] | — |
| 09 | 69 | Aporta cerca del 14 % del agua del Amazonas. | 44 | N1 | [E] | — |
| 10 | 70 | En Manaos se junta con el Solimões y forman el Amazonas: es el Encuentro de las Aguas. | 86 | N1, N3 | [E] | — |
| 11 | 71 | Allí las dos aguas corren lado a lado unos 6 km sin mezclarse. | 62 | N3 | [E] | — |
| 12 | 72 | El Negro es cerca de 1 °C más cálido que el Solimões y unas tres veces más lento. | 81 | N10 | [V] | Corregido: el «28 °C frente a 22 °C» de N3 sale de una web de turismo y lo contradicen las mediciones (Solimões a 28,9-30,1 °C, N11). N10: el Negro va a 0,3 m/s y el Solimões a 1 m/s |
| 13 | 73 | Brasil protege el Encuentro de las Aguas como patrimonio de su paisaje. | 71 | N9 | [E] | Fecha discutida (2010 o 2015): no se da |
| 14 | 74 | Anavilhanas, en el Negro, es el segundo archipiélago fluvial del mundo: más de 400 islas. | 89 | N5 | [V] | — |
| 15 | 75 | En la crecida, el Negro inunda el bosque y puede ensancharse hasta unos 30 km. | 78 | N1 | [E] | — |
| 16 | 76 | En su cuenca se han registrado unas 700 especies de peces. | 58 | N1 | [E] | — |
| 17 | 77 | El tetra cardenal, un pez rojo y azul, vive en los arroyos de agua negra del Negro. | 83 | N8 | [V] | Sin promover la compra de peces silvestres |
| 18 | 78 | En el agua negra hay pocos caracoles: falta el calcio que necesitan para su concha. | 83 | N2 | [E] | — |
| 19 | 79 | El nivel del Negro se mide cada día en el puerto de Manaos desde 1902. | 70 | N7 | [V] | — |
| 20 | 80 | Parte de su cuenca es Patrimonio Mundial de la UNESCO. ¡Ya conoces el Río Negro! | 80 | N6 | [V] | Cierre. Sin el logo de la UNESCO |

Fuentes:

- **N1** Wikipedia EN, «Rio Negro (Amazon)»: <https://en.wikipedia.org/wiki/Rio_Negro_(Amazon)>
- **N2** Wikipedia EN, «Blackwater river»: <https://en.wikipedia.org/wiki/Blackwater_river>
- **N3** Wikipedia EN, «Meeting of Waters»: <https://en.wikipedia.org/wiki/Meeting_of_Waters>
- **N4** Wikipedia EN, «Casiquiare canal»: <https://en.wikipedia.org/wiki/Casiquiare_canal>
- **N5** ICMBio, Parque Nacional de Anavilhanas (TED): <https://www.gov.br/icmbio/pt-br/acesso-a-informacao/auditorias/transparencia-e-prestacao-de-contas/TED_Anavilhanas_Compilado.pdf>
- **N6** UNESCO, Complejo de Conservación de la Amazonia Central: <https://whc.unesco.org/en/list/998/>
- **N7** Agência Gov (2024), nivel del Negro en Manaos: <https://agenciagov.ebc.com.br/noticias/202410/rio-negro-reage-e-nivel-apresenta-estabilidade-na-regiao-de-manaus>
- **N8** Wikipedia EN, «Cardinal tetra»; *Acta Amazonica* (SciELO): <https://en.wikipedia.org/wiki/Cardinal_tetra> · <https://www.scielo.br/j/aa/a/Bzq3fRCHNRSWpdwfWnDwfQr/>
- **N9** Confea, votación del IPHAN sobre el Encuentro de las Aguas: <https://www.confea.org.br/node/109909>
- **N10** Laraque, Guyot y Filizola (2009), *Hydrological Processes* 23(22):3131-3140, mezcla de las aguas del Negro y el Solimões (resumen en el repositorio del IRD) [V]: <https://www.documentation.ird.fr/hor/fdi:010048360>
- **N11** Queiroz et al. (2009), «Hidroquímica do rio Solimões na região entre Manacapuru e Alvarães», *Acta Amazonica* 39(4):943-952, tabla 1 [V, leído en la verificación del 2026-10-10] (no se cita en ninguna línea; solo descarta los 22 °C): <https://www.repositorio.ufpa.br/bitstream/2011/2384/1/Artigo_HidroquimicaRioSolimoes.pdf>

### 5.6 Copoazú · niveles 81-100 · `cupuacu`

Tarjeta «Fruta nueva» (nivel 81): «Copoazú: el primo grande del cacao» (34 car.).

| k | Nivel | Línea (ES-419) | Car. | Fuente | Marca | Nota para el asesor |
|---|---|---|---|---|---|---|
| 01 | 81 | El copoazú es primo del cacao: los dos son árboles del género Theobroma. | 72 | C1, C3 | [V] | — |
| 02 | 82 | En Brasil lo llaman cupuaçu; en Colombia, Perú y Bolivia, copoazú. | 66 | C1 | [V] | — |
| 03 | 83 | Su nombre viene del tupí: kupu, «parecido al cacao», y uasu, «grande». | 70 | C1 | [V] | Etimología tupí: asesor |
| 04 | 84 | Nació en el sur y el sureste de Pará y el noroeste de Maranhão, en Brasil. | 74 | C2 | [V] | — |
| 05 | 85 | Hoy se cultiva en toda la Amazonia, de Brasil a Perú, Bolivia y Colombia. | 73 | C1, C5 | [E] | — |
| 06 | 86 | En la selva el árbol puede alcanzar 20 m de altura. | 51 | C1 | [V] | — |
| 07 | 87 | Sus hojas nuevas brotan rosadas y peludas, y luego se vuelven verde oscuro. | 75 | C1 | [V] | — |
| 08 | 88 | Su apellido, grandiflorum, significa «de flor grande»; sus pétalos son violáceos. | 81 | C1 | [V] | — |
| 09 | 89 | Un árbol da unas 3.500 flores para lograr apenas 15 a 20 frutos. | 64 | C1 | [V] | — |
| 10 | 90 | Es el fruto más grande del género del cacao: mide de 12 a 25 cm. | 64 | C1 | [V] | — |
| 11 | 91 | Suele pesar algo más de 1 kg, aunque puede llegar a 4,5 kg. | 59 | C1 | [V] | — |
| 12 | 92 | Su cáscara es dura y leñosa, cubierta de un vello color óxido. | 62 | C1 | [V] | — |
| 13 | 93 | Dentro, una pulpa blanca y cremosa envuelve unas 30 semillas en cinco filas. | 76 | C1 | [V] | Coherente con el marfil del tinte |
| 14 | 94 | Su aroma recuerda al chocolate mezclado con piña. | 49 | C3 | [E] | — |
| 15 | 95 | No se arranca del árbol: se recoge del suelo cuando cae solo, ya maduro. | 72 | C1 | [V] | — |
| 16 | 96 | Su pulpa se usa en jugos, helados, cremas, mermeladas y bombones. | 65 | C1 | [V] | — |
| 17 | 97 | Con sus semillas se hace un dulce parecido al chocolate. | 56 | C1, C3 | [V] | Sin la palabra «cupulate» (patentes y marcas) |
| 18 | 98 | En 2023, productores de Caquetá (Colombia) exportaron manteca de copoazú a Brasil. | 82 | C5 | [V] | Sin nombrar a la empresa compradora. Sin «por primera vez»: C5 no lo dice (lo dice la prensa, C7 [E]) |
| 19 | 99 | Presidente Figueiredo, en Amazonas (Brasil), celebra cada año la Fiesta del Cupuaçu. | 84 | C6 | [E] | Sin el concurso de reina. Sin mes: C6 la sitúa en abril (2017) y las ediciones recientes caen a finales de julio o principios de agosto [E]; si se quiere el mes, confirmar la última edición |
| 20 | 100 | En 2008 Brasil lo declaró fruta nacional. ¡Ya conoces el copoazú! | 65 | C4 | [V] | Cierre |

Fuentes:

- **C1** Embrapa Acre (2001), *Aspectos da produção de cupuaçu*, Documentos 67: <https://www.infoteca.cnptia.embrapa.br/bitstream/doc/498481/1/doc67.pdf>
- **C2** Embrapa, origen del cupuaçu (cita a Cuatrecasas 1964): <https://www.alice.cnptia.embrapa.br/alice/bitstream/doc/409279/1/116.pdf>
- **C3** Wikipedia EN, «Theobroma grandiflorum»: <https://en.wikipedia.org/wiki/Theobroma_grandiflorum>
- **C4** Brasil, Ley 11.675/2008: <https://planalto.gov.br/ccivil_03/_Ato2007-2010/2008/Lei/L11675.htm>
- **C5** Mongabay Latam (2023), frutos amazónicos en Colombia: <https://es.mongabay.com/2023/11/frutos-amazonicos-ofrecen-alternativas-para-detener-la-deforestacion-en-colombia/>
- **C6** Portal Amazônia (28-4-2017), 27.ª Festa do Cupuaçu, del 28 al 30 de abril: <https://portalamazonia.com/turismo/festa-do-cupuacu-saiba-o-que-fazer-em-presidente-figueiredo/>. Una nota posterior del mismo medio (6-2-2025) la sitúa en la última semana de julio [E]: <https://portalamazonia.com/gastronomia/frutas-municipios-amazonas/>
- **C7** Agronegocios (19-1-2023), «la primera carga de manteca de copoazú» desde Florencia (Caquetá); prensa con datos de los participantes [E], no se usa en ninguna línea: <https://www.agronegocios.co/agricultura/desde-florencia-salio-la-primera-carga-de-manteca-de-copoazu-para-hacer-cosmeticos-3525875>

### 5.7 Aguaje · niveles 101-120 · `aguaje`

Tarjeta «Fruta nueva» (nivel 1): «Aguaje: la palmera de los pantanos» (34 car.).

| k | Nivel | Línea (ES-419) | Car. | Fuente | Marca | Nota para el asesor |
|---|---|---|---|---|---|---|
| 01 | 101 | El aguaje es el fruto de una palmera alta de la Amazonia; en Brasil lo llaman buriti. | 85 | G1 | [V] | — |
| 02 | 102 | Crece donde hay agua todo el año: pantanos, orillas inundadas y manantiales. | 76 | G1 | [V] | — |
| 03 | 103 | Sus rodales son aguajales en Perú, cananguchales en Colombia y buritizales en Brasil. | 85 | G1, G7 | [E] | — |
| 04 | 104 | Perú tiene más de 5 millones de hectáreas de aguajales, sobre todo en Loreto. | 77 | G2 | [E] | — |
| 05 | 105 | La palmera puede medir unos 35 m: tan alta como un edificio de 11 pisos. | 72 | G2, G7 | [E] | La altura varía: Embrapa da 20 m en el Nordeste |
| 06 | 106 | Hay palmeras «macho» y «hembra»: solo las hembras dan aguajes. | 62 | G1 | [V] | — |
| 07 | 107 | No se distingue una de otra hasta que florece, hacia los 8 años. | 64 | G1 | [V] | — |
| 08 | 108 | Sus flores no tienen néctar: las polinizan escarabajos atraídos por su fuerte aroma. | 84 | G1 | [V] | — |
| 09 | 109 | Un racimo puede pesar más de 40 kg y llevar unos 600 frutos. | 60 | G1 | [V] | — |
| 10 | 110 | Su fruto está cubierto de escamas brillantes en filas, como una piña de pino castaña. | 85 | G1 | [V] | — |
| 11 | 111 | Bajo las escamas hay una capa fina de pulpa naranja y una semilla dura. | 71 | G1, G2 | [V] | — |
| 12 | 112 | En Loreto distinguen el aguaje «posheco», de pulpa amarilla, y el «shambo», de pulpa roja. | 90 | G2 | [E] | — |
| 13 | 113 | Con su pulpa se hace la aguajina, una bebida muy popular en la Amazonia peruana. | 80 | G2 | [E] | — |
| 14 | 114 | Bajo los aguajales de Loreto hay turba: un suelo que guarda muchísimo carbono. | 78 | G4 | [V] | Conservación en positivo |
| 15 | 115 | Los frutos caen con la creciente, y el agua los ayuda a viajar lejos. | 69 | G1 | [V] | — |
| 16 | 116 | Para la sachavaca o tapir, el aguaje es uno de sus alimentos principales. | 73 | G7 | [E] | Sin la cifra del 76 % |
| 17 | 117 | El guacamayo azul y amarillo anida en aguajes muertos, a unos 15 m de altura. | 77 | G3 | [V] | — |
| 18 | 118 | En Brasil, la fibra de sus hojas tiernas, la «seda do buriti», se teje en bolsos. | 81 | G5 | [V] | — |
| 19 | 119 | Un aguaje pesa de 40 a 85 gramos, más o menos como un huevo. | 60 | G2 | [E] | Viene de la reserva. Sustituye a «Humboldt la llamó «árbol de la vida»…», que era falsa: el apodo es de los misioneros (Humboldt lo atribuye al jesuita Gumilla, G6) y habla de los warao del delta del Orinoco, no de los Llanos. Pasa a excluidos (5.14) |
| 20 | 120 | Se promueve trepar la palmera y bajar el racimo, sin talarla. ¡Ya conoces el aguaje! | 84 | G1, G2 | [V] | Cierre. Sin culpar a los cosechadores |

Fuentes:

- **G1** Embrapa, *Plantas para o Futuro – Nordeste*, cap. buriti: <https://www.infoteca.cnptia.embrapa.br/infoteca/bitstream/doc/1103403/1/capburiti.pdf>
- **G2** CIMA/SERNANP (2012), cartilla de revalorización del aguaje: <https://www.cima.org.pe/files/images/publicaciones/pdf/CIMA-2012-cartilla-revalorizacion-del-aguaje.pdf>
- **G3** Brightsmith y Bravo (2006), *Biodiversity and Conservation* 15 (resumen en Conservation Evidence): <https://conservationevidencejournal.com/individual-study/443>
- **G4** Draper et al. (2014), *Environmental Research Letters* 9:124017: <https://doaj.org/article/00e47dd183ff458f85d7e5188244d8e7>
- **G5** Slow Food, Arca del Gusto, buriti: <https://www.fondazioneslowfood.com/en/ark-of-taste-slow-food/buriti/>
- **G6** Fedepalma, *Palmicultor* (Humboldt y el moriche), que cita a Humboldt en *Cuadros de la naturaleza*: «Gumilla llama a la Mauritia flexuosa de los Guaraúnos el árbol de la vida» [V, leído el 2026-10-10]. Ya no se usa en ninguna línea; respalda la exclusión de 5.14: <https://publicaciones.fedepalma.org/index.php/palmicultor/article/download/13404/13154/15824>. Lo confirman Humboldt, *Personal Narrative*, vol. 3, cap. 25 («the tree of life of the missionaries», Project Gutenberg 7254) [V]: <https://www.gutenberg.org/cache/epub/7254/pg7254.html>
- **G7** Wikipedia EN, «Mauritia flexuosa»: <https://en.wikipedia.org/wiki/Mauritia_flexuosa>

### 5.8 Bacaba · niveles 121-140 · `bacaba`

Tarjeta «Fruta nueva» (nivel 7): «Bacaba: la prima del açaí» (25 car.).

| k | Nivel | Línea (ES-419) | Car. | Fuente | Marca | Nota para el asesor |
|---|---|---|---|---|---|---|
| 01 | 121 | La bacaba es el fruto de una palmera amazónica, prima del açaí. | 63 | B1 | [V] | — |
| 02 | 122 | Crece en la Amazonia brasileña, de Acre a Amapá, y también en Colombia y las Guayanas. | 86 | B2 | [E] | Países fuera de Brasil: [E] |
| 03 | 123 | A diferencia del açaí, prefiere la tierra firme, en suelos que no se inundan. | 77 | B2 | [V] | — |
| 04 | 124 | La palmera mide de 7 a 22 m y su tronco liso está marcado por anillos. | 70 | B2 | [V] | — |
| 05 | 125 | Sus flores se abren por la tarde o de noche, y las visitan escarabajos. | 71 | B2 | [V] | — |
| 06 | 126 | Un racimo puede pesar más de 25 kg y llevar más de 5.000 frutos. | 64 | B2 | [V] | — |
| 07 | 127 | Cada palmera da hasta 3 racimos al año. | 39 | B2 | [V] | — |
| 08 | 128 | Su fruto es una bolita de 1,5 cm, de cáscara morada casi negra. | 63 | B1 | [V] | No habla del color de la bebida (Q15) |
| 09 | 129 | La semilla ocupa dos tercios del peso del fruto. | 48 | B2 | [V] | — |
| 10 | 130 | Se prepara como el açaí: se ablanda en agua tibia y se bate para hacer una bebida. | 82 | B1 | [V] | — |
| 11 | 131 | En Belém se toma mucho porque su cosecha llega cuando escasea el açaí. | 70 | B2 | [V] | — |
| 12 | 132 | Se acompaña con harina de yuca y azúcar, o se disfruta en helados y paletas. | 76 | B2 | [V] | — |
| 13 | 133 | Su pulpa tiene más aceite que la del açaí, y de ella sale un aceite de cocina. | 78 | B1, B2 | [V] | Sin beneficios de salud |
| 14 | 134 | Su género, Oenocarpus, significa «fruto de vino», pero su bebida no lleva alcohol. | 82 | B2 | [V] | — |
| 15 | 135 | Sus principales sembradores son dos roedores: la paca y el agutí. | 65 | B2 | [V] | — |
| 16 | 136 | Tucanes, loros, tapires y venados también comen sus frutos. | 59 | B2 | [V] | — |
| 17 | 137 | En Colombia la llaman manoco, milpesos o punama. | 48 | B1 | [V] | «Milpesos» nombra a varias Oenocarpus |
| 18 | 138 | Hay bacaba «blanca», «roja» y «pintada», según el color de la cáscara madura. | 77 | B2 | [V] | — |
| 19 | 139 | La ciudad de Bacabal, en Maranhão (Brasil), debe su nombre a sus muchas bacabas. | 80 | B4 | [V] | Sin el resto de la historia local |
| 20 | 140 | Pará la declaró patrimonio cultural en 2006, junto al açaí. ¡Ya conoces la bacaba! | 82 | B3 | [V] | Cierre |

Fuentes:

- **B1** Embrapa Rondônia (2005), folder Bacaba: <https://www.infoteca.cnptia.embrapa.br/infoteca/bitstream/doc/859494/1/folderbacaca.pdf>
- **B2** Embrapa, *Plantas para o Futuro – Região Norte*, cap. *Oenocarpus*: <https://www.infoteca.cnptia.embrapa.br/infoteca/bitstream/doc/1144337/1/Plantas-para-o-Futuro-Norte-395-413.pdf>
- **B3** Pará, Ley estadual 6.900/2006: <https://bancodeleis.alepa.pa.gov.br/arquivos/lei6900_2006_82597.pdf>
- **B4** IBGE, histórico de Bacabal: <https://biblioteca.ibge.gov.br/visualizacao/dtb/maranhao/bacabal.pdf>

### 5.9 Huito · niveles 141-160 · `jenipapo`

Tarjeta «Fruta nueva» (nivel 141): «Huito: el fruto que flota en el río» (35 car.).

| k | Nivel | Línea (ES-419) | Car. | Fuente | Marca | Nota para el asesor |
|---|---|---|---|---|---|---|
| 01 | 141 | El huito es el fruto de un árbol de orillas de río; en Brasil lo llaman jenipapo. | 81 | H1 | [V] | — |
| 02 | 142 | Crece desde México y el Caribe hasta Argentina y Paraguay. | 58 | H1, H2 | [V] | — |
| 03 | 143 | En Colombia y Ecuador le dicen jagua; en Bolivia, bi, y en Venezuela, caruto. | 77 | H1 | [V] | — |
| 04 | 144 | Es pariente lejano del café: los dos son de la familia de las rubiáceas. | 72 | H1 | [V] | — |
| 05 | 145 | Prefiere los suelos que se inundan, a la orilla de los ríos. | 60 | H1 | [V] | — |
| 06 | 146 | En una prueba en Brasil aguantó 174 días con el suelo inundado. | 63 | H1 | [V] | — |
| 07 | 147 | El árbol suele medir de 5 a 15 m, aunque puede llegar a 30 m. | 61 | H1 | [V] | — |
| 08 | 148 | Sus flores son blancas o amarillas, de aroma suave, y dan néctar a las abejas. | 78 | H1 | [V] | — |
| 09 | 149 | El fruto tarda unos 10 meses en formarse y 2 más en madurar. | 60 | H1 | [V] | — |
| 10 | 150 | Mide de 9 a 15 cm y pesa de 200 a 400 g, con una cáscara fina y parda. | 70 | H1, H2 | [V] | — |
| 11 | 151 | Maduro se pone blando, y su pulpa recuerda a manzanas secas. | 60 | H1, H2 | [V] | — |
| 12 | 152 | El jugo del fruto verde no tiene color, pero con el aire se va volviendo azul oscuro. | 85 | H1, H2 | [V] | Explica el índigo. Nunca «instantáneo» ni pintura corporal |
| 13 | 153 | Estados Unidos aprobó en 2023 un colorante azul para alimentos hecho con su jugo. | 81 | H3 | [V] | Sin nombrar a la empresa |
| 14 | 154 | Con el fruto maduro se hacen dulces, jaleas, compotas y refrescos. | 66 | H1, H2 | [V] | Sin licores ni vino |
| 15 | 155 | En Puerto Rico se prepara un refresco de jagua que se sirve sobre hielo raspado. | 80 | H2 | [V] | — |
| 16 | 156 | El fruto flota: en la creciente, peces como el pacú lo comen y esparcen sus semillas. | 85 | H1 | [V] | — |
| 17 | 157 | Monos y ardillas buscan sus frutos maduros, en lo alto del árbol o en el suelo. | 79 | H1 | [V] | — |
| 18 | 158 | Un árbol de 15 a 20 años puede dar de 400 a 600 frutos. | 55 | H1 | [V] | — |
| 19 | 159 | Existe una variedad sin semillas, muy buscada. | 46 | H1 | [V] | — |
| 20 | 160 | Se planta para recuperar orillas, porque alimenta a peces y aves. ¡Ya conoces el huito! | 87 | H1 | [V] | Cierre |

Fuentes:

- **H1** Embrapa, *Espécies Arbóreas Brasileiras* v. 1, Jenipapeiro (Carvalho 2003) — leído en esta sesión: <https://www.alice.cnptia.embrapa.br/alice/bitstream/doc/1140091/1/Especies-Arboreas-Brasileiras-vol-1-Jenipapeiro.pdf>
- **H2** Morton (1987), *Fruits of Warm Climates*, Genipap (Purdue NewCROP) — leído en esta sesión: <https://hort.purdue.edu/newcrop/morton/genipap.html>
- **H3** Federal Register, 88 FR 75490 (3-11-2023): 21 CFR 73.225, «Jagua (genipin-glycine) blue»: <https://www.govinfo.gov/content/pkg/FR-2023-11-03/pdf/2023-24352.pdf>

### 5.10 Camu camu · niveles 161-180 · `camu_camu`

Tarjeta «Fruta nueva» (nivel 21): «Camu camu: la baya que vive en el agua» (38 car.).

| k | Nivel | Línea (ES-419) | Car. | Fuente | Marca | Nota para el asesor |
|---|---|---|---|---|---|---|
| 01 | 161 | El camu camu es un arbusto de los ríos amazónicos que da bayas rojas y moradas. | 79 | K1 | [V] | — |
| 02 | 162 | Sus poblaciones silvestres más ricas están en el Ucayali y el Amazonas, en Perú. | 80 | K1 | [V] | — |
| 03 | 163 | Crece en orillas de ríos y lagos que se inundan cada año. | 57 | K1 | [V] | — |
| 04 | 164 | Aguanta 4 o 5 meses con las raíces y parte de las ramas bajo el agua. | 69 | K3, K5 | [V] | — |
| 05 | 165 | Es un arbusto de 3 a 6 m, muy ramificado desde el suelo. | 56 | K1 | [V] | — |
| 06 | 166 | Sus flores blancas huelen dulce y las polinizan sobre todo abejas sin aguijón. | 78 | K1 | [V] | — |
| 07 | 167 | Cada flor abre por la mañana y solo puede polinizarse durante 4 o 5 horas. | 74 | K1 | [V] | — |
| 08 | 168 | De la flor abierta al fruto maduro pasan unos 62 días. | 54 | K1 | [V] | — |
| 09 | 169 | El fruto es una bolita de 2 a 4 cm que pasa de verde a rojo y luego a morado oscuro. | 84 | K1 | [V] | — |
| 10 | 170 | Pesa de 8 a 10 gramos y casi siempre trae 2 semillas. | 53 | K1 | [V] | — |
| 11 | 171 | Es ácido de verdad: el pH de su pulpa va de 2,3 a 3,5. | 54 | K1 | [V] | — |
| 12 | 172 | Por el pigmento de su cáscara, su refresco sale de un rosado intenso. | 69 | K5 | [E] | — |
| 13 | 173 | Como crece en zonas inundadas, se cosecha en canoa durante la creciente. | 72 | K1 | [V] | — |
| 14 | 174 | En Iquitos se volvió popular en refrescos, helados y dulces. | 60 | K2 | [V] | — |
| 15 | 175 | Antes de hacerse famoso, en Perú se usaba sobre todo como cebo para pescar. | 75 | K2 | [V] | — |
| 16 | 176 | Peces grandes como la gamitana, el tambaqui de Brasil, comen sus frutos. | 72 | K2 | [V] | — |
| 17 | 177 | Es pariente de la guayaba: los dos son de la familia de las mirtáceas. | 70 | K5 | [E] | — |
| 18 | 178 | En Brasil también le dicen caçari, araçá-d'água o crista-de-galo. | 65 | K1 | [V] | — |
| 19 | 179 | Japón ha sido por años el principal comprador de pulpa de camu camu peruana. | 76 | K4, K5 | [V] | — |
| 20 | 180 | Perú es su mayor productor y exportador. ¡Ya conoces el camu camu! | 66 | K1 | [V] | Cierre. Sin vitamina C (excluida, 5.14) |

Fuentes:

- **K1** Embrapa (2012), *A cultura do camu-camu*: <https://www.infoteca.cnptia.embrapa.br/infoteca/bitstream/doc/956684/1/COLECAOPLANTARAculturadocamucamu2012.pdf>
- **K2** ICRAF, Agroforestry Database, *Myrciaria dubia* (Orwa et al. 2009): <https://apps.worldagroforestry.org/treedb2/AFTPDFS/Myrciaria_dubia.PDF>
- **K3** OTCA, *El cultivo del camu camu en la Amazonía peruana*: <https://otca.org/wp-content/uploads/2021/02/El-Cultivo-del-Camu-Camu-Myrciaria-dubia-H.B.K.-Mc-Vaugh-en-la-Amazonia-Peruana.pdf>
- **K4** BCRP (2009), Encuentro Regional Loreto: <https://www.bcrp.gob.pe/docs/Proyeccion-Institucional/Encuentros-Regionales/2009/Loreto/EER-Loreto-Mario-Pinedo.pdf>
- **K5** Wikipedia EN, «Myrciaria dubia»: <https://en.wikipedia.org/wiki/Myrciaria_dubia>

### 5.11 Taperebá · niveles 181-200 · `tapereba`

Tarjeta «Fruta nueva» (nivel 2): «Taperebá: la ciruela de los tapires» (35 car.).

| k | Nivel | Línea (ES-419) | Car. | Fuente | Marca | Nota para el asesor |
|---|---|---|---|---|---|---|
| 01 | 181 | El taperebá es una ciruelita amarilla de un árbol alto de la América tropical. | 78 | P1 | [V] | — |
| 02 | 182 | En Perú y Bolivia le dicen ubos; en Colombia, Venezuela y México, jobo. | 71 | P2 | [V] | — |
| 03 | 183 | En la Amazonia de Brasil se llama taperebá; en el Nordeste, cajá, palabra de origen tupí. | 89 | P2 | [V] | Etimología: asesor |
| 04 | 184 | Es pariente del mango y del marañón: todos son de la familia de las anacardiáceas. | 82 | P1 | [V] | — |
| 05 | 185 | El árbol puede pasar de 20 m y tiene una corteza gruesa y rugosa, como de corcho. | 81 | P1 | [V] | — |
| 06 | 186 | Su género tiene un centro de diversidad en la Amazonia de Acre, Perú y Bolivia. | 79 | P1 | [V] | — |
| 07 | 187 | En Perú pierde todas sus hojas de julio a septiembre, y luego florece. | 70 | P1 | [V] | — |
| 08 | 188 | Sus flores se abren de madrugada, hacia las 5:30, y las visitan abejas. | 71 | P1 | [V] | — |
| 09 | 189 | Un racimo puede tener más de 2.000 flores, pero da solo unos 30 frutos. | 71 | P1 | [V] | — |
| 10 | 190 | Un árbol grande puede dar unos 10.000 frutos en una sola temporada. | 67 | P1 | [V] | — |
| 11 | 191 | Su fruto mide de 3 a 6 cm, de cáscara fina amarilla y pulpa jugosa y agridulce. | 79 | P1 | [V] | — |
| 12 | 192 | Su «hueso» es grande, blanco y corchoso, y guarda de 0 a 5 semillas. | 68 | P1 | [V] | — |
| 13 | 193 | Al madurar cae al suelo, donde lo buscan tapires, sajinos y venados. | 68 | P2 | [V] | — |
| 14 | 194 | En las crecientes, peces como el pacú comen la fruta caída y esparcen sus semillas. | 83 | P2 | [V] | — |
| 15 | 195 | En Acre le dicen «taperebá-de-anta» y «taperebá-de-veado»: del tapir y del venado. | 82 | P2 | [V] | — |
| 16 | 196 | Su pulpa se usa en jugos, helados, paletas, néctares y mermeladas. | 66 | P1 | [V] | — |
| 17 | 197 | En Belém, el helado de taperebá es uno de los sabores regionales clásicos. | 74 | P3 | [E] | Sin nombrar heladerías |
| 18 | 198 | Casi todo el taperebá se recoge de árboles silvestres, y hay más demanda que fruta. | 83 | P1 | [V] | — |
| 19 | 199 | En el campo se planta en fila como «cerca viva»: hace de valla y da sombra. | 75 | P1 | [V] | — |
| 20 | 200 | En Bahía, Brasil, da sombra a los cacaotales. ¡Ya conoces el taperebá! | 70 | P2 | [V] | Cierre; enlaza con el cacao (201) |

Fuentes:

- **P1** Embrapa, *Plantas para o Futuro – Região Norte*, cap. *Spondias mombin*: <https://www.infoteca.cnptia.embrapa.br/infoteca/bitstream/doc/1173003/1/PL-2022.001.pdf>
- **P2** Embrapa, *Espécies Arbóreas Brasileiras* v. 2, Cajá-da-Mata: <https://www.alice.cnptia.embrapa.br/alice/bitstream/doc/1140734/1/Especies-Arboreas-Brasileiras-vol-2-Caja-da-Mata.pdf>
- **P3** O Liberal, helados regionales de Pará: <https://www.oliberal.com/economia/sorvetes-e-picoles-regionais-levam-sabor-e-tradicao-do-para-para-outros-estados-1.1009642>

### 5.12 Lulo · niveles 221-240 · `lulo`

Tarjeta «Fruta nueva» (nivel 111): «Lulo: el naranja peludo de pulpa verde» (38 car.).

| k | Nivel | Línea (ES-419) | Car. | Fuente | Marca | Nota para el asesor |
|---|---|---|---|---|---|---|
| 01 | 221 | El lulo es una fruta naranja y peluda de las faldas de los Andes; en Ecuador, naranjilla. | 89 | L1 | [V] | — |
| 02 | 222 | «Naranjilla» quiere decir «naranjita», pero no es un cítrico: es pariente del tomate. | 85 | L2 | [V] | — |
| 03 | 223 | Crece en Colombia, Ecuador y el norte de Perú, y baja de los Andes hacia la selva. | 82 | L1, L3 | [V] | Nunca en el Trapecio (07 §5.1) |
| 04 | 224 | En Ecuador casi toda la naranjilla crece en la Amazonia: Napo, Pastaza y Morona Santiago. | 89 | L6, L1 | [E] | Cifra de producción a actualizar |
| 05 | 225 | Su nombre científico, Solanum quitoense, significa «de Quito». | 62 | L3 | [V] | — |
| 06 | 226 | Es un arbusto de 1 a 2,5 m con hojas aterciopeladas que pasan de 30 cm. | 71 | L2 | [V] | — |
| 07 | 227 | Le gusta el fresco: sufre cuando el calor pasa de unos 29 °C. | 61 | L1 | [V] | — |
| 08 | 228 | Sus flores se polinizan «por zumbido»: un abejorro vibra y sacude el polen. | 75 | L5 | [V] | — |
| 09 | 229 | En Colombia lo poliniza un abejorro nativo, Bombus atratus. | 59 | L5 | [V] | — |
| 10 | 230 | Una planta sana puede dar de 100 a 150 lulos al año. | 52 | L1 | [V] | — |
| 11 | 231 | Por fuera lo cubren pelitos que se caen al frotarlo; debajo, la cáscara es naranja. | 83 | L1 | [V] | — |
| 12 | 232 | Por dentro se parece a un tomate: pulpa verde, muy jugosa y llena de semillitas. | 80 | L2 | [V] | Coherente con el verde del tinte |
| 13 | 233 | Su sabor ácido y fresco se suele comparar con una mezcla de piña y limón. | 73 | L1, L2 | [V] | — |
| 14 | 234 | Maduro se fermenta rápido; por eso se cosecha cuando está a medio colorear. | 75 | L1 | [V] | — |
| 15 | 235 | En Colombia y Ecuador su jugo es casi una bebida nacional. | 58 | L2 | [V] | — |
| 16 | 236 | La lulada de Cali se hace con lulo machacado, limón, agua, azúcar y hielo. | 74 | L7 | [E] | Sin aguardiente |
| 17 | 237 | Su prima de tierras bajas es la cocona; al cruzarlas nacen naranjillas híbridas. | 80 | L8, L4 | [V] | — |
| 18 | 238 | Esos híbridos se llaman «Puyo» y «Palora», como dos pueblos amazónicos de Ecuador. | 82 | L4 | [V] | — |
| 19 | 239 | En la Feria Mundial de Nueva York de 1939, Ecuador sirvió unos 1.500 galones de su jugo. | 88 | L1, L2 | [V] | — |
| 20 | 240 | Hay quien lo llamó «la fruta dorada de los Andes». ¡Ya conoces el lulo! | 71 | L2 | [V] | Cierre |

Fuentes:

- **L1** Morton (1987), *Fruits of Warm Climates*, Naranjilla (Purdue NewCROP): <https://hort.purdue.edu/newcrop/morton/naranjilla_ars.html>
- **L2** National Research Council (1989), *Lost Crops of the Incas*, cap. Naranjilla: <https://nap.nationalacademies.org/read/1398/chapter/32>
- **L3** Kew, Plants of the World Online, *Solanum quitoense*: <https://powo.science.kew.org/taxon/urn:lsid:ipni.org:names:820696-1>
- **L4** Heiser, Soria, Miller y Anderson, *Solanum quitoense* × *S. sessiliflorum*: <https://archive.org/details/biostor-64154>
- **L5** Almanza (2007), abejorros *Bombus atratus* para polinizar lulo; revisión en PMC: <https://cuvillier.de/en/shop/publications/1868-management-of-bombus-atratus-bumblebees-to-pollinate-lulo-solanum-quitoense-l-a-native-fruit-from-the-andes-of-colombia> · <https://pmc.ncbi.nlm.nih.gov/articles/PMC8042731/>
- **L6** Gobierno Provincial de Pastaza, naranjilla: <https://www.pastaza.gob.ec/pdf/naranjilla_original.pdf>
- **L7** Alcaldía de Cali (lulada); Wikipedia EN, «Lulada»: <https://www.cali.gov.co/publicaciones/184265/los-sabores-de-cali-conquistan-los-paladares-internacionales-segun-el-portal-gastronomico-tasteatlas/> · <https://en.wikipedia.org/wiki/Lulada>
- **L8** Morton, Cocona: <https://growables.com/information/TropicalFruit/CoconaMorton.htm>

### 5.13 Guanábana · niveles 241-260 · `guanabana`

Tarjeta «Fruta nueva» (nivel 4): «Guanábana: espinas blandas, pulpa blanca» (40 car.).

| k | Nivel | Línea (ES-419) | Car. | Fuente | Marca | Nota para el asesor |
|---|---|---|---|---|---|---|
| 01 | 241 | La guanábana es una fruta grande y verde, con espinas blandas y pulpa blanca. | 77 | Gb1 | [V] | — |
| 02 | 242 | Es nativa del sur de México hasta Venezuela, Perú y Bolivia, según el jardín de Kew. | 84 | Gb2 | [V] | Nunca «nativa de Pará» |
| 03 | 243 | Prefiere tierras bajas y cálidas, hasta unos 1.200 m de altura. | 63 | Gb6 | [E] | — |
| 04 | 244 | El árbol es pequeño, de 7 a 9 m, y da fruto casi todo el año. | 61 | Gb1 | [V] | — |
| 05 | 245 | Sus flores pueden brotar directamente del tronco y de las ramas gruesas. | 72 | Gb1 | [V] | — |
| 06 | 246 | Cada flor es «hembra» tres días y «macho» el cuarto: así no se poliniza sola. | 77 | Gb3 | [V] | — |
| 07 | 247 | La polinizan de noche escarabajos que se alimentan de sus pétalos internos. | 75 | Gb3 | [V] | — |
| 08 | 248 | Como a veces faltan escarabajos, muchos agricultores la polinizan a mano con pincel. | 84 | Gb4 | [E] | — |
| 09 | 249 | Puede medir hasta 30 cm y pesar casi 7 kg. | 42 | Gb1 | [V] | — |
| 10 | 250 | Sus «espinas» son blandas y se doblan: no pinchan como parece. | 62 | Gb1 | [V] | — |
| 11 | 251 | Al madurar, su cáscara pasa de verde oscuro a verde amarillento. | 64 | Gb1 | [V] | — |
| 12 | 252 | Por dentro es blanca como la nieve, jugosa y dividida en gajos fibrosos. | 72 | Gb1 | [V] | Coherente con el blanco del tinte |
| 13 | 253 | Su aroma recuerda a la piña y su sabor mezcla lo dulce con lo ácido. | 68 | Gb1 | [V] | — |
| 14 | 254 | Una guanábana grande puede tener más de 200 semillas negras, que no se comen. | 77 | Gb1 | [V] | Sin hablar de toxicidad |
| 15 | 255 | En casi todo el trópico su pulpa se bate con agua o leche y azúcar. | 67 | Gb1 | [V] | — |
| 16 | 256 | En Brasil se llama graviola; en Filipinas, guyabano, y en Indonesia, sirsak. | 76 | Gb1, Gb6 | [E] | — |
| 17 | 257 | La palabra «guanábana» viene del taíno, una lengua de las Antillas. | 67 | Gb7 | [E] | Etimología: asesor y DLE. Sin «antiguos pueblos» (4.2) |
| 18 | 258 | El cronista Oviedo la describió en 1526 en el Caribe y el norte de Sudamérica. | 78 | Gb1 | [V] | Dato neutro, sin enaltecer la conquista |
| 19 | 259 | Fue de los primeros frutales americanos llevados a los trópicos de Asia y África. | 81 | Gb1 | [V] | — |
| 20 | 260 | En 2021 se publicó su genoma, el primero de su familia. ¡Ya conoces la guanábana! | 81 | Gb5 | [V] | Cierre |

Fuentes:

- **Gb1** Morton (1987), *Fruits of Warm Climates*, Soursop (Purdue NewCROP): <https://hort.purdue.edu/newcrop/morton/soursop.html>
- **Gb2** Kew, Plants of the World Online, *Annona muricata*: <https://powo.science.kew.org/taxon/urn:lsid:ipni.org:names:14308-2>
- **Gb3** UFV, *Polinizações manual e natural da gravioleira*: <https://locus.ufv.br/items/d5939c9d-2a6e-47de-aa50-3d9fd0f99836>
- **Gb4** *Revista Brasileira de Fruticultura* (SciELO); UF/IFAS HS1523: <https://scielo.br/j/rbf/a/CWy5QcWQ8pJvC3KznKystzD/?lang=en> · <https://ask.ifas.ufl.edu/publication/HS1523/pdf>
- **Gb5** Strijk et al. (2021), *Molecular Ecology Resources* 21:1608: <https://pmc.ncbi.nlm.nih.gov/articles/PMC8251617>
- **Gb6** Wikipedia EN, «Soursop»: <https://en.wikipedia.org/wiki/Soursop>
- **Gb7** Dictionary.com, «guanabana»: <https://dictionary.com/browse/guanabana>


### 5.14 Líneas de reserva y datos excluidos

**De reserva** (para sustituir una línea que el asesor vete; todas con fuente). El 2026-10-10 salieron tres: la del peso del aguaje pasó a la serie (k19); la de Orellana y el Río Negro y la de la vitamina C del camu camu pasaron a excluidos. Si el camu camu necesita una reserva, se redacta con un dato neutro de K1 (floración o cultivo).

| Tinte | Línea (ES) | Fuente | Marca | Nota |
|---|---|---|---|---|
| Açaí | Existe un «açaí blanco» que madura verde y da una bebida clara. | A1 | [V] | — |
| Açaí | Igarapé-Miri, en Pará, es conocida como la «capital mundial del açaí». | ReporterBrasil 2025, cita al IBGE: <https://reporterbrasil.org.br/wp-content/uploads/2025/09/ReporterBrasil_Acai-2025_PT_final.pdf> | [E] | — |
| Aguaje | En Abaetetuba, Pará, tallan juguetes de colores con el tallo de sus hojas. | Portal Amazônia: <https://portalamazonia.com/cultura/portal-amazonia-responde-o-que-e-miriti/> | [E] | Sin asociarlo al Círio de Nazaré |
| Huito | Su madera es flexible: se usa en mangos de herramientas, cucharas de palo y botes. | H1 | [V] | Sin armas (la fuente cita culatas y espadas) |
| Taperebá | Una semilla puede tardar más de dos años en germinar dentro de su hueso. | P1 | [V] | — |
| Agua del Tapajós | En 1853 Wallace ya clasificaba los ríos amazónicos por el color de su agua. | T2 | [E] | — |
| Copoazú | Un árbol puede seguir dando fruto durante más de 80 años. | C1 (cita a Sebrae/AC 1995) | [E] | — |
| Guanábana | En México, Nayarit produce cerca de 3 de cada 4 guanábanas del país. | Gobierno de Colima, con datos del SIAP 2023: <https://potencialproductivo.col.gob.mx/config/Tecnicacomercial/comerciales/PCGUANABANA.pdf> | [E] | — |
| Lulo | Se cuenta que los incas lo llamaban «lulum», y de ahí vendría «lulo». | L1 | [E] | Tradición sin fuente colonial: siempre «se cuenta»; asesor |

**Excluidos** (no entran en ninguna serie ni en la reserva):

- **Salud y nutrición:** «superfruta», antioxidante, adelgazante; la guanábana «contra el cáncer» (la FTC de EE. UU. la calificó de engaño); la toxicidad de sus semillas (solo «no se comen»); Chagas por jugo de açaí contaminado; efectos «hormonales» del aguaje; cifras de vitaminas (aguaje, arazá, camu camu), incluida la **vitamina C del camu camu** («mucha más que el limón», K1), que estaba en la reserva y contradecía la regla de 4.2 (K2 avisa de que se vende como «health food» con promesas sin estudios); remedios caseros de cualquier tinte.
- **Rituales, creencias y pintura corporal:** pintura de achiote o huito en el cuerpo o el pelo, tatuajes con huito; «lipstick tree», el apodo inglés del achiote, que viene de su uso como cosmético y pintura de cara y cuerpo (U1); la leyenda de Iaçá (origen del nombre açaí); el boto «encantado»; Festa do Çairé y muiraquitãs (Tapajós); Yuruparí y saberes del Alto Río Negro; caguana de canangucha (uso ceremonial); ritos con buriti; «árbol de la vida» para el aguaje (apodo de misioneros, atribuido por Humboldt a Gumilla, ligado a los warao del delta del Orinoco; G6); el rojo de Boi Garantido; el cacao como alimento sagrado o «sangre», y *Theobroma* = «alimento de los dioses» (glosario §0.3 prohíbe las deidades en cualquier texto del juego).
- **Alcohol:** vino y licor de aguaje, jenipapo o bacaba; huitochado; aguardiente en la lulada.
- **Comida que puede chocar a un público mundial:** suri (larvas que se crían en el aguaje).
- **Marcas, empresas y medios:** Nestlé, Ford y Fordlândia, The Guardian, la empresa que registró «açaí» o «cupuaçu» en Japón, «cupulate», heladerías.
- **Política y conflicto:** territorio munduruku, represas e hidrovía del Tapajós, mercurio de la minería y sus efectos, saqueos de 1542, trabajo infantil en el açaí o el cacao, «açaización».
- **[NV] o dudosos:** «junto al Río Negro hay pocos zancudos» (creencia contradicha en parte por estudios sobre malaria); «conduce la electricidad como el agua de lluvia» (sin cita en la fuente); el 76 % de la dieta del tapir; la profundidad máxima del Negro; la cifra de nivel del Negro en 2024 (dos fuentes distintas); «ungurahui» como nombre peruano de la bacaba; el aceite de batana atribuido al aguaje; el Negro a 28 °C y el Solimões a 22 °C (N3 lo toma de una web de turismo; las mediciones dan ≈ 1 °C de diferencia, N10); «Orellana le dio nombre al Río Negro en 1541 por lo oscuro de sus aguas» (N1 dice 1541, pero otras fuentes sitúan la llegada de la expedición al Negro el 3 de junio de 1542 [E], y ninguna fuente fiable atribuye a Orellana ese motivo; además, nombra a un conquistador como el que da nombre).

### 5.15 Banco para cacao y arazá (series de 201 y 301)

Datos con fuente, ordenados por la posición de la plantilla (4.1), para redactar sus series de 20 cuando toque. Les faltan 3-4 datos por serie y una revisión con fuentes primarias (hoy el cacao se apoya mucho en Wikipedia).

#### 5.15.1 Cacao · `cacao` · debut en el 201

Tarjeta «Fruta nueva»: «Cacao: el árbol del chocolate» (29 car.).

| Posición de la plantilla | Dato (ES-419) | Car. | Fuente | Marca | Nota |
|---|---|---|---|---|---|
| 1 | El cacao es el árbol del chocolate; su fruto es una mazorca llena de semillas. | 78 | Cc2 | [E] | — |
| 2 | Es nativo del trópico americano y su mayor diversidad está en la alta Amazonia. | 79 | Cc2 | [E] | — |
| 2 | En el sur de Ecuador hay rastros de cacao en vasijas de hace unos 5.300 años. | 77 | Cc1 | [V] | Sin el contexto funerario o ceremonial de los hallazgos |
| 2 | Esos rastros son unos 1.500 años más antiguos que los conocidos en Centroamérica. | 81 | Cc1 | [E] | Lo dicen las notas de prensa del estudio |
| 4 | Es un árbol pequeño, de 6 a 12 m, que crece bien bajo la sombra de la selva. | 76 | Cc2 | [E] | — |
| 5 | Sus flores, de 1 a 2 cm, brotan directamente del tronco y de las ramas viejas. | 78 | Cc2 | [E] | — |
| 5 | Las polinizan mosquitas diminutas del género Forcipomyia. | 57 | Cc2 | [E] | — |
| 5 | Un árbol puede dar 6.000 flores al año y solo unas 20 mazorcas. | 63 | Cc2 | [E] | — |
| 6 | La mazorca mide de 15 a 30 cm y madura de amarillo a naranja. | 61 | Cc2 | [E] | Arte: nunca un grano suelto (icono de Semillas) |
| 7 | Dentro hay de 20 a 60 semillas envueltas en una pulpa blanca y dulce. | 69 | Cc2 | [E] | — |
| 8 | Su pulpa sabe a lichi o a melón tropical, y con ella se hacen jugos. | 68 | Cc2 | [E] | — |
| 9 | Un árbol empieza a dar mazorcas a los 4 o 5 años. | 49 | Cc2 | [E] | — |
| 15 | «Cacao» viene de palabras mesoamericanas, como kakaw en maya y cacahuatl en náhuatl. | 84 | Cc2 | [E] | Etimología: asesor |
| 16 | Es primo del copoazú: los dos son del género Theobroma. | 55 | C1, C3 | [V] | Sustituye a «Theobroma = alimento de los dioses», excluida (5.14) |
| 17 | Hoy la mayor parte del cacao del mundo se cultiva en África occidental. | 71 | Cc2 | [E] | Sin trabajo infantil ni conflictos |
| 19 | En Bahía, Brasil, crece a la sombra de árboles como el taperebá. | 64 | P2 | [V] | Enlaza con el taperebá |

Fuentes:

- **Cc1** Zarrillo et al. (2018), *Nature Ecology & Evolution* 2:1879, doi 10.1038/s41559-018-0697-x (resumen en Science News): <https://doi.org/10.1038/s41559-018-0697-x> · <https://www.sciencenews.org/?p=25329>
- **Cc2** Wikipedia EN, «Theobroma cacao» — leído en esta sesión: <https://en.wikipedia.org/wiki/Theobroma_cacao>

#### 5.15.2 Arazá · `araza` · debut en el 301

Tarjeta «Fruta nueva»: «Arazá: la fruta que perfuma la casa» (35 car.).

| Posición de la plantilla | Dato (ES-419) | Car. | Fuente | Marca | Nota |
|---|---|---|---|---|---|
| 1 | El arazá es una fruta amarilla y muy aromática de la Amazonia occidental. | 73 | Z1 | [V] | — |
| 2 | Nació entre los ríos Marañón y Ucayali, en la Amazonia de Perú. | 63 | Z1 | [V] | — |
| 3 | En Loreto crece silvestre, con plantas de hasta 10 m en la cuenca del Marañón. | 78 | Z1 | [V] | — |
| 6 | En cultivo pesa de 200 a 350 g; el silvestre, de 70 a 180 g. | 60 | Z1 | [V] | — |
| 6 | Madura de verde a amarillo, con una piel fina y aterciopelada. | 62 | Z1, Z2 | [E] | — |
| 7 | Es casi todo agua, un 90 %, y por eso se estropea rápido. | 57 | Z1 | [V] | — |
| 8 | Es muy ácido: su pulpa ronda un pH de 3. | 40 | Z1 | [V] | — |
| 9 | A temperatura ambiente dura apenas unas 72 horas tras la cosecha. | 65 | Z1 | [V] | — |
| 9 | Da fruta todo el año, con cosechas más grandes cada dos o tres meses. | 69 | Z1 | [V] | — |
| 10 | Por su acidez casi no se come crudo: se usa en jugos, néctares, helados y mermeladas. | 85 | Z2, Z3 | [E] | — |
| 12 | En Colombia se siembra sobre todo en Caquetá, Putumayo y Guaviare, entre otros árboles. | 87 | Z1 | [V] | — |
| 15 | En Manaos le dicen «guayaba peruana», y en Iquitos, «guayaba brasileña». | 72 | Z1 | [V] | — |
| 15 | En Brasil se llama araçá-boi; en Ecuador también le dicen membrillo. | 68 | Z1, Z2 | [E] | — |
| 16 | Es pariente de la guayaba, la feijoa y el camu camu: familia de las mirtáceas. | 78 | Z1 | [V] | — |
| 16 | El botánico McVaugh lo describió en 1956. | 41 | Z1 | [V] | — |
| 18 | Al principio, algunos no lo comían: lo usaban para perfumar la casa. | 68 | Z1 | [V] | — |

Fuentes:

- **Z1** Hernández, Barrera y Carrillo (2006), *Arazá*, Instituto SINCHI (Colombia) — leído en esta sesión: <https://sinchi.org.co/files/publicaciones/publicaciones/pdf/araza_2web.pdf>
- **Z2** Wikipedia EN, «Eugenia stipitata» — leído en esta sesión: <https://en.wikipedia.org/wiki/Eugenia_stipitata>
- **Z3** Embrapa Mandioca e Fruticultura, folder de jalea de banana con araçá-boi: <https://www.infoteca.cnptia.embrapa.br/infoteca/bitstream/doc/919367/1/foldergeleianet.pdf>


---

## 6. Formato de datos para implementarlo (sin código)

### 6.1 Dos archivos: calendario y textos

Encaja con la propuesta de idiomas (`docs/research/idiomas/propuesta-idiomas.md` §3.3-§3.4 y §3.11), escrita en paralelo: los textos largos de frutas y fauna van en una tabla aparte, `facts.csv`, con las mismas claves `fact.acai.01` que usa este documento.

1. **Calendario, uno para todos los idiomas** [P]: `Assets/_Project/Data/Content/frutas_del_rio_v1.json`, formato `af-facts/1`, en la línea del catálogo de fauna (`af-fauna/0`). Son datos: cambiar el orden o añadir una serie no toca código. Contiene los bloques y el catálogo de fuentes.
2. **Textos y su estado:** `Assets/_Project/Data/Loc/facts.csv` de la propuesta de idiomas (§3.4). Una sola columna `max_chars` y una sola `status` por fila **no bastan**: el límite cambia por idioma (90 en español, 117 en los demás) y la compuerta de release depende del estado de cada idioma. Cabecera canónica propuesta [P], que la propuesta de idiomas debe adoptar igual (su §3.3 tiene hoy `max_chars` y `status` únicos y su ejemplo omite `notes`):
   `key,es-419,en,pt-BR,context,max_chars,status,status_en,status_pt-BR,notes`
   - `context`: «Dato del río, pantalla de victoria, serie {tinte} k/20», el nombre científico y la fuente con su URL;
   - `max_chars`: el límite en `es-419` (90). Para los demás idiomas la prueba aplica una regla escrita: `max_chars × 1,3`, redondeado hacia abajo (117). Así un idioma nuevo no añade una columna de límite;
   - `status`: el estado del texto fuente y de la revisión cultural: `borrador` → `pendiente_asesor` → `aprobado_es`. Las líneas de este documento empiezan en `pendiente_asesor`;
   - `status_en`, `status_pt-BR` (una por idioma; `status_fr`… al añadir otro): vacío → `traducido` → `revisado_nativo`. Una traducción solo vale si `status` está en `aprobado_es`: si el asesor cambia el español, las columnas de los demás idiomas vuelven a vacío;
   - `notes`: la marca [V]/[E] y la nota de sensibilidad de las tablas de la sección 5.
3. **Si el dueño elige Unity Localization** (pregunta Q3 de la propuesta de idiomas), el CSV se importa a String Tables con un método de Editor versionado y las claves no cambian. **Si elige el sistema propio**, el CSV es la tabla. En los dos casos, nada de este documento cambia.

### 6.2 Claves estables

| Clave | Qué es | Ejemplo ES-419 |
|---|---|---|
| `fact.<tinte>.<kk>` | Línea k de la serie (dos cifras) | `fact.acai.03` → «Crece en matas de hasta 35 tallos…» |
| `fact.<tinte>.tag` | Frase de la tarjeta «Fruta nueva» | `fact.acai.tag` → «Açaí: la crema morada del Pará» |
| `tint.<tinte>.name` | Nombre visible del tinte | `tint.urucum.name` → «Achiote»; en PT-BR «Urucum» |
| `river_fact` | Etiqueta (ya existe, glosario §10) | «Dato del río» |
| `fact_progress` | «{name} · {k}/{n}» | «Açaí · 3/20» |
| `fact_sheet_title` | Título de la ficha y de la lista | «Frutas del río» |
| `fact_locked` | Línea en silueta | «Nivel {n}» |
| `fact_starts_at` | Serie aún no empezada | «Su serie empieza en el nivel {n}» |
| `fact_sources` | Pie de la ficha | «Fuentes» |
| `fact_a11y` | Etiqueta del lector de pantalla | «Dato del río. {name}, {k} de {n}. {text}» |

- `<tinte>` es el ID de código del glosario §9 (`acai`, `urucum`, `jenipapo`, `agua_negro`…), que no cambia aunque cambie el nombre visible (achiote/urucum, huito/jenipapo).
- `fact_id` del evento = `<tinte>.<kk>` (`acai.03`): la analítica lleva el ID, nunca el texto (regla 6 de la propuesta de idiomas, §3.2). **Nunca se reutiliza un ID** para otro dato: si el asesor veta una línea, la nueva ocupa la misma posición en una versión nueva del calendario y el cambio queda en el historial.
- Marcadores con nombre (`{name}`, `{k}`), nunca posicionales ni concatenados; nada de `.Replace` sobre textos (el inventario encontró dos en `StuckModal`).
- Los nombres de tinte salen de claves y no de los campos `name_es/en/pt` de `tints.json` (08 §4.2), para que un cuarto idioma no obligue a cambiar el formato de la paleta.

### 6.3 Ejemplo (datos, no código)

Calendario:

```json
{
  "format": "af-facts/1",
  "version": 1,
  "max_chars_es": 90,
  "series": [
    { "subject": "acai",         "kind": "tint", "from_level": 1,  "lines": 20, "step": 1 },
    { "subject": "agua_tapajos", "kind": "tint", "from_level": 21, "lines": 20, "step": 1 },
    { "subject": "urucum",       "kind": "tint", "from_level": 41, "lines": 20, "step": 1 }
  ],
  "sources": {
    "A1": { "title": "Embrapa, Plantas para o Futuro – Região Norte, cap. açaí", "url": "https://www.infoteca.cnptia.embrapa.br/…" }
  }
}
```

Textos (`facts.csv`):

```csv
key,es-419,en,pt-BR,context,max_chars,status,status_en,status_pt-BR,notes
fact.acai.03,"Crece en matas de hasta 35 tallos, como un ramo de palmeras que salen juntas.",,,"Dato del río, victoria, serie acai 3/20. Euterpe oleracea. Fuente A1",90,pendiente_asesor,,,"[V]"
fact.acai.16,"«Açaí» viene del tupí y se suele traducir como «fruta que llora», por su jugo.",,,"Dato del río, victoria, serie acai 16/20. Fuentes A1 y A8",90,pendiente_asesor,,,"[E] Etimología tupí: asesor; nunca la leyenda de Iaçá"
```

`step` permite las series de especie de 10 líneas (una cada dos niveles) de 3.5 sin cambiar el formato. En un build de release, en español solo se muestran las líneas con `status` = `aprobado_es`; en otro idioma, las que además tienen su `status_<idioma>` = `revisado_nativo`. En desarrollo salen todas, marcadas.

### 6.4 Cómo se elige la línea de cada nivel (determinista)

1. **Buscar el bloque:** la serie con `from_level ≤ n < from_level + lines × step`. Si no hay, no hay línea.
2. **Posición:** `d = n − from_level`. Si `d` no es múltiplo de `step`, no hay línea; si lo es, `k = d / step + 1` y la clave es `fact.<subject>.<kk>`.
3. **Cadencia de Remote Config:** se muestra si `ui_river_fact_every_n_levels` = 1, o si es > 1 y `n` es múltiplo de ese valor; con 0, nunca.
4. **Revelado (ficha):** una línea está revelada si su nivel es ≤ el nivel más alto superado (`progress.max_level` del perfil, 08 §4.3) o está en `progress.skipped_levels` (Salto del Boto). Ambos campos ya existen en `Profile.cs`: no se guarda nada nuevo ni se migra el perfil.
5. **Idioma:** si la clave no existe en el idioma del jugador, la línea no se muestra (nunca se mezclan idiomas en pantalla) y la ficha la deja en silueta; la regla general de idioma por defecto es de la propuesta de idiomas.

Sin azar, sin reloj, sin diccionarios recorridos para decidir, sin asignaciones por nivel: una búsqueda en un array ordenado.

### 6.5 Notas para el traductor (van como comentario de cada clave)

- **Contexto:** «Línea educativa en la pantalla de victoria; una sola idea; máximo ≈ 117 caracteres (90 en ES + 30 %); se lee en ≈ 5 s».
- **Fuente:** la URL de la línea, para comprobar el sentido; ante una duda, se simplifica conservando el dato, nunca se añade otro.
- **Punto de vista del idioma:** las líneas de nombres se adaptan. «En Brasil lo llaman buriti» en PT-BR pasa a «No Peru, chamam de aguaje»; en EN se mantiene «Aguaje (moriche palm)» del glosario §9. Lo mismo con «graviola», «urucum», «jenipapo», «cupuaçu» y «araçá-boi».
- **El traductor no cambia afirmaciones** («el más grande» no pasa a «uno de los más grandes») ni cifras.
- **No se traducen:** nombres científicos, nombres locales explicados en la propia línea (aguajina, peconha, colorau, lulada), «cuia» y «Tintalú».
- **Español neutro:** revisar con los revisores de CO, PE y MX palabras como «banana» (plátano/banano) y «sajino» (pecarí).
- **Prohibido:** añadir salud, superlativos de marketing o exclamaciones dobles (glosario §10).

### 6.6 Validación automática [P]

Una prueba EditMode (como `FaunaCatalogTests`) comprueba en cada PR:
- los bloques cubren sin huecos ni solapes el tramo declarado (1-320 en la versión 1) y cada `from_level` de un tinte es ≥ su debut;
- cada clave del calendario existe en `facts.csv` con texto `es-419` de ≤ `max_chars` (90) y cada traducción no vacía de ≤ `max_chars × 1,3` (117);
- cada línea tiene en `context` al menos una fuente del catálogo, en `notes` la marca [V] o [E] y un `status`; ninguna [NV]; ningún `status_<idioma>` con valor si `status` no está en `aprobado_es`;
- todos los caracteres están en el atlas de fuentes vigente;
- con la pseudolocalización de 08 §14.2 (+30 %) y una prueba extra a +40 %, la línea entra sin recortes a 360 dp con la escala al 130 % y el panel sigue cabiendo en 640 dp.

---

## 7. Arte

Dirección D-003, «Selva brillante premium»: vidrio, madera y luz de juego. **Nada de lámina botánica en papel ni acuarela**, aunque el tema invite.

| Pieza | Cantidad | Tamaño | Uso | Notas |
|---|---|---|---|---|
| Fruta `fruta_<id>.png` | 14 | 256 × 256 | Icono de la línea (32 dp), cabecera de la ficha, tarjeta «Fruta nueva» | **Ya está en la lista de D-010** (07 §6.4): no es encargo nuevo. Aguas: gota turquesa y hoja seca hundida |
| Escena de ficha `ficha_<id>.png` [P] | 14 | 1024 × 640 | Cabecera de la ficha y página Frutas del Cuaderno (1.1) | La planta en su paisaje, el fruto entero y abierto. Aguas: playa blanca de río con agua turquesa (Tapajós); el Encuentro de las Aguas visto desde arriba (Negro). La silueta de lo no revelado la genera el código |
| Sello «Ya conoces» [P] | 1 | 256 × 256 | Contador 20/20 y ficha completa | Medallón de madera con hueco para el icono de la fruta. Nunca la semilla ámbar ovalada (icono de Semillas) |
| Marco de la línea y de la ficha | 2 (9-slice) | — | UI | Del UI kit (07 §7): madera clara y vidrio |

- **Reglas por escena** (07 §8.1 y checklist de 07 §8.2): sin personas, aldeas ni manos; nadie trepando una palmera (açaí); nada de pintura corporal (achiote, huito); sin árboles sagrados (sumaúma, lupuna); sin Ver-o-Peso con gente; el cacao siempre en mazorca, nunca un grano suelto; el lulo en la ladera andina-amazónica, nunca en el llano de Leticia; la bacaba con su cáscara morada real (su rosa es estilización del tinte: el asesor y el artista deciden si la escena lo matiza).
- **Coste [E]:** las 14 escenas, ½-1 día cada una (7-14 días de artista); sello y marcos, 1 día. Prioridad: açaí, Tapajós, achiote, Río Negro y copoazú (los bloques hasta el 100). Hasta que lleguen, la ficha funciona solo con el icono.

---

## 8. Riesgos y cómo se evitan

Todo queda **«pendiente del asesor cultural»** (07 §8.2): las 240 líneas, las 14 frases de «Fruta nueva», los nombres por país y las etimologías.

| Riesgo | Dónde | Cómo se evita |
|---|---|---|
| **Cultural:** pintura corporal | Achiote y huito, cuyo uso más conocido es ese | Las series hablan del fruto, la cocina, el color en alimentos y el agua. El huito explica su índigo con el jugo que «se va volviendo azul» con el aire, nunca «al instante» (07 §5.1). El asesor revisa estas dos series primero |
| **Cultural:** etimologías en lenguas indígenas | Açaí, cupuaçu, urucum, cajá, *Bixa* y guanábana (taíno) y cacao (maya y náhuatl) | Solo el significado, con «se suele traducir» cuando la fuente no es un diccionario; nada de leyendas (Iaçá). Revisión del asesor; nheengatu solo con revisión pagada (07 §8.1) |
| **Cultural:** pueblos nombrados | Tapajó (Tapajós), taíno (achiote y guanábana) | Solo como dato informativo, como permite 07 §8.1; en presente cuando el pueblo existe hoy (los tapajó son uno de los 13 pueblos del Conselho Indígena Tapajós e Arapiuns [V]) y con el nombre que usa; nunca personajes, creencias, rituales ni territorios en disputa; ningún pueblo no contactado. La regla anterior («ningún pueblo actual») no está en el GDD y dejaba a los pueblos solo en pasado: queda retirada |
| **Cultural:** mirada colonial | Orellana, Oviedo, Humboldt | Los europeos, solo como dato neutro y nunca como descubridores ni homenaje (4.2). Ya se quitaron Orellana del achiote (k19) y de la reserva del Río Negro, y Humboldt del aguaje (k19). Quedan, como dato neutro, Humboldt y Bonpland en el Casiquiare (Río Negro k06) y Oviedo en la guanábana (k18): el asesor decide si se cambian |
| **Salud** | Camu camu (vitamina C), guanábana (mito del cáncer), açaí (Chagas), aguaje (creencias) | Ninguna línea de salud o nutrición, tampoco en la reserva: la de vitamina C pasó a excluidos (5.14); las semillas de guanábana «no se comen», sin hablar de toxicidad |
| **Conservación** | Tala de palmeras de aguaje; botos En Peligro; minería en el Tapajós; tetra cardenal; sequías del Negro | Siempre en positivo y sin culpables: se promueve trepar y bajar el racimo; tortugas que se recuperan; turberas que guardan carbono; el Tapajós con su agua clara como carácter natural y el «río azul» en pasado, sin mercurio ni culpables; el comercio de peces, sin promoverlo; el boto, sin su leyenda |
| **Legal y marca** | Marcas, empresas, medios, logos | Sin empresas (Nestlé, Ford, la compradora del copoazú), sin medios (The Guardian), sin el logo de la UNESCO (solo «Patrimonio Mundial»), sin «cupulate» ni disputas de registro. Las líneas son redacción propia de datos: no reproducen texto de las fuentes |
| **Exactitud** | 72 líneas [E], muchas con Wikipedia como fuente (sobre todo las aguas y el achiote) | Antes de publicar, el asesor o un agente con web sube cada [E] a una fuente primaria (ANA o ICMBio para los ríos, FAO o Embrapa para el urucum) o la cambia por una de reserva. Las cifras van con «unos» o «cerca de» |
| **Estilizaciones** | Bacaba rosa, icono de bol del açaí, lulo andino | Ninguna línea afirma la estilización; la serie del lulo va en el Oriente de Ecuador, no en el Trapecio |
| **Primera sesión** | Una línea más en las victorias 1-10 | Sin sonido, sin animación, debajo de los botones; pregunta 3; alarma del embudo 1-10 (06 §2.4) y apagado remoto con la clave existente |
| **Cansancio** | Una línea en cada victoria | Medir `dismissed_ms`; si nadie lee, pasar a una cada dos niveles con la clave existente (2.10) |
| **Volumen de traducción y revisión** | ≈ 3.312 palabras por idioma solo en las 12 series; 09 #28 calculó 300-600 cadenas para todo el juego | ≈ 6.700 palabras para EN y PT-BR ≈ 540-1.000 USD a 0,08-0,15 USD por palabra [E, tarifa orientativa sin fuente en el repositorio], frente a los 600-1.500 USD de 09 #28, que no contaban con estos datos (es la pregunta Q12 de la propuesta de idiomas). Asesor: ≈ 4-8 h para las 240 líneas [E], dentro de sus 20-40 h. Si el presupuesto no llega, la 1.0 sale con las series de 1-100 traducidas y el resto llega en la versión siguiente; mientras tanto, en EN y PT-BR esas líneas no se muestran (6.4, punto 5) |
| **Contenido que se acaba** | Las 14 series cubren hasta el 320 | Plan de 3.5 con el mismo sistema (ríos y especies) |
| **Una novedad sobre otra** | Debuts de tinte, especie y mecánica; estrenos en p20 (20, 100, 160); el 21 | Calendario de 3.3: p1 tinte y especie en la intro, p2 tarjeta de la especie, p3 mecánica; sello sin animación en los p20 con estreno; el 21 se acepta con dos frutas en momentos separados; corregir `SightingRule` desde `campaign_v1.json` |
| **Enlaces externos** | Fuentes en la ficha | Se muestran como texto, sin abrir el navegador desde el juego |

---

## 9. Preguntas cerradas para el dueño

| # | Pregunta | Opciones | Recomendación |
|---|---|---|---|
| 1 (**decidida el 2026-10-13: A**, D-013; choque con el Mapa en la pregunta 45 de GDD.md §6.3) | ¿La serie de frutas **absorbe** el «Dato del río» del GDD? | **A.** Sí: una línea por nivel en la victoria, misma etiqueta, misma clave (default 10 → 1; 0 = apagado) y mismo evento; sale de la pantalla de la Canasta. **B.** Conviven: frutas en la victoria y Dato del río aparte cada 10 niveles. **C.** Se mantiene el canon: un dato cada 10 niveles | **A**: un solo sistema, sin claves ni eventos nuevos, y resuelve que el GDD lo ponga en tres sitios |
| 2 | ¿Cuántas líneas por fruta? | **A.** 20, una por nivel (tu ejemplo). **B.** 10, una cada dos niveles. **C.** 5 | **A**, con la opción de bajar a B desde Remote Config si las métricas dicen que no se lee |
| 3 | ¿Desde qué nivel sale la línea en la victoria? | **A.** Desde el 1 (el açaí abre el juego). **B.** Desde el 11 (las líneas 1-10 del açaí aparecen ya reveladas en la ficha). **C.** Desde el 21 | **A**: es lo que da sentido al juego desde el principio y no es una pantalla ni una instrucción; se vigila con la alarma del embudo 1-10 y se apaga en remoto si molesta. B si prefieres no tocar nada del tutorial |
| 4 | ¿Hay una ficha para releer en la 1.0? | **A.** Sí: hoja simple desde la línea y desde Ajustes → Sobre la Amazonía, con el evento nuevo `af_fact_page_open` para medir aperturas. **B.** No: en la 1.0 solo la línea; la ficha llega con el Cuaderno de campo en la 1.1 (lo revelado se recupera solo) | **A**: sin un sitio donde releer, lo que el jugador se salta se pierde, y la ficha es poca obra |
| 5 | ¿Completar una fruta da premio? | **A.** No: sello visual y ficha completa. **B.** 20-50 semillas al completar, dentro de la Canasta del hito. **C.** Solo el premio del canon para el Cuaderno de 1.1 (300 semillas + marco al completar la página Frutas) | **A** en la 1.0 y **C** en la 1.1: completar una fruta es llegar al hito, que ya paga; B obliga a recalibrar la economía |
| 6 | ¿Nombre? | **A.** «Frutas del río», con la etiqueta «Dato del río» en la victoria. **B.** «Fruta a fruta». **C.** «Del árbol a la cuia» | **A**: no añade términos a la victoria y ya está traducido; pendiente del asesor |
| 7 | ¿Dónde va la línea en el panel de victoria? (2.2, regla 6) | **A.** Debajo de «Inicio», en una ranura de alto fijo por idioma y escala: «Siguiente» sube una vez (≈ 90 dp en español) y queda en el mismo sitio en todos los niveles. **B.** Encima de la Cosecha: «Siguiente» no se mueve respecto a hoy, pero la línea queda entre el titular y la Cosecha y se lee antes de pulsar | **A**: el botón sigue siendo lo primero y la línea no estorba a quien no quiere leer; se confirma con capturas en F3. B si las capturas muestran que el panel tapa demasiado tablero |

---

## 10. Plan de implementación por pasos pequeños

Después del paso 1 de la integración (olas 3-5 en curso). Cada paso es una rama, un worktree y un PR pequeño con revisión (`docs/setup/equipo-de-agentes.md`). **Sin paquetes, SDK ni versiones nuevas** (si el dueño aprueba Unity Localization en la propuesta de idiomas, solo cambia cómo se importa el CSV).

| Paso | Agente | Qué | Archivos | Hecho cuando |
|---|---|---|---|---|
| F0 | tech-lead | Registrar la decisión (D-011 [P] o el número que toque; D-007 y D-009 están reservados sin registrar) y editar el GDD (10.1) | `docs/dev/decisiones.md`, `docs/gdd/*`, `docs/GDD.md` | El dueño responde 1-7 y aprueba las ediciones |
| F1 | unity-gameplay-dev | Calendario `af-facts/1` y textos ES de esta propuesta en `facts.csv` (series, frases de «Fruta nueva», contexto con la fuente, estado `pendiente_asesor`). Va **después del nivel 2 de la propuesta de idiomas** (CSV y fachada `Loc`), para que el texto nazca en la tabla y no en el código | `Assets/_Project/Data/Content/frutas_del_rio_v1.json` y `Assets/_Project/Data/Loc/facts.csv` (`.claude/rules/data-json.md`: UTF-8 sin BOM, LF, orden de claves estable; los `.meta` los genera Unity) | Prueba EditMode de 6.6 en verde |
| F2 | unity-gameplay-dev | Lógica pura: calendario, línea por nivel, revelado por progreso, cadencia de Remote Config | `Assets/_Project/Scripts/GameFlow/FactSchedule.cs` (asmdef sin motor), lector en `Assets/_Project/Scripts/Levels/FactCatalogReader.cs`, `Tests/EditMode/FactScheduleTests.cs` | Cobertura 1-320; k correcto en los bordes de bloque; `every_n` 0/1/2/10; Salto del Boto revela; sin asignaciones por llamada |
| F3 | unity-gameplay-dev | Línea en la victoria (2.2), en una ranura de alto fijo por idioma y escala, con el caso del nivel 10. Va después de T09 (fila del chip de D-009) | `Assets/_Project/Scripts/UI/GameScreen.cs`, `FactLine.cs` nuevo; textos por la fachada `Loc` de la propuesta de idiomas (acceso tipado desde `UiText`) | PlayMode y capturas reales a 360 × 640 dp, en español y con la pseudolocalización (+30 % y +40 %), a escala 100 % y 130 %, con y sin Cosecha doble y con el chip de T09: «Siguiente» queda en el mismo sitio con la línea más corta y con la más larga de cada idioma, no se tapa, y el panel con la cinta cabe bajo el HUD |
| F4 | unity-gameplay-dev | Ficha de lectura y «Sobre la Amazonía» mínima (si pregunta 4 = A) | `FruitSheet.cs` nuevo, Ajustes | Abre y cierra sin perder el estado de la victoria; siluetas y «Nivel {n}» correctos |
| F5 | unity-gameplay-dev | Tarjeta «Fruta nueva» del canon (hoy solo reservada) con su frase; mínimo de 12 sp en `SightingCard`; lista de niveles ocupados sacada de `campaign_v1.json` (`mechanics[].debut` ∪ `capacities[].debut`) más los hitos de 02 §13, o `intro_mechanic` escrito también en las variantes (3.3) | `SightingCard.cs`, `SightingRule.cs`, tarjeta nueva; si se elige la segunda vía, el generador (`level-pipeline-dev`) y los packs regenerados | Prueba que compara la lista con las dos fuentes (especificación y packs) y comprueba que 100, 163, 18, 22 y 160 quedan ocupados; ninguna tarjeta por debajo de 12 sp |
| F6 | unity-gameplay-dev | Emitir `af_river_fact_shown` (y `af_fact_page_open` si se aprueba) en la fachada de analítica simulada, sin SDK | Fachada de analítica existente o la que defina 08 §8 | Prueba del `fact_id` y del `dismissed_ms` |
| F7 | según la propuesta de idiomas | Revisión del asesor, traducción EN y PT-BR por lotes (primero 1-100) y revisión nativa; pseudolocalización de 08 §14.2; importación a String Tables solo si se elige Unity Localization | `Assets/_Project/Data/Loc/facts.csv` (y las tablas generadas, si las hay) | Ninguna línea por debajo de `aprobado_es` en un build de release |
| F8 (opcional) | level-pipeline-dev | Mínimo de presencia del tinte de la serie en su bloque (≥ 18/20) en el generador; regenerar 11-200 y `level-qa` | `tools/level-generator/specs/campaign_v1.json`, packs regenerados | Solo si las métricas lo piden; packs idénticos salvo la paleta y QA en verde |
| F9 | dueño → artista | Escenas de ficha y sello (sección 7) | `Assets/_Project/Art/` (PNG por Git LFS) | Revisadas por el asesor |

Verificación de cada paso con los comandos de `CLAUDE.md` (dotnet, `validate_schema.py`, `unity-compile`, EditMode y PlayMode) por el agente que tenga el Editor libre; este documento no ejecutó nada.

### 10.1 Ediciones del GDD que haría falta aprobar (paso F0)

- **Glosario §6.4:** «Dato del río» = una línea por nivel organizada en series de 20 por tinte. **§10:** claves `fact_progress`, `fact_sheet_title`, `fact_locked`, `fact_starts_at`, `fact_sources` y `fact_a11y`, con EN y PT-BR [P]. **§11:** formato de `fact_id`.
- **02 §6:** fila «Dato del río» (cada nivel, debajo de «Siguiente», abre la ficha) y el boceto de la pantalla.
- **06 §2.3:** el bocadillo del Mapa del río del nivel 10 muestra la línea 10 del açaí. **§4.4:** el Dato del río sale de la pantalla de la Canasta. **§4.5:** frase de «Fruta nueva» = `fact.<tinte>.tag`. **§4.6:** la página Frutas muestra las fichas k/20.
- **07 §5.4:** el panel de victoria pierde el «papel» (D-003) y gana la línea. **§6.2:** `rosa_fact` sin tarjeta de papel. **§8.2:** el asesor revisa las series por mundo.
- **08 §8.2:** formato de `fact_id` en `af_river_fact_shown` y, si se aprueba, `af_fact_page_open`. **§9:** default de `ui_river_fact_every_n_levels` = 1 y rango con 0 = apagado.
- **09 #28:** volumen de cadenas y horas del asesor de la sección 8.

---

## 11. Fuentes de los patrones de juego (sección 2)

Consultadas por la línea de investigación de patrones el 2026-10-09/10 [V] o [E] según 2.1:

- Magic Sort: `docs/research/magic-sort/ficha-magic-sort.md` (§7.8, §8.4) y `docs/research/09-referencia-magic-sort.md`.
- AppMagic, *Mobile live-ops in 2025*: <https://gamedevreports.substack.com/p/appmagic-mobile-live-ops-in-2025>
- Royal Match, Royal Collection: <https://oldcynic.com/royal-match-cards-royal-collection> · Gamigion sobre álbumes: <https://www.gamigion.com/?p=15197>
- Pocket Gamer (Rovio, Color Block Jam): <https://www.pocketgamer.biz/rovios-anastasiya-kara-on-how-live-ops-took-over-mobiles-biggest-games/>
- Hollow Knight, Diario del cazador: <https://hollowknight.wiki/w/Hunter's_Journal_(Hollow_Knight)> · Hades, Códice: <https://hades.fandom.com/wiki/Codex> · logros de Hades: <https://store.epicgames.com/achievements/hades> · Hades II: <https://exputer.com/guides/hades-2-book-of-shadows/>
- Pokédex: <https://bulbapedia.bulbagarden.net/wiki/Pok%C3%A9dex_entry> · <https://pokemondb.net/pokebase/404708/what-is-the-shortest-pokedex-entry>
- Balmford et al. (2002), *Science* 295:2367: <https://eepro.naaee.org/research/eeresearch/why-conservationists-should-heed-pokemon>
- Animal Crossing (Blathers): <https://nookipedia.com/wiki/Blathers> · Fisher et al. (2021), *People and Nature* 3(6):1218: <https://bes.brightminded.com/applied-ecology-resources/document/20220355063>
- Wingspan: <https://gamingtrend.com/interviews/crazy-eights-elizabeth-hargrave-on-wingspan> · <https://www.meeplemountain.com/reviews/wingspan-digital>
- Plants vs. Zombies, Almanaque: <https://plantsvszombies.wiki.gg/wiki/Almanac> · Neko Atsume: <https://bookriot.com/cats-neko-atsume-reading/>
- GameRefinery, narrativa en móviles: <https://www.gamerefinery.com/game-changing-storytelling-how-to-integrate-narrative-elements-in-mobile-games/>
- Netflix, guía de subtítulos en español: <https://partnerhelp.netflixstudios.com/hc/en-us/articles/217349997-Castilian-Latin-American-Spanish-Timed-Text-Style-Guide> · Brysbaert (2019), velocidad de lectura: <https://www.bps.org.uk/research-digest/most-comprehensive-review-date-finds-average-persons-reading-speed-slower>
- W3C, tamaño del texto al traducir: <https://www.w3.org/International/articles/article-text-size>
- Voz de Duolingo: <https://design.duolingo.com/writing/voice> (redirige a <https://blog.duolingo.com/hub/design/>)
