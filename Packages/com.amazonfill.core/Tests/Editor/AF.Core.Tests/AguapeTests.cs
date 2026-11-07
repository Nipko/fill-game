#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// M8 Aguapé y Aguapé espeso (1.1; D-068, fase F2; 02 §9.1 M8 reglas 1-8; 02 R10 d; 03 §3 y §9; 08 §4.1, <c>pack_schema</c>
    /// 5): una regla por test, con su §. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class AguapeTests
    {
        private static Cuia C(params int[] colorsBottomToTop)
        {
            return Cuia.Create(4, colorsBottomToTop);
        }

        private static Cuia A(int group, params int[] colorsBottomToTop)
        {
            return Cuia.Create(4, colorsBottomToTop).WithMechanic(MechanicType.Aguape, pairId: group, n: 1);
        }

        private static Cuia Thick(int group, params int[] colorsBottomToTop)
        {
            return Cuia.Create(4, colorsBottomToTop).WithMechanic(MechanicType.Aguape, pairId: group, n: CurveMechanics.AguapeThickSeals);
        }

        private static BoardState Board(params Cuia[] cuias)
        {
            return new BoardState(cuias);
        }

        /// <summary>A(g3)[1,1,1] A(g3)[2] [2,2,2,1] []: la 2 sella la 0 con su 1 y suelta el grupo.</summary>
        internal static BoardState Basic()
        {
            return Board(A(3, 1, 1, 1), A(3, 2), C(2, 2, 2, 1), Cuia.Empty(4));
        }

        [Test]
        public void M8_Rule2_HeldCuiasReceiveButAreNotSources()
        {
            BoardState state = Basic();

            Assert.That(PourRules.CanBeSource(state, 0), Is.False);
            Assert.That(PourRules.IsLegal(state, 0, 3), Is.False);
            Assert.That(PourRules.IsLegal(state, 1, 3), Is.False);
            Assert.That(PourRules.IsLegal(state, 2, 0), Is.True, "recibe con la regla normal (misma cima)");
            Assert.That(PourRules.IsLegal(state, 2, 1), Is.False, "regla normal: otra cima");
        }

        [Test]
        public void M8_Rule3_R10d_TheSealThatCompletesNReleasesTheWholeGroupInTheSamePour()
        {
            BoardState after = PourRules.Apply(Basic(), 2, 0, PourMode.Run, out PourEvents events);

            Assert.That(after.IsSealed(0), Is.True);
            Assert.That(events.AguapeReleasedMask, Is.EqualTo(0x3), "la cuia que sella y la otra del grupo");
            Assert.That((events.ReleasedMask & 0x3), Is.EqualTo(0x3));
            Assert.That(after[0].HasMechanic, Is.False);
            Assert.That(after[1].HasMechanic, Is.False);
            Assert.That(PourRules.IsLegal(after, 1, 3), Is.True, "suelta, ya es origen");
        }

        [Test]
        public void M8_Rule3_AnotherGroupStaysHeld()
        {
            BoardState state = Board(A(3, 1, 1, 1), A(3, 2), A(5, 3), A(5, 3), C(2, 2, 2, 1), C(3, 3), Cuia.Empty(4));

            BoardState after = PourRules.Apply(state, 4, 0, PourMode.Run, out PourEvents events);

            Assert.That(events.AguapeReleasedMask, Is.EqualTo(0x3));
            Assert.That(after[2].Mechanic, Is.EqualTo(MechanicType.Aguape));
            Assert.That(after[3].Mechanic, Is.EqualTo(MechanicType.Aguape));
        }

        [Test]
        public void M8_Thick_TheFirstSealThinsTheMat_TheSecondReleasesIt()
        {
            // P13 A (02 §9.1 M8 regla 3): el espeso pide 2 sellos; se aclara con el primero (presentación, por el estado).
            BoardState state = Board(Thick(2, 1, 1, 1), Thick(2, 2, 2, 2), Thick(2), Thick(2), C(3, 3, 2, 1), C(3, 3));

            BoardState first = PourRules.Apply(state, 4, 0, PourMode.Run, out PourEvents one);
            Assert.That(first.IsSealed(0), Is.True);
            Assert.That(one.AguapeReleasedMask, Is.EqualTo(0));
            Assert.That(first[0].Mechanic, Is.EqualTo(MechanicType.Aguape), "la cuia sellada sigue bajo la mata hasta la suelta");
            Assert.That(CurveMechanics.AguapeSeals(first, 2), Is.EqualTo(1));
            Assert.That(PourRules.CanBeSource(first, 2), Is.False);

            BoardState second = PourRules.Apply(first, 4, 1, PourMode.Run, out PourEvents two);
            Assert.That(two.AguapeReleasedMask, Is.EqualTo(0xF));
            for (int i = 0; i < 4; i++)
            {
                Assert.That(second[i].HasMechanic, Is.False, "cuia " + i);
            }

            Assert.That(CurveMechanics.AguapeSeals(second, 2), Is.EqualTo(0));
        }

        [Test]
        public void M8_Rule5_AsTargetTheNormalFutilRuleApplies_WithoutExemption()
        {
            // 02 §9.1 M8 regla 5 (anexo B de D-007): verter una monocolor de 4 en una cuia vacía del grupo es fútil, como en una
            // vacía normal (no está en cambia_al_recibir).
            BoardState state = Board(A(0), A(0, 1, 1, 1), C(2, 2, 1), C(2, 2));

            Assert.That(PourRules.IsLegal(state, 3, 0), Is.True);
            Assert.That(PourRules.IsFutile(state, 3, 0), Is.True);
        }

        [Test]
        public void M8_Rule7_Marea_UndoingTheSealPutsTheMatBack()
        {
            var session = new LevelSession(Basic(), true, PourMode.Run);
            Assert.That(session.TryPour(2, 0, out PourResult result), Is.True);
            Assert.That(result.Events.AguapeReleasedMask, Is.EqualTo(0x3));
            Assert.That(session.Current[1].HasMechanic, Is.False);

            Assert.That(session.Undo(), Is.True);

            Assert.That(session.Current.Equals(Basic()), Is.True);
            Assert.That(session.Current[1].Mechanic, Is.EqualTo(MechanicType.Aguape));
        }

        [Test]
        public void M8_Rule4_R12_TheGroupEndsSealedOrEmpty_AndTheLevelIsWon()
        {
            BoardState state = PourRules.Apply(Basic(), 2, 0, PourMode.Run);
            state = PourRules.Apply(state, 2, 1, PourMode.Run);

            Assert.That(state.IsWon(), Is.True);
        }

        [Test]
        public void M8_R13_HeldUntilSealing_WithEverythingElseFull_IsStuck()
        {
            // 02 §9.1 M8 regla 5 y R13: el grupo lleno sin sellar no da y los demás están llenos: Aguas estancadas.
            BoardState state = Board(A(0, 1, 2, 1, 2), A(0, 2, 1, 2, 1), C(3, 3, 4, 4), C(4, 4, 3, 3));

            Assert.That(PourRules.IsStuck(state, true), Is.True);
            Assert.That(new LevelSession(state, true, PourMode.Run).Status, Is.EqualTo(LevelStatus.Stuck));
        }

        [Test]
        public void M8_R13b_DeadEndProbe_AGroupThatCanNeverSeal_IsLost()
        {
            // D-014 (R13 ampliada): hay vertidos útiles (la 2 da a las del grupo), pero ninguna cuia del grupo puede llegar a
            // sellar (las dos tienen las dos frutas debajo) y el cierre se agota sin victoria.
            BoardState state = Board(A(0, 1, 2, 1), A(0, 2, 1, 2), C(1, 2));

            Assert.That(PourRules.IsStuck(state, true), Is.False);
            Assert.That(new DeadEndProbe().IsDeadEnd(state, PourMode.Run), Is.True);
        }

        [Test]
        public void M8_Builder_PackSchema5_GroupAndN()
        {
            // 08 §4.1 (pack_schema 5): aguape {group 0-7, n 1 o 2}; sin n es 1; group solo en el Aguapé; el mismo n en el grupo.
            int[] caps = { 4, 4, 4, 4 };
            int[][] cuias = { new[] { 1, 1, 1 }, new[] { 2 }, new[] { 2, 2, 2, 1 }, new int[0] };
            var group = new MechanicSpec(MechanicType.Aguape, group: 6);
            BoardBuildResult built = BoardBuilder.Build(caps, cuias, new[] { group, group, MechanicSpec.None, MechanicSpec.None });

            Assert.That(built.Error, Is.Null);
            Assert.That(built.State![0].MechanicGroup, Is.EqualTo(6));
            Assert.That(built.State![0].MechanicN, Is.EqualTo(1));

            var thick = new MechanicSpec(MechanicType.Aguape, n: 2, group: 6);
            Assert.That(BoardBuilder.Build(caps, cuias, new[] { thick, group, MechanicSpec.None, MechanicSpec.None }).Error, Does.Contain("mismo n"));
            Assert.That(BoardBuilder.Build(caps, cuias, new[] { new MechanicSpec(MechanicType.Aguape), group, MechanicSpec.None, MechanicSpec.None }).Error, Does.Contain("group"));
            Assert.That(BoardBuilder.Build(caps, cuias, new[] { new MechanicSpec(MechanicType.Aguape, n: 3, group: 6), group, MechanicSpec.None, MechanicSpec.None }).Error, Does.Contain("group"));
            Assert.That(BoardBuilder.Build(caps, cuias, new[] { new MechanicSpec(MechanicType.Hoja, key: 1, group: 2), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None }).Error, Does.Contain("solo lo lleva el Aguapé"));
        }

        [Test]
        public void M8_Rule6_Validation_AValidGroup()
        {
            BoardState state = Board(A(5, 1, 1, 1), A(5, 2, 2), C(2, 2, 1, 3), C(3, 3, 3), Cuia.Empty(4));

            Assert.That(LevelValidation.Validate(state), Is.Null);
        }

        [Test]
        public void M8_Rule6_Validation_TheGroupIsContiguous()
        {
            BoardState state = Board(A(5, 1, 1, 1), C(2, 2, 1, 3), A(5, 2, 2), C(3, 3, 3), Cuia.Empty(4));

            Assert.That(LevelValidation.Validate(state), Does.Contain("no es contiguo"));
        }

        [Test]
        public void M8_Rule1_Validation_TwoToFiveCuias_FourToFiveInTheThick()
        {
            BoardState single = Board(A(5, 1, 1, 1), C(2, 2), C(2, 2, 1, 3), C(3, 3, 3), Cuia.Empty(4));
            Assert.That(LevelValidation.Validate(single), Does.Contain("de 2 a 5"));

            BoardState thickOfTwo = Board(Thick(1, 1, 1, 1), Thick(1, 2, 2), C(2, 2, 1, 3), C(3, 3, 3), Cuia.Empty(4));
            Assert.That(LevelValidation.Validate(thickOfTwo), Does.Contain("de 4 a 5"));
        }

        [Test]
        public void M8_Rule6_Validation_SomeCuiaOfTheGroupCanSealByReceiving()
        {
            BoardState state = Board(A(0, 1, 2), A(0, 2, 1), C(1, 1, 2, 3), C(3, 3, 3, 2), Cuia.Empty(4));

            Assert.That(LevelValidation.Validate(state), Does.Contain("ninguna cuia del grupo puede sellar"));
        }

        [Test]
        public void M8_Rule6_Validation_TheThickNeedsTwoCuiasWithDistinctFruits()
        {
            // Las dos que pueden sellar son de la fruta 1: no pueden sellar las dos (03 §3: una fruta sella en una sola cuia).
            BoardState state = Board(Thick(0, 1, 1), Thick(0, 1, 1), Thick(0, 2, 3), Thick(0, 3, 2), C(2, 2, 3, 3), Cuia.Empty(4));
            Assert.That(LevelValidation.Validate(state), Does.Contain("frutas distintas"));

            BoardState valid = Board(Thick(0, 1, 1), Thick(0, 2, 2), Thick(0, 2, 3), Thick(0, 3, 1), C(1, 3, 3, 2), Cuia.Empty(4));
            Assert.That(LevelValidation.Validate(valid), Is.Null);
        }

        [Test]
        public void M8_Rule8_Validation_WithAguasTurbias_Yes_WithTheTinaja_No()
        {
            // Añadido de D-068 del 2026-11-07, pregunta 13 (02 §9.2 regla 7): el Aguapé es un filtro y va con Aguas turbias.
            BoardState murky = Board(A(5, 1, 1, 1), A(5, 2, 2), C(3, 3, 1, 2), Cuia.Create(4, new[] { 3, 2, 3 }, 0x1), Cuia.Empty(4));
            Assert.That(LevelValidation.Validate(murky), Is.Null);

            // Nunca con la Tinaja (D-019; 02 §9.1 M8 regla 8): el tablero de estreno de la tinaja con dos cuias en un grupo.
            BoardState debut = TinajaTests.DebutS0();
            var cuias = new Cuia[debut.Count];
            for (int i = 0; i < cuias.Length; i++)
            {
                cuias[i] = debut[i];
            }

            cuias[0] = cuias[0].WithMechanic(MechanicType.Aguape, pairId: 0, n: 1);
            cuias[1] = cuias[1].WithMechanic(MechanicType.Aguape, pairId: 0, n: 1);
            Assert.That(LevelValidation.Validate(new BoardState(cuias)), Does.Contain("tinaja no se combina con aguape"));
        }

        [Test]
        public void M8_Rows_AGroupNeverSplitsBetweenRows()
        {
            // 02 §5 y §9.1 M8 regla 6; 08 §4.1 rows (pack_schema 5).
            BoardState state = Board(C(1, 1, 1), A(5, 2, 2), A(5, 1), C(2, 2, 3), C(3, 3, 3), Cuia.Empty(4));

            Assert.That(LevelValidation.ValidateRows(state, new[] { 3, 3 }), Is.Null);
            Assert.That(LevelValidation.ValidateRows(state, new[] { 2, 4 }), Does.Contain("se parte entre las filas"));
            Assert.That(LevelValidation.ValidateRows(state, new[] { 3, 2 }), Does.Contain("suma"));
            Assert.That(LevelValidation.ValidateRows(state, new[] { 1, 1, 1, 3 }), Does.Contain("de 1 a 3 filas"));
        }

        [Test]
        public void M8_Canon_03_3_GroupIdsAreRenumbered_ThePartitionIsKept()
        {
            // 03 §3 (D-068): el mismo tablero con otros ids de grupo y otro orden da el mismo hash canónico; agrupar distinto, no.
            BoardState a = Board(A(2, 1, 1), A(2, 2), A(6, 1, 1), A(6, 3), C(2, 2, 3), C(3, 3, 2), Cuia.Empty(4));
            BoardState b = Board(C(3, 3, 2), A(0, 1, 1), A(0, 3), Cuia.Empty(4), A(7, 1, 1), A(7, 2), C(2, 2, 3));
            BoardState regrouped = Board(A(2, 1, 1), A(6, 2), A(6, 3), A(2, 1, 1), C(2, 2, 3), C(3, 3, 2), Cuia.Empty(4));

            Assert.That(b.CanonicalHash(), Is.EqualTo(a.CanonicalHash()));
            Assert.That(regrouped.CanonicalHash(), Is.Not.EqualTo(a.CanonicalHash()));
        }

        [Test]
        public void M8_Canon_03_3_WithoutAguapeTheOrderAndHashDoNotChange()
        {
            // Golden compartido (03 §3; D-068): sin Aguapé, BoardCanon.Sort no renumera nada (RelabelGroups sale sin tocar el
            // array), así que estos son los hashes de la 1.0. El golden de todos los packs publicados, calculado con el núcleo de
            // 1830df08, está en DotnetOnly/CanonicalHashGoldenTests.
            BoardState plain = Board(C(1, 2, 1, 2), C(2, 1, 2, 1), Cuia.Empty(4), Cuia.Empty(4));
            BoardState mechanics = Board(
                C(1, 2, 1), Cuia.Empty(4).WithMechanic(MechanicType.Hoja, key: 2, flags: Cuia.FlagClosed),
                C(2, 1, 1).WithMechanic(MechanicType.Remolino, pairId: 0), Cuia.Empty(4).WithMechanic(MechanicType.Remolino, pairId: 0),
                C(2, 1).WithMechanic(MechanicType.Brote, flags: Cuia.FlagClosed), Cuia.Empty(4));

            Assert.That(plain.CanonicalHash(), Is.EqualTo(GoldenPlain));
            Assert.That(mechanics.CanonicalHash(), Is.EqualTo(GoldenMechanics));
        }

        private const ulong GoldenPlain = 0xB7CBA6310B1C20DFUL;
        private const ulong GoldenMechanics = 0xE0C5FA248EFE1A0CUL;
    }
}
