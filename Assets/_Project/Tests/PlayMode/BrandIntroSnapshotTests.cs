#nullable enable
using System.Collections;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión de la intro de la marca (D-051, punto 3; <c>[Explicit]</c>, no forman parte de la suite), con la
    /// pantalla simulada de <see cref="SnapshotKit"/> a 360 × 780 dp (1080 × 2340 px) y en 16:9 (sufijo <c>_16x9</c>):
    /// <c>d051_01_inicio</c> (el primer fotograma: la gota sobre el río), <c>d051_02_mitad</c> (a mitad: el açaí avanza río
    /// abajo desde el impacto, con las ondas) y <c>d051_03_final</c> (el último fotograma, el estable, que es también el de
    /// Reducir movimiento). Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.BrandIntroSnapshotTests</c> sin <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class BrandIntroSnapshotTests
    {
        private GameObject? _host;
        private BootSplash? _splash;

        [TearDown]
        public void TearDown()
        {
            _splash?.Dispose();
            _splash = null;
            if (_host != null)
            {
                Object.Destroy(_host);
            }

            _host = null;
            ArtSlots.ForceProcedural = false;
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureIntro([Values(780, 640)] int screen)
        {
            string dir = SnapshotKit.Prepare();
            GameScreen.SimulatedScreen = screen == 640 ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
            int heightPx = screen == 640 ? 1920 : SnapshotKit.Height;
            string suffix = screen == 640 ? "_16x9" : string.Empty;
            ArtSlots.ForceProcedural = true;
            _host = new GameObject("BrandIntroSnapshotHost");

            yield return Take(dir, "d051_01_inicio" + suffix, heightPx, 0f);
            yield return Take(dir, "d051_02_mitad" + suffix, heightPx, 400f);
            yield return Take(dir, "d051_03_final" + suffix, heightPx, BrandIntro.AnimMs);
        }

        private IEnumerator Take(string dir, string name, int heightPx, float ms)
        {
            _splash?.Dispose();
            _splash = new BootSplash(_host!.transform, withSloth: false, withIntro: true);
            SnapshotKit.AttachOverlay(_splash.Canvas);
            _splash.Tick(0f);
            _splash.Intro!.Seek(ms);
            yield return null;
            Debug.Log("AF_INTRO " + name + ": " + ms + " ms, tinte " + _splash.Intro.TintFill);
            SnapshotKit.SaveTop(dir, name, heightPx);
        }
    }
}
