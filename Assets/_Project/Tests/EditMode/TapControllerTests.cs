#nullable enable
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Control tap-tap (GDD 02 §3.1, C1-C7) sobre el núcleo, en el modo de vertido por defecto (Run, D-005); los tests con
    /// modo explícito lo dicen en su nombre.
    /// </summary>
    public sealed class TapControllerTests
    {
        private static LevelSession Level1()
        {
            return new LevelSession(TestPacks.Tutorial.Find(1)!.CreateBoard(), true, PourModes.Default);
        }

        [Test]
        public void C1_TapOnPossibleSource_Selects()
        {
            var taps = new TapController(true);
            TapResult r = taps.OnCuiaTap(Level1(), 0);
            Assert.That(r.Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(taps.Selected, Is.EqualTo(0));
        }

        [Test]
        public void C2_TapOnEmptyWithoutSelection_IsRejected()
        {
            var taps = new TapController(true);
            LevelSession session = Level1();
            TapResult r = taps.OnCuiaTap(session, 2);
            Assert.That(r.Kind, Is.EqualTo(TapKind.Rejected));
            Assert.That(taps.Selected, Is.EqualTo(TapController.NoSelection));
            Assert.That(session.AppliedPours, Is.EqualTo(0));
        }

        [Test]
        public void C3_TapOnSelected_Deselects()
        {
            var taps = new TapController(true);
            LevelSession session = Level1();
            taps.OnCuiaTap(session, 0);
            TapResult r = taps.OnCuiaTap(session, 0);
            Assert.That(r.Kind, Is.EqualTo(TapKind.Deselected));
            Assert.That(taps.Selected, Is.EqualTo(TapController.NoSelection));
        }

        [Test]
        public void C4_LegalTarget_PoursInTheModel_AndClearsSelection()
        {
            var taps = new TapController(true);
            LevelSession session = Level1();
            taps.OnCuiaTap(session, 0);
            TapResult r = taps.OnCuiaTap(session, 2);
            Assert.That(r.Kind, Is.EqualTo(TapKind.Poured));
            Assert.That(r.Pour.Pour.Source, Is.EqualTo(0));
            Assert.That(r.Pour.Pour.Target, Is.EqualTo(2));
            Assert.That(r.Pour.Pour.Amount, Is.EqualTo(1));
            Assert.That(taps.Selected, Is.EqualTo(TapController.NoSelection));
            Assert.That(session.AppliedPours, Is.EqualTo(1));
        }

        [Test]
        public void C4_InSingleMode_ARunOfThree_MovesOneDosePerPour_AndTheOriginIsDeselectedEachTime()
        {
            // Modo single de pruebas (02 §1.2, R9 en single; C4; D-005 lo deja solo para pruebas): la cuia 2 del nivel 2
            // muestra una racha de 3 açaí y la 3 está vacía. Cada vertido 2→3 mueve una sola dosis y deja el origen sin
            // seleccionar aunque le queden dosis del mismo color.
            var taps = new TapController(true);
            var session = new LevelSession(TestPacks.Tutorial.Find(2)!.CreateBoard(), true, PourMode.Single);
            Assert.That(session.Current[2].TopRunLength, Is.EqualTo(3));
            for (int i = 1; i <= 3; i++)
            {
                Assert.That(taps.OnCuiaTap(session, 2).Kind, Is.EqualTo(TapKind.Selected), "origen " + i);
                TapResult r = taps.OnCuiaTap(session, 3);
                Assert.That(r.Kind, Is.EqualTo(TapKind.Poured), "destino " + i);
                Assert.That(r.Pour.Pour.Amount, Is.EqualTo(1), "una dosis en el vertido " + i);
                Assert.That(taps.Selected, Is.EqualTo(TapController.NoSelection), "C4 tras el vertido " + i);
                Assert.That(session.Current[3].Count, Is.EqualTo(i));
            }

            Assert.That(session.AppliedPours, Is.EqualTo(3));
            Assert.That(session.Current[2].Count, Is.EqualTo(1));
        }

        [Test]
        public void C4_InRunMode_TheSameRunMovesInOnePour()
        {
            // rules_pour_mode = "run" (08 §9; el modo por defecto desde D-005): la racha visible que cabe, en un solo vertido
            // (02 R9), y el origen queda sin seleccionar (C4).
            var taps = new TapController(true);
            var session = new LevelSession(TestPacks.Tutorial.Find(2)!.CreateBoard(), true, PourMode.Run);
            taps.OnCuiaTap(session, 2);
            TapResult r = taps.OnCuiaTap(session, 3);
            Assert.That(r.Kind, Is.EqualTo(TapKind.Poured));
            Assert.That(r.Pour.Pour.Amount, Is.EqualTo(3));
            Assert.That(taps.Selected, Is.EqualTo(TapController.NoSelection));
        }

        [Test]
        public void C5_IllegalTargetThatCanBeSource_SwitchesSelection()
        {
            var taps = new TapController(true);
            LevelSession session = Level1();
            taps.OnCuiaTap(session, 0);
            TapResult r = taps.OnCuiaTap(session, 1); // la cuia 1 está llena: ilegal, pero puede ser origen
            Assert.That(r.Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(r.PreviousSelection, Is.EqualTo(0));
            Assert.That(taps.Selected, Is.EqualTo(1));
            Assert.That(session.AppliedPours, Is.EqualTo(0));
        }

        [Test]
        public void C5_WithSwitchDisabled_RejectsAndLowersOrigin()
        {
            var taps = new TapController(false);
            LevelSession session = Level1();
            taps.OnCuiaTap(session, 0);
            TapResult r = taps.OnCuiaTap(session, 1);
            Assert.That(r.Kind, Is.EqualTo(TapKind.Rejected));
            Assert.That(r.PreviousSelection, Is.EqualTo(0));
            Assert.That(taps.Selected, Is.EqualTo(TapController.NoSelection));
        }

        [Test]
        public void C6_SealedTarget_IsRejected()
        {
            var board = new BoardState(new[]
            {
                Cuia.Create(4, new[] { 1, 1, 1, 1 }),
                Cuia.Create(4, new[] { 2, 2, 2, 3 }),
                Cuia.Create(4, new[] { 3, 3, 3, 2 }),
                Cuia.Empty(4),
            });
            var session = new LevelSession(board, true, PourModes.Default);
            var taps = new TapController(true);
            Assert.That(taps.OnCuiaTap(session, 0).Kind, Is.EqualTo(TapKind.Rejected)); // sellada: no es origen (C2)
            taps.OnCuiaTap(session, 1);
            TapResult r = taps.OnCuiaTap(session, 0);
            Assert.That(r.Kind, Is.EqualTo(TapKind.Rejected));
            Assert.That(taps.Selected, Is.EqualTo(TapController.NoSelection));
            Assert.That(session.AppliedPours, Is.EqualTo(0));
        }

        [Test]
        public void C7_BackgroundTap_Deselects()
        {
            var taps = new TapController(true);
            LevelSession session = Level1();
            Assert.That(taps.OnBackgroundTap().Kind, Is.EqualTo(TapKind.Ignored));
            taps.OnCuiaTap(session, 1);
            TapResult r = taps.OnBackgroundTap();
            Assert.That(r.Kind, Is.EqualTo(TapKind.Deselected));
            Assert.That(r.PreviousSelection, Is.EqualTo(1));
            Assert.That(taps.Selected, Is.EqualTo(TapController.NoSelection));
        }

        [Test]
        public void Taps_AreIgnoredWhenTheModelIsNotPlaying()
        {
            var taps = new TapController(true);
            var session = new LevelSession(TestPacks.Tutorial.Find(6)!.CreateBoard(), true, PourModes.Default);
            session.TryPour(3, 4, out _);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Stuck));
            Assert.That(taps.OnCuiaTap(session, 0).Kind, Is.EqualTo(TapKind.Ignored));
        }
    }
}
