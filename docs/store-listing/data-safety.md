# Seguridad de los datos (Data Safety) de Google Play · BORRADOR

- **Estado:** borrador para el formulario «Seguridad de los datos» de Play Console (08 §13.1 #12). Lo escribió el bloque B13 del Spark (2026-10-20); puesto al día el 2026-10-23 con lo que hoy es real: Firebase y el backend en AWS (D-035, D-039), el plugin propio de Play Billing en lugar de Unity IAP (D-040 y su añadido de Q-IAP-1), Parallext, LLC como responsable (D-041), la banda `<13` sin SDK (D-033, añadido del 2026-10-23) y MAX con UMP y Play Games, que se están integrando (D-042). Lo revisa el abogado (`[PENDIENTE: abogado]`) y lo envía el dueño.
- **Fuentes, en este orden:** 08 §13.3 (una fila por SDK, «[E, a confirmar con la guía de cada SDK]»), que manda; la política de privacidad pública de la web (`infra/web/src/content/es-419/privacy.html`, fuente de verdad de los textos legales); `docs/dev/firebase-cliente.md` (§1, §4 bis y §5); `docs/dev/iap.md` (§1.3 y §4.1, Q-BILL-2); 08 §2.3, §10 y §11; 05 §7.
- **Dos columnas de tiempo:** «**hoy**» es lo que recoge el build de `integracion-fase1-fase2` a 2026-10-23; «**al integrar D-042**» es lo que se añade con AppLovin MAX (con UMP y sus adaptadores) y Play Games 2.3.0. Hasta que D-042 se integre, lo segundo **todavía no está en el juego** y no se declara.
- **Nombres de categorías y tipos:** los del formulario de Play tal como los conoce este borrador [NV]; el dueño los contrasta con el formulario vigente. Las filas [E] se cierran con la guía de Data Safety de cada SDK en su versión fijada.
- **Ningún formulario se envía con el build de hoy:** no hay app en Play Console hasta el dictamen de marca (D-034; `CLAUDE.md`, regla 6). Este documento prepara la 1.0.

## 1. Qué lleva hoy el build (2026-10-23)

| Componente | Estado hoy | Evidencia |
|---|---|---|
| Firebase 13.17.0: Auth anónimo, App Check (Play Integrity en release), Analytics, Crashlytics y Remote Config | **Integrado** (D-039). Sin Functions, Firestore ni Messaging. Analytics apagada hasta el consentimiento; solo concede `analytics_storage` (nunca `ad_storage`, `ad_user_data` ni `ad_personalization`) | `docs/dev/firebase-cliente.md` §1 y §5 |
| Backend propio en AWS, `sa-east-1` (São Paulo) | **Integrado** (D-035): verificación de compras, restauración del libro mayor, código de borrado y borrado de cuenta | 08 §13.3, fila «Backend propio»; `infra/contracts/v1` |
| Google Play Billing Library 9.0.0, con el plugin propio `AfBilling.androidlib` | **Integrado** (D-040, añadido de Q-IAP-1). **Unity IAP ya no está:** se quitó con sus dependencias de Unity Services y su telemetría de compras a Unity | `docs/dev/iap.md` §1; `ci/check-no-unity-iap.ps1` |
| Unity Mobile Notifications 2.4.3 | Integrado: avisos locales, nada sale del teléfono | 08 §13.3 |
| AppLovin MAX 8.6.6 + UMP + adaptadores (AdMob, Unity Ads, Meta Audience Network, Mintegral) | **Todavía no en el juego** (D-042, en integración) | D-042 |
| Play Games 2.3.0 (inicio de sesión silencioso y Saved Games) | **Todavía no en el juego** (D-042, en integración) | D-042 |

**Permisos del manifiesto fusionado hoy** (lista blanca de `ci/check-manifest.ps1`): `INTERNET` (D-036), `ACCESS_NETWORK_STATE` (D-037), `VIBRATE`, `WAKE_LOCK` y `BIND_GET_INSTALL_REFERRER_SERVICE` (Firebase Analytics, D-039), `POST_NOTIFICATIONS` (D-033, Q-M5) y `com.android.vending.BILLING` (D-040). Sin ubicación. `AD_ID`, `ACCESS_ADSERVICES_AD_ID` y `ACCESS_ADSERVICES_ATTRIBUTION` se quitan del manifiesto (D-039) hasta MAX, y con D-042 cada permiso nuevo se pregunta aparte.

**Banda `<13` guardada** (D-033, añadido del 2026-10-23; `firebase-cliente.md` §4 bis): el arranque no inicia ningún SDK. Ni Crashlytics (se apaga por JNI y se borran los informes sin enviar), ni la descarga de Remote Config, ni Firebase en C#, Auth, App Check, Analytics, el backend, la tienda, las notificaciones ni, con D-042, los anuncios o Play Games. En el **primer arranque**, antes de la pantalla de edad, sí funcionan Crashlytics y la descarga de Remote Config (privacidad de la web, sección 3).

**El país del perfil legal** sale de la SIM o de la región del teléfono, en el dispositivo y sin servidor (D-033, Q-T3).

## 2. Panorama del formulario

| Pregunta del formulario [NV] | Respuesta hoy | Al integrar D-042 | Fuente |
|---|---|---|---|
| ¿La app recoge o comparte alguno de los tipos de datos de usuario obligatorios? | **Sí** (recoge; no comparte) | **Sí** (recoge y **comparte** con las redes de anuncios) | Sección 3 |
| ¿Todos los datos recogidos se encriptan en tránsito? | **Sí**: HTTPS de Firebase, Play Billing y la API de AWS | Sí (HTTPS de los SDK de anuncios y de Play Games) [E] | 08 §13.1 #12; privacidad de la web, sección 12 |
| ¿Ofreces una forma de pedir la eliminación de los datos? | **Sí: en la app** (Ajustes › «Eliminar cuenta y datos», dos confirmaciones) **y en la web** (página de borrado, con el **código de cuenta** de Ajustes) | Igual; el cliente borra además el snapshot `af_main` de Saved Games | 08 §11; §13.1 #7 y #12; `infra/web/src/content/*/delete.html` |
| URL de eliminación de cuenta | `https://tintalu.com/tintalu/eliminar-cuenta/` (Q-B13-1 (a); rutas en `infra/web/src/site.ts`). **El dominio aún no está conectado** (D-041): hoy la web solo se sirve en la URL de CloudFront de dev | — | D-041; 08 §11 |
| ¿La app permite crear una cuenta? | Sí, implícita: Firebase Auth anónimo; sin correo ni contraseña | Además, el vínculo con Play Games | 08 §11; §13.1 #7 |
| ¿Cumple la política de Familias? | No aplica: público 13+, sin programa Families; `<13` bloqueado | Igual | 08 §13.1 #11 |
| Revisión de seguridad independiente (MASA) | No, en la 1.0 [P] | — | — |

## 3. Tipos de datos

**Recogido** = sale del teléfono hacia Parallext, LLC o un SDK. **Compartido** = se transfiere a un tercero que no trata los datos por cuenta de Parallext, LLC; según la definición de Play, lo que trata un proveedor por nuestra cuenta (Google en Firebase, AWS) no cuenta como compartido [NV]. Ningún dato es efímero salvo donde se dice. Con la banda `<13` guardada no se recoge **nada** de esta tabla (sección 1).

### 3.1 Hoy

| Categoría · tipo [NV] | Quién lo recoge | Recogido | Compartido | Finalidad | ¿Opcional? | Notas |
|---|---|---|---|---|---|---|
| Información personal · ID de usuario | Firebase Auth (UID anónimo); backend en AWS; Google Play recibe el UID como `obfuscatedAccountId` | Sí | No | Funcionalidad (compras, restauración, borrado); prevención de fraude y seguridad | No | Sin nombre, correo, teléfono ni dirección. Ajustes enseña el **código de cuenta**, no el UID; el servidor solo guarda el hash del código (08 §11) |
| Información financiera · historial de compras | Google Play Billing (procesa el pago); backend en AWS (libro mayor, con el token de compra solo como hash) | Sí | No | Funcionalidad; prevención de fraude; cumplimiento (fiscal) | Sí (solo quien compra) | Tras el borrado, el libro mayor queda **sin el UID** el plazo fiscal `[PENDIENTE: abogado]` (08 §11). Firebase Analytics puede registrar por su cuenta el evento automático `in_app_purchase` [E: confirmarlo con la guía de Firebase; solo con consentimiento] |
| Actividad en la app · interacciones | Firebase Analytics (eventos de 08 §8.2) | Sí | No | Analítica | **Sí**: solo con consentimiento (08 §2.3; B12) | Con el país, el modelo del teléfono y el origen de la instalación (Install Referrer). Sin el ID de publicidad. **Incluye la banda de edad y las decisiones de consentimiento:** el evento `af_consent` (`age_band`, `analytics`, `ads_personalized`, `text_version`, `profile` y `step`; 08 §8.2; `GameBootstrap.Consent.cs`) y la propiedad de usuario `consent_profile` (08 §8.1), que solo salen con la analítica aceptada |
| Información y rendimiento de la app · registros de fallos | Firebase Crashlytics | Sí | No | Analítica (diagnóstico) [NV nombre de la finalidad] | **No** `[PENDIENTE: abogado]`: activo desde el arranque, también antes de la pantalla de edad, salvo con `<13` guardado, como interés legítimo (08 §2.3; 08 pregunta 6) | Sin el UID. Retención de 90 días de Firebase |
| Información y rendimiento de la app · diagnóstico | Crashlytics (estado del dispositivo: modelo, versión de Android, memoria); **la biblioteca de Play Billing** envía a Google sus propios registros de diagnóstico (fuente `PLAY_BILLING_LIBRARY` por `datatransport`) | Sí | No [E] | Analítica (diagnóstico); funcionamiento de Play Billing | Crashlytics: como la fila anterior. Play Billing: **no** opcional; la biblioteca se conecta en cada arranque (salvo `<13`), con la edad ya decidida y el UID (`GameBootstrap.Backend.cs`, `GameBootstrap.Consent.cs`) [E] | Lo de Play Billing es **Q-BILL-2** (D-040, añadido del 2026-10-23): se declara como parte de Google Play Billing, con contenido sin verificar [E]; lo revisa el abogado |
| Información y rendimiento de la app · otros datos de rendimiento | Firebase Analytics (`af_perf_sample`, `af_boot`, `af_level_load`) | Sí | No | Analítica | Sí (consentimiento de analítica) | `af_perf_sample`, 1 de cada 20 sesiones (08 §8.2) |
| ID de dispositivo u otros · ID de instancia | Firebase (Installations: Remote Config, Analytics, Crashlytics); App Check (veredicto de Play Integrity) | Sí | No | Funcionalidad; analítica; seguridad | Remote Config y App Check: no · Analytics: sí | `resetAnalyticsData()` al eliminar la cuenta (08 §13.3). El veredicto de integridad se guarda 24 h en el servidor (privacidad de la web, sección 9) |

**Dirección IP** `[PENDIENTE: abogado]` [NV]: el backend y la web la guardan 30 días en sus registros de acceso, solo para seguridad y abusos (08 §13.3, fila «Backend propio»; privacidad de la web, secciones 3 y 9). No se deriva ninguna ubicación. El abogado decide si Play pide declararla (por ejemplo, como «Ubicación aproximada» o dentro de «ID de usuario»).

**Partida:** se queda **solo en el teléfono** (`profile.json`, `profile.bak` y `PlayerPrefs`); hoy no hay guardado en la nube. No se declara porque no sale del dispositivo. La banda de edad y el consentimiento también se guardan en el teléfono, pero **sí salen** con la analítica aceptada (fila «Actividad en la app · interacciones»).

### 3.2 Al integrar D-042 (todavía no en el juego)

Se añaden a la sección 3.1, solo cuando MAX y Play Games estén integrados en el build que se envía:

| Categoría · tipo [NV] | Quién lo recoge | Recogido | Compartido | Finalidad | ¿Opcional? | Notas |
|---|---|---|---|---|---|---|
| Ubicación · aproximada | MAX y sus adaptadores (por la IP) | Sí [E] | **Sí** (redes de la mediación) | Publicidad; prevención de fraude | No [E] | El juego no pide ningún permiso de ubicación |
| ID de dispositivo u otros · ID de publicidad | MAX y adaptadores | Sí, **solo si el dueño aprueba `AD_ID`** (D-042: cada permiso se pregunta aparte) | **Sí** | Publicidad; prevención de fraude | Sí (el jugador lo reinicia o lo borra en Android) | Sin vínculo con el UID (08 §13.3). 13-17: anuncios no personalizados (`npa`) y `maxAdContentRating = G`; sin consentimiento de anuncios, no personalizados |
| Actividad en la app · interacciones (con los anuncios) | MAX y adaptadores | Sí | **Sí** | Publicidad | No [E] | — |
| Información y rendimiento de la app · diagnóstico | MAX y adaptadores | Sí | **Sí** | Publicidad; análisis de fraude | No [E] | — |
| Información personal · ID de usuario | Backend en AWS: los nonces de las recompensas de video (SSV de MAX, con el UID) | Sí | No | Prevención de fraude | Sí (solo quien ve videos opcionales) | Recompensas 30 días (08 §13.3) |
| Información personal · ID de usuario | Play Games (su ID de jugador de Play Games, que no es el código de cuenta de Ajustes) | Sí | No | Funcionalidad (guardado en la nube) | `[PENDIENTE: integrador de D-042]` [E]: depende de si el inicio de sesión silencioso cuenta como opcional | Con `<13`, sin inicio de sesión de Play Games (08 §13.1 #11) |
| Actividad en la app · otras acciones (partida guardada) [NV categoría] | Play Games Saved Games (snapshot `af_main`) | Sí | No | Funcionalidad | Igual que la fila anterior | Lo borra el cliente al eliminar la cuenta; el jugador puede además borrar su perfil de Play Games (08 §13.3; Q-B13-22) |

**UMP (Google User Messaging Platform)**, dentro de MAX: solo para el perfil legal `ump` (EEE, Reino Unido y Suiza); guarda la decisión en el teléfono [E]. 08 §13.3 no tiene fila propia para UMP: se cubre con la de MAX hasta que el integrador de D-042 confirme con la guía de Google si trata algo más.

### 3.3 No se recoge

Nombre, correo, dirección, teléfono, mensajes, fotos, vídeos, audio, archivos, calendario, contactos, salud, navegación web, ubicación precisa ni búsquedas (05 §7, «Minimización de SDK»; privacidad de la web, sección 4). **Unity Mobile Notifications** no recoge nada: son locales. **Firebase Messaging** no está en la 1.0 (entra en la 1.2 con su fila).

## 4. Por SDK (copia de trabajo de 08 §13.3, que manda)

| SDK (versión fijada) | Hoy | Qué declara |
|---|---|---|
| Firebase Analytics 13.17.0 | En el juego | Interacciones, otros datos de rendimiento, ID de instancia; analítica; solo con consentimiento |
| Firebase Crashlytics 13.17.0 | En el juego | Registros de fallos y diagnóstico; siempre activo salvo con `<13` (pendiente del abogado) |
| Firebase Remote Config, Auth y App Check 13.17.0 | En el juego | UID anónimo, ID de instancia, veredicto de integridad; funcionalidad y seguridad |
| Backend propio (AWS, `sa-east-1`) | En el juego | UID, libro mayor (token como hash), veredicto de integridad, código de borrado (como hash), IP en los registros 30 días; con D-042, nonces de recompensa |
| Google Play Billing Library 9.0.0 (plugin propio) | En el juego | Historial de compras; UID como `obfuscatedAccountId`; registros de diagnóstico de la biblioteca a Google (Q-BILL-2) [E]. Sin terceros |
| Unity Mobile Notifications 2.4.3 | En el juego | Nada |
| AppLovin MAX 8.6.6 + UMP + AdMob, Unity Ads, Meta Audience Network, Mintegral | **Al integrar D-042** | Ubicación aproximada, interacciones con anuncios, diagnóstico e ID de publicidad; compartidos con las redes |
| Play Games 2.3.0 (Saved Games) | **Al integrar D-042** | ID de jugador de Play Games y partida guardada; funcionalidad |
| Runtime de Unity 6000.3 (módulo `com.unity.modules.unityanalytics`, Engine Diagnostics) | En el build | **Sin comprobar** (Q-B13-9 y el «pendiente aparte» de D-040): build-release comprueba que no envía nada en release; si envía, lleva fila |
| ~~Unity IAP 5.4.4 y Unity Authentication~~ | **Ya no está** (D-040, Q-IAP-1) | Nada. Historia: Q-B13-20 |

## 5. Contraste con 08 §13.3 y con la privacidad de la web

- **Coinciden** en Firebase (filas de Analytics, Crashlytics y Remote Config/Auth/App Check), backend, Play Billing (con Q-BILL-2), notificaciones y el comportamiento con `<13`.
- **Huecos de 08 §13.3** (no se toca el GDD desde aquí; los recoge el integrador):
  - no tiene fila para UMP (sección 3.2) ni para el runtime de Unity (Q-B13-9);
  - la fila de Crashlytics no dice que funciona antes de la pantalla de edad, cosa que sí dicen 08 §2.3 y la web.
- **La web dice hoy** «Hoy el juego no muestra anuncios» y lista MAX y Play Games en «Lo que todavía no está en el juego» (sección 5). Al integrar D-042 hay que actualizar a la vez la web (`infra/web/src/content/*/privacy.html`, `terms.html` sección 6 y `credits.html`), este formulario y 08 §13.3.

## 6. Lo que tiene que cerrar alguien antes de enviar

- `[PENDIENTE: abogado]`: Crashlytics y la descarga de Remote Config antes del consentimiento (08 pregunta 6; 05 §7). Cambia la columna «¿Opcional?».
- `[PENDIENTE: abogado]`: el plazo fiscal del libro mayor sin el UID (08 §11); la dirección IP de los registros (sección 3.1); los registros de diagnóstico de Play Billing (Q-BILL-2).
- `[PENDIENTE: dueño]`: conectar `tintalu.com` (D-041) para que las URL de la política y del borrado sean públicas; la retención de Firebase Analytics, con la mínima del panel (Q-B13-19 (a)).
- build-release: Q-B13-9 (que el runtime de Unity no envía nada en release).
- El integrador de D-042: la guía de Data Safety de MAX, de cada adaptador, de UMP y de Play Games en su versión fijada, fila por fila (los [E] de la sección 3.2), y la decisión del dueño sobre `AD_ID`.
- **Rehacer el formulario con el build real de la 1.0**, no con este borrador.
