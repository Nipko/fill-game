# Resumen de Revisión Cultural y Nativa — Tanda 4 (Versión 1.1)

## 1. Conteo de veredictos en `revision_es.csv` (Parte A — 50 filas)

| Tabla | `ok` | `cambia` | `duda` | Total |
| :--- | :---: | :---: | :---: | :---: |
| `strings.csv` | 9 | 0 | 0 | **9** |
| `facts.csv` | 41 | 0 | 0 | **41** |
| **Total** | **50** | **0** | **0** | **50** |

### Notas de la revisión cultural en español (`es-419`)
- **Tripulación del río (`a11y_map_crew_join_toucan`, `a11y_map_crew_join_tucuxi`, `map_crew_join_toucan`, `map_crew_join_tucuxi`):** `ok`. Los nombres de especie (`tucán` / `Tucán` y `tucuxi` / `Tucuxi`) coinciden con `fauna.toucan`, `fact.agua_tapajos.15` y `docs/dev/propuesta-mapa-rio-v2.md` §10.7, sin bautizar a los animales con nombres propios (`00-glosario.md` §6.1 regla 2).
- **Etiquetas del mapa (`map_stop_label_prize`, `map_stop_label_remaining`):** `ok`. Mantienen el mismo tono y vocabulario que `map_world_prize` y `map_stop_remaining`.
- **Tutoriales de mecánicas 1.1 (`tutorial_aguape`, `tutorial_aguape_n2`, `tutorial_lancha`):** `ok`. Respetan las reglas culturales de `00-glosario.md` §6.5 (uso de `aguapé` / `aguapé espeso` sin metáforas de hielo ni escarcha; uso de `lancha` con el verbo `atraca`, sin usar «llegar en canoa» ni «balsa») y conservan intactas las plantillas `{n:plural:...}`.
- **Nombres de mecánicas (`mechanic.aguape`, `mechanic.aguape_n2`):** `ok` (`Aguapé` y `Aguapé espeso`, alineados con `00-glosario.md` §6.5 y D-007/D-068).
- **Topónimos y ríos del mapa (`map.stop.<id>.name` × 13 y `map.stop.<id>.river` × 26):** `ok` en las 39 filas. Los 13 nombres de pueblos nuevos (`Almeirim`, `Amaturá`, `Cachuela Esperanza`, `Codajás`, `Guajará-Mirim`, `Humaitá`, `Indiana`, `Manacapuru`, `Manicoré`, `Orellana`, `Porto Velho`, `Prainha`, `São Paulo de Olivença`) llevan la grafía y diacríticos oficiales locales verificados en `rio-amazonas.json`, `overrides.json` y Wikipedia/IBGE/INEI. Los 26 nombres de río (`Amazonas`, `Solimões`, `Pará`, `Beni`, `Mamoré`, `Madeira`, `Ucayali`, sin la palabra «río») coinciden con la hidrografía de cada parada y con `tools/river-map/overrides.json` (incluyendo `Solimões` para las paradas brasileñas aguas arriba de Manaos y `Pará` para Abaetetuba y Breves). Ninguna fila `map.*` requirió `duda`.

---

## 2. Celdas completadas y modificadas en `en` y `pt-BR` (Parte B — 72 filas)

Todas las 30 filas de `strings_revision.csv` y las 42 filas de `facts_revision.csv` quedan con `status_en=revisado_nativo` y `status_pt-BR=revisado_nativo` (0 celdas vacías).

### 2.1. Celdas vacías completadas en `facts_revision.csv` (4 celdas en 2 filas)

