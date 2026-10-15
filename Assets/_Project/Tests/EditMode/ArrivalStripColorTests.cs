#nullable enable
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Colores de la fila del río de la llegada (tarea M1.6, revisiones; D-003): con los tres <c>water_base</c> (glosario §11),
    /// el agua de la franja se distingue del cauce y de su borde, el cauce se ve sobre el panel crema y el agua blanca llega al
    /// mínimo de 3:1 de un elemento gráfico frente al panel. En la ronda 1 el agua blanca (#B08A5A), el borde (#B89A6A) y el
    /// cauce (#E2D2B0) eran casi del mismo tono y la fila del 80 no se leía como río.
    /// </summary>
    public sealed class ArrivalStripColorTests
    {
        [TestCase("white")]
        [TestCase("black")]
        [TestCase("clear")]
        public void TheWaterOfEachWaterBase_StandsOutFromTheBedAndItsEdge(string waterBase)
        {
            Color water = ArrivalStrip.StripWater(waterBase);
            float bed = ColorMath.ContrastRatio(water, ArrivalStrip.BedColor);
            Assert.That(bed, Is.GreaterThanOrEqualTo(3f), waterBase + ": agua frente al canto de vidrio del cauce (" + bed.ToString("0.00") + ":1)");
            float edge = ColorMath.DeltaE00(water, ArrivalStrip.BedEdge);
            Assert.That(edge, Is.GreaterThanOrEqualTo(10f), waterBase + ": el borde del cauce no tiene el tono del agua (ΔE00 " + edge.ToString("0.0") + ")");
        }

        [TestCase("white")]
        [TestCase("black")]
        [TestCase("clear")]
        public void TheBaseWaterOfThePalette_AlsoStandsOutFromTheBed(string waterBase)
        {
            // La exigencia de la revisión, sobre Palette.WaterBase tal cual: el cauce sirve también para el agua de la paleta.
            Color water = Palette.WaterBase(waterBase);
            Assert.That(ColorMath.ContrastRatio(water, ArrivalStrip.BedColor), Is.GreaterThanOrEqualTo(2.5f), waterBase);
            Assert.That(ColorMath.DeltaE00(water, ArrivalStrip.BedEdge), Is.GreaterThanOrEqualTo(10f), waterBase);
        }

        [Test]
        public void TheBedReadsOnThePanel_AndTheWhiteWaterReaches3To1()
        {
            Assert.That(ColorMath.ContrastRatio(ArrivalStrip.BedEdge, Palette.PanelLight), Is.GreaterThanOrEqualTo(4.5f), "borde del cauce frente al panel");
            Assert.That(ColorMath.ContrastRatio(ArrivalStrip.BedEdge, ArrivalStrip.BedColor), Is.GreaterThanOrEqualTo(4.5f), "borde frente al canto de vidrio");
            Assert.That(ColorMath.ContrastRatio(ArrivalStrip.StripWhiteWater, Palette.PanelLight), Is.GreaterThanOrEqualTo(3f), "agua blanca frente al panel");
            Assert.That(ArrivalStrip.StripWater("white"), Is.EqualTo(ArrivalStrip.StripWhiteWater));
            Assert.That(ArrivalStrip.StripWater("black"), Is.EqualTo(Palette.WaterBlack));
            Assert.That(ArrivalStrip.StripWater("clear"), Is.EqualTo(Palette.WaterClear));
        }

        [Test]
        public void TheWelcome_IsGreenAndReadable_AndDiffersFromThePlaceLine()
        {
            Assert.That(ColorMath.ContrastRatio(ArrivalStrip.WelcomeInk, Palette.PanelLight), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(ColorMath.ContrastRatio(ArrivalStrip.PlaceInk, Palette.PanelLight), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(ColorMath.DeltaE00(ArrivalStrip.WelcomeInk, ArrivalStrip.PlaceInk), Is.GreaterThanOrEqualTo(20f), "bienvenida y lugar, distintas");
        }
    }
}
