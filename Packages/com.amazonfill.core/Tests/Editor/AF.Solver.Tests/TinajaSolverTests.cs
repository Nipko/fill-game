#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Solver.Bot;
using AF.Solver.Murky;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Solver.Tests
{
    /// <summary>
    /// Tinaja en AF.Solver (M13; D-019; propuesta de la tinaja, b.2 R14 y «Morpho», c.2 y c.6): A* con y sin poda de fútiles
    /// frente a la búsqueda exhaustiva (con el contraejemplo de b.2), heurística admisible y consistente, búsquedas sin poda
    /// en niveles con tinaja, suelta igual en todas las realidades (Q14 A), el orden de respuestas de Morpho
    /// (<see cref="SafeMoves.Outcome"/>) y la rama mínima del bot. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class TinajaSolverTests
    {
        /// <summary>Tableros de como mucho 5.000 estados en single, para que el lote sea rápido también en Unity.</summary>
        private const int MaxStates = 5000;

        /// <summary><c>hint_budget_nodes</c> y peso de Morpho (03 §5; 08 §9).</summary>
        private static readonly SolverOptions Morpho = new SolverOptions(40000, 2, true, PourMode.Run);

        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static Cuia T(int hiddenMask, params int[] colorsBottomToTop)
        {
            return Cuia.Create(LevelValidation.TinajaCapacity, colorsBottomToTop, hiddenMask).WithMechanic(MechanicType.Tinaja);
        }

        private static Cuia Ch(params int[] colorsBottomToTop)
        {
            return Cuia.Create(LevelValidation.CucharonCapacity, colorsBottomToTop).WithMechanic(MechanicType.Cucharon);
        }

        private static BoardState Board(params Cuia[] cuias)
        {
            return new BoardState(cuias);
        }

        /// <summary>S0 de b.2: N[c,c,a] N[a,b,a] N[b,b,b] T[c(o),a(o),c] Ch[] (a = 1, b = 2, c = 3; tinaja 3, su cuia 4).</summary>
        private static BoardState DebutS0()
        {
            return Board(C(4, 3, 3, 1), C(4, 1, 2, 1), C(4, 2, 2, 2), T(0x3, 3, 1, 3), Ch());
        }

        /// <summary>
        /// Tablero perdido de b.2 («Por qué hace falta el orden»): N[c,c,c] N[a,b,a,a] N[b,b,b] T[c(o),a] Ch[]. Se llega desde
        /// S0 con un desvío legal de 3 vertidos; el único vertido legal es sacar y solo queda 1 realidad, perdida.
        /// </summary>
        private static BoardState LostBoard()
        {
            return Board(C(4, 3, 3, 3), C(4, 1, 2, 1, 1), C(4, 2, 2, 2), T(0x1, 3, 1), Ch());
        }

        /// <summary>
        /// Contraejemplo alcanzable de b.2 R14 (a-d = 1-4, U = 4), tras la suelta: N1[b,d,c,a] N2[b,a,a,a] N3[b,d] N4[b]
        /// N5[c,c,c] T[] (inerte) ch[d,d] (ya una Cuia chica normal).
        /// </summary>
        private static BoardState Counterexample()
        {
            return Board(C(4, 2, 4, 3, 1), C(4, 2, 1, 1, 1), C(4, 2, 4), C(4, 2), C(4, 3, 3, 3), T(0), C(2, 4, 4));
        }

        private static List<BoardState> Boards()
        {
            List<BoardState> boards = TinajaBoards.Generate(0x7134_0001UL, 12, false, MaxStates);
            boards.AddRange(TinajaBoards.Generate(0x7134_0002UL, 12, true, MaxStates));
            return boards;
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void M13_AStarWithAndWithoutPruning_MatchTheExhaustiveSearch_R14(PourMode mode)
        {
            // b.2 R14 y c.6: A* con poda frente a la búsqueda exhaustiva en tableros con tinaja. Sin poda, A* da el par exacto;
            // con poda, el de la búsqueda exhaustiva con los mismos movimientos útiles (puede ser más largo o no existir, nunca
            // más corto: C1-8).
            List<BoardState> boards = Boards();
            for (int i = 0; i < boards.Count; i++)
            {
                MechanicBoards.AssertAStarMatchesExhaustiveSearch(boards[i], mode, "tinaja " + i + " " + boards[i]);
            }
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void M13_Heuristic_IsAdmissibleAndConsistent_039(PourMode mode)
        {
            // c.2: mezclar en su cuia solo puede añadir cortes, así que H sigue siendo admisible; también consistente, incluido
            // el vertido que la suelta (todos los estados alcanzables de los tableros pequeños).
            List<BoardState> boards = Boards();
            for (int i = 0; i < boards.Count; i++)
            {
                MechanicBoards.AssertHeuristicAdmissibleAndConsistent(boards[i], mode, "tinaja " + i + " " + boards[i]);
            }
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void R14_ThePrunedSearchMissesTheCounterexampleOfB2_TheUnprunedOneWinsIn10(PourMode mode)
        {
            // b.2 R14 (C1-8): todo nivel con tinaja acaba con capacidades mixtas; en el contraejemplo, N4→ch (monocolor de 4 a una
            // vacía de 2) es fútil según 02 §1.4 y hace falta para ganar. A* con poda devuelve «irresoluble»; sin poda, 10.
            BoardState state = Counterexample();
            var solver = new AStarSolver();

            Assert.That(solver.Solve(state, SolverOptions.ForPipeline(mode)).Status, Is.EqualTo(SolveStatus.Unsolvable));
            SolveResult unpruned = solver.Solve(state, SolverOptions.ForPipeline(mode, state));
            Assert.That(unpruned.Status, Is.EqualTo(SolveStatus.Solved));
            if (mode == PourMode.Run)
            {
                Assert.That(unpruned.MoveCount, Is.EqualTo(10));
                BoardState first = PourRules.Apply(state, 6, 2, PourMode.Run); // ch→N3 deja vacía la Cuia chica
                Assert.That(PourRules.IsFutile(first, 3, 6), Is.True); // N4→ch: monocolor de 4 a una vacía de 2
            }

            Assert.That(PourRules.IsStuck(state, true), Is.False); // P1 se cumple: el juego no declara bloqueo
        }

        [Test]
        public void R14_TheCounterexampleOfB2_IsReachable()
        {
            // b.2: «Se llega, por ejemplo, desde N1[b,d,c,a] N2[b,a,a,a] N3[b,d] N4[b] N5[c,c] T[d,c,d] con su cuia vacía,
            // sacando d, sacando c, Ch→N5 y sacando d».
            BoardState state = Board(C(4, 2, 4, 3, 1), C(4, 2, 1, 1, 1), C(4, 2, 4), C(4, 2), C(4, 3, 3), T(0x3, 4, 3, 4), Ch());
            state = PourRules.Apply(state, 5, 6, PourMode.Run);
            state = PourRules.Apply(state, 5, 6, PourMode.Run);
            state = PourRules.Apply(state, 6, 4, PourMode.Run);
            state = PourRules.Apply(state, 5, 6, PourMode.Run);

            Assert.That(state.Equals(Counterexample()), Is.True, state.ToString());
        }

        [Test]
        public void R14_SafeMoves_InTheCounterexample_NoneWithPruning_OneWithout()
        {
            // b.2 R14: «SafeMoves con ForPipeline da 0 vertidos seguros y sin poda da 1». Con poda, además, Morpho diría «no hay
            // salida» cuando la hay: por eso las búsquedas de un nivel con tinaja van sin poda.
            BoardState state = Counterexample();
            var safe = new SafeMoves();

            Assert.That(safe.Find(state, SafeMoves.ForPipeline(PourMode.Run)), Is.EqualTo(RealityStatus.Ok));
            Assert.That(safe.Count, Is.EqualTo(0));
            Assert.That(safe.Outcome, Is.EqualTo(SafeMovesOutcome.NoExit));

            Assert.That(safe.Find(state, SafeMoves.ForPipeline(PourMode.Run, state)), Is.EqualTo(RealityStatus.Ok));
            Assert.That(safe.Count, Is.EqualTo(1));
            Assert.That(safe.Outcome, Is.EqualTo(SafeMovesOutcome.Found));
            Assert.That(safe.GetMove(0).Source, Is.EqualTo(6)); // ch→N3
            Assert.That(safe.GetMove(0).Target, Is.EqualTo(2));
        }

        [Test]
        public void SolverOptions_ForLevel_DisablesPruningOnlyInLevelsWithTinaja_c2()
        {
            // c.2: en niveles con tinaja todas las búsquedas que deciden «hay solución» van sin poda; en los demás, nada cambia.
            BoardState plain = Board(C(4, 1, 2), C(4, 2, 1), Cuia.Empty(4));
            BoardState inert = Counterexample(); // la tinaja vacía sigue en su ranura

            Assert.That(SolverOptions.RequiresUnprunedSearch(DebutS0()), Is.True);
            Assert.That(SolverOptions.RequiresUnprunedSearch(inert), Is.True);
            Assert.That(SolverOptions.RequiresUnprunedSearch(plain), Is.False);
            Assert.That(SolverOptions.ForPipeline(PourMode.Run, DebutS0()).FutileRuleEnabled, Is.False);
            Assert.That(SafeMoves.ForPipeline(PourMode.Single, DebutS0()).FutileRuleEnabled, Is.False);
            Assert.That(Morpho.ForLevel(DebutS0()).FutileRuleEnabled, Is.False);
            Assert.That(Morpho.ForLevel(DebutS0()).NodeBudget, Is.EqualTo(Morpho.NodeBudget));
            Assert.That(Morpho.ForLevel(DebutS0()).Weight, Is.EqualTo(Morpho.Weight));

            SolverOptions same = SolverOptions.ForPipeline(PourMode.Single, plain);
            Assert.That(same.FutileRuleEnabled, Is.True);
            Assert.That(same.NodeBudget, Is.EqualTo(SolverOptions.PipelineNodeBudget));
            Assert.That(same.PourMode, Is.EqualTo(PourMode.Single));
            Assert.That(SafeMoves.ForPipeline(PourMode.Run, plain).NodeBudget, Is.EqualTo(SafeMoves.PipelineRealityBudget));
            Assert.That(SafeMoves.ForPipeline(PourMode.Run, plain).FutileRuleEnabled, Is.True);
        }

        [Test]
        public void SolverOptions_ForLevel_NullLevel_ThrowsWithItsParamName()
        {
            // La excepción nombra el parámetro que documenta cada API: level en ForLevel y en los dos ForPipeline(mode, level).
            BoardState? none = null;

            Assert.That(() => Morpho.ForLevel(none!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("level"));
            Assert.That(() => SolverOptions.ForPipeline(PourMode.Run, none!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("level"));
            Assert.That(() => SafeMoves.ForPipeline(PourMode.Run, none!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("level"));
            Assert.That(() => SolverOptions.RequiresUnprunedSearch(none!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("state"));
        }

        [Test]
        public void Cucharon_ReleaseSameInAllRealities_Q14A()
        {
            // b.2 «Combinaciones» y Q-Ti1 A: la suelta depende solo del recuento visible («×n»), nunca del color de una oculta,
            // así que ocurre en el mismo vertido en todas las realidades y la premisa de SafeMoves se mantiene.
            BoardState state = Board(
                C(4, 1, 1, 1), C(4, 2, 2, 2), C(4, 3, 3, 3), C(4, 4, 4, 4), T(0x7, 1, 2, 3, 4), Ch(), Cuia.Empty(4), Cuia.Empty(4));
            int[][] pours = { new[] { 4, 5 }, new[] { 4, 5 }, new[] { 5, 6 }, new[] { 5, 7 }, new[] { 4, 5 }, new[] { 4, 5 } };
            var realities = new RealityEnumerator();
            Assert.That(realities.Enumerate(state), Is.EqualTo(RealityStatus.Ok));
            Assert.That(realities.Count, Is.EqualTo(6));

            for (int r = 0; r < realities.Count; r++)
            {
                BoardState reality = realities.GetReality(r);
                for (int p = 0; p < pours.Length; p++)
                {
                    Assert.That(PourRules.IsLegal(reality, pours[p][0], pours[p][1]), Is.True, "realidad " + r + ", vertido " + p);
                    reality = PourRules.Apply(reality, pours[p][0], pours[p][1], PourMode.Run, out PourEvents events);
                    Assert.That(events.Amount, Is.EqualTo(1));
                    Assert.That(events.CucharonReleasedMask, Is.EqualTo(p == pours.Length - 1 ? 1 << 5 : 0), "realidad " + r + ", vertido " + p);
                }

                Assert.That(reality[5].Mechanic, Is.EqualTo(MechanicType.None));
                Assert.That(reality[4].IsEmpty, Is.True);
            }
        }

        [Test]
        public void Morpho_TheLostBoardOfB2_SaysNoExit_AndNeverTheFireflyWithOneReality()
        {
            // b.2 «Morpho», respuesta 2: si ninguna realidad tiene solución (también con R = 1, cuando ya no queda nada que ver),
            // «Desde aquí no hay salida» y no cobra. Nunca la Luciérnaga con R = 1. Aguas estancadas no sale (sacar está exento).
            BoardState lost = LostBoard();
            var safe = new SafeMoves();

            Assert.That(safe.Find(lost, Morpho.ForLevel(lost)), Is.EqualTo(RealityStatus.Ok));
            Assert.That(safe.RealityCount, Is.EqualTo(1));
            Assert.That(safe.Count, Is.EqualTo(0));
            Assert.That(safe.Outcome, Is.EqualTo(SafeMovesOutcome.NoExit));
            Assert.That(PourRules.IsStuck(lost, true), Is.False);

            // Se llega desde S0 con 3 vertidos legales: sacar, N1→N2 y Ch→N1.
            BoardState state = DebutS0();
            state = PourRules.Apply(state, 3, 4, PourMode.Run);
            state = PourRules.Apply(state, 0, 1, PourMode.Run);
            state = PourRules.Apply(state, 4, 0, PourMode.Run);
            Assert.That(state.Equals(lost), Is.True, state.ToString());
        }

        [Test]
        public void Morpho_TheDebutS0OfB2_HasSafeMoves_AndTheSafeRouteWins()
        {
            // b.2 «Morpho», respuesta 1, y c.8 (ruta segura, 03 §8): S0 tiene ruta segura; la de b.2 tiene 8 vertidos.
            BoardState s0 = DebutS0();
            var safe = new SafeMoves();
            Assert.That(safe.Find(s0, Morpho.ForLevel(s0)), Is.EqualTo(RealityStatus.Ok));
            Assert.That(safe.Count, Is.GreaterThan(0));
            Assert.That(safe.Outcome, Is.EqualTo(SafeMovesOutcome.Found));

            SafePathResult path = new SafePath().Find(s0, SafeMoves.ForPipeline(PourMode.Run, s0));
            SolveResult par = new AStarSolver().Solve(s0, SolverOptions.ForPipeline(PourMode.Run, s0));
            Assert.That(path.IsOk, Is.True);
            Assert.That(path.NoSafeMoveOutcome, Is.EqualTo(SafeMovesOutcome.Found));
            Assert.That(path.MoveCount, Is.EqualTo(par.MoveCount));

            int[][] route = { new[] { 1, 0 }, new[] { 1, 2 }, new[] { 0, 1 }, new[] { 3, 4 }, new[] { 3, 4 }, new[] { 4, 1 }, new[] { 3, 4 }, new[] { 4, 0 } };
            BoardState state = s0;
            for (int i = 0; i < route.Length; i++)
            {
                Assert.That(PourRules.IsLegal(state, route[i][0], route[i][1]), Is.True, "vertido " + i);
                state = PourRules.Apply(state, route[i][0], route[i][1], PourMode.Run);
            }

            Assert.That(state.IsWon(), Is.True);
            Assert.That(par.MoveCount, Is.LessThanOrEqualTo(route.Length));
        }

        [Test]
        public void Morpho_SomeRealitySolvable_ButNoSafePour_SuggestsTheFirefly()
        {
            // b.2 «Morpho», respuesta 3 (vale también para Aguas turbias, c.10): alguna realidad tiene solución, pero ningún
            // vertido es seguro en todas. Tablero de MurkyTests: dos realidades; la verdadera tiene solución (par 4).
            BoardState state = Board(Cuia.Create(3, new[] { 3, 3, 1 }, 0x2), Cuia.Create(3, new[] { 2, 1 }, 0x1), C(3, 3, 2, 2), C(3, 1));
            var safe = new SafeMoves();

            Assert.That(safe.Find(state, SafeMoves.ForPipeline(PourMode.Run)), Is.EqualTo(RealityStatus.Ok));
            Assert.That(safe.RealityCount, Is.EqualTo(2));
            Assert.That(safe.Count, Is.EqualTo(0));
            Assert.That(safe.Outcome, Is.EqualTo(SafeMovesOutcome.SomeRealitySolvable));

            SafePathResult path = new SafePath().Find(state, SafeMoves.ForPipeline(PourMode.Run));
            Assert.That(path.Status, Is.EqualTo(SafePathStatus.NoSafeMove));
            Assert.That(path.NoSafeMoveOutcome, Is.EqualTo(SafeMovesOutcome.SomeRealitySolvable));
        }

        [Test]
        public void Morpho_OutOfBudget_SaysItCannotFindAPath()
        {
            // b.2 «Morpho», respuesta 4: si se agota el presupuesto antes de decidir, «No encuentro un camino desde aquí» (y no
            // cobra). Con 1 nodo por realidad no se demuestra nada.
            BoardState state = Board(Cuia.Create(3, new[] { 3, 3, 1 }, 0x2), Cuia.Create(3, new[] { 2, 1 }, 0x1), C(3, 3, 2, 2), C(3, 1));
            var safe = new SafeMoves();

            Assert.That(safe.Find(state, new SolverOptions(1, 1, true, PourMode.Run)), Is.EqualTo(RealityStatus.Ok));
            Assert.That(safe.Count, Is.EqualTo(0));
            Assert.That(safe.Outcome, Is.EqualTo(SafeMovesOutcome.BudgetExceeded));
        }

        [Test]
        public void Morpho_WonBoard_AndRejectedRealities()
        {
            var safe = new SafeMoves();

            Assert.That(safe.Find(Board(C(4, 1, 1, 1, 1), Cuia.Empty(4)), SafeMoves.ForPipeline(PourMode.Run)), Is.EqualTo(RealityStatus.Ok));
            Assert.That(safe.Outcome, Is.EqualTo(SafeMovesOutcome.Won));

            BoardState tooMany = Board(Cuia.Create(4, new[] { 1, 2, 3, 9 }, 0x7), Cuia.Create(4, new[] { 4, 5, 10 }, 0x3), Cuia.Empty(4));
            Assert.That(safe.Find(tooMany, SafeMoves.ForPipeline(PourMode.Run)), Is.EqualTo(RealityStatus.TooMany));
            Assert.That(safe.Outcome, Is.EqualTo(SafeMovesOutcome.RealitiesRejected));
        }

        [Test]
        public void SafePath_ReportsNoExit_OnTheLostBoardOfB2_c3()
        {
            // c.3: el informe distingue «sin vertido seguro» de «presupuesto agotado».
            BoardState lost = LostBoard();

            SafePathResult path = new SafePath().Find(lost, SafeMoves.ForPipeline(PourMode.Run, lost));

            Assert.That(path.Status, Is.EqualTo(SafePathStatus.NoSafeMove));
            Assert.That(path.NoSafeMoveOutcome, Is.EqualTo(SafeMovesOutcome.NoExit));
        }

        [Test]
        public void CasualBot_MinimalBranch_JoinsOnlyIfTheCuiaTopIsTheDrawnFruit_c2()
        {
            // c.2 (Q-Ti2 A; PUNTUACION_CASUAL de 03 §6 ×4, el resto congelado): su cuia recibe cualquier fruta, así que el +16
            // de «juntar» (y el +40 que cuelga de él) solo suma si su cima es de la fruta sacada; sacar a la cuia vacía puntúa
            // 4 − 20 = −16.
            Assert.That(ScoreOfDrawing(Ch(1)), Is.EqualTo(4 + 16 + 40));
            Assert.That(ScoreOfDrawing(Ch(3)), Is.EqualTo(4));
            Assert.That(ScoreOfDrawing(Ch()), Is.EqualTo(-16));

            // Un destino normal sigue igual: juntar con el mismo color (el 3 que deja expuesto no tiene destino: sin +8).
            BoardState state = Board(T(0x1, 2, 1), Ch(2), C(4, 3, 1), C(4, 1));
            var board = new BoardSlice(state.CuiasArray, 0, state.Count, state.UnitsTable);
            Assert.That(CasualBot.Score(in board, new SolverMove(2, 3, 1, false)), Is.EqualTo(4 + 16));
        }

        [Test]
        public void CasualBot_PlaysTinajaBoards_Reproducibly_0142()
        {
            BoardState s0 = DebutS0();

            BotResult first = new CasualBot().FailRate(s0, 7UL, PourMode.Run);
            BotResult second = new CasualBot().FailRate(s0, 7UL, PourMode.Run);

            Assert.That(second.Failures, Is.EqualTo(first.Failures));
            Assert.That(first.Runs, Is.EqualTo(BotOptions.DefaultRuns));
        }

        /// <summary>Puntuación del bot al sacar la boca (1) de la tinaja [2*, 1] a su cuia <paramref name="cuia"/>.</summary>
        private static int ScoreOfDrawing(Cuia cuia)
        {
            BoardState state = Board(T(0x1, 2, 1), cuia, C(4, 3, 1), C(4, 1));
            var board = new BoardSlice(state.CuiasArray, 0, state.Count, state.UnitsTable);
            return CasualBot.Score(in board, new SolverMove(0, 1, 1, false));
        }
    }
}
