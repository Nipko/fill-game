#nullable enable
using System;
using System.IO;
using System.Text.RegularExpressions;
using AF.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Estimación temprana del peso del arte (tarea B0): recuento de bloques ASTC frente a la propuesta del Mapa §10.7, ramas de
    /// error de la línea de órdenes (familia desconocida o repetida, opciones malas), compuerta de 80 MB, fallo de la medida y una
    /// importación real que no deja ningún asset ni <c>.meta</c>.
    /// </summary>
    public sealed class SizeEstimateTests
    {
        private static readonly string ReportPath = Path.Combine("Temp", "af-size-estimate-tests.md");

        private int calls;

        [SetUp]
        public void SetUp()
        {
            calls = 0;
            Delete();
        }

        [TearDown]
        public void TearDown()
        {
            Delete();
        }

        [Test]
        public void AstcBytes_MatchTheMapProposal()
        {
            // Propuesta del Mapa §10.7 y README de Art/Generated («Para estimar el peso»).
            Assert.That(SizeEstimate.AstcBytes(720, 240, SizeEstimate.AstcBlock.Astc6x6), Is.EqualTo(75L * 1024L), "héroe: 75,0 KiB");
            Assert.That(SizeEstimate.AstcBytes(1024, 1024, SizeEstimate.AstcBlock.Astc8x8), Is.EqualTo(256L * 1024L), "kit: 256 KiB");
            Assert.That(SizeEstimate.AstcBytes(2048, 2048, SizeEstimate.AstcBlock.Astc8x8), Is.EqualTo(1024L * 1024L), "kit 2×: 1 MiB");
            Assert.That(SizeEstimate.AstcBytes(1440, 480, SizeEstimate.AstcBlock.Astc6x6), Is.EqualTo(300L * 1024L), "héroe 2×: 300 KiB");
            Assert.That(SizeEstimate.AstcBytes(1024, 1024, SizeEstimate.AstcBlock.Astc4x4), Is.EqualTo(1024L * 1024L), "atlas de UI: 1 MiB");
            Assert.That(SizeEstimate.AstcBytes(1024, 1024, SizeEstimate.AstcBlock.Astc6x6), Is.EqualTo(171L * 171L * 16L), "kit en 6×6: 456,9 KiB");
            Assert.That(14 * SizeEstimate.AstcBytes(1024, 640, SizeEstimate.AstcBlock.Astc6x6) / (1024.0 * 1024.0), Is.EqualTo(3.9).Within(0.05), "fichas");
            Assert.Throws<ArgumentOutOfRangeException>(() => SizeEstimate.AstcBytes(0, 10, SizeEstimate.AstcBlock.Astc4x4));
        }

        [Test]
        public void UnknownFamily_ExitsWith2_WithoutMeasuring()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"\[AF\.SizeEstimate\] familia desconocida 'cielo' \(válidas: kits, heroes, mapa, ambiente, sloth, fichas\)"));

            int code = SizeEstimate.Execute(new[] { "Unity.exe", "-batchmode", "-afSizeFamilies", "kits,cielo", "-afSizeReport", ReportPath }, Fake);

            Assert.That(code, Is.EqualTo(SizeEstimate.ExitArguments));
            Assert.That(calls, Is.EqualTo(0));
            Assert.That(File.Exists(ReportPath), Is.False);
        }

        [TestCase(new[] { "-afSizeFamilies", "kits,heroes,kits" }, "familia repetida 'kits'")]
        [TestCase(new[] { "-afSizeFamilies", " , " }, "lista de familias vacía")]
        [TestCase(new[] { "-afSizeFamilies", "Kits" }, "familia desconocida 'Kits'")]
        [TestCase(new[] { "-afSizeFamilies" }, "falta el valor de -afSizeFamilies")]
        [TestCase(new[] { "-afSizeReport", "-afSizeFamilies", "kits" }, "falta el valor de -afSizeReport")]
        [TestCase(new[] { "-afSizeAabMB", "35,5" }, "-afSizeAabMB necesita una cifra")]
        [TestCase(new[] { "-afSizeAabMB", "NaN" }, "-afSizeAabMB necesita una cifra")]
        [TestCase(new[] { "-afSizeFamilia", "kits" }, "opción desconocida -afSizeFamilia")]
        [TestCase(new[] { "-afSizeReport", "a.md", "-afSizeReport", "b.md" }, "opción repetida -afSizeReport")]
        public void BadArguments_ExitWith2(string[] args, string message)
        {
            LogAssert.Expect(LogType.Error, new Regex(@"\[AF\.SizeEstimate\] " + Regex.Escape(message)));

            Assert.That(SizeEstimate.Execute(args, Fake), Is.EqualTo(SizeEstimate.ExitArguments));
            Assert.That(calls, Is.EqualTo(0));
        }

        [Test]
        public void NoOptions_AllFamilies_DefaultReportAndAab()
        {
            Assert.That(SizeEstimate.TryParseOptions(new[] { "Unity.exe", "-executeMethod", "AF.EditorTools.SizeEstimate.Run" },
                out SizeEstimate.Options? options, out string error), Is.True, error);

            Assert.That(options, Is.Not.Null);
            Assert.That(options!.Families, Is.EqualTo(SizeEstimate.Families));
            Assert.That(options.ReportPath, Is.EqualTo(SizeEstimate.DefaultReportPath));
            Assert.That(options.AabMB, Is.EqualTo(SizeEstimate.AabTodayMB));
        }

        [Test]
        public void AllFamilies_WriteTablePerFamilyAndTotal_AgainstTheGate()
        {
            int code = SizeEstimate.Execute(new[] { "-afSizeReport", ReportPath }, Fake);

            Assert.That(code, Is.EqualTo(SizeEstimate.ExitOk));
            string report = File.ReadAllText(ReportPath);
            Assert.That(report, Does.Contain("| Kits de mundo (bg_kit_w01…w09) | 9 | 2,25 | 9,00 |"));
            Assert.That(report, Does.Contain("| Héroes de los sitios (bg_hero_<sitio>) | 15 | 1,10 | 4,39 |"));
            Assert.That(report, Does.Contain("| Mapa del río (map_*) | 6 | 1,56 | 1,56 |"));
            Assert.That(report, Does.Contain("| Ambientes de los sitios (amb_*) | 3 | 0,19 | 0,19 |"));
            Assert.That(report, Does.Contain("| Perezoso protagonista (fauna_bradypus_variegatus_* y arbol_embauba) | 3 | 1,50 | 1,50 |"));
            Assert.That(report, Does.Contain("| Fichas de Frutas del río (ficha_<tinte>) | 14 | 3,91 | 3,91 |"));
            Assert.That(report, Does.Contain("| **Total** | 50 |"));
            Assert.That(report, Does.Contain("| Build (1×) |"));
            Assert.That(report, Does.Contain("| Entrega (2×) |"));
            Assert.That(report, Does.Contain("| 35,0 |"));
            Assert.That(report, Does.Contain("| 80 |"));

            // Una medida por combinación de medida y bloque, no por pieza.
            Assert.That(calls, Is.EqualTo(10));
        }

        [Test]
        public void OverTheGate_ExitsWith3_AndStillWritesTheReport()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"\[AF\.SizeEstimate\] el AAB de hoy más la estimación \(.* MB\) supera la compuerta de 80 MB"));

            int code = SizeEstimate.Execute(new[] { "-afSizeFamilies", "kits", "-afSizeAabMB", "75", "-afSizeReport", ReportPath }, Fake);

            Assert.That(code, Is.EqualTo(SizeEstimate.ExitOverGate));
            Assert.That(File.ReadAllText(ReportPath), Does.Contain("| Entrega (2×) | 9,44 | 75,0 | 84,44 | 80 | -4,44 |"));
        }

        [Test]
        public void FailedMeasurement_ExitsWith4()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"\[AF\.SizeEstimate\] no se pudo importar"));

            int code = SizeEstimate.Execute(new[] { "-afSizeFamilies", "mapa", "-afSizeReport", ReportPath },
                (w, h, b) => throw new InvalidOperationException("no se pudo importar el relleno."));

            Assert.That(code, Is.EqualTo(SizeEstimate.ExitFailed));
            Assert.That(File.Exists(ReportPath), Is.False);
        }

        [Test]
        public void NonPositiveMeasurement_ExitsWith4()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"\[AF\.SizeEstimate\] medida no válida de 256 × 256 ASTC 4×4: 0 B"));

            int code = SizeEstimate.Execute(new[] { "-afSizeFamilies", "ambiente", "-afSizeReport", ReportPath },
                (w, h, b) => new SizeEstimate.Measurement(0, "prueba"));

            Assert.That(code, Is.EqualTo(SizeEstimate.ExitFailed));
            Assert.That(File.Exists(ReportPath), Is.False);
        }

        [Test]
        public void RealImport_SmallFamily_MeasuresAndLeavesNoAsset()
        {
            int code = SizeEstimate.Execute(new[] { "-afSizeFamilies", "ambiente", "-afSizeReport", ReportPath }, null);

            Assert.That(code, Is.EqualTo(SizeEstimate.ExitOk));
            Assert.That(File.ReadAllText(ReportPath), Does.Contain("| ambiente | amb_* | 3 | ASTC 4×4 | 256 × 256 | 64,0 | 0,19 |"));
            Assert.That(AssetDatabase.IsValidFolder(SizeEstimate.TempFolder), Is.False);
            Assert.That(Directory.Exists(SizeEstimate.TempFolder), Is.False);
            Assert.That(File.Exists(SizeEstimate.TempFolder + ".meta"), Is.False);
        }

        private SizeEstimate.Measurement Fake(int width, int height, SizeEstimate.AstcBlock block)
        {
            calls++;
            return new SizeEstimate.Measurement(SizeEstimate.AstcBytes(width, height, block), "prueba");
        }

        private static void Delete()
        {
            if (File.Exists(ReportPath))
            {
                File.Delete(ReportPath);
            }
        }
    }
}
