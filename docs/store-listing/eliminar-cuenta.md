# Página web de eliminación de cuenta · ya no vive aquí

**La fuente de verdad pública de la página de borrado es la web**, en los tres idiomas:

| Idioma | Fuente (se edita aquí) | Ruta pública (`infra/web/src/site.ts`) |
|---|---|---|
| es-419 | `infra/web/src/content/es-419/delete.html` | `/tintalu/eliminar-cuenta/` (08 §11; Q-B13-1 (a)) |
| en | `infra/web/src/content/en/delete.html` | `/tintalu/en/delete-account/` |
| pt-BR | `infra/web/src/content/pt-BR/delete.html` | `/tintalu/pt-br/excluir-conta/` |

El formulario en dos pasos lo hacen `infra/web/src/browser/delete-flow.ts` y `account-code.ts` contra `/v1/account/delete-request` y `/v1/account/delete-confirm` (contrato `infra/contracts/v1`). La URL pública es `https://tintalu.com/tintalu/eliminar-cuenta/` (dominio conectado y comprobado el 2026-10-25: D-044, añadido del 2026-10-25).

## Por qué este archivo es ahora solo un puntero

- El borrador del bloque B13 (Spark, 2026-10-20) pedía el «ID de jugador» y dejaba abiertas la verificación (Q-B13-21) y el plazo. La web lo sustituyó con lo decidido:
  - pide el **código de cuenta** que enseña Ajustes (`settings_account_code`), aleatorio de 128 bits y distinto del UID; el servidor solo guarda su hash y el código rota (Q-B13-21 (a); D-035; 08 §11);
  - Ajustes lo muestra en grupos con guiones, y la web lo normaliza antes de enviarlo: quita guiones y espacios, pasa a mayúsculas e I y L → 1, O → 0 (D-033, añadido del 2026-10-22, punto 11; `infra/contracts/v1/common.schema.json`);
  - confirmación en 5 minutos y borrado al momento.
- **Mantener dos versiones las haría divergir.** Cualquier cambio se hace en `infra/web/src/content/*/delete.html`.
- **Historia:** el borrador del Spark sigue en git: `git show 964b64fb:docs/store-listing/eliminar-cuenta.md`.

## Estado (2026-10-30)

- **La web ya no lleva marcas `[[ABOGADO]]`**: el texto sobre la partida del teléfono y la de Play Games cuando el borrado llega por la web (Q-B13-22) y el plazo fiscal del libro de compras (7 años) los cerró la investigación propia (D-057).
- Correo de privacidad: `privacy@tintalu.com` (D-041, añadido del 2026-10-25).
- «Qué se borra» lista lo que borra `infra/functions/delete-account/handler.ts`: la verificación de integridad, el código de cuenta, los premios de anuncios comprobados, el seudónimo, el avatar y los puntos de la Liga del río, y el vínculo entre las compras y la cuenta. Borrar la cuenta no da derecho por sí solo a un reembolso, sin limitar los derechos del consumidor ni la política de reembolsos de Google Play (D-064).
