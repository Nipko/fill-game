# Pendientes del dueño: lo que depende de ti para llegar a la 1.0

Fecha: 2026-10-20. Lo reúne el integrador a partir de `docs/legal/marca.md`, `docs/dev/plan-tanda5.md` §3, `docs/dev/traspaso/README.md` (sección 6, paso 4), `docs/gdd/08-arquitectura-tecnica-publicacion.md` §13 y las decisiones D-033 a D-036. Ordenado por lo que más bloquea. Marca cada casilla al hacerla y anota la fecha.

La tanda 5 puede avanzar con implementaciones falsas de los SDK. Según D-034, **los borradores de publicación pueden prepararse ya** (`docs/dev/traspaso/codex-publicacion.md`); crear la app en Play Console y fijar el `applicationId` definitivo ya no esperan al dictamen (D-047, 2026-10-25: sin dictamen ni registro por ahora); el ID es `com.parallext.tintalu`. Este documento no confirma el avance de otros agentes.

## Resumen para revisar juntos

Última actualización: **2026-10-20**, con confirmación del dueño en el chat. «Me encargo yo» significa **a tu cargo**, no **hecho**; al cerrar tareas anotamos la fecha y el resultado, sin datos personales ni secretos.

- **Hecho y comprobado en Play Console (2026-10-20):** cuenta de tipo Organización, con datos de organización **AUTOMATION AI S A S**, país **Colombia (CO)** y nombre público de desarrollador **Automation AI**. El sitio `parallext.com` y los correos de contacto y desarrollador aparecen verificados. El perfil de cobros y la cuenta bancaria se revisaron después; ver su estado más abajo.
- **A tu cargo:** adquirir solo el dominio principal por ahora. `tintalu.com` sigue siendo el candidato del documento; falta confirmar la compra y el dominio exacto.
- **Aplazado:** dominios adicionales y defensivos. No bloquean preparar la web del dominio principal.
- **Entidad elegida por el dueño (2026-10-20): Colombia, AUTOMATION AI S A S.** El dueño informa que ya solicitó el D-U-N-S de Parallext LLC (EE. UU.), aún pendiente. Decide avanzar con la S.A.S. mientras tanto. No se cambia la identidad ni el país de Play. Prefiere Mercury si fuera compatible; al estar en EE. UU. y a nombre de la LLC, no se vinculará al perfil colombiano: la alternativa autorizada es la cuenta colombiana de la S.A.S., pendiente de añadir y verificar por el dueño.
- **Perfil comercial creado (2026-10-20):** el dueño pulsó Enviar; comprobado después en la consola: panel de ingresos y configuración a nombre de AUTOMATION AI S A S, con «Agregue una forma de pago para recibir sus ingresos». Formulario preparado con web `https://parallext.com/`, soporte `support@parallext.com`, categoría Software de computadoras y descriptor `AUTO AI`. **Cuenta Bancolombia añadida y marcada Principal; estado comprobado: En revisión (2026-10-20).** El dueño indica que completó el paso documental; la verificación aún no está aprobada. No se ha inspeccionado el documento finalmente enviado. El formulario bancario exige banco en Colombia y moneda USD; pide titular, banco, SWIFT/BIC, número de cuenta y, si el banco lo requiere, intermediario estadounidense e instrucciones FFC/FBO. El dueño debe obtener las instrucciones de recepción internacional de su banco e introducir los datos directamente. Sin datos bancarios ni fiscales sensibles en el repositorio.
- **Programa de cargos del servicio del 15% (2026-10-20):** creado y comprobado el grupo `AUTOMATION AI S A S`, con Automation AI como cuenta principal. El dueño confirma que también posee una cuenta personal de Play. El formulario distingue cuentas propiedad de la entidad legal y cuentas con características de marca similares: falta confirmar cómo corresponde declarar la personal y su identificador. No se han enviado solicitudes ni guardado respuestas a esa declaración. Por petición del dueño, se aplazan la asociación de cuentas y la inscripción al 15%. Se canceló el formulario sin guardar respuestas ni enviar solicitudes; el grupo ya creado permanece con solo la cuenta principal. Inscripción y aceptación de condiciones pendientes.

