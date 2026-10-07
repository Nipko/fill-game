#nullable enable
using AF.Core.Board;
using AF.Core.Session;
using AF.GameFlow;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>Control tap-tap (GDD 02 §3.1, C1-C7) sobre el núcleo.</summary>
    public sealed class TapControllerTests
    {
        private static LevelSession Level1()
        {
            return new LevelSession(TestPacks.Tutorial.Find(1)!.CreateBoard(), true);
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
            var session = new LevelSession(board, true);
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
            var session = new LevelSession(TestPacks.Tutorial.Find(6)!.CreateBoard(), true);
            session.TryPour(3, 4, out _);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Stuck));
            Assert.That(taps.OnCuiaTap(session, 0).Kind, Is.EqualTo(TapKind.Ignored));
        }
    }
}
