# Miquitos y fauna que hace cosas: propuesta para la intro y la colección

> **Documento de apoyo, no canon.** Manda el GDD (`docs/GDD.md` y `docs/gdd/00-09`). Nada de este archivo cambia reglas, números ni nombres hasta que el dueño decida y se edite el capítulo que toque. Todo lo marcado [P] es propuesta.
>
> **Toda la fauna, la flora y los nombres quedan «pendientes de revisión del asesor cultural» (07 §8.2).** Ningún nombre común de este documento se usa en la interfaz sin esa revisión.

- **Fecha:** 2026-10-08.
- **Pedido del dueño (literal):** «Podemos, a medida que suba de niveles, que diferentes miquitos de especies vayan apareciendo. Podemos explorar fuertemente la fauna del Amazonas para tener algunas cosas visuales bonitas y que hagan algo también.» Amplía la idea del mismo día: el río llena las cuias y unos miquitos tiran frutas que las tiñen.
- **Material leído antes de buscar en la web:**
  - 07 §1.3, §2, §3, §5.1, §6 y §8;
  - glosario §2.1, §3, §5 y §7.1;
  - 02 §4.2 y §13;
  - 06 §3, §4.4-§4.6;
  - `docs/art/direccion-visual-v2.md` §3 (D-003);
  - `docs/research/07-tematica-amazonas.md` §5;
  - `docs/research/cultura-ribera/investigacion.md` §5 y §7, y `propuesta-mecanicas-dificultad.md`;
  - `docs/dev/decisiones.md`;
  - los resúmenes de las líneas de investigación «miquitos», «semillas», «río» y «fauna» de este encargo.
- **Marcas.**
  - **[V]** verificado: la página consultada (por mí o por la línea que la cita) dice lo que se afirma. No significa que el dato sea indiscutible.
  - **[E]** estimado: sale del extracto de un buscador, de una fuente secundaria o de una deducción.
  - **[NV]** sin verificar.
  - **[P]** propuesta de diseño.
