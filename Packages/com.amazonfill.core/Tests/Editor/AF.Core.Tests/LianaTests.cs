#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// M3 Liana (D-007: con fruta desde el 83, de cuenta como variante del 243; propuesta de D-007 b.2 M3 y b.3; anexo B,
    /// etapas (d) y (f′); 02 §9.1 M3; 03 §9): atada, no es origen ni destino; se suelta por sellados. Test compartido
    /// (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class LianaTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static Cuia LianaFruit(int key, int capacity, params int[] colorsBottomToTop)
        {
            return C(capacity, colorsBottomToTop).WithMechanic(MechanicType.Liana, key: key);
        }

        private static Cuia LianaCount(int n, int capacity, params int[] colorsBottomToTop)
        {
            return C(capacity, colorsBottomToTop).WithMechanic(MechanicType.Liana, n: n);
        }

        /// <summary>Liana con fruta 1 que guarda [2, 2]; el vertido 1 → 2 sella el color 1.</summary>
        private static BoardState FruitBoard()
        {
            return new BoardState(new[] { LianaFruit(1, 4, 2, 2), C(4, 2, 1), C(4, 1, 1, 1), C(4, 2), Cuia.Empty(4) });
        }

        [Test]
        public void M3_TiedLiana_IsNeitherSourceNorTarget_R11()
        {
            // Propuesta b.2 M3, regla 1: atada, no es origen ni destino (vertido ilegal, R11).
            BoardState state = FruitBoard();

            Assert.That(PourRules.CanBeSource(state, 0), Is.False);
            for (int a = 1; a < state.Count; a++)
            {
                Assert.That(PourRules.IsLegal(state, a, 0), Is.False);
                Assert.That(PourRules.IsLegal(state, 0, a), Is.False);
            }
        }

        [Test]
        public void AnexoB_ThePourThatSealsC_ReleasesTheLianaInTheSamePour()
        {
            // Test obligatorio del anexo B: (c) sella c y (d) suelta la Liana con fruta c en el mismo vertido; la cuia queda
            // normal (02 R16 para D-007) y ya puede ser origen.
            BoardState state = FruitBoard();

            BoardState after = PourRules.Apply(state, 1, 2, PourMode.Run, out PourEvents events);

            Assert.That(events.WasSealed(2), Is.True);
            Assert.That(events.LianaReleasedMask, Is.EqualTo(1 << 0));
            Assert.That(events.ReleasedMask, Is.EqualTo(1 << 0));
            Assert.That(after[0].Mechanic, Is.EqualTo(MechanicType.None));
            Assert.That(after[0].Packed, Is.EqualTo(C(4, 2, 2).Packed));
            Assert.That(PourRules.CanBeSource(after, 0), Is.True);
            Assert.That(PourRules.IsLegal(after, 0, 1), Is.True);
        }

        [Test]
        public void M3_WithFruit_ASealOfAnotherColor_DoesNotReleaseIt()
        {
            var state = new BoardState(new[] { LianaFruit(1, 4, 1), C(4, 2, 2, 2), C(4, 1, 2), C(4, 1, 1), Cuia.Empty(4) });

            BoardState after = PourRules.Apply(state, 2, 1, PourMode.Run, out PourEvents events);

            Assert.That(events.WasSealed(1), Is.True);
            Assert.That(events.LianaReleasedMask, Is.EqualTo(0));
            Assert.That(after[0].Mechanic, Is.EqualTo(MechanicType.Liana));
        }

        [TestCase(1, true)]
        [TestCase(2, false)]
        public void M3_Count_ReleasesWhenThereAreNSealedCuias(int n, bool releasedByTheFirstSeal)
        {
            // Propuesta b.3, Liana de cuenta (la regla del canon 02 §9.1 M3): se suelta al haber n cuias selladas.
            var state = new BoardState(new[] { LianaCount(n, 4, 3, 3), C(4, 2, 1), C(4, 1, 1, 1), C(4, 2, 2, 2), C(4, 3, 3) });

            BoardState after = PourRules.Apply(state, 1, 2, PourMode.Run, out PourEvents first);
            Assert.That(first.LianaReleasedMask != 0, Is.EqualTo(releasedByTheFirstSeal));
            after = PourRules.Apply(after, 1, 3, PourMode.Run, out PourEvents second);

            Assert.That(after.IsSealed(3), Is.True);
            Assert.That(after[0].Mechanic, Is.EqualTo(MechanicType.None));
            Assert.That(second.LianaReleasedMask != 0, Is.EqualTo(!releasedByTheFirstSeal));
        }

        [Test]
        public void M3_Marea_TiesItAgain_AndRestartToo()
        {
            // Propuesta b.2 M3, reglas 2-3: Marea deshace el sellado y vuelve a atar la liana; Volver a la orilla también.
            BoardState initial = FruitBoard();
            var session = new LevelSession(initial, true);

            Assert.That(session.TryPour(1, 2, out PourResult result), Is.True);
            Assert.That(result.Events.LianaReleasedMask, Is.EqualTo(1));
            Assert.That(session.Current[0].Mechanic, Is.EqualTo(MechanicType.None));
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Current[0].Mechanic, Is.EqualTo(MechanicType.Liana));
            Assert.That(PourRules.CanBeSource(session.Current, 0), Is.False);
            session.TryPour(1, 2, out _);
            session.Restart();
            Assert.That(session.Current.Equals(initial), Is.True);
        }

        [Test]
        public void M3_Validation_WithBarro_IsNotMixedCapacities_D4_138()
        {
            // El Barro (15) no es un color (02 §9.1 M5): no cuenta como un color de U = 1 en el bloqueo provisional de la Liana
            // con capacidades mixtas (C1-9). Ba+Li va en el 138 del calendario d.4 de D-007.
            var state = new BoardState(new[]
            {
                LianaFruit(1, 4, 2, 2), C(4, 2, Cuia.MudColor, 1), C(4, 1, 1, 1), C(4, 2), Cuia.Empty(4),
            });

            Assert.That(LevelValidation.Validate(state), Is.Null);
        }

        [Test]
        public void M3_Validation_FruitCanSealOutside_AndNoHiddenDoses()
        {
            // Propuesta b.2 M3, regla 4 (c puede sellar fuera de la Liana) y Q14 A (sin Aguas turbias con disparadores).
            int[] caps = { 4, 4, 4, 4, 4 };
            int[][] cuias = { new[] { 2, 2 }, new[] { 2, 1 }, new[] { 1, 1, 1 }, new[] { 2 }, new int[0] };
            BoardState Build(MechanicSpec spec, int[][]? hidden = null)
            {
                var attrs = new MechanicSpec[5];
                attrs[0] = spec;
                BoardBuildResult built = BoardBuilder.Build(caps, cuias, attrs, hidden);
                Assert.That(built.IsValid, Is.True, built.Error);
                return built.State!;
            }

            Assert.That(LevelValidation.Validate(Build(new MechanicSpec(MechanicType.Liana, key: 1))), Is.Null);
            Assert.That(LevelValidation.Validate(Build(new MechanicSpec(MechanicType.Liana, key: 2))), Does.Contain("fuera"));
            Assert.That(LevelValidation.Validate(Build(new MechanicSpec(MechanicType.Liana, key: 5))), Does.Contain("tablero"));
            Assert.That(LevelValidation.Validate(Build(new MechanicSpec(MechanicType.Liana, n: 1))), Is.Null);
            Assert.That(LevelValidation.Validate(Build(new MechanicSpec(MechanicType.Liana, n: 2))), Does.Contain("n colores"));
            Assert.That(LevelValidation.Validate(Build(new MechanicSpec(MechanicType.Liana, key: 1), new[] { new[] { 2, 0 } })), Does.Contain("Q14"));
        }
    }
}