| Bloque | Lo que necesitamos de ti | Lo que pueden preparar los agentes | Detalle |
|---|---|---|---|
| Marca e identidad | Sin dictamen ni registro por ahora (D-047); `applicationId` `com.parallext.tintalu` | Reunir antecedentes y preguntas para el abogado | §1 |
| Presencia pública | Comprar dominio principal, reservar usuarios y aportar datos legales; alojamiento AWS ya decidido | Preparar web y textos con campos pendientes | §2 |
| Plataforma | Cuenta empresarial y perfil comercial creados; banco en revisión; INTERNET aprobado; quedan SDK, firma y configuración de servicios | Medir el build y preparar propuestas de integración y CI | §3 |
| Arte e idiomas | Gestionar artista, asesor cultural y revisión nativa | Consolidar encargos y material de revisión; integrar entregas | §4–§5 |
| Pruebas | Facilitar teléfono, validar sensaciones y curación; gestionar testers y QA externa | Preparar build, guion de prueba e informes | §6 |
| Legal y lanzamiento | Gestionar revisión legal, firma, cuentas y publicación; decidir precios | Preparar borradores, inventario de licencias y checklist | §7–§8 |

### Simplificaciones propuestas para decidir

Estas propuestas organizan el trabajo; **no cambian requisitos ni decisiones del GDD**.

1. **Dominios:** decidido por el dueño: solo el principal por ahora; el resto queda aplazado (§2). Las redes siguen pendientes, sin bloquear los borradores.
2. **Cuentas y SDK:** separar las aprobaciones técnicas de la creación manual de cuentas. Pedirte cada cuenta cuando la integración vaya a necesitarla, con los datos concretos que debes configurar (§3).
3. **Tamaño del AAB:** que el equipo prepare una medición y una recomendación antes de pedirte elegir entre Addressables, PAD, fuentes o arte a 1× (§3).
4. **Arte y revisión cultural:** agrupar los encargos en un paquete por profesional, con entregables de la 1.0 separados de los opcionales y de la 1.1 (§4–§5).
5. **Pruebas en teléfono:** reunir visuales, audio, rendimiento y Tinaja en un guion de sesión; mantener las comprobaciones y la QA externa exigidas (§6).
6. **Decisiones ya tomadas:** edad 13+ y compra de 13–17 siguen las decisiones vigentes; su revisión legal no implica volver a diseñarlas (§7).

### Próximas respuestas que sí necesitamos de ti

| Decisión | Propuesta de trabajo | Estado |
|---|---|---|
| Nombre del estudio y entidad publicadora | El dueño elige continuar con AUTOMATION AI S A S, Colombia. Nombre público actual: Automation AI; conservarlo o cambiarlo sigue por confirmar. Desde D-041 el desarrollador y responsable es Parallext, LLC; el `applicationId` definitivo es `com.parallext.tintalu` (D-047) | Entidad y país decididos; nombre público por confirmar |
| Dictamen de marca | Confirmar si ya lo estás gestionando; los antecedentes están en `docs/legal/marca.md` | Pendiente |
| SDK de §3 | Revisar las aprobaciones de la integración prevista. El permiso `INTERNET` ya fue aprobado por separado el 2026-10-20 (D-036); no implica aprobación de SDK | SDK pendientes; permiso resuelto |
| Arte, asesor e idiomas | Confirmar si ya tienes profesionales o contactos; los agentes pueden consolidar primero los encargos existentes | Pendiente |

**Trabajo preparable sin nuevas decisiones de producto:** borradores de publicación con campos pendientes; propuesta de tamaño basada en mediciones; paquetes de revisión para artista y asesor; guion de pruebas en teléfono. Este reparto no significa que esos entregables ya estén hechos ni que se haya activado a otro agente.