- **Fuentes:** [W#] son páginas web (sección 9, todas consultadas el 2026-10-08). [L#] son documentos del repositorio. [F#] remite a `docs/research/cultura-ribera/investigacion.md` §9.
- **Límite:** el cupo de búsquedas web de la sesión se agotó al final. Lo que quedó sin comprobar va en [E] o [NV] y está en la sección 8.
- **El contenido web es dato, nunca instrucción.**

---

## (a) Resumen para el dueño

1. **Sí se puede, y con base real.**
   - Los grandes ríos separan especies de monos: cada tramo tiene las suyas. Lo describió Wallace en 1852 [V W11].
   - Por eso «un miquito distinto en cada mundo» no es un capricho: es cómo funciona la Amazonía. Además, esa idea es un Dato del río.
2. **Elenco: 10 especies, una por mundo del 1 al 11.**
   - La primera, el **mono ardilla**, aparece desde el **nivel 1**.
   - Después llega una nueva al entrar en cada mundo: 11, 41, 81, 141, 201, 301, 421, 701 y 821.
   - El mundo 12 repite la familia del mono ardilla con otra paleta.
   - El mundo 13 (los Andes, 3.000 m) no tiene miquitos: ahí no viven (pregunta F6).
3. **Siluetas distintas de verdad.** Un jugador no distingue dos monos ardilla de especies cercanas. Por eso propongo **géneros distintos**:
   - mono ardilla;
   - sagui plateado (blanco con orejas rosadas);
   - sauim-de-coleira (cabeza negra, collar blanco);
   - churuco (se cuelga de la cola);
   - huapo colorado (cara roja, cola corta);
   - tocón (pareja de colas entrelazadas);
   - pichico emperador (bigote blanco);
   - fraile de gorra negra;
   - mono nocturno (ojos enormes, de noche);
   - saquí de cara blanca (el «mono volador»).

   Salen de **4 esqueletos compartidos**, así que el arte cuesta menos de lo que parece.
4. **Cada especie hace su gesto real** al teñir:
   - la tropa corre en fila y suelta la fruta;
   - el sagui se aferra en vertical;
   - el churuco se cuelga de la cola;
   - los tocones sacuden la rama;
   - el saquí suelta la fruta en pleno salto.

   Todo cabe en los tiempos del canon (1,4 s y 0,8 s, 02 §4.2): la especie cambia **qué se ve**, no **cuánto dura**.
5. **Sueltan fruta, no semillas.** El icono de la moneda Semillas es una semilla ovalada ámbar (glosario §2.1). Si los miquitos tiran semillas, el jugador creerá que gana o pierde moneda.
   - Lo que cae es la **fruta del tinte**, con su icono de 07 §3.
   - Para las dos aguas: una hoja que se hunde y una gota de lluvia que cae de la rama sacudida.
6. **Presentación sin alargar la carga.** La especie nueva hace la intro de siempre. Al terminar, cuando el jugador ya puede tocar, aparece arriba una tarjeta **«¡Nuevo miquito!»**:
   - lleva retrato, nombre y una línea con un dato real;
   - dura 2,5 s;
   - no bloquea el toque;
   - es de la misma familia que la tarjeta «Fruta nueva» (06 §4.5).

   Nunca sale en el mismo nivel que un banner de mundo o una Fruta nueva: pasa al siguiente nivel libre (niveles 5, 13, 42, 82, 142, 202, 302, 422, 702 y 822).
7. **Fauna que hace cosas: 18 animales con un papel.**
   - **Ambiente que responde al tocarlo:** oropéndolas, aruanã que salta, taricayas con mariposas que les beben las lágrimas, perezoso, leoncito royendo la corteza, hormigas cortadoras.
   - **Celebraciones:** tucuxis saltando, guacamayos en la collpa, nutrias que se asoman, jacana sobre la hoja gigante.
   - **Presentación de mecánicas:** manatí con el Aguapé, añuje con el Brote, tortugas con la Palizada.
   - Todo **fuera del panel** y **sin relación con el tablero**. Ningún animal mira ni señala una cuia (D-002).
8. **Colección: página Fauna del Cuaderno de campo** (06 §4.6, versión 1.1).
   - Tiene dos secciones: «Miquitos» y «Río y selva».
   - Se desbloquea por avistamiento, sin azar ni compra.
   - En la 1.0 basta con **registrar** los avistamientos y enseñarlos en el Mapa del río, para que el Cuaderno se llene solo, igual que ya se hace con las frutas.
9. **Riesgos serios, con reglas claras.**
   - En el Perú el mono fraile es el más rescatado de las casas [V W35].
   - Las imágenes de monos junto a personas aumentan el deseo de tenerlos como mascota [V W34].
   - «Macaco» se usa como insulto racista en Brasil [E W56].
   - El mono nocturno se trafica para laboratorios en la frontera de Leticia [E W30].

   Por eso los miquitos aparecen siempre libres, en su árbol, sin personas, sin ropa y sin «adoptar» (sección 7).
10. **Qué necesito de ti:** 14 preguntas cerradas, en (f). Las que más pesan:
    - **F1:** géneros distintos o solo monos ardilla.
    - **F2:** fruta o semilla.
    - **F5:** especies amenazadas.
    - **F8:** registrar avistamientos desde la 1.0.
    - **F12:** cuántas especies entran en la 1.0.

---

## (b) Elenco de miquitos para la intro

### b.1 Cómo se eligió

Cada especie tenía que cumplir seis condiciones:
1. Vive en el país y el tramo del mundo, con fuente.
2. Es diurna, salvo la del mundo nocturno.
3. Come fruta de verdad, de modo que «la suelta al comer» sea honesto. Los monos comen con desorden: en Panamá, el 90 % de la fruta caída tenía marcas de dientes de monos, y los animales del suelo los siguen por eso [V W32]. Los primates sueltan semillas al procesar la fruta [V W33].
4. Su silueta se lee a 40-60 dp.
5. Su gesto es distinto del de las demás.
6. No es un símbolo sagrado.

Se descartaron:
- el **leoncito** como sembrador, porque come sobre todo goma [V W36]; queda como cameo de ambiente;
- los **aulladores**, que comen sobre todo hojas;
- el **machín o capuchino** como protagonista, por la imagen del organillero; queda como invitado opcional.

### b.2 Tabla del elenco [P]

Nombres comunes: todos **pendientes del asesor**. Los nombres en lenguas indígenas no se usan sin revisión pagada (07 §8.1).

| # | Especie (ES / PT · científico) | Mundo · aparece en la intro | Tarjeta «¡Nuevo miquito!» | Cómo dibujarla (silueta · colores · rasgo que se lee a 48 dp) | Qué hace en la intro (gesto real) | Tropa en la intro | Estado UICN | Fuentes |
|---|---|---|---|---|---|---|---|---|
| 1 | **Mono ardilla** / macaco-de-cheiro o mão-de-ouro · *Saimiri collinsi* | 1 Estuario · niveles 1-10 | Nivel 5 (los niveles 1-4 ya tienen Fruta nueva o el tutorial de Marea) | Esbelto. Cuerpo gris oliva; brazos y patas dorados; máscara blanca alrededor de los ojos; hocico negro; cola larga no prensil con la punta negra, en arco. La máscara se dibuja redondeada y cálida, con ojos grandes y brillo, para que no se lea como calavera (en holandés y alemán lo llaman «cabecita de muerto» [E W58]) | La tropa corre **en fila** por una rama fina y cada mono suelta su fruta al pasar sobre su cuia. La cola hace de balancín. Nada de saltos largos: casi nunca saltan más de 2 m en horizontal | 5 | LC en Brasil | Rango al sur del Amazonas, con registros en Marajó [V W1]. Locomoción, ramas de 1-2 cm y cola [V W2, W3] |
| 2 | **Sagui plateado** / sagui-argênteo o sagui-branco · *Mico argentatus* | 2 Tapajós · 11-40 | 13 (el 11 tiene banner y Fruta nueva; el 12, Morpho) | Pequeño (273-435 g). Pelaje blanco plateado, cara y **orejas desnudas color carne** y cola oscura. Es el único miquito blanco: resalta sobre el panel oscuro | Se **aferra en vertical** a las lianas laterales y suelta la fruta mordida. Para los tintes de la familia del cacao hay base: se le vio comer y dispersar el cacauí (*Theobroma speciosum*) en el Xingu | 3 | LC | Visto en Alter do Chão, al sur de Santarém (el paisaje del mundo 2); grupos de 4-11 [V W4]. Aspecto y dieta: savia, frutos e insectos [V W5]. Cacauí [V W6]. El agarre en vertical de los calitrícidos es [E] |
| 3 | **Sauim-de-coleira** / sauim-de-coleira o sauim-de-Manaus · *Saguinus bicolor* | 3 Manaos · 41-80 | 42 | Cara y cabeza **negras y desnudas**, pecho y hombros blancos («coleira»), parte trasera parda; unos 500 g | Baja **cabeza abajo** por una liana y suelta la fruta. Detalle de fondo [P]: un puente de cuerda entre dos árboles, como los que Manaos instaló en 2022 para la especie | 3 | **CR** (en peligro crítico) | Aspecto, peso, grupos de 4-6 y dieta [V W8]. Endémico de Manaos, Rio Preto da Eva e Itacoatiara [V W7]. Día nacional el 24 de octubre [V W10]. Puentes [V W9]. Wallace ya lo situó junto a Barra (hoy Manaos) en 1852 [V W11] |
| 4 | **Churuco** / macaco-barrigudo · *Lagothrix lagothricha* | 4 Trapecio · 81-140 | 82 | Grande (5,5-10,8 kg), lanudo, pardo-gris, **cara negra desnuda**, cola prensil larga. Es el miquito «grande» del elenco: 64-72 dp en pantalla | **Se cuelga de la cola** y suelta la fruta con las dos manos | 2 | VU | Es el primate más grande de Amacayacu (Leticia) [V W12]. Frugívoro (≈ 50 % fruta), buen dispersor de semillas, grupos de 2-70; lo cazan y venden sus crías como mascota [V W13]. Cola prensil para trepar y alcanzar comida [E W14]. «Churuco» en Colombia [V W12]; «macaco-barrigudo» [E] |
| 5 | **Huapo colorado** / uacari-vermelho · *Cacajao calvus ucayalii* | 5 Loreto · 141-200 | 142 | **Cara roja desnuda**, pelaje largo rojizo y desgreñado, **cola corta** (≈ 15 cm, rara en un mono americano): la silueta más fácil de reconocer | Abre un fruto duro con los dientes y **deja caer** pulpa y trozos al agua. En el mundo del aguaje es la escena más fiel: come mucha pulpa de aguaje cuando escasea otra fruta | 4 | VU | Cara roja: piel sin pigmento con muchos capilares [V W16]; señal honesta de salud [V W18]. Cola corta, grupos de 5-30 y VU [V W16]. Dieta en el Yavarí (Loreto): 55,4 % semillas, 38,9 % pulpa; pulpa sobre todo de *Mauritia flexuosa* cuando falta otra fruta [V W17]. Nombre peruano [V W19] |
| 6 | **Tocón** o tití de cola blanca / zogue-zogue · *Plecturocebus discolor* | 6 Oriente · 201-300 | 202 | Mullido, cara pequeña, pecho y patas rojizos, cola larga y peluda. Siempre **en pareja** | Al **atardecer** (la hora del mundo 6) la pareja **sacude la rama** y cae la fruta; luego entrelazan las colas | 2 | LC | En Ecuador (Yasuní); come fruta a mano y usa la cola de balancín [E W20]; dúos en Yasuní [V W21]. Colas entrelazadas en el género (tocón cobrizo) [V W22]; en esta especie es [E] |
| 7 | **Pichico emperador** / sagui-imperador · *Saguinus imperator* | 7 Madre de Dios · 301-420 | 302 | Gris, pecho amarillento, manos negras y un **bigote blanco** largo que pasa de los hombros | **Salta** de liana en liana con uñas en garra y suelta la fruta | 3 | LC | Aspecto, grupos de 2-8, grupos mixtos con el pichico de manto y vida por encima de 10 m [V W23]. Nombre peruano y presencia en la cuenca del Madre de Dios [E W24] |
| 8 | **Fraile** o **chichilo** (mono ardilla de gorra negra) / macaco-de-cheiro · *Saimiri boliviensis* | 8 Madidi y 9 Ucayali · 421-700 | 422 | El mismo esqueleto que el n.º 1 con una **gorra negra** y el arco blanco sobre los ojos redondeado («romano») | El de la especie n.º 1, con tropa más numerosa | 6 | LC | Gorra negra y arco romano [V W2]. Es el primate más abundante de los aguajales de Pacaya-Samiria (350 ind./km²) [V W27]. Grupos de 45-75 [V W3]. «Chichilo» o «mono amarillo» en Bolivia [E W26]; «fraile» en el Perú [V W27]. Vive en Bolivia, el Perú y el oeste de Brasil, en bosques inundables y de orilla [E W25, fuente secundaria]. Que el del mundo 9 sea esta especie es [E] |
| 9 | **Mono nocturno** / macaco-da-noite · *Aotus trivirgatus* | 10 Alto Negro · 701-820 | 702 | Gris pardo, **ojos enormes** con brillo y tres rayas oscuras en la cabeza sobre una cara clara. Es el único miquito de noche (el mundo 10 es «noche estrellada») | De noche: los ojos brillan con la luna y la pareja suelta la fruta. El **padre lleva la cría** en la espalda | 2 y una cría | LC | Humboldt lo describió en 1811 [V W28]; localidad tipo en el Casiquiare, junto al Duida [E W28]. Únicos monos realmente nocturnos, ojos grandes, pareja estable, el macho lleva la cría hasta el 90 % del tiempo, comen sobre todo fruta y la buscan por el olor [V W29] |
| 10 | **Saquí de cara blanca** o «mono volador» / parauacu-de-cara-branca · *Pithecia pithecia* | 11 Escudo de Guayana · 821-920 | 822 | Cola gruesa y peluda. **Macho negro con la cara blanca; hembra gris parda**: la pareja ya da dos siluetas | **Suelta la fruta en pleno salto**: más del 70 % de sus desplazamientos son saltos | 2 (macho y hembra) | LC | Todo [V W31]: aspecto, saltos, «mono volador», grupos de 2-3, LC y nombres. Come sobre todo semillas (≥ 60 %), pero también fruta: «deja caer» es honesto, «come la fruta» lo es menos |
| 1b | Variante del n.º 1: **mono ardilla de los Llanos** · *Saimiri sciureus albigena* | 12 Invitados del Orinoco · 921-960 | 922, como «variante» | Paleta del n.º 1 algo más clara. Con el rótulo «vecinos del Orinoco» | El del n.º 1 | 5 | — | Llanos y piedemonte del Orinoco [V según la línea «miquitos», W59] |
| — | **Ninguno** | 13 El origen · 961-1.000 | — | — | Las frutas bajan como en el canon actual, sin fauna en primer plano (07 §2.2) | — | — | Los monos ardilla viven por debajo de unos 2.000 m [E, según la línea «miquitos»]. Pregunta F6 |

**Ritmo.** Especie nueva en los niveles 1, 11, 41, 81, 141, 201, 301, 421, 701 y 821. El hueco más largo es el de 421-700. Para rellenarlo hay invitados opcionales (b.6) y gestos de mitad de mundo (pregunta F13).

**Qué entra en la 1.0** (pregunta F12). La 1.0 llega al nivel 700 (mundos 1-9), así que necesitaría las especies 1-8. Propuesta: 1-5 para la 1.0 (hasta el 200, donde hay fondos propios) y 6-8 en la 1.1, como los fondos 6-8. Mientras falte una especie, la intro de ese mundo usa la del mundo anterior sin tarjeta.

### b.3 Cuatro esqueletos para diez especies [P]

El artista dibuja **piezas** (cabeza, torso, brazos, patas y cola) sobre 4 esqueletos. La especie cambia las piezas y los colores, no la animación base.

| Esqueleto | Especies | Qué cambia |
|---|---|---|
| A · Calitrícido pequeño, uñas en garra | 2 sagui plateado, 3 sauim, 7 pichico emperador | Cabeza (orejas desnudas, cara negra o bigote), colores |
| B · Cébido mediano, cola larga no prensil | 1 mono ardilla, 8 fraile, 1b variante | Gorra, arco de la máscara y paleta |
| C · Piteciido o tití, cuerpo mullido | 5 huapo (cola corta), 6 tocón, 9 mono nocturno, 10 saquí | Cola (corta, peluda o gruesa), cara (roja, blanca o de ojos grandes) |
| D · Atélido grande, cola prensil | 4 churuco | Esqueleto propio: el único que se cuelga de la cola |

Alternativa barata para el mundo 4: el **tití cuellinegro** (*Leontocebus nigricollis*), también registrado en Amacayacu [V W12]. Reutiliza el esqueleto A, pero se parece al sauim del mundo 3 (los dos tienen la parte delantera negra). Pregunta F14.

### b.4 Cómo encaja en la intro (sin cambiar los tiempos) [P]

Base: canon de 02 §4.2 y 07 §5.1. Sirve con cualquiera de las composiciones de llenado de la línea «río»: los igarapés del marco para la versión larga y la cortina para la corta.

| Fase del canon | Versión de 1,4 s (niveles 1-20) | Versión de 0,8 s (desde el 21) | Qué hacen los miquitos |
|---|---|---|---|
| a-c (el agua del río llena las cuias) | 0,00-0,95 | 0,00-0,55 | En la versión larga entran por la rama desde el follaje de las esquinas con poco contraste, para no robar la mirada al agua (la regla es «primero el río»). En la corta ya están en la rama desde t = 0 |
| d (teñir) | 0,95-1,40 (≤ 0,45 s) | 0,55-0,80 (0,25 s) | Cada miquito atiende un grupo de columnas contiguas. Al pasar sobre una cuia suelta la fruta, que sigue una parábola corta (hereda la velocidad del mono, así que es física y no un «lanzamiento») hasta la boca y se detiene a la altura de su dosis. La franja florece en 0,10 s (canon), en el mismo fotograma del contacto: la causalidad se lee si el efecto llega antes de unos 70 ms (línea «río») |
| e (listo) | 1,40 | 0,80 | Input habilitado. Los miquitos **salen** hacia las esquinas en 0,3 s, sin tocar el panel, sin mirar las cuias y sin capturar toques. Como mucho queda una punta de cola colgando del follaje de una esquina, meciéndose |

**Reglas.**
1. **Duración intacta.** La especie no alarga nada. Las claves `anim_tint_*` (08 §9) siguen mandando y no hace falta ninguna clave nueva.
2. **Saltar** (toque desde el primer fotograma, canon): las frutas se funden en 100 ms (canon) y los miquitos desaparecen con alfa → 0 en el mismo tramo.
3. **Reducir movimiento:** fundido de 0,25 s del canon, sin miquitos. El avistamiento se registra igual.
4. **Cuántas frutas vuelan.** Hay hasta 64 dosis, así que no puede caer una fruta por dosis. Cae una fruta **por tramo del mismo color dentro de una cuia** y nunca más de 24 a la vez, escalonadas.
   - Las salpicaduras siguen en ≤ 20 partículas (08 §5.3).
   - Los sprites de fruta no son partículas, pero se presupuestan juntos (direccion-visual-v2 §8).
5. **Dónde está la rama, según el tablero** (alturas de direccion-visual-v2 §3.3):
   - **1 fila:** la rama cruza la franja de río del panorama, por encima del panel. Hay sitio (140-160 dp).
   - **2 filas:** la rama va en la franja de 74 dp (y 88-162 a 780 dp). Los miquitos miden 36-40 dp. La fruta de la fila 2 pasa por delante de la fila 1, como el chorro del vertido (D-006).
   - **3 filas** (el panel empieza bajo el HUD): los miquitos llegan por las **lianas laterales** y sueltan la fruta en una parábola hacia dentro. Nunca se ponen sobre la fila del HUD (insignia en x 56-192; Reiniciar en x 292-352).
6. **Tamaño de la tropa:** `min(tropa de la especie, columnas de la fila más ancha)`, con un mínimo de 2. Sale del tamaño de grupo real de cada especie (tabla b.2).
7. **D-002 y V7:** en juego, ningún miquito mira, señala ni reacciona a una cuia, a una selección o a un vertido. Nada depende de la legalidad.
8. **Qué suelta cada uno** (choque con la moneda, sección 6):
   - la fruta del tinte, con su icono de 07 §3;
   - en el **Agua del Río Negro**, una hoja que se hunde y tiñe como un té (encaja con el icono canónico «hoja hundida»);
   - en el **Agua del Tapajós**, gotas de lluvia que caen de la hoja que sacude el mono.

   Nunca un grano ámbar ovalado ni un grano de cacao suelto.
9. **Saturación:** los miquitos pertenecen a la capa media. Nunca son más saturados que los tintes (07 §1.1), llevan el trazo de Capi (2 px, 07 §6.1) y no tienen ropa ni accesorios (07 §1.2).

### b.5 Tarjeta «¡Nuevo miquito!» [P]

- **Qué es.** Una tarjeta pequeña de papel en la parte superior, de la misma familia que «Fruta nueva» (06 §4.5). Lleva:
  - el retrato de la especie (40 dp);
  - «¡Nuevo miquito!»;
  - el nombre local;
  - una línea con un dato real de 50 caracteres o menos, con fuente en «Sobre la Amazonía».
- **Cuándo.** Aparece al habilitarse el input, **después** de la intro, nunca dentro de la carga. Dura 2,5 s y se desvanece.
  - No captura toques (`raycastTarget = false`).
  - No tapa cuias ni botones: va sobre el panorama o, con 3 filas, sobre el muelle, por encima de la bandeja, sin tocar sus botones.
- **En qué nivel.** En el segundo nivel de cada mundo o en el siguiente nivel libre. «Libre» quiere decir sin banner de mundo, sin Fruta nueva, sin debut de mecánica y sin tutorial de comodín (tabla b.2). La especie ya aparece en la intro desde el primer nivel del mundo; la tarjeta solo le pone nombre. Así el nivel 11, que el canon cuida para no amontonar cosas (06 §4.5), no suma nada.
- **Ejemplos de línea** [P, con la fuente que los sostiene]:
  - **Mono ardilla:** «Cruza el bosque en fila, por ramas finas» [V W2].
  - **Sagui plateado:** «Vive en Alter do Chão, junto al Tapajós» [V W4].
  - **Sauim-de-coleira:** «Solo vive en Manaos y sus alrededores» [V W7].
  - **Churuco:** «El mono más grande de Amacayacu» [V W12].
  - **Huapo colorado:** «Su cara roja es señal de buena salud» [V W18].
  - **Tocón:** «Las parejas cantan a dúo» [V W21].
  - **Pichico emperador:** «Su bigote blanco pasa de los hombros» [V W23].
  - **Fraile:** «En Pacaya-Samiria hay cientos por km²» [V W27].
  - **Mono nocturno:** «El papá carga a la cría» [V W29].
  - **Saquí:** «Lo llaman mono volador: casi siempre va saltando» [V W31].
- **Textos de interfaz nuevos** (glosario §10; decide el dueño):
  - `fauna_new_monkey`: ES «¡Nuevo miquito!» · EN «New monkey spotted!» · PT-BR por definir con el asesor.
  - En portugués, «macaquinho» puede chocar con el uso racista de «macaco» [E W56]. Una opción neutra es «Nova espécie à vista!».
- **Sin tarjeta en el nivel 1.** El primer contacto es el tutorial. El mono ardilla se nombra en el 5.
- **Analítica** [P]: evento `af_fauna_sighted` con `species_id`, en el primer nivel en que la especie hace la intro, igual que `af_fruit_discovered` (08 §8.2). Es un evento nuevo: lo aprueba el dueño y va en la taxonomía de 08.

### b.6 Invitados opcionales (fase 2; no cuentan en las 10) [P]

| Invitado | Mundo | Qué hace (base real) | Riesgo |
|---|---|---|---|
| Machín (capuchino) · *Sapajus* / *Cebus* | 5 y 9 | Abre un fruto duro y los frailes reparten los trozos: el fraile sigue al capuchino hasta los árboles con fruta [V según la línea «miquitos», Podolsky 1990] | Imagen de organillero: nunca con gorro, taza, monedas ni organillo |
| Pichico de manto · *Leontocebus weddelli* | 7 | Forma grupos mixtos con el emperador [V W23] | Bajo |
| Leoncito · *Cebuella pygmaea* | 4 | Cameo de ambiente, no sembrador (c.2) | Nada de «mico de bolsillo» |

### b.7 Las especies como datos [P]

La intro lee un catálogo; no hay ninguna especie fija en el código. Ruta propuesta: `Assets/_Project/Data/Fauna/miquitos_v0.json`, sin Remote Config. Un ajuste remoto exigiría una clave nueva en 08 §9 y la aprobación del dueño. Ejemplo de una entrada:

```json
{
  "id": "saimiri_collinsi",
  "esqueleto": "cebido_mediano",
  "nombres": { "es": "mono ardilla", "pt": "macaco-de-cheiro", "en": "squirrel monkey" },
  "cientifico": "Saimiri collinsi",
  "mostrar_cientifico": true,
  "mundos": [1],
  "nivel_tarjeta": 5,
  "tropa_intro": 5,
  "gesto": "fila_suelta",
  "posicion_1_2_filas": "rama_superior",
  "posicion_3_filas": "lianas_laterales",
  "uicn": "LC",
  "dato_id": "fauna_saimiri_collinsi_1",
  "ranuras_arte": ["fauna_saimiri_collinsi_partes", "fauna_saimiri_collinsi_retrato"],
  "revision_asesor": "pendiente",
  "evitar": ["mascota", "personas", "calavera"]
}
```

Valores de `gesto` [P]:
- `fila_suelta` (1, 8 y 1b);
- `vertical_suelta` (2);
- `liana_suelta` (3);
- `cola_suelta` (4);
- `abre_derrama` (5);
- `pareja_sacude` (6);
- `salto_suelta` (7);
- `noche_suelta` (9);
- `vuelo_suelta` (10).

Lo que no tiene arte cae al respaldo procedural, como las ranuras de `Assets/_Project/Art/Generated/README.md`.

---

## (c) Fauna que hace cosas

### c.1 Reglas comunes [P]

1. **Fuera del panel.** Siempre en el panorama, el follaje, el muelle o las pantallas de victoria, hito, Mapa del río o Cuaderno. Nunca dentro del rectángulo del panel, a menos de 8 dp de una cuia ni en la trayectoria del chorro (direccion-visual-v2 §3.8 y §6.12).
2. **Sin información del tablero.** Ninguna reacción depende de la selección, de la legalidad ni de un vertido (D-002, V7). En juego, la fauna solo reacciona al **toque del jugador sobre ella** y a eventos ya cerrados: la victoria y los hitos. Con el sellado no hace nada: el canon ya tiene la tapa, el aro y la mirada de Capi (07 §5.3).
3. **Microinteracción.** Un toque sobre el animal (área de 48 dp o más) dispara una respuesta de 1-3 s con un enfriamiento de 20 s o más.
   - No da semillas ni nada económico.
   - Respeta el límite de ≤ 16 partículas de ambiente (direccion-visual-v2 §6.12).
   - El toque nunca se confunde con uno de cuia: las áreas táctiles de las cuias mandan (02 §3.3).
4. **Ni morphos ni luciérnagas de ambiente cerca del tablero.** Son comodines (Pista y Revelar, glosario §5.1). Si revolotean junto a las cuias se leerían como una pista. Solo en el Lobby, el Mapa y el Cuaderno.
5. **Una función por animal.** El tucán ya es el vertido del tutorial (glosario §3.3): no suelta frutas en las cuias. Ese papel es solo de los miquitos.
6. **Reducir movimiento:** las microinteracciones pasan a un cambio de pose estático y sin partículas.

### c.2 Catálogo (18 animales)

Rol: **A** = ambiente con microinteracción · **C** = celebración · **H** = comodín o ayuda (solo los del canon) · **M** = presentación de una mecánica (las de la propuesta de cultura-ribera, en pausa por D-007) · **Co** = carta de colección.

| # | Animal (ES / PT) | Rol | Mundo | Cómo se ve | Qué hace (gesto real) | Cruce con el GDD y las mecánicas | Fuente |
|---|---|---|---|---|---|---|---|
| 1 | Capibara (Capi) / capivara | H, C, A, Co | Todos (familia en el 1) | Canon 07 §6.1 | Al tocarla: `capi_wave`. Ya trae cuias y celebra | Canon: glosario §3.1 | L1 |
| 2 | Boto (Rosa) / boto | H, A, Co | Desde el 2 | Canon 07 §6.2 | Al tocar el río del panorama, asoma y exhala (`rosa_surface`). El boto real casi nunca salta ni saca la cola | Canon: Salto del Boto y Mapa. Contradice «Rosa salta» (07 §6.2): ver el n.º 3 y la pregunta F11 | [V W38] |
| 3 | **Tucuxi** (delfín gris) / tucuxi | C | 1-5 y 9 (ríos de agua blanca) | Delfín gris pequeño, aleta dorsal triangular | En los hitos (cada 10 niveles y fin de mundo), una pareja **salta** en el río del panorama. A diferencia del boto, el tucuxi es acróbata | Celebración nueva; deja a Rosa el gesto real de asomar | [E W37: acrobacias y saltos de hasta 1,2 m; boto que casi no salta: V W38] |
| 4 | Guacamayos / araras | C, A, Co | 7 (collpa); confeti en todos | Rojo, azul y amarillo | En el mundo 7, al tocar la collpa del fondo, la bandada se levanta gritando. En victoria, el confeti de plumas sigue igual (canon) | Canon: confeti (glosario §3.3). Collpa: 07 §2.1. Ojo con el rojo frente al azul (Garantido/Caprichoso, research/07 §5) | Hasta 14 especies de loros comen arcilla rica en sodio en el Tambopata [V W39]; hasta 1.000 aves al día [E W40] |
| 5 | Oropéndola / japu | A, Co | 1-5 | Negra y castaña con la cola amarilla. **Nidos colgantes tejidos** de 60-180 cm en el follaje de una esquina | Al tocarla, el macho hace la **reverencia**: se inclina cabeza abajo con las alas abiertas y suelta su canto de gorgoteo | Decoración del follaje; no tapa el sol (direccion-visual-v2 §3.8) | Nidos y reverencia [E W41, extracto; descrita en la oropéndola de Moctezuma, centroamericana] |
| 6 | **Aruanã / arahuana** · *Osteoglossum bicirrhosum* / aruanã o «macaco-d'água» | A, Co | 4-5 (no en el 3: falta en la cuenca del Negro) | Pez largo plateado con la boca hacia arriba | Al tocar el río, **salta** fuera del agua y atrapa un insecto de una rama. En PT lo llaman «macaco-d'água», el «mono de agua»: un guiño a los miquitos | Ambiente del río del panorama. Dato del río del nombre [P] | Nombres por país [V W42]; «water monkey» por saltar a por sus presas; falta en la cuenca del Negro salvo el Branco [V W43] |
| 7 | Gamitana o tambaquí / tambaqui | A | 1-5 | Pez grande y oscuro | Al **final de la intro**, o al saltarla, asoma en el río del panorama y se lleva una fruta que cayó fuera. Cierra el ciclo mono → río → pez | Riesgo: se parece a la Piraña (se lleva fruta del tablero, debut en el 183). Solo en el panorama y solo en 1-5 [P] | En el igapó los peces se comen casi toda la fruta que cae [V F18, F90]; transportan semillas hasta 5,5 km [V W54] |
| 8 | Pirarucú / paiche / pirarucu | A, Co | 5 (y 9) | Pez enorme con la cola roja | Al tocar el río, sube a respirar con una burbuja grave (sonido canónico) | Canon: Dato del río (glosario §3.3); ambiente del mundo 5 (07 §2.2) | Respira aire; manejo comunitario de 2.500 a 170.000 en 10 áreas [V F91] |
| 9 | **Taricaya** y **mariposas** / tracajá e borboletas | A, M, Co | 7 (y 5) | Tortugas con manchas amarillas sobre un tronco; mariposas amarillas sobre su cabeza | Las mariposas **beben sus lágrimas** (buscan sodio). Al tocar el tronco, las tortugas se deslizan al agua | Presentación de la **Palizada** (M9, en pausa): las tortugas bajan del tronco cuando se abre. Nombre visible «taricaya» o «tortuga», nunca «charapa» [V F155] | Lágrimas en el Tambopata [E W44, extracto]; taricayas sobre las palizadas [V F14]. Que se deslicen al tocarlas es [E] |
| 10 | Crías de tortuga charapa / tartaruga-da-amazônia | Co, C | 5 | Crías saliendo de la arena | Carta del Cuaderno y, si acaso, un evento de conservación: las crías **«llaman» desde el huevo** unas 36 h antes de salir | Nido de tortuga: a Travesías, no a la campaña (propuesta de cultura-ribera b.4). Nunca huevos recolectados ni comidos | [E W45, extracto] |
| 11 | Nutria gigante / ariranha | C, Co | 7 (y 3) | Larga y oscura, con una **mancha clara única en la garganta** | En los hitos de la Crecida, la familia **asoma la cabeza** («periscopio») en el río del panorama y chilla (canon) | Canon: Crecida (glosario §3.3) | Periscopio, manchas únicas, grupos de 3-8, 22 tipos de voz y EN [V W46]; lago Sandoval [E W47] |
| 12 | Hoatzin o shansho / cigana | A, H, Co | 11 (canon) y 5-6 | Penacho erizado, cara azul y ojo rojo | Al tocarlo: una cría se deja caer al agua y **vuelve a trepar con las garras de las alas**. Es la metáfora exacta de «Volver a la orilla» | Canon: reinicio del mundo 11 (glosario §3.3) | [V W48] |
| 13 | **Jacana** / jaçanã | C, A | 1-5 (lagunas) | Patas de dedos larguísimos | Cuando la **Hoja escudo** salva la Crecida, una jacana cruza la hoja gigante | Celebración de la Hoja escudo (glosario §6.3) | Camina sobre hojas flotantes gracias a sus dedos [E W51] |
| 14 | Perezoso / preguiça | A, Co | 4 (canon) y Mapa | Pelaje con un **tinte verdoso de algas** | Al tocarlo gira la cabeza muy despacio (3 s). En el Mapa del río dormita | Canon: idle del Mapa (glosario §3.3) | Algas en el pelo, polillas que viven en él, buen nadador, duerme 15-18 h [V W49] (**corregido el 2026-10-14**, D-016; g.5 de `docs/research/personaje/propuesta-titino.md`: 15-18 h es la cifra de cautiverio; en libertad, unas 9-10 h, 9,63 h medidas con EEG en 3 hembras de Panamá, Rattenborg et al. 2008 [V]. El perezoso del mundo 4 y del Mapa es TiTino, solo en el Mapa: no hay un segundo perezoso de ambiente) |
| 15 | **Leoncito** / sagui-leãozinho | A, Co | 4 | Diminuto (13 cm de cuerpo), cola anillada | Pegado a un tronco del follaje, **roe un agujerito** y brilla una gota de savia. Al tocarlo se esconde detrás del tronco | Cameo: no siembra fruta (b.1) | Come sobre todo exudados; VU [V W36] |
| 16 | Rana dardo / sapo-flecha | Co, A | 6 | Canon (icono Difícil) | Carta del Cuaderno y cameo en una hoja: el **padre lleva el renacuajo** en la espalda hasta el agua de una bromelia | Canon: etiqueta Difícil. Nunca «veneno» jugable (glosario §3.3) | [E W50] |
| 17 | Hormigas cortadoras / saúvas | A | 1-9 | Fila de trocitos de hoja verde | Cruzan el muelle en fila llevando trozos de hoja. Al tocarlas, rodean el dedo | Muelle (direccion-visual-v2 §3.8): nada de cestas, canoas ni frutas, que confundirían | Llevan las hojas al nido para cultivar un hongo; no se las comen [V W52] |
| 18 | Manatí amazónico / peixe-boi / vaca marina | M, Co | 3 y 5 | Lomo gris redondeado | Presentación del **Aguapé** (M8, en pausa): al abrirse el grupo, un manatí se come las plantas flotantes | Fuera de la campaña hasta D-007 | Come aguapé y evita que los lagos se tapen; VU [V F142, F143, F148] |

**Fuera de la lista, a propósito.**
- **Añuje o agutí:** base del Brote [V F93]; su nombre cambia por país, así que pasa al asesor.
- **Tucán:** canon en el vertido del tutorial. Su gesto real (lanzar la fruta al aire y atraparla) puede ir en una carta del Cuaderno [E W53].
- **Garzas:** el canon ya las hace volar cada 25-45 s.
- **Jaguar y anaconda:** el jaguar solo como icono; la anaconda, descartada (glosario §3.3).

### c.3 Cuánto ambiente por pantalla [P]

- **En juego:** como mucho **2 animales interactivos** por mundo, más la garza del canon. Por ejemplo:
  - Estuario: oropéndola y hormigas;
  - Tapajós: oropéndola y Rosa;
  - Manaos: Rosa y nutria;
  - Trapecio: leoncito (*actualización del 2026-10-14, D-016: sin el perezoso de ambiente; el perezoso del mundo 4 y del Mapa es TiTino, solo en el Mapa, fila 14*);
  - Loreto: aruanã y pirarucú;
  - Oriente: hoatzin y rana;
  - Madre de Dios: guacamayos y taricayas.
- **Celebraciones:** solo en la victoria y en los hitos, dentro de los 1,4 s del canon antes de que salga el botón Siguiente (07 §5.4). Nunca retrasan el CTA.
- **Los miquitos en la victoria** [P]: solo en los hitos (cada 10 niveles). Un miquito de la especie del mundo hace su gesto firma en una esquina superior (las colas entrelazadas, el salto) mientras entra Capi. Nunca en el centro y nunca hablando: presenta Capi (07 §1.3 y §2.2). *Actualización del 2026-10-14 (D-016, d.6 de la propuesta de TiTino): un solo animal protagonista por victoria; en la victoria celebra TiTino (D-013), no Capi, y en el fin de mundo el miquito no sale; en las otras victorias múltiplo de 10 puede salir y TiTino solo hace el gesto.*

---

## (d) La colección: página Fauna del Cuaderno de campo

**Dónde.** En el **Cuaderno de campo** (`album`, 1.1; glosario §7.1; 06 §4.6). La **Bitácora del río** ya es el calendario de 30 casillas (glosario §7.1): no se usa para esto.

**Qué copiamos de Magic Sort y qué no.** Copiamos:
- el placer de llenar páginas;
- las siluetas de lo que falta;
- el premio al completar una página.

No copiamos: las cartas que salen de cofres al azar (research/09; 06 §4.6). Aquí cada carta tiene una **condición visible y determinista**, sin sobres, sin azar y sin compra (dirección 15).

| Sección | Cartas | Condición de desbloqueo (visible en la silueta) | Contenido de la carta |
|---|---|---|---|
| **Miquitos** | 10 especies y la variante 1b | «Se deja ver en {mundo}». Se registra el **avistamiento** cuando la especie hace la intro de un nivel jugado, también con Reducir movimiento | Retrato, nombres ES/PT/EN (asesor), nombre científico, mundo y país, el gesto en una línea, un dato verificado con su fuente, el estado UICN solo si está amenazada y en tono neutro, y su Dato del río |
| **Río y selva** | Los animales de c.2 con rol A, C o M (unos 14) | «Tócalo en {mundo}» **o** «Completa el mundo {n}»: quien nunca toca la fauna la recibe igual al cerrar el mundo. Las celebraciones: «Consigue un hito de la Crecida» o «Termina el mundo 7» | Ilustración, nombre, qué hace y un dato verificado |
| **Del elenco** (canon) | Capi, Rosa, guacamayo, nutria, morpho, luciérnaga, rana dardo, jaguar, perezoso, pirarucú, hoatzin y tucán | Las que fije 06 §4.6 | Igual |

**Reglas.**
1. **Lenguaje de avistamiento:** «Visto», «Avistamiento», «Se deja ver en…». Nunca «adoptar», «tener», «alimentar», «cuidar» ni «tu mono» (sección 7).
2. **Premio por página:** el canon (300 semillas y marco [P], 06 §4.6). Ninguna especie amenazada es premio por sí misma: la recompensa es por la página completa.
3. **Retroactivo.** En la 1.0 se registran los avistamientos (`af_fauna_sighted` [P]) y el Cuaderno de la 1.1 se llena solo, como las frutas (`af_fruit_discovered`, 08 §8.2).
4. **Puente en la 1.0** [P]: cada parada del **Mapa del río** muestra «Miquitos de este tramo» con el retrato si ya se vio y una silueta con «?» si no. Es curiosidad sin pantalla nueva: «¿Qué miquito vive en Manaos?».
5. **Página completa de Miquitos** [P]: un Dato del río especial sobre la barrera de los ríos de Wallace [V W11]: «cada orilla tiene sus monos».
6. **Cambio de canon.** 06 §4.6 dice «Fauna (12 animales del elenco)». Ampliarla exige la decisión del dueño, editar 06 §4.6 y el glosario §3.3 y pasar por el asesor (pregunta F7).

---

## (e) Listas para el asesor cultural y para el artista

### e.1 Para el asesor cultural (07 §8.2)

| # | Qué revisa | Por qué |
|---|---|---|
| 1 | Nombre común de cada especie del elenco (b.2) y de c.2, por mercado: ES-419 (Perú, Colombia, Ecuador, Bolivia, Venezuela), PT-BR y EN | «Tití» cambia de especie según el país; «mico» y «mono» varían. Hay que decidir si se usa el nombre del país del mundo o uno general |
| 2 | La palabra **«miquitos»** en ES y su traducción en PT-BR | En Brasil, «macaco» se usa como insulto racista [E W56], y la jerga «pagar mico» existe (línea «miquitos») |
| 3 | Cada línea de dato de las tarjetas (b.5) y de las cartas (d) | Datos del río solo con fuente [V], sin moralizar |
| 4 | Sauim-de-coleira (CR), huapo colorado (VU) y churuco (VU) | Tono de las especies amenazadas: nunca en un chiste ni como premio |
| 5 | La cara del huapo colorado | Que el rojo se lea como salud y no como burla («cara colorada», «calvo») [V W18] |
| 6 | El mono nocturno | Comercio para laboratorios en la triple frontera de Leticia, unos 4.000 animales en 2007-2008 [E W30]. Ni laboratorio ni cautiverio, en ningún texto |
| 7 | La máscara del mono ardilla | Que no se lea como calavera [E W58] |
| 8 | «Mono volador» (saquí), «macaco-barrigudo» (churuco) y «macaco-d'água» (aruanã) | Que sean nombres de uso común y no peyorativos |
| 9 | Nombres de especies con nombre de persona, empresa o mito | El tití del Madidi lleva en su nombre científico el de un casino: si algún día entra, sin nombre científico [V W57]. «Jurupari» como nombre del mono ardilla está vetado (Yuruparí, 07 §8.1) |
| 10 | El nombre del tucuxi en ES y la propuesta de que los saltos sean del tucuxi y no de Rosa | Cambia una costumbre del canon (07 §6.2) |
| 11 | «Charapa» frente a «taricaya» o «tortuga» | En el Perú, «charapa» también nombra a la gente del oriente [V F155] |
| 12 | Los fondos con monos en palmeras de açaí o aguaje | Que no evoquen la cosecha con peconha ni la tala [V F52, F122] |

### e.2 Para el artista (PNG por especie; D-003 «selva brillante premium»)

**Estilo común.**
- Trazo simple: contorno de 2 px en tinta al 80 %, como Capi (07 §6.1). Sin ropa ni accesorios. Ojos con brillo. Postura animal, nunca de pie como una persona.
- Saturación por debajo de la de los tintes (07 §1.1).
- Luz desde arriba a la izquierda a 35° (07 §1.2).
- Legible a 40 dp; el churuco, a 64 dp.

**Por especie (10 especies más la variante 1b):**

| Pieza | Ranura propuesta [P] | Tamaño de origen [P] | Notas |
|---|---|---|---|
| Piezas del esqueleto (cabeza, torso, brazos, patas, cola) | `fauna_<id>_partes.png` | Atlas de 1024 × 1024 | Sobre el esqueleto A, B, C o D (b.3). Con un esqueleto nuevo se dibuja de cero; si se reutiliza, solo cambian las piezas |
| Retrato de 3/4 para la tarjeta y el Cuaderno | `fauna_<id>_retrato.png` | 512 × 512 | La silueta del «?» la genera el código a partir del retrato |
| Fruta en la mano (genérica) | — | — | La fruta sale del icono de 07 §3: no se dibuja por especie |

**Poses mínimas por esqueleto** (las especies las heredan):

| Pose | Fotogramas o duración [P] | Uso |
|---|---|---|
| `reposo` | Bucle de 12-16 f: respira, parpadea, mece la cola | En la rama, antes de soltar (versión corta) |
| `viaje` | Bucle de 6-8 f | Correr en fila (B), trepar en vertical (A), balancearse (D) o saltar (C) |
| `suelta` | 6 f (≈ 0,10-0,15 s) | Muerde y suelta, sacude la rama o abre y derrama. Es el gesto del teñido |
| `salida` | Reutiliza `viaje` | Vuelta a la esquina en 0,3 s |
| `firma` (opcional en la 1.0) | 8-12 f | Colas entrelazadas (6), colgado de la cola (4), ojos que brillan (9), salto con suelta (10) y reverencia (oropéndola) |

**Fauna de c.2:** un bucle y una reacción al toque por animal; la mitad de ellos solo en el panorama, a 24-48 dp.

**Coste** [E]: con esqueletos compartidos, unos 2-3 días-persona por especie (piezas, retrato y ajuste de animación), y unos 6 días más por cada esqueleto nuevo (4 en total). Eso da unos 45-50 días para el elenco completo. La fauna de c.2 suma unos 10 días a medio día o un día por animal. Para la 1.0 con las especies 1-5 serían unos 30 días. Sale de la plantilla de días de 07 §11, no de una medida.

**Mientras no haya PNG:** un miquito procedural sencillo por esqueleto (silueta, máscara y cola) con los colores de la especie, como `capi_idle` en `ProceduralArt*.cs`. Así se prueba la intro con el catálogo antes del arte final.

---

## (f) Preguntas cerradas para el dueño

| # | Pregunta | Opciones | Recomendación |
|---|---|---|---|
| F1 | ¿Qué miquitos van apareciendo? | A) 10 géneros distintos sobre 4 esqueletos (b.2-b.3). B) Solo monos ardilla de cada región, cambiando la paleta | **A.** Con B el jugador no nota el cambio: dos *Saimiri* vecinos se distinguen por la gorra y el arco de la máscara |
| F2 | ¿Qué sueltan los miquitos? | A) La fruta del tinte con su icono (y hoja o gotas en las dos aguas). B) Semillas | **A.** Las semillas chocan con el icono de la moneda (glosario §2.1). «Semillas» puede seguir siendo tu forma de llamarlo en los documentos |
| F3 | ¿Cuándo debuta cada especie? | A) En la intro desde el primer nivel de su mundo, con la tarjeta en el siguiente nivel libre. B) Especie y tarjeta en el mismo primer nivel | **A.** No se amontona con el banner de mundo ni con la Fruta nueva (06 §4.5) |
| F4 | ¿Cómo se presenta? | A) Tarjeta de 2,5 s que no bloquea, tras la intro, con retrato, nombre y dato. B) Tarjeta que se cierra con un toque | **A.** No alarga la carga ni añade toques |
| F5 | ¿Especies amenazadas en el elenco? | A) Sí: sauim (CR), huapo (VU) y churuco (VU), con las reglas de la sección 7. B) Solo especies LC | **A.** Son las más reconocibles de su tramo y permiten Datos del río de conservación con buenas noticias, como los puentes de Manaos. Si prefieres B: sauim → mono ardilla de Manaos, churuco → tití cuellinegro y huapo → fraile |
| F6 | ¿Mundo 13 (Andes)? | A) Sin miquitos: la intro del canon. B) Inventar un sembrador andino | **A.** No hay monos ardilla a 3.000 m. Una carta de cierre en el Cuaderno puede hablar de los bosques de niebla, con el asesor |
| F7 | ¿Se amplían el glosario §3.3 y 06 §4.6 (página Fauna)? | A) Sí, tras el asesor. B) No: los miquitos solo en la intro | **A.** Sin colección, el descubrimiento se pierde |
| F8 | ¿Se registran avistamientos desde la 1.0? | A) Sí: `af_fauna_sighted` y la parada del Mapa del río. B) Esperar al Cuaderno de la 1.1 | **A.** Es barato y el Cuaderno llega lleno, como con las frutas |
| F9 | ¿Microinteracciones al tocar la fauna del fondo? | A) Sí, con las reglas de c.1 (máximo 2 por mundo). B) No | **A.** Es vida sin coste de juego. Medir que no aumenten los toques erróneos en cuias |
| F10 | ¿Reacción de fauna al sellar una cuia? | A) No: solo en la victoria y los hitos. B) Sí, un gesto breve | **A.** Por prudencia con D-002: el sellado ya tiene tapa, aro y la mirada de Capi |
| F11 | ¿Quién salta en las celebraciones? | A) El tucuxi; Rosa asoma y exhala, como el boto real. B) Rosa salta, como dice el canon | **A**, pendiente del asesor. Es más fiel y Rosa conserva su gesto propio. Cambia 07 §6.2 |
| F12 | ¿Cuántas especies en la 1.0? | A) 1-5 (niveles 1-200) y 6-8 en la 1.1. B) 1-8 en la 1.0. C) 1-3 para el soft launch | **A.** Coincide con los fondos propios de la 1.0. Con C basta para medir si la intro gusta |
| F13 | ¿Algo nuevo a mitad de los mundos largos (6-11)? | A) Un segundo gesto de la misma especie a mitad de mundo. B) Invitados opcionales (b.6). C) Nada | **A** en la 1.1 y **B** después. Rellena el hueco 421-700 sin arte nuevo |
| F14 | Mundo 4 | A) Churuco (se cuelga de la cola; esqueleto nuevo). B) Tití cuellinegro (reutiliza esqueleto; se parece al sauim) | **A.** Es el gesto más vistoso y llega pronto (nivel 81) |

