# infra/: backend de Amazon Fill en AWS (D-035)

Infraestructura como código con **AWS CDK v2 en TypeScript** (Q-AWS-1). La escriben y la validan los agentes **sin credenciales**; **despliega el dueño** con su sesión de IAM Identity Center. Plan completo: `docs/dev/plan-backend-aws.md`; decisión: D-035 en `docs/dev/decisiones.md`.

Ninguna cuenta, correo, clave ni secreto vive en este repositorio: lo que identifica una cuenta o a una persona se pasa por contexto (`-c clave=valor`) al ejecutar CDK, y los secretos se cargan en Secrets Manager después del despliegue.

## Qué hay

| Stack | Región | Contenido |
|---|---|---|
| `Af<Env>Global` | us-east-1 | AWS Budgets (10 USD en dev, 50 en prod; avisos al 50, 80 y 100 %) y, solo con dominio, el certificado de ACM de CloudFront |
| `Af<Env>Data` | sa-east-1 | 6 tablas de DynamoDB (`af-<env>-purchases`, `rewards`, `integrity`, `account_codes`, `rate_limits`, `jobs`): on-demand, cifradas, PITR; en prod, protección contra borrado y retención |
| `Af<Env>Api` | sa-east-1 | HTTP API con autorizador JWT de Firebase Auth, rutas `/v1/...` del contrato, 5 Lambdas (Node 24, arm64; F2-3 a F2-8), límites por ruta, 3 secretos de relleno, job diario a las 03:00 UTC con DLQ |
| `Af<Env>Web` | sa-east-1 | Web estática: S3 privado + CloudFront con OAC, cabeceras de seguridad, `noindex` en dev, la web pública de Tintalú en es-419, en y pt-BR (portada, privacidad, términos, eliminación de cuenta y créditos, en borrador), `config.json` de despliegue y `app-ads.txt` de plantilla; reenvía `/v1/account/delete-*` a la API (borrado por web, mismo origen) |
| `Af<Env>Cdn` | sa-east-1 | Contenido remoto: bucket `af-<env>-content-<cuenta>` tras CloudFront; `packs/*` inmutables (1 año), `catalog.json` y el resto 5 minutos; sin listado ni CORS |
| `Af<Env>Ops` | sa-east-1 | Alarmas (errores y throttles de Lambda, 5xx de la API, fallo del job, throttles de DynamoDB, cuota de integridad al 70 %) con aviso por SNS al correo |

Otros directorios: `functions/` (código de las Lambdas), `contracts/v1/` (contrato de la API: fuente única, Q-AWS-12; `index.json` fija rutas, autorización y límites), `web/` (web pública: fuentes en `web/src/`, generador `web/build.ts` y salida `web/site/`; ver «Web pública»), `test/` (unitarios, de contrato y de stacks).

## Qué hacen las funciones (F2-3 a F2-8)

