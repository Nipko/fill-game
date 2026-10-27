# infra/: backend de Amazon Fill en AWS (D-035)

Infraestructura como código con **AWS CDK v2 en TypeScript** (Q-AWS-1). La escriben y la validan los agentes **sin credenciales**; **despliega el dueño** con su sesión de IAM Identity Center. Plan completo: `docs/dev/plan-backend-aws.md`; decisión: D-035 en `docs/dev/decisiones.md`.

Ninguna cuenta, correo, clave ni secreto vive en este repositorio: lo que identifica una cuenta o a una persona se pasa por contexto (`-c clave=valor`) al ejecutar CDK, y los secretos se cargan en Secrets Manager después del despliegue.

## Qué hay

| Stack | Región | Contenido |
|---|---|---|
| `Af<Env>Global` | us-east-1 | AWS Budgets (10 USD en dev, 50 en prod; avisos al 50, 80 y 100 %) y, solo con dominio, el certificado de ACM de CloudFront |
| `Af<Env>Data` | sa-east-1 | 10 tablas de DynamoDB (`af-<env>-purchases`, `rewards`, `integrity`, `account_codes`, `rate_limits`, `jobs`, `league_players`, `league_board`, `league_histogram`, `league_names`): on-demand, cifradas, PITR; en prod, protección contra borrado y retención |
| `Af<Env>Api` | sa-east-1 | HTTP API con autorizador JWT de Firebase Auth, rutas `/v1/...` del contrato, 7 Lambdas (Node 24, arm64; F2-3 a F2-8), límites por ruta, 3 secretos de relleno, job diario a las 03:00 UTC con DLQ |
| `Af<Env>Web` | sa-east-1 | Web estática: S3 privado + CloudFront con OAC, cabeceras de seguridad, `noindex` en dev, la web pública de Tintalú en es-419, en y pt-BR (portada, privacidad, términos, eliminación de cuenta y créditos, en borrador), `config.json` de despliegue y `app-ads.txt` de plantilla; reenvía `/v1/account/delete-*` a la API (borrado por web, mismo origen) |
| `Af<Env>Cdn` | sa-east-1 | Contenido remoto: bucket `af-<env>-content-<cuenta>` tras CloudFront; `packs/*` inmutables (1 año), `catalog.json` y el resto 5 minutos; sin listado ni CORS |
| `Af<Env>Ops` | sa-east-1 | Alarmas (errores y throttles de Lambda, 5xx de la API, fallo del job, throttles de DynamoDB, cuota de integridad al 70 %) con aviso por SNS al correo |

Otros directorios: `functions/` (código de las Lambdas), `contracts/v1/` (contrato de la API: fuente única, Q-AWS-12; `index.json` fija rutas, autorización y límites), `web/` (web pública: fuentes en `web/src/`, generador `web/build.ts` y salida `web/site/`; ver «Web pública»), `test/` (unitarios, de contrato y de stacks).

## Qué hacen las funciones (F2-3 a F2-8)

