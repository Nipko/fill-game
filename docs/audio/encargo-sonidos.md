# Tintalú: encargo de sonidos y música definitivos

2026-10-17 · Encargo para quien produzca el audio (un agente de IA o una persona): qué entregar, con qué calidad y cómo se comprueba. Lo acompañan dos archivos que mandan sobre los números:

- `docs/audio/especificacion-sonidos.json`: la ficha de cada pieza (173), con su archivo, cuándo suena, cómo debe sonar, duración, nivel y reglas.
- `tools/audio-check/check_audio.py`: el comprobador que mide cada WAV entregado contra esa ficha. **Una entrega solo se acepta si pasa el comprobador sin ningún FALLO** y la escucha en el teléfono.

Si este texto y el GDD no coinciden, manda el GDD (`docs/gdd/07-arte-tematica-audio.md` §9 y `docs/gdd/02-nucleo-reglas-ux.md` §4).

## 1. Qué es el juego y qué tiene que sentir quien juega

Tintalú es un puzzle de ordenar agua en el móvil, con la Amazonía como tema: el jugador vierte agua teñida de fruta entre cuias (recipientes de vidrio y calabaza) hasta que cada una tenga un solo color. Se juega en sesiones largas, a menudo con el volumen bajo o sin auriculares, en teléfonos Android de gama media de América Latina.

**La dirección:** «Selva brillante premium» (D-003): vidrio, madera y luz de juego. Para el sonido, un **ASMR amazónico fresco y limpio**: agua cristalina en vidrio, madera de chonta, calabaza, maracá, fibra y semillas. Tiene que dar calma y satisfacción, como un juego de primera línea del género (la referencia de dinámica es *Magic Sort!*: gestos repetidos muy suaves y premios con brillo).

**Lo que no puede pasar nunca:** que un sonido canse, moleste o inquiete. Un efecto que se oye 300 veces por sesión tiene que ser casi un susurro agradable.

## 2. Por qué lo que hay hoy no sirve (lo que hay que evitar)

Hoy el juego genera el audio por código a partir de `docs/audio/laboratorio-sonidos.html`. Es un sustituto provisional y el dueño lo ha escuchado en el teléfono:

- **El fondo suena perturbador.** El «agua» es ruido blanco filtrado en banda y sin variación: suena a siseo, a estática de radio o a un zumbido, no a agua. La lluvia es el mismo ruido en agudo.
- **La música suena apagada y a dron.** El lecho es de acordes de onda pura generados a 11 kHz (sin brillo), que se funden cada 10 s; las capas de percusión repiten un golpe cada 1,05 s.
- **Los efectos suenan a electrónica suave**, no a materiales reales: todo está hecho con senos y con muy poca energía por encima de 4 kHz.

**Por eso el encargo pide audio de calidad final, no código de síntesis:** grabaciones reales (agua, lluvia, fauna, maderas, semillas), instrumentos reales o muestreados de calidad, y diseño de sonido producido y masterizado. Se puede usar síntesis como capa (por ejemplo, un brillo tonal afinado en un efecto), nunca como el cuerpo de un sonido natural ni de un fondo.

## 3. Qué entregar

**173 archivos WAV + un manifiesto de licencias**, en esta estructura (relativa a `Assets/_Project/Audio/`):

| Carpeta | Contenido | Cuántos |
| --- | --- | --- |
| `sfx/` | Los 61 efectos, `sfx/<id>.wav` | 61 |
| `sfx/pour/` | El chorro del vertido en 4 bucles, por nivel de llenado de la cuia de destino | 4 |
| `sfx/fruit/` | `sfx_select` y `sfx_tint` afinados a la nota de cada una de las 14 frutas | 28 |
| `amb/` | El agua de los mundos 1-9 (`w01_water` … `w09_water`) y la lluvia en 3 intensidades (`rain_light`, `rain_medium`, `rain_heavy`) | 12 |
| `amb/fauna/` | Animales sueltos de cada mundo (`wNN_fauna_KK`) | 17 |
| `music/` | Por mundo 1-9: el lecho, sus 3 capas de percusión y la variante nocturna (`wNN_bed`, `wNN_perc1..3`, `wNN_bed_night`); y el tema de menú (`menu`) | 46 |
| `music/stingers/` | 5 remates musicales cortos | 5 |
| `audio_manifest.json` | La procedencia y la licencia de cada archivo (§9) | 1 |