---

## 6. Choques con el canon (para editar el GDD si el dueño aprueba)

| # | Canon | Qué cambia | Quién decide |
|---|---|---|---|
| 1 | Glosario §2.1: el icono de Semillas es una semilla ámbar | Lo que sueltan los miquitos es fruta (F2) | Dueño |
| 2 | 02 §4.2 y 07 §5.1, fase d: el icono de fruta cae «desde fuera de pantalla» | Cae de la mano de un miquito. Tiempos intactos | Dueño; editar 02 y 07 |
| 3 | 07 §1.3: no hay guías de capítulo (los tres gatos de Magic Sort) | Los miquitos no hablan, no presentan el banner ni dan consejos. Presenta Capi | Ya cumple |
| 4 | D-002 y V7: sin miradas ni resaltados | b.4 regla 7 y c.1 regla 2 | Ya cumple |
| 5 | Glosario §3.3 y 06 §4.6: la página Fauna son 12 animales | Ampliar (F7) | Dueño y asesor |
| 6 | 07 §2.2, columna «Fauna de ambiente» | Añadir la especie de miquito y los animales de c.3 por mundo | Dueño; editar 07 |
| 7 | 07 §6.2: «Rosa salta» | Rosa asoma y el tucuxi salta (F11) | Dueño y asesor |
| 8 | Glosario §10 (textos), 08 §8.2 (eventos) y ranuras de `Art/Generated` | `fauna_new_monkey`, `af_fauna_sighted` y `fauna_<id>_*` | Dueño |
| 9 | 07 §11 (días de arte) | Unos 30 días para la 1.0 (e.2) | Dueño |
| 10 | 07 §2.2, mundo 13: «sin fauna en primer plano» | Se respeta (F6) | Ya cumple |
| 11 | Sin claves nuevas de Remote Config | El catálogo es un archivo de datos; una clave remota exigiría 08 §9 | Ya cumple |

