# Tintalú: encargo de música nueva (v2), «el río Amazonas»

2026-10-26 · Encargo para un agente de Gemini (o para una persona) que produzca la música. Lo pide el dueño, con sus palabras: «crear otra melodía un poco más tipo río Amazonas, la que suena de fondo, dado que la actual se parece mucho a la alarma predeterminada de los Samsung, suena a despertador».

> **En espera (2026-10-26):** la fase A de Gemini (rama `spark/musica-v2`, `68d88eec`; pasa el comprobador, 6 de 6) **no convenció al dueño** tras escucharla, y él la deja en espera. No se integra; el juego sigue con la música de la entrega 1. No lanzar la fase B hasta nueva decisión.

Este documento **sustituye solo la dirección musical** (el carácter y el estilo) del lecho de cada mundo y del tema de menú. Todo lo técnico sigue igual y manda:
- `docs/audio/encargo-sonidos.md`: §4 (formato), §5 (reglas para el teléfono), §8 (estructura de la música y lo prohibido) y §9 (licencias y manifiesto);
- `docs/audio/especificacion-sonidos.json`: nombres de archivo, duraciones y sonoridad de cada pieza;
- `tools/audio-check/check_audio.py`: el comprobador. **Una entrega solo se acepta si pasa sin ningún FALLO** y la escucha del dueño en el teléfono.

Si el campo `style` de `especificacion-sonidos.json` o el §8 de `encargo-sonidos.md` dicen otro estilo para el lecho o el menú, manda este documento.

## 1. Qué falla en la música actual

La entrega 1 (2026-10-17, `Assets/_Project/Audio/music/`, VSCO-2-CE) se basa en **arpegios de marimba y arpa en Do mayor pentatónico, en registro agudo y repetidos**. En el altavoz del teléfono, ese motivo brillante que sube y baja suena como el tono de alarma por defecto de Samsung («Over the Horizon») y, en general, como un tono de llamada o un despertador. Por eso el jugador lo asocia a «despertarse», no a «estar en el río».

## 2. Qué tiene que transmitir ahora

**El río Amazonas: ancho, lento, inmenso y en calma.** El jugador va río arriba en la canoa de Capi, entre selva, al atardecer. La música tiene que sentirse como **agua que fluye y aire húmedo**, no como un juguete. Sigue siendo la dirección «Selva brillante premium» (D-003): cálida, limpia y con espacio, nunca oscura ni triste.

- **Movimiento:** largo y ondulante, como la corriente. Frases de 4-8 compases que respiran, con notas largas y ligadas. Tempo lento: 60-76 BPM, o libre en el lecho.
- **Armonía:** modal y cálida (dórico, eólico o mixolidio sobre una nota pedal), con acordes abiertos y que cambian despacio (uno cada 2-4 compases). Nada de Do mayor pentatónico brillante en arpegios.
- **Instrumentos propuestos** (reales o muestreados de calidad):
  - colchón grave y cálido: cuerdas suaves en registro medio-grave, pad orgánico de órgano de lengüetas o de coro sin letra muy bajo;
  - melodía: **flauta de madera o de caña** suave (no quena ni zampoña: están prohibidas, §8 de `encargo-sonidos.md`), clarinete o violonchelo; guitarra acústica con dedos, cuatro o charango solo en el mundo que lo pide;
  - textura de agua: palo de lluvia muy suave, gotas afinadas espaciadas, cuencos de agua, maracá lenta;
  - percusión (solo en las capas `perc1..3`): tambor de marco grave y lejano, cajón suave con escobilla, curimbó apagado, semillas.
- **La marimba, el arpa o la celesta pueden aparecer** como color en el registro medio y en notas sueltas, **nunca como un ostinato o un arpegio repetido**.

## 3. Prohibido (además de lo de §8 de `encargo-sonidos.md`)

- Cualquier cosa que recuerde a un tono de llamada, una alarma o una notificación:
  - arpegios rápidos y repetidos en registro agudo (por encima de Do5) con marimba, xilófono, kalimba, celesta, campanas, glockenspiel o arpa;
  - un motivo corto de 4-8 notas que se repite igual más de dos veces seguidas;
  - escalas pentatónicas mayores brillantes que suben y bajan;
  - ataques secos y repetidos en la misma nota.
