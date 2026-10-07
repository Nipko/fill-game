# "Selva Fill" y variantes: cribado preliminar de nombre, marca y dominios

| Campo | Valor |
|---|---|
| Fecha | 2026-10-06 |
| Proyecto | Puzzle *water sort* de temática amazónica (cuias transparentes, ríos, açaí, camu camu). Interno: Amazon Fill. Mercados: CO, PE, MX → BR → US. Idiomas: ES, PT-BR, EN |
| Contexto | Descartados "Amazon Fill" (choque con Amazon.com) y "Aquazonia" (solicitud idéntica INPI 938306200, clase 41; ver `docs/setup/research/f-marca-aquazonia.md`) |
| Autor | Agente de investigación (Claude Code), para el dueño del producto |
| Estado | Borrador para decisión del dueño |
| Leyenda | **[V]** verificado con consulta reproducible (URL o número) · **[V-sec]** fuente secundaria · **[E]** estimación o criterio del agente · **[NV]** no verificable en esta sesión |

> **Aviso importante: esto no es asesoría legal.** Es un cribado preliminar hecho por un agente de IA en bases públicas, sin examen de fondo, sin búsqueda figurativa (logotipos) ni fonética con los algoritmos de cada oficina, y sin acceso a solicitudes aún no publicadas. Sirve para descartar nombres y preparar la conversación con un abogado de marcas. Ninguna conclusión garantiza que un nombre sea registrable ni que su uso no infrinja derechos de terceros.

---

## Respuesta corta

- **"Selva Fill": riesgo MEDIO-ALTO (alto en Brasil y México en la clase 41).** No hay ninguna marca, app ni dominio "Selva Fill". El problema es otro: **"FILL" es descriptivo** (la mecánica del juego), así que el elemento que pesa es **"SELVA", y "SELVA" a secas ya existe en la clase 41 para juegos y entretenimiento**:
  - **Brasil:** solicitud INPI **940179083 "SELVA"** (mixta, pendiente, publicada el 19-08-2025), que incluye literalmente *"jogos on-line, não baixáveis, providos em um ambiente virtual"* y *"serviços de entretenimento"* [V].
  - **México:** registro IMPI **3019615 "SELVA"** (mixta, concedida el 09-04-2026), que cubre *"servicios de entretenimiento"* [V].
  - Además, el conjunto "selva + fill" (≈ *jungle fill*) es **poco distintivo**: en EE. UU. puede verse como descriptivo por la doctrina de equivalentes extranjeros [E].
- **Mejor variante: "Selvagua" (selva + agua), riesgo BAJO-MEDIO** (bajo fuera de Brasil). Es una palabra inventada: **no hay ninguna marca idéntica ni casi idéntica en las clases 9, 28 o 41** de BR, CO, PE, MX, US, UE ni Madrid. Los antecedentes más cercanos tienen otros productos o están caducados (SALVAGUA, de fontanería; SELVARA, de eventos en MX). Se entiende sola en español y portugués, se pronuncia bien en inglés y cabe en los tres títulos de tienda (27, 22 y 26 caracteres). Segunda opción: **"Selvaqua"** (bajo-medio).
- **Nombres que no conviene usar:**
  - **"Selva Sort"** (alto): "sort" es el nombre del género; además, "sorte" (PT) y "sorteo" (ES) suenan a apuestas.
  - **"Fill da Selva"** (medio): nombre híbrido; excede los 30 caracteres en dos títulos y en PT suena a "filho da…".
  - **"Selva Splash"** (medio-alto): excede los 30 caracteres en el título en inglés.
- **Dominios:** **los cuatro TLD pedidos (.com, .app, .co, .com.br) aparecen libres para los ocho nombres**, incluido `selvafill.com`, que sirve para Selva Fill, SelvaFill y Selvafill [V: RDAP/WHOIS, 06-10-2026].
- **Tiendas:** ningún juego ni app con ninguno de los ocho nombres en Google Play (US, BR, MX, CO) [V]. La App Store solo pudo revisarse en parte, porque la API de iTunes bloqueó las consultas (403/429) a mitad del trabajo (sección 3).
- **Bases no consultables directamente:**
  - TMview, por el bloqueo del cortafuegos de la EUIPO.
  - INPI pePI, que no responde desde esta máquina.
  - INDECOPI Quipu, que redirige a una página de error.

  Esos datos se cubrieron con la **WIPO Global Brand Database**, que incluye BR, PE, MX, US, EM y WO, y con **SIC SIPI** para Colombia. La GBD **no tiene datos de Colombia**.

