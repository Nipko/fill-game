# Play Billing con un plugin propio detrás de `IBillingClient` (tanda 5, D-040)

Encargo: `docs/dev/traspaso/spark-reto-del-rio.md` §15 (antes, §14). Rama `spark/t5-billing`. Lo aprobó el dueño en **D-040** y en su añadido de Q-IAP-1 (2026-10-22):
- `com.android.billingclient:billing` **9.0.0** directo, como dependencia declarada;
- el permiso `com.android.vending.BILLING`;
- quitar Unity IAP 5.4.4 (`com.unity.purchasing`) y sus tres dependencias de Unity Services.

Lo que la biblioteca arrastra por Maven (Q-BILL-1) lo aprobó el dueño el 2026-10-22, con las versiones que resuelve Gradle. Nada más está aprobado.

Canon:
- 08 §2.2 (`AF.IAP`), §7 #3, §10.1, §10.3 y §13.3;
- 05 §4.1 (SKU `af_*`), §4.2 y §7;
- D-035 y sus añadidos (F2-3 a F2-8, F3-1 y F3-2, F3-3 y F3-5);
- D-033 (Q-T4 (a), Q-B12-6) y D-025 (precios);
- `infra/contracts/v1` (`verify-purchase`, `restore-ledger`) y el plan del backend §7.

Historia: la primera integración (`spark/t5-iap`, §14 del encargo) usó Unity IAP 5.4.4. El dueño la sustituyó por este plugin (Q-IAP-1 (c)), porque Unity IAP enviaba cada compra a Unity (Insights) sin consentimiento y sin borrado, y su licencia no permite anular el emisor. La capa de juego no cambia: solo el puerto de abajo.

---

## 1. Qué hace

### 1.1 Capas (de fuera a dentro)

| Capa | Archivo | Qué decide |
|---|---|---|
| Analítica | `WrapStore` (B7, sin cambios) | `af_iap_*` |
| Consentimiento y edad | `ConsentStoreService` (B12, sin cambios) | `<13`: sin compras; 13-17: segunda confirmación con el precio de la tienda antes de la hoja de Play (Q-T4 (a), Q-B12-6) |
| Tienda lista | `BillingReadyStoreService` (sin cambios) | No vende hasta que Play responde con sus precios y sus compras sin terminar |
| Compra verificada | `VerifiedStoreService` (F3-3, **sin cambios de contrato**) | Acredita una sola vez, solo con `ok` o `duplicate` del servidor; termina en la tienda después de acreditar |
| Tienda de la plataforma | `PlayBillingClient` (sin motor, **sin cambios de contrato**) sobre `IPlayStorePort` | Cobra, guarda lo cobrado sin terminar y lo termina cuando se lo piden. **No acredita nada** |
| Puerto | `NativePlayStorePort` (nuevo, `AF.IAP.PlayBilling`) sobre `IBillingBridge` | Traduce el plugin al puerto: estados, precios en micros, hora de compra, consumir o reconocer |
| Puente JNI | `AndroidBillingBridge` y `BillingListenerProxy` (nuevos; solo `UNITY_ANDROID && !UNITY_EDITOR`) | `AndroidJavaObject` hacia el plugin y `AndroidJavaProxy` de vuelta, con los avisos pasados al hilo de Unity (`AndroidApplication.InvokeOnUnityMainThread`) |
| Plugin Android | `AfBilling.androidlib` (nuevo, Java) | `BillingClient` de Play Billing Library 9.0.0. **Solo habla con Play** |

En el editor, en los tests y en cualquier build sin el backend real, la tienda de la plataforma sigue siendo `FakeBillingClient` (hoja «Compra simulada»). `PlayBillingFactory.TryCreate` devuelve null fuera de `UNITY_ANDROID && !UNITY_EDITOR`.

### 1.2 Reglas que cumple `PlayBillingClient` (siguen valiendo todas)

- **Catálogo** de 05 §4.1 (`StoreCatalog`): consumibles todos salvo `af_noads` y `af_skin_m02`-`m04`, que son no consumibles. La Mochila (`af_starter`) se registra como consumible y se consume tras acreditar: «1 por cuenta» lo hace cumplir el servidor (D-035, añadido de F2c).
- **`setObfuscatedAccountId` = UID** de Firebase Auth (08 §10.1; D-035, F3-3), con `DelegatePlayerUid(() => FirebaseAuthUid.Current)` en el momento de cada compra.
  - Play solo se conecta cuando el UID ya existe: lo crea la primera llamada autenticada, el `restoreLedger` del arranque. Así lo que entra en la cola también lo lleva.
  - El plugin pasa el UID a `BillingFlowParams.Builder.setObfuscatedAccountId`; sin UID, no lo llama.
- **`PENDING`** (efectivo, OXXO, PSE…): la compra responde `Pending` sin verificar ni acreditar. Cuando Play la cobra, llega como pedido cobrado (`PurchasesUpdatedListener` con la app abierta, o la consulta de compras) y sale por `UnfinishedChanged`. El juego llama entonces a `VerifiedStoreService.Resume()`, que la verifica y la acredita una vez.
- **Compras sin terminar al arrancar** (`Resume`): `Start()` conecta, pide los productos y consulta las compras; solo entonces la tienda queda lista.
  - Al volver a primer plano, `Refresh()` consulta otra vez. Si Play se desconectó, falló la consulta o el arranque nunca contestó, vuelve a arrancar; lo que conteste tarde un arranque abandonado no cuenta.
  - `UnfinishedTransactions()` devuelve lo cobrado sin consumir y los no consumibles que Play devuelve (restauración).
  - Un solo `UnfinishedChanged` por consulta (cada uno es un `Resume` completo).
- **Un token, una vez por sesión**: los avisos repetidos de Play no abren otra verificación. Si el aviso en vivo de la hoja abierta trae un pedido que ya se conocía, la hoja falla (no cobró nada nuevo) y ese pedido sigue en la cola.
- **Escena recargada** (`RebuildUi`, `RestartAsFirstRun`): `GameBootstrap.OnDestroy` llama a `PlayBillingClient.Dispose`.
  - Suelta el puerto, que suelta el plugin (`endConnection`), sin llamar a nadie (tampoco a la compra abierta: Play aún puede cobrarla y vuelve a la tienda nueva).
  - La tienda nueva crea otro `BillingClient` y vuelve a consultar.
