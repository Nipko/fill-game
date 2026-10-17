#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Calendario de Frutas del río <c>af-facts/1</c> (D-017; 08 §4.6; contrato 31; tarea F1F2) y sus filas de <c>facts.csv</c>,
    /// con la validación de la propuesta de Frutas §6.6:
    /// <list type="bullet">
    /// <item>El archivo real valida: 16 series de 20 líneas, una por nivel, que cubren 1-320 sin huecos ni solapes: 14 de tintes,
    /// ninguna antes del debut de su tinte (glosario §9), y las 2 de río de 261-300 (<c>kind</c> = <c>river</c> [P]; propuesta de
    /// Frutas §5.16, tarea F1c; cargadas en C1a), sin debut que comprobar.</item>
    /// <item>Cada clave del calendario existe en <c>facts.csv</c> con texto es-419 de ≤ 90 caracteres ya formateado, y cada
    /// traducción, de ≤ 117; salvo las cuatro últimas del cacao y del arazá, que el banco de §5.15 aún no tiene
    /// (<see cref="PendingWriting"/>).</item>
    /// <item>Cada línea cita en <c>context</c> al menos una fuente del catálogo, con su URL; lleva su marca [V] o [E] en
    /// <c>notes</c> (ninguna [NV]) y un estado; ningún idioma tiene estado si el español no está aprobado; y todas las fuentes del
    /// catálogo se citan.</item>
    /// <item>Frase de «Fruta nueva» (≤ 40) y nombre visible del tinte de cada serie de tinte (el de <c>glossary.csv</c> lo
    /// comprueba <c>GlossaryTableTests</c>); las de río, sin frase y con su nombre <c>fact.&lt;río&gt;.name</c> [P]. Los
    /// caracteres, contra el atlas: <c>FontCoverageTests</c>.</item>
    /// <item>Cifras (D-018, Q8-A): los marcadores de cada línea son los de su columna <c>args</c> y <c>Loc.Fact</c> da «5.200» con
    /// es-CO y «5,200» con es-MX. En release no sale ninguna línea sin aprobar.</item>
    /// <item>El lector rechaza cada valor fuera del formato con la ruta del valor.</item>
    /// </list>
    /// La prueba de la pseudolocalización a 360 dp y escala 130 % necesita la línea en la pantalla: va con F3.
    /// </summary>
    public sealed class FactCatalogTests
    {
        /// <summary>Ruta del calendario (la de <c>GameSceneBuilder.FruitCalendarPath</c>).</summary>
        internal const string CalendarAssetPath = "Assets/_Project/Data/Content/frutas_del_rio_v1.json";

        /// <summary>
        /// Claves del calendario sin fila todavía: el banco del cacao y del arazá (propuesta de Frutas §5.15) tiene 16 datos y a
        /// cada serie le faltan las líneas 17-20, con el cierre «¡Ya conoces…!». Hasta redactarlas, esos niveles no llevan línea
        /// (<c>Loc.Fact</c> da «»). Ojo, F3 y F4: la línea 20 (niveles 220 y 320) es la del sello «Ya conoces» (D-017, Q5-A) y
        /// hoy no tiene texto; que no salga el sello sin su línea. Redactarlas, con fuente primaria y el cierre, es un pendiente del
        /// dueño y del asesor cultural. Cuando lleguen, esta lista se vacía.
        /// </summary>
        private static readonly string[] PendingWriting =
        {
            "fact.araza.17", "fact.araza.18", "fact.araza.19", "fact.araza.20",
            "fact.cacao.17", "fact.cacao.18", "fact.cacao.19", "fact.cacao.20",
        };

        // Líneas que entran como borrador (D-013, 2026-10-13): repiten datos de llegada del Mapa y no salen en release.
        private static readonly string[] DraftLines = { "fact.agua_tapajos.14", "fact.agua_tapajos.19" };

        // Id de fuente citado en el context: «Fuente A1, …» o «Fuentes T1, …; T2, …».
        private static readonly Regex CitedSource = new Regex("(?:Fuentes? |; )([A-Z][A-Za-z]*[0-9]+), ");
        private static readonly Regex Marker = new Regex("\\{([a-z][a-z0-9_]*)\\}");

        private LocStateSnapshot _loc;

        private static string CalendarPath => LocTables.ProjectPath(CalendarAssetPath);

        private static FactCatalog Real => FactCatalogReader.Read(File.ReadAllText(CalendarPath, Encoding.UTF8));

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
        public void File_IsUtf8WithoutBom_Lf()
        {
            byte[] bytes = File.ReadAllBytes(CalendarPath);
            Assert.That(bytes.Length > 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, Is.False, "sin BOM");
            Assert.That(Array.IndexOf(bytes, (byte)'\r'), Is.EqualTo(-1), "LF");
        }

        [Test]
        public void RealCalendar_Validates_SixteenSeriesOfTwenty_Covering1To320()
        {
            FactCatalog catalog = Real;
            Assert.That(catalog.Version, Is.EqualTo(1));
            Assert.That(catalog.MaxCharsEs, Is.EqualTo(90), "≤ 90 caracteres en español (D-017; 08 §4.6)");
            Assert.That(catalog.Series.Count, Is.EqualTo(16), "las 12 series de 1-200 y 221-260, las 2 de río de 261-300 y el banco del cacao y del arazá");
            string[] order =
            {
                "acai", "agua_tapajos", "urucum", "agua_negro", "cupuacu", "aguaje", "bacaba", "jenipapo", "camu_camu", "tapereba", "cacao",
                "lulo", "guanabana", "rio_napo", "rio_amazonas", "araza",
            };
            for (int i = 0; i < order.Length; i++)
            {
                FactSeries s = catalog.Series[i];
                Assert.That(s.Subject, Is.EqualTo(order[i]), "orden de la propuesta de Frutas §3.2");
                bool river = s.Subject.StartsWith("rio_", StringComparison.Ordinal);
                Assert.That(s.Kind, Is.EqualTo(river ? FactSeriesKind.River : FactSeriesKind.Tint), s.Subject + ": las de río, kind river [P] (§5.16)");
                Assert.That(s.Lines, Is.EqualTo(20), s.Subject + ": 20 líneas por serie (D-017, Q2-A)");
                Assert.That(s.Step, Is.EqualTo(1), s.Subject + ": una por nivel");
                Assert.That(s.FromLevel, Is.EqualTo(1 + (20 * i)), s.Subject);
            }

            var schedule = new FactSchedule(catalog);
            for (int level = 1; level <= 340; level++)
            {
                bool covered = level <= 320;
                Assert.That(schedule.KeyFor(level) != null, Is.EqualTo(covered), "nivel " + level.ToString(CultureInfo.InvariantCulture)
                    + (covered ? " sin línea" : " con línea: la versión 1 acaba en el 320"));
            }

            Assert.That(schedule.KeyFor(261), Is.EqualTo("fact.rio_napo.01"), "propuesta de Frutas §5.16.1");
            Assert.That(schedule.KeyFor(300), Is.EqualTo("fact.rio_amazonas.20"), "propuesta de Frutas §5.16.2");
        }

        [Test]
        public void EverySeries_StartsAtOrAfterTheDebutOfItsTint()
        {
            Dictionary<string, int> debut = TintDebuts();
            Assert.That(debut.Count, Is.EqualTo(14), "K_paleta = 14 (glosario §9)");
            int rivers = 0;
            foreach (FactSeries s in Real.Series)
            {
                if (s.Kind == FactSeriesKind.River)
                {
                    // Las series de río no son tintes: no tienen debut que comprobar (propuesta de Frutas §5.16).
                    Assert.That(debut.ContainsKey(s.Subject), Is.False, s.Subject + " no es un tinte del glosario §9");
                    rivers++;
                    continue;
                }

                Assert.That(debut.ContainsKey(s.Subject), Is.True, s.Subject + " es un tinte del glosario §9");
                Assert.That(s.FromLevel, Is.GreaterThanOrEqualTo(debut[s.Subject]), s.Subject + ": nunca antes del debut (propuesta §3.1, regla 1)");
            }

            Assert.That(rivers, Is.EqualTo(2), "rio_napo y rio_amazonas");
            Assert.That(Real.SeriesOf("lulo")!.FromLevel, Is.EqualTo(221), "el lulo debuta en el 111 y su serie va en el Oriente (regla 3)");
        }

        [Test]
        public void EveryKey_HasItsLine_WithinNinetyCharacters_AndTranslationsWithinThirtyPercentMore()
        {
            FactCatalog catalog = Real;
            LocTable facts = LocSetUpFixture.Catalog.Facts;
            var problems = new StringBuilder();
            int lines = 0;
            foreach (FactSeries s in catalog.Series)
            {
                for (int k = 1; k <= s.Lines; k++)
                {
                    string key = s.KeyAt(k);
                    int row = facts.IndexOf(key);
                    if (Array.IndexOf(PendingWriting, key) >= 0)
                    {
                        Assert.That(row, Is.EqualTo(-1), key + " ya tiene fila: quítala de PendingWriting");
                        Assert.That(Loc.Fact(key), Is.Empty, key + ": sin fila, el nivel no lleva línea");
                        continue;
                    }

                    if (row < 0)
                    {
                        problems.Append(key).Append(": sin fila en facts.csv\n");
                        continue;
                    }

                    lines++;
                    Assert.That(facts.MaxCharsAt(row), Is.EqualTo(catalog.MaxCharsEs), key + ": max_chars");
                    string shown = Loc.Fact(key);
                    if (shown.Length == 0 || shown.Length > catalog.MaxCharsEs || LocTablesTests.VisibleLength(facts.TextAt(row, LocLocale.SourceLocale)) > catalog.MaxCharsEs)
                    {
                        problems.Append(key).Append(" (es-419): «").Append(shown).Append("» mide ").Append(shown.Length.ToString(CultureInfo.InvariantCulture)).Append('\n');
                    }

                    foreach (string locale in facts.Locales)
                    {
                        string text = facts.TextAt(row, locale);
                        if (locale != LocLocale.SourceLocale && text.Length > 0 && LocTablesTests.VisibleLength(text) * 10 > catalog.MaxCharsEs * 13)
                        {
                            problems.Append(key).Append(" (").Append(locale).Append("): pasa de 117\n");
                        }
                    }
                }
            }

            Assert.That(problems.ToString(), Is.Empty);
            Assert.That(lines, Is.EqualTo(240 + 16 + 16 + 40), "las 240 líneas de las 12 series, los 16 datos del banco del cacao y del arazá y las 40 de río de 261-300");
        }

        [Test]
        public void EveryLine_CitesSourcesOfTheCatalog_WithMarkAndStatus_AndEverySourceIsCited()
        {
            FactCatalog catalog = Real;
            LocTable facts = LocSetUpFixture.Catalog.Facts;
            var cited = new HashSet<string>(StringComparer.Ordinal);
            foreach (FactSeries s in catalog.Series)
            {
                string tintName = facts.TextAt(facts.IndexOf(s.NameKey), LocLocale.SourceLocale);
                Assert.That(tintName, Is.Not.Empty, s.NameKey);
                for (int k = 1; k <= s.Lines; k++)
                {
                    int row = facts.IndexOf(s.KeyAt(k));
                    if (row < 0)
                    {
                        continue;
                    }

                    string key = s.KeyAt(k);
                    string context = facts.ContextAt(row);
                    MatchCollection ids = CitedSource.Matches(context);
                    Assert.That(ids.Count, Is.GreaterThan(0), key + ": cita al menos una fuente en context");
                    foreach (Match m in ids)
                    {
                        FactSource? source = catalog.Source(m.Groups[1].Value);
                        Assert.That(source, Is.Not.Null, key + ": la fuente " + m.Groups[1].Value + " está en el catálogo");
                        Assert.That(context, Does.Contain(source!.Url), key + ": con la URL de " + source.Id);
                        cited.Add(source.Id);
                    }

                    string notes = facts.NotesAt(row);
                    Assert.That(notes.StartsWith("[V]", StringComparison.Ordinal) || notes.StartsWith("[E]", StringComparison.Ordinal), Is.True, key + ": marca [V] o [E] en notes");
                    Assert.That(notes, Does.Not.Contain("[NV]"), key + ": ninguna [NV] (propuesta §5.14)");
                    Match serie = Regex.Match(context, "línea " + k.ToString(CultureInfo.InvariantCulture) + "/" + s.Lines.ToString(CultureInfo.InvariantCulture)
                        + " de la serie " + Regex.Escape(s.Subject) + @" \(" + Regex.Escape(tintName) + @"; (.+?)\), en la victoria del nivel "
                        + s.LevelAt(k).ToString(CultureInfo.InvariantCulture) + ",");
                    Assert.That(serie.Success, Is.True, key + ": context «línea k/n de la serie <tinte> (<Nombre>; <nombre científico>), en la victoria del nivel N,»");
                    string scientific = serie.Groups[1].Value;
                    bool water = s.Subject.StartsWith("agua_", StringComparison.Ordinal);
                    bool river = s.Kind == FactSeriesKind.River;
                    string pattern = river ? @"^río [A-Z]\p{L}+ \(agua [a-z]+\)( y .+ \(agua [a-z]+\))?$"
                        : water ? @"^río [A-Z]\p{L}+ \(agua [a-z]+\)$" : "^[A-Z][a-z]+ [a-z]+$";
                    Assert.That(Regex.IsMatch(scientific, pattern), Is.True,
                        key + ": nombre científico «" + scientific + "» (en las aguas y los ríos, el río y su tipo de agua)");

                    LocStatus status = facts.StatusAt(row, LocLocale.SourceLocale);
                    bool draft = Array.IndexOf(DraftLines, key) >= 0;
                    Assert.That(status, Is.EqualTo(draft ? LocStatus.Draft : LocStatus.PendingAdvisor), key + (draft ? ": borrador (D-013)" : ": nace pendiente del asesor (07 §8.2)"));
                    foreach (string locale in facts.Locales)
                    {
                        if (locale != LocLocale.SourceLocale && status < LocStatus.ApprovedEs)
                        {
                            Assert.That(facts.StatusAt(row, locale), Is.EqualTo(LocStatus.Missing), key + " (" + locale + "): sin estado mientras el español no esté aprobado");
                        }
                    }
                }
            }

            var orphans = new List<string>();
            foreach (FactSource source in catalog.Sources)
            {
                if (!cited.Contains(source.Id))
                {
                    orphans.Add(source.Id);
                }
            }

            Assert.That(orphans, Is.Empty, "fuentes del catálogo que ninguna línea cita");
        }

        [Test]
        public void EverySeries_HasItsFruitNewPhrase_AndItsTintName()
        {
            LocTable facts = LocSetUpFixture.Catalog.Facts;
            foreach (FactSeries s in Real.Series)
            {
                if (s.Kind == FactSeriesKind.River)
                {
                    // Las de río [P] no tienen tarjeta «Fruta nueva» (no debuta ningún tinte) y su nombre es fact.<río>.name (§5.16).
                    Assert.That(s.TagKey, Is.Null, s.Subject);
                    Assert.That(facts.IndexOf("fact." + s.Subject + ".tag"), Is.EqualTo(-1), s.Subject + ": sin frase de «Fruta nueva»");
                    Assert.That(s.NameKey, Is.EqualTo("fact." + s.Subject + ".name"));
                    int riverName = facts.IndexOf(s.NameKey);
                    Assert.That(riverName, Is.GreaterThanOrEqualTo(0), s.NameKey);
                    Assert.That(facts.MaxCharsAt(riverName), Is.EqualTo(24), s.NameKey + ": como los nombres de tinte");
                    Assert.That(facts.StatusAt(riverName, LocLocale.SourceLocale), Is.EqualTo(LocStatus.PendingAdvisor), s.NameKey);
                    continue;
                }

                Assert.That(s.TagKey, Is.EqualTo("fact." + s.Subject + ".tag"));
                Assert.That(s.NameKey, Is.EqualTo("tint." + s.Subject + ".name"));
                int tag = facts.IndexOf(s.TagKey!);
                Assert.That(tag, Is.GreaterThanOrEqualTo(0), s.TagKey);
                Assert.That(facts.MaxCharsAt(tag), Is.EqualTo(40), s.TagKey + ": ≤ 40 caracteres (propuesta §5.1)");
                Assert.That(facts.TextAt(tag, LocLocale.SourceLocale).Length, Is.LessThanOrEqualTo(40), s.TagKey);
                Assert.That(facts.StatusAt(tag, LocLocale.SourceLocale), Is.EqualTo(LocStatus.PendingAdvisor), s.TagKey);
                int name = facts.IndexOf(s.NameKey);
                Assert.That(name, Is.GreaterThanOrEqualTo(0), s.NameKey);
                Assert.That(facts.StatusAt(name, LocLocale.SourceLocale), Is.EqualTo(LocStatus.PendingAdvisor), s.NameKey);
                Assert.That(Loc.Fact(s.TagKey), Does.StartWith(Loc.Fact(s.NameKey) == "Agua del Río Negro" ? "Río Negro" : Loc.Fact(s.NameKey)),
                    s.TagKey + ": «Nombre: frase»");
            }

            Assert.That(Loc.Fact("fact.acai.tag"), Is.EqualTo("Açaí: la crema morada del\u00A0Pará"), "con su espacio duro: la última palabra no queda sola (F5)");
            Assert.That(Loc.Fact("tint.urucum.name"), Is.EqualTo("Achiote"));
            Assert.That(Loc.Fact("fact.rio_napo.name"), Is.EqualTo("Napo y Cuyabeno"), "propuesta de Frutas §5.16 [P]");
            Assert.That(Loc.Fact("fact.rio_amazonas.name"), Is.EqualTo("Amazonas"));
        }

        [Test]
        public void Numbers_GoAsMarkersWithTheirArgs_AndFollowTheRegion()
        {
            LocTable facts = LocSetUpFixture.Catalog.Facts;
            int withArgs = 0;
            for (int r = 0; r < facts.Count; r++)
            {
                string key = facts.KeyAt(r);
                if (!key.StartsWith(FactCatalog.KeyPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                var markers = new List<string>();
                foreach (Match m in Marker.Matches(facts.TextAt(r, LocLocale.SourceLocale)))
                {
                    markers.Add(m.Groups[1].Value);
                }

                LocArgs args = facts.ArgsAt(r);
                Assert.That(args.Count, Is.EqualTo(markers.Count), key + ": un valor en args por marcador");
                foreach (string name in markers)
                {
                    Assert.That(args.TryGet(name, out _), Is.True, key + ": {" + name + "} con su valor en args");
                }

                withArgs += args.Count > 0 ? 1 : 0;
            }

            Assert.That(withArgs, Is.EqualTo(17 + 8), "las líneas con cifras con separador o decimales (propuesta §5, Q8-A): 17 de fruta y 8 de río (§5.16)");

            Assert.That(Loc.Fact("fact.acai.17"), Is.EqualTo("Una prima del açaí es el árbol más común de la Amazonia: hay unos 5.200 millones."), "es-419 sin región: es-CO [P]");
            Assert.That(Loc.Fact("fact.cupuacu.11"), Does.EndWith("puede llegar a 4,5 kg."), "decimal y espacio duro antes de la unidad (08 §4.7)");
            Assert.That(Loc.Fact("fact.camu_camu.11"), Does.EndWith("va de 2,3 a 3,5."));
            Assert.That(Loc.Fact("fact.rio_napo.05"), Is.EqualTo("Su cuenca ocupa cerca de 100.520\u00A0km², entre Ecuador y el Perú."), "§5.16.1, k05");
            Assert.That(Loc.Fact("fact.rio_amazonas.10"), Does.Contain("unos 1,3 millones de toneladas"), "decimal (§5.16.2, k10)");

            Loc.Configure(LocSetUpFixture.Catalog, LocLocale.SourceLocale, true, "es-CO");
            Assert.That(Loc.Fact("fact.acai.17"), Does.Contain("unos 5.200 millones"), "es-CO");
            Loc.Configure(LocSetUpFixture.Catalog, LocLocale.SourceLocale, true, "es-MX");
            Assert.That(Loc.Fact("fact.acai.17"), Does.Contain("unos 5,200 millones"), "es-MX");
            Assert.That(Loc.Fact("fact.cupuacu.11"), Does.Contain("4.5 kg"), "es-MX");
        }

        /// <summary>
        /// Sistema métrico con U+00A0 entre la cifra (o el marcador) y la unidad (08 §4.7; <c>.claude/rules/localizacion.md</c>):
        /// con un espacio normal, en una pantalla estrecha «400» y «g» pueden quedar en líneas distintas.
        /// </summary>
        [Test]
        public void Units_GoAfterANoBreakSpace()
        {
            LocTable facts = LocSetUpFixture.Catalog.Facts;
            var unit = new Regex(@"[0-9}]( |\u00A0)(?:g|kg|mg|cm|mm|m|km|%|°C)(?![\p{L}0-9])");
            var problems = new StringBuilder();
            int units = 0;
            for (int r = 0; r < facts.Count; r++)
            {
                string key = facts.KeyAt(r);
                if (!key.StartsWith(FactCatalog.KeyPrefix, StringComparison.Ordinal) && !key.StartsWith(FactCatalog.NamePrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (string locale in facts.Locales)
                {
                    foreach (Match m in unit.Matches(facts.TextAt(r, locale)))
                    {
                        units++;
                        if (m.Groups[1].Value == " ")
                        {
                            problems.Append(key).Append(" (").Append(locale).Append("): «").Append(m.Value).Append("» con un espacio normal").Append('\n');
                        }
                    }
                }
            }

            Assert.That(problems.ToString(), Is.Empty);
            Assert.That(units, Is.GreaterThanOrEqualTo(46), "las 46 cifras con unidad de las líneas de fruta de la versión 1");
        }

        [Test]
        public void InRelease_NoUnapprovedLineIsShown()
        {
            Loc.Configure(LocSetUpFixture.Catalog, LocLocale.SourceLocale, false);
            foreach (FactSeries s in Real.Series)
            {
                for (int k = 1; k <= s.Lines; k++)
                {
                    Assert.That(Loc.Fact(s.KeyAt(k)), Is.Empty, s.KeyAt(k) + ": sin aprobar, no sale en release (08 §4.7)");
                }

                if (s.TagKey != null)
                {
                    Assert.That(Loc.Fact(s.TagKey), Is.Empty, s.TagKey);
                }

                Assert.That(Loc.Fact(s.NameKey), Is.Empty, s.NameKey);
            }
        }

        // ---------------------------------------------------------------------------------------------- carga

        /// <summary>
        /// La escena <c>Game</c> que regenera <c>GameSceneBuilder.CreateGameScene</c> lleva el calendario en el campo
        /// <c>fruitCalendar</c> de <see cref="GameBootstrap"/> (se lee el YAML, nunca se edita), y
        /// <see cref="GameBootstrap.LoadFruitFacts"/> lo lee; sin él o sin validar, un aviso o un error y null (el juego sigue sin
        /// líneas de fruta, como las paradas del Mapa).
        /// </summary>
        [Test]
        public void Scene_CarriesTheCalendar_AndTheBootstrapLoadsIt()
        {
            string meta = File.ReadAllText(CalendarPath + ".meta");
            Match guid = Regex.Match(meta, "^guid: ([0-9a-f]{32})$", RegexOptions.Multiline);
            Assert.That(guid.Success, Is.True, "el .meta del calendario lo genera Unity");
            string scene = File.ReadAllText(LocTables.ProjectPath("Assets/_Project/Scenes/Game.unity"));
            Assert.That(scene, Does.Contain("fruitCalendar: {fileID: 4900000, guid: " + guid.Groups[1].Value + ", type: 3}"),
                "Game.unity regenerada con el calendario (GameSceneBuilder.CreateGameScene)");

            var asset = new TextAsset(File.ReadAllText(CalendarPath, Encoding.UTF8));
            try
            {
                FactSchedule? schedule = GameBootstrap.LoadFruitFacts(asset);
                Assert.That(schedule, Is.Not.Null);
                Assert.That(schedule!.KeyFor(3), Is.EqualTo("fact.acai.03"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void LoadFruitFacts_WithoutTheFileOrInvalid_WarnsOrLogs_AndGivesNull()
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new Regex("falta el calendario de Frutas del río"));
            Assert.That(GameBootstrap.LoadFruitFacts(null), Is.Null);

            var broken = new TextAsset("{\"format\": \"af-facts/1\", \"version\": 1, \"max_chars_es\": 90, \"series\": [{\"subject\": \"acai\"}], \"sources\": {}}");
            try
            {
                UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new Regex(@"series\[0\]\.kind.*sin líneas de fruta"));
                Assert.That(GameBootstrap.LoadFruitFacts(broken), Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(broken);
            }
        }

        // ---------------------------------------------------------------------------------------------- lector

        private const string Valid = "{\"format\": \"af-facts/1\", \"version\": 1, \"max_chars_es\": 90, \"series\": ["
            + "{\"subject\": \"acai\", \"kind\": \"tint\", \"from_level\": 1, \"lines\": 20, \"step\": 1}, "
            + "{\"subject\": \"urucum\", \"kind\": \"tint\", \"from_level\": 21, \"lines\": 10, \"step\": 2}], "
            + "\"sources\": {\"A1\": {\"title\": \"Embrapa\", \"url\": \"https://example.org/a1\"}, \"Gb2\": {\"title\": \"Kew\", \"url\": \"http://example.org/gb2\"}}}";

        [Test]
        public void Reader_ReadsAValidCalendar_WithItsKeys()
        {
            FactCatalog c = FactCatalogReader.Read(Valid);
            Assert.That(c.Series.Count, Is.EqualTo(2));
            FactSeries urucum = c.SeriesOf("urucum")!;
            Assert.That(urucum.FromLevel, Is.EqualTo(21));
            Assert.That(urucum.LastLevel, Is.EqualTo(39));
            Assert.That(urucum.EndLevel, Is.EqualTo(41));
            Assert.That(urucum.KeyAt(1), Is.EqualTo("fact.urucum.01"));
            Assert.That(urucum.KeyAt(10), Is.EqualTo("fact.urucum.10"));
            Assert.That(urucum.FactIdAt(3), Is.EqualTo("urucum.03"));
            Assert.That(urucum.LevelAt(3), Is.EqualTo(25));
            Assert.That(urucum.TagKey, Is.EqualTo("fact.urucum.tag"));
            Assert.That(urucum.NameKey, Is.EqualTo("tint.urucum.name"));
            Assert.That(() => urucum.KeyAt(0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => urucum.KeyAt(11), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(c.Sources.Count, Is.EqualTo(2));
            Assert.That(c.Sources[0].Id, Is.EqualTo("A1"), "en el orden del archivo");
            Assert.That(c.Source("Gb2")!.Title, Is.EqualTo("Kew"));
            Assert.That(c.Source("X9"), Is.Null);
            Assert.That(c.SeriesOf("lulo"), Is.Null);
        }

        [TestCase("\"format\": \"af-facts/1\"", "\"format\": \"af-facts/2\"", "format")]
        [TestCase("\"version\": 1", "\"version\": 0", "version")]
        [TestCase("\"max_chars_es\": 90", "\"max_chars_es\": 0", "max_chars_es")]
        [TestCase("\"max_chars_es\": 90", "\"max_chars_es\": 90.5", "max_chars_es")]
        [TestCase("\"subject\": \"acai\"", "\"subject\": \"Acai\"", "series[0].subject")]
        [TestCase("\"subject\": \"urucum\"", "\"subject\": \"acai\"", "series[1].subject")]
        [TestCase("\"kind\": \"tint\", \"from_level\": 1", "\"kind\": \"especie\", \"from_level\": 1", "series[0].kind")]
        [TestCase("\"kind\": \"tint\", \"from_level\": 1", "\"kind\": \"River\", \"from_level\": 1", "series[0].kind")]
        [TestCase("\"from_level\": 1,", "\"from_level\": 0,", "series[0].from_level")]
        [TestCase("\"lines\": 20", "\"lines\": 0", "series[0].lines")]
        [TestCase("\"lines\": 20", "\"lines\": 100", "series[0].lines")]
        [TestCase("\"step\": 2", "\"step\": 0", "series[1].step")]
        [TestCase("\"step\": 2", "\"step\": 1001", "series[1].step")]
        [TestCase("\"lines\": 10, \"step\": 2", "\"lines\": 1, \"step\": 2147483647", "series[1].step")]
        [TestCase("\"from_level\": 21", "\"from_level\": 20", "series[1].from_level")]
        [TestCase("\"from_level\": 21", "\"from_level\": 995", "series[1]")]
        [TestCase("\"A1\": {", "\"a1\": {", "sources.a1")]
        [TestCase("\"A1\": {", "\"A\": {", "sources.A")]
        [TestCase("\"title\": \"Embrapa\"", "\"title\": \" \"", "sources.A1.title")]
        [TestCase("\"url\": \"https://example.org/a1\"", "\"url\": \"example.org/a1\"", "sources.A1.url")]
        [TestCase("\"url\": \"https://example.org/a1\"", "\"url\": \"https://example.org/a 1\"", "sources.A1.url")]
        public void Reader_RejectsEachValueOutOfTheFormat_WithItsPath(string from, string to, string path)
        {
            Assert.That(Valid, Does.Contain(from));
            string json = ReplaceFirst(Valid, from, to);
            var e = Assert.Throws<LevelFormatException>(() => FactCatalogReader.Read(json));
            Assert.That(e!.Message, Does.Contain(path + ":"), e.Message);
        }

        [Test]
        public void Reader_ReadsARiverSeries_WithItsNameAndWithoutTag()
        {
            // kind river [P] (propuesta de Frutas §5.16; tarea C1a): el nombre es fact.<río>.name y no hay frase de «Fruta nueva».
            FactCatalog c = FactCatalogReader.Read(ReplaceFirst(Valid, "\"subject\": \"urucum\", \"kind\": \"tint\"", "\"subject\": \"rio_napo\", \"kind\": \"river\""));
            FactSeries napo = c.SeriesOf("rio_napo")!;
            Assert.That(napo.Kind, Is.EqualTo(FactSeriesKind.River));
            Assert.That(napo.KeyAt(1), Is.EqualTo("fact.rio_napo.01"));
            Assert.That(napo.FactIdAt(1), Is.EqualTo("rio_napo.01"), "fact_id de af_river_fact_shown (08 §8.2)");
            Assert.That(napo.NameKey, Is.EqualTo("fact.rio_napo.name"));
            Assert.That(napo.TagKey, Is.Null);
            Assert.That(c.SeriesOf("acai")!.Kind, Is.EqualTo(FactSeriesKind.Tint));
        }

        [Test]
        public void Series_RejectsEachValueOutOfRange_NamingItsParameter()
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => new FactSeries("acai", FactSeriesKind.Tint, 0, 20, 1))!.ParamName, Is.EqualTo("fromLevel"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => new FactSeries("acai", FactSeriesKind.Tint, 1, 0, 1))!.ParamName, Is.EqualTo("lines"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => new FactSeries("acai", FactSeriesKind.Tint, 1, 100, 1))!.ParamName, Is.EqualTo("lines"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => new FactSeries("acai", FactSeriesKind.Tint, 1, 20, 0))!.ParamName, Is.EqualTo("step"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => new FactSeries("acai", FactSeriesKind.Tint, 1, 1, int.MaxValue))!.ParamName, Is.EqualTo("step"),
                "from_level + lines × step no cabe en un int");
            Assert.That(new FactSeries("acai", FactSeriesKind.Tint, 1, 1, int.MaxValue - 1).EndLevel, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void Reader_RejectsAnEmptyOrMissingSeriesOrSources_AndInvalidJson()
        {
            Assert.Throws<LevelFormatException>(() => FactCatalogReader.Read("{\"format\": \"af-facts/1\""));
            Assert.Throws<LevelFormatException>(() => FactCatalogReader.Read("[]"));
            string noSeries = Regex.Replace(Valid, "\"series\": \\[.*\\], \"sources\"", "\"series\": [], \"sources\"");
            Assert.That(Assert.Throws<LevelFormatException>(() => FactCatalogReader.Read(noSeries))!.Message, Does.Contain("series:"));
            string noSources = Regex.Replace(Valid, "\"sources\": \\{.*\\}\\}$", "\"sources\": {}}");
            Assert.That(Assert.Throws<LevelFormatException>(() => FactCatalogReader.Read(noSources))!.Message, Does.Contain("sources:"));
        }

        // ---------------------------------------------------------------------------------------------- ayudas

        /// <summary>Nivel de debut de cada tinte (columna «Debut (mundo · nivel)» del glosario §9).</summary>
        private static Dictionary<string, int> TintDebuts()
        {
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "docs", "gdd", "00-glosario.md");
            string text = File.ReadAllText(path).Replace("\r\n", "\n");
            int start = text.IndexOf("\n## 9. ", StringComparison.Ordinal);
            int end = text.IndexOf("\n## 10. ", start + 1, StringComparison.Ordinal);
            Assert.That(start >= 0 && end > start, Is.True, "glosario §9");
            var debut = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (string line in text.Substring(start, end - start).Split('\n'))
            {
                Match m = Regex.Match(line, "^\\| `([a-z_]+)` \\| [0-9]+ \\|.*\\| [0-9]+ · ([0-9]+) \\|$");
                if (m.Success)
                {
                    debut.Add(m.Groups[1].Value, int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture));
                }
            }

            return debut;
        }

        private static string ReplaceFirst(string text, string from, string to)
        {
            int at = text.IndexOf(from, StringComparison.Ordinal);
            return text.Substring(0, at) + to + text.Substring(at + from.Length);
        }
    }
}
