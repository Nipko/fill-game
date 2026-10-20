# Borradores de publicación (bloque B13 de la tanda 5)

Encargo: `docs/dev/plan-tanda5.md` §4, fila B13 (equipo Spark, 2026-10-20; rama `spark/t5-b13`). **Solo documentación: todo es borrador.** Nada se publica, se sube ni se envía a Play Console sin el dueño, y nada se sube con el `applicationId` definitivo antes del dictamen de marca (`CLAUDE.md`, regla 6; 08 §13.1 #1; `docs/legal/marca.md`).

- **Canon:** 07 §10 (assets de tienda), 08 §11 (eliminación de cuenta), §13.1 (#2, #5-#12, #23), §13.3 (Data Safety por SDK) y §14.3 (regresión), 05 §7 (cumplimiento), glosario §1-§12, D-033. El GDD no se ha tocado: lo que choca o falta va como pregunta (sección 3).
- **Marcas:** `[PENDIENTE: dueño]` (cuentas, estudio, `applicationId`, correo, dominio, nombres de personas); `[PENDIENTE: abogado]` (textos legales, bases legales, plazos); **pendiente de revisión** = lo cultural, que revisa el asesor (07 §8.2); [P] propuesta de este bloque; [NV] dato de Google Play que el GDD no da y que el dueño contrasta con Play Console vigente; [E] estimación.
- **Idiomas:** es-419 es el predeterminado (08 §13.1 #1). En EN y PT-BR, lo que no es canon del glosario o de 07 §10 es **traducción con IA pendiente del revisor nativo**.
- **«Amazon»:** ningún identificador ni nombre propio la lleva (`CLAUDE.md`; `.claude/rules/data-json.md`). Solo sale de forma descriptiva («cuenca amazónica», «el agua del Amazonas», como el canon de 07 §10.2) y en la línea de independencia de 08 §13.1 #5.

## 1. Archivos

| Archivo | Qué es | Firma o decide |
|---|---|---|
| `es-419/ficha.md` | Ficha de Play: título, descripciones, novedades, textos de capturas, ficha de México | Dueño; asesor (lo cultural) |
| `en/ficha.md` | Ficha EN | Dueño; revisor nativo; asesor |
| `pt-BR/ficha.md` | Ficha PT-BR y ficha de Brasil | Dueño; revisor nativo; asesor; abogado brasileño |
| `data-safety.md` | Formulario «Seguridad de los datos»: hoy (sin SDK) y la 1.0 prevista | Abogado; dueño lo envía |
| `iarc.md` | Cuestionario de clasificación IARC | Dueño; abogado brasileño (ClassInd) |
| `privacidad.md` | Política de privacidad (es-419), con los anexos de México y Brasil | **Abogado** |
| `eliminar-cuenta.md` | Página web de eliminación de cuenta (es-419, en, pt-BR) | Abogado; dueño (URL y hosting) |
| `app-ads.txt` | Plantilla de `app-ads.txt`, toda comentada | Dueño (IDs de las redes) |
| `assets-tienda.md` | Encargo al artista: icono, gráfico, 24 capturas, vídeo y logotipo, con sus textos | Dueño lo envía; asesor; abogado (logotipo) |
| `creditos.md` + `strings-creditos.csv` | Contenido de la pantalla de Créditos y sus 16 claves de texto | Dueño (nombres, con consentimiento); asesor |
| `../../LICENSES.md` | Licencias de terceros: fuentes, 173 audios CC0, paquetes y SDK previstos | Abogado |
| `../qa/regresion-release.md` | Lista de regresión de release, 60 casos | — |

## 2. Longitudes medidas (Unicode NFC)

| Campo (límite) | es-419 | en | pt-BR |
|---|---|---|---|
| Título (30) | 21 | 26 | 25 |
| Descripción corta (80) | 78 | **74** | 73 |
| Descripción completa (4.000) | 2.487 | 2.346 | 2.431 |
| Novedades (500) | 143 | 145 | 134 |

El glosario §1.3 dice que la descripción corta EN mide 77; medida aquí (NFC, sin comillas), mide 74. Solo cambia la cifra, no el texto. Lo anota el integrador cuando toque el glosario.

## 3. Preguntas para el dueño (y el abogado)

Cada una con sus opciones; la recomendada va primero y marcada. Son 23 (Q-B13-1 a Q-B13-23).

| Id | Pregunta | Opciones (recomendada: **a**) | Afecta a |
|---|---|---|---|
| Q-B13-1 | Dominio y ruta de la página de eliminación de cuenta. 08 §11 dice `https://<dominio del estudio>/tintalu/eliminar-cuenta`; 08 §13.1 #2 la pone en el canónico `tintalu.com` [P], que `docs/legal/marca.md` §4.1 da como «pendiente de confirmar» | **(a)** `tintalu.com` como canónico y la ruta literal del GDD, `https://tintalu.com/tintalu/eliminar-cuenta` (sin cambiar el GDD), con `/tintalu/en/delete-account` y `/tintalu/pt-br/excluir-conta`; (b) `https://tintalu.com/eliminar-cuenta` (corrige 08 §11); (c) un dominio del estudio con `/tintalu/…` | Data Safety, política, ficha, web |
| Q-B13-2 | Titular legal (responsable del tratamiento) y nombre del desarrollador en Play (08 pregunta 1) | **(a)** una entidad del estudio, con un nombre sin ninguna forma de «Amazon», que decide el dueño con el abogado; (b) el dueño como persona física hasta crear la entidad | Política, ficha, Créditos, IARC |
| Q-B13-3 | Correo de contacto, de privacidad y del DPO («encarregado») | **(a)** buzones del dominio canónico (soporte y privacidad separados) y un DPO designado por el abogado; (b) un solo buzón para todo; (c) un correo personal | Ficha, IARC, política, web |
| Q-B13-4 | Idiomas de la política de privacidad | **(a)** el abogado aprueba la versión es-419 y después se traduce a PT-BR y EN (08 §13.1 #2 pide las tres); (b) traducir ya el borrador | Política |
| Q-B13-5 | 07 §10.2 pone el texto de las capturas «en papel marfil», y D-003 descarta el papel | **(a)** placa de madera clara o de vidrio del UI kit de D-003; (b) mantener el papel marfil como excepción de la tienda; (c) sin placa, texto con contorno | Capturas |
| Q-B13-6 | Texto de la captura 4 en EN y PT-BR («pendiente del traductor» en 07 §10.2) | **(a)** usar la propuesta de B13 («Follow the Amazon to its source» / «Suba o Amazonas até onde ele nasce») hasta el revisor nativo; (b) esperar al traductor | Capturas, fichas |
| Q-B13-7 | ¿La ficha da el número de niveles? (700 instalados, ≥ 600 con QA completa; 08 Desviación 7) | **(a)** no darlo en la 1.0, como el borrador; añadir «700 niveles» solo con la build final con QA de los 700; (b) «más de 600 niveles»; (c) «700 niveles» ya | Descripciones |
| Q-B13-8 | ¿La ficha da el peso de descarga (≤ 80 MB; hoy, AAB de 35 MB)? | **(a)** no, hasta medir el AAB de release con los SDK; (b) «menos de 80 MB» | Descripciones |
| Q-B13-9 | El manifiesto incluye `com.unity.modules.unityanalytics` | **(a)** build-release comprueba en B10 que Unity Analytics y Cloud Diagnostics están apagados en el build de release, y Data Safety no lleva fila de Unity; (b) quitar el módulo (cambio de `Packages/manifest.json`: aprobación del dueño); (c) declararlo | Data Safety |
| Q-B13-10 | «Juega sin wifi» (captura 8) y «Relajante y sin wifi» (descripción corta, canon) cuando los videos y las compras necesitan red | **(a)** mantener el canon y aclararlo en la descripción larga («La campaña se juega sin conexión. Los videos opcionales y las compras sí necesitan internet»), como el borrador; (b) cambiar la descripción corta | Fichas, capturas |
| Q-B13-11 | IARC: la Piraña muerde una cuia | **(a)** «sin violencia»: muerde un objeto, nunca a un personaje; (b) declarar violencia leve de fantasía | IARC |
| Q-B13-12 | Línea de crédito del asesor cultural en la descripción larga (07 §8.2) | **(a)** una línea al final con su nombre, solo con su consentimiento por escrito; (b) sin nombre («con revisión cultural de un asesor de la región»); (c) sin línea | Fichas |
| Q-B13-13 | Cómo se acredita en Créditos el código escrito por agentes de IA | **(a)** la sección «Programación» con el estudio y una línea «con ayuda de herramientas de IA» [P]; (b) sin mención; (c) nombrar la herramienta | Créditos |
| Q-B13-14 | Créditos y licencias de terceros | **(a)** botón «Licencias de terceros» con el texto de las OFL (lo pide la OFL al distribuir) y una línea de agradecimiento a las fuentes CC0; (b) solo las OFL; (c) las licencias en la web, no en el juego | Créditos, `LICENSES.md` |
| Q-B13-15 | Dónde se guardan los PNG y PSD de la tienda | **(a)** `docs/store-listing/assets/<idioma>/` por Git LFS (fuera de `Assets/`, así no entran en el build); (b) un repositorio o carpeta aparte; (c) `Assets/_Project/Art/Store/` | Artista |
| Q-B13-16 | Líneas de `app-ads.txt` | **(a)** copiarlas del panel de AppLovin MAX (que da su lista y la de las redes que media) cuando exista la cuenta; (b) escribirlas a mano con el panel de cada red | `app-ads.txt` |
| Q-B13-17 | Mencionar Pix (Brasil) y OXXO (México) en las fichas por país (08 §13.1 #6) | **(a)** solo tras comprobar en Play Console que se ofrecen en el país al lanzar; (b) ya en el borrador | Fichas por país |
| Q-B13-18 | 08 §13.1 #2 pide también **términos de servicio**, que B13 no lista | **(a)** borrador en un bloque aparte, cuando el abogado fije la jurisdicción y el titular (Q-B13-2); (b) redactarlos ya | Web, ficha |
| Q-B13-19 | Retención de Firebase Analytics (08 §13.3: «la configurada en Firebase») | **(a)** la mínima que permita el panel [NV], por minimización (LGPD; 05 §7); (b) la máxima, para cohortes largas | Data Safety, política |
| Q-B13-20 | Unity IAP 5.4.x necesita Unity Authentication (08 §7, fila 9), y 08 §13.3 no tiene fila para ella | **(a)** quien integre el IAP confirma con la guía de Unity qué datos trata y se añade la fila a Data Safety y a la política; el GDD la recoge en 08 §13.3; (b) tratarla como parte de la fila de Unity IAP | Data Safety, política |
| Q-B13-21 | Cómo sabe la página web que quien pide el borrado es el dueño del ID (08 §11 solo pide el ID de jugador visible en Ajustes) | **(a)** que el ID de Ajustes sea un código largo y aleatorio distinto del UID, más una confirmación en la página [P]; (b) borrado por correo, atendido a mano; (c) el ID tal cual | Web, `deleteAccount`, B8 |
| Q-B13-22 | Cómo se borran la partida de Play Games (`af_main`) y los datos del teléfono cuando el borrado llega por la web y el juego no está instalado (08 §11 solo dice que los borra el cliente) | **(a)** decirle al jugador que se borran la próxima vez que abra el juego y que puede borrar él mismo su perfil de Play Games (08 §13.3, fila Play Games), como el borrador; (b) que el servidor los borre (pide un acceso a Saved Games que el diseño no tiene); (c) no decir nada en la página | Web, política, Data Safety, B8 |
| Q-B13-23 | La captura 2 del canon dice «14 frutas, 14 colores» (07 §10.2), pero la paleta son 12 frutas y 2 aguas de río (glosario §9), y la 1.0 usa como mucho 12 colores por nivel (K_ruta ≤ 12; los 14 solo en el evento Encuentro de las Aguas) | **(a)** «14 colores de frutas y ríos» / «14 colors from fruit and rivers» / «14 cores de frutas e rios» [P], que es verdad y cabe en 36; (b) «12 frutas, 2 ríos, 14 colores»; (c) dejar el canon | Captura 2; 07 §10.2 |

Ya tienen dueño fuera de este bloque y no se repiten aquí: Crashlytics y Analytics antes del consentimiento (08 pregunta 6, abogado), la segunda fuente de edad en Brasil (05 §7, fase 5), el plazo fiscal del libro mayor (08 §11, abogado), la pregunta 8 de 07 (vídeo de 30 s o de 15-20 s) y la 3 de 07 (icono A como control).

## 3 bis. Hallazgos del audio

- **Música:** toda la música `mus_*` del manifiesto (lechos, percusión y nocturnos de los mundos 1-9, y `mus_menu`, `music/menu.wav`, el tema de menú que 07 §10.3 pone en el vídeo) es **CC0** de la entrega 1, no el encargo original con cesión total de 07 §9.5. Recomendación [P]: **el vídeo de la ficha y los creativos no la usan** hasta que haya una pista con cesión total.
- `amb_w01_fauna_02` lleva voces humanas de una favela de Río de Janeiro, contra 07 §9.5 y §9.1, y el autor del manifiesto usa la marca («Tintalú Sound Design»): detalle en `LICENSES.md` §2.

## 4. Enganches para el integrador (no aplicados en esta rama)

1. **`Assets/_Project/Data/Loc/strings.csv`:** las 16 filas de `strings-creditos.csv` (`credits_*`), ya en orden ordinal y con la cabecera de 08 §4.7, cuando exista la pantalla de Créditos. Se prueba con los tests de las tablas de textos.
2. **Glosario §1.3:** la cifra de la descripción corta EN (77 → 74; sección 2).
3. **GDD, según lo que decida el dueño:** 07 §10.2 (Q-B13-5, Q-B13-6, Q-B13-23), 08 §11 (Q-B13-1, Q-B13-21, Q-B13-22), 08 §13.3 (Q-B13-20).
4. **`docs/dev/traspaso/spark-estado.md`:** la entrada de B13 («listo para integrar»), que el coordinador de la tanda escribe al integrar.

## 5. Lo que este bloque no hace

- No crea cuentas, dominios, hosting, formularios ni `functions/` (`deleteAccount`), ni toca Play Console.
- No hace arte: el encargo es para el artista; los creativos de UA (07 §10.4) quedan fuera.
- No traduce la política de privacidad (Q-B13-4) ni redacta los términos (Q-B13-18).
- No fija el `applicationId` ni el nombre del desarrollador.
