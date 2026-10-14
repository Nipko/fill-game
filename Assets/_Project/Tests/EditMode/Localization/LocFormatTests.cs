#nullable enable
using System.Collections.Generic;
using AF.Localization;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Plantillas de las tablas (L2a; contrato 32; propuesta de idiomas §3.2, reglas 2 y 3, y §3.6): marcadores con nombre,
    /// plurales <c>{n:plural:a|b[|c]}</c> con las reglas CLDR de es-419, pt-BR y en (n = 0, 1, 2, 5, 21, 1.000 y 1.000.000;
    /// nunca <c>n == 1</c>) y el marcador de corte <c>{sep}</c>.
    /// </summary>
    public sealed class LocFormatTests
    {
        private const string Es2 = "{n:plural:{} semilla|{} semillas}";
        private const string Es3 = "{n:plural:{} semilla|{} de semillas|{} semillas}";
        private const string Pt3 = "{n:plural:{} semente|{} de sementes|{} sementes}";
        private const string En2 = "{n:plural:{} seed|{} seeds}";
        private const string En3 = "{n:plural:{} seed|{} of seeds|{} seeds}";

        private static readonly LocNumberFormat EsCo = LocNumber.For("es-CO");
        private static readonly LocNumberFormat PtBr = LocNumber.For("pt-BR");
        private static readonly LocNumberFormat En = LocNumber.For("en");

        [TestCase(0L, "0 semillas", "0 semillas")]
        [TestCase(1L, "1 semilla", "1 semilla")]
        [TestCase(2L, "2 semillas", "2 semillas")]
        [TestCase(5L, "5 semillas", "5 semillas")]
        [TestCase(21L, "21 semillas", "21 semillas")]
        [TestCase(1000L, "1.000 semillas", "1.000 semillas")]
        [TestCase(1000000L, "1.000.000 semillas", "1.000.000 de semillas")]
        public void Plural_Es419_OneManyOther_ZeroIsOther(long n, string twoForms, string threeForms)
        {
            Assert.That(LocFormat.Format(Es2, LocArgs.Of("n", n), "es-419", EsCo), Is.EqualTo(twoForms), "con 2 formas, many usa other");
            Assert.That(LocFormat.Format(Es3, LocArgs.Of("n", n), "es-419", EsCo), Is.EqualTo(threeForms));
        }

        [TestCase(0L, "0 semente")]
        [TestCase(1L, "1 semente")]
        [TestCase(2L, "2 sementes")]
        [TestCase(5L, "5 sementes")]
        [TestCase(21L, "21 sementes")]
        [TestCase(1000L, "1.000 sementes")]
        [TestCase(1000000L, "1.000.000 de sementes")]
        public void Plural_PtBr_ZeroAndOneAreOne(long n, string expected)
        {
            Assert.That(LocFormat.Format(Pt3, LocArgs.Of("n", n), "pt-BR", PtBr), Is.EqualTo(expected));
        }

        [TestCase(0L, "0 seeds")]
        [TestCase(1L, "1 seed")]
        [TestCase(2L, "2 seeds")]
        [TestCase(5L, "5 seeds")]
        [TestCase(21L, "21 seeds")]
        [TestCase(1000L, "1,000 seeds")]
        [TestCase(1000000L, "1,000,000 seeds")]
        public void Plural_En_OneAndOther(long n, string expected)
        {
            Assert.That(LocFormat.Format(En2, LocArgs.Of("n", n), "en", En), Is.EqualTo(expected));
            Assert.That(LocFormat.Format(En3, LocArgs.Of("n", n), "en", En), Is.EqualTo(expected), "en no tiene many");
        }

        [Test]
        public void PluralCategories_FollowCldr_ForIntegersAndDecimals()
        {
            Assert.That(LocPlural.Category("es-419", 0), Is.EqualTo(LocPluralCategory.Other));
            Assert.That(LocPlural.Category("es-419", 1), Is.EqualTo(LocPluralCategory.One));
            Assert.That(LocPlural.Category("es-419", 2000000), Is.EqualTo(LocPluralCategory.Many));
            Assert.That(LocPlural.Category("es-419", 1000001), Is.EqualTo(LocPluralCategory.Other));
            Assert.That(LocPlural.Category("es-419", -1), Is.EqualTo(LocPluralCategory.One), "CLDR usa el valor absoluto");
            Assert.That(LocPlural.Category("en-XA", 0), Is.EqualTo(LocPluralCategory.Other), "en-XA es español falseado");
            Assert.That(LocPlural.Category("en-XA", 1000000), Is.EqualTo(LocPluralCategory.Many));
            Assert.That(LocPlural.Category("pt-BR", 0), Is.EqualTo(LocPluralCategory.One));
            Assert.That(LocPlural.Category("pt-PT", 0), Is.EqualTo(LocPluralCategory.Other), "en pt-PT el 0 es other");
            Assert.That(LocPlural.Category("en", 0), Is.EqualTo(LocPluralCategory.Other));
            Assert.That(LocPlural.Category("en", 1000000), Is.EqualTo(LocPluralCategory.Other));

            Assert.That(LocPlural.Category("es-419", 1.0, 1), Is.EqualTo(LocPluralCategory.One), "es: n = 1 también con «1,0»");
            Assert.That(LocPlural.Category("en", 1.0, 1), Is.EqualTo(LocPluralCategory.Other), "en: i = 1 and v = 0");
            Assert.That(LocPlural.Category("pt-BR", 0.5, 1), Is.EqualTo(LocPluralCategory.One), "pt: i = 0..1");
            Assert.That(LocPlural.Category("pt-BR", 1.5, 1), Is.EqualTo(LocPluralCategory.One));
            Assert.That(LocPlural.Category("pt-BR", 2.5, 1), Is.EqualTo(LocPluralCategory.Other));
            Assert.That(LocPlural.Category("es-419", 1000000.0, 1), Is.EqualTo(LocPluralCategory.Other), "many pide v = 0");

            Assert.That(LocPlural.FormIndex(LocPluralCategory.Many, 2), Is.EqualTo(1));
            Assert.That(LocPlural.FormIndex(LocPluralCategory.Many, 3), Is.EqualTo(1));
            Assert.That(LocPlural.FormIndex(LocPluralCategory.Other, 3), Is.EqualTo(2));
            Assert.That(LocPlural.FormIndex(LocPluralCategory.Other, 1), Is.EqualTo(0));
        }

        [Test]
        public void NamedMarkers_TakeTheirArgs_AndMissingOnesStayVisible()
        {
            Assert.That(LocFormat.Format("Jugar · Nivel {level}", LocArgs.OfDigits("level", 1000), "es-419", EsCo), Is.EqualTo("Jugar · Nivel 1000"));
            Assert.That(LocFormat.Format("Premio: {n}", LocArgs.Of("n", 36000), "es-419", EsCo), Is.EqualTo("Premio: 36.000"));
            Assert.That(LocFormat.Format("{place} · {n} km", LocArgs.Of("place", "Manaos").And("n", 1480), "es-419", EsCo),
                Is.EqualTo("Manaos · 1.480 km"));
            Assert.That(LocFormat.Format("Mide {m} m", LocArgs.Of("m", 12.5, 1), "es-419", EsCo), Is.EqualTo("Mide 12,5 m"));
            Assert.That(LocFormat.Format("Mide {m} m", LocArgs.Of("m", 12.5, 1), "es-419", LocNumber.For("es-MX")), Is.EqualTo("Mide 12.5 m"));
            Assert.That(LocFormat.Format("Hola {name}", LocArgs.None, "es-419", EsCo), Is.EqualTo("Hola {name}"), "sin argumento, se ve");
            Assert.That(LocFormat.Format("Hola {0}", LocArgs.Of("n", 1), "es-419", EsCo), Is.EqualTo("Hola {0}"), "nada de posicionales");
            Assert.That(LocFormat.Format("Suelto {} y }", LocArgs.Of("n", 1), "es-419", EsCo), Is.EqualTo("Suelto {} y }"));
            Assert.That(LocFormat.Format("<b>{name}</b>", LocArgs.Of("name", "Açaí"), "es-419", EsCo), Is.EqualTo("<b>Açaí</b>"));
        }

        [Test]
        public void Fallback_ArgsFillWhatTheCallerDoesNotGive()
        {
            LocArgs row = LocArgs.Of("n", 5200).And("total", 20);
            string text = LocFormat.Format("{n} de {total}", LocArgs.Of("n", 3), row, "es-419", EsCo, LocFormat.LineBreak, 0);
            Assert.That(text, Is.EqualTo("3 de 20"), "los del código mandan sobre los de la tabla");
        }

        [Test]
        public void Plural_CanUseOtherMarkersInsideItsForms()
        {
            const string template = "{n:plural:Faltan {} nivel para {place}|Faltan {} niveles para {place}}";
            LocArgs args = LocArgs.Of("n", 1).And("place", "Anavilhanas");
            Assert.That(LocFormat.Format(template, args, "es-419", EsCo), Is.EqualTo("Faltan 1 nivel para Anavilhanas"));
            Assert.That(LocFormat.Format(template, LocArgs.Of("n", 3).And("place", "Anavilhanas"), "es-419", EsCo),
                Is.EqualTo("Faltan 3 niveles para Anavilhanas"));
            Assert.That(LocFormat.Format("{n:plural:a|b}", LocArgs.Of("n", "x"), "es-419", EsCo), Is.EqualTo("{n:plural:a|b}"),
                "un texto no elige plural");
        }

        [Test]
        public void Sep_IsALineBreak_OrTheSeparator_OneByOne()
        {
            const string template = "Mundo {n}: Açaí{sep}Brasil{sep}Amazonas";
            LocArgs args = LocArgs.OfDigits("n", 1);
            Assert.That(LocFormat.SeparatorCount(template), Is.EqualTo(2));
            Assert.That(LocFormat.Format(template, args, "es-419", EsCo), Is.EqualTo("Mundo 1: Açaí\nBrasil\nAmazonas"), "sin indicación, salto");
            Assert.That(LocFormat.Format(template, args, "es-419", EsCo, " · ", 0), Is.EqualTo("Mundo 1: Açaí · Brasil · Amazonas"));
            Assert.That(LocFormat.Format(template, args, "es-419", EsCo, " · ", 0b01), Is.EqualTo("Mundo 1: Açaí\nBrasil · Amazonas"));
            Assert.That(LocFormat.Format(template, args, "es-419", EsCo, " · ", 0b10), Is.EqualTo("Mundo 1: Açaí · Brasil\nAmazonas"));
            Assert.That(LocFormat.SeparatorCount("Sin cortes"), Is.EqualTo(0));
        }

        [Test]
        public void Validate_RejectsPositionalUnclosedAndBadPlurals()
        {
            Assert.That(LocFormat.Validate("Te faltan {n} semillas"), Is.Null);
            Assert.That(LocFormat.Validate(Es3), Is.Null);
            Assert.That(LocFormat.Validate("Mundo {n}: {name}{sep}{country}"), Is.Null);
            Assert.That(LocFormat.Validate("<b>Hola</b>, sin marcadores"), Is.Null);

            Assert.That(LocFormat.Validate("Nivel {0}"), Does.Contain("posicionales"));
            Assert.That(LocFormat.Validate("Nivel {Level}"), Does.Contain("no válido"));
            Assert.That(LocFormat.Validate("Nivel {level"), Does.Contain("sin cerrar"));
            Assert.That(LocFormat.Validate("Nivel level}"), Does.Contain("cierre suelta"));
            Assert.That(LocFormat.Validate("Te faltan {} semillas"), Does.Contain("dentro de un plural"));
            Assert.That(LocFormat.Validate("{n:plural:semilla}"), Does.Contain("1 formas"));
            Assert.That(LocFormat.Validate("{n:plural:a|b|c|d}"), Does.Contain("4 formas"));
            Assert.That(LocFormat.Validate("{n:plural:a{sep}|b}"), Does.Contain("{sep}"));
            Assert.That(LocFormat.Validate("{n:plural:{0} a|b}"), Does.Contain("posicionales"), "también dentro de las formas");
            Assert.That(LocFormat.Validate("{n:singular:a|b}"), Does.Contain("mal escrito"));
        }

        [Test]
        public void CollectNames_ListsEveryNamedMarkerOnce()
        {
            var names = new List<string>();
            LocFormat.CollectNames("{n:plural:{} de {total}|{} de {total}}{sep}Nivel {level}, {n}", names);
            Assert.That(names, Is.EqualTo(new[] { "n", "total", "level" }));
        }

        [Test]
        public void TemplateWithoutMarkers_IsReturnedAsIs_WithoutAllocating()
        {
            const string plain = "¡Nivel completado!";
            Assert.That(object.ReferenceEquals(LocFormat.Format(plain, LocArgs.Of("n", 1), "es-419", EsCo), plain), Is.True);
            LocArgs args = LocArgs.Of("n", 1);
            TestDelegate call = () => { s_sink = LocFormat.Format(plain, args, "es-419", EsCo); };
            call();
            Assert.That(call, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void LocArgs_RejectBadNames_Duplicates_AndMoreThanThree()
        {
            Assert.Throws<System.ArgumentException>(() => LocArgs.Of("0", 1));
            Assert.Throws<System.ArgumentException>(() => LocArgs.Of("N", 1));
            Assert.Throws<System.ArgumentException>(() => LocArgs.Of("sep", 1));
            Assert.Throws<System.ArgumentException>(() => LocArgs.Of("n", 1).And("n", 2));
            Assert.Throws<System.InvalidOperationException>(() => LocArgs.Of("a", 1).And("b", 2).And("c", 3).And("d", 4));
            Assert.Throws<System.ArgumentException>(() => LocArgs.Of("m", double.NaN, 1));
            Assert.That(LocArgs.Of("a", 1).And("b", "x").And("c", 2.5, 1).Count, Is.EqualTo(3));
            Assert.That(LocArgs.None.Count, Is.EqualTo(0));
        }

        private static string s_sink = string.Empty;
    }
}
