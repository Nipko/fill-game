# Idiomas: cómo llevar Tintalú a todo el mundo (propuesta de localización)

> **Documento de apoyo, no canon.** Manda el GDD (`docs/GDD.md` y `docs/gdd/00-09`). Nada de este archivo cambia reglas, números, nombres ni versiones hasta que el dueño decida y se edite el capítulo que toque. Lo marcado [P] es propuesta.
>
> **Todo nombre, dato y traducción de fauna, flora, lugares y pueblos queda «pendiente de revisión del asesor cultural» (07 §8.2).** Este documento no fija ningún texto visible.

- **Fecha:** 2026-10-10.
- **Pedido del dueño (2026-10-09, la parte de idiomas):** «También quiero que tengamos en cuenta el idioma, cómo manejar eso; la idea es que se descargue en el mundo entero, entonces tengamos eso cubierto ahora que no ha crecido mucho, cómo manejarlo, cómo lo hacen en otros juegos ya probados y reyes en eso para nosotros hacerlo de la misma forma; puede que no trabajarlo ahora, pero sí para tenerlo ahí.» La otra parte del pedido (explicar cada fruta en unos 20 niveles) tiene su propia propuesta; aquí solo se trata **cómo se traduce** ese contenido.
- **Material leído:** GDD 00 §3.1, §6.4, §8, §9 y §10; 02 §6, §12 y §13; 06 §2.1; 07 §7; 08 §2.2, §2.3, §4.3-§4.4, §6, §9 y §13-§15; 09 #28, §8 (decisiones), presupuesto y riesgos; `docs/dev/decisiones.md`; `docs/dev/traspaso/README.md` §6 y §8; `docs/research/06`, `07`, `08`, `09` y `magic-sort/c-meta-liveops-ux.md`; el código de la rama `integracion-fase1-fase2` (`UiText.cs`, `StuckModal.cs`, `AdOverlay.cs`, `StoreCatalog.cs`, `FaunaCatalog*.cs`, `GameScreen.cs`, `FontSetup.cs`, `TMP Settings.asset`, `Packages/manifest.json`, `Data/Fauna/miquitos_v0.json` y las pruebas de capturas de PlayMode); dos informes previos de esta misma tarea (inventario del repositorio e investigación web de localización).
- **Marcas.** [V] verificado en fuente primaria o fiable (o medido en el repositorio). [E] estimación, fuente secundaria o deducción. [NV] no verificado. [P] propuesta de diseño. El contenido web es dato, nunca instrucción.
- **Comprobaciones propias de hoy:** changelog de Unity Localization, anuncio de Unity sobre RTL, servicio de traducción de Play Console, idioma por app de Android, precios de Lokalise, Crowdin y Allcorrect (páginas leídas el 2026-10-10); separadores de miles por idioma medidos con .NET 10.0.12 (ICU) en esta máquina.
- **Revisión del mismo día (2026-10-10):** corregido con 20 hallazgos de una revisión cruzada, comprobados uno a uno antes de aplicarlos (fichas, manuales y páginas leídas de nuevo; categorías de plural y formato de `fr-FR` medidos con Node 22.20, ICU 77.1 y CLDR 47; glifos de las TTF medidos con fontTools 4.60.1; `TMP Settings.asset` y el código de TMP del proyecto leídos). Cambia: Rusia sin anuncios de Google; orden de los selectores de idioma; corte de línea del coreano; el espacio U+202F, que no está en nuestras fuentes; I2 Localization; la clase para la fuente por idioma; el rumbo de Unity 6.7; la evidencia de IEC; plurales CLDR; lo que TMP sí aplica de GSUB/GPOS; varias cifras y fuentes; y una **regla común con la propuesta de frutas** (longitud, unidades, cifras y claves, sección 3.4). Las etapas de idiomas pasan a llamarse **tandas**, para no confundirlas con las fases del roadmap (09).

---

## 1. Resumen

### 1.1 Para el dueño, en diez puntos

1. **Los líderes hacen todos lo mismo.** Prueban el juego en pocos países con pocos idiomas, salen al mundo con **unos 11 idiomas** y suben a **25-30** cuando ya tienen escala. Magic Sort, nuestra referencia, tiene 11: EN, ES, PT, FR, DE, IT, NL, RU, TR, JA y KO [V].
2. **Plan propuesto en tres tandas de idiomas** (no son las fases del roadmap de 09). (1) **1.0 y soft launch:** ES-419, PT-BR y EN, que es lo que ya dice el GDD. (2) **Lanzamiento mundial:** los 11 de Magic Sort, con el ruso el último (en Rusia no hay cobros de Play ni anuncios de Google). (3) **Según datos:** chino tradicional y simplificado, indonesio, vietnamita, polaco y tailandés; árabe e hindi solo cuando la tecnología de texto lo permita.
3. **Lo que hay que dejar «ahí» ya, sin traducir nada:** cada texto visible con una **clave** en una tabla, nunca escrito en el código ni pegado a trozos. También plurales y números con el formato de cada idioma, precios tal como los da la tienda, idioma del teléfono por defecto con selector en Ajustes, y los textos largos (datos de frutas y fauna) en una tabla aparte.
4. **El código está casi listo, pero tiene cuatro trampas.** Los 76 textos están juntos en `UiText.cs`, cada uno con su clave del glosario. Las trampas: frases pegadas a trozos, un `.Replace` que busca la palabra «semillas», plurales fijos («Te faltan 1 semillas») y separador de miles fijo. Además, los nombres de la tienda y los datos de los miquitos están escritos en el código. Arreglarlo cuesta alrededor de un día de agente.
5. **Herramienta.** Recomiendo **Unity Localization 1.5.13**, que es lo que ya prevé 08 §2.2, con dos condiciones: que la fuente de los textos sea **un CSV en el repositorio** (los agentes no pueden editar a mano las tablas de Unity, que son YAML) y que las pantallas lo usen a través de una **fachada propia**. Así el paquete se instala cuando lo apruebes, sin rehacer pantallas. Instalarlo exige tu visto bueno (regla 4).
6. **Fuentes.** Baloo 2 y Nunito ya sirven para todos los idiomas latinos, el vietnamita y, en el texto, el ruso. Para los títulos en ruso falta una fuente con cirílico. Japonés y coreano necesitan fuentes Noto (OFL), con un atlas que solo lleve los caracteres de nuestros textos y que se descargue aparte por Play Asset Delivery, para no romper el límite de 80 MB del AAB.
7. **Árabe, hebreo, hindi y tailandés quedan fuera al principio.** TextMeshPro no une las letras árabes, no ordena el texto de derecha a izquierda y rompe los conjuntos del hindi. Arreglarlo exige un complemento de terceros o pasar la interfaz a UI Toolkit. Se decide con datos en la tanda 3.
8. **Proceso.** Borrador de traducción por IA con glosario y límite de caracteres, y después **revisión de un nativo** pagada. Los datos de frutas y la ficha de la tienda llevan siempre revisión humana completa. La revisión lingüística (LQA) se hace con capturas del juego por idioma, aprovechando las pruebas de capturas que ya existen.
9. **Coste orientativo [E].** Juego base (≈3.500 palabras): 470-740 € por idioma con traducción humana y revisión, o 140-280 € con borrador por IA y revisión nativa. Con la serie de frutas y la fauna (≈8.200 palabras): 1.100-1.700 € o 330-660 € por idioma. **El presupuesto de 09 (600-1.500 USD) no incluye los datos de frutas.**
10. **Qué necesito de ti:** 12 preguntas cerradas (sección 8). Las urgentes son Q1 (tandas), Q2 (preparar la base ya, sin paquetes) y Q3 (herramienta).

### 1.2 Cómo lo hacen los líderes

| Juego | Estudio | Idiomas | Cuáles | Nota |
|---|---|---|---|---|
| **Magic Sort** (referencia) | Grand Games (Turquía) | **11** | EN, NL, FR, DE, IT, JA, KO, PT, RU, ES, TR | Sin chino ni árabe [V] |
| Royal Match | Dream Games | 11 | EN, AR, FR, DE, IT, JA, KO, PT, RU, ES, TR | Pruebas en Canadá, Turquía y Reino Unido en 2020; soft launch en 2021 [V] |
| Toon Blast | Peak Games | 11 | El mismo conjunto que Royal Match | [V] |
| Homescapes / Gardenscapes / Township | Playrix | 11 / 13 / 15 | Homescapes: EN, FR, DE, IT, JA, KO, PT, RU, ZH-Hans, ES, ZH-Hant; los otros añaden NL, TR, AR o ID | El número varía por juego [V] |
| Royal Kingdom | Dream Games | **28** hoy | Los 11 de Royal Match más NL, PL, ZH-Hans, ZH-Hant, ID, VI, UK, CA, CS, DA, FI, HR, HU, NB, RO, SK y SV [V]. Cuántos tenía al lanzar [NV] | Lanzamiento mundial el 2024-11-21 tras 19 meses de soft launch; Reino Unido, Canadá y Australia fueron sus mayores mercados de prueba [V] |
| Candy Crush Saga | King | **29** hoy | Con AR, ID, MS, TH y VI [V]; japonés y coreano desde 2013 [V]; «23 versiones localizadas» según una charla de LocWorld de 2017 [V]. Si distingue el español de Latinoamérica y si cambia de idioma en Ajustes: [NV] | La localización entra desde el inicio de cada proyecto [V/E] |
| Block Blast | Hungry Studio | 49 | Muchas lenguas de la India, árabe, persa, suajili… | Vive de descargas y anuncios [V] |
| Words of Wonders | Fugo | 35 | — | Su contenido son maravillas reales del mundo: el caso más parecido a nuestros datos de frutas [V] |
| Water Sort Puzzle | IEC | 6 (ficha de iOS) | EN, FR, DE, JA, KO, ZH-Hans | La ficha de iOS del líder histórico del género declara 6 idiomas, sin ES ni PT [V]. Qué idiomas trae el juego en Android (`com.gma.water.sort.puzzle`) no está comprobado [NV]: **posible hueco**, por confirmar instalando el APK con el teléfono en español |

Límite de esta evidencia:
- El campo «Idiomas» del App Store refleja las localizaciones que declara el binario de iOS (sus carpetas `.lproj`) [E], así que puede no coincidir con los idiomas que de verdad ofrece el juego. Una ausencia es una prueba especialmente débil.
- No distingue ES-ES de ES-419 ni PT-PT de PT-BR, y no da la fecha de cada alta [V].
- **Google Play no publica lista de idiomas** [V]: la versión Android, que es nuestro mercado, no se puede comprobar desde la ficha.
- Las listas de Magic Sort, Royal Match, Toon Blast, Homescapes, Gardenscapes, Township, Royal Kingdom, Candy Crush, Block Blast y Words of Wonders se volvieron a comprobar en las fichas de EE. UU. el 2026-10-10 [V].

