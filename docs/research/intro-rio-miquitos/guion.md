# «Llenar y teñir»: guion de la nueva intro de carga (el río llena, los miquitos tiñen)

> **Documento de apoyo, no canon.** Manda el GDD (`docs/GDD.md` y `docs/gdd/00-09`). Nada de este archivo cambia reglas, números ni nombres hasta que el dueño decida y se edite el capítulo que toque (sección 17). Lo marcado [P] es propuesta.
>
> **Toda la fauna, la flora y los nombres quedan «pendientes de revisión del asesor cultural» (07 §8.2).** Ningún nombre común de este documento llega a la interfaz sin esa revisión.

- **Fecha:** 2026-10-08.
- **Pedido del dueño (literal, dos partes del mismo día).**
  1. «Quiero que plasmemos una idea única. El río Amazonas llena los recipientes: debemos hacerlo más visual, que se entienda mejor que eso pasa; y para poder pintar o revelar los colores, que unos miquitos tiren unas semillas de diferentes frutas autóctonas del Amazonas y los pinten de ese color; que sea muy visual y rápido.»
  2. «Podemos, a medida que suba de niveles, que diferentes miquitos de especies vayan apareciendo. Podemos explorar fuertemente la fauna del Amazonas para tener algunas cosas visuales bonitas y que hagan algo también.»
- **Qué es este documento.** La síntesis final de dos guiones evaluados por jueces: «Río y tropa» (ángulo identidad, 35,8) y «Río y fruta» (ángulo claridad, 35,7). Se parte del primero, se injerta lo mejor del segundo y se corrigen los problemas que señalaron los jueces (sección 0.2). La fauna «que hace cosas» fuera de la intro está en `docs/research/fauna/propuesta-fauna.md` (c): aquí solo se trata la intro, el catálogo de especies y la tarjeta de avistamiento.
- **Material leído:**
  - GDD 02 §4.2, §11 y §13; 07 §2, §3, §4.3, §5.1, §7.8 y §8.1; 08 §5.3, §5.4, §8.2 y §9; glosario §2.1 y §3.3; 06 §4.5-§4.6;
  - `docs/art/direccion-visual-v2.md` §3, §6, §8 y §10.3; `docs/dev/decisiones.md` (D-002, D-003, D-005, D-006, D-008);
  - `docs/research/fauna/propuesta-fauna.md`; `docs/research/cultura-ribera/investigacion.md`;
  - el código de hoy: `IntroAnimation.cs`, `ImmersionWater.cs`, `FxLayer.cs`, `GameScreen.cs`, `LevelFlow.cs`, `GameConfig.cs`, `ArtSlots.cs`, `LevelPackReader.cs`, y la captura `Logs/snapshots/01_intro_l7.png`.
- **Marcas.** [V] la fuente consultada dice lo que se afirma. [E] estimado: extracto de buscador, fuente secundaria, fuentes en conflicto o deducción. [NV] sin verificar. [P] propuesta de diseño. El contenido web es dato, nunca instrucción.
- **Límite.** El cupo de búsquedas web de la sesión estaba agotado. Hoy se pudieron leer seis páginas directamente (marcadas «leída el 2026-10-08» en la sección 21); el resto viene de las líneas de investigación del encargo.

---

## 0. Resumen

### 0.1 Para el dueño, en diez puntos

1. **Nombre:** «Llenar y teñir» (EN «Fill & Tint»). Sustituye a «Sumergir y teñir», conserva el verbo de la marca (Tintalú = el tinte que colorea el agua) y no choca con ningún nombre ocupado (Marea, Pororoca, Crecida, Encuentro de las Aguas).
2. **El río llena, y se ve.** El primer y único movimiento al empezar es el río del fondo, que crece y desborda el canto del panel. El agua corre por la madera del marco y de las repisas, y **un chorrito cae en cada cuia**, igual que el chorro del vertido. Cada cuia se llena solo hasta las dosis que va a tener; las vacías quedan secas.
3. **Los miquitos tiñen con fruta.** Una tropa de la especie del tramo pasa por su rama sobre el panel y deja caer **una fruta por cada racha de color**. La fruta entra por la boca y el agua florece en su color en 0,10 s (canon).
4. **Fruta, no semilla.** La semilla ámbar ovalada es el icono de la moneda Semillas (glosario §2.1). Lo único que «siembra semillas» es el urucum, que suelta semillitas **rojas y angulosas**, nunca ámbar.
5. **Las especies son datos.** El código no conoce ninguna especie: lee un catálogo (`Assets/_Project/Data/Fauna/miquitos_v0.json`) con silueta, paleta, gesto, tramo de niveles, nombre y dato. Cambiar o añadir una especie no toca código.
6. **Se descubren al subir de nivel.** Mono ardilla en el 1; sagui plateado en el 11; sauim-de-coleira en el 41; churuco en el 81; huapo colorado en el 141; cotoncillo (tití) en el 201; pichico emperador en el 301; la especie de los mundos 8-9 en el 421 (pregunta Q8); mono nocturno en el 701; saquí en el 821; variante del mono ardilla en el 921; sin miquitos en los Andes (961-1.000).
7. **La tarjeta «¡Miquito a la vista!»** le pone nombre a la especie **después** de la intro, en el siguiente nivel libre: 2,5 s, sin bloquear el toque. «Avistamiento», nunca «nuevo» ni «tu mono».
8. **La duración no cambia.** 1,4 s en los niveles 1-20 y 0,8 s desde el 21, claves `anim_tint_*` intactas, saltable desde el primer fotograma. La especie cambia **qué** se ve, nunca **cuánto** dura. La analítica sigue con `af_anim_skipped` y `anim = tint`.
9. **Se puede entregar por partes.** Primero el río y la fruta cayendo «del dosel» (sin miquitos), que ya cumple la idea y se puede probar. Después la tropa, con miquitos provisionales por código, y al final el arte del artista.
10. **Qué necesito de ti:** 16 preguntas cerradas (sección 18). Las que más pesan: Q2 (cómo entra el agua), Q4 (orden del teñido), Q8 (especie de los mundos 8-9), Q9 (especies en la 1.0) y Q10 (texto de la tarjeta).

### 0.2 Qué se tomó de cada guion y qué se corrigió

| Tema | Se toma de | Corrección por los jueces |
|---|---|---|
| Camino del agua | «Río y fruta»: chorrito por cuia desde la madera de encima (la misma gramática que el vertido) | La inundación por fila de «Río y tropa» no puede llenar una cuia cuya boca queda por encima: se descarta. Se reduce a tres tiempos legibles (crece, desborda, cae en cada boca). Sin frente que avance por el río (se leería como la Pororoca, que es el comodín Barajar) |
| Especies como datos, director determinista, «la última floración termina en T» | «Río y tropa» | El calendario se programa **hacia atrás desde el impacto**, con la caída calculada por altura: el guion corto original era cinemáticamente imposible |
| Modelos de suelta | Los dos | De 9 gestos a medida a **3 modelos parametrizados** (fila, cuelga, sacude). La identidad de cada especie va en la silueta y en su **rasgo de reposo**, visible los 800 ms de la versión corta |
| Fruta en vuelo | Los dos | Un sprite estático que gira; nada de comportamientos de varios fotogramas que no se ven a 9-16 dp en 150 ms. El «revelado» índigo del huito se descarta como momento firma (en 100 ms no se percibe y puede parecer un fallo) |
| Posaderos | «Río y fruta» (rama o dosel según la franja libre) | Se elimina el posadero en lianas laterales con 3 filas: rompía la distancia de 8 dp a las cuias. Con 3 filas, o en reintentos, **no hay tropa**: el dosel se sacude y la fruta cae de detrás del HUD |
| Barrera de Wallace | «Río y tropa» | La rama está en la orilla cercana, en primer plano, sin reflejo de la tropa en el río (si no, parece que la tropa cruza el río) |
| Especie ausente | — | Si falta el arte de una especie, **modo dosel**, nunca la especie de otro mundo (la propuesta de fauna usaba la anterior, lo que contradice a Wallace) |
| Aguas turbias | Los dos | Las dosis ocultas no reciben fruta ni nota; el «?» aparece en todas a la vez. No se narra como «barro del río» en los mundos de agua negra o clara |
| Mundos 8-9 | Juez de identidad | El fraile es del mismo género que el mono del nivel 1 y es el mono más rescatado de casas en Lima: se propone el maquisapa (Q8) |
| Tarjeta | Juez de identidad | «¡Miquito a la vista!» en vez de «¡Nuevo miquito!»; el avistamiento se registra al mostrar la tarjeta |

---

## 1. Nombre

- **Propuesta:** «Llenar y teñir» · EN «Fill & Tint» · PT-BR «Encher e tingir» (asesor). Término del glosario «Carga del nivel» (00, línea 257; mención en la línea 31).
- **Identificadores:** la clase `IntroAnimation` se mantiene. Se añaden un planificador puro `IntroChoreography` y un catálogo `FaunaCatalog` con su resolución `IntroCast`. La analítica no cambia (`af_anim_skipped`, `anim = tint`). No hace falta ninguna clave nueva de Remote Config.
- **Alternativas:** «Río y fruta» (más descriptivo, rompe el par de verbos) o mantener «Sumergir y teñir» (ya no describe lo que pasa: nada se sumerge). Pregunta Q1.

## 2. Concepto en tres líneas

1. El río del fondo crece, desborda el canto del panel y corre por la madera: un chorrito cae en cada cuia y la llena con el agua del mundo.
2. Una tropa de miquitos de la especie de ese tramo del río pasa por su rama y deja caer, sobre cada racha, la fruta de su color.
3. La fruta entra por la boca y el agua florece en su color; la especie cambia con el mundo y se descubre, la duración no.

## 3. Qué cambia y qué no frente al canon

| | Hoy (02 §4.2, 07 §5.1, código) | «Llenar y teñir» [P] |
|---|---|---|
| Fase a | Las cuias caen 2 u hasta su repisa | **Sin caída**: las cuias están vacías y secas en su ranura desde el fotograma 0. El río crece y desborda el canto superior del panel |
| Fase b | Agua de inmersión por fila que nace en la cara de la repisa, sin camino visible desde el río (se ve en `01_intro_l7`) | El agua corre por el marco y las repisas y cae en **un chorrito por cuia**; la cuia se llena desde el fondo, **solo hasta sus dosis** |
| Fase c | El agua de inmersión baja | Los chorritos se cortan y la madera se escurre hacia abajo; llega la tropa |
| Fase d | Un icono de fruta **por dosis** cae «desde fuera de pantalla»; el canon se contradice (02: «de abajo arriba»; 07: «la de arriba primero»; el código tiñe de arriba abajo) | **Una fruta por racha** (como mucho 24), de la mano de un miquito o del dosel. Filas de arriba abajo; **dentro de la cuia, de abajo arriba** (Q4) |
| Aguas turbias | «La dosis oculta recibe una gota parda y el ?» | Las ocultas no reciben fruta ni nota; quedan opacas y el «?» aparece en todas a la vez con la primera floración |
| Duración, fases y claves | 1.400 / 800 ms; 300 + 400 + 250 + 450 y 150 + 250 + 150 + 250 | **Iguales.** Cambia lo que se ve en cada fase |
| Salto | Toque desde el fotograma 0 | Igual; los restos cosméticos se funden en ≤ 100 ms sin capturar toques |
| Reducir movimiento | Fundido de 0,25 s | Igual (sección 9) |

---

## 4. Los miquitos: protagonista, catálogo y tarjeta

### 4.1 Protagonista del nivel 1: el mono ardilla (*Saimiri collinsi*)

