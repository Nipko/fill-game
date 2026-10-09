#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// M2 Brote (GDD 02 §9.1 y §9.2; 03 §9; D-007: misma regla, debut en el 163): tapón del color de la cima que no deja
    /// ser origen y germina al recibir su color. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class BroteTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static Cuia Brote(int capacity, params int[] colorsBottomToTop)
        {
            return C(capacity, colorsBottomToTop).WithMechanic(MechanicType.Brote, flags: Cuia.FlagClosed);
        }

        [Test]
        public void M2_ClosedBrote_IsNotASource_029_1()
        {
            var state = new BoardState(new[] { Brote(4, 2, 1), C(4, 1, 2, 2), Cuia.Empty(4), C(4, 1, 1, 2) });

            Assert.That(PourRules.CanBeSource(state, 0), Is.False);
            Assert.That(PourRules.IsLegal(state, 0, 2), Is.False);
        }

        [Test]
        public void M2_AsTarget_FollowsTheNormalRule_029_1()
        {
            // Como destino sigue la regla normal (mismo color y hueco): otro color es ilegal (R11).
            var state = new BoardState(new[] { Brote(4, 2, 1), C(4, 2, 1, 1), C(4, 1, 2, 2), Cuia.Empty(4) });

            Assert.That(PourRules.IsLegal(state, 1, 0), Is.True);
            Assert.That(PourRules.Amount(state, 1, 0, PourMode.Run), Is.EqualTo(2));
            Assert.That(PourRules.IsLegal(state, 2, 0), Is.False);
        }

        [Test]
        public void M2_ReceivingItsColor_Germinates_InStageB_029_1()
        {
            // R10 (b) del anexo B; 02 R16 para D-007: el Brote abierto es una cuia normal con el mismo contenido.
            var state = new BoardState(new[] { Brote(4, 2, 1), C(4, 2, 1, 1), C(4, 1, 2, 2), Cuia.Empty(4) });

            BoardState after = PourRules.Apply(state, 1, 0, PourMode.Single, out PourEvents events);

            Assert.That(after[0].Mechanic, Is.EqualTo(MechanicType.None));
            Assert.That(after[0].Packed, Is.EqualTo(C(4, 2, 1, 1).Packed));
            Assert.That(events.BroteOpenedMask, Is.EqualTo(1));
            Assert.That(PourRules.CanBeSource(after, 0), Is.True);
        }

        [Test]
        public void M2_GerminatesAndSeals_InTheSamePour_R10bBeforeC()
        {
            var state = new BoardState(new[] { Brote(4, 1, 1), C(4, 2, 1, 1), C(4, 2, 2, 2), Cuia.Empty(4) });

            BoardState after = PourRules.Apply(state, 1, 0, PourMode.Run, out PourEvents events);

            Assert.That(events.BroteOpenedMask, Is.EqualTo(1));
            Assert.That(events.WasSealed(0), Is.True);
            Assert.That(after.IsSealed(0), Is.True);
        }

        [Test]
        public void M2_Marea_ClosesTheBroteAgain_029_2()
        {
            var initial = new BoardState(new[] { Brote(4, 2, 1), C(4, 2, 1, 1), C(4, 1, 2, 2), Cuia.Empty(4) });
            var session = new LevelSession(initial, true);

            Assert.That(session.TryPour(1, 0, out PourResult result), Is.True);
            Assert.That(result.Events.BroteOpenedMask, Is.EqualTo(1));
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Current[0].Mechanic, Is.EqualTo(MechanicType.Brote));
            Assert.That(PourRules.CanBeSource(session.Current, 0), Is.False);
        }

        [Test]
        public void M2_Validation_OneToThreeVisibleDoses_NotFull()
        {
            // 02 §9.1 M2: cuia con 1-3 dosis visibles; sin llenar para que pueda germinar.
            int[] caps = { 4, 4, 4, 4 };
            int[][] cuias = { new[] { 2, 1 }, new[] { 2, 1 }, new[] { 1, 2, 1, 2 }, new int[0] };
            var ok = new MechanicSpec[4];
            ok[0] = new MechanicSpec(MechanicType.Brote);
            var full = new MechanicSpec[4];
            full[2] = new MechanicSpec(MechanicType.Brote);
            var empty = new MechanicSpec[4];
            empty[3] = new MechanicSpec(MechanicType.Brote);

            BoardState valid = BoardBuilder.Build(caps, cuias, ok).State!;
            Assert.That(valid[0].MechanicFlags, Is.EqualTo(Cuia.FlagClosed));
            Assert.That(LevelValidation.Validate(valid), Is.Null);
            Assert.That(LevelValidation.Validate(BoardBuilder.Build(caps, cuias, full).State!), Does.Contain("Brote"));
            Assert.That(LevelValidation.Validate(BoardBuilder.Build(caps, cuias, empty).State!), Does.Contain("1-3"));
            Assert.That(LevelValidation.Validate(BoardBuilder.Build(caps, cuias, ok, new[] { new[] { 0, 0 } }).State!), Does.Contain("visibles"));
        }
    }
}
