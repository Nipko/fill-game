#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
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
    /// Regresión del nivel 66 (docs/dev/traspaso/pruebas-telefono-2026-10-13.md, «Defecto encontrado por el dueño»; tablero y
    /// secuencia en <see cref="Level66Board"/>). Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class DeadEndProbeTests
    {
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

        private static BoardState Build(int[] caps, int[][] cuias)
        {
            BoardBuildResult built = BoardBuilder.Build(caps, cuias, null);
            Assert.That(built.Error, Is.Null);
            return built.State!;
        }

        private static BoardState UniformBoard(int[][] cuias)
        {
            var caps = new int[cuias.Length];
            for (int i = 0; i < caps.Length; i++)
            {
                caps[i] = 4;
            }

            return Build(caps, cuias);
        }

        [Test]
        public void R13b_Level66_Loop_IsStuck()
        {
            // 02 R13 ampliada (D-014): quedan dos vertidos legales y útiles, 2 → 1 y 7 → 0, que son vaivenes parciales (los tres
            // 7 entre 1 y 2, el 10 entre 0 y 7). R13 sin ampliar no lo declara; el cierre tiene pocos estados y ninguno gana.
            BoardState state = Level66Board.Final();
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

            // Añadido de D-014 del 2026-11-06: perdido no basta; el modal espera a que todo vertido lleve a un tablero ya visto.
            // Añadido del 2026-11-17: probar cada vertido y deshacerlo con Marea no agota el tablero (lo deshecho deja de estar
            // visto); recorrerlo sin Marea, sí.
            var session = new LevelSession(state, true, PourMode.Run);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(StagnationOracle.TryEveryPourAndUndo(session), Is.EqualTo(LevelStatus.Playing));
            Assert.That(session.Current.Equals(state), Is.True);
            Assert.That(session.SeenBoardCount, Is.EqualTo(1), "solo el tablero en pantalla");
            Assert.That(StagnationOracle.WalkUntilStuck(session), Is.EqualTo(LevelStatus.Stuck));
        }

        [Test]
        public void R13b_Level66_TheOwnersSequence_IsStuckRightAfterTheTwelfthPour()
        {
            // Reproducción completa del nivel 66. El 9.º vertido (1 → 5) deja el nivel perdido de verdad: desde ahí solo se
            // alcanzan 4 estados y la búsqueda con todos los vertidos legales lo confirma. Con D-014 el modal salía justo tras
            // él; con su añadido del 2026-11-06 (estancado solo al repetir) espera a que todo vertido lleve a un tablero ya
            // visto: los vertidos 10-12 del dueño eran vueltas del bucle y Aguas estancadas sale justo tras el 12.º, no antes.
            BoardState initial = Level66Board.Initial();
            Assert.That(LevelValidation.Validate(initial), Is.Null);
            var session = new LevelSession(initial, true, PourMode.Run);
            var solver = new AStarSolver();
            var probe = new DeadEndProbe();
            var allLegal = new SolverOptions(SolverOptions.PipelineNodeBudget, 1, false, PourMode.Run);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));

            for (int i = 0; i < Level66Board.PourCount; i++)
            {
                if (i < 9)
                {
                    Assert.That(solver.Solve(session.Current, allLegal).IsSolved, Is.True, "antes del vertido " + (i + 1));
                }

                bool ok = session.TryPour(Level66Board.Source(i), Level66Board.Target(i), out PourResult result);

                Assert.That(ok, Is.True, "vertido " + (i + 1));
                Assert.That(probe.IsDeadEnd(session.Current, PourMode.Run), Is.EqualTo(i >= 8), "perdido tras el vertido " + (i + 1));
                LevelStatus expected = i == Level66Board.PourCount - 1 ? LevelStatus.Stuck : LevelStatus.Playing;
                Assert.That(session.Status, Is.EqualTo(expected), "tras el vertido " + (i + 1));
                Assert.That(result.Status, Is.EqualTo(expected), "tras el vertido " + (i + 1));
            }

            Assert.That(session.Current.Equals(Level66Board.Final()), Is.True);
            Assert.That(solver.Solve(session.Current, allLegal).Status, Is.EqualTo(SolveStatus.Unsolvable));
            Assert.That(session.TryPour(2, 1, out _), Is.False, "R15: input bloqueado aunque 2 → 1 sea útil");
            Assert.That(session.AppliedPours, Is.EqualTo(Level66Board.PourCount));
        }

        [Test]
        public void R13b_Level66_AWinnableStateWhoseUsefulClosureHasNoWin_IsPlaying()
        {
            // Por qué el cierre recorre también los fútiles (R14; CHANGELOG del núcleo, C1-8): tras los 6 primeros vertidos del
            // dueño el nivel se gana en 14, pero solo con algún fútil (Cuia chica de 2). Con solo los vertidos útiles (la poda del
            // solver) no hay victoria en 50 estados: un cierre limitado a los útiles habría declarado Aguas estancadas aquí.
            var session = new LevelSession(Level66Board.Initial(), true, PourMode.Run);
            for (int i = 0; i < 6; i++)
            {
                Assert.That(session.TryPour(Level66Board.Source(i), Level66Board.Target(i), out _), Is.True, "vertido " + (i + 1));
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
            // 04 §2.1 y §2.7 con D-014 y sus añadidos del 2026-11-06 y del 2026-11-17: Marea desde el bloqueo por bucle saca de
            // los vistos el tablero que deshace, así que el tablero del 11.º vuelve a tener un vertido a un tablero no visto y la
            // partida sigue en Playing (UndoEscapes lo decía antes); rehacer el 12.º lleva otra vez al tablero agotado y lo vuelve
            // a declarar. Volver a la orilla empieza un intento nuevo y olvida lo visto [P].
            var session = new LevelSession(Level66Board.Initial(), true, PourMode.Run);
            var oracle = new StagnationOracle();
            oracle.Check(session, "inicio");
            for (int i = 0; i < Level66Board.PourCount; i++)
            {
                Assert.That(session.TryPour(Level66Board.Source(i), Level66Board.Target(i), out _), Is.True, "vertido " + (i + 1));
                oracle.Check(session, "vertido " + (i + 1));
            }

            Assert.That(session.Status, Is.EqualTo(LevelStatus.Stuck));
            Assert.That(PourRules.IsStuck(session.Current, true), Is.False);
            int seen = session.SeenBoardCount;
            Assert.That(seen, Is.EqualTo(Level66Board.PourCount + 1));

            BoardState stuck = session.Current;
            bool escapes = session.UndoEscapes;
            Assert.That(escapes, Is.True, "Marea saca del atasco");
            Assert.That(session.Current.Equals(stuck) && session.Status == LevelStatus.Stuck, Is.True, "UndoEscapes no cambia nada");
            Assert.That(session.Undo(), Is.True);
            oracle.CheckUndo(session, stuck, escapes, "Marea");
            Assert.That(session.SeenBoardCount, Is.EqualTo(seen - 1), "el tablero deshecho deja de estar visto");
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing), "Marea deja jugar");
            Assert.That(oracle.HasPourToUnseen(session.Current, PourMode.Run), Is.True);

            int last = Level66Board.PourCount - 1;
            Assert.That(session.TryPour(Level66Board.Source(last), Level66Board.Target(last), out PourResult again), Is.True);
            oracle.Check(session, "el 12.º otra vez");
            Assert.That(again.Status, Is.EqualTo(LevelStatus.Stuck), "repetir el vertido deshecho vuelve a Aguas estancadas");
            Assert.That(session.Current.Equals(stuck), Is.True);

            // Marea hacia atrás hasta el 8.º: cada paso coincide con el oráculo y con lo que UndoEscapes dijo antes.
            for (int pours = Level66Board.PourCount - 1; pours >= 8; pours--)
            {
                BoardState leaving = session.Current;
                bool predicted = session.UndoEscapes;
                Assert.That(session.Undo(), Is.True);
                oracle.CheckUndo(session, leaving, predicted, "Marea al vertido " + pours);
                Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing), "Marea al vertido " + pours);
            }

            Assert.That(session.SeenBoardCount, Is.EqualTo(9), "los tableros del 9.º al 12.º, deshechos, ya no están vistos");
            Assert.That(session.TryPour(Level66Board.Source(8), Level66Board.Target(8), out PourResult ninth), Is.True);
            oracle.Check(session, "el 9.º otra vez");
            Assert.That(ninth.Status, Is.EqualTo(LevelStatus.Playing), "el 9.º lleva a un tablero perdido, pero sus vertidos ya no están vistos");

            session.Restart();
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(session.SeenBoardCount, Is.EqualTo(1), "Volver a la orilla: intento nuevo");
        }

        [Test]
        public void R13b_UndoAfterARoundTrip_KeepsTheBoardSeenOnce()
        {
            // I6 de LevelSession (añadido de D-014 del 2026-11-17 [P]): un contador por hash, no un conjunto. En el 66 perdido, el
            // vaivén 2 → 1 y su vuelta 1 → 2 pasan dos veces por el mismo tablero: deshacer la vuelta resta 1 y el tablero de
            // partida sigue visto por la primera vez.
            BoardState state = Level66Board.Final();
            var session = new LevelSession(state, true, PourMode.Run);
            var oracle = new StagnationOracle();
            oracle.Check(session, "inicio");
            Assert.That(session.TryPour(2, 1, out _), Is.True);
            oracle.Check(session, "ida");
            BoardState away = session.Current;
            int back = -1;
            for (int b = 0; b < away.Count && back < 0; b++)
            {
                if (b != 1 && PourRules.IsLegal(away, 1, b) && PourRules.Apply(away, 1, b).CanonicalHash() == state.CanonicalHash())
                {
                    back = b;
                }
            }

            Assert.That(back, Is.GreaterThanOrEqualTo(0), "un vertido desde la 1 vuelve al tablero de partida");
            Assert.That(session.TryPour(1, back, out _), Is.True);
            oracle.Check(session, "vuelta");
            Assert.That(session.SeenBoardCount, Is.EqualTo(2));

            BoardState leaving = session.Current;
            bool predicted = session.UndoEscapes;
            Assert.That(session.Undo(), Is.True);
            oracle.CheckUndo(session, leaving, predicted, "Marea de la vuelta");
            Assert.That(session.SeenBoardCount, Is.EqualTo(2), "el tablero de partida sigue visto por la primera vez");

            leaving = session.Current;
            predicted = session.UndoEscapes;
            Assert.That(session.Undo(), Is.True);
            oracle.CheckUndo(session, leaving, predicted, "Marea de la ida");
            Assert.That(session.SeenBoardCount, Is.EqualTo(1), "la ida, deshecha, ya no está vista");
            Assert.That(session.UndoEscapes, Is.False, "sin historial, Marea no saca de nada");
        }

        /// <summary>
        /// Tablero turbio en que el vertido revela una dosis y Marea no saca del atasco (añadido de D-014 del 2026-11-17): 0 =
        /// [2?, 1] de 3, 1 = [2?, 1?, 1] de 3 y 2 = [2, 2] de 2 (? = oculta). Hallado con una búsqueda exhaustiva de historias
        /// cortas sobre tableros turbios pequeños.
        /// </summary>
        private static BoardState MurkyTrap()
        {
            return new BoardState(new[] { Cuia.Create(3, new[] { 2, 1 }, 0x1), Cuia.Create(3, new[] { 2, 1, 1 }, 0x3), C(2, 2, 2) });
        }

        [Test]
        public void R13b_UndoEscapes_WithARevealedDose_MatchesMarea_AndCanBeFalse()
        {
            // Opción 3 del dueño (añadido de D-014 del 2026-11-17), la rama de lo revelado: 1 → 0 vierte el 1 de arriba y deja
            // arriba el 1 oculto, que se revela (I5); el tablero queda en Aguas estancadas. Marea vuelve al tablero de antes con
            // ese 1 ya visible (02 R7, T6), que no es el de antes: con lo revelado, el tablero de vuelta también está bloqueado,
            // así que UndoEscapes es false y el modal no ofrecería Marea. Es el único caso con historial en que pasa.
            BoardState start = MurkyTrap();
            var session = new LevelSession(start, true, PourMode.Run);
            var oracle = new StagnationOracle();
            oracle.Check(session, "inicio");
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(session.UndoEscapes, Is.False, "sin historial");

            Assert.That(session.TryPour(1, 0, out _), Is.True);
            oracle.Check(session, "1 → 0");
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Stuck));
            Assert.That(session.RevealedLayers(1), Is.EqualTo(0x2), "el vertido reveló el 1 oculto de la cuia 1");
            Assert.That(session.SeenBoardCount, Is.EqualTo(2));

            BoardState stuck = session.Current;
            bool predicted = session.UndoEscapes;
            Assert.That(session.Current.Equals(stuck) && session.Status == LevelStatus.Stuck, Is.True, "UndoEscapes no cambia nada");
            Assert.That(session.Undo(), Is.True);
            oracle.CheckUndo(session, stuck, predicted, "Marea");
            Assert.That(predicted, Is.False, "Marea no saca del atasco");
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Stuck));

            // El tablero de vuelta es el de antes con lo revelado (I5): la cuia 1 conserva solo el 2 oculto del fondo.
            BoardState back = new BoardState(new[] { Cuia.Create(3, new[] { 2, 1 }, 0x1), Cuia.Create(3, new[] { 2, 1, 1 }, 0x1), C(2, 2, 2) });
            Assert.That(session.Current.Equals(back), Is.True, session.Current.ToString());
            Assert.That(session.Current.Equals(start), Is.False);
            Assert.That(session.SeenBoardCount, Is.EqualTo(2), "el de partida (oculto) y el de vuelta; el deshecho ya no");
            Assert.That(session.CanUndo, Is.False);
        }

        [Test]
        public void R13b_UndoEscapes_WithoutHistory_IsFalse()
        {
            // Opción 3 del dueño (añadido de D-014 del 2026-11-17): sin historial Marea no deshace nada, así que el modal no la
            // ofrece. Es el bloqueo reabierto al reanudar en el tablero de partida del intento (ResumeStuck) y el de justo tras
            // una Pororoca, que vacía el historial (02 §14).
            var lost = new LevelSession(Level26Board.Lost(), true, PourMode.Run);
            Assert.That(lost.UndoEscapes, Is.False, "en juego, sin historial");
            Assert.That(lost.ResumeStuck(), Is.True);
            Assert.That(lost.Status, Is.EqualTo(LevelStatus.Stuck));
            Assert.That(lost.CanUndo, Is.False);
            Assert.That(lost.UndoEscapes, Is.False);
            Assert.That(lost.Undo(), Is.False);

            // Con historial, el mismo bloqueo reabierto sí: Marea vuelve al tablero de antes, que estaba en juego.
            var resumed = new LevelSession(Level26Board.Lost(), true, PourMode.Run);
            Assert.That(resumed.TryPour(1, 8, out _), Is.True, "2 → 9");
            Assert.That(resumed.ResumeStuck(), Is.True);
            Assert.That(resumed.UndoEscapes, Is.True);
            Assert.That(resumed.Undo(), Is.True);
            Assert.That(resumed.Status, Is.EqualTo(LevelStatus.Playing));
        }

        [Test]
        public void R13b_ResumeStuck_OnlyDeclaresALostBoard()
        {
            // 02 §14 con el añadido de D-014 del 2026-11-06: al reanudar con el modal abierto, ResumeStuck lo reabre solo si el
            // cierre sigue perdido; nunca con un tablero que se gana (R14), y no hace nada fuera de Playing.
            var lost = new LevelSession(Level26Board.Lost(), true, PourMode.Run);
            Assert.That(lost.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(lost.ResumeStuck(), Is.True);
            Assert.That(lost.Status, Is.EqualTo(LevelStatus.Stuck));
            Assert.That(lost.ResumeStuck(), Is.False, "ya en Stuck");
            Assert.That(lost.TryPour(1, 8, out _), Is.False, "R15");

            var winnable = new LevelSession(UniformBoard(Tutorial[5]), true, PourMode.Run);
            Assert.That(winnable.ResumeStuck(), Is.False, "R14: un tablero que se gana no se declara");
            Assert.That(winnable.Status, Is.EqualTo(LevelStatus.Playing));

            var won = new LevelSession(new BoardState(new[] { C(4, 1, 1, 1, 1), Cuia.Empty(4) }), true, PourMode.Run);
            Assert.That(won.ResumeStuck(), Is.False);
            Assert.That(won.Status, Is.EqualTo(LevelStatus.Won));
        }

        [Test]
        public void R13b_TheFireflyAndThePororoca_KeepTheSeenBoards()
        {
            // I6 de LevelSession (añadido de D-014 del 2026-11-06 [P]; 04 §2.4 y §2.5): la Luciérnaga y la Pororoca no vacían
            // los tableros vistos; su tablero nuevo entra como visto al quedar en pantalla.
            var murky = new LevelSession(new BoardState(new[] { Cuia.Create(4, new[] { 3, 1, 1, 1 }, 0x1), C(4, 2, 2, 2), Cuia.Empty(3) }), true, PourMode.Run);
            var oracle = new StagnationOracle();
            oracle.Check(murky, "inicio");
            Assert.That(murky.TryPour(1, 2, out _), Is.True);
            oracle.Check(murky, "vertido");
            Assert.That(murky.Reveal(0), Is.EqualTo(1));
            oracle.Check(murky, "Luciérnaga");
            Assert.That(murky.SeenBoardCount, Is.EqualTo(3));

            BoardState start = Board(C(4, 2, 1, 1, 2), C(4, 2, 1, 1, 2), Cuia.Empty(4));
            var shuffled = new LevelSession(start, true, PourMode.Run);
            var shuffleOracle = new StagnationOracle();
            shuffleOracle.Check(shuffled, "inicio");
            Assert.That(shuffled.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(shuffled.ApplyShuffle(Board(C(4, 1, 2, 2, 1), C(4, 1, 2, 2, 1), Cuia.Empty(4))), Is.True);
            shuffleOracle.Check(shuffled, "Pororoca");
            Assert.That(shuffled.SeenBoardCount, Is.EqualTo(2), "el tablero de antes sigue visto y entra el barajado");
        }

        private static BoardState Board(params Cuia[] cuias)
        {
            return new BoardState(cuias);
        }

        [Test]
        public void R13b_Level26_LostButWithPoursToNewBoards_IsPlaying()
        {
            // Punto 1 del añadido de D-059 del 2026-10-31 (añadido de D-014 del 2026-11-06): el cierre era correcto (12 estados
            // alcanzables, ninguno ganado; el solver lo da por irresoluble), pero el modal salió con vertidos todavía legales
            // (2 → 9 y 6 → 7 del dueño) que llevan a tableros nuevos. Ahora la partida sigue en Playing.
            BoardState state = Level26Board.Lost();
            var probe = new DeadEndProbe();
            SolveResult proof = new AStarSolver().Solve(state, new SolverOptions(SolverOptions.PipelineNodeBudget, 1, false, PourMode.Run));

            Assert.That(probe.IsDeadEnd(state, PourMode.Run), Is.True);
            Assert.That(probe.LastStateCount, Is.EqualTo(12));
            Assert.That(proof.Status, Is.EqualTo(SolveStatus.Unsolvable));
            Assert.That(PourRules.IsLegal(state, 1, 8), Is.True, "2 → 9");
            Assert.That(PourRules.IsLegal(state, 5, 6), Is.True, "6 → 7");
            Assert.That(new LevelSession(state, true, PourMode.Run).Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(new LevelSession(state, false, PourMode.Run).Status, Is.EqualTo(LevelStatus.Playing));
        }

        [Test]
        public void R13b_Level26_TheOwnersPours_KeepPlaying()
        {
            // Los tres vertidos que el dueño aún podía hacer (2 → 9, 1 → 2 y 6 → 7) llevan cada uno a un tablero nuevo: la
            // partida sigue en Playing tras cada uno.
            var session = new LevelSession(Level26Board.Lost(), true, PourMode.Run);
            var oracle = new StagnationOracle();
            oracle.Check(session, "inicio");
            int[][] pours = { new[] { 1, 8 }, new[] { 0, 1 }, new[] { 5, 6 } };
            for (int i = 0; i < pours.Length; i++)
            {
                Assert.That(session.TryPour(pours[i][0], pours[i][1], out PourResult result), Is.True, "vertido " + (i + 1));
                oracle.Check(session, "vertido " + (i + 1));
                Assert.That(result.Status, Is.EqualTo(LevelStatus.Playing), "vertido " + (i + 1));
            }

            Assert.That(session.SeenBoardCount, Is.EqualTo(4));
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void R13b_Level26_ExhaustiveWalk_IsStuckExactlyWhenEveryPourRepeats(PourMode mode)
        {
            // Recorre el cierre entero del 26 en profundidad, con vertidos y Marea, y compara la partida con el oráculo en cada
            // paso: Stuck si y solo si el tablero está perdido y todo vertido legal lleva a un tablero ya visto. Al acabar se han
            // visto todos los estados alcanzables y el tablero de partida da Aguas estancadas.
            BoardState start = Level26Board.Lost();
            var session = new LevelSession(start, true, mode);
            var oracle = new StagnationOracle();
            oracle.Check(session, "inicio");
            var walked = new HashSet<ulong> { start.CanonicalHash() };
            int stuck = Walk(session, oracle, walked, 0);
            var probe = new DeadEndProbe();
            Assert.That(probe.IsDeadEnd(start, mode), Is.True);

            // Añadido de D-014 del 2026-11-17: Marea saca de los vistos lo que deshace, así que, de vuelta al principio, solo queda
            // visto el tablero de partida y sigue en Playing; por el camino, los paseos hacia delante sí acabaron en Aguas estancadas.
            Assert.That(session.Current.Equals(start), Is.True);
            Assert.That(walked.Count, Is.EqualTo(probe.LastStateCount), "el paseo recorrió el cierre entero");
            Assert.That(session.SeenBoardCount, Is.EqualTo(1));
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(stuck, Is.GreaterThan(0));
        }

        /// <summary>Vertidos en profundidad con Marea, comprobando cada paso con el oráculo; devuelve cuántas veces salió Stuck.</summary>
        private static int Walk(LevelSession session, StagnationOracle oracle, HashSet<ulong> walked, int depth)
        {
            Assert.That(depth, Is.LessThan(DeadEndProbe.MaxStates));
            int stuck = 0;
            int count = session.Current.Count;
            for (int a = 0; a < count; a++)
            {
                for (int b = 0; b < count; b++)
                {
                    if (session.Status != LevelStatus.Playing || a == b || !PourRules.IsLegal(session.Current, a, b))
                    {
                        continue;
                    }

                    Assert.That(session.TryPour(a, b, out _), Is.True);
                    oracle.Check(session, "vertido " + a + " → " + b + " en profundidad " + depth);
                    stuck += session.Status == LevelStatus.Stuck ? 1 : 0;
                    if (walked.Add(session.Current.CanonicalHash()))
                    {
                        stuck += Walk(session, oracle, walked, depth + 1);
                    }

                    BoardState leaving = session.Current;
                    bool predicted = session.UndoEscapes;
                    Assert.That(session.Undo(), Is.True);
                    oracle.CheckUndo(session, leaving, predicted, "Marea en profundidad " + depth);
                    stuck += session.Status == LevelStatus.Stuck ? 1 : 0;
                }
            }

            return stuck;
        }

        [Test]
        public void R13b_TheVerdictIsCanonical_AnyOrderOfTheCuias()
        {
            // Determinismo (CLAUDE.md, regla dura 1): estados canónicos, así que el veredicto y el tamaño del cierre no dependen
            // del orden de las cuias en pantalla.
            BoardState state = Level66Board.Final();
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
            var session = new LevelSession(lost, true, PourMode.Run);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing), "perdido, pero con vertidos a tableros nuevos");
            Assert.That(StagnationOracle.WalkUntilStuck(session), Is.EqualTo(LevelStatus.Stuck));
            var winning = new LevelSession(winnable, true, PourMode.Run);
            Assert.That(StagnationOracle.TryEveryPourAndUndo(winning), Is.EqualTo(LevelStatus.Playing), "R14: repetir no declara un tablero ganable");
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
