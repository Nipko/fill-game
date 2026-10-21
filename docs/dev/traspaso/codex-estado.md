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

### AWS Organizations y preparación de SSO — 2026-10-21

- El dueño completó el acceso como root y autorizó preparar el proyecto desde la consola. No se capturaron ni registraron credenciales.
- Organizations no existía en esta cuenta: organización creada y confirmada por la consola, con la cuenta actual como gestión. No se habilitaron servicios de pago ni se desplegaron stacks.
- Formulario de cuenta `amazonfill-dev` preparado, con el rol estándar `OrganizationAccountAccessRole`. Falta que el dueño introduzca en la consola el correo exclusivo de esa cuenta; creación aún no enviada. `amazonfill-prod` aún no creada.
- IAM Identity Center en `sa-east-1` mostraba la opción de habilitar una instancia de organización. Se preparó región única, clave propiedad de AWS y conjuntos de permisos habilitados. Se descartó en el formulario la opción predeterminada multirregión que avisaba de cargos por una clave KMS administrada por el cliente.
- Activación de Identity Center pendiente de confirmación concreta del dueño. No se crearon usuarios, conjuntos de permisos ni asignaciones administrativas; MFA todavía no configurado.
- AWS CLI v2 sigue sin aparecer en las rutas habituales; configuración local de perfiles, bootstrap, diff y deploy siguen pendientes. El acceso root a la consola no sustituye el usuario SSO ni los perfiles locales.

Próximos pasos: completar los correos de las cuentas directamente en la consola, confirmar las operaciones de acceso en su paso final, crear dev/prod, activar Identity Center de organización en la región preparada, configurar usuario y MFA, y asignar los conjuntos de permisos según el encargo. Cada ampliación de acceso requiere confirmación en su pantalla; cada deploy espera el sí del dueño tras el diff. Las cuentas prod y gestión no reciben stacks del juego.

### Identity Center activo y primer intento de cuenta dev — 2026-10-21

- El dueño confirmó expresamente habilitar Identity Center en región única y crear `amazonfill-dev` con el rol estándar `OrganizationAccountAccessRole`.
- Identity Center: instancia de organización creada y confirmada en `sa-east-1`, directorio interno y clave propiedad de AWS. El primer intento usó un formulario anterior a la creación de Organizations y devolvió «The AWS account is already a member of an organization»; se actualizó la página y la activación correcta se completó en la organización existente, sin duplicarla.
- Política de MFA guardada y verificada: solicitar MFA en cada inicio de sesión; exigir registro de un dispositivo al primer acceso. Se permiten aplicaciones de autenticación y claves de seguridad/autenticadores integrados. Esto confirma la política, no que el dueño ya haya registrado su dispositivo.
- Cuenta dev: solicitud enviada tras confirmación; AWS la rechazó con `EMAIL_ALREADY_EXISTS`. **Dev no creada.** Formulario preparado de nuevo con el mismo nombre y rol, pendiente de un correo exclusivo no registrado que el dueño introduce directamente en la consola. No se registró ni reutilizó el correo rechazado fuera de la consola.
- Usuario SSO: formulario preparado con nombre `af-owner` y envío de instrucciones de contraseña por correo, sin generar ni mostrar contraseñas. Falta que el dueño complete sus datos en la consola, revisar y confirmar el alta. Usuario aún no creado, sin grupos ni asignaciones de cuentas.
- Cuenta prod, conjuntos de permisos, MFA personal, perfiles locales, bootstrap, diff y despliegue siguen pendientes. No se han concedido permisos administrativos a usuarios SSO.

### Cuenta dev creada — 2026-10-21

- El dueño introdujo otro correo directamente en la consola. Reintentada la misma creación autorizada de `amazonfill-dev` con `OrganizationAccountAccessRole`; comprobado el resultado en el árbol de Organizations: cuenta dev creada. El fallo de correo anterior queda únicamente como historial y no bloquea ya dev.
- Preparado el formulario de `amazonfill-prod` con el mismo nombre de rol estándar. Falta el correo exclusivo que introducirá el dueño y confirmar su creación. No se desplegarán stacks en prod.
- El formulario del usuario SSO `af-owner` sigue pendiente de completar por el dueño. No hay usuario SSO creado, contraseña generada ni permisos administrativos asignados a dicho usuario.
- Siguiente intervención del dueño: completar el correo exclusivo de prod y los datos del usuario SSO en sus respectivas pestañas, sin compartirlos en el chat ni registrarlos aquí. Después revisar las altas y las asignaciones de permisos, registrar el MFA personal y configurar los perfiles locales.

### Cuenta prod creada; datos del usuario aún pendientes — 2026-10-21

- El dueño confirmó expresamente crear `amazonfill-prod` con el correo introducido por él y `OrganizationAccountAccessRole`. Solicitud enviada y resultado comprobado en Organizations: existen gestión, `amazonfill-dev` y `amazonfill-prod`. Sin bootstrap ni stacks del juego en prod.
- El dueño indicó haber completado `af-owner`, pero la validación de la pestaña de Identity Center muestra únicamente el nombre de usuario completado y devuelve errores por correo, confirmación, nombre, apellidos y nombre de visualización vacíos. No se inventaron ni copiaron datos desde otras cuentas. Se dejó el formulario abierto y se pidió completar esa pestaña; usuario todavía no creado.
- Se mantienen pendientes el alta del usuario, su registro personal de MFA, los conjuntos de permisos y sus asignaciones, CLI/SSO local y todos los pasos de bootstrap/diff/deploy.