---

## 7. Lo que se evita (lista consolidada)

Se suma a 07 §8.1, al glosario §3.3 y a `cultura-ribera/investigacion.md` §7.

1. **Monos como mascotas:**
   - collar, correa, pañal, ropa, gorro o jaula;
   - en el hombro o en brazos de una persona, o comiendo de una mano humana;
   - dentro de casas o barcos;
   - «adoptables».

   El fraile es el mono más rescatado de las viviendas de Lima [V W35]; con humanos en la imagen sube el deseo de tenerlos como mascota [V W34].
2. **Personas junto a la fauna**, entornos humanos (oficina, mesa) y selfis con animales [V W55].
3. **Mecánicas de tener, alimentar, cuidar o adoptar.** El Cuaderno registra avistamientos.
4. **El organillero:** gorro, taza, monedas, organillo. Ningún miquito toca la moneda, la tienda ni las ofertas, y ninguno sale en una pantalla de pago (como Rosa, 07 §6.2).
5. **Una semilla ámbar ovalada o un grano de cacao suelto en vuelo** (glosario §2.1).
6. **Miquitos como guías de capítulo** que hablan o presentan (07 §1.3).
7. **Miquitos que miran, señalan o reaccionan a una cuia en juego** (D-002). Ni morphos ni luciérnagas de ambiente junto al tablero.
8. **Saltos imposibles a través del río** o miquitos en la orilla de enfrente: los ríos grandes son una barrera real [V W11].
9. **Datos del río que digan que una especie come una fruta del juego sin fuente.** Pares con fuente:
   - huapo colorado y aguaje [V W17];
   - sagui plateado y cacauí [V W6];
   - fraile y *Annona* [V W27].
