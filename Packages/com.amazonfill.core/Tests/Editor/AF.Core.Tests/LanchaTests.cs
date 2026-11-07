#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// M11 Lancha (1.1; D-068, fase F2; 02 §9.1 M11 reglas 1-4; 02 R10 f; 03 §3 y §9; 08 §4.1, <c>pack_schema</c> 5): una regla
    /// por test, con su §. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class LanchaTests
    {
        private static Cuia C(params int[] colorsBottomToTop)
        {
            return Cuia.Create(4, colorsBottomToTop);
        }

        private static Cuia L(int k, params int[] colorsBottomToTop)
        {
            return Cuia.Create(4, colorsBottomToTop).WithMechanic(MechanicType.Lancha, k: k);
        }

        private static BoardState Board(params Cuia[] cuias)
        {
            return new BoardState(cuias);
        }

        /// <summary>L(k=2)[3,3] [1,1] [1,1] [3,3] []: juntar el 1 deja 2 vacías y la lancha atraca.</summary>
        internal static BoardState Basic()
        {
            return Board(L(2, 3, 3), C(1, 1), C(1, 1), C(3, 3), Cuia.Empty(4));
        }

        [Test]
        public void M11_Rule2_BeforeDockingItIsNeitherSourceNorTarget()
        {
            BoardState state = Basic();

            Assert.That(PourRules.CanBeSource(state, 0), Is.False);
            Assert.That(PourRules.IsLegal(state, 0, 4), Is.False);
            Assert.That(PourRules.IsLegal(state, 3, 0), Is.False, "ni de su mismo color");
        }

        [Test]
        public void M11_Rule2_R10f_ItDocksInThePourThatLeavesKEmpties_AndStaysDocked()
        {
            BoardState docked = PourRules.Apply(Basic(), 1, 2, PourMode.Run, out PourEvents events);

            Assert.That(events.LanchaDockedMask, Is.EqualTo(0x1));
            Assert.That((events.ReleasedMask & 0x1), Is.EqualTo(0x1));
            Assert.That(docked[0].HasMechanic, Is.False);
            Assert.That(PourRules.IsLegal(docked, 0, 3), Is.True);

            // Atracada es una cuia normal hasta el final (03 §9: el bit no se deduce del contenido): vierte y el nivel se gana.
            BoardState won = PourRules.Apply(docked, 0, 3, PourMode.Run);
            Assert.That(won.IsWon(), Is.True);
        }

        [Test]
        public void M11_Rule2_NotYet_WithFewerThanKEmpties()
        {
            BoardState state = Board(L(3, 3, 3), C(1, 1), C(1, 1), C(3, 3), Cuia.Empty(4));

            BoardState after = PourRules.Apply(state, 1, 2, PourMode.Run, out PourEvents events);

            Assert.That(events.LanchaDockedMask, Is.EqualTo(0));
            Assert.That(after[0].Mechanic, Is.EqualTo(MechanicType.Lancha));
            Assert.That(CurveMechanics.LanchaEmpties(after, 0), Is.EqualTo(2));
        }

        [Test]
        public void M11_Rule2_Marea_UndoingTheDockingPullsItAway()
        {
            var session = new LevelSession(Basic(), true, PourMode.Run);
            Assert.That(session.TryPour(1, 2, out PourResult result), Is.True);
            Assert.That(result.Events.LanchaDockedMask, Is.EqualTo(0x1));

            Assert.That(session.Undo(), Is.True);

            Assert.That(session.Current.Equals(Basic()), Is.True);
            Assert.That(session.Current[0].Mechanic, Is.EqualTo(MechanicType.Lancha));
        }

        [Test]
        public void M11_Canon_03_3_DockedIsPartOfTheCanonicalKey()
        {
            // 03 §9: «1 bit atracada en la clave canónica». La misma carga atracada o sin atracar son estados distintos.
            BoardState waiting = Board(L(2, 3, 3), C(1, 1, 1, 1), C(3, 3), Cuia.Empty(4), Cuia.Empty(4));
            BoardState docked = Board(C(3, 3), C(1, 1, 1, 1), C(3, 3), Cuia.Empty(4), Cuia.Empty(4));

            Assert.That(waiting.CanonicalHash(), Is.Not.EqualTo(docked.CanonicalHash()));
        }

        [Test]
        public void M11_R13_MissingEmpties_AndNoUsefulPours_IsStuck()
        {
            // 02 §9.1 M11 regla 3: antes de atracar no cuenta como vertido posible (R13). Todo lo demás lleno: Aguas estancadas.
            BoardState state = Board(L(2, 3), C(1, 2, 1, 2), C(2, 1, 2, 1), Cuia.Create(3, new[] { 3, 3, 3 }));

            Assert.That(PourRules.IsStuck(state, true), Is.True);
            Assert.That(new LevelSession(state, true, PourMode.Run).Status, Is.EqualTo(LevelStatus.Stuck));
        }

        [Test]
        public void M11_R13b_DeadEndProbe_TheEmptiesCanNeverReachK_IsLost()
        {
            // D-014 (R13 ampliada): quedan vertidos útiles, pero con 8 dosis en 3 cuias de 4 nunca hay 3 vacías.
            BoardState state = Board(L(3, 3), C(1, 1, 2, 2), C(2, 2, 1, 1), Cuia.Empty(4));

            Assert.That(PourRules.IsStuck(state, true), Is.False);
            Assert.That(new DeadEndProbe().IsDeadEnd(state, PourMode.Run), Is.True);
        }

        [Test]
        public void M11_CuiaExtra_TheSessionSettlesItsBoard_AndTheLanchaDocksAtOnce()
        {
            // [P] (02 §9.1 M11: «en cuanto el tablero tiene k o más cuias vacías»): la Cuia extra o de rescate rehace la sesión
            // con una vacía de más (04 §2.2); si con ella hay k vacías, la lancha atraca sin esperar a un vertido.
            BoardState level = Board(L(2, 3, 3), C(1, 1), C(1, 1), C(3, 3), Cuia.Empty(4));
            Assert.That(PourRules.Settle(level, out _), Is.SameAs(level), "un nivel válido no cambia");

            BoardState withExtra = Board(L(2, 3, 3), C(1, 1), C(1, 1), C(3, 3), Cuia.Empty(4), Cuia.Empty(4));
            var session = new LevelSession(withExtra, true, PourMode.Run);

            Assert.That(session.Initial[0].HasMechanic, Is.False);
            Assert.That(session.Current[0].HasMechanic, Is.False);
            Assert.That(PourRules.Settle(withExtra, out PourEvents events)[0].HasMechanic, Is.False);
            Assert.That(events.LanchaDockedMask, Is.EqualTo(0x1));
        }

        [Test]
        public void M11_Builder_PackSchema5_K()
        {
            // 08 §4.1 (pack_schema 5): lancha {k}, k ≥ 2 (3 bits: ≤ 7) y sin otros campos.
            int[] caps = { 4, 4, 4, 4, 4 };
            int[][] cuias = { new[] { 3, 3 }, new[] { 1, 1 }, new[] { 1, 1 }, new[] { 3, 3 }, new int[0] };
            MechanicSpec[] attrs = { new MechanicSpec(MechanicType.Lancha, k: 2), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None };

            BoardBuildResult built = BoardBuilder.Build(caps, cuias, attrs);
            Assert.That(built.Error, Is.Null);
            Assert.That(built.State![0].MechanicK, Is.EqualTo(2));

            attrs[0] = new MechanicSpec(MechanicType.Lancha, k: 1);
            Assert.That(BoardBuilder.Build(caps, cuias, attrs).Error, Does.Contain("de 2 a 7"));
            attrs[0] = new MechanicSpec(MechanicType.Lancha, k: 8);
            Assert.That(BoardBuilder.Build(caps, cuias, attrs).Error, Does.Contain("de 2 a 7"));
            attrs[0] = new MechanicSpec(MechanicType.Lancha, key: 3, k: 2);
            Assert.That(BoardBuilder.Build(caps, cuias, attrs).Error, Does.Contain("solo k"));
        }

        [Test]
        public void M11_Rule1_Validation_AValidLancha()
        {
            BoardState state = Board(L(2, 3, 3), C(1, 1, 2), C(2, 2, 1), C(3, 3, 1, 2), Cuia.Empty(4));

            Assert.That(LevelValidation.Validate(state), Is.Null);
        }

        [Test]
        public void M11_Rule1_Validation_KIsAtLeastTheInitialEmptiesPlusOne()
        {
            BoardState state = Board(L(2, 3, 3), C(1, 1, 2), C(2, 2, 1), C(3, 3, 1, 2), Cuia.Empty(4), Cuia.Empty(4));

            Assert.That(LevelValidation.Validate(state), Does.Contain("vacíos iniciales + 1"));
        }

        [Test]
        public void M11_Rule1_Validation_OneLanchaPerLevel_WithOneToThreeDoses()
        {
            BoardState two = Board(L(2, 3, 3), L(2, 3, 1), C(1, 1, 2, 2), C(2, 2, 1, 3), Cuia.Empty(4));
            Assert.That(LevelValidation.Validate(two), Does.Contain("una sola lancha"));

            BoardState empty = Board(L(2), C(1, 1, 2, 3), C(2, 2, 1, 3), C(3, 3, 1, 2), Cuia.Empty(4));
            Assert.That(LevelValidation.Validate(empty), Does.Contain("de 1 a 3 dosis"));
        }

        [Test]
        public void M11_Rule4_Validation_NeverWithAguasTurbias_NorWithTheTinaja()
        {
            BoardState murky = Board(L(2, 3, 3), C(1, 1, 2), C(2, 2, 1), Cuia.Create(4, new[] { 3, 1, 3 }, 0x1), C(2), Cuia.Empty(4));
            Assert.That(LevelValidation.Validate(murky), Does.Contain("Aguas turbias no se combina con lancha"));

            BoardState debut = TinajaTests.DebutS0();
            var cuias = new Cuia[debut.Count];
            for (int i = 0; i < cuias.Length; i++)
            {
                cuias[i] = debut[i];
            }

            cuias[2] = cuias[2].WithMechanic(MechanicType.Lancha, k: 2);
            Assert.That(LevelValidation.Validate(new BoardState(cuias)), Does.Contain("tinaja no se combina con lancha"));
        }
    }
}