| Función | Rutas | Resumen |
|---|---|---|
| verifyPurchase | `POST /v1/purchases/verify` (JWT + integridad) | 30 por UID y hora [P]; `products.get` en Play; `pending` no concede; cancelada o inexistente → 400; cuenta ofuscada de otro UID → 409; token con lápida `voided` (anulado antes de verificarse) → 400 sin llamar a Play; `af_starter` (`once_per_account`): antes del `acknowledge` reserva la marca `once#` 15 minutos (`UpdateItem` condicional); si otro token de la cuenta ya la tiene, 400 **sin** `acknowledge` (Play la reembolsa a los 3 días; el cliente no debe consumir esa transacción); `acknowledge` si falta; transacción en `purchases` con `PutItem` condicional (dos peticiones con el mismo token: una `ok`, la otra `duplicate`; token de otro UID → 409), la comprobación de cuenta no borrada (si el borrado empezó entretanto → 401) y el paso de la reserva `once#` a marca permanente (sin `ttl`). Solo `ok` lleva `grant` (catálogo en `functions/shared/catalog.ts`, espejo de 05 §4.1 con los defaults de 08 §9). **Q-IAP-4:** un **no consumible** (`af_noads`, `af_skin_m02`-`m04`) de otro UID o anonimizado, en `ok`, del mismo SKU, que Play da como comprado y con cuenta ofuscada, pasa al UID que lo pide (`Update` condicional en transacción con la marca de cuenta borrada, el cupo diario y, si tenía dueño, una fila sombra `<token_hash>#prev#<n>` con el UID anterior, `shadow_of` y apuntada en `shadow_keys`; historial `reassign_*` sin identificadores) y responde `duplicate`; sin fila, `ok`. Topes [P]: 5 por token (volver al UID de la cuenta ofuscada no cuenta), 24 h entre dos (409) y 4 por UID y día (429; el cupo solo se gasta si la escritura sale). Detalle en `docs/dev/iap.md` §4.3 |
| restoreLedger | `POST /v1/ledger/restore` (JWT) | Compras del UID por `by_uid` con su estado y los nonces pendientes que `rewards` tiene confirmados para ese UID. Las sombras de Q-IAP-4 (compras reasignadas a otro UID) solo salen si están `voided`, con el `token_hash` real y una vez por token |
| rewardedSsv | `GET`/`POST /v1/ssv` (firma de MAX) | Firma [NV] → cada `event_id` queda atado a un único (UID, nonce): otro UID u otro nonce con la misma firma → 403 → límite de 100 por UID y día [P] (los reintentos de MAX no gastan cupo) → `PutItem` condicional en `rewards` (TTL 30 días), en transacción con la comprobación de cuenta no borrada; nonce repetido o cuenta borrada → 200 `duplicate` sin escribir |
| voidedPurchasesJob | Scheduler, 03:00 UTC | `voidedpurchases.list` desde el checkpoint de `jobs` (con 24 h de solapamiento [P], como mucho 30 días atrás), marca `voided` en `purchases` (también las filas anonimizadas y, de una compra reasignada, las sombras de `shadow_keys`); si el token aún no está, deja una lápida `voided` sin `uid` (`tombstone: true`, `PutItem` condicional) para que `verifyPurchase` no lo conceda; solo entonces avanza el checkpoint; un fallo lanza (reintentos y DLQ) |
| deleteAccount | `POST /v1/account/code`, `/delete`, `/delete-request`, `/delete-confirm` | Código de 128 bits que rota (solo su hash; 10 al día por UID [P]); borrado por app con integridad; por web, código → token de 5 minutos (5 solicitudes por código y hora [P]) → borrado. El borrado es idempotente: primero una marca de cuenta borrada de 2 h en `rate_limits` (el ID token sigue valiendo hasta 1 h y, con ella, ni `/code` ni `verify` vuelven a escribir a nombre del UID), luego `rewards`, anonimiza `purchases` (quita `uid`), `integrity` y las marcas `once#`, el usuario de Firebase Auth, una **segunda pasada** de lo mismo (lo escrito justo antes de la marca que el GSI aún no enseñaba) y, al final, `account_codes`. `verify`, el SSV y `/code` escriben en una transacción que exige que no haya marca, así que nada se escribe a nombre del UID después de ella. La caché de `integrity` también se escribe en transacción con la marca |

Integridad (Q-AWS-4 a): token de App Check de uso limitado en `X-Firebase-AppCheck`, verificado con `jose` contra `https://firebaseappcheck.googleapis.com/v1/jwks`; cada token vale una vez (anti-replay en `rate_limits`) y el veredicto se guarda 24 h por UID en `integrity`. La variante (b), Play Integrity directo, está declarada y sin implementar. Los logs llevan el UID solo como hash y nunca tokens ni códigos.

### Riesgos residuales conocidos (F2c)

