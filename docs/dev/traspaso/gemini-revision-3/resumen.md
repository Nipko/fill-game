# Resumen de la revisión 3 de textos de interfaz (confirmación neutral de edad)

2026-10-28 · Entrega del encargo `docs/dev/traspaso/gemini-revision-textos-3.md` en `docs/dev/traspaso/gemini-revision-3/`, lista para revisión del dueño e integración con `python tools/loc/apply_translations.py --review docs/dev/traspaso/gemini-revision-3`.

## 1. Qué cambia y por qué (`en` y `pt-BR`)

Se revisaron como hablante nativo las **4 claves nuevas** de la confirmación neutral de «Corregir mi edad» (`8` celdas `en` + `pt-BR`, todas promovidas a `revisado_nativo`), verificando neutralidad COPPA/FTC en las tres bandas (`consent_age_under_13`, `consent_age_13_17`, `consent_age_18_plus`), igualdad literal del título con `settings_age_correct`, simetría de peso entre ambos botones, lectura en TalkBack, marcadores (`{band}`), longitud (`VisibleLength ≤ max_chars × 1,3`) y cobertura de glifos en los atlas SDF de Nunito y Baloo 2:

| Idioma | Filas revisadas | Sin cambios (`ok`) | Ajustadas | Resumen |
|---|---:|---:|---:|---|
| `en` (`en-US`) | 4 | 3 | **1** | Corrección de antecedente pronominal en `consent_age_confirm_body` (`it` → `this`), alineado con `delete_account_confirm_body`. |
| `pt-BR` | 4 | 4 | **0** | Las 4 traducciones son naturales, neutrales, mantienen el tratamiento `você`, coinciden con `settings_age_correct` y `delete_account_confirm_body` (`Não dá para desfazer`), y encajan con las tres etiquetas de `{band}`. |
| **Total** | **4 filas (8 celdas)** | **7 celdas** | **1 celda** | 0 conflictos al aplicar con `apply_translations.py --review`; `0` filas pendientes en todo `strings.csv` (801) y `facts.csv` (822). |

### Detalle del único cambio realizado

| Clave | Idioma | Antes (`traducido`) | Después (`revisado_nativo`) | Motivo |
|---|---|---|---|---|
| `consent_age_confirm_body` | `en` | `You chose: {band}. Your age can only be corrected once, and it can't be undone.` | `You chose: {band}. Your age can only be corrected once, and this can't be undone.` | En la oración coordinada `Your age can only be corrected once, and it can't be undone`, el pronombre `it` toma como antecedente gramatical `Your age` («tu edad no se puede deshacer»). Usar el demostrativo `this` refiere limpiamente a la acción de corregirla, suena natural con las tres bandas (`Under 13`, `13 to 17`, `18 or older`), mantiene intacto el tono neutral y coincide con la fórmula ya revisada en `delete_account_confirm_body` (`This can't be undone.`). |

---

## 2. Cambios propuestos en español (`es-419`)

**Ninguno (0).**

Las 4 claves en español (`Volver a elegir`, `Elegiste: {band}. La edad solo se puede corregir una vez y no se puede deshacer.`, `Confirmar` y `Corregir mi edad`) son claras, naturales en español neutro latinoamericano (`tú`), estrictamente neutrales para las tres bandas (`Menos de 13`, `De 13 a 17`, `18 o más`), mantienen el mismo título que `settings_age_correct` y la misma fórmula que `delete_account_confirm_body` (`No se puede deshacer`).

---

## 3. Dudas

**Ninguna (0).** Como el resto del bloque B12 (`Q-B12-5`), las 4 filas conservan su `status` español en `borrador` con la nota de revisión legal del abogado, mientras que `status_en` y `status_pt-BR` quedan en `revisado_nativo` para desbloquear `LocReleaseGate`.
