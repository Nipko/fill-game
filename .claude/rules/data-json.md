---
paths:
  - "schemas/**"
  - "**/*.schema.json"
  - "Assets/_Project/Data/**"
  - "tools/out/**"
  - "tools/out_inverse/**"
  - "tools/*.py"
  - "tools/river-map/**"
---

# Datos JSON, esquema, tablas de textos y salidas de simulación

Fuentes: GDD 08 §4 (formatos y versionado; §4.5 Mapa del río, §4.6 Frutas del río y §4.7 tablas de textos) y §9 (Remote Config); 03 §10 y §16-§17; `docs/GDD.md` §7 (contratos 12, 13, 18, 19, 21, 28 y 29-32) y §6.3 (pregunta 49); `docs/dev/decisiones.md` (D-013, D-017 y D-018).

- **Formato de nivel `af-level/1`:** la única copia del esquema es `schemas/af-level-1.schema.json`. Todo paquete de niveles o del Reto del río se valida contra él y con las comprobaciones extra de 08 §4.1 (longitudes de `caps`/`cuias`/`attrs`, debuts de tinte, `risk_pairs`, `par`, etiquetas, Aguas turbias con vertido seguro, semilla que reproduce el tablero).
- **Cambiar el esquema** es cambiar un contrato: sube `pack_schema`, actualiza `tools/validate_schema.py` y sus casos, avisa al tech-lead y al dueño. El cliente rechaza paquetes con `pack_schema` desconocido.
- **Mapa del río (D-013; contratos 29 y 30):** la copia única de los formatos `af-river-map/1` y `af-river-stops/1` es 08 §4.5 más, cuando existan, `schemas/af-river-map-1.schema.json` y `schemas/af-river-stops-1.schema.json`. Esos esquemas se validan en `tools/river-map/test_schemas.py`, nunca en `tools/validate_schema.py`. `river_stops_v1.json` lleva solo claves (`arrival_fact`, `facts`, `world_question`): los textos de esas claves (`map.*`) van en `Assets/_Project/Data/Loc/facts.csv` (contrato 32), en el bloque que regenera `tools/river-map/stops.py`, nunca en un JSON. Los textos de sistema del Mapa (`map_*` y `stop_label`; glosario §10) son de UI y van en `strings.csv`.
- **Semillas e IDs:** campaña `amazonfill:v1:m{mm}:v{v}:n{nnnn}` e `id` `m{mm}_v{v}_n{nnnn}`; Reto del río `amazonfill:v1:daily:b{b}:d{ddd}` e `id` `d{b}_n{nnnn}`. Prefijo `af_` en SKU y eventos. Nunca "amazon" en identificadores públicos (tienda, SKU, eventos visibles).
- **Generados, no escritos a mano:**
  - paquetes de `Assets/_Project/Data/Levels|Daily/` (los genera `tools/level-generator`);
  - `Assets/_Project/Data/Map/**` y el bloque de filas del Mapa de `Assets/_Project/Data/Loc/facts.csv` (los genera `tools/river-map/`; ese bloque, solo `tools/river-map/stops.py`);
  - `RemoteConfigDefaults.json` (sale de la tabla única de 08 §9, con los nombres y defaults exactos);
  - `tools/out/**` y `tools/out_inverse/**` (los escriben los scripts).
- **Cifras [sim]:** toda cifra del GDD marcada [sim] coincide con `tools/out/econ_*.md`. Para comprobarlo (Git Bash): `python tools/econ_sim.py --runs 300 | diff --strip-trailing-cr -q - tools/out/econ_B.md`. Regenerarlas cambia el GDD: solo con aprobación del dueño y actualizando las cifras citadas.
- **Estilo:** UTF-8 sin BOM, LF, 2 espacios, orden de claves estable (los diffs y los hashes dependen de ello); números con punto decimal y sin separador de miles.
- **Tablas de textos de `Assets/_Project/Data/Loc/` (CSV; D-018, contrato 32):**
  - UTF-8 sin BOM, LF y RFC 4180 (comillas para comas y saltos de línea);
  - cabecera de 08 §4.7, con la forma exacta (claves de contenido y columnas de estado) de la pregunta 49 de `docs/GDD.md` §6.3, más la columna `args` [P] al final para las cifras de los textos largos;
  - una fila por clave y filas en orden ordinal por clave, para que varios agentes añadan filas sin pisarse: por código Unicode, como `sorted()` de Python y `StringComparer.Ordinal` (o `string.CompareOrdinal`) en C#; nunca el orden de la cultura (`List.Sort()` por defecto ni `InvariantCulture`), que pone `fauna_saimiri_collinsi_dato` antes que `fauna.bradypus_variegatus.dato_01` cuando el ordinal las pone al revés (`.` U+002E < `_` U+005F);
  - ningún texto de `facts.csv` lleva cifras con separador de miles ni decimales: van con el marcador `{n}` y el valor en `args` (D-018, Q8-A); las cifras sin separador ni decimales («35 m») van tal cual;
  - las reglas de redacción y de claves, en `.claude/rules/localizacion.md`.
- **Verificación:** `python tools/validate_schema.py` (código 0, todos los casos PASS), para paquetes, `dotnet run --project tools/level-qa -- <paquetes>` y, si tocas el Mapa, los comandos del Mapa de `CLAUDE.md`.