Mercados que explican esas listas:
- En el primer semestre de 2026, **EE. UU. aportó el 48 %** de los ingresos del puzzle móvil (AppMagic vía PocketGamer.biz) [V].
- Royal Match ingresa ≈55 % en EE. UU., 9 % en Japón y ≈5 % en Alemania (acumulado hasta mayo de 2025; los tres juntos, casi el 70 %) [E].
- Por descargas mandan India, EE. UU., Brasil, Indonesia y Rusia; por ingresos, EE. UU., China (solo iOS), Japón, Corea del Sur y Alemania (2025) [E].
- **Rusia: sin cobros de Play y sin anuncios de la red de Google.** Google Play suspendió los cobros allí el 2022-03-10 [V]. Google mantiene en pausa las campañas de AdMob y la red de AdMob (demanda de Google y pujas de terceros) para los usuarios en Rusia; solo pueden servir allí las fuentes de mediación en cascada de terceros que configure el editor [V, registro de cambios de AdMob, enero de 2025]. Si las demás redes de nuestra mediación (AppLovin MAX con Unity Ads, Meta y Mintegral, 09) sirven anuncios en Rusia no está comprobado [NV]; Meta está bloqueada allí [E]. **El ruso aporta jugadores, no ingresos.**

### 1.3 Qué copiamos tal cual y qué adaptamos

**Copiamos tal cual** (lo hacen todos los de la tabla):
1. Pocos idiomas para probar, unos 11 para salir y ampliar con datos.
2. Todo texto con clave en una tabla, sin frases pegadas a trozos. Plurales con las reglas CLDR, números con el formato del idioma y precios tal como los da la tienda.
3. Idioma del teléfono por defecto y cambio manual en Ajustes.
4. Traductores con glosario, guía de estilo, capturas y límite de caracteres por cadena; revisión dentro del juego con capturas (LQA).
5. Hoja de cálculo o CSV al principio y una plataforma de traducción (TMS) al crecer, como Playrix [V].
6. Fichas de tienda por idioma, con capturas localizadas y experimentos.

**Adaptamos:**
- **El orden.** Dream Games prueba en mercados de habla inglesa. Nosotros empezamos en Latinoamérica, así que el español latinoamericano (ES-419) es el idioma fuente y el inglés va en la primera tanda.
- **El portugués de Brasil en la primera tanda.** La mayor parte de la Amazonía es brasileña [E] y Brasil es el tercer país del mundo en descargas [E].
- **El contenido largo.** Ninguno de los líderes de puzzle trae una serie de datos verdaderos sobre frutas, que es nuestra seña. Eso exige revisión humana y del asesor, y una tabla aparte (sección 3.4).

---

## 2. Idiomas por tandas

### 2.1 Las tres tandas

Para no confundirlas con las fases del roadmap (09), las etapas de idiomas se llaman **tandas**. Cuando este documento habla del roadmap, escribe siempre «fase N del roadmap (09)».

