#nullable enable
using AF.Platform;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// C9, mantener pulsado (02 §3.1 y §12; bloque B9; <see cref="CuiaHoldTracker"/>): ≥ 400 ms sin mover el dedo muestra los
    /// nombres y al soltar no selecciona; moverse 24 dp [P] es el arrastre (C10) y lo cancela; varios dedos (C11) también.
    /// </summary>
    public sealed class CuiaHoldTrackerTests
    {
        private static readonly Vector2 At = new Vector2(100f, 300f);

        [Test]
        public void Holding400ms_ShowsTheNames_AndReleasingIsNotATap()
        {
            var hold = new CuiaHoldTracker();
            hold.Press(2, 0, At);
            Assert.That(hold.Tick(399f), Is.False, "399 ms: aún no");
            Assert.That(hold.Tick(1f), Is.True, "02 §3.1: ≥ 400 ms");
            Assert.That(hold.Fired, Is.True);
            Assert.That(hold.Index, Is.EqualTo(2));
            Assert.That(hold.Tick(500f), Is.False, "una sola vez");
            Assert.That(hold.Swallows(2), Is.True, "al soltar no selecciona");
            Assert.That(hold.Release(2, 0), Is.True);
            Assert.That(hold.Swallows(2), Is.True, "el touch-up de la cuia puede preguntar después de soltar");
            Assert.That(hold.Fired, Is.False);
        }

        [Test]
        public void AShortPress_IsATap()
        {
            var hold = new CuiaHoldTracker();
            hold.Press(1, 0, At);
            Assert.That(hold.Tick(300f), Is.False);
            Assert.That(hold.Swallows(1), Is.False);
            Assert.That(hold.Release(1, 0), Is.False);
            Assert.That(hold.Swallows(1), Is.False);
        }

        [Test]
        public void MovingTheDragThreshold_CancelsTheHold()
        {
            var hold = new CuiaHoldTracker();
            hold.Press(1, 0, At);
            hold.Move(0, At + new Vector2(A11yRules.HoldSlopDp - 0.5f, 0f));
            Assert.That(hold.Index, Is.EqualTo(1), "por debajo de 24 dp sigue siendo «sin mover el dedo»");
            hold.Move(0, At + new Vector2(0f, A11yRules.HoldSlopDp));
            Assert.That(hold.Index, Is.EqualTo(CuiaHoldTracker.None), "24 dp: ya es el arrastre (C10)");
            Assert.That(hold.Tick(1000f), Is.False);
            Assert.That(hold.Release(1, 0), Is.False);
        }

        [Test]
        public void MovingAfterTheNames_KeepsThemUntilRelease()
        {
            var hold = new CuiaHoldTracker();
            hold.Press(1, 0, At);
            Assert.That(hold.Tick(400f), Is.True);
            hold.Move(0, At + new Vector2(200f, 0f));
            Assert.That(hold.Swallows(1), Is.True, "[P]: tras los nombres, soltar no hace nada más (ni toque ni arrastre)");
            Assert.That(hold.Release(1, 0), Is.True);
        }

        [Test]
        public void ASecondFinger_CancelsAHoldNotYetShown()
        {
            var hold = new CuiaHoldTracker();
            hold.Press(1, 0, At);
            hold.Tick(200f);
            hold.Press(3, 1, At);
            Assert.That(hold.Tick(1000f), Is.False, "C11: con dos dedos, nada de nombres");
            Assert.That(hold.Release(3, 1), Is.False);
            Assert.That(hold.Release(1, 0), Is.False);
            hold.Press(1, 0, At);
            Assert.That(hold.Tick(400f), Is.True, "con todos los dedos fuera, vuelve a funcionar");
        }

        [Test]
        public void AnotherFingersRelease_DoesNotSwallowTheHeldOne()
        {
            var hold = new CuiaHoldTracker();
            hold.Press(1, 0, At);
            Assert.That(hold.Tick(400f), Is.True);
            hold.Press(4, 1, At);
            Assert.That(hold.Release(4, 1), Is.False, "el otro dedo es un toque normal");
            Assert.That(hold.Swallows(4), Is.False);
            Assert.That(hold.Swallows(1), Is.True);
            Assert.That(hold.Release(1, 0), Is.True);
        }

        [Test]
        public void Cancel_AfterTheNames_StillSwallowsThatFingersRelease_ButNotTheNextGesture()
        {
            var hold = new CuiaHoldTracker();
            hold.Press(3, 0, At);
            Assert.That(hold.Tick(400f), Is.True);
            hold.Cancel(); // se abre la Luciérnaga con el dedo abajo
            Assert.That(hold.Index, Is.EqualTo(CuiaHoldTracker.None));
            Assert.That(hold.Swallows(3), Is.True, "C9: al soltar ese dedo no selecciona (ni elige la cuia de la Luciérnaga)");
            hold.Release(3, 0);
            Assert.That(hold.Swallows(3), Is.True, "en el mismo touch-up");
            hold.Press(3, 0, At);
            Assert.That(hold.Swallows(3), Is.False, "el gesto siguiente es un toque normal");
        }

        [Test]
        public void Cancel_BeforeTheNames_LetsTheReleaseBeATap()
        {
            var hold = new CuiaHoldTracker();
            hold.Press(3, 0, At);
            hold.Tick(200f);
            hold.Cancel();
            Assert.That(hold.Release(3, 0), Is.False);
            Assert.That(hold.Swallows(3), Is.False);
        }

        [Test]
        public void ALostRelease_DoesNotBlockHoldForever()
        {
            var hold = new CuiaHoldTracker();
            hold.Press(1, 0, At); // su touch-up no llega nunca (el lienzo se ocultó)
            hold.Press(2, 0, At); // el mismo dedo vuelve a bajar
            Assert.That(hold.Tick(400f), Is.True, "no se trata como un segundo dedo (C11)");
            Assert.That(hold.Index, Is.EqualTo(2));
        }

        [Test]
        public void C11_AFingerThatComesBackWhileAnotherIsDown_ShowsNoNames()
        {
            var hold = new CuiaHoldTracker();
            hold.Press(1, 0, At); // A
            hold.Press(4, 1, At); // B, sigue abajo
            hold.Release(1, 0); // A sube antes de los 400 ms
            hold.Press(1, 0, At); // A vuelve a bajar con el mismo id
            Assert.That(hold.Tick(1000f), Is.False, "C11: con B abajo, nada de nombres");
            hold.Release(1, 0);
            hold.Release(4, 1);
            Assert.That(hold.AnyDown, Is.False);
            hold.Press(2, 0, At);
            Assert.That(hold.Tick(400f), Is.True, "con todos fuera, vuelve a funcionar");
        }

        [Test]
        public void ALostReleaseOfAnUntrackedFinger_IsForgottenWhenItsIdComesBack()
        {
            var hold = new CuiaHoldTracker();
            hold.Press(1, 0, At); // A, seguido
            hold.Press(4, 1, At); // B
            hold.Release(1, 0); // A sube; el touch-up de B se pierde
            hold.Press(2, 1, At); // el id de B vuelve a bajar: era un touch-up perdido
            Assert.That(hold.Tick(400f), Is.True, "C9 no se queda bloqueado por un dedo que ya no está");
            Assert.That(hold.Index, Is.EqualTo(2));
        }

        [Test]
        public void WouldFire_TellsTheStepThatShowsTheNames()
        {
            var hold = new CuiaHoldTracker();
            hold.Press(1, 0, At);
            Assert.That(hold.WouldFire(399f), Is.False);
            Assert.That(hold.WouldFire(400f), Is.True);
            hold.Tick(400f);
            Assert.That(hold.WouldFire(16f), Is.False, "ya salieron");
        }

        [Test]
        public void Reset_ForgetsEverything()
        {
            var hold = new CuiaHoldTracker();
            hold.Press(1, 0, At);
            hold.Tick(400f);
            hold.Reset();
            Assert.That(hold.Index, Is.EqualTo(CuiaHoldTracker.None));
            Assert.That(hold.Swallows(1), Is.False);
            hold.Press(2, 5, At);
            Assert.That(hold.Tick(400f), Is.True);
        }
    }
}
