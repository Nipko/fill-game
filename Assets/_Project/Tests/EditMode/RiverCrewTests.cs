#nullable enable
using AF.GameFlow;
using AF.Localization;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El marco de la tripulación del Mapa del río v2 (D-069, M2-4; propuesta del Mapa v2 §6.1-§6.2), sin Unity: los umbrales del
    /// tucán (20/21) y del tucuxi (50/51, a la salida de Manaos), el cambio de mundo (se quedan en su tramo), <c>ui_map_crew</c> apagada, como mucho dos a bordo,
    /// los miquitos nunca a bordo, la presentación deducida de <c>map_seen_level</c>, el cameo, el atlas por código y los textos nuevos.
    /// </summary>
    public sealed class RiverCrewTests
    {
        private LocStateSnapshot _loc;

        [SetUp]
        public void SetUp()
        {
            _loc = LocStateSnapshot.Take();
            LocSetUpFixture.ConfigureEs419();
        }

        [TearDown]
        public void TearDown()
        {
            _loc.Restore();
        }

        [Test]
        public void TheTable_IsTheToucanAt21InWorld2_AndTheTucuxiAt51InWorld3()
        {
            Assert.That(RiverCrew.Count, Is.EqualTo(2), "P6 (a): en la 1.1, el tucán y el tucuxi");
            CrewMember toucan = RiverCrew.Of(CrewKind.Toucan);
            CrewMember tucuxi = RiverCrew.Of(CrewKind.Tucuxi);
            Assert.That(toucan.JoinLevel, Is.EqualTo(21));
            Assert.That(toucan.World, Is.EqualTo(2));
            Assert.That(toucan.Mode, Is.EqualTo(CrewMode.Bow));
            Assert.That(toucan.SizeDp, Is.EqualTo(18f), "§6.2: polizón en la proa, 18 dp");
            Assert.That(tucuxi.JoinLevel, Is.EqualTo(51), "la salida de Manaos (pregunta 2 de M2-4)");
            Assert.That(tucuxi.World, Is.EqualTo(3));
            Assert.That(tucuxi.Mode, Is.EqualTo(CrewMode.Swim));
            Assert.That(tucuxi.SizeDp, Is.InRange(16.5f, 18f), "unos 17 dp, el 75 % de Rosa (pregunta 1 de M2-4)");
            Assert.That(tucuxi.SizeDp, Is.LessThan(RiverMapScreen.GuideWorldDp), "más pequeño que Rosa, como el animal");
        }

        [TestCase(0, false)]
        [TestCase(20, false)]
        [TestCase(21, true)]
        [TestCase(40, true)]
        [TestCase(41, false)]
        [TestCase(700, false)]
        public void TheToucan_JoinsAt21_AndStaysInWorld2(int max, bool aboard)
        {
            CrewMember toucan = RiverCrew.Of(CrewKind.Toucan);
            Assert.That(RiverCrew.IsAboard(toucan, max, true), Is.EqualTo(aboard), "max " + max);
            Assert.That(RiverCrew.IsAboardCapped(CrewKind.Toucan, max, true), Is.EqualTo(aboard), "max " + max);
            Assert.That(RiverCrew.StaysBehind(toucan, max, true), Is.EqualTo(max > 40), "en su tramo al cambiar de mundo (max " + max + ")");
        }

        [TestCase(50, false)]
        [TestCase(51, true)]
        [TestCase(71, true)]
        [TestCase(80, true)]
        [TestCase(81, false)]
        public void TheTucuxi_JoinsAt51_AndStaysInWorld3(int max, bool aboard)
        {
            CrewMember tucuxi = RiverCrew.Of(CrewKind.Tucuxi);
            Assert.That(RiverCrew.IsAboard(tucuxi, max, true), Is.EqualTo(aboard), "max " + max);
            Assert.That(RiverCrew.StaysBehind(tucuxi, max, true), Is.EqualTo(max > 80), "max " + max);
        }

        [Test]
        public void WithUiMapCrewOff_NobodyAtAll()
        {
            var into = new CrewKind[RiverCrew.MaxAboard];
            for (int max = 0; max <= 1000; max++)
            {
                Assert.That(RiverCrew.Aboard(max, false, into), Is.EqualTo(0), "max " + max);
                for (int i = 0; i < RiverCrew.Count; i++)
                {
                    CrewMember m = RiverCrew.At(i);
                    Assert.That(RiverCrew.Joined(m, max, false) || RiverCrew.StaysBehind(m, max, false) || RiverCrew.IntroPending(m, 0, max, false), Is.False);
                }

                Assert.That(RiverCrew.CameoLevel(max, max + 10, false), Is.EqualTo(-1f), "sin cameo");
            }
        }

        [Test]
        public void AtMostTwoAboard_TheNewestFirst_AndNeverAMonkey()
        {
            var into = new CrewKind[RiverCrew.MaxAboard];
            for (int max = 0; max <= 1000; max++)
            {
                int n = RiverCrew.Aboard(max, true, into);
                Assert.That(n, Is.LessThanOrEqualTo(RiverCrew.MaxAboard), "§6.1, regla 7");
                for (int i = 0; i < n; i++)
                {
                    Assert.That(into[i], Is.Not.EqualTo(CrewKind.Monkey), "los miquitos no suben a la canoa (D-010)");
                    Assert.That(RiverCrew.IsAboardCapped(into[i], max, true), Is.True);
                    if (i > 0)
                    {
                        Assert.That(RiverCrew.Of(into[i - 1]).JoinLevel, Is.GreaterThan(RiverCrew.Of(into[i]).JoinLevel), "el más reciente, primero");
                    }
                }

                Assert.That(RiverCrew.IsAboardCapped(CrewKind.Monkey, max, true), Is.False);
            }

            Assert.That(RiverCrew.BoardsTheCanoe(CrewKind.Monkey), Is.False);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => RiverCrew.Of(CrewKind.Monkey));
            Assert.That(RiverCrew.Aboard(25, true, into), Is.EqualTo(1));
            Assert.That(into[0], Is.EqualTo(CrewKind.Toucan));
            Assert.That(RiverCrew.Aboard(75, true, into), Is.EqualTo(1), "en el mundo 3, solo el tucuxi: el tucán se quedó en su tramo");
            Assert.That(into[0], Is.EqualTo(CrewKind.Tucuxi));
        }

        [Test]
        public void TheIntro_IsTheFirstMapAfterThePoint_FromTheSeenLevel_WithoutANewSaveField()
        {
            CrewMember toucan = RiverCrew.Of(CrewKind.Toucan);
            Assert.That(RiverCrew.IntroPending(toucan, 20, 21, true), Is.True, "el Mapa no se vio desde el 20: se presenta");
            Assert.That(RiverCrew.IntroPending(toucan, 15, 25, true), Is.True);
            Assert.That(RiverCrew.IntroPending(toucan, 21, 25, true), Is.False, "ya presentado (map_seen_level ≥ 21)");
            Assert.That(RiverCrew.IntroPending(toucan, 20, 20, true), Is.False, "aún no se unió");
            Assert.That(RiverCrew.IntroPending(toucan, 5, 45, true), Is.False, "ya se quedó en su tramo: no se une a este viaje");
            Assert.That(RiverCrew.IntroPending(toucan, 20, 21, false), Is.False, "ui_map_crew apagada");
            CrewMember tucuxi = RiverCrew.Of(CrewKind.Tucuxi);
            Assert.That(RiverCrew.IntroPending(tucuxi, 50, 51, true), Is.True);
            Assert.That(RiverCrew.IntroPending(tucuxi, 51, 52, true), Is.False);
            Assert.That(RiverCrew.IntroPending(tucuxi, 50, 81, true), Is.False, "ya se quedó en su tramo");
        }

        [Test]
        public void TheCameo_IsHalfwayThroughAJourney()
        {
            Assert.That(RiverCrew.CameoLevel(60f, 70f, true), Is.EqualTo(65f));
            Assert.That(RiverCrew.CameoLevel(70f, 70f, true), Is.EqualTo(-1f), "sin viaje, sin cameo");
        }

        [Test]
        public void TheAtlas_IsPaintedByCode_128KiB_AndReleased()
        {
            var art = new MapCrewArt();
            try
            {
                art.Build(null);
                Assert.That(art.Built, Is.True);
                Assert.That(art.TextureBytes, Is.EqualTo(128L * 1024L));
                Assert.That(art.Piece(CrewPiece.ToucanBody), Is.Not.Null);
                Assert.That(art.Piece(CrewPiece.Tucuxi), Is.Not.Null);
                Assert.That(art.Piece(CrewPiece.MonkeyBody), Is.Null, "sin paleta, sin miquito");
                var palette = new AF.Levels.FaunaPalette(0x8A6A44, 0x6E5230, 0xF2D7B6, 0xE8C9A0, 0x1E140E, 0x6E5230, 0x1E140E);
                art.Build(palette);
                Assert.That(art.HasMonkey, Is.True);
                Assert.That(art.Piece(CrewPiece.MonkeyHead), Is.Not.Null);
            }
            finally
            {
                art.Release();
            }

            Assert.That(art.Built, Is.False);
            Assert.That(art.TextureBytes, Is.EqualTo(0L));

            // Los píxeles de cada pieza, pintados en un lienzo (la textura no se puede leer): con contenido y sin NaN.
            var pc = new PixelCanvas(ProceduralArt.CrewAtlasWidthPx, ProceduralArt.CrewAtlasHeightPx);
            pc.Fill(new Color(0f, 0f, 0f, 0f));
            ProceduralArt.PaintCrewAtlas(pc, new AF.Levels.FaunaPalette(0x8A6A44, 0x6E5230, 0xF2D7B6, 0xE8C9A0, 0x1E140E, 0x6E5230, 0x1E140E));
            Color32[] px = pc.TakeColor32();
            Assert.That(Opaque(px, CrewPiece.ToucanBody), Is.GreaterThan(200), "el tucán");
            Assert.That(Opaque(px, CrewPiece.ToucanBeakUpper), Is.GreaterThan(50), "el pico");
            Assert.That(Opaque(px, CrewPiece.Tucuxi), Is.GreaterThan(400), "el tucuxi");
            Assert.That(Opaque(px, CrewPiece.Tree), Is.GreaterThan(1000), "el árbol");
            Assert.That(Opaque(px, CrewPiece.MonkeyHead), Is.GreaterThan(100), "el miquito");
        }

        [Test]
        public void TheTexts_ChipAndTalkBack_PerSpecies_AndFaltanInTheLabelWithThePlural()
        {
            Assert.That(UiText.MapCrewJoin(CrewKind.Toucan), Is.EqualTo("¡Se une al viaje! · Tucán"));
            Assert.That(UiText.MapCrewJoin(CrewKind.Tucuxi), Is.EqualTo("¡Se une al viaje! · Tucuxi"));
            Assert.That(UiText.A11yMapCrewJoin(CrewKind.Toucan), Is.EqualTo("Se une al viaje: tucán"));
            Assert.That(UiText.A11yMapCrewJoin(CrewKind.Tucuxi), Is.EqualTo("Se une al viaje: tucuxi"));
            Assert.That(UiText.MapStopLabelRemaining("Tefé", 5), Is.EqualTo("Tefé\nfaltan 5 niveles"));
            Assert.That(UiText.MapStopLabelRemaining("Tefé", 1), Is.EqualTo("Tefé\nfalta 1 nivel"), "nunca «1 niveles»");
            Assert.That(UiText.MapStopLabelRemaining("Tefé", -2), Is.EqualTo("Tefé\nfaltan 0 niveles"));
        }

        private static int Opaque(Color32[] px, CrewPiece piece)
        {
            RectInt r = ProceduralArt.CrewCell(piece);
            int n = 0;
            for (int y = r.yMin; y < r.yMax; y++)
            {
                for (int x = r.xMin; x < r.xMax; x++)
                {
                    n += px[(y * ProceduralArt.CrewAtlasWidthPx) + x].a > 128 ? 1 : 0;
                }
            }

            return n;
        }
    }
}
