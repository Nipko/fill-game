#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Lluvia y Piraña (T17; 02 §9.1 M6-M7; propuesta de D-007 b.2 M6-M7; Q18 B; respuesta Q-C): en los estrenos reales (143
    /// y 183) y en un banco sintético (<see cref="MechanicTestKit.Rig"/>), los pasos de la etapa (e) los ejecutan sus overlays
    /// con su duración [P] × factor de velocidad, en el orden del plan, pintando las dos cuias desde <c>ctx.Before</c> hasta
    /// <c>ctx.After</c>; tras cada paso la vista coincide con <c>Session.Current</c>. La gota cae con el tercer vertido útil
    /// (tinte 10 arriba), un fútil no cuenta, la tapa cae después de la gota, con la cuia llena la gota espera colgando y la
    /// última se lleva la nube. El cardumen sale de la cueva, muerde y se lleva la dosis (a mitad del paso la víctima aún la
    /// tiene); si el vertido sella la cuia o la cueva está llena, el mordisco se pierde (amago) y con la cueva llena duerme.
    /// El cardumen no tapa cuias ni captura toques, y nada asigna memoria durante los pasos.
    /// </summary>
    public sealed class LluviaPiranaTests
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
        public IEnumerator Lluvia143_DropAfterThreeUseful()
        {
            yield return MechanicTestKit.LoadPlaying(143);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            int rain = MechanicTestKit.IndexOf(flow, MechanicType.Lluvia);
            int bit = 1 << rain;
            var cloud = (LluviaOverlay)boot.View!.Views[rain].Overlay!;
            Assert.That(flow.Session.Current[rain].MechanicK, Is.EqualTo(3), "143: una gota cada 3 vertidos útiles");
            Assert.That(cloud.PipCount, Is.EqualTo(3));
            Assert.That(cloud.FilledPips, Is.EqualTo(0));
            Assert.That(cloud.DropCount, Is.EqualTo(2), "dos gotas pendientes");

            // Vertidos hasta la primera gota; se juegan uno a uno contando los útiles (Q18 B) y mirando los puntos.
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.LluviaDropMask & bit) != 0 && ev.LluviaEndedMask == 0);
            int useful = 0;
            int waits = 0;
            for (int i = 0; i < path.Length - 1; i++)
            {
                boot.View.Animator.Steps.ClearLog();
                MechanicTestKit.TapPour(boot, path[i], out TapResult prior);
                useful += prior.Pour.Events.Futile ? 0 : 1;
                waits += (prior.Pour.Events.LluviaWaitingMask & bit) != 0 ? 1 : 0;
                MechanicTestKit.StepToIdle(boot, 6000);
                MechanicTestKit.AssertViewMatchesModel(boot);
                Assert.That(cloud.FilledPips, Is.EqualTo(flow.Session.Current[rain].MechanicCounter), "vertido " + i + ": los puntos siguen al contador");
                Assert.That(MechanicTestKit.TryFindRecord(boot.View.Animator, MechanicStepKind.RainDrop, rain, out _), Is.False, "vertido " + i + ": aún no cae");
            }

            boot.View.Animator.Steps.ClearLog();
            PourVisual visual = MechanicTestKit.TapPour(boot, path[path.Length - 1], out TapResult poured);
            Assert.That(poured.Pour.Events.Futile, Is.False);
            int before = flow.Session.Current[rain].Count - 1; // la gota es la dosis de arriba de After (el vertido pudo llenar la Lluvia)
            useful++;
            Assert.That(useful, Is.EqualTo(3 + waits), "la gota cae con el tercer vertido útil (o el siguiente con hueco)");

            CuiaView view = boot.View.Views[rain];
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.RainDrop, rain, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.RainDrop, rain, 0.5f);
            Assert.That(cloud.IsAnimating, Is.True, "la gota la anima su overlay");
            Assert.That(cloud.FallingDropVisible, Is.True, "a mitad, la gota cae");
            Assert.That(cloud.FallingDropHeightU, Is.LessThan(view.HeightU + LluviaOverlay.CloudGapU), "ya bajo la nube");
            Assert.That(cloud.FilledPips, Is.EqualTo(3), "el vertido que completa el ritmo llena los puntos");
            Assert.That(view.DoseFill(before), Is.LessThan(0.01f), "la dosis aún no se forma");
            Assert.That(view.LiquidLevel, Is.EqualTo((float)before).Within(1e-3f), "ctx.Before no tiene la gota");

            MechanicTestKit.StepToIdle(boot, 6000);
            BoardAnimator animator = boot.View.Animator;
            MechanicTestKit.AssertLogFollowsPlan(animator, in visual);
            MechanicStepRecord drop = MechanicTestKit.Record(animator, MechanicStepKind.RainDrop, rain);
            AssertOwnStep(drop, MechanicType.Lluvia, LluviaOverlay.DropStepMs);
            Cuia after = flow.Session.Current[rain];
            Assert.That(after.TopColor, Is.EqualTo(10), "tinte 10 arriba (Agua del Tapajós)");
            Assert.That(view.DoseColor(after.Count - 1), Is.EqualTo(Palette.Tint(10)));
            Assert.That(cloud.FilledPips, Is.EqualTo(0), "el contador vuelve a empezar");
            Assert.That(cloud.DropCount, Is.EqualTo(1), "queda una gota");
            Assert.That(cloud.IsAnimating, Is.False);
            MechanicTestKit.AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator Lluvia143_FutileDoesNotAdvance()
        {
            yield return MechanicTestKit.LoadPlaying(143);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            int rain = MechanicTestKit.IndexOf(flow, MechanicType.Lluvia);
            var cloud = (LluviaOverlay)boot.View!.Views[rain].Overlay!;
            Pour[] futile = Trigger(boot, (ev, b, a) => ev.Futile && ev.LluviaDropMask == 0);
            MechanicTestKit.PlayPrefix(boot, futile);
            int counter = flow.Session.Current[rain].MechanicCounter;
            int filled = cloud.FilledPips;
            Assert.That(filled, Is.EqualTo(counter));

            TapResult f = MechanicTestKit.Tap(boot, futile[futile.Length - 1]);
            Assert.That(f.Kind, Is.EqualTo(TapKind.Poured));
            Assert.That(f.Pour.Events.Futile, Is.True);
            MechanicTestKit.StepToIdle(boot, 6000);
            Assert.That(flow.Session.Current[rain].MechanicCounter, Is.EqualTo(counter), "el fútil no avanza el contador (Q18 B)");
            Assert.That(cloud.FilledPips, Is.EqualTo(filled), "ni los puntos de la nube");
            Assert.That(MechanicTestKit.TryFindRecord(boot.View.Animator, MechanicStepKind.RainDrop, rain, out _), Is.False);
            Assert.That(MechanicTestKit.TryFindRecord(boot.View.Animator, MechanicStepKind.RainWait, rain, out _), Is.False);
            MechanicTestKit.AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator Lluvia143_DropBeforeSeal()
        {
            yield return MechanicTestKit.LoadPlaying(143);
            GameBootstrap boot = GameBootstrap.Current!;
            int rain = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Lluvia);
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.LluviaEndedMask & ev.SealedMask & (1 << rain)) != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            var cloud = (LluviaOverlay)boot.View!.Views[rain].Overlay!;
            CuiaView view = boot.View.Views[rain];
            PourVisual visual = MechanicTestKit.TapPour(boot, path[path.Length - 1], out _);

            // La última gota cae y la nube sube y se desvanece; la tapa todavía no.
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.RainEnd, rain, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.RainEnd, rain, 0.85f);
            Assert.That(view.Sealed, Is.False, "la tapa no cae con la gota");
            Assert.That(cloud.Visible, Is.True, "la nube aún se ve mientras se va");
            Assert.That(cloud.CloudAlpha, Is.LessThan(0.8f), "la nube se desvanece");
            Assert.That(cloud.CloudRiseU, Is.GreaterThan(0.2f), "la nube sube");

            // Después, la tapa (c′) sobre las cuatro aguas y sin nube.
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Seal, rain, 4000);
            Assert.That(cloud.Visible, Is.False, "con la última gota se fue la nube");
            Assert.That(view.LiquidLevel, Is.EqualTo(4f).Within(1e-3f), "la gota ya está");
            MechanicTestKit.StepToIdle(boot, 6000);

            BoardAnimator animator = boot.View.Animator;
            MechanicTestKit.AssertLogFollowsPlan(animator, in visual);
            MechanicStepRecord drop = MechanicTestKit.Record(animator, MechanicStepKind.RainEnd, rain);
            MechanicStepRecord seal = MechanicTestKit.Record(animator, MechanicStepKind.Seal, rain);
            AssertOwnStep(drop, MechanicType.Lluvia, LluviaOverlay.DropStepMs);
            Assert.That(drop.Stage, Is.EqualTo(StepStage.E));
            Assert.That(seal.Stage, Is.EqualTo(StepStage.CPrime), "la tapa es del punto fijo f′");
            Assert.That(seal.StartMs, Is.GreaterThanOrEqualTo(drop.EndMs - 0.01f), "la tapa cae después de la gota. " + MechanicTestKit.Describe(animator));
            Assert.That(boot.Flow!.Session.Current[rain].TopColor, Is.EqualTo(10));
            Assert.That(view.Sealed, Is.True);
            MechanicTestKit.AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator Lluvia_WaitWhenFull()
        {
            // La Lluvia [10, 10, 1, 1] (llena, dos gotas, k = 2): dos vertidos útiles fuera de ella y la gota no cabe (RainWait).
            yield return null;
            BoardState start = Board(
                new[] { 4, 4, 4, 4, 4 },
                new[] { new[] { 10, 10, 1, 1 }, new[] { 1, 2 }, new[] { 2, 1 }, new[] { 2, 2 }, new int[0] },
                new[] { new MechanicSpec(MechanicType.Lluvia, key: 10, n: 2, k: 2), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            _rig = new MechanicTestKit.Rig(Art, start);
            var cloud = (LluviaOverlay)_rig.Views[0].Overlay!;
            Assert.That(cloud.Stormy, Is.True, "llena: la nube se carga");
            Assert.That(cloud.HangingDrop, Is.False, "aún no toca la gota");
            BoardState counted = _rig.Pour(start, 1, 3, out _);
            _rig.RunUntilIdle(3000);
            Assert.That(cloud.HangingDrop, Is.True, "llena y con el ritmo a k − 1: la gota cuelga");
            _rig.Animator.Steps.ClearLog();

            BoardState after = _rig.Pour(counted, 2, 4, out PourVisual v);
            Assert.That(v.Events.LluviaWaitingMask, Is.EqualTo(1), "la gota espera");
            RunUntil(_rig, () => _rig.Animator.Steps.IsRunning(MechanicStepKind.RainWait, 0), 3000);
            RunToFraction(_rig, MechanicStepKind.RainWait, 0, 0.5f);
            Assert.That(cloud.IsAnimating, Is.True);
            Assert.That(cloud.CloudDarkness, Is.GreaterThan(0.9f), "la nube se oscurece");
            Assert.That(cloud.HangingDrop, Is.True, "la gota intenta caer");
            Assert.That(cloud.FilledPips, Is.EqualTo(2), "el vertido completó el ritmo");
            Assert.That(_rig.Views[0].LiquidLevel, Is.EqualTo(4f).Within(1e-3f), "nada cae");

            int elapsed = _rig.RunUntilIdle(3000);
            MechanicTestKit.AssertLogFollowsPlan(_rig.Animator, in v);
            AssertOwnStep(MechanicTestKit.Record(_rig.Animator, MechanicStepKind.RainWait, 0), MechanicType.Lluvia, LluviaOverlay.WaitStepMs);
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs));
            Assert.That(cloud.Stormy && cloud.HangingDrop, Is.True, "la gota queda colgando");
            Assert.That(cloud.FilledPips, Is.EqualTo(1), "el contador se queda en k − 1");
            Assert.That(cloud.DropCount, Is.EqualTo(2), "siguen las dos gotas");
            AssertRigShowsModel(after);
        }

        [UnityTest]
        public IEnumerator Lluvia_EndRemovesCloud()
        {
            // La Lluvia [10, 10, 10] con su última gota (k = 2): la gota cae, la nube se va y la tapa (c′) llega después.
            yield return null;
            BoardState start = Board(
                new[] { 4, 4, 4, 4, 4 },
                new[] { new[] { 10, 10, 10 }, new[] { 1, 2 }, new[] { 2, 1 }, new[] { 2, 2 }, new[] { 1, 1 } },
                new[] { new MechanicSpec(MechanicType.Lluvia, key: 10, n: 1, k: 2), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            _rig = new MechanicTestKit.Rig(Art, start);
            var cloud = (LluviaOverlay)_rig.Views[0].Overlay!;
            BoardState counted = _rig.Pour(start, 1, 3, out _);
            _rig.RunUntilIdle(3000);
            _rig.Animator.Steps.ClearLog();

            BoardState after = _rig.Pour(counted, 2, 4, out PourVisual v);
            Assert.That(v.Events.LluviaEndedMask, Is.EqualTo(1), "la última gota");
            Assert.That(after[0].Mechanic, Is.EqualTo(MechanicType.None));
            RunUntil(_rig, () => _rig.Animator.Steps.IsRunning(MechanicStepKind.RainEnd, 0), 3000);
            RunToFraction(_rig, MechanicStepKind.RainEnd, 0, 0.5f);
            Assert.That(cloud.FallingDropVisible, Is.True, "la gota cae");
            Assert.That(cloud.CloudRiseU, Is.GreaterThan(0f), "la nube empieza a subir");
            RunToFraction(_rig, MechanicStepKind.RainEnd, 0, 0.9f);
            Assert.That(cloud.CloudAlpha, Is.LessThan(0.5f), "y se desvanece");
            Assert.That(_rig.Views[0].Sealed, Is.False, "la tapa aún no");

            _rig.RunUntilIdle(3000);
            MechanicTestKit.AssertLogFollowsPlan(_rig.Animator, in v);
            AssertOwnStep(MechanicTestKit.Record(_rig.Animator, MechanicStepKind.RainEnd, 0), MechanicType.Lluvia, LluviaOverlay.DropStepMs);
            Assert.That(cloud.Visible, Is.False, "sin nube");
            Assert.That(VisibleGraphics(_rig.Views[0].OverlayHost), Is.EqualTo(0), "ninguna pieza de la nube a la vista");
            Assert.That(_rig.Views[0].DoseColor(3), Is.EqualTo(Palette.Tint(10)), "la última gota, arriba");
            Assert.That(_rig.Views[0].Sealed, Is.True, "sellada con el agua");
            AssertRigShowsModel(after);
        }

        [UnityTest]
        public IEnumerator Pirana183_BiteFromCave()
        {
            yield return MechanicTestKit.LoadPlaying(183);
            GameBootstrap boot = GameBootstrap.Current!;
            int cave = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Pirana);
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.PiranaCaveMask & (1 << cave)) != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            var school = (PiranaOverlay)boot.View!.Views[cave].Overlay!;
            Vector2 home = school.SchoolHome;
            int caveBefore = boot.Flow!.Session.Current[cave].Count;
            PourVisual visual = MechanicTestKit.TapPour(boot, path[path.Length - 1], out TapResult poured);
            int victim = poured.Pour.Receiver;
            int bitten = boot.Flow.Session.Current[cave].TopColor;
            float tapped = boot.View.Animator.Clock;
            float u = boot.View.Views[cave].Unit;

            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Bite, cave, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Bite, cave, 0.3f);
            Assert.That(school.IsAnimating, Is.True, "el mordisco lo anima la cueva");
            Assert.That((school.SchoolCenter - home).magnitude, Is.GreaterThan(0.5f * u), "el cardumen sale de la cueva");
            Assert.That(school.MorselVisible, Is.False, "aún no ha mordido");
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Bite, cave, 0.75f);
            Assert.That(school.MorselVisible, Is.True, "se lleva el bocado");

            MechanicTestKit.StepToIdle(boot, 6000);
            float elapsed = boot.View.Animator.Clock - tapped;
            BoardAnimator animator = boot.View.Animator;
            MechanicTestKit.AssertLogFollowsPlan(animator, in visual);
            MechanicStepRecord bite = MechanicTestKit.Record(animator, MechanicStepKind.Bite, cave);
            AssertOwnStep(bite, MechanicType.Pirana, PiranaOverlay.BiteStepMs);
            Assert.That(bite.OtherIndex, Is.EqualTo(victim), "OtherIndex = la víctima");
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs), "del toque a la vista quieta");
            Assert.That(boot.View.Views[cave].LiquidLevel, Is.EqualTo(caveBefore + 1f).Within(1e-3f), "la dosis, en la cueva");
            Assert.That(boot.View.Views[cave].DoseColor(caveBefore), Is.EqualTo(Palette.Tint(bitten)), "la fruta mordida");
            Assert.That(school.MorselVisible, Is.False);
            Assert.That((school.SchoolCenter - school.SchoolHome).magnitude, Is.LessThan(0.8f * u), "de vuelta junto a la cueva");
            TestContext.WriteLine("Nivel 183: " + elapsed + " ms del toque a la vista quieta. " + MechanicTestKit.Describe(animator));
            MechanicTestKit.AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator Pirana183_MidBiteVictimHasDose()
        {
            yield return MechanicTestKit.LoadPlaying(183);
            GameBootstrap boot = GameBootstrap.Current!;
            int cave = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Pirana);
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.PiranaCaveMask & (1 << cave)) != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            var school = (PiranaOverlay)boot.View!.Views[cave].Overlay!;
            int caveBefore = boot.Flow!.Session.Current[cave].Count;
            MechanicTestKit.TapPour(boot, path[path.Length - 1], out TapResult poured);
            int victim = poured.Pour.Receiver;
            int victimAfter = boot.Flow.Session.Current[victim].Count;
            int bitten = boot.Flow.Session.Current[cave].TopColor;
            CuiaView[] views = boot.View.Views;

            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Bite, cave, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Bite, cave, 0.5f);
            Assert.That(views[victim].DoseFill(victimAfter), Is.GreaterThan(0.99f), "a mitad del paso la víctima aún tiene la dosis");
            Assert.That(views[victim].DoseColor(victimAfter), Is.EqualTo(Palette.Tint(bitten)), "la fruta que se va a llevar");
            Assert.That(views[victim].LiquidLevel, Is.EqualTo(victimAfter + 1f).Within(1e-3f));
            Assert.That(views[cave].LiquidLevel, Is.EqualTo((float)caveBefore).Within(1e-3f), "la cueva aún no la tiene");
            float u = views[cave].Unit;
            Vector2 victimMouth = boot.View.Animator.Foot(victim) + new Vector2(0f, views[victim].HeightU * u);
            Assert.That((school.SchoolCenter - victimMouth).magnitude, Is.LessThan((school.SchoolHome - victimMouth).magnitude), "el cardumen está en la víctima");

            MechanicTestKit.StepToIdle(boot, 6000);
            Assert.That(views[victim].LiquidLevel, Is.EqualTo((float)victimAfter).Within(1e-3f));
            MechanicTestKit.AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator Pirana183_SealAvoidsBite()
        {
            yield return MechanicTestKit.LoadPlaying(183);
            GameBootstrap boot = GameBootstrap.Current!;
            int cave = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Pirana);
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.PiranaBiteLostMask & (1 << cave)) != 0 && ev.Receiver != cave && (ev.SealedMask & (1 << ev.Receiver)) != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            var school = (PiranaOverlay)boot.View!.Views[cave].Overlay!;
            Cuia caveBefore = boot.Flow!.Session.Current[cave];
            PourVisual visual = MechanicTestKit.TapPour(boot, path[path.Length - 1], out TapResult poured);
            int victim = poured.Pour.Receiver;
            Assert.That(poured.Pour.Events.PiranaCaveMask, Is.EqualTo(0), "sellar la cuia destino evita el mordisco (02 §9.1 M7)");
            Assert.That(boot.Flow.Session.Current.IsSealed(victim), Is.True);

            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.BiteLost, cave, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.BiteLost, cave, 0.5f);
            Assert.That(school.IsAnimating, Is.True, "el amago lo anima la cueva");
            Assert.That(school.MorselVisible, Is.False, "no se lleva nada");
            Assert.That(boot.View.Views[cave].LiquidLevel, Is.EqualTo((float)caveBefore.Count).Within(1e-3f));

            MechanicTestKit.StepToIdle(boot, 6000);
            BoardAnimator animator = boot.View.Animator;
            MechanicTestKit.AssertLogFollowsPlan(animator, in visual);
            Assert.That(MechanicTestKit.TryFindRecord(animator, MechanicStepKind.Bite, cave, out _), Is.False, "sin mordisco");
            AssertOwnStep(MechanicTestKit.Record(animator, MechanicStepKind.BiteLost, cave), MechanicType.Pirana, PiranaOverlay.BiteLostStepMs);
            Assert.That(boot.Flow.Session.Current[victim].Count, Is.EqualTo(4), "la víctima sellada entera");
            Assert.That(boot.Flow.Session.Current[cave].Count, Is.EqualTo(caveBefore.Count), "la cueva no recibe nada");
            Assert.That(school.FilledPips, Is.EqualTo(0), "el contador vuelve a empezar");
            MechanicTestKit.AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator Pirana_BiteLost_QC()
        {
            // Cueva llena [3, 4] (k = 2): el segundo vertido útil completa el ritmo y el mordisco se pierde (respuesta Q-C).
            yield return null;
            BoardState start = CaveFullBoard();
            _rig = new MechanicTestKit.Rig(Art, start);
            var school = (PiranaOverlay)_rig.Views[0].Overlay!;
            Assert.That(school.Dimmed, Is.True, "cueva llena: el cardumen duerme");
            BoardState counted = _rig.Pour(start, 4, 5, out _);
            _rig.RunUntilIdle(3000);
            _rig.Animator.Steps.ClearLog();

            BoardState after = _rig.Pour(counted, 3, 5, out PourVisual v);
            Assert.That(v.Events.PiranaBiteLostMask, Is.EqualTo(1), "mordisco perdido");
            Assert.That(v.Events.PiranaCaveMask, Is.EqualTo(0));
            RunUntil(_rig, () => _rig.Animator.Steps.IsRunning(MechanicStepKind.BiteLost, 0), 3000);
            RunToFraction(_rig, MechanicStepKind.BiteLost, 0, 0.5f);
            Assert.That(school.IsAnimating, Is.True);
            Assert.That(school.MorselVisible, Is.False);
            Assert.That(_rig.Views[0].LiquidLevel, Is.EqualTo(2f).Within(1e-3f), "la cueva no cambia");
            Assert.That(_rig.Views[5].LiquidLevel, Is.EqualTo(2f).Within(1e-3f), "la víctima no cambia");

            int elapsed = _rig.RunUntilIdle(3000);
            MechanicTestKit.AssertLogFollowsPlan(_rig.Animator, in v);
            AssertOwnStep(MechanicTestKit.Record(_rig.Animator, MechanicStepKind.BiteLost, 0), MechanicType.Pirana, PiranaOverlay.BiteLostStepMs);
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs));
            Assert.That(school.Dimmed, Is.True, "sigue durmiendo");
            Assert.That(school.FilledPips, Is.EqualTo(0));
            AssertRigShowsModel(after);
        }

        [UnityTest]
        public IEnumerator Pirana_SleepsWhenCaveFull()
        {
            yield return null;
            BoardState start = CaveFullBoard();
            _rig = new MechanicTestKit.Rig(Art, start);
            var school = (PiranaOverlay)_rig.Views[0].Overlay!;
            float u = _rig.Views[0].Unit;
            Assert.That(school.Dimmed, Is.True, "cueva llena (Blocked)");
            Assert.That(school.StonesAlpha, Is.EqualTo(PiranaOverlay.DimAlpha).Within(1e-3f));
            for (int i = 0; i < PiranaOverlay.SchoolSize; i++)
            {
                Assert.That(school.Fish(i).color.a, Is.EqualTo(PiranaOverlay.DimAlpha).Within(1e-3f), "pirañas atenuadas");
            }

            float drift = MaxFishTravel(_rig, school, 1200);
            Assert.That(school.SchoolAsleep, Is.True, "con los ojos cerrados");
            Assert.That(drift, Is.LessThan(0.1f * u), "dormido, no ronda");

            // Con hueco en la cueva, despierta y ronda.
            BoardState emptied = _rig.Pour(start, 0, 5, out _);
            Assert.That(emptied[0].IsFull, Is.False);
            _rig.RunUntilIdle(3000);
            Assert.That(school.Dimmed, Is.False, "con hueco, despierto");
            Assert.That(school.StonesAlpha, Is.EqualTo(1f).Within(1e-3f));
            Assert.That(school.SchoolAsleep, Is.False, "con los ojos abiertos");
            Assert.That(MaxFishTravel(_rig, school, 1200), Is.GreaterThan(0.3f * u), "despierto, ronda la cueva");
        }

        [UnityTest]
        public IEnumerator Cardumen_NoRaycast()
        {
            yield return MechanicTestKit.LoadPlaying(183);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen screen = boot.View!;
            int cave = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Pirana);
            var school = (PiranaOverlay)screen.Views[cave].Overlay!;
            RectTransform root = school.School;
            Assert.That(root.parent, Is.SameAs(screen.MechanicFrontLayer), "bajo la capa delantera del tablero");
            Assert.That(root.GetComponent<Canvas>(), Is.Not.Null, "Canvas anidado propio");
            Assert.That(root.GetComponentsInChildren<GraphicRaycaster>(true).Length, Is.EqualTo(0), "sin raycaster");
            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            Assert.That(graphics.Length, Is.GreaterThanOrEqualTo(PiranaOverlay.SchoolSize));
            for (int i = 0; i < graphics.Length; i++)
            {
                Assert.That(graphics[i].raycastTarget, Is.False, graphics[i].name + ": no captura toques");
            }

            // Ronda sin tapar ninguna cuia y sin quitarle el toque a nadie.
            GraphicRaycaster raycaster = screen.Canvas.GetComponent<GraphicRaycaster>();
            var hits = new List<RaycastResult>();
            Vector2 first = school.Fish(0).rectTransform.anchoredPosition;
            float travel = 0f;
            for (int t = 0; t < 2000; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                travel = Mathf.Max(travel, (school.Fish(0).rectTransform.anchoredPosition - first).magnitude);
                for (int f = 0; f < PiranaOverlay.SchoolSize; f++)
                {
                    Rect fish = WorldRect(school.Fish(f).rectTransform);
                    for (int c = 0; c < screen.Views.Length; c++)
                    {
                        Assert.That(fish.Overlaps(WorldRect(screen.Views[c].Body)), Is.False, "la piraña " + f + " no tapa la cuia " + c + " (" + t + " ms)");
                    }

                    if (raycaster != null && t % 400 == 0)
                    {
                        hits.Clear();
                        var data = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(screen.Canvas.worldCamera, fish.center) };
                        raycaster.Raycast(data, hits);
                        for (int h = 0; h < hits.Count; h++)
                        {
                            Assert.That(hits[h].gameObject.transform.IsChildOf(root), Is.False, "un toque nunca da en el cardumen");
                        }
                    }
                }
            }

            Assert.That(travel, Is.GreaterThan(0.3f * screen.Views[cave].Unit), "el cardumen ronda la cueva");
        }

        [UnityTest]
        public IEnumerator ReduceMotion_HalfDuration_NoParticles()
        {
            // Reducir movimiento (07 §7.8): los pasos van a × 0,5 por ctx.SpeedFactor y el pool no emite.
            yield return null;
            BoardState start = DropBoard();
            _rig = new MechanicTestKit.Rig(Art, start);
            _rig.Animator.ReduceMotion = true;
            _rig.Fx.ReduceMotion = true;
            BoardState counted = _rig.Pour(start, 1, 3, out _);
            _rig.RunUntilIdle(3000);
            _rig.Animator.Steps.ClearLog();
            _rig.Pour(counted, 2, 4, out PourVisual v);
            int maxParticles = 0;
            for (int t = 0; t < 3000 && !_rig.Animator.IsIdle; t += PlayModeHelpers.FrameMs)
            {
                _rig.Tick(PlayModeHelpers.FrameMs);
                maxParticles = Mathf.Max(maxParticles, _rig.Fx.ActiveCount);
            }

            MechanicStepRecord drop = MechanicTestKit.Record(_rig.Animator, MechanicStepKind.RainDrop, 0);
            Assert.That(drop.SpeedFactor, Is.EqualTo(0.5f).Within(1e-3f), "× 0,5 con Reducir movimiento");
            Assert.That(drop.EndMs - drop.StartMs, Is.EqualTo(0.5f * LluviaOverlay.DropStepMs).Within(0.5f));
            Assert.That(maxParticles, Is.EqualTo(0), "sin partículas");
        }

        [UnityTest]
        public IEnumerator NoAllocDuringSteps()
        {
            yield return null;

            // Calentamiento: cada cadena una vez en su banco, y después se mide otra vez en un banco nuevo.
            for (int pass = 0; pass < 2; pass++)
            {
                RunChain(DropBoard(), 1, 3, 2, 4, pass == 1, "gota");
                RunChain(WaitBoard(), 1, 3, 2, 4, pass == 1, "espera");
                RunChain(BiteBoard(), 1, 4, 2, 3, pass == 1, "mordisco");
                RunChain(CaveFullBoard(), 4, 5, 3, 5, pass == 1, "mordisco perdido");
            }
        }

        // Dos vertidos en un banco nuevo: el primero cuenta y el segundo dispara el paso; con measure, 60 fotogramas sin
        // asignaciones desde el toque y otros 60 hasta el final.
        private void RunChain(BoardState start, int a1, int b1, int a2, int b2, bool measure, string name)
        {
            _rig?.Dispose();
            _rig = new MechanicTestKit.Rig(Art, start);
            BoardState counted = _rig.Pour(start, a1, b1, out _);
            _rig.RunUntilIdle(3000);
            _rig.Animator.Steps.ClearLog();
            _rig.Pour(counted, a2, b2, out _);
            MechanicTestKit.Rig rig = _rig;
            if (measure)
            {
                Assert.That(() => rig.Run(60 * PlayModeHelpers.FrameMs), Is.Not.AllocatingGCMemory(), name + ": 60 fotogramas desde el toque");
                Assert.That(() => rig.Run(60 * PlayModeHelpers.FrameMs), Is.Not.AllocatingGCMemory(), name + ": 60 fotogramas más");
            }

            rig.RunUntilIdle(4000);
            Assert.That(rig.Animator.IsIdle, Is.True);
            Assert.That(rig.Animator.Steps.LogCount, Is.GreaterThan(0), name + ": hubo pasos");
            for (int i = 0; i < rig.Animator.Steps.LogCount; i++)
            {
                MechanicStepRecord r = rig.Animator.Steps.LogAt(i);
                if (r.Kind == MechanicStepKind.RainDrop || r.Kind == MechanicStepKind.RainWait || r.Kind == MechanicStepKind.Bite || r.Kind == MechanicStepKind.BiteLost)
                {
                    Assert.That(r.UsedDefault, Is.False, name + ": " + r + " lo ejecuta su overlay");
                }
            }
        }

        // La Lluvia [10, 10] (dos gotas, k = 2): 1 → 3 cuenta y 2 → 4 deja caer una gota (RainDrop).
        private static BoardState DropBoard()
        {
            return Board(
                new[] { 4, 4, 4, 4, 4 },
                new[] { new[] { 10, 10 }, new[] { 1, 2 }, new[] { 2, 1 }, new[] { 2, 2 }, new[] { 1, 1 } },
                new[] { new MechanicSpec(MechanicType.Lluvia, key: 10, n: 2, k: 2), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
        }

        // La Lluvia llena [10, 10, 1, 1] (dos gotas, k = 2): 1 → 3 cuenta y 2 → 4 deja la gota esperando (RainWait).
        private static BoardState WaitBoard()
        {
            return Board(
                new[] { 4, 4, 4, 4, 4 },
                new[] { new[] { 10, 10, 1, 1 }, new[] { 1, 2 }, new[] { 2, 1 }, new[] { 2, 2 }, new int[0] },
                new[] { new MechanicSpec(MechanicType.Lluvia, key: 10, n: 2, k: 2), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
        }

        // La cueva vacía (Cuia chica de 2, k = 2): 1 → 4 cuenta y 2 → 3 completa el ritmo; la piraña muerde la cima de 3.
        private static BoardState BiteBoard()
        {
            return Board(
                new[] { 2, 4, 4, 4, 4, 4 },
                new[] { new int[0], new[] { 1, 2 }, new[] { 2, 1 }, new[] { 1, 1 }, new[] { 2, 2 }, new int[0] },
                new[] { new MechanicSpec(MechanicType.Pirana, k: 2), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
        }

        // La cueva llena [3, 4] (k = 2): 4 → 5 cuenta y 3 → 5 completa el ritmo con la cueva llena (mordisco perdido, Q-C).
        private static BoardState CaveFullBoard()
        {
            return Board(
                new[] { 2, 4, 4, 4, 4, 4 },
                new[] { new[] { 3, 4 }, new[] { 3, 3, 3, 4 }, new[] { 4, 4, 1, 2 }, new[] { 1, 1, 2 }, new[] { 2, 1, 2 }, new int[0] },
                new[] { new MechanicSpec(MechanicType.Pirana, k: 2), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
        }

        private static BoardState Board(int[] caps, int[][] cuias, MechanicSpec[] attrs)
        {
            BoardBuildResult result = BoardBuilder.Build(caps, cuias, attrs);
            Assert.That(result.Error, Is.Null, "tablero de prueba");
            return result.State!;
        }

        private static Pour[] Trigger(GameBootstrap boot, MechanicTestKit.PourWanted wanted)
        {
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, wanted);
            Assert.That(path, Is.Not.Null, "vertidos que disparan el evento en el nivel " + boot.Flow!.LevelNumber);
            return path!;
        }

        // El paso lo ejecutó el overlay de la mecánica (no el paso por defecto) con su duración nominal × el factor del paso.
        private static void AssertOwnStep(MechanicStepRecord record, MechanicType owner, float nominalMs)
        {
            Assert.That(record.Owner, Is.EqualTo(owner), record + ": despachado al overlay de la cuia dueña");
            Assert.That(record.UsedDefault, Is.False, record + ": anima su overlay, no el paso por defecto");
            Assert.That(record.Snapped, Is.False, record + ": termina a su hora");
            Assert.That(record.EndMs - record.StartMs, Is.EqualTo(nominalMs * record.SpeedFactor).Within(0.5f), record + ": " + nominalMs + " ms × SpeedFactor");
        }

        // Con la cola quieta, cada vista del banco pinta el tablero del modelo.
        private void AssertRigShowsModel(BoardState model)
        {
            Assert.That(_rig!.Animator.IsIdle, Is.True);
            for (int i = 0; i < model.Count; i++)
            {
                Assert.That(_rig.Animator.ShownOf(i), Is.EqualTo(model[i]), "cuia " + i);
                Assert.That(_rig.Views[i].ShownCuia, Is.EqualTo(model[i]), "cuia " + i + ": la vista pinta el modelo");
                Assert.That(_rig.Views[i].Sealed, Is.EqualTo(model.IsSealed(i)), "tapa de la cuia " + i);
                Assert.That(_rig.Views[i].LiquidLevel, Is.EqualTo((float)model[i].Count).Within(1e-3f), "nivel de la cuia " + i);
                CuiaOverlay? overlay = _rig.Views[i].Overlay;
                if (overlay != null)
                {
                    Assert.That(overlay.IsAnimating, Is.False, "cuia " + i + ": ningún overlay animando");
                    Assert.That(overlay.Visible, Is.EqualTo(model[i].Mechanic == overlay.Type), "cuia " + i + ": overlay como el modelo");
                }
            }
        }

        private static void RunUntil(MechanicTestKit.Rig rig, System.Func<bool> condition, int maxMs)
        {
            for (int t = 0; t < maxMs && !condition(); t += 4)
            {
                rig.Tick(4);
            }

            Assert.That(condition(), Is.True, "condición del banco. " + MechanicTestKit.Describe(rig.Animator));
        }

        // Con el paso kind en curso en la cuia index, avanza el banco hasta la fracción fraction de su duración.
        private static void RunToFraction(MechanicTestKit.Rig rig, MechanicStepKind kind, int index, float fraction)
        {
            Assert.That(rig.Animator.Steps.TryGetRunning(kind, index, out float elapsed, out float duration), Is.True, kind + " en curso");
            int left = Mathf.FloorToInt((duration * fraction) - elapsed);
            while (left > 0)
            {
                int dt = Mathf.Min(left, PlayModeHelpers.FrameMs);
                rig.Tick(dt);
                left -= dt;
            }
        }

        // Lo más que se aleja alguna piraña de donde estaba, en ms de banco.
        private static float MaxFishTravel(MechanicTestKit.Rig rig, PiranaOverlay school, int ms)
        {
            var start = new Vector2[PiranaOverlay.SchoolSize];
            for (int i = 0; i < start.Length; i++)
            {
                start[i] = school.Fish(i).rectTransform.anchoredPosition;
            }

            float max = 0f;
            for (int t = 0; t < ms; t += PlayModeHelpers.FrameMs)
            {
                rig.Tick(PlayModeHelpers.FrameMs);
                for (int i = 0; i < start.Length; i++)
                {
                    max = Mathf.Max(max, (school.Fish(i).rectTransform.anchoredPosition - start[i]).magnitude);
                }
            }

            return max;
        }

        private static Rect WorldRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            float x0 = Mathf.Min(Mathf.Min(corners[0].x, corners[1].x), Mathf.Min(corners[2].x, corners[3].x));
            float x1 = Mathf.Max(Mathf.Max(corners[0].x, corners[1].x), Mathf.Max(corners[2].x, corners[3].x));
            float y0 = Mathf.Min(Mathf.Min(corners[0].y, corners[1].y), Mathf.Min(corners[2].y, corners[3].y));
            float y1 = Mathf.Max(Mathf.Max(corners[0].y, corners[1].y), Mathf.Max(corners[2].y, corners[3].y));
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        // Piezas de overlay a la vista bajo las cuatro anclas de la cuia.
        private static int VisibleGraphics(CuiaOverlayHost host)
        {
            return VisibleIn(host.SlotBack) + VisibleIn(host.BodyBack) + VisibleIn(host.BodyFront) + VisibleIn(host.SlotFront);
        }

        private static int VisibleIn(Transform anchor)
        {
            int count = 0;
            Graphic[] graphics = anchor.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i].enabled && graphics[i].gameObject.activeInHierarchy && graphics[i].color.a > 0.001f)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