### Usuario SSO creado y permisos aprovisionados — 2026-10-21

- El dueño completó los datos de `af-owner` en la consola. Se revisó el alta sin grupos y el dueño confirmó expresamente crear el usuario y enviar instrucciones de contraseña al correo introducido por él.
- Alta correcta y envío de instrucciones confirmados por AWS. No se generó ni se vio ninguna contraseña. Registro personal de MFA y primer acceso SSO aún pendientes de comprobar; la política exige MFA en cada inicio y registro de dispositivo al primer acceso.
- Creado el conjunto de permisos `AdministratorAccess`, política administrada de AWS homónima, sesión de 1 hora y descripción de administración inicial del proyecto.
- Revisada la asignación para un único usuario (`af-owner`) y exactamente dos cuentas (`amazonfill-dev`, `amazonfill-prod`). El dueño confirmó expresamente conceder ese acceso administrativo; AWS mostró «Todas las asignaciones se configuraron correctamente». Cuenta de gestión excluida. Sin grupos ni acceso añadido a otras cuentas.
- AWS CLI v2 sigue sin encontrarse en las ubicaciones habituales. Siguiente paso: el dueño configura contraseña y MFA desde el correo de AWS e instala manualmente la CLI v2 oficial. Después configurar `af-dev` y `af-prod`, comprobar que cada perfil corresponde a su entorno sin imprimir identificadores y preparar bootstrap solo en dev.
- Bootstrap, diff, deploy, presupuesto, SNS y comprobaciones del servicio aún no ejecutados. Ningún recurso del juego desplegado en dev, prod ni gestión.

### Primer acceso informado e instalación de CLI pendiente — 2026-10-21

- El dueño informa haber configurado su contraseña y MFA. Aún falta comprobar el acceso SSO a dev/prod y el dispositivo registrado desde la consola, sin ver credenciales ni códigos.
- A petición expresa del dueño, se reintentó instalar AWS CLI v2 con el MSI oficial para el usuario y validación de firma de Amazon prevista antes de ejecutarlo. La revisión automática volvió a rechazar el comando completo antes de su ejecución: «blocked by policy», sin más detalle. No se instaló la CLI ni se descargó el MSI mediante ese comando.
- Se intentó dejar la descarga oficial en el navegador para ejecución manual del dueño; el control de descarga del navegador tampoco concedió acceso por falta de contexto de origen. No se eludieron los controles ni se usaron métodos alternativos para ejecutar software.
- Pendiente: el dueño descarga y ejecuta manualmente el instalador oficial para el usuario desde https://awscli.amazonaws.com/AWSCLIV2-User.msi . Después comprobar la versión y configurar SSO. No hay nuevos cambios de permisos ni despliegues.

### CLI instalada y perfiles SSO preparados — 2026-10-21

- El dueño instaló manualmente AWS CLI v2. Verificada la versión `2.38.3` desde el ejecutable de su instalación de usuario.
- Configurados `af-dev` y `af-prod` con una sesión compartida de IAM Identity Center en `sa-east-1`, rol `AdministratorAccess` y región predeterminada `sa-east-1`. Configuración solo en el archivo local de AWS, fuera del repositorio, conservando los perfiles anteriores; sin claves de acceso IAM.
- Iniciado `aws sso login --profile af-dev`; pendiente de que el dueño complete el acceso y MFA en el navegador. Todavía no se verificó la identidad efectiva de los perfiles mediante la CLI.
- Bootstrap en ambas regiones de dev, diff y deploy siguen pendientes. Sin recursos desplegados ni cambios en `infra/`.

- Acceso SSO completado correctamente. Verificados ambos perfiles con STS: cada uno coincide con su cuenta esperada y utiliza el rol de Identity Center autorizado. No se imprimieron identificadores ni ARNs. La sesión compartida permite acceder a ambos perfiles; no se usaron claves IAM.

### Bootstrap dev completado y diff revisado — 2026-10-21

- El dueño confirmó crear el bootstrap exclusivamente en dev, en `sa-east-1` y `us-east-1`, tras revisar los recursos, permisos y posible coste.
- Ejecutado bootstrap estándar de CDK mediante `af-dev` en ambas regiones. Comprobado `CDKToolkit` en estado `CREATE_COMPLETE` en las dos. Sin cuentas de confianza adicionales para despliegue ni consultas; rol de ejecución con la política administrativa estándar previamente confirmada. Sin bootstrap en prod o gestión.
- Ejecutado `cdk diff` con `env=dev`, cuenta explícita y destinatario de avisos autorizado (el de af-owner). `--no-change-set` evitó crear un change set; ensamblado, plantillas y salida privada guardados fuera del repositorio. Salida 0 y diferencias en los seis stacks esperados. Sin deploy.
- Observación para el integrador: la CLI instalada de CDK avisa que `diff --all` es una opción desconocida y la ignora; aun así revisó los seis stacks. No se modificó `infra/`.
- Hallazgo previo al despliegue: la ficha de Identity Center de af-owner muestra `Dispositivos MFA (0)` y correo sin verificar, pese al MFA informado por el dueño. Hay una sesión SSO activa y ambos perfiles funcionan, pero falta resolver y comprobar el MFA propio de af-owner antes de desplegar. El MFA de root, si fue el registrado, es independiente.
- El diff añade el presupuesto mensual de 10 USD, datos, API con funciones de esqueleto, web, CDN y alarmas. Presupuesto, suscripción SNS y pruebas de servicios siguen pendientes del deploy y de la autorización expresa del dueño.
