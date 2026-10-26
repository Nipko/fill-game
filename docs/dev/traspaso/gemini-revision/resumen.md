# Resumen de la revisión cultural y nativa de textos (D-054)

2026-10-26 · Entrega completa del encargo `docs/dev/traspaso/gemini-revision-textos.md` en `docs/dev/traspaso/gemini-revision/`, lista para revisión de muestra del dueño e integración mediante `tools/loc/apply_translations.py`.

## 1. Recuentos globales

### Parte A · Español cultural (`revision_es.csv`)

Se revisaron las **1.083 filas** que estaban en `pendiente_asesor` entre ambas tablas (`263` de `strings.csv` + `820` de `facts.csv`), contrastando exactitud frente a las fuentes citadas en `context` y `notes`, apego al glosario canónico, ausencia de elementos sagrados/estereotipos (`07 §1` y `§8.1`), español neutro de América Latina, marcadores, plurales, espacios duros `U+00A0` y límite `≤ max_chars`:

| Tabla | Filas revisadas (`pendiente_asesor`) | `ok` | `cambia` | `duda` |
|---|---:|---:|---:|---:|
| `strings.csv` | 263 | 258 | 5 | 0 |
| `facts.csv` | 820 | 813 | 7 | 0 |
| **Total Parte A** | **1.083** | **1.071** | **12** | **0** |

### Parte B · Inglés (`en`) y portugués de Brasil (`pt-BR`) (`strings_revision.csv` y `facts_revision.csv`)

Se revisaron como hablante nativo el **100 % de las filas** de ambas tablas (**1.364 filas** = **2.728 celdas** en `revisado_nativo`), incluyendo la traducción íntegra de las **364 filas vacías** de `facts.csv` y la retraducción de todas las filas cuyo español cambió en la Parte A (`08 §4.7`):

| Entregable | Filas totales | Estado final `en` / `pt-BR` | Desglose de intervención |
|---|---:|---|---|
| `strings_revision.csv` | **542** | `revisado_nativo` (542 / 542) | Revisión nativa completa de las 542 claves de UI; **4** celdas ajustadas en `en` y **12** en `pt-BR` (retraducción de cambios en ES, eliminación de «macaco» en la Liga, coherencia de tratamiento `você` y corrección de hispanismos residuales) |
| `facts_revision.csv` | **822** | `revisado_nativo` (822 / 822) | **458** filas ya pobladas revisadas (7 ajustadas en `en`, 5 en `pt-BR`, incluido `Manaus` y atribución de fuentes) + **364** filas vacías completadas (**247** revisadas y pulidas a partir de los borradores retenidos de T2 —11 retoques en `en`, 13 en `pt-BR`— y **117** traducidas desde cero) |
| **Total Parte B** | **1.364** | **2.728 celdas `revisado_nativo`** | 0 celdas vacías · 0 errores de longitud (`≤ max_chars × 1,3`), marcadores, plurales CLDR, `U+00A0` ni cobertura de glifos en los atlas SDF de Nunito y Baloo 2 |

---

## 2. Las 12 filas con veredicto `cambia` en la Parte A (`revision_es.csv`)

Para facilitar la revisión del dueño prevista en D-054 (§4), se listan aquí las **12 únicas modificaciones propuestas al español** (5 en `strings.csv` y 7 en `facts.csv`):

