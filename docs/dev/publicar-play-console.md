# Publicar la 1.0 en Play Console: guía paso a paso del dueño

- **Para quién:** el dueño, con Play Console abierta. Los agentes no suben nada a Google Play ni tocan las consolas (`CLAUDE.md`, reglas duras 5 y 6); esta guía solo reúne lo que tienes que hacer y qué contestar.
- **Fecha:** 2026-10-30, en la rama `tanda6/textos-publicacion` (la web en firme en 041b18ff, sobre b81abca9 de `integracion-fase1-fase2`), con los textos públicos en firme (D-064). Antes: 2026-10-28, en 7ca60990.
- **Plan de lanzamiento (D-059):** la pista interna, **solo contigo** como único tester, para probar las compras y Play Games con la app instalada desde Play (PAD no aplica en la 1.0: §15); después, la **misma versión** se promociona a **producción al 100 %**, con la **publicación administrada** activada antes de enviar nada a revisión. Sin prueba cerrada ni abierta y sin escalonado. En todos los países de Play salvo Rusia, Bielorrusia, Vietnam e Indonesia (D-057).
- **De dónde sale cada respuesta:** se cita el § del GDD o la decisión (D-xxx) de `docs/dev/decisiones.md`. Los textos largos ya están en el repositorio y se copian de ahí: fichas en `docs/store-listing/<idioma>/ficha.md`, Data Safety en `docs/store-listing/data-safety.md`, IARC en `docs/store-listing/iarc.md`. Esta guía repite las respuestas para que no tengas que abrirlos; si alguna vez no coinciden, mandan esos archivos y avisas para corregir esta guía.
- **Marcas:** [NV] = un nombre de menú, botón, límite o comportamiento de Play Console (o de Firebase o Google Cloud) que el repositorio no puede comprobar: contrástalo con lo que ves en pantalla. [P] = propuesta de esta guía, no decisión. [E] = estimación. Lo que quedaba para el abogado lo cerró la investigación propia (D-057) o queda como riesgo aceptado.
- **Nunca** pegues en el chat, en un issue ni en el repositorio una clave JSON, el keystore, una contraseña ni el `google-services.json`.

## Lista de casillas, en orden

Marca cada una al hacerla. Entre paréntesis, la sección de esta guía.

**Antes de abrir Play Console**
- [x] Perfil de pagos verificado (banco aprobado) en la cuenta de AUTOMATION AI S.A.S (§0.1; comprobado el 2026-10-30)
- [x] Bloqueante técnico resuelto: la URL del backend de prod compilada en el cliente (§0.2)
- [ ] Licencia de Parallext, LLC a AUTOMATION AI S.A.S firmada (§0.3)
- [x] Decidido si la Liga del río sale encendida en producción: **sí** (§0.4; D-064)
- [ ] Plantilla de Remote Config de prod publicada en Firebase `tintalu` (§0.5)
- [ ] Unity Ads en los partners de los mensajes de RGPD y de EE. UU. de AdMob, y Parallext en la organización de Unity (§0.6)
- [x] Capturas de pantalla listas: las 01-08 de cada idioma (§0.7)
- [ ] Regresión de release en el teléfono y release firmado `1.0.0` desde `main` (§0.8)

**En Play Console**
- [ ] Crear la app (§1)
- [ ] Publicación administrada activada; categoría, etiquetas y datos de contacto (§2)
- [ ] Política de privacidad (§3)
- [ ] Acceso a la app, anuncios e ID de publicidad (§4)
- [ ] Ficha principal en-US y sus cuatro traducciones (§5)
- [ ] Testers de licencia y lista de la pista interna (§6.1 y §6.2)
- [ ] Primera versión en la pista interna, con Play App Signing (§6.3)
- [ ] Huellas SHA en Firebase, App Check con Play Integrity y proyecto de Play Games con Saved Games (§7)
- [ ] Segunda versión interna con el `APP_ID` de Play Games (§7.5)
- [ ] Seguridad de los datos (§8)
- [ ] Clasificación de contenido IARC (§9)
- [ ] Los 13 productos con sus precios (§10)
- [ ] Las dos cuentas de servicio cargadas en AWS y la de Play invitada en Play Console (§11)
- [ ] Retención de Analytics comprobada (§12)
- [ ] Público objetivo y contenido (§13)
- [ ] Noticias, gobierno, finanzas, salud y demás declaraciones (§14)
- [ ] Prueba completa en la pista interna (§15)
- [ ] Países y regiones de producción (§16)
- [ ] Promoción a producción al 100 %, envío a revisión y «Publicar cambios» (§17)
- [ ] Lo de después de publicar (§18)

---

## 0. Antes de abrir Play Console

### 0.1 El perfil de pagos está verificado

- **Comprobado el 2026-10-30 en Play Console:** cuenta de organización «Automation AI» (ID 8269379304370913565, correo `support@parallext.com`); forma de pago, transferencia bancaria a la cuenta ••••056 de AUTOMATION AI SAS; sin avisos. (El 2026-10-20, en `docs/dev/pendientes-del-dueno.md`, la cuenta estaba «En revisión»: ya no.)
- Cobra AUTOMATION AI S.A.S (D-048).
- **Recomendado antes de publicar:** inscribirte en el programa de cargos del 15 % (lo haces tú: hay que aceptar sus términos en Play Console [NV ruta]). Quedó aplazado (`pendientes-del-dueno.md`, resumen); hacerlo antes del lanzamiento te ahorra la diferencia desde el primer día [NV: que el 15 % solo se aplique a partir de la inscripción].

### 0.2 La URL del backend de prod en el release: resuelto

- **Resuelto:** `Assets/_Project/Scripts/Backend/Unity/BackendSetup.cs:31` ya tiene `BackendEndpoints.Prod = "https://vsublt4swh.execute-api.sa-east-1.amazonaws.com"`, la salida `ApiUrl` del stack de prod (D-035, añadido del 2026-10-27; no es secreta). El release verifica compras, restaura, borra la cuenta desde la app y habla con la Liga de prod.
- Antes estaba vacía («hasta F5-1») y el release usaba `UnavailableBackendClient`; ya no hace falta encargar nada.

### 0.3 Licencia de Parallext a la editora

- D-057 (añadido del 2026-10-27), riesgo R9: **firmar la licencia de Parallext, LLC a AUTOMATION AI S.A.S antes de publicar** (el DDA de Google exige que quien publica tenga derecho a monetizar; D-048).

### 0.4 La Liga del río: encendida en producción (D-064)

- **Decidido el 2026-10-30 (D-064, la recomendación de D-035, añadido del 2026-10-28):** la Liga sale **encendida** en producción. Su interruptor `live_league_enabled` vale `false` por defecto (08 §9), así que lo enciendes en la plantilla de Remote Config de prod (§0.5). El backend de prod ya existe (D-035, añadido del 2026-10-27).
- Por eso la ficha lleva la línea de la Liga (con el avatar), Data Safety declara sus filas y el IARC contesta «sí» a la interacción y al país (§5, §8 y §9). La política de privacidad de la web ya la describe así.

### 0.5 Remote Config de prod