| Razón | Base |
|---|---|
| **Es el mono de la orilla.** Prefiere la várzea baja, el bosque ribereño y el estacionalmente inundado | Fichas ICMBio de *S. cassiquiarensis* y disertación UFPA sobre *S. macrodon* en Mamirauá [V S3, S6] |
| **Vive donde empieza el juego.** *S. collinsi* es endémico de Brasil al sur del Amazonas, del Tapajós a Maranhão, con registros en Marajó: el Estuario del mundo 1 | Ficha ICMBio de *S. collinsi* [V S1] |
| **Es abundante y va en tropa.** En los aguajales de Pacaya-Samiria, un mono ardilla (el fraile, *S. boliviensis*) fue el primate más abundante (350 ind./km²); los grupos del género son grandes | Aquino 2005 [V S7]; S8 |
| **Se mueve como pide el dueño: rápido y en fila.** Movimientos muy rápidos en las ramas; la cola no sirve para trepar, sino de «pértiga de equilibrio» | Wikipedia, *Squirrel monkey* [V S8, leída el 2026-10-08]; ramas finas y saltos cortos [V S9, vía la línea «miquitos»] |
| **El gesto es honesto.** Los primates comen con desorden y dejan caer mucha fruta y semillas al procesarla; en Panamá, el 90 % de la fruta caída tenía marcas de dientes de mono | Barnett et al. 2012 [V S10]; Universidad de Copenhague 2021 [V S11] |
| **Se lee a 40 dp:** máscara blanca, hocico negro, brazos y patas dorados, cola larga con la punta negra | [V S8, S9] |
| **No mezcla mensajes:** está en Preocupación Menor en Brasil | [V S1] |

**Su riesgo es el contrario:** es el mono que más se rescata de casas en Lima [V S12] y casi el 40 % de los primates retenidos en Ecuador [V S13]. Por eso las reglas de representación de 4.6 son obligatorias, no estéticas.

### 4.2 Catálogo por tramo de niveles (datos, no código) [P]

La regla es la barrera de los ríos: los grandes ríos separan especies, así que cada tramo tiene las suyas (Wallace 1852 [V S14]). Por eso cambia la especie y no solo el color.

| # | Especie (científico) | Mundo · niveles en la intro | Tarjeta | Esqueleto · modelo | Rasgo que se lee en reposo (48 dp) | Tropa | UICN | Fuente principal |
|---|---|---|---|---|---|---|---|---|
| 1 | Mono ardilla (*Saimiri collinsi*) | 1 Estuario · 1-10 | 5 (o el siguiente libre) | B · fila (corre) | Máscara blanca **redondeada y cálida** (que no parezca calavera), cola en arco | 5 | LC | S1, S8, S9 |
| 2 | Sagui plateado (*Mico argentatus*) | 2 Tapajós · 11-40 | 13 | A · cuelga (agarre vertical a una liana) | Blanco plateado con **orejas desnudas color carne** y cola oscura | 3 | LC | S15, S16 |
| 3 | Sauim-de-coleira (*Saguinus bicolor*) | 3 Manaos · 41-80 | 42 | A · cuelga (cabeza abajo) | **Cabeza negra y collar blanco** | 3 | **CR** | S17, S18 |
| 4 | Churuco (*Lagothrix lagothricha*) | 4 Trapecio · 81-140 | 82 | D · cuelga (de la cola) | Grande y lanudo, cara negra, **cola enrollada en la rama** | 2 | VU | S19, S20 |
| 5 | Huapo colorado (*Cacajao calvus ucayalii*) | 5 Loreto · 141-200 | 142 | C · sacude (muerde un fruto y derrama) | **Cara roja** (señal de salud, nunca burla) y cola corta | 4 | VU | S21, S22, S23 |
| 6 | Cotoncillo colorado o tití rojo (*Plecturocebus discolor*) | 6 Oriente · 201-300 | 202 | C · sacude (la pareja) | Pareja mullida con **las colas entrelazadas** | 2 | LC | S24, S25 [género], S26 |
| 7 | Pichico emperador (*Saguinus imperator*) | 7 Madre de Dios · 301-420 | 302 | A · fila (salta) | **Bigote blanco** largo | 3 | LC | S27 |
| 8 | **Pendiente (Q8).** Recomendado: maquisapa o marimono (*Ateles chamek*). Alternativa: fraile o chichilo (*Saimiri boliviensis*) | 8 Madidi y 9 Ucayali · 421-700 | 422 | Maquisapa: D · cuelga (cola). Fraile: B · fila | Maquisapa: **brazos larguísimos**, negro, cara negra. Fraile: gorra negra | 3 / 6 | EN / LC | S28, S29 / S7 |
| 9 | Mono nocturno (*Aotus trivirgatus*) | 10 Alto Negro · 701-820 | 702 | C · fila (camina), con la LUT de noche | **Ojos enormes** con un destello de luna; el padre con la cría en la espalda | 2 y una cría | LC | S30 |
| 10 | Saquí de cara blanca (*Pithecia pithecia*) | 11 Escudo de Guayana · 821-920 | 822 | C · fila (salta) | **Macho negro con la cara blanca**, hembra gris parda, cola gruesa | 2 | LC | S31 |
| 1b | Mono ardilla de los Llanos (*Saimiri sciureus albigena*) | 12 Invitados del Orinoco · 921-960 | 922, como variante | B · fila | Paleta del n.º 1 algo más clara; rótulo «vecinos del Orinoco» | 5 | — | S32 |
| — | **Sin miquitos** | 13 El origen · 961-1.000 | — | Modo dosel | — | — | — | Los monos ardilla viven por debajo de unos 2.000 m [E S33]; 07 §2.2: «sin fauna en primer plano» |

Notas del catálogo:
- **Nombres:** todos pendientes del asesor. El catálogo guarda un nombre general por idioma y, si hace falta, uno por país del mundo. Ejemplo: para *P. discolor*, la Wikipedia en español da «cotoncillo colorado», «tití rojo», «songo songo» y «huicoco de cola blanca», sin decir en qué país se usa cada uno [V S24, leída el 2026-10-08]; «tocón» es peruano [E]. Para *A. chamek*, «maquisapa», «mono araña peruano» y «marimono» [V S29, leída el 2026-10-08], sin país.
- **Mundos 8-9:** *Ateles chamek* vive en las tierras bajas del centro y norte de Bolivia, el oeste de Brasil y el noreste del Perú hasta el río Ucayali, y llega a la zona del Parque Noel Kempff Mercado [V S29], que es paisaje del mundo 8 (07 §2.1). Su presencia concreta en el Madidi es [E]. Es frugívoro y clave para dispersar semillas; está En Peligro [V S28]. Reutiliza el esqueleto D del churuco.
- **Mono nocturno:** la Wikipedia recoge fuentes que dicen que no tiene tapetum lucidum y otras que dicen que sí, de fibras de colágeno [E S30, leída el 2026-10-08]. Por eso no se dibujan ojos que brillan en la oscuridad: solo un destello de luna en el ojo grande. Que el macho carga la cría hasta el 90 % del tiempo es [V S30].
- **Colas entrelazadas:** verificado en el género (tocón cobrizo) [V S25], no en *P. discolor* [E]. Queda como rasgo de reposo, nunca en un Dato del río.
- **Cuelga de la cola (churuco y maquisapa):** la cola prensil ayuda a trepar y a la braquiación [V S20, S28]; que se cuelguen de ella para comer es [E]. Es estilización de animación, sin Dato del río.
- **Tramos largos:** 421-700 es el hueco más largo con la misma especie. La variación determinista de 7.5 y los invitados de la propuesta de fauna (b.6) lo rellenan más adelante.

### 4.3 Formato del catálogo y resolución [P]

Ruta: `Assets/_Project/Data/Fauna/miquitos_v0.json`. No es Remote Config: ajustarlo en remoto exigiría una clave nueva en 08 §9 y la aprobación del dueño. Ejemplo de una entrada (la paleta es orientativa: la fija el artista, siempre menos saturada que los tintes):

```json
{
  "formato": "af-fauna/0",
  "especies": [
    {
      "id": "saimiri_collinsi",
      "cientifico": "Saimiri collinsi",
      "mostrar_cientifico": true,
      "nombres": { "es": "mono ardilla", "pt": "macaco-de-cheiro", "en": "squirrel monkey" },
      "esqueleto": "B",
      "modelo": "fila",
      "locomocion": "corre",
      "tam_dp": 40,
      "tropa_intro": 5,
      "rasgos_reposo": ["mascara_redondeada", "cola_balancin"],
      "paleta": { "cuerpo": "#8C8A68", "extremidades": "#D7A33E", "cara": "#F1E9DA", "hocico": "#2B2118", "gorra": "#8C8A68", "punta_cola": "#2B2118" },
      "iluminacion": "dia",
      "mundos": [1],
      "desde_nivel": 1,
      "hasta_nivel": 10,
      "nivel_tarjeta": 5,
      "uicn": "LC",
      "amenazada": false,
      "dato_tarjeta": "fauna_saimiri_collinsi_dato",
      "ranuras_arte": { "partes": "fauna_saimiri_collinsi_partes", "retrato": "fauna_saimiri_collinsi_retrato" },
      "revision_asesor": "pendiente",
      "evitar": ["mascota", "personas", "calavera"]
    }
  ],
  "sin_fauna": [ { "mundos": [13], "desde_nivel": 961, "hasta_nivel": 1000 } ]
}
```

- Valores: `esqueleto` ∈ {A calitrícido, B cébido, C pitécido o tití, D atélido}; `modelo` ∈ {`fila`, `cuelga`, `sacude`}; `locomocion` ∈ {`corre`, `salta`, `camina`, `trepa`}; `iluminacion` ∈ {`dia`, `noche`}.
- **Resolución pura** `IntroCast.Resolve(mundo, nivel, intento, arteDisponible)`. Devuelve la especie cuyo tramo contiene el nivel, o «dosel» si:
  - el tramo está en `sin_fauna`;
  - es un reintento (sección 7.1);
  - en un build que no es de desarrollo, la especie no tiene su arte final.

  En builds de desarrollo, una especie sin PNG usa el miquito provisional por código (sección 11). **Nunca** se usa la especie de otro mundo.

### 4.4 Tres modelos de suelta en lugar de nueve gestos [P]

La especie cambia el esqueleto, la paleta, la locomoción y el rasgo de reposo. La animación solo tiene tres modelos, todos con la mirada a lo largo de la rama o a la fruta de la mano, **nunca a una cuia**.

| Modelo | Especies | Versión larga | Versión corta | Posadero |
|---|---|---|---|---|
| `fila` | 1, 7, 9, 10, 1b (y el fraile si gana en Q8) | La tropa entra por la rama desde una esquina (`corre`, `salta` con arcos cortos o `camina`), frena 60 ms sobre cada columna suya, suelta y sigue | Posados desde t = 0, en reposo; cada uno suelta desde la mano sobre su grupo de columnas | Rama baja: y = panel.top − 8 |
| `cuelga` | 2, 3, 4 (y el maquisapa si gana en Q8) | Entra por la rama alta y baja colgado (de una liana corta, cabeza abajo o de la cola) sobre su grupo, y suelta desde la mano | Ya colgado desde t = 0 | Rama alta: y = hudBottom + 6; el cuerpo colgado acaba ≥ 8 dp por encima del panel. Si la franja mide menos de 64 dp, se sienta en la rama baja y suelta como `fila` |
| `sacude` | 5, 6 | Entra y se queda sobre su mitad del tablero; muerde (huapo) o sacude la rama (pareja) y la fruta cae desde la rama sobre sus columnas | Igual, ya posados | Rama baja |

- **El huapo** muerde un fruto duro, pero lo que cae sobre cada columna es **solo la fruta del tinte**: nada de trozos de otros colores o formas.
- **La suelta nunca es un lanzamiento por encima del hombro ni hacia el jugador.** La fruta sale de la mano con, como mucho, una velocidad horizontal de 250 dp/s para llegar a una columna vecina de su grupo.
- **Tamaño en pantalla:** `min(tam_dp, franja − 10)`, con un mínimo de 28 dp; si no cabe, modo dosel.
- **Tropa en pantalla:** `clamp(tropa_intro, 2, columnas con alguna cuia no vacía)`. Las columnas de todas las filas se ordenan por x y se reparten en grupos contiguos, uno por miquito.

### 4.5 Tarjeta «¡Miquito a la vista!» [P]