---

## 1. Marca y nombre (lo que más bloquea)

| ✓ | Tarea | Detalle | Fuente | Qué desbloquea |
|---|---|---|---|---|
| [—] | Contratar al abogado de marcas (aplazado por D-047: el registro, cuando haya resultados o si aparece un conflicto) | (1) Búsqueda en el INPI de Brasil de TINTALÚ, TINTALU, TINTAL\* y TINTA LU\* en las clases 9, 28 y 41 (y la 2: en Brasil «tinta» es pintura); dictamen de registrabilidad. Unos R$ 500-1.500 [E], 1-2 semanas. (2) Dictamen en México frente a EL TINTAL (IMPI reg. 2586508, clase 41) y TINTALAIRE. (3) Estrategia frente a TINTA en EE. UU. | `docs/legal/marca.md` §2 y §5 | Todo lo siguiente |
| [—] | Recibir el dictamen (no se pide, D-047) | Cubre también «TiTino». Si sale favorable, se fija el `applicationId`; si no, planes B (`marca.md` §1.3) | `marca.md` §6 | Crear la app en Play Console (D-034), la pista interna y el build de release |
| [x] | Decidir el nombre del estudio y su país (Parallext, D-041; `com.parallext.tintalu`, D-047) | Es el `<estudio>` de `com.<estudio>.tintalu`, que no cambia tras la primera subida y es público. Sin ninguna forma de «Amazon». El país decide dónde se presenta la marca | 08 §13.1 #1; `marca.md` §3 | `applicationId`, nombre de desarrollador, solicitud base |
| [ ] | Presentar la solicitud de marca | Solicitud base en las clases 9 y 41 en el país del estudio (y, si es miembro del sistema de Madrid, presupuesto para CO, MX, BR y US) | `marca.md` §3 y §5 | Protección del nombre |

---

## 2. Dominios, web y redes

| ✓ | Tarea | Detalle | Fuente |
|---|---|---|---|
| [ ] | Adquirir el dominio principal — a cargo del dueño | Confirmado el 2026-10-20: solo el principal por ahora. Candidato: `tintalu.com`; compra y dominio exacto aún sin confirmar. Se usará para web, política, eliminación de cuenta, `app-ads.txt` y enlaces de la ficha | Chat del dueño, 2026-10-20; `marca.md` §4.1; 08 §13.1 #2 |
| Aplazado | Dominios adicionales y defensivos | `tintalu.app`, `tintalu.co`, `tintalu.com.br`, `tintalu.games`, `tintalu.net`, `tintaloo.com`, `tintalú.com`. Fuera de las compras actuales por decisión del dueño del 2026-10-20 | Chat del dueño; `marca.md` §4.1 |
| [x] | Elegir el alojamiento de la web, con HTTPS | Decidido en D-035: AWS S3 + CloudFront. Falta preparar y desplegar; el dueño despliega. Dominio público tras el dictamen; dev usa dominios de AWS (Q-AWS-9). Incluye política, términos, eliminación de cuenta y `app-ads.txt` | D-035 y Q-AWS-9; 08 §11 y §13.1 #2 |
| [ ] | Reservar los usuarios en redes | Instagram, TikTok, YouTube, X y Facebook | `marca.md` §4.2 |
| [ ] | Datos legales del responsable | Nombre o razón social, dirección, correo de contacto y de privacidad, encargado (DPO) y canal para la LGPD de Brasil | 08 §13.1 #2 |

El presupuesto anterior de cuatro dominios deja de ser el presupuesto de compra actual. El coste del principal se confirmará al adquirirlo.

---

## 3. Cuentas, permisos y decisiones técnicas