---

## 1. Método y bases consultadas

| Base | Cómo | Resultado | Estado |
|---|---|---|---|
| **WIPO Global Brand Database** (branddb.wipo.int) | Búsqueda por nombre en el navegador: coincidencia exacta, "contiene" y difusa. Filtros: clases 9, 28 y 41; oficinas BR, CO, PE, MX, US, EM y WO, y designaciones de esos países | 227 marcas con "SELVA" en 9/28/41 en esas oficinas. **Ninguna** "SELVA FILL", "SELVA POUR", "SELVA SPLASH", "SELVA SORT" ni "FILL DA SELVA" en todo el mundo. Fichas abiertas para los antecedentes críticos | [V] |
| **USPTO** (tmsearch.uspto.gov) | Backend JSON público de la búsqueda (solo lectura) | Ninguna coincidencia para los 8 nombres, ni para "jungle + fill/pour/splash". Hay marcas JUNGLE y SELVA en 9/41 (sección 2.3) | [V] |
| **IMPI México** (MARCia) | Búsqueda rápida "Similar Plus" en el navegador y fichas | Sin "SELVA FILL" entre los primeros 100 de 78.482 resultados similares. Muchas marcas SELVA (detalle en 2.2) | [V] |
| **SIC Colombia** (SIPI) | Búsqueda avanzada de signos distintivos: `*selva*` por clase y cada nombre con comodines | Sin resultados para `*selva*fill*`, `*selva*pour*`, `*selva*splash*`, `*selva*sort*`, `*fill*selva*`, `*selvagua*` ni `*selvaqua*`. `*selva*`: 10 marcas en la clase 41, 2 en la 9 y 2 en la 28 | [V] |
| **INPI Brasil** (pePI) | Sin respuesta (timeout) desde esta máquina | Cubierto con la GBD, que ofrece los datos y las fichas del INPI | [V vía GBD] / [NV directo] |
| **INDECOPI Perú** (Quipu) | Redirige a `indecopi.gob.pe/en/error` | Cubierto con la GBD (datos de Perú presentes) | [V vía GBD] / [NV directo] |
| **EUIPO / TMview** | TMview devuelve "Problem detected" (cortafuegos BigIP) para curl y para el navegador | EUIPO cubierta con la GBD (oficina EM); sin coincidencias | [V vía GBD] / [NV TMview] |
| **Google Play** | Página pública de búsqueda (US, BR, MX, CO) y fichas | Sección 3 | [V] |
| **App Store** | API de iTunes (US, BR, MX, CO, PE) | Parcial: la API devolvió 403/429 tras unas 20 consultas | [V parcial] |
| **Dominios** | RDAP autoritativo: Verisign (.com), Google Registry (.app) y registro.br (.com.br). WHOIS `whois.registry.co` para .co. Controles positivos: google y globo (ocupados) y un nombre inventado (libre) | Sección 6 | [V] |
| **Web** | Búsqueda web de cada nombre | Sin usos comerciales de ninguno de los 8 nombres | [V] |

**Nota técnica:** `rdap.org` **no sirve para .co**: devuelve 404 incluso para `google.co`, porque .co no publica RDAP en el bootstrap de IANA. Para .co se usó WHOIS.

---

## 2. Tabla por nombre

Riesgo global para el objetivo del dueño, "un nombre que no genere problemas" [E]. **Bajo:** sin antecedentes relevantes. **Medio:** antecedentes que un examinador o un tercero podría invocar, defendibles. **Alto:** antecedente idéntico o casi idéntico en el mismo tipo de servicio, o un nombre muy descriptivo.

