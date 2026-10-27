# Respuestas y recomendaciones a las preguntas abiertas T2-Q8 a T2-Q11

2026-10-26 · Parte C del encargo `docs/dev/traspaso/gemini-revision-textos.md` (D-054) sobre las preguntas abiertas registradas en `docs/dev/traspaso/spark-estado.md` (líneas 1253-1256), teniendo en cuenta los textos provisionales fijados en D-033 (añadido del 2026-10-22, `docs/dev/decisiones.md`, puntos 2-8) y el estado actual de `docs/gdd/00-glosario.md` (§1.1, §8, §10 y §12) y `Assets/_Project/Data/Loc/glossary.csv`.

## Resumen ejecutivo

| Id | Pregunta | Recomendación | Qué requiere al integrar |
|---|---|---|---|
| **T2-Q8** | Entrada del glosario para «Amazonas», «Amazonía» y «Sobre la Amazonía» frente a la restricción de marca de «Amazon» (§1.1) | **Confirmar D-033 (punto 3):** `place.amazonia` = `the Amazon` / `Amazônia`; `place.amazonas` (río) = `Amazon` / `Amazonas` (manteniendo `Amazonas` invariante cuando nombra el estado o departamento); `settings_about_amazon` = `About the Amazon` / `Sobre a Amazônia`; `Tocantins` y `Putumayo` invariantes | Quitar la nota «pendiente del asesor» en `glossary.csv` y `00-glosario.md` §1.1 y §12; ninguna fila de `strings_revision.csv` ni `facts_revision.csv` cambia |
| **T2-Q9** | Mundo 3, «Manaos» (hoy invariante en §8 y `glossary.csv`): ¿pasa a «Manaus» en `en` y `pt-BR`? | **Sí, pasar a `Manaus` en `en` y `pt-BR`** (`Manaos` en `es-419`, `Manaus` en `en`, `Manaus` en `pt-BR`) | Actualizar en un mismo commit `00-glosario.md` §8 (fila 3), `glossary.csv` (`world_w03_name`) y `strings.csv` (`world_w03_name`). En `facts_revision.csv` (`map.stop.manaos.*`, `fact.agua_negro.*`, `fauna_saguinus_bicolor_dato`, `fact.araza.12`) ya va `Manaus` |
| **T2-Q10** | Textos propuestos para el glosario §10: `fact_sheet_title` «River Fruits» / «Frutas do Rio», las filas de P14 y las de `map_*` | **Confirmar todos** tal como quedan revisados en `strings_revision.csv` (incluidos `River Fruits` / `Frutas do Rio` de D-033 punto 4 y `Nova espécie à vista!` en `fauna_sighted_card`) | Copiar las columnas `en` y `pt-BR` a las celdas «pendiente del traductor» de `00-glosario.md` §10 cuando se sincronice el GDD |
| **T2-Q11** | Propuestas [P] para el asesor en `glosario.diff`: PT «Redemoinho» o «Rebojo»; EN «Tide»; «Firefly» / «Vaga-lume»; «Noite da Onça»; «boto» como PT de «delfín de río»; «Meeting of Waters» | **Confirmar todas las opciones actuales de `glossary.csv`:** `Redemoinho`, `Tide` / `Maré`, `Firefly` / `Vaga-lume`, `Jaguar Night` / `Noite da Onça`, `river dolphin` / `boto` (con matiz zoológico cuando se contrasta con el *tucuxi*) y `Meeting of Waters` / `Encontro das Águas` | Retirar la marca «pendiente del asesor» en `00-glosario.md` §12 y `glossary.csv`. Además, se recomienda reflejar en el glosario el cambio de `league_name_squirrel_monkey` en `pt-BR` a `Mico-de-cheiro` (evita «macaco») |

---

## T2-Q8 · «Amazonas», «Amazonía» y «Sobre la Amazonía»

**Pregunta (`spark-estado.md:1253`):** Entrada del glosario para «Amazonas», «Amazonía» y «Sobre la Amazonía» (con el asesor; el glosario §1 evita la cadena «Amazon» por la marca).

### Recomendación
Confirmar como definitivas las entradas introducidas provisionalmente por D-033 (2026-10-22, punto 3):

| Término / Clave | `es-419` | `en` | `pt-BR` |
|---|---|---|---|
| `place.amazonia` | Amazonía | the Amazon | Amazônia |
| `place.amazonas` (el río) | Amazonas | Amazon | Amazonas |
| `settings_about_amazon` | Sobre la Amazonía | About the Amazon | Sobre a Amazônia |
| `place.tocantins` | Tocantins | Tocantins | Tocantins |
| `place.putumayo` | Putumayo | Putumayo | Putumayo |

