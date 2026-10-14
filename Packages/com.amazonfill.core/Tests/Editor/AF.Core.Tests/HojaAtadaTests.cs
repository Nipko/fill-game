#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// Hoja atada (K3a; variante de la 1.0 con debut en el 283; D-007, propuesta de D-007 b.3; 02 §1.2 y §1.4; firma de
    /// <c>pack_schema</c> 3, pregunta 8): filtro persistente de la fruta <c>key</c> que sí es origen y nunca se suelta. El solver
    /// (A* frente a la búsqueda exhaustiva y heurística) está en <c>FilterSolverTests</c> y P1-P3, en
    /// <c>DotnetOnly/FutilePropertyTests</c>. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class HojaAtadaTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static Cuia Atada(int capacity, int key, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop).WithMechanic(MechanicType.HojaAtada, key: key);
        }

        private static MechanicSpec[] Attrs(int count, int index, MechanicSpec spec)
        {
            var attrs = new MechanicSpec[count];
            attrs[index] = spec;
            return attrs;
        }

        [Test]
        public void R_0212_OnlyReceivesItsFruit_EmptyOrWithItOnTop()
        {
            // 02 §1.2: «la Hoja atada solo recibe su fruta (vacía o con ella en la cima)»; otro color es ilegal (R11).
            var state = new BoardState(new[] { Atada(4, 1), C(4, 2, 1, 1), C(4, 1, 2, 2), C(4, 2, 1), Cuia.Empty(4) });

            Assert.That(PourRules.IsLegal(state, 1, 0), Is.True);
            Assert.That(PourRules.IsLegal(state, 2, 0), Is.False);

            BoardState after = PourRules.Apply(state, 1, 0, PourMode.Run);
            Assert.That(after[0].Packed, Is.EqualTo(Atada(4, 1, 1, 1).Packed));
            Assert.That(PourRules.IsLegal(after, 3, 0), Is.True); // con su fruta en la cima
            Assert.That(PourRules.IsLegal(after, 2, 0), Is.False);
        }

        [Test]
        public void R_0212_WithAnotherFruitOnTop_ReceivesNothing()
        {
            // b.3: puede empezar con dosis cuyo fondo es su fruta; mientras otra fruta esté en la cima no recibe ni su fruta
            // (no tiene su fruta en la cima) ni la de la cima (no es su fruta).
            var state = new BoardState(new[] { Atada(4, 1, 1, 2), C(4, 2, 1, 1), C(4, 1, 2, 2), Cuia.Empty(4) });

            Assert.That(PourRules.IsLegal(state, 1, 0), Is.False);
            Assert.That(PourRules.IsLegal(state, 2, 0), Is.False);
        }

        [Test]
        public void R9_InRun_TakesTheRunOfItsFruitThatFits()
        {
            // D-005 (02 R9): entra el tramo que cabe; en single, una dosis.
            var state = new BoardState(new[] { Atada(2, 1), C(4, 1, 1, 1), C(4, 2, 2, 2, 2), C(2, 1) });

            Assert.That(PourRules.Amount(state, 1, 0, PourMode.Run), Is.EqualTo(2));
            Assert.That(PourRules.Amount(state, 1, 0, PourMode.Single), Is.EqualTo(1));
        }

        [Test]
        public void R_0212_IsASource_AndNeverDrops()
        {
            // 02 §1.2: «la Hoja atada sí es origen»; b.3: nunca se suelta, ni al verter ni al recibir (R10 b no la abre).
            var state = new BoardState(new[] { Atada(4, 1, 1, 2), C(4, 2, 2, 2, 1), Cuia.Empty(4), C(4, 1, 1) });

            Assert.That(PourRules.CanBeSource(state, 0), Is.True);
            Assert.That(PourRules.IsLegal(state, 0, 2), Is.True);
            BoardState poured = PourRules.Apply(state, 0, 2, PourMode.Run, out PourEvents fromIt);
            Assert.That(poured[0].Packed, Is.EqualTo(Atada(4, 1, 1).Packed));
            Assert.That(fromIt.WasUnlocked(0), Is.False);

            BoardState received = PourRules.Apply(poured, 3, 0, PourMode.Run, out PourEvents intoIt);
            Assert.That(received[0].Mechanic, Is.EqualTo(MechanicType.HojaAtada));
            Assert.That(received[0].Packed, Is.EqualTo(Atada(4, 1, 1, 1, 1).Packed));
            Assert.That(intoIt.OpenedMask, Is.EqualTo(0));
            Assert.That(intoIt.HojaOpenedMask, Is.EqualTo(0));
            Assert.That(intoIt.ReleasedMask, Is.EqualTo(0));
        }

        [Test]
        public void R5_R12_StaysTiedWhenSealed_AndCountsForVictory()
        {
            // b.3: «conserva la cuerda tras sellar»; R12: sellada vale para ganar como cualquier cuia.
            var state = new BoardState(new[] { Atada(4, 1), C(4, 2, 2, 1, 1), C(4, 1, 1, 2, 2) });

            BoardState s = PourRules.Apply(state, 1, 0, PourMode.Run);
            s = PourRules.Apply(s, 2, 1, PourMode.Run);
            BoardState won = PourRules.Apply(s, 2, 0, PourMode.Run, out PourEvents events);

            Assert.That(events.WasSealed(0), Is.True);
            Assert.That(won.IsSealed(0), Is.True);
            Assert.That(won[0].Mechanic, Is.EqualTo(MechanicType.HojaAtada));
            Assert.That(PourRules.CanBeSource(won, 0), Is.False);
            Assert.That(won.IsWon(), Is.True);
        }

        [Test]
        public void R_0214_AsTarget_ItIsNeverFutile_AsSourceTheNormalRuleApplies()
        {
            // 02 §1.4: «anexo B: también la Hoja atada» en cambia_al_recibir(B); no está en origen_exento(A).
            var state = new BoardState(new[] { C(4, 1, 1), Atada(4, 1), Cuia.Empty(4), C(4, 2, 2, 1, 1), C(4, 2, 2) });

            Assert.That(PourRules.IsFutile(state, 0, 1), Is.False);
            Assert.That(PourRules.IsFutile(state, 0, 2), Is.True);

            BoardState after = PourRules.Apply(state, 0, 1, PourMode.Run);
            Assert.That(PourRules.IsLegal(after, 1, 2), Is.True);
            Assert.That(PourRules.IsFutile(after, 1, 2), Is.True);
        }

        [Test]
        public void R_0214_TheOnlyUsefulPour_IntoItsHojaAtada_AvoidsStuck_R14()
        {
            // Anexo B (02 §1.4): con la regla anterior, A [1, 1] → su Hoja atada vacía era fútil y este tablero, que se gana, se
            // declaraba en Aguas estancadas (rompía R14).
            var withAtada = new BoardState(new[] { C(4, 1, 1), Atada(4, 1), C(4, 2, 2, 1, 1), C(4, 2, 2) });
            var withPlainEmpty = new BoardState(new[] { C(4, 1, 1), Cuia.Empty(4), C(4, 2, 2, 1, 1), C(4, 2, 2) });

            Assert.That(PourRules.IsStuck(withAtada, true), Is.False);
            Assert.That(PourRules.IsFutile(withPlainEmpty, 0, 1), Is.True);
            BoardState s = PourRules.Apply(withAtada, 0, 1, PourMode.Run);
            s = PourRules.Apply(s, 2, 1, PourMode.Run);
            s = PourRules.Apply(s, 3, 2, PourMode.Run);
            Assert.That(s.IsWon(), Is.True);
        }

        [Test]
        public void R8_Marea_AndRestart_RestoreTheBoard()
        {
            var initial = new BoardState(new[] { Atada(4, 1, 1, 2), C(4, 2, 2, 2, 1), Cuia.Empty(4), C(4, 1, 1) });
            var session = new LevelSession(initial, true);

            Assert.That(session.TryPour(0, 2, out _), Is.True);
            Assert.That(session.TryPour(3, 0, out _), Is.True);
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Current.Equals(initial), Is.True);
            session.TryPour(0, 2, out _);
            session.Restart();
            Assert.That(session.Current.Equals(initial), Is.True);
        }

        [Test]
        public void Format_HojaAtada_KeyAndBuilder_PackSchema3Signature()
        {
            // Firma de pack_schema 3 (pregunta 8): {"type":"hoja_atada","key":c}; código 10 [P], que no se persiste.
            Assert.That(MechanicTypes.TryParse("hoja_atada", out MechanicType type), Is.True);
            Assert.That(type, Is.EqualTo(MechanicType.HojaAtada));
            Assert.That(MechanicTypes.ToKey(MechanicType.HojaAtada), Is.EqualTo("hoja_atada"));
            Assert.That((int)MechanicType.HojaAtada, Is.EqualTo(10));

            int[] caps = { 4, 4, 4, 4 };
            int[][] cuias = { new[] { 1, 2, 1, 2 }, new[] { 2, 1, 2, 1 }, new int[0], new int[0] };
            BoardBuildResult ok = BoardBuilder.Build(caps, cuias, Attrs(4, 3, new MechanicSpec(MechanicType.HojaAtada, key: 2)));
            Assert.That(ok.Error, Is.Null);
            Assert.That(ok.State![3].Mechanic, Is.EqualTo(MechanicType.HojaAtada));
            Assert.That(ok.State[3].MechanicKey, Is.EqualTo(2));
            Assert.That(ok.State[3].MechanicFlags, Is.EqualTo(0), "nunca está cerrada");
            Assert.That(LevelValidation.Validate(ok.State), Is.Null);

            const string signature = "lleva solo key, un tinte de 1 a 14 (propuesta de D-007 b.3";
            Assert.That(BoardBuilder.Build(caps, cuias, Attrs(4, 3, new MechanicSpec(MechanicType.HojaAtada))).Error, Does.Contain(signature));
            Assert.That(BoardBuilder.Build(caps, cuias, Attrs(4, 3, new MechanicSpec(MechanicType.HojaAtada, key: 15))).Error, Does.Contain(signature));
            Assert.That(BoardBuilder.Build(caps, cuias, Attrs(4, 3, new MechanicSpec(MechanicType.HojaAtada, key: 2, n: 1))).Error, Does.Contain(signature));
        }

        [Test]
        public void Validation_KeyOnTheBoard_FruitAtTheBottom_HomeOfItsFruit_NoHidden_0317_6()
        {
            // 03 §17 #6 (llave en el tablero); b.3 (empieza vacía o con su fruta en el fondo); [P] de K3a (capacidad = U_c de su
            // llave: 02 R16, P3); 02 §11 (una cuia con mecánica lleva sus dosis visibles).
            int[] caps = { 4, 4, 4, 4 };
            int[][] cuias = { new[] { 1, 2, 1, 2 }, new[] { 2, 1, 2 }, new[] { 1 }, new int[0] };
            Assert.That(Validate(caps, cuias, 2, new MechanicSpec(MechanicType.HojaAtada, key: 1)), Is.Null);
            Assert.That(Validate(caps, cuias, 3, new MechanicSpec(MechanicType.HojaAtada, key: 2)), Is.Null);
            Assert.That(Validate(caps, cuias, 3, new MechanicSpec(MechanicType.HojaAtada, key: 7)), Does.Contain("llave"));
            Assert.That(Validate(caps, cuias, 1, new MechanicSpec(MechanicType.HojaAtada, key: 1)), Does.Contain("fondo"));
            Assert.That(Validate(caps, cuias, 1, new MechanicSpec(MechanicType.HojaAtada, key: 2)), Is.Null);

            int[] mixed = { 4, 4, 4, 3 };
            Assert.That(Validate(mixed, cuias, 3, new MechanicSpec(MechanicType.HojaAtada, key: 2)), Does.Contain("casa"));

            BoardState hidden = BoardBuilder.Build(caps, cuias, Attrs(4, 1, new MechanicSpec(MechanicType.HojaAtada, key: 2)), new[] { new[] { 1, 0 } }).State!;
            Assert.That(LevelValidation.Validate(hidden), Does.Contain("visibles"));
        }

        private static string? Validate(int[] caps, int[][] cuias, int index, MechanicSpec spec)
        {
            BoardBuildResult built = BoardBuilder.Build(caps, cuias, Attrs(caps.Length, index, spec));
            Assert.That(built.Error, Is.Null);
            return LevelValidation.Validate(built.State!);
        }
    }
}
