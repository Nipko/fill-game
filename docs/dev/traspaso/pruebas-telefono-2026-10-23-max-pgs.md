# Prueba de MAX, Play Games y el icono v2 en el teléfono (2026-10-23)

El portátil compiló `integracion-fase1-fase2` en `a656cca4` con `Assets/_LocalSecrets/google-services.json`. Instaló el `dev.apk` en el SM-S938B por depuración inalámbrica, hizo `pm clear` y siguió `guion-prueba-telefono-max-pgs-icono.md`. No se hizo el borrado de cuenta: solo se abrió el paso 2 para ver la nota y se canceló.

**Resultado:**
- cumplen los puntos 1-5 del guion:
  - el icono v2;
  - MAX sin clave, que no arranca y deja los anuncios simulados;
  - Play Games sin `APP_ID`, que no arranca;
  - la nota del borrado en los tres idiomas;
  - la regresión 1-12, el pre-aviso de notificaciones y la Bitácora;
- hay 3 observaciones; la principal: **el Mercado no está oculto** (observación 1);
- quedan sin probar el icono temático y el cajón de apps (observación 3).

Capturas y logcat en el portátil: `Logs/device/max-pgs/` (fuera de git). El logcat completo se guardó filtrando las líneas de `DebugAppCheckProvider`, «debug secret», «debug token», `appcheck:debugtokens` y `eyJ`; se comprobó al final que no queda ninguna.

## Verificación (portátil ARM64)

| Comando | Resultado |
|---|---|
| `git pull origin integracion-fase1-fase2` | `a656cca4` («Integra el icono v2 en Android: adaptativo con monocromo (D-045)») |
| `pwsh -NoProfile -File scripts/unity-build.ps1 -Method Dev` | Código 0, `[AF.BuildDev] OK`: `dev.apk` de 69,27 MiB (72,64 MB, como los 72,4 MB del escritorio) en 843 s. El build reescribe `ProjectSettings/GooglePlayGameSettings.txt` solo con finales de línea; se descartó |
| `pwsh -NoProfile -File ci/check-manifest.ps1 Builds/Android/dev.apk -Mode dev` | OK: todos los manifiestos cumplen la compuerta (Play Billing 9.0.0) |
| `adb install -r Builds/Android/dev.apk` (inalámbrico) y `adb shell pm clear com.amazonfill.dev` | `Success` y `Success` |

## Resultados

- **1. Icono v2 (D-045): cumple en lo probado.**
  - `aapt2 dump badging`: `application-label` «Tintalú (dev)» e `application-icon` `res/mipmap-anydpi-v33/af_launcher.xml`. Su XML es un `adaptive-icon` con `background`, `foreground` y `monochrome`. En Android 16, `dumpsys package` ya no imprime el icono.
  - En «Información de la app» (`icono-info.png`) sale el icono con la máscara squircle de One UI, sin bordes blancos: la cuia vierte açaí sobre el río café con leche, con el tramo violeta a la derecha, sobre fondo petróleo.
- **2. MAX sin clave (D-042): cumple.**
  - Primer arranque tras `pm clear`, antes de contestar la edad: 0 líneas `applovin` y `[AF.Ads] proveedor: not_configured`.
  - Con 18+ y «Aceptar todo» no hay AppLovin ni formulario de UMP, y el juego entra al nivel 1 con el dedo guía.
  - En el 11, el vídeo simulado de la Cosecha doble se ve y se cobra (+20, `double_harvest`), con `af_ad_request` `filled`, `af_ad_shown` y `af_ad_reward` en `AF_EVENT`.
  - Con 13-17 (otro `pm clear`): `consent` con `npa: true`, sin interruptor de anuncios, 0 `applovin`, 0 `E/Unity` y nivel 1.
  - Con <13 (otro `pm clear`): «Ask an adult to install the game», bloqueado también al reabrir, con 0 `applovin` y 0 líneas de Play Games.
