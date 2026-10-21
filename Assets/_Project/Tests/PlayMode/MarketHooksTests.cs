#nullable enable
using System;
using System.Collections;
using AF.Boosters;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;
using AF.LiveOps;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Los enganches de B5 (parches B5-bootstrap y B5-profile) en la escena <c>Game</c> real: <c>Start</c> crea el Mercado; Atrás de
    /// Android cierra el Mercado y la tarjeta de oferta (también encima de la victoria); el Mercado cuenta como pantalla de pago
    /// (la ventanita de casa no sale); <c>geo_tier</c> sale de la región del teléfono (Q-E3 (a)); y el Cántaro vive en el perfil.
    /// </summary>
    public sealed class MarketHooksTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetMarketOverrides();
            GameBootstrap.ResetSessionOverrides();
            GameBootstrap.ResetTelemetryOverrides();
            LaunchOptions.Reset();
        }

        private static Profile ProfileAt(int nextLevel)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            return profile;
        }

        [UnityTest]
        public IEnumerator Start_CreatesTheMarket_AndBackClosesIt()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(26)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Market, Is.Not.Null, "SetUpMarket en Start (B5-bootstrap)");
            boot.GetComponent<MarketRunner>().enabled = false;
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            Assert.That(boot.Lobby!.Visible, Is.True);
            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(boot.Market!.Visible, Is.False);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby), "Atrás no sale del inicio con el Mercado abierto");
        }

        [UnityTest]
        public IEnumerator TheMarket_IsAPaymentScreen_ForTheLobby()
        {
            // D-013, punto 6: con una pantalla de pago delante no sale la ventanita de casa, y el Mapa no se abre encima.
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(26)));
            GameBootstrap boot = GameBootstrap.Current!;
            boot.GetComponent<MarketRunner>().enabled = false;
            LobbyScreen lobby = boot.Lobby!;
            yield return null;
            Assert.That(lobby.CommerceOpen, Is.False);
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            yield return null;
            Assert.That(lobby.CommerceOpen, Is.True, "CommerceOpen incluye el Mercado (B5-bootstrap)");
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.False);
            boot.Market!.Close();
            yield return null;
            Assert.That(lobby.CommerceOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator BackClosesTheOfferCard_OnTheVictory()
        {
            GameBootstrap.SessionStoreOverride = new InMemorySessionStore(new SessionRecord { Sessions = 1 });
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(ProfileAt(12));
            yield return PlayModeHelpers.LoadGame(12, new MemoryProgressStore(12, 11));
            GameBootstrap boot = GameBootstrap.Current!;
            boot.GetComponent<MarketRunner>().enabled = false;
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            boot.TickMarket(16f);
            boot.TickMarket(GameBootstrap.OfferCardDelayMs + 16f);
            Assert.That(boot.OfferCardView!.Visible, Is.True);
            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(boot.OfferCardView.Visible, Is.False);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.VictoryScreen), "la victoria sigue debajo");
        }

        [UnityTest]
        public IEnumerator GeoTier_ComesFromThePhoneRegion()
        {
            LaunchOptions.DeviceLocaleOverride = new ManualDeviceLocale("es-ES");
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(26)));
            Assert.That(GameBootstrap.Current!.Services!.Geo, Is.EqualTo(GeoTier.T1));

            LaunchOptions.DeviceLocaleOverride = new ManualDeviceLocale("es-CO");
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(26)));
            Assert.That(GameBootstrap.Current!.Services!.Geo, Is.EqualTo(GeoTier.T3));
        }

        [UnityTest]
        public IEnumerator GeoTier_GoesToTheAnalyticsCommonParams()
        {
            var sink = new AF.Analytics.FakeAnalyticsSink();
            GameBootstrap.AnalyticsSinkOverride = sink;
            LaunchOptions.DeviceLocaleOverride = new ManualDeviceLocale("es-ES");
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(26)));
            GameBootstrap boot = GameBootstrap.Current!;
            boot.GetComponent<MarketRunner>().enabled = false;
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            AF.Analytics.AnalyticsEvent view = sink.Last("af_shop_view")!;
            Assert.That(view.Find("geo_tier")!.Value.Text, Is.EqualTo("T1"), "08 §8.1: común geo_tier (Q-E3 (a))");
            Assert.That(sink.Properties.TryGetValue("geo_tier", out string? property) ? property : null, Is.EqualTo("T1"),
                "08 §8.1: también como propiedad de usuario");
        }

        [UnityTest]
        public IEnumerator TheJar_LivesInTheProfile()
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(ProfileAt(20));
            yield return PlayModeHelpers.LoadGame(20, new MemoryProgressStore(20, 19));
            GameBootstrap boot = GameBootstrap.Current!;
            boot.GetComponent<MarketRunner>().enabled = false;
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            boot.TickMarket(16f);
            Assert.That(boot.Profile!.Piggy, Is.Not.Null, "B5-profile: el bloque piggy");
            Assert.That(boot.Profile.Piggy!.Seeds, Is.EqualTo(boot.Piggy!.FillFor(boot.Flow!.HarvestTier)));
            Assert.That(boot.Profile.Piggy.LastLevel, Is.EqualTo(20));
            StringAssert.Contains("\"piggy\": {", ProfileJson.Serialize(boot.Profile));
        }
    }
}
