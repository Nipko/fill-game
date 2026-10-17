#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas del río de vidrio que se llena (tarea M3.4; D-013, puntos 4 y 5; propuesta del Mapa §3.3-§3.5), con el Mapa
    /// abierto desde el inicio a 360 × 780 dp:
    /// <list type="bullet">
    /// <item><c>14_map_lleno_mundo_max58</c>: vista Mundo con <c>max_level</c> 58, el lleno hasta Manaos, el frente al 80 % hacia
    /// Anavilhanas con sus 10 boyas (8 encendidas; las del 59 y el 60, encima de la canoa) y los rótulos «Andes ↑» y «Océano
    /// Atlántico ↓»;</item>
    /// <item><c>15_map_lleno_cuenca_max58</c>: la misma, en la Cuenca, con los países, las siluetas de los mundos 4 y 5 y las
    /// boyas en la fila corta bajo la parada (el tramo, a 1×, no deja distinguirlas);</item>
    /// <item><c>16_map_lleno_cuenca_max200</c>: la Cuenca con <c>max_level</c> 200 (el Tapajós claro, el Negro negro, los de paso
    /// enteros, y el Madeira y el Napo solo en su boca, de al menos 14 dp), con el lleno en su orla de vidrio y su reflejo sobre el
    /// vidrio fantasma;</item>
    /// <item><c>17_map_lleno_mundo_saltado</c>: vista Mundo con el 55 y el 57 saltados (sus boyas llevan a Rosa);</item>
    /// <item><c>18_map_lleno_cuenca_silueta_w6</c>: la Cuenca con <c>max_level</c> 140 y una gran parada del mundo 6 en el Napo
    /// (como la de M3.7a): su silueta «Mundo 6 · Oriente · Nivel 201».</item>
    /// </list>
    /// Cada captura escribe su línea <c>AF_MAP</c>. Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode
    /// --filter AF.Tests.PlayMode.MapProgressSnapshotTests</c> sin <c>-nographics</c>); los PNG quedan en
    /// <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas del río de vidrio que se llena (M3.4); necesitan dispositivo gráfico.")]
    public sealed class MapProgressSnapshotTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 16, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
            UnityEngine.Rendering.OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator CaptureTheFilledRiver()
        {
            string dir = SnapshotKit.Prepare();
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // 360 × 780 dp
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
            Assert.That(map.FrontFraction, Is.EqualTo(0.8f).Within(1e-6f));
            SnapshotKit.Save(dir, "14_map_lleno_mundo_max58");
            Log("14_map_lleno_mundo_max58", map);

            map.ViewBasinButton.onClick.Invoke();
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "15_map_lleno_cuenca_max58");
            Log("15_map_lleno_cuenca_max58", map);

            Reopen(boot, boot.RiverStops!, boot.RiverRoutes!, new MemoryProgressStore(201, 200, 200), 200);
            map.SetView(MapView.Basin);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "16_map_lleno_cuenca_max200");
            Log("16_map_lleno_cuenca_max200", map);

            var skipped = new MemoryProgressStore(59, 58, 58);
            skipped.MarkSkipped(55);
            skipped.MarkSkipped(57);
            Reopen(boot, boot.RiverStops!, boot.RiverRoutes!, skipped, 59);
            yield return SnapshotKit.Wait(0.3f);
            Assert.That(map.BuoyStateAt(4), Is.EqualTo(RiverMapScreen.BuoyState.Skipped));
            Assert.That(map.RosaLayer!.GetSiblingIndex(), Is.GreaterThan(map.Canoe.transform.GetSiblingIndex()), "Rosa, encima de la canoa");
            SnapshotKit.Save(dir, "17_map_lleno_mundo_saltado");
            Log("17_map_lleno_mundo_saltado", map);

            WithWorld6(boot, out RiverStopCatalog stops, out RiverRoutes routes);
            Reopen(boot, stops, routes, new MemoryProgressStore(141, 140, 140), 141);
            map.SetView(MapView.Basin);
            yield return SnapshotKit.Wait(0.3f);
            Assert.That(map.SilhouetteCount, Is.EqualTo(1));
            Rect plate = WorldRect(map.SilhouettePlate(0).rectTransform);
            Rect canoe = WorldRect(map.Canoe.rectTransform);
            Assert.That(plate.Overlaps(canoe), Is.False, "la canoa no tapa la placa del mundo 6");
            SnapshotKit.Save(dir, "18_map_lleno_cuenca_silueta_w6");
            Log("18_map_lleno_cuenca_silueta_w6", map);
        }

        private static void Reopen(GameBootstrap boot, RiverStopCatalog stops, RiverRoutes routes, IMapProgress progress, int playLevel)
        {
            RiverMapScreen map = boot.RiverMapView!;
            map.Close();
            map.Open(MapOpenSource.Lobby, boot.RiverMapData!, routes, stops, progress, playLevel, playLevel <= 200);
        }

        // Las paradas 0-19 y una gran parada del mundo 6 en el Napo (Mazán, como la de M3.7a).
        private static void WithWorld6(GameBootstrap boot, out RiverStopCatalog stops, out RiverRoutes routes)
        {
            RiverMap data = boot.RiverMapData!;
            var list = new List<RiverStop>();
            var routeList = new List<RiverStopRoute>();
            for (int n = 0; n < 20; n++)
            {
                list.Add(boot.RiverStops!.At(n)!);
                routeList.Add(boot.RiverRoutes!.At(n)!);
            }

            list.Add(new RiverStop(20, 6, "mazan", StopKind.Gate, "PE", null, null, 0, "white", null, null, new string[0], null, false,
                new RiverStopBackdrop("bg_kit_w06", null, null, null)));
            int maranon = data.LineIndexOf("maranon");
            int napo = data.LineIndexOf("napo");
            routeList.Add(new RiverStopRoute(20, "mazan", new RiverMapPlace(napo, 0.946947f),
                new[]
                {
                    new RiverPathPiece(maranon, routeList[19].At.S, 1f),
                    new RiverPathPiece(data.LineIndexOf("amazonas"), data.Lines[maranon].Mouth!.Value.S, data.Lines[napo].Mouth!.Value.S),
                    new RiverPathPiece(napo, 1f, 0.946947f),
                },
                new string[0], new int[0]));
            stops = new RiverStopCatalog("test", list.ToArray());
            routes = new RiverRoutes(routeList.ToArray());
        }

        // Caja de rt en coordenadas del mundo (las dos capas del Mapa cuelgan del mismo lienzo).
        private static Rect WorldRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static void Log(string name, RiverMapScreen map)
        {
            Debug.Log("AF_MAP " + name + ": vista " + map.View + ", zoom " + map.Zoom.ToString("0.00", CultureInfo.InvariantCulture)
                + ", max " + map.ProgressLevel + ", frente " + map.FrontStop + " al " + map.FrontFraction.ToString("0.0", CultureInfo.InvariantCulture)
                + ", boyas " + map.BuoyCount + ", siluetas " + map.SilhouetteCount + ", países " + map.CountryLabelCount);
        }
    }
}
