# Prueba de Firebase, el backend real y los idiomas en el teléfono (2026-10-23)

El portátil compiló `integracion-fase1-fase2` en `509f6d24` con `Assets/_LocalSecrets/google-services.json`, instaló el `dev.apk` en el SM-S938B por depuración inalámbrica y probó:
- lo nuevo: Firebase, el backend real, Play Billing, el inglés y el portugués, y la fuente al 130 %;
- las observaciones 1-7 de `pruebas-telefono-2026-10-22.md`.

**Resultado:**
- **Firebase arranca sin errores:** Crashlytics, Analytics con el consentimiento y Auth anónimo.
- **Backend real:** el backend real responde al `restoreLedger` del arranque.
- **App Check:** la primera petición con integridad ya está hecha (403, a la espera del token de depuración).
- **Idiomas:** el juego ofrece English y Português y sale traducido, con algunos textos vacíos (observación 1).
- **Observaciones de la hoja del 22:** las 1-5 y la 7 están resueltas; la 6 sigue.
- **Mercado:** no se ve porque la app no está en Play Console, que es lo esperado (sin probar).
- **Eliminar cuenta con integridad:** queda pendiente hasta que se registre el token.

Capturas y logcat en el portátil: `Logs/device/fb/` (fuera de git). El lector de logcat descarta toda línea con «debug secret» y no se leyó la etiqueta `DebugAppCheckProvider`.

## Verificación (portátil ARM64)

| Comando | Resultado |
|---|---|
| `git check-ignore -v Assets/_LocalSecrets/google-services.json` | Ignorado por `.gitignore:33` (`/[Aa]ssets/_LocalSecrets/`). La existencia se comprobó con `git status --ignored`: `Test-Path` lo bloquean los permisos de la sesión. El archivo no se abrió. Al principio se llamaba `google-service.json` y lo renombró el dueño |
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 errores |
| `dotnet test dotnet/AmazonFill.slnx` | 996 superados, 6 omitidos (1.002) |
| `python tools/validate_schema.py` | 116/116 PASS |
| `pwsh -NoProfile -File scripts/unity-build.ps1 -Method Dev` | Código 0, `[AF.BuildDev] OK`: `dev.apk` de 73,45 MB (77,0 MB decimales) en 801 s. El log muestra los recursos de Firebase generados (`FirebaseApp.androidlib`) |
| `pwsh -NoProfile -File ci/check-manifest.ps1 Builds/Android/dev.apk -Mode dev` | OK: targetSdk 36, minSdk 26, arm64-v8a, Play Billing 9.0.0; permisos `INTERNET`, `ACCESS_NETWORK_STATE`, `VIBRATE`, `BILLING`, `WAKE_LOCK`, `BIND_GET_INSTALL_REFERRER_SERVICE` y `POST_NOTIFICATIONS` |
| `adb install -r Builds/Android/dev.apk` (inalámbrico) | `Success` |

Tras el build queda sin seguimiento `Assets/StreamingAssets.meta`, que generó Unity y no se ha commiteado (la carpeta está ignorada, pero su `.meta` no).

## Resultados

- **Firebase: cumple.**
  - Inicialización: `FirebaseApp initialization successful`, Crashlytics 20.1.1 con la captura nativa y Analytics con `SetConsent`.
  - Auth: `Using Auth Prod for testing`.
  - No hay errores de Firebase ni de Remote Config en logcat.
- **Backend real: cumple.** `ledger_restore` llega a los ~4-7 s del arranque. Esa sonda solo se escribe cuando `restoreLedger` corre de verdad contra `BackendEndpoints.Dev`, con el ID token de Auth anónimo.
- **App Check: pendiente del dueño.**
  - Al arrancar, el proveedor de depuración pidió su primer token y recibió `403 App attestation failed`; después, `Too many attempts`. Es lo esperado sin el token registrado.
  - El secreto ya está en logcat para registrarlo (`firebase-cliente.md` §4).
  - Abrir «Eliminar cuenta y datos» y cancelar no lanza ninguna petición (`af_account_delete` `requested`, sin llamada).
- **Idiomas (observación 1 del 22): cumple, con la observación 1.**
  - Ajustes › Idioma ofrece Automático, Español, English, Português y en-XA. Con el teléfono en en-US, Automático elige inglés (`loc` `source: device`).
  - Con `af_lang en` y `pt-BR` salen traducidos el inicio («Play · Level 41», «Jogar · Nível 41»), la Bitácora («River Logbook», «Diário do Rio»), el Reto, Ajustes, el Mapa («Samples», «Mostruário», «Basin», «Bacia») y la Pororoca, con la tarjeta traducida y la ranura «Mix»/«Misturar».
