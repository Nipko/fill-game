#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// Partida del núcleo: invariantes I1-I4 de docs/dev/prototipo-v0.md (a), victoria (02 R12), bloqueo (R13, R15),
    /// Marea (04 §2.1) y Volver a la orilla (04 §2.7). Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class LevelSessionTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        /// <summary>Nivel 1 de 02 §13: [açaí ×3, aguaje], [aguaje ×3, açaí], vacía (índices de motor del ejemplo de v0).</summary>
        private static BoardState TutorialLevel1()
        {
            return new BoardState(new[] { C(4, 8, 8, 8, 4), C(4, 4, 4, 4, 8), Cuia.Empty(4) });
        }

        /// <summary>Tablero que entra en Aguas estancadas con el vertido 0 → 3 (2 dosis de 2 a la única vacía).</summary>
        private static BoardState OnePourFromStuck()
        {
            return new BoardState(new[] { C(4, 2, 3, 2, 2), C(4, 3, 3, 3, 1), C(4, 2, 1, 1, 1), Cuia.Empty(4) });
        }

        [Test]
        public void NewSession_StartsPlaying_WithEmptyHistory()
        {
            BoardState initial = TutorialLevel1();

            var session = new LevelSession(initial, true);

            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(session.Initial, Is.SameAs(initial));
            Assert.That(session.Current, Is.SameAs(initial));
            Assert.That(session.AppliedPours, Is.EqualTo(0));
            Assert.That(session.CanUndo, Is.False);
            Assert.That(session.FutileRuleEnabled, Is.True);
        }

        [Test]
        public void TryPour_Legal_AppliesAndRecordsThePour()
        {
            var session = new LevelSession(TutorialLevel1(), true);

            bool ok = session.TryPour(0, 2, out PourResult result);

            Assert.That(ok, Is.True);
            Assert.That(result.Pour, Is.EqualTo(new Pour(0, 2, 1)));
            Assert.That(result.TargetSealed, Is.False);
            Assert.That(result.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(session.Current[2], Is.EqualTo(C(4, 4)));
            Assert.That(session.AppliedPours, Is.EqualTo(1));
            Assert.That(session.CanUndo, Is.True);
            Assert.That(session.GetAppliedPour(0), Is.EqualTo(new Pour(0, 2, 1)));
        }

        [Test]
        public void I2_IllegalPour_ReturnsFalseAndChangesNothing()
        {
            // R11: un vertido ilegal no consume movimiento.
            var session = new LevelSession(TutorialLevel1(), true);
            BoardState before = session.Current;

            bool ok = session.TryPour(0, 1, out PourResult result);

            Assert.That(ok, Is.False);
            Assert.That(result.Pour, Is.EqualTo(default(Pour)));
            Assert.That(session.Current, Is.SameAs(before));
            Assert.That(session.AppliedPours, Is.EqualTo(0));
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
        }

        [Test]
        public void TryPour_IndexOutOfBoard_Throws()
        {
            var session = new LevelSession(TutorialLevel1(), true);

            Assert.That(() => { session.TryPour(0, 3, out _); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void R12_TutorialLevel1_IsWonInThreePours_AndSealsAreReported()
        {
            var session = new LevelSession(TutorialLevel1(), true);

            Assert.That(session.TryPour(0, 2, out PourResult first), Is.True);
            Assert.That(session.TryPour(1, 0, out PourResult second), Is.True);
            Assert.That(session.TryPour(1, 2, out PourResult third), Is.True);

            Assert.That(first.TargetSealed, Is.False);
            Assert.That(second.TargetSealed, Is.True);
            Assert.That(second.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(third.TargetSealed, Is.True);
            Assert.That(third.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(session.AppliedPours, Is.EqualTo(3));
        }

        [Test]
        public void I2_PourAfterVictory_ReturnsFalse()
        {
            var session = new LevelSession(TutorialLevel1(), true);
            session.TryPour(0, 2, out _);
            session.TryPour(1, 0, out _);
            session.TryPour(1, 2, out _);
            BoardState won = session.Current;

            bool ok = session.TryPour(2, 1, out _);

            Assert.That(ok, Is.False);
            Assert.That(session.Current, Is.SameAs(won));
            Assert.That(session.AppliedPours, Is.EqualTo(3));
        }

        [Test]
        public void I1_PourIntoTheLastEmpty_EndsInStuck()
        {
            var session = new LevelSession(OnePourFromStuck(), true);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));

            Assert.That(session.TryPour(0, 3, out PourResult result), Is.True);

            Assert.That(result.Pour, Is.EqualTo(new Pour(0, 3, 2)));
            Assert.That(result.Status, Is.EqualTo(LevelStatus.Stuck));
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Stuck));
        }

        [Test]
        public void I2_PourWhileStuck_ReturnsFalse_EvenIfLegal()
        {
            // R15: el input queda bloqueado desde el instante lógico del bloqueo. Tablero de "solo fútiles" (R13):
            // el vertido 0 → 1 es legal, pero la partida ya está en Aguas estancadas.
            var session = new LevelSession(new BoardState(new[] { C(5, 1, 1, 1, 1), Cuia.Empty(5), C(4, 2, 2, 2, 2) }), true);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Stuck));
            Assert.That(PourRules.IsLegal(session.Current, 0, 1), Is.True);

            bool ok = session.TryPour(0, 1, out _);

            Assert.That(ok, Is.False);
            Assert.That(session.AppliedPours, Is.EqualTo(0));
        }

        [Test]
        public void KillSwitchOff_SameBoard_IsPlaying()
        {
            // R16: con futile_rule_enabled = false solo cuenta "sin vertidos legales".
            var session = new LevelSession(new BoardState(new[] { C(5, 1, 1, 1, 1), Cuia.Empty(5), C(4, 2, 2, 2, 2) }), false);

            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(session.FutileRuleEnabled, Is.False);
        }

        [Test]
        public void FutilePour_IsAcceptedWhilePlaying()
        {
            // R13: un vertido fútil sigue siendo legal; el jugador puede hacerlo.
            var session = new LevelSession(new BoardState(new[] { C(4, 1, 1, 2, 2), C(4, 2, 2), C(4, 1, 1), Cuia.Empty(4) }), true);
            Assert.That(PourRules.IsFutile(session.Current, 1, 3), Is.True);

            bool ok = session.TryPour(1, 3, out PourResult result);

            Assert.That(ok, Is.True);
            Assert.That(result.Pour, Is.EqualTo(new Pour(1, 3, 2)));
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
        }

        [Test]
        public void I3_Undo_RestoresTheExactPreviousState_AndReopensTheSeal()
        {
            var session = new LevelSession(TutorialLevel1(), true);
            session.TryPour(0, 2, out _);
            BoardState beforeSeal = session.Current;
            session.TryPour(1, 0, out PourResult sealing);
            Assert.That(sealing.TargetSealed, Is.True);

            bool undone = session.Undo();

            Assert.That(undone, Is.True);
            Assert.That(session.Current, Is.SameAs(beforeSeal));
            Assert.That(session.Current.IsSealed(0), Is.False);
            Assert.That(session.AppliedPours, Is.EqualTo(1));
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
        }

        [Test]
        public void I3_UndoChain_EmptiesTheHistory_ThenReturnsFalse()
        {
            // 04 §2.1: se puede encadenar hasta vaciar el historial.
            var session = new LevelSession(TutorialLevel1(), true);
            session.TryPour(0, 2, out _);
            session.TryPour(1, 0, out _);

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Current.Equals(session.Initial), Is.True);
            Assert.That(session.CanUndo, Is.False);
            Assert.That(session.Undo(), Is.False);
            Assert.That(session.AppliedPours, Is.EqualTo(0));
        }

        [Test]
        public void Marea_FromStuck_ReturnsToPlaying()
        {
            // R15 y 04 §2.1: Marea desde Aguas estancadas restaura el estado y la partida sigue.
            var session = new LevelSession(OnePourFromStuck(), true);
            session.TryPour(0, 3, out _);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Stuck));

            Assert.That(session.Undo(), Is.True);

            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(session.Current.Equals(session.Initial), Is.True);
            Assert.That(session.TryPour(1, 3, out _), Is.True);
        }

        [Test]
        public void Marea_FromVictory_ReturnsToPlaying()
        {
            var session = new LevelSession(TutorialLevel1(), true);
            session.TryPour(0, 2, out _);
            session.TryPour(1, 0, out _);
            session.TryPour(1, 2, out _);

            Assert.That(session.Undo(), Is.True);

            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(session.AppliedPours, Is.EqualTo(2));
        }

        [Test]
        public void I4_Restart_GoesBackToTheInitialBoard_WithEmptyHistory()
        {
            var session = new LevelSession(OnePourFromStuck(), true);
            session.TryPour(0, 3, out _);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Stuck));

            session.Restart();

            Assert.That(session.Current.Equals(session.Initial), Is.True);
            Assert.That(session.AppliedPours, Is.EqualTo(0));
            Assert.That(session.CanUndo, Is.False);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
        }

        [Test]
        public void R8_ReplayingTheHistoryFromInitial_RebuildsCurrent()
        {
            // 02 R8: la lista de movimientos sirve para Marea y para reconstruir el estado.
            var session = new LevelSession(TutorialLevel1(), true);
            session.TryPour(0, 2, out _);
            session.TryPour(1, 0, out _);

            BoardState rebuilt = session.Initial;
            for (int i = 0; i < session.AppliedPours; i++)
            {
                Pour pour = session.GetAppliedPour(i);
                Assert.That(PourRules.Amount(rebuilt, pour.Source, pour.Target), Is.EqualTo(pour.Amount));
                rebuilt = PourRules.Apply(rebuilt, pour.Source, pour.Target);
            }

            Assert.That(rebuilt.Equals(session.Current), Is.True);
            Assert.That(() => { session.GetAppliedPour(2); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void HiddenDoses_AreRejected()
        {
            // Aguas turbias queda fuera de v0: Marea necesitaría la máscara de revelado (R7, 04 §2.1).
            var board = new BoardState(new[] { Cuia.Create(4, new[] { 1, 2, 1 }, 0x1), C(4, 2, 2, 1, 1), Cuia.Empty(4) });

            Assert.That(() => { new LevelSession(board, true); }, Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void InitialBoardAlreadyStuck_StartsStuck()
        {
            var session = new LevelSession(new BoardState(new[] { C(4, 1, 2, 1, 2), C(4, 2, 1, 2, 1) }), true);

            Assert.That(session.Status, Is.EqualTo(LevelStatus.Stuck));
            Assert.That(session.CanUndo, Is.False);
        }
    }
}