El nombre de cada archivo y todos sus números están en `especificacion-sonidos.json` (campos `id` y `file`). **No crees archivos `.meta` de Unity ni toques código:** la integración en el juego la hacemos nosotros (tarea AU3, §11).

Se puede entregar por partes, en este orden de prioridad (§10): primero lo que más se oye.

## 4. Formato técnico (todas las piezas)

| Regla | Valor |
| --- | --- |
| Formato | WAV PCM, **48.000 Hz, 24 bits**, sin compresión |
| Canales | **Mono** en efectos, notas de fruta y fauna; estéreo permitido en agua, lluvia y música |
| Pico máximo absoluto | −1 dBFS en cualquier pieza (sin recortes ni saturación) |
| Pico de cada efecto | El de su ficha (`peak_dbfs`), ± 1,5 dB: es la mezcla del juego (§5) |
| Sonoridad de fondos y música | La de su ficha (`lufs`, LUFS integrados según ITU-R BS.1770), ± 2 LU |
| Inicio | El sonido empieza en los primeros 10 ms (sin silencio delante) y sin clic: ataque de 1-5 ms |
| Final de los efectos | Termina en silencio: los últimos 5 ms por debajo de −60 dBFS (fundido natural, nunca un corte) |
| Desplazamiento de continua | Menor de 0,002 |
| Bucles | Sin costura: la última muestra enlaza con la primera (salto ≤ 0,02) y el nivel no cambia en la unión; la costura no se oye ni con auriculares |
| Capas de música | Cada capa de percusión mide exactamente lo mismo que su lecho, **muestra a muestra**, y empieza en el mismo tiempo de compás |

## 5. Reglas para que suene bien en un teléfono

- **El altavoz del teléfono casi no reproduce por debajo de 300 Hz.** En cada efecto, nota y animal, **al menos la mitad de la energía tiene que estar por encima de 300 Hz** (el comprobador lo mide). Lo grave (un «toc» de barro, una canoa) se consigue con los armónicos y el cuerpo del sonido, no con subgraves.
- **Filtro paso alto a 80-100 Hz** en todo: lo de debajo solo gasta energía.
- **Nada áspero entre 2 y 5 kHz en los gestos repetidos** (seleccionar, verter, tocar botones): es la zona que cansa el oído.
- **Jerarquía de la mezcla** (el pico de cada efecto ya la fija; esto explica el porqué):

| Familia | Pico (dBFS) | Ejemplos |
| --- | --- | --- |
| Gestos repetidos, muy suaves y cortos (≤ 0,2 s) | −36 a −26 | seleccionar, teñir, gota de fruta, botones, tic de semillas, inicio y fin del vertido |
| Acciones y comodines | −22 a −18 | Marea, Morpho, Pororoca, Luciérnaga, desbloqueos de mecánicas |
| Premios y momentos | −17 a −14 | sellar una cuia, toada de victoria, Canasta, piha del banner |
| Fondos (siempre por debajo de todo) | −34 a −40 LUFS | agua y lluvia |
| Música | −26 LUFS el lecho solo y −20 LUFS con todas sus capas | lechos y menú |

## 6. Los efectos (65 + 28 notas)

La ficha de cada uno en el JSON trae `when` (cuándo suena), `gdd` (lo que pide el GDD), `direction` (la propuesta acústica del dueño, que manda en el carácter), `hook_today` (dónde lo dispara hoy el juego) y sus números. Lo que no está en la ficha y conviene saber:

- **Materiales:** agua en vidrio (gotas, chorros, burbujas que se aclaran), madera de chonta y de canoa, calabaza hueca, maracá de semillas, fibra de arumã, hojas de palma, semillas en cesta. Nada de clics electrónicos, láseres, «error» de ordenador, voces habladas ni sonidos de castigo: el vertido no permitido es un toque de madera neutro.
- **`sfx_select` es el sonido más importante del juego:** suena en cada toque. Una gota redonda y una nota de marimba afinada, 0,12 s, a −36 dBFS. Las 14 versiones de `sfx/fruit/` llevan la nota de su fruta (escala pentatónica mayor de Do4 a Sol6, ficha `fruit_notes`, con su frecuencia exacta y ± 15 centésimas): así cualquier combinación de frutas suena consonante. `sfx_tint` es la misma idea a −30 dBFS, para la intro del nivel.
- **El vertido (`sfx_pour_start`, los 4 bucles de `sfx/pour/` y `sfx_pour_end`):** un vertido de una dosis dura unos 0,97 s y el chorro, unos 0,3-0,9 s según las dosis (02 §4). El juego encadena inicio, bucle (mientras cae) y fin. Los 4 bucles son el mismo chorro en el mismo vidrio con la cuia de destino cada vez más llena: la cavidad resuena más aguda y corta. El juego funde entre ellos y afina como mucho ± 2 semitonos, así que **no le pongas una subida de tono propia**. El `sfx_pour_loop` sin nivel es el de respaldo (nivel medio).
- **El sellado (`sfx_seal`, −17 dBFS):** suena al empezar la tapa (la tapa cae 150 ms y rebota 100 ms; total 450 ms), con una vibración ligera. Es el premio táctil del tablero: dos sacudidas de maracá, el «plop» hueco de la tapa de calabaza y un brillo corto afinado.
- **La victoria:** a los 0,50 s del último sellado entran la toada (`sfx_win_toada`, 2-3 s: carimbó corto con percusión, maracás y coro sin letra) y el confeti de plumas (`sfx_confetti`, aleteo de guacamayos); a los 0,80 s sube el panel y las semillas cuentan hacia arriba (`sfx_seeds_tick`, un solo tic que el juego repite hasta 20 veces).
- **Personajes:** ninguno habla. TiTino (perezoso) solo hace sonidos suaves (un «mmm» grave al asentir, un suspiro feliz, el roce de su rama, un bostezo); Capi (capibara) un chasquido simpático; Rosa (delfín rosado) su exhalación; los miquitos de la intro, como mucho un chillido corto y muy bajo.
- **La piha** (el ave del grito «pi-pi-YO») es la firma sonora del juego, pero **solo suena en el banner de mundo nuevo** (`sfx_world_banner`), nunca en bucle ni en el ambiente cercano.
- **La Tinaja:** «toc» grave de barro, «glup» de calabaza al sacar agua, una gota al revelar y roce de fibra al soltar. Nunca burbujas ni hervor.
- **Los 11 efectos que antes estaban «sin definir»** (Liana de cuenta, Lluvia, Piraña, Brote, Flor, Hoja atada, peque-peque, Fruta nueva, tarjeta de mecánica, Morpho por inactividad, tienda) ya tienen propuesta en `direction`; respétala.

## 7. El ambiente de cada mundo (agua, lluvia y fauna)

Es lo que hoy suena perturbador, así que es lo que más cuidado necesita.

