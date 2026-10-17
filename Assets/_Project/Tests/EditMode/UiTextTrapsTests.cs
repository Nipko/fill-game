#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using AF.Localization;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Idiomas, nivel 1 (D-018; propuesta de idiomas §3.1, §3.2 y §6; tarea L1 de la tanda 4): sin trampas para traducir y sin
    /// cambiar ningún texto en es-419.
    /// <list type="bullet">
    /// <item>El golden (<see cref="UiTextGolden"/>, generado con el código de antes) coincide línea a línea.</item>
    /// <item>Los plurales con 0, 1, 2 y 21: «Te falta 1 semilla», «en 1 día» y «Perderás 2 canoas», nunca «1 semillas».</item>
    /// <item>Las plantillas llevan marcadores con nombre y sintaxis válida; ninguna pantalla pega « · », hace <c>Replace</c> sobre
    /// el texto ni elige el plural con <c>== 1</c>.</item>
    /// <item>Las cifras siguen la región de <see cref="Loc"/>: sin región, es-CO («1.000»); el camino por buffer, sin asignar.</item>
    /// <item>Con L2b los textos salen de las tablas (<c>Data/Loc/strings.csv</c> y <c>facts.csv</c>): el golden sigue igual y las
    /// cuatro plantillas que eran posicionales (<c>{0}</c> con <c>string.Format</c>) dan el mismo texto con marcadores con
    /// nombre.</item>
    /// </list>
    /// La fachada <see cref="Loc"/> es estática: cada test parte de es-419 con las tablas del proyecto y sin región del teléfono
    /// (cifras de es-CO; <see cref="LocSetUpFixture.ConfigureEs419"/>) y lo deja como estaba.
    /// </summary>
    public sealed class UiTextTrapsTests
    {
        private static int s_end;
        private LocStateSnapshot _loc;

        private static string GoldenPath => Path.Combine(Application.dataPath, "_Project", "Tests", "EditMode", "Golden", UiTextGolden.FileName);

        private static string UiScriptsDir => Path.Combine(Application.dataPath, "_Project", "Scripts", "UI");

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
        public void Golden_Es419_MatchesTheTextsFromBeforeL1()
        {
            SortedDictionary<string, string?> golden = UiTextGolden.Parse(File.ReadAllText(GoldenPath, Encoding.UTF8));
            SortedDictionary<string, string?> now = UiTextGolden.Build();
            var diffs = new StringBuilder();
            foreach (KeyValuePair<string, string?> line in golden)
            {
                if (!now.TryGetValue(line.Key, out string? text))
                {
                    diffs.Append("falta ").Append(line.Key).Append('\n');
                }
                else if (!string.Equals(text, line.Value, System.StringComparison.Ordinal))
                {
                    diffs.Append(line.Key).Append(": antes «").Append(line.Value).Append("», ahora «").Append(text).Append("»\n");
                }
            }

            foreach (KeyValuePair<string, string?> line in now)
            {
                if (!golden.ContainsKey(line.Key))
                {
                    diffs.Append("nueva ").Append(line.Key).Append(": «").Append(line.Value).Append("» (¿una constante nueva sin golden?)\n");
                }
            }

            Assert.That(golden.Count, Is.GreaterThanOrEqualTo(360), "el golden entero");
            Assert.That(diffs.ToString(), Is.Empty, "ningún texto visible cambia en es-419");
        }

        [Test]
        public void Golden_File_IsOrdinalUtf8WithoutBomAndLf()
        {
            byte[] bytes = File.ReadAllBytes(GoldenPath);
            Assert.That(bytes.Length > 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, Is.False, "sin BOM");
            string text = Encoding.UTF8.GetString(bytes);
            Assert.That(text.IndexOf('\r'), Is.EqualTo(-1), "LF");
            Assert.That(text, Is.EqualTo(UiTextGolden.Render(UiTextGolden.Parse(text))), "una línea por caso, en orden ordinal");
        }

        [TestCase(0, "Te faltan 0 semillas")]
        [TestCase(1, "Te falta 1 semilla")]
        [TestCase(2, "Te faltan 2 semillas")]
        [TestCase(21, "Te faltan 21 semillas")]
        [TestCase(1000000, "Te faltan 1.000.000 semillas")]
        public void MissingSeeds_Plural(int n, string expected)
        {
            Assert.That(UiText.MissingSeeds(n), Is.EqualTo(expected));
            Assert.That(UiText.RevealMissing(n), Is.EqualTo(expected), "Revelar sin medios dice lo mismo que el rescate");
        }

        [TestCase(0, "Río arriba llegan 100 niveles nuevos en unos 0 días.")]
        [TestCase(1, "Río arriba llegan 100 niveles nuevos en 1 día.")]
        [TestCase(2, "Río arriba llegan 100 niveles nuevos en unos 2 días.")]
        [TestCase(21, "Río arriba llegan 100 niveles nuevos en unos 21 días.")]
        public void ContentEnd_Plural(int days, string expected)
        {
            Assert.That(UiText.ContentEnd(days), Is.EqualTo(expected));
        }

        [TestCase(0, "Perderás 0 canoas")]
        [TestCase(1, "Perderás 1 canoa")]
        [TestCase(2, "Perderás 2 canoas")]
        [TestCase(21, "Perderás 21 canoas")]
        public void LoseCanoes_Plural(int n, string expected)
        {
            Assert.That(UiText.LoseCanoes(n), Is.EqualTo(expected));
        }

        [Test]
        public void GiveUpAndQuit_SayPerderas1Canoa_FromThePluralTemplate()
        {
            Assert.That(UiText.GiveUpConfirmCanoe, Is.EqualTo("Perderás 1 canoa"));
            Assert.That(UiText.QuitConfirmCanoe, Is.EqualTo("Perderás 1 canoa"));
            Assert.That(UiText.GiveUpConfirmCanoe, Is.SameAs(UiText.GiveUpConfirmCanoe), "se arma una vez");
        }

        [TestCase(0, "+3 Deshacer · 0 semillas", "+3 Deshacer\n0 semillas")]
        [TestCase(1, "+3 Deshacer · 1 semilla", "+3 Deshacer\n1 semilla")]
        [TestCase(2, "+3 Deshacer · 2 semillas", "+3 Deshacer\n2 semillas")]
        [TestCase(21, "+3 Deshacer · 21 semillas", "+3 Deshacer\n21 semillas")]
        public void StuckRefillSeeds_Plural_AndTheCutFromTheTemplate(int price, string oneLine, string twoLines)
        {
            Assert.That(UiText.StuckRefillSeeds(3, price, false), Is.EqualTo(oneLine));
            Assert.That(UiText.StuckRefillSeeds(3, price, true), Is.EqualTo(twoLines));
        }

        [Test]
        public void StuckCards_BreakOnlyWhereTheTemplateSays()
        {
            Assert.That(UiText.StuckRefillVideo(3), Is.EqualTo("+3 Deshacer\nVer video"));
            Assert.That(UiText.StuckPackLine(1000, "US$ 1,99"), Is.EqualTo("Puñado de semillas\n1.000 · US$ 1,99"));
            Assert.That(UiText.StuckBalsaLine("COP 3.900"), Is.EqualTo("Balsa de rescate\nCOP 3.900"));

            // Un precio de tienda con « · » (o con llaves) se pinta tal cual: ya no hay Replace que lo parta.
            Assert.That(UiText.StuckBalsaLine("A · {n}"), Is.EqualTo("Balsa de rescate\nA · {n}"));
        }

        /// <summary>
        /// Donde el salto es fijo, va en la cadena («\n»), donde el traductor puede moverlo (regla de G1; propuesta de idiomas
        /// §6, nivel 1), y no en un <c>{sep}</c> que el código elige por posición: así la plantilla da lo mismo con el formato
        /// por defecto de la tabla (<c>Loc.T(key, args)</c>, que convierte cada <c>{sep}</c> en salto), sin <c>breaks</c>.
        /// El <c>{sep}</c> queda solo donde la pantalla elige de verdad: la recarga por semillas (1 o 2 líneas) y el banner.
        /// </summary>
        [Test]
        public void StuckCards_FixedBreaksLiveInTheString_SepOnlyWhereTheScreenChooses()
        {
            foreach (string key in new[] { UiText.Keys.StuckUndoRefillAd, UiText.Keys.StuckPack, UiText.Keys.StuckBalsa })
            {
                string fixedBreak = Loc.Template(key)!;
                Assert.That(LocFormat.SeparatorCount(fixedBreak), Is.EqualTo(0), "«" + fixedBreak + "»: sin {sep}");
                Assert.That(fixedBreak.IndexOf('\n'), Is.GreaterThan(0), "«" + fixedBreak + "»: el salto, en la cadena");
                Assert.That(fixedBreak.IndexOf('\n'), Is.EqualTo(fixedBreak.LastIndexOf('\n')), "«" + fixedBreak + "»: un solo salto");
            }

            Assert.That(LocFormat.SeparatorCount(Loc.Template(UiText.Keys.StuckUndoRefillSeeds)!), Is.EqualTo(1), "la recarga por semillas sí elige");

            // El formato por defecto de la tabla (Loc.T(key, args)): cada {sep} sería un salto, y aquí no hay.
            Assert.That(Loc.T(UiText.Keys.StuckUndoRefillAd, LocArgs.OfDigits("n", 3)), Is.EqualTo(UiText.StuckRefillVideo(3)));
            Assert.That(Loc.T(UiText.Keys.StuckPack, LocArgs.Of("seeds", 1000L).And("price", "US$ 1,99")), Is.EqualTo(UiText.StuckPackLine(1000, "US$ 1,99")));
            Assert.That(Loc.T(UiText.Keys.StuckBalsa, LocArgs.Of("price", "COP 3.900")), Is.EqualTo(UiText.StuckBalsaLine("COP 3.900")));
        }

        /// <summary>
        /// Las cuatro plantillas que L1 dejó posicionales porque sus pantallas las usaban con <c>string.Format</c> (la burbuja de
        /// la bandeja, Nivel fallido y los dos botones de Sin canoas) pasan a la tabla con marcadores con nombre y a un método de
        /// UiText: dan el mismo texto que antes con las cifras de siempre y, con 1, el singular.
        /// </summary>
        [Test]
        public void FormerPositionalTemplates_GiveTheSameTextAsBefore()
        {
            System.Globalization.CultureInfo inv = System.Globalization.CultureInfo.InvariantCulture;
            foreach (int n in new[] { 2, 30, 460, 1000, 1234567 })
            {
                Assert.That(UiText.TrayMissing(n), Is.EqualTo(string.Format(inv, "Te faltan {0}", UiText.Grouped(n))));
                Assert.That(UiText.LevelFailedReward(n), Is.EqualTo(string.Format(inv, "Premio: {0} semillas", UiText.Grouped(n))));
            }

            Assert.That(UiText.TrayMissing(1), Is.EqualTo("Te falta 1"));
            Assert.That(UiText.LevelFailedReward(1), Is.EqualTo("Premio: 1 semilla"));
            Assert.That(UiText.NoCanoesRefill(5), Is.EqualTo(string.Format(inv, "Llenar canoas · {0}/{0}", 5)));
            Assert.That(UiText.NoCanoesInfinite("COP 3.900"), Is.EqualTo(string.Format(inv, "Canoas infinitas 1 h · {0}", "COP 3.900")));
            Assert.That(UiText.NoCanoesInfinite(string.Empty), Is.EqualTo("Canoas infinitas 1 h · "));
        }

        [Test]
        public void TableTemplates_AreNamedAndValid_NoPositionalLeft()
        {
            LocTable strings = Loc.Catalog.Strings;
            int templates = 0;
            for (int r = 0; r < strings.Count; r++)
            {
                string template = strings.TextAt(r, LocLocale.SourceLocale);
                if (template.IndexOf('{') < 0)
                {
                    continue;
                }

                Assert.That(LocFormat.Validate(template), Is.Null, strings.KeyAt(r) + ": «" + template + "»");
                Assert.That(Regex.IsMatch(template, "\\{[0-9]"), Is.False, strings.KeyAt(r) + ": nunca posicional");
                templates++;
            }

            Assert.That(templates, Is.GreaterThanOrEqualTo(24));
        }

        [Test]
        public void WorldTexts_AreTheBannerKeyCutAtItsSeparators()
        {
            for (int w = 1; w <= AF.Levels.WorldTable.WorldCount; w++)
            {
                Assert.That(UiText.WorldBanner(w), Is.EqualTo(UiText.WorldHeading(w) + UiText.WorldSeparator + UiText.WorldPlace(w)));
                Assert.That(UiText.WorldPlaceLine(w, false, 0), Is.EqualTo(UiText.WorldPlace(w)));
                Assert.That(UiText.WorldPlaceLine(w, true, 0), Is.EqualTo("<b>" + UiText.WorldName(w) + "</b>" + UiText.WorldSeparator + UiText.WorldPlace(w)),
                    "la negrita del nombre va en la plantilla");
                Assert.That(UiText.WorldPlaceLine(w, true, 0).Replace("<b>", string.Empty).Replace("</b>", string.Empty), Is.EqualTo(UiText.WorldPlaceNamed(w)));
                Assert.That(UiText.WorldPlaceLine(w, false, 1), Is.EqualTo(UiText.WorldCountry(w) + "\n" + UiText.WorldRiver(w)));
                Assert.That(UiText.WorldPlaceLine(w, true, 1), Is.EqualTo("<b>" + UiText.WorldName(w) + "</b>\n" + UiText.WorldPlace(w)));
                Assert.That(UiText.WorldPlaceLine(w, true, 2), Is.EqualTo("<b>" + UiText.WorldName(w) + "</b>" + UiText.WorldSeparator + UiText.WorldCountry(w) + "\n" + UiText.WorldRiver(w)));
            }

            Assert.That(UiText.WorldPlaceSeparators(false), Is.EqualTo(1));
            Assert.That(UiText.WorldPlaceSeparators(true), Is.EqualTo(2));
        }

        [Test]
        public void Grouped_FollowsTheRegionOfLoc_EsCoWithoutOne()
        {
            Assert.That(Loc.RequestedNumberTag, Is.Null);
            Assert.That(UiText.Grouped(1000), Is.EqualTo("1.000"), "sin región del teléfono, es-CO (lo de hoy; notas de la tanda 4, punto 10)");
            Assert.That(UiText.MissingSeeds(1000), Is.EqualTo("Te faltan 1.000 semillas"));
            Loc.SetNumberTag("es-MX");
            Assert.That(UiText.Grouped(1000), Is.EqualTo("1,000"));
            Assert.That(UiText.MissingSeeds(1500), Is.EqualTo("Te faltan 1,500 semillas"));
            var buffer = new char[16];
            Assert.That(new string(buffer, 0, UiText.WriteNumber(buffer, 0, 36000, true)), Is.EqualTo("36,000"));
            Assert.That(UiText.Level(1000), Is.EqualTo("Nivel 1000"), "los números de nivel van sin separador");
        }

        [Test]
        public void WriteNumber_KeepsTheBufferPath_WithoutAllocating()
        {
            var buffer = new char[16];
            TestDelegate write = () => { s_end = UiText.WriteNumber(buffer, 0, 1234567, true); };
            write();
            Assert.That(write, Is.Not.AllocatingGCMemory());
            Assert.That(new string(buffer, 0, s_end), Is.EqualTo("1.234.567"));
        }

        [Test]
        public void FaunaFact_IsATableByKey_WithoutAllocating()
        {
            string fact = string.Empty;
            TestDelegate read = () => { fact = UiText.FaunaFact("fauna_saimiri_collinsi_dato"); };
            read();
            Assert.That(read, Is.Not.AllocatingGCMemory());
            Assert.That(fact, Is.EqualTo("Cruza el bosque en fila, por ramas finas"));
            Assert.That(UiText.FaunaFact("fauna_otro_dato"), Is.Empty);
            Assert.That(UiText.FaunaFact(null!), Is.Empty);
            var keys = new List<string>(UiText.FaunaFactKeys);
            Assert.That(keys.Count, Is.EqualTo(11));
            foreach (string key in keys)
            {
                Assert.That(key, Does.StartWith("fauna_"), "claves fauna_<id>_dato de facts.csv");
                Assert.That(key, Does.EndWith("_dato"), "claves fauna_<id>_dato de facts.csv");
            }
        }

        /// <summary>
        /// El nombre del miquito de la tarjeta sale de la tabla de textos largos por su clave <c>fauna.&lt;id&gt;.name</c>, en el
        /// idioma de <see cref="Loc"/> y, en en-XA, pseudolocalizado (observación 3 de la prueba de la ola 3 en el teléfono: decía
        /// «squirrel monkey» sin corchetes; tarea FX5). Sin asignaciones.
        /// </summary>
        [TestCase("es-419", "mono ardilla")]
        [TestCase("en", "squirrel monkey")]
        [TestCase("pt-BR", "macaco-de-cheiro")]
        public void FaunaName_IsATableByKey_InTheLanguageOfLoc_WithoutAllocating(string locale, string expected)
        {
            Loc.SetLocale(locale);
            string name = string.Empty;
            TestDelegate read = () => { name = UiText.FaunaName("fauna.saimiri_collinsi.name"); };
            read();
            Assert.That(read, Is.Not.AllocatingGCMemory());
            Assert.That(name, Is.EqualTo(expected));
            Assert.That(UiText.FaunaName("fauna.otro.name"), Is.Empty);
            Assert.That(UiText.FaunaName(null!), Is.Empty);
        }

        [Test]
        public void FaunaName_InEnXA_IsPseudolocalized()
        {
            Loc.SetLocale(LocLocale.Pseudo);
            string name = UiText.FaunaName("fauna.saimiri_collinsi.name");
            Assert.That(name, Does.StartWith("[").And.EndWith("]"), "en-XA pone el nombre entre corchetes, como todo texto de las tablas");
            Assert.That(name, Does.Not.Contain("squirrel monkey").And.Not.Contain("mono ardilla"), "acentuado y alargado: «" + name + "»");
        }

        /// <summary>
        /// La parte «grep» de los criterios de L1 sobre los fuentes de la UI: ningún « · » pegado con <c>+</c>, ningún
        /// <c>Replace</c> que cambie un texto con « · » ni con un salto, y ningún plural elegido con <c>== 1 ? "…"</c>.
        /// </summary>
        [Test]
        public void UiSources_HaveNoConcatenatedSeparator_NoReplaceOnText_NoEqualsOnePlural()
        {
            var traps = new[]
            {
                new Regex("·\\s*\"\\s*\\+|\\+\\s*\"\\s*·"),
                new Regex("Separator\\s*\\+|\\+\\s*(UiText\\.)?(World)?Separator\\b|\"</?b>\"\\s*\\+|\\+\\s*\"</?b>\""),
                new Regex("\\.Replace\\(\"[^\"]*·"),
                new Regex("\\.Replace\\(\"[^\"]*\",\\s*\"[^\"]*\\\\n"),
                new Regex("==\\s*1\\s*\\?\\s*\""),
            };

            var found = new StringBuilder();
            string[] files = Directory.GetFiles(UiScriptsDir, "*.cs", SearchOption.AllDirectories);
            Assert.That(files.Length, Is.GreaterThan(30));
            foreach (string file in files)
            {
                string[] lines = File.ReadAllLines(file, Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].TrimStart();
                    if (line.StartsWith("//"))
                    {
                        continue;
                    }

                    foreach (Regex trap in traps)
                    {
                        if (trap.IsMatch(line))
                        {
                            found.Append(Path.GetFileName(file)).Append(':').Append(i + 1).Append(": ").Append(line).Append('\n');
                        }
                    }
                }
            }

            Assert.That(found.ToString(), Is.Empty);
        }
    }
}