- **Qué es:** una tarjeta pequeña de la familia de «Fruta nueva» (06 §4.5). Lleva el retrato de 40 dp, «¡Miquito a la vista!», el nombre local y una línea de 50 caracteres o menos con un dato verificado (fuente en «Sobre la Amazonía»).
- **Cuándo:** al habilitarse el input, después de la intro, nunca dentro de la carga. Dura 2,5 s y se desvanece. No captura toques (`raycastTarget = false`). No tapa cuias ni botones: va sobre la franja del panorama si mide ≥ 64 dp; si no, sobre el muelle, por encima de la bandeja.
- **En qué nivel:** el primero ≥ `nivel_tarjeta` que esté «libre»: sin banner de mundo, sin Fruta nueva, sin debut de mecánica y sin tutorial pendiente al empezar (por ejemplo, el tutorial de Marea del nivel 5 si no salió en el 3). Una sola tarjeta por nivel, con prioridad tutorial → Fruta nueva → miquito.
- **Por qué «a la vista» y no «nuevo»:** «nuevo» suena a adquisición, y tres especies del elenco están amenazadas (sauim CR; churuco y huapo VU; maquisapa EN). En las amenazadas, la línea es una buena noticia de conservación (por ejemplo, los puentes de cuerda de Manaos para el sauim [V S18]), nunca la etiqueta UICN como trofeo. Sin confeti.
- **Líneas propuestas** (todas al asesor):

  | Especie | Línea | Fuente |
  |---|---|---|
  | Mono ardilla | «Cruza el bosque en fila, por ramas finas» | S9 |
  | Sagui plateado | «Vive en Alter do Chão, junto al Tapajós» | S15 |
  | Sauim-de-coleira | «Manaos tiende puentes de cuerda para él» | S18 |
  | Churuco | «El mono más grande de Amacayacu» | S19 |
  | Huapo colorado | «Su cara roja es señal de buena salud» | S22 |
  | Cotoncillo | «Las parejas cantan a dúo al amanecer» | S26 |
  | Pichico emperador | «Su bigote blanco pasa de los hombros» | S27 |
  | Maquisapa | «Siembra la selva: dispersa muchas semillas» | S28 |
  | Mono nocturno | «El papá carga a la cría» | S30 |
  | Saquí | «Casi siempre se mueve a saltos» | S31 |

- **Texto de interfaz nuevo** (glosario §10): `fauna_sighted_card`: ES «¡Miquito a la vista!» · EN «Monkey spotted!» · PT-BR con el asesor (ojo con «macaco», insulto racista en Brasil [E S34]; opción neutra: «Nova espécie à vista!»).
- **Analítica** (aprobación del dueño, 08 §8.2): `af_fauna_sighted` con `species_id`, **al mostrar la tarjeta**. Así el Cuaderno de campo nunca marca «Visto» algo que no se nombró. Se registra igual con la intro saltada o con Reducir movimiento, porque la tarjeta llega después.
- **Sin tarjeta en el nivel 1:** el primer contacto es el tutorial.

### 4.6 Representación: reglas duras

1. **Siempre libres, en su árbol, sin personas.** Nada de collar, correa, pañal, ropa, jaula, hombros ni manos humanas. Con humanos en la imagen sube el deseo de tenerlos como mascota en capuchinos y monos ardilla [V S35].
2. **Sin rasgos humanos:** ni de pie como personas, ni sonrisas humanas, ni accesorios, ni **pintura de urucum o huito en el pelaje o las manos** (07 §8.1).
3. **No son guías de capítulo:** no hablan, no presentan el banner y no dan consejos (07 §1.3). Presenta Capi.
4. **D-002 y V7:** durante el juego ningún miquito mira, señala ni reacciona a una cuia, a una selección o a un vertido. Nada depende de la legalidad.
5. **Nunca cruzan el río** ni aparecen en la orilla de enfrente [V S14]. La rama sale del follaje de las esquinas superiores, en primer plano, con el río detrás. Sin reflejo de la tropa en el agua.
6. **La máscara del mono ardilla no se lee como calavera** (en holandés y alemán lo llaman «cabecita de muerto» [E S36]).
7. **Las especies amenazadas** hacen el mismo gesto neutro que las demás: nunca un chiste, un premio ni una recompensa al azar.
8. **El árbol** es de várzea o igapó: nunca sumaúma ni lupuna (sagradas), nunca una palmera de açaí trepada con peconha.
9. **Nombres vetados:** «Jurupari», nombres en lenguas indígenas sin revisión pagada, «tití o mico de bolsillo», «tití» como genérico, el nombre científico del tití del Madidi (homenaje a un casino).

---

## 5. Tinte → fruto: qué cae, cómo es y por qué

Reglas comunes:
- **Siempre fruta del tinte**, mordida si hace falta para enseñar la pulpa del color. Es honesto: los monos comen con desorden [V S10, S11].
- **La forma en vuelo es la del icono de accesibilidad del tinte** (07 §3). Así la forma explica el color también a quien ve mal los colores.
- **Tamaño:** 0,6 u en vuelo y 0,45 u al tocar el agua (u = medio ancho de la cuia), con un mínimo de 8 dp. Siempre menor que la boca (0,68 u: el doble de `CuiaView.PourEdgeU`).
- **Sin destellos «mágicos» ni espirales moradas:** el tinte se comporta como un jugo, no como una poción (el caldero de Magic Sort está vetado, 07 §1.3).
- **La floración es igual para todos:** OKLab desde el agua del río hasta el tinte en 100 ms, con un anillo sobre la cara de la dosis. Sin floraciones distintas por tinte: en 100 ms nadie las percibe.
- **Ningún Dato del río dice que una especie come la fruta de su intro**, salvo los pares con fuente: huapo y aguaje [V S23]; sagui plateado y cacauí, de la familia del cacao [V S37]; fraile y *Annona*; machín y aguaje [V S7]. El resto es estilización.

