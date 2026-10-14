#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// Validación de la Tinaja y su cuia (M13; D-019, Q-Ti1 A; propuesta de la tinaja, b.2 regla 1 y «Combinaciones», c.1):
    /// las dos comparaciones de capacidades de 02 §8, las excepciones a Q14 A y a T3, el tope propio de ocultas, las
    /// combinaciones prohibidas y las reglas propias de las dos piezas. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class TinajaValidationTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static Cuia T(int hiddenMask, params int[] colorsBottomToTop)
        {
            return Cuia.Create(LevelValidation.TinajaCapacity, colorsBottomToTop, hiddenMask).WithMechanic(MechanicType.Tinaja);
        }

        private static Cuia Ch()
        {
            return Cuia.Empty(LevelValidation.CucharonCapacity).WithMechanic(MechanicType.Cucharon);
        }

        private static BoardState Board(params Cuia[] cuias)
        {
            return new BoardState(cuias);
        }

        [Test]
        public void TheExampleOfB2_AndTheDebutS0_AreValid()
        {
            BoardBuildResult example = TinajaTests.BuildB2Example(new MechanicSpec(MechanicType.Tinaja), new MechanicSpec(MechanicType.Cucharon));

            Assert.That(example.Error, Is.Null);
            Assert.That(LevelValidation.Validate(example.State!), Is.Null);
            Assert.That(LevelValidation.Validate(TinajaTests.DebutS0()), Is.Null);
        }

        [Test]
        public void Tinaja_DoesNotCountAsACuiaAlta_WithoutItsAbundantFruit_028()
        {
            // c.1, primera comparación de 02 §8 (cuiasWithCap[v] > colorsWithUnits[v]): la tinaja de 5 no es una Cuia alta que
            // llegue con su fruta abundante. La misma cuia de 5 sin la mecánica sí se rechaza.
            Assert.That(LevelValidation.Validate(TinajaTests.DebutS0()), Is.Null);

            BoardState plain = Board(C(4, 3, 3, 1), C(4, 1, 2, 1), C(4, 2, 2, 2), C(5, 3, 1, 3), C(2));
            Assert.That(LevelValidation.Validate(plain), Does.Contain("fruta abundante"));
        }

        [Test]
        public void Tinaja_IsNotTheHomeOfAnAbundantFruit_028()
        {
            // c.1, segunda comparación de 02 §8 (cuiasWithCap[v] < colorsWithUnits[v]): con una fruta de U = 5 y sin Cuia alta,
            // la tinaja no pasa por su casa (nunca recibe), así que esa fruta no puede sellar y el nivel se rechaza.
            BoardState state = Board(C(4, 3, 3, 1), C(4, 1, 2, 1), C(4, 2, 2, 2, 3), T(0x3, 3, 1, 3), Ch());

            Assert.That(state.UnitsOf(3), Is.EqualTo(5));
            Assert.That(LevelValidation.Validate(state), Does.Contain("1 colores con U_c = 5 y solo 0 cuias de capacidad 5"));
        }

        [Test]
        public void Tinaja_IsTheExceptionToQ14AAndT3_WithItsOwnHiddenCap_QTi1()
        {
            // Q-Ti1 A: la tinaja lleva ocultas aunque tenga mecánica (excepción a Q14 A) y puede empezar llena con 4 ocultas
            // (excepción a T3: «≤ 3 dosis y nunca llena»). Su tope es propio (4), no lvl_murky_max_*: aunque Remote Config
            // bajara Aguas turbias a 1 cuia y 2 ocultas, la tinaja ya publicada sigue siendo válida.
            BoardState full = Board(C(4, 3, 3, 3), C(4, 1, 2), C(4, 2, 1), T(0xF, 1, 2, 1, 2, 3), Ch());

            Assert.That(full[3].IsFull, Is.True);
            Assert.That(LevelValidation.Validate(full), Is.Null);
            Assert.That(LevelValidation.Validate(full, true, 1, 2), Is.Null);
        }

        [Test]
        public void Tinaja_IsTheOnlySourceOfHiddenDoses()
        {
            // b.2, combinaciones prohibidas: otras cuias turbias (la tinaja es la única fuente de ocultas).
            BoardState state = Board(C(4, 3, 3, 1), Cuia.Create(4, new[] { 1, 2, 1 }, 0x1), C(4, 2, 2, 2), T(0x3, 3, 1, 3), Ch());

            Assert.That(LevelValidation.Validate(state), Does.Contain("única fuente de dosis ocultas"));
        }

        [Test]
        public void Tinaja_WithLianaRemolinoLluviaOrPirana_IsRejected_Q14A()
        {
            // b.2, combinaciones prohibidas: Liana, Remolino, Lluvia y Piraña («LevelValidation ya lo prohíbe con ocultos»). La
            // única comprobación es la de Aguas turbias (MurkyError, antes de las reglas de cada mecánica), que en un nivel con
            // tinaja da su propio motivo: se fija el texto entero para que un cambio de fuente o de orden no pase sin verse.
            Cuia liana = C(4, 1).WithMechanic(MechanicType.Liana, key: 2);
            Cuia remolino = Cuia.Empty(4).WithMechanic(MechanicType.Remolino, pairId: 0);
            Cuia lluvia = Cuia.Empty(4).WithMechanic(MechanicType.Lluvia, key: LevelValidation.TapajosWater, n: 2, k: 3);
            Cuia pirana = Cuia.Empty(2).WithMechanic(MechanicType.Pirana, k: 3);

            Assert.That(
                LevelValidation.Validate(Board(C(4, 3, 3, 1), C(4, 1, 2), C(4, 2, 2, 2), T(0x3, 3, 1, 3), Ch(), liana)),
                Is.EqualTo("M13 (b.2, combinaciones; Q14 A): la tinaja no se combina con liana (cuia 5)"));
            Assert.That(
                LevelValidation.Validate(Board(C(4, 3, 3, 1), C(4, 1, 2, 1), C(4, 2, 2, 2), T(0x3, 3, 1, 3), Ch(), remolino, remolino)),
                Is.EqualTo("M13 (b.2, combinaciones; Q14 A): la tinaja no se combina con remolino (cuia 5)"));
            Assert.That(
                LevelValidation.Validate(Board(C(4, 3, 3, 1), C(4, 1, 2, 1), C(4, 2, 2, 2), T(0x3, 3, 1, 3), Ch(), C(4, 10, 10), lluvia)),
                Is.EqualTo("M13 (b.2, combinaciones; Q14 A): la tinaja no se combina con lluvia (cuia 6)"));
            Assert.That(
                LevelValidation.Validate(Board(C(4, 3, 3, 1), C(4, 1, 2, 1), C(4, 2, 2, 2), T(0x3, 3, 1, 3), Ch(), pirana)),
                Is.EqualTo("M13 (b.2, combinaciones; Q14 A): la tinaja no se combina con pirana (cuia 5)"));

            // Sin tinaja, el motivo de Aguas turbias no cambia (packs 1-200).
            Assert.That(
                LevelValidation.Validate(Board(C(4, 3, 3, 1), Cuia.Create(4, new[] { 1, 2, 1 }, 0x1), C(4, 2, 2, 2), C(4, 3, 3), liana)),
                Is.EqualTo("D-007, Q14 A: Aguas turbias no se combina con liana (cuia 4)"));
        }

        [Test]
        public void Tinaja_WithFilters_IsValid_Q14A()
        {
            // b.2, combinaciones permitidas (solo filtros): Hoja, Brote, Matapi y Barro fuera de la pareja.
            Cuia hoja = Cuia.Empty(4).WithMechanic(MechanicType.Hoja, key: 1, flags: Cuia.FlagClosed);
            Cuia brote = C(4, 1, 2, 1).WithMechanic(MechanicType.Brote, flags: Cuia.FlagClosed);
            Cuia matapi = Cuia.Empty(4).WithMechanic(MechanicType.Matapi);

            Assert.That(LevelValidation.Validate(Board(C(4, 3, 3, 1), C(4, 1, 2, 1), C(4, 2, 2, 2), T(0x3, 3, 1, 3), Ch(), hoja)), Is.Null);
            Assert.That(LevelValidation.Validate(Board(C(4, 3, 3, 1), brote, C(4, 2, 2, 2), T(0x3, 3, 1, 3), Ch())), Is.Null);
            Assert.That(LevelValidation.Validate(Board(C(4, 3, 3, 1), C(4, 1, 2, 1), C(4, 2, 2, 2), T(0x3, 3, 1, 3), Ch(), matapi)), Is.Null);
            Assert.That(LevelValidation.Validate(Board(C(4, 3, 3, 1), C(4, 1, Cuia.MudColor, 2, 1), C(4, 2, 2, 2), T(0x3, 3, 1, 3), Ch())), Is.Null);
        }

        [Test]
        public void Tinaja_OwnRules_c1()
        {
            // c.1 y b.2 regla 1, con el motivo exacto que da Validate en cada caso y la comprobación que lo rechaza:
            // TinajaMechanic.ValidateInitial (capacidad 5, 3-5 dosis, ocultas = todas menos la boca, una sola tinaja y con su
            // cuia); LevelValidation.MurkyError (T4 en la columna: con la máscara de la tinaja, todas sus dosis menos la boca
            // están ocultas) y LevelValidation.MudError (sin Barro: en su cuia de 5, «el Barro va en una cuia de 4»), que van
            // antes que ValidateInitial y son las únicas comprobaciones de esas dos reglas.
            Cuia four = Cuia.Create(4, new[] { 3, 1, 3 }, 0x3).WithMechanic(MechanicType.Tinaja);
            Assert.That(
                LevelValidation.Validate(Board(C(4, 3, 3, 1), C(4, 1, 2, 1), C(4, 2, 2, 2), four, Ch(), C(4))),
                Is.EqualTo("cuia 3 (tinaja): M13 (regla 1): la tinaja es de capacidad 5"));
            Assert.That(
                LevelValidation.Validate(Board(C(4, 3, 3, 1, 3), C(4, 1, 2, 1), C(4, 2, 2, 2), T(0x1, 1, 3), Ch())),
                Is.EqualTo("cuia 3 (tinaja): M13 (regla 1): la tinaja empieza con 3-5 dosis"));
            Assert.That(
                LevelValidation.Validate(Board(C(4, 3, 3, 1), C(4, 1, 2, 1), C(4, 2, 2, 2), T(0x1, 3, 1, 3), Ch())),
                Is.EqualTo("cuia 3 (tinaja): M13 (regla 1): todas las dosis de la tinaja están ocultas salvo la boca"));
            Assert.That(
                LevelValidation.Validate(Board(C(4, 3, 3, 1), C(4, 2, 1), C(4, 2, 2, 2), T(0x3, 3, 1, 1), Ch(), C(4, 3))),
                Is.EqualTo("cuia turbia 3: 02 T4: la dosis oculta 1 repite el color de la de encima"));
            Assert.That(
                LevelValidation.Validate(Board(C(4, 3, 3, 3), C(4, 2, 2), C(4, 2, 1), T(0xF, 1, 1, 2, 1, 3), Ch())),
                Is.EqualTo("cuia turbia 3: 02 T4: la dosis oculta 0 repite el color de la de encima")); // llena: toda la columna
            Assert.That(
                LevelValidation.Validate(Board(C(4, 3, 3, 1), C(4, 1, 2, 1), C(4, 2, 2, 2), T(0x3, Cuia.MudColor, 1, 3), Ch(), C(4, 3))),
                Is.EqualTo("cuia 3, capa 0: 02 §9.1 M5: el Barro va en una cuia de 4"));
            Assert.That(
                LevelValidation.Validate(Board(C(4, 3, 3, 1), C(4, 1, 2, 1), C(4, 2, 2, 2), T(0x3, 3, 1, 3), C(2), C(4))),
                Is.EqualTo("cuia 3 (tinaja): M13 (regla 1): la tinaja lleva su cuia chica, una sola (hay 0)"));
            Assert.That(
                LevelValidation.Validate(Board(C(4, 3, 3), C(4, 2, 1), C(4, 2, 2), T(0x3, 3, 1, 3), Ch(), C(4), T(0x3, 1, 2, 1), Ch())),
                Is.EqualTo("cuia 3 (tinaja): M13 (regla 1): una sola tinaja por nivel (otra en la cuia 6)"));

            // b.2 regla 1, «una sola tinaja con su cuia»: con dos cuias de tinaja, la tinaja (que va antes) cuenta 2.
            Assert.That(
                LevelValidation.Validate(Board(C(4, 3, 3, 1), C(4, 1, 2, 1), C(4, 2, 2, 2), T(0x3, 3, 1, 3), Ch(), C(4), Ch())),
                Is.EqualTo("cuia 3 (tinaja): M13 (regla 1): la tinaja lleva su cuia chica, una sola (hay 2)"));
        }

        [Test]
        public void Cucharon_OwnRules_c1()
        {
            // c.1, CucharonMechanic.ValidateInitial: Cuia chica de 2, vacía al empezar, una sola y contigua a la tinaja.
            Cuia big = Cuia.Empty(3).WithMechanic(MechanicType.Cucharon);
            Cuia full = C(2, 1).WithMechanic(MechanicType.Cucharon);
            Assert.That(LevelValidation.Validate(Board(C(4, 3, 3, 1), C(4, 1, 2, 1), C(4, 2, 2, 2), T(0x3, 3, 1, 3), big)), Does.Contain("Cuia chica de 2"));
            Assert.That(LevelValidation.Validate(Board(C(4, 3, 3, 1), C(4, 2, 1), C(4, 2, 2, 2), T(0x3, 3, 1, 3), full)), Does.Contain("empieza vacía"));
            Assert.That(LevelValidation.Validate(Board(C(4, 3, 3, 1), C(4, 1, 2, 1), Ch(), C(4, 2, 2, 2), T(0x3, 3, 1, 3))), Does.Contain("contigua"));
            Assert.That(LevelValidation.Validate(Board(C(4, 3, 3, 1), C(4, 1, 2, 1), C(4, 2, 2, 2), Ch(), C(4, 1, 3, 3))), Does.Contain("una sola por nivel (hay 0)"));

            // b.2 regla 1, «una sola tinaja con su cuia»: dos cuias de tinaja, las dos contiguas a ella (Ch, T, Ch); la primera
            // (que va antes que la tinaja) ve la otra.
            Assert.That(
                LevelValidation.Validate(Board(C(4, 3, 3, 1), C(4, 1, 2, 1), C(4, 2, 2, 2), Ch(), T(0x3, 3, 1, 3), Ch())),
                Is.EqualTo("cuia 3 (cucharon): M13 (regla 1): una sola cuia de tinaja por nivel (otra en la cuia 5)"));
        }
    }
}
