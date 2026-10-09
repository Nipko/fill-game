#nullable enable
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// R10 por etapas en el orden del anexo B de la propuesta de D-007 (Q13): (a) revelado de A, (b) aperturas, (c) sellado,
    /// (d) liberaciones, (e) contadores, (f) condiciones, (f′) punto fijo, (g) victoria y (h) bloqueo, con
    /// <see cref="BoardRules"/> como única implementación que aplica vertidos. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class R10OrderTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        /// <summary>A = [5*, 1] (5 oculta), B = [1, 1, 1], C = [5, 5, 5]; U_1 = U_5 = 4.</summary>
        private static Cuia[] RevealAndSealBoard()
        {
            return new[] { Cuia.Create(4, new[] { 5, 1 }, 0x1), C(4, 1, 1, 1), C(4, 5, 5, 5) };
        }

        private static int[] Units(Cuia[] cuias)
        {
            return new BoardState(cuias).UnitsTable;
        }

        [Test]
        public void R10a_RevealOfTheSource_IsInTheStateThatSealingEvaluates()
        {
            // Anexo B (a) antes de (c): el vertido A→B destapa la dosis oculta de A y sella B en el mismo vertido; el
            // sellado (c) se evalúa ya sobre el tablero revelado (R7). En el juego base A no puede sellar en el vertido que
            // la destapa (pierde dosis); con mecánicas lo cubre el punto fijo (f′).
            Cuia[] cuias = RevealAndSealBoard();
            int[] units = Units(cuias);
            var events = default(PourEvents);

            BoardRules.ApplyInPlace(cuias, 0, cuias.Length, units, 0, 1, PourMode.Run, false, ref events);

            Assert.That(events.SourceRevealed, Is.True);
            Assert.That(cuias[0], Is.EqualTo(C(4, 5)));
            Assert.That(cuias[0].HasHidden, Is.False);
            Assert.That(events.WasSealed(1), Is.True);
            Assert.That(events.WasSealed(0), Is.False);
            Assert.That(events.SealedMask, Is.EqualTo(1 << 1));
            Assert.That(events.Amount, Is.EqualTo(1));
            Assert.That(events.Color, Is.EqualTo(1));
            Assert.That(events.Receiver, Is.EqualTo(1));
            Assert.That(events.ChangedMask, Is.EqualTo((1 << 0) | (1 << 1)));
            Assert.That(events.StatusEvaluated, Is.False);
        }

        [Test]
        public void R10g_VictoryIsEvaluatedBeforeBlockage()
        {
            // Anexo B (g) antes de (h): tras el vertido que gana no queda ningún vertido útil, pero es victoria, no bloqueo.
            Cuia[] cuias = RevealAndSealBoard();
            int[] units = Units(cuias);
            var first = default(PourEvents);
            BoardRules.ApplyInPlace(cuias, 0, cuias.Length, units, 0, 1, PourMode.Run, false, true, ref first);
            Assert.That(first.StatusEvaluated, Is.True);
            Assert.That(first.Status, Is.EqualTo(LevelStatus.Playing));

            var second = default(PourEvents);
            BoardRules.ApplyInPlace(cuias, 0, cuias.Length, units, 0, 2, PourMode.Run, false, true, ref second);

            Assert.That(BoardRules.HasUsefulMove(cuias, 0, cuias.Length, units, true), Is.False);
            Assert.That(second.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(BoardRules.EvaluateStatus(cuias, 0, cuias.Length, units, true), Is.EqualTo(LevelStatus.Won));
            Assert.That(BoardRules.IsStuck(cuias, 0, cuias.Length, units, true), Is.False);
        }

        [Test]
        public void R10h_BlockageIsReportedWhenNoUsefulPourRemains()
        {
            // R13: el vertido 0 → 3 lleva 2 dosis a la única vacía y deja el tablero sin vertidos útiles.
            var cuias = new[] { C(4, 2, 3, 2, 2), C(4, 3, 3, 3, 1), C(4, 2, 1, 1, 1), Cuia.Empty(4) };
            int[] units = Units(cuias);
            var events = default(PourEvents);

            BoardRules.ApplyInPlace(cuias, 0, cuias.Length, units, 0, 3, PourMode.Run, false, true, ref events);

            Assert.That(events.Status, Is.EqualTo(LevelStatus.Stuck));
            Assert.That(events.Amount, Is.EqualTo(2));
        }

        [Test]
        public void LevelSession_ReportsTheStagesOfR10_AndTheStatusInTheEvents()
        {
            var plain = new LevelSession(new BoardState(new[] { C(4, 1), C(4, 1, 1, 1), C(4, 5, 5, 5, 5) }), true);

            Assert.That(plain.TryPour(0, 1, out PourResult result), Is.True);
            Assert.That(result.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(result.TargetSealed, Is.True);
            Assert.That(result.Events.WasSealed(1), Is.True);
            Assert.That(result.Events.StatusEvaluated, Is.True);
            Assert.That(result.Events.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(result.Events.Amount, Is.EqualTo(result.Pour.Amount));
        }

        [Test]
        public void PlainBoard_RunsNoMechanicStage()
        {
            // Sin mecánicas no hay (b), (d), (e) ni (f): nada se abre, se suelta, avanza ni repite el punto fijo.
            var cuias = new[] { C(4, 1, 2, 2), C(4, 2), Cuia.Empty(4), C(4, 1, 1, 1) };
            int[] units = Units(cuias);
            var events = default(PourEvents);

            BoardRules.ApplyInPlace(cuias, 0, cuias.Length, units, 0, 1, PourMode.Run, false, ref events);

            Assert.That(cuias[1], Is.EqualTo(C(4, 2, 2, 2)));
            Assert.That(events.OpenedMask, Is.EqualTo(0));
            Assert.That(events.ReleasedMask, Is.EqualTo(0));
            Assert.That(events.CountersAdvanced, Is.False);
            Assert.That(events.FixedPointPasses, Is.EqualTo(0));
        }

        [Test]
        public void FutilePour_IsRecorded_AndAdvancesNoCounter()
        {
            // Q18 B: el vertido fútil se aplica (es legal, R13) pero no avanza contadores.
            var cuias = new[] { C(4, 1, 1), C(4, 2, 2, 1, 1), Cuia.Empty(4), C(4, 2, 2) };
            int[] units = Units(cuias);
            Assert.That(BoardRules.IsFutile(cuias, 0, cuias.Length, units, 0, 2), Is.True);
            var events = default(PourEvents);

            BoardRules.ApplyInPlace(cuias, 0, cuias.Length, units, 0, 2, PourMode.Run, true, ref events);

            Assert.That(events.Futile, Is.True);
            Assert.That(events.CountersAdvanced, Is.False);
            Assert.That(cuias[2], Is.EqualTo(C(4, 1, 1)));
        }

        [Test]
        public void ApplyInPlace_UsesRelativeIndices_InsideTheSlice()
        {
            // El solver guarda sus nodos en un array plano: los índices son relativos a offset y no se toca nada fuera.
            Cuia guard = C(4, 7, 7);
            var cuias = new[] { guard, C(4, 1, 2, 2), C(4, 2), Cuia.Empty(4), guard };
            int[] units = new BoardState(new[] { C(4, 1, 2, 2), C(4, 2), Cuia.Empty(4) }).UnitsTable;
            var events = default(PourEvents);

            Assert.That(BoardRules.IsLegal(cuias, 1, 3, units, 0, 1), Is.True);
            Assert.That(BoardRules.Amount(cuias, 1, 3, units, 0, 1, PourMode.Run), Is.EqualTo(2));
            BoardRules.ApplyInPlace(cuias, 1, 3, units, 0, 1, PourMode.Run, false, ref events);

            Assert.That(cuias[1], Is.EqualTo(C(4, 1)));
            Assert.That(cuias[2], Is.EqualTo(C(4, 2, 2, 2)));
            Assert.That(cuias[0], Is.EqualTo(guard));
            Assert.That(cuias[4], Is.EqualTo(guard));
            Assert.That(events.Source, Is.EqualTo(0));
            Assert.That(events.Target, Is.EqualTo(1));
        }

        [Test]
        public void SingleMode_MovesOneDose_AndRevealsWhenTheLastDoseOfTheRunLeaves()
        {
            // R10 (a) en single: la nueva cima se revela cuando sale la última dosis de la racha (02 R10).
            var cuias = new[] { Cuia.Create(4, new[] { 3, 1, 1 }, 0x1), Cuia.Empty(4), C(4, 3, 3, 3), C(4, 1, 1) };
            int[] units = Units(cuias);
            var first = default(PourEvents);
            BoardRules.ApplyInPlace(cuias, 0, cuias.Length, units, 0, 1, PourMode.Single, false, ref first);
            var second = default(PourEvents);
            BoardRules.ApplyInPlace(cuias, 0, cuias.Length, units, 0, 1, PourMode.Single, false, ref second);

            Assert.That(first.Amount, Is.EqualTo(1));
            Assert.That(first.SourceRevealed, Is.False);
            Assert.That(second.SourceRevealed, Is.True);
            Assert.That(cuias[0], Is.EqualTo(C(4, 3)));
        }
    }
}
