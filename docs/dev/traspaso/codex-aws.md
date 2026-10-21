# Encargo para Codex: montar AWS (dev) con el dueño

Fecha: 2026-10-21. Lo escribió el integrador (Claude, en el escritorio). El dueño pidió (literal): «pasale el encargo a codex para hacer lo de aws».

## 1. Qué hay y qué falta

- **Decidido:** D-035 y sus añadidos en `docs/dev/decisiones.md` (backend híbrido: Firebase para Auth anónimo, Analytics, Crashlytics y Remote Config; AWS para la web, el CDN, las 5 funciones con DynamoDB y la 1.2). Plan: `docs/dev/plan-backend-aws.md`. D-036: permiso `INTERNET` aprobado.
- **Hecho en el repo (equipo de Claude):** `infra/` con AWS CDK v2 en TypeScript: stacks `Af<Env>Global`, `Data`, `Api`, `Web`, `Cdn` y `Ops`, el contrato `infra/contracts/v1` y los tests. Se valida sin credenciales. **Las Lambdas todavía responden 501**: las funciones reales (F2-3 a F2-8) y el CI `infra.yml` los está escribiendo ahora el equipo de Claude.
- **Lo que te toca a ti (Codex), con el dueño:** la parte de cuentas y consola de AWS y el primer despliegue de **dev**, siguiendo `infra/README.md` («Guía del dueño: desplegar dev»). Es lo que el equipo de Claude no hace porque no maneja credenciales.

## 2. Pasos (F0-4 y F4-1 del plan)

1. **AWS Organizations:** dos cuentas, dev y prod (Q-AWS-3). Nombres internos sin la marca (por ejemplo `amazonfill-dev` y `amazonfill-prod`).
2. **IAM Identity Center** en la cuenta de gestión: usuario del dueño con **MFA**, conjunto de permisos en cada cuenta. **Nada de usuarios IAM con claves de acceso**, ni uso de la cuenta root salvo lo imprescindible.
3. **AWS CLI v2** en el equipo del dueño y perfiles `af-dev` y `af-prod` con `aws configure sso`; `aws sso login --profile af-dev`.
4. **Bootstrap de CDK** en dev, en las dos regiones (Q-AWS-5): desde `infra/`, `npm ci` y luego
   `npx cdk bootstrap aws://<id-cuenta-dev>/sa-east-1 aws://<id-cuenta-dev>/us-east-1 --profile af-dev`.