- **Referencias que NO se pueden parecer:** «Over the Horizon» (Samsung), los tonos por defecto de Samsung, Pixel e iPhone («Opening», «Radar») y las alarmas de reloj.
- Melodías pegadizas tipo jingle en el lecho: el lecho acompaña sin pedir atención.

**Comprobación de quien entrega, antes de mandar:** escuchar cada pieza en el altavoz de un teléfono a volumen medio, y responder en el manifiesto (campo `notes`): «¿alguien lo confundiría con un tono de llamada o una alarma?». La respuesta tiene que ser «no».

## 4. Qué entregar, por fases

Mismos nombres, formatos, duraciones, sonoridad y reglas de bucle que la entrega 1 (ver `especificacion-sonidos.json`). En particular, **cada capa `perc1..3` mide exactamente lo mismo que su lecho, muestra a muestra, y empieza en el mismo tiempo de compás**, y la variante nocturna es el mismo lecho, más grave y oscuro.

**Fase A, para aprobar la dirección (primero esto, y esperar la respuesta del dueño):**

| Archivo | Qué es | Dirección |
|---|---|---|
| `music/menu.wav` | Tema del inicio («el muelle del río», D-050), el Mercado y Sin canoas; bucle de 60-120 s a −20 LUFS | **El atardecer en el muelle**: guitarra acústica con dedos y flauta de madera sobre un colchón cálido y el agua del río; un pulso suave de maracá y tambor de marco lejano (el menú sí puede tener pulso, sin ser bailable). Tempo 70-80 BPM. Invita a quedarse y a jugar |
| `music/w01_bed.wav` | Lecho del mundo 1, Estuario (1-40); bucle de 60-90 s a −26 LUFS, sin pulso marcado | **El estuario inmenso**: colchón de cuerdas y órgano de lengüetas, flauta de madera en frases largas, palo de lluvia y gotas afinadas; el carimbó del mundo solo como sabor en las capas |
| `music/w01_bed_night.wav` | Variante nocturna del lecho 1, a −27 LUFS | El mismo lecho, más grave y oscuro, con grillos lejanos tocando en la armonía |
| `music/w01_perc1.wav`, `perc2`, `perc3` | Capas que se suman al 25, 50 y 75 % de cuias selladas | Curimbó apagado y maracá (1), tambor de marco grave (2) y semillas y escobilla (3), en el tempo del lecho |

**Fase B, tras el visto bueno del dueño a la fase A:** los lechos, las variantes nocturnas y las capas de los mundos 2-9 (`w02` … `w09`). Cada mundo mantiene su color regional del campo `style` de la ficha (carimbó de playa en el Tapajós, toada propia en Manaos, guitarra y flauta de caña en el Trapecio, cumbia amazónica suave en Loreto, semillas y cuencos en Oriente, guitarra y cajón en Madre de Dios, charango y bombo en Madidi, cumbia o chicha suave en Ucayali), **pero con la dirección de este documento**: lento, ancho, que fluye, sin arpegios de tono de llamada.

**Los 5 remates** (`music/stingers/`) no cambian, salvo que en la fase A se note que chocan con la nueva armonía; en ese caso, dilo.

## 5. Licencias

Las de §9 de `encargo-sonidos.md`. Si se genera con IA (por ejemplo, Lyria), la licencia es `ia-uso-comercial`, con el nombre y la versión de la herramienta en `source`, y solo si sus términos permiten el uso comercial sin reclamar derechos sobre la salida. Prohibido samplear grabaciones de música existentes.

## 6. Cómo se entrega y quién integra

- Entrega los WAV en las rutas de §4, relativas a `Assets/_Project/Audio/`, más las filas nuevas del manifiesto `audio_manifest.json`, en una carpeta o una rama `spark/musica-v2` según acuerde el dueño.
- **No crees `.meta` de Unity ni toques código.** La integración la hace el escritorio: comprobador, conversión a 16 bits (`tools/audio-check/to16bit.py`), importación y prueba en el teléfono.
- Pasa el comprobador antes de entregar y adjunta su salida:
  ```
  python tools/audio-check/check_audio.py --root <carpeta de la entrega> --only music --allow-missing
  ```
  (`--allow-missing` solo en la fase A, que no trae los mundos 2-9.)