| Nombre | Apps parecidas (Play / App Store / web) | Marcas encontradas (clases 9, 28, 41) | Dominios .com · .app · .co · .com.br | Connotaciones | Registrabilidad (clases 9 y 41) | Riesgo |
|---|---|---|---|---|---|---|
| **Selva Fill** (del dueño) | Ninguna "Selva Fill". "Fill" está saturado en puzzles: Garden Fill (100K+), Jelly Fill, Color Fill 3D, Maze Fill, Neon Fill, Zen Fill… [V] | Ninguna "SELVA FILL". **SELVA** idéntica en la 41: **BR 940179083** (pendiente; incluye juegos on-line), **MX 3019615** (registrada; entretenimiento). SELVA en la 9: BR 925767174 (software de criptomonedas). JUNGLE en 9/41 en US [V] | Libres los 4 [V] | ES/PT: "selva" se entiende, neutra o positiva. "Fill" no es palabra en ES ni PT. EN: "selva" es un término geográfico poco usado (diccionarios lo recogen como "selva tropical") y "fill" es descriptivo | **Débil.** Tema + mecánica (≈ "jungle fill"). El examinador verá SELVA como elemento dominante y FILL como descriptivo. Posible negativa o suspensión en BR-41 y MX-41. En US, riesgo de 2(e)(1) por equivalentes extranjeros o de cita de JUNGLE | **MEDIO-ALTO** (alto en BR y MX, clase 41; medio en US; bajo en CO y PE) |
| **SelvaFill / Selvafill** (una palabra; la mayúscula no cambia la marca) | Ninguna [V]. En la web, "-afill" aparece en rellenos dérmicos y dentales (Bellafill, *bulk-fill*) [V-sec] | Ninguna idéntica. Difusa: SELVAFIL (España, clase 23, hilos, 1996), BELLAFILL (clases 5/10). Mismos antecedentes SELVA [V] | Libres los 4 (mismo dominio que Selva Fill) [V] | EN: suena a producto cosmético o dental ("-afill") [E] | Algo más unitaria que "Selva Fill", pero "SELVA" sigue siendo reconocible al inicio | **MEDIO** |
| **Selva Pour** | Ninguna. Los resultados son juegos de aventura en la jungla [V] | Ninguna "SELVA POUR". Mismos antecedentes SELVA [V] | Libres los 4 [V] | ES: "pour" se lee "pur" o "por". PT: "pôr". Difícil de pronunciar y recordar en LatAm. EN: correcto, pero raro como nombre | Débil (SELVA + verbo descriptivo) | **MEDIO-ALTO** |
| **Selva Splash** | Ninguna. "Splash" muy usado: Splash (Ketchapp), Water Splash, Lumina Splash, Bubble Pop Splash [V] | Ninguna "SELVA SPLASH". SPLASH muy diluida en US (~30 marcas vivas en 9/28/41) [V] | Libres los 4 [V] | "Splash" se entiende en LatAm (anglicismo) | Débil: dos elementos diluidos | **MEDIO-ALTO** (y el título EN excede los 30 caracteres) |
| **Selva Sort** | Ninguna. Hay **Jungle Bird Sort** (Apollo Mobile Games, 1M+), un *sort* de temática jungla, y decenas de "Water Sort" [V] | Ninguna "SELVA SORT". En US hay solicitudes "WATER SORT PUZZLE" y "WATERSORT" en la clase 9, suspendidas [V] | Libres los 4 [V] | "Sort" es jerga del género. PT: "sorte" (suerte). ES: evoca "sorteo" → apuestas [E] | **La más descriptiva** (nombre del género). Protección casi nula | **ALTO** |
| **Fill da Selva** | Ninguna [V] | Ninguna. SELVA va al final y pesa menos [E] | Libres los 4 [V] | Híbrido EN + PT. En ES, "da" no es correcto ("de la"). En PT-BR suena a "filho da…", inicio de un insulto común: riesgo de burla [E] | Algo más distintiva por su estructura, pero torpe | **MEDIO** (no recomendable por uso y por longitud) |
| **Selvagua** (propuesta) | Ninguna app, juego ni uso web [V Play; App Store parcial] | **Ninguna idéntica en el mundo**. Difusa en 9/28/41: SALVAGUA (BR, clase 9, extinguida en 2007; MX, clase 11, fontanería), SELVARA (MX 3076587, clase 41, eventos y bodas), SOLAGUA (MX, clase 41, en trámite desde 2015), familia SELVAGEM (BR, clase 41) [V] | Libres los 4 [V] | ES: "selva + agua", transparente y agradable. PT: "selva + água" (forma correcta "Selvágua"; sin tilde casi todos dirán "sel-VA-gua"); también evoca "selvagem" (salvaje). EN: palabra exótica, pronunciable "sel-VAH-gwah". Posible confusión con "salvagua" (ahorra agua) [E] | **Evocativa, no descriptiva**: palabra inventada que sugiere el tema sin nombrar el producto. Es la variante más fuerte. El prefijo SELVA- sigue presente (ver BR 940179083) | **BAJO-MEDIO** (bajo en CO, PE, MX y US; medio-bajo en BR, clase 41) |
| **Selvaqua** (propuesta) | Ninguna. Play solo devuelve AquaPlants [V] | Ninguna idéntica. Difusa: SYLVAQUA (US 99803768, clase 44, acupuntura, pendiente), FELAQUA (clase 9, gateras electrónicas), VELAQUA (extinguida), SPLAQUA (US, clases 3/9/10) [V] | Libres los 4 [V] | "Aqua" es latín, entendible en los tres idiomas. Se parece a las marcas de agua y filtros (familia AQUA*) y recuerda a "Aquazonia" [E] | Evocativa, aceptable. Prefijo "aqua" diluido | **BAJO-MEDIO** |

