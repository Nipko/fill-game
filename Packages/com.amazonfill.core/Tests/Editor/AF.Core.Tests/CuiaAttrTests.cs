#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Mechanics;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// Atributos de mecánica empaquetados en <see cref="Cuia.Packed"/> (08 §4.1 <c>{type, key, partner, n, k}</c>; 03 §3 y
    /// §9; propuesta de D-007 (g), bits libres 30-31, 35 y 39-63): ida y vuelta de cada campo, bits libres a 0 y el principio
    /// de 02 R16 para D-007 (una mecánica abierta deja la cuia normal). Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class CuiaAttrTests
    {
        private const ulong FreeBits = (0x3UL << 30) | (0x1UL << 35) | (0x7UL << 61);
        private const ulong MechanicBits = 0x1FFFFFFUL << 39;

        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        [Test]
        public void PlainCuia_HasNoMechanic_AndItsMechanicBitsAreZero()
        {
            Cuia cuia = Cuia.Create(4, new[] { 3, 1, 1 }, 0x1);

            Assert.That(cuia.HasMechanic, Is.False);
            Assert.That(cuia.Mechanic, Is.EqualTo(MechanicType.None));
            Assert.That(cuia.MechanicKey, Is.EqualTo(Cuia.NoColor));
            Assert.That(cuia.Packed & MechanicBits, Is.EqualTo(0UL));
            Assert.That(cuia.Packed & FreeBits, Is.EqualTo(0UL));
        }

        [TestCase(MechanicType.Hoja, 14, 0, 0, 0, 0, Cuia.FlagClosed)]
        [TestCase(MechanicType.Brote, 0, 0, 0, 0, 0, Cuia.FlagClosed)]
        [TestCase(MechanicType.Liana, 1, 0, 0, 0, 0, 0)]
        [TestCase(MechanicType.Liana, 0, 0, 2, 0, 0, 0)]
        [TestCase(MechanicType.Matapi, 0, 0, 0, 0, 0, 0)]
        [TestCase(MechanicType.Remolino, 0, 7, 0, 0, 0, 0)]
        [TestCase(MechanicType.Lluvia, 10, 0, 7, 5, 4, 0)]
        [TestCase(MechanicType.Pirana, 0, 0, 0, 2, 7, 3)]
        public void WithMechanic_RoundTripsEveryField(MechanicType type, int key, int pair, int n, int k, int counter, int flags)
        {
            Cuia plain = Cuia.Create(5, new[] { 2, 9, 9 }, 0x1);

            Cuia cuia = plain.WithMechanic(type, key, pair, n, k, counter, flags);

            Assert.That(cuia.HasMechanic, Is.True);
            Assert.That(cuia.Mechanic, Is.EqualTo(type));
            Assert.That(cuia.MechanicKey, Is.EqualTo(key));
            Assert.That(cuia.MechanicPairId, Is.EqualTo(pair));
            Assert.That(cuia.MechanicN, Is.EqualTo(n));
            Assert.That(cuia.MechanicK, Is.EqualTo(k));
            Assert.That(cuia.MechanicCounter, Is.EqualTo(counter));
            Assert.That(cuia.MechanicFlags, Is.EqualTo(flags));
            Assert.That(cuia.Packed & FreeBits, Is.EqualTo(0UL));
            Assert.That(cuia.Capacity, Is.EqualTo(5));
            Assert.That(cuia.Count, Is.EqualTo(3));
            Assert.That(cuia.HiddenMask, Is.EqualTo(0x1));
            Assert.That(cuia.ColorAt(1), Is.EqualTo(9));
            Assert.That(cuia.WithoutMechanic(), Is.EqualTo(plain));
        }

        [Test]
        public void MechanicKey_IsOnlyMeaningfulForHojaHojaAtadaLluviaLianaWithFruitAndFlor()
        {
            Assert.That(C(4).WithMechanic(MechanicType.Hoja, key: 1).MechanicKey, Is.EqualTo(1));
            Assert.That(C(4).WithMechanic(MechanicType.HojaAtada, key: 5).MechanicKey, Is.EqualTo(5));
            Assert.That(C(4).WithMechanic(MechanicType.Flor, key: 7, key2: 9).MechanicKey, Is.EqualTo(7));
            Assert.That(C(4).WithMechanic(MechanicType.Flor, key: 7, key2: 9).MechanicKey2, Is.EqualTo(9));
            Assert.That(C(4).WithMechanic(MechanicType.HojaAtada, key: 5).MechanicKey2, Is.EqualTo(Cuia.NoColor));
            Assert.That(C(4).WithMechanic(MechanicType.Brote, n: 2).MechanicKey, Is.EqualTo(Cuia.NoColor));
            Assert.That(C(4).WithMechanic(MechanicType.Liana, key: 6).MechanicKey, Is.EqualTo(6));
            Assert.That(C(4).WithMechanic(MechanicType.Liana, n: 1).MechanicKey, Is.EqualTo(Cuia.NoColor));
            Assert.That(C(4).WithMechanic(MechanicType.Matapi).MechanicKey, Is.EqualTo(Cuia.NoColor));
        }

        [Test]
        public void PourOperations_KeepTheMechanicFields()
        {
            Cuia matapi = C(4, 3).WithMechanic(MechanicType.Matapi);
            Cuia rain = C(4, 1, 1).WithMechanic(MechanicType.Lluvia, key: 10, n: 2, k: 3, counter: 1);

            Assert.That(matapi.AddOnTop(3, 2), Is.EqualTo(C(4, 3, 3, 3).WithMechanic(MechanicType.Matapi)));
            Assert.That(rain.RemoveTop(1), Is.EqualTo(C(4, 1).WithMechanic(MechanicType.Lluvia, key: 10, n: 2, k: 3, counter: 1)));
            Assert.That(rain.WithMechanicCounter(2).MechanicCounter, Is.EqualTo(2));
            Assert.That(rain.WithMechanicN(1).MechanicN, Is.EqualTo(1));
            Assert.That(rain.WithMechanicN(1).MechanicCounter, Is.EqualTo(1));
        }

        [Test]
        public void OpenedMechanic_HasTheSamePackedAsAPlainCuia()
        {
            // 02 R16 para D-007: una Hoja ya abierta es una cuia normal con el mismo contenido.
            Cuia hoja = Cuia.Empty(4).WithMechanic(MechanicType.Hoja, key: 4, flags: Cuia.FlagClosed);
            Cuia filled = hoja.AddOnTop(4, 3);

            Assert.That(hoja.WithoutMechanic(), Is.EqualTo(Cuia.Empty(4)));
            Assert.That(filled.WithoutMechanic().Packed, Is.EqualTo(C(4, 4, 4, 4).Packed));
            Assert.That(C(4, 2).WithMechanic(MechanicType.None), Is.EqualTo(C(4, 2)));
        }

        [Test]
        public void WithMechanic_RejectsFieldsOutOfRange()
        {
            Cuia cuia = Cuia.Empty(4);

            Assert.That(() => cuia.WithMechanic((MechanicType)((int)MechanicTypes.Last + 1)), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => cuia.WithMechanic(MechanicType.Hoja, key: 17), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => cuia.WithMechanic(MechanicType.Remolino, pairId: 8), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => cuia.WithMechanic(MechanicType.Liana, n: -1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => cuia.WithMechanic(MechanicType.Pirana, k: 8), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => cuia.WithMechanic(MechanicType.Pirana, counter: 8), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => cuia.WithMechanic(MechanicType.Hoja, flags: 4), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => cuia.WithMechanic(MechanicType.None, key: 1), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void ToString_ShowsTheMechanicAndItsNonZeroFields()
        {
            Assert.That(C(4, 3, 1).ToString(), Is.EqualTo("Cuia(4: 3 1)"));
            Assert.That(Cuia.Empty(4).WithMechanic(MechanicType.Hoja, key: 4, flags: Cuia.FlagClosed).ToString(), Is.EqualTo("Cuia(4: | hoja key=4 flags=1)"));
            Assert.That(C(2).WithMechanic(MechanicType.Pirana, k: 4, counter: 2).ToString(), Is.EqualTo("Cuia(2: | pirana k=4 counter=2)"));
        }

        [Test]
        public void MechanicTypes_ConvertTheJsonKeys()
        {
            // 08 §4.1 attrs.type; el Barro no es un type (02 §9.1 M5). La tinaja y su cuia (M13) son 8 y 9, y la Hoja atada y la
            // Flor de vitória-régia (b.3; firma de pack_schema 3, pregunta 8) son 10 y 11 [P], sin huecos.
            string[] keys = { "hoja", "brote", "liana", "matapi", "remolino", "lluvia", "pirana", "tinaja", "cucharon", "hoja_atada", "flor" };
            Assert.That(keys.Length, Is.EqualTo((int)MechanicTypes.Last));
            for (int i = 0; i < keys.Length; i++)
            {
                Assert.That(MechanicTypes.TryParse(keys[i], out MechanicType type), Is.True);
                Assert.That(type, Is.EqualTo((MechanicType)(i + 1)));
                Assert.That(MechanicTypes.ToKey(type), Is.EqualTo(keys[i]));
            }

            Assert.That(MechanicTypes.TryParse("barro", out MechanicType mud), Is.False);
            Assert.That(mud, Is.EqualTo(MechanicType.None));
            Assert.That(MechanicTypes.TryParse("Hoja", out _), Is.False);
            Assert.That(MechanicTypes.TryParse(null, out _), Is.False);
            Assert.That(() => MechanicTypes.ToKey(MechanicType.None), Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