- **Borrado y GSI:** una fila de `purchases` o `rewards` escrita justo antes de la marca `deleted#` cuyo GSI `by_uid` tarde más en propagarse que la llamada a Firebase Auth se escapa de las dos pasadas y conserva el `uid` (en `rewards` caduca a los 30 días; en `purchases`, no). La métrica `EraseSecondPass` cuenta las veces que la segunda pasada encontró algo. Vale también para las filas sombra de Q-IAP-4 (`<token_hash>#prev#<n>`): una reasignación justo antes de la marca de borrado del UID anterior puede dejar su sombra con el `uid`.
- **Borrado durante una compra:** si la cuenta se borra entre el `acknowledge` y la escritura, la compra queda reconocida y no se concede (401). El usuario borró su cuenta.
- **`af_starter`:** si pasan más de 15 minutos entre la reserva y la escritura (Play o DynamoDB lentos, reintentos) y otro token de la cuenta toma la reserva caducada, el primero queda reconocido sin conceder. Una reserva hecha justo cuando se borra la cuenta queda (sin el UID en claro) hasta que la borra el TTL. La marca es permanente: un `af_starter` reembolsado no se puede volver a comprar.
- **Reasignación de no consumibles (Q-IAP-4):** el servidor no ve la cuenta de Google; tener el token de compra desde la app auténtica (App Check) es la prueba. Con un token robado y un dispositivo íntegro se puede tomar un Sin anuncios o una skin ajenos, como mucho 5 veces por token y una por día; el UID de la cuenta ofuscada lo recupera siempre. Un reembolso se retira en el UID actual y en los anteriores (sombras), pero mientras la compra está vigente el UID anterior conserva el derecho en su dispositivo. Métricas `PurchaseReassigned` y `PurchaseReassignLimit`, sin alarma.
- **Tokens del borrado por web:** las filas `kind: request` de `/delete-request` se escriben sin la transacción; una creada durante el borrado queda con el `uid` hasta su TTL de 5 minutos.

## Validar sin credenciales (agentes y CI)

Node 22.18 o posterior (Node ejecuta los `.ts` directamente; el runtime de las Lambdas es Node 24). Desde `infra/` y **sin variables `AWS_*`**:

```
npm ci
npm run lint
npm run typecheck
npm test
npm run web:check        # web/site al día con web/build.ts
npx cdk synth -c env=dev
npx cdk synth -c env=prod -c firebaseAppIds=1:123456789:android:0123456789abcdef   # id de mentira para validar
```

`cdk synth` aplica **cdk-nag** (AwsSolutions) como plugin de validación: cualquier hallazgo sin reconocer hace fallar la síntesis. Cada excepción está reconocida en el código con su motivo (`ack(...)` en `lib/`). La síntesis no hace búsquedas en la cuenta (`fromLookup` está prohibido) y `cdk.json` desactiva la telemetría, los avisos y el informe de versión de la CLI.

## Contexto admitido

| Clave | Obligatoria | Ejemplo | Para qué |
|---|---|---|---|
| `env` | Sí | `dev`, `prod` | Entorno |
| `account` | Para desplegar, **sí** (recomendado) | id de 12 dígitos | Fija la cuenta: CDK se niega a desplegar si el perfil apunta a otra |
| `alertEmail` | Para recibir avisos | tu correo | Avisos de alarmas (SNS) y de presupuesto |
| `firebaseProjectId` | No | `amazon-fill-dev` | Emisor y audiencia del JWT de Firebase Auth y audiencia del token de App Check; por defecto `amazon-fill-dev` / `amazon-fill-prod` (08 §15.3) |
| `firebaseAppIds` | En prod, **sí** (en dev, no) | `1:<número>:android:<hash>` (varios, separados por comas) | El token de App Check solo vale para esas apps de Firebase (Q-AWS-4). En prod la síntesis falla sin él (F2c): así solo las apps Android registradas emiten tokens válidos, no cualquier app del proyecto (web, iOS o una de pruebas). Además, la Lambda falla cerrada (500) si en prod le falta la lista. Se copia de la consola de Firebase (Configuración del proyecto → Tus apps → ID de la app); es público, no es un secreto |
| `packageName` | En prod, **sí** para comprar | applicationId de la app | Paquete para la Google Play Developer API. En dev vale por defecto el provisional `com.amazonfill.dev`; en prod no tiene valor por defecto, solo se pasa **tras el dictamen de marca** y la síntesis rechaza el provisional (regla 6 de `CLAUDE.md`). Sin él, verifyPurchase y el job responden 500 (fallan cerrados) |
| `domain` + `hostedZoneId` | No (los dos o ninguno) | dominio y zona de Route 53 | Web en `<dominio>` y `www.`, CDN en `cdn.`, API en `api.`; **solo prod y solo tras el dictamen de marca** (Q-AWS-9) |

