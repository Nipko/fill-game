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

## Revisión 1 (2026-10-24): cambios necesarios

El code-reviewer revisó `codex/liga` en `9c54e88b`. Están bien el contrato, la concurrencia, el borrado, el país, la seguridad, el IAM y los 337 tests. Queda **un bloqueante**, y el integrador fijó la regla en `docs/dev/liga.md` §3.2 (revisión del 2026-10-24; léela entera).

1. **B1 (bloqueante), [infra/functions/league/rules.ts:61, :80; service.ts:92]:**
   - el avance de referencia (`alias.highest`) solo sube con las victorias aceptadas;
   - una victoria perdida o rechazada abre un hueco, y desde entonces todo sale `unreachable_level` y la cuenta queda `flagged` para siempre;
   - los casos reproducidos: el paso de semana sin red, la 41.ª victoria de una hora, la Liga apagada, una reinstalación y un reloj adelantado.

   **Corrección:**
   - la referencia avanza con toda victoria de campaña bien formada, aceptada o rechazada;
   - un hueco mayor de lo normal repone la referencia y la victoria cuenta;
   - `unreachable_level` solo con `level > max_level`.

   Tests de regresión de esos casos.
2. **Sin marca automática:** quita la marca por `rate_limited` y por `unreachable_level`. `flagged` queda solo para marcar a mano.
3. **S1:** 5 minutos de margen para el futuro (`rules.ts:55`).
4. **S2:**
   - cada victoria se juzga por separado (`invalid` para ella, nunca un 400 al lote por un elemento);
   - `region` mala, vacía o null, como desconocida;
   - sin tope de 700 en `level` y `max_level`: un nivel que no está en la tabla sale `invalid`.

   Ajusta el esquema de petición, porque así lo dice ahora §3.2. La forma de la petición y de la respuesta no cambia.
5. **S3:** un nivel por debajo del desbloqueo da `invalid`, sin marca.
6. **S4:** ordena por nivel las victorias de campaña del lote antes de evaluarlas.
7. **S5:** en 08 §2.4, matiza que la Liga recortada va en la 1.0 (D-046).

**Después:**
- la verificación de siempre;
- el `cdk diff` de dev, solo de AfDevApi si no cambia nada más; el dueño lo aprueba y lo despliegas;
- regístralo en `codex-estado.md` y empuja `codex/liga`.
