#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// M4 Remolino (GDD 02 §1.2 rama del remolino en <c>legal</c>, §9.1 M4; 03 §9; 04 §5.1 regresión 4; propuesta de D-007
    /// b.2 M4; respuestas Q-A y Q-B del dueño): par de cuias unidas por debajo. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class RemolinoTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static Cuia R(int capacity, int pairId, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop).WithMechanic(MechanicType.Remolino, pairId: pairId);
        }

        private static BoardState Board(params Cuia[] cuias)
        {
            return new BoardState(cuias);
        }

        [Test]
        public void M4_TouchingOneAsTarget_PutsTheRunAtTheBottomOfThePartner_AnyColor_029_1()
        {
            // 02 §9.1 M4: «la racha vertida entra por el fondo de la pareja, de cualquier color».
            BoardState state = Board(C(4, 2, 1, 1), R(4, 0, 3), R(4, 0, 3, 3));

            Assert.That(PourRules.IsLegal(state, 0, 1), Is.True);
            BoardState after = PourRules.Apply(state, 0, 1, PourMode.Run, out PourEvents events);

            Assert.That(after[1].Packed, Is.EqualTo(state[1].Packed));
            Assert.That(after[2].Packed, Is.EqualTo(R(4, 0, 1, 1, 3, 3).Packed));
            Assert.That(after[0].Packed, Is.EqualTo(C(4, 2).Packed));
            Assert.That(events.Amount, Is.EqualTo(2));
            Assert.That(events.Target, Is.EqualTo(1));
            Assert.That(events.Receiver, Is.EqualTo(2));
            Assert.That(events.ThroughRemolino, Is.True);
        }

        [Test]
        public void M4_IsLegalIfThePartnerHasSpaceAndIsNotSealed_EvenIfTheTouchedCuiaIsFull_021_2()
        {
            // legal de 02 §1.2: «si remolino(B): return espacio(pareja(B)) > 0 and not sellada(pareja(B))», tras sellada(B).
            Assert.That(PourRules.IsLegal(Board(C(4, 1, 1), R(4, 0, 2, 3, 2, 3), R(4, 0, 1)), 0, 1), Is.True);
            Assert.That(PourRules.IsLegal(Board(C(4, 1, 1), R(4, 0, 2), R(4, 0, 2, 3, 2, 3)), 0, 1), Is.False);
            Assert.That(PourRules.IsLegal(Board(C(4, 2, 2), R(4, 0, 2), R(4, 0, 1, 1, 1, 1)), 0, 1), Is.False);
            Assert.That(PourRules.IsLegal(Board(C(4, 2, 2), R(4, 0, 1, 1, 1, 1), R(4, 0, 2)), 0, 1), Is.False);
        }

        [TestCase(PourMode.Run, 2)]
        [TestCase(PourMode.Single, 1)]
        public void M4_Amount_IsMinOfRunAndPartnerSpace_InRun_AndOneInSingle_021_2(PourMode mode, int amount)
        {
            BoardState state = Board(C(4, 2, 1, 1, 1), R(4, 0, 3, 3, 3), R(4, 0, 3, 2));

            Assert.That(PourRules.Amount(state, 0, 1, mode), Is.EqualTo(amount));
        }

        [Test]
        public void M4_AsSource_FollowsTheNormalRule_029_1()
        {
            // 02 §9.1 M4: «como origen, cada cuia del par vierte su cima con la regla normal».
            BoardState state = Board(R(4, 0, 2, 1), R(4, 0, 3), C(4, 1), C(4, 2), Cuia.Empty(4));

            Assert.That(PourRules.IsLegal(state, 0, 2), Is.True);
            Assert.That(PourRules.IsLegal(state, 0, 3), Is.False);
            Assert.That(PourRules.IsLegal(state, 0, 4), Is.True);
            BoardState after = PourRules.Apply(state, 0, 2, PourMode.Run, out PourEvents events);
            Assert.That(after[2].Packed, Is.EqualTo(C(4, 1, 1).Packed));
            Assert.That(events.ThroughRemolino, Is.False);
        }

        [Test]
        public void Regression4_0451_MonocolorToAnEmptyCuiaConnectedToARemolino_IsNotFutile()
        {
            // 04 §5.1 (4) con el anexo B: «monocolor → vacía conectada a Remolino no es fútil»; la misma A hacia una vacía
            // normal sí lo es (cap_A = cap_B = U_c).
            BoardState state = Board(C(4, 1, 1), R(4, 0), R(4, 0, 2), Cuia.Empty(4), C(4, 1, 1, 2, 2));

            Assert.That(PourRules.IsFutile(state, 0, 3), Is.True);
            Assert.That(PourRules.IsFutile(state, 0, 1), Is.False);
            Assert.That(PourRules.IsLegal(state, 0, 1), Is.True);
        }

        [Test]
        public void QA_ACuiaOfThePairCannotPourIntoItsOwnPartner_R11()
        {
            // Respuesta Q-A: verter en la propia pareja es ilegal (rechazo sin texto, R11): la racha entraría por el fondo de
            // la propia cuia de origen.
            BoardState state = Board(R(4, 0, 1), R(4, 0, 2, 1), C(4, 2, 2, 2), C(4, 1, 1));
            var session = new LevelSession(state, true, PourMode.Run);

            Assert.That(PourRules.IsLegal(state, 0, 1), Is.False);
            Assert.That(PourRules.IsLegal(state, 1, 0), Is.False);
            Assert.That(session.TryPour(0, 1, out _), Is.False);
            Assert.That(session.TryPour(1, 0, out _), Is.False);
            Assert.That(session.Current.Equals(state), Is.True);
            Assert.That(session.AppliedPours, Is.EqualTo(0));
        }

        [Test]
        public void QB_ARemolinoAsOrigin_IsAlsoExempt_TheCounterexampleIsNotStuck_R14()
        {
            // Contraejemplo de Q-B: X = [1, 1] e Y = [1, 1] en par, una cuia vacía y el resto sellado. X → vacía y vacía → X
            // (entra por el fondo de Y, que sella) gana en 2. Sin la exención de origen, X → vacía sería fútil y R13 declararía
            // Aguas estancadas con solución (rompe R14). Con la respuesta Q-B del dueño, no lo es.
            BoardState state = Board(R(4, 0, 1, 1), R(4, 0, 1, 1), Cuia.Empty(4), C(4, 2, 2, 2, 2));

            Assert.That(PourRules.IsLegal(state, 0, 1), Is.False); // Q-A
            Assert.That(PourRules.IsFutile(state, 0, 2), Is.False); // Q-B
            Assert.That(PourRules.IsStuck(state, true), Is.False);

            var session = new LevelSession(state, true, PourMode.Run);
            Assert.That(session.TryPour(0, 2, out _), Is.True);
            Assert.That(session.TryPour(2, 0, out PourResult result), Is.True);
            Assert.That(result.ThroughRemolino, Is.True);
            Assert.That(result.Receiver, Is.EqualTo(1));
            Assert.That(result.TargetSealed, Is.True);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Won));
        }

        [Test]
        public void M4_WhenOneSeals_TheOtherCanNoLongerReceive_029_1()
        {
            // 02 §9.1 M4: «ambas sellan como cualquier cuia; si una sella, la otra ya no puede recibir».
            BoardState state = Board(C(4, 2, 1), R(4, 0, 2), R(4, 0, 1, 1, 1), C(4, 2, 2));

            BoardState after = PourRules.Apply(state, 0, 1, PourMode.Run, out PourEvents events);

            Assert.That(after.IsSealed(2), Is.True);
            Assert.That(events.WasSealed(2), Is.True);
            Assert.That(PourRules.IsLegal(after, 3, 1), Is.False);
        }

        [Test]
        public void M4_Marea_UndoesThePourThroughThePortalAsOneMove_0421b()
        {
            // 04 §2.1 caso b: «un vertido a través del portal se deshace como un solo movimiento».
            BoardState state = Board(C(4, 2, 1, 1), R(4, 0, 3), R(4, 0, 3, 3), C(4, 2, 2, 2), Cuia.Empty(4));
            var session = new LevelSession(state, true, PourMode.Run);

            Assert.That(session.TryPour(0, 1, out _), Is.True);
            Assert.That(session.Undo(), Is.True);

            Assert.That(session.Current.Equals(state), Is.True);
        }

        [Test]
        public void M4_Validation_APairOfTwoRemolinos_WithoutHiddenDosesNorPirana()
        {
            // 02 §9.1 M4 (par simétrico de dos cuias de tipo remolino); propuesta b.2 M4 (un Matapi nunca forma parte del par);
            // sin ocultos [P]; sin Piraña en el nivel [P].
            int[] caps = { 4, 4, 4, 4, 4 };
            int[][] cuias = { new[] { 1, 2, 1 }, new[] { 2, 1, 2 }, new[] { 1, 2 }, new int[0], new int[0] };
            var attrs = new MechanicSpec[5];
            attrs[3] = new MechanicSpec(MechanicType.Remolino, partner: 4);
            attrs[4] = new MechanicSpec(MechanicType.Remolino, partner: 3);
            BoardBuildResult built = BoardBuilder.Build(caps, cuias, attrs);
            Assert.That(built.Error, Is.Null);
            Assert.That(LevelValidation.Validate(built.State!), Is.Null);

            attrs[4] = new MechanicSpec(MechanicType.Matapi);
            Assert.That(BoardBuilder.Build(caps, cuias, attrs).Error, Does.Contain("Remolino"));

            BoardState three = Board(C(4, 1, 2, 1), C(4, 2, 1, 2), R(4, 0, 1, 2), R(4, 0), R(4, 0));
            Assert.That(LevelValidation.Validate(three), Does.Contain("par de dos"));
            BoardState alone = Board(C(4, 1, 2, 1), C(4, 2, 1, 2), C(4, 1, 2), R(4, 0), Cuia.Empty(4));
            Assert.That(LevelValidation.Validate(alone), Does.Contain("par de dos"));

            BoardState pirana = Board(C(4, 1, 2, 1), C(4, 2, 1, 2), C(4, 1, 2), R(4, 0), R(4, 0), Cuia.Empty(2).WithMechanic(MechanicType.Pirana, k: 4));
            Assert.That(LevelValidation.Validate(pirana), Does.Contain("Piraña"));

            BoardState hidden = Board(Cuia.Create(4, new[] { 1, 2, 1 }, 0x1), C(4, 2, 1, 2), C(4, 1, 2), R(4, 0), R(4, 0));
            Assert.That(LevelValidation.Validate(hidden), Does.Contain("Remolino"));
        }
    }
}
