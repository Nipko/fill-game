#nullable enable
using System.Collections.Generic;
using AF.Localization;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Pseudolocalización en-XA (L2a; propuesta de idiomas §3.8): corchetes, acentos solo de Latin-1 (para que B1 los cubra
    /// en el atlas) y un 35 % más de largo [P], sin romper los marcadores, los plurales ni las etiquetas de TextMeshPro.
    /// </summary>
    public sealed class LocPseudoTests
    {
        private static readonly LocNumberFormat EsCo = LocNumber.For("es-CO");

        [Test]
        public void Transform_AccentsBracketsAndPads()
        {
            Assert.That(LocPseudo.Transform("Siguiente"), Is.EqualTo("[Sígúíéñté ···]"), "9 visibles → 4 de relleno");
            Assert.That(LocPseudo.Transform("Nivel"), Is.EqualTo("[Ñívél ·]"), "5 visibles → 2 de relleno");
            Assert.That(LocPseudo.Transform("ok"), Is.EqualTo("[ók·]"), "2 visibles → 1 de relleno");
            Assert.That(LocPseudo.Transform(string.Empty), Is.EqualTo(string.Empty), "una traducción que falta sigue faltando");
        }

        [TestCase("¡Nivel completado!")]
        [TestCase("Te faltan 3 semillas para la Cuia de rescate")]
        [TestCase("Cruza el bosque en fila, por ramas finas")]
        [TestCase("Siguiente")]
        [TestCase("Sí")]
        public void Transform_IsAtLeast35PercentLonger(string text)
        {
            string pseudo = LocPseudo.Transform(text);
            int added = pseudo.Length - 2 - text.Length;
            Assert.That(added * 100, Is.GreaterThanOrEqualTo(text.Length * LocPseudo.ExpansionPercent), pseudo);
            Assert.That(pseudo[0], Is.EqualTo('['));
            Assert.That(pseudo[pseudo.Length - 1], Is.EqualTo(']'));
        }

        [Test]
        public void Transform_KeepsMarkers_AndStillFormats()
        {
            string pseudo = LocPseudo.Transform("Nivel {level}");
            Assert.That(pseudo, Is.EqualTo("[Ñívél {level} ··]"));
            Assert.That(LocFormat.Format(pseudo, LocArgs.OfDigits("level", 12), "en-XA", EsCo), Is.EqualTo("[Ñívél 12 ··]"));

            string banner = LocPseudo.Transform("Mundo {n}: Açaí{sep}Brasil");
            Assert.That(LocFormat.SeparatorCount(banner), Is.EqualTo(1));
            Assert.That(LocFormat.Validate(banner), Is.Null);
        }

        [Test]
        public void Transform_PluralFormsAreAccented_ButTheSyntaxIsKept()
        {
            const string source = "{n:plural:Te falta {} semilla|Te faltan {} semillas}";
            string pseudo = LocPseudo.Transform(source);
            Assert.That(pseudo, Does.StartWith("[{n:plural:Té fáltá {} sémíllá|Té fáltáñ {} sémíllás}"));
            Assert.That(LocFormat.Validate(pseudo), Is.Null);
            var before = new List<string>();
            var after = new List<string>();
            LocFormat.CollectNames(source, before);
            LocFormat.CollectNames(pseudo, after);
            Assert.That(after, Is.EqualTo(before));
            Assert.That(LocFormat.Format(pseudo, LocArgs.Of("n", 1), "en-XA", EsCo), Does.StartWith("[Té fáltá 1 sémíllá"));
            Assert.That(LocFormat.Format(pseudo, LocArgs.Of("n", 1000), "en-XA", EsCo), Does.StartWith("[Té fáltáñ 1.000 sémíllás"));
        }

        [Test]
        public void Transform_KeepsTextMeshProTags_AndLineBreaks()
        {
            string pseudo = LocPseudo.Transform("<b>Mundo 1</b>\nAçaí <color=#FFCC00>y</color>");
            Assert.That(pseudo, Does.StartWith("[<b>Múñdó 1</b>\nÁçáí <color=#FFCC00>ý</color>"));
            Assert.That(LocPseudo.Transform("a < b"), Does.StartWith("[á < b"), "un «<» que no abre etiqueta es texto");
        }

        [TestCase("¡Hola! ¿Qué tal? Ñandú, pingüino, açaí, cuia · 50 % — «comillas»")]
        [TestCase("Level complete! Nível concluído! Próximo")]
        [TestCase("{n:plural:Faltan {} nivel|Faltan {} niveles}<b>!</b>")]
        public void Transform_AddsOnlyLatin1Characters(string text)
        {
            string pseudo = LocPseudo.Transform(text);
            for (int i = 0; i < pseudo.Length; i++)
            {
                char c = pseudo[i];
                if (text.IndexOf(c) < 0)
                {
                    Assert.That(c <= 'ÿ', Is.True, "U+" + ((int)c).ToString("X4") + " no es de Latin-1");
                    Assert.That(LocPseudo.Characters.IndexOf(c), Is.GreaterThanOrEqualTo(0), "B1 lo cubre con LocPseudo.Characters");
                }
            }

            for (int i = 0; i < LocPseudo.Characters.Length; i++)
            {
                Assert.That(LocPseudo.Characters[i] <= 'ÿ', Is.True);
            }
        }

        [Test]
        public void Accent_MapsOnlyLettersWithALatin1Form()
        {
            Assert.That(LocPseudo.Accent('a'), Is.EqualTo('á'));
            Assert.That(LocPseudo.Accent('N'), Is.EqualTo('Ñ'));
            Assert.That(LocPseudo.Accent('b'), Is.EqualTo('b'));
            Assert.That(LocPseudo.Accent('á'), Is.EqualTo('á'));
            Assert.That(LocPseudo.Accent('7'), Is.EqualTo('7'));
        }
    }
}
