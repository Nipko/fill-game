# Mecánicas del río y dificultad escalonada: propuesta para D-007

> **Documento de apoyo, no canon.** Todo es propuesta [P] hasta que el dueño decida. D-007 está reservado y **en pausa** (`docs/dev/decisiones.md`, D-008). Si algo de aquí contradice el GDD, manda el GDD hasta que se edite.
>
> Todo lo que toca fauna, flora, pueblos y lugares queda **pendiente de revisión del asesor cultural** (07 §8.2).
>
> Las fuentes [F#], [G#] y [L#] remiten a `investigacion.md` §9, en esta carpeta. Web consultada el 2026-10-08. Marcas: [V] verificado, [E] estimado, [NV] sin verificar, [P] propuesta.
>
> **Revisión del 2026-10-08.** Se corrigieron citas y marcas, nombres de hitos y lugares, el alcance de la 1.0 (Q8), el arreglo de fútiles y el orden de R10 (anexo B), y se añadieron Q17 y Q18.

**Cómo se hizo.**
- Se parte de la propuesta que más puntuaron los jueces, la del ángulo del jugador (34,7). Su principio: cada mecánica se entiende en un segundo y copia una función probada de Magic Sort (MS).
- Se le injertan las mejores ideas de la propuesta del ángulo del río (34,5):
  - el ciclo de 20 reordenado para que tras cada etiqueta venga un nivel más fácil;
  - la Lancha y la Cocha;
  - el filtro «nada de temporizadores a favor del jugador»;
  - las temporadas de aguas altas y bajas.
- Se añaden ideas de las tres líneas de investigación.
- Se aplican las correcciones de los jueces. El anexo A dice cuáles y cómo.

---

## (a) Resumen para el dueño en 10 puntos

1. **Copiamos la estructura de MS, no su piel.**
   - **Sí:** etiqueta antes de jugar, premio multiplicado a la vista, respiro después del pico, plantillas que rotan y vuelven, y novedad frecuente.
   - **No:** calaveras, otra distribución en cada reintento (D-008 #7) ni «?» en niveles normales.
2. **El escalón.** El ciclo de 20 pasa a tener los retos en p6, p9, p12, p15 y p18, un **remanso** fácil en p19 y el hito en p20.
   - Tras cada nivel con etiqueta, el siguiente siempre es más fácil.
   - Tras el hito vienen dos niveles de recuperación.
   - Las cuentas no cambian (12 comodidad, 5 reto, 2 recuperación, 1 hito), así que los porcentajes de Difícil y Muy difícil tampoco.
3. **La etiqueta se ve antes de jugar:**
   - en el botón «Jugar» del inicio, con un chip y una cinta «×3» o «×4»;
   - en el botón «Siguiente» de la victoria, que es el camino más usado (D-008);
   - en la tarjeta previa, con los iconos de las mecánicas del nivel;
   - en el HUD.

   Los premios no cambian: 20, 60 y 80 semillas. Pasar a «×5» (100) exige antes rehacer `econ_sim`.
4. **12 mecánicas de campaña.** Se quedan las 7 del canon y entran **5 nuevas**:
   - Matapi;
   - Aguapé;
   - Palizada;
   - Lancha;
   - Cocha.

   Cada una copia una función probada de MS. Hay además **9 variantes** para que aparezca algo nuevo hasta el nivel 1000.
5. **Estrenos 1-200:** Hoja 43 · **Matapi 63** · Liana con fruta 83 · Aguas turbias 100 · Remolino 103 · Barro 123 · Lluvia 143 · **Brote 163** · Piraña 183.
   - Cambia el contrato 1: Brote pasa del 63 al 163, el Matapi es nuevo y la Liana debuta con fruta.
   - Hay una opción mínima que no lo toca (pregunta Q5).
6. **Rotación por plantillas**, cada una con su ciclo de vida: estreno, práctica, secundaria, descanso y regreso. Los ciclos alternan aguas altas y aguas bajas.
   - Como mínimo, un 30 % de niveles sin mecánicas.
   - Al menos una «primera vez» por ciclo.
   - Ninguna mecánica pasa más de 120 niveles sin volver.
   - La tabla 21-200 está completa; para 201-1000 hay regla y calendario.
7. **Fuera de la campaña.** Todo lo que da algo **a favor** del jugador «cada k vertidos» (nido, defeso, piracema, revelado rítmico) se puede gastar con vertidos fútiles, que son legales. Va a Travesías y eventos, igual que la Cuia de tacacá, que cambia la regla de sellado.
8. **Dos arreglos técnicos al canon** antes de programar cualquier mecánica:
   - la regla de fútiles (02 §1.4), que hoy poda vertidos útiles hacia una Hoja vacía;
   - el orden de R10, que puede soltar la Liana un vertido tarde.
9. **Cultura.**
   - Cada mecánica sale de un objeto o un fenómeno cotidiano, con fuente.
   - La lista de vetos crece: Parintins, peconha, «caboclo», cacuri, caucho, minería, boto encantado y más.
   - Hay un Dato del río distinto por país.
   - Se propone encargar la Cuia de Santarém a ASARISAN.
   - Todo pasa por el asesor.
10. **Qué necesito de ti:** 18 preguntas cerradas, en (h). Las que más pesan: el ciclo (Q1), los debuts (Q5), la Liana con fruta (Q6), el alcance de la 1.0 (Q8), los arreglos técnicos (Q13), el contenido tapado (Q17) y los fútiles con contadores (Q18).

---

## (b) Catálogo de mecánicas

### b.0 Filtro que pasó cada idea

1. **Función probada.** Copia una función que MS (o el género) ya usa, con su evidencia: ficha [L1] o guías [G3-G5].
2. **Regla exacta y compatible** con:
   - el vertido por tramo (D-005, modo `run`);
   - la regla de no resaltar destinos (D-002);
   - el solver A*;
   - la generación por superposición y verificación (03 §9).
3. **Ningún contador a favor del jugador que avance con vertidos.** Un vertido fútil es legal (02 R13). Si cada vertido acerca un premio, el jugador puede ir y venir sin pensar. Solo valen los contadores **en contra**: Lluvia, Piraña y Cocha.
   - **Matiz:** la Lluvia no es solo un contador en contra. `U_agua` incluye las gotas pendientes (02 §9.1 M6), así que cualquier vertido, también uno fútil, acerca una gota que hace falta para ganar. Qué hacer con los vertidos fútiles mientras quedan gotas o la Piraña está activa es la pregunta Q18.
4. **Se lee en un segundo:** una silueta y, como mucho, un icono de fruta o un número. Tutorial de una pantalla, de 2 líneas o menos (02 §12 y §13).
5. **Cada desbloqueo tiene animación y sonido propios**, atados al golpe del corcho, como en MS (L2 §5.1).
6. **Nada sagrado y ninguna persona** (07 §8.1), con fuente y marca. Todo queda pendiente del asesor.
7. **El contenido se ve por defecto.** Tapar dejando ver solo el fondo es la opción C de la pregunta 5.4 (L2), que decide el dueño en Q17. Hasta entonces, ninguna regla de esta propuesta tapa contenido.

### b.1 Tabla resumen

La numeración M8-M12 es provisional (la fija 02 §9.1). M8 y M9 conservan la numeración de la propuesta §5.2: Aguapé y lo que allí era la Nasa.

| # | Mecánica (ES / PT) | Estado | Función de MS que copia | Regla en una línea | Coste solver | Debut [P] | Temporada |
|---|---|---|---|---|---|---|---|
| M1 | Hoja / Palha [P] | Canon, sin cambios | Botella atada y frascos cubiertos (MS 25-60 [V G3]) | Vacío reservado a una fruta; cae con su primer vertido | S | 43 (canon) | Todo el año |
| M10 | **Matapi** / Matapi | Nueva | Frasco «?» (magic bottle; MS ~70). Regla de MS sin confirmar: equivalencia [E G3, G5] | Nunca es origen: lo que entra no sale | S | **63** | Aguas bajas |
| M3 | Liana con fruta / Cipó com fruta | Canon, cambia la regla de debut | Tela con icono (MS 387 [V L1 §3.3]) | Atada; se suelta al sellar una cuia de su fruta | S | 83 (canon) | Aguas altas |
| — | Aguas turbias / Águas turvas | Canon | Capa «?» (MS 387-389 [V L1 §3.1]) | Dosis ocultas que se revelan al quedar arriba | M | 100 (canon) | — |
| M4 | Remolino / Rebojo [P] | Canon | Portales (MS: tema [V G5]; regla [E]) | Lo vertido entra por el fondo de la pareja | M | 103 (canon) | Todo el año |
| M5 | Barro / Barro | Canon | Slime y caja regalo (MS 400 [V G3]) | Dosis inerte que acepta cualquier fruta al quedar arriba | S | 123 (canon) | Todo el año |
| M6 | Lluvia / Chuva | Canon | Caja con temporizador (MS ~1000 [V G4]) | Cada k vertidos cae agua del mundo | M (×2-3) | 143 (canon) | Aguas altas |
| M2 | Brote / Broto | Canon, cambia el debut | Corcho por color (MS 221 [V G3]) | Cerrado no es origen; germina con su color | S | **163** | Todo el año |
| M7 | Piraña / Piranha | Canon | Temporizador enemigo | Cada k vertidos muerde la última cuia destino | M-L | 183 (canon) | Aguas bajas |
| M8 | **Aguapé** / Aguapé | Nueva | Hielo por fila (MS 388 [V L1 §3.4]) | Un grupo no es origen hasta que una de sus cuias sella | S (generador: M) | 223 | Aguas altas |
| M9 | **Palizada** / pendiente del asesor | Nueva (sustituye a la «Nasa») | Armario con candado y llave dentro del líquido (MS 389 [V L1 §3.5]) | Troncos sobre 2 o 4 cuias; se abren cuando la dosis-llave queda arriba | S-M | 263 | Aguas bajas |
| M11 | **Lancha** / Recreio [P, pendiente del asesor] | Nueva | Cortina de teatro que cae con N vacíos (MS 331 [V G3]) | La cuia de carga atraca cuando hay v cuias vacías | S (generador: M) | 303 | Aguas altas |
| M12 | **Cocha** / Lago [P] | Nueva | Temporizador como ventana que se cierra (MS ~1000 [V G4]) | En v vertidos deja de recibir; solo sirve de origen | M | 583 | Aguas bajas |

### b.2 Fichas de las mecánicas

#### M1 · Hoja (canon)

- **Nombre:** Hoja. En PT, **«Palha»** [P]. «Palma» evoca en Brasil la palma de la mano o un cactus forrajero [E], y «Folha» choca con «Folha-escudo» (glosario §6.3). Decide el asesor.
- **Función de MS:** la botella atada (387, 389) y los frascos cubiertos (25-60) [V L1 §3.2; G3]. Es un **vacío reservado**.
- **Regla exacta:** canon 02 §9.1 M1, sin cambios.
  - Cuia vacía tapada por una hoja con el icono de la fruta c. Solo acepta vertidos de c; en `run` entra el tramo de c que cabe.
  - Con el primer vertido de c, la hoja cae y la cuia queda normal.
  - Mientras tiene hoja no es origen.
  - **Arreglo:** verter en una Hoja nunca es fútil (anexo B).
- **Presentación:** una hoja de palma alargada, plegada sobre la boca, con la fruta grande en el centro. Su silueta debe distinguirse de la hoja redonda de la Hoja escudo. El icono es un estado, no un resaltado: no se ilumina al seleccionar (D-002). En los mundos 4-9, el aspecto puede ser una hoja de bijao atada con fibra [P].
- **Desbloqueo:** la hoja se abre y cae girando al agua (400-600 ms). Suena a hoja seca, en el golpe del corcho.
- **Tutorial (canon):** «Esta hoja solo deja pasar aguaje. Vierte aguaje para abrirla.»
- **Base cultural:**
  - En un caserío del distrito de Nauta (río Marañón), el 84 % de los techos son de crisneja, paños de hoja de palma tejida [V F128].
  - La hoja de bijao envuelve el maito y la patarashca: es un envoltorio de cocina sin carga ritual [V F113, F114].
- **Coste:**
  - solver S: 1 bit por Hoja, ×1-1,5 (03 §9);
  - generador: superposición sobre un vacío y verificación;
  - esquema: sin cambios.
- **Debut:** 43 (canon, contrato 1), en el Mundo 3 Manaos.

#### M10 · Matapi (nueva)

- **Nombre:** Matapi, invariante en ES y PT, como «cuia» [P]. No figura en la lista de 07 §8.1: lo decide el asesor.
- **Función de MS [E]:** el frasco «?» o *magic bottle* (seis a la vez en el nivel 70 [V G3]). Su regla no está clara en las fuentes:
  - una guía dice que revela un solo color cuando recibe un vertido que coincide, que después solo admite ese color y que un segundo color lo deja en una pila de dos colores que bloquea [G3];
  - la guía del nivel 70 y la ficha de la App Store lo describen como un frasco de contenido oculto [G3 nivel 70; G5].
  - Por eso la equivalencia es [E]. Hay que observar la regla en el dispositivo (L1) antes de decidir Q5. El Matapi vale como mecánica propia [P] aunque no copie al frasco «?»: obliga a comprometerse.
  - **Diferencia [E]:** si el frasco de MS es un vacío que fija su color, en MS puede entrar un segundo color y la botella queda bloqueada. En el Matapi no puede entrar otro color, pero tampoco puede salir nada.
- **Regla exacta** (`{"type":"matapi"}`):
  1. **Nunca es origen** (`bloqueada_origen`).
  2. **Como destino sigue la regla normal:** vacía acepta cualquier fruta; con contenido, solo la de su cima. En `run` entra el tramo que cabe.
  3. Empieza vacía o con 1-3 dosis visibles de un solo color.
  4. Para ganar tiene que acabar sellada o vacía (R12, sin cambios).
  5. Como destino **nunca es fútil** (anexo B).
  6. No puede ser pareja de un Remolino, cuia de Lluvia ni cueva de Piraña. No lleva Barro ni dosis ocultas.
  7. **Piraña:** si la última cuia destino es un Matapi, ese mordisco se pierde y el contador sigue.
  8. **Comodines:**
     - Marea: normal.
     - Pororoca: su contenido queda fijo.
     - Cuia extra y Morpho: normales.
- **Presentación:** un cilindro de varillas rodea la cuia, con un embudo hacia dentro en la boca. La forma ya dice «entra». Si se toca como origen, la cuia se sacude y el embudo late, sin texto (R11).
- **Desbloqueo** (en el sellado, ~800 ms): el embudo se cierra con un tirón de cuerda y la trampa sale del agua entre gotas y un brillo. Suena un crujido de fibra. No se muestra ninguna captura.
- **Tutorial:** «Lo que entra al matapi no sale. Llénalo con una sola fruta.» Debuta en 2-3 tableros fáciles con 2 vacíos.
- **Base cultural:**
  - Brasil (Bajo Amazonas, Santarém, Pará). Trampa semifija que hacen los propios pescadores, hoy de hierro galvanizado y nylon. Se cala en orillas, canales y lagos de várzea cerca de Santarém. El camarón abunda en la estación seca (junio-diciembre) [V F35]. Ninguna fuente lo sitúa en Manaos.
  - Forma de cilindro con embudos [E F35].
  - **No se presenta como práctica sostenible:** el estudio describe una pesca «desordenada», sin fiscalización, que captura individuos de todos los tamaños [V F35, corrección de los jueces].
  - Se dibuja un matapi genérico, sin copiar el de un taller concreto.
  - Nunca el cacuri [NV F38].
- **Coste:**
  - solver S: filtro de origen, sin estado;
  - generador S: el barajado inverso lo admite de forma nativa, porque hacia atrás el Matapi solo cede dosis;
  - esquema: tipo nuevo `matapi`, sin campos (contrato 21).
- **Debut:** **63** [P], en el Mundo 3 Manaos. Su base es del Bajo Amazonas (Santarém), en el mundo 2: el Dato del río del debut lo dice así, sin atribuirlo a Manaos.
  - En MS el frasco «?» llega hacia el 70 y los corchos en el 221 [V G3]. Por eso el Matapi va antes que el Brote, pero este argumento depende de una equivalencia [E] (arriba).
  - Opción mínima: 163 (Q5).
- **Riesgo:** el compromiso es irreversible y puede bloquear el nivel. Mitigación: validación con A*, la métrica de trampas tempranas y Marea.

#### M3 · Liana con fruta (canon; cambia la regla con la que debuta)

- **Nombre:** Liana. En PT, «Cipó» [P].
- **Función de MS:** la tela con icono (387): tapa una botella y se retira al completar una botella del color del icono [V L1 §3.3]. Es la forma más visible del gancho G1 de la ficha [L1 §12.1], «cada color completado destapa algo» (no confundir con la fuente [G1]).
- **Regla exacta** (`{"type":"liana","key":c}`):
  1. Cuia, vacía o con contenido visible, atada por una liana de la que cuelga la fruta c. **No es origen ni destino.**
  2. **Se suelta** en el paso (d) del R10 propuesto (anexo B), en cuanto existe al menos otra cuia sellada de c. No añade bits: una cuia sellada solo deja de estarlo con Marea, que entonces vuelve a atar la liana.
  3. Volver a la orilla la vuelve a atar.
  4. El validador exige que c pueda sellar fuera de la liana y que el nivel no empiece atascado.
  5. **El contenido se ve** (b.0.7). Tapar todo salvo la dosis del fondo, como la tela de MS (t = 0), es la opción C de la pregunta 5.4: se decide en Q17.
- **Variantes:** Liana de cuenta (la regla actual del canon, con n sellos) en el 243 y Flor de vitória-régia en el 403 (b.3).
- **Presentación:** 2-3 vueltas de liana verde con hojas pequeñas y una fruta grande de color c colgando del nudo, con su icono.
- **Desbloqueo** (600-900 ms):
  - aparece una ✓ sobre la fruta;
  - un hilo de luz baja de la cuia recién sellada a la fruta, como el haz de MS (t = 362);
  - la liana se seca, pasa de verde a pajizo, y cae.

  Suena un «ding» en el golpe del corcho y después una rama que cruje.
- **Tutorial:** «La liana se suelta cuando selles una cuia de taperebá.»
- **Base cultural:** las lianas son trepadoras leñosas que suben a la copa y hacen de puente para monos y perezosos [V F131]. Nada de lianas rituales ni medicinales (ayahuasca, 07 §8.1), ni el cipó-ambé [NV].
- **Coste:**
  - solver S: el disparador se deduce del estado; solo filtra origen y destino;
  - generador: superposición y verificación;
  - esquema: hay que cambiar la descripción de `key` («Tinte que abre la Hoja o tinte agua del mundo de la Lluvia») y la semántica de 02 §9.1 y 03 §9 (contrato 21), además de `tools/level-qa`.
- **Debut:** 83 (el puesto del canon), en el Mundo 4 Trapecio.

#### Aguas turbias (modificador del canon)

- **Regla:** canon 02 §11, T1-T7, sin cambios: como máximo 3 cuias y 4 dosis ocultas, con vertido seguro garantizado.
- **Cambios [P]:**
  1. **Debut en el hito 100 con la información oculta mínima.** El canon ya lo estrena en el hito 100 (Muy difícil) con 1 sola cuia turbia y 1 Luciérnaga regalada (04 §2.5; 03 §9): eso no cambia. Lo nuevo es solo esto:
     - las turbias son lo único especial del nivel, sin otra mecánica;
     - la cuia turbia tiene 2 dosis ocultas.

     Lo primero choca con 02 §10, que pone «2 mecánicas activas» en Muy difícil: es una excepción que decide el dueño (Q9). Así se resuelve también la contradicción de la propuesta ganadora, que lo estrenaba en un reto (anexo A).
  2. **En 101-200,** como mucho dos retos por ciclo y solo si el nivel mide Difícil o Muy difícil (T1).
  3. **Solo junto a mecánicas «filtro»** (Hoja, Brote, Matapi, Barro y capacidades). Nunca con disparadores (Liana, Aguapé, Palizada, Lancha) ni con contadores (Lluvia, Piraña, Cocha). Hay que fijarlo en 03 §8, que dice «nunca en niveles con mecánicas con disparador» sin definir el término (Q14).
- **Revelado:** el «?» se disuelve con 3-4 destellos en menos de 1,5 s, como MS en t = 238 (L2 §5.2 ya propone cambiar los 300 ms de 02 §4.1 y 07 §4.3). Además sube una burbuja de aire, como el pirarucú cuando sale a respirar: es solo animación.
- **Tutorial:** «El agua turbia esconde frutas. Debajo nunca se repite la fruta de encima.» (T4)
- **Base cultural:**
  - El agua blanca baja cargada de sedimento andino [V L6 §2].
  - La burbuja evoca la «boiada»: el conteo del pirarucú es un saber de pescadores certificados por el Instituto Mamirauá, a quienes se da crédito en el Dato del río [V F44, F45].
- **Coste:** M (×número de realidades, como mucho 24; 03 §8).

#### M4 · Remolino (canon)

- **Nombre PT:** «Rebojo» o «Redemoinho» [P].
- **Regla:** canon 02 §9.1 M4. Restricción nueva [P]: un Matapi nunca forma parte de un par.
- **Presentación:** espiral en la base de las dos cuias, unidas por un canal bajo el tablero. El chorro desaparece en una y sube con burbujas por el fondo de la otra (~500 ms), con un sonido de succión.
- **Base cultural:**
  - El rebojo «lleva las cosas al fondo». Se forma cuando la corriente baja y choca con rocas o arena. Es frecuente en el Juruá, el Purús, el Madeira y el Japurá [V F47].
  - El Japurá nace en Colombia como Caquetá [V F145]. Pero la fuente del rebojo habla del tramo brasileño y no dice nada del Caquetá, y el Trapecio está sobre el Amazonas (glosario §8). Unir las dos cosas es [E]: queda como Dato del río, no como base del debut.
  - Es raro en el río Negro [V F47].
  - Se explica por la física: nunca la Cobra Grande ni la Yacumama.
- **Variante:** Casiquiare (b.3).
- **Coste:** M (canon 03 §9).
- **Debut:** 103 (canon).

#### M5 · Barro (canon)

- **Regla:** canon 02 §9.1 M5. Nunca dentro de un Matapi.
- **Base cultural:** el sedimento del agua blanca, que fertiliza la várzea [V F13, F17]. Nunca la terra caída [V F137].
- **Desbloqueo:** el barro se enturbia y se disuelve en la fruta nueva (~400 ms), con un chapoteo.
- **Coste:** S.
- **Debut:** 123 (canon).

#### M6 · Lluvia (canon)

- **Regla:** canon 02 §9.1 M6. Como mucho un contador por nivel [P].
- **Base cultural:** el bosque fabrica parte de su lluvia (ríos voladores) [V F138]. Sirve para un Dato del río sobre deforestación y lluvia, sin catástrofes.
- **Coste:** M (×2-3).
- **Debut:** 143 (canon). Entra en la 1.0 solo si el solver con estado temporal pasa los tests (GDD §6 #6).

#### M2 · Brote (canon; cambia el debut)

- **Función de MS:** los corchos por color (221): una tapa con color-llave que solo se abre al verter ese color encima [V G3].
- **Regla:** canon 02 §9.1 M2, sin cambios.
- **Base cultural:**
  - La semilla del aguaje flota y la reparte el agua [V F133].
  - En el bosque inundado, los peces dispersan las semillas [V F18].
  - El agutí (añuje) abre el coco de la castaña y entierra semillas que germinan [V F93]. La fuente no nombra regiones peruanas, y la castaña de las fuentes es de Madre de Dios (F94) y de Riberalta (F95), no de Loreto. Para el debut en Loreto vale el aguaje (arriba).
- **Variante:** Brote de dos hojas (b.3).
- **Coste:** S (1 bit).
- **Debut:** **163** [P], en el Mundo 5 Loreto (canon: 63; Q5).

#### M7 · Piraña (canon)

- **Regla:** canon 02 §9.1 M7. Cambios [P]:
  - no muerde un Matapi;
  - como mucho un contador por nivel;
  - nunca junto a la Lluvia.
- **Presentación:** cardumen de tres pirañas pequeñas, sin dientes exagerados y sin sangre. Muerden la fruta de la cima y se la llevan a la cueva (~500 ms).
- **Base cultural:**
  - Las pirañas son omnívoras (comen frutas, semillas y hojas), andan en cardumen para defenderse y casi nunca atacan a personas [V F140, F141].
  - La gamitana come del 78 al 98 % de fruta en la creciente [V F90]: queda para el Dato del río, no como mecánica aparte.
- **Coste:** M-L (el par es aproximado; 03 §9).
- **Debut:** 183 (canon).

#### M8 · Aguapé (nueva)

- **Nombre:** Aguapé, igual en ES y PT. En el Dato del río se usa el nombre local de cada país (lechuguín, camalote, tarope…), que está sin verificar [NV F110].
- **Función de MS:** el hielo por fila (388). Envuelve la base de una fila; sus botellas reciben vertidos, y al completar una de ellas revienta el bloque entero [V L1 §3.4].
- **Regla exacta** (`{"type":"aguape","group":g}` [P]):
  1. Una mata cubre la base de un grupo g de 2-5 cuias con **índices consecutivos** en los datos del nivel. El estado no necesita la posición.
     - **Regla de layout nueva** (02 §5 y D-006, punto 4): un grupo nunca se parte entre filas. Con D-006 la fila depende del teléfono (con 11-14 cuias, 2 o 3 filas según la pantalla). Si el reparto partiera el grupo, el nivel fija sus filas en los datos.
     - Va a `BoardLayoutTests`. Hay que revisar el dibujo de la mata cuando la Cuia extra o la de rescate cambian el ancho de la fila.
  2. Las cuias del grupo son destino con la regla normal, pero **no son origen**. [P]: MS no mostró si se puede levantar una botella congelada (L1 §3.4).
  3. La mata se retira de todo el grupo en el paso (d) del R10 propuesto, en cuanto una cuia del grupo queda sellada. Se deduce del estado, y Marea la repone si deshace ese sellado.
  4. **Validador:**
     - al menos una cuia del grupo puede sellar solo recibiendo;
     - el nivel no empieza atascado;
     - no hay Palizada en la misma fila (d.3: solo con filas fijadas en los datos).

     Una Cuia extra o de rescate que cae en esa fila es normal.
  5. **Pororoca:** las cuias bajo la mata quedan fijas.
- **Presentación:** alfombra de hojas redondas brillantes, con flores lavanda de mancha amarilla, hasta media altura de las cuias. El contenido se ve.
- **Desbloqueo** (~700 ms): la mata se parte en trozos que se alejan flotando entre pétalos. Suena a agua que se abre, en el golpe del corcho, como el estallido del hielo de MS a +1,12 s [L1 §3.4].
  - Opción: asoma un manatí y se come la mata. El manatí no está en el elenco: lo decide el dueño (Q12).
- **Tutorial:** «El aguapé sujeta estas cuias: puedes verter en ellas, pero no sacar. Sella una y se suelta.»
- **Base cultural:**
  - Es nativo de la cuenca. Flota gracias a pecíolos con aire y forma alfombras que tapan el agua [V F109, F110, F111].
  - El manatí lo come [V F142, F143].
  - En su área no se presenta como plaga.
  - Es mejor que un «hielo»: en la Amazonía no hay hielo.
- **Coste:**
  - solver S: id de grupo de 2-3 bits en los bits libres de `Cuia.Packed` (`Cuia.cs`) y filtro de origen;
  - generador M: superposición y verificación;
  - esquema: tipo nuevo y campo `group`, que suben `pack_schema`.
- **Debut:** 223, en el Mundo 6 Oriente.

#### M9 · Palizada (nueva; sustituye a la «Nasa» de L2 §5.2)

- **Nombre:** Palizada en ES, como **regionalismo amazónico [E]**.
  - Uso regional atestiguado: Soini la usa una vez, sin definirla («soleándose sobre las palizadas u orilla del río») [V F14].
  - El DLE no recoge el sentido de troncos acumulados en el río. Recoge «sitio cercado de estacas» y una defensa de estacas para dirigir los ríos [V F156]. Un jugador general puede leerla como «empalizada» o asociarla a «paliza».
  - Pasa al asesor y a una prueba de lectura con jugadores de varios países. Nombre alternativo: «Troncos».
  - En PT queda pendiente del asesor: «paliteiro» connota peligro (troncos que perforan cascos [V F135]), y «toras» está sin verificar.
- **Función de MS:** el armario con candado y la llave dentro del líquido (389). Tapa 2 o 4 botellas; cuando la dosis con la llave queda arriba, la llave vuela al candado y el armario se abre [V L1 §3.5]. La variante en cadena copia la caja fuerte del 442 [V G3].
- **Regla exacta** (`{"type":"palizada","group":g,"key":c}` en 2 o 4 cuias con índices consecutivos, con la misma regla de layout que el Aguapé, más un campo nuevo del nivel, `keys: [[cuia, capa, g]]`) [P]:
  1. Mientras está cerrada, sus cuias **no son origen ni destino**. El contenido se ve (b.0.7; Q17).
  2. La **dosis-llave** es una dosis normal de color c en otra cuia, fuera de esa palizada.
     - Nunca está en la cima al empezar y nunca es una dosis oculta.
     - **La dosis inmediatamente superior no es de color c.** Si lo fuera, con el vertido por tramo (D-005; 02 R9) la llave saldría dentro del tramo sin haber estado en la cima, y la palizada no se abriría. Con esta condición, la llave siempre pasa por la cima antes de poder salir.
     - **Nunca está en una cuia de Remolino:** lo que entra por el fondo de la pareja desplaza todas las capas (02 §9.1 M4) y la marca apuntaría a otra dosis.
     - Su marca va en los bits libres de la cuia: 3 bits para la capa y 2-3 para el grupo g, que hace falta para saber qué palizada abre cada llave (Palizada en cadena).
  3. En el paso (f) del R10 propuesto, si la dosis-llave queda en la cima de su cuia, la palizada se abre para el resto del intento. Ocurre cuando se vierte desde esa cuia o tras un mordisco de Piraña.
     - Coste: 1 bit por grupo, que Marea restaura.
     - La marca se consume y la dosis pasa a ser normal.
  4. **Validador:** la llave es alcanzable (A*), no hay ciclos en las cadenas, se cumplen las condiciones de la regla 2 y el número de trampas tempranas está dentro del tope.
  5. **Tests de propiedad:** llave bajo una dosis de su color (rechazo del generador), llave en una cuia de Remolino (rechazo), llave que llega a la cima tras un mordisco, y dos palizadas con llaves de grupos distintos.
- **Presentación:**
  - 3-4 troncos claros en horizontal, con huecos, atados con una cuerda;
  - un **nudo grande con el icono de la fruta c**, que se lee con Patrones de fruta;
  - la llave es una **silueta blanca de pez**, sin especie, sobre la banda de la dosis-llave.

  Así no hace falta leer «un pez de color» en una banda de 22-28 dp (anexo A).
- **Desbloqueo** (1,2-1,5 s): el pez salta en arco desde la boca de su cuia y toca el nudo; los troncos se separan y se van río abajo. Suenan un chapoteo y un crujido de madera, como el UNLOCK + WOOD_BREAK de MS pero más cortos [L1 §3.5].
  - Opción: la llave es Rosa (el boto juega con troncos [V F144]). Es un papel nuevo para la mascota: lo decide el dueño (Q12).
- **Tutorial:** «Cuando el pez llegue arriba, saltará y soltará los troncos.»
- **Base cultural:**
  - En Pacaya-Samiria las taricayas se asolean «sobre las palizadas» [V F14].
  - En la vaciante del Madeira bajan troncos por el río [V F135].
  - En la piracema los peces suben el río contra la corriente [V F43]. Que el pez-llave salte es una decisión de arte [P], sin base citada.
  - No usa artes de pesca de comunidades concretas.
- **Coste:**
  - solver S-M: 1 bit por grupo y la marca de la llave (capa y grupo);
  - generador M: elegir el grupo y la dosis-llave, verificar, y rechazar si la llave queda inalcanzable o incumple la regla 2;
  - esquema: tipo nuevo y campo `keys` (sube `pack_schema`; `tools/level-qa`).
- **Debut:** 263, en el Mundo 6 Oriente. Ojo: la Palizada no tiene base ecuatoriana. Sus bases son de Pacaya-Samiria (Perú) [F14] y del Madeira (Brasil) [F135]. Hay que buscar una base en Ecuador o mover el debut (lo deciden el asesor y el dueño).
- **Riesgo:** si la llave queda enterrada, el nivel se bloquea, como en MS (t = 888). Mitigación: A*, el tope de trampas tempranas y Marea.

#### M11 · Lancha (nueva)

- **Nombre:** Lancha en ES. Nunca «canoa» (las vidas) ni «balsa» (la Balsa de rescate).
  - En PT, «Recreio» [P]. En la Amazonía es un barco de pasajeros y carga, pero la misma fuente dice que ese uso es regional [V F33]. Fuera del Norte, «recreio» se lee sobre todo como el recreo escolar [E].
  - Pasa al asesor y a una prueba con jugadores brasileños. Alternativas PT: «Barco» o «Lancha».
- **Función de MS:** la cortina de teatro (331), que oculta frascos extra y cae sola cuando hay N vacíos [V G3]. Premia consolidar y trae contenido a mitad de nivel, sin azar.
- **Regla exacta** (`{"type":"lancha","k":v}` en la cuia de carga, que ocupa una ranura y cuenta entre las 14 cuias como máximo):
  1. Antes de atracar, la carga no es origen ni destino.
  2. En el paso (f) del R10 propuesto, si el tablero tiene **v o más cuias vacías**, la lancha atraca y la carga pasa a ser una cuia normal. Cuentan también la Cuia extra y la de rescate.
     - Coste: 1 bit, que Marea restaura.
  3. Valores: v ≥ vacíos iniciales + 1.
     - La carga trae 1-3 dosis visibles, que cuentan en `U_c` desde el inicio.
     - En los niveles de recuperación puede llegar vacía.
     - Una sola lancha por nivel.
  4. **No se puede explotar:** el número de cuias vacías solo sube consolidando. Un vertido fútil no lo cambia.
  5. Nunca entra en Río sin fin.
- **Presentación:**
  - un muelle de madera en la ranura;
  - al fondo, una lancha de dos o tres cubiertas con hamacas, con una banderola «v» y un icono de cuia vacía;
  - cada nueva cuia vacía acerca la lancha un tramo, con un toque de bocina.
- **Desbloqueo** (~1,4 s): la lancha entra deslizándose, como el tablero de MS (t = 9), atraca, y una cuerda deja la cuia en el muelle.
- **Tutorial:** «La lancha atraca cuando tengas 3 cuias vacías: trae fruta para terminar.»
- **Base cultural:**
  - A Iquitos solo se llega por río o por aire [V F80].
  - El recreio lleva pasajeros y carga [V F33].
  - La lancha de Yurimaguas a Iquitos tiene tres cubiertas [E F74].
  - En Belén los botes hacen de bus escolar en la creciente [V F79].
  - Puerto Maldonado construye botes [V F96].
  - Sin personas en primer plano.
- **Coste:**
  - solver S: 1 bit y un recuento de vacías;
  - generador M: se saca una cuia del tablero base, se sube a la lancha y se verifica;
  - esquema: tipo nuevo.
- **Debut:** 303, en el Mundo 7 Madre de Dios (astilleros).

#### M12 · Cocha (nueva)

- **Nombre:** Cocha en ES y «Lago» en PT [P]. «Cocha» es quechua de uso común en la selva baja [E F88]: lo revisa el asesor.
- **Función de MS:** el temporizador (~1000 [V G4, una sola guía]), convertido en una ventana que se cierra: «úsala o piérdela». Es el espejo del Matapi: cuando se cierra, de la cocha solo sale fruta.
- **Regla exacta** (`{"type":"cocha","k":v}`, con v entre 6 y 12):
  1. Mientras v > 0, es una cuia normal.
  2. Cada vertido del jugador resta 1, en el paso (e) del R10 propuesto; Marea lo devuelve.
  3. Con v = 0, el caño se seca: ya no es destino (`bloqueada_destino`), sigue siendo origen y tiene que acabar sellada o vacía (R12).
  4. **No se puede explotar:** gastar vertidos solo la cierra antes.
  5. Como mucho un contador por nivel; nunca con Aguas turbias; Pororoca deshabilitada.
- **Presentación:** una cuia unida al borde del tablero por un caño de agua, con un contador de gotas. Cuando quedan 3, el caño se estrecha y cambia de tono.
- **Desbloqueo** (el cierre, ~600 ms): el caño se seca y deja arena, con un sonido de agua que se corta.
- **Tutorial:** «El caño de la cocha se seca en 8 vertidos: después solo podrás sacar fruta.»
- **Base cultural:**
  - Yarinacocha es un antiguo meandro del Ucayali que se une al río por sus extremos en la temporada de lluvias [V F147].
  - En la vaciante del medio Solimões (Brasil), el manatí sale de los lagos antes de que se cierren sus cuellos de botella [V F148]. Es una base brasileña para un debut en el Perú: el Dato del río dice el lugar real y va en un mundo brasileño (sección e).
  - Sin peces muertos.
- **Coste:** solver M (el contador entra en la clave canónica hasta llegar a 0) y generador M.
- **Debut:** 583, en el Mundo 9 Ucayali.

### b.3 Variantes: novedad sin regla nueva, o con un giro pequeño

| Variante | Base | Qué cambia (regla exacta) | Función de MS | Coste | Debut [P] | Presentación y desbloqueo |
|---|---|---|---|---|---|---|
| **Liana de cuenta** | Liana | Se suelta al sellar n cuias (1-2) en el intento: la regla actual del canon | Huecos con candado que se liberan a mitad de nivel (221) [V G3] | S | 243 | Nudo con el número; se seca y cae |
| **Hoja atada** | Hoja | Filtro de color **persistente**: solo recibe su fruta c (vacía o con c en la cima), **sí es origen** y nunca se suelta. Puede empezar con dosis cuyo fondo es c | La botella atada de MS tal como es: puede verter (t = 776), conserva la cuerda tras sellar y en el 389 empieza mezclada con el fondo del color de la cuerda [V L1 §3.2]. Que solo acepte su color está deducido [E] | S (estático) | 283 | Hoja pequeña atada al cuello con fibra, con icono grande; nunca cae. El nombre puede confundirse con la Hoja: lo decide el asesor |
| **Brote de dos hojas** | Brote | Germina al recibir **2 dosis** de su color, en uno o varios vertidos. En `run`, un tramo de 2 lo abre de una vez (D-005; 02 R9). El validador exige espacio ≥ 2: empieza con 2 dosis como mucho. Seguro, porque cerrado no es origen y el color que entra no puede salir | Corcho con número (221) [V G3] | S-M (1-2 bits) | 343 | Brote con dos hojas cerradas; cada dosis abre una |
| **Flor de vitória-régia** | Liana con fruta | Dos llaves, c1 y c2, en lógica Y: se abre cuando existen cuias selladas de las dos frutas. Con una sola, la flor está blanca y entreabierta; con las dos, rosa y abierta. Se deduce del estado, sin bits | Cadenas de desbloqueo (442) [V G3] | S | 403 | Capullo blanco → rosa → abierto. **Solo la flor**: la hoja redonda es la Hoja escudo. Base: blanca el primer día y rosa rojiza el segundo [V F112]. Sin la leyenda de Naiá ni «Atun Sisac» |
| **Palizada en cadena** | Palizada | La dosis-llave está dentro de otra palizada, sin ciclos | Caja fuerte (442) [V G3] | M | 443 | Se abre una y después la otra |
| **Llave en la lancha** | Palizada + Lancha | La dosis-llave viaja en la carga de la lancha. **No hay regla nueva:** la carga es una cuia más (Lancha, regla 2) y la llave sigue las reglas de la Palizada. Por eso no es un debut y puede ir en un hito (c.3, regla 6) | Combinación | M | 520 (hito; combinación sin regla nueva) | La lancha atraca y el pez salta desde ella |
| **Casiquiare** | Remolino | Par de un solo sentido (`oneway`): solo A actúa como remolino; como destino, B es una cuia normal | Portales | S sobre el Remolino | 703 | Flecha de corriente de A hacia B. Base: el Casiquiare une el Orinoco con el Negro [V F146] |
| **Lancha de vuelta** | Lancha | Tras dejar la carga, vuelve con otra cuando hay v' vacías (2 bits) | Cortina | S | 763 | La lancha se va y regresa |
| **Aguapé que crece** | Aguapé | Cada k vertidos sin sellado en su grupo, cubre la siguiente cuia de una **lista fija en los datos del nivel**. Así no hace falta la posición en el estado (investigacion §6.5, hallazgo 3). Es un contador en contra: seguro | Hielo con temporizador | M | 883 | Las hojas se extienden a la cuia vecina |

### b.4 Ideas que van a Travesías y eventos, no a la campaña

| Idea | Qué haría | Por qué no va a la campaña | Dónde puede ir |
|---|---|---|---|
| Nido de tortuga (no «de charapa»: en el Perú, «charapa» también nombra a la gente del oriente [V F155]) | Cuia que se libera a los k vertidos | Contador a favor: se gasta con vertidos fútiles; además duplica la Liana de cuenta | Travesía con la regla «solo cuentan los sellos». Datos del río de conservación [V F14] |
| Defeso | Cuia vedada k vertidos | Mismo problema. El pescador vive de esa pesca: el nombre no sirve para un bloqueo | Evento de conservación, con el asesor [V F41] |
| Piracema | Un pez sube un puesto cada k vertidos | Ambigua y posicional; término brasileño | Travesía en mundos brasileños [V F43] |
| Revelado rítmico «boiada» | Una dosis oculta se revela cada k vertidos | Contador a favor; cambia T1-T6 | Solo la animación de burbuja (Aguas turbias) |
| Tipiti | Al sellarse, la cuia-prensa suelta una dosis en la vecina | El tipiti prensa mandioca, no fruta [V F56]. Tecnología indígena: necesita al asesor | Evento «Casa de farinha» en el mundo 1 [V F54, F55] |
| Cuia de tacacá | Capas en un orden fijo | Cambia la regla de sellado (R5) | Evento «Hora del tacacá» [V F57, F58] |
| Banzeiro | Una ola desplaza bloqueos una ranura | Posicional (coste L); cercano a la terra caída | No |
| Dos aguas | Cuia ancha de dos carriles de color fijo | Layout nuevo y otro predicado de victoria. «Cuia del Encuentro» ya es una skin y «Encuentro de las Aguas», un evento | Mecánica del evento Encuentro de las Aguas (1.2), con otro nombre [P] |
| Mijano | Una cocha suelta peces a otras cuias | Regionalismo peruano; exige explicación | Evento de aguas bajas en Loreto [V F86] |
| Fiesta de la fruta | Tableros con mucha dosis de un tinte | No es mecánica | Evento mensual por tinte, sin nombres oficiales [V F67] |
| Mareas de equinoccio | Pororoca más barata en marzo y septiembre | No es mecánica | Evento [V F21-F23] |

### b.5 Descartes

- **Nasa** (L2 §5.2): sustituida por la Palizada, que tiene una base verificada (Soini, IIAP) y no usa un arte de pesca concreto.
- **Tronco hueco:** sin base.
- **Tarrafa** como hielo: el aguapé se lee mejor. La tarrafa queda como animación posible de la Pororoca.
- **Curral:** es más costero que ribereño.
- **Cacuri:** asociado a un lugar sagrado.
- **Friagem** como «congelar»: el fenómeno es real [V F26], pero es poco conocido y el aguapé es más legible.
- **Gamitana** como mecánica aparte: es la Piraña con otra piel; queda para el Dato del río.
- **«Regla del río»** en la interfaz: una cuarta señal de dificultad que choca con la barra de la Crecida, donde el agua alta es buena.
- **«Repiquete»** como subtítulo visible: regionalismo; queda como nombre interno de los retos.
- **«Llave entre aguas turbias»** (como en el 389 de MS): prohibida por 03 §8 salvo que el dueño lo cambie (Q14).
- **Maromba y flutuante** como mecánicas: sin función de MS; quedan como fondo.

---

## (c) Dificultad escalonada

### c.1 Etiquetas antes de jugar

La palabra funcional siempre se ve. La decide el bot ×40, no la posición en el ciclo (02 §10).

| Dónde | Normal | Difícil | Muy difícil |
|---|---|---|---|
| Botón «Jugar · Nivel n» del inicio (D-008 #6) | Botón verde, sin chip | Chip ámbar con la rana dardo y la cinta **«×3»** | Chip índigo con el jaguar, la cinta **«×4»** y el botón en variante nocturna |
| Botón «Siguiente» de la victoria (D-008 #3; 02 §6) | Sin chip | Chip ámbar y cinta **«×3»** del nivel siguiente | Chip índigo y cinta **«×4»** del nivel siguiente |
| Tarjeta previa de 1 toque | Ninguna; en un debut, el tutorial hace de tarjeta | Rana, «Difícil», subtítulo «Aguas bravas», «Premio: 60» e **iconos de las mecánicas del nivel** [P] | Jaguar, «Muy difícil», «Noche de jaguar», «Premio: 80» e iconos de las mecánicas [P] |
| HUD | — | Chip y marco ámbar | Chip y marco índigo, fondo de noche |
| «¿Seguro?» y «Nivel fallido» (D-008) | Cabecera normal | Cabecera ámbar | Cabecera índigo |

- **Por qué también en «Siguiente».** Con D-008, tras ganar se pulsa «Siguiente» y se pasa directo al nivel N+1, sin ver el botón del inicio. Magic Sort pone la etiqueta y el «x5» en el botón con el que se arranca el siguiente nivel [L1 §3.6]. La tarjeta previa avisa igual, pero el «×3»/«×4» se anticipa en la victoria.
- **Sin calavera.**
- **Subtítulos:** se mantienen los del canon, «Aguas bravas» y «Noche de jaguar». «Repiquete» queda como nombre interno (Q4).
- Los subtítulos se apagan con `ui_show_difficulty_subtitles`.

### c.2 Premio multiplicado

- **Cifras del canon:** 20, 60 y 80 semillas (`eco_reward_*`, 08 §9).
- **Cómo se muestran:** como **«×3»** y **«×4»** sobre las 20 de un nivel normal, como la cinta «x5» de MS (100 frente a 20) [L1 §3.6].
- **Opción ×5** (Muy difícil a 100): cambia las cifras [sim]. Solo después de rehacer `tools/econ_sim.py` y `tools/out/econ_*.md` (contrato 18). Recomendación: no por ahora (Q3).
- **Coherencia:** un reto que mide menos del 75 % se publica sin chip y sin «×3».

### c.3 El ciclo de 20 reordenado

| p | Rol | Nombre interno [P] | Vacíos | Fuente | Ventana de bot (21-40 / 41-60 / 61+) | Etiqueta posible | Contenido |
|---|---|---|---|---|---|---|---|
| 1-2 | Recuperación | Vaciante | 2 | `walk`, K−1 | < 30 % | — | Puro |
| 3-5 | Comodidad o estreno | Aguas bajas | 1 (2 hasta el 30 y en debuts) | `walk` | 20-60 % (< 20 % en debuts) | — | Debut, o 0-1 mecánicas |
| **6** | Reto | Repiquete | 1 (2 en 21-39) | `fill`/`deep` | 50-70 / 60-80 / ≥ 75 | Difícil desde el 41 | 1-2 mecánicas |
| 7-8 | Comodidad | Bajada | 1 | `walk` | 20-60 % | — | 0-1 mecánicas |
| **9** | Reto | Repiquete | ídem | ídem | ídem | Difícil | 1-2 mecánicas |
| 10-11 | Comodidad | Bajada | 1 | `walk` | 20-60 % | — | 0-1 mecánicas |
| **12** | Reto | Repiquete | ídem | ídem | ídem | Difícil | 1-2 mecánicas |
| 13-14 | Comodidad | Bajada | 1 | `walk` | 20-60 % | — | 0-1 mecánicas |
| **15** | Reto | Repiquete | ídem | ídem | ídem | Difícil | 1-2 mecánicas |
| 16-17 | Comodidad | Bajada | 1 | `walk` | 20-60 % | — | 0-1 mecánicas |
| **18** | Reto | Repiquete | ídem | ídem | ídem | Difícil | 1-2 mecánicas |
| **19** | Remanso | Remanso | 1 (2 hasta el 30) | `walk` | 20-40 % | — | **Puro** |
| **20** | Hito | Pico | 1 | `deep`, K máximo | 75-85 / ≥ 90 / ≥ 90 | Difícil (40) · Muy difícil (60+) | 2 mecánicas (3 desde el 320) |

**Cuenta:** 12 de comodidad (p3-5, 7-8, 10-11, 13-14, 16-17 y 19), 5 retos, 2 de recuperación y 1 hito. Es la misma cuenta que hoy, así que los porcentajes de etiquetas de 03 §11 no cambian.

**Reglas del escalón:**
1. **Nunca hay dos niveles con etiqueta seguidos.** Entre dos retos hay siempre dos niveles, y p19 no lleva etiqueta.
2. **Tras cada nivel con etiqueta, comodidad.** El índice D baja al menos una banda, dentro del −3,4 de 03 §12.
3. **Tras el hito, dos niveles de recuperación** (canon).
4. **Los retos suben dentro del ciclo**, con dos escaleras:
   - desde el 61: D(p6) ≤ D(p9) ≤ D(p12) ≤ D(p15) ≤ D(p18) < D(p20);
   - en 41-59 solo cuentan los retos con etiqueta (regla 7): D(p6) ≤ D(p12) ≤ D(p18) < D(p20). Los retos suaves p9 y p15 quedan por debajo del reto etiquetado anterior.

   Es una escalera que el jugador nota. La etiqueta la decide la medición, no el slot (03 §12).
5. **Pico anunciado tras un respiro**, como el 387 → 388 → 389 de MS [L1 §3.6; L2 §5.5].
6. **Nada debuta en un reto ni en un hito.** Única excepción: Aguas turbias en el hito 100 (canon: 04 §2.5; 03 §9), con la información oculta mínima. Una combinación sin regla nueva (como la Llave en la lancha, 520) no es un debut.
7. **En 41-59, retos con etiqueta en p6, p12 y p18** (3 de 5); p9 y p15 son suaves. El 40 es el primer Difícil y el 60, el primer Muy difícil (contrato 6, sin cambios).

### c.4 Plantillas por posición

| Posición | Plantilla |
|---|---|
| p1-p2 | Puras: 2 vacíos, K−1 |
| p3-p5 | En un ciclo de debut: la mecánica nueva sola, en 2-3 tableros, con 2 vacíos y un solo ejemplar (02 §9.2, regla 2). En los demás ciclos: comodidad con la titular del ciclo |
| p7-p8, p10-p11, p13-p14, p16-p17 | 0-1 mecánicas; **al menos 3 de estos 8 niveles, puros** |
| Retos | Según el ciclo de vida de d.1: en su ciclo de debut, la titular va sola en p9 y en pareja en 2-3 retos más. Los demás retos llevan la secundaria (C+1) u otras mecánicas de la bolsa. Las parejas cumplen las prohibiciones de d.3 |
| p19 | Puro, más fácil que la media de comodidad |
| p20 | Mecánicas con al menos 15 niveles desde su debut y que ya salieron en un reto. Al final de un mundo, su **firma** (sección e) |

### c.5 El pulso del río, a la vista

- **La señal es la luz, no la altura del agua.** En el canon, el agua alta es la Crecida, la racha de victorias, que es buena (glosario §6.3). Subir el agua en los niveles difíciles diría lo contrario: es el mismo choque por el que se descartó la «Regla del río» (b.5).
- El Muy difícil ya lleva la variante noche de la pintura del mundo (07 §7.7) y el fondo un 15 % más oscuro (02 §10). El Difícil puede llevar una luz de tarde [P]. En la recuperación vuelve la luz normal.
- **Sigue a la etiqueta medida, no a la posición.**
- Sin barra y sin texto: la barra vertical ya es la Crecida.
- La LUT de crecida o de vaciante (07 §2) sigue la temporada del ciclo (d.1).
- «El río bajó» queda reservado al alivio tras 3 fallos (03 §13).

### c.6 Reintento y alivio

Sin cambios respecto al canon:
- el mismo tablero hasta el tercer fallo (D-008 #7);
- después, «El río bajó» con `relief[0]`;
- Salto del Boto tras 5 fallos.

No se copian las distribuciones distintas por intento de MS.

### c.7 Medición

Además de los KPI de 03 §15:
- **Intentos medios por mecánica en su ciclo de debut.** Alarma si superan 2,5 en un nivel Normal. Si salta, no se aplaza el estreno siguiente: un paquete nuevo nunca cambia un nivel ya publicado (08 §4.4). Se sustituyen los slots afectados con `level_overrides`.
- **Trampas tempranas:** jugadas iniciales que llevan a Aguas estancadas en 2 vertidos o menos. Tope de 0 en Normal y de 1 en Difícil (L2 §5.5, opción B).
- **Acierto de etiqueta:** porcentaje de retos que alcanzan el 75 %. Si en un ciclo baja del 60 %, se revisa el generador de esa posición.

### c.8 Cómo cambia 03 §12

| Aspecto | Hoy (03 §12) | Propuesta |
|---|---|---|
| Posiciones de reto | p6, p10, p14, p17, p19 | p6, p9, p12, p15, p18 |
| p19 | Reto | Remanso puro (bot 20-40 %) |
| Regla de 03 §11 «el hito siempre va precedido de un reto» | Sí | «El hito va precedido de un remanso, y este de un reto» |
| Retos suaves de 21-39 | 26 y 30 (y 34, 37, 39 sin etiqueta) | 26, 29, 32, 35, 38 |
| Etiquetados en 41-59 | p6, p14, p19 (`econ_sim.py`) | p6, p12, p18 |
| `CHALLENGE_POSITIONS` en `tools/econ_sim.py` | {6, 10, 14, 17, 19} | {6, 9, 12, 15, 18}. Hay que regenerar `tools/out/econ_*.md` (contrato 18) aunque la cuenta no cambie |
| Texto de 02 §9 «nunca en reto (p6, p10, p14, p17, p19)» | — | Actualizar a las posiciones nuevas |
| Debuts en p3 | Sí | Sí (sin cambios) |

---

## (d) Rotación de plantillas

### d.1 Conceptos

- **Plantilla:** una mecánica titular, una secundaria opcional y una forma de tablero (1-3 filas, Cuia chica o Cuia alta).
- **Temporada del ciclo:** cada ciclo de 20 es de **aguas altas (A)** o de **aguas bajas (B)**.
  - **A:** Lluvia, Aguapé, Lancha, Liana (con fruta y de cuenta) y Flor.
  - **B:** Matapi, Palizada, Piraña y Cocha.
  - **Todo el año:** Hoja, Hoja atada, Brote, Remolino, Casiquiare y Barro.
  - Base: crecida y vaciante [V F6, F7]; calendario de Pacaya-Samiria [V F14]; el camarón del matapi abunda en la estación seca [V F35]; los troncos aparecen en la vaciante [V F135].
  - La temporada solo cambia el peso de cada mecánica en la bolsa (×2 las de su familia, ×1 las de todo el año, ×0,5 las de la otra familia) y la LUT del fondo. No sigue el calendario real ni muestra texto (Q11).
- **Ciclo de vida de una mecánica:**
  - **C0 (debut):** titular. Aparece en p3-p5 (estreno), en p7 (práctica), sola en un reto (p9) y en dos o tres retos más; como mucho, **8 niveles en el ciclo**, contando los de estreno.
  - **C+1:** secundaria de la siguiente titular, en 2-3 retos y quizá en el hito.
  - **C+2:** descansa o aparece una vez en comodidad.
  - **C+3 o C+4:** vuelve en una combinación nueva.
  - **Después:** entra en la bolsa general. Su peso crece con el tiempo que lleva sin salir.

### d.2 Reglas de la bolsa

1. **Al menos un 30 % de niveles puros por ciclo:** p1, p2, p19 y al menos 3 de comodidad.
2. **Topes:**
   - una mecánica sale en 6 niveles por ciclo como mucho (8 en su ciclo de debut);
   - nunca en 3 niveles seguidos fuera de su estreno.
3. **Al menos una «primera vez» por ciclo:** un debut, una variante, una firma, la primera combinación de dos mecánicas o una forma de tablero nueva. El generador guarda un **registro de combinaciones publicadas**. Ese registro es una entrada versionada del pipeline, para que el test de reproducibilidad por hash siga valiendo.
4. **Regreso:** toda mecánica de la bolsa sale al menos una vez cada 3 ciclos de su temporada (120 niveles o menos); las de todo el año, cada 2 ciclos.
5. **Mecánicas por nivel:** como mucho 2 antes del 300 y 3 después (02 §9.2, regla 4). Recomendación: 3 solo en retos e hitos.
6. **Contadores:** como mucho uno por nivel (Lluvia, Piraña, Cocha o Aguapé que crece).
7. **Temporadas:**
   - se alternan, y nunca hay más de 2 ciclos seguidos de la misma;
   - el ciclo del debut de una **mecánica** toma la temporada de esa mecánica;
   - las variantes pueden estrenarse en cualquier temporada.
8. **Kill-switch:** con `mech_enabled.<id>` en false, el nivel se sustituye por el respaldo del mismo slot (02 §9.2, regla 3).

### d.3 Combinaciones prohibidas

- **Aguas turbias** con contadores (Lluvia, Piraña, Cocha, Aguapé que crece) o con disparadores (Liana, Flor, Aguapé, Palizada, Lancha). Pendiente de definir «disparador» en 03 §8 (Q14).
- **Dos contadores** en el mismo nivel.
- **Matapi** dentro de un par de Remolino, como cuia de Lluvia o como cueva de Piraña; con Barro o con dosis ocultas dentro.
- **Palizada y Aguapé** en el mismo nivel, salvo que el nivel fije sus filas en los datos; con filas fijas, nunca en la misma fila. Con D-006 la fila depende del teléfono y «la misma fila» no tendría un valor fijo.
- **Dosis-llave de una Palizada** en una cuia de Remolino, o bajo una dosis de su mismo color (b.2 M9, regla 2).
- **Lancha y Liana** juntas antes del 401: son dos esperas por progreso.
- **Pororoca:** las cuias con una mecánica cerrada quedan fijas. Está deshabilitada con Lluvia, Piraña, Cocha y Aguapé que crece.

### d.4 Calendario 1-200

**Niveles 1-20** (a mano, fuera del ciclo; 02 §8 y §13): tutorial 1-10 sin mecánicas; Cuia extra regalada en el 8; tinte Agua del Tapajós en el 11; Morpho en el 12; ★Cuia chica de 2 en el 14 y de 1 en el 18.

**Leyenda:**
- **Contenido puro:** R = recuperación pura (2 vacíos, K−1); C = comodidad pura; S = remanso puro.
- **Marcas de nivel:** ★ = estreno; (s) = reto suave sin etiqueta; (D) = reto que apunta a Difícil; (MD) = hito que apunta a Muy difícil.
- **Capacidades:** Ch = Cuia chica; Ch3 = Cuia chica de 3; Al = Cuia alta de 5; Al6 = Cuia alta de 6.
- **Mecánicas:** Ho Hoja · Ma Matapi · Li Liana con fruta · Tu Aguas turbias · Re Remolino · Ba Barro · Ll Lluvia · Br Brote · Pi Piraña.

Ch y Al marcan los niveles donde la capacidad es protagonista. En el resto, el generador puede usarlas como forma de tablero (02 §8).

| p | 21-40 · B · M2 | 41-60 · A · M3 | 61-80 · B · M3 | 81-100 · A · M4 | 101-120 · B · M4 | 121-140 · A · M4 | 141-160 · A · M5 | 161-180 · B · M5 | 181-200 · B · M5 |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 21 R | 41 R (tinte Achiote) | 61 R (tinte Agua del Río Negro) | 81 R (tinte Copoazú) | 101 R | 121 R | 141 R (tinte Huito) | 161 R | 181 R |
| 2 | 22 ★Ch3 (canon) | 42 R | 62 R | 82 R | 102 R | 122 R | 142 R | 162 R | 182 R |
| 3 | 23 Ch | 43 ★Ho | 63 ★Ma | 83 ★Li | 103 ★Re | 123 ★Ba | 143 ★Ll | 163 ★Br | 183 ★Pi |
| 4 | 24 C | 44 ★Ho | 64 ★Ma | 84 ★Li | 104 ★Re | 124 ★Ba | 144 ★Ll | 164 ★Br | 184 ★Pi |
| 5 | 25 C | 45 ★Ho | 65 ★Ma | 85 ★Li | 105 ★Re | 125 ★Ba | 145 ★Ll | 165 ★Br | 185 ★Pi |
| **6** | 26 Ch (s) | 46 Ch (D) | 66 Ho+Ch (D) | 86 Ma+Ho (D) | 106 Ho+Tu (D) | 126 Re+Ho (D) | 146 Ba+Re (D) | 166 Ll+Re (D) | 186 Br+Ho (D) |
| 7 | 27 ★Al (canon) | 47 Ho | 67 Ma | 87 Li | 107 Re | 127 Ba | 147 Ll | 167 Br | 187 Pi |
| 8 | 28 ★Al | 48 C | 68 C | 88 C | 108 C | 128 C | 148 C | 168 C | 188 C |
| **9** | 29 Ch (s) | 49 Ho (s) | 69 Ma (D) | 89 Li (D) | 109 Re (D) | 129 Ba (D) | 149 Ll (D) | 169 Br (D) | 189 Pi (D) |
| 10 | 30 C (Pororoca) | 50 C | 70 C | 90 C | 110 C | 130 C | 150 C | 170 C | 190 C |
| 11 | 31 C (canoas; 1 vacío) | 51 Al | 71 Al | 91 Ch | 111 Ma (tinte Lulo) | 131 Li | 151 Li | 171 Ho | 191 Re |
| **12** | 32 Al (s) | 52 Ho+Ch (D) | 72 Ma+Ho (D) | 92 Li+Ho (D) | 112 Re+Li (D) | 132 Ba+Ma (D) | 152 Ll+Ho (D) | 172 Br+Ba (D) | 192 Pi+Ma (D) |
| 13 | 33 Ch | 53 C | 73 C | 93 C | 113 C | 133 C | 153 C | 173 C | 193 C |
| 14 | 34 C | 54 C | 74 Ho | 94 Ma | 114 Ch | 134 Ho | 154 Ma | 174 Ma | 194 Ll |
| **15** | 35 Ch+Al (s) | 55 Al (s) | 75 Al+Ch (D) | 95 Li+Al (D) | 115 Ma+Tu (D) | 135 Ba+Tu (D) | 155 Ma+Tu (D) | 175 Br+Tu (D) | 195 Ba+Tu (D) |
| 16 | 36 C | 56 C | 76 C | 96 C | 116 C | 136 C | 156 C | 176 C | 196 C |
| 17 | 37 Al | 57 Ch | 77 C | 97 C | 117 C | 137 C | 157 C | 177 C | 197 C |
| **18** | 38 Ch (s) | 58 Ho+Al (D) | 78 Ma+Al (D) | 98 Li+Ma (D) | 118 Re+Ho (D) | 138 Ba+Li (D) | 158 Ll+Ba (D) | 178 Br+Ma (D) | 198 Pi+Li (D) |
| 19 | 39 S | 59 S | 79 S | 99 S | 119 S | 139 S | 159 S | 179 S | 199 S |
| **20** | 40 Ch+Al (D) «Cuias de Santarém» | 60 Ho+Ch (MD) | 80 Ho+Ma (MD) «Lago de Tefé» [P] | 100 ★Tu (MD; sola, 1 cuia, 2 ocultas) | 120 Re+Ma (MD) | 140 Re+Li (MD) «Remolinos del Trapecio» [P] | 160 Ll+Li+Al6 (MD) «Creciente del Itaya» [P] | 180 Ma+Li (MD) | 200 Pi+Ba (MD) «Vaciante de Pacaya-Samiria» |

**Comprobaciones de la tabla:**
- **Cada titular aparece 8 veces en su ciclo de debut** (por ejemplo, Hoja: 43, 44, 45, 47, 49, 52, 58 y 60).
- **Niveles puros:** 8 por ciclo (40 %). En 41-200 son p1, p2, p19 y cinco de comodidad. En 21-40 el 22 (p2) lleva la Cuia chica de 3 del canon, y el octavo puro es una comodidad más.
- **Ninguna combinación prohibida.**
- **Aguas turbias, como mucho 2 por ciclo**, siempre con filtros (Hoja, Matapi, Barro, Brote).
- **Un contador por nivel como mucho** (Lluvia y Piraña nunca juntas).
- **Cada mecánica vuelve en el ciclo siguiente como secundaria:** Hoja en el 66, Matapi en el 86, Liana en el 112, Remolino en el 126, Barro en el 146, Lluvia en el 166 y Brote en el 186.
- **Opción mínima (Q5, A):** se intercambian Ma y Br en **todas** las columnas 61-200. Intercambiarlos solo en 61-80 y 161-180 no vale: el Matapi seguiría en 86, 94, 98, 111, 115, 120, 132, 154 y 155, antes de su debut en el 163, y ningún reto ni hito puede usar una mecánica antes de sus tableros de debut (03 §9, consecuencia 3; 02 §9.2, regla 2). Las temporadas siguen valiendo: el ciclo 61-80 (B) estrenaría el Brote, que es de todo el año, y el 161-180 (B), el Matapi, que es de aguas bajas.
- **Nombres de hitos [P]:** «Puerto de Manaos» (80), «Remolinos del Trapecio» (140) y «Creciente del Itaya» (160) sustituyen a «Encuentro», «Rebojo del Caquetá» y «Belén en creciente». «Encuentro» ya nombra una skin y un evento, y el de Manaos es Negro + Solimões; «Rebojo del Caquetá» mezclaba un término brasileño con un río colombiano que no es del mundo 4; «Belén» es un barrio con pobreza extrema [V F28]. Todos pasan al asesor. *Actualización del 2026-10-14 (D-023):* el 80 pasa a «Lago de Tefé» [P] (D-013, Estado (Implementación); `tools/level-generator/specs/campaign_v1.json`), porque con el Mapa del río el 80 se juega en la parada del lago de Tefé y no en Manaos; es solo metadato y los packs no cambian. «Puerto de Manaos» queda como estaba en (e) y en (i), que son de la revisión del 2026-10-08.

### d.5 Regla para 201-1000

> **Rehecha el 2026-10-14 (D2).** El dueño decidió rehacer la tabla de novedades antes de generar 201-700 (pregunta 33 de `docs/GDD.md` §6.3, (a); D-020), con el alcance de la 1.0 de D-007 (Q8 c + b′), la Tinaja en el 323 (D-019) y las variantes. Los puntos 1, 2, 4 y 6 no cambian; el 3 añade cómo se leen «invitada» y «descansa», y el 5 cambia entero. El calendario de 201-700 va nivel a nivel, como d.4, en dos variantes: **A**, con la Tinaja, y **B**, sin ella (la salida B de D-019). Las dos cumplen la regla de 60 niveles (d.5.1-d.5.5). El apartado tiene además el registro de combinaciones (d.5.6), la tabla de 701-1000, orientativa (d.5.7), y la recomendación (d.5.8), que se aplica por D-023, punto 10. La columna «Estrenos y novedades» y las firmas de la sección (e) para los mundos 6-13 son de la revisión del 2026-10-08: mandan las de este apartado.

1. **Plantilla del ciclo:** la misma de c.3 y c.4, con las reglas de d.2 y d.3.
2. **Etiquetas:** cerca del 25 % de Difícil y del 5 % de Muy difícil desde el 201 (03 §11, sin cambios).
3. **Titulares:** 2 por ciclo (de la familia de la temporada o de todo el año) y 1-2 invitadas. Una titular descansa en el siguiente ciclo de su temporada. *Cómo lo lee D2 [P]:*
   - **invitadas:** las mecánicas que acompañan a las titulares en los retos y el hito, sin contar Aguas turbias;
   - **regresos:** las comodidades de una mecánica (p3-p5 fuera de un estreno, p8, p11, p14 y p17) traen de vuelta, de una en una, las de la bolsa que lo necesitan para cumplir d.2, regla 4, como en d.4 (171 Ho, 191 Re);
   - **descansa:** no es titular en el siguiente ciclo de su temporada (las de todo el año, en el ciclo siguiente), pero puede salir como invitada o en una comodidad;
   - **las variantes y la Tinaja** siguen el ciclo de vida de d.1 desde su estreno: 8 niveles en su ciclo, secundaria en 2 retos del siguiente (a veces también en su hito) y descanso en el C+2.
4. **Temporadas** (inicio de cada ciclo):

| Niveles | Temporadas por ciclo |
|---|---|
| 201-381 | 201 A · 221 A · 241 B · 261 B · 281 A · 301 A · 321 B · 341 A · 361 B · 381 A |
| 401-581 | 401 A · 421 B · 441 B · 461 A · 481 B · 501 A · 521 B · 541 A · 561 B · 581 B |
| 601-781 | 601 A · 621 B · 641 B · 661 A · 681 B · 701 A · 721 B · 741 A · 761 A · 781 B |
| 801-981 | 801 A · 821 B · 841 A · 861 B · 881 A · 901 B · 921 A · 941 B · 961 A · 981 B |

5. **Novedades:** nunca más de 60 niveles sin algo nunca visto (D-009, punto 8). Con el alcance de la 1.0 (Q8 c + b′ de D-007), las novedades de 201-700 son:
   - los tintes Cacao (201) y Arazá (301);
   - las 4 variantes de coste S, Liana de cuenta (243), Hoja atada (283), Brote de dos hojas (343) y Flor (403), y la Tinaja (323), solo en la variante A;
   - el hito 220, las firmas de fin de mundo (300, 420, 560 y 700), el primer trío (hito 320) y el primer reto con tres mecánicas (646);
   - la forma de tablero nueva: el primer hito de 14 cuias, el 580 (K = 13 + 1 vacío desde el 561, 03 §11);
   - y las combinaciones nuevas, que tienen que ser primera vez en el registro (d.5.6).

   Aguapé (223), Palizada (263), Lancha (303) y Cocha (583) son de la 1.1 y no salen en 201-700. Llegan con su build, por `level_overrides` o con una vuelta nueva y con la regla de migración (03 §9), y sus ciclos conservan la temporada del punto 4 para cuando lleguen. Con ellas llegan, en su nivel, Palizada en cadena (443) y Llave en la lancha (520). La Cuia de la casa no se usa (D-023, punto 7).

   **Qué cambia respecto a la revisión del 2026-10-08:**
   - el hito 220 ya no lleva «Aguas turbias y 2 filtros», que serían 3 mecánicas antes del 300 (02 §9.2, regla 4); lleva Aguas turbias con el Barro y una Cuia chica, y es la primera vez que las turbias salen acompañadas en un hito;
   - las firmas que usaban mecánicas de la 1.1 cambian de combinación, pero no de nombre: «Cuyabeno» (300) pasa de Aguapé + Liana con fruta a Hoja atada + Liana con fruta; «Zafra» (420), de Lancha + Brote a Flor + Brote de dos hojas; «Riberalta» (560), de Lancha + Flor a Flor + Remolino; y «Yarinacocha» (700), de Cocha + Matapi a Matapi + Hoja atada + Brote de dos hojas;
   - la combinación del 500 (Piraña + Palizada) y el trío de aguas bajas del 640 (Matapi + Palizada + Cocha) salen de 201-700, porque necesitan la Palizada y la Cocha; la Llave en la lancha del 520 llega con la 1.1 (párrafo anterior).

   **Cómo se comprueba:** se mide la distancia entre dos primeras veces seguidas (estrenos, tintes nuevos y entradas del registro), desde la última de 1-200 (el 200, Piraña + Barro) hasta el 700. Con la Tinaja y sin ella, ninguna pasa de 34 niveles (526 → 560), y la columna «Novedad» de d.5.2 basta por sí sola.

6. **Después del 1000,** las vueltas siguientes reciclan las plantillas con las LUT de lluvia, noche y alba (glosario §8). Las mecánicas nuevas de live-ops entran por la bolsa con el mismo ciclo de vida.

#### d.5.1 Códigos y reglas del calendario de 201-700

Las celdas tienen el formato literal de d.4: R, C, S, ★, (D), (MD), (tinte X) y «nombre» [P]. Las copia `tools/level-generator/specs/campaign_v2.json` (tarea P4). Desde el 61, todos los retos llevan (D) y todos los hitos, (MD) (D-009). Los códigos nuevos [P] son los que proponía la tarea P5 (Ln, Ha, B2 y Fl) más el de la Tinaja en level-qa (Ti):

| Código | Qué es | Estreno | Temporada en la bolsa | Con qué no va |
|---|---|---|---|---|
| Ln | Liana de cuenta (`liana` con `n`) | 243 | A, la de la Liana. Se estrena en un ciclo B: las variantes pueden estrenarse en cualquier temporada (d.2, regla 7) | Capacidades, Piraña ni Tinaja (bloqueo C1-9); Aguas turbias (Q14 A); otra Liana o la Flor |
| Ha | Hoja atada (`hoja_atada`) | 283 | Todo el año | Aguas turbias [P] |
| Ti | Tinaja y su cuia (`tinaja` + `cucharon`), que cuentan como una mecánica (02 §9.2, regla 4) | 323, solo en la variante A | B [P] (propuesta de la Tinaja, d.1) | Solo va con Hoja, Hoja atada, Brote, Brote de dos hojas [P], Matapi, Barro y capacidades: nunca con Aguas turbias, Liana, Remolino, Lluvia, Piraña ni la 1.1 (02 §9.1, M13). Nunca en p1, p2 ni p19, ni en un hito de 14 cuias |
| B2 | Brote de dos hojas (`brote` con `n` = 2) | 343 | Todo el año | Aguas turbias [P] |
| Fl | Flor de vitória-régia (`flor`) | 403 | A | Capacidades y Piraña [P]: el bloqueo de la Liana, si P1 falla (tarea K3b). Tampoco la Tinaja, Aguas turbias (Q14 A) ni otra Liana |

Además [P]:
- **Hitos de 14 cuias** (K = 13 + 1 vacío, del 580 al 700; la celda lo dice con «(MD; K = 13)»): sin capacidades, sin Piraña, cuya cueva sería la cuia 15 (02 R1), y sin Tinaja, con la que hay como mucho 12 cuias (02 §5).
- **Aguas turbias,** solo con Hoja, Matapi, Barro o Brote y con capacidades (Q14 A), nunca con las variantes de filtro. Solo en retos e hitos y, como mucho, 2 por ciclo (b.2); este calendario lleva una por ciclo como mucho, casi siempre en p15.
- **Una mecánica y su variante no van juntas** (Hoja con Hoja atada, Brote con Brote de dos hojas, Liana con fruta con Liana de cuenta o con la Flor), para que se distingan: la Hoja atada puede confundirse con la Hoja (b.3).
- **Por nivel:** 0-1 mecánicas en las comodidades; 1-2 en los retos (hasta 3 desde el 301, solo en retos e hitos); 2 en el hito (hasta 3 desde el 320).
- **Capacidades:** la Cuia chica (Ch) y la Cuia alta (Al) salen en comodidades (solas o con una mecánica) y en el hito 220, para no pasar de 120 niveles sin volver; nunca con la Liana ni con la Flor. La Cuia alta de 6 sigue solo en hitos (02 §8), y este calendario no la usa.
- **Los niveles con la Tinaja tienen como mucho 12 cuias** (02 §5). Desde el 441 sale en una comodidad suelta en cada ciclo de aguas bajas, con el tope de 3 dosis y 2 ocultas (D-019, Q-Ti1-A).

#### d.5.2 Los 25 ciclos de 201-700

Variante A, con la Tinaja. Titulares: las dos del ciclo. Invitadas: las demás mecánicas de los retos y el hito, sin contar Aguas turbias; los regresos sueltos en comodidad están en d.5.3. Distancia: los niveles desde la novedad anterior; la primera, desde el 200.

| Ciclo | Mundo | Temp. | Titulares | Invitadas (retos e hito) | Novedad | Tipo | Distancia |
|---|---|---|---|---|---|---|---|
| 201-220 | 6 Oriente | A | Liana con fruta, Remolino | Barro, Piraña | 201: Tinte Cacao<br>220: Primer hito con Aguas turbias y otra mecánica: Barro, con una Cuia chica | Tinte<br>Hito (combinación) | 1<br>19 |
| 221-240 | 6 Oriente | A | Lluvia, Hoja | Matapi, Brote | 229: Lluvia con Brote | Combinación | 9 |
| 241-260 | 6 Oriente | B | Liana de cuenta (estreno), Matapi | Remolino, Barro | 243: Liana de cuenta | Variante | 14 |
| 261-280 | 6 Oriente | B | Piraña, Barro | Brote, Liana de cuenta | 266: Piraña con Brote | Combinación | 23 |
| 281-300 | 6 Oriente | A | Hoja atada (estreno), Liana con fruta | Remolino, Brote | 283: Hoja atada<br>300: «Cuyabeno» [P]: Hoja atada + Liana con fruta | Variante<br>Firma de fin de mundo | 17<br>17 |
| 301-320 | 7 Madre de Dios | A | Lluvia, Brote | Hoja, Hoja atada | 301: Tinte Arazá (paleta completa)<br>320: Primer trío: Hoja atada + Lluvia + Brote | Tinte<br>Trío (hito) | 1<br>19 |
| 321-340 | 7 Madre de Dios | B | Tinaja (estreno), Matapi | Hoja, Brote | 323: Tinaja | Mecánica (M13) | 3 |
| 341-360 | 7 Madre de Dios | A | Brote de dos hojas (estreno), Liana de cuenta | Remolino, Tinaja | 343: Brote de dos hojas | Variante | 20 |
| 361-380 | 7 Madre de Dios | B | Piraña, Hoja | Barro, Brote de dos hojas | 366: Piraña con Brote de dos hojas | Combinación | 23 |
| 381-400 | 7 Madre de Dios | A | Liana con fruta, Hoja atada | Barro, Tinaja | 389: Hoja atada con Barro | Combinación | 23 |
| 401-420 | 7 Madre de Dios | A | Flor (estreno), Barro | Lluvia, Brote de dos hojas | 403: Flor de vitória-régia<br>420: «Zafra» [P]: Flor + Brote de dos hojas | Variante<br>Firma de fin de mundo | 14<br>17 |
| 421-440 | 8 Madidi | B | Matapi, Hoja | Remolino, Flor | 426: Flor con Matapi | Combinación | 6 |
| 441-460 | 8 Madidi | B | Piraña, Brote | Remolino, Barro | 449: Brote con Remolino | Combinación | 23 |
| 461-480 | 8 Madidi | A | Liana con fruta, Brote de dos hojas | Hoja, Liana de cuenta | 466: Liana con fruta con Brote de dos hojas | Combinación | 17 |
| 481-500 | 8 Madidi | B | Matapi, Hoja atada | Remolino, Barro | 486: Matapi con Hoja atada | Combinación | 20 |
| 501-520 | 8 Madidi | A | Lluvia, Barro | Hoja atada, Flor | 509: Flor con Hoja atada | Combinación | 23 |
| 521-540 | 8 Madidi | B | Piraña, Remolino | Hoja, Hoja atada | 526: Piraña con Hoja atada | Combinación | 17 |
| 541-560 | 8 Madidi | A | Flor, Hoja | Remolino, Liana de cuenta | 560: «Riberalta» [P]: Flor + Remolino | Firma de fin de mundo | 34 |
| 561-580 | 9 Ucayali | B | Matapi, Brote de dos hojas | Remolino, Barro | 580: Primer hito de 14 cuias (K = 13 + 1 vacío): Matapi + Brote de dos hojas | Forma de tablero | 20 |
| 581-600 | 9 Ucayali | B | Piraña, Hoja atada | Barro, Brote de dos hojas | 589: Hoja atada con Brote de dos hojas | Combinación | 9 |
| 601-620 | 9 Ucayali | A | Liana de cuenta, Remolino | Liana con fruta, Lluvia | 609: Liana con fruta con Lluvia | Combinación | 20 |
| 621-640 | 9 Ucayali | B | Matapi, Barro | Hoja, Lluvia | 629: Lluvia con Matapi | Combinación | 20 |
| 641-660 | 9 Ucayali | B | Piraña, Brote | Remolino, Hoja atada | 646: Primer reto con tres mecánicas: Piraña + Brote + Hoja atada | Trío (reto) | 17 |
| 661-680 | 9 Ucayali | A | Liana con fruta, Hoja atada | Remolino, Brote | 669: Liana con fruta con Brote | Combinación | 23 |
| 681-700 | 9 Ucayali | B | Matapi, Hoja | Hoja atada, Brote de dos hojas | 700: «Yarinacocha» [P]: Matapi + Hoja atada + Brote de dos hojas | Firma de fin de mundo (trío) | 31 |

#### d.5.3 Calendario nivel a nivel de 201-700 (variante A)

Leyenda de d.4, más Ln (Liana de cuenta), Ha (Hoja atada), Ti (Tinaja), B2 (Brote de dos hojas) y Fl (Flor), y «K = 13» en los hitos de 14 cuias. Las 19 celdas con la Tinaja tienen respaldo en d.5.4. Los nombres de hito son [P] y pasan por el asesor cultural. Con el Mapa del río (D-013), el 300, el 560 y el 700 se juegan en Napo alto y Misahuallí, en Trinidad y los Llanos de Moxos y en el Tambo bajo, no en Cuyabeno, Riberalta ni Yarinacocha (sitios candidatos, propuesta del Mapa §5.4). El asesor puede cambiar esos nombres, como se hizo con el 80 (D-023, punto 21). «Zafra» no nombra un lugar (D-020, 28 a).

| p | 201-220 · A · M6 | 221-240 · A · M6 | 241-260 · B · M6 | 261-280 · B · M6 | 281-300 · A · M6 |
|---|---|---|---|---|---|
| 1 | 201 R (tinte Cacao) | 221 R | 241 R | 261 R | 281 R |
| 2 | 202 R | 222 R | 242 R | 262 R | 282 R |
| 3 | 203 Li | 223 Ll | 243 ★Ln | 263 Pi | 283 ★Ha |
| 4 | 204 C | 224 C | 244 ★Ln | 264 C | 284 ★Ha |
| 5 | 205 Re | 225 Ho | 245 ★Ln | 265 Ba | 285 ★Ha |
| **6** | 206 Pi+Ba (D) | 226 Ll+Ho (D) | 246 Ma+Re (D) | 266 Pi+Br (D) | 286 Li+Re (D) |
| 7 | 207 C | 227 C | 247 Ln | 267 C | 287 Ha |
| 8 | 208 Br | 228 Ba | 248 C | 268 Re | 288 C |
| **9** | 209 Li+Re (D) | 229 Br+Ll (D) | 249 Ln (D) | 269 Ln+Br (D) | 289 Ha (D) |
| 10 | 210 C | 230 C | 250 C | 270 C | 290 C |
| 11 | 211 Li | 231 Ll | 251 Ma | 271 Pi | 291 Li |
| **12** | 212 Pi (D) | 232 Ma+Ho (D) | 252 Ln+Re (D) | 272 Pi+Ba (D) | 292 Ha+Re (D) |
| 13 | 213 C | 233 C | 253 C | 273 C | 293 C |
| 14 | 214 Ch | 234 Al | 254 Ho | 274 Al | 294 Ba |
| **15** | 215 Re+Ba (D) | 235 Br+Tu (D) | 255 Ma+Tu (D) | 275 Ba+Tu (D) | 295 Br+Tu (D) |
| 16 | 216 C | 236 C | 256 C | 276 C | 296 C |
| 17 | 217 Ho | 237 Re | 257 C | 277 Ho | 297 Ch |
| **18** | 218 Li+Ba (D) | 238 Ll+Ho (D) | 258 Ln+Ma (D) | 278 Ln+Ba (D) | 298 Ha+Br (D) |
| 19 | 219 S | 239 S | 259 S | 279 S | 299 S |
| **20** | 220 Ba+Tu+Ch (MD) | 240 Ho+Br (MD) | 260 Ln+Ba (MD) | 280 Pi+Br (MD) | 300 Ha+Li (MD) «Cuyabeno» [P] |

| p | 301-320 · A · M7 | 321-340 · B · M7 | 341-360 · A · M7 | 361-380 · B · M7 | 381-400 · A · M7 |
|---|---|---|---|---|---|
| 1 | 301 R (tinte Arazá) | 321 R | 341 R | 361 R | 381 R |
| 2 | 302 R | 322 R | 342 R | 362 R | 382 R |
| 3 | 303 Ll | 323 ★Ti | 343 ★B2 | 363 Pi | 383 Li |
| 4 | 304 C | 324 ★Ti | 344 ★B2 | 364 C | 384 C |
| 5 | 305 Br | 325 ★Ti | 345 ★B2 | 365 Ho | 385 Ha |
| **6** | 306 Ha+Ll (D) | 326 Br+Ma (D) | 346 Ln+Re (D) | 366 Pi+B2 (D) | 386 Li+Ba (D) |
| 7 | 307 C | 327 Ti | 347 B2 | 367 C | 387 C |
| 8 | 308 Re | 328 C | 348 C | 368 Ha | 388 Br |
| **9** | 309 Br+Ll (D) | 329 Ti (D) | 349 B2 (D) | 369 Ho+Tu (D) | 389 Ha+Ba (D) |
| 10 | 310 C | 330 C | 350 C | 370 C | 390 C |
| 11 | 311 Ba | 331 Ma | 351 Ln | 371 Re | 391 Li |
| **12** | 312 Ha+Br (D) | 332 Ti+Ho (D) | 352 B2+Ti (D) | 372 B2+Ho (D) | 392 Ti+Ha (D) |
| 13 | 313 C | 333 C | 353 C | 373 C | 393 C |
| 14 | 314 Ho | 334 Re | 354 Ha | 374 Al | 394 Ch |
| **15** | 315 Br+Tu (D) | 335 Ma+Ho (D) | 355 Ti (D) | 375 Pi+Ba (D) | 395 Ba+Tu (D) |
| 16 | 316 C | 336 C | 356 C | 376 C | 396 C |
| 17 | 317 Ll | 337 Ba | 357 Br | 377 Br | 397 Ho |
| **18** | 318 Ll+Ho (D) | 338 Ti+Ma (D) | 358 B2+Ln (D) | 378 Pi+Ho (D) | 398 Li+Ha (D) |
| 19 | 319 S | 339 S | 359 S | 379 S | 399 S |
| **20** | 320 Ha+Ll+Br (MD) | 340 Ti+Br (MD) | 360 B2+Re (MD) | 380 Pi+B2 (MD) | 400 Li+Ha+Ba (MD) |

| p | 401-420 · A · M7 | 421-440 · B · M8 | 441-460 · B · M8 | 461-480 · A · M8 | 481-500 · B · M8 |
|---|---|---|---|---|---|
| 1 | 401 R | 421 R | 441 R | 461 R | 481 R |
| 2 | 402 R | 422 R | 442 R | 462 R | 482 R |
| 3 | 403 ★Fl | 423 Ma | 443 Pi | 463 Li | 483 Ma |
| 4 | 404 ★Fl | 424 C | 444 C | 464 C | 484 C |
| 5 | 405 ★Fl | 425 Ho | 445 Br | 465 B2 | 485 Ha |
| **6** | 406 Ba+Ll (D) | 426 Fl+Ma (D) | 446 Pi+Ba (D) | 466 Li+B2 (D) | 486 Ma+Ha (D) |
| 7 | 407 Fl | 427 C | 447 C | 467 C | 487 C |
| 8 | 408 C | 428 Ba | 448 Ho | 468 Re | 488 Ho |
| **9** | 409 Fl (D) | 429 Ma+Re (D) | 449 Br+Re (D) | 469 Ln+B2 (D) | 489 Ma+Ba (D) |
| 10 | 410 C | 430 C | 450 C | 470 C | 490 C |
| 11 | 411 Re | 431 B2 | 451 Pi | 471 Br | 491 Br |
| **12** | 412 Fl+Ba (D) | 432 Fl+Ho (D) | 452 Pi+Br (D) | 472 Li+Ho (D) | 492 Ha+Ba (D) |
| 13 | 413 C | 433 C | 453 C | 473 C | 493 C |
| 14 | 414 Br | 434 Al | 454 Ti | 474 Ch | 494 Ti |
| **15** | 415 Ba+Tu (D) | 435 Ma+Tu (D) | 455 Br+Tu (D) | 475 Ho+Tu (D) | 495 Ma+Tu (D) |
| 16 | 416 C | 436 C | 456 C | 476 C | 496 C |
| 17 | 417 Ha | 437 Ha | 457 Ha | 477 Ba | 497 B2 |
| **18** | 418 Fl+Ll (D) | 438 Ho+Re (D) | 458 Ba+Re (D) | 478 Ln+Ho (D) | 498 Ha+Re (D) |
| 19 | 419 S | 439 S | 459 S | 479 S | 499 S |
| **20** | 420 Fl+B2 (MD) «Zafra» [P] | 440 Ma+Ho (MD) | 460 Pi+Br+Ba (MD) | 480 Li+B2+Ho (MD) | 500 Ma+Ha (MD) |

| p | 501-520 · A · M8 | 521-540 · B · M8 | 541-560 · A · M8 | 561-580 · B · M9 | 581-600 · B · M9 |
|---|---|---|---|---|---|
| 1 | 501 R | 521 R | 541 R | 561 R | 581 R |
| 2 | 502 R | 522 R | 542 R | 562 R | 582 R |
| 3 | 503 Ll | 523 Pi | 543 Fl | 563 Ma | 583 Pi |
| 4 | 504 C | 524 C | 544 C | 564 C | 584 C |
| 5 | 505 Ba | 525 Re | 545 Ho | 565 B2 | 585 Ha |
| **6** | 506 Ll+Ba (D) | 526 Pi+Ha (D) | 546 Fl+Ho (D) | 566 Ma+Re (D) | 586 Pi+Ha (D) |
| 7 | 507 C | 527 C | 547 C | 567 C | 587 C |
| 8 | 508 Ho | 528 Br | 548 Ba | 568 Ha | 588 Ho |
| **9** | 509 Fl+Ha (D) | 529 Ha+Re (D) | 549 Ln+Ho (D) | 569 B2+Ba (D) | 589 Ha+B2 (D) |
| 10 | 510 C | 530 C | 550 C | 570 C | 590 C |
| 11 | 511 Li | 531 Ti | 551 Ll | 571 Ho | 591 Ti |
| **12** | 512 Ll+Ha (D) | 532 Pi+Ho (D) | 552 Fl (D) | 572 Ma+Ba (D) | 592 Pi+B2 (D) |
| 13 | 513 C | 533 C | 553 C | 573 C | 593 C |
| 14 | 514 Al | 534 Ch | 554 Al | 574 Ti | 594 Al |
| **15** | 515 Ba+Tu (D) | 535 Ho+Tu (D) | 555 Ho+Tu (D) | 575 Ma+Tu (D) | 595 Ba+Tu (D) |
| 16 | 516 C | 536 C | 556 C | 576 C | 596 C |
| 17 | 517 B2 | 537 B2 | 557 Br | 577 Br | 597 Br |
| **18** | 518 Fl+Ba (D) | 538 Ho+Re (D) | 558 Ln+Re (D) | 578 B2+Re (D) | 598 Pi+Ba (D) |
| 19 | 519 S | 539 S | 559 S | 579 S | 599 S |
| **20** | 520 Ll+Ba+Ha (MD) | 540 Pi+Ha (MD) | 560 Fl+Re (MD) «Riberalta» [P] | 580 Ma+B2 (MD; K = 13) | 600 Ha+B2+Ba (MD; K = 13) |

| p | 601-620 · A · M9 | 621-640 · B · M9 | 641-660 · B · M9 | 661-680 · A · M9 | 681-700 · B · M9 |
|---|---|---|---|---|---|
| 1 | 601 R | 621 R | 641 R | 661 R | 681 R |
| 2 | 602 R | 622 R | 642 R | 662 R | 682 R |
| 3 | 603 Ln | 623 Ma | 643 Pi | 663 Li | 683 Ma |
| 4 | 604 C | 624 C | 644 C | 664 C | 684 C |
| 5 | 605 Re | 625 Ba | 645 Br | 665 Ha | 685 Ho |
| **6** | 606 Ln+Re (D) | 626 Ma+Ba (D) | 646 Pi+Br+Ha (D) | 666 Li+Ha (D) | 686 Ma+Ho (D) |
| 7 | 607 C | 627 C | 647 C | 667 C | 687 C |
| 8 | 608 Ho | 628 B2 | 648 Br+Al | 668 Ho | 688 Re |
| **9** | 609 Li+Ll (D) | 629 Ll+Ma (D) | 649 Pi+Ha (D) | 669 Li+Br (D) | 689 Ho+B2 (D) |
| 10 | 610 C | 630 C | 650 C | 670 C | 690 C |
| 11 | 611 Br | 631 Ti | 651 Ti | 671 Ba | 691 Ti |
| **12** | 612 Ll+Re (D) | 632 Ma+Ho (D) | 652 Br+Re (D) | 672 Li+Re (D) | 692 Ma+Ha (D) |
| 13 | 613 C | 633 C | 653 C | 673 C | 693 C |
| 14 | 614 Ch | 634 Al | 654 Pi | 674 Ch | 694 Ma+Ch |
| **15** | 615 Li+Re (D) | 635 Ma+Tu (D) | 655 Br+Tu (D) | 675 Br+Tu (D) | 695 Ho+Tu (D) |
| 16 | 616 C | 636 C | 656 C | 676 C | 696 C |
| 17 | 617 Fl | 637 Ha | 657 B2 | 677 B2 | 697 Ba |
| **18** | 618 Ln+Ll (D) | 638 Ba+Ho (D) | 658 Ha+Re (D) | 678 Ha+Re (D) | 698 Ha+B2 (D) |
| 19 | 619 S | 639 S | 659 S | 679 S | 699 S |
| **20** | 620 Ln+Ll+Re (MD; K = 13) | 640 Ma+Ba+Ho (MD; K = 13) | 660 Br+Ha+Re (MD; K = 13) | 680 Li+Ha+Re (MD; K = 13) | 700 Ma+Ha+B2 (MD; K = 13) «Yarinacocha» [P] |

#### d.5.4 Variante B: sin la Tinaja (salida B de D-019)

Si al generar 201-700 la Tinaja no pasa sus pruebas, o Morpho (con corte por tiempo) y la Luciérnaga no están en el cliente, el 323 sale sin ella y la Tinaja pasa a la 1.1 con el mismo nivel (D-019, Q-Ti3-A). La variante B cambia solo las 19 celdas con Tinaja, cada una por su respaldo; el resto del calendario es idéntico.

| Nivel | p | Con Tinaja (A) | Sin Tinaja (B) |
|---|---|---|---|
| 323 | 3 | ★Ti | Ma |
| 324 | 4 | ★Ti | C |
| 325 | 5 | ★Ti | Ba |
| 327 | 7 | Ti | C |
| 329 | 9 | Ti (D) | Ma (D) |
| 332 | 12 | Ti+Ho (D) | Ba+Ho (D) |
| 338 | 18 | Ti+Ma (D) | Ma+Ba (D) |
| 340 | 20 | Ti+Br (MD) | Br+Ba (MD) |
| 352 | 12 | B2+Ti (D) | B2+Ba (D) |
| 355 | 15 | Ti (D) | Re (D) |
| 392 | 12 | Ti+Ha (D) | Ba (D) |
| 454 | 14 | Ti | C |
| 494 | 14 | Ti | C |
| 531 | 11 | Ti | C |
| 574 | 14 | Ti | C |
| 591 | 11 | Ti | C |
| 631 | 11 | Ti | C |
| 651 | 11 | Ti | C |
| 691 | 11 | Ti | C |

- **321-340:** las titulares pasan a ser Matapi y Barro, y la «primera vez» del ciclo es Barro + Hoja (332), que no sale antes del 332. **341-360:** la invitada Tinaja pasa a ser el Barro (352), y el reto 355 lleva solo el Remolino.
- **Novedades:** las de A, salvo el 323, que pasa a ser el 332. Del 320 al 332 hay 12 niveles y del 332 al 343, 11. La regla de 60 se cumple igual, con un hueco máximo de 34.
- **El hito 340 tiene respaldo también en A:** si la tinaja no llega al 0,90 del bot (lo más probable, según la propuesta de la Tinaja, d.3), el pipeline usa Br+Ba (MD) y la Tinaja se queda en 7 apariciones en su ciclo.
- **Con la 1.1,** las celdas de A vuelven por `level_overrides` con la regla de migración (03 §9): el estreno, 323-340 (D-019), y las demás si el dueño quiere.

#### d.5.5 Comprobaciones

Las dos variantes se comprobaron con un script el 2026-10-14, junto con 11-200 (`campaign_v1.json`). P4 lleva estas reglas a `--validate-spec`. En las dos se cumple:
- **Celdas:** R en p1-p2, S en p19, (D) en los cinco retos y (MD) en el hito.
- **Puros:** 7-8 por ciclo en A y 7-9 en B (≥ 30 %), y al menos 4 de las 8 comodidades de p7-p17. En total, 196 de 500 en A (39 %) y 206 en B (41 %).
- **Topes:**
  - como mucho 2 mecánicas por nivel antes del 300 y 3 después, 3 solo en retos e hitos; el primer trío está en el 320;
  - una mecánica, en 6 niveles por ciclo como mucho (8 en su ciclo de estreno);
  - nunca 3 niveles seguidos fuera de su estreno;
  - un contador por nivel.
- **Estrenos:** las 4 variantes y la Tinaja están en p3-p5, solas y en 3 tableros seguidos, con 8 apariciones en su ciclo. Las de la Tinaja son 323, 324, 325, 327, 329, 332, 338 y 340, como en la propuesta de la Tinaja, d.3. Vuelven como secundarias en 2 retos del ciclo siguiente, y a veces en su hito, y descansan en el C+2.
- **Hitos:** mecánicas con 15 niveles o más desde su estreno y que ya salieron en un reto, y la firma al final de cada mundo.
- **Prohibidas:**
  - ninguna combinación de d.3 ni de 02 §9.2, regla 7, ni las de d.5.1;
  - ninguna mecánica de la 1.1;
  - la Tinaja nunca en p1, p2 ni p19.
- **Temporadas:** las del punto 4. Las titulares son de la familia de la temporada o de todo el año (salvo la variante que se estrena), y ninguna repite en el siguiente ciclo de su temporada.
- **Regreso (d.2, regla 4):**
  - las de todo el año salen en cada par de ciclos seguidos;
  - las de temporada, al menos una vez en cada 3 ciclos de su temporada;
  - ninguna mecánica ni capacidad pasa más de 120 niveles sin volver, contando 11-200. El hueco más largo es el de la Liana de cuenta, 111 niveles.
- **Primera vez por ciclo (d.2, regla 3):** todos los ciclos tienen un estreno, un tinte o una combinación de dos códigos nunca vista (FIRST-TIME de `--validate-spec`).
- **NOVELTY-60:** el hueco máximo es de 34 niveles, con la columna «Novedad» y con el registro.

#### d.5.6 Registro de combinaciones publicadas [P]

Es la entrada versionada del pipeline que pide d.2, regla 3 (03 §17, comprobación 15). Con él, «primera combinación» se decide sin mirar los packs generados, así que el test de reproducibilidad por hash sigue valiendo.
- **Archivo:** `tools/level-generator/specs/combinations_registry_v1.json`.
  - Lo genera la tarea P4 desde `campaign_v1.json` (11-200) y `campaign_v2.json` (201-700), en la variante que se publica.
  - Nunca se edita a mano, y regenerarlo da los mismos bytes (UTF-8 sin BOM y LF).
- **Formato** (`af-combinations/1`):

```json
{
  "format": "af-combinations/1",
  "source": ["campaign_v1", "campaign_v2"],
  "variant": "a",
  "combinations": [
    {"level": 14, "codes": ["Ch"], "kind": "debut"},
    {"level": 35, "codes": ["Al", "Ch"], "kind": "combination"},
    {"level": 243, "codes": ["Ln"], "kind": "variant"},
    {"level": 320, "codes": ["Br", "Ha", "Ll"], "kind": "combination"}
  ]
}
```

- **Una entrada por conjunto de códigos:**
  - los códigos son las mecánicas y las capacidades de la celda, sin R, C ni S, marcas, tintes ni nombres;
  - la entrada va en el primer nivel donde sale el conjunto, con los códigos en orden ordinal, como `SpecValidator.Combinations`, y las entradas por nivel;
  - un conjunto que ya salió no vuelve a entrar: una combinación de 11-200 nunca cuenta como nueva en 201-700.
- **`kind`:**
  - `debut`: un código solo, en su celda con ★ (mecánica del catálogo, Tinaja o capacidad);
  - `variant`: un código solo, en la ★ de una variante (Ln, Ha, B2 o Fl);
  - `combination`: dos o más códigos.
- **Lo que comprueba P4 con él,** en las dos variantes:
  - FIRST-TIME: cada ciclo tiene una ★, un tinte nuevo o una entrada del registro;
  - NOVELTY-60: entre dos primeras veces seguidas (entradas del registro o tintes nuevos) nunca hay más de 60 niveles, desde el 200 y hasta el 700.
- **Cifras de este calendario:** 100 entradas con la Tinaja (40 de 11-200 y 60 de 201-700) y 94 sin ella.

#### d.5.7 Tabla de 701-1000 (orientativa, sin generar)

701-1000 no son de la 1.0: llegan después, por `levels_remote` (08 §6.2), cuando el build de la 1.1 ya trae Aguapé, Palizada, Lancha y Cocha. La tabla supone ese catálogo; si una mecánica de la 1.1 no llega, su fila se cambia por una combinación nueva con lo que haya. Las filas [P] son ejemplos: al generar, cada novedad tiene que ser primera vez en el registro, como en 201-700, y si ya está se elige otra. Siguen los hitos de 14 cuias (K = 13 + 1 vacío). K = 14 solo se juega en el evento Encuentro de las Aguas (03 §11).

| Ciclo · mundo | Temp. | Novedad | Tipo | Distancia |
|---|---|---|---|---|
| 701-720 · 10 Alto Negro | A | 703: Casiquiare | Variante del Remolino | 3 |
| 721-740 · 10 | B | 729: Cocha con Matapi [P] | Combinación | 26 |
| 741-760 · 10 | A | 749: Aguapé con Flor [P] | Combinación | 20 |
| 761-780 · 10 | A | 763: Lancha de vuelta | Variante de la Lancha | 14 |
| 781-800 · 10 | B | 789: Casiquiare con Matapi [P] | Combinación | 26 |
| 801-820 · 10 | A | 820: «Dos cuencas» [P]: Casiquiare + Lluvia con Agua del Río Negro | Firma de fin de mundo | 31 |
| 821-840 · 11 Escudo de Guayana | B | 829: Palizada con Brote de dos hojas [P] | Combinación | 9 |
| 841-860 · 11 | A | 860: primer trío con el Aguapé: Lluvia + Aguapé + Liana con fruta | Trío (hito) | 31 |
| 861-880 · 11 | B | 880: primer trío de aguas bajas: Matapi + Palizada + Cocha | Trío (hito) | 20 |
| 881-900 · 11 | A | 883: Aguapé que crece | Variante del Aguapé | 3 |
| 901-920 · 11 | B | 920: «Rupununi» [P]: Palizada + Piraña | Firma de fin de mundo | 37 |
| 921-940 · 12 Invitados del Orinoco | A | 929: Lancha con Flor [P] | Combinación | 9 |
| 941-960 · 12 | B | 960: «Aguas del Orinoco» [P]: 13 colores, Hoja + Brote, rótulo «vecinos del Orinoco» («Río de cinco colores» está reservado a una skin de evento, glosario §7.2) | Firma de fin de mundo | 31 |
| 961-980 · 13 El origen | A | 969: Casiquiare con Aguapé que crece [P] | Combinación | 9 |
| 981-1000 · 13 | B | 1000: «Aguas cruzadas» [P]: Lluvia + Palizada + Hoja | Pico final (trío) | 31 |

- **Hueco máximo:** 37 niveles (883 → 920). Sin las filas [P] es de 60 (703 → 763), justo en el límite: por eso cada ciclo lleva su combinación nueva.
- **Respecto a la revisión del 2026-10-08:**
  - el 860 pasa a ser «el primer trío con el Aguapé», porque 201-700 ya tiene tríos con la Lluvia (320, 520 y 620);
  - el primer trío de aguas bajas pasa del 640 al 880, porque necesita la Palizada y la Cocha;
  - las firmas 820, 920, 960 y 1000 no cambian.
- **Reserva sin decidir, que no cuenta para la regla:** las variantes de la Tinaja (Dos tinajas, Tinaja grande y Cuia de la casa; propuesta de la Tinaja, d.4).

#### d.5.8 Recomendación

**Variante A, con la Tinaja.** Es lo que decidió D-019 (Q-Ti3-A: la seña propia en la 1.0), y es la única variante con el estreno de una mecánica entre el 201 y el 700.
- **P4** copia las dos variantes en `campaign_v2.json`: cada celda con Tinaja lleva su respaldo, y `--variant b` genera con ellos.
- **P7b** genera la A para publicar y guarda la B en `tools/out`.
- **La B se publica solo si,** al cerrar la generación de 301-420, no se cumple la condición de D-019: que la Tinaja pase sus pruebas en el pipeline y que Morpho (con corte por tiempo) y la Luciérnaga estén en el cliente. La B cumple la regla de 60 y todas las demás; solo pierde la seña propia, que vuelve en la 1.1.

El dueño decidió aplicar la recomendación de esta tabla (D-023, punto 10).

---

## (e) Relación con los mundos y los países

Los debuts van por nivel, no por mundo (02 §9). El mundo aporta el arte, el Dato del río y la firma de su último hito.

| Mundo · país | Niveles | Estrenos y novedades | Firma del pico de mundo | Datos del río candidatos (pendientes del asesor) | Vetos propios del mundo |
|---|---|---|---|---|---|
| 1 Estuario · Brasil (Pará) | 1-10 | Tutorial, sin mecánicas | — | Ver-o-Peso, protegido desde 1977 [V F62]; el chibé [V F72] | Peconha y trabajo infantil [F52]; objetos rituales de las bancas de hierbas [V F63] |
| 2 Tapajós · Brasil (Pará) | 11-40 | Cuia chica (14, 18, 22) y Cuia alta (27) | 40 «Cuias de Santarém» (solo capacidades) | Cuias de Santarém y ASARISAN, IPHAN 2015 [V F59, F60]; playas de vaciante [NV: falta fuente brasileña] | Copiar grafismos de las cuias |
| 3 Manaos · Brasil (Amazonas) | 41-80 | Hoja (43), Matapi (63) | 80 «Puerto de Manaos» [P]: Hoja + Matapi con Agua del Río Negro y del Tapajós | Encuentro de las Aguas, IPHAN 2010 [V F25]; régua desde 1902 [V F11]; flutuantes [V F32]; en el debut del 63, el matapi del Bajo Amazonas, cerca de Santarém (Pará), sin atribuirlo a Manaos [V F35]; el manatí y los cuellos de botella del medio Solimões [V F148] | Sequía de 2024 como juego; Parintins; demolición de la Ciudad Flotante |
| 4 Trapecio · Colombia | 81-140 | Liana con fruta (83), Aguas turbias (100), Remolino (103), Barro (123) | 140 «Remolinos del Trapecio» [P]: Remolino + Liana | La física del rebojo, frecuente en el Japurá, en Brasil [V F47]; el Japurá nace en Colombia como Caquetá [V F145] (Dato del río, no base del debut); Tarapoto, sitio Ramsar [V F105]; Puerto Nariño sin carros [V F104]; loros de Leticia [V F103] | Máscaras ticuna; Cobra Grande |
| 5 Loreto · Perú | 141-200 | Lluvia (143), Cuia alta de 6 (160), Brote (163), Piraña (183) | 160 «Creciente del Itaya» [P]; 200 «Vaciante de Pacaya-Samiria»: Piraña + Barro | Iquitos sin carretera [V F80]; Belén sube 5-6 m [V F28]; las tortugas (charapa y taricaya) de la RNPS, con crédito a las comunidades [V F14]; el pirarucú y su manejo en Brasil, en 10 áreas muestreadas [V F44, F91]; la gamitana come fruta [V F90]; el aguaje con subidor [V F122] | Pasaje Paquito; caucho y Putumayo; mito de Eiffel; karuara; Yacumama; Belén como nombre de nivel |
| 6 Oriente · Ecuador | 201-300 | Aguapé (223), Liana de cuenta (243), Palizada (263), Hoja atada (283) | 300 «Cuyabeno»: Aguapé + Liana con fruta | El Napo [V F77]; canoas-taxi de Coca [V F78]; el aguapé nativo [V F109] | Ritual de la guayusa; pueblos aislados; maito sin crédito |
| 7 Madre de Dios · Perú | 301-420 | Lancha (303), trío (320), Brote de dos hojas (343), Flor (403) | 420 «Zafra»: Lancha + Brote | Astilleros de Puerto Maldonado [V F96]; la castaña y el añuje [V F93]; la zafra [V F94] | Oro y minería; pueblos de contacto limitado; sumaúma o lupuna [V F153] |
| 8 Madidi · Bolivia | 421-560 | Palizada en cadena (443), combinación 500, Llave en la lancha (520) | 560 «Riberalta»: Lancha + Flor | Riberalta y el 78 % de la castaña mundial [V F95]; el paiche contado con sus dos caras [V F92] | El paiche como «especie a proteger» |
| 9 Ucayali · Perú | 561-700 | Cocha (583), trío de aguas bajas (640) | 700 «Yarinacocha»: Cocha + Matapi | Yarinacocha [V F147]; Pucallpa [V F108]. (El dato del manatí es del medio Solimões, en Brasil: pasa al mundo 3) | Kené, ayahuasca, turismo «chamánico»; sumaúma o lupuna [V F153] |
| 10 Alto Negro · Venezuela y Brasil | 701-820 | Casiquiare (703), Lancha de vuelta (763) | 820 «Dos cuencas»: Casiquiare + Lluvia | El Casiquiare une dos cuencas [V F146]; el rebojo es raro en el Negro [V F47], así que el Remolino normal pesa poco aquí; el agua negra es oscura pero transparente, así que hay menos Aguas turbias [E] | Neblina en primer plano; yanomami |
| 11 Escudo de Guayana | 821-920 | Trío de aguas altas (860), Aguapé que crece (883) | 920 «Rupununi»: Palizada + Piraña | El caño Pirara une dos cuencas en la crecida [V F130] | Pueblos de la zona (solo paisaje) |
| 12 Invitados del Orinoco | 921-960 | — | 960 «Aguas del Orinoco» [P] | Rótulo «vecinos del Orinoco» (07 §2.1) | Presentarlo como el río Amazonas |
| 13 El origen · Perú (Andes) | 961-1000 | — | 1000 «Aguas cruzadas» | El sedimento nace en los Andes [V L6 §2]; poca fauna | — |

---

## (f) Qué pasa con las mecánicas actuales y con la propuesta §5

| Mecánica | Qué pasa | Por qué | Toca |
|---|---|---|---|
| Hoja (M1, 43) | Se queda igual. PT «Palha» [P]. Arreglo de fútiles. Variante Hoja atada (283) | MS la usa en 387 y 389; la forma persistente es la que MS prueba | 02 §1.4, 02 §9.1, 04 §5.1 |
| Brote (M2) | Misma regla; **debut del 63 al 163** (opción B). Variante de dos hojas (343) | MS estrena los corchos en el 221, después del frasco «?» (~70); el orden depende de una equivalencia [E] (b.2 M10) | Contrato 1 |
| Liana (M3, 83) | **Debuta con fruta** (`key`, como la tela de MS); la de número pasa a variante (243). El contenido se ve; la opción C de la pregunta 5.4 se decide en Q17 | Es el gancho G1 de la ficha [L1 §12.1], legible por el icono | 02 §9.1, 03 §9, esquema (contrato 21) |
| Remolino (M4, 103) | Igual. PT «Rebojo». Matapi nunca en su par. Variante Casiquiare (703) | El rebojo real «lleva las cosas al fondo» | 02 §9.1 |
| Barro (M5, 123) | Igual. Nunca dentro de un Matapi | — | 02 §9.2 |
| Lluvia (M6, 143) | Igual. Un contador por nivel como mucho. Criterio de la 1.0 sin cambios | — | 02 §9.2 |
| Piraña (M7, 183) | Igual. No muerde al Matapi. Arte sin sangre | Biología real | 02 §9.1 |
| Aguas turbias (100) | Igual (T1-T7). Debut en el 100 como en el canon, con la información oculta mínima (sin otra mecánica y con 2 dosis ocultas; Q9); revelado en menos de 1,5 s con burbuja; solo con filtros | La información oculta no debe estrenarse fuerte; el azar es la queja n.º 1 | 02 §10-§11, 03 §8, 07 §4.3 (04 §2.5, sin cambio) |
| Cuia chica y Cuia alta | Iguales | — | — |
| Propuesta §5.2: Hoja | Se mantiene, con una etiqueta de fruta grande y clara | — | — |
| Propuesta §5.2: Liana con fruta | **Adoptada** en el 83 (antes: «1.0, pregunta 10.2 #8») | — | — |
| Propuesta §5.2: Aguapé (M8) | **Adoptado**, debut en el 223 (las dos propuestas coinciden) | — | — |
| Propuesta §5.2: Nasa con pez (M9) | **Sustituida por la Palizada** (troncos y pez); el tronco hueco se descarta | Base verificada (IIAP) y sin arte de pesca concreto | — |
| Propuesta §5.2: SUPER HARD | Etiqueta en el botón Jugar, con «×3» y «×4» | — | 02 §10, 07 §7.7 |
| Propuesta §5.2: Botella añadida | Ya es canon (D-006) | — | — |
| Propuesta 5.4 (contenido oculto) | Ninguna regla de esta propuesta tapa contenido. Pregunta cerrada Q17, con lo que tocaría | La opción C es información oculta en niveles Normales desde el 83 | 02 §11, 03 §8 |
| Propuesta 5.5 (trampas tempranas) | Recomendación B (medirlas con tope por etiqueta) | — | 03 §15 y §17 |

---

## (g) Cambios que exigiría al GDD (sin editarlo)

**Documento maestro y contratos**
- `docs/GDD.md`:
  - §2 #7 (curva: posiciones de reto y remanso);
  - §2 #12 (mecánicas y debuts);
  - §7: contrato 1 (`mech_debut_level` y, por la cadencia del ciclo, la incidencia GDD.md §7.2 #20: «retos p6, p10, p14, p17, p19») y contrato 21 (esquema);
  - §6, si cambia el criterio de alcance de la 1.0.
- `docs/dev/decisiones.md`: nueva entrada D-007, que sustituiría lo que corresponda de 02 §9 y 03 §12.

**00 · Glosario**
- §6.1: cinta «×3»/«×4»; los subtítulos no cambian.
- §6.5: nombres reservados nuevos (Matapi, Aguapé, Palizada, Lancha, Cocha) y variantes (Liana de cuenta, Hoja atada, Brote de dos hojas, Flor, Casiquiare, Lancha de vuelta, Aguapé que crece). Debuts copiados.
- §3.3: manatí y Rosa como llave, si se aprueban.
- §10: textos de los tutoriales nuevos en ES, EN y PT.

**02 · Núcleo**
- §1.2 `legal`: bloqueos de origen y destino nuevos (Matapi, Aguapé, Palizada, Lancha, Cocha, Hoja atada).
- §1.4 `futil` y R16: el arreglo del anexo B.
- R10: el orden nuevo del anexo B, con el punto fijo de (c)-(f).
- §5: regla de layout para grupos (Aguapé, Palizada): un grupo nunca se parte entre filas.
- §6: botón «Siguiente» con el chip y la cinta del nivel siguiente.
- §9: texto de cadencia y posiciones de reto.
- §9.1: tabla (debut de M2, regla de M3, filas M8-M12, variantes).
- §9.2: tabla de interacciones con comodines y reglas transversales nuevas (un contador por nivel; combinaciones prohibidas).
- §10: señalización en el botón Jugar, cinta «×n» y posiciones de reto.
- §10: excepción a «2 mecánicas activas» en el hito 100 (Q9).
- §11: T1 con la compatibilidad con filtros; Q17 si se aprueba tapar contenido.
- §4.1: duración del revelado y la fila «Hoja/Brote/Liana se abren» (350 ms), porque la propuesta usa 400-1.500 ms.
- Parámetros: `mech_debut_level.*`.

**03 · Niveles y dificultad**
- §8: definir «mecánica con disparador».
- §9: tabla de semántica para el solver y consecuencias, con los debuts nuevos.
- §11: columna «Mecánicas activas» de la tabla de rangos; la regla «el hito siempre va precedido de un reto» pasa a «de un remanso».
- §12: tabla de posiciones, excepciones de debut y calendario de etiquetas de 41-59.
- §15: KPI nuevos (intentos por mecánica en su debut, trampas tempranas, acierto de etiqueta).
- §17: QA nuevo (trampas tempranas; registro de combinaciones como entrada versionada; plantillas por posición).

**04, 05 y 06**
- 04 §2.5: sin cambio (el debut con 1 cuia turbia y 1 Luciérnaga ya es canon).
- 04 §5.1: su copia de `futil` y los tests de regresión 1-4, con el arreglo del anexo B (el test 4 pasa a «bajo Lluvia con gotas pendientes»).
- 04: interacciones de los comodines con las mecánicas nuevas (Pororoca fija, Cuia extra normal).
- 05 §2: solo si se adopta el «×5».
- 06: lista de Travesías con las reglas de b.4; eventos «Mareas de equinoccio», «Fiesta de la fruta» y «Zafra».

**07 · Arte y audio**
- §2.2: Datos del río por mundo (sección e); skin Cuia de Santarém encargada a ASARISAN.
- §4.3: estados visuales nuevos.
- §5: storyboards de cada desbloqueo.
- §7.7: botones «Jugar» y «Siguiente» con la cinta «×n»; luz de tarde del Difícil [P] (c.5).
- §8.1: léxico pendiente y vetos nuevos (investigacion §7), entre ellos la sumaúma o lupuna.
- §8.2: lista del asesor con «charapa», «palizada», «recreio» y los nombres de hitos.
- §9.2: sonidos de desbloqueo.
- §11: unas 20 piezas nuevas entre sprites, animaciones y sonidos.

**08 · Arquitectura**
- §4.1, el esquema `af-level`:
  - tipos nuevos;
  - campos `group`, `keys` y `oneway`;
  - `key` en la Liana;
  - `k` pasaría a tener tres sentidos (ritmo de Lluvia y Piraña, ventana de la Cocha y vacías de la Lancha): mejor campos propios [P];
  - enum `intro_mechanic` con los tipos nuevos, y enum `band` (¿el remanso es `comfort` o un valor nuevo? [P]);
  - la marca de la llave de la Palizada con capa y grupo;
  - `pack_schema` 2.
- §9, Remote Config: `mech_debut_level.*` nuevas, `mech_enabled.*` y defaults de `lancha_*` y `cocha_k_pours`.
- §8.2: valores de `af_mechanic_intro`.

**08 §4.4 y §6.2 · Entrega**
- Si las mecánicas nuevas llegan después de la 1.0: build nuevo, `level_overrides` o vuelta nueva, y la regla de migración de Q8.

**09 · Roadmap**
- Alcance por versión (Q8).

**Herramientas**
- `schemas/af-level-1.schema.json`, `tools/validate_schema.py`, `tools/level-qa` y `tools/level-generator`.
- `tools/econ_sim.py`: `CHALLENGE_POSITIONS` y los retos etiquetados de 41-59. Hay que regenerar `tools/out/econ_*.md` (contrato 18).

**Núcleo (más adelante, con su propia tarea)**
- `AF.Core`:
  - `BoardState` tiene que aceptar el color 15;
  - las mecánicas van en los bits libres de `Cuia.Packed` (30-31, 35 y 39-63);
  - las reglas de cuia necesitan contexto de tablero;
  - R10 y `futil` según el anexo B.
- `AF.Solver`: el estado extendido.

---

## (h) Preguntas cerradas para el dueño

| # | Pregunta | Opciones | Recomendación | Qué toca |
|---|---|---|---|---|
| Q1 | ¿Reordenamos el ciclo de 20 para que tras cada nivel con etiqueta venga uno más fácil? | A: retos en p6, p9, p12, p15 y p18, con remanso en p19. B: canon (p6, p10, p14, p17, p19) | **A**: cumple el «escalonado» que pediste y copia el 387 → 388 → 389 de MS | 03 §11-§12, 02 §9-§10, `econ_sim.py`; contrato 1 (cadencia: GDD.md §7.2 #20) |
| Q2 | ¿Mostramos la etiqueta en el botón «Jugar» con un chip y una cinta «×3»/«×4», y los iconos de las mecánicas en la tarjeta previa? | Sí / No | **Sí** | 02 §10, 07 §7.7, 06 §4.7 |
| Q3 | ¿Premios de 20, 60 y 80 («×3» y «×4») o Muy difícil a 100 («×5», como MS)? | A: 20/60/80. B: 20/60/100 tras rehacer `econ_sim` | **A** por ahora; B solo con la simulación nueva | 05, 08 §9, contrato 18 |
| Q4 | ¿Subtítulo del Difícil? | A: «Aguas bravas» (canon). B: «Repiquete» | **A**: «Repiquete» es un regionalismo poco conocido; queda como nombre interno | 00 §6.1 |
| Q5 | ¿Calendario de debuts 1-200? | A (mínima): canon intacto y Matapi en el 163. B: Matapi en el 63 y Brote en el 163 | **B**, con reservas [E]: el argumento «sigue el orden de MS» depende de la regla del frasco «?», que está sin confirmar (b.2 M10). Hay que observarla en el dispositivo (L1) antes de decidir. El Matapi debuta en un mundo brasileño (Manaos), aunque su base es del Bajo Amazonas (Santarém). La opción A exige intercambiar Ma y Br en todas las columnas 61-200 (d.4) | Contrato 1, 02 §9.1, 03 §9/§11, 08 §9, glosario §6.5 |
| Q6 | ¿La Liana debuta con fruta (como la tela de MS) y la de número pasa a variante en el 243? | Sí / No | **Sí**: es el gancho «cada sellado destapa algo» y se lee por el icono | 02 §9.1, 03 §9, esquema |
| Q7 | ¿Entran en la campaña las mecánicas nuevas Aguapé (223), Palizada (263), Lancha (303) y Cocha (583), con sus variantes? | Sí / Solo algunas / No | **Sí**, con kill-switch y respaldo | 02 §9, 03 §9, 08 §4.1/§9 |
| Q8 | ¿Alcance de la 1.0, con 700 niveles instalados? | (a) Todas las mecánicas en la 1.0. (b′) La 1.0 lleva las mecánicas hasta el 200 (canon + Matapi). La 1.1 es un **build**, porque las mecánicas nuevas exigen código en `AF.Core` y `pack_schema` 2; sustituye niveles de 201-700 con `level_overrides` o con una vuelta nueva (08 §4.4). Regla de migración: si el jugador ya pasó el debut de una mecánica, se le sirve una vez su tablero de debut antes de usarla (03 §9, consecuencia 3). (c) La 1.0 cubre 201-700 con variantes de coste S (Liana de cuenta, Hoja atada, Brote de dos hojas, Flor) y combinaciones de lo que ya trae; Aguapé, Palizada, Lancha y Cocha llegan como en (b′) | **(c) junto con (b′)** [P]: menos riesgo para el solver y el esquema, y sigue habiendo novedad en 201-700. La (b) anterior («las nuevas llegan en la 1.1 por `levels_remote`») no es posible: los niveles 1-700 van instalados en el AAB (08 §6.2), un paquete nuevo nunca cambia un nivel publicado y el cliente rechaza un `pack_schema` mayor (08 §4.4). Con (b′) sola, entre el 200 y la 1.1 no aparece nada nuevo: no se cumple d.5, regla 5, ni tu pedido de «buscar también nuevas cada vez que avanza». Con (c) hay que rehacer la tabla de d.5 | 09, 08 §4.4, §6.2, d.5 |
| Q9 | El canon ya estrena Aguas turbias en el hito 100 con 1 cuia turbia y 1 Luciérnaga regalada (04 §2.5; 03 §9). ¿Añadimos que sea lo único especial del nivel y que la cuia tenga 2 dosis ocultas? | Sí / No (canon: el Muy difícil lleva «2 mecánicas activas», 02 §10) | **Sí**: un debut con la información oculta mínima. Exige una excepción a 02 §10 | 02 §10-§11 |
| Q10 | ¿Dejamos fuera de la campaña los contadores a favor del jugador (nido, defeso, piracema, revelado rítmico) y los llevamos a Travesías? | Sí / No | **Sí**: los vertidos fútiles los gastan sin pensar | 06 |
| Q11 | ¿Las temporadas (aguas altas y bajas) se ven solo en el fondo o también con un cartel? | A: solo la LUT del fondo. B: con un cartel corto en p1 | **A**: un cartel más compite con «El río bajó» | 07 §2 |
| Q12 | ¿Manatí en el elenco (se come el Aguapé) y Rosa como llave de la Palizada? | Sí / No | **No por ahora**: la corriente se lleva el aguapé y la llave es un pez genérico. Revisar con el arte | Glosario §3 |
| Q13 | ¿Corregimos la regla de fútiles (02 §1.4 y su copia en 04 §5.1) y el orden de R10 antes de programar mecánicas? | Sí / No | **Sí** (bloqueante): hoy el juego podría anunciar «No hay más vertidos» con un vertido útil hacia una Hoja (rompe R14). El arreglo exime solo a los destinos que cambian al recibir y a los orígenes que ya no pueden recibir (anexo B) | 02 §1.4, R10 y R16; 04 §5.1 (tests 1-4); `AF.Core`; tests de propiedad |
| Q14 | ¿Qué es una «mecánica con disparador» en 03 §8 (la que no puede ir con Aguas turbias)? | A: solo las de liberación y los contadores; los filtros (Hoja, Brote, Matapi, Barro) sí pueden. B: todas | **A** (*nota del 2026-10-14: D-019, Q-Ti1-A, hace una excepción solo para la Tinaja (M13) de `propuesta-tinaja.md`: para ella, lo prohibido es «un disparador que depende de lo oculto», porque su suelta solo depende del recuento visible; también se exceptúa de T1, T3 y Q17 A*) | 03 §8, 02 §11 |
| Q15 | ¿Encargamos la skin Cuia de Santarém a ASARISAN, con pago y crédito? | Sí / Grabado propio | **Sí**, si el asesor y el presupuesto lo permiten | 07 §2.2, 09 §5 |
| Q16 | ¿`tools/level-qa` mide las trampas tempranas con tope por etiqueta (pregunta 5.5)? | A / B / C de la propuesta | **B** | 03 §15/§17 |
| Q17 | ¿Tapamos el contenido de la Liana y de la Palizada dejando ver solo la dosis del fondo, como la tela de MS (opción C de la pregunta 5.4; propuesta de MS, pregunta 10.2 #15, abierta)? | A: no, el contenido se ve. C: sí, desde su debut | **A por ahora.** La C es información oculta desde el 83 en niveles Normales. Choca con T1 (solo Difícil o Muy difícil desde el 100) y con 03 §8 («nunca en niveles con mecánicas con disparador»). Con T2 (≤ 4 dosis ocultas, ≤ 24 realidades): dos lianas con 3 dosis tapadas suman 6 ocultas, 720 realidades, el límite que el canon descartó. Con T5: Morpho solo propone vertidos seguros y tendría que enumerar. La verificación del rescate usa la realidad verdadera (04 §6.4), pero hay que confirmarlo. Si se aprueba: fijar cómo se marca lo tapado en el esquema y que cuente para `lvl_murky_max_hidden_units`, o justificar por qué no | 02 §11 (T1, T2, T5), 03 §8, 04 §6.4, esquema (contrato 21) |
| Q18 | ¿Qué pasa con los vertidos fútiles mientras quedan gotas de Lluvia o la Piraña está activa? Hoy cada vertido del jugador avanza el contador (02 §9.1 M6-M7), así que un vertido fútil cambia el estado canónico y puede hacer caer una gota que sella. R16 deja de valer y el detector podría declarar bloqueo con progreso posible | A: avanzan el contador (canon); el detector cuenta como útil el vertido que adelanta una gota o un mordisco. B: no avanzan el contador; el contador visible solo baja con vertidos útiles | **B** [P]: mantiene R16 (un vertido fútil no cambia el estado) y el bloqueo se detecta igual que sin mecánicas. A deja la Piraña sin bloqueo posible, porque nunca se acaba. Hay que probar si un contador que «no baja» confunde | 02 §1.4, R16, §9.1 M6-M7; 03 §9; 04 §5.1 |

---

## (i) Lista para el asesor cultural

**Nombres y léxico** (lista permitida de 07 §8.1):
1. Matapi, para la mecánica y el Dato del río. ¿Hay un nombre local en Loreto para la trampa de camarón?
2. Palizada en ES: es un regionalismo [E]; ¿se entiende fuera de la Amazonía peruana?, ¿se lee como «empalizada» o «paliza»? Probarlo con jugadores de varios países; alternativa «Troncos». Su nombre en PT (¿«paliteiro»?, ¿«toras»?, ¿otro?).
3. Aguapé y su nombre en cada país (lechuguín, camalote, tarope, buchón; «huama» no tiene fuente). En Brasil hay varios: aguapé, mururé, baronesa… [V F111].
4. «Rebojo» como nombre PT del Remolino.
5. Cocha y tipishca.
6. Lancha en ES y «Recreio» en PT: ¿se lee como recreo escolar fuera del Norte? Probarlo con jugadores brasileños; alternativas «Barco» o «Lancha».
7. «Palha» como PT de la Hoja.
8. «Hoja atada», o un nombre que no se confunda con la Hoja.
9. Flor de vitória-régia (sin la leyenda; ¿«Atun Sisac» se descarta?).
10. Casiquiare como nombre de la variante.
11. Solo para eventos: piracema, defeso, banzeiro, tipiti, mijano y repiquete (nombre interno).
12. «Charapa»: en el Perú también es el gentilicio coloquial de la gente del oriente [V F155]. En nombres visibles, «tortuga» o «taricaya»; «charapa» solo en Datos del río con contexto.
13. Nombres de hitos [P]: «Puerto de Manaos» (80), «Remolinos del Trapecio» (140), «Creciente del Itaya» (160) y «Aguas del Orinoco» (960). Y el uso del tinte Agua del Tapajós en el Encuentro de las Aguas, que es Negro + Solimões: es un problema del canon (glosario §7.2).
14. «Matupá»: también significa «terra caída» [V F157]; no se usa en la interfaz.

**Fauna y flora:**
- el aguapé sin tratarlo como plaga;
- el manatí, si entra;
- el pez genérico de la llave;
- las tortugas charapa y taricaya (fechas y crédito; «charapa» solo con contexto);
- el paiche contado con sus dos caras por país;
- la piraña sin sangre;
- la gamitana;
- la vitória-régia;
- lianas no rituales;
- la especie del bijao;
- la sumaúma o lupuna: árbol sagrado, ligado a la ayahuasca en el Perú [V F153, F154]; se propone vetarla en 07 §8.1;
- el cipó-ambé [NV] y la leyenda del matupá (Anaconda Gigante) [V F49].

**Lugares y pueblos en los Datos del río:**
- todos los de la sección (e);
- la sentencia del Marañón: mejor con consulta a la Federación Huaynakana Kamatahuara Kana;
- el papel de las comunidades en Tarapoto [NV];
- Belén y Afuá, sin pobreza pintoresca y nunca como nombre de nivel;
- el ritual de la guayusa: la fuente describe a los Achuar; no atribuirlo a otro pueblo sin confirmarlo;
- la Palizada no tiene base ecuatoriana y debuta en el mundo 6 (Ecuador): ¿hay una base local o se mueve el debut?;
- Puerto Nariño, por los pueblos que lo habitan;
- la Casa de Fierro, sin el mito de Eiffel y sin afirmar quién la compró;
- Riberalta e Iquitos.

**Objetos y oficios:**
- un matapi genérico, sin copiar un taller;
- las cuias de Santarém: encargo, pago, crédito y consentimiento sobre los grafismos;
- el tipiti y el tacacá (origen indígena; oficio de mujeres);
- la crisneja, la chambira y el huayruro.

**Fiestas:** la Festa do Açaí de Codajás y la del Cupuaçu (¿se pueden citar sin licencia?, también para el abogado); el Arraial do Pavulagem (raíces de São João); el Çairé y el Círio quedan fuera.

**Revisión del arte frente al trade dress de MS (07 §1.3):** los troncos no deben parecer el armario de tablones con marco naranja; la liana, la tela rosa; ni el aguapé, el bloque de hielo.

**Vetos nuevos para 07 §8.1:** la lista completa está en investigacion §7.

**Verificaciones pendientes antes de escribir cualquier Dato del río:** investigacion §8.

---

## Anexo A. Lo que encontraron los jueces y cómo se resolvió

| Problema | Resolución en esta síntesis |
|---|---|
| Aguas turbias estrenaba en el 106, un reto, contra la propia regla «nada se estrena en un reto», y en el ciclo del debut del Aguapé | Debut en el hito 100, como en el canon, sin otra mecánica (excepción a 02 §10, Q9); el Aguapé pasa al 223 |
| «Nunca dos etiquetados seguidos» chocaba con p19 (reto) seguido de p20 (hito) | Ciclo reordenado: p19 es remanso |
| El mínimo de 30 % de niveles puros no cabía en el reparto | Garantizado por construcción: p1, p2, p19 y al menos 3 comodidades puras |
| En el debut, la titular salía en 8 niveles, por encima del tope de 6 | Tope explícito de 8 en el ciclo de debut y 6 en los demás |
| El Nido (y el Defeso) se podían gastar con vertidos fútiles | Fuera de la campaña (b.4); Lancha y Cocha no se pueden explotar |
| «Cuia del Encuentro» ya era una skin y el nombre de un evento | La mecánica de dos carriles queda solo para el evento, con otro nombre |
| La «Regla del río» y la curva del Mapa repetían la barra de la Crecida | Fuera. La señal de dificultad es la luz del fondo (noche en Muy difícil), no la altura del agua (c.5) |
| La Hoja copiaba la forma más débil de la botella atada | Variante Hoja atada (283) |
| Aguapé: «no es origen» no estaba observado en MS | Marcado [P]/[E] en la ficha |
| Piracema era ambigua; Banzeiro y «Aguapé que crece» dependían de la posición | Piracema y Banzeiro, fuera; el Aguapé que crece usa una lista fija en los datos |
| «Atun sisac» sin revisión de uso regional | Fuera de la mecánica; pendiente del asesor |
| Matapi: «selectiva» estaba mal citado | Corregido: no se presenta como práctica sostenible |
| La firma «Igapó» (Liana + Aguas turbias) era un error ecológico y violaba 03 §8 | Eliminada; Aguas turbias solo con filtros |
| Términos brasileños en mundos hispanohablantes | El Matapi debuta en Manaos; piracema, defeso, banzeiro y tipiti van a eventos |
| Tipiti con Taperebá no era exacto | Fuera de la campaña; evento «Casa de farinha» |
| Cifras de las charapas (1996) sin crédito a las comunidades | El Dato del río lleva la fecha y el crédito |
| Tarapoto: las comunidades solo aparecen como comerciantes | Marcado [NV] |
| El paiche en Bolivia, contado solo como problema | Dato del río con las dos caras |
| PT «Palma» y «Paliteiro» | «Palha» [P]; el PT de la Palizada, pendiente |
| Liberación en R10 antes del sellado | R10 reordenado (anexo B) |
| Regla de fútiles mal resuelta con Matapi, Aguapé y Hoja | Arreglo (anexo B, Q13) |
| Lechuguín atribuido a Ecuador sin fuente | [NV], pendiente del asesor |
| Llave como «pez de color» difícil de leer a 39-47 dp | Silueta blanca de pez y nudo con el icono de la fruta |
| Alcance de la 1.0 subestimado | Pregunta Q8, con la recomendación (c) junto con (b′); la (b) original no era posible con 08 §4.4 |
| El registro de combinaciones rompía la reproducibilidad | Registro versionado como entrada del pipeline |

## Anexo B. R10 y `futil` propuestos (02 §1.2-§1.4)

**R10 propuesto.** Tras cada vertido se evalúa en este orden:
- (a) revelado de la nueva cima de A (R7);
- (b) aperturas por recepción en B: Hoja (primer vertido de c) y Brote (vertido de su color; en el «de dos hojas», resta las dosis recibidas);
- (c) sellado de A y de B (R5);
- (d) liberaciones por sellado: Liana (de cuenta y con fruta), Flor y Aguapé;
- (e) contadores: Lluvia, Piraña, Cocha y Aguapé que crece. Avanzan **una sola vez** por vertido;
- (f) condiciones del tablero: dosis-llave de una Palizada en la cima (también tras un mordisco) y atraque de la Lancha (vacías ≥ v);
- (f′) **se repiten (c), (d) y (f) hasta que nada cambie** (punto fijo). Una gota de Lluvia puede completar la cuia de agua, y un mordisco puede dejar una llave en la cima: lo que eso selle o libere ocurre en el mismo vertido, en el juego y en el solver;
- (g) victoria (R12);
- (h) bloqueo (R13-R15).

Es contrato con el solver y con Marea: Marea restaura en orden inverso. Test obligatorio: «una gota que sella libera la Liana con fruta (o la Flor, o el Aguapé) en el mismo vertido».

**`futil` propuesto:**

```
futil(A, B):
  si cambia_al_recibir(B): false                      # nuevo; sustituye a «B in {lluvia, remolino}»
    # Hoja puesta, Hoja atada, Brote cerrado, Matapi, cuia bajo un Aguapé cerrado,
    # Cocha abierta, Lluvia con gotas_pendientes > 0, Remolino
  si no_puede_recibir(A): false                        # nuevo: Cocha cerrada
  si tiene_ocultas(A): false
  si not (monocolor(A) and not sellada(A) and vacia(B)): false
  c = color_top(A)
  si cap_A != U_c and cap_B == U_c: false               # casa correcta (sin cambios)
  return true
```

- **Por qué hace falta:** hoy, verter el resto de una fruta en su Hoja vacía es «fútil». Pero convierte un vacío reservado en una cuia general libre: es progreso. El solver lo poda, y R13 puede declarar Aguas estancadas habiendo un vertido útil, lo que rompe R14. Pasa lo mismo con la Hoja atada, el Matapi, el Aguapé y una Cocha cerrada.
- **Por qué solo destinos (y la Cocha cerrada como origen):** eximir a cualquier origen con mecánica rompe la detección. Con una cuia de Lluvia sin gotas pendientes y una vacía, el ir y venir monocolor entre las dos contaría como útil en los dos sentidos: el jugador quedaría atascado sin que saliera nunca el modal Aguas estancadas, donde está el embudo de D-008. El canon lo detecta tras un vertido. Por eso la Lluvia solo exime como destino y mientras le queden gotas, y el Remolino solo como destino, como en el canon.
- **Copia en 04 §5.1:** hay que cambiarla a la vez, con sus tests de regresión 1-4. El test 4 pasa a «monocolor → vacía bajo Lluvia con gotas pendientes, o conectada a Remolino, no es fútil».
- **Pendiente:** qué pasa con los vertidos fútiles mientras quedan gotas o la Piraña está activa (Q18).
- **Coste:** en algunos niveles con mecánicas habrá más vertidos que cuentan como útiles, así que el modal de bloqueo puede tardar un poco más. Se acepta: R14 manda.
- **Pruebas:**
  - tests de propiedad sobre `atascado(S)` con cada mecánica, en los modos `run` y `single`, antes de su debut (02 R16);
  - un test de propiedad nuevo: no existe un ciclo de vertidos «útiles» que vuelva al mismo estado canónico.