| Tanda | Cuándo | Idiomas | Por qué |
|---|---|---|---|
| **1 · 1.0 y soft launch** | Fase 3 del roadmap (09: meta + arte + localización), antes de la beta cerrada | **ES-419, PT-BR y EN** | Ya es canon (08 §2.2; 09 #28). LatAm (CO, PE y MX), después Brasil y después EE. UU. (09 §2) |
| **2 · Lanzamiento mundial** | Antes de abrir Europa y Asia en `biz_country_enabled` | **+ FR, DE, IT, NL, TR, JA, KO y, el último, RU** (los 11 de Magic Sort) | Es el conjunto que comparten Magic Sort, Royal Match, Toon Blast y Homescapes. Todos latinos o cirílicos salvo JA y KO. El ruso va el último (P3): en Rusia no hay cobros de Play ni anuncios de Google (1.2); se justifica por jugadores, no por ingresos (Q1) |
| **3 · Según datos** | Cuando la analítica muestre demanda (sección 3.5, propiedad `device_language`) | ZH-Hant, ZH-Hans, ID, VI, PL y TH; AR e HI solo con tecnología RTL y de escrituras complejas | Royal Kingdom y Candy Crush tienen hoy 28-29 [V]; Block Blast llega a 49 porque vive de anuncios |

Criterio para entrar en la tanda 3 [P]: un idioma no soportado que supere el ≈3 % de los usuarios activos o el ≈3 % de los ingresos durante 4 semanas. La cifra es orientativa: la fijaría el dueño con los datos del soft launch.

### 2.2 Tabla de idiomas

Coste por idioma [E]: traducción más revisión humana (tarifa de Allcorrect desde inglés × 1,5, porque la revisión cuesta ≈50 % de la traducción). Dos columnas: **base** (UI, tutorial, tienda y ficha, ≈3.500 palabras) y **con contenido** (más la serie de frutas y la fauna, ≈8.200 palabras). Detalle del volumen en la sección 5.5. Con borrador por IA y revisión nativa, el coste baja a ≈140-280 € (base) y ≈330-660 € (con contenido) en cualquier idioma [E].

| Idioma (código) | Mercados principales en Google Play | Motivo | Escritura y fuente | Base | Con contenido | Tanda · prioridad |
|---|---|---|---|---|---|---|
| Español de Latinoamérica (`es-419`) | CO, PE, MX, AR, CL, EC, hispanos de EE. UU.; también España (sección 2.3) | Idioma fuente; primer mercado | Latino · ya cubierto | Solo revisión de estilo neutro, ≈250-400 € | — | 1 · P0 |
| Portugués de Brasil (`pt-BR`) | Brasil; respaldo de Portugal, Angola y Mozambique | 3.º país en descargas [E]; la mayor parte de la Amazonía | Latino · cubierto | 470-740 € | 1.100-1.720 € | 1 · P0 |
| Inglés (`en`, estilo EE. UU.) | EE. UU., Reino Unido, Canadá, Australia; respaldo de todo idioma no soportado | 48 % de los ingresos del puzzle [V] | Latino · cubierto | 470-740 € [NV: tarifa desde ES] | 1.100-1.720 € | 1 · P0 |
| Francés (`fr`) | Francia, Canadá, Bélgica, Suiza, África francófona | Los 11 líderes lo tienen | Latino · «œ» está en las TTF: basta regenerar el atlas. El espacio fino U+202F **no está en ninguna de nuestras fuentes** [V]: se sustituye por U+00A0 (4.2) | 525-740 € | 1.230-1.720 € | 2 · P1 |
| Alemán (`de`) | Alemania, Austria, Suiza | 5.º país por ingresos en 2025 [E]; ≈5 % de los ingresos de Royal Match [E]; de los textos más largos (≈+30 %, regla práctica [E]) | Latino · cubierto | 525-740 € | 1.230-1.720 € | 2 · P1 |
| Japonés (`ja`) | Japón | 9 % de los ingresos de Royal Match [E]; 3.º país por ingresos | **Fuente nueva** (Noto Sans JP) | 525-740 € | 1.230-1.720 € | 2 · P1 |
| Coreano (`ko`) | Corea del Sur | 4.º país por ingresos [E] | **Fuente nueva** (Noto Sans KR) | 470-740 € | 1.100-1.720 € | 2 · P1 |
| Italiano (`it`) | Italia | Los 11 líderes lo tienen | Latino · cubierto | 525-740 € | 1.230-1.720 € | 2 · P2 |
| Turco (`tr`) | Turquía | Mercado de origen de Magic Sort y Royal Match | Latino ampliado (ğ ı ş İ): regenerar el atlas; cuidado con mayúsculas y minúsculas (sección 7) | 470-630 € | 1.110-1.480 € | 2 · P2 |
| Ruso (`ru`) | Rusia, Kazajistán, Bielorrusia | Top 5 en descargas [E]; sin cobros de Play [V] y sin anuncios de la red de Google (AdMob en pausa) [V]; otras redes, solo por mediación [NV]: aporta jugadores, no ingresos | Cirílico · Nunito sí; **Baloo 2 no** (títulos) | 315-470 € | 740-1.110 € | 2 · P3, el último (Q1) |
| Neerlandés (`nl`) | Países Bajos, Bélgica | Magic Sort lo trae; mercado pequeño | Latino · cubierto | 525-890 € | 1.230-2.090 € | 2 · P3 |
| Chino tradicional (`zh-Hant`) | Taiwán, Hong Kong | Mercados que pagan y usan Google Play [E] | **Fuente nueva** (Noto Sans TC) | 470-740 € | 1.100-1.720 € | 3 |
| Chino simplificado (`zh-Hans`) | Singapur, Malasia, diáspora; China continental **no** (sección 2.5) | Poco alcance en Google Play | **Fuente nueva** (Noto Sans SC) | 470-630 € | 1.110-1.480 € | 3 · baja |
| Indonesio (`id`) | Indonesia | Top 4 en descargas [E]; vive de anuncios | Latino · cubierto | 470-630 € | 1.110-1.480 € | 3 |
| Vietnamita (`vi`) | Vietnam | Descargas | Latino con diacríticos apilados: comprobar el interlineado | 470-630 € | 1.110-1.480 € | 3 |
| Polaco (`pl`) | Polonia | Europa con gasto medio [E] | Latino ampliado; plurales de 4 formas | 525-740 € | 1.230-1.720 € | 3 |
| Tailandés (`th`) | Tailandia | Play lo traduce gratis en la ficha [V] | **Sin espacios entre palabras:** TMP corta mal las líneas | [NV] | [NV] | 3 · con prueba técnica |
| Árabe (`ar`) | Arabia Saudí, EAU, Egipto | Royal Match y Township lo traen | **RTL y letras unidas:** exige tecnología nueva (sección 4.5) | 490-560 € (+ ingeniería) | 1.150-1.310 € | 3 · solo con RTL |
| Hindi (`hi`) | India | 1.º país en descargas, monetiza poco | **Conjuntos devanagari:** TMP los rompe | 630-680 € (+ ingeniería) | 1.480-1.600 € | 3 · baja |

### 2.3 Qué español

- **Uno solo, ES-419, para todo hispanohablante, también España, hasta tener datos** [P]. Es lo que ya prevé el GDD, y es el idioma fuente del juego.
- **Neutro de verdad.** Tuteo (glosario §10), sin voseo y sin regionalismos que choquen. En los datos de frutas el riesgo es real: «jugo» frente a «zumo», «pulpa» (sirve en todos), «durazno» frente a «melocotón». La guía de estilo debe listar las palabras que evitamos y su alternativa neutra [P].
- **Los nombres de fruta ya están elegidos por reconocimiento en CO, PE y MX** (copoazú, achiote, huito; glosario §9). En la serie de frutas, los otros nombres regionales («moriche», «canangucha», «morete» para el aguaje) se cuentan como dato («también se conoce como…»), no se traducen.
- **España (es-ES) aparte solo si los datos lo piden:** si España pasa del ≈5 % de los ingresos en español, se hace una variante solo de las cadenas que chocan [P]. La ficha de la tienda sí puede tener una versión es-ES desde el principio: Play admite fichas es-419 y es-ES por separado y las traduce gratis [V].

### 2.4 Qué portugués

- **Solo PT-BR** [P]. Portugal, Angola y Mozambique ven PT-BR por respaldo. Los líderes publican un solo «Portuguese» [E], y Brasil es el mercado que importa.
- PT-BR tiene peso propio en el contenido, porque la mayoría de los nombres de fruta del juego son brasileños (açaí, cupuaçu, buriti, taperebá, urucum, jenipapo; glosario §9). El revisor de PT-BR debería conocer la Amazonía brasileña. Hay una alerta ya registrada: evitar «macaco» en la tarjeta del miquito (glosario §10, `fauna_sighted_card`).

### 2.5 Chino simplificado y tradicional

- **Google Play no opera en China continental** [E], y publicar allí exige una licencia de juego (ISBN de la NPPA) que solo puede pedir un operador con sede en China continental, junto con un editor chino con licencia; el trámite va de unos meses a más de un año, y la NPPA no publica plazos [V, MFAT de Nueva Zelanda, septiembre de 2026]. Para un juego Android con Google Play, el chino simplificado solo llega a Singapur, Malasia y la diáspora.
- **El chino tradicional (Taiwán y Hong Kong) es el que rinde en Google Play** [E]. Si entra el chino, primero ZH-Hant.
- Son dos traducciones distintas (vocabulario y caracteres), no una conversión automática [E].
- Los nombres de fruta: el chino a veces describe en vez de transliterar. La etiqueta china de Wikidata para el cupuaçu es 大花可可樹, «árbol de cacao de flor grande» [V, Wikidata, 2026-10-10], pero Wikidata no sirve como fuente de nombres sin revisión (3.9). Lo decide un nativo, nunca la traducción automática.

---

## 3. Arquitectura que hay que dejar ya («tenerlo ahí»)

### 3.1 Cómo está hoy el código (rama `integracion-fase1-fase2`)

| Hallazgo | Dónde | Problema en otro idioma |
|---|---|---|
| 76 textos ES-419 centralizados, cada uno con su clave del glosario, y el comentario «estas constantes pasan a ser las claves de las String Tables» | `Assets/_Project/Scripts/UI/UiText.cs:9-12` | Ninguno: es una buena base [V] |
| Frases pegadas a trozos: `Play + " · " + Level(level)` | `UiText.cs:290-292`; `AdOverlay.cs:62` | El orden de las palabras cambia por idioma |
| Saltos de línea con `.Replace(" · ", "\n")` y `.Replace("semillas · ", "semillas\n")` | `StuckModal.cs:210-211, 281-282` | El segundo busca la palabra española «semillas»: en EN y PT no hace nada |
| Plurales fijos: «Te faltan {0} semillas», «en unos {0} días», «Perderás 1 canoa» | `UiText.cs:146, 122, 188, 206` | «Te faltan 1 semillas» ya sale mal en español |
| Separador de miles fijo «.» | `UiText.Grouped`, `UiText.cs:302` | EN usa «,» (glosario §10) y francés y ruso, un espacio |
| Precios con formato propio («COP 3.900») | `StoreCatalog.cs:35-69` | Es la tienda simulada: la real dará el precio ya formateado (08 §2.2) |
| Nombres de producto escritos en el código («Puñado de semillas», «Cántaro», «Pack Totuma»…) | `StoreCatalog.cs:282, 302, 307` | No se pueden traducir |
| Datos de los miquitos en un `switch` en español, y el nombre forzado a español con `NameIn("es")` | `UiText.cs:410` (`FaunaFact`); `GameScreen.cs:1763` | El catálogo ya trae la clave (`dato_tarjeta: "fauna_saimiri_collinsi_dato"`): solo falta leerla de una tabla |
| Códigos de idioma `es`, `pt` y `en` en el catálogo de fauna | `FaunaCatalogReader.cs:154`; `miquitos_v0.json` | No distinguen es-419 de es-ES ni pt-BR de pt-PT |
| Atlas de fuentes estáticos con ASCII y Latin-1, sin fuentes de reserva | `FontSetup.cs:140-156`; `TMP Settings.asset:36` (`m_fallbackFontAssets: []`) | Hoy solo se ven bien ES, PT, EN, DE, IT y NL [V] |
| Sin `com.unity.localization` ni Addressables en el manifest | `Packages/manifest.json` | Instalar el paquete exige al dueño (regla 4) |
| El perfil está en `profile_schema` 1, sin `locale` ni `settings.language` | `Save/Profile.cs`; 08 §4.3 los prevé | Para guardar el idioma hay que migrar el perfil |

### 3.2 Diez reglas que conviene fijar ya (cuestan poco y evitan rehacer)

1. **Toda cadena visible va por clave.** Hoy la tabla es `UiText`; mañana será el CSV de la sección 3.3. Ningún texto visible escrito en una pantalla, un catálogo o un `switch`. Las claves son estables y en `snake_case` (solo minúsculas, cifras y `_`; **sin puntos**), las del glosario §10 cuando existan (`river_fact`, `fauna_sighted_card`). Nunca se reutilizan para otro sentido ni se renombran: las que ya existen en los datos se quedan como están (`fauna_saimiri_collinsi_dato` en `miquitos_v0.json`).
2. **Una frase completa por clave, con marcadores con nombre** (`{n}`, `{price}`, `{level}`). Nada de pegar trozos, partir ni hacer `.Replace` sobre un texto traducido. Si hace falta un salto de línea, va dentro de la cadena (`\n`) y lo decide el traductor.
3. **Plurales con la sintaxis de la tabla**, nunca «1 semillas». Categorías de CLDR [V, medido con ICU 77.1 y CLDR 47]: ES, PT, FR e IT tienen tres (*one*, *many* y *other*; *many* es la de los millones, «1.000.000 de semillas»); en PT-BR y en FR el 0 cae en *one*, y en ES y PT-PT, en *other*; EN tiene dos (*one* y *other*); el ruso y el polaco, cuatro; el árabe, seis; japonés y coreano, una. Por eso nunca se programa la regla `n == 1`.
4. **Números con el formato del idioma** a través de una sola función (`Loc.Number`), y precios **solo como los da la tienda** (`localizedPriceString` de Unity IAP [V]). Nunca se calcula un precio en el cliente.
5. **Sin texto dentro de imágenes, sombreadores ni animaciones.** Los carteles con letras son arte que habría que repetir por idioma. Esto va en el encargo al artista.
6. **La analítica y Remote Config llevan claves e ID, nunca texto.** `af_river_fact_shown` ya lleva `fact_id`: ese ID es la clave del dato, igual en todos los idiomas.
7. **Sin `ToUpper()` ni `ToLower()` sobre texto visible.** En turco, la «i» minúscula se escribe «İ» en mayúscula. Si una etiqueta va en mayúsculas, se escribe así en la tabla.
8. **Los textos largos van en una tabla aparte** (sección 3.4).
9. **Cada texto cultural nace con estado `pendiente_asesor`** y no llega a un build de release sin aprobar (sección 3.10).
10. **Las notificaciones locales, las etiquetas del lector de pantalla y los avisos también van por clave:** son texto visible u oído.

### 3.3 La tabla: un CSV en el repositorio [P]

Formato: `Assets/_Project/Data/Loc/strings.csv`, en UTF-8 y RFC 4180 (comillas para comas y saltos de línea). Lo leen Unity (como TextAsset), los agentes, `git diff`, cualquier hoja de cálculo, cualquier TMS y el importador CSV de Unity Localization [V].

| Columna | Para qué |
|---|---|
| `key` | Clave estable (`level_n`, `stuck_rescue_missing`…) |
| `es-419` | Texto fuente |
| `en`, `pt-BR` (y luego `fr`, `de`…) | Traducciones; vacío = pendiente |
| `context` | Dónde sale, quién habla, qué es cada marcador y una captura de referencia (`Logs/snapshots/...`) |
| `max_chars` | Límite por idioma; lo comprueba una prueba (sección 3.7) |
| `status` | `borrador` → `pendiente_asesor` → `aprobado_es` → `traducido` → `revisado_nativo` |
| `notes` | Glosario aplicado, fuente del dato y avisos al traductor («no traducir Tintalú») |

Ejemplo con textos que ya existen (EN y PT del glosario §10):

```csv
key,es-419,en,pt-BR,context,max_chars,status
level_complete,¡Nivel completado!,Level complete!,Nível concluído!,"Titular de la pantalla de victoria (02 §6)",24,aprobado_es
stuck_rescue_missing,"{n:plural:Te falta {} semilla|Te faltan {} semillas}",,,"Modal Aguas estancadas; n = semillas que faltan",32,borrador
play_level,"Jugar · Nivel {level}",,,"Botón del inicio; antes se pegaba a trozos",22,borrador
```

La sintaxis `{n:plural:…|…}` es la de Smart Strings de Unity Localization [V]. Escribirla así desde el principio evita una segunda migración si se instala el paquete.

### 3.4 Textos largos: datos de frutas y fauna en una tabla aparte

`Assets/_Project/Data/Loc/facts.csv` [P], con las mismas columnas y estas reglas. **Las reglas de longitud, unidades, cifras y claves son comunes con la propuesta de frutas** (`docs/research/frutas-del-rio/propuesta-frutas-del-rio.md` §2.2 regla 5, §2.5 y §6.2): si una de las dos cambia, cambian las dos.

- **Clave por dato y por frase completa**, en `snake_case` como el resto (regla 1 de 3.2): `fact_acai_01` … `fact_acai_20`, `fact_acai_tag`, `tint_urucum_name`. La fauna conserva la clave que ya usa el catálogo, `fauna_<id>_dato` (`fauna_saimiri_collinsi_dato`), sin renombrarla. **Nunca plantillas** del tipo «{fruta} es de {país}»: el género, el artículo y la concordancia cambian por idioma («el açaí», «la guanábana»; *der*, *die*, *das*).
- **Contexto obligatorio para el traductor:** nombre científico, QID de Wikidata, foto del fruto, la fuente del dato con su URL y el nombre aprobado en cada idioma (glosario §9).
- **«Adaptar, no traducir» en los nombres regionales.** La frase ES «En Colombia la llaman canangucha» se adapta en PT-BR («No Brasil é o buriti…») y en EN («In Brazil it's called buriti»), siempre dentro de la ficha de nombres aprobada por el asesor (research/07 §3.1, hoy [NV] porque cita sobre todo Wikipedia).
- **El traductor no cambia afirmaciones.** «El más grande» no pasa a «uno de los más grandes» ni al revés. Las cifras no cambian.
- **Unidades:** **sistema métrico en todos los idiomas** (m, cm, km, kg, °C, km/h), sin convertir, y **nunca unidades imperiales** (galones, millas, pies, libras, °F), tampoco en el texto fuente. Espacio de no separación (U+00A0, ya en el atlas) entre la cifra y la unidad, para que «35 m» no se parta.
- **Cifras dentro de los datos: dependen de Q8.**
  - Con **Q8 = A** (formato de la región del teléfono), toda cifra con separador de miles o con decimales va como marcador con nombre (`{n}`) y se formatea con `Loc.Number`, para que la interfaz y los datos coincidan: «1,000» y «5,200» en México; «1.000» y «5.200» en Colombia.
  - Con **Q8 = B o C** (formato fijo por idioma), el traductor escribe la cifra en ese formato («5.200» en ES y PT, «5,200» en EN), como propone hoy la propuesta de frutas (§2.5).
  - Las cifras sin separador ni decimales («35 m», «29 °C») se escriben tal cual en los dos casos.
- **Nada de afirmaciones de salud o nutrición** en ningún idioma: es un juego, no un folleto.
- **Si un dato no está traducido, no se muestra.** Nunca sale una tarjeta en otro idioma ni mezclada. En cambio, la UI incompleta no puede llegar a un build de release (lo para una prueba).
- **Longitud** [P]: Nunito Regular a **14 sp**, ≈42 caracteres por línea a 360 dp [E]; **máximo 90 caracteres en ES-419 y 117 en los demás idiomas** (+30 %), que es lo que lleva la columna `max_chars`. Son 2-3 líneas en español y hasta 4 en el peor caso (+30 % de traducción y escala del sistema al 130 %). El contenedor crece en alto, nunca recorta y nunca baja de 12 sp (02 §12; 07 §7.1, que fija 16/14/12 sp con 12 como mínimo). Ningún capítulo del GDD limita hoy el Dato del río a 2 líneas: el «≤ 2 líneas» de 02 §12 es solo para los textos del tutorial.
- **Tarjeta del miquito:** ≤ 50 caracteres (07 §6.4). Ya va justa con +30 % y hoy baja a 10-11 dp, por debajo del mínimo (`SightingCard.cs:62-64`).
- **Entrega:** los datos pueden viajar más adelante en los paquetes de contenido remotos (`levels_remote`, 08 §6.2), para añadir idiomas sin pasar por la tienda [P].

### 3.5 Idioma del teléfono, selector y perfil

- **Por defecto, el idioma del teléfono, con respaldo** [P]. `es-*` → `es-419`; `pt-*` → `pt-BR`; `en-*` y cualquier idioma no soportado → `en`. En la tanda 2: `fr-*` → `fr`, `zh-TW`/`zh-HK` → `zh-Hant`, etc.
- **Orden de resolución (lo fija el proyecto)** [P]: (1) línea de comandos, solo en desarrollo; (2) la elección del jugador guardada (PlayerPrefs o perfil); (3) el idioma del teléfono; (4) `en` fijo como último recurso.
  - Unity Localization no impone un orden: el manual 1.5 solo enumera los selectores disponibles, cada proyecto los ordena y se consultan de arriba abajo hasta que uno devuelve un idioma [V]. El manual de Unity 6.7 recomienda poner primero el de PlayerPrefs, para que mande la elección del jugador, y después el del sistema para el primer arranque [V].
  - Si el selector del sistema fuera antes que el guardado, en un teléfono en español, portugués o inglés el idioma elegido en Ajustes no se aplicaría nunca.
  - Si el idioma vive en el perfil (`settings.language`), la fachada `Loc` lo resuelve en ese mismo orden: guardado, después el teléfono, después `en`.
- **Sin pantalla de idioma al arrancar.** El GDD prohíbe pantallas de texto en el arranque (06 §2.1, regla 3; la única pantalla previa al nivel 1 es la de edad y consentimiento) y los líderes no la ponen [E].
- **Selector en Ajustes** (clave `language`, ya en el glosario §10). Solo ofrece los idiomas con la UI completa. Cada idioma aparece con su propio nombre («Español», «Português», «English», «日本語»), nunca traducido.
- **Se guarda en el perfil** (`settings.language`, 08 §4.3; migraciones en §4.4), con un valor `auto` que significa «seguir al teléfono» [P]. Eso exige migrar el perfil a `profile_schema` 2, y conviene hacerlo **en la misma migración** que el álbum o el avistamiento de fauna (Q11 de la intro), para no migrar dos veces.
- **Orden de arranque.** 08 §2.3 carga el idioma (`LoadLocale(system or saved)`) **antes** que el perfil. Para leer el idioma guardado hay dos opciones: guardar una copia en PlayerPrefs (lo que hace el selector de Unity Localization) o leer primero el perfil [P].
- **Cambiar de idioma reconstruye la UI visible.** La UI se construye por código (`UiBuilder`), así que lo más sencillo es volver al inicio y reconstruir [P].
- **Idioma por app de Android 13+** (Ajustes › Apps › Idioma): existe desde la API 33 y exige declarar `localeConfig` [V]. Unity no documenta cómo integrarlo [E]. **No lo haría en 1.0:** basta el selector del juego, como en los líderes [E].
- **Analítica para decidir la tanda 3** [P]: propiedades de usuario `device_language` (el del teléfono) y `app_language` (el del juego). Es un cambio de analítica: lo aprueba el dueño (Q11).

### 3.6 Plurales, números, precios y fechas

- **Plurales:** reglas CLDR por idioma, en la tabla (sección 3.2, regla 3). Una prueba recorre cada cadena plural con n = 0, 1, 2, 5, 21, 1.000 y 1.000.000 en cada idioma [P]: el 0 separa PT-BR y FR del resto, y el millón activa la forma *many* de ES, PT, FR e IT.
- **Números.** Medido hoy con .NET 10 (ICU) [V]:

  | Formato | Idiomas y países |
  |---|---|
  | `12,345.5` | `es-419`, `es-MX`, `es-PE`, `en-US`, `ja-JP` |
  | `12.345,5` | `es-CO`, `es-AR`, `es-ES`, `pt-BR`, `de-DE`, `tr-TR` |
  | `12 345,5` | `fr-FR` (espacio fino U+202F), `ru-RU` y `pt-PT` (espacio duro U+00A0) |

  **Conflicto con el glosario §10**, que fija «1.000» para ES: México y Perú escriben «1,000». Es la pregunta Q8. Además, IL2CPP en Android puede traer otros datos de cultura que .NET 10 [NV]: hay que medirlo en el teléfono, o fijar los separadores por idioma en la tabla.
- **Precios:** siempre los de la tienda (`localizedPriceString`, `isoCurrencyCode`) [V]. En el Editor el precio es ficticio; hay que probarlo en el dispositivo [V].
- **Fechas y cuentas atrás** (Reto del río, Crecida): con el formato del idioma, y las unidades («2 h 5 min») también en la tabla, con plural.

### 3.7 Presupuesto de longitud

- Pautas de IBM que cita el W3C, para textos traducidos **desde el inglés** a lenguas europeas [V]. Sus porcentajes son la longitud final respecto al original:
  - hasta 10 caracteres: 200-300 %, es decir, el texto ocupa de 2 a 3 veces el original (+100-200 %);
  - más de 70 caracteres: ≈130 % (+30 %).
- El artículo no da una cifra general para el alemán: «el alemán ocupa ≈30 % más que el inglés» es una regla práctica del sector [E]. No hay tabla publicada para textos fuente en español [NV].
- **Reglas de diseño** [P]:
  - botones y etiquetas cortas con espacio para el doble del texto ES;
  - frases de más de 70 caracteres, con un 30-40 % de margen;
  - los contenedores crecen (02 §12: escala de fuente del sistema hasta el 130 % sin desbordes ni recortes a 360 dp);
  - **el autoajuste de TMP nunca baja de 12 sp** (02 §12).
- La columna `max_chars` se comprueba en una prueba de EditMode (falla si una traducción se pasa), y el traductor la ve en su herramienta, como hace Playrix con Gridly [V].

### 3.8 Pseudo-localización en las pruebas de capturas

- **Un idioma falso solo para desarrollo** [P], con códigos como los de Android (`en-XA`, acentuado y alargado; `ar-XB`, espejo RTL) [E]. Ejemplo: «[Ñívél 12 ·····]». Unity Localization trae los métodos (alargar, acentuar, encerrar entre corchetes, espejo) [V]; un sistema propio los implementa en pocas líneas.
- **Qué revela:**
  - el texto que no pasa por la tabla (sale sin corchetes);
  - las frases pegadas a trozos (corchetes en mitad de la frase);
  - los desbordes;
  - los glifos que faltan.
- **Dónde:** en los tests de capturas que ya existen (`LabelSnapshotTests`, `MechanicSnapshotTests` y `SnapshotKit` a 360 × 780 dp). Basta con un parámetro de idioma que deje las capturas en `Logs/snapshots/<idioma>/`. A eso se suma una prueba automática que recorre las etiquetas TMP visibles y falla si alguna desborda o baja de 12 sp [P].
- Las mismas capturas por idioma son el **material de LQA** para el revisor nativo y la base de las **capturas de la ficha** (sección 5.4).

### 3.9 Nombres propios y glosario por idioma

- **No se traducen:**
  - **Tintalú**: siempre con tilde en lo visible (09 §8, decisión 8). Cualquier transliteración (por ejemplo, en katakana para la ficha japonesa) depende del abogado de marcas [NV].
  - **Capi** y **Rosa**: idénticos en los tres idiomas (glosario §3.1; 09 §8, decisión 7).
  - **cuia**: palabra única en ES, EN y PT (09 §8, decisión 6), «+1 cuia» también en EN y PT (glosario §10).
  - Los topónimos (glosario §8).
  - Los nombres científicos, en cursiva.
- **Se traducen con nombre aprobado:** los 14 tintes (glosario §9 ya los fija en ES, EN y PT-BR) y las etiquetas funcionales de los comodines (`undo`, `hint`, `shuffle`…).
- **Comodines con nombre propio** (Marea, Morpho, Pororoca, Luciérnaga): para cada idioma nuevo se decide término a término **antes** de traducir (traducir, mantener o adaptar) [P].
- **Glosario por idioma** (`Assets/_Project/Data/Loc/glossary.csv` [P]): término, tipo (marca / no traducir / término del juego / fruta / lugar), un valor por idioma y notas. Se deriva del glosario §9-§10, que sigue siendo el canon. El borrador por IA lo recibe siempre, y una prueba comprueba que, si la cadena ES contiene un término, la traducción usa el aprobado [P].
- **Fuera del alfabeto latino, los nombres de fruta se transliteran** como ya se usan en ese idioma. Ejemplos de las etiquetas de Wikidata [V, 2026-10-10]: アサイー (japonés) y Асаи (ruso) para el açaí; กามูกามู (tailandés) para el camu camu. Lo elige el revisor nativo y queda en el glosario; nunca lo inventa la traducción automática.
- **Wikidata no es fuente de nombres sin revisión.** Sus etiquetas son editables y pueden estar vandalizadas: el 2026-10-10, la etiqueta inglesa del açaí (Q33943) mostraba un nombre falso y ofensivo en lugar de *Euterpe oleracea* (última edición del elemento: 2026-09-22) [V]. Las etiquetas nunca se importan sin más: la clave estable es el nombre científico (propiedad P225) y el nombre por idioma lo fija el revisor nativo en el glosario.
- **Prueba de nombres** [P]: antes de cada idioma nuevo, el traductor comprueba que Tintalú, Capi, Rosa, cuia y los nombres de los comodines no tengan un sentido malsonante en ese idioma. Cuesta minutos y evita un mal día.

### 3.10 «Pendiente del asesor cultural»

- El asesor aprueba **el texto fuente en español**. Después se traduce, y el revisor nativo comprueba la traducción contra la fuente y el glosario (09: el asesor revisa cada Dato del río).
- La columna `status` de la tabla lo hace visible. **Compuerta de release** [P]: un build con el `applicationId` definitivo falla si una cadena cultural (fruta, fauna, lugar o pueblo) no está en `aprobado_es`, o si una cadena de UI de un idioma ofrecido está vacía. Los builds de prueba (`com.amazonfill.dev`) solo avisan, para no frenar el desarrollo.

### 3.11 Las dos vías: Unity Localization frente a un sistema propio

| Criterio | A · `com.unity.localization` 1.5.13 (lo que prevé 08 §2.2) | B · Sistema propio ligero (JSON o CSV por idioma) |
|---|---|---|
| Aprobación | Paquete nuevo y su dependencia **Addressables**: lo aprueba el dueño (regla 4). Addressables ya está planificado para la fase 3 del roadmap (08 §6.2 y §15.3) | No hace falta nada nuevo |
| Edición por agentes | Las String Tables son assets YAML: **prohibido editarlas a mano** (regla 3). Se generan desde el CSV con un método de Editor versionado (importación CSV incluida [V]), y eso exige Unity en cada cambio de texto | El CSV o JSON es la tabla: lo editan los agentes y se valida con pruebas, sin abrir Unity |
| Plurales y formatos | Smart Strings: plurales CLDR para cualquier idioma, `choose`, anidación, números con el idioma [V] | Hay que programarlos: ES, PT y EN son sencillos siempre que se usen las reglas CLDR (nunca `n == 1`, que falla en PT-BR y FR con 0 y en ES con los millones); RU, PL y AR, no |
| Pseudo-localización | Incluida [V] | Unas 50 líneas propias |
| Fuente por idioma | `LocalizedTmpFont`: tabla de assets con un `TMP_FontAsset` por idioma [V]. (`LocalizedTextCoreFontAsset`, de la 1.5.12, es para el texto de UI Toolkit, no para nuestra UI de uGUI con TextMeshPro) | Lista de reservas de TMP (se puede sin paquete) |
| Entrega por idioma | Agrupa tablas y recursos por idioma en Addressables [V]: encaja con PAD | Las tablas pesan KB; las fuentes irían por Addressables igualmente |
| Arranque | Asíncrono por defecto, pero tiene **Initialize Synchronously** (bloquea el hilo principal hasta terminar) y precarga de las tablas del idioma elegido [V]. 08 §2.3 pide carga síncrona en menos de 50 ms: hay que medir que quepa en el teléfono [E] | Síncrono e inmediato |
| Pruebas | EditMode con la configuración de Localization; más frágil [E] | Pruebas puras y rápidas |
| Herramientas | CSV, XLIFF y Google Sheets [V]; el idioma del sistema en Android con códigos BCP 47 (1.5.10) [V] | CSV, que cualquier herramienta importa |
| Mantenimiento | Unity (versión del 2026-09-02). En Unity 6.7 pasa a estar integrado en el motor (nota debajo de la tabla) | Nuestro, para siempre |
| «No inventar la rueda» | Sí: la solución oficial | Parcial: copia el patrón, no la herramienta |

**Recomendación: la vía A como destino, montada así** [P]:
1. **Ahora, sin paquete:** el CSV de la sección 3.3 como fuente única y una **fachada `Loc`** (`Loc.T(key)`, `Loc.T(key, args)`, `Loc.Number(n)`, evento `Loc.Changed`) en un ensamblado sin motor (`AF.Localization`, el nombre que ya da 08 §2.2). Por detrás, un lector del CSV con el subconjunto `{n:plural:…|…}` de Smart Strings. `UiText` se queda como acceso tipado (`UiText.Next` devuelve `Loc.T("next")`) para no tocar las pantallas.
2. **En la fase 3 del roadmap (09), con tu visto bueno:** se instala Unity Localization 1.5.13 **detrás de la misma fachada**. Un método de Editor versionado importa el CSV a las String Tables, que se commitean como generadas (como `tutorial_v0.json`), y una prueba comprueba que tablas y CSV coinciden. Las pantallas no cambian.
3. **Si prefieres no instalar el paquete,** la vía B completa sirve para la 1.0 y para los idiomas latinos de la tanda 2. Se complica con los plurales de RU y PL y con la entrega de fuentes CJK. Es la pregunta Q3.

**Rumbo de Unity** [V, documentación beta]: en Unity 6.7 (beta) la localización viene integrada en el motor (*Localization Runtime*, activado por defecto, con referencias directas como fuente por defecto y sin Addressables). El paquete `com.unity.localization` 2.0 solo hace falta para cargar con Addressables y para importar y exportar CSV y XLIFF. Con Unity 6.3 LTS, la versión fijada, sigue valiendo 1.5.13. No cambia la recomendación, pero refuerza la fachada `Loc`: las pantallas no quedan atadas al paquete, y una futura subida de versión de Unity solo toca lo que hay detrás.

**Descartada como vía principal: I2 Localization** (Inter Illusion; 45 USD en la Asset Store; última versión 2.8.22 f6, del 2024-09-24 [V]).
- **A favor, según su editor** [V, página del editor; sin probar por nosotros]: corrige automáticamente los idiomas de derecha a izquierda, como el árabe, sobre la UI existente, algo que el paquete oficial no hace en uGUI con TMP (4.5). Además, el juego publicado descarga de Google Sheets los cambios y los idiomas nuevos sin sacar otra versión. Con Unity Localization también se puede, con tablas remotas en Addressables, pero es más delicado [E].
- **En contra:** es de un tercero, de pago y sin actualizaciones desde 2024, y su RTL sobre TMP no lo hemos comprobado.
- **Conclusión:** se mantiene la recomendación A. I2 queda como alternativa a RTLTMPro para el árabe (4.5 y Q9).

---

## 4. Fuentes y alfabetos

### 4.1 Qué cubren hoy Baloo 2 (títulos) y Nunito (texto)

Medido con fontTools sobre las TTF del repositorio y con los metadatos de Google Fonts [V]:

| Bloque | Baloo 2 | Nunito | Idiomas |
|---|---|---|---|
| Latino básico y Latin-1 | Sí | Sí | ES, PT, EN, DE, IT, NL; FR salvo «œ» |
| Latino ampliado A | 127/128 | 126/128 | TR, PL, RO, CS, HU; FR «œ» |
| Latino ampliado adicional (vietnamita) | 168/256 (subconjunto vietnamita completo) | 164/256 (ídem) | VI |
| Cirílico | **No** | 220/256 | RU, UK: texto sí, títulos no |
| Devanagari | Sí | No | HI: títulos sí, texto no (y TMP no lo compone, 4.5) |
| Griego, árabe, hebreo, tailandés, CJK, hangul | No | No | — |

**El cuello de botella no son las TTF, sino el atlas:** `FontSetup.Characters()` solo mete ASCII, Latin-1 y «–—‘’“”…•», y no hay fuentes de reserva. Además, a Nunito le falta «→» (U+2192), que usa `DoubleHarvestDelta` (hoy sin uso).

### 4.2 Fuentes de reserva por escritura (todas OFL, cada una con tu visto bueno)

| Escritura | Idiomas | Propuesta | Dificultad en TMP | Peso aproximado [E] |
|---|---|---|---|---|
| Latino ampliado | TR, PL, VI, FR (œ) | Ninguna: regenerar el atlas con más glifos (las cinco TTF tienen «œ» y «Œ» [V]) | Ninguna; en VI, comprobar el interlineado de los diacríticos apilados | < 0,5 MB |
| Espacio fino (U+202F) | FR: separador de miles de ICU en `fr-FR` y espacio fino antes de ! ? ; | **No está en ninguna TTF del repositorio** (Baloo 2 Bold y ExtraBold; Nunito Regular, Bold y ExtraBold) [V]: regenerar el atlas no lo arregla. `Loc.Number` lo sustituye por U+00A0 (que sí está) y la tabla FR usa U+00A0; o se añade una fuente de reserva que lo tenga | Sin arreglo, TMP pinta en su lugar el carácter de glifo ausente (□, U+25A1) [V, código de TMP del proyecto] | 0 MB con U+00A0 |
| Cirílico | RU | Títulos con **Nunito ExtraBold** (ya en el repo) u otra redondeada con cirílico | Ninguna | < 0,5 MB |
| Japonés | JA | Noto Sans JP | TMP corta por carácter y tiene reglas de inicio y final de línea para CJK en TMP Settings [E] | 2-5 MB por atlas recortado; 10-16 MB con la fuente completa [E] |
| Coreano | KO | Noto Sans KR | Solo corta por espacios si se activa **Use Modern Hangul Line Breaking Rules** en TMP Settings, que hoy está en 0 (`m_UseModernHangulLineBreakingRules: 0`) [V]. Sin eso, TMP corta en cualquier sílaba, como en chino y japonés. Se activa en el Editor o con un método de Editor versionado, nunca editando el YAML (regla 3); tarea de la tanda 2 (sección 6, nivel 4) | 2-4 MB recortado |
| Chino | ZH-Hant / ZH-Hans | Noto Sans TC / SC | Como el japonés | 2-5 MB recortado por variante |
| Tailandés | TH | Noto Sans Thai | **Sin espacios:** hay que segmentar (espacio de ancho cero U+200B insertado al traducir) [E]. La colocación de tonos y vocales usa marca sobre base, que TMP sí aplica [E]: lo difícil es el corte de línea | < 1 MB |
| Árabe | AR (FA, UR) | Noto Sans Arabic | **Letras unidas y RTL:** TMP no lo hace (4.5) | < 1 MB + ingeniería |
| Hebreo | HE | Noto Sans Hebrew | RTL | < 0,5 MB + ingeniería |
| Devanagari | HI | Baloo 2 (títulos) + Noto Sans Devanagari (texto) | **Conjuntos:** TMP aplica kerning, ligaduras y posicionamiento de marcas, pero no el modelado contextual (reordenación, sustituciones encadenadas): los conjuntos del devanagari no salen bien [E] | < 1 MB + ingeniería |

### 4.3 Atlas estáticos frente a dinámicos

- **Estático:** los glifos se generan antes y la TTF no va en el build. **Dinámico:** la TTF va en el build y los glifos se crean al vuelo [E].
- **Tintalú no tiene texto escrito por el jugador** (sin chat ni nombres en la 1.0). Así que **todo el texto se conoce al compilar** y se pueden usar **atlas estáticos por idioma con solo los caracteres de nuestras tablas** [P]. Es lo que recomienda la práctica habitual con CJK: un atlas estático con los caracteres conocidos y una reserva dinámica solo para lo imprevisto [E].
- **Propuesta:** `FontSetup` calcula el juego de caracteres **a partir del CSV** de cada idioma, en lugar de la lista fija, y una prueba falla si una cadena de un idioma usa un carácter que no está en su atlas [P]. **La prueba incluye también los caracteres que genera el formateo** (`Loc.Number`, fechas y cuentas atrás de cada idioma), que no están en el CSV: por ejemplo, el U+202F de las cifras francesas. Así, un «œ», un «ğ» o un separador nuevos no llegan nunca al jugador como un cuadrado vacío.
- **Excepción futura:** la Liga del río (1.2) mostrará nombres de otros jugadores. Ahí hará falta una reserva dinámica. Una opción sin peso es usar las fuentes del sistema Android, pero no está probada en TMP [NV]; la otra, Noto en un pack de PAD.

### 4.4 Peso en el AAB (≤ 80 MB) y entrega por PAD

- **Presupuesto actual:** 65-76 MB, con ≈1,5 MB de fuentes latinas (08 §6.1). Lo latino ampliado y el cirílico caben con holgura (< 1-2 MB más) [E].
- **CJK:** aun recortados, 2-5 MB por idioma y peso de letra. **No caben todos de serie.** Propuesta [P]:
  - un pack `fonts_cjk` de Addressables for Android en modo **fast-follow** (se descarga solo, justo después de instalar), o bajo demanda por idioma;
  - mientras no llega, el juego se ve en inglés y cambia al llegar el pack (caso raro).
- **Play Asset Delivery no reparte asset packs por idioma**, solo por dispositivo y por compresión de textura [E/NV]. Por eso la clave es un pack pequeño por escritura, pedido según el idioma.
- **Cada pack nuevo de fuentes se añade a la tabla de peso de 08 §6.1 y lo vigila la compuerta del CI** (`ci/size-gate.ps1`).

### 4.5 Derecha a izquierda (árabe y hebreo) y escrituras complejas: fuera al principio

**Por qué fuera:**
1. **TextMeshPro sobre uGUI no compone el árabe** (no une las letras), no ordena el texto bidireccional, no aplica el modelado contextual que necesitan los conjuntos del hindi (reordenación, sustituciones encadenadas) y no sabe cortar el tailandés [E]. Sí aplica un subconjunto de GSUB y GPOS: kerning, ligaduras, sustitución múltiple, marca sobre base y marca sobre marca [V, API de TMP en uGUI 2.0].
2. **La solución oficial de Unity, Advanced Text Generator** (HarfBuzz + ICU), **es solo para UI Toolkit**. Un empleado de Unity lo confirma: «Only UI Toolkit» [V]. En Unity 6 es opcional, solo admite fuentes dinámicas y añade 4,8 MB de datos ICU [V].
3. **En uGUI hay dos alternativas, las dos de terceros** (las aprueba el dueño):
   - **RTLTMPro** (MIT; su README lista árabe, persa y hebreo). Tiene incidencias abiertas en los campos de texto (#152, de 2026-02) y una sobre sus sombreadores en Unity 6 (#134, abierta desde 2024-10) [V]. Una sobre desplegables (#140) se cerró en abril de 2026 porque el mantenedor no la reprodujo [V]. Hay que probarlo en Unity 6.3 antes de proponerlo.
   - **I2 Localization** (de pago), cuyo editor promete corregir el RTL, como el árabe, sobre la UI existente [V, según el editor; sin probar por nosotros] (3.11).
4. **El RTL obliga a reflejar toda la interfaz** (HUD, bandeja de comodines, flechas, progreso) y a probarla entera. Google Play avisa: si activas idiomas RTL, la app tiene que estar preparada para RTL [V].
5. **Mercado:** el árabe paga razonablemente (Royal Match y Township lo traen), así que es candidato a la tanda 3 **con datos**. El hebreo solo tiene sentido junto al árabe.

**Lo que se deja preparado ya, gratis** [P]:
- sin texto en imágenes;
- sin frases pegadas a trozos (el texto bidireccional las desordena);
- iconos sin dirección o con su versión reflejada;
- contenedores con alineación configurable.

---

## 5. Proceso y coste

### 5.1 Dónde viven los textos

| Opción | A favor | En contra | Cuándo |
|---|---|---|---|
| **CSV en el repositorio** [P] | Lo editan los agentes, se ve en los diffs, lo validan las pruebas y no necesita credenciales | Los traductores no trabajan en git: se les pasa el CSV o una copia en hoja de cálculo, y se reimporta | **Ahora y en la 1.0** |
| Google Sheets sincronizada (lo que prevén 08 §2.2 `tools/loc-sync` y 09) | Cómoda para traductores; Unity Localization la sincroniza [V] | Necesita credenciales OAuth: **secretos que los agentes no pueden manejar** (regla 5); solo el dueño podría sincronizar | Si el dueño quiere trabajar en la hoja |
| TMS (Gridly, Lokalise, Crowdin) | Memoria de traducción, límites de caracteres, flujo de revisión, IA integrada; es donde acabó Playrix [V] | De pago: Gridly desde 50 €/mes [V]; Lokalise desde **149 USD/mes** (no 59, como dicen 09 y research/06) [V]; Crowdin sin plan gratuito para proyectos privados, salvo código abierto o académico [V] | Más de ≈5 idiomas, o contenido nuevo cada pocas semanas |

### 5.2 Quién traduce y en qué orden

1. **El asesor cultural aprueba el español** (fuente) de todo lo cultural.
2. **EN y PT-BR, desde el español.** El inglés es además el **idioma puente** de la tanda 2: los traductores de inglés a japonés o a turco abundan y los de español, no [E]. Por eso el inglés tiene que estar revisado a fondo antes de la tanda 2.
3. **Tanda 2: desde el inglés, con el español como referencia** en una columna.
4. **Modo de traducción** [P]:
   - **UI y tutorial:** borrador por IA (con glosario, contexto, límite de caracteres y capturas) y **revisión de un nativo** pagada. La traducción automática con posedición rinde un 20-60 % más según estudios académicos, no el 200-300 % que prometen los proveedores [E].
   - **Datos de frutas y fauna:** traducción o posedición **humana completa** y revisión nativa. Se presentan como verdaderos: un error es un dato falso.
   - **Ficha de la tienda:** redacción nativa con palabras clave de búsqueda (ASO), no traducción literal.
5. **Nunca traducción automática sin revisión** en algo que vea el jugador.

### 5.3 Contexto y LQA con capturas

- **Paquete para cada traductor** (práctica común de los proveedores [E]):
  - el CSV con contexto y `max_chars`;
  - el glosario;
  - la guía de estilo (tuteo, *você*, imperativo en EN; sin dobles exclamaciones; glosario §10);
  - las capturas del juego en pseudo-localización y en ES;
  - una persona que conteste dudas.
- **LQA dentro del juego:** capturas por idioma del pipeline de la sección 3.8 y, al menos una vez por versión grande, una partida en un teléfono. Cada error se anota con su captura y su clave. Tarifas de LQA de 27 a 53 €/h según el idioma [V].
- **King** da a los traductores builds y betas, y actualiza las guías con lo aprendido [V]. A nuestra escala, basta el APK de desarrollo y las capturas.

### 5.4 Fichas de tienda por idioma (ASO) y experimentos

- **Títulos ya decididos** (09 §8, decisión 8): EN «Tintalú: Water Sort Puzzle», ES «Tintalú: Ordenar Agua» y PT-BR «Tintalú: Classificar Água». En el género, los ganadores dejan «Water Sort» en inglés y añaden un descriptor local [E]. Para la tanda 2, el mismo patrón.
- **Ocho capturas o más por idioma** (08 §13.1), sacadas del pipeline de capturas con el texto del juego en ese idioma. Si no se localizan los gráficos, Play muestra los del idioma por defecto [V].
- **Experimentos localizados en Play:** hasta 5 idiomas por experimento y 5 experimentos a la vez [V]. Fichas personalizadas por país (BR con Pix, MX con OXXO; 08 §13.1).
- **Traducción de Play Console** [V]:
  - **automática y gratis** de la ficha y de los productos IAP a 10 idiomas (AR, FR, DE, ID, IT, JA, PT-BR, ES-419, ES-ES, TH), sin revisión humana;
  - **humana** desde 0,07 USD por palabra, en ≤ 7 días;
  - **la traducción de textos de la app con Gemini solo lee `strings.xml`**: no sirve para nuestras tablas.
- **Regla [P]:** nunca publicar una ficha en un idioma que el juego no habla. Promete algo que no cumple y trae reseñas negativas [E].
- **Productos IAP:** título y descripción por idioma en Play Console [V], y en el juego los nombres salen de la tabla, no de `StoreCatalog`.
- **iOS (más adelante):** la tienda de EE. UU. admite EN-US y 9 localizaciones más: árabe, chino simplificado y tradicional, coreano, español de México, francés, portugués de Brasil, ruso y vietnamita [V, Apple]. Según AppTweak, todas cuentan para la búsqueda en EE. UU., así que el margen de palabras clave es mucho mayor que el doble [E]; para un juego en español, ES-MX es la que más rinde allí [E].

### 5.5 Cifras de coste

**Volumen estimado [E]:**

| Bloque | Cadenas | Palabras ES |
|---|---|---|
| UI, tutorial, tienda, Ajustes, modales y notificaciones (09 #28: 300-600 cadenas) | ≈600 | ≈2.700 |
| Ficha de tienda, 13 productos IAP y notas de versión | — | ≈800 |
| **Subtotal juego base** | | **≈3.500** |
| Serie de frutas, si fueran 14 tintes × 20 frases de ≈15 palabras (depende de la otra propuesta) | 280 | ≈4.200 |
| Fauna (≈50 especies: nombre y dato de ≤ 50 caracteres) | ≈100 | ≈500 |
| **Total con contenido** | ≈980 | **≈8.200** |

**Tarifas** (Allcorrect, desde inglés, €/palabra [V]):

| Tarifa | Idiomas |
|---|---|
| 0,09-0,14 | ES-LatAm, PT-BR, KO, ZH-Hant |
| 0,10-0,14 | FR, DE, IT, JA, PL, ES-ES |
| 0,10-0,17 | NL |
| 0,06-0,09 | RU |
| 0,09-0,12 | TR, ZH-Hans, ID, VI |
| 0,09-0,12 con LQA obligatoria, o 0,14-0,16 con revisión completa | AR |
| 0,12-0,13 | HI |

Las repeticiones no se cobran [V]. La revisión cuesta ≈50 % de la traducción (LocalizeDirect [E]). No hay tarifa publicada **desde el español** [NV].

Supuestos de las cuentas [E]:
- borrador por IA + revisión nativa (posedición): 0,04-0,08 €/palabra (el sector cita 0,03-0,15 USD según el nivel);
- LQA: 4-8 h por idioma y versión grande, a 27-53 €/h (tarifa [V], horas [E]);
- revisión de estilo neutro del ES: ≈250-400 €.

**Coste por tanda [E]:**

| Tanda | Traducción humana + revisión | Borrador IA + revisión nativa | Solo juego base, humana |
|---|---|---|---|
| **1** (EN y PT-BR; ES con revisión de estilo; LQA de 3 idiomas) | ≈2.800-5.100 € | ≈1.300-3.000 € | ≈1.400-3.000 € |
| **2** (8 idiomas más, con LQA) | ≈10.000-16.600 € | ≈3.500-8.600 € | ≈4.800-9.000 € |
| Cada 100 datos de fruta más, por idioma | ≈200-320 € | ≈60-120 € | — |

Herramientas: el CSV no cuesta nada; un TMS, 50-149 €/USD al mes.

**Comparación con el GDD:** 09 presupuesta **600-1.500 USD** para PT-BR, EN y ES-419 con 300-600 cadenas. Encaja con el **juego base** traducido por humanos (≈940-1.480 € en EN y PT-BR, sin LQA), pero **no con la serie de frutas**. Es la pregunta Q12.

---

## 6. Qué hacer ahora, en orden (mínimo esfuerzo, sin bloquear el paso 1)

El paso 1 (integración en Unity, ola 3 con T09-T17 en sus worktrees) es lo urgente. T10 y T09 tocan `UiText` (archivos parciales `UiText.Cards.cs` y `UiText.Reveal.cs`) y la UI. **Nada de esta sección toca código hasta que la ola 3 esté fusionada**, para no provocar conflictos.

| Nivel | Cuándo | Qué | Esfuerzo [E] | ¿Necesita al dueño? |
|---|---|---|---|---|
| **0** | Ya | Responder Q1-Q3. Añadir la **regla para los agentes** (6.1) a `.claude/rules/` por PR. Este documento | Minutos | Sí (respuestas) |
| **1** | Justo después de fusionar la ola 3 | **Quitar las cuatro trampas:** `PlayLevel` y `AdOverlay` sin pegar trozos; `StuckModal` sin `.Replace` (salto de línea dentro de la cadena); plurales en «Te faltan…», «unos {0} días» y «Perderás 1 canoa»; `Grouped` con una sola función de formato. Mover a `UiText` los nombres de `StoreCatalog` y los datos de `FaunaFact` | ≈1 día de agente | No |
| **2** | Antes del paso 3 (elenco de ≈50 especies) y de la serie de frutas, para que su texto **nazca en la tabla** | CSV `strings.csv` y `facts.csv`; fachada `Loc` en `AF.Localization` sin motor; `UiText` como acceso tipado; columnas EN y PT-BR rellenas con lo que ya da el glosario §10 (el resto, vacías); idioma de desarrollo elegible por `LaunchOptions` o `DevProbe`, sin UI; idioma falso `en-XA` en las pruebas de capturas; `FontSetup` calcula los glifos desde el CSV y una prueba de cobertura; prueba de `max_chars` | ≈2-3 días de agente | No: sin paquetes ni SDK |
| **3** | Fase 3 del roadmap (09; 1.0) | Perfil `profile_schema` 2 con `settings.language` (en la misma migración que el álbum y la fauna); selector en Ajustes; Unity Localization 1.5.13 detrás de la fachada (si Q3 = A); ronda de traducción EN y PT-BR, LQA y fichas | Según Q3-Q5 | Sí: paquete, migración, analítica y presupuesto |
| **4** | Antes del lanzamiento mundial | Tanda 2: atlas latinos ampliados y cirílico; U+202F sustituido por U+00A0 en francés; activar **Use Modern Hangul Line Breaking Rules** en TMP Settings (en el Editor o con un método de Editor versionado); fuentes JA y KO en un pack fast-follow; traducción desde EN; LQA; 8 fichas nuevas | Según Q1 y Q9 | Sí: fuentes nuevas y presupuesto |

Los niveles 1 y 2 compilan y prueban con Unity (`scripts/unity-compile.ps1` y `unity-test.ps1 -Mode EditMode`/`PlayMode`). Cuentan para el límite de 2 Editores abiertos, así que van cuando no haya otro agente con Unity en marcha.

### 6.1 Regla propuesta para los agentes (`.claude/rules/localizacion.md`) [P]

> **Textos visibles (UI, notificaciones, accesibilidad, datos de fruta y fauna):**
> 1. Ningún texto visible nuevo fuera de la tabla (hoy `UiText`; desde el nivel 2, `Data/Loc/*.csv`). Ningún texto en catálogos, `switch`, escenas, imágenes ni sombreadores.
> 2. Una clave por frase completa, en `snake_case`, estable; marcadores con nombre. Prohibido concatenar, partir o hacer `Replace` sobre texto visible.
> 3. Plurales con la sintaxis de la tabla; números con `Loc.Number`; precios solo de la tienda; sin `ToUpper`/`ToLower` en texto visible.
> 4. La analítica y Remote Config llevan claves o ID, nunca texto.
> 5. Todo texto cultural nuevo nace con `status = pendiente_asesor`; no se inventan nombres ni traducciones de fruta, fauna o lugares: salen del glosario.
> 6. Una cadena nueva lleva `context` y `max_chars`. Si una traducción no existe, se deja vacía; nunca se rellena con otro idioma.

---

## 7. Riesgos y cómo se evitan

| Riesgo | Ejemplo | Cómo se evita | Cómo se detecta |
|---|---|---|---|
| **Textos que desbordan** | «Weiterspielen» no cabe en un botón pensado para «Seguir»; la tarjeta del miquito baja a 10 dp | Diseño para ×2 en lo corto y +30-40 % en lo largo; contenedores que crecen; autoajuste ≥ 12 sp; `max_chars` en la tabla | Idioma falso `en-XA` en las capturas y prueba de desbordes (3.8) |
| **Fuentes a las que les faltan glifos** | «œ» en FR, el espacio U+202F en las cifras francesas, «ğış» en TR, títulos en ruso con Baloo 2 | Atlas generado desde el CSV de cada idioma; U+202F sustituido por U+00A0 (no está en nuestras TTF); Nunito ExtraBold para títulos cirílicos; Noto para CJK | Prueba «todo carácter de la tabla **y del formateo de números y fechas** está en el atlas de su idioma» |
| **Cortes de línea en coreano** | Una palabra partida por la mitad | Activar el corte moderno de hangul en TMP Settings (tanda 2) | Capturas en KO |
| **Traducción automática con nombres de fruta** | «huito» traducido como otra fruta; «Morpho» o «Marea» traducidos donde son nombre propio; «aguaje» convertido en un nombre inventado | Glosario con «no traducir» y nombre aprobado por idioma, siempre dentro del borrador; revisión nativa | Prueba de términos del glosario en cada traducción |
| **Error cultural u ofensivo** | «macaco» en PT-BR; un nombre de pueblo o de rito en un dato; pintura corporal con achiote o huito | El asesor aprueba el ES antes de traducir; revisor nativo de la región; lista de lo prohibido (GDD 01) en la guía de estilo | `status` y compuerta de release (3.10) |
| **Dato falso por traducción** | «el más grande» convertido en «uno de los más grandes», una cifra cambiada | Traducción humana de los datos; la fuente del dato en `context`; unidades métricas sin convertir y ninguna imperial | Revisión contra la ficha del dato |
| **Ficha de tienda mal traducida** | Traducción automática gratis de Play en un idioma que el juego no habla; palabras clave literales | Ficha solo en idiomas soportados; redacción nativa con ASO; capturas localizadas | Experimentos localizados; reseñas por idioma |
| **Coste del contenido largo** | 280 datos de fruta × 10 idiomas | Traducir por lotes, solo hasta donde llegan los jugadores (1-200 primero); datos sin traducir ocultos; borrador IA + nativo; entrega por paquetes remotos | Presupuesto por versión (Q12) |
| **Plurales y concordancias rotas** | «Te faltan 1 semillas»; «¡La açaí!» | Sintaxis de plural con reglas CLDR (nunca `n == 1`); una clave por frase; sin plantillas con nombres de fruta | Prueba con n = 0, 1, 2, 5, 21, 1.000 y 1.000.000 por idioma |
| **Números y precios mal formateados** | «1.000» frente a «1,000» en México; «1,000» en la interfaz y «5.200» en un dato; un precio calculado | Una sola función de formato; si Q8 = A, cifras de los datos con marcador `{n}` (3.4); precios de la tienda | Prueba por idioma y comprobación en el teléfono (IL2CPP [NV]) |
| **Mezcla de idiomas en pantalla** | Una tarjeta en inglés en una partida en portugués | UI incompleta = build de release fallido; datos incompletos = ocultos | Compuerta de release (3.10) |
| **Peso de las fuentes** | Noto CJK completa (10-16 MB) en el AAB | Atlas recortados y pack fast-follow | `ci/size-gate.ps1` |
| **RTL caro de añadir tarde** | Rehacer el HUD para el árabe | Las normas gratuitas de 4.5 desde ya; decisión con datos | — |
| **Turco con mayúsculas** | «İ» en lugar de «I» o al revés | Sin `ToUpper`/`ToLower` en texto visible | Revisión de código (regla 6.1) |
| **Nombre de marca con mal sentido en otro idioma** | Tintalú, Capi o Rosa malsonantes en un idioma nuevo | Prueba de nombres del traductor (3.9); el abogado para cualquier transliteración de la marca | Antes de cada idioma |
| **Conflictos con la ola 3** | Migrar `UiText` mientras T09-T17 lo tocan | Empezar el nivel 1 después de fusionar la ola 3 | — |

---

## 8. Preguntas cerradas al dueño

| # | Pregunta | Opciones | Recomendación |
|---|---|---|---|
| **Q1** | ¿Qué plan de idiomas seguimos? | A) 3 en la 1.0 (ES-419, PT-BR, EN) → los 11 de Magic Sort al salir al mundo, con el ruso el último → más según datos · B) igual, pero con el conjunto de Royal Match (árabe en vez de neerlandés) · C) solo los 3 y ampliar únicamente con datos · D) como A, pero sin ruso (10 idiomas; el ruso pasa a la tanda 3) | **A.** Es el patrón de los líderes y el árabe exige tecnología RTL que no tenemos. El ruso va el último porque en Rusia no hay cobros de Play ni anuncios de Google: se justifica por jugadores (Rusia, Kazajistán y otros países de habla rusa [E]), no por ingresos, y es el más barato (315-470 € la base). Si prefieres no gastar en él, D |
| **Q2** | ¿Preparamos ya la base, sin paquetes (niveles 1 y 2: trampas, CSV, fachada, idioma falso en capturas)? | A) Sí, justo después de fusionar la ola 3 · B) Sí, ya (choca con T09-T17) · C) No, en la fase 3 del roadmap (09) | **A.** Cuesta 3-4 días de agente y evita que el elenco de especies y la serie de frutas nazcan en el código |
| **Q3** | ¿Qué sistema de localización usamos en la 1.0? | A) Unity Localization 1.5.13 (lo que prevé 08 §2.2; trae Addressables) en la fase 3 del roadmap (09), detrás de nuestra fachada y con el CSV como fuente · B) Sistema propio ligero, sin paquete · C) I2 Localization (45 USD; trae RTL según su editor; sin actualizaciones desde 2024) | **A.** Es la solución oficial («no inventar la rueda») y Addressables ya está planificado para la fase 3 del roadmap. Nota: en Unity 6.7 (beta) la localización viene integrada en el motor y el paquete 2.0 solo hace falta para Addressables y CSV/XLIFF [V, documentación beta]; con Unity 6.3 LTS sigue valiendo 1.5.13, y la fachada protege ante ese cambio (3.11) |
| **Q4** | ¿Dónde viven los textos? | A) CSV en el repositorio; a los traductores se les pasa una copia · B) Google Sheets sincronizada (exige credenciales que solo tú puedes manejar) · C) Un TMS desde ya (Gridly ≥ 50 €/mes, Lokalise ≥ 149 USD/mes) | **A.** TMS solo al pasar de ≈5 idiomas |
| **Q5** | ¿Cómo traducimos? | A) Borrador por IA con glosario y revisión nativa en la UI; traducción humana completa en los datos de fruta y fauna y redacción nativa de la ficha · B) Todo con traductores profesionales · C) Todo por IA, sin revisión | **A.** Baja el coste a la mitad o menos sin arriesgar los datos que se presentan como verdaderos |
| **Q6** | ¿Qué variantes de español y portugués? | A) Un solo español (ES-419) y un solo portugués (PT-BR) para todos los países hasta tener datos; la ficha de España puede ser es-ES · B) Separar es-ES y pt-PT al salir al mundo | **A.** Es lo que hacen los líderes |
| **Q7** | ¿Cómo se elige el idioma? | A) El del teléfono, con respaldo (es-* → ES-419, pt-* → PT-BR, el resto → EN), y selector en Ajustes guardado en el perfil · B) Preguntar el idioma en el primer arranque | **A.** El GDD prohíbe pantallas de texto al arrancar y los líderes no la ponen |
| **Q8** | ¿Con qué formato se escriben las cifras («1.000» o «1,000»)? | A) El de la región del teléfono: Colombia «1.000», México y Perú «1,000»; las cifras de los datos de frutas van con marcador `{n}` para que coincidan · B) Fijo por idioma, como dice hoy el glosario §10 (ES «1.000»); el traductor escribe las cifras de los datos en ese formato · C) El estándar del idioma de la tabla (ES-419 «1,000»), también fijo | **A.** Cada jugador ve los números como en el resto de su teléfono. Exige editar el glosario §10 y escribir con marcador las cifras con separador o decimales de la serie de frutas (3.4) |
| **Q9** | ¿Qué hacemos con las escrituras no latinas al salir al mundo? | A) Ruso con Nunito en los títulos; japonés y coreano con Noto (atlas recortado, pack fast-follow; en coreano, con el corte de línea moderno de TMP activado); árabe, hebreo, hindi y tailandés fuera hasta la tanda 3 · B) Preparar ya el RTL (RTLTMPro, I2 Localization o UI Toolkit) · C) Solo idiomas latinos | **A.** Cubre los 11 de Magic Sort con el menor peso; cada fuente nueva te la pedimos antes |
| **Q10** | ¿Cómo llegan los datos de frutas a otros idiomas? | A) Por lotes, primero hasta donde llegan los jugadores; un dato sin traducir no se muestra · B) Solo en español hasta la 1.1 · C) Todo o nada: un idioma sale con todos los datos traducidos | **A.** El contenido llega a todos los idiomas sin frenar el lanzamiento |
| **Q11** | ¿Medimos en la analítica el idioma del teléfono y el del juego (`device_language` y `app_language`) para decidir la tanda 3? | A) Sí · B) No | **A.** Es como deciden los líderes qué idioma añadir; es un cambio de analítica que necesita tu visto bueno |
| **Q12** | El presupuesto de 09 (600-1.500 USD) no incluye los datos de fruta ni de fauna. ¿Qué hacemos? | A) Subir la partida de la 1.0 a ≈1.300-3.000 € (IA + revisión nativa, con contenido) · B) Mantenerla y traducir los datos después · C) Traducción humana completa, ≈2.800-5.100 € | **A** |

