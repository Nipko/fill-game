# Traspaso al integrador: pendientes del dueño y AWS dev

Actualizado: 2026-10-21. AWS dev desplegado y comprobado con autorización del dueño, desde la rama `codex/aws-dev`. Los apartados siguientes conservan el historial; sus pendientes quedan sustituidos por las verificaciones posteriores.

## Estado actual de AWS dev

- Organizations con gestión, dev y prod; Identity Center en `sa-east-1`; `af-owner` con MFA verificado y acceso autorizado a dev/prod mediante SSO. Perfiles locales `af-dev` y `af-prod` comprobados.
- Bootstrap solo en dev, en `sa-east-1` y `us-east-1`. Funciones reales y endurecimiento F2c redesplegados con autorización: API en `UPDATE_COMPLETE` y los otros cinco stacks en `CREATE_COMPLETE`. Pruebas sin autenticar: compras devuelve 401 y SSV sin parámetros/firma devuelve 400; 19 alarmas en OK. Las verificaciones previas de web y CDN también pasaron.
- Presupuesto de 10 USD mensuales con avisos al 50, 80 y 100 % verificado; suscripción SNS confirmada. El presupuesto no limita el gasto.
- Las tres URLs públicas figuran en «Dev desplegado y comprobado»; el resultado del redespliegue está en «Funciones reales desplegadas en dev». Firebase dev se creó después en la cuenta personal por decisión del dueño: proyecto `fill-dev-e75a2`, Android `com.amazonfill.dev`, Auth anónimo y App Check registrado sin enforcement. Ajuste de AWS al ID real pendiente de aprobación; detalles al final. Sin cambios propios en `infra/` ni cambio de rama del checkout principal. Secretos reales e integración del cliente pendientes; prod no tiene bootstrap ni stacks del juego.

## Registro inicial de Play Console — 2026-10-20

Trabajo documental y revisión de Play Console con el dueño. Este registro inicial no incluye integración de SDK, cambios en Unity ni despliegues.

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

### MFA personal verificado y despliegue preparado — 2026-10-21

- Después de la intervención del dueño, recargada la ficha de af-owner: un dispositivo MFA registrado, de tipo clave de seguridad/autenticador integrado. La advertencia de correo sin verificar ya no aparece. Portal personal accesible con las dos cuentas del proyecto.
- Verificada la política guardada de Identity Center: MFA en cada inicio de sesión; registro obligatorio de dispositivo para quienes no lo tengan. No se modificó la política ni se vieron contraseñas, códigos o claves.
- Revisión del ensamblado del diff (fuera del repo): 106 recursos en seis stacks: Global 1, Data 6, Api 52, Web 15, Cdn 10 y Ops 22. Incluye seis tablas, cinco funciones de negocio de esqueleto, tres secretos de relleno, dos distribuciones CloudFront, cuatro buckets, 19 alarmas y avisos por correo. Lambda, capa y rol adicionales de Web corresponden al despliegue interno del sitio.
- IAM del juego: roles para las funciones, Scheduler y despliegue del sitio; acciones de DynamoDB, lectura de secretos y escritura de logs por función; Scheduler invoca el job y puede enviar a su DLQ; despliegue web sincroniza los buckets de assets y sitio, incluyendo borrado de objetos. Sin permisos administrativos nuevos para usuarios en el diff del juego. CloudFront lee S3 mediante OAC; buckets privados. No se configura dominio propio.
- Pendiente del sí explícito del dueño para `cdk deploy` de los seis stacks dev del diff revisado. El presupuesto de 10 USD es una alerta, no un límite que detenga el gasto. Tras deploy: confirmar SNS, verificar presupuesto y ejecutar comprobaciones rápidas. Prod permanece sin bootstrap ni stacks.

### Despliegue dev autorizado e iniciado — 2026-10-21

- El dueño confirmó expresamente desplegar los seis stacks dev después de revisar el diff, IAM, acceso por URLs de AWS, avisos y posible coste.
- Antes de ejecutar, comprobadas la identidad SSO de dev y las seis entradas del manifiesto contra los entornos y nombres esperados. Iniciado `cdk deploy --all` con el ensamblado exacto revisado mediante `--app`, perfil af-dev y aprobación interactiva desactivada únicamente para materializar el sí recibido. Sin nueva síntesis ni cambios de código entre diff y deploy. Ensamblado, salida privada y outputs fuera del repositorio.
- Despliegue en curso; no considerar aún disponibles los servicios. No se ejecutaron operaciones en prod.

