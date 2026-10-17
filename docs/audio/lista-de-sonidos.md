# Tintalú: lista de sonidos y música

2026-10-15 (actualizada el 2026-10-16 con el audio provisional por código, sección 9, y, tras la ola 4, con el resumen de lo que ya suena en el juego y lo que falta, al principio de la sección 9) · Copia de trabajo para el dueño y los proveedores de audio (compositor y diseñador de sonido). Fuente: GDD 07 §9 y las mecánicas, personajes y pantallas de los demás capítulos. Si esta lista y el GDD no coinciden, manda el GDD hasta que se edite.

## Cómo usar esta lista

El juego necesita unos 61 efectos, el ambiente de 13 mundos (9 en la 1.0) y unas 20 piezas de música (unas 60 contando sus capas). Desde el 2026-10-16 suena con efectos, música adaptativa y ambiente provisionales hechos por código (secciones 5, 6 y 9; D-026; tareas AU1 y AU2).

- Cada fila lleva el **ID** que usará el código, **cuándo suena**, **cómo debe sonar** según el GDD y su **duración**.
- La columna **Tu propuesta** recoge la dirección acústica aprobada: **ASMR amazónico fresco de app** (agua cristalina en vidrio, marimba de chonta y kalimba de calabaza para las notas, maracá, fibra y semillas reales), siguiendo la dinámica medida en *Magic Sort!* (gestos repetidos muy suaves y cortos a −38/−27 dBFS, sellado y premios con brillo y pegada a −17/−14 dBFS, cero barro sonoro).
- Las marcas [P] son propuestas que aún no decidiste; [E], estimaciones.
- Cuando haya propuestas, se convierten en sonidos provisionales para probarlos en el teléfono y en el encargo para el compositor y el diseñador de sonido.

### Escala pentatónica de las 14 frutas (`sfx_select` y `sfx_tint`)

Para que cualquier combinación de frutas en un nivel suene armónica y fresca sin disonancias, las 14 frutas del motor (07 §3) se afinan a **14 notas de una escala pentatónica mayor cálida (Do–Re–Mi–Sol–La)** a lo largo de casi tres octavas (C4 a A6), ordenadas de más oscura/densa a más luminosa (`L*`), con timbre híbrido de **gota de agua (resonancia de burbuja Minnaert) + lámina de marimba de chonta**:

| # motor | ID | Fruta / Agua | L* | Nota pentatónica | Frecuencia (Hz) | Carácter tímbrico |
| --- | --- | --- | --- | --- | --- | --- |
| 9 | `jenipapo` | Huito | 22 | C4 | 261,6 Hz | Gota profunda en cuia grande + madera cálida |
| 13 | `cacao` | Cacao | 26 | D4 | 293,7 Hz | Gota redonda con cuerpo de calabaza |
| 8 | `acai` | Açaí | 32 | E4 | 329,6 Hz | Gota aterciopelada, resonancia media-grave |
| 6 | `camu_camu` | Camu camu | 34 | G4 | 392,0 Hz | «Plic» jugoso con armónico dulce |
| 5 | `urucum` | Achiote | 48 | A4 | 440,0 Hz | Toque cálido de chonta y agua |
| 14 | `agua_negro` | Agua del Río Negro | 50 | C5 | 523,3 Hz | Gota de río cristalina en registro medio |
| 4 | `aguaje` | Aguaje | 64 | D5 | 587,3 Hz | «Plic» brillante y redondo (par del tutorial con Açaí: E4 → D5, una séptima menor; sigue siendo pentatónica) |
| 11 | `lulo` | Lulo | 65 | E5 | 659,3 Hz | Gota fresca y cítrica |
| 7 | `bacaba` | Bacaba | 70 | G5 | 784,0 Hz | Toque perlado de agua clara |
| 10 | `agua_tapajos` | Agua del Tapajós | 70 | A5 | 880,0 Hz | Destello líquido turquesa |
| 3 | `tapereba` | Taperebá | 78 | C6 | 1046,5 Hz | Gota dorada, clara y alegre |
| 12 | `araza` | Arazá | 79 | D6 | 1174,7 Hz | «Plic» vivo de cristal fino |
| 2 | `cupuacu` | Copoazú | 85 | E6 | 1318,5 Hz | Gota delicada y luminosa |
| 1 | `guanabana` | Guanábana | 98 | G6 | 1568,0 Hz | Campana de agua cristalina en el registro más alto |

---

## 1. Tablero y vertido

Son los sonidos que más se oyen: suenan en cada toque y cada vertido, así que deben ser cortos, suaves y agradables tras horas de juego.

| ID | Cuándo suena | Cómo debe sonar (GDD) | Duración | Tu propuesta |
| --- | --- | --- | --- | --- |
| `sfx_select` | Tocar una cuia para elegirla | Toque de agua corto, afinado al color de la fruta de arriba: 14 notas de escala pentatónica, una por fruta | 0,12 s | **Gota cristalina + armónico de marimba de chonta** en la nota de la fruta superior (tabla de arriba), muy suave (−36 dBFS, como en *Magic Sort!*). Si se toca la misma cuia para bajarla, suena una variante apagada una quinta más grave (−35 dBFS). *No aplicada en el cliente:* 02 §4.1 deja la deselección sin audio y manda el GDD; queda como propuesta para el dueño (sección 9). |
| `sfx_pour_start` | Empieza el vertido | Chorro de agua que entra en vidrio | 0,2 s | **«Glup» limpio de cuello de garrafa** + primer impacto de agua fresca en vidrio (sin salpicadura ruidosa, ataque suave de 15 ms). |
| `sfx_pour_loop` | Mientras cae el chorro | Chorro continuo; sube de tono a medida que se llena la cuia | en bucle | **Corriente ASMR de microburbujas en cavidad de vidrio** (resonancia Helmholtz que sube de 380 Hz con 1 dosis a 920 Hz con 4 dosis). En vertidos por tramo (`run`), dura exactamente lo que el chorro (0,45-0,85 s según 1-3 dosis) con ±3 % de variación orgánica para no sonar robótico. |
| `sfx_pour_end` | Termina el vertido | Corte del chorro, última gota | 0,2 s | **Corte limpio del hilo de agua + dos gotitas finales («plic-ploc»)** que asientan el menisco en la nota de la fruta vertida. |
| `sfx_illegal` | Vertido no permitido | Madera neutra, nada que suene a error o castigo | 0,15 s | **Doble toque seco y hueco en calabaza/madera de balsa** («tuk-tuk» grave a 180 y 150 Hz, −28 dBFS), sincronizado con la sacudida de 240 ms. Cero chicharra ni semitono descendente. |
| `sfx_seal` | Una cuia se completa y se tapa | Maracá (dos sacudidas) y tapa de madera que cae | 0,45 s | **Dos sacudidas frescas de maracá de semillas** (0,00 s y 0,13 s) + **«plop» hueco de tapa de calabaza** (0,24 s) rematado con un acorde corto luminoso de 3 notas pentatónicas en kalimba/marimba (−17 dBFS, el premio táctil del tablero). |
| `sfx_river_rise` | Intro del nivel: el río crece | Oleada grave, de fondo (−10 dB) [P] | 0,4 s | **Vaivén cálido de orilla de río** (filtro paso bajo sobre agua en movimiento, 120-450 Hz, sin cresta brusca para no confundir con Pororoca), fundido suave. |
| `sfx_fill` | Intro: se llenan las cuias | Chorro y gorgoteo, uno por fila [P] | 0,40 s (1-20) / 0,25 s (21+) | **Cascada fresca de agua entrando en las cuias de la fila** (gorgoteo claro de vidrio que sube media octava, 1 voz por fila con ligero paneo estéreo). |
| `sfx_fruit_plip` | Intro: cae cada fruta en el agua | «Plic» suave [P] | 0,08 s | **«Plic» redondo de fruta pequeña que rompe la superficie del agua** (burbuja Minnaert rápida de 600 → 1.400 Hz, muy limpia para que 10-20 caídas seguidas suenen a lluvia de frutas y no a ruido). |
| `sfx_tint` | Intro: el agua se tiñe | Nota afinada al color, como `sfx_select`; máximo 6 a la vez | 0,1 s | **Destello de marimba de chonta + cristal** en la nota pentatónica del tinte que florece; al encadenarse de abajo arriba crea un arpegio amazónico espontáneo en cada inicio de nivel. |
| `sfx_win_toada` | Victoria | Toada corta de carimbó: percusión y coro sin letra | 1,5-3 s | **Fanfarria festiva de 2,2 s**: redoble cálido de tambor curimbó, maracás brillantes, arpegio de guitarra/flauta de caña y acorde vocal cálido («aah/ooh» sin letra) en Do mayor pentatónico. |
| `sfx_confetti` | Victoria, con el confeti de plumas | Aleteo de guacamayos | 1,0 s | **Despliegue de alas en abanico** (3-4 batidas sedosas de plumas grandes en estéreo + brisa de dosel, sin graznido estridente). |
| `sfx_stuck` | Aguas estancadas (sin jugadas) | Zumbido grave de agua quieta, sin golpe dramático | 1,5 s | **Ondulación grave de remanso** (cuenco de agua + marimba baja que desciende suavemente sol→mi→do en 220-130 Hz con una última gota solitaria). Calmo, invita a pensar o usar Marea sin castigar. |

