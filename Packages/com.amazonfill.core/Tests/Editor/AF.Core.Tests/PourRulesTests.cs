#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Rules;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// Vertido legal, cantidad, vertido parcial, sellado y victoria (GDD 02 §1.1-1.3: R3, R5, R9, R12).
    /// Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class PourRulesTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static BoardState Board(params Cuia[] cuias)
        {
            return new BoardState(cuias);
        }

        [Test]
        public void IsLegal_SameCuia_IsFalse()
        {
            BoardState state = Board(C(4, 1, 2), Cuia.Empty(4));

            Assert.That(PourRules.IsLegal(state, 0, 0), Is.False);
        }

        [Test]
        public void IsLegal_FromEmpty_IsFalse()
        {
            BoardState state = Board(Cuia.Empty(4), C(4, 1));

            Assert.That(PourRules.IsLegal(state, 0, 1), Is.False);
        }

        [Test]
        public void IsLegal_ToEmpty_IsTrue()
        {
            BoardState state = Board(C(4, 1, 2), Cuia.Empty(4));

            Assert.That(PourRules.IsLegal(state, 0, 1), Is.True);
        }

        [Test]
        public void IsLegal_SameTopColorWithSpace_IsTrue()
        {
            BoardState state = Board(C(4, 1, 2), C(4, 3, 2));

            Assert.That(PourRules.IsLegal(state, 0, 1), Is.True);
        }

        [Test]
        public void IsLegal_DifferentTopColor_IsFalse()
        {
            BoardState state = Board(C(4, 1, 2), C(4, 2, 1));

            Assert.That(PourRules.IsLegal(state, 0, 1), Is.False);
        }

        [Test]
        public void IsLegal_ToFullCuia_IsFalse()
        {
            BoardState state = Board(C(4, 2), C(4, 1, 1, 1, 2));

            Assert.That(PourRules.IsLegal(state, 0, 1), Is.False);
        }

        [Test]
        public void IsLegal_FromSealedCuia_IsFalse()
        {
            // R5: sellada no es origen ni destino.
            BoardState state = Board(C(4, 1, 1, 1, 1), Cuia.Empty(4));

            Assert.That(state.IsSealed(0), Is.True);
            Assert.That(PourRules.IsLegal(state, 0, 1), Is.False);
            Assert.That(PourRules.Amount(state, 0, 1), Is.EqualTo(0));
        }

        [Test]
        public void CanBeSource_EmptyOrSealed_IsFalse()
        {
            // 02 §1.2 (primera condición de legal) y 02 §3.1 C1-C2: origen = no vacía y no sellada. La Cuia chica llena
            // y monocolor no sella (R5), así que sigue pudiendo ser origen.
            BoardState state = Board(C(4, 1, 1, 1, 1), Cuia.Empty(4), C(4, 2, 3), C(2, 2, 2), C(4, 3, 3, 3, 2));

            Assert.That(PourRules.CanBeSource(state, 0), Is.False);
            Assert.That(PourRules.CanBeSource(state, 1), Is.False);
            Assert.That(PourRules.CanBeSource(state, 2), Is.True);
            Assert.That(PourRules.CanBeSource(state, 3), Is.True);
            Assert.That(PourRules.CanBeSource(state, 4), Is.True);
            Assert.That(() => { PourRules.CanBeSource(state, 5); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void IsLegal_ImpliesCanBeSource_OnEveryPair()
        {
            // Una sola condición de origen para legal(A, B) y para la selección del cliente (04 §5.1: sin reglas duplicadas).
            BoardState state = Board(C(4, 1, 1, 1, 1), Cuia.Empty(4), C(4, 2, 3), C(2, 2, 2), C(4, 3, 3, 3, 2), C(4, 3));
            for (int a = 0; a < state.Count; a++)
            {
                bool anyLegal = false;
                for (int b = 0; b < state.Count; b++)
                {
                    anyLegal |= PourRules.IsLegal(state, a, b);
                }

                if (anyLegal)
                {
                    Assert.That(PourRules.CanBeSource(state, a), Is.True, "cuia " + a);
                }

                if (!PourRules.CanBeSource(state, a))
                {
                    Assert.That(anyLegal, Is.False, "cuia " + a);
                }
            }
        }

        [Test]
        public void IsLegal_IndexOutOfBoard_Throws()
        {
            BoardState state = Board(C(4, 1), Cuia.Empty(4));

            Assert.That(() => { PourRules.IsLegal(state, 0, 2); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void SmallCuia_FullAndMonocolor_IsNotSealed()
        {
            // R5: una cuia de capacidad menor que U_c nunca sella ese color; es un búfer y sigue siendo origen.
            BoardState state = Board(C(2, 1, 1), C(4, 2, 1, 1), Cuia.Empty(4));

            Assert.That(state.UnitsOf(1), Is.EqualTo(4));
            Assert.That(state.IsSealed(0), Is.False);
            Assert.That(PourRules.IsLegal(state, 0, 1), Is.True);
            Assert.That(PourRules.IsLegal(state, 0, 2), Is.True);
        }

        [Test]
        public void FullMonocolorWithHiddenDose_IsNotSealed()
        {
            // R5: sellada exige todas las dosis visibles.
            BoardState state = Board(Cuia.Create(4, new[] { 1, 1, 1, 1 }, 0x1), Cuia.Empty(4));

            Assert.That(state.IsSealed(0), Is.False);
            Assert.That(PourRules.Amount(state, 0, 1), Is.EqualTo(3));
        }

        [Test]
        public void Apply_PartialPour_LeavesRestInSource()
        {
            // R9: la racha (3) no cabe entera en el destino (1 hueco): se vierte lo que cabe.
            BoardState state = Board(C(4, 2, 1, 1, 1), C(4, 3, 3, 1));

            Assert.That(PourRules.Amount(state, 0, 1), Is.EqualTo(1));

            BoardState next = PourRules.Apply(state, 0, 1);

            Assert.That(next[0], Is.EqualTo(C(4, 2, 1, 1)));
            Assert.That(next[1], Is.EqualTo(C(4, 3, 3, 1, 1)));
        }

        [Test]
        public void Apply_MovesOnlyTheVisibleRun_AndRevealsNewTop()
        {
            // R6 + R7: la dosis oculta corta la racha; al quedar arriba, pasa a ser visible.
            BoardState state = Board(Cuia.Create(4, new[] { 1, 1, 1 }, 0x1), Cuia.Empty(4));

            BoardState next = PourRules.Apply(state, 0, 1);

            Assert.That(next[1], Is.EqualTo(C(4, 1, 1)));
            Assert.That(next[0].Count, Is.EqualTo(1));
            Assert.That(next[0].HasHidden, Is.False);
        }

        [Test]
        public void Apply_DoesNotModifyTheOriginalState()
        {
            BoardState state = Board(C(4, 1, 2), Cuia.Empty(4));

            PourRules.Apply(state, 0, 1);

            Assert.That(state[0], Is.EqualTo(C(4, 1, 2)));
            Assert.That(state[1].IsEmpty, Is.True);
        }

        [Test]
        public void Apply_IllegalPour_Throws()
        {
            BoardState state = Board(C(4, 1, 2), C(4, 2, 1));

            Assert.That(() => { PourRules.Apply(state, 0, 1); }, Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void TwoPartialMonocolorCuias_AreNotVictory()
        {
            // R12: monocolor significa monocolor y llena.
            BoardState state = Board(C(4, 1, 1), C(4, 1, 1), Cuia.Empty(4));

            Assert.That(state.IsWon(), Is.False);
        }

        [Test]
        public void EmptyAndSealedCuias_AreVictory()
        {
            BoardState state = Board(C(4, 1, 1, 1, 1), Cuia.Empty(4), C(5, 2, 2, 2, 2, 2), Cuia.Empty(2));

            Assert.That(state.IsWon(), Is.True);
        }

        [Test]
        public void BoardState_RejectsMudUntilItsMechanicExists()
        {
            Assert.That(() => { Board(C(4, Cuia.MudColor), Cuia.Empty(4)); }, Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void BoardState_RejectsDefaultCuia()
        {
            Assert.That(() => { Board(default(Cuia), Cuia.Empty(4)); }, Throws.TypeOf<ArgumentException>());
        }
    }
}
