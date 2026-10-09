#nullable enable
using System;
using System.Collections;
using AF.Ads;
using AF.GameFlow;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// AD_GATE con los anuncios simulados en la escena (D-008 punto 4; 05 §6.1 y §6.3; tarea B2-4): en 22-60 y desde la 2.ª
    /// sesión, 1 intersticial cada 3 victorias, solo tras tocar «Siguiente»; con Canoas infinitas siguen saliendo (D-008);
    /// y ninguno al reintentar, al rendirse, en Nivel fallido ni en Sin canoas (05 §10), aunque Magic Sort lo haga.
    /// </summary>
    public sealed class AdGateTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator From22_SecondSession_OneInterstitialEveryThreeVictories()
        {
            LaunchOptions.LevelOffsetOverride = 21; // tableros 1-3 del tutorial como niveles 22-24
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(22, new MemoryProgressStore(22, 21));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            int[] expected = { 0, 0, 1 };
            for (int i = 0; i < expected.Length; i++)
            {
                FunnelSetup.Clock.Advance(TimeSpan.FromSeconds(200)); // ≥ 120 s de sesión y ≥ 150 s desde el anterior
                int before = FunnelSetup.AdsPresenter.Presented;
                VictorySetup.WinToVictoryScreen(boot);
                PlayModeHelpers.Step(boot, 900);
                Assert.That(FunnelSetup.AdsPresenter.Presented, Is.EqualTo(before), "nunca antes de tocar «Siguiente» (ni corta la celebración)");
                boot.View!.NextButton.onClick.Invoke();
                Assert.That(FunnelSetup.AdsPresenter.Presented, Is.EqualTo(expected[i]), "victoria " + (22 + i));
                Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(23 + i), "tras el anuncio, N + 1");
            }

            Assert.That(FunnelSetup.AdsPresenter.Last!.Placement, Is.EqualTo(AdPlacement.InterstitialPostLevel));
            Assert.That(FunnelSetup.AdsPresenter.Last.Format, Is.EqualTo(AdFormat.Interstitial));
            Assert.That(boot.Ads!.Ledger.LevelsSinceInterstitial, Is.EqualTo(0));
            Assert.That(boot.Ads.Ledger.InterstitialsToday, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator WithInfiniteCanoes_InterstitialsStillShow()
        {
            Profile profile = VictorySetup.Profile(0, levelsSinceInterstitial: 2);
            profile.Wallet.Canoes.InfiniteUntil = FunnelSetup.T0 + TimeSpan.FromHours(1);
            LaunchOptions.LevelOffsetOverride = 21;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(22, new MemoryProgressStore(22, 21));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.Wallet!.Canoes.IsInfinite, Is.True);
            FunnelSetup.Clock.Advance(TimeSpan.FromSeconds(200));
            VictorySetup.WinToVictoryScreen(boot);
            boot.View!.NextButton.onClick.Invoke();
            Assert.That(FunnelSetup.AdsPresenter.Presented, Is.EqualTo(1), "D-008: las Canoas infinitas no quitan los intersticiales");
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(23));
        }

        [UnityTest]
        public IEnumerator TheFailFunnel_NeverShowsInterstitials()
        {
            // Nivel 31 (tablero 6 con af_level_offset = 25), 2 canoas y la cadencia ya cumplida: rendirse, Nivel fallido,
            // Reintentar, otra vez, Sin canoas, Volver a la orilla y salir al inicio. Ningún anuncio en todo el embudo.
            Profile profile = VictorySetup.Profile(1000, canoes: 2, levelsSinceInterstitial: 10);
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            FunnelSetup.Clock.Advance(TimeSpan.FromSeconds(300));
            for (int attempt = 0; attempt < 2; attempt++)
            {
                boot.TapAnywhere();
                PlayModeHelpers.Step(boot, 50);
                FunnelSetup.GetStuck(boot);
                Assert.That(boot.PressGiveUp(), Is.True);
                Assert.That(boot.ConfirmGiveUp(), Is.True);
                Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.LevelFailed));
                Assert.That(boot.PressRetry(), Is.True);
                PlayModeHelpers.Step(boot, 50);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.NoCanoes));
            Assert.That(boot.PressNoCanoesHome(), Is.True);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(FunnelSetup.AdsPresenter.Presented, Is.EqualTo(0), "05 §10: nunca en el embudo de fallo");
            Assert.That(boot.Ads!.Ledger.LevelsSinceInterstitial, Is.EqualTo(10), "los fallos no cuentan para la cadencia");
        }
    }
}
