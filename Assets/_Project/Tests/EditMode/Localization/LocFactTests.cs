#nullable enable
using System;
using System.Reflection;
using AF.Localization;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// La fachada <see cref="Loc"/> (L2a; D-018; 08 §2.2 y §4.7; arquitectura de la tanda 4, §1): <c>Fact</c> da «» si falta la
    /// clave, si no hay texto en el idioma (nunca mezcla idiomas) o si no está aprobado en release; <c>T</c> da «[clave]» en
    /// desarrollo y «» en release si falta; cifras con la región (es-CO por defecto, la del teléfono si se da); en-XA solo en
    /// desarrollo; <c>T(key)</c>, <c>Fact(key)</c> y <c>WriteNumber</c> sin asignaciones; el ensamblado, sin motor.
    /// </summary>
    public sealed class LocFactTests
    {
        private static string s_sink = string.Empty;
        private static int s_end;
        private LocStateSnapshot _before;

        [SetUp]
        public void SetUp()
        {
            _before = LocStateSnapshot.Take();
        }

        [TearDown]
        public void TearDown()
        {
            _before.Restore();
        }

        [Test]
        public void Fact_ApprovedInSpanish_ShowsInRelease_WithItsArgsInTheRegionFormat()
        {
            Loc.Configure(LocSamples.Catalog(), "es-419", false);
            Assert.That(Loc.NumberFormat.Tag, Is.EqualTo("es-CO"), "sin región del teléfono, es-CO [P]");
            Assert.That(Loc.Fact("fact.acai.01"), Is.EqualTo("El açaí da 5.200 frutos por racimo."));
            Assert.That(Loc.Fact("fact.acai.04"), Is.EqualTo("Mide 12,5 metros."));

            Loc.SetNumberTag("es-MX");
            Assert.That(Loc.Fact("fact.acai.01"), Is.EqualTo("El açaí da 5,200 frutos por racimo."), "Q8-A: la región del teléfono");
            Assert.That(Loc.Fact("fact.acai.04"), Is.EqualTo("Mide 12.5 metros."));
            Assert.That(Loc.Fact("fact.acai.01", LocArgs.Of("n", 7)), Is.EqualTo("El açaí da 7 frutos por racimo."), "los del código mandan");
        }

        [Test]
        public void Fact_Unapproved_IsHiddenInRelease_AndShownInDevelopment()
        {
            Loc.Configure(LocSamples.Catalog(), "es-419", false);
            Assert.That(Loc.Fact("fact.acai.02"), Is.EqualTo(string.Empty), "pendiente_asesor no llega a release");
            Assert.That(Loc.Has("fact.acai.02"), Is.False);
            Assert.That(Loc.Fact("fact.acai.02", LocArgs.Of("n", 1)), Is.EqualTo(string.Empty));

            Loc.Configure(LocSamples.Catalog(), "es-419", true);
            Assert.That(Loc.Fact("fact.acai.02"), Is.EqualTo("Dato en borrador."));
            Assert.That(Loc.Has("fact.acai.02"), Is.True);
        }

        [Test]
        public void Fact_MissingKey_IsEmpty_InReleaseAndInDevelopment()
        {
            Loc.Configure(LocSamples.Catalog(), "es-419", true);
            Assert.That(Loc.Fact("fact.acai.99"), Is.EqualTo(string.Empty));
            Assert.That(Loc.Fact("next"), Is.EqualTo(string.Empty), "las claves de UI no son textos largos");
            Loc.Configure(LocSamples.Catalog(), "es-419", false);
            Assert.That(Loc.Fact("fact.acai.99"), Is.EqualTo(string.Empty));
        }

        [Test]
        public void Fact_Translation_NeedsNativeReview_AndNeverFallsBackToSpanish()
        {
            Loc.Configure(LocSamples.Catalog(), "en", false);
            Assert.That(Loc.NumberFormat.Tag, Is.EqualTo("en"));
            Assert.That(Loc.Fact("fact.acai.01"), Is.EqualTo("Açaí gives 5,200 fruits per bunch."), "aprobado_es + revisado_nativo");
            Assert.That(Loc.Fact("fact.acai.02"), Is.EqualTo(string.Empty), "revisado, pero el español sigue pendiente del asesor");
            Assert.That(Loc.Fact("fact.acai.04"), Is.EqualTo(string.Empty), "traducido sin revisión nativa (Q5-A)");
            Assert.That(Loc.Fact("fact.acai.03"), Is.EqualTo(string.Empty), "sin traducción: no sale (Q10-A)");

            Loc.Configure(LocSamples.Catalog(), "en", true);
            Assert.That(Loc.Fact("fact.acai.04"), Is.EqualTo("It is 12.5 metres tall."), "en desarrollo se ve lo no aprobado");
            Assert.That(Loc.Fact("fact.acai.03"), Is.EqualTo(string.Empty), "pero nunca en otro idioma");
            Assert.That(Loc.Fact("fauna_saimiri_collinsi_dato"), Is.EqualTo(string.Empty));
        }

        [Test]
        public void T_MissingKey_IsBracketedInDevelopment_AndEmptyInRelease()
        {
            Loc.Configure(LocSamples.Catalog(), "es-419", true);
            Assert.That(Loc.T("next"), Is.EqualTo("Siguiente"));
            Assert.That(Loc.T("no_such_key"), Is.EqualTo("[no_such_key]"));
            Assert.That(Loc.T("no_such_key", LocArgs.Of("n", 1)), Is.EqualTo("[no_such_key]"));
            Assert.That(Loc.T("fact.acai.01"), Is.EqualTo("[fact.acai.01]"), "los textos largos no son UI");

            Loc.Configure(LocSamples.Catalog(), "es-419", false);
            Assert.That(Loc.T("no_such_key"), Is.EqualTo(string.Empty));
            Assert.That(Loc.T("next"), Is.EqualTo("Siguiente"), "la UI no depende del estado");

            Loc.Configure(LocSamples.Catalog(), "pt-BR", true);
            Assert.That(Loc.T("level_n"), Is.EqualTo("[level_n]"), "una traducción que falta cuenta como clave que falta");
            Assert.That(Loc.T("next"), Is.EqualTo("Próximo"));
        }

        [Test]
        public void T_WithArgs_PluralsAndSeparators()
        {
            Loc.Configure(LocSamples.Catalog(), "es-419", true);
            Assert.That(Loc.T("stuck_rescue_missing", LocArgs.Of("n", 1)), Is.EqualTo("Te falta 1 semilla"), "nunca «Te faltan 1 semillas»");
            Assert.That(Loc.T("stuck_rescue_missing", LocArgs.Of("n", 1500)), Is.EqualTo("Te faltan 1.500 semillas"));
            Assert.That(Loc.T("level_n", LocArgs.OfDigits("level", 12)), Is.EqualTo("Nivel 12"));
            Assert.That(Loc.T("world_banner", LocArgs.OfDigits("n", 1), " · ", 0b10), Is.EqualTo("Mundo 1: Açaí · Brasil\nAmazonas"));
            Assert.That(LocFormat.SeparatorCount(Loc.Template("world_banner")!), Is.EqualTo(2));
            Assert.That(Loc.Template("no_such_key"), Is.Null);

            Loc.Configure(LocSamples.Catalog(), "en", true, "en-US");
            Assert.That(Loc.T("stuck_rescue_missing", LocArgs.Of("n", 1500)), Is.EqualTo("You need 1,500 more seeds"));
            Assert.That(Loc.T("stuck_rescue_missing", LocArgs.Of("n", 1)), Is.EqualTo("You need 1 more seed"));
        }

        [Test]
        public void Pseudo_OnlyInDevelopment_AndBracketsEverything()
        {
            Assert.Throws<ArgumentException>(() => Loc.Configure(LocSamples.Catalog(), "en-XA", false));
            Loc.Configure(LocSamples.Catalog(), "en-XA", true);
            Assert.That(Loc.Locale, Is.EqualTo("en-XA"));
            Assert.That(Loc.T("next"), Is.EqualTo("[Sígúíéñté ···]"));
            Assert.That(Loc.T("level_n", LocArgs.OfDigits("level", 12)), Is.EqualTo("[Ñívél 12 ··]"));
            Assert.That(Loc.T("stuck_rescue_missing", LocArgs.Of("n", 1)), Does.StartWith("[Té fáltá 1 sémíllá"));
            Assert.That(Loc.Fact("fact.acai.01"), Does.StartWith("[Él áçáí dá 5.200 "));
            Assert.That(Loc.StatusOf("next", "en-XA"), Is.EqualTo(LocStatus.ApprovedEs), "el estado del español");
        }

        [Test]
        public void SetLocale_ValidatesAndRaisesChangedOnlyWhenItChanges()
        {
            Loc.Configure(LocSamples.Catalog(), "es-419", false);
            int changes = 0;
            Action count = () => changes++;
            Loc.Changed += count;
            try
            {
                Loc.SetLocale("es-419");
                Assert.That(changes, Is.EqualTo(0), "mismo idioma: nada");
                Loc.SetLocale("en");
                Assert.That(changes, Is.EqualTo(1));
                Assert.That(Loc.T("next"), Is.EqualTo("Next"));
                Assert.That(Loc.NumberFormat.Tag, Is.EqualTo("en"), "sin región pedida, las cifras siguen al idioma");
                Loc.SetNumberTag("es-CO");
                Assert.That(changes, Is.EqualTo(2));
                Loc.SetNumberTag("es-CO");
                Assert.That(changes, Is.EqualTo(2), "misma región: nada");
                Loc.SetNumberTag("es-AR");
                Assert.That(changes, Is.EqualTo(2), "otra etiqueta con los mismos separadores: nada");
                Assert.That(Loc.RequestedNumberTag, Is.EqualTo("es-AR"));
                Loc.Configure(LocSamples.Catalog(), "es-419", false);
                Assert.That(changes, Is.EqualTo(3), "configurar también avisa");

                Assert.Throws<ArgumentException>(() => Loc.SetLocale("fr"));
                Assert.Throws<ArgumentException>(() => Loc.SetLocale("en-XA"), "en-XA no vale en release");
                Assert.That(changes, Is.EqualTo(3));
            }
            finally
            {
                Loc.Changed -= count;
            }
        }

        [Test]
        public void Has_StatusOf_IsComplete_AndNumbers()
        {
            Loc.Configure(LocSamples.Catalog(), "es-419", false, "es-MX");
            Assert.That(Loc.Has("next"), Is.True);
            Assert.That(Loc.Has("fact.acai.01"), Is.True);
            Assert.That(Loc.Has("no_such_key"), Is.False);
            Assert.That(Loc.StatusOf("level_complete", "pt-BR"), Is.EqualTo(LocStatus.Translated));
            Assert.That(Loc.StatusOf("fact.acai.02", "es-419"), Is.EqualTo(LocStatus.PendingAdvisor));
            Assert.That(Loc.StatusOf("fact.acai.03", "en"), Is.EqualTo(LocStatus.Missing));
            Assert.That(Loc.StatusOf("no_such_key", "es-419"), Is.EqualTo(LocStatus.Missing));
            Assert.That(Loc.IsComplete("es-419"), Is.True);
            Assert.That(Loc.IsComplete("en"), Is.True);
            Assert.That(Loc.IsComplete("pt-BR"), Is.False);
            Assert.That(Loc.Number(1234567), Is.EqualTo("1,234,567"));
            Assert.That(Loc.Number(5.25, 1), Is.EqualTo("5.3"));
            var buffer = new char[16];
            int end = Loc.WriteNumber(buffer, 0, 36000, true);
            Assert.That(new string(buffer, 0, end), Is.EqualTo("36,000"));
        }

        [Test]
        public void Reset_LeavesAnEmptyCatalogInDevelopmentMode()
        {
            Loc.Reset();
            Assert.That(Loc.IsConfigured, Is.False);
            Assert.That(Loc.Locale, Is.EqualTo("es-419"));
            Assert.That(Loc.T("next"), Is.EqualTo("[next]"));
            Assert.That(Loc.Fact("fact.acai.01"), Is.EqualTo(string.Empty));
            Assert.That(Loc.Number(1000), Is.EqualTo("1.000"));
        }

        [Test]
        public void T_Fact_AndWriteNumber_DoNotAllocate()
        {
            Loc.Configure(LocSamples.Catalog(), "es-419", false);
            var buffer = new char[16];
            TestDelegate t = () => { s_sink = Loc.T("next"); };
            TestDelegate title = () => { s_sink = Loc.T("level_complete"); };
            TestDelegate fact = () => { s_sink = Loc.Fact("fact.acai.01"); };
            TestDelegate hidden = () => { s_sink = Loc.Fact("fact.acai.02"); };
            TestDelegate number = () => { s_end = Loc.WriteNumber(buffer, 0, 1234567, true); };
            t();
            title();
            fact();
            hidden();
            number();
            Assert.That(t, Is.Not.AllocatingGCMemory());
            Assert.That(title, Is.Not.AllocatingGCMemory());
            Assert.That(fact, Is.Not.AllocatingGCMemory(), "con su cifra de args, ya formateada al configurar");
            Assert.That(hidden, Is.Not.AllocatingGCMemory());
            Assert.That(number, Is.Not.AllocatingGCMemory());
            Assert.That(s_end, Is.EqualTo(9));
        }

        [Test]
        public void Assembly_HasNoEngineReferences()
        {
            AssemblyName[] references = typeof(Loc).Assembly.GetReferencedAssemblies();
            for (int i = 0; i < references.Length; i++)
            {
                Assert.That(references[i].Name, Does.Not.StartWith("UnityEngine").And.Not.StartWith("UnityEditor").And.Not.StartWith("AF."),
                    "AF.Localization va sin motor y sin referencias (08 §2.2)");
            }

            Assert.That(typeof(Loc).Assembly.GetName().Name, Is.EqualTo("AF.Localization"));
        }
    }
}
