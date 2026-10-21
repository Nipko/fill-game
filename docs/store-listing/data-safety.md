# Seguridad de los datos (Data Safety) de Google Play · BORRADOR

- **Estado:** borrador del bloque B13 (2026-10-20) para el formulario «Seguridad de los datos» de Play Console (08 §13.1 #12). Lo revisa el abogado (`[PENDIENTE: abogado]`) y lo envía el dueño. **La fuente es 08 §13.3** (una fila por SDK, «[E, a confirmar con la guía de cada SDK]»), con 08 §7 (lista de SDK), §8 (analítica), §10 (seguridad), §11 (guardado y eliminación) y 05 §7 (consentimiento y edad).
- **Los nombres de las categorías y tipos de datos** son los del formulario de Play tal como los conoce este borrador [NV]: el dueño los contrasta con el formulario vigente al rellenarlo. Cada SDK publica su propia guía de Data Safety; las filas marcadas [E] se cierran con esa guía y la versión fijada en 08 §7.

## 1. Estado de hoy (2026-10-20): sin SDK

| Pregunta | Respuesta hoy | Evidencia |
|---|---|---|
| SDK de terceros en el build | Ninguno: ni MAX, ni Firebase, ni Unity IAP, ni Play Games | `Packages/manifest.json` (solo paquetes de Unity y `com.unity.pipeline`); `CLAUDE.md`, «DLL x64 en el Editor» |
| Llamadas de red del juego | Ninguna encontrada (búsqueda de `UnityWebRequest`, `HttpClient`, `WebClient` y `Socket` en `Assets/_Project/Scripts/` y en el núcleo, 2026-10-20) | — |
| Permisos Android | `VIBRATE` (aprobado, D-026); `POST_NOTIFICATIONS` (aprobado para las notificaciones locales, D-033 Q-M5; entra con B6); `INTERNET`, que Unity añade solo, **pendiente del visto bueno del dueño** | `CLAUDE.md`; D-033 |
| Datos guardados | Solo en el teléfono: `profile.json`, `profile.bak` y `PlayerPrefs` (08 §11) | 08 §11 |

Si hubiera que declarar el build de hoy, la respuesta sería «la app no recoge ni comparte datos de usuario». **No se publica nada con el build de hoy** (regla 6 de `CLAUDE.md`): este documento prepara la 1.0, con los SDK previstos en 08 §7.

**Pregunta abierta:** el manifiesto incluye el módulo `com.unity.modules.unityanalytics`. Antes de enviar el formulario hay que comprobar que Unity Analytics y Cloud Diagnostics están apagados en el build de release (Q-B13-9).

## 2. Panorama del formulario para la 1.0

| Pregunta del formulario [NV] | Respuesta propuesta | Fuente |
|---|---|---|
| ¿La app recoge o comparte alguno de los tipos de datos de usuario obligatorios? | **Sí** | 08 §13.3 |
| ¿Todos los datos de usuario recogidos se encriptan en tránsito? | **Sí** (HTTPS de los SDK y de Cloud Functions) | 08 §13.1 #12 |
| ¿Ofreces una forma de pedir la eliminación de los datos? | **Sí: en la app** (Ajustes › «Eliminar cuenta y datos», en dos pasos) **y en la web** (`[PENDIENTE: dueño]` URL; borrador en `eliminar-cuenta.md`), además del canal de soporte | 08 §11; §13.1 #7 y #12 |
| ¿La app permite crear una cuenta? | Sí, implícita: Firebase Auth anónimo desde el primer arranque y vínculo con Play Games; sin correo ni contraseña | 08 §11, «Identidad»; §13.1 #7 |
| ¿Cumple la política de Familias? | No aplica: público objetivo 13+, sin programa Families | 08 §13.1 #11 |
| Revisión de seguridad independiente (MASA) | No, en la 1.0 [P] | — |

## 3. Tipos de datos (1.0 prevista)

**Recogido** = sale del teléfono hacia el estudio o un SDK. **Compartido** = se transfiere a un tercero que no actúa como proveedor de servicio del estudio; según la definición de Play, lo que procesa un proveedor por cuenta del estudio no cuenta como compartido [NV]. Ningún dato es efímero salvo donde se dice.

| Categoría · tipo [NV] | SDK que lo recoge (08 §7, §13.3) | Recogido | Compartido | Finalidad | ¿Opcional? | Notas |
|---|---|---|---|---|---|---|
| Ubicación · aproximada | MAX y sus adaptadores (IP aproximada) | Sí [E] | **Sí** (redes de la mediación) | Publicidad; prevención de fraude | Depende de la red [E] | El juego no pide ningún permiso de ubicación. El país del perfil legal sale de la SIM o la región del teléfono, en el dispositivo, sin servidor (D-033 Q-T3); `geo_tier`, de la región (Q-E3) |
| Información personal · ID de usuario | Firebase Auth (UID anónimo); Play Games (ID de jugador) | Sí | No | Funcionalidad de la app (guardado en la nube, compras, recompensas); seguridad | No (necesario para guardar en la nube y verificar compras) | El jugador ve su ID en Ajustes para la eliminación por web (08 §11). Sin nombre, correo, teléfono ni dirección |
| Información financiera · historial de compras | Unity IAP / Play Billing; libro mayor `users/{uid}/purchases/{token}` en Cloud Functions | Sí | No (lo procesa Google Play) | Funcionalidad; prevención de fraude | Sí (solo quien compra) | Tras la eliminación, el libro mayor queda **anonimizado** (sin UID) el plazo fiscal `[PENDIENTE: abogado]` (08 §11) |
| Actividad en la app · interacciones | Firebase Analytics (eventos de 08 §8.2); MAX (interacción con anuncios) | Sí | Analytics: No · MAX: **Sí** | Analítica; publicidad | **Sí**: Analytics solo con consentimiento (08 §2.3); anuncios personalizados solo con consentimiento | Con <13 bloqueado, 13-17 sin perfilado (`npa`) |
| Información y rendimiento de la app · registros de fallos | Firebase Crashlytics | Sí | No | Diagnóstico | **`[PENDIENTE: abogado]`**: hoy se activa en el arranque como interés legítimo (08 §2.3; 08 pregunta 6) | Retención de 90 días de Firebase (08 §13.3) |
| Información y rendimiento de la app · diagnóstico | Crashlytics; MAX | Sí | MAX: **Sí** | Diagnóstico; publicidad | Como la fila anterior | — |
| Información y rendimiento de la app · otros datos de rendimiento | Firebase Analytics (`af_perf_sample`, `af_boot`, `af_level_load`) | Sí | No | Analítica | Sí (consentimiento de analítica) | `af_perf_sample`, 1 de cada 20 sesiones (08 §8.2) |
| ID de dispositivo u otros · ID de publicidad | MAX y adaptadores | Sí | **Sí** | Publicidad; prevención de fraude | Sí (el usuario lo reinicia o lo borra en Android) | Sin vínculo con el UID (08 §13.3); `TagForChildDirectedTreatment` con <13 |
| ID de dispositivo u otros · ID de instancia | Firebase (Analytics, Remote Config, App Check) | Sí | No | Analítica; funcionalidad; seguridad | Analytics: sí · Remote Config y App Check: no | `resetAnalyticsData()` al eliminar la cuenta (08 §13.3) |

**No se recoge:** nombre, correo, dirección, teléfono, mensajes, fotos, vídeos, audio, archivos, calendario, contactos, salud, navegación web, ubicación precisa ni búsquedas (05 §7, fila «Minimización de SDK»; 08 §7). **Unity Mobile Notifications** no recoge nada: son locales (08 §13.3). **Firebase Messaging** no está en la 1.0 (08 §7; entra en la 1.2 con su fila).

## 4. Por SDK (copia de trabajo de 08 §13.3, que manda)

| SDK (versión fijada en 08 §7) | Qué declara en este formulario |
|---|---|
| AppLovin MAX 8.6.6 + AdMob, Unity Ads, Meta Audience Network, Mintegral | Ubicación aproximada, actividad (interacción con anuncios), diagnóstico e ID de publicidad; compartidos con las redes; publicidad y fraude |
| Firebase Analytics 13.17.0 | Interacciones, otros datos de rendimiento, ID de instancia; analítica; solo con consentimiento |
| Firebase Crashlytics 13.17.0 | Registros de fallos y diagnóstico; diagnóstico |
| Firebase Remote Config, Auth, App Check y Functions 13.17.0 | ID de usuario (UID anónimo), ID de instancia, veredicto de integridad, libro mayor; funcionalidad y seguridad |
| Unity IAP 5.4.4 / Play Billing 9.0.0 | Historial de compras; funcionalidad |
| Play Games 2.3.0 (Saved Games) | ID de jugador y partida guardada; funcionalidad |
| Unity Gaming Services Authentication (dependencia de Unity IAP 5.4.x, 08 §7 fila 9) | **Hueco de 08 §13.3**, que no tiene fila para ella: probablemente un ID de jugador anónimo de Unity [E]; se confirma con la guía de Unity y se añade la fila (Q-B13-20) |
| Unity Mobile Notifications (versión: la anota quien la integre, D-033) | Nada |
| Addressables / PAD, EDM4U, Unity Localization | Nada propio [E] |

## 5. Lo que tiene que cerrar alguien antes de enviar

- `[PENDIENTE: abogado]`: Crashlytics y Analytics antes del consentimiento en BR/CO/PE/MX (08 pregunta 6; 05 §7, última fila). Cambia la columna «¿Opcional?».
- `[PENDIENTE: abogado]`: el plazo fiscal del libro mayor anonimizado (08 §11).
- `[PENDIENTE: dueño]`: la URL pública de la política de privacidad y la de eliminación de cuenta (08 §13.1 #2 y #7; Q-B13-1).
- `[PENDIENTE: dueño]`: la retención de datos de Firebase Analytics, que 08 §13.3 deja en «la configurada en Firebase» (Q-B13-19).
- El integrador de SDK: la guía de Data Safety de cada SDK en su versión fijada, fila por fila (los [E] de 08 §13.3).
- El integrador de SDK y el dueño: la fila que falta de Unity Authentication (sección 4; Q-B13-20).
