#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// Validación de Aguas turbias en <see cref="LevelValidation"/> (C2-1): 02 §11 T2-T4 con los límites de 08 §9
    /// (<c>lvl_murky_max_cuias</c> = 3, <c>lvl_murky_max_hidden_units</c> = 4) como parámetros, ocultos solo en cuias sin
    /// mecánica, y las combinaciones de D-007 (Q14 A: sí con los filtros Hoja, Brote, Matapi y Barro; no con Liana, Lluvia ni
    /// Piraña) y [P] (no con Remolino; el Barro nunca va oculto). T1 y Q9 son del pipeline. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class MurkyValidationTests
    {
        private const int Mud = Cuia.MudColor;

        /// <summary>
        /// [1, 2, 3], [2, 3, 1], [3, 1, 2], <paramref name="fourth"/> (por defecto [1, 2, 3]), vacía y
        /// <paramref name="sixth"/> (por defecto vacía): colores 1-3 con U = 4. <paramref name="masks"/> son las máscaras de
        /// ocultos de las cuias 0-3.
        /// </summary>
        private static BoardState Board(int[] masks, Cuia? fourth = null, Cuia? fifth = null, Cuia? sixth = null)
        {
            int[][] doses = { new[] { 1, 2, 3 }, new[] { 2, 3, 1 }, new[] { 3, 1, 2 } };
            var cuias = new Cuia[6];
            for (int i = 0; i < 3; i++)
            {
                cuias[i] = Cuia.Create(4, doses[i], masks[i]);
            }

            cuias[3] = fourth ?? Cuia.Create(4, new[] { 1, 2, 3 }, masks[3]);
            cuias[4] = fifth ?? Cuia.Empty(4);
            cuias[5] = sixth ?? Cuia.Empty(4);
            return new BoardState(cuias);
        }

        private static BoardState Valid()
        {
            // 3 cuias turbias y 4 dosis ocultas: el máximo por defecto (02 T2).
            return Board(new[] { 0x3, 0x1, 0x2, 0 });
        }

        [Test]
        public void T2_T3_T4_AValidMurkyLevel_Passes()
        {
            BoardState state = Valid();

            Assert.That(state.HiddenCount, Is.EqualTo(4));
            Assert.That(LevelValidation.Validate(state), Is.Null);
            Assert.That(LevelValidation.Validate(state, true, LevelValidation.DefaultMurkyMaxCuias, LevelValidation.DefaultMurkyMaxHiddenUnits), Is.Null);
        }

        [Test]
        public void T2_AtMostThreeMurkyCuias_021_1()
        {
            // 02 T2: lvl_murky_max_cuias = 3 por defecto; el límite entra como parámetro (08 §9).
            Assert.That(LevelValidation.Validate(Board(new[] { 0x1, 0x1, 0x1, 0x1 })), Does.Contain("lvl_murky_max_cuias"));
            Assert.That(LevelValidation.Validate(Valid(), true, 2, 4), Does.Contain("lvl_murky_max_cuias"));
        }

        [Test]
        public void T2_AtMostFourHiddenDoses_021_1()
        {
            // 02 T2: lvl_murky_max_hidden_units = 4 por defecto (≤ 24 realidades).
            Assert.That(LevelValidation.Validate(Board(new[] { 0x3, 0x3, 0x2, 0 })), Does.Contain("lvl_murky_max_hidden_units"));
            Assert.That(LevelValidation.Validate(Valid(), true, 3, 3), Does.Contain("lvl_murky_max_hidden_units"));
            Assert.That(LevelValidation.Validate(Board(new[] { 0x3, 0x3, 0x2, 0 }), true, 3, 5), Is.Null);
        }

        [Test]
        public void T3_AMurkyCuiaHasAtMostThreeDoses_AndIsNeverFull_021_1()
        {
            // 02 T3: «como mucho 3 dosis al inicio (nunca llena)».
            var four = new BoardState(new[]
            {
                Cuia.Create(4, new[] { 1, 2, 3, 1 }, 0x1), Cuia.Create(4, new[] { 2, 3, 1 }), Cuia.Create(4, new[] { 3, 1, 2 }),
                Cuia.Create(4, new[] { 2, 3 }), Cuia.Empty(4), Cuia.Empty(4),
            });
            Assert.That(LevelValidation.Validate(four), Does.Contain("T3"));

            BoardState fullSmall = Board(new[] { 0, 0, 0, 0 }, Cuia.Create(3, new[] { 1, 2, 3 }, 0x1));
            Assert.That(LevelValidation.Validate(fullSmall), Does.Contain("T3"));
        }

        [Test]
        public void T4_AHiddenDoseNeverRepeatsTheColorOfTheDoseAbove_021_1()
        {
            // 02 T4: bajo una dosis oculta nunca va el color de la dosis de encima (aunque la de encima también esté oculta).
            Assert.That(LevelValidation.Validate(RepeatedColors(0x1)), Does.Contain("T4"));
            Assert.That(LevelValidation.Validate(RepeatedColors(0x3)), Does.Contain("T4"));
            Assert.That(LevelValidation.Validate(RepeatedColors(0x2)), Is.Null); // [2, 2*, 3]: el 2 oculto va bajo un 3
        }

        /// <summary>[2, 2, 3] con la máscara <paramref name="mask"/>, [1, 3, 1], [3, 1, 2], [1, 2, 3] y dos vacías.</summary>
        private static BoardState RepeatedColors(int mask)
        {
            return new BoardState(new[]
            {
                Cuia.Create(4, new[] { 2, 2, 3 }, mask), Cuia.Create(4, new[] { 1, 3, 1 }), Cuia.Create(4, new[] { 3, 1, 2 }),
                Cuia.Create(4, new[] { 1, 2, 3 }), Cuia.Empty(4), Cuia.Empty(4),
            });
        }

        [Test]
        public void HiddenDoses_OnlyInCuiasWithoutMechanic_0211()
        {
            // 02 §11 con D-007 (Q14 A): un filtro puede ir en el nivel, pero las dosis ocultas van en cuias sin mecánica.
            Cuia brote = Cuia.Create(4, new[] { 1, 2, 3 }, 0x1).WithMechanic(MechanicType.Brote, flags: Cuia.FlagClosed);
            Assert.That(LevelValidation.Validate(Board(new[] { 0, 0, 0, 0 }, brote)), Does.Contain("sin mecánica"));
        }

        [Test]
        public void Q14A_MurkyGoesWithTheFilters_HojaBroteMatapiAndBarro()
        {
            // D-007, Q14 A: los filtros (Hoja, Brote, Matapi, Barro) pueden ir con Aguas turbias.
            Cuia brote = Cuia.Create(4, new[] { 1, 2, 3 }).WithMechanic(MechanicType.Brote, flags: Cuia.FlagClosed);
            Cuia hoja = Cuia.Empty(4).WithMechanic(MechanicType.Hoja, key: 1, flags: Cuia.FlagClosed);
            Cuia matapi = Cuia.Empty(4).WithMechanic(MechanicType.Matapi);
            Cuia mud = Cuia.Create(4, new[] { 1, Mud, 2, 3 });
            int[] masks = { 0x3, 0x1, 0x2, 0 };

            Assert.That(LevelValidation.Validate(Board(masks, brote, hoja, matapi)), Is.Null);
            Assert.That(LevelValidation.Validate(Board(masks, mud)), Is.Null);
        }

        [TestCase(MechanicType.Liana)]
        [TestCase(MechanicType.Lluvia)]
        [TestCase(MechanicType.Pirana)]
        public void Q14A_MurkyNeverGoesWithTriggersOrCounters(MechanicType type)
        {
            // D-007, Q14 A: ni disparadores (Liana) ni contadores (Lluvia, Piraña).
            Cuia other = type == MechanicType.Liana ? Cuia.Empty(4).WithMechanic(type, key: 1)
                : type == MechanicType.Lluvia ? Cuia.Empty(4).WithMechanic(type, key: 1, n: 1, k: 3)
                : Cuia.Empty(4).WithMechanic(type, k: 4);

            Assert.That(LevelValidation.Validate(Board(new[] { 0x3, 0, 0, 0 }, fifth: other)), Does.Contain("Q14 A"));
        }

        [Test]
        public void MurkyNeverGoesWithTheRemolino_P()
        {
            // [P: propuesta de D-007 b.2, Aguas turbias «solo junto a filtros»].
            Cuia remolino = Cuia.Empty(4).WithMechanic(MechanicType.Remolino, pairId: 0);
            BoardState state = Board(new[] { 0x3, 0, 0, 0 }, fifth: remolino, sixth: remolino);

            Assert.That(LevelValidation.Validate(state), Does.Contain("Remolino"));
        }

        [Test]
        public void TheMudIsNeverHidden_P()
        {
            // [P] restricción de generación: el Barro nunca va oculto.
            BoardState state = Board(new[] { 0, 0, 0, 0 }, Cuia.Create(4, new[] { 1, Mud, 2, 3 }, 0x2));

            Assert.That(LevelValidation.Validate(state), Does.Contain("nunca va oculto"));
        }
    }
}
