#nullable enable
using System.Collections;
using System.Globalization;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de la tarea FX8 (pendientes de código de la ola 6):
    /// <list type="bullet">
    /// <item><description>«ponerse al día» que cruza Santarém (visto 5, max 12; M3.5), con la vista arrastrada a Santarém o al
    /// Encuentro: al abrir (<c>fx8_map_catchup_l05</c> y <c>_l05_santarem</c>, con la placa del mundo 2), justo al llegar la canoa
    /// al 10 (<c>_l10_santarem</c>, ya sin placa; antes de FX8, la caja del mundo 2 decía ahí «Mundo 3 · Manaos», y
    /// <c>_l10_encontro</c>, con la del mundo 3 junto a su gran parada), al acabar (<c>_l12_encontro</c>) y en la vista recién
    /// abierta con 12 (<c>fx8_map_fresh_l12</c> y <c>_l12_encontro</c>, con la placa en el mismo sitio);</description></item>
    /// <item><description>la ventanita del hito p20 de la tarjeta previa con la temporada del nivel que se va a jugar (07 §2.1):
    /// el 60 (ciclo A) sin la LUT de temporada, con ella y con la B forzada, y el 40 (ciclo B) con ella y con la A forzada;</description></item>
    /// <item><description>la tarjeta de la parada del Mapa con la temporada del primer nivel de la parada: Manaos (51, ciclo A)
    /// sin ella, con ella y con la B forzada, y Alter do Chão (21, ciclo B) con ella y con la A forzada.</description></item>
    /// </list>
    /// Cada captura escribe su línea <c>AF_FX8</c>. Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode
    /// --filter AF.Tests.PlayMode.Fx8SnapshotTests</c> sin <c>-nographics</c>); los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas de FX8; necesitan dispositivo gráfico.")]
    public sealed class Fx8SnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            GameScreen.SeasonLutEnabled = true;
            GameScreen.SeasonOverride = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureTheCatchUpAcrossSantarem()
        {
            string dir = SnapshotKit.Prepare();
            yield return MapMarkersTests.OpenApp(13, 5);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(map.Canvas);
            Assert.That(map.CatchingUp, Is.True);
            Assert.That(map.SilhouetteWorld(0), Is.EqualTo(2), "premisa: con 5, la placa del mundo 2");
            Assert.That(map.SilhouetteWorld(1), Is.EqualTo(3));
            SaveMap(dir, "fx8_map_catchup_l05", map);
            CenterOnPlate(map, 2);
            SaveMap(dir, "fx8_map_catchup_l05_santarem", map);

            map.Tick(5 * MapConfig.CatchUpPerBuoyMs);
            Assert.That(map.ProgressLevel, Is.EqualTo(10), "la canoa, en Santarém");
            SaveMap(dir, "fx8_map_catchup_l10_santarem", map); // sin FX8, aquí salía «Mundo 3 · Manaos» en la caja del mundo 2
            CenterOnPlate(map, 3);
            SaveMap(dir, "fx8_map_catchup_l10_encontro", map);

            map.Tick(10 * MapConfig.CatchUpPerBuoyMs);
            map.Tick(MapConfig.ArrivalSampleFillMs);
            Assert.That(map.CatchingUp, Is.False);
            SaveMap(dir, "fx8_map_catchup_l12_encontro", map);

            MapMarkersTests.Reopen(boot, new MemoryProgressStore(13, 12, 12), 13);
            Assert.That(map.CatchingUp, Is.False);
            SaveMap(dir, "fx8_map_fresh_l12", map);
            CenterOnPlate(map, 3);
            SaveMap(dir, "fx8_map_fresh_l12_encontro", map);
        }

        // Arrastra la vista para dejar en el centro de la zona visible el punto medio entre la placa del mundo world y su gran
        // parada (sin tocar el Mapa: no completa «ponerse al día» ni recoloca nada).
        private static void CenterOnPlate(RiverMapScreen map, int world)
        {
            int i = 0;
            while (map.SilhouetteWorld(i) != world)
            {
                i++;
            }

            Vector2 p = (map.SilhouetteLocalBox(i).center + map.Space.ToLocal(map.SilhouetteMapPoint(i))) * 0.5f;
            Rect view = map.ViewportRect.rect;
            var want = new Vector2((view.width * 0.5f) - p.x, (view.height * 0.5f) + p.y);
            Vector2 now = map.ContentOffset;
            map.Drag(new Vector2(want.x - now.x, now.y - want.y));
        }

        [UnityTest]
        public IEnumerator CaptureTheHitoCardsWithTheSeason()
        {
            string dir = SnapshotKit.Prepare();
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            yield return CaptureCard(dir, "fx8_card_hito_l60_off", 60, false, null);
            yield return CaptureCard(dir, "fx8_card_hito_l60_a", 60, true, null);
            yield return CaptureCard(dir, "fx8_card_hito_l60_b_forced", 60, true, Season.LowWater);
            yield return CaptureCard(dir, "fx8_card_hito_l40_b", 40, true, null);
            yield return CaptureCard(dir, "fx8_card_hito_l40_a_forced", 40, true, Season.HighWater);
        }

        [UnityTest]
        public IEnumerator CaptureTheStopCardsWithTheSeason()
        {
            string dir = SnapshotKit.Prepare();
            yield return CaptureStopCard(dir, "fx8_stopcard_manaos_off", 5, false, null);
            yield return CaptureStopCard(dir, "fx8_stopcard_manaos_a", 5, true, null);
            yield return CaptureStopCard(dir, "fx8_stopcard_manaos_b_forced", 5, true, Season.LowWater);
            yield return CaptureStopCard(dir, "fx8_stopcard_alter_b", 2, true, null);
            yield return CaptureStopCard(dir, "fx8_stopcard_alter_a_forced", 2, true, Season.HighWater);
        }

        private static void SaveMap(string dir, string name, RiverMapScreen map)
        {
            SnapshotKit.Save(dir, name);
            var line = new System.Text.StringBuilder("AF_FX8 " + name + ": nivel " + map.ProgressLevel.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < map.SilhouetteCount; i++)
            {
                Rect box = map.SilhouetteLocalBox(i);
                Vector2 stop = map.Space.ToLocal(map.SilhouetteMapPoint(i));
                line.Append(", mundo ").Append(map.SilhouetteWorld(i).ToString(CultureInfo.InvariantCulture)).Append(" «")
                    .Append(map.SilhouetteText(i)).Append("» en ").Append(box.min.ToString("F1")).Append(" (parada en ")
                    .Append(stop.ToString("F1")).Append(')');
            }

            Debug.Log(line.ToString());
        }

        private static IEnumerator CaptureCard(string dir, string name, int level, bool season, Season? forced)
        {
            GameScreen.SeasonLutEnabled = season;
            GameScreen.SeasonOverride = forced;
            yield return CardSetup.Load(level);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelCardView card = boot.View!.LevelCard;
            Assert.That(card.Visible, Is.True, name);
            Assert.That(card.HitoStop, Is.Not.Null, name);
            Assert.That(card.HitoLut, Is.EqualTo(StopBackdrop.LutOf(card.HitoStop, card.HitoSeason)), name);
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, name);
            Debug.Log("AF_FX8 " + name + ": nivel " + level + ", parada " + card.HitoStop!.Id + ", temporada "
                + (SeasonTable.Letter(card.HitoSeason) ?? "-") + ", luz " + Rgb(card.HitoLut) + ", fondo " + Rgb(boot.View.StopBackdrop.LutColor));
        }

        private static IEnumerator CaptureStopCard(string dir, string name, int n, bool season, Season? forced)
        {
            GameScreen.SeasonLutEnabled = season;
            GameScreen.SeasonOverride = forced;
            yield return MapMarkersTests.OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(map.Canvas);
            Assert.That(map.OpenStopCard(n), Is.True, name);
            StopCard card = map.Card!;
            Assert.That(card.PanoramaTint, Is.EqualTo(StopBackdrop.LutOf(card.Stop, card.Backdrop.Season)), name);
            map.Tick(16f);
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, name);
            Debug.Log("AF_FX8 " + name + ": parada " + card.Stop!.Id + " (nivel " + card.Stop.FirstLevel + "), temporada "
                + (SeasonTable.Letter(card.Backdrop.Season) ?? "-") + ", luz " + Rgb(card.Backdrop.LutColor));
        }

        private static string Rgb(Color c)
        {
            return c.r.ToString("0.000", CultureInfo.InvariantCulture) + "/" + c.g.ToString("0.000", CultureInfo.InvariantCulture) + "/"
                + c.b.ToString("0.000", CultureInfo.InvariantCulture);
        }
    }
}