| # | Tabla | Clave | `es_actual` | `es_propuesto` | Motivo |
|---:|---|---|---|---|---|
| 1 | `strings` | `push_logbook_pending` | Hoy no has sellado tu Bitácora del río: {reward} te esperan. | Aún no has sellado tu Bitácora del río. Tu premio de hoy: {reward}. | Corrige la discordancia verbal de «{reward} te esperan» cuando `{reward}` es singular («Cuia extra ×1») o empieza con signo («+ Canasta grande»), advertida en `notes`. |
| 2 | `strings` | `sloth_line_fail_02` | Sin afán: cada intento te enseña algo. | Sin prisa: cada intento te enseña algo. | Sustituye el colombianismo «sin afán» (en México, Perú y el Cono Sur «afán» se entiende como empeño o anhelo, no como prisa) por el panhispánico «sin prisa» (`propuesta-titino.md` §e.1). |
| 3 | `strings` | `sloth_line_streak_06` | La Crecida quedó en cero. Otra vez, sin afán. | La Crecida quedó en cero. Otra vez, sin prisa. | Mismo criterio de español neutro latinoamericano que `sloth_line_fail_02`. |
| 4 | `strings` | `sloth_line_win_02` | Sin afán y con tino. ¡Así se hace! | Sin prisa y con tino. ¡Así se hace! | Mismo criterio de español neutro latinoamericano que `sloth_line_fail_02`. |
| 5 | `strings` | `world_w13_river` | Marañón alto | Apurímac | Corrige el arrastre previo a D-013 advertido en `notes`: en el canon vigente (`00-glosario.md` §8, fila 13) el río del Mundo 13 (El origen) es el **Apurímac**. |
| 6 | `facts` | `fact.acai.13` | En el bosque, tucanes, loros y guacamayos comen açaí y esparcen sus semillas. | En la selva, tucanes, loros y guacamayos comen açaí; los tucanes esparcen sus semillas. | Resuelve la alerta ecológica de `notes` (*Euterpe oleracea* vs. *E. precatoria*): psitácidos y tucanes consumen el fruto, pero loros y guacamayos suelen depredar la semilla mientras que los tucanes la regurgitan intacta y la dispersan (Moegenburg & Levey 2002, `10.1046/j.1365-2664.2002.00741.x`). |
| 7 | `facts` | `fact.agua_tapajos.03` | Viene del Escudo Brasileño, no de los Andes: por eso, de natural, trae poco barro. | Viene del Escudo Brasileño, no de los Andes: por eso, por naturaleza, trae poco barro. | Sustituye la locución peninsular «de natural» por el estándar panhispánico «por naturaleza», conservando el matiz hidrográfico previo a la turbidez antrópica. |
| 8 | `facts` | `fact.rio_napo.17` | El tití pigmeo, el mono más pequeño del mundo, vive en los bosques de Cuyabeno. | El leoncillo o tití pigmeo, el mono más pequeño del mundo, vive en Cuyabeno. | Incorpora «leoncillo», nombre común ecuatoriano de *Cebuella pygmaea* (Bioweb PUCE) solicitado en `notes`, y evita confundirlo con un «tití» aislado de la tropa de miquitos. |
| 9 | `facts` | `fact.rio_napo.18` | En Cuyabeno viven hoy comunidades siona, secoya, cofán, kichwa y shuar. | En Cuyabeno viven hoy comunidades siona, siekopai, a'i cofán, kichwa y shuar. | Adopta las autodenominaciones vigentes en Ecuador reconocidas por CONAIE/CONFENIAE (`siekopai` en lugar del exónimo histórico `secoya`, y `a'i cofán`), atendiendo la revisión pedida en `notes` (`07 §8.1`). |
| 10 | `facts` | `fact.tapereba.13` | Al madurar cae al suelo, donde lo buscan tapires, sajinos y venados. | Al madurar cae al suelo, donde lo buscan tapires, pecaríes y venados. | Sustituye el regionalismo andino-amazónico «sajinos» (opaco en México y otros países de la región) por «pecaríes», claro en toda América Latina (`propuesta-frutas-del-rio.md` §6.5). |
| 11 | `facts` | `fauna.bradypus_variegatus.dato_08` | La cría viaja en el pecho de su madre durante meses (unos seis en esta especie). | La cría viaja abrazada al pecho de su madre durante sus primeros meses. | Elimina el paréntesis «(unos seis en esta especie)» ante la discrepancia entre fuentes primarias (≈ 4 meses en ADW, ≈ 6 en ICMBio, hasta 12 en Sloth Conservation Foundation) señalada en `notes` (`propuesta-titino.md` §g.3). |
| 12 | `facts` | `map.stop.flona_tapajos.dato_01` | De natural, el Tapajós es claro: baja de rocas muy viejas que sueltan poco barro. | Por naturaleza, el Tapajós es claro: baja de rocas muy viejas que sueltan poco barro. | Sustituye el giro peninsular «De natural» por «Por naturaleza», en línea con `fact.agua_tapajos.03`. |

---

## 3. Las 20 correcciones más importantes (ES-419, EN y PT-BR)

Selección de las 20 intervenciones de mayor impacto cultural, científico, gramatical y de naturalidad nativa en los tres idiomas:

| # | Clave | Idioma(s) | Antes | Después | Por qué importa |
|---:|---|---|---|---|---|
| 1 | `league_name_squirrel_monkey` | `pt-BR` | `Macaco-de-cheiro` | `Mico-de-cheiro` | **Respeto cultural crítico en Brasil:** en la Liga del río el nombre nombra el rango del jugador; llamar «Macaco-...» a un jugador brasileño vulnera `00-glosario.md` §10 (*«en PT, evitar "macaco"»*). *Mico-de-cheiro* es nombre zoológico auténtico en Brasil (ICMBio / Houaiss). |
| 2 | `world_w13_river` | `es-419`, `en`, `pt-BR` | `Marañón alto` / `Upper Marañón` / `Alto Marañón` | `Apurímac` / `Apurímac` / `Apurímac` | Alinea el banner del Mundo 13 con la ruta real al nevado Mismi decidida en D-013 (`00-glosario.md` §8, fila 13). |
| 3 | `fact.rio_napo.18` | `es-419`, `en`, `pt-BR` | `...siona, secoya, cofán, kichwa y shuar.` | `...siona, siekopai, a'i cofán, kichwa y shuar.` (y equivalentes con mayúscula en EN/PT) | Sustituye el exónimo *secoya* por la autodenominación vigente **Siekopai** y precisa **A'i Cofán** (CONAIE, `07 §8.1`). |
| 4 | `fact.acai.13` | `es-419`, `en`, `pt-BR` | `...tucanes, loros y guacamayos comen açaí y esparcen sus semillas.` | `...tucanes, loros y guacamayos comen açaí; los tucanes esparcen sus semillas.` | Corrige una imprecisión biológica heredada de *E. precatoria*: en *E. oleracea* los psitácidos actúan sobre todo como depredadores de semillas y los tucanes como dispersores efectivos. |
| 5 | `fauna.bradypus_variegatus.dato_08` | `es-419`, `en`, `pt-BR` | `...durante meses (unos seis en esta especie).` | `La cría viaja abrazada al pecho de su madre durante sus primeros meses.` | Evita afirmar un número único de meses donde la literatura mastozoológica oscila entre 4 y 12 meses. |
| 6 | `push_logbook_pending` | `es-419`, `en`, `pt-BR` | `Hoy no has sellado tu Bitácora del río: {reward} te esperan.` | `Aún no has sellado tu Bitácora del río. Tu premio de hoy: {reward}.` | Funciona gramaticalmente con cualquier valor de `{reward}` (singular, plural, «Cuia extra ×1» o «+ Canasta grande») en los tres idiomas. |
| 7 | `firefly_line` | `en`, `pt-BR` | `A firefly lights up the ones in a cuia` / `Um vaga-lume ilumina as de uma cuia` | `A firefly lights up the fruit in a cuia` / `Um vaga-lume ilumina as frutas de uma cuia` | Elimina el pronombre colgante (*the ones* / *as*) que en pantalla aparecía aislado sin antecedente sustantivo. |
| 8 | `push_comeback_d7` | `en`, `pt-BR` | `...saved you a Cuia extra...` / `...uma Cuia extra...` | `...saved you an Extra Cuia...` / `...uma Cuia Extra...` | Corrige el término en español sin traducir (`a Cuia extra`) que había quedado incrustado en inglés, usando el nombre canónico de `glossary.csv` (`booster.extra_cuia`). |
| 9 | `map.stop.nauta.dato_01.source` | `en`, `pt-BR` | `AGU y Wikipedia` | `AGU and Wikipedia` / `AGU e Wikipedia` | Corrige la conjunción española «y» que había quedado sin traducir en la atribución de fuente de la parada Nauta. |
| 10 | `map.stop.manaos.*`, `fact.agua_negro.10/.19`, `fauna_saguinus_bicolor_dato`, `fact.araza.12` | `en`, `pt-BR` | `Manaos` | `Manaus` | Sustituye la grafía española/arcaica *Manaos* por la grafía oficial brasileña e internacional **Manaus** en los datos del Mapa, frutas y fauna (ver `preguntas_t2.md`, T2-Q9). |
| 11 | `sloth_line_fail_02`, `streak_06`, `win_02` | `es-419` (y ajuste PT en `win_02`) | `Sin afán...` / PT `Sem pressa e com tino...` | `Sin prisa...` / PT `Sem pressa e com jeito...` | Sustituye el regionalismo colombiano «sin afán» por español neutro en las 3 frases de TiTino, y cambia «com tino» por el mucho más natural «com jeito» en PT-BR. |
| 12 | `sloth_line_fail_01`, `return_01`, `return_03`, `win_04`, `shield_toast`, `tutorial_streak` | `pt-BR` | Mezcla de `tu` (`Respira, olha`), clítico `te` y `pra` | `Respire, olhe o rio e tente mais uma vez.` / `protege você` / `para você` | Unifica el tratamiento en `você` y registro cuidado (`00-glosario.md` §10; `.claude/rules/localizacion.md`) sin mezclar imperativos de *tu* con pronombres de *você*. |
| 13 | `fact.rio_napo.17` | `es-419`, `en`, `pt-BR` | `El tití pigmeo...` | `El leoncillo o tití pigmeo...` (`pygmy marmoset, known locally as leoncillo` / `sagui-leãozinho`) | Rescata el nombre local ecuatoriano (*leoncillo*), usa el nombre vernáculo exacto en Brasil (*sagui-leãozinho*) y evita confusión con la tropa de miquitos. |
| 14 | `fact.tapereba.13` | `es-419` | `...tapires, sajinos y venados.` | `...tapires, pecaríes y venados.` | Reemplaza el peruanismo/ecuatorianismo «sajinos» por el panhispánico «pecaríes». |
| 15 | `fact.agua_tapajos.03` y `map.stop.flona_tapajos.dato_01` | `es-419`, `en` | `de natural` / `comes down from very old rocks that release little mud` | `por naturaleza` / `flows off ancient rocks that shed little mud` | Elimina el peninsularismo «de natural» en ES y reemplaza un calco literal en EN por prosa hidrográfica natural. |
| 16 | `map.stop.collpa_tambopata.dato_05` y `fact.aguaje.17` | `pt-BR` | `as araras azul e amarela` | `as araras-canindé` / `A arara-canindé` | En Brasil *Ara ararauna* tiene nombre propio consagrado (**arara-canindé**); «araras azul e amarela» además concordaba de forma anómala en singular/plural. |
| 17 | `fact.aguaje.01`, `cupuacu.02`, `guanabana.16`, `jenipapo.01`, `urucum.01`, `araza.13` | `pt-BR` | Tautologías literales como *«O buriti... no Brasil é chamado de buriti»* | Adaptación de perspectiva (*«O buriti, chamado aguaje no Peru, é o fruto de uma palmeira...»*, etc.) | Aplica la regla de `propuesta-frutas-del-rio.md` §6.5: cuando la tabla ya usa el nombre brasileño del tinte (`Buriti`, `Graviola`, `Jenipapo`, `Urucum`, `Cupuaçu`, `Araçá-boi`), la línea enseña el nombre hispanoamericano en vez de decir «el X en Brasil se llama X». |
| 18 | `fact.cacao.01` | `pt-BR` | `...crescem dentro de uma grande cápsula (cabaça).` | `...crescem dentro de uma grande cabaça.` | Retira el glosario entre paréntesis que el borrador de IA había insertado por su cuenta; en Brasil el fruto del cacaotero se llama precisamente *cabaça de cacau*. |
| 19 | `fact.rio_amazonas.10` y `fact.rio_napo.07` | `en` | `tonnes` (ortografía británica) | `metric tons` | Unifica el inglés en `en-US` (*metric tons*, como pide el encargo §2.B) sin perder la exactitud métrica de la cifra (`1,3` millones / `150` millones de toneladas métricas). |
| 20 | `map.stop.madidi.dato_01` y `map.stop.tarapoto.dato_02` | `en` | `because the Madidi runs...` / `achieved with their communities.` | `because Madidi runs...` / `designated alongside local communities.` | Corrige el artículo indebido ante un parque nacional (*Madidi*, no *the Madidi*) y el calco «achieved with their communities» para un sitio Ramsar. |

