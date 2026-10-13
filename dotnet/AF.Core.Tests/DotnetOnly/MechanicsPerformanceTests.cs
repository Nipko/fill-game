#nullable enable
using System.Diagnostics;
using System.Globalization;
using System.Text;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Bot;
using AF.Solver.Murky;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Core.Tests.DotnetOnly
{
    /// <summary>
    /// Solver y bot con todas las mecánicas sobre los tableros de referencia de 12-14 cuias (C3-3; <see cref="ReferenceBoards"/>):
    /// GDD 03 §5 (300.000 nodos en el pipeline con w = 1; Morpho con w = 2 y 40.000), 03 §6 y 08 §14.2 (bot reproducible) y
    /// GDD §6 #6 (Lluvia y Piraña entran en la 1.0 solo si el solver con estado temporal pasa los tests). Con Tinaja (M13) las
    /// búsquedas van sin poda de fútiles (<c>SolverOptions.ForLevel</c>; propuesta de la tinaja, b.2 R14). Las asignaciones van
    /// en <see cref="HotLoopAllocationTests"/>. Los tiempos se miden en un test explícito de la categoría <c>Performance</c>
    /// (<c>dotnet test dotnet/AmazonFill.slnx -c Release --filter Category=Performance</c>) para no hacer frágil la suite.
    /// </summary>
    [TestFixture]
    [Parallelizable(ParallelScope.Children)]
    public sealed class MechanicsPerformanceTests
    {
        /// <summary><c>hint_budget_nodes</c> de Morpho (03 §5; 08 §9).</summary>
        private const int MorphoBudget = 40000;

        /// <summary>Peso de Morpho (03 §5).</summary>
        private const int MorphoWeight = 2;

        /// <summary>Morpho en el PC del dueño, Release (08 §14.2: «40.000 nodos &lt; 50 ms en PC»).</summary>
        private const double MorphoMaxMs = 50.0;

        /// <summary>Bot ×40 por tablero [informativo] (C3-3).</summary>
        private const double BotMaxMs = 200.0;

        private static string[] Names()
        {
            return ReferenceBoards.Names();
        }

        [Test]
        public void TheCatalog_CoversEveryMechanicAndEveryCombinationOfTheD4Calendar()
        {
            // Propuesta de D-007, d.4: las 9 mecánicas de 1-200, las dos capacidades y las 25 combinaciones del calendario; y la
            // Tinaja (M13; propuesta de la tinaja, c.6: «una fila en ReferenceBoards y en MechanicsPerformanceTests»).
            string[] expected =
            {
                "Ch", "Al", "Ho", "Ma", "Li", "Tu", "Re", "Ba", "Ll", "Br", "Pi",
                "Ho+Ch", "Ma+Ho", "Li+Ho", "Ho+Tu", "Re+Li", "Ba+Ma", "Ll+Ho", "Br+Ba", "Pi+Ma",
                "Ma+Tu", "Ba+Tu", "Br+Tu", "Ll+Re", "Ll+Ba", "Br+Ma", "Pi+Li", "Re+Ma", "Re+Ho",
                "Ba+Li", "Br+Ho", "Ba+Re", "Li+Ma", "Ll+Li+Al6", "Pi+Ba", "Ma+Li", "Ti",
            };

            Assert.That(Names(), Is.EqualTo(expected));
            for (int i = 0; i < ReferenceBoards.All.Length; i++)
            {
                int count = ReferenceBoards.All[i].State.Count;
                Assert.That(count, Is.InRange(12, 14), ReferenceBoards.All[i].Name);
            }
        }

        [TestCaseSource(nameof(Names))]
        public void ReferenceBoard_IsValid_AndAStarSolvesItWithinItsPinnedBudget_035(string name)
        {
            // 03 §5: par y solución del pipeline con A* (w = 1) y 300.000 expandidos como máximo; el número observado queda
            // fijado con un 25 % de margen (ReferenceBoard.ExpandedLimit). Con Tinaja, sin poda de fútiles (b.2 R14).
            ReferenceBoard board = ReferenceBoards.Get(name);
            string? error = LevelValidation.Validate(board.State);
            if (board.ExpectedValidation == null)
            {
                Assert.That(error, Is.Null, name);
            }
            else
            {
                Assert.That(error, Does.Contain(board.ExpectedValidation), name);
            }

            SolveResult result = new AStarSolver().Solve(board.State, SolverOptions.ForPipeline(PourMode.Run, board.State));

            Assert.That(result.Status, Is.EqualTo(SolveStatus.Solved), name);
            Assert.That(result.MoveCount, Is.EqualTo(board.Par), name);
            Assert.That(result.Expanded, Is.LessThanOrEqualTo(board.ExpandedLimit), name);
            Assert.That(result.Expanded, Is.LessThanOrEqualTo(SolverOptions.PipelineNodeBudget), name);
            Assert.That(Replays(board.State, result), Is.True, name + ": la solución no gana");
        }

        [TestCaseSource(nameof(Names))]
        public void Morpho_W2_Budget40000_IsDeterministic_035(string name)
        {
            // 03 §5: Morpho con w = 2 y 40.000 nodos: el mismo tablero da la misma pista con un solver nuevo y con uno
            // reutilizado (determinismo; en Unity corre en un hilo de fondo).
            ReferenceBoard board = ReferenceBoards.Get(name);
            SolverOptions options = new SolverOptions(MorphoBudget, MorphoWeight, true, PourMode.Run).ForLevel(board.State);
            var reused = new AStarSolver();
            reused.Solve(ReferenceBoards.Get(name == "Ch" ? "Al" : "Ch").State, options);

            SolveResult first = new AStarSolver().Solve(board.State, options);
            SolveResult second = reused.Solve(board.State, options);

            Assert.That(first.Status, Is.EqualTo(SolveStatus.Solved), name);
            Assert.That(first.MoveCount, Is.GreaterThanOrEqualTo(board.Par), name);
            Assert.That(second.Expanded, Is.EqualTo(first.Expanded), name);
            Assert.That(second.CopyMoves(), Is.EqualTo(first.CopyMoves()), name);
            Assert.That(Replays(board.State, first), Is.True, name);
        }

        [TestCaseSource(nameof(Names))]
        public void CasualBot_IsReproducible_0142(string name)
        {
            // 08 §14.2 y 03 §6: fallo_bot reproducible por semilla, también con mecánicas (la puntuación no cambia, C1-5).
            ReferenceBoard board = ReferenceBoards.Get(name);
            ulong seed = BotSeed(name);

            BotResult first = new CasualBot().FailRate(board.State, seed, PourMode.Run);
            BotResult second = new CasualBot().FailRate(board.State, seed, PourMode.Run);

            Assert.That(first.Runs, Is.EqualTo(BotOptions.DefaultRuns));
            Assert.That(second.Failures, Is.EqualTo(first.Failures), name);
        }

        [Test]
        [Explicit("Tiempos en Release para el informe de C3-3 (README del paquete); dependen de la máquina.")]
        [Category("Performance")]
        [NonParallelizable]
        public void Report_TimingsInRelease_MorphoUnder50Ms()
        {
            // Tabla de evidencias para cerrar el GDD §6 #6: A* (w = 1, 300.000), Morpho (w = 2, 40.000) y bot ×40 por tablero,
            // calentados, mediana de 5 medidas; en los tableros con Aguas turbias, también SafeMoves en el tablero inicial.
            var report = new StringBuilder();
            report.AppendLine("| Tablero | Cuias | Par | A* expandidos | A* ms | Morpho largo | Morpho expandidos | Morpho ms | Bot x40 fallos | Bot x40 ms | Realidades | SafeMoves pipeline ms | SafeMoves Morpho ms |");
            report.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            var solver = new AStarSolver();
            var bot = new CasualBot();
            var safe = new SafeMoves();
            var morphoOptions = new SolverOptions(MorphoBudget, MorphoWeight, true, PourMode.Run);
            double worstMorpho = 0;
            double worstBot = 0;
            for (int i = 0; i < ReferenceBoards.All.Length; i++)
            {
                ReferenceBoard board = ReferenceBoards.All[i];
                BoardState state = board.State;
                SolverOptions pipelineOptions = SolverOptions.ForPipeline(PourMode.Run, state);
                SolverOptions levelMorpho = morphoOptions.ForLevel(state);
                SolveResult pipeline = solver.Solve(state, pipelineOptions);
                double pipelineMs = Median(() => solver.Solve(state, pipelineOptions));
                SolveResult morpho = solver.Solve(state, levelMorpho);
                double morphoMs = Median(() => solver.Solve(state, levelMorpho));
                ulong seed = BotSeed(board.Name);
                BotResult botResult = bot.FailRate(state, seed, PourMode.Run);
                double botMs = Median(() => bot.FailRate(state, seed, PourMode.Run));
                string realities = "-";
                string safeMs = "-";
                string safeMorphoMs = "-";
                if (state.HasHidden)
                {
                    // 03 §8: 20.000 nodos por realidad en el pipeline (w = 1); el cliente pasa las opciones de Morpho (02 T2:
                    // las realidades se enumeran en < 50 ms para Morpho).
                    SolverOptions realityOptions = SafeMoves.ForPipeline(PourMode.Run, state);
                    safe.Find(state, realityOptions);
                    double ms = Median(() =>
                    {
                        safe.ClearMemo();
                        safe.Find(state, realityOptions);
                    });
                    realities = safe.RealityCount.ToString(CultureInfo.InvariantCulture);
                    safeMs = ms.ToString("F1", CultureInfo.InvariantCulture);
                    safe.Find(state, levelMorpho);
                    double morphoSafe = Median(() =>
                    {
                        safe.ClearMemo();
                        safe.Find(state, levelMorpho);
                    });
                    safeMorphoMs = morphoSafe.ToString("F1", CultureInfo.InvariantCulture);
                }

                worstMorpho = morphoMs > worstMorpho ? morphoMs : worstMorpho;
                worstBot = botMs > worstBot ? botMs : worstBot;
                report.Append("| ").Append(board.Name).Append(" | ").Append(Num(state.Count)).Append(" | ").Append(Num(pipeline.MoveCount))
                    .Append(" | ").Append(Num(pipeline.Expanded)).Append(" | ").Append(pipelineMs.ToString("F1", CultureInfo.InvariantCulture))
                    .Append(" | ").Append(Num(morpho.MoveCount)).Append(" | ").Append(Num(morpho.Expanded))
                    .Append(" | ").Append(morphoMs.ToString("F2", CultureInfo.InvariantCulture))
                    .Append(" | ").Append(Num(botResult.Failures)).Append("/40 | ").Append(botMs.ToString("F1", CultureInfo.InvariantCulture))
                    .Append(" | ").Append(realities).Append(" | ").Append(safeMs).Append(" | ").Append(safeMorphoMs).AppendLine(" |");
            }

            TestContext.WriteLine(report.ToString());
            TestContext.WriteLine("Morpho máx. " + worstMorpho.ToString("F2", CultureInfo.InvariantCulture) + " ms; bot ×40 máx. "
                + worstBot.ToString("F1", CultureInfo.InvariantCulture) + " ms [informativo, objetivo < "
                + BotMaxMs.ToString("F0", CultureInfo.InvariantCulture) + " ms]");
#if DEBUG
            Assert.Inconclusive("Los tiempos solo valen en Release: dotnet test dotnet/AmazonFill.slnx -c Release --filter Category=Performance");
#else
            Assert.That(worstMorpho, Is.LessThan(MorphoMaxMs));
#endif
        }

        /// <summary>Semilla del bot estable por nombre de tablero (FNV-1a de AF.Core, sin <c>string.GetHashCode</c>).</summary>
        private static ulong BotSeed(string name)
        {
            return CasualBot.SeedFor("amazonfill:ref:" + name);
        }

        private static bool Replays(BoardState start, SolveResult result)
        {
            BoardState state = start;
            for (int i = 0; i < result.MoveCount; i++)
            {
                Pour pour = result.GetMove(i);
                if (!PourRules.IsLegal(state, pour.Source, pour.Target))
                {
                    return false;
                }

                state = PourRules.Apply(state, pour.Source, pour.Target, PourMode.Run);
            }

            return state.IsWon();
        }

        private static double Median(System.Action action)
        {
            var samples = new double[5];
            for (int i = 0; i < samples.Length; i++)
            {
                long start = Stopwatch.GetTimestamp();
                action();
                samples[i] = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
            }

            System.Array.Sort(samples);
            return samples[samples.Length / 2];
        }

        private static string Num(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