---

## 3. Tiendas de aplicaciones (detalle)

| Búsqueda | Google Play (US, BR, MX, CO) | App Store (API de iTunes) |
|---|---|---|
| selva fill | Sin "Selva Fill". Juegos "Fill": Garden Fill (muskloftsoft, 100K+), Jelly Fill, Fill - one-line puzzle, Color Fill 3D, Maze Fill, Melt and Fill, Neon Fill, Zen Fill [V] | US, BR, MX, CO y PE: 0 resultados con "selva" (US devuelve Rubber Fill y una app de rellenos dérmicos) [V] |
| selvafill | Sin coincidencias. Apps "Selva" que no son juegos: Selva (Selva.kg), SELVA (The Members), Selva: Habit & Impulse Tracker, Selva (Selva Grupo), SelvaApp (vigilancia ambiental, 10K+), Selva Zeladoria (10K+) [V] | US: ReadyFill (negocios). BR, MX, CO y PE: 0 [V] |
| selva pour | Sin coincidencias. Juegos de jungla: Jungle Adventures, Aventura na Selva, Super Dino - Aventura na Selva [V] | US: 0 relevantes. BR y MX: 0 [V] |
| selva splash | Sin coincidencias. Splash - Imposter Game, Lumina Splash, Water Splash - Cool Match 3 [V] | US: juegos "Splash" genéricos. BR: 0 [V] |
| selva sort | Sin coincidencias. **Jungle Bird Sort** (Apollo Mobile Games, 1M+), Mystic Sort: Water Sort Puzzle, Classificação de Água, Water Sort - Puzzle Game, Waterscapes - Color Sort [V] | Bloqueada (403) [NV] |
| fill da selva | Sin coincidencias [V] | US: 0 relevantes [V]. BR, MX, CO y PE: bloqueada [NV] |
| selvagua | Sin coincidencias. Solo apps "Selva" que no son juegos [V] | Bloqueada [NV] |
| selvaqua | Un solo resultado: AquaPlants [V] | Bloqueada [NV] |
| selva | Único juego con "Selva" en el título: "Selva das Palavras" (sopa de letras, 10+). El resto son juegos de jungla en inglés [V] | Bloqueada [NV] |

**Pendiente:** repetir la API de iTunes (`https://itunes.apple.com/search?term=<nombre>&entity=software&country=<us|br|mx|co|pe>`) para Selvagua, Selvaqua, Selva Sort y Selva antes de decidir. No se espera sorpresa, porque Play y la web están limpios [E].

---

## 4. Antecedentes con "SELVA" que importan

"SELVA" es una palabra **común y muy diluida**: 227 marcas en las clases 9, 28 y 41 en BR, PE, MX, US, EM y WO, y 14 en Colombia, la mayoría compuestas ("SELVA MÁGICA", "SELVA URBANA", "SELVA NEGRA"…) [V]. La dilución ayuda a que convivan marcas "SELVA + algo distintivo". **No ayuda cuando lo que se añade es descriptivo** (FILL, SORT, POUR, SPLASH): entonces la comparación se hace contra "SELVA" sola [E].

### 4.1 Críticos (SELVA idéntica en el mismo tipo de servicio)

| Marca | Oficina · nº | Estado | Clase y productos o servicios | Titular | Impacto |
|---|---|---|---|---|---|
| SELVA (mixta) | INPI BR · **940179083** | **Pendiente**: presentada el 25-07-2025, publicada el 19-08-2025 | **41**: entre otros, *"jogos on-line, não baixáveis, providos em um ambiente virtual"*, *"serviços de entretenimento"*, *"serviços de entretenimento providos em ambientes virtuais"*, *"jogo de fuga"*, *"organização de competições"* | Thiago Pires dos Santos Araujo (BR) | **Alto** para cualquier "SELVA + término descriptivo" en BR-41. Si se concede, el INPI puede negar "SELVA FILL" por reproducción con añadido (LPI art. 124 XIX) [V datos, E impacto] |
| SELVA (mixta) | IMPI MX · **3019615** (exp. 3495589) | **Registrada** el 09-04-2026; vence el 09-04-2036 | **41**: educación, formación, **servicios de entretenimiento**, actividades deportivas y culturales | Manuela Arango Arango | **Alto o medio** en MX-41: "entretenimiento" abarca juegos [V datos, E impacto] |