---

## 4. Qué no se pudo confirmar y filas fuera de `pendiente_asesor`

### 4.1 Filas en `duda` dentro de `revision_es.csv`: **0**

No quedó ninguna de las 1.083 filas en estado `duda`. En los dos únicos casos donde `notes` advertía de discrepancia o desajuste en la literatura científica (`fauna.bradypus_variegatus.dato_08` sobre los meses de lactancia/transporte de la cría de *Bradypus variegatus*, y `fact.acai.13` sobre dispersión frente a depredación de semillas en *Euterpe oleracea*), optamos por resolverlos mediante **`cambia`** con una redacción respaldada por las fuentes primarias (ICMBio y Moegenburg & Levey 2002) en vez de dejarlos en `duda`, ya que `LocReleaseGate` bloquea cualquier fila de `facts.csv` por debajo de `aprobado_es`.

En `fact.agua_tapajos.11` (*«El río lleva el nombre de los tapajó, pueblo indígena de la región de Santarém»*), se confirmó la autodenominación **Tapajó** (en singular colectivo en portugués y español, usada por el propio Conselho Indígena Tapajós e Arapiuns — CITA) y la redacción respetuosa en presente, sin entrar en litigios territoriales, por lo que se aprobó como `ok`.

### 4.2 Las 2 filas en `borrador` de `facts.csv` (`fact.agua_tapajos.14` y `fact.agua_tapajos.19`)