| # | Tinte (HEX canon) | Qué cae (forma en vuelo) | Por qué esa forma (base real) | Fuente | Decide o vigila |
|---|---|---|---|---|---|
| 1 | Guanábana (#FAFAF7) | Gajo de pulpa blanca con 1-2 pepitas negras | El fruto entero mide 10-35 cm: un mono no lo suelta entero | S38 | Su origen amazónico es dudoso: alternativa ingá o biribá (Q15) |
| 2 | Copoazú (#E6D3A3) | Trozo de pulpa crema con borde de cáscara parda y vellosa | Pulpa blanca o crema; cáscara dura | S39 [tamaño E] | — |
| 3 | Taperebá (#F2B705) | Ciruela amarilla **con rabito y una hoja verde** | Drupa de unos 4 cm; la comen aves, murciélagos y monos | S40 | El rabito y la hoja evitan que se lea como la semilla-moneda |
| 4 | Aguaje (#EF7D1A) | Fruto escamoso vinotinto, mordido, con la pulpa naranja a la vista | Fruto de 5-7 cm cubierto de escamas, pulpa naranja | S41 | Tinte del tutorial |
| 5 | Urucum (#CF3E1C) | Cápsula roja erizada **abierta**, con 5-8 semillitas rojas angulosas | Cápsula de 2-6 cm con espinas blandas; semillas de unos 5 mm cubiertas de pigmento rojo | S42 | Es la imagen literal de «semillas que pintan», sin chocar con la moneda: rojas y angulosas, nunca ámbar. Nada de pintura corporal |
| 6 | Camu camu (#9E1B22) | 2-3 bayas granate brillantes juntas | Baya de unos 25 mm que crece en orillas inundables | S43 | El carmín es el de la cáscara |
| 7 | Bacaba (#E58FC6) | Racimito rojizo sin pruina (para no confundirlo con el açaí) | El fruto real es casi negro y su «vinho», crema | S44 | El rosa es estilización del canon (Q15). Ningún Dato del río lo llamará rosado |
| 8 | Açaí (#6A2C91) | Ramita de 4-6 bolitas violeta-negras con pruina gris | Drupa de unos 1,7 cm en racimos; el açaí batido con agua es violeta (norma MAPA de 2000) | S45, S46 | El icono de accesibilidad es un bol: en vuelo es una ramita (Q15) |
| 9 | Huito (#1F2F6B) | Fruto gris pardo partido, con la pulpa clara | El tinte sale del fruto verde y se oscurece con el aire (genipina) | S47 | Pintura corporal: solo fruto y agua. La reacción real tarda: ningún texto la llamará instantánea |
| 10 | Agua del Tapajós (#3BB8D9) | Sin fruta: el miquito sacude una hoja y caen 3 gotas turquesa (icono gota), de 0,25 u | Agua clara verdosa | S48 | En modo dosel, las gotas caen del follaje |
| 11 | Lulo (#4EB265) | Bola naranja peluda, mordida, con la pulpa verde a la vista | Pulpa y jugo verdes; crece entre 1.200 y 2.100 m | S49 | Es andino: ningún Dato del río lo situará en el Trapecio |
| 12 | Arazá (#BBCC33) | Fruto aterciopelado verde-amarillo | El lima es el tono del fruto aún verde; nativo de la Amazonía occidental | S50 | — |
| 13 | Cacao (#5C3317) | Trozo de mazorca amarillo-naranja con pulpa blanca. **Nunca un grano suelto** | Mazorca de 15-30 cm; el marrón sale de fermentar el grano. Los monos comen la pulpa y dispersan el cacauí | S51, S37 | El grano suelto es la silueta del icono de Semillas |
| 14 | Agua del Río Negro (#A9672E) | Hoja seca, estrecha, parda y rizada, de 0,55 u, que entra por la boca y **se hunde** | El color del agua negra sale de la materia orgánica en descomposición; icono canónico «hoja hundida» | S48 [V pH; E húmicos] | Nunca verde ni posada sobre la boca: sería la mecánica Hoja |

Fuera de la intro: guaraná (sagrado para los sateré-mawé), castaña, huayruro (amuleto), crajiru (pintura corporal) y los grafismos de las cuias de Santarém.

---

## 6. Cómo se ve que el río llena

### 6.1 Principio

El agua **nace en el río visible** y recorre un camino continuo hasta la boca de cada cuia. La cuia se llena porque su chorrito la toca, y solo entonces.

- **Primer movimiento en el río.** El ojo detecta el movimiento sin buscarlo y hace 3-4 fijaciones por segundo: nada más se mueve en los primeros 100 ms (línea «río» del encargo).
- **Causalidad (Michotte):** el efecto llega ≤ 70 ms después del contacto y sin hueco. Cada chorrito se abre ≤ 30 ms después de que el agua pase sobre el eje de su cuia, y la cuia empieza a subir ≤ 50 ms después de que el chorrito toque el fondo.
- **Similitud:** el agua del labio, de las tiras y de los chorritos tiene el color del preset del mundo y los mismos destellos (6 × 2 dp) que el río pintado.
- **Destino común:** cada fila se llena en ola, nunca al azar.
- **Misma gramática que el vertido:** chorro → boca → el nivel sube desde el fondo con menisco elíptico y onda (D-006). La intro enseña de paso el verbo del juego.

### 6.2 Tres tiempos

1. **El río crece** (fuente). En la franja de río visible entre el HUD y el panel, una capa superpuesta aclara el agua unos 12 puntos de L* y los destellos del río se intensifican y se deslizan unos 10 dp hacia el panel. **No hay ninguna cresta ni frente que avance por el río**: eso es un macareo, la imagen de la Pororoca (comodín Barajar, glosario §5.1).
2. **Desborda y corre por la madera** (camino). Una lámina de agua sube por encima del canto superior del marco, desde el centro hacia las dos esquinas. Desde las esquinas baja por el borde interior de los costados del marco y, en cada repisa, corre por su cara desde los dos extremos hacia el centro. El marco funciona como una canaleta: es dirección de arte, no un dato.
3. **Cae en cada boca** (efecto). Sobre cada cuia no vacía, de la madera que tiene justo encima (el marco para la fila 1; la repisa de arriba para las demás) cae un chorrito recto. La cuia se llena desde el fondo hasta sus dosis finales. **Las cuias vacías no reciben chorrito y quedan secas**: se leen como reserva.

**Vaciado:** los chorritos se cortan (la cola cae y cada boca de la madera suelta una gota) y el agua de la madera baja hacia las esquinas inferiores del panel y desaparece. Nunca una ola que retrocede de lado: es el icono de Marea (Deshacer).

### 6.3 Con 3 filas

Con 11-16 cuias en 3 filas, el panel empieza bajo el HUD y no queda franja de río visible. El agua aparece desbordando por debajo del HUD, por el canto superior del marco, y sigue el mismo camino. La metáfora ya se aprendió en los niveles 1-20, donde el río sí se ve (en 1-20 hay como mucho 2 filas [E: confirmar con la rampa de cuias del generador]). Se mide con la prueba de cinco personas (sección 16, A11). Respaldo, si falla: una «cortina» de agua que baja desde el canto superior del marco.

### 6.4 Por mundo

- El preset de agua base ya es un dato por mundo: blanca #B08A5A, clara #2E8B8B y negra #4A2C14 (07 §2.1).
- **Agua negra (mundos 3 y 10):** #4A2C14 al 70 % sobre el panel oscuro se lee casi negro, y el agua negra real es un té ámbar translúcido. Propuesta: un ámbar-té translúcido que mantenga los 29 puntos de L* frente al tinte 14 (Q14).
- **Base real, solo como metáfora de arte:** la várzea se inunda con el pulso anual del río [V F6, `cultura-ribera/investigacion.md`]; en el estuario hay además marea [E]. Ningún Dato del río generaliza la marea diaria a todos los mundos.

---

## 7. Guion fotograma a fotograma

### 7.1 Geometría de referencia y modos

Lienzo de 360 × 780 dp (`safeTop` 32, `hudBottom` 88), con las fórmulas de `direccion-visual-v2.md` §3.2 y las cuias de D-006 (alto 5,6 u; boca a 5,35 u; fondo de la cavidad a 0,6 u; 1 dosis = 1 u). Marco de 13,5 dp. La franja libre es `panel.top − hudBottom`.

| Caso | Cuias (u) | Panel (y) | Franja | Pies de fila | Superficie con 4 dosis | Fruta en vuelo / al impacto | Modo |
|---|---|---|---|---|---|---|---|
| **Nivel 1** (3 cuias, 1 fila) | 64 dp (u = 32) | 226,9-481,1 | 138,9 dp | 444,1 | 296,9 | 19,2 / 14,4 dp | Rama |
| **Nivel 7** (7 cuias, 4 + 3) | 48 dp (u = 24) | 162,1-545,9 | 74,1 dp | 334,5 y 508,9 | 224,1 y 398,5 | 14,4 / 10,8 dp | Rama |
| **16 cuias** (6 + 5 + 5) | 39,2 dp (u = 19,6) | 111,9-596,1 | 23,9 dp | 259,7, 409,5 y 559,3 | 169,5, 319,3 y 469,1 | 11,8 / 8,8 dp | Dosel |
| 16 cuias a 360 × 640 | 30,2 dp (u = 15,1) | — | — | — | — | 9,1 / 8 dp (mínimo) | Dosel |

**Modo de posadero** (lo elige el director al arrancar, nunca a mitad de intro):
- **Rama:** franja ≥ 44 dp, primer intento del nivel y especie disponible (4.3). La rama (Bézier de 4-6 dp con corteza y 3-4 hojas) sale del follaje de las dos esquinas superiores y cruza la franja en primer plano: a y = panel.top − 8 (rama baja) o a y = hudBottom + 6 (rama alta, para `cuelga`).
- **Dosel:** en los demás casos (3 filas, pantallas de 640 dp, reintentos, mundo 13, especie sin arte). No hay tropa: el follaje de las esquinas superiores se sacude (±6° durante 150 ms) y cada fruta cae desde y = hudBottom − 12, por detrás del HUD, sobre su columna.
- **Reintentos en dosel:** en cada reintento (Volver a la orilla, Reintentar), el rótulo «Nivel N · intento k» se muestra durante la intro corta en y = TopRowY + 64 (`GameScreen.OnIntroStarted`), justo en la franja de la rama. Además, la repetición cansa: menos animales en el reintento es mejor.

**Exclusión** (todas las capas de la intro): nada a menos de 8 dp de una cuia salvo el chorrito y la fruta que entran en ella; nada sobre los botones del HUD (Pausa x 8-56, insignia x 56-192, Semillas x 196-288, Reiniciar x 292-352, y 40-88); nada sobre la barra de la Crecida (x 348-356, desde el nivel 25); todo con `raycastTarget = false`.

### 7.2 Versión larga: niveles 1-20, primer intento (1.400 ms)

Las fases del canon se conservan como cortes (a 0-300, b 300-700, c 700-950, d 950-1.400). Si Remote Config cambia la duración (1.200-1.500 ms), todos los tiempos y velocidades se escalan por T / 1.400.

| t (ms) | Qué se ve | Audio (ganchos; hoy no hay audio en el código) |
|---|---|---|
| 0 | Fotograma 0, ya saltable. HUD visible. **Cuias vacías y secas en su ranura** (sin caída). La rama ya está (decoración quieta). La tropa está oculta en el follaje superior izquierdo; como mucho asoma una punta de cola quieta. El polen se pausa | El lecho del mundo ya suena |
| 0-150 | **El río crece.** La capa sobre la franja de río aclara el agua (+12 L*, ease-out) y los 6 destellos del río duplican su alfa y se deslizan 10 dp hacia el panel. Es lo único que se mueve | `sfx_river_rise`: oleada grave de 400 ms a −10 dB (nuevo) |
| 120-280 | **Desborda.** La lámina de agua sube por encima del canto superior del marco (alfa 0 → 1 en 60 ms) y se extiende desde el centro hacia las esquinas a 1.300 dp/s. Al llegar a cada esquina, 2 gotas (4 partículas) | — |
| 150-500 | **Fila 1.** Cuando la lámina pasa sobre el eje de una cuia no vacía, en ≤ 30 ms cae su chorrito desde el borde interior del marco: recto, de 0,16 a 0,10 u de grosor, con el núcleo del agua del río con +10 % de L y la línea especular del chorro (guía 6.5). La cabeza toca el fondo en ~30 ms y, en ≤ 50 ms, el agua sube desde el fondo hasta sus dosis finales en 220 ms (ease-out), con menisco elíptico de 0,4 u, onda y las marcas de dosis apareciendo al paso. En el nivel 7: las dos cuias centrales se abren hacia los 180 ms y las de los extremos hacia los 240; la fila 1 está llena hacia los 490. En el nivel 1: la cuia central hacia los 150, la izquierda hacia los 230; la derecha, vacía, queda seca | Un `sfx_fill` (chorro y gorgoteo) por fila, no por cuia |
| 270-700 | **Filas siguientes.** Desde las esquinas, dos tiras de agua de 5 dp bajan por el borde interior de los costados del marco a 2.000 dp/s y, al llegar a la repisa de la fila 1 (nivel 7: ~350 ms), corren por su cara desde los dos extremos hacia el centro a 1.600 dp/s. Al pasar sobre cada cuia no vacía de la fila 2, cae su chorrito desde el borde inferior de la repisa (unos 23 dp hasta la boca) y la cuia se llena igual. **Todo el llenado termina antes de t = 700**: si el tablero lo exige, el director sube las velocidades | — |
| ~490-760 | **Entra la tropa**, cuando la fila 1 está llena. Con el mono ardilla en el nivel 7: 5 miquitos en fila desde el follaje superior izquierdo, a ≤ 450 dp/s (≤ 7,5 dp por fotograma a 60 fps), con la cola en arco. Con tropa de 4 o más, la mitad entra por la esquina derecha, para acortar el recorrido. Con poco contraste: capa media, menos saturados que los tintes, contorno de 2 px como Capi | Sin chillidos aquí, para no robar la mirada al agua |
| 700-850 | **Se calma.** Los chorritos se cortan (la cola cae en 60 ms y cada boca de la madera suelta 1 gota) y las tiras bajan hacia las esquinas inferiores del panel y se desvanecen. Las superficies se mecen y se calman (onda decreciente de 500 ms) | Goteo |
| 740-1.300 | **Sueltas.** Cada miquito frena 60 ms sobre cada columna suya y suelta su fruta (gesto de 6 fotogramas, unos 100 ms). La fruta conserva el 30 % de la velocidad del miquito (la suelta se adelanta para compensar la deriva), gira a 180-360 °/s y cae con g = 12.000 dp/s², encogiendo de 0,6 a 0,45 u. La fruta de la fila 2 pasa por delante de la fila 1, como el chorro del vertido (D-006) | — |
| 950-1.400 | **La fruta tiñe.** Al tocar la superficie: la fruta se funde en 40 ms, un anillo elíptico se abre sobre la superficie (0,4 → 1,3 u, alfa 0,4 → 0, 250 ms) y, solo en el primer impacto de cada fila, 2 gotas (≤ 20 partículas en total). Un penacho del color baja en ≤ 60 ms hasta su racha y la racha florece en 100 ms (OKLab desde el agua del río); el patrón aparece con el tinte, como hoy. **Orden:** filas de arriba abajo; dentro de la fila, por pasadas (primero la racha de abajo de cada cuia, por x; luego la siguiente); dentro de la cuia, de abajo arriba. El «?» de Aguas turbias aparece en el fotograma del primer impacto. La última floración termina exactamente en 1.400 | Un «plic» suave por fruta y `sfx_tint` por racha (la nota pentatónica del tinte); como mucho 6 voces a la vez |
| 1.400 | **Listo:** el input se habilita en este fotograma. La onda de superficie decae en 0,5 s | — |
| 1.400-1.700 | La tropa sale en 300 ms hacia la esquina más cercana, **de espaldas al tablero**, sin capturar toques y sin mirar ninguna cuia. El polen vuelve. Si toca tarjeta, aparece ahora | — |

**Ejemplo, nivel 1** (cuia 1 = açaí × 3 abajo y aguaje arriba; cuia 2 = aguaje × 3 y açaí; cuia 3 vacía; 4 rachas, 4 frutas, 2 miquitos):

| Impacto | Suelta | Miquito | Qué |
|---|---|---|---|
| 950 | ~825 | B (cuia 1) | Açaí al fondo de la cuia 1: florecen 3 dosis violeta |
| 1.050 | ~925 | A (cuia 2) | Aguaje al fondo de la cuia 2: 3 dosis naranja |
| 1.200 | ~1.075 | B | Aguaje arriba en la cuia 1 |
| 1.300 | ~1.175 | A | Açaí arriba en la cuia 2; termina en 1.400 |

La misma fruta da el mismo color en las dos cuias: es la lección. La caída desde la mano (y ≈ 205) hasta la superficie (296,9) dura unos 124 ms.

**Ejemplo, nivel 7:** 15 rachas, 15 frutas, 5 miquitos (uno por columna con contenido). Impactos cada 25 ms entre 950 y 1.300: las 12 de la fila 1 (4 pasadas) y después las 3 de la cuia de la fila 2. Caídas de ~117 ms a la fila 1 y de ~207 ms a la fila 2.

### 7.3 Versión corta: desde el 21 y en todos los reintentos (800 ms)

Los reintentos de los niveles 1-20 ya usan la versión corta (`LevelFlow.BeginAttempt` → `AnimTintDurationMsLate`). Cortes del canon: a 0-150, b 150-400, c 400-550, d 550-800; todo se escala por T / 800.

| t (ms) | Qué se ve |
|---|---|
| 0 | Cuias vacías y secas en su sitio. **Modo rama:** la tropa ya está posada, en reposo (respira, parpadea, mece la cola) y quieta: es el momento de la especie, y su rasgo de reposo (cola entrelazada, bigote, collar, cara roja) se ve los 800 ms. **Modo dosel:** sin tropa. Saltable desde aquí |
| 0-80 | El río crece y sus destellos se deslizan hacia el panel. Con 3 filas, el desborde sale por debajo del HUD |
| 50-130 | La lámina desborda el canto del marco a 2.600 dp/s, desde el centro |
| 60-400 | Chorritos de la fila 1 al paso de la lámina; cabeza de 20 ms; llenado de 130 ms. Tiras laterales a 4.000 dp/s y por las repisas a 3.200 dp/s; filas 2 y 3 igual. **Todo lleno antes de t = 400** (el director sube las velocidades si hace falta). Un `sfx_fill` por fila |
| 400-550 | Los chorritos se cortan (40 ms) y la madera se escurre (110 ms) |
| ~300-650 | **Sueltas**, calculadas hacia atrás desde el impacto: las de las filas bajas salen antes porque caen más. **Rama:** cada miquito suelta desde la mano con su ciclo de suelta (100 ms; 1-2 frutas por ciclo; como mucho 3 ciclos por miquito). **Dosel:** el follaje se sacude y cada fruta asoma por debajo del HUD sobre su columna |
| 550-800 | **La fruta tiñe**, como en la larga. El «?» aparece en el primer impacto. Si hay más de 24 rachas, ver 7.4, paso 2. La última floración termina en 800 |
| 800 | Input habilitado en este fotograma |
| 800-1.100 | La tropa sale hacia las esquinas, de espaldas y sin capturar toques |

### 7.4 El director: cómo se calcula el calendario

`IntroChoreography` es un planificador puro y determinista que se ejecuta una vez en `Start` (≤ 2 ms en clase A) y no asigna memoria por fotograma. Entrada: geometría en dp (cuias, superficies por dosis, filas, repisas, marco, franja), estado visible (rachas por cuia y máscara de ocultas), especie o dosel, y T. Salida: arrays preasignados de eventos de agua, de fruta y de trayectoria de cada miquito.

1. **Rachas visibles:** por cuia y de abajo arriba, tramos de dosis visibles del mismo tinte. Las ocultas no forman racha ni reciben fruta.
2. **Cuántas frutas:**
   - Con ≤ 24 rachas, una por racha.
   - Con más (lo normal con 14-16 cuias), **una por cuia no vacía**: la fruta de su racha de arriba. Al tocar el agua, el penacho baja al fondo en ≤ 40 ms y la cuia florece **de abajo arriba** en cascada, 25 ms entre rachas (como mucho 175 ms con 4 rachas). La racha de arriba, la de esa fruta, florece la última.
3. **Orden de impactos:** filas de arriba abajo; dentro de la fila, por pasadas en el sentido de la tropa; dentro de la cuia, de abajo arriba.
4. **Instantes de impacto:** repartidos de forma uniforme entre el inicio de d (950 / 550, escalado) y T − duración de su floración. La última floración termina exactamente en T.
5. **Caída:** h = y de la superficie − y de la suelta; τ = √(2h / g), con g = 12.000 dp/s² y τ entre 80 y 260 ms. Si τ supera 260 ms, la fruta sale con velocidad hacia abajo: v₀ = (h − ½ g τ²) / τ, con τ = 260. Suelta = impacto − τ.
6. **Asignación:** en rama, cada miquito tiene su grupo contiguo de columnas. En dosel, la suelta está en (x de la columna, hudBottom − 12).
7. **Restricciones** (verificadas por los tests):
   - cada impacto ≥ fin del llenado de su cuia;
   - en la larga, cada suelta ≥ llegada del miquito a su columna + 50 ms;
   - en la corta, cada suelta ≥ 100 ms (el primer ciclo de suelta).

   Si una restricción no se cumple, el impacto y los siguientes se aprietan hacia T; si aun así no cabe, la fruta pasa al miquito vecino. Nunca se pasa de T.
8. **Variación determinista** (contra la repetición en tramos largos): por un hash del número de nivel (xorshift con semilla fija, sin `UnityEngine.Random`) alternan el lado de entrada, quién lidera y una de tres variantes de reposo (rascarse, mirar al cielo, acicalarse). Mismo nivel, mismo resultado: las capturas son estables.

---

## 8. Salto al primer toque

- **Canon** (`anim_tint_skippable`, nunca `false` en producción): un toque en cualquier punto desde el fotograma 0 lleva el modelo y la vista al estado final **en ese mismo fotograma**: cuias llenas y teñidas, patrón puesto, ocultas con «?». El input de juego se habilita en ese fotograma. El toque que salta lo consume `_skipCatcher`, como hoy, y no selecciona ninguna cuia.
- **Restos cosméticos:** frutas en vuelo, anillos, chorritos, tiras de la madera, capa del río y tropa se funden con alfa en ≤ 100 ms **en su sitio, sin moverse**. Nunca hay impactos tardíos y los miquitos no salen corriendo.
- **Ningún bloqueo:** todo lo de la intro lleva `raycastTarget = false` desde el primer fotograma y vive en Canvas anidados propios. El primer toque de juego tras el salto funciona en el fotograma siguiente.
- **Analítica:** `af_anim_skipped` con `anim = tint`, `at_ms` y `duration_cfg_ms`, como hoy. Si la tasa de salto en 1-20 supera `anim_tint_skip_alarm_pct` (40 %), la versión larga baja a 1.200 ms por Remote Config y el director escala solo.
- **El avistamiento** no depende del salto: se registra al mostrar la tarjeta (4.5).
- **Volver de segundo plano** entra sin intro, como hoy.

## 9. Reducir movimiento

El ajuste no existe todavía en el código (`GameConfig` no lo tiene); 07 §7.8 lo pide. Cuando exista:

- **Canon:** fundido de 250 ms de las cuias vacías al estado final, con patrones y «?». Sin río que crece, sin lámina, sin chorritos, sin frutas, sin anillos ni partículas, sin destellos que parpadeen más de 3 veces por segundo.
- **Especie** [P, Q13]: en modo rama, el miquito más cercano a la esquina aparece **quieto** en una sola pose de reposo, entra con el mismo fundido y se desvanece 600 ms después en 150 ms. Sin traslación. Así quien activa la opción también ve la especie.
- **La tarjeta** sale igual, solo con fundido.

## 10. Aguas turbias: las capas «?»

Hoy Aguas turbias no existe en la presentación (`LevelPackReader` rechaza `murky = true`). Esto es la especificación para cuando llegue (debut en el nivel 100, mundo 4).

**En la intro, nada revela una capa oculta** (T3: las ocultas empiezan pardas y opacas con «?»).
- La cuia turbia se llena de río como las demás (sus dosis cuentan para la altura).
- **Las dosis ocultas no reciben fruta ni nota afinada** (la nota delataría el tinte). Su única racha visible es la de arriba, que recibe su fruta normal.
- En el fotograma del primer impacto del tablero, todas las ocultas pasan a la vez a pardo opaco #8A7B6A con trama diagonal al 25 % y «?» (07 §4.3). Así no parece que la tropa lo ponga.
- **Fugas a evitar** (test obligatorio): el planificador trabaja con el estado **visible**. Ni el número de frutas, ni su orden, ni sus tiempos, ni el sonido dependen del color real de una oculta.
- **Sin narración de barro** en los mundos de agua negra o clara (el Negro y el Tapajós apenas llevan sedimento). El aspecto es el del canon, por legibilidad, y ningún Dato del río lo explica como «barro del río».

**En juego se reutiliza la floración, sin fauna.**
- **Revelado** (02 §4.1, 300 ms): al vaciarse la dosis de encima, la parda florece hasta su tinte con la misma rutina OKLab, partiendo de #8A7B6A en vez del agua del río. **Ningún miquito aparece**: sería fauna que reacciona a un vertido (D-002, V7).
- **Luciérnaga** (comodín Revelar): la luciérnaga se posa en la boca y las capas ocultas de esa cuia florecen con la misma rutina. Es una ayuda explícita, así que el diseño la permite.
- **Una sola implementación** de la floración (una función de evaluación por dosis) sirve para la intro, el Revelado y la Luciérnaga.

---

## 11. Arte procedural ahora (por código, sin esperar al artista)

Todo en Canvas anidados propios, con `raycastTarget = false`, generado una vez y guardado en caché; la parte de la especie, de forma perezosa por mundo (≤ 10 ms), dentro del presupuesto de ≤ 300 ms de generación al arrancar.

| Pieza | Medidas | Cómo |
|---|---|---|
| Capa de crecida del río | Toda la franja visible (x 0-360, y hudBottom … panel.top); +12 L* como máximo | Imagen con degradado vertical en los colores del río, alfa 0 → 0,35. El río está horneado en el panorama (y en el PNG `bg_estuario_panorama`), así que no se puede «subir»: se aclara. Los destellos son los 6 de `AmbientLayer` (6 × 2 dp), con un refuerzo nuevo |
| Lámina del desborde | Alto: de panel.top − 4 al borde interior del marco (≈ 17,5 dp); borde superior ondulado (amplitud 1 dp, λ 0,5 u); borde inferior claro | `WaterStripGraphic` horizontal que crece desde el centro; agua del preset al 85 % |
| Tiras del marco y de las repisas | 5 dp de ancho; costados en el borde interior del marco; repisas en su cara (de pie − 3 a pie + 2) | El mismo `WaterStripGraphic`, vertical u horizontal, que crece desde un origen; destellos que viajan a 300-400 dp/s |
| Chorritos | De 0,16 a 0,10 u (nivel 7: 3,8 → 2,4 dp); fila 1 desde el borde interior del marco; fila r desde el borde inferior de la repisa de arriba (≈ pie de la fila r − 1 + 23 dp) | Pool de 16 `StreamGraphic` rectos con el material del chorro |
| Llenado de la cavidad | Hasta las dosis finales; 220 / 130 ms | `CuiaView.SetDose` con un retardo por cuia (hoy el nivel de llenado es global) |
| Frutas en vuelo | 14 sprites de 64 × 64 px en un atlas de 512 × 128; en pantalla, 0,6 → 0,45 u, mínimo 8 dp | Formas simples por código (esfera escamosa, cápsula con púas abierta, ramita de bolitas, gajo, ciruela con rabito y hoja, hoja rizada, gotas). Sin animación de fotogramas: giro de 180-360 °/s. Si existe el PNG `fruta_<tinte>`, se usa su versión reducida |
| Anillos | 0,4 → 1,3 u, 250 ms | Pool propio de 24 con el sprite `Ring` y el material aditivo (`FxLayer` solo tiene 6) |
| Gotas | ≤ 20 en total | `FxLayer.Splash`, como hoy |
| Floración | 100 ms; cascada de 25 ms por racha en el modo por cuia | Interpolación OKLab uniforme (como hoy) con el anillo encima. La floración radial queda para después: exige tocar el shader del líquido |
| Rama | Bézier de 4-6 dp, de esquina a esquina, con 3-4 hojas | Corteza en `madera_oscura`; hojas con el sprite `LeafBit` |
| Miquito provisional | 36-40 dp (churuco y maquisapa: 56-64, recortados a franja − 10); contorno de 2 px de tinta al 80 % | Un rig recortable por esqueleto (A-D) con 6-8 piezas generadas en `PixelCanvas` (elipses de torso y cabeza, máscara o cara según el rasgo, extremidades en cápsula y cola en tira Bézier de 8 segmentos) con los 6 colores del catálogo. Poses por código: reposo (escala Y 1,00 ↔ 1,02 cada 1,6 s; parpadeo de 120 ms), viaje (balanceo de ±2 dp a 8 Hz; extremidades ±25°), suelta (brazo −40° → +20° en 100 ms) y colgado |
| Follaje que se sacude (dosel) | ±6° en 150 ms | El balanceo de las esquinas que ya existe, con una amplitud mayor |

Con esto se prueban la coreografía, los tiempos y los dos modos antes del arte final. **El vídeo de la tienda y el soft launch no se graban con los miquitos provisionales.**

## 12. Lista para el artista (PNG por Git LFS; estilo D-003)

Estilo común: contorno de 2 px de tinta al 80 % (como Capi), sin ropa ni accesorios, ojos con brillo, postura animal, saturación por debajo de los tintes, luz desde arriba a la izquierda a 35°, legible a 40 dp (los grandes, a 64 dp).

| Pieza | Ranura | Tamaño | Notas |
|---|---|---|---|
| Piezas del miquito, por especie | `fauna_<id>_partes.png` | 1024 × 1024 (rejilla de 4 × 4 celdas de 256 px) | Se dibujan sobre una **plantilla por esqueleto** (A-D) con celdas y pivotes fijos que preparan los agentes en `docs/art/`. Así no hace falta un archivo de metadatos: la animación la hace el código (Q16) |
| Retrato de 3/4 | `fauna_<id>_retrato.png` | 512 × 512 | Tarjeta (40 dp) y Cuaderno; la silueta del «?» la genera el código |
| Fruta (y aguas) | `fruta_<tinte>.png` × 14 (ya hay 5 ranuras reservadas) | 256 × 256 | Misma pieza para la tarjeta «Fruta nueva», la intro y el Cuaderno; la forma, de la familia del icono de 07 §3. Una herramienta de Editor genera el atlas de vuelo de 64 px |
| Rama del mundo 1 | `rama_estuario.png` | 1024 × 256 | Árbol de várzea (asesor), nunca sumaúma; después, una por mundo |
| Liana | `liana_tile.png` | 64 × 256, en tesela | Para el modelo `cuelga` |
| Marco del panel (opcional) | `ui_panel_tablero.png` | 512 × 512, 9-slice como hoy | Solo si se quiere una ranura tallada en el borde interior, por donde corre el agua |

- **Orden y coste** [E, plantilla de la propuesta de fauna e.2]: unos 2-3 días por especie sobre un esqueleto existente y unos 6 por esqueleto nuevo. La 1.0 con las especies 1-5 suma unos 30 días. **Prioridad: el mono ardilla**, porque sale en los niveles 1-10 y en el vídeo.
- **Audio** (07 §9.5: original o con licencia; lo encarga el dueño): `sfx_river_rise`, `sfx_fill`, `sfx_fruit_plip`, las 14 notas de `sfx_tint` que ya prevé el canon y, como mucho, un chillido corto de mono ardilla a −18 dB. Sin grabaciones de primates sin licencia.

## 13. Rendimiento

| Recurso | Presupuesto de la intro | Cómo |
|---|---|---|
| CPU de efectos y tropa | ≤ 1 ms por fotograma en clase A | Calendario precalculado; pools preasignados (24 frutas, 24 anillos, 16 chorritos, ≤ 6 miquitos × 8 piezas) |
| GPU de escena, panel, HUD y efectos | ≤ 6 ms en clase A | — |
| Asignaciones | 0 B por fotograma tras `Start` | Profiler |
| Draw calls nuevos | ≤ 6 (SetPass ≤ 4): tiras y chorritos, frutas (un atlas), anillos (aditivo), rama y tropa (un atlas por especie) | Frame Debugger |
| Reconstrucción de Canvas | Los gráficos de la intro, en Canvas anidados propios; `SetVerticesDirty` solo mientras se animan. Hoy el agua de inmersión vive en el Canvas raíz y lo reconstruye cada fotograma: se elimina | Profiler |
| Partículas y brillos | ≤ 20 partículas y ≤ 8 brillos aditivos en la intro | Contador |
| Memoria | Atlas de frutas, 256 KB; textura procedural de la especie, ≤ 256 KB; PNG de la especie (ASTC 4×4), ~1 MB, solo el del mundo actual | Memory Profiler |

El teléfono del dueño (SM-S938B) es de gama alta: no valida este presupuesto.

---

## 14. Orden con otras pantallas

Banner de mundo (antes de la intro, canon) → intro → después, como mucho una de estas: dedo guía del tutorial, «Fruta nueva» o «¡Miquito a la vista!». Si hay dedo guía, aparece cuando la tropa ya ha salido (T + 300 ms); el input está habilitado desde T igualmente. «El río bajó» va antes de la intro (ya es así). Hoy ni el banner de mundo ni «Fruta nueva» existen en el código: la regla de prioridad se implementa con la tarjeta.

## 15. Riesgos

| # | Riesgo | Mitigación |
|---|---|---|
| R1 | **Densidad en la larga:** río, madera, llenado, escurrido, tropa y fruta compiten en 1,4 s | La tropa entra cuando la fila 1 está llena y con poco contraste; el agua solo tiene tres tiempos; prueba de cinco personas (A11) antes de fijar canon |
| R2 | **La tropa se ve como un borrón** (a 30 fps, un miquito avanza medio cuerpo por fotograma) | ≤ 450 dp/s; entrada por las dos esquinas con tropa ≥ 4; recorridos cortos. Solo si no basta: versión de estreno de 1,8 s en el nivel 1 (Q12, clave nueva) |
| R3 | **Con 3 filas no se ve el río** y no hay tropa | La metáfora se aprende en 1-20; prueba de cinco personas; respaldo de la cortina. La identidad de la especie se juega en los niveles de 1-2 filas y en la tarjeta |
| R4 | **Legibilidad con 16 cuias** (hasta ~45 rachas) | Una fruta por cuia por encima de 24 rachas; fruta de 8 dp como mínimo; orden de arriba abajo; ninguna fruta se queda en pantalla |
| R5 | **Repetición** en tramos de hasta 280 niveles con la misma especie | Variación determinista por nivel (7.4, paso 8); dosel en los reintentos; vigilar `af_anim_skipped` por mundo |
| R6 | **D-002:** que un miquito parezca señalar una cuia | Miran a lo largo de la rama o a su fruta; salen de espaldas; nada depende de la selección ni de la legalidad; test (A8) |
| R7 | **Confusión de sistemas:** semilla-moneda, mecánica Hoja, Marea, Pororoca, Piraña, coleccionables | Rabito y hoja en el taperebá; nunca un grano de cacao; la hoja del Río Negro se hunde; vaciado vertical; sin frente en el río; ningún pez se lleva fruta; ninguna fruta queda tras la intro |
| R8 | **Representación:** el mono ardilla es el primate más rescatado de casas en Lima | Reglas de 4.6; asesor para nombres, «miquitos» y su traducción, el árbol y las especies amenazadas |
| R9 | **El arte depende del artista** y no está encargado | Entrega por fases (sección 19): el modo dosel ya cumple la idea sin miquitos; el soft launch y el vídeo, solo con arte final |
| R10 | **Canon contradictorio** sobre el orden del teñido (02 frente a 07) | Q4; editar 02 §4.2, 07 §5.1 y 08 §5.3 a la vez |
| R11 | **Rendimiento en clase A** sin dispositivo para medir | Pools, atlas y Canvas anidados desde el principio; medir en cuanto haya un Mali-G52 o Adreno 610 |
| R12 | **Precisión botánica y zoológica** (bacaba rosa, guanábana, lulo andino, cola prensil al comer, colas entrelazadas en *P. discolor*, tapetum del mono nocturno) | Estilización aceptada, sin Datos del río; lista de pendientes en la sección 20 |
| R13 | **Especies amenazadas como hito** de progresión | «A la vista», no «nuevo»; línea de buena noticia; sin confeti ni premio |
| R14 | **Choques de ranura** (rótulo del intento, «El río bajó», banner, tarjetas) | Dosel en los reintentos; una tarjeta por nivel con prioridad fija |

## 16. Pruebas de aceptación medibles

| # | Qué | Criterio | Cómo |
|---|---|---|---|
| A1 | Duración | La última floración termina en T ± 1 fotograma para T ∈ {1.200, 1.400, 1.500, 600, 800, 1.000}; `Phase == Playing` en T | EditMode sobre el planificador y PlayMode con `LevelFlow` |
| A2 | Causalidad | Por cuia: apertura del chorrito − paso del agua por su eje ≤ 30 ms; inicio del llenado − contacto de la cabeza ≤ 50 ms; inicio de la floración = fotograma del impacto | EditMode (calendario) |
| A3 | Primero el río | En [0, 100] ms solo se mueven la capa del río y sus destellos; en la larga, la tropa no se mueve antes de que la fila 1 esté llena | PlayMode (posiciones por fotograma) |
| A4 | Llenado | Fin del llenado ≤ 700 ms (larga) y ≤ 400 ms (corta) con 3, 7 y 16 cuias; las cuias vacías nunca reciben agua | EditMode |
| A5 | Fruta | ≤ 24 frutas; con más de 24 rachas, una por cuia no vacía; cada impacto ≥ fin del llenado de su cuia; x del impacto dentro del eje de la cuia ± 0,1 u; tamaño al impacto ≥ 8 dp con 3, 7 y 16 cuias a 360 × 780 y 360 × 640 | EditMode |
| A6 | Aguas turbias | Ninguna fruta ni nota para una dosis oculta; mismo calendario para dos tableros que solo difieren en el color de una oculta; «?» de todas en el mismo fotograma | EditMode (cuando exista la mecánica) |
| A7 | Exclusión | Ninguna pieza de la tropa ni de la rama a menos de 8 dp de una cuia, sobre un botón del HUD ni dentro del panel, con 3, 7 y 16 cuias a 360 × 780, 360 × 800 y 360 × 640 | PlayMode con rectángulos |
| A8 | D-002 | Seleccionar o verter no cambia nada de la capa de la tropa; a T + 300 ms (o 100 ms tras un salto) no queda ninguna pieza de la intro salvo, como mucho, una punta de cola en la esquina | PlayMode |
| A9 | Salto | Toque en t ∈ {0, 1 fotograma, 300, 700, 1.000, 1.399}: estado final en ese fotograma; `TapCuia` en el fotograma siguiente selecciona; restos con alfa 0 en ≤ 100 ms; ningún `Graphic` de la intro con `raycastTarget = true` en ningún momento; un solo `af_anim_skipped` con `anim = tint` | PlayMode |
| A10 | Rendimiento | En clase A: media ≥ 58 fps durante la intro, ningún fotograma de más de 33 ms, CPU de efectos ≤ 1 ms, GPU ≤ 6 ms, 0 B asignados por fotograma tras `Start`, `Start` ≤ 2 ms, generación de la especie ≤ 10 ms | Profiler por USB (pendiente de dispositivo); en el Editor, las asignaciones y los tiempos de CPU |
| A11 | Lectura (personas) | 5 personas que no conocen el juego ven las capturas o el vídeo de los niveles 1 y 7: ≥ 4 de 5 dicen que el agua «sale del río» y que el color lo pone «la fruta» o «los monos». Con 16 cuias: ≥ 3 de 5 | Prueba de pasillo |
| A12 | Determinismo | El mismo nivel produce el mismo calendario, byte a byte | EditMode |
| A13 | Especies | El catálogo valida (ids únicos; tramos sin solapes ni huecos en 1-1.000; `nivel_tarjeta` dentro del tramo; colores válidos); `IntroCast` devuelve dosel en el mundo 13, en reintentos y sin arte en un build de release, y nunca la especie de otro mundo | EditMode |
| A14 | Capturas | Nuevas capturas deterministas: `01_intro_l7` a 300, 700, 1.100 y 1.400 ms; `01c_intro_l1` a 300, 900 y 1.300; `01d_intro_short_l7` a 200, 500 y 700; `01e_intro_16` (dosel) a 500. Un enganche de depuración fija el tiempo de la intro para capturar | PlayMode (`VisualSnapshotTests`) |
| A15 | Tarjeta | Aparece en el nivel libre correcto, dura 2,5 s, no captura toques y no tapa cuias ni botones; `af_fauna_sighted` se registra una vez por especie | PlayMode (cuando se apruebe) |

---

## 17. Canon que habría que editar si el dueño aprueba

| Documento | Cambio |
|---|---|
| 02 §4.2 | Contenido de las fases a-d (sin caída; río, madera y chorrito por cuia; fruta por racha; orden de abajo arriba en la cuia). Los tiempos no cambian |
| 07 §5.1 | Tabla y pseudocódigo `PlayLoadAnim`; «un icono de fruta por dosis desde fuera de pantalla» → «una fruta por racha, de la mano de un miquito o del dosel»; «la de arriba primero» → «de abajo arriba en la cuia; filas de arriba abajo»; Aguas turbias sin gota parda |
| 08 §5.3 y guía visual 6.11 | Las mismas fases; se retira el agua de inmersión por fila |
| 07 §9.2 | SFX nuevos (`sfx_river_rise`, `sfx_fill`, `sfx_fruit_plip`); se retira `sfx_submerge` |
| Glosario (línea 257 y mención de la línea 31), §3.3 y §10 | Nombre «Llenar y teñir»; los miquitos en el elenco; `fauna_sighted_card` |
| 07 §2.2 | Columna de fauna: la especie de miquito de cada mundo |
| 06 §4.6 | Página Fauna ampliada con los miquitos (con el asesor) |
| 08 §8.2 | `af_fauna_sighted` |
| 07 §11 | Días de arte de los miquitos y las frutas |
| 08 §9 | **Nada**, salvo que se apruebe la versión de estreno (Q12) |

---

## 18. Preguntas para el dueño (cerradas, con recomendación)

| # | Pregunta | Opciones | Recomendación |
|---|---|---|---|
| Q1 | Nombre de la animación | A) «Llenar y teñir». B) «Río y fruta». C) Mantener «Sumergir y teñir» | **A.** Conserva el verbo de la marca y ya no promete una inmersión que no ocurre |
| Q2 | ¿Cómo entra el agua? | A) El río crece, desborda el canto del panel, corre por la madera y cae un chorrito en cada cuia. B) Inundación de cada fila desde los lados. C) Cortina desde arriba | **A.** Misma gramática que el vertido y causalidad clara; C queda como respaldo con 3 filas |
| Q3 | ¿Se quita la caída de las cuias (fase a)? | A) Sí: están en su sitio desde el fotograma 0. B) No | **A.** El primer movimiento tiene que ser el río |
| Q4 | Orden del teñido dentro de una cuia (02 y 07 se contradicen) | A) De abajo arriba: la fruta se hunde como un sedimento. B) De arriba abajo (07 §5.1 y el código de hoy) | **A**, con las filas de arriba abajo. Se editan 02, 07 y 08 a la vez |
| Q5 | ¿Una fruta por racha o por dosis? | A) Por racha, como mucho 24; si hay más rachas, una por cuia con cascada. B) Una por dosis (canon) | **A.** Con 16 cuias, una por dosis serían hasta 64 frutas en 250 ms |
| Q6 | ¿Qué sueltan los miquitos? | A) La fruta del tinte (hoja en el Río Negro, gotas en el Tapajós; semillitas rojas solo en el urucum). B) Semillas | **A.** La semilla ámbar es la moneda; «semillas» puede seguir en los documentos internos |
| Q7 | ¿Qué miquitos van apareciendo? | A) Géneros distintos por mundo (catálogo de 4.2). B) Solo monos ardilla con otra paleta | **A.** Con B el jugador no nota el cambio |
| Q8 | Especie de los mundos 8-9 (421-700) | A) Maquisapa (*Ateles chamek*, EN; esqueleto D del churuco). B) Fraile (*Saimiri boliviensis*; mismo género que el nivel 1). C) Otra, con el asesor | **A**, tras confirmar su presencia en el Madidi y su nombre por país. No hace falta para la 1.0 |
| Q9 | ¿Cuántas especies en la 1.0, y qué pasa sin arte? | A) 1-5; en los mundos sin arte, modo dosel (sin tropa). B) 1-8. C) 1-3 para el soft launch, el resto en dosel | **A.** Nunca la especie de otro mundo (barrera de Wallace) |
| Q10 | Texto de la tarjeta | A) «¡Miquito a la vista!». B) «¡Nuevo miquito!» | **A.** Lenguaje de avistamiento; tres especies están amenazadas |
| Q11 | ¿Cuándo se registra el avistamiento? | A) `af_fauna_sighted` al mostrar la tarjeta, desde la 1.0. B) Al arrancar la intro. C) Nada hasta la 1.1 | **A.** Barato, honesto y el Cuaderno de la 1.1 llega lleno |
| Q12 | Duración del nivel 1 | A) 1.400 ms como el resto de 1-20; medir primero. B) Versión de estreno de 1.800 ms solo en el primer intento del nivel 1 (clave nueva) | **A.** Solo B si la prueba de cinco personas lo pide |
| Q13 | Reducir movimiento con especie | A) El miquito aparece quieto durante el fundido y 600 ms más. B) Sin miquitos | **A.** Una pose quieta no es movimiento |
| Q14 | Agua negra (mundos 3 y 10) | A) Ámbar-té translúcido, manteniendo los 29 puntos de L* frente al tinte 14. B) Mantener #4A2C14 al 70 % | **A**, tras una captura de prueba |
| Q15 | Estilizaciones de color y forma (bacaba rosa, guanábana, lulo, icono bol del açaí) | A) Mantener el canon como estilización, sin Datos del río que la afirmen; en vuelo, ramita de açaí. B) Revisar los tintes con el asesor | **A** para la 1.0; B para el Cuaderno |
| Q16 | Técnica de animación de los miquitos (decide el encargo al artista) | A) Rig recortable en uGUI: el artista dibuja piezas sobre una plantilla y el código anima. B) Hojas de fotogramas por pose | **A.** Menos arte, y `SpriteSkin` no funciona en el Canvas de pantalla que usa el juego |