| Función | Rutas | Resumen |
|---|---|---|
| verifyPurchase | `POST /v1/purchases/verify` (JWT + integridad) | 30 por UID y hora [P]; `products.get` en Play; `pending` no concede; cancelada o inexistente → 400; cuenta ofuscada de otro UID → 409; token con lápida `voided` (anulado antes de verificarse) → 400 sin llamar a Play; `af_starter` (`once_per_account`): antes del `acknowledge` reserva la marca `once#` 15 minutos (`UpdateItem` condicional); si otro token de la cuenta ya la tiene, 400 **sin** `acknowledge` (Play la reembolsa a los 3 días; el cliente no debe consumir esa transacción); `acknowledge` si falta; transacción en `purchases` con `PutItem` condicional (dos peticiones con el mismo token: una `ok`, la otra `duplicate`; token de otro UID → 409), la comprobación de cuenta no borrada (si el borrado empezó entretanto → 401) y el paso de la reserva `once#` a marca permanente (sin `ttl`). Solo `ok` lleva `grant` (catálogo en `functions/shared/catalog.ts`, espejo de 05 §4.1 con los defaults de 08 §9). **Q-IAP-4:** un **no consumible** (`af_noads`, `af_skin_m02`-`m04`) de otro UID o anonimizado, en `ok`, del mismo SKU, que Play da como comprado y con cuenta ofuscada, pasa al UID que lo pide (`Update` condicional en transacción con la marca de cuenta borrada, el cupo diario y, si tenía dueño, una fila sombra `<token_hash>#prev#<n>` con el UID anterior, `shadow_of` y apuntada en `shadow_keys`; historial `reassign_*` sin identificadores) y responde `duplicate`; sin fila, `ok`. Topes [P]: 5 por token (volver al UID de la cuenta ofuscada no cuenta), 24 h entre dos (409) y 4 por UID y día (429; el cupo solo se gasta si la escritura sale). Detalle en `docs/dev/iap.md` §4.3 |
| restoreLedger | `POST /v1/ledger/restore` (JWT) | Compras del UID por `by_uid` con su estado y los nonces pendientes que `rewards` tiene confirmados para ese UID. Las sombras de Q-IAP-4 (compras reasignadas a otro UID) solo salen si están `voided`, con el `token_hash` real y una vez por token |
| rewardedSsv | `GET`/`POST /v1/ssv` (SSV de AdMob, D-049) | Consulta de más de 8.192 caracteres → 400 → firma ECDSA P-256/SHA-256 de Google sobre la consulta cruda hasta `&signature=`, con la clave pública del `key_id` (`verifier-keys.json` de gstatic, ≤ 64 KiB, en caché ≤ 24 h; un `key_id` desconocido la refresca una vez, como mucho una por minuto) → `ad_unit` fuera de la lista blanca del entorno (`admobRewardedAdUnits`) o ausente, 200 `ignored` sin escribir ni gastar cupo → sin `user_id` o `custom_data`, 200 `ignored` sin escribir → `timestamp` de más de 24 h o de más de 10 min en el futuro → 400 `request_expired` → cada `transaction_id` queda atado a un único (UID, nonce): otro UID u otro nonce → 403 → límite de 100 por UID y día [P] (las repeticiones no gastan cupo) → `PutItem` condicional en `rewards` (TTL 30 días), en transacción con la comprobación de cuenta no borrada; nonce repetido o cuenta borrada → 200 `duplicate` sin escribir. Sin las claves de Google → 502 |
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
npx cdk synth -c env=prod -c domain=tintalu.com -c firebaseAppIds=1:123456789:android:0123456789abcdef   # id de mentira para validar
npx cdk synth -c env=prod -c domain=tintalu.com -c webOnly=true                                         # solo la web de prod con DNS externo (D-044)
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
| `domain` | No | `tintalu.com` | Web en `<dominio>` y `www.` (en prod; D-044 la publica ya). Sin `hostedZoneId`, el DNS es **externo** (Cloudflare): certificados de ACM validados por CNAME que crea el dueño y ningún registro DNS en AWS; las salidas dicen qué crear. Con dominio, canonical y hreflang absolutos; solo prod con dominio se indexa |
| `hostedZoneId` | No (solo con `domain`) | id de zona de Route 53 | DNS en Route 53 (opción B): el CDK crea la validación y los alias A/AAAA, y pone también `cdn.` y `api.` en el dominio |
| `domainExtras` | No (solo con `domain` y sin `hostedZoneId`) | `cdn`, `api` o `cdn,api` | Con DNS externo, `cdn.<dominio>` en la CDN o `api.<dominio>` en la API (cada uno con su certificado y su salida de CNAME). Por defecto, ninguno |
| `noDomain` | Solo para prod sin dominio a propósito | `true` | Desde D-044, prod **exige** `-c domain` y la síntesis falla sin él: un despliegue de prod sin dominio quitaría los alias de CloudFront y borraría el certificado de la web, y `tintalu.com` caería. `noDomain=true` lo permite a sabiendas (no admite `domain`) |
| `admobRewardedAdUnits` | No | `1182085615` (varios, separados por comas; número o `ca-app-pub-<editor>/<número>`) | Lista blanca de bloques recompensados de AdMob cuyo SSV acepta `/v1/ssv` (D-049): las claves de firma de Google son las mismas para todos los editores, así que sin ella cualquiera podría apuntar su propio bloque a nuestra URL. Por defecto, prod `1182085615` (el bloque de producción) y dev además `5224354917` (el recompensado de prueba de Google). Llega a la Lambda como `AF_ADMOB_AD_UNITS`; un bloque nuevo hay que añadirlo aquí antes de activar su SSV |
| `webOnly` | No | `true` | Solo `<Prefijo>Global` y `<Prefijo>Web`, sin `Data`, `Api`, `Cdn` ni `Ops` (D-044). No exige `firebaseAppIds` y no admite `hostedZoneId` ni `domainExtras`. Sin API, el borrado por web queda desactivado (`config.json`) y la página da la vía del juego y el correo |

