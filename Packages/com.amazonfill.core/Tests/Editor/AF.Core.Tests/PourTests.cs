#nullable enable
using System;
using AF.Core.Rules;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>Valor <see cref="Pour"/> (02 R9: un vertido A→B y su cantidad). Test compartido (NUnit 3.5, C# 9).</summary>
    [TestFixture]
    public sealed class PourTests
    {
        [Test]
        public void Pour_IsAValue()
        {
            var a = new Pour(0, 2, 3);
            var b = new Pour(0, 2, 3);

            Assert.That(a == b, Is.True);
            Assert.That(a != new Pour(2, 0, 3), Is.True);
            Assert.That(a.Equals((object)b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a.ToString(), Is.EqualTo("Pour(0->2 x3)"));
        }

        [TestCase(-1, 1, 1)]
        [TestCase(16, 1, 1)]
        [TestCase(1, 1, 1)]
        [TestCase(0, 16, 1)]
        [TestCase(0, 1, 0)]
        [TestCase(0, 1, 7)]
        public void Pour_OutOfRange_Throws(int source, int target, int amount)
        {
            Assert.That(() => { new Pour(source, target, amount); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
