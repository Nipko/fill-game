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
    /// levanta la pata (69c), el soplo, una bruma baja con 3 gotas (69d), la salida antes de «Llenar y teñir» a mitad de la
    /// inmersión de Rosa (69e) y al principio del fundido final (69j), el 81 y el 141 (69f y 69g), 16:9 (69h) y la versión quieta
    /// de Reducir movimiento (69i). El arte se pide en el acto (<see cref="CardSetup.Load"/>): instantes exactos. Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter
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
            yield return Capture(dir, 41, (int)(WorldBannerView.SprayStartMs + (0.5f * WorldBannerView.SprayMs)), "69d_banner_l41_spray");
            yield return Capture(dir, 81, 1400, "69f_banner_l81");
            yield return Capture(dir, 141, 1400, "69g_banner_l141");

            // La salida, antes de «Llenar y teñir» (07 §5.1): a mitad de la inmersión de Rosa (69e) y al principio del fundido
            // final (69j). En los dos instantes el flujo sigue en LEVEL_CARD y la intro, retenida en t = 0 detrás.
            yield return ExitAt(dir, WorldBannerView.ExitMs * WorldBannerView.DiveEnd * 0.5f, "69e_banner_l41_dive");
            yield return ExitAt(dir, WorldBannerView.ExitMs - WorldBannerView.FadeTailMs + 30f, "69j_banner_l41_fade");

            // 16:9 (1080 × 1920 px; GameScreen lo maqueta en 360 × 640 dp desde DC3).
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            yield return Capture(dir, 41, 1400, "69h_banner_l41_16x9");
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;

            // Reducir movimiento: quieto desde el principio (Capi presentando y Rosa fuera, sin soplo).
            yield return CardSetup.Load(140);
            GameBootstrap boot = GameBootstrap.Current!;
            boot.Flow!.ReduceMotion = true;
            boot.Flow.Load(141);
            PlayModeHelpers.Step(boot, 400); // tras el fundido de entrada: la versión quieta, entera
            Assert.That(boot.View!.WorldBannerView.Visible, Is.True);
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "69i_banner_l141_reduced");
        }

        // El 41 quieto, un toque y la salida a los ms dados: el flujo, aún en LEVEL_CARD con el banner saliendo, y la intro sin
        // empezar (ni la viga ni los miquitos detrás).
        private static IEnumerator ExitAt(string dir, float ms, string name)
        {
            yield return CardSetup.Load(41);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 1100);
            yield return null; // un fotograma de verdad: el raycast no ve un velo que el lienzo aún no ha pintado
            WorldBannerSetup.Tap(view.WorldBannerView);
            PlayModeHelpers.TickExact(boot, Mathf.RoundToInt(ms));
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.LevelCard), name + ": antes de «Llenar y teñir»");
            Assert.That(boot.Flow.WorldBannerClosing, Is.True);
            Assert.That(view.WorldBannerView.Dismissing, Is.True);
            Assert.That(view.CardIntroHeld, Is.True);
            Assert.That(view.Intro.TimeMs, Is.EqualTo(0f));
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, name);
            Debug.Log("AF_A4 " + name + ": alpha " + view.WorldBannerView.Alpha.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                + ", rosa " + view.WorldBannerView.RosaRise.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
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
