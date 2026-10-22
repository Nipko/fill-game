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

- **Eliminar cuenta y datos con integridad:** queda para después de que el dueño registre el token de depuración de App Check.
- **El Mercado al 130 % en los tres idiomas** (pestañas en dos líneas, filas que crecen y desplazamiento): no se ve sin la tienda, y no hay extra de desarrollo para forzar la falsa.
- **Remote Config aplicado:** no hay sonda que diga si la descarga terminó; se aplica en el arranque siguiente.
- **Lo que cambia ajustes del sistema:**
  - la pantalla 16:9;
  - la fuente de Android;
  - TalkBack;
  - el modo avión;
  - la hora.
