#nullable enable
using System.Collections;
using AF.Economy;
using AF.GameFlow;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// «Volver al Mapa del río» de Sin canoas (tarea B11; 04 §7.3, wireframe; 04 §7.1: el Mapa del río sigue disponible con 0
    /// canoas; 04 §11: <c>af_no_canoes_choice</c> = <c>map</c>): <see cref="GameBootstrap.PressNoCanoesMap"/> y el enlace del panel
    /// (parche B11-nocanoes), desde el inicio y desde Nivel fallido, sin gastar canoas; y con <c>biz_feature_lives</c> apagado
    /// (D-038), Sin canoas no sale y el botón no hace nada.
    /// </summary>
    public sealed class NoCanoesMapTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator FromTheLobby_ClosesNoCanoes_AndOpensTheMap()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(NoCanoesProfile(30)));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.PressNoCanoesMap(), Is.False, "sin Sin canoas delante, nada");
            boot.Lobby!.CanoesButton.onClick.Invoke();
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.NoCanoes));
            NoCanoesPanel panel = boot.Lobby.NoCanoes;
            Assert.That(panel.HomeButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text, Is.EqualTo(TutorialTipsText.NoCanoesMap),
                "04 §7.3: «Volver al Mapa del río»");

            panel.HomeButton.onClick.Invoke();
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby), "Sin canoas se cerró");
            Assert.That(boot.NoCanoesMapOpened, Is.True);
            Assert.That(boot.RiverMapView!.Visible, Is.True, "el Mapa, encima del inicio");
            Assert.That(boot.LastNoCanoesChoice, Is.EqualTo(GameBootstrap.NoCanoesMapChoice), "af_no_canoes_choice = map (04 §11)");
            Assert.That(boot.Profile!.Wallet.Canoes.Count, Is.EqualTo(0), "sin canoa");
            Assert.That(boot.CloseRiverMap(), Is.True);
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby), "al cerrar el Mapa, el inicio");
        }

        [UnityTest]
        public IEnumerator FromLevelFailed_LeavesTheLevel_AndOpensTheMap()
        {
            LaunchOptions.LevelOffsetOverride = 25; // el 31 juega el tablero 6 del tutorial (FunnelSetup.GetStuck)
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0, 1));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            FunnelSetup.GetStuck(boot);
            boot.PressGiveUp();
            boot.ConfirmGiveUp();
            boot.PressRetry();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.NoCanoes), "Reintentar con 0 canoas abre Sin canoas (04 §7.3)");
            boot.View!.NoCanoes.HomeButton.onClick.Invoke();
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby), "sale del nivel como «Volver al inicio»");
            Assert.That(boot.NoCanoesMapOpened, Is.True);
            Assert.That(boot.RiverMapView!.Visible, Is.True);
            Assert.That(boot.LastNoCanoesChoice, Is.EqualTo(GameBootstrap.NoCanoesMapChoice));
            Assert.That(boot.Wallet!.Canoes.Count, Is.EqualTo(0), "volver al Mapa no cuesta canoa");
        }

        [UnityTest]
        public IEnumerator WithoutLives_NoCanoesNeverOpens_D038()
        {
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0, 0));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.Wallet!.LivesEnabled = false; // lo que deja ConnectWallet con biz_feature_lives = false (08 §9; D-038)
            boot.TapAnywhere();
            FunnelSetup.GetStuck(boot);
            Assert.That(boot.PressRestart(), Is.True, "D-038: desde el 31, como en 1-30");
            PlayModeHelpers.Step(boot, 50);
            Assert.That(boot.Flow!.Phase, Is.Not.EqualTo(FlowPhase.NoCanoes), "sin canoas, nunca Sin canoas (0 canoas no bloquea)");
            Assert.That(boot.PressNoCanoesMap(), Is.False, "sin Sin canoas delante, el botón no hace nada");
            Assert.That(boot.NoCanoesMapOpened, Is.False);
        }

        private static Profile NoCanoesProfile(int maxLevel)
        {
            Profile profile = FunnelSetup.Profile(500, 0);
            profile.Progress.MaxLevel = maxLevel;
            profile.Progress.NextLevel = maxLevel + 1;
            return profile;
        }
    }
}