En la intro, como mucho un chillido corto de mono ardilla, muy bajo (−18 dB); nunca chillidos mientras se llenan las cuias.

## 2. Comodines, economía e interfaz

Cada comodín tiene su sonido propio, ligado a su animal o elemento; los de interfaz son de madera y semillas, nunca «clics» electrónicos.

| ID | Cuándo suena | Cómo debe sonar (GDD) | Duración | Tu propuesta |
| --- | --- | --- | --- | --- |
| `sfx_undo` | Marea (deshacer) | Ola que retrocede, como el chorro al revés | 0,4 s | **Agua que se recoge en reversa** (envolvente invertida de burbujas que suben hacia la cuia origen + deslizamiento ascendente suave de marimba de agua). |
| `sfx_capi_paddle` | Cuia de rescate (+1 cuia): llega Capi | Pasos en arena, remo en el agua, tintineo de vidrio y chasquido corto de capibara | 0,8 s | **Dos pasitos húmedos en orilla + palada de remo en agua clara + «clinc» de cuia de vidrio nueva** y el característico **chasquido/gorjeo corto y simpático de capibara**. |
| `sfx_hint` | Morpho (pista) | Aleteo suave y campanita de madera | 0,5 s | **Tres batidas sedosas de alas de mariposa + destello cristalino de campanilla de bambú/chonta** (intervalo de quinta justa Sol5→Re6, mágico pero orgánico). |
| `sfx_shuffle` | Pororoca (mezclar) | Ola grande, remolino y 14 notas rápidas | 0,9 s | **Oleada envolvente de río + giro de agua + glissando ascendente ultrarrápido de las 14 notas pentatónicas** en marimba de chonta y gotas de cristal. |
| `sfx_reveal` | Luciérnaga (revelar Aguas turbias) | Campanilla cálida, chispa y burbuja que se aclara | 0,5 s | **Zumbido luminoso suave + chispa cálida de kalimba aguda** (Mi6→La6) + **burbuja profunda («boiada»)** que emerge y aclara el agua turbia. |
| `sfx_skip` | Salto del Boto (saltar nivel) | Salto, chapuzón y exhalación de delfín | 1,2 s | **Impulso de agua al emerger + exhalación de boto en el aire (grabación con licencia, 07 §9.5; hoy, provisional por código) + chapuzón limpio y alegre** con cola de burbujas. |
| `sfx_restart` | Volver a la orilla (reiniciar) | Agua que se recoge y madera | 0,5 s | **Barrido corto de agua que vuelve al cauce + toque firme de madera de canoa** en la orilla. |
| `sfx_river_drop` | El río bajó (nivel aliviado tras 3 fallos) | Agua que baja y exhalación de Rosa | 1,0 s | **Nivel de agua que desciende con alivio (glissando líquido grave-medio) + soplo cercano y amable de Rosa** que transmite «ahora sí, más tranquilo». |
| `sfx_seeds_tick` | Contador de semillas que sube | Semillas que caen en una cesta; 1 por cada 10 semillas, máximo 20 | 0,05 s cada uno | **«Tic» orgánico de semilla dura (huayruro/açaí) cayendo sobre fibra trenzada** (con microvariación de tono +1 semitono progresivo a lo largo de la cuenta para dar emoción de cosecha). |
| `sfx_basket_open` | Canasta (premio) | Fibra que se abre y frutas | 0,8 s | **Crujido cálido de cesto de arumã al destaparse + caída jugosa de frutas y semillas** con un acorde corto de marimba y maracá. |
| `sfx_streak_up` | Crecida: sube la racha | Chillido de nutrias | 0,8 s | **Subida de nivel de agua + gorjeo alegre y corto de nutria gigante** (su vocalización social de contacto, simpática y viva, nunca el grito áspero de alarma) con toque de marimba. |
| `sfx_streak_shield` | Hoja escudo protege la racha | Hoja que se despliega | 0,6 s | **Despliegue amplio de hoja gigante de *Victoria amazonica* sobre el agua** («fwoosh» vegetal + apoyo grave de agua que amortigua el golpe). |
| `sfx_canoe_lost` | Se pierde una canoa (vida) | Canoa que vuelca, suave | 0,6 s | **Madera que se inclina en el agua + chapoteo suave y nota grave de calabaza** (sin dramatismo ni sonido de rotura). |
| `sfx_canoe_back` | Se recupera una canoa | Remo | 0,6 s | **Palada limpia de remo de madera en el río + gotas frescas** y nota ascendente cálida. |
| `sfx_ui_tap` | Cualquier botón | Madera seca | 0,08 s | **«Toc» corto y redondo de piedra de río / madera de chonta pulida** (fundamental en 420 Hz, decaimiento en 45 ms, cero clic metálico). |
| `sfx_ui_back` | Atrás o cerrar | Madera más grave | 0,1 s | **«Tuc» suave de calabaza/madera grave** (280 Hz, un cuarto más bajo que `sfx_ui_tap`, cálido y discreto). |
| `sfx_ui_buy` | Compra en la tienda | Semillas en una cesta | 0,4 s | **Puñado generoso de semillas que caen en cumbuca de madera + doble toque alegre de marimba** (Do5→Sol5). |

