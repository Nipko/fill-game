#nullable enable
using System;
using AF.Core.Board;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>Estado de una cuia: R1, R2, R4, R6 y R7 (GDD 02 §1.1). Test compartido (NUnit 3.5, C# 9).</summary>
    [TestFixture]
    public sealed class CuiaTests
    {
        [Test]
        public void Create_StoresColorsBottomToTop()
        {
            Cuia cuia = Cuia.Create(6, new[] { 1, 16, 3, 14, 5, 6 }, 0x0B);

            Assert.That(cuia.Capacity, Is.EqualTo(6));
            Assert.That(cuia.Count, Is.EqualTo(6));
            Assert.That(cuia.IsFull, Is.True);
            Assert.That(cuia.ColorAt(0), Is.EqualTo(1));
            Assert.That(cuia.ColorAt(1), Is.EqualTo(16));
            Assert.That(cuia.ColorAt(3), Is.EqualTo(14));
            Assert.That(cuia.TopColor, Is.EqualTo(6));
            Assert.That(cuia.IsHiddenAt(0), Is.True);
            Assert.That(cuia.IsHiddenAt(1), Is.True);
            Assert.That(cuia.IsHiddenAt(2), Is.False);
            Assert.That(cuia.IsHiddenAt(3), Is.True);
            Assert.That(cuia.ToString(), Is.EqualTo("Cuia(6: 1* 16* 3 14* 5 6)"));
        }

        [Test]
        public void Empty_HasNoColorAndFullSpace()
        {
            Cuia cuia = Cuia.Empty(Cuia.DefaultCapacity);

            Assert.That(cuia.IsEmpty, Is.True);
            Assert.That(cuia.IsFull, Is.False);
            Assert.That(cuia.Space, Is.EqualTo(4));
            Assert.That(cuia.TopColor, Is.EqualTo(Cuia.NoColor));
            Assert.That(cuia.TopRunLength, Is.EqualTo(0));
        }

        [Test]
        public void EqualCuias_HaveEqualPackedValue()
        {
            Cuia a = Cuia.Create(4, new[] { 2, 3 });
            Cuia b = Cuia.Empty(4).AddOnTop(2, 1).AddOnTop(3, 1);
            Cuia c = Cuia.Create(4, new[] { 2, 3, 3 }).RemoveTop(1);

            Assert.That(a == b, Is.True);
            Assert.That(a == c, Is.True);
            Assert.That(a.Packed, Is.EqualTo(c.Packed));
            Assert.That(a.GetHashCode(), Is.EqualTo(c.GetHashCode()));
            Assert.That(a != Cuia.Create(5, new[] { 2, 3 }), Is.True);
        }

        [TestCase(0)]
        [TestCase(7)]
        public void Create_CapacityOutOfRange_Throws(int capacity)
        {
            Assert.That(() => { Cuia.Empty(capacity); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [TestCase(0)]
        [TestCase(17)]
        public void Create_ColorOutOfRange_Throws(int color)
        {
            Assert.That(() => { Cuia.Create(4, new[] { 1, color }); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Create_MoreDosesThanCapacity_Throws()
        {
            Assert.That(() => { Cuia.Create(2, new[] { 1, 1, 1 }); }, Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Create_HiddenTop_Throws()
        {
            // R7: la cima de una cuia no vacía siempre es visible.
            Assert.That(() => { Cuia.Create(4, new[] { 1, 2 }, 0x2); }, Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void IsMonocolor_IgnoresVisibility()
        {
            // R4: monocolor mira el color, no si la dosis está oculta.
            Cuia cuia = Cuia.Create(4, new[] { 3, 3, 3 }, 0x1);

            Assert.That(cuia.IsMonocolor, Is.True);
            Assert.That(cuia.HasHidden, Is.True);
        }

        [Test]
        public void TopRunLength_IsCutByHiddenDose()
        {
            // R6: una dosis oculta corta la racha aunque su color coincida.
            Cuia cuia = Cuia.Create(4, new[] { 2, 1, 1, 1 }, 0x2);

            Assert.That(cuia.TopRunLength, Is.EqualTo(2));
        }

        [Test]
        public void RemoveTop_RevealsNewTop()
        {
            // R7: la dosis oculta que queda arriba pasa a ser visible.
            Cuia cuia = Cuia.Create(4, new[] { 1, 2, 3 }, 0x3);

            Cuia after = cuia.RemoveTop(1);

            Assert.That(after.Count, Is.EqualTo(2));
            Assert.That(after.TopColor, Is.EqualTo(2));
            Assert.That(after.IsHiddenAt(1), Is.False);
            Assert.That(after.IsHiddenAt(0), Is.True);
        }

        [Test]
        public void AddOnTop_BeyondCapacity_Throws()
        {
            Cuia cuia = Cuia.Create(3, new[] { 1, 1 });

            Assert.That(() => { cuia.AddOnTop(1, 2); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
