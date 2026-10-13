#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// Aguas estancadas por bucle (02 R13 ampliada; D-014, decisión del dueño del 2026-10-13): con vertidos útiles, la partida
    /// queda en <see cref="LevelStatus.Stuck"/> si los estados alcanzables son como mucho <see cref="DeadEndProbe.MaxStates"/>
    /// (64 [P]) y ninguno es victoria; si son más, o alguno gana, sigue en <see cref="LevelStatus.Playing"/> (conservador, R14).
    /// Regresión del nivel 66 (docs/dev/traspaso/pruebas-telefono-2026-10-13.md, «Defecto encontrado por el dueño»). Test
    /// compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class DeadEndProbeTests
    {
        /// <summary>
        /// Nivel 66 (<c>m03_v1_n0066</c>), tablero inicial copiado de <c>Assets/_Project/Data/Levels/mundo_03_v1.json</c> (los
        /// tests compartidos no leen los packs): 10 cuias de 4 salvo la 5 (Cuia chica de 2), Hoja con fruta 3 en la 6.
        /// </summary>
        private static readonly int[] Level66Caps = { 4, 4, 4, 4, 4, 2, 4, 4, 4, 4 };

        private static readonly int[][] Level66Start =
        {
            new[] { 3, 8, 10, 7 }, new[] { 4, 7, 4, 7 }, new[] { 10, 7, 4, 10 }, new[] { 14, 14, 14, 3 }, new[] { 6, 6, 6, 14 },
            new int[0], new int[0], new[] { 8, 8, 10, 6 }, new[] { 3, 3, 8, 1 }, new[] { 1, 1, 1, 4 },
        };

        /// <summary>Primer intento del dueño en el teléfono (logcat del 2026-10-13): 3>6 4>3 7>4 0>5 1>5 2>0 2>1 5>2 1>5 2>1 0>7 1>2.</summary>
        private static readonly int[][] Level66Loop =
        {
            new[] { 3, 6 }, new[] { 4, 3 }, new[] { 7, 4 }, new[] { 0, 5 }, new[] { 1, 5 }, new[] { 2, 0 },
            new[] { 2, 1 }, new[] { 5, 2 }, new[] { 1, 5 }, new[] { 2, 1 }, new[] { 0, 7 }, new[] { 1, 2 },
        };

        /// <summary>
        /// Tablero tras los 12 vertidos: 0 [3,8,10] · 1 [4,7] · 2 [10,7,7,7] · 3 sellada · 4 sellada · 5 (cap 2) [4,4] · 6 [3] ·
        /// 7 [8,8,10,10] · 8 [3,3,8,1] · 9 [1,1,1,4]; la Hoja ya se abrió (sin mecánicas).
        /// </summary>
        private static readonly int[][] Level66Final =
        {
            new[] { 3, 8, 10 }, new[] { 4, 7 }, new[] { 10, 7, 7, 7 }, new[] { 14, 14, 14, 14 }, new[] { 6, 6, 6, 6 },
            new[] { 4, 4 }, new[] { 3 }, new[] { 8, 8, 10, 10 }, new[] { 3, 3, 8, 1 }, new[] { 1, 1, 1, 4 },
        };

        /// <summary>Niveles 1-10 del tutorial (<c>Assets/_Project/Data/Levels/tutorial_v0.json</c>), todas las cuias de 4.</summary>
        private static readonly int[][][] Tutorial =
        {
            new[] { new[] { 8, 8, 8, 4 }, new[] { 4, 4, 4, 8 }, new int[0] },
            new[] { new[] { 4, 8, 4, 4 }, new[] { 3, 3, 3, 4 }, new[] { 3, 8, 8, 8 }, new int[0], new int[0] },
            new[] { new[] { 3, 3, 4, 4 }, new[] { 8, 3, 8, 3 }, new[] { 8, 4, 4, 8 }, new int[0], new int[0] },
            new[] { new[] { 8, 1, 8, 1 }, new[] { 4, 8, 8, 4 }, new[] { 3, 3, 4, 4 }, new[] { 1, 1, 3, 3 }, new int[0], new int[0] },
            new[] { new[] { 8, 4, 8, 8 }, new[] { 1, 4, 1, 1 }, new[] { 4, 3, 3, 4 }, new[] { 3, 1, 3, 8 }, new int[0], new int[0] },
            new[] { new[] { 8, 1, 4, 4 }, new[] { 3, 8, 8, 1 }, new[] { 4, 3, 3, 1 }, new[] { 1, 4, 8, 3 }, new int[0] },
            new[] { new[] { 8, 8, 7, 7 }, new[] { 3, 4, 4, 7 }, new[] { 1, 1, 8, 3 }, new[] { 3, 7, 8, 4 }, new[] { 4, 1, 1, 3 }, new int[0], new int[0] },
            new[] { new[] { 4, 4, 7, 7 }, new[] { 3, 1, 8, 7 }, new[] { 4, 4, 3, 1 }, new[] { 1, 8, 3, 7 }, new[] { 1, 3, 8, 8 }, new int[0], new int[0] },
            new[] { new[] { 1, 7, 7, 8 }, new[] { 4, 4, 8, 1 }, new[] { 3, 4, 1, 7 }, new[] { 7, 8, 4, 3 }, new[] { 3, 1, 3, 8 }, new int[0], new int[0] },
            new[] { new[] { 8, 4, 3, 1 }, new[] { 7, 8, 4, 3 }, new[] { 1, 7, 8, 4 }, new[] { 3, 1, 7, 8 }, new[] { 4, 3, 1, 7 }, new int[0], new int[0] },
        };

        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static BoardState Build(int[] caps, int[][] cuias, MechanicSpec[]? attrs)
        {
            BoardBuildResult built = BoardBuilder.Build(caps, cuias, attrs);
            Assert.That(built.Error, Is.Null);
            return built.State!;
        }

        private static BoardState Level66Initial()
        {
            var attrs = new MechanicSpec[Level66Caps.Length];
            attrs[6] = new MechanicSpec(MechanicType.Hoja, key: 3);
            return Build(Level66Caps, Level66Start, attrs);
        }

        private static BoardState Level66Stuck()
        {
            return Build(Level66Caps, Level66Final, null);
        }

        private static BoardState UniformBoard(int[][] cuias)
        {
            var caps = new int[cuias.Length];
            for (int i = 0; i < caps.Length; i++)
            {
                caps[i] = 4;
            }

            return Build(caps, cuias, null);
        }

        [Test]
        public void R13b_Level66_Loop_IsStuck()
        {
            // 02 R13 ampliada (D-014): quedan dos vertidos legales y útiles, 2 → 1 y 7 → 0, que son vaivenes parciales (los tres
            // 7 entre 1 y 2, el 10 entre 0 y 7). R13 sin ampliar no lo declara; el cierre tiene pocos estados y ninguno gana.
            BoardState state = Level66Stuck();
            var useful = new Pour[PourRules.MaxPours];

            int count = PourRules.CollectUsefulMoves(state, true, PourMode.Run, useful);
            SolveResult proof = new AStarSolver().Solve(state, new SolverOptions(SolverOptions.PipelineNodeBudget, 1, false, PourMode.Run));
            var probe = new DeadEndProbe();

            Assert.That(count, Is.EqualTo(2));
            Assert.That(useful[0], Is.EqualTo(new Pour(2, 1, 2)));
            Assert.That(useful[1], Is.EqualTo(new Pour(7, 0, 1)));
            Assert.That(PourRules.IsStuck(state, true), Is.False);
            Assert.That(proof.Status, Is.EqualTo(SolveStatus.Unsolvable));
            Assert.That(probe.IsDeadEnd(state, PourMode.Run), Is.True);
            Assert.That(probe.LastStateCount, Is.InRange(2, DeadEndProbe.MaxStates));
            Assert.That(new LevelSession(state, true, PourMode.Run).Status, Is.EqualTo(LevelStatus.Stuck));
        }

        [Test]
        public void R13b_Level66_TheOwnersSequence_IsStuckRightAfterTheNinthPour()
        {
            // Reproducción completa del nivel 66. El 9.º vertido (1 → 5) deja el nivel perdido de verdad: desde ahí solo se
            // alcanzan 4 estados (los vertidos 10-12 del dueño ya eran vueltas del bucle) y la búsqueda con todos los vertidos
            // legales lo confirma. Aguas estancadas sale justo tras él y no antes; el resto de la secuencia ya no entra (R15).
            BoardState initial = Level66Initial();
            Assert.That(LevelValidation.Validate(initial), Is.Null);
            var session = new LevelSession(initial, true, PourMode.Run);
            var solver = new AStarSolver();
            var allLegal = new SolverOptions(SolverOptions.PipelineNodeBudget, 1, false, PourMode.Run);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));

            for (int i = 0; i < 9; i++)
            {
                Assert.That(solver.Solve(session.Current, allLegal).IsSolved, Is.True, "antes del vertido " + (i + 1));
                bool ok = session.TryPour(Level66Loop[i][0], Level66Loop[i][1], out PourResult result);

                Assert.That(ok, Is.True, "vertido " + (i + 1));
                LevelStatus expected = i == 8 ? LevelStatus.Stuck : LevelStatus.Playing;
                Assert.That(session.Status, Is.EqualTo(expected), "tras el vertido " + (i + 1));
                Assert.That(result.Status, Is.EqualTo(expected), "tras el vertido " + (i + 1));
            }

            Assert.That(solver.Solve(session.Current, allLegal).Status, Is.EqualTo(SolveStatus.Unsolvable));
            for (int i = 9; i < Level66Loop.Length; i++)
            {
                Assert.That(session.TryPour(Level66Loop[i][0], Level66Loop[i][1], out _), Is.False, "vertido " + (i + 1));
            }

            Assert.That(session.AppliedPours, Is.EqualTo(9));
        }

        [Test]
        public void R13b_Level66_AWinnableStateWhoseUsefulClosureHasNoWin_IsPlaying()
        {
            // Por qué el cierre recorre también los fútiles (R14; CHANGELOG del núcleo, C1-8): tras los 6 primeros vertidos del
            // dueño el nivel se gana en 14, pero solo con algún fútil (Cuia chica de 2). Con solo los vertidos útiles (la poda del
            // solver) no hay victoria en 50 estados: un cierre limitado a los útiles habría declarado Aguas estancadas aquí.
            var session = new LevelSession(Level66Initial(), true, PourMode.Run);
            for (int i = 0; i < 6; i++)
            {
                session.TryPour(Level66Loop[i][0], Level66Loop[i][1], out _);
            }

            var solver = new AStarSolver();
            SolveResult useful = solver.Solve(session.Current, new SolverOptions(SolverOptions.PipelineNodeBudget, 1, true, PourMode.Run));
            Assert.That(useful.Status, Is.EqualTo(SolveStatus.Unsolvable));
            Assert.That(useful.Expanded, Is.LessThanOrEqualTo(DeadEndProbe.MaxStates));
            SolveResult all = solver.Solve(session.Current, new SolverOptions(SolverOptions.PipelineNodeBudget, 1, false, PourMode.Run));

            Assert.That(all.IsSolved, Is.True);
            Assert.That(all.MoveCount, Is.EqualTo(14));
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
        }

        [Test]
        public void R13b_Level66_MareaAndRestart_ReevaluateTheLoop()
        {
            // 04 §2.1 y §2.7 con D-014: Marea desde el bloqueo por bucle vuelve a Playing (el estado se evalúa otra vez), el
            // mismo vertido lo vuelve a declarar y Volver a la orilla parte de un tablero que se gana.
            var session = new LevelSession(Level66Initial(), true, PourMode.Run);
            for (int i = 0; i < 9; i++)
            {
                session.TryPour(Level66Loop[i][0], Level66Loop[i][1], out _);
            }

            Assert.That(session.Status, Is.EqualTo(LevelStatus.Stuck));
            Assert.That(PourRules.IsStuck(session.Current, true), Is.False);
            Assert.That(session.TryPour(2, 1, out _), Is.False); // R15: input bloqueado aunque 2 → 1 sea útil

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(session.TryPour(1, 5, out PourResult again), Is.True);
            Assert.That(again.Status, Is.EqualTo(LevelStatus.Stuck));

            session.Restart();
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
        }

        [Test]
        public void R13b_TheVerdictIsCanonical_AnyOrderOfTheCuias()
        {
            // Determinismo (CLAUDE.md, regla dura 1): estados canónicos, así que el veredicto y el tamaño del cierre no dependen
            // del orden de las cuias en pantalla.
            BoardState state = Level66Stuck();
            var reversed = new Cuia[state.Count];
            for (int i = 0; i < reversed.Length; i++)
            {
                reversed[i] = state[state.Count - 1 - i];
            }

            var probe = new DeadEndProbe();
            bool first = probe.IsDeadEnd(state, PourMode.Run);
            int states = probe.LastStateCount;
            bool second = probe.IsDeadEnd(new BoardState(reversed), PourMode.Run);

            Assert.That(first, Is.True);
            Assert.That(second, Is.True);
            Assert.That(probe.LastStateCount, Is.EqualTo(states));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        public void R13b_WinnableTutorialBoard_IsPlayingAlongItsSolution(int index)
        {
            // R14 con D-014: un tablero que se gana nunca se declara. Tras cada vertido de la solución A* (con sus vaivenes
            // posibles), la partida sigue en Playing hasta la victoria.
            BoardState start = UniformBoard(Tutorial[index]);
            SolveResult solution = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Run));
            Assert.That(solution.IsSolved, Is.True);
            var session = new LevelSession(start, true, PourMode.Run);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));

            for (int i = 0; i < solution.MoveCount; i++)
            {
                Pour pour = solution.GetMove(i);
                Assert.That(session.TryPour(pour.Source, pour.Target, out _), Is.True);
                LevelStatus expected = i == solution.MoveCount - 1 ? LevelStatus.Won : LevelStatus.Playing;
                Assert.That(session.Status, Is.EqualTo(expected), "nivel " + (index + 1) + ", vertido " + (i + 1));
            }
        }

        [Test]
        public void R13b_ClosureOfMoreThan64States_IsNotDeclared()
        {
            // Conservador (D-014): el 1 nunca sella (U_1 = 3 y todas las cuias son de 4), así que el tablero está perdido, pero
            // los otros tres colores dan más de 64 estados: no se declara y el recorrido para al llenarse.
            BoardState state = new BoardState(new[]
            {
                C(4, 3, 4, 5, 3), C(4, 4, 5, 3, 4), C(4, 5, 3, 4, 5), C(4, 1, 1, 1), Cuia.Empty(4), Cuia.Empty(4),
            });
            var probe = new DeadEndProbe();

            SolveResult proof = new AStarSolver().Solve(state, new SolverOptions(SolverOptions.PipelineNodeBudget, 1, false, PourMode.Run));

            Assert.That(proof.Status, Is.EqualTo(SolveStatus.Unsolvable));
            Assert.That(proof.Expanded, Is.GreaterThan(DeadEndProbe.MaxStates));
            Assert.That(probe.IsDeadEnd(state, PourMode.Run), Is.False);
            Assert.That(probe.LastStateCount, Is.EqualTo(DeadEndProbe.MaxStates));
            Assert.That(new LevelSession(state, true, PourMode.Run).Status, Is.EqualTo(LevelStatus.Playing));
        }

        [Test]
        public void R13b_TheClosureWalksFutilePoursToo_SoAWinnableMixedBoardIsNotDeclared()
        {
            // R14 con capacidades mixtas (CHANGELOG del núcleo, C1-8): la única salida empieza por fútiles. Con solo los
            // vertidos útiles (la poda del solver) no hay camino a la victoria y el cierre útil es pequeño; si el cierre de
            // D-014 se quedara en los útiles, declararía perdido un tablero que se gana en 4. Por eso recorre todos los legales.
            BoardState state = new BoardState(new[] { C(6, 1, 2, 2, 2, 2), C(4, 1, 1), Cuia.Empty(2), C(4, 1, 1, 1) });
            var solver = new AStarSolver();

            SolveResult pruned = solver.Solve(state, new SolverOptions(SolverOptions.PipelineNodeBudget, 1, true, PourMode.Run));
            Assert.That(pruned.Status, Is.EqualTo(SolveStatus.Unsolvable));
            Assert.That(pruned.Expanded, Is.LessThanOrEqualTo(DeadEndProbe.MaxStates));
            SolveResult full = solver.Solve(state, new SolverOptions(SolverOptions.PipelineNodeBudget, 1, false, PourMode.Run));

            Assert.That(full.IsSolved, Is.True);
            Assert.That(full.MoveCount, Is.EqualTo(4));
            Assert.That(PourRules.IsStuck(state, true), Is.False);
            Assert.That(new DeadEndProbe().IsDeadEnd(state, PourMode.Run), Is.False);
            Assert.That(new LevelSession(state, true, PourMode.Run).Status, Is.EqualTo(LevelStatus.Playing));
        }

        [Test]
        public void R13b_MurkyWater_UsesTheTrueHiddenDoses()
        {
            // 02 §11 con D-014: el núcleo conoce las dosis ocultas y el cierre usa el estado verdadero. Los dos tableros se ven
            // igual ([?, 1, 1, 1], [2, 2, 2], Cuia chica de 3 vacía); con un 2 oculto se gana, con un 3 oculto está perdido de
            // verdad (U_3 = 1 y ninguna cuia es de 1: el 3 no sella nunca).
            var winnable = new BoardState(new[] { Cuia.Create(4, new[] { 2, 1, 1, 1 }, 0x1), C(4, 2, 2, 2), Cuia.Empty(3) });
            var lost = new BoardState(new[] { Cuia.Create(4, new[] { 3, 1, 1, 1 }, 0x1), C(4, 2, 2, 2), Cuia.Empty(3) });

            Assert.That(PourRules.IsStuck(winnable, true), Is.False);
            Assert.That(PourRules.IsStuck(lost, true), Is.False);
            Assert.That(new LevelSession(winnable, true, PourMode.Run).Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(new LevelSession(lost, true, PourMode.Run).Status, Is.EqualTo(LevelStatus.Stuck));
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void R13b_WonBoard_IsNeverADeadEnd(PourMode mode)
        {
            // R10: la victoria (g) va antes que el bloqueo (h); el cierre tampoco declara un tablero ya ganado.
            BoardState won = new BoardState(new[] { C(4, 1, 1, 1, 1), Cuia.Empty(4) });

            Assert.That(new DeadEndProbe().IsDeadEnd(won, mode), Is.False);
            Assert.That(new LevelSession(won, true, mode).Status, Is.EqualTo(LevelStatus.Won));
        }
    }
}
