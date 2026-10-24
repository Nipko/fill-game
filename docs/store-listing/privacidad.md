# Política de privacidad · ya no vive aquí

**La fuente de verdad pública de la política de privacidad es la web**, en los tres idiomas:

| Idioma | Fuente (se edita aquí) | Ruta pública (`infra/web/src/site.ts`) |
|---|---|---|
| es-419 | `infra/web/src/content/es-419/privacy.html` | `/privacidad/` |
| en | `infra/web/src/content/en/privacy.html` | `/en/privacy/` |
| pt-BR | `infra/web/src/content/pt-BR/privacy.html` | `/pt-br/privacidade/` |

`infra/web/site/` es la salida generada de esas fuentes: no se edita a mano.

## Por qué este archivo es ahora solo un puntero

- El borrador del bloque B13 (Spark, 2026-10-20) se escribió antes de la web, con lo que entonces se sabía: titular `[PENDIENTE]`, Unity IAP y Unity Authentication, borrado por «ID de jugador» y todos los SDK como previstos.
- La web (D-041, hecha por el equipo de Claude en `infra/web/`) lo sustituyó con lo que hoy es real: Parallext, LLC como responsable, Firebase con Analytics por consentimiento, Crashlytics y Remote Config, el backend en AWS `sa-east-1`, el plugin propio de Play Billing (D-040), el código de cuenta y la banda `<13` sin SDK (D-033, añadido del 2026-10-23).
- **Mantener dos versiones las haría divergir**, y la que lee el jugador, el abogado y Play Console es la de la web. Por eso aquí no queda texto legal: cualquier cambio de la política se hace en `infra/web/src/content/*/privacy.html` (y sale con el despliegue de la web).
- **Historia:** el borrador del Spark sigue en git. Para leerlo: `git show 964b64fb:docs/store-listing/privacidad.md`.

## Lo que sigue pendiente (lo marca la propia web)

- `[[DATO DEL DUEÑO]]`: estado de constitución y domicilio de Parallext, LLC; correo de contacto y de privacidad; persona o cargo de protección de datos (LGPD); representante, si el abogado lo pide; versión y fecha de entrada en vigor; retención de Analytics (Q-B13-19).
- `[[ABOGADO]]`: bases legales por finalidad, transferencias internacionales, plazo fiscal, plazos de respuesta, adolescentes (LGPD art. 14, ECA Digital) y los registros de diagnóstico de Play Billing (Q-BILL-2).
- **Al integrar D-042** (MAX con UMP y Play Games): pasar los anuncios y el guardado en la nube de «Lo que todavía no está en el juego» a la tabla de datos, a la vez que `data-safety.md` y 08 §13.3.
