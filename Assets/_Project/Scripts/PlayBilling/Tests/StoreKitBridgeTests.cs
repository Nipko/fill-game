#nullable enable
using System.Collections.Generic;
using NUnit.Framework;

namespace AF.IAP.PlayBilling.Tests
{
    /// <summary>
    /// El plugin de StoreKit 2 (<c>Plugins/iOS/AfStoreKit.swift</c>; fase M3 de <c>docs/dev/traspaso/mac-ios.md</c>) habla el protocolo del de
    /// Play: <see cref="StoreKitBridgeEvents"/> pasa cada aviso al de Play que toca, y <see cref="NativePlayStorePort"/> entiende las filas
    /// tal como las escribe el Swift (el token es el id numérico de la transacción; Ask to Buy, una fila pendiente con el token
    /// <c>pending.&lt;sku&gt;</c>; un no consumible ya terminado, una fila reconocida).
    /// </summary>
    public sealed class StoreKitBridgeTests
    {
        private const string Coins = "af_coins_2";
        private const string NoAds = "af_noads";
        private const string Tx = "2000000987654321";

        private static readonly PlayProductDefinition[] Defs =
        {
            new PlayProductDefinition(Coins, PlayProductType.Consumable),
            new PlayProductDefinition(NoAds, PlayProductType.NonConsumable),
        };

        // Como el Swift: sku, token, estado, hora en ms, reconocida.
        private static string Row(string sku, string token, int state = BillingRows.Purchased, bool acknowledged = false) =>
            sku + "\t" + token + "\t" + state + "\t1793750400000\t" + (acknowledged ? "1" : "0");

        private static (NativePlayStorePort Port, FakeBillingBridge Bridge) WithProducts()
        {
            FakeBillingBridge? bridge = null;
            var port = new NativePlayStorePort(l => bridge = new FakeBillingBridge(l));
            port.FetchProducts(Defs, _ => { });
            StoreKitBridgeEvents.Dispatch(port, StoreKitBridgeEvents.Products, bridge!.LastProductsId, BillingCodes.Ok, null,
                Coins + "\tUSD\t1990000\n" + NoAds + "\tUSD\t4990000");
            return (port, bridge);
        }

        [Test]
        public void Dispatch_SendsEveryKind_ToItsPlayCallback()
        {
            var l = new RecordingListener();
            Assert.That(StoreKitBridgeEvents.Dispatch(l, StoreKitBridgeEvents.Setup, 0, BillingCodes.Ok, null, null), Is.True);
            StoreKitBridgeEvents.Dispatch(l, StoreKitBridgeEvents.Disconnected, 0, 0, null, null);
            StoreKitBridgeEvents.Dispatch(l, StoreKitBridgeEvents.Products, 7, BillingCodes.Ok, null, "p");
            StoreKitBridgeEvents.Dispatch(l, StoreKitBridgeEvents.Purchases, 8, BillingCodes.Ok, null, "c");
            StoreKitBridgeEvents.Dispatch(l, StoreKitBridgeEvents.PurchasesUpdated, 0, BillingCodes.Ok, null, "u");
            StoreKitBridgeEvents.Dispatch(l, StoreKitBridgeEvents.LaunchFailed, 0, BillingCodes.UserCanceled, Coins, null);
            StoreKitBridgeEvents.Dispatch(l, StoreKitBridgeEvents.Consumed, 0, BillingCodes.Ok, Tx, null);
            StoreKitBridgeEvents.Dispatch(l, StoreKitBridgeEvents.Acknowledged, 0, BillingCodes.Ok, Tx, null);
            StoreKitBridgeEvents.Dispatch(l, StoreKitBridgeEvents.BillingConfig, 9, BillingCodes.FeatureNotSupported, null, null);
            Assert.That(StoreKitBridgeEvents.Dispatch(l, 42, 0, 0, null, null), Is.False, "un tipo desconocido no hace nada");
            Assert.That(l.Calls, Is.EqualTo(new[]
            {
                "setup:0", "disconnected", "products:7:0:p", "purchases:8:0:c", "updated:0:u", "launchFailed:af_coins_2:1",
                "consumed:" + Tx + ":0", "acknowledged:" + Tx + ":0", "config:9:-2:",
            }));
        }

