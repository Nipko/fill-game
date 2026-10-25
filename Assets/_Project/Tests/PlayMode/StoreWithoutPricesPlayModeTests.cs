#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Economy;
using AF.IAP;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Prueba del 2026-10-23, observación 1, en la escena <c>Game</c> real con la tienda de Play de verdad (<see cref="PlayBillingClient"/>)
    /// sobre un puerto falso: con Play conectada y ningún SKU con el precio de la tienda (la app aún no está en Play Console), el inicio
    /// no enseña el Mercado ni el Cántaro, como cuando Play no conecta; si los precios llegan en la misma sesión, el Mercado aparece. Y
    /// una compra que acaba en <see cref="PurchaseStatus.StoreUnavailable"/> (un SKU sin su precio) enseña el aviso «Las compras no están
    /// disponibles ahora» en lugar del silencio.
    /// </summary>
    public sealed class StoreWithoutPricesPlayModeTests
    {
        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetMarketOverrides();
            GameBootstrap.ResetCloudOverrides(); // también los del backend, con el puerto de Play
            GameBootstrap.ResetTelemetryOverrides();
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        private static Profile ProfileAt(int nextLevel)
        {
            Profile profile = Wallet.NewProfile(FunnelSetup.Clock);
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            return profile;
        }

        private static IEnumerator LoadLobby(InstantPlayPort port)
        {
            GameBootstrap.PlayStorePortOverride = port;
            FunnelSetup.Hooks(); // reloj manual y la hoja de compra que confirma al momento
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(26)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.SetUpMarket(), Is.True);
            boot.GetComponent<MarketRunner>().enabled = false; // el test avanza el Mercado a mano
            Assert.That(boot.PlayBilling, Is.Not.Null, "la tienda de Play sobre el puerto del test");
            boot.PlayBilling!.Start(); // sin UID de Firebase no arranca sola
            boot.TickMarket(16f);
        }

        [UnityTest]
        public IEnumerator PlayConnected_WithoutAnyPrice_TheLobbyHasNoMarket_UntilThePricesArrive()
        {
            var port = new InstantPlayPort();
            yield return LoadLobby(port);
            GameBootstrap boot = GameBootstrap.Current!;

            Assert.That(boot.PlayBilling!.IsReady, Is.True, "Play conectada");
            Assert.That(boot.Store!.IsAvailable, Is.False, "ningún SKU con el precio de la tienda");
            Assert.That(boot.MarketAvailable, Is.False);
            Assert.That(boot.MarketIcons!.MarketShown, Is.False, "el inicio sin el icono del Mercado");
            Assert.That(boot.MarketIcons.PiggyShown, Is.False, "ni el Cántaro");
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.False);
            Assert.That(boot.PressPiggy(), Is.False);

            port.Price(StoreCatalog.Handful, "COP", 3900000000L); // los productos se publican en Play Console
            boot.PlayBilling.Refresh(); // al volver a primer plano
            boot.TickMarket(16f);
            Assert.That(boot.Store.IsAvailable, Is.True);
            Assert.That(boot.MarketIcons.MarketShown, Is.True, "el Mercado aparece en la misma sesión");
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
        }

        [UnityTest]
        public IEnumerator BackendSwitchedOff_WithoutAnyPrice_TheLobbyHasNoMarket()
        {
            // biz_backend_enabled apagado y Play sin productos: sin precios de la tienda, tampoco hay Mercado que diga «no disponible»
            // (D-046, añadido del 2026-10-24; prueba del 2026-10-25, jornada 2, observación 4).
            GameBootstrap.RemoteConfigBackendOverride = new AF.RemoteConfig.FakeRemoteConfigBackend().Set("biz_backend_enabled", "false").StoreFetched();
            yield return LoadLobby(new InstantPlayPort());
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.BackendReady, Is.False);
            Assert.That(boot.StoreDownForBackend, Is.False);
            Assert.That(boot.MarketAvailable, Is.False);
            Assert.That(boot.MarketIcons!.MarketShown, Is.False);
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.False);
        }

        [UnityTest]
        public IEnumerator BackendSwitchedOff_WithPlayPrices_TheMarketOpensToSaySo()
        {
            // F3-5: con precios de Play y el backend apagado, el Mercado se abre y dice «Las compras no están disponibles ahora».
            GameBootstrap.RemoteConfigBackendOverride = new AF.RemoteConfig.FakeRemoteConfigBackend().Set("biz_backend_enabled", "false").StoreFetched();
            var port = new InstantPlayPort();
            port.Price(StoreCatalog.Handful, "COP", 3900000000L);
            yield return LoadLobby(port);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Store!.IsAvailable, Is.False, "no vende sin backend");
            Assert.That(boot.StoreDownForBackend, Is.True);
            Assert.That(boot.MarketIcons!.MarketShown, Is.True);
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            Assert.That(boot.Market!.StatusText, Is.EqualTo(MarketText.StoreUnavailable));
        }

        [UnityTest]
        public IEnumerator ABuyThatEndsStoreUnavailable_ShowsTheNotice()
        {
            var port = new InstantPlayPort();
            port.Price(StoreCatalog.Handful, "COP", 3900000000L); // solo el Puñado tiene el precio de la tienda
            yield return LoadLobby(port);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            MarketScreen market = boot.Market!;
            int row = -1;
            for (int i = 0; i < market.Rows.Count; i++)
            {
                if (market.Rows[i].Id == "af_coins_3")
                {
                    row = i;
                }
            }

            Assert.That(row, Is.GreaterThanOrEqualTo(0));
            Assert.That(boot.StoreUnavailableNoticeCount, Is.EqualTo(0));

            Assert.That(market.PressRow(row), Is.True);
            Assert.That(port.Purchases, Is.Empty, "sin su precio, la hoja de Play no se abre");
            Assert.That(boot.StoreUnavailableNoticeCount, Is.EqualTo(1));
            Assert.That(boot.SkinToast, Is.Not.Null);
            Assert.That(boot.SkinToast!.Visible, Is.True, "el aviso, en lugar del silencio");
            Assert.That(boot.SkinToast.Text, Is.EqualTo(MarketText.StoreUnavailable));
            Assert.That(MarketText.StoreUnavailable, Is.Not.Empty);
            Assert.That(boot.MetaToastCanvas!.sortingOrder, Is.GreaterThan(MarketScreen.SortingOrder), "encima del Mercado");
            yield return null;
            Assert.That(boot.SkinToast.Visible, Is.True, "sigue a la vista el tiempo de leerlo");
        }

        /// <summary>Puerto de Play que responde al momento, con los precios que se le dan y sin compras sin terminar.</summary>
        private sealed class InstantPlayPort : IPlayStorePort
        {
            private readonly Dictionary<string, KeyValuePair<string, long>> _prices = new Dictionary<string, KeyValuePair<string, long>>();

            public List<string> Purchases { get; } = new List<string>();

#pragma warning disable CS0067 // este puerto no cobra, no falla ni se desconecta
            public event Action<PlayOrder>? OrderUpdated;

            public event Action<string?, PlayFailureKind>? PurchaseFailed;

            public event Action? Disconnected;
#pragma warning restore CS0067

            public void Price(string sku, string currency, long micros) => _prices[sku] = new KeyValuePair<string, long>(currency, micros);

            public void Connect(Action<bool> onDone) => onDone(true);

            public void FetchProducts(IReadOnlyList<PlayProductDefinition> products, Action<IReadOnlyList<PlayProductInfo>?> onDone)
            {
                var list = new List<PlayProductInfo>();
                foreach (KeyValuePair<string, KeyValuePair<string, long>> p in _prices)
                {
                    list.Add(new PlayProductInfo(p.Key, p.Value.Key, p.Value.Value));
                }

                onDone(list);
            }

            public void FetchPurchases(Action<IReadOnlyList<PlayOrder>?> onDone) => onDone(new List<PlayOrder>());

            public void Purchase(string sku, string? obfuscatedAccountId) => Purchases.Add(sku);

            public bool Confirm(string token) => true;

            public bool Acknowledge(string token) => true;

            public void Dispose()
            {
            }
        }
    }
}
