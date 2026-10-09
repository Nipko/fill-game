#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// M1 Hoja (GDD 02 §9.1 y §9.2; 03 §9; propuesta de D-007 b.2 M1 y anexo B): vacío reservado a la fruta <c>key</c>.
    /// Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class HojaTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static Cuia Hoja(int capacity, int key)
        {
            return Cuia.Empty(capacity).WithMechanic(MechanicType.Hoja, key: key, flags: Cuia.FlagClosed);
        }

        [Test]
        public void M1_OnlyAcceptsItsKey_029_1()
        {
            // 02 §9.1 M1 y legal de 02 §1.2: color_top(A) == color_hoja(B) y espacio(B) > 0; otro color es ilegal (R11).
            var state = new BoardState(new[] { Hoja(4, 1), C(4, 2, 1, 1), C(4, 1, 2, 2), C(4, 2, 1) });

            Assert.That(PourRules.IsLegal(state, 1, 0), Is.True);
            Assert.That(PourRules.IsLegal(state, 2, 0), Is.False);
            Assert.That(PourRules.Amount(state, 2, 0, PourMode.Run), Is.EqualTo(0));
        }

        [Test]
        public void M1_InRun_TakesTheRunOfTheKeyThatFits_029_1()
        {
            // 02 §9.1 M1 (D-005): en run entra el tramo de key que cabe; el resto queda en A.
            var state = new BoardState(new[] { Hoja(2, 1), C(4, 1, 1, 1), C(4, 2, 2, 2, 2), C(2, 1) });

            Assert.That(PourRules.Amount(state, 1, 0, PourMode.Run), Is.EqualTo(2));
            Assert.That(PourRules.Amount(state, 1, 0, PourMode.Single), Is.EqualTo(1));
        }

        [Test]
        public void M1_IsNotASourceWhileItHasTheLeaf_029_1()
        {
            var state = new BoardState(new[] { Hoja(4, 1), C(4, 1, 1, 1), Cuia.Empty(4), C(4, 1, 2, 2, 2), C(2, 2) });

            Assert.That(PourRules.CanBeSource(state, 0), Is.False);
            for (int b = 1; b < state.Count; b++)
            {
                Assert.That(PourRules.IsLegal(state, 0, b), Is.False);
            }
        }

        [Test]
        public void M1_FirstPourOfTheKey_DropsTheLeaf_InStageB_AndTheCuiaBecomesNormal_029_1()
        {
            // R10 (b) del anexo B; 02 R16 para D-007: la Hoja abierta tiene el mismo Packed que una cuia normal.
            var state = new BoardState(new[] { Hoja(4, 1), C(4, 2, 1, 1), C(4, 2, 2, 2, 1), C(4, 1) });

            BoardState after = PourRules.Apply(state, 1, 0, PourMode.Run, out PourEvents events);

            Assert.That(after[0].Mechanic, Is.EqualTo(MechanicType.None));
            Assert.That(after[0].Packed, Is.EqualTo(C(4, 1, 1).Packed));
            Assert.That(events.HojaOpenedMask, Is.EqualTo(1 << 0));
            Assert.That(events.WasUnlocked(0), Is.True);
            Assert.That(PourRules.CanBeSource(after, 0), Is.True);
        }

        [Test]
        public void M1_OpeningAndSealing_HappenInTheSamePour_R10bBeforeC()
        {
            // Anexo B: (b) abre la Hoja y (c) la sella en el mismo vertido si recibe todas las dosis de su fruta.
            var state = new BoardState(new[] { Hoja(4, 1), C(5, 2, 1, 1, 1, 1), C(4, 2, 2, 2, 2), Cuia.Empty(4) });

            BoardState after = PourRules.Apply(state, 1, 0, PourMode.Run, out PourEvents events);

            Assert.That(events.HojaOpenedMask, Is.EqualTo(1 << 0));
            Assert.That(events.WasSealed(0), Is.True);
            Assert.That(after.IsSealed(0), Is.True);
        }

        [Test]
        public void AnexoB_PouringTheRestOfAFruitIntoItsEmptyHoja_IsNotFutile()
        {
            // Anexo B, «Por qué hace falta»: con la regla anterior A [1,1] → su Hoja vacía era fútil y este tablero, que tiene
            // solución, se declaraba en Aguas estancadas (rompía R14). Ahora la Hoja cambia al recibir y nunca es fútil.
            var withHoja = new BoardState(new[] { C(4, 1, 1), Hoja(4, 1), C(4, 1, 1, 2, 2), C(2, 2, 2) });
            var withPlainEmpty = new BoardState(new[] { C(4, 1, 1), Cuia.Empty(4), C(4, 1, 1, 2, 2), C(2, 2, 2) });

            Assert.That(PourRules.IsFutile(withHoja, 0, 1), Is.False);
            Assert.That(PourRules.IsStuck(withHoja, true), Is.False);
            Assert.That(PourRules.IsFutile(withPlainEmpty, 0, 1), Is.True);
            BoardState s = PourRules.Apply(withHoja, 0, 1, PourMode.Run);
            s = PourRules.Apply(s, 2, 0, PourMode.Run);
            s = PourRules.Apply(s, 3, 0, PourMode.Run);
            s = PourRules.Apply(s, 2, 1, PourMode.Run);
            Assert.That(s.IsWon(), Is.True);
        }

        [Test]
        public void M1_Marea_AndRestart_PutTheLeafBack_029_2()
        {
            // 02 §9.2: Marea restaura la hoja si el vertido deshecho la abrió; Volver a la orilla vuelve al tablero inicial.
            var initial = new BoardState(new[] { Hoja(4, 1), C(4, 2, 1, 1), C(4, 2, 2, 2, 1), C(4, 1) });
            var session = new LevelSession(initial, true);

            Assert.That(session.TryPour(1, 0, out PourResult result), Is.True);
            Assert.That(result.Events.HojaOpenedMask, Is.EqualTo(1));
            Assert.That(session.Current[0].Mechanic, Is.EqualTo(MechanicType.None));
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Current[0].Mechanic, Is.EqualTo(MechanicType.Hoja));
            Assert.That(session.Current.Equals(initial), Is.True);
            session.TryPour(1, 0, out _);
            session.Restart();
            Assert.That(session.Current[0], Is.EqualTo(Hoja(4, 1)));
        }

        [Test]
        public void M1_Validation_StartsEmpty_AndItsKeyIsOnTheBoard_0317_6()
        {
            // 03 §17 #6: Hoja con color llave presente; 02 §9.1 M1: cuia vacía.
            int[] caps = { 4, 4, 4, 4 };
            int[][] cuias = { new[] { 1, 2, 1, 2 }, new[] { 2, 1, 2, 1 }, new int[0], new int[0] };
            var ok = new MechanicSpec[4];
            ok[3] = new MechanicSpec(MechanicType.Hoja, key: 2);
            var absentKey = new MechanicSpec[4];
            absentKey[3] = new MechanicSpec(MechanicType.Hoja, key: 7);
            var notEmpty = new MechanicSpec[4];
            notEmpty[0] = new MechanicSpec(MechanicType.Hoja, key: 2);

            BoardBuildResult built = BoardBuilder.Build(caps, cuias, ok);
            Assert.That(built.IsValid, Is.True, built.Error);
            Assert.That(built.State![3].MechanicFlags, Is.EqualTo(Cuia.FlagClosed));
            Assert.That(LevelValidation.Validate(built.State), Is.Null);
            Assert.That(LevelValidation.Validate(BoardBuilder.Build(caps, cuias, absentKey).State!), Does.Contain("llave"));
            Assert.That(LevelValidation.Validate(BoardBuilder.Build(caps, cuias, notEmpty).State!), Does.Contain("vacía"));
        }
    }
}
