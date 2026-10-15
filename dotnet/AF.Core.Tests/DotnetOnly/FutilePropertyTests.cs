#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Solver.Tests;
using NUnit.Framework;

namespace AF.Core.Tests.DotnetOnly
{
    /// <summary>
    /// Propiedades de R13, R14 y R16 (02 §1.4) con la <c>futil</c> del anexo B (D-007, Q13), en los dos modos de vertido y
    /// con capacidades uniformes y mixtas, sobre todos los estados alcanzables de tableros pequeños con semilla fija
    /// (<see cref="PropertyBoards"/>; oráculo: <see cref="ExhaustiveSearch"/>, BFS con todos los vertidos legales):
    /// <list type="bullet">
    /// <item><description>P1 (R14): todo estado alcanzable con <c>atascado = true</c> es irresoluble.</description></item>
    /// <item><description>P2 (R16, <c>run</c>): un vertido fútil entre dos cuias sin mecánica y de la misma capacidad no cambia
    /// el estado canónico. Con capacidades distintas puede cambiarlo sin crear progreso (ver
    /// <see cref="R16_InRun_AFutilePourBetweenDifferentCapacities_CanChangeTheCanonicalState"/>); la garantía que
    /// importa es P1.</description></item>
    /// <item><description>P3 (anexo B): ningún par monocolor ↔ vacía es útil en los dos sentidos sin progreso (el vaivén no
    /// vuelve al mismo estado canónico). Con mecánicas un vaivén útil puede existir si cambia el estado: la Hoja de 1 que
    /// cae y después deja ir su fruta a la «casa correcta» de 4 (02 §1.4). <b>Excepción del Remolino por Q-B</b>: con la
    /// exención de origen, X (Remolino monocolor) → vacía es útil y vacía → X entra por el fondo de la pareja, así que con la
    /// pareja vacía se vuelve al mismo estado canónico (X e Y son simétricos); se cuenta aparte
    /// (<see cref="Report.RemolinoCycles"/>) y lo fija
    /// <see cref="P3_Remolino_QB_AUsefulRoundTripCanReturnToTheSameCanonicalState"/>. <b>Hoja atada</b>: dos de la misma fruta
    /// incumplen P3 (<see cref="P3_HojaAtada_TwoOfTheSameFruit_ARoundTripReturnsToTheSameState_D024RejectsIt"/>), y por eso
    /// D-024 (punto 2) deja como mucho una por fruta, que <c>LevelValidation</c> comprueba; con un ejemplar por fruta, P3 se
    /// cumple.</description></item>
    /// </list>
    /// Casos conocidos de P1 con capacidades mixtas (bloqueo C1-9, pregunta 24 b): la Liana
    /// (<see cref="P1_Liana_MixedCapacities_OpenR16Counterexample"/>) y la Flor de vitória-régia, su variante con dos llaves
    /// (<see cref="P1_Flor_MixedCapacities_OpenR16Counterexample"/>, K3b).
    /// Cada mecánica añade su inyector (<see cref="IMechanicInjector"/>) antes de su debut (02 R16); la Tinaja (M13), que añade
    /// dos cuias y la máscara de ocultos, su propio generador (<see cref="TinajaBoards"/>). Solo dotnet: lotes grandes.
    /// </summary>
    [TestFixture]
    public sealed class FutilePropertyTests
    {
        private const int MinStates = 400;
        private const int MaxBoards = 400;
        private const int MaxStatesPerBoard = 20000;
        private const int TargetStates = 20000;

        /// <summary>Cuenta de estados y casos comprobados de una combinación (modo × familia × mecánica).</summary>
        internal sealed class Report
        {
            public int Boards;
            public int States;
            public int Stuck;
            public int FutilePours;
            public int UsefulMonoToEmpty;
            public int UsefulRoundTrips;
            public int RemolinoCycles;
            public int CucharonHung;
            public int CucharonReleased;
            public readonly List<string> Violations = new List<string>();
        }

        [TestCase(PourMode.Run, CapacityFamily.Uniform)]
        [TestCase(PourMode.Run, CapacityFamily.Mixed)]
        [TestCase(PourMode.Single, CapacityFamily.Uniform)]
        [TestCase(PourMode.Single, CapacityFamily.Mixed)]
        public void P1_P2_P3_HoldOnEveryReachableState(PourMode mode, CapacityFamily family)
        {
            Report report = Run(mode, family, null, 0xF07E_0001UL);

            TestContext.WriteLine(Describe(report));
            Assert.That(report.Violations, Is.Empty);
            Assert.That(report.States, Is.GreaterThanOrEqualTo(MinStates));
            Assert.That(report.FutilePours, Is.GreaterThan(0));
        }

        [TestCase(PourMode.Run, CapacityFamily.Uniform)]
        [TestCase(PourMode.Run, CapacityFamily.Mixed)]
        [TestCase(PourMode.Single, CapacityFamily.Uniform)]
        [TestCase(PourMode.Single, CapacityFamily.Mixed)]
        public void P1_P2_P3_HoldWithHoja(PourMode mode, CapacityFamily family)
        {
            // C1-7: 02 R16 antes del debut de la Hoja (43).
            AssertWith(mode, family, new HojaInjector(), 0xF07E_4301UL);
        }

        [TestCase(PourMode.Run, CapacityFamily.Uniform)]
        [TestCase(PourMode.Run, CapacityFamily.Mixed)]
        [TestCase(PourMode.Single, CapacityFamily.Uniform)]
        [TestCase(PourMode.Single, CapacityFamily.Mixed)]
        public void P1_P2_P3_HoldWithBrote(PourMode mode, CapacityFamily family)
        {
            // C1-8: 02 R16 antes del debut del Brote (163).
            AssertWith(mode, family, new BroteInjector(), 0xF07E_1631UL);
        }

        [TestCase(PourMode.Run, CapacityFamily.Uniform)]
        [TestCase(PourMode.Run, CapacityFamily.Mixed)]
        [TestCase(PourMode.Single, CapacityFamily.Uniform)]
        [TestCase(PourMode.Single, CapacityFamily.Mixed)]
        public void P1_P2_P3_HoldWithHojaAtada(PourMode mode, CapacityFamily family)
        {
            // K3a: 02 R16 antes del debut de la Hoja atada (283; propuesta de D-007 b.3), con la exención de futil como destino
            // (02 §1.4, anexo B) y sin exención como origen. Un solo ejemplar por tablero (el del estreno, 02 §9.2 regla 2); dos de
            // frutas distintas, más abajo; dos de la misma fruta incumplen P3 y D-024 las prohíbe (más abajo).
            AssertWith(mode, family, new HojaAtadaInjector(), 0xF07E_2831UL);
        }

        [TestCase(PourMode.Run, CapacityFamily.Uniform)]
        [TestCase(PourMode.Run, CapacityFamily.Mixed)]
        [TestCase(PourMode.Single, CapacityFamily.Uniform)]
        [TestCase(PourMode.Single, CapacityFamily.Mixed)]
        public void P1_P2_P3_HoldWithBroteDosHojas(PourMode mode, CapacityFamily family)
        {
            // K3a: 02 R16 antes del debut del Brote de dos hojas (343; 02 R10 b; propuesta de D-007 b.3): en run un tramo de 2
            // lo abre de una vez y en single hacen falta dos vertidos.
            AssertWith(mode, family, new BroteDosHojasInjector(), 0xF07E_3431UL);
        }

