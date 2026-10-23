# Tintalú: paquete para revisión de operación y lanzamiento

23-10-2026 · Preparado, **no enviado**. El dueño aplazó el registro de marca y sus gastos. Este paquete prioriza contratos, privacidad y consumo; no encarga una solicitud de marca ni constituye presupuesto autorizado.

## 1. Caso y documentos

Juego móvil Android de ordenar colores, con anuncios, compras y cuenta técnica anónima. Estudio **PARALLEXT, LLC**, constituido en Wyoming, con dirección declarada en Florida y dirección efectiva informada por el dueño desde Colombia. Publicación/cobros inicialmente mediante **AUTOMATION AI S.A.S**, de Bogotá. El dueño quiere disponibilidad mundial. La investigación y los borradores previos estaban orientados principalmente a Latinoamérica y EE. UU.

Se revisaron copia de Articles/Certificate of Organization, carta IRS CP575G y RUT. Los originales y la extracción detallada se mantienen fuera de Git. Pedirlos al dueño por canal apropiado si se contrata la revisión. No incorporar sus datos personales o fiscales innecesariamente a una web.

- [Estructura propuesta y decisiones](estructura-publicacion.md).
- [Bases de la licencia entre empresas](acuerdo-publicacion-borrador.md).
- [Inventario exacto de marcadores, con archivo y línea](inventario-web.md).
- [Investigación previa de marca, parcial](investigacion-marca-2026-10.md).
- `infra/web/src/content/es-419/`: privacidad, términos, borrado y créditos; versiones en inglés y portugués pendientes de revisión lingüística.
- `docs/store-listing/`: borradores de Data Safety, privacidad, borrado y materiales de tienda.
- `docs/dev/anuncios-max.md` y `docs/dev/iap.md`: hechos de implementación y dudas de SDK/pagos.

## 2. Respuestas necesarias para la operación

1. ¿La licencia propuesta da a la S.A.S derechos suficientes de distribución y monetización propia para DDA §11.1, además de licencias a Google y al usuario? ¿Quién presta contractualmente cada servicio y responde por compras y reclamaciones?
2. ¿Quién puede firmar por cada entidad y qué documentos faltan? Revisar cesiones de código, audio, arte, dominios y aportaciones anteriores a constituir la LLC. La asignación de EIN no acredita por sí sola una cadena de derechos ni todas las facultades de firma.
3. Conforme a la operación real de Parallext desde Colombia, ¿qué tratamientos corresponden a la LLC, cuáles a la editora y qué anexos de encargo o cooperación hacen falta? No atribuir corresponsabilidad general por comodidad.
4. ¿Qué obligaciones territoriales activan la oferta mundial, la dirección desde Colombia y el acceso desde terceros países? Delimitar UE/EEE, Reino Unido, Suiza, Brasil, México, Perú, Colombia y jurisdicciones adicionales; representantes, información local y transferencias donde corresponda. Tres traducciones no prueban cobertura jurídica mundial.
5. ¿Qué bases legales sostienen cada finalidad y cada SDK, especialmente antes de saber la edad? Verificar adolescencia, compras de menores, COPPA/Familias cuando corresponda y régimen brasileño vigente. Una pantalla 13+ o un estilo visual no resuelven por sí solos la clasificación del público.
6. ¿Qué plazos reales de respuesta, retención fiscal y borrado se pueden prometer? Distinguir anonimización de seudonimización; un UID/hash puede seguir siendo dato personal. Verificar restauraciones, backups, registros y el riesgo residual de las lecturas eventualmente consistentes del backend.
7. Revisar cláusulas de pérdida de compras/reembolsos, suspensión, retirada de premios, cierre del juego y modificaciones de términos frente a derechos imperativos de consumo. El texto actual «no se reembolsan» requiere matiz y revisión.
8. Con el contador: contratos entre entidades, facturación, retenciones y remuneración, régimen SIMPLE declarado de la S.A.S y efectos de dirección de la LLC desde Colombia. No se concluye residencia fiscal ni tratamiento tributario en este paquete.

## 3. Preguntas técnicas que requieren evidencia además de asesoría

| ID | Tema | Evidencia / resultado necesario |
|---|---|---|
| Q-B13-24 | IP en logs durante 30 días; borrador no la declara como tipo separado | Integrador documenta uso y derivación o no de ubicación; revisar categoría aplicable de Data Safety con guía oficial. «Solo seguridad» no basta para omitir un dato si debe declararse. |
| Q-B13-25 | Google UMP sin fila propia, cubierto provisionalmente con MAX | Verificar datos del SDK y guía de divulgación; no afirmar «UMP: nada» ni asumir que la fila MAX lo cubre completamente. |
| Q-MAX-6 | 13-17 en EEE, Reino Unido y Suiza | Código actual evita iniciar MAX en ese perfil; evaluar solución conforme a consentimiento/edad antes de cambiarlo. No confundir anuncios no personalizados con ausencia de consentimiento exigible. |
| Q-BILL-2 | Diagnósticos de Play Billing enviados a Google | Precisar contenido, finalidades, roles y declaración; contrastar librería realmente integrada. |
| Q-B13-22 | Borrado web y datos locales/Play Games | Verificar qué se borra, cuándo y qué requiere volver a abrir; no prometer borrado remoto de datos locales que no existe. |

