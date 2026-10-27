# Liga del río: implementación y operación

23-10-2026 · D-046; avatar y anaconda el 27-10-2026 (D-056, D-057). Fuente única de comportamiento y API: `docs/dev/liga.md`. Textos legales en `codex/web-marca`.

## Datos y consistencia

- `league_players`: PK `uid`, SK `week`; puntos, país, victorias/fechas deduplicadas, `flagged`, instante de alcanzar los puntos y revisión CAS. TTL = fin de semana + 14 días (entre dos y tres semanas después de la victoria).
- `league_board`: PK `week#global` o `week#ISO`, SK puntos invertidos + instante + UID para desempatar solo si coinciden los milisegundos. Consulta fuerte ordenada, límite 100. El UID interno nunca se serializa en las filas públicas. Las cuentas marcadas no tienen filas públicas.
- `league_histogram`: PK del ámbito/semana, SK tramo de 100 puntos [P]; contadores de jugadores visibles. Consulta paginada por partición, sin Scan. Percentil aproximado por población superior al tramo más media población del propio tramo, redondeado hacia arriba, 1–100. Los empates exactos solo se resuelven en el top; el histograma es aproximado.
- `league_names`: PK UID para el seudónimo y PK `name#animal#cifras` para reservar la pareja. Alta de las dos filas en la misma transacción condicional. Las 24 claves siguen el orden del contrato, pero los seudónimos nuevos se sortean entre 23: sin `league_name_anaconda` (D-057, Q-AV-12; `ASSIGNABLE_NAME_KEYS` en `rules.ts`). Quien ya la tiene la conserva y sigue en el `enum`. Cifras 1000–9999 (207.000 parejas asignables).
- La fila del seudónimo guarda también el **avatar** (D-056; `avatar`, 0-63) y su cupo del día (`avatar_day`, fecha UTC, y `avatar_changes`), sin TTL: se borran con el seudónimo. Las filas de `league_board` copian `avatar` como copian `name_key` y `name_num`; sin elegir, no llevan el atributo y las respuestas lo omiten. Ante colisiones, reintento aleatorio acotado; al agotarse devuelve 503 y no reutiliza un nombre activo. Para crecer por encima de ese espacio hará falta una decisión del contrato, no ocultar el límite.

Cada lote recalcula el estado desde lecturas fuertes y escribe con revisión CAS al jugador y al avance histórico del UID. En la **misma transacción** quita/añade sus filas de top, mueve los contadores del histograma y comprueba la marca de cuenta borrada. Reintenta hasta ocho conflictos con lectura fresca; nunca responde éxito si la escritura no terminó. La marca de borrado impide la recreación con un ID token todavía válido. `deleteAccount` consulta semanas por la PK del UID (sin GSI), elimina filas/seudónimo/reserva y descuenta contadores de forma idempotente.

**Avatar (`POST /v1/league/avatar`, función `league-avatar`).** Valida `{ "avatar": 0-63 }` (solo el rango; el catálogo es del cliente), comprueba la marca de cuenta borrada y la integridad como las otras dos rutas, y crea el seudónimo si falta. Con el mismo avatar responde 200 sin escribir. Si no, en una transacción: `Put` de la fila del seudónimo con su revisión CAS, `ConditionCheck` de la revisión del jugador de la semana y `Put` de sus filas del top (global y país) con el avatar nuevo, más la marca de cuenta no borrada. Toda victoria aceptada sube la revisión del seudónimo, así que una victoria concurrente cancela la transacción y se reintenta con lectura fresca (hasta ocho veces; después, 503). Las cuentas marcadas no tienen filas públicas y no las ganan al cambiar de avatar. Cupo: 30 cambios por UID y día UTC [P], contados en la misma fila; el 31 recibe 429 con `retry_after_s` hasta la medianoche UTC. Permisos: `GetItem`/`PutItem` en `league_names`, `GetItem`/`ConditionCheckItem` en `league_players` y solo `PutItem` en `league_board` (sin histograma: la posición no cambia). Las filas de semanas anteriores conservan el avatar que tenían; no se sirven.

