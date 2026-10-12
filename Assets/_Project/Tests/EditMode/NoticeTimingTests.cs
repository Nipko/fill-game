#nullable enable
using AF.GameFlow;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Duración por lectura de los avisos del tablero (D-011, punto 2; prueba en el teléfono del 2026-10-11, §P2) [P]:
    /// <c>1.000 ms + 55 ms × caracteres visibles</c>, entre 3.000 y 7.000 ms, sin asignaciones.
    /// </summary>
    public sealed class NoticeTimingTests
    {
        [Test]
        public void ReadMs_IsOneSecondPlus55msPerChar_Between3And7Seconds()
        {
            Assert.That(NoticeTiming.ReadMs(0), Is.EqualTo(3000), "sin texto: el mínimo");
            Assert.That(NoticeTiming.ReadMs(-5), Is.EqualTo(3000));
            Assert.That(NoticeTiming.ReadMs(1), Is.EqualTo(3000));
            Assert.That(NoticeTiming.ReadMs(36), Is.EqualTo(3000), "1.000 + 55 × 36 = 2.980: el mínimo");
            Assert.That(NoticeTiming.ReadMs(37), Is.EqualTo(3035), "el corte de abajo cae en 36,4 caracteres");
            Assert.That(NoticeTiming.ReadMs(72), Is.EqualTo(4960), "la tarjeta del mono ardilla: 20 + 12 + 40");
            Assert.That(NoticeTiming.ReadMs(109), Is.EqualTo(6995), "el corte de arriba cae en 109,1 caracteres");
            Assert.That(NoticeTiming.ReadMs(110), Is.EqualTo(7000), "1.000 + 55 × 110 = 7.050: el máximo");
            Assert.That(NoticeTiming.ReadMs(5000), Is.EqualTo(7000));
            Assert.That(NoticeTiming.ReadMs(int.MaxValue), Is.EqualTo(7000), "sin desbordar");
            Assert.That(NoticeTiming.MinMs, Is.EqualTo(3000));
            Assert.That(NoticeTiming.MaxMs, Is.EqualTo(7000));
        }

        [Test]
        public void TheGiftFromCapi_Reads5455ms()
        {
            Assert.That(UiText.ExtraCuiaGift.Length, Is.EqualTo(81), "el texto de 04 §2.2 que no daba tiempo a leer en 2,6 s");
            Assert.That(NoticeTiming.ReadMs(UiText.ExtraCuiaGift), Is.EqualTo(5455), "1.000 + 55 × 81");
            Assert.That(NoticeTiming.ReadMs((string?)null), Is.EqualTo(3000));
            Assert.That(NoticeTiming.ReadMs(UiText.RescueApplied), Is.EqualTo(3000), "los cortos, al mínimo");
        }

        [Test]
        public void ThreeParts_AddTheirLengths_WithoutConcatenating()
        {
            const string name = "mono ardilla";
            const string fact = "Cruza el bosque en fila, por ramas finas";
            int n = UiText.FaunaSightedCard.Length + name.Length + fact.Length;
            Assert.That(n, Is.EqualTo(72));
            Assert.That(NoticeTiming.ReadMs(UiText.FaunaSightedCard, name, fact), Is.EqualTo(NoticeTiming.ReadMs(n)));
            Assert.That(NoticeTiming.ReadMs(UiText.FaunaSightedCard, name, fact), Is.EqualTo(4960), "unos 5 s");
            Assert.That(NoticeTiming.ReadMs(null, null, null), Is.EqualTo(3000));
        }

        [Test]
        public void ReadMs_DoesNotAllocate()
        {
            string gift = UiText.ExtraCuiaGift;
            string title = UiText.FaunaSightedCard;
            int sink = 0;
            NoticeTiming.ReadMs(81);
            NoticeTiming.ReadMs(gift);
            NoticeTiming.ReadMs(title, gift, title);
            Assert.That(() => { sink += NoticeTiming.ReadMs(81); }, Is.Not.AllocatingGCMemory());
            Assert.That(() => { sink += NoticeTiming.ReadMs(gift); }, Is.Not.AllocatingGCMemory());
            Assert.That(() => { sink += NoticeTiming.ReadMs(title, gift, title); }, Is.Not.AllocatingGCMemory());
            Assert.That(sink, Is.GreaterThan(0));
        }
    }
}
