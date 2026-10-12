#nullable enable
using System.Collections;
using System.IO;
using AF.GameFlow;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión de los avisos legibles (D-011, puntos 2 y 3; <c>[Explicit]</c>, no forman parte de la suite):
    /// <c>a2_01_capi_gift_l8</c> (el regalo de Capi al habilitarse el toque, con la tropa aún en la rama),
    /// <c>a2_02_sighting_l5</c>, <c>a2_02b_sighting_l42</c> y <c>a2_02c_sighting_l142</c> (la tarjeta «¡Miquito a la vista!»
    /// cuando la tropa ya se ha ido; en el 42 tras la intro corta y en el 142 con 3 filas) y <c>a2_03_toast_after_card_l5</c>
    /// (el aviso que esperaba a que se fuera la tarjeta). Sirven para mirar a 48 dp la legibilidad, los solapes con la tropa,
    /// las cuias, el HUD y la bandeja, y la línea de D-003. Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode
    /// --filter AF.Tests.PlayMode.NoticeSnapshotTests</c> sin <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class NoticeSnapshotTests
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
        public IEnumerator CaptureNotices()
        {
            string dir = SnapshotKit.Prepare();

            // El regalo de Capi en el 8: sale al habilitarse el toque, abajo, mientras la tropa reposa arriba.
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(8, new MemoryProgressStore(8, 7));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            StepToPlaying(boot);
            PlayModeHelpers.Step(boot, 800);
            Assert.That(boot.View!.ToastText, Is.EqualTo(UiText.ExtraCuiaGift));
            SnapshotKit.Save(dir, "a2_01_capi_gift_l8");

            // La tarjeta del miquito, cuando la tropa se ha ido (larga en el 5, corta en el 42, 3 filas en el 142).
            yield return CaptureSighting(dir, 5, "a2_02_sighting_l5");
            boot = GameBootstrap.Current!;

            // Un aviso que llega con la tarjeta a la vista sale cuando ella se va.
            boot.View!.ShowRevealMissing(70);
            Assert.That(boot.View.ToastText, Is.Null);
            for (int t = 0; t < 8000 && boot.View.Sighting.Visible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            PlayModeHelpers.Step(boot, 400);
            Assert.That(boot.View.ToastText, Is.EqualTo(UiText.RevealMissing(70)));
            SnapshotKit.Save(dir, "a2_03_toast_after_card_l5");

            yield return CaptureSighting(dir, 42, "a2_02b_sighting_l42");
            yield return CaptureSighting(dir, 142, "a2_02c_sighting_l142");
            Assert.That(File.Exists(Path.Combine(dir, "a2_02c_sighting_l142.png")), Is.True);
        }

        // Carga el nivel, deja la intro entera, espera a que la tropa se vaya y captura la tarjeta ya entrada (400 ms).
        private static IEnumerator CaptureSighting(string dir, int level, string name)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.Flow!.Load(level);
            StepToPlaying(boot);
            for (int t = 0; t < 5000 && !boot.View!.Sighting.Visible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.View!.Sighting.Visible, Is.True, name);
            Assert.That(boot.View.Intro.TroopVisible, Is.False, name + ": la tropa ya se fue");
            PlayModeHelpers.Step(boot, 400);
            SnapshotKit.Save(dir, name);
            TestContext.WriteLine(name + ": " + boot.View.Sighting.Name + " · " + boot.View.Sighting.Fact + " · " + boot.View.Sighting.DurationMs + " ms");
        }

        private static void StepToPlaying(GameBootstrap boot)
        {
            for (int t = 0; t < 5000 && boot.Flow!.Phase != FlowPhase.Playing; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
        }
    }
}