- Verificación directa de AWS Budgets tras `AfDevGlobal CREATE_COMPLETE`: presupuesto `af-dev-monthly` de 10 USD mensuales y tres notificaciones al 50 %, 80 % y 100 %. Los tres suscriptores EMAIL coinciden con el destinatario autorizado. No se guardó el correo ni el identificador de cuenta en el repo.

- Comprobaciones parciales del despliegue: CDN `/catalog.json` devuelve 403; API `GET /v1/ssv` devuelve 501 y `POST /v1/purchases/verify` sin token devuelve 401, como pide el README para los esqueletos. `AfDevData`, `AfDevApi`, `AfDevCdn` y `AfDevOps` completados sin fallos observados. Suscripción SNS al destinatario autorizado localizada como `PendingConfirmation`; se pidió al dueño confirmar el correo sin compartir su enlace.

### Dev desplegado y comprobado — 2026-10-21

- Deploy finalizado con salida 0. Verificados directamente los seis stacks en CloudFormation: AfDevGlobal en us-east-1 y AfDevData, AfDevApi, AfDevWeb, AfDevCdn y AfDevOps en sa-east-1, todos CREATE_COMPLETE.
- Web comprobada: página de preparación HTTP 200, app-ads.txt HTTP 200 y robots.txt HTTP 200 con Disallow de raíz. Cabecera X-Robots-Tag con noindex. Las pruebas previas de API (501 y 401) y catálogo CDN (403) pasaron durante el mismo despliegue.
- El dueño confirmó la suscripción SNS por correo. Verificada después en AWS como confirmada y con el destinatario autorizado. Presupuesto mensual y tres avisos ya comprobados mediante AWS Budgets.
- ApiUrl: https://4tbucfarv9.execute-api.sa-east-1.amazonaws.com
- WebUrl: https://d15n2u2htrstr6.cloudfront.net
- CdnUrl: https://d1a7tqws4dhu0f.cloudfront.net
- Sin cambios de código en infra/, sin dominio propio ni recursos del juego en prod. Firebase dev y carga de secretos reales no realizados; las funciones desplegadas siguen siendo los esqueletos del encargo. El presupuesto es un aviso y no bloquea el gasto.

## Actualización con funciones reales y Firebase dev — 2026-10-21 (en curso)

- A petición del dueño, pull de origin/integracion-fase1-fase2 en el worktree de codex/aws-dev: avance rápido hasta e80c3930. Checkout principal sin cambio de rama. Entraron funciones reales, CI y endurecimiento F2c del integrador; no se editó infra/.
- npm ci completado; lint y typecheck correctos; 14 archivos de test, 219 pruebas aprobadas. Sesión SSO af-dev comprobada contra la cuenta esperada sin imprimir identificadores.
- Diff preliminar con --no-change-set: solo AfDevApi cambia; los otros cinco stacks no tienen diferencias. Cambian código/configuración de las cinco funciones y cinco políticas IAM. Nuevas acciones: ConditionCheckItem para verifyPurchase, rewardedSsv y deleteAccount; BatchGetItem para restoreLedger; GetItem para rewardedSsv. Recursos de tablas/índices ajustados según F2c y ninguna declaración Allow de esas políticas usa Resource '*'. Sin sustitución de tablas ni cambio de presupuesto o distribución. Falta el diff final con los identificadores de Firebase realmente creados.
- El dueño confirmó aceptar las Condiciones de Firebase en este paso. Formulario preparado con nombre amazon-fill-dev. La consola rechazó el ID canónico y el alternativo con sufijo de la empresa; el ID sugerido por Firebase, fill-dev-78702, pasó validación, pero el proyecto aún no se ha creado.
- Selector de organización/recurso superior de Firebase vacío. Al comprobar Google Cloud apareció una verificación de identidad de la organización; se dejó abierta para que el dueño introduzca la contraseña directamente. No se accedió a contraseñas ni códigos. Creación/configuración de Firebase y deploy aún pendientes.
- Las salidas del diff y el ensamblado se guardaron fuera del repositorio. Ningún nuevo deploy autorizado ni ejecutado; se pedirá el sí del dueño después del diff final. Secretos reales sin cargar.