| ✓ | Tarea | Detalle | Fuente | Qué desbloquea |
|---|---|---|---|---|
| [x] | Cuenta de desarrollador de Google Play como empresa | Comprobado en Play Console el 2026-10-20: Organización; AUTOMATION AI S A S; Colombia; nombre público Automation AI. Perfil comercial creado; banco principal añadido, En revisión por Google. La app de Tintalú **no** se crea hasta el dictamen (D-034) | Play Console › Cuenta de desarrollador › Acerca de ti, 2026-10-20; 08 §13.1; D-034 | Registro de cuenta resuelto; creación de Tintalú sigue bloqueada por la marca |
| [x] | Aprobar el permiso `INTERNET` | Aprobado expresamente por el dueño el 2026-10-20 (D-036). Unity ya lo añade automáticamente según el estado documentado; no se cambió la configuración. No aprueba SDK ni activa servicios | Chat del dueño, 2026-10-20; D-036 | Resuelve el visto bueno del permiso para los servicios de red previstos |
| [ ] | Revisar las aprobaciones restantes de SDK del cliente | MAX con adaptadores y UMP, Unity IAP 5.4.4 y Play Games 2.3.0 según el plan; contrastar las aprobaciones existentes antes de pedirlas otra vez. D-035 fija Firebase 13.17.0 para Auth, Analytics, Crashlytics y Remote Config, con App Check para integridad (Q-AWS-4). Firebase Functions sale: las funciones propias y el libro mayor van en AWS. La arquitectura elegida no acredita que la integración esté terminada | `plan-tanda5.md` §3; D-035 y Q-AWS-4 | Integración real de los servicios |
| [ ] | Crear las cuentas de Firebase, AppLovin MAX, AdMob y Play Games Services | Más las redes de la mediación (Unity Ads, Meta, Mintegral) para `app-ads.txt` | 08 §13.1 #2 y #16 | Integrar los SDK, `app-ads.txt`, Data Safety real |
| [ ] | Keystore, Play App Signing y secretos del CI (aplazado por el dueño el 2026-10-25 hasta la primera subida; unos 15-20 min) | El workflow s32 (`.github/workflows/release.yml`) ya existe y espera a esto. Genera tú la clave de subida con `keytool` y crea en GitHub los secretos de repositorio `UNITY_EMAIL` y `UNITY_PASSWORD` y el Environment `release` (solo `main`; revisor obligatorio opcional) con los secretos `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_NAME` y `ANDROID_KEYALIAS_PASS`. Solo tras el dictamen, crea la variable de repositorio `AF_RELEASE_APPLICATION_ID` (recomendado `com.parallext.tintalu`, D-041). Play App Signing con la clave de firma generada por Google. Pasos en `docs/dev/release.md` | 08 §13.1 #3; s31-s32; D-034 | Lanzar a mano el release firmado (el AAB queda como artefacto; la subida a Play la haces tú) |
| [ ] | Revisar una propuesta medida de tamaño del AAB | Primero el equipo debe actualizar el peso y el desglose, estimar SDK y arte pendientes y recomendar una opción. Referencia documental: unos 52 MB frente al tope de 80 MB; no es una medición nueva. El dueño decide sobre esa propuesta | `plan-tanda5.md` §3; README §1 «Tamaño» | Paquetes 701+, fondos remotos, Unity Localization (Q-T2) |
| [x] | Aprobar el merge del CI (`ci.yml`, s30) | Ya aprobado por el dueño en el añadido de D-033 sobre B10. Quedan a su cargo los secretos de GitHub; la autorización no demuestra que el merge o la configuración de secretos se hayan completado | D-033, añadido «merge de B10 y versión de coverlet» | Autorización de merge resuelta |

---

## 4. Arte (artista 2D freelance)

Todo el arte de hoy es provisional, hecho por código. Los encargos ya están escritos:

