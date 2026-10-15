#nullable enable
using System.Collections;
using System.Globalization;
using AF.Localization;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de la llegada a la parada en la ranura del Dato del río (02 §6, fila Llegada; tarea M1.6): la victoria del 20
    /// (Alter do Chão, agua clara) y la del 80 (Leticia, con la bienvenida de Colombia), a 360 × 780 y 360 × 640 dp (esta, la
    /// pantalla de 1080 × 1920 px de <see cref="HudLabelTests.Screen640"/>), al 100 % y al 130 %, con la franja terminada; y la
    /// del 20 a mitad de la franja (la canoa llegando). Escribe <c>arrival_l{20|80}_{780|640}_{100|130}.png</c> y
    /// <c>arrival_l20_780_100_mid.png</c> en <c>Logs/snapshots/es-419/</c>, y una línea <c>AF_ARRIVAL</c> por captura con el
    /// alto de la línea de fruta, el de la llegada más alta del catálogo, el de la ranura y el de esta llegada.
    /// Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.ArrivalSnapshotTests</c>
    /// sin <c>-nographics</c>). Falla si algo de la llegada se recorta (<see cref="VictoryArrivalTests.AssertFits"/>).
    /// </summary>
    [Explicit("Capturas de la llegada (M1.6); necesitan dispositivo gráfico.")]
    public sealed class ArrivalSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.ArrivalDisabledForTests = false;
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            SnapshotKit.Release();
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator CaptureArrivals()
        {
            foreach (bool small in new[] { false, true })
            {
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    foreach (int level in new[] { 20, 80 })
                    {
                        string screen = small ? "640" : "780";
                        string pct = scale > 1.01f ? "130" : "100";
                        bool mid = level == 20 && !small && scale < 1.01f;
                        FunnelSetup.Hooks();
                        MapArtRule.ForceProvisional = true;
                        string dir = SnapshotKit.Prepare(LocLocale.SpanishLatinAmerica);
                        if (small)
                        {
                            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
                        }

                        LaunchOptions.FontScaleOverride = scale;
                        yield return VictoryArrivalTests.Win(level);
                        GameBootstrap boot = GameBootstrap.Current!;
                        GameScreen view = boot.View!;
                        SnapshotKit.Attach(boot);
                        if (mid)
                        {
                            PlayModeHelpers.TickExact(boot, (int)GameScreen.VictoryPanelUpMs + 450);
                            yield return SnapshotKit.Wait(0.2f);
                            SnapshotKit.Save(dir, "arrival_l20_780_100_mid");
                        }

                        PlayModeHelpers.Step(boot, 1600);
                        string name = "arrival_l" + level + "_" + screen + "_" + pct;
                        VictoryArrivalTests.AssertFits(view, name);
                        Assert.That(view.Arrival!.Completed, Is.True, name);
                        float fruit = view.VictorySlotContentHeight(Loc.Locale, scale);
                        float need = view.Arrival.NeedDp(scale, view.VictorySlotWidthDp);
                        Debug.Log("AF_ARRIVAL " + name + ": fruta " + F(fruit) + " dp, llegada más alta " + F(need) + " dp, ranura "
                            + F(view.VictorySlot.ContentDp) + " dp, esta llegada " + F(view.Arrival.ShownHeightDp) + " dp, Siguiente "
                            + F(view.NextTopDp) + ", cinta " + F(view.VictoryPanelTopDp - 38f) + ", cabecera " + F(view.SlothBandTopDp));
                        yield return SnapshotKit.Wait(0.2f);
                        SnapshotKit.Save(dir, name);
                        SnapshotKit.Release();
                        LaunchOptions.Reset();
                    }
                }
            }
        }

        private static string F(float v)
        {
            return v.ToString("0.0", CultureInfo.InvariantCulture);
        }
    }
}
