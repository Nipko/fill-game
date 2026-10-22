# Unity IAP 5.4.4 (Play Billing) detrás de `IBillingClient` (tanda 5, D-040)

Encargo: `docs/dev/traspaso/spark-reto-del-rio.md` §14. Rama `spark/t5-iap`. Aprobado por el dueño en **D-040** (2026-10-22): el paquete `com.unity.purchasing` **5.4.4** exacta y el permiso `com.android.vending.BILLING`. Nada más.

Canon: 08 §2.2 (`AF.IAP`), §7 #3, §10.1, §10.3 y §13.3; 05 §4.1 (SKU `af_*`), §4.2 y §7; D-035 y sus añadidos (F2-3 a F2-8, F3-1 y F3-2, F3-3 y F3-5); D-033 (Q-T4 (a), Q-B12-6); D-025 (precios); `infra/contracts/v1` (`verify-purchase`, `restore-ledger`); plan del backend §7.

---

## 1. Qué hace

### 1.1 Capas (de fuera a dentro)

| Capa | Archivo | Qué decide |
|---|---|---|
| Analítica | `WrapStore` (B7, sin cambios) | `af_iap_*` |
| Consentimiento y edad | `ConsentStoreService` (B12, sin cambios) | `<13`: sin compras; 13-17: segunda confirmación con el precio de la tienda antes de la hoja de Play (Q-T4 (a), Q-B12-6) |
| Tienda lista | `BillingReadyStoreService` (nuevo) | No vende hasta que Play responde con sus precios y sus compras sin terminar |
| Compra verificada | `VerifiedStoreService` (F3-3, **sin cambios de contrato**) | Acredita una sola vez, solo con `ok` o `duplicate` del servidor; termina en la tienda después de acreditar |
| Tienda de la plataforma | `PlayBillingClient` (nuevo, sin motor) sobre `IPlayStorePort` | Cobra, guarda lo cobrado sin terminar, lo termina cuando se lo piden. **No acredita nada** |
| SDK | `UnityIapStorePort` (nuevo, `AF.IAP.UnityIap`, solo con el paquete) | Traduce `StoreController` de Unity IAP 5.4.4 al puerto |

En el editor, los tests y cualquier build sin el paquete o sin el backend real, la tienda de la plataforma sigue siendo `FakeBillingClient` (hoja «Compra simulada»); `PlayBillingFactory.TryCreate` devuelve null fuera de `AF_UNITY_IAP && UNITY_ANDROID && !UNITY_EDITOR`.

### 1.2 Reglas que cumple `PlayBillingClient`

