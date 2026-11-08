# Promoción sin pago de Tintalú: lo que falta además de las redes

Investigación del 2026-11-07, a petición del dueño: «qué más podemos hacer para que la conozcan, la descarguen y la jueguen, todavía sin pagar publicidad».

- **Marco:** D-067: sin publicidad pagada hasta iOS. La 1.0.0 está en Play desde el 2026-10-31.
- **Ya en marcha:**
  - las redes (`estrategia.md`, una pieza al día en 4 redes);
  - el experimento de ficha A/B/C (`estrategia.md` §8);
  - la portada de tintalu.com con el enlace a Play (añadido de D-041).
- **Este documento cubre el resto:** la tienda, el propio juego, la comunidad, otras redes, prensa y festivales.
- Convención:
  - [V]: comprobado en una fuente primaria;
  - [NV]: de fuentes de terceros, sin confirmar;
  - [P]: propuesta;
  - [dueño]: espera su decisión.
- Reglas que se mantienen:
  - lo que se enseña existe en el juego (D-065);
  - nada de personas sintéticas;
  - público de 13 años en adelante, sin canales para niños;
  - ningún agente publica ni envía nada: lo hace el dueño;
  - sin SDK, paquetes ni permisos nuevos sin su aprobación (regla 4 de `CLAUDE.md`).

## 1. Resumen: qué hacer y en qué orden [P]

| # | Palanca | Impacto esperado | Coste | Quién | Cuándo |
|---|---|---|---|---|---|
| 1 | **Compartir el Reto del río al estilo Wordle** (tarjeta sin destripes, con enlace) | Alto: es el único bucle que trae jugadores nuevos sin que nadie pague | Código en el cliente + cambio del GDD | Escritorio (Unity), con el sí del dueño | 1.1 o 1.2 |
| 2 | **Contenido promocional en Play** (eventos de la ficha) | Medio-alto: sale en la ficha y en superficies de Play | 0; 15 min por evento | Dueño en Play Console; Spark redacta textos e imagen | Ya, con la 1.1 |
| 3 | **Pedir la reseña dentro del juego** (In-App Review API) | Medio: más reseñas y mejor nota suben la conversión de la ficha | Librería de Play Core nueva (aprobación) | Escritorio (sdk-integrator) | 1.1 o 1.2 |
| 4 | **BIG Festival (gamescom latam 2027)** | Medio: prensa, premio posible, contactos en Brasil | 0; inscripción gratis hasta el **2027-01-15** | Dueño inscribe; Spark prepara tráiler, textos y kit de prensa | Diciembre |
| 5 | **Pase de ASO** (palabras clave por idioma, descripción corta) | Medio: la búsqueda en Play es el canal orgánico principal del género | 0 | Spark propone; dueño aplica | Ya; repaso cada 60-90 días |
| 6 | **Canal de WhatsApp** (es-419 y pt-BR) | Medio en Brasil y LatAm, donde casi todos usan WhatsApp | 0 | Dueño lo crea; la fábrica le da piezas | Con las redes |
| 7 | **Vídeo largo en YouTube** («1 hora de vertidos relajantes») | Bajo-medio, pero dura: la búsqueda de YouTube trae visitas durante meses | 0, sale de la fábrica | Spark | Semana 3-4 |
| 8 | **Kwai** (Brasil) | Por medir: público joven, móvil y de juegos casuales [NV] | 0 | Dueño crea la cuenta | Tras 2 semanas de redes |
| 9 | **Kit de prensa y correo a prensa y creadores de LatAm** | Bajo-medio por envío; suma con el festival y la 1.1 | 0 | Spark redacta; dueño envía | Con la 1.1 |
| 10 | **Reddit** (hilos de desarrollador, con sus reglas) | Bajo, pero llega al público de Android que prueba juegos | 0 | Dueño publica; Spark redacta | Con la 1.1 |
| 11 | Compartir la llegada del Mapa del río como imagen | Medio | Código, como el 1 | Escritorio | Después del 1 |
| 12 | Google Play Games en PC | Bajo | Pruebas; hoy solo arm64 | Escritorio | Más adelante |
| 13 | Fondo Indie Games de Google Play en LatAm | Alto si sale, pero pide tracción | 0 | Dueño | Ronda de 2027 |

