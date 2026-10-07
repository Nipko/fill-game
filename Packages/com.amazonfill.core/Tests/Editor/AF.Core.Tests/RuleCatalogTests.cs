#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// Una prueba por regla de GDD 02 §1 (R1-R16) en el alcance de v0 (núcleo sin mecánicas). Los casos finos de
    /// cada regla están en CuiaTests, PourRulesTests, FutileRulesTests y LevelSessionTests; aquí se ve de un
    /// vistazo que ninguna queda sin probar. R8 (persistencia) y R15 (400 ms y modal) tienen además una parte de
    /// UI que no es del núcleo. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class RuleCatalogTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static BoardState Board(params Cuia[] cuias)
        {
            return new BoardState(cuias);
        }

        [Test]
        public void R1_BoardHasOneToSixteenCuias_OfCapacityOneToSix()
        {
            // 02 R1
            var sixteen = new Cuia[BoardState.MaxCuias];
            var seventeen = new Cuia[BoardState.MaxCuias + 1];
            for (int i = 0; i < seventeen.Length; i++)
            {
                seventeen[i] = Cuia.Empty(4);
                if (i < sixteen.Length)
                {
                    sixteen[i] = Cuia.Empty(4);
                }
            }

            Assert.That(new BoardState(sixteen).Count, Is.EqualTo(16));
            Assert.That(() => { new BoardState(seventeen); }, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => { new BoardState(new Cuia[0]); }, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(Cuia.Empty(1).Capacity, Is.EqualTo(1));
            Assert.That(Cuia.Empty(6).Capacity, Is.EqualTo(6));
            Assert.That(() => { Cuia.Empty(7); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void R2_DoseColorIsOneToSixteen_MudAndReservedWaitForTheirMechanics()
        {
            // 02 R2: K_motor = 16; 1-14 tintes, 15 Barro, 16 reservado (Barro llega con 02 §9).
            Assert.That(C(4, 1, 14).ColorAt(1), Is.EqualTo(14));
            Assert.That(() => { C(4, 0); }, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => { C(4, 17); }, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => { Board(C(4, Cuia.ReservedColor), Cuia.Empty(4)); }, Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void R3_UnitsPerColor_CountEveryDoseOfTheLevel_AndDoNotChangeWhenPouring()
        {
            // 02 R3
            BoardState state = Board(C(4, 1, 2, 1), C(4, 2, 2, 1), C(4, 1), Cuia.Empty(4));

            BoardState after = PourRules.Apply(state, 0, 2);

            Assert.That(state.UnitsOf(1), Is.EqualTo(4));
            Assert.That(state.UnitsOf(2), Is.EqualTo(3));
            Assert.That(state.UnitsOf(3), Is.EqualTo(0));
            Assert.That(after.UnitsOf(1), Is.EqualTo(4));
            Assert.That(after.UnitsOf(2), Is.EqualTo(3));
        }

        [Test]
        public void R4_MonocolorEmptyAndFull()
        {
            // 02 R4: monocolor ignora "visible"; vacía sin dosis; llena con cap_i dosis.
            Assert.That(C(4, 3, 3).IsMonocolor, Is.True);
            Assert.That(Cuia.Create(4, new[] { 3, 3 }, 0x1).IsMonocolor, Is.True);
            Assert.That(C(4, 3, 2).IsMonocolor, Is.False);
            Assert.That(Cuia.Empty(4).IsEmpty, Is.True);
            Assert.That(C(2, 1, 2).IsFull, Is.True);
            Assert.That(C(4, 1, 2).IsFull, Is.False);
        }

        [Test]
        public void R5_SealedCuia_IsFullMonocolorVisibleAndHoldsAllUnits_AndIsNeitherSourceNorTarget()
        {
            // 02 R5 y §8: Cuia chica nunca sella; una Cuia alta solo sella con su fruta abundante (U_c = 5).
            BoardState state = Board(
                C(4, 1, 1, 1, 1),
                C(2, 2, 2),
                C(4, 2, 2),
                C(5, 3, 3, 3, 3, 3),
                Cuia.Empty(4),
                C(5, 4, 4, 4, 4));

            Assert.That(state.IsSealed(0), Is.True);
            Assert.That(state.IsSealed(1), Is.False);
            Assert.That(state.IsSealed(3), Is.True);
            Assert.That(state.IsSealed(5), Is.False);
            Assert.That(PourRules.IsLegal(state, 0, 4), Is.False);
            Assert.That(PourRules.IsLegal(state, 5, 4), Is.True);
            Assert.That(PourRules.IsLegal(state, 1, 2), Is.True);
        }

        [Test]
        public void R6_VisibleTopRun_IsCutByAHiddenDose()
        {
            // 02 R6: la cantidad usa la racha visible.
            BoardState state = Board(Cuia.Create(4, new[] { 1, 1, 1, 1 }, 0x2), Cuia.Empty(4));

            Assert.That(state[0].TopRunLength, Is.EqualTo(2));
            Assert.That(PourRules.Amount(state, 0, 1), Is.EqualTo(2));
        }

        [Test]
        public void R7_TheNewTopBecomesVisible()
        {
            // 02 R7 y R10 (a)
            BoardState state = Board(Cuia.Create(4, new[] { 2, 1, 1 }, 0x1), Cuia.Empty(4), C(4, 2, 2, 2));

            BoardState after = PourRules.Apply(state, 0, 1);

            Assert.That(after[0].Count, Is.EqualTo(1));
            Assert.That(after[0].IsHiddenAt(0), Is.False);
        }

        [Test]
        public void R8_TheMoveListRebuildsTheState()
        {
            // 02 R8 (parte del núcleo): la lista de vertidos reconstruye el estado; el guardado es de la UI.
            var session = new LevelSession(Board(C(4, 8, 8, 8, 4), C(4, 4, 4, 4, 8), Cuia.Empty(4)), true);
            session.TryPour(0, 2, out _);
            session.TryPour(1, 0, out _);

            BoardState rebuilt = session.Initial;
            for (int i = 0; i < session.AppliedPours; i++)
            {
                Pour pour = session.GetAppliedPour(i);
                rebuilt = PourRules.Apply(rebuilt, pour.Source, pour.Target);
            }

            Assert.That(rebuilt.Equals(session.Current), Is.True);
        }

        [Test]
        public void R9_PartialPour_LeavesTheRestInTheSource_AndCountsAsOneMove()
        {
            // 02 R9
            var session = new LevelSession(Board(C(4, 2, 1, 1, 1), C(4, 3, 3, 1), Cuia.Empty(4), C(4, 2, 2, 2, 3), C(4, 3)), true);

            Assert.That(session.TryPour(0, 1, out PourResult result), Is.True);

            Assert.That(result.Pour.Amount, Is.EqualTo(1));
            Assert.That(session.Current[0], Is.EqualTo(C(4, 2, 1, 1)));
            Assert.That(session.AppliedPours, Is.EqualTo(1));
        }

        [Test]
        public void R10_SealingAndVictoryAreEvaluatedBeforeStuck()
        {
            // 02 R10: (c) sellado, (e) victoria y después (f) bloqueo. Tras el último vertido no queda ningún
            // vertido útil, pero el estado es Won, no Stuck.
            var session = new LevelSession(Board(C(4, 1, 1, 1), C(4, 1)), true);

            Assert.That(session.TryPour(1, 0, out PourResult result), Is.True);

            Assert.That(result.TargetSealed, Is.True);
            Assert.That(result.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(PourRules.HasUsefulMove(session.Current, true), Is.False);
        }

        [Test]
        public void R11_IllegalPour_ConsumesNoMove()
        {
            // 02 R11
            var session = new LevelSession(Board(C(4, 1, 2), C(4, 2, 1), Cuia.Empty(4), C(4, 1, 1, 2, 2)), true);

            Assert.That(session.TryPour(0, 1, out _), Is.False);
            Assert.That(session.AppliedPours, Is.EqualTo(0));
        }

        [Test]
        public void R12_Victory_IsEveryCuiaEmptyOrSealed()
        {
            // 02 R12: dos cuias monocolor parciales del mismo color no son victoria; la Cuia chica acaba vacía.
            Assert.That(Board(C(4, 1, 1), C(4, 1, 1), Cuia.Empty(2)).IsWon(), Is.False);
            Assert.That(Board(C(4, 1, 1, 1, 1), Cuia.Empty(2), Cuia.Empty(4)).IsWon(), Is.True);
            Assert.That(Board(C(4, 1, 1, 1), C(2, 1)).IsWon(), Is.False);
        }

        [Test]
        public void R13_Stuck_WhenThereIsNoUsefulPour()
        {
            // 02 R13: sin vertidos legales, o solo fútiles.
            Assert.That(PourRules.IsStuck(Board(C(4, 1, 2, 1, 2), C(4, 2, 1, 2, 1)), true), Is.True);
            Assert.That(PourRules.IsStuck(Board(C(5, 1, 1, 1, 1), Cuia.Empty(5), C(4, 2, 2, 2, 2)), true), Is.True);
        }

        [Test]
        public void R14_AnUnsolvableBoardWithUsefulPours_IsNotDeclaredStuck()
        {
            // 02 R14: el motor no anuncia "irresoluble" mientras exista un vertido útil. El solver lo demuestra
            // irresoluble, pero el núcleo sigue en Playing.
            BoardState state = Board(C(4, 2, 1, 1, 2), C(4, 2, 1, 1, 2), Cuia.Empty(4));

            SolveResult proof = new AStarSolver().Solve(state, SolverOptions.Pipeline);

            Assert.That(proof.Status, Is.EqualTo(SolveStatus.Unsolvable));
            Assert.That(PourRules.IsStuck(state, true), Is.False);
            Assert.That(new LevelSession(state, true).Status, Is.EqualTo(LevelStatus.Playing));
        }

        [Test]
        public void R15_StuckBlocksInput_AndMareaReturnsToPlaying()
        {
            // 02 R15 (parte del núcleo): input bloqueado desde el instante lógico del bloqueo, aunque quede un
            // vertido legal (fútil); Marea restaura el estado. El retraso de 400 ms y el modal son de la UI.
            var session = new LevelSession(Board(C(5, 1, 1, 1), C(5, 1), Cuia.Empty(5), C(4, 2, 2, 2, 2)), true);
            Assert.That(session.TryPour(1, 0, out _), Is.True);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Stuck));
            Assert.That(PourRules.IsLegal(session.Current, 0, 1), Is.True);

            Assert.That(session.TryPour(0, 1, out _), Is.False);
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
        }

        [Test]
        public void R16_FutilePoursKeepTheCanonicalState_AndTheKillSwitchRestoresTheClassicRule()
        {
            // 02 R16
            BoardState state = Board(C(4, 1, 1, 2, 2), C(4, 2, 2), C(4, 1, 1), Cuia.Empty(4));
            BoardState onlyFutile = Board(C(5, 1, 1, 1, 1), Cuia.Empty(5), C(4, 2, 2, 2, 2));

            BoardState after = PourRules.Apply(state, 2, 3);

            Assert.That(PourRules.IsFutile(state, 2, 3), Is.True);
            Assert.That(after.CanonicalHash(), Is.EqualTo(state.CanonicalHash()));
            Assert.That(PourRules.IsStuck(onlyFutile, true), Is.True);
            Assert.That(PourRules.IsStuck(onlyFutile, false), Is.False);
        }
    }
}
