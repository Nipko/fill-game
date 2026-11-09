# Encargo para Gemini: revisión 4 (los textos de la 1.1)

Encargo del integrador del 2026-11-09, por el encargo del dueño «Revisión 4 con Gemini» (D-069, M2-2, respuesta a la pregunta 2). Viene después de la revisión 3 (añadido de D-054 del 2026-10-28, `gemini-revision-textos-3.md`).

La 1.1 ha traído **72 filas sin revisar**: 30 de `Assets/_Project/Data/Loc/strings.csv` y 42 de `facts.csv`. Están en `traducido` en inglés y en portugués de Brasil (las tradujo una IA y no las ha revisado un nativo), o en `pendiente_asesor` en español (texto cultural sin validar). Por eso la compuerta `LocReleaseGate` **vuelve a impedir el build de release**: cuenta 52 problemas, que son:
- 1 por el inglés y 1 por el portugués con claves sin `revisado_nativo`;
- 9 textos culturales de `strings.csv`;
- 41 filas de `facts.csv` sin `aprobado_es`.

Es lo único que falta en los textos para el release de la 1.1.

Las filas a revisar están en **`docs/dev/traspaso/gemini-revision-4-entrada/`**. Son copias de las tablas y no se editan:

| Archivo | Filas | Qué es |
|---|---:|---|
| `strings_revision.csv` | 30 | Las filas de `strings.csv` tal como están hoy. Tienen el mismo formato que tu entrega (`key,es-419,en,pt-BR,status_en,status_pt-BR`). |
| `facts_revision.csv` | 42 | Lo mismo, para `facts.csv`. |
| `revision_es.csv` | 50 | La plantilla de los veredictos del español: las 9 filas de `strings.csv` y las 41 de `facts.csv` que están en `pendiente_asesor`. `es_actual` ya va relleno y el resto de columnas, vacías. |
| `filas.csv` | 72 | Datos de cada fila para revisarla: tabla, clave, motivo, si bloquea el release, `max_chars`, estados, `context` y `notes`. En `context` está dónde sale el texto, quién habla, qué es cada marcador y la fuente del dato. Si hay captura, la ruta también. |

## 1. Contexto

El contexto del juego, las lecturas previas y las reglas de forma son los de la revisión 1. Léelos antes de empezar: §1, §2 A y §2 B de `docs/dev/traspaso/gemini-revision-textos.md`. También vale `.claude/rules/localizacion.md`.

Además, para estas filas:
- **Tono y tratamiento:**
  - en español, tú (es-419 neutro, sin voseo);
  - en portugués, «você»;
  - en inglés, «you».
- **Nombres:** salen del glosario (`docs/gdd/00-glosario.md` §6.5 y §10, y `Assets/_Project/Data/Loc/glossary.csv`). Ya revisados y en firme están:
  - «Liga del río» / «River League» / «Liga do Rio»;
  - «Reto del río»;
  - «tucán» (`fauna.toucan`);
  - «tucuxi» (como en `fact.agua_tapajos.15`);
  - los nombres de los países (`map.country.*`) y de los mundos (`world_wNN_*`).
- **Coherencia con lo ya revisado:** usa el mismo léxico que estas filas, que ya están en `revisado_nativo`:
  - `map_world_prize`, «World prize · {} left» / «Prêmio do mundo · faltam {}»;
  - `map_stop_remaining`;
  - `logbook_item_frame`, «+ Country frame» / «+ Moldura do país»;
  - `avatar_home_a11y`;
  - `avatar_new`;
  - `map_arrived`, «{place}{sep}{country}{sep}{river} River» en inglés. Por eso el río de una ficha va sin la palabra «río», «River» ni «rio».

### Bloques

