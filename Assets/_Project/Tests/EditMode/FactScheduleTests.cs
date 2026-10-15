#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Text;
using AF.GameFlow;
using AF.Levels;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Línea de Frutas del río de cada nivel y lo revelado (D-017; 08 §4.6; propuesta de Frutas §6.4; tarea F1F2), con
    /// <see cref="FactSchedule"/> sobre el calendario real y sobre uno de prueba con step 2: las claves de la pregunta 49 (a)
    /// (<c>fact.&lt;tinte&gt;.&lt;kk&gt;</c>), los bordes de bloque, los niveles sin línea (fuera de toda serie y entre dos líneas
    /// con step &gt; 1), k/20 y el sello, el revelado por <c>progress.max_level</c> y por el Salto del Boto
    /// (<c>skipped_levels</c>), y que ninguna consulta asigna memoria.
    /// </summary>
    public sealed class FactScheduleTests
    {
        private static FactSchedule? s_real;
        private static string? s_sink;
        private static int s_count;

        private static FactSchedule Real => s_real ??= new FactSchedule(FactCatalogReader.Read(
            File.ReadAllText(LocTables.ProjectPath(FactCatalogTests.CalendarAssetPath), Encoding.UTF8)));

        // acai 1-3 (step 1), un hueco y urucum desde el 10, una línea cada dos niveles: 10, 12 y 14.
        private static FactSchedule Stepped => new FactSchedule(FactCatalogReader.Read(
            "{\"format\": \"af-facts/1\", \"version\": 1, \"max_chars_es\": 90, \"series\": ["
            + "{\"subject\": \"acai\", \"kind\": \"tint\", \"from_level\": 1, \"lines\": 3, \"step\": 1}, "
            + "{\"subject\": \"urucum\", \"kind\": \"tint\", \"from_level\": 10, \"lines\": 3, \"step\": 2}], "
            + "\"sources\": {\"A1\": {\"title\": \"t\", \"url\": \"https://example.org\"}}}"));

        [Test]
        public void KeyFor_GivesTheKeysOfQuestion49_AndNullOutsideTheSeries()
        {
            FactSchedule s = Real;
            Assert.That(s.KeyFor(3), Is.EqualTo("fact.acai.03"), "el açaí abre el juego (D-017, Q3-A)");
            Assert.That(s.KeyFor(21), Is.EqualTo("fact.agua_tapajos.01"), "en el 21 empieza el Agua del Tapajós (propuesta §3.1, regla 2)");
            Assert.That(s.KeyFor(261), Is.Null, "261-300 llegan con C1a (F1c)");
            Assert.That(s.KeyFor(300), Is.Null);
            Assert.That(s.KeyFor(0), Is.Null);
            Assert.That(s.KeyFor(-5), Is.Null);
            Assert.That(s.KeyFor(321), Is.Null, "la versión 1 acaba en el 320");
            Assert.That(s.KeyFor(1000), Is.Null);
            Assert.That(s.KeyFor(int.MaxValue), Is.Null);
            Assert.That(s.FactIdFor(3), Is.EqualTo("acai.03"), "fact_id de af_river_fact_shown (08 §8.2)");
            Assert.That(s.FactIdFor(261), Is.Null);
        }

        [TestCase(1, "fact.acai.01")]
        [TestCase(20, "fact.acai.20")]
        [TestCase(21, "fact.agua_tapajos.01")]
        [TestCase(40, "fact.agua_tapajos.20")]
        [TestCase(41, "fact.urucum.01")]
        [TestCase(61, "fact.agua_negro.01")]
        [TestCase(100, "fact.cupuacu.20")]
        [TestCase(101, "fact.aguaje.01")]
        [TestCase(141, "fact.jenipapo.01")]
        [TestCase(200, "fact.tapereba.20")]
        [TestCase(201, "fact.cacao.01")]
        [TestCase(220, "fact.cacao.20")]
        [TestCase(221, "fact.lulo.01")]
        [TestCase(260, "fact.guanabana.20")]
        [TestCase(301, "fact.araza.01")]
        [TestCase(320, "fact.araza.20")]
        public void BlockBorders_GiveTheFirstAndLastLine(int level, string key)
        {
            Assert.That(Real.KeyFor(level), Is.EqualTo(key));
            Assert.That(Real.LevelOf(key), Is.EqualTo(level), "y la clave vuelve a su nivel");
        }

        [Test]
        public void Progress_GivesKOfTwenty_TheSealAtTheLast_AndTheNameKey()
        {
            FactPosition p = Real.Progress(3);
            Assert.That(p.IsValid, Is.True);
            Assert.That(p.Series!.Subject, Is.EqualTo("acai"));
            Assert.That(p.K, Is.EqualTo(3));
            Assert.That(p.Lines, Is.EqualTo(20), "«Açaí · 3/20»");
            Assert.That(p.IsLast, Is.False);
            Assert.That(p.Key, Is.EqualTo("fact.acai.03"));
            Assert.That(p.FactId, Is.EqualTo("acai.03"));
            Assert.That(p.NameKey, Is.EqualTo("tint.acai.name"));

            FactPosition last = Real.Progress(160);
            Assert.That(last.Series!.Subject, Is.EqualTo("jenipapo"));
            Assert.That(last.K, Is.EqualTo(20));
            Assert.That(last.IsLast, Is.True, "la línea 20: el contador pasa a sello (D-017, Q5-A)");

            FactPosition none = Real.Progress(280);
            Assert.That(none.IsValid, Is.False);
            Assert.That(none.K, Is.EqualTo(0));
            Assert.That(none.Lines, Is.EqualTo(0));
            Assert.That(none.IsLast, Is.False);
            Assert.That(none.Key, Is.Null);
            Assert.That(none.FactId, Is.Null);
            Assert.That(none.NameKey, Is.Null);
        }

        [Test]
        public void Step_SkipsTheLevelsBetweenTwoLines_AndTheGapHasNoLine()
        {
            FactSchedule s = Stepped;
            Assert.That(s.KeyFor(3), Is.EqualTo("fact.acai.03"));
            Assert.That(s.KeyFor(4), Is.Null, "hueco entre series");
            Assert.That(s.KeyFor(9), Is.Null);
            Assert.That(s.KeyFor(10), Is.EqualTo("fact.urucum.01"));
            Assert.That(s.KeyFor(11), Is.Null, "d = 1 no es múltiplo de step = 2: sin línea (§6.4, paso 2)");
            Assert.That(s.KeyFor(12), Is.EqualTo("fact.urucum.02"));
            Assert.That(s.KeyFor(13), Is.Null);
            Assert.That(s.KeyFor(14), Is.EqualTo("fact.urucum.03"));
            Assert.That(s.KeyFor(15), Is.Null, "el bloque acaba en from_level + lines × step = 16");
            Assert.That(s.KeyFor(16), Is.Null);
            Assert.That(s.Progress(14).IsLast, Is.True);
            Assert.That(s.LevelOf("fact.urucum.03"), Is.EqualTo(14));
        }

        [Test]
        public void IsRevealed_ByMaxLevel_OrBySkipBoto_AndNeverAnUnknownKey()
        {
            FactSchedule s = Real;
            Assert.That(s.IsRevealed("fact.acai.05", 4, null), Is.False, "el 5 aún no se superó");
            Assert.That(s.IsRevealed("fact.acai.05", 5, null), Is.True, "superado el 5");
            Assert.That(s.IsRevealed("fact.acai.01", 0, null), Is.False, "perfil nuevo");
            var skipped = new List<int> { 7, 33 };
            Assert.That(s.IsRevealed("fact.acai.07", 4, skipped), Is.True, "el Salto del Boto revela la línea sin victoria (04 §2.6)");
            Assert.That(s.IsRevealed("fact.agua_tapajos.13", 4, skipped), Is.True, "el 33 saltado");
            Assert.That(s.IsRevealed("fact.acai.06", 4, skipped), Is.False);
            Assert.That(s.IsRevealed("fact.acai.06", 4, new List<int>()), Is.False);
            Assert.That(s.IsRevealed("fact.acai.tag", 1000, skipped), Is.False, "la frase de «Fruta nueva» no es una línea del calendario");
            Assert.That(s.IsRevealed("fact.lulo.21", 1000, null), Is.False);
            Assert.That(s.LevelOf("tint.acai.name"), Is.EqualTo(0));
            Assert.That(FactSchedule.IsLevelRevealed(0, 1000, skipped), Is.False);
        }

        [Test]
        public void Queries_DoNotAllocate()
        {
            FactSchedule s = Real;
            var skipped = new List<int> { 7, 33, 150 };
            IReadOnlyList<int> list = skipped;
            s_sink = s.KeyFor(1); // el primer uso no cuenta (JIT)
            TestDelegate call = () =>
            {
                int count = 0;
                for (int level = -1; level <= 340; level++)
                {
                    s_sink = s.KeyFor(level);
                    s_sink = s.FactIdFor(level);
                    FactPosition p = s.Progress(level);
                    count += p.K + (p.IsLast ? 1 : 0);
                    s_sink = p.Key;
                    s_sink = p.NameKey;
                }

                count += s.IsRevealed("fact.acai.07", 4, list) ? 1 : 0;
                count += s.IsRevealed("fact.jenipapo.10", 100, list) ? 1 : 0;
                count += s.IsRevealed("fact.acai.tag", 100, null) ? 1 : 0;
                count += s.LevelOf("fact.araza.20");
                s_count = count;
            };
            call();
            Assert.That(call, Is.Not.AllocatingGCMemory(), "sin asignaciones por consulta (propuesta §6.4; arquitectura de la tanda 4)");
            Assert.That(s_count, Is.GreaterThan(0));
        }

        [Test]
        public void Schedule_RejectsANullCatalog()
        {
            Assert.That(() => new FactSchedule(null!), NUnit.Framework.Throws.ArgumentNullException);
        }
    }
}