## Web pública (`web/`, D-041)

Web estática de Tintalú, del desarrollador **Parallext, LLC** (D-041), en es-419, en y pt-BR. Sin rastreadores, sin cookies, sin fuentes ni scripts de terceros: la CSP del stack Web (`WEB_CSP` en `lib/web-stack.ts`) solo permite el mismo origen y los tests lo comprueban. Los textos legales son **borrador** hasta el abogado (D-033, añadido del 2026-10-22, punto 7); en y pt-BR, traducción con IA pendiente del revisor nativo.

| Página | es-419 | en | pt-BR |
|---|---|---|---|
| Portada | `/` | `/en/` | `/pt-br/` |
| Privacidad | `/privacidad/` | `/en/privacy/` | `/pt-br/privacidade/` |
| Términos | `/terminos/` | `/en/terms/` | `/pt-br/termos/` |
| Eliminación de cuenta (08 §11) | `/tintalu/eliminar-cuenta/` | `/tintalu/en/delete-account/` | `/tintalu/pt-br/excluir-conta/` |
| Créditos y licencias | `/creditos/` | `/en/credits/` | `/pt-br/creditos/` |

Más `/app-ads.txt` (plantilla, todo comentado hasta tener los IDs de MAX) y `/assets/` (hoja de estilo y el script del borrado).

- **Cómo se edita:** los textos están en `web/src/content/<idioma>/<página>.html` y en `web/src/site.ts` (rutas, títulos, plantilla y textos del formulario); el script del borrado, en `web/src/browser/` (TypeScript que esbuild empaqueta sin dependencias). Después, `npm run web:build` regenera `web/site/` (**generado: no se edita a mano**) y `npm run web:check` comprueba que está al día (también lo hace `npm test`).
- **Marcadores:** `[[DATO DEL DUEÑO: …]]` (datos de Parallext, LLC que faltan) y `[[ABOGADO: …]]` (lo decide el abogado); la web los resalta. Se sustituyen en `web/src/content/` y se regenera.
- **Borrado por web:** el formulario normaliza el código (mayúsculas, sin espacios ni guiones, I y L → 1, O → 0; `common.schema.json`) y llama a `POST /v1/account/delete-request` y después a `/v1/account/delete-confirm` (contrato v1; sin sesión ni App Check). La base de la API sale de `/config.json`, que escribe este stack al desplegar (`webRuntimeConfig`): vacía, porque CloudFront reenvía `/v1/account/delete-*` a la API desde el mismo origen.
- **Desplegar en dev:** lo hace el dueño con el `cdk deploy` de la guía de abajo (o solo `npx cdk deploy AfDevWeb -c env=dev -c account=<id-cuenta-dev> --profile af-dev`); la URL es la salida `WebUrl` (`https://<id>.cloudfront.net`). La caché es de 5 minutos: los cambios se ven solos en ese tiempo. En dev la web no se indexa (`X-Robots-Tag` y `robots.txt`).
- **Sin página 404 propia:** una ruta que no existe devuelve el 403 de S3. No se usan las respuestas de error personalizadas de CloudFront porque se aplicarían también a `/v1/account/delete-*` y taparían el 404 `code_not_found` de la API.

