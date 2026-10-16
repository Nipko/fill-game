# Tintalú: lista de sonidos y música

2026-10-15 · Copia de trabajo para el dueño y los proveedores de audio (compositor y diseñador de sonido). Fuente: GDD 07 §9 y las mecánicas, personajes y pantallas de los demás capítulos. Si esta lista y el GDD no coinciden, manda el GDD hasta que se edite.

## Cómo usar esta lista

El juego necesita unos 61 efectos, el ambiente de 13 mundos (9 en la 1.0) y unas 20 piezas de música (unas 60 contando sus capas); hoy no tiene ningún sonido.

- Cada fila lleva el **ID** que usará el código, **cuándo suena**, **cómo debe sonar** según el GDD y su **duración**.
- La columna **Tu propuesta** está vacía: escribe ahí la idea, una referencia (un juego, un vídeo, un instrumento) o «ok».
- Las marcas [P] son propuestas que aún no decidiste; [E], estimaciones.
- Cuando haya propuestas, se convierten en sonidos provisionales para probarlos en el teléfono y en el encargo para el compositor y el diseñador de sonido.

## 1. Tablero y vertido

Son los sonidos que más se oyen: suenan en cada toque y cada vertido, así que deben ser cortos, suaves y agradables tras horas de juego.

| ID | Cuándo suena | Cómo debe sonar (GDD) | Duración | Tu propuesta |
| --- | --- | --- | --- | --- |
| `sfx_select` | Tocar una cuia para elegirla | Toque de agua corto, afinado al color de la fruta de arriba: 14 notas de escala pentatónica, una por fruta | 0,12 s | |
| `sfx_pour_start` | Empieza el vertido | Chorro de agua que entra en vidrio | 0,2 s | |
| `sfx_pour_loop` | Mientras cae el chorro | Chorro continuo; sube de tono a medida que se llena la cuia | en bucle | |
| `sfx_pour_end` | Termina el vertido | Corte del chorro, última gota | 0,2 s | |
| `sfx_illegal` | Vertido no permitido | Madera neutra, nada que suene a error o castigo | 0,15 s | |
| `sfx_seal` | Una cuia se completa y se tapa | Maracá (dos sacudidas) y tapa de madera que cae | 0,45 s | |
| `sfx_river_rise` | Intro del nivel: el río crece | Oleada grave, de fondo (−10 dB) [P] | 0,4 s | |
| `sfx_fill` | Intro: se llenan las cuias | Chorro y gorgoteo, uno por fila [P] | sin fijar | |
| `sfx_fruit_plip` | Intro: cae cada fruta en el agua | «Plic» suave [P] | sin fijar | |
| `sfx_tint` | Intro: el agua se tiñe | Nota afinada al color, como `sfx_select`; máximo 6 a la vez | 0,1 s | |
| `sfx_win_toada` | Victoria | Toada corta de carimbó: percusión y coro sin letra | 1,5-3 s | |
| `sfx_confetti` | Victoria, con el confeti de plumas | Aleteo de guacamayos | 1,0 s | |
| `sfx_stuck` | Aguas estancadas (sin jugadas) | Zumbido grave de agua quieta, sin golpe dramático | 1,5 s | |

En la intro, como mucho un chillido corto de mono ardilla, muy bajo (−18 dB); nunca chillidos mientras se llenan las cuias.

## 2. Comodines, economía e interfaz

Cada comodín tiene su sonido propio, ligado a su animal o elemento; los de interfaz son de madera y semillas, nunca «clics» electrónicos.

