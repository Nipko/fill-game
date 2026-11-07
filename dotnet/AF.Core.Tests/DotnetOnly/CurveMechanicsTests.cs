#nullable enable
using System.Collections.Generic;
using System.Text.Json;
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Solver.Bot;
using AF.Solver.Murky;
using AF.Solver.Search;
using AF.Solver.Shuffle;
using AF.Solver.Tests;
using NUnit.Framework;

namespace AF.Core.Tests.DotnetOnly
{
    /// <summary>
    /// Lotes de la fase F2 de D-068 (Aguapé, Aguapé espeso y Lancha; 02 §9.1 M8 y M11; 03 §3, §5, §6 y §9): la clave canónica
    /// con los grupos renumerados, el bucle caliente sin asignaciones (CLAUDE.md, regla dura 2), las ayudas (Pororoca,
    /// Luciérnaga, Morpho) y la carga de un nivel con <c>pack_schema</c> 5 (08 §4.1). Solo dotnet: lotes grandes y JSON.
    /// </summary>
    [TestFixture]
    public sealed class CurveMechanicsTests
    {
        private static List<BoardState> ValidBoards(IMechanicInjector injector, ulong seed, int count, bool solvable)
        {
            var rng = new Prng(seed);
            var boards = new List<BoardState>();
            var solver = new AStarSolver();
            for (int attempt = 0; attempt < 4000 && boards.Count < count; attempt++)
            {
                BoardState? board = FutilePropertyTests.CurveStart(rng, (attempt & 1) != 0, injector);
                if (board != null && (!solvable || solver.Solve(board, SolverOptions.ForPipeline(PourMode.Run)).IsSolved))
                {
                    boards.Add(board);
                }
            }

            Assert.That(boards.Count, Is.EqualTo(count), injector.Name);
            return boards;
        }

        [Test]
        public void Canon_03_3_AnyPermutationAndAnyGroupNumbering_GiveTheSameCanonicalState()
        {
            // D-068 (03 §3): los ids de grupo se renumeran en orden canónico. Para cada tablero con dos grupos (o un espeso) y
            // sus estados tras unos vertidos, 20 permutaciones de las cuias con otra numeración de los grupos dan el mismo
            // estado canónico, cuia a cuia, y Sort es idempotente.
            var rng = new Prng(0xC4A0_3003UL);
            var boards = new List<BoardState>();
            boards.AddRange(ValidBoards(new AguapeInjector(groups: 2), 0xC4A0_0001UL, 30, false));
            boards.AddRange(ValidBoards(new AguapeInjector(thick: true), 0xC4A0_0002UL, 10, false));
            var legal = new Pour[PourRules.MaxPours];
            int checkedStates = 0;
            for (int b = 0; b < boards.Count; b++)
            {
                BoardState state = boards[b];
                for (int step = 0; step < 4; step++)
                {
                    Cuia[] reference = Canonical(state);
                    for (int t = 0; t < 20; t++)
                    {
                        Cuia[] other = Shuffled(state, rng);
                        BoardCanon.Sort(other, 0, other.Length);
                        for (int i = 0; i < other.Length; i++)
                        {
                            Assert.That(other[i].Packed, Is.EqualTo(reference[i].Packed), "tablero " + b + ", " + state);
                        }
                    }

                    var again = (Cuia[])reference.Clone();
                    BoardCanon.Sort(again, 0, again.Length);
                    Assert.That(again, Is.EqualTo(reference));
                    checkedStates++;

                    int count = PourRules.CollectUsefulMoves(state, false, PourMode.Run, legal);
                    if (count == 0)
                    {
                        break;
                    }

                    Pour pick = legal[rng.NextInt(count)];
                    state = PourRules.Apply(state, pick.Source, pick.Target, PourMode.Run);
                }
            }

            Assert.That(checkedStates, Is.GreaterThan(100));
        }

        [Test]
        public void Solver_03_5_APermutedAndRenumberedBoardHasTheSamePar_AndItsSolutionReplays()
        {
            var rng = new Prng(0xC4A0_3005UL);
            var solver = new AStarSolver();
            List<BoardState> boards = ValidBoards(new AguapeInjector(groups: 2), 0xC4A0_0005UL, 20, true);
            for (int b = 0; b < boards.Count; b++)
            {
                var permuted = new BoardState(Shuffled(boards[b], rng));
                SolveResult a = solver.Solve(boards[b], SolverOptions.ForPipeline(PourMode.Run));
                SolveResult p = solver.Solve(permuted, SolverOptions.ForPipeline(PourMode.Run));
                Assert.That(p.MoveCount, Is.EqualTo(a.MoveCount), "tablero " + b);
                BoardState state = permuted;
                for (int i = 0; i < p.MoveCount; i++)
                {
                    Pour pour = p.GetMove(i);
                    Assert.That(PourRules.IsLegal(state, pour.Source, pour.Target), Is.True);
                    state = PourRules.Apply(state, pour.Source, pour.Target, PourMode.Run);
                }

                Assert.That(state.IsWon(), Is.True);
            }
        }