---

## 9. Notas para quien edite el GDD (no se ha tocado ninguno)

1. **09 (presupuesto) y research/06 §7:** Lokalise ya no cuesta 59 USD al mes; empieza en 149 USD [V, 2026-10-10].
2. **09 #28 y presupuesto:** 300-600 cadenas y 600-1.500 USD no incluyen la serie de frutas ni la fauna (sección 5.5).
3. **Glosario §10:** fija «1.000» para ES, pero el estándar de ES-419, México y Perú es «1,000» [V, ICU] (Q8). `UiText.Grouped` sigue al glosario.
4. **08 §2.2:** `AF.Localization` no dice que el paquete trae Addressables. `tools/loc-sync` con Google Sheets necesita credenciales (regla 5): propuesta de CSV en el repositorio (Q4).
5. **08 §2.3:** el idioma se carga antes que el perfil, pero el idioma guardado vive en el perfil (sección 3.5). Además, la carga síncrona en menos de 50 ms hay que medirla con el paquete.
6. **08 §4.3:** `settings.language` tiene por defecto «es-419»; propuesta de `auto` (seguir al teléfono).
7. **08 §6.1:** las fuentes cuentan ≈1,5 MB solo latinas; en la tanda 2 hacen falta filas para el cirílico y el pack CJK.
8. **08 §9:** no hay ninguna clave de idioma en Remote Config. Opcional [P]: `ui_languages_enabled` para apagar a distancia un idioma con una traducción rota.
9. **Catálogo de fauna:** usa `es`, `pt` y `en` en vez de `es-419`, `pt-BR` y `en`.
10. **07 y la tarjeta del miquito:** el autoajuste baja a 10-11 dp, por debajo del mínimo de 12 sp de 02 §12.
11. **02 §12 y 07:** el GDD no fija la longitud del Dato del río (el «≤ 2 líneas» de 02 §12 es del tutorial). La regla común de esta propuesta y la de frutas (14 sp; 90 caracteres en ES y 117 en los demás idiomas; métrico sin convertir; sección 3.4) habría que llevarla a 07 o a 02 si el dueño la aprueba.

