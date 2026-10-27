#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace AF.IAP.PlayBilling.Tests
{
    /// <summary>
    /// El reparto de los avisos de Java que hace el <c>AndroidJavaProxy</c> del teléfono (<c>BillingListenerProxy</c>), simulado: se le
    /// pasan el nombre del método de <c>AfBillingListener</c> y los argumentos tal como los desempaqueta Unity (<c>Integer</c> →
    /// <see cref="int"/>, <c>String</c> → <see cref="string"/> o null). Nada llega al oyente hasta que corre lo encolado en el hilo
    /// principal.
    /// </summary>
    public sealed class BillingCallbacksTests
    {
        private sealed class Recorder : IBillingBridgeListener
        {
            public List<string> Calls { get; } = new List<string>();

            public void OnSetupFinished(int code) => Calls.Add("setup " + code);

            public void OnDisconnected() => Calls.Add("disconnected");

            public void OnProducts(int requestId, int code, string? rows) => Calls.Add("products " + requestId + " " + code + " " + (rows ?? "null"));

            public void OnPurchases(int requestId, int code, string? rows) => Calls.Add("purchases " + requestId + " " + code + " " + (rows ?? "null"));

            public void OnPurchasesUpdated(int code, string? rows) => Calls.Add("updated " + code + " " + (rows ?? "null"));

            public void OnLaunchFailed(string? sku, int code) => Calls.Add("launch " + (sku ?? "null") + " " + code);

            public void OnConsumed(string? token, int code) => Calls.Add("consumed " + (token ?? "null") + " " + code);

            public void OnAcknowledged(string? token, int code) => Calls.Add("acknowledged " + (token ?? "null") + " " + code);

            public void OnBillingConfig(int requestId, int code, string? countryCode) => Calls.Add("config " + requestId + " " + code + " " + (countryCode ?? "null"));
        }

        private static (Recorder Target, List<Action> Queue) New() => (new Recorder(), new List<Action>());

        private static void Run(List<Action> queue)
        {
            foreach (Action a in queue)
            {
                a();
            }

            queue.Clear();
        }

        [Test]
        public void EveryListenerMethod_IsPostedToTheMainThread_WithItsArguments()
        {
            (Recorder target, List<Action> queue) = New();
            var calls = new (string Name, object?[] Args)[]
            {
                ("onSetupFinished", new object?[] { 0 }),
                ("onDisconnected", new object?[0]),
                ("onProducts", new object?[] { 3, 0, "af_coins_2\tBRL\t1" }),
                ("onPurchases", new object?[] { 4, 6, null }),
                ("onPurchasesUpdated", new object?[] { 1, null }),
                ("onLaunchFailed", new object?[] { "af_noads", 4 }),
                ("onConsumed", new object?[] { "tok.1", 0 }),
                ("onAcknowledged", new object?[] { "tok.2", 8 }),
                ("onBillingConfig", new object?[] { 5, 0, "DE" }),
            };

            foreach ((string name, object?[] args) in calls)
            {
                Assert.That(BillingCallbacks.TryDispatch(target, queue.Add, name, args), Is.True, name);
            }

            Assert.That(target.Calls, Is.Empty, "aún en el hilo de Java: nada hasta el hilo de Unity");
            Run(queue);
            Assert.That(target.Calls, Is.EqualTo(new[]
            {
                "setup 0", "disconnected", "products 3 0 af_coins_2\tBRL\t1", "purchases 4 6 null", "updated 1 null",
                "launch af_noads 4", "consumed tok.1 0", "acknowledged tok.2 8", "config 5 0 DE",
            }));
        }

        [Test]
        public void ObjectMethodsAndWrongArguments_AreLeftToUnity()
        {
            (Recorder target, List<Action> queue) = New();
            Assert.That(BillingCallbacks.TryDispatch(target, queue.Add, "hashCode", new object?[0]), Is.False);
            Assert.That(BillingCallbacks.TryDispatch(target, queue.Add, "toString", null), Is.False);
            Assert.That(BillingCallbacks.TryDispatch(target, queue.Add, "equals", new object?[] { null }), Is.False);
            Assert.That(BillingCallbacks.TryDispatch(target, queue.Add, "onSetupFinished", new object?[] { "0" }), Is.False, "un String no es un int");
            Assert.That(BillingCallbacks.TryDispatch(target, queue.Add, "onConsumed", new object?[] { 0, "tok" }), Is.False, "orden de los argumentos");
            Assert.That(BillingCallbacks.TryDispatch(target, queue.Add, "onProducts", new object?[] { 1, 0 }), Is.False, "aridad");
            Assert.That(queue, Is.Empty);
        }

        [Test]
        public void ThroughThePort_ACallbackFromJavaReachesThePlayBillingClient()
        {
            // Proxy simulado → NativePlayStorePort → PlayBillingClient: el arranque entero con los avisos encolados al hilo principal.
            var queue = new List<Action>();
            FakeBillingBridge? bridge = null;
            var port = new NativePlayStorePort(l => bridge = new FakeBillingBridge(l));
            var client = new PlayBillingClient(port, StoreCatalog.Default, "USD");
            client.Start();
            Assert.That(client.State, Is.EqualTo(PlayBillingState.Starting));

            Assert.That(BillingCallbacks.TryDispatch(port, queue.Add, "onSetupFinished", new object?[] { 0 }), Is.True);
            Run(queue);
            Assert.That(BillingCallbacks.TryDispatch(port, queue.Add, "onProducts", new object?[] { bridge!.LastProductsId, 0, "af_coins_2\tBRL\t54900000" }), Is.True);
            Run(queue);
            Assert.That(BillingCallbacks.TryDispatch(port, queue.Add, "onPurchases", new object?[] { bridge.LastPurchasesId, 0, "" }), Is.True);
            Run(queue);

            Assert.That(client.State, Is.EqualTo(PlayBillingState.Ready));
            Assert.That(client.CanSell("af_coins_2"), Is.True);
            Assert.That(client.CanSell("af_noads"), Is.False, "sin precio de Play no se vende");
            Assert.That(client.PriceOf(StoreCatalog.Default.Get("af_coins_2")).Currency, Is.EqualTo("BRL"));
            Assert.That(client.PriceOf(StoreCatalog.Default.Get("af_coins_2")).Amount, Is.EqualTo(54.90m));
        }
    }
}
