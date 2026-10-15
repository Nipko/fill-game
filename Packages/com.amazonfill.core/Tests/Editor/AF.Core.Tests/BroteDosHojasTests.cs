#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// Brote de dos hojas (K3a; variante de la 1.0 con debut en el 343; 02 R10 b; propuesta de D-007 b.3; firma de
    /// <c>pack_schema</c> 3 (D-023, punto 5): <c>brote</c> con <c>n</c> ∈ {1, 2}): germina al recibir 2 dosis de su color, en uno o
    /// varios vertidos. El solver está en <c>FilterSolverTests</c> y P1-P3, en <c>DotnetOnly/FutilePropertyTests</c>. Test
    /// compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class BroteDosHojasTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static Cuia Brote(int capacity, params int[] colorsBottomToTop)
        {
            return C(capacity, colorsBottomToTop).WithMechanic(MechanicType.Brote, flags: Cuia.FlagClosed);
        }

        private static Cuia Brote2(int capacity, params int[] colorsBottomToTop)
        {
            return C(capacity, colorsBottomToTop).WithMechanic(MechanicType.Brote, n: 2, flags: Cuia.FlagClosed);
        }

        private static MechanicSpec[] Attrs(int count, int index, MechanicSpec spec)
        {
            var attrs = new MechanicSpec[count];
            attrs[index] = spec;
            return attrs;
        }

        /// <summary>Brote de dos hojas [2, 1] (color 1), un origen con un tramo de 2 dosis de 1, [2, 1] y una vacía.</summary>
        private static BoardState Board()
        {
            return new BoardState(new[] { Brote2(4, 2, 1), C(4, 2, 2, 1, 1), C(4, 2, 1), Cuia.Empty(4) });
        }

        [Test]
        public void Format_BroteN_PackSchema3Signature_N1IsTheNormalBrote()
        {
            // Firma de pack_schema 3 (D-023, punto 5): brote con n ∈ {1, 2}. Sin n o con 1 es el Brote normal, con el mismo
            // Packed y el mismo hash que antes (los packs 1-200 no cambian); con 2, el de dos hojas, cerrado y con sus 2 hojas en n.
            int[] caps = { 4, 4, 4, 4 };
            int[][] cuias = { new[] { 2, 1 }, new[] { 1, 2, 1, 2 }, new[] { 2, 1 }, new int[0] };

            BoardBuildResult two = BoardBuilder.Build(caps, cuias, Attrs(4, 0, new MechanicSpec(MechanicType.Brote, n: 2)));
            BoardBuildResult one = BoardBuilder.Build(caps, cuias, Attrs(4, 0, new MechanicSpec(MechanicType.Brote, n: 1)));
            BoardBuildResult plain = BoardBuilder.Build(caps, cuias, Attrs(4, 0, new MechanicSpec(MechanicType.Brote)));

            Assert.That(two.Error, Is.Null);
            Assert.That(two.State![0].Packed, Is.EqualTo(Brote2(4, 2, 1).Packed));
            Assert.That(two.State[0].MechanicN, Is.EqualTo(2));
            Assert.That(two.State[0].MechanicFlags, Is.EqualTo(Cuia.FlagClosed));
            Assert.That(LevelValidation.Validate(two.State), Is.Null);
            Assert.That(one.State![0].Packed, Is.EqualTo(plain.State![0].Packed));
            Assert.That(plain.State[0].Packed, Is.EqualTo(Brote(4, 2, 1).Packed));
            Assert.That(one.State.CanonicalHash(), Is.EqualTo(plain.State.CanonicalHash()));
            Assert.That(two.State.CanonicalHash(), Is.Not.EqualTo(plain.State.CanonicalHash()));

            // El motivo es el de la firma (n ∈ {1, 2} y ningún otro campo), no cualquier error: casi todos llevan una «n».
            const string signature = "lleva como mucho n, 1 o 2 (Brote de dos hojas";
            Assert.That(BoardBuilder.Build(caps, cuias, Attrs(4, 0, new MechanicSpec(MechanicType.Brote, n: 3))).Error, Does.Contain(signature));
            Assert.That(BoardBuilder.Build(caps, cuias, Attrs(4, 0, new MechanicSpec(MechanicType.Brote, key: 1, n: 2))).Error, Does.Contain(signature));
        }

        [Test]
        public void R10b_InSingle_EachDoseOpensOneLeaf_AndTheSecondGerminates()
        {
            // 02 R10 b: «resta las dosis recibidas»; b.3: «cada dosis abre una». En single hacen falta dos vertidos.
            BoardState state = Board();

            BoardState first = PourRules.Apply(state, 1, 0, PourMode.Single, out PourEvents leaf);
            Assert.That(first[0].Packed, Is.EqualTo(C(4, 2, 1, 1).WithMechanic(MechanicType.Brote, n: 1, flags: Cuia.FlagClosed).Packed));
            Assert.That(leaf.BroteLeafOpenedMask, Is.EqualTo(1 << 0));
            Assert.That(leaf.BroteOpenedMask, Is.EqualTo(0));
            Assert.That(leaf.OpenedMask, Is.EqualTo(0));
            Assert.That(leaf.WasUnlocked(0), Is.False);
            Assert.That(PourRules.CanBeSource(first, 0), Is.False, "sigue cerrado: no es origen");
            var plainBrote = new BoardState(new[] { Brote(4, 2, 1, 1), C(4, 2, 2, 1), C(4, 2, 1), Cuia.Empty(4) });
            Assert.That(first.CanonicalHash(), Is.Not.EqualTo(plainBrote.CanonicalHash()), "las hojas que faltan entran en el hash (03 §3)");

            BoardState second = PourRules.Apply(first, 1, 0, PourMode.Single, out PourEvents germinated);
            Assert.That(second[0].Mechanic, Is.EqualTo(MechanicType.None));
            Assert.That(second[0].Packed, Is.EqualTo(C(4, 2, 1, 1, 1).Packed));
            Assert.That(germinated.BroteOpenedMask, Is.EqualTo(1 << 0));
            Assert.That(germinated.BroteLeafOpenedMask, Is.EqualTo(0));
            Assert.That(germinated.WasUnlocked(0), Is.True);
            Assert.That(PourRules.CanBeSource(second, 0), Is.True);
        }

        [Test]
        public void R9_R10b_InRun_ARunOfTwoOpensItAtOnce()
        {
            // b.3: «en run, un tramo de 2 lo abre de una vez» (D-005; 02 R9).
            BoardState after = PourRules.Apply(Board(), 1, 0, PourMode.Run, out PourEvents events);

            Assert.That(events.Amount, Is.EqualTo(2));
            Assert.That(events.BroteOpenedMask, Is.EqualTo(1 << 0));
            Assert.That(events.BroteLeafOpenedMask, Is.EqualTo(0));
            Assert.That(after[0].Packed, Is.EqualTo(C(4, 2, 1, 1, 1).Packed));
        }

        [Test]
        public void R10b_InRun_ARunOfOne_OpensOneLeaf_AndTheNextPourGerminates()
        {
            // En uno o varios vertidos (b.3): un tramo de 1 abre una hoja; el siguiente vertido de su color lo abre.
            BoardState state = Board();

            BoardState first = PourRules.Apply(state, 2, 0, PourMode.Run, out PourEvents leaf);
            Assert.That(leaf.Amount, Is.EqualTo(1));
            Assert.That(leaf.BroteLeafOpenedMask, Is.EqualTo(1 << 0));
            Assert.That(first[0].MechanicN, Is.EqualTo(1));
            Assert.That(first[0].Mechanic, Is.EqualTo(MechanicType.Brote));

            BoardState second = PourRules.Apply(first, 1, 0, PourMode.Run, out PourEvents germinated);
            Assert.That(germinated.Amount, Is.EqualTo(1), "solo cabe una dosis");
            Assert.That(germinated.BroteOpenedMask, Is.EqualTo(1 << 0));
            Assert.That(second[0].Packed, Is.EqualTo(C(4, 2, 1, 1, 1).Packed));
        }

        [Test]
        public void R10_GerminatesAndSeals_InTheSamePour_BBeforeC()
        {
            // R10: (b) abre el brote y (c) lo sella en el mismo vertido.
            var state = new BoardState(new[] { Brote2(4, 1, 1), C(4, 2, 2, 1, 1), C(4, 2, 2), Cuia.Empty(4) });

            BoardState after = PourRules.Apply(state, 1, 0, PourMode.Run, out PourEvents events);

            Assert.That(events.BroteOpenedMask, Is.EqualTo(1 << 0));
            Assert.That(events.WasSealed(0), Is.True);
            Assert.That(after.IsSealed(0), Is.True);
        }

        [Test]
        public void R_0212_Closed_IsNotASource_AsTargetTheNormalRule()
        {
            // 02 §1.2: Brote cerrado en bloqueada_origen; como destino, la regla normal. No se comprueba futil como destino: el
            // Brote nunca está vacío (empieza con 1-3 dosis y solo las gana), y futil solo puede dar true hacia una vacía, así
            // que nunca es destino fútil por construcción, como dice BroteMechanic (02 §1.4: además cambia al recibir).
            BoardState state = Board();

            Assert.That(PourRules.CanBeSource(state, 0), Is.False);
            Assert.That(PourRules.IsLegal(state, 0, 3), Is.False);
            Assert.That(PourRules.IsLegal(state, 1, 0), Is.True);
            Assert.That(PourRules.IsLegal(new BoardState(new[] { Brote2(4, 2, 1), C(4, 1, 2, 2), C(4, 1, 1, 2), Cuia.Empty(4) }), 1, 0), Is.False);
        }

        [Test]
        public void R8_Marea_PutsTheLeafBack_AndRestartBothLeaves()
        {
            BoardState initial = Board();
            var session = new LevelSession(initial, true, PourMode.Single);

            Assert.That(session.TryPour(1, 0, out PourResult result), Is.True);
            Assert.That(result.Events.BroteLeafOpenedMask, Is.EqualTo(1 << 0));
            Assert.That(session.Current[0].MechanicN, Is.EqualTo(1));
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Current.Equals(initial), Is.True);
            Assert.That(session.Current[0].MechanicN, Is.EqualTo(2));

            session.TryPour(1, 0, out _);
            session.TryPour(1, 0, out _);
            Assert.That(session.Current[0].Mechanic, Is.EqualTo(MechanicType.None));
            session.Restart();
            Assert.That(session.Current.Equals(initial), Is.True);
        }

        [Test]
        public void Validation_SpaceForTwoDoses_AndBothLeavesClosedAtTheStart()
        {
            // b.3: «el validador exige espacio ≥ 2: empieza con 2 dosis como mucho» (capacidad 4); 02 §9.1 M2: 1-3 dosis.
            int[] caps = { 4, 4, 4, 4 };
            int[][] cuias = { new[] { 1, 2, 1 }, new[] { 2, 2, 1, 2 }, new[] { 1 }, new int[0] };

            BoardState normal = BoardBuilder.Build(caps, cuias, Attrs(4, 0, new MechanicSpec(MechanicType.Brote))).State!;
            BoardState two = BoardBuilder.Build(caps, cuias, Attrs(4, 0, new MechanicSpec(MechanicType.Brote, n: 2))).State!;
            Assert.That(LevelValidation.Validate(normal), Is.Null);
            Assert.That(LevelValidation.Validate(two), Does.Contain("espacio"));

            var halfOpen = new BoardState(new[]
            {
                C(4, 2, 1).WithMechanic(MechanicType.Brote, n: 1, flags: Cuia.FlagClosed), C(4, 1, 2, 1, 2), C(4, 2, 1), Cuia.Empty(4),
            });
            Assert.That(LevelValidation.Validate(halfOpen), Does.Contain("hojas"));
        }
    }
}
