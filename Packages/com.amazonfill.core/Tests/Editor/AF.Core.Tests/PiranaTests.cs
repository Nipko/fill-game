#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// M7 Piraña (GDD 02 §9.1 M7 y §9.2; 03 §9; propuesta de D-007 b.2 M7 y b.2 M10 regla 7; anexo B, etapas (e) y (f′);
    /// D-007, Q10, Q14 A y Q18 B; respuesta Q-C del dueño): cada <c>k</c> vertidos útiles la piraña muerde la dosis superior
    /// de la cuia que recibió el vertido y la lleva a su cueva. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class PiranaTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static Cuia Cave(int capacity, int k, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop).WithMechanic(MechanicType.Pirana, k: k);
        }

        private static BoardState Board(params Cuia[] cuias)
        {
            return new BoardState(cuias);
        }

        /// <summary>
        /// Cueva vacía de 2 con k = 2; 2 → 3 es el primer vertido útil y el segundo decide: 1 → 4 deja [2, 2] en una vacía (la
        /// piraña muerde) y 1 → 2 sella el 2 (el mordisco se pierde).
        /// </summary>
        private static BoardState BiteBoard(Cuia fourth)
        {
            return Board(Cave(2, 2), C(4, 1, 2, 2), C(4, 2, 2, 1), C(4, 1, 1), fourth, Cuia.Empty(4));
        }

        [Test]
        public void M7_EveryKUsefulPours_BitesTheTopOfTheReceiver_IntoTheCave_029_1()
        {
            // 02 §9.1 M7: «al cumplirse, muerde la dosis superior de la última cuia destino del jugador y la lleva a su cueva»;
            // anexo B (e): una vez por vertido útil. El último destino es el del propio vertido.
            BoardState state = BiteBoard(Cuia.Empty(4));

            BoardState first = PourRules.Apply(state, 2, 3, PourMode.Run, out PourEvents one);
            Assert.That(one.CountersAdvanced, Is.True);
            Assert.That(one.PiranaBittenMask, Is.EqualTo(0));
            Assert.That(first[0].MechanicCounter, Is.EqualTo(1));

            BoardState after = PourRules.Apply(first, 1, 4, PourMode.Run, out PourEvents bite);

            Assert.That(bite.Receiver, Is.EqualTo(4));
            Assert.That(bite.PiranaBittenMask, Is.EqualTo(1 << 4));
            Assert.That(bite.PiranaCaveMask, Is.EqualTo(1 << 0));
            Assert.That(bite.PiranaBiteLostMask, Is.EqualTo(0));
            Assert.That((bite.ChangedMask & ((1 << 0) | (1 << 4))) == ((1 << 0) | (1 << 4)), Is.True);
            Assert.That(after[4].Packed, Is.EqualTo(C(4, 2).Packed));
            Assert.That(after[0].Packed, Is.EqualTo(Cave(2, 2, 2).Packed));
        }

        [Test]
        public void M7_SealingTheReceiverOnPourK_AvoidsTheBite_029_1()
        {
            // 02 §9.1 M7: «sellar la cuia destino en el vertido n·k evita el mordisco»; el contador sigue.
            BoardState state = BiteBoard(Cuia.Empty(4));
            BoardState first = PourRules.Apply(state, 2, 3, PourMode.Run);

            BoardState after = PourRules.Apply(first, 1, 2, PourMode.Run, out PourEvents events);

            Assert.That(events.WasSealed(2), Is.True);
            Assert.That(events.PiranaBittenMask, Is.EqualTo(0));
            Assert.That(events.PiranaBiteLostMask, Is.EqualTo(1 << 0));
            Assert.That(after.IsSealed(2), Is.True);
            Assert.That(after[0].Packed, Is.EqualTo(Cave(2, 2).Packed));
        }

        [Test]
        public void M7_TheMatapiIsNeverBitten_ProposalRule7()
        {
            // Propuesta b.2 M10, regla 7: «si la última cuia destino es un Matapi, ese mordisco se pierde y el contador sigue».
            BoardState state = BiteBoard(Cuia.Empty(4).WithMechanic(MechanicType.Matapi));
            BoardState first = PourRules.Apply(state, 2, 3, PourMode.Run);

            BoardState after = PourRules.Apply(first, 1, 4, PourMode.Run, out PourEvents events);

            Assert.That(events.PiranaBiteLostMask, Is.EqualTo(1 << 0));
            Assert.That(after[4].Packed, Is.EqualTo(C(4, 2, 2).WithMechanic(MechanicType.Matapi).Packed));
            Assert.That(after[0].Packed, Is.EqualTo(Cave(2, 2).Packed));
        }

        [Test]
        public void QC_WhenTheCaveIsFull_TheBiteIsLost_AndTheCounterGoesOn()
        {
            // Respuesta Q-C del dueño (A): si el contador se cumple y la piraña no puede morder porque la cueva está llena, ese
            // mordisco se pierde y el contador sigue.
            BoardState state = Board(Cave(1, 2, 2), C(4, 1, 2, 2), C(4, 2, 1), C(4, 1, 1), Cuia.Empty(4), Cuia.Empty(4));
            var session = new LevelSession(state, true, PourMode.Run);
            Assert.That(session.Counter.Blocked, Is.True);

            Assert.That(session.TryPour(2, 3, out _), Is.True);
            Assert.That(session.TryPour(1, 4, out PourResult result), Is.True);

            Assert.That(result.PiranaBiteLost, Is.True);
            Assert.That(result.PiranaBit, Is.False);
            Assert.That(session.Current[4].Packed, Is.EqualTo(C(4, 2, 2).Packed));
            Assert.That(session.Current[0].Packed, Is.EqualTo(Cave(1, 2, 2).Packed));
            Assert.That(result.Counter.Remaining, Is.EqualTo(2));
        }

        [Test]
        public void QC_WhenThePourWentToTheCave_TheBiteIsLost_AndTheCounterGoesOn()
        {
            // Respuesta Q-C del dueño (A): «o el vertido fue a la cueva».
            BoardState state = BiteBoard(Cuia.Empty(4));
            var session = new LevelSession(state, true, PourMode.Run);

            Assert.That(session.TryPour(2, 3, out _), Is.True);
            Assert.That(session.TryPour(1, 0, out PourResult result), Is.True);

            Assert.That(result.Receiver, Is.EqualTo(0));
            Assert.That(result.PiranaBiteLost, Is.True);
            Assert.That(session.Current[0].Packed, Is.EqualTo(Cave(2, 2, 2, 2).Packed));
            Assert.That(session.Counter.Remaining, Is.EqualTo(2));
            Assert.That(session.Counter.Blocked, Is.True);
        }

        [Test]
        public void Q18B_AFutilePour_DoesNotAdvanceTheCounter_NorChangeTheCanonicalState_R16()
        {
            // D-007, Q18 B: los vertidos fútiles no avanzan el contador de la Piraña (no hay mordisco sin progreso).
            BoardState state = BiteBoard(Cuia.Empty(4));
            Assert.That(PourRules.IsFutile(state, 3, 4), Is.True);

            BoardState after = PourRules.Apply(state, 3, 4, PourMode.Run, out PourEvents events);

            Assert.That(events.CountersAdvanced, Is.False);
            Assert.That(after[0].Packed, Is.EqualTo(state[0].Packed));
            Assert.That(after.CanonicalHash(), Is.EqualTo(state.CanonicalHash()));

            var session = new LevelSession(state, true, PourMode.Run);
            Assert.That(session.TryPour(3, 4, out PourResult futile), Is.True);
            Assert.That(futile.Counter.Remaining, Is.EqualTo(2));
            Assert.That(session.TryPour(2, 4, out PourResult useful), Is.True);
            Assert.That(useful.Counter.Remaining, Is.EqualTo(1));
        }

        [Test]
        public void M7_Marea_RestoresTheBite_029_2()
        {
            // 02 §9.2: «Marea restaura el mordisco» (y el contador).
            BoardState state = BiteBoard(Cuia.Empty(4));
            var session = new LevelSession(state, true, PourMode.Run);
            Assert.That(session.Counter.Type, Is.EqualTo(MechanicType.Pirana));
            Assert.That(session.Counter.CuiaIndex, Is.EqualTo(0));
            Assert.That(session.Counter.Remaining, Is.EqualTo(2));
            Assert.That(session.Counter.PendingDrops, Is.EqualTo(0));

            Assert.That(session.TryPour(2, 3, out _), Is.True);
            BoardState middle = session.Current;
            Assert.That(session.TryPour(1, 4, out PourResult bite), Is.True);
            Assert.That(bite.PiranaBit, Is.True);
            Assert.That(bite.TargetSealed, Is.False);

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Current.Equals(middle), Is.True);
            Assert.That(session.Counter.Remaining, Is.EqualTo(1));
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Current.Equals(state), Is.True);
        }

        [Test]
        public void AnexoB_ABiteThatFillsTheCave_SealsIt_AndReleasesTheLianaInTheSamePour()
        {
            // Anexo B (f′): lo que el mordisco selle o suelte ocurre en el mismo vertido. La cueva de 4 guarda [1, 1, 1]; el
            // mordisco le lleva el último 1, la sella y suelta la Liana con fruta 1.
            Cuia cave = C(4, 1, 1, 1).WithMechanic(MechanicType.Pirana, k: 2, counter: 1);
            Cuia liana = C(4, 2, 2).WithMechanic(MechanicType.Liana, key: 1);
            BoardState state = Board(cave, liana, C(4, 2, 1), C(4, 2), Cuia.Empty(4));

            BoardState after = PourRules.Apply(state, 2, 4, PourMode.Run, out PourEvents events);

            Assert.That(events.PiranaBittenMask, Is.EqualTo(1 << 4));
            Assert.That(events.WasSealed(0), Is.True);
            Assert.That(events.LianaReleasedMask, Is.EqualTo(1 << 1));
            Assert.That(after[4].IsEmpty, Is.True);
            Assert.That(after.IsSealed(0), Is.True);
            Assert.That(after[1].Mechanic, Is.EqualTo(MechanicType.None));
        }

        [Test]
        public void M7_Validation_K_OneCounter_NoHiddenDoses_NoRemolino()
        {
            // 08 §9 (k de 2 a 5), propuesta b.2 M7 (un contador por nivel; nunca con la Lluvia), D-007, Q14 A (sin ocultos) y
            // [P] (sin Remolino).
            BoardState valid = Valid();
            Assert.That(LevelValidation.Validate(valid), Is.Null);
            Assert.That(valid[5].MechanicK, Is.EqualTo(BoardBuilder.DefaultPiranaKPours));

            Cuia[] cuias = Copy(valid);
            cuias[5] = Cuia.Empty(2).WithMechanic(MechanicType.Pirana, k: 1);
            Assert.That(LevelValidation.Validate(new BoardState(cuias)), Does.Contain("k va de 2 a 5"));
            cuias[5] = Cuia.Empty(2).WithMechanic(MechanicType.Pirana, k: 4, counter: 2);
            Assert.That(LevelValidation.Validate(new BoardState(cuias)), Does.Contain("empieza en 0"));

            cuias = Copy(valid);
            cuias[4] = Cuia.Empty(4).WithMechanic(MechanicType.Lluvia, key: LevelValidation.TapajosWater, n: 4, k: 3);
            Assert.That(LevelValidation.Validate(new BoardState(cuias)), Does.Contain("un contador"));

            cuias = Copy(valid);
            cuias[2] = Cuia.Create(4, new[] { 1, 2 }, 0x1);
            Assert.That(LevelValidation.Validate(new BoardState(cuias)), Does.Contain("Q14 A"));

            cuias = Copy(valid);
            cuias[3] = Cuia.Empty(4).WithMechanic(MechanicType.Remolino, pairId: 0);
            cuias[4] = Cuia.Empty(4).WithMechanic(MechanicType.Remolino, pairId: 0);
            Assert.That(LevelValidation.Validate(new BoardState(cuias)), Does.Contain("Remolino"));
        }

        /// <summary>Nivel válido: dos colores, tres vacías de 4 y la cueva, una Cuia chica de 2 vacía (03 §9).</summary>
        private static BoardState Valid()
        {
            int[] caps = { 4, 4, 4, 4, 4, 2 };
            int[][] doses = { new[] { 1, 2, 1 }, new[] { 2, 1, 2 }, new[] { 1, 2 }, new int[0], new int[0], new int[0] };
            var attrs = new MechanicSpec[6];
            attrs[5] = new MechanicSpec(MechanicType.Pirana);
            BoardBuildResult built = BoardBuilder.Build(caps, doses, attrs, null, UnitsPerColor.Uniform(4));
            Assert.That(built.Error, Is.Null);
            return built.State!;
        }

        private static Cuia[] Copy(BoardState state)
        {
            var cuias = new Cuia[state.Count];
            for (int i = 0; i < cuias.Length; i++)
            {
                cuias[i] = state[i];
            }

            return cuias;
        }
    }
}
