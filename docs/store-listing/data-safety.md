# Seguridad de los datos (Data Safety) de Google Play · respuestas para la 1.0

- **Estado (2026-10-28):** forma final para el formulario «Seguridad de los datos» de Play Console (08 §13.1 #12), con lo que lleva el build de la 1.0 según 08 §13.3: Google Mobile Ads con UMP (AdMob directo, D-049), Unity Ads como red de su mediación (añadido de D-049 del 2026-10-27), Firebase (D-039), el backend en AWS (D-035), Play Billing con el plugin propio (D-040), Play Age Signals (D-057), Play Games con Saved Games (D-042), Unity Mobile Notifications (D-033, Q-M5) y la Liga del río (D-046). **AppLovin MAX ya no está en el build** (D-049, añadido del 2026-10-27; `ci/check-no-applovin.ps1`) y no se declara. Lo rellena y lo envía el dueño; el paso a paso está en `docs/dev/publicar-play-console.md` §8. Los `[PENDIENTE: abogado]` siguen: con D-057 los resuelve la investigación propia (`docs/legal/investigacion-legal-mundial.md`) o quedan como riesgo aceptado por el dueño.
- **Fuentes, en este orden:** 08 §13.3 (una fila por SDK, «[E, a confirmar con la guía de cada SDK]»), que manda; la política de privacidad de la web (`infra/web/src/content/en/privacy.html`, la que se declara en Play por D-057, y `es-419/privacy.html`); `docs/dev/firebase-cliente.md` (§1, §4 bis y §5); `docs/dev/iap.md` (§1.3 y §4.1, Q-BILL-2); `docs/dev/anuncios-admob.md` (§5, §8, §9 y §12); `docs/dev/play-games.md`; `docs/dev/liga.md`; 08 §2.3, §10 y §11; 05 §7.
- **Nombres de preguntas, categorías y tipos:** los del formulario de Play tal como los conoce este documento [NV]; el dueño los contrasta con el formulario vigente. Lo marcado [E] se cierra con la guía de Data Safety de cada SDK en su versión fijada.
- **Historia:** la versión anterior, con las columnas «hoy» y «al integrar D-042» (MAX), se lee con `git show 7ca60990:docs/store-listing/data-safety.md`.

## 1. Qué lleva el build de la 1.0

| Componente (versión fijada) | Qué hace | Fuente |
|---|---|---|
| Firebase 13.17.0: Auth anónimo, App Check (Play Integrity en release), Analytics, Crashlytics y Remote Config | Sin Functions, Firestore ni Messaging. Analytics apagada hasta el consentimiento; solo concede `analytics_storage` (nunca `ad_storage`, `ad_user_data` ni `ad_personalization`); no lee el ID de publicidad (`google_analytics_adid_collection_enabled` = false). Crashlytics, activado por defecto con el interruptor «Enviar informes de fallos» de Ajustes (T-L6; D-057) | 08 §13.3; `firebase-cliente.md` §1 y §5 |
| Backend propio en AWS, `sa-east-1` | Verificación de compras, restauración del libro mayor, SSV de las recompensas, código de borrado, borrado de cuenta y la Liga | 08 §13.3, fila «Backend propio»; `infra/contracts/v1` |
| Google Play Billing Library 9.0.0 (plugin propio `AfBilling.androidlib`) | Compras. Unity IAP no está (`ci/check-no-unity-iap.ps1`) | D-040 y su añadido de Q-IAP-1 |
| Google Mobile Ads 11.5.0 (`play-services-ads` 25.4.0) + UMP 4.0.0 | Anuncios de AdMob tras la edad y el consentimiento; nunca con `<13`; con 13-17, no personalizados; con 13-17 en el EEE, el Reino Unido y Suiza, sin anuncios. Permiso `AD_ID` | D-049 y su añadido del 2026-10-25; D-055, puntos 3 y 4 |
| Unity Ads 4.20.1 (adaptador 4.20.1.0, paquete 3.21.1) | Solo dentro de la mediación de AdMob, iniciado por `MobileAds.Initialize`; sin permisos nuevos | D-049, añadido del 2026-10-27; `anuncios-admob.md` §12 |
| Play Games 2.3.0 (`play-services-games-v2` 22.1.0) | Inicio de sesión silencioso y Saved Games (`af_main`), con 13+. **Solo funciona con el `APP_ID` del proyecto de Play Games** (`play-games.md` §9): sin él, la nube no hace nada y esta fila no aplica | D-042; 08 §13.3 |
| Play Age Signals API 0.0.4 | Franja de edad de Google Play, solo en el teléfono; nunca a la analítica | D-057, añadido del 2026-10-27 (T-L3, T-L4) |
| Unity Mobile Notifications 2.4.3 | Avisos locales; nada sale del teléfono | 08 §13.3 |
| Liga del río | Victorias, puntos, seudónimo automático, avatar (índice) y país, en el backend; visibles a otros jugadores. Interruptor `live_league_enabled` de Remote Config, **apagado por defecto** (08 §9) | D-046; D-056; `liga.md` |

**Permisos del manifiesto fusionado** (lista blanca de `ci/check-manifest.ps1`): `INTERNET` (D-036), `ACCESS_NETWORK_STATE` (D-037), `VIBRATE`, `WAKE_LOCK` y `BIND_GET_INSTALL_REFERRER_SERVICE` (Firebase, D-039), `POST_NOTIFICATIONS` (D-033, Q-M5), `com.android.vending.BILLING` (D-040) y `com.google.android.gms.permission.AD_ID` (D-049; D-055, punto 3). Sin ubicación. Fuera: `ACCESS_ADSERVICES_*`, `ACCESS_WIFI_STATE`, `CHECK_LICENSE`, `FOREGROUND_SERVICE` y `RECEIVE_BOOT_COMPLETED`.

**Banda `<13`** (D-033, añadido del 2026-10-23; `firebase-cliente.md` §4 bis): con la banda guardada, el arranque no inicia ningún SDK (ni Firebase, ni anuncios, ni Play Games, ni la tienda, ni el backend, ni las notificaciones, ni la Liga). En el **primer arranque**, antes de la pantalla de edad, sí funcionan Crashlytics (si el interruptor está encendido) y la descarga de Remote Config (privacidad de la web, sección 3).

**El país legal** sale, en el teléfono, de la SIM; si no, del país de la cuenta de Google Play (`getBillingConfigAsync`); si no, de la región del teléfono (D-033, Q-T3; D-057, P4 (a)). Va como `region` de la Liga si el jugador participa y, agregado en `geo_tier`, en la analítica con consentimiento (D-057, revisión del código mundial, punto 1).

**La Liga en el formulario [P]:** el formulario describe lo que la app puede hacer, no lo que hoy está encendido. Si la Liga va a estar encendida en producción en la 1.0, o puede encenderse por Remote Config sin publicar otra versión, se declaran sus filas (marcadas «Liga» abajo). Si se deja apagada hasta otra versión, se quitan y se vuelve a enviar el formulario antes de encenderla. Declarar de más está permitido; declarar de menos, no.

## 2. Preguntas generales del formulario

| # | Pregunta [NV] | Respuesta | Fuente |
|---|---|---|---|
| 1 | ¿Tu app recoge o comparte alguno de los tipos de datos de usuario obligatorios? | **Sí** | Sección 3 |
| 2 | ¿Todos los datos de usuario que recoge tu app están encriptados en tránsito? | **Sí**: HTTPS en Firebase, Play Billing, la API de AWS, AdMob, Unity Ads y Play Games | 08 §13.1 #12; privacidad de la web, sección 12 |
| 3 | ¿Qué métodos de creación de cuenta admite tu app? [NV, la forma de la pregunta] | **Sin nombre de usuario ni contraseña: cuenta anónima** (Firebase Auth anónimo, creada sola al jugar) y el inicio de sesión silencioso de Play Games. Si el formulario solo ofrece «nombre de usuario y contraseña», «OAuth» u «otro», elegir **«Otro»** y describir: «Cuenta anónima creada automáticamente; no se piden datos personales» [P] | 08 §11; §13.1 #7 |
| 4 | ¿Ofreces una forma de que los usuarios pidan que se eliminen sus datos? | **Sí, en la app y en la web.** En la app: Ajustes › «Eliminar cuenta y datos», con dos confirmaciones. En la web, con el código de cuenta que enseña Ajustes | 08 §11; §13.1 #7 y #12 |
| 5 | URL de eliminación de la cuenta | **`https://tintalu.com/tintalu/en/delete-account/`** (en-US es el idioma por defecto de la ficha, D-057). Las otras: `https://tintalu.com/tintalu/eliminar-cuenta/` (es-419) y `https://tintalu.com/tintalu/pt-br/excluir-conta/` (pt-BR) | `infra/web/src/site.ts`; D-044 |
| 6 | ¿Se puede pedir que se borren algunos datos sin borrar la cuenta? [NV] | **No** como función propia. Lo que sí hay: apagar la analítica (consentimiento) y los informes de fallos en Ajustes; el reinicio del ID de publicidad en Android | Privacidad de la web, sección 8 |
| 7 | ¿La app cumple la política de Familias? | No aplica: público 13+, sin el programa Familias; `<13` bloqueado | 08 §13.1 #11 |
| 8 | Revisión de seguridad independiente (MASA) | **No** en la 1.0 [P] | — |

## 3. Tipos de datos, uno por uno

**Recogido** = sale del teléfono hacia Parallext, LLC o hacia un SDK. **Compartido** = se transfiere a un tercero que no trata los datos por cuenta de Parallext, LLC; el proveedor que actúa siguiendo instrucciones (AWS, Google como encargado de Firebase) no cuenta. La guía de Play admite otras excepciones condicionadas a la acción, la divulgación y el consentimiento del usuario [NV]. [Guía oficial de Seguridad de los datos](https://support.google.com/googleplay/android-developer/answer/10787469?hl=en). Ningún dato se trata como efímero. Con la banda `<13` no se recoge nada de esta tabla.

En cada tipo, el formulario pregunta [NV]: recogido o compartido; si se trata de forma efímera; si es obligatorio u opcional; y las finalidades (funcionalidad de la app, analítica, comunicaciones del desarrollador, publicidad o marketing, prevención de fraude, seguridad y cumplimiento, personalización, gestión de cuentas).

| Categoría · tipo [NV] | Recogido | Compartido | Efímero | Obligatorio u opcional | Finalidades | Quién y notas |
|---|---|---|---|---|---|---|
| **Ubicación · aproximada** | Sí | **Sí** | No | Obligatorio [E] | Publicidad o marketing; prevención de fraude, seguridad y cumplimiento; **Liga:** funcionalidad de la app | AdMob y Unity Ads la derivan de la IP (08 §13.3). **Liga:** el país legal que manda el teléfono (`region`), visible a otros jugadores como bandera; ni la IP ni CloudFront dan ese país (T-L12; `infra/LEAGUE.md`). Por prudencia, el país de la Liga se declara aquí [E]. El juego no pide ningún permiso de ubicación. La IP de los registros de acceso del backend y de la web (30 días, solo seguridad) no se declara aparte (Q-B13-24 (a)) `[PENDIENTE: abogado]` |
| **Información personal · ID de usuario** | Sí | **Sí**: el UID va a Google (AdMob) como `user_id` del SSV de los recompensados, para verificar las recompensas (prevención de fraude, seguridad y cumplimiento; 08 §13.3, fila de Google Mobile Ads); **Liga:** además, el seudónimo automático, visible a otros jugadores | No | Obligatorio | Funcionalidad de la app; prevención de fraude, seguridad y cumplimiento; gestión de cuentas | El UID anónimo de Firebase Auth, en el backend y en Google Play como `obfuscatedAccountId`; en AdMob, como `user_id` del SSV de los recompensados (08 §10.2). Sin nombre, correo ni teléfono. Ajustes enseña el **código de cuenta**, no el UID; el servidor solo guarda su hash (08 §11). Play Games: el ID de jugador lo gestiona Google; el juego no lo lee, no lo guarda ni lo manda a su servidor (08 §13.3). **Liga:** seudónimo animal + cuatro cifras, sin texto libre `[PENDIENTE: abogado]` (categoría y posible excepción de transferencia iniciada por el usuario) |
| **Información financiera · historial de compras** | Sí | No | No | Opcional (solo quien compra) | Funcionalidad de la app; prevención de fraude, seguridad y cumplimiento | Google Play Billing procesa el pago; el backend guarda el libro mayor con el token de compra como hash. Tras el borrado, el libro mayor queda **sin el UID** el plazo fiscal `[PENDIENTE: abogado]` (08 §11). Firebase Analytics puede registrar el evento automático `in_app_purchase`, solo con consentimiento [E] |
| **Actividad en la app · interacciones en la app** | Sí | **Sí** (con los anuncios) | No | Analítica: **opcional**. Anuncios: obligatorio [E] | Analítica; publicidad o marketing | Firebase Analytics, solo con el consentimiento de analítica (08 §2.3; 08 §8.2), con la banda de edad autodeclarada y las decisiones de consentimiento (`af_consent`; `consent_profile`). La señal de Play Age Signals **nunca** va a la analítica (T-L4). AdMob y Unity Ads: las interacciones con los anuncios, compartidas con Google y con Unity Technologies (08 §13.3) |
| **Actividad en la app · otras acciones del usuario** | Sí | **Liga:** sí (puntos semanales visibles) | No | Obligatorio para quien juega la Liga o tiene Play Games [E] | Funcionalidad de la app | **Liga:** victorias, fechas y puntos semanales, ligados al UID; la clasificación enseña a otros jugadores el seudónimo, el avatar, la bandera y los puntos; las filas semanales caducan por TTL (semana + 14 días) y se borran con la cuenta `[PENDIENTE: abogado]`. **Play Games:** la partida guardada en Saved Games (`af_main`: progreso, cartera y ajustes, sin la edad, el consentimiento ni el código de cuenta), en la cuenta de Google del jugador; la borra el cliente al eliminar la cuenta (08 §13.3; Q-B13-22) |
| **Información y rendimiento de la app · registros de fallos** | Sí | No | No | **Opcional** (Ajustes › «Enviar informes de fallos», T-L6; D-057) | Analítica (diagnóstico) [NV nombre] | Crashlytics, activado por defecto desde el arranque (también antes de la pantalla de edad, como interés legítimo, 08 §2.3); apagado, se borran los informes sin enviar. Sin el UID. Retención de 90 días |
| **Información y rendimiento de la app · diagnóstico** | Sí | **Sí** (los SDK de anuncios) | No | Obligatorio [E] | Analítica; publicidad o marketing; prevención de fraude, seguridad y cumplimiento; funcionalidad de la app | Crashlytics (modelo, versión de Android, memoria); los registros de diagnóstico propios de la biblioteca de Play Billing a Google (Q-BILL-2, sin verificar su contenido [E]) `[PENDIENTE: abogado]`; el diagnóstico de los SDK de AdMob y Unity Ads (08 §13.3) |
| **Información y rendimiento de la app · otros datos de rendimiento** | Sí | No | No | Opcional (consentimiento de analítica) | Analítica | Firebase Analytics: `af_perf_sample` (1 de cada 20 sesiones), `af_boot` y `af_level_load` (08 §8.2) |
| **ID de dispositivo u otros · ID de dispositivo u otros identificadores** | Sí | **Sí** (ID de publicidad) | No | Obligatorio [E] | Funcionalidad de la app; analítica; publicidad o marketing; prevención de fraude, seguridad y cumplimiento | **ID de publicidad** (permiso `AD_ID`) y App Set ID: AdMob y Unity Ads; solo personalizan con 18+ y el consentimiento de anuncios personalizados (o lo que decida UMP en el EEE, el Reino Unido y Suiza); con `npa`, Google lo usa para límites de frecuencia, informes agregados y fraude [NV] `[PENDIENTE: abogado]`. Con 13-17 y país `US`, `rdp=1` (T-L5). **ID de instancia de Firebase** (Remote Config, Analytics, Crashlytics) y el veredicto de App Check (Play Integrity), que el servidor guarda 24 h. `resetAnalyticsData()` al eliminar la cuenta (08 §13.3) |

**No se recoge** (sección 4 de la privacidad de la web; 05 §7, «Minimización de SDK»): nombre, correo, dirección, teléfono, mensajes, fotos, vídeos, audio, archivos, calendario, contactos, salud y forma física, información financiera distinta del historial de compras, navegación web, ubicación precisa, búsquedas ni apps instaladas. **Unity Mobile Notifications** no recoge nada: los avisos son locales. **Play Age Signals** no saca nada del teléfono por parte del juego (08 §13.3) [E: si la guía de Google pide declarar algo, se añade]. **Firebase Messaging** no está en la 1.0.

**La partida local** (`profile.json`, `profile.bak`, `PlayerPrefs`) no sale del teléfono salvo por Saved Games (fila «otras acciones»).

## 4. Por SDK (copia de trabajo de 08 §13.3, que manda)

| SDK (versión fijada) | Qué declara |
|---|---|
| Google Mobile Ads 11.5.0 + UMP 4.0.0 (AdMob directo) | Ubicación aproximada (IP), ID de publicidad y App Set ID, interacciones con anuncios y diagnóstico; compartidos con Google; el UID como `user_id` del SSV en los recompensados. UMP guarda su decisión (TCF/GPP) en el teléfono |
| Unity Ads 4.20.1 (mediación de AdMob) | Ubicación aproximada (IP), ID de publicidad, interacciones con anuncios y diagnóstico, compartidos con Unity Technologies cuando participa en una petición; no recibe el UID ni el `custom_data` del SSV |
| Firebase Analytics 13.17.0 | Interacciones, otros datos de rendimiento, ID de instancia; analítica; solo con consentimiento |
| Firebase Crashlytics 13.17.0 | Registros de fallos y diagnóstico; opcional con el interruptor de Ajustes |
| Firebase Remote Config, Auth y App Check 13.17.0 | UID anónimo, ID de instancia, veredicto de integridad; funcionalidad y seguridad |
| Backend propio (AWS, `sa-east-1`) | UID, libro mayor (token como hash), nonces de las recompensas (30 días), veredicto de integridad, código de borrado (como hash), IP en los registros 30 días; la Liga |
| Google Play Billing Library 9.0.0 (plugin propio) | Historial de compras; UID como `obfuscatedAccountId`; registros de diagnóstico de la biblioteca a Google (Q-BILL-2) [E] |
| Play Games 2.3.0 (Saved Games) | Partida guardada; el ID de jugador lo gestiona Google |
| Play Age Signals 0.0.4 | Nada sale del teléfono por nuestra parte [E] |
| Unity Mobile Notifications 2.4.3 | Nada |
| Runtime de Unity 6000.3 (`com.unity.modules.unityanalytics`, Engine Diagnostics) | **Sin comprobar** (Q-B13-9 y el «pendiente aparte» de D-040): si envía algo en release, lleva fila |
| ~~AppLovin MAX 8.6.6~~ y ~~Unity IAP 5.4.4~~ | Fuera del build (D-049, añadido del 2026-10-27; D-040, Q-IAP-1): nada |

## 5. Contraste con 08 §13.3 y con la web

- **Coinciden** en todas las filas de 08 §13.3 de la 1.0. UMP va dentro de la fila de Google Mobile Ads de 08 §13.3, así que Q-B13-25 queda cerrada.
- **Hueco de 08 §13.3** (no se toca el GDD desde aquí): no tiene fila para el runtime de Unity (Q-B13-9) ni para la Liga (que vive en la fila «Backend propio» y en `liga.md`).
- La privacidad de la web es la fuente de los textos legales. Si este formulario y la web no dicen lo mismo, manda lo que hace el build y se corrigen los dos.

## 6. Lo que queda antes de enviar

- `[PENDIENTE: abogado]` (con D-057, investigación propia o riesgo aceptado): el plazo fiscal del libro mayor sin el UID (08 §11); la IP de los registros (Q-B13-24); los registros de diagnóstico de Play Billing (Q-BILL-2); el uso del ID de publicidad con `npa`; la categoría de los datos visibles de la Liga.
- build-release: Q-B13-9 (que el runtime de Unity no envía nada en release).
- Las guías de Data Safety de Google Mobile Ads 25.4.0, UMP 4.0.0, Unity Ads 4.20.1, Play Games 2.3.0 y Play Age Signals 0.0.4, fila por fila (los [E]).
- Decidir si la Liga entra encendida (sección 1, «La Liga en el formulario»).
- **Rellenarlo con el build que se promociona a producción**, no con este documento a ciegas: si el build lleva algo distinto (por ejemplo, Play Games sin `APP_ID`), se ajusta.
