# Tintalú: competencia, economía y compras pequeñas

Fecha de consulta: **2026-10-07**. Documento de revisión; **no cambia el GDD, los precios ni el código**. Las propuestas que difieren de decisiones de dirección requieren incorporarse explícitamente al canon antes de implementarse.

## Dictamen

La oportunidad de Tintalú está en ofrecer un water-sort agradable, legible y confiable con una identidad amazónica reconocible. La economía actual está mucho más especificada que su validación con jugadores. Conviene comprobar primero que las personas disfrutan pensar, equivocarse y volver a intentar; después ofrecer comodidad y personalización. No usar la falta de recursos como objetivo de éxito.

Recomiendo conservar inicialmente el rescate nominal de 600 como referencia de simulación, **sin escalarlo a 750/900**, simplificar la primera tienda a tres familias de producto y revisar dos decisiones antes de implementar comercio: el stock de Cuia extra inutilizable en bloqueo y la restauración de consumibles. El primer punto afecta la confianza; el segundo puede perder o duplicar valor comprado.

## 1. Qué evidencia tenemos

| Etiqueta | Significado | Qué permite concluir |
|---|---|---|
| **[T] Tienda/marketing** | Ficha publicada por el desarrollador en Google Play/App Store o su web, abierta en esta revisión | Identidad, descripción declarada, catálogo visible en esa tienda/región. No prueba una sesión real ni todas las ofertas disponibles |
| **[S] Soporte oficial** | Reglas publicadas por el operador | Comportamiento documentado; puede variar tras actualizaciones |
| **[R] Reseña** | Testimonio individual visible en la tienda | Señal para investigar; no demuestra frecuencia, causalidad ni algoritmo |
| **[A] Auditoría local** | GDD, simulador y salidas ya guardadas en el repo | Qué se diseñó o modeló, no cómo reaccionarán jugadores reales |
| **[H] Hipótesis/propuesta** | Juicio de diseño de esta revisión | Algo que debe ponerse a prueba |

**UX comprobada jugando en esta revisión: ninguna.** No se instalaron ni ejecutaron los competidores. No se atribuyen a ellos algoritmos de generación, adaptación al gasto, tasas de retención, conversión ni reparto real de ingresos. Los precios iOS EE. UU. son ejemplos de catálogo, no precios Android LatAm ni una recomendación automática para Tintalú.

El material interno de Magic Sort mezcla deconstrucciones, cifras de proveedores, estimaciones y reseñas. Su etiqueta `[V]` significa verificado en ese informe, no necesariamente medición primaria reproducible. En particular, “más del 50 % del IAP en derrota”, “primer intersticial nivel 22” y “ratio ideal 10–15:1” no quedaron corroborados por documentación primaria abierta en esta revisión; no deben convertirse por ello en leyes de diseño.

## 2. Seis referencias pertinentes

