# Encargo de arte: TiTino, Frutas del río y la Tinaja

Fecha: 2026-10-14 · Autor: documentación (agente; tanda 4, tarea M1.8) · Para: el dueño, que lo envía al artista 2D freelance, y el asesor cultural · Estado: **Frutas del río y la Tinaja (piezas y sonidos), listas para enviar al fusionar este cambio; TiTino, condicionado** (sección 0).

Fuentes:
- `docs/dev/decisiones.md`: D-016 («Consecuencias»: el asesor, el artista y el abogado), D-017 («Arte») y D-019 («Consecuencias», coste);
- glosario §3.4, §9 y §11;
- 07 §2.1, §4.1, §4.3, §4.5, §6.4, §6.5, §8.1, §8.2, §9.2, §9.5 y §11;
- `docs/research/personaje/propuesta-titino.md` c.1, c.4-c.7 y g.2;
- `docs/research/frutas-del-rio/propuesta-frutas-del-rio.md` §3.2, §7 y F9;
- `docs/research/cultura-ribera/propuesta-tinaja.md` b.2 («Aspecto» y «Cultura: la trama con fuentes»), c.7 y (h);
- `docs/art/direccion-visual-v2.md` §7;
- CLAUDE.md, «Humanos».

Las ranuras con nombre (`fauna_bradypus_variegatus_partes`, `fauna_bradypus_variegatus_retrato`, `arbol_embauba` y `ficha_<id del tinte>`) están en `Assets/_Project/Art/Generated/README.md`, con su medida, su respaldo y su compresión. El fondo del Mapa y sus piezas van en `docs/art/encargo-mapa-del-rio.md`.

Convenciones: **u**, alto de una dosis, igual a la mitad del ancho de la cuia (07 §4.1); **[P]**, propuesta pendiente de calibrar; **[E]**, estimación.

## 0. Cuándo se envía cada parte

| Parte | Se envía | Antes de integrarla | Quién la integra |
|---|---|---|---|
| **Frutas del río:** escenas de ficha, sello y marcos (sección 2) | **Al fusionar este cambio**, empezando por las cinco primeras: açaí, Agua del Tapajós, achiote, Agua del Río Negro y copoazú | Revisión del asesor de cada escena con la lista de 07 §8.2; el achiote y el huito, con cuidado especial por la pintura corporal | La tarea de integración del arte final (AR1), una entrega por PR |
| **La Tinaja:** piezas y 4 sonidos (sección 3) | Las piezas y los 4 sonidos, **al fusionar este cambio**. Los nombres de las piezas y los IDs de los sonidos se fijan en el glosario §11 antes de integrarlos (sección 5, pendientes 1 y 2) | Revisión del asesor: el gesto de sacar, el diseño neutro y su cuia colgada del jirau (sección 3.4) | Ídem |
| **TiTino:** hoja de modelo, rig, retrato, rama y bocadillo (sección 4) | **Solo cuando se cumplan las dos condiciones del recuadro** (D-016). Las piezas del rig (`fauna_bradypus_variegatus_partes`) esperan además a su plantilla de celdas y pivotes [P] (4.1; sección 5, pendiente 3); la hoja de modelo, el retrato, la rama y el bocadillo no la necesitan | El asesor revisa la hoja de modelo antes del arte final | Ídem |

> **Condición de TiTino (D-016).** El encargo de TiTino no se envía hasta que:
> 1. **el asesor cultural da su visto bueno** a la especie (*Bradypus variegatus*, el perezoso de tres dedos de garganta marrón) y al árbol: la embaúba o el yarumo. Puede tener un uso ritual: en la Amazonia colombiana, la ceniza de sus hojas se mezcla con la coca para hacer el mambe, de uso ceremonial [E];
> 2. **la búsqueda de marca del nombre está en marcha:** el abogado incluye «TiTino» y «Titino» en la de Tintalú.
>
> El arte no lleva el nombre escrito, pero el personaje es la cara del juego (glosario §3.4). Si el asesor desaconseja la embaúba, se usa la alternativa: una rama genérica sin especie reconocible o la que él proponga. Su visto bueno a la embaúba vale también para la casa de TiTino en el Mapa (`map_home` y `map_home_window`; `docs/art/encargo-mapa-del-rio.md`, sección 4).
>
> Las dos condiciones son las de D-016 y valen para todo el encargo de TiTino. Las piezas del rig piden además su plantilla (4.1), que aún no existe [P]; no frena la hoja de modelo, que va primero porque pasa por el asesor antes del arte final.

