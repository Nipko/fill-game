# Traspaso al integrador: pendientes del dueño

Fecha: 2026-10-20. Trabajo documental y revisión de Play Console con el dueño. No incluye integración de SDK, cambios en Unity ni despliegues.

## Confirmado en esta sesión

- El dueño decide continuar con AUTOMATION AI S A S, Colombia, mientras espera el D-U-N-S ya solicitado para su LLC estadounidense.
- Play Console: cuenta Organización, nombre público Automation AI; perfil comercial creado por el dueño.
- Bancolombia: forma de pago principal añadida por el dueño, estado comprobado **En revisión**. No dar los cobros por habilitados hasta la aprobación de Google.
- Dominios: solo el principal por ahora, a cargo del dueño; compra y dominio exacto sin confirmar. Los adicionales se aplazan.
- D-036: el dueño aprueba expresamente `android.permission.INTERNET`. No equivale a aprobar todos los SDK ni a modificar la configuración de Unity.
- Se creó el grupo AUTOMATION AI S A S, solo con la cuenta empresarial. El dueño aplaza asociar su cuenta personal e inscribirse al 15%; no se enviaron solicitudes ni se guardaron respuestas al formulario de asociación.

## Para incorporar y actualizar

1. Integrar este registro y `docs/dev/pendientes-del-dueno.md`; conservar el bloqueo de marca de D-034.
2. Sincronizar las menciones de INTERNET pendiente en `CLAUDE.md`, `docs/dev/plan-backend-aws.md` y el traspaso de publicación con D-036. No se modificaron esos documentos en esta entrega.
3. Revisar las aprobaciones restantes de SDK contra D-035: Firebase Functions sale del plan; Auth, Analytics, Crashlytics, Remote Config y App Check siguen según la decisión y sus añadidos. No volver a pedir alojamiento AWS ni merge de B10, ya aprobados.
4. Próximo paso propuesto: revisar/configurar los proyectos Firebase dev/prod con el dueño. No se abrió ni configuró Firebase en esta sesión.
5. Siguen pendientes el nombre público definitivo del estudio, dictamen de marca, arte, asesoría, revisión legal, QA, firma y configuración de servicios/secretos. Las propuestas de simplificación del checklist siguen siendo propuestas donde no hubo respuesta expresa.

Sin números de cuenta, NIT, documentos bancarios, credenciales ni capturas en el repositorio. Validación de esta entrega: revisión del diff y `git diff --check`; no se ejecutaron builds ni tests del juego porque solo cambia documentación.

## AWS dev — preparación del 2026-10-21

Encargo: `docs/dev/traspaso/codex-aws.md`; guía: `infra/README.md`.

- Checkout principal: `git pull --ff-only origin integracion-fase1-fase2` completado, ya al día. Su rama permanece en `integracion-fase1-fase2`.
- Trabajo aislado en worktree administrado y rama `codex/aws-dev`, desde `origin/integracion-fase1-fase2`, base `8771270`. No se modificó ningún archivo versionado de `infra/`.
- Preparación local: `npm ci --no-audit --no-fund` completado; lint y typecheck correctos; `npm test`: 8 archivos, 101 tests aprobados. `cdk synth --all -c env=dev`, sin variables AWS y con salida temporal fuera del repositorio: correcto, 6 stacks. Esta síntesis no prueba permisos ni despliega recursos.
- AWS CLI v2: no encontrada en PATH ni en las ubicaciones habituales de instalación para sistema/usuario. Los perfiles `af-dev` y `af-prod` no están configurados. Se comprobó únicamente su presencia, sin imprimir datos de configuración ni leer archivos de credenciales.
- La instalación por terminal del MSI oficial para el usuario fue rechazada por la revisión automática con «bloqueado por política», sin motivo más específico. Instalación no completada; pendiente de que el dueño ejecute el instalador oficial. Fuente consultada el 2026-10-21: https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html .
- Consola de AWS abierta para el dueño; sesión cerrada, pendiente de acceso y MFA. No se registran identificadores, correos, credenciales, capturas ni URL de acceso privadas.
- Organizations (gestión/dev/prod), Identity Center, MFA y asignaciones: aún sin comprobar en AWS. No se crearon cuentas ni se ampliaron permisos.
- Bootstrap en `sa-east-1` y `us-east-1`: pendiente. `cdk diff`: pendiente de acceso SSO y perfiles. `cdk deploy`: no ejecutado; requiere revisar el diff y el sí expreso del dueño.
- Presupuesto de dev, SNS, comprobaciones de Web/API/CDN y Firebase dev: pendientes, sin salidas desplegadas que registrar.

Siguiente paso: el dueño completa el acceso de AWS y la instalación de CLI v2. Revisar primero lo que ya existe en Organizations e Identity Center para no duplicarlo; después configurar los perfiles SSO. Solo dev se prepara para despliegue; prod se limita a cuenta/acceso/perfil, sin bootstrap ni despliegue de stacks de prod.
