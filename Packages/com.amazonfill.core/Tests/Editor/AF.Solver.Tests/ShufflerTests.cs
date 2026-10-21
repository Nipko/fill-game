#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Solver.Search;
using AF.Solver.Shuffle;
using NUnit.Framework;

namespace AF.Solver.Tests
{
    /// <summary>
    /// Pororoca · Barajar (04 §2.4; 03 §16; 03 §5): reconstrucción inversa sobre las cuias no fijas, verificación con A*
    /// (<c>shuffle_par_slack</c> = 2, <c>shuffle_budget_nodes</c> = 40.000, w = 2), respaldo Permutar + verificar, cuias fijas
    /// (selladas, Barro y mecánicas, salvo el Remolino), Lluvia y Piraña deshabilitadas, la ola que revela y el determinismo.
    /// Test compartido (NUnit 3.5, C# 9); la medida de asignaciones está en <c>DotnetOnly/ShuffleAllocationTests</c>.
    /// </summary>
    [TestFixture]
    public sealed class ShufflerTests
    {
        private const string LevelSeed = "amazonfill:v1:m02:v1:n0030";

        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        // Nivel 30 típico: 6 frutas de 4 en cuias de 4 y 2 vacíos (03 §11, tramo 11-30).
        private static BoardState Level30()
        {
            return new BoardState(new[]
            {
                C(4, 1, 2, 3, 4), C(4, 5, 6, 1, 2), C(4, 3, 4, 5, 6), C(4, 6, 5, 4, 3), C(4, 2, 1, 6, 5), C(4, 4, 3, 2, 1),
                Cuia.Empty(4), Cuia.Empty(4),
            });
        }

        // A medio jugar: la fruta 1 ya sellada y el resto revuelto con un vacío.
        private static BoardState WithSealed()
        {
            return new BoardState(new[]
            {
                C(4, 1, 1, 1, 1), C(4, 2, 3, 2, 3), C(4, 3, 2, 4, 4), C(4, 4, 4, 3, 2), Cuia.Empty(4),
            });
        }

        private static int Par(BoardState board)
        {
            var solver = new AStarSolver();
            SolveResult result = solver.Solve(board, new SolverOptions(ShuffleOptions.DefaultBudgetNodes, ShuffleOptions.DefaultWeight, true, PourMode.Run).ForLevel(board));
            return result.IsSolved ? result.MoveCount : -1;
        }

        private static int Empties(BoardState board)
        {
            int n = 0;
            for (int i = 0; i < board.Count; i++)
            {
                n += board[i].IsEmpty ? 1 : 0;
            }

            return n;
        }

        [Test]
        public void Shuffle_KeepsColorsCapacitiesAndEmpties_04_2_4()
        {
            BoardState board = Level30();
            var shuffler = new Shuffler();

            ShuffleResult result = shuffler.Shuffle(board, Shuffler.SeedFor(LevelSeed, board, 0), ShuffleOptions.Default);

            Assert.That(result.Succeeded, Is.True);
            BoardState shuffled = result.Board!;
            Assert.That(shuffled.Count, Is.EqualTo(board.Count));
            for (int i = 0; i < board.Count; i++)
            {
                Assert.That(shuffled[i].Capacity, Is.EqualTo(board[i].Capacity), "capacidad de la cuia " + i);
            }

            for (int color = Cuia.MinColor; color <= Cuia.MaxColor; color++)
            {
                Assert.That(shuffled.UnitsOf(color), Is.EqualTo(board.UnitsOf(color)), "U_c del color " + color);
            }

            Assert.That(Empties(shuffled), Is.EqualTo(Empties(board)));
            Assert.That(shuffled.CanonicalHash(), Is.Not.EqualTo(board.CanonicalHash()), "03 §16: nunca el mismo tablero");
            Assert.That(result.Method, Is.EqualTo(ShuffleMethod.Inverse));
        }

        [Test]
        public void Shuffle_NewParIsWithinTheSlack_AndNoCuiaIsFullMonocolor_04_2_4()
        {
            BoardState board = Level30();
            var shuffler = new Shuffler();
            for (int ordinal = 0; ordinal < 6; ordinal++)
            {
                ShuffleResult result = shuffler.Shuffle(board, Shuffler.SeedFor(LevelSeed, board, ordinal), ShuffleOptions.Default);

                Assert.That(result.Succeeded, Is.True, "ordinal " + ordinal);
                BoardState shuffled = result.Board!;
                Assert.That(result.ParBefore, Is.EqualTo(Par(board)));
                Assert.That(result.ParAfter, Is.EqualTo(Par(shuffled)));
                Assert.That(result.ParAfter, Is.LessThanOrEqualTo(result.ParBefore + ShuffleOptions.DefaultParSlack));
                for (int i = 0; i < shuffled.Count; i++)
                {
                    Assert.That(!shuffled[i].IsEmpty && shuffled[i].IsFull && shuffled[i].IsMonocolor, Is.False, "cuia " + i + " llena y monocolor");
                }
            }
        }

