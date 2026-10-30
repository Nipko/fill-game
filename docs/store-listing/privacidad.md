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

## Estado (2026-10-30)

- **La web ya no lleva marcas `[[ABOGADO]]` ni `[[DATO DEL DUEÑO]]`** (lo comprueba `infra/test/web/site.test.ts`): los datos del dueño están puestos (D-041, añadido del 2026-10-25; versión 1, vigente desde el 30 de octubre de 2026; retención de Analytics de 2 meses, Q-B13-19) y lo que quedaba para el abogado lo cerró la investigación propia (D-057; `docs/legal/investigacion-legal-mundial.md` §9).
- Los textos públicos están en firme para la 1.0 (D-064): sin marcas provisionales, con la Liga encendida (y su avatar), el registro de premios de anuncios de 30 días, Play Age Signals y las copias de seguridad del servidor.
- Los anuncios (AdMob con Unity Ads en su mediación, D-049) y Play Games (D-042) ya están en la tabla de datos; la sección 5 solo dice qué redes de anuncios hay hoy.