**Recomendación:** lo que no pide código (2, 4, 5, 6, 7, 9 y 10) arranca ya, y el 1 y el 3 se proponen al dueño como tareas para el cliente. El 1 es lo único que **multiplica**: todo lo demás suma por canal.

## 2. En la tienda (Google Play)

### 2.1 Contenido promocional (eventos de la ficha)
- **Qué es:** tarjetas de eventos, ofertas o novedades en la ficha. Si cumplen las pautas de calidad, Play las enseña también en otras superficies (Juegos, búsquedas, «Eventos») [NV]. Está en Play Console › Crecer › Presencia en la tienda › Contenido promocional.
- **Reglas** [NV, varias guías; confirmar en la consola]:
  - el contenido debe ser nuevo y para el jugador;
  - nada genérico ni permanente;
  - se envía con antelación (hasta 14 días antes; la revisión tarda hasta 4 días);
  - el acceso para juegos puede depender del programa «Level Up» de Play Games.
- **Lo que tenemos y es real:**
  - el lanzamiento de la 1.1, con la curva nueva (D-068) y el Mapa del río v2 (D-069);
  - el estreno de cada mundo nuevo;
  - las temporadas de la Liga del río;
  - las frutas de temporada (Frutas del río).
  - Ojo: el Reto del río diario no vale como evento por sí solo, porque es permanente.
- **Con nuestro stack:** la fábrica ya hace la imagen (1920 × 1080, sin texto en los bordes) y los textos en 3 idiomas. El dueño lo sube. **Coste: 0.**

### 2.2 ASO (posicionamiento en la búsqueda de Play)
- La búsqueda es el canal orgánico más grande del género. Funciona por **familias de palabras**, no por una sola: «ordenar agua», «water sort», «classificar água», «juego de agua relajante», «puzzle de colores», «quebra-cabeça de cores» [NV, guías ASO de 2026].
- **Ya hecho:** el nombre de la ficha sin tilde («Tintalu: Ordenar Agua», D-066), para que la búsqueda de «tintalu» lo encuentre.
- **Propuesta [P]:** un pase por idioma de la descripción corta y de la larga, con 2-3 familias cada una, sin repetir palabras clave de relleno (Play lo castiga). Se repasa cada 60-90 días con lo que digan las búsquedas de Play Console (Adquisición › términos de búsqueda). Las capturas y el vídeo ya entran en el experimento S10.

### 2.3 Reseñas
- **Pedirlas dentro del juego:** la In-App Review API de Play enseña la ventana de valoración sin salir del juego [V, developer.android.com].
  - **Cuándo:** tras un momento bueno, nunca tras cada nivel. Por ejemplo, la victoria del nivel 20 o la llegada a una parada del Mapa [P].
  - **Prohibido** preguntar antes «¿te gusta?».
  - **Cuota:** Play limita cuántas veces sale, sin decir el número. Por eso no puede ir en un botón «Valóranos» (podría no abrirse); ese botón llevaría a la ficha.
  - **Necesita la librería `com.google.android.play:review`** (Play Core ≥ 1.8.0): es una dependencia nueva, **[dueño]**. Se haría con un plugin propio, como los demás `.androidlib`, o con la librería por EDM4U.
- **Responder las reseñas** en Play Console: Spark puede redactar respuestas en los 3 idiomas, sin prometer nada (`estrategia.md` §6.3), y el dueño las publica. Las respuestas mejoran la nota percibida y a veces hacen que el usuario cambie la suya [NV].

