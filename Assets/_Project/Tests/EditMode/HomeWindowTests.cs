#nullable enable
using System.IO;
using System.Text;
using AF.Levels;
using AF.Localization;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Ventanita de casa de TiTino en el inicio (tarea M2.1; D-013, punto 6, Q17-A; propuesta del Mapa §2.2 y §6.1; D-052): los km
    /// de la píldora redondeados a la decena [P] («a unos 1.480 km» en Manaos) con el separador de la región y el espacio duro
    /// antes de «km», sin píldora en Belém (n = 0) [P], la regla del arte por código (el PNG manda; sin él, el dibujo en todos los
    /// builds, release incluido), la ventanita procedural perezosa de 56 × 56 dp, y que nada de la ventanita recibe toques salvo el área de
    /// TiTino (TI6c).
    /// </summary>
    public sealed class HomeWindowTests
    {
        private const string Nbsp = " ";

        private static RiverStopCatalog? s_stops;

        private ProceduralArt _art = null!;
        private GameObject? _host;
        private LocStateSnapshot _loc;

        private static string RiverStopsPath => Path.Combine(Application.dataPath, "_Project", "Data", "Map", "river_stops_v1.json");

        private static RiverStopCatalog Stops => s_stops ??= RiverStopCatalogReader.Read(File.ReadAllText(RiverStopsPath, Encoding.UTF8));

        [SetUp]
        public void SetUp()
        {
            MapArtRule.ForceProvisional = null;
            _loc = LocStateSnapshot.Take();
            LocSetUpFixture.ConfigureEs419();
            _art = new ProceduralArt("white");
        }

        [TearDown]
        public void TearDown()
        {
            MapArtRule.ForceProvisional = null;
            _loc.Restore();
            if (_host != null)
            {
                Object.DestroyImmediate(_host);
            }

            _art.Dispose();
        }

        [TestCase(1479, 1480)]
        [TestCase(733, 730)]
        [TestCase(1466, 1470)]
        [TestCase(3725, 3730)]
        [TestCase(5, 10)]
        [TestCase(4, 0)]
        [TestCase(0, 0)]
        [TestCase(-12, 0)]
        public void DisplayKm_RoundsToTheNearestTen(int kmHome, int expected)
        {
            Assert.That(HomeWindow.DisplayKm(kmHome), Is.EqualTo(expected), "«a unos»: a la decena [P] (propuesta del Mapa §2.2)");
        }

        [Test]
        public void Manaos_SaysAbout1480Km_WithTheRegionSeparatorAndAHardSpace()
        {
            RiverStop manaos = Stops.StopOf(58)!;
            Assert.That(manaos.Id, Is.EqualTo("manaos"));
            Assert.That(manaos.KmHome, Is.EqualTo(1479), "km_home de river_stops_v1.json");
            Assert.That(HomeWindow.HasPill(manaos), Is.True);
            Assert.That(UiText.MapHomeDistance(HomeWindow.DisplayKm(manaos.KmHome)), Is.EqualTo("Casa · a unos 1.480" + Nbsp + "km"), "sin región del teléfono, es-CO");
            Loc.SetNumberTag("es-MX");
            Assert.That(UiText.MapHomeDistance(HomeWindow.DisplayKm(manaos.KmHome)), Is.EqualTo("Casa · a unos 1,480" + Nbsp + "km"), "con la región de Loc");
        }

        [Test]
        public void Belem_IsHome_WithoutPill_AndEveryOtherStopHasOne()
        {
            RiverStop belem = Stops.At(0)!;
            Assert.That(belem.Id, Is.EqualTo("belem"));
            Assert.That(HomeWindow.HasPill(belem), Is.False, "n = 0: en casa, sin píldora [P]");
            for (int n = 1; n < Stops.Count; n++)
            {
                RiverStop stop = Stops.At(n)!;
                Assert.That(HomeWindow.HasPill(stop), Is.True, stop.Id);
                Assert.That(HomeWindow.DisplayKm(stop.KmHome), Is.GreaterThan(0), stop.Id);
            }
        }

        [Test]
        public void CanShow_FollowsMapArtRule_ForTheFrameAndThePortrait()
        {
            Assert.That(HomeWindow.CanShow(true, false, false), Is.True, "el arte por código");
            Assert.That(HomeWindow.CanShow(MapArtRule.ShowProvisionalIn(false, false), false, false), Is.True, "release sin PNG: el dibujo (D-052)");
            Assert.That(HomeWindow.CanShow(false, true, true), Is.True, "con los dos PNG finales");
            Assert.That(HomeWindow.CanShow(false, true, false), Is.False, "sin arte por código (forzado) ni el retrato final: nada");
            Assert.That(HomeWindow.CanShow(false, false, true), Is.False, "sin arte por código (forzado) ni la ventanita final: nada");
            Assert.That(HomeWindow.CanShow(false, false, false), Is.False);
        }

        [Test]
        public void ProceduralWindow_IsLazy_56dp_WithANineSliceBorder_AndThePngWins()
        {
            Assert.That(_art.HomeWindowBuilt, Is.False, "el constructor no crea la ventanita");
            Assert.That(_art.HomeWindowFor(null, false), Is.Null, "sin arte por código (forzado) ni PNG: nada");
            Assert.That(_art.HomeWindowBuilt, Is.False, "ni la crea");

            Sprite window = _art.HomeWindowFor(null, true)!;
            Assert.That(window, Is.Not.Null);
            Assert.That(_art.HomeWindowBuilt, Is.True);
            Assert.That(_art.HomeWindowProcedural, Is.SameAs(window), "se crea una vez y se guarda");
            float k = ProceduralArt.UiPxPerDp;
            Assert.That(window.rect.width / k, Is.EqualTo(ProceduralArt.HomeWindowDp).Within(0.01f));
            Assert.That(window.rect.height / k, Is.EqualTo(ProceduralArt.HomeWindowDp).Within(0.01f));
            Assert.That(ProceduralArt.HomeWindowDp, Is.EqualTo(56f), "56 × 56 dp [P]");
            Assert.That(window.border.x / k, Is.EqualTo(ProceduralArt.HomeWindowBorderDp).Within(0.01f), "9 cortes con el grosor del marco");

            var tex = new Texture2D(4, 4);
            Sprite png = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            try
            {
                Assert.That(_art.HomeWindowFor(png, false), Is.SameAs(png), "el PNG de map_home_window manda, también sin arte por código");
                Assert.That(_art.HomeWindowFor(png, true), Is.SameAs(png));
            }
            finally
            {
                Object.DestroyImmediate(png);
                Object.DestroyImmediate(tex);
            }
        }

        [Test]
        public void HomeScene_ShowsTheStop_HidesOnRequest_AndNothingTakesTouches()
        {
            MapArtRule.ForceProvisional = true;
            _host = new GameObject("Host", typeof(RectTransform), typeof(Canvas));
            var home = new HomeWindow(_host.transform, _art);
            Assert.That(home.SceneMode, Is.True, "la del inicio: TiTino en su rama (D-050)");
            home.PlaceScene(12f, 140f, 92f, 296f, 380f);
            Assert.That(home.Visible, Is.False, "nace oculta");

            RiverStop manaos = Stops.StopOf(58)!;
            home.Apply(manaos, false);
            Assert.That(home.Visible, Is.True);
            Assert.That(home.PillShown, Is.False, "D-050: sin píldora; la distancia la dice TiTino en su bocadillo");
            Assert.That(home.DistanceText, Is.EqualTo("Casa · a unos 1.480" + Nbsp + "km"));
            Assert.That(home.WindowSprite, Is.Null, "sin marco: TiTino cuelga de la rama del decorado");
            Assert.That(home.PortraitSprite, Is.Not.Null, "el retrato de M1.5, por si no hay rig");
            Assert.That(home.Root.rect.width, Is.EqualTo(HomeWindow.SceneWidthDp).Within(0.01f));
            Assert.That(home.Root.rect.height, Is.EqualTo(HomeWindow.SceneAboveDp + 92f + HomeWindow.SceneBelowDp).Within(0.01f));
            Assert.That(home.BranchLineDp, Is.EqualTo(140f));
            Assert.That(home.SceneRigDp, Is.EqualTo(92f));

            foreach (Graphic g in _host.GetComponentsInChildren<Graphic>(true))
            {
                if (home.Sloth != null && g.gameObject == home.Sloth.TapButton.gameObject)
                {
                    continue; // el único toque del inicio: TiTino (TI6c)
                }

                Assert.That(g.raycastTarget, Is.False, g.name + ": la ventanita no recibe más toque que el de TiTino (TI6c)");
            }

            home.Apply(manaos, true);
            Assert.That(home.Visible, Is.False, "oculta con Sin canoas o una pantalla de pago");
            Assert.That(home.PillShown, Is.False);

            home.Apply(Stops.At(0), false);
            Assert.That(home.Visible, Is.True, "en Belém, TiTino");
            Assert.That(home.DistanceText, Is.Empty, "en casa, sin distancia");

            home.Apply(null, false);
            Assert.That(home.Visible, Is.False, "sin parada, nada");
        }

        [Test]
        public void Window_WithoutCodeArtOrFinalArt_StaysHidden()
        {
            Assume.That(ArtSlots.Find(ProceduralArt.HomeWindowSlot), Is.Null, "sin el PNG final de la ventanita");
            MapArtRule.ForceProvisional = false; // forzado: hoy el arte por código sale en todos los builds (D-052)
            _host = new GameObject("Host", typeof(RectTransform), typeof(Canvas));
            var home = new HomeWindow(_host.transform, _art);
            home.PlaceScene(12f, 140f, 92f, 296f, 380f);
            home.Apply(Stops.StopOf(58), false);
            Assert.That(home.Visible, Is.False, "sin arte por código ni PNG, nada");
            Assert.That(home.PillShown, Is.False);
            Assert.That(_art.HomeWindowBuilt, Is.False, "ni se crea");
        }
    }
}