- El dueño informó haber completado la verificación de Google Cloud; al comprobar el resultado, la consola estaba en otra sesión de Google. Se alineó con la cuenta empresarial activa en Firebase y Google volvió a exigir verificación de identidad para esa cuenta. Pantalla dejada al dueño, sin introducir ni leer credenciales. No se creó ningún proyecto en la sesión distinta ni se autorizó un deploy.

### Cuenta empresarial: cupo comprobado y bloqueo real — 2026-10-21

- El dueño aclaró que la cuenta empresarial se abrió para verificarla y eligió revisar su cupo antes de crear Firebase. El formulario iniciado en la cuenta personal no se envió: ningún proyecto ni cuenta de Analytics creados y ningún acceso de propietario concedido. Esa vía no se continúa.
- Verificada la sesión empresarial en Google Cloud. El formulario Proyecto nuevo muestra 12 proyectos restantes en la cuota. No se solicitó aumento ni se borró ningún recurso.
- El selector Recurso superior solo ofrece Sin organización y muestra expresamente que falta resourcemanager.projects.create para crear allí. Esto explica el bloqueo de creación observado; no se atribuye ya a falta de cupo.
- Pendiente identificar quién administra Workspace/Cloud Identity y el recurso de organización/carpeta donde se debe crear dev. La asignación de permisos, si hiciera falta, se revisará y confirmará antes de aplicarla. Sin cambios de IAM en Google Cloud, proyecto Firebase sin crear y redeploy AWS aún sin autorización.

- El dueño confirma que otra persona administra Workspace/Cloud Identity. Se dejó en el chat un texto para solicitar al administrador la ubicación correcta y el rol Creador de proyectos para la cuenta empresarial, o la creación del proyecto con acceso posterior; no se envió ningún mensaje a terceros ni se modificaron permisos.
- Revisado el ensamblado conservado del diff: solo AfDevApi tiene cambios. Se ofreció redesplegar las funciones independientemente del bloqueo de Firebase, explicando que el contexto actual sigue esperando amazon-fill-dev y que deberá ajustarse si el ID final cambia. Pendiente del sí explícito del dueño; no se ejecutó deploy.

### Redespliegue dev con funciones reales autorizado — 2026-10-21

- El dueño confirmó expresamente redesplegar dev con el diff que solo cambia AfDevApi, mientras el administrador resuelve el acceso para Firebase.
- Comprobadas identidad SSO y seis entornos del manifiesto. Iniciado deploy desde el ensamblado exacto del diff aprobado, conservado fuera del repositorio. Contexto Firebase aún amazon-fill-dev; no se creó ningún proyecto Firebase ni se cargaron secretos reales.
- Pendientes al terminar: estados de stacks, 401 sin token en compras, rechazo 4xx del SSV sin firma y alarmas. No considerar todavía terminada la actualización.

### Funciones reales desplegadas en dev — 2026-10-21

- Redespliegue aprobado finalizado con salida 0 desde el ensamblado exacto revisado (base de código e80c3930, funciones reales y endurecimiento F2c). AfDevApi verificado en UPDATE_COMPLETE; Global, Data, Web, Cdn y Ops siguen CREATE_COMPLETE. Solo se actualizó API; no hay cambios en tablas, presupuesto, web o CDN.
- Pruebas HTTP posteriores: GET /v1/ssv sin parámetros/firma devuelve 400 (ya no 501); POST /v1/purchases/verify sin token devuelve 401. Consulta de CloudWatch posterior: las 19 alarmas af-dev están OK.
- Se mantienen las URLs públicas ya documentadas. Contexto Firebase aún amazon-fill-dev, paquete de pruebas com.amazonfill.dev y verificador falso habilitado solo en dev conforme al código aprobado. Firebase todavía no existe y no se cargaron secretos reales: faltan pruebas autenticadas, compras y callbacks reales.
- Firebase empresarial sigue pendiente del administrador externo: hay 12 proyectos de cupo, pero falta resourcemanager.projects.create en una ubicación permitida. No se creó el proyecto en la cuenta personal ni se concedieron permisos en Google. Cuando exista Firebase, registrar app Android, activar Auth anónimo y configurar los productos pedidos; si cambia el ID, preparar otro diff y solicitar aprobación para ajustar la API.
- Sin ediciones de infra/, sin bootstrap/deploy en prod y sin cambio de rama del checkout principal. Ensamblados y salidas privadas fuera del repositorio.

