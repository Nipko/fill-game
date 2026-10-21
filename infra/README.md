# infra/: backend de Amazon Fill en AWS (D-035)

Infraestructura como código con **AWS CDK v2 en TypeScript** (Q-AWS-1). La escriben y la validan los agentes **sin credenciales**; **despliega el dueño** con su sesión de IAM Identity Center. Plan completo: `docs/dev/plan-backend-aws.md`; decisión: D-035 en `docs/dev/decisiones.md`.

Ninguna cuenta, correo, clave ni secreto vive en este repositorio: lo que identifica una cuenta o a una persona se pasa por contexto (`-c clave=valor`) al ejecutar CDK, y los secretos se cargan en Secrets Manager después del despliegue.

## Qué hay

| Stack | Región | Contenido |
|---|---|---|
| `Af<Env>Global` | us-east-1 | AWS Budgets (10 USD en dev, 50 en prod; avisos al 50, 80 y 100 %) y, solo con dominio, el certificado de ACM de CloudFront |
| `Af<Env>Data` | sa-east-1 | 6 tablas de DynamoDB (`af-<env>-purchases`, `rewards`, `integrity`, `account_codes`, `rate_limits`, `jobs`): on-demand, cifradas, PITR; en prod, protección contra borrado y retención |
| `Af<Env>Api` | sa-east-1 | HTTP API con autorizador JWT de Firebase Auth, rutas `/v1/...` del contrato, 5 Lambdas (Node 24, arm64; **por ahora responden 501**), límites por ruta, 3 secretos de relleno, job diario a las 03:00 UTC con DLQ |
| `Af<Env>Web` | sa-east-1 | Web estática: S3 privado + CloudFront con OAC, cabeceras de seguridad, `noindex` en dev, página de marcador de posición y `app-ads.txt` provisional; reenvía `/v1/account/delete-*` a la API (borrado por web, mismo origen) |
| `Af<Env>Cdn` | sa-east-1 | Contenido remoto: bucket `af-<env>-content-<cuenta>` tras CloudFront; `packs/*` inmutables (1 año), `catalog.json` y el resto 5 minutos; sin listado ni CORS |
| `Af<Env>Ops` | sa-east-1 | Alarmas (errores y throttles de Lambda, 5xx de la API, fallo del job, throttles de DynamoDB, cuota de integridad al 70 %) con aviso por SNS al correo |

Otros directorios: `functions/` (código de las Lambdas), `contracts/v1/` (contrato de la API: fuente única, Q-AWS-12; `index.json` fija rutas, autorización y límites), `web/site/` (web estática), `test/` (unitarios, de contrato y de stacks).

## Validar sin credenciales (agentes y CI)

Node 22.18 o posterior (Node ejecuta los `.ts` directamente; el runtime de las Lambdas es Node 24). Desde `infra/` y **sin variables `AWS_*`**:

```
npm ci
npm run lint
npm run typecheck
npm test
npx cdk synth -c env=dev
npx cdk synth -c env=prod
```

`cdk synth` aplica **cdk-nag** (AwsSolutions) como plugin de validación: cualquier hallazgo sin reconocer hace fallar la síntesis. Cada excepción está reconocida en el código con su motivo (`ack(...)` en `lib/`). La síntesis no hace búsquedas en la cuenta (`fromLookup` está prohibido) y `cdk.json` desactiva la telemetría, los avisos y el informe de versión de la CLI.

## Contexto admitido

| Clave | Obligatoria | Ejemplo | Para qué |
|---|---|---|---|
| `env` | Sí | `dev`, `prod` | Entorno |
| `account` | Para desplegar, **sí** (recomendado) | id de 12 dígitos | Fija la cuenta: CDK se niega a desplegar si el perfil apunta a otra |
| `alertEmail` | Para recibir avisos | tu correo | Avisos de alarmas (SNS) y de presupuesto |
| `firebaseProjectId` | No | `amazon-fill-dev` | Emisor y audiencia del JWT; por defecto `amazon-fill-dev` / `amazon-fill-prod` (08 §15.3) |
| `domain` + `hostedZoneId` | No (los dos o ninguno) | dominio y zona de Route 53 | Web en `<dominio>` y `www.`, CDN en `cdn.`, API en `api.`; **solo prod y solo tras el dictamen de marca** (Q-AWS-9) |

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

El CDK crea tres secretos con un relleno aleatorio. Cárgalos tú **desde un archivo fuera del repositorio** (nunca los pegues en el chat ni los guardes en `fill-game/`):
```
aws secretsmanager put-secret-value --profile af-dev --region sa-east-1 --secret-id af-dev/play-service-account          --secret-string file://<ruta-fuera-del-repo>/play.json
aws secretsmanager put-secret-value --profile af-dev --region sa-east-1 --secret-id af-dev/firebase-admin-service-account --secret-string file://<ruta-fuera-del-repo>/firebase-admin.json
aws secretsmanager put-secret-value --profile af-dev --region sa-east-1 --secret-id af-dev/max-ssv-key                    --secret-string file://<ruta-fuera-del-repo>/max-ssv.txt
```
Borra los archivos locales al terminar. Mientras las Lambdas sean esqueletos (F2-2) no los leen: puedes dejarlo para cuando lleguen F2-4 a F2-8.

### 6. Comprobación rápida

- `WebUrl` muestra «Sitio en preparación»; `WebUrl/app-ads.txt` y `WebUrl/robots.txt` (que en dev prohíbe indexar) existen.
- `curl -i <ApiUrl>/v1/ssv` → `501` (`not_implemented`); `curl -i -X POST <ApiUrl>/v1/purchases/verify` sin token → `401`.
- `curl -i <CdnUrl>/catalog.json` → `403` hasta que se suba un catálogo (no hay listado).

### 7. Borrar dev

`npx cdk destroy --all -c env=dev -c account=<id-cuenta-dev> --profile af-dev`. Antes vacía los buckets de la web, del contenido y de logs (en dev se borran con el stack, pero S3 no borra un bucket con objetos).

## Prod

Igual que dev con `-c env=prod`, el perfil `af-prod` y la cuenta de prod. En prod las tablas, los buckets, los secretos y los logs se **retienen** al borrar el stack, y `Data` y `Api` tienen protección contra la terminación. El dominio (`-c domain=… -c hostedZoneId=…`) y la web pública solo **tras el dictamen de marca** (D-034, Q-AWS-9; plan F5). El verificador falso de Play no existe en prod (un test lo comprueba).
