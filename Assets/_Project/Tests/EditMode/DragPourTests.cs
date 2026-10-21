#nullable enable
using AF.GameFlow;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// C10, el arrastre (02 §3.1; 08 §9 <c>drag_pour_enabled</c> = true; tarea B11): <see cref="DragPour.Resolve"/>.
    /// </summary>
    public sealed class DragPourTests
    {
        [Test]
        public void C10_Defaults_Of02_3_1_And08_9()
        {
            Assert.That(DragPour.MinDistanceDp, Is.EqualTo(24f), "02 §3.1, C10: tras desplazarse ≥ 24 dp");
            Assert.That(DragPour.EnabledDefault, Is.True, "08 §9: drag_pour_enabled = true");
        }

        [Test]
        public void C10_DownOnX_UpOnY_After24dp_IsAPour()
        {
            Assert.That(DragPour.Resolve(true, true, 2, 5, 24f), Is.EqualTo(DragOutcome.Pour));
            Assert.That(DragPour.Resolve(true, true, 2, 0, 140f), Is.EqualTo(DragOutcome.Pour));
        }

        [Test]
        public void C10_ReleaseOverTheBackground_Cancels()
        {
            Assert.That(DragPour.Resolve(true, true, 2, -1, 200f), Is.EqualTo(DragOutcome.Cancel), "02 §3.1: soltar sobre el fondo cancela");
        }

        [Test]
        public void C10_LessThan24dp_Cancels()
        {
            Assert.That(DragPour.Resolve(true, true, 2, 3, 23.9f), Is.EqualTo(DragOutcome.Cancel));
            Assert.That(DragPour.Resolve(true, true, 2, 3, float.NaN), Is.EqualTo(DragOutcome.Cancel));
        }

        [Test]
        public void C10_SameCuia_IsNotADrag()
        {
            Assert.That(DragPour.Resolve(true, true, 2, 2, 60f), Is.EqualTo(DragOutcome.Cancel), "soltar en X es un toque (lo resuelve la vista)");
        }

        [Test]
        public void C10_Off_WithDragPourEnabledFalse()
        {
            Assert.That(DragPour.Resolve(false, true, 2, 5, 60f), Is.EqualTo(DragOutcome.Cancel), "08 §9: drag_pour_enabled = false");
        }

        [Test]
        public void C10_OnlyFromACuiaLiftedAtTouchDown()
        {
            // Sin elevar en el touch-down (había otra seleccionada o X no puede ser origen: C2), soltar fuera cancela [P].
            Assert.That(DragPour.Resolve(true, false, 2, 5, 60f), Is.EqualTo(DragOutcome.Cancel));
            Assert.That(DragPour.Resolve(true, true, -1, 5, 60f), Is.EqualTo(DragOutcome.Cancel));
        }
    }
}
