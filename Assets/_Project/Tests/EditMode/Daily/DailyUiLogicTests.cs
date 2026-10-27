#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AF.Economy;
using AF.GameFlow;
using AF.LiveOps;
using AF.Localization;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode.Daily
{
    /// <summary>
    /// Lo del Reto del río de AF.UI que no necesita escena (tarea R9): las claves nuevas de
    /// <c>docs/dev/traspaso/spark-strings-reto.csv</c> (cabecera, columnas obligatorias, que no pisan <c>strings.csv</c> y que
    /// se leen con el lector de tablas), <see cref="DailyText"/> con una tabla de prueba propia (las tablas del proyecto más esas
    /// filas) y con una clave ausente (el «[clave]» de desarrollo y «» de release de <see cref="Loc"/>), y
    /// <see cref="DailyLevelStateStore"/> (R-Q4). Ensamblado propio porque <c>AF.Tests.EditMode</c> no referencia AF.LiveOps
    /// (enganche opcional E13).
    /// </summary>
    public sealed class DailyUiLogicTests
    {
        private const string CsvPath = "docs/dev/traspaso/spark-strings-reto.csv";

        /// <summary>
        /// Claves del Spark de E6 y E7 cuyas filas entran en <c>strings.csv</c> con el enganche E5b
        /// (<c>docs/dev/traspaso/spark-enganches/E5b.diff</c>): hasta aplicarlo faltan enteras; aplicado, tienen que estar tal cual,
        /// como las demás. Con E5b integrado, esta lista se vacía.
        /// </summary>
        private static readonly string[] PendingE5bKeys = DailyText.E5bKeys;

        private const string Header = "key,es-419,en,pt-BR,context,max_chars,status,status_en,status_pt-BR,notes,args";

        private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);

        [TearDown]
        public void TearDown()
        {
            Loc.Reset();
        }

        // ---- Tabla de claves del Reto (enganche E5) ----

        [Test]
        public void Csv_HasTheExactHeader_AndEveryNewKey_WithContextMaxCharsAndDraftStatus()
        {
            IReadOnlyList<LocCsvRow> rows = ReadCsvRows(out string text);
            Assert.That(text.Split('\n')[0].TrimEnd('\r'), Is.EqualTo(Header));
            Assert.That(text.IndexOf('\r'), Is.EqualTo(-1), "LF");
            Assert.That(text.Length > 0 && text[0] == '﻿', Is.False, "sin BOM");

            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 1; i < rows.Count; i++)
            {
                IReadOnlyList<string> f = rows[i].Fields;
                Assert.That(f.Count, Is.EqualTo(11), "fila " + i);
                string key = f[0];
                Assert.That(keys.Add(key), Is.True, "clave repetida " + key);
                Assert.That(LocTable.IsValidKey(key), Is.True, key);
                Assert.That(f[1], Is.Not.Empty, key + ": texto es-419");
                Assert.That(f[4], Is.Not.Empty, key + ": context");
                Assert.That(int.TryParse(f[5], out int max) && max >= 1, Is.True, key + ": max_chars");
                // La misma regla que LocTablesTests.MaxChars_HoldsEs419_AndTranslationsGetThirtyPercentMore: es-419 cabe en
                // max_chars y cada traducción en max_chars × 1,3, con la longitud visible (marcadores como se escriben).
                Assert.That(VisibleLength(f[1]), Is.LessThanOrEqualTo(max), key + ": el texto es-419 cabe en max_chars");
                Assert.That((VisibleLength(f[2]) * 10) <= (max * 13), Is.True, key + ": en cabe en max_chars × 1,3");
                Assert.That((VisibleLength(f[3]) * 10) <= (max * 13), Is.True, key + ": pt-BR cabe en max_chars × 1,3");
                Assert.That(f[6], Is.EqualTo("borrador").Or.EqualTo("pendiente_asesor").Or.EqualTo("aprobado_es"), key + ": status (aprobado_es tras la revisión de D-054)");
                Assert.That(f[1].Contains("{0}") || f[1].Contains("{1}"), Is.False, key + ": marcadores con nombre");
            }

            foreach (string key in DailyText.NewKeys)
            {
                Assert.That(keys.Contains(key), Is.True, "falta " + key + " en " + CsvPath);
            }

            foreach (string key in DailyText.E5bKeys)
            {
                Assert.That(keys.Contains(key), Is.True, "falta " + key + " (E5b) en " + CsvPath);
            }

            Assert.That(keys.Count, Is.EqualTo(DailyText.NewKeys.Length + DailyText.E5bKeys.Length), "ninguna fila sobra");
        }

        [Test]
        public void Csv_RowsAreInStringsCsv_E5_AndReusedKeysExist()
        {
            LocCatalog project = LocTables.FromProjectFiles();
            string strings = LocTables.ReadProjectFile(LocTables.StringsAssetPath, LocCatalog.StringsFile);
            IReadOnlyList<LocCsvRow> rows = ReadCsvRows(out string daily);
            int pending = 0;
            for (int i = 1; i < rows.Count; i++)
            {
                // E5: la misma fila, tal cual (las filas del Reto se insertaron en orden ordinal por clave). Las de E5b
                // (PendingE5bKeys) pueden faltar enteras mientras no se aplique E5b; nunca estar con otro texto.
                string key = rows[i].Fields[0];
                if (IsPendingE5b(project, key))
                {
                    pending++;
                    continue;
                }

                Assert.That(project.Strings.IndexOf(key), Is.GreaterThanOrEqualTo(0), key + " falta en strings.csv (E5)");
            }

            Assert.That(pending == 0 || pending == PendingE5bKeys.Length, Is.True, "E5b entra entero o no entra");
            string[] lines = daily.Substring(daily.IndexOf('\n') + 1).Split('\n');
            foreach (string line in lines)
            {
                if (line.Length > 0 && !IsPendingE5b(project, line.Substring(0, line.IndexOf(','))))
                {
                    Assert.That(strings.Contains(line + "\n"), Is.True, "fila distinta en strings.csv: " + line);
                }
            }

            foreach (string key in DailyText.ReusedKeys)
            {
                Assert.That(project.Strings.IndexOf(key), Is.GreaterThanOrEqualTo(0), key + " se reutiliza y tiene que existir");
            }
        }

        [Test]
        public void StringsCsv_WithTheRetoRows_ParsesWithTheLocReader()
        {
            Assert.That(() => TestCatalog(), Throws.Nothing);
            LocCatalog catalog = TestCatalog();
            foreach (string key in DailyText.NewKeys)
            {
                Assert.That(catalog.Strings.IndexOf(key), Is.GreaterThanOrEqualTo(0), key);
            }

            foreach (string key in DailyText.E5bKeys)
            {
                Assert.That(catalog.Strings.IndexOf(key), Is.GreaterThanOrEqualTo(0), key + " (E5b o la fila del Spark detrás)");
            }
        }

        // ---- DailyText con la tabla de prueba ----

        [Test]
        public void DailyText_WithTheTestTable_FormatsTheCardTexts()
        {
            Loc.Configure(TestCatalog(), LocLocale.SourceLocale, true);
            var date = new DateTime(2026, 10, 19);

            Assert.That(DailyText.Title, Is.EqualTo("Reto del río"));
            Assert.That(DailyText.DateShort(date), Is.EqualTo("19 oct"));
            Assert.That(DailyText.CardTitle(date), Is.EqualTo("Reto del río · 19 oct"));
            Assert.That(DailyText.CardTitle(new DateTime(2027, 9, 3)), Is.EqualTo("Reto del río · 3 sept"));
            Assert.That(DailyText.Streak(3, 7), Is.EqualTo("3/7"));
            Assert.That(DailyText.RewardSeeds(60), Is.EqualTo("+60"));
            Assert.That(DailyText.RewardSeeds(1200), Is.EqualTo("+1.200"), "cifras de la región (es-CO)");
            Assert.That(DailyText.State(DailyCardState.Play), Is.EqualTo("Jugar"));
            Assert.That(DailyText.State(DailyCardState.Sealed), Is.EqualTo("Sellado"));
            Assert.That(DailyText.State(DailyCardState.InProgress), Is.EqualTo("Seguir"));
            Assert.That(DailyText.State(DailyCardState.PendingValidation), Is.EqualTo("Pendiente de validar"));
            Assert.That(DailyText.State(DailyCardState.Hidden), Is.Empty);
            Assert.That(DailyText.HomeLabel, Is.EqualTo("Inicio"));
            for (int m = 1; m <= 12; m++)
            {
                Assert.That(Loc.Has(DailyText.MonthShortKey(m)), Is.True, "mes " + m);
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => DailyText.MonthShortKey(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => DailyText.MonthShortKey(13));
        }

        [Test]
        public void DailyText_KeysMissingFromTheTable_FallBackLikeLoc_DevMarksReleaseEmpty()
        {
            LocCatalog project = CatalogWithoutTheRetoRows();
            var date = new DateTime(2026, 10, 19);

            Loc.Configure(project, LocLocale.SourceLocale, true); // desarrollo: «[clave]»
            Assert.That(DailyText.Title, Is.EqualTo("[daily_challenge]"));
            Assert.That(DailyText.State(DailyCardState.Sealed), Is.EqualTo("[daily_card_sealed]"));
            Assert.That(() => DailyText.CardTitle(date), Throws.Nothing);
            Assert.That(DailyText.State(DailyCardState.Play), Is.EqualTo("Jugar"), "las claves reutilizadas ya existen");

            Loc.Configure(project, LocLocale.SourceLocale, false); // release: vacío, nunca otro idioma
            Assert.That(DailyText.Title, Is.Empty);
            Assert.That(DailyText.Streak(2, 7), Is.Empty);
            Assert.That(DailyText.CardTitle(date), Is.Empty);
        }

        // ---- DailyLevelStateStore (R-Q4) ----

        [Test]
        public void StateStore_LoadsOnlyTodaysSavedState()
        {
            Fixture f = new Fixture();
            var states = new DailyLevelStateStore(f.Store, f.Clock);
            Assert.That(states.Load(), Is.Null, "sin partida");

            f.Service.Start(25);
            Assert.That(states.Load(), Is.Null, "recién empezada: nada guardado aún (level = 0)");

            var state = new LevelStateData { Level = 26, Seed = "s", Pours = 2, History = "h" };
            states.Save(state);
            state.Pours = 99; // la copia guardada no cambia
            LevelStateData? loaded = states.Load();
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.Pours, Is.EqualTo(2));
            Assert.That(loaded.Level, Is.EqualTo(26));
            Assert.That(f.Store.Persisted.InProgress!.State.Pours, Is.EqualTo(2), "se guardó en el bloque daily_challenge");
            Assert.That(f.Profile.LevelState, Is.Null, "nunca en el level_state de la campaña");
            Assert.That(states.HasStale, Is.False);

            loaded.Pours = 7;
            Assert.That(states.Load()!.Pours, Is.EqualTo(2), "Load da una copia");
        }

        [Test]
        public void StateStore_OtherDay_IsStaleAndNotLoaded()
        {
            Fixture f = new Fixture();
            var states = new DailyLevelStateStore(f.Store, f.Clock);
            f.Service.Start(25);
            states.Save(new LevelStateData { Level = 26, Seed = "s", Pours = 1 });

            f.Clock.Advance(TimeSpan.FromDays(1));

            Assert.That(states.HasStale, Is.True);
            Assert.That(states.Load(), Is.Null, "R-Q4: otro día no se reanuda");
            f.Service.DiscardStale();
            Assert.That(states.HasStale, Is.False);
            Assert.That(f.Store.Load().InProgress, Is.Null);
        }

        [Test]
        public void StateStore_Clear_KeepsThePlayOpenForTheService()
        {
            Fixture f = new Fixture();
            var states = new DailyLevelStateStore(f.Store, f.Clock);
            f.Service.Start(25);
            states.Save(new LevelStateData { Level = 26, Seed = "s", Pours = 3 });

            states.Clear(); // LevelFlow lo llama antes de avisar de la victoria

            Assert.That(states.Load(), Is.Null);
            Assert.That(f.Store.Load().InProgress, Is.Not.Null, "la cierra el servicio, no el almacén");
            Assert.That(f.Service.ClaimWin(), Is.EqualTo(ClaimOutcome.Confirmed));
            Assert.That(f.Store.Load().InProgress, Is.Null);
        }

        [Test]
        public void StateStore_WithoutPlay_SaveAndClearDoNothing()
        {
            Fixture f = new Fixture();
            var states = new DailyLevelStateStore(f.Store, f.Clock);
            int writes = f.Store.WriteCount;

            states.Save(new LevelStateData { Level = 26 });
            states.Clear();

            Assert.That(f.Store.WriteCount, Is.EqualTo(writes));
            Assert.That(states.SaveCount, Is.EqualTo(0));
            Assert.Throws<ArgumentNullException>(() => states.Save(null!));
            Assert.Throws<ArgumentNullException>(() => new DailyLevelStateStore(null!, f.Clock));
        }

        // Copia de LocTablesTests.VisibleLength (AF.Tests.EditMode; este ensamblado no lo ve): sin etiquetas de TextMeshPro ni
        // saltos de línea; un plural cuenta su forma más larga; {} cuenta 2, {sep} 3 y cualquier otro marcador, lo que ocupa escrito.
        private static int VisibleLength(string s)
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

        private static IReadOnlyList<LocCsvRow> ReadCsvRows(out string text)
        {
            text = LocCsv.Decode(File.ReadAllBytes(LocTables.ProjectPath(CsvPath)), "spark-strings-reto.csv");
            return LocCsv.Parse(text, "spark-strings-reto.csv");
        }

        // Una clave de E5b que aún no está en strings.csv.
        private static bool IsPendingE5b(LocCatalog project, string key)
        {
            return Array.IndexOf(PendingE5bKeys, key) >= 0 && project.Strings.IndexOf(key) < 0;
        }

        // Tablas del proyecto (con E5, strings.csv ya trae las filas del Reto) y, detrás, las filas de E5b que aún falten.
        private static LocCatalog TestCatalog()
        {
            string strings = LocTables.ReadProjectFile(LocTables.StringsAssetPath, LocCatalog.StringsFile);
            string facts = LocTables.ReadProjectFile(LocTables.FactsAssetPath, LocCatalog.FactsFile);
            LocCatalog project = LocCatalog.Parse(strings, facts);
            ReadCsvRows(out string daily);
            var pending = new System.Text.StringBuilder();
            foreach (string line in daily.Substring(daily.IndexOf('\n') + 1).Split('\n'))
            {
                int comma = line.IndexOf(',');
                if (comma > 0 && IsPendingE5b(project, line.Substring(0, comma)))
                {
                    pending.Append(line).Append('\n');
                }
            }

            if (pending.Length == 0)
            {
                return project;
            }

            return LocCatalog.Parse((strings.EndsWith("\n", StringComparison.Ordinal) ? strings : strings + "\n") + pending, facts);
        }

        // strings.csv sin las filas nuevas del Reto: para probar el respaldo de Loc con claves que faltan.
        private static LocCatalog CatalogWithoutTheRetoRows()
        {
            string strings = LocTables.ReadProjectFile(LocTables.StringsAssetPath, LocCatalog.StringsFile);
            string facts = LocTables.ReadProjectFile(LocTables.FactsAssetPath, LocCatalog.FactsFile);
            ReadCsvRows(out string daily);
            string kept = strings;
            foreach (string line in daily.Substring(daily.IndexOf('\n') + 1).Split('\n'))
            {
                if (line.Length > 0)
                {
                    kept = kept.Replace(line + "\n", "");
                }
            }

            return LocCatalog.Parse(kept, facts);
        }

        private sealed class Fixture
        {
            public Fixture()
            {
                Clock = new ManualClock(new DateTime(2026, 10, 19, 15, 0, 0, DateTimeKind.Utc), Bogota);
                Profile = Wallet.NewProfile(Clock);
                Wallet = new Wallet(Profile, new MemoryProfileStore(), Clock);
                Store = new InMemoryDailyChallengeStore();
                var rules = new DailyChallengeRules(Store, Wallet, Clock, FakeServerDate.Offline(), DailyChallengeConfig.Defaults);
                Service = new DailyChallengeService(Store, rules, Clock, DailyChallengeConfig.Defaults, DailyUiTestPool.Full);
            }

            public ManualClock Clock { get; }

            public Profile Profile { get; }

            public Wallet Wallet { get; }

            public InMemoryDailyChallengeStore Store { get; }

            public DailyChallengeService Service { get; }
        }
    }
}
