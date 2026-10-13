#nullable enable
using System;
using System.Diagnostics;
using System.Globalization;
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Rules;
using AF.Core.Session;
using NUnit.Framework;

namespace AF.Core.Tests.DotnetOnly
{
    /// <summary>
    /// Coste de R13 ampliada (D-014): el cierre acotado corre en <see cref="LevelSession"/> tras cada cambio de estado, así que
    /// tiene que costar menos de 1 ms por llamada en Release con tableros de 12-14 cuias. Se mide en un test explícito de la
    /// categoría <c>Performance</c>, como <see cref="MechanicsPerformanceTests"/>
    /// (<c>dotnet test dotnet/AmazonFill.slnx -c Release --filter Category=Performance</c>): el tablero del nivel 66 (el cierre
    /// se recorre entero), un tablero de 14 cuias (se llena) y todos los estados de partidas al azar sobre los 36 tableros de
    /// referencia, con el peor caso como referencia. Las asignaciones van en <see cref="HotLoopAllocationTests"/>.
    /// </summary>
    [TestFixture]
    public sealed class DeadEndProbePerformanceTests
    {
        /// <summary>Objetivo de T51 en Release: menos de 1 ms por llamada.</summary>
        private const double MaxMs = 1.0;

        private const int Playouts = 4;
        private const int MaxPours = 150;
        private const int Samples = 5;

        [Test]
        [Explicit("Tiempos en Release para T51 (D-014); dependen de la máquina.")]
        [Category("Performance")]
        [NonParallelizable]
        public void Report_DeadEndProbeUnder1MsInRelease()
        {
            var probe = new DeadEndProbe();
            double level66 = Median(probe, Level66Loop(), PourMode.Run, out bool dead66);
            double wide = Median(probe, Wide14(), PourMode.Run, out _);
            double worst = 0;
            double sum = 0;
            int calls = 0;
            int declared = 0;
            string worstBoard = "-";
            var legal = new Pour[PourRules.MaxPours];
            for (int b = 0; b < ReferenceBoards.All.Length; b++)
            {
                ReferenceBoard board = ReferenceBoards.All[b];
                var rng = new Prng(Prng.SeedFrom("amazonfill:deadend:perf:" + board.Name));
                for (int playout = 0; playout < Playouts; playout++)
                {
                    BoardState state = board.State;
                    for (int step = 0; step < MaxPours && !state.IsWon() && !PourRules.IsStuck(state, true); step++)
                    {
                        double ms = Median(probe, state, PourMode.Run, out bool dead);
                        sum += ms;
                        calls++;
                        if (ms > worst)
                        {
                            worst = ms;
                            worstBoard = board.Name + " (" + probe.LastStateCount.ToString(CultureInfo.InvariantCulture) + " estados)";
                        }

                        if (dead)
                        {
                            declared++;
                            break;
                        }

                        int count = PourRules.CollectUsefulMoves(state, false, PourMode.Run, legal);
                        Pour pick = legal[rng.NextInt(count)];
                        state = PourRules.Apply(state, pick.Source, pick.Target, PourMode.Run);
                    }
                }
            }

            TestContext.WriteLine("Nivel 66 (cierre entero, declarado " + dead66 + "): " + Format(level66) + " ms; 14 cuias (se llena): "
                + Format(wide) + " ms; partidas al azar: " + calls.ToString(CultureInfo.InvariantCulture) + " llamadas, media "
                + Format(calls == 0 ? 0 : sum / calls) + " ms, peor " + Format(worst) + " ms en " + worstBoard + "; "
                + declared.ToString(CultureInfo.InvariantCulture) + " declaradas");
#if DEBUG
            Assert.Inconclusive("Los tiempos solo valen en Release: dotnet test dotnet/AmazonFill.slnx -c Release --filter Category=Performance");
#else
            Assert.That(dead66, Is.True);
            Assert.That(level66, Is.LessThan(MaxMs));
            Assert.That(wide, Is.LessThan(MaxMs));
            Assert.That(worst, Is.LessThan(MaxMs));
#endif
        }

        /// <summary>Mediana de <see cref="Samples"/> llamadas calentadas, en ms.</summary>
        private static double Median(DeadEndProbe probe, BoardState state, PourMode mode, out bool dead)
        {
            dead = probe.IsDeadEnd(state, mode);
            var samples = new double[Samples];
            for (int i = 0; i < samples.Length; i++)
            {
                long start = Stopwatch.GetTimestamp();
                probe.IsDeadEnd(state, mode);
                samples[i] = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
            }

            Array.Sort(samples);
            return samples[samples.Length / 2];
        }

        private static string Format(double ms)
        {
            return ms.ToString("F3", CultureInfo.InvariantCulture);
        }

        private static BoardState Level66Loop()
        {
            return new BoardState(new[]
            {
                Cuia.Create(4, new[] { 3, 8, 10 }), Cuia.Create(4, new[] { 4, 7 }), Cuia.Create(4, new[] { 10, 7, 7, 7 }),
                Cuia.Create(4, new[] { 14, 14, 14, 14 }), Cuia.Create(4, new[] { 6, 6, 6, 6 }), Cuia.Create(2, new[] { 4, 4 }),
                Cuia.Create(4, new[] { 3 }), Cuia.Create(4, new[] { 8, 8, 10, 10 }), Cuia.Create(4, new[] { 3, 3, 8, 1 }),
                Cuia.Create(4, new[] { 1, 1, 1, 4 }),
            });
        }

        /// <summary>El tablero de 14 cuias de <see cref="HotLoopAllocationTests"/> (12 colores, 2 vacías).</summary>
        private static BoardState Wide14()
        {
            int[][] doses =
            {
                new[] { 9, 8, 5, 5 }, new[] { 6, 8, 6, 6 }, new[] { 4, 5, 12, 2 }, new[] { 1, 7, 10, 11 }, new[] { 12, 1, 12, 3 },
                new[] { 8, 2, 1, 3 }, new[] { 2, 3, 11, 5 }, new[] { 9, 11, 4, 4 }, new[] { 11, 8, 9, 12 }, new[] { 2, 3, 10, 7 },
                new[] { 9, 7, 1, 7 }, new[] { 10, 10, 6, 4 }, new int[0], new int[0],
            };
            var cuias = new Cuia[doses.Length];
            for (int i = 0; i < cuias.Length; i++)
            {
                cuias[i] = Cuia.Create(4, doses[i]);
            }

            return new BoardState(cuias);
        }
    }
}
