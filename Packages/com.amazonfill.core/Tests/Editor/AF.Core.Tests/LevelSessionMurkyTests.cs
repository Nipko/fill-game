#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// Aguas turbias en la partida (C2-1): 02 R7 y §11 (T6), 04 §2.1 (Marea conserva lo revelado), §2.4 a (Pororoca revela
    /// todo), §2.5 (Luciérnaga revela una cuia), §2.7 (Volver a la orilla conserva las capas reveladas) y §5.1 (regresión 3).
    /// Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class LevelSessionMurkyTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static Cuia Murky(int capacity, int hiddenMask, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop, hiddenMask);
        }

        /// <summary>[2*, 1], [1, 1, 1], [2, 2, 2], vacía: el vertido 0 → 1 sella el 1 y revela el 2 (R7).</summary>
        private static BoardState RevealByPour()
        {
            return new BoardState(new[] { Murky(4, 0x1, 2, 1), C(4, 1, 1, 1), C(4, 2, 2, 2), Cuia.Empty(4) });
        }

        /// <summary>[3*, 2*, 1], [1, 1, 1], [2, 2, 2], [3, 3, 3], vacía.</summary>
        private static BoardState OneMurkyCuia()
        {
            return new BoardState(new[] { Murky(4, 0x3, 3, 2, 1), C(4, 1, 1, 1), C(4, 2, 2, 2), C(4, 3, 3, 3), Cuia.Empty(4) });
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void T6_R7_ADoseRevealedByAPour_StaysVisibleAfterMarea_0421(PourMode mode)
        {
            // 04 §2.1: «lo único que no se revierte es el conocimiento: las unidades reveladas siguen visibles».
            BoardState initial = RevealByPour();
            var session = new LevelSession(initial, true, mode);
            Assert.That(session.HiddenCount, Is.EqualTo(1));

            Assert.That(session.TryPour(0, 1, out PourResult result), Is.True);
            Assert.That(result.Events.SourceRevealed, Is.True);
            Assert.That(result.RevealedMask, Is.EqualTo(1 << 0));
            Assert.That(session.RevealedLayers(0), Is.EqualTo(0x1));

            Assert.That(session.Undo(), Is.True);

            Assert.That(session.Current[0].Packed, Is.EqualTo(C(4, 2, 1).Packed));
            Assert.That(session.Current[0].IsHiddenAt(0), Is.False);
            Assert.That(session.HiddenCount, Is.EqualTo(0));
            Assert.That(session.AppliedPours, Is.EqualTo(0));
            Assert.That(initial[0].IsHiddenAt(0), Is.True); // el tablero inicial no cambia
            Assert.That(session.Initial.Equals(initial), Is.True);
        }

        [Test]
        public void T6_InSingle_TheDoseIsRevealedWhenTheLastDoseOfTheRunLeaves_AndMareaKeepsIt()
        {
            // R10 (a) en single: el revelado llega cuando sale la última dosis de la racha; dos Mareas no lo ocultan.
            var initial = new BoardState(new[] { Murky(4, 0x1, 2, 1, 1), C(4, 1, 1), C(4, 2, 2, 2), Cuia.Empty(4) });
            var session = new LevelSession(initial, true, PourMode.Single);

            Assert.That(session.TryPour(0, 1, out PourResult first), Is.True);
            Assert.That(first.RevealedMask, Is.EqualTo(0));
            Assert.That(session.TryPour(0, 1, out PourResult second), Is.True);
            Assert.That(second.RevealedMask, Is.EqualTo(1 << 0));

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Undo(), Is.True);

            Assert.That(session.Current[0].Packed, Is.EqualTo(C(4, 2, 1, 1).Packed));
        }

        [Test]
        public void T6_Restart_KeepsTheRevealedDoses_0427()
        {
            // 04 §2.7: Volver a la orilla restaura el tablero inicial con la misma asignación de capas ocultas y conserva las
            // capas reveladas (I4 con I5).
            BoardState initial = RevealByPour();
            var session = new LevelSession(initial, true, PourMode.Run);
            Assert.That(session.TryPour(0, 1, out _), Is.True);
            Assert.That(session.TryPour(2, 0, out _), Is.True);

            session.Restart();

            Assert.That(session.AppliedPours, Is.EqualTo(0));
            Assert.That(session.Current[0].Packed, Is.EqualTo(C(4, 2, 1).Packed));
            Assert.That(session.Current.Equals(initial), Is.False);
            for (int i = 1; i < initial.Count; i++)
            {
                Assert.That(session.Current[i].Packed, Is.EqualTo(initial[i].Packed));
            }

            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
        }

        [Test]
        public void T6_Luciernaga_RevealsAllHiddenDosesOfOneCuia_ThenMareaKeepsThem_0425()
        {
            // 04 §2.5: la Luciérnaga revela todas las unidades ocultas de una cuia; «lo revelado permanece visible tras Marea
            // y tras Volver a la orilla». No entra en el historial (Marea no deshace comodines, 04 §2.1).
            BoardState initial = OneMurkyCuia();
            var session = new LevelSession(initial, true, PourMode.Run);
            Assert.That(session.TryPour(2, 4, out _), Is.True); // un vertido que no revela nada

            Assert.That(session.Reveal(0), Is.EqualTo(2));

            Assert.That(session.HiddenCount, Is.EqualTo(0));
            Assert.That(session.RevealedLayers(0), Is.EqualTo(0x3));
            Assert.That(session.AppliedPours, Is.EqualTo(1));

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Current[0].Packed, Is.EqualTo(C(4, 3, 2, 1).Packed));
            Assert.That(session.Current[2].Packed, Is.EqualTo(initial[2].Packed));
            Assert.That(session.CanUndo, Is.False);

            session.Restart();
            Assert.That(session.Current[0].Packed, Is.EqualTo(C(4, 3, 2, 1).Packed));
            Assert.That(session.Reveal(0), Is.EqualTo(0));
        }

        [Test]
        public void T6_Pororoca_RevealAll_RevealsEveryHiddenDoseOfTheLevel_0424a()
        {
            // 04 §2.4 a: la ola revela todas las capas ocultas del nivel; permanecen reveladas.
            var initial = new BoardState(new[]
            {
                Murky(4, 0x3, 3, 2, 1), Murky(4, 0x1, 2, 1), C(4, 1, 1), C(4, 2, 2), C(4, 3, 3, 3), Cuia.Empty(4),
            });
            var session = new LevelSession(initial, true, PourMode.Run);

            Assert.That(session.RevealAll(), Is.EqualTo(3));

            Assert.That(session.HiddenCount, Is.EqualTo(0));
            Assert.That(session.RevealedLayers(0), Is.EqualTo(0x3));
            Assert.That(session.RevealedLayers(1), Is.EqualTo(0x1));
            Assert.That(session.RevealAll(), Is.EqualTo(0));
            Assert.That(session.TryPour(4, 5, out _), Is.True);
            Assert.That(session.Undo(), Is.True);
            session.Restart();
            Assert.That(session.HiddenCount, Is.EqualTo(0));
            Assert.That(initial.HiddenCount, Is.EqualTo(3));
        }

        [Test]
        public void T6_Reveal_OnACuiaWithoutHiddenDoses_ReturnsZero_AndChecksTheIndex()
        {
            var session = new LevelSession(OneMurkyCuia(), true, PourMode.Run);

            Assert.That(session.Reveal(1), Is.EqualTo(0));
            Assert.That(session.HiddenCount, Is.EqualTo(2));
            Assert.That(() => { session.Reveal(5); }, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => { session.RevealedLayers(-1); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void T6_ARevealedMurkyCuia_PlaysLikeAVisibleOne()
        {
            // Revelar no cambia el contenido: solo la visibilidad. Tras revelar, la racha ya no se corta en las dosis que
            // estaban ocultas (R6) y el sellado (R5) las admite.
            var initial = new BoardState(new[] { Murky(4, 0x1, 1, 1, 1), C(4, 1), C(4, 2, 2, 2, 2), Cuia.Empty(4) });
            var session = new LevelSession(initial, true, PourMode.Run);
            Assert.That(session.Current[0].TopRunLength, Is.EqualTo(2));

            session.Reveal(0);

            Assert.That(session.Current[0].TopRunLength, Is.EqualTo(3));
            Assert.That(session.TryPour(1, 0, out PourResult result), Is.True);
            Assert.That(result.TargetSealed, Is.True);
        }

        [Test]
        public void Regression3_0451_HiddenMonocolorVisibleToEmpty_KeepsTheSessionPlaying()
        {
            // 04 §5.1, regresión (3): [2*, 1, 1, 1] → vacía no es fútil (verter revela información). Es el único vertido útil:
            // sin la excepción la partida empezaría en Aguas estancadas.
            var initial = new BoardState(new[] { Murky(4, 0x1, 2, 1, 1, 1), C(4, 2, 2, 2), Cuia.Empty(4) });
            Assert.That(PourRules.IsFutile(initial, 1, 2), Is.True);
            Assert.That(PourRules.IsFutile(initial, 0, 2), Is.False);

            var session = new LevelSession(initial, true, PourMode.Run);

            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(session.TryPour(0, 2, out PourResult result), Is.True);
            Assert.That(result.Pour.Amount, Is.EqualTo(3));
            Assert.That(result.RevealedMask, Is.EqualTo(1 << 0));
        }

        [Test]
        public void HiddenDosesWithARemolino_AreRejected()
        {
            // [P]: el Remolino inserta por el fondo y desplazaría las capas ocultas; la validación ya prohíbe la combinación.
            var initial = new BoardState(new[]
            {
                Murky(4, 0x1, 2, 1), C(4, 1, 1, 1), C(4, 2, 2, 2),
                Cuia.Empty(4).WithMechanic(MechanicType.Remolino, pairId: 0), Cuia.Empty(4).WithMechanic(MechanicType.Remolino, pairId: 0),
            });

            Assert.That(() => { new LevelSession(initial, true, PourMode.Run); }, Throws.TypeOf<NotSupportedException>());
        }
    }
}
