#nullable enable
using System;
using System.Collections;
using System.Globalization;
using AF.Economy;
using AF.GameFlow;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas del Mapa del río v2, bloque M2-1 (D-069; propuesta del Mapa v2 §9.3: «capturas de antes y después en 3 tramos
    /// (Estuario, Encuentro y Madre de Dios) a 320, 360 y 412 dp»): cada tramo con <c>ui_map_v2</c> apagada (<c>antes</c>, el Mapa de
    /// la 1.0 con los datos <c>/2</c>) y encendida (<c>despues</c>), en la captura de siempre (1080 × 2340 px) con 3,375, 3 y 2,62
    /// px/dp. Estuario: «Jugar · Nivel 11» con la vista llevada a la casa de TiTino, en Belém; Encuentro: el 46, camino del Encuentro de
    /// las Aguas; Madre de Dios: el 346. Los PNG quedan en <c>Logs/snapshots/es-419/m21_&lt;tramo&gt;_&lt;ancho&gt;dp_&lt;antes|despues&gt;.png</c>
    /// y cada uno escribe su línea <c>AF_MAP</c>. Explícito: necesita gráficos (sin <c>-nographics</c>).
    /// </summary>
    [Explicit("Capturas del Mapa v2 (M2-1); necesitan dispositivo gráfico.")]
    public sealed class MapLiveSnapshotTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 11, 7, 15, 0, 0, DateTimeKind.Utc);

        private static readonly (string Name, int Play, bool Home)[] Stretches =
        {
            ("estuario", 11, true),
            ("encuentro", 46, false),
            ("madre_de_dios", 346, false),
        };

        private static readonly (int Dp, float PxPerDp)[] Widths =
        {
            (320, 1080f / 320f),
            (360, 3f),
            (412, 1080f / 412f),
        };

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.MapLiveConfigOverride = null;
            GameScreen.SimulatedPxPerDp = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
            UnityEngine.Rendering.OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator CaptureBeforeAndAfter_ThreeStretches_ThreeWidths()
        {
            string dir = SnapshotKit.Prepare();
            foreach ((string name, int play, bool home) in Stretches)
            {
                foreach ((int dp, float pxPerDp) in Widths)
                {
                    foreach (bool live in new[] { false, true })
                    {
                        GameScreen.SimulatedPxPerDp = pxPerDp;
                        GameBootstrap.MapLiveConfigOverride = new MapLiveConfig { UiMapV2 = live };
                        Profile profile = Wallet.NewProfile(new ManualClock(T0));
                        profile.Progress.NextLevel = play;
                        profile.Progress.MaxLevel = play - 1;
                        profile.Progress.MapSeenLevel = play - 1;
                        profile.Wallet.Seeds = 1250;
                        yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
                        GameBootstrap boot = GameBootstrap.Current!;
                        SnapshotKit.AttachLobby(boot);
                        yield return null;
                        Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True, name);
                        RiverMapScreen map = boot.RiverMapView!;
                        Assert.That(map.LiveActive, Is.EqualTo(live));
                        SnapshotKit.AttachOverlay(map.Canvas);
                        yield return SnapshotKit.Wait(0.4f);
                        if (home)
                        {
                            map.GoHome();
                            yield return SnapshotKit.Wait(0.3f);
                        }

                        Assert.That(map.CanvasWidth, Is.EqualTo(dp).Within(0.6f), "lienzo de " + dp + " dp");
                        string file = "m21_" + name + "_" + dp.ToString(CultureInfo.InvariantCulture) + "dp_" + (live ? "despues" : "antes");
                        SnapshotKit.Save(dir, file);
                        Debug.Log("AF_MAP " + file + ": v2 " + map.LiveActive + ", vista " + map.View + ", zoom "
                            + map.Zoom.ToString("0.00", CultureInfo.InvariantCulture) + ", zona " + map.ViewportSize + ", selva "
                            + (map.Canopy != null ? map.Canopy.LastQuadCount : 0) + " quads, corriente " + map.FlowSlotCount + ", arte "
                            + (map.LiveArt.TextureBytes / 1024L) + " KiB");
                        boot.CloseRiverMap();
                        yield return null;
                    }
                }
            }
        }
    }
}
