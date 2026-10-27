# Avatares del jugador en la 1.0: diseño (D-056)

Fecha: 2026-10-27. La escribió el tech-lead en modo de diseño (carril `tanda6/avatares-diseno`). Es una **propuesta**: todo lo que no viene de D-056 lleva [P] hasta que el dueño responda las preguntas de la sección 7. Lo que pidió el dueño (D-056): «unos avatars predeterminados para escoger para el personaje del jugador, esto con respecto a la tematica, y podamos mostrarlo en la pantalla principal de inicio»; en el formulario eligió la 1.0, animales de la selva, Inicio + Liga + Victoria/perfil y «unos libres y otros por hitos».

**Fuentes:** D-056; `docs/dev/liga.md` §1, §3 y §4; `infra/contracts/v1`; D-045 (identidad v2); D-050 (inicio «el muelle del río»); D-052 (arte por código); D-033 (edad y privacidad); glosario §3 (fauna), §10 y §12; 06 §3, §4.1, §4.3, §4.5, §4.7 y §6; 07 §7.8, §7.9 y §8.1; 08 §4.3, §8.2, §9, §11 y §13.1 #11; `docs/art/direccion-visual-v2.md` §2.3, §5.7 y §7.

**Estado del código (2026-10-27, `d0497d44`):** no hay avatares. El inicio (`LobbyScreen`) tiene arriba Semillas (16-128 dp), Canoas (140-268) y Ajustes (w − 56), con Música y Sonido debajo del engranaje; abajo, la barra de pestañas con Mercado | Inicio | Mapa (`MarketLobbyIcons`, `MapTab`). La Liga (`LeagueScreen.Row`) pinta rango, seudónimo, bandera con país y puntos. La victoria tiene la línea «+{n} puntos de Liga» (`GameScreen.League.cs`). El seudónimo lo elige el servidor entre 24 animales (`league_name_*`).

## 1. El catálogo

### 1.1 Qué animales [P]

**16 animales**, todos de la lista de 24 de la Liga (liga.md §1), para reutilizar sus claves `league_name_*` y su texto ya revisado en los tres idiomas (D-054). De los 24 quedan fuera ocho, cada uno por una regla escrita:

| Fuera | Por qué |
|---|---|
| Perezoso (`sloth`) | Glosario §3.4: TiTino «es el único perezoso del juego». |
| Capibara (`capybara`) y boto (`boto`) | Son Capi y Rosa (glosario §3.1 y §3.2). Un avatar con su especie se leería como «soy Capi». El avatar es el jugador, no un personaje del elenco (pregunta Q-AV-1). |
| Anaconda (`anaconda`) | Glosario §3.3, «Descartados»: sagrada para varios pueblos. Ya es raro que esté en el seudónimo; se anota en la pregunta Q-AV-12. |
| Manatí (`manatee`) | Glosario §3.3: «No entra por ahora» (D-007, Q12). |
| Mono ardilla (`squirrel_monkey`) | Es el miquito del tramo 1-10, y los miquitos son «nunca guías, mascotas ni premio» (glosario §3.3). |
| Morpho (`morpho`) | Es el icono del comodín Morpho (glosario §5.1): confusión de sistemas (guía v2 §7). |
| Pirarucú (`arapaima`) | Está reservado a los Datos del río y a una mecánica futura (glosario §3.3). Además, un pez se lee mal en un retrato redondo. |

### 1.2 Lista, índices y desbloqueo

El **índice** es lo que viaja al servidor (D-056: «guardado en el backend como un índice»). La lista **solo crece por el final**: un índice nunca cambia de animal ni se reutiliza. El perfil guarda el **id** (más legible y robusto). La tabla `AvatarCatalog` traduce entre id e índice.

| Índice | Id | Clave de texto (ya existe) | es-419 · en · pt-BR | Desbloqueo [P] | Fondo [P] |
|---|---|---|---|---|---|
| 0 | `macaw` | `league_name_macaw` | Guacamayo · Macaw · Arara | **Libre** | Hoja |
| 1 | `toucan` | `league_name_toucan` | Tucán · Toucan · Tucano | **Libre** | Petróleo |
| 2 | `tapir` | `league_name_tapir` | Tapir · Tapir · Anta | **Libre** | Agua |
| 3 | `turtle` | `league_name_turtle` | Tortuga · Turtle · Tartaruga | **Libre** | Madera |
| 4 | `coati` | `league_name_coati` | Coatí · Coati · Quati | **Libre** | Hoja |
| 5 | `hummingbird` | `league_name_hummingbird` | Colibrí · Hummingbird · Beija-flor | **Libre** | Petróleo |
| 6 | `dart_frog` | `league_name_dart_frog` | Rana dardo · Dart Frog · Sapo-flecha | Gana tu primer nivel **Difícil** (el 40; 06 §3, D-009) | Madera |
| 7 | `otter` | `league_name_otter` | Nutria · Otter · Ariranha | **Crecida**: llega al hito 10, el río crecido (06 §4.3; la nutria gigante es la voz de la Crecida, glosario §3.3) | Agua |
| 8 | `jaguar` | `league_name_jaguar` | Jaguar · Jaguar · Onça | Gana tu primer nivel **Muy difícil** (el 60; 06 §3) | Petróleo |
| 9 | `kingfisher` | `league_name_kingfisher` | Martín pescador · Kingfisher · Martim-pescador | **Reto del río**: racha de 7 días (06 §4.1, la misma que paga la Pororoca) | Agua |
| 10 | `caiman` | `league_name_caiman` | Caimán · Caiman · Jacaré | Completa el **mundo 3, Manaos** (gana el 80) | Petróleo |
| 11 | `anteater` | `league_name_anteater` | Oso hormiguero · Anteater · Tamanduá | Completa el **mundo 4, Trapecio** (gana el 140) | Madera |
| 12 | `heron` | `league_name_heron` | Garza · Heron · Garça | **Reto del río**: 20 sellos en un mes (06 §4.1, R-Q7: el premio de 20 sellos esperaba a «que existan cosméticos») | Hoja |
| 13 | `howler` | `league_name_howler` | Mono aullador · Howler Monkey · Bugio | Completa el **mundo 5, Loreto** (gana el 200) | Hoja |
| 14 | `ocelot` | `league_name_ocelot` | Ocelote · Ocelot · Jaguatirica | **Liga del río**: 50 puntos en una semana (el umbral «Participante» de 06 §6) | Petróleo |
| 15 | `armadillo` | `league_name_armadillo` | Armadillo · Armadillo · Tatu | Completa el **mundo 6, Oriente** (gana el 300) | Agua |

