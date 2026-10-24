# Corrección de fechas locales y lotes offline de la Liga

24-10-2026. El primer despliegue aprobado de Data/Api/Ops terminó correctamente. Durante la comprobación se detectó que la validación inicial exigía igualdad entre la fecha local de inicio del Reto y la fecha UTC de su victoria. Esa restricción no corresponde a 06 §4.1 / R-Q3 y puede rechazar victorias legítimas en otros husos o al cruzar medianoche.

## Cambios preparados

- Aceptar el día local de inicio del Reto sin equipararlo a la fecha UTC de `won_at`; validar que exista en el calendario y pueda solaparse con la semana. No cambian los campos ni el contrato de `docs/dev/liga.md`.
- Consultar la semana anterior también cuando un lote de la primera hora del lunes llega días después. Mantener 40 victorias/hora en ese borde y evitar premiar dos veces un mismo ID diario al cruzar la semana. La campaña sigue puntuando una vez por nivel y semana.
- Regresiones: fecha local anterior/siguiente a UTC, cruce de medianoche y del lunes, fechas inválidas/remotas, duplicado diario entre semanas y lote offline del lunes recibido el viernes.

Commits: `eb52e32e` y `85109b38`. Lint, tipos, 337 pruebas / 19 archivos, tabla de 700 niveles, web:check y synth de prod: correctos.

## Diff de infraestructura

Comparados los JSON del ensamblado aprobado y el nuevo: **Data y Ops son idénticos**. Api solo cambia `Code.S3Key` y `Metadata.aws:asset:path` de `LeagueWinsFunction03C155AC`, `LeagueBoardFunction622B45C1` y `DeleteAccountFunction50EF2EBA` (comparten el módulo de Liga; en borrado el cambio puede residir en el sourcemap). No hay nuevas tablas, rutas, políticas IAM, secretos, alarmas ni cambio de coste fijo.

El [diff estricto frente a AWS](liga-dev-calendario-diff.txt) también muestra diferencias repetidas de representación de caracteres en descripciones/metadata de recursos ya existentes. No son cambios de sus valores de secretos, permisos o destinatarios. No se propone volver a desplegar Ops por esas diferencias cosméticas. Los hashes del nuevo ensamblado están en [liga-dev-calendario-assembly.sha256.json](liga-dev-calendario-assembly.sha256.json).

**Acción aplicada, 24-10-2026:** el dueño aprobó «Sí, actualizar AfDevApi». Se desplegó únicamente Api desde el ensamblado inventariado: exit 0, UPDATE_COMPLETE. Los tres paquetes Lambda activos se compararon byte por byte con los aprobados: idénticos. Las rutas sin token responden 401 y las 27 alarmas están OK. Resultado y límites de la prueba autenticada en `codex-estado.md`.
