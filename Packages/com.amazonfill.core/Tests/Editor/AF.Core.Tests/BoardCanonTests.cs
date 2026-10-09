#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
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
        public void CanonicalHash_WithMechanics_IsTheSameForEveryPermutation()
        {
            // 03 §3 con attrs: la clave es (cap, attr, contenido); permutar cuias no la cambia.
            Cuia hoja = Cuia.Empty(4).WithMechanic(MechanicType.Hoja, key: 4, flags: Cuia.FlagClosed);
            Cuia brote = C(4, 2, 1).WithMechanic(MechanicType.Brote, flags: Cuia.FlagClosed);
            Cuia matapi = Cuia.Empty(4).WithMechanic(MechanicType.Matapi);
            var a = new BoardState(new[] { C(4, 4, 4, 1, 2), hoja, brote, C(4, 4, 4, 2, 2), matapi, C(4, 1, 1) });
            var b = new BoardState(new[] { matapi, C(4, 1, 1), brote, hoja, C(4, 4, 4, 2, 2), C(4, 4, 4, 1, 2) });

            Assert.That(a.Equals(b), Is.False);
            Assert.That(a.CanonicalHash(), Is.EqualTo(b.CanonicalHash()));
        }

        [Test]
        public void CanonicalHash_SeesTypeKeyPairNKFlagsAndCounter()
        {
            // 03 §3 y §9: tipo, key, pareja, n, k, banderas y contadores entran en la clave canónica.
            Cuia baseCuia = C(4, 1, 1);
            Cuia[] variants =
            {
                baseCuia,
                baseCuia.WithMechanic(MechanicType.Matapi),
                baseCuia.WithMechanic(MechanicType.Brote),
                baseCuia.WithMechanic(MechanicType.Brote, flags: Cuia.FlagClosed),
                baseCuia.WithMechanic(MechanicType.Liana, key: 3),
                baseCuia.WithMechanic(MechanicType.Liana, key: 5),
                baseCuia.WithMechanic(MechanicType.Liana, n: 1),
                baseCuia.WithMechanic(MechanicType.Remolino, pairId: 1),
                baseCuia.WithMechanic(MechanicType.Pirana, k: 4),
                baseCuia.WithMechanic(MechanicType.Pirana, k: 4, counter: 1),
            };

            var hashes = new ulong[variants.Length];
            for (int i = 0; i < variants.Length; i++)
            {
                hashes[i] = new BoardState(new[] { variants[i], C(4, 1, 1, 2, 2), C(4, 2, 2), Cuia.Empty(4) }).CanonicalHash();
                for (int j = 0; j < i; j++)
                {
                    Assert.That(hashes[i], Is.Not.EqualTo(hashes[j]), variants[i] + " frente a " + variants[j]);
                }
            }
        }

        [Test]
        public void BoardsWithoutAttrs_KeepTheirPackedAndHash()
        {
            // C1-3: los bits nuevos valen 0 sin mecánica, así que BoardBuilder sin attrs da el mismo Packed y el mismo hash
            // que el constructor de siempre (vectores fijos de este archivo, calculados antes de C1-3).
            BoardBuildResult built = BoardBuilder.Build(
                new[] { 4, 4, 4 }, new[] { new[] { 8, 8, 8, 4 }, new[] { 4, 4, 4, 8 }, new int[0] });

            Assert.That(built.IsValid, Is.True, built.Error);
            BoardState state = built.State!;
            Assert.That(state.CanonicalHash(), Is.EqualTo(0x7280611bba60fec9UL));
            Assert.That(state[0].Packed, Is.EqualTo(0x4400003777UL));
            Assert.That(state[1].Packed, Is.EqualTo(0x4400007333UL));
            Assert.That(state[2].Packed, Is.EqualTo(0x4000000000UL));

            var mixed = new BoardState(new[]
            {
                C(2, 1, 1), C(5, 3, 3, 3, 3, 2), C(4, 2, 2, 2, 2), C(6, 3), Cuia.Empty(4),
            });
            Assert.That(mixed.CanonicalHash(), Is.EqualTo(0xDCA318A281480632UL));
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