        [Test]
        public void CurveBoards_HotLoop_DoesNotAllocate()
        {
            // CLAUDE.md, regla dura 2: con Aguapé (dos grupos: la renumeración usa búferes en la pila) y con Lancha, movimientos
            // útiles, bloqueo, canonicalización, solver, bot casual, bot atento y cierre de D-014, una vez calentados.
            var states = new List<BoardState>();
            states.AddRange(ValidBoards(new AguapeInjector(groups: 2), 0xA110_0001UL, 2, true));
            states.AddRange(ValidBoards(new AguapeInjector(thick: true), 0xA110_0002UL, 1, true));
            states.AddRange(ValidBoards(new LanchaInjector(), 0xA110_0003UL, 2, true));
            var buffer = new Pour[PourRules.MaxPours];
            var solver = new AStarSolver();
            var casual = new CasualBot();
            var attentive = new AttentiveBot();
            var probe = new DeadEndProbe();
            var canon = new Cuia[BoardState.MaxCuias];
            SolverOptions options = SolverOptions.ForPipeline(PourMode.Run);
            for (int s = 0; s < states.Count; s++)
            {
                BoardState state = states[s];
                Assert.That(solver.Solve(state, options).IsSolved, Is.True);
                casual.FailRate(state, 3UL, PourMode.Run);
                attentive.FailRate(state, 3UL, PourMode.Run);
                probe.IsDeadEnd(state, PourMode.Run);
                int moves = PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);

                int total = 0;
                AllocationReading rules = AllocationMeter.Measure(() =>
                {
                    total = 0;
                    for (int repeat = 0; repeat < 200; repeat++)
                    {
                        total += PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);
                        total += PourRules.IsStuck(state, true) ? 1 : 0;
                        for (int i = 0; i < state.Count; i++)
                        {
                            canon[i] = state[i];
                        }

                        BoardCanon.Sort(canon, 0, state.Count);
                    }

                    casual.FailRate(state, 4UL, PourMode.Run);
                    attentive.FailRate(state, 4UL, PourMode.Run);
                    probe.IsDeadEnd(state, PourMode.Run);
                }, 1);
                AllocationReading solve = AllocationMeter.Measure(() => solver.Solve(state, options), 1024);

