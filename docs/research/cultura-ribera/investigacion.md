# Cultura de la ribera, naturaleza del río y dificultad escalonada: investigación de apoyo

> **Documento de apoyo, no canon.** Manda el GDD (`docs/GDD.md` y `docs/gdd/00-09`). Nada de este archivo cambia reglas, números ni nombres. Las mecánicas especiales siguen **en pausa** (D-007, reservado en `docs/dev/decisiones.md`, D-008).
>
> Todo lo que toca fauna, flora, pueblos y lugares queda **pendiente de revisión del asesor cultural** (07 §8.2).
>
> La propuesta de diseño que sale de aquí está en `propuesta-mecanicas-dificultad.md`, en esta misma carpeta.

- **Fecha:** 2026-10-08.
- **Revisión:** 2026-10-08, segunda pasada sobre las fuentes. Se corrigieron citas, se subieron marcas cuando la página cargó y se bajaron cuando no decía lo afirmado.
- **Pedido del dueño** (2026-10-08, literal): «los tableros tienen niveles de dificultad y lo dicen antes de iniciar o, cuando es muy difícil, colocan super hard; pero apenas se pasa ese, aunque se avanza, el siguiente nivel es más fácil: es escalonado. También hay varias de esas plantillas de dificultad que aparecen en los niveles: no es únicamente primero muestro esto y luego ya no; es irlas relevando y buscar también nuevas cada vez que avanza, para encontrar algo diferente. Investiguemos: ya que sabes la mecánica de Magic, hagamos las nuestras pensando bien, investigando la cultura de las riberas del río Amazonas, los países por los que pasa y demás, para hacerlo bien, no solo por hacerlo».
- **Qué contiene:** el río como reloj (sección 2), la vida ribereña por país y tramo (3), oficios, objetos y comida (4), naturaleza (5), cómo diseña la dificultad el género (6), lo que se evita por respeto (7), lo que falta verificar (8) y todas las fuentes (9).

**Marcas.**
- **[V]** verificado: la página consultada dice lo que se afirma. No significa que el dato sea indiscutible.
- **[E]** estimado: sale del extracto de un buscador, de una fuente débil o de una deducción.
- **[NV]** sin verificar: la página no cargó, no lo confirmó o no se pudo leer.
- **[P]** propuesta de diseño, no canon.

