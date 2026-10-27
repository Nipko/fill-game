#nullable enable
using System;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// La lógica de gestos y frases de TiTino (<see cref="SlothLines"/>, <see cref="SlothContext"/> y <see cref="SlothSession"/>;
    /// D-016, puntos 4 y 5; propuesta de TiTino d.4-d.7, d.10 y e.4; tarea TI3): topes y su orden (d.6, punto 5), los tres hitos,
    /// una línea por victoria (T6-A), el banco más específico, la repetición, el fallo, el reinicio, los saludos, los mensajes
    /// apagados, misma entrada con mismo resultado y cero asignaciones; los mini-bancos de las frases fijas y el saludo de la
    /// mañana en la primera apertura dentro de la franja (D-030, preguntas 16 b y 17 b; tarea DC25); y los textos: 38 frases
    /// (≤ 50 caracteres en ES y ≤ 85 en EN y PT-BR), 11 datos del perezoso, todo <c>pendiente_asesor</c> y ningún id con «TiTino».
    /// </summary>
    public sealed class SlothLinesTests
    {
        private static readonly VictoryLine EmptySlot = new VictoryLine(false, false, null, false, null, false, false);
        private static readonly VictoryLine FruitSlot = new VictoryLine(false, false, null, false, "fact.acai.01", false, false);

        private static int s_sink;
        private LocStateSnapshot _loc;

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

        // ------------------------------------------------------------------------------------------- ayudas

        private static SlothContext Ctx(bool enabled = true, bool messages = true, bool failEnabled = true, int every = 4, int max = 4,
            int lines = 0, bool previous = false, int[]? last = null)
        {
            return new SlothContext(enabled, messages, failEnabled, every, max, lines, previous, last ?? NewLast());
        }

        private static int[] NewLast()
        {
            return new[] { SlothData.NoLine, SlothData.NoLine, SlothData.NoLine, SlothData.NoLine, SlothData.NoLine, SlothData.NoLine, SlothData.NoLine, SlothData.NoLine };
        }

        private static SlothVictoryFacts Win(int winCount = 4, bool hard = false, SlothDebut debut = SlothDebut.None, int streak = 0,
            bool streakMilestone = false, bool rescue = false, bool hint = false, bool box = true, bool colorFact = true)
        {
            return new SlothVictoryFacts(winCount, hard, debut, streak, streakMilestone, rescue, hint, box, colorFact);
        }

        private static SlothLine Victory(int level, in VictoryLine line, in SlothContext ctx, in SlothVictoryFacts facts)
        {
            SlothContext c = ctx.WithVictory(in facts);
            return SlothLines.ForVictory(level, in line, in c);
        }

        // ------------------------------------------------------------------------------------------- textos

        [Test]
        public void Banks_HaveTheSizesOfE4_AndTheMomentsOfTheProfile()
        {
            Assert.That(SlothLines.MomentCount, Is.EqualTo(SlothData.MomentCount));
            int[] sizes = { 8, 2, 5, 2, 6, 6, 3, 6 }; // las 30 de e.4 y las 8 alternativas de D-030 (pregunta 16 b)
            for (int m = 0; m < SlothData.MomentCount; m++)
            {
                var moment = (SlothMoment)m;
                Assert.That(moment.ToString().ToLowerInvariant(), Is.EqualTo(SlothData.Moments[m]), "orden de sloth.last_line");
                Assert.That(SlothLines.BankSize(moment), Is.EqualTo(sizes[m]), moment.ToString());
                for (int i = 0; i < sizes[m]; i++)
                {
                    string expected = SlothLines.KeyPrefix + SlothData.Moments[m] + "_" + (i + 1).ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                    Assert.That(SlothLines.KeyOf(moment, i), Is.EqualTo(expected));
                }
            }

            Assert.That(new List<string>(SlothLines.AllKeys).Count, Is.EqualTo(38), "las 30 frases de e.4 y las 8 alternativas de D-030");
            Assert.Throws<ArgumentOutOfRangeException>(() => SlothLines.KeyOf(SlothMoment.Hard, 2));
        }

        [Test]
        public void ThirtyEightLines_InStringsCsv_Es50_EnAndPt85_PendingTheAdvisor()
        {
            LocTable strings = LocSetUpFixture.Catalog.Strings;
            foreach (string key in SlothLines.AllKeys)
            {
                int r = strings.IndexOf(key);
                Assert.That(r, Is.GreaterThanOrEqualTo(0), key);
                string es = strings.TextAt(r, LocLocale.SourceLocale);
                Assert.That(es.Length, Is.InRange(1, 50), key + ": ≤ 50 caracteres en español (e.2)");
                Assert.That(strings.MaxCharsAt(r), Is.EqualTo(50), key);
                Assert.That(strings.StatusAt(r, LocLocale.SourceLocale), Is.EqualTo(LocStatus.PendingAdvisor).Or.EqualTo(LocStatus.ApprovedEs), key);
                foreach (string locale in new[] { LocLocale.English, LocLocale.PortugueseBrazil })
                {
                    string text = strings.TextAt(r, locale);
                    Assert.That(text.Length, Is.InRange(1, 85), key + " (" + locale + "): borrador de e.4, ≤ 85");
                    Assert.That(strings.StatusAt(r, locale), Is.EqualTo(LocStatus.Translated).Or.EqualTo(LocStatus.NativeReviewed), key + " (" + locale + "): traducida (y revisada en D-054)");
                }
            }

            int row = strings.IndexOf(UiText.Keys.SettingsSlothMessages);
            Assert.That(row, Is.GreaterThanOrEqualTo(0));
            Assert.That(strings.StatusAt(row, LocLocale.SourceLocale), Is.EqualTo(LocStatus.PendingAdvisor).Or.EqualTo(LocStatus.ApprovedEs));
            Assert.That(UiText.SettingsSlothMessages, Is.EqualTo("Mensajes de TiTino"));
        }

        [Test]
        public void ElevenSlothFacts_InFactsCsv_WithTheKeyOfQuestion49()
        {
            LocTable facts = LocSetUpFixture.Catalog.Facts;
            Assert.That(SlothLines.SlothFactKeys.Count, Is.EqualTo(11));
            for (int i = 0; i < SlothLines.SlothFactKeys.Count; i++)
            {
                string key = SlothLines.SlothFactKeys[i];
                Assert.That(key, Is.EqualTo("fauna.bradypus_variegatus.dato_" + (i + 1).ToString("00", System.Globalization.CultureInfo.InvariantCulture)));
                int r = facts.IndexOf(key);
                Assert.That(r, Is.GreaterThanOrEqualTo(0), key);
                Assert.That(facts.TextAt(r, LocLocale.SourceLocale).Length, Is.InRange(1, 90), key + ": ≤ 90 (g.3)");
                Assert.That(facts.StatusAt(r, LocLocale.SourceLocale), Is.EqualTo(LocStatus.PendingAdvisor).Or.EqualTo(LocStatus.ApprovedEs), key);
                Assert.That(facts.ContextAt(r), Does.Contain("http"), key + ": la fuente con su URL");
                Assert.That(UiText.SlothFact(key), Is.Not.Empty, key + ": en desarrollo se ve");
            }

            Assert.That(SlothLines.SlothFactKeys[10], Is.EqualTo(SlothLines.ColorFactKey));
            // La frase n.º 3 solo sale con el dato_11 aprobado: lo aprobó la revisión de D-054.
            int colorFact = facts.IndexOf(SlothLines.ColorFactKey);
            Assert.That(UiText.SlothColorFactApproved, Is.EqualTo(facts.StatusAt(colorFact, LocLocale.SourceLocale) >= LocStatus.ApprovedEs));
            Assert.That(UiText.SlothColorFactApproved, Is.True, "dato_11 aprobado en D-054");
        }

        [Test]
        public void NoIdCarriesTheName()
        {
            var ids = new List<string>(SlothLines.AllKeys);
            ids.AddRange(SlothLines.SlothFactKeys);
            ids.Add(UiText.Keys.SettingsSlothMessages);
            ids.Add(SettingsScreen.SlothMessagesId);
            ids.AddRange(SlothData.Moments);
            foreach (LocTable table in new[] { LocSetUpFixture.Catalog.Strings, LocSetUpFixture.Catalog.Facts })
            {
                for (int r = 0; r < table.Count; r++)
                {
                    ids.Add(table.KeyAt(r));
                }
            }

            foreach (string name in Enum.GetNames(typeof(SlothMoment)))
            {
                ids.Add(name);
            }

            foreach (string id in ids)
            {
                Assert.That(id.IndexOf("titino", StringComparison.OrdinalIgnoreCase), Is.EqualTo(-1), id + ": ningún id lleva el nombre (T4-A)");
            }
        }

        [Test]
        public void TrySlothLine_FormatsTheMarker_AndHidesUnapprovedInRelease()
        {
            var streak = new SlothLine(SlothScreen.Victory, SlothGesture.Cheer, SlothMoment.Streak, 0, SlothLines.KeyOf(SlothMoment.Streak, 0), 5, false);
            Assert.That(UiText.TrySlothLine(in streak, out string text), Is.True);
            Assert.That(text, Is.EqualTo("¡5 seguidas! El río crece contigo."));
            var plain = new SlothLine(SlothScreen.Fail, SlothGesture.Encourage, SlothMoment.Fail, 4, SlothLines.KeyOf(SlothMoment.Fail, 4), 0, false);
            Assert.That(UiText.TrySlothLine(in plain, out text), Is.True);
            Assert.That(text, Is.EqualTo("Paso a paso. La próxima sale."));
            var none = new SlothLine(SlothScreen.Fail, SlothGesture.Encourage, SlothMoment.Win, SlothData.NoLine, null, 0, false);
            Assert.That(UiText.TrySlothLine(in none, out text), Is.False);

            // {n} del nivel, sin separador de miles (como «Nivel {n}» del HUD).
            var back = new SlothLine(SlothScreen.Home, SlothGesture.Wave, SlothMoment.Return, 1, SlothLines.KeyOf(SlothMoment.Return, 1), 1000, true);
            Assert.That(UiText.TrySlothLine(in back, out text), Is.True);
            Assert.That(text, Is.EqualTo("El río sigue donde lo dejaste: nivel 1000."));

            // Release con las frases y los datos pendientes del asesor (el estado con el que nacen; la tabla real los tiene
            // aprobados desde D-054): no se enseñan.
            Loc.Configure(Pending(LocSetUpFixture.Catalog, false), LocLocale.SourceLocale, false);
            Assert.That(UiText.TrySlothLine(in plain, out text), Is.False, "release: pendiente del asesor, no se enseña");
            Assert.That(text, Is.Empty);
            Assert.That(UiText.SlothFact(SlothLines.SlothFactKeys[0]), Is.Empty, "release: el dato sin aprobar es «»");

            // En otro idioma, en release, ni el borrador de e.4 (traducido) ni nada sin aprobado_es y revisado_nativo.
            foreach (string locale in new[] { LocLocale.English, LocLocale.PortugueseBrazil })
            {
                Loc.Configure(Pending(LocSetUpFixture.Catalog, true), locale, false);
                Assert.That(UiText.TrySlothLine(in plain, out text), Is.False, locale + ": release sin revisión nativa");
                Assert.That(text, Is.Empty, locale);
                Assert.That(UiText.TrySlothLine(in streak, out text), Is.False, locale + ": release sin revisión nativa");
                Assert.That(text, Is.Empty, locale);
            }

            // La tabla real (D-054): español aprobado y traducción revisada, así que en release sí salen.
            Loc.Configure(LocSetUpFixture.Catalog, LocLocale.SourceLocale, false);
            Assert.That(UiText.TrySlothLine(in plain, out text), Is.True, "release: aprobada en D-054");
            Assert.That(text, Is.EqualTo("Paso a paso. La próxima sale."));
            Assert.That(UiText.SlothFact(SlothLines.SlothFactKeys[0]), Is.Not.Empty, "release: el dato aprobado sale");
            Loc.Configure(LocSetUpFixture.Catalog, LocLocale.English, false);
            Assert.That(UiText.TrySlothLine(in plain, out text), Is.True, "en: revisado_nativo en D-054");
            Assert.That(text, Is.EqualTo("Step by step. You'll get the next one."));

            // En desarrollo, en inglés, el texto inglés (nunca mezcla idiomas).
            Loc.Configure(LocSetUpFixture.Catalog, LocLocale.English, true);
            Assert.That(UiText.TrySlothLine(in plain, out text), Is.True);
            Assert.That(text, Is.EqualTo("Step by step. You'll get the next one."));
        }

        /// <summary>
        /// <paramref name="catalog"/> con el español de las tablas en pendiente_asesor (y, con <paramref name="unreviewed"/>, las
        /// traducciones en «traducido»): el estado de antes de la revisión de D-054, para probar la regla de release.
        /// </summary>
        private static LocCatalog Pending(LocCatalog catalog, bool unreviewed)
        {
            string strings = LocTables.ReadProjectFile(LocTables.StringsAssetPath, LocCatalog.StringsFile).Replace(",aprobado_es,", ",pendiente_asesor,");
            string facts = LocTables.ReadProjectFile(LocTables.FactsAssetPath, LocCatalog.FactsFile).Replace(",aprobado_es,", ",pendiente_asesor,");
            if (unreviewed)
            {
                strings = strings.Replace(",revisado_nativo", ",traducido");
                facts = facts.Replace(",revisado_nativo", ",traducido");
            }

            Assert.That(catalog, Is.Not.Null);
            return LocCatalog.Parse(strings, facts);
        }

        // ------------------------------------------------------------------------------------------- victoria

        [Test]
        public void Victory_OneLineEveryN_OnlyWithTheSlotEmpty()
        {
            SlothContext ctx = Ctx();
            for (int won = 1; won <= 12; won++)
            {
                SlothLine line = Victory(won + 100, EmptySlot, ctx, Win(winCount: won));
                Assert.That(line.Gesture, Is.EqualTo(SlothGesture.Cheer), "gesto en cada victoria");
                Assert.That(line.Speaks, Is.EqualTo(won % 4 == 0), "1 de cada 4: victoria n.º " + won);
            }

            // Una sola línea por victoria (T6-A): con la línea de fruta, la llegada o un dato en la ranura, solo el gesto.
            Assert.That(Victory(103, FruitSlot, ctx, Win(winCount: 8)).Speaks, Is.False, "línea de fruta");
            var arrival = new VictoryLine(true, false, null, false, null, false, false);
            Assert.That(Victory(103, arrival, ctx, Win(winCount: 8)).Speaks, Is.False, "placa de llegada");
            var deferred = new VictoryLine(false, false, "map.stop.x.fact", true, null, false, false);
            Assert.That(Victory(103, deferred, ctx, Win(winCount: 8)).Speaks, Is.False, "dato desplazado");

            // Cadencia apagada; caja sin sitio para el bocadillo (d.4: la frase se pierde, no se aplaza).
            Assert.That(Victory(103, EmptySlot, Ctx(every: 0), Win(winCount: 8)).Speaks, Is.False);
            SlothLine noBox = Victory(103, EmptySlot, ctx, Win(winCount: 8, box: false));
            Assert.That(noBox.Speaks, Is.False);
            Assert.That(noBox.Gesture, Is.EqualTo(SlothGesture.Cheer));
        }

        [Test]
        public void Victory_Level10_AndTheStreakMilestones_GestureOnly()
        {
            SlothContext ctx = Ctx();
            Assert.That(Victory(10, EmptySlot, ctx, Win(winCount: 8)).Speaks, Is.False, "el 10 lo cuenta el Mapa (d.6, punto 1)");
            SlothLine otters = Victory(103, EmptySlot, ctx, Win(winCount: 8, streak: 6, streakMilestone: true));
            Assert.That(otters.Speaks, Is.False, "hito de la Crecida: protagonizan las nutrias (d.6, punto 3)");
            Assert.That(otters.Gesture, Is.EqualTo(SlothGesture.Cheer));
        }

        [Test]
        public void Victory_Hitos_SafeLineWhenTheArbiterGivesIt()
        {
            SlothContext ctx = Ctx(lines: 4, previous: true); // con el tope y la pantalla anterior con frase
            var spoke = new VictoryLine(true, false, null, false, null, true, true);
            // Sin frase vista del momento, el fin de mundo rota con el número de mundo (D-030, pregunta 16 b): el 40 (mundo 2), la
            // de siempre; el 80, la 1.ª alternativa; el 140, la 2.ª; y vuelta a empezar.
            int[] worldEnds = { 40, 80, 140, 200, 300, 1000 };
            string[] expected = { "sloth_line_milestone_01", "sloth_line_milestone_03", "sloth_line_milestone_04", "sloth_line_milestone_01", "sloth_line_milestone_03", "sloth_line_milestone_04" };
            for (int i = 0; i < worldEnds.Length; i++)
            {
                SlothLine line = Victory(worldEnds[i], spoke, ctx, Win(winCount: 1));
                Assert.That(line.Gesture, Is.EqualTo(SlothGesture.CheerBig), worldEnds[i].ToString());
                Assert.That(line.Key, Is.EqualTo(expected[i]), "fin de mundo: " + worldEnds[i]);
            }

            Assert.That(Victory(60, spoke, ctx, Win(winCount: 1)).Key, Is.EqualTo("sloth_line_milestone_02"), "primer Muy difícil");

            // Un hito sin su línea (TiTino no pudo hablar) es una victoria normal: gesto grande y sin frase.
            var silent = new VictoryLine(true, true, "map.stop.x.fact", false, null, false, true);
            SlothLine normal = Victory(80, silent, Ctx(), Win(winCount: 8));
            Assert.That(normal.Speaks, Is.False);
            Assert.That(normal.Gesture, Is.EqualTo(SlothGesture.CheerBig));
        }

        [Test]
        public void HitoVoice_WorldEndAlwaysSpeaks_FirstVeryHardRespectsTheCaps()
        {
            Assert.That(SlothLines.IsWorldEnd(10), Is.False, "el 10 no");
            foreach (int end in new[] { 40, 80, 140, 200, 300, 420, 560, 700, 820, 920, 960, 1000 })
            {
                Assert.That(SlothLines.IsWorldEnd(end), Is.True, end.ToString());
            }

            Assert.That(SlothLines.IsWorldEnd(60), Is.False);
            Assert.That(SlothLines.IsWorldEnd(41), Is.False);

            SlothContext fresh = Ctx();
            SlothContext capped = Ctx(lines: 4);
            SlothContext afterLine = Ctx(lines: 1, previous: true);
            Assert.That(SlothLines.HitoVoice(40, fresh) && SlothLines.HitoVoice(60, fresh) && SlothLines.HitoVoice(80, fresh), Is.True);
            Assert.That(SlothLines.HitoVoice(80, capped), Is.True, "fin de mundo: aunque se llegó al tope");
            Assert.That(SlothLines.HitoVoice(80, afterLine), Is.True, "fin de mundo: aunque la pantalla anterior tuvo frase");
            Assert.That(SlothLines.HitoVoice(60, capped), Is.False, "primer Muy difícil: el tope manda");
            Assert.That(SlothLines.HitoVoice(60, afterLine), Is.False, "primer Muy difícil: nunca dos pantallas seguidas");
            Assert.That(SlothLines.HitoVoice(50, fresh), Is.False, "no es hito");
            Assert.That(SlothLines.HitoVoice(80, Ctx(messages: false)), Is.False, "mensajes apagados");
            Assert.That(SlothLines.HitoVoice(80, Ctx(enabled: false)), Is.False, "ui_sloth_enabled apagado");
        }

        [Test]
        public void HitoVoice_WithTheArbiter_GivesOneLinePerVictory()
        {
            RiverStopCatalog stops = MapTestCatalogs.Full;
            SlothContext ctx = Ctx();
            foreach (int level in new[] { 40, 60, 80 })
            {
                bool voice = SlothLines.HitoVoice(level, ctx);
                var vc = new VictoryContext(level, true, stops, "fact.acai.01", RiverFactPolicy.Canon, HitoLinePolicy.SlothHito, voice, 10, 1);
                VictoryLine line = VictoryLineArbiter.For(in vc);
                SlothLine sloth = Victory(level, line, ctx, Win(winCount: 3));
                Assert.That(sloth.Speaks, Is.True, level.ToString());
                Assert.That(line.ArrivalFactKey, Is.Null, "la llegada va sin dato");
                Assert.That(line.FruitKey, Is.Null, "y sin línea de fruta: una sola línea");
            }

            // Con el tope, el 60 no habla: el árbitro da el dato y TiTino solo el gesto.
            SlothContext capped = Ctx(lines: 4);
            var c60 = new VictoryContext(60, true, stops, "fact.acai.01", RiverFactPolicy.Canon, HitoLinePolicy.SlothHito,
                SlothLines.HitoVoice(60, capped), 10, 1);
            VictoryLine l60 = VictoryLineArbiter.For(in c60);
            Assert.That(l60.SlothSpeaks, Is.False);
            Assert.That(l60.ArrivalFactKey, Is.Not.Null, "manda el dato");
            Assert.That(Victory(60, l60, capped, Win(winCount: 4)).Speaks, Is.False);
        }

        [Test]
        public void Victory_MostSpecificBank_HardThenDebutThenStreakThenWin()
        {
            SlothContext ctx = Ctx();
            Assert.That(Victory(103, EmptySlot, ctx, Win(hard: true, debut: SlothDebut.Mechanic, streak: 5)).Moment, Is.EqualTo(SlothMoment.Hard));
            SlothLine debut = Victory(103, EmptySlot, ctx, Win(debut: SlothDebut.Mechanic, streak: 5));
            Assert.That(debut.Moment, Is.EqualTo(SlothMoment.Debut));
            Assert.That(debut.Key, Is.EqualTo("sloth_line_debut_01"));
            Assert.That(Victory(103, EmptySlot, ctx, Win(debut: SlothDebut.Fruit)).Key, Is.EqualTo("sloth_line_debut_02"));
            Assert.That(Victory(103, EmptySlot, ctx, Win(debut: SlothDebut.World)).Key, Is.EqualTo("sloth_line_debut_03"));
            Assert.That(Victory(103, EmptySlot, ctx, Win(streak: 5)).Moment, Is.EqualTo(SlothMoment.Streak));
            Assert.That(Victory(103, EmptySlot, ctx, Win(streak: 3)).Moment, Is.EqualTo(SlothMoment.Win), "la Crecida cuenta desde 4");
            Assert.That(Victory(103, EmptySlot, ctx, Win()).Moment, Is.EqualTo(SlothMoment.Win));
        }

        [Test]
        public void Victory_DebutLineThatWouldRepeat_FallsToTheNextBank()
        {
            // Dos estrenos de mecánica seguidos con frase: el segundo no repite sloth_line_debut_01 (d.10), baja a streak o win.
            int[] last = NewLast();
            last[(int)SlothMoment.Debut] = 0;
            SlothContext ctx = Ctx(last: last);
            SlothLine streak = Victory(103, EmptySlot, ctx, Win(debut: SlothDebut.Mechanic, streak: 5));
            Assert.That(streak.Speaks, Is.True);
            Assert.That(streak.Moment, Is.EqualTo(SlothMoment.Streak));
            SlothLine win = Victory(103, EmptySlot, ctx, Win(debut: SlothDebut.Mechanic));
            Assert.That(win.Speaks, Is.True);
            Assert.That(win.Moment, Is.EqualTo(SlothMoment.Win));

            // Otro tipo de estreno sí dice su frase.
            Assert.That(Victory(103, EmptySlot, ctx, Win(debut: SlothDebut.Fruit)).Key, Is.EqualTo("sloth_line_debut_02"));

            // Con SlothSession: el segundo estreno de mecánica no repite la frase.
            var data = new SlothData();
            var first = new SlothSession(data, true, true, true, 1, 4);
            SlothContext c1 = first.Context.WithVictory(Win(debut: SlothDebut.Mechanic));
            SlothLine l1 = SlothLines.ForVictory(103, EmptySlot, in c1);
            Assert.That(l1.Key, Is.EqualTo("sloth_line_debut_01"));
            first.Record(in l1, in c1);
            var second = new SlothSession(data, true, true, true, 1, 4);
            SlothContext c2 = second.Context.WithVictory(Win(debut: SlothDebut.Mechanic));
            SlothLine l2 = SlothLines.ForVictory(107, EmptySlot, in c2);
            Assert.That(l2.Speaks, Is.True);
            Assert.That(l2.Key, Is.Not.EqualTo("sloth_line_debut_01"));
        }

        [Test]
        public void Victory_StreakBank_NamedLineCarriesN_HighLineOnlyFrom7To9_BrokenNever()
        {
            var seen = new HashSet<int>();
            for (int seed = 0; seed < 40; seed++)
            {
                for (int streak = 4; streak <= 9; streak++)
                {
                    SlothLine line = Victory(201 + seed, EmptySlot, Ctx(), Win(winCount: 4 * (seed + 1), streak: streak));
                    Assert.That(line.Moment, Is.EqualTo(SlothMoment.Streak));
                    Assert.That(line.Index, Is.Not.EqualTo(3), "la n.º 21 solo al romperse la Crecida");
                    Assert.That(line.Index == 2 && (streak < 7 || streak > 9), Is.False, "la n.º 20, solo de 7 a 9");
                    Assert.That(line.Arg, Is.EqualTo(line.Index == 0 ? streak : 0), "{n} = la Crecida");
                    seen.Add(line.Index);
                }
            }

            Assert.That(seen, Is.EquivalentTo(new[] { 0, 1, 2 }));
        }

        [Test]
        public void Victory_Lines4And8_NeverAfterRescueOrHint_Line3_OnlyWithTheColorFact()
        {
            var withRescue = new HashSet<int>();
            var withoutColor = new HashSet<int>();
            var all = new HashSet<int>();
            for (int seed = 0; seed < 64; seed++)
            {
                withRescue.Add(Victory(201 + seed, EmptySlot, Ctx(), Win(rescue: seed % 2 == 0, hint: seed % 2 == 1)).Index);
                withoutColor.Add(Victory(201 + seed, EmptySlot, Ctx(), Win(colorFact: false)).Index);
                all.Add(Victory(201 + seed, EmptySlot, Ctx(), Win()).Index);
            }

            Assert.That(withRescue.Contains(3) || withRescue.Contains(7), Is.False, "n.º 4 y 8 elogian la planificación (D-008)");
            Assert.That(withRescue.Count, Is.EqualTo(6));
            Assert.That(withoutColor.Contains(2), Is.False, "n.º 3 solo con el dato_11 aprobado");
            Assert.That(withoutColor.Count, Is.EqualTo(7));
            Assert.That(all, Is.EquivalentTo(new[] { 0, 1, 2, 3, 4, 5, 6, 7 }), "el banco entero sale con todo permitido");
        }

        // ------------------------------------------------------------------------------------------- topes y repetición

        [Test]
        public void Caps_InTheirOrder_SessionMaxThenTwoScreensThenOneLine()
        {
            var data = new SlothData();
            var session = new SlothSession(data, true, true, true, 1, 4);
            int spoken = 0;
            for (int won = 1; won <= 20; won++)
            {
                SlothContext c = session.Context.WithVictory(Win(winCount: won));
                SlothLine line = SlothLines.ForVictory(100 + won, EmptySlot, in c);
                Assert.That(line.Speaks && c.PreviousScreenSpoke, Is.False, "nunca dos pantallas seguidas con frase");
                session.Record(in line, in c);
                spoken += line.Speaks ? 1 : 0;
            }

            Assert.That(spoken, Is.EqualTo(4), "como mucho 4 por sesión");
            Assert.That(session.LinesThisSession, Is.EqualTo(4));

            // Tras el tope, el fin de mundo (no el primer Muy difícil) sigue hablando.
            SlothContext after = session.Context;
            Assert.That(SlothLines.HitoVoice(80, after), Is.True);
            Assert.That(SlothLines.HitoVoice(60, after), Is.False);
            var spoke = new VictoryLine(true, false, null, false, null, true, true);
            SlothContext v = after.WithVictory(Win(winCount: 21));
            SlothLine end = SlothLines.ForVictory(80, in spoke, in v);
            Assert.That(end.Key, Is.EqualTo("sloth_line_milestone_03"), "el 80, sin frase vista del momento: la 1.ª alternativa (D-030, 16 b)");
            session.Record(in end, in v);
            Assert.That(session.LinesThisSession, Is.EqualTo(5), "el fin de mundo cuenta, aunque pase el tope");

            // Una pantalla sin frase corta la racha: la siguiente ya puede hablar (si no hay tope).
            var fresh = new SlothSession(new SlothData(), true, true, true, 1, 4);
            SlothContext f1 = fresh.Context.WithVictory(Win(winCount: 1));
            SlothLine first = SlothLines.ForVictory(101, EmptySlot, in f1);
            fresh.Record(in first, in f1);
            SlothContext f2 = fresh.Context.WithVictory(Win(winCount: 2));
            SlothLine blocked = SlothLines.ForVictory(102, FruitSlot, in f2);
            Assert.That(first.Speaks && !blocked.Speaks, Is.True);
            fresh.Record(in blocked, in f2);
            SlothContext f3 = fresh.Context.WithVictory(Win(winCount: 3));
            Assert.That(SlothLines.ForVictory(103, EmptySlot, in f3).Speaks, Is.True);
        }

        [Test]
        public void Repetition_NeverTheSameAsTheLastOfTheSameMoment()
        {
            foreach (SlothMoment moment in new[] { SlothMoment.Win, SlothMoment.Hard, SlothMoment.Fail, SlothMoment.Restart, SlothMoment.Streak, SlothMoment.Return })
            {
                var data = new SlothData();
                int previous = SlothData.NoLine;
                int checkedLines = 0;
                for (int i = 0; i < 60; i++)
                {
                    var session = new SlothSession(data, true, true, true, 1, 4); // una sesión por frase: sin topes
                    SlothContext c = session.Context;
                    SlothLine line;
                    switch (moment)
                    {
                        case SlothMoment.Fail:
                            c = c.WithFail(new SlothFailFacts(true, false, i, true));
                            line = SlothLines.ForFail(31 + i, in c);
                            break;
                        case SlothMoment.Restart:
                            c = c.WithRestart(new SlothRestartFacts(true, false, false));
                            line = SlothLines.ForRestartNotice(1 + (i % 30), in c);
                            break;
                        case SlothMoment.Return:
                            string today = new DateTime(2026, 1, 1).AddDays(10 * i).ToString("yyyy'-'MM'-'dd", System.Globalization.CultureInfo.InvariantCulture);
                            string seen = new DateTime(2026, 1, 1).AddDays((10 * i) - 5).ToString("yyyy'-'MM'-'dd", System.Globalization.CultureInfo.InvariantCulture);
                            c = c.WithHome(new SlothHomeFacts(today, 18, seen, null, 7));
                            line = SlothLines.ForHome(in c);
                            break;
                        default:
                            c = c.WithVictory(Win(winCount: i + 1, hard: moment == SlothMoment.Hard, streak: moment == SlothMoment.Streak ? 8 : 0));
                            line = SlothLines.ForVictory(201 + i, EmptySlot, in c);
                            break;
                    }

                    Assert.That(line.Speaks, Is.True, moment + " " + i);
                    Assert.That(line.Moment, Is.EqualTo(moment));
                    Assert.That(line.Index, Is.Not.EqualTo(previous), moment + ": nunca la misma que la última vista");
                    session.Record(in line, in c);
                    Assert.That(data.LastLine[(int)moment], Is.EqualTo(line.Index), "sloth.last_line");
                    previous = line.Index;
                    checkedLines++;
                }

                Assert.That(checkedLines, Is.EqualTo(60));
            }
        }

        [Test]
        public void SameInput_SameResult()
        {
            int[] last = { 1, 0, 2, 1, 0, 2, SlothData.NoLine, 0 };
            for (int level = 1; level <= 1000; level += 7)
            {
                foreach (bool previous in new[] { false, true })
                {
                    SlothContext a = Ctx(lines: level % 5, previous: previous, last: (int[])last.Clone())
                        .WithVictory(Win(winCount: level, hard: level % 3 == 0, streak: level % 11, debut: (SlothDebut)(level % 4)))
                        .WithFail(new SlothFailFacts(level % 2 == 0, level % 5 == 0, level, true))
                        .WithRestart(new SlothRestartFacts(true, level % 6 == 0, level % 9 == 0))
                        .WithHome(new SlothHomeFacts("2026-10-15", level % 24, "2026-10-1" + (level % 6), null, level));
                    SlothContext b = Ctx(lines: level % 5, previous: previous, last: (int[])last.Clone())
                        .WithVictory(Win(winCount: level, hard: level % 3 == 0, streak: level % 11, debut: (SlothDebut)(level % 4)))
                        .WithFail(new SlothFailFacts(level % 2 == 0, level % 5 == 0, level, true))
                        .WithRestart(new SlothRestartFacts(true, level % 6 == 0, level % 9 == 0))
                        .WithHome(new SlothHomeFacts("2026-10-15", level % 24, "2026-10-1" + (level % 6), null, level));
                    VictoryLine line = level % 2 == 0 ? EmptySlot : FruitSlot;
                    AssertSame(SlothLines.ForVictory(level, in line, in a), SlothLines.ForVictory(level, in line, in b), "victoria " + level);
                    AssertSame(SlothLines.ForVictory(level, in line, in a), SlothLines.ForVictory(level, in line, in a), "dos veces " + level);
                    AssertSame(SlothLines.ForFail(level, in a), SlothLines.ForFail(level, in b), "fallo " + level);
                    AssertSame(SlothLines.ForRestartNotice(level, in a), SlothLines.ForRestartNotice(level, in b), "reinicio " + level);
                    AssertSame(SlothLines.ForHome(in a), SlothLines.ForHome(in b), "inicio " + level);
                }
            }
        }

        private static void AssertSame(in SlothLine x, in SlothLine y, string what)
        {
            Assert.That(x.Screen == y.Screen && x.Gesture == y.Gesture && x.Moment == y.Moment && x.Index == y.Index
                && string.Equals(x.Key, y.Key, StringComparison.Ordinal) && x.Arg == y.Arg && x.Greeting == y.Greeting
                && x.Deferred == y.Deferred, Is.True, what);
        }

        // ------------------------------------------------------------------------------------------- fallo y reinicio

        [Test]
        public void Fail_LineOnlyOnTheFirstFail_ConsolationWhenTheStreakBreaks()
        {
            SlothContext ctx = Ctx();
            SlothLine first = SlothLines.ForFail(45, ctx.WithFail(new SlothFailFacts(true, false, 3, true)));
            Assert.That(first.Gesture, Is.EqualTo(SlothGesture.Encourage));
            Assert.That(first.Moment, Is.EqualTo(SlothMoment.Fail));
            Assert.That(first.Key, Does.StartWith("sloth_line_fail_"));

            SlothLine second = SlothLines.ForFail(45, ctx.WithFail(new SlothFailFacts(false, false, 4, true)));
            Assert.That(second.Speaks, Is.False, "en los siguientes, solo el gesto");
            Assert.That(second.Gesture, Is.EqualTo(SlothGesture.Encourage));

            SlothLine broken = SlothLines.ForFail(45, ctx.WithFail(new SlothFailFacts(true, true, 3, true)));
            Assert.That(broken.Key, Is.EqualTo("sloth_line_streak_04"), "la n.º 21 con la Crecida rota");

            Assert.That(SlothLines.ForFail(45, Ctx(failEnabled: false).WithFail(new SlothFailFacts(true, false, 3, true))).Gesture,
                Is.EqualTo(SlothGesture.None), "ui_sloth_fail_enabled = false: Nivel fallido sin TiTino");
            Assert.That(SlothLines.ForFail(45, ctx.WithFail(new SlothFailFacts(true, false, 3, false))).Speaks, Is.False,
                "con la tarjeta al 130 % sin sitio, solo el gesto");
            Assert.That(SlothLines.ForFail(45, Ctx(previous: true).WithFail(new SlothFailFacts(true, false, 3, true))).Speaks, Is.False);
            Assert.That(SlothLines.ForFail(45, Ctx(lines: 4).WithFail(new SlothFailFacts(true, false, 3, true))).Speaks, Is.False);
        }

        [Test]
        public void RestartNotice_Only1To30_FirstRestart_WithoutTutorialInstruction()
        {
            SlothContext ok = Ctx().WithRestart(new SlothRestartFacts(true, false, false));
            for (int level = 1; level <= 40; level++)
            {
                SlothLine line = SlothLines.ForRestartNotice(level, in ok);
                Assert.That(line.Speaks, Is.EqualTo(level <= 30), level.ToString());
                if (line.Speaks)
                {
                    Assert.That(line.Moment, Is.EqualTo(SlothMoment.Restart));
                    Assert.That(line.Gesture, Is.EqualTo(SlothGesture.Encourage));
                }
                else
                {
                    Assert.That(line.Gesture, Is.EqualTo(SlothGesture.None), "sin aviso fuera de 1-30");
                }
            }

            Assert.That(SlothLines.ForRestartNotice(12, Ctx().WithRestart(new SlothRestartFacts(false, false, false))).Gesture, Is.EqualTo(SlothGesture.None),
                "solo el primer reinicio");
            Assert.That(SlothLines.ForRestartNotice(3, Ctx().WithRestart(new SlothRestartFacts(true, true, false))).Gesture, Is.EqualTo(SlothGesture.None),
                "con instrucción del tutorial, TiTino ya está en ella");
            Assert.That(SlothLines.ForRestartNotice(27, Ctx().WithRestart(new SlothRestartFacts(true, false, true))).Key, Is.EqualTo("sloth_line_streak_04"),
                "25-30 con la Crecida rota: la n.º 21");
        }

        // ------------------------------------------------------------------------------------------- inicio

        [Test]
        public void Home_ReturnAfterThreeDays_MorningFrom6To12_OneGreetingPerDay()
        {
            SlothLine ret = SlothLines.ForHome(Ctx().WithHome(new SlothHomeFacts("2026-10-15", 20, "2026-10-12", null, 58)));
            Assert.That(ret.Greeting, Is.True);
            Assert.That(ret.Gesture, Is.EqualTo(SlothGesture.Wave));
            Assert.That(ret.Moment, Is.EqualTo(SlothMoment.Return));
            Assert.That(ret.Index, Is.InRange(0, 2), "las de regreso, 22-24");
            Assert.That(SlothLines.ForHome(Ctx().WithHome(new SlothHomeFacts("2026-10-15", 20, "2026-10-13", null, 58))).Greeting, Is.False,
                "2 días no es un regreso");

            // Mañana: primera apertura del día entre las 06:00 y las 12:00.
            for (int hour = 0; hour < 24; hour++)
            {
                SlothLine morning = SlothLines.ForHome(Ctx().WithHome(new SlothHomeFacts("2026-10-15", hour, "2026-10-14", null, 58)));
                Assert.That(morning.Greeting, Is.EqualTo(hour >= 6 && hour < 12), hour + " h");
                if (morning.Greeting)
                {
                    Assert.That(morning.Key, Is.EqualTo("sloth_line_return_04"));
                }
            }

            Assert.That(SlothLines.ForHome(Ctx().WithHome(new SlothHomeFacts("2026-10-15", 8, "2026-10-15", "2026-10-15", 58))).Greeting, Is.False,
                "ya hubo saludo hoy");
            Assert.That(SlothLines.ForHome(Ctx().WithHome(new SlothHomeFacts("2026-10-15", 8, "2026-10-15", null, 58))).Key, Is.EqualTo("sloth_line_return_04"),
                "ya abrió hoy, pero fuera de la franja: es la primera apertura dentro de ella (D-030, pregunta 17 b)");
            Assert.That(SlothLines.ForHome(Ctx().WithHome(new SlothHomeFacts("2026-10-15", 8, null, null, 58))).Key, Is.EqualTo("sloth_line_return_04"),
                "primera apertura de la vida, por la mañana");

            // Regreso por la mañana: manda el de regreso; y nunca dos saludos el mismo día.
            var data = new SlothData { LastSeenDate = "2026-10-10" };
            var session = new SlothSession(data, true);
            SlothContext c = session.Context.WithHome(session.HomeFacts("2026-10-15", 9, 58));
            SlothLine first = SlothLines.ForHome(in c);
            Assert.That(first.Greeting && first.Index != 3, Is.True, "el de regreso, no el de la mañana");
            session.Record(in first, in c);
            Assert.That(data.LastGreetingDate, Is.EqualTo("2026-10-15"));
            Assert.That(data.LastSeenDate, Is.EqualTo("2026-10-15"));
            SlothContext again = session.Context.WithHome(session.HomeFacts("2026-10-15", 10, 58));
            Assert.That(SlothLines.ForHome(in again).Greeting, Is.False, "1 al día");

            // El día siguiente por la mañana, en otra sesión: el saludo de la mañana. En la misma sesión, tras la frase de regreso,
            // solo el saludo con la mano (nunca dos pantallas seguidas con frase).
            SlothContext sameSession = session.Context.WithHome(session.HomeFacts("2026-10-16", 7, 58));
            SlothLine wave = SlothLines.ForHome(in sameSession);
            Assert.That(wave.Greeting && !wave.Speaks, Is.True);
            var tomorrow = new SlothSession(data, true);
            SlothContext next = tomorrow.Context.WithHome(tomorrow.HomeFacts("2026-10-16", 7, 58));
            Assert.That(SlothLines.ForHome(in next).Key, Is.EqualTo("sloth_line_return_04"));
        }

        [Test]
        public void Home_Morning_IsTheFirstOpeningWithinTheBand_NotTheFirstOfTheDay()
        {
            // D-030, pregunta 17 b: «la primera apertura dentro de la franja». Abrir a las 05:30 apunta la apertura sin saludo; a
            // las 07:00 del mismo día sale el de la mañana; y no dos veces el mismo día.
            var data = new SlothData { LastSeenDate = "2026-10-14" };
            var early = new SlothSession(data, true);
            SlothContext c1 = early.Context.WithHome(early.HomeFacts("2026-10-15", 5, 58));
            SlothLine dawn = SlothLines.ForHome(in c1);
            Assert.That(dawn.Greeting || dawn.Gesture != SlothGesture.None, Is.False, "a las 05:30, TiTino duerme");
            early.Record(in dawn, in c1);
            Assert.That(data.LastSeenDate, Is.EqualTo("2026-10-15"), "la apertura queda apuntada");
            Assert.That(data.LastGreetingDate, Is.Null, "sin saludo");

            var later = new SlothSession(data, true);
            SlothContext c2 = later.Context.WithHome(later.HomeFacts("2026-10-15", 7, 58));
            SlothLine seven = SlothLines.ForHome(in c2);
            Assert.That(seven.Greeting, Is.True, "a las 07:00, la primera apertura dentro de la franja");
            Assert.That(seven.Gesture, Is.EqualTo(SlothGesture.Wave));
            Assert.That(seven.Key, Is.EqualTo("sloth_line_return_04"), "e.4, n.º 25");
            later.Record(in seven, in c2);
            Assert.That(data.LastGreetingDate, Is.EqualTo("2026-10-15"));

            var third = new SlothSession(data, true);
            SlothContext c3 = third.Context.WithHome(third.HomeFacts("2026-10-15", 9, 58));
            SlothLine nine = SlothLines.ForHome(in c3);
            Assert.That(nine.Greeting || nine.Gesture != SlothGesture.None, Is.False, "no dos veces el mismo día");

            // El regreso tras ≥ 3 días no cambia: a las 05:30 sale el de regreso (y gasta el saludo del día).
            var away = new SlothData { LastSeenDate = "2026-10-10" };
            var back = new SlothSession(away, true);
            SlothContext c4 = back.Context.WithHome(back.HomeFacts("2026-10-15", 5, 58));
            SlothLine ret = SlothLines.ForHome(in c4);
            Assert.That(ret.Greeting && ret.Index >= 0 && ret.Index <= 2, Is.True, "el de regreso, a cualquier hora");
            back.Record(in ret, in c4);
            var after = new SlothSession(away, true);
            Assert.That(SlothLines.ForHome(after.Context.WithHome(after.HomeFacts("2026-10-15", 7, 58))).Greeting, Is.False,
                "nunca el de la mañana el día del de regreso");
        }

        // ------------------------------------------------------------------------------------------- mini-bancos (D-030, 16 b)

        [Test]
        public void FixedLines_AreMiniBanksOfThree_ThatNeverRepeatTheLast()
        {
            // Morning: tres mañanas seguidas, las tres frases del mini-banco de la n.º 25, sin repetir, y vuelta a la de siempre.
            var data = new SlothData { LastSeenDate = "2026-10-14" };
            string[] mornings = { "sloth_line_return_04", "sloth_line_return_05", "sloth_line_return_06", "sloth_line_return_04" };
            for (int d = 0; d < mornings.Length; d++)
            {
                var session = new SlothSession(data, true);
                string today = new DateTime(2026, 10, 15).AddDays(d).ToString("yyyy'-'MM'-'dd", System.Globalization.CultureInfo.InvariantCulture);
                SlothContext c = session.Context.WithHome(session.HomeFacts(today, 8, 58));
                SlothLine line = SlothLines.ForHome(in c);
                Assert.That(line.Key, Is.EqualTo(mornings[d]), today);
                session.Record(in line, in c);
            }

            // La n.º 21 (Crecida rota), en el fallo y en el aviso del reinicio: rota igual.
            var crecida = new SlothData();
            string[] broken = { "sloth_line_streak_04", "sloth_line_streak_05", "sloth_line_streak_06", "sloth_line_streak_04" };
            for (int i = 0; i < broken.Length; i++)
            {
                var session = new SlothSession(crecida, true, true, true, 1, 4);
                SlothLine line;
                SlothContext c;
                if (i % 2 == 0)
                {
                    c = session.Context.WithFail(new SlothFailFacts(true, true, 3, true));
                    line = SlothLines.ForFail(45, in c);
                }
                else
                {
                    c = session.Context.WithRestart(new SlothRestartFacts(true, false, true));
                    line = SlothLines.ForRestartNotice(27, in c);
                }

                Assert.That(line.Key, Is.EqualTo(broken[i]), i.ToString());
                session.Record(in line, in c);
            }

            // El primer Muy difícil: la de siempre y, si la última vista del momento fuera ella, la alternativa siguiente.
            var spoke = new VictoryLine(true, false, null, false, null, true, true);
            int[] last = NewLast();
            Assert.That(Victory(60, spoke, Ctx(last: last), Win(winCount: 1)).Key, Is.EqualTo("sloth_line_milestone_02"));
            last[(int)SlothMoment.Milestone] = 1;
            Assert.That(Victory(60, spoke, Ctx(last: last), Win(winCount: 1)).Key, Is.EqualTo("sloth_line_milestone_05"));
            last[(int)SlothMoment.Milestone] = 4;
            Assert.That(Victory(60, spoke, Ctx(last: last), Win(winCount: 1)).Key, Is.EqualTo("sloth_line_milestone_06"));
            last[(int)SlothMoment.Milestone] = 5;
            Assert.That(Victory(60, spoke, Ctx(last: last), Win(winCount: 1)).Key, Is.EqualTo("sloth_line_milestone_02"));
        }

        [Test]
        public void WorldEnds_InARow_NeverRepeat_EvenWithTheFirstVeryHardInBetween()
        {
            // El recorrido real: los 12 fines de mundo (40-1.000) con el primer Muy difícil (60) entre el 40 y el 80, que comparte
            // momento; con el tope de la sesión y la pantalla anterior con frase, como en el peor caso (habla siempre).
            var data = new SlothData();
            var spoke = new VictoryLine(true, false, null, false, null, true, true);
            string? previousWorldEnd = null;
            int worldEnds = 0;
            for (int level = 11; level <= WorldTable.LastLevel; level++)
            {
                if (!SlothLines.IsWorldEnd(level) && level != SlothLines.FirstVeryHardLevel)
                {
                    continue;
                }

                var session = new SlothSession(data, true, true, true, 4, 4);
                SlothContext c = session.Context.WithVictory(Win(winCount: 1));
                SlothLine line = SlothLines.ForVictory(level, in spoke, in c);
                Assert.That(line.Speaks && line.Moment == SlothMoment.Milestone, Is.True, level.ToString());
                session.Record(in line, in c);
                if (level == SlothLines.FirstVeryHardLevel)
                {
                    Assert.That(line.Key, Is.EqualTo("sloth_line_milestone_02"));
                    continue;
                }

                Assert.That(line.Index == 0 || line.Index == 2 || line.Index == 3, Is.True, level + ": del mini-banco de la n.º 29");
                Assert.That(line.Key, Is.Not.EqualTo(previousWorldEnd), level + ": dos fines de mundo seguidos no repiten");
                previousWorldEnd = line.Key;
                worldEnds++;
            }

            Assert.That(worldEnds, Is.EqualTo(12), "los fines de mundo 2-13 (el 10 no: lo cuenta el Mapa)");
        }

        [Test]
        public void Context_CopiesLastLine_RecordDoesNotChangeIt()
        {
            // El contexto es un valor: copia sloth.last_line al crearlo, y un Record posterior no cambia su resultado.
            var data = new SlothData();
            var session = new SlothSession(data, true, true, true, 1, 4);
            SlothContext c = session.Context.WithFail(new SlothFailFacts(true, false, 3, true));
            SlothLine before = SlothLines.ForFail(45, in c);
            Assert.That(before.Speaks, Is.True);
            session.Record(in before, in c);
            Assert.That(data.LastLine[(int)SlothMoment.Fail], Is.EqualTo(before.Index));
            Assert.That(c.LastLineOf(SlothMoment.Fail), Is.EqualTo(SlothData.NoLine), "el contexto no ve el Record");
            AssertSame(SlothLines.ForFail(45, in c), before, "mismo contexto, mismo resultado tras Record");

            int[] last = NewLast();
            SlothContext copy = Ctx(last: last);
            last[(int)SlothMoment.Win] = 5;
            Assert.That(copy.LastLineOf(SlothMoment.Win), Is.EqualTo(SlothData.NoLine), "tampoco ve cambios en el array de origen");
            Assert.That(Ctx(last: new[] { 4, 1 }).LastLineOf(SlothMoment.Hard), Is.EqualTo(1));
            Assert.That(Ctx(last: new[] { 4, 1 }).LastLineOf(SlothMoment.Milestone), Is.EqualTo(SlothData.NoLine), "los que faltan, sin frase");
            Assert.That(new SlothContext(true, true, true, 4, 4, 0, false, null).LastLineOf(SlothMoment.Win), Is.EqualTo(SlothData.NoLine));
        }

        [Test]
        public void Home_LevelLineCarriesTheLevel_AndIsSkippedWithoutIt()
        {
            bool sawLevel = false;
            for (int d = 0; d < 30; d++)
            {
                string today = new DateTime(2026, 11, 1).AddDays(d).ToString("yyyy'-'MM'-'dd", System.Globalization.CultureInfo.InvariantCulture);
                SlothLine with = SlothLines.ForHome(Ctx().WithHome(new SlothHomeFacts(today, 20, "2026-10-01", null, 58)));
                Assert.That(with.Arg, Is.EqualTo(with.Index == 1 ? 58 : 0));
                sawLevel |= with.Index == 1;
                SlothLine without = SlothLines.ForHome(Ctx().WithHome(new SlothHomeFacts(today, 20, "2026-10-01", null, 0)));
                Assert.That(without.Index, Is.Not.EqualTo(1), "sin nivel, la n.º 23 no sale");
            }

            Assert.That(sawLevel, Is.True);
            Assert.That(SlothLines.ForHome(Ctx().WithHome(new SlothHomeFacts("no-es-fecha", 8, null, null, 1))).Gesture, Is.EqualTo(SlothGesture.None));
        }

        [Test]
        public void DayNumber_IsTheCivilDayCount()
        {
            Assert.That(SlothLines.TryDayNumber("1970-01-01", out int epoch) && epoch == 0, Is.True);
            Assert.That(SlothLines.TryDayNumber("2026-10-15", out int a), Is.True);
            Assert.That(a, Is.EqualTo((int)(new DateTime(2026, 10, 15) - new DateTime(1970, 1, 1)).TotalDays));
            Assert.That(SlothLines.TryDayNumber("2024-03-01", out int b) && SlothLines.TryDayNumber("2024-02-28", out int c) && b - c == 2, Is.True, "bisiesto");
            Assert.That(SlothLines.TryDayNumber("2026-02-30", out _), Is.False);
            Assert.That(SlothLines.TryDayNumber(null, out _), Is.False);
        }

        // ------------------------------------------------------------------------------------------- interruptores

        [Test]
        public void MessagesOff_OnlyGestures_EnabledOff_Nothing()
        {
            SlothContext quiet = Ctx(messages: false);
            var spoke = new VictoryLine(true, false, null, false, null, true, true);
            SlothLine v = Victory(103, EmptySlot, quiet, Win(winCount: 8));
            Assert.That(v.Speaks, Is.False);
            Assert.That(v.Gesture, Is.EqualTo(SlothGesture.Cheer));
            Assert.That(Victory(80, spoke, quiet, Win()).Gesture, Is.EqualTo(SlothGesture.CheerBig));
            Assert.That(Victory(80, spoke, quiet, Win()).Speaks, Is.False);
            SlothLine f = SlothLines.ForFail(45, quiet.WithFail(new SlothFailFacts(true, false, 1, true)));
            Assert.That(f.Speaks || f.Gesture != SlothGesture.Encourage, Is.False);
            SlothLine r = SlothLines.ForRestartNotice(12, quiet.WithRestart(new SlothRestartFacts(true, false, false)));
            Assert.That(r.Speaks || r.Gesture != SlothGesture.Encourage, Is.False);
            SlothLine h = SlothLines.ForHome(quiet.WithHome(new SlothHomeFacts("2026-10-15", 8, "2026-10-14", null, 5)));
            Assert.That(h.Speaks, Is.False);
            Assert.That(h.Gesture == SlothGesture.Wave && h.Greeting, Is.True, "el saludo con la mano, sin frase");

            SlothContext off = Ctx(enabled: false);
            Assert.That(Victory(103, EmptySlot, off, Win(winCount: 8)).Gesture, Is.EqualTo(SlothGesture.None));
            Assert.That(Victory(80, spoke, off, Win()).Gesture, Is.EqualTo(SlothGesture.None));
            Assert.That(SlothLines.ForFail(45, off.WithFail(new SlothFailFacts(true, false, 1, true))).Gesture, Is.EqualTo(SlothGesture.None));
            Assert.That(SlothLines.ForRestartNotice(12, off.WithRestart(new SlothRestartFacts(true, false, false))).Gesture, Is.EqualTo(SlothGesture.None));
            Assert.That(SlothLines.ForHome(off.WithHome(new SlothHomeFacts("2026-10-15", 8, "2026-10-14", null, 5))).Gesture, Is.EqualTo(SlothGesture.None));
        }

        [Test]
        public void Defaults_AreThoseOf08_9()
        {
            Assert.That(GameConfig.UiSlothEnabled, Is.True);
            Assert.That(GameConfig.UiSlothFailEnabled, Is.True);
            Assert.That(GameConfig.UiSlothWinLineEveryN, Is.EqualTo(4));
            Assert.That(GameConfig.UiSlothLinesPerSessionMax, Is.EqualTo(4));
            var session = new SlothSession(new SlothData(), true);
            Assert.That(session.Enabled && session.FailEnabled && session.WinLineEveryN == 4 && session.LinesPerSessionMax == 4, Is.True);
            Assert.That(new Profile().Settings.SlothMessages, Is.True, "settings.sloth_messages = true por defecto");
        }

        // ------------------------------------------------------------------------------------------- asignaciones

        [Test]
        public void Decisions_DoNotAllocate()
        {
            int[] last = { 0, 1, 2, 0, 1, 2, 0, 1 };
            var spoke = new VictoryLine(true, false, null, false, null, true, true);
            TestDelegate call = () =>
            {
                int acc = 0;
                for (int level = 1; level <= 200; level++)
                {
                    SlothContext c = new SlothContext(true, true, true, 4, 4, level % 5, (level & 1) == 0, last)
                        .WithVictory(new SlothVictoryFacts(level, level % 3 == 0, (SlothDebut)(level % 4), level % 10, false, false, false, true, false))
                        .WithFail(new SlothFailFacts(true, level % 7 == 0, level, true))
                        .WithRestart(new SlothRestartFacts(true, false, false))
                        .WithHome(new SlothHomeFacts("2026-10-15", level % 24, "2026-10-10", null, level));
                    VictoryLine line = (level & 2) == 0 ? EmptySlot : spoke;
                    acc += SlothLines.ForVictory(level, in line, in c).Index;
                    acc += SlothLines.ForFail(level, in c).Index;
                    acc += SlothLines.ForRestartNotice(level, in c).Index;
                    acc += SlothLines.ForHome(in c).Index;
                    acc += SlothLines.HitoVoice(level, in c) ? 1 : 0;
                }

                s_sink = acc;
            };
            call(); // calentamiento
            Assert.That(call, Is.Not.AllocatingGCMemory());
            Assert.That(s_sink, Is.Not.EqualTo(int.MinValue));
        }
    }
}