                Assert.That(total, Is.EqualTo(200 * moves), "tablero " + s);
                Assert.That(rules.Bytes, Is.EqualTo(0), rules.Describe("reglas, canon y bots, tablero " + s));
                Assert.That(solve.Bytes, Is.LessThan(1024), solve.Describe("Solve, tablero " + s));
            }
        }

        [Test]
        public void Pororoca_04_2_4_HeldAguapeAndWaitingLanchaStayFixed()
        {
            // 02 §9.1 M8 regla 7 y M11 [P]: la ola deja fijas las cuias bajo la mata y la carga sin atracar; tras soltarse o
            // atracar ya se mueven. Con un tablero barajado, la partida asienta la lancha (LevelSession.ApplyShuffle).
            var shuffler = new Shuffler();
            int shuffled = 0;
            var boards = new List<BoardState>();
            boards.AddRange(ValidBoards(new AguapeInjector(), 0x0E0A_0001UL, 6, true));
            boards.AddRange(ValidBoards(new LanchaInjector(), 0x0E0A_0002UL, 6, true));
            for (int b = 0; b < boards.Count; b++)
            {
                BoardState board = boards[b];
                for (int i = 0; i < board.Count; i++)
                {
                    if (board[i].HasMechanic)
                    {
                        Assert.That(Shuffler.IsFixed(board, i), Is.True, "tablero " + b + ", cuia " + i);
                    }
                }

                ShuffleResult result = shuffler.Shuffle(board, Shuffler.SeedFor("amazonfill:test:curve", board, b), ShuffleOptions.Default);
                if (!result.Succeeded)
                {
                    continue;
                }

                shuffled++;
                for (int i = 0; i < board.Count; i++)
                {
                    if (board[i].HasMechanic)
                    {
                        Assert.That(result.Board![i].Packed, Is.EqualTo(board[i].Packed), "tablero " + b + ", cuia " + i);
                    }
                }

                var session = new LevelSession(board, true, PourMode.Run);
                Assert.That(session.ApplyShuffle(result.Board!), Is.True);
            }

            Assert.That(shuffled, Is.GreaterThan(0));
        }

        [Test]
        public void Luciernaga_And_Morpho_WithAguapeAndAguasTurbias()
        {
            // Añadido de D-068 del 2026-11-07 (pregunta 13): Aguapé con Aguas turbias. La Luciérnaga revela la cuia turbia sin
            // tocar la mata (04 §2.5), y Morpho (SafePath, 03 §8) encuentra la ruta segura.
            BoardState state = new BoardState(new[]
            {
                Cuia.Create(4, new[] { 1, 1, 1 }).WithMechanic(MechanicType.Aguape, pairId: 5, n: 1),
                Cuia.Create(4, new[] { 2, 2 }).WithMechanic(MechanicType.Aguape, pairId: 5, n: 1),
                Cuia.Create(4, new[] { 3, 3, 1, 2 }), Cuia.Create(4, new[] { 3, 2, 3 }, 0x1), Cuia.Empty(4),
            });
            Assert.That(LevelValidation.Validate(state), Is.Null);

            SafePathResult path = new SafePath().Find(state, SolverOptions.ForPipeline(PourMode.Run));
            Assert.That(path.Status, Is.EqualTo(SafePathStatus.Ok));

            var session = new LevelSession(state, true, PourMode.Run);
            Assert.That(session.Reveal(3), Is.EqualTo(1));
            Assert.That(session.Current[0].Mechanic, Is.EqualTo(MechanicType.Aguape));
            Assert.That(session.Current[1].Mechanic, Is.EqualTo(MechanicType.Aguape));
        }

        [Test]
        public void PackSchema5_08_4_1_ALevelWithAguapeAndLanchaLoads()
        {
            // 08 §4.1 (pack_schema 5; D-068 [P]): attrs aguape {group, n?} y lancha {k}.
            const string Json = "{\"caps\":[4,4,4,4,4,4,4],\"cuias\":[[1,1,1],[2,2],[3,3,1,2],[3,2,3],[],[4,4],[4,4]],"
                + "\"attrs\":[{\"type\":\"aguape\",\"group\":3},{\"type\":\"aguape\",\"group\":3},null,null,null,{\"type\":\"lancha\",\"k\":2},null],"
                + "\"hidden\":[]}";
            using (JsonDocument doc = JsonDocument.Parse(Json))
            {
                BoardState state = CanonicalHashGoldenTests.BuildBoard(doc.RootElement);
                Assert.That(state[0].Mechanic, Is.EqualTo(MechanicType.Aguape));
                Assert.That(state[1].MechanicGroup, Is.EqualTo(3));
                Assert.That(state[1].MechanicN, Is.EqualTo(1));
                Assert.That(state[5].Mechanic, Is.EqualTo(MechanicType.Lancha));
                Assert.That(state[5].MechanicK, Is.EqualTo(2));
                Assert.That(LevelValidation.Validate(state), Is.Null);
                Assert.That(LevelValidation.ValidateRows(state, new[] { 4, 3 }), Is.Null);
            }
        }

        private static Cuia[] Canonical(BoardState state)
        {
            var cuias = new Cuia[state.Count];
            for (int i = 0; i < cuias.Length; i++)
            {
                cuias[i] = state[i];
            }

            BoardCanon.Sort(cuias, 0, cuias.Length);
            return cuias;
        }

        /// <summary>Las cuias en otro orden y con los grupos del Aguapé renumerados con una biyección al azar de 0-7.</summary>
        private static Cuia[] Shuffled(BoardState state, Prng rng)
        {
            var ids = new[] { 0, 1, 2, 3, 4, 5, 6, 7 };
            for (int i = ids.Length - 1; i > 0; i--)
            {
                int j = rng.NextInt(i + 1);
                (ids[i], ids[j]) = (ids[j], ids[i]);
            }

            var cuias = new Cuia[state.Count];
            for (int i = 0; i < cuias.Length; i++)
            {
                Cuia cuia = state[i];
                cuias[i] = cuia.Mechanic == MechanicType.Aguape ? cuia.WithMechanicPairBits(ids[cuia.MechanicPairId]) : cuia;
            }

            for (int i = cuias.Length - 1; i > 0; i--)
            {
                int j = rng.NextInt(i + 1);
                (cuias[i], cuias[j]) = (cuias[j], cuias[i]);
            }

            return cuias;
        }
    }
}
