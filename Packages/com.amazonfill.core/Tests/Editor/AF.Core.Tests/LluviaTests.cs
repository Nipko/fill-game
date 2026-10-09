#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// M6 Lluvia (GDD 02 R3, §9.1 M6 y §9.2; 03 §9; 04 §5.1 regresión 4; propuesta de D-007 b.2 M6 y anexo B; D-007, Q10,
    /// Q14 A y Q18 B; respuesta Q-B del dueño): cada <c>k</c> vertidos útiles cae una gota del tinte agua encima de la cuia de
    /// Lluvia. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class LluviaTests
    {
        /// <summary>Agua del Tapajós (02 §9.1 M6).</summary>
        private const int W = LevelValidation.TapajosWater;

        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static Cuia L(int n, int k, params int[] colorsBottomToTop)
        {
            return Cuia.Create(4, colorsBottomToTop).WithMechanic(MechanicType.Lluvia, key: W, n: n, k: k);
        }

        private static Cuia LC(int n, int k, int counter, params int[] colorsBottomToTop)
        {
            return Cuia.Create(4, colorsBottomToTop).WithMechanic(MechanicType.Lluvia, key: W, n: n, k: k, counter: counter);
        }

        private static BoardState Board(params Cuia[] cuias)
        {
            return new BoardState(cuias);
        }

        /// <summary>Lluvia [2] con 2 gotas y k = 2; 2 → 1 sella el 1 y 4 → 2 junta el 2 (dos vertidos útiles).</summary>
        private static BoardState DropBoard()
        {
            return Board(L(2, 2, 2), C(4, 1, 1, 1), C(4, 2, 2, 1), C(4, W, W), C(4, 2), Cuia.Empty(4));
        }

        [Test]
        public void R3_UWater_CountsThePendingDrops()
        {
            // 02 R3 y §9.1 M6: U_agua = dosis iniciales + gotas pendientes = 4, con el constructor y con BoardBuilder.
            Assert.That(DropBoard().UnitsOf(W), Is.EqualTo(4));

            var attrs = new MechanicSpec[6];
            attrs[0] = new MechanicSpec(MechanicType.Lluvia, key: W, n: 2, k: 2);
            BoardBuildResult built = BoardBuilder.Build(
                new[] { 4, 4, 4, 4, 4, 4 },
                new[] { new[] { 2 }, new[] { 1, 1, 1 }, new[] { 2, 2, 1 }, new[] { W, W }, new[] { 2 }, new int[0] },
                attrs,
                null,
                UnitsPerColor.Uniform(4));
            Assert.That(built.Error, Is.Null);
            Assert.That(built.State!.Equals(DropBoard()), Is.True);
        }

        [Test]
        public void M6_EveryKUsefulPours_ADropOfWaterFallsOnTop_WithoutColorRule_029_1()
        {
            // 02 §9.1 M6: «cada lluvia_k_pours vertidos del jugador cae una dosis del tinte agua del mundo encima de lo que
            // haya, sin regla de color, si hay espacio»; anexo B (e): una vez por vertido útil.
            BoardState state = DropBoard();

            BoardState first = PourRules.Apply(state, 2, 1, PourMode.Run, out PourEvents one);
            Assert.That(one.CountersAdvanced, Is.True);
            Assert.That(one.LluviaDropMask, Is.EqualTo(0));
            Assert.That(first[0].Packed, Is.EqualTo(LC(2, 2, 1, 2).Packed));

            BoardState second = PourRules.Apply(first, 4, 2, PourMode.Run, out PourEvents two);
            Assert.That(two.LluviaDropMask, Is.EqualTo(1 << 0));
            Assert.That(two.LluviaEndedMask, Is.EqualTo(0));
            Assert.That((two.ChangedMask & 1) != 0, Is.True);
            Assert.That(second[0].Packed, Is.EqualTo(LC(1, 2, 0, 2, W).Packed));
            Assert.That(second.UnitsOf(W), Is.EqualTo(4));
        }

        [Test]
        public void Q18B_AFutilePour_DoesNotAdvanceTheCounter_NorChangeTheCanonicalState_R16()
        {
            // D-007, Q18 B: los vertidos fútiles no avanzan el contador; así R16 sigue valiendo con la Lluvia en el tablero.
            BoardState state = DropBoard();
            Assert.That(PourRules.IsFutile(state, 3, 5), Is.True); // [W, W] → vacía, cap = U_agua: fútil

            BoardState after = PourRules.Apply(state, 3, 5, PourMode.Run, out PourEvents events);

            Assert.That(events.Futile, Is.True);
            Assert.That(events.CountersAdvanced, Is.False);
            Assert.That(after[0].Packed, Is.EqualTo(state[0].Packed));
            Assert.That(after.CanonicalHash(), Is.EqualTo(state.CanonicalHash()));

            var session = new LevelSession(state, true, PourMode.Run);
            Assert.That(session.Counter.Remaining, Is.EqualTo(2));
            Assert.That(session.TryPour(3, 5, out PourResult futile), Is.True); // legal (R13)
            Assert.That(futile.Counter.Remaining, Is.EqualTo(2));
            Assert.That(session.TryPour(5, 3, out _), Is.True);
            Assert.That(session.Counter.Remaining, Is.EqualTo(2));
            Assert.That(session.TryPour(2, 1, out PourResult useful), Is.True);
            Assert.That(useful.Counter.Remaining, Is.EqualTo(1));
        }

        [Test]
        public void M6_WhenTheCuiaIsFull_TheDropWaits_AndOnlySealsWithWater_029_1()
        {
            // 02 §9.1 M6: «si está llena, la gota espera al siguiente vertido»; «la cuia Lluvia solo sella con agua». Llena de
            // otra fruta, la Lluvia no está sellada: sigue siendo origen (y por Q-B su vertido a una vacía es útil). Con la
            // última gota se va la nube y la cuia pasa a ser normal.
            BoardState state = Board(L(1, 2, 2, 2, 2), C(4, W, W, W), C(4, 1, 1, 1, 2), C(4, 1), Cuia.Empty(4));
            var session = new LevelSession(state, true, PourMode.Run);

            Assert.That(session.TryPour(2, 0, out PourResult fill), Is.True);
            Assert.That(session.Current[0].IsFull, Is.True);
            Assert.That(session.Current.IsSealed(0), Is.False);
            Assert.That(PourRules.CanBeSource(session.Current, 0), Is.True);
            Assert.That(fill.Counter.Remaining, Is.EqualTo(1));
            Assert.That(fill.Counter.Blocked, Is.True);

            Assert.That(session.TryPour(3, 2, out PourResult wait), Is.True);
            Assert.That(wait.Events.WasSealed(2), Is.True);
            Assert.That(wait.RainWaiting, Is.True);
            Assert.That(wait.RainDropped, Is.False);
            Assert.That(wait.Counter.Remaining, Is.EqualTo(1)); // espera: el siguiente vertido útil la intenta
            Assert.That(wait.Counter.PendingDrops, Is.EqualTo(1));

            Assert.That(PourRules.IsFutile(session.Current, 0, 3), Is.False); // Q-B: origen exento mientras quedan gotas
            Assert.That(session.TryPour(0, 3, out PourResult drop), Is.True);
            Assert.That(drop.RainDropped, Is.True);
            Assert.That(drop.RainEnded, Is.True);
            Assert.That(drop.Counter.IsActive, Is.False);
            Assert.That(session.Current[0].Packed, Is.EqualTo(C(4, W).Packed));

            Assert.That(session.TryPour(0, 1, out PourResult win), Is.True);
            Assert.That(win.TargetSealed, Is.True);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Won));
        }

        [Test]
        public void AnexoB_ADropThatSeals_ReleasesTheLianaWithFruitInTheSamePour()
        {
            // Test obligatorio del anexo B: (e) cae la última gota y sella la Lluvia, (f′) la marca sellada y (d) suelta la
            // Liana con fruta del agua en el mismo vertido.
            Cuia liana = C(4, 1, 1).WithMechanic(MechanicType.Liana, key: W);
            BoardState state = Board(L(1, 2, W, W, W), liana, C(4, 2, 1, 1), C(4, 2, 2, 2), Cuia.Empty(4));

            BoardState first = PourRules.Apply(state, 2, 4, PourMode.Run, out PourEvents one);
            Assert.That(one.Futile, Is.False);
            BoardState after = PourRules.Apply(first, 2, 3, PourMode.Run, out PourEvents events);

            Assert.That(events.LluviaDropMask, Is.EqualTo(1 << 0));
            Assert.That(events.LluviaEndedMask, Is.EqualTo(1 << 0));
            Assert.That(events.WasSealed(3), Is.True);
            Assert.That(events.WasSealed(0), Is.True);
            Assert.That(events.LianaReleasedMask, Is.EqualTo(1 << 1));
            Assert.That(events.FixedPointPasses, Is.GreaterThanOrEqualTo(1));
            Assert.That(after[0].Packed, Is.EqualTo(C(4, W, W, W, W).Packed));
            Assert.That(after[1].Mechanic, Is.EqualTo(MechanicType.None));
            Assert.That(PourRules.CanBeSource(after, 1), Is.True);
        }

        [Test]
        public void Regression4_0451_MonocolorToAnEmptyLluviaWithPendingDrops_IsNotFutile()
        {
            // 04 §5.1 (4) con el anexo B: «monocolor → vacía bajo Lluvia con gotas pendientes no es fútil»
            // (cambia_al_recibir); la misma A hacia una vacía normal sí lo es.
            BoardState state = Board(C(4, 1, 1), L(1, 3), Cuia.Empty(4), C(4, 1, 1, W, W), C(4, W));

            Assert.That(PourRules.IsLegal(state, 0, 1), Is.True);
            Assert.That(PourRules.IsFutile(state, 0, 1), Is.False);
            Assert.That(PourRules.IsFutile(state, 0, 2), Is.True);
        }

        [Test]
        public void QB_TheEndOfLevelCounterexample_IsNotStuck_AndIsWon_R14()
        {
            // Contraejemplo de Q-B: solo queda el agua, [W, W, W] en la Lluvia con 1 gota pendiente, y dos cuias vacías; el
            // resto está sellado. Sin la exención de origen, Lluvia → vacía sería fútil (cap = U_agua) y no avanzaría el
            // contador (Q18 B): R13 declararía Aguas estancadas con solución (rompe R14). Con la respuesta Q-B no es fútil:
            // el vaivén hace pasar los vertidos hasta la gota y se gana en 4.
            BoardState state = Board(L(1, 3, W, W, W), Cuia.Empty(4), Cuia.Empty(4), C(4, 1, 1, 1, 1), C(4, 2, 2, 2, 2));

            Assert.That(PourRules.IsFutile(state, 0, 1), Is.False);
            Assert.That(PourRules.IsStuck(state, true), Is.False);
            Assert.That(state.IsWon(), Is.False); // R12: la gota que falta impide la victoria

            var session = new LevelSession(state, true, PourMode.Run);
            Assert.That(session.TryPour(0, 1, out _), Is.True);
            Assert.That(session.TryPour(1, 0, out _), Is.True); // destino Lluvia con gotas: útil
            Assert.That(session.TryPour(0, 1, out PourResult drop), Is.True);
            Assert.That(drop.RainEnded, Is.True);
            Assert.That(session.Current[0].Packed, Is.EqualTo(C(4, W).Packed));

            // La exención acaba al caer la última gota (respuesta Q-B): la cuia ya es normal y su vertido a una vacía es fútil.
            Assert.That(PourRules.IsFutile(session.Current, 1, 2), Is.True);
            Assert.That(PourRules.IsFutile(session.Current, 0, 2), Is.True);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(session.TryPour(0, 1, out PourResult win), Is.True);
            Assert.That(win.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(session.AppliedPours, Is.EqualTo(4));
        }

        [Test]
        public void R12_PendingDrops_BlockTheVictory()
        {
            // 02 R12 y R3: con gotas pendientes el agua aún no está entera en el tablero. Aquí todo el agua está por caer y no
            // queda ningún vertido: el tablero no es victoria (está atascado y no tiene solución).
            BoardState state = Board(L(4, 3), C(4, 1, 1, 1, 1), C(4, 2, 2, 2, 2));

            Assert.That(state.IsWon(), Is.False);
            Assert.That(new LevelSession(state, true, PourMode.Run).Status, Is.EqualTo(LevelStatus.Stuck));
        }

        [Test]
        public void M6_Marea_RestoresTheCounterAndTheFallenDrop_029_2()
        {
            // 02 §9.2: «Marea restaura contador y gota caída».
            BoardState state = DropBoard();
            var session = new LevelSession(state, true, PourMode.Run);
            Assert.That(session.Counter.Type, Is.EqualTo(MechanicType.Lluvia));
            Assert.That(session.Counter.CuiaIndex, Is.EqualTo(0));
            Assert.That(session.Counter.Rhythm, Is.EqualTo(2));
            Assert.That(session.Counter.PendingDrops, Is.EqualTo(2));

            Assert.That(session.TryPour(2, 1, out _), Is.True);
            BoardState middle = session.Current;
            Assert.That(session.TryPour(4, 2, out PourResult drop), Is.True);
            Assert.That(drop.RainDropped, Is.True);
            Assert.That(session.Counter.PendingDrops, Is.EqualTo(1));
            Assert.That(session.Counter.Remaining, Is.EqualTo(2));

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Current.Equals(middle), Is.True);
            Assert.That(session.Counter.PendingDrops, Is.EqualTo(2));
            Assert.That(session.Counter.Remaining, Is.EqualTo(1));
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Current.Equals(state), Is.True);

            session.TryPour(2, 1, out _);
            session.TryPour(4, 2, out _);
            session.Restart();
            Assert.That(session.Current.Equals(state), Is.True);
            Assert.That(session.Counter.Remaining, Is.EqualTo(2));
        }

        [Test]
        public void LevelCounter_WithoutLluviaNorPirana_IsNone()
        {
            var session = new LevelSession(Board(C(4, 1, 2, 1, 2), C(4, 2, 1, 2, 1), Cuia.Empty(4)), true, PourMode.Run);

            Assert.That(session.Counter.IsActive, Is.False);
            Assert.That(session.Counter.Remaining, Is.EqualTo(0));
            Assert.That(session.Counter.CuiaIndex, Is.EqualTo(-1));
            Assert.That(session.Counter.Equals(LevelCounter.None), Is.True);
        }

        [Test]
        public void M6_Validation_WorldWater_FourUnits_OneCounter_NoHiddenDoses()
        {
            // 02 §9.1 M6 (key = 10 o 14; U_agua = 4), 08 §9 (k de 2 a 5), propuesta b.2 M6 (un contador por nivel; nunca con
            // la Piraña) y D-007, Q14 A (nunca con dosis ocultas).
            Assert.That(LevelValidation.Validate(Valid(W, 2)), Is.Null);
            Assert.That(LevelValidation.Validate(Valid(LevelValidation.RioNegroWater, 2)), Is.Null);
            Assert.That(LevelValidation.Validate(Valid(9, 2)), Does.Contain("agua del mundo"));

            // n + dosis iniciales = 5 con una Cuia alta de 5 (la regla general de 02 §8 no lo rechaza; la de la Lluvia sí).
            int[] caps = { 4, 4, 4, 4, 4, 4, 5 };
            int[][] doses = { new[] { 1, 2, W }, new[] { 2, 1, W }, new[] { 1, 2 }, new[] { 1, 2 }, new int[0], new int[0], new int[0] };
            var attrs = new MechanicSpec[7];
            attrs[4] = new MechanicSpec(MechanicType.Lluvia, key: W, n: 3);
            BoardBuildResult five = BoardBuilder.Build(caps, doses, attrs);
            Assert.That(five.Error, Is.Null);
            Assert.That(LevelValidation.Validate(five.State!), Does.Contain("U_agua"));

            BoardState valid = Valid(W, 2);
            var cuias = new Cuia[valid.Count];
            for (int i = 0; i < cuias.Length; i++)
            {
                cuias[i] = valid[i];
            }

            cuias[5] = Cuia.Empty(2).WithMechanic(MechanicType.Pirana, k: 4);
            Assert.That(LevelValidation.Validate(new BoardState(cuias)), Does.Contain("un contador"));
            cuias[5] = Cuia.Empty(4).WithMechanic(MechanicType.Lluvia, key: W, n: 1, k: 3);
            cuias[0] = Cuia.Create(4, new[] { 1, 2 });
            Assert.That(LevelValidation.Validate(new BoardState(cuias)), Does.Contain("un contador"));

            cuias = new Cuia[valid.Count];
            for (int i = 0; i < cuias.Length; i++)
            {
                cuias[i] = valid[i];
            }

            cuias[4] = cuias[4].WithMechanic(MechanicType.Lluvia, key: W, n: 2, k: 1);
            Assert.That(LevelValidation.Validate(new BoardState(cuias)), Does.Contain("k va de 2 a 5"));
            cuias[4] = cuias[4].WithMechanic(MechanicType.Lluvia, key: W, n: 2, k: 3, counter: 1);
            Assert.That(LevelValidation.Validate(new BoardState(cuias)), Does.Contain("empieza en 0"));
            cuias[4] = cuias[4].WithMechanic(MechanicType.Lluvia, key: W, n: 2, k: 3);
            cuias[2] = Cuia.Create(4, new[] { 1, 2 }, 0x1);
            Assert.That(LevelValidation.Validate(new BoardState(cuias)), Does.Contain("Q14 A"));
        }

        /// <summary>Nivel válido con Lluvia de 2 gotas sobre el tinte <paramref name="water"/> (dosis iniciales 2).</summary>
        private static BoardState Valid(int water, int n)
        {
            int[] caps = { 4, 4, 4, 4, 4, 4 };
            int[][] doses = { new[] { 1, 2, water }, new[] { 2, 1, water }, new[] { 1, 2 }, new[] { 1, 2 }, new int[0], new int[0] };
            var attrs = new MechanicSpec[6];
            attrs[4] = new MechanicSpec(MechanicType.Lluvia, key: water, n: n);
            BoardBuildResult built = BoardBuilder.Build(caps, doses, attrs, null, UnitsPerColor.Uniform(4));
            Assert.That(built.Error, Is.Null);
            return built.State!;
        }
    }
}