| Bloque | Claves | Dónde sale |
|---|---|---|
| **Estreno de la Liga** (añadido de D-046 del 2026-11-06) | `league_debut_title`, `league_debut_body`, `league_debut_open`, `league_debut_later` y `league_new` | Pantalla completa que sale una vez, al desbloquearse la Liga tras el nivel 41. `league_new` es la píldora «Nueva» de la tarjeta «Liga» del inicio; va en femenino, porque concuerda con «Liga». |
| **Marcos de país del avatar** (Q-M6 de D-033, añadido del 2026-11-08; `docs/dev/avatares.md` §3.2) | `avatar_animals_title`, `avatar_frames_title`, `avatar_frame_none`, `avatar_frame_cell_a11y`, `avatar_frame_chosen_a11y`, `avatar_frame_new_a11y`, `avatar_frame_none_a11y`, `avatar_frame_none_chosen_a11y`, `avatar_home_new_frame_a11y`, `avatar_with_frame_a11y`, `logbook_frame_reveal` y `logbook_frame_view` (12) | Son la fila de marcos de «Tu avatar» y la libreta de la Bitácora. Las claves `*_a11y` solo las lee TalkBack: tienen que ser frases completas y claras. Marcadores: `{world}` = `world_wNN_name`, `{country}` = `world_wNN_country` y `{animal}` = `league_name_*`. |
| **Tutoriales del Aguapé y de la Lancha** (D-068) | `tutorial_aguape`, `tutorial_aguape_n2` y `tutorial_lancha` (en `strings.csv`); `mechanic.aguape`, `mechanic.aguape_n2` y `mechanic.lancha` (en `facts.csv`) | Los tutoriales son la tarjeta previa del estreno: como mucho 2 líneas a 360 dp. Las filas `mechanic.*` son el nombre de la mecánica que lee TalkBack en `a11y_cuia_mechanic`. Reglas del glosario §6.5: el Aguapé es «aguapé» o «mata», nunca «hielo» ni «escarcha». La Lancha «atraca», nunca «llega en canoa», porque las Canoas son las vidas. Tampoco «balsa». **`mechanic.aguape` y `mechanic.aguape_n2` no tienen inglés ni portugués:** tradúcelos tú. El glosario propone «Water Hyacinth» / «Aguapé» y «Thick Water Hyacinth» / «Aguapé denso». |
| **Mapa, M2-2: pueblos de paso** (D-069) | 13 nombres de pueblo (`map.stop.<id>.name`: los 12 nuevos y Porto Velho), 26 ríos de las fichas (`map.stop.<id>.river`) y 4 etiquetas de TalkBack (`a11y_map_town`, `a11y_map_town_place`, `a11y_map_city` y `a11y_map_city_place`) | Los pueblos nuevos son `almeirim`, `prainha`, `manacapuru`, `codajas`, `amatura`, `sao_paulo_de_olivenca`, `indiana`, `orellana`, `humaita`, `manicore`, `cachuela_esperanza`, `guajara_mirim` y `porto_velho`. El nombre sale en el rótulo junto a la viñeta del pueblo, en la ficha («{lugar} · {país} · río {río}») y en TalkBack («Coari · pueblo de paso · Brasil»). Los ríos son los de los 13 pueblos nuevos y los de los 13 de la 1.0 que tienen ficha. La fuente de cada nombre (con URL) está en `context`. Los topónimos van como en su país, que es el uso del juego: «Solimões», «Guajará-Mirim». «Amazonas» es «Amazon» en inglés. |
| **Mapa, M2-4: la tripulación** (D-069) | `map_crew_join_toucan` y `map_crew_join_tucuxi` (un chip de 2 s en una línea, como mucho 32 caracteres) y `a11y_map_crew_join_toucan` y `a11y_map_crew_join_tucuxi` (TalkBack) | Avisan de que un animal se une a la canoa de Capi. Sin nombre propio. |
| **Rótulos de parada del Mapa** (D-069, M2-3 y M2-4, y su añadido del 2026-11-09) | `map_stop_label_remaining` y `map_stop_label_prize` | Rótulo de dos líneas bajo la mini cuia de la parada siguiente: `{place}`, un salto de línea (`\n`, que se conserva) y lo que falta. En `map_stop_label_prize` la parada cierra el mundo: lo que falta para llegar coincide con lo que falta para el Premio del mundo. `{n}` va de 1 a 10. Tiene que leerse igual de bien en voz alta, porque TalkBack lo lee tal cual. |

## 2. Qué revisar