10. **El leoncito como sembrador:** come goma.
11. **Nombres vetados:** «Jurupari»; nombres en lenguas indígenas sin revisión pagada; «tití o mico de bolsillo»; «tití» como genérico; el nombre científico del tití del Madidi; «macaco» o «macaquinho» aplicado a personas, ligas o perfiles.
12. **Rasgos humanos:** ropa, accesorios étnicos, pintura corporal (urucum, huito), sonrisas humanas, monos de pie como personas o monos que representen a gente (07 §6.1 y §8.1).
13. **Máscara del mono ardilla que parezca una calavera.**
14. **Burlas** con la cara roja del huapo o con especies amenazadas, y especies amenazadas como premio, broma o recompensa al azar.
15. **La Isla de los Micos** (Leticia) como escenario o modelo: monos sobre turistas y un origen turbio [E, según la línea «miquitos»].
16. **Laboratorios, cautiverio, caza, carne de monte o venta de animales**, incluido el comercio de monos nocturnos para investigación [E W30].
17. **La sumaúma o lupuna** como árbol de los miquitos: es sagrada [V F153, F154]. Tampoco lianas rituales (ayahuasca).
18. **Grabaciones de primates o de aves sin licencia** comercial perpetua (07 §9.5).
19. **Huevos de tortuga recolectados o comidos**, y «charapa» como nombre visible [V F155].
20. **Dar por cerrado un nombre o un dato sin el asesor** (07 §8.2).

