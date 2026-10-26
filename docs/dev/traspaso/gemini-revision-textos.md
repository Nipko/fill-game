# Encargo para Gemini: revisión de los textos del juego (español, inglés y portugués de Brasil)

2026-10-26 · Encargo del integrador por decisión del dueño (D-054): «Pon el encargo a Gemini, que es mejor en idiomas». Es lo único que hoy **impide sacar un build de release**: la compuerta `LocReleaseGate` (`Assets/_Project/Editor/CI/LocReleaseGate.cs`) cuenta **1.087 problemas de textos**.

## 1. Qué es el juego (contexto para revisar bien)

Tintalú es un puzle de ordenar agua para Android, con la Amazonía como tema. El público es América Latina primero (Colombia, Perú, México y Brasil). El jugador vierte agua teñida de fruta entre cuias (recipientes de vidrio y calabaza) y sube el río Amazonas en la canoa de Capi (una capibara), con TiTino (un perezoso de tres dedos) como guía. Cada victoria enseña un **Dato del río** (frutas, fauna y lugares reales) y el **Mapa del río** recorre 100 paradas reales de la cuenca. El tono es **cálido, breve, claro y respetuoso**: es un juego para todos los públicos, sin ironía y sin infantilizar.

Lee antes:
- `docs/gdd/00-glosario.md` (§3 y §8-§11: nombres de fauna, flora, lugares, frutas y comodines, y las claves de UI) y `Assets/_Project/Data/Loc/glossary.csv`;
- `.claude/rules/localizacion.md` (reglas de los textos);
- `docs/research/idiomas/propuesta-idiomas.md` §3.2-§3.4 y §6.1;
- `docs/gdd/07-arte-tematica-audio.md` §1 (lo que está prohibido por respeto cultural).

## 2. Qué hay que revisar

Las tablas son `Assets/_Project/Data/Loc/strings.csv` (542 filas, UI) y `facts.csv` (822 filas, textos largos). Columnas: `key, es-419, en, pt-BR, context, max_chars, status, status_en, status_pt-BR, notes, args`.

| Parte | Filas | Qué falta | Estado al terminar |
|---|---|---|---|
| **A. Español cultural** | 263 de `strings.csv` y 820 de `facts.csv` en `pendiente_asesor` | Validar que cada texto con fruta, fauna, flora, lugar o pueblo es **correcto, fiel a su fuente y respetuoso** | `aprobado_es` (o corregido y luego `aprobado_es`) |
| **B. Inglés y portugués de Brasil** | Las 542 de `strings.csv` y las 822 de `facts.csv` | Revisar las traducciones de IA (hoy `traducido`) como lo haría un hablante nativo, y **traducir las 364 filas de `facts.csv` que están vacías** | `revisado_nativo` |
| **C. Preguntas abiertas** | T2-Q8 a T2-Q11 (`docs/dev/traspaso/spark-estado.md`, fila 1253 en adelante) | Responder con una recomendación y su motivo | — |

Las 206 filas en `borrador` y las 73 en `aprobado_es` de `strings.csv` no son culturales: en la parte A solo se miran si ves un error claro (dilo en `notas`).

### A. Español cultural: cómo revisar
Para cada fila:
- **Exactitud:** las afirmaciones y las cifras coinciden con la fuente citada en `context` o `notes` (URL). Si la fuente no lo dice o lo contradice, corrige el texto o márcalo `duda`.
- **Nombres:** los del glosario, siempre. El nombre científico va tal cual, en cursiva solo si la tabla ya lo marca.
- **Respeto:** nada de estereotipos sobre pueblos indígenas o ribereños, nada sagrado ni ritual (ícaros, Yuruparí, ayahuasca), nada de Parintins (Garantido o Caprichoso), nada que presente a un animal como peligroso sin motivo y ningún consejo de comer o tocar fauna silvestre.
- **Forma:** una sola idea, ≤ `max_chars` caracteres, español neutro de América Latina (sin voseo ni modismos de un solo país), con los mismos marcadores con nombre (`{n}`, `{name}`…), la misma sintaxis de plural (`{n:plural:…|…}`) y U+00A0 entre la cifra y su unidad.
- Si cambias el español de una fila que ya tenía inglés o portugués, **vuelve a traducir esos idiomas** en la parte B (08 §4.7: el cambio del español invalida las traducciones).

### B. Inglés y portugués de Brasil: cómo revisar
- Que suene natural para un jugador nativo de EE. UU. o de Brasil, en el mismo tono cálido y breve; no una traducción literal.
- Los mismos marcadores, plurales con las categorías CLDR de cada idioma (`{n:plural:…|…}`), la longitud visible ≤ `max_chars` × 1,3 y el U+00A0 (salvo antes de «%»).
- Nombres del glosario en cada idioma. Los nombres locales de fruta se conservan (açaí, cupuaçu, taperebá), como en la tabla.
- Las 364 filas vacías de `facts.csv` (sobre todo frutas y fauna): tradúcelas con el mismo cuidado.
- Sin mayúsculas forzadas: si una etiqueta va en mayúsculas, la tabla ya lo dice.

## 3. Qué entregar

En una rama `spark/revision-textos` (desde `integracion-fase1-fase2`) o en la carpeta `docs/dev/traspaso/gemini-revision/`, **sin tocar las tablas ni el código**. Todo en UTF-8 sin BOM, fin de línea LF, RFC 4180 y en orden ordinal por clave:

1. **`revision_es.csv`**, una fila por cada fila revisada en la parte A:
   ```
   key,tabla,es_actual,veredicto,es_propuesto,motivo,fuente_url
   ```
   - `tabla`: `strings` o `facts`;
   - `veredicto`: `ok` (se aprueba tal cual), `cambia` (con `es_propuesto`) o `duda` (no se puede confirmar; explica en `motivo` qué haría falta);
   - `fuente_url`: la fuente con la que comprobaste el dato.
2. **`strings_revision.csv` y `facts_revision.csv`**, con el formato que ya lee `tools/loc/apply_translations.py`:
   ```
   key,es-419,en,pt-BR,status_en,status_pt-BR
   ```
   - `es-419`: el texto español sobre el que traduces (el propuesto si cambia en la parte A);
   - `status_en` y `status_pt-BR`: `revisado_nativo`;
   - una fila por cada clave de las dos tablas, aunque la traducción no cambie (así queda constancia de que se revisó).
3. **`preguntas_t2.md`**: la respuesta a T2-Q8 a T2-Q11 con su recomendación.
4. **`resumen.md`**:
   - cuántas filas `ok`, `cambia` y `duda` hay en la parte A;
   - las 20 correcciones más importantes;
   - lo que no se pudo confirmar.

Se puede entregar por partes, en este orden: la UI de `strings.csv`, porque es lo que más se ve; después el Mapa (`map.*`); y por último frutas y fauna.

## 4. Cómo se comprueba y quién integra

- El escritorio amplía `tools/loc/apply_translations.py` con un modo de revisión, que es el único que puede sustituir una traducción `traducido` por la revisada y aplicar los veredictos de `revision_es.csv`. Lo aplica, pasa `--check`, `LocTablesTests` y la compuerta `LocReleaseGate`, y lo prueba en el teléfono en los tres idiomas.
- **El dueño revisa una muestra** del resumen y las filas en `cambia` y `duda` antes de que pasen a `aprobado_es` (D-054). Las filas en `duda` no se aprueban: siguen en `pendiente_asesor` y su texto no se muestra en release.
- No crees `.meta` de Unity ni toques código, tablas, el glosario ni la especificación.