| Parte | Filas | Qué hacer | Estado al terminar |
|---|---:|---|---|
| **A. Español cultural** | Las 50 de `revision_es.csv`: 9 de `strings.csv` (los 3 tutoriales, los 4 de la tripulación y los 2 rótulos de parada) y 41 de `facts.csv` (13 nombres, 26 ríos y los 2 nombres del Aguapé) | Valida, como en la revisión 1, §2 A, que el texto es **correcto, fiel a su fuente y respetuoso**. Los nombres de pueblo y de río se comprueban contra la fuente de `context`, con URL. | `ok`, `cambia` o `duda`, con los límites de §3 |
| **B. Inglés y portugués de Brasil** | Las 72 (30 + 42) | Revísalas como un hablante nativo, con las reglas de la revisión 1, §2 B, y traduce las 4 celdas vacías de `mechanic.aguape` y `mechanic.aguape_n2`. Las reglas son: los mismos marcadores con nombre (`{place}`, `{country}`, `{world}`, `{animal}` y `{n}`), los plurales con las categorías CLDR, una longitud visible de como mucho `max_chars` × 1,3, el `\n` conservado y el tono de la tabla. | `revisado_nativo` |
| **C. Español de la interfaz en `borrador`** | Las otras 21 de `strings.csv` (Liga, marcos y las 4 de TalkBack de los pueblos) | No son culturales y no llevan veredicto. Si ves un error o una frase poco natural, propón el cambio en `resumen.md`. | Siguen en `borrador`; el estado del español no bloquea el release |
| **D. Dato [V] de los pueblos nuevos** | Los 13 pueblos de M2-2 | Lo pide el dueño (D-069, M2-2, pregunta 2). Ver §4, punto 4. | Propuesta; no se aplica en este paso |

## 3. Lo que no te toca (D-054 y D-064)

Por D-054, Gemini sustituye al asesor cultural y a la revisión nativa, pero **el dueño aprueba**. Revisa una muestra, todas las filas en `cambia` y todas las filas en `duda` antes de que nada pase a `aprobado_es`. Por D-064, los nombres culturales de la 1.0 están en firme. En esta revisión:

1. **Los nombres ya en firme no se cambian:** tucán, tucuxi, Liga del río, Reto del río, Capi, Rosa, los países, los mundos y los nombres de los pueblos y paradas de la 1.0. Si crees que alguno está mal, dilo en `resumen.md`. No lo cambies en las traducciones.
2. **Las filas `map.*` no admiten `cambia`.** Las genera `tools/river-map/stops.py` desde la investigación (`docs/research/mapa-del-rio/rio-amazonas.json` y `tools/river-map/overrides.json`) y no se editan a mano: un `cambia` se perdería en la siguiente regeneración.
   - Si un nombre o un río está mal, márcalo `duda` y pon en `motivo` el texto correcto y su fuente. El integrador corrige la fuente, lo regenera y lo trae en una revisión aparte.
   - Sus traducciones sí se revisan aquí.
3. **Los nombres de mecánica en español** (`mechanic.aguape`, `mechanic.aguape_n2` y `mechanic.lancha`: «Aguapé», «Aguapé espeso» y «Lancha») son nombres del glosario §6.5 que decidió el dueño (D-007 y D-068). Puedes darlos `ok` o `duda`, pero no `cambia`: cambiar un nombre es decisión del dueño.
   - En inglés y en portugués sí puedes proponer otro nombre. Si lo haces, cámbialo también en los tutoriales que lo usan (`tutorial_aguape`, `tutorial_aguape_n2` y `tutorial_lancha`) y lístalo aparte en `resumen.md`. El integrador pone al día `glossary.csv` y el glosario §6.5 en el mismo paso.
4. **El español en `borrador`** (parte C) no lleva veredicto: el modo de revisión solo aplica veredictos a filas en `pendiente_asesor`, y un veredicto sobre otra fila es un conflicto. Tus propuestas van en `resumen.md`. En `strings_revision.csv`, su `es-419` es el de la tabla.
5. **El dato [V]** no entra en las tablas con esta entrega. Es una propuesta (§4, punto 4) que el dueño decide. Si la aprueba, un carril de `tools/river-map` la incorpora a la investigación y la regenera.
6. **No toques** las tablas, el glosario, el código, los `.meta` de Unity ni la especificación. Tampoco la carpeta `gemini-revision-4-entrada/`.

## 4. Qué entregar

