# Liga del río en la 1.0: especificación y contrato (D-046)

Fecha: 2026-10-23. La escribió el integrador. La pidió el dueño: «una clasificación global y por paises que sepa como va uno… agreguemos la liga como recomendas inicialmente pero pasemosla para esta primera versión».

**Base:** GDD 06 §6 (Liga del río, prevista para la 1.2). En la 1.0 entra **solo la parte de País y Global**, con puntos semanales. La cubeta semanal de 50 jugadores, los tiers y los premios con semillas se quedan en la 1.2. El backend va en AWS (D-035), sin Unity Leaderboards ni Cloud Code.

**Quién hace qué:**
- **backend** (`infra/`, contratos, Lambdas, CDK y su despliegue en dev): Codex, con `docs/dev/traspaso/codex-liga.md`;
- **cliente** (Unity): el equipo de Claude en el escritorio, en la rama `tanda6/liga-cliente`;
- **textos legales:** Codex, en la web.

Esta especificación es la fuente única de los dos lados. Si hay que cambiar el contrato, se cambia **aquí** primero, en un commit del integrador.

## 1. Qué ve el jugador

- **Desbloqueo:** la Liga aparece al ganar el nivel 41. Hasta entonces, nada.
- **Entrada:** un botón «Liga» en el inicio, desde el 41, con su icono. Al pulsarlo se abre la pantalla de la Liga.
- **Pantalla de la Liga:**
  - pestañas **Global** y **{país}** (bandera y nombre del país del jugador). Con el país desconocido, solo Global;
  - cabecera con «Semana del {inicio} al {fin}» y «Termina en {tiempo}»;
  - la **fila del jugador** siempre visible, fija abajo:
    - con su posición exacta si está en el top 100;
    - si no, «Estás en el {p} % mejor»;
    - y sus puntos;
  - el **top 100**: posición, seudónimo, bandera (en Global) y puntos;
  - sin conexión, lo último que se descargó, con «Actualizado hace {tiempo}»; sin nada guardado, «Conéctate para ver la Liga».
- **En la victoria:** una línea pequeña «+{n} puntos de Liga» desde el 41, cuando el nivel suma puntos [P].
- **Puntos** (06 §6):
  - victoria en un nivel de campaña: Normal 10, Difícil 20, Muy difícil 30;
  - Reto del río ganado: 20.

  Cada nivel de campaña suma **una vez por semana**, y el Reto, una vez por día.
- **Semana:** de lunes 12:00 UTC a lunes 12:00 UTC (06 §6). Cada lunes los puntos vuelven a 0. Sin archivo de semanas en la 1.0.
- **Seudónimo:** automático y sin texto libre: un animal del glosario y 4 cifras, p. ej. «Capibara 4821». Lo asigna el servidor la primera vez y no se puede editar en la 1.0. El animal viaja como **clave** (`name_key`), así el cliente lo pinta en su idioma; las cifras, como número (`name_num`).
- **Los animales del seudónimo** (24, en este orden; índice 0-23; nacen `pendiente_asesor`): `league_name_capybara`, `league_name_jaguar`, `league_name_toucan`, `league_name_macaw`, `league_name_boto`, `league_name_sloth`, `league_name_tapir`, `league_name_otter`, `league_name_manatee`, `league_name_turtle`, `league_name_howler`, `league_name_squirrel_monkey`, `league_name_caiman`, `league_name_anaconda`, `league_name_dart_frog`, `league_name_morpho`, `league_name_hummingbird`, `league_name_heron`, `league_name_kingfisher`, `league_name_anteater`, `league_name_armadillo`, `league_name_ocelot`, `league_name_coati`, `league_name_arapaima`.
  - El servidor elige el animal y las cifras al azar al crear el seudónimo, sin repetir la pareja animal-cifras.
  - El cliente pone el texto de cada clave en `strings.csv` en los tres idiomas.
  - Una clave desconocida (si el servidor tiene una lista más nueva) se pinta como «Jugador {n}».
- **País:** lo detecta el servidor (§3.3) y se fija para toda la semana con el primer envío. Ajustes muestra «País en la Liga: {país}», sin poder cambiarlo en la 1.0.
- **Edad:**
  - con <13 no hay Liga (D-033: no hay red ni SDK);
  - 13-17 y 18+ la ven igual: solo hay seudónimo y bandera, nunca nada que identifique.