## Web pública (`web/`, D-041)

Web estática de Tintalú, del desarrollador **Parallext, LLC** (D-041), en es-419, en y pt-BR. Sin rastreadores, sin cookies, sin fuentes ni scripts de terceros: la CSP del stack Web (`WEB_CSP` en `lib/web-stack.ts`) solo permite el mismo origen y los tests lo comprueban. Los textos legales son **borrador** hasta el abogado (D-033, añadido del 2026-10-22, punto 7); en y pt-BR, traducción con IA pendiente del revisor nativo.

| Página | es-419 | en | pt-BR |
|---|---|---|---|
| Portada | `/` | `/en/` | `/pt-br/` |
| Privacidad | `/privacidad/` | `/en/privacy/` | `/pt-br/privacidade/` |
| Términos | `/terminos/` | `/en/terms/` | `/pt-br/termos/` |
| Eliminación de cuenta (08 §11) | `/tintalu/eliminar-cuenta/` | `/tintalu/en/delete-account/` | `/tintalu/pt-br/excluir-conta/` |
| Créditos y licencias | `/creditos/` | `/en/credits/` | `/pt-br/creditos/` |

Más `/app-ads.txt` (la línea de AdMob, D-049; las demás redes, comentadas hasta sumarlas a la mediación de AdMob) y `/assets/` (hoja de estilo y el script del borrado).

- **Cómo se edita:** los textos están en `web/src/content/<idioma>/<página>.html` y en `web/src/site.ts` (rutas, títulos, plantilla y textos del formulario); el script del borrado, en `web/src/browser/` (TypeScript que esbuild empaqueta sin dependencias). Después, `npm run web:build` regenera `web/site/` (**generado: no se edita a mano**) y `npm run web:check` comprueba que está al día (también lo hace `npm test`).
- **Marcadores:** `[[DATO DEL DUEÑO: …]]` (datos de Parallext, LLC que faltan) y `[[ABOGADO: …]]` (lo decide el abogado); la web los resalta. Se sustituyen en `web/src/content/` y se regenera.
- **Borrado por web:** el formulario normaliza el código (mayúsculas, sin espacios ni guiones, I y L → 1, O → 0; `common.schema.json`) y llama a `POST /v1/account/delete-request` y después a `/v1/account/delete-confirm` (contrato v1; sin sesión ni App Check). El estado sale de `/config.json`, que escribe este stack al desplegar (`webRuntimeConfig`): con la API, `{"deleteEnabled": true, "apiBase": ""}` (base vacía, porque CloudFront reenvía `/v1/account/delete-*` a la API desde el mismo origen); sin ella (`webOnly`), `{"deleteEnabled": false}`, y la página muestra en su lugar que el borrado por web estará disponible pronto, con la vía del juego y el correo de privacidad, sin enviar nada.
- **SEO por entorno:** `web/site/` es la variante sin dominio (canonical y hreflang relativos, `robots.txt` con `Disallow: /`). Con `-c domain`, el stack Web genera al sintetizar la variante con dominio desde el mismo generador (`renderSite({ origin, indexable })`, `lib/web-site.ts`): canonical y hreflang absolutos (`https://<dominio>/…`, con `x-default`) y, solo en prod, `robots.txt` con `Allow: /` y sin `X-Robots-Tag`. No hay una segunda copia del sitio en el repositorio.
- **Desplegar en dev:** lo hace el dueño con el `cdk deploy` de la guía de abajo (o solo `npx cdk deploy AfDevWeb -c env=dev -c account=<id-cuenta-dev> --profile af-dev`); la URL es la salida `WebUrl` (`https://<id>.cloudfront.net`). La caché es de 5 minutos: los cambios se ven solos en ese tiempo. En dev la web no se indexa (`X-Robots-Tag` y `robots.txt`).
- **Sin página 404 propia:** una ruta que no existe devuelve el 403 de S3. No se usan las respuestas de error personalizadas de CloudFront porque se aplicarían también a `/v1/account/delete-*` y taparían el 404 `code_not_found` de la API.