### 4.2 Relevantes (riesgo medio o bajo)

| Marca | Oficina · nº | Estado | Clase y productos o servicios | Impacto [E] |
|---|---|---|---|---|
| SELVA | INPI BR · 925767174 | Registrada el 23-05-2023 (vence en 2033) | **9**: software descargable para operaciones con criptomonedas y *blockchain*, tokens de seguridad · Epioneers Inc. (US) | Medio-bajo en BR-9 (software descargable, aunque de otro tipo) |
| SELVA | INPI BR · 939765586 | Pendiente (publicada el 22-07-2025) | 41: educación, formación, vídeos y publicaciones en línea · Roiper Brands Ltda | Medio-bajo |
| SELVA (mixta, águila) | INPI BR · 938720619 | Registrada (estado del 28-09-2026) | 41: cursos, educación, publicación de libros · Selva Brasil Consultoria e Treinamento | Bajo |
| SELVA (mixta) | INPI BR · 501675726 (Madrid 1675726) | Registrada el 12-11-2024 | 7, 9, 12, 20, 22, **28**: náutica, buceo, pesca, esquí acuático · Selva S.p.A. (IT) | Bajo (productos náuticos) |
| SELVA (mixta) | IMPI MX · 2831680 | Registrada el 07-03-2025 | 41: educación, edición de fotos y vídeo · Uriel López Castañeda | Medio-bajo |
| SELVA MÁGICA (varias) | IMPI MX · 564259, 1899531, 1915048, 2651773… · INPI BR · 936702630 | Registradas | 41: parque de diversiones (Guadalajara) y recreación | Bajo-medio |
| EL PARQUE DE LA SELVA ¡TODO UN REINO DE DIVERSIÓN! | IMPI MX · 1619259 (9), 1634111 (28), 1702508 (41) | Registradas | 9, 28 y 41 | Bajo |
| SELVA | USPTO · 99517661 (reg. 8351924) | Registrada en 2026 | 41: discotecas · El Gourmet de Jorge Juan (ES) | Bajo-medio en US-41 |
| SELVAR | USPTO · 99848654 | Aprobada para publicación | **9**: apps móviles descargables para leer publicaciones; 26 y 42 | Bajo-medio en US-9 |
| JUNGLE | USPTO · 79361060 · 88111480 | Registradas | 9, 28 y 41: juegos (casino; entretenimiento) | **Medio en US** por la doctrina de equivalentes extranjeros (SELVA = *jungle*) |
| SELVATIKA | SIC CO · reg. 547366 | Registrada (vigencia: 29-04-2026) | Multiclase, incluidas 9, 28 y 41 (centro comercial) | Bajo |
| La ley de la selva Codiscos | SIC CO · reg. 664808 | Registrada | 9 y 41 (música) | Bajo |
| SELVA WORLD, SELVA ARENA, Selva Soy, SELVA PROFUNDA, INNOVA SELVA… | INDECOPI PE · S00136311, S00181070, S00179516, S00144760… | Registradas | 41 | Bajo (SELVA diluida en PE) |
| SELVA SECURITY S.A.C | INDECOPI PE · P00210684 | Registrada | 9 | Bajo |

### 4.3 Para el descriptor del título "Water Sort Puzzle" (EE. UU.)

En la USPTO hay solicitudes **"WATER SORT PUZZLE"** (97179691, Shanghai Zhenglang, 2021; 98761281, IEC Global, 2024) y **"WATERSORT"** (99389687, Beijing Redsea, 2025) en la clase 9. Todas siguen **suspendidas** y ninguna está concedida; hay otras dos abandonadas [V]. Usar "Water Sort Puzzle" como descriptor después de la marca es práctica habitual en las tiendas, pero conviene vigilar estas solicitudes antes del lanzamiento en EE. UU. [E].

---

## 5. Idioma y connotaciones