| ID | Cuándo suena | Cómo debe sonar (GDD) | Duración | Tu propuesta |
| --- | --- | --- | --- | --- |
| `sfx_undo` | Marea (deshacer) | Ola que retrocede, como el chorro al revés | 0,4 s | |
| `sfx_capi_paddle` | Cuia de rescate (+1 cuia): llega Capi | Pasos en arena, remo en el agua, tintineo de vidrio y chasquido corto de capibara | 0,8 s | |
| `sfx_hint` | Morpho (pista) | Aleteo suave y campanita de madera | 0,5 s | |
| `sfx_shuffle` | Pororoca (mezclar) | Ola grande, remolino y 14 notas rápidas | 0,9 s | |
| `sfx_reveal` | Luciérnaga (revelar Aguas turbias) | Campanilla cálida, chispa y burbuja que se aclara | 0,5 s | |
| `sfx_skip` | Salto del Boto (saltar nivel) | Salto, chapuzón y exhalación de delfín | 1,2 s | |
| `sfx_restart` | Volver a la orilla (reiniciar) | Agua que se recoge y madera | 0,5 s | |
| `sfx_river_drop` | El río bajó (nivel aliviado tras 3 fallos) | Agua que baja y exhalación de Rosa | 1,0 s | |
| `sfx_seeds_tick` | Contador de semillas que sube | Semillas que caen en una cesta; 1 por cada 10 semillas, máximo 20 | 0,05 s cada uno | |
| `sfx_basket_open` | Canasta (premio) | Fibra que se abre y frutas | 0,8 s | |
| `sfx_streak_up` | Crecida: sube la racha | Chillido de nutrias | 0,8 s | |
| `sfx_streak_shield` | Hoja escudo protege la racha | Hoja que se despliega | 0,6 s | |
| `sfx_canoe_lost` | Se pierde una canoa (vida) | Canoa que vuelca, suave | 0,6 s | |
| `sfx_canoe_back` | Se recupera una canoa | Remo | 0,6 s | |
| `sfx_ui_tap` | Cualquier botón | Madera seca | 0,08 s | |
| `sfx_ui_back` | Atrás o cerrar | Madera más grave | 0,1 s | |
| `sfx_ui_buy` | Compra en la tienda | Semillas en una cesta | 0,4 s | |

## 3. Mecánicas y personajes

Cada mecánica suena cuando se desbloquea, en el golpe de la tapa al sellar. Las que dicen «sin definir» aún no tienen sonido en el GDD: ahí la propuesta es la primera. Los ID de esta sección son [P]: se fijan al encargarlos (07 §9.5).

| ID [P] | Cuándo suena | Cómo debe sonar (GDD) | Duración | Tu propuesta |
| --- | --- | --- | --- | --- |
| `sfx_mech_hoja` | Se desbloquea la Hoja | Hoja seca | 0,4-0,6 s | |
| `sfx_mech_matapi` | Se abre el Matapi (trampa) | Crujido de fibra y tirón de cuerda | 0,8 s | |
| `sfx_mech_liana` | Se suelta la Liana con fruta | «Ding» y rama que cruje | 0,6 s | |
| `sfx_mech_liana_cuenta` | Baja la cuenta de la Liana de cuenta | Sin definir | corta | |
| `sfx_mech_remolino` | El chorro pasa por el Remolino | Succión y burbujas que suben en la otra cuia | 0,5 s | |
| `sfx_mech_barro` | Se limpia el Barro | Chapoteo | 0,5 s | |
| `sfx_mech_lluvia` | Llueve sobre el tablero | Sin definir | — | |
| `sfx_mech_pirana` | La Piraña muerde o se lleva fruta | Sin definir | — | |
| `sfx_mech_brote` | Se abre el Brote (de una o dos hojas) | Sin definir | — | |
| `sfx_mech_flor` | Se abre la Flor de vitória-régia | Sin definir | — | |
| `sfx_mech_hoja_atada` | Se suelta la Hoja atada | Sin definir | — | |
| `sfx_tinaja_select` | Elegir la Tinaja | «Toc» grave de barro | corta | |
| `sfx_tinaja_scoop` | Sacar agua de la Tinaja | «Glup» de calabaza en el agua; nunca burbujas ni hervor | corta | |
| `sfx_tinaja_reveal` | Se revela la boca de la Tinaja | Gota | corta | |
| `sfx_tinaja_release` | Se suelta su cuia chica | Roce de fibra y «toc» hueco | 0,8 s | |
| `sfx_sloth_nod` | TiTino asiente | «Mmm» suave y grave, sin palabras | corta | |
| `sfx_sloth_happy` | TiTino celebra | Suspiro feliz | corta | |
| `sfx_sloth_branch` | Entra la rama de TiTino | Roce de hojas | corta | |
| `sfx_sloth_yawn` | TiTino se duerme | Bostezo muy suave | corta | |
| `sfx_rosa_surface` | Rosa (delfín rosado) asoma | Exhalación real de boto | 1,0 s | |
| `sfx_monkey_chirp` | Tropa de miquitos en la intro | Chillido corto de mono ardilla, muy bajo; uno como mucho | corta | |