### Conectar `tintalu.com` (D-044: opción A, DNS en Cloudflare)

El dueño decidió publicar ya la web en `tintalu.com` (D-044), sin esperar al dictamen de marca; Play Console y el `applicationId` definitivo **siguen esperando**. El DNS se queda en **Cloudflare**: el certificado de ACM se valida por CNAME fuera de AWS y el CDK **no crea ningún registro DNS** en AWS. Como todavía no hay backend de prod, se despliega **solo la web** (`-c webOnly=true`: `AfProdGlobal` y `AfProdWeb`, sin `AfProdData`, `AfProdApi`, `AfProdCdn` ni `AfProdOps`). Lo ejecutan Codex y el dueño en el equipo del dueño, con su sesión de AWS (ningún agente de Claude despliega). Todo desde `infra/`, con `npm ci` hecho. En los comandos, `<prod>` es el id de 12 dígitos de la cuenta de prod y `<correo>` el del dueño.

1. **Acceso y bootstrap de prod (una vez).** Perfil SSO de la cuenta de prod y bootstrap en las dos regiones (la web va en sa-east-1 y su certificado en us-east-1, que es la única región cuyos certificados usa CloudFront):
   ```
   aws configure sso --profile af-prod
   aws sso login --profile af-prod
   npx cdk bootstrap aws://<prod>/sa-east-1 aws://<prod>/us-east-1 --profile af-prod
   ```
2. **Revisar.** Debe listar solo `AfProdGlobal` y `AfProdWeb`, sin ningún `AWS::Route53::*`:
   ```
   npx cdk ls   -c env=prod -c domain=tintalu.com -c webOnly=true
   npx cdk diff AfProdGlobal AfProdWeb -c env=prod -c account=<prod> -c domain=tintalu.com -c webOnly=true -c alertEmail=<correo> --profile af-prod
   ```
3. **Desplegar.** Los mismos contextos en este comando y en todos los siguientes (si se quita `-c domain`, CloudFront pierde el dominio):
   ```
   npx cdk deploy AfProdGlobal AfProdWeb -c env=prod -c account=<prod> -c domain=tintalu.com -c webOnly=true -c alertEmail=<correo> --profile af-prod
   ```
   `cdk deploy` enseña los cambios de IAM y pide confirmación. `AfProdGlobal` se queda **esperando** con el certificado en «Pendiente de validación» hasta el paso 4: no lo canceles.
4. **CNAME de validación (mientras espera).** Léelos en la consola de ACM (región **us-east-1** › Certificados › `tintalu.com` › Dominios: un nombre y un valor por `tintalu.com` y `www.tintalu.com`), en los eventos del stack («Content of DNS Record is: …») o con:
   ```
   aws acm list-certificates --region us-east-1 --certificate-statuses PENDING_VALIDATION --profile af-prod
   aws acm describe-certificate --region us-east-1 --certificate-arn <arn> --query "Certificate.DomainValidationOptions[].ResourceRecord" --profile af-prod
   ```
   En Cloudflare › `tintalu.com` › DNS › Registros, crea cada uno: tipo **CNAME**, nombre = el `_xxxx.tintalu.com` (o `_xxxx.www.tintalu.com`) de ACM, destino = el `_yyyy.acm-validations.aws.`, **Proxy: «Solo DNS» (nube gris)**. Si la zona tiene registros **CAA**, deben incluir `amazon.com` (si no, ACM no puede emitir). ACM emite el certificado en minutos y el despliegue sigue solo con `AfProdWeb`. **No borres nunca esos CNAME:** ACM los usa para renovar.
