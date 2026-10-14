#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Rules;
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
    /// <see cref="P3_Remolino_QB_AUsefulRoundTripCanReturnToTheSameCanonicalState"/>.</description></item>
    /// </list>
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
            // (02 §1.4, anexo B) y sin exención como origen.
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
            // K3a: por qué la Hoja atada es la casa de su fruta ([P] de HojaAtadaMechanic; 02 R16). Con la exención de destino de
            // futil (02 §1.4, anexo B), una Cuia chica de 3 atada al 1 (U_1 = 4) rompe P3: [1, 1, 1] en su casa de 4 → la hoja
            // atada es útil (cambia al recibir) y volver es útil (va a la casa correcta), y el tablero vuelve al mismo estado
            // canónico. Con la regla (capacidad = U_c de su llave), P1-P3 se cumplen (P1_P2_P3_HoldWithHojaAtada) y
            // LevelValidation rechaza este tablero.
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
    }
}