### 2.4 Google Play Games en PC
- Un juego solo para ARM puede salir en PC, pero solo en equipos con procesador Intel, y la certificación completa pide un build x86-64 [V, developer.android.com, con una contradicción entre idiomas sobre AMD].
- Hoy compilamos solo arm64-v8a (la compuerta del manifiesto lo exige), así que serían más pruebas, más tamaño y otra compuerta. **Más adelante.**

### 2.5 Que Play lo destaque por su cuenta
- No hay un formulario confirmado para pedir que destaquen el juego: las guías se contradicen [NV].
- Lo que sí cuenta es la calidad técnica:
  - menos del 1 % de bloqueos (el objetivo del GDD es ≥ 99,5 % de usuarios sin bloqueos, 06 §9);
  - pocos ANR;
  - novedades frecuentes;
  - la nota de la ficha.
- Eso ya lo cuida el equipo del juego.

## 3. En el juego: bucles que traen jugadores

### 3.1 Compartir el Reto del río como Wordle [P, la palanca principal]
- **Por qué:** el crecimiento de Wordle se atribuye sobre todo a su tarjeta para compartir: un resultado sin destripes, de un reto que es el mismo para todos ese día, que se pega en WhatsApp o en una red [V en lo esencial; las cifras de crecimiento, NV].
- El Reto del río (D-031) ya tiene lo que hace falta: **un tablero al día, el mismo para todos**.
- **Cómo sería:**
  - al terminar el Reto, un botón «Compartir» abre el menú de compartir de Android;
  - lo que se comparte es un texto corto, por ejemplo: «Tintalú · Reto del río 7/11 · 🟦🟧🟩 en 14 vertidos · tintalu.com/r», con un enlace con `utm_source=share&utm_campaign=reto`;
  - en vez de los emojis, puede ir una imagen generada con la silueta del tablero resuelto, sin destripar la solución.
- **Técnica:** un `Intent.ACTION_SEND` con la API de la plataforma. **Sin SDK ni permiso nuevos**, como `AfAudioFocus.androidlib`. El enlace iría a una ruta nueva de tintalu.com (por ejemplo `/r`) que redirija a Play con el UTM: un cambio pequeño en `infra/web`, con despliegue del dueño.
- **Mercado:** en Brasil, entre el 92 y el 99 % de los teléfonos usan WhatsApp [NV, fuentes de proveedores]. La tarjeta está pensada para pegarse ahí.
- **Reglas:**
  - para un público de 13+ basta con compartir texto e imagen, sin datos personales;
  - con analítica (08 §8), mide cuántos comparten y cuántos llegan por ese enlace.
- **Qué hace falta:** un cambio del GDD (06, Reto del río) y una tarea para el escritorio **[dueño]**. Lo mismo valdría después para la llegada de cada parada del Mapa del río («Llegué a Manaos»).

### 3.2 Referidos (invita y gana)
- Premia al que invita y al invitado. Pide backend, control de abusos y textos legales por país.
- Con el Reto compartido se consigue buena parte del efecto sin nada de eso. **Más adelante.**

## 4. Comunidad y otras redes

### 4.1 Canal de WhatsApp
- **Qué es:** un canal de difusión (no un grupo, sin chat abierto, así que nada que moderar), uno en español y otro en portugués [P]. Gratis.
- **Contenido:** el Reto del día con su imagen, los estrenos y la mejor pieza de la semana. Sale de la fábrica.
- **Quién:** lo crea el dueño con la cuenta del juego. Ningún agente publica.

### 4.2 Reddit
- **Dónde y con qué reglas:**
  - r/AndroidGaming y similares, con sus reglas: hilo propio con texto, cuenta con historial y el desarrollador contestando en los comentarios;
  - algunos foros solo permiten la promoción un día a la semana [NV, reglas de terceros; leer la barra lateral de cada foro antes de publicar].
- **Mejor momento:** la 1.1, que da una novedad real que contar.
- **Quién:** Spark redacta y el dueño publica con su cuenta.