## 3. Mecánicas y personajes

Cada mecánica suena cuando se desbloquea, en el golpe de la tapa al sellar. Las que decían «sin definir» llevan aquí su primera propuesta completa alineada con 02 §9.1. Los ID de esta sección son [P]: se fijan al encargarlos (07 §9.5).

| ID [P] | Cuándo suena | Cómo debe sonar (GDD) | Duración | Tu propuesta |
| --- | --- | --- | --- | --- |
| `sfx_mech_hoja` | Se desbloquea la Hoja | Hoja seca | 0,4-0,6 s | **Roce crujiente y fresco de hoja de palma que se levanta** con un soplo de brisa (0,45 s). |
| `sfx_mech_matapi` | Se abre el Matapi (trampa) | Crujido de fibra y tirón de cuerda | 0,8 s | **Tejido de varillas de arumã que cede («cric-crac» orgánico) + tirón corto de fibra vegetal** al cerrarse/completarse la trampa. |
| `sfx_mech_liana` | Se suelta la Liana con fruta | «Ding» y rama que cruje | 0,6 s | **Nota cristalina de la fruta llave («ding» de marimba aguda) + latigazo elástico de bejuco/liana que se desata** y libera la cuia. |
| `sfx_mech_liana_cuenta` | Baja la cuenta de la Liana de cuenta | Sin definir | 0,15 s [P] | **«Tac» seco de nudo de bejuco que afloja una vuelta** (madera fibrosa tensa + toque de semilla, corto para no tapar el sellado que lo dispara). |
| `sfx_mech_remolino` | El chorro pasa por el Remolino | Succión y burbujas que suben en la otra cuia | 0,5 s | **Giro líquido de succión suave («shloop» grave-medio) + rosario de burbujas ascendentes** al emerger la dosis por el fondo de la cuia pareja. |
| `sfx_mech_barro` | Se limpia el Barro | Chapoteo | 0,5 s | **Chorro de agua fresca que barre arcilla húmeda («splash-shhh»)** terminando en un destello limpio de vidrio despejado. |
| `sfx_mech_lluvia` | Llueve sobre el tablero | Sin definir | 0,40 s [P] | **Ráfaga corta de lluvia amazónica sobre hojas + «plip» redondo de gota gruesa** al sumar la dosis de agua en la cuia (sin truenos). |
| `sfx_mech_pirana` | La Piraña muerde o se lleva fruta | Sin definir | 0,40 s [P] | **Coletazo ágil en el agua + «chasquido» limpio y juguetón bajo el agua («chomp-blup»)** al llevarse la dosis a la cueva. Si el mordisco falla (cuia sellada), solo un «blup» corto al asomarse y volverse. |
| `sfx_mech_brote` | Se abre el Brote (de una o dos hojas) | Sin definir | 0,45 s [P] | **Tallo tierno que germina con un «pop» húmedo vegetal + despliegue fresco de hojitas** (con el Brote de dos hojas, doble «pop» ligero en quinta ascendente). |
| `sfx_mech_flor` | Se abre la Flor de vitória-régia | Sin definir | 0,65 s [P] | **Pétalos grandes que se abren sobre el agua con roce sedoso + acorde luminoso de dos notas** en kalimba/agua al liberarse la cuia. |
| `sfx_mech_hoja_atada` | Se suelta la Hoja atada | Sin definir | 0,50 s [P] | **Amarre de fibra que se suelta («tzik» elástico) + hoja de palma que se desprende con brisa**, uniendo la textura de Liana y Hoja. |
| `sfx_tinaja_select` | Elegir la Tinaja | «Toc» grave de barro | 0,12 s | **«Toc» profundo y mate de vasija de arcilla cocida** (resonancia hueca en 190 Hz y 520 Hz, muy distinto del cristal de la cuia). |
| `sfx_tinaja_scoop` | Sacar agua de la Tinaja | «Glup» de calabaza en el agua; nunca burbujas ni hervor | 0,30 s | **«Glup» limpio y redondo de cazo/cuia chica que recoge agua fresca** de la boca de barro (un solo movimiento líquido laminar, cero burbujeo ni efervescencia). |
| `sfx_tinaja_reveal` | Se revela la boca de la Tinaja | Gota | 0,14 s | **Gota única y clara que resuena dentro del cuello de barro** (con eco corto de vasija), afinada a la fruta que asoma en la boca. |
| `sfx_tinaja_release` | Se suelta su cuia chica | Roce de fibra y «toc» hueco | 0,8 s | **Roce de cuerda/fibra en las varas del jirau + «toc» hueco de calabaza** cuando la cuia chica baja a su ranura para jugarse normal. |
| `sfx_sloth_nod` | TiTino asiente | «Mmm» suave y grave, sin palabras | 0,35 s | **Murmullo nasal cálido y grave («mmm» tierno en 190→220 Hz, boca cerrada)**, sereno y acogedor. |
| `sfx_sloth_happy` | TiTino celebra | Suspiro feliz | 0,45 s | **Suspiro suave y contento («aaah-hm» con aire cálido y sonrisa)** + leve roce de hojas de su embaúba; nunca silbido agudo (llamada de angustia de cría). |
| `sfx_sloth_branch` | Entra la rama de TiTino | Roce de hojas | 0,35 s | **Balanceo flexible de rama de embaúba + roce fresco de hojas grandes**. |
| `sfx_sloth_yawn` | TiTino se duerme | Bostezo muy suave | 0,60 s | **Bostezo chiquito de aire suave descendente** que termina en dos chasquidos lentos al acomodarse a dormir. |
| `sfx_rosa_surface` | Rosa (delfín rosado) asoma | Exhalación real de boto | 1,0 s | **Ondulación de agua + soplo de espiráculo de boto (*Inia geoffrensis*; grabación con licencia, 07 §9.5; hoy, provisional por código)**, cálido y cercano, seguido de chapoteo suave al sumergirse. |
| `sfx_monkey_chirp` | Tropa de miquitos en la intro | Chillido corto de mono ardilla, muy bajo; uno como mucho | 0,15 s | **Trino agudo y dulce de mono ardilla (*Saimiri*), parecido a un pajarito** (3,2-4,5 kHz, muy bajo a −18 dB, solo al entrar en la rama). |

Para la 1.1 quedan tres más: Aguapé (agua que se abre), Palizada (chapoteo y madera que cruje) y Cocha (agua que se corta). Ningún personaje habla: solo sonidos, para no doblar a cada idioma.

## 4. Mapa del río, llegadas y pantallas especiales

Estos sonidos marcan momentos de viaje y descubrimiento; salen pocas veces, así que pueden ser más ricos. La línea de fruta de cada victoria va **sin sonido** a propósito, porque cansaría.

