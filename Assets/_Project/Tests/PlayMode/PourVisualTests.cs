#nullable enable
using System;
using System.Collections;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La vista sigue al modelo en cada vertido con mecánicas (arquitectura del paso 1, §4, ola 1): vistas de cuia reales en
    /// un Canvas propio y una <see cref="BoardAnimator"/> a la que se encolan <see cref="PourVisual"/> de tableros
    /// sintéticos (<see cref="BoardBuilder"/> y <see cref="PourRules"/>), con el reloj en manos del test. Por un Remolino la
    /// cuia tocada no se llena y la pareja termina como el modelo (02 §9.1 M4); con Barro en la cima el relleno empieza en su
    /// dosis (M5); la gota de la Lluvia cae en su cuia como paso de mecánica al terminar el caudal (M6; desde T07, paso
    /// RainDrop de la etapa (e)); los vertidos normales siguen encadenando (02 §3.2), también con un contador que solo cuenta
    /// el vertido (T07), y los que cambian otras cuias no; el contador de la Lluvia o de la cueva se repinta al llegar el
    /// caudal del vertido que lo causó, nunca en el toque (etapa (e) tras la transferencia), también con toques encadenados o
    /// vertidos en paralelo; y con Reducir movimiento no hay partículas (02 §4).
    /// </summary>
    public sealed class PourVisualTests
    {
        private const int FrameMs = 16;
        private static readonly PourTimings Timings = new PourTimings(100f, 300f, 280f, 80f, 300f);
        private static ProceduralArt? s_art;

        private Rig? _rig;

        private static ProceduralArt Art => s_art ??= new ProceduralArt("white");

        [TearDown]
        public void TearDown()
        {
            _rig?.Dispose();
            _rig = null;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            s_art?.Dispose();
            s_art = null;
        }

        [UnityTest]
        public IEnumerator Remolino_TouchedUnchanged_PartnerEndsAsModel()
        {
            // 0 → 1 (Remolino): la racha entra por el fondo de la pareja (2).
            BoardState before = Build(
                new[] { new[] { 1 }, new int[0], new[] { 2 }, new int[0] },
                new[] { MechanicSpec.None, new MechanicSpec(MechanicType.Remolino, partner: 2), new MechanicSpec(MechanicType.Remolino, partner: 1), MechanicSpec.None });
            BoardState after = PourRules.Apply(before, 0, 1, PourMode.Run, out PourEvents ev);
            Assert.That(ev.Receiver, Is.EqualTo(2));
            yield return StartRig(before);
            Rig rig = _rig!;

            var visual = new PourVisual(new Pour(0, 1, ev.Amount), ev, before, after);
            Assert.That(visual.ThroughRemolino, Is.True);
            rig.Animator.EnqueuePour(in visual);
            bool partnerJumped = false;
            int elapsed = rig.RunUntilIdle(3000, t =>
            {
                Assert.That(rig.Animator.ShownOf(1), Is.EqualTo(before[1]), "la cuia tocada no cambia (t = " + t + ")");
                Assert.That(rig.Views[1].LiquidLevel, Is.EqualTo(0f).Within(1e-4f), "la cuia tocada no se llena (t = " + t + ")");
                partnerJumped |= !rig.Animator.IsIdle && rig.Animator.ShownOf(2) == after[2];
            });

            Assert.That(rig.Animator.IsIdle, Is.True, "quieta en " + elapsed + " ms");
            Assert.That(partnerJumped, Is.True, "la pareja salta al llegar el caudal, antes de que vuelva el origen");
            AssertShowsModel(rig, after);
            Assert.That(rig.Views[2].LiquidLevel, Is.EqualTo(2f).Within(1e-4f));
            Assert.That(rig.Views[2].DoseColor(0), Is.EqualTo(Palette.Tint(1)), "lo vertido entra por el fondo");
            Assert.That(rig.Animator.ChainedCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator MudExposed_FillStartsOnMud()
        {
            // 0 → 1 con Barro en la cima de 1: lava el barro y las 2 dosis ocupan su lugar (espacio + 1; 02 §9.1 M5).
            BoardState before = Build(new[] { new[] { 2, 2 }, new[] { 1, Cuia.MudColor }, new int[0] }, null);
            BoardState after = PourRules.Apply(before, 0, 1, PourMode.Run, out PourEvents ev);
            Assert.That(ev.BarroWashedMask, Is.EqualTo(1 << 1));
            Assert.That(after[1].Count, Is.EqualTo(3));
            yield return StartRig(before);
            Rig rig = _rig!;
            Color mud = rig.Views[1].DoseColor(1);
            Color poured = Palette.Tint(2);
            Assert.That(mud, Is.Not.EqualTo(poured));

            rig.Animator.EnqueuePour(new PourVisual(new Pour(0, 1, ev.Amount), ev, before, after));

            // Antes de que llegue el chorro (210 ms + 60 de caída) el barro sigue a la vista.
            rig.Run(240);
            Assert.That(rig.Views[1].DoseColor(1), Is.EqualTo(mud), "el barro se ve hasta que llega el chorro");

            // Con el chorro llegando, la dosis del barro (Count − 1) se llena del color vertido.
            rig.Run(160);
            Assert.That(rig.Views[1].DoseColor(1), Is.EqualTo(poured), "la dosis 15 se sustituye por el color vertido");
            Assert.That(rig.Views[1].LiquidLevel, Is.LessThan(3f), "aún llenándose");
            Assert.That(rig.Views[1].LiquidLevel, Is.GreaterThan(1f), "sobre la dosis de abajo");

            rig.RunUntilIdle(3000, null);
            Assert.That(rig.Animator.IsIdle, Is.True);
            AssertShowsModel(rig, after);
            Assert.That(rig.Views[1].LiquidLevel, Is.EqualTo(3f).Within(1e-4f), "nunca por encima de lo que hay");
            Assert.That(rig.Views[1].DoseColor(2), Is.EqualTo(poured));
        }

        [UnityTest]
        public IEnumerator LluviaDrop_OtherCuiaEndsAsModel()
        {
            // Lluvia en la 4 (agua 10, 3 gotas, k = 2): el primer vertido útil (fuera de la vista) cuenta y el segundo, 2 → 0,
            // deja caer una gota en ella.
            BoardState start = Build(
                new[] { new[] { 1, 2 }, new[] { 2 }, new[] { 3, 1 }, new int[0], new[] { 10 } },
                new[] { MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, new MechanicSpec(MechanicType.Lluvia, key: 10, n: 3, k: 2) });
            BoardState before = PourRules.Apply(start, 0, 1, PourMode.Run);
            BoardState after = PourRules.Apply(before, 2, 0, PourMode.Run, out PourEvents ev);
            Assert.That(ev.LluviaDropMask, Is.EqualTo(1 << 4));
            yield return StartRig(before);
            Rig rig = _rig!;

            rig.Animator.EnqueuePour(new PourVisual(new Pour(2, 0, ev.Amount), ev, before, after));

            // Caudal: 210 ms + 1 dosis de 280 y 60 de caída = 550 ms; hasta entonces la Lluvia sigue como estaba.
            rig.Run(528);
            Assert.That(rig.Animator.ShownOf(4), Is.EqualTo(before[4]));
            Assert.That(rig.Views[4].LiquidLevel, Is.EqualTo(1f).Within(1e-4f));

            // Al terminar el caudal empieza la gota (paso RainDrop, 280 ms; T07): sube como una dosis.
            rig.Run(48);
            Assert.That(rig.Animator.Steps.IsRunning(AF.Presentation.Mechanics.MechanicStepKind.RainDrop, 4), Is.True, "la gota cae al llegar el caudal");
            Assert.That(rig.Views[4].LiquidLevel, Is.InRange(1f, 2f), "la gota a medio caer");

            // La gota la anima el overlay de la Lluvia (desde T17, 500 ms [P]; con el paso por defecto de T07 eran 280): al
            // terminar su paso, la Lluvia queda en su estado final.
            for (int t = 0; t < 2000 && rig.Animator.Steps.IsRunning(AF.Presentation.Mechanics.MechanicStepKind.RainDrop, 4); t += FrameMs)
            {
                rig.Run(FrameMs);
            }

            Assert.That(rig.Animator.Steps.IsRunning(AF.Presentation.Mechanics.MechanicStepKind.RainDrop, 4), Is.False, "la gota terminó");
            Assert.That(rig.Animator.ShownOf(4), Is.EqualTo(after[4]), "la Lluvia termina en su estado final");

            rig.RunUntilIdle(3000, null);
            Assert.That(rig.Animator.IsIdle, Is.True);
            AssertShowsModel(rig, after);
            Assert.That(rig.Views[4].LiquidLevel, Is.EqualTo(2f).Within(1e-4f));
            Assert.That(rig.Views[4].DoseColor(1), Is.EqualTo(Palette.Tint(10)), "la gota del tinte agua");
        }

        [UnityTest]
        public IEnumerator NormalPours_StillChain()
        {
            // single (pruebas): tres 0 → 1 de una dosis seguidos se encadenan en una sola inclinación, sin saltos (02 §3.2).
            BoardState s0 = Build(new[] { new[] { 2, 1, 1, 1 }, new[] { 1 }, new int[0] }, null);
            yield return StartRig(s0);
            Rig rig = _rig!;
            BoardState s3 = EnqueueThreeSingles(rig, s0);
            Assert.That(rig.Animator.ChainedCount, Is.EqualTo(2), "los tres van en un trabajo");
            Assert.That(rig.Animator.PendingCount, Is.EqualTo(1));
            rig.RunUntilIdle(4000, null);
            Assert.That(rig.Animator.IsIdle, Is.True);
            Assert.That(rig.Animator.LagSnapCount, Is.EqualTo(0), "sin saltos");
            AssertShowsModel(rig, s3);

            // Con una Lluvia, cada vertido útil solo cambia su contador: no retiene la cuia ni impide el encadenado (T07). El
            // contador no se pinta en el toque (la etapa (e) de R10 va tras la transferencia): antes de que llegue el caudal
            // encadenado (210 ms + 3 dosis de 280 y 60 de caída = 1.110 ms) se ve el anterior y desde entonces, el nuevo.
            rig.Dispose();
            BoardState r0 = Build(
                new[] { new[] { 2, 1, 1, 1 }, new[] { 1 }, new int[0], new[] { 10 } },
                new[] { MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, new MechanicSpec(MechanicType.Lluvia, key: 10, n: 3, k: 5) });
            yield return StartRig(r0);
            rig = _rig!;
            BoardState r3 = EnqueueThreeSingles(rig, r0);
            Assert.That(rig.Animator.ChainedCount, Is.EqualTo(2), "un cambio solo de contador no impide el encadenado");
            Assert.That(r3[3].MechanicCounter, Is.EqualTo(r0[3].MechanicCounter + 3), "el contador avanzó en el modelo");
            AssertCounterShown(rig, 3, r0[3], "en el toque, el contador anterior");
            float arrival = FlowArrivalMs(3);
            Rig chained = rig;
            rig.RunUntilIdle(6000, t => AssertCounterShown(chained, 3, t < arrival ? r0[3] : r3[3], (t < arrival ? "antes" : "después") + " del caudal (" + t + " ms)"));
            Assert.That(rig.Animator.IsIdle, Is.True);
            AssertShowsModel(rig, r3);
        }

        [UnityTest]
        public IEnumerator Counter_RepaintedWhenTheFlowArrives_NotOnTap()
        {
            // Lluvia (143+) y cueva de la Piraña (183+) con k = 5: un vertido útil solo cambia su contador. La etapa (e) de R10
            // va tras la transferencia: el contador no se ve en el toque sino al llegar el caudal (210 ms + 1 dosis de 280 y
            // 60 de caída = 550 ms), con ese valor en la vista (Show del overlay), y no retiene la cuia: la cola queda quieta
            // cuando vuelve el origen, como en un vertido normal.
            MechanicSpec[] specs = { new MechanicSpec(MechanicType.Lluvia, key: 10, n: 3, k: 5), new MechanicSpec(MechanicType.Pirana, k: 5) };
            for (int s = 0; s < specs.Length; s++)
            {
                MechanicSpec spec = specs[s];
                string name = spec.Type.ToString();
                BoardState before = Build(
                    new[] { new[] { 2, 1 }, new[] { 1 }, new int[0], spec.Type == MechanicType.Lluvia ? new[] { 10 } : new int[0] },
                    new[] { MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, spec });
                BoardState after = PourRules.Apply(before, 0, 1, PourMode.Run, out PourEvents ev);
                Assert.That(after[3].MechanicCounter, Is.EqualTo(before[3].MechanicCounter + 1), name + ": el vertido útil cuenta");
                Assert.That(after[3].Count, Is.EqualTo(before[3].Count), name + ": solo cambia el contador");
                yield return StartRig(before);
                Rig rig = _rig!;

                rig.Animator.EnqueuePour(new PourVisual(new Pour(0, 1, ev.Amount), ev, before, after));
                AssertCounterShown(rig, 3, before[3], name + " en el toque");
                float arrival = FlowArrivalMs(1);
                int elapsed = rig.RunUntilIdle(3000, t => AssertCounterShown(rig, 3, t < arrival ? before[3] : after[3], name + " a los " + t + " ms"));
                Assert.That(rig.Animator.IsIdle, Is.True, name);
                Assert.That(elapsed, Is.LessThanOrEqualTo(BoardAnimator.PourDurationMs(Timings, 1) + FrameMs), name + ": el contador no alarga el vertido");
                AssertShowsModel(rig, after);
            }
        }

        [UnityTest]
        public IEnumerator Counter_ThreeChainedTaps_RepaintedWhenTheChainedFlowArrives()
        {
            // single (pruebas): tres toques 0 → 1 de una dosis, el 2.º y el 3.º con el chorro del 1.º aún saliendo (96 y 192
            // ms), se suman al mismo trabajo y cada uno cuenta en la Lluvia (k = 5). Su caudal se alarga hasta 210 ms + 3 dosis
            // de 280 y 60 de caída = 1.110 ms: hasta entonces se ve el contador anterior (nunca +1, +2 ni +3 antes de que se
            // mueva el agua) y desde entonces, los tres.
            BoardState start = Build(
                new[] { new[] { 2, 1, 1, 1 }, new[] { 1 }, new int[0], new[] { 10 } },
                new[] { MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, new MechanicSpec(MechanicType.Lluvia, key: 10, n: 3, k: 5) });
            yield return StartRig(start);
            Rig rig = _rig!;
            int[] tapAt = { 0, 96, 192 };
            float arrival = FlowArrivalMs(3);
            BoardState state = start;
            int taps = 0;
            int t = 0;
            while (taps < tapAt.Length || !rig.Animator.IsIdle)
            {
                Assert.That(t, Is.LessThan(6000), "la vista no se quedó quieta");
                if (taps < tapAt.Length && t == tapAt[taps])
                {
                    BoardState next = PourRules.Apply(state, 0, 1, PourMode.Single, out PourEvents ev);
                    rig.Animator.EnqueuePour(new PourVisual(new Pour(0, 1, ev.Amount), ev, state, next));
                    state = next;
                    taps++;
                    AssertCounterShown(rig, 3, start[3], "toque " + taps + " (" + t + " ms)");
                }

                rig.Tick(FrameMs);
                t += FrameMs;
                AssertCounterShown(rig, 3, t < arrival ? start[3] : state[3], (t < arrival ? "antes" : "después") + " del caudal (" + t + " ms)");
            }

            Assert.That(rig.Animator.ChainedCount, Is.EqualTo(2), "los tres toques van en un trabajo");
            Assert.That(rig.Animator.LagSnapCount, Is.EqualTo(0), "sin saltos");
            Assert.That(state[3].MechanicCounter, Is.EqualTo(start[3].MechanicCounter + 3), "los tres cuentan");
            Assert.That(t, Is.GreaterThanOrEqualTo(arrival), "el contador se vio después del caudal");
            AssertShowsModel(rig, state);
        }

        [UnityTest]
        public IEnumerator Counter_TwoPoursOnTheSameLluvia_EachAtItsOwnFlow()
        {
            // Dos vertidos útiles con cuias distintas (0 → 1 y, 160 ms después, 2 → 3) se animan a la vez y cuentan en la misma
            // Lluvia (4; k = 5). Cada uno suma su punto al llegar su caudal (550 ms tras su toque): +1 a los 550 ms y +2 a los
            // 710, no los dos con el primero.
            BoardState s0 = Build(
                new[] { new[] { 2, 1 }, new[] { 1 }, new[] { 1, 2 }, new[] { 2 }, new[] { 10 } },
                new[] { MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, new MechanicSpec(MechanicType.Lluvia, key: 10, n: 3, k: 5) });
            BoardState s1 = PourRules.Apply(s0, 0, 1, PourMode.Run, out PourEvents first);
            BoardState s2 = PourRules.Apply(s1, 2, 3, PourMode.Run, out PourEvents second);
            Assert.That(s1[4].MechanicCounter, Is.EqualTo(s0[4].MechanicCounter + 1), "el primero cuenta");
            Assert.That(s2[4].MechanicCounter, Is.EqualTo(s0[4].MechanicCounter + 2), "el segundo cuenta");
            yield return StartRig(s0);
            Rig rig = _rig!;

            const int SecondAt = 160;
            float firstArrival = FlowArrivalMs(first.Amount);
            float secondArrival = SecondAt + FlowArrivalMs(second.Amount);
            rig.Animator.EnqueuePour(new PourVisual(new Pour(0, 1, first.Amount), first, s0, s1));
            int t = 0;
            while (t < SecondAt)
            {
                rig.Tick(FrameMs);
                t += FrameMs;
                AssertCounterShown(rig, 4, s0[4], "antes de los dos caudales (" + t + " ms)");
            }

            rig.Animator.EnqueuePour(new PourVisual(new Pour(2, 3, second.Amount), second, s1, s2));
            Assert.That(rig.Animator.ChainedCount, Is.EqualTo(0));
            AssertCounterShown(rig, 4, s0[4], "en el segundo toque");
            int frames = rig.RunUntilIdle(3000, ms =>
            {
                int now = SecondAt + ms;
                Cuia expected = now < firstArrival ? s0[4] : now < secondArrival ? s1[4] : s2[4];
                AssertCounterShown(rig, 4, expected, now + " ms");
            });
            Assert.That(SecondAt + frames, Is.GreaterThanOrEqualTo(secondArrival));
            AssertShowsModel(rig, s2);
        }

        [UnityTest]
        public IEnumerator Counter_LaterPourArrivingFirst_DoesNotGetAhead()
        {
            // 0 → 1 lleva 2 dosis (su caudal llega a los 830 ms) y 2 → 3, tocado a los 96 ms, 1 dosis (llega a los 646): los
            // dos cuentan en la Lluvia (4). El segundo llega antes, pero en el modelo va después del primero: el contador no se
            // adelanta al agua del primero y muestra los dos cuando llega el primero.
            BoardState s0 = Build(
                new[] { new[] { 2, 1, 1 }, new[] { 1 }, new[] { 1, 2 }, new[] { 2 }, new[] { 10 } },
                new[] { MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, new MechanicSpec(MechanicType.Lluvia, key: 10, n: 3, k: 5) });
            BoardState s1 = PourRules.Apply(s0, 0, 1, PourMode.Run, out PourEvents first);
            BoardState s2 = PourRules.Apply(s1, 2, 3, PourMode.Run, out PourEvents second);
            Assert.That(first.Amount, Is.EqualTo(2));
            Assert.That(second.Amount, Is.EqualTo(1));
            Assert.That(s2[4].MechanicCounter, Is.EqualTo(s0[4].MechanicCounter + 2), "los dos cuentan");
            yield return StartRig(s0);
            Rig rig = _rig!;

            const int SecondAt = 96;
            float firstArrival = FlowArrivalMs(first.Amount);
            float secondArrival = SecondAt + FlowArrivalMs(second.Amount);
            Assert.That(secondArrival, Is.LessThan(firstArrival), "el segundo llega antes");
            rig.Animator.EnqueuePour(new PourVisual(new Pour(0, 1, first.Amount), first, s0, s1));
            rig.Run(SecondAt);
            rig.Animator.EnqueuePour(new PourVisual(new Pour(2, 3, second.Amount), second, s1, s2));
            bool secondArrived = false;
            int frames = rig.RunUntilIdle(3000, ms =>
            {
                int now = SecondAt + ms;
                secondArrived |= now >= secondArrival && now < firstArrival;
                AssertCounterShown(rig, 4, now < firstArrival ? s0[4] : s2[4], now + " ms");
            });
            Assert.That(secondArrived, Is.True, "se comprobó el tramo entre los dos caudales");
            Assert.That(SecondAt + frames, Is.GreaterThanOrEqualTo(firstArrival));
            AssertShowsModel(rig, s2);
        }

        [UnityTest]
        public IEnumerator ReduceMotion_NoParticles()
        {
            // 0 → 1 sella la 1: salpicadura por dosis y gotas del sellado (guía 6.6 y 6.8), salvo con Reducir movimiento.
            BoardState before = Build(new[] { new[] { 1 }, new[] { 1, 1, 1 }, new int[0] }, null);
            BoardState after = PourRules.Apply(before, 0, 1, PourMode.Run, out PourEvents ev);
            Assert.That(after.IsSealed(1), Is.True);

            int peak = 0;
            yield return StartRig(before);
            Rig rig = _rig!;
            rig.Fx.ReduceMotion = true;
            rig.Animator.ReduceMotion = true;
            rig.Animator.EnqueuePour(new PourVisual(new Pour(0, 1, ev.Amount), ev, before, after));
            rig.RunUntilIdle(3000, _ => peak = Mathf.Max(peak, rig.Fx.ActiveCount));
            Assert.That(peak, Is.EqualTo(0), "sin partículas con Reducir movimiento");
            AssertShowsModel(rig, after);

            rig.Fx.Splash(new Vector2(100f, -300f), Color.white, 20f);
            rig.Fx.SealDrops(new Vector2(100f, -300f), Color.white, 20f);
            rig.Tick(FrameMs);
            Assert.That(rig.Fx.ActiveCount, Is.EqualTo(0), "Splash y SealDrops no emiten");

            // Sin Reducir movimiento, el mismo vertido sí emite (el camino existe).
            rig.Dispose();
            yield return StartRig(before);
            rig = _rig!;
            peak = 0;
            rig.Animator.EnqueuePour(new PourVisual(new Pour(0, 1, ev.Amount), ev, before, after));
            rig.RunUntilIdle(3000, _ => peak = Mathf.Max(peak, rig.Fx.ActiveCount));
            Assert.That(peak, Is.GreaterThan(0));
        }

        // Encola tres vertidos single 0 → 1 desde s0 sin avanzar el reloj y devuelve el tablero final del modelo.
        private static BoardState EnqueueThreeSingles(Rig rig, BoardState s0)
        {
            BoardState state = s0;
            for (int i = 0; i < 3; i++)
            {
                BoardState next = PourRules.Apply(state, 0, 1, PourMode.Single, out PourEvents ev);
                rig.Animator.EnqueuePour(new PourVisual(new Pour(0, 1, ev.Amount), ev, state, next));
                state = next;
            }

            return state;
        }

        // Del toque a la llegada del caudal de un trabajo de doses dosis que empieza con el reloj: 210 ms al 70 % del
        // traslado, una dosis cada 280 ms y 60 ms de caída del chorro (PourVisualTests.Timings).
        private static float FlowArrivalMs(int doses)
        {
            return (Timings.TravelMs * BoardAnimator.StreamStartFraction) + (doses * Timings.DoseMs) + BoardAnimator.StreamGrowMs;
        }

        // La cola y la vista muestran el contador (y el resto) de expected en la cuia index: lo último pintado y lo que recibió
        // el overlay con Show.
        private static void AssertCounterShown(Rig rig, int index, Cuia expected, string when)
        {
            Assert.That(rig.Animator.ShownOf(index).MechanicCounter, Is.EqualTo(expected.MechanicCounter), "contador pintado " + when);
            Assert.That(rig.Animator.ShownOf(index), Is.EqualTo(expected), "cuia " + index + " " + when);
            Assert.That(rig.Views[index].ShownCuia, Is.EqualTo(expected), "vista de la cuia " + index + " " + when);
        }

        private static void AssertShowsModel(Rig rig, BoardState model)
        {
            for (int i = 0; i < model.Count; i++)
            {
                Assert.That(rig.Animator.ShownOf(i), Is.EqualTo(model[i]), "cuia " + i);
                Assert.That(rig.Views[i].LiquidLevel, Is.EqualTo((float)model[i].Count).Within(1e-4f), "nivel de la cuia " + i);
                Assert.That(rig.Views[i].Sealed, Is.EqualTo(model.IsSealed(i)), "tapa de la cuia " + i);
            }
        }

        private IEnumerator StartRig(BoardState board)
        {
            _rig?.Dispose();
            _rig = new Rig(Art, board);
            yield return null; // un fotograma para que el Canvas se construya
        }

        // Tablero de capacidad 4 (sin LevelValidation: solo hacen falta las reglas del núcleo).
        private static BoardState Build(int[][] cuias, MechanicSpec[]? attrs)
        {
            var caps = new int[cuias.Length];
            for (int i = 0; i < caps.Length; i++)
            {
                caps[i] = 4;
            }

            BoardBuildResult result = BoardBuilder.Build(caps, cuias, attrs);
            Assert.That(result.Error, Is.Null);
            return result.State!;
        }

        /// <summary>Canvas con el tablero, las capas, los efectos y una vista por cuia, como GameScreen.</summary>
        private sealed class Rig : IDisposable
        {
            private readonly GameObject _root;
            private bool _disposed;

            public Rig(ProceduralArt art, BoardState board)
            {
                _root = new GameObject("PourVisualRig", typeof(RectTransform), typeof(Canvas));
                var canvas = _root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2
                    | AdditionalCanvasShaderChannels.TexCoord3;
                var rootRt = (RectTransform)_root.transform;
                RectTransform boardRt = NewLayer("Board", rootRt);
                RectTransform back = NewLayer("MechanicBack", boardRt);
                Fx = new FxLayer(boardRt, art);
                Views = new CuiaView[board.Count];
                for (int i = 0; i < board.Count; i++)
                {
                    Views[i] = CuiaView.Create(boardRt, i, board[i].Capacity, art, index => { }, (index, inside) => { }, Fx);
                    Views[i].SetGeometry(new Vector2(36f + (i * 64f), -600f), 40f, 60f);
                    Views[i].SetLiftDuration(100f);
                }

                RectTransform front = NewLayer("MechanicFront", boardRt);
                RectTransform streams = NewLayer("Streams", boardRt);
                Fx.Root.SetAsLastSibling();
                RectTransform pourFront = NewLayer("PourFront", rootRt);
                Animator = new BoardAnimator(streams, pourFront) { Fx = Fx, BoardWidth = 360f };
                Animator.Bind(Views, Timings, 2, back, front);
                for (int i = 0; i < board.Count; i++)
                {
                    Animator.ShowContent(i, board[i], board.IsSealed(i));
                }
            }

            public BoardAnimator Animator { get; }

            public CuiaView[] Views { get; }

            public FxLayer Fx { get; }

            public void Tick(float dtMs)
            {
                Animator.Tick(dtMs);
                for (int i = 0; i < Views.Length; i++)
                {
                    Views[i].Tick(dtMs);
                }

                Fx.Tick(dtMs);
            }

            // Avanza ms milisegundos en fotogramas de 16 ms.
            public void Run(int ms)
            {
                for (int t = 0; t < ms; t += FrameMs)
                {
                    Tick(FrameMs);
                }
            }

            // Avanza hasta que la cola queda quieta (como mucho maxMs) y devuelve el tiempo; each recibe el tiempo de cada
            // fotograma.
            public int RunUntilIdle(int maxMs, Action<int>? each)
            {
                int t = 0;
                while (!Animator.IsIdle && t < maxMs)
                {
                    Tick(FrameMs);
                    t += FrameMs;
                    each?.Invoke(t);
                }

                return t;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                Object.Destroy(_root);
            }

            private static RectTransform NewLayer(string name, RectTransform parent)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return rt;
            }
        }
    }
}