### Conectar `tintalu.com` (pasos del dueño, cuando lo decida)

**No está hecho ni se hace sin decisión del dueño:** lo público con la marca espera al dictamen (D-034, Q-AWS-9; D-041). El dominio está en **Cloudflare**, y el CDK de hoy solo sabe conectar un dominio con zona de **Route 53** (`-c domain` + `-c hostedZoneId`), así que antes hace falta elegir una vía:

1. **Opción A, DNS en Cloudflare (recomendada [P]: sin coste nuevo de AWS).** Necesita un cambio de CDK previo (lo hace un agente cuando el dueño lo pida): admitir un dominio sin zona de Route 53, con el certificado de ACM validado por DNS fuera de AWS y sin registros `Route53::RecordSet`. Después, el dueño:
   1. Despliega `AfProdGlobal`: crea el certificado de ACM en **us-east-1** (CloudFront solo usa certificados de esa región) para `tintalu.com` y `www.tintalu.com` (y `cdn.` si la CDN va en el dominio). La consola de ACM enseña un **CNAME de validación** por nombre.
   2. En Cloudflare › DNS, crea esos CNAME de validación tal cual, **sin proxy** (nube gris), y espera a que ACM marque el certificado como «Emitido». No los borres: ACM los usa para renovar.
   3. Despliega `AfProdWeb`: la distribución de CloudFront queda con los nombres alternativos (`tintalu.com`, `www.tintalu.com`) y el certificado. Copia su dominio `xxxx.cloudfront.net` (salida `WebUrl`).
   4. En Cloudflare › DNS: `tintalu.com` CNAME → `xxxx.cloudfront.net` (Cloudflare lo aplana en el apex) y `www` CNAME → el mismo, los dos **sin proxy** (nube gris) para que CloudFront sirva el certificado y las cabeceras de seguridad.
   5. Comprueba `https://tintalu.com/tintalu/eliminar-cuenta` y que `robots.txt` permite indexar en prod.
2. **Opción B, delegar el DNS a Route 53.** Crea una zona alojada `tintalu.com` en la cuenta de prod (coste mensual de la zona), cambia en Cloudflare los servidores de nombres (NS) del dominio por los de Route 53 y despliega con `-c domain=tintalu.com -c hostedZoneId=<id de la zona>`: el CDK de hoy ya crea el certificado, los alias y los registros A/AAAA. Cloudflare deja de servir el DNS.