- **3. Play Games sin `APP_ID` (D-042): cumple.**
  - No aparece el cartel de Play Juegos.
  - En nuestro proceso hay 0 líneas `PlayGamesSdk`, `GamesSignIn` o `games.provider`. Las 370 líneas de `PlayGamesServices` del logcat son de `com.google.android.gms` y salen a las 21:12:20, al instalar, antes de abrir la app.
  - El progreso se guarda en el teléfono: tras ganar el 1-12 y `am force-stop`, la app vuelve al inicio en el nivel 13 con el saldo.
  - En Ajustes no hay nada de Play Juegos.
- **4. Nota del borrado: cumple.**
  - Ajustes › «Eliminar cuenta y datos» › Continuar muestra el paso 2 con la nota: en es, «Pedimos borrar tu copia en Google Play Juegos. Si falla, se reintenta al abrir el juego con Play Juegos. También puedes borrarla allí.», y la misma idea en en y pt-BR.
  - Al 130 % cabe entera en los tres idiomas, sin tapar el botón (`borrado-nota-es-419.png`, `-en.png` y `-pt-BR.png`).
  - Se cerró con la X: solo hay `af_account_delete` `requested`, ningún `confirmed`.
- **5. Regresión rápida: cumple, salvo el Mercado (observación 1).**
  - 1-12 ganados con las sondas y 0 `E/Unity` en todo el logcat.
  - Pre-aviso de notificaciones: en la primera victoria de la sesión 2 con el 10 superado, al pulsar «Siguiente» sale «Shall we let you know when your River Challenge is ready and your Logbook is waiting?». «Not now» lo cierra (`af_push_prompt` `pre` `declined` `first`) y sigue al 12.
  - La Bitácora sale sola en el inicio y lleva al Mapa con la secuencia del 10.
  - El Cántaro está oculto.
  - Tamaño: `dev.apk` de 72,64 MB.
- **Observaciones de la hoja del 23 (de paso):** en inglés ya salen la pregunta del título del Mapa («Tapajós · Why is the Tapajós water clear?») y «8 levels left to Alter do Chão». Al 100 %, las placas de la vista Cuenca no se montan.

## Observaciones

1. **El Mercado no está oculto**, aunque el guion lo espera oculto, como el 23.
   - El inicio muestra su icono (`lobby_market ['market_tab']`) y el Mercado se abre con los precios de respaldo del catálogo (COP 3,900…).
   - Al tocar una fila: `purchase_sim` con `status: StoreUnavailable` y `af_iap_result` `failed`. No se abre la hoja de Google Play, **pero tampoco sale ningún mensaje**.
   - Por el código (`BillingReadyStoreService`), el Mercado se enseña cuando Play Billing dice que está listo y un SKU sin precio de Play no se vende. Así que Play conecta, pero sin los productos.
   - Para decidir: ocultar el Mercado mientras ningún SKU tenga precio de Play, o decir «no disponible» al tocar.
2. **El build reescribe `ProjectSettings/GooglePlayGameSettings.txt`** con finales CRLF en Windows. El contenido no cambia, pero queda como modificado tras cada build.
3. **Sin probar en el icono:**
   - el icono en la pantalla de inicio y en el cajón de apps: no se capturó para no recorrer ni fotografiar las apps personales del teléfono; las dos capturas que se tomaron se borraron;
   - el icono temático (monocromo): exige cambiar la paleta del sistema;
   - Recientes y el diálogo de desinstalar.

   Los mira el dueño a simple vista; el monocromo existe en el recurso.

## Sin probar

- El icono en el inicio, el cajón, Recientes, el diálogo de desinstalar y el tema monocromo (observación 3).
- La «fuente del sistema al 130 %»: se usó `af_font_scale 130`, sin cambiar los ajustes de Android.
- Los anuncios simulados con la banda 13-17: se comprobó `npa: true`, sin ver un vídeo.
