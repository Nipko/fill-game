#nullable enable
using System.Collections;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Platform;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Pasos de mecánica en el orden de R10 con el punto fijo f′ (arquitectura del paso 1, §4; T07): en los estrenos reales
    /// (43, 63, 83, 100, 103, 123, 143, 163 y 183) el vertido que activa la mecánica genera sus pasos en el orden del plan,
    /// despachados a la cuia dueña de la mecánica, con los estados intermedios de <see cref="StepStaging"/> y dentro del
    /// presupuesto desde el toque (2.800 ms); al quedar quieta, la vista muestra el modelo. Los vertidos se buscan con
    /// <see cref="MechanicTestKit"/> (solver y búsqueda limitada) y el reloj lo lleva el test.
    /// </summary>
    public sealed class MechanicStepsTests
    {
        private static ProceduralArt? s_art;
        private MechanicTestKit.Rig? _rig;

        private static ProceduralArt Art => s_art ??= new ProceduralArt("white");

        [TearDown]
        public void TearDown()
        {
            _rig?.Dispose();
            _rig = null;
            LaunchOptions.Reset();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            s_art?.Dispose();
            s_art = null;
        }

        [UnityTest]
        public IEnumerator Hoja43_OpenStep_MidStepLeafVisible()
        {
            yield return MechanicTestKit.LoadPlaying(43);
            GameBootstrap boot = GameBootstrap.Current!;
            int hoja = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Hoja);
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.HojaOpenedMask & (1 << hoja)) != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            CuiaView view = boot.View!.Views[hoja];
            PourVisual visual = MechanicTestKit.TapPour(boot, path[path.Length - 1], out _);
            int elapsed = StepWatching(boot, () =>
            {
                if (!MechanicTestKit.TryFindRecord(boot.View.Animator, MechanicStepKind.Open, hoja, out _))
                {
                    Assert.That(view.Overlay!.Visible, Is.True, "la hoja se ve hasta su paso Open");
                }
            });

            MechanicTestKit.AssertLogFollowsPlan(boot.View.Animator, in visual);
            MechanicStepRecord open = MechanicTestKit.Record(boot.View.Animator, MechanicStepKind.Open, hoja);
            Assert.That(open.Stage, Is.EqualTo(StepStage.B));
            Assert.That(open.Owner, Is.EqualTo(MechanicType.Hoja), "despachado al overlay de la Hoja");
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs), "del toque a la vista quieta");
            TestContext.WriteLine("Nivel " + boot.Flow!.LevelNumber + ": " + elapsed + " ms del toque a la vista quieta. " + MechanicTestKit.Describe(boot.View!.Animator));
            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(view.Overlay!.Visible, Is.False, "abierta: la hoja ya no está");

            // A mitad de un Open que dura (un overlay de prueba de 500 ms), la hoja sigue a la vista y ShowContent no la pisa.
            BoardState before = Board(
                new[] { new int[0], new[] { 3, 4, 4 }, new[] { 4, 4 }, new[] { 3, 3, 3 } },
                new[] { new MechanicSpec(MechanicType.Hoja, key: 4), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            var probe = new MechanicTestKit.ProbeOverlay(MechanicType.Hoja);
            probe.Durations[(int)MechanicStepKind.Open] = 500f;
            _rig = new MechanicTestKit.Rig(Art, before, views => views[0].UseOverlay(probe));
            _rig.Pour(before, 1, 0, out _);
            RunUntil(_rig, () => _rig.Animator.Steps.IsRunning(MechanicStepKind.Open, 0), 2000);
            _rig.Run(240);
            Assert.That(probe.IsAnimating, Is.True, "a mitad del Open");
            Assert.That(probe.Visible && probe.PieceVisible, Is.True, "a mitad de un Open, el overlay se sigue viendo");
            Assert.That(probe.Steps[0].Before.Mechanic, Is.EqualTo(MechanicType.Hoja));
            Assert.That(probe.Steps[0].After.Mechanic, Is.EqualTo(MechanicType.None));
            Assert.That(_rig.Views[0].LiquidLevel, Is.EqualTo(2f).Within(1e-3f), "con la fruta dentro");
            _rig.RunUntilIdle(3000);
            Assert.That(probe.Snaps, Is.GreaterThanOrEqualTo(1), "la cola cierra el paso con SnapStep");
            Assert.That(probe.Visible, Is.False, "el ShowContent del final del paso la oculta");
        }

        [UnityTest]
        public IEnumerator Matapi63_SealStep()
        {
            yield return MechanicTestKit.LoadPlaying(63);
            GameBootstrap boot = GameBootstrap.Current!;
            int matapi = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Matapi);
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.SealedMask & (1 << matapi)) != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            CuiaView view = boot.View!.Views[matapi];
            PourVisual visual = MechanicTestKit.TapPour(boot, path[path.Length - 1], out _);
            int elapsed = StepWatching(boot, () =>
            {
                if (!MechanicTestKit.TryFindRecord(boot.View.Animator, MechanicStepKind.Seal, matapi, out _))
                {
                    Assert.That(view.Sealed, Is.False, "el trabajo del vertido no sella el Matapi");
                }
            });

            MechanicTestKit.AssertLogFollowsPlan(boot.View.Animator, in visual);
            MechanicStepRecord seal = MechanicTestKit.Record(boot.View.Animator, MechanicStepKind.Seal, matapi);
            Assert.That(seal.Stage, Is.EqualTo(StepStage.C));
            Assert.That(seal.Owner, Is.EqualTo(MechanicType.Matapi), "al sellar el Matapi se llama a su PlayStep(Seal)");
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs));
            TestContext.WriteLine("Nivel " + boot.Flow!.LevelNumber + ": " + elapsed + " ms del toque a la vista quieta. " + MechanicTestKit.Describe(boot.View!.Animator));
            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(view.Sealed, Is.True);
        }

        [UnityTest]
        public IEnumerator Liana83_ReleaseAfterReceiverSeal()
        {
            yield return MechanicTestKit.LoadPlaying(83);
            GameBootstrap boot = GameBootstrap.Current!;
            int liana = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Liana);
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.LianaReleasedMask & (1 << liana)) != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            PourVisual visual = MechanicTestKit.TapPour(boot, path[path.Length - 1], out TapResult poured);
            int receiver = poured.Pour.Receiver;
            int key = boot.Flow!.Level.Attrs[liana].Key;
            Assert.That(boot.Flow.Session.Current[receiver].TopColor, Is.EqualTo(key), "el receptor sella el taperebá, la llave");
            int elapsed = MechanicTestKit.StepToIdle(boot, 6000);

            BoardAnimator animator = boot.View!.Animator;
            MechanicTestKit.AssertLogFollowsPlan(animator, in visual);
            MechanicStepRecord seal = MechanicTestKit.Record(animator, MechanicStepKind.Seal, receiver);
            MechanicStepRecord release = MechanicTestKit.Record(animator, MechanicStepKind.Release, liana);
            Assert.That(seal.Stage, Is.EqualTo(StepStage.C));
            Assert.That(release.Stage, Is.EqualTo(StepStage.D));
            Assert.That(release.OtherIndex, Is.EqualTo(receiver), "la Liana se suelta por la cuia sellada con su llave");
            Assert.That(release.Owner, Is.EqualTo(MechanicType.Liana));
            Assert.That(seal.EndMs, Is.LessThanOrEqualTo(release.StartMs + 0.01f), "la tapa termina antes de que se suelte la Liana. " + MechanicTestKit.Describe(animator));
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs));
            TestContext.WriteLine("Nivel " + boot.Flow!.LevelNumber + ": " + elapsed + " ms del toque a la vista quieta. " + MechanicTestKit.Describe(boot.View!.Animator));
            MechanicTestKit.AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator Murky100_RevealAfterSourceReturns()
        {
            yield return MechanicTestKit.LoadPlaying(100);
            GameBootstrap boot = GameBootstrap.Current!;
            Pour[] path = Trigger(boot, (ev, b, a) => ev.RevealedMask != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            Pour pour = path[path.Length - 1];
            CuiaView source = boot.View!.Views[pour.Source];
            BoardAnimator animator = boot.View.Animator;
            PourVisual visual = MechanicTestKit.TapPour(boot, pour, out _);
            Cuia after = boot.Flow!.Session.Current[pour.Source];
            int layer = after.Count - 1;
            float backAt = -1f;
            bool flew = false;
            int elapsed = StepWatching(boot, () =>
            {
                flew |= source.BodyInFront;
                if (flew && !source.BodyInFront && backAt < 0f)
                {
                    backAt = animator.Clock;
                }

                if (!MechanicTestKit.TryFindRecord(animator, MechanicStepKind.Reveal, pour.Source, out _))
                {
                    Assert.That(source.DoseColor(layer), Is.EqualTo(Palette.Murky), "la capa sigue turbia hasta su paso Reveal");
                }
                else if (source.Murky.IsAnimating)
                {
                    Assert.That(source.Murky.RevealingMask, Is.EqualTo(1 << layer));
                }
            });

            MechanicTestKit.AssertLogFollowsPlan(animator, in visual);
            MechanicStepRecord reveal = MechanicTestKit.Record(animator, MechanicStepKind.Reveal, pour.Source);
            Assert.That(reveal.Stage, Is.EqualTo(StepStage.A));
            Assert.That(backAt, Is.GreaterThan(0f), "el origen voló y volvió");
            Assert.That(reveal.StartMs, Is.GreaterThanOrEqualTo(backAt - PlayModeHelpers.FrameMs), "el Reveal del origen empieza después de SrcDone");
            Assert.That(reveal.EndMs - reveal.StartMs, Is.EqualTo(MurkyMarks.RevealRuleMs * reveal.SpeedFactor).Within(1f), "fundido de 300 ms (02 §4.1; 07 §4.3)");
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs));
            TestContext.WriteLine("Nivel " + boot.Flow!.LevelNumber + ": " + elapsed + " ms del toque a la vista quieta. " + MechanicTestKit.Describe(boot.View!.Animator));
            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(source.Murky.MarkVisible(layer), Is.False, "sin «?» tras revelarse");
            Assert.That(source.DoseColor(layer), Is.EqualTo(Palette.Tint(after.ColorAt(layer))), "con su tinte real");
        }

        [UnityTest]
        public IEnumerator Remolino103_ReceiveOnPartner()
        {
            yield return MechanicTestKit.LoadPlaying(103);
            GameBootstrap boot = GameBootstrap.Current!;
            Pour[] path = Trigger(boot, (ev, b, a) => ev.ThroughRemolino);
            MechanicTestKit.PlayPrefix(boot, path);
            Pour pour = path[path.Length - 1];
            PourVisual visual = MechanicTestKit.TapPour(boot, pour, out TapResult poured);
            BoardState before = visual.Before;
            int partner = poured.Pour.Receiver;
            Assert.That(partner, Is.Not.EqualTo(pour.Target));
            BoardAnimator animator = boot.View!.Animator;
            int elapsed = StepWatching(boot, () =>
            {
                if (!MechanicTestKit.TryFindRecord(animator, MechanicStepKind.Receive, partner, out _))
                {
                    Assert.That(animator.ShownOf(partner), Is.EqualTo(before[partner]), "la pareja recibe en su paso, no antes");
                }

                Assert.That(animator.ShownOf(pour.Target), Is.EqualTo(before[pour.Target]), "la cuia tocada no cambia");
            });

            MechanicTestKit.AssertLogFollowsPlan(animator, in visual);
            MechanicStepRecord receive = MechanicTestKit.Record(animator, MechanicStepKind.Receive, partner);
            Assert.That(receive.Stage, Is.EqualTo(StepStage.A));
            Assert.That(receive.OtherIndex, Is.EqualTo(pour.Target), "OtherIndex = la cuia tocada");
            Assert.That(receive.Owner, Is.EqualTo(MechanicType.Remolino), "despachado a la pareja");
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs));
            TestContext.WriteLine("Nivel " + boot.Flow!.LevelNumber + ": " + elapsed + " ms del toque a la vista quieta. " + MechanicTestKit.Describe(boot.View!.Animator));
            MechanicTestKit.AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator Barro123_MudWash()
        {
            yield return MechanicTestKit.LoadPlaying(123);
            GameBootstrap boot = GameBootstrap.Current!;
            Pour[] path = Trigger(boot, (ev, b, a) => ev.BarroWashedMask != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            Pour pour = path[path.Length - 1];
            int mudLayer = boot.Flow!.Session.Current[pour.Target].Count - 1;
            Assert.That(boot.View!.Views[pour.Target].Mud.LumpsVisible(mudLayer), Is.True, "el barro, en la cima");
            PourVisual visual = MechanicTestKit.TapPour(boot, pour, out _);
            int elapsed = MechanicTestKit.StepToIdle(boot, 6000);

            MechanicTestKit.AssertLogFollowsPlan(boot.View.Animator, in visual);
            MechanicStepRecord wash = MechanicTestKit.Record(boot.View.Animator, MechanicStepKind.MudWash, pour.Target);
            Assert.That(wash.Stage, Is.EqualTo(StepStage.A), "el receptor");
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs));
            TestContext.WriteLine("Nivel " + boot.Flow!.LevelNumber + ": " + elapsed + " ms del toque a la vista quieta. " + MechanicTestKit.Describe(boot.View!.Animator));
            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(boot.View.Views[pour.Target].Mud.LumpMask, Is.EqualTo(0), "el barro se lavó");
        }

        [UnityTest]
        public IEnumerator Lluvia143_DropAfterThreeUsefulPours()
        {
            yield return MechanicTestKit.LoadPlaying(143);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            int rain = MechanicTestKit.IndexOf(flow, MechanicType.Lluvia);
            int k = flow.Session.Current[rain].MechanicK;
            Assert.That(k, Is.EqualTo(3), "143: una gota cada 3 vertidos útiles");

            // Un vertido fútil no cuenta (Q18 B).
            Pour[] futile = Trigger(boot, (ev, b, a) => ev.Futile && ev.LluviaDropMask == 0);
            MechanicTestKit.PlayPrefix(boot, futile);
            int counter = flow.Session.Current[rain].MechanicCounter;
            TapResult f = MechanicTestKit.Tap(boot, futile[futile.Length - 1]);
            Assert.That(f.Kind, Is.EqualTo(TapKind.Poured));
            Assert.That(f.Pour.Events.Futile, Is.True);
            Assert.That(flow.Session.Current[rain].MechanicCounter, Is.EqualTo(counter), "el fútil no avanza el contador (Q18 B)");
            MechanicTestKit.StepToIdle(boot, 6000);
            Assert.That(MechanicTestKit.TryFindRecord(boot.View!.Animator, MechanicStepKind.RainDrop, rain, out _), Is.False);

            // La gota cae con el vertido útil que completa los 3. Cada camino del buscador se juega vertido a vertido: solo
            // cuentan los que el núcleo da por útiles (Events.Futile == false; un fútil del camino no cuenta, Q18 B) y en cada
            // uno se comprueba si cayó la gota. Si la Lluvia estuviera llena al tocarle, la gota espera al siguiente útil
            // (02 §9.1 M6; RainWait).
            int needed = k - counter;
            int useful = 0;
            int played = 0;
            bool done = false;
            while (!done)
            {
                Pour[]? next = MechanicTestKit.FindTrigger(flow, (ev, b, a) => !ev.Futile && !a.IsWon() && !PourRules.IsStuck(a, true));
                Assert.That(next, Is.Not.Null, "un vertido útil");
                for (int i = 0; i < next!.Length && !done; i++)
                {
                    Assert.That(played++, Is.LessThan(40), "la gota cae en un número acotado de vertidos");
                    boot.View!.Animator.Steps.ClearLog();
                    PourVisual visual = MechanicTestKit.TapPour(boot, next[i], out TapResult poured);
                    PourEvents events = poured.Pour.Events;
                    bool dropped = (events.LluviaDropMask & (1 << rain)) != 0;
                    int elapsed = MechanicTestKit.StepToIdle(boot, 6000);
                    MechanicTestKit.AssertLogFollowsPlan(boot.View.Animator, in visual);
                    if (!events.Futile)
                    {
                        useful++;
                    }

                    if (useful < needed)
                    {
                        Assert.That(dropped, Is.False, "vertido " + played + (events.Futile ? " (fútil)" : " (útil " + useful + " de " + needed + ")") + ": aún no cae");
                        continue;
                    }

                    if ((events.LluviaWaitingMask & (1 << rain)) != 0)
                    {
                        Assert.That(dropped, Is.False, "llena: la gota espera");
                        needed = useful + 1;
                        continue;
                    }

                    Assert.That(dropped, Is.True, "la gota cae con el vertido útil " + useful + " tras el fútil");
                    MechanicStepKind kind = (events.LluviaEndedMask & (1 << rain)) != 0 ? MechanicStepKind.RainEnd : MechanicStepKind.RainDrop;
                    MechanicStepRecord drop = MechanicTestKit.Record(boot.View!.Animator, kind, rain);
                    Assert.That(drop.Stage, Is.EqualTo(StepStage.E));
                    Assert.That(drop.Owner, Is.EqualTo(MechanicType.Lluvia), "despachado a la cuia de la Lluvia");
                    Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs));
                    TestContext.WriteLine("Nivel " + boot.Flow!.LevelNumber + ": " + elapsed + " ms del toque a la vista quieta (vertido " + played + ", " + useful + " útiles). " + MechanicTestKit.Describe(boot.View!.Animator));
                    MechanicTestKit.AssertViewMatchesModel(boot);
                    done = true;
                }
            }
        }

        [UnityTest]
        public IEnumerator Lluvia143_DropThenSeal_Order()
        {
            yield return MechanicTestKit.LoadPlaying(143);
            GameBootstrap boot = GameBootstrap.Current!;
            int rain = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Lluvia);
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.LluviaEndedMask & ev.SealedMask & (1 << rain)) != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            PourVisual visual = MechanicTestKit.TapPour(boot, path[path.Length - 1], out _);
            int elapsed = MechanicTestKit.StepToIdle(boot, 6000);

            BoardAnimator animator = boot.View!.Animator;
            MechanicTestKit.AssertLogFollowsPlan(animator, in visual);
            MechanicStepRecord drop = MechanicTestKit.Record(animator, MechanicStepKind.RainEnd, rain);
            MechanicStepRecord seal = MechanicTestKit.Record(animator, MechanicStepKind.Seal, rain);
            Assert.That(drop.Stage, Is.EqualTo(StepStage.E), "la última gota");
            Assert.That(seal.Stage, Is.EqualTo(StepStage.CPrime), "el sellado del punto fijo f′");
            Assert.That(seal.StartMs, Is.GreaterThanOrEqualTo(drop.EndMs - 0.01f), "la tapa cae después de la gota. " + MechanicTestKit.Describe(animator));
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs));
            TestContext.WriteLine("Nivel " + boot.Flow!.LevelNumber + ": " + elapsed + " ms del toque a la vista quieta. " + MechanicTestKit.Describe(boot.View!.Animator));
            MechanicTestKit.AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator LluviaSource_LastDropSeals_DropStaysUnderTheSeal()
        {
            // Q-B (143): la Lluvia [10, 10, 10, 1] con su última gota es el origen. 3 → 2 cuenta 1 (k = 2) y 0 → 1 vierte su 1:
            // la gota cae sobre las tres aguas (RainEnd, e) y la sella en (c′). Los dos pasos pintan el origen, así que
            // esperan a que vuelva a su ranura; mientras, el trabajo lo devuelve con tres aguas, la nube y sin tapa. Desde que
            // termina la gota hasta el final del sellado, la cuia muestra After (cuatro aguas, sin nube) con la tapa cayendo:
            // la gota nunca desaparece ni la tapa cae sobre tres dosis.
            yield return null;
            BoardState start = Board(
                new[] { new[] { 10, 10, 10, 1 }, new[] { 1, 1 }, new[] { 2, 2 }, new[] { 2 }, new[] { 1 }, new[] { 2 } },
                new[] { new MechanicSpec(MechanicType.Lluvia, key: 10, n: 1, k: 2), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            _rig = new MechanicTestKit.Rig(Art, start);
            BoardState counted = _rig.Pour(start, 3, 2, out _);
            Assert.That(counted[0].MechanicCounter, Is.EqualTo(1), "el primer vertido útil cuenta");
            _rig.RunUntilIdle(3000);
            _rig.Animator.Steps.ClearLog();

            BoardState after = _rig.Pour(counted, 0, 1, out PourVisual v);
            Assert.That(v.Events.LluviaEndedMask & v.Events.SealedMask, Is.EqualTo(1 << 0), "la última gota sella la Lluvia");
            Assert.That(after[0].Mechanic, Is.EqualTo(MechanicType.None), "sin nube");
            Assert.That(after[0].Count, Is.EqualTo(4));
            CuiaView rain = _rig.Views[0];
            MechanicStepQueue steps = _rig.Animator.Steps;
            float tapped = _rig.Animator.Clock;
            float backAt = -1f;
            bool flew = false;
            float lastLevel = -1f;
            int sealFrames = 0;
            int t = 0;
            while (!_rig.Animator.IsIdle)
            {
                Assert.That(t, Is.LessThan(6000), "la vista no se quedó quieta");
                _rig.Tick(PlayModeHelpers.FrameMs);
                t += PlayModeHelpers.FrameMs;
                flew |= rain.BodyInFront;
                if (flew && !rain.BodyInFront && backAt < 0f)
                {
                    backAt = _rig.Animator.Clock;
                }

                if (backAt < 0f)
                {
                    Assert.That(MechanicTestKit.TryFindRecord(_rig.Animator, MechanicStepKind.RainEnd, 0, out _), Is.False, "la gota no cae en el origen en vuelo");
                    continue;
                }

                bool dropDone = MechanicTestKit.TryFindRecord(_rig.Animator, MechanicStepKind.RainEnd, 0, out _) && !steps.IsRunning(MechanicStepKind.RainEnd, 0);
                if (!dropDone)
                {
                    // Del regreso al fin de la gota: sin tapa sobre tres dosis y el nivel nunca baja (la gota no se pierde).
                    Assert.That(rain.Sealed, Is.False, "sin tapa antes de la gota (" + t + " ms)");
                    Assert.That(rain.LiquidLevel, Is.GreaterThanOrEqualTo(3f - 1e-3f), "las tres aguas (" + t + " ms)");
                    Assert.That(rain.LiquidLevel, Is.GreaterThanOrEqualTo(lastLevel - 1e-3f), "la gota solo sube (" + t + " ms)");
                    lastLevel = rain.LiquidLevel;
                    continue;
                }

                // Del fin de la gota al fin del Seal (c′): After con la tapa, cuatro dosis y sin nube.
                sealFrames += steps.IsRunning(MechanicStepKind.Seal, 0) ? 1 : 0;
                Assert.That(_rig.Animator.ShownOf(0), Is.EqualTo(after[0]), "After del origen (" + t + " ms). " + MechanicTestKit.Describe(_rig.Animator));
                Assert.That(rain.Sealed, Is.True, "con la tapa (" + t + " ms)");
                Assert.That(rain.LiquidLevel, Is.EqualTo(4f).Within(1e-3f), "cuatro dosis (" + t + " ms)");
                Assert.That(rain.Overlay!.Visible, Is.False, "sin la nube de la Lluvia (" + t + " ms)");
            }

            MechanicTestKit.AssertLogFollowsPlan(_rig.Animator, in v);
            MechanicStepRecord drop = MechanicTestKit.Record(_rig.Animator, MechanicStepKind.RainEnd, 0);
            MechanicStepRecord seal = MechanicTestKit.Record(_rig.Animator, MechanicStepKind.Seal, 0);
            TestContext.WriteLine(t + " ms del toque a la vista quieta. " + MechanicTestKit.Describe(_rig.Animator));
            Assert.That(backAt, Is.GreaterThan(0f), "el origen voló y volvió");
            Assert.That(drop.StartMs, Is.GreaterThanOrEqualTo(backAt - PlayModeHelpers.FrameMs), "la gota espera a que el origen vuelva a su ranura");
            Assert.That(seal.Stage, Is.EqualTo(StepStage.CPrime));
            Assert.That(seal.StartMs, Is.GreaterThanOrEqualTo(drop.EndMs - 0.01f), "la tapa cae después de la gota");
            Assert.That(sealFrames, Is.GreaterThan(0), "se vio el sellado (c′)");
            Assert.That(_rig.Animator.Clock - tapped, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs), "del toque a la vista quieta");
            for (int i = 0; i < after.Count; i++)
            {
                Assert.That(_rig.Animator.ShownOf(i), Is.EqualTo(after[i]), "cuia " + i);
                Assert.That(_rig.Views[i].Sealed, Is.EqualTo(after.IsSealed(i)), "tapa de la cuia " + i);
            }
        }

        [UnityTest]
        public IEnumerator LluviaSource_DropThenQueuedPourIntoIt_NoGap()
        {
            // Q-B: la Lluvia [10, 1] (dos gotas, k = 2) es el origen de 0 → 2 y le cae una gota que no es la última
            // (RainDrop); el jugador ya ha encolado 1 → 0, agua sobre la gota. Ese vertido llena desde la cima del modelo
            // (FillBase = 2): mientras dura, la Lluvia muestra al menos las dos dosis de After del primero, sin hueco.
            yield return null;
            BoardState start = Board(
                new[] { new[] { 10, 1 }, new[] { 10 }, new[] { 1, 1 }, new[] { 2, 2 }, new[] { 2 }, new[] { 1 }, new[] { 2 } },
                new[] { new MechanicSpec(MechanicType.Lluvia, key: 10, n: 2, k: 2), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            _rig = new MechanicTestKit.Rig(Art, start);
            BoardState counted = _rig.Pour(start, 4, 3, out _);
            Assert.That(counted[0].MechanicCounter, Is.EqualTo(1), "el primer vertido útil cuenta");
            _rig.RunUntilIdle(3000);
            _rig.Animator.Steps.ClearLog();

            BoardState dropped = _rig.Pour(counted, 0, 2, out PourVisual first);
            Assert.That(first.Events.LluviaDropMask, Is.EqualTo(1 << 0), "cae una gota en el origen");
            Assert.That(first.Events.LluviaEndedMask, Is.EqualTo(0), "no es la última");
            Assert.That(dropped[0].Mechanic, Is.EqualTo(MechanicType.Lluvia));
            BoardState after = _rig.Pour(dropped, 1, 0, out PourVisual second);
            Assert.That(second.Receiver, Is.EqualTo(0));
            Assert.That(after[0].Count, Is.EqualTo(3));
            int floor = first.After[0].Count;
            CuiaView rain = _rig.Views[0];
            CuiaView water = _rig.Views[1];
            float backAt = -1f;
            bool flew = false;
            bool secondStarted = false;
            float secondStartAt = -1f;
            int t = 0;
            while (!_rig.Animator.IsIdle)
            {
                Assert.That(t, Is.LessThan(8000), "la vista no se quedó quieta");
                _rig.Tick(PlayModeHelpers.FrameMs);
                t += PlayModeHelpers.FrameMs;
                flew |= rain.BodyInFront;
                if (flew && !rain.BodyInFront && backAt < 0f)
                {
                    backAt = _rig.Animator.Clock;
                }

                if (!secondStarted && water.BodyInFront)
                {
                    secondStarted = true;
                    secondStartAt = _rig.Animator.Clock;
                }

                if (secondStarted)
                {
                    Assert.That(rain.LiquidLevel, Is.GreaterThanOrEqualTo(floor - 1e-3f), "durante 1 → 0 la Lluvia conserva la gota (" + t + " ms). " + MechanicTestKit.Describe(_rig.Animator));
                }
            }

            MechanicStepRecord drop = MechanicTestKit.Record(_rig.Animator, MechanicStepKind.RainDrop, 0);
            TestContext.WriteLine(t + " ms hasta la vista quieta. " + MechanicTestKit.Describe(_rig.Animator));
            Assert.That(secondStarted, Is.True, "el segundo vertido se animó");
            Assert.That(backAt, Is.GreaterThan(0f), "el origen voló y volvió");
            Assert.That(drop.StartMs, Is.GreaterThanOrEqualTo(backAt - PlayModeHelpers.FrameMs), "la gota espera a que el origen vuelva a su ranura");
            Assert.That(secondStartAt, Is.GreaterThanOrEqualTo(drop.EndMs - 0.01f), "el vertido a la Lluvia espera a su gota");
            for (int i = 0; i < after.Count; i++)
            {
                Assert.That(_rig.Animator.ShownOf(i), Is.EqualTo(after[i]), "cuia " + i);
            }

            Assert.That(rain.LiquidLevel, Is.EqualTo(3f).Within(1e-3f));
        }

        [UnityTest]
        public IEnumerator SourceSteps_WaitForReturn_CountInBudget()
        {
            // La espera hasta que el origen vuelve (320 ms en el banco: el caudal llega a los 550 y el origen vuelve a los
            // 870) cuenta en el presupuesto y no se acelera: con una gota de 3.000 ms en la Lluvia que vierte (más la tapa de
            // 450), el plan se acelera hasta acabar justo antes de los 2.800 ms desde el toque, sin saltar ninguna etapa.
            yield return null;
            BoardState start = Board(
                new[] { new[] { 10, 10, 10, 1 }, new[] { 1, 1 }, new[] { 2, 2 }, new[] { 2 }, new[] { 1 }, new[] { 2 } },
                new[] { new MechanicSpec(MechanicType.Lluvia, key: 10, n: 1, k: 2), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            var probe = new MechanicTestKit.ProbeOverlay(MechanicType.Lluvia);
            probe.Durations[(int)MechanicStepKind.RainEnd] = 3000f;
            _rig = new MechanicTestKit.Rig(Art, start, views => views[0].UseOverlay(probe));
            BoardState counted = _rig.Pour(start, 3, 2, out _);
            _rig.RunUntilIdle(3000);
            _rig.Animator.Steps.ClearLog();
            int overruns = _rig.Animator.Steps.BudgetOverruns;
            int devEvents = 0;
            _rig.Animator.DevEvent = (evt, fields) => devEvents++;

            BoardState after = _rig.Pour(counted, 0, 1, out PourVisual v);
            int elapsed = _rig.RunUntilIdle(6000);
            MechanicTestKit.AssertLogFollowsPlan(_rig.Animator, in v);
            TestContext.WriteLine(elapsed + " ms del toque a la vista quieta, factor " + _rig.Animator.Steps.LastSpeedFactor + ". " + MechanicTestKit.Describe(_rig.Animator));
            Assert.That(_rig.Animator.IsIdle, Is.True);
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs), "del toque a la vista quieta, con la espera");
            // El caudal llega a los 550 ms: quedan 2.250 − 1 de margen; la espera (320) no se acelera y la gota y la tapa
            // (3.000 + 450) se reparten el resto. Sin contar la espera, el factor sería 2.249 / 3.450 y el plan se pasaría.
            float expected = (MechanicTestKit.BudgetMs - 550f - MechanicStepQueue.BudgetSlackMs - 320f) / 3450f;
            Assert.That(_rig.Animator.Steps.LastSpeedFactor, Is.EqualTo(expected).Within(1e-3f), "la espera cuenta en el presupuesto");
            Assert.That(_rig.Animator.Steps.BudgetOverruns, Is.EqualTo(overruns), "cabe sin saltar");
            Assert.That(devEvents, Is.EqualTo(0), "sin salto no hay step_budget");
            Assert.That(probe.Steps.Count, Is.EqualTo(1), "la gota llega al overlay");
            Assert.That(probe.Steps[0].SpeedFactor, Is.EqualTo(expected).Within(1e-3f));
            Assert.That(probe.Steps[0].Kind, Is.EqualTo(MechanicStepKind.RainEnd));
            MechanicStepRecord drop = MechanicTestKit.Record(_rig.Animator, MechanicStepKind.RainEnd, 0);
            MechanicStepRecord seal = MechanicTestKit.Record(_rig.Animator, MechanicStepKind.Seal, 0);
            Assert.That(seal.Snapped, Is.False, "la tapa no salta");
            Assert.That(drop.StartMs - (_rig.Animator.Clock - elapsed), Is.EqualTo(870f).Within(0.5f), "la gota empieza cuando vuelve el origen (870 ms)");
            for (int i = 0; i < after.Count; i++)
            {
                Assert.That(_rig.Animator.ShownOf(i), Is.EqualTo(after[i]), "cuia " + i);
            }
        }

        [UnityTest]
        public IEnumerator Brote163_OpenStep()
        {
            yield return MechanicTestKit.LoadPlaying(163);
            GameBootstrap boot = GameBootstrap.Current!;
            int brote = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Brote);
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.BroteOpenedMask & (1 << brote)) != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            CuiaView view = boot.View!.Views[brote];
            PourVisual visual = MechanicTestKit.TapPour(boot, path[path.Length - 1], out _);
            int elapsed = StepWatching(boot, () =>
            {
                if (!MechanicTestKit.TryFindRecord(boot.View.Animator, MechanicStepKind.Open, brote, out _))
                {
                    Assert.That(view.Overlay!.Visible, Is.True, "el capullo se ve hasta su paso Open");
                }
            });

            MechanicTestKit.AssertLogFollowsPlan(boot.View.Animator, in visual);
            MechanicStepRecord open = MechanicTestKit.Record(boot.View.Animator, MechanicStepKind.Open, brote);
            Assert.That(open.Stage, Is.EqualTo(StepStage.B));
            Assert.That(open.Owner, Is.EqualTo(MechanicType.Brote));
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs));
            TestContext.WriteLine("Nivel " + boot.Flow!.LevelNumber + ": " + elapsed + " ms del toque a la vista quieta. " + MechanicTestKit.Describe(boot.View!.Animator));
            MechanicTestKit.AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator Pirana183_BiteFromCave_MidStepVictimHasDose()
        {
            yield return MechanicTestKit.LoadPlaying(183);
            GameBootstrap boot = GameBootstrap.Current!;
            int cave = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Pirana);
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.PiranaCaveMask & (1 << cave)) != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            int caveBefore = boot.Flow!.Session.Current[cave].Count;
            PourVisual visual = MechanicTestKit.TapPour(boot, path[path.Length - 1], out TapResult poured);
            int victim = poured.Pour.Receiver;
            int victimAfter = boot.Flow.Session.Current[victim].Count;
            float tapped = boot.View!.Animator.Clock;
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Bite, cave, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Bite, cave, 0.2f);
            CuiaView[] views = boot.View.Views;
            Assert.That(views[victim].LiquidLevel, Is.GreaterThan(victimAfter + 0.05f), "a mitad del mordisco la víctima aún muestra la dosis mordida");
            Assert.That(views[cave].LiquidLevel, Is.EqualTo((float)caveBefore).Within(1e-3f), "la cueva aún no la tiene");

            MechanicTestKit.StepToIdle(boot, 6000);
            float elapsed = boot.View.Animator.Clock - tapped;
            MechanicTestKit.AssertLogFollowsPlan(boot.View.Animator, in visual);
            MechanicStepRecord bite = MechanicTestKit.Record(boot.View.Animator, MechanicStepKind.Bite, cave);
            Assert.That(bite.Stage, Is.EqualTo(StepStage.E));
            Assert.That(bite.OtherIndex, Is.EqualTo(victim), "el mordisco lo ejecuta la cueva con OtherIndex = la víctima");
            Assert.That(bite.Owner, Is.EqualTo(MechanicType.Pirana));
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs), "del toque a la vista quieta");
            TestContext.WriteLine("Nivel " + boot.Flow!.LevelNumber + ": " + elapsed + " ms del toque a la vista quieta. " + MechanicTestKit.Describe(boot.View!.Animator));
            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(views[cave].LiquidLevel, Is.EqualTo(caveBefore + 1f).Within(1e-3f));
        }

        [UnityTest]
        public IEnumerator Chain_WithinBudget_FromTap()
        {
            // 1 → 0: tres dosis que sellan el taperebá (el caudal llega a los 1.110 ms) y sueltan la Liana. Con un Release de
            // 1.500 ms la cadena se acelera hasta llenar el presupuesto; con uno de 6.000 ni a × 0,35 cabe y la etapa (d) salta.
            // Siempre ≤ 2.800 ms: el plan acelerado acaba BudgetSlackMs antes del presupuesto y el banco avanza en fotogramas de
            // 16 ms desde el toque (2.800 = 175 × 16), así que el fotograma en que la vista queda quieta tampoco se pasa.
            yield return null;
            float[] releases = { 1500f, 6000f };
            for (int r = 0; r < releases.Length; r++)
            {
                BoardState before = Board(
                    new[] { new[] { 3 }, new[] { 3, 3, 3 }, new[] { 1, 2, 1, 2 }, new[] { 1, 1 }, new[] { 2, 2 }, new int[0] },
                    new[] { MechanicSpec.None, MechanicSpec.None, new MechanicSpec(MechanicType.Liana, key: 3), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
                var probe = new MechanicTestKit.ProbeOverlay(MechanicType.Liana);
                probe.Durations[(int)MechanicStepKind.Release] = releases[r];
                _rig?.Dispose();
                _rig = new MechanicTestKit.Rig(Art, before, views => views[2].UseOverlay(probe));
                int devEvents = 0;
                string? lastEvent = null;
                string? lastFields = null;
                _rig.Animator.DevEvent = (evt, fields) =>
                {
                    devEvents++;
                    lastEvent = evt;
                    lastFields = fields;
                };
                int overruns = _rig.Animator.Steps.BudgetOverruns;
                BoardState after = _rig.Pour(before, 1, 0, out PourVisual v);
                Assert.That(v.Amount, Is.EqualTo(3));
                int elapsed = _rig.RunUntilIdle(6000);
                Assert.That(_rig.Animator.IsIdle, Is.True);
                MechanicTestKit.AssertLogFollowsPlan(_rig.Animator, in v);
                Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs), "release " + releases[r] + ": del toque a la vista quieta");
                Assert.That(_rig.Animator.Steps.LastSpeedFactor, Is.LessThan(1f), "se acelera para caber");
                Assert.That(_rig.Animator.Steps.LastSpeedFactor, Is.GreaterThanOrEqualTo(MechanicStepQueue.MinBudgetSpeed - 1e-4f));
                TestContext.WriteLine("Release " + releases[r] + " ms: " + elapsed + " ms del toque a la vista quieta. " + MechanicTestKit.Describe(_rig.Animator));
                if (r == 0)
                {
                    Assert.That(probe.Steps.Count, Is.EqualTo(1), "el Release se ejecuta");
                    Assert.That(probe.Steps[0].SpeedFactor, Is.LessThan(1f));
                    Assert.That(_rig.Animator.Steps.BudgetOverruns, Is.EqualTo(overruns), "cabe sin saltar");
                    Assert.That(devEvents, Is.EqualTo(0), "sin salto no hay step_budget");
                }
                else
                {
                    Assert.That(_rig.Animator.Steps.BudgetOverruns, Is.EqualTo(overruns + 1), "la última etapa salta (step_budget)");
                    Assert.That(probe.Steps.Count, Is.EqualTo(0), "el Release saltado no llega al overlay");
                    Assert.That(devEvents, Is.EqualTo(1), "un step_budget por DevEvent (DevProbe.Event)");
                    Assert.That(lastEvent, Is.EqualTo("step_budget"));
                    Assert.That(lastFields, Does.Contain("\"skip_from\":\"D\""), "salta desde la etapa (d)");
                    Assert.That(lastFields, Does.StartWith("\"age_ms\":"), "miembros JSON sin llaves, como pide DevProbe.Event");
                }

                for (int i = 0; i < after.Count; i++)
                {
                    Assert.That(_rig.Animator.ShownOf(i), Is.EqualTo(after[i]), "cuia " + i);
                }
            }
        }

        [UnityTest]
        public IEnumerator Snap_RestoresModelState()
        {
            yield return MechanicTestKit.LoadPlaying(83);
            GameBootstrap boot = GameBootstrap.Current!;
            int liana = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Liana);
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.LianaReleasedMask & (1 << liana)) != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            TapResult poured = MechanicTestKit.Tap(boot, path[path.Length - 1]);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Seal, poured.Pour.Receiver, 4000);
            Assert.That(boot.View!.IsViewIdle, Is.False, "IsIdle es false mientras haya pasos");
            Assert.That(boot.View.Animator.Steps.RunningCount, Is.GreaterThan(0));
            boot.View.Animator.SnapAll();
            Assert.That(boot.View.Animator.Steps.ActiveCount, Is.EqualTo(0));
            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(boot.View.Views[liana].Overlay!.Visible, Is.False, "la Liana, suelta");
        }

        [UnityTest]
        public IEnumerator Overlay_HostWiredBeforeAttach()
        {
            // Juego real: el overlay de la Hoja ya nació con el tablero y las capas de la pantalla.
            yield return MechanicTestKit.LoadPlaying(43);
            GameBootstrap boot = GameBootstrap.Current!;
            int hoja = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Hoja);
            CuiaOverlayHost host = boot.View!.Views[hoja].Overlay!.AttachedHost!;
            Assert.That(host.Board, Is.SameAs(boot.View.Animator), "el tablero es la cola");
            Assert.That(host.BoardBackLayer, Is.Not.Null);
            Assert.That(host.BoardFrontLayer, Is.Not.Null);

            // Banco: un overlay pedido antes de BindBoard se une en BindBoard, con todo cableado antes de OnAttach.
            BoardState board = Board(new[] { new int[0], new[] { 4, 4 } }, new[] { new MechanicSpec(MechanicType.Hoja, key: 4), MechanicSpec.None });
            var probe = new MechanicTestKit.ProbeOverlay(MechanicType.Hoja);
            int attachesBeforeBind = -1;
            _rig = new MechanicTestKit.Rig(Art, board, views =>
            {
                views[0].UseOverlay(probe);
                views[0].ShowContent(board[0], false);
                attachesBeforeBind = probe.Attaches;
            });
            Assert.That(attachesBeforeBind, Is.EqualTo(0), "sin tablero no se une");
            Assert.That(probe.Attaches, Is.EqualTo(1));
            Assert.That(probe.HostWiredAtAttach, Is.True, "BoardBackLayer, BoardFrontLayer y Board antes de OnAttach");
            Assert.That(probe.Visible, Is.True, "se pinta con el último estado mostrado");
            Assert.That(probe.AttachedHost!.Board, Is.SameAs(_rig.Animator));
            Vector2 foot = _rig.Animator.Foot(1);
            Vector2 center = _rig.Animator.BodyCenter(1);
            Assert.That(center.y - foot.y, Is.EqualTo(0.5f * _rig.Views[1].HeightU * _rig.Views[1].Unit).Within(0.01f), "posiciones en el espacio de las capas");
            Rect slot = _rig.Animator.SlotRect(1);
            Assert.That(slot.Contains(center), Is.True);
            Assert.That(slot.yMin, Is.EqualTo(foot.y - IntroChoreography.ShelfUndersideBelowFootDp).Within(0.01f), "ranura y repisa: hasta 15 dp bajo el pie");
            Assert.That(slot.yMax - foot.y, Is.EqualTo((_rig.Views[1].HeightU * _rig.Views[1].Unit) + CuiaView.TouchHeadroomDp).Within(0.01f), "hasta el área táctil sobre la boca");
            Assert.That(_rig.Animator.Foot(1).x - _rig.Animator.Foot(0).x, Is.EqualTo(64f).Within(0.01f), "paso de 64 dp del banco");
        }

        [UnityTest]
        public IEnumerator BoardLayout_AfterAddCuia()
        {
            FunnelSetup.Hooks();
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return MechanicTestKit.LoadPlaying(43);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            var probe = new MechanicTestKit.ProbeOverlay(MechanicType.Matapi);
            view.Views[0].UseOverlay(probe);
            Assert.That(probe.HostWiredAtAttach, Is.True);
            int layouts = probe.BoardLayouts;
            int version = view.Animator.LayoutVersion;
            int cuias = view.Views.Length;
            Assert.That(boot.PressExtraCuia(), Is.True, "Cuia extra con semillas");
            Assert.That(view.Views.Length, Is.EqualTo(cuias + 1));
            Assert.That(view.Views[cuias].IsBoardBound, Is.True, "la cuia nueva nace con tablero");
            Assert.That(probe.BoardLayouts, Is.GreaterThan(layouts), "OnBoardLayout tras AddCuiaView");
            PlayModeHelpers.Step(boot, 700);
            Assert.That(view.Animator.LayoutVersion, Is.GreaterThan(version + 1), "y en cada fotograma del relayout");
            Assert.That(probe.BoardLayouts, Is.GreaterThan(layouts + 1));
        }

        [UnityTest]
        public IEnumerator NoAllocDuringSteps()
        {
            yield return null;

            // Calentamiento: la misma cadena una vez (JIT y primeras llamadas), y después se mide otra en un banco nuevo.
            for (int pass = 0; pass < 2; pass++)
            {
                RunChain(pass == 1);
            }
        }

        // Cadena gota → sello → liana (y otra de Liana con sellado del receptor) en el banco; con measure, 60 fotogramas
        // sin asignaciones (Recorder GC.Alloc) desde el toque y otros 60 hasta el final.
        private void RunChain(bool measure)
        {
            BoardState[] starts =
            {
                Board(
                    new[] { new[] { 10, 10, 10 }, new[] { 1, 2, 1, 2 }, new[] { 2, 1 }, new[] { 1 }, new[] { 2 }, new int[0] },
                    new[] { new MechanicSpec(MechanicType.Lluvia, key: 10, n: 1, k: 2), new MechanicSpec(MechanicType.Liana, key: 10), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None }),
                Board(
                    new[] { new[] { 3, 3, 3 }, new[] { 3 }, new[] { 1, 2, 1, 2 }, new[] { 1, 1 }, new[] { 2, 2 }, new int[0] },
                    new[] { MechanicSpec.None, MechanicSpec.None, new MechanicSpec(MechanicType.Liana, key: 3), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None }),
            };

            for (int c = 0; c < starts.Length; c++)
            {
                _rig?.Dispose();
                _rig = new MechanicTestKit.Rig(Art, starts[c]);
                BoardState s = starts[c];
                if (c == 0)
                {
                    s = _rig.Pour(s, 3, 2, out _); // cuenta 1: el vertido siguiente deja caer la última gota
                    _rig.RunUntilIdle(3000);
                    _rig.Pour(s, 2, 5, out PourVisual v);
                    Assert.That(v.Events.LianaReleasedMask, Is.EqualTo(1 << 1), "gota → sello → liana");
                }
                else
                {
                    _rig.Pour(s, 1, 0, out _);
                }

                MechanicTestKit.Rig rig = _rig;
                if (measure)
                {
                    Assert.That(() => rig.Run(60 * PlayModeHelpers.FrameMs), Is.Not.AllocatingGCMemory(), "cadena " + c + ": 60 fotogramas desde el toque");
                    Assert.That(() => rig.Run(60 * PlayModeHelpers.FrameMs), Is.Not.AllocatingGCMemory(), "cadena " + c + ": 60 fotogramas más");
                }

                rig.RunUntilIdle(4000);
                Assert.That(rig.Animator.IsIdle, Is.True);
                Assert.That(rig.Animator.Steps.LogCount, Is.GreaterThan(0), "la cadena tuvo pasos");
            }
        }

        // Vertidos del nivel cargado hasta uno que cumple wanted (el último del camino).
        private static Pour[] Trigger(GameBootstrap boot, MechanicTestKit.PourWanted wanted)
        {
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, wanted);
            Assert.That(path, Is.Not.Null, "vertidos que disparan el evento en el nivel " + boot.Flow!.LevelNumber);
            return path!;
        }

        // Avanza de fotograma en fotograma hasta la vista quieta, llamando a each en cada uno; devuelve los ms.
        private static int StepWatching(GameBootstrap boot, System.Action each)
        {
            int t = 0;
            while (!boot.View!.IsViewIdle)
            {
                Assert.That(t, Is.LessThan(6000), "la vista no se quedó quieta");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                t += PlayModeHelpers.FrameMs;
                each();
            }

            return t;
        }

        private static void RunUntil(MechanicTestKit.Rig rig, System.Func<bool> condition, int maxMs)
        {
            for (int t = 0; t < maxMs && !condition(); t += PlayModeHelpers.FrameMs)
            {
                rig.Tick(PlayModeHelpers.FrameMs);
            }

            Assert.That(condition(), Is.True, "condición del banco");
        }

        private static BoardState Board(int[][] cuias, MechanicSpec[]? attrs)
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
    }
}
