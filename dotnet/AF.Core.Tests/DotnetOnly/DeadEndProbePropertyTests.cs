#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Core.Tests.DotnetOnly
{
    /// <summary>
    /// Propiedad de R13 ampliada (D-014; 02 R14): si <see cref="DeadEndProbe"/> declara un estado perdido, el
    /// <see cref="AStarSolver"/> exhaustivo da <see cref="SolveStatus.Unsolvable"/>, con todos los vertidos legales y también con
    /// la poda de fútiles. Se comprueba en partidas al azar (vertidos legales elegidos con el PRNG del núcleo) sobre los 36
    /// tableros de referencia (todas las mecánicas y sus combinaciones) y sobre los niveles 1-200 de los packs; además, en los
    /// tableros de referencia baratos, la partida sigue en Playing a lo largo de la solución A*. Solo dotnet: lotes grandes y
    /// lectura de los packs.
    /// </summary>
    [TestFixture]
    public sealed class DeadEndProbePropertyTests
    {
        private const int PlayoutsPerBoard = 4;
        private const int MaxPoursPerPlayout = 150;

        /// <summary>Tope de A* para medir la alternativa «solo útiles» (informativo): por encima, «no se sabe».</summary>
        private const int InformativeSolveBudget = 50000;

        private static readonly string[] Packs =
        {
            "tutorial_v0.json", "mundo_02_v1.json", "mundo_03_v1.json", "mundo_04_v1.json", "mundo_05_v1.json",
        };

        /// <summary>Recuentos de una tanda de partidas al azar.</summary>
        private sealed class Report
        {
            public int Boards;
            public int States;
            public int Declared;
            public int R13Stuck;
            public int UsefulOnlyWouldDeclare;
            public int UsefulOnlyWinnable;
            public int MaxClosure;
            public readonly List<string> Violations = new List<string>();

            public override string ToString()
            {
                return Boards.ToString(CultureInfo.InvariantCulture) + " tableros, " + States.ToString(CultureInfo.InvariantCulture)
                    + " estados en Playing por R13, " + Declared.ToString(CultureInfo.InvariantCulture) + " declarados por el cierre (máx. "
                    + MaxClosure.ToString(CultureInfo.InvariantCulture) + " estados), " + R13Stuck.ToString(CultureInfo.InvariantCulture)
                    + " partidas acabadas por R13; un cierre solo con útiles declararía " + UsefulOnlyWouldDeclare.ToString(CultureInfo.InvariantCulture)
                    + ", de ellos " + UsefulOnlyWinnable.ToString(CultureInfo.InvariantCulture) + " ganables";
            }
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void R13b_ReferenceBoards_ADeclaredDeadEndIsUnsolvable(PourMode mode)
        {
            var report = new Report();
            for (int i = 0; i < ReferenceBoards.All.Length; i++)
            {
                ReferenceBoard board = ReferenceBoards.All[i];
                Play(board.State, mode, Prng.SeedFrom("amazonfill:deadend:" + board.Name), board.Name, report);
            }

            TestContext.WriteLine(mode + ": " + report);
            Assert.That(report.Violations, Is.Empty);
            Assert.That(report.Declared, Is.GreaterThan(0));
        }

        [Test]
        public void R13b_Campaign1To200_ADeclaredDeadEndIsUnsolvable()
        {
            // Los niveles reales (packs del cliente) en run, el modo de juego (D-005).
            var report = new Report();
            string dir = Path.Combine(RepoRoot(), "Assets", "_Project", "Data", "Levels");
            for (int p = 0; p < Packs.Length; p++)
            {
                using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, Packs[p]))))
                {
                    foreach (JsonElement level in doc.RootElement.GetProperty("levels").EnumerateArray())
                    {
                        int number = level.GetProperty("level").GetInt32();
                        BoardState start = BuildBoard(level);
                        string name = "nivel " + number.ToString(CultureInfo.InvariantCulture);
                        Play(start, PourMode.Run, Prng.SeedFrom("amazonfill:deadend:level:" + number.ToString(CultureInfo.InvariantCulture)), name, report);
                    }
                }
            }

            TestContext.WriteLine(report.ToString());
            Assert.That(report.Boards, Is.EqualTo(200));
            Assert.That(report.Violations, Is.Empty);
            Assert.That(report.Declared, Is.GreaterThan(0));
        }

        private static string[] CheapReferenceNames()
        {
            var names = new List<string>();
            for (int i = 0; i < ReferenceBoards.All.Length; i++)
            {
                if (ReferenceBoards.All[i].Expanded <= 15000)
                {
                    names.Add(ReferenceBoards.All[i].Name);
                }
            }

            return names.ToArray();
        }

        [TestCaseSource(nameof(CheapReferenceNames))]
        public void R13b_WinnableReferenceBoard_IsPlayingAlongItsSolution(string name)
        {
            // R14 con D-014 en tableros de 12-14 cuias con mecánicas: tras cada vertido de la solución A* la partida sigue en
            // Playing hasta la victoria.
            ReferenceBoard board = ReferenceBoards.Get(name);
            SolveResult solution = new AStarSolver().Solve(board.State, new SolverOptions(board.ExpandedLimit, 1, true, PourMode.Run));
            Assert.That(solution.IsSolved, Is.True, name);
            var session = new LevelSession(board.State, true, PourMode.Run);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing), name);

            for (int i = 0; i < solution.MoveCount; i++)
            {
                Pour pour = solution.GetMove(i);
                Assert.That(session.TryPour(pour.Source, pour.Target, out _), Is.True, name);
                LevelStatus expected = i == solution.MoveCount - 1 ? LevelStatus.Won : LevelStatus.Playing;
                Assert.That(session.Status, Is.EqualTo(expected), name + ", vertido " + (i + 1).ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Partidas al azar desde <paramref name="start"/>: en cada estado en Playing por R13, si el cierre lo declara, A* con todos
        /// los vertidos legales y con la poda tienen que dar Unsolvable (y la partida acabaría ahí). Mide también, solo como
        /// informe, cuántos de esos estados declararía un cierre limitado a los vertidos útiles y cuántos de ellos se ganan.
        /// </summary>
        private static void Play(BoardState start, PourMode mode, ulong seed, string name, Report report)
        {
            report.Boards++;
            var probe = new DeadEndProbe();
            var solver = new AStarSolver();
            var allLegal = new SolverOptions(SolverOptions.PipelineNodeBudget, 1, false, mode);
            var pruned = new SolverOptions(SolverOptions.PipelineNodeBudget, 1, true, mode);
            var usefulOnly = new SolverOptions(DeadEndProbe.MaxStates, 1, true, mode);
            var informative = new SolverOptions(InformativeSolveBudget, 1, false, mode);
            var rng = new Prng(seed);
            var legal = new Pour[PourRules.MaxPours];
            for (int playout = 0; playout < PlayoutsPerBoard; playout++)
            {
                BoardState state = start;
                for (int step = 0; step < MaxPoursPerPlayout && !state.IsWon(); step++)
                {
                    if (PourRules.IsStuck(state, true))
                    {
                        report.R13Stuck++;
                        break;
                    }

                    report.States++;
                    bool dead = probe.IsDeadEnd(state, mode);
                    if (dead)
                    {
                        report.Declared++;
                        report.MaxClosure = Math.Max(report.MaxClosure, probe.LastStateCount);
                        SolveResult all = solver.Solve(state, allLegal);
                        SolveResult withPruning = solver.Solve(state, pruned);
                        if (all.Status != SolveStatus.Unsolvable || withPruning.Status != SolveStatus.Unsolvable)
                        {
                            Violation(report, name, mode, state, "A* " + all.Status + " / con poda " + withPruning.Status);
                        }
                    }

                    // Informe: la alternativa del texto de D-014 (cierre solo con útiles) declara cuando la búsqueda con la poda se
                    // agota en ≤ 64 estados sin victoria. Si además A* con todos los legales gana, sería un bloqueo falso (R14).
                    if (solver.Solve(state, usefulOnly).Status == SolveStatus.Unsolvable)
                    {
                        report.UsefulOnlyWouldDeclare++;
                        if (!dead && solver.Solve(state, informative).IsSolved)
                        {
                            report.UsefulOnlyWinnable++;
                        }
                    }

                    if (dead)
                    {
                        break; // la partida queda en Aguas estancadas
                    }

                    int count = PourRules.CollectUsefulMoves(state, false, mode, legal);
                    Pour pick = legal[rng.NextInt(count)];
                    state = PourRules.Apply(state, pick.Source, pick.Target, mode);
                }
            }
        }

        private static void Violation(Report report, string name, PourMode mode, BoardState state, string detail)
        {
            if (report.Violations.Count < 10)
            {
                report.Violations.Add(name + " en " + mode + ": " + state + ": " + detail);
            }
        }

        /// <summary>Tablero inicial de un nivel de pack (<c>af-level/1</c>, 08 §4.1): caps, cuias, attrs y hidden.</summary>
        private static BoardState BuildBoard(JsonElement level)
        {
            JsonElement capsNode = level.GetProperty("caps");
            JsonElement cuiasNode = level.GetProperty("cuias");
            int count = capsNode.GetArrayLength();
            var caps = new int[count];
            var cuias = new int[count][];
            for (int i = 0; i < count; i++)
            {
                caps[i] = capsNode[i].GetInt32();
                JsonElement doses = cuiasNode[i];
                cuias[i] = new int[doses.GetArrayLength()];
                for (int k = 0; k < cuias[i].Length; k++)
                {
                    cuias[i][k] = doses[k].GetInt32();
                }
            }

            MechanicSpec[]? attrs = null;
            if (level.TryGetProperty("attrs", out JsonElement attrsNode) && attrsNode.ValueKind == JsonValueKind.Array)
            {
                for (int i = 0; i < count; i++)
                {
                    JsonElement entry = attrsNode[i];
                    if (entry.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    Assert.That(MechanicTypes.TryParse(entry.GetProperty("type").GetString(), out MechanicType type), Is.True);
                    attrs ??= new MechanicSpec[count];
                    attrs[i] = new MechanicSpec(type, Optional(entry, "key", 0), Optional(entry, "partner", -1), Optional(entry, "n", 0), Optional(entry, "k", 0));
                }
            }

            int[][]? hidden = null;
            if (level.TryGetProperty("hidden", out JsonElement hiddenNode) && hiddenNode.GetArrayLength() > 0)
            {
                hidden = new int[hiddenNode.GetArrayLength()][];
                for (int h = 0; h < hidden.Length; h++)
                {
                    hidden[h] = new[] { hiddenNode[h][0].GetInt32(), hiddenNode[h][1].GetInt32() };
                }
            }

            BoardBuildResult built = BoardBuilder.Build(caps, cuias, attrs, hidden);
            Assert.That(built.Error, Is.Null);
            return built.State!;
        }

        private static int Optional(JsonElement entry, string name, int fallback)
        {
            return entry.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number ? value.GetInt32() : fallback;
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

            throw new InvalidOperationException("No se encuentra la raíz del repositorio.");
        }
    }
}