---

## 19. Tareas de implementación para unity-gameplay-dev

Entrega por fases, cada una jugable y con PR propio. Antes de cerrar cada tarea: `pwsh -NoProfile -File scripts/unity-compile.ps1`, `scripts/unity-test.ps1 -Mode EditMode` y `-Mode PlayMode`, y `dotnet build`/`dotnet test` si se toca el núcleo (no debería). Ningún `.meta`, escena ni `ProjectSettings` a mano: la escena la regenera `GameSceneBuilder.CreateGameScene`.

**Fase 1: el río llena (sin fruta nueva).** Requiere Q2, Q3 y Q4.

| # | Archivo | Cambio | Criterio |
|---|---|---|---|
| I1 | `Assets/_Project/Scripts/Presentation/IntroChoreography.cs` (nuevo) | Planificador puro y determinista (7.4): entrada de geometría en dp, estado visible, modo y T; salida en arrays preasignados (eventos de agua por cuia, frentes de la lámina y las tiras, frutas, trayectorias de miquitos). Sin LINQ, sin `UnityEngine.Random`, sin asignaciones tras la primera llamada | A1, A2, A4, A5, A6 y A12 en `Tests/EditMode/IntroChoreographyTests.cs` |
| I2 | `Assets/_Project/Scripts/Presentation/WaterStripGraphic.cs` (nuevo) | `MaskableGraphic` mínimo: tira horizontal o vertical con borde ondulado y línea especular, que crece desde un origen en uno o dos sentidos; ≤ 64 cuadriláteros; `SetVerticesDirty` solo si cambian los parámetros | Test de malla en EditMode (número de vértices, longitud 0 → sin vértices) |
| I3 | `Assets/_Project/Scripts/Presentation/IntroAnimation.cs` | Quitar la caída (`DropU`) y el agua de inmersión por fila; llenado por cuia con los tiempos del planificador y solo hasta las dosis finales; chorritos con un pool de `StreamGraphic`; lámina y tiras; floración por racha de abajo arriba, de momento sin fruta (fase 2); `Complete()` deja el estado final en el mismo fotograma y funde los restos en ≤ 100 ms. Se mantiene la API `Start`/`Tick`/`Complete`. Enganche de depuración para fijar el tiempo (capturas) | A1, A3, A4 y A9 en PlayMode; las capturas `01_intro_*` muestran el chorrito sobre cada cuia |
| I4 | `Assets/_Project/Scripts/UI/GameScreen.cs`, `AmbientLayer.cs` y `ImmersionWater.cs` | Crear `IntroLayer` (Canvas anidado dentro de `Board`, delante de las cuias) y pasar a la intro la geometría del panel, el marco (`PanelFrameDp`), las repisas y `hudBottom`. Eliminar `_rowWater` y su posicionado, y borrar `ImmersionWater.cs` (Unity borra su `.meta`). `AmbientLayer.SetRiverBoost(float)` para los destellos y una capa de crecida sobre la franja de río | Sin reconstrucción del Canvas raíz durante la intro (Profiler); A7 |