- **Agua** (`amb/wNN_water`, un bucle de 60-120 s por mundo, −34 LUFS): **grabaciones reales de agua de río**, del carácter de cada mundo (ficha `ambient`, campo `character`): río ancho y lento con marea en el Estuario, olas suaves de playa en el Tapajós, dos corrientes en Manaos, río tras la lluvia con goteo en el Trapecio, agua quieta de espejo en Loreto, laguna negra de noche en Oriente, río medio en Madre de Dios, cascada lejana en Madidi y vapor muy suave con río en Ucayali. Tiene que oírse como un sitio tranquilo y vivo, con pequeñas variaciones naturales, nunca como un ruido continuo.
- **Lluvia** (`amb/rain_light`, `rain_medium`, `rain_heavy`; bucles de 20-60 s, −40, −37 y −34 LUFS): lluvia real sobre hojas y agua, sin trueno. El juego la suma al agua en los niveles con lluvia (la probabilidad por mundo está en la ficha).
- **Fauna** (`amb/fauna/wNN_fauna_KK`, 0,4-4 s, pico −20 dBFS, mono): llamadas sueltas de los animales de cada mundo (garzas, tucanes, botos, guacamayos, pericos, grillos, pirarucú, ranas, búho, nutrias, aves de altura, paiche). El juego las suelta al azar cada 20-45 s, a −10 dB bajo su pico. Grabaciones de la especie o, si no hay con licencia válida, de una especie parecida de la región; dilo en el manifiesto.
- **Prohibido en los fondos** (el comprobador lo detecta):
  - ruido blanco o rosa sintético como cuerpo del sonido;
  - zumbidos o tonos sostenidos (ningún pico tonal más de 15 dB sobre su entorno);
  - siseo: como mucho el 15 % de la energía por encima de 8 kHz;
  - pulsaciones o «respiración» regular del nivel;
  - voces humanas inteligibles, motores (salvo el peque-peque del Mapa, que es un efecto) y bocinas.

## 8. La música

- **Lecho de cada mundo** (`music/wNN_bed`, bucle de 60-90 s, −26 LUFS): música tranquila **sin pulso marcado** (sin golpes regulares; el comprobador mide la regularidad del envolvente), con el instrumento o el género del mundo (ficha `music`, campo `style`): carimbó (curimbó y maracás) en el Estuario; carimbó de playa con guitarra en el Tapajós; toada amazónica propia en Manaos; guitarra acústica y flauta de caña en el Trapecio; cumbia amazónica suave en Loreto; percusión de semillas y cuencos de agua en Oriente; guitarra y cajón ligero en Madre de Dios; charango suave y bombo en Madidi; cumbia amazónica o chicha con Farfisa y guitarra con wah-wah en Ucayali. Grabado o muestreado con calidad, con brillo y espacio, nunca acordes de onda pura.
- **Capas de percusión** (`music/wNN_perc1..3`): percusión suave del estilo del mundo, en el tempo y la tonalidad del lecho, que **se suma** a él: la capa 1 entra al sellar el 25 % de las cuias, la 2 al 50 % y la 3 al 75 % (el juego usa 2 por defecto; Remote Config puede subir a 3). Suben el volumen y la energía, nunca el tempo. Las cuatro piezas de un mundo (lecho + 3 capas) suenan juntas a −20 LUFS.
- **Variante nocturna** (`music/wNN_bed_night`, −27 LUFS): el mismo lecho más grave y oscuro, para los niveles Muy difíciles. Nunca música tensa ni de reloj.
- **Tema de menú** (`music/menu`, bucle de 60-120 s, −20 LUFS): cumbia amazónica ligera a 90-100 BPM, con Farfisa y guitarra con wah-wah suave. Para el inicio, el Mercado y la pantalla de la Crecida.
- **Remates** (`music/stingers/`, 1-2,5 s, pico −16 dBFS): banner de mundo (bajo la piha), llegada a una parada, «Fruta nueva», tarjeta de mecánica y Nivel fallido (sin dramatismo).
- **Prohibido en la música** (07 §9.3): ícaros, flautas de Yuruparí, cantos rituales, quena o zampoña (salvo el mundo 13, que no entra en la 1.0), y melodías o colores de Garantido y Caprichoso (Parintins). Ninguna voz hablada; coros sin letra, solo en la toada.

