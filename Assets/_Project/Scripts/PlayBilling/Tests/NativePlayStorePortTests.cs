#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace AF.IAP.PlayBilling.Tests
{
    /// <summary>
    /// <see cref="NativePlayStorePort"/> sobre un puente falso que responde como el plugin Java (D-040, añadido de Q-IAP-1; 08 §10.3):
    /// conexión, precios en micros, estados de las compras, avisos en vivo, consumo y reconocimiento (también el <c>deferred_ack</c> de un
    /// consumible, sin consumir), desconexión y <c>Dispose</c>.
    /// </summary>
    public sealed class NativePlayStorePortTests
    {
        private static (NativePlayStorePort Port, FakeBillingBridge Bridge) New()
        {
            FakeBillingBridge? bridge = null;
            var port = new NativePlayStorePort(l => bridge = new FakeBillingBridge(l));
            return (port, bridge!);
        }

        private static readonly PlayProductDefinition[] Defs =
        {
            new PlayProductDefinition("af_coins_2", PlayProductType.Consumable),
            new PlayProductDefinition("af_noads", PlayProductType.NonConsumable),
        };

        private static (NativePlayStorePort Port, FakeBillingBridge Bridge) WithProducts()
        {
            (NativePlayStorePort port, FakeBillingBridge bridge) = New();
            port.FetchProducts(Defs, _ => { });
            ((IBillingBridgeListener)port).OnProducts(bridge.LastProductsId, BillingCodes.Ok, "");
            return (port, bridge);
        }

        private static IReadOnlyList<PlayOrder>? Query(NativePlayStorePort port, FakeBillingBridge bridge, string rows, int code = BillingCodes.Ok)
        {
            IReadOnlyList<PlayOrder>? got = null;
            bool answered = false;
            port.FetchPurchases(o =>
            {
                Assert.That(answered, Is.False, "onDone una vez");
                answered = true;
                got = o;
            });
            ((IBillingBridgeListener)port).OnPurchases(bridge.LastPurchasesId, code, rows);
            Assert.That(answered, Is.True);
            return got;
        }

        // ------------------------------------------------------------------ conexión

        [Test]
        public void Connect_AnswersEveryWaiter_OnceWithTheSetupCode()
        {
            (NativePlayStorePort port, FakeBillingBridge bridge) = New();
            var answers = new List<bool>();
            port.Connect(answers.Add);
            port.Connect(answers.Add);
            Assert.That(bridge.ConnectCalls, Is.EqualTo(2));
            Assert.That(answers, Is.Empty, "espera a Play");

            ((IBillingBridgeListener)port).OnSetupFinished(BillingCodes.Ok);
            ((IBillingBridgeListener)port).OnSetupFinished(BillingCodes.Ok);
            Assert.That(answers, Is.EqualTo(new[] { true, true }));

            port.Connect(answers.Add);
            ((IBillingBridgeListener)port).OnSetupFinished(BillingCodes.BillingUnavailable);
            Assert.That(answers, Is.EqualTo(new[] { true, true, false }));
        }

        [Test]
        public void Connect_PluginThrows_AnswersFalse()
        {
            (NativePlayStorePort port, FakeBillingBridge bridge) = New();
            bridge.Throw = true;
            bool? connected = null;
            port.Connect(c => connected = c);
            Assert.That(connected, Is.False);
        }

        // ------------------------------------------------------------------ productos

        [Test]
        public void FetchProducts_SendsEverySku_AndMapsCurrencyAndMicros_SkippingBadRows()
        {
            (NativePlayStorePort port, FakeBillingBridge bridge) = New();
            IReadOnlyList<PlayProductInfo>? got = null;
            port.FetchProducts(Defs, p => got = p);
            Assert.That(bridge.ProductQueries[0].Value, Is.EqualTo("af_coins_2\naf_noads"));

            string rows = FakeBillingBridge.Rows(
                "af_coins_2\tBRL\t5490000",
                "af_noads\tBRL\t0", // sin precio: no es un regalo
                "af_x\tBRL\t-1",
                "af_y\tBRL",
                "af_z\tBRL\tabc");
            ((IBillingBridgeListener)port).OnProducts(bridge.LastProductsId, BillingCodes.Ok, rows);

            Assert.That(got, Has.Count.EqualTo(1));
            Assert.That(got![0].Sku, Is.EqualTo("af_coins_2"));
            Assert.That(got[0].Currency, Is.EqualTo("BRL"));
            Assert.That(got[0].PriceMicros, Is.EqualTo(5490000L), "el importe de Play, nunca la cadena formateada (D-025)");
        }

        [Test]
        public void FetchProducts_Error_AnswersNull_AndANewQuerySupersedesTheOld()
        {
            (NativePlayStorePort port, FakeBillingBridge bridge) = New();
            var answers = new List<IReadOnlyList<PlayProductInfo>?>();
            port.FetchProducts(Defs, answers.Add);
            int first = bridge.LastProductsId;
            port.FetchProducts(Defs, answers.Add);
            Assert.That(answers, Has.Count.EqualTo(1), "la anterior responde null en el acto");
            Assert.That(answers[0], Is.Null);

            ((IBillingBridgeListener)port).OnProducts(first, BillingCodes.Ok, "af_coins_2\tBRL\t1");
            Assert.That(answers, Has.Count.EqualTo(1), "la respuesta tardía de la anterior no cuenta");

            ((IBillingBridgeListener)port).OnProducts(bridge.LastProductsId, BillingCodes.NetworkError, null);
            Assert.That(answers, Has.Count.EqualTo(2));
            Assert.That(answers[1], Is.Null);
        }

        [Test]
        public void Purchase_WhileAProductsRefreshIsInFlight_IsLaunched_AndBothAnswersArrive()
        {
            // Una compra abierta con un Refresh en vuelo va al plugin, que la abre con los productos de la consulta anterior
            // (AfBilling.java solo los sustituye cuando llega la respuesta nueva): no espera a la consulta ni falla por ella.
            (NativePlayStorePort port, FakeBillingBridge bridge) = WithProducts();
            var orders = new List<PlayOrder>();
            port.OrderUpdated += orders.Add;
            IReadOnlyList<PlayProductInfo>? refreshed = null;
            port.FetchProducts(Defs, p => refreshed = p);
            int refresh = bridge.LastProductsId;
            var failures = new List<PlayFailureKind>();
            port.PurchaseFailed += (_, kind) => failures.Add(kind);

            port.Purchase("af_coins_2", "uidTest0123456789");
            Assert.That(bridge.Launches, Has.Count.EqualTo(1), "no espera a la consulta en vuelo");
            Assert.That(failures, Is.Empty);

            ((IBillingBridgeListener)port).OnPurchasesUpdated(BillingCodes.Ok, FakeBillingBridge.Row("af_coins_2", "tok.refresh"));
            Assert.That(orders, Has.Count.EqualTo(1));
            Assert.That(orders[0].Token, Is.EqualTo("tok.refresh"));

            ((IBillingBridgeListener)port).OnProducts(refresh, BillingCodes.Ok, "af_coins_2\tBRL\t4990000");
            Assert.That(refreshed, Has.Count.EqualTo(1), "la consulta responde después, sin tocar la compra");
            Assert.That(port.Confirm("tok.refresh"), Is.True, "el pedido sigue siendo un consumible que se puede terminar");
        }

        // ------------------------------------------------------------------ compras

        [Test]
        public void FetchPurchases_MapsEveryState_AndThePurchaseTime()
        {
            (NativePlayStorePort port, FakeBillingBridge bridge) = WithProducts();
            string rows = FakeBillingBridge.Rows(
                FakeBillingBridge.Row("af_coins_2", "tok.1"),
                FakeBillingBridge.Row("af_coins_2", "tok.2", acknowledged: true), // deferred_ack: sigue sin consumir
                FakeBillingBridge.Row("af_noads", "tok.3"),
                FakeBillingBridge.Row("af_noads", "tok.4", acknowledged: true),
                FakeBillingBridge.Row("af_coins_2", "tok.5", BillingRows.Pending, 0),
                "af_coins_2\ttok.6\t0\t1\t0", // UNSPECIFIED_STATE: se salta
                "af_coins_2\ttok.7\t1\t1"); // campos de menos

            IReadOnlyList<PlayOrder> orders = Query(port, bridge, rows)!;

            Assert.That(orders, Has.Count.EqualTo(5));
            Assert.That(orders[0].State, Is.EqualTo(PlayOrderState.Purchased));
            Assert.That(orders[1].State, Is.EqualTo(PlayOrderState.Purchased), "un consumible reconocido se consume tras acreditar");
            Assert.That(orders[2].State, Is.EqualTo(PlayOrderState.Purchased));
            Assert.That(orders[3].State, Is.EqualTo(PlayOrderState.Confirmed), "restauración");
            Assert.That(orders[4].State, Is.EqualTo(PlayOrderState.PaymentPending));
            Assert.That(orders[0].PurchasedUtc, Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(1792000000000L).UtcDateTime));
            Assert.That(orders[0].PurchasedUtc!.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(orders[4].PurchasedUtc, Is.Null, "purchaseTime 0: sin hora");
            Assert.That(port.KnownPurchases, Is.EqualTo(4), "lo cobrado sin terminar y el no consumible ya reconocido");
            Assert.That(port.Acknowledge("tok.4"), Is.True, "ya reconocido: hecho, sin pedirlo otra vez");
            Assert.That(port.Confirm("tok.4"), Is.True);
            Assert.That(bridge.Acknowledges, Is.Empty);
        }

        [Test]
        public void FetchPurchases_ErrorOrDisconnect_AnswersNull_AndTellsTheClient()
        {
            (NativePlayStorePort port, FakeBillingBridge bridge) = WithProducts();
            Assert.That(Query(port, bridge, "", BillingCodes.ServiceDisconnected), Is.Null);

            var answers = new List<IReadOnlyList<PlayOrder>?>();
            IReadOnlyList<PlayProductInfo>? products = new List<PlayProductInfo>();
            bool? connected = null;
            int disconnected = 0;
            port.Disconnected += () => disconnected++;
            port.FetchPurchases(answers.Add);
            port.FetchPurchases(answers.Add);
            port.FetchProducts(Defs, p => products = p);
            port.Connect(c => connected = c);

            ((IBillingBridgeListener)port).OnDisconnected();

            Assert.That(answers, Is.EqualTo(new IReadOnlyList<PlayOrder>?[] { null, null }));
            Assert.That(products, Is.Null);
            Assert.That(connected, Is.False);
            Assert.That(disconnected, Is.EqualTo(1));

            ((IBillingBridgeListener)port).OnPurchases(bridge.LastPurchasesId, BillingCodes.Ok, FakeBillingBridge.Row("af_coins_2", "tok.1"));
            Assert.That(answers, Has.Count.EqualTo(2), "lo que responda tarde ya no va a nadie");
        }

        [Test]
        public void Purchase_LaunchesWithTheUid_AndLiveUpdatesComeAsOrders()
        {
            (NativePlayStorePort port, FakeBillingBridge bridge) = WithProducts();
            var orders = new List<PlayOrder>();
            port.OrderUpdated += orders.Add;

            port.Purchase("af_coins_2", "uidTest0123456789");
            Assert.That(bridge.Launches, Has.Count.EqualTo(1));
            Assert.That(bridge.Launches[0].Key, Is.EqualTo("af_coins_2"));
            Assert.That(bridge.Launches[0].Value, Is.EqualTo("uidTest0123456789"), "setObfuscatedAccountId = UID (08 §10.1)");

            ((IBillingBridgeListener)port).OnPurchasesUpdated(BillingCodes.Ok, FakeBillingBridge.Rows(
                FakeBillingBridge.Row("af_coins_2", "tok.live"),
                FakeBillingBridge.Row("af_coins_2", "tok.cash", BillingRows.Pending)));

            Assert.That(orders, Has.Count.EqualTo(2));
            Assert.That(orders[0].Token, Is.EqualTo("tok.live"));
            Assert.That(orders[0].State, Is.EqualTo(PlayOrderState.Purchased));
            Assert.That(orders[1].State, Is.EqualTo(PlayOrderState.PaymentPending), "PENDING: no se verifica ni se acredita");
            Assert.That(port.Confirm("tok.cash"), Is.False, "un pendiente de pago no se termina");
        }

        [TestCase(BillingCodes.UserCanceled, PlayFailureKind.Cancelled)]
        [TestCase(BillingCodes.ItemAlreadyOwned, PlayFailureKind.AlreadyOwned)]
        [TestCase(BillingCodes.ServiceDisconnected, PlayFailureKind.Unavailable)]
        [TestCase(BillingCodes.BillingUnavailable, PlayFailureKind.Unavailable)]
        [TestCase(BillingCodes.ItemUnavailable, PlayFailureKind.Unavailable)]
        [TestCase(BillingCodes.NetworkError, PlayFailureKind.Unavailable)]
        [TestCase(BillingCodes.DeveloperError, PlayFailureKind.Failed)]
        [TestCase(BillingCodes.Error, PlayFailureKind.Failed)]
        public void PurchaseFailures_MapToTheKindOfFailure(int code, PlayFailureKind expected)
        {
            (NativePlayStorePort port, FakeBillingBridge _) = WithProducts();
            var failures = new List<KeyValuePair<string?, PlayFailureKind>>();
            port.PurchaseFailed += (sku, kind) => failures.Add(new KeyValuePair<string?, PlayFailureKind>(sku, kind));

            ((IBillingBridgeListener)port).OnPurchasesUpdated(code, null);
            Assert.That(failures, Is.Empty, "sin hoja abierta, un error en vivo no responde a nada");

            port.Purchase("af_coins_2", null);
            ((IBillingBridgeListener)port).OnPurchasesUpdated(code, null);
            ((IBillingBridgeListener)port).OnPurchasesUpdated(code, null);
            port.Purchase("af_noads", null);
            ((IBillingBridgeListener)port).OnLaunchFailed("af_noads", code);

            Assert.That(failures, Has.Count.EqualTo(2), "una respuesta por hoja");
            Assert.That(failures[0].Key, Is.EqualTo("af_coins_2"), "Play no dice el SKU: es el de la hoja abierta");
            Assert.That(failures[0].Value, Is.EqualTo(expected));
            Assert.That(failures[1].Key, Is.EqualTo("af_noads"));
            Assert.That(failures[1].Value, Is.EqualTo(expected));
        }

        [Test]
        public void Purchase_PluginThrows_Fails()
        {
            (NativePlayStorePort port, FakeBillingBridge bridge) = WithProducts();
            PlayFailureKind? kind = null;
            port.PurchaseFailed += (_, k) => kind = k;
            bridge.Throw = true;
            port.Purchase("af_coins_2", null);
            Assert.That(kind, Is.EqualTo(PlayFailureKind.Failed));
        }

        // ------------------------------------------------------------------ terminar

        [Test]
        public void Confirm_Consumable_Consumes_OnceInFlight_AndReleasesOnlyWhenPlayDidIt()
        {
            (NativePlayStorePort port, FakeBillingBridge bridge) = WithProducts();
            Query(port, bridge, FakeBillingBridge.Row("af_coins_2", "tok.1"));

            Assert.That(port.Confirm("tok.1"), Is.True);
            Assert.That(port.Confirm("tok.1"), Is.True, "ya pedido");
            Assert.That(bridge.Consumes, Is.EqualTo(new[] { "tok.1" }), "consumeAsync una vez");
            Assert.That(bridge.Acknowledges, Is.Empty);

            ((IBillingBridgeListener)port).OnConsumed("tok.1", BillingCodes.Error);
            Assert.That(port.KnownPurchases, Is.EqualTo(1), "falló: sigue cobrado y se puede volver a pedir");
            Assert.That(port.Confirm("tok.1"), Is.True);
            Assert.That(bridge.Consumes, Has.Count.EqualTo(2));

            ((IBillingBridgeListener)port).OnConsumed("tok.1", BillingCodes.Ok);
            Assert.That(port.KnownPurchases, Is.EqualTo(0));
            Assert.That(port.Confirm("tok.1"), Is.False, "ya terminado");
        }

        [Test]
        public void Confirm_Consumable_ItemNotOwned_MeansAlreadyConsumed()
        {
            (NativePlayStorePort port, FakeBillingBridge bridge) = WithProducts();
            Query(port, bridge, FakeBillingBridge.Row("af_coins_2", "tok.1"));
            port.Confirm("tok.1");
            ((IBillingBridgeListener)port).OnConsumed("tok.1", BillingCodes.ItemNotOwned);
            Assert.That(port.KnownPurchases, Is.EqualTo(0));
        }

        [Test]
        public void Confirm_NonConsumable_Acknowledges_AndAnAlreadyAcknowledgedOneIsDone()
        {
            (NativePlayStorePort port, FakeBillingBridge bridge) = WithProducts();
            Query(port, bridge, FakeBillingBridge.Row("af_noads", "tok.1"));

            Assert.That(port.Confirm("tok.1"), Is.True);
            Assert.That(bridge.Acknowledges, Is.EqualTo(new[] { "tok.1" }), "un no consumible no se consume");
            Assert.That(bridge.Consumes, Is.Empty);
            ((IBillingBridgeListener)port).OnAcknowledged("tok.1", BillingCodes.Ok);
            Assert.That(port.Confirm("tok.1"), Is.True, "ya reconocido: terminado");
            Assert.That(port.Acknowledge("tok.1"), Is.True);
            Assert.That(bridge.Acknowledges, Has.Count.EqualTo(1));

            Assert.That(port.Confirm("tok.unknown"), Is.False);
        }

        [Test]
        public void Acknowledge_Consumable_DeferredAck_WithoutConsuming_ThenConsumedAfterCrediting()
        {
            // 08 §10.3: a las 48 h sin respuesta del servidor, acknowledge local sin consumir (D-040, añadido de Q-IAP-1; Q-IAP-2 se reabre).
            (NativePlayStorePort port, FakeBillingBridge bridge) = WithProducts();
            Query(port, bridge, FakeBillingBridge.Row("af_coins_2", "tok.1"));

            Assert.That(port.Acknowledge("tok.1"), Is.True);
            Assert.That(port.Acknowledge("tok.1"), Is.True, "en vuelo");
            Assert.That(bridge.Acknowledges, Is.EqualTo(new[] { "tok.1" }));
            ((IBillingBridgeListener)port).OnAcknowledged("tok.1", BillingCodes.Ok);
            Assert.That(port.Acknowledge("tok.1"), Is.True, "ya reconocido");
            Assert.That(bridge.Acknowledges, Has.Count.EqualTo(1));
            Assert.That(bridge.Consumes, Is.Empty, "nunca se consume sin acreditar");
            Assert.That(port.KnownPurchases, Is.EqualTo(1), "sigue cobrado");

            // La consulta de otra sesión lo trae reconocido y cobrado: sigue siendo para consumir.
            IReadOnlyList<PlayOrder> again = Query(port, bridge, FakeBillingBridge.Row("af_coins_2", "tok.1", acknowledged: true))!;
            Assert.That(again[0].State, Is.EqualTo(PlayOrderState.Purchased));

            Assert.That(port.Confirm("tok.1"), Is.True);
            Assert.That(bridge.Consumes, Is.EqualTo(new[] { "tok.1" }), "tras acreditar, se consume");
        }

        [Test]
        public void Acknowledge_Failed_IsNotRetriedAlone_ButTheCallerCanAskAgain_AndItIsIdempotent()
        {
            (NativePlayStorePort port, FakeBillingBridge bridge) = WithProducts();
            Assert.That(port.Acknowledge("tok.nope"), Is.False);
            Query(port, bridge, FakeBillingBridge.Row("af_coins_2", "tok.1"));

            Assert.That(port.Acknowledge("tok.1"), Is.True);
            ((IBillingBridgeListener)port).OnAcknowledged("tok.1", BillingCodes.NetworkError);
            Assert.That(bridge.Acknowledges, Has.Count.EqualTo(1), "sin reintento solo: lo repite VerifiedStoreService.Resume");

            Assert.That(port.Acknowledge("tok.1"), Is.True);
            Assert.That(port.Acknowledge("tok.1"), Is.True, "en vuelo: no se repite");
            Assert.That(bridge.Acknowledges, Has.Count.EqualTo(2));
            ((IBillingBridgeListener)port).OnAcknowledged("tok.1", BillingCodes.Ok);
            Assert.That(port.Acknowledge("tok.1"), Is.True, "ya reconocido");
            Assert.That(bridge.Acknowledges, Has.Count.EqualTo(2));

            Query(port, bridge, FakeBillingBridge.Row("af_coins_2", "tok.2"));
            bridge.Throw = true;
            Assert.That(port.Acknowledge("tok.2"), Is.False, "el plugin falló: no se pidió");
        }

        [Test]
        public void Confirm_ProductThatWasNotFetched_IsNeverFinished()
        {
            // Sin el tipo no se sabe si consumirlo: consumir Sin anuncios lo borraría de Play (y de la restauración).
            (NativePlayStorePort port, FakeBillingBridge bridge) = New();
            IReadOnlyList<PlayOrder> orders = Query(port, bridge, FakeBillingBridge.Rows(
                FakeBillingBridge.Row("af_noads", "tok.1"),
                FakeBillingBridge.Row("af_noads", "tok.2", acknowledged: true)))!;

            Assert.That(orders[1].State, Is.EqualTo(PlayOrderState.Purchased), "sin el tipo, no se da por restauración");
            Assert.That(port.Confirm("tok.1"), Is.False);
            Assert.That(port.Confirm("tok.2"), Is.False);
            Assert.That(bridge.Consumes, Is.Empty);
            Assert.That(bridge.Acknowledges, Is.Empty);
        }

        [Test]
        public void LiveUpdate_OkWithoutAnyUsablePurchase_FailsOnlyASheetThisPortOpened()
        {
            (NativePlayStorePort port, FakeBillingBridge _) = WithProducts();
            var failures = new List<KeyValuePair<string?, PlayFailureKind>>();
            int orders = 0;
            port.PurchaseFailed += (sku, k) => failures.Add(new KeyValuePair<string?, PlayFailureKind>(sku, k));
            port.OrderUpdated += _ => orders++;

            ((IBillingBridgeListener)port).OnPurchasesUpdated(BillingCodes.Ok, null);
            Assert.That(failures, Is.Empty, "sin hoja abierta no hay nada que responder");

            port.Purchase("af_coins_2", null);
            ((IBillingBridgeListener)port).OnPurchasesUpdated(BillingCodes.Ok, "af_coins_2\ttok.1\t0\t1\t0"); // UNSPECIFIED_STATE
            Assert.That(failures, Has.Count.EqualTo(1), "la hoja no se queda colgada");
            Assert.That(failures[0].Key, Is.EqualTo("af_coins_2"));
            Assert.That(failures[0].Value, Is.EqualTo(PlayFailureKind.Failed));
            Assert.That(orders, Is.EqualTo(0));
        }

        [Test]
        public void Disconnect_WithNothingInFlight_KeepsTheStore_AndReissuesAnInFlightConsume()
        {
            // enableAutoServiceReconnection: la biblioteca se reconecta en la próxima llamada; una caída sin nada en vuelo no deja la
            // tienda sin vender. Un consumo en vuelo (ya acreditado) se vuelve a pedir en el acto (Play no consume dos veces).
            (NativePlayStorePort port, FakeBillingBridge bridge) = WithProducts();
            Query(port, bridge, FakeBillingBridge.Row("af_coins_2", "tok.1"));
            int disconnected = 0;
            port.Disconnected += () => disconnected++;

            port.Confirm("tok.1");
            ((IBillingBridgeListener)port).OnDisconnected();
            Assert.That(disconnected, Is.EqualTo(0));
            Assert.That(bridge.Consumes, Is.EqualTo(new[] { "tok.1", "tok.1" }));
            Assert.That(port.Confirm("tok.1"), Is.True, "en vuelo otra vez");
            Assert.That(bridge.Consumes, Has.Count.EqualTo(2));

            ((IBillingBridgeListener)port).OnConsumed("tok.1", BillingCodes.Ok);
            ((IBillingBridgeListener)port).OnConsumed("tok.1", BillingCodes.ItemNotOwned);
            Assert.That(port.KnownPurchases, Is.EqualTo(0));
        }

        [Test]
        public void Disconnect_WithADeferredAckInFlight_AcknowledgesAgain_NeverConsumes()
        {
            // D-035: nunca se consume sin acreditar. El reconocimiento en vuelo se repite como reconocimiento.
            (NativePlayStorePort port, FakeBillingBridge bridge) = WithProducts();
            Query(port, bridge, FakeBillingBridge.Row("af_coins_2", "tok.1"));

            port.Acknowledge("tok.1");
            ((IBillingBridgeListener)port).OnDisconnected();

            Assert.That(bridge.Acknowledges, Is.EqualTo(new[] { "tok.1", "tok.1" }));
            Assert.That(bridge.Consumes, Is.Empty, "un consumible sin acreditar no se consume");
        }

        [Test]
        public void Disconnect_WithTheSheetOpen_StopsSelling_ButTheSheetStillGetsItsAnswer()
        {
            // La hoja de Play es otra actividad: su respuesta llega por el resultado de la actividad aunque el servicio se caiga, y el
            // jugador aún puede pagar. No se da por fallida (el jugador vería un error y quizá pagaría dos veces).
            (NativePlayStorePort port, FakeBillingBridge _) = WithProducts();
            var failures = new List<PlayFailureKind>();
            var orders = new List<PlayOrder>();
            int disconnected = 0;
            port.PurchaseFailed += (_, k) => failures.Add(k);
            port.OrderUpdated += orders.Add;
            port.Disconnected += () => disconnected++;

            port.Purchase("af_noads", "uidTest0123456789");
            ((IBillingBridgeListener)port).OnDisconnected();
            Assert.That(disconnected, Is.EqualTo(1), "deja de vender hasta Refresh");
            Assert.That(failures, Is.Empty);

            // Un pago pendiente de otro SKU que Play cobra ahora no cierra la hoja.
            ((IBillingBridgeListener)port).OnPurchasesUpdated(BillingCodes.Ok, FakeBillingBridge.Row("af_coins_2", "tok.cash"));
            ((IBillingBridgeListener)port).OnPurchasesUpdated(BillingCodes.Ok, FakeBillingBridge.Row("af_noads", "tok.1"));
            Assert.That(orders, Has.Count.EqualTo(2));
            Assert.That(orders[1].Sku, Is.EqualTo("af_noads"));

            ((IBillingBridgeListener)port).OnPurchasesUpdated(BillingCodes.UserCanceled, null);
            Assert.That(failures, Is.Empty, "la hoja ya respondió: un error tardío no responde a nada");
        }

        [Test]
        public void Disconnect_WithTheSheetOpen_AndItsAnswerLost_TheReconnectFailsIt()
        {
            // La respuesta de la hoja se perdió (la Play Store murió con la hoja abierta): al reconectar (Refresh al volver a primer plano)
            // la hoja falla, para que la tienda no se quede «comprando» toda la sesión.
            (NativePlayStorePort port, FakeBillingBridge _) = WithProducts();
            var failures = new List<KeyValuePair<string?, PlayFailureKind>>();
            port.PurchaseFailed += (sku, k) => failures.Add(new KeyValuePair<string?, PlayFailureKind>(sku, k));

            port.Purchase("af_noads", null);
            ((IBillingBridgeListener)port).OnDisconnected();
            port.Connect(_ => { });
            ((IBillingBridgeListener)port).OnSetupFinished(BillingCodes.BillingUnavailable);
            Assert.That(failures, Is.Empty, "sin reconectar no se sabe nada");

            port.Connect(_ => { });
            ((IBillingBridgeListener)port).OnSetupFinished(BillingCodes.Ok);
            Assert.That(failures, Has.Count.EqualTo(1));
            Assert.That(failures[0].Key, Is.EqualTo("af_noads"));
            Assert.That(failures[0].Value, Is.EqualTo(PlayFailureKind.Unavailable));

            ((IBillingBridgeListener)port).OnSetupFinished(BillingCodes.Ok);
            Assert.That(failures, Has.Count.EqualTo(1), "una vez");

            // Una reconexión que llega sola (sin Connect pedido), con la hoja aún en pantalla, no la da por perdida.
            port.Purchase("af_noads", null);
            ((IBillingBridgeListener)port).OnDisconnected();
            ((IBillingBridgeListener)port).OnSetupFinished(BillingCodes.Ok);
            Assert.That(failures, Has.Count.EqualTo(1));
            ((IBillingBridgeListener)port).OnPurchasesUpdated(BillingCodes.UserCanceled, null);
            Assert.That(failures, Has.Count.EqualTo(2), "su respuesta la cierra");

            // Sin caída del servicio, reconectar no toca la hoja abierta.
            port.Purchase("af_coins_2", null);
            port.Connect(_ => { });
            ((IBillingBridgeListener)port).OnSetupFinished(BillingCodes.Ok);
            Assert.That(failures, Has.Count.EqualTo(2), "sin caída, la hoja sigue abierta");
        }

        [Test]
        public void LiveError_WithoutASheet_DoesNotStopSelling()
        {
            (NativePlayStorePort port, FakeBillingBridge _) = WithProducts();
            int disconnected = 0;
            port.Disconnected += () => disconnected++;

            ((IBillingBridgeListener)port).OnPurchasesUpdated(BillingCodes.ServiceDisconnected, null);
            Assert.That(disconnected, Is.EqualTo(0));

            port.Purchase("af_coins_2", null);
            ((IBillingBridgeListener)port).OnPurchasesUpdated(BillingCodes.ServiceDisconnected, null);
            Assert.That(disconnected, Is.EqualTo(1));
        }

        [Test]
        public void SheetAnsweredWithAnAlreadyAcknowledgedNonConsumable_FailsAsAlreadyOwned_AndRestores()
        {
            // Play responde a la hoja de Sin anuncios con la compra que ya estaba reconocida: no es un cobro nuevo. La hoja no se queda
            // colgada (falla como «ya lo tiene») y la compra sigue por la restauración (UnfinishedChanged).
            FakeBillingBridge? bridge = null;
            var port = new NativePlayStorePort(l => bridge = new FakeBillingBridge(l));
            var client = new PlayBillingClient(port, StoreCatalog.Default, "USD");
            client.Start();
            var listener = (IBillingBridgeListener)port;
            listener.OnSetupFinished(BillingCodes.Ok);
            listener.OnProducts(bridge!.LastProductsId, BillingCodes.Ok, "af_noads\tBRL\t24900000");
            listener.OnPurchases(bridge.LastPurchasesId, BillingCodes.Ok, "");
            Assert.That(client.State, Is.EqualTo(PlayBillingState.Ready));

            int unfinished = 0;
            bool sheetClosedFirst = false;
            BillingResult? result = null;
            client.UnfinishedChanged += () =>
            {
                unfinished++;
                sheetClosedFirst = result != null; // con la hoja abierta, VerifiedStoreService.Resume se saltaría el SKU
            };
            client.Purchase("af_noads", "uidTest0123456789", r => result = r);
            listener.OnPurchasesUpdated(BillingCodes.Ok, FakeBillingBridge.Row("af_noads", "tok.owned", acknowledged: true));

            Assert.That(result, Is.Not.Null, "la hoja respondió");
            Assert.That(result!.Kind, Is.EqualTo(BillingResultKind.Failed), "no se cobró nada nuevo");
            Assert.That(unfinished, Is.EqualTo(1), "la restauración sigue por la cola");
            Assert.That(sheetClosedFirst, Is.True, "la hoja se cierra antes de ofrecer la restauración a la cola");
            Assert.That(client.UnfinishedTransactions(), Has.Count.EqualTo(1));
            Assert.That(bridge.PurchaseQueries, Has.Count.EqualTo(2), "y se vuelven a consultar las compras");
        }

        [Test]
        public void LaunchFailed_BecauseTheServiceIsGone_StopsSelling()
        {
            (NativePlayStorePort port, FakeBillingBridge _) = WithProducts();
            int disconnected = 0;
            port.Disconnected += () => disconnected++;

            port.Purchase("af_coins_2", null);
            ((IBillingBridgeListener)port).OnLaunchFailed("af_coins_2", BillingCodes.ItemUnavailable);
            Assert.That(disconnected, Is.EqualTo(0));

            port.Purchase("af_coins_2", null);
            ((IBillingBridgeListener)port).OnLaunchFailed("af_coins_2", BillingCodes.ServiceDisconnected);
            Assert.That(disconnected, Is.EqualTo(1), "PlayBillingClient deja de vender hasta Refresh");
        }

        // ------------------------------------------------------------------ soltar

        [Test]
        public void Dispose_ReleasesThePlugin_AnswersWaiters_AndIgnoresLateCallbacks()
        {
            (NativePlayStorePort port, FakeBillingBridge bridge) = WithProducts();
            Query(port, bridge, FakeBillingBridge.Row("af_coins_2", "tok.1"));
            var purchases = new List<IReadOnlyList<PlayOrder>?>();
            port.FetchPurchases(purchases.Add);
            int events = 0;
            port.OrderUpdated += _ => events++;
            port.Disconnected += () => events++;

            port.Dispose();
            port.Dispose();

            Assert.That(bridge.Disposed, Is.EqualTo(1), "endConnection una vez");
            Assert.That(purchases, Is.EqualTo(new IReadOnlyList<PlayOrder>?[] { null }));
            ((IBillingBridgeListener)port).OnPurchasesUpdated(BillingCodes.Ok, FakeBillingBridge.Row("af_coins_2", "tok.2"));
            ((IBillingBridgeListener)port).OnDisconnected();
            Assert.That(events, Is.EqualTo(0));
            Assert.That(port.Confirm("tok.1"), Is.False);
            Assert.That(bridge.Consumes, Is.Empty);

            bool? connected = null;
            port.Connect(c => connected = c);
            Assert.That(connected, Is.False);
        }
    }
}
