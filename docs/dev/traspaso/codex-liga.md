# Encargo para Codex: el backend de la Liga del río, su despliegue en dev y sus textos legales

Fecha: 2026-10-23. Lo escribió el integrador (Claude, en el escritorio). Es la decisión **D-046**, punto 1, en `docs/dev/decisiones.md`. El dueño pidió (literal): «encarga a codex algo grande que pueda hacer en paralelo tambien».

**Fuente única:** `docs/dev/liga.md`, con lo que ve el jugador, el contrato de la API y las reglas de puntos, país, integridad y posición. El cliente de Unity lo programa en paralelo el escritorio contra ese mismo contrato, así que **no cambies el contrato por tu cuenta**. Si hace falta cambiarlo, escribe la propuesta en `docs/dev/traspaso/codex-estado.md` y espera a que el integrador actualice `liga.md`.

**Rama:** `codex/liga`, desde `origin/integracion-fase1-fase2` (en `a656cca4` o después), en tu propio worktree. Las reglas de siempre (`codex-aws.md` §3):
- sin secretos en el repo;
- nada a Google Play;
- ningún despliegue sin el diff revisado y el sí del dueño;
- sin push force.

El encargo de la web (`codex-web-marca.md`) sigue en paralelo en su rama. Haz los textos legales de la Liga (punto 4) **en esa rama**, para no editar la política en dos sitios.

## 1. Contrato y datos

- **Esquemas** en `infra/contracts/v1`: `league-wins.request`, `league-wins.response` y `league-board.response`, con sus ejemplos y su entrada en `index.json`, exactamente como `liga.md` §3.1, y con los tests de contrato que ya usa el resto.
- **La tabla `level → dificultad`,** generada desde `Assets/_Project/Data/Levels` (los packs de campaña publicados, 1-700) con una herramienta versionada (`infra/` o `tools/`, con su `--check`). Nunca escrita a mano. Los puntos de cada dificultad son los de `liga.md` §1.
- **Seudónimos:** la lista de las 24 claves `league_name_*` está fijada en `liga.md` §1. Tú la usas tal cual con 4 cifras; los textos los pone el escritorio en `strings.csv`.

## 2. Infraestructura y funciones (CDK v2, Node 24 arm64, como las otras 5)

- **Tablas DynamoDB bajo demanda:**
  - jugador por semana: puntos, país, `win_id` vistos y marca de sospecha;
  - índice para el top por `semana#ámbito`;
  - histograma por tramo de puntos para el percentil;
  - seudónimo por `uid`.

  Con TTL de unas 3 semanas en lo semanal (`liga.md` §3.4).
- **Funciones `leagueWins` y `leagueBoard`:** detrás de la API HTTP con el autorizador de siempre (Firebase ID token y App Check), con las reglas de integridad de `liga.md` §3.2 y el país de §3.3.
  - Lo recomendado: servir `/v1/league/*` por una distribución de CloudFront para tener `CloudFront-Viewer-Country`.
  - Si no lo haces, el respaldo es `region`. Explica lo que elijas.
- **`deleteAccount`:** que borre también la Liga del `uid`, con su test.
- **Ops:** alarmas como las demás funciones y nada de coste fijo. Estima el coste mensual con 1.000 y con 100.000 jugadores activos.
- **Tests de vitest:**
  - las reglas: puntos, semana, duplicados, ritmo, niveles imposibles y Salto del Boto;
  - top y percentil, con empates;
  - país;
  - borrado;
  - las aserciones de CDK, sin coste fijo ni recursos públicos.
- **Verificación en `infra/`:**
  - `npm ci`, `npm run lint`, `npm run typecheck`, `npm test` y `npm run web:check`;
  - `npx cdk synth -c env=dev --quiet`;
  - el synth de prod con `-c domain=tintalu.com` y `firebaseAppIds` de ejemplo.
- **Actualiza 08 §11** (la lista de funciones y endpoints) e `infra/README.md`.

## 3. Despliegue en dev (con el dueño)

1. `cdk diff` de dev. El dueño lo lee, sobre todo IAM y lo nuevo, y solo con su sí, `cdk deploy`.
2. Comprobación con `curl`:
   - sin token, 401;
   - con un token de prueba del dueño, si lo da, un envío y la lectura de `global` y `country`.
3. Anota las URL en `codex-estado.md`. Si cambian, el escritorio pone la base en `BackendEndpoints`.

## 4. Textos legales de la Liga (en tu rama `codex/web-marca`)

Lo de `liga.md` §6:
- la política, en los tres idiomas: el seudónimo, la bandera y los puntos que ven otros jugadores, de dónde sale el país, que se borra con la cuenta y que con <13 no hay Liga;
- la fila de la Seguridad de los datos en `docs/store-listing/`.

Todo con `[[ABOGADO: revisar]]`. Regenera el sitio con `npm run web:build`.

## 5. Qué devolver

En `docs/dev/traspaso/codex-estado.md`:
- los commits de `codex/liga`;
- los comandos con sus resultados;
- las URL de dev;
- el coste estimado;
- lo que elegiste para el país;
- las preguntas, con la recomendada aplicada.

Empuja `codex/liga`. El escritorio la revisa e integra.
