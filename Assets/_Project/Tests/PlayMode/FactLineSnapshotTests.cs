#nullable enable
using System.Collections;
using System.Globalization;
using System.IO;
using AF.GameFlow;
using AF.Localization;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de la línea de fruta de Frutas del río en la victoria (02 §6, fila «Dato del río»; D-017; tarea F3), con la
    /// línea entera (tras el fundido):
    /// <list type="bullet">
    /// <item>en es-419, a 360 × 780 dp y al 100 %: la victoria del 3 («Açaí · 3/20») y la del 100 sin el catálogo de paradas
    /// (con él, el 100 lleva la llegada y la línea queda en la ficha), que enseña el sello de la última línea de la serie del
    /// copoazú;</item>
    /// <item>en en-XA, a 360 × 640 dp y al 130 % (el peor caso): las mismas dos y la del 39 (Cosecha doble y el chip del 40);</item>
    /// <item>en los dos, la del 20 con la política SingleKey: la placa del lugar sin dato arriba y la línea de fruta debajo
    /// (arquitectura de la tanda 4, §2).</item>
    /// </list>
    /// Escribe <c>fact_l{3|100|20_singlekey}_780_100.png</c> en <c>Logs/snapshots/es-419/</c> y
    /// <c>fact_l{3|39|100|20_singlekey}_640_130.png</c> en <c>Logs/snapshots/en-XA/</c>, con una línea <c>AF_FACT</c> por captura
    /// (alto de la línea, H, ranura, tamaño del texto, Siguiente, cinta y cabecera del HUD). Las de 360 × 640 se recortan a los
    /// 1080 × 1920 px de la pantalla simulada (<see cref="HudLabelTests.Screen640"/>), que GameScreen maqueta arriba de la captura
    /// de 1080 × 2340: el borde de abajo de la imagen es el del marco, y se ve si el panel cabe. Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.FactLineSnapshotTests</c> sin <c>-nographics</c>). Falla si algo de la línea se recorta
    /// (<see cref="FactLineTests.AssertFits"/>) o si la cinta no cabe bajo la cabecera del HUD.
    /// </summary>
    [Explicit("Capturas de la línea de fruta (F3); necesitan dispositivo gráfico.")]
    public sealed class FactLineSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.ArrivalDisabledForTests = false;
            GameScreen.FactLineDisabledForTests = false;
        }

        [TearDown]
        public void TearDown()
        {
            SnapshotKit.Release();
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator CaptureFruitLines()
        {
            foreach (string locale in new[] { LocLocale.SpanishLatinAmerica, LocLocale.Pseudo })
            {
                bool worst = locale == LocLocale.Pseudo;
                int[] levels = worst ? new[] { 3, 39, 100, -20 } : new[] { 3, 100, -20 };
                foreach (int code in levels)
                {
                    int level = code < 0 ? -code : code;
                    bool singleKey = code < 0; // el 20 con SingleKey: la placa del lugar y la línea de fruta juntas
                    FunnelSetup.Hooks();
                    string dir = SnapshotKit.Prepare(locale);
                    if (worst)
                    {
                        GameScreen.SimulatedScreen = HudLabelTests.Screen640;
                    }

                    float scale = worst ? 1.3f : 1f;
                    LaunchOptions.FontScaleOverride = scale;
                    LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
                    yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
                    GameBootstrap boot = GameBootstrap.Current!;
                    PlayModeHelpers.TakeClock(boot);
                    GameScreen view = boot.View!;
                    if (singleKey)
                    {
                        view.RiverPolicyOverride = RiverFactPolicy.SingleKey;
                    }
                    else if (level % 10 == 0)
                    {
                        view.RiverStops = null; // sin la llegada del 10k: la línea 20 de la serie, con su sello
                    }

                    SnapshotKit.Attach(boot);
                    VictorySetup.WinToVictoryScreen(boot);
                    PlayModeHelpers.Step(boot, 1600);
                    string name = "fact_l" + level + (singleKey ? "_singlekey" : string.Empty) + "_" + (worst ? "640_130" : "780_100");
                    Assert.That(Loc.Locale, Is.EqualTo(locale), name);
                    FactLineTests.AssertFits(view, name);
                    Assert.That(view.FruitLine!.Alpha, Is.EqualTo(1f), name);
                    Assert.That(view.FruitLine.Sealed, Is.EqualTo(level == 100 || singleKey), name);
                    Assert.That(view.Arrival!.Stacked, Is.EqualTo(singleKey), name);
                    Assert.That(view.VictoryPanelTopDp - 38f, Is.GreaterThanOrEqualTo(view.SlothBandTopDp), name + ": la cinta, bajo la cabecera");
                    FactLine line = view.FruitLine;
                    Debug.Log("AF_FACT " + name + ": " + line.ShownFactId + ", línea " + F(line.ShownHeightDp) + " dp (H " + F(line.BlockHeightDp)
                        + ", cabecera " + F(line.HeaderHeightDp) + (line.HeaderWrapped ? " en dos filas" : string.Empty) + ", texto "
                        + F(line.TextSizeDp) + " dp), ranura " + F(view.VictorySlot.ContentDp) + " dp, Siguiente " + F(view.NextTopDp)
                        + ", cinta " + F(view.VictoryPanelTopDp - 38f) + ", cabecera del HUD " + F(view.SlothBandTopDp));
                    yield return SnapshotKit.Wait(0.2f);
                    if (worst)
                    {
                        SaveTop(dir, name, (int)HudLabelTests.Screen640.y);
                    }
                    else
                    {
                        SnapshotKit.Save(dir, name);
                    }

                    SnapshotKit.Release();
                    LaunchOptions.Reset();
                }
            }
        }

        // Guarda solo las heightPx filas de arriba de la captura (la pantalla simulada de 360 × 640 dp; revisión de F3).
        private static void SaveTop(string dir, string name, int heightPx)
        {
            var full = new Texture2D(2, 2, TextureFormat.RGB24, false);
            Assert.That(full.LoadImage(SnapshotKit.Capture()), Is.True, name);
            int h = Mathf.Min(heightPx, full.height);
            var top = new Texture2D(full.width, h, TextureFormat.RGB24, false);
            top.SetPixels(full.GetPixels(0, full.height - h, full.width, h));
            top.Apply();
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), top.EncodeToPNG());
            Object.Destroy(full);
            Object.Destroy(top);
        }

        private static string F(float v)
        {
            return v.ToString("0.0", CultureInfo.InvariantCulture);
        }
    }
}
