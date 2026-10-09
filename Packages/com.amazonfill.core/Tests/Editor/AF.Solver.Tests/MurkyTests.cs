#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Solver.Murky;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Solver.Tests
{
    /// <summary>
    /// Aguas turbias en AF.Solver (C2-2): realidades (02 §11 T2, T4 y <c>validar_turbio</c>; 03 §8), vertidos seguros (T5) y
    /// <c>RUTA_SEGURA</c> (03 §8) sobre tableros hechos a mano. La paridad con los 5 tableros de 03 §18.3 está en
    /// <c>DotnetOnly/MurkyParityTests</c>. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class MurkyTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static Cuia H(int capacity, int hiddenMask, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop, hiddenMask);
        }

        private static BoardState Board(params Cuia[] cuias)
        {
            return new BoardState(cuias);
        }

        /// <summary>
        /// Cuias de 2 y U = 2: [1*, 3], [2*, 3], [1, 2] y una vacía. Dos realidades (T4 deja 1 o 2 bajo cada 3) y vertidos
        /// seguros.
        /// </summary>
        private static BoardState SafeBoard()
        {
            return Board(H(2, 0x1, 1, 3), H(2, 0x1, 2, 3), C(2, 1, 2), Cuia.Empty(2));
        }

        /// <summary>
        /// Cuias de 3 y U = 3: [3, 3*, 1], [2*, 1], [3, 2, 2], [1]. Dos realidades; la verdadera tiene solución (par 4), pero
        /// ningún vertido útil es seguro en las dos (hallado con una búsqueda sobre tableros pequeños).
        /// </summary>
        private static BoardState NoSafeBoard()
        {
            return Board(H(3, 0x2, 3, 3, 1), H(3, 0x1, 2, 1), C(3, 3, 2, 2), C(3, 1));
        }

        [Test]
        public void T2_TwoHiddenDoses_GiveAtMostTwoRealities_0211()
        {
            var realities = new RealityEnumerator();

            // Dos ocultas en una cuia: [1*, 2*, 3] → {1, 2} en cualquier orden (los dos cumplen T4).
            Assert.That(realities.Enumerate(Board(H(4, 0x3, 1, 2, 3), C(4, 3, 3), Cuia.Empty(4))), Is.EqualTo(RealityStatus.Ok));
            Assert.That(realities.Count, Is.EqualTo(2));

            // Dos ocultas en dos cuias: [1*, 2] y [2*, 1]. T4 obliga a 1 bajo el 2 y a 2 bajo el 1: una sola realidad.
            BoardState crossed = Board(H(4, 0x1, 1, 2), H(4, 0x1, 2, 1), Cuia.Empty(4));
            Assert.That(realities.Enumerate(crossed), Is.EqualTo(RealityStatus.Ok));
            Assert.That(realities.Count, Is.EqualTo(1));
            Assert.That(realities.Enumerate(crossed, null, false), Is.EqualTo(RealityStatus.Ok));
            Assert.That(realities.Count, Is.EqualTo(2));
        }

        [Test]
        public void T2_FourHiddenDosesInTwoCuias_GiveAtMost24Realities_0211()
        {
            // 02 T2: 4 dosis ocultas → como mucho 4! = 24 realidades.
            var realities = new RealityEnumerator();
            BoardState distinct = Board(H(4, 0x3, 1, 2, 5), H(4, 0x3, 3, 4, 6), Cuia.Empty(4));

            Assert.That(realities.Enumerate(distinct), Is.EqualTo(RealityStatus.Ok));
            Assert.That(realities.Count, Is.EqualTo(24));
            Assert.That(realities.HiddenPositions, Is.EqualTo(4));
        }

        [Test]
        public void T2_TheCaseOf038_GivesTwelveRealitiesWithoutT4_AndFewerWithIt()
        {
            // 03 §8 y §18.3 (murky_demo n0001): 2 cuias turbias × 2 capas ocultas con el multiconjunto {3, 4, 5, 5} → 4!/2! = 12
            // realidades sin T4 (el prototipo es anterior a T4); con T4, 8.
            BoardState board = Board(
                C(4, 1, 1, 1, 3), Cuia.Empty(4), H(4, 0x6, 5, 3, 5, 2), C(4, 4, 4, 4, 2), H(4, 0x6, 5, 5, 4, 2), C(4, 6, 3, 3, 2), Cuia.Empty(4), C(4, 6, 6, 6, 1));
            var realities = new RealityEnumerator();

            Assert.That(realities.Enumerate(board, null, false), Is.EqualTo(RealityStatus.Ok));
            Assert.That(realities.Count, Is.EqualTo(12));
            Assert.That(realities.Enumerate(board), Is.EqualTo(RealityStatus.Ok));
            Assert.That(realities.Count, Is.EqualTo(8));
        }

        [Test]
        public void T2_MoreThan24Realities_RejectTheLevel_038()
        {
            // 03 §8: «≤ 24, si más → RECHAZAR nivel». 5 ocultas distintas = 120 realidades.
            var realities = new RealityEnumerator();

            Assert.That(realities.Enumerate(Board(H(4, 0x7, 1, 2, 3, 9), H(4, 0x3, 4, 5, 10), Cuia.Empty(4))), Is.EqualTo(RealityStatus.TooMany));
        }

        [Test]
        public void Realities_KeepTheVisibleDosesAndTheHiddenMask_AndIncludeTheTrueOne()
        {
            BoardState board = Board(H(4, 0x3, 1, 2, 5), H(4, 0x1, 3, 4), C(4, 1, 2, 3, 5), Cuia.Empty(4));
            var realities = new RealityEnumerator();
            Assert.That(realities.Enumerate(board), Is.EqualTo(RealityStatus.Ok));

            bool trueFound = false;
            for (int r = 0; r < realities.Count; r++)
            {
                BoardState reality = realities.GetReality(r);
                trueFound |= reality.Equals(board);
                for (int i = 0; i < board.Count; i++)
                {
                    Assert.That(reality[i].HiddenMask, Is.EqualTo(board[i].HiddenMask));
                    for (int k = 0; k < board[i].Count; k++)
                    {
                        if (!board[i].IsHiddenAt(k))
                        {
                            Assert.That(reality[i].ColorAt(k), Is.EqualTo(board[i].ColorAt(k)));
                        }
                        else if (k + 1 < board[i].Count)
                        {
                            Assert.That(reality[i].ColorAt(k), Is.Not.EqualTo(reality[i].ColorAt(k + 1)), "T4");
                        }
                    }
                }

                for (int c = Cuia.MinColor; c <= Cuia.MaxColor; c++)
                {
                    Assert.That(reality.UnitsOf(c), Is.EqualTo(board.UnitsOf(c)));
                }
            }

            Assert.That(trueFound, Is.True);
            Assert.That(realities.Count, Is.EqualTo(6)); // {1, 2, 3} en tres posiciones, todas distintas de la de encima: 3!
        }

        [Test]
        public void T4_UsesTheOriginalDoseAbove_WhenTheMudWasWashed()
        {
            // T4 habla de la dosis original de encima: tras lavar el Barro (02 §9.1 M5) la oculta puede quedar bajo su mismo
            // color, y la realidad verdadera tiene que seguir enumerándose (referencia = tablero inicial).
            BoardState initial = Board(H(4, 0x1, 3, Cuia.MudColor, 2), C(4, 3, 3, 1), C(4, 2, 2, 1));
            BoardState washed = Board(H(4, 0x1, 3, 3), C(4, 3, 3), C(4, 2, 2, 1, 2));
            var realities = new RealityEnumerator();

            Assert.That(realities.Enumerate(washed), Is.EqualTo(RealityStatus.Inconsistent));
            Assert.That(realities.Enumerate(washed, initial), Is.EqualTo(RealityStatus.Ok));
            Assert.That(realities.Count, Is.EqualTo(1));
            Assert.That(realities.GetReality(0).Equals(washed), Is.True);
        }

        [Test]
        public void Realities_WhenTheVisibleDosesExceedTheUnits_AreInconsistent()
        {
            // 02 R3: U_c cuenta las gotas pendientes de la Lluvia; si lo visible y lo pendiente pasan de U_c, no hay realidad.
            Cuia lluvia = Cuia.Empty(4).WithMechanic(MechanicType.Lluvia, key: 1, n: 2, k: 3);
            var realities = new RealityEnumerator();

            Assert.That(realities.Enumerate(Board(C(4, 1, 1), lluvia)), Is.EqualTo(RealityStatus.Inconsistent));
        }

        [Test]
        public void T5_SafeMoves_AreSolvableInEveryReality()
        {
            BoardState board = SafeBoard();
            var safe = new SafeMoves();

            Assert.That(safe.Find(board, SafeMoves.ForPipeline(PourMode.Run)), Is.EqualTo(RealityStatus.Ok));
            Assert.That(safe.RealityCount, Is.EqualTo(2));
            Assert.That(safe.Count, Is.GreaterThan(0));

            var realities = new RealityEnumerator();
            realities.Enumerate(board);
            for (int k = 0; k < safe.Count; k++)
            {
                Pour pour = safe.GetMove(k);
                for (int r = 0; r < realities.Count; r++)
                {
                    BoardState after = PourRules.Apply(realities.GetReality(r), pour.Source, pour.Target, PourMode.Run);
                    Assert.That(ExhaustiveSearch.Par(after, PourMode.Run, true), Is.GreaterThanOrEqualTo(0), pour + " en la realidad " + r);
                }
            }
        }

        [Test]
        public void T5_WithoutASafeMove_ReturnsNone_AndMorphoDoesNotCharge()
        {
            // 02 T5: si no hay vertido seguro demostrable, «ninguno»: Morpho no cobra (no existe Pista parcial). El nivel tiene
            // solución en la realidad verdadera, pero cada vertido útil arruina la partida en alguna realidad.
            BoardState board = NoSafeBoard();
            var safe = new SafeMoves();
            var buffer = new Pour[PourRules.MaxPours];

            Assert.That(safe.Find(board, SafeMoves.ForPipeline(PourMode.Run)), Is.EqualTo(RealityStatus.Ok));
            Assert.That(safe.RealityCount, Is.EqualTo(2));
            Assert.That(safe.Count, Is.EqualTo(0));
            Assert.That(new AStarSolver().Solve(board, SolverOptions.ForPipeline(PourMode.Run)).MoveCount, Is.EqualTo(4));

            // Oráculo independiente: cada vertido útil deja alguna realidad sin solución (búsqueda exhaustiva).
            var realities = new RealityEnumerator();
            realities.Enumerate(board);
            int useful = PourRules.CollectUsefulMoves(board, true, PourMode.Run, buffer);
            Assert.That(useful, Is.GreaterThan(0));
            for (int m = 0; m < useful; m++)
            {
                bool ruins = false;
                for (int r = 0; r < realities.Count; r++)
                {
                    BoardState after = PourRules.Apply(realities.GetReality(r), buffer[m].Source, buffer[m].Target, PourMode.Run);
                    ruins |= ExhaustiveSearch.Par(after, PourMode.Run, true) < 0;
                }

                Assert.That(ruins, Is.True, buffer[m].ToString());
            }

            Assert.That(new SafePath().Find(board, SafeMoves.ForPipeline(PourMode.Run)).Status, Is.EqualTo(SafePathStatus.NoSafeMove));
        }

        [Test]
        public void SafeMoves_UseTheMemoByCanonicalState_038()
        {
            BoardState board = SafeBoard();
            var safe = new SafeMoves();
            safe.Find(board, SafeMoves.ForPipeline(PourMode.Run));
            int searches = safe.Searches;
            int count = safe.Count;

            safe.Find(board, SafeMoves.ForPipeline(PourMode.Run));

            Assert.That(searches, Is.GreaterThan(0));
            Assert.That(safe.Searches, Is.EqualTo(searches)); // todo sale del memo
            Assert.That(safe.Count, Is.EqualTo(count));
            Assert.That(safe.MemoCount, Is.GreaterThan(0));
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void SafePath_ReachesVictory_WithASafeMoveAtEveryStep_038(PourMode mode)
        {
            // 03 §8 RUTA_SEGURA: en cada paso, el vertido seguro de menor par en la realidad verdadera.
            BoardState start = SafeBoard();
            var path = new SafePath();
            var check = new SafeMoves();

            SafePathResult result = path.Find(start, SafeMoves.ForPipeline(mode));

            Assert.That(result.Status, Is.EqualTo(SafePathStatus.Ok));
            Assert.That(result.MaxRealities, Is.EqualTo(2));
            Assert.That(result.FirstSafeMoves, Is.GreaterThan(0));
            BoardState state = start;
            for (int i = 0; i < result.MoveCount; i++)
            {
                Pour pour = result.GetMove(i);
                check.Find(state, SafeMoves.ForPipeline(mode), start);
                bool listed = false;
                for (int k = 0; k < check.Count; k++)
                {
                    listed |= check.GetMove(k) == pour;
                }

                Assert.That(listed, Is.True, "paso " + i + ": " + pour + " no es seguro");
                state = PourRules.Apply(state, pour.Source, pour.Target, mode);
            }

            Assert.That(state.IsWon(), Is.True);
        }

        [Test]
        public void SafePath_RejectsAStartThatBreaksT4_038()
        {
            // 03 §8: «si ¬T4(S_real, ocultas): devolver RECHAZAR».
            BoardState start = Board(H(2, 0x1, 3, 3), H(2, 0x1, 2, 1), C(2, 1, 2), Cuia.Empty(2));

            Assert.That(new SafePath().Find(start, SafeMoves.ForPipeline(PourMode.Run)).Status, Is.EqualTo(SafePathStatus.RejectedT4));
        }
    }
}
