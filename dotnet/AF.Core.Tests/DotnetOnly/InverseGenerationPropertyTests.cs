#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Rules;
using AF.Solver.Generation;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Core.Tests.DotnetOnly
{
    /// <summary>
    /// Barajado inverso <c>WALK</c> y <c>DEEP</c> (C1-6; 03 §4.1-§4.2) en lote, con semillas fijas, K de 5 a 11 y E de 1 a 2:
    /// A* resuelve todo estado devuelto (garantía por construcción), ninguna cuia empieza sellada, vacíos = E y complejidad ≥
    /// <c>compl_min</c>; la misma semilla da el mismo estado. Las estadísticas frente a 03 §18.3 son informativas.
    /// El lote completo de 200 semillas por combinación va en los tests explícitos (<c>Category("Informe")</c>).
    /// </summary>
    [TestFixture]
    public sealed class InverseGenerationPropertyTests
    {
        private const int RegressionSeeds = 10;
        private const int RegressionDeepSeeds = 1;
        private const int FullSeeds = 200;

        [Test]
        public void Walk_EveryResultIsValidAndSolvable()
        {
            TestContext.WriteLine(RunWalk(RegressionSeeds));
        }

        [Test]
        public void Deep_EveryResultIsValidAndSolvable()
        {
            TestContext.WriteLine(RunDeep(RegressionDeepSeeds));
        }

        [Test]
        [Explicit("Lote completo de C1-6 (200 semillas por combinación); se ejecuta a mano para el informe.")]
        [Category("Informe")]
        public void Walk_FullBatch()
        {
            TestContext.WriteLine(RunWalk(FullSeeds));
        }

        [Test]
        [Explicit("Lote completo de C1-6 (200 semillas por combinación); se ejecuta a mano para el informe.")]
        [Category("Informe")]
        public void Deep_FullBatch()
        {
            TestContext.WriteLine(RunDeep(FullSeeds));
        }

        [Test]
        public void SameSeed_SameState_ForBothGenerators()
        {
            InverseSpec spec = InverseSpec.Uniform(9, 1);
            for (int s = 1; s <= 10; s++)
            {
                ulong seed = Prng.LevelSeed(4, 1, 100 + s);
                Assert.That(new InverseWalk().Generate(spec, seed).State!.Equals(new InverseWalk().Generate(spec, seed).State), Is.True);
                Assert.That(new InverseDeep().Generate(spec, seed, 2000).State!.Equals(new InverseDeep().Generate(spec, seed, 2000).State), Is.True);
            }
        }

        private static string RunWalk(int seeds)
        {
            var walk = new InverseWalk();
            var solver = new AStarSolver();
            var report = new System.Text.StringBuilder("WALK (03 §4.1): K, E, válidos, profundidad mediana/máx., par mediano, intentos medios, ms por intento\n");
            for (int empties = 1; empties <= 2; empties++)
            {
                for (int colors = 5; colors <= 11; colors++)
                {
                    InverseSpec spec = InverseSpec.Uniform(colors, empties);
                    var depths = new List<int>();
                    var pars = new List<int>();
                    long attempts = 0;
                    var watch = new Stopwatch();
                    for (int s = 0; s < seeds; s++)
                    {
                        watch.Start();
                        InverseResult result = walk.Generate(spec, Prng.LevelSeed(2, 1, 1 + s));
                        watch.Stop();
                        attempts += result.Attempts;
                        Check(result, spec, solver, pars, "walk K=" + colors + " E=" + empties + " semilla " + s);
                        depths.Add(result.Depth);
                    }

                    depths.Sort();
                    pars.Sort();
                    report.Append(colors).Append(", ").Append(empties).Append(", ").Append(depths.Count).Append('/').Append(seeds)
                        .Append(", ").Append(depths[depths.Count / 2]).Append('/').Append(depths[depths.Count - 1])
                        .Append(", ").Append(pars[pars.Count / 2])
                        .Append(", ").Append(((double)attempts / seeds).ToString("0.0", CultureInfo.InvariantCulture))
                        .Append(", ").Append((watch.Elapsed.TotalMilliseconds / attempts).ToString("0.00", CultureInfo.InvariantCulture))
                        .Append('\n');
                }
            }

            return report.ToString();
        }

        private static string RunDeep(int seeds)
        {
            var deep = new InverseDeep();
            var solver = new AStarSolver();
            var report = new System.Text.StringBuilder("DEEP (03 §4.2, 20.000 nodos): K, E, válidos, profundidad mediana/máx., par mín./mediano/máx., ms por nivel\n");
            for (int empties = 1; empties <= 2; empties++)
            {
                for (int colors = 5; colors <= 11; colors++)
                {
                    InverseSpec spec = InverseSpec.Uniform(colors, empties);
                    var depths = new List<int>();
                    var pars = new List<int>();
                    var watch = new Stopwatch();
                    for (int s = 0; s < seeds; s++)
                    {
                        watch.Start();
                        InverseResult result = deep.Generate(spec, Prng.LevelSeed(4, 1, 1 + s));
                        watch.Stop();
                        Check(result, spec, solver, pars, "deep K=" + colors + " E=" + empties + " semilla " + s);
                        depths.Add(result.Depth);
                    }

                    depths.Sort();
                    pars.Sort();
                    report.Append(colors).Append(", ").Append(empties).Append(", ").Append(depths.Count).Append('/').Append(seeds)
                        .Append(", ").Append(depths[depths.Count / 2]).Append('/').Append(depths[depths.Count - 1])
                        .Append(", ").Append(pars[0]).Append('/').Append(pars[pars.Count / 2]).Append('/').Append(pars[pars.Count - 1])
                        .Append(" (2K+1..2K+3 = ").Append((2 * colors) + 1).Append("..").Append((2 * colors) + 3).Append(')')
                        .Append(", ").Append((watch.Elapsed.TotalMilliseconds / seeds).ToString("0", CultureInfo.InvariantCulture))
                        .Append('\n');
                }
            }

            return report.ToString();
        }

        private static void Check(InverseResult result, InverseSpec spec, AStarSolver solver, List<int> pars, string label)
        {
            if (!result.Succeeded)
            {
                Assert.Fail(label + ": sin tablero");
            }

            BoardState state = result.State!;
            int empties = 0;
            for (int i = 0; i < state.Count; i++)
            {
                if (state.IsSealed(i))
                {
                    Assert.Fail(label + ": la cuia " + i + " empieza sellada");
                }

                empties += state[i].IsEmpty ? 1 : 0;
            }

            if (empties != spec.Empties || Complexity.Of(state) < spec.MinComplexity)
            {
                Assert.Fail(label + ": vacíos " + empties + ", complejidad " + Complexity.Of(state));
            }

            SolveResult solved = solver.Solve(state, SolverOptions.ForPipeline(PourMode.Run));
            if (!solved.IsSolved)
            {
                Assert.Fail(label + ": A* no lo resuelve (" + solved.Status + ")");
            }

            pars.Add(solved.MoveCount);
        }
    }
}