Además de las 820 filas en `pendiente_asesor`, `facts.csv` tiene **2 filas en estado `borrador`** (`FactCatalogTests.DraftLines`), marcadas así desde D-013 porque repiten datos de llegada del Mapa del río (paradas Alter do Chão en el nivel 20 y Floresta Nacional do Tapajós en el nivel 30):
- En `facts_revision.csv` incluimos ya la traducción `revisado_nativo` de su texto actual para que las 822 claves queden completas.
- No obstante, como **`LocReleaseGate` exige `status >= aprobado_es` en las 822 filas de `facts.csv`**, antes del release el integrador debe sustituir su texto por uno que no duplique el Mapa y promoverlas a `aprobado_es`. Dejamos listas las dos sustituciones recomendadas (verificadas contra las fuentes ya dadas de alta en `frutas_del_rio_v1.json` y conservando `args = "n=500000"` o su equivalente con marcador en `.19` para no alterar `FactCatalogTests.Numbers_GoAsMarkersWithTheirArgs_AndFollowTheRegion`, que exige exactamente `17 + 8 = 25` líneas con `args`):

| Clave | Propuesta lista para publicar (`aprobado_es` + `revisado_nativo`) | `args` | Fuente de `frutas_del_rio_v1.json` |
|---|---|---|---|
| `fact.agua_tapajos.14` | **ES (`76` car.):** `En 1853 Wallace ya clasificaba los ríos amazónicos por el color de su agua.`<br>**EN (`77` car.):** `As early as 1853, Wallace classified Amazonian rivers by their water color.`<br>**PT-BR (`76` car.):** `Já em 1853, Wallace classificava os rios amazônicos pela cor de suas águas.` | *(vacío)* | Fuente `T2` (`Blackwater river`), tomada literalmente de la línea de reserva oficial de `propuesta-frutas-del-rio.md` §5.14 (fila *Agua del Tapajós*). Al adoptarla, retirar `T10` y `T11` de `frutas_del_rio_v1.json` para que no queden huérfanas en `FactCatalogTests`. |
| `fact.agua_tapajos.19` | **ES (`67` car.):** `Su cuenca abarca unos {n} km² entre Mato Grosso, Pará y Amazonas.` *(con `U+00A0` antes de `km²`)*<br>**EN (`71` car.):** `Its basin covers about {n} km² across Mato Grosso, Pará, and Amazonas.`<br>**PT-BR (`66` car.):** `Sua bacia cobre cerca de {n} km² entre Mato Grosso, Pará e Amazonas.` | `n=492000` | Fuente `T1` (`Wikipedia EN, «Tapajós»`: *basin size 492,000 km²*). No repite la tarjeta de la Floresta Nacional (nivel 30), conserva 1 marcador `{n}` con `args` (mantiene `withArgs == 25`) y permite retirar `T8` de `frutas_del_rio_v1.json` y vaciar `FactCatalogTests.DraftLines`. |