| ✓ | Tarea | Documento | Contenido |
|---|---|---|---|
| [ ] | Contratar al artista | — | Entrega en PNG/PSD por Git LFS; los agentes lo integran |
| [ ] | Enviar el encargo del Mapa y los fondos | `docs/art/encargo-mapa-del-rio.md` | Fondos por parada, kit de cada mundo, héroes de cada sitio, sprites de ambiente, segundo juego de follaje y piezas del Mapa del río (incluye coste y peso orientativos, §7) |
| [ ] | Enviar el encargo de TiTino, Frutas y la Tinaja | `docs/art/encargo-titino-frutas-tinaja.md` | TiTino (protagonista), las 14 escenas de Frutas del río y la Tinaja |
| [ ] | Encargar las skins de las cuias | 06 §4.5; 07 §4.6 | Las de los hitos 40, 80 y 140 y las del Mercado (hoy, provisionales) |
| [ ] | Encargar los assets de tienda | 07 §10; 08 §13.1 #5 | Icono 512 (4 conceptos en 07 §10.1; recomendado «A. Cuia que vierte»), gráfico 1024 × 500, 8 capturas por idioma (07 §10.2), vídeo de 30 s (§10.3) y creativos de anuncios (§10.4) |
| [ ] | (1.1) El elenco de unas 50 especies | D-033, Q-N6 | La 1.0 sale con los 11 miquitos |
| [ ] | (Opcional, mientras tanto) Arte provisional mejor | `docs/art/prompts-chatgpt.md` | Prompts listos para ChatGPT: fondos, follaje, botones, paneles, Capi, frutas y héroes provisionales |

---

## 5. Asesor cultural y revisión de idiomas

| ✓ | Tarea | Detalle | Fuente |
|---|---|---|---|
| [ ] | Contratar al asesor cultural | Revisa nombres y textos de fauna, flora, pueblos y lugares; hasta su visto bueno quedan «pendiente de revisión» | `CLAUDE.md`, «Humanos» |
| [ ] | Nombres y textos `pendiente_asesor` | Entre ellos «sauim-de-coleira», los Datos del río, las frases de TiTino (8 nuevas de DC25), `map_l10_intro`, `tutorial_morpho`, los `hint_*` con fauna, `tutorial_brote_n2`, `tutorial_flor`, la embaúba de la ventanita y las líneas 17-20 del cacao y del arazá (8 líneas por redactar) | README §6 paso 4 |
| [ ] | Audio | 24 piezas con `advisor_review` pendiente (17 con especie o región sustituta, no amazónica, entre ellas el boto) | README §6 paso 4 |
| [ ] | Preguntas de la traducción | T2-Q8 a T2-Q11: Amazonas o Amazonía, Manaus o Manaos, los textos del glosario (00 §10) y los nombres de los comodines | `docs/dev/traspaso/spark-estado.md` |
| [ ] | Otras del asesor | F7, F11 y Q17 (fauna y guion de la intro) | README §6 paso 4 |
| [ ] | Revisión nativa de inglés y portugués de Brasil | Las traducciones de hoy son de IA (D-022, pregunta 51 b) | D-022 |

---

## 6. Pruebas y QA

