#nullable enable
using System.Collections;
using AF.GameFlow;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión del banner de entrada a un mundo (07 §2.2, regla 1; 02 §6; decisión del dueño del 2026-10-11), con el
    /// catálogo real y la pantalla simulada de <see cref="SnapshotKit"/> (360 × 780 dp): <c>69_banner_l11</c> y
    /// <c>69b_banner_l41</c> (ya quietos: Capi presentando y Rosa fuera), y para la revisión el instante en que Rosa asoma y Capi
    /// levanta la pata (69c), el soplo (69d), Rosa sumergiéndose al cerrar (69e), el 81 y el 141 (69f y 69g), 16:9 (69h) y la
    /// versión quieta de Reducir movimiento (69i). Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.WorldBannerSnapshotTests</c> sin <c>-nographics</c>; los PNG van a <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class WorldBannerSnapshotTests
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
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureBanners()
        {
            string dir = SnapshotKit.Prepare();
            yield return Capture(dir, 11, 1400, "69_banner_l11");
            yield return Capture(dir, 41, 1400, "69b_banner_l41");
            yield return Capture(dir, 41, 330, "69c_banner_l41_surface");
            yield return Capture(dir, 41, 860, "69d_banner_l41_spray");
            yield return Capture(dir, 81, 1400, "69f_banner_l81");
            yield return Capture(dir, 141, 1400, "69g_banner_l141");

            // Rosa se sumerge al cerrar: 120 ms después del toque, con la intro ya en marcha detrás.
            yield return CardSetup.Load(41);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.Step(boot, 1100);
            WorldBannerSetup.Tap(boot.View!.WorldBannerView);
            PlayModeHelpers.TickExact(boot, 120);
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "69e_banner_l41_dive");
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Intro));

            // 16:9 (1080 × 1920 px; GameScreen lo maqueta en 438,75 × 780 dp).
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            yield return Capture(dir, 41, 1400, "69h_banner_l41_16x9");
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;

            // Reducir movimiento: quieto desde el principio (Capi presentando y Rosa fuera, sin soplo).
            yield return CardSetup.Load(140);
            boot = GameBootstrap.Current!;
            boot.Flow!.ReduceMotion = true;
            boot.Flow.Load(141);
            PlayModeHelpers.Step(boot, 400); // tras el fundido de entrada: la versión quieta, entera
            Assert.That(boot.View!.WorldBannerView.Visible, Is.True);
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "69i_banner_l141_reduced");
        }

        private static IEnumerator Capture(string dir, int level, int ms, string name)
        {
            yield return CardSetup.Load(level);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.WorldBannerView.Visible, Is.True, "nivel " + level);
            PlayModeHelpers.TickExact(boot, ms - 300); // CardSetup.Load ya avanzó 300 ms
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, name);
            Debug.Log("AF_A4 " + name + ": " + view.LastWorldBannerProbe);
        }
    }
}
