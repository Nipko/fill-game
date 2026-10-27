# Resumen de la revisión 2 de textos de interfaz (avatares e informes de fallos)

2026-10-27 · Entrega del encargo `docs/dev/traspaso/gemini-revision-textos-2.md` en `docs/dev/traspaso/gemini-revision-2/`, lista para revisión del dueño e integración sobre `Assets/_Project/Data/Loc/strings.csv`.

## 1. Recuento global e intervención en `en` y `pt-BR`

Se revisaron como hablante nativo las **27 claves de interfaz** que entraron en estado `traducido` tras D-054 (**25** de los avatares del jugador, D-056/D-057, y **2** del interruptor opcional de informes de fallos, T-L6/D-057), es decir, **54 celdas** (`27 en` + `27 pt-BR`), todas promovidas a `revisado_nativo`:

| Idioma | Filas revisadas | Sin cambios (`ok`) | Ajustadas | Motivos generales de los ajustes |
|---|---:|---:|---:|---|
| `en` (`en-US`) | 27 | 23 | **4** | Eliminación de calcos del español (`choose it` tras nombre de animal, `in one month` / `in one week`), verbo idiomático de logro (`Earn ... stamps`) y mayúsculas de rango en mitad de frase (`Very Hard level`, alineado con `sloth_line_milestone_02/05/06`). |
| `pt-BR` | 27 | 22 | **5** | Unificación terminológica canónica con el resto de `strings.csv` (`nível`/`níveis` en vez de `fase`/`fases` en 3 hitos, `pontos da Liga` como en `league_victory_points`, y `em Configurações` como en `consent_body` dentro de la misma pantalla). |
| **Total** | **27 filas (54 celdas)** | **45 celdas** | **9 celdas** | 0 errores de longitud (`VisibleLength ≤ max_chars × 1,3`), marcadores, plurales CLDR, `U+00A0` ni cobertura de glifos en Nunito y Baloo 2. |

### Detalle completo de las 9 celdas ajustadas

| # | Clave | Idioma | Antes (`traducido`) | Después (`revisado_nativo`) | Motivo |
|---:|---|---|---|---|---|
| 1 | `avatar_cell_a11y` | `en` | `{animal}. Tap to choose it` | `{animal}. Tap to select` | Evita el pronombre calcado del enclítico español (`elegirlo` → `choose it`) justo detrás del nombre de un animal en TalkBack y empareja la acción con el estado `avatar_chosen_a11y` (`{animal}, selected`). |
| 2 | `avatar_goal_daily_stamps_20_long` | `en` | `Get {n:plural:{} River Challenge stamp\|{} River Challenge stamps} in one month.` | `Earn {n:plural:{} River Challenge stamp\|{} River Challenge stamps} in a month.` | En inglés de EE. UU. los sellos de un reto se *ganan/obtienen* (`Earn ... stamps`), y `in a month` corrige el calco numeral `un → one` alineándose con el texto corto `avatar_goal_daily_stamps_20` (`{} stamps in a month`). |
| 3 | `avatar_goal_league_50_long` | `en` | `Score {n:plural:{} point\|{} points} in the League in one week.` | `Score {n:plural:{} point\|{} points} in the League in a week.` | Corrige el calco `en una semana → in one week` por el giro natural `in a week`, coherente con `in a month`. |
| 4 | `avatar_goal_very_hard_long` | `en` | `Win your first Very hard level.` | `Win your first Very Hard level.` | En mitad de frase antes de un sustantivo común (`level`), el nombre propio de dificultad de dos palabras lleva mayúscula en ambos términos en inglés (`Very Hard level`), igual que las tres líneas ya revisadas de TiTino (`sloth_line_milestone_02`, `05` y `06`: «Your first Very Hard level!») y `00-glosario.md` §6.1. La etiqueta aislada `avatar_goal_very_hard` conserva `Very hard` como `very_hard`. |
| 5 | `avatar_goal_hard_long` | `pt-BR` | `Vença sua primeira fase Difícil.` | `Vença seu primeiro nível Difícil.` | Coherencia terminológica con el resto del juego: `strings.csv` (37 claves revisadas: `Nível {n}`, `Nível concluído!`, `Nível difícil vencido!`) y `glossary.csv` (`skip` = `Pular nível`) llaman siempre **nível** al nivel, no *fase*. |
| 6 | `avatar_goal_very_hard_long` | `pt-BR` | `Vença sua primeira fase Muito difícil.` | `Vença seu primeiro nível Muito difícil.` | Misma unificación `fase → nível`; además restaura la concordancia en masculino con las frases de celebración de TiTino (`sloth_line_milestone_02` y `06`: «Seu primeiro Muito difícil!»). |
| 7 | `avatar_goal_world_long` | `pt-BR` | `Complete {world} ({n:plural:falta {} fase\|faltam {} fases}).` | `Complete {world} ({n:plural:falta {} nível\|faltam {} níveis}).` | Misma unificación `fase/fases → nível/níveis`, idéntica a `home_voyage_levels` (`Falta {} nível\|Faltam {} níveis`), `map_stop_remaining` y `home_sloth_basket`. |
| 8 | `avatar_goal_league_50` | `pt-BR` | `{n:plural:{} ponto de Liga\|{} pontos de Liga}` | `{n:plural:{} ponto da Liga\|{} pontos da Liga}` | En portugués de Brasil *a Liga* lleva artículo definido (`na Liga`, `da Liga`), y `pontos da Liga` coincide letra por letra con `league_victory_points` (`+{} ponto da Liga\|+{} pontos da Liga`), que se muestra junto al avatar en la pantalla de victoria. |
| 9 | `consent_diagnostics_optional` | `pt-BR` | `Os relatórios de falhas ajudam a melhorar o jogo. Você pode desativá-los nas Configurações.` | `Os relatórios de falhas ajudam a melhorar o jogo. Você pode desativá-los em Configurações.` | Unifica con `consent_body` («Você pode mudar isso quando quiser em Configurações.»), que se muestra unos centímetros más arriba en la **misma pantalla de consentimiento**, tratando `Configurações` como nombre propio de pantalla sin artículo (y dejando la longitud cruda en `90` caracteres, dentro incluso de `max_chars = 90`). |