| Término | Español (MX, CO, PE, AR) | Portugués de Brasil | Inglés (US) |
|---|---|---|---|
| **selva** | Palabra común: jungla o bosque tropical. PE: "costa, sierra y selva", con fuerte identidad regional. MX: Lacandona, Maya y el parque "Selva Mágica". Modismos: "la ley de la selva", "esto es una selva" (caos). AR: también nombre propio (Selva Alemán). Sin connotación negativa [E] | Se entiende, pero en la conversación se usa más "floresta", "mata" o "Amazônia". **"Selva!" es el saludo o grito de las unidades de selva del Ejército** (CIGS, Manaos), con tono militar o aventurero. Novela "Selva de Pedra". **El juego se buscaría por "Amazônia", "floresta", "água", "classificar água"**, no por "selva" [E] | Préstamo poco frecuente: *selva* = selva tropical, sobre todo amazónica [V-sec: dictionary.com]. Suena latino o exótico. Se lee "SEL-vuh" |
| **fill** | No es palabra española. Los jugadores lo entienden a medias; se pronuncia "fil" [E] | No es palabra portuguesa. Se lee "fiu" o "fil". En "Fill da Selva" suena a "filho da…" [E] | **Descriptivo**: "llenar", la acción del juego |
| **pour** | Se lee "pur" o se confunde con "por" [E] | Se confunde con "pôr" (poner) [E] | Descriptivo (verter) |
| **splash** | Anglicismo conocido [E] | Anglicismo conocido [E] | Común; evocativo |
| **sort** | Jerga de *gamer* ("ball sort"). Evoca "sorteo" [E] | "Sorte" = suerte → apuestas, lotería [E] | Descriptivo del género |
| **Selvagua** | Transparente: "agua de la selva". Llana, sin tilde [E] | "Selvágua" sería la grafía correcta; sin tilde se lee igual de forma natural. Evoca "selvagem" (salvaje), en positivo [E] | Palabra inventada, exótica y pronunciable [E] |
| **Selvaqua** | "Aqua" es latín, se entiende [E] | Igual [E] | "Aqua" se conoce como color o agua [E] |

No se detectan significados vulgares ni ofensivos en ninguno de los nombres, salvo el juego de palabras "Fill da Selva" ≈ "filho da…" en PT-BR [E].

---

## 6. Dominios (RDAP/WHOIS, 06-10-2026)

| Nombre | .com | .app | .co | .com.br |
|---|---|---|---|---|
| selvafill (Selva Fill / SelvaFill / Selvafill) | Libre | Libre | Libre | Libre |
| selvapour | Libre | Libre | Libre | Libre |
| selvasplash | Libre | Libre | Libre | Libre |
| selvasort | Libre | Libre | Libre | Libre |
| filldaselva | Libre | Libre | Libre | Libre |
| **selvagua** | **Libre** | **Libre** | **Libre** | **Libre** |
| selvaqua | Libre | Libre | Libre | Libre |
| Extra: playselvafill, selvafillgame, selvadrop | Libres los 4 | | | |

Fuentes:
- `.com`: `rdap.verisign.com` (404). Confirmado también con `rdap.org` para selvafill.com, selvagua.com y selvaqua.com.
- `.app`: `pubapi.registry.google/rdap` (404).
- `.com.br`: `rdap.registro.br` (404).
- `.co`: WHOIS `whois.registry.co` ("not found").
- Controles: google y globo devuelven 200 u "ocupado" en los cuatro TLD [V].

"Libre" significa que el registro no tiene el objeto. No garantiza el precio, porque algunos nombres pueden ser *premium*. **Un dominio no da derechos de marca.** `.com.br` exige CPF o CNPJ, así que hace falta un representante local [V-sec, ver informe f].

---

## 7. Títulos de tienda (máximo 30 caracteres; espacios y signos cuentan)

| Nombre | "<N>: Water Sort Puzzle" | "<N>: Ordenar Agua" | "<N>: Classificar Água" |
|---|---|---|---|
| Selva Fill | 29 ✔ | 24 ✔ | 28 ✔ |
| SelvaFill / Selvafill | 28 ✔ | 23 ✔ | 27 ✔ |
| Selva Pour | 29 ✔ | 24 ✔ | 28 ✔ |
| Selva Splash | **31 ✘** | 26 ✔ | 30 ✔ (justo) |
| Selva Sort | 29 ✔ (repite "Sort") | 24 ✔ | 28 ✔ |
| Fill da Selva | **32 ✘** | 27 ✔ | **31 ✘** |
| **Selvagua** | **27 ✔** | **22 ✔** | **26 ✔** |
| Selvaqua | 27 ✔ | 22 ✔ | 26 ✔ |

---

## 8. Registrabilidad: lectura del agente (no legal)