### 4.3 Kwai (Brasil)
- Competidor de TikTok muy usado en Brasil, con público joven, móvil y de juegos casuales [NV].
- Las piezas de la fábrica sirven tal cual (9:16). Postiz no dice si lo soporta, así que habría que subir a mano o con la app.
- **Propuesta [P]:** abrir la cuenta cuando las 4 redes lleven 2 semanas, para comparar con datos.

### 4.4 Vídeo largo en YouTube
- «1 hora de vertidos relajantes de Tintalú», con la música del juego y sin texto: la búsqueda de YouTube («ASMR water sort», «juego relajante») trae visitas durante meses, no solo el día que se publica [P].
- Sale de la fábrica con las tomas que ya hay. No usa nada nuevo.

### 4.5 Discord
- Pide moderación diaria y no tiene sentido sin una base de jugadores. **Más adelante.**

## 5. Prensa, festivales y fondos

### 5.1 BIG Festival en la gamescom latam 2027 [V]
- **Datos:**
  - inscripción gratis, abierta desde el 1 de octubre;
  - **cierre el 15 de enero de 2027** (23:59 de Brasilia);
  - el festival es del 22 al 25 de abril de 2027 en São Paulo.
- **Categorías que encajan:** Mejor juego casual, Mejor arte y Mejor sonido, entre otras.
- **Lo que hace falta:**
  - un tráiler de 60-90 s con tomas reales: la fábrica ya lo puede montar con HyperFrames;
  - descripción, capturas y kit de prensa;
  - el formulario lo rellena el dueño.
- Las reglas oficiales están en la web de la gamescom latam: hay que leerlas antes, sobre todo si admiten juegos ya publicados y si piden una versión de PC.

### 5.2 Kit de prensa y prensa y creadores de LatAm
- **Kit de prensa:** una página `tintalu.com/prensa` con tráiler, capturas, logotipo, ficha y contacto, en 3 idiomas [P]. Es otro despliegue de la web, con el sí del dueño (añadido de D-041).
- **Ventaja propia:** el modo de captura (E1) da metraje limpio, sin HUD, para que un creador monte su vídeo sin grabar nada.
- **Envíos:** correos breves a medios de móviles de Brasil, México, Colombia, Perú, Argentina y Chile, y a creadores pequeños de juegos casuales.
  - Spark redacta y el dueño envía.
  - Sin pagar a nadie ni prometer nada. Si un creador cobra, es publicidad pagada: espera a iOS.

### 5.3 Fondo Indie Games de Google Play en LatAm
- **Qué da:** entre 150.000 y 200.000 US$ sin ceder acciones, a 10 estudios por año.
- **Requisitos:**
  - estudio con sede en un país de LatAm de su lista;
  - hasta 50 empleados;
  - un juego ya publicado y con potencial [V para 2024-2025].
- **Fechas:** las rondas recientes abrieron en julio o agosto. Para 2026 la página solo invita a «mostrar interés» [V].
- **Dudas [dueño]:**
  - ¿Parallext tiene sede en un país elegible? Si es una LLC de EE. UU., quizá no valga.
  - Además, piden tracción: llegaríamos con datos de varios meses a la ronda de 2027.

### 5.4 El Amazonas como causa
- Colaborar con creadores o con ONG de conservación encaja con el tema, pero toca cultura y promesas: donaciones, impacto.
- Pide el visto bueno del asesor cultural y del dueño, y textos sin cifras sin fuente. **Más adelante.**

## 6. Lo que se pide al dueño

1. **Compartir el Reto del río** (§3.1): ¿se pide como tarea para la 1.1 o la 1.2, con el cambio del GDD?
2. **In-App Review API** (§2.3): ¿se aprueba la librería de Play para pedir la reseña tras un momento bueno?
3. **BIG Festival** (§5.1): ¿nos inscribimos? Spark prepararía el tráiler y los textos en diciembre.
4. **Contenido promocional** (§2.1): ¿Spark prepara los eventos de la 1.1 para subirlos a Play Console?
5. **Canal de WhatsApp y Kwai** (§4.1 y §4.3): ¿se crean? Kwai, tras 2 semanas de redes.
6. **Kit de prensa en tintalu.com** (§5.2): ¿se añade la página?
7. **Fondo de Google Play** (§5.3): ¿dónde tiene su sede Parallext?

