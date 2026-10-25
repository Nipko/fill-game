#nullable enable
using System.Collections;
using System.Globalization;
using AF.Levels;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión del viaje de mundo dentro del banner (D-013, punto 7; Q4-D; tarea M3.6), con el catálogo y los datos
    /// del Mapa reales y la pantalla simulada de <see cref="SnapshotKit"/> (360 × 780 dp): el 41 a mitad del tramo (69t) y en el
    /// Encuentro (69t2), el 81 con la bandera de Colombia a medio izar (69t3) y en Leticia (69t4), el 141 en Iquitos con la del
    /// Perú (69t5) y a 130 % (69t6), el 81 sin arte por código (forzado), sin bandera simplificada (69t7), el 141 con Reducir movimiento a
    /// mitad del fundido de 150 ms (69t8) y, con el catálogo de prueba de <see cref="WorldBannerTravelTests"/> (el 41 llega a una
    /// gran parada del mundo 7 por un tramo que pasa por «teotonio»; P-M8), el relevo del guía a mitad (69t9) y al llegar (69t10).
    /// El arte va en el acto (<see cref="CardSetup.Load"/>): instantes exactos. Necesitan
    /// gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.WorldBannerTravelSnapshotTests</c> sin
    /// <c>-nographics</c>; los PNG van a <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class WorldBannerTravelSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.BannerTravelOverride = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.BannerTravelOverride = null;
            GameScreen.BannerTravelDataOverride = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
            LocSetUpFixture.ConfigureEs419();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureTravels()
        {
            string dir = SnapshotKit.Prepare();
            float mid = WorldBannerView.TravelDelayMs + (0.5f * WorldBannerView.CanoeTravelMs);
            float flagMid = WorldBannerView.TravelDelayMs + WorldBannerView.CanoeTravelMs + 400f;
            yield return Capture(dir, 41, mid, 1f, "69t_travel_l41_mid");
            yield return Capture(dir, 41, 4200f, 1f, "69t2_travel_l41_end");
            yield return Capture(dir, 81, flagMid, 1f, "69t3_travel_l81_flag");
            yield return Capture(dir, 81, 4200f, 1f, "69t4_travel_l81_end");
            yield return Capture(dir, 141, 4200f, 1f, "69t5_travel_l141_end");
            yield return Capture(dir, 141, 4200f, 1.3f, "69t6_travel_l141_130");

            // Sin arte por código (MapArtRule forzado a falso; desde D-052 sale en todos los builds): la canoa llega sin bandera simplificada.
            MapArtRule.ForceProvisional = false;
            yield return Capture(dir, 81, 4200f, 1f, "69t7_travel_l81_sin_arte");
            MapArtRule.ForceProvisional = null;

            // Reducir movimiento: el estado final, a mitad del fundido de 150 ms.
            yield return CardSetup.Load(140);
            GameBootstrap boot = GameBootstrap.Current!;
            boot.Flow!.ReduceMotion = true;
            boot.Flow.Load(141);
            PlayModeHelpers.TickExact(boot, 75);
            Assert.That(boot.View!.WorldBannerView.Travel, Is.Not.Null);
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "69t8_travel_l141_reduced");

            // El relevo del guía en «teotonio» (P-M8), con el catálogo de prueba y su nombre real («Rápidos de Teotônio»): a mitad
            // del relevo (la canoa parada antes de los rápidos, los dos guías a la vista y lejos de ella) y al llegar, con el bufeo
            // delante de la proa.
            yield return CardSetup.Load(40);
            boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.RiverGeometry!(out RiverMap? realMap, out RiverRoutes? realRoutes), Is.True);
            GameScreen.BannerTravelDataOverride = WorldBannerTravelTests.RelayData(realMap!, realRoutes!, boot.RiverStops!, out _);
            boot.Flow!.Load(41);
            WorldBannerView banner = view.WorldBannerView;
            BannerTravel travel = banner.Travel!;
            Assert.That(travel.HasRelay, Is.True);
            BannerTravelStrip strip = banner.TravelStrip;

            // A mitad del relevo: la canoa parada antes de los rápidos, el bufeo ya entero y Rosa empezando a apagarse (al 80 %).
            int midRampMs = Mathf.RoundToInt(strip.RelayStartMs + BannerTravelStrip.RelayInMs + (0.2f * BannerTravelStrip.RelayOutMs));
            PlayModeHelpers.TickExact(boot, midRampMs);
            Assert.That(strip.GuideAAlpha > 0.5f && strip.GuideBAlpha >= 1f, Is.True, "los dos guías a la vista");
            Assert.That(strip.CanoeFraction * strip.TotalDp, Is.EqualTo(strip.RelayStopDp).Within(0.01f), "la canoa, parada en el relevo");
            Assert.That(strip.PassingAlpha(strip.RelayPassingIndex), Is.EqualTo(1f), "«Teotônio» a la vista");
            Assert.That(WorldBannerTravelTests.GuideOverlapDp(strip.GuideA, strip.GuideB), Is.LessThanOrEqualTo(4f), "sin pisarse");
            Assert.That(WorldBannerTravelTests.InStrip(strip, strip.GuideA.rectTransform, 0f), Is.True, "Rosa, que se queda, dentro de la franja");
            Assert.That(WorldBannerTravelTests.InStrip(strip, strip.GuideB.rectTransform, 0f), Is.True, "el bufeo, dentro de la franja");
            Assert.That(WorldBannerTravelTests.BoxGapDp(strip.GuideA.rectTransform, strip.Canoe.rectTransform), Is.GreaterThanOrEqualTo(6f), "Rosa, lejos de la canoa");
            Assert.That(WorldBannerTravelTests.BoxGapDp(strip.GuideB.rectTransform, strip.Canoe.rectTransform), Is.GreaterThanOrEqualTo(6f), "el bufeo, lejos de la canoa");
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "69t9_travel_relay_mid");
            Log(view, "69t9_travel_relay_mid");
            PlayModeHelpers.TickExact(boot, 4200 - Mathf.RoundToInt(midRampMs));
            Assert.That(strip.CanoeFraction, Is.EqualTo(1f));
            Assert.That(strip.GuideAAlpha, Is.EqualTo(0f));
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "69t10_travel_relay_end");
            Log(view, "69t10_travel_relay_end");
        }

        private static void Log(GameScreen view, string name)
        {
            BannerTravelStrip strip = view.WorldBannerView.TravelStrip;
            Debug.Log("AF_M36 " + name + ": canoa " + strip.CanoeFraction.ToString("0.00", CultureInfo.InvariantCulture) + ", guías "
                + strip.GuideAAlpha.ToString("0.00", CultureInfo.InvariantCulture) + " / " + strip.GuideBAlpha.ToString("0.00", CultureInfo.InvariantCulture)
                + ", solape " + WorldBannerTravelTests.GuideOverlapDp(strip.GuideA, strip.GuideB).ToString("0.0", CultureInfo.InvariantCulture) + " dp"
                + ", Rosa en " + strip.GuideA.rectTransform.anchoredPosition + ", bufeo en " + strip.GuideB.rectTransform.anchoredPosition
                + ", canoa en " + strip.Canoe.rectTransform.anchoredPosition
                + (strip.RelayPassingIndex >= 0 ? ", «" + strip.Passing(strip.RelayPassingIndex).text + "» en " + strip.PassingBox(strip.RelayPassingIndex).anchoredPosition
                    + " (su punto en " + strip.StripPoint(view.WorldBannerView.Travel!.PointAt(view.WorldBannerView.Travel.PassingFraction(strip.RelayPassingIndex))) + ")" : string.Empty)
                + "; "
                + view.LastWorldBannerProbe);
        }

        // El banner del nivel level con su viaje, a ms de entrar la tarjeta, con la escala de fuente scale.
        private static IEnumerator Capture(string dir, int level, float ms, float scale, string name)
        {
            // Desde el nivel anterior, con el reloj ya quitado: el banner abre al cargar el nivel, en t = 0, y el instante es exacto
            // (con CardSetup.Load(level), los fotogramas de reloj real de la carga ya lo habían adelantado unos 100 ms, y la 69t3
            // salía con la bandera casi arriba).
            yield return CardSetup.Load(level - 1);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            view.BannerFontScale = scale;
            boot.Flow!.Load(level);
            WorldBannerView banner = view.WorldBannerView;
            Assert.That(banner.Visible && banner.Travel != null, Is.True, "nivel " + level + " con su viaje");
            PlayModeHelpers.TickExact(boot, Mathf.RoundToInt(ms));
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, name);
            BannerTravelStrip strip = banner.TravelStrip;
            var pills = new System.Text.StringBuilder();
            for (int i = 0; i < strip.PassingCount; i++)
            {
                pills.Append(", «").Append(strip.Passing(i).text).Append("» en ").Append(strip.PassingBox(i).anchoredPosition)
                    .Append(strip.PassingCutsRiver(i) ? " (corta un río)" : string.Empty);
            }

            Debug.Log("AF_M36 " + name + pills + ": canoa " + strip.CanoeFraction.ToString("0.00", CultureInfo.InvariantCulture)
                + ", bandera " + strip.FlagRaise.ToString("0.00", CultureInfo.InvariantCulture) + ", " + strip.DpPerUnit.ToString("0", CultureInfo.InvariantCulture)
                + " dp/u, " + banner.Panel.rect.height.ToString("0", CultureInfo.InvariantCulture) + " dp de alto; " + view.LastWorldBannerProbe);
        }
    }
}
