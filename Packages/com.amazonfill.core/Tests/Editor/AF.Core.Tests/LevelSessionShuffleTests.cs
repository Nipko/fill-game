#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Session;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// <see cref="LevelSession.ApplyShuffle"/> (Pororoca, 04 §2.4; 02 §14 «Vacía el historial de Marea (nuevo tablero); conserva
    /// stock»; 02 T6): la ola revela todo, vacía el historial, recalcula el estado y Volver a la orilla vuelve al tablero del
    /// nivel [P]. El barajado en sí es de AF.Solver (<c>ShufflerTests</c>). Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class LevelSessionShuffleTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static BoardState Start()
        {
            return new BoardState(new[]
            {
                Cuia.Create(4, new[] { 1, 2, 1, 2 }, 0b0011), C(4, 2, 1, 2, 1), Cuia.Empty(4), Cuia.Empty(4),
            });
        }

        // Un barajado posible de Start() revelado: mismas cuias y colores, otro reparto.
        private static BoardState Shuffled()
        {
            return new BoardState(new[] { C(4, 2, 2, 1, 1), C(4, 1, 1, 2, 2), Cuia.Empty(4), Cuia.Empty(4) });
        }

        [Test]
        public void ApplyShuffle_EmptiesTheUndoHistory_02_14()
        {
            var session = new LevelSession(Start(), true);
            Assert.That(session.TryPour(1, 2, out _), Is.True);
            Assert.That(session.CanUndo, Is.True);

            Assert.That(session.ApplyShuffle(Shuffled()), Is.True);

            Assert.That(session.CanUndo, Is.False);
            Assert.That(session.AppliedPours, Is.EqualTo(0));
            Assert.That(session.Undo(), Is.False);
            Assert.That(session.Current.Equals(Shuffled()), Is.True);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
        }

        [Test]
        public void ApplyShuffle_RevealsEverything_AndRestartKeepsItRevealed_04_2_4_a_I5()
        {
            var session = new LevelSession(Start(), true);
            Assert.That(session.HiddenCount, Is.EqualTo(2));

            session.ApplyShuffle(Shuffled());

            Assert.That(session.HiddenCount, Is.EqualTo(0));
            Assert.That(session.RevealedLayers(0), Is.EqualTo(0b0011));
            session.Restart();
            Assert.That(session.Current.Equals(session.Initial), Is.False, "el inicial con lo revelado visible");
            Assert.That(session.Current[0].HiddenMask, Is.EqualTo(0));
            Assert.That(session.Current[0].ColorAt(0), Is.EqualTo(1));
            Assert.That(session.Current[1].Packed, Is.EqualTo(Start()[1].Packed), "Volver a la orilla vuelve al tablero del nivel [P]");
        }

        [Test]
        public void ApplyShuffle_OutsidePlaying_ChangesNothing_04_2_4()
        {
            // Ganado: toda cuia vacía o sellada.
            var won = new LevelSession(new BoardState(new[] { C(4, 1, 1, 1, 1), Cuia.Empty(4) }), true);
            Assert.That(won.Status, Is.EqualTo(LevelStatus.Won));

            Assert.That(won.ApplyShuffle(new BoardState(new[] { C(4, 1, 1, 1, 1), Cuia.Empty(4) })), Is.False);
            Assert.That(won.Status, Is.EqualTo(LevelStatus.Won));
        }

        [Test]
        public void ApplyShuffle_RejectsABoardThatIsNotAShuffleOfThisOne_04_2_4()
        {
            var session = new LevelSession(Start(), true);

            Assert.That(() => session.ApplyShuffle(new BoardState(new[] { C(4, 2, 2, 1, 1), C(4, 1, 1, 2, 2), Cuia.Empty(4) })),
                Throws.TypeOf<ArgumentException>(), "otro número de cuias");
            Assert.That(() => session.ApplyShuffle(new BoardState(new[] { C(4, 2, 2, 1, 1), C(4, 1, 1, 2, 2), Cuia.Empty(4), Cuia.Empty(5) })),
                Throws.TypeOf<ArgumentException>(), "otra capacidad");
            Assert.That(() => session.ApplyShuffle(new BoardState(new[] { C(4, 2, 2, 1, 1), C(4, 1, 1, 2, 3), Cuia.Empty(4), Cuia.Empty(4) })),
                Throws.TypeOf<ArgumentException>(), "otros colores");
            Assert.That(() => session.ApplyShuffle(Start()), Throws.TypeOf<ArgumentException>(), "con ocultas");
            Assert.That(session.Current.Equals(Start()), Is.True);
        }
    }
}
