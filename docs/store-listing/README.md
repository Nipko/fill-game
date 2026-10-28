# Borradores de publicación (bloque B13 de la tanda 5)

> **Nota del 2026-10-28 (D-047, D-049, D-057 y D-059):** lo que sigue es la historia del bloque B13 y su puesta al día del 23-10. Hoy: el `applicationId` `com.parallext.tintalu` ya no espera al dictamen (D-047); los anuncios son de AdMob directo con Unity Ads en su mediación y MAX salió del build (D-049 y sus añadidos); la ficha tiene **en-US por defecto**, con es-419, es-ES (copia de es-419), pt-BR y pt-PT (copia de pt-BR) (D-057, P9); la 1.0 va de la pista interna, solo con el dueño, a producción al 100 % con publicación administrada, sin prueba cerrada (D-059). El paso a paso para Play Console, con cada respuesta, está en **`docs/dev/publicar-play-console.md`**. Data Safety ya está en su forma final (`data-safety.md`).

Encargo: `docs/dev/plan-tanda5.md` §4, fila B13 (equipo Spark, 2026-10-20; rama `spark/t5-b13`). **Puesto al día el 2026-10-23** (rama `tanda5/store-listing`) con lo que hoy es real: Parallext, LLC como desarrollador y responsable y el dominio `tintalu.com` (D-041); la web pública en `infra/web/` (D-041); el plugin propio de Play Billing en lugar de Unity IAP (D-040, añadido de Q-IAP-1); Firebase y el backend en AWS `sa-east-1` (D-035, D-039); la banda `<13` sin SDK (D-033, añadido del 2026-10-23), y MAX con UMP y Play Games en integración (D-042). **Solo documentación: todo es borrador.** Nada se publica, se sube ni se envía a Play Console sin el dueño, y nada se sube con el `applicationId` definitivo antes del dictamen de marca (`CLAUDE.md`, regla 6; 08 §13.1 #1; `docs/legal/marca.md`).

- **Canon:** 07 §10 (assets de tienda), 08 §11 (eliminación de cuenta), §13.1 (#2, #5-#12, #23), §13.3 (Data Safety por SDK) y §14.3 (regresión), 05 §7 (cumplimiento), glosario §1-§12, D-033. El GDD no se ha tocado: lo que choca o falta va como pregunta (sección 3).
- **Fuente de verdad de los textos legales:** la web, `infra/web/src/content/{es-419,en,pt-BR}/` (privacidad, términos, borrado y créditos). Aquí no se duplican (sección 1).
- **Marcas:** `[PENDIENTE: dueño]` (cuentas, datos de Parallext, LLC, `applicationId`, correo, nombres de personas); `[PENDIENTE: abogado]` (textos legales, bases legales, plazos); **pendiente de revisión** = lo cultural, que revisa el asesor (07 §8.2); [P] propuesta de este bloque; [NV] dato de Google Play que el GDD no da y que el dueño contrasta con Play Console vigente; [E] estimación.
- **Idiomas:** es-419 era el predeterminado (08 §13.1 #1). **Desde D-057 (P9 (a)), el predeterminado es en-US** (`en/ficha.md`), con es-419 y pt-BR como traducciones y es-ES y pt-PT como copias de esas dos (nota del 2026-10-28). En EN y PT-BR, lo que no es canon del glosario o de 07 §10 es **traducción con IA pendiente del revisor nativo**.
- **«Amazon»:** ningún identificador ni nombre propio la lleva (`CLAUDE.md`; `.claude/rules/data-json.md`). Solo sale de forma descriptiva («cuenca amazónica», «el agua del Amazonas», como el canon de 07 §10.2) y en la línea de independencia de 08 §13.1 #5.

## 1. Archivos

| Archivo | Qué es | Firma o decide |
|---|---|---|
| `es-419/ficha.md` | Ficha de Play: título, descripciones, novedades, textos de capturas, ficha de México | Dueño; asesor (lo cultural) |
| `en/ficha.md` | Ficha EN | Dueño; revisor nativo; asesor |
| `pt-BR/ficha.md` | Ficha PT-BR y ficha de Brasil | Dueño; revisor nativo; asesor; abogado brasileño |
| `data-safety.md` | Formulario «Seguridad de los datos» en su forma final para la 1.0 (2026-10-28: AdMob, Unity Ads, Firebase, AWS, Play Billing, Age Signals, Play Games, notificaciones y la Liga; sin MAX) | Abogado (con D-057, investigación propia); dueño lo envía |
| `iarc.md` | Cuestionario de clasificación IARC | Dueño; abogado brasileño (ClassInd) |
| `privacidad.md` | **Puntero** a la política de la web (`infra/web/src/content/*/privacy.html`). El borrador de B13 queda en la historia de git | **Abogado** (sobre la web) |
| `eliminar-cuenta.md` | **Puntero** a la página de borrado de la web (`infra/web/src/content/*/delete.html`) | Abogado (sobre la web) |
| `app-ads.txt` | **Puntero** a la plantilla que publica la web (`infra/web/src/static/app-ads.txt`), hoy con las líneas activas de AdMob y Unity Ads (D-049) | Dueño (IDs de las redes que se sumen a la mediación) |
| `assets-tienda.md` | Encargo al artista: icono, gráfico, 24 capturas, vídeo y logotipo, con sus textos | Dueño lo envía; asesor; abogado (logotipo) |
| `creditos.md` + `strings-creditos.csv` | Contenido de la pantalla de Créditos del juego y sus 16 claves de texto (la web tiene su propia página de créditos, con el mismo contenido) | Dueño (nombres, con consentimiento); asesor |
| `../../LICENSES.md` | Licencias de terceros: fuentes, 173 audios CC0, paquetes y SDK previstos | Abogado |
| `../qa/regresion-release.md` | Lista de regresión de release, 60 casos | — |

**Por qué la privacidad, el borrado y `app-ads.txt` son ahora punteros:** la web ya existe y es lo que leen el jugador, el abogado y Play Console. Mantener aquí otra copia la haría divergir, como ya pasó (el borrador de B13 hablaba de Unity IAP y de un «ID de jugador» que ya no existen). Los borradores del Spark se leen con `git show 964b64fb:docs/store-listing/<archivo>`.

## 2. Longitudes medidas (Unicode NFC)

| Campo (límite) | es-419 | en | pt-BR |
|---|---|---|---|
| Título (30) | 21 | 26 | 25 |
| Descripción corta (80) | 78 | **74** | 73 |
| Descripción completa (4.000), medida el 2026-10-23 | 2.487 | 2.337 | 2.411 |
| Descripción completa (4.000), el bloque entero, medida el 2026-10-28 | 2.670 | 2.502 | 2.612 |
| Descripción completa sin la línea del asesor | 2.548 | 2.404 | 2.493 |
| Descripción completa sin el asesor ni la Liga | 2.365 | 2.239 | 2.292 |
| Novedades (500) | 143 | 145 | 134 |

**Medidas de nuevo el 2026-10-28**, tras sacar la marca `[[ABOGADO: revisar]]` del bloque de la Liga (columnas en orden: es-419, en (en-US), pt-BR). La descripción creció desde el 23-10 por la línea de la Liga (D-046). es-ES y pt-PT son copias de es-419 y pt-BR: miden lo mismo. «Sin la línea del asesor» es lo que se pega si no hay su consentimiento (Q-B13-12 (a)); «sin el asesor ni la Liga», si la Liga no va encendida en producción.

Medidas de nuevo el 2026-10-23, tras poner en EN y PT-BR los nombres de D-033 (puntos 3, 4 y 8). El glosario §1.3 dice que la descripción corta EN mide 77; medida aquí (NFC, sin comillas), mide 74. Solo cambia la cifra, no el texto. Lo anota el integrador cuando toque el glosario.

## 3. Preguntas para el dueño (y el abogado)

**Las 23 se decidieron con su recomendada (a) el 2026-10-20** («las preguntas dejemos las recomendadas»: `docs/dev/decisiones.md`, D-033, añadido del 2026-10-20, «preguntas de B13, B6, B3 y B2 del Spark»). De las 23, **15 están cerradas**: 10 por esa misma decisión, ya aplicada en estos borradores (5, 6, 7, 8, 10, 11, 13, 14, 15 y 23), y 5 por decisiones posteriores o por la web (1 y 2, D-041; 18, la web; 20, D-040; 21, D-035). Las otras 8 siguen **abiertas**: la decisión está tomada, pero falta un dato, una cuenta o el abogado. Las Q-B13-24 y Q-B13-25 son nuevas de esta puesta al día, con su recomendada aplicada en el borrador **a la espera del sí del dueño y del abogado**. Se conserva la numeración.

### 3.1 Abiertas

| Id | Pregunta | Decidido (a) | Qué falta | Quién |
|---|---|---|---|---|
| Q-B13-3 | Correo de contacto, de privacidad y del DPO («encarregado») | Buzones del dominio canónico (soporte y privacidad separados) y un DPO designado por el abogado | Los datos de Parallext, LLC que D-041 lista como faltantes (correos y persona o cargo de protección de datos). **Resuelto (D-041, añadido del 2026-10-25):** `support@tintalu.com`, `privacy@tintalu.com` y encarregado Nir Levin Bermúdez Perea | Dueño |
| Q-B13-4 | Idiomas de la política de privacidad | El abogado aprueba es-419 y después se traduce | La web ya tiene EN y PT-BR traducidos con IA, como **borrador** (igual que los textos legales del juego, D-033, añadido del 2026-10-22, punto 7). Falta que el abogado apruebe es-419 y revise las traducciones antes del lanzamiento en esos idiomas | Abogado |
| Q-B13-9 | El manifiesto incluye `com.unity.modules.unityanalytics` | build-release comprueba que Unity Analytics y Cloud Diagnostics están apagados en release, y Data Safety no lleva fila de Unity | **Cerrada el 2026-10-28** (añadido de D-040). En el AAB de release (run 40662873429) no hay `UnityConnectSettings` ni hosts de datos de Unity: Analytics, Cloud Diagnostics, Performance Reporting y la telemetría del motor no tienen configuración ni a dónde enviar. Data Safety no lleva fila de Unity por el runtime (`data-safety.md` §4). Compuerta: `ci/check-no-unity-telemetry.ps1` en `ci.yml` y `release.yml` | build-release |
| Q-B13-12 | Línea del asesor cultural en la descripción larga | Con su nombre, solo con su consentimiento por escrito | El asesor y su consentimiento | Dueño |
| Q-B13-16 | Líneas de `app-ads.txt` | Copiarlas del panel de AppLovin MAX | La cuenta de MAX y la integración de D-042; hoy la plantilla de la web no declara ningún vendedor. **Nota del 2026-10-28:** con D-049 la plantilla ya lleva activas las líneas de AdMob (`google.com, pub-2916158449453694, DIRECT, f08c47fec0942fa0`) y de Unity Ads (`unity.com, 20067668703382, DIRECT`); MAX no se declara. Falta que la vea Google tras publicar (08 §13.1 #23) y sumar Meta y Mintegral cuando entren | Dueño |
| Q-B13-17 | Pix (Brasil) y OXXO (México) en las fichas por país | Solo tras comprobar en Play Console que se ofrecen al lanzar | La app en Play Console (ya sin dictamen, D-047) | Dueño |
| Q-B13-19 | Retención de Firebase Analytics | La mínima que permita el panel, por minimización | Configurarla en el proyecto de Firebase de prod y anotarla en la web (privacidad, sección 9, hoy `[[DATO DEL DUEÑO]]`). **Nota del 2026-10-28:** hecha en Firebase prod (`tintalu`): eventos 2 meses y usuario 14 meses (D-035, añadido del 2026-10-27); la web dice «2 meses». La exportación a BigQuery queda como decisión del dueño (09, «Preguntas abiertas», 7: paneles nativos hasta confirmar la cuota; `docs/dev/publicar-play-console.md` §19, duda 5) | Dueño |
| Q-B13-22 | Partida de Play Games y datos del teléfono cuando el borrado llega por la web | Decir que se borran la próxima vez que se abre el juego y que el jugador puede borrar su perfil de Play Games | La web deja el texto como `[[ABOGADO]]`, y Play Games aún no está en el juego (D-042): el texto se completa al integrarlo. **Nota del 2026-10-28:** Play Games 2.3.0 ya está en el código (D-042; 08 §13.3); se activa con el `APP_ID` del proyecto de Play Games (`docs/dev/play-games.md` §9) | Abogado (con D-057, investigación propia) |
| Q-B13-24 [nueva; pendiente del sí del dueño y del abogado] | Data Safety: la dirección IP que guardan 30 días los registros de acceso del backend y de la web (08 §13.3, fila «Backend propio»; privacidad de la web, sección 9) | **(a) recomendada, aplicada en el borrador:** no se declara como un tipo aparte, porque solo sirve para seguridad y no se deriva ninguna ubicación; lo confirma el abogado; (b) declararla como «Ubicación aproximada» | El abogado | Abogado |

### 3.2 Cerradas

| Id | Pregunta | Cómo se cerró |
|---|---|---|
| Q-B13-1 | Dominio y ruta de la página de borrado | **D-041:** `tintalu.com`, del dueño. La web aplica la (a): `/tintalu/eliminar-cuenta/`, `/tintalu/en/delete-account/` y `/tintalu/pt-br/excluir-conta/` (`infra/web/src/site.ts`). Conectar el dominio es un paso aparte del dueño (D-041; D-034) |
| Q-B13-2 | Titular legal y desarrollador en Play | **D-041:** Parallext, LLC (parallext.com), sin ninguna forma de «Amazon». La consecuencia que queda (la cuenta de Play Console está a nombre de AUTOMATION AI S A S) la decide el dueño según D-041, «Consecuencias». **Decidida en D-048 (2026-10-25):** AUTOMATION AI S.A.S publica en Play y cobra; Parallext desarrolla y responde de los datos |
| Q-B13-5 | Texto de las capturas «en papel marfil» frente a D-003 | D-033, añadido del 2026-10-20, (a): placa de madera clara o de vidrio del UI kit (`assets-tienda.md` §1) |
| Q-B13-6 | Captura 4 en EN y PT-BR | D-033, añadido del 2026-10-20, (a): la propuesta de B13 hasta el revisor nativo |
| Q-B13-7 | Número de niveles en la ficha | D-033, añadido del 2026-10-20, (a): no se da en la 1.0 (fichas, notas de §3) |
| Q-B13-8 | Peso de descarga en la ficha | D-033, añadido del 2026-10-20, (a): no se da hasta medir el AAB de release con los SDK |
| Q-B13-10 | «Sin wifi» frente a videos y compras | D-033, añadido del 2026-10-20, (a): se mantiene el canon y se aclara en la descripción larga. Con D-042, la frase de los videos solo vale si el build lleva MAX (fichas, notas de §3). Nota del 2026-10-28: el build lleva AdMob (D-049), así que la frase vale |
| Q-B13-11 | IARC: la Piraña muerde una cuia | D-033, añadido del 2026-10-20, (a): «sin violencia» (`iarc.md` §2) |
| Q-B13-13 | Cómo se acredita el código escrito por agentes de IA | D-033, añadido del 2026-10-20, (a): «Parallext, LLC, con ayuda de herramientas de inteligencia artificial», como ya dice la web (`credits.html`). El dueño puede ajustar la redacción |
| Q-B13-14 | Créditos y licencias de terceros | D-033, añadido del 2026-10-20, (a): botón «Licencias de terceros» con las OFL y el agradecimiento CC0 (`creditos.md` §2). Qué avisos de cada SDK hay que mostrar sigue en `[[ABOGADO]]` en la web |
| Q-B13-15 | Dónde se guardan los PNG y PSD de la tienda | D-033, añadido del 2026-10-20, (a): `docs/store-listing/assets/<idioma>/` por Git LFS |
| Q-B13-18 | Términos de servicio | La web ya los tiene en borrador (`infra/web/src/content/*/terms.html`), con Parallext, LLC como titular (D-041). Los revisa el abogado |
| Q-B13-20 | Unity Authentication, dependencia de Unity IAP, sin fila en 08 §13.3 | **Obsoleta por D-040 (añadido de Q-IAP-1):** Unity IAP y sus dependencias de Unity Services salen del juego, sustituidos por el plugin propio de Play Billing 9.0.0. La fila de 08 §13.3 es ahora solo «Google Play Billing», con Q-BILL-2 |
| Q-B13-21 | Cómo sabe la web que quien pide el borrado es el dueño | **D-035** (añadido de F1-F2a) y 08 §11: un código de cuenta aleatorio de 128 bits, distinto del UID, del que el servidor solo guarda el hash, que rota y se confirma en 5 minutos, con límites por ruta y por código. Ajustes lo muestra con guiones y la web lo normaliza (D-033, añadido del 2026-10-22, punto 11). El «ID de jugador» del borrador es ahora el **código de cuenta** |
| Q-B13-23 | Captura 2, «14 frutas, 14 colores» | D-033, añadido del 2026-10-20, (a): «14 colores de frutas y ríos» / «14 colors from fruit and rivers» / «14 cores de frutas e rios» (fichas §6; `assets-tienda.md` §4) |
| Q-B13-25 | 08 §13.3 no tenía fila para Google UMP, que llegaba con MAX en D-042 (recomendada (a): cubrirlo con la fila del SDK de anuncios) | **Cerrada el 2026-10-28:** 08 §13.3 pone UMP 4.0.0 dentro de la fila de Google Mobile Ads (D-049 y su añadido del 2026-10-25); `data-safety.md` §5 |

### 3.3 Historia: las 23 tal como las planteó B13

El enunciado completo, con todas las opciones, está en `git show 964b64fb:docs/store-listing/README.md`, sección 3.

Ya tienen dueño fuera de este bloque y no se repiten aquí: Crashlytics y Analytics antes del consentimiento (08 pregunta 6, abogado), la segunda fuente de edad en Brasil (05 §7, fase 5), el plazo fiscal del libro mayor (08 §11, abogado), la pregunta 8 de 07 (vídeo de 30 s o de 15-20 s) y la 3 de 07 (icono A como control).

## 3 bis. Hallazgos del audio

- **Música:** toda la música `mus_*` del manifiesto (lechos, percusión y nocturnos de los mundos 1-9, y `mus_menu`, `music/menu.wav`, el tema de menú que 07 §10.3 pone en el vídeo) es **CC0** de la entrega 1, no el encargo original con cesión total de 07 §9.5. Recomendación [P]: **el vídeo de la ficha y los creativos no la usan** hasta que haya una pista con cesión total.
- `amb_w01_fauna_02` lleva voces humanas de una favela de Río de Janeiro, contra 07 §9.5 y §9.1, y el autor del manifiesto usa la marca («Tintalú Sound Design»): detalle en `LICENSES.md` §2.

## 4. Enganches para el integrador (no aplicados en esta rama)

0. **`docs/qa/regresion-release.md`, RR-60:** hecho en esta rama (código de cuenta y Q-B13-21 resuelta).
1. **`Assets/_Project/Data/Loc/strings.csv`:** las 16 filas de `strings-creditos.csv` (`credits_*`), ya en orden ordinal y con la cabecera de 08 §4.7, cuando exista la pantalla de Créditos. Se prueba con los tests de las tablas de textos.
2. **Glosario §1.3:** la cifra de la descripción corta EN (77 → 74; sección 2).
3. **GDD, con lo ya decidido:** 07 §10.2 (Q-B13-5, Q-B13-6 y Q-B13-23); 08 §11 ya recoge Q-B13-21 y Q-B13-1 (falta Q-B13-22, cuando el abogado cierre el texto); 08 §13.3: la fila de Crashlytics antes de la pantalla de edad, UMP (Q-B13-25) y el runtime de Unity (Q-B13-9). Q-B13-20 ya no pide nada (D-040).
3 bis. **La web (`infra/web/`):** la cabecera de `app-ads.txt` y el matiz de la banda de edad y el consentimiento en la privacidad ya están hechos en esta rama. Al integrar D-042, privacidad (secciones 2, 3 y 5), términos (sección 6) y créditos de la web, a la vez que `data-safety.md` y 08 §13.3. (Nota del 2026-10-28: los anuncios llegaron con AdMob, D-049, y la web se puso al día en `tanda6/ssv-admob-web`, `docs/dev/anuncios-admob.md` §9; `data-safety.md` ya está en su forma final.)
4. **`docs/dev/traspaso/spark-estado.md`:** la entrada de B13 («listo para integrar»), que el coordinador de la tanda escribe al integrar.

## 5. Lo que este bloque no hace

- No crea cuentas, dominios, hosting, formularios ni `functions/` (`deleteAccount`), ni toca Play Console.
- No hace arte: el encargo es para el artista; los creativos de UA (07 §10.4) quedan fuera.
- No escribe textos legales: viven en la web (`infra/web/src/content/*`), que hizo el equipo de Claude (D-041).
- El `applicationId` es `com.parallext.tintalu` (D-047). El desarrollador ya está fijado: Parallext, LLC (D-041).
