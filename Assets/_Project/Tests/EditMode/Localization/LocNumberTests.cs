#nullable enable
using AF.Localization;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Cifras con la tabla propia de separadores por región (L2a; D-018, Q8-A; propuesta de idiomas §3.6): Colombia y Brasil
    /// «1.000», México y Perú «1,000», francés con U+00A0 (nunca U+202F); un español sin región, es-CO [P]. Sin depender de
    /// la cultura del equipo y sin asignaciones en <c>WriteNumber</c>.
    /// </summary>
    public sealed class LocNumberTests
    {
        private static string s_sink = string.Empty;
        private static int s_end;

        [TestCase("es-CO", "1.234.567", "1.234,5")]
        [TestCase("es-AR", "1.234.567", "1.234,5")]
        [TestCase("es-ES", "1.234.567", "1.234,5")]
        [TestCase("pt-BR", "1.234.567", "1.234,5")]
        [TestCase("de", "1.234.567", "1.234,5")]
        [TestCase("tr", "1.234.567", "1.234,5")]
        [TestCase("es-419", "1,234,567", "1,234.5")]
        [TestCase("es-MX", "1,234,567", "1,234.5")]
        [TestCase("es-PE", "1,234,567", "1,234.5")]
        [TestCase("en", "1,234,567", "1,234.5")]
        [TestCase("ja", "1,234,567", "1,234.5")]
        [TestCase("fr", "1\u00A0234\u00A0567", "1\u00A0234,5")]
        [TestCase("ru", "1\u00A0234\u00A0567", "1\u00A0234,5")]
        [TestCase("pt-PT", "1\u00A0234\u00A0567", "1\u00A0234,5")]
        public void Table_GivesTheSeparatorsOfEachRegion(string tag, string integer, string real)
        {
            LocNumberFormat format = LocNumber.For(tag);
            Assert.That(format.Tag, Is.EqualTo(tag));
            Assert.That(LocNumber.Format(1234567L, true, format), Is.EqualTo(integer));
            Assert.That(LocNumber.Format(1234.5, 1, format), Is.EqualTo(real));
        }

        [TestCase(null, "en")]
        [TestCase("", "en")]
        [TestCase("es", "es-CO")]
        [TestCase("es-CL", "es-CO")]
        [TestCase("ES_mx", "es-MX")]
        [TestCase("es-419", "es-419")]
        [TestCase("en-US", "en")]
        [TestCase("en-XA", "en")]
        [TestCase("pt", "pt-BR")]
        [TestCase("pt-AO", "pt-BR")]
        [TestCase("pt_PT", "pt-PT")]
        [TestCase("fr-CA", "fr")]
        [TestCase("de-DE", "de")]
        [TestCase("it-IT", "en")]
        [TestCase("zh-Hant-TW", "en")]
        [TestCase("es-CO-u-nu-latn", "es-CO")]
        public void For_ExactRegion_ThenLanguage_ThenEnglish(string? tag, string expected)
        {
            Assert.That(LocNumber.For(tag).Tag, Is.EqualTo(expected));
        }

        /// <summary>
        /// Todas las regiones de español de ICU, como las escribe ICU 72.1 desde .NET 10.0.13 (Windows 11, 2026-10-14; tarea L1,
        /// pendiente de la revisión de L2a): las que no escriben como es-CO tienen su fila. Antes, es-US, es-GT, es-HN, es-NI,
        /// es-PA, es-SV, es-DO y es-PR caían en es-CO y salía «1.000» donde el teléfono escribe «1,000».
        /// </summary>
        [TestCase("es-US", "es-US", "1,234,567", "1,234.5")]
        [TestCase("es-GT", "es-GT", "1,234,567", "1,234.5")]
        [TestCase("es-HN", "es-HN", "1,234,567", "1,234.5")]
        [TestCase("es-NI", "es-NI", "1,234,567", "1,234.5")]
        [TestCase("es-PA", "es-PA", "1,234,567", "1,234.5")]
        [TestCase("es-SV", "es-SV", "1,234,567", "1,234.5")]
        [TestCase("es-DO", "es-DO", "1,234,567", "1,234.5")]
        [TestCase("es-PR", "es-PR", "1,234,567", "1,234.5")]
        [TestCase("es_us", "es-US", "1,234,567", "1,234.5")]
        [TestCase("es-CU", "es-CU", "1,234,567", "1,234.5")]
        [TestCase("es-BZ", "es-BZ", "1,234,567", "1,234.5")]
        [TestCase("es-MX", "es-MX", "1,234,567", "1,234.5")]
        [TestCase("es-PE", "es-PE", "1,234,567", "1,234.5")]
        [TestCase("es-419", "es-419", "1,234,567", "1,234.5")]
        [TestCase("es-CR", "es-CR", "1\u00A0234\u00A0567", "1\u00A0234,5")]
        [TestCase("es-CO", "es-CO", "1.234.567", "1.234,5")]
        [TestCase("es-AR", "es-AR", "1.234.567", "1.234,5")]
        [TestCase("es-ES", "es-ES", "1.234.567", "1.234,5")]
        [TestCase("es-BO", "es-CO", "1.234.567", "1.234,5")]
        [TestCase("es-CL", "es-CO", "1.234.567", "1.234,5")]
        [TestCase("es-EC", "es-CO", "1.234.567", "1.234,5")]
        [TestCase("es-GQ", "es-CO", "1.234.567", "1.234,5")]
        [TestCase("es-PH", "es-CO", "1.234.567", "1.234,5")]
        [TestCase("es-PY", "es-CO", "1.234.567", "1.234,5")]
        [TestCase("es-UY", "es-CO", "1.234.567", "1.234,5")]
        [TestCase("es-VE", "es-CO", "1.234.567", "1.234,5")]
        [TestCase("es-BR", "es-CO", "1.234.567", "1.234,5")] // ICU escribe «1,234.5»; el dueño fija el punto (observación 22 de la jornada 6)
        public void SpanishRegions_AsIcuWritesThem(string tag, string row, string integer, string real)
        {
            LocNumberFormat format = LocNumber.For(tag);
            Assert.That(format.Tag, Is.EqualTo(row), tag);
            Assert.That(LocNumber.Format(1234567L, true, format), Is.EqualTo(integer), tag);
            Assert.That(LocNumber.Format(1234.5, 1, format), Is.EqualTo(real), tag);
        }

        [Test]
        public void TableTags_AreUnique()
        {
            var seen = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (LocNumberFormat format in LocNumber.Formats)
            {
                Assert.That(seen.Add(format.Tag), Is.True, "fila repetida: " + format.Tag);
            }
        }

        [Test]
        public void TheFiveRegionsOfTheTask_EsCoEsMxPtBrEnFr()
        {
            Assert.That(LocNumber.Format(1000L, true, LocNumber.For("es-CO")), Is.EqualTo("1.000"), "lo de hoy (glosario §10)");
            Assert.That(LocNumber.Format(1000L, true, LocNumber.For("es-MX")), Is.EqualTo("1,000"));
            Assert.That(LocNumber.Format(5200L, true, LocNumber.For("pt-BR")), Is.EqualTo("5.200"));
            Assert.That(LocNumber.Format(5200L, true, LocNumber.For("en-US")), Is.EqualTo("5,200"));
            string french = LocNumber.Format(12345.5, 1, LocNumber.For("fr-FR"));
            Assert.That(french, Is.EqualTo("12\u00A0345,5"));
            Assert.That(french.IndexOf('\u202F'), Is.EqualTo(-1), "U+202F se cambia por U+00A0 (Q9-A)");
            Assert.That(LocNumber.Characters.IndexOf('\u202F'), Is.EqualTo(-1));
            Assert.That(LocNumber.Characters.IndexOf(LocNumber.NoBreakSpace), Is.GreaterThanOrEqualTo(0));
        }

        [TestCase(0L, false, "0")]
        [TestCase(7L, false, "7")]
        [TestCase(1240L, false, "1240")]
        [TestCase(999L, true, "999")]
        [TestCase(1000L, true, "1.000")]
        [TestCase(36000L, true, "36.000")]
        [TestCase(-1234L, true, "-1.234")]
        [TestCase(long.MaxValue, true, "9.223.372.036.854.775.807")]
        [TestCase(long.MinValue, true, "-9.223.372.036.854.775.808")]
        public void Write_FromAPosition_ReturnsTheNextOne(long value, bool grouped, string expected)
        {
            var buffer = new char[40];
            buffer[0] = '#';
            int end = LocNumber.Write(buffer, 1, value, grouped, LocNumber.For("es-CO"));
            Assert.That(new string(buffer, 1, end - 1), Is.EqualTo(expected));
            Assert.That(buffer[0], Is.EqualTo('#'), "no escribe antes de la posición pedida");
            Assert.That(LocNumber.Length(value, grouped), Is.EqualTo(expected.Length));
        }

        [TestCase(2.675, 2, "2,68")]
        [TestCase(0.5, 0, "1")]
        [TestCase(1234.5, 0, "1.235")]
        [TestCase(-0.004, 2, "0,00")]
        [TestCase(-1.25, 1, "-1,3")]
        [TestCase(5.2, 1, "5,2")]
        [TestCase(1000000.25, 2, "1.000.000,25")]
        public void Decimals_RoundHalfAwayFromZero(double value, int decimals, string expected)
        {
            Assert.That(LocNumber.Format(value, decimals, LocNumber.For("es-CO")), Is.EqualTo(expected));
        }

        [Test]
        public void Write_TooSmallABuffer_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => LocNumber.Write(new char[3], 0, 1000L, true, LocNumber.For("es-CO")));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => LocNumber.Write(new char[8], 6, 1000L, false, LocNumber.For("es-CO")));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => LocNumber.Format(1.5, 10, LocNumber.For("es-CO")));
            Assert.Throws<System.ArgumentException>(() => LocNumber.Format(double.NaN, 1, LocNumber.For("es-CO")));
        }

        [Test]
        public void WriteNumber_DoesNotAllocate()
        {
            var buffer = new char[32];
            LocNumberFormat format = LocNumber.For("es-MX");
            TestDelegate write = () => { s_end = LocNumber.Write(buffer, 0, 1234567L, true, format); };
            TestDelegate writeReal = () => { s_end = LocNumber.Write(buffer, 0, 1234.56, 2, true, format); };
            write();
            writeReal();
            Assert.That(write, Is.Not.AllocatingGCMemory());
            Assert.That(writeReal, Is.Not.AllocatingGCMemory());
            Assert.That(new string(buffer, 0, s_end), Is.EqualTo("1,234.56"));
        }

        [Test]
        public void Formats_DoNotDependOnTheMachineCulture()
        {
            System.Globalization.CultureInfo previous = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
                s_sink = LocNumber.Format(1234.5, 1, LocNumber.For("es-CO"));
                Assert.That(s_sink, Is.EqualTo("1.234,5"));
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
                Assert.That(LocNumber.Format(1234.5, 1, LocNumber.For("es-MX")), Is.EqualTo("1,234.5"));
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = previous;
            }
        }
    }
}
