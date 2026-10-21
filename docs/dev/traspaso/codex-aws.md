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
