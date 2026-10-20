#nullable enable
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using AF.Economy;
using AF.GameFlow;
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
    /// Capturas de la tarea FX12 (observación 1 de la prueba de la parte A de la ola 7 en el teléfono: en la vista Mundo los ríos
    /// se veían como líneas quebradas), a 1080 × 2340 px (360 × 780 dp), con el Mapa abierto desde el inicio:
    /// <list type="bullet">
    /// <item><description><c>fx12_mundo_max58</c>, <c>fx12_mundo_max120</c> y <c>fx12_mundo_max300</c>: la vista Mundo con
    /// <c>max_level</c> 58, 120 y 300, cada una con su recorte central a 2× (<c>_zoom</c>, 540 × 540 px de la mitad de la
    /// pantalla), donde se ven los meandros;</description></item>
    /// <item><description><c>fx12_cuenca_max58</c>: la Cuenca con 58, que FX12 no cambia (el trazo curvo es solo del
    /// Mundo);</description></item>
    /// <item><description><c>fx12_banner_l41_mid</c>, <c>fx12_banner_l41_end</c> y <c>fx12_banner_l81_end</c>: el viaje del banner
    /// del 41 (a mitad y al llegar) y del 81 (al llegar).</description></item>
    /// </list>
    /// Cada captura tiene su «antes» (<c>_antes</c>, con <see cref="RiverRibbonGraphic.CurveOffForSnapshots"/>: las cuerdas de
    /// antes de FX12), para compararlas. Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.Fx12SnapshotTests</c> sin
    /// <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas de FX12; necesitan dispositivo gráfico.")]
    public sealed class Fx12SnapshotTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 16, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            GameScreen.BannerTravelOverride = null;
            MapArtRule.ForceProvisional = null;
            RiverRibbonGraphic.CurveOffForSnapshots = false;
            LaunchOptions.Reset();
            SnapshotKit.Release();
            UnityEngine.Rendering.OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator CaptureTheWorldView()
        {
            yield return World(string.Empty);
        }

        [UnityTest]
        public IEnumerator CaptureTheWorldViewBefore()
        {
            RiverRibbonGraphic.CurveOffForSnapshots = true;
            yield return World("_antes");
        }

        [UnityTest]
        public IEnumerator CaptureTheBannerTravels()
        {
            yield return Banners(string.Empty);
        }

        [UnityTest]
        public IEnumerator CaptureTheBannerTravelsBefore()
        {
            RiverRibbonGraphic.CurveOffForSnapshots = true;
            yield return Banners("_antes");
        }

        // La vista Mundo con 58, 120 y 300 y la Cuenca con 58; «suffix», al final de cada nombre.
        private static IEnumerator World(string suffix)
        {
            string dir = SnapshotKit.Prepare();
            GameScreen.SimulatedScreen = Fx10Tests.Phone;
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = 59;
            profile.Progress.MaxLevel = 58;
            profile.Progress.MapSeenLevel = 58;
            profile.Wallet.Seeds = 1250;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            yield return null;
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            RiverMapScreen map = boot.RiverMapView!;
            SnapshotKit.AttachOverlay(map.Canvas);
            yield return SnapshotKit.Wait(0.4f);
            Assert.That(map.View, Is.EqualTo(MapView.World));
            SaveWithZoom(dir, "fx12_mundo_max58" + suffix, map);

            map.SetView(MapView.Basin);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "fx12_cuenca_max58" + suffix);
            Log("fx12_cuenca_max58" + suffix, map);

            foreach (int max in new[] { 120, 300 })
            {
                map.Close();
                map.Open(MapOpenSource.Lobby, boot.RiverMapData!, boot.RiverRoutes!, boot.RiverStops!,
                    new MemoryProgressStore(max + 1, max, max), max, false);
                map.SetView(MapView.World);
                yield return SnapshotKit.Wait(0.3f);
                Assert.That(map.View, Is.EqualTo(MapView.World));
                SaveWithZoom(dir, "fx12_mundo_max" + max + suffix, map);
            }
        }

        // El viaje del banner del 41 (a mitad y al llegar) y del 81 (al llegar); «suffix», al final de cada nombre.
        private static IEnumerator Banners(string suffix)
        {
            FunnelSetup.Hooks();
            GameScreen.BannerTravelOverride = true;
            string dir = SnapshotKit.Prepare();
            float mid = WorldBannerView.TravelDelayMs + (0.5f * WorldBannerView.CanoeTravelMs);
            yield return Banner(dir, 41, mid, "fx12_banner_l41_mid" + suffix);
            yield return Banner(dir, 41, 4200f, "fx12_banner_l41_end" + suffix);
            yield return Banner(dir, 81, 4200f, "fx12_banner_l81_end" + suffix);
        }

        // El banner del nivel «level» con su viaje, a «ms» de entrar la tarjeta (como WorldBannerTravelSnapshotTests).
        private static IEnumerator Banner(string dir, int level, float ms, string name)
        {
            yield return CardSetup.Load(level - 1);
            GameBootstrap boot = GameBootstrap.Current!;
            boot.Flow!.Load(level);
            WorldBannerView banner = boot.View!.WorldBannerView;
            Assert.That(banner.Visible && banner.Travel != null, Is.True, "nivel " + level + " con su viaje");
            PlayModeHelpers.TickExact(boot, Mathf.RoundToInt(ms));
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, name);
        }

        // La captura entera y su recorte central a 2× (540 × 540 px de la mitad de la pantalla, escalados a 1080).
        private static void SaveWithZoom(string dir, string name, RiverMapScreen map)
        {
            SnapshotKit.Save(dir, name);
            var full = new Texture2D(2, 2, TextureFormat.RGB24, false);
            Assert.That(full.LoadImage(SnapshotKit.Capture()), Is.True);
            const int Side = 540;
            int x0 = (full.width - Side) / 2;
            int y0 = (full.height - Side) / 2;
            Color[] crop = full.GetPixels(x0, y0, Side, Side);
            var zoom = new Texture2D(Side * 2, Side * 2, TextureFormat.RGB24, false);
            var row = new Color[Side * 2 * Side * 2];
            for (int y = 0; y < Side * 2; y++)
            {
                for (int x = 0; x < Side * 2; x++)
                {
                    row[(y * Side * 2) + x] = crop[((y / 2) * Side) + (x / 2)];
                }
            }

            zoom.SetPixels(row);
            zoom.Apply();
            File.WriteAllBytes(Path.Combine(dir, name + "_zoom.png"), zoom.EncodeToPNG());
            UnityEngine.Object.Destroy(full);
            UnityEngine.Object.Destroy(zoom);
            Log(name, map);
        }

        private static void Log(string name, RiverMapScreen map)
        {
            Debug.Log("AF_FX12 " + name + ": vista " + map.View + ", zoom " + map.Zoom.ToString("0.00", CultureInfo.InvariantCulture)
                + ", max " + map.ProgressLevel + ", frente " + map.FrontStop + " al " + map.FrontFraction.ToString("0.0", CultureInfo.InvariantCulture));
        }
    }
}
