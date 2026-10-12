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
    /// (tinte 10 arriba), un fútil no cuenta, la tapa cae después de la gota, con la cuia llena la gota que ya colgaba intenta
    /// caer (baja y la tormenta late) y sigue colgando, y la última se lleva la nube sin pasar del pie de la fila de arriba. El
    /// cardumen sale de la cueva con el cuarto vertido útil, muerde y se lleva la dosis (a mitad del paso la víctima aún la
    /// tiene): el bocado sale de esa dosis, sube por el eje de la víctima y entra recto por la boca de la cueva antes de que se
    /// forme allí la dosis. Si el vertido sella la cuia, el mordisco se pierde y se ve (el cardumen llega a la tapa, choca y
    /// vuelve dentro del paso); con la cueva llena duerme y en el mordisco perdido se asoma y vuelve a dormirse antes de que
    /// acabe el paso. Con la víctima en la columna de la cueva, el cardumen va por el pasillo entre columnas, de ida, con el
    /// bocado y de vuelta, sin cortar el cuerpo de ninguna cuia (muestreado cada 10 ms en el 183). La gota tarda ≥ 150 ms ×
    /// factor en caer, la gotita sale de la nube en cuanto sale la gota, al llenar salpica, y la nube de la última gota se
    /// desvanece en ≥ 300 ms × factor. Los puntos de los contadores miden ≈ 7 dp y los vacíos van más tenues. El cardumen no
    /// tapa cuias ni captura toques, y nada asigna memoria durante los pasos.
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
            GameScreen.SimulatedScreen = null;
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
            Assert.That(cloud.CloudTopAboveMouthDp, Is.LessThanOrEqualTo(LluviaOverlay.CloudTopMaxDp + 0.01f), "la nube no pisa el marco del panel");

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
                AssertPipsLegible(cloud.Pips, ProceduralArt.Slate, 1f, LluviaOverlay.EmptyPipAlpha, "vertido " + i);
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
        public IEnumerator Lluvia_DropFalls150ms_LeavesTheRow_SplashesOnFill([Values(false, true)] bool reduce)
        {
            // Criterios de la ola 3 (prueba del 2026-10-09): la gota cae de la nube a la superficie en ≥ 150 ms [P] × factor, la
            // gotita de la nube se va en cuanto sale la gota (a mitad del paso ya no está) y al llenar salpica con el agua del
            // mundo; con Reducir movimiento todo va a × 0,5 y sin partículas.
            yield return null;
            BoardState start = DropBoard();
            _rig = new MechanicTestKit.Rig(Art, start);
            var fx = new RecordingFx(_rig.Fx) { ReduceMotion = reduce };
            _rig.Animator.Fx = fx;
            _rig.Animator.ReduceMotion = reduce;
            var cloud = (LluviaOverlay)_rig.Views[0].Overlay!;
            BoardState counted = _rig.Pour(start, 1, 3, out _);
            _rig.RunUntilIdle(3000);
            int pending = cloud.DropCount;
            Assert.That(pending, Is.EqualTo(2), "dos gotas pendientes");
            _rig.Animator.Steps.ClearLog();
            BoardState after = _rig.Pour(counted, 2, 4, out PourVisual v);
            Assert.That(v.Events.LluviaDropMask, Is.EqualTo(1), "cae una gota");
            RunUntil(_rig, () => _rig.Animator.Steps.IsRunning(MechanicStepKind.RainDrop, 0), 3000);
            Assert.That(cloud.DropCount, Is.EqualTo(pending - 1), "la gotita sale de la fila en cuanto sale la gota");
            fx.Clear();

            // El paso entero de 4 en 4 ms: cuándo empieza a bajar la gota, cuándo deja de verse y cuándo salpica.
            float formed = cloud.FallingDropHeightU;
            float fallFrom = -1f;
            float fallTo = -1f;
            float splashAt = -1f;
            float duration = 0f;
            bool midChecked = false;
            while (_rig.Animator.Steps.TryGetRunning(MechanicStepKind.RainDrop, 0, out float elapsed, out float dur))
            {
                duration = dur;
                _rig.Tick(4f);
                float t = elapsed + 4f;
                if (cloud.FallingDropVisible)
                {
                    if (fallFrom < 0f && cloud.FallingDropHeightU < formed - 0.005f)
                    {
                        fallFrom = t;
                    }

                    fallTo = t;
                }

                if (splashAt < 0f && fx.SplashCount > 0)
                {
                    splashAt = t;
                }

                if (!midChecked && t >= 0.5f * dur)
                {
                    midChecked = true;
                    Assert.That(cloud.FallingDropVisible, Is.True, "a mitad, la gota cae");
                    Assert.That(cloud.DropCount, Is.EqualTo(pending - 1), "a mitad, la gotita ya no está en la nube");
                }
            }

            MechanicStepRecord drop = MechanicTestKit.Record(_rig.Animator, MechanicStepKind.RainDrop, 0);
            float factor = drop.SpeedFactor;
            Assert.That(factor, Is.EqualTo(reduce ? 0.5f : 1f).Within(1e-3f));
            Assert.That(duration, Is.EqualTo(LluviaOverlay.DropStepMs * factor).Within(0.5f));
            Assert.That((LluviaOverlay.FallEndFraction - LluviaOverlay.FallStartFraction) * LluviaOverlay.DropStepMs, Is.GreaterThanOrEqualTo(150f), "caída nominal ≥ 150 ms [P]");
            Assert.That(fallFrom, Is.GreaterThan(0f), "la gota cae");
            Assert.That(fallTo - fallFrom + 4f, Is.GreaterThanOrEqualTo((150f * factor) - 4f), "la gota tarda ≥ 150 ms × factor en caer de la nube a la superficie");
            float fallStart = LluviaOverlay.FallStartFraction * duration;
            Assert.That(fallFrom, Is.InRange(fallStart - 8f, fallStart + 24f), "empieza a caer al terminar de formarse (cae con gravedad: al principio, despacio)");
            Assert.That(midChecked, Is.True);
            if (reduce)
            {
                Assert.That(fx.SplashCount, Is.EqualTo(0), "con Reducir movimiento, sin salpicadura");
            }
            else
            {
                Assert.That(splashAt, Is.GreaterThanOrEqualTo(LluviaOverlay.FallEndFraction * duration), "salpica al llenar, cuando la gota ya llegó");
                Assert.That(fx.LastSplashTint, Is.EqualTo(Palette.Tint(10)), "con el agua del mundo");
            }

            _rig.RunUntilIdle(3000);
            Assert.That(cloud.DropCount, Is.EqualTo(pending - 1), "queda una gota");
            AssertRigShowsModel(after);
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

            // La última gota cae y la nube sube, encoge y se desvanece; la tapa todavía no. En el 143 la Lluvia está en la fila
            // de arriba: lo alto de la nube nunca pasa del marco del panel (02 §5; revisión de T17).
            Assert.That(cloud.CloudTopLimitDp, Is.EqualTo(LluviaOverlay.CloudTopMaxDp).Within(1e-3f), "fila de arriba: el límite es el marco");
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.RainEnd, rain, 4000);
            float highest = 0f;
            while (boot.View.Animator.Steps.TryGetRunning(MechanicStepKind.RainEnd, rain, out float elapsedMs, out float durationMs) && elapsedMs < 0.85f * durationMs)
            {
                PlayModeHelpers.TickExact(boot, 4);
                highest = Mathf.Max(highest, cloud.CloudTopAboveMouthDp);
            }

            Assert.That(view.Sealed, Is.False, "la tapa no cae con la gota");
            Assert.That(cloud.Visible, Is.True, "la nube aún se ve mientras se va");
            Assert.That(cloud.CloudAlpha, Is.LessThan(0.8f), "la nube se desvanece");
            Assert.That(cloud.CloudRiseU, Is.GreaterThan(0.05f), "la nube sube");
            Assert.That(highest, Is.LessThanOrEqualTo(LluviaOverlay.CloudTopMaxDp + 0.01f), "sin pisar el marco del panel");

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
            Assert.That(cloud.HangingDropDipU, Is.EqualTo(0f).Within(1e-3f), "en reposo, en su sitio");
            Assert.That(cloud.BellyShade, Is.EqualTo(1f), "la tormenta se lee en la panza en sombra");
            float restDark = cloud.CloudDarkness;
            _rig.Animator.Steps.ClearLog();

            BoardState after = _rig.Pour(counted, 2, 4, out PourVisual v);
            Assert.That(v.Events.LluviaWaitingMask, Is.EqualTo(1), "la gota espera");
            RunUntil(_rig, () => _rig.Animator.Steps.IsRunning(MechanicStepKind.RainWait, 0), 3000);

            // Al empezar la espera, la gota que ya colgaba sigue entera (no se encoge para volver a asomar).
            RunToFraction(_rig, MechanicStepKind.RainWait, 0, 0.1f);
            Assert.That(cloud.IsAnimating, Is.True);
            Assert.That(cloud.HangingDrop, Is.True, "la gota sigue colgando");
            Assert.That(cloud.HangingDropScale, Is.EqualTo(1f).Within(1e-3f), "la que ya colgaba no desaparece al empezar la espera");

            // Hacia la mitad, la gota intenta caer (baja de su sitio) con un pulso más oscuro de la tormenta.
            RunToFraction(_rig, MechanicStepKind.RainWait, 0, 0.55f);
            Assert.That(cloud.HangingDropDipU, Is.GreaterThan(0.05f), "la gota baja hacia la boca llena");
            Assert.That(cloud.CloudDarkness, Is.GreaterThan(restDark + (0.5f * LluviaOverlay.StormPulse)), "el pulso de la tormenta");
            Assert.That(cloud.FilledPips, Is.EqualTo(2), "el vertido completó el ritmo");
            Assert.That(_rig.Views[0].LiquidLevel, Is.EqualTo(4f).Within(1e-3f), "nada cae");

            int elapsed = _rig.RunUntilIdle(3000);
            MechanicTestKit.AssertLogFollowsPlan(_rig.Animator, in v);
            AssertOwnStep(MechanicTestKit.Record(_rig.Animator, MechanicStepKind.RainWait, 0), MechanicType.Lluvia, LluviaOverlay.WaitStepMs);
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs));
            Assert.That(cloud.Stormy && cloud.HangingDrop, Is.True, "la gota queda colgando");
            Assert.That(cloud.HangingDropDipU, Is.EqualTo(0f).Within(1e-3f), "y vuelve a su sitio");
            Assert.That(cloud.CloudDarkness, Is.EqualTo(restDark).Within(1e-3f), "la tormenta, como en reposo");
            Assert.That(cloud.FilledPips, Is.EqualTo(1), "el contador se queda en k − 1");
            Assert.That(cloud.DropCount, Is.EqualTo(2), "siguen las dos gotas");
            AssertRigShowsModel(after);
        }

        [UnityTest]
        public IEnumerator Lluvia_EndRemovesCloud([Values(false, true)] bool lowerRow)
        {
            // La Lluvia [10, 10, 10] con su última gota (k = 2): la gota cae, la nube se va y la tapa (c′) llega después. Sin
            // fila encima (el banco es una fila) la nube no pasa del marco; una fila más abajo, sube sin llegar al pie de la de
            // arriba (02 §5; BoardLayout: elevación 16 + separación 24 = 40 dp de su pie a la boca).
            yield return null;
            BoardState start = Board(
                new[] { 4, 4, 4, 4, 4 },
                new[] { new[] { 10, 10, 10 }, new[] { 1, 2 }, new[] { 2, 1 }, new[] { 2, 2 }, new[] { 1, 1 } },
                new[] { new MechanicSpec(MechanicType.Lluvia, key: 10, n: 1, k: 2), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            _rig = new MechanicTestKit.Rig(Art, start);
            if (lowerRow)
            {
                PlaceRainOneRowLower(_rig);
            }

            var cloud = (LluviaOverlay)_rig.Views[0].Overlay!;
            float limit = lowerRow ? LluviaOverlay.RiseTopMaxDp : LluviaOverlay.CloudTopMaxDp;
            Assert.That(cloud.CloudTopLimitDp, Is.EqualTo(limit).Within(1e-3f), lowerRow ? "con una fila encima: su pie" : "sin fila encima: el marco");
            BoardState counted = _rig.Pour(start, 1, 3, out _);
            _rig.RunUntilIdle(3000);
            _rig.Animator.Steps.ClearLog();

            BoardState after = _rig.Pour(counted, 2, 4, out PourVisual v);
            Assert.That(v.Events.LluviaEndedMask, Is.EqualTo(1), "la última gota");
            Assert.That(after[0].Mechanic, Is.EqualTo(MechanicType.None));
            Assert.That(cloud.CloudTopAboveMouthDp, Is.LessThanOrEqualTo(LluviaOverlay.CloudTopMaxDp + 0.01f), "en reposo, lejos del marco");
            RunUntil(_rig, () => _rig.Animator.Steps.IsRunning(MechanicStepKind.RainEnd, 0), 3000);
            RunToFraction(_rig, MechanicStepKind.RainEnd, 0, 0.5f);
            Assert.That(cloud.FallingDropVisible, Is.True, "la gota cae");
            Assert.That(cloud.CloudRiseU, Is.GreaterThan(0f), "la nube empieza a subir");

            // Al irse, lo alto de la nube no pasa de su límite: el marco o el pie de la fila de arriba (02 §5).
            float highest = 0f;
            float rose = 0f;
            while (_rig.Animator.Steps.TryGetRunning(MechanicStepKind.RainEnd, 0, out float elapsedMs, out float durationMs) && elapsedMs < 0.9f * durationMs)
            {
                _rig.Tick(4);
                if (cloud.CloudAlpha > 0.01f)
                {
                    highest = Mathf.Max(highest, cloud.CloudTopAboveMouthDp);
                    rose = Mathf.Max(rose, cloud.CloudRiseU);
                }
            }

            Assert.That(highest, Is.LessThanOrEqualTo(limit + 0.01f), "la nube que se va no pisa el marco ni tapa la fila de arriba");
            if (lowerRow)
            {
                Assert.That(rose, Is.GreaterThan(0.5f), "con sitio encima, sube de verdad");
            }

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

            // Duración del fundido (criterio de la ola 3: la nube sube y se desvanece en ≥ 300 ms [P] × factor, y llega a ~0 al
            // final): el mismo vertido otra vez, en la misma fila que arriba (lowerRow), mirando la opacidad de la nube de 4 en 4
            // ms. Entera hasta que cae la gota, todavía > 0,5 a los 150 ms × factor de fundido, desde que empieza a desvanecerse
            // hasta que no queda nada ≥ 300 ms × factor, y en el último fotograma del paso (aún animando) ya casi transparente: un
            // fundido que se quedara a medias y se cortara de golpe al cerrar el paso no pasa.
            _rig.Dispose();
            _rig = new MechanicTestKit.Rig(Art, start);
            if (lowerRow)
            {
                PlaceRainOneRowLower(_rig);
            }

            cloud = (LluviaOverlay)_rig.Views[0].Overlay!;
            Assert.That(cloud.CloudTopLimitDp, Is.EqualTo(limit).Within(1e-3f), "el fundido se mide en la misma fila");
            counted = _rig.Pour(start, 1, 3, out _);
            _rig.RunUntilIdle(3000);
            _rig.Pour(counted, 2, 4, out _);
            RunUntil(_rig, () => _rig.Animator.Steps.IsRunning(MechanicStepKind.RainEnd, 0), 3000);
            Assert.That(_rig.Animator.Steps.TryGetRunning(MechanicStepKind.RainEnd, 0, out _, out float fadeDur), Is.True);
            float factor = fadeDur / LluviaOverlay.DropStepMs;
            float fadeFrom = LluviaOverlay.FallStartFraction * fadeDur;
            float lastWhole = -1f;
            float firstGone = -1f;
            float halfway = -1f;
            float lastAnimatedAlpha = -1f;
            float lastAnimatedAt = -1f;
            while (_rig.Animator.Steps.TryGetRunning(MechanicStepKind.RainEnd, 0, out float e, out _))
            {
                _rig.Tick(4);
                float t = e + 4f;
                float a = cloud.IsAnimating ? cloud.CloudAlpha : 0f; // al cerrar el paso la nube oculta vuelve a alfa 1
                if (cloud.IsAnimating)
                {
                    lastAnimatedAlpha = cloud.CloudAlpha;
                    lastAnimatedAt = t;
                }

                if (a >= 0.999f)
                {
                    lastWhole = t;
                }

                if (firstGone < 0f && a <= 0.005f)
                {
                    firstGone = t;
                }

                if (halfway < 0f && t >= fadeFrom + (150f * factor))
                {
                    halfway = a;
                }
            }

            Assert.That(lastWhole, Is.GreaterThanOrEqualTo(fadeFrom - 8f), "la nube sigue entera hasta que cae la gota");
            Assert.That(halfway, Is.GreaterThan(0.5f), "a los 150 ms × factor de fundido aún se ve más de la mitad");
            Assert.That(lastAnimatedAt, Is.GreaterThanOrEqualTo(fadeDur - 8f), "se mide hasta el último fotograma del paso");
            Assert.That(lastAnimatedAlpha, Is.LessThanOrEqualTo(0.05f), "al final del paso, aún animando, la nube ya es casi transparente (" + lastAnimatedAlpha.ToString("F3") + "): sin corte brusco");
            Assert.That(firstGone, Is.GreaterThan(0f), "y al final se ha ido");
            Assert.That(firstGone - lastWhole, Is.GreaterThanOrEqualTo((300f * factor) - 8f), "se desvanece en ≥ 300 ms × factor (" + (firstGone - lastWhole).ToString("F0") + " ms)");
        }

        // La Lluvia (cuia 0) una fila más abajo: su pie, elevación 16 + separación 24 = 40 dp bajo la boca de las demás, que hacen
        // de fila de arriba (BoardLayout; 02 §5).
        private static void PlaceRainOneRowLower(MechanicTestKit.Rig rig)
        {
            CuiaView rainView = rig.Views[0];
            Vector2 foot = new Vector2(36f, -600f - 40f - (rainView.HeightU * rainView.Unit));
            rainView.SetGeometry(foot, 40f, 60f);
            rig.Animator.BoardLaidOut();
        }

        [UnityTest]
        public IEnumerator Pirana183_BiteFromCave()
        {
            yield return MechanicTestKit.LoadPlaying(183);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            int cave = MechanicTestKit.IndexOf(flow, MechanicType.Pirana);
            var school = (PiranaOverlay)boot.View!.Views[cave].Overlay!;
            Assert.That(flow.Session.Current[cave].MechanicK, Is.EqualTo(4), "183: un mordisco cada 4 vertidos útiles");
            Assert.That(school.PipCount, Is.EqualTo(4));
            Assert.That(school.FilledPips, Is.EqualTo(0));

            // Vertidos hasta el primer mordisco; se juegan uno a uno contando los útiles (Q18 B) y mirando los puntos: antes
            // del último, ni mordisco ni mordisco perdido.
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.PiranaCaveMask & (1 << cave)) != 0);
            int useful = 0;
            for (int i = 0; i < path.Length - 1; i++)
            {
                boot.View.Animator.Steps.ClearLog();
                MechanicTestKit.TapPour(boot, path[i], out TapResult prior);
                useful += prior.Pour.Events.Futile ? 0 : 1;
                MechanicTestKit.StepToIdle(boot, 6000);
                MechanicTestKit.AssertViewMatchesModel(boot);
                Assert.That(school.FilledPips, Is.EqualTo(flow.Session.Current[cave].MechanicCounter), "vertido " + i + ": los puntos siguen al contador");
                Assert.That(MechanicTestKit.TryFindRecord(boot.View.Animator, MechanicStepKind.Bite, cave, out _), Is.False, "vertido " + i + ": aún no muerde");
                Assert.That(MechanicTestKit.TryFindRecord(boot.View.Animator, MechanicStepKind.BiteLost, cave, out _), Is.False, "vertido " + i + ": ni pierde un mordisco");
                AssertPipsLegible(school.Pips, ProceduralArt.Pearl, school.Dimmed ? PiranaOverlay.DimAlpha : 1f, PiranaOverlay.EmptyPipAlpha, "vertido " + i);
            }

            boot.View.Animator.Steps.ClearLog();
            Vector2 home = school.SchoolHome;
            int caveBefore = flow.Session.Current[cave].Count;
            PourVisual visual = MechanicTestKit.TapPour(boot, path[path.Length - 1], out TapResult poured);
            Assert.That(poured.Pour.Events.Futile, Is.False);
            useful++;
            Assert.That(useful, Is.EqualTo(4), "el cardumen sale con el cuarto vertido útil");
            int victim = poured.Pour.Receiver;
            int bitten = flow.Session.Current[cave].TopColor;
            float tapped = boot.View.Animator.Clock;
            float u = boot.View.Views[cave].Unit;

            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Bite, cave, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Bite, cave, 0.3f);
            Assert.That(school.IsAnimating, Is.True, "el mordisco lo anima la cueva");
            Assert.That(school.FilledPips, Is.EqualTo(4), "el vertido que completa el ritmo llena los puntos");
            Assert.That((school.SchoolCenter - home).magnitude, Is.GreaterThan(0.5f * u), "el cardumen sale de la cueva");
            Assert.That(school.MorselVisible, Is.False, "aún no ha mordido");
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Bite, cave, 0.75f);
            Assert.That(school.MorselVisible, Is.True, "se lleva el bocado");
            Assert.That(school.MorselScale, Is.EqualTo(1f).Within(1e-3f), "el bocado entero, en la boca de la que muerde");
            Assert.That(school.MorselPatterned, Is.EqualTo(ProceduralArt.HasPattern(bitten)), "con el patrón de la fruta (07 §3)");
            Assert.That(school.MorselRim.color, Is.EqualTo(new Color(Palette.GlassRim.r, Palette.GlassRim.g, Palette.GlassRim.b, 0.9f)), "contorno de vidrio: se lee con cualquier tinte");
            Assert.That(school.FilledPips, Is.EqualTo(0), "con el mordisco, el contador vuelve a empezar");

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

            // El paso entero, de 4 en 4 ms. A mitad, la víctima aún tiene la dosis y el bocado ya sale de ella. Antes y después: el
            // bocado sale de la dosis de arriba de la víctima y sube por su eje; en la cueva se suelta sobre su eje y entra recto
            // por la boca, sin pasar nunca por debajo del labio, y la dosis de la cueva empieza a formarse cuando el bocado ya
            // llega al labio (causa → efecto).
            float u = views[cave].Unit;
            Vector2 victimFoot = boot.View.Animator.Foot(victim);
            Vector2 victimMouth = victimFoot + new Vector2(0f, views[victim].HeightU * u);
            Vector2 caveFoot = boot.View.Animator.Foot(cave);
            float doseTop = victimFoot.y + ((CuiaGeometry.CavityBottomU + victimAfter + 1f) * u);
            float caveLip = caveFoot.y + (views[cave].HeightU * u);
            bool mid = false;
            bool seen = false;
            bool overCave = false;
            bool caveFilling = false;
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Bite, cave, 4000);
            while (boot.View.Animator.Steps.TryGetRunning(MechanicStepKind.Bite, cave, out float elapsedMs, out float durationMs))
            {
                if (!mid && elapsedMs >= 0.5f * durationMs)
                {
                    mid = true;
                    Assert.That(views[victim].DoseFill(victimAfter), Is.GreaterThan(0.99f), "a mitad del paso la víctima aún tiene la dosis");
                    Assert.That(views[victim].DoseColor(victimAfter), Is.EqualTo(Palette.Tint(bitten)), "la fruta que se va a llevar");
                    Assert.That(views[victim].LiquidLevel, Is.EqualTo(victimAfter + 1f).Within(1e-3f));
                    Assert.That(views[cave].LiquidLevel, Is.EqualTo((float)caveBefore).Within(1e-3f), "la cueva aún no la tiene");
                    Assert.That((school.SchoolCenter - victimMouth).magnitude, Is.LessThan((school.SchoolHome - victimMouth).magnitude), "el cardumen está en la víctima");
                    Assert.That(school.MorselVisible, Is.True, "el bocado ya sale de la dosis");
                    Assert.That(school.MorselCenter.y, Is.LessThan(victimMouth.y), "aún dentro de la víctima, por debajo de su boca");
                }

                PlayModeHelpers.TickExact(boot, 4);
                caveFilling |= views[cave].DoseFill(caveBefore) > 0.01f;
                if (!school.MorselVisible)
                {
                    Assert.That(seen || !caveFilling, Is.True, "la cueva no recibe la dosis antes de que salga el bocado");
                    continue;
                }

                Vector2 m = school.MorselCenter;
                if (!seen)
                {
                    seen = true;
                    Assert.That(Mathf.Abs(m.x - victimFoot.x), Is.LessThan(0.3f * u), "el bocado sale en el eje de la víctima");
                    Assert.That(m.y, Is.LessThan(doseTop), "y de su dosis de arriba, no del aire");
                    Assert.That(school.MorselScale, Is.LessThan(0.6f), "pequeño al salir");
                }

                if (school.MorselScale < 0.85f)
                {
                    Assert.That(Mathf.Abs(m.x - victimFoot.x), Is.LessThan(0.35f * u), "sube por el eje de la víctima");
                }

                var caveMouth = new Vector2(caveFoot.x, caveLip);
                bool nearCave = Mathf.Abs(m.x - caveFoot.x) < 1.2f * u && m.y < caveLip + (0.6f * u)
                    && (m - caveMouth).sqrMagnitude < (m - victimMouth).sqrMagnitude;
                if (nearCave)
                {
                    Assert.That(m.y, Is.GreaterThanOrEqualTo(caveLip - (0.02f * u)), "el bocado nunca pasa por debajo del labio de la cueva");
                    overCave |= Mathf.Abs(m.x - caveFoot.x) < 0.1f * u;
                }

                if (caveFilling)
                {
                    Assert.That(school.MorselAlpha, Is.LessThan(0.4f), "la dosis de la cueva se forma cuando el bocado entra por la boca");
                }
            }

            Assert.That(mid, Is.True, "se miró a mitad del paso");
            Assert.That(seen, Is.True, "se vio el bocado");
            Assert.That(overCave, Is.True, "el bocado entra por la boca de la cueva, sobre su eje");
            MechanicTestKit.StepToIdle(boot, 6000);
            Assert.That(views[victim].LiquidLevel, Is.EqualTo((float)victimAfter).Within(1e-3f));
            Assert.That(views[cave].LiquidLevel, Is.EqualTo(caveBefore + 1f).Within(1e-3f));
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

            // Mordisco fallido visible (decisión C + D del dueño para el 183): el cardumen sale de la cueva, la que va delante llega
            // a la tapa de la víctima, choca, rebota y vuelve. A mitad del paso está junto a la boca de la víctima.
            float u = boot.View.Views[cave].Unit;
            Vector2 home = school.SchoolHome;
            Vector2 victimFoot = boot.View.Animator.Foot(victim);
            Vector2 lid = victimFoot + new Vector2(0f, (boot.View.Views[victim].HeightU + PiranaOverlay.LidAboveRimU) * u);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.BiteLost, cave, 4000);
            Assert.That(Vector2.Distance(school.LostContact, lid), Is.LessThan(0.01f), "choca con la tapa de la víctima");
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.BiteLost, cave, 0.25f);
            Assert.That((school.SchoolCenter - home).magnitude, Is.GreaterThan(0.5f * u), "el cardumen sale de la cueva");
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.BiteLost, cave, PiranaOverlay.LostHitAt - 0.04f);
            Assert.That(school.LostBumped, Is.False, "aún no ha chocado");
            for (int ms = 0; ms < 60 && !school.LostBumped; ms++)
            {
                PlayModeHelpers.TickExact(boot, 1);
            }

            Assert.That(school.LostBumped, Is.True, "choca");
            Assert.That(Vector2.Distance(school.LeadMouth, lid), Is.LessThan(0.2f * u), "la que va delante llega a la tapa");
            Assert.That(school.LeadMouth.y, Is.LessThan(lid.y - 0.5f), "y la toca: su boca se hunde en la tapa (" + (lid.y - school.LeadMouth.y).ToString("F1") + " dp)");

            // El golpe se lee en 400 ms (revisión de la tanda B2, ronda 2): a LostHitAt + 0,03 la que va delante sigue contra
            // la tapa, aplastada a lo largo de su cuerpo, con el destello perla en su punto máximo (su estrella, ≥ 16 dp de
            // lado: el cuerpo de la estrella, ≥ 8 dp).
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.BiteLost, cave, PiranaOverlay.LostHitAt + 0.03f);
            Assert.That(school.ImpactFlashSize, Is.GreaterThanOrEqualTo(16f), "el destello del golpe, en su punto máximo");
            Vector3 squashed = school.Fish(PiranaOverlay.LeadFish).rectTransform.localScale;
            Assert.That(Mathf.Abs(squashed.x), Is.LessThan(0.9f * Mathf.Abs(squashed.y)), "aplastada por el golpe");
            Assert.That(school.LeadMouth.y, Is.LessThan(lid.y - 0.5f), "aún contra la tapa");
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.BiteLost, cave, 0.5f);
            Assert.That(school.IsAnimating, Is.True, "el mordisco perdido lo anima la cueva");
            Assert.That(Vector2.Distance(school.LeadMouth, lid), Is.LessThan(0.6f * u), "a mitad del paso, junto a la boca de la víctima (rebotando)");
            Assert.That(school.LeadMouth.y, Is.GreaterThan(lid.y - PiranaOverlay.LostDentDp - 0.5f), "no entra en la víctima más que el golpe (LostDentDp)");
            Assert.That((school.SchoolCenter - lid).magnitude, Is.LessThan((home - lid).magnitude), "el cardumen está en la víctima");
            Assert.That(school.MorselVisible, Is.False, "no se lleva nada");
            Assert.That(boot.View.Views[cave].LiquidLevel, Is.EqualTo((float)caveBefore.Count).Within(1e-3f));
            Assert.That(boot.View.Views[victim].Sealed, Is.True, "la tapa, en su sitio");

            // En el 183 la víctima está en la primera columna (revisión de la tanda B2, ronda 2: la que esperaba a la izquierda
            // de la tapa rozaba el marco): de aquí al final del paso, ninguna piraña sale del canto interior del marco del panel
            // (≥ 2 dp), y el destello ya se apagó al final. Y vuelve DENTRO del paso (revisión de la tanda B2, ronda 3: al
            // cerrarlo, FinishStep pone el cardumen en su ronda pase lo que pase): a p ≈ 0,95 ya está junto a la cueva, y del
            // último fotograma del paso al primero sin él la que va delante no salta.
            Rect inner = MechanicTestKit.PanelInner(boot.View);
            RectTransform lead = school.Fish(PiranaOverlay.LeadFish).rectTransform;
            Vector2 lastInStep = lead.anchoredPosition;
            bool late = false;
            int frames = 0;
            while (boot.View.Animator.Steps.TryGetRunning(MechanicStepKind.BiteLost, cave, out float e, out float d))
            {
                for (int f = 0; f < PiranaOverlay.SchoolSize; f++)
                {
                    Rect fish = school.FishLayerRect(f);
                    Assert.That(
                        fish.xMin >= inner.xMin + 2f && fish.xMax <= inner.xMax - 2f && fish.yMax <= inner.yMax - 2f,
                        Is.True,
                        "la piraña " + f + " " + fish + " dentro del panel " + inner + " con ≥ 2 dp");
                }

                lastInStep = lead.anchoredPosition;
                if (!late && e >= 0.95f * d)
                {
                    late = true;
                    Assert.That(school.IsAnimating, Is.True, "aún dentro del paso");
                    Assert.That(
                        (school.SchoolCenter - school.SchoolHome).magnitude,
                        Is.LessThan(0.8f * u),
                        "a " + (e / d).ToString("F2") + " del paso, de vuelta junto a la cueva (" + ((school.SchoolCenter - school.SchoolHome).magnitude / u).ToString("F2") + " u)");
                }

                frames++;
                PlayModeHelpers.TickExact(boot, 4);
            }

            Assert.That(frames, Is.GreaterThan(10), "se miró la segunda mitad del paso");
            Assert.That(late, Is.True, "se miró el final del paso");
            Assert.That(Vector2.Distance(lead.anchoredPosition, lastInStep), Is.LessThan(0.1f * u), "al cerrar el paso no salta");
            Assert.That(school.ImpactFlashSize, Is.EqualTo(0f), "el destello ya se apagó");
            MechanicTestKit.StepToIdle(boot, 6000);
            Assert.That((school.SchoolCenter - school.SchoolHome).magnitude, Is.LessThan(0.8f * u), "y sigue junto a la cueva");
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
        public IEnumerator Pirana183_LostSwimsBesideVictimInCaveColumn()
        {
            // Revisión de la tanda B2, ronda 3 (captura 83g): con la víctima en la columna de la cueva (la cuia 4, sellada, justo
            // encima de la cueva 8 en el teléfono, 360 × 780 dp: 3 filas de 4), el arco iba recto por el eje de la víctima y el
            // cardumen se veía dentro del jugo de una cuia sellada. Ahora va por el pasillo entre columnas: muestreado cada 10 ms,
            // ninguna piraña corta el cuerpo de ninguna cuia entre su pie y su labio; sube (y baja) junto a la víctima, a su lado,
            // y llega a su tapa por un lado.
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            yield return MechanicTestKit.LoadPlaying(183);
            GameBootstrap boot = GameBootstrap.Current!;
            BoardAnimator animator = boot.View!.Animator;
            CuiaView[] views = boot.View.Views;
            int cave = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Pirana);
            const int victim = 4;
            float u = views[cave].Unit;
            Vector2 caveFoot = animator.Foot(cave);
            Vector2 victimFoot = animator.Foot(victim);
            Assert.That(boot.View.Layout!.Rows, Is.EqualTo(3), "12 cuias: 3 filas de 4 a 360 × 780");
            Assert.That(Mathf.Abs(victimFoot.x - caveFoot.x), Is.LessThan(0.5f * u), "la cuia 4, en la columna de la cueva");
            Assert.That(victimFoot.y, Is.GreaterThan(caveFoot.y), "y encima de ella");
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.PiranaBiteLostMask & (1 << cave)) != 0 && ev.Receiver == victim && (ev.SealedMask & (1 << victim)) != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            var school = (PiranaOverlay)views[cave].Overlay!;
            PourVisual visual = MechanicTestKit.TapPour(boot, path[path.Length - 1], out TapResult poured);
            Assert.That(poured.Pour.Receiver, Is.EqualTo(victim));
            Assert.That(boot.Flow!.Session.Current.IsSealed(victim), Is.True, "la víctima, sellada");

            // El cuerpo de cada cuia, de su pie a su labio, con su ancho exterior (2 u).
            Rect[] bodies = BodyRects(animator, views);
            Rect victimBody = bodies[victim];
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.BiteLost, cave, 4000);
            Assert.That(school.Detouring, Is.True, "por el pasillo entre columnas");
            Assert.That(Mathf.Abs(school.DetourCorridorX - victimFoot.x), Is.GreaterThan(0.5f * victimBody.width), "el pasillo, fuera del cuerpo de la víctima");
            int samples = 0;
            bool besideUp = false;
            bool besideDown = false;
            bool bumped = false;
            while (animator.Steps.TryGetRunning(MechanicStepKind.BiteLost, cave, out float e, out float d))
            {
                string when = " a " + Mathf.RoundToInt(e) + " de " + Mathf.RoundToInt(d) + " ms";
                AssertFishOutsideBodies(school, bodies, -1, when);
                Vector2 center = school.SchoolCenter;
                if (center.y > victimBody.yMin && center.y < victimBody.yMax && Mathf.Abs(center.x - victimFoot.x) > 0.5f * victimBody.width)
                {
                    besideUp |= e < 0.5f * d;
                    besideDown |= e >= 0.5f * d;
                }

                bumped |= school.LostBumped;
                samples++;
                PlayModeHelpers.TickExact(boot, 10);
            }

            Assert.That(samples, Is.GreaterThanOrEqualTo(30), "se muestreó el paso entero cada 10 ms");
            Assert.That(besideUp, Is.True, "de ida, sube junto a la víctima, por su lado");
            Assert.That(besideDown, Is.True, "y de vuelta, baja por su lado");
            Assert.That(bumped, Is.True, "y llega a la tapa: choca");
            MechanicTestKit.StepToIdle(boot, 6000);
            MechanicTestKit.AssertLogFollowsPlan(animator, in visual);
            AssertOwnStep(MechanicTestKit.Record(animator, MechanicStepKind.BiteLost, cave), MechanicType.Pirana, PiranaOverlay.BiteLostStepMs);
            MechanicTestKit.AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator Pirana_CaveUnderVictim_SwimsBesideIt([Values(false, true)] bool sealedVictim)
        {
            // El mordisco y el mordisco perdido contra la tapa con la cueva justo debajo de la víctima (revisión de la tanda B2,
            // ronda 3): de ida, con el bocado y de vuelta, el cardumen va por el pasillo entre columnas, estrechado para caber
            // (aquí el pasillo es más estrecho que en el 183); ninguna piraña corta el cuerpo de una cuia entre su pie y su labio
            // (salvo la que muerde mientras mete el morro en la boca de la víctima), y el paso dura lo suyo.
            yield return null;
            BoardState start = sealedVictim ? SealedVictimBoard() : BiteBoard();
            int victim = sealedVictim ? 1 : 3;
            MechanicStepKind kind = sealedVictim ? MechanicStepKind.BiteLost : MechanicStepKind.Bite;
            _rig = new MechanicTestKit.Rig(Art, start);
            PlaceCaveUnder(_rig, victim);
            var school = (PiranaOverlay)_rig.Views[0].Overlay!;
            BoardState counted = _rig.Pour(start, sealedVictim ? 3 : 1, 4, out _);
            _rig.RunUntilIdle(3000);
            _rig.Animator.Steps.ClearLog();
            BoardState after = _rig.Pour(counted, 2, victim, out PourVisual v);
            Assert.That(sealedVictim ? v.Events.PiranaBiteLostMask : v.Events.PiranaCaveMask, Is.EqualTo(1), sealedVictim ? "mordisco perdido contra la tapa" : "mordisco");
            Assert.That(after.IsSealed(victim), Is.EqualTo(sealedVictim));

            Rect[] bodies = BodyRects(_rig.Animator, _rig.Views);
            Vector2 victimFoot = _rig.Animator.Foot(victim);
            Rect victimBody = bodies[victim];
            RunUntil(_rig, () => _rig.Animator.Steps.IsRunning(kind, 0), 3000);
            Assert.That(school.Detouring, Is.True, "por el pasillo entre columnas");
            bool beside = false;
            bool carried = false;
            while (_rig.Animator.Steps.TryGetRunning(kind, 0, out float e, out float d))
            {
                float p = e / d;
                string when = " a " + p.ToString("F2") + " del paso";
                bool biting = !sealedVictim && p >= PiranaOverlay.BiteReachEnd - 0.02f && p <= PiranaOverlay.BiteCarryStart + 0.02f;
                AssertFishOutsideBodies(school, bodies, biting ? PiranaOverlay.LeadFish : -1, when);
                Vector2 center = school.SchoolCenter;
                beside |= center.y > victimBody.yMin && center.y < victimBody.yMax && Mathf.Abs(center.x - victimFoot.x) > 0.5f * victimBody.width;

                // El bocado, en la boca de la que va delante, tampoco corta ninguna cuia mientras se lo lleva (después entra recto
                // por la boca de la cueva, a propósito).
                if (!sealedVictim && school.MorselVisible && p >= PiranaOverlay.BiteCarryStart + 0.02f && p <= PiranaOverlay.BiteCarryEnd - 0.01f)
                {
                    carried = true;
                    float mu = _rig.Views[0].Unit * school.MorselScale;
                    var size = new Vector2(ProceduralArt.PiranaMorselWidthU * mu, ProceduralArt.PiranaMorselHeightU * mu);
                    var morsel = new Rect(school.MorselCenter - (0.5f * size), size);
                    for (int c = 0; c < bodies.Length; c++)
                    {
                        Assert.That(morsel.Overlaps(bodies[c]), Is.False, "el bocado " + morsel + " no corta el cuerpo de la cuia " + c + " " + bodies[c] + when);
                    }
                }

                _rig.Tick(4);
            }

            Assert.That(beside, Is.True, "sube junto a la víctima, por el pasillo");
            Assert.That(carried, Is.EqualTo(!sealedVictim), "se miró el bocado mientras se lo lleva");
            _rig.RunUntilIdle(3000);
            float nominal = sealedVictim ? PiranaOverlay.BiteLostStepMs : PiranaOverlay.BiteStepMs;
            AssertOwnStep(MechanicTestKit.Record(_rig.Animator, kind, 0), MechanicType.Pirana, nominal);
            AssertRigShowsModel(after);
        }

        // El cuerpo de cada cuia en el espacio de las capas de tablero: de su pie a su labio de vertido, con su ancho exterior.
        private static Rect[] BodyRects(BoardAnimator animator, CuiaView[] views)
        {
            var bodies = new Rect[views.Length];
            for (int c = 0; c < views.Length; c++)
            {
                Vector2 f = animator.Foot(c);
                float uc = views[c].Unit;
                float half = CuiaGeometry.BodyOuterHalfWidthU * uc;
                bodies[c] = Rect.MinMaxRect(f.x - half, f.y, f.x + half, f.y + ((views[c].HeightU - CuiaGeometry.RimU) * uc));
            }

            return bodies;
        }

        // Ninguna piraña (salvo skip) corta el cuerpo de ninguna cuia.
        private static void AssertFishOutsideBodies(PiranaOverlay school, Rect[] bodies, int skip, string when)
        {
            for (int f = 0; f < PiranaOverlay.SchoolSize; f++)
            {
                if (f == skip)
                {
                    continue; // la que muerde mete el morro en la boca de la víctima: es el mordisco
                }

                Rect fish = school.FishLayerRect(f);
                for (int c = 0; c < bodies.Length; c++)
                {
                    Assert.That(fish.Overlaps(bodies[c]), Is.False, "la piraña " + f + " " + fish + " no corta el cuerpo de la cuia " + c + " " + bodies[c] + when);
                }
            }
        }

        [UnityTest]
        public IEnumerator Pirana_BiteLost_QC([Values(false, true)] bool victimAbove)
        {
            // Cueva llena [3, 4] (k = 2): el segundo vertido útil completa el ritmo y el mordisco se pierde (respuesta Q-C). Con
            // victimAbove, la cueva va una fila más abajo, justo debajo de la víctima (como en el 183, donde la cueva tiene dos
            // víctimas posibles encima, en su columna): la que va delante se asoma hacia arriba (revisión de la tanda B2, ronda 2).
            yield return null;
            BoardState start = CaveFullBoard();
            _rig = new MechanicTestKit.Rig(Art, start);
            if (victimAbove)
            {
                PlaceCaveUnder(_rig, 5);
            }

            var school = (PiranaOverlay)_rig.Views[0].Overlay!;
            Assert.That(school.Dimmed, Is.True, "cueva llena: el cardumen duerme");
            BoardState counted = _rig.Pour(start, 4, 5, out _);
            _rig.RunUntilIdle(3000);
            _rig.Animator.Steps.ClearLog();

            BoardState after = _rig.Pour(counted, 3, 5, out PourVisual v);
            Assert.That(v.Events.PiranaBiteLostMask, Is.EqualTo(1), "mordisco perdido");
            Assert.That(v.Events.PiranaCaveMask, Is.EqualTo(0));
            float u = _rig.Views[0].Unit;
            Vector2 rest = school.Fish(PiranaOverlay.LeadFish).rectTransform.anchoredPosition;
            Assert.That(school.SchoolAsleep, Is.True, "antes del paso, dormido");
            RunUntil(_rig, () => _rig.Animator.Steps.IsRunning(MechanicStepKind.BiteLost, 0), 3000);

            // Cueva llena: se despierta, la que va delante se asoma hacia la víctima (a la derecha, o hacia arriba si está encima)
            // y vuelve, y se duermen otra vez.
            float shelfAbove = _rig.Animator.Foot(5).y - RemolinoOverlay.ShelfBottomBelowFootDp;
            float highest = float.NegativeInfinity;
            while (_rig.Animator.Steps.TryGetRunning(MechanicStepKind.BiteLost, 0, out float e, out float d) && e < 0.5f * d)
            {
                _rig.Tick(4);
                highest = Mathf.Max(highest, school.FishLayerRect(PiranaOverlay.LeadFish).yMax);
            }

            Assert.That(school.IsAnimating, Is.True);
            Assert.That(school.SchoolAsleep, Is.False, "se despierta para asomarse");
            Vector2 peek = school.Fish(PiranaOverlay.LeadFish).rectTransform.anchoredPosition;
            Assert.That(school.PeekUp, Is.EqualTo(victimAbove), victimAbove ? "víctima encima: se asoma hacia arriba" : "víctima al lado: se asoma hacia su lado");
            if (victimAbove)
            {
                Assert.That(peek.y - rest.y, Is.GreaterThan(0.3f * u), "sube hacia la víctima (" + ((peek.y - rest.y) / u).ToString("F2") + " u)");
                Assert.That(Mathf.Abs(peek.x - rest.x), Is.LessThan(0.1f * u), "en su columna");
                Assert.That(school.LeadMouth.y, Is.GreaterThan(school.FishLayerRect(PiranaOverlay.LeadFish).center.y), "con el morro arriba");
                Assert.That(highest, Is.LessThanOrEqualTo(shelfAbove - 1f), "sin pasar de la repisa de la fila de arriba (" + (shelfAbove - highest).ToString("F1") + " dp de aire)");
            }
            else
            {
                Assert.That(peek.x - rest.x, Is.GreaterThan(0.4f * u), "se asoma hacia la víctima");
            }

            Assert.That(school.Fish(PiranaOverlay.LeadFish).color.a, Is.EqualTo(1f).Within(1e-3f), "despierta, sin atenuar");
            Assert.That(school.MorselVisible, Is.False);
            Assert.That(_rig.Views[0].LiquidLevel, Is.EqualTo(2f).Within(1e-3f), "la cueva no cambia");
            Assert.That(_rig.Views[5].LiquidLevel, Is.EqualTo(2f).Within(1e-3f), "la víctima no cambia");

            // Que vuelva y se duerma DENTRO del paso (revisión de la tanda B2, ronda 3): al cerrarlo, FinishStep deja las pirañas
            // en su sitio y dormidas pase lo que pase, así que mirarlo después no prueba nada. A e ≥ 0,95·d, aún animando: los
            // ojos cerrados (desde PeekAsleepAt), la que va delante de vuelta en su sitio, atenuada (DimAlpha) y encogida
            // (DimScale); y del último fotograma del paso al primero sin él, sin saltos.
            Image lead = school.Fish(PiranaOverlay.LeadFish);
            Vector2 lastInStep = lead.rectTransform.anchoredPosition;
            bool late = false;
            while (_rig.Animator.Steps.TryGetRunning(MechanicStepKind.BiteLost, 0, out float e, out float d))
            {
                lastInStep = lead.rectTransform.anchoredPosition;
                if (!late && e >= 0.95f * d)
                {
                    late = true;
                    string when = " (a " + (e / d).ToString("F2") + " del paso)";
                    Assert.That(school.IsAnimating, Is.True, "aún dentro del paso" + when);
                    Assert.That(school.SchoolAsleep, Is.True, "ya se ha vuelto a dormir" + when);
                    Assert.That(Vector2.Distance(lastInStep, rest), Is.LessThan(0.1f * u), "la que va delante, de vuelta en su sitio" + when);
                    Assert.That(lead.color.a, Is.EqualTo(PiranaOverlay.DimAlpha).Within(0.01f), "atenuada otra vez" + when);
                    Assert.That(Mathf.Abs(lead.rectTransform.localScale.y), Is.EqualTo(PiranaOverlay.DimScale).Within(0.01f), "encogida otra vez" + when);
                }

                _rig.Tick(4);
            }

            Assert.That(late, Is.True, "se miró el final del paso");
            Assert.That(Vector2.Distance(lead.rectTransform.anchoredPosition, lastInStep), Is.LessThan(0.1f * u), "al cerrar el paso no salta");

            int elapsed = _rig.RunUntilIdle(3000);
            MechanicTestKit.AssertLogFollowsPlan(_rig.Animator, in v);
            AssertOwnStep(MechanicTestKit.Record(_rig.Animator, MechanicStepKind.BiteLost, 0), MechanicType.Pirana, PiranaOverlay.BiteLostStepMs);
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs));
            Assert.That(school.Dimmed, Is.True, "sigue durmiendo");
            Assert.That(school.SchoolAsleep, Is.True, "y dormido");
            Assert.That(school.FilledPips, Is.EqualTo(0));
            AssertRigShowsModel(after);
        }

        // La cueva (cuia 0) una fila más abajo, justo debajo de la cuia victim: su boca 16 + 24 = 40 dp bajo el pie de la fila
        // de arriba, contando desde lo alto de la cuia más alta de la fila (BoardLayout; 02 §5).
        private static void PlaceCaveUnder(MechanicTestKit.Rig rig, int victim)
        {
            CuiaView cave = rig.Views[0];
            CuiaView above = rig.Views[victim];
            Vector2 victimFoot = rig.Animator.Foot(victim);
            Vector2 foot = new Vector2(36f + (victim * 64f), -600f - 40f - (above.HeightU * above.Unit));
            cave.SetGeometry(foot, 40f, 60f);
            rig.Animator.BoardLaidOut();
            Assert.That(rig.Animator.Foot(0).x, Is.EqualTo(victimFoot.x).Within(0.01f), "la cueva, en la columna de la víctima");
            Assert.That(rig.Animator.Foot(0).y, Is.LessThan(victimFoot.y - 40f), "y una fila más abajo");
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

            // Calentamiento: cada cadena una vez en su banco, y después se mide otra vez en un banco nuevo. El mordisco perdido va
            // por sus ramas: la víctima sellada y el Matapi (el cardumen llega a la tapa o al embudo, choca, con su destello, y
            // vuelve; PoseLost) y la cueva llena (se asoma hacia un lado, PosePeek, o hacia arriba, PosePeekUp). Con la cueva bajo
            // la víctima, el mordisco y el mordisco perdido van por el pasillo (DetourPoint).
            for (int pass = 0; pass < 2; pass++)
            {
                RunChain(DropBoard(), 1, 3, 2, 4, pass == 1, "gota");
                RunChain(WaitBoard(), 1, 3, 2, 4, pass == 1, "espera");
                RunChain(BiteBoard(), 1, 4, 2, 3, pass == 1, "mordisco");
                RunChain(SealedVictimBoard(), 3, 4, 2, 1, pass == 1, "mordisco perdido contra la tapa", true, 1, true);
                RunChain(MatapiVictimBoard(), 3, 4, 2, 1, pass == 1, "mordisco perdido contra el embudo del Matapi", true, 1, false);
                RunChain(CaveFullBoard(), 4, 5, 3, 5, pass == 1, "mordisco perdido con la cueva llena", true, 5, false);
                RunChain(CaveFullBoard(), 4, 5, 3, 5, pass == 1, "mordisco perdido con la cueva llena y la víctima encima", true, 5, false, true);
                RunChain(BiteBoard(), 1, 4, 2, 3, pass == 1, "mordisco con la cueva bajo la víctima (por el pasillo)", false, 3, false, true);
                RunChain(SealedVictimBoard(), 3, 4, 2, 1, pass == 1, "mordisco perdido con la cueva bajo la víctima (por el pasillo)", true, 1, true, true);
            }
        }

        // Dos vertidos en un banco nuevo: el primero cuenta y el segundo dispara el paso; con measure, sin asignaciones (GC.Alloc
        // del Recorder, por la restricción AllocatingGCMemory) en los 60 primeros fotogramas desde el toque y, después, hasta la
        // vista quieta, sea cual sea la duración. Con lost, el segundo vertido pierde el mordisco (PiranaBiteLostMask) contra la
        // cuia victim, sellada si victimSealed, y el paso BiteLost lo anima la cueva.
        private void RunChain(BoardState start, int a1, int b1, int a2, int b2, bool measure, string name, bool lost = false, int victim = -1, bool victimSealed = false, bool caveUnder = false)
        {
            _rig?.Dispose();
            _rig = new MechanicTestKit.Rig(Art, start);
            if (caveUnder)
            {
                PlaceCaveUnder(_rig, victim);
            }

            BoardState counted = _rig.Pour(start, a1, b1, out _);
            _rig.RunUntilIdle(3000);
            _rig.Animator.Steps.ClearLog();
            BoardState after = _rig.Pour(counted, a2, b2, out PourVisual v);
            if (lost)
            {
                Assert.That(v.Events.PiranaBiteLostMask, Is.EqualTo(1), name + ": el vertido completa el ritmo y el mordisco se pierde");
                Assert.That(v.Events.Receiver, Is.EqualTo(victim), name + ": contra la cuia que recibe");
                Assert.That((v.Events.SealedMask & (1 << victim)) != 0, Is.EqualTo(victimSealed), name + ": la víctima, sellada por el vertido");
                Assert.That(after.IsSealed(victim), Is.EqualTo(victimSealed));
            }

            MechanicTestKit.Rig rig = _rig;
            if (measure)
            {
                Assert.That(() => { rig.Run(60 * PlayModeHelpers.FrameMs); }, Is.Not.AllocatingGCMemory(), name + ": 60 fotogramas desde el toque");
                Assert.That(() => { rig.RunUntilIdle(4000); }, Is.Not.AllocatingGCMemory(), name + ": hasta la vista quieta");
                Assert.That(rig.Animator.IsIdle, Is.True, name + ": la medida cubre hasta la vista quieta");
            }

            rig.RunUntilIdle(4000);
            if (lost)
            {
                MechanicStepRecord r = MechanicTestKit.Record(rig.Animator, MechanicStepKind.BiteLost, 0);
                Assert.That(r.Owner, Is.EqualTo(MechanicType.Pirana), name + ": el paso lo anima la cueva");
                Assert.That(r.OtherIndex, Is.EqualTo(victim), name + ": contra su víctima");
            }
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

        // La cueva vacía (k = 2) con la víctima [1, 1, 1]: 3 → 4 cuenta y 2 → 1 completa el ritmo y llena la víctima de una
        // fruta, que queda sellada; el mordisco se pierde contra su tapa (02 §9.1 M7; decisión C + D del 183).
        private static BoardState SealedVictimBoard()
        {
            return Board(
                new[] { 2, 4, 4, 4, 4, 4 },
                new[] { new int[0], new[] { 1, 1, 1 }, new[] { 2, 1 }, new[] { 2, 2 }, new[] { 2 }, new int[0] },
                new[] { new MechanicSpec(MechanicType.Pirana, k: 2), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
        }

        // Lo mismo con un Matapi [1] de víctima: 3 → 4 cuenta y 2 → 1 completa el ritmo en el Matapi (no se llena); el mordisco
        // se pierde contra su embudo (propuesta b.2 M10, regla 7).
        private static BoardState MatapiVictimBoard()
        {
            return Board(
                new[] { 2, 4, 4, 4, 4, 4 },
                new[] { new int[0], new[] { 1 }, new[] { 2, 1 }, new[] { 2, 2 }, new[] { 2 }, new int[0] },
                new[] { new MechanicSpec(MechanicType.Pirana, k: 2), new MechanicSpec(MechanicType.Matapi), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
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

        // Puntos del contador legibles a 48 dp (02 §5; revisión de T17): de ≈ 7 dp, los llenos en su color con opacidad alpha
        // y los vacíos (el aro) a emptyAlpha de esa opacidad, así que se distinguen por contraste y no por un hueco de menos de
        // 2 dp.
        private static void AssertPipsLegible(PipRow row, Color color, float alpha, float emptyAlpha, string label)
        {
            Assert.That(row.Count, Is.GreaterThan(0), label + ": puntos a la vista");
            for (int i = 0; i < row.Count; i++)
            {
                Image pip = row.Pip(i);
                Vector2 size = pip.rectTransform.sizeDelta;
                Assert.That(pip.gameObject.activeInHierarchy, Is.True, label + ": punto " + i + " a la vista");
                Assert.That(Mathf.Min(size.x, size.y), Is.GreaterThanOrEqualTo(6.4f), label + ": punto " + i + " de ≈ 7 dp");
                float expected = i < row.Filled ? alpha : alpha * emptyAlpha;
                Assert.That(pip.color.a, Is.EqualTo(expected).Within(1e-3f), label + ": opacidad del punto " + i);
                Assert.That(pip.color.r, Is.EqualTo(color.r).Within(1e-3f), label + ": color del punto " + i);
            }
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

        // Efectos de prueba: cuenta TODAS las salpicaduras que le piden y apunta el tinte de la última, antes de mirar Reducir
        // movimiento (así, con Reducir movimiento, SplashCount = 0 prueba que el overlay no la pidió, no que este doble la
        // filtró); a la capa de efectos real solo pasa lo que se vería (sin Reducir movimiento), para que el banco se vea igual.
        private sealed class RecordingFx : IBoardFx
        {
            private readonly IBoardFx _inner;

            public RecordingFx(IBoardFx inner)
            {
                _inner = inner;
            }

            public int SplashCount { get; private set; }

            public Color LastSplashTint { get; private set; }

            public bool ReduceMotion { get; set; }

            public void Clear()
            {
                SplashCount = 0;
                LastSplashTint = Color.clear;
            }

            public void Splash(Vector2 surface, Color tint, float unitDp)
            {
                SplashCount++;
                LastSplashTint = tint;
                if (!ReduceMotion)
                {
                    _inner.Splash(surface, tint, unitDp);
                }
            }

            public void SealDrops(Vector2 rim, Color tint, float unitDp)
            {
                if (!ReduceMotion)
                {
                    _inner.SealDrops(rim, tint, unitDp);
                }
            }

            public void Sparkles(Vector2 pos, Color color, int count, float unitDp)
            {
                if (!ReduceMotion)
                {
                    _inner.Sparkles(pos, color, count, unitDp);
                }
            }

            public void Shards(Vector2 pos, Color color, int count, float unitDp)
            {
                if (!ReduceMotion)
                {
                    _inner.Shards(pos, color, count, unitDp);
                }
            }

            public void Bubbles(Vector2 pos, int count, float unitDp)
            {
                if (!ReduceMotion)
                {
                    _inner.Bubbles(pos, count, unitDp);
                }
            }
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