- **Solo un aviso en vivo responde a la compra abierta**, nunca una consulta. La hoja de Play pausa la app y `OnApplicationPause` llama a `Refresh`; su consulta puede traer, con la hoja aún abierta, el pedido nuevo, uno del mismo SKU de otra sesión o de otro dispositivo, o un pendiente de pago antiguo.
  - Esos pedidos esperan a que la hoja responda: no entran en la cola ni se les pone el contexto de la compra abierta (nivel, Cántaro, Cuia de rescate).
  - Si no eran su respuesta, salen después por `UnfinishedChanged`.
- **Consumo solo tras acreditar** (D-035): `Finish` pide terminar el pedido (`IPlayStorePort.Confirm`) solo cuando `VerifiedStoreService` ya acreditó y guardó el `token_hash`.
  - El puerto consume los consumibles (`consumeAsync`) y reconoce los no consumibles (`acknowledgePurchase`), según el tipo con que se pidió el producto.
  - Con 400/409 no se termina: consumir también reconoce y Play ya no reembolsaría.
  - Si el consumo falla, el pedido sigue cobrado en Play y el puerto lo conserva. Cuando la consulta lo devuelve, se vuelve a pedir el consumo sin acreditar otra vez (en esta sesión o en la siguiente, por `applied_purchases`). `ITEM_NOT_OWNED` al consumir cuenta como ya consumido.
- **`deferred_ack`** (48 h sin respuesta del servidor, `sec_purchase_deferred_ack_h`, Q-BILL-5; 08 §10.3). **Cambia con el plugin** (Q-IAP-2 se reabre, como pedía Q-F3-3 3):
  - `AcknowledgeLocal` pide siempre `IPlayStorePort.Acknowledge` (`acknowledgePurchase`), sea consumible o no: con Unity IAP un no consumible se reconocía con `ConfirmPurchase`;
  - un consumible **también se reconoce sin consumirlo** (`IPlayStorePort.Acknowledge` = `acknowledgePurchase`), y sigue cobrado y en la cola hasta que el servidor lo verifique; entonces se consume;
  - si Play no lo hizo (sin red, el servicio caído), `VerifiedStoreService.Resume` lo vuelve a pedir en cada `Resume` (arranque, vuelta a primer plano y cada compra sin terminar nueva) para las entradas de la cola ya marcadas `deferred_ack`; el puerto no repite lo ya reconocido ni lo que está en vuelo (cambio interno de `VerifiedStoreService`, sin cambio de contrato);
  - el plazo cuenta desde **`purchaseTime`** de Play (la cola ya lo admitía: `BillingTransaction.PurchasedUtc`); sin él, desde que la compra entra en la cola;
  - nunca se acredita por el `deferred_ack` (D-035, F3-1 y F3-2).
- **Precios locales** (D-025, punto 3 [P]): importe y moneda de la tienda (`getPriceAmountMicros` y `getPriceCurrencyCode` de la oferta de compra única), nunca la cadena formateada de Play.
  - Mientras Play no responda, o si la moneda no se puede escribir o el precio no es positivo, vale el precio de 05 §4.2 en `LaunchOptions.DefaultStoreCurrency`, y **ese SKU no se vende** (`CanSell`): ni la hoja de Play ni la segunda confirmación de 13-17 se abren con un precio de respaldo.
  - El Mercado se repinta cuando llegan (`StateChanged`). Monedas: Q-IAP-3.
- **Tienda real solo con el backend real** [P]: `GameBootstrap.CreateStore` crea `PlayBillingClient` solo si el cliente del backend es `HttpBackendClient`. Con el backend falso (editor, desarrollo sin `google-services.json`) no se cobra dinero real que nadie verifique.
- **Arranque y edad** [P]: Play se conecta solo con la **edad ya decidida**, **nunca con la banda `<13`** (08 §7: «sin IAP») y con el UID de Firebase ya creado (Q-IAP-5).
- **Si el plugin falla al crearse** en el teléfono (clase Java ausente, JNI roto), la tienda queda sin Play (`UnavailablePlayStorePort`): el juego arranca y no vende (nunca la tienda simulada en un build real).

### 1.3 El plugin (`AfBilling.androidlib`)

- **Solo Java**, sin el plugin de Kotlin en Gradle. `build.gradle` con `namespace 'com.amazonfill.billing'` (AGP 9), `compileSdk 36`, `minSdk 26` y Java 17.
- **Dependencia:** `com.android.billingclient:billing:9.0.0` exacta.
  - Entra en el build por EDM4U: `Assets/_Project/Plugins/Editor/AfBillingDependencies.xml`, que el resolvedor escribe en `Assets/Plugins/Android/mainTemplate.gradle` (y en `ProjectSettings/AndroidResolverDependencies.xml`).
  - El plugin solo compila contra ella (`compileOnly`), con la misma versión. `AfBillingPluginTests` comprueba que las dos coinciden con la aprobada.
- **Qué usa de Play Billing Library 9.0.0** (firmas leídas con `javap` del AAR):
  - `enablePendingPurchases(PendingPurchasesParams.newBuilder().enableOneTimeProducts().build())` y `enableAutoServiceReconnection()`;
  - `startConnection` (una conexión en curso responde a todos los que esperan);
  - `queryProductDetailsAsync` (`QueryProductDetailsResult`; productos `INAPP`; de cada uno, la oferta de compra única con su `offerToken`);
  - `launchBillingFlow` con `setObfuscatedAccountId` y el `offerToken` de esa oferta;
  - `queryPurchasesAsync` (`INAPP`);
  - `consumeAsync` y `acknowledgePurchase`;
  - los avisos de `PurchasesUpdatedListener`.
