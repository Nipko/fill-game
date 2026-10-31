# Textos legales publicados el día del lanzamiento (2026-10-31)

Copia de lo que servía `https://tintalu.com` cuando se publicó la 1.0.0 (10003) en producción (D-059; `publicar-play-console.md` §18, «Archivar los textos legales mostrados»). Se descargaron con `curl` el 2026-10-31.

- **Páginas:** privacidad, términos, eliminar cuenta y créditos en es-419, en y pt-BR, más `app-ads.txt`.
- **Coincidencia con el repositorio:** el texto de privacidad y términos coincide entero con `infra/web/src/content/*/privacy.html` y `terms.html` del commit `041b18ff` (último cambio de `infra/web/src`, 2026-10-30). Las páginas de eliminar cuenta solo difieren en el formulario, que la página expande en lugar del marcador `{{delete-form}}`, y en la cabecera y el pie del sitio.
- No se editan: si la web cambia, se archiva una carpeta nueva con su fecha.
