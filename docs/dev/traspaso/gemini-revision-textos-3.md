# Encargo para Gemini: revisión 3 (4 textos nuevos de la interfaz)

Encargo del integrador del 2026-10-28. Viene después de la revisión 2 (añadido de D-054, `gemini-revision-textos-2.md`).

Desde la revisión 2 entraron **4 claves nuevas** en `Assets/_Project/Data/Loc/strings.csv`, que son la confirmación neutral al corregir la edad (añadido de D-033 del 2026-10-28; `docs/dev/consentimiento.md` §1.7). En inglés y en portugués de Brasil están en `traducido`: las tradujo una IA y no las ha revisado un nativo. Por eso la compuerta `LocReleaseGate` **vuelve a impedir el build de release**. Es lo único que falta en los textos para el Release definitivo de la 1.0.

## 1. Contexto

El contexto del juego, las lecturas previas y las reglas de forma son los de la revisión 1 (§1 y §2 B de `docs/dev/traspaso/gemini-revision-textos.md`); léelos antes de empezar.

Para estas claves, además:
- **Dónde sale.** En Ajustes › Privacidad › «Corregir mi edad», la edad se puede corregir **una sola vez**. Al elegir una banda distinta de la guardada, antes de guardarla, sale esta confirmación: un título, una línea y dos botones.
- **Tiene que ser neutral.** Sigue la pauta de la pantalla de edad neutral de la FTC (COPPA): es igual para las tres bandas y no empuja ni disuade.
  - No digas qué pasa con ninguna banda: nada de «el juego se bloqueará», «perderás…», «¿seguro que…?».
  - No pongas en un botón más peso que en el otro.
  - Usa el mismo tono que la pantalla de edad (`consent_age_*`, ya revisada).
- **`{band}`** es la etiqueta de la banda elegida, tal como sale en su botón de la pantalla de edad:

  | Banda | es-419 | en | pt-BR |
  |---|---|---|---|
  | `consent_age_under_13` | «Menos de 13» | «Under 13» | «Menos de 13» |
  | `consent_age_13_17` | «De 13 a 17» | «13 to 17» | «De 13 a 17» |
  | `consent_age_18_plus` | «18 o más» | «18 or older» | «18 ou mais» |

  La frase debe leerse bien con las tres.
- **El título** es el mismo texto que la fila de Ajustes `settings_age_correct`. En la revisión 1 quedaron «Correct my age» y «Corrigir minha idade»; mantenlos iguales a esa fila.
- **TalkBack** lee el título, la línea y los dos botones.

## 2. Qué revisar

Las 4 filas de `strings.csv` que tienen `status_en` y `status_pt-BR` = `traducido`:

```
consent_age_confirm_back, consent_age_confirm_body, consent_age_confirm_ok, consent_age_confirm_title
```

Para cada fila:
- **Español (es-419):** está en `borrador`. Si ves un error o una frase poco natural, propón el cambio en el resumen.
- **Inglés y portugués de Brasil:** revísalos como un hablante nativo, con las reglas de la revisión 1, §2 B:
  - el mismo marcador con nombre (`{band}`);
  - una longitud visible de como mucho `max_chars` × 1,3;
  - el tono neutral de arriba.

## 3. Qué entregar

Entrégalo en la carpeta `docs/dev/traspaso/gemini-revision-3/`, o en una rama `spark/revision-textos-3` creada desde `integracion-fase1-fase2`. **No toques las tablas ni el código.** Todos los archivos en UTF-8 sin BOM, con fin de línea LF y en formato RFC 4180, con las filas en orden ordinal por clave.

1. **`strings_revision.csv`**, con el formato que lee `tools/loc/apply_translations.py --review`:
   ```
   key,es-419,en,pt-BR,status_en,status_pt-BR
   ```
   - Pon una fila por cada una de las 4 claves, aunque la traducción no cambie.
   - `status_en` y `status_pt-BR` van en `revisado_nativo`.
   - En `es-419` va el español sobre el que traduces o, si propones cambiarlo, el propuesto.
2. **`resumen.md`**, con:
   - qué cambia y por qué;
   - los cambios de español que propones, con su motivo;
   - las dudas.

El integrador lo valida y lo aplica con `apply_translations.py --review` después de que lo apruebe el dueño, igual que en las revisiones 1 y 2.
