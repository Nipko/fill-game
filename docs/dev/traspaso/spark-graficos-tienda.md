# Encargo al equipo Spark: gráficos de la ficha de Google Play que conviertan (2026-10-30)

Lo escribió el integrador (escritorio) por encargo del dueño, que lo pidió así: «quiero realmente tener unas buenas imágenes de recursos y en los 3 idiomas, como también algunas generadas realzando más o haciendo unas únicas, guiándonos por las apps que funcionan y cómo lo hacen». Spark usa su stack de IA para imágenes; este documento dice **qué** hacer y **cómo**, y fija las reglas que no se pueden romper.

**Contexto:** la app ya está creada en Play Console (cuenta de organización «Automation AI», `com.parallext.tintalu`) y la 1.0 se publica en estos días (D-059). Hoy hay un juego de capturas válido en `docs/store-listing/graficos/` (gameplay real con un rótulo, 1080 × 1920, `graficos/README.md`). Sirve para la pista interna, pero el dueño quiere algo más vistoso antes de producción. **Prioridad: alta.** Entrega primero la fase 1 y el concepto A; los demás, después.

**Reglas:** las de siempre (`spark-reto-del-rio.md` §1 y §2; `CLAUDE.md` entero). Rama `spark/tienda-graficos` desde `origin/integracion-fase1-fase2`. Sin push force; el escritorio integra. No toques `docs/store-listing/graficos/` (el juego actual) ni el código del juego salvo lo que diga la fase 3. Nada nuevo en el repositorio sin el dueño: si una herramienta del repositorio necesitara un paquete nuevo (por ejemplo, Pillow), no lo añadas; tu stack de IA se queda en tu equipo y al repositorio solo llegan las imágenes, sus manifiestos y los scripts que usen lo que ya está aprobado. Las decisiones que haga falta tomar: anótalas con la recomendada aplicada y [P] (norma del dueño: ir con la recomendada).

## 0. Lo que el dueño ya decidió (D-065)

- Para los gráficos de la tienda (capturas, gráfico de funciones y, si sale, el vídeo) **se pueden usar imágenes generadas o realzadas con IA**, además del arte por código del juego (D-052).
- Límite: lo que una captura enseña como juego tiene que **existir en el juego tal como se ve** (política de metadatos de Play: nada engañoso). La IA puede crear o realzar el entorno (marco, fondo, luz, escenografía del río, personajes en pose de cartel), pero nunca inventar mecánicas, pantallas, botones, niveles, recompensas, personajes ni lugares que el juego no tenga.
- Los textos de los gráficos van en los 3 idiomas (es-419, en, pt-BR), con calidad nativa.

## 1. Fase 1 · Investigación: qué hacen las fichas que funcionan (entregar primero)

Haz un estudio corto y concreto en Google Play (tiendas de EE. UU., Brasil, México y Colombia) de **10-15 apps líderes** del género y vecinas:
- *water sort* y *color sort* (Magic Sort, Water Sort Puzzle, Water Sort: Color Puzzle, Sort Water Color…);
- *ball sort*, *nut sort* y *goods sort*;
- 2-3 puzles casuales de referencia por calidad de ficha (por ejemplo, Royal Match y Woodoku).

Por cada una, apunta:
- **las tres primeras capturas**, que son las que se ven sin desplazar. Qué enseñan, si es gameplay real o ilustración, cuánto ocupa el texto, el tamaño de letra, el tono del texto (beneficio o función) y los colores;
- **el formato**: capturas sueltas o una **panorámica continua** que se lee de izquierda a derecha, marco de teléfono o sin marco, vertical u horizontal;
- **el gráfico de funciones** y si tiene vídeo;
- **los personajes**, el «antes/después» (tablero revuelto → ordenado), la progresión (mapa, mundos) y las pruebas sociales permitidas;
- **la localización**: si cambian el texto y la UI por idioma o solo el texto.