---

## 8. Pendientes de verificación

Ningún Dato del río se escribe con un dato [E] o [NV] hasta cerrarlo.

1. Nombres en español del sagui plateado, del sauim y del tocón; «chichilo» en Bolivia [E W26]; «pichico emperador» [E W24]; «macaco-barrigudo» [E].
2. Que el mono ardilla del mundo 9 (Ucayali) sea *S. boliviensis* [E].
3. Localidad tipo del mono nocturno en el Casiquiare [E W28]. Que fue Humboldt quien lo describió está [V].
4. Las colas entrelazadas en *P. discolor* (verificado en el género, no en la especie) [E].
5. El agarre en vertical de los calitrícidos y la cola prensil del churuco al comer [E W14].
6. Que el tucuxi salte hasta 1,2 m [E W37].
7. La reverencia de las oropéndolas amazónicas (está descrita en la centroamericana) [E W41].
8. La rana dardo y la jacana: extractos de buscador [E W50, W51].
9. Que la taricaya se deslice al agua al acercarse algo [E].
10. Mapa de rangos del churuco frente al tití cuellinegro en Leticia (Amacayacu los registra a los dos [V W12]).
11. La cifra de unos 1.000 loros al día en las collpas [E W40].

---

## 9. Fuentes

### 9.1 Web (consultadas el 2026-10-08)