- **Conexión:** si la biblioteca ya se está reconectando sola (`getConnectionState() == CONNECTING`), `connect` espera a que termine (una sola espera para todos, hasta 30 s, cada 500 ms) en lugar de pedir otra conexión, que Play respondería con `DEVELOPER_ERROR`; si no termina, responde `SERVICE_UNAVAILABLE`. `launch` sin conexión espera hasta 5 s a la reconexión antes de abrir la hoja. Cada consulta de productos sustituye a la anterior: solo se puede comprar lo que Play devuelve a la última (la respuesta de una consulta vieja no toca la lista).
- **Hilos:** todas las llamadas pasan al hilo principal de Android (`Handler(Looper.getMainLooper())`), donde llegan también los avisos de la biblioteca (las respuestas de las consultas se vuelven a encolar ahí). El estado del plugin solo se toca en un hilo. C# pasa cada aviso al hilo de Unity.
- **Hacia C#** solo enteros y cadenas: las listas van como texto (una fila por línea, campos con tabulador):
  - producto: `sku`, moneda e importe en micros;
  - compra: `sku`, `purchaseToken`, estado (1 `PURCHASED`, 2 `PENDING`), `purchaseTime` en ms y reconocida (1/0).
  - Se saltan las compras de más de un producto y los campos con separadores.