- La plantilla `tools/out/remote_config_template_prod.json` (250 de las 256 claves de 08 §9) la publicas tú en el proyecto `tintalu` (D-035, añadido del 2026-10-27; `docs/dev/remote-config-analytics.md` §9). Publicarla no cambia el comportamiento: son los defaults compilados. Hazlo antes del lanzamiento para poder frenar sistemas sin publicar otra versión (`biz_feature_*`, `mech_enabled_*`; `docs/dev/release.md` §4 bis, paso 7).
- Como la Liga sale encendida (§0.4; D-064), cambia ahí `live_league_enabled` a `true` antes de publicar.

### 0.6 Pendientes de AdMob y Unity Ads

Del añadido de D-049 del 2026-10-27:
- añadir **Unity Ads** a los partners de los mensajes de **RGPD** y de **EE. UU.** en AdMob (Privacidad y mensajes) [NV]; sin esto, en el EEE el consentimiento no cubre a Unity;
- poner **Parallext, LLC** como nombre de la organización de Unity, con su dirección y los datos de cobro.

### 0.7 Material gráfico

- **Icono 512 × 512:** `docs/art/marca-oficial/v2/out/png/google-play-512.png` (D-045).
- **Gráfico de funciones 1024 × 500:** `docs/store-listing/graficos-v2/A/feature-graphic.png` (D-065, concepto A: un fotograma real de la intro de la marca sobre un amanecer en el río hecho con IA). El anterior, `docs/art/marca-oficial/v2/out/png/feature-graphic.png`, queda de respaldo.
- **Capturas de teléfono:** las del **concepto A** (D-065, elegido por el dueño el 2026-10-30), en `docs/store-listing/graficos-v2/A/<idioma>/` (`en/`, `es-419/` y `pt-BR/`): 8 PNG de 1080 × 1920 con la pantalla real del teléfono, el fondo de IA y el rótulo ya dibujado (textos en §6 de cada ficha). **Sube las 01-08 en cada idioma, en ese orden.** es-ES usa las de es-419 y pt-PT las de pt-BR. Las de `docs/store-listing/graficos/` quedan de respaldo (sin la 09 ni la 10, D-064). Los conceptos B y C (`graficos-v2/B/`, `C/`) son para una prueba de ficha tras el lanzamiento, no para subirlos ahora.
- **Vídeo (opcional):** una URL de YouTube; 07 §10.3. No usa la música CC0 (`docs/store-listing/README.md` §3 bis).

### 0.8 Regresión y release firmado

`docs/dev/release.md` §4 bis, pasos 1 y 2:
1. La regresión de release de 60 casos (`docs/qa/regresion-release.md`) en el teléfono, con el ensayo de release del mismo commit de `main` (`pwsh -NoProfile -File scripts/unity-build.ps1 -Method Release -Rehearsal -Apk`).
2. **Actions › Release firmado › Run workflow**, rama `main`, `versionName` `1.0.0` y `versionCode` vacío (da **10000**; el AAB del run 40630794359 no se subió, así que 10000 sigue libre). Descarga el artefacto `android-aab-release-1.0.0-10000`.
- El keystore de subida, el Environment `release`, sus cinco secretos y la variable `AF_RELEASE_APPLICATION_ID` = `com.parallext.tintalu` ya existen (primer release firmado del 2026-10-27, `release.md` §7). **Anota la línea «SHA-256 del certificado de subida»** que imprime el job `preflight`: la comparas en §7.1.

---

## 1. Crear la app

Play Console › Todas las apps › **Crear app** [NV]:

| Campo | Respuesta | Fuente |
|---|---|---|
| Nombre de la app | **`Tintalú: Water Sort Puzzle`** (26 caracteres) | glosario §1.3; `en/ficha.md` §1 |
| Idioma predeterminado | **Inglés (Estados Unidos) – en-US** | D-057, P9 (a); 08 §13.1 #5 |
| App o juego | **Juego** | 08 §13.1 #1 |
| Gratis o de pago | **Gratis** (con compras integradas y anuncios). No se puede cambiar a de pago después [NV] | 08 §13.1 #1 |
| Declaraciones | Políticas del programa para desarrolladores y leyes de exportación de EE. UU.: las aceptas tú | — |

- El `applicationId` no se escribe aquí: lo fija la primera subida (§6.3). Es **`com.parallext.tintalu`** (D-047) e **inmutable** tras esa subida.
- El nombre público del desarrollador es de la cuenta, no de la app: hoy «Automation AI» (`pendientes-del-dueno.md`). Las fichas dicen «Parallext, LLC»: ver la duda 1 de §19.

## 2. Publicación administrada, categoría, etiquetas y datos de contacto

### 2.1 Publicación administrada (D-059, punto 3)

- **«Resumen de publicación» › «Publicación administrada»: activada**, ahora, antes de crear ninguna versión.
- Así, cuando Google apruebe la versión de producción, no sale sola: sale cuando pulses **«Publicar cambios»** (§17).
- [NV] Con la publicación administrada activada, también los cambios de la pista interna, de la ficha y de los formularios esperan a «Publicar cambios». Si una versión interna no aparece para instalar, mira «Resumen de publicación».

### 2.2 Categoría y etiquetas

Play Console › Crecer › Presencia en la tienda › **Configuración de la tienda** [NV]:

| Campo | Respuesta | Fuente |
|---|---|---|
| Tipo de app | Juego | 08 §13.1 #1 |
| Categoría | **Puzle** («Puzzle») [NV nombre exacto de la lista] | `es-419/ficha.md` §1 |
| Etiquetas (de la lista de Play, hasta 5 [NV]) | [P] las que más se acerquen a: puzle de ordenar o clasificar colores, casual, relajante, sin conexión, un jugador. El GDD no fija etiquetas; las palabras clave de las fichas (§4 de cada una) guían la elección | Fichas §4 |

### 2.3 Datos de contacto

| Campo | Respuesta | Fuente |
|---|---|---|
| Correo electrónico (público) | **`support@tintalu.com`** | D-041, añadido del 2026-10-25 |
| Teléfono | Vacío [P] (opcional en Play [NV]) | — |
| Sitio web | **`https://tintalu.com/`** (es también donde Play y AdMob buscan `app-ads.txt`) | D-041; D-044, añadido del 2026-10-25; 08 §13.1 #23 |

## 3. Política de privacidad y eliminación de cuenta

Las URL salen de `infra/web/src/site.ts` y la web está publicada en `tintalu.com` (D-044, añadido del 2026-10-25).

| Página | en (la que se declara) | es-419 | pt-BR |
|---|---|---|---|
| Política de privacidad | **`https://tintalu.com/en/privacy/`** | `https://tintalu.com/privacidad/` | `https://tintalu.com/pt-br/privacidade/` |
| Eliminación de cuenta | **`https://tintalu.com/tintalu/en/delete-account/`** | `https://tintalu.com/tintalu/eliminar-cuenta/` | `https://tintalu.com/tintalu/pt-br/excluir-conta/` |
| Términos | `https://tintalu.com/en/terms/` | `https://tintalu.com/terminos/` | `https://tintalu.com/pt-br/termos/` |