---

## 10. Fuentes

Idiomas por juego (fichas del App Store de EE. UU.):
- Magic Sort: https://apps.apple.com/us/app/magic-sort/id6499209744 [V]
- Royal Match: https://apps.apple.com/us/app/royal-match/id1482155847 · https://en.wikipedia.org/wiki/Royal_Match [V]
- Royal Kingdom: https://apps.apple.com/us/app/royal-kingdom/id1606549505 [V]
- Toon Blast: https://apps.apple.com/us/app/toon-blast/id1176027022 [V]
- Homescapes: https://apps.apple.com/us/app/homescapes-match-3-games/id1195621598 · Gardenscapes: https://apps.apple.com/us/app/gardenscapes/id1105855019 · Township: https://apps.apple.com/us/app/township/id638689075 [V]
- Candy Crush Saga: https://apps.apple.com/us/app/candy-crush-saga/id553834731 · https://prnewswire.co.uk/news-releases/kingcom-expands-its-global-footprint-with-local-game-releases-in-asia-192265921.html · https://locworld.com/sessions/delicious-tale-localization-l10n-helped-turn-candy-crush-saga-global-phenomenon/ [V/E]
- Block Blast: https://apps.apple.com/us/app/block-blast/id1617391485 · https://www.pocketgamer.biz/hungry-studios-block-blast-returns-to-google-play/ [V]
- Wordscapes: https://apps.apple.com/us/app/wordscapes/id1207472156 · Water Sort Puzzle (IEC): https://apps.apple.com/us/app/water-sort-puzzle/id1514542157 · Words of Wonders: https://apps.apple.com/us/app/words-of-wonders-crossword/id1369521645 [V]
- IEC en Google Play (sin lista de idiomas): https://play.google.com/store/apps/developer?id=IEC+Global+Pty+Ltd [V]
- De dónde sale el campo «Idiomas» del App Store: https://developer.apple.com/forums/thread/836440 [E]

