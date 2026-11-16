#nullable enable
using System;
using System.Collections;
using AF.Audio;
using AF.Economy;
using AF.Localization;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de antes y después del tope de densidad del arte por código (<c>docs/dev/memoria.md</c> §7.5, pregunta 1, opción
    /// (a); D-060) a 360 dp de ancho: un teléfono de clase A (720 × 1600 px a 2 px/dp, como el Moto G23) y uno de 3 px/dp (1080 ×
    /// 2400). En cada uno, el inicio y el nivel 58 con el arte a la densidad fija de antes (<c>fija</c>) y con el tope del teléfono
    /// (<c>tope</c>), en <c>Logs/snapshots/es-419/density_{2|3}x_{inicio|nivel}_{fija|tope}.png</c>. A 3 px/dp las dos deben ser
    /// iguales. Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.DensityCapSnapshotTests</c>
    /// sin <c>-nographics</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class DensityCapSnapshotTests
    {
        private static readonly DateTime Day0 = new DateTime(2026, 10, 15, 15, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            AudioCues.Override = new FakeAudioService();
            HomeSloth.ManualTickForTests = true;
        }

        [TearDown]
        public void TearDown()
        {
            HomeSloth.ManualTickForTests = false;
            GameBootstrap.ArtDensityCapEnabled = true;
            LaunchOptions.Reset();
            GameScreen.SimulatedScreen = null;
            GameScreen.SimulatedPxPerDp = null;
            SnapshotKit.Release();
            AudioCues.Override = null;
            AudioCues.Install(null);
        }

        [UnityTest]
        public IEnumerator Capture([Values(2, 3)] int density)
        {
            int w = 360 * density;
            int h = density == 2 ? 1600 : 2400;
            string dir = SnapshotKit.PrepareSized(LocLocale.SourceLocale, w, h);
            GameScreen.SimulatedPxPerDp = density;
            foreach (bool cap in new[] { false, true })
            {
                GameBootstrap.ArtDensityCapEnabled = cap;
                string tag = density + "x_";
                string mode = cap ? "_tope" : "_fija";
                var clock = new ManualClock(Day0);
                LaunchOptions.ClockOverride = clock;
                Profile profile = Wallet.NewProfile(clock);
                profile.Progress.NextLevel = 58;
                profile.Progress.MaxLevel = 57;
                profile.Progress.MapSeenLevel = 57;
                profile.Wallet.Seeds = 1250;
                yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
                GameBootstrap boot = GameBootstrap.Current!;
                Assert.That(ProceduralArt.DensityCap, Is.EqualTo(cap && density == 2 ? 2f : (cap ? 3f : 0f)).Within(0.001f));
                LobbyScreen lobby = boot.Lobby!;
                lobby.Tick(16f);
                lobby.River.Seek(2600f);
                SnapshotKit.AttachLobby(boot);
                yield return SnapshotKit.Wait(0.3f);
                SnapshotKit.Save(dir, "density_" + tag + "inicio" + mode);

                Assert.That(boot.PressPlay(), Is.True);
                yield return SnapshotKit.Wait(2.5f);
                SnapshotKit.Attach(boot);
                yield return SnapshotKit.Wait(0.3f);
                SnapshotKit.Save(dir, "density_" + tag + "nivel" + mode);
            }
        }
    }
}