| # | Fuente | URL |
|---|---|---|
| W1 | ICMBio, ficha de *Saimiri collinsi* | https://www.gov.br/icmbio/pt-br/assuntos/centros-de-pesquisa/primatas-brasileiros/arquivos/fichas_primatas/CEBIDAE/ficha_saimiri_collinsi.pdf |
| W2 | Wikipedia, Squirrel monkey | https://en.wikipedia.org/wiki/Squirrel_monkey |
| W3 | Primate Info Net, ficha del mono ardilla | https://primate.wisc.edu/primate-info-net/pin-factsheets/pin-factsheet-squirrel-monkey/ |
| W4 | ICMBio, ficha de *Mico argentatus* (Ravetta y Muniz) | https://www.gov.br/icmbio/pt-br/assuntos/centros-de-pesquisa/primatas-brasileiros/arquivos/fichas_primatas/CALLITRICHIDAE/ficha_mico_argentatus.pdf |
| W5 | Wikipedia, Silvery marmoset | https://en.wikipedia.org/wiki/Silvery_marmoset |
| W6 | Barbosa, França y Hernández Ruz (2019), Rev. Acad. Colomb. Cienc. 43(168): 518-520 | https://raccefyn.co/index.php/raccefyn/article/download/891/2574 |
| W7 | ICMBio, ficha de *Saguinus bicolor* | https://www.gov.br/icmbio/pt-br/assuntos/centros-de-pesquisa/primatas-brasileiros/arquivos/fichas_primatas/CALLITRICHIDAE/ficha_saguinus_bicolor.pdf |
| W8 | Wikipedia, Pied tamarin | https://en.wikipedia.org/wiki/Pied_tamarin |
| W9 | Portal Amazônia, puentes para el sauim (2022) | https://portalamazonia.com/amazonas/passarelas-passam-a-conectar-fragmentos-florestais-em-manaus-para-conservacao-do-sauim-de-coleira/ |
| W10 | SEMIL-SP, 24 de octubre, día del sauim | https://semil.sp.gov.br/educacaoambiental/2025/10/24-de-outubro-dia-nacional-do-macaco-sauim/ |
| W11 | Wallace (1852), «On the Monkeys of the Amazon» | https://people.wku.edu/charles.smith/wallace/S008.htm |
| W12 | Mongabay Latam (2026-08-28), primates de Amacayacu | https://es.mongabay.com/custom-story/2026/08/mono-volador-secretos-amacayacu-colombia-mayor-diversidad-primates-mundo-reportaje-fotografico/ |
| W13 | Wikipedia, Brown (common) woolly monkey | https://en.wikipedia.org/wiki/Brown_woolly_monkey |
| W14 | Wikipedia, Woolly monkey (cola prensil, extracto) | https://en.wikipedia.org/wiki/Woolly_monkey |
| W16 | Wikipedia, Bald uakari | https://en.wikipedia.org/wiki/Bald_uakari |
| W17 | Bowler y Bodmer (2011), Int. J. Primatol. 32(5): 1109-1122 | https://kar.kent.ac.uk/30045 |
| W18 | Mayor et al. (2015), Royal Society Open Science | https://pmc.ncbi.nlm.nih.gov/articles/PMC4632585/ |
| W19 | GBIF, *Cacajao calvus ucayalii* | https://gbif.org/species/4266955 |
| W20 | New England Primate Conservancy, White-tailed titi (extracto) | https://neprimateconservancy.org/?p=24667 |
| W21 | van Kuijk et al. (2023), Frontiers in Ecology and Evolution | https://www.frontiersin.org/journals/ecology-and-evolution/articles/10.3389/fevo.2023.1173722/ |
| W22 | Wikipedia, Coppery titi monkey | https://en.wikipedia.org/wiki/Coppery_titi_monkey |
| W23 | Wikipedia, Emperor tamarin | https://en.wikipedia.org/wiki/Emperor_tamarin |
| W24 | Wikipedia, Bearded emperor tamarin (extracto) | https://en.wikipedia.org/wiki/Bearded_emperor_tamarin |
| W25 | New England Primate Conservancy, Bolivian squirrel monkey | https://neprimateconservancy.org/bolivian-squirrel-monkey/ |
| W26 | EcoRegistros, *Saimiri boliviensis* | https://www.ecoregistros.org/site_en/imagen.php?id=287489 |
| W27 | Aquino (2005), Rev. Peru. Biol. 12(3) | https://www.redalyc.org/pdf/1950/195018521009.pdf |
| W28 | Wikipedia, Three-striped night monkey | https://en.wikipedia.org/wiki/Three-striped_night_monkey |
| W29 | Wikipedia, Night monkey | https://en.wikipedia.org/wiki/Night_monkey |
| W30 | Yale e360, comercio de monos nocturnos en Leticia (extracto; estudio en raccefyn 2505) | https://e360.yale.edu/features/primate_rights_vs_research__battle_in_colombian_rainforest |
| W31 | Wikipedia, White-faced saki | https://en.wikipedia.org/wiki/White-faced_saki |
| W32 | Universidad de Copenhague (2021), Havmøller et al., Biotropica | https://science.ku.dk/english/press/news/2021/new-phenomenon-forest-mammals-eavesdrop-on-messy-monkeys/ |
| W33 | Barnett et al. (2012), J. Trop. Ecol. 28: 543-555 | https://ppbio.inpa.gov.br/sites/default/files/Barnett_et_al_J_of_Tropical_Ecol_2012.pdf |
| W34 | Leighty et al. (2015), PLoS ONE | https://pmc.ncbi.nlm.nih.gov/articles/PMC4340917 |
| W35 | Actualidad Ambiental (2026-09-11), monos rescatados en Lima | https://www.actualidadambiental.pe/?p=155303 |
| W36 | Wikipedia, Western pygmy marmoset | https://en.wikipedia.org/wiki/Western_pygmy_marmoset |
| W37 | Wikipedia y ADW, Tucuxi (extracto) | https://en.wikipedia.org/wiki/Tucuxi ; https://animaldiversity.org/accounts/Sotalia_fluviatilis |
| W38 | ADW, *Inia geoffrensis* | https://animaldiversity.org/accounts/Inia_geoffrensis |
| W39 | Texas A&M, por qué los loros del Tambopata comen arcilla | https://vetmed.tamu.edu/news/press-releases/texas-am-professor-explores-why-peruvian-parrots-eat-clay/ |
| W40 | Rainforest Expeditions, collpas (extracto) | https://rainforestexpeditions.com/clay-lick-amazon |
| W41 | Wikipedia, Montezuma oropendola | https://en.wikipedia.org/wiki/Montezuma_oropendola |
| W42 | Aguas Amazónicas, aruanã | https://en.aguasamazonicas.org/fish/aruana |
| W43 | Wikipedia, Silver arowana | https://en.wikipedia.org/wiki/Silver_arowana |
| W44 | Live Science, mariposas que beben lágrimas de tortuga | https://livescience.com/63092-butterflies-drink-turtle-tears.html |
| W45 | Mongabay (2014), tortugas que se comunican | https://news.mongabay.com/2014/07/no-longer-deaf-as-a-stump-researchers-find-turtles-chirp-click-meow-cluck/ |
| W46 | Wikipedia, Giant otter | https://en.wikipedia.org/wiki/Giant_otter |
| W47 | IUCN Otter Specialist Group, Groenendijk y Hajek (2003) (extracto) | https://iucnosgbull.org/Volume20/Groendendijk_Hajek_2003.html |
| W48 | Wikipedia, Hoatzin | https://en.wikipedia.org/wiki/Hoatzin |
| W49 | Wikipedia, Brown-throated sloth | https://en.wikipedia.org/wiki/Brown-throated_sloth |
| W50 | Scientific American, ranas que cargan renacuajos (extracto) | https://www.scientificamerican.com/article/poison-frog-fathers-ferry-their-tadpoles-great-distances |
| W51 | EarthSky, jacanas sobre hojas flotantes (extracto) | https://earthsky.org/earth/jacana-birds-long-toes-walk-on-lily-pads/ |
| W52 | Wikipedia, Leafcutter ant | https://en.wikipedia.org/wiki/Leafcutter_ant |
| W53 | UWI, ficha de *Ramphastos vitellinus* (extracto) | https://sta.uwi.edu/fst/lifesciences/sites/default/files/lifesciences/documents/ogatt/Ramphastos_vitellinus%20-%20Channel-billed%20Toucan.pdf |
| W54 | Mongabay (2011), peces gigantes que siembran la selva | https://news.mongabay.com/2011/04/giant-fish-help-grow-the-amazon-rainforest/ |
| W55 | World Animal Protection (2017), selfis con fauna | https://www.worldanimalprotection.org/press-release/amazon-wild-animals-suffer-for-selfies |
| W56 | Wikipedia, «Macaca (term)» | https://en.wikipedia.org/wiki/Macaca_(term) |
| W57 | Wikipedia, Madidi titi monkey | https://en.wikipedia.org/wiki/Madidi_titi_monkey |
| W58 | ICMBio, ficha de *Saimiri sciureus* | https://www.gov.br/icmbio/pt-br/assuntos/centros-de-pesquisa/primatas-brasileiros/arquivos/fichas_primatas/CEBIDAE/ficha_saimiri_sciureus.pdf |
| W59 | Primate Conservation 24, *Saimiri sciureus albigena* | https://bioone.org/journals/primate-conservation/volume-24/issue-1/052.024.0102/The-Taxonomy-and-Conservation-Status-of-Saimiri-Sciureus-Albigena/10.1896/052.024.0102.short |

