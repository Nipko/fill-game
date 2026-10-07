#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Core.Tests.DotnetOnly
{
    /// <summary>
    /// Paridad del solver A* (GDD 03 §5) con el prototipo <c>tools/levelgen_proto.py</c>: para los 140 niveles que
    /// el prototipo publicó con su par óptimo (A* w = 1, mismo criterio de fútiles con capacidad 4), AF.Solver
    /// devuelve el mismo par y una solución que gana al rehacerla con <see cref="PourRules"/>. Solo dotnet: lee
    /// JSON del disco (informe D §5.2).
    /// </summary>
    [TestFixture]
    public sealed class SolverParityTests
    {
        [TestCase("tools/out/levels_proto.json", 20)]
        [TestCase("tools/out_inverse/levels_proto.json", 20)]
        [TestCase("tools/out_inverse/levels_size_100.json", 100)]
        public void PrototypePack_ParAndSolutionMatch(string relativePath, int expectedLevels)
        {
            string path = Path.Combine(RepoRoot(), relativePath);
            var solver = new AStarSolver();
            var failures = new List<string>();
            int levels = 0;
            using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path)))
            {
                foreach (JsonElement level in doc.RootElement.GetProperty("levels").EnumerateArray())
                {
                    levels++;
                    string id = level.GetProperty("id").GetString() ?? "?";
                    int par = level.GetProperty("difficulty").GetProperty("par").GetInt32();
                    BoardState start = BuildBoard(level);

                    SolveResult result = solver.Solve(start, SolverOptions.Pipeline);

                    if (result.Status != SolveStatus.Solved || result.MoveCount != par)
                    {
                        failures.Add(id + ": par " + par + ", AF.Solver " + result.Status + " " + result.MoveCount);
                        continue;
                    }

                    BoardState state = start;
                    for (int i = 0; i < result.MoveCount; i++)
                    {
                        Pour pour = result.GetMove(i);
                        if (PourRules.Amount(state, pour.Source, pour.Target) != pour.Amount)
                        {
                            failures.Add(id + ": el vertido " + i + " no se puede rehacer");
                            break;
                        }

                        state = PourRules.Apply(state, pour.Source, pour.Target);
                    }

                    if (!state.IsWon())
                    {
                        failures.Add(id + ": la solución no gana");
                    }
                }
            }

            Assert.That(levels, Is.EqualTo(expectedLevels));
            Assert.That(failures, Is.Empty);
        }

        private static BoardState BuildBoard(JsonElement level)
        {
            JsonElement cuias = level.GetProperty("cuias");
            JsonElement caps = level.GetProperty("caps");
            var built = new Cuia[cuias.GetArrayLength()];
            for (int i = 0; i < built.Length; i++)
            {
                JsonElement doses = cuias[i];
                var colors = new int[doses.GetArrayLength()];
                for (int k = 0; k < colors.Length; k++)
                {
                    colors[k] = doses[k].GetInt32();
                }

                built[i] = Cuia.Create(caps[i].GetInt32(), colors);
            }

            return new BoardState(built);
        }

        private static string RepoRoot()
        {
            DirectoryInfo? dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "tools", "levelgen_proto.py")))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException(
                "No se encontró la raíz del repositorio (tools/levelgen_proto.py) subiendo desde " + TestContext.CurrentContext.TestDirectory);
        }
    }
}