## 1. Reglas para todas las piezas

- **Estilo:** D-003, «Selva brillante premium»: vidrio, madera y luz de juego. **Nada de papel, pergamino, acuarela ni lámina botánica**, aunque el tema invite.
- **Luz:** desde arriba a la izquierda, a 35° en la fauna.
- **Estilo de la fauna** (07 §6.4):
  - contorno de 2 px de tinta (#2B2118) al 80 %;
  - ojos con brillo;
  - postura animal;
  - saturación por debajo de la de los 14 tintes.
- **Lo que nunca aparece:** la lista «Evitar» de la guía visual (§7) y la columna «No» de 07 §8.1. La fila «Lugar equivocado» de la guía se lee así:
  - la «playa» vetada es la de mar, con cocoteros y turquesa caribeño (07 §2.1). La playa blanca de río del Tapajós sí va, en `ficha_agua_tapajos` (2.2; propuesta de Frutas §7);
  - las montañas solo van donde las nombra el canon: aquí, la ladera andina-amazónica de `ficha_lulo` (2.2; propuesta de Frutas §7).
- **Revisión de cada pieza:** la lista de 07 §8.2 (personas o símbolos, decoración propia, lugar de la cuenca, palabras permitidas y visto bueno del asesor). Todo nace «pendiente del asesor».

## 2. Frutas del río (D-017)

### 2.1 Piezas

| Pieza | Ranura | Medida | Cantidad | Uso y notas |
|---|---|---|---|---|
| Escena de ficha | `ficha_<id del tinte>` [P] | 1024 × 640 | 14 | Cabecera de la ficha «Frutas del río» y, en la 1.1, de la página Frutas del Cuaderno: **la planta en su paisaje y el fruto entero y abierto**. La silueta de lo que aún no se ha revelado la genera el código |
| Sello «Ya conoces» [P] | Sin nombre de ranura todavía (sección 5) | 256 × 256 | 1 | Contador 20/20 y ficha completa: un medallón de madera con un hueco para el icono de la fruta. **Nunca la semilla ámbar ovalada**, que es el icono de Semillas |
| Marcos de la línea y de la ficha | Sin nombre de ranura todavía (sección 5) | 9-slice | 2 | Del kit de UI (07 §7): madera clara y vidrio |

Los iconos `fruta_<id del tinte>` (256 × 256) ya estaban en la lista de D-010 (07 §6.4): no son encargo nuevo. Hasta que lleguen las escenas, la ficha funciona solo con el icono.

### 2.2 Las 14 escenas, en el orden de las series

| Orden | Ranura | Tinte | Serie (niveles) | Qué cuidar en la escena |
|---|---|---|---|---|
| 1 | `ficha_acai` | Açaí | 1-20 | Nadie trepando la palmera: nada de peconha |
| 2 | `ficha_agua_tapajos` | Agua del Tapajós | 21-40 | Una playa blanca de río con agua turquesa |
| 3 | `ficha_urucum` | Achiote | 41-60 | Nada de pintura corporal |
| 4 | `ficha_agua_negro` | Agua del Río Negro | 61-80 | El Encuentro de las Aguas visto desde arriba |
| 5 | `ficha_cupuacu` | Copoazú | 81-100 | — |
| 6 | `ficha_aguaje` | Aguaje | 101-120 | Sin cosecha a la vista: ni personas ni subidor, que solo se usa trepando. La palmera, en pie, nunca talada ni caída (07 §8.1). El subidor queda para los textos (el Dato del río), con el asesor |
| 7 | `ficha_bacaba` | Bacaba | 121-140 | Con su cáscara morada real: su rosa es una estilización del tinte, y el asesor y el artista deciden si la escena lo matiza |
| 8 | `ficha_jenipapo` | Huito | 141-160 | Nada de pintura corporal |
| 9 | `ficha_camu_camu` | Camu camu | 161-180 | — |
| 10 | `ficha_tapereba` | Taperebá | 181-200 | — |
| 11 | `ficha_cacao` | Cacao | 201-220 | Siempre en mazorca, nunca un grano suelto |
| 12 | `ficha_lulo` | Lulo | 221-240 | En la ladera andina-amazónica, nunca en el llano de Leticia |
| 13 | `ficha_guanabana` | Guanábana | 241-260 | — |
| 14 | `ficha_araza` | Arazá | 301-320 | — |

**Prioridad:** las cinco primeras, que cubren hasta el nivel 100.

**En todas las escenas** (propuesta de Frutas §7; 07 §8.1):
- sin personas, aldeas ni manos;
- sin árboles sagrados (sumaúma, lupuna);
- sin el Ver-o-Peso con gente.

**Coste [E]:**
- 14 escenas de medio día a un día cada una: 7-14 días de artista;
- el sello y los marcos, 1 día.

## 3. La Tinaja (M13; D-019)

### 3.1 Piezas

| Pieza | Qué es | Notas |
|---|---|---|
| Cuerpo | Vasija utilitaria lisa de barro cocido claro, entre **#C4906E y #C7A182**. Más alta que ancha, con hombro, cuello corto y boca de 0,70 del ancho de la cuia o más. Ocupa una ranura con el alto de la Cuia alta de 5 (6,60 u; 07 §4.5) | Decoración: ninguna, o una sola línea incisa propia. Gotitas quietas de «sudor» que pillan la luz (el barro enfría el agua) |
| Máscara de la boca | La elipse de la boca, donde el código pinta el tinte y el icono de la fruta de arriba | La boca mide unos 34 dp con la cuia de 48 dp, y el icono, 14-16 dp |
| Bandas encendidas | Las capas ya reveladas, como bandas de luz en la barriga, a su altura | El icono lo pone el código. Con la Luciérnaga y con la Pororoca, la barriga se enciende por dentro y muestra sus bandas |
| Boca abajo | La tinaja vacía, tumbada boca abajo sobre el jirau, a secar | Es la suelta: su cuia baja del jirau y queda libre |
| Jirau | La tabla de varas de madera bajo las dos ranuras, la de la tinaja y la de su cuia | Es la plataforma donde se ponen los utensilios, no un pie. Es estado permanente, no un resaltado: no cambia al seleccionar (D-002). Se estira en horizontal [P] |
| Asa de la cuia | El asa pequeña de fibra de la que su cuia chica cuelga del jirau, como las cuias pitingas | Solo arte, para que se lea como el cazo de la casa. La cuia de vidrio la dibuja el código |
| 4 sonidos | «Glup» de calabaza en el agua al sacar; gota en el revelado; roce de fibra en la suelta; y el «toc», hueco en la suelta y grave de barro al seleccionarla (07 §9.2) | Nunca burbujas ni hervor. Originales o con licencia comercial perpetua; nada CC BY-NC (07 §9.5). Si el artista 2D no hace audio, van al diseñador de sonido de los efectos (07 §11; brief de audio de 07 §8.2). Aún no tienen ID: se envían al fusionar este cambio, como las piezas, y su ID se fija en el glosario §11 antes de integrarlos (sección 5, pendiente 2) |

- **Medida [P]:** se dibuja en vector o a 4× del tamaño máximo, como la cuia (07 §4.1). El canon fija el alto (6,60 u, el de la Cuia alta de 5; 07 §4.5), pero no el ancho del cuerpo: solo dice que es más alta que ancha, que ocupa una ranura y que la boca mide 0,70 del ancho de la cuia o más (07 §4.3; propuesta de la Tinaja, b.2, «Aspecto»). **Suponiendo un ancho igual al de la cuia (2 u)**, el lienzo mide **256 × 845 px** a 4× [cálculo]:
  - de ancho, 2 u × 32 dp × 4 px/dp;
  - de alto, 6,60 u × 32 dp × 4 px/dp.

  Si el hombro pide más ancho, el límite es la ranura. La cuia mide 0,70 de su ranura, o menos cuando la limita el tope por número de cuias (07 §4.1), así que la ranura mide al menos 1/0,70 del ancho de la cuia: la tinaja nunca pasa de ≈ 2,86 u, **366 px** a 4× [cálculo].
- **Un solo diseño neutro en todos los mundos**, sin versiones regionales.

### 3.2 Vetos (07 §4.3 y §8.1; propuesta de la Tinaja, «Aspecto»)

- **Decoración:** nada de kené, marajoara ni tapajônica.
- **Forma:**
  - ni cara, ni dos bocas, ni forma de cruz, ni tamaño de urna;
  - que no recuerde el chomo shipibo, las tinajas kukama, la mucawa, la igaçaba ni el camucim (07 §8.1). El chomo y las tinajas kukama guardan masato, la mucawa es de chicha ceremonial, y la igaçaba y el camucim son urnas funerarias (propuesta de la Tinaja, b.2, «Cultura: la trama con fuentes»).
- **El caldero de Magic Sort:** nunca redonda y baja, ni morada, ni con fuego, vapor o burbujas.
- **Lo que evoca el masato:**
  - sin hoja en la boca: las tinajas de masato se tapan con hojas de plátano;
  - nadie recibe ni bebe lo que se saca;
  - nunca cuencos que pasan de mano en mano.
- **Nada de cordón, liana ni nudo** entre la tinaja y su cuia: es la imagen de la Liana (M3).
- **Color:**
  - nunca el «?» ni el pardo de Aguas turbias (#8A7B6A), porque el barro es opaco y no es agua turbia;
  - vetado el terracota #B5653A, a ΔE00 5,8 del Agua del Río Negro.
- **Tiene que distinguirse de:**
  - el Cántaro (la alcancía con semillas);
  - el lodo oscuro del Barro;
  - el agua parda con «?» de Aguas turbias;
  - la Liana.
- **Capi nunca la usa.**

### 3.3 Lo que no es arte

El gesto de sacar (la cuia chica entra por la boca y sale goteando), el revelado, la suelta y el balanceo al seleccionarla los anima el código (propuesta de la Tinaja, b.2, «Animación y sonido»): el artista solo entrega las piezas.

### 3.4 Puntos para el asesor (propuesta de la Tinaja, h)

1. **El gesto de sacar.** En la Amazonía peruana, la tinaja con la bebida guardada y el pate que saca es la escena del masato. ¿Evoca el masato? Si el asesor confirma el riesgo, rige Q-Ti7 A de D-019: se mantiene el gesto con el encuadre «solo agua», sin servir a nadie.
2. **El estreno en un mundo peruano** (7, Madre de Dios) con un dato de Pará.
3. **Su cuia colgada del jirau,** como las cuias pitingas: ¿choca con algún uso real?
4. **La comprobación del diseño final:** el asesor confirma que el diseño final no recuerda ninguna de las vasijas que 07 §8.1 ya veta (3.2, «Forma»). No es una pregunta abierta: el veto es del canon.
5. **Sus nombres** en PT y EN.

**Coste:** D-019 no lo estima en días de artista.

## 4. TiTino (D-016)

**Solo se envía con la condición del recuadro de la sección 0;** las piezas del rig, además, con su plantilla (4.1).

### 4.1 Piezas (propuesta de TiTino, c.7)

| Pieza | Ranura | Medida | Notas |
|---|---|---|---|
| Hoja de modelo | En `docs/art/`; no va en el build | — | Frente, perfil, 3/4, espalda (con el espéculo), colgado y sentado. Seis expresiones: calma, alegría, ánimo, curiosidad, sueño y sorpresa suave. **Pasa por el asesor antes del arte final** |
| Piezas del rig | `fauna_bradypus_variegatus_partes` | 1024 × 1024, rejilla de 4 × 4 celdas de 256 px con pivotes fijos | El método de los miquitos (07 §6.4): el artista dibuja las piezas sobre una plantilla de celdas y pivotes, y el código anima las poses. Incluye las hojas de `sloth_cheer` |
| Retrato | `fauna_bradypus_variegatus_retrato` | 512 × 512 | Aviso del tutorial, bocadillos, placa del Dato del río en el Mapa e icono grande de las notificaciones |
| Rama de embaúba | `arbol_embauba` | 1024 × 256 | Entra con él en cada pantalla: nunca cuelga de un botón, de una cuia ni de una tarjeta. Nada de hojas quemadas ni ceniza |
| Bocadillo de texto | Sin nombre de ranura todavía (sección 5) | 9-slice | Vidrio esmerilado con borde de madera clara (D-003), **nunca papel** |

**Plantilla del rig [P]:** como en los miquitos (07 §6.4), el artista dibuja las piezas sobre una plantilla de celdas y pivotes fijos, que se le entrega con las piezas del rig. Aún no existe, y ninguna tarea la produce: TI4 hace el rig provisional de desarrollo, pero no la plantilla para el artista (sección 5, pendiente 3). Las piezas del rig se envían cuando exista; la hoja de modelo, el retrato, la rama y el bocadillo no la esperan.

**Quedan fuera de este encargo** y se piden aparte (propuesta c.7):
- el arte de marca `key_art_sloth` (ficha, capturas y concepto D del icono);
- los sonidos `sfx_sloth_*`.

### 4.2 Quién es y cómo se ve (propuesta c.1 y c.4-c.5; 07 §6.5)

- **Especie y edad:** un perezoso de tres dedos de garganta marrón (*Bradypus variegatus*), **macho adulto joven**, nunca una cría, con el **espéculo** naranja y su raya negra en la espalda.
- **Dónde vive:** siempre salvaje y libre en su embaúba de la desembocadura.
- **Rasgos:**
  1. cabeza redonda y grande (≈ 1/3 del alto colgado), con las orejas casi invisibles bajo el pelo: nada de orejas redondas ni hocico de oso;
  2. cara clara con **antifaz oscuro** y ojos pequeños con un brillo, que miran al jugador o al río, **nunca a una cuia** (D-002);
  3. la «sonrisa» es la línea natural de la boca: sin labios, dientes ni sonrisa humana. La emoción va en los ojos, la cabeza y el cuerpo;
  4. tres garras largas, curvas, redondeadas y claras en cada extremidad;
  5. pelo largo y áspero, gris pardo con un **velo verde de algas**. Si se quiere, una polilla parda diminuta; nunca azul, porque la mariposa azul es la Morpho;
  6. postura de perezoso: colgado boca abajo o sentado en una horquilla, abrazado a la rama. Nunca de pie como una persona, aplaudiendo ni con los brazos en jarras;
  7. su saludo es levantar despacio una garra desde la rama o girar la cabeza hacia ti.
- **Legibilidad:** a 40 dp y de espaldas, sobre la selva clara y sobre el panel azul petróleo (D-004).

**Paleta orientativa [P]** (la fija el artista; se distingue del pardo de Capi y del rosa de Rosa):

| Parte | HEX |
|---|---|
| Pelaje base, gris pardo | #9A8F7A |
| Velo de algas en el lomo y los hombros (20-30 %) | #8C9C6A |
| Cara | #E3D8C2 |
| Antifaz | #4B3A2C |
| Nariz y contorno | #2B2118 |
| Garras, de hueso claro | #CDBFA3 |
| Espéculo, con su raya central | #E39A3B y #2B2118 |

**Poses que tiene que permitir el rig** (las anima el código; c.6): `sloth_idle`, `sloth_wake`, `sloth_wave`, `sloth_present`, `sloth_cheer`, `sloth_cheer_big`, `sloth_encourage`, `sloth_look`, `sloth_sunbathe` y `sloth_sleep`.

**Tamaños en pantalla [P]:**
- arranque, 140-180 dp; inicio, 120-160 dp;
- victoria, 72 dp con bocadillo o 48 dp solo con el gesto;
- Nivel fallido, 64 dp; retrato de aviso, 40 dp;
- Mapa del río, 64-96 dp.

Nunca en el tablero durante la partida.

### 4.3 Reglas duras que tocan al arte (propuesta de TiTino, g.2; 07 §6.5)

1. Siempre salvaje y libre en su árbol (la embaúba, nunca la sumaúma ni la lupuna) o cruzando el río a nado.
2. Sin personas: nunca en brazos, manos u hombros humanos, nunca junto a una persona, nunca abrazando al jugador, a la cámara ni a un peluche.
3. Sin ropa, gorro, collar, correa, pañal, accesorios ni pintura (urucum o huito).
4. Nunca dentro de una cuia, una cesta, un cubo o una caja.
5. Nunca comiendo de una mano ni alimentado por el jugador.
6. Nunca una cría sola ni un «huérfano rescatado».
7. **Nunca el brazo estirado hacia la cámara:** es la señal de miedo del perezoso, no un saludo.
8. Nunca en pantallas de pago ni vendiendo.
9. Nunca en peligro, herido, atrapado o triste, tampoco en anuncios.
10. Sin chistes de vago, sin la bajada al suelo y sin la «sonrisa» como prueba de que está feliz.
11. Ningún nombre ni dato cerrado sin el asesor.

**Coste [E]:** unos +5-6 días de artista por TiTino, con su hoja de modelo y sus 10 poses (D-016, «Consecuencias»).

## 5. Entrega y pendientes

**Entrega** (como en `docs/art/encargo-mapa-del-rio.md`, sección 6):
- PNG y, si los hay, PSD o PSB con capas, por **Git LFS**; los sonidos también van por LFS (`.gitattributes`: `*.wav` y `*.ogg`);
- cada PNG con el nombre exacto de su ranura, en `Assets/_Project/Art/Generated/`;
- el `.meta` lo genera Unity, y los ajustes de importación los aplica el script de Editor versionado;
- los agentes solo integran, y cada PNG se anota en el registro del README como «Artista»;
- el crédito del artista y del asesor va en la pantalla de créditos (07 §8.2).

**Pendientes.** Ninguno bloquea el envío de Frutas del río ni el de la Tinaja (piezas y sonidos). El 3 bloquea el de las piezas del rig de TiTino (no el de su hoja de modelo).
1. **Nombres de ranura** en el glosario §11, antes de integrar:
   - el sello «Ya conoces» y los dos marcos de Frutas del río;
   - el bocadillo de TiTino;
   - las piezas de la Tinaja.

   Hasta entonces, ningún PNG lleva un nombre inventado.
2. **Los IDs de los 4 sonidos de la Tinaja:** aún no tienen. Se envían igual que las piezas, al fusionar este cambio, y sus IDs se fijan en el glosario §11 antes de integrarlos, como los nombres de la pendiente 1. De los sonidos de los desbloqueos de mecánica, 07 §9.2 dice que «su ID se fija al encargarlos»: los IDs se pueden fijar ya al enviar el encargo, pero el envío no los espera. Hoy el cliente no tiene audio.
3. **La plantilla de celdas y pivotes del rig de TiTino** (4.1) [P]: tiene que existir antes de enviar las piezas del rig. Ninguna tarea la produce (TI4 hace el rig provisional, no la plantilla para el artista): es una pregunta para el tech-lead.
4. **Dónde viven los máster con capas:** fuera de `Assets/_Project/Art/Generated/` (la misma pregunta que en el encargo del Mapa).