| Key | Idioma | Valor completado | Motivo |
| :--- | :---: | :--- | :--- |
| `mechanic.aguape` | `en` | `Water Hyacinth` | Nombre oficial en inglés en `00-glosario.md` §6.5 y consistente con `tutorial_aguape` (`Water hyacinth is cleared...`). |
| `mechanic.aguape` | `pt-BR` | `Aguapé` | Nombre oficial en portugués brasileño en `00-glosario.md` §6.5 y consistente con `tutorial_aguape` (`O aguapé se limpa...`). |
| `mechanic.aguape_n2` | `en` | `Thick Water Hyacinth` | Nombre oficial de nivel 2 coherente con `tutorial_aguape_n2` (`Thick water hyacinth needs two adjacent matches...`). |
| `mechanic.aguape_n2` | `pt-BR` | `Aguapé denso` | Nombre oficial de nivel 2 en `00-glosario.md` §6.5 y coherente con `tutorial_aguape_n2` (`O aguapé denso precisa...`). |

### 2.2. Cambio de nombre de mecánica en `pt-BR` y su tutorial (§3.3 — 2 celdas)

| Tabla | Key | Idioma | Texto de entrada | Texto revisado | Motivo |
| :--- | :--- | :---: | :--- | :--- | :--- |
| `facts.csv` | `mechanic.lancha` | `pt-BR` | `Recreio` | `Lancha` | En `00-glosario.md` §6.5, `Lancha` está listada explícitamente como alternativa válida en `pt-BR` («alternativas: "Barco" o "Lancha"»). En el arte procedural (`ProceduralArt.Lancha.cs`) la pieza del tablero es una lancha pequeña de madera con motor de popa y 1–3 frutas, mientras que en la Amazonía brasileña el *barco de recreio* es el barco regional de dos pisos con hamacas (el de la viñeta de puerto). Además, la palabra `Recreio` aislada sin «barco de» se lee en Brasil como «recreo escolar». `Lancha` (con tilde grave/circunflejo innecesario, 6 caracteres) es natural, inequívoco en todo Brasil y fiel al objeto visual. |
| `strings.csv` | `tutorial_lancha` | `pt-BR` | `O recreio atraca quando você tiver {n:plural:{} cuia vazia|{} cuias vazias}: ele traz fruta para terminar.` | `A lancha atraca quando você tiver {n:plural:{} cuia vazia|{} cuias vazias}: ela traz fruta para terminar.` | Alineado con el cambio de `mechanic.lancha` a `Lancha` (§3.3) y concordancia femenina (`A lancha... ela traz`). Longitud visible: 78 caracteres (`max_chars` = 96). |

### 2.3. Ajustes de naturalidad nativa en inglés (`en` — 9 celdas en `strings_revision.csv`)

| Key | Texto de entrada (`en`) | Texto revisado (`en`) | Motivo |
| :--- | :--- | :--- | :--- |
| `a11y_map_city` | `{place} · passing city · {country}` | `{place} · city along the way · {country}` | En inglés, *passing city* suena a que la ciudad se está moviendo o pasando de largo (calco literal de «ciudad de paso»). *city along the way* transmite con naturalidad en TalkBack que es una ciudad por la que pasa el recorrido fluvial y cabe holgadamente en `max_chars` (60). |
| `a11y_map_city_place` | `{place} · passing city` | `{place} · city along the way` | Misma razón que `a11y_map_city` para la variante sin país. |
| `a11y_map_town` | `{place} · passing town · {country}` | `{place} · town along the way · {country}` | Misma razón que `a11y_map_city`: *town along the way* evita el calco *passing town* y suena natural en inglés estadounidense. |
| `a11y_map_town_place` | `{place} · passing town` | `{place} · town along the way` | Misma razón que `a11y_map_town` para la variante sin país. |
| `a11y_map_crew_join_toucan` | `Joins the trip: toucan` | `Joining the trip: toucan` | En inglés, abrir una etiqueta o aviso sin sujeto con el verbo conjugado en 3.ª persona (*Joins the trip*) suena truncado. El participio *Joining the trip: toucan* (24 caracteres, `max_chars` = 40) es idiomático para un anuncio en curso en TalkBack. |
| `a11y_map_crew_join_tucuxi` | `Joins the trip: tucuxi` | `Joining the trip: tucuxi` | Misma razón que `a11y_map_crew_join_toucan` (24 caracteres, `max_chars` = 40). |
| `map_crew_join_toucan` | `Joins the trip! · Toucan` | `Joining the trip! · Toucan` | Misma razón que en las claves TalkBack: *Joining the trip! · Toucan* (26 caracteres, `max_chars` = 32) es natural y cálido como cartel breve en pantalla. |
| `map_crew_join_tucuxi` | `Joins the trip! · Tucuxi` | `Joining the trip! · Tucuxi` | Misma razón que `map_crew_join_toucan` (26 caracteres, `max_chars` = 32). |
| `avatar_frame_chosen_a11y` | `{world} frame, {country}, on` | `{world} frame, {country}, in use` | Cerrar la etiqueta TalkBack con `, on` suelto detrás de `{country}` resulta ambiguo en inglés (suena a preposición truncada o a interruptor *On/Off*). `, in use` expresa con claridad el sentido de `puesto` / `colocada` dentro del límite de `max_chars` (48). |

