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
    /// Capturas de revisión de la pantalla de arranque con la marca v2 (tarea TI6d; añadido de D-051, 2026-10-25; 06 §2.2; D-016,
    /// T2-A; <c>[Explicit]</c>, no forman parte de la suite), con la pantalla simulada de <see cref="SnapshotKit"/> a 360 × 780 dp
    /// (1080 × 2340 px) y en 16:9 (360 × 640 dp, 1080 × 1920 px, sufijo <c>_16x9</c>):
    /// <list type="bullet">
    /// <item><c>ti6d_01_wake_0ms</c>, <c>ti6d_02_wake_1000ms</c> y <c>ti6d_03_wake_2000ms</c>: la primera apertura, con TiTino
    /// dormido (ojos cerrados y la cabeza caída), estirando el brazo y ya mirándote (<c>sloth_wake</c>);</item>
    /// <item><c>ti6d_04_idle</c>: las demás aperturas (<c>sloth_idle</c>);</item>
    /// <item><c>ti6d_05_sin_titino</c>: sin arte de TiTino (MapArtRule forzado; desde D-052 sale en todos los builds), lo mismo
    /// que con <c>ui_sloth_enabled</c> apagado: el mundo y el nombre, sin TiTino ni su rama;</item>
    /// <item>el empalme con la intro de la marca (D-051): <c>ti6d_06a_empalme_intro</c> (el último fotograma de la intro, justo
    /// antes de quitarse), <c>ti6d_06b_empalme_150ms</c> (a mitad del fundido del mundo) y <c>ti6d_06c_empalme_300ms</c> (ya
    /// entero, con <c>sloth_wake</c> empezando).</item>
    /// </list>
    /// Sin textos: el estado va en el nombre del archivo. Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.BootSplashSnapshotTests</c> sin <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class BootSplashSnapshotTests
    {
        private GameObject? _host;
        private ProceduralArt? _art;
        private BootSplash? _splash;

        [TearDown]
        public void TearDown()
        {
            _splash?.Dispose();
            _splash = null;
            _art?.Dispose();
            _art = null;
            if (_host != null)
            {
                Object.Destroy(_host);
            }

            _host = null;
            MapArtRule.ForceProvisional = null;
            ArtSlots.ForceProcedural = false;
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureSplash([Values(780, 640)] int screen)
        {
            string dir = SnapshotKit.Prepare();
            GameScreen.SimulatedScreen = screen == 640 ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
            int heightPx = screen == 640 ? 1920 : SnapshotKit.Height;
            string suffix = screen == 640 ? "_16x9" : string.Empty;
            ArtSlots.ForceProcedural = true;
            MapArtRule.ForceProvisional = true;
            _host = new GameObject("BootSplashSnapshotHost");
            _art = new ProceduralArt("white");

            float[] wakeMs = { 0f, 1000f, 2000f };
            string[] names = { "ti6d_01_wake_0ms", "ti6d_02_wake_1000ms", "ti6d_03_wake_2000ms" };
            for (int i = 0; i < wakeMs.Length; i++)
            {
                yield return Take(dir, names[i] + suffix, heightPx, true, wakeMs[i]);
            }

            yield return Take(dir, "ti6d_04_idle" + suffix, heightPx, false, 0f);

            MapArtRule.ForceProvisional = false; // forzado: hoy el arte por código sale en todos los builds, release incluido (D-052)
            yield return Take(dir, "ti6d_05_sin_titino" + suffix, heightPx, true, 0f);

            // El empalme: la intro en su último fotograma y, al quitarse, el mundo que entra.
            MapArtRule.ForceProvisional = true;
            float[] revealMs = { -1f, 150f, BootSplash.RevealMs };
            string[] steps = { "ti6d_06a_empalme_intro", "ti6d_06b_empalme_150ms", "ti6d_06c_empalme_300ms" };
            for (int i = 0; i < revealMs.Length; i++)
            {
                _splash?.Dispose();
                double t0 = Time.realtimeSinceStartupAsDouble;
                _splash = new BootSplash(_host!.transform, withSloth: true, withIntro: true);
                Debug.Log("AF_SPLASH crear con la intro (emblema incluido): " + ((Time.realtimeSinceStartupAsDouble - t0) * 1000.0).ToString("F0") + " ms");
                _splash.BeginIntro(false);
                _splash.ShowSloth(_art!.SlothRigArt(), _art.HomeBranch, true, false);
                SnapshotKit.AttachOverlay(_splash.Canvas);
                _splash.Intro!.Seek(BrandIntro.AnimMs);
                _splash.Tick(0f);
                if (revealMs[i] >= 0f)
                {
                    _splash.EndIntro();
                    _splash.SeekReveal(revealMs[i]);
                    _splash.Seek(revealMs[i]);
                }

                yield return null;
                Debug.Log("AF_SPLASH " + steps[i] + suffix + ": intro = " + _splash.IntroVisible + ", mundo = " + _splash.Reveal);
                SnapshotKit.SaveTop(dir, steps[i] + suffix, heightPx);
            }
        }

        private IEnumerator Take(string dir, string name, int heightPx, bool firstOpen, float ms)
        {
            _splash?.Dispose();
            _splash = new BootSplash(_host!.transform);
            _splash.ShowSloth(_art!.SlothRigArt(), _art.HomeBranch, firstOpen, false);
            SnapshotKit.AttachOverlay(_splash.Canvas);
            _splash.Tick(0f);
            _splash.Seek(ms);
            yield return null;
            Debug.Log("AF_SPLASH " + name + ": sloth = " + (_splash.Sloth != null ? SlothRig.IdOf(_splash.Sloth.Pose) : "none") + " a " + ms + " ms");
            SnapshotKit.SaveTop(dir, name, heightPx);
        }
    }
}
