---
paths:
  - "schemas/**"
  - "**/*.schema.json"
  - "Assets/_Project/Data/**"
  - "tools/out/**"
  - "tools/out_inverse/**"
  - "tools/*.py"
---

# Datos JSON, esquema y salidas de simulación

Fuentes: GDD 08 §4 (formatos y versionado) y §9 (Remote Config); 03 §10 y §16-§17; `docs/GDD.md` §7 (contratos 12, 13, 18, 19, 21 y 28).

- **Formato de nivel `af-level/1`:** la única copia del esquema es `schemas/af-level-1.schema.json`. Todo paquete de niveles o del Reto del río se valida contra él y con las comprobaciones extra de 08 §4.1 (longitudes de `caps`/`cuias`/`attrs`, debuts de tinte, `risk_pairs`, `par`, etiquetas, Aguas turbias con vertido seguro, semilla que reproduce el tablero).
- **Cambiar el esquema** es cambiar un contrato: sube `pack_schema`, actualiza `tools/validate_schema.py` y sus casos, avisa al tech-lead y al dueño. El cliente rechaza paquetes con `pack_schema` desconocido.
- **Semillas e IDs:** campaña `amazonfill:v1:m{mm}:v{v}:n{nnnn}` e `id` `m{mm}_v{v}_n{nnnn}`; Reto del río `amazonfill:v1:daily:b{b}:d{ddd}` e `id` `d{b}_n{nnnn}`. Prefijo `af_` en SKU y eventos. Nunca "amazon" en identificadores públicos (tienda, SKU, eventos visibles).
- **Generados, no escritos a mano:** paquetes de `Assets/_Project/Data/Levels|Daily/` (los genera `tools/level-generator`), `RemoteConfigDefaults.json` (sale de la tabla única de 08 §9, con los nombres y defaults exactos), `tools/out/**` y `tools/out_inverse/**` (los escriben los scripts).
- **Cifras [sim]:** toda cifra del GDD marcada [sim] coincide con `tools/out/econ_*.md`. Para comprobarlo (Git Bash): `python tools/econ_sim.py --runs 300 | diff --strip-trailing-cr -q - tools/out/econ_B.md`. Regenerarlas cambia el GDD: solo con aprobación del dueño y actualizando las cifras citadas.
- **Estilo:** UTF-8 sin BOM, LF, 2 espacios, orden de claves estable (los diffs y los hashes dependen de ello); números con punto decimal y sin separador de miles.
- **Verificación:** `python tools/validate_schema.py` (código 0, todos los casos PASS) y, para paquetes, `dotnet run --project tools/level-qa -- <paquetes>` cuando exista.