        [Test]
        public void Shuffle_IsDeterministic_PerSeed()
        {
            BoardState board = Level30();
            ulong seed = Shuffler.SeedFor(LevelSeed, board, 0);

            ShuffleResult a = new Shuffler().Shuffle(board, seed, ShuffleOptions.Default);
            var reused = new Shuffler();
            reused.Shuffle(WithSealed(), 99UL, ShuffleOptions.Default); // un barajador usado antes da lo mismo
            ShuffleResult b = reused.Shuffle(board, seed, ShuffleOptions.Default);

            Assert.That(b.Board!.Equals(a.Board), Is.True);
            Assert.That(b.Tries, Is.EqualTo(a.Tries));
            Assert.That(b.ParAfter, Is.EqualTo(a.ParAfter));
        }

        [Test]
        public void SeedFor_DependsOnTheLevel_TheRevealedBoardAndTheOrdinal()
        {
            BoardState board = Level30();
            BoardState hidden = new BoardState(new[]
            {
                Cuia.Create(4, new[] { 1, 2, 3, 4 }, 0b0011), C(4, 5, 6, 1, 2), C(4, 3, 4, 5, 6), C(4, 6, 5, 4, 3), C(4, 2, 1, 6, 5),
                C(4, 4, 3, 2, 1), Cuia.Empty(4), Cuia.Empty(4),
            });

            Assert.That(Shuffler.SeedFor(LevelSeed, hidden, 0), Is.EqualTo(Shuffler.SeedFor(LevelSeed, board, 0)), "lo oculto no cambia la ola");
            Assert.That(Shuffler.SeedFor(LevelSeed, board, 1), Is.Not.EqualTo(Shuffler.SeedFor(LevelSeed, board, 0)));
            Assert.That(Shuffler.SeedFor("amazonfill:v1:m02:v1:n0031", board, 0), Is.Not.EqualTo(Shuffler.SeedFor(LevelSeed, board, 0)));
        }

        [Test]
        public void SealedCuias_StayFixed_04_2_4()
        {
            BoardState board = WithSealed();

            ShuffleResult result = new Shuffler().Shuffle(board, Shuffler.SeedFor(LevelSeed, board, 0), ShuffleOptions.Default);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Board![0].Packed, Is.EqualTo(board[0].Packed));
            Assert.That(Shuffler.IsFixed(board, 0), Is.True);
            Assert.That(Shuffler.IsFixed(board, 1), Is.False);
        }

        [Test]
        public void MechanicCuias_StayFixed_ButRemolinoTakesPart_04_2_4_b_c()
        {
            // Hoja cerrada, Matapi con contenido y Barro quedan fijos; el Remolino entra en la ola con su pareja. U_c = 4 por fruta
            // (una dosis de 2 en el Matapi y una de 3 sobre el Barro).
            var cuias = new[]
            {
                C(4, 1, 2, 3, 1), C(4, 2, 3, 1, 2), C(4, 3, 1),
                Cuia.Empty(4).WithMechanic(MechanicType.Hoja, key: 1, flags: Cuia.FlagClosed),
                C(4, 2).WithMechanic(MechanicType.Matapi),
                C(4, Cuia.MudColor, 3),
                Cuia.Empty(4).WithMechanic(MechanicType.Remolino, pairId: 0), Cuia.Empty(4).WithMechanic(MechanicType.Remolino, pairId: 0),
            };
            var board = new BoardState(cuias);

            Assert.That(Shuffler.IsFixed(board, 3), Is.True, "Hoja cerrada");
            Assert.That(Shuffler.IsFixed(board, 4), Is.True, "Matapi");
            Assert.That(Shuffler.IsFixed(board, 5), Is.True, "Barro");
            Assert.That(Shuffler.IsFixed(board, 6), Is.False, "Remolino");
            ShuffleResult result = new Shuffler().Shuffle(board, 7UL, ShuffleOptions.Default);
            Assert.That(result.Succeeded, Is.True);
            for (int i = 3; i <= 5; i++)
            {
                Assert.That(result.Board![i].Packed, Is.EqualTo(board[i].Packed), "cuia fija " + i);
            }

            Assert.That(result.Board![6].Mechanic, Is.EqualTo(MechanicType.Remolino));
            Assert.That(result.Board![7].Mechanic, Is.EqualTo(MechanicType.Remolino));
            Assert.That(result.Board![7].MechanicPairId, Is.EqualTo(0));
        }