        [Test]
        public void APurchase_WithTheTransactionIdAsToken_IsConsumedAfterCrediting()
        {
            (NativePlayStorePort port, FakeBillingBridge bridge) = WithProducts();
            var orders = new List<PlayOrder>();
            port.OrderUpdated += orders.Add;
            port.Purchase(Coins, null);
            StoreKitBridgeEvents.Dispatch(port, StoreKitBridgeEvents.PurchasesUpdated, 0, BillingCodes.Ok, null, Row(Coins, Tx));
            Assert.That(orders.Count, Is.EqualTo(1));
            Assert.That(orders[0].Sku, Is.EqualTo(Coins));
            Assert.That(orders[0].Token, Is.EqualTo(Tx));
            Assert.That(orders[0].State, Is.EqualTo(PlayOrderState.Purchased));
            Assert.That(port.Confirm(Tx), Is.True);
            Assert.That(bridge.Consumes, Is.EqualTo(new[] { Tx }), "consumible: Consume, que en StoreKit es finish()");
        }

        [Test]
        public void AskToBuy_IsPending_AndIsNeverFinished()
        {
            (NativePlayStorePort port, FakeBillingBridge bridge) = WithProducts();
            var orders = new List<PlayOrder>();
            port.OrderUpdated += orders.Add;
            port.Purchase(Coins, null);
            StoreKitBridgeEvents.Dispatch(port, StoreKitBridgeEvents.PurchasesUpdated, 0, BillingCodes.Ok, null,
                Row(Coins, "pending." + Coins, BillingRows.Pending));
            Assert.That(orders.Count, Is.EqualTo(1));
            Assert.That(orders[0].State, Is.EqualTo(PlayOrderState.PaymentPending));
            Assert.That(port.Confirm("pending." + Coins), Is.False, "un pendiente no se termina");
            Assert.That(bridge.Consumes, Is.Empty);
        }

        [Test]
        public void AFinishedNonConsumable_ComesBackConfirmed_AndACancelFailsTheSheet()
        {
            (NativePlayStorePort port, FakeBillingBridge bridge) = WithProducts();
            IReadOnlyList<PlayOrder>? got = null;
            port.FetchPurchases(o => got = o);
            StoreKitBridgeEvents.Dispatch(port, StoreKitBridgeEvents.Purchases, bridge.LastPurchasesId, BillingCodes.Ok, null,
                Row(NoAds, "2000000111111111", acknowledged: true));
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Count, Is.EqualTo(1));
            Assert.That(got[0].State, Is.EqualTo(PlayOrderState.Confirmed), "Sin anuncios de currentEntitlements: ya terminada");

            var failures = new List<PlayFailureKind>();
            port.PurchaseFailed += (_, kind) => failures.Add(kind);
            port.Purchase(Coins, null);
            StoreKitBridgeEvents.Dispatch(port, StoreKitBridgeEvents.LaunchFailed, 0, BillingCodes.UserCanceled, Coins, null);
            Assert.That(failures, Is.EqualTo(new[] { PlayFailureKind.Cancelled }));
        }

        private sealed class RecordingListener : IBillingBridgeListener
        {
            public List<string> Calls { get; } = new List<string>();

            public void OnSetupFinished(int code) => Calls.Add("setup:" + code);

            public void OnDisconnected() => Calls.Add("disconnected");

            public void OnProducts(int requestId, int code, string? rows) => Calls.Add("products:" + requestId + ":" + code + ":" + rows);

            public void OnPurchases(int requestId, int code, string? rows) => Calls.Add("purchases:" + requestId + ":" + code + ":" + rows);

            public void OnPurchasesUpdated(int code, string? rows) => Calls.Add("updated:" + code + ":" + rows);

            public void OnLaunchFailed(string? sku, int code) => Calls.Add("launchFailed:" + sku + ":" + code);

            public void OnConsumed(string? token, int code) => Calls.Add("consumed:" + token + ":" + code);

            public void OnAcknowledged(string? token, int code) => Calls.Add("acknowledged:" + token + ":" + code);

            public void OnBillingConfig(int requestId, int code, string? countryCode) => Calls.Add("config:" + requestId + ":" + code + ":" + countryCode);
        }
    }
}