- **Interruptor:** `live_league_enabled` en Remote Config (§5), **apagado por defecto** hasta que haya backend en prod. En los builds de desarrollo se enciende con un extra de la sonda.

## 2. Qué no entra en la 1.0

Se quedan en la 1.2 (06 §6): la cubeta semanal de 50, los tiers, los premios de semillas y Canoas, los marcos de «Mejor del país» y «Mejor del río», el archivo de semanas anteriores, editar el seudónimo y elegir el país.

## 3. Backend (Codex)

### 3.1 Endpoints

Entran en `infra/contracts/v1` con sus esquemas, sus ejemplos y su entrada en `index.json`, como los demás. Llevan el mismo autorizador: el ID token de Firebase Auth anónimo y el token de App Check (D-035).
- `POST /v1/league/wins`, petición `league-wins.request.schema.json`:
  ```json
  { "wins": [ { "win_id": "c:57", "kind": "campaign", "level": 57, "won_at": "2026-10-23T15:04:05Z" },
              { "win_id": "d:2026-10-23", "kind": "daily", "day": "2026-10-23", "won_at": "2026-10-23T16:00:00Z" } ],
    "max_level": 58, "region": "CO" }
  ```
  - Hasta 50 victorias por petición: la cola sin conexión manda en lotes.
  - `win_id` hace **idempotente** cada victoria: repetirla no suma.
  - `region` es la región del teléfono (ISO 3166-1 alfa-2), el respaldo de §3.3.
- **Respuesta 200**, `league-wins.response.schema.json`:
  ```json
  { "week": "2026-W43", "points": 140, "accepted": ["c:57"], "rejected": [ { "win_id": "d:2026-10-23", "reason": "duplicate" } ],
    "name_key": "league_name_capybara", "name_num": 4821, "country": "CO" }
  ```
  `reason` puede ser `duplicate`, `rate_limited`, `unreachable_level`, `outside_week` o `invalid`.
- `GET /v1/league?scope=global|country` → 200, `league-board.response.schema.json`:
  ```json
  { "week": "2026-W43", "starts_at": "2026-10-19T12:00:00Z", "ends_at": "2026-10-26T12:00:00Z", "scope": "country", "country": "CO",
    "updated_at": "2026-10-23T20:00:00Z",
    "top": [ { "rank": 1, "name_key": "league_name_jaguar", "name_num": 1203, "country": "CO", "points": 980 } ],
    "me": { "points": 140, "rank": null, "percentile": 12, "name_key": "league_name_capybara", "name_num": 4821, "country": "CO" } }
  ```
  - `top`: como mucho 100 filas, con los empates por quien llegó antes a esos puntos.
  - `me.rank` es exacto si el jugador está en el top 100; si no, `null` con `percentile` (1-100, «estás en el p % mejor»).
  - Sin puntos esa semana: `me.points` 0, `rank` y `percentile` `null`.
  - Con `scope=country` y país desconocido: 404 `country_unknown`.

### 3.2 Puntos e integridad (06 §6, «Antifraude»)

- **El cliente nunca manda puntos:** el servidor los calcula con una tabla `level → dificultad` generada desde los packs (`Assets/_Project/Data/Levels`), con una herramienta versionada, nunca escrita a mano. La tabla se publica con el backend.
- **Formato de `win_id`:** `c:{level}` para la campaña y `d:{day}` para el Reto (AAAA-MM-DD). Un `win_id` que no case con su `kind` y su `level` o `day` sale `invalid`.
- **Cada victoria se juzga por separado:** una mala da `invalid` (o su `reason`) solo a ella, nunca un 400 a todo el lote. Una `region` vacía, en minúsculas o desconocida se trata como desconocida, no como error. Un `level` que no está en la tabla (contenido futuro) sale `invalid`, sin tope fijo de 700 en el esquema.
- **Orden:** el servidor ordena las victorias de campaña del lote por nivel antes de evaluarlas.
- **Rechazos:**
  - más de 40 victorias en una hora (ventana móvil por `won_at`): `rate_limited`;
  - un `level` mayor que `max_level`: `unreachable_level`;
  - fechas fuera de la semana o con más de **5 minutos** de futuro (margen de reloj): `outside_week`;
  - un nivel por debajo del desbloqueo de la Liga: `invalid`.
