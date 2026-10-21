#nullable enable
using System.Diagnostics;
using AF.Core.Board;
using AF.Solver.Shuffle;
using NUnit.Framework;

namespace AF.Core.Tests.DotnetOnly
{
    /// <summary>
    /// Pororoca sin asignaciones en el bucle (CLAUDE.md, regla dura 2; .claude/rules/core.md: «Bucle caliente (… Pororoca …)»):
    /// el barajado entero (A* de partida, paseos inversos, permutaciones y A* de verificación) no asigna una vez calentado; solo
    /// asigna el <see cref="BoardState"/> del resultado en <see cref="Shuffler.Shuffle"/>. Y un tope de tiempo orientativo del
    /// peor caso medido aquí (03 §5: «&lt;50 ms [E]» por llamada de A*). Solo dotnet por <see cref="AllocationMeter"/> y
    /// <c>[NonParallelizable]</c>.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public sealed class ShuffleAllocationTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static BoardState Level30()
        {
            return new BoardState(new[]
            {
                C(4, 1, 2, 3, 4), C(4, 5, 6, 1, 2), C(4, 3, 4, 5, 6), C(4, 6, 5, 4, 3), C(4, 2, 1, 6, 5), C(4, 4, 3, 2, 1),
                Cuia.Empty(4), Cuia.Empty(4),
            });
        }

        // K = 10 con 1 vacío (banda 3 del Reto, 03 §16): el caso más caro del cliente.
        private static BoardState K10()
        {
            return new BoardState(new[]
            {
                C(4, 1, 2, 3, 4), C(4, 5, 6, 7, 8), C(4, 9, 10, 1, 2), C(4, 3, 4, 5, 6), C(4, 7, 8, 9, 10), C(4, 2, 1, 4, 3),
                C(4, 6, 5, 8, 7), C(4, 10, 9, 2, 1), C(4, 4, 3, 6, 5), C(4, 8, 7, 10, 9), Cuia.Empty(4),
            });
        }

        [Test]
        public void ShuffleInto_DoesNotAllocate_OnceWarm()
        {
            BoardState board = Level30();
            var shuffler = new Shuffler();
            ShuffleOptions options = ShuffleOptions.Default;
            int inverse = 0;

            AllocationReading inverseRun = AllocationMeter.Measure(() =>
            {
                for (int seed = 1; seed <= 8; seed++)
                {
                    inverse += shuffler.ShuffleInto(board, (ulong)seed, options, out _) == ShuffleMethod.Inverse ? 1 : 0;
                }
            }, 1);

            var permuteOnly = new ShuffleOptions(ShuffleOptions.DefaultParSlack, ShuffleOptions.DefaultBudgetNodes, inverseAttempts: 0);
            int permuted = 0;
            AllocationReading permuteRun = AllocationMeter.Measure(() =>
            {
                for (int seed = 1; seed <= 8; seed++)
                {
                    permuted += shuffler.ShuffleInto(board, (ulong)seed, permuteOnly, out _) == ShuffleMethod.Permute ? 1 : 0;
                }
            }, 1);

            Assert.That(inverse, Is.GreaterThan(0));
            Assert.That(permuted, Is.GreaterThan(0));
            Assert.That(inverseRun.Bytes, Is.EqualTo(0), inverseRun.Describe("Pororoca inversa"));
            Assert.That(permuteRun.Bytes, Is.EqualTo(0), permuteRun.Describe("Pororoca permutar"));
        }

        [Test]
        public void Shuffle_WorstCaseHere_StaysWellUnderASecond()
        {
            // Orientativo (no es la cifra del móvil): 03 §5 estima <50 ms por A* en gama media; aquí se pide que la ola entera,
            // con todos sus intentos, no pase de 1 s en el peor de 20 semillas.
            BoardState board = K10();
            var shuffler = new Shuffler();
            shuffler.Shuffle(board, 1UL, ShuffleOptions.Default);
            long worst = 0;
            int ok = 0;
            for (int seed = 1; seed <= 20; seed++)
            {
                var watch = Stopwatch.StartNew();
                ShuffleResult result = shuffler.Shuffle(board, (ulong)seed, ShuffleOptions.Default);
                watch.Stop();
                worst = System.Math.Max(worst, watch.ElapsedMilliseconds);
                ok += result.Succeeded ? 1 : 0;
            }

            TestContext.WriteLine("Pororoca K=10, E=1: " + ok + "/20 con tablero, peor " + worst + " ms");
            Assert.That(ok, Is.GreaterThan(0));
            Assert.That(worst, Is.LessThan(1000));
        }
    }
}