        [Test]
        public void LluviaOrPirana_DisableTheWave_04_2_4_d()
        {
            BoardState rain = new BoardState(new[]
            {
                C(4, 1, 2, 1, 2), C(4, 2, 1, 2, 1), C(4, 10, 10).WithMechanic(MechanicType.Lluvia, key: 10, n: 2, k: 3), Cuia.Empty(4),
            });
            BoardState pirana = new BoardState(new[]
            {
                C(4, 1, 2, 1, 2), C(4, 2, 1, 2, 1), Cuia.Empty(4).WithMechanic(MechanicType.Pirana, k: 3), Cuia.Empty(4),
            });

            Assert.That(Shuffler.Check(rain, true), Is.EqualTo(ShuffleBlock.TemporalMechanic));
            Assert.That(Shuffler.Check(pirana, true), Is.EqualTo(ShuffleBlock.TemporalMechanic));
            ShuffleResult result = new Shuffler().Shuffle(rain, 1UL, ShuffleOptions.Default);
            Assert.That(result.Method, Is.EqualTo(ShuffleMethod.Failed));
            Assert.That(result.Block, Is.EqualTo(ShuffleBlock.TemporalMechanic));
            Assert.That(result.Board, Is.Null);
        }

        [Test]
        public void LessThanTwoMovableColors_IsLowWater_04_2_4_b()
        {
            BoardState board = new BoardState(new[] { C(4, 1, 1, 1, 1), C(4, 2, 2), C(4, 2, 2), Cuia.Empty(4) });

            Assert.That(Shuffler.MovableColors(board), Is.EqualTo(1));
            Assert.That(Shuffler.Check(board, true), Is.EqualTo(ShuffleBlock.LowWater));
            ShuffleResult result = new Shuffler().Shuffle(board, 1UL, ShuffleOptions.Default);
            Assert.That(result.Block, Is.EqualTo(ShuffleBlock.LowWater));
            Assert.That(result.Succeeded, Is.False);
        }

