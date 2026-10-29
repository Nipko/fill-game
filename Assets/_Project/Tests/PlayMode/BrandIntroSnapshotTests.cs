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
    /// <c>d051_01_inicio</c> (el primer fotograma: desde el añadido de D-051 del 2026-10-28, el icono del sistema en su círculo), y,
    /// con la intro de unos 3,8 s, <c>d051_02_caida</c> (el chorro llegando al agua), <c>d051_03_tinte</c> (los aros y el açaí a
    /// mitad del río), <c>d051_04_tenido</c> (el río teñido hasta el primer plano, el nombre entrando), <c>d051_05_final</c> (el
    /// emblema con «Tintalú», el último fotograma, también el de Reducir movimiento) y
    /// <c>d051_06_cargando</c> (la carga sigue tras la intro: «Cargando…» con sus puntos bajo el nombre). Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter
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

            yield return Take(dir, "d051_01_inicio" + suffix, heightPx, 0f, false);
            yield return Take(dir, "d051_02_caida" + suffix, heightPx, BrandIntro.FallMs, false);
            yield return Take(dir, "d051_03_tinte" + suffix, heightPx, (BrandIntro.FallMs + BrandIntro.MotionMs) * 0.5f, false);
            yield return Take(dir, "d051_04_tenido" + suffix, heightPx, BrandIntro.MotionMs, false);
            yield return Take(dir, "d051_05_final" + suffix, heightPx, BrandIntro.AnimMs, false);
            yield return Take(dir, "d051_06_cargando" + suffix, heightPx, BrandIntro.AnimMs, true);
        }

        /// <summary>
        /// «La intro nace del logo» (añadido de D-051 del 2026-10-28): los fotogramas 0, 25 %, 50 % y 100 % de la apertura del icono
        /// (<see cref="BrandIntro.MorphMs"/>), el açaí recogido justo antes de verter (<see cref="BrandIntro.PourFromMs"/>) y el
        /// fundido de Reducir movimiento a medias, en un teléfono de 2,625 px por dp (411 dp de ancho, como el SM-S938B). Se copian
        /// a <c>docs/art/intro-desde-logo/</c> para el dueño.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureMorph([Values(780, 640)] int screen)
        {
            string dir = SnapshotKit.Prepare();
            GameScreen.SimulatedScreen = screen == 640 ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
            GameScreen.SimulatedPxPerDp = 2.625f;
            int heightPx = screen == 640 ? 1920 : SnapshotKit.Height;
            string suffix = screen == 640 ? "_16x9" : string.Empty;
            ArtSlots.ForceProcedural = true;
            _host = new GameObject("BrandIntroSnapshotHost");
            try
            {
                yield return Take(dir, "d051m_00_logo" + suffix, heightPx, 0f, false);
                yield return Take(dir, "d051m_25" + suffix, heightPx, BrandIntro.MorphMs * 0.25f, false);
                yield return Take(dir, "d051m_50" + suffix, heightPx, BrandIntro.MorphMs * 0.5f, false);
                yield return Take(dir, "d051m_recogido" + suffix, heightPx, BrandIntro.PourFromMs, false);
                yield return Take(dir, "d051m_100" + suffix, heightPx, BrandIntro.MorphMs, false);
                yield return Take(dir, "d051m_vertido" + suffix, heightPx, BrandIntro.FallMs, false);
            }
            finally
            {
                GameScreen.SimulatedPxPerDp = null;
            }
        }

        private IEnumerator Take(string dir, string name, int heightPx, float ms, bool loading)
        {
            _splash?.Dispose();
            _splash = new BootSplash(_host!.transform, withSloth: false, withIntro: true);
            SnapshotKit.AttachOverlay(_splash.Canvas);
            _splash.Tick(0f);
            _splash.Intro!.Seek(ms);
            if (loading)
            {
                _splash.SetLoadingText(AF.Localization.Loc.T("boot_loading")); // la carga sigue tras la intro entera
            }

            _splash.Tick(0f);
            _splash.Tick(300f); // los puntos de espera, a media vuelta
            yield return null;
            Debug.Log("AF_INTRO " + name + ": " + ms + " ms, tinte " + _splash.Intro.TintFill + ", nombre " + _splash.Intro.WordmarkAlpha + ", cargando " + _splash.LoadingVisible);
            SnapshotKit.SaveTop(dir, name, heightPx);
        }
    }
}