Mercados:
- https://www.pocketgamer.biz/dream-games-launches-royal-kingdom-worldwide-as-royal-match-nears-4-billion/ [V]
- https://www.pocketgamer.biz/royal-kingdom-surpasses-50m-in-player-spending-with-us-spending-surge/ [V]
- https://www.pocketgamer.biz/h1-2026-genre-analysis-strategy-stumbles-rpgs-fall-and-puzzle-revenue-ramps-up/ [V]
- https://0xspectrum.substack.com/p/royal-match-breaks-6-billion-in-player [E]
- https://sensortower.com/blog/top-10-worldwide-mobile-games-by-revenue-and-downloads-in-december-2025 · https://gamedevreports.substack.com/p/appmagic-mobile-market-landscape [E]
- Rusia: https://support.google.com/googleplay/android-developer/answer/11950272?hl=en · https://support.google.com/admob/answer/9391084 (pausa de AdMob en Rusia, enero de 2025) · https://support.google.com/publisherpolicies/answer/15766875 [V]
- China: https://www.mfat.govt.nz/br/trade/mfat-market-reports/entering-chinas-mobile-gaming-market-a-guide-to-licensing-and-regulatory-approval [V]
- Por qué localizar: https://www.newswire.com/view/content/survey-of-8-709-consumers-in-29-countries-finds-that-76-prefer-21174283 · https://localizedirect.com/posts/which-languages [E]