5. **Apuntar el dominio.** Al terminar, la salida `AfProdWeb.WebDnsRecords` da los dos registros (y `WebUrl`, el `https://xxxx.cloudfront.net`). En Cloudflare › DNS, borra cualquier registro A, AAAA o CNAME previo de `tintalu.com` y de `www` y crea:
   - `tintalu.com` (nombre `@`) CNAME → `xxxx.cloudfront.net` (Cloudflare lo aplana en el apex), **Solo DNS**;
   - `www` CNAME → `xxxx.cloudfront.net`, **Solo DNS**.

   Sin proxy, para que CloudFront sirva su certificado, TLS 1.2 y las cabeceras de seguridad (con la nube naranja Cloudflare las taparía y haría su propio TLS). Si algún día se activa el proxy, el modo SSL/TLS de Cloudflare tiene que ser **«Full (strict)»** (con «Flexible» iría a CloudFront por HTTP y entraría en bucle con la redirección a HTTPS); lo recomendado sigue siendo «Solo DNS».

   **HSTS con `includeSubDomains`:** la web envía `Strict-Transport-Security` con `includeSubDomains` (2 años), así que los navegadores que la visiten exigirán HTTPS en **cualquier** subdominio de `tintalu.com`. Todo subdominio que se cree (correo web, `blog.`, etc.) tiene que servir HTTPS con un certificado válido.
6. **Comprobar** (esperar unos minutos a que propague el DNS):
   ```
   nslookup tintalu.com                                   # IP de CloudFront, no de Cloudflare
   curl -sI https://tintalu.com/                          # 200, strict-transport-security y sin x-robots-tag
   curl -sI https://www.tintalu.com/tintalu/eliminar-cuenta/
   curl -s  https://tintalu.com/robots.txt                # User-agent: * / Allow: /
   curl -s  https://tintalu.com/config.json               # {"deleteEnabled":false}
   ```
   Y en el navegador: `https://tintalu.com/tintalu/eliminar-cuenta/` muestra que el borrado desde la web estará disponible pronto, con la vía del juego y el correo de privacidad, sin formulario; el código fuente de las páginas lleva `canonical` y `hreflang` con `https://tintalu.com/…`. Los cambios de la web se ven solos en 5 minutos (caché).

   **Correo de privacidad del aviso:** el marcador `[[DATO DEL DUEÑO: correo de privacidad]]` del aviso «disponible pronto» no está en `web/src/content/`, sino en `web/src/site.ts` (campo `soon` de los textos del formulario, uno por idioma: líneas 150, 184 y 218). Se sustituye ahí, `npm run web:build` y se vuelve a desplegar con el comando del paso 3. **Play Console exige un medio de borrado que funcione:** mientras ese correo sea un marcador (y sin la API de prod), la URL de eliminación de cuenta no sirve para Play.
7. **Activar el borrado por web cuando exista la API de prod.** No se toca la web: al desplegar el entorno completo **sin** `webOnly`, el stack Web añade la ruta `/v1/account/delete-*` hacia la API y `config.json` pasa a `{"deleteEnabled":true,"apiBase":""}`, con lo que la página enseña el formulario. Con los secretos y el resto de pasos de «Prod», abajo:
   ```
   npx cdk diff   --all -c env=prod -c account=<prod> -c domain=tintalu.com -c firebaseAppIds=<id de la app Android de prod> -c alertEmail=<correo> --profile af-prod
   npx cdk deploy --all -c env=prod -c account=<prod> -c domain=tintalu.com -c firebaseAppIds=<id de la app Android de prod> -c alertEmail=<correo> --profile af-prod
   ```
   El certificado de la web no cambia (mismo recurso en los dos modos). La CDN queda en `xxxx.cloudfront.net` y la API en `execute-api`; para `cdn.tintalu.com` o `api.tintalu.com`, añade `-c domainExtras=cdn,api`: cada uno crea su certificado (CNAME de validación como en el paso 4) y una salida (`CdnDnsRecord`, `ApiDnsRecord`) con el CNAME que va en Cloudflare, también sin proxy. Comprueba después que `curl -s https://tintalu.com/config.json` devuelve `"deleteEnabled":true`.