Semanas antiguas no se consultan desde la API; TTL no supone borrado instantáneo del almacenamiento. Un histograma antiguo eliminado por TTL puede recibir un contador negativo al limpiar una fila más lenta en caducar: su TTL sigue vencido, nunca se sirve como semana actual y no identifica jugadores.

## Integridad y decisiones aplicadas

- JWT de Firebase en API Gateway y `requireIntegrity` común (App Check y caché vigente) en **ambos** endpoints; sin identidad enviada en el cuerpo. En dev sigue el verificador de desarrollo existente, solo habilitado con la bandera de dev; prod no lo admite.
- País: respaldo `region` de §3.3, contra la lista ISO 3166-1 alpha-2; sin una distribución nueva. Se ignora cualquier `CloudFront-Viewer-Country` del endpoint directo, que un cliente podría falsificar. ISO desconocido → `null`, sin pestaña País. Es país declarado por el teléfono, no prueba de residencia ni detección de VPN.
- Revisión del 24-10-2026 (§3.2): la referencia avanza con toda campaña bien formada, incluso rechazada; se guarda atómicamente junto al jugador. Un hueco repone la referencia y cuenta, incluido el Salto del Boto: el avance lo declara el cliente, sin prueba de juego. `unreachable_level` se limita a `level > max_level`; niveles ausentes del catálogo o anteriores al desbloqueo → `invalid`. Esta revisión sustituye el criterio de +1/+2 aprobado el día anterior.
- Solo el sobre (array de 1–50, max_level entero no negativo y campos conocidos) puede invalidar la petición completa. Cada elemento mal formado devuelve su propio `invalid`; si carece de win_id de texto se devuelve `win_id: ""`, compatible con la respuesta existente. La región inválida, vacía o null significa país desconocido. Sin tope fijo de 700 en el esquema; se consulta el catálogo generado para puntuar. Las campañas se ordenan por nivel entre sus posiciones en el lote; los Retos conservan su orden/posición.
- Límite 40/h: ventana móvil por `won_at` validado (incluye el final de la semana anterior aunque el lote de la primera hora del lunes se reciba días después), soporta lotes offline y desordenados. Se admiten hasta cinco minutos de futuro, inclusivos, sin ampliar los límites de la semana; las demás fechas fuera de semana se rechazan. Un ID semántico diferente (`c:n`, `d:fecha`) o una fecha de Reto que no existe en el calendario → `invalid`. El Reto conserva su fecha local de inicio (06 §4.1, R-Q3): no se exige que coincida con la fecha UTC de la victoria. Sin offset en el contrato, se admiten los días que se pueden solapar con la semana y el día local previo por huso/cruce de medianoche; días remotos anteriores o posteriores al día de victoria +1 se rechazan como `outside_week`.
- Los IDs diarios del borde semanal se contrastan también con la semana anterior para no premiar dos veces el mismo Reto al cruzar el lunes; los niveles de campaña sí vuelven a puntuar cada semana.
- Ningún rechazo marca automáticamente la cuenta. `flagged` se conserva únicamente para operaciones manuales: una cuenta ya marcada sigue sumando para sí sin exposición en top/histograma. No se borran marcas preexistentes de forma automática; este cambio no permite distinguir su origen histórico.
- La fecha, avance inicial y región siguen siendo declaraciones del cliente. App Check no certifica que el nivel se resolvió: no presentar estas reglas como prueba de juego ni antifraude completo. La edad y el desbloqueo visual corresponden al cliente del contrato, que no inicia red para <13.
- La tabla de 700 dificultades se genera con `tools/league-levels.ts`; incluye huellas de los packs normalizadas a LF. `--check` se ejecuta también antes de Vitest.

## Coste incremental estimado

Estimación de planificación, USD/mes, región São Paulo, consultada el 23-10-2026. **MAU no basta para fijar el coste**: se usa por jugador/mes 100 victorias agrupadas en 20 envíos y 60 lecturas de clasificación (80 peticiones), Lambda media 150 ms a 256 MB, top de unos 20 kB. Sin créditos, descuentos ni franquicias gratuitas; impuestos, Firebase y backend previo quedan fuera.

