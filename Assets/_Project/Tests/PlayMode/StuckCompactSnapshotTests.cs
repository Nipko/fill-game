#nullable enable
using System.Collections;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de FX16a (Aguas estancadas compacto en 16:9; <c>[Explicit]</c>, no forman parte de la suite): en 360 × 640 dp
    /// (1080 × 1920 px, <see cref="HudLabelTests.Screen640"/>), en es-419 y en en-XA, al 100 % y al 130 %,
    /// <c>fx16a_l31_B_&lt;pct&gt;_16x9</c> (el 31 sin semillas y con la última canoa: rama B), <c>fx16a_l31_A_&lt;pct&gt;_16x9</c>
    /// (con 1.000 semillas: rama A, el rescate a la venta con el −50 %), <c>fx16a_l640_A_&lt;pct&gt;_16x9</c> (14 cuias en 3
    /// filas, con 5.000 semillas) y <c>fx16a_l640_B_&lt;pct&gt;_16x9</c> (sin semillas), recortadas a los 1.920 px de arriba de la
    /// captura de <see cref="SnapshotKit"/>. Las de antes de FX16a, con el sufijo <c>_antes</c>, las genera este mismo test
    /// con <c>StuckModal.RegularOnlyForTests</c>: el panel en la disposición de siempre, como en 815b2e5. Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.StuckCompactSnapshotTests</c> sin <c>-nographics</c>; los PNG quedan en
    /// <c>Logs/snapshots/&lt;locale&gt;/</c>.
    /// </summary>
    [Explicit("Capturas de FX16a; necesitan dispositivo gráfico.")]
    public sealed class StuckCompactSnapshotTests
    {
        private static int Crop => (int)HudLabelTests.Screen640.y;

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            StuckModal.RegularOnlyForTests = false;
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator Capture([Values("es-419", "en-XA")] string locale, [Values(100, 130)] int percent, [Values(false, true)] bool before)
        {
            string dir = SnapshotKit.Prepare(locale);
            string suffix = before ? "_16x9_antes" : "_16x9";
            StuckModal.RegularOnlyForTests = before;
            foreach (string branch in new[] { "B", "A" })
            {
                Use(percent);
                bool a = branch == "A";
                LaunchOptions.LevelOffsetOverride = 25;
                LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(a ? 1000 : 0, a ? 5 : 1));
                yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
                GameBootstrap boot = GameBootstrap.Current!;
                PlayModeHelpers.TakeClock(boot);
                Assert.That(boot.TapAnywhere(), Is.True, "salta la intro");
                PlayModeHelpers.Step(boot, 50);
                FunnelSetup.GetStuck(boot);
                PlayModeHelpers.Step(boot, 800);
                Assert.That(boot.View!.StuckVisible, Is.True);
                yield return Shoot(dir, "fx16a_l31_" + branch + "_" + percent + suffix);
            }

            foreach (string branch in new[] { "A", "B" })
            {
                Use(percent);
                LaunchOptions.LevelOffsetOverride = null;
                LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(branch == "A" ? 5000 : 0));
                yield return Canvas16x9LayoutTests.LoadPlaying(640);
                GameBootstrap b640 = GameBootstrap.Current!;
                Pour[]? path = Dc18SnapshotTests.FindRescuableStuck(b640.Flow!);
                Assert.That(path, Is.Not.Null, "el 640 llega a Aguas estancadas");
                MechanicTestKit.PlayPrefix(b640, path!, false);
                for (int ms = 0; ms < 8000 && b640.Flow!.Phase != FlowPhase.StuckModal; ms += PlayModeHelpers.FrameMs)
                {
                    PlayModeHelpers.Step(b640, PlayModeHelpers.FrameMs);
                }

                Assert.That(b640.Flow!.Phase, Is.EqualTo(FlowPhase.StuckModal));
                PlayModeHelpers.Step(b640, 800);
                yield return Shoot(dir, "fx16a_l640_" + branch + "_" + percent + suffix);
            }
        }

        private static void Use(int percent)
        {
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            LaunchOptions.FontScaleOverride = percent / 100f;
        }

        private static IEnumerator Shoot(string dir, string name)
        {
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.SaveTop(dir, name, Crop);
            SnapshotKit.LogBoard(name, boot);
            Debug.Log("FX16a: " + name + " en " + SnapshotKit.Locale + ": panel de " + boot.View!.Stuck.Height.ToString("0.0") + " dp ("
                + (boot.View.Stuck.Compact ? "compacto" : "de siempre") + "); tablero subido " + boot.View.BoardContainer.anchoredPosition.y.ToString("0.0") + " dp.");
        }
    }
}