5. **Revisar y desplegar dev:** `npx cdk diff --all -c env=dev -c account=<id-cuenta-dev> -c alertEmail=<correo> --profile af-dev`; el dueño lee el diff (sobre todo los cambios de IAM) y, **solo con su sí**, `npx cdk deploy --all` con los mismos parámetros. Después, el dueño confirma por correo las suscripciones de SNS.
6. **Presupuesto (Q-AWS-6):** comprobar en Billing que el presupuesto de 10 USD de dev existe y avisa al correo.
7. **Comprobación rápida** (README §6): `WebUrl` con «Sitio en preparación», `GET <ApiUrl>/v1/ssv` → 501, `POST <ApiUrl>/v1/purchases/verify` sin token → 401, `<CdnUrl>/catalog.json` → 403.
8. **Firebase dev** (si el dueño quiere avanzarlo): proyecto `amazon-fill-dev` (08 §15.3 #2) con Auth anónimo, Remote Config, Analytics, Crashlytics y App Check. Su id es el que usa el autorizador JWT (`-c firebaseProjectId`, por defecto `amazon-fill-dev`).
9. **Secretos:** todavía no hace falta cargarlos (las Lambdas son esqueletos). Cuando lleguen las funciones reales, el dueño los carga desde archivos **fuera del repositorio** (README §5).

## 3. Reglas

- **Solo dev.** Prod (`-c env=prod`), el dominio `tintalu.com` y cualquier cosa pública esperan al dictamen de marca (D-034, Q-AWS-9).
- **Nunca** credenciales, ids de cuenta, correos, claves, ARNs con datos personales, capturas ni salidas de la CLI con secretos en el repositorio ni en el chat. Los ids de cuenta van solo por `-c account=` en la línea de comandos del dueño.
- **Cada `cdk deploy` y cada cambio que cueste dinero, lo confirma el dueño** después de ver el `cdk diff`.
- **No edites `infra/`** (código, stacks ni contrato): es del equipo de Claude y se está modificando ahora. Si algo falla al desplegar o el diff no cuadra, apúntalo en `docs/dev/traspaso/codex-estado.md` con el error (sin secretos) y el integrador lo arregla.
- Cambios en el repo, solo documentación (`docs/dev/traspaso/codex-estado.md` y, si hace falta, notas en `docs/publicacion/`), en tu rama `codex/aws-dev` desde `origin/integracion-fase1-fase2`; el integrador fusiona. **No cambies la rama del checkout principal del escritorio** (`C:\Users\USER\Desktop\Projects\fill-game`, en `integracion-fase1-fase2`): trabaja en un clon o worktree tuyo.

## 4. Qué apuntar en `codex-estado.md`

Fecha y qué quedó hecho (cuentas creadas sí/no, Identity Center, perfiles, bootstrap en las dos regiones, despliegue de dev y sus salidas `ApiUrl`, `WebUrl` y `CdnUrl` (son URL públicas de AWS, sin secretos), presupuesto, comprobaciones del paso 7 y Firebase dev), y cualquier error o pregunta para el integrador.

## 5. Siguiente encargo (2026-10-21): dev con las funciones reales y Firebase dev

Lo desplegado el 2026-10-21 salió de la base `8771270`, con las Lambdas de esqueleto. Desde entonces el equipo de Claude integró en `infra/` las 5 funciones reales (`ae8c8ea`), el CI `infra.yml` (`c60d40b`) y el endurecimiento F2c (`cd750f10`). Con el mismo método y las mismas reglas de las secciones 2 y 3:

1. **Actualizar el despliegue de dev:** `git pull` de `origin/integracion-fase1-fase2` en tu worktree; desde `infra/`, `npm ci` y `cdk diff -c env=dev -c account=<id-dev> -c alertEmail=<correo> --profile af-dev`. El dueño revisa el diff (sobre todo los cambios de IAM de las funciones y las tablas) y, **solo con su sí**, `cdk deploy`. La CLI de CDK instalada ignora `--all` en `diff`: no hace falta pasarlo.
2. **Comprobaciones tras el deploy:** `POST <ApiUrl>/v1/purchases/verify` sin token → 401; `GET <ApiUrl>/v1/ssv` sin firma → 4xx (ya no 501); las alarmas en estado OK. Las funciones fallan cerradas mientras falten los secretos y Firebase: es lo esperado.
3. **Firebase dev** (con el dueño, en la consola de Firebase): proyecto con id **`amazon-fill-dev`** (si el id elegido es otro, anótalo: el despliegue tendrá que pasar `-c firebaseProjectId=<id>`, porque el autorizador JWT de la API valida emisor y audiencia con ese id); app Android `com.amazonfill.dev`; **Authentication › anónimo** activado; Remote Config, Analytics y Crashlytics como estén por defecto. **App Check:** todavía no hay app en Play Console (D-034), así que Play Integrity no puede dar veredictos reales; deja App Check registrado sin **exigirlo** (sin «enforce») y anota el id de la app Android (`1:…:android:…`), que dev podrá pasar como `-c firebaseAppIds=`. **No** descargues ni subas `google-services.json` al repo ni al chat (regla 5): si el dueño lo descarga, se guarda fuera del repositorio.
4. **Secretos de dev:** todavía no. `af-dev/firebase-admin-service-account` (cuenta de servicio de Firebase con el rol mínimo para borrar usuarios de Auth) se podrá cargar cuando exista el proyecto, siempre desde un archivo fuera del repo con `put-secret-value`; los de Play y MAX esperan a esas cuentas. Formato de cada uno: `infra/README.md`.
5. **Apunta en `codex-estado.md`** el resultado (diff resumido, deploy, comprobaciones, id del proyecto de Firebase y de la app Android, que son identificadores públicos del cliente) en tu rama `codex/aws-dev`, como antes.