| ID [P] | Cuándo suena | Cómo debe sonar (GDD) | Duración | Tu propuesta |
| --- | --- | --- | --- | --- |
| `sfx_fact` | Llegada a una parada (victoria de los niveles 20, 30, 40…) | Madera y vidrio, sin papel [P] | 0,3 s | **Toque noble de tablilla de madera clara + armónico cristalino de vidrio** (acorde de quinta Do5-Sol5, limpio y sin roce de hojas de papel). |
| `sfx_world_banner` | Banner de mundo nuevo (41, 81, 141…) | Grito de la piha, la firma sonora del juego; **solo aquí**, nunca en bucle | 1-2 s | **El canto de tres sílabas de la piha gritona («qui-qui-yo»; grabación con licencia, 07 §9.5; hoy, provisional por código)** con reverberación natural de dosel amazónico, arropado por una brisa de río (1,6 s). |
| `sfx_map_paddle` | Capi rema en el Mapa | Remo de Capi (el mismo de `sfx_capi_paddle`) | 0,8 s | **Dos paladas suaves de remo en agua tranquila** (reutiliza la capa de remo y agua de `sfx_capi_paddle`). |
| `sfx_map_sample` | Se llena una muestra en el Mapa | Acorde de vidrio | 0,45 s | **Agua que colma la mini cuia del sitio + acorde luminoso de 3 copas/cuias de vidrio** (Do5-Mi5-Sol5) que confirma la parada alcanzada. |
| `sfx_map_rosa` | Rosa en el Mapa | Exhalación de Rosa | 1,0 s | **El mismo soplo amable y chapoteo corto de `sfx_rosa_surface`**, ligeramente más suave (−3 dB) para acompañar el mapa. |
| `sfx_map_pequepeque` | Opcional: barcas en el Mapa | Motor peque-peque (canoa con motor de eje largo) | 0,9 s [P] | **«Pec-pec-pec-pec» rítmico, redondo y lejano** (filtrado en agudos y a −22 dB con chapoteo de estela, solo al tocar una barca o pasar junto a un puerto para dar color ribereño sin ruido mecánico). |
| `sfx_fruit_new` | Tarjeta «Fruta nueva» | Sin definir | 0,70 s [P] | **Caída jugosa de fruta en agua clara + arpegio ascendente de 3 notas en marimba de chonta y kalimba**, rematado en la nota propia de la fruta descubierta. |
| `sfx_mech_card` | Tarjeta previa de una mecánica nueva | Sin definir | 0,35 s [P] | **Golpe suave de marco de madera + sacudida corta de semillas/maracá** (en tarjeta de Difícil/Muy difícil añade un armónico grave de tambor de agua, elegante y sin alarma). |
| `sfx_morpho_idle` | Pista del Morpho por inactividad | Sin definir (¿el mismo aleteo de `sfx_hint`, más bajo?) | 0,35 s [P] | **Sí: el aleteo sedoso de `sfx_hint` a mitad de volumen (−10 dB) con una sola nota suave de bambú**, para sugerir sin apurar al jugador. |
| `sfx_store_open` | Se abre la tienda o el Mercado | Sin definir | 0,55 s [P] | **Despliegue de estera/cesto de fibra + tintineo cálido de botellas de vidrio y semillas** (guiño al mercado de Ver-o-Peso, sin voces). |

## 5. Ambiente de cada mundo

Cada mundo tiene un fondo sonoro de 3 capas que se mezclan solas: el agua del río, una lluvia que aparece en algunos niveles y animales sueltos cada 20-45 s. La 1.0 usa los mundos 1-9; los 10-13 llegan después.

| Mundo | Agua | Lluvia (niveles con lluvia) | Animales | Tu propuesta |
| --- | --- | --- | --- | --- |
| 1 Estuario | Río ancho y lento, marea | 20 % | Garzas, mercado lejano sin voces que se entiendan, piha ocasional | **Agua amplia con vaivén de marea suave**; brisa de palmeras de açaí, vuelo de garzas y murmullo muy lejano de muelle/madera al alba (cero voces inteligibles). |
| 2 Tapajós | Olas suaves de playa | 10 % | Tucanes, piha, exhalación de Rosa | **Olas pequeñas y cristalinas sobre arena blanca (Alter do Chão)**; canto lejano de tucán al mediodía y soplo ocasional de Rosa. |
| 3 Manaos | Dos corrientes, una grave y una aguda | 25 % | Botos, guacamayos | **Textura binaural sutil**: corriente densa grave a un lado (Solimões) y rizo cristalino al otro (Río Negro), con botos y pareja de guacamayos cruzando alto. |
| 4 Trapecio | Río tras la lluvia, goteo | 50 % | Pericos al atardecer, grillos de día | **Goteo fresco y espaciado de hojas de selva sobre el río**; grillos diurnos suaves y, en la parada 9 (Leticia al atardecer), algarabía lejana y cálida de pericos. |
| 5 Loreto | Agua quieta de espejo | 30 % | Pirarucú que respira (burbuja grave), piha | **Calma de cocha/espejo de agua (Pacaya-Samiria)** con ondas mínimas; cada 30-45 s la respiración profunda y grave del pirarucú («boiada»). |
| 6 Oriente | Laguna negra de noche | 40 % | Ranas, grillos, búho lejano | **Noche serena y envolvente en Cuyabeno**: pulso suave de grillos y ranas arborícolas (afinadas para no chocar con la música) y ulular lejano de búho. |
| 7 Madre de Dios | Río medio | 20 % | Guacamayos de la collpa, nutrias | **Corriente viva de mañana en el Tambopata**; aleteos y llamadas lejanas en la collpa y gorjeos de familia de nutrias en el lago. |
| 8 Madidi | Cascada lejana | 30 % | Coro de aves de altura | **Rumor sedoso y fresco de cascada lejana con bruma** + cantos cristalinos de tangaras y aves de bosque nublado. |
| 9 Ucayali | Vapor que sisea y río | 20 % | Paiche, piha | **Meandro ancho con siseo cálido y suave de vapor (Shanay-Timpishka)** en niveles especiales + chapoteo profundo de paiche. |
| 10 Alto Negro | Río negro lento | 15 % | Noche: ranas, insectos, agua | **Corriente negra profunda bajo cielo estrellado**: coro nocturno aterciopelado, agua contra roca granítica. |
| 11 Escudo de Guayana | Viento de sabana y río | 10 % | Hoatzín (graznido áspero) | **Brisa abierta de sabana y tepuyes sobre el río**; llamada curiosa del hoatzín suavizada en mezcla para que aporte carácter sin raspar. |
| 12 Invitados del Orinoco | Río rápido sobre piedra | 10 % | Guacamayos, viento de tepuy | **Agua clara y alegre corriendo sobre lecho de piedra (Caño Cristales)** + viento alto de meseta y guacamayos. |
| 13 El origen | Torrente de altura, viento frío | 10 % | Viento, aves lejanas | **Hilo de agua pura y cristalina entre rocas andinas (Mismi/Apurímac)** con brisa fría de puna y eco limpio de altura. |

Piezas que hay que grabar o comprar: unas 13 capas de agua, 2-3 lluvias (fina, media, fuerte) y unos 25 animales. La piha, el boto, la nutria, el hoatzín y las ranas necesitan grabaciones con licencia comercial.

**Estado en el cliente (tarea AU2, 2026-10-16): ambiente provisional por código** (`AmbientSynth` y `AmbientPlan`, en `AF.Audio`):

