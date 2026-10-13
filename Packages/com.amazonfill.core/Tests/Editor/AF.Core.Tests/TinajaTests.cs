#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// M13 Tinaja y su cuia chica (D-019; propuesta <c>docs/research/cultura-ribera/propuesta-tinaja.md</c>, b.2 reglas 1-9,
    /// <c>legal</c>, <c>futil</c>, R10 por etapas, R14 y «Marea y Volver a la orilla»; c.1): una regla por test, con su §.
    /// Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class TinajaTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        /// <summary>Tinaja de capacidad 5 con las dosis indicadas (del fondo a la boca) y su máscara de ocultos.</summary>
        private static Cuia T(int hiddenMask, params int[] colorsBottomToTop)
        {
            return Cuia.Create(LevelValidation.TinajaCapacity, colorsBottomToTop, hiddenMask).WithMechanic(MechanicType.Tinaja);
        }

        /// <summary>Su cuia chica colgada (capacidad 2) con las dosis indicadas.</summary>
        private static Cuia Ch(params int[] colorsBottomToTop)
        {
            return Cuia.Create(LevelValidation.CucharonCapacity, colorsBottomToTop).WithMechanic(MechanicType.Cucharon);
        }

        private static BoardState Board(params Cuia[] cuias)
        {
            return new BoardState(cuias);
        }

        /// <summary>
        /// S0 de b.2 («Morpho», «Por qué hace falta el orden»): N[c,c,a] N[a,b,a] N[b,b,b] T[c(o),a(o),c] Ch[] con a = 1, b = 2
        /// y c = 3; la tinaja es la cuia 3 y su cuia la 4.
        /// </summary>
        internal static BoardState DebutS0()
        {
            return Board(C(4, 3, 3, 1), C(4, 1, 2, 1), C(4, 2, 2, 2), T(0x3, 3, 1, 3), Ch());
        }

        [Test]
        public void Tinaja_NeverReceives_M13r2()
        {
            // b.2 regla 2: «La tinaja nunca recibe (bloqueada_destino permanente). Tampoco desde su cuia» (R11, sin texto).
            // La cuia 0 tiene en la cima la fruta de la boca y la tinaja tiene hueco: con la regla normal sería legal.
            BoardState state = Board(C(4, 1, 3), T(0x1, 2, 3), Ch(2), C(4, 1, 1, 1), Cuia.Empty(4));

            Assert.That(PourRules.IsLegal(state, 0, 1), Is.False);
            Assert.That(PourRules.IsLegal(state, 2, 1), Is.False); // tampoco desde su cuia
            Assert.That(PourRules.IsLegal(state, 3, 1), Is.False);
            var session = new LevelSession(state, true, PourMode.Run);
            Assert.That(session.TryPour(0, 1, out _), Is.False);
            Assert.That(session.Current.Equals(state), Is.True);

            // Vacía queda inerte: ni origen (está vacía) ni destino (regla 8).
            BoardState inert = Board(C(4, 1, 3), Cuia.Empty(LevelValidation.TinajaCapacity).WithMechanic(MechanicType.Tinaja), C(2, 2), Cuia.Empty(4));
            Assert.That(PourRules.CanBeSource(inert, 1), Is.False);
            Assert.That(PourRules.IsLegal(inert, 0, 1), Is.False);
            Assert.That(PourRules.IsLegal(inert, 2, 1), Is.False);
        }

        [Test]
        public void Tinaja_PoursOnlyIntoItsCuia_M13r3()
        {
            // b.2 regla 3: «La tinaja solo vierte en su cuia» (solo_vierte_en(A)); tinaja → otra cuia es ilegal (R11), también
            // a una vacía o a una cuia con su misma fruta en la cima y hueco.
            BoardState state = Board(T(0x1, 2, 3), Ch(), C(4, 1, 3), Cuia.Empty(4), C(4, 1, 1, 1));

            Assert.That(PourRules.CanBeSource(state, 0), Is.True);
            Assert.That(PourRules.IsLegal(state, 0, 1), Is.True);
            Assert.That(PourRules.IsLegal(state, 0, 2), Is.False);
            Assert.That(PourRules.IsLegal(state, 0, 3), Is.False);
            Assert.That(PourRules.IsLegal(state, 0, 4), Is.False);
            var session = new LevelSession(state, true, PourMode.Run);
            Assert.That(session.TryPour(0, 3, out _), Is.False);
            Assert.That(session.Current.Equals(state), Is.True);

            // Con su cuia llena no hay adónde sacar.
            BoardState full = Board(T(0x1, 2, 3), Ch(1, 2), C(4, 1, 3), Cuia.Empty(4), C(4, 1, 1));
            Assert.That(PourRules.IsLegal(full, 0, 1), Is.False);
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void Tinaja_AmountIsAlwaysOne_T4(PourMode mode)
        {
            // b.2 regla 4: cantidad = min(racha visible, espacio) = 1 siempre: por T4 la racha de la boca es 1, también después de
            // una Luciérnaga (02 T6). Nunca sale una dosis que no se haya visto.
            BoardState state = Board(T(0x3, 3, 1, 3), Ch(), C(4, 1, 1, 1), C(4, 3, 3), Cuia.Empty(4));
            BoardState lit = Board(state[0].WithRevealed(0x3), Ch(), C(4, 1, 1, 1), C(4, 3, 3), Cuia.Empty(4));

            Assert.That(state[0].TopRunLength, Is.EqualTo(1));
            Assert.That(PourRules.Amount(state, 0, 1, mode), Is.EqualTo(1));
            Assert.That(PourRules.Amount(lit, 0, 1, mode), Is.EqualTo(1));
            BoardState after = PourRules.Apply(state, 0, 1, mode, out PourEvents events);
            Assert.That(events.Amount, Is.EqualTo(1));
            Assert.That(after[1].Packed, Is.EqualTo(Ch(3).Packed));
            Assert.That(after[0].Count, Is.EqualTo(2));
        }

        [Test]
        public void Tinaja_CannotPourIntoHoja_M13r3()
        {
            // c.1: el filtro de origen va antes que CheckTarget del destino, que con Accept saldría sin mirar más: la tinaja no
            // puede verter en una Hoja vacía de la fruta de su boca (combinación permitida, el 332 [P]).
            Cuia hoja = Cuia.Empty(4).WithMechanic(MechanicType.Hoja, key: 3, flags: Cuia.FlagClosed);
            BoardState state = Board(T(0x1, 2, 3), Ch(), hoja, C(4, 1, 3), C(4, 2, 2, 2));

            Assert.That(PourRules.IsLegal(state, 3, 2), Is.True); // una cuia normal con la fruta de la Hoja sí
            Assert.That(PourRules.IsLegal(state, 0, 2), Is.False);
            Assert.That(PourRules.IsLegal(state, 0, 1), Is.True);
        }

        [Test]
        public void Tinaja_CannotPourIntoExposedBarro_M13r3()
        {
            // c.1: el filtro de origen va antes que barro_expuesto(B), que acepta cualquier color (02 §1.2; §9.1 M5): la tinaja
            // no vierte en una cuia con Barro en la cima (Barro fuera de la pareja, combinación permitida).
            BoardState state = Board(T(0x1, 2, 3), Ch(), C(4, 1, Cuia.MudColor), C(4, 1, 1, 3));

            Assert.That(PourRules.IsLegal(state, 3, 2), Is.True); // una cuia normal sí
            Assert.That(PourRules.IsLegal(state, 0, 2), Is.False);
        }

        [Test]
        public void Cucharon_HoldsTwoDosesAnyFruit_M13r5()
        {
            // b.2 regla 5 (Q-Ti2 A): su cuia solo recibe de su tinaja, de cualquier fruta, mientras tenga hueco; dos dosis en
            // capas, que no se mezclan: lo último que se saca es lo primero que se juega. Regla 6: como origen, la regla normal y
            // nunca en la tinaja.
            BoardState state = Board(T(0x3, 2, 1, 3), Ch(), C(4, 1), C(4, 3), C(4, 2, 2, 2));

            Assert.That(PourRules.IsLegal(state, 3, 1), Is.False); // otra cuia con la misma fruta → su cuia vacía: ilegal
            BoardState one = PourRules.Apply(state, 0, 1, PourMode.Run);
            Assert.That(one[1].Packed, Is.EqualTo(Ch(3).Packed));
            Assert.That(PourRules.IsLegal(one, 0, 1), Is.True); // la boca es ahora 1: otra fruta, y entra
            Assert.That(PourRules.IsLegal(one, 3, 1), Is.False);
            BoardState two = PourRules.Apply(one, 0, 1, PourMode.Run);
            Assert.That(two[1].Packed, Is.EqualTo(Ch(3, 1).Packed));
            Assert.That(two[1].TopColor, Is.EqualTo(1));
            Assert.That(PourRules.IsLegal(two, 0, 1), Is.False); // llena

            Assert.That(PourRules.IsLegal(two, 1, 2), Is.True); // su cima (1) va a la cuia con 1 arriba
            Assert.That(PourRules.IsLegal(two, 1, 3), Is.False); // no a la de 3: la regla normal
            Assert.That(PourRules.IsLegal(two, 1, 0), Is.False); // nunca a la tinaja
        }

        [Test]
        public void Tinaja_LastDoseNotFutile_AnexoB()
        {
            // b.2 futil: la tinaja está exenta como origen (no_puede_recibir(A)). Sin la exención, sacar la última fruta visible a
            // su cuia vacía sería «monocolor → vacía con cap ≠ U_c» (fútil) y R13 declararía Aguas estancadas en un nivel que se
            // gana sacando y vaciando en la cuia 2 (R14).
            BoardState state = Board(T(0, 3), Ch(), C(4, 3, 3, 3), C(4, 1, 1, 1, 1));

            Assert.That(CuiaRules.IsFutile(state[0], state[1], state.UnitsTable), Is.True);
            Assert.That(PourRules.IsFutile(state, 0, 1), Is.False);
            Assert.That(PourRules.IsStuck(state, true), Is.False);

            var session = new LevelSession(state, true, PourMode.Run);
            Assert.That(session.TryPour(0, 1, out _), Is.True);
            Assert.That(session.TryPour(1, 2, out PourResult win), Is.True);
            Assert.That(win.Status, Is.EqualTo(LevelStatus.Won));
        }

        [Test]
        public void Cucharon_FullNotFutile_QB()
        {
            // b.2 futil: su cuia, antes de la suelta, también está exenta como origen (como el Remolino en Q-B). Con [x, x] llena,
            // vaciarla en una vacía de 2 o de 5 contaría como fútil, y es justo lo que deja volver a sacar. Tras la suelta es una
            // Cuia chica normal y la exención se acaba.
            BoardState state = Board(T(0x1, 3, 2), Ch(1, 1), Cuia.Empty(2), Cuia.Empty(5), C(4, 1, 1, 2, 2), C(4, 3, 3, 3));

            Assert.That(CuiaRules.IsFutile(state[1], state[2], state.UnitsTable), Is.True);
            Assert.That(CuiaRules.IsFutile(state[1], state[3], state.UnitsTable), Is.True);
            Assert.That(PourRules.IsFutile(state, 1, 2), Is.False);
            Assert.That(PourRules.IsFutile(state, 1, 3), Is.False);

            BoardState released = Board(Cuia.Empty(5).WithMechanic(MechanicType.Tinaja), C(2, 1, 1), Cuia.Empty(2), Cuia.Empty(5), C(4, 1, 1, 2, 2), C(4, 3, 3, 3));
            Assert.That(PourRules.IsFutile(released, 1, 2), Is.True);
        }

        [Test]
        public void Cucharon_ReleasedWhenEmpty_R10f()
        {
            // b.2 regla 8 y R10 (f): en cuanto la tinaja queda vacía, su cuia baja del jirau y pasa a ser una Cuia chica normal
            // (WithoutMechanic; eventos CucharonReleasedMask y ReleasedMask). La tinaja vacía queda inerte y cuenta como vacía
            // para la victoria (regla 9, R12).
            BoardState state = Board(C(4, 2, 2, 2), C(4, 1, 1, 1), T(0x1, 1, 2), Ch(), Cuia.Empty(4));
            var session = new LevelSession(state, true, PourMode.Run);

            Assert.That(session.TryPour(2, 3, out PourResult first), Is.True);
            Assert.That(first.Events.SourceRevealed, Is.True); // regla 7: la dosis de debajo queda visible (R7)
            Assert.That(first.RevealedMask, Is.EqualTo(1 << 2));
            Assert.That(first.Events.CucharonReleasedMask, Is.EqualTo(0));
            Assert.That(session.Current[3].Mechanic, Is.EqualTo(MechanicType.Cucharon));

            Assert.That(session.TryPour(2, 3, out PourResult last), Is.True);
            Assert.That(last.Events.CucharonReleasedMask, Is.EqualTo(1 << 3));
            Assert.That(last.Events.ReleasedMask, Is.EqualTo(1 << 3));
            Assert.That(last.Events.LianaReleasedMask, Is.EqualTo(0));
            Assert.That(last.Events.WasUnlocked(3), Is.True);
            BoardState after = session.Current;
            Assert.That(after[3].Packed, Is.EqualTo(C(2, 2, 1).Packed)); // una Cuia chica normal con [2, 1]
            Assert.That(after[2].Mechanic, Is.EqualTo(MechanicType.Tinaja));
            Assert.That(after[2].IsEmpty, Is.True);
            Assert.That(PourRules.CanBeSource(after, 2), Is.False);
            Assert.That(PourRules.IsLegal(after, 3, 2), Is.False);

            // Ya es una Cuia chica normal: recibe de cualquier cuia con la regla normal.
            Assert.That(session.TryPour(3, 1, out _), Is.True); // sella el 1
            Assert.That(PourRules.IsLegal(session.Current, 0, 3), Is.True);
            Assert.That(session.TryPour(3, 0, out PourResult win), Is.True); // sella el 2
            Assert.That(win.Status, Is.EqualTo(LevelStatus.Won));
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void Marea_RehangsCuiaAndKeepsRevealed_I5(PourMode mode)
        {
            // b.2 «Marea y Volver a la orilla»: Marea deshace la sacada entera (la dosis vuelve a la boca) y, si la cuia había
            // bajado del jirau, vuelve a colgarse (todo vive en Cuia.Packed); lo revelado sigue revelado (R7, T6, I5; Q-Ti6 A),
            // también tras Volver a la orilla.
            BoardState state = Board(C(4, 1, 1, 1), C(4, 2, 2, 2), C(4, 3, 3, 3), T(0x3, 3, 2, 1), Ch());
            var session = new LevelSession(state, true, mode);

            Assert.That(session.TryPour(3, 4, out _), Is.True); // saca 1 y deja ver el 2
            Assert.That(session.TryPour(4, 0, out _), Is.True);
            Assert.That(session.TryPour(3, 4, out _), Is.True); // saca 2 y deja ver el 3
            Assert.That(session.TryPour(4, 1, out _), Is.True);
            Assert.That(session.TryPour(3, 4, out PourResult last), Is.True); // la tinaja queda vacía: su cuia se suelta
            Assert.That(last.Events.CucharonReleasedMask, Is.EqualTo(1 << 4));
            Assert.That(session.Current[4].Mechanic, Is.EqualTo(MechanicType.None));
            Assert.That(session.RevealedLayers(3), Is.EqualTo(0x3));

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Current[4].Packed, Is.EqualTo(Ch().Packed)); // vuelve a colgar, vacía
            Assert.That(session.Current[3].Packed, Is.EqualTo(T(0, 3).Packed));

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Current[3].Packed, Is.EqualTo(T(0, 3, 2, 1).Packed)); // la tinaja entera, a la vista
            Assert.That(session.Current[4].Packed, Is.EqualTo(Ch().Packed));
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));

            session.Restart();
            Assert.That(session.Current[3].HiddenMask, Is.EqualTo(0));
            Assert.That(session.Initial[3].HiddenMask, Is.EqualTo(0x3)); // el tablero inicial no cambia
        }

        [Test]
        public void Luciernaga_RevealsTheWholeTinaja_AndTheAmountStaysOne_T6()
        {
            // b.2 «Luciérnaga»: revela todas las capas de la tinaja (02 T6; 04 §2.5); sacar sigue moviendo una dosis (T4).
            var session = new LevelSession(DebutS0(), true, PourMode.Run);

            Assert.That(session.Reveal(3), Is.EqualTo(2));
            Assert.That(session.Current[3].HiddenMask, Is.EqualTo(0));
            Assert.That(PourRules.Amount(session.Current, 3, 4, PourMode.Run), Is.EqualTo(1));
        }

        [Test]
        public void R14_WhileTheCuiaHangs_StuckOnlyWithoutLegalPours()
        {
            // b.2 R14: mientras la tinaja tenga dosis, R13 solo puede declararse si no queda ningún vertido legal. Ejemplo de
            // bloqueo correcto: Ch[c, a] llena, sin destino para a y sin vacías (y la tinaja, con dosis, no puede sacar).
            BoardState stuck = Board(C(4, 3, 3, 3, 2), C(4, 2, 2, 2, 1), T(0x1, 1, 3), Ch(3, 1), C(4, 1, 1, 2, 3));

            Assert.That(PourRules.HasUsefulMove(stuck, false), Is.False);
            Assert.That(PourRules.IsStuck(stuck, true), Is.True);

            // Con una vacía normal, su cuia llena siempre puede vaciarse en ella (exenta, útil): no hay bloqueo.
            BoardState free = Board(C(4, 3, 3, 3, 2), C(4, 2, 2, 2, 1), T(0x1, 1, 3), Ch(3, 1), C(4, 1, 1, 2, 3), Cuia.Empty(4));
            Assert.That(PourRules.IsStuck(free, true), Is.False);
        }

        [Test]
        public void BoardBuilder_ReadsTinajaAndCucharon_WithoutFields_c1()
        {
            // c.1: BoardBuilder lee los dos tipos, sin id de pareja (una tinaja por nivel). Ejemplo de nivel de b.2.
            BoardBuildResult built = BuildB2Example(new MechanicSpec(MechanicType.Tinaja), new MechanicSpec(MechanicType.Cucharon));

            Assert.That(built.Error, Is.Null);
            BoardState state = built.State!;
            Assert.That(state[8].Mechanic, Is.EqualTo(MechanicType.Tinaja));
            Assert.That(state[8].HiddenMask, Is.EqualTo(0x3));
            Assert.That(state[8].MechanicFlags, Is.EqualTo(0));
            Assert.That(state[9].Mechanic, Is.EqualTo(MechanicType.Cucharon));
            Assert.That(state[9].IsEmpty, Is.True);

            Assert.That(BuildB2Example(new MechanicSpec(MechanicType.Tinaja, key: 3), new MechanicSpec(MechanicType.Cucharon)).Error, Does.Contain("no lleva campos"));
            Assert.That(BuildB2Example(new MechanicSpec(MechanicType.Tinaja), new MechanicSpec(MechanicType.Cucharon, partner: 8)).Error, Does.Contain("no lleva campos"));
            Assert.That(MechanicTypes.TryParse("tinaja", out MechanicType tinaja), Is.True);
            Assert.That(tinaja, Is.EqualTo(MechanicType.Tinaja));
            Assert.That(MechanicTypes.ToKey(MechanicType.Cucharon), Is.EqualTo("cucharon"));
        }

        /// <summary>
        /// Ejemplo de nivel de b.2: caps [4 × 8, 5, 2], la tinaja en la cuia 8 con [3, 8, 6] (ocultas [[8, 0], [8, 1]]) y su
        /// cuia en la 9.
        /// </summary>
        internal static BoardBuildResult BuildB2Example(MechanicSpec tinaja, MechanicSpec cucharon)
        {
            var attrs = new MechanicSpec[10];
            attrs[8] = tinaja;
            attrs[9] = cucharon;
            return BoardBuilder.Build(
                new[] { 4, 4, 4, 4, 4, 4, 4, 4, 5, 2 },
                new[]
                {
                    new[] { 1, 4, 10, 6 }, new[] { 10, 3, 1, 4 }, new[] { 4, 8, 10, 1 }, new[] { 6, 1, 8 }, new[] { 3, 10, 4 },
                    new[] { 8, 6, 3 }, new int[0], new int[0], new[] { 3, 8, 6 }, new int[0],
                },
                attrs,
                new[] { new[] { 8, 0 }, new[] { 8, 1 } });
        }
    }
}