Para la 1.1 quedan tres más: Aguapé (agua que se abre), Palizada (chapoteo y madera que cruje) y Cocha (agua que se corta). Ningún personaje habla: solo sonidos, para no doblar a cada idioma.

## 4. Mapa del río, llegadas y pantallas especiales

Estos sonidos marcan momentos de viaje y descubrimiento; salen pocas veces, así que pueden ser más ricos. La línea de fruta de cada victoria va **sin sonido** a propósito, porque cansaría.

| ID [P] | Cuándo suena | Cómo debe sonar (GDD) | Duración | Tu propuesta |
| --- | --- | --- | --- | --- |
| `sfx_fact` | Llegada a una parada (victoria de los niveles 20, 30, 40…) | Madera y vidrio, sin papel [P] | 0,3 s | |
| `sfx_world_banner` | Banner de mundo nuevo (41, 81, 141…) | Grito de la piha, la firma sonora del juego; **solo aquí**, nunca en bucle | 1-2 s | |
| `sfx_map_paddle` | Capi rema en el Mapa | Remo de Capi (el mismo de `sfx_capi_paddle`) | 0,8 s | |
| `sfx_map_sample` | Se llena una muestra en el Mapa | Acorde de vidrio | corta | |
| `sfx_map_rosa` | Rosa en el Mapa | Exhalación de Rosa | 1,0 s | |
| `sfx_map_pequepeque` | Opcional: barcas en el Mapa | Motor peque-peque (canoa con motor de eje largo) | — | |
| `sfx_fruit_new` | Tarjeta «Fruta nueva» | Sin definir | — | |
| `sfx_mech_card` | Tarjeta previa de una mecánica nueva | Sin definir | — | |
| `sfx_morpho_idle` | Pista del Morpho por inactividad | Sin definir (¿el mismo aleteo de `sfx_hint`, más bajo?) | — | |
| `sfx_store_open` | Se abre la tienda o el Mercado | Sin definir | — | |

## 5. Ambiente de cada mundo

Cada mundo tiene un fondo sonoro de 3 capas que se mezclan solas: el agua del río, una lluvia que aparece en algunos niveles y animales sueltos cada 20-45 s. La 1.0 usa los mundos 1-9; los 10-13 llegan después.

| Mundo | Agua | Lluvia (niveles con lluvia) | Animales | Tu propuesta |
| --- | --- | --- | --- | --- |
| 1 Estuario | Río ancho y lento, marea | 20 % | Garzas, mercado lejano sin voces que se entiendan, piha ocasional | |
| 2 Tapajós | Olas suaves de playa | 10 % | Tucanes, piha, exhalación de Rosa | |
| 3 Manaos | Dos corrientes, una grave y una aguda | 25 % | Botos, guacamayos | |
| 4 Trapecio | Río tras la lluvia, goteo | 50 % | Pericos al atardecer, grillos de día | |
| 5 Loreto | Agua quieta de espejo | 30 % | Pirarucú que respira (burbuja grave), piha | |
| 6 Oriente | Laguna negra de noche | 40 % | Ranas, grillos, búho lejano | |
| 7 Madre de Dios | Río medio | 20 % | Guacamayos de la collpa, nutrias | |
| 8 Madidi | Cascada lejana | 30 % | Coro de aves de altura | |
| 9 Ucayali | Vapor que sisea y río | 20 % | Paiche, piha | |
| 10 Alto Negro | Río negro lento | 15 % | Noche: ranas, insectos, agua | |
| 11 Escudo de Guayana | Viento de sabana y río | 10 % | Hoatzín (graznido áspero) | |
| 12 Invitados del Orinoco | Río rápido sobre piedra | 10 % | Guacamayos, viento de tepuy | |
| 13 El origen | Torrente de altura, viento frío | 10 % | Viento, aves lejanas | |