Por qué estos 6 libres: son variados de silueta (tres aves con picos muy distintos, un mamífero grande, uno pequeño y un reptil) y de color, y ninguno es icono de otro sistema. Por qué estos hitos:
- cubren los cuatro sistemas que pidió el dueño (mundos, Reto, Liga, Crecida) y los dos estrenos de dificultad, que ya son momentos fuertes (D-009);
- están repartidos del 40 al 300 y todos caben en la 1.0 (1-700);
- ninguno se compra ni sale al azar (dirección 15; 06 §4.6: «colección por hitos, sin sobres, sin azar, sin compra»).

La rana dardo y el jaguar ya son los iconos de Difícil y Muy difícil. Ganarlos al superar esos niveles le da el mismo sentido al icono, y el retrato (de frente, en un disco) no se confunde con el glifo de perfil del chip.

### 1.3 Reglas del desbloqueo [P]

- **Todo sale del estado que ya existe** (08 §4.3), sin contadores nuevos:
  - mundos: `progress.max_level` > el último nivel del mundo (un nivel saltado con el Salto del Boto cuenta como pasado);
  - Difícil y Muy difícil: la victoria de un nivel con esa etiqueta, o `max_level` ≥ 41 / ≥ 61 para los perfiles anteriores;
  - Crecida: al pagar el hito 10 (`streak.river_full` pasa a true);
  - Reto: `daily_challenge.streak_days` ≥ 7 al cobrar, o `stamps.days` con 20 o más;
  - Liga: `league.me.points` ≥ 50 en una respuesta del servidor.
- **Un desbloqueo no se pierde nunca:** se apunta en `avatar.unlocked` (sección 4.1), porque la racha del Reto, los sellos del mes y los puntos de la Liga vuelven a 0.
- **Se evalúa** al cerrar una victoria, al cobrar el Reto, al recibir una respuesta de la Liga y al cargar el perfil (la retroactividad de los perfiles anteriores).
- **La Liga y sus avatares:** el avatar de la Liga (el ocelote) solo aparece en el selector si `live_league_enabled` está encendido. Con la Liga apagada no se enseña una meta imposible (pregunta Q-AV-8).
- **Sin semillas ni comodines por desbloquear:** es cosmético. No cambia la economía (05) ni `tools/econ_sim.py`.

## 2. El arte por código

### 2.1 Cómo se dibuja (D-052) [P]

Cada retrato se dibuja con `PixelCanvas`, como la fauna y las banderas, en un archivo nuevo: `ProceduralArt.Avatars.cs`. El **PNG del artista manda** por la ranura `avatar_<id>` de `ArtSlots` (`ArtSlots.AvatarPrefix`, cuadrado de 512 px). Sin PNG, sale el dibujo por código en todos los builds, release incluido (D-052). Capas, de atrás adelante:

1. **Disco de fondo** con un degradado radial suave, en una de cuatro familias de la UI y del entorno:
   - Hoja: `_fondo` #236A29 a `_base` #2F7D32;
   - Agua: #0F6275 a #13788F;
   - Madera: #46302A a #7E5638;
   - Petróleo de la marca (D-045).

   **Nunca un HEX de tinte** (guía v2 §7, «Color»; 07 §3, regla 6).
