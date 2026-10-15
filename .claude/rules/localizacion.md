---
paths:
  - "Assets/_Project/Scripts/**"
  - "Assets/_Project/Data/Loc/**"
---

# Textos visibles y localización

Fuentes: `docs/dev/decisiones.md` (D-018, idiomas; D-017, Frutas del río; D-013, Mapa del río); `docs/research/idiomas/propuesta-idiomas.md` §3.2-§3.4 y §6.1; GDD 08 §2.2 (`AF.Localization`) y §4.7 (contrato 32); glosario §3 y §8-§11; pregunta 49 de `docs/GDD.md` §6.3. El formato de los CSV está en `.claude/rules/data-json.md`.

Valen para todo texto que el jugador ve u oye: UI, tutorial, tienda, Ajustes, modales, avisos, notificaciones, etiquetas del lector de pantalla y datos de fruta, fauna y del Mapa.

**Alcance:** estas reglas valen para todo texto nuevo y para el código que se reescribe. La deuda que ya existe (los textos de `StoreCatalog` y `StuckModal` y los marcadores posicionales `{0}` de `UiText`) la migran sus tareas de la tanda 4 (L1, L2b, S12 y L3); mientras tanto no bloquea la revisión de otras tareas ni justifica migrar textos fuera de la tarea propia.

- **Ningún texto visible fuera de las tablas:** hoy `UiText`; desde que existan, `Assets/_Project/Data/Loc/strings.csv` (UI) y `facts.csv` (textos largos). Nada de texto en catálogos, `switch`, escenas, prefabs, imágenes ni sombreadores. `AF.GameFlow` y `AF.Levels` devuelven claves, nunca texto.
- **Una clave por frase completa,** estable, con la forma que fijen 08 §4.7 y el glosario §10:
  - las claves de UI, en `snake_case`, como las del glosario §10;
  - una clave nunca se reutiliza para otro sentido ni se renombra: las que ya existen en los datos se quedan como están (`fauna_saimiri_collinsi_dato` en `miquitos_v0.json`);
  - esta regla **no fija la forma de las claves de contenido** (datos de fruta, del Mapa y de fauna): es la de la pregunta 49 de `docs/GDD.md` §6.3.
- **Marcadores con nombre** (`{n}`, `{level}`, `{price}`), nunca posicionales (`{0}`).
- **Nada de concatenar, partir ni hacer `Replace` sobre texto visible.** Si hace falta un salto de línea, va dentro de la cadena (`\n`) y lo decide el traductor.
- **Plurales con la sintaxis de la tabla** (`{n:plural:…|…}`, categorías CLDR de cada idioma). Nunca `n == 1` ni «1 semillas».
- **Números con `Loc.Number` o, en lo que se actualiza por fotograma, con `Loc.WriteNumber`** (sin asignaciones). Hasta que exista `AF.Localization`, con la función única de `UiText`. Nunca un separador fijo ni un formato propio.
  - En los textos largos, toda cifra con separador de miles o con decimales va con `{n}` y el valor en la columna `args` [P] de `facts.csv` (D-018, Q8-A).
  - Sistema métrico sin convertir y U+00A0 entre la cifra y la unidad (08 §4.7).
- **Precios solo como los da la tienda** (`IStoreService` de AF.IAP; la real los da ya formateados): la UI nunca calcula ni formatea un precio.
- **Sin `ToUpper` ni `ToLower`** (tampoco sus variantes `Invariant`) sobre texto visible: si una etiqueta va en mayúsculas, se escribe así en la tabla.
- **La analítica y Remote Config llevan claves o ID, nunca texto** (por ejemplo, el `fact_id` de `af_river_fact_shown`).
- **Lo cultural nace `pendiente_asesor`** (en su columna de estado): fruta, fauna, flora, pueblos y lugares. No se inventan nombres ni traducciones: salen del glosario (§3 y §8-§10) y del asesor.
- **Cada cadena nueva lleva `context` y `max_chars`.** El contexto dice dónde sale, quién habla y qué es cada marcador, con una captura de referencia; en los textos largos (`facts.csv`), además, la fuente del dato con su URL (08 §4.7; propuesta de idiomas §3.4).
- **Una traducción que falta queda vacía:** nunca se rellena con otro idioma ni se mezclan idiomas en pantalla. Un texto largo sin traducir no se muestra (D-018, Q10-A).
- El idioma falso `en-XA` es solo para desarrollo y pruebas; nunca se ofrece en un build de release.