### Firebase dev desde la cuenta personal por decisión del dueño — 2026-10-21

- El dueño cambió expresamente la instrucción: crear al menos dev desde su cuenta personal. Retomado el formulario de Firebase en esa sesión; no se actúa en la cuenta empresarial ni se cambian sus permisos.
- Preparado nombre amazon-fill-dev e ID sugerido disponible fill-dev-e75a2. Gemini desactivado; Analytics activado con cuenta dedicada, ubicación Estados Unidos y ajustes predeterminados. Pendiente confirmación legal de las condiciones de Analytics antes de crear el proyecto. Los IDs de formularios anteriores fueron propuestas no creadas.
- Preparación de App Check: comprobado un APK dev existente, paquete com.amazonfill.dev y firma válida, mediante aapt2 y apksigner. Extraída únicamente la huella pública SHA-256 del certificado del APK para registrar la app de dev; no se leyó ningún keystore. Huella y parámetros locales fuera del repo; aún no se registró App Check.

### Firebase dev creado en la cuenta personal — 2026-10-21

- Tras la confirmación expresa del dueño, aceptadas las condiciones de Google Analytics y creado Firebase dev en la cuenta personal elegida. Nombre amazon-fill-dev; ID real del proyecto: fill-dev-e75a2. El ID canónico amazon-fill-dev no estaba disponible en los formularios anteriores; no se creó ningún proyecto con los IDs propuestos previamente.
- Plan Spark comprobado (sin coste mensual), Analytics Enabled con cuenta dedicada amazon-fill-dev, ubicación Estados Unidos y ajustes predeterminados autorizados. Gemini desactivado durante la creación. Sin cuenta de facturación de pago vinculada por esta sesión.
- App Android registrada: paquete com.amazonfill.dev, nombre Amazon Fill dev Android; ID público de app: 1:528273757148:android:45d5cf0ddb2e3021c93519. No se descargó google-services.json ni se instalaron SDK o modificaron archivos de Unity.
- Authentication inicializado y proveedor Anonymous comprobado como Enabled; limpieza automática de anónimos conservada desactivada por defecto. No se crearon usuarios de prueba ni se obtuvieron tokens.
- El dueño confirmó aceptar las condiciones de Google APIs y Play Integrity. App Check registrado con Play Integrity, huella pública del certificado extraída del APK dev y TTL predeterminado de 1 hora. Confirmado Registered y Authentication Unenforced; no se activó enforcement. No hay integración con Play Console ni verificación de atestaciones reales; esa comprobación queda pendiente del cliente y de Play. No se leyó ningún keystore.
- Remote Config revisado: estado inicial sin configuración ni parámetros publicados; se mantienen los defaults del cliente. Crashlytics revisado: app identificada, consola en Add SDK, pendiente de integración en Unity y primer evento de prueba. Analytics aún sin datos del cliente. Esta entrega configura consola y no acredita integración de SDK.
- Diff preparado con el ID real y el ID de Android: solo AfDevApi, cinco cambios de variables de Lambda y el autorizador JWT. Código de las cinco Lambdas e IAM sin cambios; los otros cinco stacks no tienen diferencias. Ensamblado y salida fuera del repo. Pendiente del sí del dueño antes de desplegar este ajuste; la API desplegada aún espera amazon-fill-dev.
- Sin secretos reales cargados ni accesos añadidos a la cuenta empresarial. Los identificadores de Firebase consignados aquí son públicos y están permitidos expresamente en la sección 5 del encargo; no se registra el número del proyecto por separado ni datos de cuentas AWS.