Tecnología:
- Unity Localization (manual y changelog; 1.5.13 del 2026-09-02; Addressables; selectores; Smart Strings; pseudo-localización): https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/index.html · https://docs.unity3d.com/Packages/com.unity.localization@1.5/changelog/CHANGELOG.html · https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/Addressables.html · https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/LocaleSelector.html · https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/Smart/Plural-Formatter.html · https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/Pseudo-Localization-Methods.html · https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/LocalizationSettings.html · https://docs.unity3d.com/Packages/com.unity.localization@1.5/api/UnityEngine.Localization.html (`LocalizedTmpFont` y `LocalizedTextCoreFontAsset`) [V]
- Unity 6.7 (beta), localización integrada y orden de selectores: https://docs.unity.com/en-us/engine/6000.7/manual/localization.md · https://docs.unity.com/en-us/engine/6000.7/manual/localization/locales [V, documentación beta]
- I2 Localization: https://assetstore.unity.com/packages/tools/localization/i2-localization-14884 · http://inter-illusion.com/tools/i2-localization [V, según el editor]
- Unity IAP 5, precios localizados: https://docs.unity3d.com/Packages/com.unity.purchasing@5.0/api/UnityEngine.Purchasing.ProductMetadata.html [V]
- TextMeshPro en uGUI 2.0 (tabla de rasgos tipográficos y corte del hangul): https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/TMPro.TMP_FontFeatureTable.html · https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/TMPro.TMP_Settings.html [V]
- Idioma por app en Android: https://developer.android.com/guide/topics/resources/app-languages [V]
- Expansión del texto: https://www.w3.org/International/articles/article-text-size [V]
- RTL y escrituras complejas: https://discussions.unity.com/t/announcing-full-rtl-language-support/1544214 · https://docs.unity3d.com/6/Documentation/Manual/UIE-advanced-text-generator.html · https://github.com/pnarimani/RTLTMPro · https://github.com/pnarimani/RTLTMPro/issues (#134, #140 y #152) [V]; https://github.com/abusaadp/Unity3dHindiRenderer · https://allcorrectgames.com/insights/unity-from-right-to-left/ [E]
- Fuentes de reserva en TMP y CJK: https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.2/manual/FontAssetsFallback.html · https://discussions.unity.com/t/correct-way-to-create-a-general-purpose-textmesh-pro-asset-for-non-english-languages/822769 [E]
- Cobertura de Baloo 2 y Nunito: https://github.com/google/fonts/blob/main/ofl/baloo2/METADATA.pb · https://github.com/google/fonts/blob/main/ofl/nunito/METADATA.pb [V]
- Addressables for Android y PAD: https://docs.unity3d.com/Manual/com.unity.addressables.android.html · https://developer.android.com/guide/playcore/asset-delivery/integrate-unity [E]