**Fase 2: la fruta tiñe, en modo dosel (sin miquitos).** Requiere Q5 y Q6. Ya cumple la idea del dueño y se puede probar con personas (A11).

| # | Archivo | Cambio | Criterio |
|---|---|---|---|
| I5 | `Assets/_Project/Scripts/Presentation/ProceduralArt.Fruit.cs` (nuevo, `partial`) y `ArtSlots.cs` | 14 sprites de fruta de 64 px en un atlas de 512 × 128, generados de forma perezosa en la primera intro; `Sprite FruitFlight(int tint)`. Ranuras `fruta_*` para los 9 tintes que faltan; si hay PNG, se usa | Test: 14 sprites distintos, una sola textura; generación ≤ 10 ms en el Editor |
| I6 | `IntroAnimation.cs` (pools en `IntroLayer`) | 24 frutas (un atlas, mezcla alfa) y 24 anillos (aditivos); caída con g = 12.000 dp/s² desde (x de la columna, hudBottom − 12); fundido de la fruta al impacto, penacho y floración; gotas solo en el primer impacto de cada fila; follaje que se sacude; «?» de las ocultas en el primer impacto (preparado para Aguas turbias) | A5 y A9; nada de la intro con `raycastTarget = true`; 0 B por fotograma tras `Start` |

