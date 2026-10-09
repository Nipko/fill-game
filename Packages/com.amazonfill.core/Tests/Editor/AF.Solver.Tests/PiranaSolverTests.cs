#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Solver.Tests
{
    /// <summary>
    /// Solver con Piraña (03 §9: el contador va en la clave canónica; el último destino es el del propio vertido, así que no
    /// se guarda): el par de A* coincide con el de una búsqueda exhaustiva en 32 tableros pequeños (capacidades uniformes y
    /// mixtas; la cueva es la cuia vacía más chica) en los dos modos, y <c>H</c> sigue siendo admisible y consistente en todos
    /// sus estados alcanzables (el mordisco se lleva una dosis que el propio vertido acaba de dejar en la cima: ver
    /// <see cref="SolverHeuristic"/>). Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class PiranaSolverTests
    {
        /// <summary>El contador multiplica los estados (×2-3, 03 §9): tableros de como mucho 5.000 en single.</summary>
        private const int MaxStates = 5000;

        private static List<BoardState> Boards()
        {
            var boards = MechanicBoards.Generate(0x1830_0001UL, 16, new PiranaInjector(), false, MaxStates);
            boards.AddRange(MechanicBoards.Generate(0x1830_0002UL, 16, new PiranaInjector(), true, MaxStates));
            return boards;
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void M7_AStarPar_EqualsExhaustiveSearch(PourMode mode)
        {
            List<BoardState> boards = Boards();
            for (int i = 0; i < boards.Count; i++)
            {
                MechanicBoards.AssertAStarMatchesExhaustiveSearch(boards[i], mode, "pirana " + i + " " + boards[i]);
            }
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void M7_Heuristic_IsAdmissibleAndConsistent_039(PourMode mode)
        {
            // 03 §9 temía que h1 y h2 dejaran de ser cotas con la Piraña; con el mordisco inmediato de la respuesta Q-C, un
            // vertido con mordisco baja cada término como mucho en 1 y el par es exacto.
            List<BoardState> boards = Boards();
            int bites = 0;
            for (int i = 0; i < boards.Count; i++)
            {
                MechanicBoards.AssertHeuristicAdmissibleAndConsistent(boards[i], mode, "pirana " + i + " " + boards[i]);
                bites += i % 4 == 0 ? CountBites(boards[i], mode) : 0;
            }

            Assert.That(bites, Is.GreaterThan(50), "los tableros tienen que tener mordiscos");
        }

        [Test]
        public void M7_Heuristic_IsTheSameAsWithoutThePirana()
        {
            // La cueva no cambia la heurística (03 §5): H depende solo del contenido.
            Cuia cave = Cuia.Create(2, new[] { 2 }).WithMechanic(MechanicType.Pirana, k: 3, counter: 1);
            var with = new BoardState(new[] { cave, Cuia.Create(4, new[] { 1, 2, 1 }), Cuia.Create(4, new[] { 2, 2, 1, 1 }), Cuia.Empty(4) });
            var without = new BoardState(new[] { Cuia.Create(2, new[] { 2 }), with[1], with[2], with[3] });

            Assert.That(SolverHeuristic.Estimate(with, PourMode.Run), Is.EqualTo(SolverHeuristic.Estimate(without, PourMode.Run)));
            Assert.That(SolverHeuristic.Estimate(with, PourMode.Single), Is.EqualTo(SolverHeuristic.Estimate(without, PourMode.Single)));
        }

        [Test]
        public void M7_Solver_ParIsExact_AndTheSolutionWins()
        {
            // El tablero del anexo B de PiranaTests (el mordisco puede sellar la cueva y soltar la Liana): el par de A* es el
            // de la búsqueda exhaustiva y la solución gana. Después del primer mordisco la cueva está llena y sellada, así que
            // los siguientes se pierden (respuesta Q-C).
            Cuia cave = Cuia.Create(4, new[] { 1, 1, 1 }).WithMechanic(MechanicType.Pirana, k: 2, counter: 1);
            Cuia liana = Cuia.Create(4, new[] { 2, 2 }).WithMechanic(MechanicType.Liana, key: 1);
            var start = new BoardState(new[] { cave, liana, Cuia.Create(4, new[] { 2, 1 }), Cuia.Create(4, new[] { 2 }), Cuia.Empty(4) });

            SolveResult result = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Run));

            Assert.That(result.IsSolved, Is.True);
            Assert.That(result.MoveCount, Is.EqualTo(3));
            Assert.That(ExhaustiveSearch.Par(start, PourMode.Run, true), Is.EqualTo(3));
            BoardState state = start;
            for (int i = 0; i < result.MoveCount; i++)
            {
                Pour pour = result.GetMove(i);
                state = PourRules.Apply(state, pour.Source, pour.Target, PourMode.Run);
            }

            Assert.That(state.IsWon(), Is.True);
        }

        /// <summary>Mordiscos en todas las aristas del grafo de estados alcanzables (para comprobar que el lote los ejercita).</summary>
        private static int CountBites(BoardState start, PourMode mode)
        {
            ExhaustiveSearch.Graph graph = ExhaustiveSearch.Explore(start, mode, MaxStates);
            int bites = 0;
            for (int s = 0; s < graph.States.Count; s++)
            {
                BoardState state = graph.States[s];
                for (int a = 0; a < state.Count; a++)
                {
                    for (int b = 0; b < state.Count; b++)
                    {
                        if (a != b && PourRules.IsLegal(state, a, b))
                        {
                            PourRules.Apply(state, a, b, mode, out PourEvents events);
                            bites += events.PiranaBittenMask != 0 ? 1 : 0;
                        }
                    }
                }
            }

            return bites;
        }
    }
}