- **Fuente al 130 %: cumple a medias** (observación 2). El inicio, la Bitácora, el Mapa y la Pororoca caben en los tres idiomas, salvo las placas de la vista Cuenca. El código de cuenta de Ajustes sale en grupos con guiones (`XXXX-XXXX-…`).
- **Observaciones del 22:**
  - 2, el aviso del Reto tras el 7: **resuelta**, el aviso va bajo TiTino sin taparse;
  - 3, el tooltip del 6: **resuelta**, va entre los dos botones con el pico sobre «Volver a la orilla (gratis)»;
  - 4, el tooltip del 11: **resuelta**, ya no tapa la línea «Cosecha +20»;
  - 5, `AF_EVENT` sin consentimiento: **resuelta en el código**, sin consentimiento la línea pasa a `AF_EVENT_UNSENT` y no llega a ningún sumidero que envíe fuera del teléfono (`LogcatAnalyticsSink`). En el teléfono no se volvió a apagar la analítica para verlo;
  - 6, «Reducir movimiento»: **sigue** siendo un interruptor de sí o no;
  - 7, el build: **resuelta**, `unity-build.ps1` da veredicto (código 0) y el log ya no avisa de `unity pipeline upgrade`.
- **Play Billing: «no disponible», lo esperado.** Con el backend real y la app fuera de Play Console, la tienda no está disponible y el inicio oculta el icono del Mercado y el Cántaro (`MarketAvailable` es falso). En logcat sale un aviso de Unity IAP: «IStoreService.Connect called without a callback defined for IStoreService.OnStoreConnected».

## Observaciones

1. **Textos vacíos en inglés y portugués.** En español salen, pero en `en` y `pt-BR` quedan en blanco:
   - la línea del inicio «Faltan N niveles para {parada}»;
   - en el Mapa, la pregunta del título («¿Por qué el río Negro es negro?»), «Premio del mundo · faltan N» y el botón «Faltan N niveles para {parada}».

   La regla manda dejar vacía una traducción que falta, así que faltan esas claves (y probablemente las preguntas de las paradas en `facts.csv`).
2. **Vista Cuenca del Mapa al 130 % en inglés:** las placas de los mundos se montan unas sobre otras («World 9 · Ucayali» tapa a otra y «World 3» queda cortada). Al 100 % en español se ven bien.
3. **Aviso de Unity IAP** al conectar la tienda: falta el callback de `OnStoreConnected` (aviso, no error).

## Sin probar

- **Eliminar cuenta y datos con integridad:** probado después y con fallo; ver el apartado siguiente.
- **El Mercado al 130 % en los tres idiomas** (pestañas en dos líneas, filas que crecen y desplazamiento): no se ve sin la tienda, y no hay extra de desarrollo para forzar la falsa.
- **Remote Config aplicado:** no hay sonda que diga si la descarga terminó; se aplica en el arranque siguiente.
- **Lo que cambia ajustes del sistema:**
  - la pantalla 16:9;
  - la fuente de Android;
  - TalkBack;
  - el modo avión;
  - la hora.

## Eliminar cuenta con integridad (2026-10-22, 13:38-13:39 PDT = 20:38-20:39 UTC)

El dueño ya había registrado el token de depuración de App Check, y Codex había cargado el secreto `af-dev/firebase-admin-service-account` más de 5 minutos antes. Se limpió logcat (`adb logcat -c`) y el dueño hizo en el teléfono Ajustes › «Eliminar cuenta y datos» › Continuar › Eliminar dos veces: la segunda tras reabrir la app. No se recompiló: es el mismo `dev.apk` de `509f6d24`.

**Logcat**, sin secretos, ID tokens ni UID (filtradas las líneas de `DebugAppCheckProvider`):

```
13:38:37.234 AF_STATE {"event":"af_account_delete","t":170571,"step":"confirmed","source":"app"}
13:38:43.429 [AF.GameBootstrap] deleteAccount: BackendError(Http 500 internal_error)
13:38:43.429 [AF.GameBootstrap] el servidor no confirmó el borrado de la cuenta (deleteAccount): no se borra nada
13:38:43.429 AF_STATE {"event":"af_account_delete","t":176765,"step":"failed","source":"app"}
13:39:08.619 AF_STATE {"event":"af_account_delete","t":7184,"step":"requested","source":"app"}
13:39:10.412 AF_STATE {"event":"af_account_delete","t":8976,"step":"confirmed","source":"app"}
13:39:15.691 [AF.GameBootstrap] deleteAccount: BackendError(Http 500 internal_error)
13:39:15.691 [AF.GameBootstrap] el servidor no confirmó el borrado de la cuenta (deleteAccount): no se borra nada
13:39:15.691 AF_STATE {"event":"af_account_delete","t":14256,"step":"failed","source":"app"}
```

No sale «Failed to exchange debug token» ni ningún error de App Check o de Firebase Auth en el cliente. El cliente no borra nada local cuando falla, como debe.

**Código:** **HTTP 500 `internal_error`** en los dos intentos, unos 5-6 s después de confirmar (`Call.RetryOrFail` agota los reintentos).

**Qué lo produce en el servidor** (`infra/functions/delete-account/handler.ts` y `infra/functions/shared/`). `withErrors` (`shared/http.ts`) solo devuelve 500 `internal_error` en dos casos:
- **`ConfigError`**, con la línea de log `config_error` y su `detail`:
  - `googleAuth.ts` `parseServiceAccount`: el secreto no es JSON («¿sigue el relleno inicial?»), o no tiene `type: service_account`, un `client_email` que acabe en `.iam.gserviceaccount.com` o un `private_key` que contenga «PRIVATE KEY»;
  - `importPKCS8`: la clave privada no es PKCS#8 RSA válida, por ejemplo con los `
