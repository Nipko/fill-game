#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Backend;
using AF.Backend.Contract;
using AF.Economy;
using AF.IAP;
using AF.Platform;
using AF.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La tienda de Play real (<see cref="PlayBillingClient"/>, D-040) bajo la tienda verificada, con respuestas de Play que llegan
    /// fotogramas después (como Unity IAP en el teléfono) y el backend falso: la hoja de Play pausa la app y el juego consulta las
    /// compras al volver antes de que llegue el aviso; Play repite el pedido; el servidor tarda. En todos los casos se acredita una
    /// vez, solo con <c>ok</c> del servidor, y Play consume después. Sin escena: solo el bucle de fotogramas.
    /// </summary>
    public sealed class PlayBillingPlayModeTests
    {
        private const string Uid = "uidTest0123456789";

        private GameObject? _host;

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
            {
                UnityEngine.Object.Destroy(_host);
            }
        }

        [UnityTest]
        public IEnumerator SheetPausesTheApp_QueryBeforeTheEvent_CreditedOnceAfterTheServer()
        {
            var clock = new ManualClock(new DateTime(2026, 10, 22, 15, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(-5));
            var disk = new MemoryProfileStore();
            Profile profile = Wallet.NewProfile(clock);
            var wallet = new Wallet(profile, disk, clock);
            var ledger = new StoreLedger(profile, wallet, disk, clock);
            StoreCatalog catalog = ledger.Catalog;
            var backend = new FakeBackendClient(clock)
            {
                GrantFor = r => CatalogGrants.For(catalog, r.Sku),
                EntitlementsFor = r => CatalogGrants.EntitlementsFor(catalog, r.Sku),
                HoldVerifyResponses = true,
            };
            _host = new GameObject("FramePlayPort");
            FramePlayPort port = _host.AddComponent<FramePlayPort>();
            port.Price("af_coins_4", "PEN", 18900000L);
            var billing = new PlayBillingClient(port, catalog, "USD");
            var verified = new VerifiedStoreService(ledger, billing, backend, clock, new BackendSwitch(), new FixedPlayerUid(Uid));
            var store = new BillingReadyStoreService(verified, billing);
            billing.UnfinishedChanged += () => verified.Resume();
            var credited = new List<string>();
            verified.VerificationChanged += e =>
            {
                if (e.State == PurchaseVerificationState.Credited)
                {
                    credited.Add(e.Sku);
                }
            };

            billing.Start();
            Assert.That(store.IsAvailable, Is.False, "Play aún no respondió");
            yield return WaitFor(() => billing.IsReady, 30);
            Assert.That(store.IsAvailable, Is.True);
            Assert.That(store.PriceOf("af_coins_4").ToString(), Is.EqualTo(new LocalPrice("PEN", 18.90m).ToString()), "precio de la tienda");

            PurchaseResult? result = null;
            int calls = 0;
            port.SilentSheet = true;
            store.Purchase(new PurchaseRequest("af_coins_4"), r =>
            {
                calls++;
                result = r;
            });
            yield return null;

            // Play cobra; la app vuelve a primer plano y consulta antes del aviso de la compra.
            string token = port.PayOpenSheetSilently();
            billing.Refresh();
            for (int i = 0; i < 5; i++)
            {
                yield return null;
                verified.Resume();
                verified.Pump();
            }

            Assert.That(result, Is.Null, "la consulta no responde a la hoja abierta");
            Assert.That(backend.VerifyRequests, Is.Empty, "ni la cola la toma mientras la hoja sigue abierta");

            port.Redeliver(token); // el aviso de la compra llega tarde: es la respuesta
            yield return WaitFor(() => backend.VerifyRequests.Count == 1, 30);
            port.Redeliver(token); // Play lo repite
            for (int i = 0; i < 5; i++)
            {
                yield return null;
                verified.Pump();
            }

            Assert.That(backend.VerifyRequests, Has.Count.EqualTo(1), "una verificación por token");
            Assert.That(wallet.Seeds, Is.EqualTo(0), "sin respuesta del servidor no hay crédito");
            Assert.That(port.Confirmed, Is.Empty, "ni consumo");

            backend.HoldVerifyResponses = false;
            backend.ReleaseVerifyResponses();
            yield return null;

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(result!.Status, Is.EqualTo(PurchaseStatus.Purchased));
            Assert.That(wallet.Seeds, Is.EqualTo(6000));
            Assert.That(credited, Is.EqualTo(new[] { "af_coins_4" }));
            Assert.That(port.Confirmed, Is.EqualTo(new[] { token }));
            Assert.That(profile.AppliedPurchases, Has.Count.EqualTo(1));

            port.Redeliver(token);
            billing.Refresh();
            verified.Resume();
            yield return null;
            yield return null;
            Assert.That(wallet.Seeds, Is.EqualTo(6000), "lo repetido no se acredita otra vez");
            Assert.That(backend.VerifyRequests, Has.Count.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator PaymentPending_SettledFramesLater_IsCreditedOnce()
        {
            var clock = new ManualClock(new DateTime(2026, 10, 22, 15, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(-5));
            var disk = new MemoryProfileStore();
            Profile profile = Wallet.NewProfile(clock);
            var wallet = new Wallet(profile, disk, clock);
            var ledger = new StoreLedger(profile, wallet, disk, clock);
            StoreCatalog catalog = ledger.Catalog;
            var backend = new FakeBackendClient(clock)
            {
                GrantFor = r => CatalogGrants.For(catalog, r.Sku),
                EntitlementsFor = r => CatalogGrants.EntitlementsFor(catalog, r.Sku),
            };
            _host = new GameObject("FramePlayPort");
            FramePlayPort port = _host.AddComponent<FramePlayPort>();
            port.Price("af_coins_2", "MXN", 19000000L);
            var billing = new PlayBillingClient(port, catalog, "USD");
            var verified = new VerifiedStoreService(ledger, billing, backend, clock, new BackendSwitch(), new FixedPlayerUid(Uid));
            billing.UnfinishedChanged += () => verified.Resume();
            billing.Start();
            yield return WaitFor(() => billing.IsReady, 30);

            port.PaymentPendingNext = true;
            PurchaseResult? result = null;
            verified.Purchase(new PurchaseRequest("af_coins_2"), r => result = r);
            yield return WaitFor(() => result != null, 30);
            Assert.That(result!.Status, Is.EqualTo(PurchaseStatus.Pending));
            Assert.That(backend.VerifyRequests, Is.Empty);

            port.SettleLast();
            yield return WaitFor(() => wallet.Seeds > 0, 30);
            for (int i = 0; i < 3; i++)
            {
                yield return null;
            }

            Assert.That(wallet.Seeds, Is.EqualTo(1000));
            Assert.That(backend.VerifyRequests, Has.Count.EqualTo(1));
            Assert.That(port.Confirmed, Has.Count.EqualTo(1));
        }

        private static IEnumerator WaitFor(Func<bool> condition, int frames)
        {
            for (int i = 0; i < frames && !condition(); i++)
            {
                yield return null;
            }

            Assert.That(condition(), Is.True, "no llegó a tiempo");
        }

        /// <summary>Puerto de Play cuyas respuestas llegan en el fotograma siguiente (en <c>Update</c>), como las de Unity IAP.</summary>
        private sealed class FramePlayPort : MonoBehaviour, IPlayStorePort
        {
            private readonly List<Action> _next = new List<Action>();
            private readonly Dictionary<string, KeyValuePair<string, long>> _prices = new Dictionary<string, KeyValuePair<string, long>>();
            private readonly List<PlayOrder> _orders = new List<PlayOrder>();
            private readonly HashSet<string> _done = new HashSet<string>();
            private string? _openSku;
            private int _counter;

            public bool SilentSheet { get; set; }

            public bool PaymentPendingNext { get; set; }

            public List<string> Confirmed { get; } = new List<string>();

            public event Action<PlayOrder>? OrderUpdated;

#pragma warning disable CS0067 // este puerto no falla compras
            public event Action<string?, PlayFailureKind>? PurchaseFailed;

            public event Action? Disconnected;
#pragma warning restore CS0067

            public void Price(string sku, string currency, long micros) => _prices[sku] = new KeyValuePair<string, long>(currency, micros);

            public void Connect(Action<bool> onDone) => _next.Add(() => onDone(true));

            public void FetchProducts(IReadOnlyList<PlayProductDefinition> products, Action<IReadOnlyList<PlayProductInfo>?> onDone)
            {
                _next.Add(() =>
                {
                    var list = new List<PlayProductInfo>();
                    foreach (KeyValuePair<string, KeyValuePair<string, long>> p in _prices)
                    {
                        list.Add(new PlayProductInfo(p.Key, p.Value.Key, p.Value.Value));
                    }

                    onDone(list);
                });
            }

            public void FetchPurchases(Action<IReadOnlyList<PlayOrder>?> onDone)
            {
                _next.Add(() =>
                {
                    var list = new List<PlayOrder>();
                    for (int i = 0; i < _orders.Count; i++)
                    {
                        if (!_done.Contains(_orders[i].Token))
                        {
                            list.Add(_orders[i]);
                        }
                    }

                    onDone(list);
                });
            }

            public void Purchase(string sku, string? obfuscatedAccountId)
            {
                Assert.That(obfuscatedAccountId, Is.EqualTo(Uid));
                _openSku = sku;
                if (SilentSheet)
                {
                    return;
                }

                bool pending = PaymentPendingNext;
                PaymentPendingNext = false;
                _next.Add(() =>
                {
                    PlayOrder order = NewOrder(sku, pending ? PlayOrderState.PaymentPending : PlayOrderState.Purchased);
                    _openSku = null;
                    OrderUpdated?.Invoke(order);
                });
            }

            /// <summary>Play cobra la hoja abierta sin avisar todavía.</summary>
            public string PayOpenSheetSilently()
            {
                PlayOrder order = NewOrder(_openSku!, PlayOrderState.Purchased);
                _openSku = null;
                return order.Token;
            }

            public void SettleLast()
            {
                PlayOrder last = _orders[_orders.Count - 1];
                var settled = new PlayOrder(last.Sku, last.Token, PlayOrderState.Purchased);
                _orders[_orders.Count - 1] = settled;
                _next.Add(() => OrderUpdated?.Invoke(settled));
            }

            public void Redeliver(string token)
            {
                for (int i = 0; i < _orders.Count; i++)
                {
                    PlayOrder order = _orders[i];
                    if (order.Token == token && !_done.Contains(token))
                    {
                        _next.Add(() => OrderUpdated?.Invoke(order));
                    }
                }
            }

            public bool Confirm(string token)
            {
                if (_done.Contains(token))
                {
                    return false;
                }

                _done.Add(token);
                Confirmed.Add(token);
                return true;
            }

            private PlayOrder NewOrder(string sku, PlayOrderState state)
            {
                _counter++;
                var order = new PlayOrder(sku, "frame.token." + _counter.ToString(System.Globalization.CultureInfo.InvariantCulture), state);
                _orders.Add(order);
                return order;
            }

            private void Update()
            {
                if (_next.Count == 0)
                {
                    return;
                }

                var now = new List<Action>(_next);
                _next.Clear();
                for (int i = 0; i < now.Count; i++)
                {
                    now[i]();
                }
            }
        }
    }
}