- Contenido de la app › **Política de privacidad** [NV]: pega **`https://tintalu.com/en/privacy/`** (D-057, P9 (a): con en-US por defecto se declara la inglesa; 08 §13.1 #5 y #7).
- La URL de eliminación se declara dentro de Seguridad de los datos (§8.1, pregunta 5).
- Antes, abre las dos URL y comprueba que cargan (200) y que la página de borrado acepta un código (`https://tintalu.com/config.json` → `"deleteEnabled":true`; `infra/README.md`).

## 4. Acceso a la app, anuncios e ID de publicidad

Contenido de la app (Política › Contenido de la app [NV]):

### 4.1 Acceso a la app (08 §13.1 #9)

- **«Todas las funciones o la mayoría están disponibles sin restricciones de acceso»** [NV texto]: no hay inicio de sesión obligatorio; la cuenta es anónima y se crea sola (08 §11).
- Si Play pide instrucciones de todos modos [P]: «No login needed. The Market opens from the home screen; purchases use Google Play. The River League unlocks after level 41.» (la Liga sale encendida, §0.4).

### 4.2 Anuncios (08 §13.1 #8)

- **«Sí, mi app contiene anuncios»** (AdMob directo con Unity Ads en su mediación, D-049 y su añadido del 2026-10-27).

### 4.3 ID de publicidad

- **¿Usa tu app el ID de publicidad?** **Sí** (permiso `com.google.android.gms.permission.AD_ID`, aprobado en el añadido de D-049 del 2026-10-25 y confirmado en D-055, punto 3).
- **Finalidad:** **Publicidad o marketing** (`docs/dev/anuncios-admob.md` §10, punto 3). Data Safety añade «prevención de fraude, seguridad y cumplimiento» para el mismo identificador (§8.2): si el formulario de esta sección también la ofrece, márcala para que coincidan [P].
- Firebase Analytics **no** lo lee (`google_analytics_adid_collection_enabled` = false; 08 §13.3).

## 5. Ficha principal (en-US) y traducciones

Crecer › Presencia en la tienda › **Fichas de Play Store** › Ficha principal [NV]. Idioma por defecto **en-US**; traducciones **es-419**, **es-ES** (copia de es-419), **pt-BR** y **pt-PT** (copia de pt-BR) (D-057, P9 (a); 08 §13.1 #5).

| Campo (límite) | en-US | es-419 y es-ES | pt-BR y pt-PT | De dónde copiarlo |
|---|---|---|---|---|
| Nombre (30) | `Tintalú: Water Sort Puzzle` (26) | `Tintalú: Ordenar Agua` (21) | `Tintalú: Classificar Água` (25) | glosario §1.3 |
| Descripción breve (80) | `Sort river water by Amazonian fruit colors. Relaxing sort puzzle, offline.` (74) | `Ordena el agua del río por colores de frutas amazónicas. Relajante y sin wifi.` (78) | `Classifique a água pelas cores das frutas amazônicas. Relaxante, offline.` (73) | Fichas §2 |
| Descripción completa (4.000) | `en/ficha.md` §3 | `es-419/ficha.md` §3 | `pt-BR/ficha.md` §3 | El bloque `text` entero |
| Icono, gráfico, capturas | §0.7: capturas 01-08 de `graficos-v2/A/en/` | Capturas 01-08 de `graficos-v2/A/es-419/` | Capturas 01-08 de `graficos-v2/A/pt-BR/` | `docs/store-listing/graficos-v2/README.md` (D-065) |

Al pegar la descripción completa:
- Se pega **el bloque `text` entero, tal cual**: ya no lleva la línea del asesor cultural (D-064) y la línea de la Liga (`- River League: …`, `- Liga del río: …`, `- Liga do Rio: …`) se queda, porque la Liga sale encendida (§0.4).
- Longitudes medidas el 2026-10-30 (NFC): en 2.412, es 2.559, pt 2.505 caracteres. Todas por debajo de 4.000 (`docs/store-listing/README.md` §2).
- Sin emojis ni mayúsculas sostenidas (08 §13.1 #5; glosario §1.1). La prohibición de «Best», «#1» y «Free» del glosario (`00-glosario.md`, fila del título) es **solo para el título**; la descripción puede decir «free», como ya hace («Tintalú is free to play»). La línea final de independencia («…not affiliated with Amazon.com, Inc.») se queda (08 §13.1 #5).

**Fichas personalizadas** (08 §13.1 #6; se quedan con la salida mundial, D-057): Brasil (Pix) y México (OXXO), solo si Play ofrece esos métodos al lanzar (Q-B13-17 (a)). Las frases están en `pt-BR/ficha.md` §7 y `es-419/ficha.md` §7. Son opcionales para el lanzamiento [P].

## 6. Testers, pista interna y Play App Signing

### 6.1 Testers de licencia (08 §13.1 #15)

- Play Console (fuera de la app, en el menú de la cuenta) › Configuración › **Pruebas de licencias** [NV]: añade **la cuenta de Google de tu teléfono de pruebas**. Respuesta de licencia: **RESPOND_NORMALLY** [NV].
- Con esa cuenta, las compras salen con tarjetas de prueba y no se cobran (`docs/dev/iap.md` §3, paso 2). Quedan en el libro mayor de prod marcadas como de prueba (`purchase_type`; `iap.md` §3, paso 3).

### 6.2 Lista de la pista interna (D-059, punto 5)

- Prueba › **Pruebas internas** › Testers › Crear lista de correo [NV]: nombre [P] «Solo dueño», con **solo tu cuenta** (la misma del teléfono). **Nunca jugadores.**
- Guarda y copia el **enlace para unirse** («Copiar enlace») [NV]; lo usas en §15.

### 6.3 Primera versión en la pista interna (y Play App Signing)

Prueba › Pruebas internas › **Crear versión** [NV]:
1. **Play App Signing** (08 §13.1 #3; `release.md` §3): la primera vez Play pregunta por la clave de firma. Elige **la clave de firma de apps generada por Google** (la recomendada) [NV texto]. Tu clave de subida queda registrada con la primera subida.
2. Sube **`android-aab-release-1.0.0-10000`** (el `.aab` de §0.8). Play lee `com.parallext.tintalu` y el permiso `BILLING`.
3. Nombre de la versión: `1.0.0` [P]. Notas: las de «Novedades» de las fichas (§17.2) o una línea interna.
4. Guardar › Revisar versión › **Iniciar lanzamiento en Pruebas internas**. Con la publicación administrada, publica el cambio desde «Resumen de publicación» [NV] (§2.1).
- [NV] Play Console puede pedir completar algunas tareas del panel antes de dejar lanzar incluso en la pista interna; si lo pide, haz antes §8, §9, §13 y §14, que no dependen de la subida.
- **Por qué subir ya:** sin un AAB con el permiso `BILLING` Play no deja crear los productos [NV], y las huellas de la firma de Play (§7) solo aparecen tras esta subida.

## 7. Huellas SHA: Firebase, App Check, Play Integrity y Play Games

### 7.1 Dónde están las huellas

Prueba y publicación › Configuración › **Integridad de la app** › Firma de apps [NV]. Hay dos certificados, cada uno con su SHA-1 y su SHA-256:
- **Clave de firma de apps** (la de Google): es la que firma lo que instalan los jugadores. **Es la que va a todos los sitios de abajo.**
- **Clave de subida:** su SHA-256 **tiene que coincidir** con la línea «SHA-256 del certificado de subida» del job `preflight` (§0.8; `release.md` §3). Si no coincide, para: el AAB no es el tuyo.

Las huellas no son secretas, pero cópialas de la consola cada vez, sin reescribirlas a mano.

### 7.2 A dónde va cada una

| Destino | Huella | Dónde [NV rutas] | Fuente |
|---|---|---|---|
| **Firebase**, app Android de prod `com.parallext.tintalu` («Tintalu Android (prod)», proyecto `tintalu`) | **SHA-1 y SHA-256 de la firma de apps** (y, si quieres probar builds firmados con la de subida, también sus dos [P]) | Consola de Firebase › Configuración del proyecto › General › Tus apps › la app Android › **Agregar huella digital** | 08 §13.1 #18; `release.md` §3 |
| **App Check con Play Integrity** | **SHA-256 de la firma de apps** | Firebase › App Check › Apps › la app Android › **Play Integrity** › Registrar (pide la SHA-256) | 08 §13.1 #18; `iap.md` §3, paso 3; D-035, añadido del 2026-10-27 |
| **Play Integrity API en Play Console** | Ninguna: se vincula el proyecto de Google Cloud | Integridad de la app › API de Play Integrity › **Vincular proyecto de Cloud** › el proyecto de Firebase `tintalu` [P: el mismo proyecto de Firebase de prod] | 08 §13.1 #18 |
| **Play Games: credencial OAuth de Android** | **SHA-1 de la firma de apps**; y una segunda credencial con el **SHA-1 de la de subida** | Crecer › Servicios de juegos de Play › Configuración y administración › Configuración › **Credenciales** › Agregar credencial › Android | `docs/dev/play-games.md` §9, paso 1; 08 §13.1 #16 |

- **App Check:** no actives la «aplicación obligatoria» (*enforcement*) de App Check para los productos de Firebase sin probarlo antes [P]: la API de AWS ya comprueba los tokens de App Check por su cuenta (`-c firebaseAppIds`, D-035).
- **Tras añadir las huellas en Firebase**, descarga de nuevo el `google-services.json` de la app de prod y compáralo con el tuyo. Si cambió, vuelve a crear el secreto `GOOGLE_SERVICES_JSON_BASE64` (`release.md` §2 bis) antes del release de §7.5 [NV: que añadir huellas lo cambie].

### 7.3 El proyecto de Play Games con Saved Games

Crecer › **Servicios de juegos de Play** › Configuración y administración › Configuración [NV] (`play-games.md` §9; 08 §13.1 #16):
1. **Crear el proyecto de juegos** para esta app. Si pregunta por un proyecto de Google Cloud, usa [P] el de Firebase `tintalu`, para tener Play Games, Firebase y Play Integrity en el mismo sitio.
2. Si pide la **pantalla de consentimiento de OAuth** del proyecto de Cloud [NV]: nombre «Tintalú», correo de asistencia `support@tintalu.com`, dominio `tintalu.com`, enlaces a la política y los términos de §3.
3. Las **dos credenciales Android** de §7.2 (SHA-1 de la firma de apps y SHA-1 de la de subida). **Sin Web Client ID** (`play-games.md` §10).
4. Propiedades › **Juegos guardados (Saved Games): activado**.
5. Logros, marcadores y eventos: **ninguno** (`play-games.md` §10; 08 §13.3). 08 §13.1 #16 dice «logros básicos»: ver la duda 6 de §19.
6. **Testers** de Play Games: añade tu cuenta [NV: hasta que publiques la configuración de Play Games, solo los testers pueden iniciar sesión].
7. **Publicar la configuración de Play Games** cuando lo pidas en §17 [NV: Play Games tiene su propio «Publicar»; sin él, los jugadores no inician sesión].

### 7.4 Recursos de Play Games

- Configuración › **Obtener recursos** › Android: copia el XML (`games-ids.xml`, con `app_id`). **No es secreto** (va en el manifiesto de la app; `play-games.md` §9, paso 2).
- Pásalo a un agente (o hazlo tú con el Editor abierto, una sola instancia): `Window › Google Play Games › Setup › Android setup`, sin Web Client ID, «Setup», y commit de los archivos que cambia, con sus `.meta`, en un **PR propio y revisado** (`play-games.md` §9, paso 3). Sin este `APP_ID` el build no tiene Play Games: la nube no hace nada.

### 7.5 Segunda versión interna

- Con el `APP_ID` (§7.4) y la URL del backend (§0.2) en `main`, lanza otra vez **Release firmado** con `versionName` `1.0.0` y **`versionCode` `10001`** (a mano: vacío daría 10000, que ya está subido; `release.md` §4).
- Súbelo a la pista interna como en §6.3. **Esta es la versión que probarás (§15) y la que promocionarás (§17).**

## 8. Seguridad de los datos (Data Safety)

Contenido de la app › **Seguridad de los datos** › Empezar [NV]. Versión larga, con las razones: `docs/store-listing/data-safety.md`. Contesta con el build de §7.5.

### 8.1 Preguntas generales

| # | Pregunta [NV] | Respuesta |
|---|---|---|
| 1 | ¿Tu app recoge o comparte alguno de los tipos de datos obligatorios? | **Sí** |
| 2 | ¿Todos los datos están encriptados en tránsito? | **Sí** |
| 3 | Métodos de creación de cuenta | La cuenta es **anónima y automática** (Firebase Auth), más el inicio de sesión silencioso de Play Games. Si no hay opción que encaje, **«Otro»**: «Anonymous account created automatically; no personal data is requested» [P] |
| 4 | ¿Los usuarios pueden pedir que se eliminen sus datos? | **Sí**: en la app (Ajustes › «Eliminar cuenta y datos») y en la web |
| 5 | URL para eliminar la cuenta | **`https://tintalu.com/tintalu/en/delete-account/`** |
| 6 | ¿Pueden pedir borrar algunos datos sin borrar la cuenta? | **No** [NV si se pregunta] |
| 7 | Revisión de seguridad independiente | **No** |

### 8.2 Tipos de datos

Marca solo estos tipos. Para cada uno, Play pregunta si se recoge, si se comparte, si es efímero (siempre **No**), si es obligatorio u opcional y las finalidades. La Liga sale encendida (§0.4; D-064): marca también lo que dice «(Liga)».

| Categoría › tipo [NV] | Recogido | Compartido | Obligatorio / opcional | Finalidades |
|---|---|---|---|---|
| Ubicación › **Ubicación aproximada** | Sí | **Sí** | Obligatorio [E] | Publicidad o marketing; prevención de fraude, seguridad y cumplimiento; (Liga) funcionalidad de la app |
| Información personal › **ID de usuario** | Sí | **Sí**: el UID va a Google (AdMob) como `user_id` del SSV de los recompensados (08 §13.3, fila de Google Mobile Ads); y, con la Liga, el seudónimo y el avatar visibles; el backend guarda 30 días el registro de cada premio comprobado | Obligatorio | Funcionalidad de la app; prevención de fraude, seguridad y cumplimiento; gestión de cuentas |
| Información financiera › **Historial de compras** | Sí | No | Opcional | Funcionalidad de la app; prevención de fraude, seguridad y cumplimiento |
| Actividad en la app › **Interacciones en la app** | Sí | **Sí** (con los anuncios) | Obligatorio [E] (la analítica sola sería opcional) | Analítica; publicidad o marketing |
| Actividad en la app › **Otras acciones del usuario** | Sí | (Liga) **Sí**; sin Liga, **No** | Obligatorio [E] | Funcionalidad de la app |
| Información y rendimiento de la app › **Registros de fallos** | Sí | No | **Opcional** (interruptor de Ajustes, T-L6; D-057) | Analítica |
| Información y rendimiento de la app › **Diagnóstico** | Sí | **Sí** (los SDK de anuncios) | Obligatorio [E] | Analítica; publicidad o marketing; prevención de fraude, seguridad y cumplimiento; funcionalidad de la app |
| Información y rendimiento de la app › **Otros datos de rendimiento** | Sí | No | **Opcional** (consentimiento de analítica) | Analítica |
| ID de dispositivo u otros › **ID de dispositivo u otros identificadores** | Sí | **Sí** (ID de publicidad) | Obligatorio [E] | Funcionalidad de la app; analítica; publicidad o marketing; prevención de fraude, seguridad y cumplimiento |

- **No marques** nombre, correo, teléfono, dirección, ubicación precisa, mensajes, fotos, vídeos, audio, archivos, calendario, contactos, salud, navegación ni apps instaladas (`data-safety.md` §3, «No se recoge»).
- Cerrado por D-057 (investigación propia o riesgo aceptado): la IP de los registros, los diagnósticos de Play Billing (Q-BILL-2), el ID de publicidad con `npa` y la categoría de los datos visibles de la Liga (`data-safety.md` §6).
- Guardar › Enviar [NV]. La vista previa que da Play es la que verá el jugador: léela entera.

## 9. Clasificación de contenido (IARC)

Contenido de la app › **Clasificación de contenido** › Empezar cuestionario [NV] (08 §13.1 #10; `iarc.md`). Es un solo cuestionario para todo el mundo (D-057).

| Pregunta o tema [NV] | Respuesta | Fuente |
|---|---|---|
| Correo de contacto | **`support@tintalu.com`** | D-041, añadido del 2026-10-25 |
| Categoría | **Juego** | 08 §13.1 #1 |
| Violencia; sangre; miedo; contenido sexual; lenguaje soez; drogas, alcohol y tabaco; discriminación | **No** en todas | `iarc.md` §2 (Q-B13-11 (a): la Piraña no es violencia) |
| Juegos de azar simulados; apuestas con dinero real | **No** | 05 §7; `iarc.md` §2 |
| Los usuarios pueden interactuar o intercambiar contenido | **Sí**: la Liga sale encendida (§0.4; D-064; 08 §13.1 #10). Otros jugadores ven el seudónimo automático, el avatar, la bandera y los puntos; sin chat ni texto libre. Si la pregunta se limita a texto, voz, imágenes o audio aportados por los usuarios, **No** | 08 §13.1 #10; `iarc.md` §3 |
| Comparte la ubicación del usuario con otros usuarios | **Sí, a nivel de país** (la bandera de la Liga, encendida: D-064). Si la pregunta se limita a la ubicación física actual o precisa, **No** | 08 §13.1 #10; `iarc.md` §3 |
| Compras digitales | **Sí** | 08 §13.1 #10 y #14 |
| Compras de elementos aleatorios (cajas de botín) | **No** (contestar «sí» por error daría ClassInd 18 en Brasil) | 05 §7; 09 R2 |
| Acceso a internet sin restricciones (navegador) | **No** (los enlaces abren el navegador del sistema) | `iarc.md` §3 |
| Contiene anuncios | **Sí** | 08 §13.1 #8; D-049 |

Resultado esperado: **ClassInd 14** en Brasil por las compras; el resto, lo que asigne IARC (08 §13.1 #10; `iarc.md` §4).

## 10. Los 13 productos de compra

Monetizar › Productos › **Productos integrados en la aplicación** (o «Productos únicos») › Crear producto [NV]. Necesita la subida de §6.3 (permiso `BILLING`).

### 10.1 Reglas para los 13

- **Tipo:** producto integrado de compra única (no suscripción). Que sea consumible o no lo decide el juego, no Play (05 §4.1; `StoreCatalog.cs`).
- **Una sola oferta de compra** por producto (Q-BILL-3 (a); `iap.md` §4.1).
- **«Compras de varias unidades»: desactivado** (Q-BILL-4 (a)).
- **Estado: activo.**
- El ID es **inmutable** y tiene que ser exactamente el de la tabla (05 §4.1; glosario §11). No crees `af_pass` (es de la 1.2) ni `af_coins_1`, `af_coins_7` o `af_noads_bundle` (reservados).
- Idiomas del nombre y la descripción: el predeterminado en-US y las traducciones es-419, es-ES (copia de es-419), pt-BR y pt-PT (copia de pt-BR), como la ficha [P]. Límites de Play [NV]: nombre hasta 55 caracteres y descripción hasta 200; todos los de abajo caben.

### 10.2 ID, nombre y precio base

Nombres de `Assets/_Project/Data/Loc/strings.csv` (claves `iap_<sku>_name`, revisadas), que son los que usa el Mercado. Precio base en **USD** (05 §4.1; D-057, P5 (a)).

| ID del producto | USD | Nombre en-US | Nombre es-419 (y es-ES) | Nombre pt-BR (y pt-PT) |
|---|---|---|---|---|
| `af_starter` | 0,99 | Starter Pack | Mochila de inicio | Mochila Inicial |
| `af_coins_2` | 1,99 | Handful | Puñado de semillas | Punhado |
| `af_coins_3` | 4,99 | Bag | Bolsa de semillas | Saco pequeno |
| `af_coins_4` | 9,99 | Sack | Saco de semillas | Saco |
| `af_coins_5` | 19,99 | Big Sack | Costal de semillas | Costal |
| `af_coins_6` | 49,99 | Harvest | Cosecha de semillas | Colheita |
| `af_rescue` | 4,99 | Rescue Raft | Balsa de rescate | Balsa de Resgate |
| `af_noads` | 4,99 | No Ads | Sin anuncios | Sem anúncios |
| `af_piggy` | 2,99 | Clay Jar | Cántaro | Pote de Barro |
| `af_infinite_canoes_1h` | 1,99 | Endless Canoes 1 h | Canoas infinitas 1 h | Canoas infinitas 1 h |
| `af_skin_m02` | 1,99 | Santarém Cuia Pack | Pack Cuia de Santarém | Pacote Cuia de Santarém |
| `af_skin_m03` | 1,99 | Meeting Cuia Pack | Pack Cuia del Encuentro | Pacote Cuia do Encontro |
| `af_skin_m04` | 1,99 | Totuma Pack | Pack Totuma | Pacote Totuma |

### 10.3 Descripciones

Salen de las líneas de contenido del Mercado en `strings.csv` con las cifras de 05 §4.1 y de `StoreCatalog.cs`; las marcadas [P] no tienen línea propia en `strings.csv` y se componen aquí. Las cifras van con el separador de miles de 05 (1.000); en inglés, con coma.

| ID | en-US | es-419 (y es-ES) | pt-BR (y pt-PT) | Origen |
|---|---|---|---|---|
| `af_starter` | 500 seeds, 2 Extra Cuia and 3 Morpho | 500 semillas, 2 Cuia extra y 3 Morpho | 500 sementes, 2 Cuia Extra e 3 Morpho | `market_starter_content` |
| `af_coins_2` | 1,000 seeds | 1.000 semillas | 1.000 sementes | `market_seeds_amount` |
| `af_coins_3` | 2,800 seeds | 2.800 semillas | 2.800 sementes | ídem |
| `af_coins_4` | 6,000 seeds | 6.000 semillas | 6.000 sementes | ídem |
| `af_coins_5` | 13,000 seeds | 13.000 semillas | 13.000 sementes | ídem |
| `af_coins_6` | 36,000 seeds | 36.000 semillas | 36.000 sementes | ídem |
| `af_rescue` | All you need to go on: seeds, a cuia and endless canoes | Todo para seguir: semillas, una cuia y canoas infinitas | Tudo para seguir: sementes, uma cuia e canoas infinitas | `stuck_balsa_499_content` |
| `af_noads` | No ads between levels, forever. Optional videos stay. +500 seeds | Sin anuncios entre niveles, para siempre. Los videos opcionales siguen. +500 semillas | Sem anúncios entre níveis, para sempre. Os vídeos opcionais continuam. +500 sementes | `market_noads_content` |
| `af_piggy` | [P] The seeds saved in your Clay Jar (1,000 to 3,000). Fills up with every win | [P] Las semillas que guarda tu Cántaro (de 1.000 a 3.000). Se llena con cada victoria | [P] As sementes guardadas no seu Pote de Barro (de 1.000 a 3.000). Enche a cada vitória | 05 §4.1; `market_piggy_fills` |
| `af_infinite_canoes_1h` | 60 min of Endless Canoes | 60 min de Canoas infinitas | 60 min de Canoas infinitas | `market_canoes_content` |
| `af_skin_m02` | [P] Santarém Cuia for your cuias and 300 seeds | [P] Cuia de Santarém para tus cuias y 300 semillas | [P] Cuia de Santarém para suas cuias e 300 sementes | `skin_af_skin_m02_name`; `StoreCatalog.cs` |
| `af_skin_m03` | [P] Meeting Cuia for your cuias and 300 seeds | [P] Cuia del Encuentro para tus cuias y 300 semillas | [P] Cuia do Encontro para suas cuias e 300 sementes | `skin_af_skin_m03_name` |
| `af_skin_m04` | [P] Totuma for your cuias and 300 seeds | [P] Totuma para tus cuias y 300 semillas | [P] Totuma para suas cuias e 300 sementes | `skin_af_skin_m04_name` |

Notas:
- Las semillas de los packs y el bono de «Sin anuncios» son los defaults de Remote Config `eco_seed_pack_amounts` y `eco_noads_bonus_seeds` (08 §9). Si algún día los cambias en Remote Config, cambia también aquí la descripción del producto.
- La Balsa lleva además una Cuia extra al stock (05 §4.1); la frase del juego no la nombra y aquí se respeta. Si `biz_feature_lives` está apagado no hay canoas (D-038): la frase cambia en el juego (`stuck_balsa_499_content_no_canoes`), no en Play.
- Los packs de skin: 05 §4.1 dice «skin + la escena de la gran parada del mundo como marco del Mapa + 300 semillas», pero el catálogo del juego solo da la skin y 300 semillas: la descripción no promete la escena (duda 4 de §19).
- Los nombres en inglés de los packs de semillas («Handful», «Bag»…) no llevan «seeds»; la descripción lo dice. Si prefieres que el nombre lo diga, es un cambio de textos con su revisión (D-054), no algo que se haga solo en Play.

### 10.4 Precios por país (05 §4.2; D-057, P5 (a))

1. Pon el **precio base en USD** de §10.2. Play **convierte automáticamente** al resto de países con sus redondeos locales.
2. **Sobrescribe a mano** solo estos países, con la tabla de 05 §4.2 (LatAm ≈ 50 % del ancla, CL 60 %, EC 70 % con piso de 0,99, EUR en paridad):

| USD (productos) | BRL (BR) | MXN (MX) | COP (CO) | PEN (PE) | AR (equiv. USD) | CLP (CL) | USD (EC) | EUR |
|---|---|---|---|---|---|---|---|---|
| 0,99 (`af_starter`) | 2,49 | 9 | 1.900 | 1,90 | 0,49 | 590 | 0,99 | 0,99 |
| 1,99 (`af_coins_2`, `af_infinite_canoes_1h`, `af_skin_m02`…`m04`) | 4,99 | 19 | 3.900 | 3,90 | 0,99 | 1.190 | 1,49 | 1,99 |
| 2,99 (`af_piggy`) | 7,90 | 29 | 5.900 | 5,90 | 1,49 | 1.790 | 1,99 | 2,99 |
| 4,99 (`af_coins_3`, `af_rescue`, `af_noads`) | 12,90 | 45 | 9.900 | 9,90 | 2,49 | 2.990 | 3,49 | 4,99 |
| 9,99 (`af_coins_4`) | 24,90 | 89 | 18.900 | 18,90 | 4,99 | 5.990 | 6,99 | 9,99 |
| 19,99 (`af_coins_5`) | 49,90 | 169 | 36.900 | 37,90 | 9,99 | 11.990 | 13,99 | 19,99 |
| 49,99 (`af_coins_6`) | 129,90 | 429 | 94.900 | 94,90 | 24,99 | 29.990 | 34,99 | 49,99 |

3. **Comprueba el mínimo de cada país** antes de guardar: 05 §4.2 avisa de que 9 MXN o R$ 2,49 pueden quedar por debajo del mínimo de Play [NV].
4. A los 30 días, revisar los países con más instalaciones fuera de la tabla (India, Indonesia ya excluida, Turquía, Filipinas…) por si conviene bajar el precio (05 §4.2).
5. El juego enseña el precio que da Play en la moneda de la cuenta (D-025, punto 3); un producto sin precio en un país no se vende allí (`lanzamiento-mundial.md` §2.2).

**Lo que no está claro de cómo encaja la tabla con D-057** (preguntas, duda 3 de §19; mientras no respondas, la recomendada [P] va entre paréntesis):
- **Argentina:** la columna de 05 §4.2 es un «equivalente en USD que se recotiza cada mes», pero no dice si Play Console fija el precio de Argentina en ARS o en USD [NV]. (Recomendada [P]: si Play pide ARS, convertir ese equivalente al cambio del día y revisarlo cada mes, como dice 05.)
- **EUR «en paridad»:** 05 no dice a qué países. (Recomendada [P]: a todos los países cuya moneda en Play es el euro; los demás europeos, con la conversión automática.)
- **El orden:** si cambias más tarde el precio base en USD, [NV] si Play recalcula también los países que sobrescribiste a mano.

## 11. Las dos cuentas de servicio (AWS Secrets Manager)

**Hecho el 2026-10-30** (D-035, añadido del 2026-10-30): las dos claves están cargadas y la web redesplegada. Lo que queda es probarlas en §15.

El CDK de prod creó los dos secretos con un relleno: mientras lo lleven, las funciones fallan cerradas y **no se acredita ninguna compra ni se borra ninguna cuenta** (`infra/README.md` §5; D-035, añadido del 2026-10-27).

**Reglas para las dos** (`CLAUDE.md`, regla dura 5; `infra/README.md` §5):
- La clave JSON se descarga **a una carpeta fuera del repositorio** (por ejemplo `C:\Users\USER\secretos-tintalu\`, nunca dentro de `fill-game`), se carga en AWS **desde el archivo** (`file://`) y se **borra** al terminar.
- **Nunca** la pegues en el chat ni en un comando como texto (`--secret-string '{…}'` deja la clave en el historial de la terminal), ni la leas de vuelta con `get-secret-value`. Para comprobar, `describe-secret`, que no enseña el valor.
- Las Lambdas guardan el valor en caché hasta 5 minutos. Para rotar, nueva clave en Google Cloud y se vuelve a cargar igual. La federación sin claves queda para la 1.1 (Q-AWS-7).

### 11.1 `af-prod/play-service-account` (verificar compras y anuladas)

Lo usan `verifyPurchase` y `voidedPurchasesJob` (ámbito `androidpublisher`).
1. **Google Cloud**, proyecto [P] `tintalu` (el de Firebase prod): APIs y servicios › **habilitar «Google Play Android Developer API»**.
2. IAM y administración › Cuentas de servicio › **Crear**: nombre [P] `af-prod-play-verifier`. No necesita roles de Cloud [NV].
3. Claves › Agregar clave › **JSON** → guárdala como `C:\Users\USER\secretos-tintalu\play.json`. Si la organización de Google Cloud prohíbe crear claves (`iam.disableServiceAccountKeyCreation`) [NV], hay que permitirlo para este proyecto.
4. **Play Console › Usuarios y permisos › Invitar a usuarios nuevos** [NV]: el correo de la cuenta de servicio (acaba en `.iam.gserviceaccount.com`). Sin permisos de cuenta. En **Permisos de la app**, añade solo Tintalú con **«Ver información financiera»** y **«Gestionar pedidos y suscripciones»** (`infra/README.md` §5). Invitar.
   - [NV] Los permisos pueden tardar en hacerse efectivos (a veces hasta un día): si la primera compra de prueba falla con un error de permisos de la API de Play, espera y repite.
5. En PowerShell:
   ```
   aws sso login --profile af-prod
   aws secretsmanager put-secret-value --profile af-prod --region sa-east-1 --secret-id af-prod/play-service-account --secret-string file://C:/Users/USER/secretos-tintalu/play.json
   aws secretsmanager describe-secret --profile af-prod --region sa-east-1 --secret-id af-prod/play-service-account
   Remove-Item C:\Users\USER\secretos-tintalu\play.json
   ```
   `describe-secret` debe mostrar una fecha de cambio (`LastChangedDate`) de hoy.

### 11.2 `af-prod/firebase-admin-service-account` (borrar la cuenta)

Lo usa `deleteAccount` (ámbito `identitytoolkit`, `accounts:delete`).
1. Google Cloud, proyecto `tintalu`: **habilitar «Identity Toolkit API»**.
2. Crear cuenta de servicio [P] `af-prod-account-deleter` con **un solo rol que solo borre usuarios de Auth**, por ejemplo **«Firebase Authentication Admin»** (`infra/README.md` §5). **No** uses la clave «firebase-adminsdk» que ofrece la consola de Firebase en Cuentas de servicio: tiene muchos más permisos de los que hacen falta.
3. Clave JSON → `C:\Users\USER\secretos-tintalu\firebase-admin.json`.
4. En PowerShell:
   ```
   aws secretsmanager put-secret-value --profile af-prod --region sa-east-1 --secret-id af-prod/firebase-admin-service-account --secret-string file://C:/Users/USER/secretos-tintalu/firebase-admin.json
   aws secretsmanager describe-secret --profile af-prod --region sa-east-1 --secret-id af-prod/firebase-admin-service-account
   Remove-Item C:\Users\USER\secretos-tintalu\firebase-admin.json
   ```

Comprobación sin secretos: en §15, una compra de prueba que se acredita prueba la primera; «Eliminar cuenta y datos» que termina bien prueba la segunda.

## 12. Firebase Analytics: retención

### 12.1 Retención (Q-B13-19)

- **Ya hecha** en el proyecto de prod `tintalu`: **eventos 2 meses** y **datos de usuario 14 meses** (D-035, añadido del 2026-10-27). La política de la web dice «Analítica: 2 meses» (`infra/web/src/content/es-419/privacy.html`, tabla de plazos).
- Compruébalo en la propiedad de Google Analytics enlazada al proyecto: Administrar › Recogida y modificación de datos › **Retención de datos** [NV ruta]. Si alguien la cambió, vuelve a 2 meses (Q-B13-19 (a): la mínima).

### 12.2 BigQuery: no es un paso de esta guía

La exportación a BigQuery **no** se activa como parte del lanzamiento: 09, «Preguntas abiertas», pregunta 7, se decidió con su propuesta por defecto, «paneles nativos hasta confirmar la cuota de BigQuery». Si quieres activarla antes del lanzamiento es una decisión tuya: la propuesta [P] y sus detalles están en la duda 5 de §19.

## 13. Público objetivo y contenido

Contenido de la app › **Público objetivo y contenido** [NV] (08 §13.1 #11; 05 §7):

| Pregunta [NV] | Respuesta | Fuente |
|---|---|---|
| Grupos de edad del público objetivo | **13-15, 16-17 y 18 o más**. Ninguno de 12 o menos | 08 §13.1 #11 |
| ¿Puede resultar atractiva para niños? | **No**: estética revisada para no serlo; pantalla de edad neutral y `<13` bloqueado («Pide a un adulto que instale el juego») | 08 §13.1 #11; 05 §7 |
| Programa Diseñado para familias | **No** | 08 §13.1 #11 |
| Ficha | Sin captación infantil (T-L14, D-057) | D-057, añadido del 2026-10-27 |

## 14. Noticias, gobierno, finanzas, salud y demás declaraciones

Contenido de la app; responde **No** o «no aplica» en todas (08 §13.1 #13):

| Declaración [NV nombres] | Respuesta |
|---|---|
| App de noticias | **No** |
| App de rastreo de contactos o de estado de la COVID-19 (si aún aparece) | **No** |
| App gubernamental | **No** |
| Funciones financieras | **Ninguna** («Mi app no ofrece funciones financieras») |
| Salud (apps y funciones de salud) | **Ninguna** |
| Permisos sensibles (ubicación, SMS, llamadas, todos los archivos, alarmas exactas, servicio en primer plano, VPN, accesibilidad) | No aplica: el manifiesto no los lleva (`ci/check-manifest.ps1`; 08 §13.3) |

**Leyes de las tiendas de EE. UU. (Texas SB 2420; T-L3, D-057):** declara la clasificación por edad de la app y de sus productos en la sección que Play Console tenga para esas leyes [NV dónde] (`docs/dev/anuncios-admob.md` §10, punto 7). Respuesta [P]: los productos son aptos para el mismo público que la app (13+), y con 13-17 la compra lleva doble confirmación en el juego más la autenticación de compras de Google Play (D-033, Q-T4).

## 15. Prueba en la pista interna (solo tú)

Con la versión de §7.5 (D-059, punto 5; `iap.md` §3; `release.md` §4 bis, paso 4):
1. En el teléfono, con la cuenta de §6, abre el **enlace para unirse** de §6.2, acepta e instala Tintalú **desde Play**.
2. Antes de comprar: §11 hecho (las dos cuentas de servicio) y §7.2 hecho (App Check con Play Integrity): la API exige el token de integridad.
3. Casos de `iap.md` §3, paso 5: precio en tu moneda; tarjeta que siempre aprueba («Confirmando…», crédito una vez); la que rechaza; las lentas; cerrar la app tras pagar; modo avión; «Sin anuncios» y reinstalar (vuelve una vez); banda 13-17 (doble confirmación); banda `<13` (no vende).
4. **PAD: no aplica en la 1.0** (todo el contenido va en el AAB): no hay Addressables ni Play Asset Delivery en `Packages/manifest.json` ni en el código, así que no hay nada que descargar aparte (08 §13.1 #17 lo preveía para `world_w05` y los mundos 6-9).
5. **Play Games:** inicio de sesión silencioso sin ventana; primera subida; reinstalar y recuperar la partida; «Eliminar cuenta y datos» con y sin red (`play-games.md` §9, paso 4).
6. **Eliminar cuenta y datos** desde Ajustes termina bien (prueba la cuenta de servicio de §11.2) y el código de cuenta funciona en `https://tintalu.com/tintalu/en/delete-account/`.
7. Si algo falla: se corrige, vuelta a §0.8 con un `versionCode` mayor y otra subida a la pista interna.

## 16. Países y regiones

Producción › **Países y regiones** › Añadir países o regiones [NV] (D-057 y su añadido del 2026-10-27; D-059, punto 2; 08 §13.1 #22):
- **Todos**, y luego quita **Rusia, Bielorrusia, Vietnam e Indonesia**. **Turquía se queda** (se revisa si pasa del 3 % de las instalaciones, riesgo R8).
- En Remote Config, `biz_country_enabled` ya es `{"*":true}` (08 §9): no hace falta tocarlo.

## 17. Producción al 100 %, revisión y «Publicar cambios»

### 17.1 Antes de enviar

- §0 a §16 hechos; en el panel de la app no queda ninguna tarea pendiente [NV].
- La **publicación administrada sigue activada** (§2.1).
- Configuración de Play Games publicada (§7.3, paso 7).

### 17.2 Promocionar la versión de la pista interna

Pruebas internas › la versión probada › **Promocionar versión › Producción** [NV] (`release.md` §4 bis, paso 5):
- Es **el mismo AAB y el mismo `versionCode`** (10001 si seguiste §7.5): no subas otro build.
- **Notas de la versión** (≤ 500; fichas §5):
  - en-US: `Welcome to the river. The first version of Tintalú: travel upriver through nine worlds, play the River Challenge every day and fill your Logbook.`
  - es-419 y es-ES: `Te damos la bienvenida al río. Primera versión de Tintalú: remonta el río por nueve mundos, juega el Reto del río cada día y llena la Bitácora.`
  - pt-BR y pt-PT: `Boas-vindas ao rio. A primeira versão de Tintalú: suba o rio por nove mundos, jogue o Desafio do Rio todo dia e preencha o seu Diário.`
- **Disponibilidad: 100 %.** Nunca un porcentaje menor (D-059, punto 2).
- Revisar versión › **Enviar a revisión** [NV]. La primera revisión de una app nueva puede tardar varios días.

### 17.3 Publicar

- Cuando Google la apruebe, en **«Resumen de publicación» › «Publicar cambios»**, el día y a la hora que elijas: sale entera en todos los países a la vez (D-059, punto 3).
- Riesgo aceptado (D-059): un fallo en un modelo no probado llega a todos a la vez; detener la versión no la retira de quien ya la instaló; UMP en el EEE se verá por primera vez con jugadores reales.

## 18. Después de publicar (08 §13.1 #23)

- **`app-ads.txt`:** `https://tintalu.com/app-ads.txt` ya sirve las líneas de AdMob y Unity Ads (`infra/web/src/static/app-ads.txt`). En AdMob, **vincula la app con su ficha de Play** y espera a que AdMob la verifique (`anuncios-admob.md` §10, punto 2) [NV: la verificación tarda]. En Unity, añade la URL de la tienda a la app «Tintalú» [P].
- **Alertas:** en Firebase › Crashlytics, alertas de fallos nuevos y de velocidad al correo `support@tintalu.com` [P]; en Play Console, las notificaciones de Android vitals [NV]. Confirma la suscripción a las alarmas de AWS que llega a `support@tintalu.com` (D-035, añadido del 2026-10-27).
- **Archivar los textos legales mostrados:** anota el commit de `infra/web` desplegado y la fecha, y guarda una copia de la política, los términos y la página de borrado tal como estaban al publicar [P].
- **Vigilar desde el primer día** (08 §13.1 #21): fallos percibidos por el usuario < 1,09 %; ANR por debajo del umbral vigente de Play [NV cifra].
- **Si hay que frenar:** primero Remote Config (`biz_feature_*`, `mech_enabled_*`); detener la versión en Play no la quita de quien ya la instaló (`release.md` §4 bis, paso 7).
- **Más adelante:** Meta Audience Network y Mintegral como redes de AdMob, que piden la app ya publicada (D-049, punto 4); a los 30 días, la revisión de precios de §10.4.

## 19. Dudas abiertas (no resueltas aquí)

1. **Nombre del desarrollador en Play.** La cuenta muestra «Automation AI» (`pendientes-del-dueno.md`, 2026-10-20), y las fichas ponen «Parallext, LLC» como desarrollador (D-041). D-048 separa editora (AUTOMATION AI S.A.S) y desarrollador (Parallext). ¿Qué nombre público quieres en la cuenta? Es un ajuste de la cuenta, no de la app [NV].
2. ~~Liga del río en producción~~ **Resuelta (D-064):** sale encendida (§0.4).
3. **Precios** (§10.4): Argentina en ARS o en USD; a qué países se aplica el EUR en paridad.
4. **Packs de skin:** 05 §4.1 promete «la escena de la gran parada del mundo como marco del Mapa del río», pero `StoreCatalog.cs` solo da la skin y 300 semillas. ¿Se corrige 05 o falta código? La descripción de §10.3 no promete la escena.
5. **Exportación a BigQuery: ¿antes del lanzamiento?** 09, «Preguntas abiertas», pregunta 7, se decidió con «paneles nativos hasta confirmar la cuota de BigQuery», así que hoy no se activa. Propuesta [P] para que decidas:
   - **Por qué antes:** la exportación solo copia los datos desde el día en que se activa [NV: no es retroactiva], y la retención de 2 meses borra los eventos de Analytics; sin ella desde el día 1, el primer mes de la 1.0 se pierde para análisis posteriores. El GDD cuenta con ella para los KPI (06 §12; 09 §6.2) y para el futuro `rcProposalJob` (08 §2.4).
   - **Cómo** [NV rutas]: consola de Firebase › Configuración del proyecto › Integraciones › BigQuery › Vincular. Pide el plan Blaze (09 §6.2): antes, una alerta de presupuesto en Google Cloud. Google Analytics con exportación diaria [P]; Crashlytics, opcional. El sandbox de BigQuery tiene un tope gratuito [NV: 10 GB de almacenamiento y 1 TB de consulta al mes, 09 §6.2].
   - **Coherencia con la política [P]:** la web promete 2 meses de analítica: caducidad de tablas de 60 días en el conjunto de datos, o cambiar la política antes.
   - **Ubicación del conjunto de datos:** ¿EE. UU. (multirregión) [P], donde ya trata los datos Google Analytics, o `southamerica-east1`, junto a AWS `sa-east-1` (D-057, P8)? Afecta a lo que dice la política sobre transferencias.
6. **Logros de Play Games:** 08 §13.1 #16 dice «logros básicos», pero `play-games.md` §10 y 08 §13.3 dicen «sin logros». Esta guía sigue a `play-games.md` (sin logros).
7. **Orden de Play Games:** D-059 y `release.md` §4 bis prevén probar Play Games en la pista interna con el AAB de §0.8, pero el `APP_ID` solo existe tras crear el proyecto de Play Games, que necesita las huellas de la primera subida. Esta guía propone dos subidas internas (10000 y 10001) y promocionar la segunda [P].
8. ~~IARC con la Liga apagada~~ **Resuelta (D-064):** la Liga sale encendida, así que el IARC contesta «Sí» a la interacción y al país (§9).