### Motivo
1. **Alcance marcario correcto (`00-glosario.md` §1.1):** La restricción frente a las guías de marca de Amazon.com aplica al nombre comercial del producto (**Tintalú**), logotipo, títulos de tienda, `applicationId`, dominios y cuentas. En el texto interior educativo y geográfico del juego, nombrar el bioma (*the Amazon*, *Amazônia*) y el río (*Amazon River*, *rio Amazonas*) es uso descriptivo legítimo e insustituible para un hablante nativo. Evitarlo en inglés obligaría a perífrasis artificiales (*the great South American basin*) que restan claridad y rigor científico.
2. **Naturalidad en `en` y `pt-BR`:**
   - En inglés estadounidense, la región se llama idiomáticamente *the Amazon* (o *the Amazon basin* / *Amazonian rainforest* según el contexto sintáctico) y el río *the Amazon* o *Amazon River*. *About the Amazon* (16 car.) es conciso y natural en Ajustes.
   - En portugués de Brasil existe una distinción léxica exacta y obligatoria: **Amazônia** (con acento circunflejo) designa el bioma y la región (*Sobre a Amazônia*, 16 car.), mientras que **Amazonas** designa el río y el estado.
3. **Regla de desambiguación río vs. estado/departamento (aplicada en `facts_revision.csv`):** Cuando «Amazonas» en `es-419` no se refiere al río sino a la **entidad subnacional** —el estado brasileño de Amazonas (`world_w03_country`, `fact.acai.11`, `fact.cupuacu.12`, `fact.tucuma.*`) o el departamento de Amazonas en Colombia o Perú (`map.stop.leticia.dato_01`)—, en inglés **se conserva `Amazonas`** (*Amazonas state*, *Colombia's Amazonas department*), nunca *Amazon*. Esta distinción ya figura en `notes` de `place.amazonas` en `glossary.csv` y conviene mantenerla explícita.

---

## T2-Q9 · Mundo 3, «Manaos»: ¿pasa a «Manaus» en `en` y `pt-BR`?

**Pregunta (`spark-estado.md:1254`):** Mundo 3, «Manaos»: es invariante y `glossary.csv` no admite dos filas con el mismo `es-419`. ¿Pasa a «Manaus» en EN y PT?

### Recomendación
**Sí.** Pasar `world_w03_name` a **`Manaus`** tanto en `en` como en `pt-BR`, conservando **`Manaos`** en `es-419`:

| Término | `es-419` | `en` | `pt-BR` |
|---|---|---|---|
| `world_w03_name` | Manaos | **Manaus** | **Manaus** |

### Motivo
1. **En portugués de Brasil es obligatorio por corrección ortográfica y respeto local:** *«Manaos»* con *o* fue la grafía histórica hasta el Formulario Ortográfico de 1943. Para cualquier jugador brasileño actual —y Brasil es mercado prioritario del juego—, leer *«Manaos»* en portugués parece una errata o un hispanismo sin revisar en el nombre de la mayor metrópoli de la Amazonía brasileña.
2. **En inglés contemporáneo el único topónimo estándar es `Manaus`:** Así lo registran el U.S. Board on Geographic Names (BGN), USGS, Encyclopaedia Britannica y la NASA (incluidas las fuentes `N1`, `N3` y `N7` del propio catálogo `frutas_del_rio_v1.json`).
3. **En español (`es-419`) conviene mantener `Manaos`:** Es el exónimo panhispánico tradicional recogido por el *Diccionario panhispánico de dudas* y usado de forma consistente en todo el corpus de `strings.csv` y `facts.csv`.
4. **Resuelve limpiamente la restricción técnica de `glossary.csv`:** Como `GlossaryTableTests` impide dos filas con el mismo `es-419`, actualizar `world_w03_name` a `Manaos | Manaus | Manaus` cubre a la vez el nombre del Mundo 3 y las apariciones de la ciudad en `facts.csv` sin duplicar claves.

### Cómo queda preparado en esta entrega
- En **`facts_revision.csv`** (donde `GlossaryTableTests` no bloquea las claves `map.stop.manaos.*`, `fauna_saguinus_bicolor_dato`, `fact.agua_negro.10`, `fact.agua_negro.19` ni `fact.araza.12`) **ya hemos escrito `Manaus` en `en` y `pt-BR`**.
- En **`strings_revision.csv`**, mantuvimos temporalmente `world_w03_name = Manaos` en `en` y `pt-BR` únicamente porque este encargo no modifica `glossary.csv`, y cambiar solo `strings.csv` rompería `GlossaryTableTests.EveryRow_MatchesWithItsSourceTable`. En cuanto el dueño confirme esta recomendación, basta con cambiar `Manaos` → `Manaus` en `en` y `pt-BR` en `00-glosario.md` §8 (fila 3), `glossary.csv` (`world_w03_name`) y `strings.csv` (`world_w03_name`).
- **Observación hermana (`world_w11_name`):** El Mundo 11 figura hoy invariante como `Escudo de Guayana` en los tres idiomas (`glossary.csv:141`), mientras que en inglés y portugués el nombre geográfico natural es **`Guiana Shield`** (13 car.) y **`Escudo das Guianas`** (18 car., dentro de `20 × 1,3 = 26`). Recomendamos aprovechar la misma actualización de §8 y `glossary.csv` para localizar también `world_w11_name`.

---

## T2-Q10 · Textos propuestos para el glosario §10 (`fact_sheet_title`, P14 y `map_*`)

**Pregunta (`spark-estado.md:1255`):** Textos propuestos para el glosario §10: `fact_sheet_title` «River Fruits» / «Frutas do Rio», las filas de P14 y las de `map_*`.

### Recomendación
**Aprobar todas las traducciones revisadas** que entregamos en `strings_revision.csv` y volcarlas a las celdas que siguen como «pendiente del traductor» en `00-glosario.md` §10. Todas cumplen el tono cálido y directo del juego, las categorías plurales CLDR de cada idioma, los espacios duros `U+00A0` y el límite visual `≤ max_chars × 1,3` (incluida la prueba combinatoria `MapTextLocalesTests` sobre las 100 paradas):

| Grupo | Clave | `es-419` | `en` (aprobado) | `pt-BR` (aprobado) |
|---|---|---|---|---|
| **Frutas del río** | `fact_sheet_title` | Frutas del río | River Fruits | Frutas do Rio |
| | `fact_progress` | `{name} · {k}/{n}` | `{name} · {k}/{n}` | `{name} · {k}/{n}` |
| | `fact_locked` | `Nivel {n}` | `Level {n}` | `Nível {n}` |
| | `fact_starts_at` | `Su serie empieza en el nivel {n}` | `Its series starts at level {n}` | `A série começa no nível {n}` |
| | `fact_sources` | `Fuentes` | `Sources` | `Fontes` |
| | `fact_a11y` | `Dato del río. {name}, {k} de {n}. {text}` | `River fact. {name}, {k} of {n}. {text}` | `Curiosidade do rio. {name}, {k} de {n}. {text}` |
| **Embudo P14 y tarjeta fauna** | `stuck_give_up` | Rendirse | Give up | Desistir |
| | `giveup_confirm_title` | ¿Seguro? | Are you sure? | Tem certeza? |
| | `giveup_confirm_canoe` | `{n:plural:Perderás {} canoa\|Perderás {} canoas}` | `{n:plural:You'll lose {} canoe\|You'll lose {} canoes}` | `{n:plural:Você perderá {} canoa\|Você perderá {} canoas}` |
| | `level_failed` | Nivel fallido | Level failed | Nível não concluído |
| | `level_failed_reward` | `{n:plural:Premio: {} semilla\|Premio: {} semillas}` | `{n:plural:Reward: {} seed\|Reward: {} seeds}` | `{n:plural:Prêmio: {} semente\|Prêmio: {} sementes}` |
| | `retry` | Reintentar | Retry | Tentar de novo |
| | `victory_perfect` | ¡Perfecto! | Perfect! | Perfeito! |
| | `fauna_sighted_card` | ¡Miquito a la vista! | Monkey spotted! | Nova espécie à vista! |
| **Mapa del río (D-013 y P-M2)** | `map_stop_remaining` | `{n:plural:Falta {} nivel para {place}\|Faltan {} niveles para {place}}` | `{n:plural:{} level left to {place}\|{} levels left to {place}}` | `{n:plural:Falta {} nível para {place}\|Faltam {} níveis para {place}}` |
| | `map_arrived` | `{place}{sep}{country}{sep}río {river}` | `{place}{sep}{country}{sep}{river} River` | `{place}{sep}{country}{sep}rio {river}` |
| | `map_home_distance` | `Casa · a unos {km} km` | `Home · about {km} km` | `Casa · a cerca de {km} km` |
| | `map_country_welcome` | `{country} te da la bienvenida` | `{country} welcomes you` | `{country} dá as boas-vindas a você` |
| | `map_world_question` | `{world} · {question}` | `{world} · {question}` | `{world} · {question}` |
| | `map_next_world` | `Mundo {n} · {name}{sep}Nivel {level}` | `World {n} · {name}{sep}Level {level}` | `Mundo {n} · {name}{sep}Nível {level}` |
| | `map_samples` | `Muestrario · {k}/{n}` | `Samples · {k}/{n}` | `Mostruário · {k}/{n}` |
| | `map_north_up` | `Norte arriba` | `North up` | `Norte para cima` |
| | `map_back_to_canoe` | `Volver a la canoa` | `Back to the canoe` | `Voltar à canoa` |
| | `map_hito_preview` | `Si lo superas, llegas a {place}` | `Beat it and you'll reach {place}` | `Se vencer, você chega a {place}` |
| | `stop_label` | `{place} · {country}` | `{place} · {country}` | `{place} · {country}` |
| | `map_view_world` | `Mundo` | `World` | `Mundo` |
| | `map_view_basin` | `Cuenca` | `Basin` | `Bacia` |
| | `map_label_andes` | `Andes ↑` | `Andes ↑` | `Andes ↑` |
| | `map_label_atlantic` | `Océano Atlántico ↓` | `Atlantic Ocean ↓` | `Oceano Atlântico ↓` |
| | `map_back_to_level` | `Volver al nivel` | `Back to level` | `Voltar ao nível` |
| | `map_open` | `Ver mapa` | `View map` | `Ver mapa` |
| | `map_card_other_fact` | `Otro dato` | `Another fact` | `Outra curiosidade` |
| | `map_card_source` | `Fuente: {source}` | `Source: {source}` | `Fonte: {source}` |
| | `map_facts_count` | `Datos del río · {k}/{n}` | `River facts · {k}/{n}` | `Curiosidades do rio · {k}/{n}` |
| | `map_l10_intro` | `Yo vivo aquí, donde el río entra al mar.` | `I live here, where the river meets the sea.` | `Eu moro aqui, onde o rio encontra o mar.` |
| **Ajustes de TiTino** | `settings_sloth_messages` | `Mensajes de TiTino` | `Messages from TiTino` | `Mensagens do TiTino` |

### Motivos destacados
- **`fact_sheet_title` (`River Fruits` / `Frutas do Rio`):** Nombra con exactitud el eje del álbum, mantiene paralelismo con `River Challenge` / `Desafio do Rio` y `River Map` / `Mapa do Rio`, y cabe con holgura tanto en la cabecera de la ficha como en la fila de Ajustes.
- **`level_failed` en PT-BR (`Nível não concluído`):** Mucho más cálido y acorde con el tono sin castigo del juego que *«Nível falhado»* (lusitanismo) o *«Você falhou»*.
- **`fauna_sighted_card` en PT-BR (`Nova espécie à vista!`):** Excelente solución provisional de D-033 punto 6 que confirmamos: celebra el hallazgo como apunte de campo, evita por completo la palabra sensible *«macaco»* (`00-glosario.md` §10) y no confunde todas las especies de la tropa (tamarinos, uacarís, sakis, monos nocturnos) bajo una sola etiqueta restrictiva.
- *(Nota menor para el GDD: la fila `quit_confirm` de `00-glosario.md` §10 no existe como clave independiente en `strings.csv`, porque el cliente reutiliza `pause_quit` + `giveup_confirm_*`.)*

---

## T2-Q11 · Propuestas [P] de `glosario.diff` para validación del asesor

**Pregunta (`spark-estado.md:1256`):** Propuestas [P] para el asesor: PT «Redemoinho» o «Rebojo» (Remolino); EN «Tide» (Marea), «Firefly» / «Vaga-lume» (Luciérnaga); «Noite da Onça»; «boto» como PT de «delfín de río»; «Meeting of Waters».

### Recomendación punto por punto

1. **Remolino (`mechanic.remolino`) en PT-BR → `Redemoinho` (frente a `Rebojo`):**
   - **Veredicto:** Confirmar **`Redemoinho`** (EN `Whirlpool`).
   - **Motivo:** Aunque *rebojo* es una voz ribereña amazónica legítima para grandes corrientes arremolinadas de fondo, muchos jugadores brasileños fuera de la región Norte no la reconocen al instante como un vórtice que enlaza dos recipientes. *Redemoinho* es comprensible en el 100 % de Brasil, describe de inmediato el giro del agua en el tablero y también es corriente en la propia Amazonía.

2. **Marea (`booster.undo`) en EN → `Tide` (y PT-BR `Maré`):**
   - **Veredicto:** Confirmar **`Tide`** y **`Maré`**.
   - **Motivo:** Breve (4 caracteres), transparente, fiel a la geografía del estuario amazónico (Mundo 1, donde las mareas del Atlántico invierten el flujo diario de los furos e igarapés) e intuitivo para «hacer retroceder el último vertido», siempre respaldado en el HUD por la etiqueta funcional `Undo` / `Desfazer`.

3. **Luciérnaga (`booster.reveal`) → EN `Firefly` / PT-BR `Vaga-lume`:**
   - **Veredicto:** Confirmar **`Firefly`** y **`Vaga-lume`** (con guion normativo según el VOLP).
   - **Motivo:** Son los nombres cotidianos más cálidos y universales en EE. UU. (*firefly* frente al regionalismo *lightning bug*) y en Brasil (*vaga-lume* frente a *pirilampo*). Comunican al primer vistazo la acción de alumbrar las dosis ocultas en Aguas turbias. *(En `strings_revision.csv` pulimos además la frase `firefly_line` a «A firefly lights up the fruit in a cuia» / «Um vaga-lume ilumina as frutas de uma cuia» para subsanar el pronombre sin antecedente de la traducción anterior).*

4. **Noche de jaguar (`difficulty_subtitle_very_hard`) → EN `Jaguar Night` / PT-BR `Noite da Onça`:**
   - **Veredicto:** Confirmar **`Jaguar Night`** y **`Noite da Onça`**.
   - **Motivo:** En Brasil *Panthera onca* se nombra siempre *onça* (u *onça-pintada*), nunca *jaguar* en el habla viva. *Noite da Onça* suena idiomático, evocador y respetuoso con la fauna (sugiere atención en la selva nocturna sin demonizar al animal, conforme a `07 §1`).

5. **«delfín de río» (`fauna.river_dolphin`) → EN `river dolphin` / PT-BR `boto`:**
   - **Veredicto:** Confirmar **`boto`** en `glossary.csv` como traducción base en PT-BR, añadiendo una nota de precisión zoológica para frases comparativas.
   - **Motivo:** En portugués de Brasil, los cetáceos dulces amazónicos (*Inia*) se llaman de forma natural *boto* (o *boto-cor-de-rosa*), nunca *golfinho-de-rio* en uso general. **Única excepción de contexto:** cuando un dato contrasta las dos especies simpátricas de delfines fluviales de la cuenca —el *boto-cor-de-rosa* (*Inia geoffrensis*) y el *tucuxi* (*Sotalia fluviatilis*)—, como ocurre en `fact.agua_tapajos.15`, la categoría supraespecífica natural en PT-BR es *«dois golfinhos de rio: o boto-cor-de-rosa e o tucuxi»* (así queda en `facts_revision.csv`), porque en la ribera amazónica *boto* y *tucuxi* se distinguen como dos animales distintos.

6. **Encuentro de las Aguas (`place.meeting_of_waters`) → EN `Meeting of Waters` / PT-BR `Encontro das Águas`:**
   - **Veredicto:** Confirmar **`Meeting of Waters`** y **`Encontro das Águas`**.
   - **Motivo:** *Encontro das Águas* es el nombre oficial brasileño del monumento natural tombado por el IPHAN frente a Manaus, y *Meeting of Waters* es su nombre consagrado en la literatura científica y divulgativa en inglés (NASA Earth Observatory, Britannica, UNESCO), alineado además con `00-glosario.md` §7.2.

7. **Validación del resto de entradas provisionales de `00-glosario.md` §12 y D-033 (puntos 5-6):**
   - Confirmamos asimismo sin cambios: `Meeting Cuia` / `Cuia do Encontro` (`skin_af_skin_m03_name`), `Totuma` invariante (`skin_af_skin_m04_name`), `Mount Mismi` / `Nevado Mismi` (`place.mismi`), `Big Basket` / `Cesta grande` (`ui.grand_basket`), `Pororoca` y `Morpho` invariantes, `Boto Leap` / `Salto do Boto` (`booster.skip`), y todas las entradas de `mechanic.*` y `fauna.*` de `glossary.csv`.
   - **Ajuste cultural recomendado para el glosario de la Liga (`league_name_squirrel_monkey`):** En `strings_revision.csv` sustituimos en `pt-BR` *«Macaco-de-cheiro»* por **`Mico-de-cheiro`** (denominación brasileña genuina documentada por ICMBio y Houaiss para *Saimiri*), evitando llamar *«Macaco-...»* a la división de un jugador en Brasil y cumpliendo la directriz expresa de `00-glosario.md` §10 (*«en PT, evitar "macaco"»*).
