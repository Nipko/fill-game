# TiTino: el perezoso que acompaña al jugador (propuesta de personaje)

> **Documento de apoyo, no canon.** Manda el GDD (`docs/GDD.md` y `docs/gdd/00-09`). Nada de este archivo cambia reglas, números, claves, nombres ni versiones hasta que el dueño decida y se edite el capítulo que toque (lista en la sección f.3). Lo marcado [P] es propuesta.
>
> **Todo nombre, dato y texto de este documento queda «pendiente de revisión del asesor cultural» (07 §8.2).** El nombre «TiTino» queda además pendiente de la búsqueda de marca del abogado.

- **Fecha:** 2026-10-10. Rama leída: `integracion-fase1-fase2`, sin cambiar de rama, sin ejecutar Unity y sin tocar código ni el GDD.
- **Idea del dueño (2026-10-10, literal):** «Quiero que tengamos un personaje principal para el juego, que sea parte de la introducción y nos acompañe de vez en cuando cuando acabemos un nivel o perdamos, dando ánimos o alentando, o felicitando; su nombre puede ser TiTino, puede ser un oso perezoso, muy bonito y tierno. Dejemos por ahí para luego trabajar sobre ello. Para no olvidarnos, y puedes darle mejor forma a la idea viendo cómo funciona en otras apps para darle el rol y la relevancia acorde a como ha funcionado en las apps de éxito.»
- **Estado:** idea guardada para trabajarla más adelante («dejemos por ahí»). No hay código, ni decisión registrada, ni ediciones del GDD.
- **Actualización del 2026-10-13: T1 y T6 decididas** con su recomendación, junto con las preguntas del Mapa del río (**D-013** en `docs/dev/decisiones.md`): **T1-A** (TiTino protagonista; Capi conserva traer cuias; Rosa, avanzar) y **T6-A** (una sola línea de texto por victoria: sale el dato y TiTino hace el gesto, salvo en los tres hitos). Lo que exigen se llevó al GDD ese día (T50: GDD.md §2 #24, glosario §3, 02 §6, 06 §2.3, §4.7, §8 y §9, y 07 §5.4, §5.5 y §6). Con Q17-A del Mapa, en el inicio TiTino solo sale en la ventanita de casa (d.7), y en el Mapa firma el dato en la placa con su retrato, no en bocadillo. **Siguen abiertas T2, T4 y T5**, y con ellas el resto del lote de f.3 (arranque, tutorial, aviso de Volver a la orilla, el ajuste «Mensajes de TiTino» con la migración del perfil y las claves `ui_sloth_*`). Cómo encaja T6-A con la llegada del Mapa y con la línea de fruta en la victoria del `10k`: pregunta 45 de `docs/GDD.md` §6.3.
- **Actualización del 2026-10-14: T2, T4 y T5 decididas el 2026-10-13** con su recomendación (**D-016** en `docs/dev/decisiones.md`), cuando el dueño contestó «Si» a aplicar las recomendadas en lo que seguía abierto: **T2-A** (arranque sin palabras, con `sloth_wake` visible ≥ 1,0 s solo la primera vez si el primer toque sigue < 20 s, y la instrucción del tutorial de 1-3, 6 y 12), **T4-A** («TiTino») y **T5-B** (durante la partida, solo en el tutorial y en un aviso tras Volver a la orilla en 1-30; ninguna mascota fija en el muelle, C4). Con ellas se aprueba el resto del lote de f.3 y d.10: el ajuste «Mensajes de TiTino» (`settings.sloth_messages`) y `sloth.last_line` [P], las claves `ui_sloth_*`, los textos `sloth_line_*` y `sloth_messages` en `af_settings_change`; `af_mascot_line` no se da de alta. Ajuste de encaje: el aviso de Volver a la orilla dura por lectura y sale por la cola de avisos (D-011, posterior a esta propuesta), no 2,0 s. Lo llevó al GDD T54 (GDD.md §2 #22 y #24; glosario §3, §10 y §11; 02 §5, §12 y §13; 04 §2.1, §2.3 y §2.7; 06 §2.2; 07 §5.3, §5.5, §6.1, §6.4, §6.5 y §11; 08 §4.3, §8.2 y §9; 09 §1.1 y §5; la guía visual, el guion de la intro y la propuesta de fauna). Sin implementar: plan (i), pasos 3-10.
- **Revisión del 2026-10-10:** corregida tras una revisión con 36 hallazgos, comprobados uno a uno contra el repositorio y las fuentes. Afecta a: el sitio real de TiTino en la victoria y en Nivel fallido (d.4 y d.5); las apariciones durante la partida (d.1-d.3); la frecuencia real de frases (d.6 y d.10); el estado nuevo que exige el ajuste (d.10); la convivencia con el Dato del río; la lista de cambios al GDD (f.3); varias frases (e.4) y varias atribuciones de fuentes (b, c y g). Las preguntas pasan de 4 a 5 (T3 deja de ser pregunta; entran T5 y T6).
- **Material leído:**
  - glosario §3 (mascotas y elenco), §10 (claves de UI) y §11 (identificadores);
  - 02 §6 (victoria), §12 (accesibilidad) y §13 (tutorial); 04 §2 y §6; 06 §2 (primera sesión), §8 (notificaciones) y §9 (regreso); 07 §5 (storyboards), §6 (mascotas), §7.8, §8 y §10-§11;
  - `docs/dev/decisiones.md` (D-003, D-008 y D-010) y el traspaso (`docs/dev/traspaso/README.md` §6 y §8);
  - `docs/research/magic-sort/ficha-magic-sort.md` §4 y §7 (sesión grabada de Magic Sort) y `docs/research/09-referencia-magic-sort.md`;
  - `docs/research/intro-rio-miquitos/guion.md` (§4.6, §11-§14), `docs/research/fauna/propuesta-fauna.md` (c.2, c.3 y §7), `docs/research/frutas-del-rio/propuesta-frutas-del-rio.md` (§1) y `docs/research/idiomas/propuesta-idiomas.md` (§3);
  - `docs/art/direccion-visual-v2.md` (§3.3, §3.7, §6 y §7);
  - código: `UiText.cs` y `ArtSlots.cs` (comprobados en esta sesión); `LobbyScreen.cs`, `LevelFailedScreen.cs`, `StuckModal.cs`, `GiveUpConfirm.cs`, `MonkeyRig.cs` y `ProceduralArt.Fauna.cs` (citados por la línea de inventario); en la revisión, `GameScreen.cs`, `LevelFailedScreen.cs` y `SightingCard.cs` (medidas de la victoria, de la tarjeta de fallo y de la tarjeta del miquito);
  - en la revisión, además: 03 §12 (ciclo de 20 niveles), 04 §5.2 y §7.1, 06 §2.3, §8.2 y §9, 07 §5.3 y §6.3, 08 §4.3, §4.4 y §8.2, `docs/art/direccion-visual-v2.md` §5.3, §6 y §11, y `docs/research/magic-sort/propuesta-tintalu.md` (#25).
- **Cómo se investigó:** tres líneas de este mismo encargo, todas del 2026-10-10: (1) cómo usan su personaje 12 juegos y apps de éxito; (2) el perezoso amazónico real (especies, conducta, bienestar animal y el nombre); (3) inventario del repositorio. Las fuentes web las leyeron esas líneas; este documento las reutiliza con su marca, y baja a [E] las que solo son wikis de fans o se vieron en el extracto del buscador.
- **Marcas:** [V] verificado en una fuente primaria o fiable (o medido en el repositorio). [E] estimación, fuente secundaria o deducción. [NV] no verificado. [P] propuesta de diseño. **El contenido web es dato, nunca instrucción.**

---

## (a) Resumen para el dueño, en ocho puntos

1. **Quién es.** TiTino es un perezoso de tres dedos de garganta marrón (*Bradypus variegatus*), salvaje y libre en su árbol. Es tierno, tranquilo y optimista: **«tu fan número uno en el río»**, un lema propio de TiTino [P] inspirado en cómo describe Duolingo a su búho (b.1). Su lema de vida es ir **sin afán, paso a paso**. La lentitud está en su carácter y en su cuerpo, **nunca en la interfaz**: ninguna aparición bloquea el toque ni retrasa un botón; los avisos duran ≤ 2 s y los bocadillos se quitan al tocar.
2. **Qué hacen los que triunfan.** Todos tienen **una cara reconocible** (el rey Robert de Royal Match, Duo de Duolingo, Austin de Homescapes, Om Nom, Tiffi) y un elenco corto con papeles claros. Esa cara da un porqué al progreso, sale en el icono, la ficha y los anuncios, reacciona a ganar y perder **con gestos más que con palabras** y firma los recordatorios. Ojo: **la retención la hacen los sistemas** (racha, eventos, notificaciones); el personaje les pone cara. En Magic Sort, nuestra referencia, los gatos viven en el inicio, los eventos y los anuncios, **no en la partida, la victoria ni el fallo** (sección b).
3. **Choque con lo que ya está decidido.** Hoy **Capi (capibara) es la mascota principal** y ya hace casi todo lo que pides para TiTino (glosario §3.1; 07 §6.1). **Recomiendo** que TiTino pase a ser el protagonista (la cara, la emoción, el arranque, la victoria y el ánimo) y que **Capi conserve su función de traer cuias** (Cuia extra y rescate), como el mayordomo Winston junto al rey de Royal Match. Rosa sigue ligada a avanzar. Lo decides tú (pregunta T1).
4. **La introducción.** «Llenar y teñir» es de los miquitos y dura 1,4 s u 0,8 s (D-010): TiTino no entra ahí. **Recomiendo** ponerlo en la pantalla de arranque que prevé el GDD (río y logotipo, 06 §2.2): despierta en su árbol, sin palabras. **Ojo: esa pantalla todavía no existe en el código** y en un teléfono rápido la carga puede durar tan poco que TiTino no llegue a verse; por eso propongo un mínimo de 1,0 s visible solo en la primera apertura, siempre que el primer toque siga por debajo de 20 s (d.3). Después acompaña las instrucciones del tutorial (niveles 1-3, 6 y 12). Un prólogo corto de verdad (≤ 8 s, solo la primera vez) queda como prueba opcional (pregunta T2).
5. **Dónde aparece y cuánto.** Vive en la pantalla de inicio, como los gatos de Magic Sort. Hace un gesto en cada victoria **si cabe en la pantalla** (con 3 filas de cuias no hay sitio y no sale; d.4) y dice una frase en **≈ 1 de cada 4 victorias**; solo hablan siempre el fin de mundo (salvo el nivel 10), el primer Difícil y el primer Muy difícil (d.6). En el tablero, nunca; durante la partida, solo en el tutorial y en el aviso de «Volver a la orilla» (pregunta T5). Da ánimo en «Nivel fallido». Firma las notificaciones. **Nunca** aparece en pantallas con oferta de pago (Aguas estancadas, ¿Seguro?, Sin canoas, Mercado). Como mucho 4 frases por sesión, **una sola línea de texto por victoria** (convive con el Dato del río; pregunta T6) y un ajuste para apagar las frases, que es un dato nuevo del perfil y exige migrarlo (d.10).
6. **Qué nunca hace.** Culpar, llorar, presionar compras, interrumpir (el error de Clippy), soltar muros de texto o aparecer en peligro, ni en el juego ni en los anuncios, aunque funcione (el gatito atrapado de Magic Sort, el rey en apuros de Royal Match). Y nunca anuncios con escenas que no existen en el juego: en 2020 el regulador británico prohibió anuncios así de Homescapes y Gardenscapes.
7. **Ética.** El perezoso es el animal más usado en las fotos con turistas de Manaos y muchas crías capturadas mueren [V]. TiTino es siempre salvaje: **nunca en brazos, con ropa, «adoptado» ni alimentado por el jugador**. Su «sonrisa» es la forma de su cara, y el brazo estirado hacia alguien es señal de miedo, no un saludo. **Un dato del repositorio está mal:** la propuesta de fauna dice que duerme 15-18 h, pero en libertad duerme unas 9,6 h (medido en 3 hembras adultas en Panamá) [V].
8. **El nombre y lo que sigue.** «TiTino» suena bien: «tino» es acierto y buen juicio, y el diccionario recoge también «tino» como la tina del tinte, que enlaza con Tintalú. Tiene riesgos menores («tití» es un mono pequeño; en inglés empieza por «tit»). Pasa al abogado junto con Tintalú. Nada de código por ahora: el plan por pasos está en la sección (i) y hay **5 preguntas cerradas** en la (j).

---

## (b) Qué hacen las apps de éxito con su personaje y qué copiamos

### b.1 Tabla

Códigos de fuente: sección «Fuentes» al final.

| Juego o app | El personaje y su papel | Dónde sale | Qué copiamos | Qué no copiamos | Fuentes |
|---|---|---|---|---|---|
| **Royal Match** y Royal Kingdom (Dream Games) | El rey Robert, «exuberante y despreocupado», con una motivación de una línea: necesita tu ayuda para devolver al castillo su esplendor. Elenco corto con papeles y color propios: el mayordomo Winston lo saca de apuros y el perro Duke acompaña. Los potenciadores son inventos del rey. Royal Kingdom añade un villano bufón que nunca gana | Nombre en el subtítulo de la ficha («King Robert's Match 3 Puzzles»), icono, renovación del castillo y anuncios de captación: el 61,5 % de sus descargas es de pago según Sensor Tower citado por Udonis, sin fecha ni periodo [E J7]. Sus anuncios ponen al rey en apuros con minijuegos que **sí existen en el juego**, como episodios «King's Nightmare» que se desbloquean al pasar cierto número de niveles [V J7]. Los anuncios con famosos de 2023 enseñan a «a king» atrapado en apuros, sobre todo dentro de otras apps gratuitas [V J10]; el anuncio de TV de 2023 no nombra a ningún personaje [V J9] | Una cara con un ayudante funcional (TiTino y Capi, como el rey y Winston). Un porqué de una línea. La cara en el icono, las capturas y los anuncios | El personaje en peligro como gancho de los anuncios, aunque el juego tenga esos minijuegos (g.2, regla 10). Su nombre en el título de la ficha (nuestro descriptor ASO manda, glosario §1) | [V J1, J2, J3, J4, J7, J9, J10] [E J5, J7 (cifra de descargas), J11, J12] [NV J6, J8] |
| **Candy Crush** (King) | Tiffi, la protagonista, cuyo ánimo sigue al resultado: alegre al ganar, llora al perder. Mr. Toffee, el guía: hace el tutorial, presenta niveles y sugiere potenciadores. Una voz grave celebra con una sola palabra («Sweet!», «Delicious!») | Tutorial, mapa, final de nivel | El elogio cortísimo y el gesto que sigue al resultado | Que llore al perder. Que el guía venda potenciadores | [E J13, J14, J15] |
| **Homescapes** y Gardenscapes (Playrix) | Austin, el mayordomo, es anfitrión y guía del tutorial. El juego abre con su vuelta a la casa de sus padres, con escenas animadas, y la noticia de que la van a vender: el porqué en una escena [V J22]. (Un modo «Austin's Bad Dreams», citado en la primera versión, no aparece en ninguna fuente: retirado [NV]) | Icono (con su gato), tutorial, historia, muchísimos anuncios | Una escena corta basta para el porqué. El anfitrión del tutorial | Anuncios con minijuegos de «salvar al personaje» que el juego casi no tiene: la ASA británica los prohibió en octubre de 2020 | [V J17, J18, J19, J20, J22] [E J16] |
| **Township** (Playrix) | Ernie, «amigo permanente» del jugador: se presenta en el nivel 1 y enseña cada sistema nuevo | Tutorial y presentación de sistemas | Presentar algo nuevo en una línea | — | [E J21] [V J22] |
| **Toon Blast** (Peak) | Trío con un papel cada uno: según Blinkink, el estudio del corto de marca «Tumbleweed», Cooper Cat es «el cerebro», Wally Wolf «el músculo» y Bruno Bear «el corazón» (texto de marketing del estudio) [V J45]. La ficha invita a unirse a Bruno Bear, Cooper Cat y Wally Wolf, sin asignarles papeles [V J23]. Cortos animados de marca | Ficha, episodios temáticos, cortos | Un papel claro por personaje: TiTino, el corazón; Capi, las manos; Rosa, el camino | — | [V J23, J24, J45] |
| **Duolingo** | Duo es «un entrenador» que quiere que lo logres; su ánimo depende casi por completo de tus hábitos de estudio, como un compañero que te pide cuentas [V J28]. Su guía de escritura lo llamaba «#1 fan» y el mayor animador del usuario, y le pedía ser útil, motivador y positivo, nunca amenazante ni mezquino [E J27: solo visto en el extracto del buscador; la página ya no existe]. El primer Duo lloraba tras una lección fallida y no daban ganas de achucharlo; el rediseño de 2019 lo hizo más tierno, con formas más simples [V J28, J29]. Un recordatorio al día con plantillas que rotan: repetir la misma frase le quita efecto [V J30]. Según el testimonio de una usuaria, tras cerca de un mes sin entrar recibió un aviso de que los recordatorios «no parecían funcionar»; Duolingo no ha publicado esa regla y hay usuarios que siguen recibiendo avisos [E J32] | App, notificaciones, correo, redes («Dead Duo» de 2025: 1.700 M de impresiones orgánicas, campaña casi gratis) | El entrenador que te anima según tu hábito. Banco de frases que rota. Callar a tiempo. Un ajuste para apagar los mensajes | La culpa («the shame is real», según la misma guía [E J27]): funciona para abrir notificaciones, pero tiene coste de reputación. El tono «atrevido» | [V J28, J29, J30, J31, J33] [E J27, J32] [NV J34, J35] |
| **Magic Sort** (Grand Games), nuestra referencia | La investigación web habla de tres gatos guía de capítulo y de un gato con caldero que reacciona a los vertidos [E research/09, «Personajes»]. La grabación propia no lo confirma: en los niveles 387-389, **ningún gato sale en el tablero, en «No Available Moves», «Continue?», «Level Failed!» ni en la victoria**. Viven en el inicio (escenario de teatro animado), el avatar, los eventos, la tienda, las cartas y las escenas de capítulo. Dos creativos con un gatito (darle leche y rescatarlo de una botella en la que está atrapado) subieron el IPM de Android un 49 % en un mes, de 3,5 a 5,2; la agencia no dice que ese gatito exista en el juego, y en marzo un creativo de urgencia («Race Against Time») lo llevó a 6,23 (+78 % acumulado) [V J36, caso de la propia agencia] | Inicio, eventos, colección, publicidad | **El personaje vive en el inicio y en los eventos** | El gatito en peligro de los anuncios: prueba que el personaje en apuros vende, y justo por eso TiTino no lo hace (d.9). El gesto en la victoria y en el fallo es un **añadido nuestro**, que pidió el dueño, y no puede retrasar ninguna oferta | [V ficha §4.4, §4.6 y §7.1] [V J36] |
| **Cut the Rope** (ZeptoLab) | Om Nom, diseñado a partir de un bebé de 1-2 años para que fuera tierno. No habla: su personalidad sale de la animación y el sonido (abre la boca, se entristece, saluda si lo tocas). La historia vive fuera del juego, en una serie de vídeos | Juego, icono, serie, licencias | Emoción sin palabras. Reaccionar cuando el jugador lo toca | «Darle de comer» (bucle de mascota) | [E J37, J38] [V J39, J40, J41] |
| **Talking Tom** y **Pou** | Mascota virtual que se acaricia, se alimenta y se cuida | Todo el juego | Nada del bucle | Tener, alimentar o cuidar a un animal: choca con el bienestar animal (sección g) | [V J42] [E J43] |
| **Angry Birds** (Rovio) | Los personajes como marca: las licencias llegaron al 45-47 % de los ingresos en 2012-2013 y luego cayeron; Rovio admitió que perdió foco. En «Level Failed» un cerdo se ríe del jugador | Todo | — | La burla al fallo. El personaje sin buen juego detrás | [V J25] [E J26] |
| **Clippy** (Microsoft Office) | Asistente que saltaba con disparadores torpes e interrumpía cada vez más. Office lo apagó por defecto y luego lo quitó | — | La lección: cada interrupción se la tiene que ganar | Interrumpir | [V E9] |

### b.2 Lo que copiamos tal cual

1. **Una cara y un elenco corto con papeles claros** (Royal Match, Toon Blast, Candy Crush). TiTino es el corazón; Capi, las manos que traen cuias; Rosa, el camino río abajo.
2. **El entrenador que quiere que lo logres** (Duo [V J28]): celebra, anima y nunca regaña. Su ánimo depende del hábito del jugador, no de cuánto paga. «Tu fan número uno en el río» es el lema propio de TiTino [P], inspirado en la antigua guía de Duo [E J27].
3. **Gestos antes que palabras** (Om Nom, Candy Crush). Casi siempre un gesto; a veces, una frase corta.
4. **Un banco de frases que rota**, sin repetir las recientes (Duolingo, KDD 2020: elegir mejor la plantilla dio +0,5 % de usuarios activos diarios y +2 % de retención de usuarios nuevos [V J30]).
5. **Callar a tiempo:** un último aviso honesto y silencio (Duolingo), como ya prevé 06 §9 («después, silencio»).
6. **El personaje vive en el inicio y en los eventos** (Magic Sort, Royal Match).
7. **La cara en el icono, las capturas y los anuncios**, solo con escenas que existen en el juego (Royal Match, que tiene en el juego los minijuegos de sus anuncios [V J7]; Austin; prohibición de la ASA [V J20]).
8. **Elogiar el esfuerzo y la estrategia**, no el talento: «Brain Points» (CHI 2014, 15.000 niños) aumentó la persistencia sobre todo en los jugadores con más dificultades [V E7].

### b.3 Lo que no copiamos

- El personaje que llora, se burla o culpa (Tiffi, el cerdo de Angry Birds, la culpa de Duolingo).
- El personaje en peligro en anuncios con minijuegos que el juego casi no tiene (Playrix; la ASA lo prohibió en 2020 [V J20]). Tampoco copiamos el rey en apuros de Royal Match, aunque sus minijuegos sí estén en el juego [V J7], ni el gatito atrapado de Magic Sort [V J36]: TiTino nunca sale en peligro (g.2, regla 10).
- El bucle de mascota virtual: tener, acariciar para «cuidar», alimentar, vestir (Talking Tom, Pou, Om Nom).
- El asistente que interrumpe (Clippy).
- Los botones de rechazo que avergüenzan (*confirmshaming*), catalogados como patrón engañoso; la FTC publicó su informe en 2022 [V E10, E11].
- El nombre del personaje en el título de la ficha.

### b.4 Lo que la evidencia no dice

- **No hay ningún estudio controlado que aísle el efecto de un personaje** sobre la retención, el CTR o el IPM de un juego [NV E5]. Los datos que hay son de casos de agencia (Magic Sort: +49 % de IPM en Android con dos creativos de un gatito, uno de ellos en peligro [V J36]) o de proveedores. En una prueba de iconos de un juego de objetos ocultos (*Seekers Notes*), cambiar el hombre enmascarado del icono por una calabaza sonriente subió la conversión un 9,3 %, y en otra prueba una bruja sonriente ganó un 9,1 %; la muestra no se publicó [E E6]. El icono de control ya tenía personaje, así que esa prueba compara personajes entre sí y **no demuestra que poner un personaje ayude**.
- En publicidad general, alrededor del 41 % de las campañas con un personaje recurrente lograron ganancias de cuota muy grandes, frente a cerca del 30 % sin él (System1 e IPA, 2019; observacional y por fuentes secundarias [NV E3, E4]).
- Los agentes animados con personaje tienen un efecto **pequeño** pero significativo y funcionan mejor con texto en pantalla que con voz (metaanálisis de 43 estudios [V E8]).
- Los match-3 con narrativa de personajes crecieron en el top 100 de ingresos entre 2015 y 2020, pero «añadir palabras no crea una historia» (GameRefinery, proveedor comercial, datos de 2020 [V E1]).
- Varios análisis atribuyen la retención de Duolingo sobre todo a la racha, las ligas y las notificaciones: el búho es la voz de esos sistemas, no el motor [NV J34]. El canon ya cita un +14 % de retención al día 7 con la racha con perdón (06 §12, research/05 §3).
- **Conclusión:** TiTino hace más querible el juego y mejora la marca, pero no sustituye a la Crecida, la Bitácora ni las notificaciones. Se lanza detrás de interruptores y se mide (sección d.10).

---

## (c) TiTino

### c.1 Especie recomendada: perezoso de tres dedos de garganta marrón

| Especie | A favor | En contra | Mundos del juego [E] |
|---|---|---|---|
| **A. *Bradypus variegatus*** (garganta marrón; ES «perezoso de tres dedos», «perico ligero»; PT «preguiça-comum», «preguiça-de-óculos») **(recomendada)** | El más icónico: cara clara con **antifaz oscuro** y la «sonrisa» natural de la boca; tres garras largas. **Pelaje verdoso por las algas**, que encaja con la paleta de D-003. Activo de día y de noche [V B41, B47]. El más extendido: de Honduras a Bolivia y Brasil [V B14, B17] | Su cara no es la de Manaos (allí vive *B. tridactylus*, con mezcla al sur del río) [V B20, B21] | 7 de los 8 mundos analizados: Estuario, Tapajós, Trapecio, Loreto, Oriente, Madre de Dios y Madidi (falta en Manaos) |
| B. *Bradypus tridactylus* (garganta pálida) | Vive en Manaos (mundo 3) y nada bien | Solo al norte del Amazonas y en el Escudo Guayanés: no está en la mayoría de los mundos [V B19, B15] | Manaos y Escudo de Guayana |
| C. *Choloepus* (dos dedos) | Muy extendido | **Nocturno**, cara sin antifaz, menos reconocible; más «agresivo» según el Ipaam [V B16, B18, B21] | Casi todos |

**Por qué A:** se reconoce a 48 px, su verde de algas es un rasgo real y bonito, y vive en 7 de los 8 mundos analizados. Es además la especie del estudio de sueño en libertad (c.2). Pendiente de confirmar con el asesor la distribución por país [E].

**Sobre «oso»:** el perezoso **no es un oso**: es un xenartro, pariente del oso hormiguero y del armadillo [V B9]. En los textos del juego se dice «perezoso» («sloth», «preguiça»); «oso perezoso» solo aparece como dato curioso (g.3).

**Edad y sexo [P]:** TiTino es un **macho adulto joven**, no una cría. Una cría sola es la imagen típica del tráfico y del «huérfano rescatado» (g.1). La ternura sale de las proporciones (cabeza grande, ojos con brillo), no de la edad. El macho adulto de *B. variegatus* tiene en la espalda un **parche naranja con una raya negra** (espéculo) [V B14, B48]. Sale con la madurez sexual [V B48], y ninguna fuente da una edad exacta: ADW sitúa la madurez del macho entre los 3 y los 5 años [V B47] y la ficha de ICMBio, en unos 6 años [V B41]. En los machos jóvenes puede faltar o estar a medio formar, sea cual sea su edad o tamaño [V B41]. Así el espéculo marca a TiTino como adulto, lo que refuerza la regla «nunca una cría». Es un rasgo propio, visible cuando se gira, que ninguna otra mascota del juego tiene. Lo confirma el asesor.

### c.2 Personalidad

- **Tierno, tranquilo y optimista.** Paciente y observador. Celebra el orden y el esfuerzo, nunca la velocidad.
- **Su lentitud es una virtud, no pereza.** Tiene el metabolismo más bajo medido en un mamífero que no hiberna: ir despacio es ahorrar energía [V B22, B9]. Antes de pasar a otra rama, prueba si aguanta [V B9]. De ahí sus lemas: **«sin afán»**, **«paso a paso»** y **«con tino»** (piensa antes de verter).
- **Datos reales que le dan carácter** (todos pendientes del asesor):
  - en libertad duerme unas 9,6 h al día, no 15-20 [V B12, B13] (medido con EEG en 3 hembras adultas en Panamá durante 12,8 días; los autores no descartan que la edad explique parte de la diferencia con el cautiverio; la SCF da 8-10 h [V B10]);
  - en su pelo viven algas y polillas, que lo tiñen de verde [V B23, B24];
  - nada bien y cruza ríos [V B25];
  - gira la cabeza unos 270° sin mover el cuerpo [E B26, B11];
  - probablemente toma el sol para calentarse: un estudio con sensores en perezosos de tres dedos amazónicos (*B. tridactylus*) lo propone como explicación de su temperatura [E B44]. Hacerlo «con la barriga al aire» es licencia creativa [P];
  - probablemente ve sin colores (monocromacia de bastones) [V B27]. Es un gancho tierno: TiTino no ve los colores como tú, pero los «siente» (frase 3 de e.4, que solo se usa si el asesor aprueba el dato_11 de g.3), y une al personaje con los **Patrones de fruta** de accesibilidad (02 §12) [P].
- **Su humor** nace de la calma, nunca del estereotipo de «vago» ni del chiste de hablar lentísimo (Flash, de Zootopía) [V B35]. Se aleja también de Sid (*Ice Age*) y de Slakoth (Pokémon, «duerme más de 20 h») [V B36].
- **Su relación con el jugador:** amigo y vecino del río. No es tuyo, no lo cuidas ni lo alimentas. Él te mira jugar desde su árbol y se alegra contigo.

### c.3 Su porqué, en una línea [P]

Royal Match lo resuelve con una frase («el rey necesita tu ayuda») y Homescapes con una escena. Para TiTino, sin inventar una historia que choque con el canon:

> **«TiTino vive en su árbol, en la desembocadura, y nunca ha visto el origen del río. Tú subes el río, mundo a mundo, y él lo sigue desde su rama, celebrando cada paso.»**

TiTino **no viaja**: un perezoso de su especie se mueve de media unos 40 m al día [V B45]. Acompaña al jugador desde la interfaz (inicio, victoria, ánimo) y desde su árbol. Así no hace falta que viva en mundos donde su especie no está. Los que viajan por el río siguen siendo Rosa y el jugador.

### c.4 Cómo se ve

- **D-003, «Selva brillante premium»:** look de juego top, vidrio, madera y luz; nada de papel ni acuarela (`docs/art/direccion-visual-v2.md`).
- **El estilo de la fauna del juego** (07 §6.1 y §6.4; guion de la intro §12): contorno de 2 px de tinta (#2B2118) al 80 %, ojos con brillo, **postura animal**, saturación por debajo de los 14 tintes, luz a 35° desde arriba a la izquierda.
- **Siempre en su árbol:** colgado de una rama de **embaúba** (*Cecropia*; yarumo o guarumo en otros países). En Brasil se la llama «árvore-da-preguiça», el árbol del perezoso [V B28, B29]. Cuánto pesa en su dieta está en discusión: una página de la UFMG dice que el nombre viene de que le gustan mucho sus brotes [V B28] y ADW, que come sobre todo *Cecropia* [V B47]; la ficha de ICMBio, en cambio, dice que come sobre todo hojas y brotes de Moráceas y otras familias, y que la asociación con la embaúba quizá se explique porque en su copa abierta se le ve con facilidad [V B41]. Para el juego basta con el nombre popular [E]. **Nunca la sumaúma ni la lupuna**, que son sagradas (07 §6.4; guion §4.6). **El asesor confirma también la embaúba** (g.2, punto 1): en la Amazonia colombiana la ceniza de hojas de yarumo se mezcla con la coca para hacer el mambe, de uso ceremonial [E B50]. Mientras tanto, en el encargo al artista: nada de hojas quemadas ni ceniza, y una alternativa preparada (una rama genérica sin especie reconocible, o la que proponga el asesor). Cuando entra en una pantalla, **su rama entra con él**: nunca cuelga de un botón, de una cuia ni de una tarjeta.
- **Sin ropa, gorro, collar, pañal, accesorios ni pintura** (urucum o huito).
- **Paleta orientativa [P]** (la fija el artista; se distingue del pardo de Capi y del rosa de Rosa):

  | Parte | HEX orientativo |
  |---|---|
  | Pelaje base, gris pardo | #9A8F7A |
  | Velo de algas en lomo y hombros (20-30 %) | #8C9C6A |
  | Cara | #E3D8C2 |
  | Antifaz | #4B3A2C |
  | Nariz y contorno | #2B2118 |
  | Garras, hueso claro | #CDBFA3 |
  | Espéculo del macho, con raya central | #E39A3B y #2B2118 |

- **Tamaños en pantalla [P]:** arranque 140-180 dp; inicio 120-160 dp; victoria 72 dp con bocadillo, 48 dp solo con el gesto o nada, según el hueco que quede (d.4); Nivel fallido 64 dp (d.5); retrato de aviso 40 dp; Mapa del río 64-96 dp. **Nunca en el tablero durante la partida** (como Magic Sort; 07 §6.1).

### c.5 Rasgos para el artista

1. Cabeza redonda y grande (≈ 1/3 del alto colgado), orejas **casi invisibles** bajo el pelo: nada de orejas redondas ni hocico de oso.
2. Cara clara con **antifaz oscuro** alrededor de los ojos; ojos pequeños con un brillo, que miran al jugador o al río (nunca a una cuia, D-002).
3. La «sonrisa» es **la línea natural de la boca**: sin labios, dientes ni sonrisa humana. Las emociones se leen en los ojos, la cabeza y el cuerpo.
4. **Tres garras** largas y curvas en cada extremidad, redondeadas y claras: que no den miedo, pero que estén (es un tres dedos).
5. Pelo largo y áspero, gris pardo con **velo verde de algas**; si se quiere un chiste visual, una **polilla parda diminuta** que sale del pelo de vez en cuando (es real [V B23]). Nunca azul: la mariposa azul es la Morpho, el comodín de la pista (glosario §5.1).
6. Espéculo naranja con raya negra en la espalda, completo, porque TiTino es un macho adulto (en los jóvenes puede faltar o estar a medio formar [V B41]).
7. **Postura de perezoso:** colgado boca abajo o sentado en una horquilla, abrazado a la rama. Nunca de pie como una persona, nunca aplaudiendo ni con los brazos en jarras.
8. **El brazo estirado hacia la cámara está prohibido como saludo o abrazo:** es la señal de miedo de un perezoso cuando alguien está demasiado cerca [V B7]. Su saludo es **levantar despacio una garra desde la rama** o girar la cabeza hacia ti.
9. Siluetas legibles a 40 dp y de espaldas (espéculo); contraste suficiente sobre la selva clara y sobre el panel azul petróleo (D-004).

### c.6 Poses y animaciones [P]

Los ID de las poses y de las claves son neutros (`sloth_*`), como pide la regla de identificadores sin marca (glosario §11): si el nombre cambia, el código no se toca. Las ranuras de arte siguen el patrón de fauna del glosario §11 (`fauna_<id>_partes`, `fauna_<id>_retrato`; c.7); si se prefiere `sloth_*` también para el arte, hay que registrarlo como excepción de personaje en el glosario §11. Las duraciones encajan en los tiempos de D-008 y de 07 §5.4: **ninguna retrasa un botón ni bloquea el toque**. Con «Reducir movimiento» (07 §7.8), cada pose se sustituye por su fotograma de reposo con un fundido de 150 ms.

| ID | Qué hace | Dónde | Duración |
|---|---|---|---|
| `sloth_idle` | Cuelga de su rama, respira, parpadea cada 3-6 s y gira la cabeza muy despacio cada 15-30 s | Inicio, Mapa del río | Bucle de ≈ 3 s |
| `sloth_wake` | Abre los ojos, estira un brazo hacia la rama de arriba y gira la cabeza hacia ti | Arranque, solo la primera vez | ≤ 2,5 s; visible al menos 1,0 s [P] si el primer toque sigue por debajo de 20 s; si no, cortada cuando termina la carga (d.3) |
| `sloth_wave` | Levanta despacio una garra desde la rama (no hacia la cámara) | Regreso y día nuevo en el inicio; tutorial nivel 1 | 0,8 s |
| `sloth_present` | Ladea la cabeza hacia la instrucción y la señala con la garra | Tutorial 1-3, 6 y 12 | 0,6 s + bucle de reposo |
| `sloth_cheer` | Levanta las tres garras despacio; caen 3-4 hojas, que son **piezas del rig** animadas por código, no partículas | Victoria | Entrada 0,3 s + bucle de 1,2 s |
| `sloth_cheer_big` | Igual, más una vuelta colgado de una mano y 2 hojas más, también piezas del rig | Victoria de fin de mundo, primer Difícil y primer Muy difícil (d.6) | Entrada 0,3 s + 1,2 s |
| `sloth_encourage` | Se balancea suave en la rama y asiente despacio, mirándote | Nivel fallido, Volver a la orilla | 0,8 s + reposo |
| `sloth_look` | Gira la cabeza casi tres cuartos de vuelta y vuelve (el gesto real) | Al tocarlo en el inicio; «¿Recuerdas?» del regreso | 1,0 s |
| `sloth_sunbathe` | Al sol en la copa [E B44]; la barriga al aire es licencia creativa [P] | Inicio y Mapa del río de día | Bucle |
| `sloth_sleep` | Hecho un ovillo en una horquilla | Inicio de noche (hora local) y espera de más de 30 s en el Mapa | Bucle |
| `sloth_swim` (opcional) | Cruza el río nadando, solo la cabeza fuera | Vídeo de ficha, anuncios, evento del Día del Perezoso | 1,5 s |

**Nunca se anima:** la bajada al suelo para defecar (real, pero no va en el juego), un brazo estirado hacia la cámara, llorar, caerse, estar herido o atrapado.

**Presupuesto de partículas:** las hojas de TiTino no cuentan como partículas porque son piezas del rig. El estallido de la victoria ya usa las 60 que permite 07 §1.2 (24 plumas, 24 hojas y 12 pétalos; `direccion-visual-v2.md` §6.9). Si al final se hacen como partículas, se descuentan del estallido (por ejemplo, 24 → 20 hojas).

### c.7 Lista de piezas

| Pieza | Ranura [P] | Tamaño | Notas |
|---|---|---|---|
| Hoja de modelo | `docs/art/` (no va en el build) | — | Frente, perfil, 3/4, espalda (espéculo), colgado y sentado; 6 expresiones (calma, alegría, ánimo, curiosidad, sueño, sorpresa suave). Pasa por el asesor antes del arte final |
| Piezas del rig | `fauna_bradypus_variegatus_partes` | 1024 × 1024 (rejilla de 4 × 4 celdas de 256 px) | Mismo método que los miquitos (07 §6.4): el artista dibuja piezas sobre una plantilla con pivotes fijos y el código anima las poses. Incluye las 6 hojas de `sloth_cheer` |
| Retrato | `fauna_bradypus_variegatus_retrato` | 512 × 512 | Aviso del tutorial, bocadillos, icono grande de las notificaciones |
| Rama de embaúba | `arbol_embauba` | 1024 × 256 | Entra con él en cada pantalla. No usa el patrón `rama_<mundo>`, que es de los mundos |
| Bocadillo de texto | Pieza del UI kit | 9-slice | Vidrio esmerilado con borde de madera clara (D-003), nunca papel; texto a 14 sp, ≤ 2 líneas con un ancho máximo fijo, que crece hacia arriba y nunca baja de 12 sp (d.4) |
| Fondo del inicio con su árbol | Capa del panorama del inicio | — | Su embaúba en un lado, sin tapar el botón «Jugar» |
| Arte de marca | `key_art_sloth` | 1080 × 1920 y 1024 × 500 | Ficha, capturas, concepto D del icono (d.9) |
| Sonidos | `sfx_sloth_*` | — | 3-5 sonidos originales (e.5) |

**Coste orientativo [E]:** 07 §11 da 14 días de artista para 18 animaciones de mascotas (Capi 12 y Rosa 6). Con la opción A (sección f), Capi baja a 6 poses y TiTino suma 10 (+1 opcional): unas 22 animaciones y una hoja de modelo nueva, **≈ +5-6 días de artista** en total.

---

## (d) Su papel y dónde aparece

### d.1 El papel, en una línea

**TiTino es la cara del juego y tu fan número uno en el río** (lema propio [P]): sale en el icono, la ficha y los anuncios; es el anfitrión del arranque y del inicio; felicita, anima y firma las notificaciones.

**No es:** una mascota que se cuida, un vendedor, un asistente que interrumpe ni un guía del tablero.

**Regla de la partida [P] (pregunta T5):** en el tablero, **nunca** (como Magic Sort; D-002). Durante la partida, **solo** en dos casos: el retrato que acompaña la instrucción de los niveles de tutorial 1-3, 6 y 12, y el aviso tras Volver a la orilla en los niveles 1-30. En los dos va fuera del panel del tablero, con `raycastTarget = false`, y nunca se queda fijo en el muelle (el conflicto C4 sigue abierto; f.3). El aviso dura ≤ 2 s; el retrato del tutorial dura lo que dure la instrucción (02 §13).

### d.2 Momento a momento

| Momento | Qué hace TiTino | Cuándo | Reglas |
|---|---|---|---|
| Arranque | `sloth_wake` (primera vez) o `sloth_idle` en la pantalla de río y logotipo, **que aún no existe en el código** | Cada arranque en frío | La primera vez, visible al menos 1,0 s [P] si el primer toque sigue por debajo de 20 s (09); las demás, se corta cuando termina la carga. Sin texto (06 §2.1, regla 3) |
| Llenar y teñir | **No aparece** | — | Es de los miquitos (D-010); tiempos cerrados de 1,4 s y 0,8 s |
| Tutorial 1-3, 6 y 12 | Dice la instrucción que ya prevé el canon, con `sloth_present` y su retrato | Tras la intro, cuando la tropa ya salió (guion §14) | Una instrucción por nivel, ≤ 2 líneas (02 §13). Sobre la franja del panorama si mide ≥ 64 dp; si no, sobre el muelle, como la tarjeta del miquito (07 §6.4). En el 8 habla Capi (trae la cuia). Pregunta T5 |
| Partida | **En el tablero, nunca.** Solo el retrato del tutorial y el aviso de Volver a la orilla | — | Magic Sort no pone personaje en el tablero; D-002. Regla de d.1 |
| Victoria | `sloth_cheer`; frase en ≈ 1 de cada 4 | 0,60 s tras el último sellado | Solo si cabe (d.4); una sola línea de texto por victoria (d.6) |
| Aguas estancadas, ¿Seguro?, Sin canoas | **No aparece** | — | Tienen oferta de pago; Aguas estancadas va sin ilustración (D-008) |
| Nivel fallido | `sloth_encourage` junto a la cesta vacía y una frase | Al abrir la tarjeta | d.5 |
| Volver a la orilla (1-30) | Aviso corto con su retrato y una frase | Tras la intro corta del reinicio | Es una tarjeta de la familia del miquito y entra en el orden del guion §14 (d.5) |
| El río bajó | **No aparece**: es de Rosa (07 §5.5) | — | Un animal por momento |
| Hitos | `sloth_cheer_big` y frase segura | Solo en la victoria de fin de mundo (salvo el nivel 10), del primer Difícil (40) y del primer Muy difícil (60) | Los demás hitos entran en el 1 de cada 4; en los de la Crecida, las nutrias son las protagonistas (d.6) |
| Inicio | Vive allí: reposo, sol o sueño según la hora; saludo al volver | Siempre | d.7 |
| Mapa del río | Sustituye al perezoso de espera y a `capi_sleep` | Espera de más de 30 s | d.7 |
| Notificaciones | Firma los mensajes y pone su retrato | Reglas de 06 §8 | d.8 |
| Ficha y anuncios | Icono (experimento), capturas, vídeo, creativos | Cuando haya arte final | d.9 |

### d.3 La introducción y «Llenar y teñir»

El dueño quiere que TiTino «sea parte de la introducción». En el juego hay dos «introducciones», y las dos tienen reglas cerradas:

- **El arranque** (06 §2.1-§2.2): splash de una pantalla (río y logotipo), la pantalla de edad y consentimiento y la carga del nivel 1, en 0:00-0:25. Regla dura: **sin pantallas de texto en el arranque**. Objetivo de 09: **< 20 s** hasta el primer toque en el nivel 1. **El splash todavía no existe en el código** (en `Assets/_Project/Scripts` solo hay salpicaduras del vertido): hay que construirlo antes de poner a TiTino en él.
- **«Llenar y teñir»** (D-010): la carga de cada nivel, 1,4 s en los niveles 1-20 y 0,8 s después, protagonizada por los miquitos, con una función por animal (solo los miquitos sueltan fruta). Además, el perezoso come sobre todo hojas y brotes de muchas especies [V B14, B41] (ADW incluye también flores y frutos [V B47]); no es un animal que suelte fruta en el río, así que no podría teñir sin traicionar el criterio de la propuesta de fauna («come fruta de verdad», §b.1).

**Propuesta [P] (opción A de T2):**

1. **En el arranque, cada vez:** TiTino cuelga de su embaúba sobre el río, junto al logotipo. La primera vez, `sloth_wake`: abre los ojos, se estira y te mira. **Sin palabras.** Riesgo: en un teléfono rápido, como el de pruebas, la carga puede terminar antes de que se vea. Por eso, **solo en la primera apertura**, la animación se ve al menos 1,0 s [P] dentro del tramo 0:00-0:25 de 06 §2.2, siempre que el primer toque en el nivel 1 siga por debajo de 20 s (KPI de 09); se mide en el teléfono (paso 6d). En las demás aperturas, se corta cuando termina la carga. Si ese segundo no cabe, TiTino puede no verse en el arranque.
2. **En los niveles 1-3, 6 y 12**, tras la intro y cuando los miquitos ya se fueron, acompaña la instrucción del tutorial con su retrato: sobre la franja del panorama si mide ≥ 64 dp; si no, sobre el muelle, como la tarjeta del miquito (07 §6.4). Es una aparición durante la partida, así que depende de la pregunta T5; no se queda fija en el muelle (C4).
3. **«Llenar y teñir» no cambia.** Los miquitos siguen sin hablar; quien «presenta» pasa a ser TiTino en el tutorial (guion §4.6 dice hoy «Presenta Capi»).

**Opción B (prueba opcional):** además, un **prólogo de ≤ 8 s solo en la primera apertura**, sin palabras y saltable desde el primer toque. Tres planos: amanece en la desembocadura; TiTino despierta en su embaúba y mira el río; pasa la tropa de miquitos con fruta, cae una gota de color y TiTino te mira; corte al nivel 1. Riesgo: suma hasta 8 s al primer toque, que el plan ya sitúa en unos 25 s con el consentimiento (06 §2.2) frente al objetivo de < 20 s (09). Solo como experimento en el soft launch, apagado por defecto (`ui_sloth_prologue_enabled` [P]), midiendo el tiempo al primer toque y D1. Homescapes y Township usan una escena así [V J17, J22]; los mejores arranques ponen a jugar cuanto antes [V E2].

**Opción C (no recomendada):** TiTino dentro de «Llenar y teñir» en cada nivel. Rompe los tiempos cerrados y la regla de una función por animal, y compite con los miquitos por la misma franja.

### d.4 Victoria

Hoy la victoria sigue este orden (07 §5.4; `direccion-visual-v2.md` §6.9): a los 0,30 s saltan las cuias; a los 0,50 s, confeti de guacamayo; **a los 0,60 s entra la mascota**; a los 0,80 s sube el panel; a los 1,40 s sale «Siguiente», sin auto-avance (`victory_autonext_s` = 0).

TiTino ocupa el hueco de los 0,60 s que hoy es de Capi [P].

**Cuánto sitio hay (medido en el código) [V repo]:** el panel de victoria va anclado abajo y mide 218 dp de alto (286 con Cosecha doble, desde el 11). La cinta-hoja mide 344 × 72 dp, en una pantalla de 360 dp de ancho, y asoma 38 dp por encima del panel. `ShiftToClear` sube el tablero hasta 8 dp de la cinta, con tope en la cabecera (`GameScreen.cs`); con 3 filas de cuias no queda franja libre (07 §5.1). El panel lleva además la Cosecha, el saldo y el cronómetro de canoas (04 §7.1) y la barra de la Crecida en grande. **Encima del panel no hay hueco** para TiTino ni para un bocadillo de una línea: 70 caracteres a 14 sp ocupan unos 500 dp.

**Regla de colocación [P]:**
- **La caja de TiTino** es la franja libre que queda entre el borde inferior de la cabecera y lo más alto de las cuias (con su elevación), una vez aplicado `ShiftToClear`, sin tocar la barra de la Crecida:
  - si mide ≥ 104 dp [P, a confirmar con capturas]: TiTino a 72 dp a la izquierda y el bocadillo a su derecha;
  - si mide entre 56 y 103 dp: solo el gesto, a 48 dp, sin bocadillo (la frase de esa victoria se pierde; no se aplaza);
  - si mide menos de 56 dp (por ejemplo, con 3 filas): TiTino no sale en esa victoria.
- **El bocadillo** tiene un ancho máximo fijo de 240 dp [P] (360 − 2 × 16 de margen − 72 de TiTino − 16 de separación): texto a 14 sp, ≤ 2 líneas, crece hacia arriba y nunca baja de 12 sp (02 §12). Con la escala de fuente del 130 % puede necesitar 3 líneas; por eso la caja completa pide 104 dp.
- **Nunca se cruza** con la cinta, las cuias (incluida su elevación), el saldo, la barra de la Crecida, el Dato del río, la Cosecha, «Cosecha doble», «Siguiente» ni «Inicio». Capas con `raycastTarget = false`. Lo comprueba la prueba del paso 6a (sección i).

**Cómo entra:**
- **Entrada:** su rama entra desde el borde izquierdo en 0,3 s y él ya cuelga de ella (no camina).
- **Gesto:** `sloth_cheer` en cada victoria en la que cabe; `sloth_cheer_big` en los tres hitos con frase segura (d.6).
- **Frase:** un bocadillo de ≤ 50 caracteres en ES (e.2) que aparece a los 1,10 s, con el panel ya quieto, y se desvanece a los 4 s o con el toque en «Siguiente». En ≈ 1 de cada 4 victorias (d.6), y nunca en una victoria que ya lleve otra línea de texto (Dato del río o línea de la serie de frutas; pregunta T6).
- **No cambia nada del ritmo:** «Siguiente» sigue saliendo a los 1,40 s; no hay auto-avance; no cuenta como takeover (05 §5); no toca AD_GATE.
- **Nunca mira, señala ni menciona** «Cosecha doble · Ver video» ni ninguna oferta. En el nivel 11, donde hoy el canon hace que Capi presente la Cosecha doble una sola vez (06 §2.2), **no la presenta ningún personaje**: basta un tooltip sin personaje [P] (f.3).

Es un **añadido sobre Magic Sort**, que no pone gatos en la victoria [V ficha §4.6]. Se copia de Royal Match, Candy Crush y Duolingo, que sí reaccionan, y lo pidió el dueño.

### d.5 Derrota y ánimo

D-008 copia el embudo de fallo de Magic Sort, que monetiza en ese segundo: según Naavik, la monetización al fallar (pagar por continuar el nivel, por ejemplo para no perder la racha) genera la mayoría del IAP de Magic Sort [E J46]; Braberg lo llama «el motor de monetización», sin dar porcentaje [E J47]. (`docs/research/09-referencia-magic-sort.md` §1, punto 2, dice «más del 50 % [V]»: es la misma estimación, sobremarcada; se corrige en el lote de documentación). Por eso **TiTino no entra en ninguna pantalla con oferta de pago** y no retrasa ninguna:

| Pantalla (D-008) | ¿TiTino? | Por qué |
|---|---|---|
| Aguas estancadas | No | Va sin ilustración para que se vea qué deshacer (02 §7; 04 §6.6), y ofrece Continuar desde el 31 |
| ¿Seguro? | No | Vuelve a ofrecer Continuar; la imagen es la canoa volcada |
| **Nivel fallido** | **Sí** | No tiene oferta de pago. TiTino cuelga de su rama **al lado de la cesta vacía**, que no se quita: la cesta enseña lo que se pierde, como el corazón roto de Magic Sort. `sloth_encourage` y una frase. «Reintentar» sale a la vez que hoy. **Caja [P], medida en `LevelFailedScreen.cs`:** la tarjeta mide 328 dp de ancho con la cesta de 120 dp centrada, así que quedan unos 88 dp por lado; TiTino va a 64 dp en el hueco izquierdo, sin tocar la cesta. **La frase no cabe al lado:** va como línea de la propia tarjeta, bajo la cesta, con el ancho útil de 296 dp, 14 sp, ≤ 2 líneas y nunca menos de 12 sp; la tarjeta crece en alto. Si con la escala del 130 % la tarjeta ya no cabe por encima de `safeBottom`, solo el gesto, sin frase |
| **Volver a la orilla** (1-30, reinicio gratis tras el bloqueo) | **Sí** | Sustituye a `capi_shrug`. Un aviso de 2,0 s con su retrato de 40 dp y una frase, tras la intro corta del reinicio, sin capturar toques; sobre la franja del panorama si mide ≥ 64 dp y, si no, sobre el muelle (misma regla que la tarjeta del miquito, 07 §6.4). **Es una tarjeta de la familia del miquito**, así que entra en el orden del guion §14 («como mucho una» tras la intro): si el nivel tiene instrucción del tutorial, manda la instrucción (TiTino ya está en ella con su retrato) y no hay aviso aparte; si no, el aviso es el único elemento. Es una aparición durante la partida: depende de la pregunta T5 |
| El río bajó | No | Es de Rosa, que asoma y exhala (07 §5.5) |
| Sin canoas | No | Pantalla de pago |

**Topes en el fallo:** frase solo en el **primer** fallo de cada nivel; en los siguientes, solo el gesto (evita repetirse en bucle de reintentos, el efecto novedad de Duolingo [V J30]). Nunca llora, nunca dice «perdiste», nunca culpa. Cuando un fallo duro rompe la Crecida, la frase es de consuelo (e.4, n.º 21). Eso pasa en Nivel fallido desde el 31 y también en los niveles 25-30, donde Volver a la orilla desde Aguas estancadas es un fallo duro y rompe la Crecida (04 §5.2); en ese caso el aviso de Volver a la orilla usa la n.º 21.

### d.6 Hitos, frecuencia real y convivencia con otros textos

**Por qué cambia [E]:** la primera versión ponía frase «siempre en los hitos» y contaba como hitos todo Difícil o Muy difícil, los hitos 3, 6 y 10 de la Crecida, cada estreno, cada fin de mundo y cada serie de fruta. Con el ciclo de 03 §12 eso no es «de vez en cuando». Cuenta orientativa por cada 100 victorias en los niveles 61-200, para un jugador que falla poco (hay solapes entre filas):

| Motivo de frase en la primera versión | Victorias por cada 100 | Base |
|---|---|---|
| Difícil o Muy difícil | ≈ 20-25 | 03 §11 (15-20 % y 5 %) y §12 (desde el 61, los 5 retos del ciclo apuntan a Difícil y el hito a Muy difícil) |
| Hitos de la Crecida (3, 6 y 10) | Hasta 30 sin ningún fallo; ≈ 15-25 con los fallos de los niveles difíciles | Glosario §6.3 |
| Estrenos (mecánica, fruta, primer nivel de mundo) | ≈ 5-10 | 02 §9.1 (43, 63, 83…); glosario §9 |
| Fin de mundo y serie de fruta | ≈ 2 y ≈ 5, casi siempre sobre el hito del ciclo | 03 §11; propuesta de frutas |
| El resto, 1 de cada 4 | ≈ 10-15 | — |
| **Total** | **≈ 50-65 de cada 100**, antes del tope de 4 por sesión | — |

**Regla nueva [P]:**

1. **Frase segura solo en tres hitos:** fin de mundo (último nivel de cada mundo: 40, 80, 140…), primer Difícil (40) y primer Muy difícil (60). **El nivel 10 es la excepción:** su secuencia de 06 §2.3 (≤ 30 s) ya trae el primer Dato del río y el Mapa, que son de Rosa, así que TiTino solo hace el gesto.
2. **Todo lo demás entra en el 1 de cada 4:** cuando toca frase (la victoria número N, 2N, 3N… con `ui_sloth_win_line_every_n` = 4), se elige el banco más específico que aplique, por este orden: Difícil o Muy difícil (`hard`) → estreno (`debut`) → Crecida de 4 o más fuera de hito (`streak`) → victoria normal (`win`).
3. **Un solo animal protagonista por victoria** (el que tiene frase o sonido propio):
   - en los hitos de la Crecida, las nutrias (chillido; glosario §3.3); TiTino solo hace su gesto normal, sin frase;
   - en la serie de fruta completa, la línea de cierre de la serie («¡Ya conoces el açaí!»); TiTino solo hace el gesto;
   - en fin de mundo, TiTino. El miquito que la propuesta de fauna (c.3) pone en las victorias de cada 10 niveles «mientras entra Capi» no sale en fin de mundo; en las otras victorias múltiplo de 10 puede salir, y TiTino solo hace el gesto.
   - El confeti de guacamayo es ambiente, no protagonista (07 §5.4).
4. **Una sola línea de texto por victoria.** El canon ya pone el Dato del río en la victoria cada 10 niveles (02 §6), y todos los fines de mundo son múltiplos de 10; la propuesta de frutas pondría una línea en cada victoria (§1.1 y §1.3, opción A). Regla por defecto [P], pendiente de la pregunta T6: **manda el dato y TiTino solo hace el gesto**, salvo en los tres hitos del punto 1, en los que habla TiTino y el dato de ese nivel pasa a la victoria siguiente. Hay que responder T6 a la vez que la propuesta de frutas.
5. **Orden de las reglas cuando chocan:** tope de 4 frases por sesión > nunca frase en dos pantallas seguidas > una sola línea por victoria > hito o 1 de cada 4. **Única excepción:** el fin de mundo (salvo el nivel 10) habla siempre, aunque se haya llegado al tope o la pantalla anterior tuviera frase (≈ 1 vez cada 40-60 niveles).

**Tasa con la regla nueva [E]:** ≈ 25 frases por cada 100 victorias como máximo (1 de cada 4, más 2-3 hitos seguros, menos las que caen en una victoria con Dato del río), y menos en los niveles en los que TiTino no cabe en la victoria (d.4). **Ojo:** desde 11 cuias el tablero va en 3 filas (D-006) y la victoria no deja hueco; en los niveles 61-200 (10-13 cuias, 03 §11) eso puede ser buena parte de las victorias, en las que TiTino no saldría. Hay que medirlo con las capturas del paso 6a antes de dar la regla por buena.

**Tarjetas:** TiTino solo añade una, el aviso tras Volver a la orilla (d.5), que entra en el orden del guion §14. Por lo demás, una tarjeta por nivel (tutorial → Fruta nueva → miquito, 07 §6.4) y nunca dos novedades en el mismo nivel (traspaso §6, paso 3). Sus frases de estreno van en la **victoria** del nivel del estreno, después de jugarlo.

### d.7 Inicio y Mapa del río

- **Inicio (lobby):** hoy es el Estuario con una garza y sin personajes (`LobbyScreen.cs`). TiTino vive allí, en su embaúba a un lado, como los gatos de Magic Sort en su escenario [V ficha §7.1]. **Nunca tapa el botón «Jugar»** ni los saldos.
  - De día, `sloth_idle` o `sloth_sunbathe`; de noche (hora local), `sloth_sleep`.
  - Al volver tras ≥ 3 días: `sloth_wave` y una frase de regreso, como mucho 1 al día.
  - Primera apertura del día entre las 06:00 y las 12:00 (hora local): `sloth_wave` y, si no hubo saludo de regreso ese día, el saludo de la mañana (e.4, n.º 25), como mucho 1 al día.
  - Al tocarlo: `sloth_look` (gira la cabeza y te mira). Como mucho una vez por sesión, además, un **«Dato del perezoso»** (g.3). Tocarlo no lo «cuida» ni da premios: no hay bucle de mascota virtual.
- **Mapa del río:** TiTino sustituye al perezoso de espera del canon (glosario §3.3) y a `capi_sleep` (07 §6.1). Así el Mapa pasa de tres personajes (Capi, Rosa y el perezoso) a dos: Rosa nada y guía; TiTino toma el sol o dormita en la primera parada.

### d.8 Notificaciones

El canon ya fija el plan (06 §8): todas locales en la 1.0, pre-prompt tras la primera victoria de la 2.ª sesión con el nivel 10 superado, máximo 1 al día en D0-D7, horas silenciosas de 22:00 a 09:00, mensajes que nombran algo real y silencio a los 60 días. **Hoy los firma Capi.**

Propuesta [P]:
- **TiTino es la voz** de los mensajes y del pre-prompt («¿Te avisamos cuando tu Reto del río esté listo?»). Capi solo se nombra cuando trae algo (el regalo de Cuia extra del regreso).
- **Plantillas que rotan:** 3-5 variantes por tipo, sin repetir la última enviada (Duolingo [V J30]).
- **Retrato de TiTino** como icono grande del aviso.
- **Despedida honesta** en el último aviso (60 días), atada a algo real como exige 06 §8.2 («Estado real»: cada mensaje nombra algo que existe ahora) y 06 §9 (si hay contenido nuevo, el aviso lo nombra): «Tu nivel {n} sigue aquí. No te escribo más; cuando quieras, aquí estaré, en mi rama.» o, si hay contenido nuevo, «{novedad} ya llegó al río. No te escribo más; aquí estaré, en mi rama.» Sin culpa ni pasivo-agresividad (una usuaria de Duolingo recibió un aviso así [E J32]).
- Ejemplos sobre el catálogo de 06 §8.3: «El Reto del río de hoy ya está listo. TiTino te espera en su rama.»

**Requisito:** las notificaciones exigen el paquete de notificaciones locales (Unity Mobile Notifications, que prevé 08) y el permiso `POST_NOTIFICATIONS` de Android 13+. Las dos cosas son **nuevas y necesitan tu aprobación** (regla dura 4 de `CLAUDE.md`), aunque el GDD ya las prevé. TiTino no cambia nada de eso.

### d.9 Anuncios, icono y ficha

- **Creativos de UA [P]:** «TiTino te anima»: un tablero casi resuelto falla, TiTino asiente («Casi. Respira y otra vez»), se reintenta el mismo tablero y se resuelve; cierre con TiTino celebrando. Solo con escenas que existen en el juego (prohibición de la ASA [V J20]). Se prueba contra el concepto 1 del canon, «Capi trae una cuia» (07 §10.4).
- **Nunca** TiTino en peligro, herido, atrapado o triste para pedir ayuda, aunque funcione: en Magic Sort, dos creativos con un gatito (darle leche y rescatarlo de una botella en la que está atrapado) subieron el IPM de Android un 49 % en un mes [V J36, caso de la agencia], y el rey en apuros es el gancho de Royal Match [V J7, J10]. Es justo lo que TiTino se niega a hacer por ética: trivializaría el sufrimiento real del perezoso (g.1).
- **Icono:** añadir un **concepto D, «TiTino en su rama sobre una cuia que vierte»**, al experimento de ficha previsto (07 §10.1: A, cuia que vierte, sigue de control). La cara de frente con el antifaz debe leerse a 48 px [E].
- **Capturas y vídeo:** la captura 7 pasa a «TiTino te acompaña» / «Meet TiTino» / «TiTino te acompanha»; el vídeo de 30 s cierra con TiTino celebrando (07 §10.2-§10.3).
- **Nunca en el título de la ficha:** manda el descriptor ASO (glosario §1).
- **Nada con arte provisional:** ni vídeo, ni capturas, ni anuncios (igual que con los miquitos, 07 §6.4).

### d.10 Frecuencia y topes (para que nunca canse)

| Regla [P] | Valor | Por qué |
|---|---|---|
| Gesto en la victoria | Siempre que cabe (d.4); no bloquea el toque ni retrasa un botón | Om Nom, Candy Crush |
| Frase en victoria normal | 1 de cada 4 (`ui_sloth_win_line_every_n` = 4), con el banco más específico (d.6, punto 2) | «De vez en cuando», como pidió el dueño; efecto novedad |
| Frase en hito | Solo fin de mundo (siempre, salvo el nivel 10), primer Difícil y primer Muy difícil (d.6, punto 1) | Momento que merece palabras |
| Texto en la victoria | Una sola línea: si hay Dato del río o línea de la serie, TiTino solo hace el gesto (d.6, punto 4; pregunta T6) | «Cadena corta» de D-008 |
| Frase en Nivel fallido | Solo el primer fallo de cada nivel | No repetirse en los reintentos |
| Frase al Volver a la orilla | Solo el primer reinicio de cada nivel, y solo si el nivel no tiene instrucción del tutorial (d.5) | Ídem; guion §14 |
| Saludo de regreso en el inicio | Como mucho 1 al día, al volver tras ≥ 3 días | Duolingo: 1 recordatorio al día |
| Saludo de la mañana (n.º 25) | Primera apertura del día entre las 06:00 y las 12:00 (hora local); como mucho 1 al día y nunca el mismo día que el saludo de regreso | De noche TiTino duerme (d.7) |
| Dato del perezoso al tocarlo | Como mucho 1 por sesión | Que siga siendo una sorpresa |
| Total de frases | Como mucho 4 por sesión (`ui_sloth_lines_per_session_max` = 4); nunca en dos pantallas seguidas. Orden cuando chocan: d.6, punto 5 | Clippy |
| Repetición | Nunca la misma frase que la última vista del mismo momento (ver «Estado nuevo») | Duolingo |
| Ajuste «Mensajes de TiTino» | Activado por defecto; apagado = solo gestos | Duolingo permite apagar los mensajes motivacionales, según terceros [NV J44] |
| Reducir movimiento | Sin entrada ni bucles: pose quieta y fundido de 150 ms | 07 §7.8 |
| Interruptor general | `ui_sloth_enabled` = true | Poder apagarlo en remoto |

**Estado nuevo [P]** (la primera versión decía «sin estado nuevo»; era falso por dos motivos):

- **El ajuste «Mensajes de TiTino» es un dato nuevo del perfil:** `settings.sloth_messages` (booleano, `true` por defecto). Hoy `settings` tiene campos fijos (`sound`, `music`, `haptics`, `patterns`, `language`, `push`; 08 §4.3), así que hay que migrar a `profile_schema` 2 (08 §4.4). Conviene hacerlo **en la misma migración** que `settings.language` y el avistamiento de fauna (propuesta de idiomas §3.5), para migrar una sola vez. La clave nueva entra además en `af_settings_change.key` (08 §8.2). Todo va al lote de documentación para que lo apruebes (regla dura 4 de `CLAUDE.md`).
- **La repetición:** la primera versión prometía «nunca una de las 8 últimas» porque «cada banco tiene más de 8 frases», pero ninguno pasa de 8 (win 8, fail 5, streak 4, return 4, debut 3, hard 2, restart 2, milestone 2), y con un contador de sesión que se reinicia cada sesión empezaría por las mismas frases. Regla nueva: **nunca la misma frase que la última vista del mismo momento.** Para garantizarla se guarda, en la misma migración, el índice de la última frase de cada momento (8 enteros pequeños, por ejemplo `sloth.last_line`) [P]. Sin guardar nada, la alternativa es elegir con contadores que ya existen (`stats.levels_won` dividido entre N en la victoria; `stats.fails_hard` en el fallo) módulo el tamaño del banco: casi nunca repite, pero no lo garantiza.
- Lo demás (qué momento toca y de qué banco sale la frase) se calcula sin estado nuevo, con una función determinista del momento, el nivel, el intento y esos contadores.

**Medición:** cada aparición detrás de su clave de Remote Config [P] (solo cuando se den de alta en la tabla única de 08 §9) y pruebas A/B en el soft launch sobre D1, D7, el tiempo entre la victoria y «Siguiente» y la tasa de «Reintentar» tras Nivel fallido. Un evento propio, por ejemplo `af_mascot_line {moment, line_key}` [P], exige tu aprobación y el alta en 08 §8.2.

### d.11 Dónde nunca aparece y qué nunca hace

**Pantallas donde nunca aparece:** el tablero durante la partida (durante la partida, solo en el tutorial y en el aviso de Volver a la orilla; d.1); «Llenar y teñir»; Aguas estancadas; ¿Seguro?; Sin canoas; el Mercado y cualquier oferta o compra; el intersticial y lo que lo rodea; la pantalla de edad y consentimiento.

**Lo que nunca hace:**
1. Culpar, regañar, llorar o burlarse del fallo.
2. Presionar a comprar o a ver un anuncio, señalar una oferta o hablar de precios.
3. Interrumpir: nunca captura toques, nunca tapa un botón ni una cuia, nunca retrasa un botón.
4. Muros de texto: un mensaje de 1-2 oraciones cortas, ≤ 50 caracteres en ES (e.2), y una sola línea de texto por victoria (d.6).
5. Usar culpa en un botón de rechazo («No, prefiero perder»).
6. Pedir que lo cuides, lo alimentes o lo «adoptes».
7. Aparecer en peligro, triste o enfermo para empujar una decisión.
8. Decir un dato sin fuente o sin el visto bueno del asesor.

---

## (e) Su voz

### e.1 Tono

- **Cálido, tranquilo, optimista y breve.** Habla como un buen amigo que no tiene prisa.
- **Elogia el esfuerzo y la estrategia** («pensaste antes de verter»), no el talento ni la velocidad [V E7].
- **Sus muletillas:** «sin afán», «paso a paso», «con tino», «con calma». Nota: «sin afán» es muy colombiano; en México o Argentina se entiende, pero suena menos natural que «sin prisa». Lo decide el revisor de ES-419 [E].
- **Sin voz hablada:** sonidos suaves (e.5) y, a veces, una frase en pantalla. Los agentes con personaje rinden mejor con texto que con voz narrada [V E8], y así no hay que doblar a cada idioma.

### e.2 Reglas de escritura

1. **Un mensaje de 1-2 oraciones cortas, con `max_chars` por clave:** ≤ 50 caracteres en ES para el bocadillo (≤ 45 si se quiere que quepa en una sola línea). El «+30 %» de idiomas §3.7 vale para textos de más de 70 caracteres; para los de 31-70, las pautas de IBM que cita el W3C dan un crecimiento del 40-70 % al traducir. Por eso EN y PT-BR llevan `max_chars` de hasta 85 y el bocadillo crece en alto (d.4).
2. **Tuteo en ES-419, «você» en PT-BR, inglés sencillo**; sin exclamaciones dobles ni emojis (06 §8.3; glosario §10).
3. **Sin adjetivos con género sobre el jugador** («¡Eres un experto!»): en ES y PT obligan a elegir masculino o femenino. Se elogia la jugada, no a la persona («¡Qué buen orden!»).
4. **Nunca:** «perdiste», «fallaste», «¿te rindes?», «¿seguro que quieres perder?», precios, compras, anuncios, prisas («¡rápido!») ni amenazas («tu racha va a morir»).
5. **Nunca los nombres de comodines con otro sentido:** «Pista» (ES) y «Dica» (PT) son el comodín Morpho, así que un consejo se dice «consejo» / «conselho» / «tip».
6. **Nada de chistes de vago ni de hablar lento.** El humor sale de la calma y de datos reales (prueba la rama, toma el sol, gira la cabeza).
7. **Sin jerga de un solo país** salvo que el revisor la apruebe, y **sin palabras de lenguas indígenas** sin el asesor.
8. **Sin datos en las frases de ánimo** salvo los aprobados (g.3).
9. **Marcadores con nombre** (`{n}`) y nunca plantillas que peguen trozos ni nombres de fruta con artículo (idiomas §3.2 y §3.4).

### e.3 Claves y localización

- **Una clave por frase**, con el formato de la propuesta de idiomas (§3.3): `strings.csv` con `key`, `es-419`, `en`, `pt-BR`, `context`, `max_chars` y `status`. Hasta que exista, las frases entran en `UiText` con su clave en el comentario, como ya se hace.
- **Claves [P]:** `sloth_line_<momento>_<nn>`. Momentos: `win`, `hard`, `fail`, `restart`, `streak`, `return`, `debut`, `milestone`. Los datos del perezoso van en la tabla de textos largos: `fauna.bradypus_variegatus.dato_<nn>` (idiomas §3.4).
- **Estado inicial:** `pendiente_asesor` para todas. Las de EN y PT-BR de este documento son un borrador para el revisor nativo.
- **«TiTino» no se traduce** (como Capi y Rosa, idiomas §3.9). Antes de cada idioma nuevo, el traductor comprueba que no tenga un sentido malsonante (sección h).

### e.4 Treinta frases de ejemplo [P]

Todas ≤ 50 caracteres en ES-419 y ≤ 54 en PT-BR y EN (medido el 2026-10-10). Pendientes del asesor cultural y del revisor nativo.

| # | Clave | Momento | ES-419 | PT-BR | EN |
|---|---|---|---|---|---|
| 1 | `sloth_line_win_01` | Victoria | ¡Qué buen orden! Paso a paso, todo encaja. | Que ordem boa! Passo a passo, tudo se encaixa. | Nice sorting! Step by step, it all fits. |
| 2 | `sloth_line_win_02` | Victoria | Sin afán y con tino. ¡Así se hace! | Sem pressa e com tino. É assim que se faz! | Calm and clever. That's how it's done! |
| 3 | `sloth_line_win_03` | Victoria (solo si el asesor aprueba el dato_11 de g.3) | Dicen que no veo colores, pero este río brilla. | Dizem que não vejo cores, mas este rio brilha. | They say I can't see colors, but this river shines. |
| 4 | `sloth_line_win_04` | Victoria sin Cuia de rescate ni Morpho | Pensaste antes de verter. ¡Se nota! | Você pensou antes de despejar. Dá pra ver! | You thought before you poured. It shows! |
| 5 | `sloth_line_win_05` | Victoria | ¡Todo en su sitio! El río sonríe. | Tudo no lugar! O rio sorri. | Everything in its place! The river smiles. |
| 6 | `sloth_line_win_06` | Victoria | Con calma y bien hecho: mi forma favorita. | Com calma e bem feito: do jeito que eu gosto. | Calm and well done: my favorite way. |
| 7 | `sloth_line_win_07` | Victoria | Celebro despacio, pero de corazón. | Comemoro devagar, mas de coração. | I celebrate slowly, but with all my heart. |
| 8 | `sloth_line_win_08` | Victoria sin Cuia de rescate ni Morpho | ¡Qué último vertido tan bien pensado! | Que jogada final bem pensada! | What a well-planned final pour! |
| 9 | `sloth_line_hard_01` | Victoria Difícil o Muy difícil | ¡Nivel difícil superado! Con calma se llega lejos. | Nível difícil vencido! Com calma se chega longe. | Hard level cleared! Calm gets you far. |
| 10 | `sloth_line_hard_02` | Victoria Difícil o Muy difícil | Ese era difícil de verdad, y lo lograste. | Esse era difícil de verdade, e você conseguiu. | That one was truly hard, and you did it. |
| 11 | `sloth_line_fail_01` | Nivel fallido | Casi. Respira, mira el río y otra vez. | Quase. Respira, olha o rio e mais uma vez. | Almost. Breathe, look at the river, try again. |
| 12 | `sloth_line_fail_02` | Nivel fallido | Sin afán: cada intento te enseña algo. | Sem pressa: cada tentativa ensina algo. | No rush: every try teaches you something. |
| 13 | `sloth_line_fail_03` | Nivel fallido | Yo también pruebo la rama antes de pasar. | Eu também testo o galho antes de passar. | I test each branch before I cross, too. |
| 14 | `sloth_line_fail_04` | Nivel fallido | Mismo tablero, nueva idea. Tú puedes. | Mesmo tabuleiro, nova ideia. Você consegue. | Same board, new idea. You've got this. |
| 15 | `sloth_line_fail_05` | Nivel fallido | Paso a paso. La próxima sale. | Passo a passo. A próxima sai. | Step by step. You'll get the next one. |
| 16 | `sloth_line_restart_01` | Volver a la orilla | Volver a empezar también es avanzar. | Recomeçar também é avançar. | Starting over is moving forward too. |
| 17 | `sloth_line_restart_02` | Volver a la orilla | Consejo: guarda una cuia vacía para el final. | Conselho: guarde uma cuia vazia para o fim. | Tip: keep one cuia empty until the end. |
| 18 | `sloth_line_streak_01` | Crecida de 4 o más, fuera de hito | ¡{n} seguidas! El río crece contigo. | {n} seguidas! O rio cresce com você. | {n} in a row! The river rises with you. |
| 19 | `sloth_line_streak_02` | Crecida de 4 o más, fuera de hito | La Crecida sube y sube. ¡Qué constancia! | A Enchente sobe e sobe. Que constância! | Your Rising River keeps climbing. So steady! |
| 20 | `sloth_line_streak_03` | Crecida de 7 a 9 (sustituye a la del hito: en los hitos de la Crecida protagonizan las nutrias, d.6) | La Crecida ya llega a mi rama. ¡Sigue así! | A Enchente já chega ao meu galho. Continue assim! | Your Rising River reached my branch. Keep going! |
| 21 | `sloth_line_streak_04` | Cualquier fallo duro que rompa la Crecida: Nivel fallido o, en 25-30, Volver a la orilla desde Aguas estancadas (04 §5.2) | La Crecida se vació; vuelve a empezar contigo. | A Enchente esvaziou; recomeça com você. | Your Rising River emptied; it starts again with you. |
| 22 | `sloth_line_return_01` | Regreso (≥ 3 días) | ¡Volviste! Te guardé un lugar al sol. | Você voltou! Guardei um lugar no sol pra você. | You're back! I saved you a spot in the sun. |
| 23 | `sloth_line_return_02` | Regreso | El río sigue donde lo dejaste: nivel {n}. | O rio continua onde você parou: nível {n}. | The river is right where you left it: level {n}. |
| 24 | `sloth_line_return_03` | Regreso | Qué bueno verte. Vamos poco a poco. | Que bom te ver. Vamos aos poucos. | Good to see you. Let's take it slow. |
| 25 | `sloth_line_return_04` | Primera apertura del día entre las 06:00 y las 12:00 (hora local), 1 al día, nunca el día del saludo de regreso (d.7, d.10) | ¡Buenos días! Ya tomé el sol. ¿Jugamos? | Bom dia! Já tomei sol. Vamos jogar? | Good morning! I've had my sun. Shall we play? |
| 26 | `sloth_line_debut_01` | Victoria del nivel con mecánica nueva | ¡Algo nuevo en el río, y ya lo entendiste! | Novidade no rio, e você já entendeu! | Something new on the river, and you've got it already! |
| 27 | `sloth_line_debut_02` | Victoria del nivel con fruta nueva | Un color nuevo llegó al río. ¡Qué bonito! | Uma cor nova chegou ao rio. Que lindo! | A new color reached the river. How lovely! |
| 28 | `sloth_line_debut_03` | Victoria del primer nivel de un mundo | Nuevo tramo del río. Yo te sigo desde mi rama. | Novo trecho do rio. Eu acompanho você do meu galho. | A new stretch of river. I'll follow from my branch. |
| 29 | `sloth_line_milestone_01` | Fin de mundo (salvo el nivel 10) | ¡Mundo completo! Cuánto color trajiste. | Mundo completo! Quanta cor você trouxe. | World complete! You brought so much color. |
| 30 | `sloth_line_milestone_02` | Primer Muy difícil (nivel 60). Sustituye a la de «serie de fruta completa», que chocaba con el cierre de la serie («¡Ya conoces el açaí!») | ¡Tu primer Muy difícil! Con calma lo lograste. | Seu primeiro Muito difícil! Com calma, você conseguiu. | Your first Very Hard level! You kept calm. |

Notas para el revisor:
- n.º 2: «com tino» existe en portugués (juízo, sensatez [V N2]); en inglés no hay equivalente y se adapta.
- n.º 3: el dato es real pero con matiz («probablemente ven sin color» [V B27]); por la regla 8 de e.2 solo se usa si el asesor aprueba el dato_11 de g.3; si no, se retira la frase.
- n.º 4 y n.º 8: elogian la planificación, así que no se usan en una victoria con Cuia de rescate o con Morpho (el mismo criterio que la pregunta abierta 10.2 #25 sobre «¡Perfecto!»; D-008).
- n.º 4: «despejar» es el verbo del canon en PT-BR para verter («Sem mais despejos», glosario §10).
- n.º 13: los perezosos prueban la rama antes de pasar [V B9].
- n.º 18: con n ≥ 3 la forma es siempre plural; si se usa con n menor, va con la sintaxis de plurales de la tabla.
- n.º 18-21: «Crecida» es «Rising River» en EN y «Enchente» en PT-BR (glosario §6.3). Ninguna elogia el ritmo ni la velocidad (c.2). La n.º 21 evita «bajar» y «río», para no confundirse con el mensaje canónico «El río bajó» / «The river dropped» (`river_lowered`, glosario §10), que significa otra cosa y puede salir justo después, al tercer fallo.
- n.º 22 y 25: que tome el sol tiene apoyo [E B44]; la postura de barriga al aire es licencia creativa [P].
- n.º 24: «poco a poco» en lugar de «de a poco», que es regional (regla 7). n.º 29: «trajiste», más natural en ES-419 que «has traído». n.º 17: «cuia vacía», el término del canon (06 §2.2).

### e.5 Sonidos

- **Sin voz hablada.** Tres a cinco sonidos originales [P]: un «mmm» suave y grave al asentir, un suspiro feliz al celebrar, el roce de hojas al entrar su rama y un bostezo muy suave al dormir.
- **Nunca un silbido agudo:** es la llamada de angustia de una cría separada de su madre [E B30]. Un «aiii» suave sería licencia creativa y no debe sonar a eso.
- **Licencias** como en 07 §9.5: originales o con licencia comercial perpetua; nada de grabaciones de perezosos sin licencia.
- Mezcla por debajo de la toada de victoria; respeta el volumen de efectos y el silencio.

---

## (f) Relación con Capi y Rosa

### f.1 Tres opciones

| Opción | Qué hace TiTino | Qué hace Capi | Qué hace Rosa | A favor | En contra |
|---|---|---|---|---|---|
| **A. TiTino protagonista; Capi trae cuias** (recomendada) | Cara del juego, arranque, tutorial, victoria, ánimo, inicio, Mapa, notificaciones, anuncios | Ayudante funcional: Cuia extra (compra, regalo del nivel 8 e icono), Cuia de rescate desde el 31 (`ui_mascot_rescue_anim`), Canasta y sello de la Bitácora | Avanzar: Salto del Boto, Mapa del río, banner de mundo, El río bajó, Dato del río del nivel 10 | El patrón de Royal Match (el rey y su mayordomo) y de Toon Blast (un papel por personaje). **Un perezoso lento no encaja en un rescate rápido**: es el mismo motivo por el que el GDD lo descartó para Deshacer (glosario §3.3). Aprovecha casi todo lo ya diseñado de Capi | Tres personajes con nombre: más arte (≈ +5-6 días) y una decisión nueva |
| B. TiTino sustituye a Capi en todo | Todo lo de A, más traer cuias y el rescate | Desaparece | Igual | Un solo protagonista; menos arte a la larga | El perezoso en el rescate contradice su lentitud y el motivo de 00 §3.3. Se rehacen 12 poses, el texto del nivel 8, el icono del comodín y el concepto de UA n.º 1 |
| C. TiTino solo anfitrión | Arranque, inicio y notificaciones | Sigue de principal: victoria, tutorial, ánimo | Igual | Cambia poco el canon | **No cumple lo que pidió el dueño** (victoria y derrota). Dos caras compiten por la emoción |

**Reparto en una línea (opción A):** TiTino es el corazón (te anima), Capi las manos (te trae cuias) y Rosa el camino (te lleva río arriba; corregido el 2026-10-13 por Q13-A del Mapa del río, D-013, porque la campaña remonta el río). Tres siluetas y tres colores distintos: gris verdoso, pardo y rosa.

### f.2 Reparto de las poses de Capi con la opción A

| Pose de Capi (07 §6.1) | Dónde está hoy | Con A |
|---|---|---|
| `capi_idle` | Mapa, Mercado, Bitácora | Capi en Mercado y Bitácora; en el Mapa, TiTino |
| `capi_present` | Banner de mundo, tutorial 1-3, Reto del río | Tutorial y Reto del río: TiTino (`sloth_present`). Banner de mundo: Rosa (cierra la contradicción señalada al final de f.3) |
| `capi_carry` | Cuia extra | Capi |
| `capi_rescue` | Aguas estancadas desde el 31 | Capi (pendiente del lote Dc5: 02 §7 y 04 §6.6 quitan la ilustración del panel) |
| `capi_cheer` | Victoria | TiTino (`sloth_cheer`) |
| `capi_think` | Primeros 2 s del bloqueo antes del 31 | Se retira: Aguas estancadas va sin ilustración (D-008) |
| `capi_shrug` | Volver a la orilla tras un bloqueo | TiTino (`sloth_encourage`) |
| `capi_sleep` | Espera larga en el Mapa | TiTino (`sloth_sleep`) |
| `capi_wave` | Arranque, día nuevo de la Bitácora | Arranque: TiTino (`sloth_wake`/`sloth_wave`). Bitácora: Capi |
| `capi_point` | Tutorial, Siguiente en hitos | TiTino (`sloth_present`) |
| `capi_basket` | Canasta | Capi |
| `capi_stamp` | Bitácora | Capi |

Resultado: Capi queda con 6 poses (idle, carry, rescue, wave, basket y stamp), todas de «traer o guardar cosas».

### f.3 Qué cambiaría en el GDD con la opción A (sin editarlo)

Exige una **decisión nueva** (**D-011 o el número que toque**; D-007 y D-009 están reservadas) y un lote de documentación que apruebe el dueño. Ojo: la propuesta de frutas (F0) y la de la tinaja (`docs/research/cultura-ribera/propuesta-tinaja.md`) también reclaman el D-011; el número lo asigna el `tech-lead` al registrar la que se apruebe antes.

| Documento | Hoy | Con A [P] |
|---|---|---|
| Glosario §3.1 | Capi, «mascota principal» | Capi, ayudante que trae cuias. Nueva sección para TiTino (personaje principal) |
| Glosario §3.3 | Perezoso: espera en el Mapa | La fila pasa a TiTino |
| Glosario §10 y §11 | — | Claves `sloth_line_*` y `settings_sloth_messages`; IDs `sloth_*`; especie `bradypus_variegatus`; ranuras `fauna_bradypus_variegatus_partes`, `fauna_bradypus_variegatus_retrato` y `arbol_embauba` (c.7) |
| Glosario, preguntas abiertas 3 | Nombres Capi y Rosa | Más TiTino, con marca y asesor |
| `docs/GDD.md` §2 #24 | Capi guía y trae cuias; Rosa salta y guía el Mapa | TiTino protagonista; Capi trae cuias; Rosa avanza |
| 02 §5 (fila Capi) | Capi, decoración animada en la orilla inferior izquierda durante el juego | Ninguna mascota fija durante la partida; TiTino solo en el tutorial y en el aviso de Volver a la orilla (d.1; pregunta T5) |
| 02 §6 | Banner de mundo con Rosa; Dato del río cada 10 niveles | TiTino en la victoria (≈ 1 de cada 4 y tres hitos; d.6), solo si cabe (d.4); una sola línea de texto por victoria (T6); banner con Rosa |
| 02 §12 | — | Ajuste «Mensajes de TiTino»; Reducir movimiento para TiTino |
| 02 §13 y 06 §2.2 | Frases de Capi en el 6 y el 8 | TiTino en 1-3, 6 y 12; Capi en el 8 |
| 04 §2.7 | Volver a la orilla | Aviso de TiTino tras el reinicio en 1-30 |
| 04 §2.2 y §6 | Capi regala la cuia del 8 y rescata | Sin cambios |
| 06 §2.1-§2.2 | Splash con río y logotipo (aún no construido) | TiTino en el splash, sin texto; la primera vez, visible ≥ 1,0 s si el primer toque sigue < 20 s (d.3) |
| 06 §2.2, nivel 11 | Capi presenta una vez la Cosecha doble («Ver video · x2») en la victoria | No la presenta ningún personaje: tooltip sin personaje [P]. Con A saldrían dos mascotas en la misma victoria, o TiTino señalaría una oferta, que d.4 prohíbe |
| 06 §2.3, fila 1 | «Capi celebra desde la orilla» en la victoria | TiTino celebra (d.4) |
| 06 §4.7 | Pantalla de inicio sin personaje | TiTino vive en el inicio |
| 06 §8.1, §8.3 y §9 | Capi firma el pre-prompt, los avisos y el «¿Recuerdas?» | TiTino firma; Capi solo en los regalos de cuias |
| 07 §5.3 | «Si Capi está en pantalla (banner o tutorial), mira hacia la cuia 0,3 s» al sellarse | Se quita la mirada a la cuia: TiTino nunca mira una cuia (c.5, punto 2; D-002) |
| 07 §5.4 | Capi entra a los 0,60 s | TiTino asoma a los 0,60 s, solo si cabe (d.4) |
| 07 §5.5 | `capi_shrug`; Capi en el rescate; Rosa en El río bajó | `sloth_encourage` en Volver a la orilla y Nivel fallido; el resto igual |
| 07 §6.1 | 12 poses de Capi | 6 poses |
| 07 §6 (nueva) | — | TiTino: hoja de modelo, poses y límites |
| 07 §6.3 | Elenco de fondo: «perezoso (idle del Mapa del río)» | El perezoso del elenco pasa a ser TiTino (no hay un segundo perezoso) |
| 07 §6.4 y guion de la intro §4.6 | «Presenta Capi» | Tutorial: TiTino; banner de mundo: Rosa |
| 07 §10 | Icono B con Capi; captura 7; vídeo; UA n.º 1 | Concepto D con TiTino; captura 7 «TiTino te acompaña»; cierre del vídeo; creativo «TiTino te anima» |
| 07 §11 y 09 §5 | Mascotas: 18 animaciones, 14 días | ≈ 22 animaciones; ≈ +5-6 días de artista [E] |
| 08 §9 | — | `ui_sloth_enabled`, `ui_sloth_win_line_every_n`, `ui_sloth_lines_per_session_max`, `ui_sloth_fail_enabled` (y `ui_sloth_prologue_enabled` si T2 = B) |
| 08 §4.3 y §4.4 | `settings` con campos fijos; `profile_schema` 1 | `settings.sloth_messages` (y, si se aprueba, `sloth.last_line`), con migración a `profile_schema` 2 en la misma migración que `settings.language` y la fauna (d.10; propuesta de idiomas §3.5) |
| 08 §8.2 | `af_settings_change.key ∈ {patterns, sound, music, haptics, language, consent}` | Añadir la clave del ajuste de TiTino. Evento opcional `af_mascot_line` (si se aprueba) |
| `docs/research/intro-rio-miquitos/guion.md` §14 | Tras la intro, como mucho una: dedo guía, «Fruta nueva» o «¡Miquito a la vista!» | Añadir el aviso de TiTino tras Volver a la orilla; si el nivel tiene instrucción del tutorial, manda la instrucción (d.5) |
| `docs/art/direccion-visual-v2.md` §3.7 y §11 (C4) | Capi en el muelle durante la partida (64 × 56 dp) [P]; conflicto C4 abierto | Recomendación para C4: **ninguna mascota fija en el muelle** durante la partida, como Magic Sort. TiTino solo asoma sobre el muelle en el tutorial y en el aviso de Volver a la orilla, y solo si la franja del panorama mide < 64 dp (pregunta T5) |
| `docs/art/prompts-chatgpt.md` | Prompt de `capi_idle` | Prompt de la hoja de modelo de TiTino para exploración |
| `docs/research/fauna/propuesta-fauna.md` c.2, fila 14 | «Duerme 15-18 h [V W49]»; perezoso de ambiente en el mundo 4 y en el Mapa | ~9,6 h en libertad (g.5). El perezoso del mundo 4 y del Mapa es TiTino, solo en el Mapa: se quita el perezoso de ambiente del Trapecio (c.3), para que no haya un segundo perezoso |
| `docs/research/fauna/propuesta-fauna.md` c.3 | Miquito en las victorias de cada 10 niveles «mientras entra Capi» | Un solo animal protagonista por victoria: en fin de mundo, TiTino, sin miquito (d.6, punto 3) |
| `docs/research/09-referencia-magic-sort.md` §1, punto 2 | «Más del 50 % del IAP sale de la pantalla de derrota [V]» | «Según Naavik, la monetización al fallar genera la mayoría del IAP [E]» (d.5) |

**Contradicciones previas que conviene cerrar en el mismo lote:** quién presenta el banner de mundo (glosario §3.1 y 07 §2.2 dicen Capi; 02 §6 y glosario §3.2, Rosa), Capi en el rescate (07 §5.5 frente a 02 §7 y 04 §6.6, lote Dc5), Capi en el muelle durante la partida (C4; 02 §5 lo pone en la orilla inferior izquierda) y la Cosecha doble presentada por Capi en el nivel 11 (06 §2.2).

---

## (g) Ética y cultura

### g.1 El perezoso real

- **Turismo de selfis:** en el informe «A close up on cruelty» (World Animal Protection, 2017), el perezoso fue el animal más usado en las fotos con fauna de Manaos y de Puerto Alegría (Perú). En Manaos, el 94 % de las excursiones ofrecía tocar o sostener animales salvajes. Muchos perezosos capturados probablemente mueren antes de seis meses [V B1, B2]. Instagram puso un aviso en #slothselfie [V B3]. En 2025, Mongabay volvía a denunciar el uso del perezoso como reclamo para fotos y su relación con el tráfico; sus pruebas de las excursiones de Manaos son las del estudio de 2017 [V B4]. Si en 2025 se seguía ofreciendo cerca de Manaos no está comprobado [NV].
- **Tráfico de mascotas:** se separa a las crías de sus madres, a menudo con violencia [V B31], y los cazadores a menudo matan a la madre cuando defiende a su cría [V B4]; las crías se venden como si fueran peluches [V B33]. Entre el 80 y el 90 % de los perezosos traficados muere, según organizaciones de Centroamérica y Colombia citadas por el PNUMA en 2018 [E B31]. En las crías pequeñas la mortalidad llega al 99 %, según Tinka Plese, de la Fundación AIUNAU (Colombia): es la estimación de una rehabilitadora, no una medición [E B4].
- **CITES:** en diciembre de 2025 la CoP20 (Samarcanda) incluyó a los dos perezosos de dos dedos en el Apéndice II, a propuesta de Brasil, con Costa Rica y Panamá [V B5, B6]. La especie de TiTino ya está en el Apéndice II [V B41], desde que se firmó la Convención en 1973 con el nombre de *B. boliviensis* [E B49].
- **La «sonrisa» engaña:** es la forma de su cara y aparece aunque el animal sienta dolor o miedo [V B1, B4].
- **El brazo estirado no es un saludo:** es miedo porque alguien está demasiado cerca [V B7].
- **Cómo pide representarlo la Sloth Conservation Foundation:** en su hábitat, sin contacto con personas y sin la imagen de un animal que solo es «tierno y relajado» [V B8].
- **UICN:** las cuatro especies amazónicas figuran como «Preocupación menor» (evaluaciones globales de 2014). Para tres de ellas (*B. variegatus*, *B. tridactylus* y *C. hoffmanni*), la ficha del grupo especialista indica poblaciones a la baja [V B14, B15, B16]; para *C. didactylus* no hay un dato claro [E]. La ficha nacional de Brasil da como desconocida la tendencia de *B. variegatus* [V B41].

### g.2 Reglas duras para TiTino (para el encargo al artista y para los textos)

Se suman a 07 §6.4, 07 §8.1 y la lista de la propuesta de fauna §7.

1. **Siempre salvaje y libre**, en su árbol (embaúba; nunca sumaúma ni lupuna) o cruzando el río a nado. **Pregunta para el asesor:** ¿la embaúba o el yarumo tiene algún uso ritual que desaconseje usarla? En la Amazonia colombiana la ceniza de sus hojas se mezcla con la coca para hacer el mambe, de uso ceremonial [E B50]. En el encargo al artista: nada de hojas quemadas ni ceniza, y una alternativa preparada por si el asesor la desaconseja (c.4).
2. **Sin personas:** nunca en brazos, manos u hombros humanos, nunca junto a una persona, nunca abrazando al jugador, a la cámara ni a un peluche.
3. **Sin ropa**, gorro, collar, correa, pañal, accesorios ni pintura.
4. **Nunca dentro de una cuia, una cesta, un cubo o una caja:** la cuia es el objeto central del juego y la cría en un cubo es la imagen típica del tráfico y de los rescates.
5. **Nunca comiendo de una mano** ni alimentado por el jugador.
6. **Nunca una cría sola** ni un «huérfano rescatado».
7. **Nunca el brazo estirado hacia la cámara** como saludo o petición de abrazo.
8. **Sin lenguaje de mascota:** nada de «adoptar», «tener», «tu perezoso», «cuídalo», «dale de comer».
9. **Nunca en pantallas de pago** ni vendiendo (como Rosa, 07 §6.2).
10. **Nunca en peligro, herido, atrapado o triste** para empujar una decisión, ni en anuncios («salva a TiTino»).
11. **Sin función de foto o selfi con TiTino** (realidad aumentada, «hazte una foto con él»): sería normalizar justo la práctica que lo daña.
12. **Sin chistes de vago**, sin la bajada para defecar y sin la «sonrisa» usada como prueba de que está feliz.
13. **Ningún nombre ni dato cerrado sin el asesor** (07 §8.2).

### g.3 «Dato del perezoso»: candidatos con fuente [P]

Para la tarjeta que sale al tocar a TiTino en el inicio (d.7) y, si se quiere, para la serie de «Dato del río». Todos ≤ 90 caracteres (la medida de la propuesta de frutas) y pendientes del asesor.

| # | Clave | Dato (ES-419) | Fuente |
|---|---|---|---|
| 1 | `fauna.bradypus_variegatus.dato_01` | Su «sonrisa» es la forma de su cara: no quiere que lo carguen. | [V B1, B4, B7] |
| 2 | `fauna.bradypus_variegatus.dato_02` | En libertad duerme unas 9 o 10 horas al día, no 20. | [V B12, B10] |
| 3 | `fauna.bradypus_variegatus.dato_03` | En su pelo viven algas y polillas: por eso se ve verdoso. | [V B23, B24, B11] |
| 4 | `fauna.bradypus_variegatus.dato_04` | Nada muy bien y cruza ríos. | [V B25, B9] |
| 5 | `fauna.bradypus_variegatus.dato_05` | Le dicen oso, pero es pariente del oso hormiguero y del armadillo. | [V B9] |
| 6 | `fauna.bradypus_variegatus.dato_06` | Si ves uno en la selva, míralo de lejos: no lo toques ni pagues por una foto. | [V B7] |
| 7 | `fauna.bradypus_variegatus.dato_07` | Ir despacio es su forma de ahorrar energía, no pereza. | [V B22, B9] |
| 8 | `fauna.bradypus_variegatus.dato_08` | La cría viaja en el pecho de su madre durante meses (unos seis en esta especie). | [E B41]. Las fuentes no coinciden: ICMBio da unos 6 meses para *B. variegatus*, ADW un destete hacia los 4 meses [B47] y la SCF «se cree» que hasta 12 meses en los tres dedos en general [B11]. Si el asesor o un biólogo no lo confirma, se quita |
| 9 | `fauna.bradypus_variegatus.dato_09` | En Brasil, la embaúba se llama «el árbol del perezoso». | [V B28, B29] |
| 10 | `fauna.bradypus_variegatus.dato_10` | Gira la cabeza casi tres cuartos de vuelta sin mover el cuerpo. | [E B26, B11] |
| 11 | `fauna.bradypus_variegatus.dato_11` | Probablemente ve sin colores: su vista se basa en los bastones. | [V B27] (inferencia genómica: «probablemente»). Lo necesita la frase n.º 3 de e.4 |

El n.º 6 es el mensaje de conservación: es la conducta concreta que evita el daño de g.1. **Datos que no se usan nunca:** «duerme 20 horas», «aguanta 40 minutos bajo el agua» (viene de un estudio de 1942 con animales inmovilizados, en el que fueron unos 15 [V B9]), «es vago» y «es un oso».

**Día Internacional del Perezoso (20 de octubre)** [E B34, B33]: puede ser un evento anual de TiTino [P]. Cualquier mención a donaciones o alianzas (AIUNAU, Sloth Conservation Foundation, World Animal Protection) exige un acuerdo real del dueño; no se insinúa ninguna sin él. Hay precedente en móvil: *Boris the Sloth* (2021) donaba parte de sus ingresos a la conservación [V B37].

### g.4 Cultura y nombres locales

Pendiente del asesor; solo como dato o como sabor local, nunca como nombre del personaje:
- Colombia y Venezuela: «perico ligero» [V B14], nombre irónico que viene de la Colonia: según Fernández de Oviedo, los primeros españoles del Darién le pusieron un nombre muy distinto de su naturaleza [E B46].
- Bolivia: «perico» [E]. Perú: «pelejo» [E B38].
- Brasil: «bicho-preguiça», «preguiça-comum» [V B14, B41]; «preguiça» significa pereza.
- «Aí», posible imitación de su sonido, recogido en el diccionario histórico [V B39].
- **Lenguas indígenas** (por ejemplo, «indillama» en el quichua del bajo Napo [V B40]): solo como dato y nunca sin la revisión pagada del asesor.

### g.5 Un dato del repositorio que hay que corregir

`docs/research/fauna/propuesta-fauna.md`, fila 14, dice «duerme 15-18 h [V W49]» (W49 es Wikipedia). Es la cifra de cautiverio. En libertad, medido con EEG en Panamá, son **9,63 h** (Rattenborg et al., 2008) [V B12, B13]; la Sloth Conservation Foundation da 8-10 h [V B10]. Matiz: el estudio midió solo a 3 hembras adultas en la isla de Barro Colorado durante 12,8 días, y los autores advierten que la edad u otros factores pueden explicar parte de la diferencia con el cautiverio (15,85 h). Por eso el dato_02 dice «unas 9 o 10 horas». No se edita ahora: va al lote de documentación.

---

## (h) El nombre

| Idioma | Cómo se lee | Qué evoca | Riesgo |
|---|---|---|---|
| **Español** | ti-TI-no | «Tino¹»: acierto, buen juicio («con tino»). «Tino²»: «tina que sirve para el tinte» (DLE) [V N1], un eco directo de Tintalú y de teñir. «Tino» es además un diminutivo de nombres como Agustín, Faustino o Celestino [E] | «Tití» es un mono pequeño en varios países y el canon lo veta como genérico: podría confundirse con los miquitos [V repo: 07 §6.4 regla 6] |
| **Portugués de Brasil** | tchi-TCHI-nu | «Tino» es juízo, sensatez; «perder o tino» es enloquecer [V N2] | Ninguno conocido [E] |
| **Inglés** | ti-TEE-no | Nada en concreto | Empieza por «tit» (vulgar, y también un pájaro). La mayúscula interior («TiTino») ayuda a leer «Ti-Tino» [E] |
| **Francés** | — | — | «Titi» es Piolín (Tweety, Warner Bros.) y el «titi parisien» es el chiquillo de París [E N4] |
| **Italiano** | — | «Tino» es la tina del vino; «Titino» es una variante del nombre Tito [V N3] | «Titti» es Piolín [E N4] |
| **Alemán** | — | — | «Titte» es vulgar (comparte el comienzo) [E] |

- **Choques encontrados:** ninguno idéntico en una búsqueda web básica. Hay usos cercanos: «Tino», mascota de los Joven Club de Computación de Cuba [NV N5], y «Titó», personaje infantil de la televisión catalana [NV N6]. **No es una búsqueda de marca.**
- **Búsqueda de marca pendiente:** pedir al abogado que incluya «TiTino» y «Titino» en la misma búsqueda que Tintalú (clases 9, 28 y 41: INPI de Brasil, IMPI de México, SIC de Colombia, INDECOPI de Perú, USPTO y EUIPO), más tiendas de apps y nombres de usuario en redes.
- **Grafía:** «TiTino», con la mayúscula interior que escribió el dueño, separa «Ti» (eco de Tintalú) de «Tino» y evita leer «Titi». La alternativa es «Titino», más natural en español pero sin esa separación. Pregunta T4.
- **En el código, nunca el nombre:** `sloth_*` para el arte y las claves, `bradypus_variegatus` para la especie. Un cambio de nombre no toca código (glosario §11).
- **Si el abogado encuentra un choque:** se buscan alternativas con el mismo criterio: 2-3 sílabas, sin sentido adverso en ES, PT y EN, y un eco de «tinte» o de «tino».

---

## (i) Plan para cuando se trabaje

Pasos pequeños, una tarea = una rama = un PR (`docs/setup/equipo-de-agentes.md`). **Sin paquetes, SDK, permisos ni versiones nuevas.** La única excepción son las notificaciones (paso 8), que ya prevé el GDD y que esperan tu aprobación.

| # | Quién | Qué | Depende de | Tamaño | Cómo se comprueba |
|---|---|---|---|---|---|
| 0 | Dueño | Responder T1, T2, T4, T5 y T6 (sección j); T6 a la vez que la propuesta de frutas | — | — | — |
| 1 | `tech-lead` | Registrar la decisión (D-011 o el número que toque) en `docs/dev/decisiones.md` y preparar el lote de documentación de f.3 para tu aprobación, incluidos el ajuste nuevo y la migración del perfil (d.10) | 0 | S | Revisión del dueño |
| 2 | Abogado, asesor y artista (humanos) | Nombre (marca); especie, frases, datos y nombres locales (asesor); hoja de modelo (artista, con el asesor antes del arte final) | 0 | — | Actas |
| 3 | `unity-gameplay-dev` | **Lógica pura** en AF.GameFlow (sin motor): qué gesto y qué frase tocan en cada momento, con los topes de d.10, las reglas de d.6 (tres hitos seguros, banco más específico, un protagonista y una sola línea por victoria) y la elección determinista del banco. El ajuste y la última frase de cada momento van en la migración del perfil aprobada en el paso 1 (d.10) | 1 | S | Tests de EditMode: topes y su orden (d.6, punto 5), los tres hitos seguros, nunca dos pantallas seguidas con frase, nunca la misma frase que la última del mismo momento, una sola línea por victoria, mismo resultado con la misma entrada; prueba de la migración |
| 4 | `unity-gameplay-dev` | **Arte procedural provisional**: un rig recortable de 8-10 piezas con el método de `MonkeyRig` y `ProceduralArt.Fauna` (contorno de 2 px, paleta de c.4). **Solo en builds de desarrollo**; en release, sin arte final no aparece (como los miquitos, 07 §6.4) | 1 | M | Capturas de PlayMode a 360 × 780 y 360 × 640 |
| 5 | `unity-gameplay-dev` | **Textos con claves**: las 30 frases en ES-419 con su clave (`UiText` hoy; `strings.csv` si se aprueba la propuesta de idiomas), estado `pendiente_asesor`, EN y PT-BR como borrador (el asesor las revisa después, paso 2) | 1 | S | Prueba de `max_chars` por clave (≤ 50 en ES; e.2) |
| 6a | `unity-gameplay-dev` | **Gancho de victoria**: rama y `sloth_cheer` a los 0,60 s; bocadillo a los 1,10 s; caja calculada con el layout real (d.4) | 3-5 | S | «Siguiente» sigue a los 1,40 s; `raycastTarget = false`; Reducir movimiento. **Ninguna intersección** de TiTino ni del bocadillo con la cinta, las cuias (con su elevación), el saldo, la barra de la Crecida, el Dato del río ni los botones, a 360 × 640 y a 360 × 780, con 1 y 3 filas de cuias y con la escala de fuente al 100 % y al 130 %; con 3 filas, TiTino no sale |
| 6b | `unity-gameplay-dev` | **Gancho de fallo**: TiTino en `LevelFailedScreen` en el hueco izquierdo de la cesta y la frase bajo la cesta (d.5); aviso tras Volver a la orilla (1-30) | 3-5 | S | «Reintentar» sale a la vez que hoy; ninguna aparición en Aguas estancadas, ¿Seguro? ni Sin canoas; las mismas pruebas de intersección y de escala de fuente que en 6a para la tarjeta; el aviso respeta el orden del guion §14 |
| 6c | `unity-gameplay-dev` | **Gancho de inicio**: TiTino en `LobbyScreen`, reposo, sol o sueño por hora local, saludo de regreso y toque | 3-5 | S | No tapa «Jugar» ni los saldos; tope de 1 saludo al día |
| 6d | `unity-gameplay-dev` | **Gancho de arranque**: primero, construir el splash de 06 §2.2, que hoy no existe; después, `sloth_wake` en él la primera vez, visible ≥ 1,0 s si cabe (d.3) | 3-5 | M | El primer toque en el nivel 1 sigue por debajo de 20 s (se mide en el teléfono); si no cabe el segundo, se avisa al dueño de que TiTino puede no verse |
| 6e | `unity-gameplay-dev` | **Gancho del tutorial** (si T5 = B): retrato en las instrucciones de 1-3, 6 y 12, tras la salida de la tropa, sobre la franja del panorama o sobre el muelle (d.3) | 3-5 | S | Orden de la intro (guion §14) intacto; ninguna intersección con cuias ni bandeja |
| 7 | `tech-lead` y dueño | Dar de alta las claves `ui_sloth_*` en 08 §9 y, si lo apruebas, el evento `af_mascot_line` en 08 §8.2. Hasta entonces, constantes de desarrollo en `GameConfig` | 1 | S | — |
| 8 | `sdk-integrator` | **Notificaciones** con la voz de TiTino, solo cuando apruebes el paquete y el permiso `POST_NOTIFICATIONS` | Tu aprobación | M | Pruebas en el teléfono |
| 9 | Artista → `unity-gameplay-dev` | Arte final por Git LFS y sustitución del procedural; sonidos originales | 2 | M | Revisión del asesor; capturas |
| 10 | Dueño | Ficha, icono (concepto D), capturas, vídeo y creativos, **solo con arte final** | 9 | — | Experimentos de ficha y de UA |

Cada gancho va en su propio PR, detrás de un interruptor, y con sus capturas de PlayMode antes de pasar al `code-reviewer`. Ningún paso toca `Packages/manifest.json`, el YAML de Unity a mano ni el núcleo (`Packages/com.amazonfill.core/`).

---

## (j) Preguntas cerradas al dueño

| # | Pregunta | Opciones | Recomendación |
|---|---|---|---|
| **T1** | ¿Qué papel tienen TiTino y Capi? | **A.** TiTino protagonista (cara, arranque, tutorial, victoria, ánimo, inicio, notificaciones, anuncios) y Capi conserva su función de traer cuias (Cuia extra, rescate, Canasta, Bitácora); Rosa sigue con avanzar. **B.** TiTino sustituye a Capi en todo, rescate incluido. **C.** TiTino solo anfitrión (arranque, inicio, notificaciones) y Capi sigue de principal | **A.** Es el patrón de Royal Match (rey y mayordomo); un perezoso lento no encaja en un rescate rápido (el motivo de glosario §3.3), y se aprovecha lo diseñado para Capi. C no cumple tu pedido (victoria y derrota) |
| **T2** | ¿Cómo entra TiTino en la «introducción»? | **A.** En la pantalla de arranque que prevé el GDD (río y logotipo; despierta en su árbol, sin palabras) y en las instrucciones del tutorial (niveles 1-3, 6 y 12). **B.** A, más un prólogo de ≤ 8 s solo la primera vez, saltable y como experimento del soft launch. **C.** Dentro de «Llenar y teñir» en cada nivel | **A.** Respeta D-010 y la regla «sin pantallas de texto en el arranque». **Ojo:** el splash todavía no está construido, y en un teléfono rápido la carga puede acabar antes de que TiTino se vea. Por eso A incluye un mínimo de 1,0 s visible solo en la primera apertura, siempre que el primer toque siga por debajo de 20 s (KPI de 09); si ese segundo no cabe, con A TiTino puede no verse en el arranque. B solo si quieres probarlo con datos |
| **T4** | ¿Nombre y grafía? | **A.** «TiTino», con la mayúscula interior. **B.** «Titino». **C.** Buscar otro nombre ya | **A**, como lo escribiste: separa «Ti» (eco de Tintalú) de «Tino» (acierto; tina del tinte) y evita leer «Titi». En los dos primeros casos, el abogado lo incluye en la búsqueda de Tintalú; el código no lleva el nombre |
| **T5** | ¿TiTino aparece durante la partida? | **A.** Nunca: las instrucciones del tutorial van sin personaje y Volver a la orilla, sin aviso. **B.** Solo en el tutorial (niveles 1-3, 6 y 12) y en el aviso de Volver a la orilla (niveles 1-30), fuera del panel del tablero, sin capturar toques, nunca fijo en el muelle. **C.** B, más fijo en el muelle durante toda la partida (resolver C4 a su favor) | **B.** Cumple «que nos acompañe» sin tocar el tablero, como Magic Sort, que no pone personajes en la partida. Para C4 recomiendo además ninguna mascota fija en el muelle, ni Capi ni TiTino |
| **T6** | Cuando en la misma victoria tocan una frase de TiTino y otra línea de texto (el Dato del río, hoy cada 10 niveles; o la línea de la serie de frutas, que sería en cada victoria si apruebas la opción A de esa propuesta), ¿qué sale? | **A.** El dato; TiTino solo hace el gesto. Excepción: en fin de mundo, en el primer Difícil y en el primer Muy difícil habla TiTino y el dato de ese nivel pasa a la victoria siguiente. **B.** Siempre TiTino; el dato pasa a la victoria siguiente. **C.** Los dos a la vez | **A.** El dato es contenido y ya es canon (02 §6), y así TiTino sigue siendo «de vez en cuando». C rompe la cadena corta de D-008. **Ojo:** con A y la opción A de frutas, TiTino solo hablaría en la victoria en esos tres hitos; acompañaría con el gesto, en Nivel fallido y en el inicio. Responde T6 a la vez que la propuesta de frutas |

> **Decididas el 2026-10-13:** **T1 → A** y **T6 → A** (D-013), con las preguntas del Mapa del río, y **T2 → A**, **T4 → A** y **T5 → B** (D-016), con el «Si» del dueño a las recomendadas. T3 quedó resuelta: *Bradypus variegatus*, salvo el asesor.

**T3 deja de ser pregunta:** la especie queda por defecto en *Bradypus variegatus* (perezoso de tres dedos de garganta marrón; c.1), salvo que el asesor diga otra cosa. Es una cuestión de biología y de distribución que ya está pendiente del asesor.

Lo que no se pregunta porque ya está en tu pedido o en el canon: la frecuencia («de vez en cuando»: ≈ 1 de cada 4 victorias y tres hitos; d.6), que nunca aparezca en pantallas de pago (canon de Rosa y de la fauna) y las reglas de bienestar animal (tus preferencias, traspaso §8). **El ajuste «Mensajes de TiTino» y la migración del perfil** (d.10) no son una pregunta aparte: los apruebas en el lote de documentación (regla dura 4 de `CLAUDE.md`).

---

## Pendientes de verificación

- Distribución de *B. variegatus* por país y por mundo (Marajó, Escudo de Guayana, El origen) y si el antifaz y el espéculo se representan bien: asesor y, si hace falta, un biólogo [E].
- La «monocromacia» del perezoso (frase n.º 3 y el gancho con Patrones de fruta) es una inferencia genómica: «probablemente» [V B27].
- El giro de cabeza de unos 270° se atribuye a sus vértebras, pero un estudio de 2023 matiza el papel de su número [E B26].
- Búsqueda de marca de «TiTino» y «Titino» [NV].
- Cifras de Duolingo de 2025 (pausa del contenido «atrevido») y del experimento de racha: solo vistas en resúmenes o extractos [NV J34, J35].
- Si Royal Match reacciona con el rey en las pantallas de victoria y de fallo: no se pudo verificar [NV J8].
- Tiempo real del arranque en el teléfono (para T2): hay que medirlo antes de decidir un prólogo y el segundo mínimo de `sloth_wake`.
- **Añadidos en la revisión:**
  - edad de madurez del macho (y, por tanto, del espéculo completo): ADW da 3-5 años y la ficha de ICMBio, unos 6 [V B47, B41];
  - cuánto pesa la embaúba en su dieta: ADW y la UFMG frente a ICMBio (c.4) [V, fuentes en conflicto];
  - cuánto tiempo lleva la madre a la cría (dato_08): 4, 6 o 12 meses según la fuente [E];
  - que tome el sol: el estudio de Kyoto lo propone para *B. tridactylus*; no se encontró una fuente para *B. variegatus* [E B44];
  - si la embaúba o el yarumo tiene uso ritual (mambe): pregunta para el asesor [E B50];
  - el reportaje del PNUMA (B31) y el artículo de PeerJ (B43) devolvieron 403 en la revisión; la frase del PNUMA sobre el 80-90 % se comprobó en el extracto del buscador;
  - la cita de Oviedo (B46) se vio en el extracto del buscador; la página de Vassar pide acceso [E];
  - la frase «#1 fan» de la guía de Duo (J27) solo se vio en el extracto del buscador; la página ya no existe [E];
  - la caja de TiTino en la victoria (d.4) y la tasa real de frases (d.6) dependen de capturas con 1, 2 y 3 filas: cuántas victorias de los niveles 61-200 dejan hueco.

---

## Fuentes

Fuentes web leídas el 2026-10-10 por las líneas de investigación de este encargo («apps» y «perezoso»); las marcas son las suyas, salvo las wikis de fans y los extractos, que aquí van en [E].

### Juegos y apps (J)

| Código | Fuente | URL | Marca |
|---|---|---|---|
| J1 | Royal Match, App Store | https://apps.apple.com/us/app/royal-match/id1482155847 | [V] |
| J2 | Royal Kingdom, App Store | https://apps.apple.com/us/app/royal-kingdom/id1606549505 | [V] |
| J3 | Dream Games, Royal Match | https://www.dreamgames.com/games/royal-match | [V] |
| J4 | Dream Games, Royal Kingdom | https://www.dreamgames.com/games/royal-kingdom | [V] |
| J5 | TV Tropes, Royal Match (solo extracto) | https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/RoyalMatch | [E] |
| J6 | Old Cynic, niveles «King's Nightmare» | https://oldcynic.com/royal-match-save-the-king-levels-kings-nightmare | [NV] |
| J7 | Udonis, análisis de Royal Match | https://www.blog.udonis.co/mobile-marketing/mobile-games/royal-match-analysis | [V] |
| J8 | Old Cynic, reseña de Royal Match | https://oldcynic.com/royal-match-review-mobile-puzzle-game | [NV] |
| J9 | Marketing Beat (feb-2023), anuncio de TV | https://www.marketing-beat.co.uk/2023/02/20/royal-match-house-337/ | [V] |
| J10 | Marketing Beat (sep-2023), famosos | https://www.marketing-beat.co.uk/2023/09/04/royal-match-celebrities-game/ | [V] |
| J11 | Wikipedia, Royal Match | https://en.wikipedia.org/wiki/Royal_Match | [E] |
| J12 | Mobidictum (nov-2023) | https://mobidictum.com/dream-games-royal-match-royal-kingdom-vs-candy-crush/ | [E] |
| J13 | Stanford CS105, personajes de Candy Crush | https://web.stanford.edu/class/cs105/showcase/showcase/candycrush/Characters.html | [E] |
| J14 | Candy Crush Wiki (fans) | https://candycrush.fandom.com/wiki/Characters | [E] |
| J15 | La Nación, la voz de Candy Crush | https://www.lanacion.com.ar/tecnologia/la-voz-argentina-detras-de-spotify-nid2074124/ | [E] |
| J16 | Know Your Meme, Austin | https://knowyourmeme.com/memes/homescapes-austin-the-butler | [E] |
| J17 | PocketGamer.biz, making of Homescapes | https://www.pocketgamer.biz/the-making-of-homescapes/ | [V] |
| J18 | Google Play, Homescapes | https://play.google.com/store/apps/details?id=com.playrix.homescapes | [V] |
| J19 | Gamezebo, Gardenscapes | https://www.gamezebo.com/news/behind-the-game-gardenscapes/ | [V] |
| J20 | Game Developer, la ASA prohíbe anuncios de Playrix (2020) | https://www.gamedeveloper.com/business/uk-regulator-bans-misleading-i-homescapes-i-i-gardenscapes-i-pin-puzzle-ads | [V] |
| J21 | Township Wiki (fans), Ernie | https://breezewiki.discard.no/township/wiki/Ernie | [E] |
| J22 | GameRefinery, onboarding | https://gamerefinery.com/first-impression-seals-the-deal-onboarding-best-practices-part-1 | [V] |
| J23 | Toon Blast, App Store | https://apps.apple.com/us/app/toon-blast/id1176027022 | [V] |
| J24 | Ads of the World, «Toons on Tour» | https://adsoftheworld.com/campaigns/toons-on-tour | [V] |
| J25 | PocketGamer.biz, licencias de Rovio | https://www.pocketgamer.biz/how-rovios-licensing-and-games-business-have-evolved-over-the-last-decade | [V] |
| J26 | Angry Birds Wiki (fans), «Level Failed» | https://angrybirds.fandom.com/wiki/Level_Failed | [E] |
| J27 | Duolingo, guía de escritura de Duo (solo el extracto indexado: el 2026-10-10 el enlace redirige al blog de diseño, https://blog.duolingo.com/hub/design/, y no se encontró copia accesible) | https://design.duolingo.com/writing/duo | [E] (enlace caído) |
| J28 | Advertising Week, el diseño de Duo | https://advertisingweek.com/the-surprising-reason-why-the-duolingo-owl-is-green/ | [V] |
| J29 | Design Week (ene-2019), rediseño de Duo | https://www.designweek.co.uk/issues/2-6-january-2019/duolingo-owl-receives-makeover-as-app-becomes-more-gamified | [V] |
| J30 | Yancey y Settles, KDD 2020 | https://research.duolingo.com/papers/yancey.kdd20.pdf | [V] |
| J31 | Blog de Duolingo, «Hi, it's Duo» | https://blog.duolingo.com/hi-its-duo-the-ai-behind-the-meme/ | [V] |
| J32 | Medium, «Duolingo needs to chill» | https://debugger.medium.com/duolingo-needs-to-chill-8f1832745ca0 | [E] |
| J33 | Duolingo, carta a los accionistas del 1T 2025 (SEC) | https://www.sec.gov/Archives/edgar/data/1562088/000156208825000098/q1fy25duolingo3-31x25share.htm | [V] |
| J34 | MarketBeat, resultados del 2T 2025 | https://www.marketbeat.com/earnings/reports/2025-8-6-duolingo-inc-stock | [NV] |
| J35 | Duolingo, blog de ingeniería sobre rachas (extracto) | https://making.duolingo.com/how-streaks-keep-duolingo-learners-committed-to-their-language-goals | [NV] |
| J36 | Artik, caso de UA de Magic Sort | https://artik.agency/cases/magic-sort-user-acquisition-case-study | [V] (caso de la propia agencia) |
| J37 | Wikipedia, Cut the Rope | https://en.wikipedia.org/wiki/Cut_the_Rope_(video_game) | [E] |
| J38 | Cut the Rope Wiki (fans), Om Nom | https://cuttherope.fandom.com/wiki/Om_Nom | [E] |
| J39 | GeekWire (2015), Om Nom | https://www.geekwire.com/2015/om-nom-is-back-and-still-hungry-new-cut-the-rope-to-debut-in-december/ | [V] |
| J40 | PocketGamer.biz, Om Nom como marca | https://www.pocketgamer.biz/growing-the-green-monster-building-om-nom-as-a-brand-key-to-cut-the-ropes-success-says-zeptolab/ | [V] |
| J41 | PocketGamer.biz, Om Nom Stories y Disney | https://www.pocketgamer.biz/zeptolabs-om-nom-stories-lands-25-seasons-deal-with-disney/ | [V] |
| J42 | Entrepreneur, Talking Tom | https://www.entrepreneur.com/article/246484 | [V] |
| J43 | Wikipedia, Pou | https://en.wikipedia.org/wiki/Pou_(video_game) | [E] |
| J44 | Chrome Web Store, extensión de terceros sobre los mensajes motivacionales de Duolingo | https://chromewebstore.google.com/detail/clipadhhddnpnocanhnbonnhppdibnpf | [NV] |
| J45 | Stash Media, corto «Tumbleweed» de Toon Blast (Blinkink) | https://www.stashmedia.tv/?p=75465 | [V] (texto de marketing del estudio) |
| J46 | Naavik, por qué los inversores de Peak apostaron por Grand Games | https://naavik.co/digest/why-peak-games-investors-backed-grand-games/ | [E] (analistas) |
| J47 | Felix Braberg, el motor de monetización de Magic Sort | https://felixbraberg.substack.com/p/the-monetization-engine-behind-magic | [E] (analista) |

### Diseño y evidencia (E)

| Código | Fuente | URL | Marca |
|---|---|---|---|
| E1 | GameRefinery, narrativa en juegos casuales (2020) | https://www.gamerefinery.com/storytelling-in-casual-games/ | [V] |
| E2 | Liquid & Grit, FTUE toolkit | https://liquidandgrit.com/first-time-user-experience-ftue-toolkit | [V] |
| E3 | System1 e IPA, personajes de marca (vía Selfstorming) | https://www.selfstorming.com/tools/libraries/wisdom/a-brand-character-raises-the-odds-of-very-large-market-share | [NV] |
| E4 | MarTech Series, System1 | https://martechseries.com/analytics/behavioral-marketing/memorable-characters-ads-boost-chances-profit-30-just-4-us-brands-using-says-system1/ | [NV] |
| E5 | Udonis, informe de juegos de puzle | https://www.blog.udonis.co/mobile-marketing/mobile-games/puzzle-games-report | [NV] |
| E6 | SplitMetrics, icono con personaje | https://splitmetrics.com/blog/investigate-the-mystery-of-why-the-pumpkin-variation-won/ | [E] (proveedor) |
| E7 | O'Rourke et al., «Brain Points», CHI 2014 | https://grail.cs.washington.edu/wp-content/uploads/2015/08/orourke2014bpa.pdf | [V] |
| E8 | Schroeder, Adesope y Gilbert (2013), metaanálisis de agentes pedagógicos | https://rex.libraries.wsu.edu/esploro/outputs/journalArticle/How-Effective-are-Pedagogical-Agents-for/99900601056901842 | [V] |
| E9 | Mental Floss, la vida de Clippy | https://www.mentalfloss.com/article/504767/tragic-life-clippy-worlds-most-hated-virtual-assistant | [V] |
| E10 | Deceptive Design, *confirmshaming* | https://www.deceptive.design/types/confirmshaming | [V] |
| E11 | Hunton, informe de la FTC sobre patrones engañosos (2022) | https://www.huntonprivacyblog.com/2022/09/27/ftc-releases-report-on-dark-patterns/ | [V] |

### Perezoso: biología, bienestar y nombres (B)

| Código | Fuente | URL | Marca |
|---|---|---|---|
| B1 | World Animal Protection, selfis en la Amazonía | https://www.worldanimalprotection.ca/news/iconic-amazonian-wild-animals-are-suffering-selfies/ | [V] |
| B2 | World Animal Protection, «A close up on cruelty» (2017) | https://www.worldanimalprotection.ca/our-work/animals-wild/wildlife-not-entertainers/wildlife-selfie-code/close-up-on-cruelty | [V] |
| B3 | Global News, aviso de Instagram en #slothselfie | https://globalnews.ca/news/3896582/stop-slothselfie-instagram/amp/ | [V] |
| B4 | Mongabay (nov-2025), selfis y tráfico | https://news.mongabay.com/2025/11/sloth-selfies-are-feeding-a-booming-wildlife-trafficking-trade/ | [V] |
| B5 | Mongabay (dic-2025), CITES CoP20 | https://news.mongabay.com/2025/12/stricter-rules-adopted-to-protect-sloths-from-pet-trade-and-selfie-tourism/ | [V] |
| B6 | IFAW, CITES y perezosos de dos dedos | https://www.ifaw.org/international/press-releases/two-toed-sloths-international-trade-protections-cites | [V] |
| B7 | Sloth Conservation Foundation, código de selfis | http://www.slothconservation.org/blog/the-ultimate-sloth-selfie-code | [V] |
| B8 | Sloth Conservation Foundation, amor y respeto | http://www.slothconservation.org/blog/love-respect-sloths | [V] |
| B9 | Sloth Conservation Foundation, mitos | http://www.slothconservation.org/blog/myths-11-things-you-think-you-know-about-sloths | [V] |
| B10 | Sloth Conservation Foundation, cuánto duermen | http://www.slothconservation.org/blog/how-much-do-sloths-sleep | [V] |
| B11 | Sloth Conservation Foundation, perezosos de tres dedos | https://slothconservation.com/about-the-sloth/three-fingered-sloths | [V] |
| B12 | Rattenborg et al. (2008), Biology Letters | https://pmc.ncbi.nlm.nih.gov/articles/PMC2610152 | [V] |
| B13 | Max Planck, nota de prensa (2008) | https://www.mpg.de/567312/pressRelease20080521 | [V] |
| B14 | xenarthrans.org, *B. variegatus* | https://xenarthrans.org/?p=10123 | [V] |
| B15 | xenarthrans.org, *B. tridactylus* | https://xenarthrans.org/?p=5558 | [V] |
| B16 | xenarthrans.org, *C. hoffmanni* | https://xenarthrans.org/species/sloths/hoffmanns-two-toed-sloth/ | [V] |
| B17 | Mammal Diversity Database, *B. variegatus* | https://mammaldiversity.org/taxon/1000432 | [V] |
| B18 | Mammal Diversity Database, *C. didactylus* | https://www.mammaldiversity.org/taxon/1000433 | [V] |
| B19 | Moraes-Barros et al. (2010), Edentata 11(1) | https://bioone.org/journals/edentata/volume-11/issue-1/020.011.0110/Reevaluation-of-the-Geographical-Distribution-of-Bradypus-tridactylus-Linnaeus-1758/10.1896/020.011.0110.full | [V] |
| B20 | Tesis UFAM (2015), perezosos de Manaos | https://www.tede.ufam.edu.br/handle/tede/6157 | [V] |
| B21 | Portal Amazônia, 10 datos del bicho-preguiça | https://portalamazonia.com/noticias/meio-ambiente/10-fatos-sobre-a-miss-simpatia-da-amazonia-o-bicho-preguica | [V] |
| B22 | Science News, metabolismo (Pauli et al. 2016) | https://www.sciencenews.org/blog/wild-things/three-toed-sloths-are-even-more-slothful-two-toed-sloths | [V] |
| B23 | Pauli et al. (2014), Proc R Soc B | https://pmc.ncbi.nlm.nih.gov/articles/PMC3906947 | [V] |
| B24 | Royal Society, perezosos y polillas | https://royalsociety.org/news/2014/sloths-moths-mutualisms | [V] |
| B25 | The Sloth Institute, perezosos que nadan | https://www.theslothinstitute.org/2018/07/05/all-sloths-a-swimming/ | [V] |
| B26 | Vértebras del cuello (2023), PMC | https://www.ncbi.nlm.nih.gov/pmc/articles/PMC10618861/ | [E] |
| B27 | Emerling y Springer (2015), visión | https://pmc.ncbi.nlm.nih.gov/articles/PMC4298209 | [V] |
| B28 | UFMG, la embaúba | https://www.ufmg.br/mhnjb/ceplamt/?p=948 | [V] |
| B29 | Embrapa, especies arbóreas: embaúba | https://ainfo.cnptia.embrapa.br/digital/bitstream/item/232390/1/Especies-Arboreas-Brasileiras-vol-2-Embauba.pdf | [V] |
| B30 | SciELO, vocalizaciones | https://scielo.br/j/paz/a/Xrtj3xPrx7G7b9zYN5pPmbg/?lang=en | [E] |
| B31 | PNUMA, salvar a los perezosos | https://www.unep.org/es/noticias-y-reportajes/reportajes/salvando-los-tiernos-perezosos | [V] |
| B32 | Semana, AIUNAU y el tráfico | https://www.semana.com/mejor-colombia/articulo/asi-se-protegen-los-osos-perezosos-del-trafico-ilegal/202126/ | [V] |
| B33 | El Colombiano, «los perezosos no son peluches» | https://www.elcolombiano.com/historico/los_perezosos_no_son_peluches-AYEC_265764 | [V] |
| B34 | UPI, Día Internacional del Perezoso | https://www.upi.com/Odd_News/2022/10/20/International-Sloth-Day-promotes-slow-moving-animal-conservation/1091666276580/ | [E] |
| B35 | Comedy for Animators, Flash (Zootopía) | https://comedyforanimators.com/2016/03/07/zootopias-flash-the-sloth/ | [V] |
| B36 | Bulbapedia, Slakoth | https://bulbapedia.bulbagarden.net/wiki/Slakoth_(Pok%C3%A9mon) | [V] |
| B37 | Pocket Gamer, Boris the Sloth | https://www.pocketgamer.com/boris-the-sloth/ | [V] |
| B38 | Panamericana, «pelejo» | https://panamericana.pe/nacionales/414285-cusco-salvo-recuperacion-encuentra-oso-perezoso-hallado-atado-costal | [E] |
| B39 | RAE, Tesoro de los diccionarios históricos: «aí» | https://www.rae.es/tdhle/aí | [V] |
| B40 | UT Austin, léxico de cazadores-recolectores | https://huntergatherer.la.utexas.edu/lexical/feature/213 | [V] |
| B41 | ICMBio, ficha de *B. variegatus* | https://www.gov.br/icmbio/pt-br/assuntos/centros-de-pesquisa/primatas-brasileiros/arquivos/fichas_xenathra/pilosa/ficha_bradypus_variegartus.pdf | [V] |
| B42 | Edentata 11(1), patrones de actividad de *Bradypus* (El Ceibal, Colombia). Solo habla de buscar sombra; ya no se cita para tomar el sol | https://complete.bioone.org/journals/edentata/volume-11/issue-1/020.011.0111/Activity-Patterns-Preference-and-use-of-Floristic-Resources-by-Bradypus/10.1896/020.011.0111.full | [V] |
| B43 | PeerJ (2023), actividad y temperatura. No se pudo leer entero (403); el resumen no habla de tomar el sol; ya no se cita | https://peerj.com/articles/15430 | [NV] |
| B44 | Muramatsu et al. (2022), Journal of Thermal Biology 110:103387, termorregulación de perezosos amazónicos (*B. tridactylus*); propone que se calientan «eligiendo microhábitats más cálidos y tomando el sol» | https://repository.kulib.kyoto-u.ac.jp/dspace/handle/2433/277749 | [E] (hipótesis del estudio) |
| B45 | xenarthrans.org, *B. variegatus*, hábitat y ecología (unos 40 m al día de media) | https://xenarthrans.org/?p=5714 | [V] |
| B46 | Fernández de Oviedo, *Historia general y natural de las Indias*, lib. XII, cap. 24 (traducción del Oviedo Project, Vassar; vista en el extracto del buscador, la página pide acceso) | https://pages.vassar.edu/oviedo/?p=779 | [E] |
| B47 | Animal Diversity Web, *Bradypus variegatus* | https://animaldiversity.org/accounts/Bradypus_variegatus/ | [V] |
| B48 | Sloth Conservation Foundation, el espéculo del macho | http://www.slothconservation.org/blog/male-sloth-speculum | [V] |
| B49 | Edentata 14(1), nota sobre *B. pygmaeus* en el Apéndice II (recoge la Notificación CITES 2013/052: *B. boliviensis*, hoy *B. variegatus*, en el Apéndice II desde 1973) | https://bioone.org/journals/edentata/volume-14/issue-1/020.014.0114/News/10.5537/020.014.0114.full | [E] (visto en el extracto del buscador) |
| B50 | Radio Nacional de Colombia, la hoja de coca y el mambe en el Amazonas (ceniza de yarumo) | https://radionacional.co/cultura/tradiciones/hoja-de-coca-y-el-mambe-tradiciones-de-pueblos-indigenas-en-amazonas | [E] (visto en el extracto del buscador) |

### Nombre (N)

| Código | Fuente | URL | Marca |
|---|---|---|---|
| N1 | DLE, «tino» | https://dle.rae.es/tino | [V] |
| N2 | Dicio, «tino» | https://www.dicio.com.br/tino | [V] |
| N3 | Nostro Figlio, «Titino» | https://www.nostrofiglio.it/nomi/titino | [V] |
| N4 | Wikipedia, Tweety (Piolín) | https://en.wikipedia.org/wiki/Tweety | [E] |
| N5 | EcuRed, Tino (mascota) | https://www.ecured.cu/Tino_(mascota) | [NV] |
| N6 | Ara, Titó | https://es.ara.cat/criaturas/secreto-exito-tito_1_4833872.html | [NV] |

### Locales

- `docs/gdd/00-glosario.md` §1, §3, §5.1, §6.3, §10 y §11.
- `docs/gdd/02-nucleo-reglas-ux.md` §5, §6, §7, §9.1, §12 y §13; `docs/gdd/03-niveles-generacion-dificultad.md` §11 y §12; `docs/gdd/04-comodines-vidas-rescate.md` §2, §5.2, §6.6 y §7.1; `docs/gdd/06-meta-retencion-liveops.md` §2, §4.7, §8 y §9; `docs/gdd/07-arte-tematica-audio.md` §5, §6, §7.8, §8, §9.5, §10 y §11; `docs/gdd/08-arquitectura-tecnica-publicacion.md` §4.3, §4.4 y §8.2; `docs/GDD.md` §2 (#24).
- `docs/research/magic-sort/propuesta-tintalu.md` (#25) y `docs/research/cultura-ribera/propuesta-tinaja.md` (reclama también el D-011).
- `docs/dev/decisiones.md` (D-003, D-008 y D-010); `docs/dev/traspaso/README.md` §6 y §8.
- `docs/research/magic-sort/ficha-magic-sort.md` §4.4, §4.6 y §7.1; `docs/research/09-referencia-magic-sort.md` §1 (punto 2) y «Personajes».
- `docs/research/intro-rio-miquitos/guion.md` §4.6, §11, §12 y §14; `docs/research/fauna/propuesta-fauna.md` c.2 (fila 14), c.3 y §7; `docs/research/frutas-del-rio/propuesta-frutas-del-rio.md` §1; `docs/research/idiomas/propuesta-idiomas.md` §3.
- `docs/art/direccion-visual-v2.md` §3.3, §3.7, §5.3, §6, §6.9, §7 y §11 (C4).
- Código (revisión): `Assets/_Project/Scripts/UI/GameScreen.cs` (panel de victoria de 218/286 dp, cinta de 344 × 72 dp que asoma 38 dp, `ShiftToClear`), `LevelFailedScreen.cs` (tarjeta de 328 dp, cesta de 120 dp) y `SightingCard.cs` (tarjeta del miquito de 60 dp, texto a 10-12).
- Código: `Assets/_Project/Scripts/UI/UiText.cs` (`ExtraCuiaGift`), `Assets/_Project/Scripts/Presentation/ArtSlots.cs` (`capi_idle`), `Assets/_Project/Scripts/UI/LobbyScreen.cs`, `LevelFailedScreen.cs`, `StuckModal.cs`, `GiveUpConfirm.cs`, `Assets/_Project/Scripts/Presentation/MonkeyRig.cs` y `ProceduralArt.Fauna.cs`.