**Fuentes:** las fichas públicas de Play y las guías de Google para Play Console. Las cifras de terceros (blogs de ASO), márcalas como [NV]. Puntos de partida:
- [AppTweak, optimización de capturas](https://www.apptweak.com/en/aso-blog/how-to-optimize-your-app-screenshots);
- [AppFollow, buenas prácticas 2026](https://appfollow.io/blog/aso-screenshots-best-practices);
- [ASO para juegos 2026](https://dev.to/aplusgamesstudio/app-store-optimization-for-games-a-2026-guide-140a);
- [guía de recursos gráficos de Play](https://support.google.com/googleplay/android-developer/answer/9866151).

Lo que ya sabemos de ahí, y que tienes que contrastar:
- en puzles casuales, **el primer fotograma tiene que enseñar el juego en acción**: esconder el gameplay hasta las capturas 3-4 baja la conversión;
- las 2-3 primeras deciden;
- los rótulos funcionan si son **cortos y de beneficio** («Relájate a tu ritmo»), no llamadas a la acción;
- Play prohíbe «#1», «Best», «Top», «New», «Free», premios no concedidos por Play, cifras de descargas y llamadas a la acción («Install now»).

**Entrega:** `docs/store-listing/investigacion-fichas.md`, con una tabla por app (enlace, qué hay en las capturas 1-3, formato y rasgos), **7-10 conclusiones accionables** para Tintalú y lo que **no** podemos copiar. El trade dress de los líderes lo excluyen 07 §1.3 y `assets-tienda.md` §1: gatos, caldero, oro biselado, gemas, cintas rojas, calavera, silueta de frasco con labio y Capi fijo en el centro inferior con un objeto. Guarda capturas de referencia solo en tu equipo, no en el repositorio: son de terceros.

## 2. Fase 2 · Tres conceptos de ficha, en los 3 idiomas

Con lo que salga de la fase 1, haz **tres conceptos**. Cada uno con 8 capturas de teléfono y un gráfico de funciones, en es-419, en y pt-BR:

| Concepto | Idea | Base |
|---|---|---|
| **A · Gameplay héroe** (el primero que entregas) | La mejora directa del juego actual: gameplay real grande y legible desde la captura 1, un rótulo corto de beneficio arriba, y luz, marco y fondo realzados con IA (selva brillante, río al atardecer). Es lo que más se parece a lo que convierte en el género | Capturas reales del juego en cada idioma, con el realce de IA alrededor |
| **B · Panorámica del río** | Las 8 capturas forman **un solo río continuo**: del Estuario a la naciente, con la canoa de Capi avanzando de una a otra. En cada tramo, un recorte de gameplay real. Cuenta el viaje (Mapa del río, nueve mundos, cinco países) | Fondo panorámico generado con IA, con los recortes reales del juego encima |
| **C · Cartel con personajes** | Las capturas 1-2 con TiTino, Capi y Rosa en pose de cartel junto a una cuia que vierte («juguete brillante», D-058). El resto, como A. Busca el gancho emocional que usan los líderes con sus mascotas, sin copiar su trade dress | Renders y poses generados con IA, fieles a los modelos del juego |

**Qué enseñar (lo pidió el dueño):** lo más relevante y lo más bonito del juego, realzado:
- **tableros** vistosos de varios mundos: el Encuentro de las Aguas, la selva, los Andes; con muchas cuias y colores de fruta; en pleno vertido y tiñéndose;
- **los miquitos** (la fauna que celebra y acompaña: `Data/Fauna/miquitos_v0.json`);
- **el Mapa del río**, con la canoa de Capi y el río de vidrio lleno de agua real;
- TiTino, Capi y Rosa;
- y lo más significativo de la app: el Reto del río, la Bitácora, la Liga, los patrones de fruta y las skins de cuia.

**El orden de las 8 capturas**, salvo que la fase 1 diga otra cosa con datos:
1. un tablero vistoso en pleno vertido, con el agua tiñéndose;
2. antes/después o «todo encaja», con los miquitos celebrando;
3. el Mapa del río y el viaje;
4. otro tablero de otro mundo, de dificultad justa (Difícil de verdad, sin trampas);
5. TiTino, Capi, Rosa y los miquitos;
6. patrones para todos los ojos;
7. Reto del río y Bitácora (algo nuevo cada día);
8. sin wifi, a tu ritmo.

**Tomas nuevas en el teléfono (el dueño lo autoriza):** Spark conecta el teléfono de pruebas y hace las tomas que hagan falta, en los 3 idiomas:
- **El build:** compila el `dev.apk` (`pwsh -NoProfile -File scripts/unity-build.ps1 -Method Dev`, veredicto por el log). Instálalo con `adb install -r`.
- **Cómo llegar a cada pantalla:**
  - `adb shell input tap x y`, guiado por las líneas `AF_LAYOUT` y `AF_STATE` de logcat (`docs/dev/prototipo-v0.md`);
  - el progreso con `af_max_level` (como en las pruebas del portátil);
  - el idioma, desde Ajustes del juego.
- **La captura:** `adb exec-out screencap -p > toma.png`, a resolución nativa. Reescálala a 1080 × 1920 al montar, sin deformarla.
- **Barra de estado limpia** con el modo demo de Android:
  - activarlo: `adb shell settings put global sysui_demo_allowed 1`, y luego `adb shell am broadcast -a com.android.systemui.demo -e command enter`, más los comandos `clock` (`-e hhmm 0930`), `battery` y `network`;
  - salir al terminar: `-e command exit`.
- **Lo que no se cambia:** ni la configuración del juego ni el perfil del dueño más allá de lo que hace falta para la toma. Si el teléfono es el del dueño, al terminar déjalo como estaba y apúntalo en `spark-estado.md`.
- **Secretos:** ni tokens, ni UID, ni el código de cuenta en las tomas ni en los registros. Si sale alguno, se tapa o se repite la toma.
- **Si `adb devices` sale vacío,** pide al dueño que reactive la depuración inalámbrica; no insistas.

Los textos de base son los de §6 de cada `ficha.md`. Puedes reescribirlos si la fase 1 lo justifica: máximo unas 5-6 palabras, con la misma idea en los 3 idiomas y calidad nativa.

**El gráfico de funciones (1024 × 500)** es uno por concepto. Lleva el logotipo v2 (`docs/art/marca-oficial/v2/`), sin más texto que el nombre y, como mucho, un lema de 3-4 palabras traducido. **No pongas nada importante en el centro**: si un día hay vídeo, Play pone encima el botón de reproducir.

**Reglas que no se pueden romper:**
- **Medidas:**
  - capturas: 1080 × 1920 px, vertical 9:16, PNG de 24 bits sin transparencia, menos de 8 MB cada una;
  - gráfico de funciones: 1024 × 500 px, PNG de 24 bits sin transparencia.
- **Fidelidad:**
  - la UI, los tableros, los colores de las aguas, los personajes y las pantallas que salgan como juego tienen que ser los del juego, en ese idioma;
  - para el gameplay usa capturas reales: `StoreListingSnapshotTests` (PlayMode, `[Explicit]`) o el teléfono, en cada idioma;
  - la IA no redibuja la UI ni los textos del juego;
  - los personajes generados se parecen a los del juego: usa los renders del juego como referencia.
- **Política de Play:**
  - sin «#1», «Best», «Mejor», «Top», «New», «Free», «Gratis», premios, cifras de descargas ni de valoraciones, precios ni llamadas a la acción;
  - nada que imite la UI de Google Play ni insignias;
  - el nombre «Tintalú», solo en el gráfico de funciones (07 §10).
- **Marca y cultura** (07 §8.1 y `assets-tienda.md` §1):
  - sin personas, ni aldeas o pueblos identificables, ni símbolos étnicos;
  - nada de la marca Amazon: ni la sonrisa o la flecha, ni el naranja #FF9900 sobre negro;
  - sin el trade dress de la fase 1;
  - dirección visual D-003, «Selva brillante premium» (vidrio, madera y luz de juego; sin papel ni acuarela);
  - Baloo 2 o Nunito en los rótulos, con contraste ≥ 4,5:1;
  - «Tintalú» siempre con tilde.
- **Procedencia de la IA:** para cada imagen, un `manifest.json` con:
  - herramienta y modelo, con su versión;
  - prompt y prompt negativo;
  - semilla;
  - imágenes de referencia;
  - fecha.

  Además:
  - usa solo modelos cuya licencia permita el uso comercial de lo generado;
  - sin prompts «al estilo de» una app, una marca o un artista vivo;
  - sin subir al modelo arte de terceros.

**Entrega:**
- en `docs/store-listing/graficos-v2/<A|B|C>/<es-419|en|pt-BR>/01-…08-*.png`, más `feature-graphic.png`;
- `graficos-v2/<concepto>/manifest.json`;
- `graficos-v2/README.md`, con qué es cada concepto, por qué (con las conclusiones de la fase 1) y lo que conviene mirar;
- una **hoja de contactos** `graficos-v2/index.html` (HTML estático sin dependencias externas) con los tres conceptos lado a lado por idioma, para que el dueño elija en el móvil y en el escritorio.

Los PNG van por Git LFS: `.gitattributes` ya lo hace con `*.png`.

## 3. Fase 3 (opcional, después) · Vídeo de la ficha

Si sobra tiempo tras A, B y C: un guion y un montaje de 20-30 s (07 §10.3), con gameplay real y los planos de cartel de C:
- sin música de terceros que no sea CC0 (`docs/store-listing/README.md` §3 bis);
- los primeros 3 s, el vertido;
- entrega en `docs/store-listing/video/`, con su `manifest.json`.

Lo sube el dueño a YouTube, como «no listado» o público y sin anuncios.

## 4. Cómo lo usa el dueño

1. Para la pista interna sube el juego actual (`graficos/`): no espera a esto.
2. Elige en la hoja de contactos el concepto ganador, o una mezcla.
3. Lo sube como ficha principal antes de producción. Con un segundo concepto, prueba una **prueba de ficha de Play Console** (Store listing experiments) tras el lanzamiento, como en la pregunta 3 de 07.
4. El escritorio integra la rama y pone al día `publicar-play-console.md` §0.7 y `graficos/README.md` con lo elegido.

## 5. Estado

Apunta el avance en `spark-estado.md` de tu rama, con lo que entregues de cada fase, las preguntas [P] y las horas de cómputo que haya usado tu stack. Avisa en cuanto estén la fase 1 y el concepto A: el escritorio los integra enseguida para que el dueño los vea.