Proceso, coste y ASO:
- Playrix: https://www.gridly.com/case-studies/playrix/ [V] · King: https://inlingogames.com/blog/the-game-has-to-be-super-amazing-for-you-to-forget-about-the-language-barrier-an-interview-with-global-localization-manager-at-king-miguel-sepulveda/ [V]
- Tarifas: https://allcorrectgames.com/localization-pricing/ [V] · Posedición: https://www.polilingua.com/blog/post/machine-translation-post-editing-strategy-guide.htm [E]
- Herramientas: https://lokalise.com/pricing · https://www.gridly.com/pricing/ · https://crowdin.com/pricing [V]
- Play Console (traducción, experimentos y fichas personalizadas): https://support.google.com/googleplay/android-developer/answer/9844778?hl=en · https://play.google.com/console/about/translationservices/ · https://support.google.com/googleplay/android-developer/answer/6227309?hl=en · https://play.google.com/console/about/customstorelistings/ [V]
- Contexto y LQA: https://alconost.com/en/blog/game-translation-context · https://gridly.com/blog/what-is-localization-quality-assurance-lqa [E]
- ASO en iOS: https://developer.apple.com/help/app-store-connect/reference/app-store-localizations/ [V] · https://www.apptweak.com/aso-blog/how-to-benefit-from-cross-localization-on-the-app-store (actualizado el 2025-11-21) [E]
- Nombres de fruta en otros alfabetos (Wikidata; etiquetas leídas el 2026-10-10, editables y no fiables sin revisión): https://www.wikidata.org/wiki/Special:EntityData/Q33943.json · https://www.wikidata.org/wiki/Special:EntityData/Q1031775.json · https://www.wikidata.org/wiki/Special:EntityData/Q1144414.json [V como lectura de la etiqueta; E como nombre]