- **No escribe nada en el log** (ni el token ni el UID), no guarda nada en disco y **no abre ninguna conexión propia**: no conoce ningún host. `AfBillingPluginTests` lo vigila (sin `java.net`, HTTP, sockets, `Log`, `SharedPreferences` ni identificadores de publicidad; solo importa la API de facturación, `java.util` y cuatro clases de Android).
- La biblioteca sí envía a Google sus propios registros de diagnóstico (fuente `PLAY_BILLING_LIBRARY` por `com.google.android.datatransport`, leído en `classes.jar`): es Google Play, pero va a Data Safety (**Q-BILL-2**). Unity IAP traía la misma biblioteca, así que no es nuevo.
- `proguard-rules.pro` como `consumerProguardFiles`: no se renombra nada del paquete `com.amazonfill.billing` (C# lo llama por nombre). Hoy el build no minimiza (`AndroidMinifyRelease: 0`).

### 1.4 El puerto (`AF.IAP.PlayBilling`)

- `NativePlayStorePort` (sin JNI, probado en el editor) sobre `IBillingBridge`:
  - `PURCHASED` → `Purchased` (se guarda para terminarlo), salvo un no consumible ya reconocido → `Confirmed` (restauración; se queda como «hecho», así que `Acknowledge` y `Confirm` responden true sin pedir nada); `PENDING` → `PaymentPending`;
  - un consumible reconocido sin consumir (tras un `deferred_ack`) sigue siendo `Purchased`: se consume tras acreditar;
  - una consulta de productos nueva deja sin respuesta (null) a la anterior; cada consulta de compras responde a quien la pidió (por su id);
  - al desconectarse Play:
    - las consultas en vuelo y la conexión pendiente responden null o false, y se avisa `Disconnected` (PlayBillingClient deja de vender hasta `Refresh`);
    - con una hoja abierta también se avisa, pero la hoja **no** se da por fallida: es otra actividad, su respuesta llega por el resultado de la actividad y el jugador aún puede pagar (darla por fallida le enseñaría un error y quizá pagaría dos veces). Si al reconectar a petición del juego (el `Refresh` de la vuelta a primer plano, con la hoja ya cerrada) sigue sin respuesta, entonces falla (`Unavailable`; una reconexión que llega sola no cuenta): la respuesta se perdió, y si Play cobró, la compra vuelve con la consulta y la cola la acredita una vez;
    - los consumos y reconocimientos en vuelo se vuelven a pedir en el acto, cada uno como lo que era (un reconocimiento nunca se convierte en consumo);
    - sin nada de eso en vuelo no se avisa: la biblioteca se reconecta sola en la próxima llamada (`enableAutoServiceReconnection`) y la tienda sigue vendiendo;
  - si la hoja no se abre o falla con `SERVICE_DISCONNECTED` (la reconexión no funcionó), se avisa `Disconnected` para no fallar cada compra;
  - un producto que no se pidió en `FetchProducts` no se termina nunca (no se sabe si consumirlo: consumir Sin anuncios lo borraría de la restauración);
  - un aviso en vivo de error, o OK sin ninguna compra utilizable, hace fallar la hoja que abrió este puerto, con su SKU, para que no se quede colgada; sin hoja abierta no responde a nada (tampoco avisa desconexión); un aviso OK de otro SKU (un pago pendiente que Play cobra) no cierra la hoja;
  - si la respuesta en vivo de la hoja es solo un no consumible que ya estaba reconocido (no es un cobro nuevo), la hoja falla como «ya lo tiene» (`AlreadyOwned`) **antes** de pasar el aviso, para que la restauración entre en la cola ya con la hoja cerrada;
  - códigos de Play → `PlayFailureKind`: `USER_CANCELED` → cancelada; `ITEM_ALREADY_OWNED` → ya comprada (la cola la termina); sin servicio, sin facturación, producto no disponible o red → no disponible; el resto → fallida.
- `BillingCallbacks.TryDispatch`: el reparto de los avisos del `AndroidJavaProxy`, sin JNI, probado con los argumentos tal como los desempaqueta Unity. `equals`, `hashCode` y `toString` se dejan a Unity.
- `AndroidBillingBridge` y `BillingListenerProxy`: lo único con JNI, solo en el teléfono. El proxy sobrescribe `Invoke(string, object[])` y además tiene los métodos con el nombre de Java con `[Preserve]`, por si la reflexión de Unity no pasara por `Invoke` (stripping High).
- `PlayBillingFactory`: como antes, en el nuevo ensamblado.

**Por qué no todo el ensamblado es solo Android:** `GameBootstrap` necesita `PlayBillingFactory` en el editor (devuelve null) y los tests del puerto corren en el editor. Por eso solo el puente JNI está entre `#if UNITY_ANDROID && !UNITY_EDITOR`; el resto no toca JNI. `AfBillingPluginTests` comprueba que el puente empieza por ese `#if`.

---

## 2. Archivos

### 2.1 En la rama

**Nuevos:**
- `Assets/_Project/Plugins/Android/AfBilling.androidlib/`:
  - `build.gradle`;
  - `proguard-rules.pro`;
  - `src/main/AndroidManifest.xml` (sin permisos);
  - `src/main/java/com/amazonfill/billing/AfBilling.java` y `AfBillingListener.java`.
- `Assets/_Project/Plugins/Editor/AfBillingDependencies.xml` (EDM4U).
- `Assets/_Project/Scripts/PlayBilling/` (`AF.IAP.PlayBilling`): `IBillingBridge.cs` (con `BillingCodes`), `BillingRows.cs`, `NativePlayStorePort.cs`, `BillingCallbacks.cs`, `AndroidBillingBridge.cs` y `PlayBillingFactory.cs`.
  - Va fuera de `Scripts/IAP/` porque el guardián de `AF.IAP` (`StoreCatalogTests.IapAssembly_HasNoEngine_NoSystemClock_AndNoNetwork`) recorre esa carpeta entera y prohíbe `using Unity…`.
- Tests:
  - `Scripts/PlayBilling/Tests/` (`AF.IAP.PlayBilling.Tests`): `FakeBillingBridge.cs`, `NativePlayStorePortTests.cs` (31 casos), `BillingCallbacksTests.cs` (3) y `AfBillingPluginTests.cs` (6);
  - `Scripts/IAP/Tests/PlayBillingPurchaseTimeTests.cs` (2);
  - `PlayBillingVerifiedTests.DeferredAck_ThatPlayDidNotDo_IsAskedAgainOnResume_InThisSessionAndTheNext_WithoutCrediting` (nuevo, en el archivo de siempre).

**Cambiados** (archivos de esta línea de trabajo, sin cambio de contrato de las cuatro tiendas):
- `Scripts/IAP/PlayStorePort.cs`: `IPlayStorePort.Acknowledge(token)` (nuevo) y `PlayOrder.PurchasedUtc` (opcional, UTC); comentarios sin Unity IAP.
- `Scripts/IAP/PlayBillingClient.cs`: `AcknowledgeLocal` llama siempre a `Acknowledge` (consumible o no; se puede repetir); la hora de compra pasa a la `BillingTransaction`; `AcknowledgeUnsupported` se queda con su nombre para no cambiar el contrato (ahora cuenta las veces que el puerto rechaza el `deferred_ack`).
- `Scripts/IAP/VerifiedStoreService.cs`: `Resume` vuelve a pedir el `deferred_ack` de las entradas de la cola que ya lo tienen marcado (sin acreditar ni consumir); el resto, comentarios.
- `Scripts/IAP/IBillingClient.cs`: solo comentarios.
- Tests que cambian, uno por uno:
  - `FakePlayStorePort.cs`: implementa `Acknowledge` como el puerto real (no repite lo ya reconocido; lista `Acknowledged`; un consumible sigue cobrado, un no consumible queda reconocido) y `FailAcknowledge` (Play no lo hace).
  - `PlayBillingClientTests.AcknowledgeLocal_NonConsumable_AcknowledgesButKeepsItUnfinished`: espera `acknowledgePurchase` (`Acknowledged`) en lugar de `ConfirmPurchase` (`Confirmed`).
  - `PlayBillingClientTests.AcknowledgeLocal_Consumable_DoesNothing_BecauseItWouldConsume` pasa a `AcknowledgeLocal_Consumable_AcknowledgesWithoutConsuming_Once`: el comportamiento cambia por decisión del dueño (Q-IAP-2 se reabre).
  - `PlayBillingVerifiedTests.DeferredAck_Consumable_NotConsumedNorCredited_UntilTheServerAnswers`: espera el `acknowledge` sin consumo en lugar de `AcknowledgeUnsupported == 1`.
  - `Tests/PlayMode/PlayBillingPlayModeTests.cs`: su puerto de fotogramas implementa `Acknowledge`.
  - Los demás tests de `PlayBillingClient` pasan sin tocarlos.
- `Scripts/UnityIap/UnityIapStorePort.cs`: `Acknowledge` que devuelve false, solo para que la rama compile mientras Unity IAP siga; `BILL-manifest.diff` borra la carpeta entera.

### 2.2 Parches (`docs/dev/traspaso/spark-enganches/`, LF, `git -c core.autocrlf=false apply`, en este orden)

1. **`BILL-bootstrap.diff`**: `AF.UI.asmdef` (`AF.IAP.UnityIap` → `AF.IAP.PlayBilling`), `GameBootstrap.Backend.cs` (`using AF.IAP.PlayBilling;` y comentarios) y dos comentarios de `GameBootstrap.cs`. Compila solo sobre la rama: la tienda real ya es la del plugin, y Unity IAP queda sin usar.
2. **`BILL-manifest.diff`** (necesita el 1): quita Unity IAP.
   - `Packages/manifest.json` sin `com.unity.purchasing`, y `Packages/packages-lock.json` tal como lo regeneró Unity (salen `com.unity.purchasing` 5.4.4, `com.unity.services.core` 1.18.0, `com.unity.services.deployment` 1.7.2 y `com.unity.services.deployment.api` 1.1.3; `com.unity.nuget.newtonsoft-json` se queda, porque lo usa otro paquete).
   - Borra `Assets/_Project/Scripts/UnityIap/` (con `AF.IAP.UnityIap`, el `versionDefines` de `AF_UNITY_IAP`, `UnityIapStorePort.cs` y la fábrica vieja) y `Assets/Resources/` (`BillingMode.json`, Q-IAP-7, y su carpeta, que queda vacía).
   - `Assets/Plugins/Android/mainTemplate.gradle` y `ProjectSettings/AndroidResolverDependencies.xml` tal como los escribió EDM4U al construir: una línea, `implementation 'com.android.billingclient:billing:9.0.0'`.
3. **`BILL-ci.diff`**: `ci/check-no-unity-iap.ps1` (nuevo, con autotest de 9 casos; si en una carpeta hay un artefacto con Unity IAP y otro ilegible, sale con 1; lee cada entrada por trozos de 4 MB con solape y un solo búfer, así que la memoria no crece con el tamaño de `libil2cpp.so`) y comentarios de `ci/check-manifest.ps1` (BILLING sigue en la lista blanca y `-RequireBilling` en release, sin cambios de lógica; su autotest sigue en 50 casos). El paso del workflow es un enganche de texto (§6), no un parche: `.github/` no lo toca este rol.
4. **`BILL-gdd.diff`**: 08 §2.2 (fila `AF.IAP`), §1 (resumen), §7 #3 (Google Play Billing Library 9.0.0 con el plugin propio), §7 #9 (Authentication ya no entra con IAP), §10.1 (consumo con Play Billing y `deferred_ack` también de consumibles), §10.3 (flujo), §13.1 #4 y §13.3 (la fila pasa a «Google Play Billing», sin Unity).
5. **`BILL-claude.diff`**: los recuentos de `CLAUDE.md` (EditMode 4.315; PlayMode sigue en 1.940), el mapa del repositorio (`Scripts/PlayBilling/`, `Plugins/`), la compuerta nueva y la nota de que Play Billing 9.0.0 sustituye a IAP 5.4.4 del plan de instalación.
6. **`BILL-rules.diff`** (del dueño: `.claude/**` no lo tocan los agentes): `.claude/rules/sdk.md` y `.claude/agents/sdk-integrator.md` con Play Billing Library 9.0.0 en lugar de Unity IAP 5.4.4 y las rutas nuevas (`Scripts/PlayBilling/`, `Assets/_Project/Plugins/`).

Cada parche se aplica solo sobre la rama, salvo el 2, que necesita el 1. Están probados juntos (§6).

---

## 3. Modo de ensayo en el teléfono (licencias de prueba) — **no ejecutado**

La app aún no existe en Play Console (D-034). Cuando el dueño la cree, en este orden (las acciones del dueño van marcadas **[dueño]**):

1. **[dueño]** En Play Console, la app con el `applicationId` que se vaya a probar.
   - Regla dura 6: nunca el definitivo `com.<estudio>.tintalu` antes del dictamen.
   - Si se usa `com.amazonfill.dev` para la pista interna, lo decide el dueño, porque reserva ese nombre en la cuenta.
   - Los productos integrados con los ids `af_*` de 05 §4.1: tipo «producto integrado», **una sola oferta de compra** por producto (Q-BILL-3), precios por país de 05 §4.2, activos y sin compras de varias unidades (Q-BILL-4).
2. **[dueño]** Licencias de prueba: Play Console › Configuración › Pruebas de licencias, con la cuenta de Google del teléfono.
3. **[dueño]** Backend de dev desplegado (F4) y `google-services.json` de dev fuera del repo (D-039): sin `HttpBackendClient` el juego usa la tienda simulada.
4. **[dueño]** Build firmado subido a la pista de prueba interna e instalado desde Play. Los agentes no suben nada.
5. Casos, con `adb logcat -s Unity` y las sondas `AF_STATE` de desarrollo. El plugin no escribe nada; el token y el UID nunca salen en el log.
   - Mercado: precio en la moneda de la cuenta (no el de 05) y «tienda no disponible» antes de que Play responda.
   - Compra con la tarjeta de prueba que **siempre aprueba**: «Confirmando…», crédito una vez y la compra consumida (se puede volver a comprar).
   - Tarjeta que **siempre rechaza**: nada cambia. Cerrar la hoja: `cancel`, nada cambia.
   - Tarjeta **lenta que aprueba**: `pending`; al aprobarse, crédito una vez sin reabrir la app (aviso en vivo de `PurchasesUpdatedListener`).
   - Tarjeta **lenta que rechaza**: `pending` y nada más.
   - Cerrar la app justo tras pagar (antes de «Confirmando…»): al reabrir, crédito una vez.
   - Modo avión tras pagar: queda en la cola; al volver la red, crédito una vez.
   - **`deferred_ack`**: con el backend de dev apagado (`biz_backend_enabled = false` en Remote Config de dev) y `sec_purchase_deferred_ack_h` en su mínimo, comprar un consumible y esperar el plazo. En Play Console › Pedidos el pedido sale reconocido y sin reembolso; la app no acredita nada. Al encender el backend, crédito una vez y después el consumo.
   - Sin anuncios y reinstalar con la misma cuenta y el mismo UID: vuelve una vez (restauración).
   - Banda 13-17: la segunda confirmación con el precio de la tienda antes de la hoja de Play, y Play pide su autenticación de compras según los ajustes de la cuenta.
   - Banda `<13`: el Mercado no vende y Play no se conecta.
   - Recargar la escena (cambiar de idioma) con Play conectada: la tienda nueva vuelve a conectar, sin avisos repetidos.
6. Comprobar con `ci/check-manifest.ps1 -Mode dev` y `ci/check-no-unity-iap.ps1` el APK de la prueba, y que `READ_GSERVICES`, `AD_ID` y compañía siguen fuera.
7. Opcional: un proxy en el teléfono para confirmar que el juego solo habla con Play, Firebase y la API de AWS (sin `prd.insights.analytics.unity3d.com`).

---

## 4. Preguntas para el dueño

### 4.1 Q-BILL-n (este bloque)

En cada una va **aplicada la recomendada**, por la regla del dueño (D-040): «si tienen recomendada, vamos con esa». Lo que necesita aprobación expresa (regla dura 4) no se da por aprobado.

- **Q-BILL-1 · Dependencias Maven que arrastra `billing:9.0.0`. Aprobadas por el dueño el 2026-10-22: «Apruebo las dependencias Maven de com.android.billingclient:billing 9.0.0 de Q-BILL-1 con las versiones que resuelve Gradle; anótalo para D-040.» El integrador las anota en D-040 con las versiones resueltas de la tabla.** Las declara el POM de la biblioteca (`billing-9.0.0.pom`, caché de Gradle). Son exactamente las mismas que ya entraban en el AAB con Unity IAP 5.4.4, que traía `billing:9.0.0` por Gradle (`IAPAndroidDependencies.cs`). Ninguna añade permisos (comprobado en el manifiesto fusionado, §6, y en el AAR de `play-services-location` 19.0.0, que no declara ninguno). Gradle sube algunas a la versión que pide Firebase, tal como ya pasaba.
  | Dependencia (versión que pide el POM) | Versión que resuelve Gradle con Firebase (dev.apk, `:unityLibrary:dependencies`) |
  |---|---|
  | `androidx.activity:activity` 1.2.3 | 1.6.0 |
  | `androidx.core:core` 1.15.0 | 1.15.0 |
  | `com.google.android.datatransport:transport-api` 3.0.0 | 3.2.0 |
  | `com.google.android.datatransport:transport-backend-cct` 3.1.8 | 3.3.0 |
  | `com.google.android.datatransport:transport-runtime` 3.1.8 | 3.3.0 |
  | `com.google.android.gms:play-services-base` 18.5.0 | 18.10.1 |
  | `com.google.android.gms:play-services-basement` 18.9.0 | 18.11.0 |
  | `com.google.android.gms:play-services-location` 19.0.0 (con `play-services-places-placereport` 17.0.0) | 19.0.0 |
  | `com.google.android.gms:play-services-tasks` 18.2.0 | 18.4.1 |

  Las de `datatransport` son el canal de los registros de la biblioteca (Q-BILL-2). Las únicas que solo trae Play Billing son `play-services-location` y `places-placereport`; el resto ya lo pedía Firebase.

  Opciones: **(a) recomendada:** aprobarlas como parte de D-040, con las versiones que pide el POM como mínimo y tal como las resuelva Gradle junto a Firebase, y anotarlas en D-040 y en 08 §7 #3; (b) excluir `play-services-location` y `places-placereport` por Gradle (`exclude group`): la biblioteca las usa internamente, así que habría que probar en el teléfono que no falla [NV]. Mientras el dueño no decida, el parche las deja entrar como entraban con Unity IAP.
- **Q-BILL-2 · Registros de diagnóstico de la biblioteca a Google.** Play Billing Library 9.0.0 envía a Google sus propios registros (fuente `PLAY_BILLING_LIBRARY` por `datatransport` y CCT, leído en `classes.jar`). El plugin no lo controla ni añade nada; Unity IAP traía la misma biblioteca. Opciones: **(a) recomendada, aplicada en `BILL-gdd.diff`:** tratarlo como parte de Google Play Billing (Google procesa el pago); se declara en la fila de 08 §13.3 como diagnóstico enviado a Google, y el abogado revisa la fila y la política; (b) intentar apagarlo (no hay API pública: habría que excluir `transport-backend-cct`, con riesgo de romper la biblioteca).
- **Q-BILL-3 · Productos con varias ofertas de compra única** (PBL 8+). El plugin usa la oferta por defecto (`getOneTimePurchaseOfferDetails()`) para el precio y su `offerToken` para la compra, así que el precio que se ve es el que se cobra. Opciones: **(a) recomendada, aplicada:** una sola oferta por producto en Play Console (05 §4.1 no prevé ofertas), lo configura el dueño; (b) elegir la oferta por etiqueta (`getOneTimePurchaseOfferDetailsList()` y `getOfferTags()`), si algún día hay descuentos.
- **Q-BILL-4 · Compras de varias unidades.** El cliente acredita una vez por token (el servidor da el contenido del catálogo, no la cantidad). Opciones: **(a) recomendada, aplicada:** no activar «compras de varias unidades» en Play Console (dueño), y el plugin salta las compras de más de un producto o con `getQuantity() != 1` (no se acreditan ni se terminan: sin reconocer, Play las reembolsa a los 3 días, así que nadie paga N y recibe 1); (b) acreditar `quantity` veces (exige cambiar el contrato `verify-purchase` y el backend).
- **Q-BILL-5 · Plazo del `deferred_ack` frente a los 3 días de Play.** `sec_purchase_deferred_ack_h` valía 72 por defecto (08 §9; rango 48-72), justo el plazo en que Play reembolsa lo que no se reconoce; con el reintento en cada `Resume`, el margen era casi nulo. Opciones: **(a) recomendada, aplicada en `tanda5/bill-restos`:** bajar el default a 48 h en 08 §9 y regenerar los defaults con `tools/remote-config/rc_defaults.py` (`VerificationQueueConfig` compila 48; el rango sigue en 48-72, así que Remote Config aún puede subirlo); (b) dejar 72 h.

### 4.2 Las Q-IAP-n de Unity IAP (historia)

- **Q-IAP-1** (telemetría de compras a Unity): **decidida, (c)**. Este bloque la ejecuta: sin Unity IAP no hay telemetría de compras a Unity ni búsqueda en su catálogo remoto (`StoreOverrideReverseLookupService`). `ci/check-no-unity-iap.ps1` lo vigila en cada build.
- **Q-IAP-2** (`deferred_ack` de consumibles): **se reabre y se aplica como pedía Q-F3-3 3**: reconocer sin consumir (§1.2). Lo dice el parche de 08 §10.1 y §10.3.
- **Q-IAP-3, Q-IAP-4 y Q-IAP-5:** siguen como estaban (D-040, añadido de Q-IAP-n).
- **Q-IAP-6** (paquetes de Unity IAP): la aprobación **se retira** (D-040, añadido de Q-IAP-1); salen con `BILL-manifest.diff`.
- **Q-IAP-7** (`BillingMode.json`): sale con su carpeta en `BILL-manifest.diff`.

---

## 5. Privacidad (lista del integrador de SDK)

| Dato | Finalidad | Lo habilita | Data Safety (08 §13.3) |
|---|---|---|---|
| Token de compra (`purchaseToken`) | Verificar la compra en el servidor (hash en el libro mayor) | Ejecución del contrato: la compra que pide el jugador | Fila «Google Play Billing» y fila del backend propio |
| UID anónimo como `obfuscatedAccountId` en Play | Antifraude de Play y contraste en `verifyPurchase` (08 §10.1) | Interés legítimo (seguridad), ya en el canon | Fila «Google Play Billing» |
| Registros de diagnóstico de la biblioteca a Google | Funcionamiento de Play Billing (de Google) | La compra que pide el jugador; Google Play es quien procesa | Fila «Google Play Billing» (Q-BILL-2, el abogado lo revisa) |

- **Ya no sale nada a Unity:** ni eventos de compra ni búsquedas en su catálogo. El plugin no habla con ningún host: solo con el servicio de facturación de la Play Store por la biblioteca.
- **`<13`:** sin compras (`ConsentStoreService`); Play no se conecta y no se crea ningún `BillingClient` conectado.
- **13-17:** segunda confirmación con el precio de la tienda antes de la hoja de Play, más la autenticación de compras de Play según la cuenta (Q-T4 (a); el cliente no la puede forzar [NV, abogado]).
- **18+:** flujo normal.
- Nada de IDs en URLs ni en logs: ni el plugin ni el puerto escriben nada (`AfBillingPluginTests`), y los `ToString` omiten el token.
- Permisos: solo `com.android.vending.BILLING` (D-040), que declara la biblioteca; el manifiesto del plugin no declara ninguno. Ver §6.

---

## 6. Verificación

Máquina: ARM64 (Snapdragon), 64 GB, Editor 6000.3.25f1 ARM64, Python 3.14.7. Rama desde `origin/integracion-fase1-fase2` en `0ad1de46`, con una fusión al final de `509f6d24` (appcheck-release con `ci/check-release-classes.ps1`, FX-130 y la traducción 3); los parches están rehechos sobre esa fusión y la verificación final es sobre ella.

| Comprobación | Rama sola | Rama + los parches (`BILL-bootstrap`, `-manifest`, `-ci`, `-gdd`) |
|---|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 avisos, 0 errores | (no lo tocan) |
| `dotnet test dotnet/AmazonFill.slnx` | 1.002 (996 + 6 omitidos) | (no lo tocan) |
| `python tools/validate_schema.py` | 116/116 PASS, sin cambios en `tools/out` | (no lo tocan) |
| `scripts/unity-compile.ps1 -Architecture arm64` | OK | OK (sin Unity IAP: `Packages/` ya no lo trae) |
| `scripts/unity-test.ps1 -Mode EditMode` | 4.315 (4.312 + 3 omitidos) | 4.315 (4.312 + 3 omitidos); antes de esta rama, 4.272 (+43 casos nuevos) |
| `scripts/unity-test.ps1 -Mode PlayMode` | — (no cambia ningún test de PlayMode salvo el puerto de fotogramas) | 1.940 (1.669 + 271 omitidos), sin fallos |
| `ci/check-manifest.ps1 --self-test` | — | 50 casos OK |
| `ci/check-no-unity-iap.ps1 --self-test` | — | 9 casos OK; y sobre el `dev.apk` de `spark/t5-iap` (con Unity IAP): **FALLO**, el host en `global-metadata.dat` (control positivo) |
| Build de desarrollo (`BuildDev`) | — | `dev.apk` 70,37 MB; `check-manifest -Mode dev` OK con Play Billing 9.0.0; `check-no-unity-iap` OK; 16 KB: 18 `.so`, 0 fallos; `AfBilling` y `BillingClient` en `classes.dex` |
| Ensayo (`BuildRelease -afRehearsal -afApk`, stripping High) | — | **AAB 54,20 MB** (54.202.160 bytes, tras la fusión; 54.195.654 antes de ella) frente a **60,23 MB** con Unity IAP: **−6,03 MB**; APK 59,63 MB; `size-gate` OK (sin bundletool); `check-manifest -Mode dev -RequireBilling` OK (AAB y APK); `check-no-unity-iap` OK; `check-release-classes` OK (2 `.dex`, 17.192 clases); 16 KB: 18 `.so`, 0 fallos; `BillingListenerProxy` y sus métodos siguen en `global-metadata.dat` tras el stripping |
| Árbol de Gradle (`:unityLibrary:dependencies`, `debugRuntimeClasspath`, sin red) | — | `billing:9.0.0` y sus dependencias como en Q-BILL-1; `AfBilling.androidlib` no añade nada en tiempo de ejecución (`compileOnly`) |

**Permisos del manifiesto fusionado** (dev.apk y ensayo, AAB y APK): `INTERNET`, `ACCESS_NETWORK_STATE`, `VIBRATE`, `com.android.vending.BILLING`, `WAKE_LOCK`, `BIND_GET_INSTALL_REFERRER_SERVICE`, `POST_NOTIFICATIONS` y el propio `com.amazonfill.dev.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION`. **Ninguno nuevo**: son los mismos que con Unity IAP. `com.android.vending.BILLING` lo declara la biblioteca 9.0.0 (aprobado en D-040); el manifiesto del plugin no declara ninguno.

**Peso:** −6,03 MB en el AAB de ensayo (salen los ensamblados IL2CPP de Unity IAP y `com.unity.services.*`; Play Billing Library 9.0.0 sigue, ahora directa).

Los builds son locales (ARM64); la referencia es el CI Linux x64. En el teléfono no se probó nada (la app no existe en Play Console, D-034).

**Enganche del workflow** (`.github/workflows/ci.yml`, para el integrador; texto, no parche): tras el paso «Compuerta de clases prohibidas en release (firebase-appcheck-debug)», añadir

```yaml
      - name: Compuerta sin Unity IAP ni su telemetria (D-040, Q-IAP-1)
        run: pwsh -NoProfile -File ci/check-no-unity-iap.ps1 build/Android
```

y en la cabecera del workflow, junto a `check-release-classes`, la compuerta nueva. Alternativa para las clases: añadir `com.unity.purchasing` a `$ForbiddenPackages` de `ci/check-release-classes.ps1` (que mira `class_def` en los `.dex`); el host de la telemetría, en cambio, solo lo encuentra `check-no-unity-iap.ps1`, porque Unity IAP 5.4.4 lo lleva en el código C# (`global-metadata.dat`), no en Java.

---

## 7. Riesgos

- **Reloj del dispositivo y `deferred_ack`:** el plazo de 48 h se mide con el reloj del teléfono frente al `purchaseTime` de Play. Con el reloj adelantado, el reconocimiento local puede llegar antes de tiempo. Nunca acredita, así que no hay riesgo de dinero.

- **Hoja de Play sin respuesta**: si Play nunca responde a una compra y el servicio no se cae (proceso de la Play Store colgado), la tienda queda «comprando» hasta reiniciar la app; al reiniciar, lo cobrado vuelve sin terminar y se acredita una vez. Si se cae el servicio, la hoja espera su respuesta, y si al reconectar sigue sin ella, falla (§1.4): si Play había cobrado, el jugador ve un fallo y la compra se acredita después sin el contexto de la hoja.
- **Aviso OK vacío con la hoja abierta**: si Play mandara, con nuestra hoja abierta, un aviso OK sin ninguna compra utilizable que no fuera su respuesta, la hoja fallaría y su cobro llegaría después por la consulta, sin el contexto de la compra (Cántaro, Cuia de rescate): se acredita una vez con el contenido mínimo del catálogo. Muy raro.
- **Re-pedir el `deferred_ack` con la lista de antes del `Refresh`**: `ResumeBackend` llama a `Refresh` (asíncrono) y en seguida a `Resume`, que vuelve a pedir el reconocimiento con lo que el puerto sabía antes de la consulta: como mucho, una llamada de más a `acknowledgePurchase` de algo que Play ya reconoció (inofensiva).
- **Conexión que no responde**: si `startConnection` no contesta nunca, el plugin no lanza otra hasta que conteste (Play devolvería `DEVELOPER_ERROR` a una segunda en curso). `Refresh` al volver a primer plano lo vuelve a pedir; si Play sigue sin contestar, la tienda queda «no disponible».
- **Pago pendiente cobrado justo con la hoja abierta del mismo SKU**: el aviso en vivo de ese cobro respondería a la hoja (con su contexto). Cada token se acredita una vez; solo el contexto (la Cuia de rescate de una Balsa) puede caer en la compra equivocada. Muy raro.
- **Mercado con un SKU sin precio de Play**: no se vende, pero la fila enseña el precio de respaldo hasta que Play lo dé (la UI del Mercado es del escritorio; sugerencia: ocultar o desactivar la fila con `PlayBilling.CanSell`).
- **Sin red en el primer arranque**: sin UID no se conecta Play, así que la tienda queda «no disponible» hasta que responda el `restoreLedger`.
- **Escena recargada con una verificación en vuelo** (anterior a D-040, de F3-5): la tienda verificada vieja sigue viva hasta que responde su `verifyPurchase`. Sugerencia para el escritorio: soltarla también en `OnDestroy`.
- **`duplicate` de una compra de esta misma instalación**: la analítica lo cuenta como restauración y no como ingreso. Raro.
- **`deferred_ack` que Play no hizo**: se vuelve a pedir en cada `Resume` (arranque y vuelta a primer plano). Si el jugador se queda horas en el juego sin salir y sin red justo en el plazo, el siguiente intento llega al volver; con `sec_purchase_deferred_ack_h` en 48 (Q-BILL-5) quedan 24 h de margen frente a los 3 días de Play; si Remote Config lo sube a 72, el margen vuelve a ser casi nulo (08 §9).
- **Compuerta de release sin Play Billing**: el workflow de release debe pasar `-RequireBilling` a `ci/check-manifest.ps1` y llamar a `ci/check-no-unity-iap.ps1` (enganche de §6).
- **Restauración y 409** (Q-IAP-4); **monedas** (Q-IAP-3).
- **Hoja huérfana con un pago lento**: si el servicio se cae con la hoja abierta y, al volver a primer plano, la reconexión pedida responde antes que la respuesta de la hoja (pago lento, 3-D Secure), la hoja falla y el cobro llega después y se acredita una vez sin el contexto de la hoja. El jugador ve un fallo de algo que sí pagó; no hay doble crédito.
- **Esperar a la reconexión antes de abrir la hoja** (hasta 5 s): si el jugador sale de la app en ese hueco, Android puede no mostrar la hoja y no llegar respuesta; la tienda queda «comprando» hasta recargar la escena. Muy raro (servicio caído y salir en esos segundos).
- **Tienda sin Play hasta volver a primer plano**: tras un `Disconnected` (desconexión con algo en vuelo, o la hoja que no se abre con `SERVICE_DISCONNECTED` tras esperar la reconexión), `PlayBillingClient` no vende hasta el siguiente `Refresh`, que hoy solo llama `ResumeBackend` al volver a primer plano. Sugerencia para el escritorio: llamar también a `Refresh` al abrir el Mercado.
- **Re-pedir el `deferred_ack` en cada `Resume`**: también en cada `UnfinishedChanged`; cuesta una búsqueda lineal por entrada marcada y una llamada al puerto, que no repite lo que está en vuelo. Con pocas compras en la cola, despreciable.
- **Pedidos que el puerto recuerda**: `NativePlayStorePort` guarda los no consumibles ya reconocidos (para responder «hecho» sin llamar a Play); no se olvidan hasta `Dispose`. Son pocos (los SKU no consumibles de la cuenta).
- **Aviso en vivo antes de los productos**: un no consumible reconocido que llegue en vivo entre la conexión y la respuesta de los productos se toma como cobrado (sin tipo aún) y vuelve a verificarse; el servidor responde `duplicate` y no se acredita dos veces.
- **`SHA256.Create()` con stripping High** (D-035, F3-1 y F3-2) y **el proxy JNI con stripping High**: se probará en el teléfono. El proxy tiene los dos caminos (`Invoke` sobrescrito y métodos con `[Preserve]`).
- **EDM4U 1.2.189 se archiva el 26-10-2026** (08 §7 #7): `AfBillingDependencies.xml` sigue el formato que lee también `com.unity.external-dependency-manager` 2.1.0, así que la migración no lo toca.
- **Builds de Android**: PBL 9 llega por Gradle desde Google Maven (necesita red en el build, como Firebase). Verificado en los builds locales (§6); la referencia es el CI Linux.
- **El runtime de Unity 6.3** (Engine Diagnostics o Insights): pendiente aparte de D-040. La compuerta nueva no lo cubre; solo vigila el host de compras y las clases de Unity IAP.