        [Test]
        public void HiddenDoses_AreRevealedByTheWave_04_2_4_a()
        {
            BoardState board = new BoardState(new[]
            {
                Cuia.Create(4, new[] { 1, 2, 3, 4 }, 0b0011), C(4, 5, 6, 1, 2), Cuia.Create(4, new[] { 3, 4, 5, 6 }, 0b0001),
                C(4, 6, 5, 4, 3), C(4, 2, 1, 6, 5), C(4, 4, 3, 2, 1), Cuia.Empty(4), Cuia.Empty(4),
            });

            Assert.That(Shuffler.Check(board, false), Is.EqualTo(ShuffleBlock.HiddenWithoutReveal));
            Assert.That(Shuffler.Check(board, true), Is.EqualTo(ShuffleBlock.None));
            ShuffleResult result = new Shuffler().Shuffle(board, Shuffler.SeedFor(LevelSeed, board, 0), ShuffleOptions.Default);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Revealed, Is.EqualTo(3));
            Assert.That(result.Board!.HasHidden, Is.False);
        }

        [Test]
        public void Tinaja_AndItsHangingCuia_StayFixed_04_2_4_g()
        {
            var cuias = new[]
            {
                Cuia.Create(5, new[] { 1, 2, 3 }, 0b011).WithMechanic(MechanicType.Tinaja),
                Cuia.Empty(2).WithMechanic(MechanicType.Cucharon),
                C(4, 1, 2, 3, 1), C(4, 2, 3, 2), C(4, 3, 1), Cuia.Empty(4), Cuia.Empty(4),
            };
            var board = new BoardState(cuias);

            ShuffleResult result = new Shuffler().Shuffle(board, Shuffler.SeedFor(LevelSeed, board, 0), ShuffleOptions.Default);

            Assert.That(Shuffler.IsFixed(board, 0), Is.True);
            Assert.That(Shuffler.IsFixed(board, 1), Is.True);
            Assert.That(result.Revealed, Is.EqualTo(2));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Board![0].Mechanic, Is.EqualTo(MechanicType.Tinaja));
            Assert.That(result.Board![0].HasHidden, Is.False, "la ola revela todas sus capas (04 §2.4 g)");
            Assert.That(result.Board![0].Count, Is.EqualTo(3));
            Assert.That(result.Board![1].Packed, Is.EqualTo(board[1].Packed));
        }

        [Test]
        public void WithoutInverseAttempts_FallsBackToPermute_04_2_4()
        {
            BoardState board = Level30();
            var options = new ShuffleOptions(ShuffleOptions.DefaultParSlack, ShuffleOptions.DefaultBudgetNodes, inverseAttempts: 0);

            ShuffleResult result = new Shuffler().Shuffle(board, Shuffler.SeedFor(LevelSeed, board, 0), options);

            Assert.That(result.Method, Is.EqualTo(ShuffleMethod.Permute));
            Assert.That(result.Board!.CanonicalHash(), Is.Not.EqualTo(board.CanonicalHash()));
            Assert.That(Empties(result.Board!), Is.EqualTo(Empties(board)));
            Assert.That(result.ParAfter, Is.LessThanOrEqualTo(result.ParBefore + ShuffleOptions.DefaultParSlack));
        }

        [Test]
        public void WhenNothingVerifies_ItFails_WithoutABoard_04_2_4()
        {
            // Presupuesto de 1 estado: el A* no resuelve nada y ningún candidato se verifica.
            BoardState board = Level30();
            var options = new ShuffleOptions(ShuffleOptions.DefaultParSlack, 1, inverseAttempts: 3, permuteAttempts: 3);

            ShuffleResult result = new Shuffler().Shuffle(board, 5UL, options);

            Assert.That(result.Method, Is.EqualTo(ShuffleMethod.Failed));
            Assert.That(result.Board, Is.Null);
            Assert.That(result.Tries, Is.EqualTo(6));
            Assert.That(result.ParBefore, Is.EqualTo(-1));
        }

        [Test]
        public void UnknownStartingPar_TheNewBoardOnlyHasToBeSolvable_P3()
        {
            // P-3 (docs/dev/pororoca.md): con 12 estados el A* no resuelve el tablero de partida (par desconocido) y sí el barajado
            // de la semilla 1 (caso buscado a mano en dotnet; si el barajador cambia, se vuelve a buscar).
            BoardState board = Level30();
            var options = new ShuffleOptions(ShuffleOptions.DefaultParSlack, 12);

            ShuffleResult result = new Shuffler().Shuffle(board, 1UL, options);

            Assert.That(result.ParBefore, Is.EqualTo(-1));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ParAfter, Is.GreaterThan(0));
        }

        [Test]
        public void OpenedOrReleasedMechanics_TakePartInTheWave_04_2_4_b_g()
        {
            // 04 §2.4 b: solo quedan fijas las cuias «con la mecánica cerrada»; la Hoja que cayó y la cuia que bajó del jirau son
            // normales (WithoutMechanic) y entran en la ola.
            var hoja = new BoardState(new[]
            {
                Cuia.Empty(4).WithMechanic(MechanicType.Hoja, key: 1, flags: Cuia.FlagClosed), C(4, 2, 1, 1), C(4, 1, 2, 2), C(4, 2, 1),
            });
            var session = new AF.Core.Session.LevelSession(hoja, true);
            Assert.That(Shuffler.IsFixed(hoja, 0), Is.True);
            Assert.That(session.TryPour(1, 0, out _), Is.True);
            Assert.That(session.Current[0].HasMechanic, Is.False);
            Assert.That(Shuffler.IsFixed(session.Current, 0), Is.False, "Hoja abierta");

            var tinaja = new BoardState(new[]
            {
                C(4, 2, 2, 2), C(4, 1, 1, 1), Cuia.Create(5, new[] { 1, 2 }, 0b01).WithMechanic(MechanicType.Tinaja),
                Cuia.Empty(2).WithMechanic(MechanicType.Cucharon), Cuia.Empty(4),
            });
            var scoop = new AF.Core.Session.LevelSession(tinaja, true);
            Assert.That(Shuffler.IsFixed(tinaja, 3), Is.True, "cuia colgada");
            Assert.That(scoop.TryPour(2, 3, out _), Is.True);
            Assert.That(scoop.TryPour(2, 3, out _), Is.True);
            Assert.That(scoop.Current[3].HasMechanic, Is.False);
            Assert.That(Shuffler.IsFixed(scoop.Current, 3), Is.False, "la cuia suelta, como Cuia chica normal");
            Assert.That(Shuffler.IsFixed(scoop.Current, 2), Is.True, "la tinaja vacía, inerte, sigue fija");
        }

        [Test]
        public void MixedCapacities_AreKeptPerCuia_04_2_4()
        {
            // Cuia chica de 2 y Cuia alta de 5 con su fruta abundante (02 §8).
            var board = new BoardState(new[]
            {
                C(5, 1, 2, 1, 3, 1), C(4, 2, 1, 3, 2), C(4, 3, 2, 3, 1), Cuia.Empty(2), Cuia.Empty(5),
            });

            ShuffleResult result = new Shuffler().Shuffle(board, Shuffler.SeedFor(LevelSeed, board, 0), ShuffleOptions.Default);

            Assert.That(result.Succeeded, Is.True);
            for (int i = 0; i < board.Count; i++)
            {
                Assert.That(result.Board![i].Capacity, Is.EqualTo(board[i].Capacity));
            }

            Assert.That(result.ParAfter, Is.LessThanOrEqualTo(result.ParBefore + ShuffleOptions.DefaultParSlack));
        }
    }
}