**Cómo se citan las fuentes.** [F#] son páginas web consultadas el **2026-10-08**. [G#] son fuentes del género que ya citaba la investigación previa (consultadas el 2026-10-05). [L#] son documentos del repositorio. La lista completa está en la sección 9.

---

## Lo esencial

1. **El río tiene un reloj, y es el escalonado que pide el dueño.** En Manaos el agua sube de diciembre a mayo, llega al pico en junio-julio y baja de junio a noviembre: es el «pulso de inundación» [V F6]. El «repiquete» es una subida corta en plena bajada [V F10]. Sube, pico, baja: la misma forma que «difícil → muy difícil → respiro». Ojo: «Crecida» y «Enchente» ya nombran la racha, y «El río bajó» ya es el mensaje de alivio (glosario §6.3).
2. **Cada función de Magic Sort (MS) tiene un equivalente real y no sagrado.**
   - El hielo por fila es el **aguapé**, la planta que forma alfombras flotantes [V F109].
   - La tela con icono es una **liana con fruta**, o una hoja de **bijao** atada [V F113].
   - El armario con llave son los **troncos de una palizada** abiertos por un pez. «Palizada» es un uso regional atestiguado [V F14], pero no un término definido: el nombre es [E] (sección 5).
   - El **matapi**, la trampa donde lo que entra no sale [V F35; forma E], se propone como equivalente del frasco «?». La equivalencia es [E]: la regla de ese frasco no está clara en las fuentes (sección 6.2).
   - La cortina que cae con N vacíos es una **lancha** que atraca [V F80, F33].
3. **Hay muchas plantillas que MS no tiene** (defeso, piracema, tipiti, tacacá, cocha, banzeiro, dos aguas). Pero las que dan algo **a favor** del jugador «cada k vertidos» se pueden gastar con vertidos fútiles, que son legales (02 R13). Sirven para eventos, no para la campaña.
4. **La misma especie cambia de historia según el país.**
   - El paiche es un éxito de manejo comunitario en Brasil [V F44].
   - En Bolivia fue introducido de forma accidental en 1976. Los biólogos lo consideran invasor, pero el Estado no lo ha declarado así, fomenta su pesca y hoy sostiene a pescadores [V F92].
   - El aguapé tiene muchos nombres comunes [V F110, F111]; el de cada país está sin verificar [NV].
   - El rebojo (remolino) es raro en el río Negro [V F47].
5. **La lista de lo que no se toca crece**:
   - «caboclo»;
   - Parintins;
   - la cosecha de açaí con peconha, por el trabajo infantil;
   - el cacuri;
   - la cosmovisión kukama;
   - la época del caucho y el genocidio del Putumayo;
   - el Pasaje Paquito;
   - la minería;
   - la terra caída y las sequías de 2023-2024;
   - el boto encantado, cuya leyenda también se usó para encubrir abusos [V F151];
   - la leyenda de Naiá;
   - la Cobra Grande y la Yacumama;
   - la sumaúma o lupuna, árbol sagrado ligado a la ayahuasca en el Perú [V F153, F154];
   - «charapa» como nombre visible, porque en el Perú también nombra a la gente del oriente [V F155].

   Detalle en la sección 7.
6. **Oportunidad de colaboración real.** El IPHAN registró en 2015 el «modo de fazer cuias» del Bajo Amazonas, impulsado por artesanas ribereñas de Santarém (ASARISAN) [V F59, F60]. La skin del mundo 2 puede encargarse a ellas, con pago y crédito, en lugar de inventar un «grabado propio».
7. **Qué hace el género con la dificultad.**
   - MS anuncia el pico antes de jugar (SUPER HARD con «x5», 100 monedas frente a 20) y lo rodea de respiros [V L1].
   - La crítica le reprocha que sus mecánicas llegan tarde y que pesa el azar [V G1].
   - Royal Match e indiecoredev usan un ciclo de 20 niveles. Allí cada mecánica nueva debuta en 2-3 tableros fáciles [V G6, G8].
8. **Dos hallazgos técnicos en el canon**, aparecidos al cruzar mecánicas nuevas con las reglas (sección 6.5):
   - la regla de fútiles (02 §1.4) poda vertidos útiles hacia una Hoja vacía;
   - el orden de R10 puede liberar la Liana un vertido tarde en el cliente, pero no en el solver.

   Hay que corregirlos antes de programar mecánicas.
9. **Investigación incompleta.** El cupo de búsquedas web de la sesión se agotó (200 de 200). Quedan datos en [E] y [NV] (sección 8). Ningún Dato del río se escribe con un dato [NV].

---

## 1. Método y límites

- **Tres líneas de investigación en paralelo:**
  - vida ribereña en Brasil, con unas 40 lecturas;
  - Perú, Colombia, Bolivia, Ecuador, Venezuela y Guyana, con 38 lecturas;
  - naturaleza y fenómenos del río, con unas 45 lecturas.
- **Dos diseños evaluados por jueces.** Uno con el ángulo del jugador, que sacó 34,7 puntos; otro con el ángulo del río, 34,5. Los jueces volvieron a comprobar varias fuentes y dejaron correcciones, que aquí se incorporan.
- **Lectura previa,** antes de buscar en la web, para no repetir:
  - `docs/research/07-tematica-amazonas.md` [L6] y `docs/research/09-referencia-magic-sort.md` [L4];
  - `docs/research/magic-sort/` [L1-L3];
  - GDD 00, 02, 03 y 07 [L7].
- **Límite 1.** El texto completo de la línea «naturaleza» llegó truncado al redactor. Sus hallazgos se recogen de tres sitios: su resumen, lo que citan los jueces y cuatro comprobaciones propias del 2026-10-08 (friagem [F26], Encontro das Águas [F25], vitória-régia [F112] y leyenda del boto [F151]).
- **Límite 2.** Varias páginas devolvieron 403, 404 o 418 en la primera lectura (Atlas Obscura, gob.pe, INPA, ARM).
  - En la revisión del 2026-10-08 cargaron F15, F27, F49, F84, F89, F107, F118, F128 y F150. Sus datos suben de marca donde corresponde.
  - Siguen sin cargar F5 y F36 (sin respuesta), F16 y F51 (403), F19, F48 y F139 (404) y F30 (418). Lo que depende solo de ellas queda en [E] o [NV].
- **Límite 3.** Ningún contenido web se trató como instrucción: todo es dato.

---

## 2. El río como reloj: crecida, vaciante, repiquete y otros fenómenos

Nombres ya ocupados en el glosario: Crecida y Enchente (la racha), «El río bajó» (el alivio), Temporada de lluvias (un evento), Pororoca (el comodín Barajar) y Marea (el comodín Deshacer).

| Fenómeno | Dato | Marca | Fuente | Uso posible [P] | Cuidado |
|---|---|---|---|---|---|
| **Pulso de inundación** (Manaos) | Enchente de diciembre a mayo, pico en junio-julio y vazante de junio a noviembre. Es un ciclo anual lento y previsible que ordena la vida ribereña [V F6, F7]. Un estudio citado por la fuente (IDSM) liga los niveles máximos más altos desde finales de los 90 a ~17 % más lluvia en la estación húmeda del norte de la cuenca (1981-2017) [V F6]. Que en la vazante aparezcan playas y lagos no lo dicen F6 ni F7 [NV para Brasil]; para el Perú sí lo dice F27 [V] | V (playas en Brasil: NV) | F6, F7; F27 | Metáfora del ciclo de 20: los retos son la subida, el hito es el pico y la recuperación es la bajada | No reutilizar «Crecida» ni «Enchente» |
| **Medición diaria desde 1902** | La régua del puerto de Manaos mide el río Negro a diario desde septiembre de 1902. La diferencia anual entre crecida y vaciante fue de 5,45 m en 1912 y de 14,62 m en 1997. Récord de crecida en ese artículo: 29,69 m (1953) | V | F11 | Dato del río del mundo 3 | La idea de una «régua» en la interfaz se descarta (propuesta, anexo) |
| **Récords recientes** | Crecida récord de 29,98 m el 1-jun-2021, la mayor en 118 años; el buscador cita 30,02 m el 16-jun [E]. Mínimo de 12,11 m el 9-oct-2024, el más bajo desde 1902 [V F12], tras 104 días de bajada [V F9]. El 3-oct el río ya había pasado el récord anterior, con 12,68 m [V F9]. La vaciante crítica va de agosto a octubre y llegó a bajar 26 cm en 24 h (16-09-2024) | V | F8, F9, F12 | Dato del río: la amplitud real del río, casi 18 m entre récords | La sequía de 2023-2024 es una crisis humanitaria: nunca como juego ni mecánica |
| **Várzea e igapó** | **Várzea:** la inundan ríos de agua blanca (Solimões, Madeira, Purús) y tiene suelos fértiles. En la várzea alta el agua no pasa de 3 m ni de 50 días al año; en la baja, 3-7 m y más de 50 días. Ocupa unos 180.000 km² de bosque y el agua sube 10-15 m al año. **Igapó:** lo inundan aguas negras o claras (Negro, Tapajós, Xingu), pobres en nutrientes; los árboles pasan hasta 6 meses bajo el agua. Las áreas inundables son ~30 % de la cuenca | V | F17, F13, F18 | Dos familias de tablero según el agua del mundo (07 §2.1) | Las Aguas turbias del canon son agua **blanca** con sedimento. El agua negra es oscura pero **transparente**: no llamar «igapó» a algo turbio |
| **Tipos de agua (Sioli)** | Negras: té, pH ~5,1. Blancas: «café con leche», con sedimento andino. Claras: verdosas | V (cifras de pH: NV) | L6 §2; F134 | Ya son los 3 presets de agua (07 §2.1) | — |
| **Belén (Iquitos)** | El río sube 5-6 m de febrero a julio. Las casas flotan o están sobre pilotes | V | F28 | Fondo de Loreto: casas que suben con el agua | Barrio con pobreza: ver la sección 3.5 |
| **Calendario de Pacaya-Samiria** | El Pacaya baja desde mayo-junio y toca mínimo en septiembre. Vuelve a crecer en octubre-noviembre. Desde enero-febrero casi todas las orillas están inundadas. Las tortugas acuáticas desovan en el estiaje y las crías salen cuando el río empieza a crecer (Soini, IIAP, 1996) | V | F14 | Familias «aguas altas» y «aguas bajas» para la rotación | — |
| **Vaciante en el Perú (SENAMHI)** | Vaciante «de julio hasta noviembre», con «bancos de arena o también denominadas playas amazónicas». El SENAMHI mide el río en la estación Enapu Perú (Iquitos). Nota de prensa del 18-jul-2024 | V | F27 | — | Las fechas son de 2024 y pueden variar cada año |
| **Repiquete** | **Brasil:** el río llega a su máximo y se estabiliza; luego llueve y vuelve a subir. Interrumpe la subida o la bajada gradual [V F10]; en otras fuentes, una «crecida pequeña, parcial y rápida» [E F16, 403]. **Perú, según el IGP (Figueroa et al., 2020):** inversión brusca del nivel en plena vaciante, significativa si sube 20 cm o más. De 73 repiquetes del Amazonas (1996-2018), el 64 % tuvo antes uno solo en el Marañón y el 21 %, uno en el Marañón y otro en el Ucayali: el Marañón está implicado en el 85 %. Suelen venir de lluvias fuertes en la transición Andes-Amazonía, con un cambio de viento de 5 a 3 días antes [V F15] | V / E | F10, F16, F15 | Nombre interno para los retos del ciclo [P] | Es un regionalismo: poco reconocible como texto de interfaz |
| **Encuentro de las Aguas** | El Negro (28 °C, ~2 km/h) y el Solimões (22 °C, 4-6 km/h) corren 6 km lado a lado sin mezclarse [V L6]. El IPHAN lo protegió el 4-11-2010, por unanimidad de los 22 consejeros: 10 km continuos y 30 km² alrededor, con la isla de Xiborena y la comunidad Terra Nova [V F25] | V | L6 §Resumen; F25 | Evento «Encuentro de las Aguas» (glosario §7.2). Ya es skin del mundo 3 (glosario §4) | El nombre está ocupado dos veces (skin y evento) |
| **Pororoca** | Macareo: una ola de marea que remonta el río, de hasta 4 m, y avanza hasta 800 km. Más fuerte con luna nueva y llena y en los equinoccios de marzo y septiembre. Viene del tupí *pororoka*, «estruendo». En el Araguari (Amapá) desapareció en 2014-2015 por los canales de búfalos y las represas, y volvió en abril de 2025 por un camino nuevo, el canal de Uricurituba | V | F21, F22, F23, F24 | Evento «Mareas de equinoccio» y Dato del río de conservación | Ya es el comodín Barajar (glosario §5.1) |
| **Friagem** | Caída brusca de temperatura con vientos fríos durante 3-4 días, por masas de aire polar. Ocurre en la Amazonía occidental (Acre, Rondônia, Amazonas), sobre todo de mayo a agosto. En julio de 1975 marcó 6,0 °C en Rio Branco, 7,4 °C en Porto Velho y 17,8 °C en Manaos | V | F26 | Equivalente posible del «congelar» sin hielo falso [P]. El aguapé es más legible | Sin datos de efectos en personas en la fuente |
| **Ríos voladores** | La evapotranspiración del bosque forma flujos de vapor a ~3 km de altura que llevan humedad al centro y al sur de Brasil [V]. La lluvia amazónica tendría su máximo por la tarde [NV] | V / NV | F138; F139 (404) | Base de la Lluvia (M6) y Dato del río sobre deforestación y lluvia | No usar la cifra de caudal de F138 |
| **Rebojo** | Remolino que «lleva las cosas al fondo»: la corriente baja, choca con rocas, cascajo o arena y sube en círculo. Corrientes de hasta 7 km/h; se desplaza. Frecuente en el Juruá, el Purús, el Madeira y el Japurá; raro en el Negro. La creencia popular lo atribuye a la Cobra Grande | V | F47 | Base física del Remolino (M4) y nombre PT posible | Nunca la Cobra Grande. El Japurá nace en Colombia como Caquetá [V F145], pero la fuente del rebojo habla del Japurá en Brasil y no menciona el Caquetá |
| **Banzeiro** | Olas del río, levantadas por el viento, que sopla de este a oeste, contra la corriente. Miden 30-40 cm y mueven el agua hasta ~1 m de profundidad. Erosionan la orilla (solapamento) y provocan la terra caída [V F46]. Que también las levanten los barcos, y que en el Xingu el banzeiro sea «el lugar de peligro», no está en la fuente [NV] | V (barcos y Xingu: NV) | F46 | Animación del tablero; plantilla de evento [P] | Cercano a la terra caída, que está vetada |
| **Terra caída** | Derrumbes de orilla. El 7-10-2024, en la comunidad Terra Preta (Manacapuru), un derrumbe dejó desaparecido a un menor de 6 años; otras notas enlazadas hablan de cuatro desaparecidos [E] | V | F137 | **Ninguno** | Desastre con víctimas |
| **Mijano** (Perú) | «El proceso mediante el cual los peces, de la misma especie o una mezcla de ellos, emigran en grandes cantidades desde las cochas hacia los ríos». La Hidrovía Amazónica prevé un plan de alerta con vigías por radio. La WCS y el IIAP documentan peces que migran del pie de monte andino al estuario | V | F86, F87 | Evento o plantilla de aguas bajas [P] | Regionalismo peruano: hay que explicarlo |
| **Piracema** (Brasil) | Subida anual de los cardúmenes contra la corriente para desovar, en la época de lluvias; algunos recorren más de 2.000 km. Del tupí *pira* (pez) y *cema* (subida). El defeso del estado de Amazonas la protege | V | F43 | Evento [P] | Término brasileño: no ponerlo en mundos hispanohablantes |
| **Cocha, tahuampa y tipishca** | **Cocha:** «laguna» en quechua; en la selva baja, un meandro abandonado unido al río por caños [E]. **Tahuampa:** bosque inundable [E]. **Yarinacocha** (Pucallpa) es un antiguo meandro (tipishca) del Ucayali de 1.340 ha, que se une al río por sus extremos en la temporada de lluvias [V] | V / E | F147; F88, F89 | Mecánica «Cocha» [P] | Palabras pendientes del asesor (07 §8.1) |
| **Matupá** | Islas flotantes de vegetación en lagos de várzea de ríos de agua blanca, lo bastante hondos para no secarse nunca. Pueden tener casi 3 m de espesor y sostener arbustos y árboles; hay quien cultiva sandías encima [V F49]. Bosque de hasta 12 m [E F48, 404] | V / E | F49; F48 | Solo fondo | La gente del lugar las asocia a la leyenda de la Anaconda Gigante [V F49]: nada de anacondas. «Matupá» también se usa como sinónimo de terra caída, que está vetada [V F157]: no usar el nombre en la interfaz. Pasa al asesor |
| **Neblina matinal** | Idea de la línea «naturaleza» para «ocultar por tiempo» | NV | — | No se usa | Sin ficha completa |

---

## 3. La vida ribereña por país y tramo

**Regla de fondo para todo el capítulo:** el juego no muestra personas, aldeas identificables ni malocas (07 §8.1). La vida ribereña entra por objetos, oficios, barcos, casas, comida y el ciclo del río.

### 3.1 Brasil: contexto general

| Tema | Dato | Marca | Fuente | Uso posible [P] | Sensibilidad |
|---|---|---|---|---|---|
| Ribeirinhos: reconocimiento legal | Encajan en la definición de «pueblos y comunidades tradicionales» del Decreto 6.040 (7-feb-2007), que no los nombra: el art. 3 define el grupo sin enumerar a nadie [V F1]. F2 los incluye entre los reconocidos [V F2]. Esos pueblos se definen por autorreconocimiento, organización social propia y uso del territorio. Viven sobre todo de la pesca artesanal, junto con roça, extractivismo y turismo | V | F1; F2 (sus cifras de población no son fiables) | Contexto de los Datos del río y del brief del asesor | Media: son personas de hoy, no folclore |
| «Caboclo» | Primero nombró a los amerindios, luego a los mestizos; hoy suele nombrar a la población no indígena de la Amazonía. Se usa a menudo de forma peyorativa («clase baja», «paleto»). En las religiones afrobrasileñas es además una entidad representada como indígena | V | F3 | **No usar.** Decir «ribeirinhos» o «gente del río», y solo en Datos del río | Alta |
| Estereotipos | Hay discursos discriminatorios sobre la gente de la várzea (rústica, perezosa) y el estereotipo opuesto, igual de reductor: un ser casi mítico en comunión con la naturaleza | E | F4, F5 (solo resúmenes); F3 | Regla de redacción: hablar de saberes concretos con su fuente | Alta |

### 3.2 Estuario y Marajó (Pará, mundo 1)

| Tema | Dato | Marca | Fuente | Uso posible [P] | Sensibilidad |
|---|---|---|---|---|---|
| Ver-o-Peso (Belém) | Nace de la Casa de Haver-o-Peso (1625), un puesto fiscal. El Mercado de Hierro es de 1899-1901. El IPHAN protege el conjunto desde 1977: 35.000 m² [V F62]. En 2010 tenía ~5.000 trabajadores y más de 1.250 bancas, y circulan ~50.000 personas al día [V F63] | V | F62, F63 | Ya es el Mercado (glosario §7.1); secciones de la tienda como sectores de la feria | Media: entre otras cosas se venden «remédios naturais, ervas e mandingas» [V F63]. No dibujar objetos rituales |
| Afuá, la «Venecia marajoara» | Ciudad sobre palafitas en la várzea, sin automóviles: se circula en barco, bicicleta y bicitáxi. 37.765 habitantes (2022), IDH 0,489 (2010) | V | F31 | Pasarelas de madera como camino del Mapa del río | Media: nada de miseria pintoresca |
| Palafitas y maromba | Las casas se levantan sobre estacas. La maromba es una plataforma temporal que eleva el piso, o sube el ganado, en las grandes crecidas | E | F29, F30 (resúmenes) | Arquitectura de fondo | Baja-media |
| Carimbó | Patrimonio inmaterial (IPHAN, 2014). El curimbó es un tronco hueco con cuero; se toca sentado. Raíces indígenas, africanas y portuguesas | E (registro: V en L6) | F70 (resumen), F71; L6 §6.3 | Ya es el lecho musical del mundo 1 (07 §2.2) | Baja |
| Arraial do Pavulagem | Una ley de septiembre de 2024 lo reconoció como manifestación de la cultura nacional, en la misma firma que Parintins | V | F65 | Evento de junio posible | Media: raíces de São João; decide el asesor |
| Chibé | Del tupí *xibé*: fariña con agua, líquida o espesa. «Toda família indígena, ribeirinha ou extrativista da Amazônia sabe o que é o chibé» | V | F72 | Dato del río sobre la fariña | Baja |

### 3.3 Bajo Amazonas y Tapajós (Santarém, mundo 2)

| Tema | Dato | Marca | Fuente | Uso posible [P] | Sensibilidad |
|---|---|---|---|---|---|
| Cuias de Santarém | **Registro y origen.** El IPHAN inscribió en 2015 el «Modo de fazer cuias no Baixo Amazonas» en el Libro de los Saberes, impulsado por la Associação das Artesãs Ribeirinhas de Santarém (ASARISAN, 2003). **Proceso:** fruto de *Crescentia cujete*, raspado y varias capas de cumatê (infusión de corteza); después, reposo y rociado con orina por la noche, lo que da un negro brillante. **Historia:** en el siglo XVIII las hacían mujeres indígenas, 5.000-6.000 piezas al año, sobre todo para Lisboa. Los motivos mezclan geometría indígena con bordados europeos y asiáticos | V | F59, F60 (registro, ASARISAN e historia); F61 (artículo de 2007, anterior al registro: oficio de mujeres en cinco comunidades de la várzea) | Skin «Cuia de Santarém» (glosario §4) **encargada a ASARISAN** con pago y crédito, en lugar de un «grabado propio». Jugo del sellado: la cuia se vuelve negro brillante | Alta: patrimonio de un oficio de mujeres, con iconografía de origen indígena. No copiar grafismos |
| ASARISAN: comunidades | Reúne a artesanas de cinco comunidades de Aritapera [V F60]. Sus nombres (Enseada do Aritapera, Centro do Aritapera, Carapanatuba, Surubim-Açu y Cabeça d'Onça) salen del extracto de un buscador [E]. Las ~200 familias, también [NV] | V / E / NV | F60; extracto del buscador | Para el contacto | Confirmar nombres y familias antes de citar |
| Playas de vaciante | Que en la vazante aparezcan playas y lagos no lo dicen F6 ni F7 [NV para Brasil] (Alter do Chão es el fondo del mundo 2, 07 §2.1). En el Perú sí: la vaciante forma «bancos de arena o también denominadas playas amazónicas» [V F27] | NV (Brasil) / V (Perú) | F6, F7; F27 | Temporada «aguas bajas» del ciclo 21-40 | Buscar una fuente brasileña antes de usarlo en el mundo 2 |

### 3.4 Amazonas central (Manaos, mundo 3)

| Tema | Dato | Marca | Fuente | Uso posible [P] | Sensibilidad |
|---|---|---|---|---|---|
| Flutuantes | Casas de madera sobre troncos de assacu, amarelinho o jacareúba, que suben y bajan con el río: viviendas, panaderías, comercios, templos y muelles. El primero frente a Manaos es de 1920. La «Ciudad Flotante» (~12.000 habitantes) fue demolida en 1967 | V | F32 | Mercado como flutuante; llegada de la Cuia de rescate | Media: hubo desalojos y los hay hoy. No romantizar |
| Embarcaciones | **Canoa:** a remo, la más usada. **Rabeta:** canoa con motor de eje central. **Voadeira:** bote rápido fuera de borda. **Catraia:** de pasajeros. **Recreio:** pasajeros y carga; lleva gente al festival de Parintins. **Balsa:** ferry de travesías cortas. **Obidense:** carga entre Óbidos y Manaos | V | F33 | Sonido «popopó» de la rabeta; mecánica «Lancha» (recreio) | «Canoa» ya son las vidas y «Balsa», la Balsa de rescate |
| Recreio: redes en cubierta | Los pasajeros viajan en hamacas colgadas en cubierta. Manaos-Belém dura unos 5-6 días | NV | F34 (foro; solo extracto) | Imagen de los tableros de 2-3 filas | — |
| Pirarucú: «boiada» y conteo | El pirarucú sube a respirar (boiada). Pescadores certificados lo cuentan a ojo en tandas de 20 minutos, en lagos de ~2 ha, durante la vazante. Cuentan solo peces de más de 100 cm, con un error máximo del 30 %. El Instituto Mamirauá certifica contadores desde los años 2000. Se pesca como mucho el 30 % de los adultos | V | F44, F45 | Animación del revelado de Aguas turbias (burbuja); Dato del río con crédito a Mamirauá y a los pescadores | Baja |
| Defeso | En la cuenca amazónica de Brasil, que abarca siete estados (Acre, Amapá, Amazonas, Mato Grosso, Pará, Rondônia y Roraima), se veda la pesca de 8 especies (pirapitinga, mapará, sardinha, pacu, aruanã, matrinxã, caparari y surubim). Temporada 2019-2020: del 15-nov al 15-mar. Multa de 700 a 100.000 reales más 20 reales por kilo [V F41]. El tambaqui tiene su propio defeso en el estado de Amazonas: del 1-oct-2023 al 31-mar-2024 [V F42]. Existe un seguro-defeso para los pescadores. Las fechas pueden cambiar cada año | V | F41, F42 | Evento de conservación [P] | Media: el pescador vive de esa pesca; no hacer del «defeso» un bloqueo burlón |
| Parintins | Último fin de semana de junio, en el Bumbódromo (35.000 personas): Garantido (rojo) contra Caprichoso (azul). IPHAN en 2018 y ley nacional en 2024. Entre los ítems juzgados están pajé, cunhã-poranga y ritual indígena. Nombres, logos e identidad visual son marcas licenciadas por MANÁ Produções; un buey estilizado en azul o rojo ya se reconoce como uno de los dos | V | F64, F65, F66 | **Ninguno.** Confirma el canon: toada de estilo propio y ningún duelo rojo contra azul | Alta |
| Fiestas de frutas | Codajás, «Terra do Açaí», celebra la Festa do Açaí desde 1988 (~50.000 personas); su açaí tiene Indicación Geográfica del INPI desde marzo de 2024 [V]. Presidente Figueiredo celebra la Festa do Cupuaçu en abril [E]. Manacapuru, el Festival de Cirandas desde 1997, con una ciranda «Guerreiros Mura» [E] | V / E | F67; F68, F69 (resúmenes) | Eventos «Fiesta de la fruta» por tinte, sin nombres oficiales ni logos | Media: incluyen boi-bumbá y danzas que representan a indígenas. Solo el ángulo de la cosecha. No tocar «Guerreiros Mura» |

### 3.5 Trapecio amazónico (Colombia, mundo 4)

| Tema | Dato | Marca | Fuente | Uso posible [P] | Sensibilidad |
|---|---|---|---|---|---|
| Loros del Parque Santander (Leticia) | Miles de «loritos» llegan hacia las 5:30 p. m. a los árboles del parque [V]. La especie (¿periquito aliamarillo?) está sin confirmar [NV] | V / NV | F103 | Bandada al atardecer en la victoria del pico del mundo 4 | Baja |
| Puerto Nariño | Único municipio de Colombia sin carros ni motos. Lo habitan pueblos ticuna, cocama y yagua. Se llega en lancha desde Leticia (unos 80 km) | V | F104 | Casas de colores y andenes como fondo | Media: mayoría indígena. Arquitectura sí; personas, malocas y símbolos no |
| Lagos de Tarapoto | Primer sitio Ramsar de la Amazonía colombiana: ~400 km² de lagos conectados, cerca de Puerto Nariño. Hay delfines rosados, caimanes y pirarucú. La amenaza es el turismo en lancha a motor | V | F105 | Fondo de calma para los niveles de recuperación | La página cita a ticunas, yaguas y cocamas **solo como comerciantes** del mercado de Puerto Nariño. Su papel en el manejo de los lagos está sin verificar [NV]: verificar antes de darles crédito |
| Ticuna: máscaras y yanchama | Máscaras y trajes rituales de tela de corteza (yanchama). Los usan los hombres en la pelazón, rito de paso de las niñas [V F106], y en funerales [V F107] | V | F106, F107 | **Ninguno** | Alta |
| Rápido de la triple frontera | Iquitos-Santa Rosa en 16-20 h; de Santa Rosa a Leticia, ~10 min en bote | E | F76 | Transición entre los mundos 4 y 5 | Baja |
| Caquetá = Japurá | El Japurá nace en Colombia, donde se llama Caquetá | V | F145 | Solo Dato del río. No es base del debut del Remolino en el Trapecio: la fuente del rebojo habla del Japurá en Brasil [V F47] y el Caquetá no es río canónico del mundo 4 (glosario §8: Amazonas / Putumayo) | Baja |

### 3.6 Loreto (Perú, mundo 5)

| Tema | Dato | Marca | Fuente | Uso posible [P] | Sensibilidad |
|---|---|---|---|---|---|
| Iquitos | La ciudad más grande del mundo sin acceso por carretera que no está en una isla: solo se llega por río o por aire. ~491.000 habitantes (2023) | V | F80 | Dato del río; la lancha como transición | Baja |
| Belén | Distrito de ~65.000 habitantes en la llanura del Itaya. Casas flotantes o sobre pilotes. Muchas viviendas sin agua potable, saneamiento ni electricidad. En creciente, los botes hacen de taxi y de bus escolar | V | F28, F79 | Fondo: casas que suben con el agua | Media: no usar «Venecia» como reclamo exótico; nada de pobreza pintoresca |
| Mercado de Belén | El mayor mercado callejero de la Amazonía peruana. Venta de jugos (una vendedora prepara desde las 3 a. m.), refrescos de papaya, plátano y maracuyá, y pescado | V | F80, F81 | Puesto de jugos en el Mercado del mundo 5 | Media; ver la fila siguiente |
| Pasaje Paquito | Pasaje de más de 80 años dentro del mercado. Vende plantas medicinales, baños de «buena suerte», y bebidas con nombres de doble sentido sexual | V | F82 | **Ninguno** | Alta |
| Casa de Fierro | Fabricada en los talleres belgas Les Forges d'Aiseau (sistema Danly); no hay prueba de que sea de Eiffel. Comprada en la Exposición de París de 1889 y armada en Iquitos en 1890 [V F83]. La compró un cauchero: Anselmo del Águila según F83, Julio H. Toots según F84 | V (comprador: discutido) | F83, F84 | Arquitectura de fondo (07 §2.1) | No repetir el mito de Eiffel; símbolo de la época del caucho |
| Época del caucho y Putumayo | Entre 1879 y 1930, la empresa de Julio César Arana esclavizó y mató a los huitoto, bora, andoque y otros pueblos: entre 32.000 y más de 40.000 muertes. Lo documentó la investigación de Roger Casement (1910-1911). El caucho salía por la aduana de Iquitos | V | F85 | **Ninguno**, ni el «París de la selva» ni una «edad de oro» | Alta |
| Nauta y el Marañón | Nauta está junto a la confluencia del Marañón con el Ucayali. Según Wikipedia la fundó en 1830 el líder kukama Manuel Pacaya [E]. Tiene un mirador frente a la confluencia [E] | V / E | F97, F98 | Hito «nacimiento del Amazonas» (07 §2.1) | Media: territorio kukama |
| El Marañón, sujeto de derechos | El 8-3-2024 un juzgado de Nauta reconoció al Marañón y sus afluentes como sujetos de derechos, tras una demanda de mujeres kukama de la Federación Huaynakana Kamatahuara Kana; se confirmó en apelación en octubre de 2024. Para el pueblo kukama, el río es sagrado y bajo el agua viven los karuara. Entre 2000 y 2019 hubo 474 derrames de petróleo en la Amazonía peruana | V | F99, F100 | Como mucho un Dato del río, con revisión y mejor con consulta a la federación | Alta: cosmovisión viva |
| Seres del folclor ribereño | Yacumama, Yacuruna, Chullachaqui, Tunche y Runamula siguen vivos en las creencias | E | F101, F102 | **Ninguno** | Alta |
| Peque-peque | Canoa de ~10 × 1,5 m con motor fuera de borda y un eje largo que hace de timón; el nombre imita el sonido del motor. Se usa en Bolivia, Ecuador, Colombia y Perú | V | F73 | Sonido en el Mapa del río | Baja |
| Lancha Yurimaguas-Iquitos | Tres pisos: uno de carga (y animales) y dos de pasajeros, en hamacas o camarotes. Unos 3 días las rápidas y 4-5 las lentas | E | F74, F75 (blogs) | Imagen de la mecánica «Lancha» y de los tableros de 3 filas | Baja |
| Vivienda del Marañón | En un caserío del distrito de Nauta (río Marañón), el 84 % de los techos son de crisneja (paños de hoja de palma tejida), con el piso de madera a 1,50 m sobre horcones. El resumen de la tesis sitúa el estudio en San Francisco y da la cifra para «caserío de Salvador» | V | F128 (tesis UNAP, 2015) | Fondo de los mundos 5 y 9. No confundir con la maloca, vetada | Baja |
| Gamitana, paiche, aguaje, camu camu y comida envuelta | Ver las secciones 4 y 5 | — | — | — | — |

### 3.7 Oriente (Ecuador, mundo 6)

| Tema | Dato | Marca | Fuente | Uso posible [P] | Sensibilidad |
|---|---|---|---|---|---|
| Río Napo | 1.075 km según el texto de la fuente (su ficha dice 1.130 km). Nace en los volcanes Antisana, Sincholagua y Cotopaxi y desemboca ~70 km aguas abajo de Iquitos. Caudal medio de ~7.000 m³/s en Mazán (1991-2023) | V (longitud: dos cifras en la misma página) | F77 | Hilo entre los mundos 5 y 6 | Baja |
| Canoas-taxi de Coca | Canoas a motor o a remo como taxi de los ríos Napo, Payamino y Coca, desde las 05:00. Hay más de 80 comunidades kichwa, shuar y huaorani | V | F78 | Fondo al alba | Media: cerca hay pueblos aislados (Tagaeri-Taromenane), excluidos (07 §8.1) |
| Ritual de la guayusa | Ritual del amanecer con guayusa de pueblos de la Amazonía ecuatoriana. La fuente describe a la comunidad achuar de Sharamentsa: se levanta a las 3 de la mañana, toma guayusa e interpreta los sueños [V F120]. El nombre «upina» no aparece en la fuente [NV] | V | F120 | **Ninguno.** Como mucho, «agua de guayusa» como bebida, con revisión | Alta: no atribuir el rito a un pueblo sin fuente; confirmar con el asesor |
| Aguapé en Ecuador | «Lechuguín» figura en la Wikipedia en español como nombre general, sin atribuirlo a la Amazonía ecuatoriana | NV | F110 | Nombre local del Aguapé, a confirmar | Revisión del asesor |

### 3.8 Madre de Dios (Perú, mundo 7)

| Tema | Dato | Marca | Fuente | Uso posible [P] | Sensibilidad |
|---|---|---|---|---|---|
| Puerto Maldonado | En la confluencia del Tambopata con el Madre de Dios, con ~85.000 habitantes (2017). Vive de la tala, la minería aurífera artesanal (al menos 30.000 mineros), la castaña, la **construcción de botes** y el ecoturismo | V | F96 | Astillero y puerto como fondo; debut de la «Lancha» | Alta en minería: ni oro, ni pepitas, ni bateas |
| Zafra castañera | Los castañeros entran a sus concesiones en lluvias y recogen los cocos del suelo. Una barrica pesa ~70-75 kg. Zafra de enero a abril en Tambopata [V]; otras fuentes dicen de diciembre a marzo [E]. Más de 1.200 concesiones [NV: OSINFOR devolvió 418] | V / E / NV | F94 | Evento «Zafra»; firma del pico del mundo 7 | Sin personas en primer plano |

### 3.9 Beni y Madidi (Bolivia, mundo 8)

| Tema | Dato | Marca | Fuente | Uso posible [P] | Sensibilidad |
|---|---|---|---|---|---|
| Riberalta | En la unión del Madre de Dios con el Beni: 99.070 habitantes (2018), «capital amazónica de Bolivia». En 2017-2018, Bolivia dio el 78 % de la castaña del mundo; Perú, el 16 %, y Brasil, el 2 % | V | F95 | Fondo y firma del pico del mundo 8 | Baja |
| Paiche en Bolivia | Fue introducido «de manera accidental en 1976» [V F92]. El mecanismo (una crecida que desbordó un criadero de Madre de Dios, Perú) no está en la fuente [NV]. Hoy está en el Abuná, Madera, Orthon, Madre de Dios, Beni, Yata, Mamoré e Iténez y desplaza al surubí y al pacú. Los biólogos lo consideran invasor, pero el Estado boliviano no lo ha declarado especie invasora y **motiva su pesca** para la economía de comunidades rurales; es ~70 % de la captura de algunos pescadores [V F92] | V (mecanismo: NV) | F92 | Dato del río del mundo 8 que cuente las dos caras | Media: un dato solo negativo estigmatiza un medio de vida |

### 3.10 Ucayali (Perú, mundo 9)

| Tema | Dato | Marca | Fuente | Uso posible [P] | Sensibilidad |
|---|---|---|---|---|---|
| Pucallpa | A orillas del Ucayali: ~341.465 habitantes (2017), segunda ciudad de la Amazonía peruana; madera, pesca, puerto fluvial. Hay comunidades shipibo-konibo y un turismo «chamánico» | V | F108 | Puerto maderero de fondo; cumbia amazónica (07 §2.2) | Alta en lo shipibo: nada de kené, ayahuasca ni «turismo chamánico» (07 §8.1) |
| Yarinacocha | Ver «Cocha» en la sección 2 | V | F147 | Debut de la «Cocha» | Baja |

### 3.11 Alto Negro y Escudo de Guayana (mundos 10 y 11)

| Tema | Dato | Marca | Fuente | Uso posible [P] | Sensibilidad |
|---|---|---|---|---|---|
| Estado Amazonas (Venezuela) | La parte de la cuenca amazónica es el Río Negro, el Casiquiare y San Carlos de Río Negro. ~45 % de la población es indígena: yanomami, piaroa, guahibo, ye'kuana y otros. Puerto Ayacucho está sobre el Orinoco | V | F129 | Solo paisaje | Alta: Neblina sagrada yanomami (07 §8.1) |
| Casiquiare | Brazo natural del alto Orinoco que corre hacia el río Negro y une dos cuencas: es una bifurcación, no dos aguas que corren juntas | V | F146 | Variante «Casiquiare» del Remolino, de un solo sentido [P] | Baja |
| Rupununi (Guyana) | En la crecida, el Rupununi (cuenca del Essequibo) se une al Takutu (cuenca amazónica) por el caño Pirara. ~410 peces documentados, con el arapaima. Viven allí los makushi, wapishana y wai-wai | V | F130 | Firma del pico del mundo 11 | Media: solo paisaje |

Los mundos 12 (Invitados del Orinoco) y 13 (El origen) no tuvieron investigación nueva en esta ronda: valen research/07 §1 y 07 §2.1.

---

## 4. Oficios, herramientas, objetos y comida

| Tema | Dato | Marca | Fuente | Uso posible [P] | Sensibilidad |
|---|---|---|---|---|---|
| **Matapi** (trampa de camarón) | **Verificado:** trampa semifija hecha por los propios pescadores, hoy de hierro galvanizado y tela de nylon. Se cala en orillas, canales y lagos de várzea cerca de Santarém. El camarón regional abunda en la estación seca (junio-diciembre). **Estimado:** forma de cilindro con embudos (el camarón entra y no sale); se cala de noche y se revisa por la mañana. **Corrección de los jueces:** el resumen del estudio **no** compara el matapi con la red de arrastre ni dice que sea «selectivo». Describe una pesca «desordenada», «sem fiscalização», que captura «indivíduos de todos os tamanhos» | V / E | F35 | Mecánica «Matapi» [P]: lo que entra no sale | Baja como objeto cotidiano. No presentarlo como práctica sostenible. Material tradicional (jupati) sin verificar [NV] |
| Curral de pesca | Cerca de varas clavadas en el fondo. Una pared guía (espia) lleva el pez a un recinto con una boca sin retorno. En la costa de Pará se usa desde el siglo XIX | E | F36 (servidor sin respuesta; dato del extracto), F37 | Alternativa de nombre; más costero que ribereño | Baja |
| Cacuri | El buscador lo asocia a una trampa en las piedras de cachoeiras como la de Iauaretê, lugar sagrado de los pueblos del Uaupés y el Papuri | NV | F38 (la página no mostró el dato) | **Ninguno** | Alta |
| Tarrafa o atarraya | Red circular con plomos en el borde (radio de 1,2-3,6 m) que se lanza a mano: se abre en el aire, se hunde y se cierra tirando de la cuerda | V | F39, F40 | Animación de la Pororoca o de una red sobre una fila | Baja. Evitar el arpón (arma) y el barbasco (veneno de pesca, [NV]) |
| Tipiti, farinha y tucupi | En la casa de farinha la mandioca se vuelve farinha, tapioca, goma, tucupi y beijus. El tipiti es una prensa cilíndrica tejida de ~2 m, de arumã o jacitara, de origen indígena (tupí), que exprime el jugo venenoso de la mandioca brava; hervido, ese jugo es el tucupi [V]. La Farinha de Bragança tendría Indicación Geográfica desde 2021, pero no lo dice ninguna de estas fuentes [NV] | V (IG: NV) | F53 (extracto), F54, F55, F56 | Evento «Casa de farinha» [P] | Media: tecnología indígena. Reconocer su origen. El tipiti prensa mandioca, no frutas |
| Tacacá y tacacazeiras | Se sirve muy caliente en cuia, por capas: tucupi, goma y camarones secos, con jambu. El IPHAN registró el Oficio de las Tacacazeiras de la Región Norte el 25-nov-2025 | V | F57, F58 | Evento «Hora del tacacá» con skin de cuia en cestita [P] | Media: oficio de mujeres recién reconocido; sin caricaturas |
| Açaí: peconha y paneiro | La peconha es un aro de hoja retorcida que se ata a los pies para trepar el açaizeiro. Una familia de la isla de Cumbu (Belém) cosecha hasta 8 paneiros (cestas) en temporada alta | V | F50 | El **paneiro** como visual de la Canasta (glosario §7.1) | Ver la fila siguiente |
| Açaí: trabajo infantil | La cosecha está prohibida a menores de 18 y figura entre las peores formas de trabajo infantil. Hubo casos en julio de 2023 en el Bajo Tocantins (Abaetetuba, Cametá, Igarapé-Miri y otros). Riesgos: caídas, cortes y serpientes | V | F52 | Regla de arte: nada de trepar palmeras (ni Capi), nada de niños cosechando | Alta |
| Comida envuelta en hoja | **Bijao** (*Calathea lutea*): hojas de 30-150 cm con envés ceroso [V F113]; envuelve el juane (Perú), el maito (Ecuador) y otros. **Maito:** se envuelve en hojas de bijao [V F114]; otra fuente dice hojas de banano «atadas con fibras naturales» y traduce «maito» como «envoltura» [V F118]. Que la fibra sea paja toquilla y que el nombre sea kichwa no está en ninguna fuente [NV]. **Juane:** se come el 24 de junio, fiesta de San Juan; una fuente dice que su forma «recuerda» la cabeza del profeta [V F115, F116]. **Patarashca:** pescado en bijao, típico de Loreto [E F117]. (El tacacho no va aquí: es plátano machacado y no se envuelve [V F119].) | V / E / NV | F113-F118 | La hoja atada como aspecto alternativo de la Hoja (M1) en los mundos 4-9 | Juane: sin iconografía de San Juan. Maito: con crédito. Especie del bijao a confirmar (la Wikipedia en inglés dice heliconia) |
| Crisneja | Ver la sección 3.6 | V | F128 | Base de la Hoja de palma | Baja |
| Chambira (fibra) | *Astrocaryum chambira*: palmera espinosa de más de 25 m. Con su fibra se tejen hamacas, bolsos y redes; la venta a turistas es fuente de ingresos | V | F126 | Material liso del borde de la cuia (07 §2.2: «Huingo, borde de chambira») | Media: los diseños son de artesanos indígenas; no copiarlos |
| Huayruro | Semilla de *Ormosia*, roja o roja y negra, usada en collares y amuletos «de buena suerte» | E | F127 | Como mucho, un detalle decorativo | Media: no usarlo como moneda ni en premios de azar |

---

## 5. Naturaleza: fauna y flora que sostienen mecánicas

| Especie o elemento | Dato | Marca | Fuente | Uso posible [P] | Sensibilidad |
|---|---|---|---|---|---|
| **Aguapé** (*Pontederia crassipes*) | Nativo de la cuenca amazónica. Flota gracias a pecíolos llenos de aire, forma alfombras densas que tapan el agua y tiene flores de lavanda a rosa. Es invasor fuera de su área. Nombres en español: jacinto de agua, lirio acuático, buchón de agua, camalote, aguapey, lechuguín, tarope y otros, sin país [V F110]. En Brasil: aguapé, mururé, orelha-de-veado, pavoá, rainha-do-lago y baronesa [V F111]. El nombre de cada país hispanohablante no está verificado [NV] | V (nombres por país: NV) | F109, F110, F111 | Equivalente del hielo de MS | No presentarlo como plaga en su área nativa |
| Manatí amazónico (peixe-boi) | Come plantas acuáticas, aguapé entre ellas, y evita que los lagos se tapen. Especie Vulnerable. En la vaciante «mide» los cuellos de botella de los lagos para salir a tiempo | V | F142, F143, F148 | Animación opcional del Aguapé | No está en el elenco (glosario §3.3): lo decide el dueño |
| Vitória-régia (*Victoria amazonica*) | Hojas de hasta 3 m. La flor se abre blanca y al segundo día pasa a rosa rojiza. Atrapa de noche a escarabajos *Cyclocephala*, que la polinizan. Crece en aguas quietas o lentas de hasta 5,25 m. Nombres en la página: Vitória-Régia o Iaupê-Jaçanã (Brasil) y «Atun Sisac», que la página atribuye al quechua inca | V | F112 (consulta propia, 2026-10-08) | Variante «Flor» de dos llaves (blanca → rosa) [P] | La hoja redonda ya es la Hoja escudo. No usar la leyenda de Naiá y Jaci, de la mitología tupí [V F150]. «Atun Sisac» no es léxico común de la región: no usarlo sin revisión. Hay dos cifras distintas de peso que soportó una hoja (194 o 226 kg): no citar ninguna |
| Lianas | Trepadoras leñosas que suben a la copa; hacen de puente para monos y perezosos; una *Bauhinia* de Surinam llegó a 600 m | V | F131 | Base de la Liana (M3) | No usar lianas rituales ni medicinales (ayahuasca) ni el cipó-ambé [NV] |
| Cipó-titica | Fibra de cestería. Es la raíz aérea de una planta hemiepífita, no una liana en sentido estricto | E | F132 (fragmento) | Matiz para el Dato del río | Baja |
| Peces frugívoros (tambaquí o gamitana, *Colossoma macropomum*) | En el igapó, durante la inundación, los peces se comen casi toda la fruta que cae al agua y dispersan las semillas [V]. La gamitana: hasta 1,1 m y 44 kg; del 78 al 98 % de su dieta en la creciente son frutos; pasa 4-7 meses en el bosque inundado [V]. Una tesis de la UFAM le atribuye 133 especies de frutos [E: F19 da 404; hace falta una fuente sustituta] | V / E | F18, F90; F19, F20 | Dato del río; versión amable de la Piraña | Baja |
| Piraña | Omnívora: come peces, insectos, frutas, semillas y hojas. Anda en cardumen para defenderse. Los ataques a personas son raros y casi siempre ocurren con aguas bajas | V | F140, F141 | Piraña (M7) sin sangre: se lleva fruta | Baja |
| Pirarucú o paiche (*Arapaima*) | Hasta 3 m. Sube a respirar aire, lo que lo hace vulnerable a la pesca. Con manejo comunitario en Brasil, en una muestra de 10 áreas, pasó de 2.500 (1999) a más de 170.000 (2017) [V F91]; se pesca como mucho el 30 % de los adultos [V F45]. Todo Dato del río dice «en 10 áreas muestreadas» | V | F91, F44, F45 | Animación del revelado; Datos del río según el país (sección 3.9) | Baja |
| Charapa y taricaya (*Podocnemis*) | Desovan en las playas de vaciante; las crías salen cuando el río empieza a subir. El programa de manejo de quelonios de la Reserva Nacional Pacaya-Samiria liberó unas 179.000 crías, ~57.000 de ellas de charapa, e inició un manejo participativo con comunidades locales (cifras de 1996). Las taricayas se asolean «sobre las palizadas» | V | F14, F136 | Base de la «Palizada» y Dato del río de conservación con fecha y crédito a las comunidades | No mostrar la recolección ni el consumo de huevos, y no moralizar. En el Perú, «charapa» también es el gentilicio coloquial de la gente del oriente peruano [V F155]: en nombres visibles, «tortuga» o «taricaya»; «charapa» solo en Datos del río con contexto. Pasa al asesor (07 §8.2) |
| Troncos en la vaciante | En la vaciante del Madeira aparecen troncos y «paliteiros» que bajan «como lanzas» y perforan los cascos de madera | V | F135 | Base de la «Palizada» | El término PT «paliteiro» connota peligro |
| Boto (delfín rosado) | Juega con palos, troncos y tortugas | V | F144 | Rosa como llave de la Palizada (opción del dueño) | La leyenda del encantado se omite (glosario §3.2). Según la página de la leyenda, «é frequentemente associada [...] a uma forma de encobrir ou camuflar casos de violência e abuso sexual» [V F151] |
| Castaña y añuje (agutí) | Un coco leñoso de hasta 2 kg, con 8-24 semillas, que tarda 14 meses en madurar. El agutí lo roe por un orificio, come unas semillas y entierra otras, que germinan. El árbol fructifica casi solo en bosque primario, porque lo polinizan abejas grandes. Casi toda la producción es silvestre | V | F93 | Base del Brote; evento «Zafra» | Nombre del añuje por país (añuje, cutia, guatusa, picure): asesor |
| Camu camu | Crece en orillas inundables y se cosecha en canoa entre las ramas en la temporada húmeda [V]; con agua baja hay que trepar [E]. Loreto tiene las mayores poblaciones naturales [E] | V / E | F121 | Dato del río del tinte (debut en el 21) | Baja |
| Aguaje | Palmeras de 25-35 m. En Loreto se consumen ~50 t diarias. Muchos cosechan **talando** las palmeras hembra; con «subidores» se corta solo el racimo y la palmera da fruto hasta 40 años | V | F122 | Dato del río del tinte Aguaje | Nunca mostrar la tala |
| Aguaje: semilla que flota | La semilla flota y la reparte el agua | V | F133 | Base del Brote | Baja |
| Ungurahui, umarí y cocona | Frutas de mercado, de tonos parecidos a tintes que ya existen (el ungurahui cae en el par 8-9) | E / V | F123, F124, F125 | Mercado y Datos del río, no tintes nuevos | Baja |
| Sumaúma o lupuna (*Ceiba pentandra*) | Árbol sagrado en varias culturas. En el Perú, los Ese Ejja la consideran «planta maestra» en el chamanismo de ayahuasca, y los Shipibo-Conibo creen que un espíritu del árbol causa visiones [V F153]. Los indígenas de la Amazonía la llaman «mãe-das-árvores» [V F154]. (La primera comprobación usó F152, una desambiguación sin contenido) | V | F153, F154 | **Ninguno** | Vetada: árbol sagrado ligado a la ayahuasca (07 §8.1). Toca sobre todo a los mundos 7 Madre de Dios y 9 Ucayali [E] |
| Garza que vigila | Idea de la línea «naturaleza» para un equivalente de la tela | NV | — | No se usa | Sin ficha |

---

## 6. Cómo diseña la dificultad el género

### 6.1 Magic Sort observado en el teléfono (niveles 387-389) [L1]

| Nivel | Etiqueta | Intentos | Qué pasó | Lectura |
|---|---|---|---|---|
| 387 | Ninguna | 6 | 1 abandono y 4 bloqueos; gana tras pagar 900 monedas | Un pico **sin aviso** |
| 388 | Ninguna | 1 | Gana a la primera, en 130 s, sin ayudas, con una mecánica nueva (hielo) | **Respiro con novedad** tras el pico |
| 389 | SUPER HARD | 12 | 11 bloqueos y 1 abandono; bloqueos a los ~6 s tras 1-2 vertidos | Pico **anunciado** |

- **Anuncio previo.** Tras ganar el 388, el botón del inicio pasa de verde («Level 388») a rojo («Super Hard / Level 389»), con una etiqueta «x5» [V L1 §3.6].
- **Premio.** En el tablero, el SUPER HARD muestra 100 monedas frente a 20. Se deduce que el «x5» es su multiplicador [E L1 §3.6].
- **Reintentos.** Cada intento sirve otra distribución, con la misma plantilla [V L1 §2.9]. El GDD no lo copia (D-008 #7).
- **Ayudas.** No resalta ningún destino ni da pistas implícitas [V L1 §2.10]. Coincide con D-002.
- **Mecánicas vistas:** capas «?», botella atada, tela con icono, hielo por fila, armario con candado y llave dentro del líquido [V L1 §3].
- **El gancho de fondo:** «cada color completado destapa algo» (gancho G1 de la ficha, L1 §12.1), con animación y sonido propios en el golpe del corcho [V L1 §3, §12.1; L2 §5.1]. No confundir con la fuente [G1] (Gamigion), que no habla de esto.

### 6.2 Magic Sort según la web [L3 §4-§5, §8; L4]

| Mecánica en las guías | Qué hace | Primer nivel | Marca |
|---|---|---|---|
| Frasco de capacidad reducida | Búfer | ~14 | V [G3] |
| Frascos cubiertos | Se vuelven utilizables al caer el color correcto | 25-60 | V [G3] |
| **Frasco «?» (magic bottle)** | Regla sin confirmar. Una guía dice que revela un solo color cuando recibe un vertido que coincide, que después solo admite ese color y que un segundo color lo deja en una pila de dos colores que bloquea [G3]. La guía del nivel 70 («six question-mark bottles») y la ficha de la App Store («question marks», colores desconocidos) lo describen como frasco de contenido oculto [G3 nivel 70; G5]. Hay que observarlo en el dispositivo (L1) | ~70 | E [G3, G5] |
| Paneles de madera | Ocultan una segunda fase del tablero | 150 | V [G3] |
| **Corchos por color** | Tapa con color-llave; puede llevar un número de vertidos | 221 | V [G3] |
| Huecos con candado | Vacíos que se liberan a mitad de nivel | 221 | V [G3] |
| **Cortina de teatro** | Oculta frascos extra; cae sola al haber N vacíos en la fila | 331 | V [G3] |
| Caja regalo | Se abre con cualquier vertido | 400 | V [G3] |
| **Caja fuerte encadenada** | La llave sale de otra caja | 442 | V [G3] |
| Caja con temporizador | Citada en la guía del nivel 1000 | ~1000 | V, una guía [G4] |

- **Cadencia de etiquetas.** No está publicada. Estimación: 1 Hard cada 5-8 niveles y 1 Super Hard cada 15-25 [E L3 §5].
- **Diente de sierra verificado.** Hay tramos «expert» largos (43-112) y respiros puros con 2 vacíos (24, 200, 280, 420) [V G3].
- **Crítica:**
  - las mecánicas nuevas «se introducen tarde y carecen de novedad» [V G1];
  - «el desarrollador controla tu progreso»; la suerte pesa más que en match-3 [V G1];
  - el «?» que se vive como azar es la queja n.º 1 [V L4].

### 6.3 Otros referentes [L5 §5]

| Fuente | Patrón | Marca |
|---|---|---|
| Royal Match (niveles 1-100) | 8 Hard (~1,6 intentos) y 5 Super Hard (~2,5 intentos) en 100 niveles; un nivel normal ronda 1,2 intentos. 11 mecánicas nuevas en 91 niveles, cada una con 2-3 tableros de aprendizaje | V [G6] |
| Candy Crush | Pico y valle: un nivel muy difícil seguido de 5-6 fáciles. Poda recurrente de los 100 peores niveles | V [G10, G11] |
| indiecoredev | Ciclo de 20: 12 comodidad, 5 reto, 2 recuperación y 1 hito. Salto máximo de +2,2 bandas al subir y −3,4 al bajar | V [G8] |
| Mobile Game Doctor (2025) | Curva de sierra: alivio → construcción → clímax → repetir. «Siempre debe sentirse superable sin boosters» | V [G9] |
| Gamigion | Intentos 1-3 = fácil; 20-35 = difícil | V [G7] |

### 6.4 Nuestro canon hoy, en seis líneas [L7]

1. **Etiquetas.** Difícil cuando el bot ×40 falla el 75 % o más; Muy difícil, el 90 % o más. Sin calavera. Premios de 20, 60 y 80 semillas. Tarjeta previa y chip en el HUD (02 §10; 07 §7.7).
2. **Ciclo de 20** (03 §12). Retos en p6, p10, p14, p17 y p19; recuperación en p1-p2; hito en p20. El hito va **siempre** precedido de un reto (03 §11).
3. **Debuts de mecánicas** (02 §9.1; contrato 1). Hoja 43, Brote 63, Liana 83, Remolino 103, Barro 123, Lluvia 143 y Piraña 183, siempre en p3, con 2-3 tableros fáciles.
4. **Aguas turbias** (02 §11). Solo en Difícil y Muy difícil desde el 100. Como mucho 3 cuias y 4 dosis ocultas, y siempre un vertido seguro.
5. **Reintento** (03 §13). Mismo tablero hasta el tercer fallo; después «El río bajó».
6. **Combinaciones** (02 §9.2). Como mucho 2 mecánicas por nivel antes del 300 y 3 después.

### 6.5 Lecciones y hallazgos para Tintalú

1. **Anunciar antes de jugar y mostrar el premio multiplicado.** MS lo hace en el botón del inicio [L1]. El canon ya tiene la tarjeta. D-008 #6 prevé el chip en «Jugar» [P].
2. **Después del pico, un respiro.** En MS, entre el 387 y el 389, el 388 es fácil y trae novedad [L1]. El canon pone un reto en p19, justo antes del hito: dos niveles exigentes seguidos.
3. **La mecánica es una función, no una piel.** Hay que copiar la función que MS prueba y vestirla con algo real del río.
4. **Antes que MS.** Su crítica es «tarde y sin novedad» [G1]. Nuestro canon ya estrena 7 bloqueos antes del 185.
5. **Azar visible = queja.** Tapar lo que no se puede jugar da curiosidad sin azar. Ocultar dosis jugables es lo que se vive como suerte (L2 §5.4).
6. **Temporizadores a favor del jugador = trampa.** Un vertido fútil (monocolor → vacía) es legal (02 R13). Un contador que da algo «cada k vertidos» se puede gastar yendo y viniendo, sin pensar. Solo valen los contadores en contra: Lluvia, Piraña, y una ventana que se cierra.
7. **Hallazgo técnico 1: fútiles.** `futil` (02 §1.4) trata como fútil «monocolor → vacía» salvo destinos Lluvia o Remolino.
   - Con una **Hoja vacía**, verter allí el resto de su fruta libera una cuia general: es progreso, y el solver lo poda.
   - Pasa lo mismo con cualquier destino que cambia al recibir (Matapi, cuia bajo Aguapé) y con un origen que ya no puede recibir (Cocha cerrada).
   - Propuesta: nunca es fútil un vertido hacia un destino que cambia al recibir, ni desde un origen que ya no puede recibir. Eximir también a cualquier origen con mecánica sería un error: un ir y venir entre una cuia de Lluvia sin gotas y una vacía contaría como útil en los dos sentidos y el bloqueo no se detectaría nunca (propuesta, anexo B).
   - Hay que cubrirlo con tests de propiedad, entre ellos uno que compruebe que ningún ciclo de vertidos «útiles» vuelve al mismo estado canónico.
8. **Hallazgo técnico 2: orden de R10.**
   - Hoy R10 evalúa (b) «liberación de Liana» antes de (c) «sellado».
   - 02 §9.2 dice que el cliente guarda un «contador de sellos». Si ese contador sube en (c), una Liana que se suelta «en el instante en que se sellan n cuias» se soltaría un vertido tarde.
   - El solver lo deriva del estado (03 §9) y no tendría el retraso, así que juego y solver pueden discrepar.
   - Hay que fijar el orden en R10. Es contrato con el solver y con Marea.
   - Pasa lo mismo después de los contadores: una gota de Lluvia puede sellar una cuia. Por eso el sellado y las liberaciones se repiten tras los contadores hasta que nada cambie (propuesta, anexo B).
9. **Hallazgo técnico 3: posición en el estado.** `BoardCanon` ordena las cuias por su valor empaquetado y borra su posición (`Cuia.cs` tiene libres los bits 30-31, 35 y 39-63) [V, código]. Toda mecánica que dependa de la **vecindad** («la cuia de al lado») exige codificar posiciones y multiplica los estados. Mejor evitarlas o fijar su orden en los datos del nivel.
10. **Bloqueos inmediatos.** El 389 de MS bloquea en 1-2 vertidos. Hay que medir las «trampas tempranas» (L2 §5.5, opción B).

---

## 7. Lo que se evita por respeto (lista consolidada)

Se suma a 07 §8.1 y al glosario §3.3. Para los términos, «evitar» significa no usarlos en interfaz, nombres, comodines, ficha de tienda ni arte. Para hechos y lugares, significa no convertirlos en mecánica, decorado ni chiste. Un Dato del río solo los toca si lo dice la columna «Qué hacemos».

| Qué | Por qué | Marca | Fuente | Qué hacemos |
|---|---|---|---|---|
| «Caboclo» | Término peyorativo y además entidad religiosa afrobrasileña | V | F3 | «Ribeirinhos» o «gente del río», solo en Datos del río |
| Personas ribereñas e indígenas; aldeas, malocas | Canon 07 §8.1. Los dos estereotipos (atrasado o mítico) | V / E | F3-F5 | Objetos, oficios, barcos, casas |
| Parintins: Garantido, Caprichoso, bueyes estilizados, rojo contra azul con estética de boi, pajé, cunhã-poranga | Marcas licenciadas; rituales representados | V | F64-F66 | Toada propia (07 §2.2) |
| Cosecha de açaí con peconha; trepar palmeras; niños cosechando | Trabajo infantil documentado | V | F52 | Açaí solo como fruta, tinte y mercado |
| Cacuri | Asociado a un lugar sagrado indígena | NV | F38 | Matapi, curral o tarrafa |
| Cosmovisión kukama: karuara, «ciudad bajo el agua» | Creencia viva de un pueblo concreto | V | F99 | Nada en mecánicas ni fondos |
| Yacumama, Yacuruna, Chullachaqui, Tunche, Runamula, Cobra Grande, boto encantado, Iara, Curupira | Creencias vigentes (glosario §3.3) | V / E | F47, F101, F102, F151 | El Remolino se explica por la física del rebojo |
| Leyenda del boto | Además de su carga de seducción, se asocia a encubrir violencia y abuso sexual | V | F151 | Rosa es un animal con comportamiento real |
| Leyenda de Naiá y Jaci (vitória-régia) | Leyenda de la mitología tupí | V | F150 | Solo la flor y su biología |
| Matupá: leyenda de la Anaconda Gigante y el nombre | La gente del lugar asocia los matupás a la Anaconda Gigante; el nombre también significa «terra caída» | V | F49, F157 | Solo fondo, sin el nombre en la interfaz; con revisión |
| Sumaúma o lupuna (*Ceiba pentandra*) | Árbol sagrado: «planta maestra» en el chamanismo de ayahuasca de los Ese Ejja; espíritu del árbol para los Shipibo-Conibo; «mãe-das-árvores» para los indígenas de la Amazonía | V | F153, F154 | Nada. Proponer que entre en la lista de 07 §8.1 (mundos 7 y 9) |
| Cipó-ambé, ayahuasca y otras lianas rituales o medicinales | Uso ritual (07 §8.1) | NV | — | Lianas leñosas genéricas |
| Ticuna: máscaras, yanchama, pelazón, funerales | Objetos y ritos de un pueblo | V | F106, F107 | Nada |
| Kené shipibo, «turismo chamánico», pintura visionaria | Patrimonio (RD 540-2008) y apropiación | V (canon) | L7 07 §8.1; F108 | Nada |
| Ritual de la guayusa al amanecer | Rito de pueblos de la Amazonía ecuatoriana; la fuente describe a los Achuar | V | F120 | Como mucho, «agua de guayusa», con revisión. No atribuir el rito a un pueblo sin fuente |
| Pasaje Paquito | Plantas medicinales, baños rituales y bebidas de doble sentido sexual | V | F82 | Nada en Mercado ni fondos |
| Bancas de hierbas del Ver-o-Peso («remédios naturais, ervas e mandingas») | Dimensión religiosa | V | F63 | No dibujar objetos rituales |
| Venta de animales silvestres y carne de monte en mercados | Riesgo de fauna | NV | — | No mostrarla |
| Época del caucho en clave nostálgica; mito de Eiffel en la Casa de Fierro | Genocidio del Putumayo | V | F83, F85 | La arquitectura, solo como paisaje |
| Minería de oro (oro, pepitas, bateas) | Mercurio, deforestación | V | F96 | Nada en los mundos 7-8 |
| Terra caída, sequías de 2023-2024, grandes crecidas como chiste o mecánica | Desastres con víctimas | V | F9, F12, F137 | Solo Datos del río serios |
| Demolición de la Ciudad Flotante (1967), desalojos, pobreza de Belén o de Afuá como pintoresca; nombres de barrios pobres como títulos de nivel | Conflictos actuales; Belén: «many of whom live in extreme poverty» | V | F28, F31, F32 | Arquitectura sin miseria; los hitos se nombran por el fenómeno (p. ej., «Creciente del Itaya») |
| «Charapa» como nombre visible | En el Perú también es el gentilicio coloquial de la gente del oriente peruano | V | F155 | «Tortuga» o «taricaya» en nombres; «charapa» solo en Datos del río con contexto |
| Tala de aguaje; escenas de caza; arpón; barbasco | Prácticas dañinas o armas | V / NV | F122 | Cosecha con subidor, si acaso |
| Huayruro como moneda o premio de azar | Uso como amuleto | E | F127 | Detalle decorativo, si acaso |
| Fiestas religiosas (Círio de Nazaré, Çairé, Marujada) como eventos; juane con iconografía de San Juan | Fiestas religiosas | NV / V | F115 | Solo el ángulo de la comida o la cosecha |
| Pueblos aislados o de contacto inicial | Canon 07 §8.1 | V | L7 | Nada |
| Paiche como «especie a proteger» en Bolivia | Allí los biólogos lo consideran invasor, aunque no está declarado oficialmente; y a la vez es medio de vida | V | F92 | Dato con las dos caras |
| Copiar grafismos de las cuias de Santarém o patrones marajoaras | Patrimonio de un oficio; iconografía indígena | V | F59-F61 | Encargo pagado con consentimiento |
| Términos brasileños en mundos hispanohablantes (piracema, defeso, banzeiro, tipiti) | Incoherencia geográfica | E | — | Nombre local por mundo o debut en un mundo brasileño |
| Nombres ya ocupados: Canoa(s), Balsa, Crecida, Enchente, «El río bajó», Pororoca, Marea, Hoja escudo / Folha-escudo, Canasta, Cántaro, Mercado, Regata, Travesía, Temporada de lluvias, Cuia del Encuentro, Encuentro de las Aguas | Choque con el glosario §2-§7 | V | L7 | Nombres nuevos para mecánicas nuevas |

---

## 8. Pendientes de verificación

Ningún Dato del río se escribe con estos datos hasta cerrarlos. Los números se conservan; los cerrados en la revisión del 2026-10-08 lo dicen.

1. Material tradicional del matapi (jupati) y si algún diseño pertenece a un pueblo concreto.
2. El cacuri y su vínculo con Iauaretê.
3. La ficha del curral del ICMBio (el servidor no respondió).
4. Los nombres de las cinco comunidades y las ~200 familias de ASARISAN. Que son cinco comunidades de Aritapera ya está verificado [V F60].
5. La duración del recreio Manaos-Belém.
6. **Cerrado (2026-10-08):** las bancas del Ver-o-Peso venden «remédios naturais, ervas e mandingas» [V F63].
7. El estatus del Çairé y del Círio.
8. **Cerrado (2026-10-08):** 12,11 m el 9 de octubre de 2024 [V F12]; el 3 de octubre el río pasó el récord anterior con 12,68 m [V F9].
9. **Cerrado (2026-10-08):** informe del IGP sobre repiquetes [V F15].
10. **Cerrado (2026-10-08):** vaciante de julio a noviembre según el SENAMHI (2024) [V F27].
11. Las cifras de Sioli.
12. La neblina matinal (ARM).
13. La especie del bijao (Calathea o Heliconia).
14. El nombre del aguapé por país (lechuguín, tarope, camalote, buchón). «Huama» no aparece en ninguna fuente: queda fuera hasta tener una.
15. El nombre del añuje por país.
16. La especie de los loros de Leticia.
17. El comprador de la Casa de Fierro: Anselmo del Águila [F83] o Julio H. Toots [F84].
18. El papel de las comunidades en el manejo de los lagos de Tarapoto.
19. **Cerrado (2026-10-08):** la sumaúma o lupuna es árbol sagrado [V F153, F154]; queda vetada.
20. El cipó-ambé.
21. **Cerrado (2026-10-08):** la leyenda de Naiá y Jaci es de la mitología tupí [V F150]; queda vetada.
22. **Cerrado (2026-10-08):** los matupás se asocian a la Anaconda Gigante [V F49]; queda vetada.
23. Las fechas de la zafra (enero-abril o diciembre-marzo) y las concesiones de OSINFOR.
24. **Nuevos tras la revisión del 2026-10-08:**
    - una fuente brasileña de playas y lagos en la vazante (Alter do Chão);
    - cómo llegó el paiche a Bolivia en 1976;
    - la Indicación Geográfica de la Farinha de Bragança (fuente del INPI);
    - la paja toquilla del maito y el origen kichwa de su nombre;
    - si los barcos también levantan banzeiro y el banzeiro del Xingu;
    - fuentes sustitutas de F19 (133 especies de frutos) y F48 (matupá, 12 m);
    - el tiempo de Puerto Nariño a Tarapoto y si hay delfines grises;
    - la regla exacta del frasco «?» de Magic Sort, observada en el dispositivo (L1).
25. **Investigación aún no hecha:**
    - mercado y malecón de Leticia;
    - muyuna (remolino en el Perú) y su carga mítica;
    - suri;
    - Guayana Francesa y Surinam;
    - nombre local de la trampa de camarón en Loreto;
    - categorías de lagos (preservación, mantenimiento y uso) en los acuerdos de pesca.

---

## 9. Fuentes

### 9.1 Web (todas consultadas el 2026-10-08)

**Brasil: sociedad y términos**
- F1 Decreto 6.040/2007: https://www2.camara.leg.br/legin/fed/decret/2007/decreto-6040-7-fevereiro-2007-550693-norma-pe.html (texto leído en https://www.planalto.gov.br/ccivil_03/_ato2007-2010/2007/decreto/d6040.htm)
- F2 Ribeirinhos (pt.wikipedia): https://pt.wikipedia.org/wiki/Ribeirinhos
- F3 Caboclo (Encyclopedia of Latin American History and Culture): https://www.encyclopedia.com/humanities/encyclopedias-almanacs-transcripts-and-maps/caboclo
- F4 Revista UFS, discursos sobre la várzea: https://periodicos.ufs.br/revec/article/download/4770/3990/13613
- F5 UFAM, el «caboclo amazónico» [sin respuesta]: https://tedehomologacao.ufam.edu.br/items/aaffbb9a-8e0f-4f58-b722-5fc79cd5f54a

**El río**
- F6 Enchente e vazante (Portal Amazônia, 2023-03-07): https://portalamazonia.com/amazonia/portal-amazonia-responde-como-funcionam-os-processos-de-enchente-e-vazante-dos-rios/
- F7 Vazante (Portal Amazônia, 2023-11-23): https://portalamazonia.com/amazonia-de-a-a-z/vazante/
- F8 Récord de crecida del Negro (2021-06-01): https://portalamazonia.com/cidades/cheia-do-rio-negro-alcanca-recorde-historico-em-manaus-confira-imagens/
- F9 El Negro vuelve a subir (2024-10-16): https://portalamazonia.com/meio-ambiente/rio-negro-manaus-volta-a-encher-2024/
- F10 Repiquete (2021-06-16): https://portalamazonia.com/amazonia-de-a-a-z/repiquete/
- F11 Acta Amazonica (2001), régua de Manaos: https://www.scielo.br/j/aa/a/KH8cBzrnZSQTSppFSB55Bjg/?lang=pt
- F12 Mayores vaciantes del Negro (2024-10-10): https://portalamazonia.com/sem-categoria/saiba-quais-foram-as-maiores-vazantes-do-rio-negro-em-manaus/
- F13 Várzea forest (en.wikipedia): https://en.wikipedia.org/wiki/V%C3%A1rzea_forest
- F14 Soini, Folia Amazónica 8(1), IIAP 1996: https://revistas.iiap.gob.pe/index.php/foliaamazonica/article/download/310/271
- F15 IGP, repiquetes (Figueroa et al., 2020; resumen completo): https://www.gob.pe/institucion/igp/informes-publicaciones/1335298-on-the-relationship-between-reversal-of-the-river-stage-repiquetes-rainfall-and-low-level-wind-regimes-over-the-western-amazon-basin
- F16 Repiquete (ProZ) [403]: https://proz.com/kudoz/6927745
- F17 Igapó y várzea (Portal Amazônia): https://portalamazonia.com/amazonia/voce-sabe-a-diferenca-entre-igapo-e-varzea-confira
- F18 Igapó (en.wikipedia): https://en.wikipedia.org/wiki/Igap%C3%B3
- F19 Tesis UFAM, tambaquí [404]: https://tede.ufam.edu.br/handle/tede/8545
- F20 In the flooded forest [fragmento]: https://app.waterbear.com/in-the-flooded-forest
- F21 Pororoca (en.wikipedia): https://en.wikipedia.org/wiki/Pororoca
- F22 Pororoca (pt.wikipedia): https://pt.wikipedia.org/wiki/Pororoca
- F23 Regreso de la pororoca del Araguari (2025-04-16): https://portalamazonia.com/amapa/rio-araguari-pororoca-11-anos/
- F24 Pororocas del Araguari extinguidas (IHU): https://ihu.unisinos.br/noticias/544931-as-pororocas-do-rio-araguari-no-amapa-foram-extintas-pela-mao-do-homem
- F25 Tombamento del Encontro das Águas (Confea; verificado por el redactor): https://www.confea.org.br/node/109909
- F26 Friagem (pt.wikipedia; verificado por el redactor): https://pt.wikipedia.org/wiki/Friagem
- F27 SENAMHI, inicio de la vaciante (2024-07-18): https://www.gob.pe/institucion/senamhi/noticias/990984-rios-amazonicos-inician-temporada-de-vaciante
- F28 Belén District, Maynas (en.wikipedia): https://en.wikipedia.org/wiki/Bel%C3%A9n_District,_Maynas

**Casas, barcos y lugares de Brasil**
- F29 Palafitas (UFPA): https://periodicos.ufpa.br/index.php/ncn/article/download/9316/8425
- F30 Vivienda ribereña (UPC) [418]: https://upcommons.upc.edu/entities/publication/a2357cfe-7bcb-440a-81d3-05cb3fd907b8/full
- F31 Afuá (pt.wikipedia): https://pt.wikipedia.org/wiki/Afu%C3%A1
- F32 Flutuantes (2021-06-14): https://portalamazonia.com/cidades/flutuantes-casas-sobre-os-rios-da-amazonia/
- F33 Embarcaciones (2022-03-12): https://portalamazonia.com/amazonia/conheca-os-tipos-de-embarcacoes-mais-usados-na-amazonia/
- F34 Manaus a Belém de barco (foro) [NV]: https://www.mochileiros.com/topic/977-manaus-a-bel%C3%A9m-de-barco/

**Pesca**
- F35 Matapi (Coêlho, Imbiriba, Serrão y Zacardi, Gaia Scientia 14(1), 2020): https://periodicos.ufpb.br/index.php/gaia/article/view/50238
- F36 Curral (ICMBio) [sin respuesta]: https://assiste.icmbio.gov.br/cepsul/images/stories/artes_de_pesca/artesanal/armadilha/cercofixo_curral.pdf
- F37 Curral (disertación UFPA, 2017): https://www.ppgeap.propesp.ufpa.br/ARQUIVOS/dissertacoes/2017/PPGEAP_Dissertac%cc%a7a%cc%83o_Cl%c3%a1udia%20Cristina%20Lima%20Mar%c3%a7al_2017.pdf
- F38 Cacuri (ISA) [NV]: https://acervo.socioambiental.org/acervo/tags/armadilha
- F39 Tarrafa (pt.wikipedia): https://pt.wikipedia.org/wiki/Tarrafa
- F40 Atarraya (es.wikipedia): https://es.wikipedia.org/wiki/Atarraya
- F41 Defeso de 8 especies (O Imparcial, 2019-11-15): https://oimparcial.com.br/meio-ambiente/2019/11/comeca-hoje-o-periodo-de-defeso-de-8-especies-de-peixes/
- F42 Defeso del tambaqui (2023-10-03): https://portalamazonia.com/amazonas/tambaqui-entra-no-periodo-de-defeso-reprodutivo-ate-marco-de-2024/
- F43 Piracema (2023-10-09): https://portalamazonia.com/meio-ambiente/portal-amazonia-responde-o-que-e-a-piracema/
- F44 Conteo de pirarucú (2018-12-05): https://portalamazonia.com/meio-ambiente/saber-antigo-de-contar-pirarucu-e-aliado-ao-manejo-sustentavel-da-especie-entenda/
- F45 Ficha del Instituto Mamirauá (Fundação Banco do Brasil): https://transforma.fbb.org.br/gestao-compartilhada-dos-recursos-pesqueiros/generate-pdf?id=80
- F46 Banzeiros (2022-07-15): https://portalamazonia.com/amazonia/portal-amazonia-responde-o-que-sao-os-banzeiros/
- F47 Rebojo (2022-01-07): https://portalamazonia.com/amazonia-de-a-a-z/rebojo/
- F48 Matupá (INPA) [404]: https://repositorio.inpa.gov.br/handle/1/14702
- F49 Islas flotantes (Atlas Obscura, 2015-05-12): https://www.atlasobscura.com/articles/the-science-and-legend-of-the-amazon-s-floating-forest-islands

**Comida y oficios de Brasil**
- F50 Cosecha de açaí (CNN Brasil, 2025-11-17): https://www.cnnbrasil.com.br/nacional/norte/pa/conheca-a-tecnica-de-colheita-do-acai-utilizada-nas-comunidades-ribeirinhas/
- F51 Açaí (The Intercept Brasil) [403; solo extracto]: https://www.intercept.com.br/?p=282743
- F52 Trabajo infantil en el açaí (Brasil de Fato, 2023-08-06): https://www.brasildefato.com.br/2023/08/06/com-flagrantes-de-trabalho-infantil-cadeia-produtiva-do-acai-fica-na-mira-da-fiscalizacao-ate-o-final-da-colheita
- F53 Farinha d'água [extracto]: https://portalamazonia.com/amazonia/descubra-como-e-feita-a-farinha-d-agua-tipica-da-amazonia
- F54 Tipiti (2021-12-21): https://portalamazonia.com/amazonia/conheca-o-tipiti-tecnologia-indigena-de-uso-secular-na-amazonia/
- F55 Casa do forno: https://portalamazonia.com/cultura-alimentar/a-casa-do-forno-patrimonio-sociocultural-da-amazonia-paraense/
- F56 Tipiti (pt.wikipedia): https://pt.wikipedia.org/wiki/Tipiti
- F57 Tacacá (Folha PE, 2023-12-03): https://www.folhape.com.br/sabores/conheca-o-tacaca-prato-de-origem-indigena-tipico-do-norte-do-brasil/304471/
- F58 Oficio de las tacacazeiras (2025-11-26): https://portalamazonia.com/cultura/oficio-tacacazeiras-patrimonio-br/
- F59 Cuias del siglo XVIII (Agência FAPESP, 2024-10-18): https://agencia.fapesp.br/cuias-feitas-por-mulheres-indigenas-abasteciam-o-mercado-de-luxo-europeu-no-seculo-xviii/53069
- F60 Cuias y lujo europeo (2024-10-21): https://portalamazonia.com/cultura/cuias-mulheres-indigenas-amazonia-luxo-europeu/
- F61 Carvalho, IPHAN (UERJ): https://www.e-publicacoes.uerj.br/tecap/article/view/12608
- F62 Ver-o-Peso Complex (en.wikipedia): https://en.wikipedia.org/wiki/Ver-o-Peso_Complex
- F63 Ver-o-Peso (2021-05-12): https://portalamazonia.com/amazonia-de-a-a-z/ver-o-peso/
- F64 Festival de Parintins (pt.wikipedia): https://pt.wikipedia.org/wiki/Festival_Folcl%C3%B3rico_de_Parintins
- F65 Parintins y Pavulagem, patrimonio (Agência Brasil, 2024-09-05): https://agenciabrasil.ebc.com.br/geral/noticia/2024-09/festival-de-parintins-e-reconhecido-patrimonio-cultural
- F66 Marca de los bois (2026-06-21): https://portalamazonia.com/cultura/marca-bois-parintins/
- F67 Açaí de Codajás, IG del INPI: https://portalamazonia.com/cultura/valorizacao-regionac-acai-de-codajas-recebe-registro-de-indicacao-geografica-do-inpi/
- F68 Festa do Cupuaçu [extracto]: https://portalamazonia.com/tudo-sobre/festa-do-cupuacu/
- F69 Cirandas de Manacapuru [extracto]: https://portalamazonia.com/amazonas/flor-matizada-tradicional-e-guerreiros-mura-conheca-o-festival-de-cirandas-de-manacapuru/
- F70 Carimbó (Brasil de Fato, 2017-02-24) [extracto]: https://www.brasildefato.com.br/2017/02/24/carimbo-manifestacao-cultural-que-retrata-a-identidade-do-povo-paraense
- F71 Carimbó (Revista CPC, USP): https://revistas.usp.br/cpc/article/view/74966
- F72 Chibé (2021-07-08): https://portalamazonia.com/amazonia-de-a-a-z/c/chibe/

**Países andinos y vecinos**
- F73 Peque peque (es.wikipedia): https://es.wikipedia.org/wiki/Peque_peque
- F74 Yurimaguas-Iquitos (blog): https://thatadventurer.co.uk/yurimaguas-to-iquitos-by-boat/
- F75 Yurimaguas-Iquitos-Pucallpa (blog): https://southamericabackpacker.com/local-riverboat-travel-in-peru-yurimaguas-to-iquitos-to-pucallpa/
- F76 Iquitos a Leticia (Gecko Routes): https://www.geckoroutes.com/es/colombia/iquitos-a-leticia/
- F77 Napo River (en.wikipedia): https://en.wikipedia.org/wiki/Napo_River
- F78 Canoas-taxi de Coca (Expreso, 2016-10-04): https://www.expreso.ec/actualidad/canoas-son-lsquo-taxis-rsquo-comunas-coca-53085.html
- F79 Belén (UCLA IEO): https://ieo.ucla.edu/peru-floating-city-of-belen/
- F80 Iquitos (en.wikipedia): https://en.wikipedia.org/wiki/Iquitos
- F81 Mercado de Belén (RPP, 2023-06-27): https://rpp.pe/peru/loreto/nuestra-tierra-rpp-visito-el-mercado-de-belen-en-iquitos-noticia-1492356
- F82 Pasaje Paquito (Andina, 2024-12-31): https://andina.pe/agencia/noticia-iquitos-tradicionales-productos-ancestrales-ofrece-pasaje-paquito-para-recibir-2025-1013375.aspx
- F83 Casa de Fierro (en.wikipedia): https://en.wikipedia.org/wiki/Casa_de_Fierro
- F84 Casa de Fierro (Atlas Obscura): https://www.atlasobscura.com/places/casa-de-fierro-iquitos-peru
- F85 Putumayo genocide (en.wikipedia): https://en.wikipedia.org/wiki/Putumayo_genocide
- F86 Mijano (Andina, 2019-08-07): https://andina.pe/agencia/noticia-pesca-artesanal-no-sera-afectada-construccion-hidrovia-amazonica-762611.aspx
- F87 Peces migratorios (WCS Perú, 2019-11-11): https://peru.wcs.org/es-es/WCS-Peru/Noticias/articleType/ArticleView/articleId/13469/Estudio-revela-que-peces-migratorios-continentales-se-reproducen-en-los-rios-Maranon-y-Huallaga.aspx
- F88 Tahuampas (Lifeder): https://www.lifeder.com/tahuampas/
- F89 Pucallpa (Living in Peru): https://livinginperu.com/what-to-do-in-pucallpa-a-city-off-the-tourist-route
- F90 Tambaqui (en.wikipedia): https://en.wikipedia.org/wiki/Tambaqui
- F91 Arapaima (en.wikipedia): https://en.wikipedia.org/wiki/Arapaima
- F92 Paiche invasor en Bolivia (Mongabay, 2025-07-05): https://es.mongabay.com/2025/07/paiche-invasor-pez-gigante-amenaza-amazonia-bolivia-reportaje-fotografico/
- F93 Brazil nut (en.wikipedia): https://en.wikipedia.org/wiki/Brazil_nut
- F94 Zafra castañera (Actualidad Ambiental, 2011-01-05): https://www.actualidadambiental.pe/inician-zafra-castanera-2011-en-la-reserva-nacional-tambopata-y-parque-nacional-bahuaja-sonene/
- F95 Riberalta (en.wikipedia): https://en.wikipedia.org/wiki/Riberalta
- F96 Puerto Maldonado (en.wikipedia): https://en.wikipedia.org/wiki/Puerto_Maldonado
- F97 Nauta (en.wikipedia): https://en.wikipedia.org/wiki/Nauta
- F98 Nauta (Adventure Life): https://adventure-life.com/peru/nauta
- F99 Guardianas del Marañón (Mongabay, 2024-04): https://es.mongabay.com/2024/04/guardianas-del-maranon-mujeres-kukama-derechos-rio-peruano/
- F100 Ratifican la sentencia del Marañón (Actualidad Ambiental): https://www.actualidadambiental.pe/ratifican-sentencia-que-declara-al-rio-maranon-y-sus-afluentes-como-sujeto-de-derecho/
- F101 Leyendas de la selva peruana (Lifeder): https://www.lifeder.com/leyendas-selva-peruana/
- F102 Yacumama (en.wikipedia): https://en.wikipedia.org/wiki/Yacumama
- F103 Loros del parque de Leticia (Semana, 2017-09-18): https://www.semana.com/nacion/articulo/los-loros-que-viven-en-el-parque-de-leticia/540875
- F104 Puerto Nariño (Blu Radio): https://www.bluradio.com/turismo/puerto-narino-pesebre-natural-de-colombia-sin-vehiculos-ni-edificios-asi-se-llega-desde-bogota-so35
- F105 Lagos de Tarapoto, sitio Ramsar (SINCHI, 2018-02-09): https://en.sinchi.org.co/tarapoto-lakes-declared-ramsar-site-and-first-for-colombian-amazon
- F106 Máscara ticuna (LAM Museum): https://lammuseum.wfu.edu/2015/02/ticuna-body-mask/
- F107 Máscara ticuna (Mask Museum; legible el 2026-10-08): https://www.maskmuseum.org/mask/ticuna-shaman-1
- F108 Pucallpa (en.wikipedia): https://en.wikipedia.org/wiki/Pucallpa
- F120 Ritual de la guayusa (Primicias): https://www.primicias.ec/entretenimiento/gastronomia/guayusa-ritual-amazonia-ecuador-significado-suenos-75693
- F128 Vivienda ribereña de Nauta (tesis UNAP, 2015): https://repositorio.unapiquitos.edu.pe/handle/20.500.12737/4778?show=full
- F129 Amazonas, Venezuela (en.wikipedia): https://en.wikipedia.org/wiki/Amazonas_(Venezuelan_state)
- F130 Rupununi (en.wikipedia): https://en.wikipedia.org/wiki/Rupununi

**Flora, fauna y comida**
- F109 Pontederia crassipes (en.wikipedia): https://en.wikipedia.org/wiki/Pontederia_crassipes
- F110 Pontederia crassipes (es.wikipedia): https://es.wikipedia.org/wiki/Pontederia_crassipes
- F111 Aguapé (pt.wikipedia): https://pt.wikipedia.org/wiki/Aguap%C3%A9
- F112 Victoria amazonica (en.wikipedia; verificado por el redactor): https://en.wikipedia.org/wiki/Victoria_amazonica
- F113 Calathea lutea (es.wikipedia): https://es.wikipedia.org/wiki/Calathea_lutea
- F114 Maito de pescado (El Oriente): https://www.eloriente.com/articulo/como-preparar-maito-de-pescado-un-plato-tipico-de-la-amazonia-ecuatoriana/44665
- F115 Juane (Andina, 2022-06-24): https://andina.pe/ingles/noticia-juane-por-es-plato-emblematico-de-selva-y-simbolo-de-fiesta-san-juan-898244.aspx
- F116 Juane (en.wikipedia): https://en.wikipedia.org/wiki/Juane
- F117 Patarashca: https://en.comida-peruana.com/recetas/main-course/patarashca
- F118 Maito (TasteAtlas; el texto está en el HTML de la página): https://tasteatlas.com/maito-de-pescado
- F119 Tacacho (en.wikipedia): https://en.wikipedia.org/wiki/Tacacho
- F121 Camu camu (USC Plants and People): https://scalar.usc.edu/works/plants-and-people/camu-camu
- F122 Aguaje, cosecha sostenible (Actualidad Ambiental): https://www.actualidadambiental.pe/aguaje-entre-la-tradicion-y-la-urgencia-de-una-cosecha-sostenible/
- F123 Ungurahui (EcuRed): https://www.ecured.cu/Ungurahui
- F124 Poraqueiba sericea (en.wikipedia): https://en.wikipedia.org/wiki/Poraqueiba_sericea
- F125 Solanum sessiliflorum (en.wikipedia): https://en.wikipedia.org/wiki/Solanum_sessiliflorum
- F126 Astrocaryum chambira (en.wikipedia): https://en.wikipedia.org/wiki/Astrocaryum_chambira
- F127 Huayruro (wikipedia) [extracto]: https://www.wikipedia.org/wiki/Huayruro
- F131 Liana (en.wikipedia): https://en.wikipedia.org/wiki/Liana
- F132 Cipó-titica (Sumários) [fragmento]: https://www.sumarios.org/artigo/explora%C3%A7%C3%A3o-e-manejo-do-cip%C3%B3-titica-heteropsis-spp
- F133 Mauritia flexuosa (en.wikipedia): https://en.wikipedia.org/wiki/Mauritia_flexuosa
- F134 Tipos de río (Aguas Amazónicas) [NV en las cifras]: https://en.aguasamazonicas.org/waters/river-types
- F135 El Madeira en la vazante (2021-08-16): https://portalamazonia.com/amazonia-ribeirinha/o-rio-madeira-na-vazante-saberes-locais-riscos-e-impactos/
- F136 Arrau turtle (en.wikipedia): https://en.wikipedia.org/wiki/Arrau_turtle
- F137 Terras caídas (CNN Brasil): https://www.cnnbrasil.com.br/nacional/seca-no-amazonas-potencializa-deslizamentos-entenda-o-fenomeno-terras-caidas/
- F138 Ríos voladores (Portal Amazônia): https://portalamazonia.com/amazonia/portal-amazonia-responde-o-que-sao-os-rios-voadores
- F139 Lluvia de tarde (ARM) [NV, 404]: https://www.arm.gov/research/highlights/636
- F140 Piranha (en.wikipedia): https://en.wikipedia.org/wiki/Piranha
- F141 Las pirañas son cobardes (Mongabay, 2007-07): https://news.mongabay.com/2007/07/man-eating-piranha-are-actually-cowards/
- F142 Peixe-boi (2021-05-05): https://portalamazonia.com/amazonia/conheca-o-peixe-boi-da-amazonia-o-cantor-que-equilibra-o-ecossistema-da-regiao/
- F143 Amazonian manatee (en.wikipedia): https://en.wikipedia.org/wiki/Amazonian_manatee
- F144 Amazon river dolphin (en.wikipedia): https://en.wikipedia.org/wiki/Amazon_river_dolphin
- F145 Japurá River (en.wikipedia): https://en.wikipedia.org/wiki/Japur%C3%A1_River
- F146 Casiquiare canal (en.wikipedia): https://en.wikipedia.org/wiki/Casiquiare_canal
- F147 Laguna de Yarinacocha (es.wikipedia): https://es.wikipedia.org/wiki/Laguna_de_Yarinacocha
- F148 El manatí sabe cuándo migrar (Revista Pesquisa FAPESP): https://revistapesquisa.fapesp.br/en/amazonian-manatee-knows-just-when-to-migrate/
- F149 Lagos aislados y oxígeno (UEA) [NV, fragmento]: https://ri.uea.edu.br/bitstreams/7133aaf2-21c5-42b4-a83b-ac286846d33a/download
- F150 Leyenda de la vitória-régia (Toda Matéria): https://www.todamateria.com.br/lenda-da-vitoria-regia/
- F151 Lenda do boto (pt.wikipedia; verificado por el redactor): https://pt.wikipedia.org/wiki/Lenda_do_boto
- F152 Sumaúma (pt.wikipedia, desambiguación sin contenido; sustituida por F153 y F154): https://pt.wikipedia.org/wiki/Suma%C3%BAma

**Añadidas en la revisión del 2026-10-08**
- F153 Ceiba pentandra (en.wikipedia): https://en.wikipedia.org/wiki/Ceiba_pentandra
- F154 Ceiba pentandra (pt.wikipedia): https://pt.wikipedia.org/wiki/Ceiba_pentandra
- F155 «charapa» (Diccionario de la lengua española, RAE): https://dle.rae.es/charapa
- F156 «palizada» (Diccionario de la lengua española, RAE): https://dle.rae.es/palizada
- F157 Matupá (pt.wikipedia; etimología del término): https://pt.wikipedia.org/wiki/Matup%C3%A1

### 9.2 Género (consultadas el 2026-10-05 por la investigación previa; G1, G3 y G5 se volvieron a consultar el 2026-10-08)

- G1 Gamigion, deconstrucción de Magic Sort: https://www.gamigion.com/game-deconstruction-magic-sort-by-grand-games/
- G2 Naavik, Grand Games: https://naavik.co/digest/why-peak-games-investors-backed-grand-games/
- G3 levelsolve (mecánicas por nivel; páginas /level/14, 25, 43, 60, 70, 100, 150, 200, 221, 280, 331, 400, 420, 442; el nivel 70 en https://levelsolve.com/magic-sort/level/70): https://levelsolve.com/magic-sort/
- G4 magicsortlevel, nivel 1000: https://magicsortlevel.com/levels/1000
- G5 App Store Story de Magic Sort: https://apps.apple.com/au/iphone/story/id1873942358
- G6 Gamigion, diseño de niveles de Royal Match: https://www.gamigion.com/royal-match-level-design-insights/
- G7 Gamigion, cómo diseñar la dificultad: https://www.gamigion.com/how-to-design-difficulty-in-puzzle-games/
- G8 indiecoredev, ciclo de 20: https://dev.to/indiecoredev/difficulty-rates-a-stage-progression-places-it-5243
- G9 Mobile Game Doctor (2025-05-30): https://mobilegamedoctor.com/2025/05/30/illuminating-level-creation-for-free-to-play-puzzle-games/
- G10 King y la poda de niveles (mobilegamer.biz): https://mobilegamer.biz/how-king-defines-a-good-candy-crush-saga-level-and-why-it-constantly-prunes-the-bad-ones/
- G11 Candy Crush (Game Developer): https://www.gamedeveloper.com/design/candy-crush-saga-a-sweet-journey-into-monetization

### 9.3 Locales

- L1 `docs/research/magic-sort/ficha-magic-sort.md` (observación propia de 20 min; material en `Logs/device/magicsort/analisis/observar.json`).
- L2 `docs/research/magic-sort/propuesta-tintalu.md` §5.
- L3 `docs/research/magic-sort/a-nucleo-niveles.md` §4, §5 y §8.
- L4 `docs/research/09-referencia-magic-sort.md`.
- L5 `docs/research/02-generacion-niveles-dificultad.md` §5.
- L6 `docs/research/07-tematica-amazonas.md` §1-§6.
- L7 GDD: `docs/gdd/00-glosario.md`, `02-nucleo-reglas-ux.md`, `03-niveles-generacion-dificultad.md` y `07-arte-tematica-audio.md`; `docs/dev/decisiones.md`; `Packages/com.amazonfill.core/Runtime/AF.Core/Board/Cuia.cs`.