        [TestCase(PourMode.Run, CapacityFamily.Uniform)]
        [TestCase(PourMode.Run, CapacityFamily.Mixed)]
        [TestCase(PourMode.Single, CapacityFamily.Uniform)]
        [TestCase(PourMode.Single, CapacityFamily.Mixed)]
        public void P1_P2_P3_HoldWithMatapi(PourMode mode, CapacityFamily family)
        {
            // C1-8: 02 R16 antes del debut del Matapi (63).
            AssertWith(mode, family, new MatapiInjector(), 0xF07E_0631UL);
        }

        [TestCase(PourMode.Run, CapacityFamily.Uniform)]
        [TestCase(PourMode.Run, CapacityFamily.Mixed)]
        [TestCase(PourMode.Single, CapacityFamily.Uniform)]
        [TestCase(PourMode.Single, CapacityFamily.Mixed)]
        public void P1_P2_P3_HoldWithBarro(PourMode mode, CapacityFamily family)
        {
            // C2-3: 02 R16 antes del debut del Barro (123).
            AssertWith(mode, family, new BarroInjector(), 0xF07E_1231UL);
        }

        [TestCase(PourMode.Run, CapacityFamily.Uniform)]
        [TestCase(PourMode.Run, CapacityFamily.Mixed)]
        [TestCase(PourMode.Single, CapacityFamily.Uniform)]
        [TestCase(PourMode.Single, CapacityFamily.Mixed)]
        public void P1_P2_P3_HoldWithRemolino(PourMode mode, CapacityFamily family)
        {
            // C2-4: 02 R16 antes del debut del Remolino (103).
            AssertWith(mode, family, new RemolinoInjector(), 0xF07E_1031UL);
        }

        [TestCase(PourMode.Run, CapacityFamily.Uniform)]
        [TestCase(PourMode.Run, CapacityFamily.Mixed)]
        [TestCase(PourMode.Single, CapacityFamily.Uniform)]
        [TestCase(PourMode.Single, CapacityFamily.Mixed)]
        public void P1_P2_P3_HoldWithLluvia(PourMode mode, CapacityFamily family)
        {
            // C3-1: 02 R16 antes del debut de la Lluvia (143), con Q18 B (los fútiles no avanzan el contador) y la exención
            // de origen de Q-B mientras quedan gotas. Un vaivén útil Lluvia ↔ vacía nunca vuelve al mismo estado: el contador
            // o las gotas pendientes cambian.
            AssertWith(mode, family, new LluviaInjector(), 0xF07E_1431UL);
        }

        [TestCase(PourMode.Run, CapacityFamily.Uniform)]
        [TestCase(PourMode.Run, CapacityFamily.Mixed)]
        [TestCase(PourMode.Single, CapacityFamily.Uniform)]
        [TestCase(PourMode.Single, CapacityFamily.Mixed)]
        public void P1_P2_P3_HoldWithPirana(PourMode mode, CapacityFamily family)
        {
            // C3-2: 02 R16 antes del debut de la Piraña (183), con Q18 B (los fútiles no avanzan el contador) y la respuesta
            // Q-C (mordisco perdido si no puede morder). La cueva es una cuia normal para futil.
            AssertWith(mode, family, new PiranaInjector(), 0xF07E_1831UL);
        }

