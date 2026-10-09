#nullable enable
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Solver.Tests
{
    /// <summary>
    /// <c>MOVIMIENTOS(S)</c> de 03 §5 (<see cref="SolverMoves"/>): vertidos útiles con la cantidad del modo, vacíos
    /// equivalentes una sola vez y fútiles marcados con el kill-switch apagado. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class SolverMovesTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static int Collect(Cuia[] cuias, PourMode mode, bool futileRuleEnabled, SolverMove[] buffer)
        {
            int[] units = new BoardState(cuias).UnitsTable;
            return SolverMoves.Collect(cuias, 0, cuias.Length, units, mode, futileRuleEnabled, buffer);
        }

        [Test]
        public void EquivalentEmpties_AreExpandedOnce()
        {
            // 03 §5: un solo destino vacío por (capacidad, attr); el orden canónico pone juntas las vacías.
            var cuias = new[] { Cuia.Empty(4), Cuia.Empty(4), C(4, 1, 2, 2), C(4, 2, 1, 1) };
            BoardCanon.Sort(cuias, 0, cuias.Length);
            var buffer = new SolverMove[SolverMoves.MaxMoves];

            int count = Collect(cuias, PourMode.Run, true, buffer);

            int toEmpty = 0;
            for (int m = 0; m < count; m++)
            {
                if (cuias[buffer[m].Target].IsEmpty)
                {
                    toEmpty++;
                }
            }

            Assert.That(toEmpty, Is.EqualTo(2), "una vacía por origen, no dos");
        }

        [Test]
        public void Amount_FollowsThePourMode()
        {
            // R9: la racha que cabe en run, una dosis en single; los pares (A, B) son los mismos.
            var cuias = new[] { C(4, 3, 1, 1, 1), C(4, 1), Cuia.Empty(4) };
            var run = new SolverMove[SolverMoves.MaxMoves];
            var single = new SolverMove[SolverMoves.MaxMoves];

            int runCount = Collect(cuias, PourMode.Run, true, run);
            int singleCount = Collect(cuias, PourMode.Single, true, single);

            Assert.That(runCount, Is.EqualTo(singleCount));
            for (int m = 0; m < runCount; m++)
            {
                Assert.That(single[m].Source, Is.EqualTo(run[m].Source));
                Assert.That(single[m].Target, Is.EqualTo(run[m].Target));
                Assert.That(single[m].Amount, Is.EqualTo(1));
                Assert.That(run[m].Amount, Is.EqualTo(BoardRules.Amount(cuias, 0, cuias.Length, new BoardState(cuias).UnitsTable, run[m].Source, run[m].Target, PourMode.Run)));
            }

            Assert.That(run[0].ToPour(), Is.EqualTo(new Pour(0, 1, 3)));
        }

        [Test]
        public void KillSwitchOff_ListsFutilePours_Marked()
        {
            // 02 R16 y Q18 B: con futile_rule_enabled = false salen también los fútiles, marcados para no avanzar contadores.
            var cuias = new[] { C(4, 1, 1), C(4, 2, 2, 1, 1), Cuia.Empty(4), C(4, 2, 2) };
            var useful = new SolverMove[SolverMoves.MaxMoves];
            var all = new SolverMove[SolverMoves.MaxMoves];

            int usefulCount = Collect(cuias, PourMode.Run, true, useful);
            int allCount = Collect(cuias, PourMode.Run, false, all);

            int futiles = 0;
            for (int m = 0; m < allCount; m++)
            {
                futiles += all[m].IsFutile ? 1 : 0;
            }

            for (int m = 0; m < usefulCount; m++)
            {
                Assert.That(useful[m].IsFutile, Is.False);
            }

            Assert.That(futiles, Is.GreaterThan(0));
            Assert.That(allCount, Is.EqualTo(usefulCount + futiles));
        }

        [Test]
        public void UsefulPairs_AreTheOnesOfPourRules_OnABoardWithoutTwins()
        {
            // Sin cuias idénticas no hay nada que deduplicar: los mismos pares que movimientos_utiles de 02 §1.4.
            BoardState state = new BoardState(new[] { C(4, 1, 2, 3), C(4, 3, 3), C(4, 2, 1), Cuia.Empty(4), C(2, 1) });
            var pours = new Pour[PourRules.MaxPours];
            var moves = new SolverMove[SolverMoves.MaxMoves];

            int expected = PourRules.CollectUsefulMoves(state, true, PourMode.Run, pours);
            var cuias = new Cuia[state.Count];
            for (int i = 0; i < cuias.Length; i++)
            {
                cuias[i] = state[i];
            }

            int count = SolverMoves.Collect(cuias, 0, cuias.Length, state.UnitsTable, PourMode.Run, true, moves);

            Assert.That(count, Is.EqualTo(expected));
            for (int m = 0; m < count; m++)
            {
                Assert.That(moves[m].ToPour(), Is.EqualTo(pours[m]));
            }
        }
    }
}