- **Agua:** un bucle de 20 s por mundo (1-9) con el carácter de la tabla: dos bandas de ruido filtrado (el río y su brillo), la marea del 1 (vaivén de 10 s), las olas del 2 (6,7 s), las dos corrientes del 3, el goteo del 4, el espejo del 5 con alguna onda, la laguna del 6, la cascada lejana del 8 (sin agudos) y el siseo de vapor del 9. Se cierra con un fundido cruzado de 2 s de potencia constante: sin costura y sin la «respiración» de ~1 Hz del simulador del laboratorio, que regeneraba el ruido cada 1,05 s. Los mundos 10-13 suenan como el 1 hasta tener su perfil [P].
- **Lluvia:** un bucle de 10 s (siseo a 2,4 kHz, llovizna fina y alguna gota gruesa) que entra con un fundido de 1 s en los niveles que la tienen: un número fijo de cada nivel por debajo de la probabilidad de su mundo (la tabla de arriba) por `audio_ambient_rain_scale` (1,0; 08 §9). El mismo nivel tiene siempre la misma lluvia.
- **Animales:** uno cada 20-45 s con los efectos provisionales que encajan, 10 dB por debajo de su pico: aleteo (`sfx_confetti`) por las garzas del 1, los pericos del 4, los guacamayos del 3 y el 7 y las aves del 8; la exhalación de Rosa (`sfx_map_rosa`) en el 2, los botos del 3, el pirarucú del 5 y el paiche del 9. Nunca la piha, que solo va en el banner. Oriente (6) no tiene ninguno hasta grabar ranas, grillos y búho [P].
- **Nivel y mezcla [P]:** agua a −39 dBFS RMS y lluvia a −43; el agua, unos 6 dB por debajo del lecho de música (que ronda los −33 dBFS RMS). Es una propuesta propia: no sale de 07 §9.4, cuyo «lecho ambiental» a −26 LUFS es la capa 1 de la música (07 §9.3), no el agua ni la lluvia del paisaje sonoro de 07 §9.1. El ambiente va con el ajuste **Música** y baja 6 dB con ella durante la toada y los modales [P]; en el inicio no hay río.

## 6. Música

La música crece con el jugador: un lecho tranquilo de 60-90 s en bucle, sin ritmo marcado, y una percusión suave que gana una capa cada vez que se sella el 25 % de las cuias (sube el volumen, nunca el tempo). En los niveles Muy difíciles el lecho pasa a su versión nocturna, más grave; nunca música tensa de reloj.

| Pieza | Dónde suena | Estilo e instrumentos (GDD) | Tu propuesta |
| --- | --- | --- | --- |
| Lecho mundo 1 Estuario | Niveles 1-10 | Carimbó: tambor curimbó y maracás | **Alba cálida en Belém**: armónicos suaves de guitarra de cuerdas de nylon y banjos/flauta de madera en notas largas, con pulso de curimbó grave y maracás que entran por capas al sellar. |
| Lecho mundo 2 Tapajós | 11-40 | Carimbó de playa, más lento: maracá y guitarra [P] | **Brisa luminosa de Alter do Chão**: arpegios espaciados de guitarra acústica con armónicos naturales como gotas de sol sobre el agua clara y shaker de semillas. |
| Lecho mundo 3 Manaos | 41-80 | Toada amazónica propia, sin nada de Parintins | **Dos corrientes musicales que dialogan**: bajo cálido y redondo (Solimões) con melodía cristalina de marimba/cuerda punteada (Río Negro); percusión amazónica original, cero citas a Garantido/Caprichoso. |
| Lecho mundo 4 Trapecio | 81-140 | Guitarra acústica y flauta de caña, sin carácter ritual [P] | **Mañana fresca tras la lluvia en Leticia/Tarapoto**: guitarra acústica cálida en compás mecido y frases cortas y dulces de flauta de caña ribereña (puramente melódica y pastoral, nada ritual). |
| Lecho mundo 5 Loreto | 141-200 | Cumbia amazónica suave, tempo bajo [P] | **Atardecer en la selva de espejos**: guitarra limpia con reverb de resorte suave y órgano cálido en acordes largos flotantes; el güiro de calabaza y los bongós entran muy suaves al sellar cuias. |
| Lecho mundo 6 Oriente | 201-300 | Percusión de semillas y cuencos de agua, nocturno [P] | **Noche hipnótica y tranquila**: cuencos de agua afinados, kalimba de calabaza y cascabeles de semillas secas con pad cálido nocturno. |
| Lecho mundo 7 Madre de Dios | 301-420 | Guitarra y cajón ligero [P] | **Amanecer peruano**: guitarra criolla/amazónica en arpegios abiertos y cajón peruano tocado con yemas de los dedos (suave y redondo, sin golpes secos fuertes). |
| Lecho mundo 8 Madidi | 421-560 | Charango suave y bombo [P] | **Transición selva-Andes**: charango dulce arpegiado con pulgar (sin rasgueo estridente) y bombo legüero profundo y lejano como latido cálido. |
| Lecho mundo 9 Ucayali | 561-700 | Cumbia amazónica / chicha: Farfisa y guitarra con wah-wah | **Homenaje fresco a Pucallpa**: órgano Farfisa suave y guitarra eléctrica limpia con wah-wah sedoso a tempo relajado, alegre y psicodélico-tropical sin saturar. |
| Lechos mundos 10-13 | 701 en adelante, después de la 1.0 | Pad nocturno y maracá · percusión de calabaza y voces sin letra · arpa y maracas llaneras · quena y zampoña (solo en el 13) | **Gradiente hacia el origen**: del misterio nocturno del Alto Negro (10) y los coros suaves del Rupununi (11), al arpa cristalina como agua de Caño Cristales (12) y el aire puro de quena y zampoña al llegar al nevado Mismi (13). |
| Capas de percusión (×4 por mundo) | Se suman al sellar el 25, 50, 75 y 100 % | Percusión suave del instrumento de cada mundo | **25 %:** semilla/shaker sutil · **50 %:** madera/cumbuca · **75 %:** tambor grave suave (curimbó/cajón/bombo) · **100 %:** brillo melódico rítmico antes de la victoria. |
| Variante nocturna de cada lecho | Niveles Muy difíciles | Menos agudos, más graves | **Filtro paso bajo cálido (corte suave en 2,2 kHz) + octava grave en el pad/bajo**, dando atmósfera de «Noche de jaguar» concentrada y misteriosa pero nunca estresante. |
| Toada de victoria | Al ganar | Percusión y coro sin letra, 2-3 s (es `sfx_win_toada`) | **Misma pieza que `sfx_win_toada`**, en la tonalidad del lecho (Do mayor / La menor pentatónico) para que funda sin choque armónico con cualquier mundo. |
| Tema de menú | Inicio, Mercado, pantalla de la Crecida | Cumbia amazónica ligera a 90-100 BPM: Farfisa y guitarra con wah-wah suave | **Melodía luminosa, hospitalaria y memorable a 94 BPM**: guitarra cristalina con wah suave, bajo redondo, maracá/güiro de calabaza y órgano Farfisa cálido que invita a viajar por el río. |
| Stingers | Momentos cortos (banner de mundo, llegada, comodines) | Golpes musicales de 1-2 s; lista por cerrar | **5 stingers cerrados [P]:** (1) *Llegada a parada (`10k`)*: acorde de guitarra + marimba de agua (1,2 s); (2) *Banner de mundo*: acorde abierto de bienvenida bajo la piha (1,8 s); (3) *Hito de la Crecida (3/6/10)*: subida festiva de carimbó/cumbia (1,2 s); (4) *El río bajó*: suspiro armónico descendente-ascendente de alivio (1,0 s); (5) *Serie de fruta completada (20/20)*: florecimiento de marimba y flauta (1,2 s). |

