# La tinaja de la casa: propuesta de mecánica propia (M13)

> **Documento de apoyo, no canon.** Todo es propuesta [P] hasta que el dueño decida. Si algo de aquí contradice el GDD, manda el GDD hasta que se edite. La entrada de registro sería **D-011** [P].
>
> Todo lo que toca objetos, lugares, bebidas y nombres queda **pendiente de revisión del asesor cultural** (07 §8.2).
>
> Las fuentes [T#] (cultura) y [M#] (mercado) están en el anexo 2. Las consultaron los agentes de investigación el 2026-10-10; el contenido web se trató como dato. Marcas: [V] verificado, [E] estimado, [NV] sin verificar, [P] propuesta, [V-proto] medido con un prototipo.
>
> **PM** remite a `propuesta-mecanicas-dificultad.md` (D-007 y D-009), en esta carpeta: «PM d.2.7» es su sección d.2, regla 7. Sin prefijo, las secciones (a)-(h) son de este documento.
>
> Rama `integracion-fase1-fase2`. No se tocó código. Los prototipos son copias del núcleo en el scratchpad de la sesión (`tinaja_v1`, `tinaja2`, `v3`; para la revisión, `verif_tinaja`, `verif_tinaja2`, `refute_c9`, `refute_tinaja_prune`, `refute_morpho` y el modelo de reglas en Python `tinaja_check`); no están en el repositorio.
>
> **Revisión del 2026-10-10.** Esta versión aplica los hallazgos verificados de la revisión: la trama pasa a «solo agua» y el verbo a «sacar», el cordón se sustituye por el jirau compartido, se dice «dosis» y no «porción», Morpho responde en un orden fijo, el cierre seguro se sustituye por paseos, la tabla de c.9 se rehízo con el generador de c.3, y se añaden la poda de fútiles con capacidades mixtas, el espionaje con Volver a la orilla (Q-Ti6), la excepción a Q14 A y el inventario de lo que lee «hay ocultos». El registro está en el anexo 3.

**De dónde sale.** Idea del dueño del 2026-10-10: «un recipiente grande de barro que no permite ver qué mezcla tiene y puedo utilizarlo solo con un recipiente pequeño de dos porciones e irlos jugando; que cuadre la trama mejor y el algoritmo para que case la idea, y darle más vida y que tenga sentido».

En el juego, las «porciones» del dueño son **dosis**: es la palabra que el GDD ya usa en la interfaz para esta misma pieza (02 §8: la Cuia chica «solo guarda dos dosis»).

**Cómo se hizo.**
- Dos investigaciones: la cultural (la cuia como cazo de la casa, vasijas de agua, vetos) y la de mercado (qué existe en el género).
- Un estudio de viabilidad sobre el núcleo, con prototipos medidos.
- Tres variantes y tres jueces:

| Variante | Juez 1 | Juez 2 | Juez 3 | Total |
|---|---|---|---|---|
| **V1 · Tinaja y su cuia chica** (tinaja que nunca recibe; cuia propia que recibe dos dosis de cualquier fruta y se suelta) | 7 | 8 | 8 | **23** |
| V3 · Tinaja de la casa (variante de Aguas turbias que se saca con la Cuia chica normal) | 8 | 7 | 7 | 22 |
| V2 · Tinaja grande de 8-12 dosis contadas en bloques de 2 | 5 | 4 | 6 | 15 |

- Se parte de **V1** y se le injertan las mejores ideas de V3 y V2. El anexo 1 dice qué encontró cada juez y cómo se resolvió.
- Después, una revisión con verificación independiente de cada hallazgo (anexo 3).

---

## (a) Resumen para el dueño en 10 puntos

1. **Qué es.** Una **tinaja** de barro con 3-5 dosis de fruta. Solo deja ver la de la boca y un número, «×n», con las que quedan dentro. Solo se saca con **su cuia chica**, que cuelga a su lado en el mismo jirau: le caben **dos dosis**, que después se juegan como cualquier cuia.
   - Es «grande» en alto, como la Cuia alta de 5, no en dosis: con más de 4 ocultas ya no se puede garantizar que se gane sin adivinar (24 realidades como máximo). La tinaja de 8-10 dosis queda como variante posterior (d.4).
2. **Cómo se juega, en 2 líneas.** Toca la tinaja y luego su cuia chica: sale la fruta de la boca y asoma la de debajo, que nunca es igual. Lo sacado no vuelve a la tinaja; cuando la vacías, la cuia chica baja del jirau y queda libre para el resto del nivel.
3. **Por qué es nuestra.** El género ya tiene dosis ocultas, botellas grandes y búferes pequeños. **No encontramos ningún water-sort con un recipiente al que solo se accede con su propio cazo** [NV: ausencia de evidencia, M8]. Además, el gesto es nuevo: la tinaja no se inclina; es la cuia chica la que viaja, entra por la boca y sale llena (gesto pendiente del asesor, (h)). En Magic Sort (MS) siempre se inclina la botella llena.
4. **Por qué es justa.** Lo oculto es la queja n.º 1 contra MS («es suerte») [V M1, M2]. Aquí:
   - nunca mueves una dosis que no has visto;
   - siempre sabes cuántas quedan;
   - como mucho hay 4 ocultas (24 realidades);
   - el generador garantiza un camino sin adivinar (ruta segura, 03 §8). Además juega paseos seguros hasta la victoria y paseos casuales que comprueban que nunca hace falta adivinar (c.3). Es un filtro, no una prueba;
   - Morpho solo da vertidos seguros. Si desde ahí no hay salida, lo dice; si falta información, sugiere la Luciérnaga sin prometer nada; y nunca cobra sin dar un vertido (b.2).
5. **Cómo cuadra la trama.** «La casa guarda, el río esconde».
   - En la casa ribereña, la tinaja guarda **el agua de la casa**, de la lluvia o del río, como el panelão de Quatipuru-Mirim [V T4]. El barro sin esmaltar es opaco, y por eso no se ve dentro; además enfría el agua por evaporación [V T7, dato general del botijo].
   - El color es el tinte de los miquitos, como en cualquier cuia: con «Llenar y teñir» (D-010), el río llena la tinaja por la boca y los miquitos solo tiñen la boca.
   - La cuia vuelve a su oficio real: recoger y verter agua [V T1, T2].
   - Es el espejo del Matapi: lo que entra al matapi no sale; lo que sale de la tinaja no vuelve.
   - Nunca guarda refrescos, jugos ni bebidas, y nadie recibe lo que se saca: tinaja, bebida guardada y cuia que saca es la escena del masato en la Amazonía peruana [V T20, T10, T11] (b.2, Cultura).
6. **Vida.** La tinaja pesa: al tocarla apenas sube y se asienta. Suda gotitas que pillan la luz. Al sacar agua, la cuia chica se hunde en la boca y sale goteando. Al vaciarse, la tinaja se tumba boca abajo a secar y su cuia baja del jirau y queda libre. Es un desbloqueo con su golpe, como en MS.
7. **Algoritmo.** Reutiliza casi todo lo que ya tenemos:
   - Aguas turbias: máscara de ocultos, realidades, ruta segura y Morpho;
   - la silueta de la Cuia alta de 5;
   - la Cuia chica de 2.

   Lo nuevo en el núcleo es pequeño: 2 tipos de mecánica, **un gancho de origen** en `BoardRules.Accepts` y unas 120 líneas de reglas. Los packs 11-200 no cambian ni un byte. Pero reutilizar el campo de ocultos obliga a revisar todo lo que hoy lee «hay ocultos» como «Aguas turbias»: level-qa, el informe del generador y unos 15 scripts de Unity (c.5 y c.6). Y en los niveles con tinaja el solver tiene que buscar sin podar los fútiles (b.2, R14).
8. **Cuándo.** Estreno en el **323** (p3 del ciclo 321-340, aguas bajas, mundo 7 Madre de Dios). Recomiendo que entre en la **1.0**, porque los niveles 201-700 aún no están generados, **siempre que** Morpho en el cliente (con corte por tiempo) y la Luciérnaga estén listos. Si no, llega en la 1.1 sin tocar nada más (Q-Ti3).
9. **Cuánto cuesta [E].** 16-23 días de agente en 3 carriles, unas 3 semanas de calendario, más el arte del artista y la revisión del asesor. No incluye las piezas que comparte con Aguas turbias y que aún no existen en el cliente (Morpho, pista por inactividad, Luciérnaga, Pororoca, tarjeta y dedo guía; c.7). El trabajo de C# no necesita Unity: puede empezar ya, en paralelo con la integración (paso 1 del traspaso). No toca la economía ni ninguna cifra [sim].
10. **Qué necesito de ti:** 6 preguntas cerradas y 1 que solo se hace si el asesor ve riesgo (g). La decisiva es Q-Ti1: aceptar contenido oculto en niveles Normales, con tope, como excepción a T1, T3, Q17 A y Q14 A.

---

## (b) La propuesta recomendada

### b.0 Qué se toma de cada variante

| Pieza | De dónde | Por qué |
|---|---|---|
| Tinaja que nunca recibe y solo vierte en su cuia; la cuia propia recibe dos dosis de cualquier fruta y se suelta al vaciar la tinaja | V1 | Es la idea literal del dueño. Con la regla de color normal y T4, a la cuia chica nunca le cabría más de una dosis de la tinaja, y las «dos porciones» desaparecerían (Q-Ti2) |
| Capacidad 5 con la silueta de la Cuia alta de 5 (6,60 u) | V3 (jueces 1 y 2) | La 6 (7,60 u) encoge el tablero sin cambiar ninguna regla: la tinaja nunca recibe |
| Códigos 8 y 9, sin `partner` en el JSON | V3 (juez 2) | Hay una sola tinaja por nivel: cada pieza reconoce a la otra por su tipo |
| Pororoca según el canon: revela la tinaja y la deja fija | V3 (jueces 1 y 2) | T6 y 04 §2.4 (a) y (b), con la fila que falta en 02 §9.2 y 03 §9 (b.2) |
| Bot: solo la rama mínima (+16 «juntar» solo si la fruta coincide) | Juez 2 | La puntuación de 03 §6 sigue congelada para todo lo demás |
| Tope de ocultas por tipo de nivel; mensaje honesto de Morpho; fórmula de vacíos; color medido | V1 | Justicia medible. El cierre seguro de V1 se sustituye por paseos (c.3) |
| Criterio de estreno «S0 sin trampas» | V3 | Los tableros de debut no esconden ninguna jugada que pierda |
| Textos sin la palabra «barro» | V3 | «Barro» es la mecánica M5 |
| «La casa guarda, el río esconde» y el espejo del Matapi | V3 | Trama |
| La tinaja que pesa, el sudor del barro y el secado | V2 | Vida |
| Temporada B (aguas bajas) | V2 | La casa guarda agua cuando escasea [V T4] |
| Compuerta de coste de Morpho en level-qa | V2 | Presupuesto del teléfono |
| Receta visible (qué frutas, sin orden) solo como interruptor apagado | V1 y V2 | Por defecto, la idea literal del dueño |
| Solo agua de la casa, nunca bebidas ni «días»; sin burbujas | Juez 3 y revisión | Nada que evoque fermentación ni la escena del masato |
| Jirau compartido en lugar de cordón | Revisión | El cordón que se desata es la imagen de la Liana (M3) |

### b.1 Filtro PM b.0 aplicado

1. **Función probada:** las piezas sueltas lo están; la combinación es nuestra.
   - Capas ocultas: MS 387 y todo el género [V M1].
   - Dispensador que suelta de uno en uno: el túnel de Bus Jam. Rollic añadió además un túnel de vidrio para que se pudiera planificar [V M3].
   - Búfer pequeño: nuestra Cuia chica y el «shuttle» de MS 14 [E].
2. **Regla exacta y compatible** con el vertido por tramo (D-005), D-002, A* y la generación por superposición. Ver b.2 y (c).
3. **Ningún contador a favor del jugador.** Sacar no acerca ningún premio: solo vacía la tinaja, que nunca vuelve a llenarse.
4. **Se lee en un segundo:** vasija de barro, boca de color, «×n» y su cuia colgada al lado en el mismo jirau. Tutorial de 2 líneas.
5. **Desbloqueo con animación y sonido:** el revelado de cada boca y la suelta de la cuia.
6. **Nada sagrado ni personas:** vasija doméstica lisa y un cazo que se mueve solo. Nadie bebe ni recibe lo que se saca.
7. **«El contenido se ve» (Q17 A): excepción.** La decide el dueño en Q-Ti1, junto con las de T1, T3 y Q14 A.

### b.2 Ficha · M13 Tinaja [P]

**Nombre** (todo pendiente del asesor; ver Q-Ti4 y (h)):

| | ES | PT | EN | Clave interna |
|---|---|---|---|---|
| La vasija | **Tinaja** | **Talha** (alternativa: «Pote d'água») | **Water Jar** | `tinaja` · `MechanicType.Tinaja` = 8 |
| Su cuia | **su cuia chica** | **cuia pequena** | **small cuia** | `cucharon` · `MechanicType.Cucharon` = 9 |

- «Clay Jar» (EN) y «Pote de Barro» (PT) ya son la alcancía (glosario §7.1).
- Los identificadores internos no llevan la marca (CLAUDE.md).
- En textos, fichas y eventos nunca «atar», «desatar» ni «atada»: son los verbos de la Liana (00 §6.5; 02 §9.1 M3).

**Función.** Un recipiente que guarda fruta oculta y la suelta de una en una, solo a través de su cazo.
- **Lo propio:** el acoplamiento exclusivo y que lo sacado no vuelva.
- **La decisión de juego:** cuándo sacar y adónde llevar lo sacado.
- **El dilema de las dos dosis:** sacar dos veces deja ver la fruta de más abajo, pero entierra la primera dosis bajo la segunda. Es un dilema **dentro de un intento**: como lo revelado sigue revelado tras Volver a la orilla, quien reinicia ya lo ha visto (ver «Marea y Volver a la orilla» y Q-Ti6).

**Regla exacta** (`attrs`: `{"type":"tinaja"}` en la tinaja y `{"type":"cucharon"}` en su cuia; `pack_schema` 3):

1. **Piezas.**
   - **Tinaja:** cuia de capacidad 5 que empieza con 3-5 dosis. Puede empezar llena, porque nunca recibe ni sella.
     - Todas sus dosis están ocultas salvo la de la cima (la boca, R7). El nivel lleva `murky: true` y `hidden` = [[i,0] … [i,n−2]]. Reutiliza los campos de Aguas turbias **sin cambiar su formato, pero sí su significado**: todo lo que hoy lee «hay ocultos» como «Aguas turbias» tiene que distinguir la tinaja (inventario en c.5 y c.6).
     - **T4 en toda la columna:** dos dosis vecinas nunca son de la misma fruta.
     - Sin Barro (15) dentro.
     - No es casa de ninguna fruta: no cuenta en la regla de 02 §8 «cada Cuia alta llega con su fruta abundante», ni en ninguna de las dos comparaciones de capacidades de `LevelValidation` (c.1).
   - **Su cuia:** Cuia chica de capacidad 2, vacía al empezar, en la **ranura contigua de la misma fila**. Las dos comparten un jirau y su cuia cuelga de él, junto a la tinaja (solo arte; ver Aspecto).
   - **Una sola tinaja por nivel**, y es lo único oculto del nivel: como mucho 4 ocultas, ≤ 24 realidades (4! = 24), sin cambiar el contrato 8. El tope se valida con una constante propia de la tinaja [P] (4, el límite de `RealityEnumerator`), no con la clave de Remote Config `lvl_murky_max_hidden_units`: si algún día se baja el tope de Aguas turbias (rango 2-4, 08 §9), no debe rechazar tinajas ya publicadas.
2. **La tinaja nunca recibe** (`bloqueada_destino` permanente). Tampoco desde su cuia. Es un rechazo sin texto (R11).
3. **La tinaja solo vierte en su cuia.** Es un filtro de origen nuevo, `solo_vierte_en(A)`. Tinaja → otra cuia es ilegal (R11), y en la interfaz se aplican C5 y C6 de 02 §3.1:
   - si esa otra cuia puede ser origen (también su cuia cuando está llena), la selección pasa a ella (`tap_switch_selection` = true);
   - si no puede serlo, la tinaja baja y esa cuia se sacude.
4. **Cantidad:** `min(racha visible, espacio)` = 1 siempre, en `run` y en `single`. Por T4 la racha de la boca es 1, también después de una Luciérnaga. **Nunca sale una dosis que no hayas visto.**
5. **Su cuia solo recibe de su tinaja, de cualquier fruta, mientras tenga hueco.** Son dos dosis en capas: no se mezclan, como en cualquier cuia. Lo último que sacas es lo primero que juegas. Desde cualquier otra cuia es ilegal (R11).
6. **Su cuia, como origen, sigue la regla normal:** vierte su cima en cualquier destino legal, nunca en la tinaja. No sella nunca (R5: cap 2 < U_c).
7. **Revelado:** al salir la boca, la dosis de debajo queda visible para el resto del intento, también tras Marea y Volver a la orilla (R7, T6, I5; ver Q-Ti6). Usa los eventos de Aguas turbias: `SourceRevealed` y `RevealedMask`.
8. **Suelta.** En cuanto la tinaja queda vacía, su cuia baja del jirau y pasa a ser una Cuia chica normal (principio de R16: la mecánica que se suelta deja la cuia normal).
   - La suelta depende solo del recuento visible («×n» llega a 0), nunca del color de una oculta: ocurre en el mismo vertido en todas las realidades.
   - La tinaja vacía queda **inerte** en su ranura, boca abajo: no es origen (está vacía) ni destino (regla 2), y cuenta como vacía para R12.
9. **Victoria (R12, sin cambios):** la tinaja tiene que acabar vacía y su cuia también, porque nunca sella.

**`legal` (02 §1.2), con los cambios marcados:**

```
legal(A, B):
  si A == B: false
  si vacia(A) o sellada(A) o bloqueada_origen(A): false
  si tinaja(A) y no cuia_de_tinaja(B): false            # nuevo: solo vierte en su cuia (va antes que cualquier filtro de B)
  si sellada(B) o bloqueada_destino(B): false            # tinaja(B) siempre está bloqueada como destino
  si cuia_de_tinaja(B): return tinaja(A) y no llena(B)   # nuevo: recibe solo de su tinaja, cualquier fruta
  ... (igual que hoy)
```

`cuia_de_tinaja(B)` vale solo mientras la tinaja tenga dosis (antes de la suelta). Después, B es una Cuia chica normal. El filtro de origen tiene que evaluarse **antes** que la mecánica del destino y que el Barro expuesto (c.1): si no, la tinaja podría verter en una Hoja vacía de la fruta de su boca o en una cuia con Barro expuesto.

**Ejemplo de nivel** (ilustrativo; no lo ha verificado el pipeline):

```json
"caps":  [4, 4, 4, 4, 4, 4, 4, 4, 5, 2],
"cuias": [[1,4,10,6], [10,3,1,4], [4,8,10,1], [6,1,8], [3,10,4], [8,6,3], [], [], [3,8,6], []],
"attrs": [null, null, null, null, null, null, null, null, {"type":"tinaja"}, {"type":"cucharon"}],
"murky": true,
"hidden": [[8,0], [8,1]],
"intro_mechanic": "tinaja"
```

- La tinaja (8) tiene taperebá (oculta), açaí (oculta) y camu camu en la boca. El chip muestra «×3». Los nombres son los de los tintes: dentro hay agua teñida, no jugo.
- Hay 6 frutas de 4 dosis y quedan 2 vacíos de 4 sin contar la tinaja ni su cuia: es la forma del debut.

**Qué se ve**

| Siempre | Nunca |
|---|---|
| La boca, con el color y el icono de Patrones de la fruta de arriba | El color o el orden de las dosis de la barriga mientras no suban a la boca |
| El chip «×n» con las dosis que quedan dentro, contando la boca | Un «?» ni el pardo #8A7B6A: no hay agua turbia; el barro es opaco |
| El jirau que comparten la tinaja y su cuia, y su cuia colgada mientras la tinaja tenga dosis | Qué frutas guarda (por defecto; Q-Ti5) |
| El contenido de su cuia, que es de vidrio | |
| Las capas ya reveladas, como bandas de luz a su altura con su icono | |

Qué frutas guarda la tinaja se puede deducir del tablero (`units_per_color` menos lo visible). Enseñarlo, sin el orden, queda tras el interruptor `ui_tinaja_show_contents` = false. Para el solver no cambia nada.

**Accesibilidad (02 §12 y C9 de 02 §3.1)** [P]:
- La etiqueta del lector de pantalla no dice nombres ocultos, ni «turbia» (aquí no hay agua turbia), ni huecos (la tinaja nunca recibe). Ejemplo: «tinaja: camu camu en la boca, 3 dosis dentro».
- Su cuia lleva la etiqueta normal de una cuia: «su cuia chica: açaí, 1 hueco».
- Mantener pulsado (C9) sobre la tinaja solo muestra el nombre de la boca y de las capas ya reveladas.

**R10 por etapas** (orden de PM anexo B, ya en el núcleo):

| Etapa | Qué hace la tinaja |
|---|---|
| (a) revelado | La nueva boca queda visible (código actual) |
| (b) aperturas por recepción | — |
| (c) sellado | — (por T4 la tinaja nunca es monocolor con 2 o más dosis; su cuia, por R5, nunca sella) |
| (d) liberaciones por sellado | — |
| (e) contadores | — (no tiene contador) |
| (f) condiciones | **Nueva:** si la tinaja está vacía, su cuia se suelta (`WithoutMechanic`; evento `CucharonReleasedMask`, además de `ReleasedMask`) |
| (f′) punto fijo | Se repite como siempre |
| (g) victoria | Sin cambios |
| (h) bloqueo | Sin cambios, con las exenciones de `futil` |

La suelta es una **liberación por condición** (etapa f), la misma familia que la Palizada y la Lancha. Por eso choca con Q14 A y con 03 §8 (ver «Combinaciones» y Q-Ti1).

**`futil` (PM anexo B).**
- **La tinaja está exenta como origen** (`no_puede_recibir(A)`, la línea prevista para la Cocha cerrada; `ExemptAsSource` = true).
  - Sin la exención, sacar la última fruta visible a su cuia vacía sería «monocolor → vacía con cap ≠ U_c», es decir, fútil. R13 declararía Aguas estancadas en un nivel ganable, y eso rompe R14.
  - Medido en el prototipo de V3: sin la exención, 0 de 12 tableros tienen ruta segura.
- **Su cuia, antes de la suelta, también está exenta como origen** (como el Remolino en Q-B).
  - Ejemplo: sacas x e y, juegas la y y vuelve a salir una x (T4 solo separa vecinas). La cuia queda [x, x] y llena. Vaciarla en una vacía de capacidad 2 o 5 contaría como fútil, y es justo lo que deja volver a sacar.
- **No abre ciclos infinitos de vertidos exentos.** Sea Φ = 2 × (dosis en la tinaja) + (dosis en su cuia). Sacar baja Φ en 1 y cada vertido desde su cuia lo baja en lo que vierte. Ningún vertido exento lo sube, así que no hay una cadena infinita de vertidos exentos que vuelva al mismo estado (el riesgo que describe PM anexo B).
- **Lo que Φ no dice:**
  - Los vertidos de sacar que quedan pendientes retrasan el modal de Aguas estancadas hasta Φ vertidos (como mucho 12), y cada uno cuesta una Marea al deshacer (hay 3 gratis por nivel).
  - Después de la suelta, la Cuia chica con 1 dosis y una cuia con racha de 2 o más de esa fruta forman un vaivén «útil» en los dos sentidos: el juego no declara Aguas estancadas aunque el nivel esté perdido. Es la incompletitud conocida de R13 (CHANGELOG del núcleo, C1-2: «R13 es correcto, no completo»), pero la cuia suelta la hace frecuente.
  - Ejemplo (notación de Morpho, más abajo): desde el estado perdido N[c,c,c] N[a,b,a,a] N[b,b,b] T[c(o),a] Ch[], los vertidos que quedan son sacar a, sacar c (la tinaja se vacía y su cuia se suelta con [a, c]) y ch→N1, que sella c. Queda N[c,c,c,c] N[a,b,a,a] N[b,b,b] T[] ch[a]. Desde ahí, N2→ch mueve una a y ch→N2 la devuelve, siempre como vertidos útiles: el modal nunca sale [V-proto, `tinaja_check/ex2.py`]. Para deshacer el vertido que perdió (N1→N2) hacen falta 5 Marea.
  - Propuesta [P] para 02 R13: valorar que el juego ofrezca el modal cuando un estado canónico se repite sin revelar nada nuevo. Y añadir a P3 un test de este vaivén con su cuia ya suelta.

**R14 (nunca se anuncia un bloqueo con progreso posible).**
- **Mientras la tinaja tenga dosis, R13 solo puede declararse si no queda ningún vertido legal.** Motivo: un vertido fútil necesita una vacía normal como destino (la Hoja y el Matapi vacíos cambian al recibir, y la tinaja nunca recibe). Si hay una vacía normal, o su cuia tiene dosis y puede vaciarse en ella (vertido exento, útil), o su cuia está vacía y sacar es legal (exento, útil).
  - Ejemplo de bloqueo correcto: Ch[c,a] llena, sin destino para a y sin vacías: no hay ningún vertido legal.
  - Las dos exenciones son necesarias. En el modelo de reglas, de 26.230 estados con su cuia colgada ninguno estaba atascado teniendo algún vertido legal; sin las exenciones aparecen 144 bloqueos falsos con solución [V-proto, `tinaja_check/tied.py`].
  - P3 se cumple mientras su cuia cuelga, porque solo recibe de la tinaja: no hay vaivén como en Q-B.
- Cuando la tinaja se vacía, la cuia se suelta y rige 02 §1.4.
- **Capacidades mixtas siempre.** Todo nivel con tinaja acaba con capacidades mixtas: su cuia se suelta como Cuia chica de 2 y la tinaja inerte es de 5. R14 se cumple (el juego no declara bloqueo con solución), pero **la poda de fútiles del solver** (`MOVIMIENTOS`, 03 §5) puede dejar sin camino útil un tablero que se gana. Es el hallazgo C1-8 del CHANGELOG del núcleo (`FutilePropertyTests.R16_MixedCapacities_PrunedParVersusExhaustivePar`), y pasa sin Liana.
  - Contraejemplo alcanzable (4 frutas a-d, U = 4), después de la suelta: N1[b,d,c,a] N2[b,a,a,a] N3[b,d] N4[b] N5[c,c,c] T[] (inerte) ch[d,d]. Se llega, por ejemplo, desde N1[b,d,c,a] N2[b,a,a,a] N3[b,d] N4[b] N5[c,c] T[d,c,d] con su cuia vacía, sacando d, sacando c, Ch→N5 y sacando d.
  - Con todos los vertidos se gana en 10: ch→N3, N4→ch, N1→N4, N1→N5, N2→N4, N2→ch, N1→N2, N3→N2, N1→N3, ch→N3. El segundo, N4→ch, es fútil según 02 §1.4: lleva una cuia monocolor de 4 a una vacía de 2.
  - Comprobado con el núcleo real: A* con poda devuelve «irresoluble» y sin poda resuelve en 10; `SafeMoves` con `ForPipeline` da 0 vertidos seguros y sin poda da 1 [V-proto, `refute_tinaja_prune`].
  - Consecuencias si no se corrige: pruebas falsas de «irresoluble», un Morpho que dice «no hay salida» cuando la hay, niveles rechazados y ramas del rescate sin motivo. Esa poda la usan `SolverMoves` con `futile_rule_enabled`, `SafeMoves.ForPipeline`, Morpho, la pista por inactividad, la prueba de irresolubilidad y la verificación del rescate.
  - **Medida [P]:** en niveles con tinaja, todas esas búsquedas van sin poda de fútiles (o con una poda sin el caso cap 4 → cap 2). La otra vía es decidir antes 02 R16 para capacidades mixtas (pregunta abierta del tech-lead, bloqueo C1-9). El coste en nodos de buscar sin poda está por medir [E].
- **Antes del estreno** tienen que pasar:
  - los tests de propiedad P1-P3, con capacidades 2, 4, 5 y 6, en `run` y en `single`, con el paso en que la cuia se suelta y Marea la vuelve a colgar;
  - la comparación «A* con poda frente a búsqueda exhaustiva» en tableros con tinaja.

**Marea y Volver a la orilla.**
- **Marea** deshace la sacada entera: la dosis vuelve a la boca y, si la cuia había bajado del jirau, vuelve a colgarse (todo vive en `Cuia.Packed`).
- **Lo revelado sigue revelado** (R7, T6, I5), como en Aguas turbias.
- **Volver a la orilla** pone el mismo tablero, con lo revelado del mismo intento (T6; `LevelSession.Restart` hace `Initial.WithRevealed(_revealed)`).
- **Consecuencia: espiar es gratis.** Volver a la orilla con menos de 8 vertidos no pide confirmación, no es fallo y devuelve las 3 Marea gratis (04 §2.7 y §5.2). Como sacar siempre es legal mientras su cuia tenga hueco, el jugador puede destapar la tinaja entera y reiniciar sin coste:
  - debut, T[a(o),b(o),c]: sacar dos veces enseña b y a; Volver a la orilla devuelve S0 con la tinaja entera a la vista;
  - con 5 dosis (4 ocultas) bastan 6 vertidos: sacar, sacar, vaciar 1, sacar, vaciar 1, sacar, si hay destino para los dos vaciados. Son menos que `restart_confirm_min_pours` = 8.
- Por eso, el dilema «mirar o enterrar» solo existe dentro de un intento, y en los niveles con tinaja la Luciérnaga sirve sobre todo para ahorrar vertidos. Lo decide el dueño en **Q-Ti6** (recomendado: aceptarlo, como ya pasa con Aguas turbias, y medirlo en el soft launch).
- **Hay que definir «intento»** en T6 (02 §11). En 04 §6.1, `attempt = fails_level + 1`, así que un reinicio con menos de 8 vertidos no abre un intento nuevo. Pero la confirmación de reinicio («Perderás los vertidos de este intento») y la intro («Nivel N · intento k») usan la palabra en otro sentido.

**Pororoca (canon; aún no está implementada, I6).**
- Revela todas las capas de la tinaja antes de barajar (T6; 04 §2.4 a).
- La tinaja y su cuia colgada quedan fijas con su contenido, como cualquier cuia con mecánica (04 §2.4 b; 03 §9, consecuencia 2: se rebarajan solo las dosis libres). Si su cuia ya bajó del jirau, es una Cuia chica normal y entra en el barajado. La verificación usa la semántica de la tinaja.
- **Problema:** la reconstrucción inversa de 04 §2.4 parte de «un estado resuelto parcial» de los bloques libres. Mientras quedan dosis en la tinaja, ese estado no existe, y tampoco modela la salida por su cuia.
  - Ejemplo: con T[a,b] revelada y Ch[c,a] fijas, en las cuias libres a tiene 2 dosis, b 3 y c 3: ninguna cuia resuelta puede quedar llena.
  - Caerá a menudo en «Permutar + verificar» o en «La ola no pudo remover el río» (sin cobro).
- **Hace falta una fila M13 en 02 §9.2 y en 03 §9** (Generación y Pororoca), como pide cada mecánica.
- Propuesta [P]: medir en el pipeline cuántas veces falla. Si es a menudo, el tech-lead la deshabilita mientras la tinaja tenga dosis, como ya se hace con la Lluvia y la Piraña.

**Luciérnaga.**
- Revela todas las capas de la tinaja (T6; 04 §2.5).
- La ranura 5 de la bandeja aparecerá porque el nivel es `murky`, y la tinaja latirá en el modo de selección como una cuia turbia. La ranura y el `RevealBooster` llegan con la tarea I3 del paso 1 (c.7).

**Morpho y pista por inactividad.**
- Solo proponen vertidos seguros sobre las realidades posibles (T5). `hint_supported` = true.
- Morpho calcula el vertido al momento, desde el estado actual: `safe_path` no viaja en el pack (03 §8 y §10; 08 §4.1; level-qa falla si aparece).
- **Orden de respuestas [P]**, que hay que alinear en 02 T5, 02 §9.2 (regla 5), 03 §8 y 04 §2.3 (hoy dan cuatro frases distintas):
  1. Si hay un vertido seguro en todas las realidades, Morpho lo propone y cobra.
  2. Si **ninguna realidad tiene solución** (también con R = 1, cuando ya no queda nada que ver): «Desde aquí no hay salida: la Marea o la orilla», con Deshacer y Volver a la orilla, y **no cobra** (04 §2.3, regla de uso).
  3. Si alguna realidad tiene solución pero ningún vertido es seguro en todas: «No veo un vertido seguro: la luciérnaga ilumina la tinaja», sin prometer nada y **sin cobrar**. Nunca con R = 1.
  4. Si se agota `hint_timeout_ms` (150) antes de decidir: «No encuentro un camino desde aquí», y **no cobra**.
- La pista por inactividad sigue el mismo orden: en los casos 2-4 no pulsa nada.
- **Por qué hace falta el orden.** Notación: N[..] es una cuia de 4, de fondo a boca; T es la tinaja (cap 5); (o) marca una dosis oculta; Ch es su cuia; a, b y c son frutas con U = 4.
  - S0 = N[c,c,a] N[a,b,a] N[b,b,b] T[c(o),a(o),c] Ch[]. Tiene ruta segura de 8 vertidos: N2→N1, N2→N3, N1→N2, sacar, sacar, Ch→N2, sacar, Ch→N1.
  - Un desvío legal de 3 vertidos (sacar, N1→N2, Ch→N1) lleva a N[c,c,c] N[a,b,a,a] N[b,b,b] T[c(o),a] Ch[]. Ahí el único vertido legal es sacar. Solo queda 1 realidad (por recuento, la oculta es c) y está perdida. Aguas estancadas no sale, porque sacar está exento de `futil`.
  - Con la regla anterior («La morpho no ve dentro de la tinaja: ilumínala con una Luciérnaga»), Morpho mandaba comprar una Luciérnaga (120 semillas) que no sirve de nada. Eso va contra la honestidad de 04 (principio 4) y contra «la compra cumple» (§6.4).
  - En el modelo de reglas, de unos 6.400 estados sin vertido seguro (10 familias de tableros pequeños, 3-5 frutas y 3-5 dosis en la tinaja), el 100 % estaba perdido en todas las realidades [V-proto, `tinaja_check`]. El caso 3 será raro y el caso 2, el habitual.
- El hueco viene del canon: 03 §8 y 04 §2.3 (a) dan la misma regla para Aguas turbias («La morpho no ve bajo el agua turbia: ilumina una cuia con Luciérnaga»). La corrección vale también para ellas (c.10).
- `SafeMoves` ya distingue por realidad «irresoluble» y «presupuesto agotado», pero hoy solo devuelve el número de vertidos seguros: hay que exponer la causa (c.2).

**Cuia extra y Cuia de rescate.**
- Son normales: no reciben de la tinaja, y su cuia sí puede verter en ellas.
- La verificación del rescate usa la realidad verdadera (04 §6.4), con vertidos sin poda (ver R14).

**Combinaciones.**
- **Permitidas:** solo con filtros (Q14 A): Hoja, Hoja atada, Brote, Matapi, Barro fuera de la pareja, Cuia chica y Cuia alta. Con Cuia alta, solo después de los tests P1-P3.
- **Prohibidas:**
  - otras cuias turbias: la tinaja es la única fuente de ocultas;
  - Liana, Remolino, Lluvia y Piraña (`LevelValidation` ya lo prohíbe con ocultos);
  - Aguapé, Palizada, Lancha y Cocha (PM d.3: disparadores y contadores);
  - Barro dentro de la tinaja o de su cuia;
  - dos tinajas (queda como variante, d.4);
  - Río sin fin y, en la 1.0, el Reto del río.
- **La tinaja sola ya choca con Q14 A.** En D-007, Q14 A define «mecánica con disparador» como «las de liberación y los contadores», y 03 §8 prohíbe dosis ocultas «en niveles con mecánicas con disparador». La suelta de su cuia es una liberación (R10 f) y la tinaja lleva ocultas. Además, `LevelValidation.MurkyError` rechaza hoy las ocultas en cuias con mecánica, citando Q14 A.
  - Se pide como excepción en **Q-Ti1**, con esta justificación: la suelta depende solo del recuento visible («×n»), nunca del color de una oculta, así que la premisa de `SafeMoves` (la legalidad y la cantidad no dependen de lo oculto) se mantiene.
  - Propuesta de texto para 03 §8: lo prohibido es un «disparador que depende de lo oculto».
  - Test: la suelta ocurre en el mismo vertido en todas las realidades.
- La tinaja y su cuia cuentan como **una** mecánica para el tope por nivel (02 §9.2, regla 4; PM d.2.5: 3 solo en retos e hitos). Hoy level-qa las contaría como dos o tres (c.5).

**Tope de ocultas por tipo de nivel** (lo comprueba el pipeline, como T1):

| Posición | Dosis en la tinaja | Ocultas | Realidades |
|---|---|---|---|
| Debut (p3-p5) y comodidad o práctica | 3 | 2 | ≤ 2 |
| Retos (p6, p9, p12, p15, p18) | 4-5 | 3-4 | ≤ 6 / ≤ 24 |
| Hito (p20) | 5 | 4 | ≤ 24 |
| Recuperación (p1-p2) y remanso (p19) | — | — | Son niveles puros: sin tinaja |

Las 5 dosis (4 ocultas) quedan condicionadas a la compuerta de coste de Morpho (c.3).

**Tutorial** (2 líneas, con dedo guía: tinaja → su cuia, y después su cuia → destino; el dedo guía depende de P5 de `docs/dev/decisiones.md`; textos pendientes del asesor):

| | Línea 1 | Línea 2 |
|---|---|---|
| ES | «La tinaja no deja ver: solo ves la fruta de arriba, y la de debajo nunca es igual.» | «Saca el agua con su cuia chica: le caben dos dosis.» |
| PT | «A talha não deixa ver: só se vê a fruta de cima, e a de baixo nunca é igual.» | «Tire a água com a cuia pequena dela: cabem duas doses.» |
| EN | «The water jar hides what's inside: you only see the top fruit, and the next one is never the same.» | «Scoop the water out with its small cuia: it holds two doses.» |

- En el segundo tablero de debut, un consejo de una línea [P]: «Si sacas dos veces, la primera dosis queda debajo.» / «Se tirar duas vezes, a primeira dose fica embaixo.» / «Scoop twice and the first dose ends up underneath.»
- Nunca la palabra «barro», que es la mecánica M5. Se dice **dosis**, como en el tutorial de la Cuia chica (02 §8: «solo guarda dos dosis»). «Porción» no es palabra del GDD; cambiarla obligaría a cambiar todos los textos de interfaz.
- El verbo es **sacar** (agua), nunca «servir»: servir con un cazo desde una tinaja es el gesto del masato (Cultura).
- Las versiones PT y EN de «dosis» siguen a la localización de 02 §8, que el GDD aún no fija [P].
- Si el dueño elige Q-Ti2 B (regla de color), la línea 2 pasa a «Saca el agua con su cuia chica, una dosis cada vez».

**Aspecto (D-003 «Selva brillante premium»: vidrio, madera y luz de juego).**
- **Material:** barro cocido claro, entre **#C4906E y #C7A182**. Lo comprobé con ΔE00:
  - a 13,7 o más de los 14 tintes (lo más cercano: Aguaje, 13,7) y a 15,5-16,3 del pardo de las ocultas (#8A7B6A);
  - por encima del mínimo de 10 que exige 07 §3, regla 6.
  - Vetado el terracota saturado **#B5653A**: está a 5,8 del Agua del Río Negro (#A9672E).
- **Silueta:** vasija utilitaria lisa, como las piezas lisas de Icoaraci [V T6].
  - Más alta que ancha, con hombro, cuello corto y boca de 0,70 del ancho de la cuia o más.
  - Ocupa una ranura con el alto de la Cuia alta de 5 (6,60 u; 07 §4.5).
- **El jirau:** una tabla de varas bajo las dos ranuras, que comparten la tinaja y su cuia. El jirau es la plataforma de varas donde se ponen los utensilios, no un pie [V T21, T1].
- **Vida:** gotitas quietas de «sudor» que pillan la luz (el barro enfría el agua [V T7]).
- **Decoración:** ninguna, o una sola línea incisa propia.
- **Vetos:**
  - nada de kené, marajoara ni tapajônica;
  - ni cara, ni dos bocas, ni forma de cruz, ni tamaño de urna [V T10];
  - nada que recuerde al caldero de MS (07 §1.3): nunca redonda y baja, ni morada, ni con fuego, vapor o burbujas;
  - sin hoja en la boca: las tinajas de masato se tapan con hojas de plátano [V T20];
  - nadie recibe ni bebe lo que se saca, y nunca cuencos que pasan de mano en mano (así se comparte el masato [V T10]);
  - nada de cordón, liana ni nudo entre las dos piezas: es la imagen de la Liana (M3).
- **Tiene que distinguirse de** el Cántaro (la alcancía con semillas), el lodo oscuro del Barro M5, el agua parda con «?» de Aguas turbias y la Liana.
- **Capi nunca la usa** (07: Capi no cocina, no mezcla ni «prepara»).
- **Un solo diseño neutro en todos los mundos**, sin versiones regionales.
- **Elementos:**
  - **Boca:** una elipse con el tinte y el icono de la fruta de arriba. Con la cuia de 48 dp mide unos 34 dp y el icono, 14-16 dp.
  - **Chip «×n»:** en el hombro, marfil sobre madera, texto de 14 sp o más y contraste de 5:1 o más.
  - **Jirau:** tabla de varas de madera bajo las dos ranuras. Es estado permanente, no un resaltado: no cambia al seleccionar (D-002).
  - **Su cuia:** una Cuia chica de vidrio que cuelga del jirau junto a la tinaja, como las cuias pitingas, que se cuelgan con alambre o fibra por dos agujeritos [V T1, foto 3]. Su asa pequeña de fibra es solo arte, para que se lea como el cazo de la casa.
  - **Capas reveladas:** bandas de luz en la barriga, a su altura, con su icono.

**Animación y sonido** (como los desbloqueos de MS: cada cambio con su golpe; tiempos [P], escalados por `anim_speed_factor_*`):

| Momento | Qué se ve | Duración | Sonido / háptico |
|---|---|---|---|
| Seleccionar la tinaja | Sube 8 dp (la mitad que una cuia: pesa) y se asienta con un balanceo de 2°. Nada más cambia (D-002) | 100 + 150 ms | «toc» grave de barro |
| Sacar agua | **La tinaja no se inclina.** Su cuia viaja en arco hasta la boca (300 ms), entra inclinada 20° por detrás del labio, se llena (280 ms) y vuelve con 2-3 gotas (300 ms). Es el gesto de recoger agua con la cuia [V T1] y **la firma visual**: se mueve el destino, no el origen. Pendiente del asesor como punto expreso (h); si confirma el riesgo, Q-Ti7 | ≈ 0,9 s, como un vertido (02 §4.1; D-006) | «glup» de calabaza en el agua; tic al empezar |
| Revelado | La boca queda oscura 100 ms; aparece un anillo de onda, sin burbujas; la fruta nueva florece en 0,10 s (OKLab, como D-010); el chip baja (×4 → ×3) con un rebote | ≤ 400 ms | gota |
| Rechazo (R11, caso C6) | La tinaja baja y la otra cuia hace la sacudida normal; si esa cuia puede ser origen, la selección pasa a ella (C5) y no hay rechazo; sin texto | 120 ms | el del rechazo de 02 §4.1 |
| **Suelta** (desbloqueo) | Con la última sacada, la boca muestra el fondo seco. Su cuia se descuelga del jirau y se asienta en su ranura (300 ms); la tinaja se tumba boca abajo sobre el jirau, a secar (500 ms) | ≈ 0,8 s, dentro de los 2,8 s de los pasos de mecánica | roce de fibra y «toc» hueco; háptico ligero |
| Intro D-010 | El chorrito entra por la boca el tiempo de sus n dosis, sin que se vea subir el agua. La tropa deja caer **una** fruta en la boca. El chip «×n» aparece en el fotograma del primer impacto. Las ocultas no reciben fruta (02 §4.2) | Dentro de las fases b-d | — |
| Luciérnaga | Entra por la boca; la barriga se enciende por dentro y muestra sus capas como bandas | 0,6 s (04 §2.5) | campanilla y grillos (04 §2.5) |
| Pororoca | Al pasar la ola se encienden las bandas, como con la Luciérnaga; la tinaja y su cuia colgada no se mueven | Dentro de los 1,2 s de la ola | — |
| Reducir movimiento | Fundidos de 0,25 s | — | — |

**Cultura: la trama con fuentes.**
- **La cuia es el cazo de la casa.**
  - En las várzeas del Bajo Amazonas, cada casa tiene cuias sin pintar (pitingas) que hacen de balde, vaso y cuenco. En Aritapera (Santarém), en el jirau, unas cuias cuelgan y otras se usan para recoger y verter agua [V T1, fotos 3 y 8].
  - Siguen en el día a día de Pará [V T2].
  - Por eso la Cuia chica, que desde el 14 sirve «para apartar», revela aquí su oficio real.
- **La vasija de agua de la casa.**
  - En Quatipuru-Mirim (Pará), las fazedeiras hacen a mano el panelão para guardar la lluvia, porque en la isla falta agua [V T4].
  - En Icoaraci hay alfarería utilitaria desde 1903 y talleres de piezas lisas sin pintar [V T5, T6].
- **Por qué no se ve dentro.** Es física, no magia: el barro sin esmaltar es opaco. El botijo de barro enfría el agua por evaporación [V T7; la fuente solo describe el enfriamiento, sin Brasil ni Amazonía]. En Brasil se sigue guardando agua en potes de barro [E T8].
- **Qué guarda: solo agua de la casa**, de la lluvia o del río, como el panelão [V T4].
  - El color es el tinte que dejan caer los miquitos (D-010), como en cualquier cuia: los nombres de fruta son los de los tintes, no el contenido.
  - Ninguna fuente de este documento dice que las casas ribereñas guarden jugos o refrescos en vasijas de barro. T4 y T9 hablan solo de agua. T16 y T17 prueban que los jugos existen, no que se guarden en tinajas. Las únicas fuentes que describen bebida guardada en tinaja (T10, T11, T20) hablan de masato.
  - Nunca «refrescos», «jugo», «guardado días», «madurar» ni capas «de otros días».
- **El riesgo del masato.** En la Amazonía peruana, tinaja + bebida guardada + cuia que entra y sale llena para servir es la escena del masato:
  - el masato se fermenta en tinajas tapadas con hojas de plátano y se saca con un pate [V T20];
  - el glosario dice que el pate es el mismo objeto que la cuia (00 §4);
  - el chomo shipibo guarda masato [V T10], y las tinajas kukama, masato y aguardiente [V T11];
  - el estreno cae en un mundo peruano (7, Madre de Dios).

  Mitigación: solo agua, el verbo «sacar», nadie que reciba lo sacado, y el gesto y el estreno pasan al asesor como punto expreso (h). Si el asesor confirma el riesgo, Q-Ti7.
- **Lo que no se afirma.**
  - Hoy la mayoría de las casas ribereñas recoge agua de lluvia y la trata [E T9]. La tinaja es un objeto tradicional, no la norma actual: ningún texto dice que todas las casas la usen.
  - Que las casas ribereñas guarden jugos en tinajas: ninguna fuente lo dice.
  - Que «lo sacado no vuelve» es una regla de juego, no una costumbre.
  - Que su cuia cuelgue junto a la tinaja es un recurso de juego [P], inspirado en las cuias colgadas del jirau [V T1].
- **Bebidas vetadas.**
  - Masato, chicha (tampoco «chicha morada», por ambigua), caxiri, cauim, aguardiente y licor de jenipapo [V T18].
  - La palabra «vinho»: desde 2025 existe un vinho de açaí fermentado al 12 % [V T19].
  - «Pócima», «poción» y «brebaje»: suenan a MS y a bebidas rituales.
- **Lo que la tinaja no es.** Nunca aparece en una fiesta, un ritual o un luto: solo en la cocina y el porche.
  - No es el **chomo** shipibo-konibo: lleva kené, guarda masato, su cuerpo se lee como el cosmos y la maweta se asocia a urnas funerarias [V T10].
  - No son las **tinajas kukama** de Santo Tomás (muritsu): sirven para masato y aguardiente, y sus diseños representan a los karuara y a los espíritus [V T11].
  - No es la **mucawa** ni el pondo kichwa (chicha ceremonial) [V T12].
  - No es la **igaçaba** ni el camucim, que también son urnas funerarias [V/E T13].
  - No es cerámica **marajoara** ni tapajônica, ni sus réplicas [V T14, T6].
  - 07 §8.1 ya admite la tinaja como objeto cotidiano con decoración propia.
- **Datos del río candidatos** (el texto nombra el lugar real aunque el nivel caiga en el mundo 7, como el Matapi en el 63):
  1. En las várzeas del Bajo Amazonas, cada casa tiene cuias sin pintar que sirven de balde, vaso y cuenco [V T1]. (Pará)
  2. En Quatipuru-Mirim, las fazedeiras hacen a mano vasijas de barro para guardar la lluvia [V T4]. (Pará)
  3. El barro sin esmaltar suda y enfría el agua [V T7]. (Dato general, sin lugar)
  4. En Icoaraci hay olarias de cerámica utilitaria desde 1903 [V T5]. (Pará)
- **Sin usar hasta confirmarla:** la etimología tupí «Kuimbúka», «concha de tirar água do pote». Solo la da un periódico [E T3]; hay que confirmarla en un diccionario de tupí antiguo (Navarro).
- **Si se usan referencias de alfareras o artesanos reales:** encargo o licencia con crédito y pago, como se propuso con ASARISAN. Nada de copiar réplicas.

---

## (c) El algoritmo

### c.1 Núcleo (`Packages/com.amazonfill.core/Runtime/AF.Core`, core-dev)

| Archivo | Cambio |
|---|---|
| `Mechanics/MechanicType.cs` | `Tinaja = 8` y `Cucharon = 9`, claves `"tinaja"` y `"cucharon"`; `Last` y `Keys` sin huecos. Los números no se persisten (lo dice el propio archivo), así que no pisan a las mecánicas de la 1.1, que tomarán los siguientes |
| `Mechanics/IMechanic.cs` y `MechanicTable.cs` | **Un gancho nuevo:** `bool AllowsTarget(in BoardSlice board, int source, int target)`, un filtro de origen con contexto. Por defecto devuelve true. Es la única pieza nueva de arquitectura: hoy solo se filtra el destino |
| `Rules/BoardRules.cs` (`Accepts`) | **Primera línea de `Accepts`:** `if (from.HasMechanic && !MechanicTable.Of(from).AllowsTarget(in board, source, target)) return false;`. Tiene que ir antes de la rama `to.HasMechanic` (`CheckTarget` devuelve Accept o Reject y sale) y antes del Barro expuesto (`return true`). Si fuera después, la tinaja podría verter en una Hoja vacía de la fruta de su boca o en una cuia con Barro expuesto, justo en dos combinaciones permitidas (el 332 con Hoja, y el Barro fuera de la pareja). Lo heredan `IsLegal`, `HasUsefulMove` (R13), `SolverMoves` (solver, bot y SafeMoves) y `CasualBot`. El descarte rápido de `SolverMoves.Collect` solo rechaza destinos normales, así que no se salta el gancho |
| `Mechanics/TinajaMechanic.cs` (nuevo) | `AllowsTarget`: el destino es la cuia `Cucharon`. `CheckTarget`: Reject. `ExemptAsSource`: true. `ValidateInitial`: cap 5, 3-5 dosis, ocultas = todas menos la cima, T4 en la columna, sin Barro, una sola tinaja y como mucho 4 ocultas (constante propia [P], no `lvl_murky_max_hidden_units`) |
| `Mechanics/CucharonMechanic.cs` (nuevo) | `CheckTarget`: Accept si el origen es la `Tinaja` y la cuia no está llena; si no, Reject. `ExemptAsSource`: true. `CheckCondition` (f): si la tinaja está vacía, `WithoutMechanic()` y el evento. `ValidateInitial`: cap 2, vacía y contigua a la tinaja |
| `Rules/PourEvents.cs` | `CucharonReleasedMask` (y `ReleasedMask`, que ya existe) |
| `Board/BoardBuilder.cs` | Leer los dos tipos. No hace falta id de pareja: con una tinaja por nivel, cada pieza encuentra a la otra por su tipo (O(T) sin asignaciones, solo en (f)) |
| `Board/LevelValidation.cs` | En `MurkyError`, la excepción de la tinaja a «ocultos solo en cuias sin mecánica» (Q14 A) y a T3 («≤ 3 dosis y nunca llena»). T4 ya se comprueba. Combinaciones prohibidas. Excluir la tinaja de **las dos** comparaciones de 02 §8: `cuiasWithCap[v] > colorsWithUnits[v]` (hoy la rechazaría como Cuia alta sin fruta abundante) y `cuiasWithCap[v] < colorsWithUnits[v]` (si no, con una fruta de U = 5 y sin Cuia alta, la tinaja pasaría por su casa y la validación aceptaría un nivel en el que esa fruta no puede sellar) |
| Sin cambios | `Cuia.cs` (el tipo ya vive en los bits 39-42 de `Packed`), `BoardCanon`, `BoardState`, `CuiaRules` (la tinaja no puede sellar por T4) y `LevelSession` (Marea restaura `Packed` y lo revelado ya persiste; si el dueño elige Q-Ti6 B, `LevelSession.Restart` y `h2` sí cambian) |

### c.2 Solver y bot (`AF.Solver`)

- **Sin cambios de código, pero con otra forma de llamarlos:**
  - `RealityEnumerator`: el multiconjunto oculto sale de U_c menos lo visible, con T4; rechaza más de 24 realidades.
  - `SafeMoves`: su premisa, que la legalidad y la cantidad no dependan de lo oculto, se cumple. Sacar solo depende de que su cuia tenga hueco y siempre mueve 1.
  - `SafePath`, `AStarSolver`, `SolverMoves`, `InverseWalk` y `EarlyTraps`.
  - **En niveles con tinaja, todas las búsquedas que deciden «hay solución» van sin poda de fútiles** (o con una poda sin el caso cap 4 → cap 2): el par, `SafePath`, `SafeMoves` (pipeline y Morpho), los paseos, la prueba de irresolubilidad y la verificación del rescate (b.2, R14; C1-8). El par tiene que calcularse igual en el generador y en QA-PAR.
- **Morpho necesita saber por qué no hay vertido seguro.** `SafeMoves` ya distingue por realidad «irresoluble» (`Unsolvable`) y «presupuesto agotado» (`BudgetExceeded`), pero hoy solo devuelve `Count` = 0. Para el orden de respuestas de b.2 hay que exponer si ninguna realidad tiene solución, si alguna la tiene o si se agotó el presupuesto. Lo mismo para el informe del pipeline (c.9).
- **Corte por tiempo:** el núcleo aún no corta por `hint_timeout_ms` (README del núcleo: «Pendiente: cancelación por tiempo del solver para Morpho; el núcleo no lee relojes»). Es una dependencia compartida con Aguas turbias (c.7).
- **Heurística (`SolverHeuristic`).** Mezclar en la cuia solo puede añadir cortes, así que sigue siendo admisible. Hay que añadir un test que lo compruebe sobre todos los estados alcanzables en tableros pequeños.
  - Opcional, solo si el presupuesto se queda corto: un término que cuente lo que falta sacar de la tinaja. Tiene que probarse admisible y **consistente**, también en el vertido que suelta la cuia, donde un término ingenuo cae de golpe.
- **Bot (`Bot/CasualBot.cs`, `Score`): rama mínima.** Hoy suma +16 por «juntar con el mismo color» a cualquier destino no vacío, porque la regla normal garantiza el color. Con la cuia que recibe cualquier fruta, el +16 solo vale si la cima de la cuia es de la fruta sacada. Es la misma corrección que ya tiene la rama del Remolino.
  - El −20 por «gastar un vacío» se queda: sacar a la cuia vacía puntúa −16.
  - Todo lo demás de `PUNTUACION_CASUAL` (03 §6) sigue congelado. El cambio se registra en D-011.
  - Medido con el generador de c.3 (capacidad 5, dosis de las cimas): con la rama mínima, el bot falla 0,00 en el debut, 0,00 de mediana con K = 7 y 4 dosis (máximo 0,82) y 0,08 de mediana con K = 8 y 5 dosis (máximo 0,48). En el prototipo de V1 (capacidad 6, dosis de cualquier capa), sin la rama mínima fallaba 0,12, y con una puntuación «neutra», que no se propone, 0,38.

### c.3 Generador (`tools/level-generator`, level-pipeline-dev)

1. **`Generation/Superposition.cs`, paso «Ti»** (va antes de «Tu»):
   - parte del tablero base de `walk`/`deep`;
   - saca n dosis (3-5, según la tabla de topes) de las cimas del tablero base y las ordena en la tinaja con T4, la boca visible y el resto oculto;
   - añade su cuia vacía en la ranura contigua de la misma fila;
   - **solo consume números aleatorios en los slots con tinaja**, para que los demás packs salgan idénticos.
2. **Vacíos:** el tablero base, sin contar la tinaja ni su cuia, conserva **2 cuias vacías de 4 en el debut (K ≤ 6) y en comodidad, y 1 en práctica, retos e hito** [P].
   - La aritmética de 02 §8 (`Σ cap_i − Σ U_c` ≥ 1, ≥ 2 en 1-30) **no cuenta la tinaja ni su cuia mientras cuelga**: la tinaja nunca recibe y su cuia solo recibe de ella. Con ellas, un tablero sin ninguna vacía normal la cumple: el S0 de b.2 tiene capacidades 4, 4, 4, 5 y 2, y 19 − 12 = 7, con solo 3 huecos sueltos.
   - Con este generador, K = 7 con 2 vacíos tiene ruta segura en 15 de 16 tableros (el que falla la encuentra con 200.000 nodos por realidad), pero el bot falla 0-2 %: 2 vacíos solo valen para comodidad. La regla anterior («2 vacíos solo con K ≤ 6») salía de otro generador (c.9).
3. **Verificación** (`SlotGenerator.cs` ya llama a `SafePath` cuando `data.Hidden.Length > 0`). Todas las búsquedas van sin poda de fútiles en niveles con tinaja (c.2).
   - `BoardBuilder` → `LevelValidation` → `SafePath` (RUTA_SEGURA, 03 §8) → A* → bot ×40 → `EarlyTraps`;
   - después, los filtros nuevos:
     - **Paseos seguros [P]:** 20 paseos por nivel, cada uno con un vertido seguro al azar hasta ganar. Se rechaza el nivel si alguno llega a un estado sin vertido seguro que no sea victoria.
     - **Paseos casuales [P]:** unos 30 paseos por nivel con cualquier vertido útil al azar. Se clasifica el primer estado sin vertido seguro de cada paseo: si es resoluble en la realidad verdadera, habría que adivinar, y el nivel se rechaza; si no lo es, es un error del jugador. Es un filtro, no una prueba.
     - Coste: unos 3 s por nivel con K = 8 si los paseos comparten el memo de `SafeMoves` [V-proto].
     - Sustituyen al «cierre seguro» de la versión anterior (BFS truncado a 300 estados con `SafeMoves`). Con 10-14 vertidos seguros en S0 y estados deduplicados por canon, 300 estados solo llegan a 4-6 vertidos de profundidad (como mucho 8) cuando el par es de 16-21: no miraba las sacadas intermedias ni la suelta, que es donde pesa lo oculto.
     - **S0 sin trampas** (solo en los tableros de debut): todo vertido útil de S0 es seguro. Se compara `PourRules.CollectUsefulMoves` con `SafeMoves` por hash canónico.
     - **Compuerta de coste de Morpho:** nodos expandidos con w = 2 y presupuesto de 40.000 por realidad, en S0, en cada estado de la ruta y en los de los paseos. Contar nodos es determinista.
       - El tope en nodos se fija midiendo en un teléfono de **clase A** con IL2CPP (08 §5.4 y §14.1, «referencia de rendimiento»: Moto G, Galaxy A0x/A1x, Redmi 9/10; 3 GB; Mali-G52 o Adreno 610), no en el teléfono de pruebas (Samsung SM-S938B, Galaxy S25 Ultra, gama alta).
       - Hasta tenerlo, se aplica un factor conservador sobre el PC [E: 4-8×]. Con ese factor, los 39-55 ms del prototipo con K = 8 y 5 dosis pasarían de los 150 ms de `hint_timeout_ms`, y Morpho y la pista responderían «No encuentro un camino» justo en los retos y el hito.
       - Si no cabe, las 5 dosis (4 ocultas) se reservan para K ≤ 7, o los retos bajan a 4 dosis.
   - El informe distingue «sin vertido seguro» de «presupuesto agotado» (c.2).
4. **Alivio (03 §13):**
   - `minus_color` nunca quita una fruta que esté en la tinaja: rompería T4 y la máscara;
   - las variantes de alivio repiten la ruta segura y los paseos.
5. **`PackWriter`:** attrs `{"type":"tinaja"}` y `{"type":"cucharon"}`, `murky` = true, `hidden` con las capas de la tinaja e `intro_mechanic` = `"tinaja"` en el debut.
6. **`Generation/CampaignReport.cs`:** `Has("Tu")` (hoy `Hidden.Length > 0`) y `Caps()` (hoy solo excluye la Piraña) distinguen la tinaja: si no, el informe la cuenta como Aguas turbias, Cuia alta y Cuia chica.
7. **Spec:** el ciclo 321-340 (d.3) en la spec de 201-700, o en `campaign_v2` si va a la 1.1.

### c.4 Esquema (`schemas/af-level-1.schema.json`)

- **`pack_schema` 3**, en los dos casos (1.0 o 1.1): el propio esquema dice que todo cambio que altere la validación sube `pack_schema`.
  - `mechanic_type` gana `tinaja` y `cucharon`, sin campos, solo con `pack_schema` 3.
  - `intro_mechanic` gana `"tinaja"`.
  - La descripción de `murky` pasa a decir «el nivel tiene dosis ocultas (Aguas turbias o tinaja)». `hidden` no cambia de formato: como mucho 4 pares. Su significado sí se amplía (c.5 y c.6).
- **Lo que el esquema no puede expresar** lo comprueba level-qa: que haya una sola tinaja con su cuia contigua y que `hidden` sea exactamente todas sus capas menos la boca.
- Casos válidos y no válidos nuevos en `tools/validate_schema.py`.
- Es el **contrato 21**: hay que avisar al tech-lead y al dueño.
- Los packs `mundo_02..05_v1.json` (11-200) siguen con `pack_schema` 2. El cliente acepta los dos.

### c.5 level-qa (`tools/level-qa/Checks`)

Hoy level-qa lee «hay ocultos» como «Aguas turbias» y cuenta cada cuia con mecánica como un ejemplar. Sin estos cambios, el debut del 323 fallaría QA-DEBUT, QA-MECH-SPEC y QA-MECH-COUNT.

| Archivo | Control |
|---|---|
| `MechanicChecks.cs` | `Codes()`: devuelve «Ti» para la pareja y no añade «Tu» cuando todas las ocultas son de la tinaja (hoy un tipo desconocido da «?» y cualquier oculta añade «Tu»: un nivel con tinaja contaría 2 mecánicas). `Instances()`: cuenta la pareja como 1 (hoy daría 3: tinaja, cucharon y una cuia turbia; QA-DEBUT exige 1). `CapacityCaps()`: excluye tinaja y cucharon, como ya excluye la cueva de la Piraña (si no, cuentan como Cuia alta y Cuia chica en QA-GAP-120 y QA-MECH-CELL). Controles nuevos: una sola tinaja; su cuia contigua y en la misma fila; máscara = todas menos la boca; T4; combinaciones; debut (sola, un ejemplar, 2 vacíos, bot < 0,20, S0 sin trampas) |
| `MurkyChecks.cs` | QA-MURKY-T1, con la excepción de la tinaja. QA-MURKY-Q9 usa `Codes().Count`: hereda el cambio de `Codes()`. Nuevo **QA-TINAJA-TIER**: el tope de ocultas por tipo de nivel (b.2) |
| `CycleChecks.cs` | `Presents("murky")` (hoy mira si hay ocultos) e `IsPure` (un nivel con ocultos no es puro) distinguen la tinaja de la turbia. El calendario de d.3, nada antes del debut y como mucho 8 apariciones en el ciclo de debut |
| `GateChecks.cs` | Igual que `CycleChecks` para las compuertas del calendario |
| `ReliefChecks.cs` | `minus_color` sin frutas de la tinaja; ruta segura y paseos en las variantes |
| Aritmética de vacíos | Donde se compruebe `Σ cap_i − Σ U_c` (02 §8), sin contar la tinaja ni su cuia colgada |
| Nuevo control de coste | Tope de nodos de Morpho en S0, en la ruta y en los paseos |

Coste: 1-2 días más en el carril del pipeline (c.7).

### c.6 Unity (unity-gameplay-dev)

| Archivo | Cambio |
|---|---|
| `Scripts/Levels/LevelPackReader.cs` y `LevelDefinition.cs` | Aceptar `pack_schema` 3 y los dos tipos. `LevelPackReader` valida con `LevelValidation`: la tinaja usa su tope propio, no el de Aguas turbias |
| `Presentation/Mechanics/TinajaOverlay.cs` y `CucharonOverlay.cs` (nuevos) | Subclases de `CuiaOverlay`: boca, chip «×n», jirau, bandas reveladas, sudor, secado, boca abajo y la cuia colgada. El contrato de overlays está congelado: solo se añaden 2 casos a `CuiaOverlays.Create` |
| `Presentation/Mechanics/MurkyMarks.cs`, `CuiaView.cs` y `Palette.cs` | No pintan «?» ni el pardo #8A7B6A (`_MurkyColor`) en la tinaja: sus ocultas no se dibujan como agua turbia (contrato 27) |
| `Presentation/PourVisual.cs` y `BoardAnimator.cs` | El gesto de sacar, en el que se mueve el destino. Los vertidos desde su cuia usan el gesto normal |
| `Presentation/Mechanics/MechanicStepKind.cs`, `MechanicStepPlan.cs`, `MechanicStepQueue.cs` y `StepStaging.cs` | Pasos de revelado en la boca y de suelta (cuia que baja del jirau y tinaja boca abajo), en el orden de R10 |
| `Presentation/IntroAnimation.cs` y `IntroChoreography.cs` | D-010: fruta solo en la boca y el chip en el primer impacto |
| `GameFlow/LevelStateSnapshot.cs` y `LevelFlow.Resume.cs` | Guardar y reanudar el estado de la tinaja (dosis, reveladas, cuia colgada o suelta) sin tratarla como turbia |
| `UI/BoardLayout.cs` | La pareja en la misma fila (como la regla de grupos del Aguapé) y el alto de la Cuia alta de 5. `BoardLayoutTests` lo comprueba a 360 × 780 y 360 × 640 |
| `UI/BoosterTray.cs` | La ranura 5 (Luciérnaga) llega con I3 del paso 1; aquí solo se activa en niveles con tinaja |
| `UI/DevProbe.cs` y `AF_STATE` | Estado de la tinaja (dosis, reveladas, cuia colgada o suelta) para probar por `adb` |
| Accesibilidad | Etiqueta del lector de pantalla y C9 de la tinaja (b.2), cuando existan para las cuias |
| Tarjeta de tutorial y dedo guía | Los textos de b.2. La tarjeta llega con I2 (`DebutCard`); el dedo guía de mecánica no está planificado (c.7) |
| Resto de consumidores | Unos 15 scripts leen `hidden` o `murky`. Hay que revisarlos uno por uno para que distingan la tinaja de la turbia |
| Tests EditMode y PlayMode | Uno por regla, citando su § |

**Tests del núcleo** (uno por regla, citando su §):
- `Tinaja_NeverReceives_M13r2`, `Tinaja_PoursOnlyIntoItsCuia_M13r3`, `Tinaja_AmountIsAlwaysOne_T4`;
- `Tinaja_CannotPourIntoHoja_M13r3` y `Tinaja_CannotPourIntoExposedBarro_M13r3` (el gancho va el primero en `Accepts`);
- `Cucharon_HoldsTwoDosesAnyFruit_M13r5`, `Tinaja_LastDoseNotFutile_AnexoB`, `Cucharon_FullNotFutile_QB`;
- `Cucharon_ReleasedWhenEmpty_R10f`, `Cucharon_ReleaseSameInAllRealities_Q14A`, `Marea_RehangsCuiaAndKeepsRevealed_I5`;
- `LevelValidation`: la tinaja no cuenta en ninguna de las dos comparaciones de capacidades (02 §8);
- Morpho: el tablero perdido de b.2 responde «Desde aquí no hay salida» y no cobra; con R = 1 nunca sugiere la Luciérnaga;
- P1-P3 (R14) con capacidades 2/4/5/6 en `run` y en `single`, más el vaivén con su cuia ya suelta;
- A* con poda frente a búsqueda exhaustiva en tableros con tinaja, con el contraejemplo de b.2 (R14);
- heurística admisible y consistente;
- `HotLoopAllocationTests` con un tablero con tinaja;
- una fila en `ReferenceBoards` y en `MechanicsPerformanceTests`.

### c.7 Tareas por agente [E]

| Agente | Días | Qué incluye | ¿Necesita Unity? |
|---|---|---|---|
| core-dev | 4-5 | Tipos, gancho `AllowsTarget` (el primero en `Accepts`), las 2 mecánicas (≈ 120 líneas), validación, eventos, rama del bot, causa del «sin vertido seguro» en `SafeMoves` y tests (por regla, propiedades, exhaustivos, heurística, sin asignaciones) | No |
| level-pipeline-dev | 4-7 | Paso «Ti», búsquedas sin poda, paseos seguros y casuales, S0 sin trampas, compuerta de Morpho, alivio, `pack_schema` 3, `validate_schema.py`, inventario de level-qa y del informe (c.5), spec del ciclo 321-340 e informe | No |
| unity-gameplay-dev | 5-7 | c.6 completa, con arte procedural provisional | Sí: después de la ola 5 del paso 1, con 2 Editores como máximo |
| docs (tech-lead) | 2 | Ediciones del GDD (c.10) y D-011 | No |
| code-reviewer | 1-2 | Revisión de los tres PR | Según el PR |
| **Total** | **16-23** | Unas 3 semanas de calendario en 3 carriles | |

**Dependencias compartidas, fuera de esta cuenta.** La justicia la garantiza el generador (c.8, puntos 1-6), pero la experiencia y varias mitigaciones de (f) se apoyan en piezas del cliente que aún no existen. Las comparte con Aguas turbias (nivel 100) y con todos los estrenos:
- **Morpho en el cliente**, con `SafeMoves` sobre las realidades y la pista por inactividad: ningún script de `Assets/` usa hoy `SafeMoves`, y no tiene tarea en el paso 1 del traspaso.
- **Corte por `hint_timeout_ms` en el núcleo:** pendiente (README del núcleo).
- **Luciérnaga:** lógica en la ola 3 y ranura en la bandeja en la ola 4 (tarea I3: `RevealBooster` y ranura 5).
- **Pororoca:** tarea I6, aún sin hacer.
- **Tarjeta de estreno:** tarea I2 (`DebutCard`). El **dedo guía de mecánica** no está en ninguna tarea.

Q-Ti3 A (estreno en la 1.0) queda condicionada a que Morpho en el cliente y la Luciérnaga estén listos. Si no lo están, el 323 sale sin tinaja (la salida a B ya prevista).

Fuera de los agentes:
- **Artista:** tinaja (cuerpo, máscara de la boca, bandas encendidas, boca abajo), jirau, asa de la cuia y 4 sonidos («glup», gota, roce de fibra y «toc»).
- **Asesor:** nombres, textos, Datos del río y el gesto de sacar (h).

### c.8 Cómo se garantiza que se gana sin adivinar

1. **Lo oculto nunca decide la legalidad ni la cantidad.** La boca siempre se ve (R7), sacar mueve siempre 1 (T4) y es legal si su cuia tiene hueco. La suelta depende del recuento visible, no del color de una oculta. Cada decisión se toma con toda la información que existe en ese momento.
2. **La incertidumbre está acotada:** 4 ocultas como mucho, es decir, 24 realidades (T2). `RealityEnumerator` rechaza más. En comodidad y práctica hay 2 ocultas: 2 realidades como mucho.
3. **Ruta segura (T5; 03 §8).** El generador rechaza el nivel si algún estado de la ruta, de S0 a la victoria, no tiene un vertido que lo deje resoluble en **todas** las realidades. Morpho propone un vertido seguro, que calcula al momento desde el estado actual (`safe_path` no viaja en el pack: 03 §8 y §10).
4. **Paseos seguros y casuales [P].** T5 solo cubre la ruta, y la ruta se elige con el par de la realidad verdadera. Los paseos seguros comprueban que, siguiendo vertidos seguros cualesquiera, siempre se llega a ganar; los casuales, que el jugador que se equivoca pierde por su error y nunca por tener que adivinar. Medido con el generador de c.3: 1.240 paseos seguros, todos ganados; 840 paseos casuales, 0 estados en los que hubiera que adivinar (c.9). Es un filtro, no una prueba completa.
5. **Debut sin trampas:** en los 3 tableros de estreno, todo vertido útil de S0 es seguro, hay 2 realidades como mucho y el bot falla menos del 20 %.
6. **Sin bloqueos falsos (R14):** las exenciones de `futil`, el argumento de Φ, los tests P1-P3 antes del estreno y las búsquedas sin poda de fútiles (C1-8). R13 no es completo: tras la suelta puede haber vaivenes útiles en un nivel ya perdido (b.2).
7. **Ayudas explícitas y honestas** (D-002: no se resalta nada): Luciérnaga, Morpho con su orden de respuestas (nunca cobra sin dar un vertido) y Marea. Dependen de piezas del cliente que aún no existen (c.7).

### c.9 Medido

**Con el generador de c.3** (capacidad 5, dosis sacadas de las cimas, sin `partner`; copia del núcleo parcheada, .NET 10 Release; 16 semillas por fila salvo donde se dice; revisión del 2026-10-10, `refute_c9` y `verif_tinaja`):

| Tablero | Ruta segura | Realidades máx. | Par mediano (mín.-máx.) | fallo_bot con la rama mínima: mediana (mín.-máx.) | Tableros ≥ 0,75 / ≥ 0,90 |
|---|---|---|---|---|---|
| **Debut, comodidad y práctica:** K = 6, 2 vacíos, 3 dosis (2 ocultas) | 16/16 | 2 | 16 (12-19) | 0,00 (0-0) | 0 / 0 |
| K = 6, 2 vacíos, 4 dosis | 16/16 | 6 | 17 (14-20) | 0,00 (0-0) | 0 / 0 |
| K = 7, 2 vacíos, 4 dosis | 15/16 (el que falla la tiene con 200.000 nodos por realidad) | 6 | 18 (15-21) | 0,00 (0-0,02) | 0 / 0 |
| **Reto con 4 dosis:** K = 7, 1 vacío, 4 dosis (3 ocultas) | 16/16 | 6 | 17 (14-19) | 0,00 (0-0,82) | 1 / 0 |
| **Reto o hito con 5 dosis:** K = 8, 1 vacío, 5 dosis (4 ocultas) | 16/16 | 24 | 20 (17-24) | 0,08 (0-0,48) | 0 / 0 |
| K = 9, 1 vacío, 5 dosis (10 semillas) | 10/10 | 24 | 21 | por medir | — |

- **Paseos seguros:** 320 de 320 ganados en el debut, 320/320 con K = 6, 2 vacíos y 4 dosis, 320/320 con K = 7 y 4 dosis, 180/180 con K = 8 y 5 dosis (12 tableros) y 100/100 con K = 9 (10 tableros). Ninguno llegó a un estado sin vertido seguro.
- **Paseos casuales** (cualquier vertido útil al azar, 30 por tablero): en el debut, 480 de 480 ganados; con K = 8 y 5 dosis, 360 paseos, 288 ganados, 72 perdidos por error propio y 0 estados en los que hubiera que adivinar.
- **El BFS de 300 estados** (el cierre seguro anterior) llega a 4-8 vertidos de profundidad (mediana 4-6) frente a un par de 16-21.
- **Tiempo de los paseos:** unos 3 s por nivel con K = 8 (180 paseos seguros sobre 12 tableros en 40 s).
- **Dificultad:** con K ≤ 8, la tinaja sola casi nunca llega a Difícil (1 tablero de 16 pasa del 0,75, con K = 7 y 4 dosis) y ninguno llega al 0,90 del hito. Ver d.3.
- **«Sin vertido seguro» frente a «presupuesto agotado».** `SafeMoves` cuenta como «no seguro» la realidad que agota su presupuesto (20.000 nodos en el pipeline). Con 200.000 nodos por realidad, los 10 tableros de K = 7 con 2 vacíos que fallaban en el prototipo de V1 y el único fallo con cimas tienen ruta segura. El informe tiene que separar las dos causas (c.2).

**Prototipo de V1** (otro generador: tinaja de **capacidad 6**, dosis sacadas de **cualquier capa** y emparejada con `partner`; `tinaja_v1`). Sus pares y fallos del bot no son los del generador de c.3, pero sirven para los tiempos de Morpho y las variantes. La equivalencia de capacidad 5 y 6 se confirmó: con capacidad 5 y dosis de cualquier capa (K = 8, 5 dosis), par 22 y bot 0,07-0,08 de mediana (máx. 0,78), igual que con capacidad 6. Lo que cambia los resultados es sacar las dosis de las cimas.

| Tablero | Ruta segura | Par mediano (mín.-máx.) | Morpho en S0 (PC) | fallo_bot |
|---|---|---|---|---|
| K = 6, 2 vacíos, 3 dosis (2 ocultas) | 16/16 | 17 (14-20) | ≤ 2,6 ms | 0,00 (máx. 0,03), rama mínima |
| K = 7, 1 vacío, 4 dosis (3 ocultas) | 16/16 | 18 (16-22) | ≤ 3,9 ms | 0,07 (0-0,55), rama mínima |
| K = 8, 1 vacío, 5 dosis (4 ocultas) | 16/16 | 22 (19-26) | 7-8 ms de mediana; máx. 39-55 ms según la ejecución | 0,07 (0-0,78), rama mínima |
| K = 9, 1 vacío, 5 dosis | 16/16 | 24 (21-30) | ≤ 27 ms | 0,45 (0-0,95) con puntuación neutra |
| K = 6, 2 vacíos, 4 dosis | 14/16 | 18 | ≤ 6,3 ms | 0,00 (neutra) |
| K = 7, 2 vacíos, 4 dosis | 6/16 con 20.000 nodos por realidad (los otros 10 agotan el presupuesto); con 200.000, 16/16 | — | — | — |
| K = 8, 5 dosis, cuia con regla de color (Q-Ti2 B) | 16/16 | 23 (20-27) | ≤ 25 ms | 0,23 (neutra) |

- **Tiempos offline con K = 8:** ruta segura en 1,6 s de mediana (11 s como mucho).
- **Variantes con K = 8 y 5 dosis:**
  - con o sin exentar su cuia, el par (22) y los nodos (8.174 de mediana) son iguales;
  - sin la suelta, el bot falla 0,50 (frente a 0,38 con la suelta, en puntuación neutra).
- **Morpho:** con K = 8 llega a 55 ms en este PC. Supera los 50 ms objetivo de T2, aunque queda bajo el tope de 150. Con el generador de c.3 no se ha medido [E: tableros más fáciles, probablemente menos]. En un teléfono de clase A puede pasar de 150 ms [E: factor 4-8×]. De ahí la compuerta de c.3.

### c.10 Ediciones del GDD que exigiría (sin editarlo)

**Registro y documento maestro**
- `docs/dev/decisiones.md`: entrada D-011 (además de D-007 y D-009, todavía sin registrar).
- `docs/GDD.md`:
  - §2 #10 (Aguas turbias) y #12;
  - §7, contratos 1 (`mech_debut_level`), 8 (Aguas turbias: 4 ocultas / 24 realidades) y 21 (esquema);
  - contrato 27, por la incidencia 28 de §7.2 (`_HiddenMask` y `_MurkyColor` = #8A7B6A): la tinaja no dibuja sus ocultas con ellos.

**00 · Glosario**
- §4: la skin 9, si se renombra.
- §6.5: «Tinaja» como nombre reservado, descrita sin «atada» ni «se libera».
- §10: textos del tutorial.

**02 · Núcleo**
- §1.2 `legal`: filtro de origen (el primero, antes de los filtros del destino) y cuia de la tinaja.
- §1.4 `futil`: las dos exenciones.
- R10 (f): la suelta.
- R13: nota [P] sobre el vaivén tras la suelta y el estado canónico repetido.
- §3.1: la tinaja con C5 y C6; si se acepta el atajo «su cuia vacía → tinaja», excepción explícita a C2.
- §4.1: sacar, revelado y suelta.
- §5: la pareja en la misma fila.
- §8: su cuia es una Cuia chica que cuelga de la tinaja; la tinaja no es casa de fruta abundante; la aritmética de vacíos no cuenta la tinaja ni su cuia colgada.
- §9.1 y §9.2: fila M13, con su columna de Pororoca y la de generación.
- §11: excepciones a T1 y T3; T2 cuenta las ocultas de la tinaja; T5 con el orden de respuestas de Morpho; T6 define «intento» (y Q-Ti6).
- §12: etiqueta accesible y C9 de la tinaja.

**03 · Niveles**
- §5: búsquedas sin poda de fútiles en niveles con tinaja.
- §6: la rama del bot.
- §8: la tinaja como fuente de ocultas, paseos y S0 sin trampas; «disparador que depende de lo oculto» en lugar de «mecánicas con disparador»; orden de respuestas de Morpho (también en Aguas turbias).
- §9: semántica para el solver, generación y Pororoca.
- §13: `minus_color`.

**04 · Comodines**
- §2.3: el orden de respuestas de Morpho, en la regla de uso y en el caso (a).
- §2.4 y §2.5: la tinaja con Pororoca y Luciérnaga.
- §2.7 y §6.1: qué es un «intento».
- §5.1: la copia de `futil` y `legalNonFutileMoves` que «usan cliente, bot y solver». Hoy exige el mismo color en todo destino y no tiene filtro de origen ni las exenciones nuevas: no generaría «sacar otra fruta a su cuia con una dosis» (con Ch[c] y la boca a, descarta T→Ch) y declararía fútil la última dosis sacada. Hay que añadir el filtro de origen, el destino «cuia de tinaja», las exenciones de la tinaja y de su cuia colgada, y sus casos en los tests de regresión 1-4.

**07 · Arte**
- §4.3 y §4.5: estados visuales y alto.
- §5: storyboards de sacar y de la suelta.
- §8.1 y §8.2: vetos nuevos (hoja en la boca, cuencos que pasan de mano en mano, cordón o liana entre las piezas) y lista del asesor.
- §9: los 4 sonidos.

**08 · Arquitectura**
- §4.1 y §4.4: el esquema y `pack_schema` 3.
- §5.1-§5.2: la tinaja no usa `_HiddenMask` ni `_MurkyColor`.
- §8.2: `af_mechanic_intro` = `tinaja`.
- §9: `mech_debut_level.tinaja` = 323, `mech_enabled.tinaja` = true y `ui_tinaja_show_contents` = false; nota de que `lvl_murky_max_hidden_units` no rige la tinaja.

**Apoyo**
- `propuesta-mecanicas-dificultad.md`: la excepción de la tinaja a Q14 A (h) y a d.3; d.4 y d.5: rehacer la tabla de novedades de 201-700 con Q8 (c).

---

## (d) Calendario

### d.1 Dónde debuta: nivel 323 [P]

- **Posición:** p3 del ciclo 321-340, que es de aguas bajas (B, tabla de PM d.5), en el mundo 7, Madre de Dios.
- **Rellena un hueco.** Entre el trío del 320 y el Brote de dos hojas del 343 no hay ninguna novedad (PM d.5, regla 5: nunca más de 60 niveles sin algo nunca visto; PM d.2.3: una «primera vez» por ciclo).
- **Evita los mundos con más riesgo cultural:** 5 (Loreto: tinajas kukama), 6 (Oriente: mucawa y chicha) y 9 (Ucayali: chomo shipibo).
- **Sigue siendo un mundo peruano.** El estreno en el mundo 7 pasa al asesor como punto expreso, junto con el gesto de sacar: tinaja y cuia que saca evocan la escena del masato en la Amazonía peruana (b.2, Cultura; (h)).
- **Temporada B [P]:** guardar agua en la tinaja tiene sentido cuando escasea [V T4]. El ciclo de un estreno toma la temporada de su mecánica (PM d.2.7), y el 321 es B.
- **Dato del río del estreno:** de Pará, dicho con su lugar real (b.2). Madre de Dios no tiene base local [NV].
- **Más visible antes del 323 [P]:** un tablero con la tinaja en el vídeo de la tienda y de UA (07, assets de tienda), y una Travesía cuando lleguen los eventos (1.2).

### d.2 En qué versión (Q-Ti3)

| | A · 1.0 (recomendada) | B · 1.1 |
|---|---|---|
| Cómo | Se genera dentro de los packs de 201-700, que aún no existen (Q8 c de D-007) | Build nuevo con el código; `pack_schema` 3; sustituye 323-340 con `level_overrides` o con una vuelta nueva (08 §4.4) |
| Condición | Que al generar 201-700 la tinaja pase sus pruebas y que Morpho en el cliente (con corte por tiempo) y la Luciérnaga estén listos (c.7) | — |
| Packs 11-200 | **No cambian ni un byte.** Se comprueba con `--check` sobre `campaign_v1` y `tutorial_v0`: el gancho solo actúa si el origen lleva mecánica, y el paso «Ti» solo consume números aleatorios en sus slots | Igual |
| Jugadores | Todos la ven en el 323 desde el primer día | Regla de migración: si el jugador ya pasó el 323, se le sirve una vez el tablero de estreno antes de cualquier nivel con tinaja (03 §9, consecuencia 3) |
| Riesgo | Más trabajo en la cola de la 1.0, aunque los carriles de C# no usan Unity y el de Unity entra después de la ola 5. Si no se cumple la condición, el 323 sale sin ella y se pasa a B | La 1.0 sale sin la seña propia, y el soft launch no la mide |
| Kill-switch | `mech_enabled.tinaja` = false: el nivel se sustituye por el respaldo del mismo slot (PM d.2.8) | Igual |

### d.3 Ciclo de estreno 321-340 (C0: como mucho 8 apariciones)

| p | Nivel | Contenido |
|---|---|---|
| 1-2 | 321-322 | Recuperación pura |
| 3-5 | **323 ★ · 324 ★ · 325 ★** | Comodidad. La tinaja sola, un solo ejemplar, 3 dosis (2 ocultas), K ≤ 6, 2 vacíos sin contar la pareja, bot < 0,20 y S0 sin trampas (02 §9.2, regla 2). Tutorial en el 323 y consejo de «la primera queda debajo» en el 324 |
| 6 | 326 | Reto con la secundaria del ciclo (la titular de 301-320), **sin tinaja**: el reto que sigue al estreno no la usa |
| 7 | **327** | Práctica: tinaja, 3 dosis (2 ocultas), 1 vacío, bot 20-60 % |
| 8 | 328 | Comodidad pura |
| 9 | **329** | Reto: tinaja sola, 4 dosis (3 ocultas). Difícil si el bot llega al 75 %; con lo medido (1 de 16 tableros con K = 7), lo normal es que se publique sin chip (PM c.2) |
| 10-11 | 330-331 | Comodidad (al menos 3 puras en el ciclo) |
| 12 | **332** | Reto: tinaja + Hoja, con K alto (8-9) y 1 vacío para buscar Difícil, dentro del tope de 12 cuias con tinaja de (f); si no llega, sin chip |
| 13-17 | 333-337 | Comodidad o retos sin tinaja |
| 18 | **338** | Reto: tinaja + Matapi, 5 dosis (4 ocultas) si la compuerta de Morpho lo permite (si no, 4 dosis), con K alto y 1 vacío. Es el espejo: «lo que entra no sale / lo que sale no vuelve» |
| 19 | 339 | Remanso puro |
| 20 | **340** | Hito: tinaja + Brote, 4 ocultas, si el bot llega a 0,90. Lleva 17 niveles desde el estreno y ya salió en un reto (PM c.4). Con lo medido (ningún tablero con K ≤ 8 llega al 0,90), lo más probable es que el hito vaya sin tinaja; lo decide el pipeline |

Apariciones previstas: 323, 324, 325, 327, 329, 332, 338 y 340, es decir 8 (7 si el hito va sin tinaja). Ninguna combinación prohibida. Nunca dos niveles con etiqueta seguidos.

### d.4 Después, y variantes

- **C+1 (341-360, A):** secundaria en 2-3 retos. Por ejemplo, Brote de dos hojas + tinaja después del estreno de esa variante en el 343.
- **C+2:** descansa. **C+3 o C+4:** vuelve en una combinación nueva.
- **Después:** entra en la bolsa como mecánica de aguas bajas y vuelve al menos una vez cada 3 ciclos de su temporada (PM d.2.4).

| Variante [P] | Qué cambia | Coste | Cuándo |
|---|---|---|---|
| **Cuia de la casa** (idea de V3) | Sin jirau compartido: la tinaja vierte en **cualquier** Cuia chica de 2 normal, con la regla de color. Esa cuia es a la vez búfer y cazo, y esa es la decisión | S (un tipo menos) | C+3, en torno al 383. Una «primera vez» más sin pieza nueva |
| **Dos tinajas** | Dos parejas; las ocultas se reparten (2 + 2, como mucho 4). Necesita `partner` (id de pareja de los bits 47-49) y sube `pack_schema` | S-M | Después del 400 |
| **Tinaja grande** (idea de V2) | Bloques de 2 dosis en una tinaja de 8-10 | M-L | Solo si el soft launch mide bien la tinaja, y con condiciones: `hidden` y `murky` normales, una sola ranura y la compuerta de Morpho. Nunca la de 12 como reto, porque es fácil (bot 0,00-0,03) |

### d.5 Relación con D-009, con la regla PM d.5 y con D-010

- **D-009.** La etiqueta sale del bot ×40, como siempre; el chip, las cintas ×3/×4 y los premios no cambian. La tinaja escala dentro de su vida:
  - de 2 a 3 y a 4 ocultas;
  - K de 6 a 9;
  - vacíos de 2 a 1.

  Sola rara vez llega a Difícil (c.9): los retos con chip necesitarán K alto y una combinación. El icono de la tinaja va en la tarjeta previa. En vivo, `lvl_label_override` corrige la etiqueta con los intentos reales.
- **Regla PM d.5.5.** El 323 cubre el ciclo 321-340. Con Q8 (c), la tabla de novedades de 201-700 hay que rehacerla igualmente (Lancha, Aguapé, Palizada y Cocha pasan a la 1.1).
- **D-010.** La especie nueva de la tropa de ese tramo no se estrena en el 323-325: una sola novedad por nivel.

---

## (e) Las otras variantes

| Variante | Puntos | Qué tenía de bueno | Por qué no se elige entera |
|---|---|---|---|
| **V1 · Tinaja y su cuia chica** | **23** | La que mejor recoge la idea literal: dos dosis reales y el dilema de mirar o enterrar (dentro de un intento). La justicia más fuerte (tope por tipo de nivel), el calendario mejor encajado (1.1 en el mundo 7, con práctica en p7) y la investigación cultural más cuidada | **Es la base.** Se corrige: capacidad 6 → 5; `partner` → por tipo; Pororoca deshabilitada → canon; puntuación «neutra» del bot → rama mínima; «barro» fuera de los textos; «lo guardaba la casa» → solo agua; cordón → jirau; cierre seguro → paseos |
| **V3 · Tinaja de la casa** | 22 | La más barata (unas 50 líneas y un solo gancho) y la que más reutiliza: Aguas turbias, Cuia alta de 5 y Cuia chica sin cambios. Sin cambios en el bot. Criterio «S0 sin trampas» y la mejor trama | Las «dos porciones» casi desaparecen: con la regla de color y T4, a la Cuia chica nunca le cabe más de una dosis de la tinaja. Si el jugador usa la Cuia chica como búfer, la tinaja queda bloqueada. Estrenaba en el 263 (mundo 6: mucawa y chicha) y movía la Palizada sin preguntar. Con 2 vacíos y K = 6, solo 10 de 16 tableros tienen ruta segura |
| **V2 · Tinaja grande** | 15 | La más fiel a «grande»; el bloque de 2 dosis como unidad mantiene ≤ 24 realidades incluso con 12 dosis; ruta segura en 110 de 110 tableros; la compuerta de Morpho | Rompe la regla de «1 hueco = 1 dosis» en el bucle caliente y en el JSON (`caps` en bloques), y crea dos fuentes de verdad para lo oculto (`murky` = false con ocultas). Morpho llega a 949 ms en un PC. Lo grande es lo fácil (bot 0,00-0,03). Ocupa 2 ranuras. Su trama («capas de otros días» y una burbuja) evoca fermentación |

**Si prefieres modificar lo que ya tenemos (V3).** Se puede hacer con la misma ficha y estos cambios:
- **Sin `cucharon`:** la tinaja (`{"type":"tinaja"}`) solo vierte en una Cuia chica de 2 sin mecánica, con la regla de color. Esa cuia sigue siendo una Cuia chica normal.
- **Sin suelta ni jirau compartido:** la regla 8 desaparece, y la tinaja vacía queda inerte.
- **Bot sin cambios:** sacar a la chica vacía puntúa −16 con la regla congelada.
- **Coste:** core-dev 2-3 días y Unity 4-5. Ahorra unos 3 días de agente.
- **Pierdes:** las dos dosis, el dilema de mirar o enterrar y la recompensa de la suelta. Además el generador rechaza más tableros (10 de 16 con K = 6 y 2 vacíos, frente a 16 de 16).
- **Igual que en la recomendada:** capacidad 5, ≤ 4 ocultas, tope por tipo de nivel, estreno en el 323, Pororoca según el canon, textos sin «barro», solo agua y la misma cultura.
- Por eso queda mejor como **variante posterior** («Cuia de la casa», d.4).

---

## (f) Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| **Choca con el canon de lo oculto:** T1 (solo en Difícil desde el 100), T3 («≤ 3 dosis, nunca llena»), Q17 A («el contenido se ve») y Q14 A con 03 §8 (ninguna oculta en niveles con mecánicas con disparador; la suelta es una liberación) | Q-Ti1. Tope de ocultas por tipo de nivel, ruta segura obligatoria, una sola fuente de ocultas por nivel y una suelta que no depende de lo oculto |
| **Sensación de azar,** la queja n.º 1 del género [V M1, M4] | La boca visible, el «×n», T4 explicado en el tutorial, Luciérnaga, Morpho honesta y el interruptor de la receta. Medir los intentos medios del estreno: alarma por encima de 2,5 (PM c.7) |
| **Falso bloqueo (R14)** con su cuia llena [x, x] | Exenciones de `futil`, el argumento de Φ y P1-P3 antes del estreno |
| **Poda de fútiles con capacidades mixtas** (C1-8): todo nivel con tinaja acaba con una Cuia chica de 2 suelta, y la poda puede dar «irresoluble» a un tablero que se gana | Búsquedas sin poda en niveles con tinaja (o poda sin el caso cap 4 → cap 2); A* con poda frente a exhaustiva antes del estreno; o decidir antes 02 R16 (C1-9) |
| **Vaivén útil tras la suelta:** R13 no declara Aguas estancadas en un nivel ya perdido | Es la incompletitud conocida de R13 (C1-2). Nota [P] en 02 R13 (estado canónico repetido) y test en P3 |
| **La ruta segura solo cubre la ruta** | Paseos seguros y casuales en el generador (c.3): 1.240 de 1.240 paseos seguros ganados y 0 estados de adivinar en 840 paseos casuales. Morpho con su orden de respuestas |
| **Morpho que manda comprar una Luciérnaga inútil** en una posición perdida | Orden de respuestas de b.2: «no hay salida» primero, Luciérnaga solo si alguna realidad tiene solución y nunca con R = 1 |
| **Coste del solver con 2 vacíos y K ≥ 7** | Con el generador de c.3, K = 7 con 2 vacíos pasa 15 de 16; aun así, 2 vacíos solo en comodidad, porque el bot no falla. El término heurístico opcional, solo con tests de consistencia. Buscar sin poda puede costar más nodos: medirlo |
| **Morpho en el teléfono** (hasta 55 ms en un PC con K = 8) | Compuerta de nodos en level-qa, calibrada en un teléfono de clase A con IL2CPP; hasta entonces, factor conservador sobre el PC. Si no cabe, 5 dosis solo con K ≤ 7 o retos de 4 dosis. Si aun así se agota, Morpho lo dice y no cobra |
| **Dependencias del cliente** (Morpho en el cliente, corte por tiempo, Luciérnaga, Pororoca, tarjeta, dedo guía) | Declaradas en c.7, fuera de los 16-23 días. Q-Ti3 A condicionada a que Morpho y la Luciérnaga estén listos |
| **Todo lo que lee «hay ocultos» como Aguas turbias** (level-qa, informe, unos 15 scripts de Unity) | Inventario en c.5 y c.6. Tope propio de ocultas para la tinaja, independiente de `lvl_murky_max_hidden_units` |
| **Con la puntuación congelada, la tinaja sola rara vez llega a Difícil** (con el generador de c.3: máx. 0,48 con K = 8 y 5 dosis; 0,82 en 1 de 16 tableros con K = 7; ninguno al 0,90) | Los retos con tinaja usan K alto (8-9), 1 vacío y una combinación, o se publican sin chip (PM c.2). El hito 340 irá probablemente sin tinaja. Recalibrar con el soft launch (03 §6) |
| **Espiar sacando y reiniciando:** Volver a la orilla con menos de 8 vertidos es gratis y conserva lo revelado | Q-Ti6. Con A, medir en el soft launch los reinicios con menos de 8 vertidos en niveles con tinaja |
| **Pororoca con la tinaja:** la reconstrucción inversa no tiene «estado resuelto parcial» mientras la tinaja tenga dosis | Pareja fija, solo dosis libres y verificación con la semántica de la tinaja. Si falla a menudo, deshabilitarla mientras la tinaja tenga dosis, como con la Lluvia y la Piraña |
| **Gesto al revés:** tocar su cuia y luego la tinaja | Si su cuia está vacía no se puede seleccionar (C2). Medir en la prueba con jugadores. Si es frecuente, aceptar «su cuia vacía → tinaja» como atajo de interfaz para sacar, como excepción explícita a C2 en 02 §3.1, sin tocar el núcleo, porque ese vertido nunca es legal |
| **Espacio:** el alto de la Cuia alta de 5 y la pareja en una fila | Como mucho 12 cuias con tinaja; regla de layout y tests a 360 × 780 y 360 × 640. P12 sigue abierta |
| **Confusión de color** | #C4906E-#C7A182 (comprobado: ΔE00 ≥ 13,7 frente a los tintes y ≥ 15,5 frente a #8A7B6A). Vetado #B5653A |
| **Trade dress de MS** (caldero) | Silueta alta y lisa, nunca morada, sin vapor ni burbujas. Capi nunca la usa |
| **Cultura:** en el Perú y Ecuador, las tinajas y vasijas evocan masato, chicha, kené y usos rituales y funerarios | Vetos con fuente, un diseño neutro en todos los mundos, estreno en el mundo 7, ningún Dato del río sobre tinajas en los mundos 5, 6 y 9, y el asesor |
| **La escena del masato:** tinaja + bebida guardada + cuia (pate) que entra y saca, con estreno en un mundo peruano | Solo agua; verbo «sacar», nunca «servir»; nadie recibe lo sacado; sin hoja en la boca [V T20]. El gesto y el estreno van al asesor como punto expreso; si confirma el riesgo, Q-Ti7 |
| **Evocar fermentación** | Solo agua de la casa; sin «refrescos», «días», burbujas, vapor ni hoja en la boca |
| **La pareja se lee como una Liana** (cordón que se desata) | Sustituido por el jirau compartido y la cuia colgada; nunca «atar», «desatar» ni «atada» |
| **Choque de nombres** (skin 9 «Tinaja»; alias retirados en 00 §5.3) | Q-Ti4 y el asesor |
| **Contrato y esquema** | `pack_schema` 3, aviso al tech-lead y al dueño; un build antiguo rechaza el pack 3 (08 §4.4) |
| **Alivio `minus_color`** | Nunca quita una fruta de la tinaja; las variantes repiten la ruta segura y los paseos |
| **Retraso de la 1.0** | Carriles de C# sin Unity; Unity después de la ola 5; salida a la 1.1 si no está lista al generar 201-700 o faltan Morpho y la Luciérnaga |
| **Anunciar «el primero del género»** | No se afirma hasta grabar 2-3 niveles de Water Loop Out (IEC, 2026), cuyos «mystery tubes» no tienen reglas publicadas [V M6] |
| **Tiempo de generación** (ruta segura hasta 11 s y paseos unos 3 s por nivel con K = 8) | Es offline y paralelizable |

---

## (g) Preguntas cerradas al dueño

| # | Pregunta | Opciones | Recomendación | Qué toca |
|---|---|---|---|---|
| **Q-Ti1** (decisiva) | ¿Aceptas la tinaja con contenido oculto desde su estreno, en niveles Normales, como excepción a T1, a T3, a Q17 A («el contenido se ve») y a Q14 A con 03 §8 (nada oculto en niveles con mecánicas con disparador, y la suelta de su cuia es una liberación)? | A: sí, con tope por tipo de nivel (2 ocultas en comodidad y práctica; 3-4 en retos e hitos), ruta segura obligatoria y una suelta que solo depende del recuento visible. B: solo como Aguas turbias, en Difícil o Muy difícil | **A.** Con B no puede estrenarse (PM c.3, regla 6: nada debuta en un reto ni en un hito) y saldría en menos de un tercio de los niveles del ciclo: dejaría de ser una seña propia. Q17 A y Q14 A siguen valiendo para todas las demás mecánicas | 02 §11, 03 §8, contrato 8, `LevelValidation.MurkyError` |
| **Q-Ti2** | ¿Su cuia chica recibe cualquier fruta de la tinaja, o solo la misma? | A: cualquiera, dos dosis en capas, como en tu idea. Exige la rama mínima del bot (+16 solo si coincide la fruta). B: solo la misma (regla normal). Por T4 solo cabe una dosis de la tinaja cada vez, y el bot no cambia | **A.** Sin ella, las «dos porciones» de tu idea desaparecen. Tiene el mismo coste para el solver (ruta segura 16/16 en los dos casos) y crea la decisión de mirar o enterrar | 02 §1.2, 03 §6 |
| **Q-Ti3** | ¿En qué versión entra? (el estreno es en el 323 en los dos casos) | A: en la 1.0, porque 201-700 aún no están generados, si la tinaja pasa sus pruebas y Morpho en el cliente y la Luciérnaga están listos; si no, pasa a B. B: en la 1.1, con build, `level_overrides` y regla de migración | **A**, con salida a B. La seña propia llega al lanzamiento y la mide el soft launch; los packs 11-200 no cambian | Q8 (c) de D-007, 08 §4.4, 09 |
| **Q-Ti4** | Nombre y skin 9 | A: la mecánica se llama «Tinaja»; la skin 9 (Ucayali, territorio shipibo) se renombra y se revisa con el asesor. B: la skin conserva «Tinaja» y la mecánica se llama «Tinajón» | **A.** El riesgo cultural de esa skin existe de todos modos [V T10]. Ojo: el glosario ya retiró «tinaja» dos veces como alias («+1 tinaja» y «tinaja de reserva» son la Cuia extra y la de rescate; la alcancía «Tinaja» es el Cántaro, 00 §5.3) | 00 §4, §5.3, §6.5 |
| **Q-Ti5** | ¿La tinaja enseña qué frutas guarda, sin el orden? | A: no; solo la boca y «×n», como en tu idea, con el interruptor `ui_tinaja_show_contents` = false para encenderlo si el soft launch muestra quejas de suerte. B: sí desde el estreno | **A.** Al solver no le cambia nada: ya se deduce de `units_per_color` | 08 §9, Unity |
| **Q-Ti6** (nueva) | Volver a la orilla con menos de 8 vertidos es gratis y lo revelado sigue revelado: se puede destapar la tinaja sacando y reiniciar sin coste. ¿Lo aceptas? | A: sí, como ya pasa con Aguas turbias (R7, T6, I5); el dilema «mirar o enterrar» queda dentro de un intento y se miden en el soft launch los reinicios con menos de 8 vertidos en niveles con tinaja. B: lo revelado al sacar se pierde al Volver a la orilla; solo persiste lo de la Luciérnaga y la Pororoca | **A.** Favorece la justicia, es coherente con Aguas turbias y no añade una regla que el jugador no ve. B exige una excepción a T6 e I5 y tocar `LevelSession` y `h2`. En los dos casos hay que definir «intento» en T6 | 02 §11 (T6), 04 §2.7 y §6.1, `LevelSession` |
| **Q-Ti7** (solo si el asesor confirma el riesgo) | El gesto propio (la cuia entra en la tinaja y sale llena) puede evocar el masato. ¿Qué hacemos? | A: mantener el gesto con el encuadre «solo agua», sin servir a nadie. B: la tinaja se inclina y vierte en su cuia, como un vertido normal | **A.** Es la firma visual que nos separa de MS, y el encuadre «solo agua» quita la bebida de la escena. B pierde la seña sin cambiar las reglas | 02 §4.1, 07 §5, Unity |

La Pororoca no necesita pregunta: sigue el canon con la pareja fija. Si en el pipeline falla a menudo, el tech-lead la deshabilita mientras la tinaja tenga dosis, como con la Lluvia y la Piraña [P].

---

## (h) Lista para el asesor cultural

**Puntos expresos**
1. **El gesto de sacar** (la firma visual): la cuia chica entra por la boca de la tinaja y sale llena. En la Amazonía peruana, tinaja + bebida guardada + pate que saca es la escena del masato [V T20, T10, T11], y el pate es la cuia (00 §4). Con el encuadre «solo agua», sin servir a nadie, ¿es aceptable? Si no lo es, Q-Ti7.
2. **El estreno en un mundo peruano** (7, Madre de Dios): ¿se presenta bien con un dato de Pará, sin base local [NV]?

**Nombres**
1. ES «Tinaja».
2. PT «Talha» frente a «Pote d'água».
   - Priberam, un diccionario de Portugal, da para «talha» el sentido de cántaro grande para el agua [V T15]. También significa talla de iglesia y es una forma de «talhar».
   - En Pará se dice «pote» [E T3], pero «Pote de Barro» es el nombre PT del Cántaro.
3. EN «Water Jar» («Clay Jar» es el Cántaro).
4. Los nombres PT y EN de la Cuia chica: «cuia pequena» o «cuinha» [E], y «small cuia».
5. «Cuia pitinga», solo como guiño PT en un Dato del río.
6. «Jirau»: solo para la plataforma de varas, nunca para un pie [V T21].

**Usos y riesgos**
- Si en el Perú y en Colombia «tinaja» evoca más el masato que el agua.
- La skin 9 «Tinaja» en Ucayali (territorio shipibo): renombrarla y revisar su diseño.
- Que ninguna tinaja jugable recuerde el chomo shipibo, las tinajas kukama o la mucawa kichwa.
- Si «sacar agua» / «tirar água» / «scoop the water» suena natural en los tres idiomas.
- Si su cuia colgada del jirau junto a la tinaja (como las cuias pitingas de T1) choca con algún uso real.
- La etimología «Kuimbúka»: confirmarla en Navarro antes de usarla.

**Datos del río y crédito**
- Los cuatro candidatos de b.2: Aritapera, Quatipuru-Mirim, el botijo (dato general) e Icoaraci.
- Cómo nombrar y acreditar a las fazedeiras y a los oleiros, y si conviene encargar referencias con pago, como se propuso con ASARISAN.

**Bebidas:** la tinaja solo guarda agua; las frutas son tintes (D-010). Lista negra: masato, chicha, caxiri, cauim, aguardiente, licor de jenipapo, «vinho», «poción», «refresco» y «jugo» como contenido de la tinaja.

**Arte:** el diseño neutro (sin kené, marajoara ni tapajônica, sin cara, dos bocas, cruz ni tamaño de urna, sin hoja en la boca, sin cuencos que pasan de mano en mano, sin cordón entre las piezas) y la distancia frente al caldero de MS.

**Nombres de hito [P]:** «Lo que entra no sale» para un hito Tinaja + Matapi.

**Pendiente de verificar antes de escribir textos**
- El uso actual de vasijas de barro en casas ribereñas del estado de Amazonas, de Loreto (casas mestizas), de Colombia y del Beni boliviano: sin fuente [NV].
- «Camucim» como urna funeraria [E].

---

## Anexo 1. Lo que encontraron los jueces y cómo se resolvió

| Problema | Juez | Resolución |
|---|---|---|
| V1: la capacidad 6 encoge el tablero sin motivo de juego | 1, 2 | Capacidad 5 con la silueta de la Cuia alta de 5 |
| V1, V2: «es de barro» en el tutorial choca con M5 | 1, 2, 3 | Textos sin «barro». Se dice «dosis», como en 02 §8 (revisión) |
| V1: la cuia que recibe cualquier fruta es una excepción difícil para el jugador casual | 1, 3 | Se mantiene, porque sin ella las «dos porciones» desaparecen (T4 + regla de color = una dosis cada vez). Las dosis quedan en capas, como en toda cuia. La regla de color queda como Q-Ti2 B y como variante «Cuia de la casa» |
| V1: cambia la puntuación congelada del bot | 1, 2 | Solo la rama mínima (+16 si coincide la fruta), registrada en D-011 |
| V1: `partner` obligatorio y códigos con huecos | 2 | Sin `partner` (una tinaja por nivel); códigos 8 y 9 |
| V1, V2: Pororoca deshabilitada contra T6 y 04 §2.4 | 1, 2 | Canon: revela, y la tinaja y su cuia quedan fijas (con la fila que falta, revisión) |
| V1: el cierre seguro está truncado | 1, 2 | Se sustituye por paseos seguros y casuales (revisión) |
| V1: hay que garantizar la contigüidad de la pareja | 1 | Regla de layout y test |
| V1, V2, V3: faltaban los alias del glosario | 1, 3 | Añadidos a Q-Ti4 |
| V1: «lo guardaba la casa» y V2: «capas de otros días» evocan fermentación | 3 | Solo agua de la casa (revisión) |
| V2: una burbuja en el revelado | 3 | Un anillo de onda, sin burbujas |
| V1: el cordón no es una costumbre | 3 | Sustituido por el jirau compartido (revisión) |
| V1: el 323 queda lejos para una seña propia | 3 | Q-Ti3 A (en la 1.0) y un tablero en el vídeo de la tienda |
| V3: estreno en el mundo 6, donde veta la mucawa y la chicha | 1, 2, 3 | 323, mundo 7 |
| V3: excepción a T1 solo en el estreno, con salto a un reto con tinaja en p6 | 1 | Tope por tipo de nivel, práctica en p7 y p6 sin tinaja |
| V3: color sin calcular (ΔE00 de 5,3 a 9,0 frente a #8A7B6A) | 3 | #C4906E-#C7A182, comprobado |
| V3: «12 de 12 con falso bloqueo» era en realidad «sin vertido seguro» | 1 | Corregido en b.2 (`futil`) |
| V2: invasiva, Morpho fuera de presupuesto y la de 12 es fácil | 1, 2, 3 | Variante futura con condiciones; compuerta de Morpho en level-qa |
| V2: la receta por defecto contradice la idea del dueño | 1, 3 | Interruptor apagado (Q-Ti5) |
| Mercado: «somos los primeros» sin prueba | Investigación | No se afirma hasta grabar Water Loop Out |

## Anexo 2. Fuentes

Consultadas por los agentes de investigación el 2026-10-10. T20, T21 y las precisiones de T1, T7 y T10 las comprobaron ese mismo día los agentes de verificación, con el texto extraído de los PDF. Las demás no se han vuelto a abrir: las marcas son las de la investigación.

**Cultura**

| # | Fuente | Qué aporta | Marca |
|---|---|---|---|
| T1 | Carvalho, Bandeira y Rocha, *Aceno* 7(13), 2020. https://periodicoscientificos.ufmt.br/ojs/index.php/aceno/article/view/8582/7555 | Cuias pitingas en Aritapera (Santarém): balde, vaso y cuenco. Se cuelgan con alambre o fibra por dos agujeritos (foto 3); en el jirau, unas cuelgan y otras sirven para recoger y verter agua (foto 8) | V |
| T2 | O Liberal, 24-08-2025. https://www.oliberal.com/belem/da-arvore-a-tradicao-cuias-carregam-seculos-de-historia-e-resistem-no-cotidiano-da-amazonia-1.1009052 | La cuia en el día a día de Pará | V |
| T3 | Diário do Pará (DOL), 09-06-2015. https://dol.com.br/_/noticia-333265-tradicao-mantida-por-mulheres.html | «Kuimbúka»: la nota lo dice, pero la etimología sale de un periódico | E |
| T4 | Pacheco, Vieira y Costa, *Revista Cocar* 22, 2023. https://periodicos.uepa.br/index.php/cocar/article/download/6824/3251/30977 | El panelão de Quatipuru-Mirim es el depósito del agua de lluvia; no habla de jugos | V |
| T5 | Diário do Pará, 25-06-2024. https://diariodopara.com.br/encartes/na-estrada-com-o-diario/ceramicas-de-icoaraci-dao-charme-a-vila-sorriso/ | Olaria do Espanhol (1903), cerámica utilitaria de Icoaraci | V |
| T6 | Souza, tesis, UFSCar, 2023. https://repositorio.ufscar.br/handle/ufscar/17640 | Piezas lisas sin pintar, y también réplicas marajoaras y tapajônicas, en Icoaraci | V |
| T7 | Botijo (Wikipedia EN). https://en.wikipedia.org/wiki/Botijo | El barro sin esmaltar enfría el agua por evaporación. Solo eso: no habla de Brasil ni de la Amazonía, ni de guardar bebidas | V |
| T8 | ENEM 2000 (extracto). https://projetoagathaedu.com.br/resolucao-questoes-enem/quimica/termoquimica/15-enem-2000-ainda-hoje-e-muito-comum-as-pessoas-utilizarem-vasilhames-de-barro-moringas-ou-potes-de-ceramica.php | Potes de barro para el agua en Brasil | E |
| T9 | Portal Amazônia. https://portalamazonia.com/amazonia/paradoxo-na-amazonia-chuvas-volumosas-e-agua-potavel-em-falta/ | Hoy las casas ribereñas recogen agua de lluvia | E |
| T10 | Belaunde, *Cerámica tradicional shipibo-konibo*, Ministerio de Cultura del Perú, 2019. https://museos.cultura.pe/sites/default/files/publicaciones/pdf/CER%C3%81MICA%20SHIPIBO%20KONIBO.pdf | Chomo (agua y masato), kené, maweta; los cuencos grandes de masato pasan de persona a persona (veto) | V |
| T11 | Taricuarima Paima, *Amazonía Peruana* XVII(34), CAAAP, 2021. https://amazoniaperuana.caaap.org.pe/index.php/amazoniaperuana/article/download/257/255/467 | Tinajas kukama para masato, agua y aguardiente; karuara (veto) | V |
| T12 | Princeton University Art Museum. https://artmuseum.princeton.edu/art/collections/objects/126614 | Mucawa canelos-quichua y bebida ceremonial (veto) | V |
| T13 | pt.wikipedia «Igaçaba» (redirige a Marajoaras) https://pt.wikipedia.org/wiki/Iga%C3%A7aba ; Dicio https://www.dicio.com.br/igacaba/ | Igaçaba también es urna funeraria (veto) | V (redirección) / E (diccionario) |
| T14 | pt.wikipedia «Cerâmica marajoara». https://pt.wikipedia.org/wiki/Cer%C3%A2mica_marajoara | Piezas funerarias y rituales (veto) | V |
| T15 | Priberam, «talha». https://dicionario.priberam.org/talha | Cántaro grande para conservar el agua; otros sentidos | V |
| T16 | PromPerú, 13-02-2025. https://peru.info/en-us/gastronomy/news/2/12/summer-2025-in-peru-the-best-soft-drinks-to-enjoy | Aguajina y camu camu, sin alcohol (prueba que existen; no que se guarden en tinajas) | V |
| T17 | BBC en El Observador, 17-06-2025. https://www.elobservador.com.uy/8-jugos-fruta-que-solo-puedes-saborear-la-amazonia-n6004468 | Ocho jugos amazónicos y los que también se fermentan (prueba que existen; no que se guarden en tinajas) | V |
| T18 | Chacón Mayorga et al., *Food Science and Technology*, 2021. https://www.scielo.br/j/cta/a/WVLctQ9TQfSbN3vCtbcPQ6r/?lang=en | Chicha, masato, caxiri y cauim son alcohólicos (veto) | V |
| T19 | Portal Amazônia, 27-10-2025 https://portalamazonia.com/gastronomia/vinho-de-acai-biotecnologia-para/ ; Embrapa Amapá, 2002 https://www.alice.cnptia.embrapa.br/alice/bitstream/doc/349708/1/AP2002avaliacaoquimicaacai.pdf | «Vinho» de açaí: alimento básico, y desde 2025 también una bebida fermentada al 12 % | V |
| T20 | Daggett, «Las funciones del masato en la cultura chayahuita», *Anthropologica* (PUCP). https://revistas.pucp.edu.pe/index.php/anthropologica/article/download/6314/6358 | Masato en tinajas de fermentación tapadas con hojas de plátano; se saca con un pate; los niños beben en pates pequeños. Es de Loreto (chayahuita), no de Madre de Dios (veto) | V |
| T21 | Dicio, «jirau». https://www.dicio.com.br/jirau/ | Plataforma de varas sobre horquetas para guardar utensilios | V |

**Mercado**

| # | Fuente | Qué aporta | Marca |
|---|---|---|---|
| M1 | Reseñas de Magic Sort en la App Store del Reino Unido y de EE. UU. https://apps.apple.com/gb/app/magic-sort/id6499209744?see-all=reviews ; https://apps.apple.com/us/app/magic-sort/id6499209744?see-all=reviews | Lo oculto es la queja más repetida («suerte») | V |
| M2 | Gamigion https://www.gamigion.com/game-deconstruction-magic-sort-by-grand-games/ ; Deconstructor of Fun, 2026-02 https://www.deconstructoroffun.com/blog/2026/2/6/sort-puzzles-how-a-new-subgenre-is-born | En MS la suerte pesa demasiado; su éxito es la presentación | V |
| M3 | Rollic, Bus Jam. https://rollic.helpshift.com/hc/en/13-bus-jam/faq/562-what-are-the-obstacles/?p=ios | El túnel suelta de uno en uno; el túnel de vidrio deja planificar | V |
| M4 | Water Sort Puzzle (IEC), reseñas negativas. https://appsupports.co/1514542157/water-sort-puzzle/negative-reviews | Quejas por solo ver la capa de arriba y por el tubo extra de 1/4 | V |
| M5 | Mega Sort Adventures. https://apps.apple.com/hn/app/mega-sort-adventures/id1551197535 | «Hidden Tube Sort»: tubos opacos, sin reglas exactas | E |
| M6 | Water Sort! 3D https://apps.apple.com/app/id6758611499 ; Water Loop Out https://mwm.ai/apps/water-loop-out/6762354402 | «Blind bags» y «mystery tubes», sin reglas publicadas | V (ficha) |
| M7 | BallJet. https://apps.apple.com/app/id6760736849 | Tubos de capacidad reducida y de un solo sentido | V |
| M8 | Búsqueda de pote + cucharón: Color Pipets Jam https://appgoblin.info/apps/6763053986 ; Kitchen Sorting https://phaser.io/news/2024/10/kitchen-sorting ; Sort Potion Mixery https://arcticapes.itch.io/sort-potion-mixery | Ningún water-sort con un recipiente que solo se usa a través de otro dedicado. Es ausencia de evidencia | NV |

**Repositorio:** `docs/research/magic-sort/ficha-magic-sort.md` y `c-meta-liveops-ux.md` (el caldero de MS); `docs/gdd/00`, `02`, `03`, `04`, `07` y `08`; `docs/GDD.md` §2 y §7; `docs/dev/traspaso/README.md` y `planes-integracion.json`; `Packages/com.amazonfill.core/CHANGELOG.md` (C1-2, C1-8, C1-9) y `README.md`; `Packages/com.amazonfill.core/Runtime` (`Cuia.cs`, `MechanicType.cs`, `IMechanic.cs`, `BoardRules.cs`, `LevelValidation.cs`, `LevelSession.cs`, `CasualBot.cs`, `SolverMoves.cs`, `SafeMoves.cs`, `RealityEnumerator.cs`); `tools/level-qa/Checks` y `tools/level-generator/Generation/CampaignReport.cs`; `Assets/_Project/Scripts` (`BoosterTray.cs`, `RescueBooster.cs` y los lectores de `hidden`/`murky`); `schemas/af-level-1.schema.json`.

## Anexo 3. Correcciones de la revisión (2026-10-10)

Cada hallazgo se comprobó contra el GDD, el código de la rama y, cuando hacía falta, con una copia del núcleo o un modelo de reglas en el scratchpad.

| # | Hallazgo | Gravedad | Qué cambió | Dónde |
|---|---|---|---|---|
| 1 | Morpho mandaba comprar una Luciérnaga en posiciones perdidas (también con R = 1) y «Morpho enseña esa ruta» era falso | Alta | Orden de respuestas: «no hay salida» primero, Luciérnaga solo si alguna realidad tiene solución, tiempo agotado al final; Morpho calcula al momento | b.2 Morpho, (a) 4, c.2, c.8 3, c.10, (f) |
| 2 | La trama juntaba tinaja, bebida guardada, cuia que «sirve» y un mundo peruano: la escena del masato; las fuentes no apoyaban «refrescos» | Alta | Solo agua; verbo «sacar»; nadie recibe lo sacado; vetos nuevos; T20 y «sin hoja en la boca» a [V]; punto expreso del asesor y Q-Ti7 condicional | (a) 5, b.2 Cultura, Aspecto, Tutorial, d.1, (f), (g), (h), anexo 2 |
| 3 | Volver a la orilla gratis con menos de 8 vertidos y lo revelado persiste: espiar no cuesta una Marea | Media | Texto corregido, dilema «dentro de un intento», Q-Ti6 y definición de «intento» | b.2 Función y Marea, (f), (g), c.10 |
| 4 | La suelta es una liberación (Q14 A, 03 §8) y Q-Ti1 no pedía esa excepción | Media | Excepción añadida a Q-Ti1 con su justificación; texto propuesto para 03 §8; test | b.2 R10 y Combinaciones, (g), c.10 |
| 5 | Con capacidades mixtas, la poda de fútiles puede dar «irresoluble» a tableros que se ganan (C1-8), y pasa sin Liana | Media | Búsquedas sin poda en niveles con tinaja; contraejemplo; prueba antes del estreno | b.2 R14, c.2, c.3, c.6, (f) |
| 6 | El cierre seguro (BFS-300) solo miraba 4-6 vertidos de profundidad | Media | Sustituido por paseos seguros y casuales, con cifras medidas | (a) 4, c.3, c.8 4, c.9, (f) |
| 7 | La compuerta de Morpho se calibraba en un teléfono de gama alta | Media | Calibrar en clase A con IL2CPP; factor conservador; 5 dosis solo con K ≤ 7 si no cabe | c.3, c.9, (f), b.2 Tope |
| 8 | Reutilizar `hidden`/`murky` rompe level-qa (Codes, Instances, CapacityCaps), CycleChecks, el informe y unos 15 scripts | Media | Inventario en c.5 y c.6; tope propio de ocultas; +1-2 días | b.2 regla 1, c.3 6, c.5, c.6, c.7, (a) 7 |
| 9 | La justicia y la experiencia se apoyan en piezas del cliente que no existen | Media | Dependencias declaradas fuera de la cuenta; Q-Ti3 A condicionada | c.6, c.7, c.8 7, d.2, (g) |
| 10 | Las cifras de c.9 salían de otro generador (cualquier capa, cap 6) | Media | c.9 rehecha con el generador de c.3; regla de vacíos revisada; plan de retos e hito ajustado | c.2, c.3 2, c.9, d.3, d.5, (f) |
| 11 | El cordón que se desata y Marea que «vuelve a atar» son la imagen de la Liana; «jirau» usado como pie | Media | Jirau compartido y cuia colgada; sin «atar/desatar/atada»; jirau solo como plataforma | Toda la ficha, Aspecto, animación, (h), anexo 2 (T21) |
| 12 | «Dosis no es palabra de la interfaz» era falso (02 §8) | Media | Se dice «dosis»; «porción» solo en la cita del dueño | Todo el documento |
| 13 | El argumento de R14 olvidaba que vaciar su cuia exige un destino | Baja | Reescrito: con la cuia colgada, R13 solo se declara sin vertidos legales | b.2 R14 |
| 14 | Φ no dice nada del retraso del modal ni del vaivén tras la suelta | Baja | Documentado, nota [P] para R13 y test de P3 | b.2 `futil`, c.6, (f) |
| 15 | La aritmética de vacíos contaba la tinaja y su cuia; `LevelValidation` solo excluía la tinaja de una comparación | Baja | Fórmula sin la pareja; las dos comparaciones | b.2 regla 1, c.1, c.3 2, c.5, c.10 |
| 16 | Faltaba 04 §5.1 en c.10 | Baja | Añadida con el ejemplo y los tests | c.10 |
| 17 | No se fijaba dónde va el gancho en `Accepts` | Baja | El primero, con dos tests | b.2 `legal`, c.1, c.6 |
| 18 | «Pororoca: no hace falta excepción» dejaba sin definir el barajado y la reconstrucción | Baja | Pareja fija, dosis libres, fila en 02 §9.2 y 03 §9, alternativa [P] | b.2 Pororoca, (f), (g), c.10 |
| 19 | «Tocar la tinaja y luego otra cuia es un rechazo» ignoraba C5 y C2 | Baja | Regla 3 con C5 y C6; el atajo, como excepción a C2 | b.2 regla 3, animación, (f), c.10 |
| 20 | Sin etiqueta accesible ni C9 para la tinaja (02 §12) | Baja | Etiquetas propuestas | b.2 Accesibilidad, c.6, c.10 |
| 21 | «Contrato 10» era el auto-avance de victoria; Aguas turbias es el 8 | Baja | «Contrato 8» en las tres apariciones; añadido `_HiddenMask`/`_MurkyColor` (ver nota) | b.2 regla 1, (g) Q-Ti1, c.10 |

**Nota sobre el hallazgo 21.** El hallazgo pedía añadir «el contrato 28 (`_HiddenMask` y `_MurkyColor`)». En `docs/GDD.md` §7.2 esa es la **incidencia 28**, asignada al **contrato 27**; §2 #10 cita los contratos 8, 27 y 28 para Aguas turbias. Se cita como «contrato 27 (incidencia 28 de §7.2)».