Otros puntos: rol de Google por producto (Billing no es automáticamente un mero encargado); SetUserId global de MAX; AD_ID según versión de Android; redes/adaptadores efectivamente instalados; retención Analytics y registro de consentimiento. Estos hechos los aporta el integrador: un abogado no puede suplir con redacción una comprobación técnica pendiente.

## 4. Marca: expediente reservado para después

Titular previsto: PARALLEXT, LLC, sujeto a cadena de derechos. Signos: **Tintalú** denominativo, **TiTino** por separado si se decide y logo cuando se seleccione. Sin solicitudes en este encargo.

Especificación orientativa para una consulta futura: clase 9, «software descargable de juegos para dispositivos móviles»; clase 41, servicios de juego en línea solo si la oferta real los justifica. No tramitar 28 por haberla incluido en búsquedas; corresponde revisarla si hay juguetes/merchandising. Clase 2 fue búsqueda de antecedentes, no propuesta de registro del videojuego. Confirmar términos aceptados por cada oficina y uso/intención reales.

Conservar las ocho cuestiones de la investigación, sin considerarlas resueltas:

1. Tintalú como conjunto frente a EL TINTAL 2586508 y TINTALAIRE 1971738, alcance e historial.
2. Uso previo de Tinta — Ball Sort Puzzle y otros juegos Tinta, territorios y similitudes.
3. Brasil 2/9/28/41, variantes fonéticas y designaciones internacionales.
4. Colombia/Perú: afinidad, derechos por uso y expedientes no vigentes.
5. TiTino, variantes y búsqueda figurativa del personaje.
6. Titular y cadena de derechos entre LLC, S.A.S y autores.
7. Oficina de origen y Madrid frente a vías nacionales, dependencia de cinco años y coste integral. La LLC estadounidense no obliga a presentar primero en USPTO en todo supuesto; analizar vínculos y estrategia. Dirección desde Colombia importa para representación ante USPTO.
8. Necesidad real de clases adicionales y derechos fuera de las clases buscadas.

Antecedentes y enlaces de expediente: [investigación §§3 y 4](investigacion-marca-2026-10.md). Los resultados no completados son **no verificados**, nunca «libres». El 23-10-2026 pePI agotó el tiempo; el nuevo buscador INPI se mostró como entorno de prueba con actualización 07-07-2026 y devolvió error al buscar TINTALU. WIPO/TMview cargaron sus interfaces, pero no se completó una consulta verificable antes de que el dueño aplazara este frente. No se superaron CAPTCHA.

## 5. Correo listo para que el dueño lo envíe si decide contratar

**Asunto:** Revisión acotada de publicación y privacidad de videojuego — LLC de Wyoming y S.A.S colombiana

Buenos días:

Estamos preparando Tintalú, un juego Android con compras y anuncios. PARALLEXT, LLC desarrolla y opera el juego desde Colombia; inicialmente AUTOMATION AI S.A.S lo publicaría y monetizaría mediante su cuenta de Google Play. Queremos evaluar disponibilidad mundial y formalizar una licencia real entre ambas empresas compatible con el DDA de Google.

Solicitamos propuesta separada para: (1) licencia, facultades y cadena de derechos; (2) privacidad y datos entre entidades/proveedores; (3) términos, compras de menores, consumo y eliminación de cuentas. Podemos aportar los documentos societarios, borradores y evidencia técnica de SDK y flujos. Necesitamos que indiquen países cubiertos, exclusiones, entregables, plazo, honorarios, impuestos y posibles apoyos locales.

Por ahora hemos aplazado las solicitudes de registro de marca. No incluyan su presentación ni tasas como servicio autorizado. Si detectan un conflicto de nombre relevante para operar, indíquennos su alcance y el presupuesto separado de revisión antes de actuar.

Por favor confirmen también si coordinan el análisis con asesoría contable colombiana/estadounidense cuando corresponda.

Gracias.

## 6. Fuentes oficiales principales

- [Google Play: identidad de organización](https://support.google.com/googleplay/android-developer/answer/13628312).
- [Google Play DDA, en particular §11](https://play.google/developer-distribution-agreement.html).
- [Google Play User Data](https://support.google.com/googleplay/android-developer/answer/10144311).
- [Ley 1581 de 2012](https://www.funcionpublica.gov.co/eva/gestornormativo/norma.php?i=49981).
- [USPTO: domicilio y representación](https://www.uspto.gov/trademarks/laws/trademark-rule-requires-foreign-applicants-and-registrants-have-us).
- [Madrid y oficinas de origen](https://www.uspto.gov/ip-policy/international-protection/madrid-protocol/outbound-applicants).

No se seleccionaron ni contactaron despachos. La selección de abogados de registro, el presupuesto Madrid/nacional y las solicitudes se aplazan por la instrucción más reciente del dueño.