---

## 2. Revisión del español (`es-419`)

**Cambios propuestos en `es-419`: 0.**

Se revisaron las 27 filas en español neutro latinoamericano (`tú`, léxico panhispánico, concordancia de género con «avatar» en las chapas y lecturas de TalkBack, nombres canónicos de sistemas y topónimos, y límites `max_chars`):
- Las 25 claves de avatares son claras, naturales en toda América Latina, concisas en las celdas de 64 dp (`320 dp` y `130 %`) y coinciden con las aserciones literales ya presentes en `AvatarTests.cs`.
- Las 2 claves de diagnóstico (`consent_diagnostics_optional` y `settings_crash_reports`) emplean la fórmula estándar de Android/Google Play en español («informes de fallos» / «Enviar informes de fallos») y mantienen la misma voz y estructura de `consent_body` y del resto de filas en infinitivo de Ajustes.
- Al mantenerse intacto `es-419` en las 27 filas, la entrega se aplica sin ningún conflicto de texto fuente ni invalidación de traducciones.

---

## 3. Dudas y notas breves para el integrador

**Dudas bloqueantes: ninguna (0).** Solo dejamos tres apuntes informativos para la integración:

1. **Observación fuera de alcance en `league_me_no_points` (`pt-BR`):** al auditar `nível` frente a `fase` en toda la tabla `strings.csv` (797 filas), vimos que tras corregir los 3 hitos de avatares queda una única fila antigua (`league_me_no_points`, ya en `revisado_nativo`) con «fase»: *«Vença uma fase para entrar na Liga»*. No se toca aquí por estar fuera de las 27 claves del encargo, pero si el integrador desea uniformidad total con las otras 40 ocurrencias de `nível`, basta cambiarla a `Vença um nível para entrar na Liga`.
2. **Recordatorio legal (bloque B12 / T-L6):** `consent_diagnostics_optional` y `settings_crash_reports` quedan con `en` y `pt-BR` en `revisado_nativo` (listas para `LocReleaseGate`), manteniendo su `status` español en `borrador` junto al resto de claves legales de B12 pendientes del visto bueno final del abogado antes del lanzamiento comercial.
3. **Aplicación con `tools/loc/apply_translations.py`:** `strings_revision.csv` pasa limpio `read_translations(..., review=True)` y `apply(..., review=True)` (0 conflictos, 54 celdas actualizadas a `revisado_nativo`, dejando `0` filas en `traducido` en todo el repositorio). Téngase en cuenta que el subcomando CLI `--review <carpeta>` actual busca también `revision_es.csv` y `facts_revision.csv` (diseñado para el lote completo de D-054), por lo que para esta entrega parcial de `strings` basta invocar `apply(..., review=True)` o hacer opcionales los archivos ausentes en `run_review`.
