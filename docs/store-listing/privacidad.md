# Política de privacidad de Tintalú · BORRADOR es-419

> **Borrador del bloque B13 (2026-10-20), escrito por un agente: no es asesoría legal.** Lo redacta en limpio y lo firma el abogado (`[PENDIENTE: abogado]`). Se publica en el dominio canónico (`[PENDIENTE: dueño]`, propuesto `tintalu.com` [P]; 08 §13.1 #2) en ES, PT-BR y EN, con DPO y canal LGPD y el aviso simplificado de México (08 §13.1 #2). EN y PT-BR se traducen cuando el abogado apruebe este texto (Q-B13-4).
>
> **Fuentes:** 08 §2.3 (arranque y consentimiento), §7 (SDK), §8 (analítica), §10 (seguridad), §11 (guardado y eliminación), §13.3 (Data Safety); 05 §7 (cumplimiento y edad); D-033 (Q-T3, Q-T4, Q-M5). Debe coincidir con `data-safety.md`: si uno cambia, cambia el otro.
>
> Las reglas de redacción son las de la UI (tuteo, español latino neutro). Los corchetes son huecos que no se pueden inventar.

---

**Versión:** `[PENDIENTE: dueño]` (número que coincide con `consent_text_version`, 08 §9) · **Fecha de entrada en vigor:** `[PENDIENTE: dueño]`

## 1. Quiénes somos

Tintalú es un juego para teléfonos Android desarrollado por `[PENDIENTE: dueño — razón social o nombre del titular]`, con domicilio en `[PENDIENTE: dueño — dirección y país]` («nosotros»). Somos los responsables del tratamiento de tus datos personales.

- Contacto de privacidad: `[PENDIENTE: dueño — correo]` (Q-B13-3).
- Encargado de protección de datos (DPO; «encarregado» de la LGPD): `[PENDIENTE: dueño — nombre y correo]`.

Tintalú es un juego independiente inspirado en la cuenca amazónica; sin relación con Amazon.com, Inc.

## 2. Resumen

- Puedes jugar sin registrarte: no te pedimos nombre, correo ni teléfono.
- Antes de jugar te preguntamos tu edad (sin pedir tu fecha de nacimiento) y, según tu país, tu consentimiento para la analítica y los anuncios personalizados. Puedes decir que no con la misma facilidad que sí, y cambiarlo después en Ajustes.
- Si declaras que tienes menos de 13 años, el juego no arranca y te pide que lo instale un adulto.
- Puedes borrar tu cuenta y tus datos desde el juego o desde nuestra web.

## 3. Qué datos tratamos y para qué

| Datos | Quién los recoge | Para qué | Base legal |
|---|---|---|---|
| Un identificador anónimo de jugador (UID) y, si inicias sesión en Google Play Juegos, tu ID de jugador | Firebase Authentication (Google) y Google Play Juegos | Guardar tu partida en la nube, restaurar tus compras y proteger las recompensas | `[PENDIENTE: abogado]` (ejecución del servicio) |
| Tu partida guardada (progreso, semillas, comodines, ajustes) | El juego, en tu teléfono; copia en Google Play Juegos (Saved Games) | Que no pierdas tu progreso al cambiar de teléfono | `[PENDIENTE: abogado]` |
| Historial de compras (identificador de la compra, producto, fecha, importe y moneda) | Google Play Billing y nuestras funciones en la nube (Firebase) | Entregarte lo que compras, evitar fraudes y cumplir obligaciones fiscales | `[PENDIENTE: abogado]` (contrato y obligación legal) |
| Cómo usas el juego (niveles jugados, victorias, uso de comodines, rendimiento del juego) e identificador de instancia | Google Analytics para Firebase | Mejorar el juego y equilibrar la dificultad | **Tu consentimiento** (08 §2.3) `[PENDIENTE: abogado]` |
| Informes de fallos y estado del dispositivo (modelo, versión de Android, memoria) | Firebase Crashlytics | Encontrar y corregir errores | `[PENDIENTE: abogado]` (hoy, interés legítimo; 08 pregunta 6) |
| ID de publicidad del dispositivo, dirección IP aproximada e interacción con los anuncios | AppLovin MAX y las redes de anuncios que media: Google AdMob, Unity Ads, Meta Audience Network y Mintegral | Mostrarte anuncios, medirlos y prevenir fraudes. Si no das tu consentimiento, o si tienes entre 13 y 17 años, solo verás anuncios **no personalizados** | Tu consentimiento para los personalizados `[PENDIENTE: abogado]` |
| Verificación de integridad de la app y del dispositivo | Firebase App Check con Google Play Integrity | Proteger las compras, las recompensas y la eliminación de cuentas frente a abusos | `[PENDIENTE: abogado]` (interés legítimo) |
| Configuración remota del juego | Firebase Remote Config | Ajustar valores del juego sin publicar una versión nueva | `[PENDIENTE: abogado]` |
| Tu banda de edad y tus decisiones de consentimiento (fecha, versión del texto, país y decisión) | El juego, **solo en tu teléfono** | Aplicar el tratamiento que corresponde a tu edad y demostrar tu consentimiento | `[PENDIENTE: abogado]` (obligación legal) |

**Notificaciones:** los avisos del juego (por ejemplo, «el Reto del río está listo») son **locales**: los programa tu teléfono y no se envía ningún dato para ellos (08 §7; D-033 Q-M5). Puedes desactivarlos en Ajustes o en Android.

**País:** para saber qué reglas de privacidad aplicar usamos el país de tu tarjeta SIM o la región de tu teléfono, en el propio teléfono (D-033 Q-T3).

## 4. Qué no hacemos

- No pedimos ni guardamos tu nombre, correo, teléfono, dirección, fotos, contactos, mensajes ni ubicación precisa.
- No vendemos tus datos personales `[PENDIENTE: abogado — redacción por país, p. ej. «no vender/compartir» de EE. UU. desde la fase 6]`.
- No tenemos chat ni contenido de otros jugadores.
- No hay compras al azar: todo lo que compras o ganas se ve antes.

## 5. Menores de edad

- Tintalú está pensado para mayores de 13 años y no participa en el programa Familias de Google Play.
- Al abrir el juego por primera vez te preguntamos tu edad con una pantalla neutral, en tres bandas: menos de 13, de 13 a 17 y 18 o más. Esa respuesta se queda **solo en tu teléfono**, sin identificador, y puedes corregirla una vez desde Ajustes (05 §7).
- **Menos de 13:** el juego no se abre («Pide a un adulto que instale el juego»). No se crea sesión, no se activa la analítica con identificadores, ni Google Play Juegos, ni las compras, y los anuncios se marcan como dirigidos a menores.
- **De 13 a 17:** solo anuncios no personalizados y aptos para todo público, sin perfiles publicitarios. Las compras piden una confirmación doble en el juego y la autenticación de Google Play (D-033 Q-T4) `[PENDIENTE: abogado]`.
- Si crees que un menor de 13 años usó el juego, escríbenos a `[PENDIENTE: dueño — correo]` y borraremos sus datos.

## 6. Con quién compartimos datos

- **Proveedores que trabajan por nosotros:** Google (Firebase, Google Play Juegos, Google Play Billing, Play Integrity) y Unity (Unity IAP y Unity Authentication, que Unity IAP necesita `[PENDIENTE: dueño — confirmar qué datos trata; Q-B13-20]`). Tratan los datos para darnos su servicio.
- **Redes de anuncios:** AppLovin MAX y las redes que media (Google AdMob, Unity Ads, Meta Audience Network y Mintegral) reciben el ID de publicidad, la IP aproximada y datos de interacción con los anuncios, según tu consentimiento y tu edad. Cada una tiene su propia política de privacidad `[PENDIENTE: dueño — enlaces de cada red]`.
- **Autoridades:** solo cuando la ley lo exija.

Enlaces a las políticas de terceros: `[PENDIENTE: dueño]`.

## 7. Transferencias internacionales

Algunos proveedores guardan datos fuera de tu país. `[PENDIENTE: abogado — países, garantías y cláusulas por ley (LGPD cap. V, Ley 1581, LFPDPPP, Ley 29733)]`.

## 8. Cuánto tiempo guardamos los datos

| Datos | Plazo |
|---|---|
| Cuenta y partida en la nube | Mientras uses el juego o hasta que borres la cuenta |
| Analítica | `[PENDIENTE: dueño]` (la retención configurada en Firebase; Q-B13-19) |
| Informes de fallos | 90 días (retención de Firebase Crashlytics; 08 §13.3) |
| Historial de compras | Tras borrar la cuenta, se conserva **sin tu identificador** durante `[PENDIENTE: abogado — plazo fiscal]` (08 §11) |
| Banda de edad y consentimiento | En tu teléfono, hasta que borres la cuenta o desinstales el juego |

## 9. Tus derechos

Puedes pedir acceso, corrección, eliminación, portabilidad e información sobre tus datos, oponerte a un tratamiento y retirar tu consentimiento en cualquier momento, sin que eso afecte a lo ya tratado.

- **En el juego:** Ajustes › consentimiento (cambiar tus decisiones) y Ajustes › «Eliminar cuenta y datos».
- **Por correo:** `[PENDIENTE: dueño — correo]`. Te responderemos en `[PENDIENTE: abogado — plazo por ley]`.
- **Brasil (LGPD, art. 18):** también puedes acudir a la ANPD. **Colombia (Ley 1581):** a la Superintendencia de Industria y Comercio. **Perú (Ley 29733):** a la Autoridad Nacional de Protección de Datos Personales. **México (LFPDPPP):** derechos ARCO ante nosotros y ante la autoridad que corresponda `[PENDIENTE: abogado — nombres vigentes de las autoridades y vías]`.

## 10. Borrar tu cuenta y tus datos

Dos caminos con el mismo resultado (08 §11):
1. **En el juego:** Ajustes › «Eliminar cuenta y datos», con dos confirmaciones. Las compras no se reembolsan.
2. **En la web:** `[PENDIENTE: dueño — URL de la página de eliminación]`, con el ID de jugador que ves en Ajustes.

Qué se borra: tu cuenta (UID), tus datos en la nube y tus recompensas registradas, tu partida guardada en Google Play Juegos y los datos del juego en tu teléfono. Qué queda: el historial de compras, **sin tu identificador**, por obligación fiscal (sección 8). Los datos de analítica vinculados a tu cuenta se restablecen y no se guardan más allá de la retención configurada.

## 11. Seguridad

Los datos viajan cifrados (HTTPS). Las funciones que dan valor (compras, recompensas, eliminación de cuenta) exigen la verificación de integridad de Google Play. El juego no guarda claves secretas en el teléfono (08 §10.1).

## 12. Cambios a esta política

Si la cambiamos de forma relevante, te avisaremos en el juego y, cuando la ley lo pida, te volveremos a pedir el consentimiento (`consent_text_version`, 08 §9). Guardamos las versiones anteriores (08 §13.1 #23).

## 13. Contacto

`[PENDIENTE: dueño — razón social, dirección, correo de privacidad y DPO]`.

---

## Anexo A · Aviso de privacidad simplificado (México) · `[PENDIENTE: abogado]`

`[PENDIENTE: dueño — razón social]` es responsable del tratamiento de tus datos personales. Los usamos para que puedas jugar Tintalú, guardar tu partida, entregarte tus compras, prevenir fraudes y, si lo aceptas, para analítica y anuncios personalizados. Puedes negarte a las finalidades que necesitan tu consentimiento desde Ajustes del juego. Los compartimos con proveedores de servicio y redes de anuncios según se explica en el aviso integral. Consulta el aviso integral y cómo ejercer tus derechos ARCO en `[PENDIENTE: dueño — URL]`.

## Anexo B · Brasil (LGPD) · `[PENDIENTE: abogado brasileño]`

- Encarregado: `[PENDIENTE: dueño]`; canal LGPD: `[PENDIENTE: dueño]`.
- Bases legales por finalidad (art. 7) y tratamiento de datos de adolescentes (art. 14; ECA Digital): `[PENDIENTE: abogado brasileño]`.
- Segunda fuente de edad (Play Age Signals API) en la fase 5 (05 §7; 08 §13.1 #11): `[PENDIENTE: abogado brasileño]`.