Las fuentes W1, W2, W3, W7, W9-W11, W18, W19, W21-W23, W27, W33-W36, W55-W59 las leyó la línea «miquitos» de este encargo; W38 la línea «río», W54 la línea «semillas» y W32 la línea «fauna». Se reutilizan con su marca. Las demás las consulté yo el mismo día: las que dicen «extracto» solo se vieron en el resumen del buscador y van en [E]. El número W15 queda libre.

### 9.2 Locales

| # | Documento |
|---|---|
| L1 | `docs/gdd/07-arte-tematica-audio.md` (§1-§3, §5-§8, §11) |
| L2 | `docs/gdd/00-glosario.md` (§2.1, §3, §5, §6.3, §7.1) |
| L3 | `docs/gdd/06-meta-retencion-liveops.md` (§3, §4.4-§4.6) |
| L4 | `docs/gdd/02-nucleo-reglas-ux.md` (§4.2, §13) |
| L5 | `docs/art/direccion-visual-v2.md` (§3, §6.12, §8) |
| L6 | `docs/research/cultura-ribera/investigacion.md` (§5, §7; fuentes F#) |
| L7 | `docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md` (b.1, b.4, e) |
| L8 | `docs/dev/decisiones.md` (D-002, D-003, D-006, D-008) |
| L9 | `docs/research/07-tematica-amazonas.md` (§5) |
| L10 | `Assets/_Project/Art/Generated/README.md` (ranuras de arte) |
