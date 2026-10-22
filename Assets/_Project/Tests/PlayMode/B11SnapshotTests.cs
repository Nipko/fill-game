#nullable enable
using System.Collections;
using AF.GameFlow;
using AF.Localization;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de la tarea B11 en <c>Logs/snapshots/es-419/</c>, a 360 × 780 dp y al 100 % (las que citan las filas nuevas de
    /// <c>strings.csv</c>, B11-loc.diff):
    /// <list type="bullet">
    /// <item><c>b11_01_canoes_card_780_100</c>: la tarjeta de las Canoas antes del 31 (04 §7.1; D-033, Q-E4);</item>
    /// <item><c>b11_02_restart_tip_{780,640}_100</c> y <c>b11_02b_restart_tip_no_lives_{780,640}_100</c>: el tooltip «Reiniciar es gratis…»
    /// encima de Aguas estancadas en el 6, con y sin canoas (D-038);</item>
    /// <item><c>b11_03_double_harvest_tip_{780,640}_100</c>: el tooltip de la Cosecha doble en la victoria del 11;</item>
    /// <item><c>b11_04_extra_cuia_finger_780_100</c>: el dedo sobre «+1 cuia» en el 8, tocando;</item>
    /// <item><c>b11_05_no_canoes_map_780_100</c>: Sin canoas con «Volver al Mapa del río» (04 §7.3);</item>
    /// <item><c>b11_06_finger_level2_setup_780_100</c>: el dedo del 2 en el paso previo (Q-B11-1), a medio viaje.</item>
    /// </list>
    /// Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.B11SnapshotTests</c>
    /// sin <c>-nographics</c>).
    /// </summary>
    [Explicit("Capturas de la tarea B11; necesitan dispositivo gráfico.")]
    public sealed class B11SnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            TutorialSlothTests.Hooks();
            GameScreen.TutorialTipsOverride = true;
        }

        [TearDown]
        public void TearDown()
        {
            SnapshotKit.Release();
            GameScreen.TutorialTipsOverride = null;
            GameScreen.TutorialFingerOverride = null;
            GameScreen.TutorialSlothOverride = null;
            GameScreen.SimulatedScreen = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator CanoesCard()
        {
            string dir = Prepare();
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(boot.Flow.LevelCard!.IntroMechanic, Is.EqualTo(LevelCardInfo.CanoesIntro));
            SnapshotKit.Attach(boot);
            PlayModeHelpers.Step(boot, 800);
            yield return Save(dir, "b11_01_canoes_card_780_100");
        }

        [UnityTest]
        public IEnumerator RestartTip([Values(true, false)] bool lives, [Values(false, true)] bool small)
        {
            string dir = Prepare(small);
            yield return TutorialSlothTests.Load(6);
            GameBootstrap boot = GameBootstrap.Current!;
            boot.Wallet!.LivesEnabled = lives;
            FunnelSetup.GetStuck(boot);
            PlayModeHelpers.Step(boot, 600);
            Assert.That(boot.View!.RestartTipVisible, Is.True);
            SnapshotKit.Attach(boot);
            yield return Save(dir, (lives ? "b11_02_restart_tip_" : "b11_02b_restart_tip_no_lives_") + (small ? "640" : "780") + "_100");
        }

        [UnityTest]
        public IEnumerator DoubleHarvestTip([Values(false, true)] bool small)
        {
            string dir = Prepare(small);
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 2500);
            Assert.That(boot.View!.DoubleHarvestTipVisible, Is.True);
            SnapshotKit.Attach(boot);
            yield return Save(dir, "b11_03_double_harvest_tip_" + (small ? "640" : "780") + "_100");
        }

        /// <summary>El tooltip de la Cosecha doble en 360 × 640 al 130 %: con la victoria compactada y, si no basta, sobre las barras, sin tapar la Cosecha.</summary>
        [UnityTest]
        public IEnumerator DoubleHarvestTip640At130()
        {
            string dir = Prepare(true);
            LaunchOptions.FontScaleOverride = 1.3f;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 2500);
            Assert.That(boot.View!.DoubleHarvestTipVisible, Is.True);
            SnapshotKit.Attach(boot);
            yield return Save(dir, "b11_03_double_harvest_tip_640_130");
        }

        [UnityTest]
        public IEnumerator ExtraCuiaFinger()
        {
            string dir = Prepare();
            yield return TutorialSlothTests.Load(8);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            for (int t = 0; t < 15000 && !view.ExtraCuiaFingerVisible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.ExtraCuiaFingerVisible, Is.True);
            TrayFinger finger = view.ExtraCuiaFinger!;
            float press = TrayFinger.AppearMs + (0.5f * TrayFinger.PressMs);
            for (int guard = 0; guard < 400 && finger.CycleTimeMs < press; guard++)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            SnapshotKit.Attach(boot);
            yield return Save(dir, "b11_04_extra_cuia_finger_780_100");
        }

        [UnityTest]
        public IEnumerator NoCanoesMap()
        {
            string dir = Prepare();
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0, 1));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            FunnelSetup.GetStuck(boot);
            boot.PressGiveUp();
            boot.ConfirmGiveUp();
            boot.PressRetry();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.NoCanoes));
            PlayModeHelpers.Step(boot, 600);
            SnapshotKit.Attach(boot);
            yield return Save(dir, "b11_05_no_canoes_map_780_100");
        }

        [UnityTest]
        public IEnumerator Level2SetupFinger()
        {
            string dir = Prepare();
            GameScreen.TutorialFingerOverride = true;
            yield return TutorialSlothTests.Load(2);
            GameBootstrap boot = GameBootstrap.Current!;
            TutorialFinger finger = TutorialFingerTests.WaitForFinger(boot, "el 2, el paso previo");
            float half = TutorialFinger.AppearMs + TutorialFinger.PressMs + (0.5f * TutorialFinger.MoveMs);
            for (int guard = 0; guard < 400 && finger.CycleTimeMs < half; guard++)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            SnapshotKit.Attach(boot);
            yield return Save(dir, "b11_06_finger_level2_setup_780_100");
        }

        private static IEnumerator Save(string dir, string name)
        {
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, name);
            Debug.Log("AF_B11_SNAPSHOT " + name);
        }

        private static string Prepare(bool small = false)
        {
            string dir = SnapshotKit.Prepare(LocLocale.SpanishLatinAmerica);
            GameScreen.SimulatedScreen = small ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
            LaunchOptions.FontScaleOverride = 1f;
            return dir;
        }
    }
}
