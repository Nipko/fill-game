#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using AF.Levels;
using AF.Localization;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Las tablas de textos del juego (D-018, nivel 2; 08 §4.7; contrato 32; tarea L2b), <c>Data/Loc/strings.csv</c> y
    /// <c>facts.csv</c>:
    /// <list type="bullet">
    /// <item>UTF-8 sin BOM, LF y RFC 4180, con la cabecera de 08 §4.7 en la forma de la pregunta 49 (a) (estado del texto fuente
    /// y uno por idioma) más <c>args</c> [P], y filas en orden ordinal por clave (<see cref="StringComparer.Ordinal"/>; regla
    /// de G1).</item>
    /// <item>Toda clave que usa <see cref="UiText"/> existe y toda fila de <c>strings.csv</c> la usa; las de la fauna son las
    /// <c>dato_tarjeta</c> del catálogo.</item>
    /// <item>es-419 nunca vacío; cada fila con contexto, <c>max_chars</c> y estado; el texto fuente cabe en <c>max_chars</c> y
    /// cada traducción, en un 30 % más (propuesta de idiomas §3.4 y §3.7; <see cref="VisibleLength"/>).</item>
    /// <item>Plantillas válidas y las traducciones con los mismos marcadores; ninguna cifra con separador ni decimales en
    /// <c>facts.csv</c> (Q8-A: van con <c>{n}</c> y su valor en <c>args</c>).</item>
    /// <item>EN y PT-BR incompletos: el idioma automático se queda en es-419 (notas de la tanda 4, punto 10); en-XA pone toda la
    /// UI entre corchetes; y nada depende de la cultura del equipo (<see cref="LocSetUpFixture"/>).</item>
    /// </list>
    /// </summary>
    public sealed class LocTablesTests
    {
        private const string Header = "key,es-419,en,pt-BR,context,max_chars,status,status_en,status_pt-BR,notes,args";

        // Cifra con separador de miles o decimales: «1.000», «5,2», «1 000» con espacio duro o fino, «1'000» y también los
        // miles con un espacio normal U+0020 («2 000», como los escriben las fuentes francesas y científicas), que solo cuentan
        // con un grupo de 1-3 cifras y otro de exactamente 3 («entre 20 y 300» o «1852 200» no lo son).
        private static readonly Regex NumberWithSeparator = new Regex("[0-9][.,\u00A0\u202F\u2009'][0-9]|(?<![0-9])[0-9]{1,3}( [0-9]{3})+(?![0-9])");

        private LocStateSnapshot _loc;

        private static IEnumerable<string> Tables => new[] { LocTables.StringsAssetPath, LocTables.FactsAssetPath };

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

        [TestCaseSource(nameof(Tables))]
        public void Table_IsUtf8WithoutBom_Lf_WithTheHeaderOf08_4_7(string assetPath)
        {
            byte[] bytes = File.ReadAllBytes(LocTables.ProjectPath(assetPath));
            Assert.That(bytes.Length > 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, Is.False, "sin BOM");
            string text = LocCsv.Decode(bytes, Path.GetFileName(assetPath));
            Assert.That(text.IndexOf('\r'), Is.EqualTo(-1), "LF");
            Assert.That(text.Substring(0, text.IndexOf('\n')), Is.EqualTo(Header), "cabecera de 08 §4.7 con la 49 (a) y args");
            Assert.That(text.EndsWith("\n", StringComparison.Ordinal), Is.True, "termina en salto de línea");

            LocTable table = LocTable.Parse(text, Path.GetFileName(assetPath));
            Assert.That(table.PerLocaleStatus, Is.True);
            Assert.That(table.HasArgs, Is.True);
            Assert.That(table.Count, Is.GreaterThan(0));
        }

        [TestCaseSource(nameof(Tables))]
        public void Rows_AreInOrdinalOrder(string assetPath)
        {
            LocTable table = LocTable.Parse(LocTables.ReadProjectFile(assetPath, Path.GetFileName(assetPath)), Path.GetFileName(assetPath));
            var keys = new List<string>();
            for (int r = 0; r < table.Count; r++)
            {
                keys.Add(table.KeyAt(r));
            }

            var sorted = new List<string>(keys);
            sorted.Sort(StringComparer.Ordinal);
            Assert.That(keys, Is.EqualTo(sorted), "orden ordinal por clave (sorted() en Python, StringComparer.Ordinal en C#)");
            Assert.That(table.UnsortedAt, Is.EqualTo(-1));
        }

        /// <summary>
        /// El orden es el ordinal, por código Unicode, y nunca el de la cultura (regla de G1; <c>.claude/rules/data-json.md</c>):
        /// «.» (U+002E) va antes que «_» (U+005F), así que una clave de contenido con puntos va antes que una de la fauna con el
        /// mismo comienzo.
        /// </summary>
        [Test]
        public void Order_IsOrdinal_DotsBeforeUnderscores()
        {
            const string head = "key,es-419\n";
            LocTable ordinal = LocTable.Parse(head + "fauna.bradypus_variegatus.dato_01,A\nfauna_saimiri_collinsi_dato,B\n", "facts.csv");
            Assert.That(ordinal.UnsortedAt, Is.EqualTo(-1));
            LocTable cultural = LocTable.Parse(head + "fauna_saimiri_collinsi_dato,B\nfauna.bradypus_variegatus.dato_01,A\n", "facts.csv");
            Assert.That(cultural.UnsortedAt, Is.EqualTo(1), "el orden de la cultura no vale");
            Assert.That(string.CompareOrdinal("fauna.b", "fauna_s"), Is.LessThan(0));
        }

        [Test]
        public void EveryUiTextKey_Exists_AndEveryStringsRow_IsUsed()
        {
            LocCatalog catalog = LocSetUpFixture.Catalog;
            var used = new HashSet<string>(UiTextKeys(), StringComparer.Ordinal);
            Assert.That(used.Count, Is.GreaterThanOrEqualTo(130));
            var missing = new StringBuilder();
            foreach (string key in used)
            {
                if (!catalog.Strings.Contains(key))
                {
                    missing.Append(key).Append(' ');
                }
            }

            var unused = new StringBuilder();
            for (int r = 0; r < catalog.Strings.Count; r++)
            {
                if (!used.Contains(catalog.Strings.KeyAt(r)))
                {
                    unused.Append(catalog.Strings.KeyAt(r)).Append(' ');
                }
            }

            Assert.That(missing.ToString(), Is.Empty, "claves de UiText que faltan en strings.csv");
            Assert.That(unused.ToString(), Is.Empty, "filas de strings.csv que UiText no usa");
        }

        [Test]
        public void FaunaFacts_AreTheCatalogKeys()
        {
            FaunaCatalog fauna = FaunaCatalogReader.Read(File.ReadAllText(LocTables.ProjectPath("Assets/_Project/Data/Fauna/miquitos_v0.json"), Encoding.UTF8));
            LocTable facts = LocSetUpFixture.Catalog.Facts;
            var keys = new List<string>();
            for (int i = 0; i < fauna.Species.Count; i++)
            {
                string key = fauna.Species[i].FactKey;
                keys.Add(key);
                Assert.That(facts.Contains(key), Is.True, key + " (dato_tarjeta de " + fauna.Species[i].Id + ")");
            }

            for (int r = 0; r < facts.Count; r++)
            {
                if (facts.KeyAt(r).StartsWith("fauna_", StringComparison.Ordinal))
                {
                    Assert.That(keys, Does.Contain(facts.KeyAt(r)), "dato de fauna sin especie en el catálogo");
                }
            }

            Assert.That(new List<string>(UiText.FaunaFactKeys).Count, Is.EqualTo(fauna.Species.Count));
        }

        /// <summary>
        /// Los nombres de la tarjeta «¡Miquito a la vista!» (tarea FX5): cada especie del catálogo tiene su fila
        /// <c>fauna.&lt;id&gt;.name</c> en <c>facts.csv</c> con los <c>nombres</c> del catálogo (es → es-419, en → en y pt → pt-BR),
        /// y toda fila <c>fauna.*.name</c> es de una especie del catálogo: las dos copias no se separan.
        /// </summary>
        [Test]
        public void FaunaNames_AreTheCatalogNames()
        {
            FaunaCatalog fauna = FaunaCatalogReader.Read(File.ReadAllText(LocTables.ProjectPath("Assets/_Project/Data/Fauna/miquitos_v0.json"), Encoding.UTF8));
            LocTable facts = LocSetUpFixture.Catalog.Facts;
            var keys = new List<string>();
            for (int i = 0; i < fauna.Species.Count; i++)
            {
                FaunaSpecies species = fauna.Species[i];
                string key = species.NameKey;
                Assert.That(key, Is.EqualTo("fauna." + species.Id + ".name"));
                keys.Add(key);
                int row = facts.IndexOf(key);
                Assert.That(row, Is.GreaterThanOrEqualTo(0), key + " (nombre de " + species.Id + ")");
                Assert.That(facts.TextAt(row, LocLocale.SourceLocale), Is.EqualTo(species.Names["es"]), key + " (es-419)");
                Assert.That(facts.TextAt(row, "en"), Is.EqualTo(species.Names.TryGetValue("en", out string? en) ? en : string.Empty), key + " (en)");
                Assert.That(facts.TextAt(row, "pt-BR"), Is.EqualTo(species.Names.TryGetValue("pt", out string? pt) ? pt : string.Empty), key + " (pt-BR)");
            }

            for (int r = 0; r < facts.Count; r++)
            {
                string key = facts.KeyAt(r);
                if (key.StartsWith(FaunaSpecies.NameKeyPrefix, StringComparison.Ordinal) && key.EndsWith(FaunaSpecies.NameKeySuffix, StringComparison.Ordinal))
                {
                    Assert.That(keys, Does.Contain(key), "nombre de fauna sin especie en el catálogo");
                }
            }
        }

        [Test]
        public void EveryRow_HasEs419_Context_MaxChars_AndAStatusPerText()
        {
            foreach (LocTable table in Both())
            {
                for (int r = 0; r < table.Count; r++)
                {
                    string what = table.File + " " + table.KeyAt(r);
                    Assert.That(table.TextAt(r, LocLocale.SourceLocale), Is.Not.Empty, what + ": es-419 nunca vacío");
                    Assert.That(table.ContextAt(r), Is.Not.Empty, what + ": contexto para el traductor");
                    Assert.That(table.MaxCharsAt(r), Is.GreaterThan(0), what + ": max_chars");
                    Assert.That(table.StatusAt(r, LocLocale.SourceLocale), Is.Not.EqualTo(LocStatus.Missing), what);
                    foreach (string locale in table.Locales)
                    {
                        if (locale == LocLocale.SourceLocale)
                        {
                            continue;
                        }

                        bool hasText = table.TextAt(r, locale).Length > 0;
                        LocStatus status = table.StatusAt(r, locale);
                        Assert.That(hasText ? status >= LocStatus.Translated : status == LocStatus.Missing, Is.True,
                            what + " (" + locale + "): una traducción lleva traducido o revisado_nativo y una que falta, ninguno");
                    }
                }
            }
        }

        [Test]
        public void CulturalRows_AreBornPendingTheAdvisor()
        {
            foreach (LocTable table in Both())
            {
                for (int r = 0; r < table.Count; r++)
                {
                    string key = table.KeyAt(r);
                    bool cultural = key.StartsWith("fauna_", StringComparison.Ordinal) || key.StartsWith("tutorial_", StringComparison.Ordinal)
                        || key.StartsWith("fauna.", StringComparison.Ordinal) || key.StartsWith(AF.GameFlow.SlothLines.KeyPrefix, StringComparison.Ordinal)
                        || key == UiText.Keys.SettingsSlothMessages
                        || key.StartsWith("difficulty_subtitle_", StringComparison.Ordinal)
                        || (key.StartsWith("world_w", StringComparison.Ordinal) && key.Length > "world_w00_".Length && char.IsDigit(key[7]));
                    if (cultural)
                    {
                        Assert.That(table.StatusAt(r, LocLocale.SourceLocale), Is.EqualTo(LocStatus.PendingAdvisor), table.File + " " + key);
                    }
                }
            }
        }

        [Test]
        public void MaxChars_HoldsEs419_AndTranslationsGetThirtyPercentMore()
        {
            var over = new StringBuilder();
            foreach (LocTable table in Both())
            {
                for (int r = 0; r < table.Count; r++)
                {
                    int max = table.MaxCharsAt(r);
                    foreach (string locale in table.Locales)
                    {
                        string text = table.TextAt(r, locale);
                        if (text.Length == 0)
                        {
                            continue;
                        }

                        int length = VisibleLength(text);
                        bool fits = locale == LocLocale.SourceLocale ? length <= max : length * 10 <= max * 13;
                        if (!fits)
                        {
                            over.Append(table.File).Append(' ').Append(table.KeyAt(r)).Append(" (").Append(locale).Append("): ")
                                .Append(length.ToString(CultureInfo.InvariantCulture)).Append(" > ").Append(max.ToString(CultureInfo.InvariantCulture))
                                .Append(locale == LocLocale.SourceLocale ? string.Empty : " × 1,3").Append('\n');
                        }
                    }
                }
            }

            Assert.That(over.ToString(), Is.Empty);
        }

        [Test]
        public void Templates_AreValid_AndTranslationsKeepTheirMarkers()
        {
            foreach (LocTable table in Both())
            {
                for (int r = 0; r < table.Count; r++)
                {
                    string source = table.TextAt(r, LocLocale.SourceLocale);
                    Assert.That(LocFormat.Validate(source), Is.Null, table.File + " " + table.KeyAt(r) + ": «" + source + "»");
                    var names = new List<string>();
                    LocFormat.CollectNames(source, names);
                    names.Sort(StringComparer.Ordinal);
                    foreach (string locale in table.Locales)
                    {
                        string text = table.TextAt(r, locale);
                        if (locale == LocLocale.SourceLocale || text.Length == 0)
                        {
                            continue;
                        }

                        string what = table.File + " " + table.KeyAt(r) + " (" + locale + ")";
                        Assert.That(LocFormat.Validate(text), Is.Null, what);
                        var translated = new List<string>();
                        LocFormat.CollectNames(text, translated);
                        translated.Sort(StringComparer.Ordinal);
                        Assert.That(translated, Is.EqualTo(names), what + ": los mismos marcadores que es-419");
                        Assert.That(LocFormat.SeparatorCount(text), Is.EqualTo(LocFormat.SeparatorCount(source)), what + ": los mismos {sep}");
                    }
                }
            }
        }

        [Test]
        public void Facts_HaveNoNumbersWithSeparators()
        {
            LocTable facts = LocSetUpFixture.Catalog.Facts;
            Assert.That(facts.Count, Is.GreaterThan(0));
            for (int r = 0; r < facts.Count; r++)
            {
                foreach (string locale in facts.Locales)
                {
                    string text = facts.TextAt(r, locale);
                    Assert.That(NumberWithSeparator.IsMatch(text), Is.False,
                        "facts.csv " + facts.KeyAt(r) + " (" + locale + "): «" + text + "» lleva una cifra con separador o decimales; va con {n} y su valor en args (D-018, Q8-A)");
                }
            }

            Assert.That(NumberWithSeparator.IsMatch("Pesa 1.000 kg"), Is.True);
            Assert.That(NumberWithSeparator.IsMatch("Mide 5,2 m"), Is.True);
            Assert.That(NumberWithSeparator.IsMatch("Pesa 2\u00A0000 kg"), Is.True, "miles con espacio duro");
            Assert.That(NumberWithSeparator.IsMatch("Pesa 2 000 kg"), Is.True, "miles con espacio normal");
            Assert.That(NumberWithSeparator.IsMatch("Vive entre 2 000 y 3 000 m"), Is.True);
            Assert.That(NumberWithSeparator.IsMatch("Hay 1 200 000 especies"), Is.True);
            Assert.That(NumberWithSeparator.IsMatch("Mide 35\u00A0m y vive 20 años"), Is.False, "sin separador, tal cual");
            Assert.That(NumberWithSeparator.IsMatch("Entre 20 y 300 frutos"), Is.False, "dos cifras sueltas");
            Assert.That(NumberWithSeparator.IsMatch("Wallace, 1852 200 especies"), Is.False, "un año y otra cifra no son un millar");
        }

        [Test]
        public void EnAndPtBr_AreIncomplete_SoTheAutomaticLanguageStaysEs419()
        {
            LocCatalog catalog = LocSetUpFixture.Catalog;
            Assert.That(catalog.IsComplete(LocLocale.SourceLocale), Is.True, "es-419, completo");
            Assert.That(catalog.IsComplete(LocLocale.English), Is.False);
            Assert.That(catalog.IsComplete(LocLocale.PortugueseBrazil), Is.False);
            foreach (string device in new[] { "en-US", "pt-BR", "es-MX", "fr-FR" })
            {
                LocResolution resolution = LocLocale.Resolve(catalog, null, null, device, false);
                Assert.That(resolution.Locale, Is.EqualTo(LocLocale.SourceLocale), device);
            }

            Assert.That(LocTables.Configure(catalog, "en-XA", true).Source, Is.EqualTo(LocSource.Dev), "af_lang en-XA en desarrollo");
            Assert.That(Loc.Locale, Is.EqualTo(LocLocale.Pseudo));
            Assert.That(LocTables.Configure(catalog, "en-XA", false).Locale, Is.EqualTo(LocLocale.SourceLocale), "en release no hay af_lang");
            Assert.That(LocTables.Configure(catalog, "en", true).Locale, Is.EqualTo(LocLocale.SourceLocale), "en está incompleto");
        }

        [Test]
        public void PseudoLocale_PutsEveryUiTextBetweenBrackets()
        {
            Loc.SetLocale(LocLocale.Pseudo);
            foreach (string key in UiTextKeys())
            {
                string text = Loc.T(key, LocArgs.Of("n", 2L).And("price", "COP 3.900").And("name", "X"));
                Assert.That(text, Does.StartWith("[").And.EndWith("]"), key);
            }

            Assert.That(UiText.Next, Does.StartWith("[").And.EndWith("]"));
            Assert.That(UiText.PlayLevel(12), Does.StartWith("[").And.Contain("12"));
            Assert.That(UiText.MissingSeeds(1000), Does.Contain("1.000"), "cifras de es-CO, como es-419");
            Assert.That(UiText.WorldBanner(2), Does.StartWith("["));
            Assert.That(UiText.FaunaFact("fauna_saimiri_collinsi_dato"), Does.StartWith("[").And.EndWith("]"));
        }

        [Test]
        public void SetUpFixture_ForcesAnotherCulture_AndTheTextsStaySpanish()
        {
            Assert.That(CultureInfo.CurrentCulture.Name, Is.EqualTo(LocSetUpFixture.ForcedCulture));
            Assert.That(Loc.Locale, Is.EqualTo(LocLocale.SourceLocale));
            Assert.That(UiText.Next, Is.EqualTo("Siguiente"));
            Assert.That(UiText.PlayLevel(1), Is.EqualTo("Jugar · Nivel 1"));
            Assert.That(UiText.Grouped(1000), Is.EqualTo("1.000"), "es-CO, no el «1,000» de en-US");
            Assert.That(UiText.RewardLine(1500), Is.EqualTo("Premio: 1.500"));
        }

        /// <summary>
        /// Longitud visible de un texto de la tabla [P] (la que mira <c>max_chars</c>): sin etiquetas de TextMeshPro ni saltos de
        /// línea; un plural cuenta su forma más larga; <c>{}</c> cuenta 2, <c>{sep}</c> 3 (« · ») y cualquier otro marcador, lo
        /// que ocupa escrito (<c>{n}</c> = 3).
        /// </summary>
        internal static int VisibleLength(string s)
        {
            return VisibleLength(s, 0, s.Length);
        }

        private static int VisibleLength(string s, int start, int end)
        {
            int n = 0;
            int i = start;
            while (i < end)
            {
                char c = s[i];
                if (c == '{')
                {
                    int j = i + 1;
                    while (j < end && (char.IsLetterOrDigit(s[j]) || s[j] == '_'))
                    {
                        j++;
                    }

                    if (j < end && s[j] == '}')
                    {
                        int nameLength = j - i - 1;
                        n += nameLength == 0 ? 2 : (string.CompareOrdinal(s, i + 1, LocFormat.SeparatorName, 0, nameLength) == 0 && nameLength == 3 ? 3 : j + 1 - i);
                        i = j + 1;
                        continue;
                    }

                    const string plural = ":plural:";
                    if (string.CompareOrdinal(s, j, plural, 0, plural.Length) == 0)
                    {
                        int k = j + plural.Length;
                        int formStart = k;
                        int depth = 0;
                        int longest = 0;
                        for (; k < end; k++)
                        {
                            char ch = s[k];
                            if (ch == '{')
                            {
                                depth++;
                            }
                            else if (ch == '}' && depth > 0)
                            {
                                depth--;
                            }
                            else if ((ch == '|' || ch == '}') && depth == 0)
                            {
                                longest = Math.Max(longest, VisibleLength(s, formStart, k));
                                formStart = k + 1;
                                if (ch == '}')
                                {
                                    break;
                                }
                            }
                        }

                        n += longest;
                        i = k + 1;
                        continue;
                    }
                }

                if (c == '<')
                {
                    int close = s.IndexOf('>', i);
                    if (close > i + 1 && (char.IsLetter(s[i + 1]) || s[i + 1] == '/'))
                    {
                        i = close + 1;
                        continue;
                    }
                }

                if (c != '\n')
                {
                    n++;
                }

                i++;
            }

            return n;
        }

        [TestCase("Siguiente", 9)]
        [TestCase("Nivel {n}", 9)]
        [TestCase("{n:plural:Te falta {} semilla|Te faltan {} semillas}", 21)]
        [TestCase("+{n} Deshacer\nVer video", 22)]
        [TestCase("<b>{name}</b>{sep}{country}", 18)]
        public void VisibleLength_CountsWhatTheScreenShows(string text, int expected)
        {
            Assert.That(VisibleLength(text), Is.EqualTo(expected));
        }

        /// <summary>Las claves de <c>strings.csv</c> que usa <see cref="UiText"/>: las de <see cref="UiText.Keys"/> y las de los mundos.</summary>
        internal static List<string> UiTextKeys()
        {
            var keys = new List<string>();
            foreach (FieldInfo f in typeof(UiText.Keys).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (f.IsLiteral && f.FieldType == typeof(string))
                {
                    keys.Add((string)f.GetValue(null)!);
                }
            }

            for (int w = 1; w <= WorldTable.WorldCount; w++)
            {
                keys.Add(UiText.WorldNameKey(w));
                keys.Add(UiText.WorldCountryKey(w));
                keys.Add(UiText.WorldRiverKey(w));
            }

            // Los nombres de tienda (S12 y L3): una clave iap_<sku>_name por SKU del catálogo, que pinta StoreSheet.
            foreach (AF.IAP.StoreProduct product in AF.IAP.StoreCatalog.Default.Products)
            {
                keys.Add(product.NameKey);
            }

            // Las 38 frases de TiTino (TI3 y DC25): las claves las da AF.GameFlow.SlothLines y las pinta UiText.TrySlothLine.
            keys.AddRange(UiText.SlothLineKeys);

            // Las del Reto del río (R9; enganche E5): las pinta AF.UI.DailyText.
            keys.AddRange(DailyText.NewKeys);

            // E6 y E7 del Spark (enganche E5b): la tarjeta del 7 y la línea de Sin canoas.
            keys.AddRange(DailyText.E5bKeys);

            // B14: los nombres de las skins de la 1.0 (skin_<id>_name), que pinta el aviso «{skin} · puesta».
            keys.AddRange(UiText.SkinNameKeys);

            // B3: los textos del Salto del Boto, que pinta AF.UI.SkipText.
            foreach (FieldInfo f in typeof(SkipText.Keys).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (f.IsLiteral && f.FieldType == typeof(string))
                {
                    keys.Add((string)f.GetValue(null)!);
                }
            }

            // B6: el título y el cuerpo de cada aviso local (AF.LiveOps.NotificationTypes) y el pre-prompt (PrePromptRequest).
            keys.Add(AF.LiveOps.NotificationTypes.TitleKey);
            foreach (AF.LiveOps.NotificationType type in AF.LiveOps.NotificationTypes.All)
            {
                keys.Add(AF.LiveOps.NotificationTypes.BodyKey(type));
            }

            keys.Add(AF.LiveOps.PrePromptRequest.BodyKey);
            keys.Add(AF.LiveOps.PrePromptRequest.AcceptKey);
            keys.Add(AF.LiveOps.PrePromptRequest.DeclineKey);

            // B6, con la UI pendiente (docs/dev/sesiones-notificaciones.md): el canal de Android (lo usa el programador real,
            // B6-notifications-real), la fila «Avisos» de Ajustes y el aviso del Regalo de regreso. Se quitan de aquí cuando su
            // pantalla los pinte.
            keys.AddRange(new[]
            {
                "push_channel_name", "push_channel_description", "settings_notifications", "settings_notifications_blocked",
                "comeback_streak_line", "comeback_logbook_line", "comeback_reminder_line",
            });

            // La Pororoca del Spark (bloque B2; B2-strings.csv): las pinta AF.UI.ShuffleText.
            keys.AddRange(ShuffleText.AllKeys);

            // B8: «Eliminar cuenta y datos» (Ajustes y su diálogo de dos pasos), que pinta AF.UI.AccountText.
            foreach (FieldInfo f in typeof(AccountText.Keys).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (f.IsLiteral && f.FieldType == typeof(string))
                {
                    keys.Add((string)f.GetValue(null)!);
                }
            }

            return keys;
        }

        private static IEnumerable<LocTable> Both()
        {
            yield return LocSetUpFixture.Catalog.Strings;
            yield return LocSetUpFixture.Catalog.Facts;
        }
    }

    /// <summary>
    /// El idioma de desarrollo <c>af_lang</c> y la sonda del idioma (D-018; tarea L2b): <see cref="LaunchOptions.ResolveDevLanguage"/>
    /// da el gancho de los tests o, en Android y en desarrollo, el extra (<c>--es af_lang en-XA</c>); y la línea
    /// <c>AF_STATE loc</c> lleva el idioma, de dónde salió y la cultura del runtime.
    /// </summary>
    public sealed class LocLaunchTests
    {
        [SetUp]
        public void SetUp()
        {
            LaunchOptions.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
        }

        [Test]
        public void DevLanguage_IsTheTestHook_OrNothingOutsideAndroid()
        {
            Assert.That(LaunchOptions.IntentExtraLanguage, Is.EqualTo("af_lang"));
            Assert.That(LaunchOptions.ResolveDevLanguage(), Is.Null, "sin gancho y fuera de Android, ningún extra");
            LaunchOptions.LanguageOverride = " en-XA ";
            Assert.That(LaunchOptions.ResolveDevLanguage(), Is.EqualTo("en-XA"));
            LaunchOptions.LanguageOverride = "  ";
            Assert.That(LaunchOptions.ResolveDevLanguage(), Is.Null);
            LaunchOptions.LanguageOverride = "en-XA";
            LaunchOptions.Reset();
            Assert.That(LaunchOptions.LanguageOverride, Is.Null, "Reset lo quita");
        }

        [Test]
        public void LocProbe_WritesLocaleSourceAndCulture()
        {
            var probe = new DevProbe();
            string? line = probe.FormatLocaleState(new LocResolution("en-XA", LocSource.Dev), "en-US");
            Assert.That(line, Does.Match("^AF_STATE \\{\"event\":\"loc\",\"t\":\\d+,\"locale\":\"en-XA\",\"source\":\"dev\",\"culture\":\"en-US\"\\}$"));
            Assert.That(probe.FormatLocaleState(new LocResolution("es-419", LocSource.Fallback), string.Empty),
                Does.EndWith(",\"locale\":\"es-419\",\"source\":\"fallback\",\"culture\":\"\"}"), "cultura invariante: vacía");
            Assert.That(probe.FormatLocaleState(new LocResolution("es-419", LocSource.Saved), string.Empty, "es-MX", "jni", "es-MX"),
                Does.EndWith(",\"source\":\"saved\",\"culture\":\"\",\"device\":\"es-MX\",\"device_src\":\"jni\",\"numbers\":\"es-MX\"}"),
                "con la etiqueta que leyó el teléfono y las cifras en uso (tarea L3)");
            Assert.That(AndroidDeviceLocale.SourceKey(AF.Platform.SystemDeviceLocale.Instance), Is.EqualTo("culture"));
            Assert.That(AndroidDeviceLocale.SourceKey(new AF.Platform.ManualDeviceLocale("pt-BR")), Is.EqualTo("manual"));
            Assert.That(AndroidDeviceLocale.ForPlatform(), Is.SameAs(AF.Platform.SystemDeviceLocale.Instance), "fuera de Android, la cultura de .NET");
            var android = new AndroidDeviceLocale(new AF.Platform.ManualDeviceLocale("es-CO"));
            Assert.That(android.LanguageTag, Is.EqualTo("es-CO"), "fuera de Android el JNI no da nada: el respaldo");
            Assert.That(AndroidDeviceLocale.SourceKey(android), Is.EqualTo("culture"));
            Assert.That(DevProbe.SourceKey(LocSource.Saved), Is.EqualTo("saved"));
            Assert.That(DevProbe.SourceKey(LocSource.Device), Is.EqualTo("device"));
            Assert.That(DevProbe.SourceKey(LocSource.Default), Is.EqualTo("default"));
        }
    }

    /// <summary>
    /// El juego arranca aunque falten las tablas de textos o una no sea válida (D-018; tarea L2b, punto 4: «si faltan los
    /// TextAsset, aviso y respaldo es-419»): <see cref="LocTables.FromAssetsOrEmpty"/> decide sin lanzar nunca y
    /// <c>GameBootstrap.SetUpTexts</c> solo registra el aviso o el error. Sin la tabla de UI (o con una rota), Loc queda en
    /// es-419 sin textos: en desarrollo cada texto se ve como «[clave]» y en release, vacío, nunca en otro idioma. Sin la de
    /// textos largos (o con una rota), la UI va con su tabla y sin datos de fauna. El arranque de la escena sin tablas lo
    /// prueba <c>LocBootTests</c> (PlayMode).
    /// </summary>
    public sealed class LocTablesFallbackTests
    {
        private const string FaunaKey = "fauna_saimiri_collinsi_dato";

        // Tabla de UI rota: la clave «next» repetida en la línea 3.
        private const string BrokenStrings = "key,es-419\nnext,Siguiente\nnext,Otra vez\n";

        private LocStateSnapshot _loc;

        [SetUp]
        public void SetUp()
        {
            _loc = LocStateSnapshot.Take();
        }

        [TearDown]
        public void TearDown()
        {
            _loc.Restore();
        }

        [Test]
        public void WithoutTheStringsTable_Es419WithoutTexts_KeysInDevelopment_EmptyInRelease()
        {
            LocCatalog catalog = LocTables.FromAssetsOrEmpty(null, null, out string? warning, out string? error);
            Assert.That(catalog, Is.SameAs(LocCatalog.Empty));
            Assert.That(warning, Is.EqualTo(LocTables.MissingStringsWarning));
            Assert.That(warning, Does.Contain("faltan las tablas de textos"));
            Assert.That(error, Is.Null);

            LocResolution development = LocTables.Configure(catalog, null, true);
            Assert.That(development.Locale, Is.EqualTo(LocLocale.SourceLocale));
            Assert.That(development.Source, Is.EqualTo(LocSource.Fallback));
            Assert.That(Loc.Locale, Is.EqualTo(LocLocale.SourceLocale));
            Assert.That(Loc.Catalog.Strings.Count, Is.EqualTo(0));
            Assert.That(UiText.Next, Is.EqualTo("[next]"), "en desarrollo, la clave entre corchetes");
            Assert.That(UiText.PlayLevel(1), Is.EqualTo("[" + UiText.Keys.PlayLevel + "]"));
            Assert.That(UiText.FaunaFact(FaunaKey), Is.Empty);

            LocResolution release = LocTables.Configure(catalog, LocLocale.Pseudo, false);
            Assert.That(release.Locale, Is.EqualTo(LocLocale.SourceLocale), "en release no hay af_lang");
            Assert.That(UiText.Next, Is.Empty, "en release, sin texto");
            Assert.That(UiText.PlayLevel(1), Is.Empty);
            Assert.That(UiText.FaunaFact(FaunaKey), Is.Empty);
        }

        [Test]
        public void WithoutTheFactsTable_TheUiKeepsItsTexts_AndThereAreNoFaunaFacts()
        {
            string strings = LocTables.ReadProjectFile(LocTables.StringsAssetPath, LocCatalog.StringsFile);
            LocCatalog catalog = LocTables.FromTextsOrEmpty(strings, null, out string? warning, out string? error);
            Assert.That(warning, Is.EqualTo(LocTables.MissingFactsWarning));
            Assert.That(warning, Does.Contain("facts.csv"));
            Assert.That(error, Is.Null);
            Assert.That(catalog.Strings.Count, Is.EqualTo(LocSetUpFixture.Catalog.Strings.Count));
            Assert.That(catalog.Facts.Count, Is.EqualTo(0));

            LocTables.Configure(catalog, null, true);
            Assert.That(UiText.Next, Is.EqualTo("Siguiente"));
            Assert.That(UiText.FaunaFact(FaunaKey), Is.Empty, "sin facts.csv, ningún dato de fauna (ni «[clave]»)");
        }

        [Test]
        public void AnInvalidStringsTable_DoesNotThrow_AndLeavesEs419WithoutTexts()
        {
            string facts = LocTables.ReadProjectFile(LocTables.FactsAssetPath, LocCatalog.FactsFile);
            LocCatalog? catalog = null;
            string? warning = null;
            string? error = null;
            Assert.DoesNotThrow(() => catalog = LocTables.FromTextsOrEmpty(BrokenStrings, facts, out warning, out error));
            Assert.That(catalog, Is.SameAs(LocCatalog.Empty));
            Assert.That(warning, Is.Null);
            Assert.That(error, Does.StartWith("la tabla de textos no es válida: strings.csv, línea 3: la clave «next» está repetida"));
            Assert.That(error, Does.EndWith("La UI irá sin textos en es-419."));

            LocTables.Configure(catalog!, null, true);
            Assert.That(Loc.Locale, Is.EqualTo(LocLocale.SourceLocale));
            Assert.That(UiText.Next, Is.EqualTo("[next]"));
            Assert.That(UiText.FaunaFact(FaunaKey), Is.Empty, "con la UI rota tampoco se leen los textos largos");
            LocTables.Configure(catalog!, null, false);
            Assert.That(UiText.Next, Is.Empty, "en release, sin texto");
        }

        [Test]
        public void AnInvalidFactsTable_KeepsTheUi_WithoutLongTexts()
        {
            string strings = LocTables.ReadProjectFile(LocTables.StringsAssetPath, LocCatalog.StringsFile);
            LocCatalog catalog = LocTables.FromTextsOrEmpty(strings, "key,es-419\n" + FaunaKey + ",\"comillas sin cerrar\n", out string? warning, out string? error);
            Assert.That(warning, Is.Null);
            Assert.That(error, Does.StartWith("la tabla de textos largos no es válida: facts.csv, línea 2"));
            Assert.That(error, Does.EndWith("La UI va con su tabla y sin datos de fauna."));
            Assert.That(catalog.Strings.Count, Is.EqualTo(LocSetUpFixture.Catalog.Strings.Count));
            Assert.That(catalog.Facts.Count, Is.EqualTo(0));

            // Una clave de textos largos que ya está en la UI tampoco vale (LocCatalog: nunca en las dos tablas).
            catalog = LocTables.FromTextsOrEmpty(strings, "key,es-419\nnext,Siguiente\n", out warning, out error);
            Assert.That(warning, Is.Null);
            Assert.That(error, Does.Contain("facts.csv, línea 2: la clave «next» ya está en strings.csv"));
            Assert.That(catalog.Facts.Count, Is.EqualTo(0));

            LocTables.Configure(catalog, null, true);
            Assert.That(UiText.Next, Is.EqualTo("Siguiente"));
            Assert.That(UiText.FaunaFact(FaunaKey), Is.Empty);
        }

        [Test]
        public void WithBothTables_NoWarningAndNoError_AndADestroyedAssetCountsAsMissing()
        {
            var strings = new UnityEngine.TextAsset(LocTables.ReadProjectFile(LocTables.StringsAssetPath, LocCatalog.StringsFile));
            var facts = new UnityEngine.TextAsset(LocTables.ReadProjectFile(LocTables.FactsAssetPath, LocCatalog.FactsFile));
            try
            {
                LocCatalog catalog = LocTables.FromAssetsOrEmpty(strings, facts, out string? warning, out string? error);
                Assert.That(warning, Is.Null);
                Assert.That(error, Is.Null);
                Assert.That(catalog.Strings.Count, Is.EqualTo(LocSetUpFixture.Catalog.Strings.Count));
                Assert.That(catalog.Facts.Count, Is.EqualTo(LocSetUpFixture.Catalog.Facts.Count));
                LocTables.Configure(catalog, null, true);
                Assert.That(UiText.Next, Is.EqualTo("Siguiente"));
                Assert.That(UiText.FaunaFact(FaunaKey), Is.EqualTo("Cruza el bosque en fila, por ramas finas"));

                // Un TextAsset destruido cuenta como que falta (el == de Unity), sin excepción.
                UnityEngine.Object.DestroyImmediate(facts);
                catalog = LocTables.FromAssetsOrEmpty(strings, facts, out warning, out error);
                Assert.That(warning, Is.EqualTo(LocTables.MissingFactsWarning));
                Assert.That(error, Is.Null);
                Assert.That(catalog.Facts.Count, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(strings);
                if (facts != null)
                {
                    UnityEngine.Object.DestroyImmediate(facts);
                }
            }
        }
    }
}