---

## 3. Revisión de las 21 filas en español con `status_es = borrador` (Parte C)

Se revisaron las 21 filas de `strings.csv` cuyo español está en `borrador`:
- **Accesibilidad de paradas del mapa (`a11y_map_city`, `a11y_map_city_place`, `a11y_map_port_place`, `a11y_map_town`, `a11y_map_town_place`, `a11y_map_water_change`, `a11y_map_water_change_place`):** El español actual es claro, conciso y consistente con `a11y_map_port` y `a11y_map_confluence` de 1.0. No requiere cambios.
- **Marcos de avatar por mundo (`avatar_frame_chosen_a11y`, `avatar_frame_default_a11y`, `avatar_frame_default_chosen_a11y`, `avatar_frame_default_label`, `avatar_frame_locked_a11y`, `avatar_frame_locked_hint`, `avatar_frame_plain_a11y`, `avatar_frame_plain_chosen_a11y`, `avatar_frame_section`, `avatar_frame_select_a11y`, `avatar_frame_unlocked_sub`, `avatar_frame_unlocked_title`, `avatar_frame_world_label`, `world_clear_frame_toast`):** El español actual sigue fielmente `docs/dev/avatares.md` §2.5 y D-069, distingue con claridad el marco inicial (`Río`), los marcos de mundo y el estado bloqueado (`Llega al mundo {n}`), y está validado en `AvatarFrameTests.cs`. No requiere cambios.

---

## 4. Datos verificados `[V]` para los 13 pueblos nuevos (`datos_pueblos.csv` — Parte D)

Se entregaron los 13 datos verificados en `datos_pueblos.csv` (ordenados en orden ordinal por `id`: `almeirim` .. `sao_paulo_de_olivenca`):
- Todos cumplen `es-419` ≤ 90 caracteres (máximo: 83 caracteres) y `en` / `pt-BR` ≤ 117 caracteres (máximo: 88 caracteres).
- Todos expresan una sola idea geográfica, fluvial o de flora local (ribera del río, confluencia, rápidos, estación ecológica/bosque nacional o cosecha de açaí en Codajás), respetando estrictamente las prohibiciones de `00-glosario.md` §4 (sin pueblos indígenas como tema, sin objetos rituales, sin explotación del caucho, sin cifras de población y sin alertas de animales peligrosos).
- Cada fila incluye `fuente_url` real consultada y `cita` textual de ≤ 25 palabras.

---

## 5. Dudas abiertas y verificaciones técnicas

- **Dudas (`duda`):** 0 en `revision_es.csv` y 0 en filas `map.*`.
- **Validación con `apply_translations.py --check`:** Ejecutado contra `docs/dev/traspaso/gemini-revision-4`; reporta **194 celdas o estados** por actualizar (50 veredictos en español + 72 filas × 2 idiomas) y **0 conflictos**.