Entrégalo en la carpeta `docs/dev/traspaso/gemini-revision-4/`, o en una rama `spark/revision-textos-4` creada desde `v11/gemini-revision-4`. Todos los archivos en UTF-8 sin BOM, con fin de línea LF y en formato RFC 4180, con las filas en orden ordinal por clave. Lo más fácil es partir de las copias de `gemini-revision-4-entrada/`.

1. **`revision_es.csv`**: las mismas 50 filas que la plantilla, con la cabecera:
   ```
   key,tabla,es_actual,veredicto,es_propuesto,motivo,fuente_url
   ```
   - `es_actual` va sin cambios.
   - `veredicto` es `ok`, `cambia` (con `es_propuesto`, que no puede estar en una fila `map.*` ni en un nombre de mecánica: ver §3) o `duda` (en `motivo`, qué haría falta para confirmarlo).
   - `fuente_url` es la fuente con la que lo comprobaste.
2. **`strings_revision.csv`** (30 filas) y **`facts_revision.csv`** (42 filas), con la cabecera que lee `tools/loc/apply_translations.py --review`:
   ```
   key,es-419,en,pt-BR,status_en,status_pt-BR
   ```
   - Pon una fila por cada clave de la entrada, aunque la traducción no cambie.
   - `status_en` y `status_pt-BR` van en `revisado_nativo`, sin celdas vacías: `mechanic.aguape` y `mechanic.aguape_n2` también.
   - En `es-419` va el texto de la tabla o, si la fila lleva `cambia` en `revision_es.csv`, el `es_propuesto`. Si cambias el español, vuelve a traducir el inglés y el portugués de esa fila.
   - Una fila `duda` también lleva sus traducciones revisadas. En `facts.csv` no se aplican mientras el español siga en `pendiente_asesor`. En `strings.csv` sí se aplican, pero la fila sigue bloqueando el release hasta que se apruebe su español.
3. **`datos_pueblos.csv`**: el dato [V] de los pueblos nuevos, una fila por pueblo (13), con la cabecera:
   ```
   id,es-419,en,pt-BR,fuente_url,cita
   ```
   - `id` es el del pueblo: `almeirim` … `porto_velho`.
   - El dato tiene que ser **[V]**, es decir, verificado en la fuente: `fuente_url` es una URL que lo dice y `cita` es la frase de la fuente que lo sostiene, de como mucho 25 palabras.
   - Como mucho 90 caracteres en español y 117 en inglés y portugués, como `map.stop.teotonio.dato_01`. Una sola idea, sobre el río, el lugar, su fauna o su flora.
   - Nada de lo que prohíbe la revisión 1, §2 A: pueblos indígenas como tema, lo sagrado o ritual, Parintins (Garantido o Caprichoso), un animal presentado como peligroso o un consejo de comer o tocar fauna. Tampoco cifras de población, que caducan.
   - Si un pueblo no tiene un dato que se pueda verificar, deja sus textos vacíos y explica en `cita` por qué. Es mejor un pueblo sin dato que un dato sin fuente.
   - El modo de revisión **no lee este archivo**.
4. **`resumen.md`**, con:
   - cuántas filas `ok`, `cambia` y `duda` hay en la parte A, y cuántas celdas cambian en la parte B, por idioma;
   - cada cambio, con su motivo;
   - los cambios de español que propones para la parte C y los de nombres de mecánica en inglés y portugués (§3, punto 3);
   - las filas `duda` de `map.*`, con el texto correcto y su fuente;
   - las dudas.

## 5. Cómo se comprueba y quién integra

- **El integrador** valida la entrega sin escribir nada: `py -3.14 tools/loc/apply_translations.py --review docs/dev/traspaso/gemini-revision-4 --check`. Con la entrega completa y sin conflictos, ese comando dice que aplicarla cambiaría 194 celdas o estados (50 veredictos y 72 filas × 2 idiomas) y sale con el código 1, que aquí no es un error.
- **El dueño** aprueba la entrega (D-054). Después el integrador la aplica con `--review` y comprueba que `--check` sale con 0, que pasan `LocTablesTests` y que `LocReleaseGate` queda en 0 problemas, o solo con las filas en `duda`, que no salen en release. Luego la prueba en el teléfono en los tres idiomas.
- **El dato [V]** de `datos_pueblos.csv`, si el dueño lo aprueba, lo incorpora un carril de `tools/river-map` en una tarea aparte.