| Juego y papel | Evidencia actual | Lección propuesta para Tintalú y límite |
|---|---|---|
| **Magic Sort! — Grand Games**, competidor directo con meta | [T] Google Play declara anuncios, IAP, vertido por compatibilidad de color/capacidad, Undo y Shuffle. [T] App Store US muestra Starter Bundle **US$0,99**, 1.000 monedas **US$1,99** y bundles mayores. [R] Una reseña de abril de 2025 objeta no poder usar ayudas almacenadas al bloquearse. [Google Play](https://play.google.com/store/apps/details?hl=en-US&id=com.grandgames.magicsort), [App Store](https://apps.apple.com/us/app/magic-sort/id6499209744). | [H] Estudiar claridad del ciclo y presentación de la ayuda. Diferenciarse permitiendo aprovechar inventario propio al necesitarlo. La reseña no prueba el comportamiento de todas las versiones; la ficha no prueba que cobrar en derrota cause su éxito. |
| **Water Sort Puzzle — IEC Global**, referencia básica | [T] Declara anuncios/IAP, juego offline, reinicio libre y dificultad creciente. [R] Reseñas de 2026 describen dificultades para recuperar progreso y ayudas interrumpidas por anuncios. [Ficha oficial](https://play.google.com/store/apps/details?hl=en_US&id=com.gma.water.sort.puzzle). | [H] Offline, reintento y continuidad entre sesiones son parte del producto. Probar salida a la tienda, background y reconexión sin perder el tablero. No extrapolar esas reseñas a una tasa de fallos del rival. |
| **Get Color / Water Sort Puzzle: Get Color — Tripledot**, antes asociado a Zephyr | [T] La ficha iOS actual identifica Tripledot, anuncia más de 1.000 puzles y frasco adicional; lista paquete pequeño **US$1,99**, “Premium palette” **US$1,99** y varios precios de “Premium”. [App Store](https://apps.apple.com/us/app/water-sort-puzzle-get-color/id1569776448). La [web de Zephyr](https://zephyrmobile.com/get-color) todavía ofrece una descripción genérica con más de 500 niveles. | [H] Hay espacio para valor estético además de monedas. No inferir contenido exacto de “Premium” ni cuál precio ve cada jugador. Las promesas de relajación/beneficios cognitivos son marketing, no pruebas clínicas ni métricas de diversión. |
| **Water Sort Puzzle - Sort Color — Guru**, alternativa de baja presión declarada | [T] Publicita ausencia de temporizador y penalizaciones, reinicio y offline; App Store US lista Remove Ads **US$7,99**. [R] Una reseña de octubre de 2025 diferencia anuncios entre niveles de vídeos para ayudas y expresa frustración con esa distinción. [Ficha oficial](https://apps.apple.com/us/app/water-sort-puzzle-sort-color/id1597897013). | [H] Escribir exactamente qué compra la persona. “Sin anuncios obligatorios; los vídeos de premio son opcionales” debe verse antes de pagar. No asumir que su catálogo equivale a un único producto en todos los países. |
| **Hexa Sort — Lion Studios Plus**, sorting adyacente | [T] Declara apilar, ordenar y fusionar fichas hexagonales, anuncios/IAP y juego offline. Sus notas de versión promocionan mecánica, colección, pase y mundo adicionales. [R] Reseñas señalan anuncios intrusivos o premios que no llegan. [Ficha oficial](https://play.google.com/store/apps/details?hl=en_SG&id=com.gamebrain.hexasort). | [H] La satisfacción de resolver puede sostener variación visual y una colección. Su cantidad de sistemas no demuestra que Tintalú necesite un pase, eventos y ofertas encadenadas en v1. Probar primero un objetivo de colección sencillo. |
| **Royal Match — Dream Games**, referente adyacente de match-3 | [T] Su ficha declara juego sin anuncios, offline, decoración y eventos. [S] Soporte: cinco vidas, una cada **30 minutos**, ocho con Royal Pass; las monedas se obtienen jugando/eventos o comprando y pagan vidas, movimientos y boosters. [Tienda](https://play.google.com/store/apps/details?hl=en_US&id=com.dreamgames.royalmatch), [vidas](https://dreamgames.helpshift.com/hc/en/3-royal-match/faq/5-lives/?p=android), [monedas](https://dreamgames.helpshift.com/hc/en/3-royal-match/faq/21-how-can-i-obtain-and-use-coins/). | [H] Tomar como referencia la conexión entre victoria y progreso visible. No copiar la escasez ni una cadencia industrial de eventos: la pérdida por movimientos de match-3 no es equivalente al bloqueo lógico de water-sort. |

Las fichas no revelan cómo se generan los niveles. No hay evidencia aquí de que los rivales modifiquen el tablero según gasto, fuercen una derrota particular o garanticen que una compra resuelva. Una queja sobre azar es evidencia de percepción, no de manipulación probada.

## 3. Auditoría del diseño actual

Fuentes locales: [04 · ayudas](../../gdd/04-comodines-vidas-rescate.md), [05 · economía](../../gdd/05-economia-monetizacion.md), [06 · meta](../../gdd/06-meta-retencion-liveops.md), [08 · arquitectura](../../gdd/08-arquitectura-tecnica-publicacion.md), [referencia Magic Sort](../../research/09-referencia-magic-sort.md) y [economía de Magic Sort](../../research/magic-sort/b-economia-monetizacion.md).

| Hallazgo [A] | Riesgo / decisión propuesta [H] |
|---|---|
| **Rescate 600; primero del día 300**, desde el 31. La Cuia extra cuesta 150 y su stock queda desactivado al no haber vertido legal (04 §2.2; 05 §2.2). | La misma necesidad visible cuesta hasta cuatro veces más después del error. Además, parte de la queja que queremos corregir sigue presente: tengo una cuia guardada y no puedo usarla. Probar permitir ese stock en bloqueo, sujeto al límite total de recipientes; si se conserva un rescate separado, debe tener un beneficio entendible y no ocultar la ayuda ya comprada. |
| **Cinco vidas/25 min** y rescate debutan juntos; a cero vidas no se inicia ni reinicia campaña (04 §7). | Dos fricciones aparecen a la vez y dificultan diagnosticar abandono. Para validar diversión, usar una build de investigación sin bloqueo de campaña por vidas; luego comparar prototipos de forma explícita. El GDD vigente sigue intacto hasta decisión del dueño. |
| **Deshacer ilimitado 1–10, tres por nivel desde el 11**, recarga de tres por 30 semillas/vídeo (04 §2.1). | La cantidad no está calibrada para la regla D-001 de una dosis por vertido. Medir cuántos pasos necesita realmente deshacer una decisión. Comparar tres usos con mayor cupo o deshacer libre en una prueba del núcleo; evitar vender corrección de toques accidentales. |
| El bloqueo hace perder vida y potencialmente racha; rescatar conserva ambas (04 §5–8). | La compra puede aliviar ansiedad más que aportar disfrute. Medir por separado satisfacción, comprensión del fallo y retorno tras rechazar el rescate. No llamar “gratis” al reintento sin explicar de forma cercana la pérdida de vida. |
| 04 §8 permite **ocultar el estado del escudo** si mostrarlo reduce conversión >15 %. | Retirar esa palanca. Información sobre lo que se conserva o pierde debe seguir visible aunque reduzca compras. Ese 15 % es una regla de propuesta, no evidencia del mercado. |
| Barajar se excluye del bloqueo para evitar canibalizar el rescate (04 §2.4). | Priorizar utilidad y comprensión de la ayuda. No habilitarlo a ciegas: el solver debe distinguir solución encontrada, imposibilidad demostrada y presupuesto agotado. Un timeout no autoriza el mensaje “no hay salida”. |
| No hay recompensa pagada aleatoria; Canasta muestra contenido y usa ciclo fijo (04 §1; 06 §4.4). | Conservar. La sorpresa puede ser audiovisual; el valor comprado debe conocerse. Aguas turbias necesita una prueba aparte: solución omnisciente no implica decisiones justas con información visible. No cobrar “Revelar” para subsanar adivinación obligatoria. |
| Cinco ubicaciones rewarded, hasta 15/día; intersticiales desde 22 y cada 2–3 victorias, con topes (05 §6). | Los topes no describen la experiencia real. Medir segundos de publicidad por minuto activo y salida tras anuncio. En `no_fill`, saldo cero o red ausente debe quedar una ruta gratuita de juego; esconder vídeo y mostrar solo precio no la garantiza. |
| **13 SKU en v1.0**, starter limitado por nivel y 48 h, compra de vidas de una hora y Balsa inmediata (05 §4–5). | Excesiva superficie para un producto que solo ha prototipado 1–10. Cada SKU añade estados de error y soporte. Evitar urgencia artificial y temporizadores que consumen lo comprado mientras el jugador paga o cierra la app. |
| Bitácora sin reinicio y racha que no caduca por reloj (06 §4). | Conservar el perdón. Una colección/objetivo visible basta para la primera validación; no atribuir mejoras de retención a añadir sistemas sin comparación. |

**Punto de equilibrio:** poder pasar gratis no significa garantizar victoria después de cualquier secuencia equivocada. Deben existir niveles de entrada resolubles sin gasto, reintento justo, aprendizaje legible y una salida razonable cuando alguien se atasca. La compra ofrece comodidad, no repara un nivel defectuoso.

## 4. Qué demuestra y qué no demuestra la simulación

Se inspeccionaron [econ_sim.py](../../../tools/econ_sim.py), [econ_variants.py](../../../tools/econ_variants.py) y sus salidas ya versionadas; **no se regeneraron**. Las cifras siguientes son resultados del modelo, no jugadores observados ni una previsión de ventas.

| Salida guardada | Hallazgo [A] | Interpretación correcta |
|---|---|---|
| [B, 300 corridas/perfil](../../../tools/out/econ_B.md) | Casual: **73 %** con al menos un bloqueo sin recursos, saldo final **1.842**. Medio: **2 %**, saldo **15.510**; hábil **26.408**. | “Bloqueo” significa que quiso rescate y no tenía saldo; no equivale a nivel imposible, abandono ni pago. Los perfiles con racha acumulan mucho más. |
| [B con 600/750/900](../../../tools/out/econ_B_tramos.md) | Casual **100 %** y medio **45 %** con ese evento; neto tardío del medio **−34/día**. | Subir precio arregla un ratio elegido y empeora accesibilidad bajo los supuestos del modelo. No demuestra mayor ingreso real. |
| [Variantes, 200 corridas/perfil](../../../tools/out/econ_variants.md) | V0 casual **70 %**; V1/V2 y varias variantes suben al **100 %**. | No mezclar 70 % con 73 % como si fueran la misma muestra. La sensibilidad advierte fragilidad, no estima churn. |

El modelo fuerza sesiones e intentos por perfil, repone vidas al comenzar cada sesión y continúa hasta 500 o el tope de días. No modela abandono, pausas cortas reales, `no_fill`, elasticidad de compra/precio ni demanda dinámica de ayudas. Por eso “100 % completa el nivel 500” **no valida retención**. El éxito de una pista, de deshacer o de rescatar usa probabilidades elegidas; no sale de partidas humanas. La tabla de ingresos por 1.000 DAU añade supuestos de eCPM, pagadores y ARPPU: no son resultados emergentes de una población real.

Antes de volver a usarla para decidir precios: sustituir tasas por intervalos medidos, modelar duración/separación de sesiones, ayuda sin publicidad, incertidumbre, abandono y probabilidades condicionadas por saldo. Corregir simplificaciones ya reconocidas en 05 §3.4: Canasta del nivel 10, canastas mensuales, máximo un rescate/nivel y contenido real de Balsa. Presentar distribución y peores casos, no solo medias. No apuntar a que un porcentaje de jugadores se quede sin recursos; usar esa cifra como alarma.

Un excedente de semillas del jugador hábil tampoco es por sí mismo un fallo económico. Puede ser una recompensa por dominio. Un sumidero cosmético voluntario permite darle uso sin empobrecer a quien más ayuda necesita.

## 5. Cartera pequeña de compras justas

**Propuesta para un primer ensayo comercial, no catálogo aprobado.** Los importes son hipótesis en USD de referencia; precio localizado, impuestos y disponibilidad deben venir de la tienda. No fijar países al 50 % por analogía: validar percepción de valor y capacidad de compra local. Mantener reglas de niveles y ayudas iguales entre pagadores y no pagadores.

| Producto | Propuesta inicial | Por qué alguien lo querría / límites |
|---|---|---|
| **Sin anuncios obligatorios** (`af_noads`) | Solo si se activan intersticiales; US$2,99–4,99 como rango a investigar; permanente. Explicar vídeos opcionales antes de pagar. | Compra comodidad duradera. Los intersticiales deben ser moderados también sin comprar. Para la primera versión, preferir un entitlement puro; si se retienen las 500 semillas actuales, registrarlas como concesión única separada para que restaurar no las duplique. |
| **Un pack visual amazónico** | US$0,99–1,99; vista previa; compra permanente. | Cuia/fondo legible, sin alterar capacidad ni ocultar marcas. Arte con aprobación cultural. Si también se gana jugando, mostrar cuántos niveles faltan y explicar que se compra acceso anticipado; alternativamente una variante visual distinta de la recompensa. No convertir accesibilidad en producto de pago. |
| **Mochila de bienvenida** (`af_starter`) | US$0,99; mantener como punto inicial 500 semillas + 2 Cuias + 3 pistas, sujeto a recalibración. Una vez por cuenta, sin cuenta atrás. | Compra ayuda conocida después de aprenderla. Proponer tienda voluntaria tras sesión inicial; no desaparecer al superar un nivel mientras el jugador compara. Mostrar unidades y utilidad, sin descuento porcentual calculado contra un valor nominal artificial. |

Primero validar no consumibles y comprensión mediante una tienda simulada sin cobro. Activar consumibles reales solo después de superar los casos de restauración/duplicados. Posponer Balsa, alcancía, pase, monedas grandes y vidas temporizadas: sus aportes no están demostrados y complican comparar productos. Si luego aparece demanda de comodidad repetible, probar **un solo pack de semillas** (`af_coins_2`, referencia existente US$1,99 / 1.000), mostrando saldo final y usos; no hace falta la escalera hasta US$49,99. Una campaña posterior podría vender un pequeño pack de niveles artesanales adicional; es una hipótesis futura, no sustituto de los niveles gratuitos prometidos.

No introducir cofres de pago, sorteos, precios por propensión individual a gastar, tableros endurecidos después de comprar ni ofertas que aparezcan por detectar vulnerabilidad. No hace falta ninguna de esas técnicas para probar si un buen juego genera compras pequeñas.

## 6. Experimentos y medición de diversión

Las pruebas siguientes son propuestas. Cambiar reglas canónicas o lanzar A/B de economía requiere decisión expresa; 05 §6.3 hoy limita esa experimentación. Se pueden empezar con prototipos de investigación y entrevistas sin dinero real.

| Orden | Pregunta / diseño | Señal útil | Guarda |
|---|---|---|---|
| 1. Núcleo | Mismos tableros en sesiones moderadas: cupo actual de Deshacer frente a cupo generoso; personas nuevas y habituales del género. | Comprensión del error, retrocesos necesarios, decisiones expresadas y deseo espontáneo de jugar otro nivel. | No usar compras ni anuncios en esta prueba. No tocar D-001/D-002 para “mejorar conversión”. |
| 2. Bloqueo | Prototipo con Cuia propia utilizable frente al modal actual; preguntar qué esperan antes de tocar. | Uso correcto del inventario, interpretación del precio, retorno voluntario tras reiniciar. | Una respuesta “creí que ya lo había comprado” obliga a revisar el flujo. |
| 3. Vidas | Comparar sesiones sin bloqueo de campaña con el prototipo de cinco vidas; registrar también la opción de parar voluntariamente. | Disfrute tras perder, vuelta posterior, sesiones terminadas porque quisieron frente a forzadas. | Más minutos retenidos por espera o más intentos irritados no cuentan como mejora. |
| 4. Anuncios | Tras validar el núcleo, comparar sin intersticiales con una cadencia conservadora; rewarded siempre voluntario. | Continuación después del anuncio, retorno, valoración de intrusión, tiempo ajeno al juego. | No aumentar carga solo porque sube ingreso inmediato. Analizar `no_fill` por separado. |
| 5. Tienda | Tres familias de producto claras, elección hipotética y luego ensayo limitado real con compras verificadas. | Comprensión de contenido/precio y uso satisfactorio de lo comprado, además de conversión. | Compra involuntaria, producto perdido, duplicado o retirada de derecho por red: detener ensayo comercial. |

Para estudio formativo, **8–12 participantes por ronda** es una propuesta práctica para encontrar problemas, no tamaño que pruebe retención ni significancia. Para experimentos cuantitativos, fijar métrica principal, horizonte, efecto mínimo relevante y cálculo de muestra desde la variabilidad real; no elegir ganadores mirando cada hora. Separar jugadores nuevos, veteranos, idioma, dispositivo y calidad de conexión. Informar intervalos de confianza y grupos que empeoran, aunque mejore el promedio.

Preguntas breves al terminar: “¿Qué hizo difícil este nivel?”, “¿Sentiste que podías encontrar otra solución?”, “¿La ayuda hizo lo que esperabas?” y escala de disfrute/frustración. Cruzarlas con comportamiento; no inferir diversión de tiempo de pantalla, gasto o racha únicamente.

### Telemetría mínima propuesta

Reutilizar eventos canónicos cuando cubran el hecho; los campos siguientes son necesidades de análisis, no nuevos nombres de evento aprobados. Registrar versión del contenido/reglas/economía, nivel, variante, ID de intento/sesión y experimento. Evitar identidad personal y contenido sensible.

| Familia | Datos mínimos / definición |
|---|---|
| Intento | Inicio, victoria, reinicio voluntario, bloqueo, salida y reanudación; tiempo activo excluyendo anuncios/background; movimientos y profundidad de undo. Distinguir salida observada de “no volvió en 24 h”, que requiere esperar esa ventana. |
| Ayuda | Disponible, ofrecida, elegida, concedida y aplicada; origen gratis/stock/semillas/vídeo; saldo e inventario antes/después; solver `solved`/`unsolvable`/`budget_exhausted`. Medir victoria posterior, sin asumir causalidad por auto-selección. |
| Presión | Recursos agotados, vida agotada, oferta rechazada y acción siguiente. Publicar denominador: por intentos, por bloqueos o por jugadores. “Quiso rescate” solo existe como proxy de toque, no como lectura de intención. |
| Anuncio | Oferta, solicitud, fill, inicio, recompensa, cierre, error; duración visible y reanudación del tablero. No confundir callback de cierre con recompensa ganada. |
| Compra | Vista, inicio, pendiente de pago, verificada, concedida, confirmada, fallo/cancelación y restauración; SKU/precio/moneda y correlación interna segura. Tokens/recibos quedan en backend, fuera de Analytics. |
| Confianza | Concesión faltante/duplicada, conflicto de guardado, derecho desaparecido, recompensa corregida, contacto/reembolso cuando esté disponible. |

D1/D7 deben definirse por cohorte y retorno en el día indicado; jugadores que aún no alcanzan la ventana no entran al denominador maduro. Ingresos netos por usuario se leen junto con retorno, calidad técnica y satisfacción. Para dificultad mirar primer intento sin ayuda, curva de aprendizaje por reintento, tiempo de razonamiento, concentración de bloqueos y ruta gratuita; no optimizar hacia un porcentaje de derrota deseado para vender rescates.

## 7. Comercio: contrato conceptual que falta cerrar

El backend y la tienda deben cumplir una compra una sola vez pese a callbacks repetidos, cierre de app y red intermitente. Google exige verificar antes de conceder, distinguir pago pendiente de compra completada y confirmar después de entregar; consumibles se consumen y no consumibles se reconocen. [Google Play Billing](https://developer.android.com/google/play/billing/integrate). Unity puede repetir `OnPurchasePending`; recomienda deduplicar y persistir consumibles remotamente antes de `ConfirmPurchase`. [Unity IAP: compras](https://docs.unity.com/en-us/iap/purchases).

**Tres fallos de contrato en el diseño, no bugs de implementación afirmados:**

1. **04 §10.2 / 08 §11: restaurar un token ausente del blob no basta.** Comprar 1.000, gastar 600, perder el dispositivo y cargar un snapshot anterior puede reponer 1.000 sin conocer el gasto; fusionar `applied_purchases` por unión mientras se elige otro saldo puede impedir reponer valor que sí se perdió. Un libro de recibos acredita compras, pero no reconstruye una cartera consumible.
2. **08 §10.3: `acknowledge/consume` precede a escribir el libro mayor.** Si el proceso cae entre ambas operaciones, la tienda puede dar la compra por cumplida mientras falta su concesión persistente. Hay que guardar de forma idempotente la compra y la concesión antes de finalizarla en tienda; un fallo de confirmación no concede otra vez. Evitar el `acknowledge` local de emergencia a 72 h sin una entrega persistida: oculta un incidente, no lo repara.
3. **Hora de dispositivo no ordena de manera confiable dinero.** `economy_ts` y elegir el nivel máximo pueden perder débitos/créditos concurrentes. Tampoco un UID anónimo es por sí solo un mecanismo de recuperación tras perder acceso. La arquitectura menciona vínculo con Play Games; hay que definir y probar ese vínculo y la migración de identidad. Firebase documenta cómo vincular una cuenta anónima con credenciales permanentes. [Firebase Auth](https://firebase.google.com/docs/auth/unity/anonymous-auth).

**Propuesta de separación:**

| Capa | Responsabilidad |
|---|---|
| Catálogo | SKU estable, tipo y contenido versionado; precio localizado de la tienda. Congelar oferta durante el checkout |
| Orden | Máquina de estados: iniciada → pendiente de pago → verificada → concesión persistida → confirmación de tienda; cancelada/fallida no otorgan. No confundir `PENDING` de Google (sin pago completado) con `PendingOrder` de Unity (pagada, pendiente de cumplir) |
| Ledger | Clave única por tienda/token/ítem y propietario; concesión atómica y repetible. Registrar créditos **y débitos** con `operation_id`, versión, origen y motivo; no sobrescribir dinero con un timestamp del cliente |
| Entitlements | Derechos permanentes separados del saldo: noads/skin. Restaurar el derecho no vuelve a regalar su bono consumible |
| Wallet local | Snapshot confirmado + operaciones aún no sincronizadas; escritura atómica y copia de respaldo. Aplicar una operación una vez y poder explicar diferencias |
| Reconciliación | Backend devuelve snapshot/versiones y operaciones pendientes, no “todas las compras para sumar de nuevo”. Reembolso/revocación es una operación compensatoria registrada; sin castigo automático por timeout |

Offline y gasto concurrente ilimitado desde dos dispositivos no tienen una solución mágica. Para el primer lanzamiento hay que elegir entre un único dispositivo activo para gastar saldo offline, presupuestos offline asignados por dispositivo, o tolerancia acotada de duplicación con reconciliación y soporte. Recomiendo empezar con un único escritor económico y migración explícita al conectar; el puzle gratuito y los derechos ya comprobados siguen disponibles sin red. Esto necesita especificación, no un nuevo SDK.

Al perder red: preservar tablero/stock local confirmado; no iniciar IAP sin conexión; una compra en curso permanece “pendiente de confirmar” y se reintenta al reconectar; no retirar `noads` por una consulta vacía. Unity advierte que `FetchPurchases` puede omitir compras si faltan metadatos de producto offline, por lo que una lista vacía no demuestra ausencia de derechos. [Restauración oficial](https://docs.unity.com/en-us/iap/restore-purchases). Para recompensas de vídeo, una SSV tardía no debe marcar automáticamente fraude: distinguir ausencia de confirmación, rechazo verificado y fallo de transporte.

Antes de cobrar, comprobar al menos: doble callback, crash antes/después de concesión, pago diferido, confirmación sin red, reinstalación, segundo dispositivo, conflicto de snapshot, producto retirado, reembolso y bonus de noads restaurado varias veces. Un recibo consumible ya cumplido no se recupera como saldo desde la tienda; la app es responsable de ese historial. [Unity IAP: consumibles](https://docs.unity.com/en-us/iap/purchases).

## 8. Orden de trabajo propuesto

1. Mantener el prototipo centrado en diversión/legibilidad y completar sesiones formativas sin comercio real.
2. Resolver como decisiones de producto el uso de stock en bloqueo, Deshacer y el papel de vidas. Mantener información del escudo visible.
3. Probar niveles y economía con rutas sin anuncio/sin compra; corregir modelo antes de ajustar precio.
4. Construir el contrato de compra/restauración y una tienda simulada de tres familias de producto; posponer el resto del catálogo.
5. Activar monetización gradualmente solo con entrega confiable, experiencia gratuita defendible y métricas definidas. No cambiar dificultad según historial de pago.

Verificación de esta revisión: lectura del GDD y fuentes locales indicadas; páginas primarias abiertas y vinculadas; inspección de salidas guardadas. No se ejecutaron simulaciones, builds, tests ni partidas de competidores, y no se modificó ningún archivo de GDD o código. Las cifras de ensayo y precios propuestos son supuestos deliberados, no benchmarks.