**Estado en el cliente (tarea AU2, 2026-10-16): música adaptativa provisional por código** (`MusicSynth` y `MenuThemeSynth`, en `AF.Audio`):

- **Lecho de cada mundo:** un pad sostenido de 63 s en bucle, sin ataques ni pulso: seis acordes de la escala del mundo del laboratorio (la raíz y las notas dos y cuatro pasos más arriba, más el bajo), uno cada 10,5 s, que se funden en 4 s con potencia constante. Cada nota suma sus armónicos 1, 2 y 4 con un número entero de ciclos por bucle, así no hay costura ni batidos entre notas. Sustituye al lecho de AU1, que eran notas de marimba cada 1,05 s y marcaban pulso.
- **Capas de percusión:** las del laboratorio (maracá del 25 %, madera del 50 % y tambor grave del 75 %), cada una en un bucle de 10,5 s que divide el del lecho y arranca a la vez. Entran y salen con un fundido de 1 s según las cuias selladas (la primera al sellar la primera cuia y otra cada 25 %, 07 §9.3), hasta `audio_music_percussion_layers` (2 por defecto; 08 §9 admite 0-3).
- **Variante nocturna (Muy difícil):** el mismo pad una octava más grave y sin nada por encima de 1,3 kHz; el peso pasa al segundo armónico para que el altavoz del teléfono la siga dando: al menos una cuarta parte de su energía queda por encima de 300 Hz (−6 dB como mucho; del 27 % de Oriente al 65 % de Trapecio, frente al 54-91 % de día). Las capas no cambian: 07 §9.3 pide la variante del lecho, y las maderas una octava más graves ya no salen por ese altavoz.
- **Tema de menú (inicio):** un bucle de 8 compases a 94 BPM: pad Do, Sol, La menor y Fa, bajo de cumbia en el 1 y el 3, maracá en corcheas con el acento a contratiempo y una frase de marimba en la pentatónica de Do. El Mercado aún no existe en el cliente: cuando exista, usa el mismo tema.
- **Mezcla [P]:** toda la música con un pico de −20 dBFS con todas las capas (el lecho, a unos −33 dBFS RMS). Al cambiar de pieza (inicio, otro mundo o la variante nocturna), lo que suena se funde en 0,8 s. Baja 6 dB durante la toada y los modales, se calla durante los anuncios y vuelve en 0,5 s, se calla en el modo silencio del teléfono y el ajuste Música la apaga al momento.
- **Pendiente (07 §9.4):** si el dueño quiere seguir la letra de 07 §9.4, el lecho de música debe quedar unos 6 dB por debajo de la música completa (lecho a −26 frente a −20 LUFS). Hoy nadie lo mide: las cifras provisionales son RMS y picos en dBFS, no LUFS. Se mide cuando llegue la mezcla del compositor.
- **Coste (Editor del escritorio x64):** se genera en un hilo de trabajo al pedirla por primera vez y solo si Música está encendida (con Música apagada no se genera nada; al encenderla, se genera entonces). El lecho y sus capas de un mundo tardan unos 170 ms y ocupan 5.426 KiB en float (1,39 millones de muestras); el tema de menú, unos 340 ms y 1.759 KiB; el agua de un mundo, unos 10 ms y 1.723 KiB; la lluvia, 861 KiB. Solo se guarda lo último de cada clase: al ir al inicio, el lecho y el agua se funden, se paran y se guardan, y al volver a un nivel del mismo mundo y variante suenan sin generarse otra vez; se sueltan al pedir otro mundo o variante. En el teléfono, por medir.
- La sonda `AF_STATE music` y `AF_STATE ambient` está en `docs/dev/prototipo-v0.md`.

**Prohibido en el encargo al compositor:** ícaros, flautas de Yuruparí, cantos rituales, quena o zampoña fuera del mundo 13, y melodías o colores de Garantido y Caprichoso (Parintins). Ninguna voz hablada.

## 7. Vibración, mezcla y reglas

La vibración usa los efectos estándar de Android (tick y click) y se puede apagar en Ajustes; nunca vibra al rechazar un vertido.

| Vibración | Cuándo | Intensidad |
| --- | --- | --- |
| `haptic_seal` | Cuia sellada | Ligera, 20 ms |
| `haptic_win` | Celebración de la victoria | Media |
| `haptic_capi` | Llega Capi con la cuia de rescate | Media |
| `haptic_boto` | Salto del Boto | Media |
| `haptic_back_blocked` | Atrás no puede cerrar un aviso obligatorio | Corta |

**Mezcla y ajustes [P]:**

- Música a −20 LUFS, lecho de ambiente a −26 LUFS y efectos con picos de −6 dBTP como máximo; la música, 6 dB por debajo de los efectos.
- **Jerarquía dinámica de efectos (como en *Magic Sort!*):**
  - *Gestos continuos* (`sfx_select`, `sfx_pour_*`, `sfx_tint`, `sfx_fruit_plip`, `sfx_ui_tap`): **−36 a −27 dBFS** (suaves, nunca cansan).
  - * Mecánicas, avisos y comodines* (`sfx_mech_*`, `sfx_undo`, `sfx_hint`, `sfx_stuck`): **−24 a −18 dBFS**.
  - *Premios y sellados* (`sfx_seal`, `sfx_win_toada`, `sfx_world_banner`, `sfx_basket_open`): **−17 a −12 dBFS** (contraste claro de recompensa).
- El lecho baja 6 dB durante la toada y los avisos.
- Sonido, Música y Vibración vienen encendidos y se apagan por separado en Ajustes.
- Respeta el modo silencio del teléfono; todo se calla durante los anuncios y vuelve con un fundido de 0,5 s.
- La piha solo suena en el banner de mundo, nunca en bucle.

**Formato:** música en Vorbis en streaming (un bucle de 2-3 min pesa unos 1,5 MB); efectos mono, comprimidos. Todo debe caber en el límite de 80 MB del AAB.

## 8. Resumen: cuánto hay y quién lo hace

Para la 1.0 hacen falta unos 61 efectos, el ambiente de 9 mundos y unas 60 piezas de música contando las capas. El GDD cuenta solo unos 35 efectos en su presupuesto de sonido; esta lista casi lo duplica.

| Bloque | Cantidad 1.0 | Quién | Presupuesto del GDD |
| --- | --- | --- | --- |
| Efectos de tablero, comodines e interfaz (§1-2) | 30 | Diseñador de sonido, con foley propio y biblioteca comercial | 600-1.500 USD (calculado para unos 35 efectos) |
| Efectos de mecánicas, personajes y Mapa (§3-4) | 31 (+3 en la 1.1) | Diseñador de sonido; los 11 que estaban sin definir ya tienen propuesta arriba | dentro de la fila anterior, a revisar |
| Ambiente (§5) | 9 aguas, 2-3 lluvias, unos 25 animales | Biblioteca con licencia comercial; la piha conviene grabarla | partida «grabaciones de fauna», sin cifra propia |
| Música (§6) | 9 lechos, 36 capas de percusión, 9 variantes nocturnas, toada, tema de menú y 5 stingers | Compositor, con cesión total de derechos; músicos regionales si se puede | 1.500-4.000 USD |

