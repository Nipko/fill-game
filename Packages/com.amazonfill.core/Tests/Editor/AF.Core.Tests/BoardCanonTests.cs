#nullable enable
using AF.Core.Board;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// Canonicalización y hash de 64 bits (GDD 03 §3; 08 §2.2) e igualdad de <see cref="BoardState"/>.
    /// Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class BoardCanonTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        [Test]
        public void Sort_OrdersByPackedAscending()
        {
            var cuias = new[] { C(4, 4, 4, 4, 8), Cuia.Empty(4), C(4, 8, 8, 8, 4) };

            BoardCanon.Sort(cuias, 0, cuias.Length);

            Assert.That(cuias[0].Packed, Is.EqualTo(0x4000000000UL));
            Assert.That(cuias[1].Packed, Is.EqualTo(0x4400003777UL));
            Assert.That(cuias[2].Packed, Is.EqualTo(0x4400007333UL));
        }

        [Test]
        public void Hash_IsFnv1a64OverLittleEndianPackedBytes()
        {
            // Vector fijo: si cambia, cambia la clave de visitados y de anti-repetición (03 §3).
            var canonical = new[] { Cuia.Empty(4), C(4, 8, 8, 8, 4), C(4, 4, 4, 4, 8) };

            Assert.That(BoardCanon.Hash(canonical, 0, 0), Is.EqualTo(BoardCanon.FnvOffsetBasis));
            Assert.That(BoardCanon.Hash(canonical, 0, 3), Is.EqualTo(0x7280611bba60fec9UL));
        }

        [Test]
        public void CanonicalHash_IsTheSameForEveryPermutation()
        {
            // 03 §3: canon(estado) elimina las T! permutaciones de cuias.
            var a = new BoardState(new[] { C(4, 8, 8, 8, 4), C(4, 4, 4, 4, 8), Cuia.Empty(4) });
            var b = new BoardState(new[] { Cuia.Empty(4), C(4, 4, 4, 4, 8), C(4, 8, 8, 8, 4) });

            Assert.That(a.Equals(b), Is.False);
            Assert.That(a.CanonicalHash(), Is.EqualTo(b.CanonicalHash()));
            Assert.That(a.CanonicalHash(), Is.EqualTo(0x7280611bba60fec9UL));
        }

        [Test]
        public void CanonicalHash_SeesCapacity()
        {
            // La capacidad forma parte de la clave (03 §3: (caps[i], attr[i], cuia_i)).
            var standard = new BoardState(new[] { C(4, 1, 1), Cuia.Empty(4) });
            var small = new BoardState(new[] { C(4, 1, 1), Cuia.Empty(2) });

            Assert.That(standard.CanonicalHash(), Is.Not.EqualTo(small.CanonicalHash()));
        }

        [Test]
        public void Equals_ComparesPackedInOrder()
        {
            var a = new BoardState(new[] { C(4, 1, 2), Cuia.Empty(4) });
            var same = new BoardState(new[] { C(4, 1, 2), Cuia.Empty(4) });
            var swapped = new BoardState(new[] { Cuia.Empty(4), C(4, 1, 2) });

            Assert.That(a.Equals(same), Is.True);
            Assert.That(a.Equals((object)same), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(same.GetHashCode()));
            Assert.That(a.Equals(swapped), Is.False);
            Assert.That(a.Equals(null), Is.False);
        }
    }
}