Piezas que hay que grabar o comprar: unas 13 capas de agua, 2-3 lluvias (fina, media, fuerte) y unos 25 animales. La piha, el boto, la nutria, el hoatzín y las ranas necesitan grabaciones con licencia comercial.

## 6. Música

La música crece con el jugador: un lecho tranquilo de 60-90 s en bucle, sin ritmo marcado, y una percusión suave que gana una capa cada vez que se sella el 25 % de las cuias (sube el volumen, nunca el tempo). En los niveles Muy difíciles el lecho pasa a su versión nocturna, más grave; nunca música tensa de reloj.

| Pieza | Dónde suena | Estilo e instrumentos (GDD) | Tu propuesta |
| --- | --- | --- | --- |
| Lecho mundo 1 Estuario | Niveles 1-10 | Carimbó: tambor curimbó y maracás | |
| Lecho mundo 2 Tapajós | 11-40 | Carimbó de playa, más lento: maracá y guitarra [P] | |
| Lecho mundo 3 Manaos | 41-80 | Toada amazónica propia, sin nada de Parintins | |
| Lecho mundo 4 Trapecio | 81-140 | Guitarra acústica y flauta de caña, sin carácter ritual [P] | |
| Lecho mundo 5 Loreto | 141-200 | Cumbia amazónica suave, tempo bajo [P] | |
| Lecho mundo 6 Oriente | 201-300 | Percusión de semillas y cuencos de agua, nocturno [P] | |
| Lecho mundo 7 Madre de Dios | 301-420 | Guitarra y cajón ligero [P] | |
| Lecho mundo 8 Madidi | 421-560 | Charango suave y bombo [P] | |
| Lecho mundo 9 Ucayali | 561-700 | Cumbia amazónica / chicha: Farfisa y guitarra con wah-wah | |
| Lechos mundos 10-13 | 701 en adelante, después de la 1.0 | Pad nocturno y maracá · percusión de calabaza y voces sin letra · arpa y maracas llaneras · quena y zampoña (solo en el 13) | |
| Capas de percusión (×4 por mundo) | Se suman al sellar el 25, 50, 75 y 100 % | Percusión suave del instrumento de cada mundo | |
| Variante nocturna de cada lecho | Niveles Muy difíciles | Menos agudos, más graves | |
| Toada de victoria | Al ganar | Percusión y coro sin letra, 2-3 s (es `sfx_win_toada`) | |
| Tema de menú | Inicio, Mercado, pantalla de la Crecida | Cumbia amazónica ligera a 90-100 BPM: Farfisa y guitarra con wah-wah suave | |
| Stingers | Momentos cortos (banner de mundo, llegada, comodines) | Golpes musicales de 1-2 s; lista por cerrar | |

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
| Efectos de mecánicas, personajes y Mapa (§3-4) | 31 (+3 en la 1.1) | Diseñador de sonido; 11 aún sin definir | dentro de la fila anterior, a revisar |
| Ambiente (§5) | 9 aguas, 2-3 lluvias, unos 25 animales | Biblioteca con licencia comercial; la piha conviene grabarla | partida «grabaciones de fauna», sin cifra propia |
| Música (§6) | 9 lechos, 36 capas de percusión, 9 variantes nocturnas, toada, tema de menú y 5-8 stingers | Compositor, con cesión total de derechos; músicos regionales si se puede | 1.500-4.000 USD |

**Mientras tanto:** se puede crear el sistema de audio con sonidos provisionales hechos por código para oír el juego en el teléfono. Los sonidos reales sustituyen a los provisionales sin tocar el código.