| ✓ | Tarea | Detalle | Fuente |
|---|---|---|---|
| [ ] | Conseguir un teléfono de gama baja (clase A) | Moto G (2x/3x de 2022-23), Galaxy A0x/A1x o Redmi 9/10, con Mali-G52 o Adreno 610. Mide fps, memoria, el shader del líquido, el factor de Morpho y TalkBack | 08 §14.1; pregunta 44 |
| [ ] | Dar por buena la Tinaja en el 323 | Depende del factor de Morpho en ese teléfono; hasta entonces 301-700 salen con la variante b | README, «El 323 sale sin Tinaja» |
| [ ] | Prueba visual de 30 min en el teléfono (s28) | — | `CLAUDE.md` |
| [ ] | Escuchar el audio en el teléfono | Notas de fruta, chorro, capas de la música tras varios bucles y que en `amb_w01_fauna_02` no se entienda ninguna voz (pregunta 64) | README §6 paso 4 |
| [ ] | Curación de los mundos 6-9 | Informes `tools/out/curation_w06.md` a `curation_w09.md` (en 7-9: 122 niveles y 13 retos sin etiqueta); decides en `curation_*.json` (D-033, Q-N8) | README §6 paso 4 |
| [ ] | Curación a mano de 11-200 | Comprobaciones #12-#13 de 03 §17 | `plan-tanda5.md` §3 |
| [ ] | Contratar QA humana externa de 201-700 | Unas 40 h; la pide la compuerta de 09 §1.1 #7 | `plan-tanda5.md` §3 |
| [ ] | Grabaciones de Magic Sort (#13) | Referencia de tiempos y sensaciones | README §6 paso 4 |
| [ ] | Prueba cerrada en Play (más adelante) | 20-50 testers de CO, PE y MX durante 1-2 semanas | 08 §13.1 #20 |

---

## 7. Legal (además de la marca)

| ✓ | Tarea | Detalle | Fuente |
|---|---|---|---|
| [ ] | Revisar los textos legales | Política de privacidad, términos y página de eliminación de cuenta (los redacta Codex como borrador) | 08 §13.1 #2 y #7 |
| [ ] | Revisar la edad y el consentimiento | Bandas `<13` (bloqueado en la 1.0), `13-17` y `18+`; consentimiento por país | 08 §13.1 #11; 05 §7 |
| [ ] | Validar implementación y revisión legal de la compra de 13-17 | El diseño ya está decidido: doble confirmación en el juego + autenticación de Google Play, sin PIN propio. Queda comprobar su implementación y revisión; no hace falta volver a aprobar el mismo diseño | D-033, Q-T4 |
| [ ] | ECA Digital (Brasil) | Consulta al abogado brasileño (art. 8) en la compuerta de la fase 5 | 08 §13.1 #11 |
| [ ] | Licencia de HydroRIVERS | Leer su acuerdo (en hydrosheds.org; no viene en la descarga), fijar el texto de atribución y decidir dónde va (aún no hay pantalla de créditos) | README §6 paso 4 |

---

## 8. Negocio (sin prisa: se revisan en la fase que las necesite)

| ✓ | Tarea | Fuente |
|---|---|---|
| [ ] | Reparto entre compras y anuncios, presupuesto del soft launch, beta, atribución, iOS y otras «Preguntas abiertas» antiguas de los capítulos 00-09 sin recomendación | README §6 paso 4 |
| [ ] | Precios por país (BR, MX, CO, PE, AR, CL, EC), a mano en Play Console | 08 §13.1 #14; 05 §4.2 |

---

## Orden sugerido (propuesta para revisar)

1. **Hecho (D-047):** `applicationId` `com.parallext.tintalu`; el dictamen ya no se pide. Entidad y país resueltos: AUTOMATION AI S A S, Colombia. La cuenta empresarial y el perfil comercial ya están creados; el banco está en revisión. El dueño adquirirá solo el dominio principal por ahora.
2. **Mientras llega el dictamen:** preparar los borradores de publicación (D-034), preparar la web en AWS (D-035) y organizar los encargos de arte, asesoría y pruebas. Revisar las aprobaciones restantes de SDK con el equipo y pedirle una propuesta medida para el tamaño del AAB; INTERNET ya aprobado (D-036).
3. **Con el ID ya fijado** (`com.parallext.tintalu`, D-047): crear la app en Play Console y la variable `AF_RELEASE_APPLICATION_ID`. Crear las cuentas de servicios según las dependencias de cada integración.
4. **Antes de la prueba cerrada:** completar web y revisión legal, arte y revisiones de contenido, curación y QA, firma y secretos del CI. Las compras defensivas, la 1.1 y las decisiones de negocio se revisan en su fase, sin darlas por canceladas.