**Fase 3: la tropa.** Requiere Q7, Q9 y Q16.

| # | Archivo | Cambio | Criterio |
|---|---|---|---|
| I7 | `Assets/_Project/Data/Fauna/miquitos_v0.json` (nuevo), `Assets/_Project/Scripts/Levels/FaunaCatalog.cs` y `FaunaCatalogReader.cs` (nuevos, `AF.Levels`, sin motor, con el lector JSON existente), `GameBootstrap.cs` y `UI/Editor/GameSceneBuilder.cs` | Catálogo de 4.3 con las especies de 4.2 (todo «pendiente del asesor»); `IntroCast.Resolve(mundo, nivel, intento, arteDisponible)`; el `TextAsset` lo asigna `GameSceneBuilder` al regenerar la escena | A13 en `Tests/EditMode/FaunaCatalogTests.cs` |
| I8 | `Assets/_Project/Scripts/Presentation/TroopLayer.cs`, `MonkeyRig.cs` y `ProceduralArt.Fauna.cs` (nuevos) | Canvas anidado entre `Board` y `Decor`; rama (Bézier); rig recortable de 6-8 piezas por esqueleto con poses por código; modelos `fila`, `cuelga` y `sacude` guiados por las trayectorias del planificador; salida de espaldas en 300 ms; modo rama solo con franja ≥ 44 dp y primer intento; ranuras `fauna_<id>_partes` y `fauna_<id>_retrato`; variación determinista por nivel | A3, A7 y A8 con 3, 7 y 16 cuias a 360 × 780 y 360 × 640; capturas A14 |
| I9 | `Assets/_Project/Editor/CI/GeneratedArt.cs` y `Assets/_Project/Art/Generated/README.md` | Ajustes de importación de las ranuras nuevas (fauna, fruta, rama, liana) y generación del atlas de vuelo de 64 px a partir de los `fruta_*.png` | Importar un PNG de prueba actualiza el catálogo de arte |
| I10 | `Assets/_Project/Tests/PlayMode/IntroTests.cs` (nuevo) y `VisualSnapshotTests.cs` | Salto en varios instantes, primer toque tras el salto, independencia de la selección, exclusión por rectángulos, capturas deterministas | A7, A8, A9 y A14 |

**Fase 4: tras las decisiones del dueño (Q10, Q11 y Q13) y de otras pantallas.**

| # | Archivo | Cambio | Criterio |
|---|---|---|---|
| I11 | `Assets/_Project/Scripts/UI/SightingCard.cs` (nuevo), `UiText.cs` y una regla de «nivel libre» en `AF.GameFlow` | Tarjeta de 4.5 y la cola de una tarjeta por nivel con prioridad tutorial → Fruta nueva → miquito. Persistir los avistamientos exige migrar el perfil (`ProfileMigrations`) y el evento `af_fauna_sighted`: PR aparte, con el visto bueno del dueño | A15 |
| I12 | `IntroAnimation.cs` y `TroopLayer.cs` | Ruta de Reducir movimiento cuando exista el ajuste (07 §7.8): fundido de 250 ms y pose quieta | Ninguna pieza cambia de posición; 250 ms en total |

---

## 20. Pendientes de verificación

Ningún Dato del río ni texto de tarjeta se escribe con un dato [E] o [NV] hasta cerrarlo.