Presupuesto conservador por lote: 26 WRU transaccionales (fila semanal media de hasta 4 kB, seudónimo, dos ámbitos y cambios de tramo) y 10 RRU; por consulta, 25 RRU para top y, si hace falta, histograma. Las transacciones cuestan el doble por unidad; lotes grandes y colisiones pueden superar estas hipótesis. Con hasta 707 victorias semanales la fila puede ser mayor: vigilar su tamaño y recalcular con tráfico real.

| Componente | 1.000 jugadores | 100.000 jugadores |
|---|---:|---:|
| Escrituras DynamoDB (aprox. 0,52 M / 52 M WRU) | 0,49 | 48,75 |
| Lecturas DynamoDB (1,7 M / 170 M RRU) | 0,32 | 31,88 |
| HTTP API (80.000 / 8 M peticiones) | 0,13 | 12,72 |
| Lambda: peticiones y cómputo arm64 | 0,06 | 5,60 |
| Salida de datos (1,2 / 120 GB, supuesto 0,25 USD/GB) | 0,30 | 30,00 |
| Logs (supuesto 1 kB/petición a 0,76 USD/GB) | 0,06 | 6,08 |
| Almacenamiento y PITR (supuesto 8 kB/jugador) | 0,01 | 0,54 |
| Alarmas adicionales (12 métricas facturadas) | 1,62 | 1,62 |
| **Total orientativo** | **2,99** | **137,19** |

Reservar **5 USD / 200 USD** respectivamente para esta carga estimada y observar el coste real. No son límites de gasto. Los escenarios suponen promedio mensual, no avalan una ráfaga de 100.000 usuarios simultáneos: la partición global y los contadores de tramo pueden sufrir contención y requieren prueba de carga antes de escalar.

Tarifas verificadas en la lista pública regional de AWS: DynamoDB Standard WRU 0,9375 USD/M, RRU 0,1875 USD/M, almacenamiento 0,375 USD/GB-mes y PITR 0,30 USD/GB-mes; HTTP API 1,59 USD/M; Lambda arm64 0,0000133334 USD/GB-s; alarma estándar 0,135 USD/métrica-mes. Peticiones Lambda 0,20 USD/M y las hipótesis de salida/logs deben contrastarse con la factura/calculadora antes de presupuestar un lanzamiento. El avatar añade, por cambio, una transacción de hasta 4 elementos pequeños (unas 8 WRU) y 3 lecturas fuertes; con unos 2 cambios por jugador y mes son unas 16 WRU, despreciables frente a las 520 de las victorias de la tabla. El coste de la reserva de nombres inicial es marginal y queda cubierto por el redondeo/margen, no supone que el alta sea gratuita.

Fuentes: [DynamoDB](https://aws.amazon.com/dynamodb/pricing/), [Lambda](https://aws.amazon.com/lambda/pricing/), [HTTP API](https://aws.amazon.com/api-gateway/pricing/), [CloudWatch](https://aws.amazon.com/cloudwatch/pricing/), [unidades y transacciones](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/read-write-operations.html). Tarifas regionales reproducibles: `https://pricing.us-east-1.amazonaws.com/offers/v1.0/aws/{AmazonDynamoDB|AWSLambda|AmazonApiGateway|AmazonCloudWatch}/current/sa-east-1/index.json`, términos OnDemand con beginRange 0.

**Sin capacidad de servidor fija:** no hay NAT, EC2, ElastiCache, reservas de concurrencia aprovisionada ni otra suscripción de ejecución. Las alarmas que pide el encargo sí facturan mientras existen, incluso sin jugadores: cuatro de Lambda y cuatro expresiones de DynamoDB con dos métricas cada una, total 12 × 0,135 = 1,62 USD/mes sin franquicia. No prometer coste cero en reposo.

## Puesta en dev

Preservar contexto e identidad de `af-dev`; revisar Data/Api/Ops, especialmente IAM. Obtener el sí del dueño sobre el diff **antes** de `cdk deploy`; usar el ensamblado revisado. Web/legal va por la otra rama, sin publicar borradores con marcadores. La base `ApiUrl` existente se mantiene; el integrador configura el cliente y el interruptor. Pruebas: 401 sin token en los tres endpoints; si el dueño facilita un token mediante un archivo privado fuera del repo, un envío y lectura Global/País, sin imprimir ni guardar el token en logs o Git.
