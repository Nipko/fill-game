#nullable enable
using System.IO;
using AF.Levels;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El tope de densidad del arte por código (<c>docs/dev/memoria.md</c> §7.5, pregunta 1, opción (a), y §9.1; D-060):
    /// <list type="bullet">
    /// <item><see cref="ProceduralArt.DensityCap"/> y <see cref="ProceduralArt.Capped"/>;</item>
    /// <item>el tope de un dispositivo (<see cref="GameBootstrap.ArtDensityCapFor"/>): solo en teléfonos, con la pantalla entera en
    /// vertical (ni la pantalla dividida ni arrancar en horizontal lo cambian) y redondeado hacia arriba a 0,05;</item>
    /// <item>una pieza pintada con tope 2 mide round(dp × 2) px con PPU 200 y los mismos dp; el panorama y el muelle, a 2; los
    /// modales, a 1,5;</item>
    /// <item>los atlas y los sprites de tamaño fijo en px (la fauna, TiTino, las frutas en vuelo, el rig de Capi y la tripulación del
    /// Mapa, los glifos y la píldora de las Aguas turbias, los iconos) siguen a 3 px/dp con tope 2.</item>
    /// </list>
    /// </summary>
    public sealed class DensityCapTests
    {
        private static ProceduralArt? s_capped;

        // Un arte con tope 2 para toda la clase (pintarlo cuesta ~1 s en el Editor).
        private static ProceduralArt Capped2
        {
            get
            {
                if (s_capped == null)
                {
                    s_capped = new ProceduralArt("white", 2f);
                }

                s_capped.Activate(); // su tope (2), por si otro test lo cambió: las piezas perezosas lo leen
                return s_capped;
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            s_capped?.Dispose();
            s_capped = null;
            ProceduralArt.DensityCap = 0f;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            GameScreen.SimulatedPxPerDp = null;
            GameBootstrap.ArtDensityCapEnabledForTests = true;
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(-1f)]
        [TestCase(0f)]
        public void DensityCap_NaNInfiniteOrNotPositive_MeansNoCap(float value)
        {
            float before = ProceduralArt.DensityCap;
            try
            {
                ProceduralArt.DensityCap = value;
                Assert.That(ProceduralArt.DensityCap, Is.EqualTo(0f));
                Assert.That(ProceduralArt.UiPxPerDp, Is.EqualTo(ProceduralArt.MaxUiPxPerDp));
                Assert.That(ProceduralArt.Capped(2.25f), Is.EqualTo(2.25f));
            }
            finally
            {
                ProceduralArt.DensityCap = before;
            }
        }

        [Test]
        public void Capped_TakesTheLowerOfThePieceAndTheCap()
        {
            float before = ProceduralArt.DensityCap;
            try
            {
                ProceduralArt.DensityCap = 2f;
                Assert.That(ProceduralArt.DensityCap, Is.EqualTo(2f));
                Assert.That(ProceduralArt.Capped(3f), Is.EqualTo(2f));
                Assert.That(ProceduralArt.Capped(2.25f), Is.EqualTo(2f));
                Assert.That(ProceduralArt.Capped(1.5f), Is.EqualTo(1.5f), "los modales, a 1,5: el tope solo baja");
                Assert.That(ProceduralArt.UiPxPerDp, Is.EqualTo(2f));
                ProceduralArt.DensityCap = 3.5f;
                Assert.That(ProceduralArt.UiPxPerDp, Is.EqualTo(3f), "por encima de 3 px/dp, nada cambia");
            }
            finally
            {
                ProceduralArt.DensityCap = before;
            }
        }

        [TestCase(720, 1600, 2f, 2f, TestName = "ClaseA_720x1600_2")]
        [TestCase(1600, 720, 2f, 2f, TestName = "ClaseA_ArrancaEnHorizontal_MismoTope")]
        [TestCase(1080, 2400, 3f, 3f, TestName = "Telefono_1080x2400_3")]
        [TestCase(1080, 2340, 2.625f, 3f, TestName = "SM_S938B_SinCambio")]
        [TestCase(945, 2048, 2.625f, 2.65f, TestName = "Redondeo_2625_A_265")]
        [TestCase(480, 800, 1.5f, 1.25f, TestName = "Telefono_320dp_AltoMinimo")]
        [TestCase(1600, 2560, 2f, 0f, TestName = "Tableta_SinTope")]
        [TestCase(2176, 1812, 2.625f, 0f, TestName = "PlegableAbierto_SinTope")]
        [TestCase(1366, 768, 1f, 0f, TestName = "Chromebook_SinTope")]
        [TestCase(720, 1600, 0f, 0f, TestName = "DensidadDesconocida_SinTope")]
        public void ArtDensityCapFor_PhoneWholeScreen(int w, int h, float pxPerDp, float expected)
        {
            Assert.That(GameBootstrap.ArtDensityCapFor(w, h, pxPerDp), Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void ArtDensityCapFor_NaNDensity_NoCap()
        {
            Assert.That(GameBootstrap.ArtDensityCapFor(720, 1600, float.NaN), Is.EqualTo(0f));
        }

        [Test]
        public void ArtDensityCap_ReadsTheDeviceNotTheWindow_AndTheTestSwitch()
        {
            GameScreen.SimulatedScreen = new Vector4(720f, 1600f, 48f, 0f);
            GameScreen.SimulatedPxPerDp = 2f;
            Assert.That(GameBootstrap.ArtDensityCap(), Is.EqualTo(2f).Within(0.0001f));

            GameScreen.SimulatedPxPerDp = null;
            Assert.That(GameBootstrap.ArtDensityCap(), Is.EqualTo(0f), "densidad desconocida: sin tope");

            GameScreen.SimulatedPxPerDp = 2f;
            GameBootstrap.ArtDensityCapEnabledForTests = false;
            Assert.That(GameBootstrap.ArtDensityCap(), Is.EqualTo(0f), "con el interruptor de los tests apagado, sin tope");

            // Pantalla dividida: la ventana de 720 × 800 px de un teléfono de 720 × 1600 daría 1,25 px/dp por la ventana; el tope sale
            // del dispositivo entero, así que es el mismo que a pantalla completa.
            Assert.That(GameBootstrap.ArtDensityCapFor(720, 1600, 2f), Is.EqualTo(2f));
            Assert.That(GameScreen.CanvasScale(720, 800, 2f), Is.EqualTo(1.25f).Within(0.0001f), "lo que daría la ventana");
        }

        [Test]
        public void WithCap2_APiece_HasRoundDpTimes2Px_Ppu200_AndTheSameDp()
        {
            ProceduralArt art = Capped2;
            Assert.That(art.BuiltDensityCap, Is.EqualTo(2f));

            AssertPiece(art.HomeBranch, ProceduralArt.HomeBranchWidthDp, ProceduralArt.HomeBranchHeightDp, 2f, "la rama de TiTino");
            AssertPiece(art.Panorama, 360f, ProceduralArt.PanoramaHeightDp, 2f, "el panorama (2,25 → 2)");
            AssertPiece(art.Dock, 360f, ProceduralArt.DockHeightDp, 2f, "el muelle (2,25 → 2)");
            AssertPiece(art.FoliageBottomLeft, ProceduralArt.BottomFoliageDp, ProceduralArt.BottomFoliageDp, 2f, "el follaje (3 → 2)");
            AssertPiece(art.ModalSurfaceFor(312f, 220f), 312f, 220f, 1.5f, "las superficies de los modales siguen a 1,5");
        }

        [Test]
        public void WithCap2_FixedSizeAtlases_StayAt3PxPerDp()
        {
            ProceduralArt art = Capped2;
            const float ppu = 100f * ProceduralArt.MaxUiPxPerDp;
            FaunaCatalog catalog = FaunaCatalogReader.Read(File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Fauna/miquitos_v0.json")));
            Assert.That(art.MonkeyArtFor(catalog.Find("saimiri_collinsi")!).Limb.pixelsPerUnit, Is.EqualTo(ppu), "la fauna");
            SlothArt? sloth = art.SlothRigArt(null, null, true);
            Assert.That(sloth, Is.Not.Null);
            Assert.That(sloth!.Limb.pixelsPerUnit, Is.EqualTo(ppu), "TiTino");
            Assert.That(art.FruitFlight(2).pixelsPerUnit, Is.EqualTo(ppu), "las frutas en vuelo");
            Assert.That(art.MurkyBadgeQuestion.pixelsPerUnit, Is.EqualTo(ppu), "los glifos de las Aguas turbias");
            Assert.That(art.MurkyFruitDisc.pixelsPerUnit, Is.EqualTo(ppu), "el disco de la fruta de la Luciérnaga");
            Assert.That(art.IconUndo.pixelsPerUnit, Is.EqualTo(ppu), "un icono de px fijos");

            // La píldora: 64 × 42 px con 22 px de borde 9-slice; con el PPU de 3 px/dp, los extremos miden lo mismo en dp que el alto
            // relativo de siempre (22 / 42), no 22 de los 30 dp que salían con PPU 200.
            Sprite pill = art.MurkyBadgePill;
            Assert.That(pill.pixelsPerUnit, Is.EqualTo(ppu));
            Assert.That(pill.border.x / pill.rect.height, Is.EqualTo(22f / 42f).Within(0.001f));
            Assert.That(pill.rect.height / (pill.pixelsPerUnit / 100f), Is.EqualTo(14f).Within(0.001f), "14 dp de alto, como sin tope");
        }

        [Test]
        public void WithCap2_TheMapRigAndCrewAtlases_PaintTheSamePixels()
        {
            float before = ProceduralArt.DensityCap;
            try
            {
                ProceduralArt.DensityCap = 0f;
                byte[] capiFree = PaintCapi();
                byte[] crewFree = PaintCrew();
                ProceduralArt.DensityCap = 2f;
                Assert.That(PaintCapi(), Is.EqualTo(capiFree), "el rig de Capi, atlas de tamaño fijo");
                Assert.That(PaintCrew(), Is.EqualTo(crewFree), "la tripulación, atlas de tamaño fijo");
            }
            finally
            {
                ProceduralArt.DensityCap = before;
            }
        }

        private static byte[] PaintCapi()
        {
            var pc = new PixelCanvas(ProceduralArt.CapiRigAtlasWidthPx, ProceduralArt.CapiRigAtlasHeightPx);
            ProceduralArt.PaintCapiRigAtlas(pc);
            return Bytes(pc);
        }

        private static byte[] PaintCrew()
        {
            var pc = new PixelCanvas(ProceduralArt.CrewAtlasWidthPx, ProceduralArt.CrewAtlasHeightPx);
            ProceduralArt.PaintCrewAtlas(pc, null);
            return Bytes(pc);
        }

        private static byte[] Bytes(PixelCanvas pc)
        {
            Color32[] px = pc.ToColor32();
            var b = new byte[px.Length * 4];
            for (int i = 0; i < px.Length; i++)
            {
                b[i * 4] = px[i].r;
                b[(i * 4) + 1] = px[i].g;
                b[(i * 4) + 2] = px[i].b;
                b[(i * 4) + 3] = px[i].a;
            }

            pc.Release();
            return b;
        }

        private static void AssertPiece(Sprite s, float wDp, float hDp, float pxPerDp, string what)
        {
            Assert.That(s.rect.width, Is.EqualTo(Mathf.RoundToInt(wDp * pxPerDp)).Within(0.5f), what + ": ancho en px");
            Assert.That(s.rect.height, Is.EqualTo(Mathf.RoundToInt(hDp * pxPerDp)).Within(0.5f), what + ": alto en px");
            Assert.That(s.pixelsPerUnit, Is.EqualTo(100f * pxPerDp).Within(0.001f), what + ": PPU");
            Assert.That(s.rect.width / (s.pixelsPerUnit / 100f), Is.EqualTo(wDp).Within(0.5f), what + ": ancho en dp");
        }
    }
}