` escapados dos veces al cargarla con `put-secret-value`;
  - `secrets.ts`: el secreto no tiene valor de texto;
  - `env.ts` `requireEnv`: falta una variable (`AF_TABLE_*`, `AF_FIREBASE_PROJECT_ID`, `AF_SECRET_FIREBASE_ADMIN`).
- **Un error no tipado**, con la línea `internal_error` y `err` y `detail`: lo más probable, de DynamoDB (`AccessDeniedException` del rol, `ResourceNotFoundException` o una `ValidationException` porque falta el índice `by_uid`) en `markDeleted`, `sweep` o el borrado de `account_codes`; o un fallo de `jose` al firmar.

**Lo que queda descartado:**
- la integridad: un fallo daría 403 `integrity_failed`, y aquí `requireIntegrity` pasó, porque va antes de `eraseAccount`;
- Identity Toolkit `accounts:delete`, el canje OAuth en Google y Secrets Manager: un fallo de estos da 502 `upstream_error`;
- la caché de 5 minutos del secreto (`secrets.ts`): ya había caducado, según codex-estado.md.

**Causa probable:** un `ConfigError` del secreto de la cuenta de servicio, cargado pero con una forma que `parseServiceAccount` o `importPKCS8` rechazan (el caso típico es el `private_key` con los saltos de línea mal escapados). Le sigue un permiso o un índice de DynamoDB. Solo el log de la Lambda lo distingue.

**Lo que necesita Codex** (no hay credenciales de AWS en el portátil). En CloudWatch Logs Insights, región `sa-east-1`:
- grupo de logs: `/aws/lambda/af-dev-delete-account` (la Lambda `af-dev-delete-account` de `AfDevApi`);
- ventana: **2026-10-22 20:37:30 a 20:40:30 UTC**;
- consulta:

```
fields @timestamp, @requestId, level, msg, operation, code, detail, err, service, upstream_status
| filter level = "ERROR" or msg in ["config_error", "internal_error", "upstream_error", "http_error", "rejected", "account_deleted"]
| sort @timestamp asc
| limit 50
```

Si sale vacío, hay que ampliar a `fields @timestamp, @message | sort @timestamp asc | limit 100` en la misma ventana, para ver los `START`/`END`/`REPORT` y un posible tiempo agotado. El campo `detail` de `config_error` dice cuál de los casos de arriba es, sin enseñar el secreto.

Según el resultado:
- **`config_error` del secreto:** volver a cargar `af-dev/firebase-admin-service-account` con el JSON de la cuenta de servicio tal cual lo descarga Google (`put-secret-value --secret-string file://clave.json`, sin reescapar el `private_key`), esperar 5 minutos o forzar un arranque en frío, y repetir la prueba;
- **`internal_error` de DynamoDB:** revisar el rol de `af-dev-delete-account` y los índices `by_uid` de `account_codes`, `purchases` y `rewards` en `AfDevData`;
- **ninguna línea:** el 500 sale antes de la Lambda (API Gateway o autorizador). Hay que mirar `/aws/apigateway/af-dev-api` en la misma ventana, con el `status` y el `integrationErrorMessage` de la ruta `POST /v1/account/delete`.

**Incidente con el secreto de App Check en el portátil.** Al leer la tercera consulta (`BackendError|AppCheck|FirebaseAuth`), el filtro por «debug token» y «debug secret» no tapó una línea de `DebugAppCheckProvider`: el comando de ejemplo `firebase appcheck:debugtokens:create`, que lleva el secreto. La línea quedó en la salida de una herramienta de la sesión de Claude Code. No se ha copiado en ningún archivo, documento ni commit, y no se repite aquí. **Recomendación:** revocar ese token en la consola (App Check › app `com.amazonfill.dev` › Manage debug tokens) y registrar uno nuevo: borrar los datos de la app (`pm clear`), abrirla y leerlo con el comando de `firebase-cliente.md` §4. Desde entonces el portátil descarta además toda línea de `DebugAppCheckProvider` y las que contienen `appcheck:debugtokens`.

## Eliminar cuenta con integridad: resuelto (2026-10-22, 20:05 hora de Colombia)

- **Causa del 500:** el secreto `af-dev/firebase-admin-service-account` contenía un JSON que no era una clave de cuenta de servicio (676 bytes, sin `service_account` ni clave privada). Codex cargó la clave correcta: forma `[2372, true, true, true]`, una sola clave activa y las anteriores revocadas (`codex-estado.md`).
- **Resultado:** en el teléfono, el borrado funcionó y el juego volvió al primer arranque, según el dueño. En CloudWatch, Codex confirmó `account_deleted` a las 01:05:37 UTC.
- **Observación nueva:** mientras borra, el diálogo no muestra nada («tosco, sin mostrar si carga o procesa»). Se arregla en el carril `tanda5/borrado-espera`.
- **Pendiente del dueño:** revocar el token de depuración de App Check que salió en el chat y registrar uno nuevo.

