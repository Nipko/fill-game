#nullable enable
using System.Collections;
using AF.Localization;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas del lienzo de 320 dp (Q-B11-5 (a); cierre de B11) en <c>Logs/snapshots/es-419/</c>: la captura de siempre (1080 ×
    /// 2340 px) con 3,375 px/dp, un teléfono de 320 × 693 dp, así que el lienzo mide 320 dp con 1 dp físico por dp:
    /// <c>c320_01_level374</c> (13 cuias en 3 filas), <c>c320_02_victory26</c> (la victoria con la Crecida, la Canasta y la fila de
    /// la Bitácora) y <c>c320_03_lobby</c>. Explícito: necesita gráficos.
    /// </summary>
    [Explicit("Capturas del lienzo de 320 dp; necesitan dispositivo gráfico.")]
    public sealed class Canvas320SnapshotTests
    {
        private const float Density = 1080f / 320f;

        [TearDown]
        public void TearDown()
        {
            SnapshotKit.Release();
            GameBootstrap.LogbookOverride = null;
            GameScreen.SimulatedScreen = null;
            GameScreen.SimulatedPxPerDp = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Level374()
        {
            string dir = Prepare();
            yield return Canvas16x9LayoutTests.LoadPlaying(374);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.View!.CanvasWidth, Is.EqualTo(320f).Within(0.01f));
            SnapshotKit.Attach(boot);
            yield return Save(dir, "c320_01_level374");
        }

        [UnityTest]
        public IEnumerator Victory26()
        {
            string dir = Prepare();
            GameBootstrap.LogbookOverride = true;
            yield return FactLineTests.Win(26);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.View!.CanvasWidth, Is.EqualTo(320f).Within(0.01f));
            SnapshotKit.Attach(boot);
            PlayModeHelpers.Step(boot, 1700);
            yield return Save(dir, "c320_02_victory26");
        }

        [UnityTest]
        public IEnumerator Lobby()
        {
            string dir = Prepare();
            Profile profile = FunnelSetup.Profile(500);
            profile.Progress.MaxLevel = 30;
            profile.Progress.NextLevel = 31;
            profile.Progress.MapSeenLevel = 30;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Lobby!.CanvasWidth, Is.EqualTo(320f).Within(0.01f));
            SnapshotKit.AttachLobby(boot);
            PlayModeHelpers.Step(boot, 600);
            yield return Save(dir, "c320_03_lobby");
        }

        private static string Prepare()
        {
            FunnelSetup.Hooks();
            string dir = SnapshotKit.Prepare(LocLocale.SpanishLatinAmerica);
            GameScreen.SimulatedPxPerDp = Density;
            LaunchOptions.FontScaleOverride = 1f;
            return dir;
        }

        private static IEnumerator Save(string dir, string name)
        {
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, name);
            Debug.Log("AF_C320_SNAPSHOT " + name);
        }
    }
}