- **Catálogo** de 05 §4.1 (`StoreCatalog`): consumibles todos salvo `af_noads` y `af_skin_m02`-`m04`, que son no consumibles. La Mochila (`af_starter`) se registra como consumible y se consume tras acreditar: «1 por cuenta» lo hace cumplir el servidor (D-035, añadido de F2c).
- **`setObfuscatedAccountId` = UID** de Firebase Auth (08 §10.1; D-035, F3-3): `DelegatePlayerUid(() => FirebaseAuthUid.Current)` en el momento de cada compra. Para que nunca falte, Play solo se conecta cuando el UID ya existe (lo crea la primera llamada autenticada, el `restoreLedger` del arranque; ver «Arranque»), así que lo que entra en la cola también lo lleva.
- **`PENDING`** (efectivo, OXXO, PSE…): la compra responde `Pending` sin verificar ni acreditar. Cuando Play la cobra llega como pedido cobrado y sale por `UnfinishedChanged`; el juego llama a `VerifiedStoreService.Resume()`, que la verifica y la acredita una vez.
- **Compras sin terminar al arrancar** (`Resume`): `Start()` conecta, pide los productos y consulta las compras; solo entonces la tienda queda lista. Al volver a primer plano, `Refresh()` consulta otra vez (y, si Play se desconectó, falló la consulta o el arranque nunca contestó, vuelve a arrancar; lo que conteste tarde un arranque abandonado no cuenta). `UnfinishedTransactions()` devuelve lo cobrado sin consumir y los no consumibles que Play devuelve (restauración). Un solo `UnfinishedChanged` por consulta (cada uno es un `Resume` completo).
- **Un token, una vez por sesión**: los avisos repetidos de Unity IAP no abren otra verificación. Si el aviso en vivo de la hoja abierta trae un pedido que ya se conocía (Unity IAP responde a un «cancelar» con el pedido sin terminar del mismo SKU), la hoja falla —no cobró nada nuevo— y ese pedido sigue en la cola.
- **Escena recargada** (`RebuildUi`, `RestartAsFirstRun`): `GameBootstrap.OnDestroy` llama a `PlayBillingClient.Dispose`, que suelta Unity IAP (sus servicios son únicos en el proceso) para que sus avisos no lleguen también a la tienda vieja, sin llamar a nadie (tampoco a la compra abierta: Play aún puede cobrarla y vuelve a la tienda nueva); la nueva vuelve a consultar.
- **Solo un aviso en vivo responde a la compra abierta**, nunca una consulta: la hoja de Play pausa la app y `OnApplicationPause` llama a `Refresh`, cuya consulta puede traer, con la hoja aún abierta, el pedido nuevo, uno del mismo SKU de otra sesión o de otro dispositivo, o un pendiente de pago antiguo. Esos pedidos esperan a que la hoja responda (no entran en la cola ni se les pone el contexto de la compra abierta: nivel, Cántaro, Cuia de rescate) y, si no eran su respuesta, salen después por `UnfinishedChanged`.
- **Consumo solo tras acreditar** (D-035): `Finish` hace `ConfirmPurchase` (consume o reconoce según el tipo registrado) solo cuando `VerifiedStoreService` ya acreditó y guardó el `token_hash`. Con 400/409 no se termina (consumir también reconoce y Play ya no reembolsaría). Si el consumo falla (Unity IAP lo dice con un `FailedOrder` en `OnPurchaseConfirmed`), el pedido sigue cobrado en Play: el puerto no suelta su `PendingOrder` y, cuando la consulta lo devuelve, se vuelve a pedir el consumo sin acreditar otra vez (en esta sesión o en la siguiente, por `applied_purchases`).
- **`deferred_ack`** (72 h sin respuesta del servidor): con un no consumible, `ConfirmPurchase` (que solo reconoce); con un consumible, **nada** (Unity IAP 5.4.4 no tiene API pública para reconocer sin consumir): se cuenta en `AcknowledgeUnsupported`. Pregunta **Q-IAP-2**.
- **Precios locales** (D-025, punto 3 [P]): importe y moneda de la tienda (`price_amount_micros` y `price_currency_code`), nunca la cadena formateada de Play; mientras Play no responda, o si la moneda no se puede escribir o el precio no es positivo (producto sin cargar), el precio de 05 §4.2 en `LaunchOptions.DefaultStoreCurrency`, y **ese SKU no se vende** (`CanSell`: ni la hoja de Play ni la segunda confirmación de 13-17 se abren con un precio de respaldo). El Mercado se repinta cuando llegan (`StateChanged`). Ver **Q-IAP-3** (monedas fuera de las cinco columnas de 05 §4.2).
- **Tienda real solo con el backend real** [P]: `GameBootstrap.CreateStore` crea `PlayBillingClient` solo si el cliente del backend es `HttpBackendClient`. Con el backend falso (editor, desarrollo sin `google-services.json`) no se cobra dinero real que nadie verifique.
- **Arranque y edad** [P]: Play se conecta solo con la **edad ya decidida**, **nunca con la banda `<13`** (08 §7: «sin IAP») y con el UID de Firebase ya creado; se intenta en `ResumeBackend`, al responder el `restoreLedger` del arranque y al cerrar la pantalla de edad y consentimiento. Sin red en el primer arranque no hay UID ni tienda hasta que vuelva (el servidor tampoco podría verificar nada). Ver **Q-IAP-5**.
- **Si Unity IAP falla al crearse** en el teléfono, la tienda queda sin Play (`UnavailablePlayStorePort`): el juego arranca y no vende (nunca la tienda simulada en un build real).

