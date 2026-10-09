#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// <see cref="BoardBuilder"/> (campos de <c>af-level/1</c>, 08 §4.1, y campos obligatorios por mecánica, 02 §9.1 y
    /// D-007) y <see cref="LevelValidation"/> (03 §17 #5; 02 §8; 02 §9.2 regla 1). Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class BoardBuilderTests
    {
        private static readonly int[] Caps4 = { 4, 4, 4, 4 };

        private static int[][] Doses(params int[][] cuias)
        {
            return cuias;
        }

        /// <summary>Tablero de 3 colores (U_c = 4) y 1 vacía, sin sellos ni bloqueo inicial.</summary>
        private static int[][] Simple()
        {
            return Doses(new[] { 1, 2, 3, 1 }, new[] { 2, 3, 1, 2 }, new[] { 3, 1, 2, 3 }, new int[0]);
        }

        private static BoardBuildResult BuildAny(int[] caps, int[][] cuias, MechanicSpec[]? attrs, int[][]? hidden = null)
        {
            // Formato de mecánicas aún no portadas: el núcleo las rechaza (fallar cerrado), aquí se prueba solo el formato.
            return BoardBuilder.Build(caps, cuias, attrs, hidden, null, BoardBuilder.DefaultLluviaKPours, BoardBuilder.DefaultPiranaKPours, false);
        }

        private static MechanicSpec[] Attrs(int count, int index, MechanicSpec spec)
        {
            var attrs = new MechanicSpec[count];
            attrs[index] = spec;
            return attrs;
        }

        [Test]
        public void PlainBoard_BuildsTheSameStateAsTheConstructor()
        {
            BoardBuildResult result = BoardBuilder.Build(Caps4, Simple());

            Assert.That(result.IsValid, Is.True, result.Error);
            Assert.That(result.Error, Is.Null);
            var expected = new BoardState(new[]
            {
                Cuia.Create(4, new[] { 1, 2, 3, 1 }), Cuia.Create(4, new[] { 2, 3, 1, 2 }), Cuia.Create(4, new[] { 3, 1, 2, 3 }), Cuia.Empty(4),
            });
            Assert.That(result.State!.Equals(expected), Is.True);
            Assert.That(result.State.UnitsOf(1), Is.EqualTo(4));
        }

        [Test]
        public void Hidden_IsPackedAsTheMaskOfEachCuia()
        {
            // 08 §4.1: hidden = lista [cuia, capa] (admite ocultos no contiguos).
            BoardBuildResult result = BoardBuilder.Build(Caps4, Simple(), null, new[] { new[] { 0, 0 }, new[] { 0, 2 }, new[] { 1, 1 } });

            Assert.That(result.IsValid, Is.True, result.Error);
            Assert.That(result.State![0].HiddenMask, Is.EqualTo(0x5));
            Assert.That(result.State[1].HiddenMask, Is.EqualTo(0x2));
        }

        [TestCase(0, 3, "R7")]
        [TestCase(3, 0, "no es una dosis")]
        [TestCase(5, 0, "no es una dosis")]
        public void Hidden_RejectsTheTopAndPositionsOutsideTheBoard(int cuia, int layer, string reason)
        {
            BoardBuildResult result = BoardBuilder.Build(Caps4, Simple(), null, new[] { new[] { cuia, layer } });

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Error, Does.Contain(reason));
        }

        [Test]
        public void Hidden_RejectsDuplicates()
        {
            BoardBuildResult result = BoardBuilder.Build(Caps4, Simple(), null, new[] { new[] { 0, 1 }, new[] { 0, 1 } });

            Assert.That(result.Error, Does.Contain("repite"));
        }

        [Test]
        public void Format_RejectsLengthsCapacitiesAndColors()
        {
            Assert.That(BoardBuilder.Build(new[] { 4, 4 }, Simple()).Error, Does.Contain("longitudes distintas"));
            Assert.That(BoardBuilder.Build(new[] { 4, 4, 7, 4 }, Simple()).Error, Does.Contain("capacidad"));
            Assert.That(BoardBuilder.Build(new[] { 4, 4, 3, 4 }, Simple()).Error, Does.Contain("no caben"));
            Assert.That(BoardBuilder.Build(Caps4, Doses(new[] { 1, 15 }, new[] { 1 }, new int[0], new int[0])).Error, Does.Contain("15 (Barro)"));
            Assert.That(BoardBuilder.Build(Caps4, Doses(new[] { 0 }, new int[0], new int[0], new int[0])).Error, Does.Contain("R2"));
            Assert.That(BoardBuilder.Build(Caps4, Simple(), new MechanicSpec[3]).Error, Does.Contain("attrs"));
        }

        [Test]
        public void UnitsPerColor_ObjectForm_IsCheckedAgainstTheContent()
        {
            // 08 §4.1: units_per_color 4 o un objeto {"13": 5} para la fruta abundante (02 §8).
            int[] caps = { 5, 4, 4 };
            int[][] cuias = Doses(new[] { 13, 13, 13, 1, 1 }, new[] { 1, 1, 13, 13 }, new int[0]);
            var overrides = new Dictionary<int, int> { { 13, 5 } };

            BoardBuildResult ok = BoardBuilder.Build(caps, cuias, null, null, UnitsPerColor.FromObject(overrides));
            BoardBuildResult wrong = BoardBuilder.Build(caps, cuias, null, null, UnitsPerColor.Uniform(4));

            Assert.That(ok.IsValid, Is.True, ok.Error);
            Assert.That(ok.State!.UnitsOf(13), Is.EqualTo(5));
            Assert.That(wrong.Error, Does.Contain("units_per_color"));
            Assert.That(UnitsPerColor.FromObject(overrides).Of(2), Is.EqualTo(UnitsPerColor.DefaultUnits));
        }

        [Test]
        public void Mechanics_NotYetPorted_AreRejected()
        {
            // Fallar cerrado: sin sus reglas, una mecánica se jugaría como una cuia normal.
            BoardBuildResult result = BoardBuilder.Build(Caps4, Simple(), Attrs(4, 3, new MechanicSpec(MechanicType.Pirana)));

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Error, Does.Contain("portada"));
        }

        [Test]
        public void Hoja_RequiresAKey_AndStartsClosed()
        {
            BoardBuildResult ok = BuildAny(Caps4, Simple(), Attrs(4, 3, new MechanicSpec(MechanicType.Hoja, key: 3)));
            BoardBuildResult noKey = BuildAny(Caps4, Simple(), Attrs(4, 3, new MechanicSpec(MechanicType.Hoja)));
            BoardBuildResult mud = BuildAny(Caps4, Simple(), Attrs(4, 3, new MechanicSpec(MechanicType.Hoja, key: 15)));

            Assert.That(ok.IsValid, Is.True, ok.Error);
            Cuia hoja = ok.State![3];
            Assert.That(hoja.Mechanic, Is.EqualTo(MechanicType.Hoja));
            Assert.That(hoja.MechanicKey, Is.EqualTo(3));
            Assert.That(hoja.MechanicFlags, Is.EqualTo(Cuia.FlagClosed));
            Assert.That(noKey.Error, Does.Contain("key"));
            Assert.That(mud.Error, Does.Contain("key"));
        }

        [Test]
        public void BroteAndMatapi_TakeNoFields()
        {
            BoardBuildResult brote = BuildAny(Caps4, Simple(), Attrs(4, 0, new MechanicSpec(MechanicType.Brote)));
            BoardBuildResult matapi = BuildAny(Caps4, Simple(), Attrs(4, 3, new MechanicSpec(MechanicType.Matapi)));
            BoardBuildResult extra = BuildAny(Caps4, Simple(), Attrs(4, 3, new MechanicSpec(MechanicType.Matapi, key: 1)));

            Assert.That(brote.State![0].MechanicFlags, Is.EqualTo(Cuia.FlagClosed));
            Assert.That(matapi.State![3].Mechanic, Is.EqualTo(MechanicType.Matapi));
            Assert.That(matapi.State[3].MechanicFlags, Is.EqualTo(0));
            Assert.That(extra.Error, Does.Contain("no lleva campos"));
        }

        [TestCase(0, 0, false)]
        [TestCase(2, 1, false)]
        [TestCase(2, 0, true)]
        [TestCase(0, 2, true)]
        [TestCase(0, 3, false)]
        public void Liana_TakesExactlyOneOfKeyOrN(int key, int n, bool valid)
        {
            // D-007: la Liana con fruta (key) debuta en el 83; la de cuenta (n = 1-2) pasa a variante.
            BoardBuildResult result = BuildAny(Caps4, Simple(), Attrs(4, 0, new MechanicSpec(MechanicType.Liana, key: key, n: n)));

            Assert.That(result.IsValid, Is.EqualTo(valid), result.Error);
        }

        [Test]
        public void Remolino_PartnerBecomesASymmetricPairId()
        {
            int[] caps = { 4, 4, 4, 4, 4, 4 };
            int[][] cuias = Doses(new[] { 1, 2 }, new[] { 2, 1 }, new[] { 1, 2 }, new[] { 2, 1 }, new[] { 1, 2, 1, 2 }, new int[0]);
            var attrs = new MechanicSpec[6];
            attrs[0] = new MechanicSpec(MechanicType.Remolino, partner: 3);
            attrs[3] = new MechanicSpec(MechanicType.Remolino, partner: 0);
            attrs[1] = new MechanicSpec(MechanicType.Remolino, partner: 2);
            attrs[2] = new MechanicSpec(MechanicType.Remolino, partner: 1);

            BoardBuildResult ok = BuildAny(caps, cuias, attrs);
            attrs[2] = new MechanicSpec(MechanicType.Remolino, partner: 0);
            BoardBuildResult asymmetric = BuildAny(caps, cuias, attrs);

            Assert.That(ok.IsValid, Is.True, ok.Error);
            Assert.That(ok.State![0].MechanicPairId, Is.EqualTo(0));
            Assert.That(ok.State[3].MechanicPairId, Is.EqualTo(0));
            Assert.That(ok.State[1].MechanicPairId, Is.EqualTo(1));
            Assert.That(ok.State[2].MechanicPairId, Is.EqualTo(1));
            Assert.That(asymmetric.Error, Does.Contain("pareja"));
        }

        [Test]
        public void LluviaAndPirana_TakeTheDefaultRhythm_AndCountPendingDrops()
        {
            // 08 §9: lluvia_k_pours / pirana_k_pours rigen si attrs no trae k; 02 R3: U_agua cuenta las gotas pendientes.
            int[] caps = { 4, 4, 4, 4, 2 };
            int[][] cuias = Doses(new[] { 1, 1, 10 }, new[] { 1, 1, 10 }, new int[0], new int[0], new int[0]);
            var attrs = new MechanicSpec[5];
            attrs[2] = new MechanicSpec(MechanicType.Lluvia, key: 10, n: 2);
            attrs[4] = new MechanicSpec(MechanicType.Pirana);

            BoardBuildResult ok = BuildAny(caps, cuias, attrs);
            attrs[4] = new MechanicSpec(MechanicType.Pirana, k: 6);
            BoardBuildResult tooSlow = BuildAny(caps, cuias, attrs);

            Assert.That(ok.IsValid, Is.True, ok.Error);
            Assert.That(ok.State![2].MechanicK, Is.EqualTo(BoardBuilder.DefaultLluviaKPours));
            Assert.That(ok.State[2].MechanicN, Is.EqualTo(2));
            Assert.That(ok.State[4].MechanicK, Is.EqualTo(BoardBuilder.DefaultPiranaKPours));
            Assert.That(ok.State.UnitsOf(10), Is.EqualTo(4));
            Assert.That(tooSlow.Error, Does.Contain("k va de 2 a 5"));
        }

        [Test]
        public void LevelValidation_AcceptsAPlayableBoard()
        {
            Assert.That(LevelValidation.Validate(BoardBuilder.Build(Caps4, Simple()).State!), Is.Null);
        }

        [Test]
        public void LevelValidation_RejectsUnitsOutsideTheCampaignRange()
        {
            // 02 R3: en campaña U_c ∈ {4, 5, 6}.
            BoardState state = BoardBuilder.Build(new[] { 4, 4, 4 }, Doses(new[] { 1, 2, 1 }, new[] { 2, 2, 1 }, new int[0])).State!;

            Assert.That(LevelValidation.Validate(state), Does.Contain("02 R3"));
        }

        [Test]
        public void LevelValidation_EveryColorNeedsItsOwnHome_AndEveryTallCuiaItsFruit()
        {
            // 02 §8 y 03 §3: para cada color, una cuia con cap = U_c; cada Cuia alta, con su fruta abundante.
            BoardState noHome = BoardBuilder.Build(
                new[] { 4, 4, 2, 2 },
                Doses(new[] { 1, 2, 1, 2 }, new[] { 2, 1, 3, 3 }, new[] { 3, 1 }, new[] { 3, 2 })).State!;
            BoardState tallWithoutFruit = BoardBuilder.Build(
                new[] { 5, 4, 4, 4 },
                Doses(new[] { 1, 2, 1, 2 }, new[] { 2, 1, 2, 1 }, new[] { 3, 3, 3, 3 }, new int[0])).State!;
            BoardState tallWithFruit = BoardBuilder.Build(
                new[] { 5, 4, 4 },
                Doses(new[] { 13, 13, 13, 1, 1 }, new[] { 1, 1, 13, 13 }, new int[0])).State!;

            Assert.That(LevelValidation.Validate(noHome), Does.Contain("02 §8"));
            Assert.That(LevelValidation.Validate(tallWithoutFruit), Does.Contain("fruta abundante"));
            Assert.That(LevelValidation.Validate(tallWithFruit), Is.Null);
        }

        [Test]
        public void LevelValidation_RejectsSealedCuiasAndStuckStarts()
        {
            // 03 §17 #5 (ninguna cuia sellada) y 02 §9.2 regla 1 (atascado(S0) = false).
            BoardState sealedStart = BoardBuilder.Build(Caps4, Doses(new[] { 1, 1, 1, 1 }, new[] { 2, 3, 2, 3 }, new[] { 3, 2, 3, 2 }, new int[0])).State!;
            BoardState stuck = BoardBuilder.Build(new[] { 4, 4 }, Doses(new[] { 1, 2, 1, 2 }, new[] { 2, 1, 2, 1 })).State!;

            Assert.That(LevelValidation.Validate(sealedStart), Does.Contain("sellada"));
            Assert.That(LevelValidation.Validate(stuck), Does.Contain("Aguas estancadas"));
        }
    }
}