1. **"Selva Fill" es demasiado débil para ser una buena marca, aunque probablemente no es irregistrable.**
   - "Selva" describe el **tema o contenido** del juego (en la Decisión 486 andina, las características del producto incluyen su temática) y "fill" describe la **mecánica**.
   - En CO, PE y MX la combinación de dos términos débiles suele admitirse, pero con **protección estrecha**: no podría impedir otros "SELVA X" ni "X FILL" [E].
   - En **EE. UU.**, la doctrina de equivalentes extranjeros traduce "SELVA" por "JUNGLE": "JUNGLE FILL" para un juego de llenar en la jungla puede recibir una objeción de **descriptividad meramente descriptiva (§2(e)(1))** o una cita de las marcas JUNGLE de juegos [E].
   - El riesgo principal es **relativo**, no absoluto. Hay "SELVA" idéntica para juegos y entretenimiento en **BR-41** (pendiente) y **MX-41** (registrada). Con "FILL" descriptivo, el examinador compara básicamente SELVA contra SELVA [E].
2. **SelvaFill en una palabra** mejora un poco la unidad del signo, pero no resuelve el problema del prefijo [E].
3. **Selva Pour, Splash y Sort** tienen el mismo problema y otros añadidos: "sort" es el nombre del género, la opción más débil.
4. **"Selvagua" es la más fuerte.**
   - Es una **palabra inventada**, que los examinadores valoran como un todo.
   - Es **evocativa**: sugiere "agua de la selva" sin describir el producto (software de juego o servicio de juego en línea).
   - Las oficinas andinas y el IMPI suelen registrar marcas evocativas sin problema.
   - Riesgo residual: el INPI podría ver el prefijo "SELVA-" y relacionarlo con la solicitud 940179083. Como la palabra inventada da una impresión de conjunto distinta (SEL-VA-GUA frente a SEL-VA), el riesgo se estima **bajo-medio** [E].
5. **Recomendación de clases:** 9 (software de juego descargable; bienes virtuales) y 41 (suministro de juegos en línea no descargables; entretenimiento). La 28 solo si habrá *merchandising* [E].

---

## 9. Recomendación

1. **No adoptar "Selva Fill" como marca principal.** Cumple el filtro de tiendas y dominios, pero choca con "SELVA" en la clase 41 en Brasil y México, y su distintividad es baja. Para el objetivo del dueño ("sin problemas"), es **medio-alto**.
2. **Candidato principal: "SELVAGUA"**, denominativa, misma grafía en los tres idiomas y sin tilde (como se decidió con Aquazonia). Títulos:
   - EN: "Selvagua: Water Sort Puzzle"
   - ES: "Selvagua: Ordenar Agua"
   - PT-BR: "Selvagua: Classificar Água"
   - "selva", "Amazônia" y "floresta" quedan como **palabras clave descriptivas** de la ficha, no como marca.
3. **Hoy (dueño, ≈30-60 USD/año [E]):** registrar `selvagua.app` y `selvagua.com`, y si se quiere `selvagua.co`. `selvagua.com.br` se registra después, mediante el representante brasileño.
4. **Antes de fijar el `applicationId`:** pedir a un abogado de marcas una **búsqueda de anterioridad profesional de SELVAGUA** en CO, PE, MX, BR y US (clases 9 y 41), con búsqueda fonética, y un parecer específico en **Brasil** sobre el prefijo SELVA frente a la solicitud 940179083. Hasta entonces sigue vigente la regla del informe f: **ningún AAB sube a Play Console** con un ID de marca. Un ID neutral provisional evita problemas.
5. **Presentar SELVAGUA pronto** en el país del estudio, en las clases 9 y 41. La fecha de prioridad del Convenio de París (6 meses) permite extenderla a los otros mercados.
6. **Plan B:** "Selvaqua" (bajo-medio). Requiere su propia búsqueda profesional y está más cerca de la familia AQUA*.
7. **Si el dueño insiste en "Selva Fill":**
   - Usarla como **"SELVAFILL" en una palabra** y acompañarla de un logotipo distintivo.
   - Pedir un parecer en BR y MX **antes** de invertir. Prever una posible negativa en la clase 41 en esos países y la necesidad de un acuerdo de convivencia con los titulares de SELVA.
   - No usar "Selva Sort" ni "Fill da Selva" en ningún caso.

---

## 10. Consultas manuales pendientes (5-10 minutos, dueño o abogado)