**Mientras tanto:** desde el 2026-10-16 (D-026, tarea AU1) el juego ya suena con sonidos provisionales hechos por código, con los mismos algoritmos del laboratorio interactivo `docs/audio/laboratorio-sonidos.html` y los arreglos de su revisión (sección 9). Los sonidos definitivos sustituyen a los provisionales sin tocar los ganchos del código.

## 9. Estado en el cliente: audio provisional por código (D-026, tarea AU1)

**Qué está ya en el juego y qué es provisional** (2026-10-16, ola 4 de la tanda 4 integrada; código en `Assets/_Project/Scripts/Audio/`):

| Bloque | Ya suena en el juego | Aún no suena | Provisional o definitivo |
| --- | --- | --- | --- |
| Efectos (§1-§4) | 38 de los 61, con su gancho: el tablero y el vertido, la intro, la victoria, Aguas estancadas, Marea, Capi, la Luciérnaga, Volver a la orilla, El río bajó, las canoas, la interfaz, la tienda, la tarjeta previa, la llegada a la parada, el banner de mundo, las mecánicas de 1-200 y la Tinaja al elegirla. La Hoja atada tiene su gancho en `AudioBoard.Step` y su vista desde VA1a (ola 4), pero ningún test comprueba aún que suene | 22: Morpho y su versión por inactividad, Pororoca, el Salto del Boto, la Canasta, la Crecida y la Hoja escudo, la Liana de cuenta, la Flor, la Tinaja al sacar, revelar y soltar, los gestos de TiTino, Rosa, el Mapa del río y la tarjeta «Fruta nueva». Su clip existe; falta la pantalla, la mecánica o la llamada (columna «Dónde suena hoy») | Todos provisionales por código, también la piha y el boto |
| Vibración (§7) | `haptic_seal`, `haptic_win`, `haptic_capi` y `haptic_back_blocked`, con el permiso `VIBRATE` aprobado por el dueño (D-026) | `haptic_boto`, con el Salto del Boto | Los efectos predefinidos de Android; se pueden quedar |
| Ambiente (§5) | Agua de los mundos 1-9, lluvia por nivel y animales con efectos provisionales (AU2) | Animales de Oriente (6) y perfiles propios de los mundos 10-13 (suenan como el 1) | Provisional por código, hasta las grabaciones con licencia |
| Música (§6) | Lecho de cada mundo 1-9, capas de percusión, variante nocturna y tema de menú (AU2) | Los 5 stingers, la pantalla de la Crecida y el Mercado (aún no existen) | Provisional por código, hasta el encargo al compositor |
| Ajustes | Sonido, Música y Vibración, encendidos por defecto; el ambiente va con Música [P] | — | Definitivo (07 §9.4) |

Las columnas «Tu propuesta» de las secciones 1-7 siguen siendo el encargo para el compositor y el diseñador de sonido: el audio provisional no las sustituye.

Desde el 2026-10-16 el juego suena con los 61 efectos del laboratorio, portados 1:1 a C# (`AF.Audio`, sin motor) y generados al arrancar en un hilo de trabajo (0 MB en el AAB). Todos son **provisionales por código**, también la piha y el boto, que el laboratorio llamaba «reales»: las grabaciones con licencia llegan después (07 §9.5) y los sustituyen sin tocar los ganchos.

**Arreglos de la revisión del laboratorio (2026-10-16), aplicados al portarlos:**

- **Niveles:** cada efecto sale a su pico de la tabla de abajo (±1,5 dB), sin el `tanh` global que los igualaba (en el laboratorio, `sfx_select` salía a −7 dBFS y la jerarquía quedaba en unos 4 dB; `sfx_confetti`, unos 20 dB por debajo de su objetivo). Solo queda un limitador suave por encima de −1 dBFS, que con estos picos no actúa (07 §9.4: picos ≤ −6 dBTP).
- **Graves:** al menos el 50 % de la energía de cada efecto por encima de 300 Hz, lo que reproduce el altavoz de un teléfono. Se consigue subiendo los armónicos 2.º y 3.º del propio sonido y, en el remanso de `sfx_stuck` y la calabaza de `sfx_canoe_lost`, con sus notas también una octava más arriba. Huito y Cacao (C4 y D4) se arreglan igual.
- **Colas:** la reverberación de dosel solo va en `sfx_win_toada`, `sfx_skip`, `sfx_tinaja_reveal` y `sfx_world_banner`. Los gestos repetidos terminan en ≤ 0,2 s: `select`, `tint`, `plip`, `ui_tap`, `ui_back`, `seeds_tick`, `pour_start` y `pour_end`. `sfx_ui_buy` conserva sus 0,4 s: es una compra, no un gesto repetido (07 §9.2 da 0,08-0,4 s a la interfaz) [P]: la exención está pendiente del visto bueno del tech lead, porque la tarea AU1 incluía `ui_*` en la regla de ≤ 0,2 s.
- **Bajar la selección no suena:** 02 §4.1 deja la deselección sin audio («Baja a su sitio | 100 ms | ease-in | — | —») y manda el GDD. La variante grave de la sección 1 queda como propuesta para el dueño; si la aprueba, se anota en 02 §4.1 y en D-026 y vuelve con su clip.
- **`sfx_pour_loop`:** un bucle continuo de 1,2 s, sin costura (su salto no supera el de los 5 ms de cada lado), que suena mientras cae el chorro. Su tono sigue al nivel de la cuia de destino, con la regla de la tabla: nivel = (dosis − 1) / (capacidad − 1), de 380 Hz con 1 dosis (o vacía) a 920 Hz llena (4 dosis en una cuia de 4).

**Música y ambiente provisionales:** desde la tarea AU2, el lecho es un pad sostenido sin pulso con hasta tres capas de percusión, variante nocturna en Muy difícil y un tema de menú en el inicio, y cada mundo tiene su agua, su lluvia por nivel y su fauna. Estado y cifras en las secciones 5 y 6.

**Coste (medido en el Editor del escritorio x64, 2026-10-16):** los 89 clips (61 efectos, más las 14 notas de `sfx_select` y de `sfx_tint`) se generan en 469,8 ms en un hilo de trabajo y su creación cuesta 2,2 ms en el hilo principal (`AF_PERF audio_build`; objetivo < 50 ms). Ocupan 6.249 KiB de PCM en float (unos 1,60 millones de muestras); la música y el ambiente, aparte (secciones 5 y 6). En el teléfono, por medir.

**Reglas (07 §9.4 y 02 §4):**

- Sonido, Música y Vibración vienen encendidos y se apagan por separado y al momento, en Ajustes.
- Se respeta el modo silencio del teléfono: en el de vibración no suena nada y en el de silencio tampoco vibra [P].
- Durante un anuncio se calla todo, y al terminar vuelve con un fundido de 0,5 s.
- Nunca vibra en un rechazo.
- La información nunca va solo por audio.

**Hápticos:** los efectos predefinidos de Android con el permiso `VIBRATE`, aprobado en D-026:

- `EFFECT_TICK` en `haptic_seal` y `haptic_back_blocked`;
- `EFFECT_CLICK` en `haptic_win`, `haptic_capi` y `haptic_boto`;
- con API 26-28, un pulso de 20, 15 o 40 ms.

