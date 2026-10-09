#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Solver.Bot;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Solver.Tests
{
    /// <summary>
    /// Bot casual ×40 (03 §6) y trampas tempranas (Q16 B de D-007): reproducible por semilla (08 §14.2), puntuación entera
    /// de <c>PUNTUACION_CASUAL</c> ×4 y casos límite. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class CasualBotTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static BoardState Medium()
        {
            return new BoardState(new[]
            {
                C(4, 1, 2, 3, 1), C(4, 2, 3, 1, 2), C(4, 3, 1, 2, 3), C(4, 4, 4, 5, 5), C(4, 5, 5, 4, 4), Cuia.Empty(4),
            });
        }

        [Test]
        public void SameSeed_SameFailures()
        {
            // 08 §14.2: bot casual reproducible por semilla.
            ulong seed = CasualBot.SeedFor("amazonfill:v1:m03:v1:n0046");
            var bot = new CasualBot();

            BotResult first = bot.FailRate(Medium(), seed, PourMode.Run);
            BotResult again = bot.FailRate(Medium(), seed, PourMode.Run);
            BotResult fresh = new CasualBot().FailRate(Medium(), seed, PourMode.Run);

            Assert.That(first.Runs, Is.EqualTo(BotOptions.DefaultRuns));
            Assert.That(again.Failures, Is.EqualTo(first.Failures));
            Assert.That(fresh.Failures, Is.EqualTo(first.Failures));
            Assert.That(first.Rate, Is.EqualTo((double)first.Failures / first.Runs));
        }

        [Test]
        public void WonBoard_NeverFails_StuckBoard_AlwaysFails()
        {
            // 03 §6: si victoria(S) → ÉXITO; si M = ∅ → FALLO (Aguas estancadas).
            var won = new BoardState(new[] { C(4, 1, 1, 1, 1), Cuia.Empty(4) });
            var stuck = new BoardState(new[] { C(4, 1, 2, 1, 2), C(4, 2, 1, 2, 1) });
            var bot = new CasualBot();

            Assert.That(bot.FailRate(won, 1UL, PourMode.Run).Failures, Is.EqualTo(0));
            Assert.That(bot.FailRate(stuck, 1UL, PourMode.Run).Failures, Is.EqualTo(BotOptions.DefaultRuns));
        }

        [Test]
        public void EveryUsefulPourWins_SoTheBotNeverFails()
        {
            var board = new BoardState(new[] { C(4, 1, 1, 1), C(4, 1), Cuia.Empty(4) });

            Assert.That(new CasualBot().FailRate(board, 7UL, PourMode.Run).Failures, Is.EqualTo(0));
            Assert.That(new CasualBot().FailRate(board, 7UL, PourMode.Single).Failures, Is.EqualTo(0));
        }

        [Test]
        public void MaxSteps_CountsAsFailure()
        {
            // 03 §6: agotar el tope de vertidos es FALLO.
            var options = new BotOptions(5, 1, 85, PourMode.Run, true);

            Assert.That(new CasualBot().FailRate(Medium(), 3UL, options).Failures, Is.EqualTo(5));
        }

        [TestCase(0, 1, (4 * 3) + 16 + 40 + 8)] // junta, sella (llena y monocolor, 02 R5) y deja expuesto el 2 con destino
        [TestCase(0, 2, (4 * 3) - 20 + 8)]      // gasta un vacío
        [TestCase(3, 1, (4 * 1) + 16 + 8)]      // junta sin sellar y deja expuesto el 3
        [TestCase(4, 2, (4 * 2) - 20 + 8)]      // Cuia alta: la racha entera a la vacía
        public void Score_IsPuntuacionCasualTimesFour(int source, int target, int expected)
        {
            // 03 §6 PUNTUACION_CASUAL ×4: n, +4 juntar, +10 sellar, −5 vacío, −3 parcial, +2 color expuesto con destino
            // (sobre el estado antes del vertido, como el prototipo: la propia vacía de destino cuenta como destino).
            var cuias = new[] { C(4, 2, 1, 1, 1), C(4, 1), Cuia.Empty(4), C(4, 3, 1), C(6, 2, 2, 3, 3) };
            int[] units = new BoardState(cuias).UnitsTable;
            var board = new BoardSlice(cuias, 0, cuias.Length, units);
            int amount = BoardRules.Amount(cuias, 0, cuias.Length, units, source, target, PourMode.Run);
            var move = new SolverMove(source, target, amount, false);

            Assert.That(BoardRules.IsLegal(cuias, 0, cuias.Length, units, source, target), Is.True);
            Assert.That(CasualBot.Score(in board, move), Is.EqualTo(expected));
        }

        [Test]
        public void Score_PenalisesAPartialPour()
        {
            // −3 ×4 si n < racha(i).
            var cuias = new[] { C(4, 1, 1, 1), C(4, 2, 2, 1), Cuia.Empty(4), C(4, 2, 2) };
            int[] units = new BoardState(cuias).UnitsTable;
            var board = new BoardSlice(cuias, 0, cuias.Length, units);
            var move = new SolverMove(0, 1, 1, false);

            Assert.That(CasualBot.Score(in board, move), Is.EqualTo(4 + 16 - 12));
        }

        /// <summary>
        /// Vierte la racha de dos dosis del 1 de <c>C(4, 1, 1)</c> en el Remolino <c>R(4, 3)</c>: entra por el fondo de la
        /// pareja <paramref name="partnerColors"/> (02 §9.1 M4). <paramref name="extra"/> ajusta <c>U_c</c>.
        /// </summary>
        private static int ScoreThroughRemolino(int[] partnerColors, Cuia extra)
        {
            var cuias = new[]
            {
                C(4, 1, 1),
                C(4, 3).WithMechanic(MechanicType.Remolino, pairId: 0),
                Cuia.Create(4, partnerColors).WithMechanic(MechanicType.Remolino, pairId: 0),
                extra,
            };
            int[] units = new BoardState(cuias).UnitsTable;
            var board = new BoardSlice(cuias, 0, cuias.Length, units);
            int amount = BoardRules.Amount(cuias, 0, cuias.Length, units, 0, 1, PourMode.Run);

            Assert.That(BoardRules.IsLegal(cuias, 0, cuias.Length, units, 0, 1), Is.True);
            Assert.That(BoardRules.ReceiverOf(in board, 1), Is.EqualTo(2));
            Assert.That(amount, Is.EqualTo(2));
            return CasualBot.Score(in board, new SolverMove(0, 1, amount, false));
        }

        [Test]
        public void Score_Remolino_MixingIntoThePartner_DoesNotCountAsJoiningOrSealing()
        {
            // 02 §9.1 M4: la racha entra por el fondo y de cualquier color. Con la pareja de otro color no junta (+16) y,
            // aunque quede llena y con un color debajo, no sella (+40): solo 4·n.
            Assert.That(ScoreThroughRemolino(new[] { 2 }, Cuia.Empty(4)), Is.EqualTo(4 * 2));
            Assert.That(ScoreThroughRemolino(new[] { 2, 2 }, Cuia.Empty(4)), Is.EqualTo(4 * 2));
            Assert.That(ScoreThroughRemolino(new[] { 1, 2 }, Cuia.Empty(4)), Is.EqualTo(4 * 2));
        }

        [Test]
        public void Score_Remolino_JoinsOnlyAMonocolorPartnerOfThePouredColor_AndSealsOnlyWithCapEqualToUnits()
        {
            // Pareja monocolor del 1: junta (+16). Llena con cap = U_1 = 4: sella (+40, 02 R5).
            Assert.That(ScoreThroughRemolino(new[] { 1, 1 }, Cuia.Empty(4)), Is.EqualTo((4 * 2) + 16 + 40));

            // Llena y monocolor, pero U_1 = 5 ≠ cap: no sella.
            Assert.That(ScoreThroughRemolino(new[] { 1, 1 }, C(4, 3, 1)), Is.EqualTo((4 * 2) + 16));

            // Monocolor del 1 sin llenarse: junta sin sellar.
            Assert.That(ScoreThroughRemolino(new[] { 1 }, Cuia.Empty(4)), Is.EqualTo((4 * 2) + 16));
        }

        [Test]
        public void Score_Remolino_EmptyPartner_CostsAnEmpty()
        {
            // −5 ×4: la pareja vacía es un vacío gastado, como en una cuia normal.
            Assert.That(ScoreThroughRemolino(new int[0], Cuia.Empty(4)), Is.EqualTo((4 * 2) - 20));
        }

        [Test]
        public void EarlyTraps_CountsFirstPoursThatGetStuckWithinTwo()
        {
            // Q16 B: el vertido 0 → 3 deja el tablero atascado en el acto (es la única jugada que lo hace).
            var board = new BoardState(new[] { C(4, 2, 3, 2, 2), C(4, 3, 3, 3, 1), C(4, 2, 1, 1, 1), Cuia.Empty(4) });
            var traps = new EarlyTraps();

            Assert.That(traps.Count(board, PourMode.Run), Is.GreaterThanOrEqualTo(1));
            Assert.That(traps.CountUnavoidable(board, PourMode.Run), Is.GreaterThanOrEqualTo(1));
            Assert.That(traps.Count(new BoardState(new[] { C(4, 1, 1, 1), C(4, 1), Cuia.Empty(4) }), PourMode.Run), Is.EqualTo(0));
        }

        [Test]
        public void Options_AreValidated()
        {
            Assert.That(() => new BotOptions(0, 400, 85, PourMode.Run, true), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new BotOptions(40, 400, 101, PourMode.Run, true), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new CasualBot().FailRate(Medium(), 1UL, default(BotOptions)), Throws.TypeOf<ArgumentException>());
        }
    }
}