| Base | Enlace | Qué comprobar |
|---|---|---|
| TMview (todas las oficinas) | https://www.tmdn.org/tmview/ | `selvagua`, `selvaqua` y `selva fill`; confirmar que siguen sin antecedentes. Bloqueó el acceso automático |
| INPI Brasil (pePI) | https://busca.inpi.gov.br/pePI/ → Marca → nº `940179083` y marca `selvagua` (radical) | Estado de 940179083 (¿concedida, archivada, con oposición?) y si hay "SELVAGUA" nueva |
| INDECOPI (Quipu) | https://enlinea.indecopi.gob.pe/quipu | Búsqueda fonética de `SELVAGUA`, clases 9 y 41 |
| IMPI (MARCia) | https://marcia.impi.gob.mx/marcas/search/quick | `SELVAGUA` (Similar Plus); ficha de 3019615 "SELVA" |
| SIC (SIPI) | https://sipi.sic.gov.co/sipi/Extra/Default.aspx → Signos Distintivos → Búsqueda avanzada | `*selvagua*` sin clase (hoy: sin resultados) |
| WIPO GBD | https://branddb.wipo.int/en/similarname | `selvagua`, fonética y difusa, clases 9 y 41 |
| USPTO | https://tmsearch.uspto.gov/search/search-information | `selvagua`; estado de 97179691 y 98761281 ("WATER SORT PUZZLE") |
| App Store | `https://itunes.apple.com/search?term=selvagua&entity=software&country=br` (cambiar el país: us, mx, co, pe) | Que no haya apps con el nombre (bloqueado en esta sesión) |

---

## Fuentes

**Marcas (consultadas el 06-10-2026)**
- WIPO Global Brand Database, búsqueda por nombre: https://branddb.wipo.int/en/similarname. Fichas citadas:
  - BR 940179083: https://branddb.wipo.int/en/similarname/brand/BR500000940179083
  - BR 939765586: https://branddb.wipo.int/en/similarname/brand/BR500000939765586
  - BR 938720619: https://branddb.wipo.int/en/similarname/brand/BR500000938720619
  - BR 925767174: https://branddb.wipo.int/en/similarname/brand/BR500000925767174
  - BR 501675726: https://branddb.wipo.int/en/similarname/brand/BR500000501675726
  - MX 3019615: https://branddb.wipo.int/en/similarname/brand/MX502025003495589
  - MX 2831680: https://branddb.wipo.int/en/similarname/brand/MX502024003253476
  - MX 3076587 SELVARA: https://branddb.wipo.int/en/similarname/brand/MX502026003573191
- USPTO Trademark Search: https://tmsearch.uspto.gov/ (seriales 99517661, 99848654, 99336930, 79361060, 88111480, 97179691, 98761281, 99389687, 97856827)
- IMPI MARCia: https://marcia.impi.gob.mx/marcas/search/quick (ficha RM202603638320 "SELVA", clase 1)
- SIC SIPI: https://sipi.sic.gov.co/sipi/Extra/Default.aspx
- TMview (bloqueado): https://www.tmdn.org/tmview/ · INPI pePI (sin respuesta): https://busca.inpi.gov.br/pePI/ · INDECOPI Quipu (error): https://enlinea.indecopi.gob.pe/quipu
- Ley brasileña 9.279/96, art. 124 XIX: https://www.planalto.gov.br/ccivil_03/leis/l9279.htm

**Tiendas**
- Google Play, búsqueda: https://play.google.com/store/search?q=selva%20fill&c=apps (y el resto de los nombres; `hl`/`gl` para US, BR, MX y CO)
- Jungle Bird Sort: https://play.google.com/store/apps/details?id=com.apollo.bird.sort · Garden Fill: https://play.google.com/store/apps/details?id=com.garden.fill.dg · Selva das Palavras: https://play.google.com/store/apps/details?id=com.anonymous.cacapalavras · SelvaApp: https://play.google.com/store/apps/details?id=com.gugarauj07.selvaapp
- API de iTunes: https://itunes.apple.com/search?term=selva%20fill&entity=software&limit=25&country=us

**Dominios**
- IANA RDAP bootstrap: https://data.iana.org/rdap/dns.json
- https://rdap.verisign.com/com/v1/domain/selvagua.com · https://pubapi.registry.google/rdap/domain/selvagua.app · https://rdap.registro.br/domain/selvagua.com.br · WHOIS `whois.registry.co` (puerto 43)

**Idioma**
- "selva" en inglés: https://www.dictionary.com/browse/selva

**Documentos internos**
- `docs/setup/research/f-marca-aquazonia.md` (método, costes, regla de no subir un AAB) · `docs/legal/marca.md` · `docs/gdd/00-glosario.md` §1 (títulos de tienda)

**Decisión posterior (2026-10-06):** el dueño eligió **Tintalú** como nombre comercial; Selvagua queda como tercer plan B, tras Cuiazú y Botonda, si el dictamen sobre Tintalú falla. Ver `docs/legal/nombres-candidatos.md`, sección "Decisión del dueño (2026-10-06)", y `docs/legal/marca.md`.
