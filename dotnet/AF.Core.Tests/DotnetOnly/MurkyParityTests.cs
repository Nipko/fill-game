#nullable enable
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Murky;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Core.Tests.DotnetOnly
{
    /// <summary>
    /// Paridad de Aguas turbias con el prototipo (03 §8 y §18.3; C2-2): los 5 tableros de
    /// <c>tools/out_inverse/murky_demo.json</c> (8 cuias, 6 colores, 2 cuias turbias × 2 capas ocultas = 4 dosis) tienen ruta
    /// segura con 12 realidades como máximo, y <see cref="SafeMoves"/> asigna menos de 1 KB por realidad una vez calentada.
    /// Solo dotnet: lee JSON del disco, mide tiempos y asignaciones.
    /// </summary>
    /// <remarks>
    /// <para><c>murky_demo.json</c> solo guarda la semilla, las cuias turbias, el par y la ruta del prototipo, no los tableros.
    /// Los tableros de abajo los reproduce <c>levelgen_proto.generate_inverse(6, 2, (4,)*8, U = 4, 36, semilla, 18)</c> con
    /// las 5 semillas del archivo (mismo par que el archivo); las capas ocultas son las 1-2 de cada cuia turbia, como en
    /// <c>murky_demo</c>. No se porta el PRNG MT19937 (decisión pendiente, riesgo R11): son datos fijos.</para>
    /// <para>El prototipo es anterior a T4 y a T3: sus cuias turbias están llenas (T3 pide como mucho 3 dosis) y n0003 y
    /// n0004 esconden una dosis bajo otra de su mismo color (T4). Por eso la paridad es con <c>enforceT4 = false</c> (las
    /// realidades son todas las permutaciones del multiconjunto oculto, como en el prototipo); con T4, 03 §8 rechaza n0003 y
    /// n0004 y los otros tres tienen ruta segura con menos realidades.</para>
    /// </remarks>
    [TestFixture]
    public sealed class MurkyParityTests
    {
        private static readonly string[] Seeds =
        {
            "amazonfill:v1:murky:n0001", "amazonfill:v1:murky:n0002", "amazonfill:v1:murky:n0003", "amazonfill:v1:murky:n0004", "amazonfill:v1:murky:n0005",
        };

        private static readonly int[][][] Boards =
        {
            new[] { new[] { 1, 1, 1, 3 }, new int[0], new[] { 5, 3, 5, 2 }, new[] { 4, 4, 4, 2 }, new[] { 5, 5, 4, 2 }, new[] { 6, 3, 3, 2 }, new int[0], new[] { 6, 6, 6, 1 } },
            new[] { new int[0], new[] { 2, 2, 6, 4 }, new[] { 3, 3, 6, 2 }, new int[0], new[] { 5, 1, 1, 4 }, new[] { 5, 1, 6, 4 }, new[] { 5, 5, 1, 4 }, new[] { 3, 3, 6, 2 } },
            new[] { new[] { 1, 1, 3, 6 }, new int[0], new int[0], new[] { 4, 2, 2, 3 }, new[] { 5, 5, 2, 1 }, new[] { 6, 6, 3, 6 }, new[] { 4, 4, 4, 2 }, new[] { 5, 5, 3, 1 } },
            new[] { new int[0], new[] { 2, 1, 1, 1 }, new[] { 3, 3, 3, 1 }, new[] { 4, 4, 4, 6 }, new[] { 5, 5, 5, 3 }, new[] { 6, 6, 2, 2 }, new[] { 5, 4, 6, 2 }, new int[0] },
            new[] { new[] { 1, 1, 1, 2 }, new int[0], new[] { 3, 2, 3, 5 }, new[] { 4, 4, 2, 6 }, new int[0], new[] { 6, 5, 5, 2 }, new[] { 4, 3, 5, 3 }, new[] { 4, 6, 6, 1 } },
        };

        private static readonly int[][] MurkyCuias = { new[] { 2, 4 }, new[] { 5, 1 }, new[] { 0, 3 }, new[] { 6, 1 }, new[] { 2, 3 } };

        private static readonly bool[] SatisfiesT4 = { true, true, false, false, true };

        /// <summary>El tablero <paramref name="index"/> con las capas 1-2 de sus cuias turbias ocultas.</summary>
        private static BoardState Board(int index)
        {
            int[][] doses = Boards[index];
            var cuias = new Cuia[doses.Length];
            for (int i = 0; i < doses.Length; i++)
            {
                bool murky = i == MurkyCuias[index][0] || i == MurkyCuias[index][1];
                cuias[i] = Cuia.Create(4, doses[i], murky ? 0x6 : 0);
            }

            return new BoardState(cuias);
        }

        private static BoardState Visible(int index)
        {
            int[][] doses = Boards[index];
            var cuias = new Cuia[doses.Length];
            for (int i = 0; i < doses.Length; i++)
            {
                cuias[i] = Cuia.Create(4, doses[i]);
            }

            return new BoardState(cuias);
        }

        [Test]
        public void DemoBoards_MatchThePrototypeFile_0318_3()
        {
            // Las semillas, las cuias turbias y el par (A* sobre el tablero visible) son los de murky_demo.json.
            string path = Path.Combine(RepoRoot(), "tools", "out_inverse", "murky_demo.json");
            var solver = new AStarSolver();
            using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path)))
            {
                JsonElement levels = doc.RootElement;
                Assert.That(levels.GetArrayLength(), Is.EqualTo(Boards.Length));
                for (int n = 0; n < Boards.Length; n++)
                {
                    JsonElement level = levels[n];
                    Assert.That(level.GetProperty("seed").GetString(), Is.EqualTo(Seeds[n]));
                    Assert.That(level.GetProperty("murky")[0].GetInt32(), Is.EqualTo(MurkyCuias[n][0]));
                    Assert.That(level.GetProperty("murky")[1].GetInt32(), Is.EqualTo(MurkyCuias[n][1]));
                    Assert.That(level.GetProperty("ok").GetBoolean(), Is.True);
                    Assert.That(level.GetProperty("realities_max").GetInt32(), Is.EqualTo(12));
                    int par = level.GetProperty("par").GetInt32();
                    Assert.That(solver.Solve(Visible(n), SolverOptions.ForPipeline(PourMode.Run)).MoveCount, Is.EqualTo(par), Seeds[n]);
                    Assert.That(RealityEnumerator.SatisfiesT4(Board(n)), Is.EqualTo(SatisfiesT4[n]), Seeds[n]);
                }
            }
        }

        [Test]
        public void DemoBoards_HaveASafePath_LikeThePrototype_038_0318_3()
        {
            // 03 §18.3: «5/5 con ruta segura; 12 realidades máx.; 0,7-8,5 s» (Python); 03 §8: en C# < 1 s [E].
            string path = Path.Combine(RepoRoot(), "tools", "out_inverse", "murky_demo.json");
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            var safePath = new SafePath();
            SolverOptions options = SafeMoves.ForPipeline(PourMode.Run);
            for (int n = 0; n < Boards.Length; n++)
            {
                BoardState start = Board(n);
                safePath.Moves.ClearMemo();
                var clock = Stopwatch.StartNew();
                SafePathResult result = safePath.Find(start, options, false);
                clock.Stop();
                int searches = safePath.Moves.Searches;
                long expanded = safePath.Moves.Expanded;

                Assert.That(result.Status, Is.EqualTo(SafePathStatus.Ok), Seeds[n]);
                Assert.That(result.MaxRealities, Is.EqualTo(12), Seeds[n]);
                Assert.That(Wins(start, result), Is.True, Seeds[n]);

                safePath.Moves.ClearMemo();
                var clockT4 = Stopwatch.StartNew();
                SafePathResult withT4 = safePath.Find(start, options);
                clockT4.Stop();
                if (SatisfiesT4[n])
                {
                    Assert.That(withT4.Status, Is.EqualTo(SafePathStatus.Ok), Seeds[n] + " con T4");
                    Assert.That(withT4.MaxRealities, Is.LessThanOrEqualTo(12), Seeds[n] + " con T4");
                    Assert.That(Wins(start, withT4), Is.True, Seeds[n] + " con T4");
                }
                else
                {
                    Assert.That(withT4.Status, Is.EqualTo(SafePathStatus.RejectedT4), Seeds[n] + " con T4");
                }

                JsonElement level = doc.RootElement[n];
                TestContext.WriteLine(Seeds[n] + ": ruta segura " + result.MoveCount.ToString(CultureInfo.InvariantCulture)
                    + " vertidos (prototipo " + level.GetProperty("safe_path_len").GetInt32().ToString(CultureInfo.InvariantCulture)
                    + ", par " + level.GetProperty("par").GetInt32().ToString(CultureInfo.InvariantCulture) + "), "
                    + result.MaxRealities.ToString(CultureInfo.InvariantCulture) + " realidades máx., "
                    + result.FirstSafeMoves.ToString(CultureInfo.InvariantCulture) + " seguros en S0, "
                    + searches.ToString(CultureInfo.InvariantCulture) + " búsquedas A*, "
                    + expanded.ToString(CultureInfo.InvariantCulture) + " expandidos, "
                    + clock.Elapsed.TotalMilliseconds.ToString("F0", CultureInfo.InvariantCulture) + " ms (prototipo "
                    + level.GetProperty("seconds").GetDouble().ToString("F2", CultureInfo.InvariantCulture) + " s); con T4: "
                    + withT4.Status + " " + withT4.MoveCount.ToString(CultureInfo.InvariantCulture) + " vertidos, "
                    + withT4.MaxRealities.ToString(CultureInfo.InvariantCulture) + " realidades máx., "
                    + clockT4.Elapsed.TotalMilliseconds.ToString("F0", CultureInfo.InvariantCulture) + " ms");
            }
        }

        [Test]
        public void SafeMoves_AllocateLessThan1KBPerReality_AfterWarmingUp()
        {
            // C2-2: < 1 KB por realidad tras calentar. Se calienta con el mismo tablero (búferes del solver, de las realidades
            // y del memo a su tamaño) y se vacía el memo antes de cada pasada, así que la medida incluye todas las búsquedas
            // A*. Calentamiento y mínimo de AllocationMeter (FX9).
            BoardState start = Board(0);
            var safe = new SafeMoves();
            SolverOptions options = SafeMoves.ForPipeline(PourMode.Run);
            RealityStatus status = safe.Find(start, options, null, false);
            AllocationReading allocated = AllocationMeter.Measure(
                () => safe.ClearMemo(), () => status = safe.Find(start, options, null, false), 1024L * safe.RealityCount);

            TestContext.WriteLine(safe.RealityCount + " realidades, " + safe.Searches + " búsquedas A*, " + allocated.Describe("SafeMoves"));
            Assert.That(status, Is.EqualTo(RealityStatus.Ok));
            Assert.That(safe.RealityCount, Is.EqualTo(12));
            Assert.That(safe.Searches, Is.GreaterThan(0));
            Assert.That(allocated.Bytes, Is.LessThan(1024L * safe.RealityCount), allocated.Describe("SafeMoves"));
        }

        private static bool Wins(BoardState start, SafePathResult result)
        {
            BoardState state = start;
            for (int i = 0; i < result.MoveCount; i++)
            {
                Pour pour = result.GetMove(i);
                state = PourRules.Apply(state, pour.Source, pour.Target, PourMode.Run);
            }

            return state.IsWon();
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
