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
    /// M5 Barro (GDD 02 R2, §1.2 <c>barro_expuesto</c> y <c>espacio_efectivo + 1</c>, §9.1 M5; 03 §9; propuesta de D-007 b.2
    /// M5; D-007, Q14 A: es un filtro): dosis inerte de color 15. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class BarroTests
    {
        private const int Mud = Cuia.MudColor;

        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static BoardState Board(params Cuia[] cuias)
        {
            return new BoardState(cuias);
        }

        [Test]
        public void R2_MudIsColor15_AndIsAccepted_TheReservedColorIsNot_021_1()
        {
            // 02 R2: 1-14 tintes, 15 Barro (inerte), 16 reservado.
            BoardState state = Board(C(4, 1, Mud, 2), C(4, 2, 2, 2, 1), C(4, 1, 1));

            Assert.That(state[0].ColorAt(1), Is.EqualTo(Mud));
            Assert.That(state.UnitsOf(Mud), Is.EqualTo(1));
            Assert.That(() => { Board(C(4, Cuia.ReservedColor)); }, Throws.TypeOf<NotSupportedException>());
            Assert.That(BoardBuilder.Build(new[] { 4, 4 }, new[] { new[] { 1, 16 }, new int[0] }).Error, Does.Contain("16"));
        }

        [Test]
        public void M5_MudCutsTheRun_029_1()
        {
            // 02 §9.1 M5: el barro no es un color y corta las rachas (R6).
            BoardState state = Board(C(4, 1, Mud, 1, 1), Cuia.Empty(4), C(4, 1, 1));

            Assert.That(state[0].TopRunLength, Is.EqualTo(2));
            Assert.That(PourRules.Amount(state, 0, 1, PourMode.Run), Is.EqualTo(2));
        }

        [Test]
        public void M5_WithMudOnTop_TheCuiaIsNotASource_029_1()
        {
            // 02 §9.1 M5: «con barro en la cima la cuia no es origen» (un toque como origen es ilegal, R11).
            BoardState state = Board(C(4, 1, Mud), Cuia.Empty(4), C(4, 1, 1, 1));

            Assert.That(state[0].MudExposed, Is.True);
            Assert.That(PourRules.CanBeSource(state, 0), Is.False);
            Assert.That(PourRules.IsLegal(state, 0, 1), Is.False);
            Assert.That(CuiaRules.CanBeSource(state[0], state.UnitsTable), Is.False);
        }

        [TestCase(2)]
        [TestCase(5)]
        public void M5_MudExposed_AcceptsAnyColor_021_2(int color)
        {
            // legal de 02 §1.2: «si barro_expuesto(B): return true» (no aplica la regla de mismo color).
            BoardState state = Board(C(4, 1, Mud), C(4, 3, color), C(4, 1, 1, 1), C(4, 3, 3, 3));

            Assert.That(PourRules.IsLegal(state, 1, 0), Is.True);
            Assert.That(CuiaRules.IsLegal(state[1], state[0], state.UnitsTable), Is.True);
        }

        [Test]
        public void M5_MudExposedInAFullCuia_StillAcceptsOneDose_021_2()
        {
            // 02 §1.2: barro_expuesto se evalúa antes que llena(B); espacio_efectivo = 0 + 1 (el barro se sustituye).
            BoardState state = Board(C(4, 1, 2, 3, Mud), C(4, 4, 4, 4));

            Assert.That(PourRules.IsLegal(state, 1, 0), Is.True);
            Assert.That(PourRules.Amount(state, 1, 0, PourMode.Run), Is.EqualTo(1));
            Assert.That(CuiaRules.EffectiveSpace(state[0]), Is.EqualTo(1));
        }

        [TestCase(PourMode.Run, 3, new[] { 1, 2, 2, 2 }, new[] { 3 })]
        [TestCase(PourMode.Single, 1, new[] { 1, 2 }, new[] { 3, 2, 2 })]
        public void M5_FirstPourWashesTheMud_AndTakesItsPlace_EffectiveSpacePlusOne_021_2(PourMode mode, int amount, int[] washed, int[] rest)
        {
            // 02 §9.1 M5 y cantidad de 02 §1.2: min(racha, espacio_efectivo(B)) con espacio_efectivo = espacio + 1. En run el
            // primer vertido lava el barro y ocupa su lugar con toda la racha que cabe; en single, una dosis.
            BoardState state = Board(C(4, 1, Mud), C(4, 3, 2, 2, 2), C(4, 1, 1, 1, 2), C(4, 3, 3, 3));
            Assert.That(PourRules.Amount(state, 1, 0, mode), Is.EqualTo(amount));

            BoardState after = PourRules.Apply(state, 1, 0, mode, out PourEvents events);

            Assert.That(after[0].Packed, Is.EqualTo(Cuia.Create(4, washed).Packed));
            Assert.That(after[1].Packed, Is.EqualTo(Cuia.Create(4, rest).Packed));
            Assert.That(after.UnitsOf(2), Is.EqualTo(4));
            Assert.That(events.Amount, Is.EqualTo(amount));
            Assert.That(events.BarroWashedMask, Is.EqualTo(1 << 0));
        }

        [Test]
        public void M5_AWashedCuiaIsNormal_TheNextPourFollowsTheNormalRule()
        {
            // Tras el lavado ya no hay barro expuesto: la cuia vuelve a la regla de mismo color (02 §1.2).
            BoardState state = Board(C(4, 1, Mud), C(4, 3, 2), C(4, 1, 1, 1), C(4, 3, 3, 3));

            BoardState after = PourRules.Apply(state, 1, 0, PourMode.Run, out PourEvents events);

            Assert.That(after[0].MudExposed, Is.False);
            Assert.That(events.BarroWashedMask, Is.EqualTo(1 << 0));
            Assert.That(PourRules.IsLegal(after, 3, 0), Is.False);
            Assert.That(PourRules.IsLegal(after, 1, 0), Is.False);
        }

        [Test]
        public void M5_ExposingTheMud_DoesNotWashIt()
        {
            // El barro solo se lava al recibir: verter la racha de encima lo deja expuesto, sin evento.
            BoardState state = Board(C(4, 1, Mud, 2, 2), Cuia.Empty(4), C(4, 1, 1, 1), C(4, 2, 2));

            BoardState after = PourRules.Apply(state, 0, 3, PourMode.Run, out PourEvents events);

            Assert.That(after[0].MudExposed, Is.True);
            Assert.That(after[0].Count, Is.EqualTo(2));
            Assert.That(events.BarroWashedMask, Is.EqualTo(0));
        }

        [Test]
        public void M5_TheWash_DoesNotRevealAHiddenDoseUnderTheMud_R7()
        {
            // R7: una dosis oculta se revela cuando queda arriba; con el lavado nunca llega a la cima (el barro se sustituye).
            BoardState state = Board(Cuia.Create(4, new[] { 3, Mud }, 0x1), C(4, 2, 2), C(4, 3, 3, 3), C(4, 2, 2));

            BoardState after = PourRules.Apply(state, 1, 0, PourMode.Run, out PourEvents events);

            Assert.That(after[0].IsHiddenAt(0), Is.True);
            Assert.That(after[0].ColorAt(1), Is.EqualTo(2));
            Assert.That(events.SourceRevealed, Is.False);
            Assert.That(events.BarroWashedMask, Is.EqualTo(1 << 0));
        }

        [Test]
        public void R5_R12_ACuiaWithMud_IsNeitherSealedNorEmpty()
        {
            // 03 §9: «victoria sin cambio: una cuia con barro no está sellada ni vacía, hay que lavarlo».
            BoardState withMud = Board(C(4, 1, 1, 1, 1), C(4, 2, 2, 2, Mud), C(4, 2));
            Assert.That(withMud.IsSealed(1), Is.False);
            Assert.That(withMud.IsWon(), Is.False);

            // Ni siquiera una cuia llena solo de barro con U_15 = cap (el barro no es un tinte, R5).
            BoardState allMud = Board(C(4, Mud, Mud, Mud, Mud), C(4, 1, 1, 1, 1));
            Assert.That(allMud.UnitsOf(Mud), Is.EqualTo(4));
            Assert.That(allMud.IsSealed(0), Is.False);
            Assert.That(allMud.IsWon(), Is.False);
        }

        [Test]
        public void M5_PourIntoExposedMud_IsNeverFutile_021_4()
        {
            // futil (anexo B): B con barro no está vacía, así que verter en ella nunca es fútil; A con barro dentro no es
            // monocolor.
            BoardState state = Board(C(4, 1, Mud), C(4, 2, 2), C(4, 1, 1, 1), C(4, 2, 2), Cuia.Empty(4));

            Assert.That(PourRules.IsFutile(state, 1, 0), Is.False);
            Assert.That(PourRules.IsStuck(state, true), Is.False);
            BoardState inside = Board(C(4, 2, Mud, 2, 2), Cuia.Empty(4), C(4, 2));
            Assert.That(PourRules.IsFutile(inside, 0, 1), Is.False);
        }

        [Test]
        public void M5_StuckDetection_CountsTheWashAsUseful_R13()
        {
            // Sin el barro expuesto no quedaría ningún vertido legal: lavarlo es útil (R13, R14).
            BoardState state = Board(C(4, 1, Mud), C(4, 2, 3, 3, 1), C(4, 3, 2, 2, 2), C(4, 1, 1, 3));

            var buffer = new Pour[PourRules.MaxPours];
            int count = PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);

            Assert.That(count, Is.GreaterThan(0));
            for (int i = 0; i < count; i++)
            {
                Assert.That(buffer[i].Target, Is.EqualTo(0));
            }

            Assert.That(PourRules.IsStuck(state, true), Is.False);
        }

        [Test]
        public void M5_LevelSession_ReportsBarroWashed_AndMareaRestoresTheMud()
        {
            // 02 §9.2: Marea restaura el barro (estado anterior exacto, 04 §2.1).
            BoardState state = Board(C(4, 1, Mud), C(4, 2, 2, 2, 1), C(4, 1, 1), C(4, 2));
            var session = new LevelSession(state, true, PourMode.Run);

            Assert.That(session.TryPour(1, 0, out PourResult result), Is.True);
            Assert.That(result.BarroWashed, Is.True);
            Assert.That(session.Current[0].Packed, Is.EqualTo(C(4, 1, 1).Packed));

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Current.Equals(state), Is.True);
            Assert.That(session.Current[0].MudExposed, Is.True);
        }

        [Test]
        public void M5_Validation_MudOnlyInLayer2Or3OfACuiaOf4_029_1()
        {
            // 02 §9.1 M5: «en posición 2 o 3 de una cuia de 4» (índice 1-2); el barro no tiene U_c (no es un color).
            Assert.That(LevelValidation.Validate(Level(new[] { 4, 4, 4, 4, 4 }, new[] { 1, Mud, 2 }, null, null)), Is.Null);
            Assert.That(LevelValidation.Validate(Level(new[] { 4, 4, 4, 4, 4 }, new[] { 1, 2, Mud }, null, null)), Is.Null);
            Assert.That(LevelValidation.Validate(Level(new[] { 4, 4, 4, 4, 4 }, new[] { Mud, 1, 2 }, null, null)), Does.Contain("capa 2 o 3"));
            Assert.That(LevelValidation.Validate(Level(new[] { 4, 4, 4, 4, 4 }, new[] { 1, 2, 2, Mud }, null, null)), Does.Contain("capa 2 o 3"));
            Assert.That(LevelValidation.Validate(Level(new[] { 3, 4, 4, 4, 4 }, new[] { 1, Mud, 2 }, null, null)), Does.Contain("cuia de 4"));
        }

        [Test]
        public void M5_Validation_NeverInACuiaWithMechanic_NorHidden()
        {
            // Propuesta de D-007 b.2 M5: «nunca dentro de un Matapi»; C2-3: nunca en una cuia con mecánica ni oculto [P].
            var brote = new MechanicSpec(MechanicType.Brote);
            Assert.That(LevelValidation.Validate(Level(new[] { 4, 4, 4, 4, 4 }, new[] { 1, Mud, 2 }, brote, null)), Does.Contain("cuia con mecánica"));
            Assert.That(LevelValidation.Validate(Level(new[] { 4, 4, 4, 4, 4 }, new[] { 1, Mud, 2 }, null, new[] { new[] { 0, 1 } })), Does.Contain("oculto"));
        }

        [Test]
        public void BoardBuilder_AcceptsMud_AndUnitsPerColorIgnoresIt()
        {
            // 08 §4.1: «el Barro no es un type de attrs: es el tinte 15 dentro de cuias»; units_per_color = 4 no lo cuenta.
            BoardBuildResult built = BoardBuilder.Build(
                new[] { 4, 4, 4, 4, 4 },
                new[] { new[] { 1, Mud, 2 }, new[] { 2, 2, 1 }, new[] { 1, 1, 2 }, new int[0], new int[0] },
                null,
                null,
                UnitsPerColor.Uniform(4));

            Assert.That(built.Error, Is.Null);
            Assert.That(built.State!.UnitsOf(Mud), Is.EqualTo(1));
        }

        /// <summary>
        /// Nivel de 5 cuias de capacidad <paramref name="caps"/> con la cuia 0 = <paramref name="first"/> y frutas 1 y 2
        /// completas (U = 4) repartidas en las cuias 1-2; las cuias 3-4 vacías.
        /// </summary>
        private static BoardState Level(int[] caps, int[] first, MechanicSpec? attr, int[][]? hidden)
        {
            int ones = 0;
            int twos = 0;
            for (int k = 0; k < first.Length; k++)
            {
                ones += first[k] == 1 ? 1 : 0;
                twos += first[k] == 2 ? 1 : 0;
            }

            var cuias = new int[5][];
            cuias[0] = first;
            cuias[1] = Fill(2, 4 - twos - 1, 1, 1);
            cuias[2] = Fill(1, 4 - ones - 1, 2, 1);
            cuias[3] = new int[0];
            cuias[4] = new int[0];
            MechanicSpec[]? attrs = null;
            if (attr != null)
            {
                attrs = new MechanicSpec[5];
                attrs[0] = attr.Value;
            }

            BoardBuildResult built = BoardBuilder.Build(caps, cuias, attrs, hidden);
            Assert.That(built.Error, Is.Null);
            return built.State!;
        }

        private static int[] Fill(int color, int count, int other, int otherCount)
        {
            var doses = new int[count + otherCount];
            for (int k = 0; k < doses.Length; k++)
            {
                doses[k] = k < count ? color : other;
            }

            return doses;
        }
    }
}