**IDs finales, niveles y ganchos:**

| ID | Pico (dBFS) | Cola | Dónde suena hoy |
| --- | --- | --- | --- |
| `sfx_select` | −36 | ≤ 0,2 s | Al elegir una cuia, con la nota de su fruta de arriba (al bajarla no suena: 02 §4.1) |
| `sfx_pour_start` | −30 | ≤ 0,2 s | Cuando el chorro empieza a caer |
| `sfx_pour_loop` | −29 | bucle | Mientras cae el chorro (bucle; el tono sigue al nivel del destino) |
| `sfx_pour_end` | −30 | ≤ 0,2 s | Al cortarse el caudal |
| `sfx_illegal` | −28 | sin cola añadida | En cada rechazo (sin háptico) |
| `sfx_seal` | −17 | sin cola añadida | Al empezar la tapa de cada sellado, con `haptic_seal` |
| `sfx_river_rise` | −26 | sin cola añadida | Intro: al empezar |
| `sfx_fill` | −25 | sin cola añadida | Intro: una por fila |
| `sfx_fruit_plip` | −32 | ≤ 0,2 s | Intro: en cada impacto |
| `sfx_tint` | −30 | ≤ 0,2 s | Intro: en cada racha que florece (máx. 6 a la vez) |
| `sfx_win_toada` | −14 | dosel | Al empezar la celebración, con `haptic_win` |
| `sfx_confetti` | −20 | sin cola añadida | Con el confeti (0,5 s después) |
| `sfx_stuck` | −20 | sin cola añadida | Al abrirse Aguas estancadas |
| `sfx_undo` | −22 | sin cola añadida | Marea |
| `sfx_capi_paddle` | −18 | sin cola añadida | Cuia extra y de rescate, con `haptic_capi` |
| `sfx_hint` | −20 | sin cola añadida | Sin gancho todavía: Morpho (MO1-MO3) |
| `sfx_shuffle` | −18 | sin cola añadida | Sin gancho todavía: Pororoca (tanda siguiente) |
| `sfx_reveal` | −19 | sin cola añadida | Luciérnaga |
| `sfx_skip` | −17 | dosel | Sin gancho todavía: Salto del Boto, que aún no está en el cliente (con `haptic_boto`) |
| `sfx_restart` | −22 | sin cola añadida | Volver a la orilla y Reintentar |
| `sfx_river_drop` | −18 | sin cola añadida | El río bajó |
| `sfx_seeds_tick` | −26 | ≤ 0,2 s | Cosecha de la victoria: 1 por 10 semillas, máx. 20 |
| `sfx_basket_open` | −16 | sin cola añadida | Sin gancho todavía: Canasta |
| `sfx_streak_up` | −17 | sin cola añadida | Sin gancho todavía: Crecida |
| `sfx_streak_shield` | −19 | sin cola añadida | Sin gancho todavía: Hoja escudo |
| `sfx_canoe_lost` | −21 | sin cola añadida | Fallo con canoa perdida |
| `sfx_canoe_back` | −19 | sin cola añadida | El inicio, al volver una canoa |
| `sfx_ui_tap` | −30 | ≤ 0,2 s | Cualquier botón cuya acción no tiene sonido propio |
| `sfx_ui_back` | −30 | ≤ 0,2 s | Cerrar (X), Volver y Atrás de Android |
| `sfx_ui_buy` | −18 | sin cola añadida | Compra confirmada en la hoja de compra |
| `sfx_mech_hoja` | −20 | sin cola añadida | Paso «se abre» de la Hoja |
| `sfx_mech_matapi` | −19 | sin cola añadida | Sellado del Matapi |
| `sfx_mech_liana` | −19 | sin cola añadida | Paso «se suelta» de la Liana |
| `sfx_mech_liana_cuenta` | −24 | sin cola añadida | Sin gancho todavía: Liana de cuenta: su cuenta aún no es un paso de mecánica |
| `sfx_mech_remolino` | −20 | sin cola añadida | La pareja recibe por el fondo |
| `sfx_mech_barro` | −20 | sin cola añadida | Se lava el Barro |
| `sfx_mech_lluvia` | −21 | sin cola añadida | Cada gota de la Lluvia |
| `sfx_mech_pirana` | −19 | sin cola añadida | Mordisco de la Piraña |
| `sfx_mech_brote` | −20 | sin cola añadida | Paso «se abre» del Brote |
| `sfx_mech_flor` | −18 | sin cola añadida | Sin gancho todavía: Flor: listo en `AudioBoard.Step`; suena cuando exista su vista |
| `sfx_mech_hoja_atada` | −20 | sin cola añadida | Paso «se abre» de la Hoja atada: el gancho está en `AudioBoard.Step` y la vista existe desde VA1a (ola 4); por comprobar en el juego, porque ningún test de audio lo prueba todavía |
| `sfx_tinaja_select` | −26 | sin cola añadida | Al elegir la Tinaja (en lugar de `sfx_select`) |
| `sfx_tinaja_scoop` | −22 | sin cola añadida | Sin gancho todavía: Tinaja (TJ3-TJ4) |
| `sfx_tinaja_reveal` | −25 | dosel | Sin gancho todavía: Tinaja (TJ3-TJ4) |
| `sfx_tinaja_release` | −19 | sin cola añadida | Sin gancho todavía: Tinaja (TJ3-TJ4) |
| `sfx_sloth_nod` | −22 | sin cola añadida | Sin gancho todavía: Gestos de TiTino (TI6a y siguientes) |
| `sfx_sloth_happy` | −20 | sin cola añadida | Sin gancho todavía: Gestos de TiTino |
| `sfx_sloth_branch` | −24 | sin cola añadida | Sin gancho todavía: Gestos de TiTino |
| `sfx_sloth_yawn` | −24 | sin cola añadida | Sin gancho todavía: Gestos de TiTino |
| `sfx_rosa_surface` | −19 | sin cola añadida | Sin gancho todavía: Rosa |
| `sfx_monkey_chirp` | −34 | sin cola añadida | Intro: uno como mucho, al entrar la tropa y nunca durante el llenado |
| `sfx_fact` | −21 | sin cola añadida | Llegada a la parada (la línea de fruta, sin sonido; D-017) |
| `sfx_world_banner` | −14 | dosel | Al entrar la tarjeta del banner de mundo (solo ahí) |
| `sfx_map_paddle` | −20 | sin cola añadida | Sin gancho todavía: Mapa del río |
| `sfx_map_sample` | −18 | sin cola añadida | Sin gancho todavía: Mapa del río |
| `sfx_map_rosa` | −21 | sin cola añadida | Sin gancho todavía: Mapa del río |
| `sfx_map_pequepeque` | −28 | sin cola añadida | Sin gancho todavía: Mapa del río (opcional) |
| `sfx_fruit_new` | −17 | sin cola añadida | Sin gancho todavía: Tarjeta «Fruta nueva» (F5) |
| `sfx_mech_card` | −20 | sin cola añadida | Tarjeta previa |
| `sfx_morpho_idle` | −28 | sin cola añadida | Sin gancho todavía: Morpho por inactividad (MO3) |
| `sfx_store_open` | −19 | sin cola añadida | Al abrirse la hoja de compra |