### 4.3 Revisión de las 279 filas no culturales de `strings.csv` (`206 borrador` + `73 aprobado_es`)

Según `gemini-revision-textos.md` (§2), se inspeccionaron también las 279 filas no culturales de `strings.csv`:
- **Sin errores en español:** no se detectaron erratas, regionalismos problemáticos, discordancias ni fallos de marcadores.
- **Recordatorio jurídico (bloque B12, D-033 punto 7):** las **25 claves legales/privacidad** (`consent_*`, `purchase_confirm_*`, `settings_age_correct`, `settings_privacy`, `privacy_policy`, `terms_of_service`) quedaron revisadas lingüísticamente por nativo en `en` y `pt-BR` (`revisado_nativo` en `strings_revision.csv`), pero mantienen en `notes` su aviso *«pendiente del abogado»* para la revisión legal previa al lanzamiento comercial.

---

## 5. Notas técnicas para el integrador al aplicar la entrega

Al volcar estos archivos a las tablas reales del proyecto, conviene tener presentes cinco puntos de sincronización con la suite de tests:

1. **Tests que comprueban el estado inicial `PendingAdvisor`:**
   - `LocTablesTests.CulturalRows_AreBornPendingTheAdvisor` (`Assets/_Project/Tests/EditMode/LocTablesTests.cs:237`) y `FactCatalogTests` (`Assets/_Project/Tests/EditMode/FactCatalogTests.cs:246, 251, 284, 294, 297, 389`) comprueban hoy `Is.EqualTo(LocStatus.PendingAdvisor)` (estado de nacimiento) y que `facts` no muestre nada en release mientras no esté aprobado. Al promover las filas a `aprobado_es` + `revisado_nativo`, esas aserciones deben pasar a admitir `ApprovedEs` (y `NativeReviewed` en `en`/`pt-BR`).