        [TestCase(PourMode.Run, CapacityFamily.Uniform)]
        [TestCase(PourMode.Run, CapacityFamily.Mixed)]
        [TestCase(PourMode.Single, CapacityFamily.Uniform)]
        [TestCase(PourMode.Single, CapacityFamily.Mixed)]
        public void P1_P2_P3_HoldWithTinaja(PourMode mode, CapacityFamily family)
        {
            // M13 (propuesta de la tinaja, b.2 R14 y c.6), antes del estreno: con capacidades 2, 4 y 5 (uniformes: cuias de 4,
            // la tinaja de 5 y su Cuia chica de 2) y además 1-3 y 6 (mixtas: Cuia alta de 5-6 con su fruta abundante y Cuia
            // chica normal), en run y en single, con las exenciones de futil de la tinaja y de su cuia colgada. Los estados
            // alcanzables incluyen los dos lados de la suelta (con la cuia colgada y ya suelta): Marea restaura el estado
            // anterior entero, que es uno de ellos, y la vuelve a colgar.
            bool mixed = family == CapacityFamily.Mixed;
            Report report = Run(mode, mixed ? 0xF07E_3232UL : 0xF07E_3231UL, rng => TinajaBoards.Start(rng, mixed));

            TestContext.WriteLine("tinaja: " + Describe(report));
            Assert.That(report.Violations, Is.Empty);
            Assert.That(report.States, Is.GreaterThanOrEqualTo(MinStates));
            Assert.That(report.CucharonHung, Is.GreaterThan(0));
            Assert.That(report.CucharonReleased, Is.GreaterThan(0));
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void P3_Tinaja_AfterTheRelease_AUsefulBackAndForthReturnsToTheSameState_R13IsNotComplete(PourMode mode)
        {
            // b.2 futil, «Lo que Φ no dice» (y la propuesta [P] para P3): tras la suelta, la Cuia chica con 1 dosis y una cuia con
            // racha de 2 o más de esa fruta forman un vaivén «útil» en los dos sentidos, aunque el nivel esté perdido. Desde el
            // tablero perdido de b.2 (N[c,c,c] N[a,b,a,a] N[b,b,b] T[c(o),a] Ch[]): sacar a, sacar c (la cuia se suelta con
            // [a, c]) y ch→N1 (sella c). Queda N[c,c,c,c] N[a,b,a,a] N[b,b,b] T[] ch[a], y N2→ch / ch→N2 vuelven al mismo estado.
            // R14 se cumple. PourRules.IsStuck (R13 de reglas) no lo declara; la partida sí, por D-014 (cierre ≤ 64 estados sin
            // victoria en LevelSession; T51). Este test fija solo lo de las reglas.
            Cuia tinaja = Cuia.Create(5, new[] { 3, 1 }, 0x1).WithMechanic(AF.Core.Mechanics.MechanicType.Tinaja);
            Cuia hung = Cuia.Empty(2).WithMechanic(AF.Core.Mechanics.MechanicType.Cucharon);
            var lost = new BoardState(new[] { Cuia.Create(4, new[] { 3, 3, 3 }), Cuia.Create(4, new[] { 1, 2, 1, 1 }), Cuia.Create(4, new[] { 2, 2, 2 }), tinaja, hung });

            BoardState state = PourRules.Apply(lost, 3, 4, mode);
            state = PourRules.Apply(state, 3, 4, mode);
            Assert.That(state[4].Packed, Is.EqualTo(Cuia.Create(2, new[] { 1, 3 }).Packed)); // ya suelta
            state = PourRules.Apply(state, 4, 0, mode);
            Assert.That(state.IsSealed(0), Is.True);

            Assert.That(PourRules.IsLegal(state, 1, 4), Is.True);
            Assert.That(PourRules.IsFutile(state, 1, 4), Is.False);
            BoardState there = PourRules.Apply(state, 1, 4, mode);
            Assert.That(PourRules.IsLegal(there, 4, 1), Is.True);
            Assert.That(PourRules.IsFutile(there, 4, 1), Is.False);
            BoardState back = PourRules.Apply(there, 4, 1, mode);

            Assert.That(back.Equals(state), Is.True);
            Assert.That(PourRules.IsStuck(state, true), Is.False);
            Assert.That(PourRules.IsStuck(there, true), Is.False);
            Assert.That(ExhaustiveSearch.Explore(state, mode, MaxStatesPerBoard).Distance[0], Is.EqualTo(int.MaxValue)); // perdido
            Assert.That(ExhaustiveSearch.Explore(lost, mode, MaxStatesPerBoard).Distance[0], Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void P3_Remolino_QB_AUsefulRoundTripCanReturnToTheSameCanonicalState()
        {
            // Consecuencia de la respuesta Q-B (exención de futil también en el origen Remolino) para la integración: el
            // anexo B avisaba de que eximir orígenes rompe la detección. X = Remolino [1, 1], su pareja Y vacía, una vacía E:
            // X → E es útil (Q-B) y E → X también (destino Remolino, entra por el fondo de Y): el tablero vuelve al mismo
            // estado canónico. R13 nunca declara bloqueo con progreso posible (P1 se cumple con Remolino), pero un ir y venir
            // sin progreso así cuenta como útil y puede retrasar el modal de Aguas estancadas.
            AF.Core.Mechanics.MechanicType remolino = AF.Core.Mechanics.MechanicType.Remolino;
            var state = new BoardState(new[]
            {
                Cuia.Create(4, new[] { 1, 1 }).WithMechanic(remolino, pairId: 0), Cuia.Empty(4).WithMechanic(remolino, pairId: 0),
                Cuia.Empty(4), Cuia.Create(4, new[] { 1, 1 }),
            });

            Assert.That(PourRules.IsFutile(state, 0, 2), Is.False);
            BoardState middle = PourRules.Apply(state, 0, 2, PourMode.Run);
            Assert.That(PourRules.IsFutile(middle, 2, 0), Is.False);
            BoardState back = PourRules.Apply(middle, 2, 0, PourMode.Run);

            Assert.That(back.Equals(state), Is.False);
            Assert.That(back.CanonicalHash(), Is.EqualTo(state.CanonicalHash()));
        }

        private const string LianaMixedBlocked =
            "BLOQUEADO (C1-9): con Liana y capacidades mixtas P1 falla; pregunta de 02 R16 al tech-lead "
            + "(ver P1_Liana_MixedCapacities_OpenR16Counterexample).";

        [TestCase(PourMode.Run, CapacityFamily.Uniform, true)]
        [TestCase(PourMode.Run, CapacityFamily.Mixed, true, Ignore = LianaMixedBlocked)]
        [TestCase(PourMode.Single, CapacityFamily.Uniform, true)]
        [TestCase(PourMode.Single, CapacityFamily.Mixed, true, Ignore = LianaMixedBlocked)]
        [TestCase(PourMode.Run, CapacityFamily.Uniform, false)]
        [TestCase(PourMode.Run, CapacityFamily.Mixed, false, Ignore = LianaMixedBlocked)]
        [TestCase(PourMode.Single, CapacityFamily.Uniform, false)]
        [TestCase(PourMode.Single, CapacityFamily.Mixed, false, Ignore = LianaMixedBlocked)]
        public void P1_P2_P3_HoldWithLiana(PourMode mode, CapacityFamily family, bool withFruit)
        {
            // C1-9: 02 R16 antes del debut de la Liana con fruta (83) y de la de cuenta (243). Con capacidades uniformes P1-P3
            // se cumplen; con mixtas P1 falla (contraejemplo abajo) y LevelValidation bloquea esa combinación.
            AssertWith(mode, family, new LianaInjector(withFruit), withFruit ? 0xF07E_0831UL : 0xF07E_2431UL);
        }

        private const string FlorMixedBlocked =
            "BLOQUEADO (C1-9, pregunta 24 b): con Flor y capacidades mixtas P1 falla, como con la Liana "
            + "(ver P1_Flor_MixedCapacities_OpenR16Counterexample).";

        [TestCase(PourMode.Run, CapacityFamily.Uniform)]
        [TestCase(PourMode.Run, CapacityFamily.Mixed, Ignore = FlorMixedBlocked)]
        [TestCase(PourMode.Single, CapacityFamily.Uniform)]
        [TestCase(PourMode.Single, CapacityFamily.Mixed, Ignore = FlorMixedBlocked)]
        public void P1_P2_P3_HoldWithFlor(PourMode mode, CapacityFamily family)
        {
            // K3b: 02 R16 antes del debut de la Flor de vitória-régia (403; propuesta de D-007 b.3), con su apertura por sellados
            // en la etapa (d) de R10. Con capacidades uniformes P1-P3 se cumplen; con mixtas P1 falla (contraejemplo abajo; la
            // muestra de esta semilla no lo encuentra) y LevelValidation bloquea esa combinación, como la de la Liana.
            AssertWith(mode, family, new FlorInjector(), 0xF07E_4031UL);
        }

        [Test]
        public void P1_Flor_MixedCapacities_OpenR16Counterexample()
        {
            // K3b: el contraejemplo de la Liana (P1_Liana_MixedCapacities_OpenR16Counterexample) vale para la Flor, que es una
            // Liana con dos llaves. Tablero que solo rechaza el bloqueo: Cuia alta de 5 con 4 dosis de 2 (la casa de la fruta
            // abundante 1, U_1 = 5, está ocupada), [1, 1, 1] en una cuia de 4, una Cuia chica de 3 vacía, la Flor (2 y 3) con
            // [1, 1], [3, 3, 3] y una Cuia chica de 1 con el último 3. El vertido 5→4 sella el 3 (la flor sigue cerrada) y deja el
            // tablero en Aguas estancadas (solo quedan fútiles: [1,1,1] o [2,2,2,2] a una Cuia chica que no es su casa), pero el
            // fútil [1,1,1] → chica libera una cuia de 4 para el 2, que sella y abre la flor: hay solución en 4. Fija el
            // comportamiento actual; cuando se decida la pregunta de 02 R16 tendrá que cambiar.
            Cuia flor = Cuia.Create(4, new[] { 1, 1 }).WithMechanic(MechanicType.Flor, key: 2, key2: 3);
            var start = new BoardState(new[]
            {
                Cuia.Create(5, new[] { 2, 2, 2, 2 }), Cuia.Create(4, new[] { 1, 1, 1 }), Cuia.Empty(3), flor,
                Cuia.Create(4, new[] { 3, 3, 3 }), Cuia.Create(1, new[] { 3 }),
            });
            Assert.That(LevelValidation.Validate(start), Does.Contain("bloqueo provisional"));

            BoardState state = PourRules.Apply(start, 5, 4, PourMode.Run);
            Assert.That(state.IsSealed(4), Is.True);
            Assert.That(state[3].Mechanic, Is.EqualTo(MechanicType.Flor));
            Assert.That(PourRules.IsStuck(state, true), Is.True);
            Assert.That(PourRules.IsFutile(state, 1, 2), Is.True);
            ExhaustiveSearch.Graph graph = ExhaustiveSearch.Explore(state, PourMode.Run, MaxStatesPerBoard);
            Assert.That(graph.Distance[0], Is.EqualTo(4));
        }

        [Test]
        public void P1_Liana_MixedCapacities_OpenR16Counterexample()
        {
            // Contraejemplo de P1 (R14) que para C1-9 (pregunta de 02 R16 y 02 §1.4 para el tech-lead), alcanzable desde un
            // tablero generado: Cuia alta de 5 con 4 dosis de 2 (la casa de la fruta abundante 1, U_1 = 5, está ocupada),
            // [1, 1, 1] en una cuia de 4, una Cuia chica de 3 vacía y una Liana con fruta 2 que guarda [1, 1] en la otra cuia
            // de 4. Solo quedan vertidos fútiles ([1,1,1] → chica y [2,2,2,2] → chica: ninguna es la casa de su fruta), así
            // que R13 declara Aguas estancadas; pero el fútil [1,1,1] → chica libera una cuia de 4 para el 2, que sella y
            // suelta la Liana: hay solución. Este test fija el comportamiento actual; cuando se decida la pregunta tendrá
            // que cambiar.
            Cuia liana = Cuia.Create(4, new[] { 1, 1 }).WithMechanic(AF.Core.Mechanics.MechanicType.Liana, key: 2);
            var state = new BoardState(new[] { Cuia.Create(5, new[] { 2, 2, 2, 2 }), Cuia.Create(4, new[] { 1, 1, 1 }), Cuia.Empty(3), liana });

            Assert.That(PourRules.IsStuck(state, true), Is.True);
            Assert.That(PourRules.IsFutile(state, 1, 2), Is.True);
            ExhaustiveSearch.Graph graph = ExhaustiveSearch.Explore(state, PourMode.Run, MaxStatesPerBoard);
            Assert.That(graph.Distance[0], Is.EqualTo(4));
            Assert.That(LevelValidation.Validate(state), Does.Contain("bloqueo provisional"));
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void P3_HojaAtada_NotTheHomeOfItsFruit_ARoundTripReturnsToTheSameState(PourMode mode)
        {
            // K3a: por qué la Hoja atada es la casa de su fruta (D-024, punto 1; 02 R16). Con la exención de destino de
            // futil (02 §1.4, anexo B), una Cuia chica de 3 atada al 1 (U_1 = 4) rompe P3: [1, 1, 1] en su casa de 4 → la hoja
            // atada es útil (cambia al recibir) y volver es útil (va a la casa correcta), y el tablero vuelve al mismo estado
            // canónico. Con la regla (capacidad = U_c de su llave), P1-P3 se cumplen con un ejemplar por fruta
            // (P1_P2_P3_HoldWithHojaAtada y P1_P2_P3_HoldWithTwoHojasAtadasOfDifferentFruits) y LevelValidation rechaza este
            // tablero.
            Cuia atada = Cuia.Empty(3).WithMechanic(AF.Core.Mechanics.MechanicType.HojaAtada, key: 1);
            var state = new BoardState(new[] { Cuia.Create(4, new[] { 1, 1, 1 }), atada, Cuia.Create(4, new[] { 2, 2, 2, 1 }), Cuia.Create(4, new[] { 2 }) });

            Assert.That(PourRules.IsFutile(state, 0, 1), Is.False);
            BoardState there = PourRules.Apply(state, 0, 1, mode);
            if (mode == PourMode.Single)
            {
                there = PourRules.Apply(PourRules.Apply(there, 0, 1, mode), 0, 1, mode);
            }

            Assert.That(there[0].IsEmpty, Is.True);
            Assert.That(PourRules.IsLegal(there, 1, 0), Is.True);
            Assert.That(PourRules.IsFutile(there, 1, 0), Is.False);
            BoardState back = PourRules.Apply(there, 1, 0, mode);
            if (mode == PourMode.Single)
            {
                back = PourRules.Apply(PourRules.Apply(back, 1, 0, mode), 1, 0, mode);
            }

            Assert.That(back.CanonicalHash(), Is.EqualTo(state.CanonicalHash()));
            Assert.That(LevelValidation.Validate(state), Does.Contain("casa de su fruta"));
        }

        [TestCase(PourMode.Run, 2)]
        [TestCase(PourMode.Run, 1)]
        [TestCase(PourMode.Single, 1)]
        public void P3_HojaAtada_TwoOfTheSameFruit_ARoundTripReturnsToTheSameState_D024RejectsIt(PourMode mode, int doses)
        {
            // Contraejemplo de P3 (02 R16; anexo B) de la revisión de K3a: dos Hojas atadas de la misma fruta, las dos de
            // capacidad U_c (la casa de su fruta, D-024, punto 1). Con la fruta en una y la otra vacía, atada → atada es útil
            // (cambia al recibir) y volver también, y el tablero vuelve al mismo estado. Basta un vertido para volver al mismo
            // estado canónico, porque el multiconjunto de cuias no cambia: es un vertido útil que equivale a no mover nada, así
            // que atascado(S) nunca se cumple mientras exista el par. Es el problema del Remolino con Q-B; el dueño eligió la
            // opción (a), sin excepción de P3: como mucho una Hoja atada por fruta (D-024, punto 2), y LevelValidation rechaza
            // el tablero. Las reglas de vertido no cambian: el vaivén sigue existiendo, pero no puede salir en un nivel válido
            // (las Hojas atadas ni aparecen ni se sueltan durante la partida).
            MechanicType hojaAtada = MechanicType.HojaAtada;
            int[] fruit = doses == 2 ? new[] { 1, 1 } : new[] { 1 };
            int[] rest = doses == 2 ? new[] { 2, 2 } : new[] { 2, 2, 1 };
            var state = new BoardState(new[]
            {
                Cuia.Create(4, fruit).WithMechanic(hojaAtada, key: 1), Cuia.Empty(4).WithMechanic(hojaAtada, key: 1),
                Cuia.Create(4, new[] { 2, 2, 1, 1 }), Cuia.Create(4, rest),
            });
            Assert.That(LevelValidation.Validate(state), Does.Contain("D-024").And.Contain("como mucho una Hoja atada por fruta"));

            Assert.That(PourRules.IsLegal(state, 0, 1), Is.True);
            Assert.That(PourRules.IsFutile(state, 0, 1), Is.False);
            BoardState there = PourRules.Apply(state, 0, 1, mode);
            Assert.That(there[0].IsEmpty, Is.True);
            Assert.That(there.Equals(state), Is.False);
            Assert.That(there.CanonicalHash(), Is.EqualTo(state.CanonicalHash()), "un solo vertido útil deja el mismo estado canónico");

            Assert.That(PourRules.IsLegal(there, 1, 0), Is.True);
            Assert.That(PourRules.IsFutile(there, 1, 0), Is.False);
            BoardState back = PourRules.Apply(there, 1, 0, mode);
            Assert.That(back.Equals(state), Is.True);
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void P3_HojaAtada_TwoOfTheSameFruit_RulesNeverDeclareTheLostBoard_DeadEndProbeDoes(PourMode mode)
        {
            // El mismo par en un tablero perdido: las dos cuias llenas tienen el 2 en la cima y las Hojas atadas solo reciben el 1,
            // así que el único vertido legal es el ir y venir entre las dos Hojas atadas. R13 de reglas (PourRules.IsStuck) no
            // declara Aguas estancadas, porque el vertido es útil; la partida sí, por el cierre de D-014 (DeadEndProbe), como con
            // el Remolino de Q-B y la Tinaja tras la suelta. P1 (R14) se cumple: un vertido útil de más nunca declara bloqueo. Con
            // D-024 (punto 2) este par no puede salir en un nivel válido; el test queda como red de seguridad de las reglas.
            MechanicType hojaAtada = MechanicType.HojaAtada;
            var lost = new BoardState(new[]
            {
                Cuia.Create(4, new[] { 1 }).WithMechanic(hojaAtada, key: 1), Cuia.Empty(4).WithMechanic(hojaAtada, key: 1),
                Cuia.Create(4, new[] { 1, 1, 2, 2 }), Cuia.Create(3, new[] { 1, 2, 2 }),
            });

            Assert.That(PourRules.IsStuck(lost, true), Is.False);
            Assert.That(ExhaustiveSearch.Explore(lost, mode, MaxStatesPerBoard).Distance[0], Is.EqualTo(int.MaxValue)); // perdido
            Assert.That(new DeadEndProbe().IsDeadEnd(lost, mode), Is.True);
            Assert.That(LevelValidation.Validate(lost), Does.Contain("D-024"));
        }

        [TestCase(PourMode.Run, CapacityFamily.Uniform)]
        [TestCase(PourMode.Run, CapacityFamily.Mixed)]
        [TestCase(PourMode.Single, CapacityFamily.Uniform)]
        [TestCase(PourMode.Single, CapacityFamily.Mixed)]
        public void P1_P2_P3_HoldWithTwoHojasAtadasOfDifferentFruits(PourMode mode, CapacityFamily family)
        {
            // Lo que D-024 (punto 2) deja en un nivel: con un ejemplar por fruta (dos Hojas atadas de frutas distintas, que nunca
            // se reciben la una a la otra) P1-P3 se cumplen (02 R16).
            AssertWith(mode, family, new TwoHojasAtadasInjector(sameFruit: false), 0xF07E_2832UL);
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void P3_TwoHojasAtadasOfTheSameFruit_FailsOnGeneratedBoards_AndD024RejectsThem(PourMode mode)
        {
            // El contraejemplo no es un caso aislado: en tableros generados con dos Hojas atadas de la misma fruta, P3 falla
            // (y solo P3: P1 y P2 se cumplen). Por eso D-024 (punto 2) las prohíbe: LevelValidation rechaza todos los tableros
            // de ese generador, con la misma semilla.
            Report report = Run(mode, CapacityFamily.Uniform, new TwoHojasAtadasInjector(sameFruit: true), 0xF07E_2833UL);

            TestContext.WriteLine("dos hojas atadas de la misma fruta: " + Describe(report) + "; " + report.Violations.Count.ToString(CultureInfo.InvariantCulture) + " violaciones (máx. 10)");
            Assert.That(report.Violations, Is.Not.Empty);
            for (int v = 0; v < report.Violations.Count; v++)
            {
                Assert.That(report.Violations[v], Does.StartWith("P3 "));
            }

            var rng = new Prng(0xF07E_2833UL);
            int boards = 0;
            for (int attempt = 0; attempt < MaxBoards; attempt++)
            {
                BoardState? start = PropertyBoards.Start(rng, CapacityFamily.Uniform, new TwoHojasAtadasInjector(sameFruit: true));
                if (start == null)
                {
                    continue;
                }

                boards++;
                Assert.That(LevelValidation.Validate(start), Is.Not.Null, start.ToString());
            }

            Assert.That(boards, Is.GreaterThanOrEqualTo(report.Boards));
        }

        [Test]
        public void TwoHojasAtadasOfTheSameFruit_PassTheCapacityOnlyCount_Unsolvable_02_8AndD024RejectIt()
        {
            // El otro efecto de dos Hojas atadas de la misma fruta (revisión de K3a): contadas solo por capacidad, la segunda
            // contaba como casa de otra fruta de la misma U_c, aunque solo recibe la suya. Aquí el 1 y el 2 tienen U_c = 4 y las
            // únicas cuias de 4 son las dos Hojas atadas al 1: la cuenta por capacidad sale (2 cuias de 4 para 2 frutas de 4) y el
            // nivel no empieza atascado, pero el 2 no tiene dónde sellar, así que es irresoluble. Ahora lo rechazan las dos reglas:
            // la cuenta de 02 §8, que cuenta una sola cuia de una sola fruta por fruta (y llega antes), y D-024, punto 2, en la
            // propia mecánica. Con la segunda cambiada por una cuia de 4 sin mecánica (una Hoja atada por fruta) cada fruta tiene
            // casa y el nivel se gana.
            MechanicType hojaAtada = MechanicType.HojaAtada;
            Cuia[] smalls = { Cuia.Create(3, new[] { 1, 2, 2 }), Cuia.Create(3, new[] { 2, 1, 1 }), Cuia.Create(2, new[] { 1, 2 }) };
            var twoOfTheSameFruit = new BoardState(new[]
            {
                Cuia.Empty(4).WithMechanic(hojaAtada, key: 1), Cuia.Empty(4).WithMechanic(hojaAtada, key: 1), smalls[0], smalls[1], smalls[2],
            });
            var onePerFruit = new BoardState(new[]
            {
                Cuia.Empty(4).WithMechanic(hojaAtada, key: 1), Cuia.Empty(4), smalls[0], smalls[1], smalls[2],
            });

            Assert.That(PassesTheCapacityOnlyCount(twoOfTheSameFruit), Is.True);
            Assert.That(EveryFruitHasAHome(twoOfTheSameFruit), Is.False);
            Assert.That(PourRules.IsStuck(twoOfTheSameFruit, true), Is.False);
            Assert.That(ExhaustiveSearch.Explore(twoOfTheSameFruit, PourMode.Run, MaxStatesPerBoard).Distance[0], Is.EqualTo(int.MaxValue));
            Assert.That(ExhaustiveSearch.Explore(twoOfTheSameFruit, PourMode.Single, MaxStatesPerBoard).Distance[0], Is.EqualTo(int.MaxValue));
            Assert.That(LevelValidation.Validate(twoOfTheSameFruit), Does.StartWith("02 §8").And.Contain("una sola por fruta"));
            Assert.That(MechanicTable.Of(twoOfTheSameFruit[0]).ValidateInitial(twoOfTheSameFruit, 0), Does.Contain("D-024"));

            Assert.That(EveryFruitHasAHome(onePerFruit), Is.True);
            Assert.That(LevelValidation.Validate(onePerFruit), Is.Null);
            Assert.That(ExhaustiveSearch.Explore(onePerFruit, PourMode.Run, MaxStatesPerBoard).Distance[0], Is.Not.EqualTo(int.MaxValue));
            Assert.That(ExhaustiveSearch.Explore(onePerFruit, PourMode.Single, MaxStatesPerBoard).Distance[0], Is.Not.EqualTo(int.MaxValue));
        }

        [Test]
        public void MatapiWithDosesAndHojaAtadaOfTheSameFruit_PassTheCapacityOnlyCount_Unsolvable_02_8RejectsIt()
        {
            // Revisión de FIXK: el mismo agujero por otro camino, con un ejemplar por fruta y sin combinación prohibida (02 §9.2,
            // regla 7, no prohíbe juntar Matapi y Hoja atada). Un Matapi con dosis del 1 nunca es origen y solo recibe el 1
            // (D-007, reglas 1-3), así que es la única casa del 1 (tiene que acabar sellado, R12); la Hoja atada al 1 solo recibe
            // el 1, así que no es la casa de nadie. El 1 y el 2 tienen U_c = 4 y las únicas cuias de 4 son esas dos: la cuenta por
            // capacidad sale, el nivel no empieza atascado y es irresoluble en run y en single. La cuenta de 02 §8 de
            // LevelValidation lo rechaza: cada cuia de una sola fruta solo es casa de su fruta, y una sola por fruta (03 §3: «cada
            // color puede sellar en una cuia propia con cap = U_c»). Control: con el Matapi vacío (casa del 2) y el 1 fuera, es
            // válido y se gana.
            MechanicType hojaAtada = MechanicType.HojaAtada;
            var oneHome = new BoardState(new[]
            {
                Cuia.Create(4, new[] { 1 }).WithMechanic(MechanicType.Matapi), Cuia.Empty(4).WithMechanic(hojaAtada, key: 1),
                Cuia.Create(3, new[] { 1, 2, 2 }), Cuia.Create(3, new[] { 2, 1, 2 }), Cuia.Create(2, new[] { 1 }),
            });
            var control = new BoardState(new[]
            {
                Cuia.Empty(4).WithMechanic(MechanicType.Matapi), Cuia.Empty(4).WithMechanic(hojaAtada, key: 1),
                Cuia.Create(3, new[] { 1, 2, 2 }), Cuia.Create(3, new[] { 2, 1, 2 }), Cuia.Create(2, new[] { 1, 1 }),
            });

            Assert.That(PassesTheCapacityOnlyCount(oneHome), Is.True);
            Assert.That(EveryFruitHasAHome(oneHome), Is.False);
            Assert.That(PourRules.IsStuck(oneHome, true), Is.False);
            Assert.That(ExhaustiveSearch.Explore(oneHome, PourMode.Run, MaxStatesPerBoard).Distance[0], Is.EqualTo(int.MaxValue));
            Assert.That(ExhaustiveSearch.Explore(oneHome, PourMode.Single, MaxStatesPerBoard).Distance[0], Is.EqualTo(int.MaxValue));
            Assert.That(MechanicTable.Of(oneHome[0]).ValidateInitial(oneHome, 0), Is.Null);
            Assert.That(MechanicTable.Of(oneHome[1]).ValidateInitial(oneHome, 1), Is.Null);
            Assert.That(LevelValidation.Validate(oneHome), Does.StartWith("02 §8").And.Contain("una sola por fruta"));

            Assert.That(EveryFruitHasAHome(control), Is.True);
            Assert.That(LevelValidation.Validate(control), Is.Null);
            Assert.That(ExhaustiveSearch.Explore(control, PourMode.Run, MaxStatesPerBoard).Distance[0], Is.Not.EqualTo(int.MaxValue));
            Assert.That(ExhaustiveSearch.Explore(control, PourMode.Single, MaxStatesPerBoard).Distance[0], Is.Not.EqualTo(int.MaxValue));
        }

        [TestCase(CapacityFamily.Uniform)]
        [TestCase(CapacityFamily.Mixed)]
        public void SingleFruitCuias_TheCountOf02_8IsExact_EveryValidBoardHasAHomeForEachFruit(CapacityFamily family)
        {
            // 02 §8 y 03 §3 con las cuias de una sola fruta: la Hoja atada (D-024: capacidad = U_c de su llave y una por fruta) y
            // el Matapi con dosis (capacidad = U_c de su color [P]). Cada una solo es casa de su fruta, y una sola por fruta. La
            // cuenta de LevelValidation equivale a que cada fruta tenga una casa propia donde sellar: se casa cada fruta con su
            // cuia de una sola fruta y las demás frutas de esa U_c con las cuias que admiten cualquier fruta. Se comprueba con el
            // reparto exacto (EveryFruitHasAHome) en tableros generados con una Hoja atada, con dos de frutas distintas, con dos de
            // la misma fruta, con un Matapi y con un Matapi y una Hoja atada (a veces de la misma fruta): todo tablero que
            // LevelValidation acepta tiene casa para cada fruta. Contadas solo por capacidad, las Hojas atadas (una por fruta) y
            // el Matapi solo ya eran exactas; con un Matapi con dosis y una Hoja atada de la misma fruta, no.
            IMechanicInjector[] injectors =
            {
                new HojaAtadaInjector(), new TwoHojasAtadasInjector(sameFruit: false), new TwoHojasAtadasInjector(sameFruit: true),
                new MatapiInjector(), new MatapiAndHojaAtadaInjector(),
            };
            int matapiAndAtadaGaps = 0;
            for (int k = 0; k < injectors.Length; k++)
            {
                var rng = new Prng(0xF07E_0824UL + (ulong)k);
                int accepted = 0;
                int countWithoutHome = 0;
                for (int attempt = 0; attempt < MaxBoards; attempt++)
                {
                    BoardState? start = PropertyBoards.Start(rng, family, injectors[k]);
                    if (start == null)
                    {
                        continue;
                    }

                    if (PassesTheCapacityOnlyCount(start) && !EveryFruitHasAHome(start))
                    {
                        countWithoutHome++;
                    }

                    if (LevelValidation.Validate(start) != null)
                    {
                        continue;
                    }

                    accepted++;
                    Assert.That(EveryFruitHasAHome(start), Is.True, injectors[k].Name + ": " + start);
                }

                TestContext.WriteLine(injectors[k].Name + " (" + family + "): " + accepted.ToString(CultureInfo.InvariantCulture)
                    + " tableros válidos; " + countWithoutHome.ToString(CultureInfo.InvariantCulture)
                    + " pasan la cuenta solo por capacidad sin casa para alguna fruta");
                if (k == 2)
                {
                    Assert.That(accepted, Is.EqualTo(0), injectors[k].Name);
                    continue;
                }

                Assert.That(accepted, Is.GreaterThan(0), injectors[k].Name);
                if (k == 4)
                {
                    matapiAndAtadaGaps = countWithoutHome;
                }
                else
                {
                    Assert.That(countWithoutHome, Is.EqualTo(0), injectors[k].Name);
                }
            }

            if (family == CapacityFamily.Mixed)
            {
                // El generador mixto llega al agujero que cierra la cuenta (los uniformes siempre tienen un vacío de 4 de más), y
                // todos esos tableros se rechazan (arriba).
                Assert.That(matapiAndAtadaGaps, Is.GreaterThan(0));
            }
        }

        private static void AssertWith(PourMode mode, CapacityFamily family, IMechanicInjector injector, ulong seed)
        {
            Report report = Run(mode, family, injector, seed);

            TestContext.WriteLine(injector.Name + ": " + Describe(report));
            Assert.That(report.Violations, Is.Empty);
            Assert.That(report.States, Is.GreaterThanOrEqualTo(MinStates));
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void R16_MixedCapacities_PrunedParVersusExhaustivePar(PourMode mode)
        {
            // Hallazgo para la integración (02 R16 y 03 §5): 03 §5 calcula el par con MOVIMIENTOS (sin fútiles) y lo da por
            // exacto. Con capacidades mixtas un fútil puede cambiar el estado canónico (test de abajo) y, aunque P1 se cumple
            // (nunca se declara Aguas estancadas con solución), podar los fútiles puede alargar el par o dejar el tablero sin
            // camino útil a la victoria: hace falta mover una fruta entre dos cuias que no son su casa (p. ej. la dosis suelta
            // de una fruta abundante de U = 5 de una Cuia chica a una cuia de 4 vacía). Se mide y se informa; la poda nunca
            // acorta el par.
            var rng = new Prng(0xF07E_9AA1UL);
            int boards = 0;
            int longer = 0;
            int unreachable = 0;
            string? example = null;
            for (int attempt = 0; attempt < 600; attempt++)
            {
                BoardState? start = PropertyBoards.Start(rng, CapacityFamily.Mixed, null);
                if (start == null)
                {
                    continue;
                }

                ExhaustiveSearch.Graph graph;
                try
                {
                    graph = ExhaustiveSearch.Explore(start, mode, MaxStatesPerBoard);
                }
                catch (InvalidOperationException)
                {
                    continue;
                }

                if (graph.Distance[0] == int.MaxValue)
                {
                    continue;
                }

                boards++;
                int pruned = ExhaustiveSearch.Par(start, mode, true);
                if (pruned < 0)
                {
                    unreachable++;
                    example ??= start + ": par " + graph.Distance[0] + ", sin camino útil con la poda";
                }
                else if (pruned != graph.Distance[0])
                {
                    Assert.That(pruned, Is.GreaterThan(graph.Distance[0]), "la poda no puede acortar el par");
                    longer++;
                    example ??= start + ": par " + graph.Distance[0] + ", con poda " + pruned;
                }
            }

            TestContext.WriteLine(mode + ": " + boards + " tableros mixtos resolubles; con la poda, par más largo en " + longer
                + " y sin camino útil en " + unreachable + (example == null ? string.Empty : " (p. ej. " + example + ")"));
            Assert.That(boards, Is.GreaterThan(100));
        }

        [Test]
        public void R16_InRun_AFutilePourBetweenDifferentCapacities_CanChangeTheCanonicalState()
        {
            // R16 dice que en run un fútil no cambia el estado canónico; con capacidades distintas no es así: A estándar
            // monocolor (cap_A = U_c, así que no es "casa correcta") → Cuia chica vacía cambia qué cuia guarda la fruta.
            // No crea progreso (P1 lo comprueba); queda como precisión del texto de R16 para la integración.
            BoardState state = new BoardState(new[] { Cuia.Create(4, new[] { 1, 1 }), Cuia.Empty(2), Cuia.Create(4, new[] { 1, 1, 2, 2 }), Cuia.Create(4, new[] { 2, 2 }) });
            Assert.That(PourRules.IsFutile(state, 0, 1), Is.True);

            BoardState after = PourRules.Apply(state, 0, 1, PourMode.Run);

            Assert.That(after.CanonicalHash(), Is.Not.EqualTo(state.CanonicalHash()));
        }

        /// <summary>
        /// Recorre tableros de la familia hasta comprobar <see cref="MinStates"/> estados (o <see cref="MaxBoards"/>
        /// tableros) y devuelve el informe. Lo usan también los tests de propiedad de cada mecánica con su inyector.
        /// </summary>
        internal static Report Run(PourMode mode, CapacityFamily family, IMechanicInjector? injector, ulong seed)
        {
            return Run(mode, seed, rng => PropertyBoards.Start(rng, family, injector));
        }

        /// <summary>
        /// Como <see cref="Run(PourMode, CapacityFamily, IMechanicInjector, ulong)"/> con otro generador de tableros iniciales
        /// (la Tinaja añade dos cuias y la máscara de ocultos: <see cref="TinajaBoards"/>).
        /// </summary>
        internal static Report Run(PourMode mode, ulong seed, Func<Prng, BoardState?> startBoard)
        {
            var report = new Report();
            var rng = new Prng(seed);
            for (int attempt = 0; attempt < MaxBoards && report.States < TargetStates; attempt++)
            {
                BoardState? start = startBoard(rng);
                if (start == null)
                {
                    continue;
                }

                ExhaustiveSearch.Graph graph;
                try
                {
                    graph = ExhaustiveSearch.Explore(start, mode, MaxStatesPerBoard);
                }
                catch (InvalidOperationException)
                {
                    continue; // tablero demasiado grande para recorrerlo entero
                }

                report.Boards++;
                Check(graph, mode, report);
            }

            return report;
        }

        /// <summary>Resumen del informe para la salida del test.</summary>
        internal static string Describe(Report report)
        {
            return report.Boards.ToString(CultureInfo.InvariantCulture) + " tableros, "
                + report.States.ToString(CultureInfo.InvariantCulture) + " estados, "
                + report.Stuck.ToString(CultureInfo.InvariantCulture) + " atascados, "
                + report.FutilePours.ToString(CultureInfo.InvariantCulture) + " fútiles, "
                + report.UsefulMonoToEmpty.ToString(CultureInfo.InvariantCulture) + " monocolor→vacía útiles, "
                + report.UsefulRoundTrips.ToString(CultureInfo.InvariantCulture) + " vaivenes útiles (con progreso)"
                + (report.RemolinoCycles == 0 ? string.Empty : ", " + report.RemolinoCycles.ToString(CultureInfo.InvariantCulture) + " vaivenes Remolino sin progreso (Q-B)")
                + (report.CucharonHung + report.CucharonReleased == 0 ? string.Empty : ", " + report.CucharonHung.ToString(CultureInfo.InvariantCulture)
                    + " con la cuia de la tinaja colgada y " + report.CucharonReleased.ToString(CultureInfo.InvariantCulture) + " con la tinaja vacía");
        }

        private static void Check(ExhaustiveSearch.Graph graph, PourMode mode, Report report)
        {
            for (int s = 0; s < graph.States.Count; s++)
            {
                BoardState state = graph.States[s];
                report.States++;
                CountTinaja(state, report);
                bool stuck = PourRules.IsStuck(state, true);
                if (stuck)
                {
                    report.Stuck++;
                    if (graph.Distance[s] != int.MaxValue)
                    {
                        Violation(report, "P1 (R14)", mode, state, "atascado con solución a " + graph.Distance[s].ToString(CultureInfo.InvariantCulture) + " vertidos");
                    }
                }

                for (int a = 0; a < state.Count; a++)
                {
                    for (int b = 0; b < state.Count; b++)
                    {
                        if (a == b || !PourRules.IsLegal(state, a, b))
                        {
                            continue;
                        }

                        bool futile = PourRules.IsFutile(state, a, b);
                        BoardState after = PourRules.Apply(state, a, b, mode);
                        if (futile)
                        {
                            report.FutilePours++;
                            bool plainPair = !state[a].HasMechanic && !state[b].HasMechanic;
                            if (mode == PourMode.Run && plainPair && state[a].Capacity == state[b].Capacity
                                && after.CanonicalHash() != state.CanonicalHash())
                            {
                                Violation(report, "P2 (R16)", mode, state, "el fútil " + a + "→" + b + " cambia el estado canónico");
                            }

                            continue;
                        }

                        // P3: A monocolor → B vacía útil y que vacía A; si volver de B a A también es útil, el vaivén no
                        // puede devolver el estado canónico de partida (un ir y venir sin progreso que esquivaría el modal).
                        if (state[b].IsEmpty && state[a].IsMonocolor && after[a].IsEmpty)
                        {
                            report.UsefulMonoToEmpty++;
                            if (PourRules.IsLegal(after, b, a) && !PourRules.IsFutile(after, b, a))
                            {
                                report.UsefulRoundTrips++;
                                BoardState back = PourRules.Apply(after, b, a, mode);
                                if (back.CanonicalHash() == state.CanonicalHash())
                                {
                                    if (state[a].Mechanic == AF.Core.Mechanics.MechanicType.Remolino)
                                    {
                                        report.RemolinoCycles++; // excepción de Q-B (ver el resumen de la clase)
                                    }
                                    else
                                    {
                                        Violation(report, "P3 (anexo B)", mode, state, a + "↔" + b + " es útil en los dos sentidos y vuelve al mismo estado");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>Cuenta los estados con tinaja: con su cuia colgada o con la tinaja ya vacía (la cuia suelta).</summary>
        private static void CountTinaja(BoardState state, Report report)
        {
            int tinaja = TinajaBoards.TinajaIndex(state);
            if (tinaja < 0)
            {
                return;
            }

            if (state[tinaja].IsEmpty)
            {
                report.CucharonReleased++;
            }
            else
            {
                report.CucharonHung++;
            }
        }

        private static void Violation(Report report, string property, PourMode mode, BoardState state, string detail)
        {
            if (report.Violations.Count < 10)
            {
                report.Violations.Add(property + " en " + mode + ": " + state + ": " + detail);
            }
        }

        /// <summary>
        /// La cuenta de capacidades de 02 §8 contada solo por capacidad, como la hacía <c>LevelValidation</c> antes de la revisión
        /// de FIXK, sin Tinaja: para cada <c>v</c>, cuias de capacidad <c>v</c> ≥ frutas con <c>U_c = v</c>. Deja pasar los
        /// tableros en los que una cuia de una sola fruta cuenta como casa de otra.
        /// </summary>
        private static bool PassesTheCapacityOnlyCount(BoardState state)
        {
            int[] units = state.UnitsTable;
            for (int v = LevelValidation.MinUnits; v <= LevelValidation.MaxUnits; v++)
            {
                int fruits = 0;
                for (int c = Cuia.MinColor; c <= Cuia.MaxTintColor; c++)
                {
                    if (units[c] == v)
                    {
                        fruits++;
                    }
                }

                int cuias = 0;
                for (int i = 0; i < state.Count; i++)
                {
                    if (state[i].Capacity == v)
                    {
                        cuias++;
                    }
                }

                if (cuias < fruits)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Reparto exacto de casas (02 §8; 03 §3) cuando las únicas mecánicas son la Hoja atada y el Matapi: cada fruta necesita
        /// una cuia propia de capacidad <c>U_c</c> que la acepte hasta sellar. La Hoja atada solo sirve a su llave; el Matapi con
        /// dosis, solo a su color (nunca es origen y solo recibe su cima); el Matapi vacío y las cuias sin mecánica, a cualquiera.
        /// Las de una sola fruta se casan primero (una por fruta, aunque haya dos de la misma) y las demás frutas de esa
        /// <c>U_c</c> van a las cuias que admiten cualquier fruta.
        /// </summary>
        private static bool EveryFruitHasAHome(BoardState state)
        {
            int[] units = state.UnitsTable;
            for (int v = LevelValidation.MinUnits; v <= LevelValidation.MaxUnits; v++)
            {
                int fruits = 0;
                int housedAlone = 0;
                for (int c = Cuia.MinColor; c <= Cuia.MaxTintColor; c++)
                {
                    if (units[c] != v)
                    {
                        continue;
                    }

                    fruits++;
                    for (int i = 0; i < state.Count; i++)
                    {
                        if (state[i].Capacity == v && OnlyFruitOf(state[i]) == c)
                        {
                            housedAlone++;
                            break;
                        }
                    }
                }

                int open = 0;
                for (int i = 0; i < state.Count; i++)
                {
                    if (state[i].Capacity == v && OnlyFruitOf(state[i]) == Cuia.NoColor)
                    {
                        open++;
                    }
                }

                if (open < fruits - housedAlone)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>La única fruta que la cuia puede recibir en toda la partida, o <c>NoColor</c> si puede recibir cualquiera.</summary>
        private static int OnlyFruitOf(Cuia cuia)
        {
            if (cuia.Mechanic == MechanicType.HojaAtada)
            {
                return cuia.MechanicKey;
            }

            return cuia.Mechanic == MechanicType.Matapi && !cuia.IsEmpty ? cuia.ColorAt(0) : Cuia.NoColor;
        }

        /// <summary>
        /// Dos Hojas atadas (<see cref="HojaAtadaInjector"/> dos veces, cada una con capacidad = <c>U_c</c> de su llave), de la
        /// misma fruta o de frutas distintas; si salen al revés de lo pedido, se genera otro tablero.
        /// </summary>
        private sealed class TwoHojasAtadasInjector : IMechanicInjector
        {
            private readonly HojaAtadaInjector _one = new HojaAtadaInjector();
            private readonly bool _sameFruit;

            public TwoHojasAtadasInjector(bool sameFruit)
            {
                _sameFruit = sameFruit;
            }

            public string Name => _sameFruit ? "dos hojas atadas de la misma fruta" : "dos hojas atadas de frutas distintas";

            public bool Inject(Prng rng, int[] caps, List<int>[] doses, MechanicSpec[] attrs)
            {
                if (!_one.Inject(rng, caps, doses, attrs) || !_one.Inject(rng, caps, doses, attrs))
                {
                    return false;
                }

                int first = -1;
                for (int i = 0; i < attrs.Length; i++)
                {
                    if (attrs[i].Type != MechanicType.HojaAtada)
                    {
                        continue;
                    }

                    if (first < 0)
                    {
                        first = i;
                        continue;
                    }

                    return (attrs[first].Key == attrs[i].Key) == _sameFruit;
                }

                return false;
            }
        }

        /// <summary>
        /// Un Matapi (<see cref="MatapiInjector"/>: vacío o con dosis de un color y capacidad = <c>U_c</c>) y una Hoja atada
        /// (<see cref="HojaAtadaInjector"/>) en otra cuia; la llave puede ser la fruta del Matapi.
        /// </summary>
        private sealed class MatapiAndHojaAtadaInjector : IMechanicInjector
        {
            private readonly MatapiInjector _matapi = new MatapiInjector();
            private readonly HojaAtadaInjector _atada = new HojaAtadaInjector();

            public string Name => "matapi y hoja atada";

            public bool Inject(Prng rng, int[] caps, List<int>[] doses, MechanicSpec[] attrs)
            {
                return _matapi.Inject(rng, caps, doses, attrs) && _atada.Inject(rng, caps, doses, attrs);
            }
        }
    }
}
