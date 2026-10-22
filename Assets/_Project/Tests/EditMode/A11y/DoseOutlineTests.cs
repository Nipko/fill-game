#nullable enable
using System.IO;
using System.Text.RegularExpressions;
using AF.Platform;
using AF.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Contorno de dosis de «Alto contraste» (07 §3, regla 5: 1,5 px de diseño, que el cliente toma como dp [P]; Q-B9-6): la propiedad <c>_DoseOutlinePx</c> del shader del
    /// líquido, que fija <see cref="LiquidGraphic.ApplyDoseOutline"/>, y su espejo en CPU (<see cref="LiquidGraphic.DoseOutlineAt"/>).
    /// Las capturas del shader real están en <c>AF.Tests.PlayMode.DoseOutlineCaptureTests</c> (<c>[Explicit]</c>).
    /// </summary>
    public sealed class DoseOutlineTests
    {
        private const string LiquidShaderPath = "_Project/Shaders/Resources/AF_UI_Liquid.shader";

        [TearDown]
        public void TearDown()
        {
            LiquidGraphic.ApplyDoseOutline(0f);
        }

        [Test]
        public void TheOutline_Is15Px_AsIn07Rule5()
        {
            Assert.That(A11yRules.HighContrastDoseOutlinePx, Is.EqualTo(1.5f));
        }

        [Test]
        public void ApplyDoseOutline_SetsTheSharedMaterial_AndZeroTakesItAway()
        {
            LiquidGraphic.ApplyDoseOutline(A11yRules.HighContrastDoseOutlinePx);
            Assert.That(LiquidGraphic.ShaderDoseOutline, Is.EqualTo(1.5f).Within(1e-6f));
            LiquidGraphic.ApplyDoseOutline(0f);
            Assert.That(LiquidGraphic.ShaderDoseOutline, Is.EqualTo(0f));
            LiquidGraphic.ApplyDoseOutline(-2f);
            Assert.That(LiquidGraphic.ShaderDoseOutline, Is.EqualTo(0f), "un negativo cuenta como 0");
            LiquidGraphic.ApplyDoseOutline(float.NaN);
            Assert.That(LiquidGraphic.ShaderDoseOutline, Is.EqualTo(0f), "y un NaN también");
        }

        [Test]
        public void TheMirror_DrawsTheLineCenteredOnTheArc_OfEveryDoseButTheBottomOne()
        {
            const float w = 1.5f; // dp
            const float density = 3f; // px por dp (un teléfono de 1.080 px de ancho a 360 dp)
            Assert.That(LiquidGraphic.DoseOutlineAt(0, w, density, 0.5f), Is.EqualTo(0f), "la dosis 0 no lleva línea: la separa el fondo");
            Assert.That(LiquidGraphic.DoseOutlineAt(1, 0f, density, 0.5f), Is.EqualTo(0f), "sin Alto contraste, nada");
            Assert.That(LiquidGraphic.DoseOutlineAt(1, w, density, 0f), Is.EqualTo(1f), "sobre el arco, tinta entera");
            Assert.That(LiquidGraphic.DoseOutlineAt(3, w, density, 1.75f), Is.EqualTo(1f), "centrada en el arco: por encima");
            Assert.That(LiquidGraphic.DoseOutlineAt(3, w, density, -1.75f), Is.EqualTo(1f), "y por debajo");
            Assert.That(LiquidGraphic.DoseOutlineAt(3, w, density, 2.25f), Is.EqualTo(0.5f).Within(1e-6f), "medio píxel de antialias en el borde");
            Assert.That(LiquidGraphic.DoseOutlineAt(3, w, density, -2.25f), Is.EqualTo(0.5f).Within(1e-6f));
            Assert.That(LiquidGraphic.DoseOutlineAt(3, w, density, 2.75f), Is.EqualTo(0f), "1,5 dp = 4,5 px en total");
            Assert.That(LiquidGraphic.DoseOutlineAt(3, w, 1f, 0.75f), Is.EqualTo(0.5f).Within(1e-6f), "a 1 px por dp, 1,5 px");
            Assert.That(LiquidGraphic.DoseOutlineAt(3, w, density, 30f), Is.EqualTo(0f), "ni en el cuerpo de la dosis");
        }

        [Test]
        public void TheShader_DeclaresAndUsesTheOutline()
        {
            string code = File.ReadAllText(Path.Combine(Application.dataPath, LiquidShaderPath));
            code = Regex.Replace(code, @"//[^\r\n]*", string.Empty);
            Assert.That(Regex.IsMatch(code, @"_DoseOutlineDp\s*\(""[^""]*"",\s*Float\)\s*=\s*0\b"), Is.True, "propiedad, 0 por defecto");
            Assert.That(Regex.IsMatch(code, @"float\s+_DoseOutlineDp\s*;"), Is.True, "uniforme");
            Assert.That(Regex.IsMatch(code, @"if\s*\(\s*_DoseOutlineDp\s*>\s*0\.0\s*&&\s*k\s*>\s*0\.5\s*\)"), Is.True,
                "rama: solo con contorno y en las dosis k > 0 (espejo de DoseOutlineAt)");
            Assert.That(code.Contains("float halfU = 0.5 * _DoseOutlineDp * dpU;"), Is.True, "en dp, como el espejo en CPU");
            Assert.That(code.Contains("((halfU - abs(ya - doseBottom)) / pix) + 0.5"), Is.True, "centrada en el arco, con medio píxel de antialias");
            Assert.That(code.Contains("alpha = max(alpha, outline * topAlpha);"), Is.True, "opaca también bajo el arco");
            Assert.That(LiquidGraphic.OutlineMarginU, Is.GreaterThan(0f), "la malla baja bajo el arco para la mitad de abajo");
        }
    }
}