### 1.3 Unity IAP 5.4.4, leído en el código del paquete

- Dependencias que entran (las resuelve Unity; ver `packages-lock.json` del parche): `com.unity.services.core` 1.18.0, `com.unity.services.deployment` 1.7.2 y `com.unity.services.deployment.api` 1.1.3 (y `com.unity.nuget.newtonsoft-json`, que ya estaba). **No** entra `com.unity.services.authentication`: en 5.4.4 es opcional (08 §7 #3 decía «requiere 3.7.1»: el parche de docs lo corrige).
- Play Billing Library **9.0.0** como dependencia Gradle (`com.android.billingclient:billing:9.0.0`), más dos *stubs* vacíos de `kotlin-stdlib-jdk7/jdk8` 1.8.22.
- Sin `Resources/BillingMode.json`, Unity IAP usa la tienda **falsa** en Android. `UnityIapStorePort` pide la tienda por nombre (`GooglePlay`) para no depender de ese archivo. Aun así, el editor de Unity IAP lo crea (`{"androidStore":"GooglePlay"}`) en cada importación y build: va en `IAP-manifest.diff` para que el integrador lo commitee una vez y el árbol quede limpio.
- No llamamos a `UnityServices.InitializeAsync`: sin él no arrancan la telemetría de Unity Services ni los servicios de pagos externos y webshop del paquete.
- Si una compra devuelta por Play es de un SKU que no está en la caché de productos, Unity IAP lo busca en el catálogo remoto de Unity (`StoreOverrideReverseLookupService`, una llamada HTTP a Unity con el id del proyecto). Con nuestro orden (productos antes que compras) no pasa con los SKU de 05 §4.1; sí con uno que esta versión no venda. Entra en **Q-IAP-1**.
- **Telemetría de compras a Unity**: cada compra con Google Play envía a `prd.insights.analytics.unity3d.com` un evento (intención, pagada, fallida, completada) con el SKU, el precio, el token, el modelo, el sistema, los idiomas y el espacio total del dispositivo, la hora de instalación y el id y la versión de la app; el GAID y los ids de Firebase solo si el consentimiento de anuncios **de Unity** está concedido (no lo usamos). No depende de `UnityServices.InitializeAsync` y el cliente no lo puede apagar. Pregunta **Q-IAP-1**.

---

## 2. Archivos

### 2.1 En la rama (nuevos)

- `Assets/_Project/Scripts/IAP/PlayStorePort.cs`: el puerto `IPlayStorePort` y sus tipos (sin motor).
- `Assets/_Project/Scripts/IAP/PlayBillingClient.cs`: `IBillingClient` real e `IBillingAvailability`.
- `Assets/_Project/Scripts/IAP/BillingReadyStoreService.cs`: la tienda no vende hasta que Play esté lista.
- `Assets/_Project/Scripts/IAP/DelegatePlayerUid.cs`: `IPlayerUidProvider` que lee el UID al comprar.
- `Assets/_Project/Scripts/IAP/IStoreDecorator.cs`: `IStoreDecorator` y `StoreDecorators.Find` (bajar por los envoltorios de la tienda sin conocerlos).
- `Assets/_Project/Scripts/UnityIap/AF.IAP.UnityIap.asmdef` (`versionDefines`: `com.unity.purchasing` 5.4.4 → `AF_UNITY_IAP`), `UnityIapStorePort.cs` (`#if AF_UNITY_IAP`) y `PlayBillingFactory.cs`. Va fuera de `Scripts/IAP/` porque el guardián de `AF.IAP` (`StoreCatalogTests.IapAssembly_HasNoEngine_NoSystemClock_AndNoNetwork`) recorre esa carpeta entera y prohíbe `using Unity…`; si el integrador la prefiere en `Scripts/IAP/UnityIap/`, basta con que el guardián salte las subcarpetas con su propio `.asmdef`.
- `Assets/_Project/Scripts/Backend/Firebase/FirebaseAuthUid.cs`: el UID de Firebase Auth (solo en el teléfono).
- Tests: `Scripts/IAP/Tests/FakePlayStorePort.cs`, `PlayBillingClientTests.cs` (37 casos), `PlayBillingVerifiedTests.cs` (19 casos) y `Tests/PlayMode/PlayBillingPlayModeTests.cs` (2).

Sin el parche del manifiesto, `AF.IAP.UnityIap` compila vacío salvo la fábrica (que devuelve null).

### 2.2 Parches (`docs/dev/traspaso/spark-enganches/`, LF, `git -c core.autocrlf=false apply`, en este orden)

1. **`IAP-manifest.diff`**: `Packages/manifest.json` (`"com.unity.purchasing": "5.4.4"`), `Packages/packages-lock.json` tal como lo regeneró Unity, y `ci/check-manifest.ps1` con `com.android.vending.BILLING` en la lista blanca (cita a D-040) y su autotest (45 casos, OK). Sin el paquete no hay tienda real.
2. **`IAP-bootstrap.diff`** (necesita el 1 para que exista `AF_UNITY_IAP`; compila también sin él): `AF.UI.asmdef` (+`AF.IAP.UnityIap`, +`AF.Backend.Firebase`), `GameBootstrap.Backend.cs` (tienda real con el backend real, UID de Firebase, `BillingReadyStoreService`, `Resume` al llegar una compra fuera de la hoja, repintado del Mercado, `StartPlayBilling` en `ResumeBackend` y tras el `restoreLedger`, sin la banda `<13` y con UID, `Refresh` al volver a primer plano y `DisposeBilling`), `GameBootstrap.Consent.cs` (una línea: la comprobación previa de 13-17 tampoco confirma un SKU sin precio de Play) y `GameBootstrap.cs` (una línea en `OnDestroy`: `DisposeBilling()`).
3. **`IAP-analytics.diff`** (sobre la rama, que trae `IStoreDecorator`): `ConsentStoreService` y `AnalyticsStoreService` implementan `IStoreDecorator` y `AnalyticsStoreService` encuentra la tienda verificada bajando por la cadena (`StoreDecorators.Find`), para emitir `af_iap_result` y `purchase` de lo que se acredita tarde (pagos pendientes, la cola; 08 §8.2). **Ojo: el fallo ya existía** desde B12 con el consentimiento activo (el envoltorio solo miraba `inner is VerifiedStoreService`). Además, `PurchaseVerificationEvent.IsDuplicate` (aditivo en `VerifiedStoreService`): un no consumible acreditado por `duplicate` fuera de una compra abierta es una restauración (`af_entitlement_restored` `no_ads`, sin `af_iap_result` ni `purchase`), y un `purchase` tardío antes de que Play dé su precio sale sin `value` ni `currency` (no se inventa el ingreso). Con tres tests.
4. **`IAP-price.diff`** (opcional, **Q-IAP-3 (a)**): `LocalPrice` admite cualquier código ISO 4217 (la tienda simulada sigue con las cinco columnas de 05 §4.2) y `StoreCatalogTests` al día.
5. **`IAP-gdd.diff`**: 08 §7 #3 (dependencias reales, sin `services.authentication`, permiso), §10.1 (fila de la amenaza: el `acknowledge` local solo en no consumibles) y §13.3 (fila Unity IAP / Play Billing con el permiso y la telemetría a Unity, pendiente de Q-IAP-1).

Cada parche se aplica también solo sobre la rama, salvo el 2, que con el 1 crea la tienda real; probados juntos (§6).

---

## 3. Modo de ensayo en el teléfono (licencias de prueba) — **no ejecutado**

La app aún no existe en Play Console (D-034). Cuando el dueño la cree, en este orden (acciones del dueño marcadas **[dueño]**):

1. **[dueño]** En Play Console: la app con el `applicationId` que se vaya a probar (regla dura 6: nunca el definitivo `com.<estudio>.tintalu` antes del dictamen; si se usa `com.amazonfill.dev` para la pista interna, es decisión del dueño, porque reserva ese nombre en la cuenta), los productos integrados con los ids `af_*` de 05 §4.1 (tipo «producto integrado»; precios por país de 05 §4.2) y activos.
2. **[dueño]** Licencias de prueba: Play Console › Configuración › Pruebas de licencias, con la cuenta de Google del teléfono.
3. **[dueño]** Backend de dev desplegado (F4) y `google-services.json` de dev fuera del repo (D-039): sin `HttpBackendClient` el juego usa la tienda simulada.
4. **[dueño]** Build firmado subido a la pista de prueba interna e instalado desde Play (los agentes no suben nada).
5. Casos (con `adb logcat -s Unity` y las sondas `AF_STATE` de desarrollo; el token y el UID nunca salen en el log):
   - Mercado: precio en la moneda de la cuenta (no el de 05) y «tienda no disponible» antes de que Play responda.
   - Compra con la tarjeta de prueba que **siempre aprueba**: «Confirmando…», crédito una vez, la compra consumida (se puede volver a comprar).
   - Tarjeta que **siempre rechaza**: nada cambia. Cerrar la hoja: `cancel`, nada cambia.
   - Tarjeta **lenta que aprueba**: `pending`; al aprobarse, crédito una vez sin reabrir la app.
   - Tarjeta **lenta que rechaza**: `pending` y nada más.
   - Cerrar la app justo tras pagar (antes de «Confirmando…»): al reabrir, crédito una vez.
   - Modo avión tras pagar: queda en la cola; al volver la red, crédito una vez.
   - Sin anuncios y reinstalar con la misma cuenta y el mismo UID: vuelve una vez (restauración).
   - Banda 13-17: la segunda confirmación con el precio de la tienda antes de la hoja de Play, y Play pide su autenticación de compras según los ajustes de la cuenta.
   - Banda `<13`: el Mercado no vende y Play no se conecta.
6. Comprobar en `ci/check-manifest.ps1 -Mode dev` el APK de la prueba y que `READ_GSERVICES`, `AD_ID` y compañía siguen fuera.

---

## 4. Preguntas para el dueño (Q-IAP-n)

- **Q-IAP-1 · Telemetría de compras de Unity IAP a Unity.** Unity IAP 5.4.4 envía a Unity (Insights) un evento por cada paso de cada compra con datos del dispositivo y el token (§1.3), sin pasar por nuestro consentimiento y sin interruptor en el cliente. No está en Data Safety ni en la política. Opciones: **(a) recomendada:** seguir con Unity IAP para las pruebas internas; antes del soft launch, el dueño desactiva la recogida de «Developer Data» en el panel de Unity si el proyecto se vincula (hoy no lo está: el evento sale sin `ProjectId`), el abogado valora la fila de Data Safety y la política, y se mide en el teléfono con un proxy qué sale; (b) copiar el paquete como embebido y anular el emisor (modificar un paquete de Unity: mantenimiento y licencia por revisar); (c) cambiar Unity IAP por un plugin propio de Play Billing 9 (`.androidlib`) detrás del mismo `IBillingClient` (sin telemetría de terceros; más trabajo y una dependencia nueva que aprobar).
- **Q-IAP-2 · `deferred_ack` de consumibles.** Unity IAP 5.4.4 no permite reconocer un consumible sin consumirlo (solo `ConfirmPurchase`, que consume). Opciones: **(a) recomendada, aplicada:** con un consumible, nada (si el servidor no verifica en 3 días, Play la reembolsa; como no se acreditó nada, no hay pérdida ni fraude; el servidor reconoce al verificar, así que solo importa con el backend caído más de 72 h); con un no consumible, `ConfirmPurchase` (solo reconoce); y 08 §10.3 lo dice (parche de docs); (b) llamar por JNI a `acknowledgePurchase` del `BillingClient` interno de Unity IAP (API interna, frágil); (c) un plugin Android propio solo para reconocer (código nativo nuevo).
- **Q-IAP-3 · Monedas fuera de 05 §4.2.** `LocalPrice` solo admite USD, BRL, MXN, COP y PEN; con una cuenta de Play en EUR, ARS o CLP esos SKU no tienen precio de la tienda y, en la rama, **no se venden** (`CanSell`). Opciones: **(a) recomendada:** aplicar `IAP-price.diff` (cualquier ISO 4217, con los decimales de `LocPrice`: 2 salvo COP y MXN) y, en otra tarea, fijar en D-025 los decimales de las monedas sin céntimos (CLP, JPY: hoy saldría «CLP 990,00») y de las de tres decimales (KWD, BHD: 0,299 se redondearía a 0,30, que no es lo que cobra Play); (b) dejarlo así: fuera de las cinco monedas, no se vende; (c) aplicar el parche sin más.
- **Q-IAP-4 · No consumibles con otro UID.** Tras reinstalar sin recuperar el UID, Play devuelve Sin anuncios pero el servidor responde 409 (token de otro UID; D-035, F2-3 a F2-8) y el cliente no lo acredita (nunca sin el servidor). Como el rechazo solo se recuerda en la sesión, **cada arranque** vuelve a preguntar y vuelve a recibir 409 (una llamada, un `af_iap_result` `verify_failed` y una nota «No se pudo confirmar» de Sin anuncios en cada arranque). D-035 dice que «los no consumibles se recuperan desde Play», pero eso exige al servidor. Opciones: **(a) recomendada:** tarea del backend: con un no consumible de otro UID que Play da como comprado por esta cuenta, el servidor reasigna el derecho (o responde `duplicate`); (b) dejarlo (Sin anuncios se pierde si cambia el UID); (c) acreditar Sin anuncios en el cliente con solo Play (va contra D-040: nunca sin el servidor).
- **Q-IAP-6 · Paquetes que trae Unity IAP.** D-040 aprueba `com.unity.purchasing` 5.4.4 «y nada más», y el paquete arrastra `com.unity.services.core` 1.18.0, `com.unity.services.deployment` 1.7.2 y `com.unity.services.deployment.api` 1.1.3 (dependencias obligatorias, sin permisos Android; `services.core` no se inicializa porque no llamamos a `UnityServices.InitializeAsync`). Opciones: **(a) recomendada:** aprobarlos como parte de D-040 y anotarlos en D-040 con su versión; (b) no integrar Unity IAP (sin ellos no compila).
- **Q-IAP-5 · Conectar Play antes de elegir la edad.** En el primer arranque, `ResumeBackend` corre antes de que se decida la edad. Opciones: **(a) recomendada, aplicada (tras la revisión):** conectar solo con la edad decidida (una línea más en `FinishConsentAtBoot`, en `IAP-bootstrap.diff`): así ni la consulta de compras ni la búsqueda en el catálogo remoto de Unity (§1.3) ocurren antes de saber que no es `<13`; (b) conectar en cuanto haya UID (sin compras hasta decidir, porque `ConsentStoreService` falla cerrado).
- **Q-IAP-7 · `Resources/BillingMode.json`.** El editor de Unity IAP lo crea en cada importación (`{"androidStore":"GooglePlay"}`), aunque nuestro puerto pide la tienda por nombre y no lo necesita. Opciones: **(a) recomendada, en `IAP-manifest.diff`:** commitearlo una vez (con su carpeta `Assets/Resources`), para que el árbol quede limpio y, si algún día se usa `UnityIAPServices.DefaultStore()`, Android no caiga en la tienda falsa; (b) ignorarlo en `.gitignore` (el build lo regenera igual).

---

## 5. Privacidad (lista del integrador de SDK)

| Dato | Finalidad | Lo habilita | Data Safety (08 §13.3) |
|---|---|---|---|
| Token de compra (`purchaseToken`) | Verificar la compra en el servidor (hash en el libro mayor) | Ejecución del contrato: la compra que pide el jugador | Fila Unity IAP / Play Billing y fila del backend propio |
| UID anónimo como `obfuscatedAccountId` en Play | Antifraude de Play y contraste en `verifyPurchase` (08 §10.1) | Interés legítimo (seguridad), ya en el canon | Fila Unity IAP / Play Billing (parche) |
| Eventos de compra a Unity (Insights) | Informes de ingresos de Unity | **Nada nuestro: pendiente de Q-IAP-1** | Fila Unity IAP / Play Billing (parche, marcada pendiente) |

- **`<13`:** sin compras (`ConsentStoreService`), Play no se conecta y no sale ningún evento de compra.
- **13-17:** segunda confirmación con el precio de la tienda antes de la hoja de Play, más la autenticación de compras de Play según la cuenta (Q-T4 (a); el cliente no la puede forzar [NV, abogado]); los eventos de Unity salen igual que con 18+ (Q-IAP-1).
- **18+:** flujo normal.
- Nada de IDs en URLs ni en logs: ni el token ni el UID se escriben (los `ToString` los omiten).
- Permisos: solo `com.android.vending.BILLING` (D-040). Ver §6 para el manifiesto fusionado medido.

---

## 6. Verificación

(Se completa en la entrada de `spark-estado.md`: recuentos, build de desarrollo y `check-manifest`.)

---

## 7. Riesgos

- **Telemetría a Unity** (Q-IAP-1): bloqueante para el soft launch hasta que decidan el dueño y el abogado.
- **Hoja de Play sin respuesta**: si Unity IAP nunca responde a una compra (proceso matado a medias), la tienda queda «comprando» hasta reiniciar la app; al reiniciar, lo cobrado vuelve sin terminar y se acredita una vez.
- **Pago pendiente cobrado justo con la hoja abierta del mismo SKU**: el aviso en vivo de ese cobro respondería a la hoja (con su contexto); el de la hoja llegaría después como otra compra sin contexto. Cada token se acredita una vez; solo el contexto (la Cuia de rescate de una Balsa) puede caer en la compra equivocada. Muy raro.
- **Mercado con un SKU sin precio de Play**: no se vende, pero la fila enseña el precio de respaldo hasta que Play lo dé (la UI del Mercado es del escritorio: sugerencia, ocultar o desactivar la fila con `PlayBilling.CanSell`).
- **Sin red en el primer arranque**: sin UID no se conecta Play, así que la tienda queda «no disponible» hasta que el `restoreLedger` responda (al volver a primer plano con red).
- **Escena recargada con una verificación en vuelo** (anterior a D-040, de F3-5): `DisposeBilling` suelta Play, pero la tienda verificada de la escena vieja sigue viva hasta que su petición `verifyPurchase` responde (`UnityWebRequest` sobrevive a la escena); esa respuesta puede acreditar y guardar sobre el perfil de la sesión anterior y escribir su `purchase_queue`. Sugerencia para el escritorio: soltar también la tienda verificada en `OnDestroy` (quitar sus suscripciones e ignorar lo que llegue después).
- **`duplicate` de una compra de esta misma instalación**: si el `ok` de Sin anuncios o de una skin se pierde por un fallo de red y el reintento recibe `duplicate`, la analítica lo cuenta como restauración (`af_entitlement_restored`) y no como ingreso. Raro; para afinarlo, la cola tendría que marcar las entradas que nacen de una hoja abierta en este dispositivo.
- **Compuerta de release sin Play Billing**: `ci/check-manifest.ps1` solo anota un manifiesto sin Play Billing; el workflow de release debe pasarle `-RequireBilling` (lo dice ya la cabecera del script tras el parche).
- **Plazo de 72 h** sin `purchaseTime`: Unity IAP no da la hora de compra; el plazo del `deferred_ack` cuenta desde que la compra entra en la cola (casi lo mismo en la compra en vivo).
- **Restauración y 409** (Q-IAP-4); **monedas** (Q-IAP-3).
- **`SHA256.Create()` con stripping High** (D-035, F3-1 y F3-2): sigue por comprobar en el teléfono.
- **Builds de Android**: el paquete añade `android.enableJetifier=true` a `gradle.properties` y PBL 9 por Gradle (necesita red en el build); verificado en el build de desarrollo (§6), pero la referencia es el CI Linux.
