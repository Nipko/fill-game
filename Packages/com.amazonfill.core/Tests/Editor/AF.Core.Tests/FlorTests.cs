#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// Flor de vitória-régia (K3b; variante de la Liana con fruta de la 1.0 con debut en el 403; D-007, propuesta de D-007 b.3;
    /// 02 §1.2, R10 (d) y §9.1; firma de <c>pack_schema</c> 3, pregunta 8: <c>flor {key, key2}</c> con key ≠ key2): cerrada no es
    /// origen ni destino y se abre cuando existen cuias selladas de sus dos frutas. El solver (A* frente a la búsqueda exhaustiva
    /// y heurística) está en <c>LianaSolverTests</c> y P1-P3, en <c>DotnetOnly/FutilePropertyTests</c>. Test compartido (NUnit
    /// 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class FlorTests
    {
        /// <summary>Agua del Tapajós (02 §9.1 M6).</summary>
        private const int W = LevelValidation.TapajosWater;

        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static Cuia Flor(int key, int key2, int capacity, params int[] colorsBottomToTop)
        {
            return C(capacity, colorsBottomToTop).WithMechanic(MechanicType.Flor, key: key, key2: key2);
        }

        private static MechanicSpec[] Attrs(int count, int index, MechanicSpec spec)
        {
            var attrs = new MechanicSpec[count];
            attrs[index] = spec;
            return attrs;
        }

        /// <summary>Flor (1, 2) que guarda [3, 3]; 1 → 2 sella el 1 y después 1 → 3 sella el 2.</summary>
        private static BoardState FlorBoard()
        {
            return new BoardState(new[] { Flor(1, 2, 4, 3, 3), C(4, 2, 1), C(4, 1, 1, 1), C(4, 2, 2, 2), C(4, 3, 3), Cuia.Empty(4) });
        }

        [Test]
        public void R_0212_ClosedFlor_IsNeitherSourceNorTarget_R11()
        {
            // 02 §1.2: «la Flor cerrada, como la Liana» en bloqueada_origen y bloqueada_destino; un toque es ilegal (R11).
            BoardState state = FlorBoard();

            Assert.That(PourRules.CanBeSource(state, 0), Is.False);
            for (int a = 1; a < state.Count; a++)
            {
                Assert.That(PourRules.IsLegal(state, a, 0), Is.False);
                Assert.That(PourRules.IsLegal(state, 0, a), Is.False);
            }
        }

        [Test]
        public void R10d_OneFruitSealed_StaysClosed_BothSealed_OpensInTheSamePour()
        {
            // b.3: «se abre cuando existen cuias selladas de las dos frutas» (lógica Y). (c) sella la segunda y (d) la abre en el
            // mismo vertido; la cuia queda normal (02 R16 para D-007) y ya puede ser origen. No es una Liana: no sale en
            // LianaReleasedMask.
            BoardState state = FlorBoard();

            BoardState one = PourRules.Apply(state, 1, 2, PourMode.Run, out PourEvents first);
            Assert.That(first.WasSealed(2), Is.True);
            Assert.That(first.ReleasedMask, Is.EqualTo(0));
            Assert.That(one[0].Mechanic, Is.EqualTo(MechanicType.Flor));
            Assert.That(PourRules.CanBeSource(one, 0), Is.False);

            BoardState two = PourRules.Apply(one, 1, 3, PourMode.Run, out PourEvents second);
            Assert.That(second.WasSealed(3), Is.True);
            Assert.That(second.ReleasedMask, Is.EqualTo(1 << 0));
            Assert.That(second.WasUnlocked(0), Is.True);
            Assert.That(second.LianaReleasedMask, Is.EqualTo(0));
            Assert.That(two[0].Mechanic, Is.EqualTo(MechanicType.None));
            Assert.That(two[0].Packed, Is.EqualTo(C(4, 3, 3).Packed));
            Assert.That(PourRules.CanBeSource(two, 0), Is.True);

            BoardState won = PourRules.Apply(two, 0, 4, PourMode.Run);
            Assert.That(won.IsWon(), Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void R10d_EitherOrder_AndSealsOfOtherFruits_OnlyTheSecondKeyOpensIt(bool keyFirst)
        {
            // Lógica Y sin orden: se abre con el sellado de la segunda llave, sea key o key2. Sellar otra fruta (el 4) no la abre.
            var state = new BoardState(new[]
            {
                Flor(1, 2, 4, 3, 3), C(4, 1, 1, 1), C(4, 1), C(4, 2, 2, 2), C(4, 2), C(4, 3, 3), C(4, 4, 4, 4), C(4, 4), Cuia.Empty(4),
            });

            BoardState other = PourRules.Apply(state, 7, 6, PourMode.Run, out PourEvents fourth);
            Assert.That(fourth.WasSealed(6), Is.True);
            Assert.That(fourth.ReleasedMask, Is.EqualTo(0));

            PourEvents first;
            BoardState one = keyFirst ? PourRules.Apply(other, 2, 1, PourMode.Run, out first) : PourRules.Apply(other, 4, 3, PourMode.Run, out first);
            Assert.That(first.SealedMask, Is.Not.EqualTo(0));
            Assert.That(first.ReleasedMask, Is.EqualTo(0));
            Assert.That(one[0].Mechanic, Is.EqualTo(MechanicType.Flor));

            PourEvents second;
            BoardState two = keyFirst ? PourRules.Apply(one, 4, 3, PourMode.Run, out second) : PourRules.Apply(one, 2, 1, PourMode.Run, out second);
            Assert.That(second.ReleasedMask, Is.EqualTo(1 << 0));
            Assert.That(two[0].Mechanic, Is.EqualTo(MechanicType.None));
        }

        [Test]
        public void AnexoB_ADropThatSealsTheSecondFruit_OpensTheFlorInTheSamePour()
        {
            // Como el test obligatorio del anexo B para la Liana: el vertido sella el 2 (c) y la última gota sella el agua (e);
            // el punto fijo (f′) marca el sellado y (d) abre la flor (W, 2) en el mismo vertido.
            Cuia rain = C(4, W, W, W).WithMechanic(MechanicType.Lluvia, key: W, n: 1, k: 2);
            var state = new BoardState(new[] { rain, Flor(W, 2, 4, 1, 1), C(4, 2, 1, 1), C(4, 2, 2, 2), Cuia.Empty(4) });

            BoardState first = PourRules.Apply(state, 2, 4, PourMode.Run, out PourEvents one);
            Assert.That(one.Futile, Is.False);
            BoardState after = PourRules.Apply(first, 2, 3, PourMode.Run, out PourEvents events);

            Assert.That(events.LluviaDropMask, Is.EqualTo(1 << 0));
            Assert.That(events.WasSealed(3), Is.True);
            Assert.That(events.WasSealed(0), Is.True);
            Assert.That(events.ReleasedMask, Is.EqualTo(1 << 1));
            Assert.That(events.FixedPointPasses, Is.GreaterThanOrEqualTo(1));
            Assert.That(after[1].Mechanic, Is.EqualTo(MechanicType.None));
            Assert.That(PourRules.CanBeSource(after, 1), Is.True);
        }

        [Test]
        public void R8_AMareaThatUndoesASeal_ClosesItAgain_AndRestartToo()
        {
            // Criterio de K3b: Marea restaura el estado anterior entero, así que deshacer el sellado de la segunda fruta la
            // vuelve a cerrar (se deduce del estado); Volver a la orilla también.
            BoardState initial = FlorBoard();
            var session = new LevelSession(initial, true);

            Assert.That(session.TryPour(1, 2, out _), Is.True);
            Assert.That(session.TryPour(1, 3, out PourResult result), Is.True);
            Assert.That(result.Events.ReleasedMask, Is.EqualTo(1 << 0));
            Assert.That(session.Current[0].Mechanic, Is.EqualTo(MechanicType.None));

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Current[0].Mechanic, Is.EqualTo(MechanicType.Flor));
            Assert.That(session.Current.IsSealed(2), Is.True);
            Assert.That(PourRules.CanBeSource(session.Current, 0), Is.False);

            Assert.That(session.TryPour(1, 3, out _), Is.True);
            Assert.That(session.TryPour(0, 4, out PourResult won), Is.True);
            Assert.That(won.Status, Is.EqualTo(LevelStatus.Won));
            session.Restart();
            Assert.That(session.Current.Equals(initial), Is.True);
        }

        [Test]
        public void Format_Flor_KeyKey2AndBuilder_PackSchema3Signature()
        {
            // Firma de pack_schema 3 (pregunta 8): {"type":"flor","key":c1,"key2":c2} con c1 ≠ c2; código 11 [P], que no se
            // persiste; sin bandera (se deduce del estado).
            Assert.That(MechanicTypes.TryParse("flor", out MechanicType type), Is.True);
            Assert.That(type, Is.EqualTo(MechanicType.Flor));
            Assert.That(MechanicTypes.ToKey(MechanicType.Flor), Is.EqualTo("flor"));
            Assert.That((int)MechanicType.Flor, Is.EqualTo(11));

            int[] caps = { 4, 4, 4, 4, 4 };
            int[][] cuias = { new[] { 1, 2, 1, 2 }, new[] { 2, 1, 2, 1 }, new[] { 3, 3 }, new[] { 3, 3 }, new int[0] };
            BoardBuildResult ok = BoardBuilder.Build(caps, cuias, Attrs(5, 2, new MechanicSpec(MechanicType.Flor, key: 1, key2: 2)));
            Assert.That(ok.Error, Is.Null);
            Cuia flor = ok.State![2];
            Assert.That(flor.Mechanic, Is.EqualTo(MechanicType.Flor));
            Assert.That(flor.MechanicKey, Is.EqualTo(1));
            Assert.That(flor.MechanicKey2, Is.EqualTo(2));
            Assert.That(flor.MechanicFlags, Is.EqualTo(0), "sin bandera: se deduce del estado");
            Assert.That(flor.MechanicN, Is.EqualTo(0));
            Assert.That(flor.MechanicK, Is.EqualTo(0));
            Assert.That(LevelValidation.Validate(ok.State), Is.Null);

            const string signature = "lleva solo key y key2, dos tintes distintos de 1 a 14 (propuesta de D-007 b.3";
            Assert.That(Build(caps, cuias, new MechanicSpec(MechanicType.Flor, key: 1)), Does.Contain(signature));
            Assert.That(Build(caps, cuias, new MechanicSpec(MechanicType.Flor, key2: 2)), Does.Contain(signature));
            Assert.That(Build(caps, cuias, new MechanicSpec(MechanicType.Flor, key: 1, key2: 1)), Does.Contain(signature));
            Assert.That(Build(caps, cuias, new MechanicSpec(MechanicType.Flor, key: 1, key2: 15)), Does.Contain(signature));
            Assert.That(Build(caps, cuias, new MechanicSpec(MechanicType.Flor, key: 1, key2: 2, n: 1)), Does.Contain(signature));
            Assert.That(Build(caps, cuias, new MechanicSpec(MechanicType.Flor, key: 1, key2: 2, k: 2)), Does.Contain(signature));
            Assert.That(Build(caps, cuias, new MechanicSpec(MechanicType.Flor, key: 1, partner: 0, key2: 2)), Does.Contain(signature));

            Assert.That(Build(caps, cuias, new MechanicSpec(MechanicType.Liana, key: 1, key2: 2)), Does.Contain("key2 solo lo lleva la Flor"));
            Assert.That(Build(caps, cuias, new MechanicSpec(MechanicType.None, key2: 2)), Does.Contain("campos sin type"));
        }

        [Test]
        public void Format_MechanicSpec_Key2_InEqualsHashAndToString()
        {
            var flor = new MechanicSpec(MechanicType.Flor, key: 3, key2: 7);

            Assert.That(flor.Key2, Is.EqualTo(7));
            Assert.That(flor.Equals(new MechanicSpec(MechanicType.Flor, key: 3, key2: 7)), Is.True);
            Assert.That(flor.Equals(new MechanicSpec(MechanicType.Flor, key: 3, key2: 8)), Is.False);
            Assert.That(flor.GetHashCode(), Is.EqualTo(new MechanicSpec(MechanicType.Flor, key: 3, key2: 7).GetHashCode()));
            Assert.That(flor.GetHashCode(), Is.Not.EqualTo(new MechanicSpec(MechanicType.Flor, key: 3, key2: 8).GetHashCode()));
            Assert.That(flor.ToString(), Is.EqualTo("{type:flor, key:3, key2:7}"));
            Assert.That(new MechanicSpec(MechanicType.Hoja, key: 4).ToString(), Is.EqualTo("{type:hoja, key:4}"));
        }

        [Test]
        public void Packed_Key2InTheBitsOfNAndK_EntersTheCanonicalHash_TheOtherMechanicsKeepTheirBits_033()
        {
            // K3b: key2 ocupa los bits 50-53 (los de n y k, que la Flor no usa), así que entra en Packed y en el hash canónico
            // (03 §3; BoardCanon ordena y hashea Packed). Las demás mecánicas conservan sus bits y su hash: vectores fijos
            // calculados con la distribución de bits anterior a K3b.
            Assert.That(Flor(1, 2, 4, 3, 3).Packed, Is.EqualTo(0x405C200000022UL));
            Assert.That(Flor(14, 13, 4, 3, 3).Packed, Is.EqualTo(0x306DC200000022UL));
            Assert.That(Flor(14, 13, 4, 3, 3).ToString(), Is.EqualTo("Cuia(4: 3 3 | flor key=14 key2=13)"));

            Assert.That(C(4, 2).WithMechanic(MechanicType.Lluvia, key: 10, n: 2, k: 3, counter: 1).Packed, Is.EqualTo(0x1684B4100000001UL));
            Assert.That(Cuia.Empty(4).WithMechanic(MechanicType.Liana, n: 2).Packed, Is.EqualTo(0x801C000000000UL));
            Assert.That(C(4, 2, 1).WithMechanic(MechanicType.Brote, n: 2, flags: Cuia.FlagClosed).Packed, Is.EqualTo(0x808014200000001UL));
            Assert.That(Cuia.Empty(2).WithMechanic(MechanicType.Pirana, k: 4, counter: 2).Packed, Is.EqualTo(0x28003A000000000UL));
            Assert.That(C(4, 2).WithMechanic(MechanicType.Lluvia, key: 10, n: 2).MechanicKey2, Is.EqualTo(Cuia.NoColor));

            BoardState a = new BoardState(new[] { Flor(1, 2, 4, 3, 3), C(4, 1, 2, 1, 2), C(4, 2, 1, 2, 1), C(4, 3, 3), Cuia.Empty(4) });
            BoardState b = new BoardState(new[] { Flor(1, 4, 4, 3, 3), C(4, 1, 2, 1, 2), C(4, 2, 1, 2, 1), C(4, 3, 3), Cuia.Empty(4) });
            Assert.That(a[0].Packed, Is.Not.EqualTo(b[0].Packed));
            Assert.That(a.CanonicalHash(), Is.Not.EqualTo(b.CanonicalHash()));
            Assert.That(a[0].WithoutMechanic(), Is.EqualTo(C(4, 3, 3)));
        }

        [Test]
        public void Cuia_WithMechanic_Key2OnlyInTheFlor_AndTheFlorTakesNoNOrK()
        {
            Cuia cuia = Cuia.Empty(4);

            Assert.That(() => cuia.WithMechanic(MechanicType.Liana, key: 1, key2: 2), Throws.TypeOf<ArgumentException>());
            Assert.That(() => cuia.WithMechanic(MechanicType.None, key2: 2), Throws.TypeOf<ArgumentException>());
            Assert.That(() => cuia.WithMechanic(MechanicType.Flor, key: 1, n: 1, key2: 2), Throws.TypeOf<ArgumentException>());
            Assert.That(() => cuia.WithMechanic(MechanicType.Flor, key: 1, k: 1, key2: 2), Throws.TypeOf<ArgumentException>());
            Assert.That(() => cuia.WithMechanic(MechanicType.Flor, key: 1, key2: 17), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Validation_DistinctKeysOnTheBoard_NotInside_AbleToSealOutsideAtOnce_0317_6()
        {
            // Firma de la pregunta 8 (key ≠ key2); 03 §17 #6 (llaves en el tablero); b.3, como la Liana con fruta (regla 4): sus
            // frutas no tienen dosis dentro y las dos pueden sellar fuera a la vez (una cuia de capacidad U_c para cada una).
            int[] caps = { 4, 4, 4, 4, 4 };
            int[][] cuias = { new[] { 1, 2, 1, 2 }, new[] { 2, 1, 2, 1 }, new[] { 3, 3 }, new[] { 3, 3 }, new int[0] };
            Assert.That(Validate(caps, cuias, 2, new MechanicSpec(MechanicType.Flor, key: 1, key2: 2)), Is.Null);
            Assert.That(Validate(caps, cuias, 4, new MechanicSpec(MechanicType.Flor, key: 2, key2: 3)), Is.Null);
            Assert.That(Validate(caps, cuias, 2, new MechanicSpec(MechanicType.Flor, key: 1, key2: 5)), Does.Contain("en el tablero"));
            Assert.That(Validate(caps, cuias, 0, new MechanicSpec(MechanicType.Flor, key: 1, key2: 3)), Does.Contain("dentro"));

            var sameKeys = new BoardState(new[] { Flor(1, 1, 4, 3, 3), C(4, 1, 2, 1, 2), C(4, 2, 1, 2, 1), C(4, 3, 3), Cuia.Empty(4) });
            Assert.That(LevelValidation.Validate(sameKeys), Does.Contain("key ≠ key2"));

            // Las dos llaves con U_c = 5 y una sola Cuia alta de 5 fuera de la flor (la otra es la propia flor): 02 §8 se cumple,
            // pero no pueden estar selladas a la vez fuera de ella.
            var oneHome = new BoardState(new[]
            {
                Flor(1, 2, 5, 3, 3), C(5, 1, 2, 1, 2, 1), C(4, 2, 1, 2, 1), C(4, 2, 3, 3), Cuia.Empty(4),
            });
            Assert.That(LevelValidation.Validate(oneHome), Does.Contain("a la vez"));
        }

        [Test]
        public void Validation_MixedCapacities_AreBlockedLikeTheLiana_Question24b()
        {
            // Pregunta 24 b de GDD §6.3 (C1-9): con capacidades mixtas P1 falla también con la Flor
            // (DotnetOnly/FutilePropertyTests.P1_Flor_MixedCapacities_OpenR16Counterexample), así que se bloquea como la Liana.
            var mixed = new BoardState(new[]
            {
                Flor(1, 2, 4, 3, 3), C(4, 1, 2, 1, 2), C(4, 2, 1, 2, 1), C(4, 3, 3), Cuia.Empty(4), Cuia.Empty(2),
            });
            Assert.That(LevelValidation.Validate(mixed), Does.Contain("bloqueo provisional"));
        }

        [Test]
        public void Validation_NoHiddenDoses_NoTinaja_ItIsATrigger_Q14A()
        {
            // 02 T1 (Aguas turbias «nunca con mecánicas con disparador (Liana, Flor, …)», Q14 A) y 02 §9.2 regla 7 (la Tinaja,
            // «solo con filtros y capacidades»).
            int[] caps = { 4, 4, 4, 4, 4 };
            int[][] cuias = { new[] { 1, 2, 1, 2 }, new[] { 2, 1, 2, 1 }, new[] { 3, 3 }, new[] { 4, 3, 3 }, new[] { 4, 4, 4 } };
            BoardState murky = BoardBuilder.Build(caps, cuias, Attrs(5, 2, new MechanicSpec(MechanicType.Flor, key: 1, key2: 2)), new[] { new[] { 3, 0 } }).State!;
            Assert.That(LevelValidation.Validate(murky), Does.Contain("Q14 A").And.Contain("flor"));

            Cuia tinaja = Cuia.Create(5, new[] { 4, 3, 4 }, 0x3).WithMechanic(MechanicType.Tinaja);
            Cuia hung = Cuia.Empty(2).WithMechanic(MechanicType.Cucharon);
            var withTinaja = new BoardState(new[]
            {
                Flor(1, 2, 4, 3, 3), C(4, 1, 2, 1, 2), C(4, 2, 1, 2, 1), C(4, 4, 4), C(4, 3), tinaja, hung,
            });
            Assert.That(LevelValidation.Validate(withTinaja), Does.Contain("la tinaja no se combina con flor"));
        }

        [Test]
        public void Validation_TheLevelDoesNotStartStuck_0292_Rule1()
        {
            // 02 §9.2 regla 1: con la flor cerrada, el 3 suelto no tiene adónde ir y el resto no tiene vertidos legales.
            var stuck = new BoardState(new[] { Flor(1, 2, 4, 3, 3, 3), C(4, 1, 2, 1, 2), C(4, 2, 1, 2, 1), C(4, 3) });
            Assert.That(PourRules.IsStuck(stuck, true), Is.True);
            Assert.That(LevelValidation.Validate(stuck), Does.Contain("Aguas estancadas"));
        }

        private static string? Build(int[] caps, int[][] cuias, MechanicSpec spec)
        {
            return BoardBuilder.Build(caps, cuias, Attrs(caps.Length, 2, spec)).Error;
        }

        private static string? Validate(int[] caps, int[][] cuias, int index, MechanicSpec spec)
        {
            BoardBuildResult built = BoardBuilder.Build(caps, cuias, Attrs(caps.Length, index, spec));
            Assert.That(built.Error, Is.Null);
            return LevelValidation.Validate(built.State!);
        }
    }
}