- **Avance de referencia (revisión del 2026-10-24):** el servidor guarda el nivel más alto visto del jugador.
  - La primera victoria lo fija.
  - **Avanza con toda victoria de campaña bien formada,** aceptada o rechazada por cualquier motivo. Hoy es solo informativo: ninguna regla de rechazo lo lee.
  - Un salto de +1 es normal, y de +2 si el nivel omitido es `skippable` (Salto del Boto, D-008).
  - **Un hueco mayor no se rechaza ni marca:** la referencia se repone a ese nivel, como con la primera victoria, y la victoria cuenta. Una victoria puede no llegar nunca (cola perdida al reinstalar, la Liga apagada, una semana pasada sin red), y el progreso lo decide el cliente: no hay prueba de juego en el servidor (D-046).
- **Sin marca automática en la 1.0:** ningún rechazo marca la cuenta. `flagged` existe para marcar a mano (operación). Las cuentas marcadas siguen sumando para sí, pero se ocultan del top de los demás.
- **Borrado de cuenta:** `deleteAccount` borra también todo lo de la Liga de ese `uid`, incluido el seudónimo.

### 3.3 País

- **El orden:**
  1. la cabecera `CloudFront-Viewer-Country`, si la petición llega por CloudFront (recomendado: servir `/v1/league/*` por la distribución que ya enruta `/v1/account/delete-*`, o la que decida Codex);
  2. si no, `region` de la petición, validado contra ISO 3166-1;
  3. si no, desconocido, y solo hay Global.
- Se guarda el país de la semana con el primer envío aceptado y no cambia hasta el lunes.

### 3.4 Clasificación y posición

- **Top 100 exacto por ámbito** (global y cada país) con una consulta ordenada (DynamoDB: una partición por `semana#ámbito` y la clave de orden por puntos).
- **Percentil aproximado** con un histograma de puntos por ámbito y semana: contadores por tramo, sin recorrer la tabla.
- **Coste:** sin ElastiCache ni ningún servicio con coste fijo; todo bajo demanda y con TTL de unas 3 semanas en las filas de la semana.

## 4. Cliente (equipo de Claude)

- **`AF.LiveOps` (sin motor):**
  - `LeagueRules`: semana, puntos por dificultad (solo para la línea de la victoria, porque el servidor manda) y desbloqueo;
  - `LeagueQueue`: la cola de victorias, persistida en el perfil, que sobrevive sin conexión y manda en lotes;
  - `ILeagueApi`, con implementación falsa para el editor y los tests.
  - Las respuestas `accepted` y `rejected` se casan con la cola **por `win_id`**, nunca por posición: el servidor procesa las victorias de campaña en orden de nivel.
- **`AF.Backend`:** el cliente HTTP real de §3.1, con el autorizador de siempre.
- **UI:** el botón del inicio, la pantalla de la Liga, la línea de la victoria y la fila del país en Ajustes. Textos en `strings.csv` en los tres idiomas, con `context` y `max_chars`. Los nombres de animal `league_name_*` nacen `pendiente_asesor`.
- **Banderas:** dibujadas por código como el resto del arte provisional. Siempre con el nombre del país al lado; la bandera nunca va sola.
- **Accesibilidad:** TalkBack lee cada fila (posición, seudónimo, país, puntos). Al 130 % todo cabe en los tres idiomas.
- **Sin perfil de servidor:** la cola vive en el perfil (`league.pending[]`, 08 §4.3). Lo que se descarga es caché, nunca la fuente de verdad.

## 5. Remote Config y analítica (08 §9 y §8)

**Claves nuevas en 08 §9** (las regenera `tools/remote-config/rc_defaults.py`):
- `live_league_enabled`: `false`;
- `live_league_unlock_level`: `41`;
- `live_league_cache_minutes`: `10` [P].

**Eventos nuevos en 08 §8.2:**
- `af_league_open` (`scope`);
- `af_league_wins_sent` (`count`, `accepted`, `rejected`);
- `af_league_unavailable` (`reason`).

Nunca llevan seudónimo ni país, solo claves (regla de `localizacion.md`).

## 6. Privacidad (Codex)

La política, en tres idiomas, tiene que decir:
- que la Liga muestra a otros jugadores un **seudónimo automático, la bandera del país y los puntos de la semana**;
- que el país sale de la conexión o de la región del teléfono;
- que se borra con la cuenta;
- que con <13 no hay Liga.

**Seguridad de los datos:** «Actividad en la app: otras acciones» (puntos de la Liga) y el país aproximado. Todo va con `[[ABOGADO: revisar]]`.
