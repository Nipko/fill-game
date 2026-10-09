#nullable enable
using System;
using AF.Core.Determinism;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// PRNG determinista (09 R26; 08 §2.1 principio 5): xoshiro256** sembrado con SplitMix64 desde la FNV-1a de 64 bits de la
    /// semilla del nivel. Los valores dorados se calcularon con una implementación independiente en Python del código de
    /// referencia; EditMode los confirma en Mono (y el cliente en IL2CPP). Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class PrngTests
    {
        private static readonly ulong[] Level43 =
        {
            0x79e40f5402b7a56fUL, 0xe79aa9c212cc08e2UL, 0x8fceadbec8ae13caUL, 0xf2e4ae753b9a1eefUL,
            0x0b1f0ed0b5cf486fUL, 0x28d941084205a0c0UL, 0xf371fc7722bfbbf0UL, 0xaf9983bc4997c8aaUL,
            0x728f6d6b447ab04dUL, 0xecddff033324dc2eUL, 0x814b61531bb1e352UL, 0x63195e4d4826a079UL,
            0x766a18fc85566de7UL, 0x46e6ab38e1de14a8UL, 0xce8bab7928d9475cUL, 0x38bde642a83ce98eUL,
        };

        private static readonly ulong[] Level1 =
        {
            0xc2ff09c358cd70bfUL, 0x9f93d6cd529caf27UL, 0x3e9d3af7cfd75b55UL, 0x3e7bd9cd1d593a3fUL,
            0x2c102483e041b0bcUL, 0x0b845bd64b985a23UL, 0x0f68ed81cf151b88UL, 0xd655d2c54c37ceb7UL,
            0xe588a0f0c0ec6cbbUL, 0x2efd340cf25d3188UL, 0x5dff61caae802ca4UL, 0x212dcf6b20ca2bbcUL,
            0xc8fb0003a4c14df1UL, 0xc2344ca66dfaa876UL, 0x524d9e4114302cf2UL, 0xac24d61b286b94a9UL,
        };

        private static readonly ulong[] SeedZero =
        {
            0x99ec5f36cb75f2b4UL, 0xbf6e1f784956452aUL, 0x1a5f849d4933e6e0UL, 0x6aa594f1262d2d2cUL,
            0xbba5ad4a1f842e59UL, 0xffef8375d9ebcacaUL, 0x6c160deed2f54c98UL, 0x8920ad648fc30a3fUL,
            0xdb032c0ba7539731UL, 0xeb3a475a3e749a3dUL, 0x1d42993fa43f2a54UL, 0x11361bf526a14bb5UL,
            0x1b4f07a5ab3d8e9cUL, 0xa7a3257f6986db7fUL, 0x7efdaa95605dfc9cUL, 0x4bde97c0a78eaab8UL,
        };

        [Test]
        public void LevelSeedText_PadsWorldAndLevel()
        {
            // 08 §4.1 y glosario §11: amazonfill:v1:m{mm}:v{v}:n{nnnn} con ceros a la izquierda.
            Assert.That(Prng.LevelSeedText(3, 1, 43), Is.EqualTo("amazonfill:v1:m03:v1:n0043"));
            Assert.That(Prng.LevelSeedText(13, 2, 1000), Is.EqualTo("amazonfill:v1:m13:v2:n1000"));
            Assert.That(() => Prng.LevelSeedText(0, 1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void SeedFrom_IsFnv1a64OfTheUtf8Bytes()
        {
            Assert.That(Prng.SeedFrom(string.Empty), Is.EqualTo(Prng.FnvOffsetBasis));
            Assert.That(Prng.SeedFrom("amazonfill:v1:m03:v1:n0043"), Is.EqualTo(0xaefbcef7a91a4377UL));
            Assert.That(Prng.LevelSeed(1, 1, 1), Is.EqualTo(0x2e6f09749e21d55bUL));
        }

        [Test]
        public void GoldenValues_FirstSixteenOutputs_OfThreeSeeds()
        {
            AssertSequence(new Prng("amazonfill:v1:m03:v1:n0043"), Level43);
            AssertSequence(new Prng(Prng.LevelSeed(1, 1, 1)), Level1);
            AssertSequence(new Prng(0UL), SeedZero);
        }

        [Test]
        public void SameSeed_GivesTheSameSequence_AndReseedRestartsIt()
        {
            var a = new Prng(12345UL);
            var b = new Prng(12345UL);
            var first = new ulong[64];
            for (int i = 0; i < first.Length; i++)
            {
                first[i] = a.NextULong();
                Assert.That(b.NextULong(), Is.EqualTo(first[i]));
            }

            a.Reseed(12345UL);
            for (int i = 0; i < first.Length; i++)
            {
                Assert.That(a.NextULong(), Is.EqualTo(first[i]));
            }
        }

        [Test]
        public void NextInt_StaysInsideTheBound_AndIsDeterministic()
        {
            var a = new Prng(7UL);
            var b = new Prng(7UL);
            int[] bounds = { 1, 2, 3, 7, 10, 100, 1000, int.MaxValue };
            for (int i = 0; i < 2000; i++)
            {
                int bound = bounds[i % bounds.Length];
                int value = a.NextInt(bound);
                Assert.That(value, Is.GreaterThanOrEqualTo(0));
                Assert.That(value, Is.LessThan(bound));
                Assert.That(b.NextInt(bound), Is.EqualTo(value));
            }

            Assert.That(new Prng(1UL).NextInt(1), Is.EqualTo(0));
            Assert.That(a.NextInt(-3, 4), Is.InRange(-3, 3));
            Assert.That(() => a.NextInt(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void NextInt_GoldenValues()
        {
            // Fija también la reducción sin sesgo: si cambia, cambian todos los niveles generados.
            var rng = new Prng("amazonfill:v1:m03:v1:n0043");
            int[] expected = new int[8];
            for (int i = 0; i < expected.Length; i++)
            {
                expected[i] = (int)(((Level43[i] >> 32) * 10UL) >> 32);
            }

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(rng.NextInt(10), Is.EqualTo(expected[i]));
            }
        }

        [Test]
        public void SubSeed_IsDeterministic_AndDistinctPerStream()
        {
            ulong seed = Prng.LevelSeed(3, 1, 43);
            var seen = new ulong[200];
            for (int s = 0; s < seen.Length; s++)
            {
                seen[s] = Prng.SubSeed(seed, (ulong)s);
                Assert.That(Prng.SubSeed(seed, (ulong)s), Is.EqualTo(seen[s]));
                for (int t = 0; t < s; t++)
                {
                    Assert.That(seen[s], Is.Not.EqualTo(seen[t]));
                }
            }

            Assert.That(Prng.SubSeed(seed, 0), Is.Not.EqualTo(seed));
        }

        private static void AssertSequence(Prng rng, ulong[] expected)
        {
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(rng.NextULong(), Is.EqualTo(expected[i]), "salida " + i);
            }
        }
    }
}
