# Encargo para Gemini: revisión 2 (27 textos nuevos de la interfaz)

Encargo del integrador del 2026-10-27, que sigue a la revisión 1 (D-054, `gemini-revision-textos.md`).

Desde la revisión 1 han entrado **27 claves nuevas** en `Assets/_Project/Data/Loc/strings.csv`:
- los avatares del jugador (D-056 y D-057, `docs/dev/avatares.md`);
- el interruptor «Enviar informes de fallos» (T-L6, D-057).

En inglés y en portugués de Brasil están en `traducido`: las tradujo una IA y no las ha revisado un nativo. Por eso la compuerta `LocReleaseGate` **vuelve a impedir el build de release**.

## 1. Contexto

El contexto del juego, las lecturas previas y las reglas de forma son los de la revisión 1, §1 y §2 B (`docs/dev/traspaso/gemini-revision-textos.md`). Léelos antes de empezar. Para estas claves, además:
- **Avatares.** Son 16 animales de la Amazonía que el jugador elige como retrato. Los nombres de los animales **no** están en estas claves: son los `league_name_*`, que ya se revisaron. Los textos `avatar_goal_*` explican el hito que desbloquea un avatar. La versión corta va en la chapa de la rejilla; la `_long`, en el globo que sale al tocar un avatar bloqueado. Los textos `*_a11y` los lee TalkBack: frases completas y claras.
- **Informes de fallos.** `settings_crash_reports` es el nombre del interruptor en Ajustes. `consent_diagnostics_optional` es la frase de la pantalla de consentimiento que explica que se pueden desactivar.

## 2. Qué revisar

Las 27 filas de `strings.csv` con `status_en` o `status_pt-BR` = `traducido`:

```
avatar_cell_a11y, avatar_cell_new_a11y, avatar_chosen_a11y,
avatar_goal_daily_stamps_20, avatar_goal_daily_stamps_20_long,
avatar_goal_daily_streak_7, avatar_goal_daily_streak_7_long,
avatar_goal_hard, avatar_goal_hard_long, avatar_goal_league_50, avatar_goal_league_50_long,
avatar_goal_streak_10, avatar_goal_streak_10_long, avatar_goal_very_hard, avatar_goal_very_hard_long,
avatar_goal_world, avatar_goal_world_long, avatar_header_a11y, avatar_home_a11y,
avatar_home_new_a11y, avatar_league_alias, avatar_locked_a11y, avatar_new,
avatar_settings_row, avatar_title, consent_diagnostics_optional, settings_crash_reports
```

Para cada fila:
- **Español (es-419).** Las claves de avatares están en `borrador`. Si ves un error o una frase poco natural, propón el cambio en `notas`. No hay contenido cultural: los nombres de los animales están en otras claves.
- **Inglés y portugués de Brasil.** Revísalos como un hablante nativo, según las reglas de la revisión 1, §2 B:
  - los mismos marcadores con nombre (`{animal}`, `{n}`…);
  - los plurales con las categorías CLDR;
  - una longitud visible de como mucho `max_chars` × 1,3;
  - el tono de la tabla.
  - **«Ajustes»:** en portugués la pantalla se llama «Configurações».

## 3. Qué entregar

Entrégalo en la carpeta `docs/dev/traspaso/gemini-revision-2/` o en una rama `spark/revision-textos-2` (desde `integracion-fase1-fase2`). **No toques las tablas ni el código.** Todo en UTF-8 sin BOM, con fin de línea LF, en formato RFC 4180 y en orden ordinal por clave:

1. **`strings_revision.csv`**, con el formato que lee `tools/loc/apply_translations.py --review`:
   ```
   key,es-419,en,pt-BR,status_en,status_pt-BR
   ```
   Una fila por cada una de las 27 claves, aunque la traducción no cambie. `status_en` y `status_pt-BR` van en `revisado_nativo`. En `es-419`, pon el español sobre el que traduces; si propones cambiarlo, el propuesto.
2. **`resumen.md`**, con:
   - cuántas filas cambian y por qué, a grandes rasgos;
   - los cambios de español que propones, con su motivo;
   - las dudas.

El integrador lo valida y lo aplica con `apply_translations.py --review`, tras la aprobación del dueño, igual que la revisión 1.
