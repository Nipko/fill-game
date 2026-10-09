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
    /// cae y después deja ir su fruta a la «casa correcta» de 4 (02 §1.4).</description></item>
    /// </list>
    /// Cada mecánica añade su inyector (<see cref="IMechanicInjector"/>) antes de su debut (02 R16). Solo dotnet: lotes
    /// grandes.
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
        public void P1_P2_P3_HoldWithMatapi(PourMode mode, CapacityFamily family)
        {
            // C1-8: 02 R16 antes del debut del Matapi (63).
            AssertWith(mode, family, new MatapiInjector(), 0xF07E_0631UL);
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
            var report = new Report();
            var rng = new Prng(seed);
            for (int attempt = 0; attempt < MaxBoards && report.States < TargetStates; attempt++)
            {
                BoardState? start = PropertyBoards.Start(rng, family, injector);
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
                + report.UsefulRoundTrips.ToString(CultureInfo.InvariantCulture) + " vaivenes útiles (con progreso)";
        }

        private static void Check(ExhaustiveSearch.Graph graph, PourMode mode, Report report)
        {
            for (int s = 0; s < graph.States.Count; s++)
            {
                BoardState state = graph.States[s];
                report.States++;
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
                                    Violation(report, "P3 (anexo B)", mode, state, a + "↔" + b + " es útil en los dos sentidos y vuelve al mismo estado");
                                }
                            }
                        }
                    }
                }
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
