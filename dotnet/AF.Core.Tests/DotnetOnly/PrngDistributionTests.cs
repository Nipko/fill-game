#nullable enable
using AF.Core.Determinism;
using NUnit.Framework;

namespace AF.Core.Tests.DotnetOnly
{
    /// <summary>
    /// Uniformidad básica del PRNG del núcleo (C1-4): χ² de <see cref="Prng.NextInt(int)"/> con 10.000 muestras y semillas
    /// fijas (resultado determinista), contra el valor crítico de p = 0,001. Solo dotnet: lotes grandes.
    /// </summary>
    [TestFixture]
    public sealed class PrngDistributionTests
    {
        private const int Samples = 10000;

        // Valores críticos de χ² con p = 0,001 (tablas estándar) para k − 1 grados de libertad.
        [TestCase(2, 10.828)]
        [TestCase(7, 22.458)]
        [TestCase(10, 27.877)]
        [TestCase(40, 73.402)]
        public void NextInt_PassesChiSquare(int bound, double critical)
        {
            string[] seeds = { "amazonfill:v1:m03:v1:n0043", "amazonfill:v1:m05:v1:n0183", "amazonfill:v1:daily:b2:d417" };
            for (int s = 0; s < seeds.Length; s++)
            {
                var rng = new Prng(seeds[s]);
                var counts = new int[bound];
                for (int i = 0; i < Samples; i++)
                {
                    counts[rng.NextInt(bound)]++;
                }

                double expected = (double)Samples / bound;
                double chi = 0;
                for (int k = 0; k < bound; k++)
                {
                    double d = counts[k] - expected;
                    chi += d * d / expected;
                }

                Assert.That(chi, Is.LessThan(critical), seeds[s] + ": χ² = " + chi);
            }
        }

        [Test]
        public void EveryBitOfNextULong_IsBalanced()
        {
            var rng = new Prng(Prng.LevelSeed(3, 1, 43));
            var ones = new int[64];
            for (int i = 0; i < Samples; i++)
            {
                ulong value = rng.NextULong();
                for (int b = 0; b < 64; b++)
                {
                    ones[b] += (int)((value >> b) & 1UL);
                }
            }

            for (int b = 0; b < 64; b++)
            {
                // 5 desviaciones típicas de una binomial(10.000, 1/2): 50 · 5 = 250.
                Assert.That(ones[b], Is.InRange((Samples / 2) - 250, (Samples / 2) + 250), "bit " + b);
            }
        }
    }
}
