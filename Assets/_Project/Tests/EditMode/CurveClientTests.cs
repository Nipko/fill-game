#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.RemoteConfig;
using AF.RemoteConfig.Binding;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// La curva 2 de la 1.1 en el cliente (D-068, fase F4): el lector de <c>pack_schema</c> 5 (08 §4.1: <c>group</c>,
    /// <c>n</c>, <c>k</c>, <c>rows</c>, <c>difficulty.pressure</c>, <c>kind</c> = <c>curve</c> con los ids <c>_c{c}</c> y
    /// <c>replaces</c>), el selector (<see cref="CurveSelection"/>: <c>progress.curve2_from_level</c> y <c>lvl_curve2_enabled</c>,
    /// solo en 301-700), los kill switches <c>mech_enabled_aguape</c> y <c>mech_enabled_lancha</c> con <c>level_overrides</c> y la
    /// reserva de la curva, las filas fijas (<see cref="BoardLayout"/>), la tarjeta y el dedo guía de los estrenos, TalkBack y los
    /// pasos de la suelta y del atraque (<see cref="MechanicStepPlan"/>).
    /// </summary>
    public sealed class CurveClientTests
    {
        private static LevelPack Campaign301To304() => LevelPackReader.Read(CurveFixtures.Campaign(301, 304));

        private static LevelPack CurvePack() => LevelPackReader.Read(CurveFixtures.Curve());

        private static LevelPack CurveReserve() => LevelPackReader.Read(CurveFixtures.CurveReserve());

        // ---- Lector de pack_schema 5 (08 §4.1) ----

        [Test]
        public void Reader_Schema5_ReadsTheCurvePack_WithGroupRowsPressureAndIds()
        {
            LevelPack pack = CurvePack();
            Assert.That(LevelPackReader.SupportedPackSchema, Is.EqualTo(5));
            Assert.That(pack.IsCurve, Is.True);
            Assert.That(pack.IsReserve, Is.False);

            LevelDefinition aguape = pack.Find(303)!;
            Assert.That(aguape.Id, Is.EqualTo("m07_v1_n0303_c2"));
            Assert.That(aguape.Replaces, Is.EqualTo("m07_v1_n0303"), "replaces = el id de la curva 1 del slot");
            Assert.That(aguape.Curve, Is.EqualTo(LevelDefinition.Curve2));
            Assert.That(aguape.IsReserve, Is.False);
            Assert.That(aguape.Rows, Is.EqualTo(new[] { 4, 3 }));
            Assert.That(aguape.Pressure, Is.EqualTo(4));
            Assert.That(aguape.IntroMechanic, Is.EqualTo("aguape"));
            Assert.That(aguape.Attrs[0].Type, Is.EqualTo(MechanicType.Aguape));
            Assert.That(aguape.Attrs[1].Group, Is.EqualTo(3));
            BoardState board = aguape.CreateBoard();
            Assert.That(board[0].Mechanic, Is.EqualTo(MechanicType.Aguape));
            Assert.That(board[1].MechanicN, Is.EqualTo(1));

            LevelDefinition plain = pack.Find(302)!;
            Assert.That(plain.Pressure, Is.EqualTo(LevelDefinition.NoPressure), "sin difficulty.pressure");
            Assert.That(plain.Rows, Is.Null);

            LevelDefinition reserve = CurveReserve().Find(303)!;
            Assert.That(reserve.IsReserve, Is.True);
            Assert.That(reserve.Curve, Is.EqualTo(LevelDefinition.Curve2), "la reserva de la curva 2 también es de la curva 2 (analítica)");
            Assert.That(reserve.Replaces, Is.EqualTo("m07_v1_n0303_c2"));

            LevelDefinition campaign = Campaign301To304().Find(303)!;
            Assert.That(campaign.Curve, Is.EqualTo(LevelDefinition.Curve1));
            Assert.That(campaign.Replaces, Is.Null);
        }

        [Test]
        public void Reader_Schema5_ThickAguapeAndLancha_Load()
        {
            LevelPack thick = LevelPackReader.Read(CurveFixtures.Pack("mundo_10_v1", "campaign", 5, 10,
                CurveFixtures.Level(10, 701, CurveFixtures.ThickCuias, CurveFixtures.ThickAttrs, ", \"intro_mechanic\": \"aguape_n2\"")));
            BoardState board = thick.First.CreateBoard();
            Assert.That(board[2].MechanicN, Is.EqualTo(2), "el espeso: n = 2");
            Assert.That(thick.First.IntroMechanic, Is.EqualTo("aguape_n2"));

            LevelPack lancha = LevelPackReader.Read(CurveFixtures.Pack("mundo_10_v1", "campaign", 5, 10,
                CurveFixtures.Level(10, 702, CurveFixtures.LanchaCuias, CurveFixtures.LanchaAttrs, ", \"intro_mechanic\": \"lancha\"")));
            Assert.That(lancha.First.CreateBoard()[0].MechanicK, Is.EqualTo(2));
            Assert.That(lancha.First.Curve, Is.EqualTo(LevelDefinition.Curve1), "desde el 701 la campaña nace con la curva 2 sin sufijo: curve 1 en el pack");
        }

        [TestCase(", \"rows\": [2, 2]", "'rows' necesita pack_schema 5")]
        [TestCase(", \"intro_mechanic\": \"lancha\"", "'intro_mechanic' lancha necesita pack_schema 5")]
        public void Reader_Schema5Fields_AreRejectedWithSchema4(string extra, string expected)
        {
            string json = CurveFixtures.Pack("mundo_07_v1", "campaign", 4, 7, CurveFixtures.Level(7, 301, CurveFixtures.PlainCuias, null, extra));
            Assert.That(() => LevelPackReader.Read(json), Throws.TypeOf<LevelFormatException>().With.Message.Contains(expected));
            string group = CurveFixtures.Pack("mundo_07_v1", "campaign", 4, 7,
                CurveFixtures.Level(7, 301, CurveFixtures.PlainCuias, "[{\"type\": \"hoja\", \"key\": 8, \"group\": 1}, null, null, null]"));
            Assert.That(() => LevelPackReader.Read(group), Throws.TypeOf<LevelFormatException>().With.Message.Contains("'group' necesita pack_schema 5"));
        }

        [Test]
        public void Reader_Schema5_RejectsWhatTheFormatForbids()
        {
            // pressure con el 4, group con el 4, kind curve con el 4.
            string withPressure = CurveFixtures.Campaign(301, 301).Replace("\"band\": \"challenge\" }", "\"band\": \"challenge\", \"pressure\": 3 }");
            Assert.That(() => LevelPackReader.Read(withPressure), Throws.TypeOf<LevelFormatException>().With.Message.Contains("'difficulty.pressure' necesita pack_schema 5"));
            Assert.That(() => LevelPackReader.Read(CurveFixtures.Curve().Replace("\"pack_schema\": 5", "\"pack_schema\": 4")),
                Throws.TypeOf<LevelFormatException>().With.Message.Contains("kind curve) necesita pack_schema 5"));

            // pressure fuera de 0-9.
            string badPressure = CurveFixtures.Curve().Replace("\"pressure\": 4", "\"pressure\": 10");
            Assert.That(() => LevelPackReader.Read(badPressure), Throws.TypeOf<LevelFormatException>().With.Message.Contains("de 0 a 9"));

            // replaces de la curva 2 que no es el propio slot.
            string otherSlot = CurveFixtures.Curve().Replace("\"replaces\": \"m07_v1_n0302\"", "\"replaces\": \"m07_v1_n0301\"");
            Assert.That(() => LevelPackReader.Read(otherSlot), Throws.TypeOf<LevelFormatException>().With.Message.Contains("'replaces' = ese id"));

            // un id sin _c{c} en el pack de la curva, y un _c2 en la campaña.
            string noSuffix = CurveFixtures.Curve().Replace("\"id\": \"m07_v1_n0302_c2\"", "\"id\": \"m07_v1_n0302\"");
            Assert.That(() => LevelPackReader.Read(noSuffix), Throws.TypeOf<LevelFormatException>());
            string campaignC2 = CurveFixtures.Pack("mundo_07_v1", "campaign", 5, 7, CurveFixtures.Level(7, 301, CurveFixtures.PlainCuias, idSuffix: "_c2"));
            Assert.That(() => LevelPackReader.Read(campaignC2), Throws.TypeOf<LevelFormatException>().With.Message.Contains("_c{c} solo va"));

            // rows que no suman las cuias o que parten el grupo (las cuias 0 y 1, en filas 1/6).
            string badSum = CurveFixtures.Curve().Replace("\"rows\": [4, 3]", "\"rows\": [4, 2]");
            Assert.That(() => LevelPackReader.Read(badSum), Throws.TypeOf<LevelFormatException>().With.Message.Contains("rows suma"));
            string split = CurveFixtures.Curve().Replace("\"rows\": [4, 3]", "\"rows\": [1, 6]");
            Assert.That(() => LevelPackReader.Read(split), Throws.TypeOf<LevelFormatException>().With.Message.Contains("m07_v1_n0303_c2"));

            // la reserva de la curva sustituye a un tablero de la curva 2.
            string reserveOfCurve1 = CurveFixtures.CurveReserve().Replace("\"replaces\": \"m07_v1_n0303_c2\"", "\"replaces\": \"m07_v1_n0303\"");
            Assert.That(() => LevelPackReader.Read(reserveOfCurve1), Throws.TypeOf<LevelFormatException>().With.Message.Contains("reserva de la curva 2 sustituye"));
        }

        // ---- Selector de la curva 2 (08 §4.3 y §4.4; D-068, P1 B y P10 A) ----

        [Test]
        public void Selector_ServesCurve2_FromCurve2FromLevel_Only301To700()
        {
            Assert.That(new CurveSelection(true, 302).UsesCurve2(301), Is.False);
            Assert.That(new CurveSelection(true, 302).UsesCurve2(302), Is.True);
            Assert.That(new CurveSelection(true, 1).UsesCurve2(300), Is.False, "antes del 301 no hay curva 2");
            Assert.That(new CurveSelection(true, 1).UsesCurve2(701), Is.False, "desde el 701 la campaña nace con ella");
            Assert.That(new CurveSelection(false, 1).UsesCurve2(350), Is.False, "lvl_curve2_enabled = false: la curva 1");
            Assert.That(CurveSelection.Off.UsesCurve2(350), Is.False);

            var warnings = new List<string>();
            LevelCatalog catalog = Create(new CurveSelection(true, 303), MechanicSwitches.AllOn, null, warnings);
            Assert.That(catalog.Find(301)!.Id, Is.EqualTo("m07_v1_n0301"));
            Assert.That(catalog.Find(302)!.Id, Is.EqualTo("m07_v1_n0302"), "302 < curve2_from_level: lo jugado no cambia de tablero");
            Assert.That(catalog.Find(303)!.Id, Is.EqualTo("m07_v1_n0303_c2"));
            Assert.That(catalog.Find(303)!.Curve, Is.EqualTo(2));
            Assert.That(catalog.Find(304)!.Id, Is.EqualTo("m07_v1_n0304"), "sin tablero de la curva 2: la curva 1");
            Assert.That(catalog.Curve2Levels, Is.EqualTo(new[] { 303 }));
            Assert.That(catalog.FindPublished(303)!.Id, Is.EqualTo("m07_v1_n0303"), "el publicado sigue siendo el de la curva 1");
            Assert.That(catalog.PackOf(303)!.PackId, Is.EqualTo("mundo_07_v1"));
            Assert.That(warnings, Is.Empty);

            LevelCatalog all = Create(new CurveSelection(true, 1), MechanicSwitches.AllOn, null, warnings);
            Assert.That(all.Curve2Levels, Is.EqualTo(new[] { 302, 303 }), "una instalación nueva (curve2_from_level = 1)");

            LevelCatalog off = Create(new CurveSelection(false, 1), MechanicSwitches.AllOn, null, warnings);
            Assert.That(off.Curve2Levels, Is.Empty, "el segmento A del A/B: la curva 1 en todos");
            Assert.That(off.Find(303)!.Id, Is.EqualTo("m07_v1_n0303"));
        }

        [Test]
        public void Catalog_RejectsACurvePackAsCampaign_AndIgnoresOutOfRangeOrOrphanCurveBoards()
        {
            Assert.That(() => LevelCatalog.Create(new[] { CurvePack() }), Throws.TypeOf<LevelFormatException>().With.Message.Contains("curva 2"));

            // Un tablero de la curva para un slot que no está en el catálogo: se ignora con aviso.
            var warnings = new List<string>();
            LevelCatalog catalog = LevelCatalog.Create(new[] { LevelPackReader.Read(CurveFixtures.Campaign(301, 302)) }, 0, null, MechanicSwitches.AllOn,
                w => warnings.Add(w), null, new[] { CurvePack() }, new CurveSelection(true, 1), null);
            Assert.That(catalog.Curve2Levels, Is.EqualTo(new[] { 302 }));
            Assert.That(warnings.Count, Is.EqualTo(1));
            StringAssert.Contains("m07_v1_n0303_c2", warnings[0]);

            // Fuera de 301-700 (un 300 de prueba): se ignora con aviso.
            string at300 = CurveFixtures.Pack("curva_07_v1", "curve", 5, 7,
                CurveFixtures.Level(7, 300, CurveFixtures.PlainCuias, idSuffix: "_c2", replaces: CurveFixtures.Id(7, 300)));
            warnings.Clear();
            LevelCatalog low = LevelCatalog.Create(new[] { LevelPackReader.Read(CurveFixtures.Campaign(300, 301)) }, 0, null, MechanicSwitches.AllOn,
                w => warnings.Add(w), null, new[] { LevelPackReader.Read(at300) }, new CurveSelection(true, 1), null);
            Assert.That(low.Find(300)!.Curve, Is.EqualTo(1));
            Assert.That(warnings.Count, Is.EqualTo(1));
            StringAssert.Contains("fuera de 301-700", warnings[0]);
        }

        // ---- Kill switches, level_overrides y la reserva de la curva (08 §9; D-068) ----

        [Test]
        public void KillSwitch_Aguape_ServesTheCurveReserve_ThenTheCurve1Board()
        {
            MechanicSwitches off = MechanicSwitches.AllOn.With(MechanicSwitch.Aguape, false);
            var warnings = new List<string>();

            // Con la reserva de la curva: su tablero _c2_r1 (por replaces), sin level_overrides [P].
            LevelCatalog withReserve = Create(new CurveSelection(true, 1), off, null, warnings, CurveReserve());
            Assert.That(withReserve.Find(303)!.Id, Is.EqualTo("m07_v1_n0303_c2_r1"));
            Assert.That(withReserve.IsOverridden(303), Is.True);
            Assert.That(withReserve.Find(302)!.Id, Is.EqualTo("m07_v1_n0302_c2"), "sin el Aguapé: no cambia");

            // Sin la reserva: el de la curva 1 del slot [P].
            LevelCatalog noReserve = Create(new CurveSelection(true, 1), off, null, warnings);
            Assert.That(noReserve.Find(303)!.Id, Is.EqualTo("m07_v1_n0303"));
            Assert.That(noReserve.FallbackLevels, Is.Empty);

            // Con la clave encendida no cambia nada.
            LevelCatalog on = Create(new CurveSelection(true, 1), MechanicSwitches.AllOn, null, warnings, CurveReserve());
            Assert.That(on.Find(303)!.Id, Is.EqualTo("m07_v1_n0303_c2"));
            Assert.That(on.OverriddenLevels, Is.Empty);
        }

        [Test]
        public void KillSwitch_LevelOverrides_AppliesOnTheCurveThePlayerIsOn()
        {
            MechanicSwitches off = MechanicSwitches.AllOn.With(MechanicSwitch.Aguape, false);
            var pool = new List<LevelPack> { Campaign301To304(), CurvePack(), CurveReserve() };

            // level_overrides con el tablero de reserva de la curva 2 del slot: se aplica (08 §9).
            LevelOverrides toReserve = LevelOverrides.Create("{\"303\": \"m07_v1_n0303_c2_r1\"}", pool);
            Assert.That(toReserve.Problems, Is.Empty);
            LevelCatalog a = Create(new CurveSelection(true, 1), off, toReserve, new List<string>());
            Assert.That(a.Find(303)!.Id, Is.EqualTo("m07_v1_n0303_c2_r1"));

            // Un override de la curva 1 no vale para un jugador de la curva 2: cae a la curva 1 del slot [P].
            LevelOverrides toCurve1 = LevelOverrides.Create("{\"303\": \"m07_v1_n0304\"}", pool);
            LevelCatalog b = Create(new CurveSelection(true, 1), off, toCurve1, new List<string>());
            Assert.That(b.Find(303)!.Id, Is.EqualTo("m07_v1_n0303"));

            // Un tablero de la curva 2 pedido para otro slot se descarta.
            LevelOverrides wrongSlot = LevelOverrides.Create("{\"304\": \"m07_v1_n0303_c2\"}", pool);
            Assert.That(wrongSlot.Count, Is.EqualTo(0));
            Assert.That(wrongSlot.Problems[0], Does.Contain("la curva 2 del slot 303"));
        }

        [Test]
        public void KillSwitches_HaveTheAguapeAndLanchaKeys_AndBlockTheirLevels()
        {
            Assert.That(MechanicSwitches.KeyOf(MechanicSwitch.Aguape), Is.EqualTo("mech_enabled_aguape"));
            Assert.That(MechanicSwitches.KeyOf(MechanicSwitch.Lancha), Is.EqualTo("mech_enabled_lancha"));
            LevelDefinition aguape = CurvePack().Find(303)!;
            Assert.That(MechanicSwitches.UsedMask(aguape), Is.EqualTo(1 << (int)MechanicSwitch.Aguape));
            LevelDefinition lancha = LevelPackReader.Read(CurveFixtures.Pack("mundo_10_v1", "campaign", 5, 10,
                CurveFixtures.Level(10, 702, CurveFixtures.LanchaCuias, CurveFixtures.LanchaAttrs))).First;
            Assert.That(MechanicSwitches.UsedMask(lancha), Is.EqualTo(1 << (int)MechanicSwitch.Lancha));

            // Remote Config (08 §9): los defaults encendidos, y false apaga.
            var rc = new RemoteConfigService();
            Assert.That(RcLevelBinder.MechEnabled(rc.Freeze(), MechanicSwitches.AllOn).AnyOff, Is.False);
            Assert.That(RcLevelBinder.Curve2Enabled(rc.Freeze()), Is.True, "lvl_curve2_enabled = true por defecto (08 §9)");
        }

        // ---- Filas fijas (02 §5; rows) ----

        [Test]
        public void Layout_FixedRows_RuleTheRows_AndAddedCuiasGoToTheShortest()
        {
            const float h4 = 5.6f;
            BoardLayoutResult r = BoardLayout.Compute(7, 7, h4, 360f, 120f, 620f, -1, -1, new[] { 4, 3 });
            Assert.That(r.Rows, Is.EqualTo(2));
            Assert.That(r.PerRow[0], Is.EqualTo(4));
            Assert.That(r.PerRow[1], Is.EqualTo(3));
            Assert.That(r.RowOf[3], Is.EqualTo(0));
            Assert.That(r.RowOf[4], Is.EqualTo(1));

            // Con 11 cuias, rows 4/4/3 en 3 filas aunque la regla diera otra cosa; la Cuia extra va a la más corta (la de abajo).
            BoardLayoutResult three = BoardLayout.Compute(12, 11, h4, 360f, 120f, 620f, -1, -1, new[] { 3, 4, 4 });
            Assert.That(three.Rows, Is.EqualTo(3));
            Assert.That(three.PerRow[0], Is.EqualTo(4), "la Cuia extra, a la derecha de la fila más corta (la de arriba, la única de 3)");
            Assert.That(three.RowOf[11], Is.EqualTo(0));
            Assert.That(three.RowOf[2], Is.EqualTo(0));
            Assert.That(three.RowOf[3], Is.EqualTo(1), "el grupo de las cuias 3-6 no se parte");

            // rows que no casan con las generadas (una variante sin rows, otro tablero): la regla de siempre.
            Assert.That(BoardLayout.FixedRowsFit(new[] { 4, 2 }, 7), Is.False);
            Assert.That(BoardLayout.FixedRowsFit(new[] { 8 }, 8), Is.False, "como mucho 7 por fila");
            BoardLayoutResult rule = BoardLayout.Compute(7, 7, h4, 360f, 120f, 620f, -1, -1, new[] { 4, 2 });
            Assert.That(rule.PerRow[0], Is.EqualTo(4));
            Assert.That(rule.PerRow[1], Is.EqualTo(3));
        }

        // ---- Tarjeta, tutoriales, dedo guía y TalkBack ----

        [Test]
        public void Card_LanchaK_AndDebutMechanics()
        {
            Assert.That(LevelCardInfo.DebutMechanicType("aguape"), Is.EqualTo(MechanicType.Aguape));
            Assert.That(LevelCardInfo.DebutMechanicType("aguape_n2"), Is.EqualTo(MechanicType.Aguape));
            Assert.That(LevelCardInfo.DebutMechanicType("lancha"), Is.EqualTo(MechanicType.Lancha));

            LevelDefinition lancha = LevelPackReader.Read(CurveFixtures.Pack("mundo_10_v1", "campaign", 5, 10,
                CurveFixtures.Level(10, 702, CurveFixtures.LanchaCuias, CurveFixtures.LanchaAttrs, ", \"intro_mechanic\": \"lancha\""))).First;
            LevelCardInfo card = LevelCardInfo.For(lancha, 702, AF.Economy.EconomyConfig.Defaults, false)!;
            Assert.That(card.LanchaK, Is.EqualTo(2));
            Assert.That(card.HasMechanic(MechanicType.Lancha), Is.True, "el icono de la Lancha en la tarjeta del Difícil");

            LocSetUpFixture.ConfigureEs419();
            Assert.That(UiText.Tutorial("lancha", 2), Does.Contain("2 cuias vacías"));
            Assert.That(UiText.Tutorial("lancha"), Does.Contain("3 cuias vacías"), "en el estreno, 3 [P]");
            Assert.That(UiText.Tutorial("aguape"), Does.StartWith("El aguapé sujeta estas cuias"));
            Assert.That(UiText.Tutorial("aguape_n2"), Does.StartWith("Este aguapé es espeso"));
        }

        [Test]
        public void Finger_DebutsOfTheCurve_PointAtTheirLesson()
        {
            Assert.That(TutorialInstruction.FingerFor(303, "aguape"), Is.EqualTo(FingerLesson.AguapeSeal));
            Assert.That(TutorialInstruction.FingerFor(347, "aguape_n2"), Is.EqualTo(FingerLesson.AguapeSeal));
            Assert.That(TutorialInstruction.FingerFor(363, "lancha"), Is.EqualTo(FingerLesson.LanchaEmpty));

            BoardState aguape = CurvePack().Find(303)!.CreateBoard();
            Assert.That(TutorialInstruction.IsAguapeLesson(aguape, 2, 1), Is.True, "2 → 1: verter en la mata");
            Assert.That(TutorialInstruction.IsAguapeLesson(aguape, 2, 4), Is.False, "a una cuia sin la mata");
            Assert.That(TutorialInstruction.IsAguapeLesson(aguape, 0, 4), Is.False, "la mata no es origen");

            BoardState lancha = LevelPackReader.Read(CurveFixtures.Pack("mundo_10_v1", "campaign", 5, 10,
                CurveFixtures.Level(10, 702, CurveFixtures.LanchaCuias, CurveFixtures.LanchaAttrs))).First.CreateBoard();
            Assert.That(TutorialInstruction.IsLanchaLesson(lancha, 1, 2, PourMode.Run), Is.True, "vacía la 1");
            Assert.That(TutorialInstruction.IsLanchaLesson(lancha, 3, 4, PourMode.Run), Is.True);
            Assert.That(TutorialInstruction.IsLanchaLesson(lancha, 0, 4, PourMode.Run), Is.False, "la lancha no es origen");
        }

        [Test]
        public void TalkBack_NamesTheCurveMechanics()
        {
            BoardState aguape = CurvePack().Find(303)!.CreateBoard();
            Assert.That(AF.UI.A11y.CuiaSpeech.MechanicId(aguape[0]), Is.EqualTo("aguape"));
            BoardState thick = LevelPackReader.Read(CurveFixtures.Pack("mundo_10_v1", "campaign", 5, 10,
                CurveFixtures.Level(10, 701, CurveFixtures.ThickCuias, CurveFixtures.ThickAttrs))).First.CreateBoard();
            Assert.That(AF.UI.A11y.CuiaSpeech.MechanicId(thick[0]), Is.EqualTo("aguape_n2"));
            BoardState lancha = LevelPackReader.Read(CurveFixtures.Pack("mundo_10_v1", "campaign", 5, 10,
                CurveFixtures.Level(10, 702, CurveFixtures.LanchaCuias, CurveFixtures.LanchaAttrs))).First.CreateBoard();
            Assert.That(AF.UI.A11y.CuiaSpeech.MechanicId(lancha[0]), Is.EqualTo("lancha"));

            LocSetUpFixture.ConfigureEs419();
            StringAssert.EndsWith("Aguapé", AF.UI.A11y.CuiaSpeech.Label(aguape, 0));
            StringAssert.EndsWith("Lancha", AF.UI.A11y.CuiaSpeech.Label(lancha, 0));
        }

        // ---- Pasos de mecánica (R10 d y f) ----

        [Test]
        public void Steps_LoosenTheWholeGroupInStageD_AndDockInStageF()
        {
            BoardState aguape = CurvePack().Find(303)!.CreateBoard();
            BoardState mid = PourRules.Apply(aguape, 2, 1, PourMode.Run, out _);
            BoardState after = PourRules.Apply(mid, 2, 0, PourMode.Run, out PourEvents ev);
            Assert.That(ev.AguapeReleasedMask, Is.EqualTo(0x3));
            var visual = new PourVisual(new Pour(2, 0, ev.Amount), ev, mid, after);
            var buffer = new StepEntry[MechanicStepPlan.MaxSteps];
            int n = MechanicStepPlan.Build(in visual, buffer);
            int loosen = 0;
            int lead = 0;
            for (int i = 0; i < n; i++)
            {
                if (buffer[i].Kind == MechanicStepKind.Loosen)
                {
                    loosen++;
                    Assert.That(buffer[i].Stage, Is.EqualTo(StepStage.D));
                    Assert.That(buffer[i].OtherIndex, Is.EqualTo(-1));
                    lead += buffer[i].Tint == MechanicStepPlan.LoosenLeadTint ? 1 : 0;
                }
            }

            Assert.That(loosen, Is.EqualTo(2), "una por cuia del grupo");
            Assert.That(lead, Is.EqualTo(1), "suena una vez por grupo");
            Assert.That(MechanicStepPlan.ReceiverSealIsStep(in visual), Is.True, "el sello de la cuia bajo la mata es un paso");

            BoardState lancha = LevelPackReader.Read(CurveFixtures.Pack("mundo_10_v1", "campaign", 5, 10,
                CurveFixtures.Level(10, 702, CurveFixtures.LanchaCuias, CurveFixtures.LanchaAttrs))).First.CreateBoard();
            BoardState docked = PourRules.Apply(lancha, 1, 2, PourMode.Run, out PourEvents dockEv);
            Assert.That(dockEv.LanchaDockedMask, Is.EqualTo(0x1));
            var dockVisual = new PourVisual(new Pour(1, 2, dockEv.Amount), dockEv, lancha, docked);
            n = MechanicStepPlan.Build(in dockVisual, buffer);
            bool dock = false;
            for (int i = 0; i < n; i++)
            {
                if (buffer[i].Kind == MechanicStepKind.Dock)
                {
                    dock = true;
                    Assert.That(buffer[i].Stage, Is.EqualTo(StepStage.F));
                    Assert.That(buffer[i].Index, Is.EqualTo(0));
                }
            }

            Assert.That(dock, Is.True);
            Assert.That(CuiaOverlays.Create(MechanicType.Aguape), Is.InstanceOf<AguapeOverlay>());
            Assert.That(CuiaOverlays.Create(MechanicType.Lancha), Is.InstanceOf<LanchaOverlay>());
            Assert.That(new AguapeOverlay().StepDurationMs(MechanicStepKind.Loosen), Is.EqualTo(700f), "unos 700 ms (02 §9.1 M8, regla 9)");
            Assert.That(new LanchaOverlay().StepDurationMs(MechanicStepKind.Dock), Is.EqualTo(1400f), "unos 1,4 s (02 §9.1 M11, regla 5)");
        }

        private LevelCatalog Create(CurveSelection curve, MechanicSwitches switches, LevelOverrides? overrides, List<string> warnings, LevelPack? reserve = null)
        {
            return LevelCatalog.Create(new[] { Campaign301To304() }, 0, overrides, switches, w => warnings.Add(w), null,
                new[] { CurvePack() }, curve, reserve == null ? null : new[] { reserve });
        }
    }
}