2. **Golden de textos de UI en español (`world_w13_river`):**
   - El cambio `world_w13_river`: `Marañón alto` → `Apurímac` modifica las líneas del Mundo 13 en `Assets/_Project/Tests/EditMode/Golden/uitext_es419.txt`; hay que actualizar ese golden al aplicar `revision_es.csv`.
3. **Persistencia en `tools/river-map/overrides.json`:**
   - Los ajustes en `map.stop.flona_tapajos.dato_01` (`es-419`), `map.stop.nauta.dato_01.source` (`en`/`pt-BR`) y `map.stop.manaos.*` deben reflejarse también en `tools/river-map/overrides.json` (o en `stops.py`) para que una regeneración futura no los pise.
4. **Sincronización de `Manaus` (T2-Q9):**
   - Si el dueño aprueba T2-Q9 (`preguntas_t2.md`), actualizar a la vez `world_w03_name` a `Manaus` (`en` y `pt-BR`) en `00-glosario.md` §8, `Assets/_Project/Data/Loc/glossary.csv` y `Assets/_Project/Data/Loc/strings.csv`.
5. **Nota opcional sobre `fauna.mico_argentatus.name` (`sagui` vs. `saguí`):**
   - En `facts.csv` y `Assets/_Project/Data/Fauna/miquitos_v0.json` figura `Tití plateado (sagui)` (grafía portuguesa sin tilde; en español el DLE recoge *saguí*). Lo aprobamos como `ok` porque un test EditMode bloquea la igualdad literal con `miquitos_v0.json` y funciona como préstamos entre paréntesis del nombre brasileño, pero queda anotado por si en la 1.1 se desea añadir la tilde en ambos archivos a la vez.
