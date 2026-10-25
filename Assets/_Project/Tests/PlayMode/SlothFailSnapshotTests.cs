#nullable enable
using System.Collections;
using AF.GameFlow;
using AF.Localization;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de TiTino en Nivel fallido y del aviso tras Volver a la orilla (tarea TI6b; propuesta de TiTino d.5 y paso 6b de
    /// (i)), en <c>Logs/snapshots/es-419/</c>:
    /// <list type="bullet">
    /// <item><c>ti6b_fallo_{780|640}_{100|130}</c>: el primer fallo del 31 (Aguas estancadas → ¿Seguro? → Nivel fallido) a 1,0
    /// s de abrirse la tarjeta, con TiTino y su frase bajo la cesta (o solo el gesto si la frase no cabe);</item>
    /// <item><c>ti6b_fallo_gesto_780_100</c>: el segundo fallo del 31, solo el gesto;</item>
    /// <item><c>ti6b_aviso_{780|640}_{100|130}</c>: el aviso tras el primer reinicio del 9, con la tarjeta a la vista (a 640, 16:9
    /// desde DC3, el 9 no deja sitio y el aviso no sale: sin captura, como mide <c>SlothFailTests.RestartNotice_NoIntersection_…</c>);</item>
    /// <item><c>ti6b_aviso_modal_780_100</c>: el aviso tras Volver a la orilla desde el pie de Aguas estancadas (el 10 con el
    /// tablero 6 del tutorial).</item>
    /// </list>
    /// Cada captura comprueba los solapes (<see cref="SlothFailTests.AssertFailedClear"/> y
    /// <see cref="SlothFailTests.AssertNoticeClear"/>) y escribe una línea <c>AF_SLOTH_FAIL</c> o <c>AF_SLOTH_RESTART</c>.
    /// Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.SlothFailSnapshotTests</c>
    /// sin <c>-nographics</c>).
    /// </summary>
    [Explicit("Capturas de TiTino en Nivel fallido y del aviso tras Volver a la orilla (TI6b); necesitan dispositivo gráfico.")]
    public sealed class SlothFailSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            SnapshotKit.Release();
            GameScreen.SlothRestartNoticeOverride = null;
            GameScreen.SimulatedScreen = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator CaptureLevelFailed()
        {
            foreach (bool small in new[] { false, true })
            {
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    string name = "ti6b_fallo_" + (small ? "640" : "780") + "_" + (scale > 1.01f ? "130" : "100");
                    string dir = Prepare(small, scale);
                    yield return SlothFailTests.LoadLevel31(5);
                    GameBootstrap boot = GameBootstrap.Current!;
                    SlothFailTests.FailToLevelFailed(boot, true);
                    yield return CaptureFailed(boot, dir, name);
                    if (!small && scale < 1.01f)
                    {
                        boot.PressRetry();
                        PlayModeHelpers.Step(boot, 1200);
                        SlothFailTests.FailToLevelFailed(boot, false);
                        Assert.That(boot.View!.Failed.PhraseShown, Is.False, "segundo fallo: solo el gesto");
                        yield return CaptureFailed(boot, dir, "ti6b_fallo_gesto_780_100");
                    }

                    Release();
                }
            }
        }

        [UnityTest]
        public IEnumerator CaptureRestartNotice()
        {
            foreach (bool small in new[] { false, true })
            {
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    string name = "ti6b_aviso_" + (small ? "640" : "780") + "_" + (scale > 1.01f ? "130" : "100");
                    string dir = Prepare(small, scale);
                    yield return SlothFailTests.LoadLevel(9, 0);
                    GameBootstrap boot = GameBootstrap.Current!;
                    Assert.That(boot.PressRestart(), Is.True);
                    if (small && !SlothFailTests.NoticeShows(boot))
                    {
                        // 16:9 desde DC3 (D-028): el 9, de 2 filas, no deja sitio ni en la franja ni sobre el muelle y el aviso no sale
                        // (si no cabe, no sale), como mide SlothFailTests.RestartNotice_NoIntersection_…: sin captura.
                        Assert.That(boot.View!.SlothNoticeVisible, Is.False, name);
                        Debug.Log("AF_SLOTH_RESTART " + name + ": no cabe y no sale (sin captura)");
                        Release();
                        continue;
                    }

                    yield return CaptureNotice(boot, dir, name);
                    Release();
                }
            }

            string modalDir = Prepare(false, 1f);
            yield return SlothFailTests.LoadLevel(10, 4);
            GameBootstrap modal = GameBootstrap.Current!;
            FunnelSetup.GetStuck(modal);
            Assert.That(modal.PressRestart(), Is.True);
            yield return CaptureNotice(modal, modalDir, "ti6b_aviso_modal_780_100");
            Release();
        }

        private static IEnumerator CaptureFailed(GameBootstrap boot, string dir, string name)
        {
            GameScreen view = boot.View!;
            SnapshotKit.Attach(boot);
            for (int t = 0; t < 1000; t += PlayModeHelpers.FrameMs)
            {
                SlothFailTests.AssertFailedClear(view, name + " a los " + t + " ms");
                PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
            }

            SlothFailTests.AssertFailedClear(view, name);
            LevelFailedScreen failed = view.Failed;
            Assert.That(failed.SlothVisible, Is.True, name + ": TiTino, a la vista");
            Debug.Log("AF_SLOTH_FAIL " + name + ": TiTino " + SlothFailTests.F(failed.SlothSize) + " dp, frase "
                + (failed.PhraseShown ? "«" + failed.PhraseText + "» en " + failed.PhraseLines + " líneas a " + SlothFailTests.F(failed.PhraseFontDp) + " dp" : "no")
                + ", tarjeta de " + SlothFailTests.F(failed.Panel.sizeDelta.y) + " dp desde " + SlothFailTests.F(-failed.Panel.anchoredPosition.y)
                + ", línea " + (view.LastSlothFailLine.Key ?? "ninguna"));
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, name);
        }

        private static IEnumerator CaptureNotice(GameBootstrap boot, string dir, string name)
        {
            GameScreen view = boot.View!;
            for (int t = 0; t < 6000 && !view.SlothNoticeVisible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.SlothNoticeVisible, Is.True, name + ": el aviso de TiTino");
            SnapshotKit.Attach(boot);
            PlayModeHelpers.Step(boot, 600);
            SlothFailTests.AssertNoticeClear(view, name);
            SlothNoticeCard card = view.SlothNoticeView!;
            Debug.Log("AF_SLOTH_RESTART " + name + ": «" + card.Text + "» en " + card.Lines + " líneas a " + SlothFailTests.F(card.Label.fontSize)
                + " dp, " + (view.SlothNoticeOnDock ? "sobre el muelle" : "sobre el panorama") + ", " + SlothFailTests.F(card.Height) + " dp, "
                + SlothFailTests.F(card.DurationMs) + " ms");
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, name);
        }

        private static void Hooks()
        {
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
            GameScreen.SlothRestartNoticeOverride = true;
        }

        private static string Prepare(bool small, float scale)
        {
            Hooks();
            string dir = SnapshotKit.Prepare(LocLocale.SpanishLatinAmerica);
            GameScreen.SimulatedScreen = small ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
            LaunchOptions.FontScaleOverride = scale;
            return dir;
        }

        private static void Release()
        {
            SnapshotKit.Release();
            LaunchOptions.Reset();
        }
    }
}