## 9. Licencias y manifiesto

**Todo el audio tiene que poder usarse en un juego comercial, en anuncios y en tráilers, para siempre y en todo el mundo, sin atribución en pantalla.** Licencias admitidas (campo `license` del manifiesto):

| `license` | Qué es |
| --- | --- |
| `propia` | Grabado, compuesto o producido por quien entrega, con cesión total al dueño |
| `cc0` | Dominio público (Creative Commons Zero), con el enlace a la página de origen |
| `comercial-con-cesion` | Biblioteca o servicio de pago con licencia perpetua para uso comercial; el dueño tiene que tener la factura |
| `ia-uso-comercial` | Generado con una herramienta de IA cuyos términos permiten el uso comercial y no reclaman derechos sobre la salida; con el nombre y la versión de la herramienta |

**Prohibido:** CC BY-NC, CC BY-SA, CC BY (exige atribución), «solo uso personal», licencias desconocidas, y samplear grabaciones de música existentes (carimbó, toadas o chicha grabados tienen derechos de autor e intérprete). Ante la duda, pregunta al dueño antes de usarlo.

El manifiesto (`Assets/_Project/Audio/audio_manifest.json`) lleva una fila por archivo:

```json
{
  "version": 1,
  "files": [
    {
      "id": "sfx_seal",
      "file": "sfx/sfx_seal.wav",
      "license": "propia",
      "source": "Maracá y tapa de calabaza grabadas a 48 kHz; capa tonal sintetizada",
      "author": "nombre de quien lo hizo",
      "url": "",
      "notes": ""
    }
  ]
}
```

## 10. Orden de entrega

1. **Lo que más se oye:** `sfx_select` y `sfx_tint` con sus 28 notas de fruta, el vertido (inicio, 4 bucles y fin), `sfx_illegal`, `sfx_seal`, la intro (`sfx_river_rise`, `sfx_fill`, `sfx_fruit_plip`), la victoria (`sfx_win_toada`, `sfx_confetti`, `sfx_seeds_tick`) y la interfaz (`sfx_ui_tap`, `sfx_ui_back`, `sfx_ui_buy`).
2. **El ambiente** de los mundos 1-5 y la lluvia: es lo que el dueño nota hoy.
3. Comodines, mecánicas, personajes, Mapa y pantallas.
4. La música de los mundos 1-5, el menú y los remates.
5. El ambiente y la música de los mundos 6-9.

Cada parte se entrega con su manifiesto al día y se comprueba sola con `--allow-missing`.

## 11. Cómo se comprueba y qué hacemos nosotros

**Comprobación automática** (Python 3.14, solo biblioteca estándar), desde la raíz del repositorio:

```bash
py -3.14 tools/audio-check/check_audio.py --root Assets/_Project/Audio --allow-missing
```

Mide formato, duración, pico, continua, silencio inicial, fundido final, energía por encima de 300 Hz, costura de los bucles, LUFS, afinación de las notas, longitud de las capas, pulso en los lechos, zumbidos y siseo en los fondos, y el manifiesto. Sale con código 0 si no hay ningún FALLO. `--self-test` comprueba la propia herramienta. Sin `--allow-missing`, cada archivo que falte es un FALLO.

**Escucha en el teléfono:** el dueño instala cada entrega en el teléfono de pruebas y juega al menos 20 minutos con el volumen a la mitad. Una pieza se rechaza si cansa, molesta, se oye por encima de lo que debe o no se oye.

**Integración (la hacemos nosotros, tarea AU3):** el juego carga los WAV de `Assets/_Project/Audio/` como clips (comprimidos al construir el APK: música en Vorbis por streaming y efectos en mono), con los 4 bucles del vertido fundidos por nivel, y conserva el audio por código solo como respaldo de lo que falte. Quien entrega no toca código ni archivos de Unity.