**Opción B (no elegida), delegar el DNS a Route 53:** zona alojada `tintalu.com` en la cuenta de prod (coste mensual), NS de Cloudflare cambiados por los de Route 53 y `-c domain=tintalu.com -c hostedZoneId=<id de la zona>`, sin `webOnly`: el CDK crea el certificado compartido (con `cdn.`), su validación, los alias A/AAAA de la web y la CDN y `api.<dominio>`.

Tras conectar el dominio, la URL de eliminación de cuenta (`https://tintalu.com/tintalu/eliminar-cuenta`) y la de la política son las que irán a Play Console (08 §13.1 #2 y #7) cuando haya dictamen.

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

El CDK crea dos secretos con un relleno aleatorio (el SSV de AdMob no necesita secreto: verifica con las claves públicas de Google); mientras lleven el relleno, las funciones que los usan fallan cerradas (500 o 403) y no conceden nada. Cárgalos tú **desde un archivo fuera del repositorio** (nunca los pegues en el chat ni los guardes en `fill-game/`):
```
aws secretsmanager put-secret-value --profile af-dev --region sa-east-1 --secret-id af-dev/play-service-account          --secret-string file://<ruta-fuera-del-repo>/play.json
aws secretsmanager put-secret-value --profile af-dev --region sa-east-1 --secret-id af-dev/firebase-admin-service-account --secret-string file://<ruta-fuera-del-repo>/firebase-admin.json
```
Borra los archivos locales al terminar.

| Secreto | Formato | Lo usa | Permisos que necesita la cuenta |
|---|---|---|---|
| `af-<env>/play-service-account` | La clave JSON tal cual la descarga Google Cloud (`"type": "service_account"`, `client_email` acabado en `.iam.gserviceaccount.com`, `private_key` PKCS#8). Su `token_uri` se ignora: el canje va siempre a `https://oauth2.googleapis.com/token` | verifyPurchase y voidedPurchasesJob (ámbito `androidpublisher`) | En Play Console, la cuenta de servicio invitada con «Ver información financiera» y «Gestionar pedidos y suscripciones» solo de esta app; la Google Play Android Developer API activada en su proyecto |
| `af-<env>/firebase-admin-service-account` | La clave JSON de una cuenta de servicio del proyecto de Firebase del entorno, mismo formato | deleteAccount (ámbito `identitytoolkit`, `accounts:delete`) | Un rol que solo borre usuarios de Auth (p. ej. «Firebase Authentication Admin»); no hace falta Firebase Admin completo. La Identity Toolkit API activada |

Las claves se rotan en Google Cloud y se vuelven a cargar igual; las Lambdas guardan el valor en caché como mucho 5 minutos. La federación sin claves (WIF) queda para la 1.1 (Q-AWS-7).

### 5 bis. Verificación en el servidor (SSV) de AdMob (D-049)

Cuando la API esté desplegada, en la consola de AdMob, en **cada bloque recompensado de producción** (hoy uno, «Tintalu Rewarded», `ca-app-pub-2916158449453694/1182085615`; `docs/dev/anuncios-admob.md` §3 bis. **No** el antiguo `…/2877105387`: es un intersticial recompensado y no está en la lista blanca), abre la configuración avanzada, activa **«Verificación del lado del servidor»** y pon como URL de devolución de llamada:
```
<ApiUrl>/v1/ssv
```
con `<ApiUrl>` la salida `ApiUrl` del stack de prod (`https://<id>.execute-api.<región>.amazonaws.com`) o, si despliegas con `-c domainExtras=api`, `https://api.[[DATO DEL DUEÑO: dominio de la API]]`. Sin parámetros: AdMob añade los suyos (`ad_network`, `ad_unit`, `custom_data`, `reward_amount`, `reward_item`, `timestamp`, `transaction_id`, `user_id`, `signature` y `key_id`). Los bloques de prueba de Google de los builds de desarrollo no llaman a nuestra URL.

- **Solo nuestros bloques:** las claves de firma de Google son las mismas para todos los editores, así que `/v1/ssv` solo acepta los bloques de la lista blanca del entorno (contexto `admobRewardedAdUnits`; por defecto `1182085615` en prod y además `5224354917`, el recompensado de prueba de Google, en dev). Un callback bien firmado de otro bloque o sin `ad_unit` recibe `200 ignored` sin escribir (log `ssv_foreign_ad_unit`). Si creas otro bloque recompensado, añádelo con `-c admobRewardedAdUnits=1182085615,<nuevo>` y despliega **antes** de activar su SSV.
- **Antes de activarlo:** que la API de prod responda (`curl -i "<ApiUrl>/v1/ssv"` → `403 bad_signature`). La prueba de la URL que hace la consola, si llega firmada pero sin `user_id`/`custom_data`, recibe `200 {"status":"ignored"}`. **[NV]** No está comprobado qué manda exactamente esa prueba: si la consola dice que la URL no responde bien, mira el log `ssv_bad_signature` / `ssv_ignored` de la Lambda (sin datos personales) y avisa.
- El cliente pone `UserId` = UID de Firebase Auth anónimo y `CustomData` = `placement|nonce` en `ServerSideVerificationOptions` antes de cada recompensado (08 §10.2); sin UID no fija nada y la recompensa sigue solo provisional.
- La firma se comprueba sobre la consulta **tal cual llega** a API Gateway (`rawQueryString`): no pongas delante ningún proxy que reescriba la consulta.
- No hay secreto que cargar ni rotar: Google rota sus claves y la Lambda las descarga de `https://www.gstatic.com/admob/reward/verifier-keys.json` (salida HTTPS normal; las Lambdas no van en VPC).

### 5 ter. Verificador falso en dev

En dev (y solo en dev: un test de stacks lo comprueba) las funciones llevan `AF_FAKE_PLAY_VERIFIER=1`, porque sin app en Play Console no hay compras ni Play Integrity reales (D-034). Con él:
- compras con `purchaseToken` que empieza por `fake-`: `fake-pending-…` pendiente, `fake-canceled-…` cancelada, `fake-unknown-…` inexistente y cualquier otro `fake-…` comprada (se «reconoce» en memoria); los demás tokens van a Play de verdad;
- integridad: la cabecera `X-Firebase-AppCheck: fake-ok-<algo único>` pasa; cualquier otro token se verifica de verdad contra App Check;
- el job de anuladas no consulta Play.

### 6. Comprobación rápida

- `WebUrl` muestra la portada de Tintalú; `WebUrl/tintalu/eliminar-cuenta`, `WebUrl/config.json` (`{"deleteEnabled":true,"apiBase":""}`), `WebUrl/app-ads.txt` y `WebUrl/robots.txt` (que en dev prohíbe indexar) existen. En la página de borrado, un código con formato válido que no existe responde «No encontramos ese código» (la API devuelve 404 `code_not_found`).
- `curl -i "<ApiUrl>/v1/ssv"` → `403` (`bad_signature`: sin consulta firmada por AdMob no se mira nada más); `curl -i -X POST <ApiUrl>/v1/purchases/verify` sin token → `401`.
- Con un ID token de un usuario anónimo de dev: `POST /v1/account/code` devuelve un código; `POST /v1/ledger/restore` con `{"pending_nonces":[]}` devuelve listas vacías; `POST /v1/purchases/verify` con `X-Firebase-AppCheck: fake-ok-1` y `{"token":"fake-ok-1","sku":"af_coins_2"}` devuelve `ok` y, repetido (con otro `fake-ok-…`), `duplicate`.
- `curl -i <CdnUrl>/catalog.json` → `403` hasta que se suba un catálogo (no hay listado).

### 7. Borrar dev

`npx cdk destroy --all -c env=dev -c account=<id-cuenta-dev> --profile af-dev`. Antes vacía los buckets de la web, del contenido y de logs (en dev se borran con el stack, pero S3 no borra un bucket con objetos).

## Prod

Igual que dev con `-c env=prod`, el perfil `af-prod`, la cuenta de prod y **siempre `-c domain=tintalu.com`**: desde D-044 todos los comandos de prod (`synth`, `diff`, `deploy`, `destroy`) lo llevan. Sin él la síntesis de prod falla a propósito; si se forzara con `-c noDomain=true`, el despliegue quitaría los alias `tintalu.com` y `www.` de CloudFront e intentaría borrar el certificado de ACM en uso, y la web caería. En prod las tablas, los buckets, los secretos y los logs se **retienen** al borrar el stack, y `Data` y `Api` tienen protección contra la terminación. La web pública en `tintalu.com` se publica ya (D-044, solo web: «Conectar `tintalu.com`» arriba); Play Console y el `applicationId` definitivo siguen esperando al dictamen de marca. El verificador falso de Play no existe en prod (un test lo comprueba). Para que las compras funcionen en prod hace falta `-c packageName=<applicationId definitivo>`, que solo existe tras el dictamen de marca. La síntesis de prod exige `-c firebaseAppIds=<id de la app Android de prod>` (sin él falla con un mensaje que lo pide):
```
npx cdk synth -c env=prod -c domain=tintalu.com -c firebaseAppIds=1:<número>:android:<hash>
```

Las referencias entre stacks son débiles (`Fn::GetStackOutput`, el valor por defecto de esta versión de CDK): CloudFormation no impide borrar `Data` o `Api` aunque otros stacks los usen. En prod lo frenan la retención y la protección contra la terminación; aun así, borra en orden inverso (`Ops`, `Cdn`, `Web`, `Api`, `Data`, `Global`).

## Liga del río — D-046

Backend Global/País de la 1.0, contrato sin cambios de `docs/dev/liga.md`; detalles operativos y estimación en [LEAGUE.md](LEAGUE.md). Dos funciones adicionales Node 24 arm64 (`league-wins`, `league-board`), ambas con JWT Firebase y App Check, y cuatro tablas cifradas bajo demanda. API: `POST /v1/league/wins` y `GET /v1/league?scope=global|country`. `deleteAccount` limpia la Liga; Ops añade errores/throttles de las funciones y throttles de las tablas. Sin VPC/NAT ni caché provisionada; las alarmas sí tienen coste mensual.

Tabla de dificultad desde tutorial/mundos publicados 1–700, nunca reservas ni escritura manual:

```sh
npm run league:levels
npm run league:check
```

`npm test` comprueba esa tabla antes de Vitest. No cambiar `levels.generated.json` a mano. El tutorial sin etiqueta es Normal, igual que `LevelPackReader`; no puntúa antes del 41. País: en este despliegue se usa `region` validado, sin añadir CloudFront a la Liga. No aceptar una cabecera `CloudFront-Viewer-Country` en el endpoint directo. Las URL base del cliente se conservan.

Para diff/despliegue, preservar **todos los contextos reales del dev existente**, incluidos `firebaseProjectId`, `firebaseAppIds`, `packageName`, `account` y `alertEmail`, con `--profile af-dev`. No usar valores de ejemplo contra el entorno real. Revisar `cdk diff AfDevData AfDevApi AfDevOps --no-change-set` con esos contextos, obtener el sí del dueño y desplegar esas tres pilas desde el mismo ensamblado revisado. No desplegar Web/Cdn/Global por este cambio. Comprobar 401 sin token en los dos endpoints; con tokens de prueba proporcionados por el dueño, enviar una victoria y leer Global y País. Los tokens no se escriben en el repo ni en el chat. La activación de `live_league_enabled` y el cliente Unity los lleva el integrador.
