#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace AF.IAP.Tests
{
    /// <summary>
    /// <see cref="PlayBillingClient"/> sobre un puerto de Play falso (D-040; 08 §10.3; 05 §4.1): conexión, precios de la tienda con el
    /// respaldo de 05 §4.2, compras (cobrada, cancelada, fallida, pendiente de pago, ya comprada), compras sin terminar entre sesiones,
    /// repeticiones de Play y cómo se terminan. El cliente no acredita: eso lo cubre <c>PlayBillingVerifiedTests</c>.
    /// </summary>
    public sealed class PlayBillingClientTests
    {
        private const string Uid = "uidTest0123456789";

        private static (PlayBillingClient Client, FakePlayStorePort Port) Ready(FakePlayAccount? account = null, string fallback = "USD")
        {
            var port = new FakePlayStorePort(account).PriceAll(StoreCatalog.Default);
            var client = new PlayBillingClient(port, StoreCatalog.Default, fallback);
            client.Start();
            Assert.That(client.State, Is.EqualTo(PlayBillingState.Ready));
            return (client, port);
        }

        private static BillingResult? Buy(PlayBillingClient client, string sku = "af_coins_2", string? uid = Uid)
        {
            BillingResult? result = null;
            client.Purchase(sku, uid, r =>
            {
                Assert.That(result, Is.Null, "onDone una sola vez");
                result = r;
            });
            return result;
        }

        // ------------------------------------------------------------------ arranque y precios

        [Test]
        public void Start_ConnectsFetchesProductsAndPurchases_ThenIsReady()
        {
            var port = new FakePlayStorePort { Async = true };
            var client = new PlayBillingClient(port, StoreCatalog.Default);
            int changes = 0;
            client.StateChanged += () => changes++;

            client.Start();
            Assert.That(client.State, Is.EqualTo(PlayBillingState.Starting));
            Assert.That(client.IsReady, Is.False);
            Assert.That(Buy(client)!.Kind, Is.EqualTo(BillingResultKind.Failed), "sin la tienda lista no se cobra nada");
            Assert.That(port.Purchases, Is.Empty);

            port.Flush();
            Assert.That(client.State, Is.EqualTo(PlayBillingState.Ready));
            Assert.That(port.FetchPurchasesCalls, Is.EqualTo(1), "lista solo tras consultar las compras sin terminar");
            Assert.That(changes, Is.EqualTo(2));
            Assert.That(port.Account.Types["af_noads"], Is.EqualTo(PlayProductType.NonConsumable));
            Assert.That(port.Account.Types["af_starter"], Is.EqualTo(PlayProductType.Consumable), "la Mochila se consume (1 por cuenta, F2c)");
            Assert.That(port.Account.Types["af_skin_m02"], Is.EqualTo(PlayProductType.NonConsumable));
            Assert.That(port.Account.Types, Has.Count.EqualTo(StoreCatalog.Default.Products.Count), "todo el catálogo de 05 §4.1");
        }

        [Test]
        public void Prices_AreTheStoresAmountAndCurrency_WithThe05PricesAsFallback()
        {
            var port = new FakePlayStorePort();
            port.Prices["af_coins_2"] = new KeyValuePair<string, long>("BRL", 4990000L);
            port.Prices["af_noads"] = new KeyValuePair<string, long>("BRL", 12900000L);
            var client = new PlayBillingClient(port, StoreCatalog.Default, "COP");
            client.Start();
            StoreCatalog catalog = StoreCatalog.Default;

            Assert.That(client.PriceOf(catalog.Get("af_coins_2")), Is.EqualTo(new LocalPrice("BRL", 4.99m)), "D-025: importe y moneda de la tienda");
            Assert.That(client.PriceOf(catalog.Get("af_noads")), Is.EqualTo(new LocalPrice("BRL", 12.90m)));
            Assert.That(client.HasStorePrice("af_coins_2"), Is.True);
            Assert.That(client.HasStorePrice("af_coins_3"), Is.False);
            Assert.That(client.PriceOf(catalog.Get("af_coins_3")), Is.EqualTo(StorePrices.Local(4.99m, "COP")), "sin precio de Play: el de 05 §4.2");
            Assert.That(client.MissingPrices, Is.EqualTo(catalog.Products.Count - 2));
            Assert.That(client.CanSell("af_coins_2"), Is.True);
            Assert.That(client.CanSell("af_coins_3"), Is.False, "sin el precio de la tienda no se vende");

            BillingResult result = Buy(client, "af_coins_3")!;
            Assert.That(result.Kind, Is.EqualTo(BillingResultKind.Failed));
            Assert.That(port.Purchases, Is.Empty, "no se abre la hoja de Play con un precio de respaldo");
        }

        [Test]
        public void Prices_BeforeTheStoreAnswers_AreThe05Ones()
        {
            var port = new FakePlayStorePort { Async = true };
            port.Prices["af_coins_2"] = new KeyValuePair<string, long>("MXN", 19000000L);
            var client = new PlayBillingClient(port, StoreCatalog.Default, "USD");
            client.Start();

            Assert.That(client.PriceOf(StoreCatalog.Default.Get("af_coins_2")), Is.EqualTo(new LocalPrice("USD", 1.99m)));
            port.Flush();
            Assert.That(client.PriceOf(StoreCatalog.Default.Get("af_coins_2")), Is.EqualTo(new LocalPrice("MXN", 19m)));
        }

        [Test]
        public void Prices_AnInvalidOrNegativeStorePrice_KeepsTheFallback()
        {
            var port = new FakePlayStorePort();
            port.Prices["af_coins_2"] = new KeyValuePair<string, long>("brl", 4990000L);
            port.Prices["af_coins_3"] = new KeyValuePair<string, long>("BRL", -1L);
            port.Prices["af_coins_4"] = new KeyValuePair<string, long>("BRL", 0L);
            var client = new PlayBillingClient(port, StoreCatalog.Default, "USD");
            client.Start();

            Assert.That(client.HasStorePrice("af_coins_2"), Is.False, "no es un código ISO 4217");
            Assert.That(client.HasStorePrice("af_coins_3"), Is.False);
            Assert.That(client.HasStorePrice("af_coins_4"), Is.False, "precio 0: producto sin datos de la tienda");
            Assert.That(client.PriceOf(StoreCatalog.Default.Get("af_coins_2")), Is.EqualTo(new LocalPrice("USD", 1.99m)));
        }

        [Test]
        public void Prices_MicrosAreRoundedWithTheCurrencysDecimals()
        {
            Assert.That(PlayBillingClient.TryLocalPrice("COP", 3900000000L, out LocalPrice cop), Is.True);
            Assert.That(cop, Is.EqualTo(new LocalPrice("COP", 3900m)));
            Assert.That(PlayBillingClient.TryLocalPrice("PEN", 3895000L, out LocalPrice pen), Is.True);
            Assert.That(pen, Is.EqualTo(new LocalPrice("PEN", 3.90m)), "la mitad hacia fuera");
        }

        [Test]
        public void Start_WithoutPlay_IsUnavailable_AndRefreshRetries()
        {
            var port = new FakePlayStorePort { ConnectOk = false };
            var client = new PlayBillingClient(port, StoreCatalog.Default);
            client.Start();
            Assert.That(client.State, Is.EqualTo(PlayBillingState.Unavailable));
            Assert.That(Buy(client)!.Kind, Is.EqualTo(BillingResultKind.Failed));

            port.ConnectOk = true;
            client.Refresh();
            Assert.That(client.State, Is.EqualTo(PlayBillingState.Ready));
            Assert.That(port.ConnectCalls, Is.EqualTo(2));
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        public void Start_FailedQueries_LeaveItUnavailable(bool failProducts, bool failPurchases)
        {
            var port = new FakePlayStorePort { FailProducts = failProducts, FailPurchases = failPurchases };
            var client = new PlayBillingClient(port, StoreCatalog.Default);
            client.Start();
            Assert.That(client.State, Is.EqualTo(PlayBillingState.Unavailable), "sin saber qué hay sin terminar, no se vende");
        }

        // ------------------------------------------------------------------ compras

        [Test]
        public void Purchase_Paid_ReturnsTheTransaction_WithTheObfuscatedAccountId()
        {
            (PlayBillingClient client, FakePlayStorePort port) = Ready();
            int unfinishedEvents = 0;
            client.UnfinishedChanged += () => unfinishedEvents++;

            BillingResult result = Buy(client)!;

            Assert.That(result.Kind, Is.EqualTo(BillingResultKind.Purchased));
            Assert.That(result.Transaction!.Sku, Is.EqualTo("af_coins_2"));
            Assert.That(result.Transaction.Token, Is.EqualTo(FakePlayAccount.Token(1)));
            Assert.That(port.Purchases[0].Value, Is.EqualTo(Uid), "08 §10.1: setObfuscatedAccountId = UID");
            Assert.That(client.UnfinishedTransactions().Count, Is.EqualTo(1), "sin terminar hasta que se acredite");
            Assert.That(port.Confirmed, Is.Empty, "el cliente nunca consume por su cuenta");
            Assert.That(unfinishedEvents, Is.EqualTo(0), "la respuesta de la compra abierta no es «otra» compra");
        }

        [Test]
        public void Purchase_WithoutAValidUid_DoesNotSetTheAccountId()
        {
            (PlayBillingClient client, FakePlayStorePort port) = Ready();
            Buy(client, uid: null);
            Buy(client, "af_coins_3", new string('x', 65));
            Assert.That(port.Purchases[0].Value, Is.Null);
            Assert.That(port.Purchases[1].Value, Is.Null, "más de 64 caracteres no cumple el contrato");
        }

        [TestCase(FakePlayOutcome.Cancel, BillingResultKind.Cancelled)]
        [TestCase(FakePlayOutcome.Fail, BillingResultKind.Failed)]
        [TestCase(FakePlayOutcome.Unavailable, BillingResultKind.Failed)]
        [TestCase(FakePlayOutcome.PaymentPending, BillingResultKind.Pending)]
        public void Purchase_NotPaid_ReturnsNoTransaction(FakePlayOutcome outcome, BillingResultKind expected)
        {
            (PlayBillingClient client, FakePlayStorePort port) = Ready();
            port.NextOutcome = outcome;

            BillingResult result = Buy(client)!;

            Assert.That(result.Kind, Is.EqualTo(expected));
            Assert.That(result.Transaction, Is.Null);
            Assert.That(client.UnfinishedTransactions(), Is.Empty, "nada que verificar");
        }

        [Test]
        public void Purchase_OnlyOneSheetAtATime()
        {
            (PlayBillingClient client, FakePlayStorePort port) = Ready();
            port.NextOutcome = FakePlayOutcome.Silent;
            Assert.That(Buy(client), Is.Null, "la hoja sigue abierta");

            Assert.That(Buy(client, "af_coins_3")!.Kind, Is.EqualTo(BillingResultKind.Failed));
            Assert.That(port.Purchases, Has.Count.EqualTo(1), "no se abre otra hoja");
        }

        [Test]
        public void PaymentPending_WhenPlayCollectsIt_ComesBackAsUnfinished()
        {
            (PlayBillingClient client, FakePlayStorePort port) = Ready();
            port.NextOutcome = FakePlayOutcome.PaymentPending;
            Assert.That(Buy(client)!.Kind, Is.EqualTo(BillingResultKind.Pending));
            int unfinishedEvents = 0;
            client.UnfinishedChanged += () => unfinishedEvents++;

            port.Settle(FakePlayAccount.Token(1));

            Assert.That(unfinishedEvents, Is.EqualTo(1), "el juego llama a Resume");
            IReadOnlyList<BillingTransaction> unfinished = client.UnfinishedTransactions();
            Assert.That(unfinished.Count, Is.EqualTo(1));
            Assert.That(unfinished[0].Token, Is.EqualTo(FakePlayAccount.Token(1)));
        }

        [Test]
        public void AlreadyOwned_Fails_AndTheOldPurchaseComesBackFromTheQuery()
        {
            var account = new FakePlayAccount();
            (PlayBillingClient client, FakePlayStorePort port) = Ready(account);
            account.Add("af_coins_2", FakePlayState.Purchased); // cobrada en otro dispositivo, aún sin consumir
            int unfinishedEvents = 0;
            client.UnfinishedChanged += () => unfinishedEvents++;
            port.NextOutcome = FakePlayOutcome.AlreadyOwned;

            Assert.That(Buy(client)!.Kind, Is.EqualTo(BillingResultKind.Failed), "no se cobró nada nuevo");
            Assert.That(unfinishedEvents, Is.EqualTo(1));
            Assert.That(client.UnfinishedTransactions()[0].Token, Is.EqualTo(FakePlayAccount.Token(1)));
        }

        [Test]
        public void TheSameOrderTwice_IsTrackedOnce()
        {
            (PlayBillingClient client, FakePlayStorePort port) = Ready();
            Buy(client);
            int unfinishedEvents = 0;
            client.UnfinishedChanged += () => unfinishedEvents++;

            port.Redeliver(FakePlayAccount.Token(1));
            client.Refresh();

            Assert.That(client.UnfinishedTransactions().Count, Is.EqualTo(1));
            Assert.That(unfinishedEvents, Is.EqualTo(0), "Play lo repite: no es una compra nueva");
        }

        [Test]
        public void AQueryWhileTheSheetIsOpen_DoesNotAnswerIt_ThePurchaseEventDoes()
        {
            // La hoja de Play pausa la app; al volver, el juego consulta las compras (Refresh) antes de que llegue la respuesta.
            (PlayBillingClient client, FakePlayStorePort port) = Ready();
            port.NextOutcome = FakePlayOutcome.Silent;
            BillingResult? result = null;
            int calls = 0;
            client.Purchase("af_coins_2", Uid, r =>
            {
                calls++;
                result = r;
            });
            int unfinishedEvents = 0;
            client.UnfinishedChanged += () => unfinishedEvents++;

            FakePlayEntry entry = port.Account.Add("af_coins_2", FakePlayState.Purchased, Uid); // Play ya la cobró
            client.Refresh();
            Assert.That(result, Is.Null, "una consulta nunca responde a la hoja abierta");
            Assert.That(client.UnfinishedTransactions(), Is.Empty, "ni se ofrece a la cola mientras la hoja sigue abierta");

            port.Redeliver(entry.Token); // llega el aviso de la compra
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(result!.Kind, Is.EqualTo(BillingResultKind.Purchased));
            Assert.That(result.Transaction!.Token, Is.EqualTo(entry.Token));
            Assert.That(unfinishedEvents, Is.EqualTo(0));
            Assert.That(client.UnfinishedTransactions().Count, Is.EqualTo(1));
        }

        [Test]
        public void AnotherSessionsOrderOfTheSameSku_FetchedWithTheSheetOpen_IsReleasedWhenTheSheetCloses()
        {
            (PlayBillingClient client, FakePlayStorePort port) = Ready();
            port.NextOutcome = FakePlayOutcome.Silent;
            BillingResult? result = null;
            client.Purchase("af_rescue", Uid, r => result = r);
            int unfinishedEvents = 0;
            client.UnfinishedChanged += () => unfinishedEvents++;
            FakePlayEntry other = port.Account.Add("af_rescue", FakePlayState.Purchased); // de otro dispositivo
            client.Refresh();
            Assert.That(result, Is.Null, "no se le pone el contexto (nivel, Cuia de rescate) de la compra abierta");

            port.Respond(FakePlayOutcome.Cancel);

            Assert.That(result!.Kind, Is.EqualTo(BillingResultKind.Cancelled), "el rechazo de la Balsa cuenta");
            Assert.That(unfinishedEvents, Is.EqualTo(1), "ya con la hoja cerrada, a la cola sin contexto");
            Assert.That(client.UnfinishedTransactions()[0].Token, Is.EqualTo(other.Token));
        }

        [Test]
        public void AnOldPaymentPendingOrder_FetchedWithTheSheetOpen_DoesNotAnswerPending()
        {
            var account = new FakePlayAccount();
            account.Add("af_coins_2", FakePlayState.PaymentPending);
            (PlayBillingClient client, FakePlayStorePort port) = Ready(account);
            port.NextOutcome = FakePlayOutcome.Silent;
            BillingResult? result = null;
            client.Purchase("af_coins_2", Uid, r => result = r);

            client.Refresh();
            Assert.That(result, Is.Null);

            port.Respond(FakePlayOutcome.Purchase);
            Assert.That(result!.Kind, Is.EqualTo(BillingResultKind.Purchased));
        }

        [Test]
        public void Disconnected_StopsSelling_AndRefreshReconnects()
        {
            (PlayBillingClient client, FakePlayStorePort port) = Ready();
            int changes = 0;
            client.StateChanged += () => changes++;

            port.Disconnect();
            Assert.That(client.State, Is.EqualTo(PlayBillingState.Unavailable));
            Assert.That(client.CanSell("af_coins_2"), Is.False);
            Assert.That(Buy(client)!.Kind, Is.EqualTo(BillingResultKind.Failed));
            Assert.That(port.Purchases, Is.Empty);

            client.Refresh();
            Assert.That(client.State, Is.EqualTo(PlayBillingState.Ready));
            Assert.That(port.ConnectCalls, Is.EqualTo(2));
            Assert.That(changes, Is.EqualTo(3), "sin Play, conectando, lista");
        }

        [Test]
        public void Refresh_WhenTheQueryFails_IsUnavailableUntilTheNextTry()
        {
            (PlayBillingClient client, FakePlayStorePort port) = Ready();
            port.FailPurchases = true;
            client.Refresh();
            Assert.That(client.State, Is.EqualTo(PlayBillingState.Unavailable));

            port.FailPurchases = false;
            client.Refresh();
            Assert.That(client.State, Is.EqualTo(PlayBillingState.Ready));
        }

        [Test]
        public void LateAnswersOfAnAbandonedStart_AreIgnored()
        {
            var port = new FakePlayStorePort { Async = true }.PriceAll(StoreCatalog.Default);
            var client = new PlayBillingClient(port, StoreCatalog.Default);
            client.Start();
            port.Disconnect(); // antes de que Play conteste
            Assert.That(client.State, Is.EqualTo(PlayBillingState.Unavailable));

            client.Refresh(); // un arranque nuevo
            port.Flush(); // contestan el primero (tarde) y el nuevo
            Assert.That(client.State, Is.EqualTo(PlayBillingState.Ready));
            Assert.That(port.FetchPurchasesCalls, Is.EqualTo(1), "solo el arranque vigente sigue con las consultas");
        }

        [Test]
        public void Create_WhenTheSdkThrows_TheStoreIsUnavailable_AndTheGameGoesOn()
        {
            PlayBillingClient client = PlayBillingClient.Create(() => throw new InvalidOperationException("sin plugin"), StoreCatalog.Default, "USD");
            client.Start();

            Assert.That(client.State, Is.EqualTo(PlayBillingState.Unavailable));
            Assert.That(Buy(client)!.Kind, Is.EqualTo(BillingResultKind.Failed));
        }

        [Test]
        public void OrdersOfUnknownProductsOrWithBadTokens_AreIgnored()
        {
            (PlayBillingClient client, FakePlayStorePort port) = Ready();
            port.Push(new PlayOrder("af_pass", "play.token.9999", PlayOrderState.Purchased));
            port.Push(new PlayOrder("af_coins_2", "token con espacios", PlayOrderState.Purchased));
            Assert.That(client.UnfinishedTransactions(), Is.Empty);

            port.NextOutcome = FakePlayOutcome.Silent;
            BillingResult? result = null;
            client.Purchase("af_coins_2", Uid, r => result = r);
            port.Push(new PlayOrder("af_coins_2", "", PlayOrderState.Purchased));
            Assert.That(result!.Kind, Is.EqualTo(BillingResultKind.Failed), "la compra abierta con un token que no cumple el contrato falla");
        }

        // ------------------------------------------------------------------ terminar

        [Test]
        public void Finish_ConfirmsOnce_AndForgetsTheOrder()
        {
            (PlayBillingClient client, FakePlayStorePort port) = Ready();
            string token = Buy(client)!.Transaction!.Token;

            client.Finish("af_coins_2", token, true);
            client.Finish("af_coins_2", token, true);
            client.Finish("af_coins_2", "play.token.0404", true);

            Assert.That(port.Confirmed, Is.EqualTo(new[] { token }));
            Assert.That(port.Account.StateOf(token), Is.EqualTo(FakePlayState.Consumed));
            Assert.That(client.UnfinishedTransactions(), Is.Empty);

            port.Redeliver(token);
            client.Refresh();
            Assert.That(client.UnfinishedTransactions(), Is.Empty, "terminada en esta sesión: no vuelve");
        }

        [Test]
        public void Finish_NoAds_IsAcknowledgedNotConsumed()
        {
            (PlayBillingClient client, FakePlayStorePort port) = Ready();
            string token = Buy(client, "af_noads")!.Transaction!.Token;

            client.Finish("af_noads", token, false);

            Assert.That(port.Account.StateOf(token), Is.EqualTo(FakePlayState.Acknowledged));
        }

        [Test]
        public void Finish_WhenTheConfirmIsLost_TheOrderComesBackNextSession()
        {
            var account = new FakePlayAccount();
            (PlayBillingClient client, FakePlayStorePort port) = Ready(account);
            string token = Buy(client)!.Transaction!.Token;
            port.CrashOnConfirm = true;

            client.Finish("af_coins_2", token, true);
            Assert.That(account.StateOf(token), Is.EqualTo(FakePlayState.Purchased), "Play no la consumió");

            (PlayBillingClient next, _) = Ready(account);
            Assert.That(next.UnfinishedTransactions().Count, Is.EqualTo(1), "vuelve: la cola la termina sin acreditarla otra vez");
        }

        [Test]
        public void Finish_WhenPlayFailsToConsume_TheNextQueryAsksAgain_WithoutTrackingItAgain()
        {
            (PlayBillingClient client, FakePlayStorePort port) = Ready();
            string token = Buy(client)!.Transaction!.Token;
            port.FailConfirm = true;
            client.Finish("af_coins_2", token, true);
            Assert.That(port.Account.StateOf(token), Is.EqualTo(FakePlayState.Purchased), "el consumo asíncrono falló");
            int unfinishedEvents = 0;
            client.UnfinishedChanged += () => unfinishedEvents++;

            port.FailConfirm = false;
            client.Refresh(); // p. ej. tras ITEM_ALREADY_OWNED, o al volver a primer plano

            Assert.That(port.Account.StateOf(token), Is.EqualTo(FakePlayState.Consumed), "se vuelve a pedir: ya estaba acreditado");
            Assert.That(client.UnfinishedTransactions(), Is.Empty, "no vuelve a la cola");
            Assert.That(unfinishedEvents, Is.EqualTo(0));
            Assert.That(Buy(client)!.Kind, Is.EqualTo(BillingResultKind.Purchased), "y se puede volver a comprar");
        }

        [Test]
        public void AcknowledgeLocal_NonConsumable_AcknowledgesButKeepsItUnfinished()
        {
            (PlayBillingClient client, FakePlayStorePort port) = Ready();
            string token = Buy(client, "af_noads")!.Transaction!.Token;

            client.AcknowledgeLocal("af_noads", token);
            client.AcknowledgeLocal("af_noads", token);

            Assert.That(port.Confirmed, Is.EqualTo(new[] { token }), "reconocer un no consumible es ConfirmPurchase, una vez");
            Assert.That(port.Account.StateOf(token), Is.EqualTo(FakePlayState.Acknowledged));
            Assert.That(client.UnfinishedTransactions().Count, Is.EqualTo(1), "sigue esperando al servidor");
            Assert.That(client.AcknowledgeUnsupported, Is.EqualTo(0));

            client.Finish("af_noads", token, false);
            Assert.That(port.Confirmed, Has.Count.EqualTo(1), "ya reconocida: Finish no vuelve a confirmar");
            Assert.That(client.UnfinishedTransactions(), Is.Empty);
        }

        [Test]
        public void AcknowledgeLocal_Consumable_DoesNothing_BecauseItWouldConsume()
        {
            (PlayBillingClient client, FakePlayStorePort port) = Ready();
            string token = Buy(client)!.Transaction!.Token;

            client.AcknowledgeLocal("af_coins_2", token);

            Assert.That(port.Confirmed, Is.Empty, "Unity IAP 5.4.4 no reconoce sin consumir (Q-IAP-2): nunca se consume sin acreditar");
            Assert.That(port.Account.StateOf(token), Is.EqualTo(FakePlayState.Purchased));
            Assert.That(client.AcknowledgeUnsupported, Is.EqualTo(1));
            Assert.That(client.UnfinishedTransactions().Count, Is.EqualTo(1));
        }

        // ------------------------------------------------------------------ entre sesiones

        [Test]
        public void NextSession_ReturnsWhatPlayStillHasUnfinished()
        {
            var account = new FakePlayAccount();
            account.Add("af_coins_2", FakePlayState.Consumed);
            FakePlayEntry unfinished = account.Add("af_coins_3", FakePlayState.Purchased);
            account.Add("af_coins_4", FakePlayState.PaymentPending);
            FakePlayEntry noAds = account.Add("af_noads", FakePlayState.Acknowledged);
            account.Add("af_coins_5", FakePlayState.Acknowledged); // un consumible que llegara como «reconocido» no se toma como restaurable
            account.Types["af_coins_5"] = PlayProductType.Consumable;

            (PlayBillingClient client, _) = Ready(account);

            var tokens = new List<string>();
            foreach (BillingTransaction t in client.UnfinishedTransactions())
            {
                tokens.Add(t.Token);
            }

            Assert.That(tokens, Is.EquivalentTo(new[] { unfinished.Token, noAds.Token }),
                "lo cobrado sin consumir y el no consumible que Play devuelve (restauración); ni lo consumido ni lo pendiente de pago");
        }
    }
}
