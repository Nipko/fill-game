#nullable enable
using System.Collections;
using System.Globalization;
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
    /// Perú (69t5) y a 130 % (69t6), el 81 como en release, sin bandera simplificada (69t7), y el 141 con Reducir movimiento a
    /// mitad del fundido de 150 ms (69t8). El arte va en el acto (<see cref="CardSetup.Load"/>): instantes exactos. Necesitan
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
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
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

            // Como en release (MapArtRule falso): la canoa llega sin bandera simplificada.
            MapArtRule.ForceProvisional = false;
            yield return Capture(dir, 81, 4200f, 1f, "69t7_travel_l81_release");
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
        }

        // El banner del nivel level con su viaje, a ms de entrar la tarjeta, con la escala de fuente scale.
        private static IEnumerator Capture(string dir, int level, float ms, float scale, string name)
        {
            yield return CardSetup.Load(level);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            WorldBannerView banner = view.WorldBannerView;
            Assert.That(banner.Visible && banner.Travel != null, Is.True, "nivel " + level + " con su viaje");
            view.BannerFontScale = scale;
            PlayModeHelpers.TickExact(boot, Mathf.Max(0, Mathf.RoundToInt(ms) - 304)); // CardSetup.Load ya avanzó 304 ms
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, name);
            BannerTravelStrip strip = banner.TravelStrip;
            Debug.Log("AF_M36 " + name + ": canoa " + strip.CanoeFraction.ToString("0.00", CultureInfo.InvariantCulture)
                + ", bandera " + strip.FlagRaise.ToString("0.00", CultureInfo.InvariantCulture) + ", " + strip.DpPerUnit.ToString("0", CultureInfo.InvariantCulture)
                + " dp/u, " + banner.Panel.rect.height.ToString("0", CultureInfo.InvariantCulture) + " dp de alto; " + view.LastWorldBannerProbe);
        }
    }
}
