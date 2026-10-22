#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Simulador de daltonismo de B9 sobre la paleta de 14 tintes (07 §3, regla 3, y §7.8; 08 §14.3; el método es [P], Q-B9-10:
    /// Machado et al. 2009 con severidad 1,0 y CIEDE2000). Comprueba el simulador y que, en cada uno de los tres tipos, el par de
    /// tintes más parecido es un par de riesgo de la lista del GDD (07 §3, columna «Pares de riesgo»; glosario §9; la que aplica el
    /// generador con <c>max_risk_pairs</c>). No hay umbral numérico: el GDD no lo fija (Q-B9-10). El informe completo sale en la
    /// consola del test, y el de los píxeles del shader real, en <c>ColorVisionShaderTests</c> (PlayMode, <c>[Explicit]</c>).
    /// </summary>
    public sealed class ColorVisionTests
    {
        /// <summary>Los 10 pares de riesgo de 07 §3 (y de <c>tools/level-generator/specs/campaign_v1.json</c>), por número de motor.</summary>
        public static readonly int[,] RiskPairs =
        {
            { 1, 2 }, { 3, 12 }, { 4, 5 }, { 4, 14 }, { 5, 6 }, { 6, 13 }, { 8, 9 }, { 10, 11 }, { 11, 12 }, { 13, 14 },
        };

        /// <summary>
        /// Los 2 pares del simulador de daltonismo de 07 §3, regla 3 (y glosario §9; <c>cvd_pairs</c> de
        /// <c>campaign_v1.json</c>): 7-10 (protanopía) y 5-14 (deuteranopía). No cuentan en <c>max_risk_pairs</c>; el generador
        /// no los deja juntos en una cuia (Q-B9-13 bis).
        /// </summary>
        public static readonly int[,] CvdPairs = { { 7, 10 }, { 5, 14 } };

        private static readonly ColorVisionDeficiency[] Types = { ColorVisionDeficiency.Protan, ColorVisionDeficiency.Deutan, ColorVisionDeficiency.Tritan };

        [Test]
        public void Grays_StayGray_AndNoneIsTheIdentity()
        {
            foreach (ColorVisionDeficiency type in Types)
            {
                for (int i = 0; i <= 10; i++)
                {
                    var gray = new Color(i / 10f, i / 10f, i / 10f);
                    Color seen = ColorVision.Simulate(gray, type);
                    Assert.That(seen.r, Is.EqualTo(gray.r).Within(0.01f), type + " " + i);
                    Assert.That(seen.g, Is.EqualTo(gray.g).Within(0.01f), type + " " + i);
                    Assert.That(seen.b, Is.EqualTo(gray.b).Within(0.01f), type + " " + i);
                }
            }

            Color acai = Palette.Tint(8);
            Color same = ColorVision.Simulate(acai, ColorVisionDeficiency.None);
            Assert.That(same.r, Is.EqualTo(acai.r).Within(1e-6f));
            Assert.That(same.b, Is.EqualTo(acai.b).Within(1e-6f));
        }

        [Test]
        public void DeltaE2000_MatchesTheReferenceData()
        {
            // Sharma, Wu y Dalal (2005), «The CIEDE2000 color-difference formula», tabla 1, pares 1 y 7.
            Assert.That(ColorVision.DeltaE2000(new Vector3(50f, 2.6772f, -79.7751f), new Vector3(50f, 0f, -82.7485f)), Is.EqualTo(2.0425).Within(1e-3));
            Assert.That(ColorVision.DeltaE2000(new Vector3(50f, 0f, 0f), new Vector3(50f, -1f, 2f)), Is.EqualTo(2.3669).Within(1e-3));
            Assert.That(ColorVision.Lab(Color.white).x, Is.EqualTo(100f).Within(0.01f));
        }

        [Test]
        public void TheClosestPairOfEachDeficiency_IsADeclaredRiskOrSimulatorPair()
        {
            var report = new StringBuilder();
            foreach (ColorVisionDeficiency type in Types)
            {
                List<(double De, int A, int B)> pairs = Pairs(type);
                report.Append(type).Append(':');
                for (int i = 0; i < 8; i++)
                {
                    (double de, int a, int b) = pairs[i];
                    report.Append(' ').Append(a).Append('-').Append(b).Append('=').Append(de.ToString("0.0", CultureInfo.InvariantCulture))
                        .Append(IsRisk(a, b) ? "(R)" : IsCvd(a, b) ? "(S)" : string.Empty);
                }

                report.Append('\n');
                Assert.That(IsRisk(pairs[0].A, pairs[0].B) || IsCvd(pairs[0].A, pairs[0].B), Is.True,
                    type + ": el par más parecido (" + pairs[0].A + "-" + pairs[0].B + ") no es un par de riesgo ni del simulador de 07 §3");
            }

            TestContext.WriteLine("ΔE2000 de los 8 pares más parecidos por tipo ((R) = par de riesgo de 07 §3; (S) = par del simulador):\n" + report);
        }

        /// <summary>Los 91 pares de tintes con su ΔE2000 bajo <paramref name="type"/>, del más parecido al menos.</summary>
        public static List<(double De, int A, int B)> Pairs(ColorVisionDeficiency type)
        {
            var pairs = new List<(double, int, int)>();
            for (int a = 1; a <= 14; a++)
            {
                for (int b = a + 1; b <= 14; b++)
                {
                    pairs.Add((ColorVision.DeltaE2000(Palette.Tint(a), Palette.Tint(b), type), a, b));
                }
            }

            pairs.Sort((x, y) => x.Item1.CompareTo(y.Item1));
            return pairs;
        }

        /// <summary>El par (<paramref name="a"/>, <paramref name="b"/>) es del simulador de daltonismo según 07 §3, regla 3.</summary>
        public static bool IsCvd(int a, int b)
        {
            for (int i = 0; i < CvdPairs.GetLength(0); i++)
            {
                if ((CvdPairs[i, 0] == a && CvdPairs[i, 1] == b) || (CvdPairs[i, 0] == b && CvdPairs[i, 1] == a))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>El par (<paramref name="a"/>, <paramref name="b"/>) es de riesgo según 07 §3.</summary>
        public static bool IsRisk(int a, int b)
        {
            for (int i = 0; i < RiskPairs.GetLength(0); i++)
            {
                if ((RiskPairs[i, 0] == a && RiskPairs[i, 1] == b) || (RiskPairs[i, 0] == b && RiskPairs[i, 1] == a))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