En las dos: la web pública con dominio va en **prod** (en dev se queda en `*.cloudfront.net`), y el CDK de hoy también pone `cdn.<dominio>` en el certificado. Tras conectar el dominio, la URL de eliminación de cuenta (`https://tintalu.com/tintalu/eliminar-cuenta`) y la de la política son las que van a Play Console (08 §13.1 #2 y #7).

## Guía del dueño: desplegar dev

Todo lo de esta sección lo hace el dueño en su equipo; ningún agente ejecuta estos comandos.

### 1. Cuentas y acceso (una vez)

1. En AWS Organizations, crea dos cuentas: dev y prod (Q-AWS-3).
2. Activa **IAM Identity Center** en la cuenta de gestión, crea tu usuario con MFA y asígnale en cada cuenta un conjunto de permisos (para empezar, `AdministratorAccess`; más adelante uno más estrecho). **No crees usuarios IAM con claves de acceso.**
3. Instala AWS CLI v2 y configura un perfil por cuenta:
   ```
   aws configure sso --profile af-dev
   aws sso login --profile af-dev
   ```
   La sesión caduca sola; vuelve a hacer `aws sso login` cuando haga falta.

### 2. Bootstrap de CDK (una vez por cuenta, en las dos regiones)

Desde `infra/`, con `npm ci` hecho:
```
npx cdk bootstrap aws://<id-cuenta-dev>/sa-east-1 aws://<id-cuenta-dev>/us-east-1 --profile af-dev
```

### 3. Revisar y desplegar

```
npx cdk diff  --all -c env=dev -c account=<id-cuenta-dev> -c alertEmail=<tu-correo> --profile af-dev
npx cdk deploy --all -c env=dev -c account=<id-cuenta-dev> -c alertEmail=<tu-correo> --profile af-dev
```
- `cdk deploy` enseña los cambios de IAM y pide confirmación: léelos antes de aceptar.
- Al terminar, **confirma por correo** las suscripciones de SNS (alarmas) que te llegan.
- Las salidas (`ApiUrl`, `WebUrl`, `CdnUrl`, `ContentBucketName`) son las URL del entorno; la de la API es la que se compila en el build de dev del cliente.

### 4. Presupuesto (Q-AWS-6)

`Af<Env>Global` crea el presupuesto mensual (10 USD en dev, 50 en prod) con avisos al 50, 80 y 100 % al `alertEmail`. Si despliegas sin `alertEmail`, el presupuesto existe pero no avisa: vuelve a desplegar con el correo. Comprueba en la consola de Billing que lo ves y, si la cuenta es miembro de Organizations, que tiene acceso a la información de facturación.

### 5. Cargar los secretos

El CDK crea tres secretos con un relleno aleatorio; mientras lleven el relleno, las funciones que los usan fallan cerradas (500 o 403) y no conceden nada. Cárgalos tú **desde un archivo fuera del repositorio** (nunca los pegues en el chat ni los guardes en `fill-game/`):
```
aws secretsmanager put-secret-value --profile af-dev --region sa-east-1 --secret-id af-dev/play-service-account          --secret-string file://<ruta-fuera-del-repo>/play.json
aws secretsmanager put-secret-value --profile af-dev --region sa-east-1 --secret-id af-dev/firebase-admin-service-account --secret-string file://<ruta-fuera-del-repo>/firebase-admin.json
aws secretsmanager put-secret-value --profile af-dev --region sa-east-1 --secret-id af-dev/max-ssv-key                    --secret-string file://<ruta-fuera-del-repo>/max-ssv.txt
```
Borra los archivos locales al terminar.

| Secreto | Formato | Lo usa | Permisos que necesita la cuenta |
|---|---|---|---|
| `af-<env>/play-service-account` | La clave JSON tal cual la descarga Google Cloud (`"type": "service_account"`, `client_email` acabado en `.iam.gserviceaccount.com`, `private_key` PKCS#8). Su `token_uri` se ignora: el canje va siempre a `https://oauth2.googleapis.com/token` | verifyPurchase y voidedPurchasesJob (ámbito `androidpublisher`) | En Play Console, la cuenta de servicio invitada con «Ver información financiera» y «Gestionar pedidos y suscripciones» solo de esta app; la Google Play Android Developer API activada en su proyecto |
| `af-<env>/firebase-admin-service-account` | La clave JSON de una cuenta de servicio del proyecto de Firebase del entorno, mismo formato | deleteAccount (ámbito `identitytoolkit`, `accounts:delete`) | Un rol que solo borre usuarios de Auth (p. ej. «Firebase Authentication Admin»); no hace falta Firebase Admin completo. La Identity Toolkit API activada |
| `af-<env>/max-ssv-key` | Texto plano: la clave S2S de recompensas de la cuenta de AppLovin MAX (se recortan espacios y saltos de línea) | rewardedSsv | — |

Las claves se rotan en Google Cloud y en MAX y se vuelven a cargar igual; las Lambdas guardan el valor en caché como mucho 5 minutos. La federación sin claves (WIF) queda para la 1.1 (Q-AWS-7).

### 5 bis. Callback de recompensas en MAX

En el panel de MAX, en la URL de los callbacks S2S de recompensas, pon (con `<ApiUrl>` la salida del stack):
```
<ApiUrl>/v1/ssv?user_id={USER_ID}&custom_data={CUSTOM_DATA}&event_id={EVENT_ID}&event_token={EVENT_TOKEN}&network={NETWORK_NAME}
```
**[NV]** Se supone que MAX calcula `{EVENT_TOKEN}` = SHA-256 en hexadecimal de `{EVENT_ID}` seguido de la clave S2S (lo comprueba `functions/rewarded-ssv/maxSignature.ts`). Si la documentación de MAX dice otra cosa, se cambia solo esa función. El cliente pone `SetUserId(UID)` y `customData = placement|nonce` (08 §10.2).

### 5 ter. Verificador falso en dev

En dev (y solo en dev: un test de stacks lo comprueba) las funciones llevan `AF_FAKE_PLAY_VERIFIER=1`, porque sin app en Play Console no hay compras ni Play Integrity reales (D-034). Con él:
- compras con `purchaseToken` que empieza por `fake-`: `fake-pending-…` pendiente, `fake-canceled-…` cancelada, `fake-unknown-…` inexistente y cualquier otro `fake-…` comprada (se «reconoce» en memoria); los demás tokens van a Play de verdad;
- integridad: la cabecera `X-Firebase-AppCheck: fake-ok-<algo único>` pasa; cualquier otro token se verifica de verdad contra App Check;
- el job de anuladas no consulta Play.

### 6. Comprobación rápida

- `WebUrl` muestra la portada de Tintalú; `WebUrl/tintalu/eliminar-cuenta`, `WebUrl/config.json` (`{"apiBase":""}`), `WebUrl/app-ads.txt` y `WebUrl/robots.txt` (que en dev prohíbe indexar) existen. En la página de borrado, un código con formato válido que no existe responde «No encontramos ese código» (la API devuelve 404 `code_not_found`).
- `curl -i "<ApiUrl>/v1/ssv"` → `400` (`bad_request`, faltan los parámetros) y con parámetros pero sin firma buena → `403` (`bad_signature`); `curl -i -X POST <ApiUrl>/v1/purchases/verify` sin token → `401`.
- Con un ID token de un usuario anónimo de dev: `POST /v1/account/code` devuelve un código; `POST /v1/ledger/restore` con `{"pending_nonces":[]}` devuelve listas vacías; `POST /v1/purchases/verify` con `X-Firebase-AppCheck: fake-ok-1` y `{"token":"fake-ok-1","sku":"af_coins_2"}` devuelve `ok` y, repetido (con otro `fake-ok-…`), `duplicate`.
- `curl -i <CdnUrl>/catalog.json` → `403` hasta que se suba un catálogo (no hay listado).

### 7. Borrar dev

`npx cdk destroy --all -c env=dev -c account=<id-cuenta-dev> --profile af-dev`. Antes vacía los buckets de la web, del contenido y de logs (en dev se borran con el stack, pero S3 no borra un bucket con objetos).

## Prod

Igual que dev con `-c env=prod`, el perfil `af-prod` y la cuenta de prod. En prod las tablas, los buckets, los secretos y los logs se **retienen** al borrar el stack, y `Data` y `Api` tienen protección contra la terminación. El dominio (`-c domain=… -c hostedZoneId=…`) y la web pública solo **tras el dictamen de marca** (D-034, Q-AWS-9; plan F5). El verificador falso de Play no existe en prod (un test lo comprueba). Para que las compras funcionen en prod hace falta `-c packageName=<applicationId definitivo>`, que solo existe tras el dictamen de marca. La síntesis de prod exige `-c firebaseAppIds=<id de la app Android de prod>` (sin él falla con un mensaje que lo pide):
```
npx cdk synth -c env=prod -c firebaseAppIds=1:<número>:android:<hash>
```

Las referencias entre stacks son débiles (`Fn::GetStackOutput`, el valor por defecto de esta versión de CDK): CloudFormation no impide borrar `Data` o `Api` aunque otros stacks los usen. En prod lo frenan la retención y la protección contra la terminación; aun así, borra en orden inverso (`Ops`, `Cdn`, `Web`, `Api`, `Data`, `Global`).
