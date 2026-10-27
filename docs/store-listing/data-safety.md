# Seguridad de los datos (Data Safety) de Google Play · BORRADOR

- **Estado:** borrador para el formulario «Seguridad de los datos» de Play Console (08 §13.1 #12). Lo escribió el bloque B13 del Spark (2026-10-20); puesto al día el 2026-10-23 con lo que hoy es real: Firebase y el backend en AWS (D-035, D-039), el plugin propio de Play Billing en lugar de Unity IAP (D-040 y su añadido de Q-IAP-1), Parallext, LLC como responsable (D-041), la banda `<13` sin SDK (D-033, añadido del 2026-10-23) y MAX con UMP y Play Games, que se están integrando (D-042). Lo revisa el abogado (`[PENDIENTE: abogado]`) y lo envía el dueño.
- **Fuentes, en este orden:** 08 §13.3 (una fila por SDK, «[E, a confirmar con la guía de cada SDK]»), que manda; la política de privacidad pública de la web (`infra/web/src/content/es-419/privacy.html`, fuente de verdad de los textos legales); `docs/dev/firebase-cliente.md` (§1, §4 bis y §5); `docs/dev/iap.md` (§1.3 y §4.1, Q-BILL-2); 08 §2.3, §10 y §11; 05 §7.
- **Dos columnas de tiempo:** «**hoy**» es lo que recoge el build de `integracion-fase1-fase2` a 2026-10-23; «**al integrar D-042**» es lo que se añade con AppLovin MAX (con UMP y sus adaptadores) y Play Games 2.3.0. Hasta que D-042 se integre, lo segundo **todavía no está en el juego** y no se declara.
- **Nombres de categorías y tipos:** los del formulario de Play tal como los conoce este borrador [NV]; el dueño los contrasta con el formulario vigente. Las filas [E] se cierran con la guía de Data Safety de cada SDK en su versión fijada.
- **Ningún formulario se envía con el build de hoy:** la app aún no está creada en Play Console; ya no espera al dictamen (D-047) y la crea el dueño con AUTOMATION AI S.A.S como editora (D-048). Este documento prepara la 1.0.

## 1. Qué lleva hoy el build (2026-10-24)

| Componente | Estado hoy | Evidencia |
|---|---|---|
| Firebase 13.17.0: Auth anónimo, App Check (Play Integrity en release), Analytics, Crashlytics y Remote Config | **Integrado** (D-039). Sin Functions, Firestore ni Messaging. Analytics apagada hasta el consentimiento; solo concede `analytics_storage` (nunca `ad_storage`, `ad_user_data` ni `ad_personalization`) | `docs/dev/firebase-cliente.md` §1 y §5 |
| Backend propio en AWS, `sa-east-1` (São Paulo) | **Integrado** (D-035): verificación de compras, restauración del libro mayor, código de borrado y borrado de cuenta | 08 §13.3, fila «Backend propio»; `infra/contracts/v1` |
| Google Play Billing Library 9.0.0, con el plugin propio `AfBilling.androidlib` | **Integrado** (D-040, añadido de Q-IAP-1). **Unity IAP ya no está:** se quitó con sus dependencias de Unity Services y su telemetría de compras a Unity | `docs/dev/iap.md` §1; `ci/check-no-unity-iap.ps1` |
| Unity Mobile Notifications 2.4.3 | Integrado: avisos locales, nada sale del teléfono | 08 §13.3 |
| **Google Mobile Ads 11.5.0 (AdMob directo) + UMP 4.0.0** | **Integrado en `tanda6/sdk-gma` (D-049, 2026-10-25)**: anuncios de AdMob tras la edad, nuestro consentimiento y UMP; nunca con `<13`; permiso `AD_ID` activado (D-049; confirmado en D-055, punto 3). Sin anuncios con 13-17 en el EEE, el Reino Unido y Suiza (D-055, punto 4). Primera red de mediación: Unity Ads (fila siguiente) | `docs/dev/anuncios-admob.md` |
| **Unity Ads 4.20.1 (red de mediación de AdMob; adaptador 4.20.1.0, paquete 3.21.1)** | **Integrado en `tanda6/unity-ads` (D-049, añadido del 2026-10-27)**: solo dentro de la mediación de AdMob, iniciado por `MobileAds.Initialize` (su inicializador automático se quita del manifiesto); recibe nuestra decisión de anuncios personalizados por su `MetaData` (`gdpr.consent` y `privacy.consent`). Sin permisos nuevos | `docs/dev/anuncios-admob.md` §12 |
| AppLovin MAX 8.6.6 | Instalado **sin servir anuncios** y con su UMP apagado (D-049): no se inicializa, no recoge nada | `docs/dev/anuncios-max.md` |
| Play Games 2.3.0 (inicio de sesión silencioso y Saved Games) | **Todavía no en el juego** (D-042, en integración) | D-042 |

**Permisos del manifiesto fusionado hoy** (lista blanca de `ci/check-manifest.ps1`): `INTERNET` (D-036), `ACCESS_NETWORK_STATE` (D-037), `VIBRATE`, `WAKE_LOCK` y `BIND_GET_INSTALL_REFERRER_SERVICE` (Firebase Analytics, D-039), `POST_NOTIFICATIONS` (D-033, Q-M5), `com.android.vending.BILLING` (D-040) y **`com.google.android.gms.permission.AD_ID`** para los anuncios de AdMob (D-049; confirmado en D-055, punto 3; Firebase Analytics sigue sin leerlo: `google_analytics_adid_collection_enabled` = false). Sin ubicación. Siguen fuera, quitados en `LauncherManifest.xml`, `ACCESS_ADSERVICES_*`, `ACCESS_WIFI_STATE` y `CHECK_LICENSE` (D-055, punto 3), `FOREGROUND_SERVICE` y `RECEIVE_BOOT_COMPLETED`.

**Banda `<13` guardada** (D-033, añadido del 2026-10-23; `firebase-cliente.md` §4 bis): el arranque no inicia ningún SDK. Ni Crashlytics (se apaga por JNI y se borran los informes sin enviar), ni la descarga de Remote Config, ni Firebase en C#, Auth, App Check, Analytics, el backend, la tienda, las notificaciones ni, con D-042, los anuncios o Play Games. En el **primer arranque**, antes de la pantalla de edad, sí funcionan Crashlytics y la descarga de Remote Config (privacidad de la web, sección 3).

**El país del perfil legal** sale de la SIM o de la región del teléfono, en el dispositivo y sin servidor (D-033, Q-T3).

## 2. Panorama del formulario

| Pregunta del formulario [NV] | Respuesta hoy | Al integrar D-042 | Fuente |
|---|---|---|---|
| ¿La app recoge o comparte alguno de los tipos de datos de usuario obligatorios? | **Sí** (recoge; si la Liga está habilitada en el release, también muestra a otros jugadores los datos indicados en su sección) | **Sí** (recoge y **comparte** con las redes de anuncios, además de la Liga si está activa) | Sección 3 |
| ¿Todos los datos recogidos se encriptan en tránsito? | **Sí**: HTTPS de Firebase, Play Billing y la API de AWS | Sí (HTTPS de los SDK de anuncios y de Play Games) [E] | 08 §13.1 #12; privacidad de la web, sección 12 |
| ¿Ofreces una forma de pedir la eliminación de los datos? | **Sí: en la app** (Ajustes › «Eliminar cuenta y datos», dos confirmaciones) **y en la web** (página de borrado, con el **código de cuenta** de Ajustes) | Igual; el cliente borra además el snapshot `af_main` de Saved Games | 08 §11; §13.1 #7 y #12; `infra/web/src/content/*/delete.html` |
| URL de eliminación de cuenta | `https://tintalu.com/tintalu/eliminar-cuenta/` (Q-B13-1 (a); rutas en `infra/web/src/site.ts`). **El dominio aún no está conectado** (D-041): hoy la web solo se sirve en la URL de CloudFront de dev | — | D-041; 08 §11 |
| ¿La app permite crear una cuenta? | Sí, implícita: Firebase Auth anónimo; sin correo ni contraseña | Además, el vínculo con Play Games | 08 §11; §13.1 #7 |
| ¿Cumple la política de Familias? | No aplica: público 13+, sin programa Families; `<13` bloqueado | Igual | 08 §13.1 #11 |
| Revisión de seguridad independiente (MASA) | No, en la 1.0 [P] | — | — |

## 3. Tipos de datos

**Recogido** = sale del teléfono hacia Parallext, LLC o un SDK. **Compartido** = se transfiere a un tercero que no trata los datos por cuenta de Parallext, LLC; el proveedor de servicio que actúa siguiendo instrucciones no cuenta como compartido, y la guía de Play contempla otras excepciones condicionadas a la acción, divulgación y consentimiento del usuario [NV]. Por la visibilidad de la Liga a otros jugadores, el borrador recomienda declarar como compartidos los campos públicos hasta que el abogado confirme la categoría y cualquier excepción. [Guía oficial de Seguridad de los datos](https://support.google.com/googleplay/android-developer/answer/10787469?hl=en). Ningún dato es efímero salvo donde se dice. Con la banda `<13` guardada no se recoge **nada** de esta tabla (sección 1).

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

### 3.2 Anuncios (D-049: Google AdMob directo) y Play Games (D-042)

Se añaden a la sección 3.1 cuando el build que se envía los lleve. **Con D-049 los anuncios son de Google Mobile Ads (AdMob directo)**, no de MAX: donde abajo dice «MAX y adaptadores» léase «Google Mobile Ads (AdMob) y las redes de su mediación». **Desde el añadido de D-049 del 2026-10-27, Unity Ads es una de ellas**: comparte con Unity Technologies la ubicación aproximada (IP), el ID de publicidad, las interacciones con los anuncios y el diagnóstico del SDK cuando participa en una petición (no el UID ni el `custom_data` del SSV); en Data Safety, «Compartido: Sí» en esas filas cubre a Google y a Unity:

| Categoría · tipo [NV] | Quién lo recoge | Recogido | Compartido | Finalidad | ¿Opcional? | Notas |
|---|---|---|---|---|---|---|
| Ubicación · aproximada | MAX y sus adaptadores (por la IP) | Sí [E] | **Sí** (redes de la mediación) | Publicidad; prevención de fraude | No [E] | El juego no pide ningún permiso de ubicación |
| ID de dispositivo u otros · ID de publicidad | Google Mobile Ads (AdMob) | **Sí: `AD_ID` aprobado y activado (D-049, añadido del 2026-10-25; confirmado en D-055, punto 3)**; también el App Set ID | **Sí** | Publicidad; prevención de fraude | Sí (el jugador lo reinicia o lo borra en Android) | Sin vínculo con el UID (08 §13.3). Anuncios personalizados con el ID de publicidad solo con 18+ y el consentimiento de anuncios personalizados (en el EEE, el Reino Unido y Suiza, lo que decida el mensaje de UMP). 13-17: anuncios no personalizados (`npa`), `TagForUnderAgeOfConsent` y `maxAdContentRating = G`; 13-17 en el EEE, el Reino Unido y Suiza: sin anuncios (D-055, punto 4); `<13`: ningún SDK de anuncios. Sin consentimiento de anuncios, no personalizados (con `npa`, Google usa el ID solo para límites de frecuencia, informes agregados y prevención de fraude [NV, lo revisa el abogado]). Firebase Analytics no lo lee |
| Actividad en la app · interacciones (con los anuncios) | MAX y adaptadores | Sí | **Sí** | Publicidad | No [E] | — |
| Información y rendimiento de la app · diagnóstico | MAX y adaptadores | Sí | **Sí** | Publicidad; análisis de fraude | No [E] | — |
| Información personal · ID de usuario | Backend en AWS: los nonces de las recompensas de video (SSV de AdMob con D-049, con el UID como `user_id`; la verificación del servidor está pendiente) | Sí | No | Prevención de fraude | Sí (solo quien ve videos opcionales) | Recompensas 30 días (08 §13.3) |
| Información personal · ID de usuario | Play Games (su ID de jugador de Play Games, que no es el código de cuenta de Ajustes) | Sí | No | Funcionalidad (guardado en la nube) | `[PENDIENTE: integrador de D-042]` [E]: depende de si el inicio de sesión silencioso cuenta como opcional | Con `<13`, sin inicio de sesión de Play Games (08 §13.1 #11) |
| Actividad en la app · otras acciones (partida guardada) [NV categoría] | Play Games Saved Games (snapshot `af_main`) | Sí | No | Funcionalidad | Igual que la fila anterior | Lo borra el cliente al eliminar la cuenta; el jugador puede además borrar su perfil de Play Games (08 §13.3; Q-B13-22) |

**UMP (Google User Messaging Platform) 4.0.0**, con el plugin de Google (D-049): mensaje del RGPD (EEE, Reino Unido y Suiza) y de los estados de EE. UU.; guarda la decisión en el teléfono [E]. Se cubre con la fila de Google Mobile Ads de 08 §13.3.

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
| Google Mobile Ads 11.5.0 + UMP 4.0.0 (AdMob directo) | **D-049, rama `tanda6/sdk-gma`** | Ubicación aproximada (IP), interacciones con anuncios, diagnóstico e ID de publicidad; compartidos con Google (y las redes de su mediación cuando entren) |
| Unity Ads 4.20.1 (mediación de AdMob) | **D-049, añadido del 2026-10-27, rama `tanda6/unity-ads`** | Ubicación aproximada (IP), ID de publicidad, interacciones con anuncios y diagnóstico, compartidos con Unity Technologies cuando participa en una petición; nada antes de `MobileAds.Initialize` |
| AppLovin MAX 8.6.6 | Instalado sin servir (D-049) | Nada mientras no se inicialice |
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
- La guía de Data Safety de Google Mobile Ads 25.4.0 y UMP 4.0.0 (D-049), de cada red de mediación que entre y de Play Games en su versión fijada, fila por fila (los [E] de la sección 3.2). `AD_ID`: aprobado (D-049, añadido; confirmado en D-055, punto 3); se declara en Play Console («Uso del ID de publicidad»: publicidad).
- **Rehacer el formulario con el build real de la 1.0**, no con este borrador.

## Liga del río — D-046 (función prevista para la 1.0)

El alcance de producto incluye la Liga en la 1.0. El backend está desplegado solo en dev y `live_league_enabled` tiene valor predeterminado `false`; la función aún no está habilitada en producción. Para el formulario de Play del release, incluir estas filas si la build permite Liga y la configuración de producción la activa. Contrastar la build y la configuración antes del envío. [[ABOGADO: revisar]]

| Tipo de dato del formulario [NV] | Recogida / visibilidad | Finalidad y eliminación |
|---|---|---|
| Actividad en la app: otras acciones (victorias, fechas y puntos semanales de la Liga) | El backend AWS recibe victorias y fechas ligadas al UID. La clasificación pública muestra a otros jugadores el seudónimo, la bandera y los puntos semanales. **Compartido: sí, provisionalmente, por su visibilidad a otros jugadores.** [[ABOGADO: revisar categoría y posible excepción]] | Clasificación, progreso e integridad; no se usan para publicidad ni analítica adicional. Se eliminan al borrar la cuenta. Las filas semanales expiran por TTL al final de la semana más 14 días (aprox. tres semanas); eliminación técnica posterior. [[ABOGADO: revisar]] |
| País de la Liga (ISO 3166-1) [NV, confirmar categoría del formulario] | **Lo manda el teléfono, no sale de la conexión** (T-L12, comprobado en el código el 2026-10-27): el cliente envía `region` con el país del perfil legal (`GameBootstrap.League.cs` → `ConsentCountryCode`: el de la SIM o, si no hay, la región del teléfono; `ConsentCountry.Resolve`), y el servidor lo valida contra la lista ISO 3166-1 (`infra/functions/league/rules.ts`, `countryOf`) e **ignora `CloudFront-Viewer-Country`** (`infra/LEAGUE.md`). Ni la IP ni CloudFront dan el país; sin GPS ni permiso de ubicación. Queda fijado con la primera victoria aceptada de la semana y la bandera aparece en la clasificación. **No se declara «Ubicación aproximada» por la conexión** (la condición de §7.5 y T-L12 de la investigación legal era CloudFront). **Compartido: sí, como país visible a otros jugadores**; si el formulario pide una categoría para un país que el dispositivo declara, el dueño elige «Ubicación aproximada» por prudencia [E], igual que la respuesta de IARC (investigación legal §10.1, fila 11) | Permitir la clasificación por país. Se elimina con la cuenta y con la retención semanal descrita arriba. Es **el mismo país** que el del perfil legal; la política de la web (sección 3, «País» y «Liga del río») lo dice igual |
| Identificadores de usuario: UID y seudónimo automático | UID para atribuir victorias; seudónimo animal + cuatro cifras, sin texto libre. El UID no se muestra. En el tablero otros jugadores ven el seudónimo junto con bandera y puntos. **Compartido: sí para el seudónimo público; no para el UID interno.** [[ABOGADO: revisar categoría y posible excepción]] | Funcionamiento y prevención de duplicados/abuso. Borrar cuenta elimina puntuaciones, seudónimo y reserva de pareja del jugador. [[ABOGADO: revisar]] |

Con menos de 13 años no se muestra la Liga ni se envían victorias o país para ella. Para 13–17 y 18+ la exposición en el tablero es la misma. La recomendación provisional es declarar como compartidos los campos que ven otros jugadores; el abogado debe confirmar su clasificación y si se cumple una excepción de transferencia iniciada por el usuario. No marcar «no compartidos» automáticamente. Todo usa HTTPS y AWS, descrito como proveedor en la política. [[ABOGADO: revisar]]