2. **Selva al pie:** tres o cuatro hojas ovadas en un tono más oscuro del fondo, cortadas por el disco. Solo hojas ovadas: las lanceoladas daban NaN antes de `tanda6/hojas-drawleaf` y el inicio ya está aprobado con ovadas (D-051).
3. **El animal**, de busto, de frente o a tres cuartos:
   - primitivas (elipses, cápsulas y polígonos de Bézier) con el **contorno de tinta de 2 px** de Capi y la fauna;
   - como mucho tres tonos por zona y un brillo;
   - **proporciones de adulto:** ojos ≤ 12 % del ancho de la cara, sin cabezas gigantes ni rubor de peluche (08 §13.1 #11: «no atractivo para niños»; sección 2.4);
   - sin ropa ni accesorios (07 §8.1; guía v2 §7, «Cultura»);
   - comportamiento real: el caimán con la boca cerrada, el jaguar sin rasgos humanos (07 §8.1), la rana sin nada que diga «veneno» (glosario §3.3).
4. **Aro de madera** del 7 % del diámetro: canto #2E2018, luz #BF8F62, con un filete `crema` #FFF8E6 por dentro. Es el sitio que ocupará el **marco** de la 1.2 (`entitlements.frames`, 06 §6): el retrato no cambia cuando lleguen los marcos.

**Bloqueado:**
- el fondo se desatura al 40 %;
- el animal pasa a **silueta sólida** en el `_contorno` de su familia, con alfa 0,85, sin ojos ni brillos;
- el aro pasa a `deshabilitado_contorno` #7C766D;
- abajo a la derecha, una **chapa de madera** de 30 % del diámetro con el **icono del hito**, en `crema`:
  - mundo: el banderín de la parada (`map_*`);
  - Difícil o Muy difícil: el glifo del chip;
  - Crecida: la regla con agua;
  - Reto: el disco con olas;
  - Liga: el podio.

Nunca solo color: la silueta, la chapa y el texto del hito (sección 3.2) dicen lo mismo (07 §7.8).

### 2.2 Tamaños y formato [P]

| Dónde | Tamaño visual (dp) | Fuente |
|---|---|---|
| Línea de la Liga en la victoria | 24 | retrato elegido |
| Fila de la Liga, fila de Ajustes | 32 | atlas |
| Inicio | 40 (área táctil de 48) | retrato elegido |
| Rejilla del selector | 64 | atlas |
| Cabecera del perfil | 112 | retrato elegido |

- **Retrato elegido:** una textura de 112 dp a la densidad de la pantalla (`ceil(112 × px/dp)`, entre 2 y 4 px/dp; 336 px a 3 px/dp, unos 450 KB en RGBA32 con mipmaps). Mipmaps encendidos para que se lea limpio a 24 y 40 dp. Se rehace solo al cambiar de avatar.
- **Atlas:** 16 celdas de 64 dp (4 × 4; a 3 px/dp, 768 × 768 px, unos 3 MB con mipmaps), en RGBA32. Para cada animal, el retrato normal y, para los bloqueados, la versión bloqueada (se dibuja la que toca; al desbloquear, se repinta la celda).
  - Se crea **perezoso** (al abrir el selector o la Liga) y **por fotogramas**: dos retratos por fotograma, con el disco de fondo como hueco mientras tanto. Ningún tirón.
  - Se libera al cerrar las dos pantallas, en la línea del trabajo de memoria del 2026-10-26 (`PixelBuffers`).
- **Presupuesto [P]:** ≤ 8 ms por retrato de 192 px en el Editor x64 y sin asignaciones por fotograma. Si se pasa en el teléfono, una caché en disco como `BrandEmblemCache` (clave: id, densidad, `ArtVersion`).
- `ArtVersion` sube al entrar el arte, para que la caché de disco no sirva texturas viejas.

### 2.3 Bocetos en texto

Tucán (libre, fondo Petróleo): mira a la derecha; cabeza y lomo negros, babero crema-amarillo, pico grande con la punta oscura que sale del disco hacia el aro, ojo con anillo azul.

```
          .-~~~~~~~~~-.            aro de madera + filete crema
       .'  .-------.   '.
      /   /  @@     \____\___    @@ = ojo con anillo azul y brillo de 1 punto
     |   |  @@  ______________>  pico naranja natural, punta oscura, rebasa el disco
     |   |      \_____________/
     |    \ ::::  /         |    :::: = babero crema-amarillo
      \    '-::-'  ,  ,    /     hojas ovadas oscuras al pie
       '.  \|/ \|/ \|/   .'
          '-..........-'
```

Jaguar (por hito, fondo Petróleo): de frente, orejas redondas, rosetas abiertas en la frente y las mejillas, hocico claro, boca cerrada y mirada tranquila. No es el jaguar de perfil del chip de Muy difícil.

```
          .-~~~~~~~~~-.
       .'  (o)     (o)  '.       (o) = orejas redondas con el interior claro
      /   .-~~~~~~~~~-.   \
     |   /  o  .  .  o \   |     o = ojos ámbar, pupila redonda (adulto: pequeños)
     |   | .  ( ^^ )  . |  |     ( ^^ ) = hocico crema con la nariz rosada
     |   \ o   '--'   o /  |     o . = rosetas abiertas
      \   '-.______.-'    /
       '.   \|/  \|/    .'
          '-..........-'
```

Nutria gigante (por hito, fondo Agua): cabeza ancha y plana, mancha crema irregular en la garganta, bigotes, asomando del agua con una onda.

Jaguar **bloqueado**: la misma composición en silueta sólida, el fondo apagado, el aro gris y la chapa del hito.

```
          .-~~~~~~~~~-.            aro gris #7C766D
       .'  (#)     (#)  '.
      /   .###########.   \      ### = silueta sólida (_contorno, alfa 0,85)
     |   /#############\   |
     |   |#############|   |
     |   \#############/ .---.   chapa de madera con el glifo de Muy difícil
      \   '-.#######.-' | J |    (la cabeza de jaguar del chip, en crema)
       '.               '---'
          '-..........-'
           Muy difícil              ← texto del hito bajo la celda (sección 3.2)
```

### 2.4 Riesgo de «atractivo para niños» (08 §13.1 #11)

El juego es 13+ sin programa Families. Unos retratos de animales tiernos pueden empujar a Play a verlo como dirigido a niños. Para evitarlo [P]:
- el mismo estilo que Capi, la fauna y la tropa (trazo de tinta, luz de juego), nunca «chibi»;
- adultos y con proporciones reales;
- sin colores pastel de golosina;
- la cuadrícula se presenta como «Tu avatar», nunca como «mascota que se cuida» (glosario §3.4).

La revisión de capturas de la ficha (07 §10) los incluye.

## 3. Las pantallas

### 3.1 Inicio (D-050) [P]

**Dónde:** el avatar va en la **esquina de arriba a la izquierda**, en la barra de los saldos. Es el sitio de perfil habitual en el género y la composición de D-050 no cambia:
- arriba, los saldos;
- en el centro, «Jugar»;
- abajo, las pestañas, que ya están llenas: Mercado, Inicio y Mapa.

Medidas:
- el disco de 40 dp con su área de 48 dp va en x = 8;
- Semillas pasa a x = 60;
- Canoas va 4 dp después de Semillas y 4 dp antes del engranaje.

A 360 dp: avatar 8-56, Semillas 60-168 (108 dp, 4 menos que hoy), Canoas 172-300 y Ajustes 304-352. **A 320 dp**: Semillas 60-152 (92 dp, con el autoajuste de 14-20 sp que ya tiene) y Canoas 156-260 (104 dp, con el icono 8 dp más estrecho). Lo deciden los tests de composición a 320 dp y al 130 %, que hoy ya existen para esta fila. TiTino, su rama y su bocadillo no se mueven: están debajo de la fila.

**Cuándo aparece:** desde que se gana el **nivel 10**, con el Mapa y el Mercado (06 §3, fila 10). Antes del 10 el inicio sigue igual: la primera sesión enseña una sola cosa por nivel (06 §2.1, regla 6). Pregunta Q-AV-3.

**El punto «nuevo»:** el punto de heliconia del Mapa (`MapTabDot`), con su canto crema, sale sobre el disco:
- hasta que el jugador abre el selector por primera vez;
- y cada vez que hay un avatar desbloqueado sin ver (`avatar.seen`).

**Al tocarlo:** se abre el **perfil** (3.2), con `af_avatar_open` y `source = home`. TalkBack: «Tu avatar: Tucán. Toca para cambiarlo» y, con el punto, «Hay un avatar nuevo».

### 3.2 Perfil y selector [P]

Es **una sola pantalla**, «Tu avatar», un modal de la familia de Ajustes (panel de madera clara, X arriba a la derecha, Atrás de Android la cierra). Se abre desde el inicio, desde una fila nueva de Ajustes, «Avatar» (arriba del todo, con el retrato de 32 dp y una flecha), y desde la fila del jugador en la Liga. De arriba abajo:

1. **Cabecera:**
   - el retrato elegido a 112 dp y, a su lado, el nombre del animal;
   - si la Liga ya le dio seudónimo, «En la Liga: Capibara 4821», con su bandera y el nombre del país;
   - si no, nada.

   Sin estadísticas en la 1.0 (pregunta Q-AV-10).
2. **Rejilla:**
   - de 4 columnas a 360 dp y de 3 a 320 dp o con el texto al 130 %, con celdas de 64 dp y el nombre debajo en una o dos líneas;
   - primero los libres y después los de hito, en el orden de la tabla 1.2, para que la fila de los bloqueados se lea como un camino;
   - se desplaza si no cabe, con TalkBack en listas (Q-B9-17).
3. **Estados de una celda:**
   - **elegido:** el aro en `Hoja` `_luz` #86D25A de 3 dp y una marca ✓ en una chapa;
   - **libre:** normal;
   - **nuevo:** la chapa «Nuevo» en heliconia con texto crema;
   - **bloqueado:** la silueta de 2.1 y, bajo el nombre, el **texto corto del hito** («Muy difícil», «Mundo 4 · Trapecio», «Crecida 10», «7 Retos seguidos», «20 sellos en un mes», «50 puntos de Liga»).
4. **Al tocar** un avatar libre: se elige en el acto, sin botón de confirmar. Pasa lo siguiente:
   - el retrato de la cabecera cambia, con un rebote de 150 ms, o un fundido con Reducir movimiento;
   - suena el `sfx` de UI de siempre;
   - se guarda el perfil y sale `af_avatar_select`;
   - si la Liga está activa, se encola el envío (4.4).
5. **Al tocar uno bloqueado:**
   - una sacudida de ±8 dp (guía v2 §6.3, el rechazo de siempre);
   - un globo bajo la celda con la frase entera del hito: «Gana tu primer nivel Muy difícil.» o «Completa Trapecio (te faltan 23 niveles).»;
   - no se elige.
6. Al cerrar el selector, los «Nuevo» que se vieron pasan a `avatar.seen`.

### 3.3 Liga [P]

**Fila del top** (`LeagueScreen.Row`): `rango · avatar de 32 dp · seudónimo · bandera con el país · puntos`.
- El avatar va entre el rango y el nombre, centrado en la fila (mínimo de 48 dp, sin crecer).
- Al 130 % y a 320 dp, el nombre ya se ajusta (`NoWrap` + ajuste). El avatar resta 40 dp al nombre, así que el test de 320 dp al 130 % con el peor seudónimo («Martim-pescador 4821») tiene que seguir cabiendo; si no, el país baja a la segunda línea, como ya hace la fila global.
- Sin `avatar` en la respuesta (servidor antiguo, o un jugador que nunca eligió): el **disco neutro**, el fondo Hoja con una hoja ovada crema, sin animal.
- Un índice desconocido (un avatar de una versión más nueva) también sale con el disco neutro.

**Fila del jugador** (fija abajo): su avatar del perfil local, no el que dice el servidor, para que un cambio se vea en el acto. Tocar la fila abre el perfil (`source = league`).

### 3.4 Victoria [P]

La victoria ya está llena (02 §6: una sola línea de texto por victoria, T6-A; panel único de D-029). Por eso:
- el avatar sale **solo en la línea de la Liga**, «⟨avatar 24 dp⟩ +{n} puntos de Liga», desde el 41 y con la Liga activa;
- el desbloqueo de un avatar **no añade nada a la victoria**: el aviso es el punto del inicio y el «Nuevo» del selector;
- el «perfil» que pidió el dueño es la pantalla 3.2.

Pregunta Q-AV-4, con la alternativa de un medallón en la cabecera de la victoria.

### 3.5 Primer arranque [P]

**Se asigna uno por defecto, sin preguntar:**
- uno de los 6 libres al azar, guardado en el primer guardado del perfil (azar de la UI, no del núcleo: la regla de determinismo es solo de `Packages/com.amazonfill.core`);
- así no hay pantalla nueva antes del nivel 1 (06 §2.1, regla 3: «sin pantallas de texto en el arranque») y la Liga sale variada desde el primer día;
- desde el 10, el punto «nuevo» invita a cambiarlo;
- `avatar.chosen` = false hasta la primera elección.

Pregunta Q-AV-2.

### 3.6 Accesibilidad (07 §7.8; 02 §12) [P]

- **TalkBack:**
  - el botón del inicio y la fila de Ajustes: «Tu avatar: {animal}. Toca para cambiarlo»;
  - cada celda: «{animal}, elegido», «{animal}, nuevo» o «{animal}, bloqueado. {frase del hito}»;
  - la cabecera: «{animal}. En la Liga: {seudónimo}, {país}».
- **En las filas de la Liga el avatar es decorativo y no se lee.** La fila ya dice «posición, seudónimo, país, puntos», y el seudónimo ya lleva un animal: «Tucán, Capibara 4821» confundiría.
- **Texto al 130 %:**
  - nombres de hasta 18 caracteres en dos líneas (el `max_chars` de `league_name_*`);
  - textos cortos de hito con `max_chars` 22;
  - la rejilla pasa a 3 columnas;
  - el globo del hito crece a lo alto;
  - se comprueba en es-419, en, pt-BR y en-XA a 360 × 780, 360 × 640 y 320 dp.
- **Reducir movimiento:** sin rebote ni sacudida (fundido de 150 ms), y el punto no late.
- **Sondas:**
  - `AF_LAYOUT lobby` con `avatar`;
  - `AF_LAYOUT avatar` con `close` y `avatar_<id>`;
  - `AF_STATE avatar`, con `id`, `chosen`, `unlocked`, `new` y `cause` (`select`, `unlock` o `merge`).

## 4. Datos

### 4.1 Perfil (08 §4.3) y fusión con la nube (08 §11) [P]

Campo nuevo, sin migración (como `map_seen_level`: el esquema aún no se ha publicado y entra con su default; `CurrentSchema` sigue en 1):

```json
"avatar": { "id": "toucan", "chosen": true, "at": "2026-10-27T15:04:05Z",
            "unlocked": ["dart_frog", "otter"], "seen": ["dart_frog"] }
```

- `id`: el elegido. Si falta o no está en el catálogo, se asigna uno libre (3.5).
- `chosen`: el jugador eligió alguna vez.
- `at`: el instante de la última elección, para la fusión.
- `unlocked`: los de hito ya desbloqueados (los libres no se apuntan). Nunca encoge.
- `seen`: los desbloqueados que ya vio en el selector. El punto del inicio sale si `unlocked` ⊄ `seen` o si `chosen` es false (desde el 10).
- **En la Liga, solo del dispositivo** (bloque `league`, que no sube a la nube): `league.avatar_sent`, el último índice que el servidor confirmó, y `league.avatar_pending`, el índice por enviar o null.

**Fusión** (`CloudMerge`):

```
merged.avatar.unlocked = union(local, cloud)
merged.avatar.seen     = union(local, cloud)
merged.avatar.chosen   = local.chosen OR cloud.chosen
(merged.avatar.id, merged.avatar.at) = el de at más reciente; sin at, el del ganador
después: reevaluar los desbloqueos con el perfil fusionado (1.3) y, si id no es libre ni está en unlocked, asignar uno libre
```

La elección es una preferencia, así que gana la más reciente y no la del blob con más nivel. `league.*` sigue siendo del dispositivo. El cambio de avatar que trajo la nube se encola para la Liga (4.4).

### 4.2 Analítica (08 §8.2) [P]

| Evento | Parámetros | Cuándo |
|---|---|---|
| `af_avatar_open` | `source ∈ {home, settings, league}` | Se abre el perfil |
| `af_avatar_select` | `avatar` (id), `free` (bool), `first` (bool: la primera elección del jugador) | Elige un avatar distinto del que tenía |
| `af_avatar_unlock` | `avatar` (id), `milestone ∈ {hard_win, very_hard_win, world_3, world_4, world_5, world_6, streak_10, daily_streak_7, daily_stamps_20, league_50}`, `retro` (bool: desbloqueado al cargar un perfil anterior) | Se desbloquea un avatar |

- Llevan los 15 comunes y ≤ 3 propios (08 §8.1).
- El id del avatar no es dato personal: es uno de 16 dibujos del juego.
- Nunca llevan seudónimo ni país (regla de `localizacion.md`, como los de la Liga).
- `af_settings_change` no cambia: el avatar no es un ajuste.
- Para el panel de 06 §12: % de jugadores que eligen avatar antes del día 7 y popularidad por id.

### 4.3 Remote Config (08 §9) [P]

Una sola clave nueva, de respaldo:

| Clave | Tipo | Default | Uso |
|---|---|---|---|
| `ui_avatars_enabled` | bool | `true` | Apagada: sin botón en el inicio, sin fila en Ajustes y sin avatar en la Liga ni en la victoria. El perfil conserva el campo y la Liga deja de enviarlo. |

Los hitos van en el código (`AvatarCatalog`) y no en Remote Config, porque una regla de colección que cambiara por servidor rompería la promesa de «condición de desbloqueo visible» (06 §4.6). El envío a la Liga sigue a `live_league_enabled`, sin clave propia. La tabla de 08 §9 pasa de 254 a 255 claves, y `rc_defaults.py --check` lo comprueba.

### 4.4 Contrato de la Liga (`infra/contracts/v1`) [P]

Primero se cambia `docs/dev/liga.md` §3, en un commit del integrador (liga.md, «Esta especificación es la fuente única de los dos lados»), y después el contrato y el backend.

**Compatible hacia atrás con v1:**
- todo lo nuevo es **opcional** y **aditivo**;
- los clientes de hoy leen las respuestas con `strict: false` (`Backend/Contract/League.cs`, `TryParse`) e ignoran un campo que no conocen;
- el cliente nuevo tolera que falte (disco neutro).

Cambios:
1. **Ruta nueva** `POST /v1/league/avatar` (operación `leagueAvatar`, función `league-avatar`, JWT + App Check como las otras dos, throttle `default`):
   - petición `league-avatar.request.schema.json`: `{ "avatar": 7 }`, con `avatar` entero de 0 a 63 (`additionalProperties: false`);
   - respuesta 200 `league-avatar.response.schema.json`: `{ "avatar": 7 }`.
   - **El servidor no valida contra la lista de 16, solo el rango:** un avatar nuevo de la 1.1 no exige desplegar el backend, igual que los niveles que no están en la tabla (liga.md §3.2).
   - Errores: 400 `bad_request` (fuera de rango o mal formado), 401, 403 `integrity_failed`, 429 `rate_limited` (más de 30 cambios en 24 h por UID [P]).
   - Si el UID aún no tiene seudónimo, **lo crea** (la misma función que `leagueWins`) y guarda el avatar con él, para que el jugador salga con su avatar desde su primera victoria.
2. **`league-board.response`:** `top[].avatar` y `me.avatar`, opcionales, `integer` 0-63 o `null` (null = nunca eligió).
3. **`league-wins.response`:** `avatar`, opcional, el mismo tipo. La petición de victorias **no cambia** (`wins` sigue con `minItems` 1; el avatar no viaja con las victorias, para no mezclar un cambio de preferencia con la cola idempotente).
4. **Ejemplos** nuevos en `examples/` (petición, respuesta, petición inválida fuera de rango) y su entrada en `index.json`. Los ejemplos de hoy siguen siendo válidos (el campo es opcional).

**Qué cambia en `infra/`:**
- `functions/league/service.ts`:
  - `Alias` gana `avatar: number | null`;
  - `boardChanges` copia `avatar` en las filas de `league_board`, como ya copia `name_key` y `name_num`;
  - `leagueBoard` lo devuelve en `top` y en `me`.
- `functions/league-avatar/handler.ts` (nuevo):
  - escribe `avatar` en la fila de `league_names` del UID;
  - en la **misma transacción**, reescribe sus filas de la semana en curso (global y país) si existen, para que el top lo vea sin esperar a otra victoria;
  - comprueba la marca de cuenta borrada, como `leagueWins`;
  - el límite de 30 por día va en un contador con TTL en la misma fila.
- `lib/api-stack.ts` (la ruta) y `lib/contract.ts` (si enumera operaciones).
- Tests de contrato y unitarios:
  - cambio con y sin seudónimo previo;
  - reescritura de filas de top;
  - 0, 63 y 64;
  - el límite;
  - cuenta borrada.
- **Borrado de cuenta:**
  - `deleteAccount` ya elimina la fila de `league_names` y las del top (liga.md §3.2; `infra/LEAGUE.md`), así que el avatar se va con ellas;
  - se añade un test que lo afirme y que compruebe que ninguna fila nueva guarda el avatar fuera de esas tablas;
  - el contador del límite vive en la misma fila y se borra con ella.
- **Coste:** una escritura transaccional de unas 3 filas por cambio. Con unos 2 cambios por jugador al mes es despreciable frente a la tabla de `infra/LEAGUE.md`.
- `infra/LEAGUE.md` documenta la ruta. El despliegue lo hace el dueño (CLAUDE.md, `infra/`).

**Cliente:**
- `AF.Backend` gana `LeagueAvatarRequestDto` y `LeagueAvatarResponseDto` y lee `avatar` en las dos respuestas.
- `AF.LiveOps.LeagueService`:
  - envía `league.avatar_pending` cuando la Liga está activa y desbloqueada;
  - envía tras elegir, al reconectar y al primer envío de la cola si `avatar_sent` ≠ el elegido.
- Errores:
  - con un 404 de ruta (backend aún sin desplegar) o un 429, deja el pendiente para la siguiente apertura, sin `af_league_unavailable` repetido;
  - con un 400 (un índice que el servidor no admite), descarta el pendiente y lo anota en `af_error{module=league}`.

## 5. Privacidad (D-033; liga.md §6) [P]

- **El índice no identifica a nadie:**
  - es la elección entre 16 dibujos del juego, sin foto, sin texto libre ni nada que el jugador escriba;
  - **no hay moderación** que hacer;
  - va unido al UID anónimo igual que el seudónimo, así que se trata **igual que el seudónimo**: pseudónimo, visible a otros jugadores en la Liga y borrado con la cuenta.
- **13-17:** lo ven igual que 18+, como el seudónimo y la bandera (liga.md §1). <13 no juega en la 1.0 (08 §13.1 #11), y con esa banda guardada no hay red (D-033, añadido del 2026-10-23); el avatar funciona en local sin red.
- **La política:** una frase en los tres idiomas, en el párrafo de la Liga: «… un seudónimo automático, el avatar de animal que elijas entre los del juego, la bandera del país y los puntos de la semana…» `[[ABOGADO: revisar]]`. Lo escribe quien lleve los textos legales de la web (liga.md §6).
- **Seguridad de los datos de Play:** sin categoría nueva. Entra en «Actividad en la app: otras acciones», ya declarada para la Liga `[[ABOGADO: revisar]]`.
- **La analítica** lleva el id del avatar, nunca junto al seudónimo ni al país (4.2).

## 6. El plan por carriles

| # | Carril | Agente | Qué | Tamaño | Depende de |
|---|---|---|---|---|---|
| 0 | `tanda6/avatares-gdd` | integrador (docs) | Con las respuestas de la sección 7: 06 §4.7 (Lobby y la pantalla «Tu avatar»), 06 §4.6 (el avatar no es el Cuaderno), 07 §7.9, 08 §4.3, §8.2, §9 (+1 clave) y §11 (la fusión), glosario §10 y §11, **liga.md §1, §3 y §4** (contrato), GDD.md §2 y §7. Regenerar `rc_defaults.py`. | S | Respuestas del dueño |
| 1 | `tanda6/avatares-textos` | integrador o Gemini (D-054) | `strings.csv`:<br>• ~30 filas nuevas (`avatar_title`, `avatar_settings_row`, `avatar_home_a11y`, `avatar_new`, `avatar_new_dot_a11y`, `avatar_chosen_a11y`, `avatar_locked_a11y`, `avatar_league_alias`, y `avatar_goal_<hito>` y `avatar_goal_<hito>_long` para los 10 hitos, con `{n}` y `{world}`);<br>• `context` y `max_chars`;<br>• borrador en es-419, en y pt-BR.<br>Añadir a la `context` de las 16 `league_name_*` «también nombre de avatar (avatares.md)». `apply_translations.py --check`. | S | 0 |
| 2 | `tanda6/avatares-backend` | Codex (`infra/`) | 4.4: esquemas, ejemplos, `index.json`, `league-avatar`, `service.ts`, `api-stack.ts`, tests, `infra/LEAGUE.md`. Verificación sin credenciales: `npm ci`, lint, typecheck, tests y `cdk synth`. Despliega el dueño. | M (~300-400 líneas) | 0 (liga.md) |
| 3a | `tanda6/avatares-datos` | unity-gameplay-dev (sin Editor para los tests de dominio) | `AvatarCatalog` y `AvatarRules` (sin motor, en `AF.LiveOps`), `avatar` en `Profile.cs`, `CloudMerge.Fields.cs`, la evaluación de 1.3 enganchada a victoria, Reto, Crecida y Liga, los eventos de 4.2 y la clave de 4.3. Tests EditMode: hitos, retroactividad, fusión y default. | M | 0 |
| 3b | `tanda6/avatares-arte` | unity-gameplay-dev (Editor) | `ProceduralArt.Avatars.cs`: los 16 retratos, el bloqueado, el disco neutro, el atlas perezoso por fotogramas y la ranura `avatar_<id>`. Tests: sin NaN, opacidad del disco, tiempo por retrato y sin asignaciones por fotograma. Capturas para el dueño (D-050: «el dueño ve capturas antes de integrar»). Si pasa de 400 líneas, en dos PR de 8 y 8. | L | 0; en paralelo con 3a |
| 3c | `tanda6/avatares-ui` | unity-gameplay-dev (Editor) | Botón del inicio y su reflujo de la fila de arriba (3.1), la pantalla «Tu avatar» (3.2), la fila de Ajustes, TalkBack, 130 % y sondas. Tests PlayMode de composición a 360 × 780, 360 × 640 y 320 dp, al 100 % y al 130 %, en los cuatro idiomas. | M-L | 1, 3a, 3b |
| 3d | `tanda6/avatares-liga` | unity-gameplay-dev | DTO de `AF.Backend` con `avatar`, el envío de 4.4 (`FakeLeagueApi` primero), el avatar en las filas de la Liga y en la línea de la victoria (3.3 y 3.4). Tests de contrato con los ejemplos nuevos. | M | 2 (contrato fusionado, no desplegado), 3a, 3c |
| 4 | prueba en el teléfono | dueño + escritorio | Jornada corta: elegir avatar, desbloquear por la sonda (extra `af_avatar_unlock`) y ver la Liga en dev con el backend desplegado. | — | 2 desplegado, 3c, 3d |

**Orden y paralelo:**
- 0 primero, porque congela el contrato y los nombres;
- después, a la vez: 1, 2, 3a y 3b (dos de C# o Node puros y uno con Editor);
- después 3c y, al final, 3d.

Caben en el semáforo de Unity y en el tope de carriles (máximo 2 con Editor a la vez: 3b y 3c no coinciden).

**Verificación de los carriles de Unity:**
- `dotnet build dotnet/AmazonFill.slnx -warnaserror` y `dotnet test`;
- `scripts/unity-compile.ps1`;
- `unity-test.ps1 -Mode EditMode` y `-Mode PlayMode -TimeoutSec 7200`;
- en 3a, además, `python tools/remote-config/rc_defaults.py --check` y sus tests;
- en 1, `python tools/loc/apply_translations.py --check`.

## 7. Preguntas abiertas para el dueño

Cada una con la recomendada primero. Si el dueño responde «las recomendadas», se aplican todas.

| # | Pregunta | Recomendada | Alternativas |
|---|---|---|---|
| Q-AV-1 | ¿Qué animales? | **(a) Los 16 de 1.2**, sin los del elenco (perezoso, capibara y boto) ni los vetados por el glosario | (b) Añadir la capibara como libre, dibujada distinta de Capi. (c) Menos, 12: quitar garza, coatí, armadillo y colibrí. |
| Q-AV-2 | ¿Primer arranque? | **(a) Uno libre al azar, sin preguntar**, con el punto «nuevo» desde el 10 | (b) Elegir en una pantalla al ganar el 10, dentro de la secuencia de cuatro pantallas de 06 §2.3, que pasaría a cinco. (c) Siempre el mismo por defecto (el guacamayo). |
| Q-AV-3 | ¿Dónde en el inicio y desde cuándo? | **(a) Arriba a la izquierda, desde el 10**, con Semillas y Canoas un poco más estrechas | (b) Lo mismo, desde el nivel 1. (c) Una cuarta pestaña abajo, a la derecha del Mapa (a 320 dp obliga a estrechar «Inicio»). |
| Q-AV-4 | ¿Avatar en la victoria? | **(a) Solo en la línea «+{n} puntos de Liga»**; el desbloqueo se avisa con el punto del inicio | (b) Además, un medallón de 40 dp junto a la cinta de la victoria en todas las victorias. (c) Una tarjeta «¡Nuevo avatar!» en la victoria del desbloqueo, que cuenta como el takeover de la sesión (`ui_max_takeovers_per_session`). |
| Q-AV-5 | ¿El seudónimo sigue al avatar? | **(a) No: son independientes**. El seudónimo lo da el servidor y no se edita en la 1.0 (liga.md §1) | (b) Si el avatar está en la lista de la Liga, el seudónimo cambia a ese animal (exige cambiar el contrato de nombres y la reserva de parejas). |
| Q-AV-6 | ¿Cómo se ve uno bloqueado? | **(a) Silueta, chapa del hito y texto** (se sabe qué es y cómo se gana) | (b) Disco con «?» y solo el hito: el animal es sorpresa. |
| Q-AV-7 | ¿Los hitos de 1.2 valen? | **(a) Sí, los diez** | (b) Cambiar alguno; por ejemplo, el ocelote de la Liga por «top 10 % del país» en una semana. |
| Q-AV-8 | ¿Y el avatar de la Liga con la Liga apagada? | **(a) Oculto en el selector** hasta que la Liga esté activa | (b) Visible y bloqueado con «Llega con la Liga». |
| Q-AV-9 | ¿El avatar se ve en la Liga para 13-17? | **(a) Sí**, como el seudónimo y la bandera | (b) Solo para 18+. |
| Q-AV-10 | ¿Qué lleva el perfil? | **(a) Solo cabecera (avatar, seudónimo de la Liga y país) y rejilla** | (b) Además 3 cifras: niveles ganados, paradas del Mapa y Retos sellados (`stats` y el Mapa ya las tienen). |
| Q-AV-11 | ¿Marcos alrededor del avatar? | **(a) En la 1.2, con la Liga completa** (`entitlements.frames`); el aro de madera les guarda el sitio | (b) Ya en la 1.0, el de 20 sellos del Reto como marco en lugar de la garza. |
| Q-AV-12 | Hallazgo: el seudónimo de la Liga incluye la **anaconda**, que el glosario §3.3 descarta (sagrada para varios pueblos) | **(a) Retirarla de los seudónimos nuevos** sin cambiar el índice (el servidor deja de elegirla para los seudónimos nuevos; quien ya la tiene la conserva y la clave sigue en el `enum` para leerla) | (b) Dejarla: es solo texto, sin dibujo. |