1. Presencia de *Ateles chamek* en el Madidi y sus nombres por país («maquisapa», «marimono») [E].
2. Nombre de *P. discolor* en Ecuador («cotoncillo colorado» u otro) y si las colas entrelazadas están descritas en la especie [E].
3. Tapetum lucidum de *Aotus*: las fuentes se contradicen [E S30]. No se dibujan ojos que brillan.
4. El agarre vertical de los calitrícidos y la bajada cabeza abajo del sauim [E].
5. Que churuco y maquisapa se cuelgan de la cola para comer [E].
6. Que las hembras de mono ardilla cargan la cría en el lomo [E]; hasta entonces, sin cría en el mono ardilla.
7. Nombres en español del sagui plateado y del sauim; «pichico emperador»; «chichilo» [E].
8. Rampa de cuias del generador en los niveles 1-20 (que no haya 3 filas antes del 21) [E].
9. Tamaño del copoazú, química del agua negra y color del grano fresco de cacao [E, extractos de buscador].

## 21. Fuentes

### 21.1 Locales

| # | Documento |
|---|---|
| L1 | `docs/gdd/02-nucleo-reglas-ux.md` §4.1, §4.2, §11 y §13 |
| L2 | `docs/gdd/07-arte-tematica-audio.md` §1.3, §2, §3, §4.3, §5.1, §7.8, §8.1 y §9.5 |
| L3 | `docs/gdd/08-arquitectura-tecnica-publicacion.md` §5.3, §5.4, §8.2 y §9 |
| L4 | `docs/gdd/00-glosario.md` §2.1 y §3.3; `docs/gdd/06-meta-retencion-liveops.md` §4.5-§4.6 |
| L5 | `docs/art/direccion-visual-v2.md` §3, §6, §7, §8 y §10.3 |
| L6 | `docs/dev/decisiones.md` (D-002, D-003, D-005, D-006, D-008) |
| L7 | `docs/research/fauna/propuesta-fauna.md` (b, c.1, d, e, 7 y 8) |
| L8 | `docs/research/cultura-ribera/investigacion.md` (§5; F6 pulso de inundación) |
| L9 | Código: `IntroAnimation.cs`, `ImmersionWater.cs`, `FxLayer.cs` (`MaxRings` = 6, `MaxDrops` = 28), `GameScreen.cs` (`OnIntroStarted`, rótulo del intento en `TopRowY + 64`), `LevelFlow.cs` (`BeginAttempt` con la intro corta), `GameConfig.cs` (sin Reducir movimiento), `ArtSlots.cs` (5 ranuras `fruta_*`), `LevelPackReader.cs` (rechaza `murky`); captura `Logs/snapshots/01_intro_l7.png` |

### 21.2 Web (consultadas el 2026-10-08 por las líneas del encargo, salvo las marcadas «leída hoy»)

| # | Fuente | URL |
|---|---|---|
| S1 | ICMBio, ficha de *Saimiri collinsi* | https://www.gov.br/icmbio/pt-br/assuntos/centros-de-pesquisa/primatas-brasileiros/arquivos/fichas_primatas/CEBIDAE/ficha_saimiri_collinsi.pdf |
| S2 | ICMBio, ficha de *Saimiri sciureus* | https://www.gov.br/icmbio/pt-br/assuntos/centros-de-pesquisa/primatas-brasileiros/arquivos/fichas_primatas/CEBIDAE/ficha_saimiri_sciureus.pdf |
| S3 | ICMBio, ficha de *Saimiri cassiquiarensis* | https://www.gov.br/icmbio/pt-br/assuntos/centros-de-pesquisa/primatas-brasileiros/arquivos/fichas_primatas/CEBIDAE/ficha_saimiri_cassiquiarensis.pdf |
| S6 | Disertación UFPA, ecología alimentaria de *Saimiri* en Mamirauá | https://www.repositorio.ufpa.br/bitstream/2011/10727/1/Dissertacao_EcologiaAlimentarSaimiri.pdf |
| S7 | Aquino (2005), Rev. Peru. Biol. 12(3) | https://www.redalyc.org/pdf/1950/195018521009.pdf |
| S8 | Wikipedia, *Squirrel monkey* (leída hoy: cola como pértiga de equilibrio, movimientos muy rápidos) | https://en.wikipedia.org/wiki/Squirrel_monkey |
| S9 | Primate Info Net, ficha del mono ardilla | https://primate.wisc.edu/primate-info-net/pin-factsheets/pin-factsheet-squirrel-monkey/ |
| S10 | Barnett et al. (2012), J. Trop. Ecol. 28: 543-555 | https://ppbio.inpa.gov.br/sites/default/files/Barnett_et_al_J_of_Tropical_Ecol_2012.pdf |
| S11 | Universidad de Copenhague (2021), monos que comen con desorden | https://science.ku.dk/english/press/news/2021/new-phenomenon-forest-mammals-eavesdrop-on-messy-monkeys/ |
| S12 | Actualidad Ambiental (2026-09-11), monos rescatados en Lima | https://www.actualidadambiental.pe/?p=155303 |
| S13 | El Comercio (2026-10-08), mono ardilla y tráfico en Ecuador | https://www.elcomercio.com/tendencias/mono-ardilla-victima-trafico-especies |
| S14 | Wallace (1852), «On the Monkeys of the Amazon» | https://people.wku.edu/charles.smith/wallace/S008.htm |
| S15 | ICMBio, ficha de *Mico argentatus* | https://www.gov.br/icmbio/pt-br/assuntos/centros-de-pesquisa/primatas-brasileiros/arquivos/fichas_primatas/CALLITRICHIDAE/ficha_mico_argentatus.pdf |
| S16 | Wikipedia, *Silvery marmoset* | https://en.wikipedia.org/wiki/Silvery_marmoset |
| S17 | ICMBio, ficha de *Saguinus bicolor* | https://www.gov.br/icmbio/pt-br/assuntos/centros-de-pesquisa/primatas-brasileiros/arquivos/fichas_primatas/CALLITRICHIDAE/ficha_saguinus_bicolor.pdf |
| S18 | Portal Amazônia, puentes para el sauim (2022) | https://portalamazonia.com/amazonas/passarelas-passam-a-conectar-fragmentos-florestais-em-manaus-para-conservacao-do-sauim-de-coleira/ |
| S19 | Mongabay Latam (2026-08), primates de Amacayacu | https://es.mongabay.com/custom-story/2026/08/mono-volador-secretos-amacayacu-colombia-mayor-diversidad-primates-mundo-reportaje-fotografico/ |
| S20 | Wikipedia, *Woolly monkey* (leída hoy: la cola prensil ayuda a trepar; no dice que se cuelguen de ella para comer) | https://en.wikipedia.org/wiki/Woolly_monkey |
| S21 | Wikipedia, *Bald uakari* | https://en.wikipedia.org/wiki/Bald_uakari |
| S22 | Mayor et al. (2015), Royal Society Open Science | https://pmc.ncbi.nlm.nih.gov/articles/PMC4632585/ |
| S23 | Bowler y Bodmer (2011), Int. J. Primatol. 32(5) | https://kar.kent.ac.uk/30045 |
| S24 | Wikipedia en español, *Plecturocebus discolor* (leída hoy: nombres comunes; Perú, Ecuador y Colombia) | https://es.wikipedia.org/wiki/Plecturocebus_discolor |
| S25 | Wikipedia, *Coppery titi monkey* | https://en.wikipedia.org/wiki/Coppery_titi_monkey |
| S26 | van Kuijk et al. (2023), Frontiers in Ecology and Evolution | https://www.frontiersin.org/journals/ecology-and-evolution/articles/10.3389/fevo.2023.1173722/ |
| S27 | Wikipedia, *Emperor tamarin* | https://en.wikipedia.org/wiki/Emperor_tamarin |
| S28 | Wikipedia, *Peruvian spider monkey* (leída hoy: rango, En Peligro, frugívoro y dispersor, cola prensil y braquiación, grupos de 20-30) | https://en.wikipedia.org/wiki/Peruvian_spider_monkey |
| S29 | Wikipedia en español, *Ateles chamek* (leída hoy: nombres comunes; rango hasta el Ucayali y Noel Kempff) | https://es.wikipedia.org/wiki/Ateles_chamek |
| S30 | Wikipedia, *Night monkey* (leída hoy: tapetum en disputa; frugívoro; el macho carga la cría hasta el 90 % del tiempo) | https://en.wikipedia.org/wiki/Night_monkey |
| S31 | Wikipedia, *White-faced saki* | https://en.wikipedia.org/wiki/White-faced_saki |
| S32 | Primate Conservation 24, *Saimiri sciureus albigena* | https://bioone.org/journals/primate-conservation/volume-24/issue-1/052.024.0102/The-Taxonomy-and-Conservation-Status-of-Saimiri-Sciureus-Albigena/10.1896/052.024.0102.short |
| S33 | Wikipedia en español, *Saimiri* | https://es.wikipedia.org/wiki/Saimiri |
| S34 | Wikipedia, «Macaca (term)» | https://en.wikipedia.org/wiki/Macaca_(term) |
| S35 | Leighty et al. (2015), PLoS ONE | https://pmc.ncbi.nlm.nih.gov/articles/PMC4340917 |
| S36 | ICMBio, ficha de *Saimiri sciureus* (nombres en otras lenguas) | Ver S2 |
| S37 | Barbosa, França y Hernández Ruz (2019), Rev. Acad. Colomb. Cienc. 43(168) | https://raccefyn.co/index.php/raccefyn/article/download/891/2574 |
| S38 | Wikipedia, *Soursop*; *Inga edulis*; *Annona mucosa* | https://en.wikipedia.org/wiki/Soursop ; https://en.wikipedia.org/wiki/Inga_edulis ; https://en.wikipedia.org/wiki/Annona_mucosa |
| S39 | Wikipedia, *Theobroma grandiflorum*; extracto de Embrapa | https://en.wikipedia.org/wiki/Theobroma_grandiflorum ; https://www.infoteca.cnptia.embrapa.br/bitstream/doc/901504/1/Cot303cupuacu.pdf |
| S40 | Wikipedia, Taperebá y *Spondias mombin* | https://pt.wikipedia.org/wiki/Taperebá ; https://en.wikipedia.org/wiki/Spondias_mombin |
| S41 | Wikipedia en español, *Mauritia flexuosa* | https://es.wikipedia.org/wiki/Mauritia_flexuosa |
| S42 | Wikipedia, *Bixa orellana* y Urucum | https://es.wikipedia.org/wiki/Bixa_orellana ; https://pt.wikipedia.org/wiki/Urucum |
| S43 | Wikipedia, *Myrciaria dubia* | https://en.wikipedia.org/wiki/Myrciaria_dubia |
| S44 | Embrapa Rondônia, folleto de la bacaba | https://www.infoteca.cnptia.embrapa.br/infoteca/bitstream/doc/859494/1/folderbacaca.pdf |
| S45 | Wikipedia, *Euterpe oleracea*; Embrapa, Comunicado Técnico 29 | https://en.wikipedia.org/wiki/Euterpe_oleracea ; https://www.infoteca.cnptia.embrapa.br/bitstream/doc/676204/1/ComTec29.pdf |
| S46 | MAPA, Instrução Normativa nº 1/2000 | https://laboratorios.ufrrj.br/laab-rural/wp-content/uploads/sites/3/2015/03/IN-01_mapa-Polpas-e-Sucos.pdf |
| S47 | Wikipedia, Jenipapo; 21 CFR 73.225 | https://pt.wikipedia.org/wiki/Jenipapo ; https://www.law.cornell.edu/cfr/text/21/73.225 |
| S48 | Wikipedia, *Blackwater river* | https://en.wikipedia.org/wiki/Blackwater_river |
| S49 | Wikipedia en español, *Solanum quitoense* | https://es.wikipedia.org/wiki/Solanum_quitoense |
| S50 | Wikipedia, *Eugenia stipitata* | https://en.wikipedia.org/wiki/Eugenia_stipitata |
| S51 | Wikipedia, *Theobroma cacao* | https://en.wikipedia.org/wiki/Theobroma_cacao |

Los números S4 y S5 quedan libres.
