#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Solver.Bot;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Solver.Tests
{
    /// <summary>
    /// Solver con las mecánicas de la 1.1 (D-068, fase F2; 03 §9): el Aguapé (y su variante espesa) y la Lancha son filtros, así
    /// que la heurística (h1, h2) sigue admisible y consistente y el par de A* coincide con el de una búsqueda exhaustiva en
    /// tableros pequeños con capacidades uniformes y mixtas, en los dos modos. Además, la traducción de la solución canónica a
    /// los índices reales con dos grupos de Aguapé (la canonicalización los renumera) y el bot atento ×40 [P]. Test compartido
    /// (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class CurveSolverTests
    {
        private static List<BoardState> Boards(IMechanicInjector injector, ulong seed)
        {
            var boards = MechanicBoards.Generate(seed, 12, injector, false);
            boards.AddRange(MechanicBoards.Generate(seed + 1, 12, injector, true));
            return boards;
        }

        private static void Check(IMechanicInjector injector, ulong seed, PourMode mode)
        {
            List<BoardState> boards = Boards(injector, seed);
            for (int i = 0; i < boards.Count; i++)
            {
                string label = injector.Name + " " + i + " " + boards[i];
                MechanicBoards.AssertAStarMatchesExhaustiveSearch(boards[i], mode, label);
                MechanicBoards.AssertHeuristicAdmissibleAndConsistent(boards[i], mode, label);
            }
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void M8_Aguape_AStarPar_EqualsExhaustiveSearch_AndHeuristicHolds(PourMode mode)
        {
            // 03 §9: «sin cambio (un filtro no rompe la admisibilidad; se comprueba con P1-P3 y capacidades mixtas)».
            Check(new AguapeInjector(), 0x3030_0001UL, mode);
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void M8_AguapeEspeso_AStarPar_EqualsExhaustiveSearch_AndHeuristicHolds(PourMode mode)
        {
            Check(new AguapeInjector(thick: true), 0x3470_0001UL, mode);
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void M8_TwoAguapeGroups_AStarPar_EqualsExhaustiveSearch_AndHeuristicHolds(PourMode mode)
        {
            // Con dos grupos, la canonicalización renumera los ids (03 §3) y el solver traduce su solución con ellos renumerados.
            Check(new AguapeInjector(groups: 2), 0x3031_0001UL, mode);
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void M11_Lancha_AStarPar_EqualsExhaustiveSearch_AndHeuristicHolds(PourMode mode)
        {
            Check(new LanchaInjector(), 0x3630_0001UL, mode);
        }

        [Test]
        public void M8_Solver_NeverPoursFromAHeldAguape_AndItsSolutionReplays()
        {
            // 02 §9.1 M8 regla 2: bajo la mata no es origen. Grupo {0, 1}: la 0 sella recibiendo el 1 de la 2 y suelta las dos.
            BoardState start = Curve.Board(
                Curve.Aguape(4, new[] { 1, 1, 1 }), Curve.Aguape(4, new[] { 2, 2 }), Cuia.Create(4, new[] { 2, 2, 1 }), Cuia.Empty(4));

            SolveResult result = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Run));

            Assert.That(result.IsSolved, Is.True);
            BoardState state = start;
            for (int i = 0; i < result.MoveCount; i++)
            {
                Pour pour = result.GetMove(i);
                Assert.That(state[pour.Source].Mechanic, Is.Not.EqualTo(MechanicType.Aguape), "vertido " + i);
                state = PourRules.Apply(state, pour.Source, pour.Target, PourMode.Run);
            }

            Assert.That(state.IsWon(), Is.True);
        }

        [Test]
        public void M8_Canon_TwoGroupsWithIdenticalCuias_SolveAndReplayOnTheRealIndices()
        {
            // 03 §3: los dos grupos tienen cuias idénticas salvo el id (una forma canónica con empate de grupos). La solución
            // canónica tiene que rehacerse sobre los índices reales con los ids de entrada (6 y 1): el grupo que sella su cuia
            // vacía con el 2 se suelta y su 1 sella la cuia del otro grupo.
            BoardState start = Curve.Board(
                Curve.Aguape(6, new int[0]), Curve.Aguape(6, new[] { 1 }), Curve.Aguape(1, new int[0]), Curve.Aguape(1, new[] { 1 }),
                Cuia.Create(4, new[] { 2, 2, 1, 1 }), Cuia.Create(4, new[] { 2, 2, 3, 3 }), Cuia.Create(4, new[] { 3, 3 }));

            for (int w = 1; w <= 2; w++)
            {
                SolveResult result = new AStarSolver().Solve(start, new SolverOptions(SolverOptions.PipelineNodeBudget, w, true, PourMode.Run));
                Assert.That(result.IsSolved, Is.True, "w = " + w);
                BoardState state = start;
                for (int i = 0; i < result.MoveCount; i++)
                {
                    Pour pour = result.GetMove(i);
                    Assert.That(PourRules.IsLegal(state, pour.Source, pour.Target), Is.True, "w = " + w + ", vertido " + i);
                    state = PourRules.Apply(state, pour.Source, pour.Target, PourMode.Run);
                }

                Assert.That(state.IsWon(), Is.True, "w = " + w);
            }
        }

        [Test]
        public void AttentiveBot_IsReproducible_AndPlaysTheCurveMechanics()
        {
            // Propuesta de la curva §6.4 [P]: misma semilla, mismo resultado; juega Aguapé y Lancha sin código propio.
            var bot = new AttentiveBot();
            var boards = new List<BoardState>();
            boards.AddRange(MechanicBoards.Generate(0xA7E0_0001UL, 6, new AguapeInjector(), false));
            boards.AddRange(MechanicBoards.Generate(0xA7E0_0002UL, 6, new LanchaInjector(), false));
            for (int i = 0; i < boards.Count; i++)
            {
                ulong seed = AttentiveBot.SeedFor("amazonfill:test:" + i);
                BotResult first = bot.FailRate(boards[i], seed, PourMode.Run);
                BotResult again = new AttentiveBot().FailRate(boards[i], seed, PourMode.Run);
                Assert.That(first.Runs, Is.EqualTo(BotOptions.DefaultRuns));
                Assert.That(again.Failures, Is.EqualTo(first.Failures), "tablero " + i);
            }
        }

        [Test]
        public void AttentiveBot_LeavesOutThePoursThatBlockAtOnce_WhenAnotherExists()
        {
            // §6.4 [P]: «sin los vertidos que dejan el tablero bloqueado de inmediato». En tableros pequeños (en orden canónico,
            // así los índices del bot son los del tablero), cada vertido del grupo no bloquea y cada vertido que deja fuera bloquea;
            // si todos bloquean, el grupo son todos.
            var bot = new AttentiveBot();
            var moves = new SolverMove[SolverMoves.MaxMoves];
            var pool = new int[SolverMoves.MaxMoves];
            int excluded = 0;
            var boards = new List<BoardState>();
            boards.AddRange(MechanicBoards.Generate(0xA7E0_0005UL, 16, new LanchaInjector(), true));
            boards.AddRange(MechanicBoards.Generate(0xA7E0_0006UL, 16, new AguapeInjector(), true));
            for (int b = 0; b < boards.Count; b++)
            {
                var sorted = new Cuia[boards[b].Count];
                for (int i = 0; i < sorted.Length; i++)
                {
                    sorted[i] = boards[b][i];
                }

                BoardCanon.Sort(sorted, 0, sorted.Length);
                var state = new BoardState(sorted);
                BoardState? cursor = state;
                for (int step = 0; step < 6 && cursor != null && !cursor.IsWon(); step++)
                {
                    int size = bot.Candidates(cursor, PourMode.Run, moves, pool, out int count);
                    if (count == 0)
                    {
                        break;
                    }

                    var inPool = new bool[count];
                    for (int p = 0; p < size; p++)
                    {
                        inPool[pool[p]] = true;
                    }

                    bool anySafe = false;
                    for (int m = 0; m < count; m++)
                    {
                        BoardState after = PourRules.Apply(cursor, moves[m].Source, moves[m].Target, PourMode.Run);
                        bool blocks = !after.IsWon() && PourRules.IsStuck(after, true);
                        anySafe |= !blocks;
                        if (size < count)
                        {
                            Assert.That(inPool[m], Is.EqualTo(!blocks), "tablero " + b + ", " + moves[m] + " en " + cursor);
                        }

                        excluded += inPool[m] ? 0 : 1;
                    }

                    if (!anySafe)
                    {
                        Assert.That(size, Is.EqualTo(count), "tablero " + b + ": si todos bloquean, el grupo son todos");
                    }
                    BoardState next = PourRules.Apply(cursor, moves[pool[0]].Source, moves[pool[0]].Target, PourMode.Run);
                    var resorted = new Cuia[next.Count];
                    for (int i = 0; i < resorted.Length; i++)
                    {
                        resorted[i] = next[i];
                    }

                    BoardCanon.Sort(resorted, 0, resorted.Length);
                    cursor = new BoardState(resorted);
                }
            }

            TestContext.WriteLine("vertidos que bloquean, fuera del grupo: " + excluded);
            Assert.That(excluded, Is.GreaterThan(0));
        }

        [Test]
        public void AttentiveBot_FailsNoMoreThanTheCasualBot_OnTheSameBoards()
        {
            // Mide la percepción con más cuidado: en tableros pequeños resolubles, el atento falla como mucho igual que el casual
            // sumando 24 tableros (no partida a partida: los dos usan su azar).
            var boards = new List<BoardState>();
            boards.AddRange(MechanicBoards.Generate(0xA7E0_0003UL, 12, new AguapeInjector(), true));
            boards.AddRange(MechanicBoards.Generate(0xA7E0_0004UL, 12, new LanchaInjector(), true));
            int casual = 0;
            int attentive = 0;
            var casualBot = new CasualBot();
            var attentiveBot = new AttentiveBot();
            for (int i = 0; i < boards.Count; i++)
            {
                casual += casualBot.FailRate(boards[i], CasualBot.SeedFor("amazonfill:test:" + i), PourMode.Run).Failures;
                attentive += attentiveBot.FailRate(boards[i], AttentiveBot.SeedFor("amazonfill:test:" + i), PourMode.Run).Failures;
            }

            TestContext.WriteLine("fallos: casual " + casual + ", atento " + attentive);
            Assert.That(attentive, Is.LessThanOrEqualTo(casual));
        }
    }

    /// <summary>Tableros a mano con las mecánicas de la 1.1 para los tests del solver.</summary>
    internal static class Curve
    {
        public static Cuia Aguape(int group, int[] doses, int n = 1)
        {
            return Cuia.Create(4, doses).WithMechanic(MechanicType.Aguape, pairId: group, n: n);
        }

        public static BoardState Board(params Cuia[] cuias)
        {
            return new BoardState(cuias);
        }
    }
}