## 7. Fuentes (2026-11-07)

- Contenido promocional en Play:
  - [AppTweak, LiveOps](https://www.apptweak.com/en/aso-blog/what-are-google-play-liveops-best-practices-examples)
  - [MobileAction, guía de 2026](https://www.mobileaction.co/guide/in-app-events-promotional-content-guide/)
  - [Zeevron, apps pequeñas](https://www.zeevron.com/insights/google-play-promotional-content-small-apps)
  - [AppRadar](https://appradar.com/blog/google-play-liveops-guide)
- ASO y destacados:
  - [AppFollow, ASO para juegos 2026](https://appfollow.io/blog/app-store-optimization-for-games)
  - [ASO World, junio de 2026](https://asoworld.com/en/blog/global-mobile-game-rankings-in-june-2026-revenue-downloads-and-aso-signals-to-watch/)
  - [ASOMobile, destacados 2026](https://asomobile.net/en/blog/app-store-and-google-play-featuring-2026-how-to-get-into-editorial-collections/)
  - [AppTweak, destacados](https://www.apptweak.com/en/aso-blog/how-to-get-your-app-featured-on-the-app-store)
  - [AppAgent, adquisición 2026](https://appagent.com/blog/mobile-game-user-acquisition-strategy/)
- In-App Review API:
  - [developer.android.com](https://developer.android.com/guide/playcore/in-app-review?hl=en)
  - [ProAndroidDev](https://proandroiddev.com/google-play-in-app-review-api-integration-and-experience-a96b30805345)
- Play Games en PC:
  - [inicio](https://developer.android.com/games/playgames/start)
  - [lista de revisión](https://developer.android.com/games/playgames/checklist?authuser=0)
- Fondo Indie Games:
  - [programa Indie Games de Play](https://play.google.com/console/about/programs/indiegames/)
  - [Google Developers Blog, ronda de 2025](https://developers.googleblog.com/indie-games-fund-google-plays-2m-fund-in-latin-america-is-back/)
  - [PocketGamer.biz](https://www.pocketgamer.biz/google-plays-indie-games-fund-returns-to-latin-america-with-2-million-for-10-studios/)
- BIG Festival:
  - [GamesBeat](https://gamesbeat.com/gamescom-latam-opens-speaker-call-and-free-indie-game-submissions-for-2027/)
  - [Olhar Digital](https://olhardigital.com.br/2026/10/19/games-e-consoles/gamescom-latam-2027-abre-inscricoes-para-big-festival-e-palestras/)
  - [Teragames](https://www.teragames.com.mx/anuncian-la-convocatoria-abierta-para-ponentes-e-indies-para-la-gamescom-latam-2027/)
- Wordle y compartir:
  - [Flourish, el auge de Wordle](https://flourish.studio/blog/the-rise-of-wordle/)
  - [AOL, Wordle y el NYT](https://www.aol.com/news/wordle-brought-tens-millions-users-150000203.html)
- WhatsApp en Brasil:
  - [Infobip](https://www.infobip.com/pt/blog/marketing-no-whatsapp)
  - [Pushwoosh](https://www.pushwoosh.com/pt/products/whatsapp-for-gaming/)
- Kwai:
  - [Nova Beyond](https://novabeyond.com/info-414.html)
  - [Propmark](https://propmark.com.br/anunciantes/como-o-kwai-quer-conquistar-novas-audiencias-durante-a-copa-2026-com-estrategia-mobile-first/)
- Reddit:
  - [reglas de r/Android (copia)](https://axebps-redlib.hf.space/r/Android/wiki/rulesandregs)
  - [GTStu, promoción en Reddit](https://gtstu.com/promote-indie-game-reddit-without-ban/)
